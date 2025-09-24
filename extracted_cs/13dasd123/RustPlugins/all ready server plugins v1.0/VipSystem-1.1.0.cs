using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("VipSystem", "Chibubrik/Topplugin.ru", "1.1.0")]
    class VipSystem : RustPlugin
    {
        #region Вар
        private string Layer = "VIP_UI";
        private string LayerAlert = "ALERT_UI";
        [PluginReference] private Plugin ImageLibrary;
        private Dictionary<ulong, Dictionary<string, VipData>> vipData;
        #endregion

        #region Класс
        public class ItemSettings
        {
            [JsonProperty("Название предмета")] public string DisplayName;
            [JsonProperty("ShortName заменяемого предмета")] public string ShortName;
            [JsonProperty("SkinID заменяемого предмета")] public ulong SkinID;
            [JsonProperty("Шанс выпадения")] public int DropChance;
            [JsonProperty("Время всплывающего уведомления(в секундах)")] public int AlertTime;
        }

        public class VipSettings
        {
            [JsonProperty("Название привилегии")] public string DisplayName;
            [JsonProperty("На сколько дней будет выдаваться привилегия(для названия)")] public string Day;
            [JsonProperty("Команда")] public string Command;
            [JsonProperty("Сколько нужно собрать фрагментов?")] public int Count;
            [JsonProperty("Шанс привлегии")] public int DropChance;
            [JsonProperty("Изображение привилегии")] public string Url;
        }

        public class VipData
        {
            [JsonProperty("Собранные фрагменты игрока")] public int Count;
        }
        #endregion

        #region Конфиг
        public Configuration config;
        public class Configuration
        {
            [JsonProperty("Настройки")] public ItemSettings ItemSettings = new ItemSettings();
            [JsonProperty("Привилегии")] public List<VipSettings> vipSettings;
            [JsonProperty("Ящики, в которых будет падать кастомный предмет")] public List<string> ContainerList;
            public static Configuration GetNewConfig()
            {
                return new Configuration
                {
                    ItemSettings = new ItemSettings
                    {
                        DisplayName = "Фрагменты",
                        ShortName = "xmas.present.large",
                        SkinID = 1858702962,
                        DropChance = 100,
                        AlertTime = 10
                    },
                    vipSettings = new List<VipSettings>
                    {
                        new VipSettings()
                        {
                            DisplayName = "КИНГ",
                            Day = "2 дня",
                            Command = "o.grant user %STEAMID% КИНГ",
                            Count = 15,
                            DropChance = 70,
                            Url = "https://imgur.com/Rp7DJlc.png"

                        },                    
                        new VipSettings()
                        {
                            DisplayName = "ГОЛД",
                            Day = "2 дня",
                            Command = "o.grant user %STEAMID% ГОЛД",
                            Count = 10,
                            DropChance = 1,
                            Url = "https://imgur.com/gO9u6Td.png"
                        },           
                        new VipSettings()
                        {
                            DisplayName = "ВИП",
                            Day = "2 дня",
                            Command = "o.grant user %STEAMID% ВИП",
                            Count = 10,
                            DropChance = 1,
                            Url = "https://imgur.com/T4oZ1Ol.png"
                        },             
                        new VipSettings()
                        {
                            DisplayName = "ПРЕМИУМ",
                            Day = "2 дня",
                            Command = "o.grant user %STEAMID% ПРЕМИУМ",
                            Count = 5,
                            DropChance = 1,
                            Url = "https://imgur.com/cp7QSRi.png"
                        }
                    },
                    ContainerList = new List<string>
                    {
                        { "crate_elite" },
                        { "crate_normal_2" },
                        { "supply_drop" }
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config?.ContainerList == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfig();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Команды
        [ChatCommand("vip")]
        private void CommandVip(BasePlayer player, string command, string[] args)
        {
            VipUI(player);
        }

        [ConsoleCommand("vip")]
        private void ConsoleVip(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "give")
                {
                    var vip = config.vipSettings.FirstOrDefault(x => x.DisplayName == args.Args[1]);
                    var data = AddPlayersData(player.userID, vip.DisplayName);
                    if (data.Count >= vip.Count)
                    {
                        Server.Command(vip.Command.Replace("%STEAMID%", player.UserIDString));
                        SendReply(player, $"Вы успешно получили привилегию: {vip.DisplayName}");
                        data.Count -= vip.Count;
                        CuiHelper.DestroyUi(player, Layer);
                    }
                    else
                    {
                        return;
                    }
                }
                if (args.Args[0] == "fragments")
                {
                    if (player != null && !player.IsAdmin) return;
                    if (args.Args == null || args.Args.Length < 2)
                    {
                        player.ConsoleMessage("Команда: vip fragments SteamID количество фрагментов");
                        return;
                    }
                    BasePlayer target = BasePlayer.Find(args.Args[1]);
                    if (target == null)
                    {
                        player.ConsoleMessage($"Игрок {target} не найден");
                        return;
                    }
                    int change;
                    if (!int.TryParse(args.Args[2], out change))
                    {
                        player.ConsoleMessage("Вы не указали кол-во");
                        return;
                    }
                    player.ConsoleMessage($"Игроку {target}, были успешно выданы фрагментый.\nВ размере: {change}");
                    Item item = ItemManager.CreateByItemID(ItemManager.FindItemDefinition(config.ItemSettings.ShortName).itemid, change, config.ItemSettings.SkinID);
                    item.name = config.ItemSettings.DisplayName;
                    player.inventory.GiveItem(item);
                }
            }
        }
        #endregion

        #region Хуки
        private void OnServerInitialized()
        {
            foreach (var check in config.vipSettings)
            {
                ImageLibrary.Call("AddImage", check.Url, check.Url);
            }
        }

        private void Loaded()
        {
            vipData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, VipData>>>("VipSystem/Player");
        }
        
        private VipData AddPlayersData(ulong userID, string name)
        {
            if (!vipData.ContainsKey(userID))
                vipData[userID] = new Dictionary<string, VipData>();

            if (!vipData[userID].ContainsKey(name))
                vipData[userID][name] = new VipData();

            return vipData[userID][name];
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("VipSystem/Player", vipData);
        }
        
        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.DestroyUi(player, LayerAlert);
            }
            SaveData();
        }

        private void OnDisconnected(BasePlayer player, string reason)
        {
            SaveData();
        }

        private object OnItemAction(Item item, string action)
        {
            if (item == null || action == null || action == "")
                return null;
            if (item.skin != config.ItemSettings.SkinID)
                return null;
            if (action != "unwrap")
                return null;
            BasePlayer player = item.GetRootContainer().GetOwnerPlayer();
            if (player == null)
                return null;
            foreach (var check in config.vipSettings)
            {
                if (UnityEngine.Random.Range(0f, 100f) < check.DropChance)
                {
                    var data = AddPlayersData(player.userID, check.DisplayName);
                    if (data.Count < check.Count)
                    {
                        data.Count += 1;
                    }
                    AlertUI(player, check.DisplayName);
                }
            }
            ItemRemovalThink(item, player, 1);
            Effect.server.Run("assets/prefabs/misc/xmas/presents/effects/unwrap.prefab", player.transform.position);
            return false;
        }

        private static void ItemRemovalThink(Item item, BasePlayer player, int itemsToTake)
        {
            if (item.amount == itemsToTake)
            {
                item.RemoveFromContainer();
                item.Remove();
            }
            else
            {
                item.amount = item.amount - itemsToTake;
                player.inventory.SendSnapshot();
            }
        }

        private void OnLootSpawn(LootContainer container)
        {
            if (container == null || container?.net.ID == null) return;
            if (config.ContainerList.Contains(container.ShortPrefabName))
            {
                var item = ItemManager.CreateByName(config.ItemSettings.ShortName, 1);
                item.name = config.ItemSettings.DisplayName;
                item.skin = config.ItemSettings.SkinID;
                item.MoveToContainer(container.inventory);
                container.inventory.MarkDirty();
            }
        }

        object OnItemSplit(Item thisI, int split_Amount)
        {
            Item item = null;
            if (thisI.skin == 0uL) return null;
            if (thisI.skin == config.ItemSettings.SkinID)
            {
                thisI.amount -= split_Amount; item = ItemManager.CreateByItemID(thisI.info.itemid, split_Amount, thisI.skin);
                if (item != null)
                {
                    item.amount = split_Amount;
                    item.name = thisI.name;
                    item.OnVirginSpawn();
                    if (thisI.IsBlueprint()) item.blueprintTarget = thisI.blueprintTarget;
                    if (thisI.hasCondition) item.condition = thisI.condition;
                    item.MarkDirty();
                    return item;
                }
            }
            return null;
        }

        object CanStackItem(Item item, Item targetItem)
        {
            if (item.info.itemid == ItemManager.FindItemDefinition(config.ItemSettings.ShortName).itemid && targetItem.info.itemid == ItemManager.FindItemDefinition(config.ItemSettings.ShortName).itemid) if (item.skin == config.ItemSettings.SkinID || targetItem.skin == config.ItemSettings.SkinID) if (targetItem.skin != item.skin) return false;
            return null;
        }
        #endregion

        #region Интерфейс
        private void VipUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();
            int VipCount = config.vipSettings.Count();
            float Position = 0.2f, Width = 0.1335f, Height = 0.445f, Margin = 0.011f, MinHeight = 0.262f;
            Position = 0.5f - VipCount / 2f * Width - (VipCount - 1) / 2f * Margin;

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                Image = {Color = "0 0 0 0.9"},
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-2 -2", AnchorMax = "2 2", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0.9", Close = Layer },
                Text = { Text = "" }
            }, Layer);

            foreach (var check in config.vipSettings)
            {
                var vips = AddPlayersData(player.userID, check.DisplayName);
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{Position} {MinHeight}", AnchorMax = $"{Position + Width} {MinHeight + Height}", OffsetMin = "0 0", OffsetMax = "0 0" },
                    Image = { Color = "1 1 1 0" }
                }, Layer, "Vip");
                Position += (Width + Margin);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0.913", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0" },
                    Text = { Text = $"{check.DisplayName} {check.Day}", Align = TextAnchor.MiddleCenter, FontSize = 24, Font = "robotocondensed-regular.ttf" }
                }, "Vip");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0.16", AnchorMax = "1 0.91", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0.1" },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter, FontSize = 25, Font = "robotocondensed-regular.ttf" }
                }, "Vip", "Progress");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 {(float)vips.Count / check.Count}", OffsetMax = "0 0" },
                    Button = { Color = "0.40 0.67 0.41 0.4" },
                    Text = { Text = "" }
                }, "Progress");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0.28", AnchorMax = "1 0.91", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0" },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter, FontSize = 25, Font = "robotocondensed-regular.ttf" }
                }, "Vip", "Images");

                var color = vips.Count >= check.Count ? "1 1 1 1" : "1 1 1 0.1";
                container.Add(new CuiElement
                {
                    Parent = "Images",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.Url), Color = color, FadeIn = 0.5f },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "1 9", OffsetMax = "-1 -9" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0.16", AnchorMax = "1 0.28", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0" },
                    Text = { Text = $"<b>Собрано:</b>\n<size=19>{vips.Count} ИЗ {check.Count}</size>", Color = "1 1 1 0.5", Align = TextAnchor.UpperCenter, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                }, "Vip");

                var text = vips.Count >= check.Count ? "ПОЛУЧИТЬ" : "НЕДОСТУПНО";
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.133", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0.1", Command = $"vip give {check.DisplayName}" },
                    Text = { Text = $"<color=#FFFFFF5A>{text}</color>", Align = TextAnchor.MiddleCenter, FontSize = 24, Font = "robotocondensed-bold.ttf" }
                }, "Vip");
            }

            CuiHelper.AddUi(player, container);
        }

        private void AlertUI(BasePlayer player, string name)
        {
            CuiHelper.DestroyUi(player, LayerAlert);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 180", OffsetMax = "400 280" },
                Image = { Color = "0 0 0 0" },
            }, "Hud", LayerAlert);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = $"<color=#FFFFFF9A><size=20><b>ФРАГМЕНТЫ</b></size>\nВы получили фрагмент: <b>{name.ToUpper()}</b></color>\nОткрыть меню с <b>привилегиями</b> можно прописав команду <b>/vip</b>", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
            }, LayerAlert);

            CuiHelper.AddUi(player, container);
            timer.Once(config.ItemSettings.AlertTime, () => { CuiHelper.DestroyUi(player, LayerAlert); });
        }
        #endregion
    }
}