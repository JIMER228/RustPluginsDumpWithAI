using System.Collections.Generic;
using System.Globalization;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("FriendsUI", "S1m0n", "1.0.0", ResourceId = 0)]
    class FriendsUI : RustPlugin
    {
        #region Fields
        [PluginReference]
        Plugin Friends;

        const string FriendUI = "FriendsUI";
        const string FriendBG = "FriendsUIBG";

        private int maxFriends;

        private bool isRustFriends;    

        private SortedList<string, string> playerList = new SortedList<string, string>();
        private Dictionary<ulong, string[]> temporaryLists = new Dictionary<ulong, string[]>();
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
            if (!Friends)
            {
                PrintError("Unable to find a valid Friends plugin! Unable to continue");
                Interface.Oxide.UnloadPlugin("FriendsUI");
                return;
            }

            if (Friends.ResourceId == 686)            
                isRustFriends = true;

            GetMaximumFriends();

            cmd.AddChatCommand(configData.MenuActivation.CommandToOpen, this, cmdFriendUI);                      

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
                player.Command("bind " + configData.MenuActivation.KeyToBind + " FriendsUIToggle");
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            openMenu.Remove(player.userID); 
            if (temporaryLists.ContainsKey(player.userID))
                temporaryLists.Remove(player.userID);

            CuiHelper.DestroyUi(player, FriendBG);
            CuiHelper.DestroyUi(player, FriendUI);
        }
        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (temporaryLists.ContainsKey(player.userID))
                    temporaryLists.Remove(player.userID);
                CuiHelper.DestroyUi(player, FriendBG);
                CuiHelper.DestroyUi(player, FriendUI);
            }
            openMenu.Clear();
        }
        #endregion

        #region Functions
        private void GetMaximumFriends()
        {
            if (isRustFriends)            
                maxFriends = (int)Friends.Config["MaxFriends"];
            else maxFriends = (int)Friends.Call("GetMaxFriends");
        }
        private string RemoveTag(string str)
        {
            if (str.StartsWith("[") && str.Contains("]") && str.Length > str.IndexOf("]"))            
                str = str.Substring(str.IndexOf("]") + 1).Trim();            

            if (str.StartsWith("[") && str.Contains("]") && str.Length > str.IndexOf("]"))
                RemoveTag(str);

            return str;
        }

        private string TrimToSize(string str, int size) => str.Length <= size ? str : str.Substring(0, size);
        #endregion

        #region Helpers
        bool AddFriend(string playerId, string friendId)
        {
            if (playerId == friendId) return false;
            if (isRustFriends)
                return (bool)Friends.Call("AddFriendS", playerId, friendId);
            else return (bool)Friends.Call("AddFriend", playerId, friendId);
        }
        bool RemoveFriend(string playerId, string friendId)
        {
            if (isRustFriends)
                return (bool)Friends.Call("RemoveFriendS", playerId, friendId);
            else return (bool)Friends.Call("RemoveFriend", playerId, friendId);
        }
        bool AreFriends(string playerId, string friendId)
        {
            if (isRustFriends)
                return (bool)Friends.Call("AreFriendsS", playerId, friendId);
            else return (bool)Friends.Call("AreFriends", playerId, friendId);
        }
        string[] GetFriendsList(string playerId)
        {
            if (isRustFriends)            
                return Friends.Call("GetFriendsS", playerId) as string[];            
            else return Friends.Call("GetFriends", playerId) as string[];
        }
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
        class PlayerInfo { public string playerId, playerName; }

        void LoadFriendMenu(BasePlayer player, int page = 0)
        {
            var container = UI.CreateElementContainer(FriendBG, "0 0 0 0", "0.15 0.15", "0.85 0.85", true);
            openMenu.Add(player.userID);

            CuiHelper.AddUi(player, container);
            FriendsMenu(player, page);
        }
        void FriendsMenu(BasePlayer player, int page = 0)
        {            
            if (!temporaryLists.ContainsKey(player.userID))            
                temporaryLists.Add(player.userID, GetFriendsList(player.UserIDString));            

            var friendList = temporaryLists[player.userID];
            var container = UI.CreateElementContainer(FriendUI, UI.Color(configData.UIColors.Background.HexColor, configData.UIColors.Background.Opacity), $"{configData.UISize.X_Position} {configData.UISize.Y_Position}", $"{configData.UISize.X_Position + configData.UISize.X_Dimension} {configData.UISize.Y_Position + configData.UISize.Y_Dimension}");
            UI.CreateButton(ref container, FriendUI, UI.Color(configData.UIColors.CloseButton.HexColor, configData.UIColors.CloseButton.Opacity), "X", 18, "0.92 0.931", "0.998 0.999", "FriendsUI close");

            UI.CreateLabel(ref container, FriendUI, "", msg("Администрация друзей", player.UserIDString), 20, "0.3 0.94", "0.8 1", TextAnchor.MiddleLeft);
            UI.CreatePanel(ref container, FriendUI, UI.Color(configData.UIColors.Background.HexColor, configData.UIColors.Background.Opacity), "0 0", "0 0");

            UI.CreatePanel(ref container, FriendUI, UI.Color(configData.UIColors.TitlePanel.HexColor, configData.UIColors.TitlePanel.Opacity), "0.01 0.875", "0.4 0.93");
            UI.CreatePanel(ref container, FriendUI, UI.Color(configData.UIColors.TextPanel.HexColor, configData.UIColors.TextPanel.Opacity), "0.41 0.875", "0.69 0.93");
            UI.CreateLabel(ref container, FriendUI, "", msg("Список друзей", player.UserIDString), 16, "0.02 0.875", "0.39 0.93", TextAnchor.MiddleLeft);
            UI.CreateLabel(ref container, FriendUI, "", $"({friendList.Length}/{maxFriends})", 16, "0.02 0.875", "0.39 0.93", TextAnchor.MiddleRight);
            UI.CreateButton(ref container, FriendUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Добавить друга", player.UserIDString), 16, "0.7 0.875", "0.998 0.93", "FriendsUI friendselect 0");
            UI.CreateLabel(ref container, FriendUI, "", msg("<color=#00E500><size=14>•</size></color> Взаимная дружба | <color=#ce422b><size=14>•</size></color> Не-Взаимная дружба", player.UserIDString), 10, "0.02 0.83", "0.98 0.875", TextAnchor.MiddleLeft);

            for (int i = (12 * page); i < friendList.Length; i++)
            {
                if (i >= (12 * page) + 12)
                    break;
                string friendId = friendList[i];
                bool isMutual = AreFriends(player.UserIDString, friendId);              
                
                AddFriendEntry(ref container, player.UserIDString, friendId, isMutual, i - (12 * page), page);
            }

            bool hasPages = false;
            int maxCount = friendList.Length;
            if (maxCount > 10)
            {
                hasPages = true;
                var maxpages = (maxCount - 1) / 10 + 1;
                if (page < maxpages - 1)
                    UI.CreateButton(ref container, FriendUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), ">>>", 14, "0.205 0.01", "0.38 0.05", $"FriendsUI mainmenu {page + 1}");
                if (page > 0)
                    UI.CreateButton(ref container, FriendUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), "<<<", 14, "0.02 0.01", "0.195 0.05", $"FriendsUI mainmenu {page - 1}");
            }


            CuiHelper.DestroyUi(player, FriendUI);
            CuiHelper.AddUi(player, container);
        }
        void AddFriendEntry(ref CuiElementContainer container, string playerId, string memberId, bool isMutual, int number, int page)
        {
            var targetPlayer = covalence.Players.FindPlayerById(memberId);
            float yPos = 0.78f - (0.06f * number);
            UI.CreatePanel(ref container, FriendUI, UI.Color(configData.UIColors.TextPanel.HexColor, configData.UIColors.TextPanel.Opacity), $"0.02 {yPos}", $"0.38 {yPos + 0.05f}");
            UI.CreateLabel(ref container, FriendUI, "", $"{(isMutual ? "<color=#00E500>" : "<color=#ce422b>")}•</color> {targetPlayer?.Name ?? memberId}", 14, $"0.03 {yPos}", $"0.37 {yPos + 0.05f}", TextAnchor.MiddleLeft);
            for (int i = 0; i < configData.Commands.Count; i++)
            {
                float xPos = 0.39f + (0.11f * i);
                var command = configData.Commands[i];
                UI.CreateButton(ref container, FriendUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), command.Name, 14, $"{xPos} {yPos}", $"{xPos + 0.1f} {yPos + 0.05f}", $"FriendsUI command {i} {memberId} {targetPlayer?.Name.Replace(" ", "$$%%^^") ?? memberId}");
            }
            UI.CreateButton(ref container, FriendUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("remove", playerId), 14, $"0.88 {yPos}", $"0.99 {yPos + 0.05f}", $"FriendsUI remove {page} {memberId}");
            
        }
        void FriendSelection(BasePlayer player, int page)
        {
            var container = UI.CreateElementContainer(FriendUI, UI.Color(configData.UIColors.Background.HexColor, configData.UIColors.Background.Opacity), $"{configData.UISize.X_Position} {configData.UISize.Y_Position}", $"{configData.UISize.X_Position + configData.UISize.X_Dimension} {configData.UISize.Y_Position + configData.UISize.Y_Dimension}");
            UI.CreateButton(ref container, FriendUI, UI.Color(configData.UIColors.CloseButton.HexColor, configData.UIColors.CloseButton.Opacity), "X", 18, "0.92 0.931", "0.998 0.999", "FriendsUI close");

            UI.CreateLabel(ref container, FriendUI, "", msg("Администрация друзей", player.UserIDString), 20, "0.3 0.94", "0.8 1", TextAnchor.MiddleLeft);
            UI.CreatePanel(ref container, FriendUI, UI.Color(configData.UIColors.Background.HexColor, configData.UIColors.Background.Opacity), "0 0", "0 0");

            UI.CreatePanel(ref container, FriendUI, UI.Color(configData.UIColors.TitlePanel.HexColor, configData.UIColors.TitlePanel.Opacity), "0.01 0.875", "0.75 0.93");
            UI.CreateLabel(ref container, FriendUI, "", msg("Укажите игрока, чтобы добавить его в список друзей", player.UserIDString), 16, "0.02 0.875", "0.844 0.93", TextAnchor.MiddleLeft);
			
			UI.CreatePanel(ref container, FriendUI, UI.Color(configData.UIColors.TextPanel.HexColor, configData.UIColors.TextPanel.Opacity), "0.76 0.875", "0.856 0.93");
            UI.CreateButton(ref container, FriendUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), msg("Назад", player.UserIDString), 17, "0.864 0.875", "0.998 0.93", "FriendsUI mainmenu 0");

            int count = playerList.Count();
            for (int i = (60 * page); i < count; i++)
            {
                if (i >= (60 * page) + 60)
                    break;
                var targetId = playerList.Keys[i];
                FriendButton(ref container, playerList[targetId], targetId, i - (60 * page));                
            }

            bool hasPages = false;
            int maxCount = playerList.Count;
            if (maxCount > 60)
            {
                hasPages = true;
                var maxpages = (maxCount - 1) / 60 + 1;
                if (page < maxpages - 1)
                    UI.CreateButton(ref container, FriendUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), ">>>", 14, "0.205 0.01", "0.38 0.05", $"FriendsUI friendselect {page + 1}");
                if (page > 0)
                    UI.CreateButton(ref container, FriendUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), "<<<", 14, "0.02 0.01", "0.195 0.05", $"FriendsUI friendselect {page - 1}");
            }


            CuiHelper.DestroyUi(player, FriendUI);
            CuiHelper.AddUi(player, container);
        }
        void FriendButton(ref CuiElementContainer container, string name, string playerId, int number)
        {
            float[] position = CalculateEntryPos(number);
            UI.CreateButton(ref container, FriendUI, UI.Color(configData.UIColors.ButtonColor.HexColor, configData.UIColors.ButtonColor.Opacity), name, 14, $"{position[0]} {position[1]}", $"{position[2]} {position[3]}", $"FriendsUI addfriend 0 {playerId}");
        }
        
        private float[] CalculateEntryPos(int number)
        {
            Vector2 position = new Vector2(0.01f, 0.805f);
            Vector2 dimensions = new Vector2(0.188f, 0.055f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 5)
            {
                offsetX = (0.01f + dimensions.x) * number;
            }
            if (number > 4 && number < 10)
            {
                offsetX = (0.01f + dimensions.x) * (number - 5);
                offsetY = (-0.01f - dimensions.y) * 1;
            }
            if (number > 9 && number < 15)
            {
                offsetX = (0.01f + dimensions.x) * (number - 10);
                offsetY = (-0.01f - dimensions.y) * 2;
            }
            if (number > 14 && number < 20)
            {
                offsetX = (0.01f + dimensions.x) * (number - 15);
                offsetY = (-0.01f - dimensions.y) * 3;
            }
            if (number > 19 && number < 25)
            {
                offsetX = (0.01f + dimensions.x) * (number - 20);
                offsetY = (-0.01f - dimensions.y) * 4;
            }
            if (number > 24 && number < 30)
            {
                offsetX = (0.01f + dimensions.x) * (number - 25);
                offsetY = (-0.01f - dimensions.y) * 5;
            }
            if (number > 29 && number < 35)
            {
                offsetX = (0.01f + dimensions.x) * (number - 30);
                offsetY = (-0.01f - dimensions.y) * 6;
            }
            if (number > 34 && number < 40)
            {
                offsetX = (0.01f + dimensions.x) * (number - 35);
                offsetY = (-0.01f - dimensions.y) * 7;
            }
            if (number > 39 && number < 45)
            {
                offsetX = (0.01f + dimensions.x) * (number - 40);
                offsetY = (-0.01f - dimensions.y) * 8;
            }
            if (number > 44 && number < 50)
            {
                offsetX = (0.01f + dimensions.x) * (number - 45);
                offsetY = (-0.01f - dimensions.y) * 9;
            }
            if (number > 49 && number < 55)
            {
                offsetX = (0.01f + dimensions.x) * (number - 50);
                offsetY = (-0.01f - dimensions.y) * 10;
            }
            if (number > 54 && number < 60)
            {
                offsetX = (0.01f + dimensions.x) * (number - 55);
                offsetY = (-0.01f - dimensions.y) * 11;
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }
        #endregion

        #region UI Commands
        [ConsoleCommand("FriendsUIToggle")]
        void ccmdFriendsUIToggle(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;
            if (!openMenu.Contains(player.userID))
            {
                LoadFriendMenu(player);
            }
            else
            {
                openMenu.Remove(player.userID);
                if (temporaryLists.ContainsKey(player.userID))
                    temporaryLists.Remove(player.userID);

                CuiHelper.DestroyUi(player, FriendBG);
                CuiHelper.DestroyUi(player, FriendUI);
            }
        }
        [ConsoleCommand("FriendsUI")]
        void ccmdFriendsUI(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            string command = arg.GetString(0);
            if (command == "close")
            {
                openMenu.Remove(player.userID);
                if (temporaryLists.ContainsKey(player.userID))
                    temporaryLists.Remove(player.userID);

                CuiHelper.DestroyUi(player, FriendBG);
                CuiHelper.DestroyUi(player, FriendUI);
                return;
            }
                        
            int page = arg.GetInt(1);
            string targetId = "";
            if (arg.Args.Length > 2)
                targetId = arg.GetString(2);

            switch (arg.Args[0])
            {               
                case "mainmenu":
                    FriendsMenu(player, page);
                    return;
                case "remove":
                    if (RemoveFriend(player.UserIDString, targetId))
                        temporaryLists[player.userID] = GetFriendsList(player.UserIDString);
                    FriendsMenu(player, page);
                    return;
                case "command":
                    string targetName = arg.GetString(3).Replace("$$%%^^", " ");
                    var cmd = configData.Commands[page];
                    rust.RunClientCommand(player, $"chat.say", new string[] { $"{cmd.Command} \"{cmd.Arg.Replace("{playerName}", targetName).Replace("{playerId}", targetId)}\"" });
                    return;
                case "addfriend":
                    if (AddFriend(player.UserIDString, targetId))                    
                        temporaryLists[player.userID] = GetFriendsList(player.UserIDString); 
                    FriendsMenu(player, page);
                    return;
                case "friendselect":
                    FriendSelection(player, page);
                    return;
                default:
                    break;
            }
        }
        #endregion

        #region Commands       
        void cmdFriendUI(BasePlayer player, string command, string[] args) => LoadFriendMenu(player);        
        #endregion       

        #region Config        
        private ConfigData configData;
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
        class MenuActivation
        {
            public string KeyToBind { get; set; }
            public string CommandToOpen { get; set; }
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
                    CommandToOpen = "fmenu",
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
                    X_Dimension = 0.4f,
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
            {"title", "Администрация друзей"},
            {"friendList", "Список друзей"},
            {"on", "ВКЛ" },
            {"back", "Назад" },
            {"off", "ВЫКЛ" },
            {"remove", "Удалить" },
            {"addFriend", "Добавить друга" },
            {"friendSelect", "Укажите игрока, чтобы добавить его в список друзей" },
            {"mutualInfo", "<color=#00E500><size=12>•</size></color> Взаимная дружба | <color=#ce422b><size=12>•</size></color> Не-Взаимная дружба" }
        };
        #endregion
    }
}
