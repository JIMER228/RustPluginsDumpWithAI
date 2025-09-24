// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using ox = Oxide.Core;
using Oxide.Core.Plugins;
using System.Linq;
using UnityEngine;
using System.Data;
using Oxide.Core;
using Rust;
using ru = Oxide.Game.Rust;
using Oxide.Plugins;
using Oxide.Core.Configuration;
using System.Globalization;
using Oxide.Game.Rust.Cui;
using ProtoBuf;

namespace Oxide.Plugins
{
    [Info("AntiCheat", "RustPlugin.ru", "1.2.7")]
    class AntiCheat : RustPlugin
    {
        public static ox.Libraries.Permission perm => ox.Interface.Oxide.GetLibrary<ox.Libraries.Permission>("Permission");
        private static HashSet<PlayerData> LoadedPlayerData = new HashSet<PlayerData>();
        [PluginReference]
        Plugin Duel;
		[PluginReference]
        Plugin CarCommander;
		static List<BasePlayer> adminList = new List<BasePlayer>();
        #region Data
        class PlayerData
        {
            public string SteamID;
        }
        #endregion

        //////// Конфиг-начало
        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config["Количество детектов для автоматического бана за макрос:"] = 10;
            Config["Включить проверку на Speedhack?"] = true;
            Config["Включить проверку Flyhack?"] = true;
            Config["Процент попадания в голову для автоматического бана за AIM"] = 35f;
            Config["Количество попаданий для автоматического бана за AIM, если процент попадания больше зазначеного в конфиге:"] = 40;
            Config["[Топ-Игроков] Цвет Панели"] = "0.0 0.0 0.0 0.8";
            Config["[Топ-Игроков] Включить?"] = true;
            Config["[Топ-Игроков] Время Между Сообщениями"] = 300f;
            Config["[Топ-Игроков] Цвет Оповещаний"] = "#FF6347";
            SaveConfig();
        }
        ///Топ-Игроков
        [HookMethod("OnServerSave")]
        private void OnServerSave()
        {
            Saved();
        }



