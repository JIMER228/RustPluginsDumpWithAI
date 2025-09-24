/*
 * Copyright (C) 2017 chucklenugget
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
 * associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software
 * is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using Oxide.Core;
  using Oxide.Core.Configuration;
  using UnityEngine;

  [Info("AuthGroups", "chucklenugget", "0.3.0")]
  public partial class AuthGroups : RustPlugin
  {
    static AuthGroups Instance;

    const string FAILURE_EFFECT = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab";

    DynamicConfigFile DataFile;
    AuthGroupManager Groups;
    Dictionary<string, Interaction> PendingInteractions;

    void Init()
    {
      Instance = this;
      DataFile = Interface.Oxide.DataFileSystem.GetFile("AuthGroups");
      Groups = new AuthGroupManager();
      PendingInteractions = new Dictionary<string, Interaction>();
    }

    void OnServerInitialized()
    {
      List<AuthGroupInfo> infos = new List<AuthGroupInfo>();

      try
      {
        infos = DataFile.ReadObject<List<AuthGroupInfo>>();
        Puts($"Loaded {infos.Count} auth groups.");
      }
      catch (Exception ex)
      {
        PrintError(ex.ToString());
        PrintWarning("Couldn't load data, defaulting to empty list of groups.");
      }

      Groups.Init(infos);
    }

    void OnServerSave()
    {
      DataFile.WriteObject(Groups.Serialize());
    }

    void OnPlayerSleepEnded(BasePlayer player)
    {
      foreach (AuthGroup group in Groups.GetAllByMember(player))
        group.EnsureAuthorized(player);
    }

    void OnHammerHit(BasePlayer player, HitInfo hit)
    {
      if (player == null || hit == null || hit.HitEntity == null)
        return;

      Interaction interaction;
      if (PendingInteractions.TryGetValue(player.UserIDString, out interaction))
        interaction.Handle(hit);
    }

    void OnEntityKill(BaseNetworkable networkable)
    {
      var entity = networkable as BaseEntity;

      if (entity == null || !ManagedEntity.IsSupportedEntity(entity))
        return;

      foreach (AuthGroup group in Groups.GetAllByEntity(entity))
      {
        group.RemoveEntity(entity);
        Puts($"Managed entity {entity.net.ID} was removed from auth group {group.Name} of player {group.OwnerId} since it was destroyed.");
      }
    }

    object CanPickupEntity(BaseCombatEntity entity, BasePlayer player)
    {
      var turret = entity as AutoTurret;

      if (turret != null && ShouldBlockAccess(turret, player))
      {
        PlayEffectAtEntity(turret, FAILURE_EFFECT);
        return false;
      }

      return null;
    }

    void OnLootEntity(BasePlayer player, BaseEntity entity)
    {
      var turret = entity.GetComponent<AutoTurret>();

      if (turret != null && ShouldBlockAccess(turret, player))
      {
        PlayEffectAtEntity(turret, FAILURE_EFFECT);
        NextTick(player.EndLooting);
      }
    }

    object OnTurretAuthorize(AutoTurret turret, BasePlayer player)
    {
      if (turret != null && ShouldBlockAccess(turret, player))
      {
        PlayEffectAtEntity(turret, FAILURE_EFFECT);
        return false;
      }

      return null;
    }

    object OnTurretDeauthorize(AutoTurret turret, BasePlayer player)
    {
      if (turret != null && ShouldBlockAccess(turret, player))
      {
        PlayEffectAtEntity(turret, FAILURE_EFFECT);
        return false;
      }

      return null;
    }

    bool ShouldBlockAccess(AutoTurret turret, BasePlayer player)
    {
      foreach (AuthGroup group in Groups.GetAllByEntity(turret))
      {
        if (group.HasFlag(AuthGroupFlag.TurretsLocked) && !group.HasOwner(player) && !group.HasManager(player))
          return true;
      }

      return false;
    }

    void PlayEffectAtEntity(BaseEntity entity, string effectPrefab)
    {
      Effect.server.Run(effectPrefab, entity, 0, Vector3.zero, Vector3.forward);
    }

    string FormatPlayerName(string playerId)
    {
      BasePlayer player = BasePlayer.Find(playerId);
      return (player == null) ? playerId : player.displayName;
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupAddCommand(BasePlayer player, string groupName, string[] args)
    {
      AuthGroup group = Groups.GetByOwnerOrManager(player, groupName);

      if (group == null)
      {
        SendReply(player, Messages.YouAreNotOwnerOrManagerOfGroup, groupName);
        return;
      }

      if (args.Length == 0)
      {
        PendingInteractions.Add(player.UserIDString, new AddEntityToGroup(player, group));
        SendReply(player, Messages.SelectEntityToAddToGroup, group.Name);
        return;
      }

      var playerName = args[0].Trim();
      BasePlayer member = BasePlayerEx.FindByNameOrId(playerName);

      if (member == null)
      {
        SendReply(player, Messages.NoSuchPlayer, playerName);
        return;
      }

      if (group.HasMember(member))
      {
        SendReply(player, Messages.CannotAddMemberAlreadyMemberOfGroup, member.displayName, group.Name);
        return;
      }

      group.AddMember(member);
      SendReply(player, Messages.MemberAdded, member.displayName, group.Name);
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Linq;

  public partial class AuthGroups : RustPlugin
  {
    [ChatCommand("ag")]
    void OnAuthGroupCommand(BasePlayer player, string chatCommand, string[] args)
    {
      if (args.Length == 0)
      {
        if (PendingInteractions.ContainsKey(player.UserIDString))
          OnAuthGroupCancelCommand(player);
        else
          OnAuthGroupListCommand(player);
        return;
      }

      switch (args[0].ToLowerInvariant())
      {
        case "help":
          OnAuthGroupHelpCommand(player);
          return;
        case "cancel":
          OnAuthGroupCancelCommand(player);
          return;
        case "create":
          OnAuthGroupCreateCommand(player, args[1].Trim());
          return;
        case "delete":
          OnAuthGroupDeleteCommand(player, args[1].Trim());
          return;
        default:
          break;
      }

      string groupName = args[0].Trim();

      if (args.Length == 1)
      {
        OnAuthGroupShowCommand(player, groupName);
        return;
      }

      string command = args[1].ToLowerInvariant();
      string[] commandArgs = args.Skip(2).ToArray();

      switch (command)
      {
        case "show":
          OnAuthGroupShowCommand(player, groupName);
          break;
        case "add":
          OnAuthGroupAddCommand(player, groupName, commandArgs);
          break;
        case "remove":
          OnAuthGroupRemoveCommand(player, groupName, commandArgs);
          break;
        case "promote":
          OnAuthGroupPromoteCommand(player, groupName, commandArgs);
          break;
        case "demote":
          OnAuthGroupDemoteCommand(player, groupName, commandArgs);
          break;
        case "sync":
          OnAuthGroupSyncCommand(player, groupName);
          return;
        case "turrets":
          OnAuthGroupTurretsCommand(player, groupName, commandArgs);
          break;
        case "codelocks":
          OnAuthGroupCodeLocksCommand(player, groupName, commandArgs);
          break;
        default:
          OnAuthGroupHelpCommand(player);
          break;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupCancelCommand(BasePlayer player)
    {
      if (PendingInteractions.Remove(player.UserIDString))
        SendReply(player, Messages.InteractionCanceled);
      else
        OnAuthGroupHelpCommand(player);
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupCodeLocksCommand(BasePlayer player, string groupName, string[] args)
    {
      string command = (args.Length == 0) ? null : args[0].ToLowerInvariant();

      if (command == null)
      {
        OnAuthGroupCodeLocksHelpCommand(player);
        return;
      }

      AuthGroup group = Groups.GetByOwnerOrManager(player, groupName);

      if (group == null)
      {
        SendReply(player, Messages.YouAreNotOwnerOrManagerOfGroup, groupName);
        return;
      }

      var codeLocks = group.GetAllManagedEntitiesOfType<ManagedCodeLock>();

      if (codeLocks.Length == 0)
      {
        SendReply(player, Messages.CannotSendLocksCommandNoLocksInGroup, group.Name);
        return;
      }

      string code = (args.Length < 2) ? null : args[1].Trim();

      switch (command)
      {
        case "code":
          if (code == null || code.Length != 4)
          {
            OnAuthGroupCodeLocksHelpCommand(player);
            break;
          }
          foreach (ManagedCodeLock codeLock in codeLocks)
            codeLock.SetCode(code);
          SendReply(player, Messages.AllLocksCodeChanged, group.Name);
          break;

        case "guestcode":
          if (code == null || code.Length != 4)
          {
            OnAuthGroupCodeLocksHelpCommand(player);
            break;
          }
          foreach (ManagedCodeLock codeLock in codeLocks)
            codeLock.SetGuestCode(code);
          SendReply(player, Messages.AllLocksGuestCodeChanged, group.Name);
          break;

        case "lock":
          foreach (ManagedCodeLock codeLock in codeLocks)
            codeLock.SetIsLocked(true);
          SendReply(player, Messages.AllLocksLocked, group.Name);
          break;

        case "unlock":
          foreach (ManagedCodeLock codeLock in codeLocks)
            codeLock.SetIsLocked(false);
          SendReply(player, Messages.AllLocksUnlocked, group.Name);
          break;

        default:
          OnAuthGroupTurretsHelpCommand(player);
          break;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Text;

  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupCodeLocksHelpCommand(BasePlayer player)
    {
      var sb = new StringBuilder();

      sb.AppendLine("The following commands can be sent to locks:");
      sb.AppendLine("  <color=#ffd479>/ag NAME codelocks code NNNN</color> Change the master code on all codelocks");
      sb.AppendLine("  <color=#ffd479>/ag NAME codelocks guestcode NNNN</color> Change the guest code on all codelocks");
      sb.AppendLine("  <color=#ffd479>/ag NAME codelocks lock|unlock</color> Lock or unlock all codelocks");

      SendReply(player, sb.ToString());
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Text;

  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupCreateCommand(BasePlayer player, string groupName)
    {
      AuthGroup existingGroup = Groups.GetByOwnerOrManager(player, groupName);

      if (existingGroup != null)
      {
        SendReply(player, Messages.CannotCreateGroupOneAlreadyExists, groupName);
        return;
      }

      AuthGroup group = Groups.Create(player, groupName);
      SendReply(player, Messages.GroupCreated, groupName);
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupDeleteCommand(BasePlayer player, string groupName)
    {
      AuthGroup group = Groups.GetByOwnerOrManager(player, groupName);

      if (group == null)
      {
        SendReply(player, Messages.YouAreNotOwnerOrManagerOfGroup, groupName);
        return;
      }

      Groups.Delete(group);
      SendReply(player, Messages.GroupDeleted, group.Name);
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupDemoteCommand(BasePlayer player, string groupName, string[] args)
    {
      if (args.Length == 0)
      {
        SendReply(player, "<color=#ffd479>Usage:</color> /ag NAME demote PLAYER");
        return;
      }

      AuthGroup group = Groups.GetByOwnerOrManager(player, groupName);

      if (group == null)
      {
        SendReply(player, Messages.YouAreNotOwnerOrManagerOfGroup, groupName);
        return;
      }

      var playerName = args[0].Trim();
      BasePlayer member = BasePlayerEx.FindByNameOrId(playerName);

      if (member == null)
      {
        SendReply(player, Messages.NoSuchPlayer, playerName);
        return;
      }

      if (group.HasOwner(member))
      {
        SendReply(player, Messages.CannotDemoteIsOwnerOfGroup, member.displayName, group.Name);
        return;
      }

      if (!group.HasManager(member))
      {
        SendReply(player, Messages.CannotDemoteNotManagerOfGroup, member.displayName, group.Name);
        return;
      }

      group.Demote(member);
      SendReply(player, Messages.ManagerRemoved, member.displayName, group.Name);
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Text;

  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupHelpCommand(BasePlayer player)
    {
      var sb = new StringBuilder();

      sb.AppendLine($"The following commands are available:");
      sb.AppendLine("  <color=#ffd479>/ag</color> List auth groups you are managing");
      sb.AppendLine("  <color=#ffd479>/ag create NAME</color> Create a new auth group");
      sb.AppendLine("  <color=#ffd479>/ag delete NAME</color> Delete an auth group you own");
      sb.AppendLine("  <color=#ffd479>/ag NAME</color> Show info about an auth group");
      sb.AppendLine("  <color=#ffd479>/ag NAME add|remove</color> Add/remove items to/from an auth group");
      sb.AppendLine("  <color=#ffd479>/ag NAME add|remove PLAYER</color> Add/remove a player to/from an auth group");
      sb.AppendLine("  <color=#ffd479>/ag NAME promote|demote PLAYER</color> Add/remove a player as manager of an auth group");
      sb.AppendLine("  <color=#ffd479>/ag NAME sync</color> Ensure that only group members are auth'd on the items in an auth group");
      sb.AppendLine("  <color=#ffd479>/ag NAME turrets</color> Send a command to all turrets in an auth group");
      sb.AppendLine("  <color=#ffd479>/ag NAME codelocks</color> Send a command to all locks in an auth group");

      SendReply(player, sb.ToString());
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Text;

  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupListCommand(BasePlayer player)
    {
      var sb = new StringBuilder();
      AuthGroup[] groups = Groups.GetAllByOwnerOrManager(player);

      sb.AppendLine($"<size=18>AuthGroups</size> v{Version} by chucklenugget");

      if (groups.Length == 0)
      {
        sb.AppendLine("You are not a manager of any auth groups.");
      }
      else
      {
        sb.AppendLine($"You are a manager of {groups.Length} auth group(s):");
        foreach (AuthGroup group in groups)
          sb.AppendLine($"  <color=#ffd479>{group.Name}</color>");
      }

      sb.AppendLine("To learn more about auth groups, type <color=#ffd479>/ag help</color>.");

      SendReply(player, sb.ToString());
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupPromoteCommand(BasePlayer player, string groupName, string[] args)
    {
      if (args.Length == 0)
      {
        SendReply(player, "<color=#ffd479>Usage:</color> /ag NAME promote PLAYER");
        return;
      }

      AuthGroup group = Groups.GetByOwnerOrManager(player, groupName);

      if (group == null)
      {
        SendReply(player, Messages.YouAreNotOwnerOrManagerOfGroup, groupName);
        return;
      }

      var playerName = args[0].Trim();
      BasePlayer member = BasePlayerEx.FindByNameOrId(playerName);

      if (member == null)
      {
        SendReply(player, Messages.NoSuchPlayer, playerName);
        return;
      }

      if (!group.HasMember(member))
      {
        SendReply(player, Messages.CannotPromoteNotMemberOfGroup, member.displayName, group.Name);
        return;
      }

      if (group.HasOwner(member))
      {
        SendReply(player, Messages.CannotPromoteIsOwnerOfGroup, member.displayName, group.Name);
        return;
      }

      if (group.HasManager(member))
      {
        SendReply(player, Messages.CannotPromoteAlreadyManagerOfGroup, member.displayName, group.Name);
        return;
      }

      group.Promote(member);
      SendReply(player, Messages.ManagerAdded, member.displayName, group.Name);
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupRemoveCommand(BasePlayer player, string groupName, string[] args)
    {
      AuthGroup group = Groups.GetByOwnerOrManager(player, groupName);

      if (group == null)
      {
        SendReply(player, Messages.YouAreNotOwnerOrManagerOfGroup, groupName);
        return;
      }

      if (args.Length == 0)
      {
        PendingInteractions.Add(player.UserIDString, new RemoveEntityFromGroup(player, group));
        SendReply(player, Messages.SelectEntityToRemoveFromGroup, group.Name);
        return;
      }

      var playerName = args[0].Trim();
      BasePlayer member = BasePlayerEx.FindByNameOrId(playerName);

      if (member == null)
      {
        SendReply(player, Messages.NoSuchPlayer, playerName);
        return;
      }

      if (!group.HasMember(member))
      {
        SendReply(player, Messages.CannotRemoveMemberNotMemberOfGroup, member.displayName, group.Name);
        return;
      }

      group.RemoveMember(member);
      SendReply(player, Messages.MemberRemoved, member.displayName, group.Name);
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Linq;
  using System.Text;

  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupShowCommand(BasePlayer player, string groupName)
    {
      AuthGroup group = Groups.GetByOwnerOrManager(player, groupName);

      if (group == null)
      {
        SendReply(player, Messages.YouAreNotOwnerOrManagerOfGroup, groupName);
        return;
      }

      var sb = new StringBuilder();

      sb.Append($"Auth group <color=#ffd479>{group.Name}</color> ");

      if (group.Entities.Count == 0)
        sb.AppendLine("isn't managing any items.");
      else
        sb.AppendLine($"is managing <color=#ffd479>{group.Entities.Count}</color> items:");

      var cupboards = group.GetAllManagedEntitiesOfType<ManagedToolCupboard>();
      if (cupboards.Length > 0)
        sb.AppendLine($"  <color=#ffd479>{cupboards.Length}</color> tool cupboard(s)");

      var turrets = group.GetAllManagedEntitiesOfType<ManagedAutoTurret>();
      if (turrets.Length > 0)
        sb.AppendLine($"  <color=#ffd479>{turrets.Length}</color> turret(s)");

      var codeLocks = group.GetAllManagedEntitiesOfType<ManagedCodeLock>();
      if (codeLocks.Length > 0)
        sb.AppendLine($"  <color=#ffd479>{codeLocks.Length}</color> code lock(s)");

      sb.AppendLine($"Owner: {FormatPlayerName(group.OwnerId)}");

      if (group.ManagerIds.Count > 0)
      {
        sb.Append($"<color=#ffd479>{group.ManagerIds.Count}</color> managers: ");
        sb.AppendLine(String.Join(", ", group.ManagerIds.Select(FormatPlayerName).ToArray()));
      }

      sb.Append($"<color=#ffd479>{group.MemberIds.Count}</color> members: ");
      sb.AppendLine(String.Join(", ", group.MemberIds.Select(FormatPlayerName).ToArray()));

      SendReply(player, sb.ToString());
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupSyncCommand(BasePlayer player, string groupName)
    {
      AuthGroup group = Groups.GetByOwnerOrManager(player, groupName);

      if (group == null)
      {
        SendReply(player, Messages.YouAreNotOwnerOrManagerOfGroup, groupName);
        return;
      }

      group.SynchronizeAll();
      SendReply(player, Messages.GroupSynchronized, group.Name, group.MemberIds.Count);
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupTurretsCommand(BasePlayer player, string groupName, string[] args)
    {
      string command = (args.Length == 0) ? null : args[0].ToLowerInvariant();

      if (command == null)
      {
        OnAuthGroupTurretsHelpCommand(player);
        return;
      }

      AuthGroup group = Groups.GetByOwnerOrManager(player, groupName);

      if (group == null)
      {
        SendReply(player, Messages.YouAreNotOwnerOrManagerOfGroup, groupName);
        return;
      }

      var turrets = group.GetAllManagedEntitiesOfType<ManagedAutoTurret>();

      if (turrets.Length == 0)
      {
        SendReply(player, Messages.CannotSendTurretsCommandNoTurretsInGroup, group.Name);
        return;
      }

      switch (command)
      {
        case "on":
          foreach (ManagedAutoTurret turret in turrets)
            turret.SetIsOnline(true);
          SendReply(player, Messages.AllTurretsSetOn, group.Name);
          break;

        case "off":
          foreach (ManagedAutoTurret turret in turrets)
            turret.SetIsOnline(false);
          SendReply(player, Messages.AllTurretsSetOff, group.Name);
          break;

        case "hostile":
          foreach (ManagedAutoTurret turret in turrets)
            turret.SetIsPeacekeeper(false);
          SendReply(player, Messages.AllTurretsSetHostile, group.Name);
          break;

        case "peacekeeper":
          foreach (ManagedAutoTurret turret in turrets)
            turret.SetIsPeacekeeper(true);
          SendReply(player, Messages.AllTurretsSetPeacekeeper, group.Name);
          break;

        case "lock":
          group.AddFlag(AuthGroupFlag.TurretsLocked);
          SendReply(player, Messages.AllTurretsLocked, group.Name);
          break;

        case "unlock":
          group.RemoveFlag(AuthGroupFlag.TurretsLocked);
          SendReply(player, Messages.AllTurretsUnlocked, group.Name);
          break;

        default:
          OnAuthGroupTurretsHelpCommand(player);
          break;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Text;

  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupTurretsHelpCommand(BasePlayer player)
    {
      var sb = new StringBuilder();

      sb.AppendLine("The following commands can be sent to turrets:");
      sb.AppendLine("  <color=#ffd479>/ag NAME turrets on|off</color> Turn all turrets on or off");
      sb.AppendLine("  <color=#ffd479>/ag NAME turrets peacekeeper|hostile</color> Turn on or off peacekeeper mode");
      sb.AppendLine("  <color=#ffd479>/ag NAME turrets lock|unlock</color> Restrict opening or authing on turrets to group managers only");

      SendReply(player, sb.ToString());
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Converters;

  public partial class AuthGroups : RustPlugin
  {
    class AuthGroup
    {
      public string Name;
      public string OwnerId;
      public HashSet<string> MemberIds;
      public HashSet<string> ManagerIds;
      public HashSet<ManagedEntity> Entities;
      public AuthGroupFlag Flags;

      public AuthGroup()
      {
        MemberIds = new HashSet<string>();
        ManagerIds = new HashSet<string>();
        Entities = new HashSet<ManagedEntity>();
      }

      public AuthGroup(BasePlayer owner, string name)
        : this()
      {
        OwnerId = owner.UserIDString;
        Name = name;
      }

      public bool AddMember(BasePlayer player)
      {
        return AddMember(player.UserIDString);
      }

      public bool AddMember(string playerId)
      {
        if (HasMember(playerId))
          return false;

        MemberIds.Add(playerId);

        BasePlayer player = BasePlayer.Find(playerId);
        if (player != null)
          EnsureAuthorized(player);

        return true;
      }

      public bool RemoveMember(BasePlayer player)
      {
        return RemoveMember(player.UserIDString);
      }

      public bool RemoveMember(string playerId)
      {
        if (HasOwner(playerId))
          throw new InvalidOperationException($"Cannot remove player {playerId} from auth group, since they are the owner");

        if (!HasMember(playerId))
          return false;

        MemberIds.Remove(playerId);
        ManagerIds.Remove(playerId);

        BasePlayer player = BasePlayer.Find(playerId);
        if (player != null)
          EnsureDeauthorized(player);

        return true;
      }

      public bool Promote(BasePlayer player)
      {
        return Promote(player.UserIDString);
      }

      public bool Promote(string playerId)
      {
        if (!MemberIds.Contains(playerId))
          throw new InvalidOperationException($"Cannot promote player {playerId} in group {Name} owned by {OwnerId}, since they are not a member");

        return ManagerIds.Add(playerId);
      }

      public bool Demote(BasePlayer player)
      {
        return Demote(player.UserIDString);
      }

      public bool Demote(string playerId)
      {
        if (!MemberIds.Contains(playerId))
          throw new InvalidOperationException($"Cannot demote player {playerId} in group {Name} owned by {OwnerId}, since they are not a member");

        return ManagerIds.Remove(playerId);
      }

      public bool AddEntity(ManagedEntity entity)
      {
        if (Entities.Any(e => e.Id == entity.Id))
          return false;

        Entities.Add(entity);

        entity.DeauthorizeAll();
        entity.Authorize(GetActiveAndSleepingMembers());

        return true;
      }

      public bool RemoveEntity(ManagedEntity entity)
      {
        if (!Entities.Remove(entity))
          return false;

        entity.DeauthorizeAll();
        return true;
      }

      public bool RemoveEntity(BaseEntity entity)
      {
        ManagedEntity managedEntity = GetManagedEntity(entity);

        if (managedEntity == null)
          return false;

        return RemoveEntity(managedEntity);
      }

      public void EnsureAuthorized(BasePlayer player)
      {
        foreach (ManagedEntity entity in Entities.Where(entity => !entity.IsAuthorized(player)))
          entity.Authorize(player);
      }

      public void EnsureDeauthorized(BasePlayer player)
      {
        foreach (ManagedEntity entity in Entities.Where(entity => entity.IsAuthorized(player)))
          entity.Deauthorize(player);
      }

      public void SynchronizeAll()
      {
        foreach (ManagedEntity entity in Entities)
          entity.SetAuthorizedPlayers(GetActiveAndSleepingMembers());
      }

      public void DeauthorizeAll()
      {
        foreach (ManagedEntity entity in Entities)
          entity.DeauthorizeAll();
      }

      public ManagedEntity GetManagedEntity(BaseEntity entity)
      {
        return Entities.FirstOrDefault(e => e.Id == entity.net.ID);
      }

      public BasePlayer[] GetActiveAndSleepingMembers()
      {
        return MemberIds.Select(id => BasePlayer.Find(id)).Where(p => p != null).ToArray();
      }

      public T[] GetAllManagedEntitiesOfType<T>() where T : ManagedEntity
      {
        return Entities.OfType<T>().ToArray();
      }

      public bool HasMember(BasePlayer basePlayer)
      {
        return HasMember(basePlayer.UserIDString);
      }

      public bool HasMember(string playerId)
      {
        return MemberIds.Contains(playerId);
      }

      public bool HasManager(BasePlayer basePlayer)
      {
        return HasManager(basePlayer.UserIDString);
      }

      public bool HasManager(string playerId)
      {
        return ManagerIds.Contains(playerId);
      }

      public bool HasOwner(BasePlayer basePlayer)
      {
        return HasOwner(basePlayer.UserIDString);
      }

      public bool HasOwner(string playerId)
      {
        return OwnerId == playerId;
      }

      public bool HasEntity(BaseEntity entity)
      {
        return GetManagedEntity(entity) != null;
      }
      
      public bool HasFlag(AuthGroupFlag flag)
      {
        return (Flags & flag) != 0;
      }

      public void AddFlag(AuthGroupFlag flag)
      {
        Flags |= flag;
      }

      public void RemoveFlag(AuthGroupFlag flag)
      {
        Flags &= ~flag;
      }

      public AuthGroupInfo Serialize()
      {
        return new AuthGroupInfo {
          Name = Name,
          OwnerId = OwnerId,
          Flags = Flags,
          ManagerIds = ManagerIds.ToArray(),
          MemberIds = MemberIds.ToArray(),
          EntityIds = Entities.Select(entity => entity.Id).ToArray()
        };
      }
    }

    public class AuthGroupInfo
    {
      [JsonProperty("name")]
      public string Name;

      [JsonProperty("ownerId")]
      public string OwnerId;

      [JsonProperty("flags"), JsonConverter(typeof(StringEnumConverter))]
      public AuthGroupFlag Flags;

      [JsonProperty("memberIds")]
      public string[] MemberIds;

      [JsonProperty("managerIds")]
      public string[] ManagerIds;

      [JsonProperty("entityIds")]
      public uint[] EntityIds;
    }

    [Flags]
    public enum AuthGroupFlag
    {
      TurretsLocked = 1
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  public partial class AuthGroups : RustPlugin
  {
    class AuthGroupManager
    {
      List<AuthGroup> Groups = new List<AuthGroup>();

      public void Init(IEnumerable<AuthGroupInfo> infos)
      {
        foreach (AuthGroupInfo info in infos)
        {
          Groups.Add(new AuthGroup {
            Name = info.Name,
            OwnerId = info.OwnerId,
            Flags = info.Flags,
            MemberIds = new HashSet<string>(info.MemberIds),
            ManagerIds = new HashSet<string>(info.ManagerIds),
            Entities = new HashSet<ManagedEntity>(FindEntities(info.EntityIds).Select(ManagedEntity.Create))
          });
        }
      }

      public AuthGroup GetByOwnerOrManager(BasePlayer player, string name)
      {
        return GetAllByOwnerOrManager(player).FirstOrDefault(group => String.Compare(group.Name, name, true) == 0);
      }

      public AuthGroup[] GetAllByOwnerOrManager(BasePlayer player)
      {
        return Groups.Where(group => group.HasOwner(player) || group.HasManager(player)).ToArray();
      }

      public AuthGroup[] GetAllByMember(BasePlayer player)
      {
        return Groups.Where(group => group.HasMember(player)).ToArray();
      }

      public AuthGroup[] GetAllByEntity(BaseEntity entity)
      {
        return Groups.Where(group => group.HasEntity(entity)).ToArray();
      }

      public AuthGroup Create(BasePlayer owner, string name)
      {
        if (GetByOwnerOrManager(owner, name) != null)
          throw new InvalidOperationException($"Player ${owner.userID} is already the manager of an auth group named {name}!");

        var group = new AuthGroup(owner, name);
        Groups.Add(group);

        return group;
      }

      public void Delete(AuthGroup group)
      {
        group.DeauthorizeAll();
        Groups.Remove(group);
      }

      public AuthGroupInfo[] Serialize()
      {
        return Groups.Select(group => group.Serialize()).ToArray();
      }

      IEnumerable<BaseEntity> FindEntities(IEnumerable<uint> ids)
      {
        return ids.Select(FindEntity).Where(entity => entity != null);
      }

      BaseEntity FindEntity(uint id)
      {
        var entity = BaseNetworkable.serverEntities.Find(id) as BaseEntity;

        if (entity == null)
          Instance.PrintWarning($"Couldn't find entity {id} for an auth group!");

        return entity;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Linq;

  public partial class AuthGroups : RustPlugin
  {
    public static class BasePlayerEx
    {
      public static BasePlayer FindByNameOrId(string searchString)
      {
        return FindById(searchString) ?? FindByName(searchString);
      }

      public static BasePlayer FindById(string id)
      {
        return BasePlayer.activePlayerList.Concat(BasePlayer.sleepingPlayerList)
          .FirstOrDefault(p => p.UserIDString == id);
      }

      public static BasePlayer FindByName(string searchString)
      {
        return BasePlayer.activePlayerList.Concat(BasePlayer.sleepingPlayerList)
          .Where(p => p.displayName.ToLowerInvariant().Contains(searchString.ToLowerInvariant()))
          .OrderBy(p => GetLevenshteinDistance(searchString.ToLowerInvariant(), p.displayName.ToLowerInvariant()))
          .FirstOrDefault();
      }

      static int GetLevenshteinDistance(string source, string target)
      {
        if (String.IsNullOrEmpty(source) && String.IsNullOrEmpty(target))
          return 0;

        if (source.Length == target.Length)
          return source.Length;

        if (source.Length == 0)
          return target.Length;

        if (target.Length == 0)
          return source.Length;

        var distance = new int[source.Length + 1, target.Length + 1];

        for (int idx = 0; idx <= source.Length; distance[idx, 0] = idx++) ;
        for (int idx = 0; idx <= target.Length; distance[0, idx] = idx++) ;

        for (int i = 1; i <= source.Length; i++)
        {
          for (int j = 1; j <= target.Length; j++)
          {
            int cost = target[j - 1] == source[i - 1] ? 0 : 1;
            distance[i, j] = Math.Min(
              Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
              distance[i - 1, j - 1] + cost
            );
          }
        }

        return distance[source.Length, target.Length];
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    static class Messages
    {
      public const string YouAreNotOwnerOrManagerOfGroup = "You are not the owner or manager of an auth group named <color=#ffd479>{0}</color>.";
      public const string NoSuchPlayer = "No player with the name or id <color=#ffd479>{0}</color> was found.";
      public const string InteractionCanceled = "Command canceled.";
      public const string InvalidEntitySelected = "That item cannot be added to an auth group.";

      public const string CannotCreateGroupOneAlreadyExists = "You are already the manager of an auth group named <color=#ffd479>{0}</color>!";
      public const string GroupCreated = "You have created the auth group <color=#ffd479>{0}</color>.";
      public const string GroupDeleted = "You have deleted the auth group <color=#ffd479>{0}</color>. The auth lists of all items in the group have been cleared.";

      public const string MemberAdded = "You have added <color=#ffd479>{0}</color> as a member of the auth group <color=#ffd479>{1}</color>.";
      public const string MemberRemoved = "You have removed <color=#ffd479>{0}</color> as a member of the auth group <color=#ffd479>{1}</color>.";
      public const string ManagerAdded = "You have added <color=#ffd479>{0}</color> as a manager of the auth group <color=#ffd479>{1}</color>.";
      public const string ManagerRemoved = "You have removed <color=#ffd479>{0}</color> as a manager of the auth group <color=#ffd479>{1}</color>.";
      public const string CannotAddMemberAlreadyMemberOfGroup = "The player <color=#ffd479>{0}</color> is already a member of the auth group <color=#ffd479>{1}</color>.";
      public const string CannotRemoveMemberNotMemberOfGroup = "The player <color=#ffd479>{0}</color> is not a member of the auth group <color=#ffd479>{1}</color>.";
      public const string CannotPromoteNotMemberOfGroup = "The player <color=#ffd479>{0}</color> is not a member of the auth group <color=#ffd479>{1}</color>.";
      public const string CannotPromoteAlreadyManagerOfGroup = "The player <color=#ffd479>{0}</color> is already a manager of the auth group <color=#ffd479>{1}</color>.";
      public const string CannotDemoteNotManagerOfGroup = "The player <color=#ffd479>{0}</color> is not a manager of the auth group <color=#ffd479>{1}</color>.";
      public const string CannotPromoteIsOwnerOfGroup = "The player <color=#ffd479>{0}</color> is the already the owner of the auth group <color=#ffd479>{1}</color>.";
      public const string CannotDemoteIsOwnerOfGroup = "The player <color=#ffd479>{0}</color> cannot be demoted, since they are the owner of the auth group <color=#ffd479>{1}</color>.";

      public const string SelectEntityToAddToGroup = "Use the hammer to select a tool cupboard or auto turret to add to the auth group <color=#ffd479>{0}</color>. Say <color=#ffd479>/ag</color> to cancel.";
      public const string SelectEntityToRemoveFromGroup = "Use the hammer to select a tool cupboard or auto turret to remove from the auth group <color=#ffd479>{0}</color>. Say <color=#ffd479>/ag</color> to cancel.";

      public const string EntityAdded = "You have added this item to the auth group <color=#ffd479>{0}</color>. <color=#ffd479>{1}</color> members have been authorized.";
      public const string EntityRemoved = "You have removed this item from the auth group <color=#ffd479>{0}</color>, and its auth list has been cleared.";
      public const string GroupSynchronized = "The auth list of all items in the group <color=#ffd479>{0}</color> has been cleared, and the <color=#ffd479>{1}</color> members of the group have been re-added.";

      public const string CannotSendTurretsCommandNoTurretsInGroup = "There are no turrets in the group <color=#ffd479>{0}</color>!";
      public const string AllTurretsSetOn = "All turrets in the auth group <color=#ffd479>{0}</color> have been turned on.";
      public const string AllTurretsSetOff = "All turrets in the auth group <color=#ffd479>{0}</color> have been turned off.";
      public const string AllTurretsSetHostile = "All turrets in the auth group <color=#ffd479>{0}</color> have been turned hostile.";
      public const string AllTurretsSetPeacekeeper = "All turrets in the auth group <color=#ffd479>{0}</color> have been turned to peacekeeper mode.";
      public const string AllTurretsLocked = "All turrets in the group <color=#ffd479>{0}</color> are now locked, and will only be accessible by group managers.";
      public const string AllTurretsUnlocked = "All turrets in the group <color=#ffd479>{0}</color> are now unlocked.";

      public const string CannotSendLocksCommandNoLocksInGroup = "There are no codelocks in the group <color=#ffd479>{0}</color>!";
      public const string AllLocksCodeChanged = "The code for all codelocks in the auth group <color=#ffd479>{0}</color> has been changed.";
      public const string AllLocksGuestCodeChanged = "The guest code for all codelocks in the auth group <color=#ffd479>{0}</color> has been changed.";
      public const string AllLocksLocked = "All codelocks in the auth group <color=#ffd479>{0}</color> have been locked.";
      public const string AllLocksUnlocked = "All codelocks in the auth group <color=#ffd479>{0}</color> have been unlocked.";

      public const string CannotAddEntityNotAuthorized = "You must be authorized on the item to add it to the auth group.";
      public const string CannotAddEntityAlreadyEnrolledInGroup = "That item is already enrolled in the auth group <color=#ffd479>{0}</color>.";
      public const string CannotRemoveEntityNotAuthorized = "You must be authorized on the item to remove it from the auth group.";
      public const string CannotRemoveEntityNotEnrolledInGroup = "That item is not enrolled in the auth group <color=#ffd479>{0}</color>.";
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  public partial class AuthGroups : RustPlugin
  {
    class ManagedAutoTurret : ManagedEntity
    {
      AutoTurret Turret;

      public ManagedAutoTurret(AutoTurret turret)
        : base(turret)
      {
        Turret = turret;
      }

      public override void Authorize(BasePlayer player)
      {
        Turret.authorizedPlayers.Add(CreateAuthListEntry(player));
        Turret.SendNetworkUpdate();
      }

      public override void Authorize(IEnumerable<BasePlayer> players)
      {
        Turret.authorizedPlayers.AddRange(players.Select(player => CreateAuthListEntry(player)));
        Turret.SendNetworkUpdate();
      }

      public override void Deauthorize(BasePlayer player)
      {
        Turret.authorizedPlayers.RemoveAll(entry => entry.userid == player.userID);
        Turret.SendNetworkUpdate();
        Turret.SetTarget(null);
      }

      public override void Deauthorize(IEnumerable<BasePlayer> players)
      {
        Turret.authorizedPlayers.RemoveAll(entry => players.Any(player => player.userID == entry.userid));
        Turret.SendNetworkUpdate();
        Turret.SetTarget(null);
      }

      public override void DeauthorizeAll()
      {
        Turret.authorizedPlayers.Clear();
        Turret.SetIsOnline(false);
        Turret.SendNetworkUpdate();
        Turret.SetTarget(null);
      }

      public override BasePlayer[] GetAuthorizedPlayers()
      {
        return Turret.authorizedPlayers.Select(entry => BasePlayer.FindByID(entry.userid)).ToArray();
      }

      public override void SetAuthorizedPlayers(IEnumerable<BasePlayer> players)
      {
        Turret.authorizedPlayers.Clear();
        Turret.authorizedPlayers.AddRange(players.Select(player => CreateAuthListEntry(player)));
        Turret.SendNetworkUpdate();
        Turret.SetTarget(null);
      }

      public override bool IsAuthorized(BasePlayer player)
      {
        return Turret.IsAuthed(player);
      }

      public void SetIsOnline(bool online)
      {
        Turret.SetIsOnline(online);
        Turret.SendNetworkUpdate();
      }

      public void SetIsPeacekeeper(bool peacekeeper)
      {
        Turret.SetPeacekeepermode(peacekeeper);
        Turret.SendNetworkUpdate();
        Turret.SetTarget(null);
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class AuthGroups : RustPlugin
  {
    class ManagedCodeLock : ManagedEntity
    {
      CodeLock CodeLock;

      public ManagedCodeLock(CodeLock codeLock)
        : base(codeLock)
      {
        CodeLock = codeLock;
      }

      public override void Authorize(BasePlayer player)
      {
        if (!CodeLock.guestPlayers.Contains(player.userID))
          CodeLock.guestPlayers.Add(player.userID);
        CodeLock.SendNetworkUpdate();
      }

      public override void Authorize(IEnumerable<BasePlayer> players)
      {
        CodeLock.guestPlayers.AddRange(players.Select(player => player.userID));
        CodeLock.SendNetworkUpdate();
      }

      public override void Deauthorize(BasePlayer player)
      {
        CodeLock.guestPlayers.RemoveAll(entry => entry == player.userID);
        CodeLock.SendNetworkUpdate();
      }

      public override void Deauthorize(IEnumerable<BasePlayer> players)
      {
        CodeLock.guestPlayers.RemoveAll(entry => players.Any(player => player.userID == entry));
        CodeLock.SendNetworkUpdate();
      }

      public override void DeauthorizeAll()
      {
        CodeLock.guestPlayers.Clear();
        CodeLock.SendNetworkUpdate();
      }

      public override BasePlayer[] GetAuthorizedPlayers()
      {
        return CodeLock.guestPlayers.Select(entry => BasePlayer.FindByID(entry)).ToArray();
      }

      public override void SetAuthorizedPlayers(IEnumerable<BasePlayer> players)
      {
        CodeLock.guestPlayers.Clear();
        CodeLock.guestPlayers.AddRange(players.Select(player => player.userID));
        CodeLock.SendNetworkUpdate();
      }

      public override bool IsAuthorized(BasePlayer player)
      {
        return CodeLock.whitelistPlayers.Contains(player.userID) || CodeLock.guestPlayers.Contains(player.userID);
      }

      public void SetIsLocked(bool locked)
      {
        PlayEffectAtEntity(locked ? CodeLock.effectLocked : CodeLock.effectUnlocked);
        CodeLock.SetFlag(BaseEntity.Flags.Locked, locked, false);
        CodeLock.SendNetworkUpdate();
      }

      public void SetCode(string code)
      {
        PlayEffectAtEntity(CodeLock.effectCodeChanged);
        CodeLock.code = code;
        CodeLock.SendNetworkUpdate();
      }

      public void SetGuestCode(string code)
      {
        PlayEffectAtEntity(CodeLock.effectCodeChanged);
        CodeLock.guestCode = code;
        CodeLock.SendNetworkUpdate();
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using UnityEngine;

  public partial class AuthGroups : RustPlugin
  {
    abstract class ManagedEntity
    {
      public BaseEntity Entity { get; }
      public uint Id { get; }

      public abstract void Authorize(BasePlayer player);
      public abstract void Authorize(IEnumerable<BasePlayer> players);
      public abstract void Deauthorize(BasePlayer player);
      public abstract void Deauthorize(IEnumerable<BasePlayer> players);
      public abstract void DeauthorizeAll();
      public abstract void SetAuthorizedPlayers(IEnumerable<BasePlayer> players);
      public abstract BasePlayer[] GetAuthorizedPlayers();
      public abstract bool IsAuthorized(BasePlayer player);

      public static bool IsSupportedEntity(BaseEntity entity)
      {
        return (entity is BuildingPrivlidge) || (entity is AutoTurret) || (entity is CodeLock);
      }

      public static ManagedEntity Create(BaseEntity entity)
      {
        var cupboard = entity as BuildingPrivlidge;
        if (cupboard != null)
          return new ManagedToolCupboard(cupboard);

        var turret = entity as AutoTurret;
        if (turret != null)
          return new ManagedAutoTurret(turret);

        var codeLock = entity as CodeLock;
        if (codeLock != null)
          return new ManagedCodeLock(codeLock);

        codeLock = entity.GetSlot(BaseEntity.Slot.Lock) as CodeLock;
        if (codeLock != null)
          return new ManagedCodeLock(codeLock);

        return null;
      }

      protected ManagedEntity(BaseEntity entity)
      {
        Entity = entity;
        Id = entity.net.ID;
      }

      protected ProtoBuf.PlayerNameID CreateAuthListEntry(BasePlayer player, bool shouldPool = true)
      {
        return new ProtoBuf.PlayerNameID {
          userid = player.userID,
          username = player.displayName,
          ShouldPool = shouldPool
        };
      }

      public void PlayEffectAtEntity(GameObjectRef effect)
      {
        Effect.server.Run(effect.resourcePath, Entity, 0, Vector3.zero, Vector3.forward);
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class AuthGroups : RustPlugin
  {
    class ManagedToolCupboard : ManagedEntity
    {
      BuildingPrivlidge Cupboard;

      public ManagedToolCupboard(BuildingPrivlidge cupboard)
        : base(cupboard)
      {
        Cupboard = cupboard;
      }

      public override void Authorize(BasePlayer player)
      {
        Cupboard.authorizedPlayers.Add(CreateAuthListEntry(player));
        Cupboard.SendNetworkUpdate();
      }

      public override void Authorize(IEnumerable<BasePlayer> players)
      {
        Cupboard.authorizedPlayers.AddRange(players.Select(player => CreateAuthListEntry(player)));
        Cupboard.SendNetworkUpdate();
      }

      public override void Deauthorize(BasePlayer player)
      {
        Cupboard.authorizedPlayers.RemoveAll(entry => entry.userid == player.userID);
        Cupboard.SendNetworkUpdate();
      }

      public override void Deauthorize(IEnumerable<BasePlayer> players)
      {
        Cupboard.authorizedPlayers.RemoveAll(entry => players.Any(player => player.userID == entry.userid));
        Cupboard.SendNetworkUpdate();
      }

      public override void DeauthorizeAll()
      {
        Cupboard.authorizedPlayers.Clear();
        Cupboard.SendNetworkUpdate();
      }

      public override BasePlayer[] GetAuthorizedPlayers()
      {
        return Cupboard.authorizedPlayers.Select(entry => BasePlayer.FindByID(entry.userid)).ToArray();
      }

      public override void SetAuthorizedPlayers(IEnumerable<BasePlayer> players)
      {
        Cupboard.authorizedPlayers.Clear();
        Cupboard.authorizedPlayers.AddRange(players.Select(player => CreateAuthListEntry(player)));
        Cupboard.SendNetworkUpdate();
      }

      public override bool IsAuthorized(BasePlayer player)
      {
        return Cupboard.IsAuthed(player);
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    class AddEntityToGroup : Interaction
    {
      public AddEntityToGroup(BasePlayer player, AuthGroup group)
        : base(player, group)
      {
      }

      public override void Handle(HitInfo hit)
      {
        if (Group.HasEntity(hit.HitEntity))
        {
          Instance.SendReply(Player, Messages.CannotAddEntityAlreadyEnrolledInGroup, Group.Name);
          return;
        }

        var entity = ManagedEntity.Create(hit.HitEntity);

        if (entity == null)
        {
          Instance.SendReply(Player, Messages.InvalidEntitySelected);
          return;
        }

        if (!entity.IsAuthorized(Player))
        {
          Instance.SendReply(Player, Messages.CannotAddEntityNotAuthorized);
          return;
        }

        Group.AddEntity(entity);
        Instance.SendReply(Player, Messages.EntityAdded, Group.Name, Group.MemberIds.Count);
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    abstract class Interaction
    {
      public BasePlayer Player { get; private set; }
      public AuthGroup Group { get; private set; }

      public Interaction(BasePlayer player, AuthGroup group)
      {
        Player = player;
        Group = group;
      }

      public abstract void Handle(HitInfo hit);
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    class RemoveEntityFromGroup : Interaction
    {
      public RemoveEntityFromGroup(BasePlayer player, AuthGroup group)
        : base(player, group)
      {
      }

      public override void Handle(HitInfo hit)
      {
        ManagedEntity entity = Group.GetManagedEntity(hit.HitEntity);

        if (entity == null)
        {
          Instance.SendReply(Player, Messages.CannotRemoveEntityNotEnrolledInGroup, Group.Name);
          return;
        }

        if (!entity.IsAuthorized(Player))
        {
          Instance.SendReply(Player, Messages.CannotRemoveEntityNotAuthorized);
          return;
        }

        Group.RemoveEntity(entity);
        Instance.SendReply(Player, Messages.EntityRemoved, Group.Name);
      }
    }
  }
}
