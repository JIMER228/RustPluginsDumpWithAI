// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using JetBrains.Annotations;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch;

namespace Oxide.Plugins
{
    [Info("BPUnlockerVip", "Vlad-00003", "1.4.5")]
    [Description("Unlock all blueprints to the players")]
    /*
     * Author info:
     *   E-mail: Vlad-00003@mail.ru
     *   Vk: vk.com/vlad_00003
     */
    internal class BPUnlockerVip : RustPlugin
    {
        #region Oxide hooks‌​‌‌‍﻿﻿

        private object CanCraft(PlayerBlueprints bps, ItemDefinition itemDef, int skinId)
        {
            var player = bps.GetComponent<BasePlayer>();
            if (!player)
                return null;
            var reply = 0;
            var hasPermission = permission.UserHasPermission(player.UserIDString, _config.NoWorkbench);
            return (skinId == 0 || bps.CheckSkinOwnership(skinId, player.userID)) &&
                   (hasPermission || player.currentCraftLevel >= itemDef.Blueprint.workbenchLevelRequired) &&
                   bps.HasUnlocked(itemDef);
        }
        void OnPlayerConnected(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(2, () => OnPlayerConnected(player));
                return;
            }
            SetNoWorkbench(player,  permission.UserHasPermission(player.UserIDString, _config.NoWorkbench));
            ServerMgr.Instance.StartCoroutine(UnlockOnline(player));
        }

        #region Permission handling‌​‌‌‍﻿﻿
        
        private void OnUserGroupAdded(string id, string groupName)
        {
            var permissions = permission.GetGroupPermissions(groupName);
            foreach (var s in permissions)
            {
                OnUserPermissionGranted(id,s);
            }
        }
        private void OnUserGroupRemoved(string id, string groupName)
        {
            var permissions = permission.GetGroupPermissions(groupName);
            foreach (var s in permissions)
            {
                OnUserPermissionRevoked(id,s);
            }
        }
        void OnGroupPermissionRevoked(string name, string permName)
        {
            var players = permission.GetUsersInGroup(name);

            if (!permName.Equals(_config.NoWorkbench, StringComparison.OrdinalIgnoreCase)) 
                return;

            foreach (var playerId in players)
                SetNoWorkbench(playerId.Split(' ')[0],false);
        }
        void OnUserPermissionRevoked(string id, string permName)
        {
            if (!permName.Equals(_config.NoWorkbench, StringComparison.OrdinalIgnoreCase)) 
                return;
            SetNoWorkbench(id,false);
        }

        void OnUserPermissionGranted(string id, string permName)
        {
            var players = new[] {id};
            if (permName.Equals(_config.NoWorkbench, StringComparison.OrdinalIgnoreCase))
            {
                SetNoWorkbench(id,true);
                return;
            }
            if (permName.Equals(_config.All, StringComparison.OrdinalIgnoreCase))
            {
                ServerMgr.Instance.StartCoroutine(Unlock(players, _available));
                return;
            }

            List<ItemDefinition> lst;
            if (!_config.Custom.TryGetValue(permName, out lst))
                return;
            ServerMgr.Instance.StartCoroutine(Unlock(players, lst));
        }
        void OnGroupPermissionGranted(string name, string permName)
        {
            var players = permission.GetUsersInGroup(name);
            for (int i = 0; i < players.Length; i++)
            {
                players[i] = players[i].Split(' ')[0];
            }
            
            if (permName.Equals(_config.NoWorkbench, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var playerId in players)
                    SetNoWorkbench(playerId,true);

                return;
            }

            List<ItemDefinition> lst;
            if (permName.Equals(_config.All, StringComparison.OrdinalIgnoreCase))
            {
                lst = _available;
            }
            else
            {
                if (!_config.Custom.TryGetValue(permName, out lst))
                    return;
            }

            ServerMgr.Instance.StartCoroutine(Unlock(players, lst));
        }

        #endregion
        
