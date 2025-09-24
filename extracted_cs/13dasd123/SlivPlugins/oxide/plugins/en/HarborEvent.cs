// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.IO;
using Facepunch;
using UnityEngine.Networking;
using Oxide.Plugins.HarborEventExtensionMethods;

namespace Oxide.Plugins
{
    [Info("HarborEvent", "KpucTaJl", "2.2.0")]
    internal class HarborEvent : RustPlugin
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
            if (_config.PluginVersion < new VersionNumber(2, 1, 7))
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
            if (_config.PluginVersion < new VersionNumber(2, 1, 9))
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
                    Text = "HarborEvent"
                };
                _config.PveMode.DamageTurret = false;
                _config.PveMode.TargetTurret = false;
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
            [JsonProperty("Prefab")] public string Prefab { get; set; }
            [JsonProperty(En ? "Which loot table should the plugin use? (0 - default; 1 - own; 2 - AlphaLoot; 3 - CustomLoot; 4 - loot table of the Rust objects; 5 - combine the 1 and 4 methods)" : "Какую таблицу предметов необходимо использовать? (0 - стандартную; 1 - собственную; 2 - AlphaLoot; 3 - CustomLoot; 4 - таблица предметов объектов Rust; 5 - совместить 1 и 4 методы)")] public int TypeLootTable { get; set; }
            [JsonProperty(En ? "Loot table from prefabs (if the loot table type is 4 or 5)" : "Таблица предметов из prefab-ов (если тип таблицы предметов - 4 или 5)")] public PrefabLootTableConfig PrefabLootTable { get; set; }
            [JsonProperty(En ? "Own loot table (if the loot table type is 1 or 5)" : "Собственная таблица предметов (если тип таблицы предметов - 1 или 5)")] public LootTableConfig OwnLootTable { get; set; }
        }

        public class HackCrateConfig
        {
            [JsonProperty(En ? "Time to unlock the Crates [sec.]" : "Время разблокировки ящиков [sec.]")] public float UnlockTime { get; set; }
            [JsonProperty(En ? "Increase the event time if it's not enough to unlock the locked crate? [true/false]" : "Увеличивать время ивента, если недостаточно чтобы разблокировать заблокированный ящик? [true/false]")] public bool IncreaseEventTime { get; set; }
            [JsonProperty(En ? "Calling a patrol helicopter when the unlock begins?" : "Вызывать патрульный вертолет, когда начинается взлом? [true/false]")] public bool CallHelicopter { get; set; }
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
            [JsonProperty(En ? "Destruction of Bradley" : "Уничтожение Bradley")] public double Bradley { get; set; }
            [JsonProperty(En ? "Killing an NPC" : "Убийство NPC")] public double Npc { get; set; }
            [JsonProperty(En ? "Hacking a locked crate" : "Взлом заблокированного ящика")] public double LockedCrate { get; set; }
            [JsonProperty(En ? "Pressing the button" : "Нажатие кнопки")] public double Button { get; set; }
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
            [JsonProperty(En ? "Can the non-owner of the event do damage to Bradley? [true/false]" : "Может ли не владелец ивента наносить урон по Bradley? [true/false]")] public bool DamageTank { get; set; }
            [JsonProperty(En ? "Can the non-owner of the event do damage to Patrol Helicopter? [true/false]" : "Может ли не владелец ивента наносить урон по патрульному вертолету? [true/false]")] public bool DamageHelicopter { get; set; }
            [JsonProperty(En ? "Can the non-owner of the event do damage to Turret? [true/false]" : "Может ли не владелец ивента наносить урон по турелям? [true/false]")] public bool DamageTurret { get; set; }
            [JsonProperty(En ? "Can an Npc attack a non-owner of the event? [true/false]" : "Может ли Npc атаковать не владельца ивента? [true/false]")] public bool TargetNpc { get; set; }
            [JsonProperty(En ? "Can Bradley attack a non-owner of the event? [true/false]" : "Может ли Bradley атаковать не владельца ивента? [true/false]")] public bool TargetTank { get; set; }
            [JsonProperty(En ? "Can Patrol Helicopter attack a non-owner of the event? [true/false]" : "Может ли патрульный вертолет атаковать не владельца ивента? [true/false]")] public bool TargetHelicopter { get; set; }
            [JsonProperty(En ? "Can Turret attack a non-owner of the event? [true/false]" : "Могут ли турели атаковать не владельца ивента? [true/false]")] public bool TargetTurret { get; set; }
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

        public class NpcConfigCargo
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
            [JsonProperty(En ? "Remove a corpse after death? (it is recommended to use the true value to improve performance) [true/false]" : "Удалять труп после смерти? (рекомендуется использовать значение true для повышения производительности) [true/false]")] public bool IsRemoveCorpse { get; set; }
            [JsonProperty(En ? "Wear items" : "Одежда")] public HashSet<NpcWear> WearItems { get; set; }
            [JsonProperty(En ? "Belt items" : "Быстрые слоты")] public HashSet<NpcBelt> BeltItems { get; set; }
            [JsonProperty(En ? "Kit (it is recommended to use the previous 2 settings to improve performance)" : "Kit (рекомендуется использовать предыдущие 2 пункта настройки для повышения производительности)")] public string Kit { get; set; }
            [JsonProperty(En ? "Which loot table should the plugin use? (0 - default; 1 - own; 2 - AlphaLoot; 3 - CustomLoot; 4 - loot table of the Rust objects; 5 - combine the 1 and 4 methods)" : "Какую таблицу предметов необходимо использовать? (0 - стандартную; 1 - собственную; 2 - AlphaLoot; 3 - CustomLoot; 4 - таблица предметов объектов Rust; 5 - совместить 1 и 4 методы)")] public int TypeLootTable { get; set; }
            [JsonProperty(En ? "Loot table from prefabs (if the loot table type is 4 or 5)" : "Таблица предметов из prefab-ов (если тип таблицы предметов - 4 или 5)")] public PrefabLootTableConfig PrefabLootTable { get; set; }
            [JsonProperty(En ? "Own loot table (if the loot table type is 1 or 5)" : "Собственная таблица предметов (если тип таблицы предметов - 1 или 5)")] public LootTableConfig OwnLootTable { get; set; }
        }

        public class BradleyConfig
        {
            [JsonProperty(En ? "Can Bradley appear? [true/false]" : "Должен ли появляться Bradley? [true/false]")] public bool IsBradley { get; set; }
            [JsonProperty(En ? "Health" : "Кол-во ХП")] public float Hp { get; set; }
            [JsonProperty(En ? "The viewing distance" : "Дальность обзора")] public float ViewDistance { get; set; }
            [JsonProperty(En ? "Radius of search" : "Радиус поиска")] public float SearchRange { get; set; }
            [JsonProperty(En ? "Scale damage" : "Множитель урона")] public float ScaleDamage { get; set; }
            [JsonProperty(En ? "The multiplier of Machine-gun aim cone" : "Множитель разброса пулемёта")] public float CoaxAimCone { get; set; }
            [JsonProperty(En ? "The multiplier of Machine-gun fire rate" : "Множитель скорострельности пулемёта")] public float CoaxFireRate { get; set; }
            [JsonProperty(En ? "Amount of Machine-gun burst shots" : "Кол-во выстрелов очереди пулемёта")] public int CoaxBurstLength { get; set; }
            [JsonProperty(En ? "Time that Bradley holds in memory the position of its last target [sec.]" : "Время, которое Bradley помнит позицию своей последней цели [sec.]")] public float MemoryDuration { get; set; }
            [JsonProperty(En ? "The time between shots of the main gun [sec.]" : "Время между залпами основного орудия [sec.]")] public float NextFireTime { get; set; }
            [JsonProperty(En ? "The time between shots of the main gun in a fire rate [sec.]" : "Время между выстрелами основного орудия в залпе [sec.]")] public float TopTurretFireRate { get; set; }
            [JsonProperty(En ? "Numbers of Crates" : "Кол-во ящиков после уничтожения")] public int CountCrates { get; set; }
            [JsonProperty(En ? "Which loot table should the plugin use? (0 - default; 1 - own; 2 - AlphaLoot; 3 - CustomLoot; 4 - loot table of the Rust objects; 5 - combine the 1 and 4 methods)" : "Какую таблицу лута необходимо использовать? (0 - стандартную; 1 - собственную; 2 - AlphaLoot; 3 - CustomLoot; 4 - таблица предметов объектов Rust; 5 - совместить 1 и 4 методы)")] public int TypeLootTable { get; set; }
            [JsonProperty(En ? "Loot table from prefabs (if the loot table type is 4 or 5)" : "Таблица предметов из prefab-ов (если тип таблицы предметов - 4 или 5)")] public PrefabLootTableConfig PrefabLootTable { get; set; }
            [JsonProperty(En ? "Own loot table (if the loot table type is 1 or 5)" : "Собственная таблица предметов (если тип таблицы предметов - 1 или 5)")] public LootTableConfig OwnLootTable { get; set; }
        }

        public class HelicopterConfig
        {
            [JsonProperty(En ? "Health" : "Кол-во ХП")] public float Hp { get; set; }
            [JsonProperty(En ? "Health Main Rotor" : "Кол-во ХП основного винта")] public float HpMainRotor { get; set; }
            [JsonProperty(En ? "Health Tail Rotor" : "Кол-во ХП хвостового винта")] public float HpTailRotor { get; set; }
            [JsonProperty(En ? "Numbers of Crates" : "Кол-во ящиков после уничтожения")] public int CountCrates { get; set; }
            [JsonProperty(En ? "Time between firing rockets" : "Время между выстрелом ракет")] public float TimeBetweenRockets { get; set; }
            [JsonProperty(En ? "Time between turret shots" : "Время между выстрелами пулемета")] public float FireRate { get; set; }
            [JsonProperty(En ? "Time between turret bursts" : "Время между очередями пулемета")] public float TimeBetweenBursts { get; set; }
            [JsonProperty(En ? "Duration of the burst turret" : "Продолжительность очереди пулемета")] public float BurstLength { get; set; }
            [JsonProperty(En ? "Turret firing radius" : "Дистанция стрельбы пулемета")] public float MaxTargetRange { get; set; }
        }

        public class TurretConfig
        {
            [JsonProperty(En ? "Can Auto Turret appear? [true/false]" : "Использовать турели? [true/false]")] public bool IsTurret { get; set; }
            [JsonProperty(En ? "Health" : "Кол-во ХП")] public float Hp { get; set; }
            [JsonProperty(En ? "Weapon ShortName" : "ShortName оружия")] public string ShortNameWeapon { get; set; }
            [JsonProperty(En ? "Ammo ShortName" : "ShortName патронов")] public string ShortNameAmmo { get; set; }
            [JsonProperty(En ? "Number of ammo" : "Кол-во патронов")] public int CountAmmo { get; set; }
        }

        private class PluginConfig
        {
            [JsonProperty(En ? "Minimum time between events [sec.]" : "Минимальное время между ивентами [sec.]")] public float MinStartTime { get; set; }
            [JsonProperty(En ? "Maximum time between events [sec.]" : "Максимальное время между ивентами [sec.]")] public float MaxStartTime { get; set; }
            [JsonProperty(En ? "Is active the timer on to start the event? [true/false]" : "Активен ли таймер для запуска ивента? [true/false]")] public bool EnabledTimer { get; set; }
            [JsonProperty(En ? "Duration of the event [sec.]" : "Время проведения ивента [sec.]")] public int FinishTime { get; set; }
            [JsonProperty(En ? "Time before the starting of the event after receiving a chat message [sec.]" : "Время до начала ивента после сообщения в чате [sec.]")] public float PreStartTime { get; set; }
            [JsonProperty(En ? "Notification time until the end of the event [sec.]" : "Время оповещения до окончания ивента [sec.]")] public int PreFinishTime { get; set; }
            [JsonProperty(En ? "Crates settings in Cargo Container" : "Настройка ящиков в контейнере")] public HashSet<CrateConfig> ContainerCrates { get; set; }
            [JsonProperty(En ? "Crates settings on Cargo Ship" : "Настройка ящиков на корабле")] public HashSet<CrateConfig> CargoCrates { get; set; }
            [JsonProperty(En ? "Locked crates settings in Cargo Container" : "Настройка заблокированных ящиков в контейнере")] public HackCrateConfig ContainerHackCrates { get; set; }
            [JsonProperty(En ? "Locked crates settings on Cargo Ship" : "Настройка заблокированных ящиков на корабле")] public HackCrateConfig CargoHackCrates { get; set; }
            [JsonProperty(En ? "NPCs settings in Small Harbor" : "Настройка NPC в Малом Порту")] public HashSet<PresetConfig> NpcSmall { get; set; }
            [JsonProperty(En ? "NPCs settings in Large Harbor" : "Настройка NPC в Большом Порту")] public HashSet<PresetConfig> NpcLarge { get; set; }
            [JsonProperty(En ? "Mobile NPCs settings on Cargo Ship" : "Настройка двигающихся NPC на корабле")] public NpcConfigCargo NpcMovingCargo { get; set; }
            [JsonProperty(En ? "Stationary NPCs settings inside Cargo Ship" : "Настройка стационарных NPC внутри корабля")] public NpcConfigCargo NpcStationaryInsideCargo { get; set; }
            [JsonProperty(En ? "Stationary NPCs settings outside Cargo Ship" : "Настройка стационарных NPC снаружи корабля")] public NpcConfigCargo NpcStationaryOutsideCargo { get; set; }
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
            [JsonProperty(En ? "Interrupt the teleport in harbor? (only for users NTeleportation plugin) [true/false]" : "Запрещать телепорт в порту? (только для тех, кто использует плагин NTeleportation) [true/false]")] public bool NTeleportationInterrupt { get; set; }
            [JsonProperty(En ? "Disable NPCs from the BetterNpc plugin on the monument while the event is on? [true/false]" : "Отключать NPC из плагина BetterNpc на монументе пока проходит ивент? [true/false]")] public bool RemoveBetterNpc { get; set; }
            [JsonProperty(En ? "Economy setting (total values will be added up and rewarded at the end of the event)" : "Настройка экономики (конечное значение суммируется и будет выдано игрокам по окончанию ивента)")] public EconomyConfig Economy { get; set; }
            [JsonProperty(En ? "List of commands banned in the event zone" : "Список команд запрещенных в зоне ивента")] public HashSet<string> Commands { get; set; }
            [JsonProperty(En ? "Bradley setting" : "Настройка танка")] public BradleyConfig Bradley { get; set; }
            [JsonProperty(En ? "Helicopter setting" : "Настройка вертолета")] public HelicopterConfig Helicopter { get; set; }
            [JsonProperty(En ? "Setting AutoTurrets" : "Настройка турелей")] public TurretConfig Turret { get; set; }
            [JsonProperty(En ? "The CCTV camera" : "Название камеры")] public string Cctv { get; set; }
            [JsonProperty(En ? "Can an event appear in a Small Harbor? [true/false]" : "Должен ли ивент появляться в Малом Порту? [true/false]")] public bool IsSmallHarbor { get; set; }
            [JsonProperty(En ? "Can an event appear in a Large Harbor? [true/false]" : "Должен ли ивент появляться в Большом Порту? [true/false]")] public bool IsLargeHarbor { get; set; }
            [JsonProperty(En ? "Should Cargo Ship sail away after timer ends? (сan bug out if Harbor is covered or between Islands) [true/false]" : "Корабль уплывает с карты? (возможен баг с прохождением сквозь землю, если порт находится в заливе или между островами) [true/false]")] public bool LeavingShip { get; set; }
            [JsonProperty(En ? "Can SAM Site turrets appear? [true/false]" : "Должны ли появляться Sam Site турели? [true/false]")] public bool IsSamSites { get; set; }
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
                    ContainerCrates = new HashSet<CrateConfig>
                    {
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_elite.prefab",
                            TypeLootTable = 0,
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_elite.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 25, MaxAmount = 25, Chance = 100f, IsBluePrint = false, SkinId = 0, Name = "" } }
                            }
                        },
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_normal.prefab",
                            TypeLootTable = 0,
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_normal.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 8, MaxAmount = 8, Chance = 100f, IsBluePrint = false, SkinId = 0, Name = "" } }
                            }
                        },
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/dmloot/dm c4.prefab",
                            TypeLootTable = 0,
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/dmloot/dm c4.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 100f, IsBluePrint = false, SkinId = 0, Name = "" } }
                            }
                        },
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/dmloot/dm ammo.prefab",
                            TypeLootTable = 0,
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/dmloot/dm ammo.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 100f, IsBluePrint = false, SkinId = 0, Name = "" } }
                            }
                        }
                    },
                    CargoCrates = new HashSet<CrateConfig>
                    {
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_elite.prefab",
                            TypeLootTable = 0,
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_elite.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 25, MaxAmount = 25, Chance = 100f, IsBluePrint = false, SkinId = 0, Name = "" } }
                            }
                        },
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_normal.prefab",
                            TypeLootTable = 0,
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_normal.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 8, MaxAmount = 8, Chance = 100f, IsBluePrint = false, SkinId = 0, Name = "" } }
                            }
                        },
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_normal_2.prefab",
                            TypeLootTable = 0,
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_normal_2.prefab" } }
                            },
                            OwnLootTable = new LootTableConfig
                            {
                                Min = 1, Max = 1, UseCount = true,
                                Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 5, Chance = 100f, IsBluePrint = false, SkinId = 0, Name = "" } }
                            }
                        }
                    },
                    ContainerHackCrates = new HackCrateConfig
                    {
                        UnlockTime = 600f,
                        IncreaseEventTime = true,
                        CallHelicopter = true,
                        TypeLootTable = 0,
                        PrefabLootTable = new PrefabLootTableConfig
                        {
                            Min = 1, Max = 1, UseCount = true,
                            Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab" } }
                        },
                        OwnLootTable = new LootTableConfig
                        {
                            Min = 1, Max = 1, UseCount = true,
                            Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 100f, IsBluePrint = false, SkinId = 0, Name = "" } }
                        }
                    },
                    CargoHackCrates = new HackCrateConfig
                    {
                        UnlockTime = 600f,
                        IncreaseEventTime = true,
                        TypeLootTable = 0,
                        CallHelicopter = false,
                        PrefabLootTable = new PrefabLootTableConfig
                        {
                            Min = 1, Max = 1, UseCount = true,
                            Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab" } }
                        },
                        OwnLootTable = new LootTableConfig
                        {
                            Min = 1, Max = 1, UseCount = true,
                            Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 100f, IsBluePrint = false, SkinId = 0, Name = "" } }
                        }
                    },
                    NpcSmall = new HashSet<PresetConfig>
                    {
                        new PresetConfig
                        {
                            Min = 2,
                            Max = 2,
                            Positions = new HashSet<string>
                            {
                                "(35.0, 4.8, 77.5)",
                                "(31.4, 9.0, 82.7)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "Soldier",
                                Health = 200f,
                                RoamRange = 0f,
                                ChaseRange = 0f,
                                AttackRangeMultiplier = 5f,
                                SenseRange = 100f,
                                MemoryDuration = 10f,
                                DamageScale = 2f,
                                AimConeScale = 0f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 0f,
                                DisableRadio = false,
                                Stationary = true,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "hoodie", SkinId = 2563940111 },
                                    new NpcWear { ShortName = "pants", SkinId = 2563935722 },
                                    new NpcWear { ShortName = "shoes.boots", SkinId = 2575506021 },
                                    new NpcWear { ShortName = "roadsign.jacket", SkinId = 2570233552 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinId = 2582714399 },
                                    new NpcWear { ShortName = "coffeecan.helmet", SkinId = 2570227850 },
                                    new NpcWear { ShortName = "roadsign.kilt", SkinId = 2570237224 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "rifle.m39", Amount = 1, SkinId = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.small.scope" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 10, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "grenade.f1", Amount = 10, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
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
                            Min = 1,
                            Max = 1,
                            Positions = new HashSet<string>
                            {
                                "(37.1, 16.2, 83.3)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "Sniper",
                                Health = 200f,
                                RoamRange = 0f,
                                ChaseRange = 0f,
                                AttackRangeMultiplier = 1f,
                                SenseRange = 150f,
                                MemoryDuration = 30f,
                                DamageScale = 0.25f,
                                AimConeScale = 1f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 0f,
                                DisableRadio = false,
                                Stationary = true,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "hat.boonie", SkinId = 1275532550 },
                                    new NpcWear { ShortName = "mask.bandana", SkinId = 1623665052 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinId = 1113475533 },
                                    new NpcWear { ShortName = "hoodie", SkinId = 1275521888 },
                                    new NpcWear { ShortName = "pants", SkinId = 1277403128 },
                                    new NpcWear { ShortName = "shoes.boots", SkinId = 0 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "rifle.bolt", Amount = 1, SkinId = 897867582, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.8x.scope" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 5, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
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
                            Min = 4,
                            Max = 4,
                            Positions = new HashSet<string>
                            {
                                "(40.2, 2.3, 69.1)",
                                "(30.0, 2.3, 69.1)",
                                "(26.8, 2.3, 90.9)",
                                "(43.7, 2.3, 90.4)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "Porter",
                                Health = 175f,
                                RoamRange = 10f,
                                ChaseRange = 30f,
                                AttackRangeMultiplier = 2.5f,
                                SenseRange = 70f,
                                MemoryDuration = 120f,
                                DamageScale = 1.7f,
                                AimConeScale = 0.6f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 7.5f,
                                DisableRadio = true,
                                Stationary = false,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "hoodie", SkinId = 1819497052 },
                                    new NpcWear { ShortName = "shoes.boots", SkinId = 0 },
                                    new NpcWear { ShortName = "hat.beenie", SkinId = 0 },
                                    new NpcWear { ShortName = "movembermoustache", SkinId = 0 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinId = 0 },
                                    new NpcWear { ShortName = "pants", SkinId = 1819498178 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "smg.mp5", Amount = 1, SkinId = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 5, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "grenade.f1", Amount = 2, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
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
                    NpcLarge = new HashSet<PresetConfig>
                    {
                        new PresetConfig
                        {
                            Min = 2,
                            Max = 2,
                            Positions = new HashSet<string>
                            {
                                "(91.0, 11.7, -23.6)",
                                "(85.7, 7.6, -26.7)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "Soldier",
                                Health = 200f,
                                RoamRange = 0f,
                                ChaseRange = 0f,
                                AttackRangeMultiplier = 5f,
                                SenseRange = 100f,
                                MemoryDuration = 10f,
                                DamageScale = 2f,
                                AimConeScale = 0f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 0f,
                                DisableRadio = false,
                                Stationary = true,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "hoodie", SkinId = 2563940111 },
                                    new NpcWear { ShortName = "pants", SkinId = 2563935722 },
                                    new NpcWear { ShortName = "shoes.boots", SkinId = 2575506021 },
                                    new NpcWear { ShortName = "roadsign.jacket", SkinId = 2570233552 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinId = 2582714399 },
                                    new NpcWear { ShortName = "coffeecan.helmet", SkinId = 2570227850 },
                                    new NpcWear { ShortName = "roadsign.kilt", SkinId = 2570237224 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "rifle.m39", Amount = 1, SkinId = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.small.scope" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 10, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "grenade.f1", Amount = 10, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
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
                            Min = 1,
                            Max = 1,
                            Positions = new HashSet<string>
                            {
                                "(91.4, 19.0, -29.2)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "Sniper",
                                Health = 200f,
                                RoamRange = 0f,
                                ChaseRange = 0f,
                                AttackRangeMultiplier = 1f,
                                SenseRange = 150f,
                                MemoryDuration = 30f,
                                DamageScale = 0.25f,
                                AimConeScale = 1f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 0f,
                                DisableRadio = false,
                                Stationary = true,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "hat.boonie", SkinId = 1275532550 },
                                    new NpcWear { ShortName = "mask.bandana", SkinId = 1623665052 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinId = 1113475533 },
                                    new NpcWear { ShortName = "hoodie", SkinId = 1275521888 },
                                    new NpcWear { ShortName = "pants", SkinId = 1277403128 },
                                    new NpcWear { ShortName = "shoes.boots", SkinId = 0 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "rifle.bolt", Amount = 1, SkinId = 897867582, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.8x.scope" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 5, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
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
                            Max = 3,
                            Positions = new HashSet<string>
                            {
                                "(77.1, 5.0, -32.7)",
                                "(77.1, 5.0, -23.8)",
                                "(74.1, 5.0, -23.8)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "Porter",
                                Health = 175f,
                                RoamRange = 10f,
                                ChaseRange = 30f,
                                AttackRangeMultiplier = 2.5f,
                                SenseRange = 70f,
                                MemoryDuration = 120f,
                                DamageScale = 1.7f,
                                AimConeScale = 0.6f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 7.5f,
                                DisableRadio = true,
                                Stationary = false,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "hoodie", SkinId = 1819497052 },
                                    new NpcWear { ShortName = "shoes.boots", SkinId = 0 },
                                    new NpcWear { ShortName = "hat.beenie", SkinId = 0 },
                                    new NpcWear { ShortName = "movembermoustache", SkinId = 0 },
                                    new NpcWear { ShortName = "burlap.gloves", SkinId = 0 },
                                    new NpcWear { ShortName = "pants", SkinId = 1819498178 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "smg.mp5", Amount = 1, SkinId = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 5, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "grenade.f1", Amount = 2, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
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
                    NpcMovingCargo = new NpcConfigCargo
                    {
                        Name = "Scientist",
                        Health = 250f,
                        RoamRange = 30f,
                        ChaseRange = 60f,
                        AttackRangeMultiplier = 1f,
                        SenseRange = 50f,
                        MemoryDuration = 30f,
                        DamageScale = 1.15f,
                        AimConeScale = 1.3f,
                        CheckVisionCone = false,
                        VisionCone = 135f,
                        Speed = 7.5f,
                        DisableRadio = false,
                        IsRemoveCorpse = true,
                        WearItems = new HashSet<NpcWear>
                        {
                            new NpcWear { ShortName = "hat.cap", SkinId = 2891590451 },
                            new NpcWear { ShortName = "hoodie", SkinId = 2882740093 },
                            new NpcWear { ShortName = "pants", SkinId = 2882737241 },
                            new NpcWear { ShortName = "shoes.boots", SkinId = 826587881 },
                            new NpcWear { ShortName = "sunglasses", SkinId = 0 }
                        },
                        BeltItems = new HashSet<NpcBelt>
                        {
                            new NpcBelt { ShortName = "rifle.lr300", Amount = 1, SkinId = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                            new NpcBelt { ShortName = "syringe.medical", Amount = 10, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                            new NpcBelt { ShortName = "grenade.f1", Amount = 10, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                        },
                        Kit = "",
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
                    NpcStationaryInsideCargo = new NpcConfigCargo
                    {
                        Name = "Scientist",
                        Health = 250f,
                        RoamRange = 10f,
                        ChaseRange = 100f,
                        AttackRangeMultiplier = 1f,
                        SenseRange = 50f,
                        MemoryDuration = 30f,
                        DamageScale = 1.15f,
                        AimConeScale = 1.3f,
                        CheckVisionCone = false,
                        VisionCone = 135f,
                        Speed = 7.5f,
                        DisableRadio = false,
                        IsRemoveCorpse = true,
                        WearItems = new HashSet<NpcWear>
                        {
                            new NpcWear { ShortName = "hat.cap", SkinId = 0 },
                            new NpcWear { ShortName = "hoodie", SkinId = 2408787588 },
                            new NpcWear { ShortName = "pants", SkinId = 2408786118 },
                            new NpcWear { ShortName = "shoes.boots", SkinId = 826587881 },
                            new NpcWear { ShortName = "sunglasses", SkinId = 0 }
                        },
                        BeltItems = new HashSet<NpcBelt>
                        {
                            new NpcBelt { ShortName = "rifle.lr300", Amount = 1, SkinId = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                            new NpcBelt { ShortName = "syringe.medical", Amount = 10, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                            new NpcBelt { ShortName = "grenade.f1", Amount = 10, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                        },
                        Kit = "",
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
                    NpcStationaryOutsideCargo = new NpcConfigCargo
                    {
                        Name = "Scientist",
                        Health = 175f,
                        RoamRange = 10f,
                        ChaseRange = 100f,
                        AttackRangeMultiplier = 1f,
                        SenseRange = 50f,
                        MemoryDuration = 30f,
                        DamageScale = 1.15f,
                        AimConeScale = 1.3f,
                        CheckVisionCone = false,
                        VisionCone = 135f,
                        Speed = 7.5f,
                        DisableRadio = false,
                        IsRemoveCorpse = true,
                        WearItems = new HashSet<NpcWear>
                        {
                            new NpcWear { ShortName = "hazmatsuit_scientist", SkinId = 0 }
                        },
                        BeltItems = new HashSet<NpcBelt>
                        {
                            new NpcBelt { ShortName = "rifle.lr300", Amount = 1, SkinId = 0, Mods = new HashSet<string> { "weapon.mod.flashlight", "weapon.mod.holosight" }, Ammo = string.Empty },
                            new NpcBelt { ShortName = "syringe.medical", Amount = 10, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                            new NpcBelt { ShortName = "grenade.f1", Amount = 10, SkinId = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
                        },
                        Kit = "",
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
                        Text = "HarborEvent"
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
                    Prefix = "[HarborEvent]",
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
                            "KillBradley",
                            "OpenDoor"
                        }
                    },
                    Radius = 200f,
                    IsCreateZonePvp = false,
                    PveMode = new PveModeConfig
                    {
                        Pve = false,
                        Damage = 500f,
                        ScaleDamage = new HashSet<ScaleDamageConfig>
                        {
                            new ScaleDamageConfig { Type = "NPC", Scale = 1f },
                            new ScaleDamageConfig { Type = "Bradley", Scale = 2f },
                            new ScaleDamageConfig { Type = "Helicopter", Scale = 2f }
                        },
                        LootCrate = false,
                        HackCrate = false,
                        LootNpc = false,
                        DamageNpc = false,
                        DamageTank = false,
                        DamageHelicopter = false,
                        DamageTurret = false,
                        TargetNpc = false,
                        TargetTank = false,
                        TargetHelicopter = false,
                        TargetTurret = false,
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
                            ["crate_elite"] = 0.4,
                            ["crate_normal"] = 0.2,
                            ["crate_normal_2"] = 0.1,
                            ["dm c4"] = 0.4,
                            ["dm ammo"] = 0.3
                        },
                        Bradley = 0.8,
                        Npc = 0.3,
                        LockedCrate = 0.5,
                        Button = 0.4,
                        Commands = new HashSet<string>()
                    },
                    Commands = new HashSet<string>
                    {
                        "/remove",
                        "remove.toggle"
                    },
                    Bradley = new BradleyConfig
                    {
                        IsBradley = true,
                        Hp = 1000f,
                        ViewDistance = 100.0f,
                        SearchRange = 100.0f,
                        ScaleDamage = 1.0f,
                        CoaxAimCone = 1.1f,
                        CoaxFireRate = 1.0f,
                        CoaxBurstLength = 10,
                        MemoryDuration = 20f,
                        NextFireTime = 10f,
                        TopTurretFireRate = 0.25f,
                        CountCrates = 3,
                        TypeLootTable = 0,
                        PrefabLootTable = new PrefabLootTableConfig
                        {
                            Min = 1, Max = 1, UseCount = true,
                            Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/prefabs/npc/m2bradley/bradley_crate.prefab" } }
                        },
                        OwnLootTable = new LootTableConfig
                        {
                            Min = 1, Max = 1, UseCount = true,
                            Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 100f, IsBluePrint = false, SkinId = 0, Name = "" } }
                        }
                    },
                    Helicopter = new HelicopterConfig
                    {
                        Hp = 1000f,
                        HpMainRotor = 750f,
                        HpTailRotor = 375f,
                        CountCrates = 4,
                        TimeBetweenRockets = 0.2f,
                        FireRate = 0.125f,
                        TimeBetweenBursts = 3f,
                        BurstLength = 3f,
                        MaxTargetRange = 300f
                    },
                    Turret = new TurretConfig
                    {
                        IsTurret = true,
                        Hp = 1000f,
                        ShortNameWeapon = "rifle.ak",
                        ShortNameAmmo = "ammo.rifle",
                        CountAmmo = 1000
                    },
                    Cctv = "Harbor",
                    IsSmallHarbor = true,
                    IsLargeHarbor = true,
                    LeavingShip = false,
                    IsSamSites = true,
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
                ["PreStart"] = "{0} Loading of the Valuable Cargo on the Cargo Ship will begin in the Harbor in <color=#55aaff>{1}</color>!",
                ["Start"] = "{0} The Cargo Ship <color=#738d43>has arrived</color> at the Harbor in grid <color=#55aaff>{1}</color>\nThe loading of the cargo <color=#738d43>has begun</color>!\nCCTV: <color=#55aaff>{2}</color>",
                ["PreFinish"] = "{0} Loading of the Valuable Cargo <color=#ce3f27>will end</color> in <color=#55aaff>{1}</color>!",
                ["Finish"] = "{0} Loading of the Valuable Cargo <color=#ce3f27>has concluded</color>! The ship is leaving the map!",
                ["SetOwner"] = "{0} Player <color=#55aaff>{1}</color> <color=#738d43>has received</color> the owner status for the <color=#55aaff>Harbor Event</color>",
                ["EventActive"] = "{0} This event is active now. To finish this event (<color=#55aaff>/harborstop</color>), then (<color=#55aaff>/harborstart</color>) to start the next one!",
                ["EnterPVP"] = "{0} You <color=#ce3f27>have entered</color> the PVP zone, now other players <color=#ce3f27>can damage</color> you!",
                ["ExitPVP"] = "{0} You <color=#738d43>have left</color> the PVP zone, now other players <color=#738d43>cannot damage</color> you!",
                ["HeliArrive"] = "{0} A Patrol Helicopter <color=#ce3f27>was called</color> to defend the Loading of the Valuable Cargo!",
                ["KillBradley"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>destroyed</color> the tank!",
                ["OpenDoor"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>opened</color> the loot container door!",
                ["NTeleportation"] = "{0} You <color=#ce3f27>cannot</color> teleport into the event zone!",
                ["SendEconomy"] = "{0} You <color=#738d43>have earned</color> <color=#55aaff>{1}</color> points in economics for participating in the event",
                ["NoCommand"] = "{0} You <color=#ce3f27>cannot</color> use this command in the event zone!"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PreStart"] = "{0} Через <color=#55aaff>{1}</color> начнется погрузка ценного груза на корабль в порту!",
                ["Start"] = "{0} Корабль <color=#738d43>прибыл</color> в порт на квадрате <color=#55aaff>{1}</color>\nПогрузка груза <color=#738d43>началась</color>!\nКамера: <color=#55aaff>{2}</color>",
                ["PreFinish"] = "{0} Погрузка груза <color=#ce3f27>закончится</color> через <color=#55aaff>{1}</color>!",
                ["Finish"] = "{0} Погрузка груза <color=#ce3f27>закончена</color>! Корабль уплывает с карты",
                ["SetOwner"] = "{0} Игрок <color=#55aaff>{1}</color> <color=#738d43>получил</color> статус владельца ивента для <color=#55aaff>Harbor Event</color>",
                ["EventActive"] = "{0} Ивент в данный момент активен, сначала завершите текущий ивент (<color=#55aaff>/harborstop</color>), чтобы начать следующий!",
                ["EnterPVP"] = "{0} Вы <color=#ce3f27>вошли</color> в PVP зону, теперь другие игроки <color=#ce3f27>могут</color> наносить вам урон!",
                ["ExitPVP"] = "{0} Вы <color=#738d43>вышли</color> из PVP зоны, теперь другие игроки <color=#738d43>не могут</color> наносить вам урон!",
                ["HeliArrive"] = "{0} К месту погрузки ценного груза в порту <color=#ce3f27>вылетел</color> патрульный вертолет!",
                ["KillBradley"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>уничтожил</color> танк!",
                ["OpenDoor"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>открыл</color> дверь в контейнер!",
                ["NTeleportation"] = "{0} Вы <color=#ce3f27>не можете</color> телепортироваться в зоне ивента!",
                ["SendEconomy"] = "{0} Вы <color=#738d43>получили</color> <color=#55aaff>{1}</color> баллов в экономику за прохождение ивента",
                ["NoCommand"] = "{0} Вы <color=#ce3f27>не можете</color> использовать данную команду в зоне ивента!"
            }, this, "ru");
        }

        private string GetMessage(string langKey, string userId) => lang.GetMessage(langKey, _ins, userId);

        private string GetMessage(string langKey, string userId, params object[] args) => (args.Length == 0) ? GetMessage(langKey, userId) : string.Format(GetMessage(langKey, userId), args);
        #endregion Lang

        #region Oxide Hooks
        private static HarborEvent _ins;

        private void Init()
        {
            _ins = this;
            Unsubscribes();
        }

        private void OnServerInitialized()
        {
            if (GetMonument() == null)
            {
                PrintError("The harbor location is missing on the map. The plugin cannot be loaded!");
                NextTick(() => Interface.Oxide.UnloadPlugin(Name));
                return;
            }
            CheckAllLootTables();
            ServerMgr.Instance.StartCoroutine(DownloadImages());
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
            if (Controller.Entities.Contains(entity))
            {
                if (entity is SamSite) return null;
                if (entity is AutoTurret)
                {
                    if (entity.health - info.damageTypes.Total() <= 0f) (entity as AutoTurret).inventory.ClearItemsContainer();
                    return null;
                }
                return true;
            }
            if (info.Initiator == Controller.Bradley) info.damageTypes.ScaleAll(_config.Bradley.ScaleDamage);
            return null;
        }

        private object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            if (planner == null) return null;
            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null) return null;
            if (Controller.Players.Contains(player)) return true;
            return null;
        }

        private object CanChangeGrade(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade, ulong skin)
        {
            if (block != null && Controller.Entities.Contains(block)) return false;
            else return null;
        }

        private object OnStructureRotate(BuildingBlock block, BasePlayer player)
        {
            if (block != null && Controller.Entities.Contains(block)) return true;
            else return null;
        }

        private object OnButtonPress(PressButton button, BasePlayer player)
        {
            if (button == null || player == null) return null;
            if (button == Controller.Button && Controller.Door.IsExists())
            {
                if (ActivePveMode && PveMode.Call("CanActionEvent", Name, player) != null) return true;
                Controller.Door.Kill();
                ActionEconomy(player.userID, "Button");
                AlertToAllPlayers("OpenDoor", _config.Prefix, player.displayName);
            }
            return null;
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!_config.Marker.Enabled || Controller == null || !player.IsPlayer()) return;
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot)) timer.In(2f, () => OnPlayerConnected(player));
            else Controller.UpdateMapMarkers();
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player != null && Controller.Players.Contains(player))
                Controller.ExitPlayer(player);
        }

        private void OnEntityDeath(BradleyAPC bradley, HitInfo info)
        {
            if (bradley == null || info == null) return;
            if (bradley == Controller.Bradley)
            {
                BasePlayer attacker = info.InitiatorPlayer;
                if (attacker != null)
                {
                    ActionEconomy(attacker.userID, "Bradley");
                    AlertToAllPlayers("KillBradley", _config.Prefix, attacker.displayName);
                }
            }
        }

        private void OnEntityDeath(ScientistNPC npc, HitInfo info)
        {
            if (npc == null || info == null) return;
            BasePlayer attacker = info.InitiatorPlayer;
            if (Controller.Scientists.Contains(npc) && attacker.IsPlayer()) ActionEconomy(attacker.userID, "Npc");
        }

        private object CanBradleyApcTarget(BradleyAPC bradley, BaseEntity entity)
        {
            if (bradley == null || entity == null) return null;
            if (bradley == Controller.Bradley)
            {
                if ((entity as BasePlayer).IsPlayer()) return null;
                else return false;
            }
            return null;
        }

        private object OnTurretTarget(AutoTurret turret, BaseCombatEntity entity)
        {
            if (turret == null || entity == null) return null;
            if (Controller.Turrets.Contains(turret))
            {
                if ((entity as BasePlayer).IsPlayer()) return null;
                else return true;
            }
            return null;
        }

        private object OnEntityKill(BaseEntity entity)
        {
            if (entity == null || Controller == null) return null;
            if (Controller.Entities.Contains(entity))
            {
                if (entity is SamSite || entity is AutoTurret || entity is Door) return null;
                if (!Controller.KillEntities) return true;
            }
            return null;
        }

        private object OnEntityKill(CargoShip entity)
        {
            if (entity == null || Controller == null) return null;
            if (entity == Controller.Cargo) return true;
            return null;
        }

        private void OnEntityKill(LootContainer entity)
        {
            if (entity == null || Controller == null) return;
            if (Controller.Crates.ContainsKey(entity)) Controller.Crates.Remove(entity);
            else if (entity is HackableLockedCrate)
            {
                HackableLockedCrate hackcrate = entity as HackableLockedCrate;
                if (Controller.HackCrates.ContainsKey(hackcrate)) Controller.HackCrates.Remove(hackcrate);
            }
        }

        private void OnEntitySpawned(ScientistNPC entity)
        {
            if (entity != null && (entity.ShortPrefabName == "scientistnpc_cargo_turret_lr300" || entity.ShortPrefabName == "scientistnpc_cargo_turret_any" || entity.ShortPrefabName == "scientistnpc_cargo") && Vector3.Distance(entity.transform.position, Controller.transform.position) < _config.Radius)
                timer.In(1.5f, () => Controller.SpawnCargoScientist(entity));
        }

        private void OnEntitySpawned(LootContainer entity)
        {
            if (entity == null) return;
            if (entity.ShortPrefabName != "codelockedhackablecrate" && entity.ShortPrefabName != "crate_elite" && entity.ShortPrefabName != "crate_normal" && entity.ShortPrefabName != "crate_normal_2") return;
            if (Vector3.Distance(entity.transform.position, Controller.transform.position) > _config.Radius) return;
            timer.In(1.5f, () => Controller.SpawnCargoCrate(entity));
        }

        private Dictionary<ulong, ulong> StartHackCrates { get; } = new Dictionary<ulong, ulong>();

        private void CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (player == null || crate == null) return;
            if (Controller.HackCrates.ContainsKey(crate))
            {
                ulong crateId = crate.net.ID.Value;
                if (StartHackCrates.ContainsKey(crateId)) StartHackCrates[crateId] = player.userID;
                else StartHackCrates.Add(crateId, player.userID);
            }
        }

        private void OnCrateHack(HackableLockedCrate crate)
        {
            if (crate == null) return;
            ulong crateId = crate.net.ID.Value;
            ulong playerId;
            if (StartHackCrates.TryGetValue(crateId, out playerId))
            {
                StartHackCrates.Remove(crateId);
                ActionEconomy(playerId, "LockedCrate");
                if (Controller.IsMainHackCrate(crate))
                {
                    if (_config.ContainerHackCrates.IncreaseEventTime && Controller.TimeToFinish < (int)_config.ContainerHackCrates.UnlockTime) Controller.TimeToFinish += (int)_config.ContainerHackCrates.UnlockTime;
                    if (_config.ContainerHackCrates.CallHelicopter && !Controller.Helicopter.IsExists())
                    {
                        AlertToAllPlayers("HeliArrive", _config.Prefix);
                        Controller.SpawnHelicopter();
                    }
                }
                else
                {
                    if (_config.CargoHackCrates.IncreaseEventTime && Controller.TimeToFinish < (int)_config.CargoHackCrates.UnlockTime) Controller.TimeToFinish += (int)_config.CargoHackCrates.UnlockTime;
                    if (_config.CargoHackCrates.CallHelicopter && !Controller.Helicopter.IsExists())
                    {
                        AlertToAllPlayers("HeliArrive", _config.Prefix);
                        Controller.SpawnHelicopter();
                    }
                }
            }
        }

        private HashSet<ulong> LootableCrates { get; } = new HashSet<ulong>();

        private void OnLootEntity(BasePlayer player, LootContainer container)
        {
            if (!player.IsPlayer() || !container.IsExists() || LootableCrates.Contains(container.net.ID.Value)) return;
            if (Controller.Crates.ContainsKey(container))
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
        internal class Prefab { public string Path; public Vector3 Pos; public Vector3 Rot; }
        internal HashSet<Prefab> PrefabsHarborSmall { get; } = new HashSet<Prefab>
        {
            //wall
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(33.460f, 17.358f, 102.607f), Rot = new Vector3(0f, 180f, 90f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(33.460f, 17.358f, 105.607f), Rot = new Vector3(0f, 180f, 90f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(33.460f, 17.358f, 108.607f), Rot = new Vector3(0f, 180f, 90f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(36.460f, 14.358f, 102.607f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(36.460f, 14.358f, 105.607f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(36.460f, 14.358f, 108.607f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(33.460f, 14.358f, 102.607f), Rot = new Vector3(0f, 180f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(33.460f, 14.358f, 105.607f), Rot = new Vector3(0f, 180f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(33.460f, 14.358f, 108.607f), Rot = new Vector3(0f, 180f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(36.460f, 14.358f, 102.607f), Rot = new Vector3(0f, 180f, 270f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(36.460f, 14.358f, 105.607f), Rot = new Vector3(0f, 180f, 270f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(36.460f, 14.358f, 108.607f), Rot = new Vector3(0f, 180f, 270f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(34.960f, 14.358f, 101.107f), Rot = new Vector3(0f, 90f, 0f) },
            //floor.triangle
            new Prefab { Path = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", Pos = new Vector3(46.958f, 1.999f, 132.434f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", Pos = new Vector3(66.624f, 1.999f, 132.434f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", Pos = new Vector3(103.751f, 1.999f, 132.434f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", Pos = new Vector3(124.836f, 1.999f, 132.434f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", Pos = new Vector3(144.475f, 1.999f, 132.434f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", Pos = new Vector3(154.811f, 7.526f, 126.887f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", Pos = new Vector3(154.811f, 7.526f, 114.205f), Rot = new Vector3(0f, 90f, 0f) },
            //wall.doorway
            new Prefab { Path = "assets/prefabs/building core/wall.doorway/wall.doorway.prefab", Pos = new Vector3(34.959f, 14.358f, 110.104f), Rot = new Vector3(0f, 270f, 0f) },
            //barricade.concrete
            new Prefab { Path = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", Pos = new Vector3(37.582f, 2.232f, 70.936f), Rot = new Vector3(0f, 270f, 0f) },
            new Prefab { Path = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", Pos = new Vector3(37.582f, 2.232f, 67.406f), Rot = new Vector3(0f, 270f, 0f) },
            new Prefab { Path = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", Pos = new Vector3(34.994f, 2.249f, 64.757f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", Pos = new Vector3(32.359f, 2.232f, 67.406f), Rot = new Vector3(0f, 270f, 0f) },
            new Prefab { Path = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", Pos = new Vector3(32.359f, 2.232f, 70.936f), Rot = new Vector3(0f, 270f, 0f) },
            //ladder.wooden.wall
            new Prefab { Path = "assets/prefabs/building/ladder.wall.wood/ladder.wooden.wall.prefab", Pos = new Vector3(36.455f, 14.775f, 82.395f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/building/ladder.wall.wood/ladder.wooden.wall.prefab", Pos = new Vector3(36.455f, 11.762f, 82.395f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/building/ladder.wall.wood/ladder.wooden.wall.prefab", Pos = new Vector3(36.455f, 8.757f, 82.395f), Rot = new Vector3(0f, 0f, 0f) },
            //door.hinged.security.red
            new Prefab { Path = "assets/bundled/prefabs/static/door.hinged.security.red.prefab", Pos = new Vector3(34.958f, 14.355f, 110.095f), Rot = new Vector3(0f, 270f, 0f) },
            //button
            new Prefab { Path = "assets/prefabs/deployable/playerioents/button/button.prefab", Pos = new Vector3(36.102f, 16.272f, 84.702f), Rot = new Vector3(0f, 90f, 0f) },
            //cctv_deployed
            new Prefab { Path = "assets/prefabs/deployable/cctvcamera/cctv_deployed.prefab", Pos = new Vector3(34.981f, 16.977f, 110.049f), Rot = new Vector3(17.766f, 180f, 0f) },
            //wall.frame.netting
            new Prefab { Path = "assets/prefabs/building/wall.frame.netting/wall.frame.netting.prefab", Pos = new Vector3(34.964f, 11.361f, 110.185f), Rot = new Vector3(0f, 270f, 0f) },
            new Prefab { Path = "assets/prefabs/building/wall.frame.netting/wall.frame.netting.prefab", Pos = new Vector3(34.964f, 8.562f, 110.185f), Rot = new Vector3(0f, 270f, 0f) },
            //sam_static
            new Prefab { Path = "assets/prefabs/npc/sam_site_turret/sam_static.prefab", Pos = new Vector3(20.664f, 8.7f, 120.541f), Rot = new Vector3(0f, 270f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/sam_site_turret/sam_static.prefab", Pos = new Vector3(137.663f, 26.7f, 126.55f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/sam_site_turret/sam_static.prefab", Pos = new Vector3(137.648f, 26.7f, 114.545f), Rot = new Vector3(0f, 180f, 0f) },
            //autoturret_deployed
            new Prefab { Path = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab", Pos = new Vector3(46.955f, 2.1f, 133.746f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab", Pos = new Vector3(66.621f, 2.1f, 133.746f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab", Pos = new Vector3(103.748f, 2.1f, 133.746f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab", Pos = new Vector3(124.833f, 2.1f, 133.746f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab", Pos = new Vector3(144.472f, 2.1f, 133.746f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab", Pos = new Vector3(156.124f, 7.627f, 126.890f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab", Pos = new Vector3(156.124f, 7.627f, 114.208f), Rot = new Vector3(0f, 90f, 0f) }
        };
        internal HashSet<Prefab> PrefabsHarborLarge { get; } = new HashSet<Prefab>
        {
            //wall
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(115.511f, 20.266f, -31.090f), Rot = new Vector3(0f, 0f, 90f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(115.511f, 20.266f, -28.090f), Rot = new Vector3(0f, 0f, 90f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(115.511f, 20.266f, -25.090f), Rot = new Vector3(0f, 0f, 90f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(112.511f, 17.266f, -31.090f), Rot = new Vector3(0f, 0f, 270f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(112.511f, 17.266f, -28.090f), Rot = new Vector3(0f, 0f, 270f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(112.511f, 17.266f, -25.090f), Rot = new Vector3(0f, 0f, 270f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(115.511f, 17.266f, -25.090f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(115.511f, 17.266f, -28.090f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(115.511f, 17.266f, -31.090f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(112.511f, 17.266f, -31.090f), Rot = new Vector3(0f, 180f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(112.511f, 17.266f, -28.090f), Rot = new Vector3(0f, 180f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(112.511f, 17.266f, -25.090f), Rot = new Vector3(0f, 180f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/wall/wall.prefab", Pos = new Vector3(114.011f, 17.266f, -23.590f), Rot = new Vector3(0f, 270f, 0f) },
            //floor.triangle
            new Prefab { Path = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", Pos = new Vector3(127.462f, 1.999f, -12.477f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", Pos = new Vector3(127.462f, 1.999f, -32.223f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", Pos = new Vector3(127.462f, 1.999f, -69.428f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", Pos = new Vector3(127.462f, 1.999f, -90.086f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", Pos = new Vector3(127.462f, 1.999f, -109.74f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", Pos = new Vector3(121.838f, 7.528f, -120.257f), Rot = new Vector3(0f, 180f, 0f) },
            new Prefab { Path = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab", Pos = new Vector3(109.133f, 7.528f, -120.257f), Rot = new Vector3(0f, 180f, 0f) },
            //wall.doorway
            new Prefab { Path = "assets/prefabs/building core/wall.doorway/wall.doorway.prefab", Pos = new Vector3(114.023f, 17.240f, -32.587f), Rot = new Vector3(0f, 90f, 0f) },
            //barricade.concrete
            new Prefab { Path = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", Pos = new Vector3(78.970f, 4.983f, -31.049f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", Pos = new Vector3(75.440f, 4.983f, -31.049f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", Pos = new Vector3(72.791f, 5f, -28.461f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", Pos = new Vector3(75.440f, 4.983f, -25.826f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", Pos = new Vector3(78.970f, 4.983f, -25.826f), Rot = new Vector3(0f, 0f, 0f) },
            //ladder.wooden.wall
            new Prefab { Path = "assets/prefabs/building/ladder.wall.wood/ladder.wooden.wall.prefab", Pos = new Vector3(90.542f, 11.658f, -28.498f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/building/ladder.wall.wood/ladder.wooden.wall.prefab", Pos = new Vector3(90.542f, 14.602f, -28.498f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/building/ladder.wall.wood/ladder.wooden.wall.prefab", Pos = new Vector3(90.542f, 17.590f, -28.498f), Rot = new Vector3(0f, 90f, 0f) },
            //door.hinged.security.red
            new Prefab { Path = "assets/bundled/prefabs/static/door.hinged.security.red.prefab", Pos = new Vector3(114.024f, 17.237f, -32.578f), Rot = new Vector3(0f, 90f, 0f) },
            //button
            new Prefab { Path = "assets/prefabs/deployable/playerioents/button/button.prefab", Pos = new Vector3(92.911f, 18.997f, -28.235f), Rot = new Vector3(0f, 180f, 0f) },
            //cctv_deployed
            new Prefab { Path = "assets/prefabs/deployable/cctvcamera/cctv_deployed.prefab", Pos = new Vector3(114.008f, 19.979f, -32.538f), Rot = new Vector3(22.497f, 0f, 0f) },
            ////wall.frame.netting
            new Prefab { Path = "assets/prefabs/building/wall.frame.netting/wall.frame.netting.prefab", Pos = new Vector3(114.007f, 14.268f, -32.668f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/building/wall.frame.netting/wall.frame.netting.prefab", Pos = new Vector3(114.007f, 11.469f, -32.668f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/building/wall.frame.netting/wall.frame.netting.prefab", Pos = new Vector3(114.007f, 8.670f, -32.668f), Rot = new Vector3(0f, 90f, 0f) },
            //sam_static
            new Prefab { Path = "assets/prefabs/npc/sam_site_turret/sam_static.prefab", Pos = new Vector3(115.486f, 8.45f, 16.882f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/sam_site_turret/sam_static.prefab", Pos = new Vector3(121.478f, 26.45f, -103.129f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/sam_site_turret/sam_static.prefab", Pos = new Vector3(109.478f, 26.45f, -103.112f), Rot = new Vector3(0f, 270f, 0f) },
            //autoturret_deployed
            new Prefab { Path = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab", Pos = new Vector3(128.768f, 2.1f, -12.491f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab", Pos = new Vector3(128.768f, 2.1f, -32.237f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab", Pos = new Vector3(128.768f, 2.1f, -69.442f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab", Pos = new Vector3(128.768f, 2.1f, -90.1f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab", Pos = new Vector3(128.768f, 2.1f, -109.754f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab", Pos = new Vector3(121.824f, 7.628f, -121.562f), Rot = new Vector3(0f, 180f, 0f) },
            new Prefab { Path = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab", Pos = new Vector3(109.119f, 7.628f, -121.562f), Rot = new Vector3(0f, 180f, 0f) }
        };
        internal HashSet<Prefab> CratesHarborSmall { get; } = new HashSet<Prefab>
        {
            new Prefab { Path = "assets/bundled/prefabs/radtown/crate_elite.prefab", Pos = new Vector3(34.027f, 14.465f, 104.138f), Rot = new Vector3(0f, 270f, 0f) },
            new Prefab { Path = "assets/bundled/prefabs/radtown/crate_elite.prefab", Pos = new Vector3(33.977f, 14.465f, 107.138f), Rot = new Vector3(0f, 270f, 0f) },
            new Prefab { Path = "assets/bundled/prefabs/radtown/crate_normal.prefab", Pos = new Vector3(33.989f, 15.096f, 104.130f), Rot = new Vector3(0f, 270f, 0f) },
            new Prefab { Path = "assets/bundled/prefabs/radtown/crate_normal.prefab", Pos = new Vector3(33.989f, 15.096f, 107.130f), Rot = new Vector3(0f, 270f, 0f) },
            new Prefab { Path = "assets/bundled/prefabs/radtown/dmloot/dm c4.prefab", Pos = new Vector3(35.884f, 14.443f, 104.104f), Rot = new Vector3(0f, 180f, 0f) },
            new Prefab { Path = "assets/bundled/prefabs/radtown/dmloot/dm ammo.prefab", Pos = new Vector3(35.884f, 14.443f, 107.105f), Rot = new Vector3(0f, 180f, 0f) },
            new Prefab { Path = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab", Pos = new Vector3(34.941f, 14.440f, 101.856f), Rot = new Vector3(0f, 0f, 0f) }
        };
        internal HashSet<Prefab> CratesHarborLarge { get; } = new HashSet<Prefab>
        {
            new Prefab { Path = "assets/bundled/prefabs/radtown/crate_elite.prefab", Pos = new Vector3(114.944f, 17.373f, -26.621f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/bundled/prefabs/radtown/crate_elite.prefab", Pos = new Vector3(114.944f, 17.373f, -29.621f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/bundled/prefabs/radtown/crate_normal.prefab", Pos = new Vector3(114.982f, 18.004f, -26.613f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/bundled/prefabs/radtown/crate_normal.prefab", Pos = new Vector3(114.982f, 18.004f, -29.613f), Rot = new Vector3(0f, 90f, 0f) },
            new Prefab { Path = "assets/bundled/prefabs/radtown/dmloot/dm c4.prefab", Pos = new Vector3(113.087f, 17.351f, -26.587f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/bundled/prefabs/radtown/dmloot/dm ammo.prefab", Pos = new Vector3(113.087f, 17.351f, -29.589f), Rot = new Vector3(0f, 0f, 0f) },
            new Prefab { Path = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab", Pos = new Vector3(114.030f, 17.348f, -24.339f), Rot = new Vector3(0f, 180f, 0f) }
        };

        internal HashSet<Vector3> Marker { get; } = new HashSet<Vector3>
        {
            new Vector3(48f, 0f, 8f),
            new Vector3(48f, 0f, 6f),
            new Vector3(48f, 0f, 4f),
            new Vector3(48f, 0f, 2f),
            new Vector3(48f, 0f, 0f),
            new Vector3(48f, 0f, -2f),
            new Vector3(48f, 0f, -4f),
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
            new Vector3(44f, 0f, 22f),
            new Vector3(44f, 0f, 20f),
            new Vector3(44f, 0f, 18f),
            new Vector3(44f, 0f, 16f),
            new Vector3(44f, 0f, 14f),
            new Vector3(44f, 0f, 12f),
            new Vector3(44f, 0f, 10f),
            new Vector3(44f, 0f, -4f),
            new Vector3(44f, 0f, -6f),
            new Vector3(44f, 0f, -8f),
            new Vector3(44f, 0f, -10f),
            new Vector3(44f, 0f, -12f),
            new Vector3(44f, 0f, -14f),
            new Vector3(44f, 0f, -16f),
            new Vector3(42f, 0f, 26f),
            new Vector3(42f, 0f, 24f),
            new Vector3(42f, 0f, 22f),
            new Vector3(42f, 0f, 20f),
            new Vector3(42f, 0f, 18f),
            new Vector3(42f, 0f, -14f),
            new Vector3(42f, 0f, -16f),
            new Vector3(42f, 0f, -18f),
            new Vector3(42f, 0f, -20f),
            new Vector3(40f, 0f, 30f),
            new Vector3(40f, 0f, 28f),
            new Vector3(40f, 0f, 26f),
            new Vector3(40f, 0f, 24f),
            new Vector3(40f, 0f, -18f),
            new Vector3(40f, 0f, -20f),
            new Vector3(40f, 0f, -22f),
            new Vector3(40f, 0f, -24f),
            new Vector3(38f, 0f, 32f),
            new Vector3(38f, 0f, 30f),
            new Vector3(38f, 0f, 28f),
            new Vector3(38f, 0f, -22f),
            new Vector3(38f, 0f, -24f),
            new Vector3(38f, 0f, -26f),
            new Vector3(36f, 0f, 34f),
            new Vector3(36f, 0f, 32f),
            new Vector3(36f, 0f, 30f),
            new Vector3(36f, 0f, -24f),
            new Vector3(36f, 0f, -26f),
            new Vector3(36f, 0f, -28f),
            new Vector3(34f, 0f, 36f),
            new Vector3(34f, 0f, 34f),
            new Vector3(34f, 0f, 32f),
            new Vector3(34f, 0f, -28f),
            new Vector3(34f, 0f, -30f),
            new Vector3(32f, 0f, 38f),
            new Vector3(32f, 0f, 36f),
            new Vector3(32f, 0f, 34f),
            new Vector3(32f, 0f, -30f),
            new Vector3(32f, 0f, -32f),
            new Vector3(30f, 0f, 40f),
            new Vector3(30f, 0f, 38f),
            new Vector3(30f, 0f, 36f),
            new Vector3(30f, 0f, -6f),
            new Vector3(30f, 0f, -8f),
            new Vector3(30f, 0f, -10f),
            new Vector3(30f, 0f, -12f),
            new Vector3(30f, 0f, -14f),
            new Vector3(30f, 0f, -32f),
            new Vector3(30f, 0f, -34f),
            new Vector3(28f, 0f, 42f),
            new Vector3(28f, 0f, 40f),
            new Vector3(28f, 0f, 38f),
            new Vector3(28f, 0f, 2f),
            new Vector3(28f, 0f, 0f),
            new Vector3(28f, 0f, -2f),
            new Vector3(28f, 0f, -4f),
            new Vector3(28f, 0f, -6f),
            new Vector3(28f, 0f, -8f),
            new Vector3(28f, 0f, -10f),
            new Vector3(28f, 0f, -12f),
            new Vector3(28f, 0f, -34f),
            new Vector3(28f, 0f, -36f),
            new Vector3(26f, 0f, 42f),
            new Vector3(26f, 0f, 40f),
            new Vector3(26f, 0f, 2f),
            new Vector3(26f, 0f, 0f),
            new Vector3(26f, 0f, -2f),
            new Vector3(26f, 0f, -4f),
            new Vector3(26f, 0f, -6f),
            new Vector3(26f, 0f, -8f),
            new Vector3(26f, 0f, -10f),
            new Vector3(26f, 0f, -34f),
            new Vector3(26f, 0f, -36f),
            new Vector3(26f, 0f, -38f),
            new Vector3(24f, 0f, 44f),
            new Vector3(24f, 0f, 42f),
            new Vector3(24f, 0f, 0f),
            new Vector3(24f, 0f, -2f),
            new Vector3(24f, 0f, -4f),
            new Vector3(24f, 0f, -6f),
            new Vector3(24f, 0f, -8f),
            new Vector3(24f, 0f, -10f),
            new Vector3(24f, 0f, -12f),
            new Vector3(24f, 0f, -36f),
            new Vector3(24f, 0f, -38f),
            new Vector3(22f, 0f, 44f),
            new Vector3(22f, 0f, 42f),
            new Vector3(22f, 0f, 0f),
            new Vector3(22f, 0f, -2f),
            new Vector3(22f, 0f, -4f),
            new Vector3(22f, 0f, -6f),
            new Vector3(22f, 0f, -8f),
            new Vector3(22f, 0f, -10f),
            new Vector3(22f, 0f, -12f),
            new Vector3(22f, 0f, -14f),
            new Vector3(22f, 0f, -16f),
            new Vector3(22f, 0f, -38f),
            new Vector3(22f, 0f, -40f),
            new Vector3(20f, 0f, 46f),
            new Vector3(20f, 0f, 44f),
            new Vector3(20f, 0f, 0f),
            new Vector3(20f, 0f, -2f),
            new Vector3(20f, 0f, -4f),
            new Vector3(20f, 0f, -6f),
            new Vector3(20f, 0f, -8f),
            new Vector3(20f, 0f, -10f),
            new Vector3(20f, 0f, -12f),
            new Vector3(20f, 0f, -14f),
            new Vector3(20f, 0f, -16f),
            new Vector3(20f, 0f, -18f),
            new Vector3(20f, 0f, -38f),
            new Vector3(20f, 0f, -40f),
            new Vector3(18f, 0f, 46f),
            new Vector3(18f, 0f, 44f),
            new Vector3(18f, 0f, -2f),
            new Vector3(18f, 0f, -4f),
            new Vector3(18f, 0f, -6f),
            new Vector3(18f, 0f, -12f),
            new Vector3(18f, 0f, -14f),
            new Vector3(18f, 0f, -16f),
            new Vector3(18f, 0f, -18f),
            new Vector3(18f, 0f, -20f),
            new Vector3(18f, 0f, -40f),
            new Vector3(18f, 0f, -42f),
            new Vector3(16f, 0f, 48f),
            new Vector3(16f, 0f, 46f),
            new Vector3(16f, 0f, -2f),
            new Vector3(16f, 0f, -4f),
            new Vector3(16f, 0f, -6f),
            new Vector3(16f, 0f, -14f),
            new Vector3(16f, 0f, -16f),
            new Vector3(16f, 0f, -18f),
            new Vector3(16f, 0f, -20f),
            new Vector3(16f, 0f, -40f),
            new Vector3(16f, 0f, -42f),
            new Vector3(14f, 0f, 48f),
            new Vector3(14f, 0f, 46f),
            new Vector3(14f, 0f, -4f),
            new Vector3(14f, 0f, -16f),
            new Vector3(14f, 0f, -18f),
            new Vector3(14f, 0f, -20f),
            new Vector3(14f, 0f, -22f),
            new Vector3(14f, 0f, -40f),
            new Vector3(14f, 0f, -42f),
            new Vector3(12f, 0f, 48f),
            new Vector3(12f, 0f, 46f),
            new Vector3(12f, 0f, 12f),
            new Vector3(12f, 0f, 10f),
            new Vector3(12f, 0f, 8f),
            new Vector3(12f, 0f, -18f),
            new Vector3(12f, 0f, -20f),
            new Vector3(12f, 0f, -22f),
            new Vector3(12f, 0f, -42f),
            new Vector3(12f, 0f, -44f),
            new Vector3(10f, 0f, 50f),
            new Vector3(10f, 0f, 48f),
            new Vector3(10f, 0f, 12f),
            new Vector3(10f, 0f, 10f),
            new Vector3(10f, 0f, 8f),
            new Vector3(10f, 0f, -18f),
            new Vector3(10f, 0f, -20f),
            new Vector3(10f, 0f, -22f),
            new Vector3(10f, 0f, -24f),
            new Vector3(10f, 0f, -42f),
            new Vector3(10f, 0f, -44f),
            new Vector3(8f, 0f, 50f),
            new Vector3(8f, 0f, 48f),
            new Vector3(8f, 0f, 12f),
            new Vector3(8f, 0f, 10f),
            new Vector3(8f, 0f, 8f),
            new Vector3(8f, 0f, -20f),
            new Vector3(8f, 0f, -22f),
            new Vector3(8f, 0f, -24f),
            new Vector3(8f, 0f, -42f),
            new Vector3(8f, 0f, -44f),
            new Vector3(6f, 0f, 50f),
            new Vector3(6f, 0f, 48f),
            new Vector3(6f, 0f, 32f),
            new Vector3(6f, 0f, 30f),
            new Vector3(6f, 0f, 28f),
            new Vector3(6f, 0f, 26f),
            new Vector3(6f, 0f, 24f),
            new Vector3(6f, 0f, 12f),
            new Vector3(6f, 0f, 10f),
            new Vector3(6f, 0f, 8f),
            new Vector3(6f, 0f, -20f),
            new Vector3(6f, 0f, -22f),
            new Vector3(6f, 0f, -24f),
            new Vector3(6f, 0f, -42f),
            new Vector3(6f, 0f, -44f),
            new Vector3(4f, 0f, 50f),
            new Vector3(4f, 0f, 48f),
            new Vector3(4f, 0f, 34f),
            new Vector3(4f, 0f, 32f),
            new Vector3(4f, 0f, 26f),
            new Vector3(4f, 0f, 24f),
            new Vector3(4f, 0f, 22f),
            new Vector3(4f, 0f, 12f),
            new Vector3(4f, 0f, 10f),
            new Vector3(4f, 0f, 8f),
            new Vector3(4f, 0f, -20f),
            new Vector3(4f, 0f, -22f),
            new Vector3(4f, 0f, -24f),
            new Vector3(4f, 0f, -42f),
            new Vector3(4f, 0f, -44f),
            new Vector3(2f, 0f, 50f),
            new Vector3(2f, 0f, 48f),
            new Vector3(2f, 0f, 36f),
            new Vector3(2f, 0f, 34f),
            new Vector3(2f, 0f, 24f),
            new Vector3(2f, 0f, 22f),
            new Vector3(2f, 0f, 20f),
            new Vector3(2f, 0f, 18f),
            new Vector3(2f, 0f, 16f),
            new Vector3(2f, 0f, 14f),
            new Vector3(2f, 0f, 12f),
            new Vector3(2f, 0f, 10f),
            new Vector3(2f, 0f, 8f),
            new Vector3(2f, 0f, 6f),
            new Vector3(2f, 0f, 4f),
            new Vector3(2f, 0f, 2f),
            new Vector3(2f, 0f, 0f),
            new Vector3(2f, 0f, -2f),
            new Vector3(2f, 0f, -4f),
            new Vector3(2f, 0f, -6f),
            new Vector3(2f, 0f, -8f),
            new Vector3(2f, 0f, -10f),
            new Vector3(2f, 0f, -12f),
            new Vector3(2f, 0f, -14f),
            new Vector3(2f, 0f, -16f),
            new Vector3(2f, 0f, -18f),
            new Vector3(2f, 0f, -20f),
            new Vector3(2f, 0f, -22f),
            new Vector3(2f, 0f, -24f),
            new Vector3(2f, 0f, -26f),
            new Vector3(2f, 0f, -28f),
            new Vector3(2f, 0f, -44f),
            new Vector3(0f, 0f, 50f),
            new Vector3(0f, 0f, 48f),
            new Vector3(0f, 0f, 36f),
            new Vector3(0f, 0f, 34f),
            new Vector3(0f, 0f, 24f),
            new Vector3(0f, 0f, 22f),
            new Vector3(0f, 0f, 20f),
            new Vector3(0f, 0f, 18f),
            new Vector3(0f, 0f, 16f),
            new Vector3(0f, 0f, 14f),
            new Vector3(0f, 0f, 12f),
            new Vector3(0f, 0f, 10f),
            new Vector3(0f, 0f, 8f),
            new Vector3(0f, 0f, 6f),
            new Vector3(0f, 0f, 4f),
            new Vector3(0f, 0f, 2f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, -2f),
            new Vector3(0f, 0f, -4f),
            new Vector3(0f, 0f, -6f),
            new Vector3(0f, 0f, -8f),
            new Vector3(0f, 0f, -10f),
            new Vector3(0f, 0f, -12f),
            new Vector3(0f, 0f, -14f),
            new Vector3(0f, 0f, -16f),
            new Vector3(0f, 0f, -18f),
            new Vector3(0f, 0f, -20f),
            new Vector3(0f, 0f, -22f),
            new Vector3(0f, 0f, -24f),
            new Vector3(0f, 0f, -26f),
            new Vector3(0f, 0f, -28f),
            new Vector3(0f, 0f, -30f),
            new Vector3(0f, 0f, -44f),
            new Vector3(-2f, 0f, 50f),
            new Vector3(-2f, 0f, 48f),
            new Vector3(-2f, 0f, 36f),
            new Vector3(-2f, 0f, 34f),
            new Vector3(-2f, 0f, 24f),
            new Vector3(-2f, 0f, 22f),
            new Vector3(-2f, 0f, 20f),
            new Vector3(-2f, 0f, 18f),
            new Vector3(-2f, 0f, 16f),
            new Vector3(-2f, 0f, 14f),
            new Vector3(-2f, 0f, 12f),
            new Vector3(-2f, 0f, 10f),
            new Vector3(-2f, 0f, 8f),
            new Vector3(-2f, 0f, 6f),
            new Vector3(-2f, 0f, 4f),
            new Vector3(-2f, 0f, 2f),
            new Vector3(-2f, 0f, 0f),
            new Vector3(-2f, 0f, -2f),
            new Vector3(-2f, 0f, -4f),
            new Vector3(-2f, 0f, -6f),
            new Vector3(-2f, 0f, -8f),
            new Vector3(-2f, 0f, -10f),
            new Vector3(-2f, 0f, -12f),
            new Vector3(-2f, 0f, -14f),
            new Vector3(-2f, 0f, -16f),
            new Vector3(-2f, 0f, -18f),
            new Vector3(-2f, 0f, -20f),
            new Vector3(-2f, 0f, -22f),
            new Vector3(-2f, 0f, -24f),
            new Vector3(-2f, 0f, -26f),
            new Vector3(-2f, 0f, -28f),
            new Vector3(-2f, 0f, -44f),
            new Vector3(-4f, 0f, 50f),
            new Vector3(-4f, 0f, 48f),
            new Vector3(-4f, 0f, 34f),
            new Vector3(-4f, 0f, 32f),
            new Vector3(-4f, 0f, 30f),
            new Vector3(-4f, 0f, 26f),
            new Vector3(-4f, 0f, 24f),
            new Vector3(-4f, 0f, 12f),
            new Vector3(-4f, 0f, 10f),
            new Vector3(-4f, 0f, 8f),
            new Vector3(-4f, 0f, -20f),
            new Vector3(-4f, 0f, -22f),
            new Vector3(-4f, 0f, -24f),
            new Vector3(-4f, 0f, -42f),
            new Vector3(-4f, 0f, -44f),
            new Vector3(-6f, 0f, 50f),
            new Vector3(-6f, 0f, 48f),
            new Vector3(-6f, 0f, 32f),
            new Vector3(-6f, 0f, 30f),
            new Vector3(-6f, 0f, 28f),
            new Vector3(-6f, 0f, 26f),
            new Vector3(-6f, 0f, 12f),
            new Vector3(-6f, 0f, 10f),
            new Vector3(-6f, 0f, 8f),
            new Vector3(-6f, 0f, -20f),
            new Vector3(-6f, 0f, -22f),
            new Vector3(-6f, 0f, -24f),
            new Vector3(-6f, 0f, -42f),
            new Vector3(-6f, 0f, -44f),
            new Vector3(-8f, 0f, 50f),
            new Vector3(-8f, 0f, 48f),
            new Vector3(-8f, 0f, 12f),
            new Vector3(-8f, 0f, 10f),
            new Vector3(-8f, 0f, 8f),
            new Vector3(-8f, 0f, -20f),
            new Vector3(-8f, 0f, -22f),
            new Vector3(-8f, 0f, -24f),
            new Vector3(-8f, 0f, -42f),
            new Vector3(-8f, 0f, -44f),
            new Vector3(-10f, 0f, 50f),
            new Vector3(-10f, 0f, 48f),
            new Vector3(-10f, 0f, 12f),
            new Vector3(-10f, 0f, 10f),
            new Vector3(-10f, 0f, 8f),
            new Vector3(-10f, 0f, -18f),
            new Vector3(-10f, 0f, -20f),
            new Vector3(-10f, 0f, -22f),
            new Vector3(-10f, 0f, -24f),
            new Vector3(-10f, 0f, -42f),
            new Vector3(-10f, 0f, -44f),
            new Vector3(-12f, 0f, 48f),
            new Vector3(-12f, 0f, 46f),
            new Vector3(-12f, 0f, 10f),
            new Vector3(-12f, 0f, 8f),
            new Vector3(-12f, 0f, -4f),
            new Vector3(-12f, 0f, -18f),
            new Vector3(-12f, 0f, -20f),
            new Vector3(-12f, 0f, -22f),
            new Vector3(-12f, 0f, -42f),
            new Vector3(-12f, 0f, -44f),
            new Vector3(-14f, 0f, 48f),
            new Vector3(-14f, 0f, 46f),
            new Vector3(-14f, 0f, -4f),
            new Vector3(-14f, 0f, -6f),
            new Vector3(-14f, 0f, -16f),
            new Vector3(-14f, 0f, -18f),
            new Vector3(-14f, 0f, -20f),
            new Vector3(-14f, 0f, -22f),
            new Vector3(-14f, 0f, -42f),
            new Vector3(-16f, 0f, 48f),
            new Vector3(-16f, 0f, 46f),
            new Vector3(-16f, 0f, -2f),
            new Vector3(-16f, 0f, -4f),
            new Vector3(-16f, 0f, -6f),
            new Vector3(-16f, 0f, -14f),
            new Vector3(-16f, 0f, -16f),
            new Vector3(-16f, 0f, -18f),
            new Vector3(-16f, 0f, -20f),
            new Vector3(-16f, 0f, -40f),
            new Vector3(-16f, 0f, -42f),
            new Vector3(-18f, 0f, 46f),
            new Vector3(-18f, 0f, 44f),
            new Vector3(-18f, 0f, -2f),
            new Vector3(-18f, 0f, -4f),
            new Vector3(-18f, 0f, -6f),
            new Vector3(-18f, 0f, -8f),
            new Vector3(-18f, 0f, -12f),
            new Vector3(-18f, 0f, -14f),
            new Vector3(-18f, 0f, -16f),
            new Vector3(-18f, 0f, -18f),
            new Vector3(-18f, 0f, -40f),
            new Vector3(-18f, 0f, -42f),
            new Vector3(-20f, 0f, 46f),
            new Vector3(-20f, 0f, 44f),
            new Vector3(-20f, 0f, 0f),
            new Vector3(-20f, 0f, -2f),
            new Vector3(-20f, 0f, -4f),
            new Vector3(-20f, 0f, -6f),
            new Vector3(-20f, 0f, -8f),
            new Vector3(-20f, 0f, -10f),
            new Vector3(-20f, 0f, -12f),
            new Vector3(-20f, 0f, -14f),
            new Vector3(-20f, 0f, -16f),
            new Vector3(-20f, 0f, -38f),
            new Vector3(-20f, 0f, -40f),
            new Vector3(-22f, 0f, 46f),
            new Vector3(-22f, 0f, 44f),
            new Vector3(-22f, 0f, 42f),
            new Vector3(-22f, 0f, 0f),
            new Vector3(-22f, 0f, -2f),
            new Vector3(-22f, 0f, -4f),
            new Vector3(-22f, 0f, -6f),
            new Vector3(-22f, 0f, -8f),
            new Vector3(-22f, 0f, -10f),
            new Vector3(-22f, 0f, -12f),
            new Vector3(-22f, 0f, -14f),
            new Vector3(-22f, 0f, -38f),
            new Vector3(-22f, 0f, -40f),
            new Vector3(-24f, 0f, 44f),
            new Vector3(-24f, 0f, 42f),
            new Vector3(-24f, 0f, 2f),
            new Vector3(-24f, 0f, 0f),
            new Vector3(-24f, 0f, -2f),
            new Vector3(-24f, 0f, -4f),
            new Vector3(-24f, 0f, -6f),
            new Vector3(-24f, 0f, -8f),
            new Vector3(-24f, 0f, -10f),
            new Vector3(-24f, 0f, -12f),
            new Vector3(-24f, 0f, -36f),
            new Vector3(-24f, 0f, -38f),
            new Vector3(-26f, 0f, 42f),
            new Vector3(-26f, 0f, 40f),
            new Vector3(-26f, 0f, 2f),
            new Vector3(-26f, 0f, 0f),
            new Vector3(-26f, 0f, -2f),
            new Vector3(-26f, 0f, -4f),
            new Vector3(-26f, 0f, -6f),
            new Vector3(-26f, 0f, -8f),
            new Vector3(-26f, 0f, -10f),
            new Vector3(-26f, 0f, -12f),
            new Vector3(-26f, 0f, -36f),
            new Vector3(-26f, 0f, -38f),
            new Vector3(-28f, 0f, 42f),
            new Vector3(-28f, 0f, 40f),
            new Vector3(-28f, 0f, 38f),
            new Vector3(-28f, 0f, 0f),
            new Vector3(-28f, 0f, -2f),
            new Vector3(-28f, 0f, -4f),
            new Vector3(-28f, 0f, -6f),
            new Vector3(-28f, 0f, -8f),
            new Vector3(-28f, 0f, -10f),
            new Vector3(-28f, 0f, -12f),
            new Vector3(-28f, 0f, -34f),
            new Vector3(-28f, 0f, -36f),
            new Vector3(-30f, 0f, 40f),
            new Vector3(-30f, 0f, 38f),
            new Vector3(-30f, 0f, -10f),
            new Vector3(-30f, 0f, -12f),
            new Vector3(-30f, 0f, -14f),
            new Vector3(-30f, 0f, -32f),
            new Vector3(-30f, 0f, -34f),
            new Vector3(-32f, 0f, 38f),
            new Vector3(-32f, 0f, 36f),
            new Vector3(-32f, 0f, -30f),
            new Vector3(-32f, 0f, -32f),
            new Vector3(-34f, 0f, 36f),
            new Vector3(-34f, 0f, 34f),
            new Vector3(-34f, 0f, -28f),
            new Vector3(-34f, 0f, -30f),
            new Vector3(-36f, 0f, 34f),
            new Vector3(-36f, 0f, 32f),
            new Vector3(-36f, 0f, 30f),
            new Vector3(-36f, 0f, -26f),
            new Vector3(-36f, 0f, -28f),
            new Vector3(-38f, 0f, 32f),
            new Vector3(-38f, 0f, 30f),
            new Vector3(-38f, 0f, 28f),
            new Vector3(-38f, 0f, -22f),
            new Vector3(-38f, 0f, -24f),
            new Vector3(-38f, 0f, -26f),
            new Vector3(-40f, 0f, 30f),
            new Vector3(-40f, 0f, 28f),
            new Vector3(-40f, 0f, 26f),
            new Vector3(-40f, 0f, 24f),
            new Vector3(-40f, 0f, -20f),
            new Vector3(-40f, 0f, -22f),
            new Vector3(-40f, 0f, -24f),
            new Vector3(-42f, 0f, 26f),
            new Vector3(-42f, 0f, 24f),
            new Vector3(-42f, 0f, 22f),
            new Vector3(-42f, 0f, 20f),
            new Vector3(-42f, 0f, -14f),
            new Vector3(-42f, 0f, -16f),
            new Vector3(-42f, 0f, -18f),
            new Vector3(-42f, 0f, -20f),
            new Vector3(-44f, 0f, 22f),
            new Vector3(-44f, 0f, 20f),
            new Vector3(-44f, 0f, 18f),
            new Vector3(-44f, 0f, 16f),
            new Vector3(-44f, 0f, 14f),
            new Vector3(-44f, 0f, -8f),
            new Vector3(-44f, 0f, -10f),
            new Vector3(-44f, 0f, -12f),
            new Vector3(-44f, 0f, -14f),
            new Vector3(-44f, 0f, -16f),
            new Vector3(-44f, 0f, -18f),
            new Vector3(-46f, 0f, 18f),
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
            new Vector3(-48f, 0f, 12f),
            new Vector3(-48f, 0f, 10f),
            new Vector3(-48f, 0f, 8f),
            new Vector3(-48f, 0f, 6f),
            new Vector3(-48f, 0f, 4f),
            new Vector3(-48f, 0f, 2f),
            new Vector3(-48f, 0f, 0f),
            new Vector3(-48f, 0f, -2f),
            new Vector3(-48f, 0f, -4f),
            new Vector3(-48f, 0f, -6)
        };

        internal HashSet<string> TrashList { get; } = new HashSet<string>
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

        private ControllerHarborEvent Controller { get; set; } = null;
        private bool Active { get; set; } = false;

        private void StartTimer()
        {
            if (!_config.EnabledTimer) return;
            timer.In(UnityEngine.Random.Range(_config.MinStartTime, _config.MaxStartTime), () =>
            {
                if (!Active) Start(null);
                else Puts("This event is active now. To finish this event (harborstop), then to start the next one");
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
                Puts("HarborEvent has begun");
                Subscribes();
                Controller = new GameObject().AddComponent<ControllerHarborEvent>();
                if (ActivePveMode) Controller.Owner = player;
                if (_config.RemoveBetterNpc && plugins.Exists("BetterNpc")) BetterNpc.Call("DestroyController", Controller.IsSmallHarbor ? "Small Harbor" : "Large Harbor");
                if (plugins.Exists("MonumentOwner")) MonumentOwner.Call("RemoveZone", Controller.Monument);
                AlertToAllPlayers("Start", _config.Prefix, PhoneController.PositionToGridCoord(Controller.transform.position), _config.Cctv);
            });
        }

        private void Finish()
        {
            Unsubscribes();
            if (ActivePveMode) PveMode.Call("EventRemovePveMode", Name, true);
            if (Controller != null)
            {
                if (plugins.Exists("MonumentOwner")) MonumentOwner.Call("CreateZone", Controller.Monument);
                if (_config.RemoveBetterNpc && plugins.Exists("BetterNpc")) BetterNpc.Call("CreateController", Controller.IsSmallHarbor ? "Small Harbor" : "Large Harbor");
                UnityEngine.Object.Destroy(Controller.gameObject);
            }
            Active = false;
            SendBalance();
            LootableCrates.Clear();
            StartHackCrates.Clear();
            AlertToAllPlayers("Finish", _config.Prefix);
            Interface.Oxide.CallHook("OnHarborEventEnd");
            Puts("HarborEvent has ended");
            StartTimer();
        }

        internal class ControllerHarborEvent : FacepunchBehaviour
        {
            private PluginConfig _config => _ins._config;

            internal MonumentInfo Monument { get; set; } = null;
            internal bool IsSmallHarbor => Monument.name.Contains("harbor_1");

            private SphereCollider SphereCollider { get; set; } = null;

            private HashSet<MapMarkerGenericRadius> Markers { get; } = new HashSet<MapMarkerGenericRadius>();
            private VendingMachineMapMarker VendingMarker { get; set; } = null;

            private Coroutine SpawnEntitiesCoroutine { get; set; } = null;
            internal bool KillEntities { get; set; } = false;
            internal HashSet<BaseEntity> Entities { get; } = new HashSet<BaseEntity>();
            internal PressButton Button { get; set; } = null;
            internal Door Door { get; set; } = null;

            private bool LeavingCargo { get; set; } = _ins._config.LeavingShip;
            internal CargoShip Cargo { get; set; } = null;
            private int CargoTime { get; set; } = 5;

            internal HashSet<AutoTurret> Turrets { get; } = new HashSet<AutoTurret>();
            internal HashSet<SamSite> SamSites { get; } = new HashSet<SamSite>();

            internal BradleyAPC Bradley { get; set; } = null;
            internal Vector3 PositionBradley { get; set; } = Vector3.zero;

            internal PatrolHelicopter Helicopter { get; set; } = null;

            internal HashSet<ScientistNPC> Scientists { get; } = new HashSet<ScientistNPC>();

            internal Dictionary<LootContainer, int> Crates { get; } = new Dictionary<LootContainer, int>();
            internal Dictionary<HackableLockedCrate, int> HackCrates { get; } = new Dictionary<HackableLockedCrate, int>();

            internal int TimeToFinish { get; set; } = _ins._config.FinishTime;

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

                SpawnMapMarker(_config.Marker);

                SpawnEntitiesCoroutine = ServerMgr.Instance.StartCoroutine(SpawnEntities());
            }

            private void OnDestroy()
            {
                if (SpawnEntitiesCoroutine != null) ServerMgr.Instance.StopCoroutine(SpawnEntitiesCoroutine);

                CancelInvoke(InvokeUpdates);

                if (SphereCollider != null) Destroy(SphereCollider);

                if (VendingMarker.IsExists()) VendingMarker.Kill();
                foreach (MapMarkerGenericRadius marker in Markers) if (marker.IsExists()) marker.Kill();

                foreach (BasePlayer player in Players) CuiHelper.DestroyUi(player, "Tabs_KpucTaJl");

                if (Helicopter.IsExists()) Helicopter.Kill();
                if (Bradley.IsExists()) Bradley.Kill();

                foreach (ScientistNPC npc in Scientists) if (npc.IsExists()) npc.Kill();

                foreach (KeyValuePair<LootContainer, int> dic in Crates) if (dic.Key.IsExists()) dic.Key.Kill();
                foreach (KeyValuePair<HackableLockedCrate, int> dic in HackCrates) if (dic.Key.IsExists()) dic.Key.Kill();

                KillEntities = true;
                foreach (BaseEntity entity in Entities) if (entity.IsExists()) entity.Kill();

                if (Cargo.IsExists())
                {
                    if (LeavingCargo)
                    {
                        Cargo.FindInitialNode();
                        Cargo.egressing = false;
                        Cargo.StartEgress();
                    }
                    else Cargo.Kill();
                }
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

            private void OnTriggerExit(Collider other) => ExitPlayer(other.GetComponentInParent<BasePlayer>());

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
                CheckMoveHelicopter();
                UpdateTimeToFinish();
            }

            private void UpdateGui(BasePlayer player)
            {
                Dictionary<string, string> dic = new Dictionary<string, string> { ["Clock_KpucTaJl"] = GetTimeFormat(TimeToFinish) };
                if (Scientists.Count > 0) dic.Add("Npc_KpucTaJl", Scientists.Count.ToString());
                if (Crates.Count + HackCrates.Count > 0) dic.Add("Crate_KpucTaJl", $"{Crates.Count + HackCrates.Count}");
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

                if (_config.MainPoint.Enabled)
                {
                    HashSet<Vector3> points = new HashSet<Vector3>();
                    if (Door.IsExists()) points.Add(Button.transform.position);
                    if (Bradley.IsExists()) points.Add(Bradley.transform.position);
                    if (points.Count > 0) foreach (BasePlayer player in Players) foreach (Vector3 point in points) UpdateMarkerForPlayer(player, point, _config.MainPoint);
                    points = null;
                }

                if (_config.AdditionalPoint.Enabled && !Door.IsExists() && !Bradley.IsExists() && Crates.Count + HackCrates.Count > 0)
                {
                    HashSet<Vector3> points = new HashSet<Vector3>();
                    foreach (KeyValuePair<LootContainer, int> dic in Crates) if (dic.Key.IsExists()) points.Add(dic.Key.transform.position);
                    foreach (KeyValuePair<HackableLockedCrate, int> dic in HackCrates) if (dic.Key.IsExists()) points.Add(dic.Key.transform.position);
                    if (points.Count > 0) foreach (BasePlayer player in Players) foreach (Vector3 point in points) UpdateMarkerForPlayer(player, point, _config.AdditionalPoint);
                    points = null;
                }
            }

            private void UpdateTimeToFinish()
            {
                if (CargoTime > 0 && Cargo.lootRoundsPassed >= CargoShip.loot_rounds) CargoTime--;

                if (CargoTime == 0 && Crates.Count + HackCrates.Count == 0 && TimeToFinish > _config.PreFinishTime) TimeToFinish = _config.PreFinishTime;
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

            private static void CheckTrash(Vector3 pos, float radius) { foreach (BaseEntity entity in GetEntities<BaseEntity>(pos, radius, -1)) if (_ins.TrashList.Contains(entity.ShortPrefabName) && entity.IsExists()) entity.Kill(); }

            private IEnumerator SpawnEntities()
            {
                Vector3 cargoPos = IsSmallHarbor ? GetGlobalPosition(new Vector3(95.651f, 0f, 120.546f)) : GetGlobalPosition(new Vector3(115.485f, 0f, -61.115f));
                Quaternion cargoRot = IsSmallHarbor ? GetGlobalRotation(new Vector3(0f, 270f, 0f)) : GetGlobalRotation(new Vector3(0f, 0f, 0f));

                Cargo = GameManager.server.CreateEntity("assets/content/vehicles/boats/cargoship/cargoshiptest.prefab", cargoPos, cargoRot) as CargoShip;
                Cargo.enableSaving = false;
                Cargo.Spawn();

                Cargo.skinID = 81182151852251420;
                Cargo.CancelInvoke(Cargo.FindInitialNode);
                Cargo.egressing = true;

                RHIB rhib = Cargo.children.FirstOrDefault(x => x.ShortPrefabName == "rhib") as RHIB;
                if (rhib != null) rhib.EnableSaving(false);

                yield return CoroutineEx.waitForSeconds(4f);

                Cargo.transform.rotation = cargoRot;
                Cargo.transform.RotateAround(cargoPos, new Vector3(0, 1, 0), 0f);

                yield return CoroutineEx.waitForSeconds(1f);

                foreach (Prefab prefab in IsSmallHarbor ? _ins.PrefabsHarborSmall : _ins.PrefabsHarborLarge)
                {
                    if (prefab.Path == "assets/prefabs/npc/sam_site_turret/sam_static.prefab" && !_config.IsSamSites) continue;
                    if (prefab.Path == "assets/prefabs/npc/autoturret/autoturret_deployed.prefab" && !_config.Turret.IsTurret) continue;
                    
                    BaseEntity entity = SpawnEntity(prefab.Path, GetGlobalPosition(prefab.Pos), GetGlobalRotation(prefab.Rot));

                    if (entity is DecayEntity && !(entity is SamSite) && !(entity is CCTV_RC)) (entity as DecayEntity).lifestate = BaseCombatEntity.LifeState.Dead;

                    if (entity is BuildingBlock)
                    {
                        BuildingBlock buildingBlock = entity as BuildingBlock;
                        buildingBlock.ChangeGradeAndSkin(BuildingGrade.Enum.Metal, 10221);
                        buildingBlock.SetCustomColour(5);
                    }

                    if (entity is Door) Door = entity as Door;

                    if (entity is PressButton) Button = entity as PressButton;

                    if (entity is CCTV_RC)
                    {
                        CCTV_RC cctv = entity as CCTV_RC;
                        cctv.UpdateFromInput(5, 0);
                        cctv.rcIdentifier = _config.Cctv;
                    }

                    if (entity is AutoTurret)
                    {
                        AutoTurret turret = entity as AutoTurret;
                        turret.inventory.Insert(ItemManager.CreateByName(_config.Turret.ShortNameWeapon));
                        turret.inventory.Insert(ItemManager.CreateByName(_config.Turret.ShortNameAmmo, _config.Turret.CountAmmo));
                        turret.SendNetworkUpdate();
                        turret.UpdateFromInput(10, 0);
                        turret.InitializeHealth(_config.Turret.Hp, _config.Turret.Hp);
                        Turrets.Add(turret);
                    }

                    if (entity is SamSite) SamSites.Add(entity as SamSite);

                    Entities.Add(entity);
                }

                SpawnCrates();

                if (_config.Bradley.IsBradley)
                {
                    PositionBradley = IsSmallHarbor ? GetGlobalPosition(new Vector3(34.947f, 2.185f, 69.194f)) : GetGlobalPosition(new Vector3(76.715f, 4.767f, -28.421f));

                    CheckTrash(PositionBradley, 10f);

                    SpawnSmoke(PositionBradley);

                    Bradley = GameManager.server.CreateEntity("assets/prefabs/npc/m2bradley/bradleyapc.prefab", PositionBradley, IsSmallHarbor ? GetGlobalRotation(new Vector3(0f, 180f, 0f)) : GetGlobalRotation(new Vector3(0f, 270f, 0f))) as BradleyAPC;
                    Bradley.enableSaving = false;
                    Bradley.Spawn();

                    Bradley.skinID = 81182151852251420;

                    Bradley.InstallPatrolPath(new BasePath());
                    Bradley.patrolPath = null;

                    Bradley._maxHealth = _config.Bradley.Hp;
                    Bradley.health = Bradley._maxHealth;

                    Bradley.maxCratesToSpawn = _config.Bradley.CountCrates;

                    Bradley.viewDistance = _config.Bradley.ViewDistance;
                    Bradley.searchRange = _config.Bradley.SearchRange;

                    Bradley.coaxAimCone *= _config.Bradley.CoaxAimCone;
                    Bradley.coaxFireRate *= _config.Bradley.CoaxFireRate;
                    Bradley.coaxBurstLength = _config.Bradley.CoaxBurstLength;

                    Bradley.nextFireTime = _config.Bradley.NextFireTime;
                    Bradley.topTurretFireRate = _config.Bradley.TopTurretFireRate;

                    Bradley.memoryDuration = _config.Bradley.MemoryDuration;
                }

                foreach (PresetConfig preset in IsSmallHarbor ? _config.NpcSmall : _config.NpcLarge) SpawnPreset(preset);

                EnablePveMode(_config.PveMode, Owner);

                InvokeRepeating(InvokeUpdates, 0f, 1f);

                Interface.Oxide.CallHook("OnHarborEventStart", transform.position, _config.Radius);
            }

            private void SpawnCrates()
            {
                foreach (Prefab prefab in IsSmallHarbor ? _ins.CratesHarborSmall : _ins.CratesHarborLarge)
                {
                    LootContainer crate = GameManager.server.CreateEntity(prefab.Path, GetGlobalPosition(prefab.Pos), GetGlobalRotation(prefab.Rot)) as LootContainer;
                    crate.enableSaving = false;
                    crate.Spawn();
                    if (crate is HackableLockedCrate) SetHackCrate(crate as HackableLockedCrate, _config.ContainerHackCrates);
                    else SetCrate(crate, _config.ContainerCrates.FirstOrDefault(x => x.Prefab == prefab.Path));
                }
            }

            internal void SpawnCargoCrate(LootContainer crate)
            {
                if (Cargo == null || crate.GetParentEntity() != Cargo) return;
                if (crate is HackableLockedCrate)
                {
                    HackableLockedCrate hackcrate = crate as HackableLockedCrate;
                    if (HackCrates.ContainsKey(hackcrate)) return;
                    SetHackCrate(hackcrate, _config.CargoHackCrates);
                }
                else
                {
                    if (Crates.ContainsKey(crate)) return;
                    SetCrate(crate, _config.CargoCrates.FirstOrDefault(x => x.Prefab == crate.PrefabName));
                }
                if (_ins.ActivePveMode) _ins.PveMode.Call("EventAddCrates", _ins.Name, new HashSet<ulong> { crate.net.ID.Value });
            }

            private void SetHackCrate(HackableLockedCrate crate, HackCrateConfig config)
            {
                HackCrates.Add(crate, config.TypeLootTable);
                crate.hackSeconds = HackableLockedCrate.requiredHackSeconds - config.UnlockTime;
                if (config.TypeLootTable == 1 || config.TypeLootTable == 4 || config.TypeLootTable == 5)
                {
                    crate.inventory.ClearItemsContainer();
                    if (config.TypeLootTable == 4 || config.TypeLootTable == 5) _ins.AddToContainerPrefab(crate.inventory, config.PrefabLootTable);
                    if (config.TypeLootTable == 1 || config.TypeLootTable == 5) _ins.AddToContainerItem(crate.inventory, config.OwnLootTable);
                }
            }

            private void SetCrate(LootContainer crate, CrateConfig config)
            {
                Crates.Add(crate, config.TypeLootTable);
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

            internal bool IsMainHackCrate(HackableLockedCrate crate) => Vector3.Distance(crate.transform.position, IsSmallHarbor ? GetGlobalPosition(new Vector3(34.941f, 14.440f, 101.856f)) : GetGlobalPosition(new Vector3(114.030f, 17.348f, -24.339f))) < 1f;

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

            internal void SpawnCargoScientist(ScientistNPC entity)
            {
                if (entity.skinID == 11162132011012 || Cargo == null || entity.GetParentEntity() != Cargo) return;

                bool isStationary = entity.ShortPrefabName == "scientistnpc_cargo_turret_lr300" || entity.ShortPrefabName == "scientistnpc_cargo_turret_any";
                NpcConfigCargo config = entity.ShortPrefabName == "scientistnpc_cargo_turret_lr300" ? _config.NpcStationaryOutsideCargo : entity.ShortPrefabName == "scientistnpc_cargo_turret_any" ? _config.NpcStationaryInsideCargo : _config.NpcMovingCargo;

                ScientistNPC npc = (ScientistNPC)_ins.NpcSpawn.Call("SpawnNpc", entity.transform.position, GetObjectConfigCargo(config, isStationary));
                Scientists.Add(npc);

                _ins.NextTick(() =>
                {
                    _ins.NpcSpawn.Call("SetParentEntity", npc, Cargo, Cargo.transform.InverseTransformPoint(entity.transform.position));
                    npc.Brain.Navigator.CanUseNavMesh = false;
                    if (!isStationary)
                    {
                        npc.Brain.Navigator.AStarGraph = entity.Brain.Navigator.AStarGraph;
                        npc.Brain.Navigator.CanUseAStar = true;
                    }
                    entity.Kill();
                });
            }

            private static JObject GetObjectConfigCargo(NpcConfigCargo config, bool isStationary)
            {
                HashSet<string> states = isStationary ? new HashSet<string> { "IdleState", "CombatStationaryState" } : new HashSet<string> { "RoamState", "ChaseState", "CombatState" };
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
                    ["CanRunAwayWater"] = false,
                    ["CanSleep"] = false,
                    ["SleepDistance"] = 100f,
                    ["Speed"] = config.Speed,
                    ["AreaMask"] = 25,
                    ["AgentTypeID"] = 0,
                    ["HomePosition"] = string.Empty,
                    ["MemoryDuration"] = config.MemoryDuration,
                    ["States"] = new JArray { states }
                };
            }

            private static void SpawnSmoke(Vector3 pos)
            {
                SmokeGrenade grenade = GameManager.server.CreateEntity("assets/prefabs/tools/smoke grenade/grenade.smoke.deployed.prefab", pos) as SmokeGrenade;
                grenade.enableSaving = false;
                grenade.Spawn();
                grenade.GetComponent<Rigidbody>().useGravity = false;
            }

            internal void SpawnHelicopter()
            {
                Helicopter = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab") as PatrolHelicopter;
                Helicopter.enableSaving = false;
                Helicopter.Spawn();

                Helicopter.skinID = 81182151852251420;

                Helicopter.startHealth = _config.Helicopter.Hp;
                Helicopter.InitializeHealth(_config.Helicopter.Hp, _config.Helicopter.Hp);
                PatrolHelicopter.weakspot[] weakspots = Helicopter.weakspots;
                weakspots[0].maxHealth = _config.Helicopter.HpMainRotor;
                weakspots[1].maxHealth = _config.Helicopter.HpTailRotor;
                weakspots[0].health = _config.Helicopter.HpMainRotor;
                weakspots[1].health = _config.Helicopter.HpTailRotor;

                Helicopter.maxCratesToSpawn = _config.Helicopter.CountCrates;

                Helicopter.myAI.timeBetweenRockets = _config.Helicopter.TimeBetweenRockets;
                Helicopter.myAI.leftGun.fireRate = Helicopter.myAI.rightGun.fireRate = _config.Helicopter.FireRate;
                Helicopter.myAI.leftGun.burstLength = Helicopter.myAI.rightGun.burstLength = _config.Helicopter.BurstLength;
                Helicopter.myAI.leftGun.timeBetweenBursts = Helicopter.myAI.rightGun.timeBetweenBursts = _config.Helicopter.TimeBetweenBursts;
                Helicopter.myAI.leftGun.maxTargetRange = Helicopter.myAI.rightGun.maxTargetRange = _config.Helicopter.MaxTargetRange;

                Helicopter.transform.position = transform.position + new Vector3(0f, 70f, 0f);

                if (_ins.ActivePveMode) _ins.PveMode.Call("EventAddHelicopters", _ins.Name, new HashSet<ulong> { Helicopter.net.ID.Value });

                GoToNewPosHelicopter();
            }

            private void CheckMoveHelicopter()
            {
                if (!Helicopter.IsExists() || Helicopter.myAI == null) return;
                if (Helicopter.myAI._currentState != PatrolHelicopterAI.aiState.PATROL) return;
                GoToNewPosHelicopter();
            }

            private void GoToNewPosHelicopter()
            {
                Vector2 vector2 = UnityEngine.Random.insideUnitCircle * 70f;
                Vector3 pos = Cargo.transform.position + new Vector3(vector2.x, 70f, vector2.y);
                Helicopter.myAI.hasInterestZone = true;
                Helicopter.myAI.interestZoneOrigin = pos;
                Helicopter.myAI.ExitCurrentState();
                Helicopter.myAI.State_Move_Enter(pos);
            }

            private void EnablePveMode(PveModeConfig config, BasePlayer player)
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
                    ["DamageTank"] = config.DamageTank,
                    ["DamageHelicopter"] = config.DamageHelicopter,
                    ["DamageTurret"] = config.DamageTurret,
                    ["TargetNpc"] = config.TargetNpc,
                    ["TargetTank"] = config.TargetTank,
                    ["TargetHelicopter"] = config.TargetHelicopter,
                    ["TargetTurret"] = config.TargetTurret,
                    ["CanEnter"] = config.CanEnter,
                    ["CanEnterCooldownPlayer"] = config.CanEnterCooldownPlayer,
                    ["TimeExitOwner"] = config.TimeExitOwner,
                    ["AlertTime"] = config.AlertTime,
                    ["RestoreUponDeath"] = config.RestoreUponDeath,
                    ["CooldownOwner"] = config.CooldownOwner,
                    ["Darkening"] = config.Darkening
                };

                HashSet<ulong> crates = Crates.Select(x => x.Key.net.ID.Value);
                foreach (KeyValuePair<HackableLockedCrate, int> dic in HackCrates) crates.Add(dic.Key.net.ID.Value);

                _ins.PveMode.Call("EventAddPveMode", _ins.Name, objectConfig, transform.position, _config.Radius, crates, Scientists.Select(x => x.net.ID.Value), _config.Bradley.IsBradley ? new HashSet<ulong> { Bradley.net.ID.Value } : new HashSet<ulong>(), new HashSet<ulong>(), new HashSet<ulong>(), player);
                _ins.PveMode.Call("EventAddTurrets", _ins.Name, Turrets.Select(x => x.net.ID.Value));
            }
        }
        #endregion Controller

        #region Find Position
        internal MonumentInfo GetMonument()
        {
            List<MonumentInfo> list = Pool.GetList<MonumentInfo>();
            foreach (MonumentInfo monument in TerrainMeta.Path.Monuments)
            {
                if ((monument.name.Contains("harbor_1") && _config.IsSmallHarbor) || 
                    (monument.name.Contains("harbor_2") && _config.IsLargeHarbor)) 
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

            if (!Controller.Scientists.Contains(entity)) return;
            Controller.Scientists.Remove(entity);

            int typeLootTable;
            PrefabLootTableConfig prefabLootTable;
            LootTableConfig ownTableConfig;
            bool isRemoveCorpse;

            if (entity.displayName == _config.NpcMovingCargo.Name)
            {
                typeLootTable = _config.NpcMovingCargo.TypeLootTable;
                prefabLootTable = _config.NpcMovingCargo.PrefabLootTable;
                ownTableConfig = _config.NpcMovingCargo.OwnLootTable;
                isRemoveCorpse = _config.NpcMovingCargo.IsRemoveCorpse;
            }
            else if (entity.displayName == _config.NpcStationaryInsideCargo.Name)
            {
                typeLootTable = _config.NpcStationaryInsideCargo.TypeLootTable;
                prefabLootTable = _config.NpcStationaryInsideCargo.PrefabLootTable;
                ownTableConfig = _config.NpcStationaryInsideCargo.OwnLootTable;
                isRemoveCorpse = _config.NpcStationaryInsideCargo.IsRemoveCorpse;
            }
            else if (entity.displayName == _config.NpcStationaryOutsideCargo.Name)
            {
                typeLootTable = _config.NpcStationaryOutsideCargo.TypeLootTable;
                prefabLootTable = _config.NpcStationaryOutsideCargo.PrefabLootTable;
                ownTableConfig = _config.NpcStationaryOutsideCargo.OwnLootTable;
                isRemoveCorpse = _config.NpcStationaryOutsideCargo.IsRemoveCorpse;
            }
            else
            {
                PresetConfig preset = Controller.IsSmallHarbor ? _config.NpcSmall.FirstOrDefault(x => x.Config.Name == entity.displayName) : _config.NpcLarge.FirstOrDefault(x => x.Config.Name == entity.displayName);
                typeLootTable = preset.TypeLootTable;
                prefabLootTable = preset.PrefabLootTable;
                ownTableConfig = preset.OwnLootTable;
                isRemoveCorpse = preset.Config.IsRemoveCorpse;
            }

            NextTick(() =>
            {
                if (corpse == null) return;
                ItemContainer container = corpse.containers[0];
                if (typeLootTable == 1 || typeLootTable == 4 || typeLootTable == 5)
                {
                    container.ClearItemsContainer();
                    if (typeLootTable == 4 || typeLootTable == 5) AddToContainerPrefab(container, prefabLootTable);
                    if (typeLootTable == 1 || typeLootTable == 5) AddToContainerItem(container, ownTableConfig);
                }
                if (isRemoveCorpse && corpse.IsExists()) corpse.Kill();
            });
        }

        private object CanPopulateLoot(ScientistNPC entity, NPCPlayerCorpse corpse)
        {
            if (entity == null || corpse == null || Controller == null) return null;
            if (!Controller.Scientists.Contains(entity)) return null;
            if (GetTypeLootTableNpc(entity) == 2) return null;
            else return true;
        }

        private object OnCustomLootNPC(NetworkableId netId)
        {
            if (Controller == null) return null;
            ScientistNPC entity = Controller.Scientists.FirstOrDefault(x => x.IsExists() && x.net.ID.Value == netId.Value);
            if (entity == null) return null;
            if (GetTypeLootTableNpc(entity) == 3) return null;
            else return true;
        }

        private int GetTypeLootTableNpc(ScientistNPC entity)
        {
            if (_config.NpcMovingCargo.Name == entity.displayName) return _config.NpcMovingCargo.TypeLootTable;
            else if (_config.NpcStationaryInsideCargo.Name == entity.displayName) return _config.NpcStationaryInsideCargo.TypeLootTable;
            else if (_config.NpcStationaryOutsideCargo.Name == entity.displayName) return _config.NpcStationaryOutsideCargo.TypeLootTable;
            else
            {
                PresetConfig preset = Controller.IsSmallHarbor ? _config.NpcSmall.FirstOrDefault(x => x.Config.Name == entity.displayName) : _config.NpcLarge.FirstOrDefault(x => x.Config.Name == entity.displayName);
                return preset.TypeLootTable;
            }
        }
        #endregion NPC

        #region Crates
        private bool IsEventBradleyCrate(LootContainer container) => container is LockedByEntCrate && container.ShortPrefabName == "bradley_crate" && Vector3.Distance(container.transform.position, Controller.PositionBradley) < 10f;

        private void OnEntitySpawned(LockedByEntCrate crate)
        {
            if (crate != null && IsEventBradleyCrate(crate) && (_config.Bradley.TypeLootTable == 1 || _config.Bradley.TypeLootTable == 4 || _config.Bradley.TypeLootTable == 5))
            {
                NextTick(() =>
                {
                    crate.inventory.ClearItemsContainer();
                    if (_config.Bradley.TypeLootTable == 4 || _config.Bradley.TypeLootTable == 5) AddToContainerPrefab(crate.inventory, _config.Bradley.PrefabLootTable);
                    if (_config.Bradley.TypeLootTable == 1 || _config.Bradley.TypeLootTable == 5) AddToContainerItem(crate.inventory, _config.Bradley.OwnLootTable);
                });
            }
        }

        private object CanPopulateLoot(LootContainer container)
        {
            if (container == null || Controller == null) return null;
            else return GetResultLoot(container, 2);
        }

        private object OnCustomLootContainer(NetworkableId netId)
        {
            if (Controller == null) return null;
            LootContainer container = BaseNetworkable.serverEntities.Find(netId) as LootContainer;
            return container == null ? null : GetResultLoot(container, 3);
        }

        private object OnContainerPopulate(LootContainer container)
        {
            if (container == null || Controller == null) return null;
            else return GetResultLoot(container, 6);
        }

        private object GetResultLoot(LootContainer container, int type)
        {
            int typeLootTable;
            if (Controller.Crates.TryGetValue(container, out typeLootTable))
            {
                if (typeLootTable == type) return null;
                else return true;
            }
            else if (container is HackableLockedCrate && Controller.HackCrates.TryGetValue(container as HackableLockedCrate, out typeLootTable))
            {
                if (typeLootTable == type) return null;
                else return true;
            }
            else if (IsEventBradleyCrate(container))
            {
                if (_config.Bradley.TypeLootTable == type) return null;
                else return true;
            }
            else return null;
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
            foreach (CrateConfig crateConfig in _config.ContainerCrates)
            {
                CheckLootTable(crateConfig.OwnLootTable);
                CheckPrefabLootTable(crateConfig.PrefabLootTable);
            }
            foreach (CrateConfig crateConfig in _config.CargoCrates)
            {
                CheckLootTable(crateConfig.OwnLootTable);
                CheckPrefabLootTable(crateConfig.PrefabLootTable);
            }

            CheckLootTable(_config.ContainerHackCrates.OwnLootTable);
            CheckPrefabLootTable(_config.ContainerHackCrates.PrefabLootTable);

            CheckLootTable(_config.CargoHackCrates.OwnLootTable);
            CheckPrefabLootTable(_config.CargoHackCrates.PrefabLootTable);

            CheckLootTable(_config.Bradley.OwnLootTable);
            CheckPrefabLootTable(_config.Bradley.PrefabLootTable);

            foreach (PresetConfig preset in _config.NpcSmall)
            {
                CheckLootTable(preset.OwnLootTable);
                CheckPrefabLootTable(preset.PrefabLootTable);
            }

            foreach (PresetConfig preset in _config.NpcLarge)
            {
                CheckLootTable(preset.OwnLootTable);
                CheckPrefabLootTable(preset.PrefabLootTable);
            }

            CheckLootTable(_config.NpcMovingCargo.OwnLootTable);
            CheckPrefabLootTable(_config.NpcMovingCargo.PrefabLootTable);

            CheckLootTable(_config.NpcStationaryInsideCargo.OwnLootTable);
            CheckPrefabLootTable(_config.NpcStationaryInsideCargo.PrefabLootTable);

            CheckLootTable(_config.NpcStationaryOutsideCargo.OwnLootTable);
            CheckPrefabLootTable(_config.NpcStationaryOutsideCargo.PrefabLootTable);

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
            if (!victim.IsPlayer() || hitinfo == null || Controller == null) return null;
            BaseEntity attacker = hitinfo.Initiator;
            if (!ActivePveMode && attacker is AutoTurret && Controller.Turrets.Contains(attacker as AutoTurret)) return true;
            else if (attacker is SamSite && Controller.SamSites.Contains(attacker as SamSite)) return true;
            else if (attacker is BasePlayer && _config.IsCreateZonePvp && Controller.Players.Contains(victim) && Controller.Players.Contains(attacker as BasePlayer)) return true;
            else return null;
        }

        private object CanEntityTakeDamage(AutoTurret victim, HitInfo hitinfo)
        {
            if (victim == null || hitinfo == null || Controller == null) return null;
            BasePlayer attacker = hitinfo.InitiatorPlayer;
            if (!ActivePveMode && attacker.IsPlayer() && Controller.Turrets.Contains(victim)) return true;
            else return null;
        }

        private object CanEntityBeTargeted(BasePlayer player, AutoTurret turret)
        {
            if (!player.IsPlayer() || turret == null || Controller == null) return null;
            if (!ActivePveMode && Controller.Turrets.Contains(turret)) return true;
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

        #region BetterNpc
        private object CanBradleySpawnNpc(BradleyAPC bradley)
        {
            if (Controller == null) return null;
            if (Vector3.Distance(bradley.transform.position, Controller.transform.position) < _config.Radius) return true;
            else return null;
        }

        private object CanHelicopterSpawnNpc(PatrolHelicopter helicopter)
        {
            if (Controller == null) return null;
            if (Vector3.Distance(helicopter.transform.position, Controller.transform.position) < _config.Radius) return true;
            else return null;
        }

        private object CanCargoShipSpawnNpc(CargoShip cargo)
        {
            if (Controller == null) return null;
            if (Vector3.Distance(cargo.transform.position, Controller.transform.position) < _config.Radius) return true;
            else return null;
        }
        #endregion BetterNpc

        #region Bradley Tiers
        private object CanBradleyTiersEdit(BradleyAPC bradley)
        {
            if (Controller == null) return null;
            if (Vector3.Distance(bradley.transform.position, Controller.transform.position) < _config.Radius) return true;
            else return null;
        }
        #endregion Bradley Tiers

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
                case "Bradley":
                    AddBalance(playerId, _config.Economy.Bradley);
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
            Interface.Oxide.CallHook("OnHarborEventWinner", winnerId);
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
        private HashSet<string> Names { get; } = new HashSet<string>
        {
            "Tab_KpucTaJl",
            "Clock_KpucTaJl",
            "Crate_KpucTaJl",
            "Npc_KpucTaJl"
        };
        private Dictionary<string, string> Images { get; } = new Dictionary<string, string>();

        private IEnumerator DownloadImages()
        {
            foreach (string name in Names)
            {
                string url = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + "Images" + Path.DirectorySeparatorChar + name + ".png";
                using (UnityWebRequest unityWebRequest = UnityWebRequestTexture.GetTexture(url))
                {
                    yield return unityWebRequest.SendWebRequest();
                    if (unityWebRequest.result != UnityWebRequest.Result.Success)
                    {
                        PrintError($"Image {name} was not found. Maybe you didn't upload it to the .../oxide/data/Images/ folder");
                        break;
                    }
                    else
                    {
                        Texture2D tex = DownloadHandlerTexture.GetContent(unityWebRequest);
                        Images.Add(name, FileStorage.server.Store(tex.EncodeToPNG(), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString());
                        Puts($"Image {name} download is complete");
                        UnityEngine.Object.DestroyImmediate(tex);
                    }
                }
            }
            if (Images.Count < Names.Count) Interface.Oxide.UnloadPlugin(Name);
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

            foreach (KeyValuePair<string, string> dic in tabs)
            {
                i++;
                float xmin = 109f * (i - 1);
                container.Add(new CuiElement
                {
                    Name = $"Tab_{i}_KpucTaJl",
                    Parent = "Tabs_KpucTaJl",
                    Components =
                    {
                        new CuiRawImageComponent { Png = Images["Tab_KpucTaJl"] },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"{xmin} 0", OffsetMax = $"{xmin + 105f} 20" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = $"Tab_{i}_KpucTaJl",
                    Components =
                    {
                        new CuiRawImageComponent { Png = Images[dic.Key] },
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
            "CanBuild",
            "CanChangeGrade",
            "OnStructureRotate",
            "OnButtonPress",
            "OnPlayerConnected",
            "OnPlayerDeath",
            "OnEntityDeath",
            "CanBradleyApcTarget",
            "OnEntityKill",
            "CanHackCrate",
            "OnCrateHack",
            "OnTurretTarget",
            "OnLootEntity",
            "OnPlayerCommand",
            "OnServerCommand",
            "OnCorpsePopulate",
            "CanPopulateLoot",
            "OnCustomLootNPC",
            "OnEntitySpawned",
            "OnCustomLootContainer",
            "OnContainerPopulate",
            "SetOwnerPveMode",
            "ClearOwnerPveMode",
            "CanEntityTakeDamage",
            "CanEntityBeTargeted",
            "CanTeleport",
            "OnPlayerTeleported",
            "CanBradleySpawnNpc",
            "CanHelicopterSpawnNpc",
            "CanCargoShipSpawnNpc",
            "CanBradleyTiersEdit"
        };

        private void Unsubscribes() { foreach (string hook in _hooks) Unsubscribe(hook); }

        private void Subscribes() { foreach (string hook in _hooks) Subscribe(hook); }

        private const string StrSec = En ? "sec." : "сек.";
        private const string StrMin = En ? "min." : "мин.";
        private const string StrH = En ? "h." : "ч.";

        private static string GetTimeFormat(int time)
        {
            if (time <= 60) return $"{time} {StrSec}";
            else if (time <= 3600)
            {
                int sec = time % 60;
                int min = (time - sec) / 60;
                return sec == 0 ? $"{min} {StrMin}" : $"{min} {StrMin} {sec} {StrSec}";
            }
            else
            {
                int minSec = time % 3600;
                int hour = (time - minSec) / 3600;
                int sec = minSec % 60;
                int min = (minSec - sec) / 60;
                if (min == 0 && sec == 0) return $"{hour} {StrH}";
                else if (sec == 0) return $"{hour} {StrH} {min} {StrMin}";
                else return $"{hour} {StrH} {min} {StrMin} {sec} {StrSec}";
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
            webrequest.Enqueue("http://37.153.157.216:5000/Api/GetPluginVersions?pluginName=HarborEvent", null, (code, response) =>
            {
                if (code != 200 || string.IsNullOrEmpty(response)) return;
                string[] array = response.Replace("\"", string.Empty).Split('.');
                VersionNumber latestVersion = new VersionNumber(Convert.ToInt32(array[0]), Convert.ToInt32(array[1]), Convert.ToInt32(array[2]));
                if (Version < latestVersion) PrintWarning($"A new version ({latestVersion}) of the plugin is available! You need to update the plugin:\n- https://lone.design/product/harbor-event-rust-plugin\n- https://codefling.com/plugins/harbor-event");
            }, this);
        }
        #endregion Helpers

        #region Commands
        [ChatCommand("harborstart")]
        private void ChatStartEvent(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                if (!Active) Start(null);
                else PrintToChat(player, GetMessage("EventActive", player.UserIDString, _config.Prefix));
            }
        }

        [ChatCommand("harborstop")]
        private void ChatStopEvent(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                if (Controller != null) Finish();
                else Interface.Oxide.ReloadPlugin(Name);
            }
        }

        [ChatCommand("harborpos")]
        private void ChatCommandPos(BasePlayer player)
        {
            if (!player.IsAdmin || Controller == null) return;
            Vector3 pos = Controller.transform.InverseTransformPoint(player.transform.position);
            Puts($"Position: {pos}");
            PrintToChat(player, $"Position: {pos}");
        }

        [ConsoleCommand("harborstart")]
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
            else Puts("This event is active now. To finish this event (harborstop), then to start the next one");
        }

        [ConsoleCommand("harborstop")]
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

namespace Oxide.Plugins.HarborEventExtensionMethods
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

        public static TSource Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double> predicate)
        {
            TSource result = default(TSource);
            double resultValue = double.MinValue;
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