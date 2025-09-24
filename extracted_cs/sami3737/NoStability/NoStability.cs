using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("NoStability", "sami37", "1.0.0")]
    [Description("Allow player to build no statibility")]
    public class NoStability : RustPlugin
    {
        private DynamicConfigFile DataFile;
        private StoredData storedPlayerData;

        #region Oxide Hooks

        class StoredData
        {
            public Dictionary<ulong, bool>  status = new Dictionary<ulong, bool>();
        }

        private void OnServerInitialized()
        {
            try
            {
                DataFile = Interface.Oxide.DataFileSystem.GetFile(Name);
                storedPlayerData = DataFile.ReadObject<StoredData>();
            }
            catch (Exception e)
            {
                DataFile = new DynamicConfigFile(Name);
                storedPlayerData = new StoredData();
                PrintWarning("Unable to load data creating new datafile.");
            }
            permission.RegisterPermission("NoStability.nostab", this);

            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["Status"] = "NoStability is set to {0}",
                ["NoPerm"] = "You don't have permission to do it."
            }, this);
        }

        void SaveData()
        {
            if (storedPlayerData != null)
            {
                DataFile.WriteObject(storedPlayerData);
                PrintWarning("Data saved.");
                return;
            }
            PrintWarning("Failed to save data.");
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is BuildingBlock)
            {
                var block = (BuildingBlock)entity;
                if (permission.UserHasPermission(block.OwnerID.ToString(), "NoStability.nostab") && storedPlayerData.status?[block.OwnerID] == true)
                    block.grounded = true;
            }
        }
        #endregion

        [ChatCommand("ns")]
        void cmdChatNoStab(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "NoStability.nostab"))
            {
                if (args != null && args.Length > 0)
                {
                    switch (args[0])
                    {
                        case "1":
                        case "on":
                        case "yes":
                            if (storedPlayerData.status.ContainsKey(player.userID))
                            {
                                storedPlayerData.status[player.userID] = true;
                            }
                            else
                            {
                                storedPlayerData.status = new Dictionary<ulong, bool> {{player.userID, true}};
                            }
                            break;
                        case "0":
                        case "off":
                        case "no":
                            if (storedPlayerData.status.ContainsKey(player.userID))
                            {
                                storedPlayerData.status[player.userID] = false;
                            }
                            else
                            {
                                storedPlayerData.status = new Dictionary<ulong, bool> {{player.userID, false}};
                            }
                            break;
                        default:
                            if (storedPlayerData.status.ContainsKey(player.userID))
                            {
                                if (storedPlayerData.status[player.userID] == false)
                                {
                                    storedPlayerData.status[player.userID] = true;
                                }
                                else
                                {
                                    storedPlayerData.status[player.userID] = false;
                                }
                            }
                            else
                            {
                                storedPlayerData.status = new Dictionary<ulong, bool> {{player.userID, true}};
                            }
                            break;
                    }
                    SendReply(player, string.Format(lang.GetMessage("Status", this, player.UserIDString), (storedPlayerData.status[player.userID] ? "<color=green>On</color>" : "<color=red>Off</color>")));
                    SaveData();
                }
            }
            else
            {
                SendReply(player, lang.GetMessage("NoPerm", this, player.UserIDString));
            }
        }
    }

}