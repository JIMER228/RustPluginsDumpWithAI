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
