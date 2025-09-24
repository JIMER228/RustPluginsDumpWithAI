// Requires: MonumentAddons

using Oxide.Core.Plugins;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("NPC Tag Rewards", "nuGGGz", "1.0.0")]
    [Description("Allows for depositing and rewarding players for NPC ID tags")]
    class NPCTagRewards : CovalencePlugin
    {
        [PluginReference]
        private Plugin ServerRewards;

        // Watching for DropBoxes and Outpost
        String dropboxPrefab = "dropbox.deployed.prefab";
        String outpostPrefab = "compound.prefab";

        // List of spawned dropboxes
        List<DropBox> spawnedBoxes = new List<DropBox>();

        // Rewards values
        Dictionary<string, int> Rewards = new Dictionary<string, int>();

        private void Init()
        {
            DefineRewards();
        }

        // Catches the event when a player submits an item to a box
        private object? OnItemSubmit(Item item, DropBox box, BasePlayer player)
        {
            // Check if we are in outpost
            MonumentInfo outpost = UnityEngine.Object.FindObjectsOfType<MonumentInfo>().Where(x => x.name.Contains(outpostPrefab)).FirstOrDefault();
            if (outpost == null)
            {
                return null;
            }

            // Null checks - dumb, but necessary
            if (box == null)
            {
                return null;
            }
            if (player == null)
            {
                return null;
            }

            // Getting the box entity
            var entity = box.GetEntity();
            string boxID = entity.net.ID.ToString();

            // Make sure the box is in the list
            if (spawnedBoxes.Any(x => x.net.ID.ToString() == boxID))
            {
                // Make sure the item is a tag
                if (item.info.shortname.Contains("idtag"))
                {
                    // Get the item info
                    int quantity = item.amount;
                    string shortName = item.info.shortname;
                    int reward = Rewards[shortName];
                    int totalRewards = reward * quantity;

                    // Player ID for rewards
                    string playerId = player.UserIDString;
                    
                    // Reward the player
                    if ((bool)(ServerRewards?.Call("AddPoints", playerId, totalRewards)))
                    {
                        // Play some sweet-ass sounds
                        Effect.server.Run("assets/prefabs/misc/casino/slotmachine/effects/payout_jackpot.prefab", player.transform.position);
                        // Notify the player
                        player.ChatMessage("You just received <color=#FEC601>" + totalRewards + "</color> rewards points!");
                        // Remove the item
                        item.Remove();
                    }
                    return null;
                }
                player.ChatMessage("This item cannot be deposited for RP");
                return false;
            }
            return null;
        }

        // Catches the event when a dropbox is spawned
        private void OnMonumentEntitySpawned(BaseEntity entity, MonoBehaviour monument, Guid guid)
        {
            if (entity.name.Contains(dropboxPrefab))
            {
                Puts("here");
                string monumentName = monument.name;
                if (monumentName.Contains(outpostPrefab))
                {
                    // Get the DropBox
                    DropBox dropBox = entity.GetComponent<DropBox>();
                    Puts(dropBox.name + "!!!!!");

                    // Add the dropbox to the spawned list
                    spawnedBoxes.Add(dropBox);
                }
            }
        }

        private void DefineRewards()
        {
            // add the rewards to the dictionary
            Rewards.Add("greenidtag", 5);
            Rewards.Add("yellowidtag", 10);
            Rewards.Add("blueidtag", 15);
            Rewards.Add("redidtag", 20);
        }
    }
}
