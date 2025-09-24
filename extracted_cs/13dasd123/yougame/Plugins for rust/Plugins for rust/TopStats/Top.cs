// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using System.Globalization;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Top", "s1m0n", "1.0.4")]
    [Description("Топ игроков для сервера")]



    class Top : RustPlugin
    {
        //Создаем конфиг 
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Создание конфига");
            Config.Clear();
            Config["ЦветПанели"] = "0.0 0.0 0.0 0.8";
            //Config["КлавишаДляБинда"] = "P";
            Config["ВремяМеждуСообщениями"] = 300f;
            Config["ЦветОповещаний"] = "#ffa500";
            SaveConfig();
        }

        //Создаем чат команды
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
                        top.ПредметовСкрафчено = 0;
                        top.РесурсовСобрано = 0;
                        top.ВертолётовУничтожено = 0;
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
            string cvet = Convert.ToString(Config["ЦветПанели"]);
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
            if (arg.Player() == null)
                return;
            BasePlayer player = arg.Player();
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("Панелька", null, null, null, null));
        }

        void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
        {
            if (victim.ToPlayer() != null)
            {
                string death = Convert.ToString(victim.ToPlayer().userID);
                TopData con = (from x in Tops where x.UID == death select x).FirstOrDefault();
                //Считаем смерти
                con.Смертей += 1;
                Saved();
            }
            if (!info.Initiator) return;
            if (info.Initiator.ToPlayer())
            {
                string killer = Convert.ToString(info.Initiator.ToPlayer().userID);
                TopData con2 = (from x in Tops where x.UID == killer select x).FirstOrDefault();
                //Считаем убийства животных

                if ((bool)victim?.name?.Contains("agents"))
                {
                    con2.УбийствЖивотных += 1;
                }
                //Считаем количество сбитых "Гвинтокрилів"
                if (victim.name.Contains("patrolhelicopter.prefab") && victim.name.Contains("gibs"))
                {
                    con2.ВертолётовУничтожено += 1;
                }
                //Считаем убийства игроков
                if (victim.ToPlayer() != null && victim.ToPlayer() != info.Initiator.ToPlayer())
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
        //Считаем выстрелы
        void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
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
            Saved();
        }
        //Считаем крафт
        void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            if (task.owner is BasePlayer)
            {
                TopData con = (from x in Tops where x.UID == Convert.ToString(task.owner.userID) select x).FirstOrDefault();
                con.ПредметовСкрафчено += 1;
                Saved();
            }
        }

        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            DoGather(player, item);
        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (entity == null || !(entity is BasePlayer) || item == null || dispenser == null) return;
            if (entity.ToPlayer() is BasePlayer)
                DoGather(entity.ToPlayer(), item);
        }

        //Подсчитываем количество собраных ресурсов
        void DoGather(BasePlayer player, Item item)
        {
            if (player == null) return;
            //item.amount = (int)(item.amount);
            TopData con = (from x in Tops where x.UID == Convert.ToString(player.userID) select x).FirstOrDefault();
            con.РесурсовСобрано += item.amount;
            Saved();
            return;
        }

        void CreateInfo(BasePlayer player)
        {
            if (player == null) return;
            Tops.Add(new TopData((string)player.displayName, player.UserIDString, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
            Saved();
        }

        //Добавляем игрока в Базу Данных и Биндим клавишу
        void OnPlayerInit(BasePlayer player)
        {
            //timer.Once(2f, () =>
            //{
            //    player.SendConsoleCommand($"bind {Convert.ToString(Config["КлавишаДляБинда"])} top.show");
            //});
            var check = (from x in Tops where x.UID == player.UserIDString select x).Count();
            if (check == 0) CreateInfo(player);
            //Обновляем игровой ник
            TopData con = (from x in Tops where x.UID == Convert.ToString(player.userID) select x).FirstOrDefault();
            con.Ник = (string)player.displayName;
            Saved();
        }

        //Загружаем TopData.json и проверяем есть ли все игроки в Базе Данных
        void Loaded()
        {
            Tops = Interface.Oxide.DataFileSystem.ReadObject<List<TopData>>("TopData");
            foreach (var player in BasePlayer.activePlayerList)
            {
                var check = (from x in Tops where x.UID == player.UserIDString select x).Count();
                if (check == 0) CreateInfo(player);
            }
            //что то делаем
            timer.Repeat(Convert.ToInt32(Config["ВремяМеждуСообщениями"]), 0, () =>
             {
                 var TopPlayer = (from x in Tops select x).OrderByDescending(x => x.УбийствPVP).Take(1);
                 int r = Core.Random.Range(1, 5);
                 if (r == 1)
                 {
                     TopPlayer = (from x in Tops select x).OrderByDescending(x => x.УбийствPVP).Take(1);
                     foreach (var top in TopPlayer) rust.BroadcastChat($"<size=16><color={Convert.ToString(Config["ЦветОповещаний"])}>TOP Киллер</color> - {top.Ник} ({top.УбийствPVP})</size>");
                 }
                 else if (r == 2)
                 {
                     TopPlayer = (from x in Tops select x).OrderByDescending(x => x.УбийствЖивотных).Take(1);
                     foreach (var top in TopPlayer) rust.BroadcastChat($"<size=16><color={Convert.ToString(Config["ЦветОповещаний"])}>TOP Охотник</color> - {top.Ник} ({top.УбийствЖивотных})</size>");
                 }
                 else if (r == 3)
                 {
                     TopPlayer = (from x in Tops select x).OrderByDescending(x => x.РакетВыпущено + x.ВзрывчатокИспользовано).Take(1);
                     foreach (var top in TopPlayer) rust.BroadcastChat($"<size=16><color={Convert.ToString(Config["ЦветОповещаний"])}>TOP Рейдер</color> - {top.Ник} ({top.РакетВыпущено + top.ВзрывчатокИспользовано})</size>");
                 }
                 else if (r == 4)
                 {
                     TopPlayer = (from x in Tops select x).OrderByDescending(x => x.РесурсовСобрано).Take(1);
                     foreach (var top in TopPlayer) rust.BroadcastChat($"<size=16><color={Convert.ToString(Config["ЦветОповещаний"])}>TOP Фармер</color> - {top.Ник} ({top.РесурсовСобрано})</size>");
                 }
             });
        }

        //Сохраняем инфу в TopData
        void Saved()
        {
            Interface.Oxide.DataFileSystem.WriteObject("TopData", Tops);
        }

        public List<TopData> Tops = new List<TopData>();
        public class TopData
        {
            public TopData(string Ник, string UID, int РакетВыпущено, int УбийствPVP, int ВзрывчатокИспользовано, int УбийствЖивотных, int ПульВыпущено, int СтрелВыпущено, int Смертей, int ПредметовСкрафчено, int РесурсовСобрано, int ВертолётовУничтожено)
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
            }

            public string Ник { get; set; }
            public string UID { get; set; }
            public int РакетВыпущено { get; set; }
            public int УбийствPVP { get; set; }
            public int ВзрывчатокИспользовано { get; set; }
            public int УбийствЖивотных { get; set; }
            public int ПульВыпущено { get; set; }
            public int СтрелВыпущено { get; set; }
            public int Смертей { get; set; }
            public int ПредметовСкрафчено { get; set; }
            public int РесурсовСобрано { get; set; }
            public int ВертолётовУничтожено { get; set; }
        }


    }
}