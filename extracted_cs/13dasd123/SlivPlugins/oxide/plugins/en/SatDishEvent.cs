using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Facepunch;
using Oxide.Plugins.SatDishEventExtensionMethods;

namespace Oxide.Plugins
{
    [Info("SatDishEvent", "KpucTaJl", "2.1.5")]
    internal class SatDishEvent : RustPlugin
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
            if (_config.PluginVersion < new VersionNumber(2, 0, 5))
            {
                _config.Gui = new GuiConfig
                {
                    IsGui = true,
                    OffsetMinY = "-56"
                };
                foreach (PresetConfig preset in _config.Npc) foreach (NpcBelt belt in preset.Config.BeltItems) belt.Ammo = string.Empty;
            }
            if (_config.PluginVersion < new VersionNumber(2, 0, 8))
            {
                _config.Commands = new HashSet<string>
                {
                    "/remove",
                    "remove.toggle"
                };
            }
            if (_config.PluginVersion < new VersionNumber(2, 1, 0))
            {
                _config.Marker.Name = "SatDishEvent ({time})";
                _config.Radius = 90f;
            }
            if (_config.PluginVersion < new VersionNumber(2, 1, 4))
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

        public class BradleyConfig
        {
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
            [JsonProperty(En ? "Can an Npc attack a non-owner of the event? [true/false]" : "Может ли Npc атаковать не владельца ивента? [true/false]")] public bool TargetNpc { get; set; }
            [JsonProperty(En ? "Can Bradley attack a non-owner of the event? [true/false]" : "Может ли Bradley атаковать не владельца ивента? [true/false]")] public bool TargetTank { get; set; }
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
            [JsonProperty(En ? "Destruction of Bradley" : "Уничтожение Bradley")] public double Bradley { get; set; }
            [JsonProperty(En ? "Killing an NPC" : "Убийство NPC")] public double Npc { get; set; }
            [JsonProperty(En ? "Hacking a locked crate" : "Взлом заблокированного ящика")] public double LockedCrate { get; set; }
            [JsonProperty(En ? "Killing an Zombie" : "Убийство зомби")] public double Zombie { get; set; }
            [JsonProperty(En ? "List of commands that are executed in the console at the end of the event ({steamid} - the player who collected the highest number of points)" : "Список команд, которые выполняются в консоли по окончанию ивента ({steamid} - игрок, который набрал наибольшее кол-во баллов)")] public HashSet<string> Commands { get; set; }
        }

        public class ZombieConfig
        {
            [JsonProperty(En ? "Health" : "Кол-во ХП")] public float Hp { get; set; }
            [JsonProperty(En ? "Movement speed" : "Скорость движения")] public float Speed { get; set; }
            [JsonProperty(En ? "Remove a corpse after death? (it is recommended to use the true value to improve performance) [true/false]" : "Удалять труп после смерти? (рекомендуется использовать значение true для повышения производительности) [true/false]")] public bool IsRemoveCorpse { get; set; }
            [JsonProperty(En ? "Which loot table should the plugin use? (0 - default; 1 - own; 2 - AlphaLoot; 3 - CustomLoot; 4 - loot table of the Rust objects; 5 - combine the 1 and 4 methods)" : "Какую таблицу лута необходимо использовать? (0 - стандартную; 1 - собственную; 2 - AlphaLoot; 3 - CustomLoot; 4 - таблица предметов объектов Rust; 5 - совместить 1 и 4 методы)")] public int TypeLootTable { get; set; }
            [JsonProperty(En ? "Loot table from prefabs (if the loot table type is 4 or 5)" : "Таблица предметов из prefab-ов (если тип таблицы предметов - 4 или 5)")] public PrefabLootTableConfig PrefabLootTable { get; set; }
            [JsonProperty(En ? "Own loot table (if the loot table type is 1 or 5)" : "Собственная таблица предметов (если тип таблицы предметов - 1 или 5)")] public LootTableConfig OwnLootTable { get; set; }
        }