        #endregion

        #region Commands‌​‌‌‍﻿﻿

        private void CmdBp(IPlayer player, string cmd, string[] args)
        {
            if (args == null || args.Length < 2)
            {
                player.Message(GetMsg("Syntax", player.Id, _config.Command));
                return;
            }

            var targets = covalence.Players.FindPlayers(args[1]).ToArray();
            if (targets.Length == 0)
            {
                player.Message(GetMsg("NoPlayer", player.Id, args[1]));
                return;
            }

            if (targets.Length > 1)
            {
                player.Message(GetMsg("MultiplyUsers", player.Id, args[1],string.Join("\n\t",targets.Select(FormatIPlayer))));
                return;
            }

            var target = targets[0];
            var toUnlock = string.Join(" ", args.Skip(2));
            switch (args[0].ToLower())
            {
                default:
                    player.Message(GetMsg("Syntax", player.Id, _config.Command));
                    return;
                case "unlock":
                    if (args.Length < 3)
                    {
                        var msg = GetMsg("Syntax", player.Id, _config.Command) + "\n" + GetMsg("Syntax1", player.Id);
                        player.Message(msg);
                        return;
                    }

                    if (toUnlock.Equals("all",StringComparison.OrdinalIgnoreCase))
                    {
                        ServerMgr.Instance.StartCoroutine(Unlock(target, _available));
                        player.Message(GetMsg("UserUnlocked", player.Id, target.Name));
                        return;
                    }

                    if (_config.Custom.ContainsKey(toUnlock))
                    {
                        ServerMgr.Instance.StartCoroutine(Unlock(target, _config.Custom[toUnlock]));
                        player.Message(GetMsg("UserUnlockedSome", player.Id, target.Name,toUnlock));
                        return;
                    }

                    var def = _available.FirstOrDefault(p =>
                        p.displayName.english == toUnlock || p.shortname == toUnlock);
                    if (def == null)
                    {
                        player.Message(GetMsg("ItemNotFound", player.Id, toUnlock));
                        return;
                    }

                    ServerMgr.Instance.StartCoroutine(Unlock(target, new List<ItemDefinition>{def}));
                    player.Message(GetMsg("ItemUnlocked", player.Id, def.displayName.english, target.Name));
                    return;
                case "lock":
                    ServerMgr.Instance.StartCoroutine(Reset(target));
                    player.Message(GetMsg("UserLocked", player.Id, target.Name));
                    return;
            }
        }

        #endregion

        #region Helpers‌​‌‌‍﻿﻿

        private void SetNoWorkbench(string playerId, bool enabled)
        {
            SetNoWorkbench(BasePlayer.FindAwakeOrSleeping(playerId), enabled);
        }

        private void SetNoWorkbench(BasePlayer player, bool enabled)
        {
            if(player && player.IsConnected)
                player.ClientRPCPlayer(null, player, "craftMode", enabled ? 1 : 0);
        }

        private string FormatIPlayer(IPlayer player)
        {
            string res = $"{player.Name} ({player.Id})";
            if (player.IsConnected)
                res += " [Online]";
            return res;
        }
        private List<ItemDefinition> GetBpList(string playerId)
        {
            return permission.UserHasPermission(playerId, _config.All)
                ? _available.ToList()
                : _config.Custom.Where(p => permission.UserHasPermission(playerId.ToString(),p.Key)).SelectMany(x => x.Value).ToList();
        }

        #endregion

        #region Config‌​‌‌‍﻿﻿

        private class PluginConfig
        {
            [JsonIgnore]
            public readonly Dictionary<string, List<ItemDefinition>> Custom =
                new Dictionary<string, List<ItemDefinition>>(StringComparer.OrdinalIgnoreCase);

            [JsonProperty("Permission to automatically unlock ALL blueprints")]
            public string All;
            
            [JsonProperty("Permission to use command")]
            public string CommandPermission;
            
