using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("XPSystem", "whiteplugins.ru edited by CASHR", "0.1.0")]
    class XPSystem : RustPlugin
    {
        [PluginReference] Plugin ImageLibrary;
        private string Layer = "UI_XPSYSTEM";
        private readonly List<uint> crateInfo = new List<uint>();
        private static Configuration Settings = new Configuration();

        #region Configuration [Конфиг] 

        private class Configuration
        {
            public class API
            {
                [JsonProperty("APIKey (Секретный ключ) - Оставьте поле пустым если у Вас Moscow.ovh магазин")]
                public string SecretKey = "";
                [JsonProperty("ShopID (ИД магазина в сервисе) - Оставьте поле пустым если у Вас Moscow.ovh магазин")]
                public string ShopID = "";
            }

            public class Balance
            {
                [JsonProperty("Сколько рублей на баланс магазина будет выдаваться")]
                public int Money = 5;
            }

            public class Gather
            {
                [JsonProperty("Сколько игроку будет выдаваться XP за подбирание ресурсов")]
                public float CollectiblePickup = 0.15f; //Settings.GatherSettings.CollectiblePickup
                [JsonProperty("Сколько игроку будет выдаваться XP за подбирание плантации")]
                public float CropGather = 0.15f; //Settings.GatherSettings.CropGather
                [JsonProperty("Сколько игроку будет выдаваться XP за фарм бочек")]
                public float BarrelGather = 0.5f; //Settings.GatherSettings.BarrelGather
                [JsonProperty("Сколько игроку будет выдаваться XP за фарм ящиков")]
                public float CrateGather = 0.5f; //Settings.GatherSettings.CrateGather
                [JsonProperty("Сколько игроку будет выдаваться XP за убийство животного")]
                public float AnimalGather = 0.5f; //Settings.GatherSettings.AnimalGather
            }

            public class GathersOptions
            {
                [JsonProperty("Сколько игроку будет выдаваться XP за фарм дерева")]
                public float WoodGathers = 0.5f; //Settings.GathersSetting.WoodGathers
                [JsonProperty("Сколько игроку будет выдаваться XP за фарм обычного камня stones")]
                public float StonesGathers = 0.5f; //Settings.GathersSetting.StonesGathers
                [JsonProperty("Сколько игроку будет выдаваться XP за фарм металлического камня")]
                public float MetalOreGathers = 0.5f; //Settings.GathersSetting.MetalOreGathers
                [JsonProperty("Сколько игроку будет выдаваться XP за фарм серного камня")]
                public float SulfurGathers = 0.5f; //Settings.GathersSetting.SulfurGathers
                [JsonProperty("Сколько игроку будет выдаваться XP за фарм бонус МВК")]
                public float HQMGathers = 0.5f; //Settings.GathersSetting.HQMGathers						
            }

            public class GUI
            {
                [JsonProperty("Разрешение для использования команды /xp")]
                public string XpPermission = "xpsystem.use";
                [JsonProperty("Курс обмена XP на баланс магазина")]
                public string XPToBalance = "Курс обмена XP - <color=#a5e664>100XP = 5RUB</color>\nна баланс в магазине.";
                [JsonProperty("Текст в основном блоке (возле аватарки)")]
                public string MainBlockText = "<color=#a5e664><b>XP</b></color> можно заработать путём:\n\n<color=#a5e664><b>—</b></color> Подбирания ресурсов\n<color=#a5e664><b>—</b></color> Фарма руды и деревьев\n<color=#a5e664><b>—</b></color> Выращивания еды (плантации)\n<color=#a5e664><b>—</b></color> Фарма бочек и ящиков\n<color=#a5e664><b>—</b></color> Охоты на людей и животных";
                [JsonProperty("Текст во втором нижнем блоке")]
                public string SecondBlockText = "Перед выводом <color=#a5e664><b>XP</b></color> убедитесь, что Вы авторизованы в нашем магазине <color=#a5e664>SHOP.GAMESTORES.SU</color>";
            }
            [JsonProperty("Во сколько увеличиваем количество начисляемого XP у привилегированного игрока")]
            public float vipamount = 2; 
            [JsonProperty("Какую привилегию использовать для большего начисления XP")]
            public  string vippermission = "xpsystem.vip";
            [JsonProperty("Настройки API плагина")]
            public API APISettings = new API();
            [JsonProperty("Настройка кол-ва выдачи баланса игроку")]
            public Balance BalanceSettings = new Balance();
            [JsonProperty("Настройка кол-ва выдачи XP с фарма ресурсов")]
            public Gather GatherSettings = new Gather();
            [JsonProperty("Более подробная настройка кол-ва выдачи XP с фарма ресурсов и животных")]
            public GathersOptions GathersSetting = new GathersOptions();
            [JsonProperty("Настройка интерфейса")]
            public GUI GUISettings = new GUI();
        }

        #endregion

        #region ConfigLoad / DataFiles [Загрузка конфига и информация в дате]

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                Settings = Config.ReadObject<Configuration>();
                if (Settings?.APISettings == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка в конфиге... Создаю новую конфигурацию!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig()
        {
            Settings = new Configuration();
        }

        protected override void SaveConfig() => Config.WriteObject(Settings);

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
            SaveData();
        }

        private void OnServerSave()
        {
            SaveData();
        }
        public class Xp
        {
            [JsonProperty("Количество XP у игрока:")]
            public float XP { get; set; }
        }

        private Dictionary<ulong, Xp> XPS { get; set; }

        #endregion

        #region GameStores [Запрос в GameStores для обмена XP на рубли] 


        [PluginReference] Plugin RustStore;
        bool LogsPlayer = true;

        private void AddMoney(ulong userId, int amount)
        {
            ExecuteApiRequest(new Dictionary<string, string>()
            {
                { "action", "moneys" },
                { "type", "plus" },
                { "steam_id", userId.ToString() },
                { "amount", amount.ToString() },
                { "mess", "Вывод средств XPSystem"}
            }, RustStore);
        }

        void ExecuteApiRequest(Dictionary<string, string> args, bool Moscow)
        {
            if (!Moscow)
            {
                string url = $"http://gamestores.ru/api?shop_id={Settings.APISettings.ShopID}&secret={Settings.APISettings.SecretKey}" +
                             $"{string.Join("", args.Select(arg => $"&{arg.Key}={arg.Value}").ToArray())}";
                LogToFile("test", url, this);
                webrequest.EnqueueGet(url, (i, s) =>
                {
                    if (i != 200)
                    {
                        PrintError($"Ошибка соединения с сайтом GS!");
                    }
                    else
                    {
                        JObject jObject = JObject.Parse(s);
                        if (jObject["result"].ToString() == "fail")
                        {
                            PrintError($"Ошибка пополнения баланса для {args["steam_id"]}!");
                            PrintError($"Причина: {jObject["message"].ToString()}");
                        }
                        else
                            PrintWarning($"Игрок {args["steam_id"]} успешно получил {args["amount"]} рублей");
                    }
                }, this);
            }
            else
            {
                RustStore?.Call("APIChangeUserBalance", Convert.ToUInt64(args["steam_id"]), Convert.ToInt32(args["amount"]), null);
            }

        }

        #endregion

        #region XPFromGather [Добыча]

        object OnCollectiblePickup(Item item, BasePlayer player)
        {
            GiveExp(player,"pickup");
            return null;
        }
        object OnGrowableGather(GrowableEntity plant, BasePlayer player)
        {
            GiveExp(player, "growable");
            return null;
        }
        private void GiveExp(BasePlayer player, string type)
        {
            if (!XPS.ContainsKey(player.userID)) OnPlayerConnected(player);
            float amount = 1;
            if (permission.UserHasPermission(player.UserIDString, Settings.vippermission))
            {
                amount = Settings.vipamount;
            }
            switch (type)
            {
                case "pickup":
                    XPS[player.userID].XP += Settings.GatherSettings.CollectiblePickup * amount;
                    break;
                case "growable":
                    XPS[player.userID].XP += Settings.GatherSettings.CropGather * amount;
                    break;
                case "barrel":
                    XPS[player.userID].XP += Settings.GatherSettings.BarrelGather * amount;
                    break;
                case "animal":
                    XPS[player.userID].XP += Settings.GatherSettings.AnimalGather * amount;
                    break;
                case "crate":
                    XPS[player.userID].XP += Settings.GatherSettings.CrateGather * amount;
                    break;
                default:
                    PrintError($"Не найден нужный тип поинтов, отпишите разработчику {type}");
                    break;
            }
        }
        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            ProcessItem(player, item);
            return;
        }

        void ProcessItem(BasePlayer player, Item item)
        {
            if (!XPS.ContainsKey(player.userID)) OnPlayerConnected(player);
            float amount = 1;
            if (permission.UserHasPermission(player.UserIDString, Settings.vippermission))
            {
                amount = Settings.vipamount;
            }
            switch (item.info.shortname)
            {
                case "wood":
                    XPS[player.userID].XP += Settings.GathersSetting.WoodGathers * amount;
                    return;
                case "stones":
                    XPS[player.userID].XP += Settings.GathersSetting.StonesGathers * amount;
                    return;
                case "metal.ore":
                    XPS[player.userID].XP += Settings.GathersSetting.MetalOreGathers * amount;
                    return;
                case "sulfur.ore":
                    XPS[player.userID].XP += Settings.GathersSetting.SulfurGathers * amount;
                    return;
                case "hq.metal.ore":
                    XPS[player.userID].XP += Settings.GathersSetting.HQMGathers * amount;
                    return;
            }
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null) { return; }
            if (info == null) { return; }
            if (info.InitiatorPlayer == null) { return; }
            BasePlayer player = info.InitiatorPlayer;
            if (player == null) return;
            if (entity is BaseAnimalNPC)
            {
                GiveExp(player,"animal");
                return;
            }
            if (entity.PrefabName.Contains("barrel"))
            {
                GiveExp(player,"barrel");
                return;
            }
        }

        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (!entity.ShortPrefabName.Contains("crate_") && entity.ShortPrefabName != "heli_crate")
                return;
            if (crateInfo.Contains(entity.net.ID))
                return;
            crateInfo.Add(entity.net.ID);
            GiveExp(player,"crate");
        }

        #endregion

        #region Hooks [Хуки/проверка]

        private void OnServerInitialized()
        {
            if (string.IsNullOrEmpty(Settings.APISettings.SecretKey) && !RustStore)
            {
                PrintError("У Вас не указаны данные для вывода на GameStores, так же не найден плагин RustStore от MoscowOVH");
                Interface.Oxide.UnloadPlugin(Title);
                return;
            }
            permission.RegisterPermission(Settings.GUISettings.XpPermission, this);
            permission.RegisterPermission(Settings.vippermission, this);
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("XPSystem/Database"))
            {
                XPS = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Xp>>("XPSystem/Database");
            }
            else
            {
                XPS = new Dictionary<ulong, Xp>();
            }

            foreach (var check in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(check);
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                timer.In(3f, () =>
                {
                    OnPlayerConnected(player);
                    return;
                });
            }
            if (player == null) return;
            if (!XPS.ContainsKey(player.userID))
                XPS.Add(player.userID, new Xp { XP = 0 });
        }

        private void SaveData()
        {
            if (XPS != null)
                Interface.Oxide.DataFileSystem.WriteObject("XPSystem/Database", XPS);
        }

        #endregion

        #region Commands [Команды]

        [ChatCommand("xp")]
        void cmdXp(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, Settings.GUISettings.XpPermission))
            {
                SendReply(player, "У вас <color=#a5e664>недостаточно</color> прав для использования этой команды!");
                return;
            }

            DrawGUI(player);
        }

        [ConsoleCommand("xpsell")]
        private void CmdXpSell(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            CuiHelper.DestroyUi(player, Layer);

            if (XPS[player.userID].XP >= 100)
            {
                SendReply(player,
                    $"Перевод <color=#a5e664>XP в рубли</color> успешно произведён!\nПроверьте свой баланс в магазине.\n<color=#a5e664>В случае не поступления средств сообщите администрации!</color>");
                XPS[player.userID].XP -= 100;
                AddMoney(player.userID, Settings.BalanceSettings.Money);
                return;
            }
            else
            {
                SendReply(player,
                    $"<color=#a5e664>Обмен не произведён!</color>\nДля обмена нужно иметь <color=#a5e664>100 XP</color>!\n У вас <color=#a5e664>{XPS[player.userID].XP} XP</color>!");
                return;
            }
        }

        [ConsoleCommand("xp.give")]
        private void CmdXpgv(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();

            BasePlayer target = BasePlayer.Find(args.Args[0]);
            if (target != null)
            {
                if (XPS.ContainsKey(target.userID))
                {
                    XPS[target.userID].XP += int.Parse(args.Args[1]);
                    Puts("Баланс успешно изменен");
                }
            }

        }

        #endregion

        #region GUI [Интерфейс]
        private void DrawGUI(BasePlayer player)
        {
            if (!XPS.ContainsKey(player.userID)) OnPlayerConnected(player);
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0.45", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "Overlay", Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent {Text = $"ОБМЕН ВАЛЮТЫ", Color = HexToRustFormat("#FFFFFF"), Font = "robotocondensed-regular.ttf", FontSize = 32, Align = TextAnchor.MiddleCenter },
                    new CuiRectTransformComponent { AnchorMin = "0.1 0.54", AnchorMax = "0.998 0.998"},

                },
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent {Text = Settings.GUISettings.XPToBalance,   Color = HexToRustFormat("#FFFFFF"), Font = "robotocondensed-regular.ttf", FontSize = 15, Align = TextAnchor.MiddleCenter },
                    new CuiRectTransformComponent { AnchorMin = "0.1507811 -0.06944446", AnchorMax = "0.9542188 0.66083329"},

                },
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.45 0.47", AnchorMax = $"0.66 0.713" },
                Button = { Color = "0 0 0 0.4" },
                Text = { Text = Settings.GUISettings.MainBlockText, Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, FadeIn = 1f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.45 0.33", AnchorMax = $"0.66 0.453" },
                Button = { Color = "0 0 0 0.4" },
                Text = { Text = Settings.GUISettings.SecondBlockText, Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, FadeIn = 1f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3 0.4", AnchorMax = $"0.445 0.453" },
                Button = { Color = "0 0 0 0.4" },
                Text = { Text = $"Ваш баланс: <color=#a5e664>{XPS[player.userID].XP}</color>", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, FadeIn = 1f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3 0.47", AnchorMax = $"0.46 0.74" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, FadeIn = 1f }
            }, Layer, "Avatar");

            container.Add(new CuiElement
            {
                Parent = "Avatar",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary?.Call("GetImage", player.UserIDString) },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0.898 0.898", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.281 0.48", AnchorMax = "0.463 0.995" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = $"Здравствуй, <color=#a5e664><b>{player.displayName}</b></color>!", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf", Color = "0.91 0.91 0.91 1" }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3 0.33", AnchorMax = "0.375 0.385" },
                Button = { Color = "0 0 0 0.4", Command = "xpsell" },
                Text = { Text = "ОБМЕНЯТЬ", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf", Color = "0.91 0.91 0.91 1" }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.381 0.33", AnchorMax = "0.445 0.385" },
                Button = { Color = "0 0 0 0.4", Close = Layer },
                Text = { Text = "ЗАКРЫТЬ", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, FadeIn = 1f }
            }, Layer);
            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Helpers [Хелперы]

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

            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        #endregion
    }
