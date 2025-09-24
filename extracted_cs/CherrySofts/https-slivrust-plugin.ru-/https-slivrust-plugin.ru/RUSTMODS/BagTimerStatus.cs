// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("BagTimerStatus", "rustmods.ru", "1.0.0")]
    [Description("Shows pending bag timers as statuses.")]
    internal class BagTimerStatus : CovalencePlugin
    {
        [PluginReference]
        private readonly Plugin SimpleStatus;

        private readonly string PermissionShow = "bagtimerstatus.show";

        private const string STATUS_ID = "bagtimerstatus.{0}";

        private readonly HashSet<ulong> ToggledPlayers = new HashSet<ulong>();

        private bool PluginIsLoaded = false;

        private void OnServerInitialized()
        {
            if (!permission.PermissionExists(PermissionShow))
            {
                permission.RegisterPermission(PermissionShow, this);
            }
            if (!string.IsNullOrWhiteSpace(config.ToggleBagStatusCommand))
            {
                AddCovalenceCommand(config.ToggleBagStatusCommand, nameof(CmdToggleBags));
            }
            for(int i = 0; i < config.MaxBagStatuses; i++)
            {
                SimpleStatus.CallHook("CreateStatus", this, BagStatusId(i), config.StatusBackgroundColor, "", config.StatusTitleColor, null, config.StatusTextColor, "itemid:-1754948969", "1 1 1 1");
            }
            Subscribe(nameof(OnPlayerSleepEnded));
            Subscribe(nameof(OnEntitySpawned));
            Subscribe(nameof(CanAssignBed));
            Subscribe(nameof(CanRenameBed));
            Subscribe(nameof(OnSleepingBagDestroyed));
            Subscribe(nameof(OnEntityDeath));
            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                OnPlayerSleepEnded(basePlayer);
            }
        }

        void Unload()
        {
            Unsubscribe(nameof(OnPlayerSleepEnded));
            Unsubscribe(nameof(OnEntitySpawned));
            Unsubscribe(nameof(CanAssignBed));
            Unsubscribe(nameof(CanRenameBed));
            Unsubscribe(nameof(OnSleepingBagDestroyed));
            Unsubscribe(nameof(OnEntityDeath));
            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                UpdateBagStatuses(basePlayer.userID, remove: true);
            }
        }

        void CmdToggleBags(IPlayer player, string wipeday, string[] args)
        {
            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) { return; }
            if (ToggledPlayers.Contains(basePlayer.userID))
            {
                ToggledPlayers.Remove(basePlayer.userID);
            }
            else
            {
                ToggledPlayers.Add(basePlayer.userID);
            }
            UpdateBagStatuses(basePlayer.userID);
        }

        private string BagStatusId(int index) => string.Format(STATUS_ID, index);

        private readonly SleepingBag[] EmptyBagList = new SleepingBag[0];

        private void UpdateBagStatuses(ulong userID, bool remove = false)
        {
            if (userID == 0) { return; }
            SleepingBag[] bags = EmptyBagList;
            if (!remove && !ToggledPlayers.Contains(userID))
            {
                bags = SleepingBag.FindForPlayer(userID, true);
                bags.Reverse();
            }
            var hasPerm = permission.UserHasPermission(userID.ToString(), PermissionShow);
            for (int i = 0; i < config.MaxBagStatuses; i++)
            {
                if (hasPerm && i < bags.Length)
                {
                    var bag = bags[i];
                    var seconds = bag.unlockTime - UnityEngine.Time.realtimeSinceStartup;
                    if (bag.unlockSeconds > 0)
                    {
                        SimpleStatus.Call("SetStatus", userID, BagStatusId(i), (int)Math.Floor(bag.unlockSeconds));
                        SimpleStatus.Call("SetStatusProperty", userID, BagStatusId(i), new Dictionary<string, object>
                        {
                            ["title"] = $"{bag.niceName}",
                            ["icon"] = bag.ShortPrefabName == "bed_deployed" ? $"itemid:-1273339005" : $"itemid:-1754948969"
                        });
                        continue;
                    }
                }
                SimpleStatus.Call("SetStatus", userID, BagStatusId(i), 0);
            }
        }
        #region Hooks
        void OnPlayerSleepEnded(BasePlayer basePlayer) => UpdateBagStatuses(basePlayer.userID);
        void OnEntitySpawned(SleepingBag bag) => UpdateBagStatuses(bag.deployerUserID);
        void CanAssignBed(BasePlayer basePlayer, SleepingBag bag, ulong targetPlayerId)
        {
            NextTick(() =>
            {
                if (basePlayer == null) { return; }
                if (basePlayer.userID != targetPlayerId)
                {
                    UpdateBagStatuses(basePlayer.userID);
                }
                UpdateBagStatuses(targetPlayerId);
            });
        }

        void CanRenameBed(BasePlayer basePlayer, SleepingBag bed, string bedName)
        {
            NextTick(() =>
            {
                if (basePlayer == null) { return; }
                UpdateBagStatuses(basePlayer.userID);
            });
        }

        void OnSleepingBagDestroyed(SleepingBag sleepingBag, BasePlayer player) => UpdateBagStatuses(sleepingBag.OwnerID);

        void OnEntityDeath(SleepingBag sleepingBag, HitInfo info)
        {
            var ownerId = sleepingBag.OwnerID;
            NextTick(() =>
            {
                UpdateBagStatuses(ownerId);
            });
        }

        #endregion

        #region Config
        private Configuration config;
        private partial class Configuration
        {
            public int MaxBagStatuses = 15;
            public string StatusBackgroundColor = "0.25490 0.61176 0.86275 1";
            public string StatusTitleColor = "0.87451 0.83529 0.80000 1";
            public string StatusTextColor = "0.87451 0.83529 0.80000 1";
            public string ToggleBagStatusCommand = "bags";
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new Exception();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
            SaveConfig();
        }
        protected override void SaveConfig() => Config.WriteObject(config);
        protected override void LoadDefaultConfig() => config = new Configuration();
        #endregion
    }
}