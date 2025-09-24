// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿        #region Header

using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Core;
using System.Reflection;
using Oxide.Core;
using System.Linq;
using System.Globalization;
using Facepunch;
using System.IO;
using Oxide.Game.Rust.Cui;
namespace Oxide.Plugins
{
    /*
	Проверить system.call в msg
    */
    [Info("MadMan", "Enigma", "0.1.3")]
    [Description("MadMan")]
    public class MadMan : RustPlugin
    {
        #endregion

        #region Config

        public static int maxTime = 900;
        public static string msgPrefix = "[<color=red>Mad</color> Man]";
        public static string msgCooldown = "Команда будет доступна через {0} сек.";
        public static string msgStop = "Мод остановлен";
        public static string msgError = "Произошла ошибка. Обратитесь к администратору";
        public static string msgNoOne = "Пока что никто не безумен.";
        public static string msgYouAre = "Ты безумный человек.\nТвои выстрелы наносят <color=red>2x</color> урона!\nТвоя защита от выстрелов на <color=red>50%</color> сильнее!\nУбивший тебя получит твою способность!\nЧерез <color=red>{0}</color> мин ты потеряешь способность!";
        public static string msgChatCommand = "\nБезумный человек: <color=#7b9ef6>{0}</color>\nРасстояние до него: <color=red>{1}</color> м.\nОглянитесь, на него указывает стрелка.";
        public static string msgForNew = "Ты обезумел!\nТвои выстрелы наносят <color=red>2x</color> урона!\nТвоя защита от выстрелов на <color=red>50%</color> сильнее!\nУбивший тебя получит твою способность!\nЧерез <color=red>{0}</color> мин ты потеряешь способность!";
        public static string msgYouLost = "Ты потерял своё безумие!";
        public static string msgAnnounce = "\n<color=#7b9ef6>{0}</color> безумный человек!\nЕго выстрелы наносят <color=red>2x</color> урона,\nЕго защита от пуль на <color=red>50%</color> сильнее!\nИнформация о нём: <color=lime>/mad</color>";
        public static string msgTimeOut = "Время вышло.\nТы больше не безумен!";

        public static bool AutoDrawAndDistance = false;

        #endregion

        #region Variables

        static Man someMan = null;
        Dictionary<ulong, float> antiFlood = new Dictionary<ulong, float>();
        int counter = 0;

        #endregion

        #region Chat Command
        
        [ChatCommand("mad")]
        void RunChat(BasePlayer player)
        {
            float now = UnityEngine.Time.realtimeSinceStartup;
            float value;
            if (antiFlood.TryGetValue(player.userID, out value))
            {
                if (now - value < 4f)
                {
                    var difference = 5 - (now - value);
                    msgPlayer(player, String.Format(msgCooldown, (int)difference));
                    return;
                }
            }
            counter++;
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
                msgPlayer(player, String.Format(msgYouAre, (int)(maxTime / 60)));
                return;
            }
            var from = someMan.player.transform.position;
            from.y += 750;
            var distance = Vector3.Distance(player.transform.position, someMan.player.transform.position);
            msgPlayer(player, String.Format(msgChatCommand, someMan.player.displayName, (int)distance));
            player.SendConsoleCommand("ddraw.arrow", 15f, Color.red, from, someMan.player.transform.position, 10f);
        }

        #endregion
        
        #region Man class

        public class Man : MonoBehaviour
        {
            public BasePlayer player;
            public BasePlayer previousPlayer;
            public Vector3 lastPosition;
            public float startTime;
            public bool isFound = false;

            void Awake()
            {
                someMan = this;
                FindNewMan();
                Notify();
                Update3Sec();
            }

            public void OnManDeath(BasePlayer attacker)
            {
                isFound = false;
                var msg = String.Format(msgForNew, (int)(maxTime / 60));
                msgPlayer(attacker, msg);
                msgPlayer(player, msgYouLost);
                player = attacker;
                ShowGui(attacker, msg, 0.3f, 0.35f, 0.7f, 0.65f, "0.10 0 0 0.98");
                lastPosition = player.transform.position;
                startTime = UnityEngine.Time.realtimeSinceStartup;
                isFound = true;
            }

