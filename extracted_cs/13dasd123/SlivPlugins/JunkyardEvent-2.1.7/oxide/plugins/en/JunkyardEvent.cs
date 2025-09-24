using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.IO;
using System.Reflection;
using Facepunch;
using Oxide.Plugins.JunkyardEventExtensionMethods;

namespace Oxide.Plugins
{
    [Info("JunkyardEvent", "KpucTaJl", "2.1.7")]
    internal class JunkyardEvent : RustPlugin
    {
        #region Config
        private const bool En = true;

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
            if (_config.PluginVersion < new VersionNumber(2, 0, 4))
            {
                _config.Gui = new GuiConfig
                {
                    IsGui = true,
                    OffsetMinY = "-56"
                };
                foreach (PresetConfig preset in _config.PresetsNpc) foreach (NpcBelt belt in preset.Config.BeltItems) belt.Ammo = string.Empty;
                foreach (NpcBelt belt in _config.NpcTruck.Config.BeltItems) belt.Ammo = string.Empty;
            }
            if (_config.PluginVersion < new VersionNumber(2, 0, 6))
            {
                _config.Commands = new HashSet<string>
                {
                    "/remove",
                    "remove.toggle"
                };
            }
            if (_config.PluginVersion < new VersionNumber(2, 0, 7))
            {
                _config.Radius = 100f;
            }
            if (_config.PluginVersion < new VersionNumber(2, 1, 1))
            {
                _config.MainPoint = new PointConfig
                {
                    Enabled = true,
                    Text = "◈",
                    Size = 45,
                    Color = "#CCFF00"
                };
                _config.AdditionalPoint = new PointConfig
                {
                    Enabled = true,
                    Text = "◆",
                    Size = 25,
                    Color = "#FFC700"
                };
            }
            if (_config.PluginVersion < new VersionNumber(2, 1, 3))
            {
                _config.IsFuelCrane = true;
            }
            if (_config.PluginVersion < new VersionNumber(2, 1, 4))
            {
                _config.GameTip = new GameTipConfig
                {
                    IsGameTip = false,
                    Style = 2
                };
                _config.Marker = new MarkerConfig
                {
                    Enabled = true,
                    Type = 1,
                    Radius = 0.37967f,
                    Alpha = 0.35f,
                    Color = new ColorConfig { R = 0.81f, G = 0.25f, B = 0.15f },
                    Text = "JunkyardEvent"
                };
            }
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
            [JsonProperty("SkinID (0 - default)")] public ulong SkinId { get; set; }
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
            [JsonProperty(En ? "Number of crates" : "Кол-во ящиков")] public int Count { get; set; }
            [JsonProperty(En ? "Which loot table should the plugin use? (0 - default; 1 - own; 2 - AlphaLoot; 3 - CustomLoot; 4 - loot table of the Rust objects; 5 - combine the 1 and 4 methods)" : "Какую таблицу лута необходимо использовать? (0 - стандартную; 1 - собственную; 2 - AlphaLoot; 3 - CustomLoot; 4 - таблица предметов объектов Rust; 5 - совместить 1 и 4 методы)")] public int TypeLootTable { get; set; }
            [JsonProperty(En ? "Loot table from prefabs (if the loot table type is 4 or 5)" : "Таблица предметов из prefab-ов (если тип таблицы предметов - 4 или 5)")] public PrefabLootTableConfig PrefabLootTable { get; set; }
            [JsonProperty(En ? "Own loot table (if the loot table type is 1 or 5)" : "Собственная таблица предметов (если тип таблицы предметов - 1 или 5)")] public LootTableConfig OwnLootTable { get; set; }
        }

        public class HackCrateConfig
        {
            [JsonProperty(En ? "Number of crates" : "Кол-во ящиков")] public int Count { get; set; }
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
            [JsonProperty(En ? "Use map marker? [true/false]" : "Использовать маркер на карте? [true/false]")] public bool Enabled { get; set; }
            [JsonProperty(En ? "Type (0 - simple, 1 - advanced)" : "Тип (0 - упрощенный, 1 - расширенный)")] public int Type { get; set; }
            [JsonProperty(En ? "Background radius (if the marker type is 0)" : "Радиус фона (если тип маркера - 0)")] public float Radius { get; set; }
            [JsonProperty(En ? "Background transparency" : "Прозрачность фона")] public float Alpha { get; set; }
            [JsonProperty(En ? "Color" : "Цвет")] public ColorConfig Color { get; set; }
            [JsonProperty(En ? "Text" : "Текст")] public string Text { get; set; }
        }

        public class PointConfig
        {
            [JsonProperty(En ? "Enabled? [true/false]" : "Включен? [true/false]")] public bool Enabled { get; set; }
            [JsonProperty(En ? "Text" : "Текст")] public string Text { get; set; }
            [JsonProperty(En ? "Size" : "Размер")] public int Size { get; set; }
            [JsonProperty(En ? "Color" : "Цвет")] public string Color { get; set; }
        }

        public class GuiConfig
        {
            [JsonProperty(En ? "Do you use the countdown GUI? [true/false]" : "Использовать ли GUI обратного отсчета? [true/false]")] public bool IsGui { get; set; }
            [JsonProperty("OffsetMin Y")] public string OffsetMinY { get; set; }
        }

        public class GameTipConfig
        {
            [JsonProperty(En ? "Use Facepunch Game Tips (notification bar above hotbar)? [true/false]" : "Использовать ли Facepunch Game Tip (оповещения над слотами быстрого доступа игрока)? [true/false]")] public bool IsGameTip { get; set; }
            [JsonProperty(En ? "Style (0 - Blue Normal, 1 - Red Normal, 2 - Blue Long, 3 - Blue Short, 4 - Server Event)" : "Стиль (0 - Blue Normal, 1 - Red Normal, 2 - Blue Long, 3 - Blue Short, 4 - Server Event)")] public int Style { get; set; }
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

        public class EconomyConfig
        {
            [JsonProperty(En ? "Which economy plugins do you want to use? (Economics, Server Rewards, IQEconomic)" : "Какие плагины экономики вы хотите использовать? (Economics, Server Rewards, IQEconomic)")] public HashSet<string> Plugins { get; set; }
            [JsonProperty(En ? "The minimum value that a player must collect to get points for the economy" : "Минимальное значение, которое игрок должен заработать, чтобы получить баллы за экономику")] public double Min { get; set; }
            [JsonProperty(En ? "Looting of crates" : "Ограбление ящиков")] public Dictionary<string, double> Crates { get; set; }
            [JsonProperty(En ? "Killing an NPC" : "Убийство NPC")] public double Npc { get; set; }
            [JsonProperty(En ? "Hacking a locked crate" : "Взлом заблокированного ящика")] public double LockedCrate { get; set; }
            [JsonProperty(En ? "Recycling car in a shredder" : "Переработка машины в шредере")] public double ShredderCar { get; set; }
            [JsonProperty(En ? "Recycling truck in a shredder" : "Переработка грузовика в шредере")] public double ShredderTruck { get; set; }
            [JsonProperty(En ? "List of commands that are executed in the console at the end of the event ({steamid} - the player who collected the highest number of points)" : "Список команд, которые выполняются в консоли по окончанию ивента ({steamid} - игрок, который набрал наибольшее кол-во баллов)")] public HashSet<string> Commands { get; set; }
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
            [JsonProperty("SkinID (0 - default)")] public ulong SkinId { get; set; }
            [JsonProperty(En ? "Mods" : "Модификации на оружие")] public HashSet<string> Mods { get; set; }
            [JsonProperty(En ? "Ammo" : "Боеприпасы")] public string Ammo { get; set; }
        }

        public class NpcWear
        {
            [JsonProperty("ShortName")] public string ShortName { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong SkinId { get; set; }
        }

        public class PresetConfig
        {
            [JsonProperty(En ? "Minimum" : "Минимальное кол-во")] public int Min { get; set; }
            [JsonProperty(En ? "Maximum" : "Максимальное кол-во")] public int Max { get; set; }
            [JsonProperty(En ? "List of locations" : "Список расположений")] public HashSet<string> Positions { get; set; }
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

