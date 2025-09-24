using Oxide.Core.Plugins;
using Oxide.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("MailBomber", "herbs.acab", "1.1.0")]
    [Description("Interacting with a mailbox deploys C4 and prevents the player from moving, jumping, or crouching.")]
    public class MailBomber : RustPlugin
    {
        private List<BaseEntity> createdEntities = new List<BaseEntity>();
        private List<string> noteMessages = new List<string>();
        private HashSet<BaseEntity> bombMailboxes = new HashSet<BaseEntity>();
        private Dictionary<ulong, Timer> frozenPlayers = new Dictionary<ulong, Timer>();

        private void Init()
        {
            LoadConfig();
        }

        protected override void LoadDefaultConfig()
        {
            Config["NoteMessages"] = new List<string>
            {
                "Unfortunately due to the crimes you have committed, we will be executing you on the spot.",
                "This is a second configurable message."
            };
            SaveConfig();
        }

        private void LoadConfig()
        {
            noteMessages = Config.Get<List<string>>("NoteMessages");
        }

        [ChatCommand("mailbomber")]
        private void MailBomberCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                SendReply(player, "You must be an admin to use this command.");
                return;
            }

            RaycastHit hit;
            if (!Physics.Raycast(player.eyes.HeadRay(), out hit))
            {
                SendReply(player, "No mailbox found.");
                return;
            }

            var mailbox = hit.GetEntity();
            if (mailbox == null || mailbox.ShortPrefabName != "mailbox.deployed")
            {
                SendReply(player, "You must look at a mailbox to use this command.");
                return;
            }

            if (bombMailboxes.Contains(mailbox))
            {
                SendReply(player, "This mailbox is already configured as a MailBomber.");
                return;
            }

            bombMailboxes.Add(mailbox);
            mailbox.gameObject.AddComponent<MailBomberMailbox>();
            createdEntities.Add(mailbox);
            AddNotesToMailbox(mailbox);
            SendReply(player, "MailBomber has been activated on this mailbox.");
        }

        [ChatCommand("clearmailbomber")]
        private void ClearMailBomberCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                SendReply(player, "You must be an admin to use this command.");
                return;
            }

            int clearedCount = 0;
            foreach (var mailbox in bombMailboxes)
            {
                if (mailbox != null && !mailbox.IsDestroyed)
                {
                    var component = mailbox.GetComponent<MailBomberMailbox>();
                    if (component != null)
                    {
                        GameObject.Destroy(component);
                    }
                    createdEntities.Remove(mailbox);
                    clearedCount++;
                }
            }

            bombMailboxes.Clear();
            SendReply(player, $"MailBomber has been cleared from {clearedCount} mailboxes.");
        }

        private void OnEntityKill(BaseNetworkable entity)
        {
            if (createdEntities.Contains(entity as BaseEntity))
            {
                createdEntities.Remove(entity as BaseEntity);
                bombMailboxes.Remove(entity as BaseEntity);
            }
        }

        private void AddNotesToMailbox(BaseEntity mailbox)
        {
            foreach (var message in noteMessages)
            {
                var noteItem = ItemManager.Create(ItemManager.FindItemDefinition("note"), 1);
                if (noteItem != null)
                {
                    noteItem.text = message;
                    noteItem.MarkDirty();

                    var storage = mailbox as StorageContainer;
                    if (storage != null)
                    {
                        storage.inventory.Insert(noteItem);
                    }
                }
            }
        }

        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity.ShortPrefabName == "mailbox.deployed" && bombMailboxes.Contains(entity))
            {
                var position = entity.transform.position;
                DeployC4(position);

                PreventPlayerMovement(player);
                Debug.Log("C4 deployed and player movement prevented.");
            }
        }

        private void DeployC4(Vector3 position)
        {
            var c4 = GameManager.server.CreateEntity("assets/prefabs/tools/c4/explosive.timed.deployed.prefab", position, Quaternion.identity);
            if (c4 != null)
            {
                c4.Spawn();
            }
        }

        private void PreventPlayerMovement(BasePlayer player)
        {
            player.PauseFlyHackDetection(10f); // Prevents false positives for fly hacking

            Vector3 originalPosition = player.transform.position;

            if (frozenPlayers.ContainsKey(player.userID))
            {
                frozenPlayers[player.userID].Destroy();
            }

            frozenPlayers[player.userID] = timer.Repeat(0.1f, 100, () =>
            {
                player.SetParent(null, true, true);
                player.MovePosition(originalPosition);
                player.ClientRPCPlayer(null, player, "ForcePositionTo", originalPosition);
                player.SendNetworkUpdateImmediate();
            });

            frozenPlayers[player.userID] = timer.Once(10f, () => AllowPlayerMovement(player));
        }

        private void AllowPlayerMovement(BasePlayer player)
        {
            if (frozenPlayers.ContainsKey(player.userID))
            {
                frozenPlayers[player.userID].Destroy();
                frozenPlayers.Remove(player.userID);
            }
        }

        private class MailBomberMailbox : MonoBehaviour
        {
            private BaseEntity entity;

            private void Awake()
            {
                entity = GetComponent<BaseEntity>();
            }
        }
    }
}
