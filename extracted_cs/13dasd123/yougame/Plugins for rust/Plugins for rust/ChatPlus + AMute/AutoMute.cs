using System;
using System.Collections.Generic;
using Oxide.Core;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using System.Globalization;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using System.Linq;
using Rust;


namespace Oxide.Plugins
{
    [Info("AutoMute", "Mr. Gr1me", "1.0.4")]
    [Description("AutoMute для сервера Rust Oxide ")]
    class AutoMute : RustPlugin
    {
        void OnPlayerChat(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                return;
            BasePlayer player = arg.Player();
            foreach (var Reply in Chat)
            {
                foreach (var spec in Reply.Say)
                {
                    if (arg.Args[0].ToLower().Contains(spec))
                    {
                        bool exclud = true;
                        string SteamID = player.UserIDString;
                        string Nickname = player.displayName;
                        string str = arg.GetString(0, null);
                        string message = str.ToUpper();
                        foreach (var exc in Reply.Exclusion)
                        {
                            if (arg.Args[0].ToLower().Contains(exc)) exclud = false;
                        }
                        if (player.net?.connection?.authLevel < 1) // Отключён мут на администратора (1 - Модератор и Администратор, 2 - Только администратор ).
                            if (exclud)
                            {
                                rust.RunServerCommand($"mute " + SteamID + " 2m"); // Время мута: 1d - день, 1h - час, 1m - минута, 1s - секунда.
                                Puts("Auto mute " + Nickname + "(" + SteamID + ") 2m reason: (" + message + ")");
                                LogToFile("log", $"({DateTime.Now.ToShortDateString()}) ({DateTime.Now.ToShortTimeString()}) Игроку {Nickname} ({SteamID}) был заблокирован  чат на 2 минуты. Сообщение: ({message})",this, false);
                                return;
                            }
                    }
                }
            }
            return;
        }
        public List<MuteList> Chat = new List<MuteList>();
        public class MuteList
        {
            public MuteList(string[] Say, string[] Exclusion)
            {
                this.Say = Say;
                this.Exclusion = Exclusion;
            }

            public string[] Say { get; set; }
            public string[] Exclusion { get; set; }
        }

        #region [HELPERS]
        void Loaded()
        {
           
            int checkChat = (from x in Chat select x).Count();
            if (checkChat == 0)
            {
                Chat.Add(new MuteList(new string[] {
    "бля",
    "еба",
    "аху",
    "впиз",
    "въеб",
    "выбля",
    "выеб",
    "выёб",
    "гнид",
    "гонд",
    "доеб",
    "долбо",
    "дроч",
    "ёб",
    "елд",
    "заеб",
    "заёб",
    "залуп",
    "захуя",
    "заяб",
    "злоеб",
    "ипа",
    "лох",
    "лошар",
    "манд",
    "мля",
    "мраз",
    "муд",
    "наеб",
    "наёб",
    "напизд",
    "нах",
    "нех",
    "нии",
    "обоср",
    "отпиз",
    "отъеб",
    "оху",
    "падл",
    "падон",
    "педр",
    "пез",
    "перд",
    "пид",
    "пиз",
    "подъеб",
    "поеб",
    "поёб",
    "похе",
    "похр",
    "поху",
    "придур",
    "приеб",
    "проеб",
    "разху",
    "разъеб",
    "распиз",
    "соси",
    "спиз",
    "сук",
    "суч",
    "трах",
    "ублю",
    "уеб",
    "уёб",
    "ху",
    "целка",
    "чмо",
    "шалав",
    "шлюх",
    "ска"},
        new string[] {
    "мандар",
      "мудр",
      "наха",
      "нахо",
      "нахл",
      "нехо",
      "нехв",
      "неха",
      "пидж",
      "похуд",
      "сосиск",
      "худ",
      "хуж",
      "хут",
      "хур",
      "хулиг",
      "Команда",
      "команда",
      "команду",
      "тебе",
      "тебя",
      "скайп",
      "скайпу",
      "скайпе",
      "сказано",
      "стёб",
      "стеб"
            }
            ));
			Interface.Oxide.DataFileSystem.WriteObject("AutoMute", Chat);
                Puts("Маты, исключения и ответ добавлять в /Data/AutoMute.json");
            }
        }

        #endregion


    }
}
