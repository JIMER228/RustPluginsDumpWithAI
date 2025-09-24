// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("F1Spawn", "Colon Blow", "1.0.7")]
    [Description("Allows use of F1 Item List Spawn")]
    class F1Spawn : CovalencePlugin
    {
        // added better check if spawn wait time is zero to not check for zero wait.
        // changed default wait time to 1 second

        #region Load

        const string permBL1 = "f1spawn.blacklist1";
        const string permBL2 = "f1spawn.blacklist2";
        const string permAL1 = "f1spawn.allowlist1";
        const string permAL2 = "f1spawn.allowlist2";
        const string permALL = "f1spawn.allowall";

        private List<BasePlayer> usedCommandList = new List<BasePlayer>();

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permBL1, this);
            permission.RegisterPermission(permBL2, this);
            permission.RegisterPermission(permAL1, this);
            permission.RegisterPermission(permAL2, this);
            permission.RegisterPermission(permALL, this);
        }

        bool isAllowed(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        #endregion

        #region Configuration

        private static PluginConfig config;

        private class PluginConfig
        {
            [JsonProperty(PropertyName = "Bypass checks if Admin ? ")] public bool AdminBypass { get; set; }
            [JsonProperty(PropertyName = "Bypass checks if Moderator ? ")] public bool ModBypass { get; set; }
            [JsonProperty(PropertyName = "Enable Debug Log to show F1 Spawn usage ? ")] public bool EnableDebugLog { get; set; }
            [JsonProperty(PropertyName = "Disable the 1000 quantity button ? ")] public bool Disable1000Button { get; set; }
            [JsonProperty(PropertyName = "Disbale the 100 quantity button ? ")] public bool Disable100Button { get; set; }
            [JsonProperty(PropertyName = "Blacklist 1 Items : ")] public List<string> BlackListedItems1 { get; set; }
            [JsonProperty(PropertyName = "Blacklist 2 Items : ")] public List<string> BlackListedItems2 { get; set; }
            [JsonProperty(PropertyName = "Allowed list 1 Items : ")] public List<string> AllowListItems1 { get; set; }
            [JsonProperty(PropertyName = "Allowed list 2 Items : ")] public List<string> AllowListItems2 { get; set; }
            [JsonProperty(PropertyName = "Delay Time between F1 spawn command usage : ")] public float WaitTime { get; set; }

            public static PluginConfig DefaultConfig() => new PluginConfig()
            {
                WaitTime = 1f,
                AdminBypass = true,
                ModBypass = true,
                EnableDebugLog = true,
                Disable1000Button = false,
                Disable100Button = false,
                BlackListedItems1 = new List<string>()
                    {
                        "Satchel Charge",
                        "Timed Explosive Charge"
                    },
                BlackListedItems2 = new List<string>()
                    {
                        "Beancan Grenade",
                        "F1 Grenade"
                    },
                AllowListItems1 = new List<string>()
                    {
                        "Hammer",
                        "Building Plan"
                    },
                AllowListItems2 = new List<string>()
                    {
                        "Wood",
                        "Stones"
                    }
            };
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("New configuration file created!!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            SaveConfig();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Give Command and Hook

        [Command("inventory.giveid")]
        void GiveIdCommand(IPlayer player, string command, string[] args)
        {
        }

        object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.cmd == null) return null;
            string command = arg.cmd.Name;
            if (command.Equals("giveid") || command.Equals("givearm"))
            {
                BasePlayer player = arg.Player();
                if (!player) return null;
                if (usedCommandList.Contains(player)) return false;
                if (isAllowed(player, permALL) || isAllowed(player, permAL1) || isAllowed(player, permAL2) || isAllowed(player, permBL1) || isAllowed(player, permBL2) || player.net?.connection?.authLevel > 0)
                {
                    Item item = ItemManager.CreateByItemID(arg.GetInt(0), 1, 0);
                    if (item == null) return false;
                    var allowspawn = false;
                    var adminSpawn = false;
                    if ((player.IsAdmin || player.IsDeveloper || player.net?.connection?.authLevel >= 2) && config.AdminBypass) { adminSpawn = true; allowspawn = true; }
                    else if (player.net?.connection?.authLevel == 1 && config.ModBypass) { adminSpawn = true; allowspawn = true; }
                    else if (isAllowed(player, permALL)) allowspawn = true;
                    else if (isAllowed(player, permAL1) && ((config.AllowListItems1.Contains(item.info.displayName.english) || config.AllowListItems1.Contains(item.info.shortname)))) allowspawn = true;
                    else if (isAllowed(player, permAL2) && ((config.AllowListItems2.Contains(item.info.displayName.english) || config.AllowListItems2.Contains(item.info.shortname)))) allowspawn = true;
                    else if (isAllowed(player, permBL1) && (!(config.BlackListedItems1.Contains(item.info.displayName.english) || config.BlackListedItems1.Contains(item.info.shortname)))) allowspawn = true;
                    else if (isAllowed(player, permBL2) && (!(config.BlackListedItems2.Contains(item.info.displayName.english) || config.BlackListedItems2.Contains(item.info.shortname)))) allowspawn = true;
                    else return false;

                    if (allowspawn)
                    {
                        item.amount = arg.GetInt(1, 1);
                        if (!adminSpawn && config.Disable1000Button && item.amount >= 1000) item.amount = 100;
                        if (!adminSpawn && config.Disable100Button && (item.amount < 1000 && item.amount >= 100)) item.amount = 1;
                        if (!player.inventory.GiveItem(item, null))
                        {
                            item.Remove(0f);
                            return false;
                        }
                        player.Command("note.inv", new object[] { item.info.itemid, item.amount });
                        if (config.EnableDebugLog) Debug.Log(string.Concat(new object[] { "[F1Spawn] giving ", player.displayName, " ", item.amount, " x ", item.info.displayName.english }));
                        if (!adminSpawn)
                        {
                            if (config.WaitTime > 0f)
                            {
                                usedCommandList.Add(player);
                                timer.Once(config.WaitTime, () => RemovePlayerFromList(player));
                            }
                        }
                        return false;
                    }
                    else return false;
                }
            }
            return null;
        }

        private void RemovePlayerFromList(BasePlayer player)
        {
            if (player == null) return;
            if (usedCommandList.Contains(player)) usedCommandList.Remove(player);
        }

        #endregion
    }
}