        private class PluginConfig
        {
            [JsonProperty(En ? "Minimum time between events [sec.]" : "Минимальное время между ивентами [sec.]")] public float MinStartTime { get; set; }
            [JsonProperty(En ? "Maximum time between events [sec.]" : "Максимальное время между ивентами [sec.]")] public float MaxStartTime { get; set; }
            [JsonProperty(En ? "Is active the timer on to start the event? [true/false]" : "Активен ли таймер для запуска ивента? [true/false]")] public bool EnabledTimer { get; set; }
            [JsonProperty(En ? "Duration of the event [sec.]" : "Время проведения ивента [sec.]")] public int FinishTime { get; set; }
            [JsonProperty(En ? "Time before the starting of the event after receiving a chat message [sec.]" : "Время до начала ивента после сообщения в чате [sec.]")] public float PreStartTime { get; set; }
            [JsonProperty(En ? "Notification time until the end of the event [sec.]" : "Время оповещения до окончания ивента [sec.]")] public int PreFinishTime { get; set; }
            [JsonProperty(En ? "Bradley Crates setting" : "Настройка ящиков Bradley")] public CrateConfig BradleyCrates { get; set; }
            [JsonProperty(En ? "Helicopter Crates setting" : "Настройка ящиков вертолета")] public CrateConfig HeliCrates { get; set; }
            [JsonProperty(En ? "Locked Crates setting" : "Настройка заблокированных ящиков")] public HackCrateConfig HackCrates { get; set; }
            [JsonProperty(En ? "Settings of all NPC presets when start the event" : "Настройки всех пресетов NPC при запуске ивента")] public HashSet<PresetConfig> PresetsNpc { get; set; }
            [JsonProperty(En ? "NPC settings that guard the truck" : "Настройки NPC, которые охраняют грузовик")] public PresetConfig NpcTruck { get; set; }
            [JsonProperty(En ? "Marker configuration on the map" : "Настройка маркера на карте")] public MarkerConfig Marker { get; set; }
            [JsonProperty(En ? "Main marker settings for key event points shown on players screen" : "Настройки основного маркера на экране игрока")] public PointConfig MainPoint { get; set; }
            [JsonProperty(En ? "Additional marker settings for key event points shown on players screen" : "Настройки дополнительного маркера на экране игрока")] public PointConfig AdditionalPoint { get; set; }
            [JsonProperty(En ? "GUI setting" : "Настройки GUI")] public GuiConfig Gui { get; set; }
            [JsonProperty(En ? "Prefix of chat messages" : "Префикс сообщений в чате")] public string Prefix { get; set; }
            [JsonProperty(En ? "Do you use the chat? [true/false]" : "Использовать ли чат? [true/false]")] public bool IsChat { get; set; }
            [JsonProperty(En ? "Facepunch Game Tips setting" : "Настройка сообщений Facepunch Game Tip")] public GameTipConfig GameTip { get; set; }
            [JsonProperty(En ? "GUI Announcements setting" : "Настройка GUI Announcements")] public GuiAnnouncementsConfig GuiAnnouncements { get; set; }
            [JsonProperty(En ? "Notify setting" : "Настройка Notify")] public NotifyConfig Notify { get; set; }
            [JsonProperty(En ? "Discord setting (only for users DiscordMessages plugin)" : "Настройка оповещений в Discord (только для тех, кто использует плагин DiscordMessages)")] public DiscordConfig Discord { get; set; }
            [JsonProperty(En ? "Radius of the event zone" : "Радиус зоны ивента")] public float Radius { get; set; }
            [JsonProperty(En ? "Do you create a PVP zone in the event area? (only for users TruePVE plugin) [true/false]" : "Создавать зону PVP в зоне проведения ивента? (только для тех, кто использует плагин TruePVE) [true/false]")] public bool IsCreateZonePvp { get; set; }
            [JsonProperty(En ? "PVE Mode Setting (only for users PveMode plugin)" : "Настройка PVE режима работы плагина (только для тех, кто использует плагин PveMode)")] public PveModeConfig PveMode { get; set; }
            [JsonProperty(En ? "Interrupt the teleport in the event area? (only for users NTeleportation plugin) [true/false]" : "Запрещать телепорт в зоне проведения ивента? (только для тех, кто использует плагин NTeleportation) [true/false]")] public bool NTeleportationInterrupt { get; set; }
            [JsonProperty(En ? "Disable NPCs from the BetterNpc plugin on the monument while the event is on? [true/false]" : "Отключать NPC из плагина BetterNpc на монументе пока проходит ивент? [true/false]")] public bool RemoveBetterNpc { get; set; }
            [JsonProperty(En ? "Economy setting (total values will be added up and rewarded at the end of the event)" : "Настройка экономики (конечное значение суммируется и будет выдано игрокам по окончанию ивента)")] public EconomyConfig Economy { get; set; }
            [JsonProperty(En ? "List of commands banned in the event zone" : "Список команд запрещенных в зоне ивента")] public HashSet<string> Commands { get; set; }
            [JsonProperty(En ? "Can SAM Site turrets appear in the event zone? [true/false]" : "Должны ли появляться Sam Site турели в зоне ивента? [true/false]")] public bool IsSamSites { get; set; }
            [JsonProperty(En ? "The number of broken cars in the junkyard when the event starts (no more than 15)" : "Кол-во сломанных машин на свалке, когда начинается ивент (не более 15)")] public int CountBrokenCars { get; set; }
            [JsonProperty(En ? "Plane flight speed multiplier" : "Множитель скорости полета самолета")] public float ScaleSpeedPlane { get; set; }
            [JsonProperty(En ? "Should an additional crane spawn? [true/false]" : "Должен ли появляться дополнительный кран? [true/false]")] public bool AdditionalCrane { get; set; }
            [JsonProperty(En ? "Is fuel necessary for crane operation? [true/false]" : "Необходимо ли топливо для работы крана? [true/false]")] public bool IsFuelCrane { get; set; }
            [JsonProperty(En ? "Configuration version" : "Версия конфигурации")] public VersionNumber PluginVersion { get; set; }

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig
                {
                    MinStartTime = 10800f,
                    MaxStartTime = 10800f,
                    EnabledTimer = true,
                    FinishTime = 3600,
                    PreStartTime = 300f,
                    PreFinishTime = 300,
                    BradleyCrates = new CrateConfig
                    {
                        Count = 2,
                        TypeLootTable = 0,
                        PrefabLootTable = new PrefabLootTableConfig
                        {
                            Min = 1,
                            Max = 1,
                            UseCount = true,
                            Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/prefabs/npc/m2bradley/bradley_crate.prefab" } }
                        },
                        OwnLootTable = new LootTableConfig
                        {
                            Min = 1,
                            Max = 1,
                            UseCount = true,
                            Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinId = 0, Name = "" } }
                        }
                    },
                    HeliCrates = new CrateConfig
                    {
                        Count = 2,
                        TypeLootTable = 0,
                        PrefabLootTable = new PrefabLootTableConfig
                        {
                            Min = 1,
                            Max = 1,
                            UseCount = true,
                            Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/prefabs/npc/patrol helicopter/heli_crate.prefab" } }
                        },
                        OwnLootTable = new LootTableConfig
                        {
                            Min = 1,
                            Max = 1,
                            UseCount = true,
                            Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinId = 0, Name = "" } }
                        }
                    },
                    HackCrates = new HackCrateConfig
                    {
                        Count = 1,
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
                            Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinId = 0, Name = "" } }
                        }
                    },
                    PresetsNpc = new HashSet<PresetConfig>
                    {
                        new PresetConfig
                        {
                            Min = 3,
                            Max = 4,
                            Positions = new HashSet<string>
                            {
                                "(50.8, 0.1, 26.4)",
                                "(72.1, 0.1, -27.6)",
                                "(22.2, 0.0, -56.9))",
                                "(-1.0, 0.1, -28.0)",
                                "(22.0, 0.1, -5.4)",
                                "(-10.5, 0.1, 8.0)",
                                "(-42.5, 11.3, 0.6)",
                                "(-31.7, 16.0, -29.2)",
                                "(-45.7, 0.1, 83.2)",
                                "(-64.6, 0.1, 8.8)",
                                "(32.9, 0.1, 71.4)",
                                "(-18.7, 15.0, 36.7)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "Outcast",
                                Health = 100f,
                                RoamRange = 10f,
                                ChaseRange = 50f,
                                AttackRangeMultiplier = 2f,
                                SenseRange = 20f,
                                MemoryDuration = 30f,
                                DamageScale = 0.5f,
                                AimConeScale = 1.8f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 7.5f,
                                DisableRadio = true,
                                Stationary = false,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "mask.bandana", SkinId = 1780166642 },
                                    new NpcWear { ShortName = "hoodie", SkinId = 1780158056 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinId = 1362361447 },
                                    new NpcWear { ShortName = "pants", SkinId = 1780161166 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "pistol.python", Amount = 1, SkinId = 0, Mods = new HashSet<string> { "weapon.mod.flashlight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 10, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
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
                                    new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 50f, IsBluePrint = false, SkinId = 0, Name = "" },
                                    new ItemConfig { ShortName = "syringe.medical", MinAmount = 1, MaxAmount = 2, Chance = 70.0f, IsBluePrint = false, SkinId = 0, Name = "" }
                                }
                            }
                        },
                        new PresetConfig
                        {
                            Min = 2,
                            Max = 2,
                            Positions = new HashSet<string>
                            {
                                "(50.8, 0.1, 26.4)",
                                "(72.1, 0.1, -27.6)",
                                "(22.2, 0.0, -56.9))",
                                "(-1.0, 0.1, -28.0)",
                                "(22.0, 0.1, -5.4)",
                                "(-10.5, 0.1, 8.0)",
                                "(-42.5, 11.3, 0.6)",
                                "(-31.7, 16.0, -29.2)",
                                "(-45.7, 0.1, 83.2)",
                                "(-64.6, 0.1, 8.8)",
                                "(32.9, 0.1, 71.4)",
                                "(-18.7, 15.0, 36.7)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "Scavenger",
                                Health = 130f,
                                RoamRange = 10f,
                                ChaseRange = 100f,
                                AttackRangeMultiplier = 0.5f,
                                SenseRange = 50f,
                                MemoryDuration = 30f,
                                DamageScale = 5f,
                                AimConeScale = 1f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 8.5f,
                                DisableRadio = true,
                                Stationary = false,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "mask.bandana", SkinId = 1780166642 },
                                    new NpcWear { ShortName = "clatter.helmet", SkinId = 0 },
                                    new NpcWear { ShortName = "hoodie", SkinId = 1780158056 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinId = 1362361447 },
                                    new NpcWear { ShortName = "pants", SkinId = 1780161166 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "shotgun.pump", Amount = 1, SkinId = 630162685, Mods = new HashSet<string> { "weapon.mod.flashlight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 10, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
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
                                    new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 50f, IsBluePrint = false, SkinId = 0, Name = "" },
                                    new ItemConfig { ShortName = "syringe.medical", MinAmount = 1, MaxAmount = 2, Chance = 70.0f, IsBluePrint = false, SkinId = 0, Name = "" }
                                }
                            }
                        },
                        new PresetConfig
                        {
                            Min = 3,
                            Max = 4,
                            Positions = new HashSet<string>
                            {
                                "(50.8, 0.1, 26.4)",
                                "(72.1, 0.1, -27.6)",
                                "(22.2, 0.0, -56.9))",
                                "(-1.0, 0.1, -28.0)",
                                "(22.0, 0.1, -5.4)",
                                "(-10.5, 0.1, 8.0)",
                                "(-42.5, 11.3, 0.6)",
                                "(-31.7, 16.0, -29.2)",
                                "(-45.7, 0.1, 83.2)",
                                "(-64.6, 0.1, 8.8)",
                                "(32.9, 0.1, 71.4)",
                                "(-18.7, 15.0, 36.7)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "Nine Toes",
                                Health = 100f,
                                RoamRange = 10f,
                                ChaseRange = 50f,
                                AttackRangeMultiplier = 2f,
                                SenseRange = 20f,
                                MemoryDuration = 30f,
                                DamageScale = 1f,
                                AimConeScale = 2f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 7.5f,
                                DisableRadio = true,
                                Stationary = false,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "mask.bandana", SkinId = 1780166642 },
                                    new NpcWear { ShortName = "hoodie", SkinId = 1780158056 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinId = 1362361447 },
                                    new NpcWear { ShortName = "pants", SkinId = 1780161166 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "pistol.semiauto", Amount = 1, SkinId = 0, Mods = new HashSet<string> { "weapon.mod.flashlight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 10, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
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
                                    new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 50f, IsBluePrint = false, SkinId = 0, Name = "" },
                                    new ItemConfig { ShortName = "syringe.medical", MinAmount = 1, MaxAmount = 2, Chance = 70.0f, IsBluePrint = false, SkinId = 0, Name = "" }
                                }
                            }
                        }
                    },
                    NpcTruck = new PresetConfig
                    {
                        Min = 6,
                        Max = 6,
                        Positions = new HashSet<string>
                        {
                            "(3.5, 0.2, -15.9)",
                            "(-2.1, 0.1, -22.8)",
                            "(-7.8, 0.6, -17.2)",
                            "(-11.3, 0.1, -9.7)",
                            "(-4.2, 0.3, -6.4)",
                            "(4.6, 0.1, -7.8)"
                        },
                        Config = new NpcConfig
                        {
                            Name = "Defenders of the faith",
                            Health = 350f,
                            RoamRange = 10f,
                            ChaseRange = 50f,
                            AttackRangeMultiplier = 2f,
                            SenseRange = 50f,
                            MemoryDuration = 30f,
                            DamageScale = 0.5f,
                            AimConeScale = 1f,
                            CheckVisionCone = false,
                            VisionCone = 135f,
                            Speed = 7.5f,
                            DisableRadio = true,
                            Stationary = false,
                            IsRemoveCorpse = true,
                            WearItems = new HashSet<NpcWear>
                            {
                                new NpcWear { ShortName = "mask.bandana", SkinId = 2017569333 },
                                new NpcWear { ShortName = "metal.facemask", SkinId = 1547235630 },
                                new NpcWear { ShortName = "burlap.shirt", SkinId = 2017554105 },
                                new NpcWear { ShortName = "burlap.gloves", SkinId = 0 },
                                new NpcWear { ShortName = "shoes.boots", SkinId = 0 },
                                new NpcWear { ShortName = "burlap.trousers", SkinId = 2017556850 }
                            },
                            BeltItems = new HashSet<NpcBelt>
                            {
                                new NpcBelt { ShortName = "smg.thompson", Amount = 1, SkinId = 2370519330, Mods = new HashSet<string> { "weapon.mod.flashlight" }, Ammo = string.Empty },
                                new NpcBelt { ShortName = "syringe.medical", Amount = 10, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
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
                                new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 50f, IsBluePrint = false, SkinId = 0, Name = "" },
                                new ItemConfig { ShortName = "syringe.medical", MinAmount = 1, MaxAmount = 2, Chance = 70.0f, IsBluePrint = false, SkinId = 0, Name = "" }
                            }
                        }
                    },
                    Marker = new MarkerConfig
                    {
                        Enabled = true,
                        Type = 1,
                        Radius = 0.37967f,
                        Alpha = 0.35f,
                        Color = new ColorConfig { R = 0.81f, G = 0.25f, B = 0.15f },
                        Text = "JunkyardEvent"
                    },
                    MainPoint = new PointConfig
                    {
                        Enabled = true,
                        Text = "◈",
                        Size = 45,
                        Color = "#CCFF00"
                    },
                    AdditionalPoint = new PointConfig
                    {
                        Enabled = true,
                        Text = "◆",
                        Size = 25,
                        Color = "#FFC700"
                    },
                    Gui = new GuiConfig
                    {
                        IsGui = true,
                        OffsetMinY = "-56"
                    },
                    Prefix = "[JunkyardEvent]",
                    IsChat = true,
                    GameTip = new GameTipConfig
                    {
                        IsGameTip = false,
                        Style = 2
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
                            "KillBrokenCar",
                            "KillTruck",
                            "TruckArrived"
                        }
                    },
                    Radius = 100f,
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
                    NTeleportationInterrupt = true,
                    RemoveBetterNpc = true,
                    Economy = new EconomyConfig
                    {
                        Plugins = new HashSet<string> { "Economics", "Server Rewards", "IQEconomic" },
                        Min = 0,
                        Crates = new Dictionary<string, double>
                        {
                            ["bradley_crate"] = 0.4,
                            ["heli_crate"] = 0.4
                        },
                        Npc = 0.3,
                        LockedCrate = 0.5,
                        ShredderCar = 0.3,
                        ShredderTruck = 0.5,
                        Commands = new HashSet<string>()
                    },
                    Commands = new HashSet<string>
                    {
                        "/remove",
                        "remove.toggle"
                    },
                    IsSamSites = true,
                    CountBrokenCars = 7,
                    ScaleSpeedPlane = 4f,
                    AdditionalCrane = false,
                    IsFuelCrane = true,
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
                ["PreStart"] = "{0} The Rubbish Men <color=#738d43>will arrive</color> at the <color=#55aaff>Junkyard</color> location in <color=#55aaff>{1}</color>!",
                ["Start"] = "{0} The Rubbish Men <color=#ce3f27>have hidden</color> a <color=#55aaff>supply signal grenade</color> inside one of the cars in The <color=#55aaff>Junkyard</color>. We need to <color=#738d43>destroy</color> the <color=#55aaff>cars</color> in The Junkyard shredder to <color=#738d43>locate</color> the <color=#55aaff>supplysignal grenade</color> and activate it!",
                ["PreFinish"] = "{0} Junkyard Event <color=#ce3f27>will end</color> in <color=#55aaff>{1}</color>!",
                ["Finish"] = "{0} The Junkyard Event <color=#ce3f27>has concluded</color>!",
                ["KillBrokenCar"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>has destroyed</color> a <color=#55aaff>car</color> in the shredder with a <color=#55aaff>supply signal grenade</color> inside. A <color=#55aaff>truck</color> with loot <color=#738d43>will arrive</color> in a short time. Be careful! There are <color=#55aaff>mercenaries</color> inside the truck. They <color=#ce3f27>will try to stop</color> you from getting the loot!",
                ["PlayerKillBrokenCar"] = "{0} You <color=#738d43>have destroyed</color> a <color=#55aaff>car</color> in the shredder, but there was <color=#ce3f27>no</color> <color=#55aaff>supply signal grenade</color> in this car. Continue looking for the right car...",
                ["TruckArrived"] = "{0} <color=#55aaff>Loot Truck</color> <color=#738d43>has arrived</color> in The <color=#55aaff>Junkyard</color>. It is guarded by mercenaries. But the <color=#55aaff>loot truck</color> <color=#ce3f27>is locked</color>! If you want to get access to the crates you need to <color=#738d43>destroy</color> the <color=#55aaff>loot truck</color> using The Junkyard shredder",
                ["KillTruck"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>destroyed</color> the <color=#55aaff>loot truck</color> in The Junkyard shredder. Access to the loot <color=#738d43>is all unlocked</color>!",
                ["SetOwner"] = "{0} Player <color=#55aaff>{1}</color> <color=#738d43>has received</color> the owner status for the <color=#55aaff>Junkyard Event</color>",
                ["EventActive"] = "{0} This event is active now. To finish this event (<color=#55aaff>/jstop</color>), then (<color=#55aaff>/jstart</color>) to start the next one!",
                ["EnterPVP"] = "{0} You <color=#ce3f27>have entered</color> the PVP zone, now other players <color=#ce3f27>can damage</color> you!",
                ["ExitPVP"] = "{0} You <color=#738d43>have left</color> the PVP zone, now other players <color=#738d43>cannot damage</color> you!",
                ["NTeleportation"] = "{0} You <color=#ce3f27>cannot</color> teleport into the event zone!",
                ["SendEconomy"] = "{0} You <color=#738d43>have earned</color> <color=#55aaff>{1}</color> points in economics for participating in the event",
                ["NoCommand"] = "{0} You <color=#ce3f27>cannot</color> use this command in the event zone!"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PreStart"] = "{0} Через <color=#55aaff>{1}</color> мусорщики <color=#738d43>прибудут</color> в локацию <color=#55aaff>Свалка</color>!",
                ["Start"] = "{0} Мусорщики <color=#ce3f27>спрятали</color> на <color=#55aaff>Свалке</color> в одной из сломанных машин <color=#55aaff>сигнальную гранату</color> для вызова грузовика с припасами. Вам необходимо <color=#738d43>уничтожить</color> <color=#55aaff>машину</color> в шредере, чтобы <color=#738d43>найти</color> <color=#55aaff>сигнальную гранату</color> и привести её в исполнение",
                ["PreFinish"] = "{0} Ивент на свалке <color=#ce3f27>закончится</color> через <color=#55aaff>{1}</color>!",
                ["Finish"] = "{0} Ивент на свалке <color=#ce3f27>закончен</color>!",
                ["KillBrokenCar"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>уничтожил</color> в шредере <color=#55aaff>машину</color>, внутри которой находилась <color=#55aaff>сигнальная граната</color>. В ближайшее время <color=#738d43>будет доставлен</color> <color=#55aaff>грузовик</color> с припасами. Будьте осторожны, внутри грузовика находятся <color=#55aaff>наемники</color>, которые <color=#ce3f27>помешают</color> вам заполучить припасы",
                ["PlayerKillBrokenCar"] = "{0} Вы <color=#738d43>уничтожили</color> <color=#55aaff>машину</color> в шредере, но в данной машине <color=#ce3f27>не было</color> <color=#55aaff>сигнальной гранаты</color>. Продолжайте искать необходимую машину...",
                ["TruckArrived"] = "{0} <color=#55aaff>Грузовик</color> <color=#738d43>доставлен</color> в локацию <color=#55aaff>Свалка</color>, его охраняют наемники. Но <color=#55aaff>грузовик</color> <color=#ce3f27>закрыт</color>, чтобы получить доступ к ящикам вам необходимо <color=#738d43>уничтожить</color> <color=#55aaff>грузовик</color> через шредер",
                ["KillTruck"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>уничтожил</color> в шредере <color=#55aaff>грузовик</color>. Доступ к припасам <color=#738d43>открыт</color>!",
                ["SetOwner"] = "{0} Игрок <color=#55aaff>{1}</color> <color=#738d43>получил</color> статус владельца ивента для <color=#55aaff>Junkyard Event</color>",
                ["EventActive"] = "{0} Ивент в данный момент активен, сначала завершите текущий ивент (<color=#55aaff>/jstop</color>), чтобы начать следующий!",
                ["EnterPVP"] = "{0} Вы <color=#ce3f27>вошли</color> в PVP зону, теперь другие игроки <color=#ce3f27>могут</color> наносить вам урон!",
                ["ExitPVP"] = "{0} Вы <color=#738d43>вышли</color> из PVP зоны, теперь другие игроки <color=#738d43>не могут</color> наносить вам урон!",
                ["NTeleportation"] = "{0} Вы <color=#ce3f27>не можете</color> телепортироваться в зоне ивента!",
                ["SendEconomy"] = "{0} Вы <color=#738d43>получили</color> <color=#55aaff>{1}</color> баллов в экономику за прохождение ивента",
                ["NoCommand"] = "{0} Вы <color=#ce3f27>не можете</color> использовать данную команду в зоне ивента!"
            }, this, "ru");
        }

        private string GetMessage(string langKey, string userId) => lang.GetMessage(langKey, _ins, userId);

        private string GetMessage(string langKey, string userId, params object[] args) => (args.Length == 0) ? GetMessage(langKey, userId) : string.Format(GetMessage(langKey, userId), args);
        #endregion Lang

        #region Oxide Hooks
        private static JunkyardEvent _ins;

        private void Init()
        {
            _ins = this;
            Unsubscribes();
        }

        private void OnServerInitialized()
        {
            CheckAllLootTables();
            DownloadImage();
            if (GetMonument() == null)
            {
                PrintError("The Junkyard location is missing on the map. The plugin cannot be loaded!");
                NextTick(() => Interface.Oxide.UnloadPlugin(Name));
                return;
            }
            StartTimer();
        }

        private void Unload()
        {
            if (Controller != null) Finish();
            _ins = null;
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return null;
            if (entity == Controller.Truck || entity == Controller.Module) return true;
            return null;
        }

        private object CanMountEntity(BasePlayer player, BaseMountable entity)
        {
            if (player == null || entity == null) return null;
            BaseEntity parentEntity = entity.GetParentEntity();
            if (parentEntity == null) return null;
            if (parentEntity == Controller.Module) return true;
            if (parentEntity is MagnetCrane && Controller.Players.Contains(player))
            {
                if (ActivePveMode && PveMode.Call("CanActionEvent", Name, player) != null) return true;
                if (!Controller.PlayersInCrane.Contains(player)) Controller.PlayersInCrane.Add(player);
            }
            return null;
        }

        private object CanDismountEntity(BasePlayer player, BaseMountable entity)
        {
            if (player == null || entity == null) return null;
            MagnetCrane crane = entity.GetParentEntity() as MagnetCrane;
            if (crane != null && Controller.PlayersInCrane.Contains(player)) Controller.PlayersInCrane.Remove(player);
            return null;
        }

        private void OnEntitySpawned(MagnetCrane crane)
        {
            if (crane == null || Controller == null) return;
            if (Vector3.Distance(crane.transform.position, Controller.transform.position) < _config.Radius)
            {
                int count = Controller.Cranes.Count;
                if (count == 2) NextTick(() => crane.Kill());
                else if (count == 1)
                {
                    if (_config.AdditionalCrane) Controller.Cranes.Add(crane);
                    else NextTick(() => crane.Kill());
                }
                else if (count == 0) Controller.Cranes.Add(crane);
            }
        }

        private void OnEntityKill(MagnetCrane crane)
        {
            if (crane == null || Controller == null) return;
            if (Controller.Cranes.Contains(crane))
            {
                if (!_config.IsFuelCrane) crane.GetFuelSystem().GetFuelContainer().inventory.ClearItemsContainer();
                Controller.Cranes.Remove(crane);
                timer.In(1f, () => Controller.KillCrane());
            }
        }

        private void OnEntityKill(LockedByEntCrate crate)
        {
            if (crate == null) return;
            if (Controller.HeliCrates.Contains(crate)) Controller.HeliCrates.Remove(crate);
            else if (Controller.BradleyCrates.Contains(crate)) Controller.BradleyCrates.Remove(crate);
        }

        private void OnEntityKill(HackableLockedCrate crate) { if (crate != null && Controller.HackCrates.Contains(crate)) Controller.HackCrates.Remove(crate); }

        private void OnEntityEnter(LargeShredderTrigger trigger, BaseCombatEntity entity)
        {
            if (trigger == null || entity == null) return;
            if (trigger == Controller.Shredder.trigger) Controller.CheckCar(entity.net.ID.Value);
        }

        private void OnCargoPlaneSignaled(CargoPlane cargoPlane, SupplySignal supplySignal)
        {
            if (cargoPlane == null || supplySignal == null) return;
            if (supplySignal == Controller.Supply)
            {
                Controller.Plane = cargoPlane;
                cargoPlane.secondsToTake *= 1f / _config.ScaleSpeedPlane;
                Unsubscribe("OnCargoPlaneSignaled");
            }
        }

        private void OnSupplyDropDropped(SupplyDrop supplyDrop, CargoPlane cargoPlane)
        {
            if (supplyDrop == null || cargoPlane == null) return;
            if (cargoPlane == Controller.Plane)
            {
                Controller.SpawnTruck(supplyDrop.transform.position);
                if (supplyDrop.IsExists()) supplyDrop.Kill();
                Unsubscribe("OnSupplyDropDropped");
            }
        }

        private object OnVehiclePush(ModularCar vehicle, BasePlayer player)
        {
            if (vehicle != null && vehicle == Controller.Truck) return true;
            else return null;
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!_config.Marker.Enabled || Controller == null || !player.IsPlayer()) return;
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot)) timer.In(2f, () => OnPlayerConnected(player));
            else Controller.UpdateMapMarkers();
        }

        private object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player.IsPlayer() && Controller.Players.Contains(player))
            {
                Controller.ExitPlayer(player);
                if (Controller.PlayersInCrane.Contains(player)) Controller.PlayersInCrane.Remove(player);
            }
            return null;
        }

        private void OnEntityDeath(ScientistNPC npc, HitInfo info)
        {
            if (npc == null || info == null) return;
            BasePlayer attacker = info.InitiatorPlayer;
            if (Controller.Scientists.Contains(npc) && attacker.IsPlayer()) ActionEconomy(attacker.userID, "Npc");
        }

        private Dictionary<ulong, ulong> StartHackCrates { get; } = new Dictionary<ulong, ulong>();

        private object CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (player == null || crate == null) return null;
            if (Controller.HackCrates.Contains(crate))
            {
                if (StartHackCrates.ContainsKey(crate.net.ID.Value)) StartHackCrates[crate.net.ID.Value] = player.userID;
                else StartHackCrates.Add(crate.net.ID.Value, player.userID);
            }
            return null;
        }

        private void OnCrateHack(HackableLockedCrate crate)
        {
            if (crate == null) return;
            ulong crateId = crate.net.ID.Value;
            ulong playerId;
            if (StartHackCrates.TryGetValue(crateId, out playerId))
            {
                StartHackCrates.Remove(crateId);
                if (_config.HackCrates.IncreaseEventTime && Controller.TimeToFinish < (int)_config.HackCrates.UnlockTime) Controller.TimeToFinish += (int)_config.HackCrates.UnlockTime;
                ActionEconomy(playerId, "LockedCrate");
            }
        }

        private HashSet<ulong> LootableCrates { get; } = new HashSet<ulong>();

        private void OnLootEntity(BasePlayer player, LockedByEntCrate container)
        {
            if (player == null || container == null || LootableCrates.Contains(container.net.ID.Value)) return;
            if (Controller.HeliCrates.Contains(container) || Controller.BradleyCrates.Contains(container))
            {
                LootableCrates.Add(container.net.ID.Value);
                ActionEconomy(player.userID, "Crates", container.ShortPrefabName);
            }
        }

        private object OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            if (player != null && Controller.Players.Contains(player))
            {
                command = "/" + command;
                if (_config.Commands.Contains(command.ToLower()))
                {
                    AlertToPlayer(player, GetMessage("NoCommand", player.UserIDString, _config.Prefix));
                    return true;
                }
            }
            return null;
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.cmd == null) return null;
            BasePlayer player = arg.Player();
            if (player != null && Controller.Players.Contains(player))
            {
                if (_config.Commands.Contains(arg.cmd.Name.ToLower()) || _config.Commands.Contains(arg.cmd.FullName.ToLower()))
                {
                    AlertToPlayer(player, GetMessage("NoCommand", player.UserIDString, _config.Prefix));
                    return true;
                }
            }
            return null;
        }
        #endregion Oxide Hooks

        #region Controller
        internal HashSet<Vector3> Marker { get; } = new HashSet<Vector3>
        {
            new Vector3(48f, 0f, 8f),
            new Vector3(48f, 0f, 6f),
            new Vector3(48f, 0f, 4f),
            new Vector3(48f, 0f, 2f),
            new Vector3(48f, 0f, 0f),
            new Vector3(48f, 0f, -2f),
            new Vector3(48f, 0f, -4f),
            new Vector3(48f, 0f, -6f),
            new Vector3(48f, 0f, -8f),
            new Vector3(46f, 0f, 16f),
            new Vector3(46f, 0f, 14f),
            new Vector3(46f, 0f, 12f),
            new Vector3(46f, 0f, 10f),
            new Vector3(46f, 0f, 8f),
            new Vector3(46f, 0f, 6f),
            new Vector3(46f, 0f, 4f),
            new Vector3(46f, 0f, 2f),
            new Vector3(46f, 0f, 0f),
            new Vector3(46f, 0f, -2f),
            new Vector3(46f, 0f, -4f),
            new Vector3(46f, 0f, -6f),
            new Vector3(46f, 0f, -8f),
            new Vector3(46f, 0f, -10f),
            new Vector3(46f, 0f, -12f),
            new Vector3(46f, 0f, -14f),
            new Vector3(44f, 0f, 20f),
            new Vector3(44f, 0f, 18f),
            new Vector3(44f, 0f, 16f),
            new Vector3(44f, 0f, 14f),
            new Vector3(44f, 0f, 12f),
            new Vector3(44f, 0f, 10f),
            new Vector3(44f, 0f, 8f),
            new Vector3(44f, 0f, -8f),
            new Vector3(44f, 0f, -10f),
            new Vector3(44f, 0f, -12f),
            new Vector3(44f, 0f, -14f),
            new Vector3(44f, 0f, -16f),
            new Vector3(44f, 0f, -18f),
            new Vector3(44f, 0f, -20f),
            new Vector3(42f, 0f, 24f),
            new Vector3(42f, 0f, 22f),
            new Vector3(42f, 0f, 20f),
            new Vector3(42f, 0f, 18f),
            new Vector3(42f, 0f, 16f),
            new Vector3(42f, 0f, -16f),
            new Vector3(42f, 0f, -18f),
            new Vector3(42f, 0f, -20f),
            new Vector3(42f, 0f, -22f),
            new Vector3(42f, 0f, -24f),
            new Vector3(40f, 0f, 26f),
            new Vector3(40f, 0f, 24f),
            new Vector3(40f, 0f, 22f),
            new Vector3(40f, 0f, -22f),
            new Vector3(40f, 0f, -24f),
            new Vector3(40f, 0f, -26f),
            new Vector3(38f, 0f, 30f),
            new Vector3(38f, 0f, 28f),
            new Vector3(38f, 0f, 26f),
            new Vector3(38f, 0f, 24f),
            new Vector3(38f, 0f, -24f),
            new Vector3(38f, 0f, -26f),
            new Vector3(38f, 0f, -28f),
            new Vector3(38f, 0f, -30f),
            new Vector3(36f, 0f, 32f),
            new Vector3(36f, 0f, 30f),
            new Vector3(36f, 0f, 28f),
            new Vector3(36f, 0f, -28f),
            new Vector3(36f, 0f, -30f),
            new Vector3(36f, 0f, -32f),
            new Vector3(34f, 0f, 34f),
            new Vector3(34f, 0f, 32f),
            new Vector3(34f, 0f, 30f),
            new Vector3(34f, 0f, -30f),
            new Vector3(34f, 0f, -32f),
            new Vector3(34f, 0f, -34f),
            new Vector3(32f, 0f, 36f),
            new Vector3(32f, 0f, 34f),
            new Vector3(32f, 0f, 32f),
            new Vector3(32f, 0f, -32f),
            new Vector3(32f, 0f, -34f),
            new Vector3(32f, 0f, -36f),
            new Vector3(30f, 0f, 38f),
            new Vector3(30f, 0f, 36f),
            new Vector3(30f, 0f, 34f),
            new Vector3(30f, 0f, -34f),
            new Vector3(30f, 0f, -36f),
            new Vector3(30f, 0f, -38f),
            new Vector3(28f, 0f, 38f),
            new Vector3(28f, 0f, 36f),
            new Vector3(28f, 0f, -12f),
            new Vector3(28f, 0f, -14f),
            new Vector3(28f, 0f, -16f),
            new Vector3(28f, 0f, -18f),
            new Vector3(28f, 0f, -20f),
            new Vector3(28f, 0f, -36f),
            new Vector3(28f, 0f, -38f),
            new Vector3(26f, 0f, 40f),
            new Vector3(26f, 0f, 38f),
            new Vector3(26f, 0f, -10f),
            new Vector3(26f, 0f, -12f),
            new Vector3(26f, 0f, -14f),
            new Vector3(26f, 0f, -16f),
            new Vector3(26f, 0f, -20f),
            new Vector3(26f, 0f, -22f),
            new Vector3(26f, 0f, -38f),
            new Vector3(26f, 0f, -40f),
            new Vector3(24f, 0f, 42f),
            new Vector3(24f, 0f, 40f),
            new Vector3(24f, 0f, 38f),
            new Vector3(24f, 0f, -6f),
            new Vector3(24f, 0f, -8f),
            new Vector3(24f, 0f, -10f),
            new Vector3(24f, 0f, -12f),
            new Vector3(24f, 0f, -20f),
            new Vector3(24f, 0f, -22f),
            new Vector3(24f, 0f, -38f),
            new Vector3(24f, 0f, -40f),
            new Vector3(24f, 0f, -42f),
            new Vector3(22f, 0f, 42f),
            new Vector3(22f, 0f, 40f),
            new Vector3(22f, 0f, 12f),
            new Vector3(22f, 0f, 10f),
            new Vector3(22f, 0f, 8f),
            new Vector3(22f, 0f, -6f),
            new Vector3(22f, 0f, -8f),
            new Vector3(22f, 0f, -20f),
            new Vector3(22f, 0f, -22f),
            new Vector3(22f, 0f, -40f),
            new Vector3(22f, 0f, -42f),
            new Vector3(20f, 0f, 44f),
            new Vector3(20f, 0f, 42f),
            new Vector3(20f, 0f, 12f),
            new Vector3(20f, 0f, 10f),
            new Vector3(20f, 0f, 8f),
            new Vector3(20f, 0f, 6f),
            new Vector3(20f, 0f, 4f),
            new Vector3(20f, 0f, 2f),
            new Vector3(20f, 0f, -20f),
            new Vector3(20f, 0f, -22f),
            new Vector3(20f, 0f, -42f),
            new Vector3(20f, 0f, -44f),
            new Vector3(18f, 0f, 44f),
            new Vector3(18f, 0f, 42f),
            new Vector3(18f, 0f, 6f),
            new Vector3(18f, 0f, 4f),
            new Vector3(18f, 0f, 2f),
            new Vector3(18f, 0f, 0f),
            new Vector3(18f, 0f, -20f),
            new Vector3(18f, 0f, -22f),
            new Vector3(18f, 0f, -42f),
            new Vector3(18f, 0f, -44f),
            new Vector3(16f, 0f, 46f),
            new Vector3(16f, 0f, 44f),
            new Vector3(16f, 0f, 42f),
            new Vector3(16f, 0f, 8f),
            new Vector3(16f, 0f, 6f),
            new Vector3(16f, 0f, 4f),
            new Vector3(16f, 0f, 2f),
            new Vector3(16f, 0f, 0f),
            new Vector3(16f, 0f, -20f),
            new Vector3(16f, 0f, -22f),
            new Vector3(16f, 0f, -42f),
            new Vector3(16f, 0f, -44f),
            new Vector3(14f, 0f, 46f),
            new Vector3(14f, 0f, 44f),
            new Vector3(14f, 0f, 12f),
            new Vector3(14f, 0f, 10f),
            new Vector3(14f, 0f, 8f),
            new Vector3(14f, 0f, 6f),
            new Vector3(14f, 0f, 4f),
            new Vector3(14f, 0f, 2f),
            new Vector3(14f, 0f, 0f),
            new Vector3(14f, 0f, -20f),
            new Vector3(14f, 0f, -22f),
            new Vector3(14f, 0f, -44f),
            new Vector3(14f, 0f, -46f),
            new Vector3(12f, 0f, 46f),
            new Vector3(12f, 0f, 44f),
            new Vector3(12f, 0f, 16f),
            new Vector3(12f, 0f, 14f),
            new Vector3(12f, 0f, 12f),
            new Vector3(12f, 0f, 10f),
            new Vector3(12f, 0f, 8f),
            new Vector3(12f, 0f, 4f),
            new Vector3(12f, 0f, 2f),
            new Vector3(12f, 0f, -20f),
            new Vector3(12f, 0f, -22f),
            new Vector3(12f, 0f, -44f),
            new Vector3(12f, 0f, -46f),
            new Vector3(10f, 0f, 46f),
            new Vector3(10f, 0f, 44f),
            new Vector3(10f, 0f, 18f),
            new Vector3(10f, 0f, 16f),
            new Vector3(10f, 0f, 14f),
            new Vector3(10f, 0f, 12f),
            new Vector3(10f, 0f, 4f),
            new Vector3(10f, 0f, 2f),
            new Vector3(10f, 0f, -20f),
            new Vector3(10f, 0f, -22f),
            new Vector3(10f, 0f, -44f),
            new Vector3(10f, 0f, -46f),
            new Vector3(8f, 0f, 48f),
            new Vector3(8f, 0f, 46f),
            new Vector3(8f, 0f, 44f),
            new Vector3(8f, 0f, 22f),
            new Vector3(8f, 0f, 20f),
            new Vector3(8f, 0f, 18f),
            new Vector3(8f, 0f, 16f),
            new Vector3(8f, 0f, 6f),
            new Vector3(8f, 0f, 4f),
            new Vector3(8f, 0f, 2f),
            new Vector3(8f, 0f, -20f),
            new Vector3(8f, 0f, -22f),
            new Vector3(8f, 0f, -44f),
            new Vector3(8f, 0f, -46f),
            new Vector3(8f, 0f, -48f),
            new Vector3(6f, 0f, 48f),
            new Vector3(6f, 0f, 46f),
            new Vector3(6f, 0f, 26f),
            new Vector3(6f, 0f, 24f),
            new Vector3(6f, 0f, 22f),
            new Vector3(6f, 0f, 20f),
            new Vector3(6f, 0f, 4f),
            new Vector3(6f, 0f, -20f),
            new Vector3(6f, 0f, -22f),
            new Vector3(6f, 0f, -46f),
            new Vector3(6f, 0f, -48f),
            new Vector3(4f, 0f, 48f),
            new Vector3(4f, 0f, 46f),
            new Vector3(4f, 0f, 28f),
            new Vector3(4f, 0f, 26f),
            new Vector3(4f, 0f, 24f),
            new Vector3(4f, 0f, 22f),
            new Vector3(4f, 0f, -14f),
            new Vector3(4f, 0f, -20f),
            new Vector3(4f, 0f, -22f),
            new Vector3(4f, 0f, -30f),
            new Vector3(4f, 0f, -46f),
            new Vector3(4f, 0f, -48f),
            new Vector3(2f, 0f, 48f),
            new Vector3(2f, 0f, 46f),
            new Vector3(2f, 0f, 28f),
            new Vector3(2f, 0f, 26f),
            new Vector3(2f, 0f, -14f),
            new Vector3(2f, 0f, -16f),
            new Vector3(2f, 0f, -20f),
            new Vector3(2f, 0f, -22f),
            new Vector3(2f, 0f, -26f),
            new Vector3(2f, 0f, -28f),
            new Vector3(2f, 0f, -30f),
            new Vector3(2f, 0f, -46f),
            new Vector3(2f, 0f, -48f),
            new Vector3(0f, 0f, 48f),
            new Vector3(0f, 0f, 46f),
            new Vector3(0f, 0f, 30f),
            new Vector3(0f, 0f, 28f),
            new Vector3(0f, 0f, -14f),
            new Vector3(0f, 0f, -16f),
            new Vector3(0f, 0f, -18f),
            new Vector3(0f, 0f, -20f),
            new Vector3(0f, 0f, -22f),
            new Vector3(0f, 0f, -24f),
            new Vector3(0f, 0f, -26f),
            new Vector3(0f, 0f, -28f),
            new Vector3(0f, 0f, -46f),
            new Vector3(0f, 0f, -48f),
            new Vector3(-2f, 0f, 48f),
            new Vector3(-2f, 0f, 46f),
            new Vector3(-2f, 0f, 28f),
            new Vector3(-2f, 0f, 26f),
            new Vector3(-2f, 0f, -16f),
            new Vector3(-2f, 0f, -18f),
            new Vector3(-2f, 0f, -20f),
            new Vector3(-2f, 0f, -22f),
            new Vector3(-2f, 0f, -24f),
            new Vector3(-2f, 0f, -26f),
            new Vector3(-2f, 0f, -46f),
            new Vector3(-2f, 0f, -48f),
            new Vector3(-4f, 0f, 48f),
            new Vector3(-4f, 0f, 46f),
            new Vector3(-4f, 0f, 28f),
            new Vector3(-4f, 0f, 26f),
            new Vector3(-4f, 0f, 24f),
            new Vector3(-4f, 0f, 22f),
            new Vector3(-4f, 0f, -18f),
            new Vector3(-4f, 0f, -20f),
            new Vector3(-4f, 0f, -22f),
            new Vector3(-4f, 0f, -24f),
            new Vector3(-4f, 0f, -46f),
            new Vector3(-4f, 0f, -48f),
            new Vector3(-6f, 0f, 48f),
            new Vector3(-6f, 0f, 46f),
            new Vector3(-6f, 0f, 26f),
            new Vector3(-6f, 0f, 24f),
            new Vector3(-6f, 0f, 22f),
            new Vector3(-6f, 0f, 20f),
            new Vector3(-6f, 0f, -20f),
            new Vector3(-6f, 0f, -22f),
            new Vector3(-6f, 0f, -46f),
            new Vector3(-6f, 0f, -48f),
            new Vector3(-8f, 0f, 48f),
            new Vector3(-8f, 0f, 46f),
            new Vector3(-8f, 0f, 44f),
            new Vector3(-8f, 0f, 22f),
            new Vector3(-8f, 0f, 20f),
            new Vector3(-8f, 0f, 18f),
            new Vector3(-8f, 0f, -44f),
            new Vector3(-8f, 0f, -46f),
            new Vector3(-8f, 0f, -48f),
            new Vector3(-10f, 0f, 46f),
            new Vector3(-10f, 0f, 44f),
            new Vector3(-10f, 0f, 18f),
            new Vector3(-10f, 0f, 6f),
            new Vector3(-10f, 0f, 4f),
            new Vector3(-10f, 0f, 2f),
            new Vector3(-10f, 0f, 0f),
            new Vector3(-10f, 0f, -2f),
            new Vector3(-10f, 0f, -44f),
            new Vector3(-10f, 0f, -46f),
            new Vector3(-12f, 0f, 46f),
            new Vector3(-12f, 0f, 44f),
            new Vector3(-12f, 0f, 10f),
            new Vector3(-12f, 0f, 8f),
            new Vector3(-12f, 0f, 6f),
            new Vector3(-12f, 0f, 4f),
            new Vector3(-12f, 0f, 2f),
            new Vector3(-12f, 0f, 0f),
            new Vector3(-12f, 0f, -44f),
            new Vector3(-12f, 0f, -46f),
            new Vector3(-14f, 0f, 46f),
            new Vector3(-14f, 0f, 44f),
            new Vector3(-14f, 0f, 12f),
            new Vector3(-14f, 0f, 10f),
            new Vector3(-14f, 0f, 8f),
            new Vector3(-14f, 0f, 6f),
            new Vector3(-14f, 0f, -20f),
            new Vector3(-14f, 0f, -22f),
            new Vector3(-14f, 0f, -44f),
            new Vector3(-14f, 0f, -46f),
            new Vector3(-16f, 0f, 46f),
            new Vector3(-16f, 0f, 44f),
            new Vector3(-16f, 0f, 42f),
            new Vector3(-16f, 0f, 10f),
            new Vector3(-16f, 0f, 8f),
            new Vector3(-16f, 0f, 6f),
            new Vector3(-16f, 0f, 4f),
            new Vector3(-16f, 0f, 2f),
            new Vector3(-16f, 0f, -20f),
            new Vector3(-16f, 0f, -22f),
            new Vector3(-16f, 0f, -42f),
            new Vector3(-16f, 0f, -44f),
            new Vector3(-18f, 0f, 44f),
            new Vector3(-18f, 0f, 42f),
            new Vector3(-18f, 0f, 10f),
            new Vector3(-18f, 0f, 8f),
            new Vector3(-18f, 0f, 4f),
            new Vector3(-18f, 0f, 2f),
            new Vector3(-18f, 0f, 0f),
            new Vector3(-18f, 0f, -2f),
            new Vector3(-18f, 0f, -20f),
            new Vector3(-18f, 0f, -22f),
            new Vector3(-18f, 0f, -42f),
            new Vector3(-18f, 0f, -44f),
            new Vector3(-20f, 0f, 44f),
            new Vector3(-20f, 0f, 42f),
            new Vector3(-20f, 0f, 10f),
            new Vector3(-20f, 0f, 8f),
            new Vector3(-20f, 0f, 2f),
            new Vector3(-20f, 0f, 0f),
            new Vector3(-20f, 0f, -2f),
            new Vector3(-20f, 0f, -4f),
            new Vector3(-20f, 0f, -20f),
            new Vector3(-20f, 0f, -22f),
            new Vector3(-20f, 0f, -42f),
            new Vector3(-20f, 0f, -44f),
            new Vector3(-22f, 0f, 42f),
            new Vector3(-22f, 0f, 40f),
            new Vector3(-22f, 0f, 10f),
            new Vector3(-22f, 0f, 8f),
            new Vector3(-22f, 0f, 6f),
            new Vector3(-22f, 0f, -2f),
            new Vector3(-22f, 0f, -4f),
            new Vector3(-22f, 0f, -6f),
            new Vector3(-22f, 0f, -8f),
            new Vector3(-22f, 0f, -20f),
            new Vector3(-22f, 0f, -22f),
            new Vector3(-22f, 0f, -40f),
            new Vector3(-22f, 0f, -42f),
            new Vector3(-24f, 0f, 42f),
            new Vector3(-24f, 0f, 40f),
            new Vector3(-24f, 0f, 38f),
            new Vector3(-24f, 0f, 8f),
            new Vector3(-24f, 0f, 6f),
            new Vector3(-24f, 0f, -6f),
            new Vector3(-24f, 0f, -8f),
            new Vector3(-24f, 0f, -10f),
            new Vector3(-24f, 0f, -12f),
            new Vector3(-24f, 0f, -20f),
            new Vector3(-24f, 0f, -22f),
            new Vector3(-24f, 0f, -38f),
            new Vector3(-24f, 0f, -40f),
            new Vector3(-24f, 0f, -42f),
            new Vector3(-26f, 0f, 40f),
            new Vector3(-26f, 0f, 38f),
            new Vector3(-26f, 0f, -10f),
            new Vector3(-26f, 0f, -12f),
            new Vector3(-26f, 0f, -14f),
            new Vector3(-26f, 0f, -16f),
            new Vector3(-26f, 0f, -20f),
            new Vector3(-26f, 0f, -22f),
            new Vector3(-26f, 0f, -38f),
            new Vector3(-26f, 0f, -40f),
            new Vector3(-28f, 0f, 38f),
            new Vector3(-28f, 0f, 36f),
            new Vector3(-28f, 0f, -12f),
            new Vector3(-28f, 0f, -14f),
            new Vector3(-28f, 0f, -16f),
            new Vector3(-28f, 0f, -18f),
            new Vector3(-28f, 0f, -20f),
            new Vector3(-28f, 0f, -36f),
            new Vector3(-28f, 0f, -38f),
            new Vector3(-30f, 0f, 38f),
            new Vector3(-30f, 0f, 36f),
            new Vector3(-30f, 0f, 34f),
            new Vector3(-30f, 0f, -34f),
            new Vector3(-30f, 0f, -36f),
            new Vector3(-30f, 0f, -38f),
            new Vector3(-32f, 0f, 36f),
            new Vector3(-32f, 0f, 34f),
            new Vector3(-32f, 0f, 32f),
            new Vector3(-32f, 0f, -32f),
            new Vector3(-32f, 0f, -34f),
            new Vector3(-32f, 0f, -36f),
            new Vector3(-34f, 0f, 34f),
            new Vector3(-34f, 0f, 32f),
            new Vector3(-34f, 0f, 30f),
            new Vector3(-34f, 0f, -30f),
            new Vector3(-34f, 0f, -32f),
            new Vector3(-34f, 0f, -34f),
            new Vector3(-36f, 0f, 32f),
            new Vector3(-36f, 0f, 30f),
            new Vector3(-36f, 0f, 28f),
            new Vector3(-36f, 0f, -28f),
            new Vector3(-36f, 0f, -30f),
            new Vector3(-36f, 0f, -32f),
            new Vector3(-38f, 0f, 30f),
            new Vector3(-38f, 0f, 28f),
            new Vector3(-38f, 0f, 26f),
            new Vector3(-38f, 0f, 24f),
            new Vector3(-38f, 0f, -24f),
            new Vector3(-38f, 0f, -26f),
            new Vector3(-38f, 0f, -28f),
            new Vector3(-38f, 0f, -30f),
            new Vector3(-40f, 0f, 26f),
            new Vector3(-40f, 0f, 24f),
            new Vector3(-40f, 0f, 22f),
            new Vector3(-40f, 0f, -22f),
            new Vector3(-40f, 0f, -24f),
            new Vector3(-40f, 0f, -26f),
            new Vector3(-42f, 0f, 24f),
            new Vector3(-42f, 0f, 22f),
            new Vector3(-42f, 0f, 20f),
            new Vector3(-42f, 0f, 18f),
            new Vector3(-42f, 0f, 16f),
            new Vector3(-42f, 0f, -16f),
            new Vector3(-42f, 0f, -18f),
            new Vector3(-42f, 0f, -20f),
            new Vector3(-42f, 0f, -22f),
            new Vector3(-42f, 0f, -24f),
            new Vector3(-44f, 0f, 20f),
            new Vector3(-44f, 0f, 18f),
            new Vector3(-44f, 0f, 16f),
            new Vector3(-44f, 0f, 14f),
            new Vector3(-44f, 0f, 12f),
            new Vector3(-44f, 0f, 10f),
            new Vector3(-44f, 0f, 8f),
            new Vector3(-44f, 0f, -8f),
            new Vector3(-44f, 0f, -10f),
            new Vector3(-44f, 0f, -12f),
            new Vector3(-44f, 0f, -14f),
            new Vector3(-44f, 0f, -16f),
            new Vector3(-44f, 0f, -18f),
            new Vector3(-44f, 0f, -20f),
            new Vector3(-46f, 0f, 16f),
            new Vector3(-46f, 0f, 14f),
            new Vector3(-46f, 0f, 12f),
            new Vector3(-46f, 0f, 10f),
            new Vector3(-46f, 0f, 8f),
            new Vector3(-46f, 0f, 6f),
            new Vector3(-46f, 0f, 4f),
            new Vector3(-46f, 0f, 2f),
            new Vector3(-46f, 0f, 0f),
            new Vector3(-46f, 0f, -2f),
            new Vector3(-46f, 0f, -4f),
            new Vector3(-46f, 0f, -6f),
            new Vector3(-46f, 0f, -8f),
            new Vector3(-46f, 0f, -10f),
            new Vector3(-46f, 0f, -12f),
            new Vector3(-46f, 0f, -14f),
            new Vector3(-46f, 0f, -16f),
            new Vector3(-48f, 0f, 8f),
            new Vector3(-48f, 0f, 6f),
            new Vector3(-48f, 0f, 4f),
            new Vector3(-48f, 0f, 2f),
            new Vector3(-48f, 0f, 0f),
            new Vector3(-48f, 0f, -2f),
            new Vector3(-48f, 0f, -4f),
            new Vector3(-48f, 0f, -6f),
            new Vector3(-48f, 0f, -8)
        };

        private ControllerJunkyardEvent Controller { get; set; } = null;
        private bool Active { get; set; } = false;

        private void StartTimer()
        {
            if (!_config.EnabledTimer) return;
            timer.In(UnityEngine.Random.Range(_config.MinStartTime, _config.MaxStartTime), () =>
            {
                if (!Active) Start(null);
                else Puts("This event is active now. To finish this event (jstop), then to start the next one");
            });
        }

        private void Start(BasePlayer player)
        {
            if (!plugins.Exists("NpcSpawn"))
            {
                PrintError("NpcSpawn plugin doesn`t exist! (https://drive.google.com/drive/folders/1-18L-mG7yiGxR-PQYvd11VvXC2RQ4ZCu?usp=sharing)");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            CheckVersionPlugin();
            Active = true;
            AlertToAllPlayers("PreStart", _config.Prefix, GetTimeFormat((int)_config.PreStartTime));
            timer.In(_config.PreStartTime, () =>
            {
                Puts("JunkyardEvent has begun");
                if (_config.RemoveBetterNpc && plugins.Exists("BetterNpc")) BetterNpc.Call("DestroyController", "Junkyard");
                Subscribes();
                Controller = new GameObject().AddComponent<ControllerJunkyardEvent>();
                if (plugins.Exists("MonumentOwner")) MonumentOwner.Call("RemoveZone", Controller.Monument);
                Controller.EnablePveMode(_config.PveMode, player);
                Interface.Oxide.CallHook("OnJunkyardEventStart", Controller.transform.position, _config.Radius);
                AlertToAllPlayers("Start", _config.Prefix);
            });
        }

        private void Finish()
        {
            Unsubscribes();
            if (ActivePveMode) PveMode.Call("EventRemovePveMode", Name, true);
            if (Controller != null)
            {
                if (plugins.Exists("MonumentOwner")) MonumentOwner.Call("CreateZone", Controller.Monument);
                UnityEngine.Object.Destroy(Controller.gameObject);
            }
            Active = false;
            SendBalance();
            LootableCrates.Clear();
            AlertToAllPlayers("Finish", _config.Prefix);
            Interface.Oxide.CallHook("OnJunkyardEventEnd");
            if (_config.RemoveBetterNpc && plugins.Exists("BetterNpc")) BetterNpc.Call("CreateController", "Junkyard");
            Puts("JunkyardEvent has ended");
            StartTimer();
        }

        internal HashSet<string> TrashList = new HashSet<string>
        {
            "minicopter.entity",
            "scraptransporthelicopter",
            "hotairballoon",
            "rowboat",
            "rhib",
            "submarinesolo.entity",
            "submarineduo.entity",
            "sled.deployed",
            "magnetcrane.entity",
            "sedantest.entity",
            "2module_car_spawned.entity",
            "3module_car_spawned.entity",
            "4module_car_spawned.entity",
            "wolf",
            "chicken",
            "boar",
            "stag",
            "bear",
            "testridablehorse",
            "servergibs_bradley",
            "servergibs_patrolhelicopter"
        };

        internal class ControllerJunkyardEvent : FacepunchBehaviour
        {
            private PluginConfig _config => _ins._config;

            internal MonumentInfo Monument { get; set; } = null;

            private SphereCollider SphereCollider { get; set; } = null;

            private HashSet<MapMarkerGenericRadius> Markers { get; } = new HashSet<MapMarkerGenericRadius>();
            private VendingMachineMapMarker VendingMarker { get; set; } = null;

            private Dictionary<Vector3, Vector3> SamSitePositions { get; } = new Dictionary<Vector3, Vector3>
            {
                [new Vector3(-24.295f, 18.782f, -28.644f)] = new Vector3(0f, 90f, 0f),
                [new Vector3(-41.871f, 17.303f, 9.515f)] = new Vector3(0f, 284.807f, 0f),
                [new Vector3(-20.451f, 20.874f, 38.877f)] = new Vector3(0f, 0f, 0f)
            };
            private HashSet<SamSite> SamSites { get; } = new HashSet<SamSite>();

            internal LargeShredder Shredder { get; set; } = null;

            internal HashSet<MagnetCrane> Cranes { get; set; } = null;
            internal Vector3 MainCranePos { get; set; } = Vector3.zero;
            internal Vector3 MainCraneRot { get; set; } = Vector3.zero;
            internal Vector3 AddCranePos { get; set; } = Vector3.zero;
            internal Vector3 AddCraneRot { get; set; } = Vector3.zero;

            private Vector3 LandingTruckPos { get; set; } = Vector3.zero;
            internal SupplySignal Supply { get; set; } = null;
            internal CargoPlane Plane { get; set; } = null;
            internal ModularCar Truck { get; set; } = null;
            internal VehicleModuleCamper Module { get; set; } = null;
            private BaseVehicle Parachute { get; set; } = null;
            private BasePlayer PlayerParachute { get; set; } = null;

            private Coroutine SpawnBrokenCarsCoroutine { get; set; } = null;
            private HashSet<Vector3> CarPositionsLocal { get; } = new HashSet<Vector3>
            {
                new Vector3(17.056f, 0.125f, 17.901f),
                new Vector3(35.747f, 0.059f, 14.970f),
                new Vector3(55.908f, 0.125f, 11.471f),
                new Vector3(71.476f, 0.125f, -27.498f),
                new Vector3(59.288f, 0.104f, -40.456f),
                new Vector3(31.557f, 0.050f, -62.765f),
                new Vector3(-2.163f, 0.125f, -52.034f),
                new Vector3(31.694f, 0.117f, 57.948f),
                new Vector3(5.459f, 0.109f, 75.459f),
                new Vector3(-18.921f, 0.108f, 79.526f),
                new Vector3(-48.276f, 0.136f, 79.298f),
                new Vector3(-68.626f, 0.077f, 35.397f),
                new Vector3(-69.282f, 0.125f, 8.979f),
                new Vector3(-73.574f, 0.111f, -12.416f),
                new Vector3(-20.111f, 0.088f, 1.391f)
            };
            private HashSet<Vector3> CarPositionsGlobal { get; } = new HashSet<Vector3>();
            internal List<BaseCombatEntity> BrokenCars { get; } = new List<BaseCombatEntity>();
            internal ulong BrokenCarId { get; set; } = 0;
            private ulong TruckId { get; set; } = 0;
            private HashSet<ulong> CheckedCars { get; } = new HashSet<ulong>();

            internal HashSet<ScientistNPC> Scientists { get; } = new HashSet<ScientistNPC>();

            private Vector3 SpawnPosRail { get; set; } = Vector3.zero;
            private Quaternion SpawnRotRail { get; set; } = Quaternion.identity;
            private HashSet<PointAnimationTransform> RailPointsLocal { get; } = new HashSet<PointAnimationTransform>
            {
                new PointAnimationTransform { Time = 0.25f, Pos = new Vector3(-4.650f, 1.473f, 0f), Rot = new Vector3(0f, 270f, 0f) },
                new PointAnimationTransform { Time = 0.25f, Pos = new Vector3(-5.867f, 1.473f, 0f), Rot = new Vector3(0f, 270f, 0f) },
                new PointAnimationTransform { Time = 0.25f, Pos = new Vector3(-6.906f, 1.571f, 0f), Rot = new Vector3(352.399f, 270f, 0f) },
                new PointAnimationTransform { Time = 0.25f, Pos = new Vector3(-7.860f, 1.871f, 0f), Rot = new Vector3(335.144f, 270f, 0f) },
                new PointAnimationTransform { Time = 0.25f, Pos = new Vector3(-8.931f, 2.421f, 0f), Rot = new Vector3(330.605f, 270f, 0f) },
                new PointAnimationTransform { Time = 0.25f, Pos = new Vector3(-9.912f, 2.973f, 0f), Rot = new Vector3(330.605f, 270f, 0f) },
                new PointAnimationTransform { Time = 0.25f, Pos = new Vector3(-10.891f, 3.525f, 0f), Rot = new Vector3(330.605f, 270f, 0f) },
                new PointAnimationTransform { Time = 0.25f, Pos = new Vector3(-11.864f, 4.063f, 0f), Rot = new Vector3(334.411f, 270f, 0f) },
                new PointAnimationTransform { Time = 0.25f, Pos = new Vector3(-12.989f, 4.318f, 0f), Rot = new Vector3(355.833f, 270f, 0f) },
                new PointAnimationTransform { Time = 0.25f, Pos = new Vector3(-14.047f, 4.351f, 0f), Rot = new Vector3(0f, 270f, 0f) },
                new PointAnimationTransform { Time = 0.25f, Pos = new Vector3(-15.199f, 4.329f, 0f), Rot = new Vector3(2.748f, 270f, 0f) },
                new PointAnimationTransform { Time = 0.25f, Pos = new Vector3(-16.323f, 4.231f, 0f), Rot = new Vector3(6.429f, 270f, 0f) },
                new PointAnimationTransform { Time = 0.5f, Pos = new Vector3(-19.302f, 3.895f, 0f), Rot = new Vector3(6.429f, 270f, 0f) }
            };
            private HashSet<PointAnimationTransform> RailPointsGlobal { get; } = new HashSet<PointAnimationTransform>();

            private Coroutine SpawnCratesCoroutine { get; set; } = null;
            private HashSet<DroppedItemContainer> BackpackCrates { get; } = new HashSet<DroppedItemContainer>();
            internal HashSet<LockedByEntCrate> HeliCrates { get; } = new HashSet<LockedByEntCrate>();
            internal HashSet<LockedByEntCrate> BradleyCrates { get; } = new HashSet<LockedByEntCrate>();
            internal HashSet<HackableLockedCrate> HackCrates { get; } = new HashSet<HackableLockedCrate>();

            internal int TimeToFinish = _ins._config.FinishTime;

            internal HashSet<BasePlayer> PlayersInCrane { get; } = new HashSet<BasePlayer>();
            internal HashSet<BasePlayer> Players { get; } = new HashSet<BasePlayer>();
            internal BasePlayer Owner { get; set; } = null;

            private void Awake()
            {
                Monument = _ins.GetMonument();
                transform.position = Monument.transform.position;
                transform.rotation = Monument.transform.rotation;

                gameObject.layer = 3;
                SphereCollider = gameObject.AddComponent<SphereCollider>();
                SphereCollider.isTrigger = true;
                SphereCollider.radius = _config.Radius;

                Shredder = GetNearEntity<LargeShredder>(transform.position, 25f, -1);
                SpawnPosRail = Shredder.transform.TransformPoint(new Vector3(-3.429f, 1.473f, 0f));
                SpawnRotRail = Shredder.transform.rotation * Quaternion.Euler(new Vector3(0f, 270f, 0f));
                foreach (PointAnimationTransform point in RailPointsLocal) RailPointsGlobal.Add(new PointAnimationTransform { Time = point.Time, Pos = Shredder.transform.TransformPoint(point.Pos), Rot = (Shredder.transform.rotation * Quaternion.Euler(point.Rot)).eulerAngles });

                MainCranePos = GetGlobalPosition(new Vector3(1.482f, 0.118f, 13.213f));
                MainCraneRot = GetGlobalRotation(new Vector3(0f, 71.964f, 0f)).eulerAngles;
                AddCranePos = GetGlobalPosition(new Vector3(8.627f, 0.118f, -8.731f));
                AddCraneRot = GetGlobalRotation(new Vector3(0f, 71.964f, 0f)).eulerAngles;
                Cranes = GetEntities<MagnetCrane>(transform.position, _config.Radius, -1);
                foreach (MagnetCrane crane in Cranes) if (crane.IsExists()) crane.Kill();
                Cranes.Clear();
                SpawnCrane(MainCranePos, MainCraneRot);
                if (_config.AdditionalCrane) SpawnCrane(AddCranePos, AddCraneRot);

                foreach (Vector3 pos in CarPositionsLocal) CarPositionsGlobal.Add(GetGlobalPosition(pos));
                FindAllBrokenCars();

                LandingTruckPos = GetGlobalPosition(new Vector3(-2.897f, 0.125f, -13.176f));

                if (_config.IsSamSites) SpawnSamSites();

                foreach (PresetConfig preset in _config.PresetsNpc) SpawnPreset(preset);
                
                SpawnMapMarker(_config.Marker);

                InvokeRepeating(InvokeUpdates, 0f, 1f);
            }

            private void OnDestroy()
            {
                if (SpawnBrokenCarsCoroutine != null) ServerMgr.Instance.StopCoroutine(SpawnBrokenCarsCoroutine);
                if (SpawnCratesCoroutine != null) ServerMgr.Instance.StopCoroutine(SpawnCratesCoroutine);

                CancelInvoke(InvokeUpdates);
                CancelInvoke(UpdateTruck);

                if (SphereCollider != null) Destroy(SphereCollider);

                if (VendingMarker.IsExists()) VendingMarker.Kill();
                foreach (MapMarkerGenericRadius marker in Markers) if (marker.IsExists()) marker.Kill();

                foreach (BasePlayer player in Players) CuiHelper.DestroyUi(player, "Tabs_KpucTaJl");

                foreach (ScientistNPC npc in Scientists) if (npc.IsExists()) npc.Kill();

                foreach (DroppedItemContainer backpack in BackpackCrates) if (backpack.IsExists()) backpack.Kill();
                foreach (LockedByEntCrate crate in HeliCrates) if (crate.IsExists()) crate.Kill();
                foreach (LockedByEntCrate crate in BradleyCrates) if (crate.IsExists()) crate.Kill();
                foreach (HackableLockedCrate crate in HackCrates) if (crate.IsExists()) crate.Kill();

                foreach (MagnetCrane crane in Cranes)
                {
                    if (!crane.IsExists()) continue;
                    crane.GetFuelSystem().GetFuelContainer().inventory.ClearItemsContainer();
                    crane.Kill();
                }
                
                foreach (BaseCombatEntity car in BrokenCars) if (car.IsExists()) car.Kill();

                DestroyParachute();
                if (Truck.IsExists()) Truck.Kill();

                foreach (SamSite samSite in SamSites) if (samSite.IsExists()) samSite.Kill();
            }

            private void OnTriggerEnter(Collider other) => EnterPlayer(other.GetComponentInParent<BasePlayer>());

            internal void EnterPlayer(BasePlayer player)
            {
                if (!player.IsPlayer()) return;
                if (Players.Contains(player)) return;
                Players.Add(player);
                if (_config.IsCreateZonePvp) _ins.AlertToPlayer(player, _ins.GetMessage("EnterPVP", player.UserIDString, _config.Prefix));
                if (_config.Gui.IsGui) UpdateGui(player);
            }

            private void OnTriggerExit(Collider other)
            {
                ExitPlayer(other.GetComponentInParent<BasePlayer>());
                BaseCombatEntity entity = other.GetComponentInParent<BaseCombatEntity>();
                if (entity.IsExists() && entity.ShortPrefabName == "shreddable_pickuptruck" && BrokenCars.Contains(entity) && Vector3.Distance(transform.position, entity.transform.position) > 25f)
                {
                    BrokenCars.Remove(entity);
                    BaseCombatEntity car = GameManager.server.CreateEntity("assets/content/vehicles/crane_magnet/shreddable_pickuptruck.prefab", GetSpawnPosBrokenCar()) as BaseCombatEntity;
                    car.enableSaving = false;
                    car.Spawn();
                    BrokenCars.Add(car);
                    if (entity.net.ID.Value == BrokenCarId) BrokenCarId = BrokenCars.GetRandom().net.ID.Value;
                }
            }

            internal void ExitPlayer(BasePlayer player)
            {
                if (!player.IsPlayer()) return;
                if (!Players.Contains(player)) return;
                Players.Remove(player);
                if (_config.IsCreateZonePvp) _ins.AlertToPlayer(player, _ins.GetMessage("ExitPVP", player.UserIDString, _config.Prefix));
                if (_config.Gui.IsGui) CuiHelper.DestroyUi(player, "Tabs_KpucTaJl");
            }

            private void InvokeUpdates()
            {
                if (_config.Gui.IsGui) foreach (BasePlayer player in Players) UpdateGui(player);
                UpdateVendingMarker();
                UpdateMarkerForPlayers();
                UpdateTimeToFinish();
            }

            private void UpdateGui(BasePlayer player)
            {
                Dictionary<string, string> dic = new Dictionary<string, string> { ["Clock_KpucTaJl"] = GetTimeFormat(TimeToFinish) };
                if (Scientists.Count > 0) dic.Add("Npc_KpucTaJl", Scientists.Count.ToString());
                _ins.CreateTabs(player, dic);
            }

            private void SpawnMapMarker(MarkerConfig config)
            {
                if (!config.Enabled) return;

                MapMarkerGenericRadius background = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", transform.position) as MapMarkerGenericRadius;
                background.Spawn();
                background.radius = config.Type == 0 ? config.Radius : 0.37967f;
                background.alpha = config.Alpha;
                background.color1 = new Color(config.Color.R, config.Color.G, config.Color.B);
                background.color2 = new Color(config.Color.R, config.Color.G, config.Color.B);
                Markers.Add(background);

                if (config.Type == 1)
                {
                    foreach (Vector3 pos in _ins.Marker)
                    {
                        MapMarkerGenericRadius marker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", transform.position + pos) as MapMarkerGenericRadius;
                        marker.Spawn();
                        marker.radius = 0.008f;
                        marker.alpha = 1f;
                        marker.color1 = new Color(config.Color.R, config.Color.G, config.Color.B);
                        marker.color2 = new Color(config.Color.R, config.Color.G, config.Color.B);
                        Markers.Add(marker);
                    }
                }

                VendingMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", transform.position) as VendingMachineMapMarker;
                VendingMarker.Spawn();

                UpdateMapMarkers();
            }

            private void UpdateVendingMarker()
            {
                if (!_config.Marker.Enabled) return;
                VendingMarker.markerShopName = _config.Marker.Text + "\n" + GetTimeFormat(TimeToFinish);
                if (Owner != null) VendingMarker.markerShopName += $"\n{Owner.displayName}";
                VendingMarker.SendNetworkUpdate();
            }

            internal void UpdateMapMarkers() { foreach (MapMarkerGenericRadius marker in Markers) marker.SendUpdate(); }

            private void UpdateMarkerForPlayers()
            {
                if (Players.Count == 0) return;
                if (_config.MainPoint.Enabled && PlayersInCrane.Count > 0)
                {
                    HashSet<Vector3> points = new HashSet<Vector3>();
                    foreach (BaseCombatEntity car in BrokenCars) if (car.IsExists()) points.Add(car.transform.position);
                    if (Truck.IsExists()) points.Add(Truck.transform.position);
                    if (points.Count > 0) foreach (BasePlayer player in PlayersInCrane) foreach (Vector3 point in points) UpdateMarkerForPlayer(player, point, _config.MainPoint);
                    points = null;
                }
                if (_config.AdditionalPoint.Enabled && HeliCrates.Count + BradleyCrates.Count + HackCrates.Count > 0)
                {
                    HashSet<Vector3> points = new HashSet<Vector3>();
                    foreach (LockedByEntCrate crate in HeliCrates) if (crate.IsExists()) points.Add(crate.transform.position);
                    foreach (LockedByEntCrate crate in BradleyCrates) if (crate.IsExists()) points.Add(crate.transform.position);
                    foreach (HackableLockedCrate crate in HackCrates) if (crate.IsExists()) points.Add(crate.transform.position);
                    if (points.Count > 0) foreach (BasePlayer player in Players) foreach (Vector3 point in points) UpdateMarkerForPlayer(player, point, _config.AdditionalPoint);
                    points = null;
                }
            }

            private void UpdateTimeToFinish()
            {
                if (SpawnCratesCoroutine != null && HeliCrates.Count + BradleyCrates.Count + HackCrates.Count == 0 && TimeToFinish > _config.PreFinishTime) TimeToFinish = _config.PreFinishTime;
                else TimeToFinish--;
                if (TimeToFinish == _config.PreFinishTime) _ins.AlertToAllPlayers("PreFinish", _config.Prefix, GetTimeFormat(_config.PreFinishTime));
                else if (TimeToFinish == 0)
                {
                    CancelInvoke(InvokeUpdates);
                    _ins.Finish();
                }
            }

            private Vector3 GetGlobalPosition(Vector3 localPosition) => transform.TransformPoint(localPosition);

            private Quaternion GetGlobalRotation(Vector3 localRotation) => transform.rotation * Quaternion.Euler(localRotation);

            private static HashSet<T> GetEntities<T>(Vector3 position, float radius, int layerMask) where T : BaseEntity
            {
                HashSet<T> result = new HashSet<T>();
                List<T> list = Pool.GetList<T>();
                Vis.Entities<T>(position, radius, list, layerMask);
                foreach (T entity in list) result.Add(entity);
                Pool.FreeList(ref list);
                return result;
            }

            private static T GetNearEntity<T>(Vector3 position, float radius, int layerMask) where T : BaseEntity
            {
                List<T> list = Pool.GetList<T>();
                Vis.Entities<T>(position, radius, list, layerMask);
                T result = list.Count == 0 ? null : list.Min(s => Vector3.Distance(position, s.transform.position));
                Pool.FreeList(ref list);
                return result;
            }

            private void ChechTrash(Vector3 pos, float radius) { foreach (BaseEntity entity in GetEntities<BaseEntity>(pos, radius, -1)) if (_ins.TrashList.Contains(entity.ShortPrefabName) && entity.IsExists()) entity.Kill(); }

            private void SpawnSamSites()
            {
                foreach (KeyValuePair<Vector3, Vector3> dic in SamSitePositions)
                {
                    SamSite samSite = GameManager.server.CreateEntity("assets/prefabs/npc/sam_site_turret/sam_static.prefab", GetGlobalPosition(dic.Key), GetGlobalRotation(dic.Value)) as SamSite;
                    samSite.enableSaving = false;
                    samSite.Spawn();
                    SamSites.Add(samSite);
                }
            }

            private void FindAllBrokenCars()
            {
                foreach (BaseCombatEntity entity in GetEntities<BaseCombatEntity>(transform.position, _config.Radius, 1 << 15)) if (entity.ShortPrefabName == "shreddable_pickuptruck") BrokenCars.Add(entity);
                int count = _config.CountBrokenCars - BrokenCars.Count;
                if (count < 0)
                {
                    while (BrokenCars.Count > _config.CountBrokenCars)
                    {
                        BaseCombatEntity car = BrokenCars.GetRandom();
                        BrokenCars.Remove(car);
                        if (car.IsExists()) car.Kill();
                    }
                    BrokenCarId = BrokenCars.GetRandom().net.ID.Value;
                }
                else if (count == 0) BrokenCarId = BrokenCars.GetRandom().net.ID.Value;
                else SpawnBrokenCarsCoroutine = ServerMgr.Instance.StartCoroutine(SpawnBrokenCars(count));
            }

            internal IEnumerator SpawnBrokenCars(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector3 pos = GetSpawnPosBrokenCar();
                    if (pos == Vector3.zero) continue;
                    BaseCombatEntity car = GameManager.server.CreateEntity("assets/content/vehicles/crane_magnet/shreddable_pickuptruck.prefab", pos) as BaseCombatEntity;
                    car.enableSaving = false;
                    car.Spawn();
                    BrokenCars.Add(car);
                    yield return CoroutineEx.waitForSeconds(0.5f);
                }
                BrokenCarId = BrokenCars.GetRandom().net.ID.Value;
            }

            private Vector3 GetSpawnPosBrokenCar()
            {
                foreach (Vector3 pos in CarPositionsGlobal) if (IsValidPlaceSpawnBrokenCar(pos)) return pos;
                return Vector3.zero;
            }

            private static bool IsValidPlaceSpawnBrokenCar(Vector3 pos)
            {
                foreach (BaseEntity entity in GetEntities<BaseEntity>(pos, 4f, 1 << 15 | 1 << 17)) if (entity.ShortPrefabName == "shreddable_pickuptruck" || entity is BasePlayer) return false;
                return true;
            }

            internal void CheckCar(ulong id)
            {
                if (CheckedCars.Contains(id)) return;
                else CheckedCars.Add(id);

                BasePlayer player = GetPlayerCrane;

                if (id == BrokenCarId)
                {
                    if (player != null) _ins.ActionEconomy(player.userID, "ShredderCar");
                    _ins.AlertToAllPlayers("KillBrokenCar", _config.Prefix, player != null ? player.displayName : "Player");
                    BrokenCarId = 0;
                    BrokenCars.Clear();
                    SpawnEntityInConveyor("assets/prefabs/tools/supply signal/grenade.supplysignal.deployed.prefab");
                }
                else if (id == TruckId)
                {
                    if (player != null) _ins.ActionEconomy(player.userID, "ShredderTruck");
                    _ins.AlertToAllPlayers("KillTruck", _config.Prefix, player != null ? player.displayName : "Player");
                    TruckId = 0;
                    SpawnCratesCoroutine = ServerMgr.Instance.StartCoroutine(SpawnCrates());
                }
                else if (BrokenCarId != 0 && player != null) _ins.AlertToPlayer(player, _ins.GetMessage("PlayerKillBrokenCar", player.UserIDString, _config.Prefix));
            }

            private BasePlayer GetPlayerCrane => PlayersInCrane.Count == 0 ? null : PlayersInCrane.Min(x => Vector3.Distance(x.transform.position, Shredder.transform.position));

            private void SpawnEntityInConveyor(string prefab)
            {
                BaseEntity entity = GameManager.server.CreateEntity(prefab, SpawnPosRail, SpawnRotRail);
                entity.enableSaving = false;
                entity.Spawn();

                if (entity is SupplySignal) Supply = entity as SupplySignal;
                else if (entity is HackableLockedCrate)
                {
                    HackableLockedCrate hackCrate = entity as HackableLockedCrate;
                    hackCrate.hackSeconds = HackableLockedCrate.requiredHackSeconds - _config.HackCrates.UnlockTime;
                    HackCrates.Add(hackCrate);
                    HackCrateConfig config = _config.HackCrates;
                    if (config.TypeLootTable == 1 || config.TypeLootTable == 4 || config.TypeLootTable == 5)
                    {
                        _ins.NextTick(() =>
                        {
                            hackCrate.inventory.ClearItemsContainer();
                            if (config.TypeLootTable == 4 || config.TypeLootTable == 5) _ins.AddToContainerPrefab(hackCrate.inventory, config.PrefabLootTable);
                            if (config.TypeLootTable == 1 || config.TypeLootTable == 5) _ins.AddToContainerItem(hackCrate.inventory, config.OwnLootTable);
                        });
                    }
                }
                else if (entity is LockedByEntCrate)
                {
                    LockedByEntCrate crate = entity as LockedByEntCrate;
                    if (entity.ShortPrefabName == "heli_crate") HeliCrates.Add(crate);
                    else BradleyCrates.Add(crate);
                    entity.SetFlag(BaseEntity.Flags.Locked, true);
                }

                AnimationTransform animation = entity.gameObject.AddComponent<AnimationTransform>();
                animation.AddPath(RailPointsGlobal);
            }

            private IEnumerator SpawnCrates()
            {
                for (int i = 0; i < _config.HeliCrates.Count; i++)
                {
                    SpawnEntityInConveyor("assets/prefabs/npc/patrol helicopter/heli_crate.prefab");
                    yield return CoroutineEx.waitForSeconds(1f);
                }
                for (int i = 0; i < _config.BradleyCrates.Count; i++)
                {
                    SpawnEntityInConveyor("assets/prefabs/npc/m2bradley/bradley_crate.prefab");
                    yield return CoroutineEx.waitForSeconds(1f);
                }
                for (int i = 0; i < _config.HackCrates.Count; i++)
                {
                    SpawnEntityInConveyor("assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab");
                    yield return CoroutineEx.waitForSeconds(1f);
                }
                yield return CoroutineEx.waitForSeconds(4f);
                if (_ins.ActivePveMode)
                {
                    HashSet<ulong> crates = new HashSet<ulong>();
                    foreach (LockedByEntCrate crate in HeliCrates) crates.Add(crate.net.ID.Value);
                    foreach (LockedByEntCrate crate in BradleyCrates) crates.Add(crate.net.ID.Value);
                    foreach (HackableLockedCrate crate in HackCrates) crates.Add(crate.net.ID.Value);
                    _ins.PveMode.Call("EventAddCrates", _ins.Name, crates);
                    crates = null;
                }
            }

            internal void AddRigidBody(LockedByEntCrate ent)
            {
                DroppedItemContainer backpack = GameManager.server.CreateEntity("assets/prefabs/misc/item drop/item_drop_backpack.prefab", ent.transform.position, ent.transform.rotation) as DroppedItemContainer;
                backpack.enableSaving = false;
                backpack.Spawn();
                backpack.CancelInvoke(backpack.RemoveMe);
                BackpackCrates.Add(backpack);

                LockedByEntCrate entity = GameManager.server.CreateEntity(ent.PrefabName) as LockedByEntCrate;
                entity.enableSaving = false;
                entity.SetParent(backpack);

                if (ent.IsExists())
                {
                    if (ent.ShortPrefabName == "heli_crate") HeliCrates.Remove(ent);
                    else BradleyCrates.Remove(ent);
                    ent.Kill();
                }

                entity.Spawn();
                if (entity.ShortPrefabName == "heli_crate") HeliCrates.Add(entity);
                else BradleyCrates.Add(entity);
                entity.SetFlag(BaseEntity.Flags.Locked, true);

                backpack.GetComponent<Rigidbody>().AddForce(backpack.transform.forward * 100f, ForceMode.Force);

                Invoke(() =>
                {
                    if (!backpack.IsExists() || !entity.IsExists()) return;

                    LockedByEntCrate crate = GameManager.server.CreateEntity(entity.PrefabName, entity.transform.position, entity.transform.rotation) as LockedByEntCrate;
                    crate.enableSaving = false;
                    crate.Spawn();

                    if (entity.ShortPrefabName == "heli_crate") HeliCrates.Remove(entity);
                    else BradleyCrates.Remove(entity);
                    BackpackCrates.Remove(backpack);
                    backpack.Kill();

                    if (crate.ShortPrefabName == "heli_crate")
                    {
                        HeliCrates.Add(crate);
                        CrateConfig config = _config.HeliCrates;
                        if (config.TypeLootTable == 1 || config.TypeLootTable == 4 || config.TypeLootTable == 5)
                        {
                            _ins.NextTick(() =>
                            {
                                crate.inventory.ClearItemsContainer();
                                if (config.TypeLootTable == 4 || config.TypeLootTable == 5) _ins.AddToContainerPrefab(crate.inventory, config.PrefabLootTable);
                                if (config.TypeLootTable == 1 || config.TypeLootTable == 5) _ins.AddToContainerItem(crate.inventory, config.OwnLootTable);
                            });
                        }
                    }
                    else
                    {
                        BradleyCrates.Add(crate);
                        CrateConfig config = _config.BradleyCrates;
                        if (config.TypeLootTable == 1 || config.TypeLootTable == 4 || config.TypeLootTable == 5)
                        {
                            _ins.NextTick(() =>
                            {
                                crate.inventory.ClearItemsContainer();
                                if (config.TypeLootTable == 4 || config.TypeLootTable == 5) _ins.AddToContainerPrefab(crate.inventory, config.PrefabLootTable);
                                if (config.TypeLootTable == 1 || config.TypeLootTable == 5) _ins.AddToContainerItem(crate.inventory, config.OwnLootTable);
                            });
                        }
                    }
                }, 5f);
            }

            private void SpawnCrane(Vector3 pos, Vector3 rot)
            {
                ChechTrash(pos, 5f);

                MagnetCrane crane = GameManager.server.CreateEntity("assets/content/vehicles/crane_magnet/magnetcrane.entity.prefab", pos, Quaternion.Euler(rot)) as MagnetCrane;
                crane.enableSaving = false;
                crane.Spawn();

                if (!_config.IsFuelCrane)
                {
                    StorageContainer container = crane.GetFuelSystem().GetFuelContainer();
                    ItemManager.CreateByName("lowgradefuel", 1000000).MoveToContainer(container.inventory);
                    container.SetFlag(BaseEntity.Flags.Locked, true);
                }

                if (!Cranes.Contains(crane)) Cranes.Add(crane);
            }

            internal void KillCrane()
            {
                if (IsValidPlaceSpawnCrane(MainCranePos)) SpawnCrane(MainCranePos, MainCraneRot);
                else SpawnCrane(AddCranePos, AddCraneRot);
            }

            private static bool IsValidPlaceSpawnCrane(Vector3 pos)
            {
                foreach (BaseEntity entity in GetEntities<BaseEntity>(pos, 5f, -1))
                    if (entity is MagnetCrane || entity is BasePlayer)
                        return false;
                return true;
            }

            internal void SpawnTruck(Vector3 pos)
            {
                Truck = GameManager.server.CreateEntity("assets/content/vehicles/modularcar/2module_car_spawned.entity.prefab", pos, transform.rotation) as ModularCar;
                Truck.enableSaving = false;
                Truck.spawnSettings.useSpawnSettings = false;
                Truck.Spawn();

                Truck.GetFuelSystem().GetFuelContainer().inventory.capacity = 0;

                TruckId = Truck.net.ID.Value;

                Truck.transform.position = new Vector3(LandingTruckPos.x, Truck.transform.position.y, LandingTruckPos.z);
                Truck.transform.rotation = transform.rotation;

                Item moduleItem = ItemManager.CreateByName("vehicle.2mod.camper");
                if (!Truck.TryAddModule(moduleItem)) moduleItem.Remove();
                _ins.NextTick(() => Module = Truck.AttachedModuleEntities[0] as VehicleModuleCamper);

                Truck.rigidBody.useGravity = false;
                Truck.rigidBody.detectCollisions = false;

                SpawnParachute();

                ChechTrash(LandingTruckPos, 2f);

                Truck.rigidBody.AddForce(Vector3.down * 10000f, ForceMode.Force);
                InvokeRepeating(UpdateTruck, 0, 0.5f);
            }

            private void UpdateTruck()
            {
                if (Truck.transform.position.y - LandingTruckPos.y > 100f) Truck.rigidBody.AddForce(Vector3.down * 40000f, ForceMode.Force);
                else Truck.rigidBody.AddForce(Vector3.down * 20000f, ForceMode.Force);
                if (Truck.transform.position.y - LandingTruckPos.y < 1f)
                {
                    Truck.transform.position = LandingTruckPos;
                    Truck.transform.rotation = transform.rotation;
                    Truck.rigidBody.useGravity = true;
                    Truck.rigidBody.detectCollisions = true;
                    DestroyParachute();
                    SpawnPreset(_config.NpcTruck);
                    if (_ins.ActivePveMode) _ins.PveMode.Call("EventAddScientists", _ins.Name, Scientists.Select(x => x.net.ID.Value));
                    _ins.AlertToAllPlayers("TruckArrived", _config.Prefix);
                    CancelInvoke(UpdateTruck);
                }
            }

            private void SpawnParachute()
            {
                Parachute parachute = GameManager.server.CreateEntity("assets/prefabs/misc/parachute/parachute.prefab") as Parachute;

                Parachute = parachute.gameObject.AddComponent<BaseVehicle>();
                CopySerializableFields(parachute, Parachute);
                DestroyImmediate(parachute, true);

                Parachute.enableSaving = false;

                Parachute.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                Parachute.SetParent(Truck);

                Parachute.Spawn();

                Parachute.SetToKinematic();

                PlayerParachute = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", Truck.transform.position) as BasePlayer;
                PlayerParachute.Spawn();

                PlayerParachute.DisablePlayerCollider();
                PlayerParachute.playerRigidbody.isKinematic = true;

                Parachute.AttemptMount(PlayerParachute, false);
            }

            private void DestroyParachute()
            {
                if (Parachute.IsExists()) Parachute.Kill();
                if (PlayerParachute.IsExists()) PlayerParachute.Kill();
            }

            private void SpawnPreset(PresetConfig preset)
            {
                int count = UnityEngine.Random.Range(preset.Min, preset.Max + 1);

                List<Vector3> positions = Pool.GetList<Vector3>();
                foreach (string pos in preset.Positions) positions.Add(GetGlobalPosition(pos.ToVector3()));

                JObject config = GetObjectConfig(preset.Config);

                for (int i = 0; i < count; i++)
                {
                    Vector3 pos = positions.GetRandom();
                    positions.Remove(pos);
                    ScientistNPC npc = (ScientistNPC)_ins.NpcSpawn.Call("SpawnNpc", pos, config);
                    Scientists.Add(npc);
                }

                Pool.FreeList(ref positions);
            }

            private static JObject GetObjectConfig(NpcConfig config)
            {
                HashSet<string> states = config.Stationary ? new HashSet<string> { "IdleState", "CombatStationaryState" } : new HashSet<string> { "RoamState", "ChaseState", "CombatState" };
                if (config.BeltItems.Any(x => x.ShortName == "rocket.launcher" || x.ShortName == "explosive.timed")) states.Add("RaidState");
                return new JObject
                {
                    ["Name"] = config.Name,
                    ["WearItems"] = new JArray { config.WearItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["SkinID"] = x.SkinId }) },
                    ["BeltItems"] = new JArray { config.BeltItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["Amount"] = x.Amount, ["SkinID"] = x.SkinId, ["Mods"] = new JArray { x.Mods }, ["Ammo"] = x.Ammo }) },
                    ["Kit"] = config.Kit,
                    ["Health"] = config.Health,
                    ["RoamRange"] = config.RoamRange,
                    ["ChaseRange"] = config.ChaseRange,
                    ["SenseRange"] = config.SenseRange,
                    ["ListenRange"] = config.SenseRange / 2f,
                    ["AttackRangeMultiplier"] = config.AttackRangeMultiplier,
                    ["CheckVisionCone"] = config.CheckVisionCone,
                    ["VisionCone"] = config.VisionCone,
                    ["HostileTargetsOnly"] = false,
                    ["DamageScale"] = config.DamageScale,
                    ["TurretDamageScale"] = 0f,
                    ["AimConeScale"] = config.AimConeScale,
                    ["DisableRadio"] = config.DisableRadio,
                    ["CanRunAwayWater"] = true,
                    ["CanSleep"] = false,
                    ["SleepDistance"] = 100f,
                    ["Speed"] = config.Speed,
                    ["AreaMask"] = 1,
                    ["AgentTypeID"] = -1372625422,
                    ["HomePosition"] = string.Empty,
                    ["MemoryDuration"] = config.MemoryDuration,
                    ["States"] = new JArray { states }
                };
            }

            internal void EnablePveMode(PveModeConfig config, BasePlayer player)
            {
                if (!_ins.ActivePveMode) return;

                JObject objectConfig = new JObject
                {
                    ["Damage"] = config.Damage,
                    ["ScaleDamage"] = new JArray { config.ScaleDamage.Select(x => new JObject { ["Type"] = x.Type, ["Scale"] = x.Scale }) },
                    ["LootCrate"] = config.LootCrate,
                    ["HackCrate"] = config.HackCrate,
                    ["LootNpc"] = config.LootNpc,
                    ["DamageNpc"] = config.DamageNpc,
                    ["DamageTank"] = false,
                    ["DamageHelicopter"] = false,
                    ["DamageTurret"] = false,
                    ["TargetNpc"] = config.TargetNpc,
                    ["TargetTank"] = false,
                    ["TargetHelicopter"] = false,
                    ["TargetTurret"] = false,
                    ["CanEnter"] = config.CanEnter,
                    ["CanEnterCooldownPlayer"] = config.CanEnterCooldownPlayer,
                    ["TimeExitOwner"] = config.TimeExitOwner,
                    ["AlertTime"] = config.AlertTime,
                    ["RestoreUponDeath"] = config.RestoreUponDeath,
                    ["CooldownOwner"] = config.CooldownOwner,
                    ["Darkening"] = config.Darkening
                };

                _ins.PveMode.Call("EventAddPveMode", _ins.Name, objectConfig, transform.position, _config.Radius, new HashSet<ulong>(), Scientists.Select(x => x.net.ID.Value), new HashSet<ulong>(), new HashSet<ulong>(), new HashSet<ulong>(), player);
            }
        }
        #endregion Controller

        #region Animation
        internal class PointAnimationTransform { public float Time; public Vector3 Pos; public Vector3 Rot; }

        internal class AnimationTransform : FacepunchBehaviour
        {
            private BaseEntity Main { get; set; } = null;

            private List<PointAnimationTransform> Path { get; } = new List<PointAnimationTransform>();

            private Rigidbody Rigidbody { get; set; } = null;

            private float SecondsTaken { get; set; } = 0f;
            private float SecondsToTake { get; set; } = 0f;
            private float WaypointDone { get; set; } = 0f;

            private Vector3 StartPos { get; set; } = Vector3.zero;
            private Vector3 EndPos { get; set; } = Vector3.zero;

            private Vector3 StartRot { get; set; } = Vector3.zero;
            private Vector3 EndRot { get; set; } = Vector3.zero;

            private void Awake()
            {
                Main = GetComponent<BaseEntity>();
                Rigidbody = GetComponent<Rigidbody>();
                enabled = false;
            }

            internal void AddPath(HashSet<PointAnimationTransform> path)
            {
                foreach (PointAnimationTransform point in path) Path.Add(point);
                if (Rigidbody != null) Rigidbody.isKinematic = true;
                enabled = true;
            }

            private void FixedUpdate()
            {
                if (SecondsTaken == 0f)
                {
                    if (Path.Count == 0)
                    {
                        StartPos = EndPos = Vector3.zero;
                        StartRot = EndRot = Vector3.zero;
                        SecondsToTake = 0f;
                        SecondsTaken = 0f;
                        WaypointDone = 0f;
                        enabled = false;
                        if (Rigidbody != null)
                        {
                            Rigidbody.isKinematic = false;
                            float mass = Rigidbody.mass;
                            Rigidbody.mass = 1f;
                            Rigidbody.AddForce(Main.transform.forward * 100f, ForceMode.Force);
                            Invoke(() => Rigidbody.mass = mass, 5f);
                        }
                        if (Main is LockedByEntCrate) _ins.Controller.AddRigidBody(Main as LockedByEntCrate);
                        return;
                    }
                    StartPos = transform.position;
                    StartRot = transform.rotation.eulerAngles;
                    if (Path[0].Pos != StartPos || Path[0].Rot != StartRot)
                    {
                        EndPos = Path[0].Pos != StartPos ? Path[0].Pos : StartPos;
                        EndRot = Path[0].Rot != StartRot ? Path[0].Rot : StartRot;
                        SecondsToTake = Path[0].Time;
                        SecondsTaken = 0f;
                        WaypointDone = 0f;
                    }
                    Path.RemoveAt(0);
                }
                if (StartPos != EndPos || StartRot != EndRot)
                {
                    SecondsTaken += Time.deltaTime;
                    WaypointDone = Mathf.InverseLerp(0f, SecondsToTake, SecondsTaken);
                    if (StartPos != EndPos) transform.position = Vector3.Lerp(StartPos, EndPos, WaypointDone);
                    if (StartRot != EndRot) transform.rotation = Quaternion.Lerp(Quaternion.Euler(StartRot), Quaternion.Euler(EndRot), WaypointDone);
                    Main.TransformChanged();
                    Main.SendNetworkUpdate();
                    if (WaypointDone >= 1f) SecondsTaken = 0f;
                }
            }
        }
        #endregion Animation

        #region Find Position
        internal MonumentInfo GetMonument()
        {
            List<MonumentInfo> list = Pool.GetList<MonumentInfo>();
            foreach (MonumentInfo monument in TerrainMeta.Path.Monuments)
            {
                if (monument.displayPhrase.english != "Junkyard") continue;
                list.Add(monument);
            }
            MonumentInfo result = list.Count > 0 ? list.GetRandom() : null;
            Pool.FreeList(ref list);
            return result;
        }
        #endregion Find Position

        #region Spawn Loot
        #region NPC
        private void OnCorpsePopulate(ScientistNPC entity, NPCPlayerCorpse corpse)
        {
            if (entity == null) return;
            if (Controller.Scientists.Contains(entity))
            {
                Controller.Scientists.Remove(entity);
                PresetConfig preset = _config.PresetsNpc.FirstOrDefault(x => x.Config.Name == entity.displayName) ?? _config.NpcTruck;
                NextTick(() =>
                {
                    if (corpse == null) return;
                    ItemContainer container = corpse.containers[0];
                    if (preset.TypeLootTable == 1 || preset.TypeLootTable == 4 || preset.TypeLootTable == 5)
                    {
                        container.ClearItemsContainer();
                        if (preset.TypeLootTable == 4 || preset.TypeLootTable == 5) AddToContainerPrefab(container, preset.PrefabLootTable);
                        if (preset.TypeLootTable == 1 || preset.TypeLootTable == 5) AddToContainerItem(container, preset.OwnLootTable);
                    }
                    if (preset.Config.IsRemoveCorpse && !corpse.IsDestroyed) corpse.Kill();
                });
            }
        }

        private object CanPopulateLoot(ScientistNPC entity, NPCPlayerCorpse corpse)
        {
            if (entity == null || corpse == null || Controller == null) return null;
            if (Controller.Scientists.Contains(entity))
            {
                PresetConfig preset = _config.PresetsNpc.FirstOrDefault(x => x.Config.Name == entity.displayName) ?? _config.NpcTruck;
                if (preset.TypeLootTable == 2) return null;
                else return true;
            }
            return null;
        }

        private object OnCustomLootNPC(NetworkableId netId)
        {
            if (Controller == null) return null;
            ScientistNPC entity = Controller.Scientists.FirstOrDefault(x => x.IsExists() && x.net.ID.Value == netId.Value);
            if (entity != null)
            {
                PresetConfig preset = _config.PresetsNpc.FirstOrDefault(x => x.Config.Name == entity.displayName) ?? _config.NpcTruck;
                if (preset.TypeLootTable == 3) return null;
                else return true;
            }
            return null;
        }
        #endregion NPC

        #region Crates
        private object CanPopulateLoot(LootContainer container)
        {
            if (container == null || Controller == null) return null;
            if (container is LockedByEntCrate)
            {
                LockedByEntCrate crate = container as LockedByEntCrate;
                if (Controller.HeliCrates.Contains(crate))
                {
                    if (_config.HeliCrates.TypeLootTable == 2) return null;
                    else return true;
                }
                else if (Controller.BradleyCrates.Contains(crate))
                {
                    if (_config.BradleyCrates.TypeLootTable == 2) return null;
                    else return true;
                }
            }
            else if (container is HackableLockedCrate && Controller.HackCrates.Contains(container as HackableLockedCrate))
            {
                if (_config.HackCrates.TypeLootTable == 2) return null;
                else return true;
            }
            return null;
        }

        private object OnCustomLootContainer(NetworkableId netId)
        {
            if (Controller == null) return null;
            if (Controller.HeliCrates.Any(x => x.IsExists() && x.net.ID.Value == netId.Value))
            {
                if (_config.HeliCrates.TypeLootTable == 3) return null;
                else return true;
            }
            else if (Controller.BradleyCrates.Any(x => x.IsExists() && x.net.ID.Value == netId.Value))
            {
                if (_config.BradleyCrates.TypeLootTable == 3) return null;
                else return true;
            }
            else if (Controller.HackCrates.Any(x => x.IsExists() && x.net.ID.Value == netId.Value))
            {
                if (_config.HackCrates.TypeLootTable == 3) return null;
                else return true;
            }
            return null;
        }

        private object OnContainerPopulate(LootContainer container)
        {
            if (container == null || Controller == null) return null;
            if (container is LockedByEntCrate)
            {
                LockedByEntCrate crate = container as LockedByEntCrate;
                if (Controller.HeliCrates.Contains(crate))
                {
                    if (_config.HeliCrates.TypeLootTable == 6) return null;
                    else return true;
                }
                else if (Controller.BradleyCrates.Contains(crate))
                {
                    if (_config.BradleyCrates.TypeLootTable == 6) return null;
                    else return true;
                }
            }
            else if (container is HackableLockedCrate && Controller.HackCrates.Contains(container as HackableLockedCrate))
            {
                if (_config.HackCrates.TypeLootTable == 6) return null;
                else return true;
            }
            return null;
        }
        #endregion Crates

        private void AddToContainerPrefab(ItemContainer container, PrefabLootTableConfig lootTable)
        {
            if (lootTable.UseCount)
            {
                int count = 0, max = UnityEngine.Random.Range(lootTable.Min, lootTable.Max + 1);
                while (count < max)
                {
                    foreach (PrefabConfig prefab in lootTable.Prefabs)
                    {
                        if (UnityEngine.Random.Range(0f, 100f) > prefab.Chance) continue;
                        SpawnIntoContainer(container, prefab.PrefabDefinition);
                        count++;
                        if (count == max) break;
                    }
                }
            }
            else foreach (PrefabConfig prefab in lootTable.Prefabs) if (UnityEngine.Random.Range(0f, 100f) <= prefab.Chance) SpawnIntoContainer(container, prefab.PrefabDefinition);
        }

        private void SpawnIntoContainer(ItemContainer container, string prefab)
        {
            if (_allLootSpawnSlots.ContainsKey(prefab))
            {
                foreach (LootContainer.LootSpawnSlot lootSpawnSlot in _allLootSpawnSlots[prefab])
                    for (int j = 0; j < lootSpawnSlot.numberToSpawn; j++)
                        if (UnityEngine.Random.Range(0f, 1f) <= lootSpawnSlot.probability)
                            lootSpawnSlot.definition.SpawnIntoContainer(container);
            }
            else _allLootSpawn[prefab].SpawnIntoContainer(container);
        }

        private void AddToContainerItem(ItemContainer container, LootTableConfig lootTable)
        {
            if (lootTable.UseCount)
            {
                HashSet<int> indexMove = new HashSet<int>();
                int count = UnityEngine.Random.Range(lootTable.Min, lootTable.Max + 1);
                while (indexMove.Count < count)
                {
                    for (int i = 0; i < lootTable.Items.Count; i++)
                    {
                        if (indexMove.Contains(i)) continue;
                        if (SpawnIntoContainer(container, lootTable.Items[i]))
                        {
                            indexMove.Add(i);
                            if (indexMove.Count == count) break;
                        }
                    }
                }
                indexMove = null;
            }
            else foreach (ItemConfig item in lootTable.Items) SpawnIntoContainer(container, item);
        }

        private bool SpawnIntoContainer(ItemContainer container, ItemConfig config)
        {
            if (UnityEngine.Random.Range(0f, 100f) > config.Chance) return false;
            Item item = config.IsBluePrint ? ItemManager.CreateByName("blueprintbase") : ItemManager.CreateByName(config.ShortName, UnityEngine.Random.Range(config.MinAmount, config.MaxAmount + 1), config.SkinId);
            if (item == null)
            {
                PrintWarning($"Failed to create item! ({config.ShortName})");
                return false;
            }
            if (config.IsBluePrint) item.blueprintTarget = ItemManager.FindItemDefinition(config.ShortName).itemid;
            if (!string.IsNullOrEmpty(config.Name)) item.name = config.Name;
            if (container.capacity < container.itemList.Count + 1) container.capacity++;
            if (!item.MoveToContainer(container))
            {
                item.Remove();
                return false;
            }
            return true;
        }

        private void CheckAllLootTables()
        {
            CheckLootTable(_config.HeliCrates.OwnLootTable);
            CheckPrefabLootTable(_config.HeliCrates.PrefabLootTable);

            CheckLootTable(_config.BradleyCrates.OwnLootTable);
            CheckPrefabLootTable(_config.BradleyCrates.PrefabLootTable);

            CheckLootTable(_config.HackCrates.OwnLootTable);
            CheckPrefabLootTable(_config.HackCrates.PrefabLootTable);

            foreach (PresetConfig preset in _config.PresetsNpc)
            {
                CheckLootTable(preset.OwnLootTable);
                CheckPrefabLootTable(preset.PrefabLootTable);
            }

            CheckLootTable(_config.NpcTruck.OwnLootTable);
            CheckPrefabLootTable(_config.NpcTruck.PrefabLootTable);

            SaveConfig();
        }

        private void CheckLootTable(LootTableConfig lootTable)
        {
            for (int i = lootTable.Items.Count - 1; i >= 0; i--)
            {
                ItemConfig item = lootTable.Items[i];

                if (!ItemManager.itemList.Any(x => x.shortname == item.ShortName))
                {
                    PrintWarning($"Unknown item removed! ({item.ShortName})");
                    lootTable.Items.Remove(item);
                    continue;
                }
                if (item.Chance <= 0f)
                {
                    PrintWarning($"An item with an incorrect probability has been removed from the loot table ({item.ShortName})");
                    lootTable.Items.Remove(item);
                    continue;
                }

                if (item.MinAmount <= 0) item.MinAmount = 1;
                if (item.MaxAmount < item.MinAmount) item.MaxAmount = item.MinAmount;
            }

            lootTable.Items = lootTable.Items.OrderByQuickSort(x => x.Chance);
            if (lootTable.Items.Any(x => x.Chance >= 100f))
            {
                HashSet<ItemConfig> newItems = new HashSet<ItemConfig>();

                for (int i = lootTable.Items.Count - 1; i >= 0; i--)
                {
                    ItemConfig itemConfig = lootTable.Items[i];
                    if (itemConfig.Chance < 100f) break;
                    newItems.Add(itemConfig);
                    lootTable.Items.Remove(itemConfig);
                }

                int count = newItems.Count;

                if (count > 0)
                {
                    foreach (ItemConfig itemConfig in lootTable.Items) newItems.Add(itemConfig);
                    lootTable.Items.Clear();
                    foreach (ItemConfig itemConfig in newItems) lootTable.Items.Add(itemConfig);
                }

                newItems = null;

                if (lootTable.Min < count) lootTable.Min = count;
                if (lootTable.Max < count) lootTable.Max = count;
            }

            if (lootTable.Max > lootTable.Items.Count) lootTable.Max = lootTable.Items.Count;
            if (lootTable.Min > lootTable.Max) lootTable.Min = lootTable.Max;
            if (lootTable.Items.Count == 0) lootTable.UseCount = false;
        }

        private void CheckPrefabLootTable(PrefabLootTableConfig lootTable)
        {
            HashSet<string> prefabs = new HashSet<string>();

            for (int i = lootTable.Prefabs.Count - 1; i >= 0; i--)
            {
                PrefabConfig prefab = lootTable.Prefabs[i];
                if (prefabs.Any(x => x == prefab.PrefabDefinition))
                {
                    lootTable.Prefabs.Remove(prefab);
                    PrintWarning($"Duplicate prefab removed from loot table! ({prefab.PrefabDefinition})");
                }
                else
                {
                    GameObject gameObject = GameManager.server.FindPrefab(prefab.PrefabDefinition);
                    global::HumanNPC humanNpc = gameObject.GetComponent<global::HumanNPC>();
                    ScarecrowNPC scarecrowNpc = gameObject.GetComponent<ScarecrowNPC>();
                    LootContainer lootContainer = gameObject.GetComponent<LootContainer>();
                    if (humanNpc != null && humanNpc.LootSpawnSlots.Length != 0)
                    {
                        if (!_allLootSpawnSlots.ContainsKey(prefab.PrefabDefinition)) _allLootSpawnSlots.Add(prefab.PrefabDefinition, humanNpc.LootSpawnSlots);
                        prefabs.Add(prefab.PrefabDefinition);
                    }
                    else if (scarecrowNpc != null && scarecrowNpc.LootSpawnSlots.Length != 0)
                    {
                        if (!_allLootSpawnSlots.ContainsKey(prefab.PrefabDefinition)) _allLootSpawnSlots.Add(prefab.PrefabDefinition, scarecrowNpc.LootSpawnSlots);
                        prefabs.Add(prefab.PrefabDefinition);
                    }
                    else if (lootContainer != null && lootContainer.LootSpawnSlots.Length != 0)
                    {
                        if (!_allLootSpawnSlots.ContainsKey(prefab.PrefabDefinition)) _allLootSpawnSlots.Add(prefab.PrefabDefinition, lootContainer.LootSpawnSlots);
                        prefabs.Add(prefab.PrefabDefinition);
                    }
                    else if (lootContainer != null && lootContainer.lootDefinition != null)
                    {
                        if (!_allLootSpawn.ContainsKey(prefab.PrefabDefinition)) _allLootSpawn.Add(prefab.PrefabDefinition, lootContainer.lootDefinition);
                        prefabs.Add(prefab.PrefabDefinition);
                    }
                    else
                    {
                        lootTable.Prefabs.Remove(prefab);
                        PrintWarning($"Unknown prefab removed! ({prefab.PrefabDefinition})");
                    }
                }
            }

            prefabs = null;

            lootTable.Prefabs = lootTable.Prefabs.OrderByQuickSort(x => x.Chance);
            if (lootTable.Prefabs.Any(x => x.Chance >= 100f))
            {
                HashSet<PrefabConfig> newPrefabs = new HashSet<PrefabConfig>();

                for (int i = lootTable.Prefabs.Count - 1; i >= 0; i--)
                {
                    PrefabConfig prefabConfig = lootTable.Prefabs[i];
                    if (prefabConfig.Chance < 100f) break;
                    newPrefabs.Add(prefabConfig);
                    lootTable.Prefabs.Remove(prefabConfig);
                }

                int count = newPrefabs.Count;

                if (count > 0)
                {
                    foreach (PrefabConfig prefabConfig in lootTable.Prefabs) newPrefabs.Add(prefabConfig);
                    lootTable.Prefabs.Clear();
                    foreach (PrefabConfig prefabConfig in newPrefabs) lootTable.Prefabs.Add(prefabConfig);
                }

                newPrefabs = null;

                if (lootTable.Min < count) lootTable.Min = count;
                if (lootTable.Max < count) lootTable.Max = count;
            }

            if (lootTable.Min > lootTable.Max) lootTable.Min = lootTable.Max;
            if (lootTable.Prefabs.Count == 0) lootTable.UseCount = false;
        }

        private readonly Dictionary<string, LootSpawn> _allLootSpawn = new Dictionary<string, LootSpawn>();

        private readonly Dictionary<string, LootContainer.LootSpawnSlot[]> _allLootSpawnSlots = new Dictionary<string, LootContainer.LootSpawnSlot[]>();
        #endregion Spawn Loot

        #region PveMode
        [PluginReference] private readonly Plugin PveMode;

        internal bool ActivePveMode => _config.PveMode.Pve && plugins.Exists("PveMode");

        private void SetOwnerPveMode(string shortname, BasePlayer player)
        {
            if (string.IsNullOrEmpty(shortname) || shortname != Name || !player.IsPlayer()) return;
            Controller.Owner = player;
            AlertToAllPlayers("SetOwner", _config.Prefix, player.displayName);
        }

        private void ClearOwnerPveMode(string shortname)
        {
            if (string.IsNullOrEmpty(shortname) || shortname != Name) return;
            Controller.Owner = null;
        }
        #endregion PveMode

        #region TruePVE
        private object CanEntityTakeDamage(BasePlayer victim, HitInfo hitinfo)
        {
            if (!_config.IsCreateZonePvp || victim == null || hitinfo == null || Controller == null) return null;
            BasePlayer attacker = hitinfo.InitiatorPlayer;
            if (Controller.Players.Contains(victim) && (attacker == null || Controller.Players.Contains(attacker))) return true;
            else return null;
        }
        #endregion TruePVE

        #region NTeleportation
        private object CanTeleport(BasePlayer player, Vector3 to)
        {
            if (_config.NTeleportationInterrupt && Controller != null && (Controller.Players.Contains(player) || Vector3.Distance(Controller.transform.position, to) < _config.Radius)) return GetMessage("NTeleportation", player.UserIDString, _config.Prefix);
            else return null;
        }

        private void OnPlayerTeleported(BasePlayer player, Vector3 oldPos, Vector3 newPos)
        {
            if (Controller == null || !player.IsPlayer()) return;
            if (!Controller.Players.Contains(player) && Vector3.Distance(Controller.transform.position, newPos) < _config.Radius) Controller.EnterPlayer(player);
            if (Controller.Players.Contains(player) && Vector3.Distance(Controller.transform.position, newPos) > _config.Radius) Controller.ExitPlayer(player);
        }
        #endregion NTeleportation

        #region Economy
        [PluginReference] private readonly Plugin Economics, ServerRewards, IQEconomic, XPerience;

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
                case "ShredderCar":
                    AddBalance(playerId, _config.Economy.ShredderCar);
                    break;
                case "ShredderTruck":
                    AddBalance(playerId, _config.Economy.ShredderTruck);
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
            if (_config.Economy.Plugins.Count > 0)
            {
                foreach (KeyValuePair<ulong, double> dic in _playersBalance)
                {
                    if (dic.Value < _config.Economy.Min) continue;
                    int intCount = Convert.ToInt32(dic.Value);
                    if (_config.Economy.Plugins.Contains("Economics") && plugins.Exists("Economics") && dic.Value > 0) Economics.Call("Deposit", dic.Key.ToString(), dic.Value);
                    if (_config.Economy.Plugins.Contains("Server Rewards") && plugins.Exists("ServerRewards") && intCount > 0) ServerRewards.Call("AddPoints", dic.Key, intCount);
                    if (_config.Economy.Plugins.Contains("IQEconomic") && plugins.Exists("IQEconomic") && intCount > 0) IQEconomic.Call("API_SET_BALANCE", dic.Key, intCount);
                    BasePlayer player = BasePlayer.FindByID(dic.Key);
                    if (player != null)
                    {
                        if (_config.Economy.Plugins.Contains("XPerience") && plugins.Exists("XPerience") && dic.Value > 0) XPerience?.Call("GiveXP", player, dic.Value);
                        AlertToPlayer(player, GetMessage("SendEconomy", player.UserIDString, _config.Prefix, dic.Value));
                    }
                }
            }
            ulong winnerId = _playersBalance.Max(x => x.Value).Key;
            Interface.Oxide.CallHook("OnJunkyardEventWinner", winnerId);
            foreach (string command in _config.Economy.Commands) Server.Command(command.Replace("{steamid}", $"{winnerId}"));
            _playersBalance.Clear();
        }
        #endregion Economy

        #region Alerts
        [PluginReference] private readonly Plugin GUIAnnouncements, DiscordMessages, Notify;

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
            if (!string.IsNullOrEmpty(_config.Prefix)) message = message.Replace(_config.Prefix + " ", string.Empty);
            return message;
        }

        private bool CanSendDiscordMessage => _config.Discord.IsDiscord && !string.IsNullOrEmpty(_config.Discord.WebhookUrl) && _config.Discord.WebhookUrl != "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks";

        private void AlertToAllPlayers(string langKey, params object[] args)
        {
            if (CanSendDiscordMessage && _config.Discord.Keys.Contains(langKey))
            {
                object fields = new[] { new { name = Title, value = ClearColorAndSize(GetMessage(langKey, null, args)), inline = false } };
                DiscordMessages?.Call("API_SendFancyMessage", _config.Discord.WebhookUrl, "", _config.Discord.EmbedColor, JsonConvert.SerializeObject(fields), null, this);
            }
            foreach (BasePlayer player in BasePlayer.activePlayerList) AlertToPlayer(player, GetMessage(langKey, player.UserIDString, args));
        }

        private void AlertToPlayer(BasePlayer player, string message)
        {
            if (_config.IsChat) PrintToChat(player, message);
            if (_config.GameTip.IsGameTip) player.SendConsoleCommand("gametip.showtoast", _config.GameTip.Style, ClearColorAndSize(message));
            if (_config.GuiAnnouncements.IsGuiAnnouncements) GUIAnnouncements?.Call("CreateAnnouncement", ClearColorAndSize(message), _config.GuiAnnouncements.BannerColor, _config.GuiAnnouncements.TextColor, player, _config.GuiAnnouncements.ApiAdjustVPosition);
            if (_config.Notify.IsNotify && plugins.Exists("Notify")) Notify?.Call("SendNotify", player, _config.Notify.Type, ClearColorAndSize(message));
        }
        #endregion Alerts

        #region GUI
        public class ImageUrl { public string Name; public string Url; }

        private readonly HashSet<ImageUrl> _urls = new HashSet<ImageUrl>
        {
            new ImageUrl { Name = "Tab_KpucTaJl", Url = "Images/Tab_KpucTaJl.png" },
            new ImageUrl { Name = "Clock_KpucTaJl", Url = "Images/Clock_KpucTaJl.png" },
            new ImageUrl { Name = "Npc_KpucTaJl", Url = "Images/Npc_KpucTaJl.png" }
        };

        private readonly HashSet<string> _failedImages = new HashSet<string>();

        private readonly Dictionary<string, string> _images = new Dictionary<string, string>();

        private void DownloadImage()
        {
            ImageUrl image = _urls.FirstOrDefault(x => !_images.ContainsKey(x.Name) && !_failedImages.Contains(x.Name));
            if (image != null)
            {
                Puts($"Downloading image {image.Name}...");
                ServerMgr.Instance.StartCoroutine(ProcessDownloadImage(image));
            }
            else if (_failedImages.Count > 0) Interface.Oxide.UnloadPlugin(Name);
        }

        private IEnumerator ProcessDownloadImage(ImageUrl image)
        {
            string url = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + image.Url;
            using (WWW www = new WWW(url))
            {
                yield return www;
                if (www.error != null)
                {
                    _failedImages.Add(image.Name);
                    PrintError($"Image {image.Name} was not found. Maybe you didn't upload it to the .../oxide/data/Images/ folder");
                }
                else
                {
                    Texture2D tex = www.texture;
                    _images.Add(image.Name, FileStorage.server.Store(tex.EncodeToPNG(), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString());
                    Puts($"Image {image.Name} download is complete");
                    UnityEngine.Object.DestroyImmediate(tex);
                }
                DownloadImage();
            }
        }

        private void CreateTabs(BasePlayer player, Dictionary<string, string> tabs)
        {
            CuiHelper.DestroyUi(player, "Tabs_KpucTaJl");

            CuiElementContainer container = new CuiElementContainer();

            float border = 52.5f + 54.5f * (tabs.Count - 1);
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = $"{-border} {_config.Gui.OffsetMinY}", OffsetMax = $"{border} {_config.Gui.OffsetMinY + 20}" },
                CursorEnabled = false,
            }, "Under", "Tabs_KpucTaJl");

            int i = 0;

            foreach (var dic in tabs)
            {
                i++;
                float xmin = 109f * (i - 1);
                container.Add(new CuiElement
                {
                    Name = $"Tab_{i}_KpucTaJl",
                    Parent = "Tabs_KpucTaJl",
                    Components =
                    {
                        new CuiRawImageComponent { Png = _images["Tab_KpucTaJl"] },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"{xmin} 0", OffsetMax = $"{xmin + 105f} 20" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = $"Tab_{i}_KpucTaJl",
                    Components =
                    {
                        new CuiRawImageComponent { Png = _images[dic.Key] },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "9 3", OffsetMax = "23 17" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = $"Tab_{i}_KpucTaJl",
                    Components =
                    {
                        new CuiTextComponent() { Color = "1 1 1 1", Text = dic.Value, Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-bold.ttf" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "28 0", OffsetMax = "100 20" }
                    }
                });
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion GUI

        #region Helpers
        [PluginReference] private readonly Plugin NpcSpawn, BetterNpc, MonumentOwner;

        private readonly HashSet<string> _hooks = new HashSet<string>
        {
            "OnEntityTakeDamage",
            "CanMountEntity",
            "CanDismountEntity",
            "OnEntitySpawned",
            "OnEntityKill",
            "OnEntityEnter",
            "OnCargoPlaneSignaled",
            "OnSupplyDropDropped",
            "OnVehiclePush",
            "OnPlayerConnected",
            "OnPlayerDeath",
            "OnEntityDeath",
            "CanHackCrate",
            "OnCrateHack",
            "OnLootEntity",
            "OnPlayerCommand",
            "OnServerCommand",
            "OnCorpsePopulate",
            "CanPopulateLoot",
            "OnCustomLootNPC",
            "OnCustomLootContainer",
            "OnContainerPopulate",
            "SetOwnerPveMode",
            "ClearOwnerPveMode",
            "CanEntityTakeDamage",
            "CanTeleport",
            "OnPlayerTeleported"
        };

        private void Unsubscribes() { foreach (string hook in _hooks) Unsubscribe(hook); }

        private void Subscribes() { foreach (string hook in _hooks) Subscribe(hook); }

        private static void CopySerializableFields<T>(T src, T dst)
        {
            FieldInfo[] srcFields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in srcFields)
            {
                object value = field.GetValue(src);
                field.SetValue(dst, value);
            }
        }

        private static string GetTimeFormat(int time)
        {
            if (time <= 60) return $"{time} sec.";
            else
            {
                int sec = time % 60;
                int min = (time - sec) / 60;
                if (sec == 0) return $"{min} min.";
                else return $"{min} min. {sec} sec.";
            }
        }

        private static void UpdateMarkerForPlayer(BasePlayer player, Vector3 pos, PointConfig config)
        {
            if (player == null || player.IsSleeping()) return;
            bool isAdmin = player.IsAdmin;
            if (!isAdmin)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                player.SendNetworkUpdateImmediate();
            }
            try
            {
                player.SendConsoleCommand("ddraw.text", 1f, Color.white, pos, $"<size={config.Size}><color={config.Color}>{config.Text}</color></size>");
            }
            finally
            {
                if (!isAdmin)
                {
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                    player.SendNetworkUpdateImmediate();
                }
            }
        }

        private void CheckVersionPlugin()
        {
            webrequest.Enqueue("http://37.153.157.216:5000/Api/GetPluginVersions?pluginName=JunkyardEvent", null, (code, response) =>
            {
                if (code != 200 || string.IsNullOrEmpty(response)) return;
                string[] array = response.Replace("\"", string.Empty).Split('.');
                VersionNumber latestVersion = new VersionNumber(Convert.ToInt32(array[0]), Convert.ToInt32(array[1]), Convert.ToInt32(array[2]));
                if (Version < latestVersion) PrintWarning($"A new version ({latestVersion}) of the plugin is available! You need to update the plugin:\n- https://lone.design/product/junkyardevent-rust-plugin\n- https://codefling.com/plugins/junkyard-event");
            }, this);
        }
        #endregion Helpers

        #region Commands
        [ChatCommand("jstart")]
        private void ChatStartEvent(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                if (!Active) Start(null);
                else PrintToChat(player, GetMessage("EventActive", player.UserIDString, _config.Prefix));
            }
        }

        [ChatCommand("jstop")]
        private void ChatStopEvent(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                if (Controller != null) Finish();
                else Interface.Oxide.ReloadPlugin(Name);
            }
        }

        [ChatCommand("jpos")]
        private void ChatCommandPos(BasePlayer player)
        {
            if (!player.IsAdmin || Controller == null) return;
            Vector3 pos = Controller.transform.InverseTransformPoint(player.transform.position);
            Puts($"Position: {pos}");
            PrintToChat(player, $"Position: {pos}");
        }

        [ConsoleCommand("jstart")]
        private void ConsoleStartEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null) return;
            if (!Active)
            {
                if (arg.Args == null || arg.Args.Length != 1)
                {
                    Start(null);
                    return;
                }
                ulong steamId = Convert.ToUInt64(arg.Args[0]);
                BasePlayer target = BasePlayer.FindByID(steamId);
                if (target == null)
                {
                    Start(null);
                    Puts($"Player with SteamID {steamId} not found!");
                    return;
                }
                Start(target);
            }
            else Puts("This event is active now. To finish this event (jstop), then to start the next one");
        }

        [ConsoleCommand("jstop")]
        private void ConsoleStopEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
            {
                if (Controller != null) Finish();
                else Interface.Oxide.ReloadPlugin(Name);
            }
        }
        #endregion Commands
    }
}

