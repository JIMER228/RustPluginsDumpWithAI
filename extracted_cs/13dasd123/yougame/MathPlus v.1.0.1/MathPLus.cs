// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("MathPlus", "Kaidoz | vk.com/kaidoz", "1.0.1")]
    [Description("Бот математик для Rust и HurtWorld Legacy/ItemV2")]

    class MathPlus : CovalencePlugin
    {
        private struct Configuration
        {
            public static int timer = 600;
            public static int rejim = 1;
            public static List<object> items = new List<object>()
            {
                "52;1;Ценное"
            };
        }

        void Loaded()
        {
            LoadDefaultConfig();
            LoadConfig();
            timer.Repeat(Configuration.timer, 0, () =>
            {
                startmath();
            });

#if HURTWORLDITEMV2
            ConvertsGuid();
#endif        
        }

        private new void LoadConfig()
        {
            GetConfig(ref Configuration.timer, "MathPlus", "Повторение(в секундах)");
            GetConfig(ref Configuration.rejim, "MathPlus", "Сложность примера(больше число-тяжелее пример)");
            GetConfig(ref Configuration.items, "MathPlus", "Список(ид;количество;название)");
            SaveConfig();
        }

        protected override void LoadDefaultConfig() => PrintWarning("Создание нового конфига для Math Plus...");

#if HURTWORLDITEMV2
        void ConvertsGuid()
        {
            for(int d = 0;d<Configuration.items.Count();d++)
            {
                string str = (string)Configuration.items[d];
                if (str[0] == '@')
                {
                    string[] idk = str.Split(';');
                    int id = Convert.ToInt32(idk[0]);
                    ItemObject item = Singleton<GlobalItemManager>.Instance.GetItem(id);
                    string guid = RuntimeHurtDB.Instance.GetGuid(item.Generator);
                    Configuration.items[d] = "@" + guid + ";" + idk[1] + ";" + idk[2];
                }
            }
            SaveConfig();
        }
#endif

        System.Random rnd = new System.Random();
        private string answer = "";

        void OnUserChat(IPlayer player, string message)
        {
            if (answer == "")
                return;

            if (message == answer)
            {
                try
                {
                    endmath(player);
                }
                catch { }
            }
        }

        void startmath()
        {
            string[] adv = generatos(Configuration.rejim).Split(';');
            answer = adv[1];

            messageall($"<color=green> <b>[Математик] </b></color>Был сгенерирован пример! \n Найдите ответ: {adv[0]}");
        }

        void endmath(IPlayer player)
        {
            answer = "";
            long steamid = Convert.ToInt64(player.Id);
            string[] adv = Convert.ToString(Configuration.items[rnd.Next(0, Configuration.items.Count())]).Split(';');
            string id = adv[0];
            int count = Convert.ToInt32(adv[1]);
            string name = adv[2];
#if RUST
            BasePlayer player_ = BasePlayer.Find(player.Id);

            if(player_==null)
                return;
            player_.inventory.GiveItem(ItemManager.CreateByItemID(Convert.ToInt32(id), count, 0), player_.inventory.containerMain);
#endif
#if HURTWORLD
            
            PlayerSession player_ = getSession(player.Name);
            if (player_ == null)
                return;
			var itemmanager = Singleton<GlobalItemManager>.Instance;
            itemmanager.GiveItem(player_.Player, itemmanager.GetItem(Convert.ToInt32(id)), count);			
#endif
#if HURTWORLDITEMV2
            PlayerSession player_ = getSession(player.Name);
            if (player_ == null)
                return;

			var itemmanager = Singleton<GlobalItemManager>.Instance;
            itemmanager.GiveItem(player_.Player, itemmanager.GetItem(RuntimeHurtDB.Instance.GetObjectByGuid<ItemGeneratorAsset>(id.Replace("@","")).GeneratorId).Generator, count);
#endif
            messageall($"<color=green> <b>[Математик] </b></color>Победитель {player.Name} .Он получил {name} в количестве {count}");
        }

        void messageall(string msg)
        {
            foreach (var a in players.Connected)
            {
                a.Reply(msg);
            }
        }

        // ДЛЯ РАСТА
#if RUST
                BasePlayer GetBasePlayer(long steamid)
                {
                    var Online = BasePlayer.activePlayerList as List<BasePlayer>;
                    foreach (BasePlayer player in Online)
                    {
                        if ((long)player.OwnerID == steamid)
                            return player;
                    }

                    return null;
                }
#endif
#if HURTWORLD

        //ДЛЯ ХАРТА

        private PlayerSession getSession(string identifier)
        {
            var sessions = GameManager.Instance.GetSessions();
            PlayerSession session = null;

            foreach (var i in sessions)
            {
                if (i.Value.Name.ToLower().Contains(identifier.ToLower()) || identifier.Equals(i.Value.SteamId.ToString()))
                {
                    session = i.Value;
                    break;
                }
            }

            return session;
        }
#endif
#if HURTWORLDITEMV2
        private PlayerSession getSession(string identifier)
        {
            var sessions = GameManager.Instance.GetSessions();
            PlayerSession session = null;

            foreach (var i in sessions)
            {
                if (i.Value.Identity.Name.ToLower().Contains(identifier.ToLower()) || identifier.Equals(i.Value.SteamId.ToString()))
                {
                    session = i.Value;
                    break;
                }
            }

            return session;
        }
#endif
        string generatos(int rejim)
        {

            int a1 = rnd.Next(230, 500) * rejim;
            int a2 = rnd.Next(5, 15) * rejim;
            int a3 = rnd.Next(10, 15) * rejim;
            int a4 = rnd.Next(10, 50) * rejim;

            int answ = a1 - a2 * a3 + a4;

            return $"{a1} - {a2} * {a3} + {a4};{answ}";
        }

        private void GetConfig<T>(ref T variable, params string[] path)
        {
            if (path.Length == 0)
                return;

            if (Config.Get(path) == null)
            {
                Config.Set(path.Concat(new object[] { variable }).ToArray());
                PrintWarning($"Added field to config: {string.Join("/", path)}");
            }

            variable = (T)Convert.ChangeType(Config.Get(path), typeof(T));
        }
    }
}