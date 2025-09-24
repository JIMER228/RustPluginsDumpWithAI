using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("LuckyBox", "herbs.acab", "1.0.2")]
    [Description("A plugin that spawns a lucky box in a small wooden box, rewards the finder.")]
    public class LuckyBox : RustPlugin
    {
        private const int LuckyBoxItemId = -1002156085;
        private const string NoteItem = "note";
        private const string SmallWoodenBoxPrefab = "assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab";
        private const ulong BoxSkinId = 2000024196;
        private const string FireworkSoundEffect = "assets/prefabs/npc/patrol helicopter/effects/rocket_fire.prefab";
        private BaseEntity luckyBox;
        private System.Random random = new System.Random();

        private string predefinedKey;
        private bool boxFound;
        private Vector3 boxPosition;

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            Config["PredefinedKey"] = "This is a secret key";
            Config["BoxFound"] = false;
            Config["BoxPosition"] = null;
        }

        private void OnServerInitialized()
        {
            InitPlugin();
        }

        private void InitPlugin()
        {
            predefinedKey = Config["PredefinedKey"]?.ToString() ?? "This is a secret key";
            boxFound = Config["BoxFound"] != null && Convert.ToBoolean(Config["BoxFound"]);

            if (Config["BoxPosition"] != null)
            {
                boxPosition = JsonConvert.DeserializeObject<Vector3>(Config["BoxPosition"].ToString());
            }

            if (!boxFound)
            {
                if (boxPosition != Vector3.zero)
                {
                    SpawnLuckyBoxAtPosition(boxPosition);
                }
                else
                {
                    SpawnLuckyBox();
                }
            }

            // Register the chat commands
            AddCovalenceCommand("box", "BoxStatusCommand");
            AddCovalenceCommand("locatebox", "AdminBoxStatusCommand");
            AddCovalenceCommand("newbox", "NewAdminBoxCommand");
            AddCovalenceCommand("tpbox", "TeleportToBoxCommand");
        }

        private void Unload()
        {
            SaveConfig();
            SaveBoxPosition();
            luckyBox?.Kill();
        }

        private void SaveBoxPosition()
        {
            if (luckyBox != null)
            {
                Config["BoxPosition"] = JsonConvert.SerializeObject(luckyBox.transform.position);
                SaveConfig();
            }
        }

        private void SpawnLuckyBox()
        {
            luckyBox?.Kill();

            boxPosition = GetValidPositionOutsideMonuments();
            if (boxPosition != Vector3.zero)
            {
                SpawnLuckyBoxAtPosition(boxPosition);
            }
            else
            {
                PrintError("Failed to find a valid position for the lucky box.");
            }
        }

        private void SpawnLuckyBoxAtPosition(Vector3 position)
        {
            try
            {
                luckyBox = GameManager.server.CreateEntity(SmallWoodenBoxPrefab, position, Quaternion.identity, true);
                if (luckyBox == null)
                {
                    PrintError("Failed to create small wooden box entity.");
                    return;
                }

                luckyBox.skinID = BoxSkinId;
                luckyBox.Spawn();

                var storageContainer = luckyBox.GetComponent<StorageContainer>();
                if (storageContainer?.inventory == null)
                {
                    PrintError("Small wooden box does not have an inventory.");
                    return;
                }

                // Make sure the item exists in the game
                ItemDefinition boxItemDef = ItemManager.FindItemDefinition(LuckyBoxItemId);
                if (boxItemDef == null)
                {
                    PrintError("Failed to find the lucky box item definition.");
                    return;
                }

                Item luckyBoxItem = ItemManager.Create(boxItemDef, 1);
                if (luckyBoxItem == null)
                {
                    PrintError("Failed to create lucky box item.");
                    return;
                }

                if (!storageContainer.inventory.Insert(luckyBoxItem))
                {
                    PrintError("Failed to insert the lucky box item into the small wooden box.");
                    return;
                }

                if (luckyBox != null)
                {
                    PrintToChat("A lucky box has been hidden in a small wooden box on the map. Happy hunting!");
                    SaveBoxPosition();

                    // Call hook to notify other plugins
                    Interface.CallHook("OnLuckyBoxSpawned", position, predefinedKey);
                }
                else
                {
                    PrintError("Failed to assign lucky box entity.");
                }
            }
            catch (Exception ex)
            {
                PrintError($"Exception during SpawnLuckyBoxAtPosition: {ex.Message}");
            }
        }

        private Vector3 GetValidPositionOutsideMonuments()
        {
            List<MonumentInfo> monuments = TerrainMeta.Path.Monuments;
            if (monuments == null || monuments.Count == 0)
            {
                PrintError("No monuments found on the map.");
                return Vector3.zero;
            }

            for (int i = 0; i < 100; i++) // Try up to 100 times to find a valid position outside monuments
            {
                float x = UnityEngine.Random.Range(-3000f, 3000f);
                float z = UnityEngine.Random.Range(-3000f, 3000f);
                float y = TerrainMeta.HeightMap.GetHeight(new Vector3(x, 0, z));
                Vector3 potentialPosition = new Vector3(x, y, z);

                if (IsValidPosition(potentialPosition, monuments))
                {
                    Puts($"Found valid position at {potentialPosition}");
                    return potentialPosition;
                }
                else
                {
                    Puts($"Invalid position at {potentialPosition}, trying again...");
                }
            }

            return Vector3.zero; // Return zero vector if no valid position is found
        }

        private bool IsValidPosition(Vector3 position, List<MonumentInfo> monuments)
        {
            // Check for terrain and junkpiles
            if (Physics.Raycast(position + Vector3.up * 2, Vector3.down, out RaycastHit hit, 4f))
            {
                if (hit.collider.CompareTag("Terrain") || hit.collider.name.Contains("junkpile"))
                {
                    return false;
                }
            }

            // Check if position is in water
            if (TerrainMeta.HeightMap.GetHeight(position) < TerrainMeta.WaterMap.GetHeight(position))
            {
                return false;
            }

            // Check if position is inside any monument
            foreach (var monument in monuments)
            {
                if (IsPositionWithinMonument(position, monument))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsPositionWithinMonument(Vector3 position, MonumentInfo monument)
        {
            Vector3 localPosition = monument.transform.InverseTransformPoint(position);
            return monument.Bounds.Contains(localPosition);
        }

        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity == null || entity != luckyBox)
            {
                return;
            }

            PrintToChat($"The lucky box has been found by {player.displayName}!");
            RewardPlayer(player);
            PlayFireworkSound();
            luckyBox?.Kill();
            luckyBox = null;
            boxFound = true;
            Config["BoxFound"] = true;
            Config["BoxPosition"] = null;
            SaveConfig();

            // Call hook to notify other plugins
            Interface.CallHook("OnLuckyBoxFound", player, luckyBox.transform.position, predefinedKey);
        }

        private void RewardPlayer(BasePlayer player)
        {
            Item noteItem = ItemManager.CreateByName(NoteItem, 1);
            if (noteItem != null)
            {
                noteItem.text = predefinedKey;
                player.inventory.GiveItem(noteItem);
                PrintToChat(player, "You have been given a note with a secret key. Please open a ticket on Discord with this secret key to claim your prize!");
            }
            else
            {
                PrintError("Failed to create note item.");
            }
        }

        private void PlayFireworkSound()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                Effect.server.Run(FireworkSoundEffect, player.transform.position);
            }
        }

        // This method handles the /box command
        private void BoxStatusCommand(IPlayer player, string command, string[] args)
        {
            if (boxFound)
            {
                player.Reply("The lucky box has already been found.");
            }
            else
            {
                player.Reply("The lucky box is still hidden somewhere on the map. Keep looking!");
            }
        }

        // This method handles the /locatebox command for administrators
        private void AdminBoxStatusCommand(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                player.Reply("You do not have permission to use this command.");
                return;
            }

            if (luckyBox == null)
            {
                player.Reply("The lucky box has not been placed yet.");
                return;
            }

            var position = luckyBox.transform.position;
            var gridLocation = $"{position.x.ToString("F2")}, {position.y.ToString("F2")}, {position.z.ToString("F2")}";
            var mapGrid = GetGridLocation(position);
            player.Reply($"The lucky box is located at grid: {gridLocation} (Map Grid: {mapGrid})");
            Puts($"Admin {player.Name} checked the lucky box's location: {gridLocation} (Map Grid: {mapGrid})");
        }

        // This method handles the /newbox command for administrators to create a new lucky box
        private void NewAdminBoxCommand(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                player.Reply("You do not have permission to use this command.");
                return;
            }

            if (args.Length == 0)
            {
                player.Reply("You must provide a new secret key. Usage: /newbox <secret_key>");
                return;
            }

            string newSecretKey = args[0];

            // Remove the previous lucky box
            luckyBox?.Kill();
            luckyBox = null;

            // Update the secret key
            predefinedKey = newSecretKey;
            Config["PredefinedKey"] = newSecretKey;
            boxFound = false;
            Config["BoxFound"] = false;
            Config["BoxPosition"] = null;
            SaveConfig();

            // Create a new lucky box
            SpawnLuckyBox();

            player.Reply($"A new lucky box has been created with the new secret key: {newSecretKey}");
            Puts($"Admin {player.Name} created a new lucky box with the new secret key: {newSecretKey}");
        }

        // This method handles the /tpbox command for administrators to teleport to the lucky box
        private void TeleportToBoxCommand(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                player.Reply("You do not have permission to use this command.");
                return;
            }

            if (luckyBox == null)
            {
                player.Reply("The lucky box has not been placed yet.");
                return;
            }

            var position = luckyBox.transform.position;
            var basePlayer = player.Object as BasePlayer;
            if (basePlayer != null)
            {
                basePlayer.Teleport(position);
                player.Reply($"You have been teleported to the lucky box at: {position}");
                Puts($"Admin {player.Name} teleported to the lucky box at: {position}");
            }
        }

        private string GetGridLocation(Vector3 position)
        {
            char letter = (char)('A' + Mathf.FloorToInt((position.x + 3000) / 6000 * 26));
            int number = Mathf.FloorToInt((3000 - position.z) / 6000 * 26) + 1;
            return $"{letter}{number}";
        }
    }
}
