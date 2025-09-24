// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("StripesEvent", "https://discord.gg/dNGbxafuJn", "0.0.8")]
    public class StripesEvent : RustPlugin
    {
        [PluginReference] private Plugin Notifications, ImageLibrary;

        public enum TypeEvent
        {
            Gather = 1,
            Kill = 3,
            Loot = 4,
            Craft = 5,
            GatherBonus = 2
        }


        public Dictionary<ulong, List<DataEvent>> DataFile = new Dictionary<ulong, List<DataEvent>>();

        public class DataEvent
        {
            public string EventName;
            public string ShortNameTarget;
            public int EventType;
            public int AmountTarget;
            public int AmountSuc;
            public bool IsSuc;
        }


        public class ItemStripes
        {
            [JsonProperty("ШортНейм предмета или команда ( Аргумент {steamid} в команде заменит на steamid64 игрока! ) || Shortname of the item or team (the {steamid} Argument in the team will replace the player with steamid64! )")]
            public string PrizeEvent;

            [JsonProperty("СкинИД предмета || SkinID ShortName")]
            public ulong PrizeSkinID;

            [JsonProperty("Количество предмета || Amount ShortName")]
            public int Amount;

            [JsonProperty("Выполнять команду или же выдача предмета? ( true - Команда // false - Предмет ) || To execute a command or the results of the subject? ( true-Command / / false-Item )")]
            public bool ItemOrCommands;
        }

        public class EventSettings
        {
            [JsonProperty("Название нашивки || Name Stripes")]
            public string NameStripes;

            [JsonProperty("Картинка || Image")] 
            public string ImageStripes;

            [JsonProperty("Тип нашивки ( 5 видов - 1 - Gather, 2 - GatherBonus, 3 - Kill, 4 - Loot, 5 - Craft ) || Type Stipes ( 5 types - 1 - Gather, 2 - GatherBonus, 3 - Kill, 4 - Loot, 5 - Craft )")]
            public int TypeStripes;

            [JsonProperty("Предмет, который необходимо добыть, сломать, залутать, или же скрафтить || An item that you need to get, break, looting, or craft")]
            public string TargetItem;

            [JsonProperty("Количество предмета, которое необходимо добыть, сломать, залутать, или же скрафтить || The number of items you need to get, break, looting, or craft")]
            public int TargetAmount;

            [JsonProperty("Описание нашивки || Comments Stipes")]
            public string CommentsSripes;

            [JsonProperty("Описание призов || Comments Prize")]
            public string CommentsPrize;

            [JsonProperty("Призы, которые получит игрок после выполнения данной нашивки || Prizes that the player will receive after completing this patch")]
            public List<ItemStripes> ListPrize { get; set; }

        }

        public class VKSettings
        {
            [JsonProperty("Оповещать ли администрации о полностью пройденном ивенте в ВК? || Should I notify the administration of a fully completed event in the VK?")]
            public bool NotificationAdmins;

            [JsonProperty("Вк админов ( ПРИМЕР: 1111111,222222,33333 ) || VK admins ( EXAMPLE: 1111111,222222,33333 )")]
            public string ListAdmins;

            [JsonProperty("VK TOKEN")] public string VKToken;

            [JsonProperty("Текст оповещения || Message Notification")]
            public string MessageNotification;
        }


        public class MainSettings
        {
            [JsonProperty("Включить выдачу баланса на магазин? ( Тут 2 выбора - либо после выполнения нашивок вам в вк отправляется сообщение, потом уже вы перекидываете ему реальные деньги, либо же выдается баланс на магазин АВТОМАТИЧЕСКИ ) || To include the results of balance at the store? ( There are 2 choices-either after the event is completed, a message is sent to you in the VK, then you transfer real money to it, or the balance is issued to the store AUTOMATICALLY )")]
            public bool GiveBalance;

            [JsonProperty("MoscowOVH или GameStores || MoscowOVH or GameStores")]
            public bool Store;

            [JsonProperty("Номер магазина! ( для GameStores ) || The store number! ( for GameStores )")]
            public string ShopID;

            [JsonProperty("Секретный ключ ( для GameStores ) || Secret key ( for Game Stores )")]
            public string APIKey;

            [JsonProperty("Количество рублей, которое будет выдаваться при сдаче всех нашивок || The number of rubles that will be issued when all stripes are handed over")]
            public int MoneyRub;

            [JsonProperty("Оповещать ли игрока через плагин Notifications (RustPlugin.ru) о том, что он выполнил нашивку? || Whether to notify the player via the notifications plugin (RustPlugin.ru) that he completed the patch?")]
            public bool Notif;

            [JsonProperty("Текст оповещения || Text Notification")]
            public string DescNotif;

            [JsonProperty("Картинка оповещения || Image Notifaction")]
            public string ImageNotif;

            [JsonProperty("Текст возле кнопки || The text next to the button")]
            public string TextInButton;

            [JsonProperty("Вайп даты || Wipe Data")]
            public bool WipeData;

            [JsonProperty("Сообщение, когда игрок сдает одну нашивку || Message when a player deals one stripes")]
            public string MessageToTake;

            [JsonProperty("Сообщение, когда игрок сдал все нашивки || Message when the player has passed all the stripes")]
            public string MessageToAllTake;
        }


        private ConfigData _config;

        class ConfigData
        {
            [JsonProperty("Настройка плагина // Settings Plugin")]
            public MainSettings SettingsMain;

            [JsonProperty("Настройка оповещений // Settings Notification")]
            public VKSettings SettingsVK;

            [JsonProperty("Настройка нашивок // Settings Stripes")]
            public List<EventSettings> SettingsStipes;

            public static ConfigData GetNewCong()
            {
                ConfigData newConfig = new ConfigData();
                newConfig.SettingsMain = new MainSettings()
                {
                    GiveBalance = true,
                    Store = true,
                    ShopID = "SHOPID",
                    APIKey = "KEY",
                    Notif = true,
                    DescNotif = "",
                    ImageNotif = "",
                    TextInButton = "Откройте все {1} нашивок и нажмите кнопку ПОЛУЧИТЬ",
                    WipeData = true,
                    MoneyRub = 1500,
                    MessageToTake = "Поздравляю! Вы прошли нашивку - {1} и получили приз с него!",
                    MessageToAllTake = "Поздравляю! Вы полностью прошли все нашивки, и заслужили приз. Проверьте баланса магазина, или отпишите администратору, что бы он выдал вам приз!" 
                };
                newConfig.SettingsVK = new VKSettings()
                {
                    NotificationAdmins = false,
                    ListAdmins = "11111, 11111",
                    VKToken = "VK TOKEN",
                    MessageNotification = "Игрок {1} выполнил все нашивки и нажал кнопку ЗАБРАТЬ",
                };
                newConfig.SettingsStipes = new List<EventSettings>()
                {
                    new EventSettings()
                    {
                        NameStripes = "Приготовления к рейду",
                        ImageStripes = "https://i.imgur.com/20xxq2k.png",
                        TypeStripes = 2,
                        TargetAmount = 50,
                        TargetItem = "sulfur.ore",
                        CommentsSripes = "Добыть 50 твёрдой серной породы",
                        CommentsPrize = "Награда: Набор ресурсов",
                        ListPrize = new List<ItemStripes>()
                        {
                            new ItemStripes()
                            {
                                PrizeEvent = "scrap",
                                Amount = 500,
                                PrizeSkinID = 0,
                                ItemOrCommands = false,
                            },
                            new ItemStripes()
                            {
                                PrizeEvent = "sulfur",
                                Amount = 1500,
                                PrizeSkinID = 0,
                                ItemOrCommands = false,
                            },
                            new ItemStripes()
                            {
                                PrizeEvent = "metal.fragments",
                                Amount = 2500,
                                PrizeSkinID = 0,
                                ItemOrCommands = false
                            }
                        }
                    },
                };
                return newConfig;
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => _config = ConfigData.GetNewCong();
        protected override void SaveConfig() => Config.WriteObject(_config);



        void OnNewSave()
        {
            if (_config.SettingsMain.WipeData)
            {
                DataFile?.Clear();
                DataFile  = new Dictionary<ulong, List<DataEvent>>();
                Interface.Oxide.DataFileSystem.WriteObject(Name, DataFile);
                Interface.Oxide.ReloadPlugin(Name);
            }
        }
        void OnServerInitialized()
        {
						PrintWarning("\n-----------------------------\n" +
            " Author - Sempai#3239\n" +
            " VK - https://vk.com/rustnastroika/n" +
            " Forum - https://whiteplugins.ru/n" +
            " Discord - https://discord.gg/5DPTsRmd3G/n" +
            "-----------------------------");
            try
            {
                DataFile = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, List<DataEvent>>>(Name);
            }
            catch (Exception ex)
            {

                PrintError($"Failed to load stripes file (is the file corrupt?). Please contact developer. Error: ({ex.Message})");
                DataFile = new Dictionary<ulong, List<DataEvent>>();
            }

            foreach (var variable in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(variable);
            }

            foreach (var key in _config.SettingsStipes)
            {
                ImageLibrary.Call("AddImage", key.ImageStripes, key.ImageStripes);
            }

            if (_config.SettingsMain.Notif)
            {
                Notifications?.Call("AddImage", "stripes", _config.SettingsMain.ImageNotif);
            }

            timer.Every(60, () =>
            {
                if (DataFile != null)
                    Interface.Oxide.DataFileSystem.WriteObject(Name, DataFile);
            });
            
            PrintWarning($"|-----------------------------------|\n|          Author: SPARKLESS     |\n|          VK: vk.com/draggb         |\n|          Discord: Sparkless#7640      |\n|          Email: romansparkless@gmail.com      |\n|-----------------------------------|\nIf you want to order a plugin from me, I am waiting for you in discord.");
            PrintWarning("Благодарим за покупку плагина на сайте RustPlugin.ru. Если вы передадите этот плагин сторонним лицам знайте - это лишает вас гарантированных обновлений!");
            PrintWarning("Плагин был успешно загружен!");

        }
        
        
        void EventProcess(BasePlayer player)
        {
            foreach (var variable in _config.SettingsStipes)
            {
                var data = DataFile[player.userID];
                var find = data.FirstOrDefault(p => p.EventName == variable.NameStripes);
                if (find == null)
                {
                    data.Add(new DataEvent
                    {
                        EventName = variable.NameStripes,
                        EventType = variable.TypeStripes,
                        AmountTarget = variable.TargetAmount,
                        AmountSuc = 0,
                        ShortNameTarget = variable.TargetItem
                    });
                }
            }
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player == null) return;
            if (!DataFile.ContainsKey(player.userID))
            {
                DataFile.Add(player.userID, new List<DataEvent>());
            }
            EventProcess(player);
        }

        void Unload()
        {
            if (DataFile != null)
                Interface.Oxide.DataFileSystem.WriteObject(Name, DataFile);
        }

        void OnServerSave()
        {
            if (DataFile != null)
                Interface.Oxide.DataFileSystem.WriteObject(Name, DataFile);
        }

        public string Layer = "UI_CupLayerEvent";

        private static string HexToCuiColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
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
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }
        
        [ChatCommand("stripes")]
        void OpenEvent(BasePlayer player)
        {
            if (!DataFile.ContainsKey(player.userID))
            {
                OnPlayerConnected(player);
            }

            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                Image = {Color = HexToCuiColor("#000000E6")},
            }, "Overlay", Layer);
            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "-100 -100", AnchorMax = "100 100", OffsetMax = "0 0"},
                Button = {Color = "0 0 0 0", Close = Layer},
                Text = {Text = ""}
            }, Layer);
            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.2296872 0.7296272", AnchorMax = "0.76875 0.8398125"},
                Button = {Color = "0.2784314 0.2627451 0.227451 0.8271052", FadeIn = 0.1f},
                Text =
                {
                    Text = $"НАШИВКИ", FontSize = 29, Align = TextAnchor.MiddleCenter, Color = HexToCuiColor("#bdb9b6"),
                    Font = "robotocondensed-bold.ttf"
                }
            }, Layer);
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {Color = "0.227451 0.2156863 0.1921569 0.8271052"},
                    new CuiRectTransformComponent {AnchorMin = "0.2296872 0.2703704", AnchorMax = "0.76875 0.727779"}
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {Color = "0.2784314 0.2627451 0.227451 0.8271052"},
                    new CuiRectTransformComponent {AnchorMin = "0.2296872 0.1583376", AnchorMax = "0.76875 0.2685263"}
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.2942707 0.1657407", AnchorMax = "0.5567709 0.2666665"},
                Button = {Color = "0 0 0 0", FadeIn = 0.1f},
                Text =
                {
                    Text = _config.SettingsMain.TextInButton.Replace("{1}", _config.SettingsStipes.Count.ToString()), FontSize = 12, Align = TextAnchor.MiddleLeft,
                    Color = "0.7607843 0.7450981 0.7411765 1", Font = "robotocondensed-bold.ttf"
                }
            }, Layer);
            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.5739585 0.1805556", AnchorMax = "0.659896 0.2509257"},
                Button =
                {
                    Color = "0.3411765 0.3372549 0.3176471 0.8235294", Close = Layer,
                    Command = "stripes.event takeall", FadeIn = 0.1f
                },
                Text =
                {
                    Text = $"ПОЛУЧИТЬ", FontSize = 14, Align = TextAnchor.MiddleCenter,
                    Color = HexToCuiColor("#bdb9b6"), Font = "robotocondensed-bold.ttf"
                }
            }, Layer);

            CuiHelper.AddUi(player, container);
            OpenStripesList(player, 1);
        }


        void OpenStripesList(BasePlayer player, int page)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layer + ".Back");
            container.Add(new CuiButton
            {
                RectTransform =
                    {AnchorMin = "0.2312569 0.1611111", AnchorMax = "0.3036531 0.2703702", OffsetMax = "0 0"},
                Button =
                {
                    FadeIn = 0f, Color = "0 0 0 0",
                    Command = $"stripes.event {page - 1}"
                },
                Text =
                {
                    Text = "◄", Align = TextAnchor.MiddleCenter, Color = "0.7137255 0.7137255 0.7058824 1",
                    Font = "robotocondensed-bold.ttf",
                    FontSize = 30
                }
            }, Layer, Layer + ".Back");
            CuiHelper.DestroyUi(player, Layer + ".Run");
            container.Add(new CuiButton
            {
                RectTransform =
                    {AnchorMin = "0.6937503 0.1611111", AnchorMax = "0.7661461 0.2703702", OffsetMax = "0 0"},
                Button =
                {
                    FadeIn = 0f, Color = "0 0 0 0",
                    Command = $"stripes.event {page + 1}"
                },
                Text =
                {
                    Text = "►", Align = TextAnchor.MiddleCenter, Color = "0.7137255 0.7137255 0.7058824 1",
                    Font = "robotocondensed-bold.ttf",
                    FontSize = 30
                }
            }, Layer, Layer + ".Run");

            for (int i = 0; i < 9; i++)
            {
                CuiHelper.DestroyUi(player, Layer + $".{i}.EventStrip");
            }
            foreach (var check in _config.SettingsStipes.Select((i, t) => new {A = i, B = t - (page - 1) * 9}).Skip((page - 1) * 9).Take(9))
            {
                container.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin =
                            $"{0.2317708 + check.B * 0.181 - Math.Floor((double) check.B / 3) * 3 * 0.181} {0.5842593 - Math.Floor((double) check.B / 3) * 0.155}",
                        AnchorMax =
                            $"{0.4057293 + check.B * 0.181 - Math.Floor((double) check.B / 3) * 3 * 0.181} {0.7249999 - Math.Floor((double) check.B / 3) * 0.155}",
                        OffsetMax = "0 0"
                    },
                    Button =
                    {
                        Color = HexToCuiColor("#43403BD2"),
                        Command = $""
                    },
                    Text =
                    {
                        Text = $"", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf",
                        FontSize = 25, Color = HexToCuiColor("#d5cfcd")
                    }
                }, Layer, Layer + $".{check.B}.EventStrip");
                var list = DataFile[player.userID].FindAll(p => p.EventName == check.A.NameStripes);
                if (list.Count <= 0)
                {
                    PrintWarning("List Count is null || zero. Please Contact Developer!");
                    CuiHelper.AddUi(player, container);
                    return;
                }
                container.Add(new CuiElement
                {
                    FadeOut = 0.3f,
                    Parent = Layer + $".{check.B}.EventStrip",
                    Name = Layer + $".{check.B}.EventStripImg",
                    Components =
                    {
                        new CuiRawImageComponent
                            {FadeIn = 0.3f, Png = (string) ImageLibrary.Call("GetImage", check.A.ImageStripes)},
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0.5628417", AnchorMax = "1 0.9781424"}
                    }
                });
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0 0.398907", AnchorMax = "1 0.5628405"},
                    Button = {Color = "0 0 0 0", FadeIn = 0.1f},
                    Text =
                    {
                        Text = check.A.NameStripes, FontSize = 10, Align = TextAnchor.MiddleCenter,
                        Color = "0.7607843 0.7450981 0.7411765 1", Font = "robotocondensed-regular.ttf"
                    }
                }, Layer + $".{check.B}.EventStrip", Layer + $".{check.B}.EventStripTextName");


                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0 0.3068017", AnchorMax = "1 0.4707354"},
                    Button = {Color = "0 0 0 0", FadeIn = 0.1f},
                    Text =
                    {
                        Text = check.A.CommentsSripes, FontSize = 10, Align = TextAnchor.MiddleCenter,
                        Color = "0.7607843 0.7450981 0.7411765 1", Font = "robotocondensed-regular.ttf"
                    }
                }, Layer + $".{check.B}.EventStrip", Layer + $".{check.B}.EventStripTextComments");
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0 0.2081174", AnchorMax = "1 0.3720511"},
                    Button = {Color = "0 0 0 0", FadeIn = 0.1f},
                    Text =
                    {
                        Text = $"{check.A.CommentsPrize}", FontSize = 10, Align = TextAnchor.MiddleCenter,
                        Color = "0.7607843 0.7450981 0.7411765 1", Font = "robotocondensed-regular.ttf"
                    }
                }, Layer + $".{check.B}.EventStrip", Layer + $".{check.B}.EventStripTextPrize");

                var listdata = list.FirstOrDefault(p => p.EventName == check.A.NameStripes);

                if (listdata != null && listdata.AmountSuc >= listdata.AmountTarget && !listdata.IsSuc)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = "0 0.005463466", AnchorMax = "0.9970052 0.1475405"},
                        Button =
                        {
                            Color = HexToCuiColor("#575651D2"),
                            Command = $"stripes.event take {check.A.NameStripes}", FadeIn = 0.1f
                        },
                        Text =
                        {
                            Text = "ЗАБРАТЬ ПРИЗ", FontSize = 10, Align = TextAnchor.MiddleCenter,
                            Color = "0.7607843 0.7450981 0.7411765 1", Font = "robotocondensed-regular.ttf"
                        }
                    }, Layer + $".{check.B}.EventStrip", Layer + $".{check.B}.EventStripPrize");
                }
                else if (listdata != null && listdata.AmountSuc < listdata.AmountTarget)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = "0 0.005463466", AnchorMax = "1 0.1475405"},
                        Button =
                        {
                            Color = "0 0 0 0", FadeIn = 0.1f
                        },
                        Text =
                        {
                            Text = $"{listdata.AmountSuc}/{listdata.AmountTarget}", FontSize = 10,
                            Align = TextAnchor.MiddleCenter, Color = "0.7607843 0.7450981 0.7411765 1",
                            Font = "robotocondensed-regular.ttf"
                        }
                    }, Layer + $".{check.B}.EventStrip", Layer + $".{check.B}.EventStripPrize");
                }
                else if (listdata != null && listdata.IsSuc)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = "0 0.005463466", AnchorMax = "0.9970052 0.1475405"},
                        Button = {Color = "0 0 0 0", FadeIn = 0.1f},
                        Text =
                        {
                            Text = "ВЫПОЛНЕНО", FontSize = 10, Align = TextAnchor.MiddleCenter,
                            Color = "0.7607843 0.7450981 0.7411765 1", Font = "robotocondensed-regular.ttf"
                        }
                    }, Layer + $".{check.B}.EventStrip", Layer + $".{check.B}.EventStripPrize");
                }
            }
            

            CuiHelper.AddUi(player, container);
        }


        [ConsoleCommand("stripes.event")]
        void StripesCommand(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            int page = 1;
            if (int.TryParse(args.Args[0], out page) && page > 0 && (page - 1) * 9 <= _config.SettingsStipes.Count)
            {
                OpenStripesList(player, page);
            }
            else if (args.Args[0] == "take")
            {
                var findconfig = _config.SettingsStipes.FirstOrDefault(p => p.NameStripes == string.Join(" ", args.Args.Skip(1).ToArray()));
                if (findconfig == null) return;
                var finddata = DataFile[player.userID].FirstOrDefault(p => p.EventName == string.Join(" ", args.Args.Skip(1).ToArray()));
                if (finddata == null) return;
                if (finddata.AmountSuc < finddata.AmountTarget) return;
                if (finddata.IsSuc) return;
                foreach (var item in findconfig.ListPrize)
                {
                    if (item.ItemOrCommands)
                    {
                        GiveCommands(player, item.PrizeEvent);
                    }
                    else
                    {
                        var itemtarget = ItemManager.CreateByName(item.PrizeEvent, item.Amount, item.PrizeSkinID);
                        player.GiveItem(itemtarget, BaseEntity.GiveItemReason.PickedUp);
                    }
                }
                player.ChatMessage(_config.SettingsMain.MessageToTake.Replace("{1}", findconfig.NameStripes));
                finddata.IsSuc = true;
            }
            else if (args.Args[0] == "takeall")
            {
                var find = DataFile[player.userID].FindAll(p => p.IsSuc == true);
                if (find.Count < _config.SettingsStipes.Count) return;
                if (_config.SettingsMain.GiveBalance)
                {
                    if (_config.SettingsMain.Store)
                    {
                        APIChangeUserBalance(player.userID, _config.SettingsMain.MoneyRub, null);
                    }
                    else
                    {
                        MoneyPlus(player.userID, _config.SettingsMain.MoneyRub);
                    }
                    DataFile[player.userID].Clear();
                    EventProcess(player);
                    player.ChatMessage(_config.SettingsMain.MessageToAllTake);
                }
                else
                {
                    if (_config.SettingsVK.NotificationAdmins)
                    {
                        SendVkMessage(_config.SettingsVK.ListAdmins, _config.SettingsVK.MessageNotification.Replace("{1}", $"[{player.userID}] {player.displayName}"));
                    }
                    DataFile[player.userID].Clear();
                    EventProcess(player);
                    player.ChatMessage(_config.SettingsMain.MessageToAllTake);
                }
            }
        }
        
        private string URLEncode(string input)
        {
            if (input.Contains("#")) input = input.Replace("#", "%23");
            if (input.Contains("$")) input = input.Replace("$", "%24");
            if (input.Contains("+")) input = input.Replace("+", "%2B");
            if (input.Contains("/")) input = input.Replace("/", "%2F");
            if (input.Contains(":")) input = input.Replace(":", "%3A");
            if (input.Contains(";")) input = input.Replace(";", "%3B");
            if (input.Contains("?")) input = input.Replace("?", "%3F");
            if (input.Contains("@")) input = input.Replace("@", "%40");
            return input;
        }
        
        void GetCallback(int number, string param, string message)
        {
             
        }
        private string app = "v=5.92";
        
        private System.Random random = new System.Random();
        private string RandomId() => random.Next(Int32.MinValue, Int32.MaxValue).ToString();
        private void SendVkMessage(string reciverID, string msg) => webrequest.Enqueue("https://api.vk.com/method/messages.send?user_ids=" + reciverID + "&message=" + URLEncode(msg) + "&"+app + "&random_id=" + RandomId() + "&access_token=" + _config.SettingsVK.VKToken, null, (code, response) => GetCallback(code, response, "Сообщение"), this);
        
        
        void MoneyPlus(ulong userId, int amount)
        {
            ExecuteApiRequest(new Dictionary<string, string>()
            {
                { "action", "moneys" },
                { "type", "plus" },
                { "steam_id", userId.ToString() },
                { "amount", amount.ToString() }
            });
        }
        void ExecuteApiRequest(Dictionary<string, string> args)
        {
            string url = $"http://gamestores.ru/api?shop_id={_config.SettingsMain.ShopID}&secret={_config.SettingsMain.APIKey}" +
                         $"{string.Join("", args.Select(arg => $"&{arg.Key}={arg.Value}").ToArray())}";
            webrequest.EnqueueGet(url, (i, s) =>
            {
                if (i != 200)
                {
                    LogToFile("ATMBox", $"Код ошибки: {i}, подробности:\n{s}", this);
                }
                else
                {
                    if (s.Contains("fail"))
                    {
                        return;
                    }
                }
            }, this);
        }

        void APIChangeUserBalance(ulong steam, int balanceChange, Action<string> callback)
        {
            plugins.Find("RustStore")?.CallHook("APIChangeUserBalance", steam, balanceChange, new Action<string>((result) =>
            {
                if (result == "SUCCESS")
                {
                    Interface.Oxide.LogDebug($"Баланс пользователя {steam} увеличен на {balanceChange}");
                    return;
                }
                Interface.Oxide.LogDebug($"Баланс не был изменен, ошибка: {result}");
            }));
        }
        
        void GiveCommands(BasePlayer player, string text)
        {
            string command = text.Replace("{steamid}", player.UserIDString);
            ConsoleSystem.Arg arg = new ConsoleSystem.Arg(ConsoleSystem.Option.Server, command);
            arg.cmd.Call(arg);
        }

        public bool CheckStripes(BasePlayer player, string shortname, TypeEvent type)
        {
            if (!DataFile.ContainsKey(player.userID))
            {
                OnPlayerConnected(player);
            }
            var data = DataFile[player.userID].FirstOrDefault(p => p.ShortNameTarget == shortname && p.EventType == (int) type && p.IsSuc == false);
            if (data != null)
            {
                return true;
            }

            return false;
        }

        public void Progress(BasePlayer player, string shortname, int amount)
        {
            var data = DataFile[player.userID];
            if (string.IsNullOrEmpty(shortname)) return;
            if (data.Count > 0)
            {
                var key = data.FirstOrDefault(p => p.ShortNameTarget == shortname);
                if (key != null)
                {
                    if (key.AmountSuc < key.AmountTarget && key.IsSuc == false)
                    {
                        key.AmountSuc += amount;
                        if (key.AmountSuc >= key.AmountTarget)
                        {
                            key.AmountSuc = key.AmountTarget;
                            if (_config.SettingsMain.Notif)
                            {
                                Notifications.Call("ShowNotify", player.userID, 5f, "ОПОВЕЩЕНИЕ", _config.SettingsMain.DescNotif, "stripes", null);  
                            }
                        }
                    }
                }
            }
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            if (CheckStripes(player, item.info.shortname, TypeEvent.GatherBonus))
            {
                NextTick(() => Progress(player, item.info.shortname, 1));
            }
        }

        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (item == null || player == null) return;
            if (player != null)
            {
                if (CheckStripes(player, item.info.shortname, TypeEvent.Gather))
                {
                    NextTick(() => Progress(player, item.info.shortname, item.amount));
                }
            }
        }

        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            if (CheckStripes(player, item.info.shortname, TypeEvent.Gather))
            {
                NextTick(() => Progress(player, item.info.shortname, item.amount));
            }

            return null;
        }

        private List<string> containerNames = new List<string>
        {
            "crate_basic",
            "crate_elite",
            "crate_mine",
            "crate_tools",
            "crate_normal",
            "crate_normal_2",
            "crate_normal_2_food",
            "crate_normal_2_medical",
            "crate_underwater_advanced",
            "crate_underwater_basic",
            "foodbox",
            "minecart",
            "bradley_crate",
            "heli_crate",
            "codelockedhackablecrate",
            "supply_drop",
            "presentdrop"
        };
        
        public List<StorageContainer> ListContainer = new List<StorageContainer>();

        object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (container == null || player == null) return null;
            if (!containerNames.Contains(container.ShortPrefabName)) return null;
            if (ListContainer.Contains(container)) return null;
            if (container.inventory.itemList.Count == 0) return null;
            foreach (var key in container.inventory.itemList)
            {
                if (CheckStripes(player, key.info.shortname, TypeEvent.Loot))
                {
                    NextTick(() => Progress(player, key.info.shortname, key.amount));
                }
            }
            ListContainer.Add(container);
            return null;
        }

        void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            var player = task.owner;
            if (player != null)
            {
                if (CheckStripes(player, item.info.shortname, TypeEvent.Craft))
                {
                    NextTick(() => Progress(player, item.info.shortname, item.amount));
                }
            }
        }

        private Dictionary<uint, string> LastHeliHit = new Dictionary<uint, string>();

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return null;
            if (entity is BaseHelicopter && info.Initiator is BasePlayer)
                LastHeliHit[entity.net.ID] = info.InitiatorPlayer.UserIDString;
            return null;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;
            if (entity is BaseHelicopter)
            {
                if (LastHeliHit.ContainsKey(entity.net.ID))
                {
                    if (LastHeliHit[entity.net.ID] != null)
                    {
                        BasePlayer player = BasePlayer.Find(LastHeliHit[entity.net.ID]);
                        if (player != null)
                        {
                            var entnames = entity.ShortPrefabName;
                            if (CheckStripes(player, entnames, TypeEvent.Kill))
                            {
                                LastHeliHit.Remove(entity.net.ID);
                                Progress(player, entnames, 1);
                            }
                        }
                    }   
                }
            }
            var entname = entity.ShortPrefabName;
            BasePlayer players = info.InitiatorPlayer;
            if (players != null)
            {
                if (CheckStripes(players, entname, TypeEvent.Kill))
                {
                    Progress(players, entname, 1);
                }
            }
        }
    }
