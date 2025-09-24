// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("WipeBlock", "Fartus", "1.0.0")]
    [Description("Временная блокировка предметов после вайпа")]

    class WipeBlock : RustPlugin
    {
        #region Vars
        private Dictionary<BasePlayer, Timer> Main = new Dictionary<BasePlayer, Timer>();
        private List<BasePlayer> OnScreen = new List<BasePlayer>();
        private Timer OnScreenUpdater;
        private PluginConfig config;
        [PluginReference] Plugin Duel;
        #endregion

        #region Config setup
        private class GUIPanel
        {
            [JsonProperty("Положение панели (Anchor Min)")]
            public string Amin;
            [JsonProperty("Положение панели (Anchor Max)")]
            public string Amax;
            [JsonProperty("Цвет панели")]
            public string Color;
        }
        private class GUIText : GUIPanel
        {
            [JsonProperty("Размер текста")]
            public int Size;
            [JsonProperty("Обводка текста")]
            public GUIOutline Outline;
        }
        private class GUIOutline
        {
            [JsonProperty("Использовать обводку текста")]
            public bool Use = true;
            [JsonProperty("Цвет обводки текста")]
            public string Color = "0 0 0 1";
            [JsonProperty("Расстояние обводки текста")]
            public string Distance = "1.0 -1.0";
        }
        #endregion
		
        #region GUI Settings
        private class GUISettings
        {
            [JsonProperty("Фон для главной панели")]
            public GUIPanel Background = new GUIPanel()
            {
                Amin = null,
                Amax = null,
                Color = "0 0 0 0.8"
            };
            [JsonProperty("Настройки главной панели (отображается, если игрок пытается использовать заблокированный предмет)")]
            public GUIPanel MainPanel = new GUIPanel()
            {
                Amin = "0.266 0.361",
                Amax = "0.734 0.639",
                Color = "#42e2f49f"
            };
            [JsonProperty("Настройки текста главной панели")]
            public GUIText TextOnMain = new GUIText()
            {
                Amin = "0 0",
                Amax = "1 1",
                Color = "#f4d041",
                Size = 20,
                Outline = new GUIOutline()
            };
            [JsonProperty("Использовать панель на экране")]
            public bool UseOnScreenPanel = true;
            [JsonProperty("Количество отоброжаемых предметов для ближайшей разблокировки")]
            public int ItemsAmount = 3;
            [JsonProperty("Настройки панели на экране (отображается, если активен блок)")]
            public GUIPanel OnScreenPanel = new GUIPanel()
            {
                Amin = "0.65 0.028",
                Amax = "0.83 0.155",
                Color = "0 0 0 0.2"
            };
            [JsonProperty("Настройки текста панели на экране")]
            public GUIText TextOnScreen = new GUIText()
            {
                Amin = "0 0",
                Amax = "0.96 1",
                Color = "#dbd2cc",
                Size = 13,
                Outline = new GUIOutline()
            };
        }
        private class PluginConfig
        {
            [JsonProperty("Дата вайпа")]
            public string DateOfWipeStr;
            [JsonProperty("Префикс чата")]
            public string Prefix = "[WipeBlock]";
            [JsonProperty("Цвет префикса")]
            public string PrefixColor = "#f44253";
            [JsonProperty("Использовать чат вместо GUI")]
            public bool UseChat = false;
            [JsonProperty("Привилегия для обхода блока")]
            public string BypassPermission = "wipeblock.bypass";
            [JsonProperty("Настройки GUI")]
            public GUISettings Gui = new GUISettings();
            [JsonProperty("Список заблокированного оружия")]
            public Dictionary<string, int> BlockedItemsStr;
            [JsonProperty("Список заблокированных вещей")]
            public Dictionary<string, int> BlockedClothesStr;
            [JsonProperty("Список заблокированных боеприпасов")]
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
            PrintWarning("Создание нового файла конфигурации...");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            DateTime WipeDate;
            if (!DateTime.TryParseExact(config.DateOfWipeStr, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out WipeDate))
            {
                WipeDate = SaveRestore.SaveCreatedTime;
                PrintWarning($"Невозможно разобрать формат даты вайпа, дата вайпа установлена на текущую дату: {WipeDate.ToString("dd.MM.yyyy HH:mm:ss")}");
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
                    PrintWarning($"Предмет \"{item.Key}\" неверен!");
                    continue;
                }
                if (!config.Blocked.ContainsKey(def))
                    config.Blocked.Add(def, item.Value);
                else
                    PrintWarning($"Предмет \"{def.displayName.english}\" имеет несколько записей в конфиге! Используйте {config.Blocked[def]} в данное время!");
            }
            foreach (var item in config.BlockedItemsStr)
            {
                var def = itemdefs.Where(p => p.shortname == item.Key || p.displayName.english == item.Key).FirstOrDefault();
                if (def == null)
                {
                    PrintWarning($"Предмет \"{item.Key}\" неверен!");
                    continue;
                }
                if (!config.Blocked.ContainsKey(def))
                    config.Blocked.Add(def, item.Value);
                else
                    PrintWarning($"Предмет \"{def.displayName.english}\" имеет несколько записей в конфиге! Используйте {config.Blocked[def]} в данное время!");
            }
            foreach (var item in config.BlockedAmmo)
            {
                var def = itemdefs.Where(p => p.shortname == item.Key || p.displayName.english == item.Key).FirstOrDefault();
                if (def == null)
                {
                    PrintWarning($"Предмет \"{item.Key}\" неверен!");
                    continue;
                }
                if (!config.Blocked.ContainsKey(def))
                    config.Blocked.Add(def, item.Value);
                else
                    PrintWarning($"Предмет \"{def.displayName.english}\" имеет несколько записей в конфиге! Используйте {config.Blocked[def]} в данное время!");
            }
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        #endregion

        #region Oxide hooks
        void OnServerInitialized()
        {
			OnScreenPanel(true);
        }
        void Unload()
		{
			DestroyAllGui();
		}
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
            SaveConfig();
            PrintWarning($"Обнаружен вайп. Блок оружия установлен до {config.DateOfWipeStr}");
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

            public static void CreatePanel(ref CuiElementContainer container, string Parent, string Name, GUIPanel panel, bool CursorEnabled = false) =>
                CreatePanel(ref container, Parent, Name, panel.Color, panel.Amin, panel.Amax, CursorEnabled);
            public static void CreateText(ref CuiElementContainer container, string Parent, string Name, GUIText TextComp, string Text, TextAnchor Anchor = TextAnchor.MiddleCenter) =>
                CreateText(ref container, Parent, Name, TextComp.Amin, TextComp.Amax, Text, TextComp.Color, TextComp.Size, TextComp.Outline, Anchor);

            public static void CreatePanel(ref CuiElementContainer container, string Parent, string Name, string Color, string Amin, string Amax, bool CursorEnabled = false)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = Amin, AnchorMax = Amax },
                    Image = { Color = ToRustColor(Color) },
                    CursorEnabled = CursorEnabled
                }, Parent, Name);
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

        private string MainParent = "wipeblock.main";
        private string MainPanel = "wipeblock.panel";
        private string MainText = "wipeblock.text";
        private string OnScreenParent = "wipeblock.ONS";
        private string OnScreenText = "wipeblock.ONSText";

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
        private bool IsNPC(BasePlayer player) //NPC Spawn
        { 
            if (player is NPCPlayer)
                return true;
  
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
