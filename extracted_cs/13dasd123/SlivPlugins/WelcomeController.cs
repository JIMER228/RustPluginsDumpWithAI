// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Oxide.Game.Rust.Cui;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Newtonsoft.Json;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("WelcomeController", "Amino", "1.0.8")]
    [Description("An advanced info panel system")]
    public class WelcomeController : RustPlugin
    {
        [PluginReference] Plugin ShopController, JobController, LoadoutController, KitController, SkinController, ImageLibrary;

        #region Config
        public class Configuration
        {
            [JsonProperty(PropertyName = "Display UI on server join")]
            public bool DisplayOnJoin { get; set; } = true;
            [JsonProperty(PropertyName = "Use social link display ( Requires Welcome UI Additions )")]
            public bool UseLinks = false;
            public List<PanelSettings> UIPanels { get; set; } = new List<PanelSettings>();
            public string AddonInfo { get; set; } = "Accepted plugins (KitController, LoadoutController, ServersController, SkinController, ShopController, StatsController, WUIAttachments)";
            public List<AddonSettings> UIAddons { get; set; } = new List<AddonSettings>();
            public UISettings UISettings { get; set; } = new UISettings();
            public UIPositions UIPositions { get; set; } = new UIPositions();
            public static Configuration DefaultConfig()
            {
                return new Configuration()
                {
                    UIPanels = new List<PanelSettings>()
                    {
                        new PanelSettings()
                        {
                            Commands = { "info", "welcome" },
                            PanelPages = new List<List<string>>
                            {
                                new List<string>
                                {
                                    "<color=#5cffa8><size=35>RUSTMANIA 10X</size></color>",
                                    "<size=30>WIPE SCHEDULE: <color=#5cffa8>Monday's and Friday's</color> @ <color=#5cffa8>4PM</color> (ET)</size>",
                                    "",
                                    "<color=#5cffa8><size=25>SERVER INFO:</size></color>",
                                    "<size=20>- 10x Gather Rates",
                                    "- NoBPs",
                                    "- Mazes",
                                    "- Custom UI",
                                    "- Instant Barrels</size>",
                                    "",
                                    "<color=#5cffa8><size=25>LINKS:</size></color>",
                                    "<size=20>DISCORD: <color=#5cffa8>discord.gg/rustmania</color>",
                                    "STORE: <color=#5cffa8>store.rustmania.net</color>",
                                    "LINKING: <color=#5cffa8>rustmania.net</color></size>"
                                },
                                new List<string>
                                {
                                    "This is page 2, put whatever you want..."
                                }
                            },
                            PanelName = "INFO",
                            PagePosition = 1
                        },
                        new PanelSettings()
                        {
                            Commands = { "rules" },
                            PanelPages = new List<List<string>>
                            {
                                new List<string>
                                {
                                    "<color=#5cffa8><size=35>RULES</size></color>",
                                    "<size=20>- No racism",
                                    "- No Homophobia",
                                    "- No Cheating",
                                    "- No Exploiting",
                                    "- No Exceeding team limit of 3</size>"
                                }
                            },
                            PanelName = "RULES",
                            PagePosition = 2
                        }
                    },
                    UIAddons = new List<AddonSettings>()
                    {
                        new AddonSettings()
                        {
                            AddonName = "KitController",
                            PanelName = "KITS",
                            Commands = { "kit", "kits" },
                            PagePosition = 3
                        },
                        new AddonSettings()
                        {
                            AddonName = "WUIAttachments_Q&A_1",
                            PanelName = "Q&A",
                            Commands = { "qna", "questions" },
                            PagePosition = 3
                        }
                    }
                };
            }
        }

        public class EditUIPositions
        {
            public string ActiveUI { get; set; }
            public string ActiveUIPage { get; set; }
            public int EditPage { get; set; }
            public UIPositions UIPostions = new UIPositions();
        }

        public class UIPositions
        {
            public Dictionary<string, UIParts> UIPartsDict { get; set; } = new Dictionary<string, UIParts>();

            public UIPositions()
            {
                UIPartsDict.Add("BackgroundPanel", new UIParts { XMin = 0, YMin = 0, XMax = 1, YMax = 1, Color = "0 0 0 .6", Blur = true });
                UIPartsDict.Add("InfoPanel", new UIParts { XMin = 0, YMin = 0, XMax = 1, YMax = 1, Color = "0 0 0 0" });
                UIPartsDict.Add("MainPanel", new UIParts { XMin = .25, YMin = .08, XMax = .95, YMax = .88, Color = "0 0 0 .6" });
                UIPartsDict.Add("TitlePanel", new UIParts { XMin = .25, YMin = .90, XMax = .95, YMax = .96, Color = "0 0 0 .6", Text = "{serverName}", TextColor = "1 1 1 .7", FontSize = 30, Alignment = TextAnchor.MiddleCenter });
                UIPartsDict.Add("PlayersPanel", new UIParts { XMin = .07, YMin = .90, XMax = .2, YMax = .96, Color = "0 0 0 .6", Text = "{players} / {maxPlayers}", TextColor = "1 1 1 .7", FontSize = 30, Alignment = TextAnchor.MiddleCenter });
                UIPartsDict.Add("CloseButton", new UIParts { XMin = .94, YMin = 0, XMax = .99, YMax = .99, Color = "1 1 1 0", Text = "X" });
                UIPartsDict.Add("LogoPanel", new UIParts { XMin = .07, YMin = .64, XMax = .2, YMax = .88, Color = "1 1 1 0", Image = "https://i.ibb.co/9v9vdnG/House-Icon.png" });
                UIPartsDict.Add("ButtonPanel", new UIParts { XMin = .05, YMin = .07, XMax = .22, YMax = .63, Color = "1 1 1 0" });
                UIPartsDict.Add("ButtonLayout", new UIParts { IsSuperPanel = false, Color = "0 0 0 0", XMin = 0, XMax = 1, YMax = 1, YMin = .9, ButtonSpacing = .1 });
                UIPartsDict.Add("SocialLinksPanel", new UIParts { XMin = 0, YMin = -0.08, XMax = 1, YMax = -0.01, Color = "0 0 0 .5" });
                UIPartsDict.Add("APGTextPanel_1", new UIParts { XMin = .02, YMin = .02, XMax = .98, YMax = .88, Color = "0 0 0 .5" });
                UIPartsDict.Add("APGTextPanel_2", new UIParts { XMin = .06, YMin = .02, XMax = .94, YMax = .88, Color = "0 0 0 .5" });
                UIPartsDict.Add("APGTextPanel_3", new UIParts { XMin = .06, YMin = .19, XMax = .94, YMax = .88, Color = "0 0 0 .5" });
                UIPartsDict.Add("APGTextPanel_4", new UIParts { XMin = .02, YMin = .19, XMax = .98, YMax = .88, Color = "0 0 0 .5" });
                UIPartsDict.Add("APGButtonBack_1", new UIParts { XMin = .02, YMin = .02, XMax = .05, YMax = .88, Color = "0 0 0 .5", Text = "<", TextColor = "1 1 1 .7", FontSize = 30, Alignment = TextAnchor.MiddleCenter });
                UIPartsDict.Add("APGButtonNext_1", new UIParts { XMin = .95, YMin = .02, XMax = .98, YMax = .88, Color = "0 0 0 .5", Text = ">", TextColor = "1 1 1 .7", FontSize = 30, Alignment = TextAnchor.MiddleCenter });
                UIPartsDict.Add("APGButtonBack_2", new UIParts { XMin = .02, YMin = .19, XMax = .05, YMax = .88, Color = "0 0 0 .5", Text = "<", TextColor = "1 1 1 .7", FontSize = 30, Alignment = TextAnchor.MiddleCenter });
                UIPartsDict.Add("APGButtonNext_2", new UIParts { XMin = .95, YMin = .19, XMax = .98, YMax = .88, Color = "0 0 0 .5", Text = ">", TextColor = "1 1 1 .7", FontSize = 30, Alignment = TextAnchor.MiddleCenter });
                UIPartsDict.Add("APGTitleForPage", new UIParts { XMin = .02, YMin = .89, XMax = .98, YMax = .98, Color = "0 0 0 .5", Text = "{panelName}", FontSize = 30 });
                UIPartsDict.Add("APGLogoForPage", new UIParts { XMin = .02, YMin = .02, XMax = .98, YMax = .18, Color = "1 1 1 0", Image = "https://i.ibb.co/9v9vdnG/House-Icon.png" });
            }

            public UIPositions Clone()
            {
                UIPositions clonedPositions = new UIPositions();
                clonedPositions.UIPartsDict.Clear();

                foreach (var entry in UIPartsDict)
                {
                    clonedPositions.UIPartsDict.Add(entry.Key, entry.Value.Clone());
                }

                return clonedPositions;
            }

            public List<string> GetAllUIPartNames()
            {
                return UIPartsDict.Keys.ToList();
            }
        }

        public class UIParts
        {
            public double XMin { get; set; }
            public double XMax { get; set; }
            public double YMin { get; set; }
            public double YMax { get; set; }
            public string Color { get; set; } = "0 0 0 0";
            public bool Blur { get; set; } = false;
            public string Text { get; set; } = String.Empty;
            public string TextColor { get; set; } = "1 1 1 1";
            public int FontSize { get; set; } = 15;
            public string Image { get; set; } = null;
            public TextAnchor Alignment { get; set; } = TextAnchor.MiddleCenter;
            public bool IsSuperPanel { get; set; } = true;
            public bool Vertical = true;
            public double ButtonSpacing = .15;
            public UIParts Clone()
            {
                return (UIParts)MemberwiseClone();
            }
        }

        public class UISettings
        {
            public string BackgroundBlurAmount = ".5";
            public string BackgroundBlurColorDay = "0 0 0 .3";
            public string BackgroundBlurColorNight = "1 1 1 .15";
        }

        public class PanelSettings
        {
            public bool Enabled { get; set; } = true;
            [JsonProperty(Order = 0, PropertyName = "Panel Name")]
            public string PanelName { get; set; } = String.Empty;
            [JsonProperty(Order = 0, PropertyName = "Panel Image")]
            public string PanelImage { get; set; } = String.Empty;
            [JsonProperty(Order = 0, PropertyName = "Button Image (Leave blank for no image)")]
            public string ButtonImage { get; set; } = String.Empty;
            [JsonProperty(Order = 0, PropertyName = "Display button name (Button stays there, but the text gets removed)")]
            public bool DisplayButtonName { get; set; } = true;
            [JsonProperty(Order = 0, PropertyName = "Panel Permission")]
            public string PanelPermission { get; set; } = String.Empty;
            [JsonProperty(Order = 0, PropertyName = "Show no permission panel")]
            public bool ShowPanelPermission { get; set; } = true;
            [JsonProperty(Order = 0, PropertyName = "Panel No Permission Text")]
            public string PanelNoPermissionText { get; set; } = "You do not have permission to use this page!";
            [JsonProperty(Order = 0, PropertyName = "Text Panel Image (Covers entire text panel)")]
            public string TextPanelImage { get; set; } = String.Empty;
            [JsonProperty(Order = 0, PropertyName = "Panel Commands")]
            public List<string> Commands = new List<string>();
            [JsonProperty(Order = 0, PropertyName = "Page Position")]
            public int PagePosition { get; set; }
            [JsonProperty(Order = 0, PropertyName = "Panel Pages")]
            public List<List<string>> PanelPages = new List<List<string>>();
        }

        public class AddonSettings
        {
            public bool Enabled { get; set; } = true;
            [JsonProperty(Order = 0, PropertyName = "Plugin Addon Name")]
            public string AddonName { get; set; }
            [JsonProperty(Order = 0, PropertyName = "Panel Name")]
            public string PanelName { get; set; }
            [JsonProperty(Order = 0, PropertyName = "Panel Commands")]
            public List<string> Commands = new List<string>();
            [JsonProperty(Order = 0, PropertyName = "Panel Permission")]
            public string PanelPermission { get; set; } = String.Empty;
            [JsonProperty(Order = 0, PropertyName = "Show no permission panel")]
            public bool ShowPanelPermission { get; set; } = true;
            [JsonProperty(Order = 0, PropertyName = "Panel No Permission Text")]
            public string PanelNoPermissionText { get; set; } = "You do not have permission to use this page!";
            [JsonProperty(Order = 0, PropertyName = "Button Image (Leave blank for no image)")]
            public string ButtonImage { get; set; } = String.Empty;
            [JsonProperty(Order = 0, PropertyName = "Display button name (Button stays there, but the text gets removed)")]
            public bool DisplayButtonName { get; set; } = true;
            [JsonProperty(Order = 0, PropertyName = "Page Position")]
            public int PagePosition { get; set; }
        }

        public class ButtonListConst
        {
            public int position { get; set; }
            public AddonSettings addon { get; set; }
            public PanelSettings panel { get; set; }
        }

        private static Configuration _config;
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) LoadDefaultConfig();
                _config.UIPositions.UIPartsDict["CloseButton"].IsSuperPanel = true;
                SaveConfig();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PrintWarning("Creating new config file.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => _config = Configuration.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(_config);
        #endregion

        #region Constructors
        public Dictionary<ulong, EditUIPositions> _editPeople = new Dictionary<ulong, EditUIPositions>();
        public ServerInfoParsed _serverInfo = new ServerInfoParsed();
        private readonly Dictionary<string, string> _storedImages = new Dictionary<string, string>();
        public class ServerInfoParsed
        {
            public string Hostname { get; set; }
            public int MaxPlayers { get; set; }
            public int Players { get; set; }
            public double Time { get; set; }
        }

        public class ServerInfoObj
        {
            public string Hostname { get; set; }
            public int MaxPlayers { get; set; }
            public int Players { get; set; }
            public int Queued { get; set; }
            public int Joining { get; set; }
            public int EntityCount { get; set; }
            public string GameTime { get; set; }
            public int Uptime { get; set; }
            public string Map { get; set; }
            public double Framerate { get; set; }
            public int Memory { get; set; }
            public int MemoryUsageSystem { get; set; }
            public int Collections { get; set; }
            public int NetworkIn { get; set; }
            public int NetworkOut { get; set; }
            public bool Restarting { get; set; }
            public string SaveCreatedTime { get; set; }
            public int Version { get; set; }
            public string Protocol { get; set; }
        }
        #endregion

        #region Hooks
        void OnServerInitialized(bool initial)
        {
            GetServerInfo();
            RegisterCommandsAndPermissions();
            ImportImages();

            timer.Every(30f, GetServerInfo);
        }

        private void Unload()
        {
            if (!Interface.Oxide.IsShuttingDown)
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(player, "WCMainEditPanel");
                    CuiHelper.DestroyUi(player, "WCMainPanel");
                }

            _config = null;
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (!_config.DisplayOnJoin) return;
            var UIPanel = _config.UIPanels.First()?.Commands[0];
            if(UIPanel != null) UIOpenWelcomeMenuPanels(player, UIPanel, null);
        }


        void OnPluginLoaded(Plugin plugin)
        {
            if(plugin == null) return;

            var isAttached = _config.UIAddons.FirstOrDefault(x => x.Enabled && x.AddonName.Equals(plugin.Name, StringComparison.OrdinalIgnoreCase));
            if (isAttached == null) return;

            var newCommands = Interface.Call<List<string>>($"{plugin.Name}Commands");

            if (newCommands != null) isAttached.Commands.AddRange(newCommands.Where(x => !isAttached.Commands.Contains(x)));
        }

        #endregion

        #region Commands
        [ConsoleCommand("wc_main")]
        private void CMDWelcomeMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            switch (arg.Args[0])
            {
                case "close":
                    CuiHelper.DestroyUi(player, "WCMainEditPanel");
                    CuiHelper.DestroyUi(player, "WCMainPanel");
                    break;
                case "panelpage":
                    var panelInfo = _config.UIPanels.FirstOrDefault(x => x.Enabled && x.PanelName.Equals(String.Join(" ", arg.Args.Skip(2)), StringComparison.OrdinalIgnoreCase));
                    if (panelInfo != null) UICreateMiddleUI(player, true, panel: panelInfo, page: int.Parse(arg.Args[1]));
                    break;
                case "panel":
                    var panelName = string.Join(" ", arg.Args.Skip(3));
                    if (_editPeople.ContainsKey(player.userID)) _editPeople[player.userID].ActiveUIPage = panelName;
                    if (!bool.Parse(arg.Args[1]))
                    {
                        panelInfo = _config.UIPanels.FirstOrDefault(x => x.Enabled && x.PanelName.Equals(panelName, StringComparison.OrdinalIgnoreCase));
                        var hasPerms = string.IsNullOrEmpty(panelInfo.PanelPermission) ? true : HasPermission(player, panelInfo.PanelPermission);

                        if (panelInfo != null) UICreateMiddleUI(player, true, panel: panelInfo, hasPerms: hasPerms);
                    } else
                    {
                        var addon = _config.UIAddons.FirstOrDefault(x => x.Enabled && x.AddonName.Equals(panelName, StringComparison.OrdinalIgnoreCase));
                        var hasPerms = string.IsNullOrEmpty(addon.PanelPermission) ? true : HasPermission(player, addon.PanelPermission);

                        UICreateMiddleUI(player, false, addon, hasPerms: hasPerms);
                        if(hasPerms) Interface.CallHook("OnWCRequestedUIPanel", player, arg.Args[2], panelName);
                    }
                    break;
                default:
                    CuiHelper.DestroyUi(player, "WCMainPanel");
                    break;
            }
        }

        [ConsoleCommand("wc_edit")]
        private void CMDEditMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            if (!_editPeople.ContainsKey(player.userID))
            {
                CuiHelper.DestroyUi(player, "WCMainEditPanel"); 
                CuiHelper.DestroyUi(player, "WCMainPanel");
                return;
            }

            var editUIPositions = _editPeople[player.userID];
            UIParts uiParts = uiParts = editUIPositions.UIPostions.UIPartsDict[editUIPositions.ActiveUI];

            List<string> reloadPanels = new List<string> { "color", "image", "text", "activeui" };
            switch (arg.Args[0])
            {
                case "editpage":
                    editUIPositions.EditPage = int.Parse(arg.Args[1]);
                    break;
                case "save":
                    _config.UIPositions.UIPartsDict = editUIPositions.UIPostions.Clone().UIPartsDict;
                    SaveConfig();

                    if(_editPeople.ContainsKey(player.userID)) _editPeople.Remove(player.userID);

                    CuiHelper.DestroyUi(player, "WCMainEditPanel");
                    CuiHelper.DestroyUi(player, "WCMainPanel");
                    break;
                case "panelinfo":
                    break;
                case "close":
                    _editPeople.Remove(player.userID);
                    CuiHelper.DestroyUi(player, "WCMainEditPanel");
                    CuiHelper.DestroyUi(player, "WCMainPanel");
                    break;
                case "color":
                    uiParts.Color = string.Join(" ", arg.Args.Skip(1));
                    break;
                case "image":
                    if (arg.Args.Length >= 2 && arg.Args[1] == "img")
                    {
                        uiParts.Image = arg.Args.Length < 3 ? null : String.Join(" ", arg.Args.Skip(2));
                        if (uiParts.Image != null) RegisterNewImage($"UI{editUIPositions.ActiveUI}", uiParts.Image);
                    }
                    break;
                case "text":
                    if(arg.Args.Length >= 2)
                    {
                        switch (arg.Args[1])
                        {
                            case "text":
                                if (arg.Args.Length >= 2) uiParts.Text = arg.Args.Length < 2 ? null : String.Join(" ", arg.Args.Skip(2));
                                break;
                            case "size":
                                uiParts.FontSize = arg.Args.Length < 3 ? 15 : int.Parse(arg.Args[2]);
                                break;
                            case "color":
                                uiParts.TextColor = arg.Args.Length < 3 ? null : String.Join(" ", arg.Args.Skip(2));
                                break;
                            case "anchor":
                                switch (arg.Args[2])
                                {
                                    case "topleft":
                                        uiParts.Alignment = TextAnchor.UpperLeft;
                                        break;
                                    case "topcenter":
                                        uiParts.Alignment = TextAnchor.UpperCenter;
                                        break;
                                    case "topright":
                                        uiParts.Alignment = TextAnchor.UpperRight;
                                        break;
                                    case "middleleft":
                                        uiParts.Alignment = TextAnchor.MiddleLeft;
                                        break;
                                    case "middlecenter":
                                        uiParts.Alignment = TextAnchor.MiddleCenter;
                                        break;
                                    case "middleright":
                                        uiParts.Alignment = TextAnchor.MiddleRight;
                                        break;
                                    case "bottomleft":
                                        uiParts.Alignment = TextAnchor.LowerLeft;
                                        break;
                                    case "bottomcenter":
                                        uiParts.Alignment = TextAnchor.LowerCenter;
                                        break;
                                    case "bottomright":
                                        uiParts.Alignment = TextAnchor.LowerRight;
                                        break;
                                }
                                break;
                        }
                    }
                    break;
                case "ymax":
                    if (double.TryParse(arg.Args[1], out double yMax)) uiParts.YMax = yMax;
                    break;
                case "ymin":
                    if(double.TryParse(arg.Args[1], out double yMin)) uiParts.YMin = yMin;
                    break;
                case "xmin":
                    if(double.TryParse(arg.Args[1], out double xMin)) uiParts.XMin = xMin;
                    break;
                case "xmax":
                    if (double.TryParse(arg.Args[1], out double xMax)) uiParts.XMax = xMax;
                    break;
                case "space":
                    if (double.TryParse(arg.Args[1], out double space)) uiParts.ButtonSpacing = space;
                    break;
                case "layout":
                    uiParts.Vertical = bool.Parse(arg.Args[1]);

                    if(uiParts.Vertical)
                    {
                        var buttonPanel = editUIPositions.UIPostions.UIPartsDict["ButtonPanel"];
                        buttonPanel.XMax = .22;
                        buttonPanel.XMin = .05;
                        buttonPanel.YMax = .63;
                        buttonPanel.YMin = .07;

                        uiParts.XMin = 0;
                        uiParts.XMax = 1;
                        uiParts.YMin = .9;
                        uiParts.YMax = 1;
                    } else
                    {
                        var buttonPanel = editUIPositions.UIPostions.UIPartsDict["ButtonPanel"];
                        buttonPanel.XMax = .95;
                        buttonPanel.XMin = .05;
                        buttonPanel.YMax = .63;
                        buttonPanel.YMin = .53;

                        uiParts.XMin = 0;
                        uiParts.XMax = .125;
                        uiParts.YMin = 0;
                        uiParts.YMax = 1;
                    }
                    break;
                case "activeui":
                    editUIPositions.ActiveUI = string.Join(" ", arg.Args.Skip(1));
                    break;
                case "selectpanel":
                    editUIPositions.ActiveUI = string.Join(" ", arg.Args.Skip(1));

                    var uiNames = _config.UIPositions.GetAllUIPartNames();
                    for (int i = 0; i < _config.UIPositions.UIPartsDict.Keys.Count; i++)
                    {
                        if (uiNames[i] == editUIPositions.ActiveUI) editUIPositions.EditPage = i / 10;
                    }
                    break;
                case "smallclose":
                    CuiHelper.DestroyUi(player, "WCSmallEditPanel");
                    break;
                case "smallimgclose":
                    CuiHelper.DestroyUi(player, "WCSmallEditPanel");
                    break;
                default:
                    CuiHelper.DestroyUi(player, "WCMainEditPanel");
                    break; 
            }

            if (arg.Args[0] != "close" && arg.Args[0] != "save")
            {
                if (arg.Args[0] == "image" && arg.Args.Length > 2) timer.Once(.5f, () => UICreateEditMenu(player));
                else UICreateEditMenu(player);
            }

            if (arg.Args[0] == "text") UICreateSmallEditPanel(player);
            if (arg.Args[0] == "image") UICreateSmallEditImage(player);
        }
        #endregion

        #region UI
        bool HasPermission(BasePlayer player, string panelPerm)
        {
            if(!permission.UserHasPermission(player.UserIDString, panelPerm.ToLower().Contains("welcomecontroller") ? panelPerm : "welcomecontroller." + panelPerm)) return false;
            return true;
        }

        private void UIOpenWelcomeMenuPanels(BasePlayer player, string command, string[] args)
        {
            PanelSettings panel = _config.UIPanels.FirstOrDefault(x => x.Enabled && x.Commands.Any(y => y.Equals(command, StringComparison.OrdinalIgnoreCase)));

            bool hasPerms = true;
            if(!string.IsNullOrEmpty(panel.PanelPermission) && !HasPermission(player, panel.PanelPermission))
            {
                if(!panel.ShowPanelPermission)
                {
                    SendReply(player, panel.PanelNoPermissionText);
                    return;
                }

                hasPerms = false;
            }

            UIOpenWelcomeMenu(player, false, panel: panel, createColorPanel: true, hasPerms: hasPerms);
        }

        private void UIOpenWelcomeMenuAddons(BasePlayer player, string command, string[] args)
        {
            AddonSettings addon = _config.UIAddons.FirstOrDefault(x => x.Enabled && x.Commands.Any(y => y.Equals(command, StringComparison.OrdinalIgnoreCase)));

            if (args.Length > 0 && addon.AddonName.Equals("KitController", StringComparison.OrdinalIgnoreCase))
            {
                Interface.CallHook("ClaimKit", player, args);
                return;
            }

            bool hasPerms = true;
            if (!string.IsNullOrEmpty(addon.PanelPermission) && !HasPermission(player, addon.PanelPermission))
            {
                if (!addon.ShowPanelPermission)
                {
                    SendReply(player, addon.PanelNoPermissionText);
                    return;
                }

                hasPerms = false;
            }

            UIOpenWelcomeMenu(player, false, addon: addon, createColorPanel: false, hasPerms: hasPerms);
            if(hasPerms) Interface.CallHook("OnWCRequestedUIPanel", player, "WCSourcePanel", addon.AddonName);
        }

        private void UIOpenWelcomeMenu(BasePlayer player, bool isAddon, AddonSettings addon = null, PanelSettings panel = null, bool createColorPanel = false, bool hasPerms = true)
        {
            var container = new CuiElementContainer();

            var UIPos = _config.UIPositions.UIPartsDict;
            var isEdit = _editPeople.ContainsKey(player.userID);
            if (isEdit) UIPos = _editPeople[player.userID].UIPostions.UIPartsDict;

            List<ButtonListConst> btnList = new List<ButtonListConst>();

            foreach (var pnl in _config.UIPanels.Where(x => x.Enabled)) btnList.Add(new ButtonListConst { position = btnList.Any(x => x.position == pnl.PagePosition) ? btnList.Count + 1 : pnl.PagePosition, panel = pnl });
            foreach (var adn in _config.UIAddons.Where(x => x.Enabled)) btnList.Add(new ButtonListConst { position = btnList.Any(x => x.position == adn.PagePosition) ? btnList.Count + 1 : adn.PagePosition, addon = adn });

            var bgPanel = CreateSuperPanel(ref container, $"{UIPos["BackgroundPanel"].XMin} {UIPos["BackgroundPanel"].YMin}", $"{UIPos["BackgroundPanel"].XMax} {UIPos["BackgroundPanel"].YMax}", UIPos["BackgroundPanel"].Color, UIPos["BackgroundPanel"].Blur, UIPos["BackgroundPanel"].Image == null ? null : GetImage("BackgroundPanel"), null, UIPos["BackgroundPanel"].FontSize, UIPos["BackgroundPanel"].TextColor, UIPos["BackgroundPanel"].Alignment, "Overlay", "WCMainPanel", true);
            if (isEdit) CreateButton(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", " ", 15, "wc_edit selectpanel BackgroundPanel", bgPanel);

            CreatePanel(ref container, "0 0", "1 1", _serverInfo.Time > 08 && _serverInfo.Time < 19 ? _config.UISettings.BackgroundBlurColorDay : _config.UISettings.BackgroundBlurColorNight, "WCMainPanel", "WCColorPanel");
            var infPanel = CreateSuperPanel(ref container, $"{UIPos["InfoPanel"].XMin} {UIPos["InfoPanel"].YMin}", $"{UIPos["InfoPanel"].XMax} {UIPos["InfoPanel"].YMax}", UIPos["InfoPanel"].Color, UIPos["InfoPanel"].Blur, UIPos["InfoPanel"].Image == null ? null : GetImage("InfoPanel"), null, UIPos["InfoPanel"].FontSize, UIPos["InfoPanel"].TextColor, UIPos["InfoPanel"].Alignment, "WCColorPanel", "WCBlockPanel");
            if (isEdit) CreateButton(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", " ", 15, "wc_edit selectpanel InfoPanel", infPanel);

            var titlePanel = CreateSuperPanel(ref container, $"{UIPos["TitlePanel"].XMin} {UIPos["TitlePanel"].YMin}", $"{UIPos["TitlePanel"].XMax} {UIPos["TitlePanel"].YMax}", UIPos["TitlePanel"].Color, UIPos["TitlePanel"].Blur, UIPos["TitlePanel"].Image == null ? null : GetImage("TitlePanel"), UIPos["TitlePanel"].Text?.Replace("{serverName}", $"{_serverInfo.Hostname}"), UIPos["TitlePanel"].FontSize, UIPos["TitlePanel"].TextColor, UIPos["TitlePanel"].Alignment, "WCBlockPanel", "WCMainPanelHostname");
            CreateButton(ref container, $"{UIPos["CloseButton"].XMin} {UIPos["CloseButton"].YMin}", $"{UIPos["CloseButton"].XMax} {UIPos["CloseButton"].YMax}", UIPos["CloseButton"].Color, "1 1 1 .7", UIPos["CloseButton"].Text, 30, "wc_main close", "WCMainPanelHostname", TextAnchor.MiddleCenter);
            if (isEdit) CreateButton(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", " ", 15, "wc_edit selectpanel TitlePanel", titlePanel);

            var plyrPanel = CreateSuperPanel(ref container, $"{UIPos["PlayersPanel"].XMin} {UIPos["PlayersPanel"].YMin}", $"{UIPos["PlayersPanel"].XMax} {UIPos["PlayersPanel"].YMax}", UIPos["PlayersPanel"].Color, UIPos["PlayersPanel"].Blur, UIPos["PlayersPanel"].Image == null ? null : GetImage("PlayersPanel"), UIPos["PlayersPanel"].Text?.Replace("{players}", $"{_serverInfo.Players}").Replace("{maxPlayers}", $"{_serverInfo.MaxPlayers}"), UIPos["PlayersPanel"].FontSize, UIPos["PlayersPanel"].TextColor, UIPos["PlayersPanel"].Alignment, "WCBlockPanel");
            if (isEdit) CreateButton(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", " ", 15, "wc_edit selectpanel PlayersPanel", plyrPanel);

            var logoPanel = CreateSuperPanel(ref container, $"{UIPos["LogoPanel"].XMin} {UIPos["LogoPanel"].YMin}", $"{UIPos["LogoPanel"].XMax} {UIPos["LogoPanel"].YMax}", UIPos["LogoPanel"].Color, UIPos["LogoPanel"].Blur, UIPos["LogoPanel"].Image == null ? null : GetImage("LogoPanel"), UIPos["LogoPanel"].Text, UIPos["LogoPanel"].FontSize, UIPos["LogoPanel"].TextColor, UIPos["LogoPanel"].Alignment, "WCBlockPanel");
            if (isEdit) CreateButton(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", " ", 15, "wc_edit selectpanel LogoPanel", logoPanel);

            var btnPanel = CreateSuperPanel(ref container, $"{UIPos["ButtonPanel"].XMin} {UIPos["ButtonPanel"].YMin}", $"{UIPos["ButtonPanel"].XMax} {UIPos["ButtonPanel"].YMax}", UIPos["ButtonPanel"].Color, UIPos["ButtonPanel"].Blur, UIPos["ButtonPanel"].Image == null ? null : GetImage("ButtonPanel"), UIPos["ButtonPanel"].Text, UIPos["ButtonPanel"].FontSize, UIPos["ButtonPanel"].TextColor, UIPos["ButtonPanel"].Alignment, "WCBlockPanel", "WCButtonPanel");
            if (isEdit) CreateButton(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", " ", 15, "wc_edit selectpanel ButtonPanel", btnPanel);

            int skippedButtons = 0;
            foreach (ButtonListConst pnlInfo in btnList)
            {
                string pnlName = null;
                bool displayName = true;
                var buttonLayout = UIPos["ButtonLayout"];
                var buttonSpace = (pnlInfo.position - 1 - skippedButtons) * buttonLayout.ButtonSpacing;

                var xMin = !buttonLayout.Vertical ? buttonLayout.XMin + buttonSpace : buttonLayout.XMin;
                var xMax = !buttonLayout.Vertical ? buttonLayout.XMax + buttonSpace : buttonLayout.XMax;
                var yMin = buttonLayout.Vertical ? buttonLayout.YMin - buttonSpace : buttonLayout.YMin;
                var yMax = buttonLayout.Vertical ? buttonLayout.YMax - buttonSpace : buttonLayout.YMax;

                if (pnlInfo.panel != null)
                {
                    pnlName = pnlInfo.panel.PanelName;
                    if (!pnlInfo.panel.ShowPanelPermission && !string.IsNullOrEmpty(pnlInfo.panel.PanelPermission) && !HasPermission(player, pnlInfo.panel.PanelPermission))
                    {
                        skippedButtons++;
                        continue;
                    }

                    if (!string.IsNullOrEmpty(pnlInfo.panel.ButtonImage)) CreateImagePanel(ref container, $"{xMin} {yMin}", $"{xMax} {yMax}", pnlInfo.panel.ButtonImage, "WCButtonPanel");
                    if (!pnlInfo.panel.DisplayButtonName) displayName = false;
                }

                if (pnlInfo.addon != null)
                {
                    pnlName = pnlInfo.addon.PanelName;
                    if (!pnlInfo.addon.ShowPanelPermission && !string.IsNullOrEmpty(pnlInfo.addon.PanelPermission) && !HasPermission(player, pnlInfo.addon.PanelPermission))
                    {
                        skippedButtons++;
                        continue;
                    }

                    if(!string.IsNullOrEmpty(pnlInfo.addon.ButtonImage)) CreateImagePanel(ref container, $"{xMin} {yMin}", $"{xMax} {yMax}", pnlInfo.addon.ButtonImage, "WCButtonPanel");
                    if (!pnlInfo.addon.DisplayButtonName) displayName = false;
                }

                CreateButton(ref container, $"{xMin} {yMin}", $"{xMax} {yMax}", buttonLayout.Color, "1 1 1 1", pnlName, 25, $"wc_main panel {pnlInfo.addon != null} WCSourcePanel {(pnlInfo.addon != null ? pnlInfo.addon.AddonName : pnlInfo.panel.PanelName)}", "WCButtonPanel");
            }

            CuiHelper.DestroyUi(player, "WCMainPanel");
            CuiHelper.AddUi(player, container);

            UICreateMiddleUI(player, createColorPanel, addon, panel, hasPerms: hasPerms);
        }

        bool UICreateMiddleUI(BasePlayer player, bool createColorPanel = false, AddonSettings addon = null, PanelSettings panel = null, int page = 0, bool hasPerms = true)
        {
            var container = new CuiElementContainer();

            var UIPos = _config.UIPositions.UIPartsDict;
            var isEdit = _editPeople.ContainsKey(player.userID);
            if (isEdit) UIPos = _editPeople[player.userID].UIPostions.UIPartsDict;

            var mnPanel = CreateSuperPanel(ref container, $"{UIPos["MainPanel"].XMin} {UIPos["MainPanel"].YMin}", $"{UIPos["MainPanel"].XMax} {UIPos["MainPanel"].YMax}", "0 0 0 0", false, parent: "WCBlockPanel", panelName: "WCSourcePanel");
            if (isEdit) CreateButton(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", " ", 15, "wc_edit selectpanel MainPanel", mnPanel);

            if (!hasPerms)
            {
                var spo = CreateSuperPanel(ref container, "0 0", "1 1", UIPos["MainPanel"].Color, UIPos["MainPanel"].Blur, UIPos["MainPanel"].Image == null ? null : GetImage("MainPanel"), UIPos["MainPanel"].Text, UIPos["MainPanel"].FontSize, UIPos["MainPanel"].TextColor, UIPos["MainPanel"].Alignment, "WCSourcePanel", "WCSourcePanelOverlay");
                CreateLabel(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", panel != null ? panel.PanelNoPermissionText : addon.PanelNoPermissionText, 20, TextAnchor.MiddleCenter, spo);
            } else
            {
                if (createColorPanel)
                {
                    string buttonLast = null;
                    string buttonNext = null;
                    var textPanel = "APGTextPanel_1";
                    if (panel.PanelPages.Count > 1 && !string.IsNullOrEmpty(panel.PanelImage))
                    {
                        textPanel = "APGTextPanel_3";
                        buttonLast = "APGButtonBack_2";
                        buttonNext = "APGButtonNext_2";
                    }
                    else if (panel.PanelPages.Count > 1 && string.IsNullOrEmpty(panel.PanelImage))
                    {
                        textPanel = "APGTextPanel_2";
                        buttonLast = "APGButtonBack_1";
                        buttonNext = "APGButtonNext_1";
                    }
                    else if (panel.PanelPages.Count == 1 && !string.IsNullOrEmpty(panel.PanelImage)) textPanel = "APGTextPanel_4";

                    var maxPage = panel.PanelPages.Count - 1;
                    if (page > maxPage) page = 0;
                    if (page < 0) page = maxPage;

                    var firstPage = panel.PanelPages.Count > 0 ? String.Join("\n", panel.PanelPages[page]) : "NoText";
                    var spo = CreateSuperPanel(ref container, "0 0", "1 1", UIPos["MainPanel"].Color, UIPos["MainPanel"].Blur, UIPos["MainPanel"].Image == null ? null : GetImage("MainPanel"), UIPos["MainPanel"].Text, UIPos["MainPanel"].FontSize, UIPos["MainPanel"].TextColor, UIPos["MainPanel"].Alignment, "WCSourcePanel", "WCSourcePanelOverlay");
                    if (isEdit) CreateButton(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", " ", 15, "wc_edit selectpanel MainPanel", spo);

                    var txtpnl = CreateSuperPanel(ref container, $"{UIPos[textPanel].XMin} {UIPos[textPanel].YMin}", $"{UIPos[textPanel].XMax} {UIPos[textPanel].YMax}", UIPos[textPanel].Color, UIPos[textPanel].Blur, String.IsNullOrEmpty(panel.TextPanelImage) ? null : GetImage($"{panel.PanelName}-text"), UIPos[textPanel].Text?.Replace("{panelName}", panel.PanelName), UIPos[textPanel].FontSize, UIPos[textPanel].TextColor, UIPos[textPanel].Alignment, spo);

                    var pageTitle = CreateSuperPanel(ref container, $"{UIPos["APGTitleForPage"].XMin} {UIPos["APGTitleForPage"].YMin}", $"{UIPos["APGTitleForPage"].XMax} {UIPos["APGTitleForPage"].YMax}", UIPos["APGTitleForPage"].Color, UIPos["APGTitleForPage"].Blur, UIPos["APGTitleForPage"].Image == null ? null : GetImage("APGTitleForPage"), UIPos["APGTitleForPage"].Text?.Replace("{panelName}", panel.PanelName), UIPos["APGTitleForPage"].FontSize, UIPos["APGTitleForPage"].TextColor, UIPos["APGTitleForPage"].Alignment, spo);
                    if (isEdit) CreateButton(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", " ", 15, $"wc_edit selectpanel APGTitleForPage", pageTitle);

                    if (buttonLast != null)
                    {
                        var btnL = CreateSuperPanel(ref container, $"{UIPos[buttonLast].XMin} {UIPos[buttonLast].YMin}", $"{UIPos[buttonLast].XMax} {UIPos[buttonLast].YMax}", UIPos[buttonLast].Color, UIPos[buttonLast].Blur, UIPos[buttonLast].Image == null ? null : GetImage(buttonLast), UIPos[buttonLast].Text, UIPos[buttonLast].FontSize, UIPos[buttonLast].TextColor, UIPos[buttonLast].Alignment, spo, buttonCommand: $"wc_main panelpage {page - 1} {panel.PanelName}");
                        if (isEdit) CreateButton(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", " ", 15, $"wc_edit selectpanel {buttonLast}", btnL);
                    }
                    if (buttonNext != null)
                    {
                        var btnN = CreateSuperPanel(ref container, $"{UIPos[buttonNext].XMin} {UIPos[buttonNext].YMin}", $"{UIPos[buttonNext].XMax} {UIPos[buttonNext].YMax}", UIPos[buttonNext].Color, UIPos[buttonNext].Blur, UIPos[buttonNext].Image == null ? null : GetImage(buttonNext), UIPos[buttonNext].Text, UIPos[buttonNext].FontSize, UIPos[buttonNext].TextColor, UIPos[buttonNext].Alignment, spo, buttonCommand: $"wc_main panelpage {page + 1} {panel.PanelName}");
                        if (isEdit) CreateButton(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", " ", 15, $"wc_edit selectpanel {buttonNext}", btnN);
                    }
                    if (!string.IsNullOrEmpty(panel.PanelImage))
                    {
                        var logo = CreateSuperPanel(ref container, $"{UIPos["APGLogoForPage"].XMin} {UIPos["APGLogoForPage"].YMin}", $"{UIPos["APGLogoForPage"].XMax} {UIPos["APGLogoForPage"].YMax}", UIPos["APGLogoForPage"].Color, UIPos["APGLogoForPage"].Blur, string.IsNullOrEmpty(panel.PanelImage) ? null : GetImage($"{panel.PanelName}"), UIPos["APGLogoForPage"].Text, UIPos["APGLogoForPage"].FontSize, UIPos["APGLogoForPage"].TextColor, UIPos["APGLogoForPage"].Alignment, spo);
                        if (isEdit) CreateButton(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", " ", 15, $"wc_edit selectpanel APGLogoForPage", logo);
                    }

                    CreateLabel(ref container, ".015 .02", ".985 .98", "0 0 0 0", "1 1 1 1", firstPage, 15, TextAnchor.UpperLeft, txtpnl);
                    if (isEdit) CreateButton(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", " ", 15, $"wc_edit selectpanel {textPanel}", txtpnl);

                    if (_config.UseLinks)
                    {
                        var scPanel = CreateSuperPanel(ref container, $"{UIPos["SocialLinksPanel"].XMin} {UIPos["SocialLinksPanel"].YMin}", $"{UIPos["SocialLinksPanel"].XMax} {UIPos["SocialLinksPanel"].YMax}", UIPos["SocialLinksPanel"].Color, UIPos["SocialLinksPanel"].Blur, UIPos["SocialLinksPanel"].Image == null ? null : GetImage("SocialLinksPanel"), UIPos["SocialLinksPanel"].Text?.Replace("{panelName}", panel.PanelName), UIPos["SocialLinksPanel"].FontSize, UIPos["SocialLinksPanel"].TextColor, UIPos["SocialLinksPanel"].Alignment, spo, "WCSocialsPanel");
                        if (isEdit) CreateButton(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", " ", 15, $"wc_edit selectpanel SocialLinksPanel", scPanel);
                    }
                }

            }

            CuiHelper.DestroyUi(player, "WCSourcePanel");
            CuiHelper.AddUi(player, container);

            if(createColorPanel && _config.UseLinks) Interface.CallHook("OnWCRequestedUIPanel", player, "WCSourcePanel", "WUIAttachments_SocialLinks");

            return hasPerms;
        }

        private void UIOpenEditMenu(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "welcomecontroller.admin"))
            {
                SendReply(player, "You do not have the permission 'welcomecontroller.admin' to use this command!");
                return;
            }

            _editPeople[player.userID] = new EditUIPositions { ActiveUI = "MainPanel", UIPostions = _config.UIPositions.Clone() };
            UICreateEditMenu(player);
        }

        private void UICreateEditMenu(BasePlayer player)
        {
            var editUIPositions = _editPeople[player.userID];

            PanelSettings panel = null;
            AddonSettings isAddon = null;
            if (!string.IsNullOrEmpty(editUIPositions.ActiveUIPage))
            {
                panel = _config.UIPanels.FirstOrDefault(x => x.Enabled && x.PanelName.Equals(editUIPositions.ActiveUIPage, StringComparison.OrdinalIgnoreCase));
                if(panel == null) isAddon = _config.UIAddons.FirstOrDefault(x => x.Enabled && x.AddonName.Equals(editUIPositions.ActiveUIPage, StringComparison.OrdinalIgnoreCase));
            }
            
            if (panel == null && isAddon == null)
            {
                panel = _config.UIPanels.First();
                editUIPositions.ActiveUIPage = panel.PanelName;
            }

            UIOpenWelcomeMenu(player, false, addon: isAddon, panel: panel, createColorPanel: isAddon == null);
            if (isAddon != null) Interface.CallHook("OnWCRequestedUIPanel", player, "WCSourcePanel", editUIPositions.ActiveUIPage);

            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = ".007 .015",
                    AnchorMax = ".993 .15"
                },
                Image = { Color = $".17 .17 .17 .8" },
                CursorEnabled = true
            }, "Overlay", "WCMainEditPanel");

            List<string> uiPartNames = _config.UIPositions.Clone().GetAllUIPartNames();

            CreateButton(ref container, "0 1.03", ".15 1.3", "0.41 1 0.26 .5", "0.41 1 0.26 .7", "SAVE MENU", 15, "wc_edit save", "WCMainEditPanel");
            if (editUIPositions.UIPostions.UIPartsDict.TryGetValue(editUIPositions.ActiveUI, out UIParts uiPart))
            {
                if (editUIPositions.ActiveUI == "ButtonLayout")
                {
                    var layoutPanel = CreatePanel(ref container, ".155 1.03", ".40 1.32", ".17 .17 .17 1", "WCMainEditPanel");
                    CreateButton(ref container, ".01 .07", ".2 .87", (uiPart.Vertical ? "0.11 1 0.04 .6" : "1 1 1 .15"), "1 1 1 1", "VERTICAL", 10, "wc_edit layout true", layoutPanel);
                    CreateButton(ref container, ".215 .07", ".4 .87", (!uiPart.Vertical ? "0.11 1 0.04 .6" : "1 1 1 .15"), "1 1 1 1", "HORIZONTAL", 10, "wc_edit layout false", layoutPanel);
                    CreateLabel(ref container, ".415 .07", ".99 .9", "1 1 1 .15", "1 1 1 1", "  BUTTON SPACE", 10, TextAnchor.MiddleLeft, layoutPanel);
                    CreateButton(ref container, ".65 .2", ".81 .77", "1 1 1 .15", "1 1 1 1", "+", 10, $"wc_edit space {uiPart.ButtonSpacing + .01}", layoutPanel);
                    CreateButton(ref container, ".82 .2", ".98 .77", "1 1 1 .15", "1 1 1 1", "-", 10, $"wc_edit space {uiPart.ButtonSpacing - .01}", layoutPanel);
                }

                CreateButton(ref container, ".97 .65", "1 1", "1 1 1 0", "1 1 1 1", "X", 25, "wc_edit close", "WCMainEditPanel");

                CreateInput(ref container, ".005 .75", ".16 .95", "wc_edit color", "1 1 1 .1", "1 1 1 1", uiPart.Color, 13, TextAnchor.MiddleCenter, "WCMainEditPanel");

                CreateLabel(ref container, ".005 .55", ".04 .7", "1 1 1 .1", "1 1 1 1", "TOP", 10, TextAnchor.MiddleCenter, "WCMainEditPanel");
                CreateImageButton(ref container, ".005 .05", ".02 .24", "1 1 1 .1", $"wc_edit ymax {uiPart.YMax + .01}", GetImage("UpArrow"), "WCMainEditPanel");
                CreateImageButton(ref container, ".025 .05", ".04 .24", "1 1 1 .1", $"wc_edit ymax {uiPart.YMax - .01}", GetImage("DownArrow"), "WCMainEditPanel");
                CreateInput(ref container, ".005 .3", ".04 .5", "wc_edit ymax", "1 1 1 .1", "1 1 1 1", $"{uiPart.YMax}", 13, TextAnchor.MiddleCenter, "WCMainEditPanel");

                CreateLabel(ref container, ".045 .55", ".08 .7", "1 1 1 .1", "1 1 1 1", "BTM", 10, TextAnchor.MiddleCenter, "WCMainEditPanel");
                CreateImageButton(ref container, ".045 .05", ".06 .24", "1 1 1 .1", $"wc_edit ymin {uiPart.YMin + .01}", GetImage("UpArrow"), "WCMainEditPanel");
                CreateImageButton(ref container, ".065 .05", ".08 .24", "1 1 1 .1", $"wc_edit ymin {uiPart.YMin - .01}", GetImage("DownArrow"), "WCMainEditPanel");
                CreateInput(ref container, ".045 .3", ".08 .5", "wc_edit ymin", "1 1 1 .1", "1 1 1 1", $"{uiPart.YMin}", 13, TextAnchor.MiddleCenter, "WCMainEditPanel");

                CreateLabel(ref container, ".085 .55", ".12 .7", "1 1 1 .1", "1 1 1 1", "LEFT", 10, TextAnchor.MiddleCenter, "WCMainEditPanel");
                CreateImageButton(ref container, ".085 .05", ".1 .24", "1 1 1 .1", $"wc_edit xmin {uiPart.XMin - .01}", GetImage("LeftArrow"), "WCMainEditPanel");
                CreateImageButton(ref container, ".105 .05", ".12 .24", "1 1 1 .1", $"wc_edit xmin {uiPart.XMin + .01}", GetImage("RightArrow"), "WCMainEditPanel");
                CreateInput(ref container, ".085 .3", ".12 .5", "wc_edit xmin", "1 1 1 .1", "1 1 1 1", $"{uiPart.XMin}", 13, TextAnchor.MiddleCenter, "WCMainEditPanel");

                CreateLabel(ref container, ".125 .55", ".16 .7", "1 1 1 .1", "1 1 1 1", "RIGHT", 10, TextAnchor.MiddleCenter, "WCMainEditPanel");
                CreateImageButton(ref container, ".125 .05", ".14 .24", "1 1 1 .1", $"wc_edit xmax {uiPart.XMax - .01}", GetImage("LeftArrow"), "WCMainEditPanel");
                CreateImageButton(ref container, ".145 .05", ".16 .24", "1 1 1 .1", $"wc_edit xmax {uiPart.XMax + .01}", GetImage("RightArrow"), "WCMainEditPanel");
                CreateInput(ref container, ".125 .3", ".16 .5", "wc_edit xmax", "1 1 1 .1", "1 1 1 1", $"{uiPart.XMax}", 13, TextAnchor.MiddleCenter, "WCMainEditPanel");

                if (uiPart.IsSuperPanel)
                {
                    CreateButton(ref container, ".165 .525", ".235 .95", "1 1 1 .1", "1 1 1 1", "TEXT", 15, "wc_edit text", "WCMainEditPanel");
                    CreateButton(ref container, ".165 .05", ".235 .475", "1 1 1 .1", "1 1 1 1", "IMAGE", 15, "wc_edit image", "WCMainEditPanel");
                }
            }

            var maxPage = (_config.UIPositions.UIPartsDict.Count - 1) / 10;
            if (editUIPositions.EditPage > maxPage) editUIPositions.EditPage = 0;
            if (editUIPositions.EditPage < 0) editUIPositions.EditPage = maxPage;

            int i = 0;
            int row = 0;
            foreach (var uiInfo in uiPartNames.Skip(10 * editUIPositions.EditPage).Take(10))
            {
                if (i == 5)
                {
                    row++;
                    i = 0;
                }
                CreateButton(ref container, $"{.24 + (i * .14)} {(row == 1 ? .05 : .525)}", $"{.375 + (i * .14)} {(row == 1 ? .475 : .95)}", editUIPositions.ActiveUI.Equals(uiInfo) ? ".01 .6 .99 .5" : "1 1 1 .1", "1 1 1 1", uiInfo, 20, $"wc_edit activeui {uiInfo}", "WCMainEditPanel");
                i++;
            }

            CreateButton(ref container, ".94 .525", ".965 .95", "1 1 1 .2", "1 1 1 1", ">", 15, $"wc_edit editpage {editUIPositions.EditPage + 1}", "WCMainEditPanel");
            CreateButton(ref container, ".94 .05", ".965 .475", "1 1 1 .2", "1 1 1 1", "<", 15, $"wc_edit editpage {editUIPositions.EditPage - 1}", "WCMainEditPanel");

            CuiHelper.DestroyUi(player, "WCMainEditPanel");
            CuiHelper.AddUi(player, container);
        }

        private void UICreateSmallEditPanel(BasePlayer player)
        {
            var container = new CuiElementContainer();

            var editUIPositions = _editPeople[player.userID];
            if (editUIPositions.UIPostions.UIPartsDict.TryGetValue(editUIPositions.ActiveUI, out UIParts uiPart))
            {
                var smallPanel = CreatePanel(ref container, ".05 1.02", ".35 3", ".17 .17 .17 1", "WCMainEditPanel", "WCSmallEditPanel", true);
                var txtLabel = CreateLabel(ref container, ".025 .825", ".975 .95", "1 1 1 .1", "1 1 1 1", "TEXT", 10, TextAnchor.MiddleCenter, smallPanel);
                CreateButton(ref container, ".9 0", "1 1", "1 1 1 0", "1 1 1 1", "X", 15, "wc_edit smallclose", txtLabel);
                CreateInput(ref container, ".025 .425", ".975 .8", "wc_edit text text", "1 1 1 .1", "1 1 1 1", uiPart.Text == null ? " " : uiPart.Text, 12, TextAnchor.MiddleCenter, smallPanel);

                CreateLabel(ref container, ".025 .325", ".32 .4", "1 1 1 .1", "1 1 1 1", "FONT SIZE", 10, TextAnchor.MiddleCenter, smallPanel);
                CreateInput(ref container, ".025 .05", ".32 .3", "wc_edit text size", "1 1 1 .1", "1 1 1 1", $"{uiPart.FontSize}", 15, TextAnchor.MiddleCenter, smallPanel);

                CreateLabel(ref container, ".337 .325", ".668 .4", "1 1 1 .1", "1 1 1 1", "FONT COLOR", 10, TextAnchor.MiddleCenter, smallPanel);
                CreateInput(ref container, ".337 .05", ".667 .3", "wc_edit text color", "1 1 1 .1", "1 1 1 1", uiPart.TextColor == null ? " " : uiPart.TextColor, 15, TextAnchor.MiddleCenter, smallPanel);

                CreateLabel(ref container, ".68 .325", ".975 .4", "1 1 1 .1", "1 1 1 1", "TEXT ANCHOR", 10, TextAnchor.MiddleCenter, smallPanel);

                var c1 = "1 1 1 .1";
                var c2 = ".01 .6 .99 .5";

                CreateButton(ref container, ".68 .2235", ".772 .3", uiPart.Alignment == TextAnchor.UpperLeft ? c2 : c1, "1 1 1 1", "o", 10, "wc_edit text anchor topleft", smallPanel);
                CreateButton(ref container, ".783 .2235", ".876 .3", uiPart.Alignment == TextAnchor.UpperCenter ? c2 : c1, "1 1 1 1", "o", 10, "wc_edit text anchor topcenter", smallPanel);
                CreateButton(ref container, ".886 .2235", ".971 .3", uiPart.Alignment == TextAnchor.UpperRight ? c2 : c1, "1 1 1 1", "o", 10, "wc_edit text anchor topright", smallPanel);

                CreateButton(ref container, ".68 .137", ".772 .2075", uiPart.Alignment == TextAnchor.MiddleLeft ? c2 : c1, "1 1 1 1", "o", 10, "wc_edit text anchor middleleft", smallPanel);
                CreateButton(ref container, ".783 .137", ".876 .2075", uiPart.Alignment == TextAnchor.MiddleCenter ? c2 : c1, "1 1 1 1", "o", 10, "wc_edit text anchor middlecenter", smallPanel);
                CreateButton(ref container, ".886 .137", ".971 .2075", uiPart.Alignment == TextAnchor.MiddleRight ? c2 : c1, "1 1 1 1", "o", 10, "wc_edit text anchor middleright", smallPanel);

                CreateButton(ref container, ".68 .05", ".772 .12", uiPart.Alignment == TextAnchor.LowerLeft ? c2 : c1, "1 1 1 1", "o", 10, "wc_edit text anchor bottomleft", smallPanel);
                CreateButton(ref container, ".783 .05", ".876 .12", uiPart.Alignment == TextAnchor.LowerCenter ? c2 : c1, "1 1 1 1", "o", 10, "wc_edit text anchor bottomcenter", smallPanel);
                CreateButton(ref container, ".886 .05", ".971 .12", uiPart.Alignment == TextAnchor.LowerRight ? c2 : c1, "1 1 1 1", "o", 10, "wc_edit text anchor bottomright", smallPanel);
            }

            CuiHelper.DestroyUi(player, "WCSmallEditPanel");
            CuiHelper.AddUi(player, container);
        }

        private void UICreateSmallEditImage(BasePlayer player)
        {
            var container = new CuiElementContainer();

            var editUIPositions = _editPeople[player.userID];
            if (editUIPositions.UIPostions.UIPartsDict.TryGetValue(editUIPositions.ActiveUI, out UIParts uiPart))
            {
                double widthPixels = Math.Round((double)(uiPart.XMax - uiPart.XMin) * 1920);
                double heightPixels = Math.Round((double)(uiPart.YMax - uiPart.YMin) * 1080);

                var smallPanel = CreatePanel(ref container, ".05 1.02", ".35 2.24", ".17 .17 .17 1", "WCMainEditPanel", "WCSmallEditImage", true);
                var txtLabel = CreateLabel(ref container, ".025 .75", ".975 .95", "1 1 1 .1", "1 1 1 1", $"IMAGE SIZE -> <color=#ff4545ff>{widthPixels} x {heightPixels}</color>", 10, TextAnchor.MiddleCenter, smallPanel);
                CreateButton(ref container, ".9 0", "1 1", "1 1 1 0", "1 1 1 1", "X", 15, "wc_edit smallimgclose", txtLabel);

                CreateLabel(ref container, ".025 .53", ".975 .72", "1 1 1 .1", "1 1 1 1", "IMAGE", 10, TextAnchor.MiddleCenter, smallPanel);
                CreateInput(ref container, ".025 .05", ".975 .5", "wc_edit image img", "1 1 1 .1", "1 1 1 1", uiPart.Image == null ? " " : uiPart.Image, 12, TextAnchor.MiddleCenter, smallPanel);
            }

            CuiHelper.DestroyUi(player, "WCSmallEditImage");
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Methods
        [HookMethod("IsUsingPlugin")]
        bool IsUsingPlugin(string pluginName)
        {
            return _config.UIAddons.Any(x => x.Enabled && x.AddonName.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
        }

        private void RegisterNewImage(string imageName, string imageUrl)
        {
            Puts($"Registering {imageName} to {imageUrl}");
            ImageLibrary?.Call("AddImage", imageUrl, imageName, 0UL, null);
        }

        private string GetImage(string imageName)
        {
            if (ImageLibrary == null)
            {
                PrintError("Could not load images due to no Image Library");
                return null;
            }

            return ImageLibrary?.Call<string>("GetImage", "UI" + imageName, 0UL, false);
        }

        private void GetServerInfo()
        {
            var serverInfo = ConVar.Admin.ServerInfo();
            var timeInfo = int.Parse(serverInfo.GameTime.Split(' ')[1].Split(':')[0]);
            _serverInfo = new ServerInfoParsed { Hostname = serverInfo.Hostname, MaxPlayers = serverInfo.MaxPlayers, Players = serverInfo.Players, Time = timeInfo };
        }

        void ImportImages()
        {
            Dictionary<string, string> images = new Dictionary<string, string> { 
                { "UILeftArrow", "https://i.ibb.co/pLsjbtx/Left-Arrow.png" }, 
                { "UIRightArrow", "https://i.ibb.co/mCRYNQG/Right-Arrow.png" },
                { "UIUpArrow", "https://i.ibb.co/HdvBXCz/UpArrow.png" },
                { "UIDownArrow", "https://i.ibb.co/0GGnFNB/Down-Arrow.png" }
            };

            foreach (var item in _config.UIPositions.UIPartsDict.Where(x => !string.IsNullOrEmpty(x.Value.Image))) images.Add($"UI{item.Key}", item.Value.Image);
            foreach(var item in _config.UIPanels.Where(x => x.Enabled && !string.IsNullOrEmpty(x.PanelImage))) images.Add($"UI{item.PanelName}", item.PanelImage);
            foreach (var item in _config.UIPanels.Where(x => x.Enabled && !string.IsNullOrEmpty(x.TextPanelImage))) images.Add($"UI{item.PanelName}-text", item.TextPanelImage);
            foreach (var item in _config.UIPanels.Where(x => x.Enabled && !string.IsNullOrEmpty(x.ButtonImage))) images.Add($"UI{item.PanelName}-button", item.ButtonImage);
            foreach (var item in _config.UIAddons.Where(x => x.Enabled && !string.IsNullOrEmpty(x.ButtonImage))) images.Add($"UI{item.PanelName}-button", item.ButtonImage);

            ImageLibrary?.Call("ImportImageList", "WelcomeController", images, 0UL, true, null);
        }

        void RegisterCommandsAndPermissions()
        {
            if (!_config.DisplayOnJoin) Unsubscribe(nameof(OnPlayerConnected));

            for (int i = 0; i < _config.UIPanels.Count; i++)
            {
                var panelInfo = _config.UIPanels[i];
                if (!panelInfo.Enabled) continue;
                for (int i2 = 0; i2 < panelInfo.Commands.Count; i2++)
                    cmd.AddChatCommand(panelInfo.Commands[i2], this, UIOpenWelcomeMenuPanels);
            }

            for (int i = 0; i < _config.UIAddons.Count; i++)
            {
                var panelInfo = _config.UIAddons[i];
                if (!panelInfo.Enabled) continue;
                for (int i2 = 0; i2 < panelInfo.Commands.Count; i2++)
                    cmd.AddChatCommand(panelInfo.Commands[i2], this, UIOpenWelcomeMenuAddons);
            }
            
            foreach (var perm in _config.UIPanels)
            {
                if (!perm.Enabled) continue;
                if (!string.IsNullOrEmpty(perm.PanelPermission))
                {
                    var perms = perm.PanelPermission.ToLower().Contains("welcomecontroller") ? perm.PanelPermission : "welcomecontroller." + perm.PanelPermission;
                    permission.RegisterPermission(perms, this);
                }
            }

            foreach (var perm in _config.UIAddons)
            {
                if (!perm.Enabled) continue;
                if (!string.IsNullOrEmpty(perm.PanelPermission))
                {
                    var perms = perm.PanelPermission.ToLower().Contains("welcomecontroller") ? perm.PanelPermission : "welcomecontroller." + perm.PanelPermission;
                    permission.RegisterPermission(perms, this);
                }
            }

            permission.RegisterPermission("welcomecontroller.admin", this);

            cmd.AddChatCommand("welcomeedit", this, UIOpenEditMenu);
        }
        #endregion

        #region UI Methods
        private static string CreateSuperPanel(ref CuiElementContainer container, string anchorMin, string anchorMax, string panelColor, bool panelBlur, string panelImage = null, string text = null, int fontSize = 15, string textColor = "1 1 1 1", TextAnchor alignment = TextAnchor.MiddleCenter, string parent = "Overlay", string panelName = null, bool mainPanel = false, string buttonCommand = null)
        {
            CuiPanel panel = new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = anchorMin,
                    AnchorMax = anchorMax
                },
                Image = { Color = panelColor }
            };

            if (panelBlur) panel.Image.Material = "assets/content/ui/uibackgroundblur.mat";
            if (mainPanel) panel.CursorEnabled = true;

            string thePanel = container.Add(panel, parent, panelName);

            if (panelImage != null)
            {
                container.Add(new CuiElement
                {
                    Parent = thePanel,
                    Components =
                    {
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                        new CuiRawImageComponent {Png = panelImage},
                    }
                });
            }

            if(text != null)
            {
                container.Add(new CuiLabel
                {
                    Text =
                    {
                        Color = textColor,
                        Text = text,
                        Align = alignment,
                        FontSize = fontSize,
                        Font = "robotocondensed-bold.ttf"
                    }
                }, thePanel);
            }

            if (buttonCommand != null) container.Add(new CuiButton
            {
                Button = { Command = $"{buttonCommand}", Color = "0 0 0 0" }
            }, thePanel);

            return thePanel;
        }

        private static string CreateItemPanel(ref CuiElementContainer container, string anchorMin, string anchorMax, float padding, string color, int itemId, string parent = "Overlay",
        string panelName = null, ulong skinId = 0L)
        {
            var panel = container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = anchorMin,
                    AnchorMax = anchorMax
                },
                Image = { Color = color }
            }, parent, panelName);

            container.Add(new CuiElement
            {
                Parent = panel,
                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{padding} {padding + .004f}",
                        AnchorMax = $"{1 - padding - .004f} {1 - padding - .02f}"
                    },
                    new CuiImageComponent {ItemId = itemId, SkinId = skinId}
                }
            });

            return panel;
        }

        private static string CreateLabel(ref CuiElementContainer container, string anchorMin, string anchorMax, string backgroundColor, string textColor,
            string labelText, int fontSize, TextAnchor alignment, string parent = "Overlay",
            string labelName = null)
        {
            var panel = container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = anchorMin, AnchorMax = anchorMax                },
                Image = { Color = backgroundColor }
            }, parent, labelName);
            container.Add(new CuiLabel
            {
                Text =
                {
                    Color = textColor,
                    Text = labelText,
                    Align = alignment,
                    FontSize = fontSize,
                    Font = "robotocondensed-bold.ttf"
                }
            }, panel);
            return panel;
        }

        private static string CreatePanel(ref CuiElementContainer container, string anchorMin, string anchorMax, string panelColor, string parent = "Overlay",
            string panelName = null, bool blur = false)
        {
            if (blur)
                return container.Add(new CuiPanel
                {
                    RectTransform =
            {
                AnchorMin = anchorMin,
                AnchorMax = anchorMax
            },
                    Image = { Color = panelColor, Material = "assets/content/ui/uibackgroundblur.mat" }
                }, parent, panelName);
            else
                return container.Add(new CuiPanel
                {
                    RectTransform =
            {
                AnchorMin = anchorMin,
                AnchorMax = anchorMax
            },
                    Image = { Color = panelColor }
                }, parent, panelName);
        }

        private static void CreateImagePanel(ref CuiElementContainer container, string anchorMin, string anchorMax, string panelImage, string parent = "Overlay",
        string panelName = null)
        {
            container.Add(new CuiElement
            {
                Parent = parent,
                Name = panelName,
                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = anchorMin,
                        AnchorMax = anchorMax

                    },
                    new CuiRawImageComponent {Url = panelImage},
                }
            });
        }

        private static void CreateImageButton(ref CuiElementContainer container, string anchorMin, string anchorMax, string buttonColor, string buttonCommand, string panelImage, string parent = "Overlay", string panelName = null)
        {
            var panel = container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = anchorMin,
                    AnchorMax = anchorMax
                },
                Image = { Color = buttonColor }
            }, parent, panelName);

            container.Add(new CuiElement
            {
                Parent = panel,
                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = ".2 .2",
                        AnchorMax = ".8 .8"
                    },
                    new CuiRawImageComponent {Png = panelImage},
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = $"{buttonCommand}" }
            }, panel);
        }

        private static string CreateButton(ref CuiElementContainer container, string anchorMin, string anchorMax, string buttonColor, string textColor, string buttonText,
        int fontSize, string buttonCommand, string parent = "Overlay",
        TextAnchor labelAnchor = TextAnchor.MiddleCenter)
        {
            var panel = container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = anchorMin,
                    AnchorMax = anchorMax
                },
                Image = { Color = "0 0 0 0" }
            }, parent);

            container.Add(new CuiButton
            {
                Button = { Color = buttonColor, Command = $"{buttonCommand}" },
                Text = { Align = labelAnchor, Color = textColor, FontSize = fontSize, Text = buttonText }
            }, panel);
            return panel;
        }

        private static string CreateInput(ref CuiElementContainer container, string anchorMin, string anchorMax, string command, string backgroundColor, string textColor,
        string labelText, int fontSize, TextAnchor alignment, string parent = "Overlay",
        string labelName = null)
        {
            
            var panel = container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = anchorMin, AnchorMax = anchorMax
                },
                Image = { Color = backgroundColor }
            }, parent, labelName);

            container.Add(new CuiElement
            {
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Color = textColor,
                        Text = labelText,
                        Align = alignment,
                        FontSize = fontSize,
                        Font = "robotocondensed-bold.ttf",
                        NeedsKeyboard = true,
                        Command = command
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                },
                Parent = panel
            });

            return panel;
        }
        #endregion
    }
}
