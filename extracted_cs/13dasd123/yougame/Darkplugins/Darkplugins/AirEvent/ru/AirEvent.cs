// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Oxide.Plugins.AirEventExtensionMethods;

namespace Oxide.Plugins
{
    [Info("AirEvent", "Sempai#3239", "2.0.5")]
    internal class AirEvent : RustPlugin
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
			if (_config.PluginVersion < new VersionNumber(2, 0, 4))
            {
                _config.Gui = new GuiConfig
                {
                    IsGui = true,
                    OffsetMinY = "-56"
                };
                foreach (PresetConfig preset in _config.Npc) foreach (NpcBelt belt in preset.Config.BeltItems) belt.Ammo = string.Empty;
            }
            _config.PluginVersion = Version;
            SaveConfig();
            Puts("Config update completed!");
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
            [JsonProperty("OffsetMin Y")] public string OffsetMinY { get; set; }
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
			[JsonProperty(En ? "Ammo" : "Боеприпасы")] public string Ammo { get; set; }
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
            [JsonProperty(En ? "Attack Range Multiplier" : "Множитель радиуса атаки")] public float AttackRangeMultiplier { get; set; }
            [JsonProperty(En ? "Sense Range" : "Радиус обнаружения цели")] public float SenseRange { get; set; }
            [JsonProperty(En ? "Target Memory Duration [sec.]" : "Длительность памяти цели [sec.]")] public float MemoryDuration { get; set; }
            [JsonProperty(En ? "Scale damage" : "Множитель урона")] public float DamageScale { get; set; }
            [JsonProperty(En ? "Aim Cone Scale" : "Множитель разброса")] public float AimConeScale { get; set; }
            [JsonProperty(En ? "Detect the target only in the NPC's viewing vision cone? [true/false]" : "Обнаруживать цель только в углу обзора NPC? [true/false]")] public bool CheckVisionCone { get; set; }
            [JsonProperty(En ? "Vision Cone" : "Угол обзора")] public float VisionCone { get; set; }
            [JsonProperty(En ? "Disable radio effects? [true/false]" : "Отключать эффекты рации? [true/false]")] public bool DisableRadio { get; set; }
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
            [JsonProperty(En ? "List of commands that are executed in the console at the end of the event ({steamid} - the player who collected the highest number of points)" : "Список команд, которые выполняются в консоли по окончанию ивента ({steamid} - игрок, который набрал наибольшее кол-во баллов)")] public HashSet<string> Commands { get; set; }
        }

        private class PluginConfig
        {
            [JsonProperty(En ? "Minimum time between events [sec.]" : "Минимальное время между ивентами [sec.]")] public float MinStartTime { get; set; }
            [JsonProperty(En ? "Maximum time between events [sec.]" : "Максимальное время между ивентами [sec.]")] public float MaxStartTime { get; set; }
            [JsonProperty(En ? "Is active the timer on to start the event? [true/false]" : "Активен ли таймер для запуска ивента? [true/false]")] public bool EnabledTimer { get; set; }
            [JsonProperty(En ? "Duration of the event [sec.]" : "Время проведения ивента [sec.]")] public int FinishTime { get; set; }
            [JsonProperty(En ? "Time before the starting of the event after receiving a chat message [sec.]" : "Время до начала ивента после сообщения в чате [sec.]")] public float PreStartTime { get; set; }
            [JsonProperty(En ? "Time until the end of the event after the last locked crate has been looted [sec.]" : "Время до окончания ивента после того, как последний заблокированный ящик будет украден [sec.]")] public int PreFinishTime { get; set; }
            [JsonProperty(En ? "Time to spawn each object during a airship appears on the map [sec.]" : "Время для спавна каждого объекта при появлении дирижабля на карте [sec.]")] public float Delay { get; set; }
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
            [JsonProperty(En ? "Interrupt the teleport in a airship? (only for users NTeleportation plugin) [true/false]" : "Запрещать телепорт на дирижабле? (только для тех, кто использует плагин NTeleportation) [true/false]")] public bool NTeleportationInterrupt { get; set; }
            [JsonProperty(En ? "NPCs setting" : "Настройка NPC")] public HashSet<PresetConfig> Npc { get; set; }
            [JsonProperty(En ? "Economy setting (total values will be added up and rewarded at the end of the event)" : "Настройка экономики (конечное значение суммируется и будет выдано игрокам по окончанию ивента)")] public EconomyConfig Economy { get; set; }
            [JsonProperty(En ? "The first CCTV camera" : "Название первой камеры")] public string Cctv1 { get; set; }
            [JsonProperty(En ? "The second CCTV camera" : "Название второй камеры")] public string Cctv2 { get; set; }
            [JsonProperty(En ? "Height above the ground for the event appearance" : "Высота над землей для появления ивента")] public float Height { get; set; }
            [JsonProperty(En ? "Do you want to make a smoke screen for the airship appearance? [true/false]" : "Создавать ли дымовую завесу для появления дирижабля? [true/false]")] public bool IsSmoke { get; set; }
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
                    Delay = 0.001f,
                    TypeLootTableCrates = 0,
                    DefaultCrates = new HashSet<CrateConfig>
                    {
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_elite.prefab",
                            Position = "(-7.637, 7.350, 13.646)",
                            Rotation = "(0.121, 152.430, 356.192)",
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
                            Position = "(6.899, 7.279, -14.312)",
                            Rotation = "(359.880, 332.430, 3.808)",
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
                            Position = "(-3.389, 2.982, -3.597)",
                            Rotation = "(356.192, 62.438, 89.879)",
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
                            Position = "(4.698, 3.599, 0.826)",
                            Rotation = "(359.879, 332.430, 93.808)",
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
                            Position = "(0.595, 6.674, 0.731)",
                            Rotation = "(359.880, 332.430, 3.808)",
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
                            Position = "(-2.830, 3.935, 3.819)",
                            Rotation = "(0.121, 152.430, 356.192)",
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
                        UnlockTime = 600f,
                        IncreaseEventTime = true,
                        TypeLootTable = 0,
                        PrefabLootTable = new PrefabLootTableConfig
                        {
                            Min = 1, Max = 1, UseCount = true,
                            Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab" } }
                        },
                        OwnLootTable = new LootTableConfig
                        {
                            Min = 1, Max = 1, UseCount = true,
                            Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinID = 0, Name = "" } }
                        }
                    },
                    Marker = new MarkerConfig
                    {
                        Name = "AirEvent ({time} sec.)",
                        Radius = 0.4f,
                        Alpha = 0.6f,
                        Color = new ColorConfig { R = 0.81f, G = 0.25f, B = 0.15f }
                    },
                    Prefix = "[AirEvent]",
                    IsChat = true,
                    Gui = new GuiConfig
                    {
                        IsGui = true,
                        OffsetMinY = "-56"
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
                            "HackCrate"
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
                    Npc = new HashSet<PresetConfig>
                    {
                        new PresetConfig
                        {
                            Min = 15,
                            Max = 15,
                            Positions = new List<string>
                            {
                                "(9.4, 7.2, -8.5)",
                                "(0.7, 6.6, -13.0)",
                                "(5.2, 6.9, -11.0)",
                                "(7.6, 7.3, -2.0)",
                                "(4.7, 7.3, 3.3)",
                                "(-3.4, 6.5, -7.8)",
                                "(-6.2, 6.5, -2.4)",
                                "(-12.9, 4.5, 11.1)",
                                "(-2.3, 5.3, 16.8)",
                                "(-1.4, 3.5, -10.4)",
                                "(8.7, 4.3, -4.6)",
                                "(-8.9, 3.5, 3.0)",
                                "(2.0, 4.3, 8.6)",
                                "(-8.9, 3.9, 13.1)",
                                "(-6.1, 4.1, 14.5)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "AirEvent",
                                Health = 200f,
                                AttackRangeMultiplier = 1f,
                                SenseRange = 50f,
                                MemoryDuration = 10f,
                                DamageScale = 2f,
                                AimConeScale = 1f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                DisableRadio = true,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "hoodie", SkinID = 1700935391 },
                                    new NpcWear { ShortName = "movembermoustache", SkinID = 0 },
                                    new NpcWear { ShortName = "pants", SkinID = 1700938224 },
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 2575506021 },
                                    new NpcWear { ShortName = "burlap.headwrap", SkinID = 1694253807 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "rifle.lr300", Amount = 1, SkinID = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 5, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
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
                            Min = 1,
                            Max = 1,
                            Positions = new List<string>
                            {
                                "(-4.8, 7.0, 8.2)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "Boss",
                                Health = 500f,
                                AttackRangeMultiplier = 1f,
                                SenseRange = 50f,
                                MemoryDuration = 10f,
                                DamageScale = 2f,
                                AimConeScale = 1f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                DisableRadio = true,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "hoodie", SkinID = 1700935391 },
                                    new NpcWear { ShortName = "movembermoustache", SkinID = 0 },
                                    new NpcWear { ShortName = "pants", SkinID = 1700938224 },
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 2575506021 },
                                    new NpcWear { ShortName = "burlap.headwrap", SkinID = 1694253807 },
                                    new NpcWear { ShortName = "gloweyes", SkinID = 0 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "lmg.m249", Amount = 1, SkinID = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 5, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                                },
                                Kit = ""
                            },
                            TypeLootTable = 5,
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_heavy.prefab" } }
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
                        Commands = new HashSet<string>()
                    },
                    Cctv1 = "AirShipBow",
                    Cctv2 = "AirShipStern",
                    Height = 150f,
                    IsSmoke = false,
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
                ["PreStart"] = "{0} The Airship with scientists is coming to the island!\nIt will arrive in <color=#55aaff>{1} sec.</color>",
                ["Start"] = "{0} The Airship scientists <color=#738d43>have arrived</color>!\nThe Airship is located in grid <color=#55aaff>{1}</color> at a height of <color=#55aaff>{2} m.</color>\nCCTV cameras: <color=#55aaff>{3}</color>, <color=#55aaff>{4}</color>",
                ["PreFinish"] = "{0} The Airship <color=#ce3f27>will self destruct</color> in <color=#55aaff>{1} sec.</color>!",
                ["Finish"] = "{0} The Airship <color=#ce3f27>has self destructed</color>!",
                ["EventActive"] = "{0} This event is active now. To finish this event (<color=#55aaff>/airstop</color>), then (<color=#55aaff>/airstart</color>) to start the next one!",
                ["EnterPVP"] = "{0} You <color=#ce3f27>have entered</color> the PVP zone, now other players <color=#ce3f27>can damage</color> you!",
                ["ExitPVP"] = "{0} You <color=#738d43>have left</color> the PVP zone, now other players <color=#738d43>cannot damage</color> you!",
                ["HackCrate"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>has started</color> hacking a locked crate on The Airship!",
                ["NTeleportation"] = "{0} You <color=#ce3f27>cannot</color> teleport into the event zone!",
                ["SendEconomy"] = "{0} You <color=#738d43>have earned</color> <color=#55aaff>{1}</color> points in economics for participating in the event"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PreStart"] = "{0} Дирижабль с учеными приближается к острову!\nПрибудет через <color=#55aaff>{1} сек.</color>",
                ["Start"] = "{0} Ученые <color=#738d43>прибыли</color>!\nДирижабрь находится в квадрате <color=#55aaff>{1}</color> на высоте <color=#55aaff>{2} м.</color>\nКамеры: <color=#55aaff>{3}</color>, <color=#55aaff>{4}</color>",
                ["PreFinish"] = "{0} Дирижабль будет <color=#ce3f27>уничтожен</color> через <color=#55aaff>{1} сек.</color>!",
                ["Finish"] = "{0} Дирижабль <color=#ce3f27>уничтожен</color>!",
                ["EventActive"] = "{0} Ивент в данный момент активен, сначала завершите текущий ивент (<color=#55aaff>/airstop</color>), чтобы начать следующий!",
                ["EnterPVP"] = "{0} Вы <color=#ce3f27>вошли</color> в PVP зону, теперь другие игроки <color=#ce3f27>могут</color> наносить вам урон!",
                ["ExitPVP"] = "{0} Вы <color=#738d43>вышли</color> из PVP зоны, теперь другие игроки <color=#738d43>не могут</color> наносить вам урон!",
                ["HackCrate"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>начал</color> взлом заблокированного ящика на дирижабле!",
                ["NTeleportation"] = "{0} Вы <color=#ce3f27>не можете</color> телепортироваться в зоне ивента!",
                ["SendEconomy"] = "{0} Вы <color=#738d43>получили</color> <color=#55aaff>{1}</color> баллов в экономику за прохождение ивента"
            }, this, "ru");
        }

        private string GetMessage(string langKey, string userID) => lang.GetMessage(langKey, _ins, userID);

        private string GetMessage(string langKey, string userID, params object[] args) => (args.Length == 0) ? GetMessage(langKey, userID) : string.Format(GetMessage(langKey, userID), args);
        #endregion Lang

        #region Oxide Hooks
        private static AirEvent _ins;

        private void Init()
        {
            _ins = this;
            Unsubscribes();
        }

        private void OnServerInitialized()
        {
            LoadDefaultMessages();
            CheckAllLootTables();
			DownloadImage();
            if (_config.EnabledTimer)
            {
                timer.In(UnityEngine.Random.Range(_config.MinStartTime, _config.MaxStartTime), () =>
                {
                    if (!_active) Start();
                    else Puts("This event is active now. To finish this event (airstop), then to start the next one");
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
            if (_controller.Entities.Contains(entity) && !entity.ShortPrefabName.Contains("door_barricade_b")) return true;
            HotAirBalloon attackerBalloon = info.Initiator as HotAirBalloon;
            if (attackerBalloon != null && _controller.AirBalloons.Contains(attackerBalloon) && (entity as BasePlayer).IsPlayer()) return true;
            return null;
        }

        private object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            if (planner == null) return null;
            BasePlayer player = planner.GetOwnerPlayer();
            if (player != null && _controller.Players.Contains(player)) return false;
            return null;
        }

        private object CanAffordUpgrade(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade)
        {
            if (block != null && _controller.Entities.Contains(block)) return false;
            else return null;
        }

        private object OnStructureRotate(BaseCombatEntity entity, BasePlayer player)
        {
            if (entity != null && _controller.Entities.Contains(entity)) return true;
            else return null;
        }

        private void OnEntitySpawned(DroppedItemContainer container) { if (container != null && Vector3.Distance(_controller.transform.position, container.transform.position) < Radius) _controller.Backpacks.Add(container); }

        private void OnEntitySpawned(SimpleShark shark) { if (shark.IsExists() && Vector2.Distance(new Vector2(_controller.transform.position.x, _controller.transform.position.z), new Vector2(shark.transform.position.x, shark.transform.position.z)) < Radius) shark.Kill(); }

        private object OnEntityKill(BaseEntity entity)
        {
            if (entity == null) return null;
            if (entity is DroppedItemContainer && _controller.Backpacks.Contains(entity as DroppedItemContainer))
            {
                _controller.Backpacks.Remove(entity as DroppedItemContainer);
                return null;
            }
            if (entity is HackableLockedCrate && _controller.HackCrates.Contains(entity as HackableLockedCrate))
            {
                _controller.HackCrates.Remove(entity as HackableLockedCrate);
                return null;
            }
            if (entity is LootContainer && _controller.Crates.Contains(entity as LootContainer))
            {
                _controller.Crates.Remove(entity as LootContainer);
                return null;
            }
            if (_controller.Entities.Contains(entity))
            {
                if (entity.ShortPrefabName.Contains("door_barricade_b")) return null;
                if (!_controller.KillEntities) return true;
            }
            return null;
        }

        private void OnEntityDeath(ScientistNPC npc, HitInfo info)
        {
            if (npc == null || info == null) return;
            BasePlayer attacker = info.InitiatorPlayer;
            if (_controller.Scientists.Contains(npc) && attacker.IsPlayer()) ActionEconomy(attacker.userID, "Npc");
        }

        private readonly Dictionary<uint, BasePlayer> _startHackCrates = new Dictionary<uint, BasePlayer>();

        private void CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (player == null || crate == null) return;
            if (_controller.HackCrates.Contains(crate))
            {
                if (_startHackCrates.ContainsKey(crate.net.ID)) _startHackCrates[crate.net.ID] = player;
                else _startHackCrates.Add(crate.net.ID, player);
            }
        }

        private void OnCrateHack(HackableLockedCrate crate)
        {
            if (crate == null) return;
            uint crateId = crate.net.ID;
            BasePlayer player;
            if (_startHackCrates.TryGetValue(crateId, out player))
            {
                _startHackCrates.Remove(crateId);
                if (_config.HackCrate.IncreaseEventTime && _controller.TimeToFinish < (int)_config.HackCrate.UnlockTime) _controller.TimeToFinish += (int)_config.HackCrate.UnlockTime;
                ActionEconomy(player.userID, "LockedCrate");
                AlertToAllPlayers("HackCrate", _config.Prefix, player.displayName);
            }
        }

        private object OnSamSiteTarget(SamSite entity, HotAirBalloon target)
        {
            if (entity == null || target == null) return null;
            if (_controller.AirBalloons.Contains(target)) return false;
            else return null;
        }

        private readonly HashSet<uint> _lootableCrates = new HashSet<uint>();

        private void OnLootEntity(BasePlayer player, LootContainer container)
        {
            if (!player.IsPlayer() || !container.IsExists() || _lootableCrates.Contains(container.net.ID)) return;
            if (_controller.Crates.Contains(container))
            {
                _lootableCrates.Add(container.net.ID);
                ActionEconomy(player.userID, "Crates", container.ShortPrefabName);
            }
        }
        #endregion Oxide Hooks

        #region Controller
        private ControllerAirEvent _controller;
        private bool _active = false;
        internal Vector3 SpawnPos;

        private void Start()
        {
            if (!plugins.Exists("NpcSpawn"))
            {
                PrintError("NpcSpawn plugin doesn`t exist! Please read the file ReadMe.txt");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            _active = true;
            AlertToAllPlayers("PreStart", _config.Prefix, _config.PreStartTime);
            timer.In(_config.PreStartTime - 10f <= 0f ? 0f : _config.PreStartTime - 10f, () =>
            {
                SpawnPos = new Vector3(UnityEngine.Random.Range(-World.Size / 4f, World.Size / 4f), _config.Height + 10f, UnityEngine.Random.Range(-World.Size / 4f, World.Size / 4f));
                if (TerrainMeta.HeightMap.GetHeight(SpawnPos) > 0f) SpawnPos.y += TerrainMeta.HeightMap.GetHeight(SpawnPos);
                if (_config.IsSmoke)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        SmokeGrenade grenade = GameManager.server.CreateEntity("assets/prefabs/tools/smoke grenade/grenade.smoke.deployed.prefab", SpawnPos + UnityEngine.Random.insideUnitSphere * 25f) as SmokeGrenade;
                        grenade.enableSaving = false;
                        grenade.Spawn();
                        grenade.GetComponent<Rigidbody>().useGravity = false;
                    }
                }
                timer.In(10f, () =>
                {
                    SpawnPos.y -= 10f;
                    Subscribes();
                    _controller = new GameObject().AddComponent<ControllerAirEvent>();
                    AlertToAllPlayers("Start", _config.Prefix, PhoneController.PositionToGridCoord(SpawnPos), (int)SpawnPos.y, _config.Cctv1, _config.Cctv2);
                });
            });
        }

        private void Finish()
        {
            Unsubscribes();
            if (_config.PveMode.Pve && plugins.Exists("PveMode")) PveMode.Call("EventRemovePveMode", Name, true);
            if (_controller != null) UnityEngine.Object.Destroy(_controller.gameObject);
            _active = false;
			SpawnPos = Vector3.zero;
            SendBalance();
            AlertToAllPlayers("Finish", _config.Prefix);
            Interface.Oxide.CallHook("OnAirEventEnd");
            if (_config.EnabledTimer)
            {
                timer.In(UnityEngine.Random.Range(_config.MinStartTime, _config.MaxStartTime), () =>
                {
                    if (!_active) Start();
                    else Puts("This event is active now. To finish this event (airstop), then to start the next one");
                });
            }
        }

        internal class Prefab { public string prefab; public Vector3 pos; public Vector3 rot; }
        internal HashSet<Prefab> Prefabs = new HashSet<Prefab>
        {
            //floor
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(6.422f, 1.005f, -5.951f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(3.769f, 0.805f, -7.336f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(1.115f, 0.606f, -8.721f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-0.273f, 0.612f, -6.062f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(2.380f, 0.812f, -4.677f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(5.034f, 1.011f, -3.292f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(3.645f, 1.017f, -0.633f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(0.992f, 0.818f, -2.018f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-1.662f, 0.619f, -3.403f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-3.050f, 0.625f, -0.743f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-0.397f, 0.824f, 0.642f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(2.257f, 1.024f, 2.027f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(0.868f, 1.030f, 4.686f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-1.785f, 0.831f, 3.301f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-4.439f, 0.631f, 1.916f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-5.827f, 0.638f, 4.575f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-3.174f, 0.837f, 5.960f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-0.520f, 1.036f, 7.345f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-1.909f, 1.042f, 10.005f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-4.562f, 0.843f, 8.620f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-7.216f, 0.644f, 7.235f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-11.258f, 0.451f, 8.509f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-8.604f, 0.650f, 9.894f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-5.951f, 0.850f, 11.279f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-3.297f, 1.049f, 12.664f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-0.643f, 1.248f, 14.049f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-2.032f, 1.254f, 16.708f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-4.686f, 1.055f, 15.323f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-7.339f, 0.856f, 13.938f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-9.993f, 0.657f, 12.553f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-12.647f, 0.457f, 11.168f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(8.902f, 4.197f, -4.664f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(6.249f, 3.998f, -6.049f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(3.595f, 3.799f, -7.434f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(0.941f, 3.600f, -8.819f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-1.712f, 3.400f, -10.204f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-3.101f, 3.407f, -7.545f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-0.447f, 3.606f, -6.160f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(2.207f, 3.805f, -4.775f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(4.860f, 4.004f, -3.390f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(7.514f, 4.204f, -2.005f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(6.125f, 4.210f, 0.655f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(3.472f, 4.011f, -0.730f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(0.818f, 3.811f, -2.115f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-1.836f, 3.612f, -3.501f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-4.489f, 3.413f, -4.886f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-5.878f, 3.419f, -2.226f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-3.224f, 3.618f, -0.841f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-0.570f, 3.818f, 0.544f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(2.083f, 4.017f, 1.929f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(4.737f, 4.216f, 3.314f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(3.348f, 4.222f, 5.973f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(0.695f, 4.023f, 4.588f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-1.959f, 3.824f, 3.203f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-4.613f, 3.625f, 1.818f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-7.266f, 3.426f, 0.433f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-8.655f, 3.432f, 3.092f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-6.001f, 3.631f, 4.477f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-3.347f, 3.830f, 5.863f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-0.694f, 4.030f, 7.248f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(1.960f, 4.229f, 8.633f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(0.519f, 5.137f, 11.263f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-2.082f, 4.036f, 9.907f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-4.736f, 3.837f, 8.522f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-7.390f, 3.637f, 7.137f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-10.096f, 4.340f, 5.722f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-11.484f, 4.344f, 8.382f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-8.778f, 3.644f, 9.796f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-6.124f, 3.843f, 11.181f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-3.471f, 4.042f, 12.566f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-0.869f, 5.141f, 13.922f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-2.258f, 5.148f, 16.581f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-4.859f, 4.049f, 15.226f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-7.513f, 3.849f, 13.841f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-10.167f, 3.650f, 12.455f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-12.872f, 4.351f, 11.041f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(1.835f, 6.381f, 8.562f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(3.175f, 7.209f, 5.876f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(4.564f, 7.202f, 3.216f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(5.952f, 7.196f, 0.556f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(7.341f, 7.190f, -2.103f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(8.729f, 7.191f, -4.762f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-8.780f, 5.584f, 3.021f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-7.440f, 6.412f, 0.334f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-6.051f, 6.405f, -2.325f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-4.663f, 6.399f, -4.984f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-3.275f, 6.400f, -7.644f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-1.886f, 6.394f, -10.303f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-6.175f, 6.624f, 4.380f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-3.520f, 6.816f, 5.765f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-0.867f, 7.023f, 7.150f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-2.255f, 7.022f, 9.809f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-4.909f, 6.823f, 8.424f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-7.563f, 6.631f, 7.039f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-8.952f, 6.637f, 9.698f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-6.298f, 6.836f, 11.083f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-3.644f, 7.028f, 12.468f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-10.393f, 7.549f, 12.328f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-10.340f, 6.636f, 12.358f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-5.032f, 7.035f, 15.128f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-5.085f, 7.948f, 15.098f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor/floor.prefab", pos = new Vector3(-7.686f, 6.835f, 13.743f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            //floor.triangle
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(10.685f, 7.227f, -6.901f), rot = new Vector3(3.237f, 272.491f, 2.011f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(8.032f, 7.028f, -8.285f), rot = new Vector3(356.763f, 92.491f, 357.989f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(8.032f, 7.028f, -8.285f), rot = new Vector3(3.237f, 272.491f, 2.011f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(5.379f, 6.829f, -9.669f), rot = new Vector3(356.763f, 92.491f, 357.989f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(5.379f, 6.829f, -9.669f), rot = new Vector3(3.237f, 272.491f, 2.011f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(2.726f, 6.630f, -11.053f), rot = new Vector3(356.763f, 92.491f, 357.989f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(2.726f, 6.630f, -11.053f), rot = new Vector3(3.237f, 272.491f, 2.011f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(1.398f, 6.530f, -11.745f), rot = new Vector3(3.358f, 212.381f, 358.199f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(0.073f, 6.430f, -12.436f), rot = new Vector3(3.237f, 272.491f, 2.011f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(2.605f, 6.525f, -14.049f), rot = new Vector3(3.237f, 272.491f, 2.011f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(3.932f, 6.624f, -13.355f), rot = new Vector3(3.358f, 212.381f, 358.199f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(5.259f, 6.724f, -12.664f), rot = new Vector3(3.237f, 272.491f, 2.011f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(6.586f, 6.824f, -11.971f), rot = new Vector3(3.358f, 212.381f, 358.199f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(7.912f, 6.923f, -11.279f), rot = new Vector3(3.237f, 272.491f, 2.011f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(9.240f, 7.023f, -10.586f), rot = new Vector3(3.358f, 212.381f, 358.199f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(10.564f, 7.122f, -9.897f), rot = new Vector3(3.237f, 272.491f, 2.011f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(9.114f, 6.918f, -13.583f), rot = new Vector3(356.642f, 32.381f, 1.802f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(9.053f, 6.865f, -15.081f), rot = new Vector3(359.879f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(6.523f, 6.771f, -13.469f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(6.399f, 6.666f, -16.466f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(3.807f, 6.526f, -16.352f), rot = new Vector3(356.642f, 32.381f, 1.802f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(10.862f, 4.241f, -6.800f), rot = new Vector3(3.237f, 272.491f, 2.011f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(8.208f, 4.042f, -8.185f), rot = new Vector3(356.763f, 92.491f, 357.989f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(8.208f, 4.042f, -8.185f), rot = new Vector3(3.237f, 272.491f, 2.011f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(6.880f, 3.942f, -8.877f), rot = new Vector3(3.358f, 212.381f, 358.198f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(5.553f, 3.843f, -9.571f), rot = new Vector3(3.237f, 272.491f, 2.011f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(2.901f, 3.643f, -10.955f), rot = new Vector3(356.763f, 92.491f, 357.989f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(2.901f, 3.643f, -10.955f), rot = new Vector3(3.237f, 272.491f, 2.011f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(0.248f, 3.437f, -12.340f), rot = new Vector3(356.763f, 92.491f, 357.989f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(0.248f, 3.438f, -12.340f), rot = new Vector3(3.237f, 272.491f, 2.011f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(2.779f, 3.532f, -13.951f), rot = new Vector3(3.237f, 272.491f, 2.011f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(4.106f, 3.638f, -13.257f), rot = new Vector3(3.358f, 212.381f, 358.199f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(4.106f, 3.638f, -13.257f), rot = new Vector3(356.643f, 32.381f, 1.802f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(5.433f, 3.738f, -12.564f), rot = new Vector3(356.763f, 92.491f, 357.989f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(8.086f, 3.937f, -11.181f), rot = new Vector3(3.237f, 272.491f, 2.011f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(9.413f, 4.037f, -10.487f), rot = new Vector3(3.358f, 212.381f, 358.199f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(10.740f, 4.130f, -9.796f), rot = new Vector3(3.237f, 272.491f, 2.011f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(9.288f, 3.925f, -13.484f), rot = new Vector3(356.642f, 32.381f, 1.802f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(9.226f, 3.872f, -14.983f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(6.634f, 3.732f, -14.869f), rot = new Vector3(356.642f, 32.381f, 1.802f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(6.573f, 3.673f, -16.368f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(3.981f, 3.526f, -16.255f), rot = new Vector3(356.643f, 32.381f, 1.802f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(1.809f, 0.603f, -10.051f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(4.401f, 0.750f, -10.165f), rot = new Vector3(3.358f, 212.381f, 178.199f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(4.463f, 0.802f, -8.666f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(5.728f, 0.849f, -9.472f), rot = new Vector3(356.763f, 92.491f, 177.990f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(7.117f, 1.001f, -7.281f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(6.994f, 0.896f, -10.277f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(6.932f, 0.844f, -11.776f), rot = new Vector3(3.358f, 212.381f, 178.199f) },
            new Prefab { prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", pos = new Vector3(4.339f, 0.697f, -11.663f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            //wall.low
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(3.113f, 7.322f, 9.227f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(4.675f, 4.322f, 6.666f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(5.890f, 7.309f, 3.909f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(7.452f, 4.309f, 1.347f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(8.667f, 7.297f, -1.410f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(10.230f, 4.290f, -3.972f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(10.688f, 7.235f, -6.898f), rot = new Vector3(357.993f, 2.377f, 3.239f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(10.739f, 4.136f, -9.795f), rot = new Vector3(357.993f, 2.377f, 3.239f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(10.566f, 7.130f, -9.893f), rot = new Vector3(357.993f, 2.377f, 3.239f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(1.277f, 6.432f, -14.741f), rot = new Vector3(358.202f, 122.487f, 356.641f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-1.254f, 6.338f, -13.130f), rot = new Vector3(358.202f, 122.487f, 356.641f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(1.451f, 3.438f, -14.643f), rot = new Vector3(358.202f, 122.487f, 356.641f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-3.039f, 3.301f, -10.897f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-4.601f, 6.300f, -8.335f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-5.816f, 3.313f, -5.578f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-7.378f, 6.313f, -3.017f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-8.593f, 3.326f, -0.259f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-10.155f, 6.326f, 2.302f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-9.371f, 3.435f, 4.464f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-8.755f, 3.535f, 6.424f), rot = new Vector3(359.879f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-10.140f, 3.541f, 9.085f), rot = new Vector3(359.879f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-11.528f, 3.548f, 11.745f), rot = new Vector3(359.879f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(1.244f, 4.225f, 10.005f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-0.711f, 4.139f, 10.622f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-2.101f, 4.145f, 13.281f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-3.492f, 4.151f, 15.939f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-3.705f, 7.134f, 15.820f), rot = new Vector3(359.879f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-4.338f, 7.031f, 13.798f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-5.727f, 7.038f, 16.457f), rot = new Vector3(3.808f, 242.438f, 0.121f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-9.646f, 6.640f, 11.028f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-11.666f, 6.537f, 11.665f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-11.034f, 6.639f, 13.687f), rot = new Vector3(3.808f, 242.438f, 0.121f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-9.013f, 6.736f, 13.050f), rot = new Vector3(359.879f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.low/wall.low.prefab", pos = new Vector3(-6.359f, 6.935f, 14.435f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            //wall.half
            new Prefab { prefab = "assets/prefabs/building core/wall.half/wall.half.prefab", pos = new Vector3(-11.370f, 3.339f, 5.059f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.half/wall.half.prefab", pos = new Vector3(-12.759f, 3.345f, 7.719f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.half/wall.half.prefab", pos = new Vector3(-14.147f, 3.351f, 10.378f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.half/wall.half.prefab", pos = new Vector3(-13.514f, 3.454f, 12.400f), rot = new Vector3(3.808f, 242.438f, 0.121f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.half/wall.half.prefab", pos = new Vector3(-2.900f, 4.251f, 17.940f), rot = new Vector3(3.808f, 242.438f, 0.121f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.half/wall.half.prefab", pos = new Vector3(-0.879f, 4.347f, 17.303f), rot = new Vector3(359.879f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.half/wall.half.prefab", pos = new Vector3(0.510f, 4.341f, 14.644f), rot = new Vector3(359.879f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.half/wall.half.prefab", pos = new Vector3(1.898f, 4.335f, 11.985f), rot = new Vector3(359.879f, 332.430f, 3.808f) },
            //wall
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(-10.564f, 0.448f, 7.179f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(-12.585f, 0.351f, 7.816f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(-13.973f, 0.358f, 10.476f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(-13.341f, 0.461f, 12.498f), rot = new Vector3(3.808f, 242.438f, 0.121f) },
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(-10.687f, 0.660f, 13.883f), rot = new Vector3(3.808f, 242.438f, 0.121f) },
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(-8.033f, 0.859f, 15.268f), rot = new Vector3(3.808f, 242.438f, 0.121f) },
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(-5.380f, 1.058f, 16.653f), rot = new Vector3(3.808f, 242.438f, 0.121f) },
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(-2.726f, 1.258f, 18.038f), rot = new Vector3(3.808f, 242.438f, 0.121f) },
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(-5.553f, 4.052f, 16.555f), rot = new Vector3(3.808f, 242.438f, 0.121f) },
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(-8.207f, 3.852f, 15.170f), rot = new Vector3(3.808f, 242.438f, 0.121f) },
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(-10.861f, 3.653f, 13.785f), rot = new Vector3(3.808f, 242.438f, 0.121f) },
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(-0.705f, 1.354f, 17.401f), rot = new Vector3(359.879f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(0.683f, 1.348f, 14.742f), rot = new Vector3(359.879f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(0.051f, 1.245f, 12.719f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(10.442f, 7.018f, -12.889f), rot = new Vector3(357.993f, 2.377f, 3.239f) },
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(9.053f, 6.865f, -15.081f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(6.399f, 6.666f, -16.466f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/building core/wall/wall.prefab", pos = new Vector3(3.807f, 6.526f, -16.352f), rot = new Vector3(358.202f, 122.487f, 356.641f) },
            //wall.window
            new Prefab { prefab = "assets/prefabs/building core/wall.window/wall.window.prefab", pos = new Vector3(9.226f, 3.872f, -14.983f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.window/wall.window.prefab", pos = new Vector3(6.573f, 3.673f, -16.368f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            //shutter.metal.embrasure.a
            new Prefab { prefab = "assets/prefabs/building/wall.window.embrasure/shutter.metal.embrasure.a.prefab", pos = new Vector3(9.166f, 4.906f, -15.017f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/building/wall.window.embrasure/shutter.metal.embrasure.a.prefab", pos = new Vector3(6.513f, 4.706f, -16.402f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            //wall.frame
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(3.287f, 4.328f, 9.325f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(6.064f, 4.316f, 4.006f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(8.841f, 4.303f, -1.312f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(0.758f, 3.387f, 8.007f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(10.862f, 4.241f, -6.800f), rot = new Vector3(357.993f, 2.377f, 3.239f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(10.739f, 4.136f, -9.795f), rot = new Vector3(357.993f, 2.377f, 3.239f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(10.615f, 4.030f, -12.791f), rot = new Vector3(357.993f, 2.377f, 3.239f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(3.980f, 3.532f, -16.254f), rot = new Vector3(358.202f, 122.487f, 356.641f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(1.451f, 3.438f, -14.643f), rot = new Vector3(358.202f, 122.487f, 356.641f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(-1.081f, 3.344f, -13.032f), rot = new Vector3(358.202f, 122.487f, 356.641f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(-4.428f, 3.307f, -8.237f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(-7.205f, 3.320f, -2.919f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(-9.982f, 3.332f, 2.400f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(-7.289f, 2.782f, 3.807f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(0.001f, 4.019f, 5.918f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(-2.653f, 3.827f, 4.533f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(-5.307f, 3.628f, 3.148f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(6.942f, 3.995f, -7.380f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(1.636f, 3.596f, -10.149f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(8.775f, 6.972f, -11.383f), rot = new Vector3(356.682f, 90.087f, 358.127f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(5.802f, 6.837f, -12.096f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(3.518f, 6.575f, -14.107f), rot = new Vector3(356.574f, 34.639f, 1.668f) },
            //wall.frame.netting
            new Prefab { prefab = "assets/prefabs/building/wall.frame.netting/wall.frame.netting.prefab", pos = new Vector3(6.942f, 3.995f, -7.380f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/building/wall.frame.netting/wall.frame.netting.prefab", pos = new Vector3(1.636f, 3.596f, -10.149f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            //wall.frame.fence
            new Prefab { prefab = "assets/prefabs/building/wall.frame.fence/wall.frame.fence.prefab", pos = new Vector3(8.775f, 6.972f, -11.383f), rot = new Vector3(356.682f, 90.087f, 358.127f) },
            new Prefab { prefab = "assets/prefabs/building/wall.frame.fence/wall.frame.fence.prefab", pos = new Vector3(3.518f, 6.575f, -14.107f), rot = new Vector3(356.574f, 34.639f, 1.668f) },
            //wall.frame.fence.gate
            new Prefab { prefab = "assets/prefabs/building/wall.frame.fence/wall.frame.fence.gate.prefab", pos = new Vector3(5.802f, 6.837f, -12.096f), rot = new Vector3(3.808f, 242.438f, 0.121f) },
            //barricade.concrete
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", pos = new Vector3(3.449f, 7.171f, 2.634f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", pos = new Vector3(6.226f, 7.158f, -2.684f), rot = new Vector3(356.192f, 62.438f, 359.879f) },
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", pos = new Vector3(-4.942f, 6.541f, -1.745f), rot = new Vector3(3.808f, 242.438f, 0.121f) },
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", pos = new Vector3(-2.166f, 6.547f, -7.064f), rot = new Vector3(3.808f, 242.438f, 0.121f) },
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", pos = new Vector3(1.395f, 6.594f, -11.747f), rot = new Vector3(3.358f, 212.381f, 358.199f) },
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", pos = new Vector3(1.569f, 3.587f, -11.649f), rot = new Vector3(3.358f, 212.381f, 358.199f) },
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", pos = new Vector3(8.028f, 7.092f, -8.287f), rot = new Vector3(356.763f, 92.491f, 357.989f) },
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", pos = new Vector3(8.203f, 4.085f, -8.189f), rot = new Vector3(356.763f, 92.491f, 357.989f) },
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", pos = new Vector3(6.694f, 3.829f, -13.373f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", pos = new Vector3(4.227f, 6.845f, -9.091f), rot = new Vector3(359.879f, 332.430f, 3.808f) },
            //barricade.stone
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.stone.prefab", pos = new Vector3(9.691f, 7.018f, -13.690f), rot = new Vector3(357.993f, 2.377f, 3.239f) },
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.stone.prefab", pos = new Vector3(9.759f, 7.075f, -12.068f), rot = new Vector3(357.993f, 2.377f, 3.239f) },
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.stone.prefab", pos = new Vector3(8.527f, 7.005f, -12.015f), rot = new Vector3(357.993f, 2.377f, 3.239f) },
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.stone.prefab", pos = new Vector3(4.891f, 6.664f, -16.200f), rot = new Vector3(356.642f, 32.381f, 1.802f) },
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.stone.prefab", pos = new Vector3(3.517f, 6.613f, -15.328f), rot = new Vector3(356.642f, 32.381f, 1.802f) },
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.stone.prefab", pos = new Vector3(4.180f, 6.685f, -14.282f), rot = new Vector3(356.642f, 32.381f, 1.802f) },
            //electric.flasherlight.deployed
            new Prefab { prefab = "assets/prefabs/deployable/playerioents/lights/flasherlight/electric.flasherlight.deployed.prefab", pos = new Vector3(-3.644f, 7.036f, 12.468f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/deployable/playerioents/lights/flasherlight/electric.flasherlight.deployed.prefab", pos = new Vector3(-6.298f, 6.836f, 11.083f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/deployable/playerioents/lights/flasherlight/electric.flasherlight.deployed.prefab", pos = new Vector3(-8.952f, 6.637f, 9.698f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/deployable/playerioents/lights/flasherlight/electric.flasherlight.deployed.prefab", pos = new Vector3(-4.910f, 6.830f, 8.424f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/deployable/playerioents/lights/flasherlight/electric.flasherlight.deployed.prefab", pos = new Vector3(-0.867f, 7.023f, 7.150f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/deployable/playerioents/lights/flasherlight/electric.flasherlight.deployed.prefab", pos = new Vector3(-3.521f, 6.824f, 5.765f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/deployable/playerioents/lights/flasherlight/electric.flasherlight.deployed.prefab", pos = new Vector3(-6.175f, 6.624f, 4.380f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            //furnace.large
            new Prefab { prefab = "assets/prefabs/deployable/furnace.large/furnace.large.prefab", pos = new Vector3(-2.380f, 3.041f, 16.212f), rot = new Vector3(3.808f, 242.438f, 270.121f) },
            new Prefab { prefab = "assets/prefabs/deployable/furnace.large/furnace.large.prefab", pos = new Vector3(-12.299f, 2.296f, 11.035f), rot = new Vector3(3.808f, 242.438f, 270.121f) },
            //sign.pole.banner.large
            new Prefab { prefab = "assets/prefabs/deployable/signs/sign.pole.banner.large.prefab", pos = new Vector3(0.708f, 7.990f, -2.510f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            //sign.pictureframe.tall
            new Prefab { prefab = "assets/prefabs/deployable/signs/sign.pictureframe.tall.prefab", pos = new Vector3(8.591f, 2.373f, -13.985f), rot = new Vector3(320.154f, 327.277f, 95.161f) },
            //roof
            new Prefab { prefab = "assets/prefabs/building core/roof/roof.prefab", pos = new Vector3(-8.778f, 3.644f, 9.796f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/roof/roof.prefab", pos = new Vector3(-6.124f, 3.843f, 11.181f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/roof/roof.prefab", pos = new Vector3(-3.471f, 4.042f, 12.566f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            new Prefab { prefab = "assets/prefabs/building core/roof/roof.prefab", pos = new Vector3(-10.043f, 3.438f, 5.752f), rot = new Vector3(3.808f, 242.438f, 180.121f) },
            new Prefab { prefab = "assets/prefabs/building core/roof/roof.prefab", pos = new Vector3(-8.655f, 3.432f, 3.092f), rot = new Vector3(3.808f, 242.438f, 180.121f) },
            new Prefab { prefab = "assets/prefabs/building core/roof/roof.prefab", pos = new Vector3(-7.266f, 3.426f, 0.433f), rot = new Vector3(3.808f, 242.438f, 180.121f) },
            new Prefab { prefab = "assets/prefabs/building core/roof/roof.prefab", pos = new Vector3(-5.878f, 3.419f, -2.226f), rot = new Vector3(3.808f, 242.438f, 180.121f) },
            new Prefab { prefab = "assets/prefabs/building core/roof/roof.prefab", pos = new Vector3(-4.489f, 3.413f, -4.886f), rot = new Vector3(3.808f, 242.438f, 180.121f) },
            new Prefab { prefab = "assets/prefabs/building core/roof/roof.prefab", pos = new Vector3(-3.101f, 3.407f, -7.545f), rot = new Vector3(3.808f, 242.438f, 180.121f) },
            new Prefab { prefab = "assets/prefabs/building core/roof/roof.prefab", pos = new Vector3(-1.712f, 3.400f, -10.204f), rot = new Vector3(3.808f, 242.438f, 180.121f) },
            new Prefab { prefab = "assets/prefabs/building core/roof/roof.prefab", pos = new Vector3(0.571f, 4.235f, 11.292f), rot = new Vector3(356.192f, 62.438f, 179.879f) },
            new Prefab { prefab = "assets/prefabs/building core/roof/roof.prefab", pos = new Vector3(1.960f, 4.229f, 8.633f), rot = new Vector3(356.192f, 62.438f, 179.879f) },
            new Prefab { prefab = "assets/prefabs/building core/roof/roof.prefab", pos = new Vector3(3.348f, 4.222f, 5.973f), rot = new Vector3(356.192f, 62.438f, 179.879f) },
            new Prefab { prefab = "assets/prefabs/building core/roof/roof.prefab", pos = new Vector3(4.737f, 4.216f, 3.314f), rot = new Vector3(356.192f, 62.438f, 179.879f) },
            new Prefab { prefab = "assets/prefabs/building core/roof/roof.prefab", pos = new Vector3(6.125f, 4.210f, 0.655f), rot = new Vector3(356.192f, 62.438f, 179.879f) },
            new Prefab { prefab = "assets/prefabs/building core/roof/roof.prefab", pos = new Vector3(7.514f, 4.204f, -2.005f), rot = new Vector3(356.192f, 62.438f, 179.879f) },
            new Prefab { prefab = "assets/prefabs/building core/roof/roof.prefab", pos = new Vector3(8.902f, 4.197f, -4.664f), rot = new Vector3(356.192f, 62.438f, 179.879f) },
            //hotairballoon
            new Prefab { prefab = "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab", pos = new Vector3(6.952f, 6.863f, -14.305f), rot = new Vector3(359.880f, 332.430f, 3.808f) },
            new Prefab { prefab = "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab", pos = new Vector3(-7.691f, 6.930f, 13.740f), rot = new Vector3(0.121f, 152.430f, 356.192f) },
            //roof.triangle
            new Prefab { prefab = "assets/prefabs/building core/roof.triangle/roof.triangle.prefab", pos = new Vector3(-0.558f, 3.402f, -12.210f), rot = new Vector3(3.358f, 212.381f, 178.199f) },
            new Prefab { prefab = "assets/prefabs/building core/roof.triangle/roof.triangle.prefab", pos = new Vector3(1.953f, 3.495f, -13.828f), rot = new Vector3(3.358f, 212.381f, 178.199f) },
            new Prefab { prefab = "assets/prefabs/building core/roof.triangle/roof.triangle.prefab", pos = new Vector3(4.469f, 3.588f, -15.430f), rot = new Vector3(3.358f, 212.381f, 178.199f) },
            new Prefab { prefab = "assets/prefabs/building core/roof.triangle/roof.triangle.prefab", pos = new Vector3(6.089f, 3.682f, -15.441f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/roof.triangle/roof.triangle.prefab", pos = new Vector3(8.739f, 3.881f, -14.058f), rot = new Vector3(0.121f, 152.430f, 176.192f) },
            new Prefab { prefab = "assets/prefabs/building core/roof.triangle/roof.triangle.prefab", pos = new Vector3(9.623f, 3.975f, -12.732f), rot = new Vector3(356.763f, 92.491f, 177.990f) },
            new Prefab { prefab = "assets/prefabs/building core/roof.triangle/roof.triangle.prefab", pos = new Vector3(9.763f, 4.081f, -9.749f), rot = new Vector3(356.763f, 92.491f, 177.990f) },
            new Prefab { prefab = "assets/prefabs/building core/roof.triangle/roof.triangle.prefab", pos = new Vector3(9.886f, 4.186f, -6.769f), rot = new Vector3(356.763f, 92.491f, 177.990f) },
            //door_barricade_b
            new Prefab { prefab = "assets/prefabs/deployable/door barricades/door_barricade_b.prefab", pos = new Vector3(-4.143f, 3.622f, -2.325f), rot = new Vector3(273.810f, 240.619f, 91.815f) },
            new Prefab { prefab = "assets/prefabs/deployable/door barricades/door_barricade_b.prefab", pos = new Vector3(3.862f, 4.228f, 1.741f), rot = new Vector3(273.810f, 240.619f, 91.815f) },
            //cctv_deployed
            new Prefab { prefab = "assets/prefabs/deployable/cctvcamera/cctv_deployed.prefab", pos = new Vector3(7.688f, 6.701f, -15.680f), rot = new Vector3(28.894f, 334.543f, 4.350f) },
            new Prefab { prefab = "assets/prefabs/deployable/cctvcamera/cctv_deployed.prefab", pos = new Vector3(-5.477f, 3.941f, 9.970f), rot = new Vector3(359.879f, 332.430f, 3.808f) },
        };

        internal class ControllerAirEvent : FacepunchBehaviour
        {
            private MapMarkerGenericRadius _mapmarker;
            private VendingMachineMapMarker _vendingMarker;
            private SphereCollider _sphereCollider;

            private DiveSite _diveSite;
            private readonly HashSet<FreeableLootContainer> _diveSiteCrates = new HashSet<FreeableLootContainer>();

            private int _countCctv;
			internal bool KillEntities = false;
            internal HashSet<BaseEntity> Entities = new HashSet<BaseEntity>();
            internal HashSet<HotAirBalloon> AirBalloons = new HashSet<HotAirBalloon>();

            private Coroutine _spawnEntitiesCoroutine = null;

            internal HashSet<LootContainer> Crates = new HashSet<LootContainer>();
            internal HashSet<HackableLockedCrate> HackCrates = new HashSet<HackableLockedCrate>();

            internal HashSet<DroppedItemContainer> Backpacks = new HashSet<DroppedItemContainer>();
            internal HashSet<ScientistNPC> Scientists = new HashSet<ScientistNPC>();

            internal int TimeToFinish;
            internal HashSet<BasePlayer> Players = new HashSet<BasePlayer>();

            private void Awake()
            {
                transform.position = _ins.SpawnPos;
                transform.rotation = Quaternion.Euler(new Vector3(1.860f, 27.461f, 356.719f));

                SpawnMapMarker();

                gameObject.layer = 3;
                _sphereCollider = gameObject.AddComponent<SphereCollider>();
                _sphereCollider.isTrigger = true;
                _sphereCollider.radius = _ins.Radius;

                InvokeRepeating(SpawnDiveSite, 0, 1800f);

                TimeToFinish = _ins._config.FinishTime;

                _spawnEntitiesCoroutine = ServerMgr.Instance.StartCoroutine(SpawnEntities());
            }

            private void OnDestroy()
            {
                if (_spawnEntitiesCoroutine != null) ServerMgr.Instance.StopCoroutine(_spawnEntitiesCoroutine);

                CancelInvoke(SpawnDiveSite);
                CancelInvoke(CheckContainers);
                if (_diveSite.IsExists()) _diveSite.Kill();

                CancelInvoke(UpdateMapMarker);
                if (_mapmarker.IsExists()) _mapmarker.Kill();
                if (_vendingMarker.IsExists()) _vendingMarker.Kill();

                KillEntities = true;
				foreach (BaseEntity entity in Entities)
                {
                    if (entity is HotAirBalloon)
                    {
                        HotAirBalloon airBalloon = entity as HotAirBalloon;
                        if (Physics.OverlapSphere(airBalloon.transform.position, 1f).Any(x => x.ToBaseEntity() != null && x.ToBaseEntity() is BasePlayer))
                        {
                            StorageContainer container = airBalloon.GetComponentsInChildren<StorageContainer>().FirstOrDefault(x => x.ShortPrefabName == "fuel_storage");
                            if (container != null) container.SetFlag(BaseEntity.Flags.Locked, false);
                            airBalloon.myRigidbody.isKinematic = false;
                        }
                        else if (airBalloon.IsExists()) airBalloon.Kill();
                    }
                    else if (entity.IsExists()) entity.Kill();
                }

                foreach (LootContainer crate in Crates) if (crate.IsExists()) crate.Kill();
                foreach (HackableLockedCrate hackCrate in HackCrates) if (hackCrate.IsExists()) hackCrate.Kill();

                foreach (DroppedItemContainer backpack in Backpacks) if (backpack.IsExists()) backpack.Kill();
                foreach (ScientistNPC npc in Scientists) if (npc.IsExists()) npc.Kill();

                CancelInvoke(ChangeToFinishTime);
                foreach (BasePlayer player in Players) CuiHelper.DestroyUi(player, "Tabs_KpucTaJl");
            }

            private void FixedUpdate() { foreach (HotAirBalloon airBalloon in AirBalloons) airBalloon.inflationLevel = float.PositiveInfinity; }

            private void OnTriggerEnter(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player.IsPlayer())
                {
                    Players.Add(player);
                    if (_ins._config.Gui.IsGui) _ins.CreateTabs(player, new Dictionary<string, string> { ["Clock_KpucTaJl"] = GetTimeFormat(), ["Crate_KpucTaJl"] = $"{Crates.Count + HackCrates.Count}" });
                    if (_ins._config.IsCreateZonePvp) _ins.AlertToPlayer(player, _ins.GetMessage("EnterPVP", player.UserIDString, _ins._config.Prefix));
                }
            }

            private void OnTriggerExit(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player.IsPlayer())
                {
                    Players.Remove(player);
                    if (_ins._config.Gui.IsGui) CuiHelper.DestroyUi(player, "Tabs_KpucTaJl");
                    if (_ins._config.IsCreateZonePvp) _ins.AlertToPlayer(player, _ins.GetMessage("ExitPVP", player.UserIDString, _ins._config.Prefix));
                }
            }

            private void ChangeToFinishTime()
            {
                TimeToFinish--;
                if (_ins._config.Gui.IsGui) foreach (BasePlayer player in Players) _ins.CreateTabs(player, new Dictionary<string, string> { ["Clock_KpucTaJl"] = GetTimeFormat(), ["Crate_KpucTaJl"] = $"{Crates.Count + HackCrates.Count}" });
                if ((TimeToFinish == _ins._config.PreFinishTime || Crates.Count + HackCrates.Count == 0) && TimeToFinish >= _ins._config.PreFinishTime)
                {
                    TimeToFinish = _ins._config.PreFinishTime;
                    _ins.AlertToAllPlayers("PreFinish", _ins._config.Prefix, _ins._config.PreFinishTime);
                }
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

            private IEnumerator SpawnEntities()
            {
                foreach (Prefab prefab in _ins.Prefabs)
                {
                    Vector3 pos; Quaternion rot;
                    GetGlobal(transform, prefab.pos, prefab.rot, out pos, out rot);
                    BaseEntity entity = SpawnEntity(prefab.prefab, pos, rot);

                    if (entity is BuildingBlock)
                    {
                        BuildingBlock buildingBlock = entity as BuildingBlock;
                        buildingBlock.SetGrade(BuildingGrade.Enum.Metal);
                        buildingBlock.SetHealthToMax();
                    }

                    if (entity is FlasherLight) (entity as FlasherLight).UpdateFromInput(1, 0);

                    if (entity is BaseOven)
                    {
                        BaseOven oven = entity as BaseOven;
                        oven.SetFlag(BaseEntity.Flags.On, true);
                        oven.SetFlag(BaseEntity.Flags.Locked, true);
                    }

                    if (entity is HotAirBalloon)
                    {
                        HotAirBalloon airBalloon = entity as HotAirBalloon;
                        StorageContainer container = airBalloon.GetComponentsInChildren<StorageContainer>().FirstOrDefault(x => x.ShortPrefabName == "fuel_storage");
                        if (container != null) container.SetFlag(BaseEntity.Flags.Locked, true);
                        airBalloon.myRigidbody.isKinematic = true;
                        AirBalloons.Add(airBalloon);
                    }

                    if (entity is CCTV_RC)
                    {
                        _countCctv++;
                        CCTV_RC cctv = entity as CCTV_RC;
                        cctv.UpdateFromInput(5, 0);
                        if (_countCctv == 1) cctv.rcIdentifier = _ins._config.Cctv1;
                        else if (_countCctv == 2) cctv.rcIdentifier = _ins._config.Cctv2;
                    }

                    if (entity is Signage) (entity as Signage).SetFlag(BaseEntity.Flags.Busy, true, true);

                    Entities.Add(entity);

                    yield return CoroutineEx.waitForSeconds(_ins._config.Delay);
                }
                SpawnCrates();
                SpawnHackCrate(new Vector3(5.488f, 3.882f, -11.071f), new Vector3(359.880f, 332.430f, 3.808f));
                SpawnHackCrate(new Vector3(-7.823f, 3.946f, 14.420f), new Vector3(0.121f, 152.430f, 356.192f));
                foreach (PresetConfig preset in _ins._config.Npc) SpawnPreset(preset);
                if (_ins._config.PveMode.Pve && _ins.plugins.Exists("PveMode"))
                {
                    JObject config = new JObject
                    {
                        ["Damage"] = _ins._config.PveMode.Damage,
                        ["ScaleDamage"] = new JArray { _ins._config.PveMode.ScaleDamage.Select(x => new JObject { ["Type"] = x.Type, ["Scale"] = x.Scale }) },
                        ["LootCrate"] = _ins._config.PveMode.LootCrate,
                        ["HackCrate"] = _ins._config.PveMode.HackCrate,
                        ["LootNpc"] = _ins._config.PveMode.LootNpc,
                        ["DamageNpc"] = _ins._config.PveMode.DamageNpc,
                        ["DamageTank"] = false,
                        ["TargetNpc"] = _ins._config.PveMode.TargetNpc,
                        ["TargetTank"] = false,
                        ["CanEnter"] = _ins._config.PveMode.CanEnter,
                        ["CanEnterCooldownPlayer"] = _ins._config.PveMode.CanEnterCooldownPlayer,
                        ["TimeExitOwner"] = _ins._config.PveMode.TimeExitOwner,
                        ["AlertTime"] = _ins._config.PveMode.AlertTime,
                        ["RestoreUponDeath"] = _ins._config.PveMode.RestoreUponDeath,
                        ["CooldownOwner"] = _ins._config.PveMode.CooldownOwner,
                        ["Darkening"] = _ins._config.PveMode.Darkening
                    };
                    HashSet<uint> crates = Crates.Select(x => x.net.ID);
                    foreach (HackableLockedCrate crate in HackCrates) crates.Add(crate.net.ID);
                    _ins.PveMode.Call("EventAddPveMode", _ins.Name, config, transform.position, _ins.Radius, crates, Scientists.Select(x => x.net.ID), new HashSet<uint>(), new HashSet<ulong>(), null);
                }
                InvokeRepeating(ChangeToFinishTime, 1f, 1f);
				Interface.Oxide.CallHook("OnAirEventStart", Entities);
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

            private void SpawnHackCrate(Vector3 posLocal, Vector3 rotLocal)
            {
                Vector3 pos; Quaternion rot;
                GetGlobal(transform, posLocal, rotLocal, out pos, out rot);
                HackableLockedCrate hackCrate = GameManager.server.CreateEntity("assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab", pos, rot) as HackableLockedCrate;
                hackCrate.enableSaving = false;
                hackCrate.Spawn();
                hackCrate.hackSeconds = HackableLockedCrate.requiredHackSeconds - _ins._config.HackCrate.UnlockTime;
                HackCrates.Add(hackCrate);
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

            private void SpawnPreset(PresetConfig preset)
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
                return new JObject
                {
                    ["Name"] = config.Name,
                    ["WearItems"] = new JArray { config.WearItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["SkinID"] = x.SkinID }) },
                    ["BeltItems"] = new JArray { config.BeltItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["Amount"] = x.Amount, ["SkinID"] = x.SkinID, ["Mods"] = new JArray { x.Mods }, ["Ammo"] = x.Ammo }) },
                    ["Kit"] = config.Kit,
                    ["Health"] = config.Health,
                    ["RoamRange"] = 0f,
                    ["ChaseRange"] = 0f,
                    ["DamageScale"] = config.DamageScale,
					["TurretDamageScale"] = 1f,
                    ["AimConeScale"] = config.AimConeScale,
                    ["DisableRadio"] = config.DisableRadio,
                    ["CanUseWeaponMounted"] = true,
                    ["CanRunAwayWater"] = true,
                    ["Speed"] = 0f,["AreaMask"] = 1,
                    ["AgentTypeID"] = -1372625422,
                    ["HomePosition"] = string.Empty,
                    ["States"] = new JArray { "IdleState", "CombatStationaryState" },
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

            private void SpawnDiveSite()
            {
                if (_diveSite.IsExists()) _diveSite.Kill();
                _diveSite = GameManager.server.CreateEntity("assets/prefabs/misc/divesite/divesite_a.prefab", transform.position, transform.rotation) as DiveSite;
                _diveSite.enableSaving = false;
                _diveSite.Spawn();
                InvokeRepeating(CheckContainers, 2f, 0.5f);
            }

            private void CheckContainers()
            {
                foreach (Collider hit in Physics.OverlapSphere(_diveSite.transform.position, 10f))
                {
                    FreeableLootContainer container = hit.ToBaseEntity() as FreeableLootContainer;
                    if (container == null || container.GetParentEntity() != null) continue;
                    container.SetParent(_diveSite);
                    container.transform.localPosition = Vector3.zero;
                    _diveSiteCrates.Add(container);
                    if (_diveSiteCrates.Count == 3)
                    {
                        CancelInvoke(CheckContainers);
                        break;
                    }
                }
            }

            private string GetTimeFormat()
            {
                if (TimeToFinish <= 60) return $"{TimeToFinish} sec.";
                else
                {
                    int sec = TimeToFinish % 60;
                    int min = (TimeToFinish - sec) / 60;
                    return $"{min} min. {sec} sec.";
                }
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
                PresetConfig preset = _config.Npc.FirstOrDefault(x => x.Config.Name == entity.displayName);
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
                PresetConfig preset = _config.Npc.FirstOrDefault(x => x.Config.Name == entity.displayName);
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
                PresetConfig preset = _config.Npc.FirstOrDefault(x => x.Config.Name == entity.displayName);
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
            if (_controller.Crates.Contains(container))
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
            if (_controller.Crates.Any(x => x.IsExists() && x.net.ID == netID))
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

        private object OnContainerPopulate(LootContainer container)
        {
            if (container == null || _controller == null) return null;
            if (_controller.Crates.Contains(container))
            {
                if (_config.TypeLootTableCrates == 6) return null;
                else return true;
            }
            else if (container is HackableLockedCrate && _controller.HackCrates.Contains(container as HackableLockedCrate))
            {
                if (_config.HackCrate.TypeLootTable == 6) return null;
                else return true;
            }
            else return null;
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

            foreach (PresetConfig preset in _config.Npc)
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
                    if (player != null) AlertToPlayer(player, GetMessage("SendEconomy", player.UserIDString, _config.Prefix, dic.Value));
                }
            }
            ulong winnerId = _playersBalance.Max(x => x.Value).Key;
            Interface.Oxide.CallHook("OnAirEventWinner", winnerId);
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
            if (!string.IsNullOrEmpty(_config.Prefix)) message = message.Replace(_config.Prefix + " ", string.Empty);
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
        public class ImageURL { public string Name; public string Url; }

        private readonly HashSet<ImageURL> _urls = new HashSet<ImageURL>
        {
            new ImageURL { Name = "Tab_KpucTaJl", Url = "Images/Tab_KpucTaJl.png" },
            new ImageURL { Name = "Clock_KpucTaJl", Url = "Images/Clock_KpucTaJl.png" },
            new ImageURL { Name = "Crate_KpucTaJl", Url = "Images/Crate_KpucTaJl.png" },
        };

        private readonly HashSet<string> _failedImages = new HashSet<string>();

        private readonly Dictionary<string, string> _images = new Dictionary<string, string>();

        private void DownloadImage()
        {
            ImageURL image = _urls.FirstOrDefault(x => !_images.ContainsKey(x.Name) && !_failedImages.Contains(x.Name));
            if (image != null)
            {
                Puts($"Downloading image {image.Name}...");
                ServerMgr.Instance.StartCoroutine(ProcessDownloadImage(image));
            }
            else if (_failedImages.Count > 0) Interface.Oxide.UnloadPlugin(Name);
        }

        IEnumerator ProcessDownloadImage(ImageURL image)
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
            }, "Hud", "Tabs_KpucTaJl");

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
        [PluginReference] private readonly Plugin NpcSpawn, PveMode;

        internal float Radius = 50f;

        private readonly HashSet<string> _hooks = new HashSet<string>
        {
            "OnEntityTakeDamage",
            "CanBuild",
            "CanAffordUpgrade",
            "OnStructureRotate",
            "OnEntitySpawned",
            "OnEntityKill",
            "OnEntityDeath",
            "CanHackCrate",
            "OnCrateHack",
            "OnSamSiteTarget",
            "OnLootEntity",
            "OnCorpsePopulate",
            "CanPopulateLoot",
            "OnCustomLootNPC",
            "OnCustomLootContainer",
			"OnContainerPopulate",
            "CanEntityTakeDamage",
            "CanTeleport"
        };

        private void Unsubscribes() { foreach (string hook in _hooks) Unsubscribe(hook); }

        private void Subscribes()
        {
            foreach (string hook in _hooks)
            {
                if (hook == "CanTeleport" && !_config.NTeleportationInterrupt) continue;
				if (hook == "CanEntityTakeDamage" && !_config.IsCreateZonePvp) continue;
                Subscribe(hook);
            }
        }
		
		private static BaseEntity SpawnEntity(string prefab, Vector3 pos, Quaternion rot)
        {
            BaseEntity entity = GameManager.server.CreateEntity(prefab, pos, rot);
            entity.enableSaving = false;

            GroundWatch groundWatch = entity.GetComponent<GroundWatch>();
            if (groundWatch != null) UnityEngine.Object.DestroyImmediate(groundWatch);

            DestroyOnGroundMissing destroyOnGroundMissing = entity.GetComponent<DestroyOnGroundMissing>();
            if (destroyOnGroundMissing != null) UnityEngine.Object.DestroyImmediate(destroyOnGroundMissing);

            entity.Spawn();

            if (entity is StabilityEntity) (entity as StabilityEntity).grounded = true;
            if (entity is BaseCombatEntity) (entity as BaseCombatEntity).pickup.enabled = false;

            return entity;
		}	
        #endregion Helpers

        #region Commands
        [ChatCommand("airstart")]
        private void ChatStartEvent(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                if (!_active) Start();
                else PrintToChat(player, GetMessage("EventActive", player.UserIDString, _config.Prefix));
            }
        }

        [ChatCommand("airstop")]
        private void ChatStopEvent(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                if (_controller != null) Finish();
                else Interface.Oxide.ReloadPlugin(Name);
            }
        }

        [ChatCommand("airpos")]
        private void ChatCommandPos(BasePlayer player)
        {
            if (!player.IsAdmin || _controller == null) return;
            Vector3 pos = _controller.transform.InverseTransformPoint(player.transform.position);
            Puts($"Position: {pos}");
            PrintToChat(player, $"Position: {pos}");
        }

        [ConsoleCommand("airstart")]
        private void ConsoleStartEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
            {
                if (!_active) Start();
                else Puts("This event is active now. To finish this event (airstop), then to start the next one");
            }
        }

        [ConsoleCommand("airstop")]
        private void ConsoleStopEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
            {
                if (_controller != null) Finish();
                else Interface.Oxide.ReloadPlugin(Name);
            }
        }
        #endregion Commands
    }
}

namespace Oxide.Plugins.AirEventExtensionMethods
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