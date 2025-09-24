using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("WipeReward", "whiteplugins.ru", "1.0.2")]
    [Description("Награда первым N игрокам после вайпа")]
    public class WipeReward : RustPlugin
    {
        #region Data
        public Dictionary<ulong, DateTime> RewardsList = new Dictionary<ulong, DateTime>();
        void LoadData()
        {
            try
            {
                RewardsList = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, DateTime>>($"{Title}_Players");
                if (RewardsList == null)
                    RewardsList = new Dictionary<ulong, DateTime>();
            }
            catch
            {
                RewardsList = new Dictionary<ulong, DateTime>();
            }
        }

        void SaveData()
        {
            if (RewardsList != null)
                Interface.Oxide.DataFileSystem.WriteObject($"{Title}_Players", RewardsList);
        }

        void Unload()
        {
            SaveData();
        }
        #endregion


        #region Config
        public Configurarion config;

        public class Setings
        {
            [JsonProperty("Количество игроков")]
            public int PlayersIntConnect;
            [JsonProperty("Команда для выдачи приза (если не нужно то оставить поля пустым)")]
            public string CommandPrize;
            [JsonProperty("У вас магазин ОВХ?")]
            public bool OVHStore;
            [JsonProperty("Бонус в виде баланса GameStores или OVH (если не нужно оставить пустым)")]
            public string GameStoreBonus;
            [JsonProperty("Лог сообщения(Показывается в магазине после выдачи в истории. Если OVH оставить пустым)")]
            public string GameStoreMSG;
            [JsonProperty("Id Магазина(GameStore. Если OVH оставить пустым)")]
            public string Store_Id;
            [JsonProperty("API KEY Магазина(GameStore. Если OVH оставить пустым)")]
            public string Store_Key;
        }

        public class Configurarion
        {
            [JsonProperty("Настройки")]
            public Setings setings;
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configurarion()
            {
                setings = new Setings
                {
                    PlayersIntConnect = 100,
                    CommandPrize = "say %STEAMID%",
                    OVHStore = false,
                    GameStoreBonus = "",
                    GameStoreMSG = "За заход после вайпа:3",
                    Store_Id = "ID",
                    Store_Key = "KEY"

                }
            };
            SaveConfig(config);
        }

        void SaveConfig(Configurarion config)
        {
            Config.WriteObject(config, true);
            SaveConfig();
        }

        public void LoadConfigVars()
        {
            config = Config.ReadObject<Configurarion>();
            Config.WriteObject(config, true);
        }
        #endregion

        private void OnServerInitialized()
        {
            LoadConfigVars();
            if (!string.IsNullOrEmpty(config.setings.GameStoreBonus) && !config.setings.OVHStore)
            {
                if (config.setings.Store_Id == "ID" || config.setings.Store_Key == "KEY")
                {
                    PrintError("Вы не настроили ID И KEY от магазина GameStores");
                    Interface.Oxide.UnloadPlugin(Title);
                    return;
                }
            }
            LoadData();
            BasePlayer.activePlayerList.ForEach(OnPlayerInit);
        }

        void OnNewSave(string filename)
        {
            LoadData();
            RewardsList.Clear();
            SaveData();
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerInit(player));
                return;
            }
            if (RewardsList.Count < config.setings.PlayersIntConnect && !RewardsList.ContainsKey(player.userID))
            {
                RewardsList.Add(player.userID, DateTime.Now);
                WipeRewardGui(player);
            }
        }

        [ConsoleCommand("giveprize")]
        void GivePrize(ConsoleSystem.Arg arg)
        {
            BasePlayer p = arg.Player();
            CuiHelper.DestroyUi(p, WipeR);

            if (!string.IsNullOrEmpty(config.setings.CommandPrize))
            {
                Server.Command(config.setings.CommandPrize.Replace("%STEAMID%", p.UserIDString));
            }
            if (!string.IsNullOrEmpty(config.setings.GameStoreBonus))
            {
                GiveReward(p.userID);
            }
            SendReply(p, "Вы успешно <color=#A1FF919A>забрали награду</color>!");
        }

        void GiveReward(ulong ID)
        {
            if (!config.setings.OVHStore)
            {
                string url = $"https://gamestores.ru/api?shop_id={config.setings.Store_Id}&secret={config.setings.Store_Key}&action=moneys&type=plus&steam_id={ID}&amount={config.setings.GameStoreBonus}&mess={config.setings.GameStoreMSG}";
                webrequest.Enqueue(url, null, (i, s) =>
                {
                    if (i != 200) { }
                    if (s.Contains("success"))
                    {
                        PrintWarning($"Игрок [{ID}] зашел 1 из первых, и получил бонус в нашем магазине. В виде [{config.setings.GameStoreBonus} руб]");
                    }
                    else
                    {
                        PrintWarning($"Игрок {ID} проголосовал за сервер, но не авторизован в магазине.");
                    }
                }, this);
            }
            else
            {
                plugins.Find("RustStore").CallHook("APIChangeUserBalance", ID, config.setings.GameStoreBonus, new Action<string>((result) =>
                {
                    if (result == "SUCCESS")
                    {
                        PrintWarning($"Игрок [{ID}] зашел 1 из первых, и получил бонус в нашем магазине. В виде [{config.setings.GameStoreBonus} руб]");
                        return;
                    }
                    PrintWarning($"Игрок {ID} проголосовал за сервер, но не авторизован в магазине. Ошибка: {result}");
                }));
            }
        }



        #region Parent
        public static string WipeR = "WipeR_CUI";
        #endregion

        #region GUI

        public void WipeRewardGui(BasePlayer p)
        {
            CuiHelper.DestroyUi(p, WipeR);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-175 -340", OffsetMax = "-1 -280" },
                Image = { Color = "0 0 0 0.4", Material = "assets/content/ui/uibackgroundblur.mat", Sprite = "assets/content/ui/ui.background.tiletex.psd" }
            }, "Overlay", WipeR);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.1973175 0.01111111", AnchorMax = "0.7988508 0.3999999" },
                Button = { Command = "giveprize", Color = HexToRGB("#71FF9A9A") },
                Text = { Text = "Забрать награду", Align = TextAnchor.MiddleCenter, FontSize = 13 }
            }, WipeR);

            #region Title
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.4444442", AnchorMax = "1 1" },
                Text = { Text = $"Вы {RewardsList.Count} из {config.setings.PlayersIntConnect}\n Поэтому получаете награду", FontSize = 13, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }

            }, WipeR);

            #endregion

            CuiHelper.AddUi(p, container);
        }

        #endregion

        #region Help

        private static string HexToRGB(string hex)
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
                throw new InvalidOperationException("Cannot convert a wrong format #181");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        public void ReplyWithHelper(BasePlayer player, string message, string[] args = null)
        {
            if (args != null)
                message = string.Format(message, args);
            player.SendConsoleCommand("chat.add", new object[2]
            {
                76561198283599982,
                string.Format("<size=16><color={2}>{0}</color>:</size>\n{1}", "WipeReward", message, "#3B85F5B1")
            });
        }

        #endregion

    }
}