            void Notify()
            {
                Invoke("Notify", 900f);
                if (!isFound) return;
                msgAll(String.Format(msgAnnounce, player.displayName));
            }

            public void FindNewMan()
            {
                isFound = false;
                List<BasePlayer> goodPl = new List<BasePlayer>();
                foreach (var pl in BasePlayer.activePlayerList)
                {
                    if (pl.IsWounded() || pl.IsSleeping() || pl.IsDead() || (previousPlayer != null && previousPlayer == pl)) continue;
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
                BasePlayer man = goodPl[random];
                player = man;
                var msg = String.Format(msgForNew, (int)(maxTime / 60));
                msgPlayer(player, msg);
                ShowGui(player, msg, 0.3f, 0.35f, 0.7f, 0.65f, "0.10 0 0 0.98");
                lastPosition = player.transform.position;
                isFound = true;
                someMan = this;
                startTime = UnityEngine.Time.realtimeSinceStartup;
                DrawPosition();
            }

            void DrawPosition()
            {
                if (!AutoDrawAndDistance) return;
                Invoke("DrawPosition", 30f);
                if (!isFound) return;
                var from = lastPosition;
                from.y += 750;
                foreach (var pl in BasePlayer.activePlayerList)
                {
                    var distance = Vector3.Distance(pl.transform.position, player.transform.position);
                    msgPlayer(pl, String.Format(msgChatCommand, player.displayName, (int)distance));
                    player.SendConsoleCommand("ddraw.arrow", 15f, Color.red, from, lastPosition, 10f);
                }
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
                if (UnityEngine.Time.realtimeSinceStartup - startTime > maxTime)
                {
                    msgPlayer(player, msgTimeOut);
                    FindNewMan();
                    return;
                }
                lastPosition = player.transform.position;
            }
            
            public void Destroy(string reason = null)
            {
                if (reason != null) msgAll(reason);
                UnityEngine.Object.Destroy(this);
            }
        }

        #endregion

        #region Gui

        [ConsoleCommand("madman.ok")]
        void ccmdtoggle(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            var player = arg.Player();
            CuiHelper.DestroyUi(player, "MadGui");
        }

        public static void ShowGui(BasePlayer player, string text, float x1, float y1, float x2, float y2, string color)
        {
            CuiHelper.DestroyUi(player, "MadGui");
            var elements = new CuiElementContainer();
            var panel = elements.Add(new CuiPanel
            {
                Image = { Color = color },
                RectTransform = { AnchorMin = $"{x1} {y1}", AnchorMax = $"{x2} {y2}" }, CursorEnabled = true
            },  "Overlay", "MadGui");
            elements.Add(new CuiLabel
            {
                Text = { Text = text, FontSize = 24, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0.3", AnchorMax = "1 1" }
            }, panel);
            elements.Add(new CuiButton
            {
                Button = { Command = "madman.ok", Color = "0.3 0.3 0.3 0.5" },
                RectTransform = { AnchorMin = "0.42 0.05", AnchorMax = "0.58 0.21" },
                Text = { Text = "ОК", FontSize = 20, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            }, panel);
            CuiHelper.AddUi(player, elements);
        }

        #endregion

        #region Other Methods

        public static void msgPlayer(BasePlayer player, string msg)
        {
            player.ChatMessage($"{msgPrefix} {msg}");
        }
        
        public static void msgAll(string msg)
        {
            ConsoleNetwork.BroadcastToAllClients("chat.add", 0, $"{msgPrefix} {msg}");
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
        }
        
        void Unloaded()
        {
            ConVar.Server.Log("oxide/logs/MadMan.txt", $"Прописано команды раз: {counter.ToString()}");
            foreach (var pl in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(pl, "MadGui");
            someMan.Destroy(msgStop);
        }

        #endregion

        #region Footer
    }
}
        #endregion