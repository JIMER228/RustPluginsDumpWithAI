using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("MagicKits", "opaque.poppy + herbs.acab", "1.2.0")]
    [Description("Gives customizable starter kits per permission group on player respawn")]
    public class MagicKits : CovalencePlugin
    {
        private Dictionary<string, Dictionary<string, int>> kitsByPermission;

        private const string permissionPrefix = "starterkit.";

        protected override void LoadDefaultConfig()
        {
            Config["StarterKits", "default"] = new Dictionary<string, object>()
            {
                { "stones", 100 },
                { "wood", 200 },
                { "spear", 1 },
                { "cloth", 50 }
            };

            Config["StarterKits", "1"] = new Dictionary<string, object>()
            {
                { "stones", 150 },
                { "wood", 300 },
                { "bow.hunting", 1 },
                { "cloth", 75 }
            };

            Config["StarterKits", "2"] = new Dictionary<string, object>()
            {
                { "stones", 200 },
                { "wood", 400 },
                { "spear", 2 },
                { "cloth", 100 },
                { "arrow.wooden", 50 }
            };

            Config["StarterKits", "admin"] = new Dictionary<string, object>()
            {
                { "stones", 500 },
                { "wood", 1000 },
                { "gun.pistol", 1 },
                { "ammo.pistol", 100 },
                { "cloth", 200 }
            };

            SaveConfig();
        }

        private void LoadConfigKits()
        {
            kitsByPermission = new Dictionary<string, Dictionary<string, int>>();
            var kitsSection = Config.GetSection("StarterKits");
            foreach (var kitKey in kitsSection.GetKeys())
            {
                var kitDict = new Dictionary<string, int>();
                var rawDict = kitsSection.Get<Dictionary<string, object>>(kitKey);
                foreach (var pair in rawDict)
                {
                    kitDict[pair.Key] = Convert.ToInt32(pair.Value);
                }
                kitsByPermission[kitKey] = kitDict;
            }
        }

        private void OnServerInitialized()
        {
            LoadConfigKits();
            PrintWarning("MagicKits loaded and starter kits configured.");
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;

            RemoveDefaultItems(player);

            string kitToGive = GetPlayerKit(player);

            if (kitsByPermission.TryGetValue(kitToGive, out var kitItems))
            {
                GiveStarterKit(player, kitItems);
            }
            else
            {
                PrintWarning($"MagicKits: No kit found for '{kitToGive}', giving default kit.");
                if (kitsByPermission.TryGetValue("default", out var defaultKit))
                {
                    GiveStarterKit(player, defaultKit);
                }
            }
        }

        private string GetPlayerKit(BasePlayer player)
        {
            // Check for permissions in order of priority
            if (player.IPlayer.HasPermission(permissionPrefix + "admin"))
                return "admin";

            if (player.IPlayer.HasPermission(permissionPrefix + "2"))
                return "2";

            if (player.IPlayer.HasPermission(permissionPrefix + "1"))
                return "1";

            // fallback
            return "default";
        }

        private void RemoveDefaultItems(BasePlayer player)
        {
            var inv = player.inventory;
            if (inv == null) return;

            RemoveItemFromInventory(inv, "rock");
            RemoveItemFromInventory(inv, "torch");
        }

        private void RemoveItemFromInventory(PlayerInventory inv, string shortName)
        {
            var itemsToRemove = new List<Item>();
            foreach (var item in inv.containerMain.itemList)
            {
                if (item.info.shortname == shortName)
                {
                    itemsToRemove.Add(item);
                }
            }

            foreach (var item in itemsToRemove)
            {
                item.Remove();
            }
        }

        private void GiveStarterKit(BasePlayer player, Dictionary<string, int> starterKitItems)
        {
            foreach (var kitItem in starterKitItems)
            {
                var itemDef = ItemManager.FindItemDefinition(kitItem.Key);
                if (itemDef != null)
                {
                    player.GiveItem(ItemManager.CreateByItemID(itemDef.itemid, kitItem.Value));
                }
                else
                {
                    PrintWarning($"MagicKits: Item '{kitItem.Key}' not found.");
                }
            }
        }
    }
}
