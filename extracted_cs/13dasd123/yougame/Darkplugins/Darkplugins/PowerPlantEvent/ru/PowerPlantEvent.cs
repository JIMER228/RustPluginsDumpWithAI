// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Plugins.PowerPlantEventExtensionMethods;

namespace Oxide.Plugins
{
    [Info("PowerPlantEvent", "Sempai#3239", "2.0.2")]
    internal class PowerPlantEvent : RustPlugin
    {
        #region Config
        private const bool En = false;

        private PluginConfig _config;

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a default config...");
            _config = PluginConfig.DefaultConfig();
            _config.PluginVersion = Version;
            SaveConfig();
            Puts("Creation of the default config completed!");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
            if (_config.PluginVersion < Version) UpdateConfigValues();
        }

        private void UpdateConfigValues()
        {
            Puts("Config update detected! Updating config values...");
            _config.PluginVersion = Version;
            Puts("Config update completed!");
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        public class ItemConfig
        {
            [JsonProperty("ShortName")] public string ShortName { get; set; }
            [JsonProperty(En ? "Minimum" : "Минимальное кол-во")] public int MinAmount { get; set; }
            [JsonProperty(En ? "Maximum" : "Максимальное кол-во")] public int MaxAmount { get; set; }
            [JsonProperty(En ? "Chance [0.0-100.0]" : "Шанс выпадения предмета [0.0-100.0]")] public float Chance { get; set; }
            [JsonProperty(En ? "Is this a blueprint? [true/false]" : "Это чертеж? [true/false]")] public bool IsBluePrint { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong SkinID { get; set; }
            [JsonProperty(En ? "Name (empty - default)" : "Название (empty - default)")] public string Name { get; set; }
        }

        public class LootTableConfig
        {
            [JsonProperty(En ? "Minimum numbers of items" : "Минимальное кол-во элементов")] public int Min { get; set; }
            [JsonProperty(En ? "Maximum numbers of items" : "Максимальное кол-во элементов")] public int Max { get; set; }
            [JsonProperty(En ? "Use minimum and maximum values? [true/false]" : "Использовать минимальное и максимальное значение? [true/false]")] public bool UseCount { get; set; }
            [JsonProperty(En ? "List of items" : "Список предметов")] public List<ItemConfig> Items { get; set; }
        }

        public class PrefabConfig
        {
            [JsonProperty(En ? "Chance [0.0-100.0]" : "Шанс выпадения [0.0-100.0]")] public float Chance { get; set; }
            [JsonProperty(En ? "The path to the prefab" : "Путь к prefab-у")] public string PrefabDefinition { get; set; }
        }

        public class PrefabLootTableConfig
        {
            [JsonProperty(En ? "Minimum numbers of prefabs" : "Минимальное кол-во prefab-ов")] public int Min { get; set; }
            [JsonProperty(En ? "Maximum numbers of prefabs" : "Максимальное кол-во prefab-ов")] public int Max { get; set; }
            [JsonProperty(En ? "Use minimum and maximum values? [true/false]" : "Использовать минимальное и максимальное значение? [true/false]")] public bool UseCount { get; set; }
            [JsonProperty(En ? "List of prefabs" : "Список prefab-ов")] public List<PrefabConfig> Prefabs { get; set; }
        }

        public class CrateConfig
        {
            [JsonProperty("Prefab")] public string Prefab { get; set; }
            [JsonProperty(En ? "Position" : "Позиция")] public string Position { get; set; }
            [JsonProperty(En ? "Rotation" : "Вращение")] public string Rotation { get; set; }
            [JsonProperty(En ? "Loot table from prefabs (if the loot table type is 4 or 5)" : "Таблица предметов из prefab-ов (если тип таблицы предметов - 4 или 5)")] public PrefabLootTableConfig PrefabLootTable { get; set; }
            [JsonProperty(En ? "Own loot table (if the loot table type is 1 or 5)" : "Собственная таблица предметов (если тип таблицы предметов - 1 или 5)")] public LootTableConfig OwnLootTable { get; set; }
        }

        public class HackCrateConfig
        {
            [JsonProperty(En ? "Location of all Crates" : "Расположение всех ящиков")] public HashSet<CoordConfig> Coordinates { get; set; }
            [JsonProperty(En ? "Time to unlock the Crates [sec.]" : "Время разблокировки ящиков [sec.]")] public float UnlockTime { get; set; }
            [JsonProperty(En ? "Increase the event time if it's not enough to unlock the locked crate? [true/false]" : "Увеличивать время ивента, если недостаточно чтобы разблокировать заблокированный ящик? [true/false]")] public bool IncreaseEventTime { get; set; }
            [JsonProperty(En ? "Which loot table should the plugin use? (0 - default; 1 - own; 2 - AlphaLoot; 3 - CustomLoot; 4 - loot table of the Rust objects; 5 - combine the 1 and 4 methods)" : "Какую таблицу лута необходимо использовать? (0 - стандартную; 1 - собственную; 2 - AlphaLoot; 3 - CustomLoot; 4 - таблица предметов объектов Rust; 5 - совместить 1 и 4 методы)")] public int TypeLootTable { get; set; }
            [JsonProperty(En ? "Loot table from prefabs (if the loot table type is 4 or 5)" : "Таблица предметов из prefab-ов (если тип таблицы предметов - 4 или 5)")] public PrefabLootTableConfig PrefabLootTable { get; set; }
            [JsonProperty(En ? "Own loot table (if the loot table type is 1 or 5)" : "Собственная таблица предметов (если тип таблицы предметов - 1 или 5)")] public LootTableConfig OwnLootTable { get; set; }
        }

        public class ColorConfig
        {
            [JsonProperty("r")] public float R { get; set; }
            [JsonProperty("g")] public float G { get; set; }
            [JsonProperty("b")] public float B { get; set; }
        }

        public class MarkerConfig
        {
            [JsonProperty(En ? "Name" : "Название")] public string Name { get; set; }
            [JsonProperty(En ? "Radius" : "Радиус")] public float Radius { get; set; }
            [JsonProperty(En ? "Alpha" : "Прозрачность")] public float Alpha { get; set; }
            [JsonProperty(En ? "Marker color" : "Цвет маркера")] public ColorConfig Color { get; set; }
        }

        public class GuiConfig
        {
            [JsonProperty(En ? "Do you use the countdown GUI? [true/false]" : "Использовать ли GUI обратного отсчета? [true/false]")] public bool IsGui { get; set; }
            [JsonProperty("AnchorMin")] public string AnchorMin { get; set; }
            [JsonProperty("AnchorMax")] public string AnchorMax { get; set; }
        }

        public class GuiAnnouncementsConfig
        {
            [JsonProperty(En ? "Do you use the GUI Announcements? [true/false]" : "Использовать ли GUI Announcements? [true/false]")] public bool IsGuiAnnouncements { get; set; }
            [JsonProperty(En ? "Banner color" : "Цвет баннера")] public string BannerColor { get; set; }
            [JsonProperty(En ? "Text color" : "Цвет текста")] public string TextColor { get; set; }
            [JsonProperty(En ? "Adjust Vertical Position" : "Отступ от верхнего края")] public float ApiAdjustVPosition { get; set; }
        }

        public class NotifyConfig
        {
            [JsonProperty(En ? "Do you use the Notify? [true/false]" : "Использовать ли Notify? [true/false]")] public bool IsNotify { get; set; }
            [JsonProperty(En ? "Type" : "Тип")] public string Type { get; set; }
        }

        public class DiscordConfig
        {
            [JsonProperty(En ? "Do you use the Discord? [true/false]" : "Использовать ли Discord? [true/false]")] public bool IsDiscord { get; set; }
            [JsonProperty("Webhook URL")] public string WebhookUrl { get; set; }
            [JsonProperty(En ? "Embed Color (DECIMAL)" : "Цвет полосы (DECIMAL)")] public int EmbedColor { get; set; }
            [JsonProperty(En ? "Keys of required messages" : "Ключи необходимых сообщений")] public HashSet<string> Keys { get; set; }
        }

        public class ScaleDamageConfig
        {
            [JsonProperty(En ? "Type of target" : "Тип цели")] public string Type { get; set; }
            [JsonProperty(En ? "Damage Multiplier" : "Множитель урона")] public float Scale { get; set; }
        }

        public class PveModeConfig
        {
            [JsonProperty(En ? "Use the PVE mode of the plugin? [true/false]" : "Использовать PVE режим работы плагина? [true/false]")] public bool Pve { get; set; }
            [JsonProperty(En ? "The amount of damage that the player has to do to become the Event Owner" : "Кол-во урона, которое должен нанести игрок, чтобы стать владельцем ивента")] public float Damage { get; set; }
            [JsonProperty(En ? "Damage coefficients for calculate to become the Event Owner" : "Коэффициенты урона для подсчета, чтобы стать владельцем события")] public HashSet<ScaleDamageConfig> ScaleDamage { get; set; }
            [JsonProperty(En ? "Can the non-owner of the event loot the crates? [true/false]" : "Может ли не владелец ивента грабить ящики? [true/false]")] public bool LootCrate { get; set; }
            [JsonProperty(En ? "Can the non-owner of the event hack locked crates? [true/false]" : "Может ли не владелец ивента взламывать заблокированные ящики? [true/false]")] public bool HackCrate { get; set; }
            [JsonProperty(En ? "Can the non-owner of the event loot NPC corpses? [true/false]" : "Может ли не владелец ивента грабить трупы NPC? [true/false]")] public bool LootNpc { get; set; }
            [JsonProperty(En ? "Can the non-owner of the event deal damage to the NPC? [true/false]" : "Может ли не владелец ивента наносить урон по NPC? [true/false]")] public bool DamageNpc { get; set; }
            [JsonProperty(En ? "Can an Npc attack a non-owner of the event? [true/false]" : "Может ли Npc атаковать не владельца ивента? [true/false]")] public bool TargetNpc { get; set; }
            [JsonProperty(En ? "Allow the non-owner of the event to enter the event zone? [true/false]" : "Разрешать входить внутрь зоны ивента не владельцу ивента? [true/false]")] public bool CanEnter { get; set; }
            [JsonProperty(En ? "Allow a player who has an active cooldown of the Event Owner to enter the event zone? [true/false]" : "Разрешать входить внутрь зоны ивента игроку, у которого активен кулдаун на получение статуса владельца ивента? [true/false]")] public bool CanEnterCooldownPlayer { get; set; }
            [JsonProperty(En ? "The time that the Event Owner may not be inside the event zone [sec.]" : "Время, которое владелец ивента может не находиться внутри зоны ивента [сек.]")] public int TimeExitOwner { get; set; }
            [JsonProperty(En ? "The time until the end of Event Owner status when it is necessary to warn the player [sec.]" : "Время таймера до окончания действия статуса владельца ивента, когда необходимо предупредить игрока [сек.]")] public int AlertTime { get; set; }
            [JsonProperty(En ? "Prevent the actions of the RestoreUponDeath plugin in the event zone? [true/false]" : "Запрещать работу плагина RestoreUponDeath в зоне действия ивента? [true/false]")] public bool RestoreUponDeath { get; set; }
            [JsonProperty(En ? "The time that the player can`t become the Event Owner, after the end of the event and the player was its owner [sec.]" : "Время, которое игрок не сможет стать владельцем ивента, после того как ивент окончен и игрок был его владельцем [sec.]")] public double CooldownOwner { get; set; }
            [JsonProperty(En ? "Darkening the dome (0 - disables the dome)" : "Затемнение купола (0 - отключает купол)")] public int Darkening { get; set; }
        }

        public class NpcBelt
        {
            [JsonProperty("ShortName")] public string ShortName { get; set; }
            [JsonProperty(En ? "Amount" : "Кол-во")] public int Amount { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong SkinID { get; set; }
            [JsonProperty(En ? "Mods" : "Модификации на оружие")] public HashSet<string> Mods { get; set; }
        }

        public class NpcWear
        {
            [JsonProperty("ShortName")] public string ShortName { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong SkinID { get; set; }
        }

        public class PresetConfig
        {
            [JsonProperty(En ? "Minimum" : "Минимальное кол-во")] public int Min { get; set; }
            [JsonProperty(En ? "Maximum" : "Максимальное кол-во")] public int Max { get; set; }
            [JsonProperty(En ? "List of locations" : "Список расположений")] public List<string> Positions { get; set; }
            [JsonProperty(En ? "NPCs setting" : "Настройки NPC")] public NpcConfig Config { get; set; }
            [JsonProperty(En ? "Which loot table should the plugin use? (0 - default; 1 - own; 2 - AlphaLoot; 3 - CustomLoot; 4 - loot table of the Rust objects; 5 - combine the 1 and 4 methods)" : "Какую таблицу предметов необходимо использовать? (0 - стандартную; 1 - собственную; 2 - AlphaLoot; 3 - CustomLoot; 4 - таблица предметов объектов Rust; 5 - совместить 1 и 4 методы)")] public int TypeLootTable { get; set; }
            [JsonProperty(En ? "Loot table from prefabs (if the loot table type is 4 or 5)" : "Таблица предметов из prefab-ов (если тип таблицы предметов - 4 или 5)")] public PrefabLootTableConfig PrefabLootTable { get; set; }
            [JsonProperty(En ? "Own loot table (if the loot table type is 1 or 5)" : "Собственная таблица предметов (если тип таблицы предметов - 1 или 5)")] public LootTableConfig OwnLootTable { get; set; }
        }

        public class NpcConfig
        {
            [JsonProperty(En ? "Name" : "Название")] public string Name { get; set; }
            [JsonProperty(En ? "Health" : "Кол-во ХП")] public float Health { get; set; }
            [JsonProperty(En ? "Roam Range" : "Дальность патрулирования местности")] public float RoamRange { get; set; }
            [JsonProperty(En ? "Chase Range" : "Дальность погони за целью")] public float ChaseRange { get; set; }
            [JsonProperty(En ? "Attack Range Multiplier" : "Множитель радиуса атаки")] public float AttackRangeMultiplier { get; set; }
            [JsonProperty(En ? "Sense Range" : "Радиус обнаружения цели")] public float SenseRange { get; set; }
            [JsonProperty(En ? "Target Memory Duration [sec.]" : "Длительность памяти цели [sec.]")] public float MemoryDuration { get; set; }
            [JsonProperty(En ? "Scale damage" : "Множитель урона")] public float DamageScale { get; set; }
            [JsonProperty(En ? "Aim Cone Scale" : "Множитель разброса")] public float AimConeScale { get; set; }
            [JsonProperty(En ? "Detect the target only in the NPC's viewing vision cone? [true/false]" : "Обнаруживать цель только в углу обзора NPC? [true/false]")] public bool CheckVisionCone { get; set; }
            [JsonProperty(En ? "Vision Cone" : "Угол обзора")] public float VisionCone { get; set; }
            [JsonProperty(En ? "Speed" : "Скорость")] public float Speed { get; set; }
            [JsonProperty(En ? "Disable radio effects? [true/false]" : "Отключать эффекты рации? [true/false]")] public bool DisableRadio { get; set; }
            [JsonProperty(En ? "Is this a stationary NPC? [true/false]" : "Это стационарный NPC? [true/false]")] public bool Stationary { get; set; }
            [JsonProperty(En ? "Remove a corpse after death? (it is recommended to use the true value to improve performance) [true/false]" : "Удалять труп после смерти? (рекомендуется использовать значение true для повышения производительности) [true/false]")] public bool IsRemoveCorpse { get; set; }
            [JsonProperty(En ? "Wear items" : "Одежда")] public HashSet<NpcWear> WearItems { get; set; }
            [JsonProperty(En ? "Belt items" : "Быстрые слоты")] public HashSet<NpcBelt> BeltItems { get; set; }
            [JsonProperty(En ? "Kit (it is recommended to use the previous 2 settings to improve performance)" : "Kit (рекомендуется использовать предыдущие 2 пункта настройки для повышения производительности)")] public string Kit { get; set; }
        }

        public class EconomyConfig
        {
            [JsonProperty(En ? "Which economy plugins do you want to use? (Economics, Server Rewards, IQEconomic)" : "Какие плагины экономики вы хотите использовать? (Economics, Server Rewards, IQEconomic)")] public HashSet<string> Plugins { get; set; }
            [JsonProperty(En ? "The minimum value that a player must collect to get points for the economy" : "Минимальное значение, которое игрок должен заработать, чтобы получить баллы за экономику")] public double Min { get; set; }
            [JsonProperty(En ? "Looting of crates" : "Ограбление ящиков")] public Dictionary<string, double> Crates { get; set; }
            [JsonProperty(En ? "Killing an NPC" : "Убийство NPC")] public double Npc { get; set; }
            [JsonProperty(En ? "Hacking a locked crate" : "Взлом заблокированного ящика")] public double LockedCrate { get; set; }
            [JsonProperty(En ? "Pressing the button" : "Нажатие кнопки")] public double Button { get; set; }
            [JsonProperty(En ? "List of commands that are executed in the console at the end of the event ({steamid} - the player who collected the highest number of points)" : "Список команд, которые выполняются в консоли по окончанию ивента ({steamid} - игрок, который набрал наибольшее кол-во баллов)")] public HashSet<string> Commands { get; set; }
        }

        public class CoordConfig
        {
            [JsonProperty(En ? "Position" : "Позиция")] public string Position { get; set; }
            [JsonProperty(En ? "Rotation" : "Вращение")] public string Rotation { get; set; }
        }

        private class PluginConfig
        {
            [JsonProperty(En ? "Minimum time between events [sec.]" : "Минимальное время между ивентами [sec.]")] public float MinStartTime { get; set; }
            [JsonProperty(En ? "Maximum time between events [sec.]" : "Максимальное время между ивентами [sec.]")] public float MaxStartTime { get; set; }
            [JsonProperty(En ? "Is active the timer on to start the event? [true/false]" : "Активен ли таймер для запуска ивента? [true/false]")] public bool EnabledTimer { get; set; }
            [JsonProperty(En ? "Duration of the event [sec.]" : "Время проведения ивента [sec.]")] public int FinishTime { get; set; }
            [JsonProperty(En ? "Time before the starting of the event after receiving a chat message [sec.]" : "Время до начала ивента после сообщения в чате [sec.]")] public float PreStartTime { get; set; }
            [JsonProperty(En ? "Notification time until the end of the event [sec.]" : "Время оповещения до окончания ивента [sec.]")] public int PreFinishTime { get; set; }
            [JsonProperty(En ? "Which loot table should the plugin use in the crates? (0 - default; 1 - own; 2 - AlphaLoot; 3 - CustomLoot; 4 - loot table of the Rust objects; 5 - combine the 1 and 4 methods)" : "Какую таблицу лута необходимо использовать в ящиках? (0 - стандартную; 1 - собственную; 2 - AlphaLoot; 3 - CustomLoot; 4 - таблица предметов объектов Rust; 5 - совместить 1 и 4 методы)")] public int TypeLootTableCrates { get; set; }
            [JsonProperty(En ? "Crates setting" : "Настройка ящиков")] public HashSet<CrateConfig> DefaultCrates { get; set; }
            [JsonProperty(En ? "Locked Crates setting" : "Настройка заблокированных ящиков")] public HackCrateConfig HackCrate { get; set; }
            [JsonProperty(En ? "Marker configuration on the map" : "Настройка маркера на карте")] public MarkerConfig Marker { get; set; }
            [JsonProperty(En ? "Prefix of chat messages" : "Префикс сообщений в чате")] public string Prefix { get; set; }
            [JsonProperty(En ? "Do you use the chat? [true/false]" : "Использовать ли чат? [true/false]")] public bool IsChat { get; set; }
            [JsonProperty(En ? "GUI setting" : "Настройки GUI")] public GuiConfig Gui { get; set; }
            [JsonProperty(En ? "GUI Announcements setting" : "Настройка GUI Announcements")] public GuiAnnouncementsConfig GuiAnnouncements { get; set; }
            [JsonProperty(En ? "Notify setting" : "Настройка Notify")] public NotifyConfig Notify { get; set; }
            [JsonProperty(En ? "Discord setting (only for users DiscordMessages plugin)" : "Настройка оповещений в Discord (только для тех, кто использует плагин DiscordMessages)")] public DiscordConfig Discord { get; set; }
            [JsonProperty(En ? "Do you create a PVP zone in the event area? (only for users TruePVE plugin) [true/false]" : "Создавать зону PVP в зоне проведения ивента? (только для тех, кто использует плагин TruePVE) [true/false]")] public bool IsCreateZonePvp { get; set; }
            [JsonProperty(En ? "PVE Mode Setting (only for users PveMode plugin)" : "Настройка PVE режима работы плагина (только для тех, кто использует плагин PveMode)")] public PveModeConfig PveMode { get; set; }
            [JsonProperty(En ? "Interrupt the teleport in Power Plant? (only for users NTeleportation plugin) [true/false]" : "Запрещать телепорт на электростанции? (только для тех, кто использует плагин NTeleportation) [true/false]")] public bool NTeleportationInterrupt { get; set; }
            [JsonProperty(En ? "Disable NPCs from the BetterNpc plugin on the monument while the event is on? [true/false]" : "Отключать NPC из плагина BetterNpc на монументе пока проходит ивент? [true/false]")] public bool RemoveBetterNpc { get; set; }
            [JsonProperty(En ? "NPCs setting at the beginning of the event" : "Настройка NPC в начале ивента")] public HashSet<PresetConfig> NpcStart { get; set; }
            [JsonProperty(En ? "NPCs setting when the train arrives" : "Настройка NPC, когда приезжает поезд")] public HashSet<PresetConfig> NpcTrain { get; set; }
            [JsonProperty(En ? "NPCs setting when the fire extinguishing system is activated" : "Настройка NPC, когда активирована система пожаротушения")] public HashSet<PresetConfig> NpcButton { get; set; }
            [JsonProperty(En ? "Economy setting (total values will be added up and rewarded at the end of the event)" : "Настройка экономики (конечное значение суммируется и будет выдано игрокам по окончанию ивента)")] public EconomyConfig Economy { get; set; }
            [JsonProperty(En ? "The CCTV camera" : "Название камеры")] public string Cctv { get; set; }
            [JsonProperty(En ? "Can SAM Site turrets appear in the event zone? [true/false]" : "Должны ли появляться Sam Site турели в зоне ивента? [true/false]")] public bool IsSamSites { get; set; }
            [JsonProperty(En ? "Delayed departure of CH47 after the start of the event [sec.]" : "Задержка вылета CH47 после начала ивента [sec.]")] public float DelayCh47 { get; set; }
            [JsonProperty(En ? "Delayed departure of workcart after the crash of CH47 [sec.]" : "Задержка выезда вагона после крушения CH47 [sec.]")] public float DelayWorkcart { get; set; }
            [JsonProperty(En ? "Flight altitude CH47 [m.]" : "Высота полета CH47 [m.]")] public float HeightCh47 { get; set; }
            [JsonProperty(En ? "The required amount of water in a barrel on the roof of the building to extinguish the fire" : "Необходимое кол-во воды в бочке на крыше, чтобы потушить пожар")] public int WaterAmount { get; set; }
            [JsonProperty(En ? "Configuration version" : "Версия конфигурации")] public VersionNumber PluginVersion { get; set; }

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    MinStartTime = 10800f,
                    MaxStartTime = 10800f,
                    EnabledTimer = true,
                    FinishTime = 3600,
                    PreStartTime = 300f,
                    PreFinishTime = 300,
                    TypeLootTableCrates = 0,
                    DefaultCrates = new HashSet<CrateConfig>
                    {
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_elite.prefab",
                            Position = "(38.271, 15.515, -83.755)",
                            Rotation = "(0, 243.51, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_elite.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinID = 0, Name = "" } }
                            }
                        },
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_elite.prefab",
                            Position = "(48.199, 15.517, -72.949)",
                            Rotation = "(0, 310.953, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_elite.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinID = 0, Name = "" } }
                            }
                        },
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_normal.prefab",
                            Position = "(47.605, 15.515, -63.634)",
                            Rotation = "(0, 0, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_normal.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinID = 0, Name = "" } }
                            }
                        },
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_normal.prefab",
                            Position = "(47.447, 15.513, -82.216)",
                            Rotation = "(0, 0, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_normal.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinID = 0, Name = "" } }
                            }
                        },
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_normal_2.prefab",
                            Position = "(28.779, 18.304, -76.519)",
                            Rotation = "(5.233, 0.302, 8.405)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_normal_2.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinID = 0, Name = "" } }
                            }
                        },
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_normal_2.prefab",
                            Position = "(38.1, 15.512, -65.748)",
                            Rotation = "(0, 0, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_normal_2.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinID = 0, Name = "" } }
                            }
                        }
                    },
                    HackCrate = new HackCrateConfig
                    {
                        Coordinates = new HashSet<CoordConfig>
                        {
                            new CoordConfig { Position = "(28.399, 15.517, -63.13)", Rotation = "(0, 135, 0)" },
                            new CoordConfig { Position = "(43.372, 16.521, -75.476)", Rotation = "(6.397, 1.034, 5.285)" }
                        },
                        UnlockTime = 600f,
                        IncreaseEventTime = true,
                        TypeLootTable = 0,
                        PrefabLootTable = new PrefabLootTableConfig
                        {
                            Min = 1,
                            Max = 1,
                            UseCount = true,
                            Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab" } }
                        },
                        OwnLootTable = new LootTableConfig
                        {
                            Min = 1,
                            Max = 1,
                            UseCount = true,
                            Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinID = 0, Name = "" } }
                        }
                    },
                    Marker = new MarkerConfig
                    {
                        Name = "PowerPlantEvent ({time} sec.)",
                        Radius = 0.4f,
                        Alpha = 0.6f,
                        Color = new ColorConfig { R = 0.81f, G = 0.25f, B = 0.15f }
                    },
                    Prefix = "[PowerPlantEvent]",
                    IsChat = true,
                    Gui = new GuiConfig
                    {
                        IsGui = true,
                        AnchorMin = "0 0.9",
                        AnchorMax = "1 0.95"
                    },
                    GuiAnnouncements = new GuiAnnouncementsConfig
                    {
                        IsGuiAnnouncements = false,
                        BannerColor = "Orange",
                        TextColor = "White",
                        ApiAdjustVPosition = 0.03f
                    },
                    Notify = new NotifyConfig
                    {
                        IsNotify = false,
                        Type = "0"
                    },
                    Discord = new DiscordConfig
                    {
                        IsDiscord = false,
                        WebhookUrl = "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks",
                        EmbedColor = 13516583,
                        Keys = new HashSet<string>
                        {
                            "PreStart",
                            "Start",
                            "PreFinish",
                            "Finish",
                            "CrashCh47",
                            "FinishWorkcart",
                            "Button"
                        }
                    },
                    IsCreateZonePvp = false,
                    PveMode = new PveModeConfig
                    {
                        Pve = false,
                        Damage = 500f,
                        ScaleDamage = new HashSet<ScaleDamageConfig>
                        {
                            new ScaleDamageConfig { Type = "NPC", Scale = 1f }
                        },
                        LootCrate = false,
                        HackCrate = false,
                        LootNpc = false,
                        DamageNpc = false,
                        TargetNpc = false,
                        CanEnter = false,
                        CanEnterCooldownPlayer = true,
                        TimeExitOwner = 300,
                        AlertTime = 60,
                        RestoreUponDeath = true,
                        CooldownOwner = 86400,
                        Darkening = 12
                    },
                    NTeleportationInterrupt = false,
                    RemoveBetterNpc = true,
                    NpcStart = new HashSet<PresetConfig>
                    {
                        new PresetConfig
                        {
                            Min = 6,
                            Max = 6,
                            Positions = new List<string>
                            {
                                "(39.3, 0.3, 66.9)",
                                "(34.6, 0.3, 71.3)",
                                "(46.9, 0.5, 73.3)",
                                "(-82.7, 3.3, -79.1)",
                                "(-95.2, 0.3, -84.8)",
                                "(-96.1, 0.3, -66.0)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "Mercenary",
                                Health = 125f,
                                RoamRange = 10f,
                                ChaseRange = 100f,
                                AttackRangeMultiplier = 2.5f,
                                SenseRange = 40f,
                                MemoryDuration = 60f,
                                DamageScale = 1.5f,
                                AimConeScale = 0.8f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 7.5f,
                                DisableRadio = true,
                                Stationary = false,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "hazmatsuit_scientist", SkinID = 0 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "smg.mp5", Amount = 1, SkinID = 2373921258, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" } },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 10, SkinID = 0, Mods = new HashSet<string>() },
                                    new NpcBelt { ShortName = "grenade.f1", Amount = 2, SkinID = 0, Mods = new HashSet<string>() }
                                },
                                Kit = ""
                            },
                            TypeLootTable = 5,
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Items = new List<ItemConfig>
                                {
                                    new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 50f, IsBluePrint = false, SkinID = 0, Name = "" },
                                    new ItemConfig { ShortName = "syringe.medical", MinAmount = 1, MaxAmount = 2, Chance = 70.0f, IsBluePrint = false, SkinID = 0, Name = "" }
                                }
                            }
                        },
                        new PresetConfig
                        {
                            Min = 11,
                            Max = 11,
                            Positions = new List<string>
                            {
                                "(-22.9, 18.3, 17.2)",
                                "(-22.9, 18.3, 4.4)",
                                "(-32.4, 18.3, 7.9)",
                                "(-41.4, 18.3, 17.0)",
                                "(-25.4, 0.3, -2.0)",
                                "(-42.8, 0.3, -8.1)",
                                "(-12.8, 0.3, 10.7)",
                                "(-26.0, 0.3, 18.3)",
                                "(-27.7, 12.2, 12.6)",
                                "(-42.8, 0.3, 18.6)",
                                "(-32.9, 12.3, 17.8)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "Worker",
                                Health = 125f,
                                RoamRange = 8f,
                                ChaseRange = 50f,
                                AttackRangeMultiplier = 2f,
                                SenseRange = 30f,
                                MemoryDuration = 30f,
                                DamageScale = 0.7f,
                                AimConeScale = 2f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 7.5f,
                                DisableRadio = true,
                                Stationary = false,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "hoodie", SkinID = 2000507925 },
                                    new NpcWear { ShortName = "pants", SkinID = 1987863036 },
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 2006541391 },
                                    new NpcWear { ShortName = "riot.helmet", SkinID = 840685680 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinID = 2006542444 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "pistol.m92", Amount = 1, SkinID = 0, Mods = new HashSet<string> { "weapon.mod.flashlight" } },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 5, SkinID = 0, Mods = new HashSet<string>() }
                                },
                                Kit = ""
                            },
                            TypeLootTable = 5,
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Items = new List<ItemConfig>
                                {
                                    new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 50f, IsBluePrint = false, SkinID = 0, Name = "" },
                                    new ItemConfig { ShortName = "syringe.medical", MinAmount = 1, MaxAmount = 2, Chance = 70.0f, IsBluePrint = false, SkinID = 0, Name = "" }
                                }
                            }
                        }
                    },
                    NpcTrain = new HashSet<PresetConfig>
                    {
                        new PresetConfig
                        {
                            Min = 3,
                            Max = 3,
                            Positions = new List<string>
                            {
                                "(-59.6, 0.2, 9.0)",
                                "(-68.9, 0.0, -2.4)",
                                "(-65.5, 0.3, 17.0)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "Machinist",
                                Health = 200f,
                                RoamRange = 10f,
                                ChaseRange = 50f,
                                AttackRangeMultiplier = 1f,
                                SenseRange = 50f,
                                MemoryDuration = 10f,
                                DamageScale = 2f,
                                AimConeScale = 1f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 7.5f,
                                DisableRadio = true,
                                Stationary = false,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "hoodie", SkinID = 2000507925 },
                                    new NpcWear { ShortName = "pants", SkinID = 1987863036 },
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 2006541391 },
                                    new NpcWear { ShortName = "riot.helmet", SkinID = 840685680 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinID = 2006542444 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "pistol.m92", Amount = 1, SkinID = 0, Mods = new HashSet<string> { "weapon.mod.flashlight" } },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 5, SkinID = 0, Mods = new HashSet<string>() },
                                    new NpcBelt { ShortName = "grenade.f1", Amount = 2, SkinID = 0, Mods = new HashSet<string>() }
                                },
                                Kit = ""
                            },
                            TypeLootTable = 5,
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Items = new List<ItemConfig>
                                {
                                    new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 50f, IsBluePrint = false, SkinID = 0, Name = "" },
                                    new ItemConfig { ShortName = "syringe.medical", MinAmount = 1, MaxAmount = 2, Chance = 70.0f, IsBluePrint = false, SkinID = 0, Name = "" }
                                }
                            }
                        }
                    },
                    NpcButton = new HashSet<PresetConfig>
                    {
                        new PresetConfig
                        {
                            Min = 5,
                            Max = 5,
                            Positions = new List<string>
                            {
                                "(38.0, 0.3, -55.2)",
                                "(56.9, 0.3, -73.6)",
                                "(18.2, 0.3, -71.5)",
                                "(25.6, 0.3, -58.3)",
                                "(48.7, 0.3, -59.1)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "PowerPlantEvent",
                                Health = 200f,
                                RoamRange = 10f,
                                ChaseRange = 100f,
                                AttackRangeMultiplier = 1f,
                                SenseRange = 50f,
                                MemoryDuration = 10f,
                                DamageScale = 2f,
                                AimConeScale = 1f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 7.5f,
                                DisableRadio = false,
                                Stationary = false,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "hazmatsuit_scientist", SkinID = 0 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "rifle.lr300", Amount = 1, SkinID = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" } },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 10, SkinID = 0, Mods = new HashSet<string>() },
                                    new NpcBelt { ShortName = "grenade.f1", Amount = 10, SkinID = 0, Mods = new HashSet<string>() }
                                },
                                Kit = ""
                            },
                            TypeLootTable = 5,
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Items = new List<ItemConfig>
                                {
                                    new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 50f, IsBluePrint = false, SkinID = 0, Name = "" },
                                    new ItemConfig { ShortName = "syringe.medical", MinAmount = 1, MaxAmount = 2, Chance = 70.0f, IsBluePrint = false, SkinID = 0, Name = "" }
                                }
                            }
                        }
                    },
                    Economy = new EconomyConfig
                    {
                        Plugins = new HashSet<string> { "Economics", "Server Rewards", "IQEconomic" },
                        Min = 0,
                        Crates = new Dictionary<string, double>
                        {
                            ["crate_elite"] = 0.4,
                            ["crate_normal"] = 0.2,
                            ["crate_normal_2"] = 0.1
                        },
                        Npc = 0.3,
                        LockedCrate = 0.5,
                        Button = 0.4,
                        Commands = new HashSet<string>()
                    },
                    Cctv = "PowerPlant",
                    IsSamSites = true,
                    DelayCh47 = 0f,
                    DelayWorkcart = 0f,
                    HeightCh47 = 200f,
                    WaterAmount = 10000,
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion Config

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PreStart"] = "{0} A Chinook brings <color=#738d43>Crates</color> with loot for scientists to the <color=#55aaff>Power Plant</color> in <color=#55aaff>{1} sec.</color>!",
                ["Start"] = "{0} A Chinook <color=#738d43>has flown</color> to grid <color=#55aaff>{1}</color> to get Crates with loot!",
                ["PreFinish"] = "{0} The Power Plant Event <color=#ce3f27>will end</color> in <color=#55aaff>{1} sec.</color>!",
                ["Finish"] = "{0} The Power Plant Event <color=#ce3f27>has concluded</color>!",
                ["CrashCh47"] = "{0} The Chinook failed to control safe flying conditions and <color=#55aaff>crashed</color> into the broken cooling tower at The Power Plant! You need to <color=#55aaff>put out the fire</color> to get access to the Loot up top!\nCCTV: <color=#55aaff>{1}</color>",
                ["StartWorkcart"] = "{0} The Power Plant Workcart <color=#738d43>will arrive</color> soon. You can put out the fire at the power plant using the water on the Workcart.",
                ["FinishWorkcart"] = "{0} The Workcart has arrived. You need to <color=#55aaff>transfer water</color> from the Workcart to the Water Barrel on the top of the building. You need <color=#55aaff>{1}</color> units of water",
                ["FinishWaterBarrel"] = "{0} Water Barrel is <color=#55aaff>full</color> of enough water to put out The Fire. You <color=#55aaff>have to turn on the fire system</color> using the button on the Water Barrel",
                ["NoWaterInBarrel"] = "{0} There is <color=#ce3f27>not enough</color> water to put out the fire. You need <color=#55aaff>{1}</color> units of water",
                ["Button"] = "{0} Fire Sprinklers <color=#738d43>activated</color> by <color=#55aaff>{1}</color>! The Crates in the broken cooling tower of The Power Plant are <color=#738d43>available</color>",
                ["EventActive"] = "{0} This event is active. To finish this event (<color=#55aaff>/ppstop</color>), then (<color=#55aaff>/ppstart</color>) to start the next one!",
                ["GUI"] = "The Power Plant Event will end in {0} sec.",
                ["EnterPVP"] = "{0} You <color=#ce3f27>have entered</color> the PVP zone, now other players <color=#ce3f27>can damage</color> you!",
                ["ExitPVP"] = "{0} You <color=#738d43>have left</color> the PVP zone, now other players <color=#738d43>cannot damage</color> you!",
                ["NTeleportation"] = "{0} You <color=#ce3f27>cannot</color> teleport into the event zone!",
                ["SendEconomy"] = "{0} You <color=#738d43>have earned</color> <color=#55aaff>{1}</color> points in economics for participating in the event"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PreStart"] = "{0} Через <color=#55aaff>{1} сек.</color> в локацию <color=#55aaff>Электростанция</color> CH47 <color=#738d43>доставит</color> новое снаряжение для ученых!",
                ["Start"] = "{0} CH47 <color=#738d43>вылетел</color> в квадрат <color=#55aaff>{1}</color>, чтобы доставить снаряжение!",
                ["PreFinish"] = "{0} Ивент на электростанции <color=#ce3f27>закончится</color> через <color=#55aaff>{1} сек.</color>!",
                ["Finish"] = "{0} Ивент на электростанции <color=#ce3f27>закончен</color>!",
                ["CrashCh47"] = "{0} CH47 не справился с управлением и <color=#55aaff>потерпел крушение</color> в градирне электростанции! Необходимо <color=#55aaff>потушить пожар</color>, чтобы получить доступ к снаряжению\nКамера: <color=#55aaff>{1}</color>",
                ["StartWorkcart"] = "{0} В скором времени прибудет поезд с водой, которой вы сможете потушить пожар на электростанции",
                ["FinishWorkcart"] = "{0} Поезд <color=#738d43>прибыл</color>, необходимо <color=#55aaff>переместить воду</color> из поезда в бочку на крыше здания. Необходимо <color=#55aaff>{1}</color> ед. воды",
                ["FinishWaterBarrel"] = "{0} В бочке <color=#738d43>достаточно</color> воды, чтобы потушить пожар. Необходимо <color=#55aaff>включить систему пожаротушения</color> кнопкой на корпусе бочки с водой",
                ["NoWaterInBarrel"] = "{0} В бочке <color=#ce3f27>недостаточно</color> воды, чтобы потушить пожар. Необходимо еще <color=#55aaff>{1}</color> ед. воды",
                ["Button"] = "{0} Режим пожаротушения <color=#738d43>активировал</color> <color=#55aaff>{1}</color>! Ящики в градирне электростанции <color=#738d43>доступны</color>",
                ["EventActive"] = "{0} Ивент в данный момент активен, сначала завершите текущий ивент (<color=#55aaff>/ppstop</color>), чтобы начать следующий!",
                ["GUI"] = "Ивент на электростанции закончится через {0} сек.",
                ["EnterPVP"] = "{0} Вы <color=#ce3f27>вошли</color> в PVP зону, теперь другие игроки <color=#ce3f27>могут</color> наносить вам урон!",
                ["ExitPVP"] = "{0} Вы <color=#738d43>вышли</color> из PVP зоны, теперь другие игроки <color=#738d43>не могут</color> наносить вам урон!",
                ["NTeleportation"] = "{0} Вы <color=#ce3f27>не можете</color> телепортироваться в зоне ивента!",
                ["SendEconomy"] = "{0} Вы <color=#738d43>получили</color> <color=#55aaff>{1}</color> баллов в экономику за прохождение ивента"
            }, this, "ru");
        }

        private string GetMessage(string langKey, string userID) => lang.GetMessage(langKey, _ins, userID);

        private string GetMessage(string langKey, string userID, params object[] args) => (args.Length == 0) ? GetMessage(langKey, userID) : string.Format(GetMessage(langKey, userID), args);
        #endregion Lang

        #region Oxide Hooks
        private static PowerPlantEvent _ins;

        private void Init()
        {
            _ins = this;
            Unsubscribes();
        }

        private void OnServerInitialized()
        {
			PrintWarning("\n-----------------------------\n" +
            " Author - Sempai#3239\n" +
            " VK - https://vk.com/rustnastroika/n" +
            " Forum - https://darkplugins.ru/n" +
            " Discord - https://discord.gg/5DPTsRmd3G/n" +
            "-----------------------------");
            LoadDefaultMessages();
            CheckAllLootTables();
            foreach (MonumentInfo monument in TerrainMeta.Path.Monuments) if (monument.displayPhrase.english == "Power Plant") StartLocations.Add(new Location { pos = monument.transform.position, rot = monument.transform.rotation.eulerAngles });
            if (StartLocations.Count == 0)
            {
                PrintError("The Power Plant location is missing on the map. The plugin cannot be loaded!");
                NextTick(() => Server.Command($"o.unload {Name}"));
                return;
            }
            if (_config.EnabledTimer)
            {
                timer.In(UnityEngine.Random.Range(_config.MinStartTime, _config.MaxStartTime), () =>
                {
                    if (!_active) Start();
                    else Puts("This event is active now. To finish this event (ppstop), then to start the next one");
                });
            }
        }

        private void Unload()
        {
            if (_active) Finish();
            _ins = null;
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return null;
            if (_controller.Entities.Contains(entity) ||
                (entity is CH47Helicopter && entity == _controller.Ch47) ||
                (entity is TrainEngine && entity == _controller.Train) ||
                (entity is NPCShopKeeper && entity == _controller.Conductor)) return true;
            return null;
        }

        private object CanPickupEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity != null && _controller.Entities.Contains(entity)) return false;
            else return null;
        }

        private object OnStructureRotate(BaseCombatEntity entity, BasePlayer player)
        {
            if (entity != null && _controller.Entities.Contains(entity)) return true;
            else return null;
        }

        private object OnEntityGroundMissing(BaseEntity entity)
        {
            if (entity != null && _controller.Entities.Contains(entity)) return true;
            else return null;
        }

        private void OnEntityDeath(BasePlayer player, HitInfo info)
        {
            if (player != null && _controller.Players.Contains(player))
            {
                _controller.Players.Remove(player);
                if (_config.Gui.IsGui) CuiHelper.DestroyUi(player, "TextMain");
            }
        }

        private void OnEntityDeath(ScientistNPC npc, HitInfo info)
        {
            if (npc == null || info == null) return;
            BasePlayer attacker = info.InitiatorPlayer;
            if (_controller.Scientists.Contains(npc) && attacker.IsPlayer()) ActionEconomy(attacker.userID, "Npc");
        }

        private object CanMountEntity(BasePlayer player, BaseMountable entity)
        {
            if (player == null || entity == null || _controller.Train == null) return null;
            if (player != _controller.Conductor && entity == _controller.Train.mountPoints[0].mountable) return true;
            else return null;
        }

        private readonly Dictionary<uint, ulong> _startHackCrates = new Dictionary<uint, ulong>();

        private object CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (player == null || crate == null) return null;
            if (_controller.HackCrates.Contains(crate))
            {
                if (!_controller.IsExtinguish) return true;
                if (_startHackCrates.ContainsKey(crate.net.ID)) _startHackCrates[crate.net.ID] = player.userID;
                else _startHackCrates.Add(crate.net.ID, player.userID);
            }
            return null;
        }

        private void OnCrateHack(HackableLockedCrate crate)
        {
            if (crate == null) return;
            uint crateId = crate.net.ID;
            ulong playerId;
            if (_startHackCrates.TryGetValue(crateId, out playerId))
            {
                _startHackCrates.Remove(crateId);
                if (_config.HackCrate.IncreaseEventTime && _controller.TimeToFinish < (int)_config.HackCrate.UnlockTime) _controller.TimeToFinish += (int)_config.HackCrate.UnlockTime;
                ActionEconomy(playerId, "LockedCrate");
            }
        }

        private object OnButtonPress(PressButton button, BasePlayer player)
        {
            if (button == null || player == null) return null;
            if (button == _controller.Button && _controller.StageCh47 == 4)
            {
                if (_controller.WaterBarrel.GetLiquidCount() >= _config.WaterAmount)
                {
                    if (_config.PveMode.Pve && plugins.Exists("PveMode") && PveMode.Call("CanActionEvent", Name, player) != null) return true;
                    _controller.Extinguish();
                    foreach (PresetConfig preset in _ins._config.NpcButton) _controller.SpawnPreset(preset);
                    if (_config.PveMode.Pve && plugins.Exists("PveMode")) PveMode.Call("EventAddScientists", Name, _controller.Scientists.Select(x => x.net.ID));
                    ActionEconomy(player.userID, "Button");
                    AlertToAllPlayers("Button", _config.Prefix, player.displayName);
                    Unsubscribe("OnButtonPress");
                }
                else if (!_controller.IsExtinguish) AlertToPlayer(player, GetMessage("NoWaterInBarrel", player.UserIDString, _config.Prefix, _config.WaterAmount - _controller.WaterBarrel.GetLiquidCount()));
            }
            return null;
        }

        private object OnNpcTarget(BaseEntity attacker, NPCShopKeeper victim)
        {
            if (attacker == null || victim == null) return null;
            if (victim == _controller.Conductor) return true;
            else return null;
        }

        private readonly HashSet<uint> _lootableCrates = new HashSet<uint>();

        private void OnLootEntity(BasePlayer player, LootContainer container)
        {
            if (player == null || container == null || _lootableCrates.Contains(container.net.ID)) return;
            if (_controller.Crates.Contains(container))
            {
                _lootableCrates.Add(container.net.ID);
                ActionEconomy(player.userID, "Crates", container.ShortPrefabName);
            }
        }
        #endregion Oxide Hooks

        #region Controller
        private ControllerPowerPlantEvent _controller;
        private bool _active = false;

        private void Start()
        {
            if (!plugins.Exists("NpcSpawn"))
            {
                PrintError("NpcSpawn plugin doesn`t exist! Please read the file ReadMe.txt");
                Server.Command($"o.unload {Name}");
                return;
            }
            _active = true;
            AlertToAllPlayers("PreStart", _config.Prefix, _config.PreStartTime);
            timer.In(_config.PreStartTime, () =>
            {
                Interface.Oxide.CallHook("OnPowerPlantEventStart");
                Subscribes();
                _controller = new GameObject().AddComponent<ControllerPowerPlantEvent>();
                if (_config.PveMode.Pve && plugins.Exists("PveMode"))
                {
                    JObject config = new JObject
                    {
                        ["Damage"] = _config.PveMode.Damage,
                        ["ScaleDamage"] = new JArray { _config.PveMode.ScaleDamage.Select(x => new JObject { ["Type"] = x.Type, ["Scale"] = x.Scale }) },
                        ["LootCrate"] = _config.PveMode.LootCrate,
                        ["HackCrate"] = _config.PveMode.HackCrate,
                        ["LootNpc"] = _config.PveMode.LootNpc,
                        ["DamageNpc"] = _config.PveMode.DamageNpc,
                        ["DamageTank"] = false,
                        ["TargetNpc"] = _config.PveMode.TargetNpc,
                        ["TargetTank"] = false,
                        ["CanEnter"] = _config.PveMode.CanEnter,
                        ["CanEnterCooldownPlayer"] = _config.PveMode.CanEnterCooldownPlayer,
                        ["TimeExitOwner"] = _config.PveMode.TimeExitOwner,
                        ["AlertTime"] = _config.PveMode.AlertTime,
                        ["RestoreUponDeath"] = _config.PveMode.RestoreUponDeath,
                        ["CooldownOwner"] = _config.PveMode.CooldownOwner,
                        ["Darkening"] = _config.PveMode.Darkening
                    };
                    PveMode.Call("EventAddPveMode", Name, config, _controller.transform.position, Radius, new HashSet<uint>(), _controller.Scientists.Select(x => x.net.ID), new HashSet<uint>(), new HashSet<ulong>(), null);
                }
                if (_config.RemoveBetterNpc && plugins.Exists("BetterNpc")) BetterNpc.Call("DestroyController", "Power Plant");
                AlertToAllPlayers("Start", _config.Prefix, PhoneController.PositionToGridCoord(_controller.transform.position));
            });
        }

        private void Finish()
        {
            Unsubscribes();
            if (_config.PveMode.Pve && plugins.Exists("PveMode")) PveMode.Call("EventRemovePveMode", Name, true);
            if (_controller != null) UnityEngine.Object.Destroy(_controller.gameObject);
            _active = false;
            SendBalance();
            AlertToAllPlayers("Finish", _config.Prefix);
            Interface.Oxide.CallHook("OnPowerPlantEventEnd");
            if (_config.RemoveBetterNpc && plugins.Exists("BetterNpc")) BetterNpc.Call("CreateController", "Power Plant");
            NextTick(() => Server.Command($"o.reload {Name}"));
        }

        private class Prefab { public string prefab; public Vector3 pos; public Vector3 rot; }
        private readonly HashSet<Prefab> _prefabs = new HashSet<Prefab>
        {
            //sam_static
            new Prefab { prefab = "assets/prefabs/npc/sam_site_turret/sam_static.prefab", pos = new Vector3(-32.231f, 18.25f, 19.184f), rot = new Vector3(0f, 0f, 0f) },
            new Prefab { prefab = "assets/prefabs/npc/sam_site_turret/sam_static.prefab", pos = new Vector3(-21.197f, 18.25f, 11.122f), rot = new Vector3(0f, 90f, 0f) },
            new Prefab { prefab = "assets/prefabs/npc/sam_site_turret/sam_static.prefab", pos = new Vector3(-32.231f, 18.25f, 3.062f), rot = new Vector3(0f, 180f, 0f) },
            new Prefab { prefab = "assets/prefabs/npc/sam_site_turret/sam_static.prefab", pos = new Vector3(-43.304f, 18.25f, 11.122f), rot = new Vector3(0f, 270f, 0f) },
            //waterbarrel
            new Prefab { prefab = "assets/prefabs/deployable/liquidbarrel/waterbarrel.prefab", pos = new Vector3(-38.125f, 18.25f, 12.609f), rot = new Vector3(0f, 0f, 0f) },
            //button
            new Prefab { prefab = "assets/prefabs/deployable/playerioents/button/button.prefab", pos = new Vector3(-38.766f, 18.523f, 12.606f), rot = new Vector3(0f, 270f, 0f) },
            //electric.sprinkler.deployed
            new Prefab { prefab = "assets/prefabs/deployable/playerioents/sprinkler/electric.sprinkler.deployed.prefab", pos = new Vector3(31.676f, 18.939f, -56.999f), rot = new Vector3(296.716f, 150.739f, 186.214f) },
            new Prefab { prefab = "assets/prefabs/deployable/playerioents/sprinkler/electric.sprinkler.deployed.prefab", pos = new Vector3(44.946f, 18.94f, -57.049f), rot = new Vector3(295.963f, 199.152f, 185.441f) },
            new Prefab { prefab = "assets/prefabs/deployable/playerioents/sprinkler/electric.sprinkler.deployed.prefab", pos = new Vector3(54.255f, 18.945f, -66.452f), rot = new Vector3(296.042f, 242.526f, 184.222f) },
            new Prefab { prefab = "assets/prefabs/deployable/playerioents/sprinkler/electric.sprinkler.deployed.prefab", pos = new Vector3(54.216f, 18.943f, -79.646f), rot = new Vector3(296.249f, 297.468f, 176.694f) },
            new Prefab { prefab = "assets/prefabs/deployable/playerioents/sprinkler/electric.sprinkler.deployed.prefab", pos = new Vector3(45.223f, 18.934f, -88.831f), rot = new Vector3(63.195f, 155.179f, 180f) },
            new Prefab { prefab = "assets/prefabs/deployable/playerioents/sprinkler/electric.sprinkler.deployed.prefab", pos = new Vector3(31.609f, 18.954f, -88.97f), rot = new Vector3(0.889f, 291.783f, 243.052f) },
            //cctv_deployed
            new Prefab { prefab = "assets/prefabs/deployable/cctvcamera/cctv_deployed.prefab", pos = new Vector3(48.717f, 35.249f, -75.75f), rot = new Vector3(53.057f, 270f, 0f) }
        };

        internal class ControllerPowerPlantEvent : FacepunchBehaviour
        {
            private MapMarkerGenericRadius _mapmarker;
            private VendingMachineMapMarker _vendingMarker;
            private SphereCollider _sphereCollider;

            internal CH47Helicopter Ch47;
            private CH47HelicopterAIController _ch47Ai;
            internal int StageCh47;
            private Vector3 _spawnCh47Pos;
            private Vector2 _targetCh47Pos;
            private Vector2 _crashCh47Pos;

            internal TrainEngine Train;
            private Vector3 _finishTrainPos;
            internal NPCShopKeeper Conductor;
            private LiquidContainer _trainWaterBarrel;
            internal LootContainer TrainCrate;

            internal PressButton Button;
            internal LiquidContainer WaterBarrel;
            internal bool IsExtinguish;
            private bool _notificationFinishWaterBarrel;

            internal HashSet<BaseEntity> Entities = new HashSet<BaseEntity>();
            private readonly HashSet<Sprinkler> _sprinklers = new HashSet<Sprinkler>();
            private readonly HashSet<FireBall> _fireBalls = new HashSet<FireBall>();

            internal HashSet<LootContainer> Crates = new HashSet<LootContainer>();
            internal HashSet<HackableLockedCrate> HackCrates = new HashSet<HackableLockedCrate>();

            internal int TimeToFinish;
            internal HashSet<BasePlayer> Players = new HashSet<BasePlayer>();

            internal HashSet<ScientistNPC> Scientists = new HashSet<ScientistNPC>();

            private FireBall[] _engineFires;

            private void Awake()
            {
                Location location = _ins.StartLocations.GetRandom();
                transform.position = location.pos;
                transform.rotation = Quaternion.Euler(location.rot);

                SpawnMapMarker();

                gameObject.layer = 3;
                _sphereCollider = gameObject.AddComponent<SphereCollider>();
                _sphereCollider.isTrigger = true;
                _sphereCollider.radius = _ins.Radius;

                Vector3 pos = GetGlobalPosition(new Vector3(-40.128f, 0f, -76.298f));
                _targetCh47Pos = new Vector2(pos.x, pos.z);
                pos = GetGlobalPosition(new Vector3(38.094f, 0f, -72.874f));
                _crashCh47Pos = new Vector2(pos.x, pos.z);
                RandomSpawnPosCh47();
                InvokeRepeating(UpdateCh47, _ins._config.DelayCh47, 1f);

                SpawnEntities();

                TimeToFinish = _ins._config.FinishTime;
                InvokeRepeating(ChangeToFinishTime, 1f, 1f);

                foreach (PresetConfig preset in _ins._config.NpcStart) SpawnPreset(preset);

                _finishTrainPos = GetGlobalPosition(new Vector3(-65.747f, 0.282f, 8.075f));
            }

            private void OnDestroy()
            {
                CancelInvoke(UpdateCh47);
                if (Ch47.IsExists()) Ch47.Kill();

                CancelInvoke(UpdateMapMarker);
                if (_mapmarker.IsExists()) _mapmarker.Kill();
                if (_vendingMarker.IsExists()) _vendingMarker.Kill();

                CancelInvoke(SpawnWorkcart);
                CancelInvoke(UpdateWorkcart);
                CancelInvoke(SpawnTrainEntities);
                if (Train.IsExists()) Train.Kill();
                if (Conductor.IsExists()) Conductor.Kill();
                if (TrainCrate.IsExists()) TrainCrate.Kill();

                foreach (BaseEntity entity in Entities) if (entity.IsExists()) entity.Kill();
                foreach (FireBall fireBall in _fireBalls) if (fireBall.IsExists()) fireBall.Kill();

                foreach (LootContainer crate in Crates) if (crate.IsExists()) crate.Kill();
                foreach (HackableLockedCrate crate in HackCrates) if (crate.IsExists()) crate.Kill();

                foreach (ScientistNPC npc in Scientists) if (npc.IsExists()) npc.Kill();

                CancelInvoke(ChangeToFinishTime);
                foreach (BasePlayer player in Players) CuiHelper.DestroyUi(player, "TextMain");
            }

            private void OnTriggerEnter(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player.IsPlayer())
                {
                    Players.Add(player);
                    if (_ins._config.Gui.IsGui) _ins.MessageGUI(player, _ins.GetMessage("GUI", player.UserIDString, TimeToFinish));
                    if (_ins._config.IsCreateZonePvp) _ins.AlertToPlayer(player, _ins.GetMessage("EnterPVP", player.UserIDString, _ins._config.Prefix));
                }
            }

            private void OnTriggerExit(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player.IsPlayer())
                {
                    Players.Remove(player);
                    if (_ins._config.Gui.IsGui) CuiHelper.DestroyUi(player, "TextMain");
                    if (_ins._config.IsCreateZonePvp) _ins.AlertToPlayer(player, _ins.GetMessage("ExitPVP", player.UserIDString, _ins._config.Prefix));
                }
            }

            private void ChangeToFinishTime()
            {
                TimeToFinish--;
                if (_ins._config.Gui.IsGui) foreach (BasePlayer player in Players) _ins.MessageGUI(player, _ins.GetMessage("GUI", player.UserIDString, TimeToFinish));
                if (_trainWaterBarrel != null && _trainWaterBarrel.GetLiquidCount() < 20000) _trainWaterBarrel.GetLiquidItem().amount = 20000;
                if (!_notificationFinishWaterBarrel && StageCh47 == 4 && WaterBarrel.GetLiquidCount() >= _ins._config.WaterAmount)
                {
                    _ins.AlertToAllPlayers("FinishWaterBarrel", _ins._config.Prefix);
                    _notificationFinishWaterBarrel = true;
                }
                if (TimeToFinish == _ins._config.PreFinishTime) _ins.AlertToAllPlayers("PreFinish", _ins._config.Prefix, _ins._config.PreFinishTime);
                else if (TimeToFinish == 0)
                {
                    CancelInvoke(ChangeToFinishTime);
                    _ins.Finish();
                }
            }

            private void SpawnMapMarker()
            {
                _mapmarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", transform.position) as MapMarkerGenericRadius;
                _mapmarker.Spawn();
                _mapmarker.radius = _ins._config.Marker.Radius;
                _mapmarker.alpha = _ins._config.Marker.Alpha;
                _mapmarker.color1 = new Color(_ins._config.Marker.Color.R, _ins._config.Marker.Color.G, _ins._config.Marker.Color.B);

                _vendingMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", transform.position) as VendingMachineMapMarker;
                _vendingMarker.markerShopName = _ins._config.Marker.Name.Replace("{time}", $"{TimeToFinish}");
                _vendingMarker.Spawn();

                InvokeRepeating(UpdateMapMarker, 0, 1f);
            }

            private void UpdateMapMarker()
            {
                _mapmarker.SendUpdate();
                _vendingMarker.markerShopName = _ins._config.Marker.Name.Replace("{time}", $"{TimeToFinish}");
                _vendingMarker.SendNetworkUpdate();
            }

            private static void GetGlobal(Transform Transform, Vector3 localPosition, Vector3 localRotation, out Vector3 globalPosition, out Quaternion globalRotation)
            {
                globalPosition = Transform.TransformPoint(localPosition);
                globalRotation = Transform.rotation * Quaternion.Euler(localRotation);
            }

            private Vector3 GetGlobalPosition(Vector3 localPosition) => transform.TransformPoint(localPosition);

            private void SpawnEntities()
            {
                foreach (Prefab prefab in _ins._prefabs)
                {
                    if (prefab.prefab == "assets/prefabs/npc/sam_site_turret/sam_static.prefab" && !_ins._config.IsSamSites) continue;

                    Vector3 pos; Quaternion rot;
                    GetGlobal(transform, prefab.pos, prefab.rot, out pos, out rot);
                    BaseEntity entity = GameManager.server.CreateEntity(prefab.prefab, pos, rot);
                    entity.enableSaving = false;
                    entity.Spawn();

                    if (entity is StabilityEntity) (entity as StabilityEntity).grounded = true;

                    if (entity is LiquidContainer) WaterBarrel = entity as LiquidContainer;

                    if (entity is PressButton) Button = entity as PressButton;

                    if (entity is Sprinkler) _sprinklers.Add(entity as Sprinkler);

                    if (entity is CCTV_RC)
                    {
                        CCTV_RC cctv = entity as CCTV_RC;
                        cctv.UpdateFromInput(5, 0);
                        cctv.rcIdentifier = _ins._config.Cctv;
                    }

                    Entities.Add(entity);
                }
            }

            private void SpawnCrates()
            {
                foreach (CrateConfig crateConfig in _ins._config.DefaultCrates)
                {
                    Vector3 pos; Quaternion rot;
                    GetGlobal(transform, crateConfig.Position.ToVector3(), crateConfig.Rotation.ToVector3(), out pos, out rot);
                    LootContainer crate = GameManager.server.CreateEntity(crateConfig.Prefab, pos, rot) as LootContainer;
                    crate.enableSaving = false;
                    crate.Spawn();
                    Crates.Add(crate);
                    SpawnFireBall(pos);
                    crate.SetFlag(BaseEntity.Flags.Locked, true);
                    if (_ins._config.TypeLootTableCrates == 1 || _ins._config.TypeLootTableCrates == 4 || _ins._config.TypeLootTableCrates == 5)
                    {
                        _ins.NextTick(() =>
                        {
                            crate.inventory.ClearItemsContainer();
                            if (_ins._config.TypeLootTableCrates == 4 || _ins._config.TypeLootTableCrates == 5) _ins.AddToContainerPrefab(crate.inventory, crateConfig.PrefabLootTable);
                            if (_ins._config.TypeLootTableCrates == 1 || _ins._config.TypeLootTableCrates == 5) _ins.AddToContainerItem(crate.inventory, crateConfig.OwnLootTable);
                        });
                    }
                }
            }

            private void SpawnHackCrates()
            {
                foreach (CoordConfig coord in _ins._config.HackCrate.Coordinates)
                {
                    Vector3 pos; Quaternion rot;
                    GetGlobal(transform, coord.Position.ToVector3(), coord.Rotation.ToVector3(), out pos, out rot);
                    HackableLockedCrate hackCrate = GameManager.server.CreateEntity("assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab", pos, rot) as HackableLockedCrate;
                    hackCrate.enableSaving = false;
                    hackCrate.Spawn();
                    hackCrate.hackSeconds = HackableLockedCrate.requiredHackSeconds - _ins._config.HackCrate.UnlockTime;
                    HackCrates.Add(hackCrate);
                    SpawnFireBall(pos);
                    hackCrate.SetFlag(BaseEntity.Flags.Locked, true);
                    if (_ins._config.HackCrate.TypeLootTable == 1 || _ins._config.HackCrate.TypeLootTable == 4 || _ins._config.HackCrate.TypeLootTable == 5)
                    {
                        _ins.NextTick(() =>
                        {
                            hackCrate.inventory.ClearItemsContainer();
                            if (_ins._config.HackCrate.TypeLootTable == 4 || _ins._config.HackCrate.TypeLootTable == 5) _ins.AddToContainerPrefab(hackCrate.inventory, _ins._config.HackCrate.PrefabLootTable);
                            if (_ins._config.HackCrate.TypeLootTable == 1 || _ins._config.HackCrate.TypeLootTable == 5) _ins.AddToContainerItem(hackCrate.inventory, _ins._config.HackCrate.OwnLootTable);
                        });
                    }
                }
            }

            internal void SpawnPreset(PresetConfig preset)
            {
                int count = UnityEngine.Random.Range(preset.Min, preset.Max + 1);
                List<Vector3> positions = preset.Positions.Select(x => transform.TransformPoint(x.ToVector3()));
                JObject config = GetObjectConfig(preset.Config);
                for (int i = 0; i < count; i++)
                {
                    Vector3 pos = positions.GetRandom();
                    positions.Remove(pos);
                    ScientistNPC npc = (ScientistNPC)_ins.NpcSpawn.Call("SpawnNpc", pos, config);
                    Scientists.Add(npc);
                }
            }

            private JObject GetObjectConfig(NpcConfig config)
            {
                HashSet<string> states = config.Stationary ? new HashSet<string> { "IdleState", "CombatStationaryState" } : new HashSet<string> { "RoamState", "ChaseState", "CombatState" };
                if (config.BeltItems.Any(x => x.ShortName == "rocket.launcher" || x.ShortName == "explosive.timed")) states.Add("RaidState");
                return new JObject
                {
                    ["Name"] = config.Name,
                    ["WearItems"] = new JArray { config.WearItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["SkinID"] = x.SkinID }) },
                    ["BeltItems"] = new JArray { config.BeltItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["Amount"] = x.Amount, ["SkinID"] = x.SkinID, ["Mods"] = new JArray { x.Mods }, ["Ammo"] = string.Empty }) },
                    ["Kit"] = config.Kit,
                    ["Health"] = config.Health,
                    ["RoamRange"] = config.RoamRange,
                    ["ChaseRange"] = config.ChaseRange,
                    ["DamageScale"] = config.DamageScale,
                    ["TurretDamageScale"] = 1f,
                    ["AimConeScale"] = config.AimConeScale,
                    ["DisableRadio"] = config.DisableRadio,
                    ["CanUseWeaponMounted"] = true,
                    ["CanRunAwayWater"] = true,
                    ["Speed"] = config.Speed,
                    ["AreaMask"] = 1,
                    ["AgentTypeID"] = -1372625422,
                    ["HomePosition"] = string.Empty,
                    ["States"] = new JArray { states },
                    ["Sensory"] = new JObject
                    {
                        ["AttackRangeMultiplier"] = config.AttackRangeMultiplier,
                        ["SenseRange"] = config.SenseRange,
                        ["MemoryDuration"] = config.MemoryDuration,
                        ["CheckVisionCone"] = config.CheckVisionCone,
                        ["VisionCone"] = config.VisionCone
                    }
                };
            }

            private void SpawnFireBall(Vector3 pos)
            {
                FireBall fireBall = GameManager.server.CreateEntity("assets/bundled/prefabs/fireball.prefab", pos) as FireBall;
                fireBall.enableSaving = false;
                fireBall.Spawn();
                fireBall.lifeTimeMin = TimeToFinish;
                fireBall.lifeTimeMax = TimeToFinish;
                fireBall.AddLife(TimeToFinish);
                _fireBalls.Add(fireBall);
            }

            private void RandomSpawnPosCh47()
            {
                List<float> list = new List<float> { -World.Size / 2, World.Size / 2 };
                while (_spawnCh47Pos == Vector3.zero)
                {
                    _spawnCh47Pos = new Vector3(list.GetRandom(), _ins._config.HeightCh47, list.GetRandom());
                    if (Vector2.Distance(new Vector2(_spawnCh47Pos.x, _spawnCh47Pos.z), _targetCh47Pos) > Vector2.Distance(new Vector2(_spawnCh47Pos.x, _spawnCh47Pos.z), _crashCh47Pos)) _spawnCh47Pos = Vector3.zero;
                }
            }

            private void UpdateCh47()
            {
                if (StageCh47 == 0)
                {
                    SpawnNewCh47(_spawnCh47Pos, Quaternion.identity, new Vector3(_targetCh47Pos.x, _ins._config.HeightCh47, _targetCh47Pos.y));
                    StageCh47++;
                }
                else if (StageCh47 == 1)
                {
                    if (Vector2.Distance(new Vector2(Ch47.transform.position.x, Ch47.transform.position.z), _targetCh47Pos) < _ins.Radius * 3f)
                    {
                        SpawnNewCh47(Ch47.transform.position, Ch47.transform.rotation, new Vector3(_targetCh47Pos.x, 0f, _targetCh47Pos.y));
                        StageCh47++;
                    }
                }
                else if (StageCh47 == 2)
                {
                    if (Vector2.Distance(new Vector2(Ch47.transform.position.x, Ch47.transform.position.z), _targetCh47Pos) < _ins.Radius)
                    {
                        SpawnNewCh47(Ch47.transform.position, Ch47.transform.rotation, new Vector3(_crashCh47Pos.x, 0f, _crashCh47Pos.y));
                        RunEffect("assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab", null, Ch47.transform.position);
                        RunEffect("assets/prefabs/npc/patrol helicopter/damage_effect_debris.prefab", null, Ch47.transform.position);
                        if (_engineFires == null) _engineFires = new FireBall[] { SpawnFireball(Ch47, new Vector3(-1, 3, 1), 1200), SpawnFireball(Ch47, new Vector3(1, 3, 1), 1200) };
                        StageCh47++;
                    }
                }
                else if (StageCh47 == 3)
                {
                    if (Vector2.Distance(new Vector2(Ch47.transform.position.x, Ch47.transform.position.z), _crashCh47Pos) < 15f)
                    {
                        CancelInvoke(UpdateCh47);
                        Ch47.Die(new HitInfo(Ch47, Ch47, DamageType.Explosion, 1000f));
                        SpawnCrates();
                        SpawnHackCrates();
                        if (_ins._config.PveMode.Pve && _ins.plugins.Exists("PveMode"))
                        {
                            HashSet<uint> crates = Crates.Select(x => x.net.ID);
                            foreach (HackableLockedCrate crate in HackCrates) crates.Add(crate.net.ID);
                            _ins.PveMode.Call("EventAddCrates", _ins.Name, crates);
                        }
                        SpawnFireBall(GetGlobalPosition(new Vector3(38.181f, 15.512f, -57.818f)));
                        SpawnFireBall(GetGlobalPosition(new Vector3(23.125f, 15.514f, -72.957f)));
                        SpawnFireBall(GetGlobalPosition(new Vector3(53.493f, 15.511f, -73.002f)));
                        Invoke(SpawnWorkcart, _ins._config.DelayWorkcart);
                        _ins.AlertToAllPlayers("CrashCh47", _ins._config.Prefix, _ins._config.Cctv);
                        StageCh47++;
                    }
                }
            }

            private void SpawnNewCh47(Vector3 pos, Quaternion rot, Vector3 landingTarget)
            {
                CH47Helicopter ch47New = GameManager.server.CreateEntity("assets/prefabs/npc/ch47/ch47scientists.entity.prefab", pos, rot) as CH47Helicopter;
                CH47HelicopterAIController ch47AInew = ch47New.GetComponent<CH47HelicopterAIController>();
                ch47AInew.SetLandingTarget(landingTarget);
                if (Ch47.IsExists()) Ch47.Kill();
                Ch47 = ch47New;
                _ch47Ai = ch47AInew;
                Ch47.Spawn();
                _ch47Ai.CancelInvoke(_ch47Ai.SpawnScientists);
                Ch47.rigidBody.detectCollisions = false;
                _ch47Ai.numCrates = 0;
                _ch47Ai.SetMinHoverHeight(0f);
            }

            private void SpawnWorkcart()
            {
                Vector3 pos; Quaternion rot;
                GetGlobal(transform, new Vector3(-27.654f, 0.282f, 116.994f), new Vector3(0f, 225f, 0f), out pos, out rot);
                Train = GameManager.server.CreateEntity("assets/content/vehicles/workcart/workcart.entity.prefab", pos, rot) as TrainEngine;
                Train.enableSaving = false;
                Train.Spawn();
                Train.decayDuration = 12000f;
                Train.GetFuelSystem().cachedHasFuel = true;
                Train.GetFuelSystem().nextFuelCheckTime = float.MaxValue;
                StartMoveTrain();
                InvokeRepeating(UpdateWorkcart, 0f, 1f);
                _ins.AlertToAllPlayers("StartWorkcart", _ins._config.Prefix);
            }

            private void UpdateWorkcart()
            {
                if (Vector3.Distance(Train.transform.position, _finishTrainPos) < 5f)
                {
                    CancelInvoke(UpdateWorkcart);
                    FinishMoveTrain();
                    Invoke(SpawnTrainEntities, 2.5f);
                    foreach (PresetConfig preset in _ins._config.NpcTrain) SpawnPreset(preset);
                    if (_ins._config.PveMode.Pve && _ins.plugins.Exists("PveMode")) _ins.PveMode.Call("EventAddScientists", _ins.Name, Scientists.Select(x => x.net.ID));
                    _ins.AlertToAllPlayers("FinishWorkcart", _ins._config.Prefix, _ins._config.WaterAmount);
                }
            }

            private void SpawnConductor()
            {
                Conductor = GameManager.server.CreateEntity("assets/prefabs/npc/bandit/shopkeepers/bandit_shopkeeper.prefab", Train.transform.position, Train.transform.rotation) as NPCShopKeeper;
                Conductor.enableSaving = false;
                Conductor.Spawn();
                Conductor.CancelInvoke(Conductor.Greeting);
                Conductor.CancelInvoke(Conductor.TickMovement);
                Train.mountPoints[0].mountable.AttemptMount(Conductor, false);
            }

            private void StartMoveTrain()
            {
                SpawnConductor();
                Train.engineController.TryStartEngine(Conductor);
                Train.SetThrottle(TrainEngine.EngineSpeeds.Fwd_Lo);
            }

            private void FinishMoveTrain()
            {
                Train.SetThrottle(TrainEngine.EngineSpeeds.Zero);
                Train.engineController.StopEngine();
                if (Conductor.IsExists()) Conductor.Kill();
            }

            private void SpawnTrainEntities()
            {
                Vector3 pos; Quaternion rot;

                GetGlobal(Train.transform, new Vector3(0.853f, 1.422f, -1.196f), new Vector3(0f, 270f, 0f), out pos, out rot);
                _trainWaterBarrel = GameManager.server.CreateEntity("assets/prefabs/deployable/liquidbarrel/waterbarrel.prefab", pos, rot) as LiquidContainer;
                _trainWaterBarrel.enableSaving = false;
                _trainWaterBarrel.startingAmount = 20000;
                _trainWaterBarrel.Spawn();
                RemoveColliderProtection(_trainWaterBarrel);
                _trainWaterBarrel.SendNetworkUpdateImmediate();
                Entities.Add(_trainWaterBarrel);

                GetGlobal(Train.transform, new Vector3(0.888f, 2.604f, 0.836f), new Vector3(0f, 270f, 0f), out pos, out rot);
                TrainCrate = GameManager.server.CreateEntity("assets/bundled/prefabs/radtown/underwater_labs/crate_normal.prefab", pos, rot) as LootContainer;
                TrainCrate.enableSaving = false;
                TrainCrate.Spawn();
                RemoveColliderProtection(TrainCrate);
                TrainCrate.SendNetworkUpdateImmediate();
                _ins.NextTick(() =>
                {
                    TrainCrate.inventory.ClearItemsContainer();
                    for (int i = 0; i < 6; i++)
                    {
                        Item item = ItemManager.CreateByName("waterjug");
                        if (!item.MoveToContainer(TrainCrate.inventory)) item.Remove();
                    }
                });
            }

            private static void RemoveColliderProtection(BaseEntity entity)
            {
                foreach (MeshCollider meshCollider in entity.GetComponentsInChildren<MeshCollider>()) UnityEngine.Object.DestroyImmediate(meshCollider);
                UnityEngine.Object.DestroyImmediate(entity.GetComponent<GroundWatch>());
                UnityEngine.Object.DestroyImmediate(entity.GetComponent<DestroyOnGroundMissing>());
            }

            internal void Extinguish()
            {
                if (IsExtinguish) return;
                IsExtinguish = true;
                foreach (Sprinkler sprinkler in _sprinklers) sprinkler.SetFlag(BaseEntity.Flags.On, true);
                foreach (FireBall fireBall in _fireBalls) if (fireBall.IsExists()) fireBall.Kill();
                foreach (LootContainer crate in Crates) crate.SetFlag(BaseEntity.Flags.Locked, false);
                foreach (HackableLockedCrate crate in HackCrates) crate.SetFlag(BaseEntity.Flags.Locked, false);
                Item item = WaterBarrel.GetLiquidItem();
                if (item.amount == _ins._config.WaterAmount) item.Remove();
                else if (item.amount > _ins._config.WaterAmount)
                {
                    item.amount -= _ins._config.WaterAmount;
                    item.MarkDirty();
                }
            }

            private FireBall SpawnFireball(BaseEntity parent, object offset = null, float lifetime = 0)
            {
                FireBall fireBall = GameManager.server.CreateEntity("assets/bundled/prefabs/oilfireballsmall.prefab", parent.transform.position) as FireBall;
                if (fireBall)
                {
                    if (offset is Vector3) fireBall.transform.localPosition = (Vector3)offset;
                    fireBall.Spawn();
                    fireBall.SetParent(parent, false, true);
                    fireBall.GetComponent<Rigidbody>().isKinematic = true;
                    fireBall.GetComponent<Collider>().enabled = false;
                    fireBall.Invoke(fireBall.Extinguish, lifetime == 0 ? TimeToFinish : lifetime);
                    return fireBall;
                }
                return null;
            }

            private static void RunEffect(string name, BaseEntity entity = null, Vector3 position = new Vector3(), Vector3 offset = new Vector3())
            {
                if (entity != null) Effect.server.Run(name, entity, 0, offset, position, null, true);
                else Effect.server.Run(name, position, Vector3.up, null, true);
            }
        }
        #endregion Controller

        #region Spawn Loot
        #region NPC
        private void OnCorpsePopulate(ScientistNPC entity, NPCPlayerCorpse corpse)
        {
            if (entity == null) return;
            if (_controller.Scientists.Contains(entity))
            {
                _controller.Scientists.Remove(entity);
                PresetConfig preset = _config.NpcStart.FirstOrDefault(x => x.Config.Name == entity.displayName);
                if (preset == null) preset = _config.NpcTrain.FirstOrDefault(x => x.Config.Name == entity.displayName);
                if (preset == null) preset = _config.NpcButton.FirstOrDefault(x => x.Config.Name == entity.displayName);
                NextTick(() =>
                {
                    if (corpse == null) return;
                    ItemContainer container = corpse.containers[0];
                    if (preset.TypeLootTable == 0)
                    {
                        for (int i = container.itemList.Count - 1; i >= 0; i--)
                        {
                            Item item = container.itemList[i];
                            if (preset.Config.WearItems.Any(x => x.ShortName == item.info.shortname))
                            {
                                item.RemoveFromContainer();
                                item.Remove();
                            }
                        }
                        return;
                    }
                    if (preset.TypeLootTable == 2 || preset.TypeLootTable == 3)
                    {
                        if (preset.Config.IsRemoveCorpse && !corpse.IsDestroyed) corpse.Kill();
                        return;
                    }
                    container.ClearItemsContainer();
                    if (preset.TypeLootTable == 4 || preset.TypeLootTable == 5) AddToContainerPrefab(container, preset.PrefabLootTable);
                    if (preset.TypeLootTable == 1 || preset.TypeLootTable == 5) AddToContainerItem(container, preset.OwnLootTable);
                    if (preset.Config.IsRemoveCorpse && !corpse.IsDestroyed) corpse.Kill();
                });
            }
        }

        private object CanPopulateLoot(ScientistNPC entity, NPCPlayerCorpse corpse)
        {
            if (entity == null || corpse == null || _controller == null) return null;
            if (_controller.Scientists.Contains(entity))
            {
                PresetConfig preset = _config.NpcStart.FirstOrDefault(x => x.Config.Name == entity.displayName);
                if (preset == null) preset = _config.NpcTrain.FirstOrDefault(x => x.Config.Name == entity.displayName);
                if (preset == null) preset = _config.NpcButton.FirstOrDefault(x => x.Config.Name == entity.displayName);
                if (preset.TypeLootTable == 2) return null;
                else return true;
            }
            return null;
        }

        private object OnCustomLootNPC(uint netID)
        {
            if (_controller == null) return null;
            ScientistNPC entity = _controller.Scientists.FirstOrDefault(x => x.IsExists() && x.net.ID == netID);
            if (entity != null)
            {
                PresetConfig preset = _config.NpcStart.FirstOrDefault(x => x.Config.Name == entity.displayName);
                if (preset == null) preset = _config.NpcTrain.FirstOrDefault(x => x.Config.Name == entity.displayName);
                if (preset == null) preset = _config.NpcButton.FirstOrDefault(x => x.Config.Name == entity.displayName);
                if (preset.TypeLootTable == 3) return null;
                else return true;
            }
            return null;
        }
        #endregion NPC

        #region Crates
        private object CanPopulateLoot(LootContainer container)
        {
            if (container == null || _controller == null) return null;
            if (container == _controller.TrainCrate) return true;
            else if (_controller.Crates.Contains(container))
            {
                if (_config.TypeLootTableCrates == 2) return null;
                else return true;
            }
            else if (container is HackableLockedCrate && _controller.HackCrates.Contains(container as HackableLockedCrate))
            {
                if (_config.HackCrate.TypeLootTable == 2) return null;
                else return true;
            }
            else return null;
        }

        private object OnCustomLootContainer(uint netID)
        {
            if (_controller == null) return null;
            if (_controller.TrainCrate.IsExists() && _controller.TrainCrate.net.ID == netID) return true;
            else if (_controller.Crates.Any(x => x.IsExists() && x.net.ID == netID))
            {
                if (_config.TypeLootTableCrates == 3) return null;
                else return true;
            }
            else if (_controller.HackCrates.Any(x => x.IsExists() && x.net.ID == netID))
            {
                if (_config.HackCrate.TypeLootTable == 3) return null;
                else return true;
            }
            return null;
        }
        #endregion Crates

        private void AddToContainerPrefab(ItemContainer container, PrefabLootTableConfig lootTable)
        {
            if (lootTable.UseCount)
            {
                int count = UnityEngine.Random.Range(lootTable.Min, lootTable.Max + 1);
                HashSet<string> prefabsInContainer = new HashSet<string>();
                while (prefabsInContainer.Count < count)
                {
                    foreach (PrefabConfig prefab in lootTable.Prefabs)
                    {
                        if (prefabsInContainer.Contains(prefab.PrefabDefinition)) continue;
                        if (UnityEngine.Random.Range(0.0f, 100.0f) <= prefab.Chance)
                        {
                            if (_allLootSpawnSlots.ContainsKey(prefab.PrefabDefinition))
                            {
                                LootContainer.LootSpawnSlot[] lootSpawnSlots = _allLootSpawnSlots[prefab.PrefabDefinition];
                                foreach (LootContainer.LootSpawnSlot lootSpawnSlot in lootSpawnSlots)
                                    for (int j = 0; j < lootSpawnSlot.numberToSpawn; j++)
                                        if (UnityEngine.Random.Range(0f, 1f) <= lootSpawnSlot.probability)
                                            lootSpawnSlot.definition.SpawnIntoContainer(container);
                            }
                            else _allLootSpawn[prefab.PrefabDefinition].SpawnIntoContainer(container);
                            prefabsInContainer.Add(prefab.PrefabDefinition);
                            if (prefabsInContainer.Count == count) return;
                        }
                    }
                }
            }
            else
            {
                HashSet<string> prefabsInContainer = new HashSet<string>();
                foreach (PrefabConfig prefab in lootTable.Prefabs)
                {
                    if (prefabsInContainer.Contains(prefab.PrefabDefinition)) continue;
                    if (UnityEngine.Random.Range(0.0f, 100.0f) <= prefab.Chance)
                    {
                        if (_allLootSpawnSlots.ContainsKey(prefab.PrefabDefinition))
                        {
                            LootContainer.LootSpawnSlot[] lootSpawnSlots = _allLootSpawnSlots[prefab.PrefabDefinition];
                            foreach (LootContainer.LootSpawnSlot lootSpawnSlot in lootSpawnSlots)
                                for (int j = 0; j < lootSpawnSlot.numberToSpawn; j++)
                                    if (UnityEngine.Random.Range(0f, 1f) <= lootSpawnSlot.probability)
                                        lootSpawnSlot.definition.SpawnIntoContainer(container);
                        }
                        else _allLootSpawn[prefab.PrefabDefinition].SpawnIntoContainer(container);
                        prefabsInContainer.Add(prefab.PrefabDefinition);
                    }
                }
            }
        }

        private void AddToContainerItem(ItemContainer container, LootTableConfig lootTable)
        {
            if (lootTable.UseCount)
            {
                int count = UnityEngine.Random.Range(lootTable.Min, lootTable.Max + 1);
                HashSet<int> indexMove = new HashSet<int>();
                while (indexMove.Count < count)
                {
                    foreach (ItemConfig item in lootTable.Items)
                    {
                        if (indexMove.Contains(lootTable.Items.IndexOf(item))) continue;
                        if (UnityEngine.Random.Range(0.0f, 100.0f) <= item.Chance)
                        {
                            Item newItem = item.IsBluePrint ? ItemManager.CreateByName("blueprintbase") : ItemManager.CreateByName(item.ShortName, UnityEngine.Random.Range(item.MinAmount, item.MaxAmount + 1), item.SkinID);
                            if (newItem == null)
                            {
                                PrintWarning($"Failed to create item! ({item.ShortName})");
                                continue;
                            }
                            if (item.IsBluePrint) newItem.blueprintTarget = ItemManager.FindItemDefinition(item.ShortName).itemid;
                            if (!string.IsNullOrEmpty(item.Name)) newItem.name = item.Name;
                            if (container.capacity < container.itemList.Count + 1) container.capacity++;
                            if (!newItem.MoveToContainer(container)) newItem.Remove();
                            else
                            {
                                indexMove.Add(lootTable.Items.IndexOf(item));
                                if (indexMove.Count == count) return;
                            }
                        }
                    }
                }
            }
            else
            {
                HashSet<int> indexMove = new HashSet<int>();
                foreach (ItemConfig item in lootTable.Items)
                {
                    if (indexMove.Contains(lootTable.Items.IndexOf(item))) continue;
                    if (UnityEngine.Random.Range(0.0f, 100.0f) <= item.Chance)
                    {
                        Item newItem = item.IsBluePrint ? ItemManager.CreateByName("blueprintbase") : ItemManager.CreateByName(item.ShortName, UnityEngine.Random.Range(item.MinAmount, item.MaxAmount + 1), item.SkinID);
                        if (newItem == null)
                        {
                            PrintWarning($"Failed to create item! ({item.ShortName})");
                            continue;
                        }
                        if (item.IsBluePrint) newItem.blueprintTarget = ItemManager.FindItemDefinition(item.ShortName).itemid;
                        if (!string.IsNullOrEmpty(item.Name)) newItem.name = item.Name;
                        if (container.capacity < container.itemList.Count + 1) container.capacity++;
                        if (!newItem.MoveToContainer(container)) newItem.Remove();
                        else indexMove.Add(lootTable.Items.IndexOf(item));
                    }
                }
            }
        }

        private void CheckAllLootTables()
        {
            foreach (CrateConfig crateConfig in _config.DefaultCrates)
            {
                CheckLootTable(crateConfig.OwnLootTable);
                CheckPrefabLootTable(crateConfig.PrefabLootTable);
            }

            CheckLootTable(_config.HackCrate.OwnLootTable);
            CheckPrefabLootTable(_config.HackCrate.PrefabLootTable);

            foreach (PresetConfig preset in _config.NpcStart)
            {
                CheckLootTable(preset.OwnLootTable);
                CheckPrefabLootTable(preset.PrefabLootTable);
            }
            foreach (PresetConfig preset in _config.NpcTrain)
            {
                CheckLootTable(preset.OwnLootTable);
                CheckPrefabLootTable(preset.PrefabLootTable);
            }
            foreach (PresetConfig preset in _config.NpcButton)
            {
                CheckLootTable(preset.OwnLootTable);
                CheckPrefabLootTable(preset.PrefabLootTable);
            }

            SaveConfig();
        }

        private void CheckLootTable(LootTableConfig lootTable)
        {
            lootTable.Items = lootTable.Items.OrderBy(x => x.Chance);
            if (lootTable.Max > lootTable.Items.Count) lootTable.Max = lootTable.Items.Count;
            if (lootTable.Min > lootTable.Max) lootTable.Min = lootTable.Max;
        }

        private void CheckPrefabLootTable(PrefabLootTableConfig lootTable)
        {
            HashSet<PrefabConfig> prefabs = new HashSet<PrefabConfig>();
            foreach (PrefabConfig prefabConfig in lootTable.Prefabs)
            {
                if (prefabs.Any(x => x.PrefabDefinition == prefabConfig.PrefabDefinition)) PrintWarning($"Duplicate prefab removed from loot table! ({prefabConfig.PrefabDefinition})");
                else
                {
                    GameObject gameObject = GameManager.server.FindPrefab(prefabConfig.PrefabDefinition);
                    global::HumanNPC humanNpc = gameObject.GetComponent<global::HumanNPC>();
                    ScarecrowNPC scarecrowNPC = gameObject.GetComponent<ScarecrowNPC>();
                    LootContainer lootContainer = gameObject.GetComponent<LootContainer>();
                    if (humanNpc != null && humanNpc.LootSpawnSlots.Length != 0)
                    {
                        if (!_allLootSpawnSlots.ContainsKey(prefabConfig.PrefabDefinition)) _allLootSpawnSlots.Add(prefabConfig.PrefabDefinition, humanNpc.LootSpawnSlots);
                        prefabs.Add(prefabConfig);
                    }
                    else if (scarecrowNPC != null && scarecrowNPC.LootSpawnSlots.Length != 0)
                    {
                        if (!_allLootSpawnSlots.ContainsKey(prefabConfig.PrefabDefinition)) _allLootSpawnSlots.Add(prefabConfig.PrefabDefinition, scarecrowNPC.LootSpawnSlots);
                        prefabs.Add(prefabConfig);
                    }
                    else if (lootContainer != null && lootContainer.LootSpawnSlots.Length != 0)
                    {
                        if (!_allLootSpawnSlots.ContainsKey(prefabConfig.PrefabDefinition)) _allLootSpawnSlots.Add(prefabConfig.PrefabDefinition, lootContainer.LootSpawnSlots);
                        prefabs.Add(prefabConfig);
                    }
                    else if (lootContainer != null && lootContainer.lootDefinition != null)
                    {
                        if (!_allLootSpawn.ContainsKey(prefabConfig.PrefabDefinition)) _allLootSpawn.Add(prefabConfig.PrefabDefinition, lootContainer.lootDefinition);
                        prefabs.Add(prefabConfig);
                    }
                    else PrintWarning($"Unknown prefab removed! ({prefabConfig.PrefabDefinition})");
                }
            }
            lootTable.Prefabs = prefabs.OrderBy(x => x.Chance);
            if (lootTable.Max > lootTable.Prefabs.Count) lootTable.Max = lootTable.Prefabs.Count;
            if (lootTable.Min > lootTable.Max) lootTable.Min = lootTable.Max;
        }

        private readonly Dictionary<string, LootSpawn> _allLootSpawn = new Dictionary<string, LootSpawn>();

        private readonly Dictionary<string, LootContainer.LootSpawnSlot[]> _allLootSpawnSlots = new Dictionary<string, LootContainer.LootSpawnSlot[]>();
        #endregion Spawn Loot

        #region TruePVE
        private object CanEntityTakeDamage(BasePlayer victim, HitInfo hitinfo)
        {
            if (!_config.IsCreateZonePvp || victim == null || hitinfo == null || _controller == null) return null;
            BasePlayer attacker = hitinfo.InitiatorPlayer;
            if (_controller.Players.Contains(victim) && (attacker == null || _controller.Players.Contains(attacker))) return true;
            else return null;
        }
        #endregion TruePVE

        #region NTeleportation
        private object CanTeleport(BasePlayer player, Vector3 to)
        {
            if (_config.NTeleportationInterrupt && _controller != null && (_controller.Players.Contains(player) || Vector3.Distance(_controller.transform.position, to) < Radius)) return GetMessage("NTeleportation", player.UserIDString, _config.Prefix);
            else return null;
        }
        #endregion NTeleportation

        #region Economy
        [PluginReference] private readonly Plugin Economics, ServerRewards, IQEconomic;

        private readonly Dictionary<ulong, double> _playersBalance = new Dictionary<ulong, double>();

        private void ActionEconomy(ulong playerId, string type, string arg = "")
        {
            switch (type)
            {
                case "Crates":
                    if (_config.Economy.Crates.ContainsKey(arg)) AddBalance(playerId, _config.Economy.Crates[arg]);
                    break;
                case "Npc":
                    AddBalance(playerId, _config.Economy.Npc);
                    break;
                case "LockedCrate":
                    AddBalance(playerId, _config.Economy.LockedCrate);
                    break;
                case "Button":
                    AddBalance(playerId, _config.Economy.Button);
                    break;
            }
        }

        private void AddBalance(ulong playerId, double balance)
        {
            if (balance == 0) return;
            if (_playersBalance.ContainsKey(playerId)) _playersBalance[playerId] += balance;
            else _playersBalance.Add(playerId, balance);
        }

        private void SendBalance()
        {
            if (_playersBalance.Count == 0) return;
            foreach (KeyValuePair<ulong, double> dic in _playersBalance)
            {
                if (dic.Value < _config.Economy.Min) continue;
                int intCount = Convert.ToInt32(dic.Value);
                if (_config.Economy.Plugins.Contains("Economics") && plugins.Exists("Economics") && dic.Value > 0) Economics.Call("Deposit", dic.Key.ToString(), dic.Value);
                if (_config.Economy.Plugins.Contains("Server Rewards") && plugins.Exists("ServerRewards") && intCount > 0) ServerRewards.Call("AddPoints", dic.Key, intCount);
                if (_config.Economy.Plugins.Contains("IQEconomic") && plugins.Exists("IQEconomic") && intCount > 0) IQEconomic.Call("API_SET_BALANCE", dic.Key, intCount);
                BasePlayer player = BasePlayer.FindByID(dic.Key);
                if (player != null) AlertToPlayer(player, GetMessage("SendEconomy", player.UserIDString, _config.Prefix, dic.Value));
            }
            ulong winnerId = _playersBalance.Max(x => x.Value).Key;
            foreach (string command in _config.Economy.Commands) Server.Command(command.Replace("{steamid}", $"{winnerId}"));
            _playersBalance.Clear();
        }
        #endregion Economy

        #region Alerts
        [PluginReference] private readonly Plugin GUIAnnouncements, DiscordMessages;

        private string ClearColorAndSize(string message)
        {
            message = message.Replace("</color>", string.Empty);
            message = message.Replace("</size>", string.Empty);
            while (message.Contains("<color="))
            {
                int index = message.IndexOf("<color=", StringComparison.Ordinal);
                message = message.Remove(index, message.IndexOf(">", index, StringComparison.Ordinal) - index + 1);
            }
            while (message.Contains("<size="))
            {
                int index = message.IndexOf("<size=", StringComparison.Ordinal);
                message = message.Remove(index, message.IndexOf(">", index, StringComparison.Ordinal) - index + 1);
            }
            message = message.Replace(_config.Prefix + " ", string.Empty);
            return message;
        }

        private bool CanSendDiscordMessage() => _config.Discord.IsDiscord && !string.IsNullOrEmpty(_config.Discord.WebhookUrl) && _config.Discord.WebhookUrl != "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks";

        private void AlertToAllPlayers(string langKey, params object[] args)
        {
            if (CanSendDiscordMessage() && _config.Discord.Keys.Contains(langKey))
            {
                object fields = new[] { new { name = Title, value = ClearColorAndSize(GetMessage(langKey, null, args)), inline = false } };
                DiscordMessages?.Call("API_SendFancyMessage", _config.Discord.WebhookUrl, "", _config.Discord.EmbedColor, JsonConvert.SerializeObject(fields), null, this);
            }
            foreach (BasePlayer player in BasePlayer.activePlayerList) AlertToPlayer(player, GetMessage(langKey, player.UserIDString, args));
        }

        private void AlertToPlayer(BasePlayer player, string message)
        {
            if (_config.IsChat) PrintToChat(player, message);
            if (_config.GuiAnnouncements.IsGuiAnnouncements) GUIAnnouncements?.Call("CreateAnnouncement", ClearColorAndSize(message), _config.GuiAnnouncements.BannerColor, _config.GuiAnnouncements.TextColor, player, _config.GuiAnnouncements.ApiAdjustVPosition);
            if (_config.Notify.IsNotify) player.SendConsoleCommand($"notify.show {_config.Notify.Type} {ClearColorAndSize(message)}");
        }
        #endregion Alerts

        #region GUI
        private void MessageGUI(BasePlayer player, string text)
        {
            CuiHelper.DestroyUi(player, "TextMain");

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = _config.Gui.AnchorMin, AnchorMax = _config.Gui.AnchorMax },
                CursorEnabled = false,
            }, "Hud", "TextMain");
            container.Add(new CuiElement
            {
                Parent = "TextMain",
                Components =
                {
                    new CuiTextComponent() { Color = "1 1 1 1", FadeIn = 0f, Text = text, FontSize = 24, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                    new CuiOutlineComponent { Distance = "1 1", Color = "0 0 0 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            CuiHelper.AddUi(player, container);
        }
        #endregion GUI

        #region Helpers
        [PluginReference] private readonly Plugin NpcSpawn, BetterNpc, PveMode;

        internal float Radius = 140f;

        private readonly HashSet<string> _hooks = new HashSet<string>
        {
            "OnEntityTakeDamage",
            "CanPickupEntity",
            "OnStructureRotate",
            "OnEntityGroundMissing",
            "OnEntityDeath",
            "CanMountEntity",
            "CanHackCrate",
            "OnCrateHack",
            "OnButtonPress",
            "OnNpcTarget",
            "OnLootEntity",
            "OnCorpsePopulate",
            "CanPopulateLoot",
            "OnCustomLootNPC",
            "OnCustomLootContainer",
            "CanEntityTakeDamage",
            "CanTeleport"
        };

        private void Unsubscribes() { foreach (string hook in _hooks) Unsubscribe(hook); }

        private void Subscribes()
        {
            foreach (string hook in _hooks)
            {
                if (hook == "CanEntityTakeDamage" && !_config.IsCreateZonePvp) continue;
                if (hook == "CanTeleport" && !_config.NTeleportationInterrupt) continue;
                Subscribe(hook);
            }
        }

        internal class Location { public Vector3 pos; public Vector3 rot; }

        internal List<Location> StartLocations = new List<Location>();
        #endregion Helpers

        #region Commands
        [ChatCommand("ppstart")]
        private void ChatStartEvent(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                if (!_active) Start();
                else PrintToChat(player, GetMessage("EventActive", player.UserIDString, _config.Prefix));
            }
        }

        [ChatCommand("ppstop")]
        private void ChatStopEvent(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                if (_controller != null) Finish();
                else Server.Command($"o.reload {Name}");
            }
        }

        [ChatCommand("pppos")]
        private void ChatCommandPos(BasePlayer player)
        {
            if (!player.IsAdmin || _controller == null) return;
            Vector3 pos = _controller.transform.InverseTransformPoint(player.transform.position);
            Vector3 rot = player.viewAngles - _controller.transform.rotation.eulerAngles;
            Puts($"Position: {pos}. Rotation: {rot}");
            PrintToChat(player, $"Position: {pos}\nRotation: {rot}");
        }

        [ConsoleCommand("ppstart")]
        private void ConsoleStartEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
            {
                if (!_active) Start();
                else Puts("This event is active now. To finish this event (ppstop), then to start the next one");
            }
        }

        [ConsoleCommand("ppstop")]
        private void ConsoleStopEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
            {
                if (_controller != null) Finish();
                else Server.Command($"o.reload {Name}");
            }
        }
        #endregion Commands
    }
}

