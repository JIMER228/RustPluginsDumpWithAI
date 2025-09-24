using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using System.Globalization;
using UnityEngine;
using System.Linq;
using Oxide.Core.Plugins;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("PlayerGifts", "FuzeEffect", "1.0.1")]
    class PlayerGifts : RustPlugin
    {
        #region Reference
        Plugin ImageLibrary => Interface.Oxide.RootPluginManager.GetPlugin("ImageLibrary");
        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        #endregion

        #region Configuration
        private static Configuration config = new Configuration();

        private class Configuration
        {
            [JsonProperty("Сколько времени нужно отыграть игроку для получения награды (секунды)")]
            public ulong TimePrize = 300;

            internal class UISettings
            {
                internal class UIInventorySetting
                {
                    [JsonProperty("Текст в инвентаре")]
                    public string TitleInventory = "<size=30>Ваш инвентарь вещей за проведенное время на сервере</size>";
                    [JsonProperty("Описание в инвентаре")]
                    public string DescriptionInventory = "<size=18>Вы можете забрать вещи из инвентаря в любое время</size>";
                    [JsonProperty("Шрифт текста в инвентаре")]
                    public string Font = "robotocondensed-bold.ttf";
                    [JsonProperty("Цвет UI с сообщением")]
                    public string UIMsgColor = "#eb678a";
                }

                internal class UILogoSetting
                {
                    [JsonProperty("Не активная картинка(Будет показана если игрок не отыграл определенное время(ссылка)")]
                    public string InactivePng = "https://i.imgur.com/sxWUBY2.png";
                    [JsonProperty("Aктивная картинка(Будет показана если игрок  отыграл определенное время(ссылка)")]
                    public string ActivePng = "https://i.imgur.com/3BQUnKi.png";
                    [JsonProperty("AnchorMin для иконки(для опытных юзеров)")]
                    public string AnchorMin = "0.5 0.5";
                    [JsonProperty("AnchorMax для иконки(для опытных юзеров)")]
                    public string AnchorMax = "0.5 0.5";
                    [JsonProperty("OffsetMin для иконки(для опытных юзеров)")]
                    public string OffsetMin = "-260 -341";
                    [JsonProperty("OffsetMax для иконки(для опытных юзеров)")]
                    public string OffsetMax = "-200 -282";
                }
                [JsonProperty("Настройка UI инвентаря")]
                public UIInventorySetting UIInventorySettings = new UIInventorySetting();
                [JsonProperty("Настройка UI лого")]
                public UILogoSetting UILogoSettings = new UILogoSetting();
            }

            internal class StoreSettings
            {
                [JsonProperty("API от Магазина(Секретный ключ)")]
                public string SecretKeyStore = "SecretKey";
                [JsonProperty("ServerID в магазине")]
                public string ServerID = "ServerID";
                [JsonProperty("Сообщение при получении баланса(отображается в магазине)")]
                public string Messages = "Вы получили баланс за проведенное время на сервере!";
            }

            internal class Prize
            {
                [JsonProperty("Предмет из игры или команда. (Если вы ставите предмет из игры(Пример: rifle.ak) не заполняйте URL")]
                public string Value;
                [JsonProperty("Ссылка на фото для команды или денешки")]
                public string Url;
                [JsonProperty("Значение,сколько предметов вам дадут!(Если оставить Value пустым,выдадут баланс на GameStores)")]
                public int Amount;
            }

            [JsonProperty("Список предметов! Когда игрок отыграет определенное время на сервере,ему дадут 1 награду из списка.")]
            public List<Prize> ListPrize = new List<Prize>();
            [JsonProperty("Настройка UI плагина")]
            public UISettings UISetting = new UISettings();
            [JsonProperty("Настройка магазина")]
            public StoreSettings StoreSetting = new StoreSettings();

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    ListPrize = new List<Prize>
                    {
                        new Prize
                        {
                            Value = "rifle.ak",
                            Url = "",
                            Amount = 1
                        },
                        new Prize
                        {
                            Value = "addgroup %STEAMID% vip 3d",
                            Url = "https://bestgamesru.ru/uploads/monthly_2017_05/vipo.png.3602d7bb30a99c1762a8bf7fd98f5f19.png",
                            Amount = 1
                        },
                        new Prize
                        {
                            Value = "",
                            Url = "https://pics.clipartpng.com/Golden_Coins_PNG_Clipart-665.png",
                            Amount = 150
                        }
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config?.ListPrize == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Data
        public Dictionary<ulong, Parametrs> PlayerTimer = new Dictionary<ulong, Parametrs>();
        public List<ulong> Optimization = new List<ulong>();
        public class Parametrs
        {
            public double TimeGame { get; set; }
            public double PlayerAuthTime { get; set; }

            public List<InventoryItem> Inventory = new List<InventoryItem>();

            public class InventoryItem
            {
                public string Value { get; set; }
                public int Amount { get; set; }
                public string Url { get; set; }

                public InventoryItem(string value = "", int amount = 0, string url = "")
                {
                    this.Value = value;
                    this.Amount = amount;
                    this.Url = url;
                }
            }
        }
        #endregion

        #region Hooks
        private void OnServerInitialized()
        {
            PlayerTimer = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Parametrs>>("PlayerGifts/PlayerTimer");
            LoadDefaultMessages();
            AddImage(config.UISetting.UILogoSettings.InactivePng, "InactivePng");
            AddImage(config.UISetting.UILogoSettings.ActivePng, "ActivePng");

            foreach (var configItem in config.ListPrize)
            {
                if (!string.IsNullOrEmpty(configItem.Url))
                {
                    string name = !string.IsNullOrEmpty(configItem.Value) ? configItem.Value : "Coins";
                    AddImage(configItem.Url, name);
                }
            }
            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerInit(player);
                TimerRefresh(player);
            }
        }

        private void OnPlayerDie(BasePlayer player, HitInfo info) => CuiHelper.DestroyUi(player, MainPanel);

        private void OnPlayerRespawn(BasePlayer player) => UIElement(player);

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerRespawned(player));
                return;
            }
            UIElement(player);
        }

        public void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerInit(player));
                return;
            }
            if (!PlayerTimer.ContainsKey(player.userID))
            {
                Parametrs NewUser = new Parametrs()
                {
                    PlayerAuthTime = AuthTime,
                    TimeGame = 0.0,

                    Inventory = new List<Parametrs.InventoryItem> { }
                };
                PlayerTimer.Add(player.userID, NewUser);
            }
            UIElement(player);
            TimerRefresh(player);
        }

        void OnPlayerDisconnected(BasePlayer player, string reason) => Optimization.Remove(player.userID);
        void OnServerSave() => Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("PlayerGifts/PlayerTimer", PlayerTimer);
        void Unload()
        {
            OnServerSave();
            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, MainPanel);
        }
        #endregion

        #region Core

        void TimerRefresh(BasePlayer player)
        {
            timer.Every(config.TimePrize / 5, () => // Таймер
            {
                if (PlayerTimer[player.userID].TimeGame >= config.TimePrize && !Optimization.Contains(player.userID))
                {
                    //Засветить иконку и дропнуть инвентарь
                    UIElement(player);
                    Optimization.Add(player.userID); // Добавляем в лист,чтобы не обновлять кучу раз
                }
                else
                {
                    PlayerTimer[player.userID].TimeGame = Math.Max(PlayerTimer[player.userID].TimeGame + (CurrentTime() - PlayerTimer[player.userID].PlayerAuthTime), 0);
                    PlayerTimer[player.userID].PlayerAuthTime = CurrentTime();
                }
            });
        }

        #region Request

        private void AddMoney(ulong userId, float amount, string mess, Action<bool> callback)
        {
            ExecuteApiRequest(new Dictionary<string, string>()
            {
                {"action", "moneys"},
                {"type", "plus"},
                {"steam_id", userId.ToString()},
                {"amount", amount.ToString()},
                {"mess", mess}
            }, callback);
        }

        private void ExecuteApiRequest(Dictionary<string, string> args, Action<bool> callback)
        {
            string url = $"https://gamestores.ru/api?shop_id={config.StoreSetting.ServerID}&secret={config.StoreSetting.SecretKeyStore}" +
                         $"{string.Join("", args.Select(arg => $"&{arg.Key}={arg.Value}").ToArray())}";
            webrequest.EnqueueGet(url, (i, s) =>
            {
                if (i != 200)
                {
                    PrintError($"Ошибка зачисления, подробнисти в ЛОГ-Файле");
                    LogToFile("PlayerGifts", $"Код ошибки: {i}, подробности:\n{s}", this);
                    callback(false);
                }
                else
                {
                    if (s.Contains("fail"))
                    {
                        callback(false);
                        return;
                    }
                    callback(true);
                }
            }, this);
        }

        #endregion

        #endregion

        #region Commands

        [ChatCommand("pg")]
        void PlayerGiftInventory(BasePlayer player)
        {
            UIInventory(player); // Открываем ивнвентарь
        }

        [ConsoleCommand("CheckGift")]
        void CheckGift(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player(); // Объявили игрока
            if (PlayerTimer[player.userID].Inventory.Count >= 39) { MessageUI(player, lang.GetMessage("inventorypgfull", this)); return; }
            PlayerTimer[player.userID].TimeGame = 0; // Обнуляем время игры игрока
            UIElement(player); // Обновляем UI

            var Element = config.ListPrize.ElementAt(UnityEngine.Random.Range(0, config.ListPrize.Count)); // Выбираем элемент
            var Inventory = PlayerTimer[player.userID].Inventory; // Выбираем инвентарь

            Inventory.Add(new Parametrs.InventoryItem { Value = Element.Value, Amount = Element.Amount, Url = Element.Url }); // Добавляем в инвентарь          
            MessageUI(player, lang.GetMessage("rewardgive", this)); // MSG
            Optimization.Remove(player.userID); // Удаляем из листа,чтобы мог брать награду дальше
        }

        [ConsoleCommand("TakeItem")]
        void TakeItem(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player(); // Player
            var Item = PlayerTimer[player.userID].Inventory.ElementAt(Convert.ToInt32(args.Args[0])); // Выбираем инвентарь  /// ПРЕДМЕТ - КОЛИЧЕСТВО ( КОМАНДА - КОЛИЧЕСТВО )

            if (string.IsNullOrEmpty(Item.Url))
            {
                Item Gifts = ItemManager.CreateByName(Item.Value, Item.Amount, 0); // Создаем награду
                player.GiveItem(Gifts); // Выдаем награду
            }
            else
            {
                rust.RunServerCommand(Item.Value.Replace("%STEAMID%", $"{player.UserIDString}")); // Выполняем команду
            }
            if (string.IsNullOrEmpty(Item.Value) && Item.Amount != 0)
            {
                if (config.StoreSetting.ServerID != "ServerID" || config.StoreSetting.SecretKeyStore != "SecretKey")
                {
                    AddMoney(player.userID, Item.Amount, config.StoreSetting.Messages, (Action<bool>)((b) =>
                    {
                        if (!b)
                        {
                            MessageUI(player, lang.GetMessage("noauth", this));
                            return;
                        }
                    }));
                }
                else { MessageUI(player, "Администратор не настроил плагин.Сообщите ему об этом!"); return; }
            }

            PlayerTimer[player.userID].Inventory.Remove(Item); // Удаляем вещь в инвентаре
            MessageUI(player, lang.GetMessage("takeitem", this)); // MSG
            CuiHelper.DestroyUi(player, InventoryMain); // Закрываем инвентарь
        }

        [ConsoleCommand("TakeAll")]
        void TakeAll(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player(); // Player
            foreach (var AllItems in PlayerTimer[player.userID].Inventory)
            {
                if (string.IsNullOrEmpty(AllItems.Url))
                {
                    Item Gifts = ItemManager.CreateByName(AllItems.Value, AllItems.Amount, 0); // Создаем награду
                    player.GiveItem(Gifts); // Выдаем награду
                }
                else
                {
                    rust.RunServerCommand(AllItems.Value.Replace("%STEAMID%", $"{player.UserIDString}")); // Выполняем команду
                }
                if (string.IsNullOrEmpty(AllItems.Value) && AllItems.Amount != 0)
                {
                    if (config.StoreSetting.ServerID != "ServerID" || config.StoreSetting.SecretKeyStore != "SecretKey")
                    {
                        AddMoney(player.userID, AllItems.Amount, config.StoreSetting.Messages, (Action<bool>)((b) =>
                        {
                            if (!b)
                            {
                                MessageUI(player, lang.GetMessage("noauth", this));
                                return;
                            }
                        }));
                    }
                    else { MessageUI(player, "Администратор не настроил плагин.Сообщите ему об этом!"); CuiHelper.DestroyUi(player, InventoryMain); return; } // Закрываем инвентарь                        
                }
                timer.Once(0.5f, () => PlayerTimer[player.userID].Inventory.Remove(AllItems)); // Удаляем вещь в инвентаре
            }

            if (PlayerTimer[player.userID].Inventory.Count < 1)  // Попытка взять пустой инвентарь
                MessageUI(player, lang.GetMessage("takeallnull", this)); // MSG
            else
                MessageUI(player, lang.GetMessage("takeallinventory", this)); // MSG
            CuiHelper.DestroyUi(player, InventoryMain); // Закрываем инвентарь
        }
        #endregion

        #region UI

        static string MainPanel = "XCC_MAINPANELGIFT{DarkPluginsID}";
        static string InventoryMain = "XCC_INVENTORY";
        static string MessagePanel = "XCC_MESSAGES_UI";
        void UIElement(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, MainPanel);
            CuiElementContainer container = new CuiElementContainer();

            string png = PlayerTimer[player.userID].TimeGame >= config.TimePrize ? "ActivePng" : "InactivePng";

            container.Add(new CuiElement
            {
                Parent = "Hud",
                Name = MainPanel,
                Components = {
                    new CuiRawImageComponent {
                        Png = GetImage(png),
                    },
                    new CuiRectTransformComponent {
                      AnchorMin = config.UISetting.UILogoSettings.AnchorMin,
                      AnchorMax = config.UISetting.UILogoSettings.AnchorMax,
                      OffsetMin = config.UISetting.UILogoSettings.OffsetMin,
                      OffsetMax = config.UISetting.UILogoSettings.OffsetMax
                    },
                }
            });

            if (PlayerTimer[player.userID].TimeGame >= config.TimePrize) // Проверяем,отыграл игрок сколько положено или нет
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "CheckGift", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, MainPanel);
            }

            CuiHelper.AddUi(player, container);
        }

        void UIInventory(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, InventoryMain);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0.7", Sprite = "assets/content/ui/ui.background.transparent.radial.psd",  Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "Overlay", InventoryMain);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100" },
                Button = { Close = InventoryMain, Color = "0 0 0 0" },
                Text = { FadeIn = 0.8f, Text = "" }
            }, InventoryMain);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9138894", AnchorMax = "0.981482 1", OffsetMax = "0 0" },
                Text = { Text = config.UISetting.UIInventorySettings.TitleInventory, Font = config.UISetting.UIInventorySettings.Font, Align = TextAnchor.MiddleCenter }
            }, InventoryMain);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.8962963", AnchorMax = "1 0.9324074", OffsetMax = "0 0" },
                Text = { Text = config.UISetting.UIInventorySettings.DescriptionInventory, Font = config.UISetting.UIInventorySettings.Font, Align = TextAnchor.MiddleCenter }
            }, InventoryMain);

            container.Add(new CuiPanel
            {
                FadeOut = 0.5f,
                RectTransform = { AnchorMin = "0.08958333 0.2629631", AnchorMax = "0.9520833 0.8916668" },
                Image = { FadeIn = 0.5f, Color = "0 0 0 0"}
            }, InventoryMain, "InventoryPanel");

            for(int i = 0, x = 0, y = 0; i < 36; i++)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0 + (x * 0.105)} {0.7805594 - (y * 0.23)}", AnchorMax = $"{0.09722221 + (x * 0.105)} {0.995 - (y * 0.23)}" },
                    Image = { Color = "0 0 0 0.7", Sprite = "assets/content/ui/ui.background.transparent.radial.psd", Material = "assets/content/ui/uibackgroundblur.mat" }
                }, "InventoryPanel", $"Slot_{i}");

                x++;
                if (x >= 9)
                {
                    x = 0;
                    y++;
                }
                if (x >= 9 && y >= 4) break;
            }

            for (int i = 0, x = 0, y = 0; i < PlayerTimer[player.userID].Inventory.Count; i++)
            {
                string png = string.IsNullOrEmpty(PlayerTimer[player.userID].Inventory.ElementAt(i).Value) ? "Coins" : PlayerTimer[player.userID].Inventory.ElementAt(i).Value;
                container.Add(new CuiElement
                {
                    Parent = $"Slot_{i}",
                    Name = "ItemInventory",
                    Components = {
                    new CuiRawImageComponent {
                        Png = GetImage(png),
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = $"0.08 0.04",
                        AnchorMax = $"0.9 0.9"
                    },
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = $"TakeItem {i}", Color = "0 0 0 0", Sprite = "assets/content/ui/ui.background.transparent.radial.psd", },
                    Text = { Text = PlayerTimer[player.userID].Inventory.ElementAt(i).Amount.ToString() + "шт", Align = TextAnchor.LowerCenter, FontSize = 17, Font = config.UISetting.UIInventorySettings.Font }
                }, "ItemInventory");

                x++;
                if (x >= 10)
                {
                    x = 0;
                    y++;
                }
                if (x == 10 && y == 4) break;
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.410417 0.2064815", AnchorMax = "0.5901042 0.2638889" },
                Button = { Command = "TakeAll", Color = HexToRustFormat("#0000005D") },
                Text = { Text = "Забрать все", Align = TextAnchor.MiddleCenter, FontSize = 23, Font = config.UISetting.UIInventorySettings.Font }
            }, InventoryMain);

            CuiHelper.AddUi(player, container);
        }

        #region Message

        void MessageUI(BasePlayer player, string Messages)
        {
            CuiHelper.DestroyUi(player, MessagePanel);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.3291668 0.8583333", AnchorMax = "0.6614581 0.9166667" },
                Image = { FadeIn = 0.4f, Color = HexToRustFormat(config.UISetting.UIInventorySettings.UIMsgColor) }
            }, "Overlay", MessagePanel);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = String.Format(Messages), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", Color = HexToRustFormat("#FFFFFFFF") }
            }, MessagePanel);

            CuiHelper.AddUi(player, container);

            timer.Once(2f, () => { CuiHelper.DestroyUi(player, MessagePanel); });
        }


        #endregion

        #endregion

        #region Helpers

        public double AuthTime = CurrentTime();
        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;

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
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            PrintWarning("Языковой файл загружается...");
            timer.In(2.5f, () => {
                Dictionary<string, string> Lang = new Dictionary<string, string>
                {
                    ["takeallnull"] = "У вас ничего нет",
                    ["takeallinventory"] = "Вы успешно забрали весь инвентарь",
                    ["inventoryfull"] = "Ваш инвентарь полон,награда выброшена под ноги!",
                    ["inventorypgfull"] = "Ваш инвентарь полон,освободите слоты чтобы получить награду",
                    ["takeitem"] = "Вы успешно забрали награду",
                    ["rewardgive"] = "Вы успешно получили награду",
                    ["noauth"] = "Для того чтобы получить баланс вы должны быть авторизованы в магазине!",
                };
                lang.RegisterMessages(Lang, this, "en");
                PrintWarning("Языковой файл загружен успешно");
            });
        }
        #endregion
    }
}
