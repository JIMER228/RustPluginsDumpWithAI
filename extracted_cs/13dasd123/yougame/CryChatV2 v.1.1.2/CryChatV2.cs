using System.Text.RegularExpressions;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using System.IO;
using UnityEngine;
using System.Linq;
using Oxide.Core;
using System;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("CryChat", "Kaidoz | vanya770 | vk.com/kaidoz", "1.1.2")]
    [Description("Единственная система чата которая меня не раздражает")]

    class CryChatV2 : HurtworldPlugin
    {
        #region Lists
        Dictionary<string, prefix> prefixes = new Dictionary<string, prefix>();
        List<string> wordfilter = new List<string>();
        List<string> wordnotfilter = new List<string>();
        List<Mute> mutelist = new List<Mute>();
        List<MuteVoice> mutevoicelist = new List<MuteVoice>();
        List<PM> pm_ = new List<PM>();

        #endregion

        [PluginReference("HWClans")]
        Plugin Clans;

        public class prefix
        {
            public prefix(string permission, Format_ Formats, int rank, string namecolor, string title, string titlecolor, string messagecolor)
            {
                this.permission = permission;
                this.Formats = Formats;
                this.rank = rank;
                this.namecolor = namecolor;
                this.title = title;
                this.titlecolor = titlecolor;
                this.messagecolor = messagecolor;
            }
            public string permission { get; set; }
            public Format_ Formats { get; set; }
            public int rank { get; set; }
            public string namecolor { get; set; }
            public string title { get; set; }
            public string titlecolor { get; set; }
            public string messagecolor { get; set; }
        }

        public class Format_
        {
            public Format_(string formatchat, string formatchat_clan, string formatupname, string formatupname_clan)
            {
                this.formatchat = formatchat;
                this.formatchat_clan = formatchat_clan;
                this.formatupname = formatupname;
                this.formatupname_clan = formatupname_clan;
            }
            public string formatchat { get; set; }
            public string formatchat_clan { get; set; }
            public string formatupname { get; set; }
            public string formatupname_clan { get; set; }
        }

        public class Mute
        {
            public Mute(ulong steamid, string time)
            {
                this.steamid = steamid;
                this.time = time;
            }
            public ulong steamid { get; set; }
            public string time { get; set; }
        }

        public class MuteVoice
        {
            public MuteVoice(ulong steamid, string time)
            {
                this.steamid = steamid;
                this.time = time;
            }
            public ulong steamid { get; set; }
            public string time { get; set; }
        }

        public class SYM_
        {
            public SYM_(int a, int b)
            {
                this.a = a;
                this.b = b;
            }
            public int a { get; set; }
            public int b { get; set; }
        }

        public class PM
        {
            public PM(PlayerSession pl, PlayerSession sd, string cnsl)
            {
                this.pl = pl;
                this.sd = sd;
                this.cnsl = cnsl;
            }
            public PlayerSession pl { get; set; }
            public PlayerSession sd { get; set; }
            public string cnsl { get; set; }
        }

        #region Configuration

        private struct C
        {
            public static int prefixcount = 1; // Не трогать. Возможно в бущудущем реализую
            public static int mutecount = 60;
            public static int spamcount = 60;
            public static bool mute = true;
            public static bool wordfilter = true;
            public static string unicorn = "*мат*";
            public static string clantag = "<{clantag}>";
            /*public static int infamy = 0;
            public static string infamystr = "☠";
            public static string infamy80 = "#FF1212";
            public static string infamy60 = "#FF4D4D";
            public static string infamy40 = "#FF7F7F";
            public static string infamy20 = "#FFA6A6";
            public static string infamy0 = "##FFD1D";*/

        }

        protected override void LoadDefaultConfig() => PrintWarning("Creating new config file for Cry Chat...");

        private new void LoadConfig()
        {
            GetConfig(ref C.clantag, "CryChat", "HWClans", "Clantag");
            GetConfig(ref C.mute, "CryChat", "AntiMat", "Enabled");
            GetConfig(ref C.mutecount, "CryChat", "AntiMat", "Mute Time(sec.)");
            GetConfig(ref C.wordfilter, "CryChat", "AntiMat", "ReplaceAll");
            GetConfig(ref C.unicorn, "CryChat", "AntiMat", " Unicorn");
            /*GetConfig(ref C.infamy, "CryChat", "Infamy", " Показ дурной(0 - не показывать.1 - показать покраснением ника,но пропадет его цвет.2 - показать символом)");
            GetConfig(ref C.infamystr, "CryChat", "Infamy", "Символ дурки");
            GetConfig(ref C.infamy80, "CryChat", "Infamy", "Цвета", "От 80 до 100");
            GetConfig(ref C.infamy60, "CryChat", "Infamy", "Цвета", "От 60 до 80");
            GetConfig(ref C.infamy40, "CryChat", "Infamy", "Цвета", "От 40 до 60");
            GetConfig(ref C.infamy20, "CryChat", "Infamy", "Цвета", "От 20 до 40");
            GetConfig(ref C.infamy20, "CryChat", "Infamy", "Цвета", "От 0 до 20");*/
            SaveConfig();
        }

        #endregion

        void Loaded()
        {
            LoadConfig();
            LoadData();
            LoadTitles();
            LoadPerm();
            LoadWords();
            clearmute();
        }

        void SaveDataAll()
        {
            Interface.Oxide.DataFileSystem.WriteObject("CryChat/Prefix", prefixes);
            Interface.Oxide.DataFileSystem.WriteObject("CryChat/WordFilter", wordfilter);
            Interface.Oxide.DataFileSystem.WriteObject("CryChat/Mute", mutelist);
            Interface.Oxide.DataFileSystem.WriteObject("CryChat/MuteVoice", mutevoicelist);
            Interface.Oxide.DataFileSystem.WriteObject("CryChat/WordNotFilter", wordnotfilter);
        }

        void LoadData()
        {
            try
            {
                prefixes = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, prefix>>("CryChat/Prefix");
            }
            catch (Exception)
            {
                Puts($"Ошибка в загрузе префикса.");
            }
            wordfilter = Interface.Oxide.DataFileSystem.ReadObject<List<string>>("CryChat/WordFilter");
            mutevoicelist = Interface.Oxide.DataFileSystem.ReadObject<List<MuteVoice>>("CryChat/MuteVoice");
            mutelist = Interface.Oxide.DataFileSystem.ReadObject<List<Mute>>("CryChat/Mute");
            wordnotfilter = Interface.Oxide.DataFileSystem.ReadObject<List<string>>("CryChat/WordNotFilter");
        }

        void clearmute()
        {
            List<Mute> newlist = new List<Mute>();
            List<MuteVoice> newvoicelist = new List<MuteVoice>();
            foreach (var d in mutelist)
            {
                if (DateTime.Compare(DateTime.Parse(d.time), DateTime.Now) > 0)
                {
                    newlist.Add(d);
                }
            }
            foreach (var d in mutevoicelist)
            {
                if (DateTime.Compare(DateTime.Parse(d.time), DateTime.Now) > 0)
                {
                    newvoicelist.Add(d);
                }
            }
            mutevoicelist = newvoicelist;
            mutelist = newlist;
            SaveDataAll();
        }

        void LoadPerm()
        {
            foreach (var d in prefixes as Dictionary<string, prefix>)
            {
                if (!permission.PermissionExists($"crychat.{d.Value.permission}"))
                    permission.RegisterPermission($"crychat.{d.Value.permission}", this);
            }
            if (!permission.PermissionExists("crychat.mute"))
                permission.RegisterPermission("crychat.mute", this);

            if (!permission.PermissionExists("crychat.admin"))
                permission.RegisterPermission("crychat.admin", this);
        }

        void LoadWords()
        {
            LoadFilter();
            LoadNotFilter();
            LoadData();
        }

        void LoadFilter()
        {
            webrequest.EnqueuePost("https://getfile.dokpub.com/yandex/get/https://yadi.sk/d/vyIBCmIH3SBJQU", "", (code, response) =>
            {
                response = response.Replace(",", "");

                string[] s = response.Split(' ');

                for (int d = 0; d < s.Length; d++)
                {
                    if (!wordfilter.Contains(s[d]) && s[d] != "")
                    {
                        wordfilter.Add(s[d]);
                        //SaveDataAll();
                        Interface.Oxide.DataFileSystem.WriteObject("CryChat/WordFilter", wordfilter);
                    }
                }
            }, this);
        }

        void LoadNotFilter()
        {
            webrequest.EnqueuePost("https://getfile.dokpub.com/yandex/get/https://yadi.sk/d/myci1cMY3SBJNc", "", (code_, response_) =>
            {
                response_ = response_.Replace(",", "");

                string[] s = response_.Split(' ');

                for (int d = 0; d < s.Length; d++)
                {
                    if (!wordnotfilter.Contains(s[d]) && (s[d] != ""))
                    {
                        wordnotfilter.Add(s[d]);
                        //SaveDataAll();
                        Interface.Oxide.DataFileSystem.WriteObject("CryChat/WordNotFilter", wordnotfilter);
                    }
                }
            }, this);
        }

        void LoadTitles()
        {
            // string permission, string formatchat,string formatupname, int rank,string namecolor, string title, string titlecolor, string messagecolor
            int def_t = (from x in prefixes where x.Key == "default" select x).Count();

            if (def_t == 0)
            {
                var playerformat = new Format_("{title} {name}: {message}", "{title} {clan} {name}: {message}", "{name}", "{clan} {name}");
                prefixes.Add("default", new prefix("default", playerformat, 0, "white", "[Игрок]", "orange", "white"));
            }

            int adm_t = (from x in prefixes where x.Key == "admin" select x).Count();

            if (adm_t == 0)
            {
                var adminformat = new Format_("{title} {name}: {message}", "{title} {name}: {message}", "{title} {name}", "{title} {name}");
                prefixes.Add("admin", new prefix("admin", adminformat, 10, "white", "[Админ]", "red", "white"));
            }
            SaveDataAll();
        }

        #region PM

        [ChatCommand("pm")]
        void pmcry(PlayerSession session, string command, string[] args)
        {
            if (args.Length == 0)
            {
                send(session, "<color=#88a6fe>[PM]</color> Синтакс: /pm <ник> <сообщение>");
                return;
            }
            if (args.Length >= 2)
            {
                List<PlayerSession> nik = find(args[0]);

                if (nik.Count != 1)
                {
                    string b = "";
                    send(session, "<color=#88a6fe>[PM]</color> Найдено несколько игроков: ");
                    foreach (PlayerSession d in nik)
                    {
                        b += $"{d.Identity.Name} ,";
                    }
                    send(session, b.Substring(0, b.Length - 2));
                    return;
                }
                else if (nik == null)
                {
                    send(session, "<color=#88a6fe>[PM]</color> Игрок не найден");
                    return;
                }
                send(session, $"<color=#88a6fe>[PM для </color>{nik[0].Identity.Name}<color=#88a6fe>]</color> : {string.Join(" ", args, 1, args.Length - 1)}");
                send(nik[0], $"<color=#88a6fe>[PM от </color>{session.Identity.Name}<color=#88a6fe>]</color> : {string.Join(" ", args, 1, args.Length - 1)}");
                int pm_c = (from x in pm_ where x.pl == nik[0] select x).Count();

                if (pm_c == 0)
                {
                    pm_.Add(new PM(nik[0], session, ""));
                    return;
                }
                else
                {
                    var pm_s = (from x in pm_ where x.pl == session select x).LastOrDefault();
                    pm_s.sd = session;
                }
            }
        }

        [ChatCommand("r")]
        void answerpmcry(PlayerSession session, string command, string[] args)
        {
            if (args.Length == 0)
            {
                send(session, "<color=#88a6fe>[R]</color> Синтакс: /r <сообщение>");
                return;
            }
            if (args.Length >= 1)
            {
                int pm_c = (from x in pm_ where x.pl == session select x).Count();

                if (pm_c == 0)
                {
                    send(session, "<color=#88a6fe>[R]</color> Вам некому ответить!");
                    return;
                }

                var pm_s = (from x in pm_ where x.pl == session select x).LastOrDefault();
                string msg = string.Join(" ", args, 0, args.Length);

                if (!IsValidSession(pm_s.sd) && pm_s.cnsl == "")
                {
                    send(session, "<color=#88a6fe>[R]</color> Игрока нет в сети!");
                    return;
                }
                else if (pm_s.cnsl == "console")
                {
                    Puts($"[PM от {session.Identity.Name}] : {msg}");
                    send(session, $"<color=#88a6fe>[PM для </color>CONSOLE<color=#88a6fe>]</color> : {msg}");
                    return;
                }
                send(pm_s.sd, $"<color=#88a6fe>[PM от </color>{session.Identity.Name}<color=#88a6fe>]</color> : {msg}");
                send(session, $"<color=#88a6fe>[PM для </color>{session.Identity.Name}<color=#88a6fe>]</color> : {msg}");
            }
        }

        object OnServerCommand(string arg)
        {
            string[] args = arg.Split(new char[] { ' ' });

            if (args[0] == "say")
            {
                if (args.Length == 1)
                {
                    Puts("say <игрок>");
                    return null;
                }

                string msg = string.Join(" ", args, 2, args.Length - 2);
                Puts(sendconsole(args[1], msg));
            }
            if (args[0] == "sayall")
            {
                if (args.Length == 1)
                {
                    Puts("sayall <сообщение>");
                    return null;
                }
                string msg = string.Join(" ", args, 1, args.Length - 1);
                broadcast($"<color=#88a6fe>[Консоль]</color> : {msg}");
            }
            return null;
        }

        string sendconsole(string name, string msg)
        {
            List<PlayerSession> nik = find(name);

            if (nik.Count != 1)
            {
                string b = "";

                foreach (PlayerSession d in nik)
                {
                    b += $"{d.Identity.Name} ,";
                }
                return "Найдено несколько игроков: {b}";
            }
            else if (nik == null)
            {
                return "Игрок не найден";
            }

            send(nik[0], $"<color=#88a6fe>[PM от </color>CONSOLE<color=#88a6fe>]</color> : {msg}");

            int pm_c = (from x in pm_ where x.pl == nik[0] select x).Count();

            if (pm_c == 0)
            {
                pm_.Add(new PM(nik[0], null, "console"));
            }
            else
            {
                var pm_s = (from x in pm_ where x.pl == nik[0] select x).LastOrDefault();
                pm_s.sd = null;
                pm_s.cnsl = "console";
            }
            return $"[PM для {nik[0].Identity.Name}] : {msg}";
        }

        #endregion

        #region MuteChat

        [ChatCommand("unmute")]
        void unmutecry(PlayerSession session, string command, string[] args)
        {
            if (!perm(session, "crychat.mute"))
            {
                send(session, "У вас нет прав");
                return;
            }
            if (args.Length == 0)
            {
                send(session, "<color=#88a6fe>[UnMute]</color> Синтакс: /unmute <ник или стимид>");
                return;
            }
            if (args.Length == 1)
            {
                List<PlayerSession> nik = find(args[0]);

                if (nik.Count > 1)
                {
                    string b = "";
                    send(session, "<color=#88a6fe>[UnMute]</color> Найдено несколько игроков: ");
                    foreach (PlayerSession d in nik)
                    {
                        b += $"{d.Identity.Name} ,";
                    }
                    send(session, b.Substring(0, b.Length - 2));
                    return;
                }
                else if (nik == null)
                {
                    send(session, "<color=#88a6fe>[UnMute]</color> Игрок не найден");
                    return;
                }
                if ((from x in mutelist where x.steamid == (ulong)nik[0].SteamId select x).Count() >= 1)
                {
                    var steamid = (from x in mutelist where x.steamid == (ulong)nik[0].SteamId select x).LastOrDefault();
                    mutelist.Remove(steamid);
                    broadcast($"<color=red>!</color> Был снят мут с игрока {nik[0].Identity.Name}");
                    SaveDataAll();
                }
                else
                {
                    send(session, "<color=#88a6fe>[UnMute]</color> У игрока нет мута");
                }
            }
        }

        [ChatCommand("mute")]
        void mutecry(PlayerSession session, string command, string[] args)
        {
            if (!perm(session, "crychat.mute"))
            {
                send(session, "У вас нет прав");
                return;
            }
            if (args.Length == 0)
            {
                send(session, "<color=#88a6fe>[Mute]</color> Синтакс: /mute <ник> <время:m.h>");
                return;
            }
            if (args.Length >= 2)
            {
                List<PlayerSession> nik = find(args[0]);

                if (nik.Count > 1)
                {
                    string b = "";
                    send(session, "<color=#88a6fe>[Mute]</color> Найдено несколько игроков: ");
                    foreach (PlayerSession d in nik)
                    {
                        b += $"{d.Identity.Name} ,";
                    }
                    send(session, b.Substring(0, b.Length - 2));
                    return;
                }
                else if (nik == null)
                {
                    send(session, "<color=#88a6fe>[Mute]</color> Игрок не найден");
                    return;
                }
                else if (!mutesession(nik[0]))
                {
                    send(session, "<color=#88a6fe>[Mute]</color> У игрока уже есть мут.");
                    return;
                }
                if (Mute_player(nik[0], args[1], string.Join(" ", args, 2, args.Length - 2), false) == "error")
                    send(session, "<color=#88a6fe>[MuteVoice]</color> Произошла ошибка! Сообщите vk.com/kaidoz");
                Log("Mute", $" Был выдан мут игроком {session.Identity.Name} игроку {nik[0].Identity.Name} | {nik[0].SteamId}");
                return;
            }
            send(session, "<color=#88a6fe>[Mute]</color> Синтакс: /mute <ник> <время:m.h> <причина>");
        }

        #endregion

        #region VoiceChat

        [ChatCommand("unmutevoice")]
        void unmutevoicecry(PlayerSession session, string command, string[] args)
        {
            if (!perm(session, "crychat.mute"))
            {
                send(session, "У вас нет прав");
                return;
            }
            if (args.Length == 0)
            {
                send(session, "<color=#88a6fe>[UnMuteVoice]</color> Синтакс: /unmute <ник или стимид>");
                return;
            }
            if (args.Length == 1)
            {
                List<PlayerSession> nik = find(args[0]);

                if (nik.Count > 1)
                {
                    string b = "";
                    send(session, "<color=#88a6fe>[UnMuteVoice]</color> Найдено несколько игроков: ");
                    foreach (PlayerSession d in nik)
                    {
                        b += $"{d.Identity.Name} ,";
                    }
                    send(session, b.Substring(0, b.Length - 2));
                    return;
                }
                else if (nik == null)
                {
                    send(session, "<color=#88a6fe>[UnMuteVoice]</color> Игрок не найден");
                    return;
                }
                if ((from x in mutevoicelist where x.steamid == (ulong)nik[0].SteamId select x).Count() >= 1)
                {
                    var steamid = (from x in mutevoicelist where x.steamid == (ulong)nik[0].SteamId select x).LastOrDefault();
                    mutevoicelist.Remove(steamid);
                    broadcast($"<color=red>!</color> Был снят мут ВойсЧата с игрока {nik[0].Identity.Name}");
                    SaveDataAll();
                }
                else
                {
                    send(session, "<color=#88a6fe>[UnMuteVoice]</color> У игрока нет мута ВойсЧата");
                }
            }
        }

        [ChatCommand("mutevoice")]
        void mutevoicecry(PlayerSession session, string command, string[] args)
        {
            if (!perm(session, "crychat.mute"))
            {
                send(session, "У вас нет прав");
                return;
            }
            if (args.Length == 0)
            {
                send(session, "<color=#88a6fe>[MuteVoice]</color> Синтакс: /mutevoice <ник> <время:m.h>");
                return;
            }
            if (args.Length >= 2)
            {
                List<PlayerSession> nik = find(args[0]);

                if (nik.Count > 1)
                {
                    string b = "";
                    send(session, "<color=#88a6fe>[MuteVoice]</color> Найдено несколько игроков: ");
                    foreach (PlayerSession d in nik)
                    {
                        b += $"{d.Identity.Name} ,";
                    }
                    send(session, b.Substring(0, b.Length - 2));
                    return;
                }
                else if (nik == null)
                {
                    send(session, "<color=#88a6fe>[MuteVoice]</color> Игрок не найден");
                    return;
                }
                else if (!mutesession(nik[0]))
                {
                    send(session, "<color=#88a6fe>[MuteVoice]</color> У игрока уже есть мут.");
                    return;
                }
                if (Mute_player(nik[0], args[1], string.Join(" ", args, 2, args.Length - 2), true) == "error")
                    send(session, "<color=#88a6fe>[MuteVoice]</color> Произошла ошибка! Сообщите vk.com/kaidoz");
                Log("Mute", $" Был выдан мут ВойсЧата игроком {session.Identity.Name} игроку {nik[0].Identity.Name} | {nik[0].SteamId}");
                return;
            }
            send(session, "<color=#88a6fe>[Mute]</color> Синтакс: /mutevoice <ник> <время:m.h> <причина>");
        }

        object OnPlayerVoice(PlayerSession session)
        {
            if (!mutevoicesession(session))
            {
                return true;
            }
            return null;
        }

        #endregion

        object OnPlayerChat(PlayerSession session, string message)
        {
            if (!mutesession(session))
                return false;

            message = RemoveTags(message);

            if (message.Length == 0)
                return false;
            // ФИЛЬТР МАТА
            if (C.mute)
                message = GetFilteredMesssage(session, message);
            // ФИЛЬТР ИП
            message = filterip(session, message);

            List<prefix> titles = new List<prefix>();

            foreach (var jkh in prefixes as Dictionary<string, prefix>)
            {
                if (permission.UserHasPermission(session.SteamId.ToString(), "crychat." + jkh.Value.permission) || jkh.Value.permission == "default" || (session.IsAdmin && jkh.Value.permission == "admin"))
                {
                    titles.Add(jkh.Value);
                }
            }

            var title_ = (from x in titles select x).OrderByDescending(x => x.rank).Take(1);
            string chata = format_chat(session, message, title_.ElementAt(0));
            broadcast(chata);
            return false;
        }

        string format_chat(PlayerSession session, string message, prefix titilesa)
        {
            string nameplayer = session.Identity.Name.Replace("{", "");
            string title = titilesa.title;
            string titlecolor = nocolor(titilesa.titlecolor);
            string namecolor = nocolor(titilesa.namecolor);
            string formatchat = titilesa.Formats.formatchat;
            string messagecolor = nocolor(titilesa.messagecolor);

            if (!string.IsNullOrEmpty(clantag(session)))
            {
                formatchat = titilesa.Formats.formatchat_clan
                   .Replace("{title}", $"<color={titlecolor}>{title}</color>")
                   .Replace("{clan}", $"{clantag(session)}")

                   .Replace("{name}", $"<color={namecolor}>{nameplayer}</color>")
                   .Replace("{message}", $"<color={messagecolor}>{message}</color>");
            }
            else
            {
                formatchat = formatchat
                   .Replace("{title}", $"<color={titlecolor}>{title}</color>")
                   .Replace("{name}", $"<color={namecolor}>{nameplayer}</color>")
                   .Replace("{message}", $"<color={messagecolor}>{message}</color>");
            }
            return formatchat;
        }

        string format_upname(PlayerSession session, prefix titilesa)
        {
            string nameplayer = session.Identity.Name.Replace("{", "");
            string title = titilesa.title;
            string titlecolor = nocolor(titilesa.titlecolor);
            string namecolor = nocolor(titilesa.namecolor);
            string formatupname = titilesa.Formats.formatupname;
            //string infamys = infamy(session);

            /*if (!formatupname.ToLower().Contains("{inf}") && C.infamy == 2)
                nameplayer += $"{infamys}";
            else if (C.infamy == 1)
                namecolor = nameplayer;
            else*/
            nameplayer = $"<color={namecolor}>{nameplayer}</color>";

            if (string.IsNullOrEmpty(clantag(session)))
            {
                formatupname = titilesa.Formats.formatupname_clan
                    .Replace("{title}", $"<color={titlecolor}>{title}</color>")
                    .Replace("{clan}", $"{clantag(session)}")
                    .Replace("{name}", $"{nameplayer}");

                /*if (formatupname.ToLower().Contains("{inf}") && C.infamy == 2)
                    formatupname = formatupname.Replace("{inf}", infamys);*/
            }
            else
            {
                formatupname = formatupname
                    .Replace("{title}", $"<color={titlecolor}>{title}</color>")
                    .Replace("{name}", $"{nameplayer}");

                /*if (formatupname.ToLower().Contains("{inf}") && C.infamy == 2)
                    formatupname = formatupname.Replace("{inf}", infamys);*/
            }
            return formatupname;
        }

        [ChatCommand("prefix")]
        void MyPrefix(PlayerSession session)
        {
            List<prefix> titles = new List<prefix>();

            foreach (var jkh in prefixes as Dictionary<string, prefix>)
            {
                if (permission.UserHasPermission(session.SteamId.ToString(), "crychat." + jkh.Value.permission) || jkh.Value.permission == "default" || (session.IsAdmin && jkh.Value.permission == "admin"))
                {
                    titles.Add(jkh.Value);
                }
            }

            var title_ = (from x in titles select x).OrderByDescending(x => x.rank).Take(1);
            string upname = format_upname(session, title_.ElementAt(0));
            string chata = format_chat(session, "", title_.ElementAt(0));
            send(session, "<color=#88a6fe>[Chat]</color> Ваши префиксы: ");
            send(session, $"ЧАТ: {chata}");

            send(session, $"НАД ГОЛОВОЙ: {upname}");
        }

        [ChatCommand("chat.admin")]
        void admincmd(PlayerSession session, string command, string[] args)
        {
            if (!perm(session, "crychat.admin"))
            {
                send(session, "У вас нет прав");
                return;
            }

            if (args.Length == 1 && args[0].ToLower().Contains("reload"))
            {
                send(session, "<color=#88a6fe>[Chat]</color> Плагин перезагружен");
                Loaded();
                return;
            }
            if (args.Length == 1 && args[0].ToLower().Contains("test"))
            {
                send(session, "<color=#88a6fe>[Chat]</color> Добавлен тестовый префикс");
                var playerformat = new Format_("{title} {name}: {message}", "{title} {clan} {name}: {message}", "{name}", "{clan} {name}");
                prefixes.Add("test", new prefix("permissiontest", playerformat, 1, "white", "[Тест]", "orange", "white"));
                SaveDataAll();
            }
        }

        bool perm(PlayerSession session, string perm) => session.IsAdmin || permission.UserHasPermission(session.SteamId.ToString(), perm);

        //void OnPlayerConnected(PlayerSession session)
        //{
        //    OnPlayerRespawn(session);
        //}

        //void OnPlayerRespawn(PlayerSession session)
        //{
        //    List<prefix> titles = new List<prefix>();

        //    foreach (var jkh in prefixes as Dictionary<string, prefix>)
        //    {
        //        if (permission.UserHasPermission(session.SteamId.ToString(), "crychat." + jkh.Value.permission) || jkh.Value.permission == "default" || (session.IsAdmin && jkh.Value.permission == "admin"))
        //        {
        //            titles.Add(jkh.Value);
        //        }
        //    }

        //    var title_ = (from x in titles select x).OrderByDescending(x => x.rank).Take(1);
        //    string upname = format_upname(session, title_.ElementAt(0));
        //    session.WorldPlayerEntity.GetComponent<HurtMonoBehavior>().RPC("UpdateName", uLink.RPCMode.OthersExceptOwnerBuffered, upname);
        //}

        string Mute_player(PlayerSession session, string m, string reason, bool voice)
        {
            int mutetime = 1;
            int checktime = 1;

            if (m.ToLower().EndsWith("m"))
                checktime = 60;
            else
            if (m.ToLower().EndsWith("h"))
                checktime = 3600;
            else
            if (m.ToLower().EndsWith("d"))
                checktime = 86400;
            if(checktime!=1)
                mutetime = Convert.ToInt32(m.Remove(m.Length - 1));
            else
                mutetime = Convert.ToInt32(m);

            string msg = "";
            try
            {
                if (!voice)
                {
                    msg = $"<color=red>!</color> Был замучен {session.Identity.Name} {mutetime * checktime} секунд";
                    if (reason == "")
                        broadcast(msg);
                    else
                        broadcast(msg + $"\n Причина: {reason}");

                    var unmute = DateTime.Now.AddSeconds(mutetime * checktime);
                    mutelist.Add(new Mute((ulong)session.SteamId, Convert.ToString(unmute)));
                }
                else
                {
                    msg = $"<color=red>!</color> Был замучен войсчат {session.Identity.Name} {mutetime * checktime} секунд";
                    if (reason == "")
                        broadcast(msg);
                    else
                        broadcast(msg + $"\n Причина: {reason}");

                    var unmute = DateTime.Now.AddSeconds(mutetime * checktime);
                    mutevoicelist.Add(new MuteVoice((ulong)session.SteamId, Convert.ToString(unmute)));
                }
            }
            catch (Exception ex)
            {
                Puts("Error: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);
                Log("Errors", string.Format("Error: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace));
                return "error";
            }
            SaveDataAll();
            return "";
        }


        string clantag(PlayerSession session)
        {
            if (session.Identity.Clan != null)
            {
                //return session.WorldPlayerEntity.GetComponent<Clan>().ClanTag;
                return $"<color=#{session.Identity.Clan.ClanColor.GetHexCode().Remove(6)}>[{session.Identity.Clan.ClanTag}]</color>";
            }
            return "";
        }

        string noskob(string text) => text.Replace("[", "").Replace("]", "");

        int integer(string a)
        {
            int ads = 0;
            a = a.Substring(1, a.Length - 1);
            if (int.TryParse(a, out ads))
            {
                return ads;
            }
            return ads;
        }

        bool mutevoicesession(PlayerSession session)
        {
            if ((from x in mutevoicelist where x.steamid == (ulong)session.SteamId select x).Count() == 0) return true;

            int result = DateTime.Compare(DateTime.Parse((from x in mutevoicelist where x.steamid == (ulong)session.SteamId select x).LastOrDefault().time), DateTime.Now);

            if (result <= 0)
                return true;

            TimeSpan ts = DateTime.Parse((from x in mutevoicelist where x.steamid == (ulong)session.SteamId select x).LastOrDefault().time) - DateTime.Now;
            notice(session, $"Заблокировано! Осталось: {Math.Round(ts.TotalSeconds)} секунд");
            // СООБЩЕНИЕ О МУТЕ
            return false;
        }

        bool mutesession(PlayerSession session)
        {
            if ((from x in mutelist where x.steamid == (ulong)session.SteamId select x).Count() == 0) return true;

            int result = DateTime.Compare(DateTime.Parse((from x in mutelist where x.steamid == (ulong)session.SteamId select x).LastOrDefault().time), DateTime.Now);

            if (result <= 0)
                return true;

            TimeSpan ts = DateTime.Parse((from x in mutelist where x.steamid == (ulong)session.SteamId select x).LastOrDefault().time) - DateTime.Now;
            send(session, $"<color=red>@</color> Вы замучены! Осталось времени: {Math.Round(ts.TotalSeconds)} секунд.");
            // СООБЩЕНИЕ О МУТЕ
            return false;
        }

        /// ОСТАЛЬНОЕ
        string GetFilteredMesssage(PlayerSession session, string msg)
        {
            var vv = mat_i(msg);

            if (vv.Count() != 0)
            {
                int nv = vv.Count();

                try
                {
                    for (int a = 0; a < nv; a++)
                    {
                        string mat = msg.Substring(vv[a].a, vv[a].b);

                        if (C.wordfilter)
                        {
                            msg = msg.Replace(mat, C.unicorn);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Puts($"{ex}");
                }

                var unmute = DateTime.Now.AddSeconds(C.mutecount);
                mutelist.Add(new Mute((ulong)session.SteamId, Convert.ToString(unmute)));
                send(session, $"<color=red>!</color> Вы были замучены {C.mutecount} секунд за мат!");
            }
            return msg;
        }

        string filterip(PlayerSession session, string text)
        {
            bool spam = false;
            string pattern = "[0-9]+.+[0-9]+.+[0-9]+.+[0-9]";
            string pattern2 = "[A-Za-zА-Яа-я0-9-�-��-�]+\\.(com|lt|net|org|gg|ru|��|int|info|ru.com|ru.net|com.ru|net.ru|���|org.ru|moscow|biz|���|������|msk.ru|su|msk.su|md|tj|kz|tm|pw|travel|name|de|eu|eu.com|coin|com.de|me|org.lv|pl|nl|at|co.at|be|wien|info.pl|cz|ch|com.pl|or.at|net.pl|org.pl|hamburg|cologne|koeln|berlin|de.com|es|biz.pl|bayern|scot|edu|edu.pl|money|com.es|nom.es|nom|nom.pl|brussels|org.es|gb|gb.net|shop|shop.pl|waw|waw.pl|wales|vlaanderen|gr.com|hu|hu.net|si|se|se.net|cymru|melbourne|im|sk|lat|gent|co.uk|uk|com.im|co.im|co|org.uk|me.uk|ist|saarland|org.im|istanbul|uk.net|uk.com|li|lu|gr|london|eu.com|lv|ro|com.ro|fi|net.fv|fv|com.lv|net.lv|as|asia|ind.in|net.ph|org.ph|io|jp|qa|ae.org|ae|ph|ind|af|jp.net|sa.com|sa|tl|tw|tv|tokyo|jpn.com|jpn|net.af|com.af|nagoya|org.af|com.tw|cn|cn.com|cx|la|club|club.tw|idv.tw|idv|yokohama|ebiz|ebiz.tw|mn|christmas|in|game|game.tw|to|com.my|co.in|in.net|net.in|net.my|org.my|ist|istanbul|pk|org.in|in.net|ph|com.ph|firm|firm.in|gen|gen.in|us|us.com|net.ec|ec|info.ec|co.lc|lc|com.lc|net.lc|org.lc|pro|pro.ec|med|med.ec|la|us.org|ag|gl|mx|com.mx|fin|fin.ec|co.ag|gl|mx|com.mx|pe|co.gl|com.gl|com.ag|net.ag|org.ag|net.gl|org.gl|net.pe|com.pe|gs|org.pe|nom|nom.ag|gy|sr|sx|bz|br|br.com|co.gy|co.bz|com.gy|vc|com.vc|net.vc|net.gy|hn|net.bz|com.bz|org.bz|com.hn|org.vc|co.ve|ve|net.hn|quebec|cl|org.hn|com.ve|ht|vegas|com.co|nyc|co.com|com.ht|us.com|miami|net.ht|org.ht|nom.co|nom|net.co|ec|info.ht|us.org|lc|com.ec|ac|as|mu|com.mu|tk|ws|net.mu|cc|cd|nf|org.mu|za|za.com|co.za|org.za|net.za|com.nf|net.nf|co.cm|cm|com.cm|org.nf|web|web.za|net.cm|ps|nu|net.so|nz|fm|irish|co.nz|radio|radio.fm|gg|net.nz|ml|com.ki|net.ki|ki|cf|org.nz|sb|com.sb|net.sb|tv|mg|srl|fm|sc|org.sb|biz.ki|org.ki|je|info.ki|net.sc|com.sc|durban|joburg|cc|capetown|sh|org.sc|ly|com.ly|ms|so|st|xyz|north-kazakhstan.su|nov|nov.su|ru.com|ru.net|com.ru|net.ru|org.ru|pp|pp.ru|msk.ru|msk|msk.su|spb|spb.ru|spb.su|tselinograd.su|ashgabad.su|abkhazia.su|adygeya.ru|adygeya.su|arkhangelsk.su|azerbaijan.su|balashov.su|bashkiria.ru|bashkiria.su|bir|bir.ru|bryansk.su|obninsk.su|penza.su|pokrovsk.su|pyatigorsk.ru|sochi.su|tashkent.su|termez.su|togliatti.su|troitsk.su|tula.su|tuva.su|vladikavkaz.su|vladikavkaz.ru|vladimir.ru|vladimir.su|spb.su|tatar|com.ua|kiev.ua|co.ua|biz.ua|pp.ua|am|co.am|com.am|net.am|org.am|net.am|radio.am|armenia.su|georgia.su|com.kz|bryansk.su|bukhara.su|cbg|cbg.ru|dagestan.su|dagestan.ru|grozny.su|grozny.ru|ivanovo.su|kalmykia.ru|kalmykia.su|kaluga.su|karacol.su|karelia.su|khakassia.su|krasnodar.su|kurgan.su|lenug.su|com.ua|ru.com|����.��|���������.��|�����.��|�����������.��|�������.��|���������.��|���������.��|��������.��|��������.��|���������.��|��������.��|���������.��|vologda.su|org.kz|aktyubinsk.su|chimkent.su|east-kazakhstan.su|jambyl.su|karaganda.su|kustanal.ru|mangyshlak.su|kiev.ua|co.ua|biz.ua|radio.am|nov.ru|navoi.sk|nalchik.su|nalchik.ru|mystis.ru|murmansk.su|mordovia.su|mordovia.ru|marine.ru|tel|aero|mobi|xxx|aq|ax|az|bb|ba|be|bg|bi|bj|bh|bo|bs|bt|ca|cat|cd|cf|cg|ch|ci|ck|co.ck|co.ao|co.bw|co.id|id|co.fk|co.il|co.in|il|ke|ls|co.ls|mz|no|co.mz|co.no|th|tz|co.th|co.tz|uz|uk|za|zm|zw|co.uz|co.uk|co.za|co.zm|co.zw|ar|au|cy|eg|et|fj|gt|gu|gn|gh|hk|jm|kh|kw|lb|lr|com.ai|com.ar|com.au|com.bd|com.bn|com.br|com.cn|com.cy|com.eg|com.et|com.fj|com.gh|com.gu|com.gn|com.gt|com.hk|com.jm|com.kh|com.kw|com.lb|com.lr|com.|com.|bd|mt|mv|ng|ni|np|nr|om|pa|py|qa|sa|sb|sg|sv|sy|tr|tw|ua|uy|ve|vi|vn|ye|coop|com.mt|com.mv|com.ng|com.ni|com.np|com.nr|com.om|com.pa|com.pl|com.py|com.qa|com.sa|com.sb|com.sv|com.sg|com.sy|com.tr|com.tw|com.ua|com.uy|com.ve|com.vi|com.vn|com.ye|cr|cu|cx|cv|cz|de|de.com|dj|dk|dm|do|dz|ec|edu|ee|es|eu|eu.com|fi|fo|fr|qa|qd|qf|gi|gl|gm|gp|gr|gs|gy|hk|hm|hr|ht|hu|ie|im|in|in.ua|io|ir|is|it|je|jo|jobs|jp|kg|ki|kn|kr|la|li|lk|lt|lu|lv|ly|ma|mc|md|me.uk|mg|mk|mo|mp|ms|mu|museum|mw|mx|my|na|nc|ne|nl|no|nf|nu|pe|ph|pk|pl|pn|pr|ps|pt|re|ro|rs|rw|sd|se|sg|sh|si|sk|sl|sm|sn|so|sr|st|sz|tc|td|tg|tj|tk|tl|tn|to|tt|tw|ug|us|vg|vn|vu|ws)";
            Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);

            if (new Regex(pattern, RegexOptions.IgnoreCase).IsMatch(text) || new Regex(pattern2, RegexOptions.IgnoreCase).IsMatch(text))
            {
                spam = true;
                text = rgx.Replace(text, "*спам*").Trim();
            }
            if (spam)
            {
                var unmute = DateTime.Now.AddSeconds(C.spamcount);
                mutelist.Add(new Mute((ulong)session.SteamId, Convert.ToString(unmute)));
                send(session, $"<color=red>!</color> Вы были замучены {C.mutecount} секунд за спам!");
            }
            return text;
        }

        List<SYM_> mat_i(string text)
        {
            text = text.ToLower();

            List<SYM_> mat_z = new List<SYM_>();
            var antimat_ = mat_o(text);

            for (int mn = 0; mn < wordfilter.Count(); mn++)
            {
                for (int d = 0; d < text.Length; d++)
                {
                    if (d + wordfilter[mn].Length <= text.Length)
                    {
                        string op = string.Empty;

                        op = text.Substring(d, wordfilter[mn].Length);
                        if (checktext(antimat_, d, wordfilter[mn].Length) && wordfilter[mn] == op)
                        {
                            mat_z.Add(new SYM_(d, wordfilter[mn].Length));
                        }
                    }
                }
            }
            return mat_z;
        }

        List<SYM_> mat_o(string text)
        {
            List<SYM_> mat_z = new List<SYM_>();

            for (int mn = 0; mn < wordnotfilter.Count(); mn++)
            {
                for (int d = 0; d < text.Length; d++)
                {
                    string op = string.Empty;

                    if (d + wordnotfilter[mn].Length <= text.Length)
                        op = text.Substring(d, wordnotfilter[mn].Length);
                    else
                        return mat_z;
                    if (wordnotfilter[mn] == op)
                    {
                        mat_z.Add(new SYM_(mn, wordfilter[mn].Length));
                    }
                }
            }
            if (mat_z.Count() != 0)
            {
                return mat_z;
            }
            return null;
        }

        bool checktext(List<SYM_> ads, int _1, int _2)
        {
            if (ads == null)
                return true;
            for (int j = 0; j < ads.Count(); j++)
            {
                if (ads[j].a < _1 && ads[j].b > _2)
                    return false;
            }
            return true;
        }

        List<PlayerSession> find(string name)
        {
            int count = 0;
            var sessions = GameManager.Instance?.GetSessions()?.Values.Where(IsValidSession).ToList();
            List<PlayerSession> sess = new List<PlayerSession>();
            List<PlayerSession> sess_ = new List<PlayerSession>();

            foreach (PlayerSession a in sessions)
            {
                try
                {
                    if (a.Identity.Name.Length >= name.Length && a.Identity.Name.Substring(0, name.Length).ToLower() == name.ToLower())
                    {
                        sess_.Add(a);
                        count++;
                    }
                }
                catch { }
                if (a.Identity.Name.Contains(name))
                    sess.Add(a);
            }
            if (sess_.Count() == 1)
                return sess_;

            return sess;
        }

        /*string infamy(PlayerSession session)
        {
            var entityStats = session?.WorldPlayerEntity?.GetComponent<EntityStats>();
            float inf = entityStats.GetFluidEffect(EEntityFluidEffectType.Infamy).GetValue();
            string sym = $"{C.infamystr}";
            string color = string.Empty;
            if (inf >= 80)
            {
                color = C.infamy80;
                sym = $"<color={nocolor(color)}>" + sym + "</color>";
            }
            else if (inf >= 60)
            {
                color = C.infamy60;
                sym = $"<color={nocolor(color)}>" + sym + "</color>";
            }
            else if (inf >= 40)
            {
                color = C.infamy40;
                sym = $"<color={nocolor(color)}>" + sym + "</color>";
            }
            else if (inf >= 20)
            {
                color = C.infamy20;
                sym = $"<color={nocolor(color)}>" + sym + "</color>";
            }
            else if (inf > 0)
            {
                sym = $"<color={nocolor(color)}>" + sym + "</color>";
            }
            else
                sym = "";

            return sym;
        }*/

        string nocolor(string color)
        {
            if (color == "")
                return "white";

            return color;
        }

        string RemoveTags(string phrase)
        {
            List<string> forbiddenTags = new List<string>{
                 "</color>",
                 "</size>",
                 "<b>",
                 "</b>",
                 "<i>",
                 "</i>",
                 "{",
                 "}"
             };

            //	Replace Color Tags
            phrase = new Regex("(<color=.+?>)").Replace(phrase, "");
            //	Replace Size Tags
            phrase = new Regex("(<size=.+?>)").Replace(phrase, "");
            phrase = new Regex("(<.+?>)").Replace(phrase, "");

            foreach (string tag in forbiddenTags)
                phrase = phrase.Replace(tag, "");
            return phrase;
        }

        private string HexConverter(Color c)
        {
            return "#" + c.r.ToString("X2") + c.g.ToString("X2") + c.b.ToString("X2");
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

        public bool IsValidSession(PlayerSession session)
        {
            return session?.SteamId != null && session.IsLoaded && session.Identity.Name != null && session.Identity != null &&
                   session.WorldPlayerEntity?.transform?.position != null;
        }

        void Log(string filename, string text)
        {
            string time = DateTime.Now.ToString("hh:mm");
            LogToFile(filename, $"[{time}]" + " " + text, this);
        }

        void notice(PlayerSession session, string s) => Singleton<AlertManager>.Instance.GenericTextNotificationServer(s, session.Player);

        void send(PlayerSession session, string msg) => hurt.SendChatMessage(session, null, msg);

        void broadcast(string msg) => Server.Broadcast(msg);
    }
}