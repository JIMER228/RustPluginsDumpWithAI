// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using CompanionServer.Handlers;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Time = UnityEngine.Time;

namespace Oxide.Plugins
{
    [Info("Safe Respawn", "VisEntities", "1.5.0")]
    [Description("Gives players temporary protection after spawning.")]
    public class SafeRespawn : RustPlugin
    {
        #region 3rd Party Dependencies

        [PluginReference]
        private readonly Plugin BetterNoEscape;

        #endregion 3rd Party Dependencies

        #region Fields

        private static SafeRespawn _plugin;
        private static Configuration _config;
        private StoredData _storedData;
        private Dictionary<ulong, DateTime> _playerProtectionEndTimes = new Dictionary<ulong, DateTime>();
        private const int LAYER_BEDS = Layers.Mask.Deployed;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Protection Duration Seconds")]
            public float ProtectionDurationSeconds { get; set; }

            [JsonProperty("Enable Protection Against NPC")]
            public bool EnableProtectionAgainstNPC { get; set; }

            [JsonProperty("Enable Protection Against Animals")]
            public bool EnableProtectionAgainstAnimals { get; set; }

            [JsonProperty("Enable Protection Against Patrol Helicopter")]
            public bool EnableProtectionAgainstHelicopter { get; set; }

            [JsonProperty("Protected Players Cannot Harm Others")]
            public bool ProtectedPlayersCannotHarmOthers { get; set; }

            [JsonProperty("Protect Owned Entities")]
            public bool ProtectOwnedEntities { get; set; }

            [JsonProperty("Enable Protection Only For First Spawn")]
            public bool EnableProtectionOnlyForFirstSpawn { get; set; }

            [JsonProperty("Ignore Sleeping Bag Spawns")]
            public bool IgnoreSleepingBagSpawns { get; set; }

            [JsonProperty("Reset Data On Wipe")]
            public bool ResetDataOnWipe { get; set; }

            [JsonProperty("End Protection If Combat Blocked (Better No Escape)")]
            public bool EndProtectionIfCombatBlocked { get; set; }

            [JsonProperty("End Protection If Raid Blocked (Better No Escape)")]
            public bool EndProtectionIfRaidBlocked { get; set; }

            [JsonProperty("UI")]
            public UiConfig Ui { get; set; }
        }

        private class UiConfig
        {
            [JsonProperty("Background Color (RGBA)")]
            public string BackgroundColor { get; set; }

            [JsonProperty("Icon Color (RGBA)")]
            public string IconColor { get; set; }

            [JsonProperty("Font Color (RGBA)")]
            public string FontColor { get; set; }

            [JsonProperty("Icon Path")]
            public string IconPath { get; set; }

            [JsonProperty("Title Text")]
            public string TitleText { get; set; }

            [JsonProperty("Anchor Min")]
            public string AnchorMin { get; set; }

            [JsonProperty("Anchor Max")]
            public string AnchorMax { get; set; }

            [JsonProperty("Offset Min")]
            public string OffsetMin { get; set; }

            [JsonProperty("Offset Max")]
            public string OffsetMax { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            if (string.Compare(_config.Version, "1.1.0") < 0)
                _config.ResetDataOnWipe = defaultConfig.ResetDataOnWipe;

            if (string.Compare(_config.Version, "1.2.0") < 0)
                _config.EnableProtectionAgainstAnimals = defaultConfig.EnableProtectionAgainstAnimals;

            if (string.Compare(_config.Version, "1.3.0") < 0)
            {
                _config.EnableProtectionAgainstHelicopter= defaultConfig.EnableProtectionAgainstHelicopter;
                _config.ProtectedPlayersCannotHarmOthers= defaultConfig.ProtectedPlayersCannotHarmOthers;
                _config.ProtectOwnedEntities = defaultConfig.ProtectOwnedEntities;
            }

            if (string.Compare(_config.Version, "1.4.0") < 0)
                _config.Ui = defaultConfig.Ui;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                ProtectionDurationSeconds = 60f,
                EnableProtectionOnlyForFirstSpawn = true,
                EnableProtectionAgainstAnimals = true,
                EnableProtectionAgainstNPC = true,
                EnableProtectionAgainstHelicopter = true,
                ProtectedPlayersCannotHarmOthers = true,
                ProtectOwnedEntities = true,
                IgnoreSleepingBagSpawns = true,
                ResetDataOnWipe = true,
                EndProtectionIfCombatBlocked = false,
                EndProtectionIfRaidBlocked = false,
                Ui = new UiConfig
                {
                    BackgroundColor = "0.000 0.700 0.000 0.670",
                    IconColor = "0.150 0.850 0.150 1",
                    FontColor = "0.989 0.922 0.910 1",
                    IconPath = "assets/content/ui/map/icon-map_shield.png",
                    TitleText = "Guarded",
                    AnchorMin = "1 0.5",
                    AnchorMax = "1 0.5",
                    OffsetMin = "-176 -109.3333",
                    OffsetMax = "-16 -82.6667"
                }
            };
        }