        [HookMethod("Saved")]
        private void Saved()
        {
            Interface.Oxide.DataFileSystem.WriteObject("TopData", Tops);
        }
		private bool IsNPC(BasePlayer player)
        {
            if (player == null) return false;
            //BotSpawn
            if (player is NPCPlayer)
                return true;
            //HumanNPC
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L))
                return true;
            return false;
        }
        [ChatCommand("rank")]
        void TurboRankCommand(BasePlayer player, string command)
        {
            var TopPlayer = (from x in Tops where x.UID == player.UserIDString select x).OrderByDescending(x => x.РакетВыпущено + x.ВзрывчатокИспользовано);
            player.ChatMessage($"<size=15>Статистика игрока <color=#ffa500>{player.displayName}</color></size>");
            foreach (var top in TopPlayer)
           {
                rust.SendChatMessage(player, $"<size=14><color=#ffffff>Убийств игроков: <color=#ffa500>{top.УбийствPVP}</color> | Смертей: <color=#ffa500>{top.Смертей}</color></color></size>", null, "0");
                rust.SendChatMessage(player, $"<size=14><color=#ffffff>Ракет выпущено: <color=#ffa500>{top.РакетВыпущено}</color> | Взрывчаток использовано: <color=#ffa500>{top.ВзрывчатокИспользовано}</color></color></size>", null, "0");
                rust.SendChatMessage(player, $"<size=14><color=#ffffff>Ресурсов собрано: <color=#ffa500>{top.РесурсовСобрано}</color> | Животных убито: <color=#ffa500>{top.УбийствЖивотных}</color></color></size>", null, "0");
                rust.SendChatMessage(player, $"<size=14><color=#ffffff>Пуль выпущено: <color=#ffa500>{top.ПульВыпущено}</color> | Стрел выпущено: <color=#ffa500>{top.СтрелВыпущено}</color></color></size>", null, "0");
                rust.SendChatMessage(player, $"<size=14><color=#ffffff>Предметов скрафчено: <color=#ffa500>{top.ПредметовСкрафчено}</color> | Вертолетов сбито: <color=#ffa500>{top.ВертолётовУничтожено}</color></color></size>", null, "0");
                rust.SendChatMessage(player, $"<size=14><color=#ffffff>NPC убито: <color=#ffa500>{top.NPCУбито}</color> | Танков уничтожено: <color=#ffa500>{top.ТанковУничтожено}</color></color></size>", null, "0");
		   }
            return;
        }
        [ChatCommand("top")]
        void TurboTopCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 1)
            {
                int n = 0;
                // Очистка статистики игроков
                if (args[0] == "reset")
                {
                    if (!player.IsAdmin)
                    {
                        rust.SendChatMessage(player, $"<size=14><color=#FFA500>Ты кто такой? Давай досвиданье!</color></size>", null, "0");
                        return;
                    }
                    var TopPlayer = (from x in Tops select x);
                    foreach (var top in TopPlayer)
                    {
                        top.РакетВыпущено = 0;
                        top.УбийствPVP = 0;
                        top.ВзрывчатокИспользовано = 0;
                        top.УбийствЖивотных = 0;
                        top.ПульВыпущено = 0;
                        top.СтрелВыпущено = 0;
                        top.Смертей = 0;
                        top.РесурсовСобрано = 0;
                        top.ВертолётовУничтожено = 0;
						top.NPCУбито = 0;
                        Saved();
                    }
                    rust.SendChatMessage(player, $"<size=14><color=#FFA500>Статистика игроков обнулена!</color></size>", null, "0");
                    return;
                }
                if (args[0] == "farm")
                {
                    bool prov = false;
                    player.ChatMessage("<size=14><color=#FF6347>[СТАТИСТИКА]</color> ТОП Фармеров</size>");
                    var TopPlayer = (from x in Tops select x).OrderByDescending(x => x.РесурсовСобрано);
                    foreach (var top in TopPlayer)
                    {
                        n++;
                        if (n <= 5)
                        {
                            rust.SendChatMessage(player, $"<size=14><color=#FFA500>{n}.</color> <color=#FF8C00>{top.Ник}</color> ({top.РесурсовСобрано})</size>", null, top.UID);
                            if (top.UID == player.UserIDString)
                            {
                                prov = true;
                            }
                        }
                    }
                    if (!prov)
                    {
                        player.ChatMessage("...");
                        int i = 0;
                        foreach (var top in TopPlayer)
                        {
                            i++;
                            if (top.UID == player.UserIDString)
                            {
                                rust.SendChatMessage(player, $"<size=14><color=#FFA500>{i}.</color> <color=#FF8C00>{top.Ник}</color> ({top.РесурсовСобрано})</size>", null, player.UserIDString);
                            }
                        }
                    }
                    return;
                }
                if (args[0] == "pvp")
                {
                    bool prov = false;
                    player.ChatMessage("<size=14><color=#FF6347>[СТАТИСТИКА]</color> ТОП Убийств PVP</size>");
                    var TopPlayer = (from x in Tops select x).OrderByDescending(x => x.УбийствPVP);
                    foreach (var top in TopPlayer)
                    {
                        n++;
                        if (n <= 5)
                        {
                            rust.SendChatMessage(player, $"<size=14><color=#FFA500>{n}.</color> <color=#FF8C00>{top.Ник}</color> ({top.УбийствPVP})</size>", null, top.UID);
                            if (top.UID == player.UserIDString)
                            {
                                prov = true;
                            }
                        }
                    }
                    if (!prov)
                    {
                        player.ChatMessage("<size=14>...</size>");
                        int i = 0;
                        foreach (var top in TopPlayer)
                        {
                            i++;
                            if (top.UID == player.UserIDString)
                            {
                                rust.SendChatMessage(player, $"<size=14><color=#FFA500>{i}.</color> <color=#FF8C00>{top.Ник}</color> ({top.УбийствPVP})</size>", null, player.UserIDString);
                            }
                        }
                    }
                    return;
                }
                if (args[0] == "raid")
                {
                    bool prov = false;
                    player.ChatMessage("<size=14><color=#FF6347>[СТАТИСТИКА]</color> ТОП Рейдеров</size>");
                    var TopPlayer = (from x in Tops select x).OrderByDescending(x => x.РакетВыпущено + x.ВзрывчатокИспользовано);
                    foreach (var top in TopPlayer)
                    {
                        n++;
                        if (n <= 5)
                        {
                            rust.SendChatMessage(player, $"<size=14><color=#FFA500>{n}.</color> <color=#FF8C00>{top.Ник}</color> ({top.РакетВыпущено + top.ВзрывчатокИспользовано})</size>", null, top.UID);
                            if (top.UID == player.UserIDString)
                            {
                                prov = true;
                            }
                        }
                    }
                    if (!prov)
                    {
                        player.ChatMessage("<size=14>...</size>");
                        int i = 0;
                        foreach (var top in TopPlayer)
                        {
                            i++;
                            if (top.UID == player.UserIDString)
                            {
                                rust.SendChatMessage(player, $"<size=14><color=#FFA500>{i}.</color> <color=#FF8C00>{top.Ник}</color> ({top.РакетВыпущено + top.ВзрывчатокИспользовано})</size>", null, player.UserIDString);
                            }
                        }
                    }
                    return;
                }
            }
            else
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("Панелька", null, null, null, null));
                CuiElementContainer elements = CreatePanel("0");
                CuiHelper.AddUi(player, elements);
            }
        }

        // GUI панелька
        [ConsoleCommand("top.show")]
        private void TopShowOpenCmd2(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if ((bool)Config["[Топ-Игроков] Включить?"] == false) return;
            if (player == null)
                return;
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("Панелька", null, null, null, null));
            string number = arg.Args[0];
            CuiElementContainer elements = CreatePanel(number);
            CuiHelper.AddUi(player, elements);
            return;
        }
        CuiElementContainer CreatePanel(string number)
        {
            string cvet = Convert.ToString(Config["[Топ-Игроков] Цвет Панели"]);
            var elements = new CuiElementContainer();
            var panel = elements.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0.75" },
                RectTransform = { AnchorMin = "0.2 0.13", AnchorMax = "0.8 0.95" },
                CursorEnabled = true
            }, "Hud", "Панелька");
            elements.Add(new CuiPanel
            {
                Image = { Color = $"{cvet}" },
                RectTransform = { AnchorMin = "0 0.81", AnchorMax = "1 1" },
            }, panel);

            elements.Add(new CuiLabel
            {
                Text = { Text = "<color=#ffa500>TOP 10 ИГРОКОВ</color>", FontSize = 30, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0.89", AnchorMax = "1 1" },
            }, panel);
            elements.Add(new CuiButton
            {
                Button = { Command = "top.exit", Color = $"{cvet}" },
                RectTransform = { AnchorMin = "0.9 0.90", AnchorMax = "1 1" },
                Text = { Text = "<color=#ffa500>X</color>", FontSize = 18, Align = TextAnchor.MiddleCenter }
            }, panel);


            elements.Add(new CuiPanel
            {
                Image = { Color = $"{cvet}" },
                RectTransform = { AnchorMin = "0 0.81", AnchorMax = "0.298 0.8899999" },
            }, panel);
            elements.Add(new CuiLabel
            {
                Text = { Text = "<color=#ffa500>Игрок</color>", FontSize = 20, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0.81", AnchorMax = "0.29 0.8899999" },
            }, panel);
            elements.Add(new CuiButton
            {
                Text = { Text = "<color=#ffa500>Убийства</color>", FontSize = 15, Align = TextAnchor.MiddleCenter },
                Button = { Command = "top.show 1", Color = $"{cvet}" },
                RectTransform = { AnchorMin = "0.30 0.81", AnchorMax = "0.454 0.8899999" }
            }, panel);
            elements.Add(new CuiButton
            {
                Text = { Text = "<color=#ffa500>Смертей</color>", FontSize = 15, Align = TextAnchor.MiddleCenter },
                Button = { Command = "top.show 2", Color = $"{cvet}" },
                RectTransform = { AnchorMin = "0.455 0.81", AnchorMax = "0.578 0.8899999" }
            }, panel);
            elements.Add(new CuiButton
            {
                Text = { Text = "<color=#ffa500>Животные</color>", FontSize = 15, Align = TextAnchor.MiddleCenter },
                Button = { Command = "top.show 3", Color = $"{cvet}" },
                RectTransform = { AnchorMin = "0.58 0.81", AnchorMax = "0.678 0.8899999" }
            }, panel);
            elements.Add(new CuiButton
            {
                Text = { Text = "<color=#ffa500>Взрывов</color>", FontSize = 15, Align = TextAnchor.MiddleCenter },
                Button = { Command = "top.show 4", Color = $"{cvet}" },
                RectTransform = { AnchorMin = "0.68 0.81", AnchorMax = "0.859 0.8899999" }
            }, panel);
            elements.Add(new CuiButton
            {
                Text = { Text = "<color=#ffa500>Ресурсы</color>", FontSize = 15, Align = TextAnchor.MiddleCenter },
                Button = { Command = "top.show 5", Color = $"{cvet}" },
                RectTransform = { AnchorMin = "0.86 0.81", AnchorMax = "1 0.8899999" }
            }, panel);

            string polosa = "0 0 0 0.9";
            int n = 0;
            var TopPlayer = (from x in Tops select x).OrderByDescending(x => x.УбийствPVP).Take(10);
            if (number == "2")
            {
                TopPlayer = (from x in Tops select x).OrderByDescending(x => x.Смертей).Take(10);
            }
            else if (number == "3")
            {
                TopPlayer = (from x in Tops select x).OrderByDescending(x => x.УбийствЖивотных).Take(10);
            }
            else if (number == "4")
            {
                TopPlayer = (from x in Tops select x).OrderByDescending(x => x.РакетВыпущено + x.ВзрывчатокИспользовано).Take(10);
            }
            else if (number == "5")
            {
                TopPlayer = (from x in Tops select x).OrderByDescending(x => x.РесурсовСобрано).Take(10);
            }
            else
            {
                TopPlayer = (from x in Tops select x).OrderByDescending(x => x.УбийствPVP).Take(10);
            }
            foreach (var top in TopPlayer)
            {
                if (n % 2 == 0)
                {
                    polosa = "0 0 0 0.7";
                }
                else
                {
                    polosa = "1 1 1 0.05";
                }
                elements.Add(new CuiPanel
                {
                    Image = { Color = polosa },
                    RectTransform = { AnchorMin = $"0 {0.72 - (n * 0.08)}", AnchorMax = $"1 {0.8 - (n * 0.08)}" },
                }, panel);
                elements.Add(new CuiLabel
                {
                    Text = { Text = Convert.ToString(top.Ник), FontSize = 15, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0 {0.72 - (n * 0.08)}", AnchorMax = $"0.29 {0.8 - (n * 0.08)}" },
                }, panel);
                elements.Add(new CuiLabel
                {
                    Text = { Text = Convert.ToString(top.УбийствPVP), FontSize = 15, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.3 {0.72 - (n * 0.08)}", AnchorMax = $"0.454 {0.8 - (n * 0.08)}" },
                }, panel);
                elements.Add(new CuiLabel
                {
                    Text = { Text = Convert.ToString(top.Смертей), FontSize = 15, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.455 {0.72 - (n * 0.08)}", AnchorMax = $"0.578 {0.8 - (n * 0.08)}" },
                }, panel);
                elements.Add(new CuiLabel
                {
                    Text = { Text = Convert.ToString(top.УбийствЖивотных), FontSize = 15, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.58 {0.72 - (n * 0.08)}", AnchorMax = $"0.678 {0.8 - (n * 0.08)}" },
                }, panel);
                elements.Add(new CuiLabel
                {
                    Text = { Text = $"{ Convert.ToString(top.ВзрывчатокИспользовано + top.РакетВыпущено)}", FontSize = 15, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.68 {0.72 - (n * 0.08)}", AnchorMax = $"0.85 {0.8 - (n * 0.08)}" },
                }, panel);
                elements.Add(new CuiLabel
                {
                    Text = { Text = Convert.ToString(top.РесурсовСобрано), FontSize = 15, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.86 {0.72 - (n * 0.08)}", AnchorMax = $"0.99 {0.8 - (n * 0.08)}" },
                }, panel);
                n++;
            }
            return elements;
        }


        // Выход с панельки
        [ConsoleCommand("top.exit")]
        private void MagazineOpenCmd2(ConsoleSystem.Arg arg)
        {
            if ((bool)Config["[Топ-Игроков] Включить?"] == false) return;
            if (arg.Player() == null)
                return;
            BasePlayer player = arg.Player();
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("Панелька", null, null, null, null));
        }

		 private Dictionary<uint, string> LastHeliHit = new Dictionary<uint, string>();
		 
        void OnEntityDeath(BaseCombatEntity victim, HitInfo info, BaseCombatEntity entity)
        {
			
			if (entity == null || info == null) return;
			if (victim == null) return;
            if (entity?.net?.ID == null) return;
            BasePlayer victimBP = victim.ToPlayer();
            BasePlayer initiator = info.InitiatorPlayer;
			 if (entity is BaseHelicopter && info.Initiator is BasePlayer)
                LastHeliHit[entity.net.ID] = info.InitiatorPlayer.UserIDString;
            if (victimBP != null && !IsNPC(victimBP))
            {
                string death = victimBP.UserIDString;
                TopData con = (from x in Tops where x.UID == death select x).FirstOrDefault();
                //Считаем смерти
                con.Смертей += 1;
                Saved();
            }
            if (initiator == null)
            {
                //Считаем количество сбитых "Гвинтокрилів"
                if (victim is BaseHelicopter)
                {
                    if (LastHeliHit.ContainsKey(victim.net.ID))
                    {
                        TopData data = Tops.Where(p => p.UID == LastHeliHit[victim.net.ID]).FirstOrDefault();
                        data.ВертолётовУничтожено += 1;
                        LastHeliHit.Remove(victim.net.ID);
                    }
                }
                return;
            }
            if (initiator != null && !IsNPC(initiator))
            {
                string killer = initiator.UserIDString;
                TopData con2 = (from x in Tops where x.UID == killer select x).FirstOrDefault();
                //NPC
                if (IsNPC(victimBP))
                {
                    con2.NPCУбито++;
                }
                //Считаем убийства животных
                if (victim is BaseAnimalNPC)
                {
                    con2.УбийствЖивотных += 1;
                }
                if (victim is BradleyAPC)
                {
                    con2.ТанковУничтожено++;
                }
                //Считаем убийства игроков
                if (victimBP != null && victimBP != initiator)
                {
                    con2.УбийствPVP += 1;
                }
                Saved();
            }
            return;
        }
        //Считаем взрывчатку
        void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            TopData con = (from x in Tops where x.UID == Convert.ToString(player.userID) select x).FirstOrDefault();
            con.ВзрывчатокИспользовано += 1;
            Saved();
        }
        //Считаем ракеты
        void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            TopData con = (from x in Tops where x.UID == Convert.ToString(player.userID) select x).FirstOrDefault();
            con.РакетВыпущено += 1;
            Saved();
        }


        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            DoGather(player, item.amount);
        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (entity == null || !(entity is BasePlayer) || item == null || dispenser == null) return;
            if (entity.ToPlayer() is BasePlayer)
                DoGather(entity.ToPlayer(), item.amount);
        }


        //Подсчитываем количество собраных ресурсов
        private void DoGather(BasePlayer player, int item)
        {
            if (player == null) return;
            TopData con = (from x in Tops where x.UID == Convert.ToString(player.userID) select x).FirstOrDefault();
            con.РесурсовСобрано += item;
            Saved();
            return;
        }

        private void CreateInfo(BasePlayer player)
        {
            if ((bool)Config["[Топ-Игроков] Включить?"] == false) return;
            if (player == null) return;
            Tops.Add(new TopData((string)player.displayName, player.UserIDString, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
            Saved();
        }


        //////// Конфиг-конец
        [ConsoleCommand("ban.user")]
        private void cmdBan(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin)
            {
                return;
            }
            if (arg.Args == null || arg.Args.Length < 2)
            {
                arg.ReplyWith("Неверный синтаксис! Используйте ban.user <SteamID> <Причина>");
                return;
            }
            BasePlayer target = null;
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.UserIDString == arg.Args[0])
                {
                    target = player;
                    ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { 0, $"<color=#FF4500>[Анти-чит]</color> <color=#FF6347>{player.displayName}({player.UserIDString})</color> забанен! Причина: {arg.Args[1]}!" });
                }
            }
            LoadedPlayerData.Add(new PlayerData
            {
                SteamID = arg.Args[0],
            });
            SaveData();
            if (target != null && target.IsConnected) Kick(target, "VAC BAN");
            arg.ReplyWith($"{arg.Args[0]} забанен");

        }

        [HookMethod("UnbanCommand")]
        private void UnbanCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin)
            {
                return;
            }
            if (arg.Args == null || arg.Args.Length != 1)
            {
                arg.ReplyWith("Неверный синтаксис! Используйте unban.user <SteamID>");
                return;
            }

            LoadedPlayerData.RemoveWhere(p => p.SteamID == arg.Args[0]);
            arg.ReplyWith($"{arg.Args[0]} разбанен");
            SaveData();
        }

        private object CanUserLogin(string name, string id) => !LoadedPlayerData.Any(p => p.SteamID == id);

        //////// Бан колонка
        public List<BasePlayer> Players => BasePlayer.activePlayerList;
        public BasePlayer FindById(ulong id)
        {
            foreach (var player in Players)
            {
                if (!id.Equals(player.userID)) continue;
                return player;
            }
            return null;
        }

        public bool IsConnected(BasePlayer player) => BasePlayer.activePlayerList.Contains(player);
        public void Kick(BasePlayer player, string reason = "") => player.Kick(reason);
        public bool IsBanned(ulong id) => ServerUsers.Is(id, ServerUsers.UserGroup.Banned);
        public void Ban(ulong id, string reason = "")
        {
            if (IsBanned(id)) return;

            var player = FindById(id);
            ServerUsers.Set(id, ServerUsers.UserGroup.Banned, player?.displayName ?? "Unknown", reason);
            ServerUsers.Save();
            if (player != null && IsConnected(player)) Kick(player, reason);
        }

        private readonly Dictionary<ulong, AimLockData> aimlock = new Dictionary<ulong, AimLockData>();

        public class AimLockData
        {
            public int Ticks = 1;
            public string Body = "";
        }
		  
        [HookMethod("OnEntityTakeDamage")]
        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info, BaseCombatEntity victim, BasePlayer player)
        {
            // Собираем инфу для начала
            if (entity is BasePlayer && info.Initiator is BasePlayer)
            {
				if (victim == null) return;
                if (entity?.net?.ID == null) return;
                var distance = info.Initiator.Distance(entity.transform.position);
				if (entity is BaseHelicopter && info.Initiator is BasePlayer)
				
                LastHeliHit[entity.net.ID] = info.InitiatorPlayer.UserIDString;
                if (distance > 10)
                {
                    BasePlayer ent = entity.ToPlayer();
                    BasePlayer init = info.Initiator.ToPlayer();

                    AimLockData bodylock;
                    if (!aimlock.TryGetValue(init.userID, out bodylock))
                    {
                        aimlock.Add(init.userID, bodylock = new AimLockData());
                    }
                    var _bodyPart = entity?.skeletonProperties?.FindBone(info.HitBone)?.name?.english ?? "";

                    if (bodylock.Body == _bodyPart && _bodyPart != "lower spine")
                    {
                        bodylock.Ticks++;
                    }
                    else
                    {
                        bodylock.Ticks = 1;
                    }

                    if (bodylock.Ticks > 2)
                    {
                        ConVar.Server.Log("oxide/logs/AntiCheat.txt", $"(АимЛок) {init.displayName}({init.UserIDString})| обнаружений {bodylock.Ticks} |  {bodylock?.Body ?? ""} | {distance} м.");
                        bodylock.Ticks = 1;
                    }
                    bodylock.Body = _bodyPart;

                    AimData con = (from x in Aim where x.SteamId == init.UserIDString select x).FirstOrDefault();
                    con.Попаданий += 1;
                    if (info.isHeadshot)
                    {
                        con.Голова += 1;
                    }
                    double aim = Math.Floor((con.Голова * 1f / con.Попаданий * 1f) * 100);
                    TopData play = (from x in Tops where x.UID == init.UserIDString select x).FirstOrDefault();
                    if (play.Смертей == 0) play.Смертей = 1;
                    double kdr = Math.Round(play.УбийствPVP * 1f / play.Смертей * 1f, 2);
                    if (con.Попаданий > 20 && aim > 50)
                    {
                        SendDetection(player, string.Format("<color=green>[Античит детект]</color> " + "(<color=red>AimHack</color>) " + player.displayName + "забанен! Процент попаданий в голову слишком большой {aim}% (AimHack)!"));
                        Debug.LogWarning($"[Анти-чит] {init.displayName}({init.UserIDString}) забанен! Причина: AimHack!");
                        Ban(init.userID, "[Анти-чит] AimHack");
                        ConVar.Server.Log("oxide/logs/AntiCheat.txt", $"{init.displayName}({init.UserIDString}) забанен! Процент попаданий в голову слишком большой {aim}% (AimHack)!");
                    }

                    if (con.Попаданий > 80 && aim < 7 && kdr > 2)
                    {
                        SendDetection(player, string.Format("<color=green>[Античит детект]</color> " + "(<color=red>AimLock</color>) " + player.displayName + "забанен! Соотношение попаданий в голову {aim}% и КДР аномальные (AimLock)!"));
                        Debug.LogWarning($"[Анти-чит] {init.displayName}({init.UserIDString}) забанен! Причина: AimLock!");
                        Ban(init.userID, "[Анти-чит] AimLock");
                        ConVar.Server.Log("oxide/logs/AntiCheat.txt", $"{init.displayName}({init.UserIDString}) забанен! Соотношение попаданий в голову {aim}% и КДР аномальные (AimLock)!");
                    }
                }
            }
        }

        #region Console Commands
        [HookMethod("AimCheck")]
        private void AimCheck(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin)
            {
                return;
            }
            if (arg.Args.Length == 1)
            {
                var check = (from x in Aim where x.SteamId == arg.Args[0] select x).Count();
                if (check > 0)
                {
                    AimData con = (from x in Aim where x.SteamId == arg.Args[0] select x).FirstOrDefault();
                    double aim = Math.Floor((con.Голова * 1f / con.Попаданий * 1f) * 100);
                    arg.ReplyWith($"[Анти-чит] {con.Игрок}: Aim: {aim}% при {con.Попаданий} попаданиях (с растояния 10 метров и выше)");
                }
                else
                {
                    arg.ReplyWith("Игрока не найдено!");
                }
            }
            return;
        }

        [HookMethod("AimCheckServer")]
        private void AimCheckServer(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin)
            {
                return;
            }
            double popa = 0;
            double head = 0;
            var Top = (from x in Aim select x);
            foreach (var top in Top)
            {
                popa = popa + top.Попаданий;
                head = head + top.Голова;
            }

            arg.ReplyWith($"[Анти-чит]: В голову попадают в {Math.Floor((head * 1f / popa * 1f) * 100f)}% случаев (с растояния 10 метров и выше)");

            return;
        }

        [HookMethod("CheckServer")]
        private void CheckServer(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin)
            {
                return;
            }
            int i = 0;
            double procent = 0;
            string players = "";
            double popa = 0;
            double head = 0;
            string aimdesc = "";
            var Top = (from x in Aim select x);
            foreach (var top in Top)
            {
                popa = popa + top.Попаданий;
                head = head + top.Голова;
            }
            double aimserver = Math.Floor((head * 1f / popa * 1f) * 100f);
            players = "----------------------------------Игроки---------------------------------- \n";
            foreach (var player in BasePlayer.activePlayerList)
            {
                TopData play = (from x in Tops where x.UID == player.UserIDString select x).FirstOrDefault();
                if (play.Смертей == 0) play.Смертей = 1;
                AimData aim = (from x in Aim where x.SteamId == play.UID select x).FirstOrDefault();
                if (aim.Попаданий == 0) aim.Попаданий = 1;
                double aimprocent = Math.Floor((aim.Голова * 1f / aim.Попаданий * 1f) * 100f);
                double kdr = Math.Round(play.УбийствPVP * 1f / play.Смертей * 1f, 2);
                double razn = aimserver - aimprocent;
                if (aim.Попаданий < 30 || play.УбийствPVP < 10)
                {
                    aimdesc = "Новый игрок";
                }
                else if (razn > -5 && razn > 5 && kdr < 2)
                {
                    aimdesc = "Простой игрок";
                }
                else if (razn > -5 && razn > 5 && kdr < 3)
                {
                    aimdesc = "Подозрительный игрок";
                }
                else if (razn > -5 && razn > 5 && kdr >= 3)
                {
                    aimdesc = "Очень подозрительный игрок";
                }
                else if (razn > -5 && razn < -8 && kdr < 2)
                {
                    aimdesc = "Игрок с хорошей точностью в голову";
                }
                else if (razn > -5 && razn < -8 && kdr < 3)
                {
                    aimdesc = "Скилловый игрок";
                }
                else if (razn > -5 && razn < -8 && kdr < 4)
                {
                    aimdesc = "Подозрительный игрок";
                }
                if (razn > -5 && razn < -8 && kdr >= 4)
                {
                    aimdesc = "Читер";
                }
                else if (razn > 5 && razn < 8 && kdr < 1)
                {
                    aimdesc = "Игрок со слабым скиллом";
                }
                else if (razn > 5 && razn < 8 && kdr < 2)
                {
                    aimdesc = "Подозрительный игрок";
                }
                else if (razn > 5 && razn < 8 && kdr < 3)
                {
                    aimdesc = "Очень подозрительный игрок";
                }
                if (razn > 5 && razn < 8 && kdr >= 4)
                {
                    aimdesc = "Читер";
                }


                i++;
                players = players + $"{i}. {play.Ник} ({play.UID}) | aim: {aimprocent}% | kdr {kdr} | {aimdesc} \n";


            }
            arg.ReplyWith(players + "-------------------------------------------------------------------------------");
        }

        #endregion
        [HookMethod("OnPlayerInit")]
        private void OnPlayerInit(BasePlayer player)
        {
            if (player == null) return;
            var check = (from x in Tops where x.UID == player.UserIDString select x).Count();
            if (check == 0) CreateInfo(player);
            TopData con = (from x in Tops where x.UID == Convert.ToString(player.userID) select x).FirstOrDefault();
            if (player.displayName != con.Ник) con.Ник = player.displayName;
            Saved();
            new PluginTimers(this).Once(2f, () => CheckSpeed(player));
            new PluginTimers(this).Once(2f, () => AimPlayer(player));
			if (isAdmin(player)) { if (!adminList.Contains(player)) adminList.Add(player); }
        }
		bool isAdmin( BasePlayer player )
        {
        	if (player.net.connection.authLevel > 0) return true;
        	return permission.UserHasPermission(player.userID.ToString(), "anticheat.admin");
        }
        private void AimPlayer(BasePlayer player)
        {
            if (player == null) return;

            var check = (from x in Aim where x.SteamId == player.UserIDString select x).Count();
            if (check == 0)
            {
                Aim.Add(new AimData(player.displayName, player.UserIDString, 0, 0));
                SaveData();
            }
        }
		
        [HookMethod("CheckSpeed")]
        private void CheckSpeed(BasePlayer player)
        {
            if (player == null) return;
            if (player.net?.connection?.authLevel >= 2) return;
           if (perm.UserHasPermission(player.UserIDString, "anticheat.admin")) return;
            BaseMountable mount = player.GetMounted();
            if (mount == null) return;
            var position = player.transform.position;
            int n = 0;
            int f = 0;
            if (Duel && (bool)Duel?.CallHook("IsDuelPlayer", player)) return;
            new PluginTimers(this).Repeat(2f, 0, () =>
            {
                if (player == null) return;
                if (!player.IsConnected) return;
                //ФлайХак 
                if (player.IsFlying && (bool)Config["Включить проверку Flyhack?"] && !player.IsSwimming() && !player.IsDead() && !player.IsSleeping() && !player.IsWounded())

                {
                    f++;
                    if (f >= 2)
                    {
						Kick(player, "[Анти-чит] FlyHack");
                        SendDetection(player, string.Format("<color=green>[Античит детект]</color> " + "(<color=red>FLY</color>) " + player.displayName + " кикнут! Слишком долго находился в воздухе!"));
						Debug.LogWarning($"[Анти-чит] {player.displayName}({player.UserIDString}) кикнут! Причина: FlyHack!");
					   LogToFile("log", $"(ФлайХак) {player.displayName}({player.UserIDString}) кикнут! Слишком долго находился в воздухе!", this, false);
                    }
                }
                else
                {
                    f = 0;
                }
                var distance = player.Distance(position);
                position = player.transform.position;
                // Спидхак
                if (distance > 11)
                {
                    if (n >= 2)
                    {
                        if (player.IsOnGround() && (bool)Config["Включить проверку на Speedhack?"])
                        {
                            Kick(player, "[Анти-чит] SpeedHack");
							SendDetection(player, string.Format("<color=green>[Античит детект]</color> " + "(<color=red>SPEEDHACK</color>) " + player.displayName + "кикнут! Двигался со скоростью выше нормы!"));
                            Debug.LogWarning($"[Анти-чит] {player.displayName}({player.UserIDString}) кикнут! Причина: SpeedHack!");
                             LogToFile("log", $"(СпидХак) {player.displayName}({player.UserIDString}) кикнут! Двигался со скоростью выше нормы!", this, false);
                        }
                        
                        SaveData();
                    }
                    n++;
                }
                else
                {
                    n = 0;
                }
            });
        }
        ////////////////////////////////////////////////////////////
        // Static Fields
        ////////////////////////////////////////////////////////////
        static int bulletmask;
        static DamageTypeList emptyDamage = new DamageTypeList();
        static Vector3 VectorDown = new Vector3(0f, -1f, 0f);
        static Hash<BasePlayer, float> lastWallhack = new Hash<BasePlayer, float>();
        static RaycastHit cachedRaycasthit;

        [HookMethod("WallhackKillCheck")]
        private void WallhackKillCheck(BasePlayer player, BasePlayer attacker, HitInfo hitInfo)
        {
            if (Physics.Linecast(attacker.eyes.position, hitInfo.HitPositionWorld, out cachedRaycasthit, bulletmask))
            {
                BuildingBlock block = cachedRaycasthit.collider.GetComponentInParent<BuildingBlock>();
                if (block != null)
                {
                    if (block.blockDefinition.hierachyName == "wall.window") return;

                    CancelDamage(hitInfo);
                    if (Time.realtimeSinceStartup - lastWallhack[attacker] > 0.5f)
                    {
                        lastWallhack[attacker] = Time.realtimeSinceStartup;
                        Debug.LogWarning($"WallhackAttack обнаружен у {player.displayName}");
						SendDetection(player, string.Format("<color=green>[Античит детект]</color> " + "(<color=red>WallhackAttack</color>) " + player.displayName + "нанес урон через препятствие."));
                        LogToFile("log", $"((WallhackAttack) {player.displayName}({player.UserIDString}) нанес урон через препятствие.", this, false);

                    }
                }
            }
        }

        private void CancelDamage(HitInfo hitinfo)
        {
            hitinfo.damageTypes = emptyDamage;
            hitinfo.HitEntity = null;
        }

        private readonly Dictionary<ulong, NoRecoilData> data = new Dictionary<ulong, NoRecoilData>();
        private readonly Dictionary<ulong, Timer> detections = new Dictionary<ulong, Timer>();
        private readonly int detectionDiscardSeconds = 300;
        private readonly int violationProbability = 30;
        private readonly int maximumViolations = 30;
        private readonly Dictionary<string, int> probabilityModifiers = new Dictionary<string, int>() {
            {"weapon.mod.muzzleboost", -5},
            {"weapon.mod.silencer", 5},
            {"weapon.mod.holosight", 5},
            {"crouching", 8},
            {"aiming", 5}

        };

        private readonly List<string> blacklistedAttachments = new List<string>()
        {
            "weapon.mod.muzzlebreak",
            "weapon.mod.lasersight",
            "weapon.mod.small.scope"
        };

        public class NoRecoilData
        {
            public int Ticks = 0;
            public int Count;
            public int Violations;
        }
		static void SendDetection(BasePlayer player, string msg)
        {
			
			if (perm.UserHasPermission(player.UserIDString, "anticheat.send"))
			 {
					 player.SendConsoleCommand("chat.add", new object[] { 0, msg});
			 }
        Interface.GetMod().LogWarning(msg);
        }
		
		void OnItemCraftFinished(ItemCraftTask task, Item item)
				{
					if (task.owner is BasePlayer)
					{
						TopData con = (from x in Tops where x.UID == Convert.ToString(task.owner.userID) select x).FirstOrDefault();
						con.ПредметовСкрафчено += 1;
						Saved();
					}
				}
        [HookMethod("OnWeaponFired")]
		
        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProjectileShoot projectileShoot)
        {
            if ((bool)Config["[Топ-Игроков] Включить?"])
            {
                TopData con = (from x in Tops where x.UID == Convert.ToString(player.userID) select x).FirstOrDefault();
                if (projectile.primaryMagazine.ammoType.itemid == -420273765 || projectile.primaryMagazine.ammoType.itemid == -1280058093)
                {
                    con.СтрелВыпущено += 1;
                }
                else
                {
                    con.ПульВыпущено += 1;
                }
            }
            var item = player.GetActiveItem();
            if (!(item.info.shortname == "rifle.ak" || item.info.shortname == "lmg.m249"))
                return;

            if (item.contents.itemList.Any(x => blacklistedAttachments.Contains(x.info.shortname)))
                return;

            NoRecoilData info;
            if (!data.TryGetValue(player.userID, out info))
                data.Add(player.userID, info = new NoRecoilData());

            UnityEngine.Vector3 eyesDirection = player.eyes.HeadForward();

            if (eyesDirection.y < -0.80)
                return;

            info.Ticks++;

            int probModifier = 0;
            foreach (Item attachment in item.contents.itemList)
                if (probabilityModifiers.ContainsKey(attachment.info.shortname))
                    probModifier += probabilityModifiers[attachment.info.shortname];

            if (player.modelState.aiming && probabilityModifiers.ContainsKey("aiming"))
                probModifier += probabilityModifiers["aiming"];

            if (player.IsDucked() && probabilityModifiers.ContainsKey("crouching"))
                probModifier += probabilityModifiers["crouching"];

            Timer detectionTimer;
            if (detections.TryGetValue(player.userID, out detectionTimer))
                detectionTimer.Reset(detectionDiscardSeconds);
            else
                detections.Add(player.userID, timer.Once(detectionDiscardSeconds, delegate ()
                {
                    if (info.Violations > 0)
                        info.Violations--;
                }));

            timer.Once(.5f, () =>
            {
                ProcessRecoil(projectile, player, mod, projectileShoot, info, probModifier, eyesDirection);
            });

        }

        [HookMethod("ProcessRecoil")]
        private void ProcessRecoil(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProjectileShoot projectileShoot, NoRecoilData info, int probModifier, UnityEngine.Vector3 eyesDirection)
        {
            var nextEyesDirection = player.eyes.HeadForward();
            if (Math.Abs(nextEyesDirection.y - eyesDirection.y) < .009 &&
                nextEyesDirection.y < .8) info.Count++;
            if (info.Ticks <= 10) return;
            var prob = 101 * info.Count / info.Ticks;
            var item = player.GetActiveItem();
            if (prob > ((101 - violationProbability) + probModifier))
            {
                if (prob > 10) prob = 101;
                info.Violations++;
				
                Debug.LogWarning("(Макрос) " + player.displayName + ": вероятность " + string.Format("{0}", prob) + "% | обнаружений " + info.Violations.ToString() + ".");
				SendDetection(player, string.Format("<color=green>[Античит детект]</color> " + "(<color=red>NoRecoil</color>) " + "У игрока " + player.displayName + " обнаружен NoRecoil " + ",вероятность " + string.Format("{0}", prob) + "% | обнаружений " + info.Violations.ToString() ));
				LogToFile("log", $"(Макрос) " + player.displayName + ": вероятность " + string.Format("{0}", prob) + "% | обнаружений " + info.Violations.ToString() + " | " + item.info.shortname, this, false);
                if (info.Violations > (int)Config["Количество детектов для автоматического бана за NoRecoil:"])
                {
                    Ban(player.userID, "[Анти-чит] Обнаружен NoRecoil");
					SendDetection(player, string.Format("<color=green>[Античит детект]</color> " + "(<color=red>NoRecoil</color>) " + player.displayName + "забанен. Обнаружен NoRecoil!"));
                    LogToFile("log", $"{player.displayName}({player.userID}) забанен! Обнаружен NoRecoil!", this, false);
                }
            }

            info.Ticks = 0;
            info.Count = 0;
        }



        [HookMethod("OnBasePlayerAttacked")]
        private void OnBasePlayerAttacked(BasePlayer player, HitInfo hitInfo)
        {
            if (player.IsDead()) return;
            if (hitInfo.Initiator == null) return;
            if (player.health - hitInfo.damageTypes.Total() > 0f) return;
            BasePlayer attacker = hitInfo.Initiator.ToPlayer();
            if (attacker == null) return;
            if (attacker == player) return;
            WallhackKillCheck(player, attacker, hitInfo);
        }

        #region Other Methods

        public static void msgPlayer(BasePlayer player, string msg)
        {
            player.ChatMessage($"[Анти-Чит] {msg}");
        }

        public static void msgAll(string msg)
        {
            ConsoleNetwork.BroadcastToAllClients("chat.add", 0, $"[Анти-Чит] {msg}");
        }

        #endregion

        [HookMethod("Init")]
        private void Init()
        {
            perm.RegisterPermission("anticheat.admin", this);
			perm.RegisterPermission("anticheat.send", this);

            Interface.Oxide.GetLibrary<ru.Libraries.Command>(null).AddConsoleCommand("unban.user", this, "UnbanCommand");
            Interface.Oxide.GetLibrary<ru.Libraries.Command>(null).AddConsoleCommand("aim.check", this, "AimCheck");
            Interface.Oxide.GetLibrary<ru.Libraries.Command>(null).AddConsoleCommand("aim.server", this, "AimCheckServer");
            Interface.Oxide.GetLibrary<ru.Libraries.Command>(null).AddConsoleCommand("check.server", this, "CheckServer");
            Aim = Interface.Oxide.DataFileSystem.ReadObject<List<AimData>>("AimData");
            Tops = Interface.Oxide.DataFileSystem.ReadObject<List<TopData>>("TopData");
            LoadedPlayerData = Interface.Oxide.DataFileSystem.ReadObject<HashSet<PlayerData>>("Blacklist");
            foreach (var player in BasePlayer.activePlayerList)
            {
                var check = (from x in Tops where x.UID == player.UserIDString select x).Count();
                if (check == 0) CreateInfo(player);
                new PluginTimers(this).Once(2f, () => CheckSpeed(player));
                new PluginTimers(this).Once(2f, () => AimPlayer(player));
            }
            new PluginTimers(this).Repeat(Convert.ToInt32(Config["[Топ-Игроков] Время Между Сообщениями"]), 0, () =>
     {
         var TopPlayer = (from x in Tops select x).OrderByDescending(x => x.УбийствPVP).Take(1);
                 int r = Core.Random.Range(1, 6);
                 if (r == 1)
                 {
                     TopPlayer = (from x in Tops select x).OrderByDescending(x => x.УбийствPVP).Take(1);
                     foreach (var top in TopPlayer) rust.BroadcastChat($"<size=16><color={Convert.ToString(Config["[Топ-Игроков] Цвет Оповещаний"])}>TOP Киллер</color> - {top.Ник} ({top.УбийствPVP})</size>");
                 }
                 else if (r == 2)
                 {
                     TopPlayer = (from x in Tops select x).OrderByDescending(x => x.УбийствЖивотных).Take(1);
                     foreach (var top in TopPlayer) rust.BroadcastChat($"<size=16><color={Convert.ToString(Config["[Топ-Игроков] Цвет Оповещаний"])}>TOP Охотник</color> - {top.Ник} ({top.УбийствЖивотных})</size>");
                 }
                 else if (r == 3)
                 {
                     TopPlayer = (from x in Tops select x).OrderByDescending(x => x.РакетВыпущено + x.ВзрывчатокИспользовано).Take(1);
                     foreach (var top in TopPlayer) rust.BroadcastChat($"<size=16><color={Convert.ToString(Config["[Топ-Игроков] Цвет Оповещаний"])}>TOP Рейдер</color> - {top.Ник} ({top.РакетВыпущено + top.ВзрывчатокИспользовано})</size>");
                 }
                 else if (r == 4)
                 {
                     TopPlayer = (from x in Tops select x).OrderByDescending(x => x.РесурсовСобрано).Take(1);
                     foreach (var top in TopPlayer) rust.BroadcastChat($"<size=16><color={Convert.ToString(Config["[Топ-Игроков] Цвет Оповещаний"])}>TOP Фармер</color> - {top.Ник} ({top.РесурсовСобрано})</size>");
                 }
				  else if (r == 5)
                 {
                     TopPlayer = (from x in Tops select x).OrderByDescending(x => x.NPCУбито).Take(1);
                     foreach (var top in TopPlayer) rust.BroadcastChat($"<size=16><color={Convert.ToString(Config["[Топ-Игроков] Цвет Оповещаний"])}>TOP Убийц NPC</color> - {top.Ник} ({top.NPCУбито})</size>");
                 }
     });
        }

        [HookMethod("SaveData")]
        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Blacklist", LoadedPlayerData);
            Interface.Oxide.DataFileSystem.WriteObject("AimData", Aim);
        }




        public List<AimData> Aim = new List<AimData>();
        public class AimData
        {
            public AimData(string Игрок, string SteamId, int Попаданий, int Голова)
            {

                this.Игрок = Игрок;
                this.SteamId = SteamId;
                this.Попаданий = Попаданий;
                this.Голова = Голова;
            }


            public string Игрок { get; set; }
            public string SteamId { get; set; }
            public int Попаданий { get; set; }
            public int Голова { get; set; }

        }
        public List<Blacklist> list = new List<Blacklist>();
        public class Blacklist
        {
            public Blacklist(string Игрок, string SteamId)
            {

                this.Игрок = Игрок;
                this.SteamId = SteamId;

            }


            public string Игрок { get; set; }
            public string SteamId { get; set; }


        }
        public List<TopData> Tops = new List<TopData>();
        public class TopData
        {
            public TopData(string Ник, string UID, int РакетВыпущено, int УбийствPVP, int ВзрывчатокИспользовано, int УбийствЖивотных, int ПульВыпущено, int СтрелВыпущено, int Смертей, int ПредметовСкрафчено, int РесурсовСобрано, int ВертолётовУничтожено, int NPCУбито, int ТанковУничтожено)
            {
                this.Ник = Ник;
                this.UID = UID;
                this.РакетВыпущено = РакетВыпущено;
                this.УбийствPVP = УбийствPVP;
                this.ВзрывчатокИспользовано = ВзрывчатокИспользовано;
                this.УбийствЖивотных = УбийствЖивотных;
                this.ПульВыпущено = ПульВыпущено;
                this.СтрелВыпущено = СтрелВыпущено;
                this.Смертей = Смертей;
				this.ПредметовСкрафчено = ПредметовСкрафчено;
                this.РесурсовСобрано = РесурсовСобрано;
                this.ВертолётовУничтожено = ВертолётовУничтожено;
                this.NPCУбито = NPCУбито;
				this.ТанковУничтожено = ТанковУничтожено;
            }

            public string Ник { get; set; }
            public string UID { get; set; }
            public int РакетВыпущено { get; set; }
            public int УбийствPVP { get; set; }
            public int ВзрывчатокИспользовано { get; set; }
            public int УбийствЖивотных { get; set; }
            public int ПульВыпущено { get; set; }
            public int СтрелВыпущено { get; set; }
			public int ПредметовСкрафчено { get; set; }
            public int Смертей { get; set; }
            public int РесурсовСобрано { get; set; }
            public int ВертолётовУничтожено { get; set; }
			public int ТанковУничтожено { get; set; }
			public int NPCУбито { get; set; }
        }
    }
}
