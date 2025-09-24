using System;
using System.Collections.Generic;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{ 
    [Info("AlertSystem", "Kira", "1.0.0")]
    [Description("Система оповещений")]
    public class  AlertSystem : RustPlugin
    {
        #region [Vars]

        private const string UIMain = "UI.Alert";
        private const string Blur = "assets/content/ui/uibackgroundblur.mat";
        private StoredData _dataBase = new StoredData();

        #endregion

        #region [Classes]

        public class Alert
        {
            [JsonProperty(PropertyName = "ID")] public int ID;

            [JsonProperty(PropertyName = "Оповещение одноразовое ?")]
            public bool IsRepit;

            [JsonProperty(PropertyName = "Shortname")]
            public string Shortname;

            [JsonProperty(PropertyName = "SkinID")]
            public ulong SkinID;

            [JsonProperty(PropertyName = "Текст [RU]")]
            public string TextRU;

            [JsonProperty(PropertyName = "Текст [ENG]")]
            public string TextENG;

            [JsonProperty(PropertyName = "Список команд выполняемых при нажатии на оповещение")]
            public List<string> Command = new List<string>();
        }

        public class AlertEvent
        {
            [JsonProperty(PropertyName = "ID")] public int ID;

            [JsonProperty(PropertyName = "Оповещение одноразовое ?")]
            public bool IsRepit;

            [JsonProperty(PropertyName = "Текст [RU]")]
            public string TextRU;

            [JsonProperty(PropertyName = "Текст [ENG]")]
            public string TextENG;

            [JsonProperty(PropertyName = "Список команд выполняемых при нажатии на оповещение")]
            public List<string> Command = new List<string>();
        }

        #endregion 
  
        #region [Configuraton]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "MoonInfoMenu - Config")]
            public AlertCFG AlertSettings = new AlertCFG();

            public class AlertCFG
            {
                [JsonProperty(PropertyName = "Скорость появления (float)")]
                public float FadeIn;

                [JsonProperty(PropertyName = "Скорость исчезания (float)")]
                public float FadeOut;

                [JsonProperty(PropertyName = "Цвет оповещения (Rust)")]
                public string Color;

                [JsonProperty(PropertyName = "Список оповещений [Предметы]")]
                public List<Alert> Alerts;

                [JsonProperty(PropertyName = "Список оповещений [Events]")]
                public List<AlertEvent> AlertsEvents;
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                AlertSettings = new ConfigData.AlertCFG
                {
                    FadeIn = 1f,
                    FadeOut = 5f,
                    Color = "0.56 0.50 0.47 0.15",
                    Alerts = new List<Alert>
                    {
                        new Alert
                        {
                            ID = 0,
                            IsRepit = true,
                            Shortname = "ducttape",
                            SkinID = 2814895972,
                            TextRU = "Вы нашли метеоритную пыль, нажмите на оповещение чтобы получить информацию",
                            TextENG = "You have found meteor dust, click on the alert to get information",
                            Command = new List<string>
                            {
                                "chat.say OK"
                            }
                        }
                    },
                    AlertsEvents = new List<AlertEvent>
                    {
                        new AlertEvent
                        {
                            ID = 1,
                            IsRepit = true,
                            TextRU = "123123123",
                            TextENG = "123123123",
                            Command = new List<string>()
                        }
                    }
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Файл конфигурации поврежден (или не существует), создан новый!");
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion 

        #region [DrawUI]

        private void DrawUI_Alert(BasePlayer player, Alert alert)
        {
            var ui = new CuiElementContainer();
            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = _config.AlertSettings.Color,
                    Command = $"alert.send {alert.ID}",
                    Close = UIMain,
                    Material = Blur,
                    FadeIn = _config.AlertSettings.FadeIn
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1",
                    FontSize = 14,
                    FadeIn = _config.AlertSettings.FadeIn,
                    Text = lang.GetLanguage(player.UserIDString) == "en" ? alert.TextENG : alert.TextRU
                },
                RectTransform =
                {
                    AnchorMin = "0.009895824 0.5490741",
                    AnchorMax = "0.2166667 0.7240741"
                }
            }, "Overlay", UIMain);

            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0.81 0.18 0.04 0.9",
                    Close = UIMain,
                    FadeIn = _config.AlertSettings.FadeIn
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.8",
                    Text = "✖",
                    FontSize = 18,
                    FadeIn = _config.AlertSettings.FadeIn
                },
                RectTransform =
                {
                    AnchorMin = "0.8992443 0.7883595",
                    AnchorMax = "0.995 0.996"
                }
            }, UIMain);

            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0.23 0.74 0.18 0.7",
                    Close = UIMain,
                    Command = $"alert.send {alert.ID}",
                    FadeIn = _config.AlertSettings.FadeIn
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1",
                    FontSize = 14,
                    FadeIn = _config.AlertSettings.FadeIn,
                    Text = lang.GetLanguage(player.UserIDString) == "en" ? "GO" : "ПЕРЕЙТИ"
                },
                RectTransform =
                {
                    AnchorMin = "0.5692695 -0.2275134",
                    AnchorMax = "0.995 -0.005290791"
                }
            }, UIMain);

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
            timer.Once(_config.AlertSettings.FadeOut, () => CuiHelper.DestroyUi(player, UIMain));
        }

        private void DrawUI_AlertEvent(BasePlayer player, AlertEvent alert)
        {
            var ui = new CuiElementContainer();
            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = _config.AlertSettings.Color,
                    Command = $"alert.send {alert.ID}",
                    Close = UIMain,
                    Material = Blur,
                    FadeIn = _config.AlertSettings.FadeIn
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1",
                    FontSize = 14,
                    FadeIn = _config.AlertSettings.FadeIn,
                    Text = lang.GetLanguage(player.UserIDString) == "en" ? alert.TextENG : alert.TextRU
                },
                RectTransform =
                {
                    AnchorMin = "0.009895824 0.5490741",
                    AnchorMax = "0.2166667 0.7240741"
                }
            }, "Overlay", $"{UIMain}.Event");

            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0.81 0.18 0.04 0.9",
                    Close = $"{UIMain}.Event",
                    FadeIn = _config.AlertSettings.FadeIn
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.8",
                    Text = "✖",
                    FontSize = 18,
                    FadeIn = _config.AlertSettings.FadeIn
                },
                RectTransform =
                {
                    AnchorMin = "0.8992443 0.7883595",
                    AnchorMax = "0.995 0.996"
                }
            }, $"{UIMain}.Event");

            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0.23 0.74 0.18 0.7",
                    Close = $"{UIMain}.Event",
                    Command = $"alert.send {alert.ID}",
                    FadeIn = _config.AlertSettings.FadeIn
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1",
                    FontSize = 14,
                    FadeIn = _config.AlertSettings.FadeIn,
                    Text = lang.GetLanguage(player.UserIDString) == "en" ? "GO" : "ПЕРЕЙТИ"
                },
                RectTransform =
                {
                    AnchorMin = "0.5692695 -0.2275134",
                    AnchorMax = "0.995 -0.005290791"
                }
            }, $"{UIMain}.Event");

            CuiHelper.DestroyUi(player, $"{UIMain}.Event");
            CuiHelper.AddUi(player, ui);
            timer.Once(_config.AlertSettings.FadeOut, () => CuiHelper.DestroyUi(player, $"{UIMain}.Event"));
        }

        #endregion
  
        #region [Hooks]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            LoadData();
        }

        // ReSharper disable once UnusedMember.Local
        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (container == null | item == null) return;
            if (item.GetOwnerPlayer() == null) return;
            var player = item.GetOwnerPlayer();
            var alert = _config.AlertSettings.Alerts.Find(x =>
                x.Shortname == item.info.shortname
                & x.SkinID == item.skin);
            if (alert == null) return;
            if (!_dataBase.UsedAlerts.ContainsKey(player.userID))
                _dataBase.UsedAlerts.Add(player.userID, new List<int>());
            var db = _dataBase.UsedAlerts[player.userID];
            if (db.Contains(alert.ID))
                if (alert.IsRepit) DrawUI_Alert(player, alert);
                else return;
            DrawUI_Alert(player, alert);
            if (!db.Contains(alert.ID)) db.Add(alert.ID);
        }

        // ReSharper disable once UnusedMember.Local 
        private void AlertAll(int ID)
        {
            var alert = _config.AlertSettings.AlertsEvents.Find(f => f.ID == ID);
            if (alert == null) return;
            foreach (var player in BasePlayer.activePlayerList) DrawUI_AlertEvent(player, alert);
        }

        // ReSharper disable once UnusedMember.Local 
        private void Unload()
        {
            SaveData();
        }

        #endregion

        #region [Command]

        [ConsoleCommand("alert.send")]
        private void SendCommand(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            var alert = _config.AlertSettings.Alerts.Find(i => i.ID == args.Args[0].ToInt());
            if (alert == null) return;
            if (alert.Command == null) return;
            foreach (var obj in alert.Command) player.SendConsoleCommand(obj);
        }

        #endregion

        #region [DataBase]

        private class StoredData
        {
            public Dictionary<ulong, List<int>> UsedAlerts = new Dictionary<ulong, List<int>>();
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _dataBase);

        private void LoadData()
        {
            try
            {
                _dataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception)
            {
                _dataBase = new StoredData();
            }
        }

        #endregion
    }
}