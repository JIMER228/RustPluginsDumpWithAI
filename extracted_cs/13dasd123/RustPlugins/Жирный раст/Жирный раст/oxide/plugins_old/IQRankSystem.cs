using Newtonsoft.Json;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using System.Linq;
using System;
using ConVar;
using System.Text;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("IQRankSystem", "Mercury", "0.0.1")]
    [Description("Ваши ранги для сенрвера")]
    class IQRankSystem : RustPlugin
    {
        #region Reference
        [PluginReference] Plugin IQChat, IQCases, IQHeadReward, IQEconomic;

        #region IQChat
        public void SendChat(BasePlayer player, string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            var Chat = config.Setting.ReferenceSetting.IQChatSetting;
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, Chat.CustomPrefix, Chat.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion

        #region IQCases
        void OpenCase(BasePlayer player, string DisplayNameCase)
        {
            if (!DataInformation.ContainsKey(player.userID)) return;
            var Data = DataInformation[player.userID];
            Data.InformationUser.IQCasesOpenCase++;
        }
        #endregion

        #region IQHeadReward
        void KillHead(BasePlayer player)
        {
            if (!DataInformation.ContainsKey(player.userID)) return;
            var Data = DataInformation[player.userID];
            Data.InformationUser.IQHeadRewardKillAmount++;
        }
        #endregion

        #region IQEconomic
        void SET_BALANCE_USER(ulong userID, int SetBalance)
        {
            if (!DataInformation.ContainsKey(userID)) return;
            var Data = DataInformation[userID];
            Data.InformationUser.IQEconomicAmountBalance += SetBalance;
        }
        #endregion

        #endregion

        #region Vars
        public enum Obtaining
        {
            Gather,
            Time,
            GatherAndTime,
            IQCases,
            IQHeadReward,
            IQEconomic,
        }
        StringBuilder sb = new StringBuilder();
        private string GetLang(string LangKey, string userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }

        void RegisteredPermissions()
        {
            foreach(var Perm in config.RankList.Where(p => !String.IsNullOrWhiteSpace(p.Value.PermissionRank)))
                if (!permission.PermissionExists(Perm.Value.PermissionRank, this))
                    permission.RegisterPermission(Perm.Value.PermissionRank, this);
        }
        #endregion

        #region Configuration
        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Список рангов и их настройка")]
            public Dictionary<string, RankSettings> RankList = new Dictionary<string, RankSettings>();
            [JsonProperty("Настройки плагина")]
            public Settings Setting = new Settings();

            internal class RankSettings
            {
                [JsonProperty("Название ранга")]
                public string DisplayNameRank;
                [JsonProperty("Права с которыми доступен данный ранг")]
                public string PermissionRank;
                [JsonProperty("Настройки получения ранга")]
                public Obtainings Obtaining = new Obtainings();
                internal class Obtainings
                {
                    [JsonProperty("Выберите за что возможно получить доступ к данному рангу" +
                                  "(0 - Добыча, " +
                                  "1 - Время игры на сервере, " +
                                  "2 - Вермя игры на сервере и добыча вместе," +
                                  "3 - IQCases , открыть N количество кейсов," +
                                  "4 - IQHeadReward , убить N количество игроков в розыске" +
                                  "5 - IQEconomic, собрать за все время N количество валюты")]
                    public Obtaining ObtainingType;
                    [JsonProperty("Настройки получения ранга за время")]
                    public ObtainingsTime ObtainingsTimes = new ObtainingsTime();
                    [JsonProperty("Настройки получения ранга за добычу")]
                    public ObtainingsGather ObtainingsGathers = new ObtainingsGather();
                    [JsonProperty("Настройки получения ранга за открытие кейсов IQCases")]
                    public ObtainingsIQCases ObtainingsIQCase = new ObtainingsIQCases();
                    [JsonProperty("Настройки получения ранга за убийство разыскиваемых IQHeadReward")]
                    public ObtainingsIQHeadReward ObtainingsIQHeadRewards = new ObtainingsIQHeadReward();
                    [JsonProperty("Настройки получения ранга за собранную валюту IQEconomic")]
                    public ObtainingsIQEconomic ObtainingsIQEconomics = new ObtainingsIQEconomic();
                    internal class ObtainingsIQEconomic
                    {
                        [JsonProperty("Сколько собрать валюты для получения этого ранга")]
                        public int IQEconomicBalance;
                    }
                    internal class ObtainingsIQCases
                    {
                        [JsonProperty("Сколько кейсов открыть для получения этого ранга")]
                        public int OpenCaseAmount;
                    }
                    internal class ObtainingsIQHeadReward
                    {
                        [JsonProperty("Сколько нужно убить разыскиваемых для получения этого ранга")]
                        public int KillHeadAmount;
                    }
                    internal class ObtainingsTime
                    {
                        [JsonProperty("Время, которое нужно отыграть для получения этого ранга")]
                        public int TimeGame;
                    }
                    internal class ObtainingsGather
                    {
                        [JsonProperty("Сколько нужно добыть всего ресурсов для ранга")]
                        public int Amount;
                    }
                }
            }

            internal class Settings
            {
                [JsonProperty("Настройки плагинов совместимости")]
                public ReferenceSettings ReferenceSetting = new ReferenceSettings();
                [JsonProperty("Общие настройки")]
                public GeneralSettings GeneralSetting = new GeneralSettings();
                internal class GeneralSettings
                {
                    [JsonProperty("Отображать время игры на сервере перед рангом")]
                    public bool ShowTimeGame;
                    [JsonProperty("При получении нового ранга сразу устанавливать его(true - да/false - нет)")]
                    public bool RankSetupNew;
                }
                internal class ReferenceSettings
                {
                    [JsonProperty("Настройки IQChat")]
                    public IQChatSettings IQChatSetting = new IQChatSettings();
                    internal class IQChatSettings
                    {
                        [JsonProperty("IQChat : Кастомный префикс в чате")]
                        public string CustomPrefix;
                        [JsonProperty("IQChat : Кастомный аватар в чате(Если требуется)")]
                        public string CustomAvatar;
                    }
                }
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    #region General Settings
                    Setting = new Settings
                    {
                        GeneralSetting = new Settings.GeneralSettings
                        {
                            ShowTimeGame = false,
                            RankSetupNew = true,
                        },
                        ReferenceSetting = new Settings.ReferenceSettings
                        {
                            IQChatSetting = new Settings.ReferenceSettings.IQChatSettings
                            {
                                CustomAvatar = "",
                                CustomPrefix = "",
                            },
                        },
                    },
                    #endregion

                    #region Rank List
                    RankList = new Dictionary<string, RankSettings>
                    {
                        ["newmember"] = new RankSettings
                        {
                            DisplayNameRank = "Новобранец",
                            PermissionRank = "",
                            Obtaining = new RankSettings.Obtainings
                            {
                                ObtainingType = Obtaining.Time,
                                ObtainingsTimes = new RankSettings.Obtainings.ObtainingsTime
                                {
                                    TimeGame = 0,
                                },
                            },
                        },
                        ["member"] = new RankSettings
                        {
                            DisplayNameRank = "Местный",
                            PermissionRank = "",
                            Obtaining = new RankSettings.Obtainings
                            {
                                ObtainingType = Obtaining.Time,
                                ObtainingsTimes = new RankSettings.Obtainings.ObtainingsTime
                                {
                                    TimeGame = 300,
                                },
                            },
                        },
                        ["experienced"] = new RankSettings
                        {
                            DisplayNameRank = "Бывалый",
                            PermissionRank = "",
                            Obtaining = new RankSettings.Obtainings
                            {
                                ObtainingType = Obtaining.Time,
                                ObtainingsTimes = new RankSettings.Obtainings.ObtainingsTime
                                {
                                    TimeGame = 600,
                                },
                            },
                        },
                        ["farmer"] = new RankSettings
                        {
                            DisplayNameRank = "Фармила",
                            PermissionRank = "",
                            Obtaining = new RankSettings.Obtainings
                            {
                                ObtainingType = Obtaining.Gather,
                                ObtainingsGathers = new RankSettings.Obtainings.ObtainingsGather
                                {
                                   Amount = 5000
                                }
                            },
                        },
                        ["adminFriend"] = new RankSettings
                        {
                            DisplayNameRank = "Кент Админа",
                            PermissionRank = "iqranksystem.vip",
                            Obtaining = new RankSettings.Obtainings
                            {
                                ObtainingType = Obtaining.Time,
                                ObtainingsTimes = new RankSettings.Obtainings.ObtainingsTime
                                {
                                    TimeGame = 600,
                                }
                            },
                        },
                        ["gamer"] = new RankSettings
                        {
                            DisplayNameRank = "Задрот",
                            PermissionRank = "",
                            Obtaining = new RankSettings.Obtainings
                            {
                                ObtainingType = Obtaining.GatherAndTime,
                                ObtainingsTimes = new RankSettings.Obtainings.ObtainingsTime
                                {
                                    TimeGame = 500,
                                },
                                ObtainingsGathers = new RankSettings.Obtainings.ObtainingsGather
                                {
                                    Amount = 5000,
                                }
                            },
                        },
                        ["azart"] = new RankSettings
                        {
                            DisplayNameRank = "Азартный игрок",
                            PermissionRank = "",
                            Obtaining = new RankSettings.Obtainings
                            {
                                ObtainingType = Obtaining.IQCases,
                                ObtainingsIQCase = new RankSettings.Obtainings.ObtainingsIQCases
                                {
                                    OpenCaseAmount = 3
                                }
                            },
                        },
                        ["killer"] = new RankSettings
                        {
                            DisplayNameRank = "Шериф",
                            PermissionRank = "",
                            Obtaining = new RankSettings.Obtainings
                            {
                                ObtainingType = Obtaining.IQHeadReward,
                                ObtainingsIQHeadRewards = new RankSettings.Obtainings.ObtainingsIQHeadReward
                                {
                                    KillHeadAmount = 5,
                                }
                            },
                        },
                        ["IqEconomicMillioner"] = new RankSettings
                        {
                            DisplayNameRank = "Скрудж Макдак",
                            PermissionRank = "",
                            Obtaining = new RankSettings.Obtainings
                            {
                                ObtainingType = Obtaining.IQEconomic,
                                ObtainingsIQEconomics = new RankSettings.Obtainings.ObtainingsIQEconomic
                                {
                                    IQEconomicBalance = 350,
                                }
                            },
                        },
                    }
                    #endregion
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения #355 конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Data
        [JsonProperty("Информация о пользователях")] 
        public Dictionary<ulong, DataClass> DataInformation = new Dictionary<ulong, DataClass>();
        public class DataClass
        {
            [JsonProperty("Активный ранг")]
            public string RankActive;
            [JsonProperty("Доступные ранги")]
            public List<string> RankAccessList = new List<string>();
            [JsonProperty("Информация о прогрессе игроков")]
            public Infromation InformationUser = new Infromation();
            internal class Infromation
            {
                [JsonProperty("Время на сервере")]
                public int TimeGame;
                [JsonProperty("Добыто всего")]
                public int GatherAll;
                [JsonProperty("IQCases : Открыто кейсов")]
                public int IQCasesOpenCase;
                [JsonProperty("IQHeadReward : Убито разыскиваемых")]
                public int IQHeadRewardKillAmount;
                [JsonProperty("IQEconomic : Собрано валюты")]
                public int IQEconomicAmountBalance;
            }
        }
        void ReadData() => DataInformation = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, DataClass>>("IQRankSystem/DataPlayers");
        void WriteData() => Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQRankSystem/DataPlayers", DataInformation);
        void RegisteredDataUser(ulong userID)
        {
            if (!DataInformation.ContainsKey(userID))
                 DataInformation.Add(userID, new DataClass { RankActive = "", RankAccessList = new List<string> { }, InformationUser = new DataClass.Infromation { GatherAll = 0, TimeGame = 0, IQCasesOpenCase = 0, IQHeadRewardKillAmount = 0, IQEconomicAmountBalance = 0 } });
        }
        #endregion

        #region Hooks
        private void Init() => Unsubscribe(nameof(OnPlayerChat));
        private void OnServerInitialized()
        {
            if (!IQChat)
                Subscribe(nameof(OnPlayerChat));
            RegisteredPermissions();
            ReadData();

            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);

            WriteData();
            TrackerTime();
        }
        void OnPlayerConnected(BasePlayer player) => RegisteredDataUser(player.userID);
        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity as BasePlayer;
            if (player == null) return null;
            if (item == null) return null;
            if (entity == null) return null;
            if (!DataInformation.ContainsKey(player.userID)) return null;
            var Data = DataInformation[player.userID].InformationUser;
            Data.GatherAll += item.amount;
            return null;
        }
        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (player == null) return;
            if (item == null) return;
            if (!DataInformation.ContainsKey(player.userID)) return;
            var Data = DataInformation[player.userID].InformationUser;
            Data.GatherAll += item.amount;
        }
        private void Unload() => WriteData();
        #endregion

        #region Metods

        #region ChatMetods
        private bool OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            if (Interface.Oxide.CallHook("CanChatMessage", player, message) != null) return false;
            SeparatorChat(channel, player, message);
            return false;
        }
        private void SeparatorChat(Chat.ChatChannel channel, BasePlayer player, string Message)
        {
            if (IQChat) return;
            var Data = DataInformation[player.userID];
            string Rank = config.RankList[Data.RankActive].DisplayNameRank;
            string Time = API_GET_TIME_GAME(player.userID);

            string ModifiedChannel = channel == Chat.ChatChannel.Team ? "<color=#a5e664>[Team]</color>" : "";
            string MessageSeparator = !String.IsNullOrWhiteSpace(Rank) && !String.IsNullOrWhiteSpace(Time) ? $"{ModifiedChannel} [{Rank}]{player.displayName}[{Time}]: {Message}" : !String.IsNullOrWhiteSpace(Time) ? $"{ModifiedChannel}{player.displayName}[{Time}]: {Message}" : !String.IsNullOrWhiteSpace(Rank) ? $"{ModifiedChannel} [{Rank}]{player.displayName}: {Message}" : $"{ModifiedChannel}{player.displayName}: {Message}";

            if (channel == Chat.ChatChannel.Global)
                foreach (BasePlayer p in BasePlayer.activePlayerList)
                    p.SendConsoleCommand("chat.add", new object[] { (int)channel, player.userID, MessageSeparator });
            if (channel == Chat.ChatChannel.Team)
            {
                RelationshipManager.PlayerTeam Team = RelationshipManager.Instance.FindTeam(player.currentTeam);
                if (Team == null) return;
                foreach (var FindPlayers in Team.members)
                {
                    BasePlayer TeamPlayer = BasePlayer.FindByID(FindPlayers);
                    if (TeamPlayer == null) continue;

                    TeamPlayer.SendConsoleCommand("chat.add", channel, player.userID, MessageSeparator);
                }
            }
        }
        #endregion

        public void RankAccess(BasePlayer player)
        {
            var Ranks = config.RankList;
            if (Ranks == null) return;
            var Data = DataInformation[player.userID];
            if (Data == null) return;

            foreach(var Rank in Ranks.Where(r => !Data.RankAccessList.Contains(r.Key) && (String.IsNullOrEmpty(Ranks[r.Key].PermissionRank) || permission.UserHasPermission(player.UserIDString, Ranks[r.Key].PermissionRank))))
            {
                var ObtainingSetup = Rank.Value.Obtaining;
                switch (ObtainingSetup.ObtainingType)
                {
                    case Obtaining.Time:
                        {
                            if (ObtainingSetup.ObtainingsTimes.TimeGame <= Data.InformationUser.TimeGame)
                                SetupRank(player, Rank.Key);
                            break;
                        };
                    case Obtaining.Gather:
                        {
                            if (ObtainingSetup.ObtainingsGathers.Amount <= Data.InformationUser.GatherAll)
                                SetupRank(player, Rank.Key);
                            break;
                        };
                    case Obtaining.GatherAndTime:
                        {
                            if (ObtainingSetup.ObtainingsGathers.Amount <= Data.InformationUser.GatherAll && ObtainingSetup.ObtainingsTimes.TimeGame <= Data.InformationUser.TimeGame)
                                SetupRank(player, Rank.Key);                     
                            break;
                        }
                    case Obtaining.IQCases:
                        {
                            if (!IQCases) return;
                            if(ObtainingSetup.ObtainingsIQCase.OpenCaseAmount <= Data.InformationUser.IQCasesOpenCase)
                                SetupRank(player, Rank.Key);
                            break;
                        }
                    case Obtaining.IQHeadReward:
                        {
                            if (!IQHeadReward) return;
                            if (ObtainingSetup.ObtainingsIQHeadRewards.KillHeadAmount <= Data.InformationUser.IQHeadRewardKillAmount)
                                SetupRank(player, Rank.Key);
                            break;
                        }
                    case Obtaining.IQEconomic:
                        {
                            if (!IQEconomic) return;
                            if (ObtainingSetup.ObtainingsIQEconomics.IQEconomicBalance <= Data.InformationUser.IQEconomicAmountBalance)
                                SetupRank(player, Rank.Key);
                            break;
                        }
                }
            }             
        }
        void SetupRank(BasePlayer player, string RankKey)
        {
            var Data = DataInformation[player.userID];
            string NameRank = config.RankList[RankKey].DisplayNameRank;
            var GeneralSetting = config.Setting.GeneralSetting;

            Data.RankAccessList.Add(RankKey);
            SendChat(player, GetLang("RANK_NEW", player.UserIDString, NameRank));
            if (GeneralSetting.RankSetupNew)
                Data.RankActive = RankKey;
        }
        public void TrackerTime()
        {
            timer.Every(60f, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (!DataInformation.ContainsKey(player.userID))
                        RegisteredDataUser(player.userID);
                    else DataInformation[player.userID].InformationUser.TimeGame += 60;
                    RankAccess(player);
                }
            });
        }

        void RankSetUp(BasePlayer player, string RankName)
        {
            var Data = DataInformation[player.userID];
            string RankKey = config.RankList.FirstOrDefault(r => r.Value.DisplayNameRank.Contains(RankName)).Key;
            if (String.IsNullOrWhiteSpace(RankName) || String.IsNullOrWhiteSpace(RankKey) || !Data.RankAccessList.Contains(RankKey))
            {
                SendChat(player, GetLang("COMMAND_RANK_LIST_NO_ANY", player.UserIDString));
                return;
            }
            Data.RankActive = RankKey;
            SendChat(player, GetLang("COMMAND_RANK_LIST_TO_ACTIVE", player.UserIDString, config.RankList[RankKey].DisplayNameRank));
        }

        #region HelpMetods
        public static string FormatTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result = $"{Format(time.Days, "д", "д", "д")}";

            if (time.Hours != 0 && time.Days == 0)
                result = $"{Format(time.Hours, "ч", "ч", "ч")}";

            if (time.Minutes != 0 && time.Hours == 0 && time.Days == 0)
                result = $"{Format(time.Minutes, "м", "м", "м")}";

            if (time.Seconds != 0 && time.Days == 0 && time.Minutes == 0 && time.Hours == 0)
                result = $"{Format(time.Seconds, "с", "с", "с")}";

            return result;
        }
        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units}{form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units}{form2}";

            return $"{units}{form3}";
        }
        #endregion

        #endregion

        #region Commands
        [ChatCommand("rank")]
        void ChatRankCommand(BasePlayer player, string cmd, string[] arg)
        {
            if (arg.Length == 0 || arg == null)
            {
                SendChat(player, GetLang("COMMAND_RANK_NO_ARG", player.UserIDString));
                PrintError(FormatTime(TimeSpan.FromSeconds(DataInformation[player.userID].InformationUser.TimeGame)));
                return;
            }
            string Action = arg[0];
            switch(Action)
            {
                case "list":
                    {
                        if (!DataInformation.ContainsKey(player.userID) || DataInformation[player.userID].RankAccessList.Count == 0)
                        {
                            SendChat(player, GetLang("COMMAND_RANK_LIST_NO_ACCES", player.UserIDString));
                            return;
                        }
                        StringBuilder RankListString = new StringBuilder();
                        var RankList = DataInformation[player.userID].RankAccessList;
                        foreach(string RankMe in RankList.Where(r => config.RankList.ContainsKey(r)))
                        {
                            string NameRankInConfig = config.RankList[RankMe].DisplayNameRank;
                            RankListString.Append($"\n- {NameRankInConfig}");
                        }
                        SendChat(player, GetLang("COMMAND_RANK_LIST", player.UserIDString, RankListString));
                        break;
                    }
                case "add":
                case "take":
                case "set":
                case "setup":
                    {
                        RankSetUp(player, arg[1]);
                        break;
                    }
                case "remove":
                case "clear":
                case "stop":
                case "revoke":
                    {
                        if(!DataInformation.ContainsKey(player.userID) || String.IsNullOrWhiteSpace(DataInformation[player.userID].RankActive))
                        {
                            SendChat(player, GetLang("COMMAND_RANK_LIST_NO_CLEAR", player.UserIDString));
                            return;
                        }
                        DataInformation[player.userID].RankActive = string.Empty;
                        SendChat(player, GetLang("COMMAND_RANK_LIST_CLEAR", player.UserIDString));
                        break;
                    }
            }
        }
        #endregion

        #region Lang    
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["RANK_NEW"] = "Поздравляем, вы получили ранг {0}",

                ["COMMAND_RANK_NO_ARG"] = "\n<size=15>Используйте синтаксис</size>\n- rank list - список доступных рангов" +
                                          "\n- rank add НазваниеРанга - устанавливает нужнный вам ранг" +
                                          "\n- rank remove - очищает ваш активный ранг",
                ["COMMAND_RANK_LIST"] = "\n<size=15>Список доступных рангов:</size>{0}",
                ["COMMAND_RANK_LIST_NO_ACCES"] = "У вас нет доступных рангов",
                ["COMMAND_RANK_LIST_NO_ANY"] = "Вы ввели неправильно название ранга , либо такого ранга у вас нет",
                ["COMMAND_RANK_LIST_TO_ACTIVE"] = "Вы успешно установили себе ранг: {0}",
                ["COMMAND_RANK_LIST_NO_CLEAR"] = "У вас нет установленного ранга",
                ["COMMAND_RANK_LIST_CLEAR"] = "Вы успешно очистили ранг",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["RANK_NEW"] = "Поздравляем, вы получили ранг {0}",

                ["COMMAND_RANK_NO_ARG"] = "\n<size=15>Используйте синтаксис</size>\n- rank list - список доступных рангов" +
                                          "\n- rank add НазваниеРанга - устанавливает нужнный вам ранг" +
                                          "\n- rank remove - очищает ваш активный ранг",
                ["COMMAND_RANK_LIST"] = "\n<size=15>Список доступных рангов:</size>{0}",
                ["COMMAND_RANK_LIST_NO_ACCES"] = "У вас нет доступных рангов",
                ["COMMAND_RANK_LIST_NO_ANY"] = "Вы ввели неправильно название ранга , либо такого ранга у вас нет",
                ["COMMAND_RANK_LIST_TO_ACTIVE"] = "Вы успешно установили себе ранг: {0}",
                ["COMMAND_RANK_LIST_NO_CLEAR"] = "У вас нет установленного ранга",
                ["COMMAND_RANK_LIST_CLEAR"] = "Вы успешно очистили ранг",

            }, this, "ru");
            PrintWarning("Языковой файл загружен успешно");
        }
        #endregion

        #region API

        string API_GET_RANK_NAME(ulong userID)
        {
            string Rank = DataInformation[userID].RankActive;
            if (!config.RankList.ContainsKey(Rank)) return null;
            string RankDisplayName = config.RankList[Rank].DisplayNameRank;
            return RankDisplayName;
        }
        string API_GET_RANK_NAME(string Key)
        {
            if (!config.RankList.ContainsKey(Key)) return null;
            string Rank = config.RankList[Key].DisplayNameRank;
            return Rank;
        }
        bool API_IS_RANK_REALITY(string Key)
        {
            if (!config.RankList.ContainsKey(Key)) return false;
            else return true;
        }
        string API_GET_RANK_PERM(string Key)
        {
            if (!config.RankList.ContainsKey(Key)) return null;
            string RankPermission = config.RankList[Key].PermissionRank;
            return RankPermission;
        }
        bool API_GET_RANK_ACCESS(ulong userID, string Key)
        {
            if (!config.RankList.ContainsKey(Key)) return false;
            string RankPermission = config.RankList[Key].PermissionRank;
            if (String.IsNullOrWhiteSpace(RankPermission)) return true;
            return permission.UserHasPermission(userID.ToString(), RankPermission);
        }
        bool API_GET_AVAILABILITY_RANK_USER(ulong userID, string Key)
        {
            if (!config.RankList.ContainsKey(Key)) return false;
            if (!DataInformation.ContainsKey(userID)) return false;
            var RankAccesList = DataInformation[userID].RankAccessList;
            return RankAccesList.Contains(Key);
        }
        string API_GET_TIME_GAME(ulong userID)
        {
            if (!config.Setting.GeneralSetting.ShowTimeGame) return "";
            string TimeGame = FormatTime(TimeSpan.FromSeconds(DataInformation[userID].InformationUser.TimeGame));
            return TimeGame;
        }
        int API_GET_SECONDGAME(ulong userID)
        {
            string Rank = DataInformation[userID].RankActive;
            if(!config.RankList.ContainsKey(Rank)) return 0;
            int SecondGame = DataInformation[userID].InformationUser.TimeGame;
            return SecondGame;
        }
        List<string> API_RANK_USER_KEYS(ulong userID)
        {
            if (!DataInformation.ContainsKey(userID)) return null;
            List<string> RankList = DataInformation[userID].RankAccessList;
            return RankList;
        }
        void API_SET_ACTIVE_RANK(ulong userID, string RankKey)
        {
            if (!DataInformation.ContainsKey(userID)) return;
            if (String.IsNullOrWhiteSpace(RankKey)) return;
            if (!config.RankList.ContainsKey(RankKey)) return;
            var Data = DataInformation[userID];
            Data.RankActive = RankKey;
            BasePlayer player = BasePlayer.FindByID(userID);
            if (player != null)
                SendChat(player, GetLang("COMMAND_RANK_LIST_TO_ACTIVE", player.UserIDString, config.RankList[RankKey].DisplayNameRank));
        }
        #endregion
    }
}
