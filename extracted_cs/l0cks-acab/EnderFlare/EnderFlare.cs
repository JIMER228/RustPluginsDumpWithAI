using System;
using UnityEngine;
using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("EnderFlare", "herbs.acab", "1.9.6")]
    [Description("Teleports the player to where the thrown flare lands.")]
    public class EnderFlare : RustPlugin
    {
        private const string FlarePrefab = "assets/prefabs/tools/flare/flare.deployed.prefab";
        private const ulong FlareSkinID = 3278321692;
        private const string FlareItemShortName = "flare";
        private const int FlareAmountPerCraft = 5;
        private const string CustomFlareID = "EnderFlare";
        private Dictionary<NetworkableId, BasePlayer> flareOwners = new Dictionary<NetworkableId, BasePlayer>();

        [ChatCommand("craftpearl")]
        private void CraftPearlCommand(BasePlayer player, string command, string[] args)
        {
            CraftEnderPearl(player);
        }

        [ChatCommand("givepearl")]
        private void GivePearlCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                player.ChatMessage("You do not have permission to use this command.");
                return;
            }

            GiveEnderPearl(player, 1);
            player.ChatMessage("You have received an Ender Flare!");
        }

        private void CraftEnderPearl(BasePlayer player)
        {
            int requiredMetalFragments = 50;
            int requiredLowGradeFuel = 5;
            int requiredHighQualityMetal = 1;

            if (player.inventory.GetAmount(ItemManager.FindItemDefinition("metal.fragments").itemid) < requiredMetalFragments ||
                player.inventory.GetAmount(ItemManager.FindItemDefinition("lowgradefuel").itemid) < requiredLowGradeFuel ||
                player.inventory.GetAmount(ItemManager.FindItemDefinition("metal.refined").itemid) < requiredHighQualityMetal)
            {
                player.ChatMessage("To craft Ender Flares you must have <color=#ff0000>50x Metal Frags, 5x Low Grade & 1 HQM!</color>");
                return;
            }

            // Remove the required resources
            player.inventory.Take(null, ItemManager.FindItemDefinition("metal.fragments").itemid, requiredMetalFragments);
            player.inventory.Take(null, ItemManager.FindItemDefinition("lowgradefuel").itemid, requiredLowGradeFuel);
            player.inventory.Take(null, ItemManager.FindItemDefinition("metal.refined").itemid, requiredHighQualityMetal);

            // Give the player the crafted Ender Flare
            GiveEnderPearl(player, FlareAmountPerCraft);
            player.ChatMessage("You have crafted Ender Flares!");
        }

        private void GiveEnderPearl(BasePlayer player, int amount)
        {
            Item item = ItemManager.CreateByName(FlareItemShortName, amount, FlareSkinID);
            if (item != null)
            {
                item.text = CustomFlareID; // Set a custom identifier
                player.GiveItem(item);
            }
        }

        private void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            if (entity == null || player == null)
                return;

            if (entity.ShortPrefabName == "flare.deployed" && IsCustomFlare(entity.skinID))
            {
                flareOwners[entity.net.ID] = player;

                player.ChatMessage("You have thrown a <color=#800080>Ender</color> Flare");

                timer.Once(1.5f, () =>
                {
                    if (entity != null && !entity.IsDestroyed && flareOwners.ContainsKey(entity.net.ID))
                    {
                        Vector3 flarePosition = entity.transform.position;
                        TeleportPlayer(flareOwners[entity.net.ID], flarePosition);
                        flareOwners.Remove(entity.net.ID);
                    }
                });
            }
        }

        private bool IsCustomFlare(ulong skinID)
        {
            return skinID == FlareSkinID;
        }

        private void TeleportPlayer(BasePlayer player, Vector3 position)
        {
            if (player == null || position == Vector3.zero)
                return;

            player.Teleport(position);
        }

        private void Init()
        {
            Puts("EnderFlare plugin loaded.");
        }

        private void Unload()
        {
            Puts("EnderFlare plugin unloaded.");
        }
    }
}
