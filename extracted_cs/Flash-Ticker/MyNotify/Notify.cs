using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Notify", "RustFlash", "1.0.0")]
    [Description("A modern notification system for Rust")]
    public class Notify : RustPlugin
    {
        #region Felder

        private const string LAYER_NAME = "UI.MyNotify";
        private static Notify instance;
        private readonly Dictionary<ulong, NotificationManager> playerNotifications = new Dictionary<ulong, NotificationManager>();
        private readonly Dictionary<ulong, Timer> notificationTimers = new Dictionary<ulong, Timer>();
        
        private class NotificationData
        {
            public string Message;
            public int Type;
            public string Id = CuiHelper.GetGuid();
            public float CreationTime;
        }

        private const string 
            PermSeeNotify = "notify.see",
            PermNotify = "notify.notify",
            PermPlayerNotify = "notify.player",
            PermAllPlayersNotify = "notify.allplayer";

        #endregion

        #region Konfiguration

        private ConfigData config;

        public class ConfigData
        {
            public string DisplayType = "Overlay"; 
            public float Height = 80f;             
            public float Width = 450f;             
            public float XMargin = 20f;
            public float YMargin = 5f;
            public float YStartPosition = -50f;
            public bool ShowAtTopRight = false;    
            public float DefaultDuration = 5f;
            public int MaxNotificationsOnScreen = 5;
            public bool SendChatMessageIfNoPermission = true;
            public Dictionary<int, NotificationType> NotificationTypes { get; set; }

            public VersionNumber Version { get; set; }
        }

        public class NotificationType
        {
            public bool Enabled = true;
            public string BackgroundColor = "0.12 0.12 0.14 0.95";
            public string BorderColor = "0.4 0.6 1 1";
            public string IconText = "i";
            public string IconColor = "0.4 0.6 1 1";
            public string TitleKey = "Notification";
            public float FadeIn = 0.2f;
            public float FadeOut = 0.5f;
            public string SoundEffect = "assets/bundled/prefabs/fx/notice/item.select.fx.prefab";

            public TextSettings IconSettings = new TextSettings
            {
                AnchorMin = "0.02 0.2",  
                AnchorMax = "0.1 0.8",  
                FontSize = 24,        
                IsBold = true,
                Align = TextAnchor.MiddleCenter,
                Color = "1 1 1 1"
            };

            public TextSettings TitleSettings = new TextSettings
            {
                AnchorMin = "0.12 0.6",  
                AnchorMax = "0.98 0.95", 
                FontSize = 18,       
                IsBold = true,
                Align = TextAnchor.MiddleLeft,
                Color = "1 1 1 1"
            };

            public TextSettings MessageSettings = new TextSettings
            {
                AnchorMin = "0.12 0.05",  
                AnchorMax = "0.98 0.59", 
                FontSize = 16,          
                IsBold = false,
                Align = TextAnchor.UpperLeft, 
                Color = "0.9 0.9 0.9 1"
            };

            public bool UseCustomDuration = false;
            public float CustomDuration = 0f;
            public bool UseCustomWidth = false;
            public float CustomWidth = 0f;
            public bool UseCustomHeight = false;
            public float CustomHeight = 0f;
            public bool UseClickCommand = false;
            public string ClickCommand = "";
            public bool CloseAfterCommand = false;
        }

        public class TextSettings
        {
            public string AnchorMin;
            public string AnchorMax;
            public int FontSize;
            public bool IsBold;
            public TextAnchor Align;
            public string Color;
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
                else if (config.Version < Version)
                {
                    UpdateConfig();
                }
            }
            catch
            {
                PrintError("Error loading the configuration! Load standard configuration...");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Create a new configuration...");

            config = new ConfigData
            {
                NotificationTypes = new Dictionary<int, NotificationType>
                {
                    [0] = new NotificationType 
                    {
                        TitleKey = "Notification",
                        BackgroundColor = "0.12 0.12 0.14 0.95",
                        BorderColor = "0.4 0.6 1 1",
                        IconColor = "0.4 0.6 1 1",
                        IconText = "i"      
                    },
                    [1] = new NotificationType 
                    {
                        TitleKey = "Error",
                        BackgroundColor = "0.12 0.12 0.14 0.95", 
                        BorderColor = "0.9 0.3 0.3 1",
                        IconColor = "0.9 0.3 0.3 1",
                        IconText = "✕"       
                    },
                    [2] = new NotificationType 
                    {
                        TitleKey = "Success",
                        BackgroundColor = "0.12 0.12 0.14 0.95",
                        BorderColor = "0.3 0.9 0.3 1",
                        IconColor = "0.3 0.9 0.3 1",
                        IconText = "✓"       
                    },
                    [3] = new NotificationType 
                    {
                        TitleKey = "Warning",
                        BackgroundColor = "0.12 0.12 0.14 0.95",
                        BorderColor = "0.9 0.7 0.2 1", 
                        IconColor = "0.9 0.7 0.2 1",
                        IconText = "!"       
                    },
                    [4] = new NotificationType 
                    {
                        TitleKey = "Event",
                        BackgroundColor = "0.12 0.12 0.14 0.95",
                        BorderColor = "0.6 0.2 0.8 1",
                        IconColor = "0.6 0.2 0.8 1",
                        IconText = "★",     
                        UseCustomHeight = true,
                        CustomHeight = 80,
                        UseCustomWidth = true,
                        CustomWidth = 450,
                        FadeIn = 0.3f,
                        FadeOut = 0.8f
                    }
                }
            };
        }
        
        private void UpdateConfig()
        {
            PrintWarning("Update configuration to version " + Version.ToString());
                       
            config.Version = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Hooks

        private void OnServerInitialized()
        {
            instance = this;
            
            permission.RegisterPermission(PermSeeNotify, this);
            permission.RegisterPermission(PermNotify, this);
            permission.RegisterPermission(PermPlayerNotify, this);
            permission.RegisterPermission(PermAllPlayersNotify, this);
            
            AddCovalenceCommand("notify.show", nameof(CmdShowNotify));
            AddCovalenceCommand("notify.player", nameof(CmdShowPlayerNotify));
            AddCovalenceCommand("notify.allplayers", nameof(CmdShowAllPlayerNotify));
            
            // Für Kompatibilität auch die mynotify-Befehle registrieren
            AddCovalenceCommand("mynotify.show", nameof(CmdShowNotify));
            AddCovalenceCommand("mynotify.player", nameof(CmdShowPlayerNotify));
            AddCovalenceCommand("mynotify.allplayers", nameof(CmdShowAllPlayerNotify));
        }

        private void Unload()
        {
            foreach (var manager in playerNotifications.Values.ToList())
            {
                manager?.Destroy();
            }

            foreach (var timer in notificationTimers.Values.ToList())
            {
                timer?.Destroy();
            }
            notificationTimers.Clear();

            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, LAYER_NAME);
            }

            instance = null;
        }

        #endregion

        #region Commands

        private void CmdShowNotify(IPlayer player, string command, string[] args)
        {
            if (!player.IsServer && !permission.UserHasPermission(player.Id, PermNotify))
            {
                player.Reply(GetMsg("NoPermission", player.Id));
                return;
            }

            BasePlayer basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;

            if (args.Length < 2 || !int.TryParse(args[0], out int type))
            {
                player.Reply(string.Format(GetMsg("SyntaxNotify", player.Id), command));
                return;
            }

            string message = string.Join(" ", args.Skip(1));
            if (string.IsNullOrEmpty(message)) return;

            SendNotify(basePlayer, type, message);
        }

        private void CmdShowPlayerNotify(IPlayer player, string command, string[] args)
        {
            if (!player.IsServer && !permission.UserHasPermission(player.Id, PermPlayerNotify))
            {
                player.Reply(GetMsg("NoPermission", player.Id));
                return;
            }

            if (args.Length < 3 || !int.TryParse(args[1], out int type))
            {
                player.Reply(string.Format(GetMsg("SyntaxPlayerNotify", player.Id), command));
                return;
            }

            IPlayer target = covalence.Players.FindPlayer(args[0]);
            if (target == null || !(target.Object is BasePlayer))
            {
                player.Reply(string.Format(GetMsg("PlayerNotFound", player.Id), args[0]));
                return;
            }

            string message = string.Join(" ", args.Skip(2));
            if (string.IsNullOrEmpty(message)) return;

            SendNotify(target.Object as BasePlayer, type, message);
        }

        private void CmdShowAllPlayerNotify(IPlayer player, string command, string[] args)
        {
            if (!player.IsServer && !permission.UserHasPermission(player.Id, PermAllPlayersNotify))
            {
                player.Reply(GetMsg("NoPermission", player.Id));
                return;
            }

            if (args.Length < 2 || !int.TryParse(args[0], out int type))
            {
                player.Reply(string.Format(GetMsg("SyntaxAllPlayerNotify", player.Id), command));
                return;
            }

            string message = string.Join(" ", args.Skip(1));
            if (string.IsNullOrEmpty(message)) return;

            SendNotifyAllPlayers(type, message);
        }

        #endregion
        
        #region API Methoden 

        [HookMethod("SendNotify")]
        private void SendNotify(BasePlayer player, int type, string message)
        {
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PermSeeNotify))
            {
                if (config.SendChatMessageIfNoPermission)
                    player.ChatMessage(message);
                return;
            }
            
            if (!config.NotificationTypes.TryGetValue(type, out NotificationType notifyType) || !notifyType.Enabled)
            {
                if (!config.NotificationTypes.TryGetValue(0, out notifyType) || !notifyType.Enabled)
                    return;
            }
            
            ShowNotification(player, message, notifyType, type);
            
            if (!string.IsNullOrEmpty(notifyType.SoundEffect))
            {
                PlayEffect(player, notifyType.SoundEffect);
            }
        }
        
        [HookMethod("SendNotify")]
        private void SendNotify(string userId, int type, string message)
        {
            if (string.IsNullOrEmpty(userId)) return;
            
            ulong steamId;
            if (ulong.TryParse(userId, out steamId))
            {
                SendNotify(BasePlayer.FindByID(steamId), type, message);
            }
        }
        
        [HookMethod("SendNotify")]
        private void SendNotify(ulong userId, int type, string message)
        {
            SendNotify(BasePlayer.FindByID(userId), type, message);
        }
        
        [HookMethod("SendNotifyAllPlayers")]
        private void SendNotifyAllPlayers(int type, string message)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                SendNotify(player, type, message);
            }
        }

        // Kompatibilität mit dem ursprünglichen Notify API
        [HookMethod("SendNotifyToPlayer")]
        private void SendNotifyToPlayer(BasePlayer player, int type, string message)
        {
            SendNotify(player, type, message);
        }

        [HookMethod("SendNotifyToPlayer")]
        private void SendNotifyToPlayer(string userId, int type, string message)
        {
            SendNotify(userId, type, message);
        }

        [HookMethod("SendNotifyToPlayer")]
        private void SendNotifyToPlayer(ulong userId, int type, string message)
        {
            SendNotify(userId, type, message);
        }

        #endregion

        #region UI and notifications

        private void ShowNotification(BasePlayer player, string message, NotificationType type, int typeId)
        {
            if (player == null || !player.IsConnected) return;
            
            CuiHelper.DestroyUi(player, LAYER_NAME);
            
            if (notificationTimers.TryGetValue(player.userID, out var existingTimer))
            {
                existingTimer?.Destroy();
                notificationTimers.Remove(player.userID);
            }
            
            var container = new CuiElementContainer();
            
            float width = type.UseCustomWidth ? type.CustomWidth : config.Width;
            float height = type.UseCustomHeight ? type.CustomHeight : config.Height;
            
            string panelName = LAYER_NAME;
            string anchorSide;
            string offsetMin;
            string offsetMax;

            if (config.ShowAtTopRight) 
            {
                anchorSide = "1 1";
                offsetMin = $"{-width - config.XMargin} {config.YStartPosition - height}";
                offsetMax = $"{-config.XMargin} {config.YStartPosition}";
            }
            else 
            {
                anchorSide = "0.5 1";
                offsetMin = $"{-(width / 2)} {config.YStartPosition - height}";
                offsetMax = $"{width / 2} {config.YStartPosition}";
            }
            
            container.Add(new CuiPanel
            {
                RectTransform = 
                {
                    AnchorMin = anchorSide,
                    AnchorMax = anchorSide,
                    OffsetMin = offsetMin,
                    OffsetMax = offsetMax
                },
                Image = 
                { 
                    Color = type.BackgroundColor,
                    Material = "assets/content/ui/ui.background.transparent.radial.mat",
                    FadeIn = type.FadeIn
                },
                FadeOut = type.FadeOut
            }, "Overlay", panelName);
            
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.981", AnchorMax = "1 1" }, 
                Image = { Color = type.BorderColor }
            }, panelName);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0" },
                Image = { Color = type.BorderColor }
            }, panelName);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0" }, 
                Image = { Color = type.BorderColor }
            }, panelName);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "0 1" }, 
                Image = { Color = type.BorderColor }
            }, panelName);
           
            container.Add(new CuiPanel
            {
                RectTransform = { 
                    AnchorMin = type.IconSettings.AnchorMin, 
                    AnchorMax = type.IconSettings.AnchorMax
                },
                Image = { 
                    Color = type.IconColor, 
                    Material = "assets/content/ui/ui.background.transparent.radial.mat" 
                }
            }, panelName, panelName + ".IconBg");
            
            container.Add(new CuiElement
            {
                Parent = panelName + ".IconBg",
                Components =
                {
                    new CuiTextComponent 
                    { 
                        Text = type.IconText, 
                        FontSize = type.IconSettings.FontSize,
                        Font = type.IconSettings.IsBold ? "robotocondensed-bold.ttf" : "robotocondensed-regular.ttf",
                        Align = type.IconSettings.Align, 
                        Color = type.IconSettings.Color
                    },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = panelName,
                Components =
                {
                    new CuiTextComponent 
                    { 
                        Text = GetMsg(type.TitleKey, player.UserIDString), 
                        FontSize = type.TitleSettings.FontSize, 
                        Font = type.TitleSettings.IsBold ? "robotocondensed-bold.ttf" : "robotocondensed-regular.ttf",
                        Align = type.TitleSettings.Align, 
                        Color = "0 0 0 0.5" 
                    },
                    new CuiRectTransformComponent { 
                        AnchorMin = type.TitleSettings.AnchorMin, 
                        AnchorMax = type.TitleSettings.AnchorMax
                    }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = panelName,
                Components =
                {
                    new CuiTextComponent 
                    { 
                        Text = GetMsg(type.TitleKey, player.UserIDString), 
                        FontSize = type.TitleSettings.FontSize, 
                        Font = type.TitleSettings.IsBold ? "robotocondensed-bold.ttf" : "robotocondensed-regular.ttf", 
                        Align = type.TitleSettings.Align, 
                        Color = type.TitleSettings.Color
                    },
                    new CuiRectTransformComponent { 
                        AnchorMin = type.TitleSettings.AnchorMin, 
                        AnchorMax = type.TitleSettings.AnchorMax,
                        OffsetMin = "0 1",
                        OffsetMax = "0 1"
                    }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = panelName,
                Components =
                {
                    new CuiTextComponent 
                    { 
                        Text = message, 
                        FontSize = type.MessageSettings.FontSize, 
                        Font = type.MessageSettings.IsBold ? "robotocondensed-bold.ttf" : "robotocondensed-regular.ttf",
                        Align = type.MessageSettings.Align, 
                        Color = "0 0 0 0.5" 
                    },
                    new CuiRectTransformComponent { 
                        AnchorMin = type.MessageSettings.AnchorMin, 
                        AnchorMax = type.MessageSettings.AnchorMax
                    }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = panelName,
                Components =
                {
                    new CuiTextComponent 
                    { 
                        Text = message, 
                        FontSize = type.MessageSettings.FontSize, 
                        Font = type.MessageSettings.IsBold ? "robotocondensed-bold.ttf" : "robotocondensed-regular.ttf",
                        Align = type.MessageSettings.Align, 
                        Color = type.MessageSettings.Color
                    },
                    new CuiRectTransformComponent { 
                        AnchorMin = type.MessageSettings.AnchorMin, 
                        AnchorMax = type.MessageSettings.AnchorMax,
                        OffsetMin = "0 1",
                        OffsetMax = "0 1"
                    }
                }
            });
            
            if (type.UseClickCommand && !string.IsNullOrEmpty(type.ClickCommand))
            {
                var button = new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = type.ClickCommand },
                    Text = { Text = "" }
                };
                
                if (type.CloseAfterCommand)
                {
                    button.Button.Close = panelName;
                }
                
                container.Add(button, panelName);
            }
            
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.02", AnchorMax = "1 0.039" }, 
                Image = { Color = type.BorderColor }
            }, panelName, "TimerBar");
            
            CuiHelper.AddUi(player, container);
            
            float duration = type.UseCustomDuration ? type.CustomDuration : config.DefaultDuration;
            
            if (duration > 0.5f)
            {
                float stepTime = 0.1f;
                float steps = duration / stepTime;
                int currentStep = 0;
                
                notificationTimers[player.userID] = timer.Repeat(stepTime, (int)steps, () => {
                    if (player == null || !player.IsConnected) return;
                    
                    currentStep++;
                    float progress = 1f - (currentStep / steps);
                    
                    var timerContainer = new CuiElementContainer();
                    CuiHelper.DestroyUi(player, "TimerBar");
                    
                    timerContainer.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = $"0 0.02", AnchorMax = $"{progress} 0.039" },
                        Image = { Color = type.BorderColor }
                    }, panelName, "TimerBar");
                    
                    CuiHelper.AddUi(player, timerContainer);
                    
                    if (currentStep >= steps)
                    {
                        CuiHelper.DestroyUi(player, LAYER_NAME);
                        notificationTimers.Remove(player.userID);
                    }
                });
            }
            else
            {
                notificationTimers[player.userID] = timer.Once(duration, () => {
                    if (player != null && player.IsConnected)
                    {
                        CuiHelper.DestroyUi(player, LAYER_NAME);
                    }
                    notificationTimers.Remove(player.userID);
                });
            }
        }
        
        private void PlayEffect(BasePlayer player, string effect)
        {
            if (player == null || string.IsNullOrEmpty(effect)) return;
            
            Effect.server.Run(effect, player.transform.position);
        }

        [ConsoleCommand("notify.close")]
        private void CloseNotification(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            CuiHelper.DestroyUi(player, LAYER_NAME);
            
            if (notificationTimers.TryGetValue(player.userID, out var existingTimer))
            {
                existingTimer?.Destroy();
                notificationTimers.Remove(player.userID);
            }
        }

        // Kompatibilitätsbefehl für MyNotify
        [ConsoleCommand("mynotify.close")]
        private void CloseNotificationOriginal(ConsoleSystem.Arg arg)
        {
            CloseNotification(arg);
        }

        #endregion

        #region NotificationManager Klasse

        private class NotificationManager
        {
            private readonly BasePlayer player;
            
            public NotificationManager(BasePlayer player)
            {
                this.player = player;
            }
            
            public void Destroy()
            {
                if (player != null && player.IsConnected)
                {
                    CuiHelper.DestroyUi(player, LAYER_NAME);
                }
                
                if (instance.notificationTimers.TryGetValue(player.userID, out var timer))
                {
                    timer?.Destroy();
                    instance.notificationTimers.Remove(player.userID);
                }
                
                instance.playerNotifications.Remove(player.userID);
            }
        }

        #endregion

        #region Multilingual

                private Dictionary<string, string> GetDefaultMessages()
                {
                    return new Dictionary<string, string>
                    {
                        ["Notification"] = "Notification",
                        ["Error"] = "Error",
                        ["Success"] = "Success",
                        ["Warning"] = "Warning",
                        ["Event"] = "Event",
                        ["NoPermission"] = "You don't have permission to use this command.",
                        ["SyntaxNotify"] = "Syntax: /{0} [type] [message]",
                        ["SyntaxPlayerNotify"] = "Syntax: /{0} [playerID] [type] [message]",
                        ["SyntaxAllPlayerNotify"] = "Syntax: /{0} [type] [message]",
                        ["PlayerNotFound"] = "Player '{0}' not found!"
                    };
                }

                protected override void LoadDefaultMessages()
                {
                    lang.RegisterMessages(GetDefaultMessages(), this);

                    // German
                    lang.RegisterMessages(new Dictionary<string, string>
                    {
                        ["Notification"] = "Benachrichtigung",
                        ["Error"] = "Fehler",
                        ["Success"] = "Erfolg",
                        ["Warning"] = "Warnung",
                        ["Event"] = "Event",
                        ["NoPermission"] = "Sie haben keine Berechtigung, diesen Befehl zu verwenden.",
                        ["SyntaxNotify"] = "Syntax: /{0} [Typ] [Nachricht]",
                        ["SyntaxPlayerNotify"] = "Syntax: /{0} [SpielerID] [Typ] [Nachricht]",
                        ["SyntaxAllPlayerNotify"] = "Syntax: /{0} [Typ] [Nachricht]",
                        ["PlayerNotFound"] = "Spieler '{0}' wurde nicht gefunden!"
                    }, this, "de");

                    // Russian
                    lang.RegisterMessages(new Dictionary<string, string>
                    {
                        ["Notification"] = "Уведомление",
                        ["Error"] = "Ошибка",
                        ["Success"] = "Успех",
                        ["Warning"] = "Предупреждение",
                        ["Event"] = "Событие",
                        ["NoPermission"] = "У вас нет разрешения использовать эту команду.",
                        ["SyntaxNotify"] = "Синтаксис: /{0} [тип] [сообщение]",
                        ["SyntaxPlayerNotify"] = "Синтаксис: /{0} [идентификатор игрока] [тип] [сообщение]",
                        ["SyntaxAllPlayerNotify"] = "Синтаксис: /{0} [тип] [сообщение]",
                        ["PlayerNotFound"] = "Игрок '{0}' не найден!"
                    }, this, "ru");

                    // Italian
                    lang.RegisterMessages(new Dictionary<string, string>
                    {
                        ["Notification"] = "Notifica",
                        ["Error"] = "Errore",
                        ["Success"] = "Successo",
                        ["Warning"] = "Avviso",
                        ["Event"] = "Evento",
                        ["NoPermission"] = "Non hai il permesso di usare questo comando.",
                        ["SyntaxNotify"] = "Sintassi: /{0} [tipo] [messaggio]",
                        ["SyntaxPlayerNotify"] = "Sintassi: /{0} [IDgiocatore] [tipo] [messaggio]",
                        ["SyntaxAllPlayerNotify"] = "Sintassi: /{0} [tipo] [messaggio]",
                        ["PlayerNotFound"] = "Giocatore '{0}' non trovato!"
                    }, this, "it");

                    // Dutch
                    lang.RegisterMessages(new Dictionary<string, string>
                    {
                        ["Notification"] = "Kennisgeving",
                        ["Error"] = "Fout",
                        ["Success"] = "Succes",
                        ["Warning"] = "Waarschuwing",
                        ["Event"] = "Gebeurtenis",
                        ["NoPermission"] = "Je hebt geen toestemming om dit commando te gebruiken.",
                        ["SyntaxNotify"] = "Syntax: /{0} [type] [bericht]",
                        ["SyntaxPlayerNotify"] = "Syntax: /{0} [spelerID] [type] [bericht]",
                        ["SyntaxAllPlayerNotify"] = "Syntax: /{0} [type] [bericht]",
                        ["PlayerNotFound"] = "Speler '{0}' niet gevonden!"
                    }, this, "nl");

                    // Polish
                    lang.RegisterMessages(new Dictionary<string, string>
                    {
                        ["Notification"] = "Powiadomienie",
                        ["Error"] = "Błąd",
                        ["Success"] = "Sukces",
                        ["Warning"] = "Ostrzeżenie",
                        ["Event"] = "Wydarzenie",
                        ["NoPermission"] = "Nie masz uprawnień do użycia tego polecenia.",
                        ["SyntaxNotify"] = "Składnia: /{0} [typ] [wiadomość]",
                        ["SyntaxPlayerNotify"] = "Składnia: /{0} [IDgracza] [typ] [wiadomość]",
                        ["SyntaxAllPlayerNotify"] = "Składnia: /{0} [typ] [wiadomość]",
                        ["PlayerNotFound"] = "Gracz '{0}' nie został znaleziony!"
                    }, this, "pl");

                    // Portuguese
                    lang.RegisterMessages(new Dictionary<string, string>
                    {
                        ["Notification"] = "Notificação",
                        ["Error"] = "Erro",
                        ["Success"] = "Sucesso",
                        ["Warning"] = "Aviso",
                        ["Event"] = "Evento",
                        ["NoPermission"] = "Você não tem permissão para usar este comando.",
                        ["SyntaxNotify"] = "Sintaxe: /{0} [tipo] [mensagem]",
                        ["SyntaxPlayerNotify"] = "Sintaxe: /{0} [IDjogador] [tipo] [mensagem]",
                        ["SyntaxAllPlayerNotify"] = "Sintaxe: /{0} [tipo] [mensagem]",
                        ["PlayerNotFound"] = "Jogador '{0}' não encontrado!"
                    }, this, "pt");

                    // Swedish
                    lang.RegisterMessages(new Dictionary<string, string>
                    {
                        ["Notification"] = "Avisering",
                        ["Error"] = "Fel",
                        ["Success"] = "Framgång",
                        ["Warning"] = "Varning",
                        ["Event"] = "Händelse",
                        ["NoPermission"] = "Du har inte behörighet att använda detta kommando.",
                        ["SyntaxNotify"] = "Syntax: /{0} [typ] [meddelande]",
                        ["SyntaxPlayerNotify"] = "Syntax: /{0} [spelarID] [typ] [meddelande]",
                        ["SyntaxAllPlayerNotify"] = "Syntax: /{0} [typ] [meddelande]",
                        ["PlayerNotFound"] = "Spelare '{0}' hittades inte!"
                    }, this, "sv");

                    // Turkish
                    lang.RegisterMessages(new Dictionary<string, string>
                    {
                        ["Notification"] = "Bildirim",
                        ["Error"] = "Hata",
                        ["Success"] = "Başarı",
                        ["Warning"] = "Uyarı",
                        ["Event"] = "Etkinlik",
                        ["NoPermission"] = "Bu komutu kullanma izniniz yok.",
                        ["SyntaxNotify"] = "Sözdizimi: /{0} [tip] [mesaj]",
                        ["SyntaxPlayerNotify"] = "Sözdizimi: /{0} [oyuncuID] [tip] [mesaj]",
                        ["SyntaxAllPlayerNotify"] = "Sözdizimi: /{0} [tip] [mesaj]",
                        ["PlayerNotFound"] = "Oyuncu '{0}' bulunamadı!"
                    }, this, "tr");

                    // Ukrainian
                    lang.RegisterMessages(new Dictionary<string, string>
                    {
                        ["Notification"] = "Сповіщення",
                        ["Error"] = "Помилка",
                        ["Success"] = "Успіх",
                        ["Warning"] = "Попередження",
                        ["Event"] = "Подія",
                        ["NoPermission"] = "У вас немає дозволу на використання цієї команди.",
                        ["SyntaxNotify"] = "Синтаксис: /{0} [тип] [повідомлення]",
                        ["SyntaxPlayerNotify"] = "Синтаксис: /{0} [IDгравця] [тип] [повідомлення]",
                        ["SyntaxAllPlayerNotify"] = "Синтаксис: /{0} [тип] [повідомлення]",
                        ["PlayerNotFound"] = "Гравця '{0}' не знайдено!"
                    }, this, "uk");

                    // Chinese (Simplified)
                    lang.RegisterMessages(new Dictionary<string, string>
                    {
                        ["Notification"] = "通知",
                        ["Error"] = "错误",
                        ["Success"] = "成功",
                        ["Warning"] = "警告",
                        ["Event"] = "事件",
                        ["NoPermission"] = "您没有权限使用此命令。",
                        ["SyntaxNotify"] = "语法：/{0} [类型] [消息]",
                        ["SyntaxPlayerNotify"] = "语法：/{0} [玩家ID] [类型] [消息]",
                        ["SyntaxAllPlayerNotify"] = "语法：/{0} [类型] [消息]",
                        ["PlayerNotFound"] = "未找到玩家 '{0}'！"
                    }, this, "zh-cn");

                    // Chinese (Traditional)
                    lang.RegisterMessages(new Dictionary<string, string>
                    {
                        ["Notification"] = "通知",
                        ["Error"] = "錯誤",
                        ["Success"] = "成功",
                        ["Warning"] = "警告",
                        ["Event"] = "事件",
                        ["NoPermission"] = "您沒有權限使用此命令。",
                        ["SyntaxNotify"] = "語法：/{0} [類型] [訊息]",
                        ["SyntaxPlayerNotify"] = "語法：/{0} [玩家ID] [類型] [訊息]",
                        ["SyntaxAllPlayerNotify"] = "語法：/{0} [類型] [訊息]",
                        ["PlayerNotFound"] = "找不到玩家 '{0}'！"
                    }, this, "zh-tw");

                    // Korean
                    lang.RegisterMessages(new Dictionary<string, string>
                    {
                        ["Notification"] = "알림",
                        ["Error"] = "오류",
                        ["Success"] = "성공",
                        ["Warning"] = "경고",
                        ["Event"] = "이벤트",
                        ["NoPermission"] = "이 명령을 사용할 권한이 없습니다.",
                        ["SyntaxNotify"] = "구문: /{0} [유형] [메시지]",
                        ["SyntaxPlayerNotify"] = "구문: /{0} [플레이어ID] [유형] [메시지]",
                        ["SyntaxAllPlayerNotify"] = "구문: /{0} [유형] [메시지]",
                        ["PlayerNotFound"] = "플레이어 '{0}'를 찾을 수 없습니다!"
                    }, this, "ko");
                }

                private string GetMsg(string key, string userId = null)
                {
                    return lang.GetMessage(key, this, userId);
                }

        #endregion
    }
}