            [JsonProperty("Command to unlock/lock blueprints for the player")]
            public string Command;
            
            [JsonProperty("Permission to remove workbench requirements")]
            public string NoWorkbench;

            [JsonProperty("List of cutom permissions")]
            public Dictionary<string, List<string>> CustomPermissions;

            [JsonProperty("List of all available blueprints to unlock(editing does nothing)")]
            public Dictionary<string, List<string>> Available = new Dictionary<string, List<string>>();

            #region Default Config‌​‌‌‍﻿﻿

            public static PluginConfig DefaultConfig => new PluginConfig
            {
                All = "bpunlockervip.all",
                NoWorkbench = "bpunlockervip.noworkbench",
                Command = "bp",
                CommandPermission = "bpunlockervip.admin",
                CustomPermissions = new Dictionary<string, List<string>>
                {
                    ["bpunlockervip.sniper"] = new List<string>
                    {
                        "HV 5.56 Rifle Ammo",
                        "Bolt Action Rifle",
                        "Large Medkit",
                        "Coffee Can Helmet",
                        "Road Sign Jacket",
                        "Road Sign Kilt"
                    },
                    ["bpunlockervip.heavy"] = new List<string>
                    {
                        "Heavy Plate Helmet",
                        "Heavy Plate Jacket",
                        "Heavy Plate Pants",
                        "5.56 Rifle Ammo",
                        "Assault Rifle",
                        "Explosive 5.56 Rifle Ammo"
                    },
                    ["bpunlockervip.builder"] = new List<string>
                    {
                        "Auto Turret",
                        "Concrete Barricade",
                        "Metal Barricade",
                        "Sandbag Barricade",
                        "Bed",
                        "Locker",
                        "Mail Box",
                        "High External Stone Gate",
                        "High External Wooden Gate",
                        "High External Stone Wall",
                        "High External Wooden Wall"
                    }
                }
            };

            #endregion

            public void RegisterPermissions(Plugin plugin)
            {
                OxidePermissions.RegisterPermission(CommandPermission,plugin);
                OxidePermissions.RegisterPermission(All,plugin);
                OxidePermissions.RegisterPermission(NoWorkbench, plugin);
                foreach (var custom in CustomPermissions)
                    OxidePermissions.RegisterPermission(custom.Key,plugin);
            }
        }

        #endregion

        #region Vars‌​‌‌‍﻿﻿

        private PluginConfig _config;
        private List<ItemDefinition> _available;
        private static readonly Permission OxidePermissions = Interface.Oxide.GetLibrary<Permission>();

        #endregion

        #region Config Initialization‌​‌‌‍﻿﻿

        private bool FillAvailable()
        {
            if (_available != null)
                return false;
            var wasChanged = false;
            _available = new List<ItemDefinition>();
            foreach (var bp in ItemManager.GetBlueprints())
            {
                if (!bp.userCraftable || bp.defaultBlueprint)
                    continue;
                _available.Add(bp.targetItem);
                var category = bp.targetItem.category.ToString("F");
                var itemName = bp.targetItem.displayName.english;

                List<string> list;
                if (!_config.Available.TryGetValue(category, out list))
                {
                    list = new List<string>();
                    _config.Available[category] = list;
                }

                if (list.Contains(itemName))
                    continue;

                list.Add(itemName);
                wasChanged = true;
            }

            return wasChanged;
        }

        protected override void LoadDefaultConfig()
        {
            _config = PluginConfig.DefaultConfig;
            FillAvailable();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
            var definitions = ItemManager.GetItemDefinitions();
            foreach (var perm in _config.CustomPermissions)
            {
                foreach (var item in perm.Value)
                {
                    var definition = definitions.FirstOrDefault(p => p.displayName.english == item || p.shortname == item);
                    if (definition == null)
                    {
                        PrintWarning(GetMsg("NoDefFound", null, item, perm.Key));
                        continue;
                    }

                    List<ItemDefinition> lst;
                    if (!_config.Custom.TryGetValue(perm.Key, out lst))
                    {
                        lst = new List<ItemDefinition>();
                        _config.Custom[perm.Key] = lst;
                    }

                    lst.Add(definition);
                }
            }
           
            if (FillAvailable())
                SaveConfig();
            if(CheckConfig())
                SaveConfig();
            _config.RegisterPermissions(this);
        }

