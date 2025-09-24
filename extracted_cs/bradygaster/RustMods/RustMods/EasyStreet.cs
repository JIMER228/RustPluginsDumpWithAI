using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("EasyStreet", "Brady Gaster", "1.0.0")]
    [Description("Starts a new game with an inventory that sets you up for building a base and surviving the first night.")]
    class EasyStreet : CovalencePlugin
    {
        private const string LOG_FLAG = "<------------{ EASY STREET }------------>";

        private StoredData storedData;

        private void Init()
        {
            Puts("Initializing Easy Street...");
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("easystreet");
            Puts("Initialized Easy Street...");
        }

        private bool HasPlayerBeenWelcomed(BasePlayer player)
        {
            Puts("Entering HasPlayerBeenWelcomed");

            bool playerhasbeenwelcomed = false;
            if (storedData.Players.Contains(player.displayName))
            {
                Puts($"HasPlayerBeenWelcomed: Player {player.displayName} has already been welcomed.");
                playerhasbeenwelcomed = true;
            }
            else
            {
                Puts($"HasPlayerBeenWelcomed: Player {player.displayName} has not yet been welcomed.");

                Puts($"HasPlayerBeenWelcomed: Adding Player {player.displayName} to data file.");
                storedData.Players.Add(player.displayName);
                Interface.Oxide.DataFileSystem.WriteObject("easystreet", storedData);
                Puts($"HasPlayerBeenWelcomed: Added Player {player.displayName} to data file.");
            }

            Puts("Exiting HasPlayerBeenWelcomed");

            return playerhasbeenwelcomed;
        }

        private object OnDefaultItemsReceived(PlayerInventory inventory)
        {
            Puts("Entering OnDefaultItemsReceived");

            var player = inventory.baseEntity;

            var hasPlayerBeenWelcomed = HasPlayerBeenWelcomed(player);
            if (hasPlayerBeenWelcomed)
            {
                Puts(LOG_FLAG);
                Puts($"Welcome {player.displayName} back to Easy Street.");
                Puts(LOG_FLAG);
                return null;
            }

            Puts(LOG_FLAG);
            Puts($"Let's welcome {player.displayName} to Easy Street with a nice starting inventory.");
            Puts(LOG_FLAG);

            // clear this player's inventory
            Puts(LOG_FLAG);
            inventory.containerBelt.Clear();
            Puts($"Cleared {player.displayName}'s starting inventory.");
            Puts(LOG_FLAG);

            // resources
            foreach (var resource in new[] { "wood", "stones", "metal.fragments" })
            {
                for (int i = 0; i < 6; i++)
                {
                    AddItemToInventory(player, resource, 1000, player.inventory.containerMain);
                }
            }

            // main inventory
            AddItemToInventory(player, "ammo.rifle", 128, player.inventory.containerMain);
            AddItemToInventory(player, "cloth", 50, player.inventory.containerMain);
            AddItemToInventory(player, "lowgradefuel", 20, player.inventory.containerMain);
            AddItemToInventory(player, "pumpkin", 20, player.inventory.containerMain);

            // belt
            AddItemToInventory(player, "rifle.lr300", 1, player.inventory.containerBelt);
            AddItemToInventory(player, "knife.combat", 1, player.inventory.containerBelt);
            AddItemToInventory(player, "chainsaw", 1, player.inventory.containerBelt);
            AddItemToInventory(player, "jackhammer", 1, player.inventory.containerBelt);

            // clothing
            AddItemToInventory(player, "hat.miner", 1, player.inventory.containerWear);
            AddItemToInventory(player, "tshirt.long", 1, player.inventory.containerWear);
            AddItemToInventory(player, "roadsign.jacket", 1, player.inventory.containerWear);
            AddItemToInventory(player, "pants", 1, player.inventory.containerWear);
            AddItemToInventory(player, "shoes.boots", 1, player.inventory.containerWear);
            AddItemToInventory(player, "mask.bandana", 1, player.inventory.containerWear);

            // final confirmation
            Puts(LOG_FLAG);
            Puts($"{player.displayName} welcomed to Easy Street with a nice starting inventory.");
            Puts(LOG_FLAG);

            Puts("Exiting OnDefaultItemsReceived");

            return null;
        }

        private void AddItemToInventory(BasePlayer player, string itemName, int amount, ItemContainer container)
        {
            Puts($"Adding {itemName}");
            player.inventory.GiveItem(ItemManager.CreateByName(itemName, amount), container);
        }
    }

    public class StoredData
    {
        public HashSet<string> Players = new HashSet<string>();

        public StoredData()
        {
        }
    }
}