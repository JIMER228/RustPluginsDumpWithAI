using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TutorialDisplay", "RoadTech", "2.0.4")]
    [Description("Displays custom tutorials for players with specific permissions")]
    public class TutorialDisplay : RustPlugin
    {
        [PluginReference]
        Plugin ImageLibrary, ZoneManager;

        private StoredData storedData;

        private HashSet<string> playersWithTutorial = new HashSet<string>();
        private Dictionary<string, TutorialDisplayData> tutorialData;
        private HashSet<string> playersClosedTutorial = new HashSet<string>();
        private Dictionary<ulong, Timer> zoneTutorialTimers = new Dictionary<ulong, Timer>();
        private Dictionary<ulong, (float Timestamp, float Duration)> playerZoneTimers = new Dictionary<ulong, (float Timestamp, float Duration)>();
        private Dictionary<ulong, Dictionary<string, int>> playerZoneDisplays = new Dictionary<ulong, Dictionary<string, int>>();
        private Dictionary<ulong, Dictionary<string, int>> playerTutorialDisplays = new Dictionary<ulong, Dictionary<string, int>>();
        private Dictionary<ulong, Dictionary<string, int>> playerZoneCommands = new Dictionary<ulong, Dictionary<string, int>>();
        private Dictionary<ulong, Dictionary<string, Dictionary<string, int>>> playerZoneTutorialDisplays = new Dictionary<ulong, Dictionary<string, Dictionary<string, int>>>();
        private Dictionary<ulong, bool> playerZoneUiDisplayed = new Dictionary<ulong, bool>();
        private Dictionary<ulong, (TutorialInfo tutorialInfo, DescriptionInfo descriptionInfo)> playerCurrentTutorials = new Dictionary<ulong, (TutorialInfo, DescriptionInfo)>();

        private void CheckPlayerPermissions()
        {
            var players = BasePlayer.activePlayerList;
            foreach (var player in players)
            {
                if (playerZoneUiDisplayed != null && playerZoneUiDisplayed.TryGetValue(player.userID, out var isZoneUiDisplayed) && isZoneUiDisplayed)
                {
                    continue;
                }

                if (permission == null || tutorialData == null) continue;

                var userPermissions = permission.GetUserPermissions(player.UserIDString);
                var relevantPermissions = tutorialData.Keys.Intersect(userPermissions);
                bool hasAnyTutorialPermission = relevantPermissions.Any();
                bool shouldDestroyUI = false;

                if (hasAnyTutorialPermission)
                {
                    if (playerCurrentTutorials != null && playerCurrentTutorials.TryGetValue(player.userID, out var currentTutorial))
                    {
                        if (!relevantPermissions.Contains(currentTutorial.tutorialInfo.Tutorial.Permission))
                        {
                            shouldDestroyUI = true;
                        }
                    }
                    else
                    {
                        UpdateTutorialDisplayUI(player);
                    }
                }
                else
                {
                    shouldDestroyUI = true;
                }

                if (shouldDestroyUI)
                {
                    DestroyTutorialDisplayUI(player);
                    if (playerCurrentTutorials != null)
                    {
                        playerCurrentTutorials.Remove(player.userID);
                    }
                }
            }
        }

        private ConfigData configData;

        public class TutorialDisplayData
        {
            public string Permission { get; set; }
            public string Title { get; set; }
            public List<string> TextPages { get; set; }
            public int CharacterImageLevel { get; set; }
            public string Effect { get; set; }
        }

        public class TutorialInfo
        {
            public TutorialDisplayData Tutorial { get; set; }
            public int CurrentPage { get; set; }
        }

        public class ZoneTutorialEvent
        {
            public List<string> DescriptionPages { get; set; }
            public string Title { get; set; }
            public int CharacterImageLevel { get; set; }
            public float Timer { get; set; }
            public string Effect { get; set; }
            public int? MaxDisplays { get; set; }
            public List<string> Commands { get; set; }
            public string Permission { get; set; }
        }


        public class DescriptionInfo
        {
            public ZoneTutorialEvent TutorialEvent { get; set; }
            public int CurrentPage { get; set; }
        }


        class ConfigData
        {
            public List<TutorialDisplayData> Tutorials { get; set; }
            public Dictionary<string, ZoneTutorialData> ZoneTutorials { get; set; }
        }

        private class ZoneTutorialData
        {
            public ZoneTutorialEvent OnEnter { get; set; }
            public ZoneTutorialEvent OnLeave { get; set; }
        }

        private Dictionary<string, ZoneTutorialData> zoneTutorialData;
        private Dictionary<ulong, float> playerZoneTimestamps = new Dictionary<ulong, float>();

        private class StoredData
        {
            public Dictionary<ulong, PlayerTutorialData> PlayerTutorialDisplays = new Dictionary<ulong, PlayerTutorialData>();
            public Dictionary<ulong, PlayerZoneTimerData> PlayerZoneTimers = new Dictionary<ulong, PlayerZoneTimerData>(); // Ajouté
        }

        private class PlayerZoneTimerData
        {
            public float Timestamp { get; set; }
            public float Duration { get; set; }
        }

        private class PlayerTutorialData
        {
            public Dictionary<string, int> OnEnter = new Dictionary<string, int>();
            public Dictionary<string, int> OnLeave = new Dictionary<string, int>();
        }


        protected override void LoadDefaultConfig()
        {
            ConfigData defaultConfig = new ConfigData
            {
                Tutorials = new List<TutorialDisplayData>
                {
                    new TutorialDisplayData
                    {
                        Permission = "tutorialdisplay.first",
                        Title = "WELCOME TO 'YOUR SERVER NAME'",
                        TextPages = new List<string>
                        {
                            "You have just regained your senses, stay calm!",
                            "First, leave the hospital and head to the town hall to register and obtain your ID card.",
                            "ADJUST THE TEXT LIKE YOU WANT"
                        },
                        CharacterImageLevel = 1,
                        Effect = "assets/prefabs/misc/easter/painted eggs/effects/gold_open.prefab"
                    },
                    new TutorialDisplayData
                    {
                        Permission = "tutorialdisplay.second",
                        Title = "CREATE BANK ACCOUNT",
                        TextPages = new List<string>
                        {
                            "After getting your ID, open a bank account",
                            "At the local bank to manage your in-game finances.",
                            "ADJUST THE TEXT LIKE YOU WANT"
                        },
                        CharacterImageLevel = 2,
                        Effect = "assets/prefabs/misc/easter/painted eggs/effects/gold_open.prefab"
                    },
                    new TutorialDisplayData
                    {
                        Permission = "tutorialdisplay.third",
                        Title = "TAKE YOUR JOB",
                        TextPages = new List<string>
                        {
                            "Visit the employment agency",
                            "Choose a suitable job to start earning money",
                            "And progressing. ADJUST THE TEXT LIKE YOU WANT"
                        },
                        CharacterImageLevel = 3,
                        Effect = "assets/prefabs/misc/easter/painted eggs/effects/gold_open.prefab"
                    },
                    new TutorialDisplayData
                    {
                        Permission = "tutorialdisplay.fourth",
                        Title = "START HISTORY QUEST",
                        TextPages = new List<string>
                        {
                            "Embark on an adventure by starting the main quest.",
                            "Find the indicated starting point",
                            "To uncover the server's story. ADJUST THE TEXT LIKE YOU WANT"
                        },
                        CharacterImageLevel = 4,
                        Effect = "assets/prefabs/misc/easter/painted eggs/effects/gold_open.prefab"
                    }
                },

                ZoneTutorials = new Dictionary<string, ZoneTutorialData>
                {
                    {
                        "64890463",
                        new ZoneTutorialData
                        {
                            OnEnter = new ZoneTutorialEvent
                            {
                                DescriptionPages = new List<string> { "Description when entering the zone", "Another page of description", "And so on..." },
                                Title = "Enter Zone Title",
                                CharacterImageLevel = 1,
                                Permission = null,
                                Timer = 10.0f,
                                Effect = "assets/prefabs/misc/easter/painted eggs/effects/gold_open.prefab",
                                MaxDisplays = null,
                                Commands = new List<string> { "say Welcome to the zone! {playername}", "othercommand" }
                            },
                            OnLeave = new ZoneTutorialEvent
                            {
                                DescriptionPages = new List<string> { "Description when entering the zone", "Another page of description", "And so on..." },
                                Title = "Leave Zone Title",
                                CharacterImageLevel = 2,
                                Permission = "tutorialdisplay.onleave",
                                Timer = 10.0f,
                                Effect = "assets/prefabs/misc/easter/painted eggs/effects/gold_open.prefab",
                                MaxDisplays = 3,
                                Commands = new List<string> { "say Goodbye from the zone!", "othercommand" }
                            }
                        }
                    },
                }
            };

            Config.WriteObject(defaultConfig, true);
        }

        private void SavePlayerTutorialDisplays()
        {
            Config["PlayerZoneTutorialDisplays"] = playerZoneTutorialDisplays.ToDictionary(
                entry => entry.Key.ToString(),
                entry => entry.Value.ToDictionary(
                    zoneEntry => zoneEntry.Key,
                    zoneEntry => zoneEntry.Value.ToDictionary(
                        eventEntry => eventEntry.Key,
                        eventEntry => (object)eventEntry.Value
                    )
                )
            );
            SaveConfig();
        }

        private void Init()
        {
            configData = Config.ReadObject<ConfigData>();
            if (configData == null)
            {
                PrintWarning("Configuration file is missing or corrupt. Loading default configuration.");
                LoadDefaultConfig();
                configData = Config.ReadObject<ConfigData>();
            }
            else
            {
                PrintWarning("Configuration file loaded successfully.");
            }

            LoadPlayerTutorialDisplays();
            ZoneManager?.CallHook("OnEnterZone", this);
            ZoneManager?.CallHook("OnExitZone", this);
            timer.Every(5f, CheckPlayerPermissionsWrapper);

            timer.Every(10f, CheckZoneTimersWrapper);

            if (storedData == null)
            {
                storedData = new StoredData();
            }

            LoadZoneTimers();
        }

        private void OnServerInitialized()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("TutorialDisplayData");
            var success = ImageLibrary?.Call("AddImage", "http://www.image-heberg.fr/files/16979388842771375397.png", "ImageID1");
                if (success == null || !(bool)success)
                {
                    Puts("Failed to load ImageID1");
                }

                success = ImageLibrary?.Call("AddImage", "http://www.image-heberg.fr/files/16979372391516802823.png", "ImageID2");
                if (success == null || !(bool)success)
                {
                    Puts("Failed to load ImageID2");
                }

                success = ImageLibrary?.Call("AddImage", "http://www.image-heberg.fr/files/16979774362771375397.png", "ImageID3");
                if (success == null || !(bool)success)
                {
                    Puts("Failed to load ImageID3");
                }

                success = ImageLibrary?.Call("AddImage", "http://www.image-heberg.fr/files/16979779363922503647.png", "ImageID4");
                if (success == null || !(bool)success)
                {
                    Puts("Failed to load ImageID4");
                }
            LoadConfigData();

            if (configData == null)
            {
                PrintWarning("configData is null. Please check the LoadConfigData method.");
                return;
            }

            if (configData.Tutorials == null)
            {
                PrintWarning("configData.Tutorials is null. Please check the configuration file.");
                return;
            }

            RegisterTutorialPermissions();

            RegisterZonePermissions();

            permission.RegisterPermission("tutorialdisplay.admin", this);
        }

        private void RegisterTutorialPermissions()
        {
            foreach (var tutorial in configData.Tutorials)
            {
                if (!string.IsNullOrEmpty(tutorial.Permission) && tutorial.Permission.StartsWith("tutorialdisplay."))
                {
                    permission.RegisterPermission(tutorial.Permission, this);
                }
            }
        }

        private void RegisterZonePermissions()
        {
            foreach (var zoneTutorial in configData.ZoneTutorials.Values)
            {
                if (!string.IsNullOrEmpty(zoneTutorial.OnEnter.Permission) && zoneTutorial.OnEnter.Permission.StartsWith("tutorialdisplay."))
                {
                    permission.RegisterPermission(zoneTutorial.OnEnter.Permission, this);
                }

                if (!string.IsNullOrEmpty(zoneTutorial.OnLeave.Permission) && zoneTutorial.OnLeave.Permission.StartsWith("tutorialdisplay."))
                {
                    permission.RegisterPermission(zoneTutorial.OnLeave.Permission, this);
                }
            }
        }

        private void LoadPlayerTutorialDisplays()
        {
            var storedData = Config["PlayerZoneTutorialDisplays"] as Dictionary<string, object>;
            if (storedData != null)
            {
                foreach (var playerEntry in storedData)
                {
                    ulong playerId = ulong.Parse(playerEntry.Key);
                    var zonesData = playerEntry.Value as Dictionary<string, object>;
                    if (!playerZoneTutorialDisplays.ContainsKey(playerId))
                    {
                        playerZoneTutorialDisplays[playerId] = new Dictionary<string, Dictionary<string, int>>();
                    }
                    foreach (var zoneEntry in zonesData)
                    {
                        var eventsData = zoneEntry.Value as Dictionary<string, object>;
                        if (!playerZoneTutorialDisplays[playerId].ContainsKey(zoneEntry.Key))
                        {
                            playerZoneTutorialDisplays[playerId][zoneEntry.Key] = new Dictionary<string, int>();
                        }
                        foreach (var eventEntry in eventsData)
                        {
                            playerZoneTutorialDisplays[playerId][zoneEntry.Key][eventEntry.Key] = Convert.ToInt32(eventEntry.Value);
                        }
                    }
                }
            }
        }

        private void LoadConfigData()
        {
            var configData = Config.ReadObject<ConfigData>();
            if (configData == null)
            {
                Puts("Failed to load config data");
                return;
            }

            tutorialData = configData.Tutorials.ToDictionary(t => t.Permission, t => t);
            zoneTutorialData = configData.ZoneTutorials;
        }

        private TutorialDisplayData ConvertToTutorialDisplayData(Dictionary<string, object> data)
        {
            var tutorialDisplayData = new TutorialDisplayData
            {
                Title = data.ContainsKey("Title") ? data["Title"].ToString() : "",
                CharacterImageLevel = data.ContainsKey("CharacterImageLevel") ? Convert.ToInt32(data["CharacterImageLevel"]) : 0,
                Effect = data.ContainsKey("Effect") ? data["Effect"].ToString() : "",
                TextPages = data.ContainsKey("TextPages") 
                    ? ((List<object>)data["TextPages"]).ConvertAll(x => x.ToString()) 
                    : new List<string>()
            };

            return tutorialDisplayData;
        }

        private void ApplyEffect(BasePlayer player, string effect)
        {
            if (!string.IsNullOrEmpty(effect))
            {
                Effect.server.Run(effect, player.transform.position);
            }
        }

        private ZoneTutorialData ConvertToZoneTutorialData(Dictionary<string, object> data)
        {
            return new ZoneTutorialData
            {
                OnEnter = ConvertToZoneTutorialEvent(data["OnEnter"] as Dictionary<string, object>),
                OnLeave = ConvertToZoneTutorialEvent(data["OnLeave"] as Dictionary<string, object>)
            };
        }

        private ZoneTutorialEvent ConvertToZoneTutorialEvent(Dictionary<string, object> data)
        {
            if (data == null) return null;

            return new ZoneTutorialEvent
            {
                DescriptionPages = data.ContainsKey("DescriptionPages") 
                    ? ((List<object>)data["DescriptionPages"]).ConvertAll(x => x.ToString()) 
                    : new List<string>(),
                Title = data.ContainsKey("Title") ? data["Title"].ToString() : "",
                CharacterImageLevel = data.ContainsKey("CharacterImageLevel") ? Convert.ToInt32(data["CharacterImageLevel"]) : 0,
                Timer = data.ContainsKey("Timer") ? Convert.ToSingle(data["Timer"]) : 0,
                Effect = data.ContainsKey("Effect") ? data["Effect"].ToString() : "",
                MaxDisplays = data.ContainsKey("MaxDisplays") ? (int?)Convert.ToInt32(data["MaxDisplays"]) : null,
                Commands = data.ContainsKey("Commands") ? ((List<object>)data["Commands"]).ConvertAll(x => x.ToString()) : new List<string>()
            };
        }

        private void SaveZoneTimers()
        {
            storedData.PlayerZoneTimers = playerZoneTimers.ToDictionary(
                entry => entry.Key,
                entry => new PlayerZoneTimerData
                {
                    Timestamp = entry.Value.Timestamp,
                    Duration = entry.Value.Duration
                }
            );
            SaveData();
        }

        private void LoadZoneTimers()
        {
            storedData.PlayerZoneTimers ??= new Dictionary<ulong, PlayerZoneTimerData>();

            foreach (var entry in storedData.PlayerZoneTimers)
            {
                playerZoneTimers[entry.Key] = (entry.Value.Timestamp, entry.Value.Duration);
            }
        }

        private void StartZoneTimer(BasePlayer player, float duration)
        {
            var userID = player.userID;
            playerZoneTimers[userID] = (Time.realtimeSinceStartup, duration);
            zoneTutorialTimers[userID] = timer.Once(duration, () =>
            {
                playerZoneTimers.Remove(userID);
                HideZoneTutorial(player);
            });
            SaveZoneTimers();

        }

        private bool StopZoneTimer(BasePlayer player, string zoneId)
        {
            var userID = player.userID;
            bool timerWasActive = false;

            if (zoneTutorialTimers.TryGetValue(userID, out var timer))
            {
                timerWasActive = true;
                timer.Destroy();
                zoneTutorialTimers.Remove(userID);
            }

            if (playerZoneTimers.ContainsKey(userID))
            {
                playerZoneTimers.Remove(userID);
                UpdateZoneTutorialDisplayUI(player, zoneId, false);
            }

            SaveZoneTimers();

            return timerWasActive;
        }
        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("TutorialDisplayData", storedData);
        }

        [ChatCommand("tutorial")]
        private void TutorialCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                SendReply(player, lang.GetMessage("Command.Usage", this, player.UserIDString));
                return;
            }

            switch (args[0].ToLower())
            {
                case "on":
                    var userPermissions = permission.GetUserPermissions(player.UserIDString);
                    var hasTutorialPermissions = tutorialData.Keys.Any(perm => userPermissions.Contains(perm));

                    if (!hasTutorialPermissions)
                    {
                        SendReply(player, "Sorry, you don't have any active tutorial.");
                        return;
                    }

                    playersClosedTutorial.Remove(player.UserIDString);
                    UpdateTutorialDisplayUI(player);
                    SendReply(player, lang.GetMessage("Tutorial.On", this, player.UserIDString));
                    break;

                case "off":
                    DestroyTutorialDisplayUI(player);
                    playersClosedTutorial.Add(player.UserIDString);
                    SendReply(player, lang.GetMessage("Tutorial.Off", this, player.UserIDString));
                    break;

                default:
                    SendReply(player, lang.GetMessage("Invalid.Argument", this, player.UserIDString));
                    break;
            }
        }

        [ChatCommand("reset_tutorial")]
        private void ResetTutorialCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "tutorialdisplay.admin"))
            {
                SendReply(player, "Vous n'avez pas la permission d'utiliser cette commande.");
                return;
            }

            if (args.Length == 0)
            {
                ResetAllTutorialData();
                SendReply(player, "Toutes les données de tutoriel ont été réinitialisées pour tous les joueurs.");
            }
            else
            {
                string targetPlayerName = args[0];
                BasePlayer targetPlayer = BasePlayer.activePlayerList.FirstOrDefault(p => p.displayName.Equals(targetPlayerName, StringComparison.OrdinalIgnoreCase));

                if (targetPlayer == null)
                {
                    SendReply(player, $"Joueur '{targetPlayerName}' introuvable.");
                    return;
                }

                ResetTutorialDataForPlayer(targetPlayer);
                SendReply(player, $"Les données de tutoriel ont été réinitialisées pour le joueur '{targetPlayerName}'.");
            }
        }

        private void ResetTutorialDataForPlayer(BasePlayer targetPlayer)
        {
            storedData.PlayerTutorialDisplays.Remove(targetPlayer.userID);
            playerTutorialDisplays.Remove(targetPlayer.userID);

            SaveData();

            UpdateTutorialDisplayUI(targetPlayer);
        }

        private void ResetAllTutorialData()
        {
            storedData.PlayerTutorialDisplays.Clear();

            playerTutorialDisplays.Clear();

            SaveData();

            UpdateAllPlayersTutorialUI();
        }

        private void UpdateAllPlayersTutorialUI()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                UpdateTutorialDisplayUI(player);
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            UpdateTutorialDisplayUI(player);
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            DestroyTutorialDisplayUI(player);

            if (zoneTutorialTimers.TryGetValue(player.userID, out var playerTimer))
            {
                playerTimer.Destroy();
                zoneTutorialTimers.Remove(player.userID);
            }
        }

        private void OnUserPermissionAdded(string userId, string perm)
        {
            HandlePermissionChange(userId, perm, true);
        }

        private void OnUserPermissionRemoved(string userId, string perm)
        {
            HandlePermissionChange(userId, perm, false);
        }

        private void OnGroupPermissionAdded(string groupName, string perm)
        {
            HandleGroupPermissionChange(groupName, perm, true);
        }

        private void OnGroupPermissionRemoved(string groupName, string perm)
        {
            HandleGroupPermissionChange(groupName, perm, false);
        }

        void OnEnterZone(string ZoneID, BasePlayer player)
        {
            HandleZoneEvent(ZoneID, player, true);
        }

        void OnExitZone(string ZoneID, BasePlayer player)
        {
            HandleZoneEvent(ZoneID, player, false);
        }

        private void HandleZoneEvent(string zoneId, BasePlayer player, bool isEntering)
        {
            bool timerWasActive = StopZoneTimer(player, zoneId);

            if (!zoneTutorialData.TryGetValue(zoneId, out var tutorialData)) return;

            var zoneEvent = isEntering ? tutorialData.OnEnter : tutorialData.OnLeave;
            if (zoneEvent == null) return;

            // Vérifier si le joueur a la permission requise pour cet événement de zone
            if (!string.IsNullOrEmpty(zoneEvent.Permission) && !permission.UserHasPermission(player.UserIDString, zoneEvent.Permission))
            {
                Puts($"Player {player.displayName} does NOT have permission '{zoneEvent.Permission}'");
                DestroyTutorialDisplayUI(player); // Détruire l'UI si le joueur n'a pas la permission
                return;
            }

            // Autres vérifications et logique
            if (!CanDisplayTutorial(player.userID, zoneId, isEntering ? "enter" : "leave", zoneEvent.MaxDisplays))
            {
                return;
            }

            if (!HasSeenTutorial(player.userID, zoneId, isEntering ? "enter" : "leave"))
            {
                ExecuteCommandsForZoneEvent(player, zoneEvent.Commands);
            }

            IncrementTutorialDisplay(player.userID, zoneId, isEntering ? "enter" : "leave");

            if (isEntering && zoneEvent.Timer > 0)
            {
                StartZoneTimer(player, zoneEvent.Timer);
            }

            if (!playerCurrentTutorials.TryGetValue(player.userID, out var currentTutorial))
            {
                currentTutorial = (new TutorialInfo(), new DescriptionInfo());
            }

            currentTutorial.descriptionInfo = new DescriptionInfo
            {
                TutorialEvent = zoneEvent,
                CurrentPage = 0
            };

            playerCurrentTutorials[player.userID] = currentTutorial;

            if (isEntering)
            {
                ShowZoneTutorial(player, zoneEvent);
                playerZoneUiDisplayed[player.userID] = true;
            }
            else
            {
                if (timerWasActive)
                {
                    ShowZoneTutorial(player, zoneEvent);
                }
                else
                {
                    if (playerZoneUiDisplayed.ContainsKey(player.userID) && playerZoneUiDisplayed[player.userID])
                    {
                        DestroyTutorialDisplayUI(player);
                    }
                }
                playerZoneUiDisplayed.Remove(player.userID);
            }

            UpdateZoneTutorialDisplayUI(player, zoneId, isEntering);
        }
        
        private bool HasSeenTutorial(ulong playerId, string zoneId, string eventType)
        {
            if (storedData.PlayerTutorialDisplays.TryGetValue(playerId, out var playerData))
            {
                var eventData = eventType == "enter" ? playerData.OnEnter : playerData.OnLeave;
                return eventData.ContainsKey(zoneId);
            }
            return false;
        }

        private bool CanDisplayTutorial(ulong playerId, string zoneId, string eventType, int? maxDisplays)
        {
            if (!maxDisplays.HasValue)
            {
                return true;
            }

            if (!storedData.PlayerTutorialDisplays.TryGetValue(playerId, out var playerData))
            {
                return true;
            }

            var eventData = eventType == "enter" ? playerData.OnEnter : playerData.OnLeave;
            if (eventData.TryGetValue(zoneId, out var count))
            {
                return count < maxDisplays.Value;
            }

            return true;
        }

        private void IncrementTutorialDisplay(ulong playerId, string zoneId, string eventType)
        {
            if (!storedData.PlayerTutorialDisplays.ContainsKey(playerId))
            {
                storedData.PlayerTutorialDisplays[playerId] = new PlayerTutorialData();
            }

            var playerData = storedData.PlayerTutorialDisplays[playerId];
            var eventData = eventType == "enter" ? playerData.OnEnter : playerData.OnLeave;

            if (!eventData.ContainsKey(zoneId))
            {
                eventData[zoneId] = 0;
            }

            eventData[zoneId]++;
            SaveData();
        }

        private bool CheckAndIncrementDisplayCount(ulong playerId, string zoneId, int? maxDisplays)
        {
            if (!maxDisplays.HasValue)
            {
                return true;
            }

            if (!playerZoneDisplays.TryGetValue(playerId, out var displays))
            {
                displays = new Dictionary<string, int>();
                playerZoneDisplays[playerId] = displays;
            }

            if (displays.TryGetValue(zoneId, out var count) && count >= maxDisplays.Value)
            {
                return false;
            }

            displays[zoneId] = count + 1;
            return true;
        }

        private void ExecuteCommandsForZoneEvent(BasePlayer player, List<string> commands)
        {
            if (commands == null || commands.Count == 0) return;

            foreach (var command in commands)
            {
                var formattedCommand = command.Replace("{playername}", player.displayName);
                rust.RunServerCommand(formattedCommand);
            }
        }

        private void HandlePermissionChange(string userId, string perm, bool isGranted)
        {
            if (tutorialData.ContainsKey(perm))
            {
                var player = BasePlayer.FindByID(ulong.Parse(userId));
                if (player != null)
                {
                    if (isGranted)
                    {
                        UpdateTutorialDisplayUI(player);
                    }
                    else
                    {
                        DestroyTutorialDisplayUI(player);
                    }
                }
            }
        }

        private void HandleGroupPermissionChange(string groupName, string perm, bool isGranted)
        {
            if (tutorialData.ContainsKey(perm))
            {
                var players = BasePlayer.activePlayerList;
                foreach (var player in players)
                {
                    if (permission.UserHasGroup(player.UserIDString, groupName))
                    {
                        if (isGranted)
                        {
                            UpdateTutorialDisplayUI(player);
                        }
                        else
                        {
                            DestroyTutorialDisplayUI(player);
                        }
                    }
                }
            }
        }

        private void UpdateZoneTutorialDisplayUI(BasePlayer player, string zoneId, bool isEntering)
        {
            DestroyTutorialDisplayUI(player);

            if (zoneTutorialData.TryGetValue(zoneId, out var tutorialData))
            {
                var tutorialEvent = isEntering ? tutorialData.OnEnter : tutorialData.OnLeave;
                if (tutorialEvent != null)
                {
                    ShowZoneTutorial(player, tutorialEvent);
                }
            }
        }

        private void UpdateTutorialDisplayUI(BasePlayer player)
        {
            if (playersClosedTutorial.Contains(player.UserIDString))
            {
                return;
            }

            DestroyTutorialDisplayUI(player);

            var userPermissions = permission.GetUserPermissions(player.UserIDString);
            
            TutorialDisplayData highestPriorityTutorial = null;

            if (playerCurrentTutorials.TryGetValue(player.userID, out var savedState))
            {
                if (userPermissions.Contains(savedState.tutorialInfo.Tutorial.Permission))
                {
                    CreateTutorialDisplayUI(player, savedState.tutorialInfo.Tutorial.CharacterImageLevel);
                    UpdateTutorialText(player, savedState.tutorialInfo.Tutorial, savedState.tutorialInfo.CurrentPage);
                }
            }
            else
            {
                foreach (var perm in userPermissions)
                {
                    if (tutorialData.TryGetValue(perm, out var tutorial))
                    {
                        if (highestPriorityTutorial == null || tutorial.CharacterImageLevel > highestPriorityTutorial.CharacterImageLevel)
                        {
                            highestPriorityTutorial = tutorial;
                        }
                    }
                }

                if (highestPriorityTutorial != null)
                {
                    CreateTutorialDisplayUI(player, highestPriorityTutorial.CharacterImageLevel);
                    UpdateTutorialText(player, highestPriorityTutorial, 0);

                    playerCurrentTutorials[player.userID] = (new TutorialInfo { Tutorial = highestPriorityTutorial, CurrentPage = 0 }, new DescriptionInfo());
                }
                else
                {
                    playerCurrentTutorials.Remove(player.userID);
                }
            }
        }

        private void CheckZoneTimers()
        {
            foreach (var entry in playerZoneTimers.ToList())
            {
                if (Time.realtimeSinceStartup - entry.Value.Timestamp >= entry.Value.Duration)
                {
                    var player = BasePlayer.FindByID(entry.Key);
                    if (player != null)
                    {
                        if (!zoneTutorialTimers.ContainsKey(player.userID))
                        {
                            HideZoneTutorial(player);
                        }
                    }
                    playerZoneTimers.Remove(entry.Key);
                }
            }
        }

        private void CheckPlayerPermissionsWrapper()
        {
            CheckPlayerPermissions();
        }

        private void CheckZoneTimersWrapper()
        {
            CheckZoneTimers();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Command.Usage"] = "Usage: /tutorial on|off",
                ["Tutorial.On"] = "Tutorial display is now ON.",
                ["Tutorial.Off"] = "Tutorial display is now OFF.",
                ["Invalid.Argument"] = "Invalid argument. Use /tutorial on|off.",
            }, this, "en");
            
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Command.Usage"] = "Utilisation : /tutorial on|off",
                ["Tutorial.On"] = "L'affichage du tutoriel est maintenant ACTIVÉ.",
                ["Tutorial.Off"] = "L'affichage du tutoriel est maintenant DÉSACTIVÉ.",
                ["Invalid.Argument"] = "Argument invalide. Utilisez /tutorial on|off.",
            }, this, "fr");
        }

        private void CreateTutorialDisplayUI(BasePlayer player, int characterImageLevel)
        {
            var container = new CuiElementContainer();
            string imageId = (string)ImageLibrary.Call("GetImage", "ImageID" + characterImageLevel);

            if (string.IsNullOrEmpty(imageId))
            {
                Puts("Error: Can't load image.");
                return;
            }

            CuiHelper.DestroyUi(player, "TutorialDisplayPanel");

        int panelWidth = 450;
        int panelHeight = 175;

        int offsetX = -(panelWidth / 2);
        int offsetYBottom = 77;
        int offsetYTop = offsetYBottom + panelHeight;

        container.Add(new CuiPanel
        {
            RectTransform = {
                AnchorMin = "0.5 0",
                AnchorMax = "0.5 0",
                OffsetMin = $"{offsetX} {offsetYBottom}",
                OffsetMax = $"{offsetX + panelWidth} {offsetYTop}"
            },
            Image = { Color = "1 1 1 0" }
        }, "Overlay", "TutorialDisplayPanel");

        container.Add(new CuiElement
        {
            Parent = "TutorialDisplayPanel",
            Components =
            {
                new CuiRawImageComponent { Png = imageId },
                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
            }
        });

            CuiHelper.AddUi(player, container);
            playersWithTutorial.Add(player.UserIDString);
        }

        private void UpdateTutorialText(BasePlayer player, TutorialDisplayData tutorial, int currentPage = 0)
        {
            string title = tutorial.Title;
            List<string> textPages = tutorial.TextPages;
            string currentPageText = textPages.ElementAtOrDefault(currentPage);

            CuiHelper.DestroyUi(player, "TutorialTitle");
            CuiHelper.DestroyUi(player, "TutorialCloseButton");
            CuiHelper.DestroyUi(player, "TutorialText");
            CuiHelper.DestroyUi(player, "TutorialPrevButton");
            CuiHelper.DestroyUi(player, "TutorialNextButton");

            var container = new CuiElementContainer();

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.66", AnchorMax = "0.9 0.99" },
                Text = { Text = title, FontSize = 13, Align = TextAnchor.MiddleCenter }
            }, "TutorialDisplayPanel", "TutorialTitle");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.45 0.225", AnchorMax = "0.97 0.825" },
                Text = { Text = currentPageText, FontSize = 11, Align = TextAnchor.MiddleCenter }
            }, "TutorialDisplayPanel", "TutorialText");

            container.Add(new CuiButton
            {
                Button = { Command = "tutorial.close", Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.925 0.75", AnchorMax = "0.99 0.895" },
                Text = { Text = "X", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.8 0.2 0.2 1" }
            }, "TutorialDisplayPanel", "TutorialCloseButton");

            if (currentPage > 0)
            {
                container.Add(new CuiButton
                {
                    Button = { Command = $"tutorial.prev {currentPage - 1}", Color = "0.8 0.2 0.2 0.65" },
                    RectTransform = { AnchorMin = "0.86 0.25", AnchorMax = "0.91 0.35" },
                    Text = { Text = "<", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "TutorialDisplayPanel", "TutorialPrevButton");
            }

            if (currentPage < textPages.Count - 1)
            {
                container.Add(new CuiButton
                {
                    Button = { Command = $"tutorial.next {currentPage + 1}", Color = "0.8 0.2 0.2 0.65" },
                    RectTransform = { AnchorMin = "0.92 0.25", AnchorMax = "0.97 0.35" },
                    Text = { Text = ">", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "TutorialDisplayPanel", "TutorialNextButton");
            }
            CuiHelper.AddUi(player, container);
        }

        private void UpdateZoneTutorialUI(BasePlayer player, ZoneTutorialEvent tutorialEvent, int currentPage = 0)
        {
            if (tutorialEvent == null) return;

            string title = tutorialEvent.Title;
            List<string> descriptionPages = tutorialEvent.DescriptionPages;
            string currentPageDescription = tutorialEvent.DescriptionPages.ElementAtOrDefault(currentPage);

            CuiHelper.DestroyUi(player, "TutorialTitle");
            CuiHelper.DestroyUi(player, "TutorialCloseButton");
            CuiHelper.DestroyUi(player, "TutorialText");
            var container = new CuiElementContainer();

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.66", AnchorMax = "0.9 0.99" },
                Text = { Text = title, FontSize = 13, Align = TextAnchor.MiddleCenter }
            }, "TutorialDisplayPanel", "TutorialTitle");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.45 0.225", AnchorMax = "0.97 0.825" },
                Text = { Text = currentPageDescription, FontSize = 11, Align = TextAnchor.MiddleCenter }
            }, "TutorialDisplayPanel", "TutorialText");


            if (currentPage > 0)
            {
                container.Add(new CuiButton
                {
                    Button = { Command = $"description.prev {currentPage - 1}", Color = "0.8 0.2 0.2 0.65" },
                    RectTransform = { AnchorMin = "0.86 0.25", AnchorMax = "0.91 0.35" },
                    Text = { Text = "<", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "TutorialDisplayPanel", "TutorialPrevButton");
            }

            if (currentPage < descriptionPages.Count - 1)
            {
                container.Add(new CuiButton
                {
                    Button = { Command = $"description.next {currentPage + 1}", Color = "0.8 0.2 0.2 0.65" },
                    RectTransform = { AnchorMin = "0.92 0.25", AnchorMax = "0.97 0.35" },
                    Text = { Text = ">", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "TutorialDisplayPanel", "TutorialNextButton");
            }

            container.Add(new CuiButton
            {
                Button = { Command = "tutorial.close", Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.925 0.75", AnchorMax = "0.99 0.895" },
                Text = { Text = "X", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.8 0.2 0.2 1" }
            }, "TutorialDisplayPanel", "TutorialCloseButton");

            CuiHelper.AddUi(player, container);
        }
        
        private void ShowZoneTutorial(BasePlayer player, ZoneTutorialEvent tutorialEvent)
        {
            if (tutorialEvent == null) return;

            CreateTutorialDisplayUI(player, tutorialEvent.CharacterImageLevel);
            UpdateZoneTutorialUI(player, tutorialEvent);

            if (tutorialEvent.Timer > 0)
            {
                if (zoneTutorialTimers.TryGetValue(player.userID, out var existingTimer))
                {
                    existingTimer.Destroy();
                }

                zoneTutorialTimers[player.userID] = timer.Once(tutorialEvent.Timer, () =>
                {
                    if (player != null && player.IsConnected)
                    {
                        HideZoneTutorial(player);
                    }
                });
            }

            playerZoneUiDisplayed[player.userID] = true;
        }

        private void HideZoneTutorial(BasePlayer player)
        {
            DestroyTutorialDisplayUI(player);
        }

        private void DestroyTutorialDisplayUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "TutorialDisplayPanel");
            playersWithTutorial.Remove(player.UserIDString);
            playerZoneUiDisplayed.Remove(player.userID);
        }

        [ConsoleCommand("tutorial.prev")]
        private void PrevTutorialPageCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            if (!playerCurrentTutorials.TryGetValue(player.userID, out var info))
            {
                return;
            }

            if (info.tutorialInfo.CurrentPage <= 0) return;

            info.tutorialInfo.CurrentPage -= 1;
            UpdateTutorialText(player, info.tutorialInfo.Tutorial, info.tutorialInfo.CurrentPage);

            playerCurrentTutorials[player.userID] = info;
        }
        [ConsoleCommand("tutorial.next")]
        private void NextTutorialPageCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            if (!playerCurrentTutorials.TryGetValue(player.userID, out var info))
            {
                return;
            }

            if (info.tutorialInfo.CurrentPage >= info.tutorialInfo.Tutorial.TextPages.Count - 1) return;

            info.tutorialInfo.CurrentPage += 1;
            UpdateTutorialText(player, info.tutorialInfo.Tutorial, info.tutorialInfo.CurrentPage);

            playerCurrentTutorials[player.userID] = info;
        }
        [ConsoleCommand("description.prev")]
        private void PrevDescriptionPageCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            if (!playerCurrentTutorials.TryGetValue(player.userID, out var info))
            {
                return;
            }

            if (info.descriptionInfo.CurrentPage <= 0) return;

            info.descriptionInfo.CurrentPage -= 1;
            UpdateZoneTutorialUI(player, info.descriptionInfo.TutorialEvent, info.descriptionInfo.CurrentPage);

            playerCurrentTutorials[player.userID] = info;
        }

        [ConsoleCommand("description.next")]
        private void NextDescriptionPageCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
            {
            return;
            }

            if (!playerCurrentTutorials.TryGetValue(player.userID, out var info))
            {
                return;
            }

            if (info.descriptionInfo == null || info.descriptionInfo.TutorialEvent == null)
            {
                return;
            }

            if (info.descriptionInfo.CurrentPage < info.descriptionInfo.TutorialEvent.DescriptionPages.Count - 1)
            {
                info.descriptionInfo.CurrentPage++;
                UpdateZoneTutorialUI(player, info.descriptionInfo.TutorialEvent, info.descriptionInfo.CurrentPage);
                playerCurrentTutorials[player.userID] = info;
            }
        }
        [ConsoleCommand("tutorial.close")]
        private void CloseTutorialCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            DestroyTutorialDisplayUI(player);
            playersClosedTutorial.Add(player.UserIDString);

            if (zoneTutorialTimers.TryGetValue(player.userID, out var playerTimer))
            {
                playerTimer.Destroy();
                zoneTutorialTimers.Remove(player.userID);
            }
        }
    }
}