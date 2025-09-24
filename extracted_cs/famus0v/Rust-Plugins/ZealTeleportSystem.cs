using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using ConVar;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealTeleportSystem", "Kira", "1.0.0")]
    public class ZealTeleportSystem : RustPlugin
    {
        [PluginReference] Plugin ImageLibrary;
        StoredData DataBase = new StoredData();
        private static string Sharp = "assets/content/ui/ui.background.tile.psd";
        private static string Blur = "assets/content/ui/uibackgroundblur.mat";


        private static ZealTeleportSystem _;

        public class Teleport_PlayerCore : MonoBehaviour
        {
            public BasePlayer player;
            public HashSet<ulong> Friends;
            public Active_Call ActiveCall;
            public StoredData DataBase;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                Friends = new HashSet<ulong>();
                DataBase = _.DataBase;
                foreach (var player in BasePlayer.activePlayerList) Friends.Add(player.userID);
                DrawUI_TeleportAccept(player);
            }

            public void DrawUI_TeleportAccept(BasePlayer caller)
            {
                CuiHelper.DestroyUi(player, "BGTeleportAccept");
                CuiElementContainer UI = new CuiElementContainer();

                UI.Add(new CuiElement
                {
                    Name = "BGTeleportAccept",
                    Parent = "UI_MainPanel",
                    FadeOut = 1f,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage("BGTeleportAccept"),
                            FadeIn = 1f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.1283196 1.135138",
                            AnchorMax = "0.871 1.905945"
                        }
                    }
                });

                UI.Add(new CuiButton
                {
                    Button =
                    {
                        Command = "tpa cancel",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 12,
                        Color = HexToRustFormat("#5B2626FF"),
                        Text = "✖",
                        FadeIn = 1f
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0.22 1"
                    },
                    FadeOut = 1f,
                }, "BGTeleportAccept", "TPButtonCancel");

                UI.Add(new CuiButton
                {
                    Button =
                    {
                        Command = "tpa accept",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 11,
                        Color = HexToRustFormat("#415F15FF"),
                        Text = "✔",
                        FadeIn = 1f
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.78 0",
                        AnchorMax = "1 1"
                    },
                    FadeOut = 1f,
                }, "BGTeleportAccept", "TPButtonAccept");

                UI.Add(new CuiElement
                {
                    Name = "TeleportTXT",
                    Parent = "BGTeleportAccept",
                    FadeOut = 1f,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 11,
                            Text = $"Телепорт от : {caller.displayName}",
                            Color = HexToRustFormat("#9C8E82D7"),
                            FadeIn = 1f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }
                });

                CuiHelper.AddUi(player, UI);
            }

            public void DrawUI_FastTeleport()
            {
                CuiHelper.DestroyUi(player, "CenterHUD");
                CuiElementContainer UI = new CuiElementContainer();

                UI.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    Image =
                    {
                        Color = "0 0 0 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5"
                    }
                }, "Hud", "CenterHUD");

                UI.Add(new CuiElement
                {
                    Name = "CloseIMG",
                    Parent = "CenterHUD",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#ba3838"),
                            Png = _.GetImage("BGTPFastClose")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "-24.00039 -25.00073",
                            AnchorMax = "26.00038 25.00075"
                        }
                    }
                });


                UI.Add(new CuiButton
                {
                    Button =
                    {
                        Close = "CenterHUD",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Text = " "
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, $"CloseIMG");

                int butnum = 0;
                foreach (var friend in Friends)
                {
                    int r = 55 * Friends.Count;
                    double c = (double) Friends.Count / 2;
                    double rad = (double) butnum / c * 3.14;
                    double x = r * Math.Cos(rad);
                    double y = r * Math.Sin(rad);

                    string displayname = BasePlayer.FindByID(friend).displayName;
                    if (displayname.Length > 12)
                    {
                        displayname = displayname.Substring(0, 9);
                        displayname = $"{displayname}...";
                    }

                    UI.Add(new CuiElement
                    {
                        Name = $"TPBG{friend}",
                        Parent = "CenterHUD",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = _.GetImage("BGTPFastFriend"),
                                Color = HexToRustFormat("#496ddb"),
                                FadeIn = 0.2f * butnum
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{x - 50} {y - 50}",
                                AnchorMax = $"{x + 50} {y + 50}"
                            }
                        }
                    });

                    UI.Add(new CuiButton
                    {
                        Button =
                        {
                            Close = "CenterHUD",
                            Color = "0 0 0 0"
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Text = displayname,
                            FontSize = 14
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }, $"TPBG{friend}");

                    butnum++;
                }

                CuiHelper.AddUi(player, UI);
            }

            public void Teleport_Call(BasePlayer caller)
            {
                ActiveCall = new Active_Call {caller = caller};
                DrawUI_TeleportAccept(caller);
            }

            public class Active_Call
            {
                public BasePlayer caller;
            }

            private void OnDestroy()
            {
                CuiHelper.DestroyUi(player, "BGTeleportAccept");
                CuiHelper.DestroyUi(player, "CenterHUD");
                Destroy(this);
            }
        }

        private void OnServerInitialized()
        {
            _ = this;
            LoadData();
            ImageLibrary.Call("AddImage", "https://i.imgur.com/hMPAODH.png", "BGTeleportAccept");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/FVeJbNJ.png", "BGTPFastFriend");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/Fpkaxgp.png", "BGTPFastClose");
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                MonoBehaviour.Destroy(player.GetComponent<Teleport_PlayerCore>());
        }

        #region [Commands] / [Команды]

        [ConsoleCommand("teleport.fastmenu")]
        private void Call_TeleportFastMenu(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            args.Player().GetComponent<Teleport_PlayerCore>().DrawUI_FastTeleport();
        }

        [ChatCommand("tp")]
        private void Call_Teleport(BasePlayer player, string command, string[] args)
        {
            if (_.DataBase.PlayerTeleport_Info[player.userID].blacklist.Contains(player.userID))
                player.ChatMessage(
                    $"Вы находитесь в черном списке игрока, и не можете отправлять ему запрос на телепортацию");
            else
            {
                BasePlayer call = BasePlayer.Find(args[0]);
                if (call == player)
                {
                    player.ChatMessage("Вы не можете отправить запрос себе");
                    return;
                }

                if (call.GetComponent<Teleport_PlayerCore>().ActiveCall != null)
                {
                    player.ChatMessage("Игрок уже имеет активный запрос на телепортацию");
                    return;
                }

                call.GetComponent<Teleport_PlayerCore>().Teleport_Call(player);
                player.ChatMessage($"Запрос {call.displayName} отправлен, ожидайте подтверждения");
            }
        }

        [ConsoleCommand("tpa")]
        private void Teleport_Accept(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
        }

        [ConsoleCommand("teleport.fastmenu.close")]
        private void Close_TeleportFastMenu(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            CuiHelper.DestroyUi(args.Player(), "CenterHUD");
            CuiHelper.DestroyUi(args.Player(), "ParentLoading");
        }

        #endregion

        #region [DataBase] / [База Данных]

        public class StoredData
        {
            public Dictionary<ulong, Player_TeleportInfo> PlayerTeleport_Info =
                new Dictionary<ulong, Player_TeleportInfo>();
        }

        public class Player_TeleportInfo
        {
            public ulong steamid;
            public int cooldown_tp_player;
            public int cooldown_tp_home;
            public List<Player_Homes> homes;
            public List<ulong> blacklist;
        }

        public class Player_Homes
        {
            public string name;
            public ulong owner;
            public Vector3 position;
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        public IEnumerator Checked_DataBase()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!DataBase.PlayerTeleport_Info.ContainsKey(player.userID))
                    DataBase.PlayerTeleport_Info.Add(player.userID, new Player_TeleportInfo());
            }

            SaveData();

            yield return 0;
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, DataBase);

        private void LoadData()
        {
            try
            {
                DataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception e)
            {
                DataBase = new StoredData();
            }
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

        public string GetImage(string name)
        {
            return (string) ImageLibrary.Call("GetImage", name);
        }

        #endregion
    }
}