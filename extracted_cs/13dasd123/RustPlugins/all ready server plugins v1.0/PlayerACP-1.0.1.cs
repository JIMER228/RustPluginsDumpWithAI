using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("PlayerACP", "NoName", "1.0.1", ResourceId = 0)]
      //  Слив плагинов server-rust by Apolo YouGame
    [Description("Добавляет удобную панель управления для администраторов.")]
    public class PlayerACP : RustPlugin
    {
        #region GUI
        #region Types
        public class CuiColor
        {
            public byte R { get; set; } = 255;
            public byte G { get; set; } = 255;
            public byte B { get; set; } = 255;
            public float A { get; set; } = 1f;

            public CuiColor() { }

            public CuiColor(byte red, byte green, byte blue, float alpha = 1f)
            {
                R = red;
                G = green;
                B = blue;
                A = alpha;
            }

            public override string ToString() => $"{(double)R / 255} {(double)G / 255} {(double)B / 255} {A}";
        }

        public class CuiPoint
        {
            public float X { get; set; } = 0f;
            public float Y { get; set; } = 0f;

            public CuiPoint() { }

            public CuiPoint(float x, float y)
            {
                X = x;
                Y = y;
            }

            public override string ToString() => $"{X} {Y}";
        }

        private enum UiPage
        {
            Main = 0,
            PlayersOnline,
            PlayersOffline,
            PlayersBanned,
            PlayerPage,
            PlayerPageBanned
        }
        #endregion Types

        #region Defaults
        public class CuiDefaultColors
        {
            public static CuiColor Background { get; } = new CuiColor(240, 240, 240, 0.3f);
            public static CuiColor BackgroundMedium { get; } = new CuiColor(76, 74, 72, 0.83f);
            public static CuiColor BackgroundDark { get; } = new CuiColor(42, 42, 42, 0.93f);
            public static CuiColor Button { get; } = new CuiColor(42, 42, 42, 0.9f);
            public static CuiColor ButtonInactive { get; } = new CuiColor(168, 168, 168, 0.9f);
            public static CuiColor ButtonDecline { get; } = new CuiColor(192, 0, 0, 0.9f);
            public static CuiColor Text { get; } = new CuiColor(0, 0, 0, 1f);
            public static CuiColor TextAlt { get; } = new CuiColor(255, 255, 255, 1f);
            public static CuiColor TextTitle { get; } = new CuiColor(206, 66, 43, 1f);
            public static CuiColor None { get; } = new CuiColor(0, 0, 0, 0f);
        }
        #endregion Defaults

        #region UI object definitions
        public class CuiInputField
        {
            public CuiInputFieldComponent InputField { get; } = new CuiInputFieldComponent();
            public CuiRectTransformComponent RectTransform { get; } = new CuiRectTransformComponent();
            public float FadeOut { get; set; }
        }
        #endregion UI object definitions

        #region Component container
        public class CustomCuiElementContainer : CuiElementContainer
        {
            public string Add(CuiInputField inputField, string parent = "Hud", string name = null)
            {
                if (string.IsNullOrEmpty(name))
                    name = CuiHelper.GetGuid();

                Add(new CuiElement {
                    Name = name,
                    Parent = parent,
                    FadeOut = inputField.FadeOut,
                    Components = {
                        inputField.InputField,
                        inputField.RectTransform
                    }
                });
                return name;
            }
        }
        #endregion Component container

        public class Cui
        {
            public const string PARENT_HUD = "Hud";
            public const string PARENT_OVERLAY = "Overlay";

            public string MainPanelName { get; set; }

            private BasePlayer player;
            private CustomCuiElementContainer container = new CustomCuiElementContainer();

            public Cui(BasePlayer player)
            {
                this.player = player;
            }

            public string AddPanel(string parent, CuiPoint leftBottomAnchor, CuiPoint rightTopAnchor, bool cursorEnabled, CuiColor color = null, string name = null, string png = null) =>
                AddPanel(parent, leftBottomAnchor, rightTopAnchor, new CuiPoint(), new CuiPoint(), cursorEnabled, color, name, png);

            public string AddPanel(string parent, CuiPoint leftBottomAnchor, CuiPoint rightTopAnchor, CuiPoint leftBottomOffset, CuiPoint rightTopOffset, bool cursorEnabled, CuiColor color = null, string name = null, string png = null)
            {
                CuiPanel panel = new CuiPanel() {
                    RectTransform = {
                        AnchorMin = leftBottomAnchor.ToString(),
                        AnchorMax = rightTopAnchor.ToString(),
                        OffsetMin = leftBottomOffset.ToString(),
                        OffsetMax = rightTopOffset.ToString()
                    },
                    CursorEnabled = cursorEnabled
                };

                if (!string.IsNullOrEmpty(png) || (color != null)) {
                    panel.Image = new CuiImageComponent() {
                        Color = color.ToString(),
                        Png = png
                    };
                }

                return container.Add(panel, parent, name);
            }

            public string AddLabel(string parent, CuiPoint leftBottomAnchor, CuiPoint rightTopAnchor, CuiColor color, string text, string name = null, int fontSize = 14, TextAnchor align = TextAnchor.UpperLeft) =>
                AddLabel(parent, leftBottomAnchor, rightTopAnchor, new CuiPoint(), new CuiPoint(), color, text, name, fontSize, align);

            public string AddLabel(string parent, CuiPoint leftBottomAnchor, CuiPoint rightTopAnchor, CuiPoint leftBottomOffset, CuiPoint rightTopOffset, CuiColor color, string text, string name = null, int fontSize = 14, TextAnchor align = TextAnchor.UpperLeft)
            {
                return container.Add(new CuiLabel() {
                    Text = {
                        Text = text ?? "",
                        FontSize = fontSize,
                        Align = align,
                        Color = color.ToString()
                    },
                    RectTransform = {
                        AnchorMin = leftBottomAnchor.ToString(),
                        AnchorMax = rightTopAnchor.ToString(),
                        OffsetMin = leftBottomOffset.ToString(),
                        OffsetMax = rightTopOffset.ToString()
                    }
                }, parent, name);
            }

            public string AddButton(string parent, CuiPoint leftBottomAnchor, CuiPoint rightTopAnchor, CuiColor buttonColor, CuiColor textColor, string text, string command = "", string close = "", string name = null, int fontSize = 14, TextAnchor align = TextAnchor.MiddleCenter) =>
                AddButton(parent, leftBottomAnchor, rightTopAnchor, new CuiPoint(), new CuiPoint(), buttonColor, textColor, text, command, close, name, fontSize, align);

            public string AddButton(string parent, CuiPoint leftBottomAnchor, CuiPoint rightTopAnchor, CuiPoint leftBottomOffset, CuiPoint rightTopOffset, CuiColor buttonColor, CuiColor textColor, string text, string command = "", string close = "", string name = null, int fontSize = 14, TextAnchor align = TextAnchor.MiddleCenter)
            {
                return container.Add(new CuiButton() {
                    Button = {
                        Command = command ?? "",
                        Close = close ?? "",
                        Color = buttonColor.ToString()
                    },
                    RectTransform = {
                        AnchorMin = leftBottomAnchor.ToString(),
                        AnchorMax = rightTopAnchor.ToString(),
                        OffsetMin = leftBottomOffset.ToString(),
                        OffsetMax = rightTopOffset.ToString()
                    },
                    Text = {
                        Text = text ?? "",
                        FontSize = fontSize,
                        Align = align,
                        Color = textColor.ToString()
                    }
                }, parent, name);
            }

            public string AddInputField(string parent, CuiPoint leftBottomAnchor, CuiPoint rightTopAnchor, CuiColor color, string text = "", int charsLimit = 100, string command = "", bool isPassword = false, string name = null, int fontSize = 14, TextAnchor align = TextAnchor.MiddleLeft) =>
                AddInputField(parent, leftBottomAnchor, rightTopAnchor, new CuiPoint(), new CuiPoint(), color, text, charsLimit, command, isPassword, name, fontSize, align);

            public string AddInputField(string parent, CuiPoint leftBottomAnchor, CuiPoint rightTopAnchor, CuiPoint leftBottomOffset, CuiPoint rightTopOffset, CuiColor color, string text = "", int charsLimit = 100, string command = "", bool isPassword = false, string name = null, int fontSize = 14, TextAnchor align = TextAnchor.MiddleLeft)
            {
                return container.Add(new CuiInputField() {
                    InputField = {
                        Text = text ?? "",
                        FontSize = fontSize,
                        Align = align,
                        Color = color.ToString(),
                        CharsLimit = charsLimit,
                        Command = command ?? "",
                        IsPassword = isPassword
                    },
                    RectTransform = {
                        AnchorMin = leftBottomAnchor.ToString(),
                        AnchorMax = rightTopAnchor.ToString(),
                        OffsetMin = leftBottomOffset.ToString(),
                        OffsetMax = rightTopOffset.ToString()
                    }
                }, parent, name);
            }

            public bool Draw()
            {
                if (!string.IsNullOrEmpty(MainPanelName))
                    return CuiHelper.AddUi(player, container);

                return false;
            }

            public string GetPlayerId() => player.UserIDString;
        }
        #endregion GUI

        #region Utility methods
        List<T> GetPage<T>(IList<T> aList, int aPage, int aPageSize) => aList.Skip(aPage * aPageSize).Take(aPageSize).ToList();

        private void AddTabMenuBtn(ref Cui aUIObj, string aParent, string aCaption, string aCommand, int aPos, bool aIndActive)
        {
            Vector2 dimensions = new Vector2(0.096f, 0.75f);
            Vector2 offset = new Vector2(0.005f, 0.1f);
            CuiColor btnColor = (aIndActive ? CuiDefaultColors.ButtonInactive : CuiDefaultColors.Button);
            CuiPoint LBAnchor = new CuiPoint(((dimensions.x + offset.x) * aPos) + offset.x, offset.y);
            CuiPoint RTAnchor = new CuiPoint(LBAnchor.X + dimensions.x, offset.y + dimensions.y);
            aUIObj.AddButton(aParent, LBAnchor, RTAnchor, btnColor, CuiDefaultColors.TextAlt, aCaption, (aIndActive ? "" : aCommand));
        }

        private void AddPlayerButtons<T>(ref Cui aUIObj, string aParent, ref List<T> aUserList, string aCommandFmt, int aPage)
        {
            List<T> userRange = GetPage(aUserList, aPage, MAX_PLAYER_BUTTONS);
            Vector2 dimensions = new Vector2(0.194f, 0.09f);
            Vector2 offset = new Vector2(0.005f, 0.01f);
            int col = -1;
            int row = 0;
            float margin = 0.12f;

            foreach (T user in userRange) {
                if (++col >= MAX_PLAYER_COLS) {
                    row++;
                    col = 0;
                };

                float calcTop = (1f - margin) - (((dimensions.y + offset.y) * row) + offset.y);
                float calcLeft = ((dimensions.x + offset.x) * col) + offset.x;
                CuiPoint LBAnchor = new CuiPoint(calcLeft, calcTop - dimensions.y);
                CuiPoint RTAnchor = new CuiPoint(calcLeft + dimensions.x, calcTop);

                if (typeof(T) == typeof(BasePlayer)) {
                    string btnText = (user as BasePlayer).displayName;
                    string btnCommand = string.Format(aCommandFmt, (user as BasePlayer).UserIDString);
                    aUIObj.AddButton(aParent, LBAnchor, RTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, btnText, btnCommand, "", "", 16);
                } else {
                    string btnText = (user as ServerUsers.User).username;
                    string btnCommand = string.Format(aCommandFmt, (user as ServerUsers.User).steamid);

                    if (string.IsNullOrEmpty(btnText) || UNKNOWN_NAME_LIST.Contains(btnText.ToLower()))
                        btnText = (user as ServerUsers.User).steamid.ToString();

                    aUIObj.AddButton(aParent, LBAnchor, RTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, btnText, btnCommand, "", "", 16);
                }
            };
        }

        private string _(string aKey, string aPlayerId, params object[] args) => string.Format(lang.GetMessage(aKey, this, aPlayerId), args);
        private void LogError(string aMessage) => LogToFile("", $"[{DateTime.Now.ToString("hh:mm:ss")}] ERROR > {aMessage}", this);
        private void LogInfo(string aMessage) => LogToFile("", $"[{DateTime.Now.ToString("hh:mm:ss")}] INFO > {aMessage}", this);
      //  Слив плагинов server-rust by Apolo YouGame
        private void SendMessage(ref BasePlayer aPlayer, string aMessage) => rust.SendChatMessage(aPlayer, "", aMessage);

        private bool VerifyPermission(ref BasePlayer aPlayer, string aPermission)
        {
            if (permission.UserHasPermission(aPlayer.UserIDString, aPermission)) // User MUST have the required permission
                return true;

            SendMessage(ref aPlayer, _("Permission Error Text", aPlayer.UserIDString));
            LogError(_("Permission Error Log Text", aPlayer.UserIDString, aPlayer.displayName, aPermission));
            return false;
        }

        private List<BasePlayer> GetServerUserList(bool aIndOffline = false)
        {
            List<BasePlayer> result = new List<BasePlayer>();

            if (!aIndOffline) {
                Player.Players.ForEach(user => {
                    ServerUsers.User servUser = ServerUsers.Get(user.userID);

                    if (servUser == null || servUser?.group != ServerUsers.UserGroup.Banned)
                        result.Add(user);
                });
            } else {
                Player.Sleepers.ForEach(user => {
                    ServerUsers.User servUser = ServerUsers.Get(user.userID);

                    if (servUser == null || servUser?.group != ServerUsers.UserGroup.Banned)
                        result.Add(user);
                });
            }

            return result;
        }

        private List<ServerUsers.User> GetBannedUserList() => ServerUsers.GetAll(ServerUsers.UserGroup.Banned).ToList();


        private bool GetTargetFromArg(ref ConsoleSystem.Arg aArg, out ulong aTarget)
        {
            aTarget = 0;

            if (!aArg.HasArgs() || !ulong.TryParse(aArg.Args[0], out aTarget))
                return false;

            return true;
        }

        private bool GetTargetAmountFromArg(ref ConsoleSystem.Arg aArg, out ulong aTarget, out float aAmount)
        {
            aTarget = 0;
            aAmount = 0;

            if (!aArg.HasArgs(2) || !ulong.TryParse(aArg.Args[0], out aTarget) || !float.TryParse(aArg.Args[1], out aAmount))
                return false;

            return true;
        }
        #endregion Utility methods

        #region GUI build methods
        private void BuildTabMenu(ref Cui aUIObj, UiPage aPageType)
        {
            string uiUserId = aUIObj.GetPlayerId();
            string headerPanel = aUIObj.AddPanel(aUIObj.MainPanelName, TabMenuHeaderContainerLBAnchor, TabMenuHeaderContainerRTAnchor, false, CuiDefaultColors.None);
            string tabBtnPanel = aUIObj.AddPanel(aUIObj.MainPanelName, TabMenuTabBtnContainerLBAnchor, TabMenuTabBtnContainerRTAnchor, false, CuiDefaultColors.Background);
            
			aUIObj.AddLabel(headerPanel, TabMenuHeaderLblLBAnchor, TabMenuHeaderLblRTAnchor, CuiDefaultColors.TextTitle, "AdminCP", "", 22, TextAnchor.MiddleCenter);
            aUIObj.AddButton(headerPanel, TabMenuCloseBtnLBAnchor, TabMenuCloseBtnRTAnchor, CuiDefaultColors.ButtonDecline, CuiDefaultColors.TextAlt, "X", "admcp_closeui", "", null, 22);
            
            AddTabMenuBtn(ref aUIObj, tabBtnPanel, _("Main Tab Text", uiUserId), "admcp_switchui Main", 0, (aPageType == UiPage.Main ? true : false));
            AddTabMenuBtn(ref aUIObj, tabBtnPanel, _("Online Player Tab Text", uiUserId), "admcp_switchui PlayersOnline 0", 1, (aPageType == UiPage.PlayersOnline ? true : false));
            AddTabMenuBtn(ref aUIObj, tabBtnPanel, _("Offline Player Tab Text", uiUserId), "admcp_switchui PlayersOffline 0", 2, (aPageType == UiPage.PlayersOffline ? true : false));
            AddTabMenuBtn(ref aUIObj, tabBtnPanel, _("Banned Player Tab Text", uiUserId), "admcp_switchui PlayersBanned 0", 3, (aPageType == UiPage.PlayersBanned ? true : false));
        }

        private void BuildMainPage(ref Cui aUIObj)
        {
            string uiUserId = aUIObj.GetPlayerId();
            string panel = aUIObj.AddPanel(aUIObj.MainPanelName, MainPagePanelLBAnchor, MainPagePanelRTAnchor, false, CuiDefaultColors.Background);
            aUIObj.AddLabel(panel, MainPageLblTitleLBAnchor, MainPageLblTitleRTAnchor, CuiDefaultColors.TextAlt, "", "", 18, TextAnchor.MiddleLeft);
            aUIObj.AddLabel(panel, MainPageLblBanByIdTitleLBAnchor, MainPageLblBanByIdTitleRTAnchor, CuiDefaultColors.TextTitle, _("Ban By ID Title Text", uiUserId), null, 16, TextAnchor.MiddleLeft);
            aUIObj.AddLabel(panel, MainPageLblBanByIdLBAnchor, MainPageLblBanByIdRTAnchor, CuiDefaultColors.TextAlt, _("Ban By ID Label Text", uiUserId), null, 14, TextAnchor.MiddleLeft);
            string panelBanByIdGroup = aUIObj.AddPanel(panel, MainPagePanelBanByIdLBAnchor, MainPagePanelBanByIdRTAnchor, false, CuiDefaultColors.BackgroundDark);
            aUIObj.AddInputField(panelBanByIdGroup, MainPageEdtBanByIdLBAnchor, MainPageEdtBanByIdRTAnchor, CuiDefaultColors.TextAlt, null, 24, "admcp_mainpagebanidinputtext");

            if (configData.EnableBan) {
                aUIObj.AddButton(panel, MainPageBtnBanByIdLBAnchor, MainPageBtnBanByIdRTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, "Ban", "admcp_mainpagebanbyid");
            } else {
                aUIObj.AddButton(panel, MainPageBtnBanByIdLBAnchor, MainPageBtnBanByIdRTAnchor, CuiDefaultColors.ButtonInactive, CuiDefaultColors.TextAlt, "Ban");
            }
        }

        private void BuildUserBtnPage(ref Cui aUIObj, UiPage aPageType, int aPage)
        {
            string pageLabel = _("User Button Page Title Text", aUIObj.GetPlayerId());
            string npBtnCommandFmt;
            int userCount;
            string panel = aUIObj.AddPanel(aUIObj.MainPanelName, UserBtnPanelLBAnchor, UserBtnPanelRTAnchor, false, CuiDefaultColors.Background);
            aUIObj.AddLabel(panel, UserBtnLblLBAnchor, UserBtnLblRTAnchor, CuiDefaultColors.TextAlt, pageLabel, "", 18, TextAnchor.MiddleLeft);

            if (aPageType == UiPage.PlayersOnline || aPageType == UiPage.PlayersOffline) {
                BuildUserButtons(ref aUIObj, panel, aPageType, ref aPage, out npBtnCommandFmt, out userCount);
            } else {
                BuildBannedUserButtons(ref aUIObj, panel, ref aPage, out npBtnCommandFmt, out userCount);
            }

            if (aPage == 0) {
                aUIObj.AddButton(panel, UserBtnPreviousBtnLBAnchor, UserBtnPreviousBtnRTAnchor, CuiDefaultColors.ButtonInactive, CuiDefaultColors.TextAlt, "<<", "", "", "", 18);
            } else {
                aUIObj.AddButton(panel, UserBtnPreviousBtnLBAnchor, UserBtnPreviousBtnRTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, "<<", string.Format(npBtnCommandFmt, aPage - 1), "", "", 18);
            };

            if (userCount > MAX_PLAYER_BUTTONS * (aPage + 1)) {
                aUIObj.AddButton(panel, UserBtnNextBtnLBAnchor, UserBtnNextBtnRTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, ">>", string.Format(npBtnCommandFmt, aPage + 1), "", "", 18);
            } else {
                aUIObj.AddButton(panel, UserBtnNextBtnLBAnchor, UserBtnNextBtnRTAnchor, CuiDefaultColors.ButtonInactive, CuiDefaultColors.TextAlt, ">>", "", "", "", 18);
            };
        }

        private void BuildUserButtons(ref Cui aUIObj, string aParent, UiPage aPageType, ref int aPage, out string aBtnCommandFmt, out int aUserCount)
        {
            string commandFmt = "admcp_switchui PlayerPage {0}";
            List<BasePlayer> userList;

            if (aPageType == UiPage.PlayersOnline) {
                userList = GetServerUserList();
                aBtnCommandFmt = "admcp_switchui PlayersOnline {0}";
            } else {
                userList = GetServerUserList(true);
                aBtnCommandFmt = "admcp_switchui PlayersOffline {0}";
            }

            aUserCount = userList.Count;

            if ((aPage != 0) && (userList.Count <= MAX_PLAYER_BUTTONS))
                aPage = 0;

            AddPlayerButtons(ref aUIObj, aParent, ref userList, commandFmt, aPage);
        }

        private void BuildBannedUserButtons(ref Cui aUIObj, string aParent, ref int aPage, out string aBtnCommandFmt, out int aUserCount)
        {
            string commandFmt = "admcp_switchui PlayerPageBanned {0}";
            List<ServerUsers.User> userList = GetBannedUserList();
            aBtnCommandFmt = "admcp_switchui PlayersBanned {0}";
            aUserCount = userList.Count;

            if ((aPage != 0) && (userList.Count <= MAX_PLAYER_BUTTONS))
                aPage = 0; // Reset page to 0 if user count is lower or equal to max button count

            AddPlayerButtons(ref aUIObj, aParent, ref userList, commandFmt, aPage);
        }

        private void BuildUserPage(ref Cui aUIObj, UiPage aPageType, ulong aPlayerId)
        {
            string uiUserId = aUIObj.GetPlayerId();

            string panel = aUIObj.AddPanel(aUIObj.MainPanelName, UserPagePanelLBAnchor, UserPagePanelRTAnchor, false, CuiDefaultColors.Background);
            string infoPanel = aUIObj.AddPanel(panel, UserPageInfoPanelLBAnchor, UserPageInfoPanelRTAnchor, false, CuiDefaultColors.BackgroundMedium);
      //  Слив плагинов server-rust by Apolo YouGame
            string actionPanel = aUIObj.AddPanel(panel, UserPageActionPanelLBAnchor, UserPageActionPanelRTAnchor, false, CuiDefaultColors.BackgroundMedium);

            aUIObj.AddLabel(infoPanel, UserPageLblinfoTitleLBAnchor, UserPageLblinfoTitleRTAnchor, CuiDefaultColors.TextTitle, _("Player Info Label Text", uiUserId), "", 14, TextAnchor.MiddleLeft);
      //  Слив плагинов server-rust by Apolo YouGame
            aUIObj.AddLabel(actionPanel, UserPageLblActionTitleLBAnchor, UserPageLblActionTitleRTAnchor, CuiDefaultColors.TextTitle, _("Player Actions Label Text", uiUserId), "", 14, TextAnchor.MiddleLeft);

            if (aPageType == UiPage.PlayerPage) {
                BasePlayer player = BasePlayer.FindByID(aPlayerId) ?? BasePlayer.FindSleeping(aPlayerId);
                bool playerConnected = player.IsConnected;
                string lastCheatStr = _("Never Label Text", uiUserId);
                string authLevel = ServerUsers.Get(aPlayerId)?.group.ToString() ?? "None";

                if (player.lastAdminCheatTime != 0f) {
                    TimeSpan lastCheatSinceStart = new TimeSpan(0, 0, (int)(Time.realtimeSinceStartup - player.lastAdminCheatTime));
                    DateTime lastCheat = DateTime.UtcNow.Subtract(lastCheatSinceStart);
                    lastCheatStr = $"{lastCheat.ToString(@"yyyy\/MM\/dd HH:mm:ss")} UTC";
                };

                aUIObj.AddLabel(panel, UserPageLblLBAnchor, UserPageLblRTAnchor, CuiDefaultColors.TextAlt, _("User Page Title Format", uiUserId, player.displayName, ""), "", 18, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblIdLBAnchor, UserPageLblIdRTAnchor, CuiDefaultColors.TextAlt, _("Id Label Format", uiUserId, aPlayerId, (player.IsDeveloper ? _("Dev Label Text", uiUserId) : "")), "", 14, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblAuthLBAnchor, UserPageLblAuthRTAnchor, CuiDefaultColors.TextAlt, _("Auth Level Label Format", uiUserId, authLevel), "", 14, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblConnectLBAnchor, UserPageLblConnectRTAnchor, CuiDefaultColors.TextAlt, _("Connection Label Format", uiUserId, ( playerConnected ? _("Connected Label Text", uiUserId) : _("Disconnected Label Text", uiUserId))  ), "", 14, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblSleepLBAnchor, UserPageLblSleepRTAnchor, CuiDefaultColors.TextAlt, _("Status Label Format", uiUserId, ( player.IsSleeping() ? _("Sleeping Label Text", uiUserId) : _("Awake Label Text", uiUserId)  ), ( player.IsAlive() ? _("Alive Label Text", uiUserId) : _("Dead Label Text", uiUserId)) ), "", 14, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblFlagLBAnchor, UserPageLblFlagRTAnchor, CuiDefaultColors.TextAlt, _("Flags Label Format", uiUserId, (player.IsFlying ? _("Flying Label Text", uiUserId) : ""), (player.isMounted ? _("Mounted Label Text", uiUserId) : "") ), "", 14, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblPosLBAnchor, UserPageLblPosRTAnchor, CuiDefaultColors.TextAlt, _("Position Label Format", uiUserId, player.ServerPosition), "", 14, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblRotLBAnchor, UserPageLblRotRTAnchor, CuiDefaultColors.TextAlt, _("Rotation Label Format", uiUserId, player.GetNetworkRotation()), "", 14, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblAdminCheatLBAnchor, UserPageLblAdminCheatRTAnchor, CuiDefaultColors.TextAlt, _("Last Admin Cheat Label Format", uiUserId, lastCheatStr), "", 14, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblIdleLBAnchor, UserPageLblIdleRTAnchor, CuiDefaultColors.TextAlt, _("Idle Time Label Format", uiUserId, Convert.ToInt32(player.IdleTime)), "", 14, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblHealthLBAnchor, UserPageLblHealthRTAnchor, CuiDefaultColors.TextAlt, _("Health Label Format", uiUserId, player.health), "", 14, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblCalLBAnchor, UserPageLblCalRTAnchor, CuiDefaultColors.TextAlt, _("Calories Label Format", uiUserId, player.metabolism?.calories?.value), "", 14, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblHydraLBAnchor, UserPageLblHydraRTAnchor, CuiDefaultColors.TextAlt, _("Hydration Label Format", uiUserId, player.metabolism?.hydration?.value), "", 14, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblTempLBAnchor, UserPageLblTempRTAnchor, CuiDefaultColors.TextAlt, _("Temp Label Format", uiUserId, player.metabolism?.temperature?.value), "", 14, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblWetLBAnchor, UserPageLblWetRTAnchor, CuiDefaultColors.TextAlt, _("Wetness Label Format", uiUserId, player.metabolism?.wetness?.value), "", 14, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblComfortLBAnchor, UserPageLblComfortRTAnchor, CuiDefaultColors.TextAlt, _("Comfort Label Format", uiUserId, player.metabolism?.comfort?.value), "", 14, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblBleedLBAnchor, UserPageLblBleedRTAnchor, CuiDefaultColors.TextAlt, _("Bleeding Label Format", uiUserId, player.metabolism?.bleeding?.value), "", 14, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblRads1LBAnchor, UserPageLblRads1RTAnchor, CuiDefaultColors.TextAlt, _("Radiation Label Format", uiUserId, player.metabolism?.radiation_poison?.value), "", 14, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblRads2LBAnchor, UserPageLblRads2RTAnchor, CuiDefaultColors.TextAlt, _("Radiation Protection Label Format", uiUserId, player.RadiationProtection()), "", 14, TextAnchor.MiddleLeft);

                /* Build player action panel */
                if (configData.EnableBan) {
                    aUIObj.AddButton(actionPanel, UserPageBtnBanLBAnchor, UserPageBtnBanRTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt,  _("Ban Button Text", uiUserId), $"admcp_banuser {aPlayerId}");
                } else {
                    aUIObj.AddButton(actionPanel, UserPageBtnBanLBAnchor, UserPageBtnBanRTAnchor, CuiDefaultColors.ButtonInactive, CuiDefaultColors.Text, _("Ban Button Text", uiUserId));
                }
                aUIObj.AddButton(actionPanel, UserPageBtnTpLBAnchor, UserPageBtnTpRTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt,  _("Tp Button Text", uiUserId), $"admcp_teleport2player {aPlayerId}");
                aUIObj.AddButton(actionPanel, UserPageBtnTp2LBAnchor, UserPageBtnTp2RTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt,  _("Tp2me Button Text", uiUserId), $"admcp_teleport2me {aPlayerId}");

                if (configData.EnableKill) {
                    aUIObj.AddButton(actionPanel, UserPageBtnKillLBAnchor, UserPageBtnKillRTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, _("Kill Button Text", uiUserId), $"admcp_killuser {aPlayerId}");
                } else {
                    aUIObj.AddButton(actionPanel, UserPageBtnKillLBAnchor, UserPageBtnKillRTAnchor, CuiDefaultColors.ButtonInactive, CuiDefaultColors.Text, _("Kill Button Text", uiUserId));
                }

                if (configData.EnableKick && playerConnected) {
                    aUIObj.AddButton(actionPanel, UserPageBtnKickLBAnchor, UserPageBtnKickRTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, _("Kick Button Text", uiUserId), $"admcp_kickuser {aPlayerId}");
                } else {
                    aUIObj.AddButton(actionPanel, UserPageBtnKickLBAnchor, UserPageBtnKickRTAnchor, CuiDefaultColors.ButtonInactive, CuiDefaultColors.Text, _("Kick Button Text", uiUserId));
                };

                // Add reset buttons
                if (configData.EnableClearInv) {
                    aUIObj.AddButton(actionPanel, UserPageBtnClearInventoryLBAnchor, UserPageBtnClearInventoryRTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, _("Clear Inventory Button Text", uiUserId), $"admcp_clearuserinventory {aPlayerId}");
                } else {
                    aUIObj.AddButton(actionPanel, UserPageBtnClearInventoryLBAnchor, UserPageBtnClearInventoryRTAnchor, CuiDefaultColors.ButtonInactive, CuiDefaultColors.Text, _("Clear Inventory Button Text", uiUserId));
                }

                if (configData.EnableResetBP) {
                    aUIObj.AddButton(actionPanel, UserPageBtnResetBPLBAnchor, UserPageBtnResetBPRTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, _("Reset Blueprints Button Text", uiUserId), $"admcp_resetuserblueprints {aPlayerId}");
                } else {
                    aUIObj.AddButton(actionPanel, UserPageBtnResetBPLBAnchor, UserPageBtnResetBPRTAnchor, CuiDefaultColors.ButtonInactive, CuiDefaultColors.Text, _("Reset Blueprints Button Text", uiUserId));
                }

                if (configData.EnableResetMetabolism) {
                    aUIObj.AddButton(actionPanel, UserPageBtnResetMetabolismLBAnchor, UserPageBtnResetMetabolismRTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, _("Reset Metabolism Button Text", uiUserId), $"admcp_resetusermetabolism {aPlayerId}");
                } else {
                    aUIObj.AddButton(actionPanel, UserPageBtnResetMetabolismLBAnchor, UserPageBtnResetMetabolismRTAnchor, CuiDefaultColors.ButtonInactive,  CuiDefaultColors.Text, _("Reset Metabolism Button Text", uiUserId));
                }

                // Add hurt buttons
                if (configData.EnableHurt) {
                    aUIObj.AddButton(actionPanel, UserPageBtnHurt25LBAnchor, UserPageBtnHurt25RTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, _("Hurt 25 Button Text", uiUserId), $"admcp_hurtuser {aPlayerId} 25");
                    aUIObj.AddButton(actionPanel, UserPageBtnHurt50LBAnchor, UserPageBtnHurt50RTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, _("Hurt 50 Button Text", uiUserId), $"admcp_hurtuser {aPlayerId} 50");
                    aUIObj.AddButton(actionPanel, UserPageBtnHurt75LBAnchor, UserPageBtnHurt75RTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, _("Hurt 75 Button Text", uiUserId), $"admcp_hurtuser {aPlayerId} 75");
                    aUIObj.AddButton(actionPanel, UserPageBtnHurt100LBAnchor, UserPageBtnHurt100RTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, _("Hurt 100 Button Text", uiUserId), $"admcp_hurtuser {aPlayerId} 100");
                } else {
                    aUIObj.AddButton(actionPanel, UserPageBtnHurt25LBAnchor, UserPageBtnHurt25RTAnchor, CuiDefaultColors.ButtonInactive, CuiDefaultColors.Text, _("Hurt 25 Button Text", uiUserId));
                    aUIObj.AddButton(actionPanel, UserPageBtnHurt50LBAnchor, UserPageBtnHurt50RTAnchor, CuiDefaultColors.ButtonInactive, CuiDefaultColors.Text, _("Hurt 50 Button Text", uiUserId));
                    aUIObj.AddButton(actionPanel, UserPageBtnHurt75LBAnchor, UserPageBtnHurt75RTAnchor, CuiDefaultColors.ButtonInactive, CuiDefaultColors.Text, _("Hurt 75 Button Text", uiUserId));
                    aUIObj.AddButton(actionPanel, UserPageBtnHurt100LBAnchor, UserPageBtnHurt100RTAnchor, CuiDefaultColors.ButtonInactive, CuiDefaultColors.Text, _("Hurt 100 Button Text", uiUserId));
                }

                // Add heal buttons
                if (configData.EnableHeal) {
                    aUIObj.AddButton(actionPanel, UserPageBtnHeal25LBAnchor, UserPageBtnHeal25RTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, _("Heal 25 Button Text", uiUserId), $"admcp_healuser {aPlayerId} 25");
                    aUIObj.AddButton(actionPanel, UserPageBtnHeal50LBAnchor, UserPageBtnHeal50RTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, _("Heal 50 Button Text", uiUserId), $"admcp_healuser {aPlayerId} 50");
                    aUIObj.AddButton(actionPanel, UserPageBtnHeal75LBAnchor, UserPageBtnHeal75RTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, _("Heal 75 Button Text", uiUserId), $"admcp_healuser {aPlayerId} 75");
                    aUIObj.AddButton(actionPanel, UserPageBtnHeal100LBAnchor, UserPageBtnHeal100RTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, _("Heal 100 Button Text", uiUserId), $"admcp_healuser {aPlayerId} 100");
                } else {
                    aUIObj.AddButton(actionPanel, UserPageBtnHeal25LBAnchor, UserPageBtnHeal25RTAnchor, CuiDefaultColors.ButtonInactive, CuiDefaultColors.Text, _("Heal 25 Button Text", uiUserId));
                    aUIObj.AddButton(actionPanel, UserPageBtnHeal50LBAnchor, UserPageBtnHeal50RTAnchor, CuiDefaultColors.ButtonInactive, CuiDefaultColors.Text, _("Heal 50 Button Text", uiUserId));
                    aUIObj.AddButton(actionPanel, UserPageBtnHeal75LBAnchor, UserPageBtnHeal75RTAnchor, CuiDefaultColors.ButtonInactive, CuiDefaultColors.Text, _("Heal 75 Button Text", uiUserId));
                    aUIObj.AddButton(actionPanel, UserPageBtnHeal100LBAnchor, UserPageBtnHeal100RTAnchor, CuiDefaultColors.ButtonInactive, CuiDefaultColors.Text, _("Heal 100 Button Text", uiUserId));
                }
            } else {
                ServerUsers.User serverUser = ServerUsers.Get(aPlayerId);
				
                aUIObj.AddLabel(panel, UserPageLblLBAnchor, UserPageLblRTAnchor, CuiDefaultColors.TextAlt, _("User Page Title Format", uiUserId, serverUser.username, _("Banned Label Text", uiUserId)), "", 18, TextAnchor.MiddleLeft);
                aUIObj.AddLabel(infoPanel, UserPageLblIdLBAnchor, UserPageLblIdRTAnchor, CuiDefaultColors.TextAlt, _("Id Label Format", uiUserId, aPlayerId, ""), "", 14, TextAnchor.MiddleLeft);


                if (configData.EnableUnban) {
                    aUIObj.AddButton(actionPanel, UserPageBtnBanLBAnchor, UserPageBtnBanRTAnchor, CuiDefaultColors.Button, CuiDefaultColors.TextAlt, _("Unban Button Text", uiUserId), $"admcp_unbanuser {aPlayerId}");
                } else {
                    aUIObj.AddButton(actionPanel, UserPageBtnBanLBAnchor, UserPageBtnBanRTAnchor, CuiDefaultColors.ButtonInactive, CuiDefaultColors.Text, _("Unban Button Text", uiUserId));
                }
            }
        }

        private void BuildUI(BasePlayer aPlayer, UiPage aPageType, string aArg = "")
        {
            // Initiate the new UI and panel
            Cui newUiLib = new Cui(aPlayer);
            newUiLib.MainPanelName = newUiLib.AddPanel(Cui.PARENT_OVERLAY, MainLBAnchor, MainRTAnchor, true, CuiDefaultColors.BackgroundDark, MAIN_PANEL_NAME);
            BuildTabMenu(ref newUiLib, aPageType);

            switch (aPageType) {
                case UiPage.Main: {
                    BuildMainPage(ref newUiLib);
                    break;
                }
                case UiPage.PlayersOnline:
                case UiPage.PlayersOffline:
                case UiPage.PlayersBanned: {
                    int page = 0;

                    if (aArg != "")
                        if (!int.TryParse(aArg, out page))
                            page = 0; // Just to be sure

                    BuildUserBtnPage(ref newUiLib, aPageType, page);
                    break;
                }
                case UiPage.PlayerPage:
                case UiPage.PlayerPageBanned: {
                    ulong playerId = aPlayer.userID;

                    if (aArg != "")
                        if (!ulong.TryParse(aArg, out playerId))
                            playerId = aPlayer.userID; // Just to be sure

                    BuildUserPage(ref newUiLib, aPageType, playerId);
                    break;
                }
            };

            CuiHelper.DestroyUi(aPlayer, MAIN_PANEL_NAME);
            newUiLib.Draw();
        }
        #endregion GUI build methods

        #region Config
        private class ConfigData
        {
            [DefaultValue("true")]
            [JsonProperty("Enable kick action")]
            public bool EnableKick { get; set; }
            [DefaultValue("true")]
            [JsonProperty("Enable ban action")]
            public bool EnableBan { get; set; }
            [DefaultValue("true")]
            [JsonProperty("Enable unban action")]
            public bool EnableUnban { get; set; }
            [DefaultValue("true")]
            [JsonProperty("Enable kill action")]
            public bool EnableKill { get; set; }
            [DefaultValue("true")]
            [JsonProperty("Enable inventory clear action")]
            public bool EnableClearInv { get; set; }
            [DefaultValue("true")]
            [JsonProperty("Enable blueprint resetaction")]
            public bool EnableResetBP { get; set; }
            [DefaultValue("true")]
            [JsonProperty("Enable metabolism reset action")]
            public bool EnableResetMetabolism { get; set; }
            [DefaultValue("true")]
            [JsonProperty("Enable hurt action")]
            public bool EnableHurt { get; set; }
            [DefaultValue("true")]
            [JsonProperty("Enable heal action")]
            public bool EnableHeal { get; set; }
        }
        #endregion

        #region Constants
        private const int MAX_PLAYER_COLS = 5;
        private const int MAX_PLAYER_ROWS = 8;
        private const int MAX_PLAYER_BUTTONS = MAX_PLAYER_COLS * MAX_PLAYER_ROWS;
        private const string MAIN_PANEL_NAME = "ADMCP_MainPanel";
        private readonly List<string> UNKNOWN_NAME_LIST = new List<string> { "unnamed", "unknown" };
        /* Define layout */
        // Main panel bounds
        private readonly CuiPoint MainLBAnchor = new CuiPoint(0.03f, 0.15f);
        private readonly CuiPoint MainRTAnchor = new CuiPoint(0.97f, 0.97f);
        // Tab menu bounds
        private readonly CuiPoint TabMenuHeaderContainerLBAnchor = new CuiPoint(0.005f, 0.9085f);
        private readonly CuiPoint TabMenuHeaderContainerRTAnchor = new CuiPoint(0.995f, 1f);
        private readonly CuiPoint TabMenuTabBtnContainerLBAnchor = new CuiPoint(0.005f, 0.83f);
        private readonly CuiPoint TabMenuTabBtnContainerRTAnchor = new CuiPoint(0.995f, 0.90849f);
        private readonly CuiPoint TabMenuHeaderLblLBAnchor = new CuiPoint(0f, 0f);
        private readonly CuiPoint TabMenuHeaderLblRTAnchor = new CuiPoint(1f, 1f);
        private readonly CuiPoint TabMenuCloseBtnLBAnchor = new CuiPoint(0.965f, 0.1f);
        private readonly CuiPoint TabMenuCloseBtnRTAnchor = new CuiPoint(0.997f, 0.9f);
        // Main page bounds
        private readonly CuiPoint MainPagePanelLBAnchor = new CuiPoint(0.005f, 0.01f);
        private readonly CuiPoint MainPagePanelRTAnchor = new CuiPoint(0.995f, 0.817f);
        private readonly CuiPoint MainPageLblTitleLBAnchor = new CuiPoint(0.005f, 0.88f);
        private readonly CuiPoint MainPageLblTitleRTAnchor = new CuiPoint(0.995f, 0.99f);
        private readonly CuiPoint MainPageLblBanByIdTitleLBAnchor = new CuiPoint(0.005f, 0.82f);
        private readonly CuiPoint MainPageLblBanByIdTitleRTAnchor = new CuiPoint(0.995f, 0.87f);
        private readonly CuiPoint MainPageLblBanByIdLBAnchor = new CuiPoint(0.005f, 0.76f);
        private readonly CuiPoint MainPageLblBanByIdRTAnchor = new CuiPoint(0.05f, 0.81f);
        private readonly CuiPoint MainPagePanelBanByIdLBAnchor = new CuiPoint(0.055f, 0.76f);
        private readonly CuiPoint MainPagePanelBanByIdRTAnchor = new CuiPoint(0.305f, 0.81f);
        private readonly CuiPoint MainPageEdtBanByIdLBAnchor = new CuiPoint(0.005f, 0f);
        private readonly CuiPoint MainPageEdtBanByIdRTAnchor = new CuiPoint(0.995f, 1f);
        private readonly CuiPoint MainPageBtnBanByIdLBAnchor = new CuiPoint(0.315f, 0.76f);
        private readonly CuiPoint MainPageBtnBanByIdRTAnchor = new CuiPoint(0.365f, 0.81f);
        // User button page bounds 
        private readonly CuiPoint UserBtnPanelLBAnchor = new CuiPoint(0.005f, 0.01f);
        private readonly CuiPoint UserBtnPanelRTAnchor = new CuiPoint(0.995f, 0.817f);
        private readonly CuiPoint UserBtnLblLBAnchor = new CuiPoint(0.005f, 0.88f);
        private readonly CuiPoint UserBtnLblRTAnchor = new CuiPoint(0.995f, 0.99f);
        private readonly CuiPoint UserBtnPreviousBtnLBAnchor = new CuiPoint(0.005f, 0.01f);
        private readonly CuiPoint UserBtnPreviousBtnRTAnchor = new CuiPoint(0.035f, 0.061875f);
        private readonly CuiPoint UserBtnNextBtnLBAnchor = new CuiPoint(0.96f, 0.01f);
        private readonly CuiPoint UserBtnNextBtnRTAnchor = new CuiPoint(0.995f, 0.061875f);
        // User page panel bounds
        private readonly CuiPoint UserPagePanelLBAnchor = new CuiPoint(0.005f, 0.01f);
        private readonly CuiPoint UserPagePanelRTAnchor = new CuiPoint(0.995f, 0.817f);
        private readonly CuiPoint UserPageInfoPanelLBAnchor = new CuiPoint(0.005f, 0.01f);
      //  Слив плагинов server-rust by Apolo YouGame
        private readonly CuiPoint UserPageInfoPanelRTAnchor = new CuiPoint(0.28f, 0.87f);
      //  Слив плагинов server-rust by Apolo YouGame
        private readonly CuiPoint UserPageActionPanelLBAnchor = new CuiPoint(0.285f, 0.01f);
        private readonly CuiPoint UserPageActionPanelRTAnchor = new CuiPoint(0.995f, 0.87f);
        // User page title label bounds
        private readonly CuiPoint UserPageLblLBAnchor = new CuiPoint(0.005f, 0.88f);
        private readonly CuiPoint UserPageLblRTAnchor = new CuiPoint(0.995f, 0.99f);
        private readonly CuiPoint UserPageLblinfoTitleLBAnchor = new CuiPoint(0.025f, 0.94f);
        private readonly CuiPoint UserPageLblinfoTitleRTAnchor = new CuiPoint(0.975f, 0.99f);
        private readonly CuiPoint UserPageLblActionTitleLBAnchor = new CuiPoint(0.01f, 0.94f);
        private readonly CuiPoint UserPageLblActionTitleRTAnchor = new CuiPoint(0.99f, 0.99f);
        // User page info label bounds
        private readonly CuiPoint UserPageLblIdLBAnchor = new CuiPoint(0.025f, 0.87f);
        private readonly CuiPoint UserPageLblIdRTAnchor = new CuiPoint(0.975f, 0.92f);
        private readonly CuiPoint UserPageLblAuthLBAnchor = new CuiPoint(0.025f, 0.81f);
        private readonly CuiPoint UserPageLblAuthRTAnchor = new CuiPoint(0.975f, 0.86f);
        private readonly CuiPoint UserPageLblConnectLBAnchor = new CuiPoint(0.025f, 0.75f);
        private readonly CuiPoint UserPageLblConnectRTAnchor = new CuiPoint(0.975f, 0.80f);
        private readonly CuiPoint UserPageLblSleepLBAnchor = new CuiPoint(0.025f, 0.69f);
        private readonly CuiPoint UserPageLblSleepRTAnchor = new CuiPoint(0.975f, 0.74f);
        private readonly CuiPoint UserPageLblFlagLBAnchor = new CuiPoint(0.025f, 0.63f);
        private readonly CuiPoint UserPageLblFlagRTAnchor = new CuiPoint(0.975f, 0.68f);
        private readonly CuiPoint UserPageLblPosLBAnchor = new CuiPoint(0.025f, 0.57f);
        private readonly CuiPoint UserPageLblPosRTAnchor = new CuiPoint(0.975f, 0.62f);
        private readonly CuiPoint UserPageLblRotLBAnchor = new CuiPoint(0.025f, 0.51f);
        private readonly CuiPoint UserPageLblRotRTAnchor = new CuiPoint(0.975f, 0.56f);
        private readonly CuiPoint UserPageLblAdminCheatLBAnchor = new CuiPoint(0.025f, 0.45f);
        private readonly CuiPoint UserPageLblAdminCheatRTAnchor = new CuiPoint(0.975f, 0.50f);
        private readonly CuiPoint UserPageLblIdleLBAnchor = new CuiPoint(0.025f, 0.39f);
        private readonly CuiPoint UserPageLblIdleRTAnchor = new CuiPoint(0.975f, 0.44f);
        private readonly CuiPoint UserPageLblHealthLBAnchor = new CuiPoint(0.025f, 0.25f);
        private readonly CuiPoint UserPageLblHealthRTAnchor = new CuiPoint(0.975f, 0.30f);
        private readonly CuiPoint UserPageLblCalLBAnchor = new CuiPoint(0.025f, 0.19f);
        private readonly CuiPoint UserPageLblCalRTAnchor = new CuiPoint(0.5f, 0.24f);
        private readonly CuiPoint UserPageLblHydraLBAnchor = new CuiPoint(0.5f, 0.19f);
        private readonly CuiPoint UserPageLblHydraRTAnchor = new CuiPoint(0.975f, 0.24f);
        private readonly CuiPoint UserPageLblTempLBAnchor = new CuiPoint(0.025f, 0.13f);
        private readonly CuiPoint UserPageLblTempRTAnchor = new CuiPoint(0.5f, 0.18f);
        private readonly CuiPoint UserPageLblWetLBAnchor = new CuiPoint(0.5f, 0.13f);
        private readonly CuiPoint UserPageLblWetRTAnchor = new CuiPoint(0.975f, 0.18f);
        private readonly CuiPoint UserPageLblComfortLBAnchor = new CuiPoint(0.025f, 0.07f);
        private readonly CuiPoint UserPageLblComfortRTAnchor = new CuiPoint(0.5f, 0.12f);
        private readonly CuiPoint UserPageLblBleedLBAnchor = new CuiPoint(0.5f, 0.07f);
        private readonly CuiPoint UserPageLblBleedRTAnchor = new CuiPoint(0.975f, 0.12f);
        private readonly CuiPoint UserPageLblRads1LBAnchor = new CuiPoint(0.025f, 0.01f);
        private readonly CuiPoint UserPageLblRads1RTAnchor = new CuiPoint(0.5f, 0.06f);
        private readonly CuiPoint UserPageLblRads2LBAnchor = new CuiPoint(0.5f, 0.01f);
        private readonly CuiPoint UserPageLblRads2RTAnchor = new CuiPoint(0.975f, 0.06f);
        // User page button bounds
        private readonly CuiPoint UserPageBtnBanLBAnchor = new CuiPoint(0.01f, 0.85f);
        private readonly CuiPoint UserPageBtnBanRTAnchor = new CuiPoint(0.16f, 0.92f);
        private readonly CuiPoint UserPageBtnTpLBAnchor = new CuiPoint(0.17f, 0.85f);
        private readonly CuiPoint UserPageBtnTpRTAnchor = new CuiPoint(0.32f, 0.92f);
        private readonly CuiPoint UserPageBtnTp2LBAnchor = new CuiPoint(0.33f, 0.85f);
        private readonly CuiPoint UserPageBtnTp2RTAnchor = new CuiPoint(0.48f, 0.92f);
        private readonly CuiPoint UserPageBtnKillLBAnchor = new CuiPoint(0.01f, 0.76f);
        private readonly CuiPoint UserPageBtnKillRTAnchor = new CuiPoint(0.16f, 0.83f);
        private readonly CuiPoint UserPageBtnKickLBAnchor = new CuiPoint(0.17f, 0.76f);
        private readonly CuiPoint UserPageBtnKickRTAnchor = new CuiPoint(0.32f, 0.83f);
        private readonly CuiPoint UserPageBtnClearInventoryLBAnchor = new CuiPoint(0.01f, 0.67f);
        private readonly CuiPoint UserPageBtnClearInventoryRTAnchor = new CuiPoint(0.16f, 0.74f);
        private readonly CuiPoint UserPageBtnResetBPLBAnchor = new CuiPoint(0.17f, 0.67f);
        private readonly CuiPoint UserPageBtnResetBPRTAnchor = new CuiPoint(0.32f, 0.74f);
        private readonly CuiPoint UserPageBtnResetMetabolismLBAnchor = new CuiPoint(0.33f, 0.67f);
        private readonly CuiPoint UserPageBtnResetMetabolismRTAnchor = new CuiPoint(0.48f, 0.74f);
        private readonly CuiPoint UserPageBtnHurt25LBAnchor = new CuiPoint(0.01f, 0.49f);
        private readonly CuiPoint UserPageBtnHurt25RTAnchor = new CuiPoint(0.16f, 0.56f);
        private readonly CuiPoint UserPageBtnHurt50LBAnchor = new CuiPoint(0.17f, 0.49f);
        private readonly CuiPoint UserPageBtnHurt50RTAnchor = new CuiPoint(0.32f, 0.56f);
        private readonly CuiPoint UserPageBtnHurt75LBAnchor = new CuiPoint(0.33f, 0.49f);
        private readonly CuiPoint UserPageBtnHurt75RTAnchor = new CuiPoint(0.48f, 0.56f);
        private readonly CuiPoint UserPageBtnHurt100LBAnchor = new CuiPoint(0.49f, 0.49f);
        private readonly CuiPoint UserPageBtnHurt100RTAnchor = new CuiPoint(0.64f, 0.56f);
        private readonly CuiPoint UserPageBtnHeal25LBAnchor = new CuiPoint(0.01f, 0.40f);
        private readonly CuiPoint UserPageBtnHeal25RTAnchor = new CuiPoint(0.16f, 0.47f);
        private readonly CuiPoint UserPageBtnHeal50LBAnchor = new CuiPoint(0.17f, 0.40f);
        private readonly CuiPoint UserPageBtnHeal50RTAnchor = new CuiPoint(0.32f, 0.47f);
        private readonly CuiPoint UserPageBtnHeal75LBAnchor = new CuiPoint(0.33f, 0.40f);
        private readonly CuiPoint UserPageBtnHeal75RTAnchor = new CuiPoint(0.48f, 0.47f);
        private readonly CuiPoint UserPageBtnHeal100LBAnchor = new CuiPoint(0.49f, 0.40f);
        private readonly CuiPoint UserPageBtnHeal100RTAnchor = new CuiPoint(0.64f, 0.47f);
        #endregion Constants

        #region Variables
        private ConfigData configData;
        // Format: <userId, text>
        private Dictionary<ulong, string> mainPageBanIdInputText = new Dictionary<ulong, string>();
        #endregion Variables

        #region Hooks
        void Loaded()
        {
            configData = Config.ReadObject<ConfigData>();
            permission.RegisterPermission("playeracp.show", this);
        }

        void Unload()
        {
            foreach (BasePlayer player in Player.Players) {
                CuiHelper.DestroyUi(player, MAIN_PANEL_NAME);

                if (mainPageBanIdInputText.ContainsKey(player.userID))
                    mainPageBanIdInputText.Remove(player.userID);
            }
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (mainPageBanIdInputText.ContainsKey(player.userID))
                mainPageBanIdInputText.Remove(player.userID);
        }

        protected override void LoadDefaultConfig()
        {
            ConfigData config = new ConfigData {
                EnableKick = true,
                EnableBan = true,
                EnableUnban = true,
                EnableKill = true,
                EnableClearInv = true,
                EnableResetBP = true,
                EnableResetMetabolism = true,
                EnableHurt = true,
                EnableHeal = true
            };
            Config.WriteObject(config, true);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string> {
                { "Permission Error Text", "You do not have the required permissions to use this command." },
                { "Permission Error Log Text", "{0}: Tried to execute a command requiring the '{1}' permission" },
                { "Kick Reason Message Text", "Administrative decision" },
                { "Ban Reason Message Text", "Administrative decision" },

                { "Never Label Text", "Never" },
                { "Banned Label Text", " (Banned)" },
                { "Dev Label Text", " (Developer)" },
                { "Connected Label Text", "Connected" },
                { "Disconnected Label Text", "Disconnected" },
                { "Sleeping Label Text", "Sleeping" },
                { "Awake Label Text", "Awake" },
                { "Alive Label Text", "Alive" },
                { "Dead Label Text", "Dead" },
                { "Flying Label Text", " Flying" },
                { "Mounted Label Text", " Mounted" },

                { "User Button Page Title Text", "Click a username to go to the player's control page" },
                { "User Page Title Format", "Control page for player '{0}'{1}" },

                { "Ban By ID Title Text", "Ban a user by ID" },
                { "Ban By ID Label Text", "User ID:" },
                { "Player Info Label Text", "Player information:" },
      //  Слив плагинов server-rust by Apolo YouGame
                { "Player Actions Label Text", "Player actions:" },

                { "Id Label Format", "ID: {0}{1}" },
                { "Auth Level Label Format", "Auth level: {0}" },
                { "Connection Label Format", "Connection: {0}" },
                { "Status Label Format", "Status: {0} and {1}" },
                { "Flags Label Format", "Flags:{0}{1}" },
                { "Position Label Format", "Position: {0}" },
                { "Rotation Label Format", "Rotation: {0}" },
                { "Last Admin Cheat Label Format", "Last admin cheat: {0}" },
                { "Idle Time Label Format", "Idle time: {0} seconds" },
                { "Health Label Format", "Health: {0}" },
                { "Calories Label Format", "Calories: {0}" },
                { "Hydration Label Format", "Hydration: {0}" },
                { "Temp Label Format", "Temperature: {0}" },
                { "Wetness Label Format", "Wetness: {0}" },
                { "Comfort Label Format", "Comfort: {0}" },
                { "Bleeding Label Format", "Bleeding: {0}" },
                { "Radiation Label Format", "Radiation: {0}" },
                { "Radiation Protection Label Format", "Protection: {0}" },

                { "Main Tab Text", "Main" },
                { "Online Player Tab Text", "Online Players" },
                { "Offline Player Tab Text", "Offline Players" },
                { "Banned Player Tab Text", "Banned Players" },

                { "Clear Inventory Button Text", "Clear Inventory" },
                { "Reset Blueprints Button Text", "Reset Blueprints" },
                { "Reset Metabolism Button Text", "Reset Metabolism" },

                { "Hurt 25 Button Text", "Hurt 25" },
                { "Hurt 50 Button Text", "Hurt 50" },
                { "Hurt 75 Button Text", "Hurt 75" },
                { "Hurt 100 Button Text", "Hurt 100" },

                { "Heal 25 Button Text", "Heal 25" },
                { "Heal 50 Button Text", "Heal 50" },
                { "Heal 75 Button Text", "Heal 75" },
                { "Heal 100 Button Text", "Heal 100" },

                { "Ban Button Text", "Ban" },
                { "Tp Button Text", "ТП к игроку" },
                { "Tp2me Button Text", "ТП игрока" },
                { "Kick Button Text", "Kick" },
                { "Kill Button Text", "Kill" },
                { "Unban Button Text", "Unban" }
            }, this, "en");
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "Permission Error Text", "У вас нет доступа к этой команде." },
                { "Permission Error Log Text", "{0}: пытался использовать команду требующую '{1}' привилегии" },
                { "Kick Reason Message Text", "Решение администратора" },
                { "Ban Reason Message Text", "Решение администратора" },

                { "Never Label Text", "Навсегда" },
                { "Banned Label Text", " (Заблокирован)" },
                { "Dev Label Text", " (Разработчик)" },
                { "Connected Label Text", "Онлайн" },
                { "Disconnected Label Text", "Офлайн" },
                { "Sleeping Label Text", "Спящий" },
                { "Awake Label Text", "Бодрствующий" },
                { "Alive Label Text", "Живой" },
                { "Dead Label Text", "Мёртвый" },
                { "Flying Label Text", " Летающий" },
                { "Mounted Label Text", " Mounted" },

                { "User Button Page Title Text", "Кликните по нику чтобы попасть на страницу управления" },
                { "User Page Title Format", "Страница управления игрока '{0}'{1}" },

                { "Ban By ID Title Text", "Заблокировать игрока по ID" },
                { "Ban By ID Label Text", "ID Игрока:" },
                { "Player Info Label Text", "Информация о игроке:" },
      //  Слив плагинов server-rust by Apolo YouGame
                { "Player Actions Label Text", "Действия:" },

                { "Id Label Format", "ID: {0}{1}" },
                { "Auth Level Label Format", "Уровень авторизации: {0}" },
                { "Connection Label Format", "Состояние: {0}" },
                { "Status Label Format", "Статус: {0} и {1}" },
                { "Flags Label Format", "Флаги:{0}{1}" },
                { "Position Label Format", "Позиция: {0}" },
                { "Rotation Label Format", "Ротейт: {0}" },
                { "Last Admin Cheat Label Format", "Использование: {0}" },
                { "Idle Time Label Format", "Время в сети: {0} seconds" },
                { "Health Label Format", "Здоровье: {0}" },
                { "Calories Label Format", "Голод: {0}" },
                { "Hydration Label Format", "Жажда: {0}" },
                { "Temp Label Format", "Температура: {0}" },
                { "Wetness Label Format", "Влажность: {0}" },
                { "Comfort Label Format", "Комфорт: {0}" },
                { "Bleeding Label Format", "Кровотечение: {0}" },
                { "Radiation Label Format", "Радиация: {0}" },
                { "Radiation Protection Label Format", "Защита: {0}" },

                { "Main Tab Text", "Главная" },
                { "Online Player Tab Text", "Онлайн Игроки" },
                { "Offline Player Tab Text", "Офлайн Игроки" },
                { "Banned Player Tab Text", "Заблокированные Игроки" },

                { "Clear Inventory Button Text", "Очистить инвентарь" },
                { "Reset Blueprints Button Text", "Сбросить чертежи" },
                { "Reset Metabolism Button Text", "Сбросить метаболизм" },

                { "Hurt 25 Button Text", "Ударить на 25" },
                { "Hurt 50 Button Text", "Ударить на 50" },
                { "Hurt 75 Button Text", "Ударить на 75" },
                { "Hurt 100 Button Text", "Ударить на 100" },

                { "Heal 25 Button Text", "Вылечить на 25" },
                { "Heal 50 Button Text", "Вылечить на 50" },
                { "Heal 75 Button Text", "Вылечить на 75" },
                { "Heal 100 Button Text", "Вылечить на 100" },

                { "Ban Button Text", "Заблокировать" },
                { "Kick Button Text", "Кикнуть" },
                { "Kill Button Text", "Убить" },
                { "Unban Button Text", "Разблокировать" }
            }, this, "ru");
        }
        #endregion Hooks

        #region Command Callbacks
        [ChatCommand("admincp")]
        void PlayerManagerUICallback(BasePlayer aPlayer, string aCommand, string[] aArgs)
        {
            if (!VerifyPermission(ref aPlayer, "playeracp.show"))
                return;

            LogInfo($"{aPlayer.displayName}: Opened the menu");
      //  Слив плагинов server-rust by Apolo YouGame
            BuildUI(aPlayer, UiPage.Main);
        }

        [ConsoleCommand("admcp_closeui")]
        void PlayerManagerCloseUICallback(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            CuiHelper.DestroyUi(arg.Player(), MAIN_PANEL_NAME);

            if (mainPageBanIdInputText.ContainsKey(player.userID))
                mainPageBanIdInputText.Remove(player.userID);
        }

        [ConsoleCommand("admcp_switchui")]
        void PlayerManagerSwitchUICallback(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();

            if (!VerifyPermission(ref player, "playeracp.show") || !arg.HasArgs())
                return;

            switch (arg.Args[0].ToLower()) {
                case "playersonline": {
                    BuildUI(player, UiPage.PlayersOnline, (arg.HasArgs(2) ? arg.Args[1] : ""));
                    break;
                }
                case "playersoffline": {
                    BuildUI(player, UiPage.PlayersOffline, (arg.HasArgs(2) ? arg.Args[1] : ""));
                    break;
                }
                case "playersbanned": {
                    BuildUI(player, UiPage.PlayersBanned, (arg.HasArgs(2) ? arg.Args[1] : ""));
                    break;
                }
                case "playerpage": {
                    BuildUI(player, UiPage.PlayerPage, (arg.HasArgs(2) ? arg.Args[1] : ""));
                    break;
                }
                case "playerpagebanned": {
                    BuildUI(player, UiPage.PlayerPageBanned, (arg.HasArgs(2) ? arg.Args[1] : ""));
                    break;
                }
                default: { 
                    BuildUI(player, UiPage.Main);
                    break;
                }
            };
        }

        [ConsoleCommand("admcp_kickuser")]
        void PlayerManagerKickUserCallback(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            ulong targetId;
            
            if (!VerifyPermission(ref player, "playeracp.show") || !GetTargetFromArg(ref arg, out targetId) || !configData.EnableKick)
                return;

            BasePlayer.FindByID(targetId)?.Kick(_("Kick Reason Message Text", targetId.ToString()));
            LogInfo($"{player.displayName}: Kicked user ID {targetId}");
      //  Слив плагинов server-rust by Apolo YouGame
            BuildUI(player, UiPage.PlayerPage, targetId.ToString());
        }

        [ConsoleCommand("admcp_banuser")]
        void PlayerManagerBanUserCallback(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            ulong targetId;

            if (!VerifyPermission(ref player, "playeracp.show") || !GetTargetFromArg(ref arg, out targetId) || !configData.EnableBan)
                return;

            Player.Ban(targetId, _("Ban Reason Message Text", targetId.ToString()));
            LogInfo($"{player.displayName}: Banned user ID {targetId}");
      //  Слив плагинов server-rust by Apolo YouGame
            BuildUI(player, UiPage.PlayerPage, targetId.ToString());
        }

        [ConsoleCommand("admcp_mainpagebanbyid")]
        void PlayerManagerMainPageBanByIdCallback(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            ulong targetId;

            if (!VerifyPermission(ref player, "playeracp.show") || !mainPageBanIdInputText.ContainsKey(player.userID) ||
                !ulong.TryParse(mainPageBanIdInputText[player.userID], out targetId) || !configData.EnableBan)
                return;

            Player.Ban(targetId, _("Ban Reason Message Text", targetId.ToString()));
            LogInfo($"{player.displayName}: Banned user ID {targetId}");
      //  Слив плагинов server-rust by Apolo YouGame
            BuildUI(player, UiPage.Main);
        }

        [ConsoleCommand("admcp_unbanuser")]
        void PlayerManagerUnbanUserCallback(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            ulong targetId;

            if (!VerifyPermission(ref player, "playeracp.show") || !GetTargetFromArg(ref arg, out targetId) || !configData.EnableUnban)
                return;

            Player.Unban(targetId);
            LogInfo($"{player.displayName}: Unbanned user ID {targetId}");
      //  Слив плагинов server-rust by Apolo YouGame
            BuildUI(player, UiPage.Main);
        }

        [ConsoleCommand("admcp_killuser")]
        void PlayerManagerKillUserCallback(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            ulong targetId;

            if (!VerifyPermission(ref player, "playeracp.show") || !GetTargetFromArg(ref arg, out targetId) || !configData.EnableKill)
                return;

            (BasePlayer.FindByID(targetId) ?? BasePlayer.FindSleeping(targetId))?.Die();
            LogInfo($"{player.displayName}: Killed user ID {targetId}");
      //  Слив плагинов server-rust by Apolo YouGame
            BuildUI(player, UiPage.PlayerPage, targetId.ToString());
        }

        [ConsoleCommand("admcp_clearuserinventory")]
        void PlayerManagerClearUserInventoryCallback(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            ulong targetId;

            if (!VerifyPermission(ref player, "playeracp.show") || !GetTargetFromArg(ref arg, out targetId) || !configData.EnableClearInv)
                return;

            (BasePlayer.FindByID(targetId) ?? BasePlayer.FindSleeping(targetId))?.inventory.Strip();
            LogInfo($"{player.displayName}: Cleared the inventory of user ID {targetId}");
      //  Слив плагинов server-rust by Apolo YouGame
            BuildUI(player, UiPage.PlayerPage, targetId.ToString());
        }

        [ConsoleCommand("admcp_resetuserblueprints")]
        void PlayerManagerResetUserBlueprintsCallback(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            ulong targetId;

            if (!VerifyPermission(ref player, "playeracp.show") || !GetTargetFromArg(ref arg, out targetId) || !configData.EnableResetBP)
                return;

            (BasePlayer.FindByID(targetId) ?? BasePlayer.FindSleeping(targetId))?.blueprints.Reset();
            LogInfo($"{player.displayName}: Reset the blueprints of user ID {targetId}");
      //  Слив плагинов server-rust by Apolo YouGame
            BuildUI(player, UiPage.PlayerPage, targetId.ToString());
        }

        [ConsoleCommand("admcp_resetusermetabolism")]
        void PlayerManagerResetUserMetabolismCallback(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            ulong targetId;

            if (!VerifyPermission(ref player, "playeracp.show") || !GetTargetFromArg(ref arg, out targetId) || !configData.EnableResetMetabolism)
                return;

            (BasePlayer.FindByID(targetId) ?? BasePlayer.FindSleeping(targetId))?.metabolism.Reset();
            LogInfo($"{player.displayName}: Reset the metabolism of user ID {targetId}");
      //  Слив плагинов server-rust by Apolo YouGame
            BuildUI(player, UiPage.PlayerPage, targetId.ToString());
        }

        [ConsoleCommand("admcp_teleport2player")]
        void PlayerManagerTeleport2playerCallback(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            ulong targetId;

            if (!VerifyPermission(ref player, "playeracp.show") || !GetTargetFromArg(ref arg, out targetId))
                return;

            player.Teleport((BasePlayer.FindByID(targetId) ?? BasePlayer.FindSleeping(targetId)).transform.position);
            LogInfo($"{player.displayName}: Teleport to user ID {targetId}");
      //  Слив плагинов server-rust by Apolo YouGame
            BuildUI(player, UiPage.PlayerPage, targetId.ToString());
        }

        [ConsoleCommand("admcp_teleport2me")]
        void PlayerManagerTeleport2meCallback(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            ulong targetId;

            if (!VerifyPermission(ref player, "playeracp.show") || !GetTargetFromArg(ref arg, out targetId))
                return;

            var target = (BasePlayer.FindByID(targetId) ?? BasePlayer.FindSleeping(targetId));
            target.StartSleeping();
            target.Teleport(player.transform.position);
            LogInfo($"{player.displayName}: Teleport user ID {targetId} to self");
      //  Слив плагинов server-rust by Apolo YouGame
            BuildUI(player, UiPage.PlayerPage, targetId.ToString());
        }

        [ConsoleCommand("admcp_hurtuser")]
        void PlayerManagerHurtUserCallback(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            ulong targetId;
            float amount;

            if (!VerifyPermission(ref player, "playeracp.show") || !GetTargetAmountFromArg(ref arg, out targetId, out amount) ||
                !configData.EnableHurt)
                return;

            (BasePlayer.FindByID(targetId) ?? BasePlayer.FindSleeping(targetId))?.Hurt(amount);
            LogInfo($"{player.displayName}: Hurt user ID {targetId} for {amount} points");
      //  Слив плагинов server-rust by Apolo YouGame
            BuildUI(player, UiPage.PlayerPage, targetId.ToString());
        }

        [ConsoleCommand("admcp_healuser")]
        void PlayerManagerHealUserCallback(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            ulong targetId;
            float amount;

            if (!VerifyPermission(ref player, "playeracp.show") || !GetTargetAmountFromArg(ref arg, out targetId, out amount) ||
                !configData.EnableHeal)
                return;

            (BasePlayer.FindByID(targetId) ?? BasePlayer.FindSleeping(targetId))?.Heal(amount);
            LogInfo($"{player.displayName}: Healed user ID {targetId} for {amount} points");
      //  Слив плагинов server-rust by Apolo YouGame
            BuildUI(player, UiPage.PlayerPage, targetId.ToString());
        }
        #endregion Command Callbacks

        #region Text Update Callbacks
        [ConsoleCommand("admcp_mainpagebanidinputtext")]
        void PlayerManagerMainPageBanIdInputTextCallback(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();

            if (!VerifyPermission(ref player, "playeracp.show") || !arg.HasArgs()) {
                if (mainPageBanIdInputText.ContainsKey(player.userID))
                    mainPageBanIdInputText.Remove(player.userID);

                return;
            };

            if (mainPageBanIdInputText.ContainsKey(player.userID)) {
                mainPageBanIdInputText[player.userID] = arg.Args[0];
            } else {
                mainPageBanIdInputText.Add(player.userID, arg.Args[0]);
            };
        }
        #endregion Text Update Callbacks
    }
}