        #endregion Configuration

        #region Stored Data

        public class StoredData
        {
            [JsonProperty("Previously Connected Players")]
            public HashSet<ulong> PreviouslyConnectedPlayers { get; set; } = new HashSet<ulong>();
        }

        #endregion Stored Data

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
            _storedData = DataFileUtil.LoadOrCreate<StoredData>(DataFileUtil.GetFilePath());
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player == null)
                    continue;

                UI.Hide(player);

                ProtectionComponent protection = player.GetComponent<ProtectionComponent>();
                if (protection != null)
                    protection.DestroySelf();
            }

            _config = null;
            _plugin = null;
        }

        private void OnNewSave()
        {
            if (_config.ResetDataOnWipe)
                DataFileUtil.LoadOrCreate<StoredData>(DataFileUtil.GetFilePath());
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (player == null || !PermissionUtil.HasPermission(player, PermissionUtil.USE))
                return;

            bool isFirstSpawn = !_storedData.PreviouslyConnectedPlayers.Contains(player.userID);
            bool applyProtection = !_config.EnableProtectionOnlyForFirstSpawn || isFirstSpawn;

            if (applyProtection)
            {
                NextTick(() =>
                {
                    if (!_playerProtectionEndTimes.TryGetValue(player.userID, out DateTime protectionEndTime) || DateTime.Now >= protectionEndTime)
                    {
                        if (!_config.IgnoreSleepingBagSpawns || !AnySleepingBagOrBedNearby(player.transform.position, 2f))
                        {
                            _playerProtectionEndTimes[player.userID] = DateTime.Now.AddSeconds(_config.ProtectionDurationSeconds);
                            ProtectionComponent.Install(player, _config.ProtectionDurationSeconds);
                        }
                    }

                    if (isFirstSpawn)
                    {
                        _storedData.PreviouslyConnectedPlayers.Add(player.userID);
                        DataFileUtil.Save(DataFileUtil.GetFilePath(), _storedData);
                    }
                });
            }
        }

        private object OnEntityTakeDamage(BaseEntity hurtEntity, HitInfo hitInfo)
        {
            if (hurtEntity == null || hitInfo == null)
                return null;

            if (hurtEntity is BasePlayer victimPlayer)
            {
                return HandleDamageToPlayer(victimPlayer, hitInfo);
            }
            else if (hurtEntity is BaseEntity entity)
            {
                return HandleDamageToOwnedEntity(hurtEntity, hitInfo);
            }

            return null;
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null)
                return;

            UI.Hide(player);

            ProtectionComponent protection = player.GetComponent<ProtectionComponent>();
            if (protection != null)
                protection.DestroySelf();
        }

        #endregion Oxide Hooks

        #region Damage Handling

        private object HandleDamageToPlayer(BasePlayer hurtPlayer, HitInfo hitInfo)
        {
            if (hurtPlayer == null || hitInfo == null)
                return null;

            BasePlayer attackerPlayer = hitInfo.InitiatorPlayer;
            BaseNpc animalAttacker = hitInfo.Initiator as BaseNpc;
            PatrolHelicopter heliAttacker = hitInfo.Initiator as PatrolHelicopter;

            if (attackerPlayer == hurtPlayer)
                return null;

            if (attackerPlayer == null && animalAttacker == null && heliAttacker == null)
                return null;

            if (_playerProtectionEndTimes.TryGetValue(hurtPlayer.userID, out DateTime victimProtectionEnd))
            {
                if (DateTime.Now < victimProtectionEnd)
                {
                    if (!_config.EnableProtectionAgainstNPC && attackerPlayer != null && attackerPlayer.IsNpc)
                        return null;

                    if (!_config.EnableProtectionAgainstAnimals && animalAttacker != null)
                        return null;

                    if (!_config.EnableProtectionAgainstHelicopter && heliAttacker != null)
                        return null;

                    if (attackerPlayer != null && !PlayerUtil.IsNPC(attackerPlayer))
                    {
                        TimeSpan remaining = victimProtectionEnd - DateTime.Now;
                        MessagePlayer(attackerPlayer, Lang.PlayerProtected, FormatTime(remaining.TotalSeconds));
                    }

                    hitInfo.damageTypes.Clear();
                    return true;
                }
                else
                {
                    _playerProtectionEndTimes.Remove(hurtPlayer.userID);
                }
            }

            if (attackerPlayer != null
                && _config.ProtectedPlayersCannotHarmOthers
                && _playerProtectionEndTimes.TryGetValue(attackerPlayer.userID, out DateTime attackerProtectionEnd)
                && DateTime.Now < attackerProtectionEnd)
            {
                TimeSpan remaining = attackerProtectionEnd - DateTime.Now;
                MessagePlayer(attackerPlayer, Lang.ProtectedCantAttackOthers, FormatTime(remaining.TotalSeconds));

                hitInfo.damageTypes.Clear();
                return true;
            }

            return null;
        }

        private object HandleDamageToOwnedEntity(BaseEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null)
                return null;

            if (!_config.ProtectOwnedEntities)
                return null;

            BasePlayer owner = PlayerUtil.FindById(entity.OwnerID);
            if (owner == null || PlayerUtil.IsNPC(owner))
                return null;

            BasePlayer attackerPlayer = hitInfo.InitiatorPlayer;
            if (attackerPlayer == null || PlayerUtil.IsNPC(attackerPlayer))
                return null;

            if (owner.userID == attackerPlayer.userID)
                return null;

            if (_playerProtectionEndTimes.TryGetValue(owner.userID, out DateTime protectionEndTime))
            {
                if (DateTime.Now < protectionEndTime)
                {
                    TimeSpan remaining = protectionEndTime - DateTime.Now;
                    MessagePlayer(attackerPlayer, Lang.OwnedEntityProtected, FormatTime(remaining.TotalSeconds));

                    hitInfo.damageTypes.Clear();
                    return true;
                }
                else
                {
                    _playerProtectionEndTimes.Remove(owner.userID);
                }
            }

            return null;
        }

        #endregion Damage Handling

        #region Sleeping Bag Detection

        private bool AnySleepingBagOrBedNearby(Vector3 position, float radius)
        {
            List<SleepingBag> nearbySleepingBags = Pool.Get<List<SleepingBag>>();
            bool isNearSleepingBagOrBed = false;

            Vis.Entities(position, radius, nearbySleepingBags, LAYER_BEDS, QueryTriggerInteraction.Ignore);

            foreach (SleepingBag sleepingBag in nearbySleepingBags)
            {
                if (sleepingBag != null)
                {
                    isNearSleepingBagOrBed = true;
                    break;
                }
            }

            Pool.FreeUnmanaged(ref nearbySleepingBags);
            return isNearSleepingBagOrBed;
        }

        #endregion Sleeping Bag Detection

        #region Protection Component

        public class ProtectionComponent : FacepunchBehaviour
        {
            public BasePlayer Player { get; private set; }
            private double _endTime;
            private float _nextUiRefresh;

            private const float UI_REFRESH_INTERVAL = 1f;
            
            public static ProtectionComponent Install(BasePlayer player, double seconds)
            {
                ProtectionComponent protection = player.GetComponent<ProtectionComponent>();
                if (protection == null)
                    protection = player.gameObject.AddComponent<ProtectionComponent>();

                protection.Player = player;
                protection._endTime = Time.realtimeSinceStartup + seconds;
                protection._nextUiRefresh = 0f;
                return protection;
            }

            public void DestroySelf()
            {
                DestroyImmediate(this);
            }

            private void Update()
            {
                if (Player == null)
                    return;

                if ((_config.EndProtectionIfCombatBlocked && BetterNoEscapeUtil.IsCombatBlocked(Player)) ||
                     (_config.EndProtectionIfRaidBlocked && BetterNoEscapeUtil.IsRaidBlocked(Player)))
                {
                    _plugin.CancelProtection(Player);
                    return;
                }

                if (Time.time < _nextUiRefresh)
                    return;

                _nextUiRefresh = Time.time + UI_REFRESH_INTERVAL;

                double secondsLeft = _endTime - Time.realtimeSinceStartup;
                if (secondsLeft <= 0)
                {
                    UI.Hide(Player);
                    Destroy(this);
                    return;
                }

                UI.Show(Player, secondsLeft);
            }

            private void OnDestroy()
            {
                if (Player != null)
                    UI.Hide(Player);
            }
        }

        #endregion Protection Component

        #region 3rd Party Integration

        public static class BetterNoEscapeUtil
        {
            private static bool Loaded
            {
                get
                {
                    return _plugin != null &&
                           _plugin.BetterNoEscape != null &&
                           _plugin.BetterNoEscape.IsLoaded;
                }
            }

            public static bool IsCombatBlocked(BasePlayer player)
            {
                if (!Loaded || player == null)
                    return false;

                return _plugin.BetterNoEscape.Call<bool>("API_IsCombatBlocked", player);
            }

            public static bool IsRaidBlocked(BasePlayer player)
            {
                if (!Loaded || player == null)
                    return false;

                return _plugin.BetterNoEscape.Call<bool>("API_IsRaidBlocked", player);
            }
        }

        #endregion 3rd Party Integration

        #region Helper Functions

        private void CancelProtection(BasePlayer player)
        {
            if (player == null)
                return;

            _playerProtectionEndTimes.Remove(player.userID);

            UI.Hide(player);

            ProtectionComponent protection = player.GetComponent<ProtectionComponent>();
            if (protection != null)
                protection.DestroySelf();
        }

        private static string FormatTime(double seconds)
        {
            if (seconds >= 86400)
                return $"{Math.Floor(seconds / 86400)}d {Math.Floor(seconds % 86400 / 3600)}h";

            if (seconds >= 3600)
                return $"{Math.Floor(seconds / 3600)}h {Math.Floor(seconds % 3600 / 60)}m";

            if (seconds >= 60)
                return $"{Math.Floor(seconds / 60)}m {Math.Floor(seconds % 60)}s";

            return $"{Math.Ceiling(seconds)}s";
        }

        #endregion Helper Functions

        #region Helper Classes

        public class PlayerUtil
        {
            public static BasePlayer FindById(ulong playerId)
            {
                return RelationshipManager.FindByID(playerId);
            }

            public static bool IsNPC(BasePlayer player)
            {
                return player.IsNpc || !player.userID.IsSteamId();
            }
        }

        public class DataFileUtil
        {
            private const string FOLDER = "";

            public static void EnsureFolderCreated()
            {
                string path = Path.Combine(Interface.Oxide.DataDirectory, FOLDER);

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }

            public static string GetFilePath(string filename = null)
            {
                if (filename == null)
                    filename = _plugin.Name;

                return Path.Combine(FOLDER, filename);
            }

            public static string[] GetAllFilePaths(bool filenameOnly = false)
            {
                string[] filePaths = Interface.Oxide.DataFileSystem.GetFiles(FOLDER);

                for (int i = 0; i < filePaths.Length; i++)
                {
                    filePaths[i] = filePaths[i].Substring(0, filePaths[i].Length - 5);

                    if (filenameOnly)
                    {
                        filePaths[i] = Path.GetFileName(filePaths[i]);
                    }
                }
                return filePaths;
            }

            public static bool Exists(string filePath)
            {
                return Interface.Oxide.DataFileSystem.ExistsDatafile(filePath);
            }

            public static T Load<T>(string filePath) where T : class, new()
            {
                T data = Interface.Oxide.DataFileSystem.ReadObject<T>(filePath);
                if (data == null)
                    data = new T();

                return data;
            }

            public static T LoadIfExists<T>(string filePath) where T : class, new()
            {
                if (Exists(filePath))
                    return Load<T>(filePath);
                else
                    return null;
            }

            public static T LoadOrCreate<T>(string filePath) where T : class, new()
            {
                T data = LoadIfExists<T>(filePath);
                if (data == null)
                    data = new T();

                return data;
            }

            public static void Save<T>(string filePath, T data)
            {
                Interface.Oxide.DataFileSystem.WriteObject<T>(filePath, data);
            }

            public static void Delete(string filePath)
            {
                Interface.Oxide.DataFileSystem.DeleteDataFile(filePath);
            }
        }

        #endregion Helper Classes

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "saferespawn.use";
            private static readonly List<string> _permissions = new List<string>
            {
                USE,
            };

            public static void RegisterPermissions()
            {
                foreach (var permission in _permissions)
                {
                    _plugin.permission.RegisterPermission(permission, _plugin);
                }
            }

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permissionName);
            }
        }

        #endregion Permissions

        #region Localization

        private class Lang
        {
            public const string PlayerProtected = "PlayerProtected ";
            public const string OwnedEntityProtected = "OwnedEntityProtected";
            public const string ProtectedCantAttackOthers = "ProtectedCantAttackOthers";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.PlayerProtected] = "The player you tried to attack is under spawn protection for {0}.",
                [Lang.OwnedEntityProtected] = "You cannot damage this player's structures; they are spawn protected for {0}.",
                [Lang.ProtectedCantAttackOthers] = "You cannot attack players while you are under spawn protection for {0}."
            }, this, "en");
        }

        private static string GetMessage(BasePlayer player, string messageKey, params object[] args)
        {
            string message = _plugin.lang.GetMessage(messageKey, _plugin, player.UserIDString);

            if (args.Length > 0)
                message = string.Format(message, args);

            return message;
        }

        public static void MessagePlayer(BasePlayer player, string messageKey, params object[] args)
        {
            string message = GetMessage(player, messageKey, args);

            if (!string.IsNullOrWhiteSpace(message))
                _plugin.SendReply(player, message);
        }

        #endregion Localization

        #region UI

        private static class UI
        {
            private const string UI_ROOT = "saferespawn-ui";

            public static void Show(BasePlayer player, double secondsLeft)
            {
                if (player == null)
                    return;

                string root = UI_ROOT;
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = _config.Ui.BackgroundColor
                    },
                    RectTransform =
                    {
                        AnchorMin = _config.Ui.AnchorMin,
                        AnchorMax = _config.Ui.AnchorMax,
                        OffsetMin = _config.Ui.OffsetMin,
                        OffsetMax = _config.Ui.OffsetMax
                    }
                }, "Hud", root, root);

                container.Add(new CuiElement
                {
                    Parent = root,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Sprite = _config.Ui.IconPath,
                            Color  = _config.Ui.IconColor
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-88 -18",
                            OffsetMax = "-46 18"
                        }
                    }
                });

                container.Add(new CuiLabel
                {
                    Text =
                    {
                        Text  = _config.Ui.TitleText,
                        FontSize = 13,
                        Align = TextAnchor.MiddleLeft,
                        Color = _config.Ui.FontColor,
                        Font  = "RobotoCondensed-Bold.ttf"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                        OffsetMin = "29 -10",
                        OffsetMax = "139 10"
                    }
                }, root);

                container.Add(new CuiLabel
                {
                    Text =
                    {
                        Text  = FormatTime(secondsLeft),
                        FontSize = 13,
                        Align = TextAnchor.MiddleRight,
                        Color = _config.Ui.FontColor,
                        Font  = "RobotoCondensed-Bold.ttf"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                        OffsetMin = "-49.6667 -10",
                        OffsetMax = "-6.3333 10"
                    }
                }, root);

                CuiHelper.AddUi(player, container);
            }

            public static void Hide(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, UI_ROOT);
            }
        }

        #endregion UI
    }
}