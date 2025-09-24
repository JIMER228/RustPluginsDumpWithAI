// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("CommercialNick", "https://discord.gg/dNGbxafuJn", "1.0.6")]
    [Description("Плагин позволяющий давать награду за приставку в нике , например название вашего сервера")]

    class CommercialNick : RustPlugin
    {
        #region config
        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Наградить за что то в нике:")]
            public List<string> CONF_BlockedParts;

            [JsonProperty("Настройки")]
            public setings seting;

            [JsonProperty("Настройка префикса")]
            public setingpref setingsprefix;
        }

        public class setings
        {
            [JsonProperty("Использовать выдачу баланса ?")]
            public bool GameStore;
            [JsonProperty("Сколько выдать руб")]
            public int GameStorePrize;
            [JsonProperty("Store id")]
            public string storeid;
            [JsonProperty("Store key")]
            public string storekey;
            [JsonProperty("Использовать выдачу привилегии")]
            public bool commands;
            [JsonProperty("Команда для выдачи")]
            public string commandsgo;
            [JsonProperty("Названия того что он получит от команды")]
            public string commandprize;
            [JsonProperty("Время которое нужно отыграть игроку с приставкой в нике что бы получить награду (секнды)")]
            public int timeplay;
            [JsonProperty("Разршить после вайпа получть приз заного")]
            public bool wipeclear;                                                                                     
        }
        public class setingpref
        {
            [JsonProperty("Префикс")]
            public string prefix;

            [JsonProperty("Цвет префикса")]
            public string prefixcolor;

            [JsonProperty("SteamId Для аватарки")]
            public ulong steamids ;
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration()
            {
                CONF_BlockedParts = new List<string>()
                {
                   "PONYLAND",
                   "PONY LAND",
                   "pony land",
                   "ponyland",
                   "Pony Land",
                },
                seting = new setings
                {
                    GameStore = false,
                    GameStorePrize = 10,
                    storeid = "storeid",
                    storekey = "storekey",
                    commands = false,
                    commandsgo = "addgroup %STEAMID% vip 1d",
                    commandprize = "vip",
                    timeplay = 1800,
                    wipeclear = true,

                },
                setingsprefix = new setingpref
                {
                    prefix = "PrefixName",
                    prefixcolor = "#816AD0",
                    steamids = 76561198854646370
                }
                
            };
            SaveConfig(config);
        }

        void SaveConfig(Configuration config)
        {
            Config.WriteObject(config, true);
            SaveConfig();
        }

        public void LoadConfigVars()
        {
            config = Config.ReadObject<Configuration>();
            Config.WriteObject(config, true);
        }

        #endregion

        #region data
        private Dictionary<ulong, PlayerInfo> ConnectedPlayers = new Dictionary<ulong, PlayerInfo>();

        private class PlayerInfo
        {
            public string playername { get; set; }
            public bool prize { get; set; }
            public double TimePlay { get; set; }
            public double Time { get; set; }
        }


        #endregion

        void OnNewSave(string filename)
        {
            if (config.seting.wipeclear)
            {
                PlayerInfo NewUser = new PlayerInfo()
                {
                    prize = false,
                    playername = "",   
                };
                PrintWarning("Обнаружен WIPE . Дата игроков сброшена");
            }
        }

        void TimerActivate(BasePlayer player)
        {
            ConnectedPlayers[player.userID].TimePlay = Math.Max(ConnectedPlayers[player.userID].TimePlay - (CurrentTime() - ConnectedPlayers[player.userID].Time), 0);
            ConnectedPlayers[player.userID].Time = CurrentTime();
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if(ConnectedPlayers[player.userID].prize == false)
            {
                TimerActivate(player);
            }
        }

        private void OnServerInitialized()
        {
            #region Load Config / Lang
            LoadConfigVars();
            #endregion

            if (Interface.Oxide.DataFileSystem.ExistsDatafile("ConnectedPlayers/PlayerInfoConnect"))
                ConnectedPlayers = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerInfo>>("ConnectedPlayers/PlayerInfoConnect");

            BasePlayer.activePlayerList.ForEach(OnPlayerInit);

            #region info   
            PrintError($"-----------------------------------");
            PrintError($"            ConnectPlayer          ");
            PrintError($"           Author =  DezLife       ");
            PrintError($"          Version = {Version}      ");
            PrintError($"-----------------------------------");

            if (config.seting.storeid == "storeid" || config.seting.storekey == "storekey" && config.seting.GameStore == true)
            {
                PrintWarning("Вы не настроили магазин ошибка {DarkPluginsID}");
            }


            #endregion
        }

        void OnServerSave()
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("ConnectedPlayers/PlayerInfoConnect", ConnectedPlayers);
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (ConnectedPlayers[player.userID].prize == false)
                {
                    TimerActivate(player);
                }                  
            }    
        }


        void OnPlayerInit(BasePlayer player)
        {
            if (!ConnectedPlayers.ContainsKey(player.userID))
            {
                PlayerInfo NewUser = new PlayerInfo()
                {
                    playername = "",
                    prize = false,
                    Time = TimeFreeze,
                    TimePlay = config.seting.timeplay,
                };
                ConnectedPlayers.Add(player.userID, NewUser);

                Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("ConnectedPlayers/PlayerInfoConnect", ConnectedPlayers);
            }

            if (ConnectedPlayers[player.userID].prize == false)
            {
                TimerActivate(player);
            }

            NickName(player);
        }

        void NickName (BasePlayer player) 
        {
            string namecheck;

            if (ContainsAny(player.displayName, config.CONF_BlockedParts, out namecheck) && ConnectedPlayers[player.userID].prize == false)
            {
                if (ConnectedPlayers[player.userID].TimePlay == 0)
                {
                    if (config.seting.GameStore == true)
                    {
                        PrizeGive(player.UserIDString);
                    }

                    if (config.seting.commands == true)
                    {

                        ConnectedPlayers[player.userID].prize = true;
                        Server.Command(config.seting.commandsgo.Replace("%STEAMID%", player.userID.ToString()));
                        LogToFile("ConnectPlayer", $" [{player.userID}] получил {config.seting.commandsgo}", this);
                        ReplyWithHelper(player, $"Вы получили награду в виде {config.seting.commandprize}. Награда за приставку в нике");

                    }
                }

                PrintError(namecheck);
            }
        }

        public void PrizeGive(string id)
        {     
            BasePlayer player = BasePlayer.FindByID(ulong.Parse(id));
            PlayerInfo PlayerInfo = ConnectedPlayers[player.userID];
            string url = $"http://panel.gamestores.ru/api?shop_id={config.seting.storeid}&secret={config.seting.storekey}&action=moneys&type=plus&steam_id={id}&amount={config.seting.GameStorePrize}&mess=Спасибо за поддержку";
                webrequest.Enqueue(url, null, (i, s) =>
                {
                    if (i != 200)
                    {

                    }
                    if (s.Contains("success"))
                    {                       
                            ConnectedPlayers[ulong.Parse(id)].prize = true;
                            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("ConnectedPlayers/PlayerInfoConnect", ConnectedPlayers);
                            LogToFile("ConnectPlayer", $" [{id}] получил {config.seting.GameStorePrize} рублей", this);
                             ReplyWithHelper(player, $"Вы получили награду в виде {config.seting.GameStorePrize} рублей. Награда за приставку в нике");
                    }
                    else
                    {
                        ReplyWithHelper(player, "Вы не получили приз за приставку в ники т.к не авторизованы в магазине.\n Авторизуйтесь в магазине и перезайдите на сервер!");
                    }

                }, this);
        }


        #region helpers

        #region ReplyMSG
        public void ReplyWithHelper(BasePlayer player, string message, string[] args = null)
        {
            if (args != null)
                message = string.Format(message, args);
            player.SendConsoleCommand("chat.add", new object[2]
            {
                config.setingsprefix.steamids,
                string.Format("<size=16><color={2}>{0}</color>:</size>\n{1}", config.setingsprefix.prefix, message, config.setingsprefix.prefixcolor)
            });
        }
        #endregion

        private bool ContainsAny(string input, List<string> check, out string result)
        {
            result = "";

            foreach (var block in check)
            {
                if (input.Contains(block))
                {
                    result = block;
                    return true;
                }
            }
            return false;
        }

        public double TimeFreeze = CurrentTime();

        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        #endregion
    }
}
