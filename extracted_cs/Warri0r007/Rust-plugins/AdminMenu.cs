using Oxide.Core.Plugins;
using System.Collections.Generic;
using Network;
using System.Collections;
using System.Linq;
using System;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using UnityEngine.UI;
using Oxide.Core.Libraries.Covalence;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("AdminMenu", "0xF", "1.0.2")]
    [Description("Multifunctional in-game admin menu.")]
    public class AdminMenu : RustPlugin
    {
        [PluginReference]
        private Plugin ImageLibrary, Economics, ServerRewards, Clans;
        private const string PERMISSION_USE = "adminmenu.use";
        private const string PERMISSION_FULLACCESS = "adminmenu.fullaccess";
        private const string PERMISSION_CONVARS = "adminmenu.convars";
        private const string PERMISSION_PERMISSIONMANAGER = "adminmenu.permissionmanager";
        private const string PERMISSION_PLUGINMANAGER = "adminmenu.pluginmanager";
        private const string PERMISSION_GIVE = "adminmenu.give";
        private const string ADMINMENU_IMAGEBASE64 = "iVBORw0KGgoAAAANSUhEUgAAAWcAAAD3CAYAAADBqZV6AAAACXBIWXMAAAsTAAALEwEAmpwYAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAYeSURBVHgB7d3hbdtWGIbRj0EG8AZxJ4g3qLJBN4g7QZMJ6kyQdgNng2wQeQNtUI3gTMBeoilQFKKc2CT1mjoHIAj4/mDuFfWEoAmzCgAAAAAAAAAAAAAAAAAAAAAAAIDz1fV9f1Mcsu26bntooK3Zpu02tQ67Ns/PtSDn3JPs2+d1e2igretF272r6Y2eI+2YV233S63D5zbP3aGBGec5esyXbfu9GLMd+fmmVrRu7cS7bifIp1qOc+7x7tp2OzI2xHmOtb1t29h/4Fe1ns9z37bdyNhc89yPHfNFQfvytUC/LSCGOPMvgYYg4sx/CTSEEGf+T6AhgDhziEDDiYkzYwQaTkicOUag4UTEmYcINJyAOPM9BBoWJs58L4GGBYkzP0KgYSHizI8SaFiAOPMYAg0zE2ceS6BhRuLMUwg0zESceSqBhhmIM1MQaJiYODMVgYYJiTNTEmiYiDgzNYGGCbysaQ0vKnxfyxpevPix1mFYu11N77ptSwbztm1LvjB28NC596XWccxj7tv2pqa3r+X9WeMvld3UGbwkeOo4f+26blsLaldptSK7mdZv+22d1nxFe/Tcm+k8OcUxR7V/yxDnba3D6HehretlnQG3Nc5EO9Gva/mrWeCRxPmMCDQ8H+J8ZgQangdxPkMCDfnE+UwJNGSb+mkNnpEh0O0336/rn8cReebaZ3nRdu9qesOTE5+LRYkz98VaDHGe4/nf2xp/5piZuK0BEEicAQKJM0AgcQYIJM4AgcQZIJA4AwQSZ4BA4gwQSJwBAokzQCBxBggkzgCBxBkgkDgDBBJngEDiDBBInAECiTNAIHEGCDT1C15f933/pZZ1UQArM3Wch1BuCoAncVsDIJA4AwQSZ4BA4gwQSJwBAk39tAak+lDT2xfMRJw5C13X3RQ8I25rAAQSZ4BA4gwQSJwBAokzQCBxBggkzgCBxBkgkDgDBBJngEDiDBBInAECiTNAoKnjfNctrB3zTQGsjCtngEDiDBBInAECiTNAIHEGCCTOAIG84DXLx77v72tZVwUMfmvfv7cjY5e1MHHOIpRwOlHfP7c1AAKJM0AgcQYIJM4AgcQZIJA4AwQSZ4BA4gwQSJwBAokzQCBxBggkzgCBxBkgkDgDBBJngEDiDBBInAECiTNAIHEGCCTOAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwpuv7/qb4Ufuu624PDbT1vGy76+KY27Z++0MDbf2u2+6yVqDN8WZsbK55PnDMd213UdO6b8f8oxbU5jHM4V1Nb9fm8rlCDHHuix911z7EzaGBtpzDz78Ux7xp67c9NNDWb1i7Ta1Am2M3NjbXPB845r7tXtW0hguVn2pB3y6A/qrpDRcNv1aIFwVAHHEGCCTOAIHEGSCQOAMEEmeAQOIMEEicAQKJM0AgcQYIJM4AgcQZIJA4AwQSZ4BA4gwQSJwBAokzQCBxBggkzgCBxBkg0Mua1q5t74+Mz/Hi01Mc8xQ+te221mFXy3tzZOxj264Kgkwd569jb1UezPSi71Mc8xT2x+bJcQ+cI/cFYdzWAAgkzgCBxBkgkDgDBBJngEDiDBBInAECiTNAIHEGCCTOAIHEGSCQOAMEmvoPHzGfV33fb2oddl3X+WNDcIQ4Px/X37Y1GP5857aAUW5rAAQSZ4BA4gwQSJwBAokzQCBxBggkzgCBxBkgkDgDBBJngEDiDBBInAECiTNAIHEGCCTOAIHEGSCQOAMEEmeAQOIMEEicAQIt/YLXDzW9fZ2Hbdvuah32BRy1aJy7rrspHuvO+sH5cFsDIJA4AwQSZ4BA4gwQSJwBAokzQCBxBggkzgCBxBkgkDgDBBJngEDiDBBInAECiTNAIHEGCCTOAIHEGSCQOAMEEmeAQOIMEGjpt2/zeG/7vv+51uF913W7Wrn2eX05MnxV63DxwDxnOWadAXF+Pi6/bWtwFl+uZlPrN3yWm2JybmsABBJngEDiDBBInAECiTNAIHEGCCTOAIHEGSCQOAMEEmeAQOIMEEicAQKJM0AgcQYIJM4AgcQZIJA4AwQSZ4BA4gwQSJwBAv0Nie8vXooXAzkAAAAOZVhJZk1NACoAAAAIAAAAAAAAANJTkwAAAABJRU5ErkJggg==";
        private static AdminMenu Instance;
        private static Dictionary<string, Panel> panelList;
        private static string ADMINMENU_IMAGECRC;
        private MainMenu mainMenu;
        private Dictionary<string, string> defaultLang = new Dictionary<string, string>();
        static Configuration config;
        public AdminMenu()
        {
            Instance = this;
        }

        public class ButtonArray<T> : List<T> where T : Button
        {
            public ButtonArray() : base()
            {
            }

            public ButtonArray(IEnumerable<T> collection) : base(collection)
            {
            }

            public IEnumerable<T> GetAllowedButtons(Connection connection)
            {
                return GetAllowedButtons(connection.userid.ToString());
            }

            public IEnumerable<T> GetAllowedButtons(string userId)
            {
                return this.Where(b => b == null || b.UserHasPermission(userId));
            }
        }

        public class ButtonArray : ButtonArray<Button>
        {
        }

        public class Button
        {
            private string label = null;
            private string permission = null;
            public string Command { get; set; }
            public string[] Args { get; set; }
            public Label Label { get; set; }
            public virtual int FontSize { get; set; } = 14;
            public string BackgroundColor { get; set; }

            public string Permission
            {
                get
                {
                    return permission;
                }

                set
                {
                    permission = string.Format("adminmenu.{0}", value);
                    if (!Instance.permission.PermissionExists(permission))
                        Instance.permission.RegisterPermission(permission, Instance);
                }
            }

            public Button(string label, string backgroundColor, string command, params string[] args)
            {
                Label = new Label(label);
                BackgroundColor = backgroundColor;
                Command = command;
                Args = args;
            }

            public void Press(ConnectionData connectionData)
            {
                connectionData.userData[$"button_{this.GetHashCode()}.state.pressed"] = true;
            }

            public void Unpress(ConnectionData connectionData)
            {
                connectionData.userData[$"button_{this.GetHashCode()}.state.pressed"] = false;
            }

            public bool UserHasPermission(Connection connection)
            {
                return UserHasPermission(connection.userid.ToString());
            }

            public bool UserHasPermission(string userId)
            {
                return permission == null || Instance.permission.UserHasPermission(userId, PERMISSION_FULLACCESS) || Instance.permission.UserHasPermission(userId, Permission);
            }

            public virtual bool IsPressed(ConnectionData connectionData)
            {
                bool result = false;
                if (connectionData.userData.TryGetValue($"button_{this.GetHashCode()}.state.pressed", out object state))
                    result = (bool)state;
                return result;
            }

            public virtual bool IsHidden(ConnectionData connectionData)
            {
                return false;
            }
        }

        public class CategoryButton : Button
        {
            public override int FontSize { get; set; } = 22;

            public CategoryButton(string label, string command, params string[] args) : base(label, null, command, args)
            {
            }
        }

        public class HideButton : Button
        {
            public HideButton(string label, string command, params string[] args) : base(label, null, command, args)
            {
            }

            public override bool IsHidden(ConnectionData connectionData)
            {
                return connectionData.userData["backcommand"] == null;
            }
        }

        public static class SidebarCollection
        {
            private static Dictionary<string, Sidebar> all = new Dictionary<string, Sidebar>();
            public static Sidebar TryGet(string key)
            {
                if (all.TryGetValue(key, out Sidebar sidebars))
                    return sidebars;
                return null;
            }

            public static void Set(string key, Sidebar value)
            {
                all[key] = value;
            }
        }

        public static class ContentCollection
        {
            private static Dictionary<string, Content> all = new Dictionary<string, Content>();
            public static Content TryGet(string key)
            {
                if (all.TryGetValue(key, out Content content))
                    return content;
                return null;
            }

            public static void Set(string key, Content value)
            {
                all[key] = value;
            }
        }

        public static class Collector
        {
            public static void CollectAll()
            {
                CollectPluginsStaff();
            }

            public static void CollectPluginsStaff()
            {
                List<string> all_permissions = Instance.permission.GetPermissions().ToList();
                List<string> pluginNames = new List<string>();
                foreach (var plugin in Instance.plugins.GetAll())
                {
                    if (plugin.IsCorePlugin)
                        continue;
                    string pluginName = plugin.Name;
                    string pluginNameLower = plugin.ToString().Replace("Oxide.Plugins.", "").ToLower();
                    IEnumerable<string> permissions = all_permissions.Where(perm => perm.ToLower().Contains($"{pluginNameLower}"));
                    if (permissions.Count() > 0)
                    {
                        pluginNames.Add(pluginName);
                        ContentCollection.Set($"permissions.{pluginName.GetHashCode()}", new PermissionsContent { plugin = plugin, permissions = permissions.ToArray() });
                    }
                }

                pluginNames = pluginNames.OrderBy(name => name).ToList();
                int pluginsCount = pluginNames.Count;
                int amountPerPage = 12;
                int c = Mathf.CeilToInt(pluginsCount / (float)amountPerPage);
                for (int i = 0; i < c; i++)
                {
                    List<string> range = pluginNames.GetRange(i * amountPerPage, Mathf.Min(amountPerPage, pluginsCount - i * amountPerPage));
                    ButtonArray<CategoryButton> buttons = new ButtonArray<CategoryButton>();
                    if (i > 0)
                        buttons.Add(new CategoryButton("PREV PAGE", "permissions.pluginlistpage.open", $"{i - 1}") { FontSize = 18 });
                    buttons.AddRange(range.Select(name => new CategoryButton(name, "permissions.pluginpermissions.open", name.GetHashCode().ToString()) { FontSize = 18 }));
                    if (i < c - 1)
                        buttons.Add(new CategoryButton("NEXT PAGE", "permissions.pluginlistpage.open", $"{i + 1}") { FontSize = 18 });
                    SidebarCollection.Set($"pluginpermissions.{i}", new Sidebar() { AutoActivateCategoryButtonIndex = -1, CategoryButtons = buttons });
                }
            }
        }

        public class Configuration
        {
            [JsonProperty(PropertyName = "Text under the ADMIN MENU")]
            public string Subtext { get; set; } = "BY 0XF";

            [JsonProperty(PropertyName = "Disable X button hook")]
            public bool DisableSwapSeatsHook { get; set; } = false;

            [JsonProperty(PropertyName = "Chat command to show admin menu")]
            public string ChatCommand { get; set; } = "admin";

            public static Configuration DefaultConfig()
            {
                return new Configuration();
            }
        }

        public class ConnectionData
        {
            public Connection connection;
            public MainMenu currentMainMenu;
            public Panel currentPanel;
            public Sidebar currentSidebar;
            public Content currentContent;
            public Dictionary<string, object> userData;
            public ConnectionData(BasePlayer player) : this(player.Connection)
            {
            }

            public ConnectionData(Connection connection)
            {
                this.connection = connection;
                this.userData = new Dictionary<string, object>()
                {
                    {
                        "userId",
                        connection.userid
                    },
                    {
                        "userinfo.lastuserid",
                        connection.userid
                    },
                    {
                        "backcommand",
                        null
                    },
                };
                this.UI = new ConnectionUI(this);
                try
                {
                    Init();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            public ConnectionUI UI { get; private set; }
            public bool IsAdminMenuDisplay { get; set; }
            public bool IsDestroyed { get; set; }

            public void Init()
            {
                UI.RenderMainMenu(Instance.mainMenu);
                this.currentMainMenu = Instance.mainMenu;
            }

            public void ShowAdminMenu()
            {
                UI.ShowAdminMenu();
                IsAdminMenuDisplay = true;
            }

            public void HideAdminMenu()
            {
                UI.HideAdminMenu();
                IsAdminMenuDisplay = false;
            }

            public void OpenPanel(string panelName)
            {
                if (panelList.TryGetValue(panelName, out Panel panel))
                {
                    if (currentPanel == panel)
                        return;
                    if (currentContent != null)
                        currentContent.RestoreUserData(userData);
                    currentContent = null;
                    if (currentPanel != null)
                        currentPanel.OnClose(this);
                    currentPanel = panel;
                    currentSidebar = currentPanel.Sidebar;
                    UI.RenderPanel(currentPanel);
                    currentPanel.OnOpen(this);
                    Content defaultPanelContent = panel.DefaultContent;
                    if (defaultPanelContent != null)
                        ShowPanelContent(defaultPanelContent);
                    if (panel.Sidebar != null && panel.Sidebar.AutoActivateCategoryButtonIndex.HasValue)
                        Instance.HandleCommand(connection, "uipanel.sidebar.button_pressed", panel.Sidebar.AutoActivateCategoryButtonIndex.Value.ToString(), panel.Sidebar.CategoryButtons.GetAllowedButtons(connection).Count().ToString());
                }
                else
                {
                    Instance.PrintError($"Panel with name \"{panelName}\" not founded!");
                }
            }

            public void SetSidebar(Sidebar sidebar)
            {
                bool needsChangeContentSize = (currentSidebar != sidebar);
                currentSidebar = sidebar;
                CUI.Root root = new CUI.Root("AdminMenu_Panel");
                if (sidebar != null)
                    UI.AddSidebar(root, sidebar);
                else
                    root.Add(new CUI.Element { DestroyUi = "AdminMenu_Panel_Sidebar" });
                root.Render(connection);
                if (needsChangeContentSize)
                {
                    CUI.Root updateRoot = new CUI.Root();
                    updateRoot.Add(new CUI.Element { Components = { new CuiRectTransformComponent { OffsetMin = $"{(sidebar != null ? 250 : 0)} 0", } }, Name = "AdminMenu_Panel_Content" });
                    updateRoot.Update(connection);
                }
            }

            public void ShowPanelContent(Content content)
            {
                if (content == null)
                {
                    CUI.Root root = new CUI.Root();
                    root.Add(new CUI.Element { DestroyUi = "AdminMenu_Panel_TempContent" });
                    root.Render(connection);
                    return;
                }

                if (currentContent != null)
                    currentContent.RestoreUserData(userData);
                currentContent = content;
                currentContent.LoadDefaultUserData(userData);
                UI.RenderContent(content);
            }

            public void ShowPanelContent(string contentId)
            {
                ShowPanelContent(currentPanel.TryGetContent(contentId));
            }

            public void Dispose()
            {
                all.Remove(connection);
            }

            public static Dictionary<Connection, ConnectionData> all = new Dictionary<Connection, ConnectionData>();
            public static ConnectionData Get(Connection connection)
            {
                if (connection == null)
                    return null;
                ConnectionData data;
                if (all.TryGetValue(connection, out data))
                    return data;
                return null;
            }

            public static ConnectionData Get(BasePlayer player)
            {
                return Get(player.Connection);
            }

            public static ConnectionData GetOrCreate(Connection connection)
            {
                if (connection == null)
                    return null;
                ConnectionData data = Get(connection);
                if (data == null)
                    data = all[connection] = new ConnectionData(connection);
                return data;
            }

            public static ConnectionData GetOrCreate(BasePlayer player)
            {
                return GetOrCreate(player.Connection);
            }
        }

        public class ConnectionUI
        {
            Connection connection;
            ConnectionData connectionData;
            public ConnectionUI(ConnectionData connectionData)
            {
                this.connectionData = connectionData;
                this.connection = connectionData.connection;
            }

            public void ShowAdminMenu()
            {
                CUI.Root root = new CUI.Root();
                root.Add(new CUI.Element { Components = { new CuiRectTransformComponent { AnchorMin = "0 0.00001", AnchorMax = "1 1.00001" } }, Name = "AdminMenu", Update = true });
                root.Add(new CUI.Element { Components = { new CuiNeedsCursorComponent() }, Parent = "AdminMenu", Name = "AdminMenu_Cursor" });
                root.Render(connection);
            }

            public void HideAdminMenu()
            {
                CUI.Root root = new CUI.Root();
                root.Add(new CUI.Element { Components = { new CuiRectTransformComponent { AnchorMin = "1000 1000", AnchorMax = "1001 1001" } }, DestroyUi = "AdminMenu_Cursor", Name = "AdminMenu", Update = true });
                root.Render(connection);
            }

            public void DestroyAdminMenu()
            {
                CuiHelper.DestroyUi(connection.player as BasePlayer, "AdminMenu");
                connectionData.IsDestroyed = true;
            }

            public void DestroyAll()
            {
                DestroyAdminMenu();
                CuiHelper.DestroyUi(connection.player as BasePlayer, "AdminMenu_OpenButton");
            }

            public void AddSidebar(CUI.Element element, Sidebar sidebar)
            {
                if (sidebar == null)
                    return;
                var sidebarPanel = element.AddPanel(color: "0.784 0.329 0.247 0.427", material: "assets/content/ui/uibackgroundblur-ingamemenu.mat", imageType: Image.Type.Tiled, anchorMin: "0 0", anchorMax: "0 1", offsetMin: "0 0", offsetMax: "250 0", name: "AdminMenu_Panel_Sidebar").AddDestroySelfAttribute();
                sidebarPanel.AddPanel(color: "0.217 0.217 0.217 0.796", sprite: "assets/content/ui/ui.background.transparent.linear.psd", material: "assets/content/ui/namefontmaterial.mat", anchorMin: "0 0", anchorMax: "1 1");
                IEnumerable<CategoryButton> categoryButtons = sidebar.CategoryButtons.GetAllowedButtons(connection);
                if (categoryButtons != null)
                {
                    int categoryButtonsCount = categoryButtons.Count();
                    if (categoryButtonsCount == 0)
                        return;
                    var sidebarButtonGroup = sidebarPanel.AddContainer(anchorMin: "0 0.5", anchorMax: "1 0.5", offsetMin: $"0 -{categoryButtonsCount * 48 / 2}", offsetMax: $"0 {categoryButtonsCount * 48 / 2}");
                    for (int i = 0; i < categoryButtonsCount; i++)
                    {
                        CategoryButton categoryButton = categoryButtons.ElementAt(i);
                        sidebarButtonGroup.AddButton(command: $"adminmenu uipanel.sidebar.button_pressed {i} {categoryButtonsCount}", color: $"0 0 0 0", anchorMin: "0 1", anchorMax: "1 1", offsetMin: $"16 -{(i + 1) * 48}", offsetMax: $"0 -{i * 48}", name: $"UIPanel_SideBar_Button{i}").AddText(text: categoryButton.Label.Localize(connection), color: "0.969 0.922 0.882 1", font: CUI.Font.RobotoCondensedBold, fontSize: categoryButton.FontSize, align: TextAnchor.MiddleRight, offsetMin: "16 0", offsetMax: "-16 0");
                    }
                }
            }

            private void AddNavButtons(CUI.Element element, MainMenu mainMenu)
            {
                IEnumerable<Button> navButtons = mainMenu.NavButtons.GetAllowedButtons(connection);
                int navButtonsCount = navButtons.Count();
                var navButtonGroup = element.AddContainer(anchorMin: "0 0", anchorMax: "1 0", offsetMin: "64 64", offsetMax: $"0 {64 + navButtonsCount * 42}", name: "Navigation ButtonGroup").AddDestroySelfAttribute();
                for (int i = 0; i < navButtonsCount; i++)
                {
                    Button navButton = navButtons.ElementAtOrDefault(i);
                    if (navButton == null)
                        continue;
                    navButtonGroup.AddButton(command: navButton.IsHidden(connectionData) ? null : $"adminmenu navigation.button_pressed {i} {navButtonsCount}", anchorMin: "0 1", anchorMax: "1 1", offsetMin: $"0 -{(i + 1) * 42}", offsetMax: $"0 -{i * 42}").AddText(text: navButton.Label.Localize(connection).ToUpper(), color: $"0.969 0.922 0.882 {(navButton.IsHidden(connectionData) ? 0 : (navButton.IsPressed(connectionData) ? 1 : 0.180f))}", fontSize: 28, font: CUI.Font.RobotoCondensedBold, align: TextAnchor.LowerLeft, overflow: VerticalWrapMode.Truncate, offsetMin: "10 5", name: $"NavigationButtonText{i}");
                }
            }

            public void UpdateNavButtons(MainMenu mainMenu)
            {
                CUI.Root root = new CUI.Root("AdminMenu_Navigation");
                AddNavButtons(root, mainMenu);
                root.Render(connection);
            }

            public void RenderOverlayOpenButton()
            {
                CUI.Root root = new CUI.Root("Overlay");
                root.AddButton(command: "adminmenu", color: "0.969 0.922 0.882 0.035", material: "assets/icons/greyout.mat", anchorMin: "0 0", anchorMax: "0 0", offsetMin: "0 0", offsetMax: "100 30", name: "AdminMenu_OpenButton").AddDestroySelfAttribute().AddText(text: "ADMIN MENU", color: "0.969 0.922 0.882 0.45", font: CUI.Font.RobotoCondensedBold, fontSize: 14, align: TextAnchor.MiddleCenter);
                root.Render(connection);
            }

            public void RenderMainMenu(MainMenu mainMenu)
            {
                if (mainMenu == null)
                    return;
                CUI.Root root = new CUI.Root("Overlay");
                var container = root.AddPanel(color: "0.169 0.162 0.143 1", material: "assets/content/ui/uibackgroundblur-mainmenu.mat", imageType: Image.Type.Tiled, anchorMin: "1000 1000", anchorMax: "1001 1001", name: "AdminMenu").AddDestroySelfAttribute();
                container.AddPanel(color: "0.301 0.283 0.235 1", sprite: "assets/content/ui/ui.background.transparent.radial.psd", material: "assets/content/ui/namefontmaterial.mat", anchorMin: "0 0", anchorMax: "1 1");
                container.AddPanel(color: "0.169 0.162 0.143 0.384", sprite: "assets/content/ui/ui.background.transparent.radial.psd", material: "assets/content/ui/namefontmaterial.mat", anchorMin: "0 0", anchorMax: "1 1");
                var navigation = container.AddContainer(anchorMin: "0 0", anchorMax: "0 1", offsetMin: "0 0", offsetMax: "350 0", name: "AdminMenu_Navigation");
                var homeButton = navigation.AddContainer(//command: "adminmenu homebutton",
                anchorMin: "0 1", anchorMax: "1 1", offsetMin: $"64 -{102f + 32}", offsetMax: "0 -32");
                homeButton.AddImage(content: ADMINMENU_IMAGECRC, color: "0.811 0.811 0.811 1", material: "assets/content/ui/namefontmaterial.mat", anchorMin: "0 0", anchorMax: "0 1", offsetMax: $"146.4 0");
                homeButton.AddText(text: config.Subtext, color: "0.824 0.824 0.824 1", font: CUI.Font.RobotoCondensedBold, fontSize: 16, anchorMin: "0 0", anchorMax: "0 0", offsetMin: "0 -35", offsetMax: "146.4 -10");
                AddNavButtons(navigation, mainMenu);
                var body = container.AddContainer(anchorMin: "0 0", anchorMax: "1 1", offsetMin: "350 0", offsetMax: "-64 0", name: "AdminMenu_Body");
                var right = container.AddContainer(anchorMin: "1 0", anchorMax: "1 1", offsetMin: "-64 0", offsetMax: "0 0");
                root.Render(connection);
                connectionData.IsDestroyed = false;
            }

            public void RenderPanel(Panel panel)
            {
                if (panel == null)
                    return;
                CUI.Root root = new CUI.Root("AdminMenu_Body");
                var container = root.AddContainer(anchorMin: "0 0", anchorMax: "1 1", name: "AdminMenu_Panel").AddDestroySelfAttribute();
                Sidebar sidebar = panel.Sidebar;
                if (sidebar != null)
                    AddSidebar(container, sidebar);
                CUI.Element panelBackground = container.AddContainer(anchorMin: "0 0", anchorMax: "1 1", offsetMin: $"{(sidebar != null ? 250 : 0)} 0", offsetMax: "0 0", name: "AdminMenu_Panel_Content");
                if (true)
                {
                    panelBackground = panelBackground.AddPanel(color: "1 1 1 1", material: "assets/content/ui/menuui/mainmenu.panel.mat", imageType: Image.Type.Tiled);
                }

                root.Render(connection);
            }

            public void RenderContent(Content content)
            {
                if (content == null)
                    return;
                CUI.Root root = new CUI.Root("AdminMenu_Panel_Content");
                var container = root.AddContainer(name: "AdminMenu_Panel_TempContent").AddDestroySelfAttribute();
                root.Render(connection);
                content.Render(connectionData);
            }
        }

        public class Label
        {
            private static readonly Regex richTextRegex = new Regex(@"<[^>]*>");
            string label;
            string langKey;
            public Label(string label)
            {
                this.label = label;
                this.langKey = richTextRegex.Replace(label.ToPrintable(), string.Empty).Trim();
                if (!Instance.defaultLang.ContainsKey(this.langKey))
                    Instance.defaultLang.Add(this.langKey, label);
            }

            public override string ToString()
            {
                return label;
            }

            public string Localize(string userId)
            {
                return Instance.lang.GetMessage(this.langKey, Instance, userId);
            }

            public string Localize(Connection connection)
            {
                return Localize(connection.userid.ToString());
            }
        }

        public class MainMenu
        {
            public ButtonArray NavButtons { get; set; }
        }

        public class Panel
        {
            public virtual Sidebar Sidebar { get; set; }
            public virtual Dictionary<string, Content> Content { get; set; }
            public Content DefaultContent { get => TryGetContent("default"); }

            public Content TryGetContent(string id)
            {
                if (Content == null)
                    return null;
                if (Content.TryGetValue(id, out Content content))
                    return content;
                return null;
            }

            public virtual void OnOpen(ConnectionData connectionData)
            {
                connectionData.UI.UpdateNavButtons(connectionData.currentMainMenu);
            }

            public virtual void OnClose(ConnectionData connectionData)
            {
                connectionData.userData["backcommand"] = null;
            }
        }

        public class UserInfoPanel : Panel
        {
        }

        public class PermissionPanel : Panel
        {
            public override Sidebar Sidebar { get => SidebarCollection.TryGet("pluginpermissions.0"); }

            public override void OnClose(ConnectionData connectionData)
            {
                connectionData.userData["backcommand"] = null;
            }
        }

        public class Sidebar
        {
            public ButtonArray<CategoryButton> CategoryButtons { get; set; }
            public int? AutoActivateCategoryButtonIndex { get; set; } = 0;
        }

        public class CUI
        {
            private static readonly string[] FontNames = new string[]
            {
                "RobotoCondensed-Bold.ttf",
                "RobotoCondensed-Regular.ttf",
                "RobotoMono-Regular.ttf",
                "DroidSansMono.ttf",
                "PermanentMarker.ttf"
            };
            public enum Font
            {
                RobotoCondensedBold,
                RobotoCondensedRegular,
                RobotoMonoRegular,
                DroidSansMono,
                PermanentMarker
            }

            public static void AddUI(Connection connection, string json)
            {
                CommunityEntity.ServerInstance.ClientRPCEx<string>(new SendInfo { connection = connection }, null, "AddUI", json);
            }

            [JsonObject(MemberSerialization.OptIn)]
            public class Element : CuiElement
            {
                public string elementName;
                public new string Name
                {
                    get => elementName;
                    set
                    {
                        if (value == null)
                            return;
                        elementName = value;
                    }
                }

                public Element ParentElement { get; set; }
                public virtual List<Element> Container => ParentElement?.Container;

                [JsonProperty("name")]
                public string JsonName
                {
                    get
                    {
                        if (Name == null)
                            SetupDefaultName();
                        return Name;
                    }
                }

                public Element()
                {
                }

                public Element(Element parent)
                {
                    AssignParent(parent);
                }

                public void SetupDefaultName()
                {
                    Name = Guid.NewGuid().ToString().MurmurHashSigned().ToString();
                }

                public void AssignParent(Element parent)
                {
                    if (parent == null)
                        return;
                    ParentElement = parent;
                    Parent = ParentElement.Name;
                    Name = Parent.MurmurHashSigned().ToString();
                }

                public Element AddDestroySelfAttribute()
                {
                    this.DestroyUi = this.Name;
                    return this;
                }

                public Element Add(Element element)
                {
                    if (element.Name == null && element.ParentElement == null)
                        element.AssignParent(this);
                    Container.Add(element);
                    return element;
                }

                public Element AddText(string text = "Text", string color = "1 1 1 1", Font font = Font.RobotoCondensedRegular, int fontSize = 14, TextAnchor align = TextAnchor.UpperLeft, VerticalWrapMode overflow = VerticalWrapMode.Overflow, string anchorMin = "0 0", string anchorMax = "1 1", string offsetMin = "0 0", string offsetMax = "0 0", float fadeIn = 0f, float fadeOut = 0f, string name = null)
                {
                    return Add(new Element(this) { Components = { new CuiTextComponent { Text = text, Color = color, Font = FontNames[(int)font], VerticalOverflow = overflow, FontSize = fontSize, Align = align, FadeIn = fadeIn }, new CuiRectTransformComponent() { AnchorMin = anchorMin, AnchorMax = anchorMax, OffsetMin = offsetMin, OffsetMax = offsetMax }, }, Name = name, FadeOut = fadeOut });
                }

                public Element AddOutlinedText(string text = "Text", string color = "1 1 1 1", Font font = Font.RobotoCondensedRegular, int fontSize = 14, TextAnchor align = TextAnchor.UpperLeft, VerticalWrapMode overflow = VerticalWrapMode.Overflow, string outlineColor = "0 0 0 1", float outlineWidth = 1, string anchorMin = "0 0", string anchorMax = "1 1", string offsetMin = "0 0", string offsetMax = "0 0", float fadeIn = 0f, float fadeOut = 0f, string name = null)
                {
                    return Add(new Element(this) { Components = { new CuiTextComponent { Text = text, Color = color, Font = FontNames[(int)font], VerticalOverflow = overflow, FontSize = fontSize, Align = align, FadeIn = fadeIn }, new CuiOutlineComponent { Color = outlineColor, Distance = $"{outlineWidth} {outlineWidth}" }, new CuiRectTransformComponent() { AnchorMin = anchorMin, AnchorMax = anchorMax, OffsetMin = offsetMin, OffsetMax = offsetMax }, }, FadeOut = fadeOut, Name = name });
                }

                public Element AddInputfield(string command, string text = "Enter text here...", string color = "1 1 1 1", Font font = Font.RobotoCondensedRegular, int fontSize = 14, TextAnchor align = TextAnchor.UpperLeft, string anchorMin = "0 0", string anchorMax = "1 1", string offsetMin = "0 0", string offsetMax = "0 0", bool needsKeyboard = true, bool autoFocus = false, bool isPassword = false, int charsLimit = 0, string name = null)
                {
                    return Add(new Element(this) { Components = { new CuiInputFieldComponent { Text = text, Color = color, Font = FontNames[(int)font], FontSize = fontSize, Align = align, Autofocus = autoFocus, Command = command, IsPassword = isPassword, CharsLimit = charsLimit, NeedsKeyboard = needsKeyboard, }, new CuiRectTransformComponent() { AnchorMin = anchorMin, AnchorMax = anchorMax, OffsetMin = offsetMin, OffsetMax = offsetMax }, }, Name = name });
                }

                public Element AddPanel(string color = "1 1 1 1", string sprite = "assets/content/ui/ui.background.tile.psd", string material = "assets/content/ui/namefontmaterial.mat", UnityEngine.UI.Image.Type imageType = UnityEngine.UI.Image.Type.Simple, string anchorMin = "0 0", string anchorMax = "1 1", string offsetMin = "0 0", string offsetMax = "0 0", float fadeIn = 0f, float fadeOut = 0f, bool cursorEnabled = false, bool keyboardEnabled = false, string name = null)
                {
                    Element element = new Element(this)
                    {
                        Components =
                        {
                            new CuiRectTransformComponent
                            {
                                AnchorMin = anchorMin,
                                AnchorMax = anchorMax,
                                OffsetMin = offsetMin,
                                OffsetMax = offsetMax,
                            },
                            new CuiImageComponent
                            {
                                Color = color,
                                Sprite = sprite,
                                Material = material,
                                ImageType = imageType,
                                FadeIn = fadeIn,
                            }
                        },
                        Name = name,
                        FadeOut = fadeOut
                    };
                    if (cursorEnabled)
                        element.Components.Add(new CuiNeedsCursorComponent());
                    if (keyboardEnabled)
                        element.Components.Add(new CuiNeedsKeyboardComponent());
                    return Add(element);
                }

                public Element AddButton(string command, string close = null, string color = "0 0 0 0", string sprite = "assets/content/ui/ui.background.tile.psd", string material = "assets/icons/iconmaterial.mat", UnityEngine.UI.Image.Type imageType = UnityEngine.UI.Image.Type.Simple, string anchorMin = "0 0", string anchorMax = "1 1", string offsetMin = "0 0", string offsetMax = "0 0", float fadeIn = 0f, float fadeOut = 0f, string name = null)
                {
                    return Add(new Element(this) { Components = { new CuiRectTransformComponent { AnchorMin = anchorMin, AnchorMax = anchorMax, OffsetMin = offsetMin, OffsetMax = offsetMax, }, new CuiButtonComponent { Close = close, Command = command, Color = color, Sprite = sprite, Material = material, ImageType = imageType, FadeIn = fadeIn, } }, Name = name, FadeOut = fadeOut });
                }

                public Element AddImage(string content, string color = "1 1 1 1", string material = "assets/content/ui/namefontmaterial.mat", string anchorMin = "0 0", string anchorMax = "1 1", string offsetMin = "0 0", string offsetMax = "0 0", float fadeIn = 0f, float fadeOut = 0f, string name = null)
                {
                    Element element = new Element(this)
                    {
                        Components =
                        {
                            new CuiRectTransformComponent()
                            {
                                AnchorMin = anchorMin,
                                AnchorMax = anchorMax,
                                OffsetMin = offsetMin,
                                OffsetMax = offsetMax
                            },
                        },
                        Name = name,
                        FadeOut = fadeOut
                    };
                    if (!string.IsNullOrEmpty(content))
                    {
                        CuiRawImageComponent rawImageComponent = new CuiRawImageComponent()
                        {
                            Color = color,
                            Material = material,
                            FadeIn = fadeIn
                        };
                        if (content.Contains("://"))
                            rawImageComponent.Url = content;
                        else
                            rawImageComponent.Png = content;
                        element.Components.Add(rawImageComponent);
                    };
                    return Add(element);
                }

                public Element AddHImage(string content, string color = "1 1 1 1", string anchorMin = "0 0", string anchorMax = "1 1", string offsetMin = "0 0", string offsetMax = "0 0", string name = null)
                {
                    return Add(new Element(this) { Components = { new CuiRawImageComponent() { Color = color, Png = content, Material = "assets/icons/iconmaterial.mat" }, new CuiRectTransformComponent() { AnchorMin = anchorMin, AnchorMax = anchorMax, OffsetMin = offsetMin, OffsetMax = offsetMax }, }, Name = name });
                }

                public Element AddIcon(int itemId, ulong skin = 0, string color = "1 1 1 1", string sprite = "assets/content/ui/ui.background.tile.psd", string material = "assets/icons/iconmaterial.mat", UnityEngine.UI.Image.Type imageType = UnityEngine.UI.Image.Type.Simple, string anchorMin = "0 0", string anchorMax = "1 1", string offsetMin = "0 0", string offsetMax = "0 0", float fadeIn = 0f, float fadeOut = 0f, string name = null)
                {
                    return Add(new Element(this) { Components = { new CuiImageComponent() { Color = color, ItemId = itemId, SkinId = skin, Sprite = sprite, Material = material, ImageType = imageType, FadeIn = fadeIn }, new CuiRectTransformComponent() { AnchorMin = anchorMin, AnchorMax = anchorMax, OffsetMin = offsetMin, OffsetMax = offsetMax }, }, Name = name, FadeOut = fadeOut });
                }

                public Element AddContainer(string anchorMin = "0 0", string anchorMax = "1 1", string offsetMin = "0 0", string offsetMax = "0 0", string name = null)
                {
                    return Add(new Element(this) { Components = { new CuiRectTransformComponent() { AnchorMin = anchorMin, AnchorMax = anchorMax, OffsetMin = offsetMin, OffsetMax = offsetMax }, }, Name = name, });
                }
            }

            public class Root : Element
            {
                public bool wasRendered = false;
                public Root()
                {
                    Name = string.Empty;
                }

                public Root(string rootObjectName = "Overlay")
                {
                    Name = rootObjectName;
                }

                public override List<Element> Container { get; } = new List<Element>();

                private void AddElementsToList(Element element, List<Element> list)
                {
                    list.Add(element);
                    foreach (Element child in element.Container)
                        AddElementsToList(child, list);
                }

                public List<Element> GetCommonContainer(bool includeRoot = false)
                {
                    var list = new List<Element>();
                    if (includeRoot)
                        list.Add(this);
                    foreach (Element child in this.Container)
                        AddElementsToList(child, list);
                    return list;
                }

                public string ToJson(List<Element> elements, bool format = false)
                {
                    return JsonConvert.SerializeObject(elements, format ? Formatting.Indented : Formatting.None, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }).Replace("\\n", "\n");
                }

                public string ToJson(bool format = false)
                {
                    return ToJson(Container, format);
                }

                public void Render(Connection connection)
                {
                    if (connection == null || !connection.connected)
                        return;
                    wasRendered = true;
                    CUI.AddUI(connection, ToJson(Container));
                }

                public void Render(BasePlayer player)
                {
                    Render(player.Connection);
                }

                public void Update(Connection connection)
                {
                    foreach (Element element in Container)
                        element.Update = true;
                    CUI.AddUI(connection, ToJson(Container));
                }

                public void Update(BasePlayer player)
                {
                    Update(player.Connection);
                }
            }
        }

        public class CenteredTextContent : Content
        {
            public string text = string.Empty;
            public CUI.Font font = CUI.Font.RobotoCondensedBold;
            public int fontSize = 24;
            protected override void Render(CUI.Element root, ConnectionData connectionData, Dictionary<string, object> userData)
            {
                root.AddText(text: text, font: CUI.Font.RobotoCondensedBold, fontSize: fontSize, align: TextAnchor.MiddleCenter).AddDestroySelfAttribute();
            }
        }

        public class Content
        {
            public void Render(ConnectionData connectionData)
            {
                CUI.Root root = new CUI.Root("AdminMenu_Panel_TempContent");
                Render(root, connectionData, connectionData.userData);
                root.Render(connectionData.connection);
            }

            protected virtual void Render(CUI.Element root, ConnectionData connectionData, Dictionary<string, object> userData)
            {
            }

            public virtual void LoadDefaultUserData(Dictionary<string, object> userData)
            {
            }

            public virtual void RestoreUserData(Dictionary<string, object> userData)
            {
            }
        }

        public class ConvarsContent : Content
        {
            private static readonly Label SEARCH_LABEL = new Label("Search..");
            public override void LoadDefaultUserData(Dictionary<string, object> userData)
            {
                userData["convars.searchQuery"] = string.Empty;
                userData["convars.page"] = 1;
            }

            protected override void Render(CUI.Element root, ConnectionData connectionData, Dictionary<string, object> userData)
            {
                string searchQuery = (string)userData["convars.searchQuery"];
                int page = (int)userData["convars.page"];
                var container = root.AddContainer(anchorMin: "0.03 0.04", anchorMax: "0.97 0.99", name: "AdminMenu_Convars").AddDestroySelfAttribute();
                int rows = 16;
                List<ConsoleSystem.Command> commands = ConsoleGen.All.Where(command => command.ServerAdmin && command.Variable && (string.IsNullOrEmpty(searchQuery) || command.FullName.Contains(searchQuery.ToLower()))).ToList();
                int endPage = Mathf.CeilToInt(commands.Count / (float)rows);
                userData["convars.page"] = page = Mathf.Clamp(page, 1, endPage);
                if (commands.Count > 0)
                {
                    int alreadySnownCount = rows * (page - 1);
                    commands = commands.GetRange(rows * (page - 1), Mathf.Min(rows, commands.Count - alreadySnownCount));
                }

                float width = 1f / rows;
                for (int i = 0; i < rows; i++)
                {
                    ConsoleSystem.Command command = commands.ElementAtOrDefault(i);
                    if (command == null)
                        return;
                    var convarContainer = container.AddContainer(anchorMin: $"0 {1f - width * (i + 1)}", anchorMax: $"1 {1f - width * i}");
                    convarContainer.AddText(text: $"<b>{command.FullName}</b>{(!string.IsNullOrEmpty(command.Description) ? $"\n<color=#7A7A7A><size=11>{command.Description}</size></color>" : string.Empty)}", align: TextAnchor.MiddleLeft, anchorMax: "0.7 1");
                    convarContainer.AddPanel(sprite: "assets/content/ui/ui.background.rounded.png", imageType: UnityEngine.UI.Image.Type.Tiled, color: "0.2 0.2 0.2 1", anchorMin: "0.75 0.1", anchorMax: "1 0.9").AddInputfield(command: $"adminmenu convar.setvalue {command.FullName} ", text: command.String, align: TextAnchor.MiddleCenter, offsetMin: "10 0", offsetMax: "-10 0");
                }

                var bottom = root.AddContainer(anchorMin: "0.02 0", anchorMax: "0.98 0.035", name: "AdminMenu_Convars_Bottom").AddDestroySelfAttribute();
                var searchPanel = bottom.AddButton(command: "adminmenu convars.opensearch", color: "0.15 0.15 0.15 0.7", anchorMin: "0.5 0", anchorMax: "0.5 1", offsetMin: "-100 0", offsetMax: "100 0", name: "Search");
                if (string.IsNullOrEmpty(searchQuery))
                {
                    searchPanel.AddText(text: SEARCH_LABEL.Localize(connectionData.connection), font: CUI.Font.RobotoCondensedBold, align: TextAnchor.MiddleCenter, offsetMin: "10 0", name: "Search_Placeholder");
                }
                else
                {
                    searchPanel.AddInputfield(command: "adminmenu convars.search.input", text: searchQuery, align: TextAnchor.MiddleLeft, offsetMin: "10 0", name: "Search_Inputfield");
                }

                searchPanel.AddButton(command: "adminmenu convars.pagination prev", color: "0.25 0.25 0.25 0.5", anchorMin: "0 0", anchorMax: "0 1", offsetMin: "-40 0").AddText(text: "<", font: CUI.Font.RobotoCondensedBold, align: TextAnchor.MiddleCenter);
                searchPanel.AddButton(command: "adminmenu convars.pagination next", color: "0.25 0.25 0.25 0.5", anchorMin: "1 0", anchorMax: "1 1", offsetMax: "40 0").AddText(text: ">", font: CUI.Font.RobotoCondensedBold, align: TextAnchor.MiddleCenter);
                searchPanel.AddPanel(color: "0.9 0.4 0.4 0.5", anchorMin: "0 0", anchorMax: "0 1", offsetMin: "-2 0", offsetMax: "0 0");
                searchPanel.AddPanel(color: "0.9 0.4 0.4 0.5", anchorMin: "1 0", anchorMax: "1 1", offsetMin: "0 0", offsetMax: "2 0");
            }

            public void OpenSearch(Connection connection)
            {
                CUI.Root root = new CUI.Root("Search");
                root.AddInputfield(command: "adminmenu convars.search.input", text: "", align: TextAnchor.MiddleLeft, autoFocus: true, offsetMin: "10 0", name: "Search_Inputfield").DestroyUi = "Search_Placeholder";
                root.Render(connection);
            }
        }

        public class GiveMenuContent : Content
        {
            private static readonly Label NAME_LABEL = new Label("NAME");
            private static readonly Label SKINID_LABEL = new Label("SKIN ID");
            private static readonly Label AMOUNT_LABEL = new Label("AMOUNT");
            private static readonly Label GIVE_LABEL = new Label("GIVE");
            private static readonly Label BLUEPRINT_LABEL = new Label("BLUEPRINT?");
            private static readonly Label SEARCH_LABEL = new Label("Search..");
            public override void LoadDefaultUserData(Dictionary<string, object> userData)
            {
                userData["givemenu.category"] = ItemCategory.All;
                userData["givemenu.searchQuery"] = string.Empty;
                userData["givemenu.page"] = 1;
                userData["givemenu.popup.shown"] = false;
            }

            protected override void Render(CUI.Element root, ConnectionData connectionData, Dictionary<string, object> userData)
            {
                if ((bool)userData["givemenu.popup.shown"])
                {
                    Popup(root, connectionData, userData);
                    return;
                }
                else
                {
                    root.Add(new CUI.Element { DestroyUi = "AdminMenu_GiveMenu_GivePopup" });
                }

                ItemCategory category = (ItemCategory)userData["givemenu.category"];
                string searchQuery = (string)userData["givemenu.searchQuery"];
                int page = (int)userData["givemenu.page"];
                var layout = root.AddContainer(anchorMin: "0.01 0.04", anchorMax: "0.99 0.99", name: "AdminMenu_ItemList_Layout").AddDestroySelfAttribute();
                layout.Add(new CUI.Element()).DestroyUi = "AdminMenu_GiveMenu_Give";
                int columns = 10;
                int rows = 11;
                int itemsPerPage = columns * rows;
                List<ItemDefinition> itemList = ItemManager.itemList.Where(def => (category == ItemCategory.All || def.category == category) && (string.IsNullOrEmpty(searchQuery) || def.displayName.english.ToLower().Contains(searchQuery.ToLower()) || def.shortname.ToLower().Contains(searchQuery.ToLower()))).ToList();
                int endPage = Mathf.CeilToInt(itemList.Count / (float)itemsPerPage);
                userData["givemenu.page"] = page = Mathf.Clamp(page, 1, endPage);
                if (itemList.Count > 0)
                {
                    int alreadySnownItemsCount = itemsPerPage * (page - 1);
                    itemList = itemList.GetRange(itemsPerPage * (page - 1), Mathf.Min(itemsPerPage, itemList.Count - alreadySnownItemsCount));
                }

                float width = 1f / columns;
                float height = 1f / rows;
                float panelSize = 60;
                float dividedPanelSize = panelSize / 2;
                for (int a = 0; a < rows; a++)
                {
                    for (int b = 0; b < columns; b++)
                    {
                        int index = a * columns + b;
                        ItemDefinition itemDef = itemList.ElementAtOrDefault(index);
                        if (itemDef == null)
                            break;
                        var itemButton = layout.AddContainer(anchorMin: $"{b * width} {1 - (a + 1) * height}", anchorMax: $"{(b + 1) * width} {1 - a * height}").AddButton(command: $"adminmenu givemenu.popup show {itemDef.itemid}", color: "0.25 0.25 0.25 0.6", sprite: "assets/content/ui/ui.rounded.tga", anchorMin: "0.5 0.5", anchorMax: "0.5 0.5", offsetMin: $"-{dividedPanelSize} -{dividedPanelSize}", offsetMax: $"{dividedPanelSize} {dividedPanelSize}");
                        itemButton.AddIcon(itemId: itemDef.itemid, offsetMin: "4 4", offsetMax: "-4 -4");
                    }
                }

                var bottom = root.AddPanel(color: "0.25 0.25 0.25 0.6", anchorMin: "0.02 0", anchorMax: "0.98 0.035", name: "AdminMenu_ItemList_Bottom").AddDestroySelfAttribute();
                bottom.AddText(text: $"{page}/{endPage}", font: CUI.Font.RobotoCondensedBold, align: TextAnchor.MiddleLeft, offsetMin: "10 0");
                var searchPanel = bottom.AddButton(command: "adminmenu givemenu.opensearch", color: "0.15 0.15 0.15 1", anchorMin: "0.5 0", anchorMax: "0.5 1", offsetMin: "-100 0", offsetMax: "100 0", name: "Search");
                searchPanel.AddPanel(color: "0.9 0.4 0.4 0.5", anchorMin: "0 0", anchorMax: "0 1", offsetMin: "-2 0", offsetMax: "0 0");
                searchPanel.AddPanel(color: "0.9 0.4 0.4 0.5", anchorMin: "1 0", anchorMax: "1 1", offsetMin: "0 0", offsetMax: "2 0");
                if (string.IsNullOrEmpty(searchQuery))
                {
                    searchPanel.AddText(text: SEARCH_LABEL.Localize(connectionData.connection), font: CUI.Font.RobotoCondensedBold, align: TextAnchor.MiddleCenter, offsetMin: "10 0", name: "Search_Placeholder");
                }
                else
                {
                    searchPanel.AddInputfield(command: "adminmenu givemenu.search.input", text: searchQuery, align: TextAnchor.MiddleLeft, offsetMin: "10 0", name: "Search_Inputfield");
                }

                var pagination = bottom.AddContainer(anchorMin: "1 0", anchorMax: "1 1", offsetMin: "-100 0");
                pagination.AddButton(command: "adminmenu givemenu.pagination prev", color: "0.25 0.25 0.25 0.6", anchorMin: "0 0", anchorMax: "0.45 1", offsetMin: "0 0").AddText(text: "<", font: CUI.Font.RobotoCondensedBold, align: TextAnchor.MiddleCenter);
                pagination.AddButton(command: "adminmenu givemenu.pagination next", color: "0.25 0.25 0.25 0.6", anchorMin: "0.55 0", anchorMax: "1 1", offsetMin: "0 0").AddText(text: ">", font: CUI.Font.RobotoCondensedBold, align: TextAnchor.MiddleCenter);
            }

            private void Popup(CUI.Element root, ConnectionData connectionData, Dictionary<string, object> userData)
            {
                int itemId = (int)userData["givemenu.popup.itemid"];
                ItemDefinition itemDefinition = ItemManager.FindItemDefinition(itemId);
                if (itemDefinition == null)
                    return;
                int amount = (int)userData["givemenu.popup.amount"];
                ulong skinId = (ulong)userData["givemenu.popup.skin"];
                string name = (string)userData["givemenu.popup.name"];
                bool isBluprint = (bool)userData["givemenu.popup.isblueprint"];
                var backgroundButton = root.AddButton(color: "0 0 0 0.8", command: "adminmenu givemenu.popup close", anchorMin: "0 0", anchorMax: "1 1", name: "AdminMenu_GiveMenu_GivePopup").AddDestroySelfAttribute();
                var panel = backgroundButton.AddButton(command: null, anchorMin: "0.5 0.5", anchorMax: "0.5 0.5", offsetMin: "-200 -250", offsetMax: "200 250").AddPanel(color: "0.05 0.05 0.05 1");
                var header = panel.AddPanel(color: "0.1 0.1 0.1 1", anchorMin: "0 1", anchorMax: "1 1", offsetMin: "0 -40", offsetMax: "0 0");
                header.AddText(text: itemDefinition.displayName.translated.ToUpper(), font: CUI.Font.RobotoCondensedBold, fontSize: 18, align: TextAnchor.MiddleCenter, overflow: VerticalWrapMode.Truncate);
                panel.AddIcon(itemId: itemDefinition.itemid, anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-100 -265", offsetMax: "100 -65");
                var outer = panel.AddPanel(color: "0.1 0.1 0.1 1", anchorMin: "0 0", anchorMax: "1 0", offsetMin: "0 0", offsetMax: "0 200");
                outer.AddText(text: NAME_LABEL.Localize(connectionData.connection), font: CUI.Font.RobotoCondensedBold, fontSize: 18, align: TextAnchor.MiddleCenter, overflow: VerticalWrapMode.Truncate, anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-170 -40", offsetMax: "-60 -15");
                outer.AddPanel(color: "0.05 0.05 0.05 1", anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-40 -40", offsetMax: "170 -15").AddInputfield(command: "adminmenu givemenu.popup set_name", text: name ?? itemDefinition.displayName.translated, font: CUI.Font.RobotoCondensedRegular, fontSize: 14, align: TextAnchor.MiddleLeft, offsetMin: "10 0", offsetMax: "-10 0");
                outer.AddText(text: SKINID_LABEL.Localize(connectionData.connection), font: CUI.Font.RobotoCondensedBold, fontSize: 18, align: TextAnchor.MiddleCenter, overflow: VerticalWrapMode.Truncate, anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-170 -75", offsetMax: "-60 -50");
                outer.AddPanel(color: "0.05 0.05 0.05 1", anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-40 -75", offsetMax: "170 -50").AddInputfield(command: "adminmenu givemenu.popup set_skin", text: skinId.ToString(), font: CUI.Font.RobotoCondensedRegular, fontSize: 14, align: TextAnchor.MiddleLeft, offsetMin: "10 0", offsetMax: "-10 0");
                outer.AddText(text: AMOUNT_LABEL.Localize(connectionData.connection), font: CUI.Font.RobotoCondensedBold, fontSize: 18, align: TextAnchor.MiddleCenter, overflow: VerticalWrapMode.Truncate, anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-170 -110", offsetMax: "-60 -85");
                outer.AddPanel(color: "0.05 0.05 0.05 1", anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-40 -110", offsetMax: "50 -85").AddInputfield(command: "adminmenu givemenu.popup set_amount", text: amount.ToString(), font: CUI.Font.RobotoCondensedRegular, fontSize: 14, align: TextAnchor.MiddleLeft, offsetMin: "10 0", offsetMax: "-10 0");
                outer.AddButton(color: "0.2 0.2 0.2 1", command: $"adminmenu givemenu.popup set_amount {amount + 1}", anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "55 -110", offsetMax: "78.75 -85").AddText(text: "+1", font: CUI.Font.RobotoCondensedBold, fontSize: 10, align: TextAnchor.MiddleCenter);
                outer.AddButton(color: "0.2 0.2 0.2 1", command: $"adminmenu givemenu.popup set_amount {amount + 100}", anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "83.75 -110", offsetMax: "107.5 -85").AddText(text: "+100", font: CUI.Font.RobotoCondensedBold, fontSize: 10, align: TextAnchor.MiddleCenter);
                outer.AddButton(color: "0.2 0.2 0.2 1", command: $"adminmenu givemenu.popup set_amount {amount + 1000}", anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "112.5 -110", offsetMax: "136.25 -85").AddText(text: "+1k", font: CUI.Font.RobotoCondensedBold, fontSize: 10, align: TextAnchor.MiddleCenter);
                outer.AddButton(color: "0.2 0.2 0.2 1", command: $"adminmenu givemenu.popup set_amount {amount + 10000}", anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "141.25 -110", offsetMax: "170 -85").AddText(text: "+10k", font: CUI.Font.RobotoCondensedBold, fontSize: 10, align: TextAnchor.MiddleCenter);
                outer.AddText(text: BLUEPRINT_LABEL.Localize(connectionData.connection), font: CUI.Font.RobotoCondensedBold, fontSize: 18, align: TextAnchor.MiddleCenter, overflow: VerticalWrapMode.Truncate, anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-170 -145", offsetMax: "-60 -120");
                var blueprintCheckbox = outer.AddButton(command: "adminmenu givemenu.popup isblueprint_toggle", color: "0.05 0.05 0.05 1", anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-40 -145", offsetMax: "-15 -120");
                if (isBluprint)
                {
                    blueprintCheckbox.AddPanel(color: "0.698 0.878 0.557 0.6", fadeIn: 0.2f, offsetMin: "4 4", offsetMax: "-4 -4");
                }

                outer.AddButton(color: "0.2 0.2 0.2 1", command: "adminmenu givemenu.popup give", anchorMin: "0.5 0", anchorMax: "0.5 0", offsetMin: "-80 10", offsetMax: "80 37").AddText(text: GIVE_LABEL.Localize(connectionData.connection), font: CUI.Font.RobotoCondensedBold, fontSize: 18, align: TextAnchor.MiddleCenter);
            }

            public void OpenSearch(Connection connection)
            {
                CUI.Root root = new CUI.Root("Search");
                root.AddInputfield(command: "adminmenu givemenu.search.input", text: "", align: TextAnchor.MiddleLeft, autoFocus: true, offsetMin: "10 0", name: "Search_Inputfield").DestroyUi = "Search_Placeholder";
                root.Render(connection);
            }
        }

        public class GroupInfoContent : Content
        {
            private static readonly Label GROUPNAME_LABEL = new Label("Group Name: {0}");
            private static readonly Label USERS_LABEL = new Label("Users: {0}");
            private static readonly Label REMOVECONFIRM_LABEL = new Label("Are you sure you want to <color=red>remove the group</color>?");
            private static readonly Label REMOVE_LABEL = new Label("Remove");
            private static readonly Label CANCEL_LABEL = new Label("Cancel");
            private static readonly Label CGP_CLONE_LABEL = new Label("CLONE");
            private static readonly Label CGP_CLONEGROUP_LABEL = new Label("CLONE GROUP");
            private static readonly Label CGP_NAME_LABEL = new Label("NAME <color=#bb0000>*</color>");
            private static readonly Label CGP_TITLE_LABEL = new Label("TITLE   ");
            private static readonly Label CGP_CLONEUSERS_LABEL = new Label("CLONE USERS");
            public ButtonArray[] buttons;
            public override void LoadDefaultUserData(Dictionary<string, object> userData)
            {
                userData["groupinfo[popup:clonegroup]"] = false;
            }

            protected override void Render(CUI.Element root, ConnectionData connectionData, Dictionary<string, object> userData)
            {
                if ((bool)userData["groupinfo[popup:clonegroup]"])
                {
                    CloneGroupPopup(root, connectionData, userData);
                    return;
                }
                else
                {
                    root.Add(new CUI.Element { DestroyUi = "AdminMenu_GroupInfo_CloneGroupPopup" });
                }

                if (buttons == null)
                    return;
                string connectionUserId = ((ulong)userData["userId"]).ToString();
                string groupName = userData["groupinfo.groupName"].ToString();
                if (groupName == null)
                    return;
                if (!Instance.permission.GroupExists(groupName))
                    return;
                var container = root.AddContainer(name: "AdminMenu_GroupInfo_Info").AddDestroySelfAttribute();
                var basic_info_container = container.AddContainer(anchorMin: "0 1", anchorMax: "1 1", offsetMin: "30 -180", offsetMax: "-30 -30");
                basic_info_container.AddInputfield(command: null, text: string.Format(GROUPNAME_LABEL.Localize(connectionData.connection), groupName), font: CUI.Font.RobotoCondensedBold, fontSize: 24, align: TextAnchor.MiddleLeft, anchorMin: "0 1", anchorMax: "1 1", offsetMin: "20 -30", offsetMax: "0 0");
                basic_info_container.AddText(text: string.Format(USERS_LABEL.Localize(connectionData.connection), Instance.permission.GetUsersInGroup(groupName).Length), font: CUI.Font.RobotoCondensedBold, fontSize: 18, align: TextAnchor.MiddleLeft, anchorMin: "0 1", anchorMax: "1 1", offsetMin: "20 -60", offsetMax: "0 -35");
                var actionButtonsContainer = container.AddContainer(anchorMin: "0 0", anchorMax: "1 1", offsetMin: "30 10", offsetMax: "-30 -230");
                float offset = 10f;
                for (int a = 0; a < buttons.Length; a++)
                {
                    IEnumerable<Button> rowButtons = buttons[a].GetAllowedButtons(connectionUserId);
                    for (int b = 0; b < rowButtons.Count(); b++)
                    {
                        Button button = rowButtons.ElementAtOrDefault(b);
                        if (button == null)
                            continue;
                        actionButtonsContainer.AddButton(color: "0.3 0.3 0.3 0.6", command: $"adminmenu {button.Command} {string.Join(" ", button.Args)}", anchorMin: "0 1", anchorMax: "0 1", offsetMin: $"{b * 150 + b * offset} -{(a + 1) * 35 + a * offset}", offsetMax: $"{(b + 1) * 150 + b * offset} -{a * 35 + a * offset}").AddText(text: button.Label.Localize(connectionUserId), font: CUI.Font.RobotoMonoRegular, fontSize: 12, align: TextAnchor.MiddleCenter);
                    }
                }
            }

            public void RemoveConfirmPopup(ConnectionData connectionData)
            {
                CUI.Root root = new CUI.Root("AdminMenu_Panel_TempContent");
                var backgroundButton = root.AddButton(color: "0 0 0 0.8", command: null, close: "AdminMenu_GroupInfo_RemoveConfirmPopup", anchorMin: "0 0", anchorMax: "1 1", name: "AdminMenu_GroupInfo_RemoveConfirmPopup").AddDestroySelfAttribute();
                var panel = backgroundButton.AddButton(command: null, anchorMin: "0.5 0.5", anchorMax: "0.5 0.5", offsetMin: "-200 -85", offsetMax: "200 85").AddPanel(color: "0.05 0.05 0.05 1", sprite: "assets/content/ui/ui.background.rounded.png", imageType: UnityEngine.UI.Image.Type.Tiled);
                panel.AddText(text: REMOVECONFIRM_LABEL.Localize(connectionData.connection), color: "0.8 0.8 0.8 1", font: CUI.Font.RobotoCondensedBold, fontSize: 20, align: TextAnchor.MiddleCenter, anchorMin: "0.1 0.5", anchorMax: "0.9 0.5", offsetMin: "0 -20", offsetMax: "0 40");
                panel.AddButton(command: "adminmenu groupinfo.action remove.confirmed", color: "0.749 0.243 0.243 1", anchorMin: "0.5 0", anchorMax: "0.5 0", offsetMin: "-150 20", offsetMax: "-30 50").AddText(text: REMOVE_LABEL.Localize(connectionData.connection), color: "1 0.8 0.8 1", align: TextAnchor.MiddleCenter);
                panel.AddButton(command: null, close: "AdminMenu_GroupInfo_RemoveConfirmPopup", color: "0.25 0.25 0.25 1", anchorMin: "0.5 0", anchorMax: "0.5 0", offsetMin: "30 20", offsetMax: "150 50").AddText(text: CANCEL_LABEL.Localize(connectionData.connection), color: "0.9 0.9 0.9 1", align: TextAnchor.MiddleCenter);
                root.Render(connectionData.connection);
            }

            public void LoadDefaultCloneGroupUserData(Dictionary<string, object> userData)
            {
                userData["groupinfo[popup:clonegroup].name"] = null;
                userData["groupinfo[popup:clonegroup].title"] = null;
                userData["groupinfo[popup:clonegroup].cloneusers"] = false;
            }

            private void CloneGroupPopup(CUI.Element root, ConnectionData connectionData, Dictionary<string, object> userData)
            {
                string name = (string)userData["groupinfo[popup:clonegroup].name"];
                string title = (string)userData["groupinfo[popup:clonegroup].title"];
                bool cloneUsers = (bool)userData["groupinfo[popup:clonegroup].cloneusers"];
                var backgroundButton = root.AddButton(color: "0 0 0 0.8", command: "adminmenu groupinfo[popup:clonegroup] close", anchorMin: "0 0", anchorMax: "1 1", name: "AdminMenu_GroupInfo_CloneGroupPopup").AddDestroySelfAttribute();
                var panel = backgroundButton.AddButton(command: null, anchorMin: "0.5 0.5", anchorMax: "0.5 0.5", offsetMin: "-200 -105", offsetMax: "200 105").AddPanel(color: "0.05 0.05 0.05 1");
                var header = panel.AddPanel(color: "0.1 0.1 0.1 1", anchorMin: "0 1", anchorMax: "1 1", offsetMin: "0 -30", offsetMax: "0 0");
                header.AddText(text: CGP_CLONEGROUP_LABEL.Localize(connectionData.connection), color: "0.85 0.85 0.85 0.8", font: CUI.Font.RobotoCondensedRegular, fontSize: 14, align: TextAnchor.MiddleCenter);
                panel.AddText(text: CGP_NAME_LABEL.Localize(connectionData.connection), color: "0.9 0.9 0.9 0.8", font: CUI.Font.RobotoCondensedBold, fontSize: 16, align: TextAnchor.MiddleCenter, anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-170 -80", offsetMax: "-60 -55");
                panel.AddPanel(color: "0.1 0.1 0.1 1", anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-40 -80", offsetMax: "170 -55").AddInputfield(command: "adminmenu groupinfo[popup:clonegroup] set_name", text: name ?? string.Empty, font: CUI.Font.RobotoCondensedRegular, fontSize: 14, align: TextAnchor.MiddleLeft, offsetMin: "10 0", offsetMax: "-10 0");
                panel.AddText(text: CGP_TITLE_LABEL.Localize(connectionData.connection), color: "0.9 0.9 0.9 0.8", font: CUI.Font.RobotoCondensedBold, fontSize: 16, align: TextAnchor.MiddleCenter, anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-170 -120", offsetMax: "-60 -95");
                panel.AddPanel(color: "0.1 0.1 0.1 1", anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-40 -120", offsetMax: "170 -95").AddInputfield(command: "adminmenu groupinfo[popup:clonegroup] set_title", text: title ?? string.Empty, font: CUI.Font.RobotoCondensedRegular, fontSize: 14, align: TextAnchor.MiddleLeft, offsetMin: "10 0", offsetMax: "-10 0");
                panel.AddText(text: CGP_CLONEUSERS_LABEL.Localize(connectionData.connection), color: "0.9 0.9 0.9 0.8", font: CUI.Font.RobotoCondensedBold, fontSize: 16, align: TextAnchor.MiddleCenter, anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-170 -160", offsetMax: "-60 -135");
                var cloneUsersCheckbox = panel.AddButton(command: "adminmenu groupinfo[popup:clonegroup] cloneusers_toggle", color: "0.1 0.1 0.1 1", anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-40 -160", offsetMax: "-15 -135");
                if (cloneUsers)
                {
                    cloneUsersCheckbox.AddPanel(color: "0.698 0.878 0.557 0.6", fadeIn: 0.2f, offsetMin: "4 4", offsetMax: "-4 -4");
                }

                panel.AddButton(color: "0.1 0.1 0.1 1", command: "adminmenu groupinfo[popup:clonegroup] clone", anchorMin: "0.5 0", anchorMax: "0.5 0", offsetMin: "-80 10", offsetMax: "80 37").AddText(text: CGP_CLONE_LABEL.Localize(connectionData.connection), color: "0.8 0.8 0.8 0.8", font: CUI.Font.RobotoCondensedBold, fontSize: 18, align: TextAnchor.MiddleCenter);
            }
        }

        public class GroupListContent : Content
        {
            private static readonly Label CREATEGROUP_LABEL = new Label("CREATE GROUP");
            private static readonly Label CGP_NAME_LABEL = new Label("NAME <color=#bb0000>*</color>");
            private static readonly Label CGP_TITLE_LABEL = new Label("TITLE   ");
            private static readonly Label CGP_CREATE_LABEL = new Label("CREATE");
            public override void LoadDefaultUserData(Dictionary<string, object> userData)
            {
                userData["grouplist[popup:creategroup]"] = false;
            }

            protected override void Render(CUI.Element root, ConnectionData connectionData, Dictionary<string, object> userData)
            {
                if ((bool)userData["grouplist[popup:creategroup]"])
                {
                    CreateGroupPopup(root, connectionData, userData);
                    return;
                }
                else
                {
                    root.Add(new CUI.Element { DestroyUi = "AdminMenu_GroupList_СreateGroupPopup" });
                }

                string[] groups = Instance.permission.GetGroups();
                var layout = root.AddContainer(anchorMin: "0.01 0.04", anchorMax: "0.99 0.99", name: "AdminMenu_GroupList_Layout").AddDestroySelfAttribute();
                int columns = 4;
                int rows = 16;
                float width = 1f / columns;
                float height = 1f / rows;
                for (int a = 0; a < rows; a++)
                {
                    for (int b = 0; b < columns; b++)
                    {
                        int index = a * columns + b;
                        if (index >= groups.Length)
                            break;
                        string group = groups[index];
                        var container = layout.AddContainer(anchorMin: $"{b * width} {1 - (a + 1) * height}", anchorMax: $"{(b + 1) * width} {1 - a * height}");
                        var button = container.AddButton(command: $"adminmenu groupinfo.open {group}", color: "0.25 0.25 0.25 0.6", anchorMin: "0.5 0.5", anchorMax: "0.5 0.5", offsetMin: $"-73 -20", offsetMax: $"73 20");
                        button.AddText(text: group, fontSize: 12, align: TextAnchor.MiddleCenter, offsetMin: "6 0", offsetMax: "-6 0");
                    }
                }

                root.AddButton(command: "adminmenu grouplist[popup:creategroup] show", color: "0.25 0.25 0.25 0.6", anchorMin: "0 0", anchorMax: "1 0.035").AddText(text: CREATEGROUP_LABEL.Localize(connectionData.connection), fontSize: 12, align: TextAnchor.MiddleCenter, offsetMin: "6 0", offsetMax: "-6 0");
            }

            private void CreateGroupPopup(CUI.Element root, ConnectionData connectionData, Dictionary<string, object> userData)
            {
                string name = (string)userData["grouplist[popup:creategroup].name"];
                string title = (string)userData["grouplist[popup:creategroup].title"];
                var backgroundButton = root.AddButton(color: "0 0 0 0.8", command: "adminmenu grouplist[popup:creategroup] close", anchorMin: "0 0", anchorMax: "1 1", name: "AdminMenu_GroupList_СreateGroupPopup").AddDestroySelfAttribute();
                var panel = backgroundButton.AddButton(command: null, anchorMin: "0.5 0.5", anchorMax: "0.5 0.5", offsetMin: "-200 -85", offsetMax: "200 85").AddPanel(color: "0.05 0.05 0.05 1");
                var header = panel.AddPanel(color: "0.1 0.1 0.1 1", anchorMin: "0 1", anchorMax: "1 1", offsetMin: "0 -30", offsetMax: "0 0");
                header.AddText(text: CREATEGROUP_LABEL.Localize(connectionData.connection), color: "0.85 0.85 0.85 0.8", font: CUI.Font.RobotoCondensedRegular, fontSize: 14, align: TextAnchor.MiddleCenter);
                panel.AddText(text: CGP_NAME_LABEL.Localize(connectionData.connection), color: "0.9 0.9 0.9 0.8", font: CUI.Font.RobotoCondensedBold, fontSize: 16, align: TextAnchor.MiddleCenter, anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-170 -80", offsetMax: "-60 -55");
                panel.AddPanel(color: "0.1 0.1 0.1 1", anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-40 -80", offsetMax: "170 -55").AddInputfield(command: "adminmenu grouplist[popup:creategroup] set_name", text: name ?? string.Empty, font: CUI.Font.RobotoCondensedRegular, fontSize: 14, align: TextAnchor.MiddleLeft, offsetMin: "10 0", offsetMax: "-10 0");
                panel.AddText(text: CGP_TITLE_LABEL.Localize(connectionData.connection), color: "0.9 0.9 0.9 0.8", font: CUI.Font.RobotoCondensedBold, fontSize: 16, align: TextAnchor.MiddleCenter, anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-170 -120", offsetMax: "-60 -95");
                panel.AddPanel(color: "0.1 0.1 0.1 1", anchorMin: "0.5 1", anchorMax: "0.5 1", offsetMin: "-40 -120", offsetMax: "170 -95").AddInputfield(command: "adminmenu grouplist[popup:creategroup] set_title", text: title ?? string.Empty, font: CUI.Font.RobotoCondensedRegular, fontSize: 14, align: TextAnchor.MiddleLeft, offsetMin: "10 0", offsetMax: "-10 0");
                panel.AddButton(color: "0.1 0.1 0.1 1", command: "adminmenu grouplist[popup:creategroup] create", anchorMin: "0.5 0", anchorMax: "0.5 0", offsetMin: "-80 10", offsetMax: "80 37").AddText(text: CGP_CREATE_LABEL.Localize(connectionData.connection), color: "0.8 0.8 0.8 0.8", font: CUI.Font.RobotoCondensedBold, fontSize: 18, align: TextAnchor.MiddleCenter);
            }
        }

        public class PermissionsContent : Content
        {
            public Plugin plugin;
            public string[] permissions;
            protected override void Render(CUI.Element root, ConnectionData connectionData, Dictionary<string, object> userData)
            {
                if (permissions == null)
                    return;
                string type = userData["permissions.target_type"].ToString();
                string target = userData["permissions.target"].ToString();
                if (type == null || target == null)
                    return;
                bool isTargetUser;
                switch (type)
                {
                    case "user":
                        isTargetUser = true;
                        break;
                    case "group":
                        isTargetUser = false;
                        break;
                    default:
                        return;
                }

                string userColor = "0.5 0.7 0.4 1";
                string groupColor = "0.3 0.6 0.7 1";
                var layout = root.AddContainer(anchorMin: "0.01 0.01", anchorMax: "0.99 0.99", name: "AdminMenu_PermissionList_Layout").AddDestroySelfAttribute();
                int columns = 3;
                int rows = 16;
                float width = 1f / columns;
                float height = 1f / rows;
                float panelSize = width;
                float dividedPanelSize = panelSize / 2;
                for (int a = 0; a < rows; a++)
                {
                    for (int b = 0; b < columns; b++)
                    {
                        int index = a * columns + b;
                        if (index >= permissions.Length)
                            break;
                        string permission = permissions[index];
                        bool hasUser = false;
                        bool hasGroup = false;
                        if (isTargetUser)
                        {
                            var permUserData = Instance.permission.GetUserData(target);
                            if (permUserData.Perms.Contains(permission, StringComparer.OrdinalIgnoreCase))
                            {
                                hasUser = true;
                            }
                            else if (Instance.permission.GroupsHavePermission(permUserData.Groups, permission))
                            {
                                hasGroup = true;
                            }
                        }
                        else
                        {
                            hasGroup = Instance.permission.GroupHasPermission(target, permission);
                        }

                        var container = layout.AddContainer(anchorMin: $"{b * width} {1 - (a + 1) * height}", anchorMax: $"{(b + 1) * width} {1 - a * height}");
                        var button = container.AddButton(command: $"adminmenu permission.action {(isTargetUser && !hasUser || !isTargetUser && !hasGroup ? "grant" : "revoke")} {permission}", color: "0.25 0.25 0.25 0.6", anchorMin: "0.5 0.5", anchorMax: "0.5 0.5", offsetMin: $"-98 -20", offsetMax: $"98 20");
                        if (hasUser || hasGroup)
                        {
                            string color = hasGroup ? groupColor : userColor;
                            button.AddPanel(color: color, anchorMin: "0 0", anchorMax: "0 1", offsetMin: "0 0", offsetMax: "1.5 0");
                            button.AddPanel(color: color, anchorMin: "1 0", anchorMax: "1 1", offsetMin: "-1.5 0", offsetMax: "0 0");
                            button.AddPanel(color: color, anchorMin: "0 0", anchorMax: "1 0", offsetMin: "0 0", offsetMax: "0 1.5");
                            button.AddPanel(color: color, anchorMin: "0 1", anchorMax: "1 1", offsetMin: "0 -1.5", offsetMax: "0 0");
                        }

                        button.AddText(text: permission, fontSize: 12, align: TextAnchor.MiddleCenter, offsetMin: "6 0", offsetMax: "-6 0");
                    }
                }
            }

            public override void LoadDefaultUserData(Dictionary<string, object> userData)
            {
            }
        }

        public class PlayerListContent : Content
        {
            private static readonly Label SEARCH_LABEL = new Label("Search..");
            public override void LoadDefaultUserData(Dictionary<string, object> userData)
            {
                if (!userData.ContainsKey("playerlist.filter"))
                    userData["playerlist.filter"] = (Func<IPlayer, bool>)((IPlayer player) => true);
                userData["playerlist.searchQuery"] = string.Empty;
                userData["playerlist.page"] = 1;
            }

            public override void RestoreUserData(Dictionary<string, object> userData)
            {
                userData["playerlist.filter"] = (Func<IPlayer, bool>)((IPlayer player) => true);
            }

            protected override void Render(CUI.Element root, ConnectionData connectionData, Dictionary<string, object> userData)
            {
                Func<IPlayer, bool> filter = (Func<IPlayer, bool>)userData["playerlist.filter"];
                string searchQuery = (string)userData["playerlist.searchQuery"];
                int page = (int)userData["playerlist.page"];
                int columns = 4;
                int rows = 16;
                int playersPerPage = columns * rows;
                List<IPlayer> players = Instance.covalence.Players.All.Where(filter).ToList();
                if (!string.IsNullOrEmpty(searchQuery))
                    players = players.Where(player => player.Name.ToLower().Contains(searchQuery.ToLower()) || player.Id == searchQuery).ToList();
                int endPage = Mathf.CeilToInt(players.Count / (float)playersPerPage);
                userData["playerlist.page"] = page = Mathf.Clamp(page, 1, endPage);
                if (players.Count > 0)
                {
                    int alreadySnownPlayersCount = playersPerPage * (page - 1);
                    players = players.GetRange(playersPerPage * (page - 1), Mathf.Min(playersPerPage, players.Count - alreadySnownPlayersCount));
                }

                players = players.OrderBy(p => p.Name).ToList();
                int playersCount = players.Count;
                var layout = root.AddContainer(anchorMin: "0.01 0.04", anchorMax: "0.99 0.99", name: "AdminMenu_PlayerList_Layout").AddDestroySelfAttribute();
                float width = 1f / columns;
                float height = 1f / rows;
                for (int a = 0; a < rows; a++)
                {
                    for (int b = 0; b < columns; b++)
                    {
                        int index = a * columns + b;
                        IPlayer player = players.ElementAtOrDefault(index);
                        if (player == null)
                            break;
                        var container = layout.AddContainer(anchorMin: $"{b * width} {1 - (a + 1) * height}", anchorMax: $"{(b + 1) * width} {1 - a * height}");
                        var button = container.AddButton(command: $"adminmenu userinfo.open {player.Id}", color: "0.25 0.25 0.25 0.6", anchorMin: "0.5 0.5", anchorMax: "0.5 0.5", offsetMin: $"-73 -20", offsetMax: $"73 20");
                        string frameColor = null;
                        var serverUser = ServerUsers.Get(ulong.Parse(player.Id));
                        if (serverUser != null)
                        {
                            switch (serverUser.group)
                            {
                                case ServerUsers.UserGroup.Owner:
                                    frameColor = "0.8 0.2 0.2 0.6";
                                    break;
                                case ServerUsers.UserGroup.Moderator:
                                    frameColor = "1 0.6 0.3 0.6";
                                    break;
                                case ServerUsers.UserGroup.Banned:
                                    frameColor = "0 0 0 1";
                                    break;
                            }
                        }

                        if (frameColor != null)
                        {
                            button.AddPanel(color: frameColor, anchorMin: "0 0", anchorMax: "0 1", offsetMin: "0 0", offsetMax: "1.5 0");
                            button.AddPanel(color: frameColor, anchorMin: "1 0", anchorMax: "1 1", offsetMin: "-1.5 0", offsetMax: "0 0");
                            button.AddPanel(color: frameColor, anchorMin: "0 0", anchorMax: "1 0", offsetMin: "0 0", offsetMax: "0 1.5");
                            button.AddPanel(color: frameColor, anchorMin: "0 1", anchorMax: "1 1", offsetMin: "0 -1.5", offsetMax: "0 0");
                        }

                        button.AddText(text: player.Name, fontSize: 12, align: TextAnchor.MiddleCenter, offsetMin: "6 0", offsetMax: "-6 0");
                    }
                }

                var bottom = root.AddPanel(color: "0.20 0.20 0.20 0.6", anchorMin: "0.02 0", anchorMax: "0.98 0.035", name: "AdminMenu_PlayerList_Bottom").AddDestroySelfAttribute();
                bottom.AddText(text: $"{page}/{endPage}", font: CUI.Font.RobotoCondensedBold, align: TextAnchor.MiddleLeft, offsetMin: "10 0");
                var searchPanel = bottom.AddButton(command: "adminmenu playerlist.opensearch", color: "0.15 0.15 0.15 1", anchorMin: "0.5 0", anchorMax: "0.5 1", offsetMin: "-100 0", offsetMax: "100 0", name: "Search");
                searchPanel.AddPanel(color: "0.9 0.4 0.4 0.5", anchorMin: "0 0", anchorMax: "0 1", offsetMin: "-2 0", offsetMax: "0 0");
                searchPanel.AddPanel(color: "0.9 0.4 0.4 0.5", anchorMin: "1 0", anchorMax: "1 1", offsetMin: "0 0", offsetMax: "2 0");
                if (string.IsNullOrEmpty(searchQuery))
                {
                    searchPanel.AddText(text: SEARCH_LABEL.Localize(connectionData.connection), font: CUI.Font.RobotoCondensedBold, align: TextAnchor.MiddleCenter, offsetMin: "10 0", name: "Search_Placeholder");
                }
                else
                {
                    searchPanel.AddInputfield(command: "adminmenu playerlist.search.input", text: searchQuery, align: TextAnchor.MiddleLeft, offsetMin: "10 0", name: "Search_Inputfield");
                }

                var pagination = bottom.AddContainer(anchorMin: "1 0", anchorMax: "1 1", offsetMin: "-100 0");
                pagination.AddButton(command: "adminmenu playerlist.pagination prev", color: "0.25 0.25 0.25 0.6", anchorMin: "0 0", anchorMax: "0.45 1", offsetMin: "0 0").AddText(text: "<", font: CUI.Font.RobotoCondensedBold, align: TextAnchor.MiddleCenter);
                pagination.AddButton(command: "adminmenu playerlist.pagination next", color: "0.25 0.25 0.25 0.6", anchorMin: "0.55 0", anchorMax: "1 1", offsetMin: "0 0").AddText(text: ">", font: CUI.Font.RobotoCondensedBold, align: TextAnchor.MiddleCenter);
            }

            public void OpenSearch(Connection connection)
            {
                CUI.Root root = new CUI.Root("Search");
                root.AddInputfield(command: "adminmenu playerlist.search.input", text: "", align: TextAnchor.MiddleLeft, autoFocus: true, offsetMin: "10 0", name: "Search_Inputfield").DestroyUi = "Search_Placeholder";
                root.Render(connection);
            }
        }

        public class PluginManagerContent : Content
        {
            private static readonly MethodInfo GetPluginMethod = typeof(PluginLoader).GetMethod("GetPlugin", (BindingFlags)(-1));
            private static Button LOAD_BUTTON = new Button("Load", "0.451 0.737 0.349 1", "pluginmanager.load")
            {
                Permission = "pluginmanager.load"
            };
            private static Button UNLOAD_BUTTON = new Button("Unload", "0.737 0.353 0.349 1", "pluginmanager.unload")
            {
                Permission = "pluginmanager.unload"
            };
            private static Button RELOAD_BUTTON = new Button("Reload", "0.455 0.667 0.737 1", "pluginmanager.reload")
            {
                Permission = "pluginmanager.reload"
            };
            private static readonly Label SEARCH_LABEL = new Label("Search..");
            public override void LoadDefaultUserData(Dictionary<string, object> userData)
            {
                userData["pluginmanager.searchQuery"] = string.Empty;
                userData["pluginmanager.page"] = 1;
            }

            protected override void Render(CUI.Element root, ConnectionData connectionData, Dictionary<string, object> userData)
            {
                string searchQuery = (string)userData["pluginmanager.searchQuery"];
                int page = (int)userData["pluginmanager.page"];
                string lastUsedPluginName = null;
                if (userData.TryGetValue("pluginmanager.lastusedplugin", out object obj))
                    lastUsedPluginName = obj as string;
                var container = root.AddContainer(anchorMin: "0.01 0.045", anchorMax: "0.99 0.99", name: "AdminMenu_PluginManager").AddDestroySelfAttribute();
                int rows = 15;
                IEnumerable<FileInfo> enumerable =
                    from f in new DirectoryInfo(Core.Interface.Oxide.PluginDirectory).GetFiles("*.cs")
                    where (f.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden
                    select f;
                List<string> pluginNames = enumerable.Select(fileInfo => Path.GetFileNameWithoutExtension(fileInfo.FullName)).Where(name => name.ToLower().Contains(searchQuery.ToLower())).ToList();
                int endPage = Mathf.CeilToInt(pluginNames.Count / (float)rows);
                userData["pluginmanager.page"] = page = Mathf.Clamp(page, 1, endPage);
                if (pluginNames.Count > 0)
                {
                    int alreadySnownCount = rows * (page - 1);
                    pluginNames = pluginNames.ToList().GetRange(rows * (page - 1), Mathf.Min(rows, pluginNames.Count - alreadySnownCount));
                }

                float width = 1f / rows;
                for (int i = 0; i < rows; i++)
                {
                    string pluginName = pluginNames.ElementAtOrDefault(i);
                    if (pluginName == null)
                        return;
                    Plugin plugin = Core.Interface.Oxide.RootPluginManager.GetPlugin(pluginName);
                    var convarContainer = container.AddContainer(anchorMin: $"0 {1f - width * (i + 1)}", anchorMax: $"1 {1f - width * i}");
                    var panel = convarContainer.AddPanel(color: (pluginName == lastUsedPluginName ? "0.1 0.1 0.1 1" : "0.25 0.25 0.25 0.4"), sprite: "assets/content/ui/ui.background.rounded.png", imageType: UnityEngine.UI.Image.Type.Tiled, anchorMin: $"0 0.045", anchorMax: $"1 0.955");
                    panel.AddText(text: $"<b>{(plugin != null ? $"{plugin.Name} v{plugin.Version}" : pluginName)}</b>{(plugin != null && !string.IsNullOrEmpty(plugin.Description) ? $"\n<color=#7A7A7A><size=11>{plugin.Description}</size></color>" : string.Empty)}", align: TextAnchor.MiddleLeft, anchorMin: "0.01 0", anchorMax: "0.7 1");
                    List<Button> buttonsToRender = new List<Button>();
                    if (plugin != null && plugin.IsLoaded)
                    {
                        if (UNLOAD_BUTTON.UserHasPermission(connectionData.connection))
                            buttonsToRender.Add(UNLOAD_BUTTON);
                        if (RELOAD_BUTTON.UserHasPermission(connectionData.connection))
                            buttonsToRender.Add(RELOAD_BUTTON);
                    }
                    else
                    {
                        if (LOAD_BUTTON.UserHasPermission(connectionData.connection))
                            buttonsToRender.Add(LOAD_BUTTON);
                    }

                    for (int j = 0; j < buttonsToRender.Count; j++)
                    {
                        Button button = buttonsToRender[j];
                        panel.AddButton(command: $"adminmenu {button.Command} {pluginName}", sprite: "assets/content/ui/ui.background.rounded.png", imageType: UnityEngine.UI.Image.Type.Tiled, color: button.BackgroundColor, anchorMin: "0.99 0.1", anchorMax: "0.99 0.9", offsetMin: $"-{(j + 1) * 100 + j * 5} 0", offsetMax: $"-{j * 100 + j * 5} 0").AddText(text: button.Label.Localize(connectionData.connection), align: TextAnchor.MiddleCenter, offsetMin: "10 0", offsetMax: "-10 0");
                    }
                }

                var bottom = root.AddContainer(anchorMin: "0.02 0", anchorMax: "0.98 0.035", name: "AdminMenu_PluginManager_Bottom").AddDestroySelfAttribute();
                var searchPanel = bottom.AddButton(command: "adminmenu pluginmanager.opensearch", color: "0.15 0.15 0.15 0.7", anchorMin: "0.5 0", anchorMax: "0.5 1", offsetMin: "-100 0", offsetMax: "100 0", name: "Search");
                if (string.IsNullOrEmpty(searchQuery))
                {
                    searchPanel.AddText(text: SEARCH_LABEL.Localize(connectionData.connection), font: CUI.Font.RobotoCondensedBold, align: TextAnchor.MiddleCenter, offsetMin: "10 0", name: "Search_Placeholder");
                }
                else
                {
                    searchPanel.AddInputfield(command: "adminmenu pluginmanager.search.input", text: searchQuery, align: TextAnchor.MiddleLeft, offsetMin: "10 0", name: "Search_Inputfield");
                }

                searchPanel.AddButton(command: "adminmenu pluginmanager.pagination prev", color: "0.25 0.25 0.25 0.5", anchorMin: "0 0", anchorMax: "0 1", offsetMin: "-40 0").AddText(text: "<", font: CUI.Font.RobotoCondensedBold, align: TextAnchor.MiddleCenter);
                searchPanel.AddButton(command: "adminmenu pluginmanager.pagination next", color: "0.25 0.25 0.25 0.5", anchorMin: "1 0", anchorMax: "1 1", offsetMax: "40 0").AddText(text: ">", font: CUI.Font.RobotoCondensedBold, align: TextAnchor.MiddleCenter);
                searchPanel.AddPanel(color: "0.9 0.4 0.4 0.5", anchorMin: "0 0", anchorMax: "0 1", offsetMin: "-2 0", offsetMax: "0 0");
                searchPanel.AddPanel(color: "0.9 0.4 0.4 0.5", anchorMin: "1 0", anchorMax: "1 1", offsetMin: "0 0", offsetMax: "2 0");
            }

            public void OpenSearch(Connection connection)
            {
                CUI.Root root = new CUI.Root("Search");
                root.AddInputfield(command: "adminmenu pluginmanager.search.input", text: "", align: TextAnchor.MiddleLeft, autoFocus: true, offsetMin: "10 0", name: "Search_Inputfield").DestroyUi = "Search_Placeholder";
                root.Render(connection);
            }
        }

        public class QuickMenuContent : Content
        {
            public ButtonArray[] buttons;
            protected override void Render(CUI.Element root, ConnectionData connectionData, Dictionary<string, object> userData)
            {
                if (this.buttons == null)
                    return;
                string connectionUserId = ((ulong)userData["userId"]).ToString();
                var container = root.AddContainer(anchorMin: "0.01 0.01", anchorMax: "0.99 0.99", name: "AdminMenu_QuickMenu").AddDestroySelfAttribute();
                float offset = 10f;
                List<IEnumerable<Button>> buttons = new List<IEnumerable<Button>>();
                foreach (ButtonArray buttonArray in this.buttons)
                {
                    IEnumerable<Button> rowButtons = buttonArray.GetAllowedButtons(connectionUserId);
                    if (buttonArray.Count > 0 && rowButtons.Count() == 0)
                        continue;
                    buttons.Add(rowButtons);
                }

                for (int a = 0; a < buttons.Count; a++)
                {
                    IEnumerable<Button> rowButtons = buttons.ElementAt(a);
                    for (int b = 0; b < rowButtons.Count(); b++)
                    {
                        Button button = rowButtons.ElementAtOrDefault(b);
                        if (button == null)
                            continue;
                        container.AddButton(color: "0.3 0.3 0.3 0.6", command: $"adminmenu {button.Command} {string.Join(" ", button.Args)}", anchorMin: "0 1", anchorMax: "0 1", offsetMin: $"{b * 150 + b * offset} -{(a + 1) * 35 + a * offset}", offsetMax: $"{(b + 1) * 150 + b * offset} -{a * 35 + a * offset}").AddText(text: button.Label.Localize(connectionUserId), font: CUI.Font.RobotoMonoRegular, fontSize: 12, align: TextAnchor.MiddleCenter);
                    }
                }
            }
        }

        public class UserGroupsContent : Content
        {
            protected override void Render(CUI.Element root, ConnectionData connectionData, Dictionary<string, object> userData)
            {
                string userId = userData["userinfo.userid"].ToString();
                if (userId == null)
                    return;
                string[] groups = Instance.permission.GetGroups();
                var layout = root.AddContainer(anchorMin: "0.01 0.01", anchorMax: "0.99 0.99", name: "AdminMenu_UserGroups_Layout").AddDestroySelfAttribute();
                int columns = 4;
                int rows = 16;
                float width = 1f / columns;
                float height = 1f / rows;
                for (int a = 0; a < rows; a++)
                {
                    for (int b = 0; b < columns; b++)
                    {
                        int index = a * columns + b;
                        if (index >= groups.Length)
                            break;
                        string group = groups[index];
                        var container = layout.AddContainer(anchorMin: $"{b * width} {1 - (a + 1) * height}", anchorMax: $"{(b + 1) * width} {1 - a * height}");
                        bool hasGroup = Instance.permission.UserHasGroup(userId, group);
                        var button = container.AddButton(command: $"adminmenu usergroups.action {(!hasGroup ? "grant" : "revoke")} {group}", color: "0.25 0.25 0.25 0.6", anchorMin: "0.5 0.5", anchorMax: "0.5 0.5", offsetMin: $"-73 -20", offsetMax: $"73 20");
                        if (hasGroup)
                        {
                            string color = "0.3 0.6 0.7 1";
                            button.AddPanel(color: color, anchorMin: "0 0", anchorMax: "0 1", offsetMin: "0 0", offsetMax: "1.5 0");
                            button.AddPanel(color: color, anchorMin: "1 0", anchorMax: "1 1", offsetMin: "-1.5 0", offsetMax: "0 0");
                            button.AddPanel(color: color, anchorMin: "0 0", anchorMax: "1 0", offsetMin: "0 0", offsetMax: "0 1.5");
                            button.AddPanel(color: color, anchorMin: "0 1", anchorMax: "1 1", offsetMin: "0 -1.5", offsetMax: "0 0");
                        }

                        button.AddText(text: group, fontSize: 12, align: TextAnchor.MiddleCenter, offsetMin: "6 0", offsetMax: "-6 0");
                    }
                }
            }
        }

        public class UserInfoContent : Content
        {
            private static readonly Regex AvatarFullRegex = new Regex(@"<avatarFull><!\[CDATA\[(.*)\]\]></avatarFull>");
            private static readonly Label HEALTH_LABEL = new Label("Health: {0}/{1}");
            private static readonly Label GRID_LABEL = new Label("Grid: {0}");
            private static readonly Label CONNECTIONTIME_LABEL = new Label("CTIME: {0:D2}h {1:D2}m {2:D2}s");
            private static readonly Label BALANCE_LABEL = new Label("Balance: {0}$");
            private static readonly Label CLAN_LABEL = new Label("Clan: {0}");
            public ButtonArray[] buttons;
            protected override void Render(CUI.Element root, ConnectionData connectionData, Dictionary<string, object> userData)
            {
                if (buttons == null)
                    return;
                string connectionUserId = ((ulong)userData["userId"]).ToString();
                string userid = userData["userinfo.userid"].ToString();
                if (userid == null)
                    return;
                IPlayer player = Instance.covalence.Players.FindPlayerById(userid);
                if (player == null)
                    return;
                ulong playerIdUlong = ulong.Parse(player.Id);
                BasePlayer playerInWorld = BasePlayer.FindAwakeOrSleeping(userid);
                Vector3 position = Vector3.zero;
                float health = 0f;
                float maxHealth = 0f;
                bool isMuted = false;
                if (playerInWorld != null)
                {
                    position = playerInWorld.transform.position;
                    health = playerInWorld.health;
                    maxHealth = playerInWorld.MaxHealth();
                    isMuted = playerInWorld.HasPlayerFlag(BasePlayer.PlayerFlags.ChatMute);
                }

                var container = root.AddContainer(name: "AdminMenu_UserInfo_Info").AddDestroySelfAttribute();
                var basic_info_container = container.AddContainer(anchorMin: "0 1", anchorMax: "1 1", offsetMin: "30 -180", offsetMax: "-30 -30");
                var avatarContainer = basic_info_container.AddContainer(anchorMin: "0 0", anchorMax: "0 0", offsetMin: "0 0", offsetMax: "150 150", name: "AdminMenu_UserInfo_AvatarContainer");
                string avatar = Instance.ImageLibrary?.Call<string>("GetImage", player.Id, 1080UL);
                if (avatar != null)
                {
                    avatarContainer.AddImage(content: avatar, color: "1 1 1 1", name: "AdminMenu_UserInfo_Avatar").AddDestroySelfAttribute();
                }
                else
                {
                    avatarContainer.AddPanel(color: "0.3 0.3 0.3 0.5", name: "AdminMenu_UserInfo_Avatar").AddDestroySelfAttribute().AddOutlinedText(text: "NO\nAVATAR", outlineWidth: 2f, outlineColor: "0 0 0 0.5", font: CUI.Font.RobotoCondensedBold, fontSize: 25, align: TextAnchor.MiddleCenter);
                    if (playerIdUlong.IsSteamId() && Instance.ImageLibrary != null)
                    {
                        Instance.webrequest.Enqueue($"http://steamcommunity.com/profiles/{player.Id}?xml=1", null, (code, response) =>
                        {
                            if (code != 200 || response == null)
                                return;
                            var avatarUrl = AvatarFullRegex.Match(response).Groups[1].ToString();
                            if (string.IsNullOrEmpty(avatarUrl))
                                return;
                            Instance.ImageLibrary.Call("AddImage", avatarUrl, player.Id, 1080UL, new Action(() => Render(connectionData)));
                        }, Instance);
                    }
                }

                string name = player.Name;
                var serverUser = ServerUsers.Get(playerIdUlong);
                if (serverUser != null)
                {
                    switch (serverUser.group)
                    {
                        case ServerUsers.UserGroup.Owner:
                            name = "[Admin] " + name;
                            break;
                        case ServerUsers.UserGroup.Moderator:
                            name = "[Moderator] " + name;
                            break;
                        case ServerUsers.UserGroup.Banned:
                            name = "[Banned] " + name;
                            break;
                    }
                }

                var text_info_container = basic_info_container.AddContainer(anchorMin: "0 0", anchorMax: "1 1", offsetMin: "180 0");
                text_info_container.AddPanel(color: "1 1 1 1", sprite: $"assets/icons/flags/{Instance.lang.GetLanguage(player.Id)}.png", material: "assets/content/ui/namefontmaterial.mat", anchorMin: "0 1", anchorMax: "0 1", offsetMin: $"0 -30", offsetMax: $"35 0");
                text_info_container.AddInputfield(command: null, text: name, font: CUI.Font.RobotoCondensedBold, fontSize: 24, align: TextAnchor.MiddleLeft, anchorMin: "0 1", anchorMax: "1 1", offsetMin: "40 -30", offsetMax: "0 0");
                text_info_container.AddText(text: "Steam ID:", font: CUI.Font.RobotoCondensedBold, fontSize: 18, align: TextAnchor.MiddleLeft, anchorMin: "0 1", anchorMax: "1 1", offsetMin: "0 -60", offsetMax: "0 -35");
                text_info_container.AddInputfield(command: "", color: "1 1 1 1", text: player.Id, font: CUI.Font.RobotoCondensedBold, fontSize: 18, align: TextAnchor.MiddleLeft, anchorMin: "0 1", anchorMax: "1 1", offsetMin: "90 -60", offsetMax: "0 -35");
                List<string> leftColumn = new List<string>
                {
                    string.Format(HEALTH_LABEL.Localize(connectionUserId), Mathf.Round(health), Mathf.Round(maxHealth)),
                    string.Format(GRID_LABEL.Localize(connectionUserId), (position != Vector3.zero ? PhoneController.PositionToGridCoord(position) : "Unknown")),
                    string.Format("{0} (P: {1})", player.Address ?? "not connected", (player.IsConnected ? player.Ping : -1))
                };
                List<string> rightColumn = new List<string>();
                if (Instance.Economics || Instance.ServerRewards)
                {
                    double? balance = null;
                    if (Instance.Economics)
                    {
                        balance = Instance.Economics.Call<double>("Balance", new object[] { playerIdUlong });
                    }
                    else if (Instance.ServerRewards)
                    {
                        object points = Instance.ServerRewards.Call("Balance", new object[] { playerIdUlong });
                        if (points is int)
                            balance = (int)points;
                    }

                    if (balance.HasValue)
                        rightColumn.Add(string.Format(BALANCE_LABEL.Localize(connectionUserId), balance.Value));
                }

                if (Instance.Clans != null)
                {
                    string clanTag = Instance.Clans.Call<string>("GetClanOf", userid);
                    if (!string.IsNullOrEmpty(clanTag))
                        rightColumn.Add(string.Format(CLAN_LABEL.Localize(connectionUserId), clanTag));
                }

                if (playerInWorld != null && playerInWorld.IsConnected)
                {
                    TimeSpan timeSpan = TimeSpan.FromSeconds(playerInWorld.Connection.GetSecondsConnected());
                    rightColumn.Add(string.Format(CONNECTIONTIME_LABEL.Localize(connectionUserId), timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds));
                }

                for (int i = 0; i < leftColumn.Count; i++)
                {
                    text_info_container.AddText(text: leftColumn[i], font: CUI.Font.RobotoCondensedBold, fontSize: 18, align: TextAnchor.MiddleLeft, overflow: VerticalWrapMode.Truncate, anchorMin: "0 1", anchorMax: "0.49 1", offsetMin: $"0 -{65 + (i + 1) * 25}", offsetMax: $"0 -{65 + i * 25}");
                }

                for (int i = 0; i < rightColumn.Count; i++)
                {
                    text_info_container.AddText(text: rightColumn[i], font: CUI.Font.RobotoCondensedBold, fontSize: 18, align: TextAnchor.MiddleLeft, overflow: VerticalWrapMode.Truncate, anchorMin: "0.51 1", anchorMax: "1 1", offsetMin: $"0 -{65 + (i + 1) * 25}", offsetMax: $"0 -{65 + i * 25}");
                }

                var actionButtonsContainer = container.AddContainer(anchorMin: "0 0", anchorMax: "1 1", offsetMin: "30 10", offsetMax: "-30 -230");
                float offset = 10f;
                for (int a = 0; a < buttons.Length; a++)
                {
                    if (buttons[a].Count == 0)
                        continue;
                    List<Button> rowButtons = buttons[a].GetAllowedButtons(connectionUserId).ToList();
                    foreach (Button button in rowButtons.ToArray())
                    {
                        if (button == null)
                            continue;
                        if (button.Command != "userinfo.action")
                            continue;
                        string firstArg = button.Args[0];
                        if (firstArg == "mute" && isMuted || firstArg == "unmute" && !isMuted)
                            rowButtons.Remove(button);
                    }

                    for (int b = 0; b < rowButtons.Count(); b++)
                    {
                        Button button = rowButtons.ElementAtOrDefault(b);
                        if (button == null)
                            continue;
                        actionButtonsContainer.AddButton(color: "0.3 0.3 0.3 0.6", command: $"adminmenu {button.Command} {string.Join(" ", button.Args)}", anchorMin: "0 1", anchorMax: "0 1", offsetMin: $"{b * 150 + b * offset} -{(a + 1) * 35 + a * offset}", offsetMax: $"{(b + 1) * 150 + b * offset} -{a * 35 + a * offset}").AddText(text: button.Label.Localize(connectionUserId), font: CUI.Font.RobotoMonoRegular, fontSize: 12, align: TextAnchor.MiddleCenter);
                    }
                }
            }
        }

        private void adminmenu_chatcmd(BasePlayer player)
        {
            if (!CanUseAdminMenu(player))
                return;
            ConnectionData.GetOrCreate(player).ShowAdminMenu();
        }

        [ConsoleCommand("adminmenu")]
        private void adminmenu_cmd(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
                return;
            HandleCommand(player.Connection, arg.GetString(0), arg.Args?.Skip(1).ToArray());
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                    LoadDefaultConfig();
                SaveConfig();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                PrintWarning("Creating new config file.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(config);
        private bool CanUseCommand(Connection connection, string permission)
        {
            string userId = connection.userid.ToString();
            return Instance.permission.UserHasPermission(userId, PERMISSION_FULLACCESS) || Instance.permission.UserHasPermission(userId, permission);
        }

        private void HandleCommand(Connection connection, string command, params string[] args)
        {
            if (!CanUseAdminMenu(connection))
                return;
            ConnectionData connectionData;
            switch (command)
            {
                case "":
                    connectionData = ConnectionData.GetOrCreate(connection);
                    connectionData.ShowAdminMenu();
                    break;
                case "close":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        foreach (Button navButton in connectionData.currentMainMenu.NavButtons)
                            if (navButton != null)
                                navButton.Unpress(connectionData);
                        connectionData.HideAdminMenu();
                    }

                    break;
                case "openpanel":
                    ConnectionData.GetOrCreate(connection).OpenPanel(args[0]);
                    break;
                case "homebutton":
                    // Still thinking about it
                    break;
                case "uipanel.sidebar.button_pressed":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        int buttonIndex = int.Parse(args[0]);
                        int buttonCount = int.Parse(args[1]);
                        UpdateOneActiveImageElement(connection, "UIPanel_SideBar_Button", buttonIndex, buttonCount, "0 0 0 0.6", "0 0 0 0");
                        if (buttonIndex >= 0)
                        {
                            Button button = connectionData.currentSidebar.CategoryButtons.GetAllowedButtons(connection).ElementAt(buttonIndex);
                            if (!button.UserHasPermission(connection))
                                return;
                            HandleCommand(connection, button.Command, button.Args);
                        }
                    }

                    break;
                case "navigation.button_pressed":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        int buttonIndex2 = int.Parse(args[0]);
                        int buttonCount2 = int.Parse(args[1]);
                        IEnumerable<Button> navButtons = connectionData.currentMainMenu.NavButtons.GetAllowedButtons(connection);
                        if (buttonIndex2 != 0)
                        {
                            for (int i = 0; i < navButtons.Count(); i++)
                            {
                                Button navButton = navButtons.ElementAtOrDefault(i);
                                if (navButton == null)
                                    continue;
                                if (i == buttonIndex2)
                                    navButton.Press(connectionData);
                                else
                                    navButton.Unpress(connectionData);
                            }
                        }

                        if (buttonIndex2 >= 0)
                        {
                            Button navButton = navButtons.ElementAt(buttonIndex2);
                            if (navButton != null && navButton.UserHasPermission(connection))
                                HandleCommand(connection, navButton.Command, navButton.Args);
                        }

                        connectionData.UI.UpdateNavButtons(connectionData.currentMainMenu);
                    }

                    break;
                case "showcontent":
                    ConnectionData.GetOrCreate(connection).ShowPanelContent(args[0]);
                    break;
                case "back":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        string backcommand = (string)connectionData.userData["backcommand"];
                        if (backcommand != null)
                        {
                            string[] a = backcommand.Split(' ');
                            HandleCommand(connection, a[0], a.Skip(1).ToArray());
                            connectionData.userData["backcommand"] = null;
                        }
                    }

                    break;
                case "playerlist.opensearch":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                        (connectionData.currentContent as PlayerListContent)?.OpenSearch(connection);
                    break;
                case "playerlist.search.input":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        string searchQuery = string.Empty;
                        if (args.Length > 0)
                            searchQuery = string.Join(" ", args);
                        connectionData.userData["playerlist.searchQuery"] = searchQuery;
                        connectionData.currentContent.Render(connectionData);
                    }

                    break;
                case "playerlist.pagination":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        int currentPage = (int)connectionData.userData["playerlist.page"];
                        switch (args[0])
                        {
                            case "next":
                                currentPage++;
                                break;
                            case "prev":
                                if (currentPage > 0)
                                    currentPage--;
                                break;
                            default:
                                return;
                        }

                        connectionData.userData["playerlist.page"] = currentPage;
                        connectionData.currentContent.Render(connectionData);
                    }

                    break;
                case "playerlist.filter":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        Func<IPlayer, bool> filterFunc;
                        switch (args[0])
                        {
                            case "online":
                                filterFunc = (IPlayer player) => player.IsConnected;
                                break;
                            case "offline":
                                filterFunc = (IPlayer player) => !player.IsConnected && BasePlayer.FindSleeping(player.Id);
                                break;
                            case "admins":
                                filterFunc = (IPlayer player) => ServerUsers.Get(ulong.Parse(player.Id))?.group == ServerUsers.UserGroup.Owner;
                                break;
                            case "moders":
                                filterFunc = (IPlayer player) => ServerUsers.Get(ulong.Parse(player.Id))?.group == ServerUsers.UserGroup.Moderator;
                                break;
                            default:
                                filterFunc = (IPlayer player) => true;
                                break;
                        }

                        connectionData.userData["playerlist.filter"] = filterFunc;
                        connectionData.currentContent.Render(connectionData);
                    }

                    break;
                case "permissions.open":
                    if (!CanUseCommand(connection, PERMISSION_PERMISSIONMANAGER))
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        connectionData.OpenPanel("permissions");
                        HandleCommand(connection, "permissions.pluginlistpage.open", "0");
                    }

                    break;
                case "permissions.pluginlistpage.open":
                    if (!CanUseCommand(connection, PERMISSION_PERMISSIONMANAGER))
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        int page = int.Parse(args[0]);
                        Sidebar sidebar = SidebarCollection.TryGet($"pluginpermissions.{page}");
                        if (sidebar != null)
                            connectionData.SetSidebar(sidebar);
                    }

                    break;
                case "permissions.pluginpermissions.open":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        Content content = ContentCollection.TryGet($"permissions.{args[0]}");
                        connectionData.ShowPanelContent(content);
                    }

                    break;
                case "grouplist[popup:creategroup]":
                    if (!CanUseCommand(connection, PERMISSION_PERMISSIONMANAGER))
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        switch (args[0])
                        {
                            case "show":
                                connectionData.userData["grouplist[popup:creategroup]"] = true;
                                connectionData.userData["grouplist[popup:creategroup].name"] = null;
                                connectionData.userData["grouplist[popup:creategroup].title"] = null;
                                break;
                            case "close":
                                connectionData.userData["grouplist[popup:creategroup]"] = false;
                                break;
                            case "set_name":
                                connectionData.userData["grouplist[popup:creategroup].name"] = string.Join(" ", args.Skip(1));
                                break;
                            case "set_title":
                                connectionData.userData["grouplist[popup:creategroup].title"] = string.Join(" ", args.Skip(1));
                                break;
                            case "create":
                                bool creategroup_result = false;
                                string name = (string)connectionData.userData["grouplist[popup:creategroup].name"];
                                string title = (string)connectionData.userData["grouplist[popup:creategroup].title"];
                                if (permission.GroupExists(name))
                                {
                                    connectionData.userData["grouplist[popup:creategroup].name"] = null;
                                    creategroup_result = false;
                                }
                                else
                                {
                                    creategroup_result = permission.CreateGroup(name, title, 0);
                                }

                                if (creategroup_result)
                                    connectionData.userData["grouplist[popup:creategroup]"] = false;
                                break;
                        }

                        connectionData.currentContent.Render(connectionData);
                    }

                    break;
                case "group.managepermissions":
                    if (!CanUseCommand(connection, PERMISSION_PERMISSIONMANAGER))
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        HandleCommand(connection, "permissions.open");
                        connectionData.userData["permissions.target"] = args[0];
                        connectionData.userData["permissions.target_type"] = "group";
                    }

                    break;
                case "groupinfo.open":
                    if (!CanUseCommand(connection, PERMISSION_PERMISSIONMANAGER))
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        connectionData.userData["groupinfo.groupName"] = args[0];
                        if (connectionData.currentContent is GroupInfoContent)
                            connectionData.currentContent.Render(connectionData);
                        else
                            connectionData.OpenPanel("groupinfo");
                    }

                    break;
                case "groupinfo.permissions":
                    if (!CanUseCommand(connection, PERMISSION_PERMISSIONMANAGER))
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        HandleCommand(connection, "permissions.open");
                        connectionData.userData["backcommand"] = $"groupinfo.open {connectionData.userData["groupinfo.groupName"]}";
                        connectionData.UI.UpdateNavButtons(connectionData.currentMainMenu);
                        connectionData.userData["permissions.target"] = connectionData.userData["groupinfo.groupName"];
                        connectionData.userData["permissions.target_type"] = "group";
                    }

                    break;
                case "groupinfo.users.open":
                    if (!CanUseCommand(connection, PERMISSION_PERMISSIONMANAGER))
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        connectionData.userData["playerlist.filter"] = (Func<IPlayer, bool>)((IPlayer player) => permission.UserHasGroup(player.Id, connectionData.userData["groupinfo.groupName"].ToString()));
                        connectionData.ShowPanelContent("users");
                        connectionData.currentContent.Render(connectionData);
                    }

                    break;
                case "groupinfo.action":
                    if (!CanUseCommand(connection, PERMISSION_PERMISSIONMANAGER))
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        switch (args[0])
                        {
                            case "remove":
                                (connectionData.currentContent as GroupInfoContent).RemoveConfirmPopup(connectionData);
                                break;
                            case "remove.confirmed":
                                string groupName = (string)connectionData.userData["groupinfo.groupName"];
                                if (groupName == null)
                                    return;
                                if (permission.RemoveGroup(groupName))
                                    connectionData.OpenPanel("permissionmanager");
                                break;
                        }
                    }

                    break;
                case "groupinfo[popup:clonegroup]":
                    if (!CanUseCommand(connection, PERMISSION_PERMISSIONMANAGER))
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        switch (args[0])
                        {
                            case "show":
                                connectionData.userData["groupinfo[popup:clonegroup]"] = true;
                                (connectionData.currentContent as GroupInfoContent).LoadDefaultCloneGroupUserData(connectionData.userData);
                                break;
                            case "close":
                                connectionData.userData["groupinfo[popup:clonegroup]"] = false;
                                break;
                            case "set_name":
                                connectionData.userData["groupinfo[popup:clonegroup].name"] = string.Join(" ", args.Skip(1));
                                break;
                            case "set_title":
                                connectionData.userData["groupinfo[popup:clonegroup].title"] = string.Join(" ", args.Skip(1));
                                break;
                            case "cloneusers_toggle":
                                connectionData.userData["groupinfo[popup:clonegroup].cloneusers"] = !(bool)connectionData.userData["groupinfo[popup:clonegroup].cloneusers"];
                                break;
                            case "clone":
                                string groupName = (string)connectionData.userData["groupinfo.groupName"];
                                if (groupName == null)
                                    return;
                                bool creategroup_result = false;
                                string name = (string)connectionData.userData["groupinfo[popup:clonegroup].name"];
                                string title = (string)connectionData.userData["groupinfo[popup:clonegroup].title"];
                                bool cloneUsers = (bool)connectionData.userData["groupinfo[popup:clonegroup].cloneusers"];
                                if (permission.GroupExists(name))
                                {
                                    connectionData.userData["groupinfo[popup:clonegroup].name"] = null;
                                    creategroup_result = false;
                                }
                                else
                                {
                                    if (permission.CreateGroup(name, title, 0))
                                    {
                                        string[] perms = permission.GetGroupPermissions(groupName);
                                        for (int i = 0; i < perms.Length; i++)
                                            permission.GrantGroupPermission(name, perms[i], null);
                                        if (cloneUsers)
                                        {
                                            string[] users = permission.GetUsersInGroup(groupName);
                                            for (int i = 0; i < users.Length; i++)
                                            {
                                                string userId = users[i].Split(' ')?[0];
                                                if (!string.IsNullOrEmpty(userId))
                                                    permission.AddUserGroup(userId, name);
                                            }
                                        }

                                        creategroup_result = true;
                                    }
                                }

                                if (creategroup_result)
                                {
                                    connectionData.userData["groupinfo[popup:clonegroup]"] = false;
                                    HandleCommand(connection, "groupinfo.open", name);
                                }

                                break;
                        }

                        connectionData.currentContent.Render(connectionData);
                    }

                    break;
                case "userinfo.open":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        ulong userid;
                        switch (args[0])
                        {
                            case "self":
                                userid = connection.userid;
                                break;
                            case "last":
                                userid = (ulong)connectionData.userData["userinfo.lastuserid"];
                                break;
                            default:
                                ulong.TryParse(args[0], out userid);
                                break;
                        }

                        if (userid == 0)
                            return;
                        connectionData.userData["userinfo.userid"] = userid;
                        connectionData.userData["userinfo.lastuserid"] = userid;
                        if (connectionData.currentContent is UserInfoContent)
                            connectionData.currentContent.Render(connectionData);
                        else
                            connectionData.OpenPanel("userinfo");
                    }

                    break;
                case "userinfo.groups.open":
                    if (!CanUseCommand(connection, PERMISSION_PERMISSIONMANAGER))
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        connectionData.ShowPanelContent("groups");
                    }

                    break;
                case "userinfo.givemenu.open":
                    if (!CanUseCommand(connection, PERMISSION_GIVE))
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        connectionData.userData["givemenu.targets"] = new ulong[]
                        {
                            (ulong)connectionData.userData["userinfo.userid"]
                        };
                        connectionData.ShowPanelContent("give");
                    }

                    break;
                case "userinfo.permissions":
                    if (!CanUseCommand(connection, PERMISSION_PERMISSIONMANAGER))
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        HandleCommand(connection, "permissions.open");
                        connectionData.userData["backcommand"] = $"userinfo.open {connectionData.userData["userinfo.userid"]}";
                        connectionData.UI.UpdateNavButtons(connectionData.currentMainMenu);
                        connectionData.userData["permissions.target"] = connectionData.userData["userinfo.userid"];
                        connectionData.userData["permissions.target_type"] = "user";
                    }

                    break;
                case "userinfo.action":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        string userid = connectionData.userData["userinfo.userid"].ToString();
                        if (userid == null)
                            return;
                        string action = args[0];
                        UserInfoContent userInfoContent = (connectionData.currentContent as UserInfoContent);
                        Button button = null;
                        foreach (ButtonArray buttonRow in userInfoContent.buttons)
                        {
                            if (buttonRow == null || buttonRow.Count == 0)
                                continue;
                            Button foundedButton = buttonRow.Find(b => b != null && b.Args[0] == action);
                            if (foundedButton == null)
                                continue;
                            button = foundedButton;
                            break;
                        }

                        if (button == null)
                            return;
                        if (!button.UserHasPermission(connection))
                            return;
                        BasePlayer admin = connection.player as BasePlayer;
                        BasePlayer user = BasePlayer.FindAwakeOrSleeping(userid);
                        if (admin == null || user == null)
                            return;
                        switch (action)
                        {
                            case "teleportselfto":
                                admin.Teleport(user);
                                break;
                            case "teleporttoself":
                                user.Teleport(admin);
                                break;
                            case "teleporttoauth":
                                BaseEntity[] entities = BaseEntity.Util.FindTargetsAuthedTo(user.userID, string.Empty);
                                if (entities.Length > 0)
                                    admin.Teleport(entities.GetRandom().transform.position);
                                break;
                            case "teleporttodeathpoint":
                                ProtoBuf.MapNote UserDeathNote = user.ServerCurrentDeathNote;
                                if (UserDeathNote != null)
                                    admin.Teleport(UserDeathNote.worldPosition);
                                break;
                            case "heal":
                                if (user.IsWounded())
                                    user.StopWounded();
                                user.Heal(user.MaxHealth());
                                user.metabolism.calories.value = user.metabolism.calories.max;
                                user.metabolism.hydration.value = user.metabolism.hydration.max;
                                user.metabolism.radiation_level.value = 0;
                                user.metabolism.radiation_poison.value = 0;
                                connectionData.currentContent.Render(connectionData);
                                break;
                            case "heal50":
                                if (user.IsWounded())
                                    user.StopWounded();
                                user.Heal(user.MaxHealth() / 50);
                                connectionData.currentContent.Render(connectionData);
                                break;
                            case "kill":
                                user.DieInstantly();
                                connectionData.currentContent.Render(connectionData);
                                break;
                            case "viewinv":
                                PlayerLoot playerLoot = admin.inventory.loot;
                                bool IsLooting = playerLoot.IsLooting();
                                playerLoot.containers.Clear();
                                playerLoot.entitySource = null;
                                playerLoot.itemSource = null;
                                if (IsLooting)
                                    playerLoot.SendImmediate();
                                NextFrame(() =>
                                {
                                    playerLoot.PositionChecks = false;
                                    playerLoot.entitySource = RelationshipManager.ServerInstance;
                                    playerLoot.AddContainer(user.inventory.containerMain);
                                    playerLoot.AddContainer(user.inventory.containerWear);
                                    playerLoot.AddContainer(user.inventory.containerBelt);
                                    playerLoot.SendImmediate();
                                    admin.ClientRPCPlayer<string>(null, admin, "RPC_OpenLootPanel", "player_corpse");
                                });
                                HandleCommand(connection, "close");
                                break;
                            case "stripinventory":
                                user.inventory.Strip();
                                break;
                            case "unlockblueprints":
                                user.blueprints.UnlockAll();
                                break;
                            case "revokeblueprints":
                                user.blueprints.Reset();
                                break;
                            case "spectate":
                                if (!admin.IsDead())
                                    admin.DieInstantly();
                                if (admin.IsDead())
                                {
                                    admin.StartSpectating();
                                    admin.UpdateSpectateTarget(user.userID);
                                }

                                break;
                            case "mute":
                                user.SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, true);
                                connectionData.currentContent.Render(connectionData);
                                break;
                            case "unmute":
                                user.SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, false);
                                connectionData.currentContent.Render(connectionData);
                                break;
                            case "kick":
                                user.Kick(string.Empty);
                                connectionData.currentContent.Render(connectionData);
                                break;
                            case "ban":
                                global::ServerUsers.User serverUser = global::ServerUsers.Get(user.userID);
                                if (serverUser != null && serverUser.group == global::ServerUsers.UserGroup.Banned)
                                {
                                    admin.ConsoleMessage(string.Format("User {0} is already banned", user.userID));
                                    return;
                                }

                                ServerUsers.Set(user.userID, global::ServerUsers.UserGroup.Banned, user.displayName, "Banned");
                                if (user.IsConnected && user.net.connection.ownerid != 0UL && user.net.connection.ownerid != user.net.connection.userid)
                                    global::ServerUsers.Set(user.net.connection.ownerid, global::ServerUsers.UserGroup.Banned, user.displayName, string.Format("Family share owner of {0}", user.net.connection.userid), -1L);
                                ServerUsers.Save();
                                Net.sv.Kick(user.net.connection, "Banned", false);
                                connectionData.currentContent.Render(connectionData);
                                break;
                            default:
                                break;
                        }
                    }

                    break;
                case "usergroups.action":
                    if (!CanUseCommand(connection, PERMISSION_PERMISSIONMANAGER))
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        string userId = connectionData.userData["userinfo.userid"].ToString();
                        if (userId == null)
                            return;
                        bool isGrant;
                        switch (args[0])
                        {
                            case "grant":
                                isGrant = true;
                                break;
                            case "revoke":
                                isGrant = false;
                                break;
                            default:
                                return;
                        }

                        string groupName = args[1];
                        if (isGrant)
                            Instance.permission.AddUserGroup(userId, groupName);
                        else
                            Instance.permission.RemoveUserGroup(userId, groupName);
                        connectionData.currentContent.Render(connectionData);
                    }

                    break;
                case "permission.action":
                    if (!CanUseCommand(connection, PERMISSION_PERMISSIONMANAGER))
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        string type = connectionData.userData["permissions.target_type"].ToString();
                        string target = connectionData.userData["permissions.target"].ToString();
                        if (type == null || target == null)
                            return;
                        bool isTargetUser;
                        switch (type)
                        {
                            case "user":
                                isTargetUser = true;
                                break;
                            case "group":
                                isTargetUser = false;
                                break;
                            default:
                                return;
                        }

                        bool isGrant;
                        switch (args[0])
                        {
                            case "grant":
                                isGrant = true;
                                break;
                            case "revoke":
                                isGrant = false;
                                break;
                            default:
                                return;
                        }

                        string permission = args[1];
                        if (permission == "adminmenu.fullaccess")
                            return;
                        if (isTargetUser)
                        {
                            if (isGrant)
                                Instance.permission.GrantUserPermission(target, permission, null);
                            else
                                Instance.permission.RevokeUserPermission(target, permission);
                        }
                        else
                        {
                            if (isGrant)
                                Instance.permission.GrantGroupPermission(target, permission, null);
                            else
                                Instance.permission.RevokeGroupPermission(target, permission);
                        }

                        connectionData.currentContent.Render(connectionData);
                    }

                    break;
                case "givemenu.open":
                    if (!CanUseCommand(connection, PERMISSION_GIVE))
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        ulong userid;
                        switch (args[0])
                        {
                            case "self":
                                userid = connection.userid;
                                break;
                            default:
                                ulong.TryParse(args[0], out userid);
                                break;
                        }

                        connectionData.userData["givemenu.targets"] = new ulong[]
                        {
                            userid
                        };
                        connectionData.OpenPanel("givemenu");
                    }

                    break;
                case "givemenu.opensearch":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                        (connectionData.currentContent as GiveMenuContent)?.OpenSearch(connection);
                    break;
                case "givemenu.search.input":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        string searchQuery = string.Empty;
                        if (args.Length > 0)
                            searchQuery = string.Join(" ", args);
                        connectionData.userData["givemenu.searchQuery"] = searchQuery;
                        connectionData.currentContent.Render(connectionData);
                    }

                    break;
                case "givemenu.pagination":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        int currentPage = (int)connectionData.userData["givemenu.page"];
                        switch (args[0])
                        {
                            case "next":
                                currentPage++;
                                break;
                            case "prev":
                                if (currentPage > 0)
                                    currentPage--;
                                break;
                            default:
                                return;
                        }

                        connectionData.userData["givemenu.page"] = currentPage;
                        connectionData.currentContent.Render(connectionData);
                    }

                    break;
                case "givemenu.filter":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        connectionData.userData["givemenu.category"] = (ItemCategory)int.Parse(args[0]);
                        connectionData.userData["givemenu.page"] = 1;
                        connectionData.currentContent.Render(connectionData);
                    }

                    break;
                case "givemenu.popup":
                    if (!CanUseCommand(connection, PERMISSION_GIVE))
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        switch (args[0])
                        {
                            case "show":
                                connectionData.userData["givemenu.popup.shown"] = true;
                                connectionData.userData["givemenu.popup.itemid"] = int.Parse(args[1]);
                                connectionData.userData["givemenu.popup.amount"] = 1;
                                connectionData.userData["givemenu.popup.skin"] = 0UL;
                                connectionData.userData["givemenu.popup.name"] = null;
                                connectionData.userData["givemenu.popup.isblueprint"] = false;
                                break;
                            case "close":
                                connectionData.userData["givemenu.popup.shown"] = false;
                                break;
                            case "set_amount":
                                if (int.TryParse(args[1], out int set_amount))
                                    connectionData.userData["givemenu.popup.amount"] = set_amount;
                                break;
                            case "set_skin":
                                if (ulong.TryParse(args[1], out ulong set_skinid))
                                    connectionData.userData["givemenu.popup.skin"] = set_skinid;
                                break;
                            case "set_name":
                                connectionData.userData["givemenu.popup.name"] = string.Join(" ", args);
                                break;
                            case "isblueprint_toggle":
                                connectionData.userData["givemenu.popup.isblueprint"] = !(bool)connectionData.userData["givemenu.popup.isblueprint"];
                                break;
                            case "give":
                                connectionData = ConnectionData.Get(connection);
                                if (connectionData != null)
                                {
                                    int give_itemId = (int)connectionData.userData["givemenu.popup.itemid"];
                                    int give_amount = (int)connectionData.userData["givemenu.popup.amount"];
                                    ulong give_skin = (ulong)connectionData.userData["givemenu.popup.skin"];
                                    string give_name = (string)connectionData.userData["givemenu.popup.name"];
                                    bool isBlueprint = (bool)connectionData.userData["givemenu.popup.isblueprint"];
                                    ulong[] targets = (ulong[])connectionData.userData["givemenu.targets"];
                                    foreach (ulong targetUserId in targets)
                                    {
                                        BasePlayer playerToGive = BasePlayer.FindAwakeOrSleeping(targetUserId.ToString());
                                        if (playerToGive == null)
                                            continue;
                                        Item newItem = ItemManager.CreateByItemID(isBlueprint ? ItemManager.blueprintBaseDef.itemid : give_itemId, give_amount, give_skin);
                                        if (newItem != null)
                                        {
                                            if (isBlueprint)
                                            {
                                                newItem.blueprintTarget = give_itemId;
                                                newItem.OnVirginSpawn();
                                            }
                                            else
                                            {
                                                if (give_name != null)
                                                    newItem.name = give_name;
                                            }

                                            playerToGive.GiveItem(newItem);
                                        }
                                    }

                                    connectionData.userData["givemenu.popup.shown"] = false;
                                }

                                break;
                        }

                        connectionData.currentContent.Render(connectionData);
                    }

                    break;
                case "quickmenu.action":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        BasePlayer admin = connection.player as BasePlayer;
                        if (admin == null)
                            return;
                        string action = args[0];
                        QuickMenuContent quickMenuContent = (connectionData.currentContent as QuickMenuContent);
                        Button button = null;
                        foreach (ButtonArray buttonRow in quickMenuContent.buttons)
                        {
                            Button foundedButton = buttonRow.Find(b => b != null && b.Args[0] == action);
                            if (foundedButton == null)
                                continue;
                            button = foundedButton;
                            break;
                        }

                        if (button == null)
                            return;
                        if (!button.UserHasPermission(connection))
                            return;
                        switch (action)
                        {
                            case "teleportto_000":
                                admin.Teleport(Vector3.zero);
                                break;
                            case "teleportto_deathpoint":
                                var deathMapNote = admin.ServerCurrentDeathNote;
                                if (deathMapNote != null)
                                    admin.Teleport(deathMapNote.worldPosition);
                                break;
                            case "teleportto_randomspawnpoint":
                                global::BasePlayer.SpawnPoint spawnPoint = global::ServerMgr.FindSpawnPoint(admin);
                                if (spawnPoint != null)
                                    admin.Teleport(spawnPoint.pos);
                                break;
                            case "healself":
                                if (admin.IsWounded())
                                    admin.StopWounded();
                                admin.Heal(admin.MaxHealth());
                                admin.metabolism.calories.value = admin.metabolism.calories.max;
                                admin.metabolism.hydration.value = admin.metabolism.hydration.max;
                                admin.metabolism.radiation_level.value = 0;
                                admin.metabolism.radiation_poison.value = 0;
                                break;
                            case "killself":
                                admin.DieInstantly();
                                break;
                            case "helicall":
                                global::BaseEntity heliEntity = global::GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", default(Vector3), default(Quaternion), true);
                                if (heliEntity)
                                {
                                    heliEntity.GetComponent<global::PatrolHelicopterAI>().SetInitialDestination(admin.transform.position + new Vector3(0f, 10f, 0f), 0.25f);
                                    heliEntity.Spawn();
                                }

                                break;
                            case "spawnbradley":
                                GameManager.server.CreateEntity("assets/prefabs/npc/m2bradley/bradleyapc.prefab", admin.CenterPoint(), default(Quaternion), true).Spawn();
                                break;
                            case "spawncargo":
                                BaseEntity cargo = global::GameManager.server.CreateEntity("assets/content/vehicles/boats/cargoship/cargoshiptest.prefab", default(Vector3), default(Quaternion), true);
                                if (cargo != null)
                                {
                                    cargo.SendMessage("TriggeredEventSpawn", SendMessageOptions.DontRequireReceiver);
                                    cargo.Spawn();
                                    return;
                                }

                                break;
                            case "giveaway_online":
                                connectionData.userData["givemenu.targets"] = BasePlayer.activePlayerList.Select(p => p.userID).ToArray();
                                connectionData.OpenPanel("givemenu");
                                break;
                            case "giveaway_everyone":
                                connectionData.userData["givemenu.targets"] = BasePlayer.allPlayerList.ToArray();
                                connectionData.OpenPanel("givemenu");
                                break;
                            case "settime":
                                float time = float.Parse(args[1]);
                                ConVar.Env.time = time;
                                break;
                        }
                    }

                    break;
                case "convars.opensearch":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                        (connectionData.currentContent as ConvarsContent)?.OpenSearch(connection);
                    break;
                case "convars.search.input":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        string searchQuery = string.Empty;
                        if (args.Length > 0)
                            searchQuery = string.Join(" ", args);
                        connectionData.userData["convars.searchQuery"] = searchQuery;
                        connectionData.currentContent.Render(connectionData);
                    }

                    break;
                case "convars.pagination":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        int currentPage = (int)connectionData.userData["convars.page"];
                        switch (args[0])
                        {
                            case "next":
                                currentPage++;
                                break;
                            case "prev":
                                if (currentPage > 0)
                                    currentPage--;
                                break;
                            default:
                                return;
                        }

                        connectionData.userData["convars.page"] = currentPage;
                        connectionData.currentContent.Render(connectionData);
                    }

                    break;
                case "convar.setvalue":
                    var convar = ConsoleGen.All.FirstOrDefault(c => c.FullName == args[0]);
                    if (convar != null)
                    {
                        convar.Set(string.Join(" ", args.Skip(1)));
                        connectionData = ConnectionData.Get(connection);
                        if (connectionData != null)
                            connectionData.currentContent?.Render(connectionData);
                    }

                    break;
                case "pluginmanager.opensearch":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                        (connectionData.currentContent as PluginManagerContent)?.OpenSearch(connection);
                    break;
                case "pluginmanager.search.input":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        string searchQuery = string.Empty;
                        if (args.Length > 0)
                            searchQuery = string.Join(" ", args);
                        connectionData.userData["pluginmanager.searchQuery"] = searchQuery;
                        connectionData.currentContent.Render(connectionData);
                    }

                    break;
                case "pluginmanager.pagination":
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                    {
                        int currentPage = (int)connectionData.userData["pluginmanager.page"];
                        switch (args[0])
                        {
                            case "next":
                                currentPage++;
                                break;
                            case "prev":
                                if (currentPage > 0)
                                    currentPage--;
                                break;
                            default:
                                return;
                        }

                        connectionData.userData["pluginmanager.page"] = currentPage;
                        connectionData.currentContent.Render(connectionData);
                    }

                    break;
                case "pluginmanager.load":
                    if (args.Length > 1)
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                        connectionData.userData["pluginmanager.lastusedplugin"] = args[0];
                    Oxide.Core.Interface.Oxide.LoadPlugin(args[0]);
                    break;
                case "pluginmanager.unload":
                    if (args.Length > 1)
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                        connectionData.userData["pluginmanager.lastusedplugin"] = args[0];
                    Oxide.Core.Interface.Oxide.UnloadPlugin(args[0]);
                    break;
                case "pluginmanager.reload":
                    if (args.Length > 1)
                        return;
                    connectionData = ConnectionData.Get(connection);
                    if (connectionData != null)
                        connectionData.userData["pluginmanager.lastusedplugin"] = args[0];
                    Oxide.Core.Interface.Oxide.ReloadPlugin(args[0]);
                    break;
                default:
                    break;
            }
        }

        void Init()
        {
            permission.RegisterPermission(PERMISSION_USE, this);
            permission.RegisterPermission(PERMISSION_FULLACCESS, this);
            permission.RegisterPermission(PERMISSION_CONVARS, this);
            permission.RegisterPermission(PERMISSION_PERMISSIONMANAGER, this);
            permission.RegisterPermission(PERMISSION_PLUGINMANAGER, this);
            permission.RegisterPermission(PERMISSION_GIVE, this);
            if (!config.DisableSwapSeatsHook)
                cmd.AddConsoleCommand("swapseats", this, "swapseats_hook");
            cmd.AddChatCommand(config.ChatCommand, this, "adminmenu_chatcmd");
            FormatMainMenu();
            FormatPanelList();
        }

        void OnServerInitialized()
        {
            ADMINMENU_IMAGECRC = FileStorage.server.Store(Convert.FromBase64String(ADMINMENU_IMAGEBASE64), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
            Collector.CollectAll();
            lang.RegisterMessages(defaultLang, this);
            foreach (var pair in ConnectionData.all)
            {
                Connection connection = pair.Key;
                ConnectionData data = pair.Value;
                if (connection?.connected == true && data.IsDestroyed)
                    data.Init();
            }
        }

        void Debug()
        {
            BasePlayer player = BasePlayer.activePlayerList[0];
            if (player == null || player.userID != 76561198309519060)
                return;
            if (!CanUseAdminMenu(player))
                return;
            ConnectionData connectionData = ConnectionData.GetOrCreate(player);
            connectionData.ShowAdminMenu();
            HandleCommand(connectionData.connection, "userinfo.open", "self");
        }

        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                ConnectionData.Get(player)?.UI.DestroyAll();
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            ConnectionData.Get(player)?.Dispose();
        }

        void OnPluginLoaded()
        {
            Collector.CollectPluginsStaff();
            foreach (ConnectionData connectionData in ConnectionData.all.Values)
            {
                if (!connectionData.connection.connected)
                    continue;
                if (connectionData.currentContent is PluginManagerContent)
                    connectionData.currentContent.Render(connectionData);
            }
        }

        void OnPluginUnloaded()
        {
            Collector.CollectPluginsStaff();
            foreach (ConnectionData connectionData in ConnectionData.all.Values)
            {
                if (!connectionData.connection.connected)
                    continue;
                if (connectionData.currentContent is PluginManagerContent)
                    connectionData.currentContent.Render(connectionData);
            }
        }

        void FormatMainMenu()
        {
            mainMenu = new MainMenu
            {
                NavButtons = new ButtonArray
                {
                    new HideButton("BACK", "back"),
                    null,
                    new Button("QUICK MENU", null, "openpanel", "quickmenu"),
                    new Button("PLAYER LIST", null, "openpanel", "playerlist"),
                    new Button("CONVARS", null, "openpanel", "convars")
                    {
                        Permission = "convars"
                    },
                    new Button("PERMISSION MANAGER", null, "openpanel", "permissionmanager")
                    {
                        Permission = "permissionmanager"
                    },
                    new Button("PLUGIN MANAGER", null, "openpanel", "pluginmanager")
                    {
                        Permission = "pluginmanager"
                    },
                    null,
                    new Button("GIVE SELF", null, "givemenu.open", "self")
                    {
                        Permission = "give"
                    },
                    new Button("SELECT LAST USER", null, "userinfo.open", "last"),
                    null,
                    new Button("CLOSE", null, "close"),
                }
            };
        }

        private bool CanUseAdminMenu(Connection connection)
        {
            string userId = connection.userid.ToString();
            return permission.UserHasPermission(userId, PERMISSION_USE) || permission.UserHasPermission(userId, PERMISSION_FULLACCESS);
        }

        private bool CanUseAdminMenu(BasePlayer player)
        {
            return CanUseAdminMenu(player.Connection);
        }

        void UpdateOneActiveImageElement(Connection connection, string baseId, int activeButtonIndex, int buttonCount, string activeColor, string disableColor) => UpdateOneActiveElement(connection, baseId, activeButtonIndex, buttonCount, activeColor, disableColor, true);
        void UpdateOneActiveTextElement(Connection connection, string baseId, int activeButtonIndex, int buttonCount, string activeColor, string disableColor) => UpdateOneActiveElement(connection, baseId, activeButtonIndex, buttonCount, activeColor, disableColor, false);
        void UpdateOneActiveElement(Connection connection, string baseId, int activeButtonIndex, int buttonCount, string activeColor, string disableColor, bool isImage)
        {
            CUI.Root root = new CUI.Root();
            for (int i = 0; i < buttonCount; i++)
            {
                CUI.Element element = new CUI.Element
                {
                    Name = $"{baseId}{i}"};
                if (isImage)
                {
                    element.Components.Add(new CuiImageComponent { Color = (i == activeButtonIndex ? activeColor : disableColor) });
                }
                else
                {
                    element.Components.Add(new CuiTextComponent { Color = (i == activeButtonIndex ? activeColor : disableColor) });
                }

                root.Add(element);
            }

            root.Update(connection);
        }

        void FormatPanelList()
        {
            ButtonArray<CategoryButton> givemenuCategories = new ButtonArray<CategoryButton>()
            {
                new CategoryButton("ALL", "showcontent", "all")
            };
            string[] categoryNames = Enum.GetNames(typeof(ItemCategory));
            for (int i = 0; i < categoryNames.Length; i++)
            {
                string categoryName = categoryNames[i];
                if (categoryName == "All" || categoryName == "Search" || categoryName == "Favourite")
                    continue;
                givemenuCategories.Add(new CategoryButton(categoryName.ToUpper(), "givemenu.filter", i.ToString()));
            }

            GiveMenuContent giveMenuContent = new GiveMenuContent();
            QuickMenuContent quickMenuContent = new QuickMenuContent()
            {
                buttons = new ButtonArray[16]
                {
                    new ButtonArray
                    {
                        new Button("Teleport to 0 0 0", null, "quickmenu.action", "teleportto_000")
                        {
                            Permission = "quickmenu.teleportto000"
                        },
                        new Button("Teleport to\nDeathpoint", null, "quickmenu.action", "teleportto_deathpoint")
                        {
                            Permission = "quickmenu.teleporttodeath"
                        },
                        new Button("Teleport to\nSpawn point", null, "quickmenu.action", "teleportto_randomspawnpoint")
                        {
                            Permission = "quickmenu.teleporttospawnpoint"
                        },
                    },
                    new ButtonArray
                    {
                        new Button("Kill Self", null, "quickmenu.action", "killself"),
                        new Button("Heal Self", null, "quickmenu.action", "healself")
                        {
                            Permission = "quickmenu.healself"
                        },
                        new Button("Time to 12", null, "quickmenu.action", "settime", "12")
                        {
                            Permission = "quickmenu.settime"
                        },
                    },
                    new ButtonArray
                    {
                        new Button("Giveaway\nto online players", null, "quickmenu.action", "giveaway_online")
                        {
                            Permission = "quickmenu.giveaway"
                        },
                        new Button("Giveaway\nto everyone", null, "quickmenu.action", "giveaway_everyone")
                        {
                            Permission = "quickmenu.giveaway"
                        },
                    },
                    new ButtonArray
                    {
                        new Button("Call Heli", null, "quickmenu.action", "helicall")
                        {
                            Permission = "quickmenu.helicall"
                        },
                        new Button("Spawn Bradley", null, "quickmenu.action", "spawnbradley")
                        {
                            Permission = "quickmenu.spawnbradley"
                        },
                        new Button("Spawn Cargo", null, "quickmenu.action", "spawncargo")
                        {
                            Permission = "quickmenu.spawncargo"
                        },
                    },
                    new ButtonArray
                    {
                    },
                    new ButtonArray
                    {
                    },
                    new ButtonArray
                    {
                    },
                    new ButtonArray
                    {
                    },
                    new ButtonArray
                    {
                    },
                    new ButtonArray
                    {
                    },
                    new ButtonArray
                    {
                    },
                    new ButtonArray
                    {
                    },
                    new ButtonArray
                    {
                    },
                    new ButtonArray
                    {
                    },
                    new ButtonArray
                    {
                    },
                    new ButtonArray
                    {
                    },
                }
            };
            UserInfoContent userInfoContent = new UserInfoContent()
            {
                buttons = new ButtonArray[10]
                {
                    new ButtonArray
                    {
                        new Button("Teleport Self To", null, "userinfo.action", "teleportselfto")
                        {
                            Permission = "userinfo.teleportselfto"
                        },
                        new Button("Teleport To Self", null, "userinfo.action", "teleporttoself")
                        {
                            Permission = "userinfo.teleporttoself"
                        },
                        new Button("Teleport To Auth", null, "userinfo.action", "teleporttoauth")
                        {
                            Permission = "userinfo.teleporttoauth"
                        },
                    },
                    new ButtonArray
                    {
                        new Button("Heal", null, "userinfo.action", "heal")
                        {
                            Permission = "userinfo.fullheal"
                        },
                        new Button("Heal 50%", null, "userinfo.action", "heal50")
                        {
                            Permission = "userinfo.halfheal"
                        },
                        new Button("Teleport to\nDeathpoint", null, "userinfo.action", "teleporttodeathpoint")
                        {
                            Permission = "userinfo.teleporttodeath"
                        },
                    },
                    new ButtonArray
                    {
                        new Button("View Inventory", null, "userinfo.action", "viewinv")
                        {
                            Permission = "userinfo.viewinv"
                        },
                        new Button("Unlock Blueprints", null, "userinfo.action", "unlockblueprints")
                        {
                            Permission = "userinfo.unlockblueprints"
                        },
                        new Button("Spectate", null, "userinfo.action", "spectate")
                        {
                            Permission = "userinfo.spectate"
                        },
                    },
                    new ButtonArray
                    {
                        new Button("Mute", null, "userinfo.action", "mute")
                        {
                            Permission = "userinfo.mute"
                        },
                        new Button("Unmute", null, "userinfo.action", "unmute")
                        {
                            Permission = "userinfo.unmute"
                        },
                    },
                    new ButtonArray
                    {
                    },
                    new ButtonArray
                    {
                        null,
                        new Button("<color=olive>Strip Inventory</color>", null, "userinfo.action", "stripinventory")
                        {
                            Permission = "userinfo.stripinventory"
                        },
                        new Button("<color=olive>Revoke Blueprints</color>", null, "userinfo.action", "revokeblueprints")
                        {
                            Permission = "userinfo.revokeblueprints"
                        },
                    },
                    new ButtonArray
                    {
                        new Button("Kill", null, "userinfo.action", "kill")
                        {
                            Permission = "userinfo.kill"
                        },
                        new Button("<color=red>Kick</color>", null, "userinfo.action", "kick")
                        {
                            Permission = "userinfo.kick"
                        },
                        new Button("<color=red>Ban</color>", null, "userinfo.action", "ban")
                        {
                            Permission = "userinfo.ban"
                        },
                    },
                    new ButtonArray
                    {
                    },
                    new ButtonArray
                    {
                    },
                    new ButtonArray
                    {
                    }
                }
            };
            GroupInfoContent groupInfoContent = new GroupInfoContent
            {
                buttons = new ButtonArray[1]
                {
                    new ButtonArray
                    {
                        new Button("<color=#dd0000>Remove Group</color>", null, "groupinfo.action", "remove")
                        {
                            Permission = "groupinfo.removegroup"
                        },
                        new Button("<color=olive>Clone Group</color>", null, "groupinfo[popup:clonegroup]", "show")
                        {
                            Permission = "groupinfo.clonegroup"
                        },
                    }
                }
            };
            panelList = new Dictionary<string, Panel>()
            {
                {
                    "empty",
                    new Panel
                    {
                        Sidebar = null,
                        Content = null
                    }
                },
                {
                    "quickmenu",
                    new Panel
                    {
                        Sidebar = null,
                        Content = new Dictionary<string, Content>()
                        {
                            {
                                "default",
                                quickMenuContent
                            }
                        }
                    }
                },
                {
                    "permissionmanager",
                    new Panel
                    {
                        Sidebar = new Sidebar
                        {
                            CategoryButtons = new ButtonArray<CategoryButton>
                            {
                                new CategoryButton("GROUPS", "showcontent", "groups"),
                                new CategoryButton("USERS", "showcontent", "users"),
                            }
                        },
                        Content = new Dictionary<string, Content>
                        {
                            {
                                "groups",
                                new GroupListContent()
                            },
                            {
                                "users",
                                new PlayerListContent()
                            }
                        }
                    }
                },
                {
                    "pluginmanager",
                    new Panel
                    {
                        Sidebar = null,
                        Content = new Dictionary<string, Content>
                        {
                            {
                                "default",
                                new PluginManagerContent()
                            }
                        }
                    }
                },
                {
                    "permissions",
                    new PermissionPanel
                    {
                        Content = new Dictionary<string, Content>
                        {
                            {
                                "default",
                                new CenteredTextContent()
                                {
                                    text = "Please select the plugin from left side",
                                }
                            }
                        }
                    }
                },
                {
                    "userinfo",
                    new Panel
                    {
                        Sidebar = new Sidebar()
                        {
                            CategoryButtons = new ButtonArray<CategoryButton>
                            {
                                new CategoryButton("INFO", "showcontent", "info"),
                                new CategoryButton("GIVE", "userinfo.givemenu.open")
                                {
                                    Permission = "give"
                                },
                                new CategoryButton("USER GROUPS", "userinfo.groups.open")
                                {
                                    Permission = "permissionmanager"
                                },
                                new CategoryButton("PERMISSIONS", "userinfo.permissions")
                                {
                                    Permission = "permissionmanager"
                                }
                            }
                        },
                        Content = new Dictionary<string, Content>()
                        {
                            {
                                "info",
                                userInfoContent
                            },
                            {
                                "groups",
                                new UserGroupsContent()
                            },
                            {
                                "give",
                                giveMenuContent
                            }
                        }
                    }
                },
                {
                    "groupinfo",
                    new Panel
                    {
                        Sidebar = new Sidebar()
                        {
                            CategoryButtons = new ButtonArray<CategoryButton>
                            {
                                new CategoryButton("INFO", "showcontent", "info"),
                                new CategoryButton("USERS", "groupinfo.users.open"),
                                new CategoryButton("PERMISSIONS", "groupinfo.permissions")
                            }
                        },
                        Content = new Dictionary<string, Content>()
                        {
                            {
                                "info",
                                groupInfoContent
                            },
                            {
                                "users",
                                new PlayerListContent()
                            }
                        }
                    }
                },
                {
                    "playerlist",
                    new Panel
                    {
                        Sidebar = new Sidebar
                        {
                            CategoryButtons = new ButtonArray<CategoryButton>
                            {
                                new CategoryButton("ONLINE", "playerlist.filter", "online"),
                                new CategoryButton("OFFLINE", "playerlist.filter", "offline"),
                                new CategoryButton("ADMINS", "playerlist.filter", "admins"),
                                new CategoryButton("MODERS", "playerlist.filter", "moders"),
                                new CategoryButton("ALL", "playerlist.filter", "all"),
                            },
                        },
                        Content = new Dictionary<string, Content>
                        {
                            {
                                "default",
                                new PlayerListContent()
                            }
                        }
                    }
                },
                {
                    "givemenu",
                    new Panel
                    {
                        Sidebar = new Sidebar
                        {
                            CategoryButtons = givemenuCategories
                        },
                        Content = new Dictionary<string, Content>()
                        {
                            {
                                "all",
                                giveMenuContent
                            }
                        }
                    }
                },
                {
                    "convars",
                    new Panel
                    {
                        Sidebar = null,
                        Content = new Dictionary<string, Content>()
                        {
                            {
                                "default",
                                new ConvarsContent()
                            }
                        }
                    }
                }
            };
        }

        private void swapseats_hook(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
                return;
            Connection connection = player.Connection;
            if (connection != null && CanUseAdminMenu(connection) && !player.isMounted)
            {
                if (ConnectionData.Get(connection)?.IsAdminMenuDisplay == true)
                {
                    HandleCommand(connection, "close");
                }
                else
                {
                    HandleCommand(connection, "");
                    ConnectionData connectionData = ConnectionData.Get(connection);
                    ulong lastUserId = (ulong)connectionData.userData["userinfo.lastuserid"];
                    Ray ray = player.eyes.HeadRay();
                    RaycastHit raycastHit;
                    if (Physics.Raycast(ray, out raycastHit, 10, 1218652417))
                    {
                        BasePlayer hitPlayer = null;
                        BaseEntity hitEntity = raycastHit.GetEntity();
                        if (hitEntity != null)
                        {
                            hitPlayer = hitEntity as BasePlayer;
                            if (hitPlayer == null)
                            {
                                BaseVehicle hitVehicle = hitEntity as BaseVehicle;
                                if (hitVehicle != null)
                                {
                                    hitPlayer = hitVehicle.GetMounted();
                                }
                            }
                        }

                        if (hitPlayer == null)
                        {
                            List<BasePlayer> list = Facepunch.Pool.GetList<global::BasePlayer>();
                            Vis.Entities<BasePlayer>(raycastHit.point, 3, list, 131072, QueryTriggerInteraction.UseGlobal);
                            list = list.Where(basePlayer => basePlayer != null && !basePlayer.IsNpc && basePlayer.userID.IsSteamId() && basePlayer.userID != player.userID && basePlayer.userID != lastUserId).ToList();
                            hitPlayer = list.GetRandom();
                            Facepunch.Pool.FreeList<BasePlayer>(ref list);
                        }

                        if (hitPlayer == null || hitPlayer.IsNpc || !hitPlayer.userID.IsSteamId())
                            return;
                        if (hitPlayer.userID == player.userID)
                            return;
                        HandleCommand(connection, "userinfo.open", hitPlayer.UserIDString);
                    }
                }
            }
            else
            {
                ConVar.vehicle.swapseats(arg);
            }
        }
    }
}