using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Medal", "Kira", "1.0.8")]
    [Description("Uniq medal for Rust server")]
    public class Medal : RustPlugin
    {
        #region [Vars]

#pragma warning disable CS0649
        [PluginReference] private Plugin ImageLibrary;
#pragma warning restore CS0649
        private static Medal _;
        private MedalCore medal;
        private WipeCore wipe;
        private StoredData _dataBase = new StoredData();
        private bool IsWipe;
        private const string GenericPrefab = "assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab";
        private const string UIMain = "UI.Medal";

        #endregion

        #region [Configuration] / [Конфигурация]

        private static ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "Medal [Configuration]")]
            public MedalConfig MedalCfg = new MedalConfig();

            public class MedalConfig
            {
                [JsonProperty(PropertyName = "Название медали")]
                public string MarkerName;

                [JsonProperty(PropertyName = "SkinID медали")]
                public ulong SkinID;

                [JsonProperty(PropertyName = "Задний фон уведомления о победителе (URL)")]
                public string Image;

                [JsonProperty(PropertyName = "Цвет текста (HEX)")]
                public string Color;

                [JsonProperty(PropertyName = "Размер текста (HEX)")]
                public int Size;

                [JsonProperty(PropertyName = "Частота обновления (От данной настрокий зависит нагрузка)")]
                public float UpdateTime;

                [JsonProperty(PropertyName = "Вайп (День недели от 1 до 7)")]
                public DayOfWeek WipeDay;

                [JsonProperty(PropertyName = "Вайп (Время дня от 0 до 24)")]
                public int WipeHour;

                [JsonProperty(PropertyName = "Автовайп (Вкл/Выкл)")]
                public bool AutoWipe;

                [JsonProperty(PropertyName = "Discord WebHook")]
                public string DiscordHook;
            }
        }

        private static ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                MedalCfg = new ConfigData.MedalConfig
                {
                    MarkerName = "Medal",
                    SkinID = 1,
                    Image = "https://i.imgur.com/TaPCCcP.png",
                    Size = 15,
                    Color = "#3b3d45",
                    UpdateTime = 60,
                    WipeDay = DayOfWeek.Tuesday,
                    WipeHour = 20,
                    AutoWipe = true
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
            PrintError("The config file is corrupted (or does not exist), a new one was created!");
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region [Lang]

        protected override void LoadDefaultMessages()
        {
            var ru = new Dictionary<string, string>
            {
                ["WINNER"] = "Вы выиграли",
                ["MEDAL_PICKUP"] = "Медаль была найдена !!",
                ["MEDAL_PUT_IN_CUP"] = "Медаль положена в шкаф на базе!!!",
                ["MEDAL_PICKUP_CUP"] = "Медаль изъята из шкафа базы"
            };

            var en = new Dictionary<string, string>
            {
                ["WINNER"] = "You WIN",
                ["MEDAL_PICKUP"] = "Медаль была найдена !!",
                ["MEDAL_PUT_IN_CUP"] = "Медаль положена в шкаф на базе!!!",
                ["MEDAL_PICKUP_CUP"] = "Медаль изъята из шкафа базы"
            };
            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        #endregion

        #region [MonoBehaviour]

        public class WipeCore : MonoBehaviour
        {
            private void Awake()
            {
                InvokeRepeating(nameof(Timer), 1f, 1f);
            }

            public void Timer()
            {
                if (DateTime.Now.DayOfWeek != _config.MedalCfg.WipeDay) return;
                if (DateTime.Now.Hour != _config.MedalCfg.WipeHour) return;
                if (_.IsWipe) return;
                _.IsWipe = true;
                _.Wipe();
            }
        }

        public class MedalCore : MonoBehaviour
        {
            public BaseEntity Container;
            public BuildingPrivlidge Cupboard;
            private VendingMachineMapMarker Marker;
            private Dictionary<ulong, int> Players = new Dictionary<ulong, int>();
            public Vector3 Position = _._dataBase.CurrentPos;
            public ulong Owner;
            public int Time;

            private void Start()
            {
                if (_._dataBase.MedalList != null | _._dataBase.MedalList.Count > 1) Players = _._dataBase.MedalList;
                if (_._dataBase.CurrentOwner != 0)
                {
                    var ent = (BaseEntity) BaseNetworkable.serverEntities.entityList[_._dataBase.CurrentOwner];
                    SwitchOwner(ent);
                }

                InvokeRepeating(nameof(Timer), _config.MedalCfg.UpdateTime, _config.MedalCfg.UpdateTime);
            }

            private void CreateMarker()
            {
                Marker = GameManager.server.CreateEntity(GenericPrefab, Position)
                    .GetComponent<VendingMachineMapMarker>();
                Marker.markerShopName = _config.MedalCfg.MarkerName;
                Marker.enableSaving = false;
                Marker.Spawn();
            }

            public void CheckInList(ulong player)  
            {
                if (player == 0) return;
                if (Players.ContainsKey(player)) return;
                // if (Players.Count < 1)
                //     foreach (var obj in BasePlayer.activePlayerList)
                //         obj.ChatMessage(_.lang.GetMessage("MEDAL_PICKUP", _, obj.UserIDString));
                Players.Add(player, Time);
            }

            private void UpdateMarker()
            {
                if (Container == null) return;
                Marker.transform.position = Container.GetNetworkPosition();
                Marker.SendNetworkUpdate();
            }

            public void SwitchOwner(BaseEntity ent)
            {
                if (ent == null) return;
                Owner = ent.ToPlayer() != null ? ((BasePlayer) ent).userID : ent.OwnerID;
                if (Owner == 0) return;
                Position = _._dataBase.CurrentPos;
                // var admin = BasePlayer.FindByID(Owner);
                // if (admin != null)
                //     if (admin.IsAdmin)
                //         return;
                CheckInList(Owner);
                if (Marker == null) CreateMarker();
                Container = ent;
                Time = Players[Owner];
                _._dataBase.CurrentOwner = ent.net.ID;
                _._dataBase.CurrentPos = ent.GetNetworkPosition();

                _.SaveData();
            }

            public void Timer()
            {
                if (Owner == 0) return;
                if (Container == null) return;
                CheckInList(Owner);
                UpdateMarker();
                if (!(Container is BuildingPrivlidge)) return;
                Time++;
                Cupboard = Container as BuildingPrivlidge;
                foreach (var player in Cupboard.authorizedPlayers)
                {
                    CheckInList(player.userid);
                    Players[player.userid] = Time;
                }
            }

            private void OnDestroy()
            {
                _._dataBase.MedalList = Players;
                if (Marker != null) Marker.Kill();
            }
        }

        #endregion

        #region [DrawUI] / [Отрисовка UI]

        private void DrawUI_Alert(BasePlayer player)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = true,
                        Image =
                        {
                            Color = "0 0 0 0"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    },
                    "Overlay", UIMain
                },
                new CuiElement
                {
                    Name = $"{UIMain}.Background",
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage($"{UIMain}.Background")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3666667 0.3814815",
                            AnchorMax = "0.6333333 0.6185185"
                        }
                    }
                }
            };


            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = HexToRustFormat(_config.MedalCfg.Color),
                    FadeIn = 1f,
                    FontSize = _config.MedalCfg.Size,
                    Text = lang.GetMessage("WINNER", this, player.UserIDString)
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, $"{UIMain}.Background");

            ui.Add(new CuiButton
            {
                Button =
                {
                    Close = UIMain,
                    Color = "0 0 0 0"
                },
                Text = {Text = " "},
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, UIMain);

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Hooks]

        // ReSharper disable once UnusedMember.Local
        private void OnServerSave()
        {
            timer.Once(5f, SaveData);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            LoadData();
            ImageLibrary.Call("AddImage", _config.MedalCfg.Image, $"{UIMain}.Background");
            _ = this;
            medal = ServerMgr.Instance.gameObject.AddComponent<MedalCore>();
            if (_config.MedalCfg.AutoWipe) wipe = ServerMgr.Instance.gameObject.AddComponent<WipeCore>();
        }

        // ReSharper disable once UnusedMember.Local  
        private void Unload()
        {
            SaveData();
            if (medal != null) UnityEngine.Object.Destroy(medal);
            if (wipe != null) UnityEngine.Object.Destroy(wipe);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (item.skin != _config.MedalCfg.SkinID) return;
            if (container.entityOwner == null) medal.SwitchOwner(container.playerOwner);
            if (container.playerOwner != null) return;
            medal.SwitchOwner(container.entityOwner);
            if (!(container.entityOwner is BuildingPrivlidge)) return;
            foreach (var player in BasePlayer.activePlayerList)
                player.ChatMessage(lang.GetMessage("MEDAL_PUT_IN_CUP", this, player.UserIDString));
            
        }

        // ReSharper disable once UnusedMember.Local
        private object CanMoveItem(Item item)
        {
            if (item.skin != _config.MedalCfg.SkinID) return null;
            if (item.GetRootContainer() == null) return null;
            if (!(item.GetRootContainer().entityOwner is BuildingPrivlidge)) return null;
            foreach (var player in BasePlayer.activePlayerList)
                player.ChatMessage(lang.GetMessage("MEDAL_PICKUP_CUP", this, player.UserIDString));
            return null;
        }

        // ReSharper disable once UnusedMember.Local
        private void OnItemDropped(Item item, BaseEntity entity)
        {
            if (item.skin != _config.MedalCfg.SkinID) return;
            if (entity == null) return;
            medal.SwitchOwner(entity);
        }

        private string GetImage(string name)
        {
            return (string) ImageLibrary.Call("GetImage", name);
        }

        #endregion

        #region [Commands]

        [ChatCommand("medal")]
        // ReSharper disable once UnusedMember.Local
        private void GiveMedal(BasePlayer player)
        {
            if (!player.IsAdmin) return;
            var item = ItemManager.CreateByName("dogtagneutral");
            item.skin = _config.MedalCfg.SkinID;
            player.GiveItem(item);
        }

        #endregion

        #region [DataBase] / [База данных]

        private void Wipe()
        {
            if (_dataBase.MedalList.Count > 0) return;
            var winner = _dataBase.MedalList.OrderBy(x => x.Value).ToList();
            if (winner.Count < 1) return;
            var id = winner[0].Key;
            _dataBase.MedalList.Clear();
            _dataBase.CurrentOwner = 0;
            SaveData();
            var player = BasePlayer.FindByID(id);
            if (player != null) DrawUI_Alert(player);
            SendDiscordMessage(id.ToString());
            PrintWarning($"Winner : {id}");
        }

        private class StoredData
        {
            public uint CurrentOwner;
            public Vector3 CurrentPos;
            public Dictionary<ulong, int> MedalList = new Dictionary<ulong, int>();
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

        #region [Helpers] / [Вспомогательный код]

        private class DiscordMessage
        {
            public DiscordMessage(string content, params Embed[] embeds)
            {
                Content = content;
                Embeds = embeds.ToList();
            }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
            [JsonProperty("content")] private string Content { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
            [JsonProperty("embeds")] private List<Embed> Embeds { get; set; }

            public string ToJson()
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        private class Embed
        {
            // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
            // ReSharper disable once CollectionNeverQueried.Local
            [JsonProperty("fields")] private List<Field> Fields { get; set; } = new List<Field>();

            public Embed AddField(string name, string value, bool inline)
            {
                Fields.Add(new Field(name, Regex.Replace(value, "<.*?>", string.Empty), inline));

                return this;
            }
        }

        private class Field
        {
            public Field(string name, string value, bool inline)
            {
                Name = name;
                Value = value;
                Inline = inline;
            }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
            [JsonProperty("name")] private string Name { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
            [JsonProperty("value")] private string Value { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
            [JsonProperty("inline")] private bool Inline { get; set; }
        }

        private void SendDiscordMessage(string id)
        {
            var embed = new Embed()
                .AddField("Winner ID:", id, true);

            webrequest.Enqueue(_config.MedalCfg.DiscordHook, new DiscordMessage("", embed).ToJson(),
                (code, response) => { },
                this,
                RequestMethod.POST, new Dictionary<string, string>
                {
                    {"Content-Type", "application/json"}
                });
        }

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');
            if (str.Length == 6)
                str += "FF";
            if (str.Length != 8)
            {
                throw new Exception(hex);
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        #endregion
    }
}