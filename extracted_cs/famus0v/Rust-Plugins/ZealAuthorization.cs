using System;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Globalization;
using ConVar;
using Facepunch.Models;
using Newtonsoft.Json;
using Global = Rust.Global;

namespace Oxide.Plugins
{
    [Info("ZealAuthorization", "Kira", "1.0.0")]
    [Description("Система авторизации")]
    public class ZealAuthorization : RustPlugin
    {
        #region [Dictionary/Vars] / [Словари/Переменные]

        private static string Sharp = "assets/content/ui/ui.background.tile.psd";
        private static string Blur = "assets/content/ui/uibackgroundblur.mat";
        private static string radial = "assets/content/ui/ui.background.transparent.radial.psd";
        private static string regular = "robotocondensed-regular.ttf";

        private static string Layer = "BoxAuthorization";
        public static ZealAuthorization _;
        public Coroutine Proccess;
        private static StoredData DataBase = new StoredData();

        #endregion

        #region [Classes] / [Классы]

        private class Authorization : MonoBehaviour
        {
            public BasePlayer player;
            public PlayerInfo info;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                info = DataBase.DBAuthorization[player.userID];
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, true);
                InvokeRepeating("Block", 1f, 1f);
                if (DataBase.DBAuthorization.ContainsKey(player.userID)) DrawUI_Login();
                else DrawUI_Registration();
            }

