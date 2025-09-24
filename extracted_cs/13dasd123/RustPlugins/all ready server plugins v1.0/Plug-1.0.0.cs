using System;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Plug", "Kill me", "1.0.0")]
    [Description("Добавляет на ваш сервер возможность курить траву, приобретено на whiteplugins.ru")]
    public  class Plug : RustPlugin
    {
        /*
         * МЫ НИЧЕГО НЕ ПРОПАГАНДИРУЕМ, УПОТРЕБЛЕНИЕ И РАСПРОСТРАНЕНИЕ НАРКОТИЧЕСКИХ ВЕЩЕСТ КАРАЕТСЯ ЗАКОНОМ!
         * НАРКОТИКИ ЭТО ЗЛО!
         */
        
        #region Cfg [Конфигурация]
        
        private Configuration config { get; set; }
        
        private class Configuration
        {
            internal class BuyPerm
            {
                [JsonProperty("Shortname предмета для покупка <статуса дилера>")]
                public string Shortname { get; set; }
                [JsonProperty("Кол-во для покупки <статуса дилера>")]
                public int Amount { get; set; }
                [JsonProperty("Пермиссион(<статус дилера>)")]
                public string Perm { get; set; }
            }

            internal class Bong
            {
                [JsonProperty("Shortname предмета выступающего в роли бонга")]
                public string Shortname { get; set; }
                [JsonProperty("SkinID бонга")]
                public ulong SkinID { get; set; }
                [JsonProperty("Название предмета(Отображается в инвентаре)")]
                public string DisplayName { get; set; }
            }
            internal class Guiset
            {
                [JsonProperty("Текст с описанием в панеле покупки статуса")]
                public string BText { get; set; }
                [JsonProperty("Текст с описанием в панеле обмена сырья")]
                public string TText { get; set; }
                
                [JsonProperty("Текст внизу в панеле покупки статуса")]
                public string BSText { get; set; }
                [JsonProperty("Текст внизу в панеле обмена сырья")]
                public string TSText { get; set; }
            }
            
            internal class Drugs
            {
                [JsonProperty("Shortname предмета выступающего в роли сырья для создания нарк*тиков")]
                public string Shortname { get; set; }
                [JsonProperty("Кол-во сырья для создания нарк*тика")]
                public int cam { get; set; }
                [JsonProperty("Кол-во получаемого нарк*тика")]
                public int oud { get; set; }
                [JsonProperty("Отображаемое имя нарк*тика")]
                public string DisplayName { get; set; }
                [JsonProperty("SkinID нарк*тика")]
                public ulong SkinID { get; set; }
            }

            internal class Effects
            {           
                [JsonProperty("На сколько будет хилить игрока?")]
                public int Hp { get; set; }
                [JsonProperty("Сколько игроку даём кровотечения? (0 - выкл)")]
                public int Bleeding { get; set; }
                [JsonProperty("Сколько даёт воды?")]
                public int Water { get; set; }
                [JsonProperty("Сколько даёт еды?")]
                public int Eat { get; set; }
            }
            
            [JsonProperty("Настройка покупки <статуса дилера>", Order = 0)]
            public BuyPerm bp = new BuyPerm();
            
            [JsonProperty("Настройка бонга!", Order = 1)]
            public Bong bg = new Bong();
            
            [JsonProperty("Настройка нарк*тика!", Order = 3)]
            public Drugs dg = new Drugs();
            
            [JsonProperty("Настройка эффектов!", Order = 4)]
            public Effects ef = new Effects();
            
            [JsonProperty("Настройка гуи!", Order = 5)]
            public Guiset gui = new Guiset();

            public static Configuration GetNewCong()
            {
                return new Configuration
                {
                    bp = new BuyPerm
                    {
                        Amount = 5,
                        Perm = "plug.use"
                    },
                    bg = new Bong
                    {
                        Shortname = "battery.small",
                        DisplayName = "Бонг",
                        SkinID = 1646611210
                    },
                    dg = new Drugs
                    {
                        DisplayName = "Шишки",
                        SkinID = 1635126133,
                        cam = 10,
                        oud = 5,
                    },
                    ef = new Effects
                    {
                        Hp = 10,
                        Bleeding = 10,
                        Water = 100,
                        Eat = 100
                    },
                    gui = new Guiset
                    {
                        BText = $"Привет, хочешь попасть к нам? \nУ нас можно выгодно обменять сырьё на хороший товар. \nЕсли ты захотел попасть, принеси нам сырьё.\nТак мы поймем, достоин ли ты стать дилером!",
                        TText = $"Собирай срезы и обменивай их на хороший товар!\nВозьми бонг в активный слот и нажми на кнопку E",
                        BSText = $"Сырьё - срез конопли. посадите коноплю и сделайте срез!\nпосле того как вы нашли нужное кол-во срезов,\nпросто нажмите на картинку!",
                        TSText = $"ПОМНИ! НАРКОТИКИ ЭТО ЗЛО!    "
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
                if (config?.dg == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Что то с этим конфигом не так! 'oxide/config/{Name}', создаём новую конфигурацию!");
                LoadDefaultConfig();
            }
             
            NextTick(SaveConfig);
        }
         
        protected override void LoadDefaultConfig() => config = Configuration.GetNewCong();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Hooks [хуки]
        
        [PluginReference] Plugin ImageLibrary;

        #region Init [Инициализация]

        private void OnServerInitialized()
        {  
            if (!plugins.Exists("ImageLibrary"))
            {
                PrintError("Плагин ImageLibrary не загружен");
            }
            
            permission.RegisterPermission(config.bp.Perm, this);
            
            //при покупке
            ImageLibrary.Call("AddImage", $"https://rustlabs.com/img/items180/clone.hemp.png", "f");  
 
            SaveConfig();
        }

        #endregion

        #region Check  [Проверка]
        
        void OnPlayerInput(BasePlayer player, InputState input, Item item = null)
        {
            Item activeItem = new Item();
            if (item != null) activeItem = item;
            else activeItem = player.GetActiveItem();
            if (input.WasJustPressed(BUTTON.USE))
            {
                if (activeItem != null)
                {
                    if (activeItem.skin == config.bg.SkinID)
                    {
                        Bg(player);
                    }
                }
            }
        }

        #endregion

        #region ef [Эффекты]

        private void Bg(BasePlayer player)
        {        
            int amount;
            foreach (var cc in player.inventory.AllItems().Where(p => p.skin == config.dg.SkinID))
            {
                var g = ItemManager.FindItemDefinition("xmas.window.garland").itemid;
                
                if (cc.amount >= 1)
                {
                    player.inventory.Take(null, g, 1);
                }
                else
                {
                    SendReply(player, "Увы, но у тебя не хватит чтобы прикурить :(");
                    return;
                }
                
               /* EffectNetwork.Send( new Effect("assets/prefabs/npc/murderer/sound/breathing.prefab", player, 0,Vector3.zero, Vector3.forward), player.net.connection);
                EffectNetwork.Send( new Effect("assets/bundled/prefabs/fx/screen_jump.prefab", player, 0,Vector3.zero, Vector3.forward), player.net.connection);
                EffectNetwork.Send( new Effect("assets/prefabs/weapons/hatchet/effects/strike_screenshake.prefab", player, 0,Vector3.zero, Vector3.forward), player.net.connection);*/
                player.metabolism.bleeding.value = config.ef.Bleeding;
                player.metabolism.calories.value = config.ef.Eat;
                player.metabolism.hydration.value = config.ef.Water;
                player.Heal(config.ef.Hp);
            }
        }

        #endregion

        #region Give

        bool GiveDrugs(ItemContainer container)
        {
            var item = ItemManager.CreateByName("xmas.window.garland", config.dg.oud, config.dg.SkinID);
            item.name = config.dg.DisplayName;
            return item.MoveToContainer(container, -1, false);
        }

        bool GiveDrugs(BasePlayer player)
        {
            var item = ItemManager.CreateByName("xmas.window.garland", config.dg.oud, config.dg.SkinID);
            item.name = config.dg.DisplayName;
            if (!player.inventory.GiveItem(item))
            {
                item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity, new Quaternion());
                return false;
            }
            return true;
        }
        bool GiveBong(ItemContainer container)
        {
            var item = ItemManager.CreateByName(config.bg.Shortname, 1, config.bg.SkinID);
            item.name = config.bg.DisplayName;
            return item.MoveToContainer(container, -1, false);
        }

        bool GiveBong(BasePlayer player)
        {
            var item = ItemManager.CreateByName(config.bg.Shortname, 1, config.bg.SkinID);
            item.name = config.bg.DisplayName;
            if (!player.inventory.GiveItem(item))
            {
                item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity, new Quaternion());
                return false;
            }
            return true;
        }

        #endregion

        #endregion

        #region Command [Команды]

        #region Open UI [Открывает интерфейс]

        [ChatCommand("plug")]
        private void CmdDrawP(BasePlayer player)
        {
            if ( !permission.UserHasPermission(player.UserIDString, config.bp.Perm))
            {
                Buy(player);
                return;
            }
            Main(player);  
        }

        #endregion

        #region Buy/trade [Покупка/обмен]

        [ConsoleCommand("PLUG_DG")]
        private void CmdTradeBuy(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            int amount;
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, config.bp.Perm))
            {
                return;
            }

            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "buyperm")
                {
                    var g = player.inventory.GetAmount(-886280491);
                
                    if (g >= config.bp.Amount)
                    {
                        player.inventory.Take(null, -886280491, config.bp.Amount);
                    }
                    else
                    {
                        SendReply(player, "Увы, но у тебя не хватает травы чтобы стать дилером:(");
                        return;
                    }
                    SendReply(player, "Теперь ты дилер, введи команду ещё раз!");
                    Server.Command($"oxide.grant user {player.userID} {config.bp.Perm}");
                }

                if (args.Args[0] == "tradedrug")
                {
                    bool enough = true;

                    var v = player.inventory.GetAmount(-886280491);
                    if (v >= config.dg.cam)
                    {
                        player.inventory.Take(null, -886280491, config.dg.cam);
                    }
                    else
                    {
                        SendReply(player, "У тебя не хватает травы :(");
                        return;
                    }

                    GiveDrugs(player);
                    
                    SendReply(player, "Успешно! Сегодня у тебя будет хорошое настроение :)");

                    GiveBong(player);
                    
                    SendReply(player, "Держи бонг!");     
                }               
            }
        }

        #endregion

        #region Give drugs / bong

        [ChatCommand("p.givep")]
        private void CmdGiv(BasePlayer player)
        {
            if (!player.IsAdmin) return;
            if (GiveBong(player))
            {
                SendReply(player, "Бонг выдан");
            }
            if (GiveDrugs(player))
            {
                SendReply(player, "Наркотик выдан");
            }
        }

        #endregion

        #endregion

        #region Interface [Визуализация]

        private string bb = "ui_buy";
        private string mm = "ui_main";

        #region Buy [Покупка пермиссиона]

        private void Buy(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat("#FFFFFF00") },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1"},
                CursorEnabled = true,
            }, "Overlay", bb);
            
            container.Add(new CuiElement
            {
                Parent = bb,
                Components =
                {
                    new CuiRawImageComponent { Color = "0 0 0 0.85", FadeIn = 0.25f, Sprite = "assets/content/ui/ui.background.tiletex.psd", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = bb,
                Components =
                {
                    new CuiTextComponent {Text = "НАРКОПРИТОН", Align = TextAnchor.MiddleCenter, FontSize = 25, FadeIn = 0.25f, Font = "RobotoCondensed-regular.ttf"},
                    new CuiRectTransformComponent { AnchorMin = "0.0003660321 0.9479167", AnchorMax = "0.999634 0.9999964"}
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = bb,
                Components =
                {
                    new CuiTextComponent {Text = config.gui.BText, Align = TextAnchor.MiddleCenter, FadeIn = 0.27f, FontSize = 20, Font = "RobotoCondensed-regular.ttf"},
                    new CuiRectTransformComponent { AnchorMin = "0.0003660321 0.7565105", AnchorMax = "0.999634 0.9518201"}
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = bb,
                Components =
                {
                    new CuiTextComponent {Text = $"ВАМ НУЖНО ДОСТАТЬ:", Align = TextAnchor.MiddleCenter, FadeIn = 0.29f, FontSize = 20, Font = "RobotoCondensed-regular.ttf"},
                    new CuiRectTransformComponent { AnchorMin = "0.0003660321 0.7174515", AnchorMax = "0.999634 0.7695312"}
                }
            });

            container.Add(new CuiElement
            {
                Parent = bb,
                Components =
                {
                    new CuiRawImageComponent {Png = (string) ImageLibrary?.Call("GetImage", "f"), FadeIn = 0.25f},
                    new CuiRectTransformComponent {AnchorMin = "0.4158126 0.4296875", AnchorMax = "0.5841874 0.7096375"}
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = bb,
                Components =
                {
                    new CuiTextComponent {Text =  config.gui.BSText, Align = TextAnchor.UpperCenter, FadeIn = 0.27f, FontSize = 18, Font = "RobotoCondensed-regular.ttf"},
                    new CuiRectTransformComponent { AnchorMin = "0.0003660321 2.467306E-06", AnchorMax = "0.999634 0.3880209"}
                }
            });
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = {Close = bb, Color = HexToRustFormat("#FFFFFF00")},
                Text = { Text = ""},
            }, bb);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.4158126 0.4296875", AnchorMax = "0.5841874 0.7096375" },
                Button = {Close = bb, Command = "PLUG_DG buyperm", Color = HexToRustFormat("#FFFFFF01"), Material = "assets/content/ui/ui.background.tiletex.psd"},
                Text = { Text = $"{config.bp.Amount}", Align = TextAnchor.MiddleCenter, FadeIn = 0.20f, FontSize = 25, Font = "RobotoCondensed-regular.ttf"},
            }, bb);

            CuiHelper.DestroyUi(player, bb);
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Main [Панелька обмена]

        private void Main(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat("#FFFFFF00") },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1"},
                CursorEnabled = true,
            }, "Overlay", mm);
            
            container.Add(new CuiElement
            {
                Parent = mm,
                Components =
                {
                    new CuiRawImageComponent { Color = "0 0 0 0.85", FadeIn = 0.25f, Sprite = "assets/content/ui/ui.background.tiletex.psd", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = mm,
                Components =
                {
                    new CuiTextComponent {Text = "НАРКОПРИТОН", Align = TextAnchor.MiddleCenter, FontSize = 25, FadeIn = 0.25f, Font = "RobotoCondensed-regular.ttf"},
                    new CuiRectTransformComponent { AnchorMin = "0.0003660321 0.9479167", AnchorMax = "0.999634 0.9999964"}
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = mm,
                Components =
                {
                    new CuiTextComponent {Text = config.gui.TText, Align = TextAnchor.MiddleCenter, FadeIn = 0.27f, FontSize = 20, Font = "RobotoCondensed-regular.ttf"},
                    new CuiRectTransformComponent { AnchorMin = "0.0003660321 0.7565105", AnchorMax = "0.999634 0.9518201"}
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = mm,
                Components =
                {
                    new CuiTextComponent {Text = config.gui.TSText, Align = TextAnchor.UpperCenter, FadeIn = 0.27f, FontSize = 18, Font = "RobotoCondensed-regular.ttf"},
                    new CuiRectTransformComponent { AnchorMin = "0.0003660321 2.467306E-06", AnchorMax = "0.999634 0.3880209"}
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = mm,
                Components =
                {
                    new CuiRawImageComponent {Png = (string) ImageLibrary?.Call("GetImage", "f"), FadeIn = 0.25f},
                    new CuiRectTransformComponent {AnchorMin = "0.4158126 0.4296875", AnchorMax = "0.5841874 0.7096375"}
                }
            });
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = {Close = mm, Color = HexToRustFormat("#FFFFFF00")},
                Text = { Text = ""},
            }, mm);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.4158126 0.4296875", AnchorMax = "0.5841874 0.7096375"},
                Button = {Command = "PLUG_DG tradedrug", Color = HexToRustFormat("#FFFFFF03")},
                Text = { Text = $"ОБМЕНЯТЬ СЫРЬЁ:\n{config.dg.cam} к {config.dg.oud}", Align = TextAnchor.MiddleCenter, FadeIn = 0.20f, FontSize = 20, Font = "RobotoCondensed-regular.ttf"},
            }, mm);

            CuiHelper.DestroyUi(player, mm);
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #endregion

        #region Utils. [Всякая херня]

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

            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        #endregion
    }