        public class PointConfig
        {
            [JsonProperty(En ? "Enabled? [true/false]" : "Включен? [true/false]")] public bool Enabled { get; set; }
            [JsonProperty(En ? "Text" : "Текст")] public string Text { get; set; }
            [JsonProperty(En ? "Size" : "Размер")] public int Size { get; set; }
            [JsonProperty(En ? "Color" : "Цвет")] public string Color { get; set; }
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
            [JsonProperty(En ? "Locked crate setting" : "Настройка заблокированного ящика")] public HackCrateConfig HackCrate { get; set; }
            [JsonProperty(En ? "Marker configuration on the map" : "Настройка маркера на карте")] public MarkerConfig Marker { get; set; }
            [JsonProperty(En ? "Main marker settings for key event points shown on players screen" : "Настройки основного маркера на экране игрока")] public PointConfig MainPoint { get; set; }
            [JsonProperty(En ? "Additional marker settings for key event points shown on players screen" : "Настройки дополнительного маркера на экране игрока")] public PointConfig AdditionalPoint { get; set; }
            [JsonProperty(En ? "Prefix of chat messages" : "Префикс сообщений в чате")] public string Prefix { get; set; }
            [JsonProperty(En ? "Do you use the chat? [true/false]" : "Использовать ли чат? [true/false]")] public bool IsChat { get; set; }
            [JsonProperty(En ? "GUI setting" : "Настройки GUI")] public GuiConfig Gui { get; set; }
            [JsonProperty(En ? "GUI Announcements setting" : "Настройка GUI Announcements")] public GuiAnnouncementsConfig GuiAnnouncements { get; set; }
            [JsonProperty(En ? "Notify setting" : "Настройка Notify")] public NotifyConfig Notify { get; set; }
            [JsonProperty(En ? "Discord setting (only for users DiscordMessages plugin)" : "Настройка оповещений в Discord (только для тех, кто использует плагин DiscordMessages)")] public DiscordConfig Discord { get; set; }
            [JsonProperty(En ? "Radius of the event zone" : "Радиус зоны ивента")] public float Radius { get; set; }
            [JsonProperty(En ? "Does an additional Bradley appear at the beginning of the event? [true/false]" : "Появляется дополнительный Bradley в начале ивента? [true/false]")] public bool IsAdditionalBradley { get; set; }
            [JsonProperty(En ? "Bradley setting" : "Настройка танка")] public BradleyConfig Bradley { get; set; }
            [JsonProperty(En ? "Do you create a PVP zone in the event area? (only for users TruePVE plugin) [true/false]" : "Создавать зону PVP в зоне проведения ивента? (только для тех, кто использует плагин TruePVE) [true/false]")] public bool IsCreateZonePvp { get; set; }
            [JsonProperty(En ? "PVE Mode Setting (only for users PveMode plugin)" : "Настройка PVE режима работы плагина (только для тех, кто использует плагин PveMode)")] public PveModeConfig PveMode { get; set; }
            [JsonProperty(En ? "Interrupt the teleport in Satellite Dish? (only for users NTeleportation plugin) [true/false]" : "Запрещать телепорт на спутниковых тарелках? (только для тех, кто использует плагин NTeleportation) [true/false]")] public bool NTeleportationInterrupt { get; set; }
            [JsonProperty(En ? "Disable NPCs from the BetterNpc plugin on the monument while the event is on? [true/false]" : "Отключать NPC из плагина BetterNpc на монументе пока проходит ивент? [true/false]")] public bool RemoveBetterNpc { get; set; }
            [JsonProperty(En ? "NPCs setting" : "Настройка NPC")] public HashSet<PresetConfig> Npc { get; set; }
            [JsonProperty(En ? "Economy setting (total values will be added up and rewarded at the end of the event)" : "Настройка экономики (конечное значение суммируется и будет выдано игрокам по окончанию ивента)")] public EconomyConfig Economy { get; set; }
            [JsonProperty(En ? "The CCTV camera" : "Название камеры")] public string Cctv { get; set; }
            [JsonProperty(En ? "Can SAM Site turrets appear in the event zone? [true/false]" : "Должны ли появляться Sam Site турели в зоне ивента? [true/false]")] public bool IsSamSites { get; set; }
            [JsonProperty(En ? "Delayed departure of CH47 after the start of the event [sec.]" : "Задержка вылета CH47 после начала ивента [sec.]")] public float DelayCh47 { get; set; }
            [JsonProperty(En ? "Flight altitude CH47 [m.]" : "Высота полета CH47 [m.]")] public float HeightCh47 { get; set; }
            [JsonProperty(En ? "Plane flight speed multiplier" : "Множитель скорости полета самолета")] public float ScaleSpeedPlane { get; set; }
            [JsonProperty(En ? "Zombies setting" : "Настройка зомби")] public ZombieConfig Zombies { get; set; }
            [JsonProperty(En ? "List of commands banned in the event zone" : "Список команд запрещенных в зоне ивента")] public HashSet<string> Commands { get; set; }
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
                            Position = "(-4.393, 5.991, -7.11)",
                            Rotation = "(0, 335.197, 0)",
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
                            Position = "(2.995, 6.069, -18.281)",
                            Rotation = "(0, 0, 0)",
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
                            Position = "(-14.655, 6.245, -37.951)",
                            Rotation = "(0, 29.72, 0)",
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
                            Position = "(-5.894, 6.855, -25.436)",
                            Rotation = "(0, 233.92, 326.004)",
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
                            Position = "(-64.114, 0.089, -44.697)",
                            Rotation = "(0, 60.775, 0)",
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
                            Position = "(0.022, 6.069, -18.171)",
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
                        },
                        new CrateConfig
                        {
                            Prefab = "assets/bundled/prefabs/radtown/crate_normal_2.prefab",
                            Position = "(-12.004, 5.893, -29.853)",
                            Rotation = "(0, 32.067, 0)",
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
                            Prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_medical.prefab",
                            Position = "(-63.339, 1.263, -38.308)",
                            Rotation = "(5.213, 0, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/crate_medical.prefab" } }
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
                            Prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_medical.prefab",
                            Position = "(-7.506, 7.041, -23.058)",
                            Rotation = "(5.335, 323.221, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/crate_medical.prefab" } }
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
                            Prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_medical.prefab",
                            Position = "(2.758, 7.284, -7.781)",
                            Rotation = "(0, 0, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/crate_medical.prefab" } }
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
                            Prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_ammunition.prefab",
                            Position = "(-0.005, 7.284, -8.157)",
                            Rotation = "(0, 0, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/crate_ammunition.prefab" } }
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
                            Prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_ammunition.prefab",
                            Position = "(-30.254, 6.932, -40.082)",
                            Rotation = "(0, 0, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/crate_ammunition.prefab" } }
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
                            Prefab = "assets/bundled/prefabs/radtown/underwater_labs/tech_parts_2.prefab",
                            Position = "(0.433, 7.284, -6.215)",
                            Rotation = "(0, 0, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/tech_parts_2.prefab" } }
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
                            Prefab = "assets/bundled/prefabs/radtown/underwater_labs/tech_parts_1.prefab",
                            Position = "(-60.971, 1.265, -43.093)",
                            Rotation = "(2.554, 355.733, 12.007)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/tech_parts_1.prefab" } }
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
                            Prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_tools.prefab",
                            Position = "(-27.461, 5.893, -43.416)",
                            Rotation = "(0, 58.631, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/crate_tools.prefab" } }
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
                            Prefab = "assets/bundled/prefabs/radtown/crate_normal_2_medical.prefab",
                            Position = "(-10.263, 6.054, -18.183)",
                            Rotation = "(0, 0, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/crate_normal_2_medical.prefab" } }
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
                            Prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_food_1.prefab",
                            Position = "(9.189, 6.067, -4.821)",
                            Rotation = "(0, 80.919, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/crate_food_1.prefab" } }
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
                            Prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_food_1.prefab",
                            Position = "(11.024, 6.067, -4.711)",
                            Rotation = "(0, 16.709, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/crate_food_1.prefab" } }
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
                            Prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_food_1.prefab",
                            Position = "(8.289, 6.067, -18.256)",
                            Rotation = "(0, 322.681, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/crate_food_1.prefab" } }
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
                            Prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_food_2.prefab",
                            Position = "(7.022, 6.067, -9.697)",
                            Rotation = "(0, 15.952, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/crate_food_2.prefab" } }
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
                            Prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_food_2.prefab",
                            Position = "(11.098, 6.067, -11.957)",
                            Rotation = "(0, 15.952, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/crate_food_2.prefab" } }
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
                            Prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_food_2.prefab",
                            Position = "(11.102, 6.067, -15.587)",
                            Rotation = "(0, 334.754, 0)",
                            PrefabLootTable = new PrefabLootTableConfig
                            {
                                Min = 1,
                                Max = 1,
                                UseCount = true,
                                Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 50.0f, PrefabDefinition = "assets/bundled/prefabs/radtown/underwater_labs/crate_food_2.prefab" } }
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
                        Name = "SatDishEvent ({time})",
                        Radius = 0.4f,
                        Alpha = 0.6f,
                        Color = new ColorConfig { R = 0.81f, G = 0.25f, B = 0.15f }
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
                    Prefix = "[SatDishEvent]",
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
                            "StartDeal",
                            "TakeCH47",
                            "BrokeDeal",
                            "AnswerPhone",
                            "CallReinforcement",
                            "OpenLockedCrate",
                            "KillBradley"
                        }
                    },
                    Radius = 90f,
                    IsAdditionalBradley = true,
                    Bradley = new BradleyConfig
                    {
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
                            Items = new List<ItemConfig> { new ItemConfig { ShortName = "scrap", MinAmount = 100, MaxAmount = 200, Chance = 50.0f, IsBluePrint = false, SkinID = 0, Name = "" } }
                        }
                    },
                    IsCreateZonePvp = false,
                    PveMode = new PveModeConfig
                    {
                        Pve = false,
                        Damage = 500f,
                        ScaleDamage = new HashSet<ScaleDamageConfig>
                        {
                            new ScaleDamageConfig { Type = "NPC", Scale = 1f },
                            new ScaleDamageConfig { Type = "Bradley", Scale = 2f }
                        },
                        LootCrate = false,
                        HackCrate = false,
                        LootNpc = false,
                        DamageNpc = false,
                        DamageTank = false,
                        TargetNpc = false,
                        TargetTank = false,
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
                    Npc = new HashSet<PresetConfig>
                    {
                        new PresetConfig
                        {
                            Min = 8,
                            Max = 13,
                            Positions = new List<string>
                            {
                                "(-55.8, 1.2, -48.1)",
                                "(-62.8, 0.2, -45.0)",
                                "(-58.3, 0.7, -38.4)",
                                "(-5.8, 6.2, -17.7)",
                                "(-6.2, 6.1, 3.6)",
                                "(-10.6, 6.0, -6.3)",
                                "(-35.8, 6.0, -7.0)",
                                "(-67.5, 6.1, -18.8)",
                                "(-67.6, 6.1, 4.2)",
                                "(-32.8, 0.2, 20.1)",
                                "(-26.1, 0.0, 7.1)",
                                "(-18.8, 0.0, 15.0)",
                                "(31.1, 0.0, -1.7)",
                                "(33.1, 0.0, 11.7)",
                                "(22.3, 0.0, 14.1)",
                                "(26.7, 0.0, -21.4)",
                                "(17.6, 0.0, -29.1)",
                                "(35.6, -0.1, -27.9)",
                                "(42.4, 6.0, -7.1)",
                                "(67.4, 6.1, 4.5)",
                                "(67.7, 6.1, -18.5)",
                                "(-33.1, 0.5, -56.6)",
                                "(-11.5, 0.8, -52.6)",
                                "(3.5, 0.4, -41.8)",
                                "(-32.0, 0.0, -16.0)",
                                "(-22.0, 0.0, -24.9)",
                                "(-38.2, 0.1, -29.1)",
                                "(-26.7, 6.1, 50.5)",
                                "(-4.8, 6.1, 56.3)",
                                "(-3.1, 6.1, 33.0)"
                            },
                            Config = new NpcConfig
                            {
                                Name = "Scientist",
                                Health = 150f,
                                RoamRange = 8f,
                                ChaseRange = 100f,
                                AttackRangeMultiplier = 2f,
                                SenseRange = 85f,
                                MemoryDuration = 30f,
                                DamageScale = 0.4f,
                                AimConeScale = 1f,
                                CheckVisionCone = false,
                                VisionCone = 135f,
                                Speed = 7.5f,
                                DisableRadio = false,
                                Stationary = false,
                                IsRemoveCorpse = true,
                                WearItems = new HashSet<NpcWear>
                                {
                                    new NpcWear { ShortName = "hoodie", SkinID = 2187105866 },
                                    new NpcWear { ShortName = "shoes.boots", SkinID = 0 },
                                    new NpcWear { ShortName = "sunglasses", SkinID = 0 },
                                    new NpcWear { ShortName = "pants", SkinID = 2187107432 }
                                },
                                BeltItems = new HashSet<NpcBelt>
                                {
                                    new NpcBelt { ShortName = "pistol.m92", Amount = 1, SkinID = 0, Mods = new HashSet<string> { "weapon.mod.flashlight" }, Ammo = string.Empty },
                                    new NpcBelt { ShortName = "syringe.medical", Amount = 5, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty },
                                    new NpcBelt { ShortName = "grenade.f1", Amount = 1, SkinID = 0, Mods = new HashSet<string>(), Ammo = string.Empty }
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
                            ["crate_normal_2"] = 0.1,
                            ["crate_medical"] = 0.1,
                            ["crate_ammunition"] = 0.2,
                            ["tech_parts_2"] = 0.1,
                            ["tech_parts_1"] = 0.1,
                            ["crate_tools"] = 0.1,
                            ["crate_normal_2_medical"] = 0.1,
                            ["crate_food_1"] = 0.1,
                            ["crate_food_2"] = 0.1
                        },
                        Bradley = 0.8,
                        Npc = 0.3,
                        LockedCrate = 0.5,
                        Zombie = 0.4,
                        Commands = new HashSet<string>()
                    },
                    Cctv = "SatDish",
                    IsSamSites = true,
                    DelayCh47 = 0f,
                    HeightCh47 = 200f,
                    ScaleSpeedPlane = 4f,
                    Zombies = new ZombieConfig
                    {
                        Hp = 200f,
                        Speed = 1f,
                        IsRemoveCorpse = true,
                        TypeLootTable = 5,
                        PrefabLootTable = new PrefabLootTableConfig
                        {
                            Min = 1,
                            Max = 1,
                            UseCount = true,
                            Prefabs = new List<PrefabConfig> { new PrefabConfig { Chance = 100f, PrefabDefinition = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_heavy.prefab" } }
                        },
                        OwnLootTable = new LootTableConfig
                        {
                            Min = 1,
                            Max = 1,
                            UseCount = true,
                            Items = new List<ItemConfig>
                            {
                                new ItemConfig { ShortName = "scrap", MinAmount = 5, MaxAmount = 10, Chance = 50f, IsBluePrint = false, SkinID = 0, Name = "" },
                                new ItemConfig { ShortName = "syringe.medical", MinAmount = 1, MaxAmount = 2, Chance = 70.0f, IsBluePrint = false, SkinID = 0, Name = "" }
                            }
                        }
                    },
                    Commands = new HashSet<string>
                    {
                        "/remove",
                        "remove.toggle"
                    },
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
                ["PreStart"] = "{0} The Biological Weapons Transaction will begin at the <color=#55aaff>Satellite Dish</color> location in <color=#55aaff>{1} sec.</color>!",
                ["Start"] = "{0} The Chinook <color=#738d43>has flown out</color> to grid <color=#55aaff>{1}</color> in order to pick up prototypes for the bioweapons transaction!\nCCTV: <color=#55aaff>{2}</color>",
                ["PreFinish"] = "{0} The Biological Weapons Transaction <color=#ce3f27>will end</color> in <color=#55aaff>{1} sec.</color>!",
                ["Finish"] = "{0} The Biological Weapons Transaction <color=#ce3f27>has concluded</color>!",
                ["StartDeal"] = "{0} The Biological Weapons Transaction <color=#738d43>has begun</color>! Chinook <color=#738d43>has dropped</color> the locked crate and <color=#738d43>started loading</color> prototypes onto the Chinook!",
                ["TakeCH47"] = "{0} The Chinook was able to obtain <color=#55aaff>{1}</color> biological prototypes!",
                ["BrokeDeal"] = "{0} <color=#55aaff>{1}</color> has disturbed The Biological Weapons Transaction! You have to answer the phone, otherwise reinforcements will be sent into the Event Zone.",
                ["AnswerPhone"] = "{0} <color=#55aaff>{1}</color> answered the phone call! Reinforcements will not be sent as we were able to fake the all clear!",
                ["CallReinforcement"] = "{0} Nobody has answered the phone! Reinforcements will arrive to the <color=#55aaff>Satellite Dish</color> soon! The plane is already on its way to the island",
                ["OpenLockedCrate"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>has started hacking</color> the locked crate!",
                ["KillBradley"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>destroyed</color> the tank!",
                ["EventActive"] = "{0} This event is active. To finish this event (<color=#55aaff>/satdishstop</color>), then (<color=#55aaff>/satdishstart</color> to start the next one!",
                ["EnterPVP"] = "{0} You <color=#ce3f27>have entered</color> the PVP zone, now other players <color=#ce3f27>can damage</color> you!",
                ["ExitPVP"] = "{0} You <color=#738d43>have left</color> the PVP zone, now other players <color=#738d43>cannot damage</color> you!",
                ["NTeleportation"] = "{0} You <color=#ce3f27>cannot</color> teleport into the Event Zone!",
                ["SendEconomy"] = "{0} You <color=#738d43>have earned</color> <color=#55aaff>{1}</color> points in economics for participating in the event",
                ["NoCommand"] = "{0} You <color=#ce3f27>cannot</color> use this command in the event zone!"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PreStart"] = "{0} Через <color=#55aaff>{1} сек.</color> в локации <color=#55aaff>Спутниковые тарелки</color> начнется сделка по продаже биологического оружия!",
                ["Start"] = "{0} На сделку по продаже биологического оружия <color=#738d43>вылетел</color> CH47 в квадрат <color=#55aaff>{1}</color>, чтобы забрать опытные образцы!\nКамера: <color=#55aaff>{2}</color>",
                ["PreFinish"] = "{0} Сделка по продаже биологического оружия <color=#ce3f27>закончится</color> через <color=#55aaff>{1} сек.</color>!",
                ["Finish"] = "{0} Сделка по продаже биологического оружия <color=#ce3f27>закончена</color>!",
                ["StartDeal"] = "{0} Сделка по продаже биологического оружия <color=#738d43>началась</color>! CH47 <color=#738d43>скинул</color> заблокированный ящик и <color=#738d43>начал погрузку</color> опытных образцов к себе на борт",
                ["TakeCH47"] = "{0} CH47 удалось забрать <color=#55aaff>{1}</color> опытных образцов!",
                ["BrokeDeal"] = "{0} <color=#55aaff>{1}</color> сорвал сделку по продаже биологического оружия! Необходимо ответить на телефонный звонок, иначе в зону ивента прибудет подкрепление",
                ["AnswerPhone"] = "{0} <color=#55aaff>{1}</color> ответил на телефонный звонок! Вызов подкрепления отменен",
                ["CallReinforcement"] = "{0} Никто не ответил на телефонный звонок! В локацию <color=#55aaff>Спутниковые тарелки</color> скоро прибудет подкрепление! Самолет уже вылетел к острову",
                ["OpenLockedCrate"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>начал</color> взлом заблокированного ящика!",
                ["KillBradley"] = "{0} <color=#55aaff>{1}</color> <color=#738d43>уничтожил</color> танк!",
                ["EventActive"] = "{0} Ивент в данный момент активен, сначала завершите текущий ивент (<color=#55aaff>/satdishstop</color>), чтобы начать следующий!",
                ["EnterPVP"] = "{0} Вы <color=#ce3f27>вошли</color> в PVP зону, теперь другие игроки <color=#ce3f27>могут</color> наносить вам урон!",
                ["ExitPVP"] = "{0} Вы <color=#738d43>вышли</color> из PVP зоны, теперь другие игроки <color=#738d43>не могут</color> наносить вам урон!",
                ["NTeleportation"] = "{0} Вы <color=#ce3f27>не можете</color> телепортироваться в зоне ивента!",
                ["SendEconomy"] = "{0} Вы <color=#738d43>получили</color> <color=#55aaff>{1}</color> баллов в экономику за прохождение ивента",
                ["NoCommand"] = "{0} Вы <color=#ce3f27>не можете</color> использовать данную команду в зоне ивента!"
            }, this, "ru");
        }

        private string GetMessage(string langKey, string userID) => lang.GetMessage(langKey, _ins, userID);

        private string GetMessage(string langKey, string userID, params object[] args) => (args.Length == 0) ? GetMessage(langKey, userID) : string.Format(GetMessage(langKey, userID), args);
        #endregion Lang

        #region Sound
        private readonly Dictionary<string, List<byte[]>> _sound = new Dictionary<string, List<byte[]>>();

        private void LoadSound()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("SatelliteDishEvent/sound_en")) _sound.Add("en", Interface.Oxide.DataFileSystem.ReadObject<List<byte[]>>("SatelliteDishEvent/sound_en"));
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("SatelliteDishEvent/sound_ru")) _sound.Add("ru", Interface.Oxide.DataFileSystem.ReadObject<List<byte[]>>("SatelliteDishEvent/sound_ru"));
        }

        private Coroutine _playCoroutine = null;

        private IEnumerator PlaySoundToPlayer(BasePlayer player)
        {
            if (_sound != null && _sound.Count != 0)
            {
                string language = lang.GetLanguage(player.UserIDString);
                foreach (byte[] data in _sound.ContainsKey(language) ? _sound[language] : _sound["en"])
                {
                    Network.NetWrite netWrite = Network.Net.sv.StartWrite();
                    netWrite.PacketID(Network.Message.Type.VoiceData);
                    netWrite.UInt64(_controller.Dummy.net.ID.Value);
                    netWrite.BytesWithSize(data);
                    netWrite.Send(new Network.SendInfo(player.Connection) { priority = Network.Priority.Immediate });
                    yield return CoroutineEx.waitForSeconds(0.07f);
                }
            }
        }
        #endregion Sound

        #region Oxide Hooks
        private static SatDishEvent _ins;

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
            foreach (MonumentInfo monument in TerrainMeta.Path.Monuments) if (monument.displayPhrase.english == "Satellite Dish") StartLocations.Add(new Location { pos = monument.transform.position, rot = monument.transform.rotation.eulerAngles });
            if (StartLocations.Count == 0)
            {
                PrintError("The Satellite Dish location is missing on the map. The plugin cannot be loaded!");
                NextTick(() => Interface.Oxide.UnloadPlugin(Name));
                return;
            }
            LoadSound();
            if (_config.EnabledTimer)
            {
                timer.In(UnityEngine.Random.Range(_config.MinStartTime, _config.MaxStartTime), () =>
                {
                    if (!_active) Start();
                    else Puts("This event is active now. To finish this event (satdishstop), then to start the next one");
                });
            }
        }

        private void Unload()
        {
            if (_active) Finish();
            if (_playCoroutine != null) ServerMgr.Instance.StopCoroutine(_playCoroutine);
            _ins = null;
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return null;
            if (_controller.Entities.Contains(entity) ||
                (entity is BradleyAPC && entity == _controller.Bradley && !(entity as BradleyAPC).myRigidBody.useGravity) ||
                (entity is CH47Helicopter && entity == _controller.Ch47) ||
                (entity is ScientistNPC && _controller.Zombies.Contains(entity as ScientistNPC) && _controller.StageCh47 < 6) ||
                (entity is BasePlayer && entity == _controller.Dummy) ||
                (entity is Telephone && entity == _controller.Phone)) return true;
            else if (info.Initiator is BradleyAPC && (info.Initiator == _controller.Bradley || info.Initiator == _controller.AddBradley)) info.damageTypes.ScaleAll(_config.Bradley.ScaleDamage);
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

        private void OnSupplyDropDropped(SupplyDrop supplyDrop, CargoPlane cargoPlane)
        {
            if (supplyDrop == null || cargoPlane == null) return;
            if (cargoPlane == _controller.Plane)
            {
                _controller.SpawnBradley(supplyDrop.transform.position);
                if (supplyDrop.IsExists()) supplyDrop.Kill();
                Unsubscribe("OnSupplyDropDropped");
            }
        }

        private readonly Dictionary<ulong, BasePlayer> _startHackCrates = new Dictionary<ulong, BasePlayer>();

        private void CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (player == null || crate == null) return;
            if (crate == _controller.HackCrate)
            {
                if (_startHackCrates.ContainsKey(crate.net.ID.Value)) _startHackCrates[crate.net.ID.Value] = player;
                else _startHackCrates.Add(crate.net.ID.Value, player);
            }
        }

        private void OnCrateHack(HackableLockedCrate crate)
        {
            if (crate == null) return;
            ulong crateId = crate.net.ID.Value;
            BasePlayer player;
            if (_startHackCrates.TryGetValue(crateId, out player))
            {
                _startHackCrates.Remove(crateId);
                if (_config.HackCrate.IncreaseEventTime && _controller.TimeToFinish < (int)_config.HackCrate.UnlockTime) _controller.TimeToFinish += (int)_config.HackCrate.UnlockTime;
                ActionEconomy(player.userID, "LockedCrate");
                AlertToAllPlayers("OpenLockedCrate", _config.Prefix, player.displayName);
                Unsubscribe("CanHackCrate");
                Unsubscribe("OnCrateHack");
            }
        }

        private object CanBradleyApcTarget(BradleyAPC bradley, BaseEntity entity)
        {
            if (bradley == null || entity == null) return null;
            if (bradley == _controller.Bradley || bradley == _controller.AddBradley)
            {
                if (bradley == _controller.Bradley && !bradley.myRigidBody.useGravity) return false;
                if ((entity as BasePlayer).IsPlayer()) return null;
                else return false;
            }
            else return null;
        }

        private void OnEntityDeath(BasePlayer player, HitInfo info)
        {
            if (player != null && _controller.Players.Contains(player))
            {
                _controller.Players.Remove(player);
                if (_config.Gui.IsGui) CuiHelper.DestroyUi(player, "Tabs_KpucTaJl");
            }
        }

        private void OnEntityDeath(BradleyAPC bradley, HitInfo info)
        {
            if (bradley == null || info == null) return;
            BasePlayer attacker = info.InitiatorPlayer;
            if (bradley == _controller.Bradley)
            {
                if (attacker != null)
                {
                    ActionEconomy(attacker.userID, "Bradley");
                    AlertToAllPlayers("KillBradley", _config.Prefix, attacker.displayName);
                }
                if (_controller.TimeToFinish > _config.PreFinishTime)
                {
                    if (_config.HackCrate.IncreaseEventTime && _controller.HackCrate != null && _controller.HackCrate.IsBeingHacked()) _controller.TimeToFinish = _config.PreFinishTime + (int)(HackableLockedCrate.requiredHackSeconds - _controller.HackCrate.hackSeconds);
                    else _controller.TimeToFinish = _config.PreFinishTime;
                }
            }
            else if (bradley == _controller.AddBradley && attacker != null) ActionEconomy(attacker.userID, "Bradley");
        }

        private void OnEntityDeath(ScientistNPC npc, HitInfo info)
        {
            if (npc == null || info == null) return;
            BasePlayer attacker = info.InitiatorPlayer;
            if (attacker == null) return;
            if (_controller.Zombies.Contains(npc))
            {
                ActionEconomy(attacker.userID, "Zombie");
                if (!_controller.IsAlarm)
                {
                    _controller.IsAlarm = true;
                    AlertToAllPlayers("BrokeDeal", _config.Prefix, attacker.displayName);
                    _controller.Alarm.UpdateFromInput(1, 0);
                    _controller.Siren.UpdateFromInput(1, 0);
                    _controller.CallPhone();
                }
            }
            else if (_controller.Scientists.Contains(npc)) ActionEconomy(attacker.userID, "Npc");
        }

        private object CanMountEntity(BasePlayer player, BaseMountable entity)
        {
            if (entity == null || player == null) return null;
            BaseEntity parent = entity.GetParentEntity();
            if (parent == null) return null;
            if ((_controller.Entities.Contains(parent) || parent == _controller.Ch47) && _controller.Players.Contains(player)) return true;
            if (parent == _controller.Ch47 && player.IsPlayer())
            {
                if (_controller.StageCh47 == 7)
                {
                    _controller.CancelInvoke(_controller.UpdateCh47);
                    _controller.Ch47.rigidBody.detectCollisions = true;
                    _controller.Ch47Ai.ClearLandingTarget();
                    _controller.Ch47 = null;
                    _controller.Ch47Ai = null;
                }
                else
                {
                    _controller.CancelInvoke(_controller.UpdateCh47);
                    foreach (Door door in _controller.Doors) door.SetOpen(true);
                    _controller.StageCh47 = 6;
                    _controller.Ch47.rigidBody.detectCollisions = true;
                    _controller.Ch47Ai.ClearLandingTarget();
                    _controller.Ch47 = null;
                    _controller.Ch47Ai = null;
                    _controller.IsAlarm = true;
                    AlertToAllPlayers("BrokeDeal", _config.Prefix, player.displayName);
                    _controller.Alarm.UpdateFromInput(1, 0);
                    _controller.Siren.UpdateFromInput(1, 0);
                    _controller.CallPhone();
                }
            }
            return null;
        }

        private object OnNpcTarget(ScientistNPC npc, BaseEntity entity)
        {
            if (npc == null || entity == null) return null;
            if (_controller.Zombies.Contains(npc)) return true;
            else return null;
        }

        private object OnNpcTarget(BaseEntity npc, BasePlayer entity)
        {
            if (npc == null || entity == null) return null;
            if (entity == _controller.Dummy || _controller.Zombies.Contains(entity as ScientistNPC)) return true;
            else return null;
        }

        private void OnPhoneAnswered(PhoneController receiverPhone, PhoneController callerPhone)
        {
            if (receiverPhone == null || callerPhone == null) return;
            if (receiverPhone == _controller.PhoneMonument.Controller && callerPhone == _controller.Phone.Controller)
            {
                _playCoroutine = ServerMgr.Instance.StartCoroutine(PlaySoundToPlayer(receiverPhone.currentPlayer));
                string name = receiverPhone.currentPlayer.displayName;
                timer.In(10f, () =>
                {
                    receiverPhone.SetPhoneStateWithPlayer(Telephone.CallState.Idle);
                    _controller.Alarm.UpdateFromInput(0, 0);
                    _controller.Siren.UpdateFromInput(0, 0);
                    AlertToAllPlayers("AnswerPhone", _config.Prefix, name);
                    if (_controller.TimeToFinish > _config.PreFinishTime)
                    {
                        if (_config.HackCrate.IncreaseEventTime && _controller.HackCrate != null && _controller.HackCrate.IsBeingHacked()) _controller.TimeToFinish = _config.PreFinishTime + (int)(HackableLockedCrate.requiredHackSeconds - _controller.HackCrate.hackSeconds);
                        else _controller.TimeToFinish = _config.PreFinishTime;
                    }
                });
            }
        }

        private void OnPhoneDialTimedOut(PhoneController callerPhone, PhoneController receiverPhone, BasePlayer player)
        {
            if (callerPhone == null || receiverPhone == null) return;
            if (receiverPhone == _controller.PhoneMonument.Controller && callerPhone == _controller.Phone.Controller)
            {
                _controller.SpawnPlane();
                AlertToAllPlayers("CallReinforcement", _config.Prefix);
            }
        }

        private object OnPhoneDial(PhoneController callerPhone, PhoneController receiverPhone, BasePlayer player)
        {
            if (callerPhone == null || receiverPhone == null) return null;
            if (callerPhone == _controller.Phone.Controller && receiverPhone == _controller.PhoneMonument.Controller) return null;
            if (callerPhone == _controller.PhoneMonument.Controller || receiverPhone == _controller.PhoneMonument.Controller) return true;
            else return null;
        }

        private object OnEntityKill(BaseEntity entity)
        {
            if (entity == null || _controller == null) return null;
            if (!_controller.KillEntities)
            {
                if (_controller.Entities.Contains(entity)) return true;
                if (entity is Telephone && entity == _controller.Phone) return true;
            }
            return null;
        }

        private readonly HashSet<ulong> _lootableCrates = new HashSet<ulong>();

        private void OnLootEntity(BasePlayer player, LootContainer container)
        {
            if (player == null || container == null || _lootableCrates.Contains(container.net.ID.Value)) return;
            if (_controller.Crates.Contains(container))
            {
                _lootableCrates.Add(container.net.ID.Value);
                ActionEconomy(player.userID, "Crates", container.ShortPrefabName);
            }
        }

        private object OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            if (player != null && _controller.Players.Contains(player))
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
            if (player != null && _controller.Players.Contains(player))
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
        private ControllerSatDishEvent _controller;
        private bool _active = false;

        private void Start()
        {
            if (!plugins.Exists("NpcSpawn"))
            {
                PrintError("NpcSpawn plugin doesn`t exist! (https://drive.google.com/drive/folders/1-18L-mG7yiGxR-PQYvd11VvXC2RQ4ZCu?usp=sharing)");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            _active = true;
            AlertToAllPlayers("PreStart", _config.Prefix, _config.PreStartTime);
            timer.In(_config.PreStartTime, () =>
            {
                Puts("SatDishEvent has begun");
                Subscribes();
                _controller = new GameObject().AddComponent<ControllerSatDishEvent>();
                Interface.Oxide.CallHook("OnSatDishEventStart", _controller.transform.position, _config.Radius);
                if (_config.RemoveBetterNpc && plugins.Exists("BetterNpc")) BetterNpc.Call("DestroyController", "Satellite Dish");
                if (plugins.Exists("MonumentOwner")) MonumentOwner.Call("RemoveZone", FindMonumentInfo(_controller.transform.position, "Satellite Dish"));
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
                        ["DamageTank"] = _config.PveMode.DamageTank,
                        ["DamageHelicopter"] = false,
                        ["TargetNpc"] = _config.PveMode.TargetNpc,
                        ["TargetTank"] = _config.PveMode.TargetTank,
                        ["TargetHelicopter"] = false,
                        ["CanEnter"] = _config.PveMode.CanEnter,
                        ["CanEnterCooldownPlayer"] = _config.PveMode.CanEnterCooldownPlayer,
                        ["TimeExitOwner"] = _config.PveMode.TimeExitOwner,
                        ["AlertTime"] = _config.PveMode.AlertTime,
                        ["RestoreUponDeath"] = _config.PveMode.RestoreUponDeath,
                        ["CooldownOwner"] = _config.PveMode.CooldownOwner,
                        ["Darkening"] = _config.PveMode.Darkening
                    };
                    HashSet<ulong> tanks = _config.IsAdditionalBradley ? new HashSet<ulong> { _controller.AddBradley.net.ID.Value } : new HashSet<ulong>();
                    PveMode.Call("EventAddPveMode", Name, config, _controller.transform.position, _config.Radius, _controller.Crates.Select(x => x.net.ID.Value), _controller.Scientists.Select(x => x.net.ID.Value), tanks, new HashSet<ulong>(), new HashSet<ulong>(), null);
                }
                AlertToAllPlayers("Start", _config.Prefix, PhoneController.PositionToGridCoord(_controller.transform.position), _config.Cctv);
            });
        }

        private void Finish()
        {
            Unsubscribes();
            if (_config.PveMode.Pve && plugins.Exists("PveMode")) PveMode.Call("EventRemovePveMode", Name, true);
            if (plugins.Exists("MonumentOwner") && _controller != null) MonumentOwner.Call("CreateZone", FindMonumentInfo(_controller.transform.position, "Satellite Dish"));
            if (_config.RemoveBetterNpc && plugins.Exists("BetterNpc")) BetterNpc.Call("CreateController", "Satellite Dish");
            if (_controller != null) UnityEngine.Object.Destroy(_controller.gameObject);
            _active = false;
            SendBalance();
            AlertToAllPlayers("Finish", _config.Prefix);
            Interface.Oxide.CallHook("OnSatDishEventEnd");
            Puts("SatDishEvent has ended");
            if (_config.EnabledTimer)
            {
                timer.In(UnityEngine.Random.Range(_config.MinStartTime, _config.MaxStartTime), () =>
                {
                    if (!_active) Start();
                    else Puts("This event is active now. To finish this event (satdishstop), then to start the next one");
                });
            }
        }

        internal class Prefab { public string prefab; public Vector3 pos; public Vector3 rot; }
        internal HashSet<Prefab> Prefabs = new HashSet<Prefab>
        {
            //sedantest.entity
            new Prefab { prefab = "assets/content/vehicles/sedan_a/sedantest.entity.prefab", pos = new Vector3(-64.318f, 0.114f, -39.859f), rot = new Vector3(359.604f, 212.535f, 359.278f) },
            new Prefab { prefab = "assets/content/vehicles/sedan_a/sedantest.entity.prefab", pos = new Vector3(-58.631f, 0.55f, -44.455f), rot = new Vector3(8.012f, 301.821f, 5.017f) },
            new Prefab { prefab = "assets/content/vehicles/sedan_a/sedantest.entity.prefab", pos = new Vector3(-28.056f, 5.828f, -39.16f), rot = new Vector3(0f, 247.503f, 0f) },
            new Prefab { prefab = "assets/content/vehicles/sedan_a/sedantest.entity.prefab", pos = new Vector3(-6.493f, 5.893f, -24.499f), rot = new Vector3(0f, 143.92f, 0f) },
            //barricade.concrete
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", pos = new Vector3(-64.79f, 0.17f, -45.018f), rot = new Vector3(0f, 61.32f, 0f) },
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", pos = new Vector3(-27.891f, 5.882f, -43.674f), rot = new Vector3(0f, 58.369f, 0f) },
            new Prefab { prefab = "assets/prefabs/deployable/barricades/barricade.concrete.prefab", pos = new Vector3(-11.787f, 5.894f, -31.016f), rot = new Vector3(0f, 32.462f, 0f) },
            //wall.frame
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(6.352f, 5.987f, -17.156f), rot = new Vector3(0f, 0f, 0f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(6.352f, 5.987f, -14.156f), rot = new Vector3(0f, 0f, 0f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(6.352f, 5.987f, -11.156f), rot = new Vector3(0f, 0f, 0f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(6.352f, 5.987f, -8.156f), rot = new Vector3(0f, 0f, 0f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(6.352f, 5.987f, -5.156f), rot = new Vector3(0f, 0f, 0f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(12.144f, 5.987f, -8.44f), rot = new Vector3(0f, 0f, 0f) },
            new Prefab { prefab = "assets/prefabs/building core/wall.frame/wall.frame.prefab", pos = new Vector3(12.144f, 5.987f, -5.44f), rot = new Vector3(0f, 0f, 0f) },
            //wall.frame.fence
            new Prefab { prefab = "assets/prefabs/building/wall.frame.fence/wall.frame.fence.prefab", pos = new Vector3(6.352f, 5.987f, -17.156f), rot = new Vector3(0f, 0f, 0f) },
            new Prefab { prefab = "assets/prefabs/building/wall.frame.fence/wall.frame.fence.prefab", pos = new Vector3(6.352f, 5.987f, -14.156f), rot = new Vector3(0f, 0f, 0f) },
            new Prefab { prefab = "assets/prefabs/building/wall.frame.fence/wall.frame.fence.prefab", pos = new Vector3(6.352f, 5.987f, -11.156f), rot = new Vector3(0f, 0f, 0f) },
            new Prefab { prefab = "assets/prefabs/building/wall.frame.fence/wall.frame.fence.prefab", pos = new Vector3(6.352f, 5.987f, -5.156f), rot = new Vector3(0f, 0f, 0f) },
            new Prefab { prefab = "assets/prefabs/building/wall.frame.fence/wall.frame.fence.prefab", pos = new Vector3(12.144f, 5.987f, -5.44f), rot = new Vector3(0f, 0f, 0f) },
            //wall.frame.cell.gate
            new Prefab { prefab = "assets/prefabs/building/wall.frame.cell/wall.frame.cell.gate.prefab", pos = new Vector3(12.144f, 5.987f, -8.44f), rot = new Vector3(0f, 180f, 0f) },
            new Prefab { prefab = "assets/prefabs/building/wall.frame.cell/wall.frame.cell.gate.prefab", pos = new Vector3(6.352f, 5.987f, -8.156f), rot = new Vector3(0f, 0f, 0f) },
            //cctv_deployed
            new Prefab { prefab = "assets/prefabs/deployable/cctvcamera/cctv_deployed.prefab", pos = new Vector3(-2.973f, 8.774f, -18.899f), rot = new Vector3(7.488f, 31.633f, 0f) },
            //electric.sirenlight.deployed
            new Prefab { prefab = "assets/prefabs/deployable/playerioents/lights/sirenlight/electric.sirenlight.deployed.prefab", pos = new Vector3(9.328f, 10.249f, -3.887f), rot = new Vector3(270f, 0f, 0f) },
            //audioalarm
            new Prefab { prefab = "assets/prefabs/deployable/playerioents/alarms/audioalarm.prefab", pos = new Vector3(9.336f, 9.942f, -3.896f), rot = new Vector3(0f, 180f, 0f) },
            //searchlight.deployed
            new Prefab { prefab = "assets/prefabs/deployable/search light/searchlight.deployed.prefab", pos = new Vector3(-57.828f, 10.455f, -13.166f), rot = new Vector3(-61.816f, 0.17f, -41.567f) },
            new Prefab { prefab = "assets/prefabs/deployable/search light/searchlight.deployed.prefab", pos = new Vector3(-54.752f, 40.844f, -17.276f), rot = new Vector3(-24.01f, 5.901f, -41.144f) },
            new Prefab { prefab = "assets/prefabs/deployable/search light/searchlight.deployed.prefab", pos = new Vector3(49.403f, 10.566f, -7.368f), rot = new Vector3(-7.892f, 5.901f, -28.549f) },
            new Prefab { prefab = "assets/prefabs/deployable/search light/searchlight.deployed.prefab", pos = new Vector3(-53.096f, 18.766f, -0.508f), rot = new Vector3(-3.957f, 6.048f, -5.545f) },
            new Prefab { prefab = "assets/prefabs/deployable/search light/searchlight.deployed.prefab", pos = new Vector3(-10.223f, 9.785f, 36.017f), rot = new Vector3(28f, 6.002f, -7.106f) },
            //sam_static
            new Prefab { prefab = "assets/prefabs/npc/sam_site_turret/sam_static.prefab", pos = new Vector3(9.336f, 10.13f, -2.138f), rot = new Vector3(0f, 180f, 0f) }
        };

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

        internal class ControllerSatDishEvent : FacepunchBehaviour
        {
            private MapMarkerGenericRadius _mapmarker;
            private VendingMachineMapMarker _vendingMarker;
            private SphereCollider _sphereCollider;
            internal bool IsAlarm;

            internal CH47Helicopter Ch47;
            internal CH47HelicopterAIController Ch47Ai;
            internal int StageCh47;
            private Vector3 _spawnCh47Pos;
            internal Vector3 DropCratePos;
            internal Vector3 LandingCh47Pos;
            private Vector3 _landingCh47Rot;

            internal CargoPlane Plane;

            internal BradleyAPC Bradley;
            internal Vector3 LandingBradleyPos;
            private Vector3 _landingBradleyRot;
            private readonly HashSet<BaseEntity> _parachutes = new HashSet<BaseEntity>();

            internal BradleyAPC AddBradley;
            internal Vector3 AddBradleyPos;

            internal bool KillEntities = false;
            internal HashSet<BaseEntity> Entities = new HashSet<BaseEntity>();
            internal HashSet<Door> Doors = new HashSet<Door>();
            internal AudioAlarm Alarm;
            internal SirenLight Siren;
            private readonly HashSet<SearchLight> _searchLights = new HashSet<SearchLight>();
            private bool _isLight;

            internal HashSet<LootContainer> Crates = new HashSet<LootContainer>();
            internal HackableLockedCrate HackCrate;

            internal int TimeToFinish;
            internal HashSet<BasePlayer> Players = new HashSet<BasePlayer>();

            private readonly HashSet<Vector3> _path = new HashSet<Vector3>();
            internal HashSet<ScientistNPC> Zombies = new HashSet<ScientistNPC>();
            internal int Ch47TakeZombies;

            internal Telephone Phone;
            internal Telephone PhoneMonument;
            internal BasePlayer Dummy;

            internal HashSet<ScientistNPC> Scientists = new HashSet<ScientistNPC>();

            private void Awake()
            {
                Location location = _ins.StartLocations.GetRandom();
                transform.position = location.pos;
                transform.rotation = Quaternion.Euler(location.rot);

                SpawnMapMarker();

                gameObject.layer = 3;
                _sphereCollider = gameObject.AddComponent<SphereCollider>();
                _sphereCollider.isTrigger = true;
                _sphereCollider.radius = _ins._config.Radius;

                IsAlarm = false;

                TimeToFinish = _ins._config.FinishTime;

                _isLight = true;
                SpawnEntities();

                SpawnCrates();

                Vector3 pos; Quaternion rot;

                DropCratePos = GetGlobalPosition(new Vector3(1.157f, 6.047f, -12.599f));
                GetGlobal(transform, new Vector3(28.011f, 6.047f, -6.973f), new Vector3(0f, 90f, 0f), out LandingCh47Pos, out rot);
                _landingCh47Rot = rot.eulerAngles;
                _spawnCh47Pos = GetSpawnPosition();
                InvokeRepeating(UpdateCh47, _ins._config.DelayCh47, 1f);

                GetGlobal(transform, new Vector3(-3.521f, 5.808f, 0.88f), new Vector3(0f, 180f, 0f), out LandingBradleyPos, out rot);
                _landingBradleyRot = rot.eulerAngles;

                _path.Add(GetGlobalPosition(new Vector3(10.382f, 6.067f, -7.845f)));
                _path.Add(GetGlobalPosition(new Vector3(12.157f, 6.067f, -7.845f)));
                _path.Add(LandingCh47Pos);
                SpawnZombies();

                TimeToFinish = _ins._config.FinishTime;
                InvokeRepeating(ChangeToFinishTime, 1f, 1f);

                FindPhoneMonument();
                SpawnPhone();
                GetGlobal(transform, new Vector3(6.616f, 6.067f, -1.576f), new Vector3(0f, 270f, 0f), out pos, out rot);
                SpawnDummy(pos, rot.eulerAngles);

                foreach (PresetConfig preset in _ins._config.Npc) SpawnPreset(preset);

                if (_ins._config.IsAdditionalBradley) SpawnAddBradley();
                else AddBradleyPos = Vector3.zero;
            }

            private void OnDestroy()
            {
                CancelInvoke(UpdateCh47);
                CancelInvoke(UpdateBradley);

                CancelInvoke(UpdateMapMarker);
                if (_mapmarker.IsExists()) _mapmarker.Kill();
                if (_vendingMarker.IsExists()) _vendingMarker.Kill();

                if (Ch47.IsExists()) Ch47.Kill();
                if (Plane.IsExists()) Plane.Kill();
                if (Bradley.IsExists()) Bradley.Kill();
                if (AddBradley.IsExists()) AddBradley.Kill();
                foreach (BaseEntity parachute in _parachutes) if (parachute.IsExists()) parachute.Kill();

                KillEntities = true;
                foreach (BaseEntity entity in Entities) if (entity.IsExists()) entity.Kill();

                foreach (LootContainer crate in Crates) if (crate.IsExists()) crate.Kill();
                if (HackCrate.IsExists()) HackCrate.Kill();

                foreach (ScientistNPC zombie in Zombies) if (zombie.IsExists()) zombie.Kill();

                if (Phone.IsExists()) Phone.Kill();
                if (Dummy.IsExists()) Dummy.Kill();

                foreach (ScientistNPC npc in Scientists) if (npc.IsExists()) npc.Kill();

                CancelInvoke(ChangeToFinishTime);
                foreach (BasePlayer player in Players) CuiHelper.DestroyUi(player, "Tabs_KpucTaJl");
            }

            private void OnTriggerEnter(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player.IsPlayer())
                {
                    Players.Add(player);
                    if (_ins._config.Gui.IsGui) _ins.CreateTabs(player, new Dictionary<string, string> { ["Clock_KpucTaJl"] = GetTimeFormat() });
                    if (_ins._config.IsCreateZonePvp) _ins.PrintToChat(player, _ins.GetMessage("EnterPVP", player.UserIDString, _ins._config.Prefix));
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
                if (_ins._config.Gui.IsGui)
                {
                    Dictionary<string, string> dic = new Dictionary<string, string> { ["Clock_KpucTaJl"] = GetTimeFormat() };
                    if (Scientists.Count > 0) dic.Add("Npc_KpucTaJl", Scientists.Count.ToString());
                    foreach (BasePlayer player in Players) _ins.CreateTabs(player, dic);
                }
                UpdateMarkerForPlayers();
                if ((TOD_Sky.Instance.Cycle.Hour > TimeSpan.Parse("20:00").TotalHours || TOD_Sky.Instance.Cycle.Hour < TimeSpan.Parse("8:00").TotalHours) && !_isLight)
                {
                    foreach (SearchLight light in _searchLights) light.UpdateFromInput(10, 0);
                    _isLight = true;
                }
                else if (TOD_Sky.Instance.Cycle.Hour < TimeSpan.Parse("20:00").TotalHours && TOD_Sky.Instance.Cycle.Hour > TimeSpan.Parse("8:00").TotalHours && _isLight)
                {
                    foreach (SearchLight light in _searchLights) light.UpdateFromInput(0, 0);
                    _isLight = false;
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
                _vendingMarker.markerShopName = _ins._config.Marker.Name.Replace("{time}", GetTimeFormat());
                _vendingMarker.Spawn();

                InvokeRepeating(UpdateMapMarker, 0, 1f);
            }

            private void UpdateMapMarker()
            {
                _mapmarker.SendUpdate();
                _vendingMarker.markerShopName = _ins._config.Marker.Name.Replace("{time}", GetTimeFormat());
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
                foreach (Prefab prefab in _ins.Prefabs)
                {
                    if (prefab.prefab == "assets/prefabs/npc/sam_site_turret/sam_static.prefab" && !_ins._config.IsSamSites) continue;

                    Vector3 pos; Quaternion rot;
                    GetGlobal(transform, prefab.pos, prefab.rot, out pos, out rot);
                    BaseEntity entity = SpawnEntity(prefab.prefab, pos, rot);

                    if (entity is BuildingBlock)
                    {
                        BuildingBlock buildingBlock = entity as BuildingBlock;
                        buildingBlock.SetGrade(BuildingGrade.Enum.Metal);
                        buildingBlock.SetHealthToMax();
                    }

                    if (entity is CCTV_RC)
                    {
                        CCTV_RC cctv = entity as CCTV_RC;
                        cctv.UpdateFromInput(5, 0);
                        cctv.rcIdentifier = _ins._config.Cctv;
                    }

                    if (entity is AudioAlarm) Alarm = entity as AudioAlarm;
                    if (entity is SirenLight) Siren = entity as SirenLight;

                    if (entity is Door)
                    {
                        Door door = entity as Door;
                        door.canTakeCloser = false;
                        door.canTakeKnocker = false;
                        door.canTakeLock = false;
                        door.canHandOpen = false;
                        door.hasHatch = false;
                        Doors.Add(door);
                    }

                    if (entity is BasicCar)
                    {
                        (entity as BasicCar).rigidBody.isKinematic = true;
                        FlasherLight flasherLight = GameManager.server.CreateEntity("assets/prefabs/deployable/playerioents/lights/flasherlight/electric.flasherlight.deployed.prefab") as FlasherLight;
                        flasherLight.enableSaving = false;
                        flasherLight.SetParent(entity as BasicCar);
                        flasherLight.transform.localPosition = new Vector3(0f, 1.64f, 0f);
                        flasherLight.Spawn();
                        flasherLight.UpdateFromInput(1, 0);
                        Entities.Add(flasherLight);
                    }

                    if (entity is SearchLight)
                    {
                        SearchLight light = entity as SearchLight;
                        light.UpdateFromInput(10, 0);
                        light.needsBuildingPrivilegeToUse = true;
                        light.SetTargetAimpoint(GetGlobalPosition(prefab.rot));
                        _searchLights.Add(light);
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

            private static JObject GetObjectConfig(NpcConfig config)
            {
                HashSet<string> states = config.Stationary ? new HashSet<string> { "IdleState", "CombatStationaryState" } : new HashSet<string> { "RoamState", "ChaseState", "CombatState" };
                if (config.BeltItems.Any(x => x.ShortName == "rocket.launcher" || x.ShortName == "explosive.timed")) states.Add("RaidState");
                return new JObject
                {
                    ["Name"] = config.Name,
                    ["WearItems"] = new JArray { config.WearItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["SkinID"] = x.SkinID }) },
                    ["BeltItems"] = new JArray { config.BeltItems.Select(x => new JObject { ["ShortName"] = x.ShortName, ["Amount"] = x.Amount, ["SkinID"] = x.SkinID, ["Mods"] = new JArray { x.Mods }, ["Ammo"] = x.Ammo }) },
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
                    ["TurretDamageScale"] = 1f,
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

            private static Vector3 GetSpawnPosition()
            {
                List<float> list = new List<float> { -World.Size / 2, World.Size / 2 };
                return new Vector3(list.GetRandom(), _ins._config.HeightCh47, list.GetRandom());
            }

            internal void UpdateCh47()
            {
                if (StageCh47 == 0)
                {
                    Vector3 targetPos = DropCratePos;
                    targetPos.y = _ins._config.HeightCh47;
                    SpawnNewCh47(_spawnCh47Pos, Quaternion.identity, targetPos, 0);
                    StageCh47++;
                }
                else if (StageCh47 == 1)
                {
                    if (Vector2.Distance(new Vector2(Ch47.transform.position.x, Ch47.transform.position.z), new Vector2(DropCratePos.x, DropCratePos.z)) < 1f)
                    {
                        SpawnNewCh47(Ch47.transform.position, Ch47.transform.rotation, new Vector3(DropCratePos.x, DropCratePos.y + 15f, DropCratePos.z), 1);
                        Ch47.transform.rotation = Quaternion.Euler(_landingCh47Rot);
                        StageCh47++;
                    }
                }
                else if (StageCh47 == 2)
                {
                    if (Ch47.transform.position.y - Ch47Ai.currentDesiredAltitude < 1f)
                    {
                        Ch47Ai.AiAltitudeForce = 0f;
                        Ch47Ai.rigidBody.AddForce(Vector3.down * 10000f, ForceMode.Force);
                        StageCh47++;
                    }
                }
                else if (StageCh47 == 3)
                {
                    Ch47Ai.rigidBody.AddForce(Vector3.down * 10000f, ForceMode.Force);
                    if (Ch47.transform.position.y - DropCratePos.y - 15f < 1f)
                    {
                        ChechTrash(DropCratePos, 10f);
                        Ch47Ai.DropCrate();
                        SpawnNewCh47(Ch47.transform.position, Ch47.transform.rotation, LandingCh47Pos, 0);
                        StageCh47++;
                    }
                }
                else if (StageCh47 == 4)
                {
                    if (Vector2.Distance(new Vector2(Ch47.transform.position.x, Ch47.transform.position.z), new Vector2(LandingCh47Pos.x, LandingCh47Pos.z)) < 1f)
                    {
                        Ch47.transform.rotation = Quaternion.Euler(_landingCh47Rot);
                        Ch47Ai.AiAltitudeForce = 0f;
                        ChechTrash(LandingCh47Pos, 10f);
                        Ch47Ai.rigidBody.AddForce(Vector3.down * 10000f, ForceMode.Force);
                        StageCh47++;
                    }
                }
                else if (StageCh47 == 5)
                {
                    Ch47Ai.rigidBody.AddForce(Vector3.down * 10000f, ForceMode.Force);
                    if (Ch47.transform.position.y - (LandingCh47Pos.y + 7.5f) < 1f)
                    {
                        _ins.AlertToAllPlayers("StartDeal", _ins._config.Prefix);
                        foreach (Door door in Doors) door.SetOpen(true);
                        foreach (ScientistNPC npc in Zombies)
                        {
                            PathController pathfollower = npc.gameObject.AddComponent<PathController>();
                            foreach (Vector3 point in _path) pathfollower.Paths.Add(point);
                        }
                        if (_ins._config.PveMode.Pve && _ins.plugins.Exists("PveMode")) _ins.PveMode.Call("EventAddScientists", _ins.Name, Zombies.Select(x => x.net.ID.Value));
                        StageCh47++;
                    }
                }
                else if (StageCh47 == 6)
                {
                    if (Zombies.Count == 0)
                    {
                        SpawnNewCh47(Ch47.transform.position, Ch47.transform.rotation, _spawnCh47Pos, 0);
                        _ins.AlertToAllPlayers("TakeCH47", _ins._config.Prefix, Ch47TakeZombies);
                        if (!IsAlarm && TimeToFinish > _ins._config.PreFinishTime)
                        {
                            if (HackCrate != null && HackCrate.IsBeingHacked()) TimeToFinish = _ins._config.PreFinishTime + (int)(HackableLockedCrate.requiredHackSeconds - HackCrate.hackSeconds);
                            else TimeToFinish = _ins._config.PreFinishTime;
                        }
                        StageCh47++;
                    }
                }
                else if (StageCh47 == 7)
                {
                    if (Vector2.Distance(new Vector2(Ch47.transform.position.x, Ch47.transform.position.z), new Vector2(_spawnCh47Pos.x, _spawnCh47Pos.z)) < 1f)
                    {
                        if (Ch47.IsExists()) Ch47.Kill();
                        CancelInvoke(UpdateCh47);
                    }
                }
            }

            private void SpawnNewCh47(Vector3 pos, Quaternion rot, Vector3 landingTarget, int numCrates)
            {
                CH47Helicopter ch47New = GameManager.server.CreateEntity("assets/prefabs/npc/ch47/ch47scientists.entity.prefab", pos, rot) as CH47Helicopter;
                CH47HelicopterAIController ch47AInew = ch47New.GetComponent<CH47HelicopterAIController>();
                ch47AInew.SetLandingTarget(landingTarget);
                if (Ch47.IsExists()) Ch47.Kill();
                Ch47 = ch47New;
                Ch47Ai = ch47AInew;
                Ch47.Spawn();
                Ch47Ai.CancelInvoke(Ch47Ai.SpawnScientists);
                Ch47.rigidBody.detectCollisions = false;
                Ch47Ai.numCrates = numCrates;
            }

            private void SpawnZombies()
            {
                Zombies.Add(SpawnZombie(new Vector3(9.3f, 6.1f, -15.9f), new Vector3(6.5f, 1.4f, 0f)));
                Zombies.Add(SpawnZombie(new Vector3(7.8f, 6.1f, -12.1f), new Vector3(7.4f, 32f, 0f)));
                Zombies.Add(SpawnZombie(new Vector3(8f, 6.1f, -6.3f), new Vector3(5.2f, 106.4f, 0f)));
                Zombies.Add(SpawnZombie(new Vector3(10.4f, 6.1f, -8.5f), new Vector3(8.7f, 102.3f, 0f)));
                Zombies.Add(SpawnZombie(new Vector3(10.6f, 6.1f, -13.8f), new Vector3(22.5f, 332.9f, 0f)));
            }

            private ScientistNPC SpawnZombie(Vector3 pos, Vector3 rot)
            {
                Vector3 position; Quaternion viewAngles;
                GetGlobal(transform, pos, rot, out position, out viewAngles);
                ScientistNPC npc = GameManager.server.CreateEntity("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_cargo_turret_any.prefab", position) as ScientistNPC;
                npc.enableSaving = false;
                npc.Spawn();
                npc.viewAngles = viewAngles.eulerAngles;
                npc.displayName = "Zombie";
                npc.startHealth = _ins._config.Zombies.Hp;
                npc.InitializeHealth(_ins._config.Zombies.Hp, _ins._config.Zombies.Hp);
                npc.inventory.containerWear.Clear();
                npc.inventory.containerBelt.Clear();
                Item mummysuit = ItemManager.CreateByName("halloween.mummysuit");
                if (!mummysuit.MoveToContainer(npc.inventory.containerWear)) mummysuit.Remove();
                Item gloweyes = ItemManager.CreateByName("gloweyes");
                if (!gloweyes.MoveToContainer(npc.inventory.containerWear)) gloweyes.Remove();
                npc.CancelInvoke(npc.PlayRadioChatter);
                npc.RadioChatterEffects = Array.Empty<GameObjectRef>();
                npc.DeathEffects = Array.Empty<GameObjectRef>();
                return npc;
            }

            internal void SpawnPlane()
            {
                Plane = GameManager.server.CreateEntity("assets/prefabs/npc/cargo plane/cargo_plane.prefab", GetSpawnPosition()) as CargoPlane;
                Plane.Spawn();
                Plane.UpdateDropPosition(LandingBradleyPos);
                Plane.secondsToTake *= 1f / _ins._config.ScaleSpeedPlane;
            }

            internal void SpawnBradley(Vector3 pos)
            {
                Bradley = GameManager.server.CreateEntity("assets/prefabs/npc/m2bradley/bradleyapc.prefab", pos, Quaternion.Euler(_landingBradleyRot)) as BradleyAPC;
                Bradley.enableSaving = false;
                Bradley.Spawn();

                Bradley.myRigidBody.useGravity = false;
                Bradley.myRigidBody.detectCollisions = false;

                Bradley.transform.position = new Vector3(LandingBradleyPos.x, Bradley.transform.position.y, LandingBradleyPos.z);
                Bradley.transform.rotation = Quaternion.Euler(_landingBradleyRot);

                Bradley.InstallPatrolPath(new BasePath());
                Bradley.patrolPath = null;

                Bradley._maxHealth = _ins._config.Bradley.Hp;
                Bradley.health = Bradley._maxHealth;

                Bradley.maxCratesToSpawn = _ins._config.Bradley.CountCrates;

                Bradley.viewDistance = _ins._config.Bradley.ViewDistance;
                Bradley.searchRange = _ins._config.Bradley.SearchRange;

                Bradley.coaxAimCone *= _ins._config.Bradley.CoaxAimCone;
                Bradley.coaxFireRate *= _ins._config.Bradley.CoaxFireRate;
                Bradley.coaxBurstLength = _ins._config.Bradley.CoaxBurstLength;

                Bradley.nextFireTime = _ins._config.Bradley.NextFireTime;
                Bradley.topTurretFireRate = _ins._config.Bradley.TopTurretFireRate;

                Bradley.memoryDuration = _ins._config.Bradley.MemoryDuration;

                BaseEntity parachute1 = GameManager.server.CreateEntity("assets/prefabs/misc/parachute/parachute.prefab");
                parachute1.enableSaving = false;
                parachute1.SetParent(Bradley);
                parachute1.transform.localPosition = new Vector3(0f, 1.278f, -2.667f);
                parachute1.Spawn();
                _parachutes.Add(parachute1);
                BaseEntity parachute2 = GameManager.server.CreateEntity("assets/prefabs/misc/parachute/parachute.prefab");
                parachute2.enableSaving = false;
                parachute2.SetParent(Bradley);
                parachute2.transform.localPosition = new Vector3(0f, 1.278f, 3.374f);
                parachute2.Spawn();
                _parachutes.Add(parachute2);

                Bradley.myRigidBody.AddForce(Vector3.down * 1000000f, ForceMode.Force);
                InvokeRepeating(UpdateBradley, 0, 1f);

                ChechTrash(LandingBradleyPos, 10f);
            }

            private void UpdateBradley()
            {
                if (Bradley.transform.position.y > _ins._config.HeightCh47) Bradley.myRigidBody.AddForce(Vector3.down * 1000000f, ForceMode.Force);
                else Bradley.myRigidBody.AddForce(Vector3.down * 100000f, ForceMode.Force);
                if (Bradley.transform.position.y - LandingBradleyPos.y < 1f)
                {
                    Bradley.transform.position = LandingBradleyPos;
                    Bradley.myRigidBody.useGravity = true;
                    Bradley.myRigidBody.detectCollisions = true;
                    foreach (BaseEntity parachute in _parachutes) if (parachute.IsExists()) parachute.Kill();
                    if (_ins._config.PveMode.Pve && _ins.plugins.Exists("PveMode")) _ins.PveMode.Call("EventAddTanks", _ins.Name, new HashSet<ulong> { Bradley.net.ID.Value });
                    CancelInvoke(UpdateBradley);
                }
            }

            private void SpawnAddBradley()
            {
                Vector3 pos; Quaternion rot;
                GetGlobal(transform, new Vector3(-8.413f, 5.807f, -33.424f), new Vector3(0f, 217.118f, 0f), out pos, out rot);

                ChechTrash(pos, 10f);

                SpawnSmoke(pos);

                AddBradley = GameManager.server.CreateEntity("assets/prefabs/npc/m2bradley/bradleyapc.prefab", pos, rot) as BradleyAPC;
                AddBradley.enableSaving = false;
                AddBradley.Spawn();

                AddBradley.InstallPatrolPath(new BasePath());
                AddBradley.patrolPath = null;

                AddBradley._maxHealth = _ins._config.Bradley.Hp;
                AddBradley.health = AddBradley._maxHealth;

                AddBradley.maxCratesToSpawn = _ins._config.Bradley.CountCrates;

                AddBradley.viewDistance = _ins._config.Bradley.ViewDistance;
                AddBradley.searchRange = _ins._config.Bradley.SearchRange;

                AddBradley.coaxAimCone *= _ins._config.Bradley.CoaxAimCone;
                AddBradley.coaxFireRate *= _ins._config.Bradley.CoaxFireRate;
                AddBradley.coaxBurstLength = _ins._config.Bradley.CoaxBurstLength;

                AddBradley.nextFireTime = _ins._config.Bradley.NextFireTime;
                AddBradley.topTurretFireRate = _ins._config.Bradley.TopTurretFireRate;

                AddBradley.memoryDuration = _ins._config.Bradley.MemoryDuration;

                AddBradleyPos = pos;
            }

            private static void SpawnSmoke(Vector3 pos)
            {
                SmokeGrenade grenade = GameManager.server.CreateEntity("assets/prefabs/tools/smoke grenade/grenade.smoke.deployed.prefab", pos) as SmokeGrenade;
                grenade.enableSaving = false;
                grenade.Spawn();
                grenade.GetComponent<Rigidbody>().useGravity = false;
            }

            private void ChechTrash(Vector3 pos, float radius) { foreach (BaseEntity entity in GetEntities<BaseEntity>(pos, radius, -1)) if (_ins.TrashList.Contains(entity.ShortPrefabName) && entity.IsExists()) entity.Kill(); }

            private static HashSet<T> GetEntities<T>(Vector3 position, float radius, int layerMask) where T : BaseEntity
            {
                HashSet<T> result = new HashSet<T>();
                foreach (Collider collider in Physics.OverlapSphere(position, radius, layerMask))
                {
                    BaseEntity entity = collider.ToBaseEntity();
                    if (entity.IsExists() && entity is T) result.Add(entity as T);
                }
                return result;
            }

            private void FindPhoneMonument()
            {
                foreach (Collider collider in Physics.OverlapSphere(GetGlobalPosition(new Vector3(6.257f, 6.113f, -1.543f)), 1f))
                {
                    BaseEntity entity = collider.ToBaseEntity();
                    if (entity != null && entity is Telephone) PhoneMonument = entity as Telephone;
                }
            }

            internal void SpawnPhone()
            {
                Phone = SpawnEntity("assets/prefabs/voiceaudio/telephone/telephone.deployed.prefab", GetGlobalPosition(new Vector3(56.195f, 16.855f, -6.724f)), Quaternion.identity) as Telephone;
                Phone.UpdateFromInput(1, 0);
            }

            private void SpawnDummy(Vector3 pos, Vector3 rot)
            {
                Dummy = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", pos) as BasePlayer;
                Dummy.viewAngles = rot;
                Dummy.enableSaving = false;
                Dummy.Spawn();
            }

            internal void CallPhone() => Phone.Controller.CallPhone(PhoneMonument.Controller.PhoneNumber);

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

            private void UpdateMarkerForPlayers()
            {
                if (Players.Count == 0) return;
                if (_ins._config.MainPoint.Enabled)
                {
                    HashSet<Vector3> points = new HashSet<Vector3>();
                    if (AddBradley.IsExists()) points.Add(AddBradley.transform.position);
                    if (Bradley.IsExists()) points.Add(Bradley.transform.position);
                    if (StageCh47 == 6)
                    {
                        if (IsAlarm && PhoneMonument.IsExists() && ((Siren.IsExists() && Siren.HasFlag(BaseEntity.Flags.Reserved8)) || (Alarm.IsExists() && Alarm.HasFlag(BaseEntity.Flags.Reserved8)))) points.Add(PhoneMonument.transform.position);
                        if (!IsAlarm && Zombies.Count == 5) foreach (ScientistNPC zombie in Zombies) points.Add(zombie.transform.position);
                    }
                    if (points.Count > 0) foreach (BasePlayer player in Players) foreach (Vector3 point in points) UpdateMarkerForPlayer(player, point, _ins._config.MainPoint);
                    points = null;
                }
                if (_ins._config.AdditionalPoint.Enabled)
                {
                    HashSet<Vector3> points = new HashSet<Vector3>();
                    if (IsAlarm && Zombies.Count > 0 && Zombies.Count < 5) foreach(ScientistNPC zombie in Zombies) points.Add(zombie.transform.position);
                    if (StageCh47 == 7)
                    {
                        foreach (LootContainer crate in Crates) if (crate.IsExists()) points.Add(crate.transform.position);
                        if (HackCrate.IsExists()) points.Add(HackCrate.transform.position);
                    }
                    if (points.Count > 0) foreach (BasePlayer player in Players) foreach (Vector3 point in points) UpdateMarkerForPlayer(player, point, _ins._config.AdditionalPoint);
                    points = null;
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
        }
        #endregion Controller

        #region PathController
        internal class PathController : FacepunchBehaviour
        {
            internal List<Vector3> Paths = new List<Vector3>();
            private float _secondsTaken;
            private float _secondsToTake;
            private float _waypointDone;
            private Vector3 _startPos;
            private Vector3 _endPos;
            private ScientistNPC _npc;

            private void Awake() { _npc = GetComponent<ScientistNPC>(); }

            private void FixedUpdate()
            {
                if (_secondsTaken == 0f)
                {
                    if (Paths.Count == 0)
                    {
                        _startPos = _endPos = Vector3.zero;
                        _ins._controller.Ch47TakeZombies++;
                        enabled = false;
                        _ins._controller.Zombies.Remove(_npc);
                        if (_npc.IsExists()) _npc.Kill();
                        return;
                    }
                    _startPos = _npc.transform.position;
                    if (Paths[0] != _startPos)
                    {
                        _endPos = Paths[0];
                        _secondsToTake = Vector3.Distance(_endPos, _startPos) / _ins._config.Zombies.Speed;
                        _npc.viewAngles = Quaternion.LookRotation(_endPos - _startPos).eulerAngles;
                        _secondsTaken = 0f;
                        _waypointDone = 0f;
                    }
                    Paths.RemoveAt(0);
                }
                if (_startPos != _endPos)
                {
                    _secondsTaken += Time.deltaTime;
                    _waypointDone = Mathf.InverseLerp(0f, _secondsToTake, _secondsTaken);
                    _npc.transform.position = Vector3.Lerp(_startPos, _endPos, _waypointDone);
                    _npc.viewAngles = Quaternion.LookRotation(_endPos - _startPos).eulerAngles;
                    if (_waypointDone >= 1f) _secondsTaken = 0f;
                }
            }
        }
        #endregion PathController

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
            else if (_controller.Zombies.Contains(entity))
            {
                _controller.Zombies.Remove(entity);
                NextTick(() =>
                {
                    if (corpse == null) return;
                    ItemContainer container = corpse.containers[0];
                    if (_config.Zombies.TypeLootTable == 0)
                    {
                        for (int i = container.itemList.Count - 1; i >= 0; i--)
                        {
                            Item item = container.itemList[i];
                            if (item.info.shortname == "halloween.mummysuit" || item.info.shortname == "gloweyes")
                            {
                                item.RemoveFromContainer();
                                item.Remove();
                            }
                        }
                        return;
                    }
                    if (_config.Zombies.TypeLootTable == 2 || _config.Zombies.TypeLootTable == 3)
                    {
                        if (_config.Zombies.IsRemoveCorpse && !corpse.IsDestroyed) corpse.Kill();
                        return;
                    }
                    container.ClearItemsContainer();
                    if (_config.Zombies.TypeLootTable == 4 || _config.Zombies.TypeLootTable == 5) AddToContainerPrefab(container, _config.Zombies.PrefabLootTable);
                    if (_config.Zombies.TypeLootTable == 1 || _config.Zombies.TypeLootTable == 5) AddToContainerItem(container, _config.Zombies.OwnLootTable);
                    if (_config.Zombies.IsRemoveCorpse && !corpse.IsDestroyed) corpse.Kill();
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
            if (_controller.Zombies.Contains(entity))
            {
                if (_config.Zombies.TypeLootTable == 2) return null;
                else return true;
            }
            return null;
        }

        private object OnCustomLootNPC(NetworkableId netID)
        {
            if (_controller == null) return null;
            ScientistNPC entity = _controller.Scientists.FirstOrDefault(x => x.IsExists() && x.net.ID.Value == netID.Value);
            if (entity != null)
            {
                PresetConfig preset = _config.Npc.FirstOrDefault(x => x.Config.Name == entity.displayName);
                if (preset.TypeLootTable == 3) return null;
                else return true;
            }
            if (_controller.Zombies.Any(x => x.IsExists() && x.net.ID.Value == netID.Value))
            {
                if (_config.Zombies.TypeLootTable == 3) return null;
                else return true;
            }
            return null;
        }
        #endregion NPC

        #region Crates
        private bool IsEventBradleyCrate(LootContainer container) => container is LockedByEntCrate && container.ShortPrefabName == "bradley_crate" && (Vector3.Distance(container.transform.position, _controller.LandingBradleyPos) < 10f || (_controller.AddBradleyPos != Vector3.zero && Vector3.Distance(container.transform.position, _controller.AddBradleyPos) < 10f));

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

        private void OnEntitySpawned(HackableLockedCrate crate)
        {
            if (crate != null && Vector2.Distance(new Vector2(crate.transform.position.x, crate.transform.position.z), new Vector2(_controller.DropCratePos.x, _controller.DropCratePos.z)) < 1f)
            {
                _controller.HackCrate = crate;
                crate.hackSeconds = HackableLockedCrate.requiredHackSeconds - _config.HackCrate.UnlockTime;
                if (_config.PveMode.Pve && plugins.Exists("PveMode")) PveMode.Call("EventAddCrates", Name, new HashSet<ulong> { crate.net.ID.Value });
                if (_config.HackCrate.TypeLootTable == 1 || _config.HackCrate.TypeLootTable == 4 || _config.HackCrate.TypeLootTable == 5)
                {
                    NextTick(() =>
                    {
                        crate.inventory.ClearItemsContainer();
                        if (_config.HackCrate.TypeLootTable == 4 || _config.HackCrate.TypeLootTable == 5) AddToContainerPrefab(crate.inventory, _config.HackCrate.PrefabLootTable);
                        if (_config.HackCrate.TypeLootTable == 1 || _config.HackCrate.TypeLootTable == 5) AddToContainerItem(crate.inventory, _config.HackCrate.OwnLootTable);
                    });
                }
            }
        }

        private object CanPopulateLoot(LootContainer container)
        {
            if (container == null || _controller == null) return null;
            if (_controller.Crates.Contains(container))
            {
                if (_config.TypeLootTableCrates == 2) return null;
                else return true;
            }
            else if (container is HackableLockedCrate && container == _controller.HackCrate)
            {
                if (_config.HackCrate.TypeLootTable == 2) return null;
                else return true;
            }
            else if (IsEventBradleyCrate(container))
            {
                if (_config.Bradley.TypeLootTable == 2) return null;
                else return true;
            }
            else return null;
        }

        private object OnCustomLootContainer(NetworkableId netID)
        {
            if (_controller == null) return null;
            if (_controller.Crates.Any(x => x.IsExists() && x.net.ID.Value == netID.Value))
            {
                if (_config.TypeLootTableCrates == 3) return null;
                else return true;
            }
            else if (_controller.HackCrate.IsExists() && _controller.HackCrate.net.ID.Value == netID.Value)
            {
                if (_config.HackCrate.TypeLootTable == 3) return null;
                else return true;
            }
            LootContainer crate = BaseNetworkable.serverEntities.Find(netID) as LootContainer;
            if (crate != null && IsEventBradleyCrate(crate))
            {
                if (_config.Bradley.TypeLootTable == 3) return null;
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
            else if (container is HackableLockedCrate && container == _controller.HackCrate)
            {
                if (_config.HackCrate.TypeLootTable == 6) return null;
                else return true;
            }
            else if (IsEventBradleyCrate(container))
            {
                if (_config.Bradley.TypeLootTable == 6) return null;
                else return true;
            }
            else return null;
        }
        #endregion Crates

        private void AddToContainerPrefab(ItemContainer container, PrefabLootTableConfig lootTable)
        {
            HashSet<string> prefabsInContainer = new HashSet<string>();
            if (lootTable.UseCount)
            {
                int count = 0, max = UnityEngine.Random.Range(lootTable.Min, lootTable.Max + 1);
                while (count < max)
                {
                    foreach (PrefabConfig prefab in lootTable.Prefabs)
                    {
                        if (prefabsInContainer.Count < lootTable.Prefabs.Count && prefabsInContainer.Contains(prefab.PrefabDefinition)) continue;
                        if (UnityEngine.Random.Range(0f, 100f) > prefab.Chance) continue;
                        SpawnIntoContainer(container, prefab.PrefabDefinition);
                        if (!prefabsInContainer.Contains(prefab.PrefabDefinition)) prefabsInContainer.Add(prefab.PrefabDefinition);
                        count++;
                        if (count == max)
                        {
                            prefabsInContainer = null;
                            return;
                        }
                    }
                }
            }
            else
            {
                foreach (PrefabConfig prefab in lootTable.Prefabs)
                {
                    if (prefabsInContainer.Contains(prefab.PrefabDefinition)) continue;
                    if (UnityEngine.Random.Range(0f, 100f) > prefab.Chance) continue;
                    SpawnIntoContainer(container, prefab.PrefabDefinition);
                    prefabsInContainer.Add(prefab.PrefabDefinition);
                }
            }
            prefabsInContainer = null;
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
            HashSet<int> indexMove = new HashSet<int>();
            if (lootTable.UseCount)
            {
                int count = UnityEngine.Random.Range(lootTable.Min, lootTable.Max + 1);
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

            CheckLootTable(_config.Bradley.OwnLootTable);
            CheckPrefabLootTable(_config.Bradley.PrefabLootTable);

            foreach (PresetConfig preset in _config.Npc)
            {
                CheckLootTable(preset.OwnLootTable);
                CheckPrefabLootTable(preset.PrefabLootTable);
            }

            CheckLootTable(_config.Zombies.OwnLootTable);
            CheckPrefabLootTable(_config.Zombies.PrefabLootTable);

            SaveConfig();
        }

        private static void CheckLootTable(LootTableConfig lootTable)
        {
            lootTable.Items = lootTable.Items.OrderByQuickSort(x => x.Chance);
            if (lootTable.Max > lootTable.Items.Count) lootTable.Max = lootTable.Items.Count;
            if (lootTable.Min > lootTable.Max) lootTable.Min = lootTable.Max;
        }

        private void CheckPrefabLootTable(PrefabLootTableConfig lootTable)
        {
            List<PrefabConfig> prefabs = Pool.GetList<PrefabConfig>();
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
            lootTable.Prefabs = prefabs.OrderByQuickSort(x => x.Chance).ToList();
            Pool.FreeList(ref prefabs);
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

        #region NPCKits
        private object OnNpcKits(ScientistNPC npc)
        {
            if (npc == null || _controller == null) return null;
            if (_controller.Zombies.Contains(npc)) return true;
            else return null;
        }
        #endregion NPCKits

        #region NTeleportation
        private object CanTeleport(BasePlayer player, Vector3 to)
        {
            if (_config.NTeleportationInterrupt && _controller != null && (_controller.Players.Contains(player) || Vector3.Distance(_controller.transform.position, to) < _config.Radius)) return GetMessage("NTeleportation", player.UserIDString, _config.Prefix);
            else return null;
        }
        #endregion NTeleportation

        #region BetterNpc
        private object CanBradleySpawnNpc(BradleyAPC bradley)
        {
            if (_controller == null) return null;
            if (Vector3.Distance(bradley.transform.position, _controller.transform.position) < _config.Radius) return true;
            else return null;
        }

        private object CanCh47SpawnNpc(CH47HelicopterAIController ai)
        {
            if (_controller == null) return null;
            if (Vector3.Distance(ai.transform.position, _controller.transform.position) < _config.Radius) return true;
            else return null;
        }
        #endregion BetterNpc

        #region Bradley Tiers
        private object CanBradleyTiersEdit(BradleyAPC bradley)
        {
            if (_controller == null) return null;
            if (Vector3.Distance(bradley.transform.position, _controller.transform.position) < _config.Radius) return true;
            else return null;
        }
        #endregion Bradley Tiers

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
                case "Bradley":
                    AddBalance(playerId, _config.Economy.Bradley);
                    break;
                case "Npc":
                    AddBalance(playerId, _config.Economy.Npc);
                    break;
                case "LockedCrate":
                    AddBalance(playerId, _config.Economy.LockedCrate);
                    break;
                case "Zombie":
                    AddBalance(playerId, _config.Economy.Zombie);
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
            Interface.Oxide.CallHook("OnSatDishEventWinner", winnerId);
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
            new ImageURL { Name = "Npc_KpucTaJl", Url = "Images/Npc_KpucTaJl.png" }
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

        private IEnumerator ProcessDownloadImage(ImageURL image)
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
        [PluginReference] private readonly Plugin NpcSpawn, BetterNpc, PveMode, MonumentOwner;

        private readonly HashSet<string> _hooks = new HashSet<string>
        {
            "OnEntityTakeDamage",
            "CanAffordUpgrade",
            "OnStructureRotate",
            "OnSupplyDropDropped",
            "CanHackCrate",
            "OnCrateHack",
            "CanBradleyApcTarget",
            "OnEntityDeath",
            "CanMountEntity",
            "OnNpcTarget",
            "OnPhoneAnswered",
            "OnPhoneDialTimedOut",
            "OnPhoneDial",
            "OnEntityKill",
            "OnLootEntity",
            "OnPlayerCommand",
            "OnServerCommand",
            "OnCorpsePopulate",
            "CanPopulateLoot",
            "OnCustomLootNPC",
            "OnCustomLootContainer",
            "OnContainerPopulate",
            "OnEntitySpawned",
            "CanEntityTakeDamage",
            "OnNpcKits",
            "CanTeleport",
            "CanBradleySpawnNpc",
            "CanCh47SpawnNpc",
            "CanBradleyTiersEdit"
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

        private static MonumentInfo FindMonumentInfo(Vector3 pos, string name)
        {
            foreach (MonumentInfo monument in TerrainMeta.Path.Monuments) if (monument.displayPhrase.english == name && Vector3.Distance(pos, monument.transform.position) < 1f) return monument;
            return null;
        }
        #endregion Helpers

        #region Commands
        [ChatCommand("satdishstart")]
        private void ChatStartEvent(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                if (!_active) Start();
                else PrintToChat(player, GetMessage("EventActive", player.UserIDString, _config.Prefix));
            }
        }

        [ChatCommand("satdishstop")]
        private void ChatStopEvent(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                if (_controller != null) Finish();
                else Interface.Oxide.ReloadPlugin(Name);
            }
        }

        [ChatCommand("satdishpos")]
        private void ChatCommandPos(BasePlayer player)
        {
            if (!player.IsAdmin || _controller == null) return;
            Vector3 pos = _controller.transform.InverseTransformPoint(player.transform.position);
            Puts($"Position: {pos}");
            PrintToChat(player, $"Position: {pos}");
        }

        [ConsoleCommand("satdishstart")]
        private void ConsoleStartEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
            {
                if (!_active) Start();
                else Puts("This event is active now. To finish this event (satdishstop), then to start the next one");
            }
        }

        [ConsoleCommand("satdishstop")]
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

namespace Oxide.Plugins.SatDishEventExtensionMethods
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