// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using Oxide.Core;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("MachiningTools", "Vlad-00003", "1.1.2", ResourceId = 89)]
    [Description("Creates tools that would gather refined materials from the resource nodes")]	
    /*
     * Author info:
     *   E-mail: Vlad-00003@mail.ru
     *   Vk: vk.com/vlad_00003
     * v1.1.2
     *  Исправлена ошибка, из-за которой не происходила переработка серы при получении бонуса за полное разрушение жилы.
     *  В стандартную конфигурацию выведены бензопила и отбойный молоток, как примеры.
     */
    class MachiningTools : RustPlugin
    {
        #region Vars
        private Dictionary<uint, SavedData> Tools;
        private PluginConfig config;
        private Dictionary<ItemDefinition, ItemDefinition> Transmutations;
        private readonly List<string> Transmutatable = new List<string>()
        {
            "chicken.raw",
            "humanmeat.raw",
            "bearmeat",
            "deermeat.raw",
            "meat.boar",
            "wolfmeat.raw",
            "hq.metal.ore",
            "metal.ore",
            "sulfur.ore",
            "horsemeat.raw"
        };
        #endregion

        #region Data handling
        private class SavedData
        {
            public Transmutetion Transmutation;
            public bool CanRepair;
            public bool CanRecycle;
        }
        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Title, Tools);
        }
        void LoadData()
        {
            try
            {
                Tools = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<uint, SavedData>>(Title);
            }
            catch (Exception ex)
            {
                PrintError($"Failed to load cupboard data file (is the file corrupt?) ({ex.Message})");
                Tools = new Dictionary<uint, SavedData>();
            }
        }
        #endregion

        #region Config
        private class Tool
        {
            [JsonProperty("Короткое имя предмета")]
            public string Item;
            [JsonProperty("ID скина предмета (Поддерживается Workshop)")]
            public ulong SkinId;
            [JsonProperty("Название предмета (Выводится в описании предмета в инвентаре)")]
            public string Name;
            [JsonProperty("Можно ли ремонтировать предмет")]
            public bool CanRepair;
            [JsonProperty("Можно ли перерабатывать пердмет")]
            public bool CanRecycle;
            [JsonProperty("Настройки переработки")]
            public Transmutetion Transmutation;
        }
        private class Transmutetion
        {
            [JsonProperty("Перерабатывать дерево в уголь")]
            public bool Wood;
            [JsonProperty("Перерабатывать руду МВК в металл")]
            public bool Hqm;
            [JsonProperty("Перерабатывать металлическую руду в фрагменты")]
            public bool Metal;
            [JsonProperty("Перерабатывать серную руду в серу")]
            public bool Sulfur;
            [JsonProperty("Перерабатывать мясо медведя в жаренное")]
            public bool Bear;
            [JsonProperty("Перерабатывать свинину в жаренную")]
            public bool Boar;
            [JsonProperty("Перерабатывать мясо курицы в жаренное")]
            public bool Chicken;
            [JsonProperty("Перерабатывать мясо лошади в жаренное")]
            public bool Horse;
            [JsonProperty("Перерабатывать мясо волка в жаренное")]
            public bool Wolf;
            [JsonProperty("Перерабатывать мясо оленя в жаренное")]
            public bool Deer;
            [JsonProperty("Перерабатывать человеческое мясо в жаренное")]
            public bool Human;
            public static Transmutetion DefaultPick()
            {
                return new Transmutetion()
                {
                    Wood = false,
                    Hqm = true,
                    Metal = true,
                    Sulfur = true,
                    Bear = false,
                    Boar = false,
                    Chicken = false,
                    Wolf = false,
                    Deer = false,
                    Human = false,
                    Horse = false
                };
            }
            public static Transmutetion DefaultAxe()
            {
                return new Transmutetion()
                {
                    Wood = true,
                    Hqm = false,
                    Metal = false,
                    Sulfur = false,
                    Bear = true,
                    Boar = true,
                    Chicken = true,
                    Wolf = true,
                    Deer = true,
                    Human = true,
                    Horse = true
                };
            }
        }
        private class PluginConfig
        {
            [JsonProperty("Привилегия для использования команд")]
            public string Permission;
            [JsonProperty("Команда(чат/консоль)")]
            public string Command;
            [JsonProperty("Список инструментов")]
            public Dictionary<string, Tool> Tools;
            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    Permission = "machiningtools.use",
                    Command = "givetool",
                    Tools = new Dictionary<string, Tool>()
                    {
                        ["hatchet"] = new Tool()
                        {
                            Item = "hatchet",
                            Name = "Магический топор",
                            CanRepair = true,
                            CanRecycle = true,
                            SkinId = 901876821,
                            Transmutation = Transmutetion.DefaultAxe()
                        },
                        ["pickaxe"] = new Tool()
                        {
                            Item = "pickaxe",
                            Name = "Магическая кирка",
                            CanRepair = true,
                            CanRecycle = true,
                            SkinId = 902892485,
                            Transmutation = Transmutetion.DefaultPick()
                        },
                        ["icepick"] = new Tool()
                        {
                            Item = "icepick.salvaged",
                            Name = "Магический ледоруб",
                            CanRepair = false,
                            CanRecycle = false,
                            SkinId = 804307574,
                            Transmutation = Transmutetion.DefaultPick()
                        },
                        ["axe"] = new Tool()
                        {
                            Item = "axe.salvaged",
                            Name = "Магический топор",
                            CanRepair = false,
                            CanRecycle = false,
                            SkinId = 0,
                            Transmutation = Transmutetion.DefaultAxe()
                        },
                        ["chainsaw"] = new Tool()
                        {
                            Item = "chainsaw",
                            Name = "Магическая бензопила",
                            CanRepair = false,
                            CanRecycle = false,
                            SkinId = 0,
                            Transmutation = Transmutetion.DefaultAxe()
                        },
                        ["jackhammer"] = new Tool()
                        {
                            Item = "jackhammer",
                            Name = "Магичесий отбойный молоток",
                            CanRepair = false,
                            CanRecycle = false,
                            SkinId = 0,
                            Transmutation = Transmutetion.DefaultPick()
                        }
                    }
                };
            }
        }
        #endregion

        #region Config handling

        #region Config checker

        private void CheckConfig()
        {
            bool changed = false;
            foreach (var tool in config.Tools)
            {
                //compability with 1.0.9
                if (string.IsNullOrEmpty(tool.Value.Name))
                {
                    tool.Value.Name = "Unnamed";
                    changed = true;
                }
            }
            if (changed)
                SaveConfig();

        }

        #endregion
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за приобритение плагина на сайте RustPlugin.ru. Если вы приобрели этот плагин на другом ресурсе знайте - это лишает вас гарантированных обновлений!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        #endregion

        #region Init and quiting

        void Init()
        {
            LoadData();
            AddCovalenceCommand(config.Command, "GiveToolsCommand", config.Permission);
        }

        void OnServerInitialized()
        {
            Transmutations = ItemManager.GetItemDefinitions().Where(p => Transmutatable.Contains(p.shortname))
                .ToDictionary(p => p, p => p.GetComponent<ItemModCookable>()?.becomeOnCooked);
            ItemDefinition wood = ItemManager.FindItemDefinition("wood");
            ItemDefinition charcoal = ItemManager.FindItemDefinition("charcoal");
            Transmutations.Add(wood, charcoal);
            var keys = Transmutations.Select(p => p.Key).ToList();
            foreach (var key in keys)
            {
                if (Transmutations[key] != null) continue;
                PrintError($"Не удалось получить ItemModCookable для \"{key.displayName.english}\"\nСообщите об этом разработчику: https://vk.com/vlad_00003");
                Transmutations.Remove(key);
            }
        }
        void Unload() => SaveData();
        void OnServerSave() => SaveData();
        #endregion

        #region Oxide Hooks
        void OnNewSave()
        {
            Tools.Clear();
            PrintWarning("Обнаружен вайп. Инструменты сброшены.");
            SaveData();
        }
        void OnItemRemove(Item item, ulong playerid = 104448)
        {
            if (Tools.ContainsKey(item.uid))
                Tools.Remove(item.uid);
        }
        object OnItemRepair(BasePlayer player, Item item)
        {
            var entity = item?.uid;
            if (entity.HasValue)
            {
                SavedData data;
                if (Tools.TryGetValue(entity.Value, out data))
                {
                    if (!data.CanRepair)
                    {
                        player.ChatMessage(GetMsg("Can't repair", player.userID));
                        return false;
                    }
                }
            }
            return null;
        }
        object CanRecycle(Recycler recycler, Item item)
        {
            var entity = item?.uid;
            if (!entity.HasValue) return null;

            SavedData data;
            if (!Tools.TryGetValue(entity.Value, out data)) return null;

            if (!data.CanRecycle)
                return false;

            return null;
        }
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            Item weapon = entity.ToPlayer()?.GetActiveItem();
            if (weapon == null) return;

            SavedData data;
            if (!Tools.TryGetValue(weapon.uid, out data)) return;

            switch (item.info.shortname)
            {
                case "stones":
                    break;
                case "leather":
                    break;
                case "bone.fragments":
                    break;
                case "fat.animal":
                    break;
                case "skull.wolf":
                    break;
                case "cloth":
                    break;
                case "cactusflesh":
                    break;
                case "skull.human":
                    break;
                case "humanmeat.raw":
                    if (data.Transmutation.Human)
                        Transmute(item);
                    break;
                case "bearmeat":
                    if (data.Transmutation.Bear)
                        Transmute(item);
                    break;
                case "chicken.raw":
                    if (data.Transmutation.Chicken)
                        Transmute(item);
                    break;
                case "meat.boar":
                    if (data.Transmutation.Boar)
                        Transmute(item);
                    break;
                case "deermeat.raw":
                    if (data.Transmutation.Deer)
                        Transmute(item);
                    break;
                case "wolfmeat.raw":
                    if (data.Transmutation.Wolf)
                        Transmute(item);
                    break;
                case "sulfur.ore":
                    if (data.Transmutation.Sulfur)
                        Transmute(item);
                    break;
                case "metal.ore":
                    if (data.Transmutation.Metal)
                        Transmute(item);
                    break;
                case "wood":
                    if (data.Transmutation.Wood)
                        Transmute(item);
                    break;
                case "horsemeat.raw":
                    if (data.Transmutation.Horse)
                        Transmute(item);
                    break;
                default:
                    Puts($"Игрок добыл неизвестный предмет - {item.info.shortname}!\nСообщите об этом разработчику: https://vk.com/vlad_00003");
                    break;
            }
        }
        void OnDispenserBonus(ResourceDispenser disp, BasePlayer player, Item item)
        {
            if (player == null)
            {
                Puts("Финальный бонус был присвоен без игрока!\nСообщите об этом разработчику: https://vk.com/vlad_00003");
                return;
            }
            var weapon = player.GetActiveItem();
            if (weapon == null)
            {
                Puts("Игрок получил бонус за завершение добычи без использования оружия!\nСообщите об этом разработчику: https://vk.com/vlad_00003");
                return;
            }
            SavedData data;
            if (!Tools.TryGetValue(weapon.uid, out data)) return;
            switch (item.info.shortname)
            {
                case "sulfur.ore":
                    if (data.Transmutation.Sulfur)
                        Transmute(item);
                    break;
                case "metal.ore":
                    if (data.Transmutation.Metal)
                        Transmute(item);
                    break;
                case "hq.metal.ore":
                    if (data.Transmutation.Hqm)
                        Transmute(item);
                    break;
                case "stones":
                    break;
                case "wood":
                    if (data.Transmutation.Wood)
                        Transmute(item);
                    break;
                default:
                    Puts($"Игроку присвоен неизвестный предмет - {item.info.shortname}!\nСообщите об этом разработчику: https://vk.com/vlad_00003");
                    break;
            }
        }
        #endregion

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["Syntax"] = "Incorrect synax! Use: {0} player item [item2] [item3] ...",
                ["No item"] = "Item \"{0}\" could not found in the tool list and can't be given!",
                ["No player"] = "Player \"{0}\" could not be found",
                ["Not on server"] = "Player \"{0}\" is not on the server",
                ["Multiply players"] = "Found multiply players:\n{0}",
                ["Successfull"] = "Successfully gave player \"{0}\" tools:\n{1}",
                ["Can't repair"] = "You can not repair this tool!"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["Syntax"] = "Неверный синтаксис! Используйте: {0} player item [item2] [item3] ...",
                ["No item"] = "Предмет \"{0}\" не найден в списке инструментов и не может быть выдан!",
                ["No player"] = "Игрок \"{0}\" не найден",
                ["Not on server"] = "Игрок \"{0}\" не находится на сервере",
                ["Multiply players"] = "Найдено несколько игроков:\n{0}",
                ["Successfull"] = "Успешно выдали игроку \"{0}\" предметы:\n{1}",
                ["Can't repair"] = "Данный предмет не подлежит ремонту!"
            }, this, "ru");
        }
        private string GetMsg(string langkey, object userId = null) => lang.GetMessage(langkey, this, userId?.ToString());
        #endregion

        #region Command
        private void GiveToolsCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length < 2)
            {
                Reply(player, "Syntax", config.Command);
                return;
            }
            var receivers = GetPlayers(args[0]);
            if (receivers == null || receivers.Count == 0)
            {
                Reply(player, "No player", args[0]);
                return;
            }
            if (receivers.Count > 1)
            {
                Reply(player, "Multiply players", string.Join("\n", receivers.Select(p => $"{p.Value} ({p.Key})").ToArray()));
                return;
            }
            var ireceiver = receivers.First();
            var receiver = FindBasePlayer(ireceiver.Key);
            if (receiver == null)
            {
                Reply(player, "Not on server", ireceiver.Value);
                return;
            }
            var tools = args.ToList();
            tools.RemoveAt(0);
            var toolCheck = tools.Where(t => !config.Tools.ContainsKey(t)).ToList();
            foreach (var mistake in toolCheck)
            {
                Reply(player, "No item", mistake);
            }
            if (toolCheck.Any()) return;
            foreach (var tool in tools)
            {
                var data = config.Tools[tool];
                Item item = ItemManager.CreateByName(data.Item, 1, data.SkinId);
                item.name = config.Tools[tool].Name;
                receiver.GiveItem(item);
                uint id = item.uid;
                Tools.Add(id, new SavedData()
                {
                    CanRepair = data.CanRepair,
                    Transmutation = data.Transmutation,
                    CanRecycle = data.CanRecycle
                });
            }
            Reply(player, "Successfull", ireceiver.Value, string.Join("\n", tools.ToArray()));
        }
        #endregion

        #region API
        object IsMachiningToolItem(Item item)
        {
            if (item == null)
                return null;
            return Tools.ContainsKey(item.uid);
        }
        #endregion

        #region Helpers
        private void Transmute(Item item)
        {
            if (!Transmutations.ContainsKey(item.info))
            {
                PrintWarning($"Неизвестный предмет отправлен на переплавку - {item.info.displayName.english}!\nСообщите об этом разработчику: https://vk.com/vlad_00003");
                return;
            }
            item.info = Transmutations[item.info];
        }
        private void Reply(IPlayer player, string langkey, params object[] args)
        {
            player.Reply(string.Format(GetMsg(langkey, player.Id), args));
        }
        private Dictionary<ulong, string> GetPlayers(string nameOrId)
        {
            var pl = covalence.Players.FindPlayers(nameOrId).ToList();
            return pl.Select(p => new KeyValuePair<ulong, string>(ulong.Parse(p.Id), p.Name)).ToDictionary(x => x.Key, x => x.Value);
        }
        private BasePlayer FindBasePlayer(ulong userId)
        {
            BasePlayer player = BasePlayer.activePlayerList.FirstOrDefault(p => p.userID == userId);
            player = player ?? BasePlayer.sleepingPlayerList.FirstOrDefault(p => p.userID == userId);
            return player;
        }
        #endregion
    }
}
///////////////////////////////////////////////////////////
