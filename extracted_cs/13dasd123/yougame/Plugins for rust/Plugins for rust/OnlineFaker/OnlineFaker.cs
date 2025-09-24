// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Facepunch;
using Facepunch.Math;
using Facepunch.Steamworks;
using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
namespace Oxide.Plugins
{
    [Info("OnlineFaker", "RustPlugin.ru", "1.2.2")]
    class OnlineFaker : RustPlugin
    {
        public bool Active = true;
        private bool Modded = true;
        private bool Real = false;
        private int FreeCount = 5;
        // Стартовое значение фейкового онлайна
        static int FAKE_ONLINE = 10;

        // 7 часов
        private int MAX_FAKE_ONLINE7 = 45;
        private int MIN_FAKE_ONLINE7 = 40;
        // 8 часов
        private int MAX_FAKE_ONLINE8 = 58;
        private int MIN_FAKE_ONLINE8 = 50;
        // 9 часов
        private int MAX_FAKE_ONLINE9 = 70;
        private int MIN_FAKE_ONLINE9 = 65;
        // 10 часов
        private int MAX_FAKE_ONLINE10 = 85;
        private int MIN_FAKE_ONLINE10 = 67;
        // 11 часов
        private int MAX_FAKE_ONLINE11 = 111;
        private int MIN_FAKE_ONLINE11 = 102;
        // 12 часов
        private int MAX_FAKE_ONLINE12 = 113;
        private int MIN_FAKE_ONLINE12 = 110;
        // 13 часов
        private int MAX_FAKE_ONLINE13 = 120;
        private int MIN_FAKE_ONLINE13 = 110;
        // 14 часов
        private int MAX_FAKE_ONLINE14 = 131;
        private int MIN_FAKE_ONLINE14 = 125;
        // 15 часов
        private int MAX_FAKE_ONLINE15 = 140;
        private int MIN_FAKE_ONLINE15 = 134;
        // 16 часов
        private int MAX_FAKE_ONLINE16 = 151;
        private int MIN_FAKE_ONLINE16 = 143;
        // 17 часов
        private int MAX_FAKE_ONLINE17 = 164;
        private int MIN_FAKE_ONLINE17 = 150;
        // 18 часов
        private int MAX_FAKE_ONLINE18 = 170;
        private int MIN_FAKE_ONLINE18 = 160;
        // 19 часов
        private int MAX_FAKE_ONLINE19 = 173;
        private int MIN_FAKE_ONLINE19 = 162;
        // 20 часов
        private int MAX_FAKE_ONLINE20 = 164;
        private int MIN_FAKE_ONLINE20 = 150;
        // 21 часов
        private int MAX_FAKE_ONLINE21 = 158;
        private int MIN_FAKE_ONLINE21 = 150;
        // 22 часов
        private int MAX_FAKE_ONLINE22 = 130;
        private int MIN_FAKE_ONLINE22 = 115;
        // 23 часов
        private int MAX_FAKE_ONLINE23 = 90;
        private int MIN_FAKE_ONLINE23 = 75;
        // 00 часов
        private int MAX_FAKE_ONLINE0 = 70;
        private int MIN_FAKE_ONLINE0 = 50;



        // night time settings
        static int FAKE_ONLINE_NIGHT = 10;
        private int MAX_FAKE_ONLINE_NIGHT = 50;
        private int MIN_FAKE_ONLINE_NIGHT = 15;

        // Максимальное изменение онлайна, при обновлении
        private int MAX_STEP_FAKE = 3;
        // Минимальное изменение онлайна, при обновлении
        private int MIN_STEP_FAKE = -4;

        static readonly System.Random random = new System.Random();

