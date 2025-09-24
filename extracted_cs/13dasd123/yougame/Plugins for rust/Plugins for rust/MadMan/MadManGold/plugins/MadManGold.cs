using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Core;
using System.IO;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("MadManGold", "RustPlugin.ru -Additions by Vlad-00003", "1.0.0")]
    [Description("MadMan GoldEdition by Vlad-00003")]
    public class MadManGold : RustPlugin
    {

        [PluginReference]
        Plugin LustyMap;
        [PluginReference]
        Plugin Map;

        #region Config Setup

        private static int maxTime = 900;
        private string IconName = "MadMan";
        private string Icon = "mad.png";
        private static bool PanelAlwaysVisible = true;
        private static int PanelHideTime = 15;
        private static string Prefix = "[<color=red>MadMan</color> <color=#c48f00>Gold</color>]";

        private static string GuiMadAmin = "0.3 0.35";
        private static string GuiMadAmax = "0.7 0.65";
        private static string GuiMadBackColor = "0.00 0.00 0.00 0.80";
		
        private static string GuiPanelMadAmin = "0.8 0.91";
        private static string GuiPanelMadAmax = "0.990 0.990";
        private static string GuiPanelMadBackColor = "0.00 0.00 0.00 0.80";
		
        private static string GuiPanelPlayerAmin = "0.755 0.85";
        private static string GuiPanelPlayerAmax = "0.990 0.990";
        private static string GuiPanelPlayerBackColor = "0.00 0.00 0.00 0.80";
        private void LoadConfigValues()
        {
            GetConfig("Основные настройки", "Продолжительность безумия", ref maxTime);
            GetConfig("Основные настройки", "Имя безумца на карте", ref IconName);
            GetConfig("Основные настройки", "Иконка на карте (ссылка или имя файла в data)", ref Icon);
            GetConfig("Основные настройки", "Префикс в чате", ref Prefix);
            GetConfig("Основные настройки", "Тип активации инфо панели (true - панель видна постоянно, false - панель открывается только по команде /mad)", ref PanelAlwaysVisible);
            GetConfig("Основные настройки", "Таймер исчазновения панели", ref PanelHideTime);

            GetConfig("GUI для безумца (показывается когда игрок становится безумным)", "Минимальный отступ", ref GuiMadAmin);
            GetConfig("GUI для безумца (показывается когда игрок становится безумным)", "Максимальный отступ", ref GuiMadAmax);
            GetConfig("GUI для безумца (показывается когда игрок становится безумным)", "Цвет фона", ref GuiMadBackColor);

            GetConfig("GUI для безумца (Постоянно на экране)", "Минимальный отступ", ref GuiPanelMadAmin);
            GetConfig("GUI для безумца (Постоянно на экране)", "Максимальный отступ", ref GuiPanelMadAmax);
            GetConfig("GUI для безумца (Постоянно на экране)", "Цвет фона", ref GuiPanelMadBackColor);

            GetConfig("GUI для игроков", "Минимальный отступ", ref GuiPanelPlayerAmin);
            GetConfig("GUI для игроков", "Максимальный отступ", ref GuiPanelPlayerAmax);
            GetConfig("GUI для игроков", "Цвет фона", ref GuiPanelPlayerBackColor);
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за приобритение плагина на сайте RustPlugin.ru. Если вы приобрели этот плагин на другом ресурсе знайте - это лишает вас гарантированных обновлений!");
        }
        #endregion

        #region localization
        private static string msgCooldown = "Команда будет доступна через {0} сек.";
        private static string msgStop = "Мод остановлен";
        private static string msgError = "Произошла ошибка. Обратитесь к администратору";
        private static string msgNoOne = "Пока что никто не безумен.";
        private static string msgYouAre = "Ты безумный человек.\nТвои выстрелы наносят <color=red>2x</color> урона!\nТвоя защита от выстрелов на <color=red>50%</color> сильнее!\nУбивший тебя получит твою способность!\nЧерез <color=red>{0}</color> мин ты потеряешь способность!";
        private static string msgChatCommand = "Безумный человек: <color=#7b9ef6>{0}</color>\nРасстояние до него: <color=red>{1}</color> м. <color=green>{2}</color>.\nВы также можете найти его на карте.\nДо смены безумца: <color=#c48f00>{3}</color>";
        private static string msgForNew = "Ты обезумел!\nТвои выстрелы наносят <color=red>2x</color> урона!\nТвоя защита от выстрелов на <color=red>50%</color> сильнее!\nУбивший тебя получит твою способность!\nЧерез <color=red>{0}</color> мин ты потеряешь способность!";
        private static string msgYouLost = "Ты потерял своё безумие!";
        private static string msgAnnounce = "\n<color=#7b9ef6>{0}</color> безумный человек!\nЕго выстрелы наносят <color=red>2x</color> урона,\nЕго защита от пуль на <color=red>50%</color> сильнее!\nУбейте его, чтобы получить его способности!\nИнформация о нём: <color=lime>/mad</color>";
        private static string msgTimeOut = "Время вышло.\nТы больше не безумен!";
        private static string msgPanelMad = "Ты безумный человек!\nДо конца безумия осталось <color=#c48f00>{0}</color>";
        private static string msgPanelPlayer = "Безумный человек: <color=#7b9ef6>{0}</color>\nРасстояние до него: <color=red>{1}</color> м.\nВы также можете найти его на карте.\nДо смены безумца: <color=#c48f00>{2}</color>";

        #endregion

        #region Variables

        static Man someMan = null;
        private bool IconCreated = false;
        private static Dictionary<ulong, float> antiFlood = new Dictionary<ulong, float>();
        private static Dictionary<ulong, Timer> OpenedPanel = new Dictionary<ulong, Timer>();

        #endregion

        #region Chat Command

        [ChatCommand("mad")]
        void RunChat(BasePlayer player)
        {
            float now = Time.realtimeSinceStartup;
            float value;
            string msg;
            if (antiFlood.TryGetValue(player.userID, out value))
            {
                if (now - value < 4f)
                {
                    var difference = 5 - (now - value);
                    msgPlayer(player, string.Format(msgCooldown, (int)difference));
                    return;
                }
            }
            antiFlood[player.userID] = now;
            if (someMan == null)
            {
                msgPlayer(player, msgError);
                return;
            }
            
            if (!someMan.isFound)
            {
                msgPlayer(player, msgNoOne);
                return;
            }
            if (someMan.player == player)
            {
                msg = string.Format(msgYouAre, someMan.GetFormatTime());
                msgPlayer(player, msg);
                return;
            }
            var from = someMan.player.transform.position;
            from.y += 750;
            var distance = Vector3.Distance(player.transform.position, someMan.player.transform.position);
            msg = string.Format(msgChatCommand, someMan.player.displayName, (int)distance, someMan.player.transform.position, someMan.GetFormatTime());
            msgPlayer(player, msg);
            if (!PanelAlwaysVisible)
            {
                if (OpenedPanel.ContainsKey(player.userID))
                {
                    OpenedPanel[player.userID].Destroy();
                    CuiHelper.DestroyUi(player, "MadPanel");
                    OpenedPanel.Remove(player.userID);
                    return;
                }

                int i = 0;
                OpenedPanel.Add(player.userID, timer.Repeat(1f, PanelHideTime, () =>
                {
                    i++;
                    from = someMan.player.transform.position;
                    from.y += 750;
                    distance = Vector3.Distance(player.transform.position, someMan.player.transform.position);
                    msg = string.Format(msgPanelPlayer, someMan.player.displayName, (int)distance, someMan.GetFormatTime());
                    ShowPanel(player, GuiPanelPlayerAmin, GuiPanelPlayerAmax, GuiPanelPlayerBackColor, msg);
                    if (i == PanelHideTime)
                    {
                        OpenedPanel.Remove(player.userID);
                        CuiHelper.DestroyUi(player, "MadPanel");
                    }
                }));
            }
        }
        

        #endregion

        #region Man class

        public class Man : MonoBehaviour
        {
            public BasePlayer player;
            public BasePlayer previousPlayer;
            public float startTime;
            public bool isFound = false;
            
            void Awake()
            {
                someMan = this;
                FindNewMan();
                Notify();
                Update3Sec();
            }

            public string GetFormatTime()
            {
                var time = maxTime - (Time.realtimeSinceStartup - startTime);
                TimeSpan timeleft = DateTime.Now.AddSeconds(time) - DateTime.Now;
                return string.Format("{0:00}:{1:00}", timeleft.Minutes, timeleft.Seconds);
            }

            public void OnManDeath(BasePlayer attacker)
            {
                isFound = false;
                msgPlayer(player, msgYouLost);
                if (attacker != previousPlayer)
                {
                    player = attacker;
                    startTime = Time.realtimeSinceStartup;
                    isFound = true;
                    var msg = string.Format(msgForNew, GetFormatTime());
                    msgPlayer(attacker, msg);
                    ShowGui(attacker,GuiMadAmin,GuiMadAmax,GuiMadBackColor, msg);
                }
                else
                {
                    FindNewMan();
                }
            }

            void Notify()
            {
                Invoke("Notify", 900f);
                if (!isFound) return;
                msgAllButMad(string.Format(msgAnnounce, player.displayName));
            }

            public void FindNewMan()
            {
                isFound = false;
                List<BasePlayer> goodPl = new List<BasePlayer>();
                foreach (var pl in BasePlayer.activePlayerList)
                {
                    if (pl.IsWounded() || pl.IsSleeping() || pl.IsDead() || previousPlayer == pl) continue;
                    goodPl.Add(pl);
                }
                if (goodPl.Count == 0)
                {
                    Invoke("FindNewMan", 10f);
                    return;
                }
                if (player != null)
                previousPlayer = player;
                var random = UnityEngine.Random.Range(0, goodPl.Count);
                player = goodPl[random];
                isFound = true;
                someMan = this;
                startTime = Time.realtimeSinceStartup;
                var msg = string.Format(msgForNew, GetFormatTime());
                msgPlayer(player, msg);
                ShowGui(player,GuiMadAmin,GuiMadAmax,GuiMadBackColor, msg);
            }

            void Update3Sec()
            {
                Invoke("Update3Sec", 3f);
                if (!isFound) return;
                if (!player.IsConnected)
                {
                    FindNewMan();
                    return;
                }
                if (Time.realtimeSinceStartup - startTime > maxTime)
                {
                    msgPlayer(player, msgTimeOut);
                    FindNewMan();
                    return;
                }
            }
            
            public void Destroy(string reason = null)
            {
                if (reason != null) msgAll(reason);
                Destroy(this);
            }
        }

        #endregion
        
        #region Gui
        private static void ShowGui(BasePlayer player,string amin, string amax,string backgroundColor, string text)
        {
            CuiHelper.DestroyUi(player, "MadGui");
            var elements = new CuiElementContainer();
            var panel = elements.Add(new CuiPanel
            {
                Image = { Color = backgroundColor },
                RectTransform = { AnchorMin = amin, AnchorMax = amax }, CursorEnabled = true
            },  "Overlay", "MadGui");
            elements.Add(new CuiLabel
            {
                Text = { Text = text, FontSize = 24, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0.3", AnchorMax = "1 1" }
            }, panel);
            elements.Add(new CuiButton
            {
                Button = { Close = "MadGui", Color = "0 0 0 0.70" },
                RectTransform = { AnchorMin = "0.42 0.05", AnchorMax = "0.58 0.21" },
                Text = { Text = "Я понял!", FontSize = 20, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            }, panel);
            CuiHelper.AddUi(player, elements);
        }
        private static void ShowPanel(BasePlayer player,string amin, string amax, string backgroundColor, string text)
        {
            CuiHelper.DestroyUi(player, "MadPanel");
            var elements = new CuiElementContainer();
            var panel = elements.Add(new CuiPanel
            {
                Image = { Color = backgroundColor },
                RectTransform = { AnchorMin = amin, AnchorMax = amax },
                CursorEnabled = false
            }, "Overlay", "MadPanel");
            elements.Add(new CuiLabel
            {
                Text = { Text = text, FontSize = 16, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, panel);
            CuiHelper.AddUi(player, elements);
        }
        #endregion

        #region Oxide

        void OnEntityTakeDamage(BaseCombatEntity victim, HitInfo hitInfo)
        {
            if (someMan == null) return;
            if (!someMan.isFound) return;
            BasePlayer victimPlayer = (victim as BasePlayer);
            if (victimPlayer == null) return;
            if (victimPlayer == someMan.player)
            {
                var dmgType = hitInfo.damageTypes.GetMajorityDamageType();
                if (dmgType != Rust.DamageType.Bullet) return;
                hitInfo.damageTypes.ScaleAll(0.5f);
            }
            BasePlayer attacker = (hitInfo?.Initiator as BasePlayer);
            if (attacker == someMan.player)
            {
                var dmgType = hitInfo.damageTypes.GetMajorityDamageType();
                if (dmgType != Rust.DamageType.Bullet) return;
                hitInfo.damageTypes.ScaleAll(2f);
            }
        }

        void OnEntityDeath(BaseEntity entity, HitInfo hitinfo)
        {
            if (someMan == null) return;
            if (!someMan.isFound) return;
            BasePlayer player = (entity as BasePlayer);
            if (player == null) return;
            if (player != someMan.player) return;
            BasePlayer attacker = (hitinfo?.Initiator as BasePlayer);
            if (attacker != null)
            {
                if (attacker == player) return;
                someMan.OnManDeath(attacker);
            }
        }
        
        void OnServerInitialized()
        {
            var entities = BaseNetworkable.serverEntities.ToList();
            Man man = entities[0].GetComponent<Man>() ?? entities[0].gameObject.AddComponent<Man>();
            if(LustyMap == null && Map == null)
            {
                PrintWarning("No map plugin found. Map icons would not work.");
            }
            LoadConfigValues();

            timer.Repeat(1f, 0, () => { UpdateAll(); });

            if (!Icon.ToLower().Contains("http"))
            {
                Icon = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + Icon;
            }
        }
        void Unloaded()
        {
            foreach (var pl in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(pl, "MadGui");
                CuiHelper.DestroyUi(pl, "MadPanel");
            }
            someMan.Destroy(msgStop);
            RemoveMapMarker();
            RemoveLustyMapMarker();
        }
        #endregion

        #region Hud and Map
        private void UpdateAll()
        {
            string msg;
            if (someMan == null) return;
            if (!someMan.isFound)
            {
                RemoveLustyMapMarker();
                RemoveMapMarker();
                IconCreated = false;
                foreach(var pl in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(pl, "MadPanel");
                }
                return;
            }
            if (!IconCreated)
            {
                AddLustyMapMarker();
                AddMapMarker();
                IconCreated = true;
            }else
            {
                UpdateLustyMapMarker();
                UpdateMapMarker();
            }
            if (PanelAlwaysVisible)
            {
                foreach (BasePlayer pl in BasePlayer.activePlayerList)
                {
                    if (pl == someMan.player) continue;
                    var from = someMan.player.transform.position;
                    from.y += 750;
                    var distance = Vector3.Distance(pl.transform.position, someMan.player.transform.position);
                    msg = string.Format(msgPanelPlayer, someMan.player.displayName, (int)distance, someMan.GetFormatTime());
                    ShowPanel(pl,GuiPanelPlayerAmin,GuiPanelPlayerAmax,GuiPanelPlayerBackColor, msg);
                }
            }
            msg = string.Format(msgPanelMad, someMan.GetFormatTime());
            ShowPanel(someMan.player, GuiPanelMadAmin, GuiPanelMadAmax,GuiPanelMadBackColor, msg);
        }
        public void AddLustyMapMarker() => LustyMap?.Call("AddMarker", someMan.player.transform.position.x, someMan.player.transform.position.z, IconName, Icon);
        public void RemoveLustyMapMarker() => LustyMap?.Call("RemoveMarker", "MadMan");
        public void UpdateLustyMapMarker() => LustyMap?.Call("UpdateMarker", someMan.player.transform.position.x, someMan.player.transform.position.z, IconName, Icon);
        private void AddMapMarker() => Map?.Call("ApiAddPointUrl", Icon, IconName, someMan.player.transform.position);
        private void UpdateMapMarker() => Map?.Call("ApiUpdatePointUrl", Icon, IconName, someMan.player.transform.position);
        private void RemoveMapMarker() => Map?.Call("ApiRemovePointUrl", Icon, IconName, someMan.player.transform.position);
        #endregion

        #region Helpers
        private void GetConfig<T>(string MainMenu, string Key, ref T var)
        {
            if (Config[MainMenu,Key] != null)
            {
                var = (T)Convert.ChangeType(Config[MainMenu, Key], typeof(T));
            }
            Config[MainMenu, Key] = var;
        }

        public static void msgPlayer(BasePlayer player, string msg)
        {
            player.ChatMessage($"{Prefix} {msg}");
        }

        public static void msgAll(string msg)
        {
            ConsoleNetwork.BroadcastToAllClients("chat.add", 0, $"{Prefix} {msg}");
        }
        public static void msgAllButMad(string msg)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player != someMan.player) player.ChatMessage(msg);
            }
        }
        #endregion
    }
}