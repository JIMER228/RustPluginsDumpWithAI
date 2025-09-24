// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿﻿using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("DailyReward", "Fixed RustPlugin.ru", "1.1.1")]
    class DailyReward : RustPlugin
    {
        [PluginReference]
        Plugin ImageLibrary;

        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);

        public Dictionary<ulong, DailyPlayer> DailyPlayers = new Dictionary<ulong, DailyPlayer>();

        private List<DailyItem> DailyItems = new List<DailyItem>();

        private class DailyItem
        {
            public string Url { get; set; }
            public string ShortName { get; set; }
            public int Amount { get; set; }
            public string Command { get; set; }
            public int Money { get; set; }
        }

        public class DailyPlayer
        {
            public int Day { get; set; }
            public double Timestamp { get; set; }
        }

        #region UI
        [ChatCommand("günlük")]
        void ChatDailyReward(BasePlayer player, string cmd, string[] args)
        {
            DailyRewardUI(player);
        }

        [ConsoleCommand("dr")]
        void CmdDailyReward(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            DailyRewardUI(player);
        }

        void DailyRewardUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "DailyRewardUI");
            var DPlayer = DailyPlayers[player.userID];
            CuiElementContainer Container = new CuiElementContainer();
            CreatePanel(Container, "DailyRewardUI", "Hud", "0.87 0.87 0.87 1.00", "0.2 0.1", "0.8 0.9");
            CreateImage(Container, $"DailyItemImgBP", $"DailyRewardUI", GetImage("DRImage"), null, "1 1 1 1", "0 0.8561229", "1 0.9554068");
            CreateImage(Container, $"DailyItemImgBP", $"DailyRewardUI", GetImage("DRImageSnow"), null, "1 1 1 1", "0 0.0003255315", "0.998 0.4641927");
           
            Container.Add(new CuiElement
            {
                Name = "DRCursor",
                Parent = "DailyRewardUI",
                Components = { new CuiNeedsCursorComponent() }
            });
            double Time = DPlayer.Timestamp - GrabCurrentTime() > 0 ? DPlayer.Timestamp - GrabCurrentTime() : 0;
            var Text = GrabCurrentTime() > DPlayer.Timestamp ? "" : $"<size=20><color=#DC4986>S</color><color=#DB5484>o</color><color=#DA5F82>n</color><color=#DA6A81>r</color><color=#D9757F>a</color><color=#D9807E>k</color><color=#D88B7C>i</color><color=#D8967A> </color><color=#D7A179>Ö</color><color=#D6AD77>d</color><color=#D6B876>ü</color><color=#D5C374>l</color><color=#D5CE72>e</color><color=#D0D36D> </color><color=#C7D264>K</color><color=#BED15C>a</color><color=#B5CF53>l</color><color=#ACCE4A>a</color><color=#A3CD42>n</color><color=#9ACC39> </color><color=#91CB30>S</color><color=#88C927>ü</color><color=#7FC81F>r</color><color=#76C716>e</color><color=#6DC60D>:</color><color=orange> {FormatTime(TimeSpan.FromSeconds(Time))}</color></size>";
            CreatePanel(Container, "DailyRewardUITop", "DailyRewardUI", "1.00 0.59 0.00 1.00", "0 0.95", "1 1", true);
            CreateTitle(Container, "DailyRewardUITopTitle", "DailyRewardUITop", "<size=20><color=#810674>►</color><color=#B91345>T</color><color=#B71F47>E</color><color=#B52C49>F</color><color=#B4394B>O</color><color=#B2464D> </color><color=#B05350>G</color><color=#AF6052>Ü</color><color=#AD6D54>N</color><color=#AC7A56>L</color><color=#AA8758>Ü</color><color=#A8945B>K</color><color=#A7A05D> </color><color=#A5AD5F>Ö</color><color=#A3BA61>D</color><color=#A2C764>Ü</color><color=#A0D466>L</color><color=#D77844>◄</color></size>", TextAnchor.MiddleCenter, "0 0", "1 1", "0 0 0 1", "1 -1", true);
            CreateButton(Container, "DailyRewardUIBtnClose", "DailyRewardUI", "1.00 0.59 0.00 1.00", "", "0.685 0.075", "0.9355 0.15", "", "DailyRewardUI");
            CreateButton(Container, "DailyRewardUIBtnRefresh", "DailyRewardUI", "1.00 0.59 0.00 1.00", "dr", "0.685 0.175", "0.9355 0.25", "");
            CreateTitle(Container, "DailyRewardUIBtnTitle", "DailyRewardUIBtnClose", "<size=20><color=#F71FA8>►</color><color=#E53596>S</color><color=#D34C85>a</color><color=#C26374>y</color><color=#B07A62>f</color><color=#9F9151>a</color><color=#8DA740>y</color><color=#7CBE2F>ı</color><color=#6AD51D> </color><color=#6DDA20>K</color><color=#7AD72E>a</color><color=#87D43B>p</color><color=#94D049>a</color><color=#A1CD56>t</color><color=#AEC964>◄</color></size>", TextAnchor.MiddleCenter, "0 0", "1 1", "0 0 0 1", "1 -1", true);
            CreateTitle(Container, "DailyRewardUIBtnRefreshTitle", "DailyRewardUIBtnRefresh", "<size=20><color=#E0EB77>►</color><color=#E3E17B>S</color><color=#E6D780>i</color><color=#E9CE85>s</color><color=#ECC48A>t</color><color=#EFBB8F>e</color><color=#F2B194>m</color><color=#F5A799>i</color><color=#F89E9E> </color><color=#F58FA4>Y</color><color=#EF7DAB>e</color><color=#E86CB2>n</color><color=#E25AB9>i</color><color=#DB49C0>l</color><color=#D537C7>e</color><color=#CE26CE>◄</color></size>", TextAnchor.MiddleCenter, "0 0", "1 1", "0 0 0 1", "1 -1", true);
            CreateTitle(Container, "DailyRewardUITitle", "DailyRewardUI", $"<size=20>{DateTime.Now.ToString("dd/MM/yyyy")}</size>", TextAnchor.MiddleCenter, "0 0.85", "1 0.95", "0 0 0 1", "1 -1", true);
            CreateTitle(Container, $"DailyRewardUIBotTitle", $"DailyRewardUI", Text, TextAnchor.MiddleLeft, "0.075 0.175", "0.8 0.25", "0 0 0 1", "1 -1", true);
            CreateTitle(Container, $"DailyRewardUIBotTitle", $"DailyRewardUI", $"<size=20><color=#DC4986>A</color><color=#DB5684>l</color><color=#DA6482>ı</color><color=#D97280>n</color><color=#D9807E>a</color><color=#D88E7C>n</color><color=#D79C7A> </color><color=#D7AA78>Ö</color><color=#D6B876>d</color><color=#D5C674>ü</color><color=#D5D472>l</color><color=#C9D267> </color><color=#BED15C>S</color><color=#B3CF51>a</color><color=#A7CE46>y</color><color=#9CCC3B>ı</color><color=#91CB30>s</color><color=#85C925>ı</color><color=#7AC81A>:</color><color=orange> {DPlayer.Day}</color></size>", TextAnchor.MiddleLeft, "0.075 0.075", "0.4 0.15", "0 0 0 1", "1 -1", true);
            if (DPlayer.Day > 29)
            {
                CreateButton(Container, "DailyRewardUIBtnReward", "DailyRewardUI", "1.00 0.59 0.00 1.00", "dailyrewardget", "0.685 0.275", "0.9355 0.35", "");
                CreateTitle(Container, "DailyRewardUIBtnRewardTitle", "DailyRewardUIBtnReward", "<size=20><color=#DC4986>A</color><color=#DB5684>l</color><color=#DA6482>ı</color><color=#D97280>n</color><color=#D9807E>a</color><color=#D88E7C>n</color><color=#D79C7A> </color><color=#D7AA78>Ö</color><color=#D6B876>d</color><color=#D5C674>ü</color><color=#D5D472>l</color><color=#C9D267> </color><color=#BED15C>S</color><color=#B3CF51>a</color><color=#A7CE46>y</color><color=#9CCC3B>ı</color><color=#91CB30>s</color><color=#85C925>ı</color><color=#7AC81A>:</color></size>", TextAnchor.MiddleCenter, "0 0", "1 1", "0 0 0 1", "1 -1", true);
            }
            int i = 0;
            int x = 0, y = 0;
            foreach (var item in DailyItems)
            {
                CreatePanel(Container, $"DailyItem{i}", "DailyRewardUI", "1.00 0.75 0.51 0.80", $"{0.07 + (0.0875 * x)} {0.7375 - (0.135 * y)}", $"{0.145 + (0.0875 * x)} {0.85 - (0.135 * y)}");
                if (DPlayer.Day == i)
                {
                    if (GrabCurrentTime() > DPlayer.Timestamp)
                    {
                        CreatePanel(Container, $"DailyItem{i}", "DailyRewardUI", "0.00 1.00 0.25 0.8", $"{0.07 + (0.0875 * x)} {0.7375 - (0.135 * y)}", $"{0.145 + (0.0875 * x)} {0.85 - (0.135 * y)}");
                        CreateImage(Container, $"DailyItemImg{i}", $"DailyItem{i}", GetImage(item.ShortName), null, "1 1 1 1", "0.1 0.1", "0.9 0.9");
                        CreateTitle(Container, $"DailyItemImgTitle{i}", $"DailyItem{i}", $"{item.Amount}", TextAnchor.LowerCenter, "0 0", "1 1", "0 0 0 1", "1 -1", true);
                        CreateButton(Container, $"DailyItemBtn{i}", $"DailyItem{i}", "0 0 0 0", $"dailyrewardget", "0 0", "1 1", "");
                    }
                    else
                    {
                        CreateImage(Container, $"DailyItemImg{i}", $"DailyItem{i}", GetImage(item.ShortName), null, "1 1 1 1", "0.1 0.1", "0.9 0.9");
                        CreatePanel(Container, $"DailyItemFon{i}", $"DailyItem{i}", "0 0 0 0.5", "0 0", "1 1");
                        CreateTitle(Container, $"DailyItemImgTitle{i}", $"DailyItem{i}", $"Yarın", TextAnchor.MiddleCenter, "0 0", "1 1", "0 0 0 1", "1 -1", true);
                    }
                }
                else if (DPlayer.Day > i)
                {
                    CreateImage(Container, $"DailyItemImg{i}", $"DailyItem{i}", GetImage(item.ShortName), null, "1 1 1 1", "0.1 0.1", "0.9 0.9");
                    CreatePanel(Container, $"DailyItemFon{i}", $"DailyItem{i}", "0 0 0 0.5", "0 0", "1 1");
                    CreateTitle(Container, $"DailyItemImgTitle{i}", $"DailyItem{i}", $"{item.Amount}", TextAnchor.LowerCenter, "0 0", "1 1", "0 0 0 1", "1 -1", true);
                    CreateTitle(Container, $"DailyItemFonTitle{i}", $"DailyItemFon{i}", "<size=30>✓</size>", TextAnchor.MiddleCenter, "0 0", "1 1", "0 0 0 1", "1 -1", true);
                }
                else if (DPlayer.Day < i)
                {
                    CreatePanel(Container, $"DailyItemImg{i}", $"DailyItem{i}", "0.87 0.87 0.87 0.5", "0.1 0.1", "0.9 0.9");
                    CreateImage(Container, $"DailyItemImgBPSnowMan", $"DailyItem{i}", GetImage("DRImageSnowman"), null, "1 1 1 1", "0.15 0.15", "0.85 0.85");
                }
                i++; x++;
                if (x > 9) { y++; x = 0; }
            }
            CuiHelper.AddUi(player, Container);
        }
        #endregion

        [ConsoleCommand("dailyrewardget")]
        void DailyRewardGet(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            DailyPlayer DPlayer = DailyPlayers[arg.Player().userID];
            DailyItem DItem = DailyItems[DPlayer.Day];
            
            if (GrabCurrentTime() > DPlayer.Timestamp)
            {
                DPlayer.Day++;
                DPlayer.Timestamp = GrabCurrentTime() + 86400;
                if (DItem != null)
                {
                    if (string.IsNullOrEmpty(DItem.Command))
                    {
                        Item _item = ItemManager.CreateByName(DItem.ShortName, DItem.Amount);
                        arg.Player().GiveItem(_item);
                    }
                    else
                    {
                        ConsoleSystem.Run(ConsoleSystem.Option.Server, String.Format($"{DItem.Command}", arg.Player().userID));
                        DailyRewardUI(arg.Player());
                    }
                }
                else
                {
                    SendReply(arg.Player(), "kek");
                }
                if (DailyItems.Count <= DPlayer.Day)
                {
                    DPlayer.Day = 0;
                    DPlayer.Timestamp = GrabCurrentTime() + 86400;
                }
                
                DailyRewardUI(arg.Player());
            }
        }

        #region HOOK
        void OnServerInitialized()
        {
            LoadData();
            CheckData();
            foreach (var item in DailyItems)
            {
                if (string.IsNullOrEmpty(item.Url))
                    plugins.Find("ImageLibrary").CallHook("GetImage", item.ShortName, 0, true);
                else
                    AddImage(item.Url, item.ShortName);
            }
            foreach(var img in Images)
            {
                AddImage(img.Key, img.Value);
            }

            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(player);
            }
        }

        Dictionary<string, string> Images = new Dictionary<string, string>()
        {
            {"https://i.imgur.com/K4h19zN.png", "DRImage"},
            {"https://i.hizliresim.com/S1W39N.png", "DRImageSnow" },
            {"https://i.hizliresim.com/mY465O.png", "DRImageSnowman" }
        };


        void CheckData()
        {
            foreach (var item in DailyItems)
            {
                if (string.IsNullOrEmpty(item.ShortName))
                {
                    PrintError($"Внимание! У предмета не установлено название!");
                    continue;
                }
                if (string.IsNullOrEmpty(item.Amount.ToString()))
                {
                    PrintError($"Внимание! У предмета {item.ShortName} не установлено количество!");
                    continue;
                }
            }
        }

        void LoadData()
        {
            try
            {
                DailyPlayers = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, DailyPlayer>>("DailyReward/Players");
            }
            catch
            {
                DailyPlayers = new Dictionary<ulong, DailyPlayer>();
            }
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("DailyReward/RewardsList"))
            {
                DailyItems.Add(new DailyItem
                {
                    Url = "",

                    ShortName = "supply.signal",
                    Amount = 1,

                    Command = "",
                    Money = 0
                });

                DailyItems.Add(new DailyItem
                {

                    Url = "",
                    ShortName = "stones",
                    Amount = 7500,
                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",
                    ShortName = "metal.fragments",
                    Amount = 12500,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "scrap",
                    Amount = 350,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "metal.refined",
                    Amount = 200,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "sulfur.ore",
                    Amount = 5000,

                    Command = "",
                    Money = 0
                });

                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "blueberries",
                    Amount = 30,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "gunpowder",
                    Amount = 1500,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "wood",
                    Amount = 17000,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "rifle.semiauto",
                    Amount = 1,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "lowgradefuel",
                    Amount = 1000,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "ammo.rocket.basic",
                    Amount = 1,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "metal.refined",
                    Amount = 300,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "metal.fragments",
                    Amount = 18000,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "wood",
                    Amount = 30000,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "scrap",
                    Amount = 1900,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "sulfur.ore",
                    Amount = 4000,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "ammo.rocket.basic",
                    Amount = 5,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "ammo.rocket.basic",
                    Amount = 1,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "sulfur.ore",
                    Amount = 5000,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "metal.fragments",
                    Amount = 23000,

                    Command = "",
                    Money = 0
                });
                DailyItems.Add(new DailyItem
                {

                    Url = "",

                    ShortName = "metal.refined",
                    Amount = 450,

                    Command = "",
                    Money = 0
                });
                Interface.Oxide.DataFileSystem.WriteObject("DailyReward/RewardsList", DailyItems);
            }
            DailyItems = Interface.Oxide.DataFileSystem.ReadObject<List<DailyItem>>("DailyReward/RewardsList");

        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (!DailyPlayers.ContainsKey(player.userID))
            {
                DailyPlayers.Add(player.userID, new DailyPlayer
                {
                    Day = 0,
                    Timestamp = GrabCurrentTime()
                });
            }
            if (GrabCurrentTime() > DailyPlayers[player.userID].Timestamp)
            {
                DailyRewardUI(player);
            }
        }

        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "DailyRewardUI");
            }
            SaveData();
        }

        void OnServerSave()
        {
            SaveData();
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("DailyReward/Players", DailyPlayers);
        }

        public static string FormatTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result += $"{Format(time.Days, "gün", "gün", "gün")} ";

            if (time.Hours != 0)
                result += $"{Format(time.Hours, "saat", "saat", "saat")} ";

            if (time.Minutes != 0)
                result += $"{Format(time.Minutes, "dakika", "dakika", "dakika")} ";

            return result;
        }

        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }

        private double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        #endregion

        #region GUI Template
        private void CreateImage(CuiElementContainer Container, string Name, string Parent, string Png, string Url, string Color, string AnchorMin, string AnchorMax)
        {
            Container.Add(new CuiElement
            {
                Name = Name,
                Parent = Parent,
                Components = {
                        new CuiRawImageComponent {
                            Png = Png,
                            Url = Url,
                            Color = Color,
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = AnchorMin,
                            AnchorMax = AnchorMax
                        }
                    }
            });
        }

        private void CreateButton(CuiElementContainer Container, string Name, string Parent, string Color, string Command, string AnchorMin, string AnchorMax, string Text, string Close = "")
        {
            Container.Add(new CuiButton
            {
                Button = { Color = Color, Command = Command, Close = Close },
                RectTransform = { AnchorMin = AnchorMin, AnchorMax = AnchorMax },
                Text = { Text = Text, Align = TextAnchor.MiddleCenter }
            }, Parent, Name);
        }

        private void CreateTitle(CuiElementContainer Container, string Name, string Parent, string Text, TextAnchor Align, string AnchorMin, string AnchorMax, string OutlineColor, string OutlineDistance, bool UseAlpha)
        {
            Container.Add(new CuiElement
            {
                Name = Name,
                Parent = Parent,
                Components = {
                        new CuiTextComponent {
                            Text = Text,
                            Align = Align
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = AnchorMin,
                        AnchorMax = AnchorMax
                        },
                        new CuiOutlineComponent
                        {
                             Color = OutlineColor,
                             Distance = OutlineDistance,
                             UseGraphicAlpha = UseAlpha
                        }
                    }
            });
        }

        static public void CreatePanel(CuiElementContainer container, string Name, string Parent, string color, string aMin, string aMax, bool cursor = false)
        {
            container.Add(new CuiPanel
            {
                Image = { Color = color },
                RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                CursorEnabled = cursor
            },
            Parent, Name);
        }
        #endregion
    }
}
                           