        void Loaded()
        {
            LoadConfigVals();
            GenerateOnline();
        }
        void LoadConfigVals()
        {
            GetConfig("Основные настройки", "Активен ли плагин", ref Active);
            GetConfig("Основные настройки", "Раздел сервера(true - modded, false - community)", ref Modded);
            GetConfig("Основные настройки", "Выводить ли реальный онлайн(если true то выводимый онлайн будет = реальный + фейк)", ref Real);
            GetConfig("Основные настройки", "Максимальное изменение онлайна при обновлении", ref MAX_STEP_FAKE);
            GetConfig("Основные настройки", "Минимальное изменение онлайна при обновлении", ref MIN_STEP_FAKE);
            GetConfig("Основные настройки", "Количество свободных слотов если фэйковый онлайн превышает максимальный", ref FreeCount);

            GetConfig("7 часов", "Максимальный онлайн", ref MAX_FAKE_ONLINE7);
            GetConfig("7 часов", "Минимальный онлайн", ref MIN_FAKE_ONLINE7);

            GetConfig("8 часов", "Максимальный онлайн", ref MAX_FAKE_ONLINE8);
            GetConfig("8 часов", "Минимальный онлайн", ref MIN_FAKE_ONLINE8);

            GetConfig("9 часов", "Максимальный онлайн", ref MAX_FAKE_ONLINE9);
            GetConfig("9 часов", "Минимальный онлайн", ref MIN_FAKE_ONLINE9);

            GetConfig("10 часов", "Максимальный онлайн", ref MAX_FAKE_ONLINE10);
            GetConfig("10 часов", "Минимальный онлайн", ref MIN_FAKE_ONLINE10);

            GetConfig("11 часов", "Максимальный онлайн", ref MAX_FAKE_ONLINE11);
            GetConfig("11 часов", "Минимальный онлайн", ref MIN_FAKE_ONLINE11);

            GetConfig("12 часов", "Максимальный онлайн", ref MAX_FAKE_ONLINE12);
            GetConfig("12 часов", "Минимальный онлайн", ref MIN_FAKE_ONLINE12);

            GetConfig("13 часов", "Максимальный онлайн", ref MAX_FAKE_ONLINE13);
            GetConfig("13 часов", "Минимальный онлайн", ref MIN_FAKE_ONLINE13);

            GetConfig("14 часов", "Максимальный онлайн", ref MAX_FAKE_ONLINE14);
            GetConfig("14 часов", "Минимальный онлайн", ref MIN_FAKE_ONLINE14);

            GetConfig("15 часов", "Максимальный онлайн", ref MAX_FAKE_ONLINE15);
            GetConfig("15 часов", "Минимальный онлайн", ref MIN_FAKE_ONLINE15);

            GetConfig("16 часов", "Максимальный онлайн", ref MAX_FAKE_ONLINE16);
            GetConfig("16 часов", "Минимальный онлайн", ref MIN_FAKE_ONLINE16);

            GetConfig("17 часов", "Максимальный онлайн", ref MAX_FAKE_ONLINE17);
            GetConfig("17 часов", "Минимальный онлайн", ref MIN_FAKE_ONLINE17);

            GetConfig("18 часов", "Максимальный онлайн", ref MAX_FAKE_ONLINE18);
            GetConfig("18 часов", "Минимальный онлайн", ref MIN_FAKE_ONLINE18);

            GetConfig("19 часов", "Максимальный онлайн", ref MAX_FAKE_ONLINE19);
            GetConfig("19 часов", "Минимальный онлайн", ref MIN_FAKE_ONLINE19);

            GetConfig("20 часов", "Максимальный онлайн", ref MAX_FAKE_ONLINE20);
            GetConfig("20 часов", "Минимальный онлайн", ref MIN_FAKE_ONLINE20);

            GetConfig("21 час", "Максимальный онлайн", ref MAX_FAKE_ONLINE21);
            GetConfig("21 час", "Минимальный онлайн", ref MIN_FAKE_ONLINE21);

            GetConfig("22 часа", "Максимальный онлайн", ref MAX_FAKE_ONLINE22);
            GetConfig("22 часа", "Минимальный онлайн", ref MIN_FAKE_ONLINE22);

            GetConfig("23 часа", "Максимальный онлайн", ref MAX_FAKE_ONLINE23);
            GetConfig("23 часа", "Минимальный онлайн", ref MIN_FAKE_ONLINE23);

            GetConfig("0 часов", "Максимальный онлайн", ref MAX_FAKE_ONLINE0);
            GetConfig("0 часов", "Минимальный онлайн", ref MIN_FAKE_ONLINE0);

            GetConfig("1-7 часов(ночь)", "Максимальный онлайн", ref MAX_FAKE_ONLINE_NIGHT);
            GetConfig("1-7 часов(ночь)", "Минимальный онлайн", ref MIN_FAKE_ONLINE_NIGHT);
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за приобритение плагина на сайте RustPlugin.ru. Если вы приобрели этот плагин на другом ресурсе знайте - это лишает вас гарантированных обновлений!");
        }
        void GenerateOnline()
        {
            timer.Once(30, GenerateOnline);

            int step = random.Next(MIN_STEP_FAKE, MAX_STEP_FAKE + 1);

            if (DateTime.Now.Hour >= 11 && 12 > DateTime.Now.Hour)
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE11, MAX_FAKE_ONLINE11);
            }
			else if (DateTime.Now.Hour >= 10 && 11 >= DateTime.Now.Hour)
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE10, MAX_FAKE_ONLINE10);
            }
			else if (DateTime.Now.Hour >= 13 && 14 > DateTime.Now.Hour)
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE13, MAX_FAKE_ONLINE13);
            }
			else if (DateTime.Now.Hour >= 12 && 13 > DateTime.Now.Hour)
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE12, MAX_FAKE_ONLINE12);
            }
			else if (DateTime.Now.Hour >= 14 && 15 > DateTime.Now.Hour)
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE14, MAX_FAKE_ONLINE14);
            }
			else if (DateTime.Now.Hour >= 15 && 16 > DateTime.Now.Hour)
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE15, MAX_FAKE_ONLINE15);
            }
			else if (DateTime.Now.Hour >= 16 && 17 > DateTime.Now.Hour)
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE16, MAX_FAKE_ONLINE16);
            }
			else if (DateTime.Now.Hour >= 17 && 18 > DateTime.Now.Hour)
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE17, MAX_FAKE_ONLINE17);
            }
			else if (DateTime.Now.Hour >= 18 && 19 > DateTime.Now.Hour)
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE18, MAX_FAKE_ONLINE18);
            }
			else if (DateTime.Now.Hour >= 19 && 20 > DateTime.Now.Hour)
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE19, MAX_FAKE_ONLINE19);
            }
			else if (DateTime.Now.Hour >= 20 && 21 > DateTime.Now.Hour)
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE20, MAX_FAKE_ONLINE20);
            }
			else if (DateTime.Now.Hour >= 21 && 22 > DateTime.Now.Hour)
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE21, MAX_FAKE_ONLINE21);
            }
			else if (DateTime.Now.Hour >= 22 && 23 > DateTime.Now.Hour)
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE22, MAX_FAKE_ONLINE22);
            }
			else if (DateTime.Now.Hour >= 23 && 0 > DateTime.Now.Hour)
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE23, MAX_FAKE_ONLINE23);
            }
			else if (DateTime.Now.Hour >= 0 && 1 > DateTime.Now.Hour)
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE0, MAX_FAKE_ONLINE0);
            }
			else if(DateTime.Now.Hour >= 9 && 10 > DateTime.Now.Hour )
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE9, MAX_FAKE_ONLINE9);
            }
			else if(DateTime.Now.Hour >= 8 && 9 > DateTime.Now.Hour )
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE8, MAX_FAKE_ONLINE8);
            }
			else if(DateTime.Now.Hour >= 7 && 8 > DateTime.Now.Hour )
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE + step, MIN_FAKE_ONLINE7, MAX_FAKE_ONLINE7);
            }
			else if(DateTime.Now.Hour >= 1 && 7 > DateTime.Now.Hour )
            {
                FAKE_ONLINE = Mathf.Clamp(FAKE_ONLINE_NIGHT + step, MIN_FAKE_ONLINE_NIGHT, MAX_FAKE_ONLINE_NIGHT);
            }
        }
        object OnUSI()
        {
            if (!Active) return null;

            PrintWarning($"Реальных игроков на сервере:{BasePlayer.activePlayerList.Count}. Онлайн: {GetFakeOnline()}");
            string text = "stok";
            string text2 = string.Format("born{0}", Epoch.FromDateTime(global::SaveRestore.SaveCreatedTime));
            string mod = Modded ? "oxide,modded" : "community";
            string gameTags = string.Format(mod + ",mp{0},cp{1},qp{5},v{2}{3},h{4},{6},{7}", new object[]
        {
            ConVar.Server.maxplayers,
            GetFakeOnline(),
            Rust.Protocol.network,
            (!ConVar.Server.pve) ? string.Empty : ",pve",
            "null",
            SingletonComponent<global::ServerMgr>.Instance.connectionQueue.Queued,
            text,
            text2
        });
            Rust.Global.SteamServer.GameTags = gameTags;
            string[] array = ConVar.Server.description.SplitToChunks(100).ToArray<string>();
            for (int i = 0; i < 16; i++)
            {
                if (i < array.Length)
                {
                    Rust.Global.SteamServer.SetKey(string.Format("description_{0:00}", i), array[i]);
                }
                else
                {
                    Rust.Global.SteamServer.SetKey(string.Format("description_{0:00}", i), string.Empty);
                }
            }
            Rust.Global.SteamServer.SetKey("hash", "null");
            Rust.Global.SteamServer.SetKey("world.seed", global::World.Seed.ToString());
            Rust.Global.SteamServer.SetKey("world.size", global::World.Size.ToString());
            Rust.Global.SteamServer.SetKey("pve", ConVar.Server.pve.ToString());
            Rust.Global.SteamServer.SetKey("headerimage", ConVar.Server.headerimage);
            Rust.Global.SteamServer.SetKey("url", ConVar.Server.url);
            Rust.Global.SteamServer.SetKey("uptime", ((int)UnityEngine.Time.realtimeSinceStartup).ToString());
            Rust.Global.SteamServer.SetKey("gc_mb", global::Performance.report.memoryAllocations.ToString());
            Rust.Global.SteamServer.SetKey("gc_cl", global::Performance.report.memoryCollections.ToString());
            Rust.Global.SteamServer.SetKey("fps", global::Performance.report.frameRate.ToString());
            Rust.Global.SteamServer.SetKey("fps_avg", global::Performance.report.frameRateAverage.ToString("0.00"));
            Rust.Global.SteamServer.SetKey("ent_cnt", global::BaseNetworkable.serverEntities.Count.ToString());
            Rust.Global.SteamServer.SetKey("build", BuildInfo.Current.Scm.ChangeId);
            return false;

        }
        int GetFakeOnline()
        {
            if (!Active) return BasePlayer.activePlayerList.Count;

            int online = FAKE_ONLINE;
            if (Real)
            {
                online = BasePlayer.activePlayerList.Count + FAKE_ONLINE;
            }
            if (online > ConVar.Server.maxplayers)
            {
                if (BasePlayer.activePlayerList.Count >= ConVar.Server.maxplayers)
                {
                    online = ConVar.Server.maxplayers;
                }
                else
                {
                    online = ConVar.Server.maxplayers - FreeCount;
                }
            }
            return online;
        }
        private void GetConfig<T>(string Menu,string Key, ref T var)
        {
            if (Config[Menu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[Menu, Key], typeof(T));
            }
            Config[Menu, Key] = var;
        }
    }
}