            public void DrawUI_Registration()
            {
                CuiElementContainer Gui = new CuiElementContainer();
                CuiHelper.DestroyUi(player, Layer);
                Gui.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    Image =
                    {
                        Color = HexToRustFormat("#000000F9"),
                        Material = Blur,
                        Sprite = radial
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, "Overlay", Layer);

                Gui.Add(new CuiElement
                {
                    Name = "Zagolovok",
                    Parent = Layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 35,
                            Text = $"{ConVar.Server.hostname}\n" +
                                   "<color=#DEDEDE><size=20><b>ДЛЯ ПРОДОЛЖЕНИЯ ИГРЫ, ПОЖАЛУЙСТА ЗАРЕГИСТРИРУЙТЕСЬ</b></size></color>",
                            Font = regular
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.8444445",
                            AnchorMax = "1 1"
                        }
                    }
                });

                Gui.Add(new CuiElement
                {
                    Name = "DescriptionAuth",
                    Parent = Layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 20,
                            Text = "<b>ПРИДУМАЙТЕ НАДЕЖНЫЙ ПАРОЛЬ</b>\n" +
                                   "<color=#DEDEDE><size=15>Учтите, изменить пароль без помощи администратора невозможно</size></color>",
                            Font = regular
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.328125 0.5213059",
                            AnchorMax = "0.6776041 0.6175926"
                        }
                    }
                });

                Gui.Add(new CuiElement
                {
                    Name = "BGInputField",
                    Parent = Layer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#0000005C"),
                            Material = Blur
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3697916 0.4712972",
                            AnchorMax = "0.6302083 0.5157419"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#0000008A"),
                            Distance = "0.5 0.5"
                        }
                    }
                });

                Gui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = HexToRustFormat("#56945DFF"),
                        Material = Blur,
                        Command = ""
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#46734BFF"),
                        FontSize = 27,
                        Text = ">"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.9280002 0",
                        AnchorMax = "0.997 0.96"
                    }
                }, "BGInputField", "ButtonAuth");

                Gui.Add(new CuiElement
                {
                    Name = "InputField",
                    Parent = "BGInputField",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            Font = regular,
                            FontSize = 20,
                            CharsLimit = 30,
                            Command = "registration.pass "
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.01 0",
                            AnchorMax = "0.924 1"
                        }
                    }
                });

                CuiHelper.AddUi(player, Gui);
            }

            public void DrawUI_Login()
            {
                CuiElementContainer Gui = new CuiElementContainer();
                CuiHelper.DestroyUi(player, Layer);
                Gui.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    Image =
                    {
                        Color = HexToRustFormat("#000000F9"),
                        Material = Blur,
                        Sprite = radial
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, "Overlay", Layer);

                Gui.Add(new CuiElement
                {
                    Name = "Zagolovok",
                    Parent = Layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 35,
                            Text = $"{ConVar.Server.hostname}\n" +
                                   "<color=#DEDEDE><size=20><b>ДЛЯ ПРОДОЛЖЕНИЯ ИГРЫ, ПОЖАЛУЙСТА АВТОРИЗУЙТЕСЬ</b></size></color>",
                            Font = regular
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.8444445",
                            AnchorMax = "1 1"
                        }
                    }
                });

                Gui.Add(new CuiElement
                {
                    Name = "DescriptionAuth",
                    Parent = Layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 20,
                            Text = "<b>ВВЕДИТЕ СВОЙ ПАРОЛЬ</b>\n" +
                                   "<color=#DEDEDE><size=15>Если забыли пароль, обратитесь к администратору</size></color>",
                            Font = regular
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.328125 0.5213059",
                            AnchorMax = "0.6776041 0.6175926"
                        }
                    }
                });

                Gui.Add(new CuiElement
                {
                    Name = "BGInputField",
                    Parent = Layer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#0000005C"),
                            Material = Blur
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3697916 0.4712972",
                            AnchorMax = "0.6302083 0.5157419"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#0000008A"),
                            Distance = "0.5 0.5"
                        }
                    }
                });

                Gui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = HexToRustFormat("#56945DFF"),
                        Material = Blur,
                        Command = ""
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#46734BFF"),
                        FontSize = 27,
                        Text = ">"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.9280002 0",
                        AnchorMax = "0.997 0.96"
                    }
                }, "BGInputField", "ButtonAuth");

                Gui.Add(new CuiElement
                {
                    Name = "InputField",
                    Parent = "BGInputField",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            Font = regular,
                            FontSize = 20,
                            CharsLimit = 30,
                            Command = "login.pass "
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.01 0",
                            AnchorMax = "0.924 1"
                        }
                    }
                });

                CuiHelper.AddUi(player, Gui);
            }

            void Block()
            {
                player.Teleport(info.Position);
                _.Puts("1");
            }

            public void OnDestroy()
            {
                CuiHelper.DestroyUi(player, Layer);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, false);
                Destroy(this);
            }
        }

        #endregion

        #region [Configuraton] / [Конфигурация]

        public static ConfigData config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "ZealAuthorization")]
            public ZealAuth ZealAuthorization = new ZealAuth();

            public class ZealAuth
            {
                [JsonProperty(PropertyName = "Минимальное кол-во символов для пароля")]
                public int MinSymbolCount;
            }
        }

        public ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                ZealAuthorization = new ConfigData.ZealAuth
                {
                    MinSymbolCount = 6
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Файл конфигурации поврежден (или не существует), создан новый!");
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region [DrawUI] / [Показ UI]

        void DrawMessage(BasePlayer player, string message, float time)
        {
            CuiElementContainer Gui = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "BoxMessage");
            Gui.Add(new CuiElement
            {
                Name = "BoxMessage",
                Parent = "Overlay",
                FadeOut = 0.8f,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#0000005C"),
                        Material = Blur,
                        FadeIn = 1f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3494791 0.7925926",
                        AnchorMax = "0.6479167 0.9027777"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#0000008A"),
                        Distance = "0.5 1"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "TEXT",
                Parent = "BoxMessage",
                FadeOut = 0.3f,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 20,
                        Text = message,
                        Font = regular,
                        FadeIn = 1f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            CuiHelper.AddUi(player, Gui);
            timer.Once(time, () =>
            {
                CuiHelper.DestroyUi(player, "BoxMessage");
                CuiHelper.DestroyUi(player, "TEXT");
            });
        }

        #endregion

        #region [ChatCommand] / [Чат команды]

        [ConsoleCommand("registration.pass")]
        private void GetRegistrationPass(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            string password = args.Args[0].Replace(" ", "");
            BasePlayer player = args.Player();
            if (password.Length < config.ZealAuthorization.MinSymbolCount || password.Contains(" "))
                DrawMessage(player,
                    $"Пароль должен содержать минимум {config.ZealAuthorization.MinSymbolCount} символов, и не должен содержать пробелы",
                    5f);
            else
            {
                CheckDataBase(player);
                DataBase.DBAuthorization[player.userID].Password = password;
                DataBase.DBAuthorization[player.userID].SteamID = player.userID;
                DataBase.DBAuthorization[player.userID].Name = player.displayName;
                DataBase.DBAuthorization[player.userID].IP = player.Connection.ipaddress.Split(':')[0];
                DrawMessage(player, "Регистрация завершена успешно, удачной игры", 5f);
                player.GetComponent<Authorization>().OnDestroy();
            }
        }

        [ConsoleCommand("login.pass")]
        private void GetLoginPass(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            BasePlayer player = args.Player();
            string password = args.Args[0];
            if (password.Length < 1) return;
            if (password == DataBase.DBAuthorization[player.userID].Password)
            {
                DrawMessage(player, "Вы удачно авторизировались, приятной игры", 5f);
                player.GetComponent<Authorization>().OnDestroy();
            }
            else
            {
                DrawMessage(player, "Пароль неверный, попробуйте снова", 5f);
            }
        }

        #endregion

        #region [Hooks] / [Крюки]

        void OnServerInitialized()
        {
            LoadData();
            _ = this;
            Proccess = Global.Runner.StartCoroutine(LoadBehaviourComponents());
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            CheckDataBase(player);
            SavePositon(player);
        }

        void OnPlayerConnected(Network.Message packet)
        {
            BasePlayer player = packet.Player();
            CheckDataBase(player);
            DataBase.DBAuthorization[player.userID].Login_History.Add(new LoginHistory
            {
                Name = player.displayName,
                DateTime = DateTime.Now.ToString("G"),
                IP = player.Connection.ipaddress.Split(':')[0]
            });

            if (DataBase.DBAuthorization.ContainsKey(player.userID) &
                (player.Connection.ipaddress.Split(':')[0] != DataBase.DBAuthorization[player.userID].IP))
            {
                PrintWarning($"Игрок подключился с стороннего IP\n" +
                             $"IPAdress : {player.Connection.ipaddress}\n" +
                             $"Name : {player.displayName}");

                DataBase.DBAuthorization[player.userID].IPHistories.Add(new IPHistory
                {
                    IP = player.Connection.ipaddress.Split(':')[0],
                    DateTime = DateTime.Now.ToString("G"),
                    Name = player.displayName
                });
            }
            else
            {
                PrintWarning($"Подключается авторизированный игрок : {player.displayName}");
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            NextTick(() => player.gameObject.AddComponent<Authorization>());
        }

        private void Unload()
        {
            Proccess = Global.Runner.StartCoroutine(DestroyBehaviourComponents());
            if (Proccess != null) Global.Runner.StopCoroutine(Proccess);
            SaveData();
        }

        #endregion

        #region [DataBase] / [Хранение данных]

        public class StoredData
        {
            public Dictionary<ulong, PlayerInfo> DBAuthorization = new Dictionary<ulong, PlayerInfo>();
        }

        public class PlayerInfo
        {
            public string Name;
            public ulong SteamID;
            public string Password;
            public string IP;
            public Vector3 Position;
            public List<LoginHistory> Login_History;
            public List<IPHistory> IPHistories;
        }

        public class IPHistory
        {
            public string Name;
            public string IP;
            public string DateTime;
        }

        public class LoginHistory
        {
            public string Name;
            public string DateTime;
            public string IP;
        }


        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, DataBase);

        private void LoadData()
        {
            try
            {
                DataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception e)
            {
                DataBase = new StoredData();
            }
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        IEnumerator LoadBehaviourComponents()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                player.gameObject.AddComponent<Authorization>();
                yield return new WaitForSeconds(0.5f);
            }

            yield return 0;
        }

        IEnumerator DestroyBehaviourComponents()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.GetComponent<Authorization>() != null)
                    player.GetComponent<Authorization>().OnDestroy();
            }

            yield return 0;
        }

        void SavePositon(BasePlayer player)
        {
            CheckDataBase(player);
            DataBase.DBAuthorization[player.userID].Position = player.GetNetworkPosition();
            SaveData();
        }

        void CheckDataBase(BasePlayer player)
        {
            if (!DataBase.DBAuthorization.ContainsKey(player.userID))
                DataBase.DBAuthorization.Add(player.userID, new PlayerInfo());
        }

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');
            if (str.Length == 6)
                str += "FF";
            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        #endregion
    }
}