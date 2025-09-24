//Requires: Clans
using System.Collections.Generic;
using System.Globalization;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("UClansUI", "S1m0n", "1.0.0", ResourceId = 0)]
    class UClansUI : RustPlugin
    {
        #region Fields
        [PluginReference]
        Clans Clans;

        const string ClanUI = "ClansUI";
        const string ClanBG = "ClansUIBG";
        private int maxMembers;
        private int maxAllies;        

        SortedList<string, string> playerList = new SortedList<string, string>();
        private List<ulong> openMenu = new List<ulong>();
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            lang.RegisterMessages(Messages, this);
        }
        void OnServerInitialized()
        {
            LoadVariables();
            if (Clans.Author != "k1lly0u")
            {
                PrintError("This UI menu can only be used with Universal Clans!\nYou can download a copy of it here : http://oxidemod.org/plugins/clans.2087/");
                Interface.Oxide.UnloadPlugin(Title);
                return;
            }
            cmd.AddChatCommand(configData.MenuActivation.CommandToOpen, this, cmdClanUI);
            maxMembers = Clans.memberLimit;
            maxAllies = Clans.allyLimit;            

            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerInit(player);
        }
        void OnPlayerInit(BasePlayer player)
        {
            var name = TrimToSize(RemoveTag(player.displayName), 15);
            if (!playerList.ContainsKey(player.UserIDString))
                playerList.Add(player.UserIDString, name);
            else playerList[player.UserIDString] = name;
            if (!string.IsNullOrEmpty(configData.MenuActivation.KeyToBind))
                player.Command("bind " + configData.MenuActivation.KeyToBind + " ClansUIToggle");
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, ClanBG);
            CuiHelper.DestroyUi(player, ClanUI);
        }
        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, ClanBG);
                CuiHelper.DestroyUi(player, ClanUI);
            }
        }
        #endregion

        #region Functions
        private string RemoveTag(string str)
        {
            if (str.StartsWith("[") && str.Contains("]") && str.Length > str.IndexOf("]"))
            {
                str = str.Substring(str.IndexOf("]") + 1).Trim();
            }

            if (str.StartsWith("[") && str.Contains("]") && str.Length > str.IndexOf("]"))
                RemoveTag(str);

            return str;
        }

        private string TrimToSize(string str, int size) => str.Length <= size ? str : str.Substring(0, size);
        #endregion

        #region UI
        class UI
        {
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool cursor = false, string parent = "Hud")
            {
                var NewElement = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        Image = {Color = color},
                        RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                        CursorEnabled = cursor
                    },
                    new CuiElement().Parent = parent,
                    panelName
                }
            };
                return NewElement;
            }
            static public void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                panel, CuiHelper.GetGuid());
            }
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = 0, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel, CuiHelper.GetGuid());

            }
            static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0 },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel, CuiHelper.GetGuid());
            }
            static public string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.TrimStart('#');
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
        #endregion

        #region UI Creation
        enum AllianceType { Current, Pending, Offer }
        class PlayerInfo { public string playerId, playerName; }

        void LoadClanMenu(BasePlayer player, Clans.Clan clan, int page = 0)
        {
            if (!openMenu.Contains(player.userID))
                openMenu.Add(player.userID);
            var container = UI.CreateElementContainer(ClanBG, "0 0 0 0", "0.15 0.15", "0.85 0.85", true);
            CuiHelper.AddUi(player, container);
            MembersMenu(player, clan, page);
        }
        void MembersMenu(BasePlayer player, Clans.Clan clan, int page = 0)
        {
            var container = UI.CreateElementContainer(ClanUI, UI.Color(configData.UIColors.Background.HexColor, configData.UIColors.Background.Opacity), $"{configData.UISize.X_Position} {configData.UISize.Y_Position}", $"{configData.UISize.X_Position + configData.UISize.X_Dimension} {configData.UISize.Y_Position + configData.UISize.Y_Dimension}");
            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.CloseButton.HexColor, configData.UIColors.CloseButton.Opacity), "X", 18, "0.965 0.945", "0.998 0.995", "ClansUI close");

            UI.CreateLabel(ref container, ClanUI, "", msg("Администрирование кланом", player.UserIDString), 18, "0.38 0.94", "0.7 1", TextAnchor.MiddleLeft);
            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.Background.HexColor, configData.UIColors.Background.Opacity), "0.0001 0.0001", "0.9999 0.94");
            UI.CreateLabel(ref container, ClanUI, "", $"{msg("Название клана: ", player.UserIDString)} {clan.clanTag}", 17, "0.02 0.65", "0.25 0.93", TextAnchor.UpperLeft);

            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.TitlePanel.HexColor, configData.UIColors.TitlePanel.Opacity), "0.26 0.875", "0.99 0.93");
            UI.CreateLabel(ref container, ClanUI, "", msg("НАСТРОЙКА ВЛАДЕЛЬЦА / СОВЕТА КЛАНА", player.UserIDString), 16, "0.28 0.875", "0.97 0.93", TextAnchor.MiddleLeft);

            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.TextPanel.HexColor, configData.UIColors.TextPanel.Opacity), "0.26 0.805", "0.51 0.86");

            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.TextPanel.HexColor, configData.UIColors.TitlePanel.Opacity), "0.26 0.735", "0.75 0.79");
            UI.CreateLabel(ref container, ClanUI, "", $"{msg("Союз между:", player.UserIDString)} {(clan.clanAlliances.ToSentence())}", 15, "0.27 0.735", "0.75 0.79", TextAnchor.MiddleLeft);
            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Управление альянсами", player.UserIDString), 15, "0.76 0.735", "0.99 0.79", "ClansUI alliances 0");

            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Пригласить игрока", player.UserIDString), 15, "0.52 0.805", "0.75 0.86", "ClansUI invitelist 0");
            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Отменить приглашения", player.UserIDString), 15, "0.76 0.805", "0.99 0.86", "ClansUI cancelinvites 0");

            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.TitlePanel.HexColor, configData.UIColors.TitlePanel.Opacity), "0.01 0.67", "0.25 0.72");
            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.TextPanel.HexColor, configData.UIColors.TextPanel.Opacity), "0.26 0.67", "0.99 0.72");
            UI.CreateLabel(ref container, ClanUI, "", msg("Участники", player.UserIDString), 16, "0.02 0.67", "0.25 0.72", TextAnchor.MiddleLeft);
            UI.CreateLabel(ref container, ClanUI, "", $"({clan.members.Count}/{maxMembers})", 16, "0.02 0.67", "0.23 0.72", TextAnchor.MiddleRight);

            for (int i = (10 * page); i < clan.members.Count; i++)
            {
                if (i >= (10 * page) + 10)
                    break;
                string memberId = clan.members.Values.ToArray()[i];
                bool isOwner = false;
                bool isPlayer = false;
                if (memberId == player.UserIDString)
                {
                    if (clan.IsOwner(memberId))
                        isOwner = true;
                    else isPlayer = true;
                }
                AddClanMember(ref container, player.UserIDString, memberId, i - (10 * page), page, clan.IsOwner(memberId) ? msg("ВЛАДЕЛЕЦ", player.UserIDString) : clan.IsModerator(memberId) ? msg("МОДЕР", player.UserIDString) : "", isOwner, isPlayer);
            }

            bool hasPages = false;
            int maxCount = clan.members.Count;
            if (maxCount > 10)
            {
                hasPages = true;
                var maxpages = (maxCount - 1) / 10 + 1;
                if (page < maxpages - 1)
                    UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), ">>>", 14, "0.16 0.01", "0.24 0.05", $"ClansUI members {page + 1}");
                if (page > 0)
                    UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), "<<<", 14, "0.02 0.01", "0.1 0.05", $"ClansUI members {page - 1}");
            }

            CuiHelper.DestroyUi(player, ClanUI);
            CuiHelper.AddUi(player, container);
        }
        void AddClanMember(ref CuiElementContainer container, string playerId, string memberId, int number, int page, string status, bool isOwner, bool isPlayer)
        {
            var targetPlayer = covalence.Players.FindPlayerById(memberId);
            float yPos = 0.6f - (0.06f * number);
            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.TextPanel.HexColor, configData.UIColors.TextPanel.Opacity), $"0.02 {yPos}", $"0.24 {yPos + 0.05f}");
            UI.CreateLabel(ref container, ClanUI, "", targetPlayer?.Name ?? memberId, 14, $"0.03 {yPos}", $"0.23 {yPos + 0.05f}", TextAnchor.MiddleLeft);
            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.TextPanel.HexColor, configData.UIColors.TextPanel.Opacity), $"0.26 {yPos}", $"0.31 {yPos + 0.05f}");
            UI.CreateLabel(ref container, ClanUI, "", status, 14, $"0.265 {yPos}", $"0.305 {yPos + 0.05f}");
            for (int i = 0; i < configData.Commands.Count; i++)
            {
                float xPos = 0.315f + (0.075f * i);
                var command = configData.Commands[i];
                UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), command.Name, 14, $"{xPos} {yPos}", $"{xPos + 0.07f} {yPos + 0.05f}", $"ClansUI command {i} {memberId} {targetPlayer?.Name.Replace(" ", "$$%%^^") ?? memberId}");
            }
            if (isOwner)
            {
                UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("ВЫЙТИ", playerId), 14, $"0.855 {yPos}", $"0.925 {yPos + 0.05f}", $"ClansUI leave {page} {memberId}");
                UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("РАСПУСТИТЬ", playerId), 14, $"0.77 {yPos}", $"0.85 {yPos + 0.05f}", $"ClansUI predisband {page} {memberId}");
            }
            else if (isPlayer)
            {
                UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("ВЫЙТИ", playerId), 14, $"0.855 {yPos}", $"0.925 {yPos + 0.05f}", $"ClansUI leave {page} {memberId}");
            }
            else
            {
                UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("ПОВЫСИТЬ", playerId), 14, $"0.745 {yPos}", $"0.83 {yPos + 0.05f}", $"ClansUI promote {page} {memberId}");
                UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("ПОНИЗИТЬ", playerId), 14, $"0.835 {yPos}", $"0.915 {yPos + 0.05f}", $"ClansUI demote {page} {memberId}");
                UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("ВЫГНАТЬ", playerId), 14, $"0.92 {yPos}", $"0.99 {yPos + 0.05f}", $"ClansUI kick {page} {memberId}");
            }
        }
        void MemberSelection(BasePlayer player, Clans.Clan clan, int page, bool isRemoving = false)
        {
            var container = UI.CreateElementContainer(ClanUI, UI.Color(configData.UIColors.Background.HexColor, configData.UIColors.Background.Opacity), $"{configData.UISize.X_Position} {configData.UISize.Y_Position}", $"{configData.UISize.X_Position + configData.UISize.X_Dimension} {configData.UISize.Y_Position + configData.UISize.Y_Dimension}");
            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.CloseButton.HexColor, configData.UIColors.CloseButton.Opacity), "X", 18, "0.965 0.945", "0.998 0.995", "ClansUI close");

            UI.CreateLabel(ref container, ClanUI, "", msg("Администрирование кланом", player.UserIDString), 18, "0.38 0.94", "0.7 1", TextAnchor.MiddleLeft);
            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.Background.HexColor, configData.UIColors.Background.Opacity), "0.0001 0.0001", "0.9999 0.94");

            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.TitlePanel.HexColor, configData.UIColors.TitlePanel.Opacity), "0.01 0.875", "0.854 0.93");
            UI.CreateLabel(ref container, ClanUI, "", isRemoving ? msg("Выберите ожидающее приглашение для отмены", player.UserIDString) : msg("Выберите игрока из списка, чтобы пригласить его в клан", player.UserIDString), 16, "0.02 0.875", "0.844 0.93", TextAnchor.MiddleLeft);

            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Назад", player.UserIDString), 15, "0.864 0.875", "0.99 0.93", "ClansUI members 0");

            int count = isRemoving ? clan.invitedPlayers.Count : playerList.Count();
            for (int i = (84 * page); i < count; i++)
            {
                if (i >= (84 * page) + 84)
                    break;
                var targetId = isRemoving ? clan.invitedPlayers.Keys.ToArray()[i] : playerList.Keys[i];
                MemberButton(ref container, playerList[targetId], targetId, i - (84 * page), isRemoving);
            }

            bool hasPages = false;
            int maxCount = isRemoving ? clan.invitedPlayers.Count : playerList.Count;
            if (maxCount > 84)
            {
                hasPages = true;
                var maxpages = (maxCount - 1) / 84 + 1;
                if (page < maxpages - 1)
                    UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), ">>>", 14, "0.16 0.01", "0.24 0.05", $"ClansUI invitelist {page + 1}");
                if (page > 0)
                    UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), "<<<", 14, "0.02 0.01", "0.1 0.05", $"ClansUI invitelist {page - 1}");
            }

            CuiHelper.DestroyUi(player, ClanUI);
            CuiHelper.AddUi(player, container);
        }
        void MemberButton(ref CuiElementContainer container, string name, string playerId, int number, bool isRemoving)
        {
            float[] position = CalculateEntryPos(number);
            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), isRemoving ? covalence.Players.FindPlayerById(name)?.Name ?? name : name, 14, $"{position[0]} {position[1]}", $"{position[2]} {position[3]}", $"ClansUI {(isRemoving ? "withdraw" : "invitemember")} 0 {(isRemoving ? name : playerId)}");
        }

        void AllianceMenu(BasePlayer player, Clans.Clan clan, AllianceType type = AllianceType.Current, int page = 0)
        {
            var container = UI.CreateElementContainer(ClanUI, UI.Color(configData.UIColors.Background.HexColor, configData.UIColors.Background.Opacity), $"{configData.UISize.X_Position} {configData.UISize.Y_Position}", $"{configData.UISize.X_Position + configData.UISize.X_Dimension} {configData.UISize.Y_Position + configData.UISize.Y_Dimension}");
            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.CloseButton.HexColor, configData.UIColors.CloseButton.Opacity), "X", 18, "0.965 0.945", "0.998 0.995", "ClansUI close");

            UI.CreateLabel(ref container, ClanUI, "", msg("Администрирование кланом", player.UserIDString), 18, "0.38 0.94", "0.7 1", TextAnchor.MiddleLeft);
            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.Background.HexColor, configData.UIColors.Background.Opacity), "0.0001 0.0001", "0.9999 0.94");
            UI.CreateLabel(ref container, ClanUI, "", $"{msg("Название клана: ", player.UserIDString)} {clan.clanTag}", 17, "0.02 0.65", "0.25 0.93", TextAnchor.UpperLeft);

            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.TitlePanel.HexColor, configData.UIColors.TitlePanel.Opacity), "0.26 0.875", "0.99 0.93");
            UI.CreateLabel(ref container, ClanUI, "", msg("СОЮЗ КЛАНА", player.UserIDString), 16, "0.28 0.875", "0.97 0.93", TextAnchor.MiddleLeft);

            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Союзники", player.UserIDString), 15, "0.26 0.805", "0.401 0.86", "ClansUI alliances 0 current");
            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Предложения", player.UserIDString), 15, "0.411 0.805", "0.552 0.86", "ClansUI alliances 0 offer");
            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Приглашения", player.UserIDString), 15, "0.562 0.805", "0.703 0.86", "ClansUI alliances 0 invites");
            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Предложить союз", player.UserIDString), 15, "0.713 0.805", "0.854 0.86", "ClansUI offermenu 0");
            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Назад", player.UserIDString), 15, "0.864 0.805", "0.99 0.86", "ClansUI members 0");

            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.TitlePanel.HexColor, configData.UIColors.TitlePanel.Opacity), "0.01 0.805", "0.25 0.86");
            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.TextPanel.HexColor, configData.UIColors.TitlePanel.Opacity), "0.26 0.805", "0.99 0.56");
            UI.CreateLabel(ref container, ClanUI, "", type == AllianceType.Current ? msg("Союзники", player.UserIDString) : type == AllianceType.Offer ? msg("Предложения:", player.UserIDString) : msg("Приглашения:", player.UserIDString), 16, "0.02 0.805", "0.25 0.86", TextAnchor.MiddleLeft);
            if (type == AllianceType.Current)
                UI.CreateLabel(ref container, ClanUI, "", $"({clan.clanAlliances.Count}/{maxAllies})", 16, "0.02 0.805", "0.23 0.86", TextAnchor.MiddleRight);

            List<string> allyList = type == AllianceType.Current ? clan.clanAlliances : type == AllianceType.Offer ? clan.invitedAllies : clan.pendingInvites;

            for (int i = (12 * page); i < allyList.Count; i++)
            {
                if (i >= (12 * page) + 12)
                    break;
                string allyTag = allyList[i];
                AddAlliance(ref container, type, player.UserIDString, allyTag, i - (12 * page), page);
            }

            bool hasPages = false;
            int maxCount = allyList.Count;
            if (maxCount > 10)
            {
                hasPages = true;
                var maxpages = (maxCount - 1) / 10 + 1;
                if (page < maxpages - 1)
                    UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), ">>>", 14, "0.16 0.01", "0.24 0.05", $"ClansUI alliances {page + 1} {type}");
                if (page > 0)
                    UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), "<<<", 14, "0.02 0.01", "0.1 0.05", $"ClansUI alliances {page - 1} {type}");
            }

            CuiHelper.DestroyUi(player, ClanUI);
            CuiHelper.AddUi(player, container);
        }
        void AddAlliance(ref CuiElementContainer container, AllianceType type, string playerId, string clanTag, int number, int page)
        {
            float yPos = 0.735f - (0.06f * number);
            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.TextPanel.HexColor, configData.UIColors.TextPanel.Opacity), $"0.02 {yPos}", $"0.24 {yPos + 0.05f}");
            UI.CreateLabel(ref container, ClanUI, "", clanTag, 14, $"0.03 {yPos}", $"0.23 {yPos + 0.05f}", TextAnchor.MiddleLeft);
            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.TextPanel.HexColor, configData.UIColors.TextPanel.Opacity), $"0.26 {yPos}", $"0.401 {yPos + 0.05f}");
            UI.CreateLabel(ref container, ClanUI, "", $"{msg("Участники", playerId)}: {Clans.FindClanByTag(clanTag).members.Count}", 14, $"0.27 {yPos}", $"0.391 {yPos + 0.05f}", TextAnchor.MiddleLeft);
            if (type == AllianceType.Current)
                UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Принять предложение", playerId), 14, $"0.864 {yPos}", $"0.99 {yPos + 0.05f}", $"ClansUI cancelalliance {page} {clanTag}");

            else if (type == AllianceType.Offer)
                UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Отказаться от предложения", playerId), 12, $"0.864 {yPos}", $"0.99 {yPos + 0.05f}", $"ClansUI canceloffer {page} {clanTag}");

            else if (type == AllianceType.Pending)
            {
                UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Принять предложение", playerId), 14, $"0.73 {yPos}", $"0.854 {yPos + 0.05f}", $"ClansUI acceptoffer {page} {clanTag}");
                UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Отклонить предложение", playerId), 14, $"0.864 {yPos}", $"0.99 {yPos + 0.05f}", $"ClansUI declineoffer {page} {clanTag}");
            }
        }
        void AllySelection(BasePlayer player, Clans.Clan clan, int page)
        {
            var container = UI.CreateElementContainer(ClanUI, UI.Color(configData.UIColors.Background.HexColor, configData.UIColors.Background.Opacity), $"{configData.UISize.X_Position} {configData.UISize.Y_Position}", $"{configData.UISize.X_Position + configData.UISize.X_Dimension} {configData.UISize.Y_Position + configData.UISize.Y_Dimension}");
            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.CloseButton.HexColor, configData.UIColors.CloseButton.Opacity), "X", 18, "0.965 0.945", "0.998 0.995", "ClansUI close");

            UI.CreateLabel(ref container, ClanUI, "", msg("Администрирование кланом", player.UserIDString), 18, "0.38 0.94", "0.7 1", TextAnchor.MiddleLeft);
            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.Background.HexColor, configData.UIColors.Background.Opacity), "0.0001 0.0001", "0.9999 0.94");

            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.TitlePanel.HexColor, configData.UIColors.TitlePanel.Opacity), "0.01 0.875", "0.854 0.93");
            UI.CreateLabel(ref container, ClanUI, "", msg("Выберите клан из списка, чтобы предложить союз", player.UserIDString), 16, "0.02 0.875", "0.844 0.93", TextAnchor.MiddleLeft);

            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Назад", player.UserIDString), 15, "0.864 0.875", "0.99 0.93", "ClansUI alliances 0");

            for (int i = (84 * page); i < Clans.clanCache.Count; i++)
            {
                if (i >= (84 * page) + 84)
                    break;
                string allyTag = Clans.clanCache.Keys.ToArray()[i];
                ClanAllyButton(ref container, allyTag, i - (84 * page));
            }

            bool hasPages = false;
            int maxCount = Clans.clanCache.Keys.Count;
            if (maxCount > 84)
            {
                hasPages = true;
                var maxpages = (maxCount - 1) / 84 + 1;
                if (page < maxpages - 1)
                    UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), ">>>", 14, "0.16 0.01", "0.24 0.05", $"ClansUI offermenu {page + 1}");
                if (page > 0)
                    UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), "<<<", 14, "0.02 0.01", "0.1 0.05", $"ClansUI offermenu {page - 1}");
            }

           
            CuiHelper.DestroyUi(player, ClanUI);
            CuiHelper.AddUi(player, container);
        }
        void ClanAllyButton(ref CuiElementContainer container, string clanTag, int number)
        {
            float[] position = CalculateEntryPos(number);
            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), clanTag, 14, $"{position[0]} {position[1]}", $"{position[2]} {position[3]}", $"ClansUI offeralliance 0 {clanTag}");
        }
        void ConfirmDisband(BasePlayer player, Clans.Clan clan)
        {
            var container = UI.CreateElementContainer(ClanUI, UI.Color(configData.UIColors.Background.HexColor, configData.UIColors.Background.Opacity), $"0.35 0.4", $"0.65 0.6");
            UI.CreateLabel(ref container, ClanUI, "", msg("РАСПУСТИТЬ", player.UserIDString), 18, "0.02 0.8", "0.5 1", TextAnchor.MiddleLeft);
            UI.CreatePanel(ref container, ClanUI, UI.Color(configData.UIColors.Background.HexColor, configData.UIColors.Background.Opacity), "0.0001 0.0001", "0.9999 0.8");
            UI.CreateLabel(ref container, ClanUI, "", msg("Нажмите «Подтвердить», чтобы продолжить", player.UserIDString), 18, "0.1 0.5", "0.9 0.85");
            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Подтвердить", player.UserIDString), 14, "0.1 0.1", "0.45 0.3", $"ClansUI disband 0");
            UI.CreateButton(ref container, ClanUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Назад", player.UserIDString), 14, "0.55 0.1", "0.9 0.3", $"ClansUI members 0");
            CuiHelper.DestroyUi(player, ClanUI);
            CuiHelper.AddUi(player, container);
        }
        private float[] CalculateEntryPos(int number)
        {
            Vector2 position = new Vector2(0.01f, 0.805f);
            Vector2 dimensions = new Vector2(0.1315f, 0.055f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 7)
            {
                offsetX = (0.01f + dimensions.x) * number;
            }
            if (number > 6 && number < 14)
            {
                offsetX = (0.01f + dimensions.x) * (number - 7);
                offsetY = (-0.01f - dimensions.y) * 1;
            }
            if (number > 13 && number < 21)
            {
                offsetX = (0.01f + dimensions.x) * (number - 14);
                offsetY = (-0.01f - dimensions.y) * 2;
            }
            if (number > 20 && number < 28)
            {
                offsetX = (0.01f + dimensions.x) * (number - 21);
                offsetY = (-0.01f - dimensions.y) * 3;
            }
            if (number > 27 && number < 35)
            {
                offsetX = (0.01f + dimensions.x) * (number - 28);
                offsetY = (-0.01f - dimensions.y) * 4;
            }
            if (number > 34 && number < 42)
            {
                offsetX = (0.01f + dimensions.x) * (number - 35);
                offsetY = (-0.01f - dimensions.y) * 5;
            }
            if (number > 41 && number < 49)
            {
                offsetX = (0.01f + dimensions.x) * (number - 42);
                offsetY = (-0.01f - dimensions.y) * 6;
            }
            if (number > 48 && number < 56)
            {
                offsetX = (0.01f + dimensions.x) * (number - 49);
                offsetY = (-0.01f - dimensions.y) * 7;
            }
            if (number > 55 && number < 63)
            {
                offsetX = (0.01f + dimensions.x) * (number - 56);
                offsetY = (-0.01f - dimensions.y) * 8;
            }
            if (number > 62 && number < 70)
            {
                offsetX = (0.01f + dimensions.x) * (number - 63);
                offsetY = (-0.01f - dimensions.y) * 9;
            }
            if (number > 69 && number < 77)
            {
                offsetX = (0.01f + dimensions.x) * (number - 70);
                offsetY = (-0.01f - dimensions.y) * 10;
            }
            if (number > 76 && number < 84)
            {
                offsetX = (0.01f + dimensions.x) * (number - 77);
                offsetY = (-0.01f - dimensions.y) * 11;
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }
        #endregion

        #region UI Commands
        [ConsoleCommand("ClansUIToggle")]
        void ccmdClansUIToggle(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;
            if (!openMenu.Contains(player.userID))
            {
                Clans.Clan clan = Clans.FindClanByID(player.UserIDString);
                if (clan == null)
                    SendReply(player, msg("Вы не являетесь участников ни одного из активных кланов", player.UserIDString));
                else LoadClanMenu(player, clan);                
            }
            else
            {
                openMenu.Remove(player.userID);
                CuiHelper.DestroyUi(player, ClanBG);
                CuiHelper.DestroyUi(player, ClanUI);
            }
        }
        [ConsoleCommand("ClansUI")]
        void ccmdClansUI(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            var iPlayer = covalence.Players.FindPlayerById(player.UserIDString);
            if (iPlayer == null)
                return;

            string command = arg.GetString(0);
            if (command == "close")
            {
                CuiHelper.DestroyUi(player, ClanBG);
                CuiHelper.DestroyUi(player, ClanUI);
                return;
            }

            Clans.Clan clan = Clans.FindClanByID(player.UserIDString);
            int page = arg.GetInt(1);
            string targetId = "";
            if (arg.Args.Length > 2)
                targetId = arg.GetString(2);

            switch (arg.Args[0])
            {               
                case "members":
                    MembersMenu(player, clan, page);
                    return;
                case "alliances":
                    AllianceType type = targetId == "invites" ? AllianceType.Pending : targetId == "offer" ? AllianceType.Offer : AllianceType.Current;
                    AllianceMenu(player, clan, type, page);
                    return;
                case "command":
                    string targetName = arg.GetString(3).Replace("$$%%^^", " ");
                    var cmd = configData.Commands[page];
                    rust.RunClientCommand(player, $"chat.say", new string[] { $"{cmd.Command} \"{cmd.Arg.Replace("{playerName}", targetName).Replace("{playerId}", targetId)}\"" });
                    return;
                case "promote":
                    Clans.PromoteMember(iPlayer, targetId);
                    MembersMenu(player, clan, page);
                    return;
                case "demote":
                    Clans.DemoteMember(iPlayer, targetId);
                    MembersMenu(player, clan, page);
                    return;
                case "kick":
                    Clans.KickMember(iPlayer, targetId);
                    MembersMenu(player, clan, page);
                    return;
                case "invitelist":
                    MemberSelection(player, clan, page);
                    return;
                case "cancelinvites":
                    MemberSelection(player, clan, page, true);
                    return;
                case "invitemember":
                    Clans.InviteMember(iPlayer, new string[] { "", targetId });
                    MemberSelection(player, clan, 0);
                    return;
                case "withdraw":
                    Clans.InviteMember(iPlayer, new string[] { "", "cancel", targetId });
                    MemberSelection(player, clan, page, true);
                    return;
                case "offermenu":
                    AllySelection(player, clan, page);
                    return;
                case "offeralliance":
                    Clans.Alliance(iPlayer, new string[] { "", "request", targetId });
                    AllianceMenu(player, clan, AllianceType.Offer, 0);
                    return;
                case "cancelalliance":
                    Clans.Alliance(iPlayer, new string[] { "", "cancel", targetId });
                    AllianceMenu(player, clan, AllianceType.Current, page);
                    return;
                case "canceloffer":
                    Clans.Alliance(iPlayer, new string[] { "", "cancel", targetId });
                    AllianceMenu(player, clan, AllianceType.Offer, page);
                    return;
                case "declineoffer":
                    Clans.Alliance(iPlayer, new string[] { "", "decline", targetId });
                    AllianceMenu(player, clan, AllianceType.Pending, page);
                    return;
                case "acceptoffer":
                    Clans.Alliance(iPlayer, new string[] { "", "accept", targetId });
                    AllianceMenu(player, clan, AllianceType.Current, page);
                    return;
                case "leave":
                    Clans.LeaveClan(iPlayer);
                    CuiHelper.DestroyUi(player, ClanBG);
                    CuiHelper.DestroyUi(player, ClanUI);
                    return;
                case "predisband":
                    ConfirmDisband(player, clan);
                    return;
                case "disband":
                    Clans.DisbandClan(iPlayer);
                    CuiHelper.DestroyUi(player, ClanBG);
                    CuiHelper.DestroyUi(player, ClanUI);
                    return;
                default:
                    break;
            }
        }
        #endregion

        #region Commands        
        void cmdClanUI(BasePlayer player, string command, string[] args)
        {            
            Clans.Clan clan = Clans.FindClanByID(player.UserIDString);
            if (clan == null)
                SendReply(player, msg("Вы не являетесь участников ни одного из активных кланов", player.UserIDString));
            else LoadClanMenu(player, clan);            
        }
        #endregion       

        #region Config        
        private ConfigData configData;
        class MenuActivation
        {
            public string KeyToBind { get; set; }
            public string CommandToOpen { get; set; }
        }
        class UIColor
        {
            public string HexColor { get; set; }
            public float Opacity { get; set; }
        }
        class UIColors
        {
            public UIColor Background { get; set; }
            public UIColor TitlePanel { get; set; }
            public UIColor TextPanel { get; set; }
            public UIColor CloseButton { get; set; }
            public UIColor ButtonColor { get; set; }
        }
        class UISize
        {
            public float X_Position { get; set; }
            public float X_Dimension { get; set; }
            public float Y_Position { get; set; }
            public float Y_Dimension { get; set; }
        }
        class CommandButton
        {
            public string Name { get; set; }
            public string Command { get; set; }
            public string Arg { get; set; }
        }
        class ConfigData
        {
            public List<CommandButton> Commands { get; set; }
            public UIColors UIColors { get; set; }
            public UISize UISize { get; set; }
            public MenuActivation MenuActivation { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                Commands = new List<CommandButton>
                {
                    new CommandButton
                    {
                        Name = "TPR",
                        Command = "/tpr",
                        Arg = "{playerName}"
                    },
                    new CommandButton
                    {
                        Name = "TRADE",
                        Command = "/trade",
                        Arg = "{playerName}"
                    }
                },
                MenuActivation = new MenuActivation
                {
                    CommandToOpen = "cmenu",
                    KeyToBind = ""
                },
                UIColors = new UIColors
                {
                    Background = new UIColor { HexColor = "#00001F", Opacity = 0.7f },
                    ButtonColor = new UIColor { HexColor = "#00006A", Opacity = 0.7f },
                    TextPanel = new UIColor { HexColor = "#00006A", Opacity = 0.7f },
                    TitlePanel = new UIColor { HexColor = "#0000B2", Opacity = 0.7f },
                    CloseButton = new UIColor { HexColor = "#ce422b", Opacity = 0.7f }
                },
                UISize = new UISize
                {
                    X_Position = 0.31f,
                    X_Dimension = 0.685f,
                    Y_Position = 0.2f,
                    Y_Dimension = 0.7f
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Messages
        string msg(string key, string playerId = null) => lang.GetMessage(key, this, playerId);
        Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"title", "Администрирование кланом"},
            {"name", "Название клана: "},
            {"description", "Описание: "},
            {"members", "Участники"},
            {"settingsTitle", "НАСТРОЙКА ВЛАДЕЛЬЦА / СОВЕТА КЛАНА"},
            {"ffToggle", "Дружественный огонь для клана"},
            {"radarToggle", "Радар клана"},
            {"allyAdmin", "Управление альянсами"},
            {"allyCount", "Союзники ({0}/{1}):"},
            {"ownerTag", "ВЛАДЕЛЕЦ"},
            {"councilTag", "СОВЕТ"},
            {"modTag", "МОДЕР"},
            {"promote", "ПОВЫСИТЬ"},
            {"demote", "ПОНИЗИТЬ"},
            {"kick", "ВЫГНАТЬ"},
            {"allyRetract", "Отказаться от \nпредложения" },
            {"allyDecline", "Отклонить предложение"},
            {"allyAccept", "Принять предложение" },
            {"allyCancel", "Принять предложение"},
            {"memberInvite", "Пригласить игрока"},
            {"cancelInvites", "Отменить приглашения" },
            {"manageAlliances", "Управление альянсами" },
            {"memberCancel", "Отменить приглашение" },
            {"cmdNoClan", "Вы не являетесь участников ни одного из активных кланов" },
            {"on", "<color=#00E500>ВКЛ</color>" },
            {"off", "<color=#ce422b>ВЫКЛ</color>" },
            {"allies", "Союз между:" },
            {"alliances", "Союзники" },
            {"allianceSettings", "СОЮЗ КЛАНА" },
            {"offers", "Предложения:" },
            {"invites", "Приглашения:" },
            {"offerAlliance", "Предложить союз" },
            {"back", "Назад" },
            {"allySelect", "Выберите клан из списка, чтобы предложить союз" },
            {"memberSelect", "Выберите игрока из списка, чтобы пригласить его в клан" },
            {"cancelSelect", "Выберите ожидающее приглашение для отмены" },
            {"disabled", "Отключить" },
            {"leave", "ВЫЙТИ" },
            {"disband", "РАСПУСТИТЬ" },
            {"confirmDisband", "Нажмите «Подтвердить», чтобы продолжить" },
            {"confirm", "Подтвердить" }
        };
        #endregion
    }
}
