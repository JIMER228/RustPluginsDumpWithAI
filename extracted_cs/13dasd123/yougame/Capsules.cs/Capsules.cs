using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Capsules", "SERVER-RUST(fix HUZAKI)","1.0.3")]
    [Description("SERVER-RUST(fix HUZAKI)")] 
    class Capsules : RustPlugin
    {
        #region [Дата+Ланг]
        protected override void LoadDefaultMessages()
         {
             var ru = new Dictionary<string, string>()
             {
                 ["Lable"] = "<size=40>КАПСУЛЫ</size>\nИспытай свою удачу открыв одну из капсул.",
                 ["AddExp"] = "[CAPSULES] Вы получили {0} EXP",
                 ["InterMoney"] = "[CAPSULES] Вам поступило на счет ${0}",
                 ["Bonus"] = "Вы уже получали бонус!\nСледующий через",
                 ["NHave"] = "У вас недостаточно денег",
                 ["Balic"] = "Ваш баланс {0}$",
                 ["Bonus2"] = "Получить бонус",
                 ["BUY"] = "КУПИТЬ",
                 ["Take"] = "Вы забрали капсулу",
             }.ToDictionary(rus => rus.Key, rus => rus.Value);
             lang.RegisterMessages(ru, this, "ru");
             var en = new Dictionary<string, string>()
             {
                 ["Lable"] = "<size=40>CAPSULES</size>\nTry your luck by opening one of the capsules.",
                 ["AddExp"] = "[CAPSULES] You received {0} EXP",
                 ["InterMoney"] = "[CAPSULES] You received an invoice ${0}",
                 ["Bonus"] = "You have already received a bonus!\n Next through",
                 ["NHave"] = "You don't have enough money",
                 ["Balic"] = "Your balance {0}$",
                 ["BUY"] = "BUY",
                 ["Bonus2"] = "Receive a bonus",
                 ["Take"] = "You took the capsule",
             }.ToDictionary(eng => eng.Key, eng => eng.Value);
             lang.RegisterMessages(en, this);
         }
        private CapsulsConfig Settings { get; set; }
        private Dictionary<ulong, PlayerDat> _playerDatas = new Dictionary<ulong, PlayerDat>();

        public class CapsulsConfig
        {
            [JsonProperty("Бонус")] public int cost;
            [JsonProperty("Сколько нужно EXP, чтобы получить денег?")] public int exp;
            [JsonProperty("Сколько денег за EXP?")] public int expCost;
            [JsonProperty("Exp за ресурсы включить?")] public bool ExpDespTrue;
            [JsonProperty("Сколько нужно добыть для получения?")] public Dictionary<string, int> ResGather = new Dictionary<string, int>();
            [JsonProperty("Exp сколько(За ресурсы)?")] public int ExpDesp;
            [JsonProperty("Перезарядка бонуса ")] public string BonusCd; 
            [JsonProperty("Капсулы")] public List<Capsuls> capsuls = new List<Capsuls>();

            
            public class Capsuls
            {
                [JsonProperty("Название капсулы")] public string name;
                [JsonProperty("Ссылка на картинку")] public string image;
                [JsonProperty("Цена")] public int cost;
                [JsonProperty("СкинАйди")] public ulong skinId;
                [JsonProperty("Предметы")] public List<ItemList> itemList = new List<ItemList>();
            }

            public class ItemList
            {
                [JsonProperty("Шортнейм предмета(Если картинка,то название ее)")] public string shortname;
                [JsonProperty("Команда")] public string Command;
                [JsonProperty("Если конманда то ссылку на картинку")] public string url;
                [JsonProperty("Кол-во")] public int Ammount;
            }

            public static CapsulsConfig GetNewConf()
            {
                CapsulsConfig newConfig = new CapsulsConfig();
                newConfig.cost = 5;
                newConfig.exp = 150;
                newConfig.expCost = 50;
                newConfig.ExpDespTrue = true;
                newConfig.ResGather = new Dictionary<string, int>()
                {
                    {"metal.ore", 2500},
                    {"stones", 5000},
                    {"sulfur.ore", 2000},
                    {"hq.metal.ore", 150}
                };
                newConfig.ExpDesp = 5;
                newConfig.BonusCd = "1d";
                newConfig.capsuls = new List<Capsuls>
                {
                    new Capsuls()
                    {
                        name = "Resourse",
                        image = "https://imgur.com/ovnDS7w.png",
                        skinId = 1989523189,
                        cost = 50,
                        itemList = new List<ItemList>()
                        {
                            new ItemList()
                            {
                                shortname = "wood",
                                Command = "",
                                url = "",
                                Ammount = 2500
                            },
							new ItemList()
                            {
                                shortname = "stones",
                                Command = "",
                                url = "",
                                Ammount = 2500
                            },
							new ItemList()
                            {
                                shortname = "metal.fragments",
                                Command = "",
                                url = "",
                                Ammount = 2000
                            },
							new ItemList()
                            {
                                shortname = "metal.refined",
                                Command = "",
                                url = "",
                                Ammount = 150
                            },
							new ItemList()
                            {
                                shortname = "sulfur",
                                Command = "",
                                url = "",
                                Ammount = 1500
                            },
                            new ItemList()
                            {
                                shortname = "scrap",
                                Command = "",
                                url = "",
                                Ammount = 200
                            },
                            new ItemList()
                            {
                                shortname = "explosives",
                                Command = "",
                                url = "",
                                Ammount = 5
                            },
                            new ItemList()
                            {
                                shortname = "gunpowder",
                                Command = "",
                                url = "",
                                Ammount = 1000
                            },
                            new ItemList()
                            {
                                shortname = "leather",
                                Command = "",
                                url = "",
                                Ammount = 250
                            },
                            new ItemList()
                            {
                                shortname = "lowgradefuel",
                                Command = "",
                                url = "",
                                Ammount = 250
                            },
                            new ItemList()
                            {
                                shortname = "cloth",
                                Command = "",
                                url = "",
                                Ammount = 300
                            },
                            new ItemList()
                            {
                                shortname = "charcoal",
                                Command = "",
                                url = "",
                                Ammount = 1500
                            },
                        }
                    },
                    new Capsuls()
                    {
                        name = "Weapon",
                        image = "https://imgur.com/qJhg5sO.png",
                        skinId = 1989523352,
                        cost = 50,
                        itemList = new List<ItemList>()
                        {
                            new ItemList()
                            {
                                shortname = "rifle.semiauto",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "pistol.revolver",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "pistol.python",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "shotgun.waterpipe",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "shotgun.pump",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "pistol.semiauto",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "pistol.nailgun",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "smg.mp5",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "smg.2",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "smg.thompson",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "pistol.m92",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "rifle.bolt",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                        }
                    },
                    new Capsuls()
                    {
                        name = "Components",
                        image = "https://imgur.com/fz8qgOw.png",
                        skinId = 1989523626,
                        cost = 50,
                        itemList = new List<ItemList>()
                        {
                            new ItemList()
                            {
                                shortname = "metalblade",
                                Command = "",
                                url = "",
                                Ammount = 5
                            },
                            new ItemList()
                            {
                                shortname = "gears",
                                Command = "",
                                url = "",
                                Ammount = 5
                            },
                            new ItemList()
                            {
                                shortname = "propanetank",
                                Command = "",
                                url = "",
                                Ammount = 15
                            },
                            new ItemList()
                            {
                                shortname = "metalpipe",
                                Command = "",
                                url = "",
                                Ammount = 10
                            },
                            new ItemList()
                            {
                                shortname = "roadsigns",
                                Command = "",
                                url = "",
                                Ammount = 4
                            },
                            new ItemList()
                            {
                                shortname = "rope",
                                Command = "",
                                url = "",
                                Ammount = 15
                            },
                            new ItemList()
                            {
                                shortname = "sewingkit",
                                Command = "",
                                url = "",
                                Ammount = 15
                            },
                            new ItemList()
                            {
                                shortname = "sheetmetal",
                                Command = "",
                                url = "",
                                Ammount = 4
                            },
                            new ItemList()
                            {
                                shortname = "metalspring",
                                Command = "",
                                url = "",
                                Ammount = 5
                            },
                            new ItemList()
                            {
                                shortname = "tarp",
                                Command = "",
                                url = "",
                                Ammount = 15
                            },
                            new ItemList()
                            {
                                shortname = "smgbody",
                                Command = "",
                                url = "",
                                Ammount = 3
                            },
                            new ItemList()
                            {
                                shortname = "semibody",
                                Command = "",
                                url = "",
                                Ammount = 2
                            },
                        }
                    },
                    new Capsuls()
                    {
                        name = "Mixed",
                        image = "https://imgur.com/pW4cziZ.png",
                        skinId = 1989523872,
                        cost = 50,
                        itemList = new List<ItemList>()
                        {
                            new ItemList()
                            {
                                shortname = "wood",
                                Command = "",
                                url = "",
                                Ammount = 2500
                            },
                            new ItemList()
                            {
                                shortname = "stones",
                                Command = "",
                                url = "",
                                Ammount = 2500
                            },
                            new ItemList()
                            {
                                shortname = "metal.fragments",
                                Command = "",
                                url = "",
                                Ammount = 2000
                            },
                            new ItemList()
                            {
                                shortname = "sewingkit",
                                Command = "",
                                url = "",
                                Ammount = 15
                            },
                            new ItemList()
                            {
                                shortname = "sheetmetal",
                                Command = "",
                                url = "",
                                Ammount = 4
                            },
                            new ItemList()
                            {
                                shortname = "metalspring",
                                Command = "",
                                url = "",
                                Ammount = 5
                            },
                            new ItemList()
                            {
                                shortname = "rifle.semiauto",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "pistol.revolver",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "pistol.python",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "ladder.wooden.wall",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "roadsign.gloves",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "metal.facemask",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                        }
                    },
                    new Capsuls()
                    {
                        name = "Food",
                        image = "https://imgur.com/TNagGiv.png",
                        skinId = 1989524045,
                        cost = 50,
                        itemList = new List<ItemList>()
                        {
                            new ItemList()
                            {
                                shortname = "granolabar",
                                Command = "",
                                url = "",
                                Ammount = 4
                            },
                            new ItemList()
                            {
                                shortname = "chicken.cooked",
                                Command = "",
                                url = "",
                                Ammount = 5
                            },
                            new ItemList()
                            {
                                shortname = "deermeat.cooked",
                                Command = "",
                                url = "",
                                Ammount = 5
                            },
                            new ItemList()
                            {
                                shortname = "horsemeat.cooked",
                                Command = "",
                                url = "",
                                Ammount = 5
                            },
                            new ItemList()
                            {
                                shortname = "bearmeat.cooked",
                                Command = "",
                                url = "",
                                Ammount = 5
                            },
                            new ItemList()
                            {
                                shortname = "wolfmeat.cooked",
                                Command = "",
                                url = "",
                                Ammount = 5
                            },
                            new ItemList()
                            {
                                shortname = "meat.pork.cooked",
                                Command = "",
                                url = "",
                                Ammount = 5
                            },
                            new ItemList()
                            {
                                shortname = "waterjug",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "fish.cooked",
                                Command = "",
                                url = "",
                                Ammount = 3
                            },
                            new ItemList()
                            {
                                shortname = "chocholate",
                                Command = "",
                                url = "",
                                Ammount = 4
                            },
                            new ItemList()
                            {
                                shortname = "can.tuna",
                                Command = "",
                                url = "",
                                Ammount = 2
                            },
                            new ItemList()
                            {
                                shortname = "can.beans",
                                Command = "",
                                url = "",
                                Ammount = 2
                            },
                        }
                    },
                    new Capsuls()
                    {
                        name = "Luckys",
                        image = "https://imgur.com/RyTmRj5.png",
                        skinId = 1989524299,
                        cost = 50,
                        itemList = new List<ItemList>()
                        {
                            new ItemList()
                            {
                                shortname = "guntrap",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "metal.plate.torso",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "riflebody",
                                Command = "",
                                url = "",
                                Ammount = 3
                            },
                            new ItemList()
                            {
                                shortname = "bed",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "roadsign.jacket",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "hazmatsuit",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "metal.facemask",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "wall.frame.garagedoor",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "wall.external.high.stone",
                                Command = "",
                                url = "",
                                Ammount = 3
                            },
                            new ItemList()
                            {
                                shortname = "gates.external.high.stone",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "door.hinged.toptier",
                                Command = "",
                                url = "",
                                Ammount = 1
                            },
                            new ItemList()
                            {
                                shortname = "ammo.rocket.basic",
                                Command = "",
                                url = "",
                                Ammount = 2
                            },
                        }
                    },
                };
                return newConfig;
            }
        }

        protected override void LoadDefaultConfig() => Settings = CapsulsConfig.GetNewConf();
        protected override void SaveConfig() => Config.WriteObject(Settings);

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                Settings = Config.ReadObject<CapsulsConfig>();
                if (Settings?.capsuls == null) LoadDefaultConfig();
            }
            catch
            {
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        private class PlayerDat
        {
            [JsonProperty("Ник")]
            public string Name;
            [JsonProperty("Деньги")]
            public int Money;
            [JsonProperty("Exp")]
            public int Exp;
            [JsonProperty("Перезагрузка бонуса")]
            public double BonusTime = CurrentTime();
            
            public double IsCooldown()
            {
                return Math.Max(BonusTime - CurrentTime(), 0);
            }
        }
        #endregion

        #region [CMD]

        [ChatCommand("capsules")]
        void OpenCapsulsMenu(BasePlayer player)
        {
            StartUi(player);
        }

        [ConsoleCommand("capsules")]
        private void UiCommandsAdmin(ConsoleSystem.Arg arg)
        {
            if(arg.Player() != null && !permission.UserHasPermission(arg.Player().UserIDString, "capsules.admin")) return;
            if (arg?.Args == null)
            {
                Puts("Э не туда лезешь!");
                return;
            }

            if (arg.Args.Length < 3 || arg.Args.Length > 3)
            {
                Puts("Вы пишите что-то не так!");
                return;
            }
            ulong s;
            if(!ulong.TryParse(arg.Args[0], out s))             
            {
                Puts("Вы пишите что-то не так!");
                return; 
            }
            PlayerDat t;
            int p;
            if (!int.TryParse(arg.Args[2], out p))
            {
                Puts("Вы пишите что-то не так!");
                return; 
            }
            if (!_playerDatas.TryGetValue(s, out t))
            {
                Puts("Player not found");
                return;
            }
            switch (arg.Args[1])
            {
                case "money":
                    t.Money += p;
                    Puts($"Вы выдали игрок {t.Name} {p}$");
                    break;
                case "exp":
                    t.Exp += p;
                    Puts($"Вы выдали игрок {t.Name} {p} EXP");
                    break;
                default:
                    Puts($"money/exp");
                    break;
            }
            return;
        }
        
        
        [ConsoleCommand("UI_Capsuls")]
        private void UiCommands(ConsoleSystem.Arg arg)
        {
            if (arg?.Args == null || arg.Args.Length < 1)
            {
                Puts("Э не туда лезешь!");
                return;
            }
            var targetPlayer = arg.Player();
            if (targetPlayer == null)
            {
                Puts("Э не туда лезешь!");
                return;
            }
            PlayerDat t;
            if(!_playerDatas.TryGetValue(targetPlayer.userID,out t)) return;
            switch (arg.Args[0])
            {
                case "destroy" :
                    CuiHelper.DestroyUi(targetPlayer, Layer);
                    break;
                case "back" :
                    CuiHelper.DestroyUi(targetPlayer, Layer);
                    StartUi(targetPlayer);
                    break;
                case "bonus" :
                    
                    if (t.IsCooldown() <= 0)
                    {
                        _playerDatas[targetPlayer.userID].Money += Settings.cost;
                        _playerDatas[targetPlayer.userID].BonusTime = CurrentTime() + TimeToSeconds(Settings.BonusCd);
                        StartUi(targetPlayer);
                        GiveUi(targetPlayer, string.Format(lang.GetMessage("InterMoney", this, targetPlayer.UserIDString), Settings.cost), 14);
                    }
                    else
                        GiveUi(targetPlayer, $"{lang.GetMessage("Bonus", this, targetPlayer.UserIDString)} {FormatTime(TimeSpan.FromSeconds(t.IsCooldown()), lang.GetLanguage(targetPlayer.UserIDString))}", 14);
                    break;
                case "nextpage" :
                    CuiHelper.DestroyUi(targetPlayer, Layer + "NextPage");
                    CuiHelper.DestroyUi(targetPlayer, Layer + "NextPage1");
                    LoadCapsules(targetPlayer, arg.Args[1].ToInt());
                    break;
                case "capsula" :
                    var argList = arg.Args.ToList();
                    argList.RemoveAt(0);
                    var caps = string.Join(" ", argList.ToArray());
                    CapsulitemsCapsules(targetPlayer, caps);
                    break;
                case "givecapsula" :
                    var argLists = arg.Args.ToList();
                    argLists.RemoveAt(0);
                    var capss = string.Join(" ", argLists.ToArray());
                    var findCapsuls = Settings.capsuls.FirstOrDefault(p => p.name == capss);
                    if(findCapsuls == null) return;
                    if (t.Money >= findCapsuls.cost)
                    {
                        t.Money -= findCapsuls.cost;
                        GiveItem(targetPlayer, capss);
                    }
                    else
                        GiveUi(targetPlayer,lang.GetMessage("NHave", this, targetPlayer.UserIDString), 18);
                    break;
            }
        }
        #endregion
        #region [UI]

        private static string Overlay = "Overlay";
        private static string Layer = "CapsulsUI";
        private CuiPanel MainUiPanel = new CuiPanel()
        {
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-1920 -1080", OffsetMax = "1920 1080"},
            Image = {Color = "0 0 0 0"}
        };
        private CuiPanel Fon = new CuiPanel()
        {
            CursorEnabled = true,
            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1"},
            Image = {Color = HexToRustFormat("#4B5B3CFF"), Material = "assets/content/ui/uibackgroundblur.mat", Sprite = "assets/content/ui/ui.background.transparent.radial.psd"}
        };
        private CuiPanel Fon2 = new CuiPanel()
        {
            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1"},
            Image = {Color = "0 0 0 0.8"}
        };
        private CuiButton DestroyCuiButton = new CuiButton()
        {
            RectTransform = {AnchorMin = "0.6118051 0.3404321", AnchorMax = "0.6578121 0.3629629"},
            Button = {Color = "0 0 0 0", Command = "UI_Capsuls destroy"},
            Text = {Text = "Покинуть", Align = TextAnchor.MiddleCenter, FontSize = 20}
        };
        private CuiButton BackButton = new CuiButton()
        {
            RectTransform = {AnchorMin = "0.6118051 0.3404321", AnchorMax = "0.6578121 0.3629629"},
            Button = {Color = "0 0 0 0", Command = "UI_Capsuls back"},
            Text = {Text = "Назад", Align = TextAnchor.MiddleCenter, FontSize = 20}
        };
        private CuiPanel ItemsPanel = new CuiPanel()
        {
            Image = {Color = "0 0 0 0"},
            RectTransform = {AnchorMin = "0.4487839 0.425926", AnchorMax = "0.5512125 0.6126543"}
        };
        private CuiElement ButtonVisual = new CuiElement()
        {
            Parent = Layer + "Main",
            Components =
            {
                new CuiTextComponent{Text = "------------------", FontSize = 15,Align = TextAnchor.LowerCenter, Color = HexToRustFormat("#00FF7CFF")},
                new CuiOutlineComponent{Color = HexToRustFormat("#00FF7CFF"), Distance = "1 -1"},
                new CuiRectTransformComponent{AnchorMin = "0.6118051 0.3404321", AnchorMax = "0.6578121 0.3629629"}
            }
        };
        private CuiElement IconDestroy = new CuiElement()
        {
            Parent = Layer + "Main",
            Components =
            {
                new CuiImageComponent(){Sprite = "assets/icons/exit.png", Color = "1 1 1 1"},
                new CuiRectTransformComponent{AnchorMin = "0.6517361 0.3410494", AnchorMax = "0.6625 0.3567901"}
            }
        };
        private CuiElement IconBack = new CuiElement()
        {
            Parent = Layer + "Main",
            Components =
            {
                new CuiImageComponent(){Sprite = "assets/icons/enter.png", Color = "1 1 1 1"},
                new CuiRectTransformComponent{AnchorMin = "0.6517361 0.3410494", AnchorMax = "0.6625 0.3567901"}
            }
        };
        private CuiElement IconBonus = new CuiElement()
        {
            Parent = Layer + "Main",
            Components =
            {
                new CuiImageComponent(){Sprite = "assets/icons/favourite_active.png",Color = HexToRustFormat("#F1FF00FF")},
                new CuiRectTransformComponent{AnchorMin = "0.4954861 0.3595673", AnchorMax = "0.503125 0.3728395"}
            }
        };
        private CuiElement ButtonBonusVisual = new CuiElement()
        {
            Parent = Layer + "Main",
            Components =
            {
                new CuiTextComponent{Text = "-------------------------------", FontSize = 15,Align = TextAnchor.LowerCenter, Color = HexToRustFormat("#00FF7CFF")},
                new CuiOutlineComponent{Color = HexToRustFormat("#00FF7CFF"), Distance = "1 -1"},
                new CuiRectTransformComponent{AnchorMin = "0.4763791 0.3404321", AnchorMax = "0.52239 0.3629629"}
            }
        };
        private void StartUi(BasePlayer player)
        {
            var cont = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layer);
            cont.Add(Fon, Overlay, Layer);
            cont.Add(Fon2, Layer);
            cont.Add(MainUiPanel, Layer, Layer + "Main");

            cont.Add(new CuiElement
            {
                Parent = Layer + "Main",
                Name = Layer + "Text",
                Components =
                {
                    new CuiTextComponent {Text = lang.GetMessage("Lable", this, player.UserIDString), FontSize = 15, Align = TextAnchor.MiddleCenter},
                    new CuiRectTransformComponent {AnchorMin = "0.4 0.5623451", AnchorMax = "0.6 0.6629624"}
                }
            });
            cont.Add(IconBonus);
            cont.Add(ButtonBonusVisual);
            PlayerDat t;
            if (!_playerDatas.TryGetValue(player.userID, out t)) return;
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Main",
                Components =
                {
                    new CuiImageComponent{Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur.mat"},
                    new CuiOutlineComponent(){Color = "1 1 1 1", Distance = "1 -1"},
                    new CuiRectTransformComponent(){AnchorMin = "0.3371531 0.3404321", AnchorMax = "0.3826389 0.3577161"}
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Main",
                Components =
                {
                    new CuiTextComponent{Color = "1 1 1 1", Text = $"EXP {t.Exp}/{Settings.exp}", Align = TextAnchor.MiddleCenter, FontSize = 20},
                    new CuiRectTransformComponent(){AnchorMin = "0.3371531 0.3404321", AnchorMax = "0.3826389 0.3577161"}
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Main",
                Components =
                {
                    new CuiTextComponent
                    {
                        Color = "1 1 1 1", Text = $"{string.Format(lang.GetMessage("Balic", this, player.UserIDString), t.Money)}", Align = TextAnchor.MiddleCenter, FontSize = 16
                        
                    },
                    new CuiRectTransformComponent(){AnchorMin = "0.3371531 0.3552465", AnchorMax = "0.3826389 0.3725305"}
                }
            });
            cont.Add(new CuiButton()
                    {
                        RectTransform = {AnchorMin = "0.4763791 0.3404321", AnchorMax = "0.52239 0.3629629"},
                        Button = {Color = "0 0 0 0", Command = "UI_Capsuls bonus"},
                        Text = {Text = lang.GetMessage("Bonus2", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 20}
                    }, Layer + "Main");
            CuiHelper.AddUi(player, cont);
            LoadCapsules(player, 0);
        }

        private void LoadCapsules(BasePlayer player, int page)
        {
            var cont = new CuiElementContainer();
            if (page > 0)
            {
                cont.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = "0.3336926 0.3339506", AnchorMax = "0.3543558 0.666358"},
                        Button = {Color = "0 0 0 0", Command = $"UI_Capsuls nextpage {page - 1}"},
                        Text = {Text = "<", FontSize = 50, Align =  TextAnchor.MiddleCenter},
                    }, Layer + "Main", Layer + "NextPage");
            }
            if (Settings.capsuls.Count > 6 * (1 + page))
            {
                cont.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = "0.6455005 0.3339506", AnchorMax = "0.6661602 0.666358"},
                        Button = {Color = "0 0 0 0", Command = $"UI_Capsuls nextpage {page + 1}"},
                        Text = {Text = ">", FontSize = 50, Align =  TextAnchor.MiddleCenter},
                    }, Layer + "Main", Layer + "NextPage1");
            }
            cont.Add(ButtonVisual);
            cont.Add(IconDestroy);
            cont.Add(DestroyCuiButton, Layer + "Main");
            foreach (var check in Settings.capsuls.Select((i, t) => new {A = i, B = t}))
                CuiHelper.DestroyUi(player, Layer + "Main" + check.B);
            foreach (var check in Settings.capsuls.Select((i, t) => new {A = i, B = t - page * 6})
                .Skip(page * 6).Take(6))
            {
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Main",
                    Name = Layer + "Main" + check.B,
                    Components =
                    {
                        new CuiRawImageComponent() {Color = "1 1 1 1", Png = GetImage(check.A.name)},
                        new CuiRectTransformComponent
                        {
                            AnchorMin =
                                $"{0.3644991 + check.B * 0.042- Math.Floor((double) check.B / 6) * 6 * 0.042} 0.4583337",
                            AnchorMax =
                                $"{0.4148469 + check.B * 0.042 - Math.Floor((double) check.B / 6) * 6 * 0.042} 0.5524694",
                        }
                    }
                });
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                    Button = {Color = "0 0 0 0", Command = $"UI_Capsuls capsula {check.A.name}"},
                    Text = { Text = $"{check.A.cost}$    ", Align = TextAnchor.UpperRight, FontSize = 22}
                },Layer + "Main" + check.B);
            }
            CuiHelper.AddUi(player, cont);
        }

        private void CapsulitemsCapsules(BasePlayer player, string capsule)
        {
            var cont = new CuiElementContainer();
            
            var findCapsul = Settings.capsuls.Find(p => p.name == capsule);
            CuiHelper.DestroyUi(player, Layer + "Main");
            cont.Add(MainUiPanel, Layer, Layer + "Main");
            cont.Add(ItemsPanel, Layer + "Main", Layer + "Items");
            cont.Add(IconBack);
            cont.Add(ButtonVisual);
            cont.Add(BackButton, Layer + "Main");
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Items",
                Name = Layer + "Item",
                Components =
                {
                    new CuiRawImageComponent{Color = "1 1 1 1", Png = GetImage(capsule)},
                    new CuiRectTransformComponent(){AnchorMin = "0.3067933 0.3074378", AnchorMax = "0.705106 0.6859503"}
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Item",
                Components =
                {
                    new CuiTextComponent(){Text =$"{findCapsul.cost}$     ", Align = TextAnchor.UpperRight, FontSize = 22},
                    new CuiRectTransformComponent(){AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0.3012385 0.1157327", AnchorMax = "0.7101834 0.2083251"},
                Button = {Color = HexToRustFormat("#5ADE6AFF"), Command = $"UI_Capsuls givecapsula {capsule}"},
                Text = {Text = lang.GetMessage("BUY", this, player.UserIDString), FontSize = 25, Align = TextAnchor.MiddleCenter}
            },Layer + "Items");
            var ff = 0;
            foreach (var check in findCapsul.itemList.Select((i, t) => new {A = i, B = t}))
            {
                if (check.B < findCapsul.itemList.Count / 2)
                {
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + "Items",
                        Name = Layer + "Items" + check.B,
                        Components =
                        {
                            new CuiRawImageComponent() {Color = "1 1 1 1", Png = GetImage(check.A.shortname)},
                            new CuiRectTransformComponent
                            {
                                AnchorMin =
                                    $"{0.001341387 + check.B * 0.18 - Math.Floor((double) check.B / 2) * 2 * 0.18} {0.5586505 - Math.Floor((double) check.B / 2) * 0.17}",
                                AnchorMax =
                                    $"{0.153972 + check.B * 0.18 - Math.Floor((double) check.B / 2) * 2 * 0.18} {0.7044114 - Math.Floor((double) check.B / 2) * 0.17}",
                            }
                        }
                    });
                }
                else if(check.B <findCapsul.itemList.Count)
                {
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + "Items",
                        Name = Layer + "Items" + check.B,
                        Components =
                        {
                            new CuiRawImageComponent() {Color = "1 1 1 1", Png = GetImage(check.A.shortname)},
                            new CuiRectTransformComponent
                            {
                                AnchorMin =
                                    $"{0.6639819 + ff * 0.18 - Math.Floor((double) ff / 2) * 2 * 0.18} {0.5586505 - Math.Floor((double) ff / 2) * 0.17}",
                                AnchorMax =
                                    $"{0.8166137 + ff * 0.18 - Math.Floor((double) ff / 2) * 2 * 0.18} {0.7044114 - Math.Floor((double) ff / 2) * 0.17}",
                            }
                        }
                    });
                    ff += 1;
                }
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Items" + check.B,
                    Components =
                    {
                        new CuiImageComponent(){Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur.mat"},
                        new CuiOutlineComponent(){Color = "1 1 1 1", Distance = "1 -1"},
                        new CuiRectTransformComponent(){AnchorMin = "0 0", AnchorMax = "1 1"}
                    }
                });
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Items" + check.B,
                    Components =
                    {
                        new CuiTextComponent(){Text = $"x{check.A.Ammount}", Align = TextAnchor.LowerRight, FontSize = 10},
                        new CuiRectTransformComponent(){AnchorMin = "0 0", AnchorMax = "1 1"}
                    }
                });
            }
            CuiHelper.AddUi(player, cont);
        }

        private Timer _anonc;
        private void GiveUi(BasePlayer player, string text, int fontsize)
        {
            _anonc?.Destroy();
            CuiHelper.DestroyUi(player, Layer + "Anonce");
            var cont = new CuiElementContainer();
            cont.Add(new CuiPanel()
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0.4638891 0.3870364", AnchorMax = "0.5361115 0.4148143"}
            }, Layer + "Main", Layer + "Anonce");
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Anonce",
                Components =
                {
                   new CuiImageComponent{Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur.mat"},
                   new CuiOutlineComponent(){Color = "1 1 1 1", Distance = "1 -1"},
                   new CuiRectTransformComponent(){AnchorMin = "0 0 ", AnchorMax = "1 1"}
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Anonce",
                Components =
                {
                    new CuiTextComponent{Color = "1 1 1 1", Text = text, Align = TextAnchor.MiddleCenter, FontSize = fontsize},
                    new CuiRectTransformComponent(){AnchorMin = "0 0 ", AnchorMax = "1 1"}
                }
            });
            CuiHelper.AddUi(player, cont);
            _anonc = timer.Once(3f, () => CuiHelper.DestroyUi(player,Layer + "Anonce"));
        }
        #endregion

        #region [My]

        private void GiveItem(BasePlayer player, string capsule)
        {
            var findCapsul = Settings.capsuls.FirstOrDefault(p => p.name == capsule);
            if(findCapsul == null|| player == null) return;
            Item item = ItemManager.CreateByName("xmas.present.large", 1, findCapsul.skinId);
            item.name = findCapsul.name;
            if (!player.inventory.GiveItem(item))
                item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity);
            CapsulitemsCapsules(player, capsule);
            var mes = lang.GetMessage("Take", this, player.UserIDString);
            GiveUi(player, mes, 18);
        }
        #endregion        
        
        #region [Хуки]

        #region HelpDesp
        
        private Dictionary<ulong, Dispense> listDispenser = new Dictionary<ulong, Dispense>();

        public class Dispense
        {
            public Dictionary<string, int> gather = new Dictionary<string, int>();
        }

        #endregion
        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!Settings.ExpDespTrue) return null;
            if (entity == null) return null;
            var player = entity as BasePlayer;
            if (dispenser == null || player == null || item == null) return null;
            int ss;
            if(!Settings.ResGather.TryGetValue(item.info.shortname, out ss)) return null;
            PlayerDat t;
            if (!_playerDatas.TryGetValue(player.userID, out t)) return null;
            Dispense f;
            if (!listDispenser.TryGetValue(player.userID, out f))
            {
                listDispenser.Add(player.userID, new Dispense());
                return null;
            }
            timer.Once(0.001f, () =>
            {
                int p;
                if (!f.gather.TryGetValue(item.info.shortname, out p))
                {
                    f.gather.Add(item.info.shortname, item.amount);
                    return;
                }
                f.gather[item.info.shortname] += item.amount;
                if (p < ss) return;
                SendReply(player, string.Format(lang.GetMessage("AddExp", this, player.UserIDString), Settings.exp));
                f.gather[item.info.shortname] = 0;
                t.Exp += Settings.ExpDesp;
                if (t.Exp < Settings.exp) return;
                t.Exp = 0;
                t.Money += Settings.expCost;
                SendReply(player, string.Format(lang.GetMessage("InterMoney", this, player.UserIDString), Settings.expCost));
            });
            return null;
        }

        object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item) =>
            OnDispenserGather(dispenser, player, item);
        
        private object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (item == null || action == null || player == null) return null;
            var findCapsule = Settings.capsuls.FirstOrDefault(p => p.skinId == item.skin);
            if (findCapsule == null) return null;
            if (action != "unwrap") return null;
            if (item.info.shortname != "xmas.present.large" && item.skin != findCapsule.skinId) return null;
            var itemList = findCapsule.itemList.ToList().GetRandom();
            if (itemList.Command == "")
            {
                var giveItem = ItemManager.CreateByName(itemList.shortname,
                    itemList.Ammount);
                var container = item.GetRootContainer();
                var slot = item.position;
                if (giveItem == null)
                {
                    SendReply(player, "Сообщите администратору об ошибке![Capsules]");
                    Puts($"Что то пошло не так у игрока [{player.displayName}]");
                    return false;
                }

                item.DoRemove();
                Effect x = new Effect("assets/prefabs/misc/easter/painted eggs/effects/gold_open.prefab", player, 0,
                    new Vector3(), new Vector3());
                EffectNetwork.Send(x, player.Connection);
                giveItem.MoveToContainer(container, slot);
            }
            else
            {
                item.DoRemove();
                Effect x = new Effect("assets/prefabs/misc/easter/painted eggs/effects/gold_open.prefab", player, 0,
                    new Vector3(), new Vector3());
                EffectNetwork.Send(x, player.Connection);
                rust.RunServerCommand(String.Format(itemList.Command, player.userID));
            }
            return false;
        }
        
        private void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject("CapsulesData", _playerDatas);
            foreach (var t in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(t, Layer);
            }
        }

        private void OnServerInitialized()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("CapsulesData"))
                _playerDatas = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerDat>>("CapsulesData");
            foreach (var t in Settings.capsuls)
                AddImage(t.image, t.name);
            foreach (var t1 in Settings.capsuls.SelectMany(t => t.itemList))
                AddImage(t1.url != "" ? t1.url : "", t1.shortname);
            if(!permission.PermissionExists("capsules.admin"))
                permission.RegisterPermission("capsules.admin", this);
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
        }
        private void OnPlayerConnected(BasePlayer player)
        {
            PlayerDat t;
            if (!_playerDatas.TryGetValue(player.userID, out t))
                _playerDatas.Add(player.userID,
                    new PlayerDat
                    {
                        Name = player.displayName,
                        Money = 0,
                        BonusTime = 0
                    });
        }

        #endregion
        #region [Help]

        private static string FormatTime(TimeSpan time, string language, int maxSubstr = 5)
        {
            var result = string.Empty;
            switch (language)
            {
                case "ru":
                    var i = 0;
                    if (time.Days != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result)) result += " ";
                        result += $"{Format(time.Days, "дней", "дня", "день")}";
                        i++;
                    }

                    if (time.Hours != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result)) result += " ";
                        result += $"{Format(time.Hours, "часов", "часа", "час")}";
                        i++;
                    }

                    if (time.Minutes != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result)) result += " ";
                        result += $"{Format(time.Minutes, "минут", "минуты", "минута")}";
                        i++;
                    }

                    if (time.Seconds != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result)) result += " ";
                        result += $"{Format(time.Seconds, "секунд", "секунды", "секунда")}";
                        i++;
                    }

                    break;
                default:
                {
                    var i2 = 0;
                    if (time.Days != 0 && i2 < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result)) result += " ";
                        result += $"{Format(time.Days, "days'", "day's", "day")}";
                        i2++;
                    }

                    if (time.Hours != 0 && i2 < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result)) result += " ";
                        result += $"{Format(time.Hours, "hours'", "hour's", "hour")}";
                        i2++;
                    }

                    if (time.Minutes != 0 && i2 < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result)) result += " ";
                        result += $"{Format(time.Minutes, "minutes", "minutes", "minute")}";
                        i2++;
                    }

                    if (time.Seconds != 0 && i2 < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result)) result += " ";
                        result += $"{Format(time.Seconds, "second", "seconds", "second")}";
                        i2++;
                    }

                    break;
                }
            }

            return result;
        }
        private static long TimeToSeconds(string time)
        {
            time = time.Replace(" ", "").Replace("d", "d ").Replace("h", "h ").Replace("m", "m ").Replace("s", "s ")
                .TrimEnd(' ');
            var arr = time.Split(' ');
            long seconds = 0;
            foreach (var s in arr)
            {
                var n = s.Substring(s.Length - 1, 1);
                var t = s.Remove(s.Length - 1, 1);
                var d = int.Parse(t);
                switch (n)
                {
                    case "s":
                        seconds += d;
                        break;
                    case "m":
                        seconds += d * 60;
                        break;
                    case "h":
                        seconds += d * 3600;
                        break;
                    case "d":
                        seconds += d * 86400;
                        break;
                }
            }

            return seconds;
        }

        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;
            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9) return $"{units} {form1}";
            if (tmp >= 2 && tmp <= 4) return $"{units} {form2}";
            return $"{units} {form3}";
        }

        private static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);

        private static double CurrentTime()
        {
            return DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        }
        private string GetImage(string shortname, ulong skin = 0) =>
            (string) ImageLibrary.Call("GetImage", shortname, skin);

        private bool AddImage(string url, string shortname, ulong skin = 0) =>
            (bool) ImageLibrary.Call("AddImage", url, shortname, skin);
        [PluginReference] private Plugin ImageLibrary;
        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";
            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
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