namespace Oxide.Plugins.PowerPlantEventExtensionMethods
{
    public static class ExtensionMethods
    {
        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return true;
            return false;
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return enumerator.Current;
            return default(TSource);
        }

        #region Select
        public static HashSet<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> predicate)
        {
            HashSet<TResult> result = new HashSet<TResult>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(predicate(enumerator.Current));
            return result;
        }

        public static List<TResult> Select<TSource, TResult>(this IList<TSource> source, Func<TSource, TResult> predicate)
        {
            List<TResult> result = new List<TResult>();
            for (int i = 0; i < source.Count; i++)
            {
                TSource element = source[i];
                result.Add(predicate(element));
            }
            return result;
        }
        #endregion Select

        public static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
        {
            List<TSource> result = new List<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(enumerator.Current);
            return result;
        }

        public static TSource Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double> predicate)
        {
            TSource result = source.ElementAt(0);
            double resultValue = predicate(result);
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    TSource element = enumerator.Current;
                    double elementValue = predicate(element);
                    if (elementValue > resultValue)
                    {
                        result = element;
                        resultValue = elementValue;
                    }
                }
            }
            return result;
        }

        public static List<TSource> OrderBy<TSource>(this IEnumerable<TSource> source, Func<TSource, float> predicate)
        {
            List<TSource> result = source.ToList();
            for (int i = 0; i < result.Count; i++)
            {
                for (int j = 0; j < result.Count - 1; j++)
                {
                    if (predicate(result[j]) > predicate(result[j + 1]))
                    {
                        TSource z = result[j];
                        result[j] = result[j + 1];
                        result[j + 1] = z;
                    }
                }
            }
            return result;
        }

        public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, int index)
        {
            int movements = 0;
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (movements == index) return enumerator.Current;
                    movements++;
                }
            }
            return default(TSource);
        }

        public static bool IsPlayer(this BasePlayer player) => player != null && player.userID.IsSteamId();

        public static bool IsExists(this BaseNetworkable entity) => entity != null && !entity.IsDestroyed;

        public static void ClearItemsContainer(this ItemContainer container)
        {
            for (int i = container.itemList.Count - 1; i >= 0; i--)
            {
                Item item = container.itemList[i];
                item.RemoveFromContainer();
                item.Remove();
            }
        }
    }
}