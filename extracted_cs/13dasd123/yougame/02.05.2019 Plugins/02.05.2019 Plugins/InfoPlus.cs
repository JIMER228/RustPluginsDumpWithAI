// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Oxide.Core;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("InfoPlus", "Kaidoz | vk.com/kaidoz", "1.5.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    [Description("Плагин с полезными функциями")]

    class InfoPlus : CovalencePlugin
      //  Слив плагинов server-rust by Apolo YouGame
    {
        void Loaded()
        {
            LoadConfig();
            pingchecker();

            if (!permission.PermissionExists("infoplus.one")) permission.RegisterPermission("infoplus.one", this);
            if (!permission.PermissionExists("infoplus.nokick")) permission.RegisterPermission("infoplus.nokick", this);
            Advert();
        }

        protected override void LoadDefaultConfig() => PrintWarning("Creating new config file for InfoPlus...");
      //  Слив плагинов server-rust by Apolo YouGame

#if HURTWORLD
        void OnServerInitialized()
        {
            if (Configuration.ConB)
                GameManager.Instance.ServerConfig.ChatConnectionMessagesEnabled = false;
        }
#endif

        private struct Configuration
        {
            public static List<object> Messages = new List<object>
            {
                "Пример1",
                "Пример2",
                "Пример3",
                "Пример4"
            };

            public static List<object> msgwlc = new List<object>
            {
                "Добро пожаловать на сервер {player}",
                "Магазин сервера магазин.ком"
            };

            public static int AdvertInterval = 10;
            public static bool AdvertB = true;
            public static bool ConB = true;
            public static bool fb = true;
            public static string Disconnect = "Отключился от сервера {player}";
            public static string Connect = "Присоединился к серверу {player} ";
            public static bool PCH = true;
            public static int PCHT = 120;
            public static int PCHM = 200;
            public static string OnlineTekst = "Онлайн: {online}; <color=red>Админов:</color> {admin}; <color=blue>Модеров:</color> {moder}";
            public static int niklegth = 30;
            public static List<object> groupcolors = new List<object>()
            {
                "admin;red",
                "moder;blue",
                "default;white"
            };
            public static bool adm_players = true;
            public static bool admin_con = true;
        }

        private new void LoadConfig()
        {
            GetConfig(ref Configuration.Messages, "Реклама", "Сообщения");
            GetConfig(ref Configuration.AdvertB, "Реклама", "Включен(false-будет отключен)");
            GetConfig(ref Configuration.AdvertInterval, "Реклама", "Интервал рекламы (в минутах)");
            GetConfig(ref Configuration.ConB, "Выход/Вход", "Включен(false-будет отключен)");
            GetConfig(ref Configuration.Disconnect, "Выход/Вход", "Вышел");
            GetConfig(ref Configuration.admin_con, "Выход/Вход", "Показывать админа");
            GetConfig(ref Configuration.Connect, "Выход/Вход", "Зашел");
            GetConfig(ref Configuration.fb, "Впервые на сервере", "Включить(false-будет отключен)");
            GetConfig(ref Configuration.msgwlc, "Впервые на сервере", "Сообщения");
            GetConfig(ref Configuration.PCH, "Пинг", "Проверка пинга");
            GetConfig(ref Configuration.PCHM, "Пинг", "Максимальный пинг");
            GetConfig(ref Configuration.PCHT, "Пинг", "Частота проверки(в секунда)");
            GetConfig(ref Configuration.OnlineTekst, "Список игроков", "Текст(для количества указывать группу)");
            GetConfig(ref Configuration.groupcolors, "Список игроков", "Список(группа;его цвет)");
            GetConfig(ref Configuration.niklegth, "Список игроков", "Количество отображаемых имен");
            GetConfig(ref Configuration.adm_players, "Список игроков", "Показывать админов в списке");
            SaveConfig();
        }

        private int advert;

        private void Advert()
        {
            if (Configuration.AdvertB)
            {
                timer.Repeat(Configuration.AdvertInterval * 60, 0, () =>
                {
                    if (Configuration.Messages.Count == 0)
                        return;

                    advert += 1;
                    if (advert > Configuration.Messages.Count - 1)
                        advert = 0;
                    server.Broadcast((string)Configuration.Messages[advert]);
                    Puts((string)Configuration.Messages[advert]);
                });
            }
        }

        private void OnUserDisconnected(IPlayer player)
        {
            if (player.IsAdmin && !Configuration.admin_con)
                return;

            if (Configuration.ConB)
                server.Broadcast((Configuration.Disconnect).Replace("{player}", player.Name));
        }

        private void OnUserConnected(IPlayer player)
        {
            if (Configuration.fb)
            {
                if (!permission.UserHasPermission(player.Id.ToString(), "infoplus.one"))
                {
                    foreach (string msg in Configuration.msgwlc)
                    {
                        player.Reply((msg).Replace("{player}", player.Name));
                    }

                    permission.GrantUserPermission(player.Id.ToString(), "infoplus.one", null);
                }
            }

            if (player.IsAdmin && !Configuration.admin_con)
                return;

            if (Configuration.ConB)
                server.Broadcast((Configuration.Connect).Replace("{player}", player.Name));
        }

        [Command("players")]
        void cmdPlayers(IPlayer player)
        {
            string playerscount = players.Connected.Count().ToString();
            int[] gplist = new int[Configuration.groupcolors.Count()];
            List<string> playerslist = new List<string>();
            for (int g = 0; g < players.Connected.Count(); g++)
            {
                var pl_ = players.Connected.ToList();

                if (!Configuration.adm_players && !pl_[g].IsAdmin)
                    continue;

                for (int _g = 0; g < Configuration.groupcolors.Count(); g++)
                {
                    string[] sp_ = Configuration.groupcolors[_g].ToString().Split(';');
                    if (permission.UserHasGroup(pl_[g].Id, sp_[0]))
                    {
                        playerslist.Add($"<color={sp_[1]}>{pl_[g].Name}</color>");
                        break;
                    }                   
                }
            }
            string msg_1 = Configuration.OnlineTekst.Replace("{online}", playerscount);

            foreach (string _mg in Configuration.groupcolors)
            {
                string[] sp_ = _mg.ToString().Split(';');
                var countm = (from x in players.Connected.ToList() where permission.UserHasGroup(x.Id.ToString(), sp_[0]) select x).Count();
                msg_1 = msg_1.Replace("{" + sp_[0] + "}", countm.ToString());
            }
            string msgful = "";

            for (int _g = 0; _g < playerslist.Count(); _g++)
            {
                try
                {
                    if (Configuration.niklegth <= _g)
                    {
                        msgful = msgful + " ...";
                        break;
                    }
                    msgful += playerslist[_g] + ", ";
                    if (_g == playerslist.Count() - 1)
                        msgful = msgful.Remove(msgful.Length - 2);
                }
                catch { }
            }
            player.Reply(msg_1 + "\n" + msgful);
        }

        [Command("online")]
        void cmdsetonline(IPlayer player) => player.Reply($"На сервере: {players.Connected.Count()}");

        [Command("pos")]
        void cmdPlayerd(IPlayer player) => player.Reply($"Ваша позиция: {player.Position()}");

        [Command("ping")]
        void cmdsetping(IPlayer player) => player.Reply($"Ваш пинг: {player.Ping}");

        void pingchecker()
        {
            if (Configuration.PCH)
            {
                timer.Repeat(Configuration.PCHT, 0, () =>
                {
                    foreach (var d in players.Connected)
                    {
                        if (d.Ping >= Configuration.PCHM && (!d.IsAdmin || !permission.UserHasPermission(d.Id.ToString(), "infoplus.nokick")))
                        {
                            d.Reply($"<color=red>!</color> Ваш пинг высокий: {d.Ping}! Возможно исключение с сервера.");
                            timer.Once(60, () =>
                            {
                                try
                                {
                                    if (d != null && d.Ping >= Configuration.PCHM)
                                    {
                                        d.Kick("Вы были исключены из-за высокого пинга. Проверьте соединение!");
                                    }
                                }
                                catch { }
                            });
                        }
                    }
                });
            }
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

        // Как жизнь?
    }
}
