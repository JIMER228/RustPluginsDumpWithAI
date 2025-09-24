using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Supply Limiter", "RustPluginsSliv", "1.0.4")]
    [Description("https://discord.gg/ExNctg3fJP")]
    public class SupplyLimiter : RustPlugin
    {
        #region Vars

        private const string itemShortname = "supply.signal";

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            LoadData();

            foreach (var value in config.permissions)
            {
                if (permission.PermissionExists(value.permission) == false)
                {
                    permission.RegisterPermission(value.permission, this);
                }
            }
        }

        private void Unload()
        {
            SaveData();
        }

        private void OnExplosiveThrown(BasePlayer player, BaseEntity entity, ThrownWeapon item)
        {
            CheckSupply(player, entity, item);
        }

        private void OnExplosiveDropped(BasePlayer player, BaseEntity entity, ThrownWeapon item)
        {
            CheckSupply(player, entity, item);
        }

        #endregion

        #region Core

        private void CheckSupply(BasePlayer player, BaseEntity entity, ThrownWeapon thrown)
        {
            var item = thrown.GetItem();
            if (item == null)
            {
                return;
            }

            if (item.info.shortname != itemShortname)
            {
                return;
            }

            if (item.skin != 0 || entity.skinID != 0 || thrown.skinID != 0)
            {
                return;
            }

            var def = GetPermission(player.UserIDString, config.permissions);
            if (def == null)
            {
                entity.Kill();
                GiveRefund(player, item);
                SendMessage(player, Message.Permission);
                return;
            }
            
            var info = Data.Get(player.UserIDString);

            if (def.cooldown > 0)
            {
                var leftFriends = LeftFriends(player, def.cooldown);
                if (leftFriends > 0)
                {
                    entity.Kill();
                    GiveRefund(player, item);
                    var leftTime = leftFriends.ToString("0.0");
                    SendMessage(player, Message.Cooldown, new Dictionary<string, object> {{"{time.seconds}", leftTime}});
                    return;
                }
               
                if (info.nextUseTime != new DateTime())
                {
                    var left = (info.nextUseTime - DateTime.UtcNow).TotalSeconds;
                    if (left > 0)
                    {
                        entity.Kill();
                        GiveRefund(player, item);
                        var leftTime = left.ToString("0.0");
                        SendMessage(player, Message.Cooldown, new Dictionary<string, object>{{"{time.seconds}", leftTime}});
                        return;
                    }
                    else
                    {
                        info.nextUseTime = new DateTime();
                    }
                }
           
                if (def.triggerAmount > 0 && info.count >= def.triggerAmount)
                {
                    entity.Kill();
                    GiveRefund(player, item);
                    info.count = 0;
                    info.nextUseTime = DateTime.UtcNow.AddSeconds(def.cooldown);
                    SendMessage(player, Message.Cooldown, new Dictionary<string, object>{{"{time.seconds}", def.cooldown}});
                    return;
                }
            }
            
            info.lastUse = DateTime.UtcNow;
            info.count++;
        }

        private void GiveRefund(BasePlayer player, Item original)
        {
            var item = ItemManager.CreateByName(itemShortname);
            if (item != null)
            {
                item.name = original.name;
                item.skin = original.skin;
                player.GiveItem(item);
            }
        }

        private double LeftFriends(BasePlayer player, int cooldown)
        {
            var info = (DataEntry) null;
            var left = (double) 0;

            if (config.checkClan == true)
            {
                var tag = GetPlayerClan(player);
                if (string.IsNullOrEmpty(tag) == false)
                {
                    info = Data.Get(tag);
                    left = (info.nextUseTime - DateTime.UtcNow).TotalSeconds;
                    if (left > 0)
                    {
                        return left;
                    }
                }
            }

            if (config.checkFriends == true)
            {
                var friends = GetFriends(player.UserIDString);
                if (friends.Length != 0)
                {
                    foreach (var friend in friends)
                    {
                        info = Data.Get(friend);
                        left = (info.nextUseTime - DateTime.UtcNow).TotalSeconds;
                        if (left > 0)
                        {
                            return left;
                        }
                    }
                }
            }

            if (config.checkTeam == true && player.currentTeam != 0)
            {
                var members = RelationshipManager.ServerInstance.FindTeam(player.currentTeam).members;
                foreach (var member in members)
                {
                    if (member == player.userID)
                    {
                        continue;
                    }

                    info = Data.Get(member.ToString());
                    left = (info.nextUseTime - DateTime.UtcNow).TotalSeconds;
                    if (left > 0)
                    {
                        return left;
                    }
                }
            }

            return left;
        }

        #endregion

        #region Configuration | 2.0.0

        private static ConfigData config = new ConfigData();

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Check friends")]
            public bool checkFriends = false;

            [JsonProperty(PropertyName = "Check team")]
            public bool checkTeam = false;

            [JsonProperty(PropertyName = "Check clan")]
            public bool checkClan = false;

            [JsonProperty(PropertyName = "Permissions")]
            public PermissionEntry[] permissions =
            {
                new PermissionEntry
                {
                    permission = "supplylimiter.default",
                    cooldown = 3000,
                    priority = 1,
                },
                new PermissionEntry
                {
                    permission = "supplylimiter.vip",
                    cooldown = 300,
                    priority = 2,
                },
                new PermissionEntry
                {
                    permission = "supplylimiter.god",
                    cooldown = 60,
                    priority = 3,
                    triggerAmount = 3
                },
            };
        }

        private class PermissionEntry
        {
            [JsonProperty(PropertyName = "Permission")]
            public string permission = "example";

            [JsonProperty(PropertyName = "Priority")]
            public int priority = 1;

            [JsonProperty(PropertyName = "Cooldown")]
            public int cooldown = 300;

            [JsonProperty(PropertyName = "Amount to start cooldown")]
            public int triggerAmount = 1;
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                }
                
                LoadDefaultConfig();
                return;
            }

            ValidateConfig();
            SaveConfig();
        }

        private void ValidateConfig()
        {
            if (Interface.Oxide.CallHook("Debug_UseDefaultValues") != null)
            {
                PrintWarning("Using default configuration in debug mode");
                config = new ConfigData();
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigData();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Language | 2.0.0
        
        private Dictionary<object, string> langMessages = new Dictionary<object, string>
        {
            {Message.Permission, "<color=#ff0000>You don't have permission to use that!</color>"},
            {Message.Cooldown, "Cooldown for <color=#00ffff>{time.seconds}</color> seconds!"},
        };
        
        private enum Message
        {
            Permission,
            Cooldown,
        }
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(langMessages.ToDictionary(x => x.Key.ToString(), y => y.Value), this);
        }

        private string GetMessage(Message key, Dictionary<string, object> args = null, string playerID = null)
        {
            var message = lang.GetMessage(key.ToString(), this, playerID);

            if (args != null)
            {
                foreach (var pair in args)
                {
                    var s0 = "{" + pair.Key + "}";
                    var s1 = pair.Key;
                    var s2 = pair.Value != null ? pair.Value.ToString() : "null";
                    message = message.Replace(s0, s2, StringComparison.InvariantCultureIgnoreCase);
                    message = message.Replace(s1, s2, StringComparison.InvariantCultureIgnoreCase);
                }
            }

            return message;
        }

        private void SendMessage(object receiver, string message)
        {
            if (receiver == null)
            {
                Puts(message);
                return;
            }
            
            var console = receiver as ConsoleSystem.Arg;
            if (console != null)
            {
                SendReply(console, message);
                return;
            }
            
            var player = receiver as BasePlayer;
            if (player != null)
            {
                player.ChatMessage(message);
                return;
            }
        }

        private void SendMessage(object receiver, Message key, Dictionary<string, object> args = null)
        {
            var userID = (receiver as BasePlayer)?.UserIDString;
            var message = GetMessage(key, args, userID);
            SendMessage(receiver, message);
        }

        #endregion
        
        #region Data | 2.0.0
        
        private const string filename = "data";
        private bool corruptedData;
        private class DataEntry
        {
            public DateTime lastUse;
            public DateTime nextUseTime;
            public int count;
        }
       
        private static PluginData Data = new PluginData();
        private class PluginData
        {
            public Dictionary<string, DataEntry> cache = new Dictionary<string, DataEntry>();
            public Dictionary<string, DataEntry> info = new Dictionary<string, DataEntry>();

            public DataEntry Get(string key)
            {
                var value = (DataEntry) null;
                if (cache.TryGetValue(key, out value) == true)
                {
                    return value;
                }

                if (info.TryGetValue(key, out value) == false)
                {
                    value = new DataEntry();
                    info.Add(key, value);
                }
               
                cache.Add(key, value);
                return value;
            }

            public DateTime creationTime = SaveRestore.SaveCreatedTime;
            public bool needWipe => SaveRestore.SaveCreatedTime != creationTime;
        }

        private void LoadData(string keyName = filename)
        {
            try
            {
                Data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>($"{Name}/{keyName}");
                if (Data.needWipe)
                {
                    PrintWarning($"Data was wiped by auto-wiping function (Old: {Data.creationTime}, New: {SaveRestore.SaveCreatedTime})");
                    SaveData(filename + "_old");
                    Data = new PluginData();
                    SaveData();
                }
                
                timer.Every(Core.Random.Range(500, 700f), () => SaveData());
            }
            catch (Exception e)
            {
                corruptedData = true;
                Data = new PluginData();
                
                timer.Every(30f, () =>
                {
                    PrintError($"!!! CRITICAL DATA ERROR !!!\n * Data was not loaded!\n * Data auto-save was disabled!\n * Error: {e.Message}");
                });
                
                LogToFile("errors", $"\n\nError: {e.Message}\n\nTrace: {e.StackTrace}\n\n", this);
            }
        }

        private void SaveData(string keyName = filename)
        {
            if (corruptedData == false && Data != null)
            {
                Data.cache.Clear();
                Interface.Oxide.DataFileSystem.WriteObject($"{Name}/{keyName}", Data);
            }
        }

        #endregion

        #region Permissions Support

        private Dictionary<string, PermissionEntry> cachePermission = new Dictionary<string, PermissionEntry>();

        private PermissionEntry GetPermission(string playerID, PermissionEntry[] permissions)
        {
            var value = (PermissionEntry) null;
            if (cachePermission.TryGetValue(playerID, out value) == true)
            {
                return value;
            }
            
            var idString = playerID.ToString();
            var num = -1;

            foreach (var entry in permissions)
            {
                if (permission.UserHasPermission(idString, entry.permission) && entry.priority > num)
                {
                    num = entry.priority;
                    value = entry;
                }
            }

            if (value != null)
            {
                cachePermission.Add(playerID, value);
            }
            
            return value;
        }

        #endregion

        #region Friends Support

        [PluginReference] private Plugin Friends, RustIOFriendListAPI;

        private string[] GetFriends(string playerID)
        {
            var flag1 = Friends?.Call<string[]>("GetFriends", playerID) ?? new string[] { };
            var flag2 = RustIOFriendListAPI?.Call<string[]>("GetFriends", playerID) ?? new string[] { };
            return flag1.Length > 0 ? flag1 : flag2;
        }

        #endregion

        #region Clans Support

        [PluginReference] private Plugin Clans;

        private string GetPlayerClan(BasePlayer player)
        {
            return Clans?.Call<string>("GetClanOf", player.userID);
        }

        #endregion
    }
}
