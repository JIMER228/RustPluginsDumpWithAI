// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("FriendAuthorization", "UberCode", "1.0")]
    class FriendAuthorization : RustPlugin
    {
        [PluginReference]
        Plugin Friends;

        private static string _mainUI = "ui.Friendauthorization";

        #region Oxide Hooks

        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["A Cupboards"] = "<color=#0c8c35>Вы авторизовали своих друзей в шкафах</color>",
                ["A AutoTurret"] = "<color=#0c8c35>Вы авторизовали своих друзей в турелях</color>",
                ["A CodeLock"] = "<color=#0c8c35>Вы авторизовали своих друзей в замках</color>",
                ["D Cupboards"] = "<color=#ff0000>Вы деавторизовали своих друзей в шкафах</color>",
                ["D AutoTurret"] = "<color=#ff0000>Вы деавторизовали своих друзей в турелях</color>",
                ["D CodeLock"] = "<color=#ff0000>Вы деавторизовали своих друзей в замках</color>",
                ["No Friend"] = "У вас нет друзей",
                ["No Cupboards"] = "У вас нет шкафа",
                ["No AutoTurret"] = "У вас нет турели",
                ["No CodeLock"] = "У вас нет замков",
                
            }, this);
        }
        #endregion

        #region Commands

        [ConsoleCommand("auth")]
        private void CommandConsoleAuth(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Player() == null || !arg.HasArgs()) return;

            switch (arg.Args[0].ToLower())
            {
                case "a.cupboard":
                    AuthorizationCupboard(arg.Player());
                    return;
                case "a.autoturret":
                    AuthorizationAutoTurret(arg.Player());
                    return;
                case "a.codelock":
                    AuthorizationCodeLocks(arg.Player());
                    return;
                case "d.cupboard":
                    DeauthorizationCupboard(arg.Player());
                    return;
                case "d.autoturret":
                    DeauthorizationAutoTurret(arg.Player());
                    return;
                case "d.codelock":
                    DeauthorizationCodeLocks(arg.Player());
                    return;
            }
        }

        [ChatCommand("auth")]
        private void CommandChatAuth(BasePlayer player, string cmd, string[] args)
        {
            if (player == null) return;

            UI.InitilizeUI(player);
        }

        #endregion

        #region GUI

        static class UI
        {
            public static void InitilizeUI(BasePlayer player)
            {
                var container = new CuiElementContainer();

                // Main panel
                InitilizePanel(ref container, "Hud", "0.28 0.28", "0.7 0.7", "#000000dd", true, _mainUI);

                // Title
                InitilizeLabel(ref container, _mainUI, TextAnchor.MiddleCenter, "0 0.85", "1 1", "#ffffff", 24, "<color=#c94d20><b>RUST</b></color> <color=#901fc9><b>Utopia</b></color>");

                // Button close
                InitilizeButton(ref container, _mainUI, "#c62929", "0.9 0.91", "0.998 0.998", text: "X", close: _mainUI);

                InitilizeBlock(ref container, _mainUI, "0.05 0.65", "0.95 0.80", "auth a.cupboard", "auth d.cupboard", "Шкафы");
                InitilizePanel(ref container, _mainUI, "0.05 0.60", "0.95 0.60", "#ffffff");
                InitilizeBlock(ref container, _mainUI, "0.05 0.40", "0.95 0.55", "auth a.autoturret", "auth d.autoturret", "Турели");
                InitilizePanel(ref container, _mainUI, "0.05 0.35", "0.95 0.35", "#ffffff");
                InitilizeBlock(ref container, _mainUI, "0.05 0.15", "0.95 0.3", "auth a.codelock", "auth d.codelock", "Кодовые замки");

                CuiHelper.AddUi(player, container);
            }

            private static string InitilizePanel(ref CuiElementContainer container, string parent, string anchorMin, string anchorMax, string color, bool cursor = false, string name = null)
            {
                return container.Add(new CuiPanel
                {
                    Image = { Color = HexToRustFormat(color) },
                    RectTransform = { AnchorMin = anchorMin, AnchorMax = anchorMax },
                    CursorEnabled = cursor
					
                }, parent, name);
            }

            private static string InitilizeLabel(ref CuiElementContainer container, string parent, TextAnchor align, string anchorMin, string anchorMax, string color, int fontSize, string text)
            {
                var name = container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = anchorMin, AnchorMax = anchorMax }
                }, parent);

                container.Add(new CuiLabel
                {
                    Text = { Align = align,Font = "RobotoCondensed-Regular.ttf", Color = HexToRustFormat(color), FontSize = fontSize, Text = text }
                }, name);

                return name;
            }

            private static string InitilizeButton(ref CuiElementContainer container, string parent, string color, string anchorMin, string anchorMax, string command = null, string text = null, string close = null)
            {
                return container.Add(new CuiButton
                {
                    Button = { Color = HexToRustFormat(color), Command = command, Close = close },
                    RectTransform = { AnchorMin = anchorMin, AnchorMax = anchorMax },
                    Text = { Text = text,Font = "RobotoCondensed-Regular.ttf",Align = TextAnchor.MiddleCenter, FontSize = 16 }
                }, parent);
            }

            private static void InitilizeBlock(ref CuiElementContainer container, string parent, string anchorMin, string anchorMax, string activeCommand, string passiveCommand, string text)
            {
                var name = container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0.0" },
                    RectTransform = { AnchorMin = anchorMin, AnchorMax = anchorMax }
                }, parent);

                InitilizeButton(ref container, name, "#ffec8b", "0 0", "0.3 1", activeCommand, "<color=black><b>Авторизовать</b></color>");
                InitilizeLabel(ref container, name, TextAnchor.MiddleCenter, "0.3 0", "0.7 1", "#FFD700", 18, text);
                InitilizeButton(ref container, name, "#FF4040", "0.7 0", "1 1", passiveCommand, "<color=black><b>Деавторизовать</b></color>");
            }

            public static void Alert(BasePlayer player, string message)
            {
                var container = new CuiElementContainer();

                var name = CuiHelper.GetGuid();

                container.Add(new CuiElement
                {
                    Name = name,
                    Parent = _mainUI,
                    FadeOut = 0.5f,
                    Components =
                    {
                        new CuiTextComponent { Align = TextAnchor.MiddleCenter,Font = "RobotoCondensed-Regular.ttf", Color = "1 1 1 1", FontSize = 18, FadeIn = 0.25f, Text = message },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 0.15" }
                    }
                });

                CuiHelper.AddUi(player, container);

                Interface.Oxide.GetLibrary<Oxide.Core.Libraries.Timer>().Once(1f, () => CuiHelper.DestroyUi(player, name));
            }
        }

        #endregion

        #region Core

        private void Authorization<T>(BasePlayer player, List<T> entities, string completionLang, string noEntityLang, Action<T, BasePlayer> execute) where T : BaseNetworkable
        {
            if (entities == null || entities.Count() == 0)
            {
                UI.Alert(player, lang.GetMessage(noEntityLang, this));
                return;
            }

            var friends = GetFriends(player.userID);
            if (friends.Count() == 0)
            {
                UI.Alert(player, lang.GetMessage("No Friend", this));
                return;
            }

            foreach (var entity in entities)
            {
                foreach (var friend in friends)
                    execute.Invoke(entity, friend);

                entity.SendNetworkUpdateImmediate();
            }

            foreach (var friend in friends)
                friend.SendNetworkUpdateImmediate();

            UI.Alert(player, lang.GetMessage(completionLang, this));
        }

        private void Deauthorization<T>(BasePlayer player, List<T> entities, string completionLang, string noEntityLang, Action<T> execute) where T : BaseNetworkable
        {
            if (entities == null || entities.Count() == 0)
            {
                UI.Alert(player, lang.GetMessage(noEntityLang, this));
                return;
            }

            foreach (var entity in entities)
            {
                execute.Invoke(entity);
                entity.SendNetworkUpdateImmediate();
            }

            UI.Alert(player, lang.GetMessage(completionLang, this));
        }



        private void AuthorizationCupboard(BasePlayer player)
        {
            Authorization(player, player.GetCupboards(), "A Cupboards", "No Cupboards", (cupboard, friend) =>
            {
                if (!cupboard.IsAuthed(friend))
                {
                    cupboard.authorizedPlayers.Add(new ProtoBuf.PlayerNameID
                    {
                        userid = friend.userID,
                        username = friend.displayName,
                        ShouldPool = true
                    });
                }
            });
        }

        private void DeauthorizationCupboard(BasePlayer player)
        {
            Deauthorization(player, player.GetCupboards(), "D Cupboards", "No Cupboards", cupboard =>
            {
                cupboard.authorizedPlayers.Clear();
                cupboard.authorizedPlayers.Add(new ProtoBuf.PlayerNameID
                {
                    userid = player.userID,
                    username = player.displayName,
                    ShouldPool = true
                });
            });
        }

        private void AuthorizationAutoTurret(BasePlayer player)
        {
            Authorization(player, player.GetAutoTurrets(), "A AutoTurret", "No AutoTurret", (turret, friend) =>
            {
                if (!turret.IsAuthed(friend))
                {
                    turret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID
                    {
                        userid = friend.userID,
                        username = friend.displayName,
                        ShouldPool = true
                    });
                }
            });
        }

        private void DeauthorizationAutoTurret(BasePlayer player)
        {
            Deauthorization(player, player.GetAutoTurrets(), "D AutoTurret", "No AutoTurret", turret =>
            {
                turret.authorizedPlayers.Clear();
                turret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID
                {
                    userid = player.userID,
                    username = player.displayName,
                    ShouldPool = true
                });
            });
        }

        private void AuthorizationCodeLocks(BasePlayer player)
        {
            Authorization(player, player.GetCodeLocks(), "A CodeLock", "No CodeLock", (codeLock, friend) =>
            {
                var whiteList = codeLock.GetWhiteList();

                if (!whiteList.Contains(friend.userID))
                    whiteList.Add(friend.userID);
            });
        }

        private void DeauthorizationCodeLocks(BasePlayer player)
        {
            Deauthorization(player, player.GetCodeLocks(), "D CodeLock", "No CodeLock", codeLock =>
            {
                var whiteList = codeLock.GetWhiteList();
                whiteList.Clear();
                whiteList.Add(player.userID);
            });

        }

        #endregion

        #region Helpers

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

        private List<BasePlayer> GetFriends(ulong userID)
        {
            var result = Friends?.Call("GetFriends", userID);
            if (result == null)
                return null;

            return (result as ulong[]).Select(x => RustCore.FindPlayerById(x)).ToList();
        }

        #endregion
    }
}

static class HelperExtension
{
    private static readonly FieldInfo _codeLockWhiteList = typeof(CodeLock).GetField("whitelistPlayers", BindingFlags.Instance | BindingFlags.NonPublic);

    public static List<BuildingPrivlidge> GetCupboards(this BasePlayer player)
        => UnityEngine.Object.FindObjectsOfType<BuildingPrivlidge>().Where(x => x.OwnerID == player.userID && x.IsAuthed(player)).ToList();

    public static List<AutoTurret> GetAutoTurrets(this BasePlayer player)
        => UnityEngine.Object.FindObjectsOfType<AutoTurret>().Where(x => x.OwnerID == player.userID && x.IsAuthed(player)).ToList();

    public static List<CodeLock> GetCodeLocks(this BasePlayer player)
        => UnityEngine.Object.FindObjectsOfType<CodeLock>().Where(x => x.GetParentEntity() != null && x.GetParentEntity().OwnerID == player.userID && x.GetWhiteList() != null && x.GetWhiteList().Contains(player.userID)).ToList();

    public static List<ulong> GetWhiteList(this CodeLock codeLock)
        => (List<ulong>)_codeLockWhiteList.GetValue(codeLock);
}