namespace Oxide.Plugins.JunkyardEventExtensionMethods
{
    public static class ExtensionMethods
    {
        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return true;
            return false;
        }

        public static TSource FirstOrDefault<TSource>(this HashSet<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return enumerator.Current;
            return default(TSource);
        }

        public static HashSet<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> predicate)
        {
            HashSet<TResult> result = new HashSet<TResult>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(predicate(enumerator.Current));
            return result;
        }

        public static TSource Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float> predicate)
        {
            TSource result = source.ElementAt(0);
            float resultValue = predicate(result);
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    TSource element = enumerator.Current;
                    float elementValue = predicate(element);
                    if (elementValue < resultValue)
                    {
                        result = element;
                        resultValue = elementValue;
                    }
                }
            }
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

        private static void Replace<TSource>(this IList<TSource> source, int x, int y)
        {
            TSource t = source[x];
            source[x] = source[y];
            source[y] = t;
        }

        private static List<TSource> QuickSort<TSource>(this List<TSource> source, Func<TSource, float> predicate, int minIndex, int maxIndex)
        {
            if (minIndex >= maxIndex) return source;

            int pivotIndex = minIndex - 1;
            for (int i = minIndex; i < maxIndex; i++)
            {
                if (predicate(source[i]) < predicate(source[maxIndex]))
                {
                    pivotIndex++;
                    source.Replace(pivotIndex, i);
                }
            }
            pivotIndex++;
            source.Replace(pivotIndex, maxIndex);

            QuickSort(source, predicate, minIndex, pivotIndex - 1);
            QuickSort(source, predicate, pivotIndex + 1, maxIndex);

            return source;
        }

        public static List<TSource> OrderByQuickSort<TSource>(this List<TSource> source, Func<TSource, float> predicate) => source.QuickSort(predicate, 0, source.Count - 1);

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