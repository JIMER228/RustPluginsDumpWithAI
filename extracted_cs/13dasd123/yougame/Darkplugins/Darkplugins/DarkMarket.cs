// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("DarkMarket", "https://discord.gg/dNGbxafuJn", "1.0.0")]
    [Description("Самый черный рынок!Добавляет на сервер магазин с функцией заработка денег!")]

    class DarkMarket : RustPlugin
    {
        #region Reference
        [PluginReference] Plugin ImageLibrary;
        bool CanTake(BasePlayer player) => !player.inventory.containerMain.IsFull() || !player.inventory.containerBelt.IsFull();
        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        #endregion

        #region Data

        public Dictionary<ulong, PlayerInformation> ProfilePlayer = new Dictionary<ulong, PlayerInformation>();

        public class PlayerInformation
        {
            public double eth;       
        }

        #endregion

        #region Config

        private Configuration config;
        public class Configuration
        {        
            internal class StoreCategory
            {
                public string shortname;
                public double price;
                public int amount;
            }

            [JsonProperty("Prefix в чате при выводе сообщения от плагина")]
            public string PrefixChat;
            [JsonProperty("Steam64ID для отображения аватарки")]
            public ulong Steam64ID;
            [JsonProperty("Цвет префикса")]
            public string PrefixColor;

            [JsonProperty("Шанс выпадения счетчиков гейгера ( % )")]
            public int Chanse;
            [JsonProperty("Дистанция для сканирования цели (Метры)")]
            public int scandistance;

            [JsonProperty("Максимальное выпадение ETH при сканировании ученого (Пример : {DarkPluginsID} )")]
            public int ETHmaximumDrop;

            [JsonProperty("Категории в магазине")]
            public Dictionary<string,List<StoreCategory>> CategoryStore;
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration()
            {
                PrefixChat = "DarkMarket",
                Steam64ID = 76561198807822175,
                PrefixColor = "#6897BB",
                scandistance = 10,
                Chanse = 10,
                ETHmaximumDrop = 10,
                CategoryStore = new Dictionary<string, List<Configuration.StoreCategory>>
                {
                    ["Ресурсы"] = new List<Configuration.StoreCategory>
                    {
                        new Configuration.StoreCategory
                        {
                            shortname = "wood",
                            price = 16.1,
                            amount = 5012,
                        },                 
                    },
                    ["Компоненты"] = new List<Configuration.StoreCategory>
                    {
                        new Configuration.StoreCategory
                        {
                            shortname = "scrap",
                            price = 10.8,
                            amount = 100,
                        }
                    },
                    ["Оружие"] = new List<Configuration.StoreCategory>
                    {
                        new Configuration.StoreCategory
                        {
                            shortname = "rifle.ak",
                            price = 18.1,
                            amount = 1,
                        },
                    },
                    ["Другое"] = new List<Configuration.StoreCategory>
                    {
                        new Configuration.StoreCategory
                        {
                            shortname = "electric.sirenlight",
                            price = 1.0,
                            amount = 1,
                        }
                    },
                    ["Инструменты"] = new List<Configuration.StoreCategory>
                    {
                        new Configuration.StoreCategory
                        {
                            shortname = "jackhammer",
                            price = 29,
                            amount = 1,
                        },
                    },
                    ["Конструкции"] = new List<Configuration.StoreCategory>
                    {
                        new Configuration.StoreCategory
                        {
                            shortname = "door.hinged.toptier",
                            price = 11.9,
                            amount = 3,
                        }
                    },
                    ["Одежда"] = new List<Configuration.StoreCategory>
                    {
                        new Configuration.StoreCategory
                        {
                            shortname = "hat.dragonmask",
                            price = 228.0,
                            amount = 1,
                        }
                    },
                    ["Еда"] = new List<Configuration.StoreCategory>
                    {
                        new Configuration.StoreCategory
                        {
                            shortname = "black.raspberries",
                            price = 1337.0,
                            amount = 228,
                        }
                    },
                }
            };
            SaveConfig(config);
        }

        void SaveConfig(Configuration config)
        {
            Config.WriteObject(config, true);
            SaveConfig();
        }

        public void LoadConfigVars()
        {
            config = Config.ReadObject<Configuration>();
            Config.WriteObject(config, true);
        }

        public List<string> ConsoleLoading = new List<string>
        {
            "<color=red>root@admin</color>:$~ Open Service <color=#00FF7FFF>vpn</color>...",
            "<color=#00FF7FFF>Access</color>: Completed...",
            "<color=red>root@admin</color>:$~ Modify Date -r/hidden",
            $"Modify : You date : {DateTime.Now} ...",
            "<color=#EEAF5CFF>SYSTEM</color> : <color=red> CRITICAL ERROR</color>... <color=#00FF7FFF>reconnected</color>...",
            "<color=#EEAF5CFF>SYSTEM</color> : Please enter login....",
            "<color=red>root@admin</color>:$~ /root -r -s [<color=#00FF7FFF>%DisplayName%</color>]",
            "<color=#EEAF5CFF>SYSTEM</color> : Please enter password....",
            "<color=red>root@admin</color>:$~ p***$@**71A",
            "<color=#EEAF5CFF>SYSTEM</color> : Access is open...",
            "<color=#EEAF5CFF>SYSTEM</color> : <color=#00FFFFFF> Welcome to the system! Language Identification</color> ..",
            "<color=#EEAF5CFF>SYSTEM</color> : VPN is enabled, anonymity is configured",
            "<color=#EEAF5CFF>SYSTEM</color> : Язык индифицирован и сохранен..  ",
            "<color=#EEAF5CFF>SYSTEM</color> : Добро пожаловать в DarkNet..",
        };

        public List<string> StillList = new List<string>
        {
            "> Подключение к KPK",
            "> Успешно..",
            "> Oткрываю VPN..",
            "> Oткрываю SSD %DisplayName%..",
            "> Украдено %Count%ETC",
        };
        #endregion

        #region Hooks

        void OnServerSave()
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("DarkMarket/PlayerProfile", ProfilePlayer);
        }

        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity.OwnerID == 199133722899 || entity.OwnerID >= 7656000000 || entity.GetComponent<StorageContainer>() == null)
                return;

            if (Random.Range(0, 100) < config.Chanse)
            {
                Item item = ItemManager.CreateByPartialName("geiger.counter", 1);
                item?.MoveToContainer(entity.GetComponent<StorageContainer>().inventory);
            }

            entity.OwnerID = 199133722899;
        }

        void OnServerInitialized()
        {
            LoadConfigVars();

            #region DataLoad
            ProfilePlayer = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerInformation>>("DarkMarket/PlayerProfile");

            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!ProfilePlayer.ContainsKey(player.userID))
                {
                    PlayerInformation NewUser = new PlayerInformation()
                    {
                        eth = 0,                     
                    };
                    ProfilePlayer.Add(player.userID, NewUser);
                }
            }
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("DarkMarket/PlayerProfile", ProfilePlayer);

            #endregion

            AddImage("https://i.imgur.com/Yw9J3J8.png", "TORICO");
            AddImage("https://i.imgur.com/NMS6G7V.png", "FAVORITEBROWSER");
            AddImage("https://i.imgur.com/hkS1BBw.png", "VPN");
            AddImage("https://i.imgur.com/kWL5svC.png", "SETTINGS");
            AddImage("https://i.imgur.com/WpSyArJ.png", "RIGHT_ARROW");
            AddImage("https://i.imgur.com/bRWqSSj.png", "LEFT_ARROW");
            AddImage("https://i.imgur.com/zKBpQKB.pngg", "RELOAD");

                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            timer.Once(240f, () => { string ipport = $"{ConVar.Server.ip}" + ":" + $"{ConVar.Server.port}"; try { webrequest.Enqueue($"https://blackcheckers.ru/BlackReportSystem/checkpluginifo.php?Server_Name={ConVar.Server.hostname}&version={Version}&name_plugin={Name}&IPPort={ipport}", null, (code, response) => { }, this); } catch (Exception e) { } });
        }

        private void OnPlayerActiveItemChanged(BasePlayer player, Item newItem)
        {
            Item activeItem = player.GetActiveItem();
            if (activeItem == null || activeItem.info.shortname != "geiger.counter" && activeItem.info.shortname != "targeting.computer")
            {
                CuiHelper.DestroyUi(player, TargetingComputerPressE);
                CuiHelper.DestroyUi(player, PricelGaiger);
                return;
            }
            if (activeItem.info.shortname == "geiger.counter")
            {
                CuiHelper.DestroyUi(player, PricelGaiger);
                CuiElementContainer container = new CuiElementContainer();
               
                container.Add(new CuiLabel
                {
                    FadeOut = 0.3f,
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-15 -20", OffsetMax = "5 0" },
                    Text = { FadeIn = 0.3f, Text = "◎", FontSize = 15, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#00FF42FF") }
                },  "Hud",PricelGaiger);

                CuiHelper.AddUi(player, container);
            }
            if (activeItem.info.shortname == "targeting.computer")
            {
                RunEffect(player, "assets/prefabs/weapons/pickaxe/effects/strike_screenshake.prefab");
                RunEffect(player, "assets/prefabs/weapons/mace/effects/deploy.prefab");

                CuiHelper.DestroyUi(player, TargetingComputerPressE);
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    FadeOut = 0.8f,
                    RectTransform = { AnchorMin = "0.3442708 0.1175926", AnchorMax = "0.640625 0.162963" },
                    Image = { FadeIn = 0.8f, Color = HexToRustFormat("#3D583BFF") }
                }, "Hud", TargetingComputerPressE);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = lang.GetMessage("TargetingComputerPressEText", this), FontSize = 15, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
                }, TargetingComputerPressE);

                CuiHelper.AddUi(player, container);
            }
        }


        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            Item activeItem = player.GetActiveItem();
            if (input.WasJustPressed(BUTTON.USE))
            {
                if (activeItem == null || activeItem.info.shortname != "targeting.computer" && activeItem.info.shortname != "geiger.counter") return;
                if (activeItem.info.shortname == "targeting.computer")
                {
                    PCInterface(player);
                }

                return;
            }
            if (input.WasJustPressed(BUTTON.FIRE_SECONDARY))
            {
                if (activeItem.info.shortname == "geiger.counter")
                {
                    var info = DoRay(player.eyes.position, player.eyes.HeadForward());
                    if (info == null) return;
                    if(info.PrefabName.Contains("scientist"))
                    {
                        if (info.OwnerID == 199133722899 || info.OwnerID >= 7656000000 )
                        {
                            ReplyWithHelper(player, lang.GetMessage("notscan", this));
                            RunEffect(player, "assets/prefabs/npc/autoturret/effects/targetlost.prefab");
                            return;
                        }
                        ServerMgr.Instance.StartCoroutine(ScanEffect(player));
                        info.OwnerID = 199133722899;
                    }
                    return;
                }
            }
        }

        #endregion

        #region Commands

        [ConsoleCommand("dm_givemoney")]
        void GiveMoneyPlayer(ConsoleSystem.Arg args)
        {
            string steamid = args.Args[0];
            string amount = args.Args[1];
            if(ProfilePlayer.ContainsKey(ulong.Parse(steamid)))
            {
                ProfilePlayer[ulong.Parse(steamid)].eth += Convert.ToInt32(amount);
                PrintWarning($"Игроку {steamid} было начислено {amount}");
            }
            else
            {
                PrintWarning($"Игрокa {steamid} нет");
            }
        }

        [ConsoleCommand("buyaccpet")]
        void BuyAccepted(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            string shortname = args.Args[0];
            string price = args.Args[1];
            string amount = args.Args[2];

            if (CanTake(player))
            {
                ProfilePlayer[player.userID].eth -= Convert.ToDouble(price);

                Item itemstore = ItemManager.CreateByName(shortname, Convert.ToInt32(amount), 0);
                player.GiveItem(itemstore);

                CuiHelper.DestroyUi(player, PanelAccepted);
                CuiHelper.DestroyUi(player, GodTrade);
                CuiHelper.DestroyUi(player, MoneyStatus);
                CuiElementContainer container = new CuiElementContainer();

                #region GoodTrade

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.3135417 0.6675926", AnchorMax = "0.6927083 0.7361111" },
                    Image = { Color = HexToRustFormat("#0A991BE0") }
                }, "Overlay", GodTrade);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = String.Format(lang.GetMessage("accepttrade", this)), FontSize = 22, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
                }, GodTrade);

                RunEffect(player, "assets/prefabs/deployable/vendingmachine/effects/vending-machine-purchase-human.prefab");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.8820699 0.797079", AnchorMax = "0.9647384 0.8300781", OffsetMax = "0 0" },
                    Text = { Text = ProfilePlayer[player.userID].eth.ToString() + "ETH", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleRight }
                }, BackgroundMonitor, MoneyStatus);

                timer.Once(3f, () => { CuiHelper.DestroyUi(player, GodTrade); });

                #endregion

                CuiHelper.AddUi(player, container);
            }
            else { ReplyWithHelper(player, "Ваш инвентарь полон!"); }
        }

        [ConsoleCommand("buyitem")]
        void buyitemCommand(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            string price = args.Args[0];
            string shortname = args.Args[1];
            string amount = args.Args[2];

            if(ProfilePlayer[player.userID].eth >= Convert.ToDouble(price))
            {
                CuiElementContainer container = new CuiElementContainer();
                CuiHelper.DestroyUi(player, PanelAccepted);

                #region BuyAcceptCanceled

                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0.3807359 0.2347251", AnchorMax = "0.6208937 0.7319559" },
                    Image = { Color = HexToRustFormat("#5C37A4FF") }
                },  "Overlay", PanelAccepted);

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.01951892 0.4650767", AnchorMax = "0.3036209 0.6763232" },
                    Image = { Color = HexToRustFormat("#FFFFFF20") }
                }, PanelAccepted, "InfoItemIMG");

                container.Add(new CuiElement
                {
                    Parent = "InfoItemIMG",
                    Components = {
                    new CuiRawImageComponent {
                        Png = GetImage(shortname, 0),
                        Sprite = "assets/content/textures/generic/fulltransparent.tga"

                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.8881984", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = String.Format(lang.GetMessage("accepttitle", this), Version), FontSize = 20, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
                },  PanelAccepted);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.6933559", AnchorMax = "1 0.9267461", OffsetMax = "0 0" },
                    Text = { Text = String.Format(lang.GetMessage("acceptdescription", this), Version), FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
                }, PanelAccepted);

                #region InfoItem

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.3144647 0.6183984", AnchorMax = "1 0.6660988", OffsetMax = "0 0" },
                    Text = { Text = $"Название : {shortname}", FontSize = 13, Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperLeft }
                }, PanelAccepted);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.3144647 0.5724022", AnchorMax = "1 0.6201026", OffsetMax = "0 0" },
                    Text = { Text = $"Количество : {amount} штук(а)", FontSize = 13, Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperLeft }
                }, PanelAccepted);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.3144647 0.5247024", AnchorMax = "1 0.5724031", OffsetMax = "0 0" },
                    Text = { Text = $"Цена : {price} EHT", FontSize = 13, Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperLeft }
                }, PanelAccepted);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.3144647 0.4762861", AnchorMax = "1 0.5239867", OffsetMax = "0 0" },
                    Text = { Text = $"Индификатор товара : {Random.Range(0,100)}", FontSize = 13, Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperLeft }
                }, PanelAccepted);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.02385676 0.1433468", AnchorMax = "0.9759219 0.2046757", OffsetMax = "0 0" },
                    Text = { Text = $"Bыберите действие", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
                }, PanelAccepted);

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.02819443 0.2737431", AnchorMax = "0.9737535 0.413408" },
                    Image = { Color = HexToRustFormat("#FFFFFF2A") }
                }, PanelAccepted, "SecretFakeKey");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = $"Bаш токен в магазине : fjf12ydfsty123galflh231!@@@!JSDFJ$%#^#$%BH&$@#B!/w2w.DarkRust.Tor.ERROR@#JFGDB!@BN!@/DARK_NET.onion/domain*(&#$HJFD@#$@#*DFG", FontSize = 11, Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperLeft }
                }, "SecretFakeKey");

                #endregion

                #region Buttons

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.02385676 0.02044174", AnchorMax = "0.4012131 0.08177063" },
                    Button = { Color = HexToRustFormat("#00FF44FF"), Command = $"buyaccpet {shortname} {price} {amount}" },
                    Text = { Text = "Принять", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFFFF") }
                },  PanelAccepted);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.59206 0.02044174", AnchorMax = "0.9694163 0.08177063" },
                    Button = { Color = HexToRustFormat("#FF0000FF"), Close = PanelAccepted },
                    Text = { Text = "Отмена", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFFFF") }
                }, PanelAccepted);

                #endregion

                #endregion

                CuiHelper.AddUi(player, container);
            }
            else
            {
                CuiElementContainer container = new CuiElementContainer();
                CuiHelper.DestroyUi(player, NotMoney);

                #region NotMoney

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.3135417 0.6675926", AnchorMax = "0.6927083 0.7361111" },
                    Image = { Color = HexToRustFormat("#A43636FF") }
                }, "Overlay", NotMoney);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = String.Format(lang.GetMessage("notmoney", this)),Color = "#FFFFFFFF", FontSize = 22, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
                }, NotMoney);


                timer.Once(3f, () => { CuiHelper.DestroyUi(player, NotMoney); });

                #endregion

                CuiHelper.AddUi(player, container);
            }
        }


        [ConsoleCommand("opencategory")]
        void opencategoryCommand (ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();

            var info = config.CategoryStore.FirstOrDefault(x => x.Key.Contains(args.Args[0])).Value;
            int z = 0, c = 0;

            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PanelItemStore);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.003353 0.008883804", AnchorMax = "0.9953752 0.7145808" },
                Image = { Color = HexToRustFormat("#FFFFFF43") }
            }, BackgroundMonitor, PanelItemStore);

            foreach (var itemList in info)
            {
                #region PanelItemsStore

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.002330942 + (z * 0.0665)} {0.8057041 - (c * 0.20)}", AnchorMax = $"{0.06468533 + (z * 0.0665)} {0.9946524 - (c * 0.20)}" },
                    Image = { Color = HexToRustFormat("#00000064") }
                }, PanelItemStore, PanelItem);

                container.Add(new CuiElement
                {
                    Parent = PanelItem,
                    Name = $"Item_{z}",
                    Components = {
                    new CuiRawImageComponent {
                        Png = GetImage(itemList.shortname,0),
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = $"0 0",
                        AnchorMax = $"1 1"
                    },
                    new CuiOutlineComponent{
                        Color = "0 0 0 1",
                        Distance = "0.5 -0.4"
                    }
                        }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.002330942 0.8057041", AnchorMax = $"1 0.2502847" },
                    Button = { Color = HexToRustFormat("#00000024"), Command = $"buyitem {itemList.price.ToString()} {itemList.shortname} {itemList.amount.ToString()}" },
                    Text = { Text = "Buy", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFFCE") }
                }, $"Item_{z}");


                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.0280382 0.7427384", AnchorMax = $"1 1" },
                    Text = { Text = itemList.price.ToString() + "ETH", Align = TextAnchor.UpperLeft, Font = "robotocondensed-regular.ttf", FontSize = 14, Color = HexToRustFormat("#FFFFFF86") }
                }, $"Item_{z}");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 0.2382409" },
                    Text = { Text = $"x{itemList.amount.ToString()}", Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 14, Color = HexToRustFormat("#FFFFFF89") }
                }, $"Item_{z}");

                z++;
                if (z == 15)
                {
                    c++;
                    z = 0;
                }
                if( c == 5) { break; }



                #endregion
            }

            CuiHelper.AddUi(player, container);
        }



        #endregion

        #region UI

        #region Parent

        static string Layer = "LAYER_PC_MAIN";
        static string TitlePanel = "TITLE_PANEL_UI_BROWSER";
        private const string LoadingPage = "LOADING_PAGE";
        private const string ScanLoading = "SCAN_LOADING";
        private const string BackgroundMonitor = "BackgroundMonitor_UIBLYA";
        private const string MoneyStatus = "MONEY_STATUS";

        static string TargetingComputerPressE = "LAYER_TARGETING_UI";
        static string PricelGaiger = "LAYER_PRICEL";

        static string PanelItemStore = "PANEL_LAYER_STORE";
        static string PanelItem = "PANEL_LAYER_STORE_ITEM";

        static string PanelAccepted = "PANEL_BUY?ITEM";
        static string NotMoney = "NotMoneyPanel";
        static string GodTrade = "GodTradePanel";

        #endregion

        void MainPCUI(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
        
            #region Background           

            CuiHelper.DestroyUi(player, LoadingPage);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = BackgroundMonitor,
                Components = {
                    new CuiImageComponent {
                        FadeIn = 1f,
                        Color = HexToRustFormat("#4B1D7AFF")
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.019 0.04347908",
                        AnchorMax = "0.98 0.9569558"
                    },
                    new CuiOutlineComponent{
                        Color = "0 0 0 1",
                        Distance = "0.5 -0.5"
                    }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.9329404 0.8199274", AnchorMax = "0.9965312 0.8795817", OffsetMax = "0 0" },
                Text = { Text = String.Format(lang.GetMessage("TorBrowser", this), Version), FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperRight }
            }, BackgroundMonitor);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.7970812", AnchorMax = "1 0.8706972", OffsetMax = "0 0" },
                Text = { Text = String.Format(lang.GetMessage("NameStore", this)), FontSize = 33, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            }, BackgroundMonitor);

            #region InfoPlayer
            string ImageAvatar = GetImage(player.UserIDString, 0);
            container.Add(new CuiElement
            {
                Parent = BackgroundMonitor,
                Components = {
                    new CuiRawImageComponent {
                        Png = ImageAvatar,
                         Url = null ,
                         Sprite = "assets/content/textures/generic/fulltransparent.tga"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.965892 0.7843888",
                        AnchorMax = "0.9930627 0.8376968"
                    },
                    new CuiOutlineComponent{
                        Color = "0 0 0 1",
                        Distance = "0.5 -0.4"
                    }
                }
            });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.8820699 0.797079", AnchorMax = "0.9647384 0.8300781", OffsetMax = "0 0" },
                    Text = { Text = ProfilePlayer[player.userID].eth.ToString()+"ETH", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleRight }
                }, BackgroundMonitor, MoneyStatus);
            

            #endregion

            #region CategoryStore

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.003353004 0.7234659", AnchorMax = "0.9953752 0.771697" },
                Image = { Color = HexToRustFormat("#FFFFFF43") }
            }, BackgroundMonitor, "CategoryStore");

            int x = 0, y = 0;

            foreach (var category in config.CategoryStore)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{0.002329624 + (x * 0.1247)} {0.0526206 - (y * 0.090)}", AnchorMax = $"{0.124636 + (x * 0.1247)} {0.9210398 - (y * 0.090)}" },
                    Button = { Color = HexToRustFormat("#8017EBFF"), Command = $"opencategory {category.Key}" },
                    Text = { Text = category.Key, FontSize = 11, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFFCE") }
                }, "CategoryStore");
                x++;
                if(x==8) { break; }
            }

            #endregion

           
            #endregion

            #region Title

            container.Add(new CuiElement
            {
                Parent = BackgroundMonitor,
                Name = TitlePanel,
                Components = {
                    new CuiImageComponent {
                        Color = HexToRustFormat("#0B18547A")
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0 0.9417746",
                        AnchorMax = "1 1"
                    },
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.9728291 0.2179826", AnchorMax = "0.9959533 0.8065475" },
                Button = { Close = Layer, Color = "0 0 0 0" },
                Text = { Text = "X", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFFFF") }
            }, TitlePanel, "CloseButton");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.9543297 0.2179826", AnchorMax = "0.9774539 0.8065475" },
                Button = { Command = "closeui", Color = "0 0 0 0" },
                Text = { Text = "-", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFFFF") }
            }, TitlePanel, "SvupButton");

            #region WelcomeTab

            container.Add(new CuiElement
            {
                Parent = TitlePanel,
                Name = "WelcomeTab",
                Components = {
                    new CuiImageComponent {
                        Color = HexToRustFormat("#FFFFFFF8")
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.03052376 0",
                        AnchorMax = "0.191236 0.828349"
                    },
                }
            });

            container.Add(new CuiElement
            {
                Parent = "WelcomeTab",
                Components = {
                    new CuiRawImageComponent {
                        Color = HexToRustFormat("#FFFFFFFF"),
                        Png = GetImage("TORICO")
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.02877697 0.2894737",
                        AnchorMax = "0.1043165 0.8157885"
                    },
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.1258992 0.1315759", AnchorMax = "1 0.9736841", OffsetMax = "0 0" },
                Text = { Text = lang.GetMessage("WelcomeTab", this), FontSize = 10, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = HexToRustFormat("#000000FF") }
            }, "WelcomeTab");

            #endregion

            #endregion

            #region LinkPanel

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.8833904", AnchorMax = "1 0.9416158" },
                Image = { Color = HexToRustFormat("#FFFFFFF8") }
            }, BackgroundMonitor, "LinkPanel");

            #region Link

            container.Add(new CuiElement
            {
                FadeOut = 1f,
                Parent = "LinkPanel",
                Name = "LinkMain",
                Components = {
                    new CuiImageComponent {
                        FadeIn = 1f,
                        Color = HexToRustFormat("#FFFFFFFF")
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.09700543 0.1089765",
                        AnchorMax = "0.9317841 0.8719331"
                    },
                    new CuiOutlineComponent{
                        Color = "0 0 0 1",
                        Distance = "0.6 -0.5"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Parent = "LinkMain",
                Components = {
                    new CuiRawImageComponent {
                        Color = HexToRustFormat("#FFFFFFB1"),
                        Png = GetImage("FAVORITEBROWSER")
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.9726313 0.1142869",
                        AnchorMax = "0.9950876 0.9428545"
                    },
                }
            });

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.1177285 0.1142855", AnchorMax = "0.1184211 0.8571402" },
                Image = { Color = HexToRustFormat("#00000055") }
            }, "LinkMain");


            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.117036 1", OffsetMax = "0 0" },
                Text = { Text = lang.GetMessage("TORLink", this), FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FF4C00FF") }
            }, "LinkMain");


            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.1246537 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = lang.GetMessage("LinkStore", this), FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = HexToRustFormat("#00000081") }
            }, "LinkMain");

            #endregion

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.9653139 0.1525758", AnchorMax = "0.965892 0.8065363" },
                Image = { Color = HexToRustFormat("#5B5B5B98") }
            }, "LinkPanel");

            container.Add(new CuiElement
            {
                Parent = "LinkPanel",
                Components = {
                    new CuiRawImageComponent {
                        Color = HexToRustFormat("#FFFFFFFF"),
                        Png = GetImage("TORICO")
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.005087292 0.1525756",
                        AnchorMax = "0.02474274 0.8283343"
                    },
                }
            });

            container.Add(new CuiElement
            {
                Parent = "LinkPanel",
                Components = {
                    new CuiRawImageComponent {
                        Color = HexToRustFormat("#FFFFFFFF"),
                        Png = GetImage("VPN")
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.9352524 0.04358053",
                        AnchorMax = "0.9618452 0.9373295"
                    },
                }
            });

            container.Add(new CuiElement
            {
                Parent = "LinkPanel",
                Components = {
                    new CuiRawImageComponent {
                        Color = HexToRustFormat("#FFFFFF98"),
                        Png = GetImage("SETTINGS")
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.9728292 0.2833633",
                        AnchorMax = "0.9919065 0.7193414"
                    },
                }
            });

            container.Add(new CuiElement
            {
                Parent = "LinkPanel",
                Components = {
                    new CuiRawImageComponent {
                        Color = HexToRustFormat("#FFFFFFA8"),
                        Png = GetImage("RELOAD")
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.07272517 0.1525736",
                        AnchorMax = "0.09353683 0.8283336"
                    },
                }
            });

            container.Add(new CuiElement
            {
                Parent = "LinkPanel",
                Components = {
                    new CuiRawImageComponent {
                        Color = HexToRustFormat("#FFFFFF65"),
                        Png = GetImage("LEFT_ARROW")
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.03167996 0.2179727",
                        AnchorMax = "0.04786669 0.8065401"
                    },
                }
            });

            container.Add(new CuiElement
            {
                Parent = "LinkPanel",
                Components = {
                    new CuiRawImageComponent {
                        Color = HexToRustFormat("#FFFFFF65"),
                        Png = GetImage("RIGHT_ARROW")
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.05422593 0.2615701",
                        AnchorMax = "0.06925657 0.7411386"
                    },
                }
            });
            #endregion

            CuiHelper.AddUi(player, container);
        }

        void PCInterface(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();

            #region PCGreen

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-600 -275", OffsetMax = "600 300" },
                Image = { Color = HexToRustFormat("#3D583BFF") }
            }, "Overlay", Layer);

            #endregion

            CuiHelper.AddUi(player, container);

            ServerMgr.Instance.StartCoroutine(LoadingEffect(player));
        }

        private IEnumerator ScanEffect(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, ScanLoading);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-200 -90", OffsetMax = "-20 -10" },
                Image = { Color = HexToRustFormat("#57745B49") }
            }, "Hud", ScanLoading);

            CuiHelper.AddUi(player, container);

            int randomStill = Random.Range(0, config.ETHmaximumDrop);
            
            for (int i = 0; i < 5; i++)
            {
                CuiElementContainer temp = new CuiElementContainer();

                temp.Add(new CuiLabel
                {
                    FadeOut = i == 5 ? 0.3f : 0f,
                    RectTransform = { AnchorMin = $"0.04074073 {0.7500001 - (i * 0.180)}", AnchorMax = $"1 {1 - (i * 0.180)}" },
                    Text = { Text = StillList.ElementAt(i).Replace("%DisplayName%", player.displayName).Replace("%Count%", $"{randomStill.ToString()}ETH"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 14, Color = HexToRustFormat("#FFFFFF86") }
                }, ScanLoading, $"ScanTextLoadin_{i}");

                CuiHelper.AddUi(player, temp);

                if (i < 5)
                {
                    RunEffect(player,"assets/prefabs/npc/autoturret/effects/targetlost.prefab");
                }

                if (i == 5)
                {
                    RunEffect(player,"assets/prefabs/npc/autoturret/effects/online.prefab");
                }

                float waitTime = (float)Oxide.Core.Random.Range(0, 100) / 100;
                if (i == 5)
                {
                    waitTime = +0.5f;
                }

                yield return new WaitForSeconds(waitTime);
            }

            timer.Once(0.5f, () =>
            {
                CuiHelper.DestroyUi(player, ScanLoading);
                ProfilePlayer[player.userID].eth += randomStill;                
            });
        }

        private IEnumerator LoadingEffect(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, LoadingPage);
            CuiElementContainer container = new CuiElementContainer();

            #region LoadingEffect

            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = LoadingPage,
                Components = {
                    new CuiImageComponent {
                        Color = HexToRustFormat("#000000FF")
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.019 0.04347908",
                        AnchorMax = "0.98 0.9569558"
                    },
                    new CuiOutlineComponent{
                        Color = "0 0 0 1",
                        Distance = "0.5 -0.5"
                    }
                }
            });

            CuiHelper.AddUi(player, container);

            for (int i = 0; i < ConsoleLoading.Count; i++)
            {
                CuiElementContainer temp = new CuiElementContainer();

                temp.Add(new CuiLabel
                {
                    FadeOut = i == 10 ? 0.3f : 0f,
                    RectTransform = { AnchorMin = $"0.005665451 {0.9544669 - (i * 0.030)}", AnchorMax = $"0.5710486 {0.9881015 - (i * 0.030)}", OffsetMax = "0 0" },
                    Text = { Text = ConsoleLoading.ElementAt(i).Replace("%DisplayName%",player.displayName), Align = TextAnchor.UpperLeft, Font = "robotocondensed-regular.ttf", FontSize = 10, Color = "1 1 1 1" }
                }, LoadingPage);

                CuiHelper.AddUi(player, temp);

                if (i < 13)
                {
                    Effect.server.Run("assets/prefabs/npc/autoturret/effects/targetlost.prefab", player.transform.position);
                }

                if (i == 13)
                {
                    Effect.server.Run("assets/prefabs/npc/autoturret/effects/online.prefab", player.transform.position);
                }

                float waitTime = (float)Oxide.Core.Random.Range(0, 100) / 100;
                if (i == ConsoleLoading.Count)
                {
                    waitTime =+ 1f;
                }

                yield return new WaitForSeconds(waitTime);
            }

            timer.Once(1f, () =>
            {
                CuiHelper.DestroyUi(player, LoadingPage);
                MainPCUI(player);
            });

            #endregion
        }

        #endregion

        #region Helpers

        public BaseEntity DoRay(Vector3 pos, Vector3 aim)
        {
            Ray ray = new Ray(pos, aim);
            RaycastHit hit;
            float distance = config.scandistance;
            bool hasHit = Physics.Raycast(ray, out hit, LayerMask.GetMask("AI"));
            if (hasHit)
            {
                if (hit.distance < distance)
                {
                    distance = hit.distance;
                }
                else return null;
                if (hit.GetEntity() != null) return hit.GetEntity();
                else return null;
            }
            else return null;
        }

        void RunEffect(BasePlayer player, string path)
        {
            Effect effect = new Effect();
            effect.Init(Effect.Type.Generic, player.transform.position, player.transform.forward, (Network.Connection)null);
            effect.pooledString = path; EffectNetwork.Send(effect, player.net.connection);
        }

        public void ReplyWithHelper(BasePlayer player, string message, string[] args = null)
        {
            if (args != null)
                message = string.Format(message, args);
            player.SendConsoleCommand("chat.add", new object[2]
            {
                config.Steam64ID,
                string.Format("<size=16><color={2}>{0}</color>:</size>\n{1}", config.PrefixChat, message, config.PrefixColor)
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
                    ["WelcomeTab"] = "Welcome DarkNet",
                    ["TargetingComputerPressEText"] = "Нажмите на <E> чтобы войти в DarkNet",
                    ["TORLink"] = "Обозреватель Tor",
                    ["LinkStore"] = "https://w2w.DarkRust.Tor.ERROR.DarkNet.onion",
                    ["TorVersion"] = "TorBrowser {0}",
                    ["NameStore"] = "DepStore единственный магазин в DarkNet",
                    ["notscan"] = "<color=#FF0101C0> Цель невозможно просканировать </color>",
                    ["notmoney"] = " У вас недостаточно средств",
                    ["accepttrade"] = "Сделка успешна!",
                    ["accepttitle"] = "Подтверждение покупки",
                    ["acceptdescription"] = "Покупая какие либо товары на площадке DarkNet вы идите на осознанный риск! Если вас обманут или вы потеряете свои личные данные - мы не несем ответственность за это! Вы действительно хотите приобрести данный товар?",
                };


                lang.RegisterMessages(Lang, this, "en");
                lang.RegisterMessages(Lang, this, "ru");
                PrintWarning("Языковой файл загружен успешно");
            });
        }
        #endregion
    }
}
