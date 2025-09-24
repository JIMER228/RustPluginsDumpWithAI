using System;
using System.Collections.Generic;
using System.Reflection;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("EasyAccess", "sami37", "0.0.1")]
    [Description("Access to arena")]
    public class EasyAccess : RustPlugin
    {
        [PluginReference] private Plugin Jail;

        private float Bank_x = 0;
        private float Bank_y = 0;
        private float Bank_z = 0;
        private float Bank2_x = 0;
        private float Bank2_y = 0;
        private float Bank2_z = 0;
        private float Bank3_x = 0;
        private float Bank3_y = 0;
        private float Bank3_z = 0;

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is BuildingBlock)
            {
                var block = (BuildingBlock)entity;
                if (permission.UserHasPermission(block.OwnerID.ToString(), "easyaccess.nostab"))
                    block.grounded = true;
            }
        }

        private void ReadFromConfig<T>(string key, ref T var)
        {
            if (Config[key] != null)
            {
                var = (T)Convert.ChangeType(Config[key], typeof(T));
            }
            Config[key] = var;
        }

        private void OnServerInitialized()
        {
            permission.RegisterPermission("easyaccess.canaccess", this);
            permission.RegisterPermission("easyaccess.cansetup", this);
            permission.RegisterPermission("easyaccess.nostab", this);
            permission.RegisterPermission("easyaccess.cantpbank", this);
            #region arena_config
            ReadFromConfig("Bank_x", ref Bank_x);
            ReadFromConfig("Bank_y", ref Bank_y);
            ReadFromConfig("Bank_z", ref Bank_z);
            ReadFromConfig("Bank2_x", ref Bank2_x);
            ReadFromConfig("Bank2_y", ref Bank2_y);
            ReadFromConfig("Bank2_z", ref Bank2_z);
            ReadFromConfig("Bank3_x", ref Bank3_x);
            ReadFromConfig("Bank3_y", ref Bank3_y);
            ReadFromConfig("Bank3_z", ref Bank3_z);
            #endregion
            SaveConfig();

            var messages = new Dictionary<string, string>
            {
                {
                    "NoAccess", "You can't access to arena."
                },
				{
					"NoPerm", "You don't have permission to do this."
				},
				{
					"NoStarted", "Arena is not start."
				},
				{
					"NothingFound", "You must look at a building."
				},
				{
					"Help", "/arena setup -- Active entranceA edit mode."
				},
				{
					"Help1", "/arena setup entranceA -- Active entranceA edit mode."
				},
				{
					"Help2", "/arena setup entranceB -- Active entranceB edit mode."
				},
				{
					"Help3", "/arena setup entranceFFA -- Active entranceFFA edit mode."
				}
            };
            lang.RegisterMessages(messages, this);
        }

        private object DoRay(Vector3 Pos, Vector3 Aim)
        {
            var hits = Physics.RaycastAll(Pos, Aim);
            float distance = 3f;
            object target = false;
            foreach (var hit in hits)
            {
                if (hit.distance < distance)
                {
                    distance = hit.distance;
                    target = hit.GetEntity();
                }
            }
            return target;
        }

        private string GetMessage(string name, string sid = null)
        {
            return lang.GetMessage(name, this, sid);
        }

        #region messages
        [ChatCommand("arena")]
        private void AccessCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 0)
            {
                if (permission.UserHasPermission(player.UserIDString, "easyaccess.cansetup"))
                {
                    if (args[0] == "setup")
                    {
                        if (args.Length == 3)
                        {
                            if (args[1] == "spawn")
                            {
                                var position = player.transform.position + player.eyes.BodyForward() + new Vector3(0, 1, 0);
                                if (args[2] == "bank")
                                {
                                    Bank_x = position.x;
                                    Bank_y = position.y;
                                    Bank_z = position.z;
                                    Config["Bank_x"] = Bank_x;
                                    Config["Bank_y"] = Bank_y;
                                    Config["Bank_z"] = Bank_z;
                                    SendReply(player, string.Format("Spawn Bank position configured and saved ({0}|{1}|{2})", Config["Bank_x"], Config["Bank_y"], Config["Bank_z"]));
                                }
                                if (args[2] == "bank2")
                                {
                                    Bank2_x = position.x;
                                    Bank2_y = position.y;
                                    Bank2_z = position.z;
                                    Config["Bank2_x"] = Bank2_x;
                                    Config["Bank2_y"] = Bank2_y;
                                    Config["Bank2_z"] = Bank2_z;
                                    SendReply(player, string.Format("Spawn Bank position configured and saved ({0}|{1}|{2})", Config["Bank2_x"], Config["Bank2_y"], Config["Bank2_z"]));
                                }
                                if (args[2] == "bank3")
                                {
                                    Bank3_x = position.x;
                                    Bank3_y = position.y;
                                    Bank3_z = position.z;
                                    Config["Bank3_x"] = Bank3_x;
                                    Config["Bank3_y"] = Bank3_y;
                                    Config["Bank3_z"] = Bank3_z;
                                    SendReply(player, string.Format("Spawn Bank position configured and saved ({0}|{1}|{2})", Config["Bank3_x"], Config["Bank3_y"], Config["Bank3_z"]));
                                }
                                SaveConfig();
                            }
                            else
                            {
                                SendHelpText(player);
                            }
                        }
                        else
                        {
                            SendHelpText(player);
                        }
                    }
                }
                else
                {
                    SendReply(player, GetMessage("NoPerm"));
                }
            }
            else
            {
                SendHelpText(player);
            }
        }

        private void PrintMessage(BasePlayer player, string msgId, params object[] args)
        {
            PrintToChat(player, lang.GetMessage(msgId, this, player.UserIDString), args);
        }

        [ChatCommand("bank")]
        private void AccessBank(BasePlayer player, string command, string[] args)
        {
            if (Jail != null && Jail.IsLoaded)
            {
                var prisoner = (bool)Jail.CallHook("IsPrisoner", player);
                if (prisoner)
                {
                    SendReply(player, "You can't tp while you are in the jail");
                    return;
                }
            }
            if (permission.UserHasPermission(player.UserIDString, "easyaccess.cantpbank"))
            {
                if (!player.IsWounded())
                {
                    Teleport(player, new Vector3(Bank_x, Bank_y, Bank_z));
                    SendReply(player, "Teleported to bank");
                    SendReply(player, "You can also use /bank2 or /bank3");
                }
                else
                {
                    SendReply(player, "You can't use it now.");
                }
            }
            else
            {
                SendReply(player, "You don't have permission !");
            }
        }

        [ChatCommand("bank2")]
        private void AccessBank2(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "easyaccess.cantpbank"))
            {
                if (!player.IsWounded())
                {
                    Teleport(player, new Vector3(Bank2_x, Bank2_y, Bank2_z));
                    SendReply(player, "Teleported to bank 2");
                    SendReply(player, "You can also use /bank or /bank3");
                }
                else
                {
                    SendReply(player, "You can't use it now.");
                }
            }
            else
            {
                SendReply(player, "You don't have permission !");
            }
        }

        [ChatCommand("bank3")]
        private void AccessBank3(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "easyaccess.cantpbank"))
            {
                if (!player.IsWounded())
                {
                    Teleport(player, new Vector3(Bank3_x, Bank3_y, Bank3_z));
                    SendReply(player, "Teleported to bank 3");
                    SendReply(player, "You can also use /bank or /bank2");
                }
                else
                {
                    SendReply(player, "You can't use it now.");
                }
            }
            else
            {
                SendReply(player, "You don't have permission !");
            }
        }

        private void Teleport(BasePlayer player, Vector3 position)
        {
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading");
            StartSleeping(player);
            player.MovePosition(position);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            //player.UpdatePlayerCollider(true, false);
            player.SendNetworkUpdateImmediate();
            if (player.net?.connection == null) return;
            //TODO temporary for potential rust bug
            try { player.ClearEntityQueue(); } catch { }
            player.SendFullSnapshot();
        }

        private void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
                return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
        }

        private void SendHelpText(BasePlayer player)
        {
            PrintMessage(player, "Help");
            PrintMessage(player, "Help1");
            PrintMessage(player, "Help2");
            PrintMessage(player, "Help3");
        }


        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file!");
            Config.Clear();
            Config["entranceA"] = 0;
            Config["entranceB"] = 0;
            Config["entranceFFA"] = 0;
            SaveConfig();
        }

        #endregion
    }
}