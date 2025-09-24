// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Rust;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Global Offline Raid Protection", "Billy Joe", "1.0.0")]
    [Description("Makes explosions do less damage during a certain time range.")]
    public class GlobalOfflineRaidProtection : CovalencePlugin
    {
        #region Config
        static Configuration config;
        public class Configuration
        {
            [JsonProperty(PropertyName = "System Time to Deactivate Raiding")] public string raidDeactivateTime;
            [JsonProperty(PropertyName = "System Time to Reactivate Raiding")] public string raidActivateTime;
            [JsonProperty(PropertyName = "Amount to scale explosive damage (0-1) (0 - No Damage, 0.5 - Half Damage, 1 - Normal Damage)")] public float scaleDamage;
            [JsonProperty(PropertyName = "Send message when raiding in deactivated hours?")] public bool sendMessage;
            [JsonProperty(PropertyName = "Send message when connecting in deactivated hours?")] public bool sendMessageOnConnect;
            [JsonProperty(PropertyName = "Reset raid protection value to default on server wipe?")] public bool resetRaidOnWipe;
            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    raidDeactivateTime = "03:00 AM",
                    raidActivateTime = "09:00 AM",
                    scaleDamage = 0.5f,
                    sendMessage = true,
                    sendMessageOnConnect = true,
                    resetRaidOnWipe = true
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
                SaveConfig();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PrintWarning("Creating new config file.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Language
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ScaledDown"] = "<color=#d4af37>[Offline Raid Protection]</color> Explosions damage have been <color=green>reduced</color> by {0}%, until {1} {2}",
                ["ScaledUp"] = "<color=#d4af37>[Offline Raid Protection]</color> Explosions damage have been <color=green>reinstated</color> back to full damage, until {0} {1}",
                ["PlayerMessage"] = "<color=#d4af37>[Offline Raid Protection]</color> Damage has been <color=red>scaled down</color> {0}% as you are raiding in offlining hours!",
                ["PlayerMessageConnect"] = "<color=#d4af37>[Offline Raid Protection]</color> All Explosive Damage has been <color=red>scaled down</color> {0}% as you are raiding in offlining hours! Explosive damage will be reinstated at {1} {2}.",
                ["Timezone"] = "<color=#d4af37>[Offline Raid Protection]</color> Current Server Timezone: {0}",
                ["NoPermission"] = "<color=#d4af37>[Offline Raid Protection]</color> You do not have permission to use this command.",
            }, this);
        }
        #endregion

        #region Data
        public class Save
        {
            public bool raidingDeactivated = false;
        }

        private Save _save;
        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _save);
        #endregion

        #region Defines
        Timer AntiRaidTimer = null;
        string dateTime = string.Empty;
        const string adminPerm = "globalofflineraidprotection.admin";
        List<DamageType> damageTypes = new List<DamageType>()
        {
            DamageType.Arrow,
            DamageType.Blunt,
            DamageType.Bullet,
            DamageType.Explosion,
            DamageType.Heat,
            DamageType.Slash,
            DamageType.Stab
        };
        #endregion

        #region Hooks
        void Loaded()
        {
            _save = Interface.Oxide.DataFileSystem.ReadObject<Save>(Name);
            if (config.resetRaidOnWipe)
                Unsubscribe("OnNewSave");

            permission.RegisterPermission(adminPerm, this);
        }
        void OnServerInitialized(bool initial)
        {
            AntiRaidTimer = timer.Repeat(1, 0, () =>
            {
                dateTime = DateTime.Now.ToString("hh:mm tt");
                if (dateTime == config.raidDeactivateTime && !_save.raidingDeactivated)
                {
                    server.Broadcast(string.Format(lang.GetMessage("ScaledDown", this), (config.scaleDamage * 100), config.raidActivateTime, TimeZone.CurrentTimeZone.StandardName));
                    _save.raidingDeactivated = true;
                    SaveData();
                }

                if (dateTime == config.raidActivateTime && _save.raidingDeactivated)
                {
                    server.Broadcast(string.Format(lang.GetMessage("ScaledUp", this), config.raidDeactivateTime, TimeZone.CurrentTimeZone.StandardName));
                    _save.raidingDeactivated = false;
                    SaveData();
                }
            });
        }

        void OnNewSave(string filename)
        {
            _save.raidingDeactivated = false;
            SaveData();
        }

        void Unload()
        {
            if (AntiRaidTimer != null && !AntiRaidTimer.Destroyed)
            {
                AntiRaidTimer.Destroy();
                AntiRaidTimer = null;
            }

            config = null;
        }

        void OnEntityTakeDamage(StabilityEntity entity, HitInfo info)
        {
            if (info == null || !damageTypes.Contains(info.damageTypes.GetMajorityDamageType())) return;
            if (_save.raidingDeactivated)
            {
                info.damageTypes.ScaleAll(config.scaleDamage);
                if (config.sendMessage && info.InitiatorPlayer != null && info.WeaponPrefab != null) info.InitiatorPlayer.ChatMessage(string.Format(lang.GetMessage("PlayerMessage", this), (config.scaleDamage * 100)));
            }

            return;
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (config.sendMessageOnConnect && _save.raidingDeactivated)
                player.ChatMessage(string.Format(lang.GetMessage("PlayerMessageConnect", this), (config.scaleDamage * 100), config.raidActivateTime, TimeZone.CurrentTimeZone.StandardName));
        }
        #endregion

        #region Commands
        [Command("gettimezone")]
        private void GetTimezoneCMD(IPlayer iPlayer, string command, string[] args)
        {
            if (iPlayer.IsServer)
                Puts($"[Offline Raid Protection] Current Server Timezone: {TimeZone.CurrentTimeZone.StandardName}");
            else
            {
                BasePlayer player = iPlayer.Object as BasePlayer;
                if (!permission.UserHasPermission(player.UserIDString, adminPerm)) { player.ChatMessage(lang.GetMessage("NoPermission", this)); return; }
                player.ChatMessage(string.Format(lang.GetMessage("Timezone", this), TimeZone.CurrentTimeZone.StandardName));
            }
        }

        [Command("toggleraidprotection")]
        private void ToggleRaidProtectionCMD(IPlayer iPlayer, string command, string[] args)
        {
            if (!iPlayer.IsServer)
            {
                BasePlayer player = iPlayer.Object as BasePlayer;
                if (!permission.UserHasPermission(player.UserIDString, adminPerm)) { player.ChatMessage(lang.GetMessage("NoPermission", this)); return; }
            }

            _save.raidingDeactivated = !_save.raidingDeactivated;

            if (_save.raidingDeactivated)
            {
                Puts("[Offline Raid Protection] Turned on raid protection.");
                server.Broadcast(string.Format(lang.GetMessage("ScaledDown", this), (config.scaleDamage * 100), config.raidActivateTime, TimeZone.CurrentTimeZone.StandardName));
            }
            else
            {
                Puts("[Offline Raid Protection] Turned off raid protection.");
                server.Broadcast(string.Format(lang.GetMessage("ScaledUp", this), config.raidDeactivateTime, TimeZone.CurrentTimeZone.StandardName));
            }

            SaveData();
        }
        #endregion
    }
}