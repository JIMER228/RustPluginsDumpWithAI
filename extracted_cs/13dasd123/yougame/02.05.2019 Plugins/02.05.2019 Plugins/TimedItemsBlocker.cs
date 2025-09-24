using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using Oxide.Core;
using System.IO;
using System.Collections;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("TimedItemsBlocker", "Vlad-00003", "2.1.2", ResourceId = 54)]
      //  Слив плагинов server-rust by Apolo YouGame
    [Description("Prevents some items from being used for a limited period of time.")]

    class TimedItemsBlocker : RustPlugin
    {

        #region Vars
        private Dictionary<BasePlayer, Timer> Main = new Dictionary<BasePlayer, Timer>();
        private List<BasePlayer> OnScreen = new List<BasePlayer>();
        private Timer OnScreenUpdater;
        private PluginConfig config;
        //private string Image;
        private Dictionary<string, string> Image = new Dictionary<string, string>();
        [PluginReference]
        Plugin Duel;
        #endregion

        #region Config setup

        #region GUI Settings
        private class GUIPanel
        {
            [JsonProperty("Minimum anchor")]
            public string Amin;
            [JsonProperty("Maximum anchor")]
            public string Amax;
            [JsonProperty("Color")]
            public string Color;
        }
        private class GUIText : GUIPanel
        {
            [JsonProperty("Size")]
            public int Size;
            [JsonProperty("Outline")]
            public GUIOutline Outline;
        }
        private class GUIImage
        {
            [JsonProperty("Minimum anchor")]
            public string Amin;
            [JsonProperty("Maximum anchor")]
            public string Amax;
            [JsonProperty("Link to the image or file in the data folder")]
            public string Image;
            [JsonProperty("Opacity of the image")]
            public float Opacity;
        }
        private class GUIOutline
        {
            [JsonProperty("Use Outline")]
            public bool Use = true;
            [JsonProperty("Outline color")]
            public string Color = "0 0 0 1";
            [JsonProperty("Outline distance")]
            public string Distance = "1.0 -1.0";
        }
        #endregion

        private class GUISettings
        {
            [JsonProperty("Backgound for main panel")]
            public GUIPanel Background = new GUIPanel()
            {
                Amin = null,
                Amax = null,
                Color = "0 0 0 0.8"
            };
            [JsonProperty("Main panel settings (shows if player attemts to use blocked cloth/item)")]
            public GUIPanel MainPanel = new GUIPanel()
            {
                Amin = "0.266 0.361",
                Amax = "0.734 0.639",
                Color = "#42e2f49f"
            };
            [JsonProperty("Settings for the text on main panel")]
            public GUIText TextOnMain = new GUIText()
            {
                Amin = "0 0",
                Amax = "1 1",
                Color = "#f4d041",
                Size = 20,
                Outline = new GUIOutline()
            };
            [JsonProperty("Use On Screen Panel")]
            public bool UseOnScreenPanel = true;
            [JsonProperty("Amount of nearest items on the On Screen Panel")]
            public int ItemsAmount = 3;
            [JsonProperty("On Screen Panel (shown if the block is active)")]
            public GUIPanel OnScreenPanel = new GUIPanel()
            {
                Amin = "0.016 0.028",
                Amax = "0.25 0.167",
                Color = "0 0 0 0.7"
            };
            [JsonProperty("Text settings for On Screen Panel")]
            public GUIText TextOnScreen = new GUIText()
            {
                Amin = "0 0",
                Amax = "0.69 1",
                Color = "green",
                Size = 13,
                Outline = new GUIOutline()
            };
            [JsonProperty("Image (shown on On Screen Panel")]
            public GUIImage Image = new GUIImage()
            {
                Amin = "0.71 0.13",
                Amax = "0.96 0.88",
                Opacity = 0.8f,
                Image = "http://www.rigormortis.be/wp-content/uploads/rust-icon-512.png"
            };
        }
        private class PluginConfig
        {
            [JsonProperty("Wipe date")]
            public string DateOfWipeStr;
            [JsonProperty("Chat prefix")]
            public string Prefix = "[Timed Items Blocker]";
            [JsonProperty("Chat prefix color")]
            public string PrefixColor = "#f44253";
            [JsonProperty("Use chat insted of GUI")]
            public bool UseChat = false;
            [JsonProperty("Bypass permission")]
            public string BypassPermission = "timeditemsblocker.bypass";
            [JsonProperty("GUI Settings")]
            public GUISettings Gui = new GUISettings();
            [JsonProperty("List of blocked items")]
            public Dictionary<string, int> BlockedItemsStr;
            [JsonProperty("List of blocked clothes")]
            public Dictionary<string, int> BlockedClothesStr;
            [JsonProperty("List of blocked ammunition")]
            public Dictionary<string, int> BlockedAmmo;

            [JsonIgnore]
            public DateTime DateOfWipe;
            [JsonIgnore]
            public Dictionary<ItemDefinition, int> Blocked;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    DateOfWipeStr = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                    BlockedItemsStr = new Dictionary<string, int>()
                    {
                        ["Satchel Charge"] = 30,
                        ["Timed Explosive Charge"] = 30,
                        ["Eoka Pistol"] = 30,
                        ["Custom SMG"] = 30,
                        ["Assault Rifle"] = 30,
                        ["Bolt Action Rifle"] = 30,
                        ["Waterpipe Shotgun"] = 30,
                        ["Revolver"] = 30,
                        ["Thompson"] = 30,
                        ["Semi-Automatic Rifle"] = 30,
                        ["Semi-Automatic Pistol"] = 30,
                        ["Pump Shotgun"] = 30,
                        ["M249"] = 30,
                        ["Rocket Launcher"] = 30,
                        ["Flame Thrower"] = 30,
                        ["Double Barrel Shotgun"] = 30,
                        ["Beancan Grenade"] = 30,
                        ["F1 Grenade"] = 30,
                        ["MP5A4"] = 30,
                        ["LR-300 Assault Rifle"] = 30,
                        ["M92 Pistol"] = 30,
                        ["Python Revolver"] = 30
                    },
                    BlockedClothesStr = new Dictionary<string, int>()
                    {
                        ["Metal Facemask"] = 30,
                        ["Road Sign Kilt"] = 30,
                        ["Road Sign Jacket"] = 30,
                        ["Metal Chest Plate"] = 30,
                        ["Heavy Plate Pants"] = 30,
                        ["Heavy Plate Jacket"] = 30,
                        ["Heavy Plate Helmet"] = 30,
                        ["Riot Helmet"] = 30,
                        ["Bucket Helmet"] = 30,
                        ["Coffee Can Helmet"] = 30
                    },
                    BlockedAmmo = new Dictionary<string, int>()
                    {
                        ["HV Pistol Ammo"] = 30,
                        ["Incendiary Pistol Bullet"] = 30,
                        ["HV 5.56 Rifle Ammo"] = 30,
                        ["Incendiary 5.56 Rifle Ammo"] = 30,
                        ["Explosive 5.56 Rifle Ammo"] = 30,
                        ["12 Gauge Slug"] = 30,
                        ["High Velocity Arrow"] = 30,
                        ["Incendiary Rocket"] = 30,
                        ["Rocket"] = 30,
                        ["High Velocity Rocket"] = 30
                    }
                };
            }
        }
        #endregion

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ItemBlocked"] = "Using this item is blocked!",
                ["BlockTimeLeft"] = "\n{0}d {1:00}:{2:00}:{3:00} until unblock.",
                ["Weapon line 2"] = "\nYou can only use Hunting bow and Crossbow",
                ["Cloth line 2"] = "\nYou can only use wood and bone armor!",
                ["OnlyPlayer"] = "This command can be executed only from the game!",
                ["OnScreenText"] = "Some of the items are blocked!\nNeares items to unlock:",
                ["OnScreenItem"] = "\n{0} : {1}d {2:00}:{3:00}:{4:00}",
                ["Ammo blocked"] = "The ammo you are trying to use is blocked!"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ItemBlocked"] = "Использование данного предмета заблокировано!",
                ["BlockTimeLeft"] = "\nДо окончания блокировки осталось {0}д. {1:00}:{2:00}:{3:00}",
                ["Weapon line 2"] = "\nВы можете использовать только Лук и Арбалет",
                ["Cloth line 2"] = "\nИспользуйте только деревянную и костяную броню!",
                ["OnlyPlayer"] = "Эту команду можно использовать только в игре!",
                ["OnScreenText"] = "Некоторые предметы заблокированы!\nСкоро будут разблокированы:",
                ["OnScreenItem"] = "\n{0} : {1}д {2:00}:{3:00}:{4:00}",
                ["Ammo blocked"] = "Вид боеприпасов, которые вы пытаетесь использовать заблокирован!"
            }, this, "ru");
        }
        string GetMsg(string key, BasePlayer player = null) => lang.GetMessage(key, this, player?.UserIDString);
        string GetMsg(string key) => lang.GetMessage(key, this);
        #endregion

        #region Config and Data Initialization
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за приобритение плагина на сайте RustPlugin.ru. Если вы приобрели этот плагин на другом ресурсе знайте - это лишает вас гарантированных обновлений!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            DateTime WipeDate;
            if (!DateTime.TryParseExact(config.DateOfWipeStr, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out WipeDate))
      //  Слив плагинов server-rust by Apolo YouGame
            {
                WipeDate = SaveRestore.SaveCreatedTime;
                PrintWarning($"Unable to parse wipe date format, wipe date set to current date: {WipeDate.ToString("dd.MM.yyyy HH:mm:ss")}");
                config.DateOfWipeStr = WipeDate.ToString("dd.MM.yyyy HH:mm:ss");
                SaveConfig();
            }
            config.DateOfWipe = WipeDate;
            permission.RegisterPermission(config.BypassPermission, this);
            var itemdefs = ItemManager.GetItemDefinitions();
            config.Blocked = new Dictionary<ItemDefinition, int>();
            foreach (var item in config.BlockedClothesStr)
            {
                var def = itemdefs.Where(p => p.shortname == item.Key || p.displayName.english == item.Key).FirstOrDefault();
                if (def == null)
                {
                    PrintWarning($"Item \"{item.Key}\" is invalid!");
                    continue;
                }
                if (!config.Blocked.ContainsKey(def))
                    config.Blocked.Add(def, item.Value);
                else
                    PrintWarning($"Item \"{def.displayName.english}\" has multiply recordes in the config! Using {config.Blocked[def]} as the time!");
            }
            foreach (var item in config.BlockedItemsStr)
            {
                var def = itemdefs.Where(p => p.shortname == item.Key || p.displayName.english == item.Key).FirstOrDefault();
                if (def == null)
                {
                    PrintWarning($"Item \"{item.Key}\" is invalid!");
                    continue;
                }
                if (!config.Blocked.ContainsKey(def))
                    config.Blocked.Add(def, item.Value);
                else
                    PrintWarning($"Item \"{def.displayName.english}\" has multiply recordes in the config! Using {config.Blocked[def]} as the time!");
            }
            foreach (var item in config.BlockedAmmo)
            {
                var def = itemdefs.Where(p => p.shortname == item.Key || p.displayName.english == item.Key).FirstOrDefault();
                if (def == null)
                {
                    PrintWarning($"Item \"{item.Key}\" is invalid!");
                    continue;
                }
                if (!config.Blocked.ContainsKey(def))
                    config.Blocked.Add(def, item.Value);
                else
                    PrintWarning($"Item \"{def.displayName.english}\" has multiply recordes in the config! Using {config.Blocked[def]} as the time!");
            }

            if (!string.IsNullOrEmpty(config.Gui.Image.Image))
                if (!config.Gui.Image.Image.ToLower().Contains("http"))
                {
                    config.Gui.Image.Image = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + config.Gui.Image.Image;
                }
            LoadData();
            permission.RegisterPermission("timeditemsblocker.refresh", this);
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        #endregion

        #region Data (Image save\load)
        private void LoadData()
        {
            try
            {
                Image = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, string>>(Title);
            }
            catch(Exception ex)
            {
                if(ex is JsonSerializationException)
                {
                    try
                    {
                        string old = Interface.Oxide.DataFileSystem.ReadObject<string>(Title);
                        Image[config.Gui.Image.Image] = old;
                        SaveData();
                        return;
                    }
                    catch (Exception ex1)
                    {
                        PrintWarning("Failed to convert old data fromat to the new. Data wiped.\n{0}", ex1.Message);
                        Image = new Dictionary<string, string>();
                        return;
                    }
                }
                PrintWarning("Failed to load datafile (is the file corrupt?)\n{0}", ex.GetType());
                Image = new Dictionary<string, string>();
            }
            
        }
        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Title, Image);
        }
        #endregion

        #region Initialization and quiting
        void OnServerInitialized()
        {
            if (!Image.ContainsKey(config.Gui.Image.Image))
                DownloadImage();
            else
                OnScreenPanel(true);
        }
        void Unload() => DestroyAllGui();
        #endregion

        #region Image
        [ConsoleCommand("tib.refresh")]
        private void CmdRefresh(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.player != null)
            {
                BasePlayer player = arg.Connection.player as BasePlayer;

                if (!permission.UserHasPermission(player.UserIDString, "timeditemsblocker.refresh"))
                    return;
            }
            DownloadImage();
        }
        private void DownloadImage()
        {
            PrintWarning("Downloading image...");
            ServerMgr.Instance.StartCoroutine(DownloadImage(config.Gui.Image.Image));
        }
        IEnumerator DownloadImage(string url)
        {
            using (var www = new WWW(url))
            {
                yield return www;
                if (this == null) yield break;
                if (www.error != null)
                {
                    PrintError($"Failed to add image. File address possibly invalide\n {url}");
                }
                else
                {
                    var reply = 0;
                    var tex = www.texture;
                    byte[] bytes = tex.EncodeToPNG();
                    Image[url] = FileStorage.server.Store(www.bytes, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
                    SaveData();
                    PrintWarning("Image download is complete.");
                    OnScreenPanel(true);
                    UnityEngine.Object.DestroyImmediate(tex);
                    yield break;
                }
            }
        }
        #endregion

        #region Oxide hooks
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (Main.ContainsKey(player))
                Main.Remove(player);
            if (OnScreen.Contains(player))
                OnScreen.Remove(player);
        }
        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!InBlock())
                return;
            if (!OnScreen.Contains(player))
                OnScreenPanelMain(player);
        }
        void OnNewSave(string filename)
        {
            config.DateOfWipe = DateTime.Now;
            config.DateOfWipeStr = config.DateOfWipe.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
      //  Слив плагинов server-rust by Apolo YouGame
            SaveConfig();
            PrintWarning($"Wipe detected. Block end set to {config.DateOfWipeStr}");
        }
        object CanEquipItem(PlayerInventory inventory, Item item)
        {
            int BlockEnd;
            if (config.Blocked.TryGetValue(item.info, out BlockEnd))
            {
                if (InBlock(BlockEnd))
                {
                    var player = inventory.GetComponent<BasePlayer>();
                    if (InDuel(player) || IsNPC(player)) return null;
                    if (permission.UserHasPermission(player.UserIDString, config.BypassPermission))
                        return null;
                    string reply = GetMsg("ItemBlocked", player);
                    reply += GetMsg("BlockTimeLeft", player);
                    reply += GetMsg("Weapon line 2", player);
                    if (config.UseChat)
                    {
                        SendToChat(player, reply, BlockEnd);
                    }
                    else
                    {
                        BlockerUI(player, reply, BlockEnd);
                    }
                    return false;
                }
            }
            return null;
        }

        object CanWearItem(PlayerInventory inventory, Item item)
        {
            int BlockEnd;
            if (config.Blocked.TryGetValue(item.info, out BlockEnd))
            {
                if (InBlock(BlockEnd))
                {
                    var player = inventory.GetComponent<BasePlayer>();
                    if (InDuel(player) || IsNPC(player)) return null;
                    if (permission.UserHasPermission(player.UserIDString, config.BypassPermission))
                        return null;
                    string reply = GetMsg("ItemBlocked", player);
                    reply += GetMsg("BlockTimeLeft", player);
                    reply += GetMsg("Cloth line 2", player);
                    if (config.UseChat)
                    {
                        SendToChat(player, reply, BlockEnd);
                    }
                    else
                    {
                        BlockerUI(player, reply, BlockEnd);
                    }
                    return false;
                }
            }
            return null;
        }
        void OnReloadWeapon(BasePlayer player, BaseProjectile projectile)
        {
            if (InDuel(player) || IsNPC(player)) return;
            int BlockEnd;
            if (IsAmmoBlocked(player, projectile, out BlockEnd))
                    SendToChat(player, GetMsg("Ammo blocked", player) + GetMsg("BlockTimeLeft", player), BlockEnd);
        }
        object OnReloadMagazine(BasePlayer player, BaseProjectile projectile)
        {
            if (InDuel(player) || IsNPC(player)) return null;
            int BlockEnd;
            if (IsAmmoBlocked(player, projectile, out BlockEnd))
            {
                projectile.SendNetworkUpdateImmediate();
                return false;
            }
            return null;
        }
        #endregion

        #region GUI

        #region Commands
        [ConsoleCommand("tib.close")]
        private void CmdCloseUI(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
            {
                arg.ReplyWith(GetMsg("OnlyPlayer"));
                return;
            }
            if (Main.ContainsKey(player))
            {
                Main[player]?.Destroy();
                Main.Remove(player);
                CuiHelper.DestroyUi(player, MainParent);
            }
        }
        #endregion

        #region GUI Creation
        private class UI
        {
            private static string ToRustColor(string input)
            {
                Color color;
                if (!ColorUtility.TryParseHtmlString(input, out color))
                {
                    var split = input.Split(' ');
                    for (var i = 0; i < 4; i++)
                    {
                        float num;
                        if (!float.TryParse(split[i], out num))
                        {
                            return null;
                        }
                        color[i] = num;
                    }
                }
                return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
            }

            #region Override to custom classes
            public static void CreatePanel(ref CuiElementContainer container, string Parent, string Name, GUIPanel panel, bool CursorEnabled = false) =>
                CreatePanel(ref container, Parent, Name, panel.Color, panel.Amin, panel.Amax, CursorEnabled);
            public static void CreateImage(ref CuiElementContainer container, string Parent, string Name, GUIImage Image, string Png) =>
                CreateImage(ref container, Parent, Name, Image.Opacity, Image.Amin, Image.Amax, Png);
            public static void CreateText(ref CuiElementContainer container, string Parent, string Name, GUIText TextComp, string Text, TextAnchor Anchor = TextAnchor.MiddleCenter) =>
                CreateText(ref container, Parent, Name, TextComp.Amin, TextComp.Amax, Text, TextComp.Color, TextComp.Size, TextComp.Outline, Anchor);
            #endregion
            public static void CreatePanel(ref CuiElementContainer container, string Parent, string Name, string Color, string Amin, string Amax, bool CursorEnabled = false)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = Amin, AnchorMax = Amax },
                    Image = { Color = ToRustColor(Color) },
                    CursorEnabled = CursorEnabled
                }, Parent, Name);
            }
            public static void CreateImage(ref CuiElementContainer container, string Parent, string Name, float opacity, string Amin, string Amax, string Image)
            {
                var ImageComp = new CuiRawImageComponent { Sprite = "assets/content/textures/generic/fulltransparent.tga", Color = $"1 1 1 {opacity}" };
                if (Image != null)
                {
                    ImageComp.Png = Image;
                }
                container.Add(new CuiElement
                {
                    Name = Name ?? CuiHelper.GetGuid(),
                    Parent = Parent,
                    Components = { ImageComp, new CuiRectTransformComponent { AnchorMin = Amin, AnchorMax = Amax } }
                });
            }
            public static void CreateFulscreenButton(ref CuiElementContainer container, string Parent, string Name, string Command)
            {
                container.Add(new CuiButton
                {
                    Button = { Command = Command, Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = "" }
                }, Parent, Name);
            }
            public static void CreateText(ref CuiElementContainer container, string Parent, string Name, string Amin, string Amax, string Text, string TextColor, int FontSize, GUIOutline outline = null, TextAnchor Anchor = TextAnchor.MiddleCenter)
            {
                var Element = new CuiElement
                {
                    Parent = Parent,
                    Name = Name ?? CuiHelper.GetGuid(),
                    Components =
                    {
                        new CuiTextComponent { Color = ToRustColor(TextColor), FontSize = FontSize, Text = Text, Align = Anchor },
                        new CuiRectTransformComponent { AnchorMin = Amin, AnchorMax = Amax }
                    }
                };
                if (outline != null && outline.Use)
                {
                    Element.Components.Add(
                        new CuiOutlineComponent { Color = ToRustColor(outline.Color), Distance = outline.Distance });
                }
                container.Add(Element);
            }
        }
        #endregion

        #region PanelNames
        private string MainParent = "timeditemsblocker.main";
        private string MainPanel = "timeditemsblocker.panel";
        private string MainText = "timeditemsblocker.text";
        private string OnScreenParent = "timeditemsblocker.ONS";
        private string OnScreenText = "timeditemsblocker.ONSText";
        #endregion

        private void DestroyAllGui()
        {
            OnScreenUpdater?.Destroy();
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, OnScreenParent);
                if (Main.ContainsKey(player))
                {
                    Main[player]?.Destroy();
                    Main.Remove(player);
                    CuiHelper.DestroyUi(player, MainParent);
                }
            }
        }
        private void OnScreenPanel(bool init = false)
        {
            if (!config.Gui.UseOnScreenPanel)
                return;
            if (init)
                foreach (var player in BasePlayer.activePlayerList)
                    OnScreenPanelMain(player);
            var nearest = GetNearesUnblock;
            if (nearest.Count == 0)
            {
                DestroyAllGui();
                return;
            }
            foreach (var player in OnScreen)
            {
                CuiHelper.DestroyUi(player, OnScreenText);
                var container = new CuiElementContainer();
                string text = GetMsg("OnScreenText", player);
                foreach (var item in nearest)
                {
                    var timeleft = TimeLeft(item.Value);
                    text += string.Format(GetMsg("OnScreenItem", player), item.Key.displayName.english,
                        timeleft.Days, timeleft.Hours, timeleft.Minutes, timeleft.Seconds);
                }
                UI.CreateText(ref container, OnScreenParent, OnScreenText, config.Gui.TextOnScreen, text);
                CuiHelper.AddUi(player, container);
            }
            OnScreenUpdater = timer.Once(1f, () => OnScreenPanel());
        }
        private void OnScreenPanelMain(BasePlayer player)
        {
            if (!config.Gui.UseOnScreenPanel)
                return;
            CuiHelper.DestroyUi(player, OnScreenParent);
            var container = new CuiElementContainer();
            UI.CreatePanel(ref container, "Hud", OnScreenParent, config.Gui.OnScreenPanel);
            if(Image.ContainsKey(config.Gui.Image.Image))
                UI.CreateImage(ref container, OnScreenParent, null, config.Gui.Image, Image[config.Gui.Image.Image]);
            CuiHelper.AddUi(player, container);
            OnScreen.Add(player);
        }
        private void PrepareMain(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, MainParent);
            var container = new CuiElementContainer();
            UI.CreatePanel(ref container, "Overlay", MainParent, config.Gui.Background.Color, "0 0", "1 1", true);
            UI.CreatePanel(ref container, MainParent, MainPanel, config.Gui.MainPanel);
            UI.CreateFulscreenButton(ref container, MainParent, null, "tib.close");
            CuiHelper.AddUi(player, container);
        }
        private void BlockerUI(BasePlayer player, string Text, int BlockEnd)
        {
            if (!Main.ContainsKey(player))
                PrepareMain(player);
            CuiHelper.DestroyUi(player, MainText);
            var timeleft = TimeLeft(BlockEnd);
            var container = new CuiElementContainer();
            UI.CreateText(ref container, MainPanel, MainText, config.Gui.TextOnMain,
                string.Format(Text, timeleft.Days, timeleft.Hours, timeleft.Minutes, timeleft.Seconds));
            CuiHelper.AddUi(player, container);
            Main[player] = timer.Once(1f, () => BlockerUI(player, Text, BlockEnd));
        }

        #endregion

        #region Helpers
        private bool IsAmmoBlocked(BasePlayer owner, BaseProjectile proj, out int BlockEnd)
        {
            List<Item> currentAmmo = owner.inventory.FindItemIDs(proj.primaryMagazine.ammoType.itemid).ToList();
            Item newAmmo = null;
            BlockEnd = 0;
            if (currentAmmo.Count == 0)
            {
                List<Item> newAmmoList = new List<Item>();
                owner.inventory.FindAmmo(newAmmoList, proj.primaryMagazine.definition.ammoTypes);
                if (newAmmoList.Count == 0)
                    return false;
                newAmmo = newAmmoList[0];
            }
            else
                newAmmo = currentAmmo[0];

            if (config.Blocked.TryGetValue(newAmmo.info, out BlockEnd))
            {
                if (InBlock(BlockEnd))
                    return true;
            }
            return false;
        }
        private Dictionary<ItemDefinition, int> GetNearesUnblock
        {
            get
            {
                return config.Blocked.OrderBy(p => p.Value).Where(p =>
                InBlock(p.Value)).Take(config.Gui.ItemsAmount).ToDictionary(x => x.Key, x => x.Value);
            }
        }
        private bool IsNPC(BasePlayer player)
        {
            //BotSpawn
            if (player is NPCPlayer)
                return true;
            //HumanNPC
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L))
                return true;
            return false;
        }
        private bool InDuel(BasePlayer player)
        {
            if (Duel == null) return false;
            return (bool)Duel.Call("IsPlayerOnActiveDuel", player);
        }
        private bool InBlock() => GetNearesUnblock.Count > 0;
        private bool InBlock(int EndTime)
        {
            if (TimeLeft(EndTime).TotalSeconds >= 0)
            {
                return true;
            }
            return false;
        }
        TimeSpan TimeLeft(int hours) => config.DateOfWipe.AddHours(hours).Subtract(DateTime.Now);
        private void SendToChat(BasePlayer Player, string Message, int BlockEnd)
        {
            var timeleft = TimeLeft(BlockEnd);
            Message = string.Format(Message, timeleft.Days, timeleft.Hours, timeleft.Minutes, timeleft.Seconds);
            PrintToChat(Player, "<color=" + config.PrefixColor + ">" + config.Prefix + "</color> " + Message);
        }
        #endregion
    }
}