        private bool CheckConfig()
        {
            var res = false;
            var defaultConfig = PluginConfig.DefaultConfig;
            if(string.IsNullOrEmpty(_config.CommandPermission))
            {
                PrintWarning("Command permission was null! Setting to the default value...");
                _config.Command = defaultConfig.CommandPermission;
                res = true;
            }
            if(string.IsNullOrEmpty(_config.All))
            {
                PrintWarning("Unlock all permission was null! Setting to the default value...");
                _config.All = defaultConfig.All;
                res = true;
            }
            if(string.IsNullOrEmpty(_config.NoWorkbench))
            {
                PrintWarning("No workbench permission was null! Setting to the default value...");
                _config.NoWorkbench = defaultConfig.NoWorkbench;
                res = true;
            }

            return res;
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region Init‌​‌‌‍﻿﻿

        private void Init()
        {
            AddCovalenceCommand(_config.Command, "CmdBp", _config.CommandPermission);
        }
        
        private void OnServerInitialized()
        {
           ServerMgr.Instance.StartCoroutine(UpdateAllPlayers());
        }

        #endregion

        #region Localization‌​‌‌‍﻿﻿

        private string GetMsg(string langKey, object userId = null, params object[] args)
        {
            var msg = lang.GetMessage(langKey, this, userId?.ToString());
            return args.Length != 0 ? string.Format(msg, args) : msg;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UserUnlocked"] = "Unlocked all blueprints for player \"{0}\"",
                ["UserUnlockedSome"] = "Player \"{0}\" has unlocked blueprints pack \"{1}\"",
                ["UserLocked"] = "Blueprints for player \"{0}\" reset to default",
                ["Syntax"] =
                    "Wrong syntax! Use /{0} [unlock/lock] [player] [blueprint or custom permissions group or all]",
                ["Syntax1"] = "You forgot to specify what to unlock",
                ["NoPlayer"] = "Player {0} not found on the server!",
                ["NoDefFound"] = "Definition for item \"{0}\" (permission \"{1}\") not found! Check your config!",
                ["ItemNotFound"] = "Item \"{0}\" not found!",
                ["ItemUnlocked"] = "Blueprint for item \"{0}\" has being unlocked to player \"{1}\"!",
                ["MultiplyUsers"] = "Found multiply users by {0}:\n\t{1}!"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UserUnlocked"] = "Игроку {0} теперь доступны все чертежи",
                ["UserUnlockedSome"] = "Игроку \"{0}\" разблокирован набор чертежей \"{1}\"",
                ["UserLocked"] = "Чертежи игрока \"{0}\" сброшены до стандартных",
                ["Syntax"] =
                    "Неверный синтаксис! Используйте /{0} [unlock/lock] [игрок] [чертёж или набор чертежей или all]",
                ["Syntax1"] = "Вы забыли указать что разблокировать",
                ["NoPlayer"] = "Игрок \"{0}\" сейчас не находится на сервере!",
                ["NoDefFound"] = "Предмет \"{0}\" (привилегия \"{1}\") не найден! Проверьте файл конфигурации!",
                ["ItemNotFound"] = "Предмет \"{0}\" не найден!",
                ["ItemUnlocked"] = "Чертёж предмета \"{0}\" разблокирован для игрока \"{1}\"!",
                ["MultiplyUsers"] = "Найдено несколько игроков, подходящих под {0}:\n\t{1}!"
            }, this, "ru");
        }

        #endregion

        #region Functions‌​‌‌‍﻿﻿

        private IEnumerator UpdateAllPlayers(ulong playerid = 0)
        {
            foreach (var player in BasePlayer.allPlayerList.ToList())
            {
                if (player == null)
                    continue;
                yield return UnlockOnline(player);
                SetNoWorkbench(player, permission.UserHasPermission(player.UserIDString, _config.NoWorkbench));
            }
        }

        private IEnumerator Unlock(IPlayer iPlayer, List<ItemDefinition> list = null)
        {
            list = list ?? GetBpList(iPlayer.Id);
            var player = iPlayer.Object as BasePlayer ?? BasePlayer.FindAwakeOrSleeping(iPlayer.Id);
            if(!player)
            {
                yield return UnlockOffline(iPlayer.Id,list);
                yield break;
            }
            
            yield return UnlockOnline(player,list);
        }

        private IEnumerator Unlock(IEnumerable<string> players, List<ItemDefinition> list)
        {
            foreach (var playerId in players)
            {
                var player = BasePlayer.FindAwakeOrSleeping(playerId);
                if (player)
                    yield return UnlockOnline(player, list);
                else
                    yield return UnlockOffline(playerId, list);
                
            }
        }


        #region Reset functions‌​‌‌‍﻿﻿

        private IEnumerator Reset(IPlayer iPlayer)
        {
            var player = iPlayer.Object as BasePlayer ?? BasePlayer.FindAwakeOrSleeping(iPlayer.Id);
            if(!player)
            {
                yield return ResetOffline(iPlayer.Id);
            }
            else
            {
                player.blueprints.Reset();
                yield return UnlockOnline(player);
                //yield return null;
            }
            
        }

        private IEnumerator ResetOffline(string playerId)
        { 
            if(!permission.UserIdValid(playerId))
                yield break;
            var userId = ulong.Parse(playerId);
            var playerInfo = ServerMgr.Instance.persistance.GetPlayerInfo(userId);
            if (playerInfo.unlockedItems != null)
                playerInfo.unlockedItems.Clear();
            else
                playerInfo.unlockedItems = Pool.GetList<int>();
            ServerMgr.Instance.persistance.SetPlayerInfo(userId, playerInfo);
        }

        #endregion

        #region Unlock functions‌​‌‌‍﻿﻿

        private IEnumerator UnlockOffline(string playerId)
        {
            if(!permission.UserIdValid(playerId))
                yield break;
            yield return UnlockOffline(playerId, GetBpList(playerId));
        }
        private IEnumerator UnlockOffline(string playerId, [NotNull] List<ItemDefinition> list)
        {
            if(!permission.UserIdValid(playerId))
                yield break;
            var userId = ulong.Parse(playerId);
            var playerInfo = ServerMgr.Instance.persistance.GetPlayerInfo(userId);
            foreach (var def in list)
                if (!playerInfo.unlockedItems.Contains(def.itemid))
                    playerInfo.unlockedItems.Add(def.itemid);
            ServerMgr.Instance.persistance.SetPlayerInfo(userId, playerInfo);
            yield return null;

        }

        private IEnumerator UnlockOnline(BasePlayer player)
        {
            yield return UnlockOnline(player, GetBpList(player.UserIDString));
        }
        private IEnumerator UnlockOnline(BasePlayer player, [NotNull] List<ItemDefinition> list)
        {
            var playerInfo = player.PersistantPlayerInfo;
            foreach (var def in list)
                if (!playerInfo.unlockedItems.Contains(def.itemid))
                    playerInfo.unlockedItems.Add(def.itemid);
            player.PersistantPlayerInfo = playerInfo;
            player.SendNetworkUpdateImmediate(false);
            player.ClientRPCPlayer(null, player, "UnlockedBlueprint", list.LastOrDefault()?.itemid ?? 0);
            player.stats.Add("blueprint_studied", 1, (global::Stats)5);
            yield return null;
        }

        #endregion
        
        #endregion
    }
}
////////////////////////////////////////////////////////////////////
