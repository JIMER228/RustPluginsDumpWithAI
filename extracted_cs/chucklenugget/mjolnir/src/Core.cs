namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;

  [Info("Mjolnir", "chucklenugget", "0.1.0")]
  public partial class Mjolnir : RustPlugin
  {
    const string PERM_MJOLNIR = "mjolnir.wield";
    const uint SKIN_ID = 833423263;

    HashSet<Item> Hammers;
    BuildingBlockDestroyer Destroyer;

    void Init()
    {
      permission.RegisterPermission(PERM_MJOLNIR, this);

      Hammers = new HashSet<Item>();
      Destroyer = new GameObject().AddComponent<BuildingBlockDestroyer>();
      Destroyer.Init(this);
    }

    void Unload()
    {
      foreach (Item hammer in Hammers)
        hammer.Remove();

      if (Destroyer != null)
        UnityEngine.Object.DestroyImmediate(Destroyer);
    }

    [ChatCommand("mjolnir")]
    void OnMjolnirCommand(BasePlayer player, string command, string[] args)
    {
      if (!IsPlayerWorthy(player))
      {
        SendReply(player, "You are not worthy to summon Mjolnir!");
        return;
      }

      foreach (Item item in player.inventory.AllItems())
      {
        if (Hammers.Contains(item))
        {
          SendReply(player, "You have sent Mjolnir back whence it came!");
          item.Remove();
          return;
        }
      }

      ItemDefinition itemDef = ItemManager.FindItemDefinition("hammer");
      Item hammer = ItemManager.CreateByItemID(itemDef.itemid, 1, SKIN_ID);

      if (!player.inventory.GiveItem(hammer, player.inventory.containerBelt))
      {
        SendReply(player, "You don't have room in your inventory.");
        return;
      }

      Hammers.Add(hammer);
    }

    void OnHammerHit(BasePlayer player, HitInfo hit)
    {
      if (player == null || hit == null)
        return;

      var block = hit.HitEntity as BuildingBlock;
      if (block == null)
        return;

      var hammer = hit.Weapon.GetItem();
      if (!Hammers.Contains(hammer))
        return;

      var building = block.GetBuilding();

      if (building == null)
      {
        SendReply(player, "Couldn't determine building!");
        return;
      }

      BuildingBlock[] blocks = building.buildingBlocks.ToArray();

      SendReply(player, "Smiting {0} entities!", blocks.Length);
      Puts("{0} ({1}) smited {2} entities from building {3}", player.displayName, player.UserIDString, blocks.Length, building.ID);

      Destroyer.Destroy(blocks);
    }

    object OnItemPickup(Item item, BasePlayer player)
    {
      if (Hammers.Contains(item) && !IsPlayerWorthy(player))
      {
        SendReply(player, "You are not worthy to wield Mjolnir!");
        return false;
      }

      return null;
    }

    bool IsPlayerWorthy(BasePlayer player)
    {
      return permission.UserHasPermission(player.UserIDString, PERM_MJOLNIR);
    }
  }
}
