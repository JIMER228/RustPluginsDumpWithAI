using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Oxide;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("CustomEvents", "https://discord.gg/dNGbxafuJn", "1.0.0")]
    [Description("New events for your server")]
    class CustomEvents : RustPlugin
    {
        #region Classes
        private class EventsHandler : MonoBehaviour
        {
            private double EventsTime = _instance.config.evTime;
            public string CurEvent = "";
            public string CurEventDescription = "";
            private string UI_Layer = "UI_EventsLayer";
            public Dictionary<BasePlayer, int> playerScores = new Dictionary<BasePlayer, int>();

            private void Awake()
            {
                if(playerScores != null) playerScores.Clear();
                InvokeRepeating(nameof(EventHandler), 1f, 1f);
            }

            private void EventHandler()
            {
                if (CurEvent == "") return;

                EventsTime--;
                
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (playerScores.ContainsKey(player) && CurEvent == _instance.lang.GetMessage("KingEvent", _instance)) playerScores[player] = (int)player.transform.position.y;
                    
                    DrawUI(player);
                }

                if (EventsTime < 1)
                {
                    CancelInvoke(nameof(EventHandler));
                    _instance.EndEvent();
                }
            }

            private void DrawUI(BasePlayer player)
            {
                if (playerScores == null || player == null || playerScores.Count < 1) return;
                
                CuiElementContainer container = new CuiElementContainer
                {
                    {
                        new CuiPanel
                        {
                            CursorEnabled = false,
                            RectTransform = { AnchorMin = "0.01 0.8", AnchorMax = "0.01 0.8", OffsetMin = "0 -150", OffsetMax = "300 0" },

                            Image = { Color = GetColor("#000000", 0.0f) }
                        },
                        "Hud", UI_Layer
                    },
                    {
                        new CuiElement
                        {
                            Parent = UI_Layer,
                            Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"{_instance.lang.GetMessage("EventTitle", _instance)} {CurEvent} ({EventsTime}):\n{CurEventDescription}\n{GetBestPlayers(playerScores)}\n{(playerScores.ContainsKey(player)?$"Ваш счёт: {playerScores[player]}":"Вы не участвуете.")}",
                                    Align = TextAnchor.UpperLeft,
                                    FontSize = 16,
                                    Font = "robotocondensed-bold.ttf",
                                    Color = GetColor("#FFFFFF", 1f)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 0",
                                    AnchorMax = $"1 1"
                                },
                                new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                            }
                        }
                    }

                };

                CuiHelper.DestroyUi(player, UI_Layer);
                CuiHelper.AddUi(player, container);
            }

            private string GetBestPlayers(Dictionary<BasePlayer, int> scores)
            {
                Dictionary<BasePlayer, int> players = new Dictionary<BasePlayer, int>(scores);
                string str = "";
                var mySortedList = players.OrderByDescending(p => p.Value).Take(3);
                int i = 1;
                foreach (var n in mySortedList)
                {
                    str += $"{i++}. {n.Key.displayName} ({n.Value})\n";
                }

                return str;
            }

            public static BasePlayer KeyByValue(Dictionary<BasePlayer, int> dict, int val)
            {
                BasePlayer key = null;
                foreach (KeyValuePair<BasePlayer, int> pair in dict)
                {
                    if (pair.Value == val)
                    {
                        key = pair.Key;
                        break;
                    }
                }
                return key;
            }

            private static string GetColor(string hex, float alpha = 1f)
            {
                var color = ColorTranslator.FromHtml(hex);
                var r = Convert.ToInt16(color.R) / 255f;
                var g = Convert.ToInt16(color.G) / 255f;
                var b = Convert.ToInt16(color.B) / 255f;

                return $"{r} {g} {b} {alpha}";
            }
        }

        private class PluginConfig
        {
            [JsonProperty("Events Timer (How often in seconds)")]
            public float timerStart = 7200f;
            [JsonProperty("Min players to start")]
            public int minPlayers = 5;
            [JsonProperty("Event duration")]
            public double evTime = 300.0;
            [JsonProperty("Events price amount (next winner will get less)")]
            public double evPrice = 500.0;
            [JsonProperty("Events max winners")]
            public int maxWinners = 3;
            [JsonProperty("Name of plugin to give")]
            public string evPriceType = "UniversalShop";
            [JsonProperty("Call function")]
            public string evPriceCallFunc = "API_ShopAddBalance";
        }
        #endregion

        #region Variables
        private static CustomEvents _instance;
        GameObject controller;
        Timer EventsTimer;

        private Dictionary<string, string> EventsList;

        private PluginConfig config;
        #endregion

        #region OxideHooks
        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig();
        }

        void OnServerInitialized()
        {
            _instance = this;
            controller = new GameObject();
            
            EventsList = new Dictionary<string, string>
            {
                 [lang.GetMessage("KingEvent", this)] = lang.GetMessage("KingDesc", this),
                 [lang.GetMessage("GatherEvent", this)] = lang.GetMessage("GatherDesc", this),
                 [lang.GetMessage("ComponentEvent", this)] = lang.GetMessage("ComponentDesc", this),
                 [lang.GetMessage("HuntEvent", this)] = lang.GetMessage("HuntDesc", this)
            };

            EventsTimer = timer.Once(config.timerStart, StartEvent);
        }

        private void Unload()
        {
            if (controller != null)
            {
                var component = controller.GetComponent<EventsHandler>();
                if (component != null) UnityEngine.Object.Destroy(component);
            }
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["GatherEvent"] = "<color=#F79F81>Resource Farm</color>",
                ["GatherDesc"] = "Get as many resources as possible!",
                ["ComponentEvent"] = "<color=#ACFA58>Components Farm</color>",
                ["ComponentDesc"] = "Loot as much as possible scrap!",
                ["HuntEvent"] = "<color=#00BFFF>Animals hunt</color>",
                ["HuntDesc"] = "Kill animals! Human isn't animal :D",
                ["KingEvent"] = "<color=#A9BCF5>Mountain King</color>",
                ["KingDesc"] = "Be above all to win!",
                ["EventTitle"] = "[<color=#F7D358>CustomEvents</color>]",
                ["EventStart"] = "[<color=#F7D358>CustomEvents</color>] Start event: {0}!",
                ["EventEnd"] = "[<color=#F7D358>CustomEvents</color>] Event: {0} ended.",
                ["WinnerMessage"] = "[<color=#F7D358>CustomEvents</color>] Congratulations! Your prize {0} on mini store account!",
            }, this, "en");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["GatherEvent"] = "<color=#F79F81>Добыча ресурсов</color>",
                ["GatherDesc"] = "Добывай как можно больше ресурсов!",
                ["ComponentEvent"] = "<color=#ACFA58>Добыча компонентов</color>",
                ["ComponentDesc"] = "Лутай как можно больше скрапа!",
                ["HuntEvent"] = "<color=#00BFFF>Охота на животных</color>",
                ["HuntDesc"] = "Убивай животных! Человек не животное :D",
                ["KingEvent"] = "<color=#A9BCF5>Царь горы</color>",
                ["KingDesc"] = "Будь выше всех чтобы победить!",
                ["EventTitle"] = "[<color=#F7D358>CustomEvents</color>]",
                ["EventStart"] = "[<color=#F7D358>CustomEvents</color>] Запуск ивента: {0}!",
                ["EventEnd"] = "[<color=#F7D358>CustomEvents</color>] Ивент: {0} окончен.",
                ["WinnerMessage"] = "[<color=#F7D358>CustomEvents</color>] Поздравляем с победой! Вам начислено {0} на счёт мини-магазина!",
            }, this, "ru");
        }

        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (controller == null) return null;
            EventsHandler component = controller.GetComponent<EventsHandler>();
            if (component == null || component.CurEvent != lang.GetMessage("GatherEvent", this)) return null;

            var player = entity as BasePlayer;
            var ent = dispenser.name;
            if (component.playerScores.ContainsKey(player)) component.playerScores[player] += item.amount;
            return null;
        }

        void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            if (player == null || entity == null || controller == null) return;
            EventsHandler component = controller.GetComponent<EventsHandler>();
            if (component == null || component.CurEvent != lang.GetMessage("ComponentEvent", this)) return;

            if (entity.ShortPrefabName.Contains("crate"))
            {
                var ent = entity as LootContainer;
                if (ent != null && ent.inventory.itemList.Count < 1 && component.playerScores.ContainsKey(player))
                    component.playerScores[player]++;
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null || controller == null) return;
            EventsHandler component = controller.GetComponent<EventsHandler>();
            var player = info?.InitiatorPlayer;
            if (player == null || string.IsNullOrEmpty(player.UserIDString) || player.UserIDString.Length < 17) return;

            if (component != null && component.CurEvent == lang.GetMessage("ComponentEvent", this))
            {
                if (entity.ShortPrefabName.Contains("barrel"))
                {
                    if (component.playerScores.ContainsKey(player)) component.playerScores[player]++;
                }
            }
            if (component != null && component.CurEvent == lang.GetMessage("HuntEvent", this))
            {
                //Puts(entity.ShortPrefabName);
                switch (entity.ShortPrefabName)
                {
                    case "bear":
                        {
                            if (component.playerScores.ContainsKey(player)) component.playerScores[player] += 15;
                            break;
                        }
                    case "boar":
                        {
                            if (component.playerScores.ContainsKey(player)) component.playerScores[player] += 10;
                            break;
                        }
                    case "horse":
                        {
                            if (component.playerScores.ContainsKey(player)) component.playerScores[player] += 10;
                            break;
                        }
                    case "chicken":
                        {
                            if (component.playerScores.ContainsKey(player)) component.playerScores[player] += 2;
                            break;
                        }
                    case "wolf":
                        {
                            if (component.playerScores.ContainsKey(player)) component.playerScores[player] += 10;
                            break;
                        }
                    case "stag":
                        {
                            if (component.playerScores.ContainsKey(player)) component.playerScores[player] += 10;
                            break;
                        }
                }
            }
        }
        #endregion

        #region Functions
        private void StartEvent()
        {
            if (!EventsTimer.Destroyed) EventsTimer.Destroy();
            if(BasePlayer.activePlayerList.Count < config.minPlayers)
            {
                EventsTimer = timer.Once(config.timerStart, StartEvent);
                PrintWarning($"Not enough players to start! (Min {config.minPlayers})");
                return;
            }

            EventsHandler component;
            if (controller != null)
            {
                component = controller.GetComponent<EventsHandler>();
                if (component != null) UnityEngine.Object.Destroy(component);
            }

            controller.AddComponent<EventsHandler>();
            component = controller.GetComponent<EventsHandler>();
            //Убейте меня
            System.Random rand = new System.Random();
            List<string> values = Enumerable.ToList(EventsList.Keys);
            component.CurEvent = values[rand.Next(EventsList.Keys.Count)];
            component.CurEventDescription = EventsList[component.CurEvent];
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!component.playerScores.ContainsKey(player)) component.playerScores.Add(player, 0);
                //if (player.userID != 76561198869937617) continue;
                SendReply(player, $"{string.Format(lang.GetMessage("EventStart", this), component.CurEvent)}");
            }
        }

        private void EndEvent()
        {
            if (!EventsTimer.Destroyed) EventsTimer.Destroy();

            EventsHandler component = controller.GetComponent<EventsHandler>();

            if (component == null) return;
            
            foreach (var player in BasePlayer.activePlayerList)
            {
                //if (player.userID != 76561198869937617) continue;
                CuiHelper.DestroyUi(player, "UI_EventsLayer");
                SendReply(player, $"{string.Format(lang.GetMessage("EventEnd", this), component.CurEvent)}");
                
            }

            var mySortedList = component.playerScores.OrderByDescending(p => p.Value).Take(config.maxWinners);
            double money = config.evPrice;
            foreach (var a in mySortedList)
            {
                if (a.Key == null) break;
                if (plugins.Exists(config.evPriceType))
                {
                    Interface.Call(config.evPriceCallFunc, a.Key.userID, money);
                    SendReply(a.Key, $"{string.Format(lang.GetMessage("WinnerMessage", this), money)}");
                }
                money -= config.evPrice / config.maxWinners;
            }

            if (controller != null)
            {
                //component = controller.GetComponent<EventsHandler>();
                if (component != null) UnityEngine.Object.Destroy(component);
            }

            EventsTimer = timer.Once(config.timerStart, StartEvent);
        }
        #endregion

        #region Cmds
        [ChatCommand("ev")]
        void CMD_Events(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1) return;// || !player.IsAdmin) return;

            if(args[0] == "start")
                StartEvent();
            if (args[0] == "end")
                EndEvent();
        }
        #endregion

        #region UTILS

        #endregion
    }
}

