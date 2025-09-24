using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;
using Oxide.Core;
using System.IO;
using Rust;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;
using System;
using System.Linq;
using System.Globalization;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("RustySkills", "__red", "1.1.13")]
    class RustySkills : RustPlugin
    {
        /// <summary>
        /// Имитирует одну секунду для таймеров.
        /// </summary>
        private const int ONE_SECOND_IMIT = 1;

        /// <summary>
        /// Имитирует числовой нуль
        /// </summary>
        private const int INT_NULL = 0;

        [PluginReference]
        private Plugin ImageLibrary;

        #region Config
        /// <summary>
        /// Объект конфигурации плагина
        /// </summary>
        private class PluginConfig
        {
            /// <summary>
            /// Префикс чата для плагина
            /// </summary>
            [JsonProperty("CFG_PLUGIN_CHAT_PREFIX")]
            public string CHAT_PREFIX;

            /// <summary>
            /// Цвет префикса чата для плагина
            /// </summary>
            [JsonProperty("CFG_PLUGIN_CHAT_PREFIX_COLOR")]
            public string CHAT_PREFIX_COLOR;

            /// <summary>
            /// Максимальный уровень умений
            /// </summary>
            [JsonProperty("CFG_SPELL_MAX_LEVEL")]
            public int MAX_LEVEL;

            /// <summary>
            /// Минимальный уровень умений
            /// </summary>
            [JsonProperty("CFG_SPELL_MIN_LEVEL")]
            public int MIN_LEVEL;

            /// <summary>
            /// Модификатор сложности улучшения умений
            /// </summary>
            [JsonProperty("CFG_SPELL_EXP_MOD")]
            public int EXP_MODIFIER;

            /// <summary>
            /// Время в секундах для отката умений при их переключении
            /// </summary>
            [JsonProperty("CFG_SWITCH_COLDOWN_DEFAULT")]
            public int SWITCH_COLDOWN_DEAFULT;

            /// <summary>
            /// Время в секундах для отображения экстра информации (уворот, критический удар)
            /// </summary>
            [JsonProperty("CFG_EXTRA_SHOW_TIME")]
            public int EXTRA_SHOW_TIMER;

            /// <summary>
            /// Возвращает экземпляр создания нового класса конфигурации
            /// </summary>
            /// <returns></returns>
            public static PluginConfig CreateDefault()
            {
                return new PluginConfig()
                {
                    CHAT_PREFIX = "[RS]: ",
                    CHAT_PREFIX_COLOR = "#ff00dd4f",
                    MAX_LEVEL = 5,
                    MIN_LEVEL = 1,
                    EXP_MODIFIER = 10,
                    SWITCH_COLDOWN_DEAFULT = 15,
                    EXTRA_SHOW_TIMER = 2,
                };
            }
        }

        /// <summary>
        /// Сохраняет конфигурацию плагина
        /// </summary>
        protected override void SaveConfig()
        {
            Config.WriteObject(_Config);
        }

        /// <summary>
        /// Загрудает конфигурацию плагина
        /// </summary>
        protected override void LoadConfig()
        {
            base.LoadConfig();

            _Config = Config.ReadObject<PluginConfig>();
        }

        /// <summary>
        /// Загружает конфигурацию плагина по умолчанию
        /// </summary>
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Plugin created a new config file !", this);

            _Config = PluginConfig.CreateDefault();
        }
        #endregion

        #region Spell Data Types
        /// <summary>
        /// Перечисление. Указывает на тип умения
        /// </summary>
        public enum SpellType
        {
            /// <summary>
            /// Пустой тип
            /// </summary>
            Empty = -1,

            /// <summary>
            /// Умение на модификацию урона
            /// </summary>
            Damage = 0,

            /// <summary>
            /// Умение на блокирование некоторого урона
            /// </summary>
            Defense = 1,

            /// <summary>
            /// Умение на нанесение критического удара
            /// </summary>
            Critical = 2,

            /// <summary>
            /// Умение на уворот от полученного урона
            /// </summary>
            Evasion = 4,

            /// <summary>
            /// Умение на возврат урона
            /// </summary>
            Spike = 5,

            /// <summary>
            /// Умение на восстановление жизни от нанесенного урона
            /// </summary>
            Vampiric = 6
        }

        /// <summary>
        /// Главный класс оболочка для умений
        /// </summary>
        public class Spell
        {
            /// <summary>
            /// Возвращает информацию об эффекте умения
            /// </summary>
            public SpellEffect   Effect      { get; set; }

            /// <summary>
            /// Возвращает информацию об умении
            /// </summary>
            public SpellData     Data        { get; set; }

            /// <summary>
            /// Возвращает информацию об уровне умения
            /// </summary>
            public SpellLevel    Level       { get; set; }

            /// <summary>
            /// Конструктор по умолчанию
            /// </summary>
            public Spell()
            {
                Effect = new SpellEffect();
                Data = new SpellData();
                Level = new SpellLevel();
            }

            /// <summary>
            /// Конструктор по умолчанию с иньекцией кода
            /// </summary>
            /// <param name="effect">Экземпляр эффекта</param>
            /// <param name="data">Экземпляр данных</param>
            /// <param name="level">Экземпляр уровня</param>
            public Spell(SpellEffect effect, SpellData data, SpellLevel level)
            {
                Effect = effect;
                Data = data;
                Level = level;
            }

            /// <summary>
            /// Метод - прототип.
            /// Создает новый экземпляр умения с указанными параметрами
            /// </summary>
            /// <param name="effect">Экземпляр эффекта</param>
            /// <param name="data">Экземпляр данных</param>
            /// <param name="level">Экземпляр уровня</param>
            /// <returns></returns>
            public static Spell Create(SpellEffect effect, SpellData data, SpellLevel level)
            {
                return new Spell(effect, data, level);
            }
        }

        /// <summary>
        /// Структура эффектов умения
        /// </summary>
        public class SpellEffect
        {
            /// <summary>
            /// Возвращает тип умения
            /// </summary>
            public SpellType            EffectType      { get; set; }

            /// <summary>
            /// Возвращает текущий бонус умения
            /// </summary>
            public int                  CurrentBonus    { get; set; }

            /// <summary>
            /// Возвращает следующий бонус умения
            /// </summary>
            public int                  NextBonus       { get; set; }

            /// <summary>
            /// Возвращает коллекцию бонусов умения на разных уровнях
            /// </summary>
            public Dictionary<int, float> BonusCollection { get; set; }

            /// <summary>
            /// Конструктор по умолчанию
            /// </summary>
            public SpellEffect()
            {
                EffectType = SpellType.Empty;
                CurrentBonus = 0;
                NextBonus = 0;
                BonusCollection = new Dictionary<int, float>();
            }

            /// <summary>
            /// Создает новый экземпляр эффектов умения с указанными параметрами
            /// </summary>
            /// <param name="type">Тип уменя</param>
            /// <param name="cub">Текущий бонус</param>
            /// <param name="neb">Будущий бонус</param>
            /// <param name="collection">Коллекция бонусов</param>
            /// <returns></returns>
            public static SpellEffect Create(SpellType type, int cub, int neb, Dictionary<int, float> collection)
            {
                return new SpellEffect()
                {
                    EffectType = type,
                    CurrentBonus = cub,
                    NextBonus = neb,
                    BonusCollection = collection
                };
            }

            /// <summary>
            /// Создает новый экземпляр эффектов умения по умолчанию
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            public static SpellEffect CreateDefault(SpellType type)
            {
                SpellEffect effect = new SpellEffect
                {
                    EffectType = type,
                    BonusCollection = CreateDefaultBonus()
                };
                effect.CurrentBonus = effect.CalculateBonus(SpellLevel.MIN_LEVEL);
                effect.NextBonus = effect.CalculateNextBonus(SpellLevel.MIN_LEVEL);

                return effect;
            }

            /// <summary>
            /// Обновляет данные о бонусах
            /// </summary>
            /// <param name="level"></param>
            public void UpdateBonus(int level)
            {
                CurrentBonus = CalculateBonus(level);
                NextBonus = CalculateNextBonus(level);
            }

            /// <summary>
            /// Пересчитывает следующий бонус согласно уровню
            /// </summary>
            /// <param name="level">Уровень</param>
            /// <returns></returns>
            public int CalculateBonus(int level)
            {
                if (BonusCollection.ContainsKey(level))
                {
                    return (int)BonusCollection[level];
                }
                else
                {
                    return default(int);
                }
            }

            /// <summary>
            /// Пересчитывает будущий бонус согласно уровню
            /// </summary>
            /// <param name="level">Уровень</param>
            /// <returns></returns>
            public int CalculateNextBonus(int level)
            {
                if (BonusCollection.ContainsKey(level + 1))
                {
                    return (int)BonusCollection[level + 1];
                }
                else
                {
                    return default(int);
                }
            }

            /// <summary>
            /// Создает бонус по умолчанию
            /// </summary>
            /// <returns></returns>
            private static Dictionary<int, float> CreateDefaultBonus()
            {
                return new Dictionary<int, float>()
                {
                    [1] = 3f,
                    [2] = 6f,
                    [3] = 9f,
                    [4] = 12f,
                    [5] = 15f,
                };
            }
        }

        /// <summary>
        /// Структура для хранения данных об умении
        /// </summary>
        public class SpellData
        {
            /// <summary>
            /// Уникальный идентификатор умения
            /// </summary>
            public int      Id          { get; set; }

            /// <summary>
            /// Картинка умения
            /// </summary>
            public string   Icon        { get; set; }

            /// <summary>
            /// Название умения
            /// </summary>
            public string   Name        { get; set; }

            /// <summary>
            /// Описание умения
            /// </summary>
            public string   Description { get; set; }

            /// <summary>
            /// Массив данных об информации умения на разных уровнях
            /// </summary>
            public string[] Help        { get; set; }
            public bool IsSwitched { get; internal set; }

            /// <summary>
            /// Конструктор по умолчанию
            /// </summary>
            public SpellData()
            {
                Id = -1;
                Icon = "";
                Name = "";
                Description = "";
                Help = new string[6];
            }

            /// <summary>
            /// Создает новый экземпляр данных умения с заданными 
            /// параметрами
            /// </summary>
            /// <param name="id">Идентификатор</param>
            /// <param name="icon">Картинка</param>
            /// <param name="name">Название</param>
            /// <param name="desc">Описание</param>
            /// <param name="help">Массив информации об уровнях</param>
            /// <returns></returns>
            public static SpellData Create(int id, string icon, string name, string desc, string[] help)
            {
                return new SpellData()
                {
                    Id = id,
                    Icon = icon,
                    Name = name,
                    Description = desc,
                    Help = help
                };
            }

            /// <summary>
            /// Обновлеяет описание умения
            /// </summary>
            /// <param name="newDesc"></param>
            public void Update(string newDesc)
            {
                Description = newDesc;
            }

            /// <summary>
            /// Обновляет массив информации об умении
            /// </summary>
            /// <param name="newHelp"></param>
            public void Update(string[] newHelp)
            {
                Help = newHelp;
            }
        }

        /// <summary>
        /// Структура для хранения информации об уровне умения
        /// </summary>
        public class SpellLevel
        {
            /// <summary>
            /// Минимальный уровень умения
            /// </summary>
            public const int MIN_LEVEL    = 1;

            /// <summary>
            /// Максимальный уровень умения
            /// </summary>
            public const int MAX_LEVEL    = 5;

            /// <summary>
            /// Модификатор улучшения умения
            /// </summary>
            public const int EXP_MODIFIER = 140;

            /// <summary>
            /// Возвращает текущий уровень умения
            /// </summary>
            public int Level      { get; set; }

            /// <summary>
            /// Возвращает текущее количество опыта умения
            /// </summary>
            public int CurrentExp { get; set; }

            /// <summary>
            /// Возвращает количество опыта требуемое для повышения уровня умения
            /// </summary>
            public int NeededExp  { get; set; }

            /// <summary>
            /// Конструктор по умолчанию
            /// </summary>
            public SpellLevel()
            {
                Level = -1;
                CurrentExp = -1;
                NeededExp = -1;
            }

            /// <summary>
            /// Переставляет курсор уровня на указанную позицию
            /// </summary>
            /// <param name="level">позиция уровня</param>
            public void Seek(int level)
            {
                Level = level;
            }

            /// <summary>
            /// Переставляет курсор опыта на указанные позиции
            /// </summary>
            /// <param name="currentExp">текущее состояние опыта</param>
            /// <param name="neededExp">требуемое состояние опыта</param>
            public void Seek(int currentExp, int neededExp)
            {
                CurrentExp = currentExp;
                NeededExp = neededExp;
            }

            /// <summary>
            /// Итерирует уровень умения
            /// </summary>
            public void IterLevel()
            {
                if (Level >= MAX_LEVEL)
                {
                    return;
                }

                Level++;
                Seek(0, GetNeededExp());
            }
            /// <summary>
            /// Итерирует текущее количество опыта уровня
            /// </summary>
            public void IterExp()
            {
                if (CurrentExp < NeededExp)
                {
                    CurrentExp++;
                }

                if (CurrentExp >= NeededExp)
                {
                    IterLevel();
                }
            }

            /// <summary>
            /// Устанавливает значение текущего количества опыта на указанное
            /// </summary>
            /// <param name="exp">количество опыта</param>
            public void GiveExp(int exp)
            {
                if (CurrentExp < NeededExp)
                {
                    CurrentExp += exp;
                }

                if (CurrentExp >= NeededExp)
                {
                    IterLevel();
                }
            }

            /// <summary>
            /// Возвращает результат формулы для пересчета требуемого количество опыта
            /// </summary>
            /// <returns></returns>
            public int GetNeededExp()
            {
                return ((Level + (Level + 1)) + EXP_MODIFIER);
            }

            /// <summary>
            /// Создает экземпляр класса по умолчанию для текущего объекта
            /// </summary>
            /// <returns></returns>
            public static SpellLevel CreateDefault()
            {
                return new SpellLevel()
                {
                    Level = 1,
                    CurrentExp = 0,
                    NeededExp = 3
                };
            }

            /// <summary>
            /// Создает новый экземпляр в соответствии с указанными параметрами
            /// </summary>
            /// <param name="level">Уровень</param>
            /// <param name="currentExp">текущее количество опыта</param>
            /// <param name="neededExp">требуемое количество опыта</param>
            /// <returns></returns>
            public static SpellLevel CreateCustom(int level, int currentExp, int neededExp)
            {
                return new SpellLevel()
                {
                    Level = level,
                    CurrentExp = currentExp,
                    NeededExp = neededExp
                };
            }

            /// <summary>
            /// Возвращает строковое представление для текущего объекта
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"Level: {Level}, CurrentExp: {CurrentExp}, NeededExp: {NeededExp}";
            }
        }
        #endregion

        #region Factory
        /// <summary>
        /// Список уникальных идентификаторов умений
        /// </summary>
        public enum Spells
        {
            /// <summary>
            /// Ярость
            /// </summary>
            Rage     = 0,

            /// <summary>
            /// Защита
            /// </summary>
            Shield   = 1,

            /// <summary>
            /// Уклонение
            /// </summary>
            Evasion  = 2,

            /// <summary>
            /// Контр-удар
            /// </summary>
            Spike    = 3,

            /// <summary>
            /// Вампирик
            /// </summary>
            Vampiric = 4,

            /// <summary>
            /// Критический удар
            /// </summary>
            Critical = 5
        }

        /// <summary>
        /// Класс для реализации прототипа.
        /// Создает умения по умолчанию
        /// </summary>
        public class SpellFactory
        {
            /// <summary>
            /// Создает умение "Ярость"
            /// </summary>
            /// <returns></returns>
            public static Spell CreateRageSpell()
            {
                return Spell.Create(
                    SpellEffect.CreateDefault(SpellType.Damage),
                    SpellData.Create((int)Spells.Rage, "icon", "Мощь", "Увеличивает наносимый вами урон по игрокам и существам", new string[]
                    {
                        "1Lvl: 3%",
                        "2Lvl: 6%",
                        "3Lvl: 9%",
                        "4Lvl: 12%",
                        "5Lvl: 15%"
                    }),
                    SpellLevel.CreateDefault());
            }

            /// <summary>
            /// Создает умение "Защита"
            /// </summary>
            /// <returns></returns>
            public static Spell CreateShieldSpell()
            {
                return Spell.Create(
                    SpellEffect.CreateDefault(SpellType.Defense),
                    SpellData.Create((int)Spells.Shield, "icon", "Крепость", "Блокирует некоторое количество урона получаемое персонажем от игровых существ.", new string[]
                    {
                        "1Lvl: 3%",
                        "2Lvl: 6%",
                        "3Lvl: 9%",
                        "4Lvl: 12%",
                        "5Lvl: 15%"
                    }),
                    SpellLevel.CreateDefault());
            }

            /// <summary>
            /// Создает умение "Контр-удар"
            /// </summary>
            /// <returns></returns>
            public static Spell CreateSpikeSpell()
            {
                return Spell.Create(
                     SpellEffect.CreateDefault(SpellType.Spike),
                     SpellData.Create((int)Spells.Spike, "icon", "Отражение", "С шансом равным уровню умения Вы возвращаете урон атакующему Вас игровому существу.", new string[]
                     {
                        "1Lvl: 3%",
                        "2Lvl: 6%",
                        "3Lvl: 9%",
                        "4Lvl: 12%",
                        "5Lvl: 15%"
                     }),
                     SpellLevel.CreateDefault());
            }

            /// <summary>
            /// Создает умение "Вампирик"
            /// </summary>
            /// <returns></returns>
            public static Spell CreateVampiricSpell()
            {
                return Spell.Create(
                     SpellEffect.CreateDefault(SpellType.Vampiric),
                     SpellData.Create((int)Spells.Vampiric, "icon", "Вампиризм", "Восстанавливает Ваше здоровье от наносимых Вами атак", new string[]
                     {
                        "1Lvl: 3%",
                        "2Lvl: 6%",
                        "3Lvl: 9%",
                        "4Lvl: 12%",
                        "5Lvl: 15%"
                     }),
                     SpellLevel.CreateDefault());
            }

            /// <summary>
            /// Создает умение "Критический удар"
            /// </summary>
            /// <returns></returns>
            public static Spell CreateCriticalSpell()
            {
                SpellEffect effect = new SpellEffect
                {
                    EffectType = SpellType.Critical,
                    BonusCollection = new Dictionary<int, float>()
                    {
                        [1] = 2,
                        [2] = 2.2f,
                        [3] = 2.3f,
                        [4] = 2.4f,
                        [5] = 2.7f,
                    }
                };
                effect.CurrentBonus = effect.CalculateBonus(SpellLevel.MIN_LEVEL);
                effect.NextBonus = effect.CalculateNextBonus(SpellLevel.MIN_LEVEL);

                return Spell.Create(
                     effect,
                     SpellData.Create((int)Spells.Critical, "icon", "Фокусировка", "С некоторой вероятностью Вы можете нанести своей цели дополнительный урон", new string[]
                     {
                        "1Lvl: x2",
                        "2Lvl: x2.2",
                        "3Lvl: x2.3",
                        "4Lvl: x2.4",
                        "5Lvl: x2.7"
                     }),
                     SpellLevel.CreateDefault());
            }

            /// <summary>
            /// Создает умение "Уклонение"
            /// </summary>
            /// <returns></returns>
            public static Spell CreateEvasionSpell()
            {
                return Spell.Create(
                     SpellEffect.CreateDefault(SpellType.Evasion),
                     SpellData.Create((int)Spells.Evasion, "icon", "Ловкость", "С некоторой вероятностью Вы можете увернуться от урона получаемого игровыми существами", new string[]
                     {
                        "1Lvl: 3%",
                        "2Lvl: 6%",
                        "3Lvl: 9%",
                        "4Lvl: 12%",
                        "5Lvl: 15%"
                     }),
                     SpellLevel.CreateDefault());
            }

            /// <summary>
            /// Создает умение согласно указанному идентификатору
            /// </summary>
            /// <param name="id">идентификатор умения</param>
            /// <returns></returns>
            public static Spell CreateByID(int id)
            {
                if (id == (int)Spells.Rage)
                {
                    return CreateRageSpell();
                }
                else if (id == (int)Spells.Shield)
                {
                    return CreateShieldSpell();
                }
                else if (id == (int)Spells.Critical)
                {
                    return CreateCriticalSpell();
                }
                else if (id == (int)Spells.Evasion)
                {
                    return CreateEvasionSpell();
                }
                else if (id == (int)Spells.Vampiric)
                {
                    return CreateVampiricSpell();
                }
                else if (id == (int)Spells.Spike)
                {
                    return CreateSpikeSpell();
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Используется для хранения данных об умениях игроков
        /// </summary>
        [JsonObject]
        public class SpellPlayerStore
        {
            /// <summary>
            /// Возвращает уникальный идентификатор STEAM игрока
            /// </summary>
            public string PlayerSteamId { get; set; }

            /// <summary>
            /// Возвращает имя игрока
            /// </summary>
            public string PlayerName    { get; set; }

            /// <summary>
            /// Возвращает текущее активное умение для игрока
            /// !!МОЖЕТ БЫТЬ NULL
            /// </summary>
            public Spell  ActiveSpell   { get; set; }

            /// <summary>
            /// Возвращает список всех умений игрока
            /// </summary>
            public List<Spell> AllSpells { get; set; }

            /// <summary>
            /// Конструктор по умолчанию
            /// </summary>
            public SpellPlayerStore()
            {
                PlayerSteamId = "";
                PlayerName = "";
                ActiveSpell = null;
            }

            /// <summary>
            /// Конструктор с иньекцией кода
            /// </summary>
            /// <param name="steam">иденртфикатор steam</param>
            /// <param name="name">имя игрока</param>
            /// <param name="active">активное умение</param>
            /// <param name="spells">список всех умений</param>
            public SpellPlayerStore(string steam, string name, Spell active, List<Spell> spells)
            {
                PlayerSteamId = steam;
                PlayerName = name;
                ActiveSpell = active;
                AllSpells = spells;
            }
        }

        /// <summary>
        /// Контролёр эффектов для текущего плагина
        /// </summary>
        public static class EffectController
        {
            /// <summary>
            /// Возвращает результат скалирования по формуле 
            /// для выдачи критического урона
            /// </summary>
            /// <param name="current">текущее значение бонуса</param>
            /// <returns></returns>
            public static float GetCriticalScale(int current)
            {
                return current;
            }

            /// <summary>
            /// Возвращает результат вычитания по формуле
            /// для восполнение здоровья
            /// </summary>
            /// <param name="total">нанесенный урон</param>
            /// <param name="bonus">текущее значение бонуса</param>
            /// <returns></returns>
            public static float GetVampireCount(float total, int bonus)
            {
                return (total / 100f) * bonus;
            }

            public static bool IsAction(int current)
            {
                int result = UnityEngine.Random.Range(0, 100);
                if (result <= current)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            /// <summary>
            /// Возвращает результат скалирования по формуле для 
            /// увеличения наносимого урона
            /// </summary>
            /// <param name="current">текущее значение бонуса</param>
            /// <returns></returns>
            public static float GetAttackScale(int current)
            {
                return (1 + ((float)current / 100));
            }

            /// <summary>
            /// Возвращает результат скалирования по формуле для
            /// блокирования определенного количества урона
            /// </summary>
            /// <param name="current">текущее значение бонуса</param>
            /// <returns></returns>
            public static float GetBlockScale(int current)
            {
                return (1 - ((float)current / 100));
            }

            /// <summary>
            /// Возвращает результат скалирования по формуле
            /// для возврата определенного количества урона
            /// </summary>
            /// <param name="total">общее количество нанесенного урона</param>
            /// <param name="current">текущее значение бонуса</param>
            /// <returns></returns>
            public static float GetSpikeScale(float total, int current)
            {
                float dmg = ((total / 100f) * current);
                return (total * dmg);
            }
        }

        public class SwitcheableData
        {
            public bool IsSpellSwitched;
            public int ExpireSeconds;
        }
        #endregion

        #region Variables
        /// <summary>
        /// Обозначает строковое представление для привилегии "админ"
        /// </summary>
        private const string PERMISSION_ADMIN = "rustyskills.admin";

        /// <summary>
        /// Обозначает строковое представление для привилегии "вип"
        /// </summary>
        private const string PERMISSION_VIP = "rustyskills.vip";

        /// <summary>
        /// Множители для привилегий
        /// </summary>
        private Dictionary<string, int> PermissionMultipliers;

        /// <summary>
        /// Активные умений персонажей
        /// </summary>
        private Dictionary<BasePlayer, Spell> ActiveSpells;

        /// <summary>
        /// Хранилище всех персонажей, которые взаимодействиуют с плагином
        /// </summary>
        private Dictionary<BasePlayer, SpellPlayerStore> OnlinePlayersStore;

        private Dictionary<BasePlayer, SwitcheableData> SpellsSwitcher;

        /// <summary>
        /// Объект конфигурации плагина
        /// </summary>
        private PluginConfig _Config;
        #endregion

        #region Initialization

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public RustySkills()
        {
            ActiveSpells = new Dictionary<BasePlayer, Spell>();
            PermissionMultipliers = new Dictionary<string, int>();
            OnlinePlayersStore = new Dictionary<BasePlayer, SpellPlayerStore>();
            SpellsSwitcher = new Dictionary<BasePlayer, SwitcheableData>();

            PrintWarning("Initializing by default constructor !");

            RegisterPermissions();
        }

        /// <summary>
        /// Регистрирует привилегии для плагина
        /// </summary>
        private void RegisterPermissions()
        {
            permission.RegisterPermission(PERMISSION_ADMIN, this);
            permission.RegisterPermission(PERMISSION_VIP, this);

            PermissionMultipliers.Add(PERMISSION_ADMIN, 3);
            PermissionMultipliers.Add(PERMISSION_VIP, 2);
        }
        #endregion

        #region Instruments
        /// <summary>
        /// Сохраняет всех игроков на сервере
        /// </summary>
        private void SaveAllPlayers()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (IsActiveSpell(player))
                {
                    SavePlayer(player);
                }
            }
        }

        /// <summary>
        /// Загружает всех игроков на сервере
        /// </summary>
        private void LoadAllPlayers()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                LoadPlayer(player);
            }
        }
        #endregion

        #region Hooks

        //Server hook
        /// <summary>
        /// Присходит при загрузе плагина
        /// </summary>
        void Loaded()
        {
            LoadAllPlayers();
        }

        /// <summary>
        /// Происходит при выгрузке плагина
        /// </summary>
        void Unload()
        {
            SaveAllPlayers();

            foreach(var player in BasePlayer.activePlayerList)
            {
                if(OnlinePlayersStore.ContainsKey(player))
                {
                    DestroyExpLine(player);
                    DissectPanels(player);
                    DestroyExtraInfoPanel(player);
                }
            }
        }

        /// <summary>
        /// Происходит при инициализации игрока
        /// </summary>
        /// <param name="player"></param>
        void OnPlayerInit(BasePlayer player)
        {
            LoadPlayer(player);

            SetPanels(player);

            if(SpellsSwitcher.ContainsKey(player))
            {
                SpellsSwitcher.Remove(player);
            }

            SpellsSwitcher.Add(player, new SwitcheableData());
        }

        /// <summary>
        /// Происходит перед получение урона
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="info"></param>
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (info.InitiatorPlayer == null)
            {
                return;
            }
            BasePlayer attacker = info.InitiatorPlayer;

            if (entity.ToPlayer() == null)
            {
                return;
            }
            BasePlayer victim = entity.ToPlayer();

            if (attacker == victim)
            {
                return;
            }

            if (IsActiveSpell(attacker, SpellType.Vampiric))
            {
                Spell aSpell = ActiveSpells[attacker];

                float vampire = EffectController.GetVampireCount(info.damageTypes.Total(), aSpell.Effect.CurrentBonus);
                attacker.Heal(vampire);

                if (EffectController.IsAction(3))
                {
                    AddExpToSpell(attacker, aSpell.Data.Id, 1);
                }

#if PRAGMA_DEBUG
                SendReply(attacker, $"Вы восполнили себе здоровье в объеме: {vampire}");
#endif
            }

            if (IsActiveSpell(attacker, SpellType.Critical))
            {
                Spell aSpell = ActiveSpells[attacker];

                if(EffectController.IsAction(aSpell.Effect.CurrentBonus))
                {
                    info.damageTypes.ScaleAll(EffectController.GetCriticalScale(aSpell.Effect.CurrentBonus));

                    AddExpToSpell(attacker, aSpell.Data.Id, 1);

                    info.damageTypes.ScaleAll(aSpell.Effect.CurrentBonus);

                    //SendReply(attacker, $"Вы нанесли критический урон {info.damageTypes.Total()}");

                    SetExtraInfoPanel(attacker, "Крит. удар !");
                }
            }

            if (IsActiveSpell(attacker, SpellType.Damage))
            {
                Spell aSpell = ActiveSpells[attacker];

                float old = info.damageTypes.Total(); //DEBUG
                info.damageTypes.ScaleAll(EffectController.GetAttackScale(aSpell.Effect.CurrentBonus));
#if PRAGMA_DEBUG
                SendReply(attacker, $"Old: {old}, Mult.: {multiplier}, Total: {info.damageTypes.Total()}");
#endif

                if(EffectController.IsAction(3))
                {
                    AddExpToSpell(attacker, aSpell.Data.Id, 1);
                }
            }

            if (IsActiveSpell(victim))
            {
                Spell spell = ActiveSpells[victim];

                if (IsActiveSpell(victim, SpellType.Defense))
                {
                    float old = info.damageTypes.Total(); //DEBUG
                    info.damageTypes.ScaleAll(EffectController.GetBlockScale(spell.Effect.CurrentBonus));
#if PRAGMA_DEBUG
                    SendReply(victim, $"Old: {old}, Mult.: {multiplier}, Total: {info.damageTypes.Total()}");
#endif
                }
                else if (IsActiveSpell(victim, SpellType.Evasion))
                {
                    if (EffectController.IsAction(spell.Effect.CurrentBonus))
                    {
                        info.damageTypes.ScaleAll(INT_NULL);
#if PRAGMA_DEBUG
                        SendReply(victim, $"<color=#ff0000ff>[SK]</color>: <color=#008000ff>Вы успешно увернулись от атаки</color>");
#endif
                        SetExtraInfoPanel(attacker, "Уклонение");
                    }
                }
                else if (IsActiveSpell(victim, SpellType.Spike))
                {
                    float ret = EffectController.GetSpikeScale(info.damageTypes.Total(), spell.Effect.CurrentBonus);
                    attacker.Hurt(ret);

#if PRAGMA_DEBUG
                    SendReply(victim, $"Ваш противник получил возврат урона в количестве: {ret}");
#endif
                    SetExtraInfoPanel(attacker, "Возврат урона !");
                }

                if (EffectController.IsAction(3))
                {
                    AddExpToSpell(victim, spell.Data.Id, 1);
                }
            }
        }

        /// <summary>
        /// Происходит при отключении игрока от сервера
        /// </summary>
        /// <param name="player"></param>
        /// <param name="reason"></param>
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            SavePlayer(player);
            ActiveSpells.Remove(player);
            OnlinePlayersStore.Remove(player);
        }
        #endregion

        #region Data Store
        /// <summary>
        /// Назначает активное умение указанному игроку
        /// </summary>
        /// <param name="player">игрок</param>
        /// <param name="active">активное умение</param>
        public void SetActive(BasePlayer player, Spell active)
        {
            if (OnlinePlayersStore.ContainsKey(player))
            {
                AddToStore(player, OnlinePlayersStore[player].ActiveSpell.Data.Id, OnlinePlayersStore[player].ActiveSpell);

                OnlinePlayersStore[player].ActiveSpell = active;
            }
        }

        /// <summary>
        /// Добавляет указанное умение в коллекцию умений игрока
        /// </summary>
        /// <param name="player">игрок</param>
        /// <param name="id">идентификатор умения</param>
        /// <param name="spell">умение</param>
        public void AddToStore(BasePlayer player, int id, Spell spell)
        {
            if (spell == null)
            {
                return;
            }
            if (id == -1)
            {
                return;
            }

            if (OnlinePlayersStore.ContainsKey(player))
            {
                if (OnlinePlayersStore[player].AllSpells.Contains(spell))
                {
                    for (int i = 0; i < OnlinePlayersStore[player].AllSpells.Count; i++)
                    {
                        if (OnlinePlayersStore[player].AllSpells[i].Data.Id == id)
                        {
                            OnlinePlayersStore[player].AllSpells[i] = spell;
                        }
                    }
                }
                else
                {
                    bool reWrite = false;
                    for (int i = 0; i < OnlinePlayersStore[player].AllSpells.Count; i++)
                    {
                        if (OnlinePlayersStore[player].AllSpells[i].Data.Name == null)
                        {
                            OnlinePlayersStore[player].AllSpells[i] = spell;

                            reWrite = true;
                        }
                    }

                    if (!reWrite)
                    {
                        OnlinePlayersStore[player].AllSpells.Add(spell);
                    }
                }
            }
        }

        /// <summary>
        /// Загружает указанного игрока
        /// </summary>
        /// <param name="player">игрок</param>
        private void LoadPlayer(BasePlayer player)
        {
            SpellPlayerStore store;
            List<Spell> loadeable = Interface.Oxide.DataFileSystem.ReadObject<List<Spell>>(Title + "\\" + player.displayName + "_spells");

            if (loadeable != null)
            {
                int count = 0;
                foreach (var spell in loadeable)
                {
                    PrintWarning($"[{player.displayName}_spells]: Id:{spell.Data.Id}, Name:{spell.Data.Name}, Lvl:{spell.Level.Level}");
                    count++;
                }
                PrintWarning($"Finding '{count}' spells for player: '{player.displayName}'");

                if(count == 0)
                {
                    loadeable = null;
                    loadeable = new List<Spell>()
                    {
                        SpellFactory.CreateRageSpell(),
                        SpellFactory.CreateShieldSpell(),
                        SpellFactory.CreateCriticalSpell(),
                        SpellFactory.CreateVampiricSpell(),
                        SpellFactory.CreateEvasionSpell(),
                        SpellFactory.CreateSpikeSpell()
                    };
                }
            }

            store = new SpellPlayerStore(player.UserIDString, player.displayName, null, loadeable);
            AddToStore(player, store);

            timer.Once(10, () =>
            {
                SetPanels(player);

                try
                {
                    SpellsSwitcher.Add(player, new SwitcheableData());
                }
                catch(Exception) { }
            });
        }

        /// <summary>
        /// Сохраняет указанного игрока
        /// </summary>
        /// <param name="player">игрок</param>
        private void SavePlayer(BasePlayer player)
        {
            if (OnlinePlayersStore.ContainsKey(player))
            {
                PrintWarning($"Saving player({player.displayName}) and him spell store");

                Interface.Oxide.DataFileSystem.WriteObject(Title + "\\" + player.displayName + "_spells", OnlinePlayersStore[player].AllSpells);
            }
        }
        #endregion

        #region Spells Store
        /// <summary>
        /// Добавляет хранилище для хранения в коллекцию
        /// </summary>
        /// <param name="player"></param>
        /// <param name="spell"></param>
        public void AddToStore(BasePlayer player, SpellPlayerStore spell)
        {
            if (!OnlinePlayersStore.ContainsKey(player))
            {
                OnlinePlayersStore.Add(player, spell);
            }
        }

        /// <summary>
        /// Удаляет хранилище из коллекции хранения
        /// </summary>
        /// <param name="player"></param>
        public void RemoveFromStore(BasePlayer player)
        {
            if (!OnlinePlayersStore.ContainsKey(player))
            {
                return;
            }

            OnlinePlayersStore.Remove(player);
        }

        /// <summary>
        /// Получает хранилище указанного игрока из коллекции
        /// </summary>
        /// <param name="player">игрок</param>
        /// <returns></returns>
        public SpellPlayerStore GetFromStore(BasePlayer player)
        {
            if (!OnlinePlayersStore.ContainsKey(player))
            {
                return null;
            }

            return OnlinePlayersStore[player];
        }

        /// <summary>
        /// Возвращает указанное умение из хранилища указанного игрока
        /// </summary>
        /// <param name="player">игрок</param>
        /// <param name="store">хранилище</param>
        /// <param name="spellId">идентификатор умения</param>
        /// <returns></returns>
        public Spell GetSpellFromStore(BasePlayer player, SpellPlayerStore store, int spellId)
        {
            Spell spell = null;

            if (store != null)
            {
                if (store.AllSpells.Count != 0)
                {
                    for (int i = 0; i < store.AllSpells.Count; i++)
                    {
                        if (store.AllSpells[i].Data.Id == spellId)
                        {
                            if (store.AllSpells[i].Data.Name == null)
                            {
                                store.AllSpells.Remove(store.AllSpells[i]);
                                PrintWarning("Null spell is deleted");

                                continue;
                            }

                            spell = store.AllSpells[i];

                            //PrintWarning($"Finded spell for player: '{player.displayName}' spell: '{store.AllSpells[i].Data.Name}'");

                            break;
                        }
                    }
                }
            }

            if (spell == null)
            {
                return null;
            }
            else
            {
                return spell;
            }
        }
        #endregion

        #region Image Library
        void OnServerInitialized()
        {
            try
            {
                ImageLibrary.Call("isLoaded", null);
            }
            catch (Exception)
            {
                PrintWarning("No Image Library.. load ImageLibrary to use this Plugin", Name);

                Interface.Oxide.UnloadPlugin(Name);
                return;
            }

            AddImage("http://zehack.ru/rustyskills/attack.png", "attack_icon");
            AddImage("http://zehack.ru/rustyskills/shield.png", "shield_icon");
            AddImage("http://zehack.ru/rustyskills/critical.png", "critical_icon");
            AddImage("http://zehack.ru/rustyskills/vampiric.png", "vampiric_icon");
            AddImage("http://zehack.ru/rustyskills/evasion.png", "evasion_icon");
            AddImage("http://zehack.ru/rustyskills/spike.png", "spike_icon");
        }


        //ADD THESE
        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        #endregion

        #region GUI & Timers

        public class UI
        {
            public static CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool useCursor = false, string parent = "Overlay")
            {
                var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panelName
                    }
                };
                return NewElement;
            }

            public static void LoadImage(ref CuiElementContainer container, string panel, string url, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    FadeOut = 0.15f,
                    Components =
                    {
                        new CuiRawImageComponent { Url = url, FadeIn = 0.3f },
                        new CuiRectTransformComponent { AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }

            public static void CreateInput(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, bool password, int charLimit, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiInputFieldComponent { Text = text, FontSize = size, Align = align, Color = color, Command = command, IsPassword = password, CharsLimit = charLimit},
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }

            public static void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                panel);
            }

            public static void CreateText(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);
            }

            public static void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 1.0f },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }

            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                {
                    hexColor = hexColor.TrimStart('#');
                }

                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }

        private string SpellOverlayName = "RustySpellUI";
        private string SpellImgsOverlayName = "RustySpellIMG";
        private string ProgressOverlayName = "RustySpellExpUI";
        private string TimerColdownOverlayName = "RustySpellCDTimer";

        private string TextColor = GetRustColor("#ffffff", 0.8f);
        private string ClearBGColor = GetRustColor("#ffffff", 0.0f);
        private string BackgroundColor = GetRustColor("#b3b3b3", 0.05f);
        private string BackgroundColor1 = GetRustColor("#b3b3b3", 0f);
        public string TextColourLearned = "#27ae60";
        public string TextColourNotLearned = "#e74c3c";
        public string HudColourLevel = "#CD7C41";

        public string Green = "#95BB42";
        public string Magenta = "800000ff";
        public string Teal = "#008080ff";
        public string Red = "#ff0000ff";

        public static string GetRustColor(string hexColor, float alpha)
        {
            if (hexColor.StartsWith("#"))
            {
                hexColor = hexColor.TrimStart('#');
            }

            int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
            return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
        }

        /// <summary>
        /// Список экстра информации для вывода
        /// </summary>
        public enum ExtraInfo
        {
            /// <summary>
            /// Критический урон
            /// </summary>
            Critical = 0,

            /// <summary>
            /// Улонение
            /// </summary>
            Evasion = 1,

            /// <summary>
            /// Восполнение жизни
            /// </summary>
            Vampire = 2
        }

        /// <summary>
        /// Создает панели по умолчанию для игрока
        /// </summary>
        /// <param name="player">игрок</param>
        private void SetPanels(BasePlayer player)
        {
            DissectPanels(player);

            var elementValue = UI.CreateElementContainer(SpellOverlayName, BackgroundColor, 0.010f + " " + 0.025f, 0.300f + " " + 0.105f);

            //ATTACK ICON
            CuiElement AttackIcon = new CuiElement
            {
                Name = "AttackIcon",
                Parent = SpellOverlayName,
                Components = {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("attack_icon"),
                            Color = "0.75 0.75 0.75 1.00"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.02 0.10",
                            AnchorMax = "0.17 0.95"
                        }
                }
            };
            elementValue.Add(AttackIcon);
            UI.CreateText(ref elementValue, SpellOverlayName, TextColor, $"Lv.{GetSpellByID(player, 0).Level.Level}({GetSpellByID(player, 0).Effect.CurrentBonus}%)", 13, "0.03 0.10", "0.17 0.95", TextAnchor.LowerLeft);
            //END ATTACK ICON

            CuiElement DefenseIcon = new CuiElement
            {
                Name = "DefenseIcon",
                Parent = SpellOverlayName,
                Components = {
                        new CuiRawImageComponent {
                            Png = GetImage("shield_icon"),
                            Color = "0.75 0.75 0.75 1.00"
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.18 0.10",
                        AnchorMax = "0.33 0.95"
                        }
                }
            };
            elementValue.Add(DefenseIcon);
            UI.CreateText(ref elementValue, SpellOverlayName, TextColor, $"Lv.{GetSpellByID(player, (int)Spells.Shield).Level.Level}({GetSpellByID(player, (int)Spells.Shield).Effect.CurrentBonus}%)", 13, "0.19 0.10", "0.34 0.95", TextAnchor.LowerLeft);

            CuiElement CriticalIcon = new CuiElement
            {
                Name = "CriticalIcon",
                Parent = SpellOverlayName,
                Components = {
                        new CuiRawImageComponent {
                            Png = GetImage("critical_icon"),
                            Color = "0.75 0.75 0.75 1.00"
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.34 0.10",
                        AnchorMax = "0.49 0.95"
                        }
                }
            };
            elementValue.Add(CriticalIcon);
            UI.CreateText(ref elementValue, SpellOverlayName, TextColor, $"Lv.{GetSpellByID(player, (int)Spells.Critical).Level.Level}({GetSpellByID(player, (int)Spells.Critical).Effect.CurrentBonus}%)", 13, "0.35 0.10", "0.52 0.95", TextAnchor.LowerLeft);

            CuiElement VampiricIcon = new CuiElement
            {
                Name = "VampiricIcon",
                Parent = SpellOverlayName,
                Components = {
                        new CuiRawImageComponent {
                            Png = GetImage("vampiric_icon"),
                            Color = "0.75 0.75 0.75 1.00"
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.50 0.10",
                        AnchorMax = "0.65 0.95"
                        }
                }
            };
            elementValue.Add(VampiricIcon);
            UI.CreateText(ref elementValue, SpellOverlayName, TextColor, $"Lv.{GetSpellByID(player, (int)Spells.Vampiric).Level.Level}({GetSpellByID(player, (int)Spells.Vampiric).Effect.CurrentBonus}%)", 13, "0.51 0.10", "0.70 0.95", TextAnchor.LowerLeft);

            CuiElement EvasionIcon = new CuiElement
            {
                Name = "EvasionIcon",
                Parent = SpellOverlayName,
                Components = {
                        new CuiRawImageComponent {
                            Png = GetImage("evasion_icon"),
                            Color = "0.75 0.75 0.75 1.00"
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.66 0.10",
                        AnchorMax = "0.81 0.95"
                        }
                }
            };
            elementValue.Add(EvasionIcon);
            UI.CreateText(ref elementValue, SpellOverlayName, TextColor, $"Lv.{GetSpellByID(player, (int)Spells.Evasion).Level.Level}({GetSpellByID(player, (int)Spells.Evasion).Effect.CurrentBonus}%)", 13, "0.67 0.10", "0.88 0.95", TextAnchor.LowerLeft);

            CuiElement SpikeIcon = new CuiElement
            {
                Name = "SpikeIcon",
                Parent = SpellOverlayName,
                Components = {
                        new CuiRawImageComponent {
                            Png = GetImage("spike_icon"),
                            Color = "0.75 0.75 0.75 1.00"
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.82 0.10",
                        AnchorMax = "0.98 0.95"
                        }
                }
            };
            elementValue.Add(SpikeIcon);
            UI.CreateText(ref elementValue, SpellOverlayName, TextColor, $"Lv.{GetSpellByID(player, (int)Spells.Spike).Level.Level}({GetSpellByID(player, (int)Spells.Spike).Effect.CurrentBonus}%)", 13, "0.83 0.10", "0.98 0.95", TextAnchor.LowerLeft);


            CuiHelper.AddUi(player, elementValue);
        }

        private void SetExtraInfoPanel(BasePlayer player, string text)
        {
            DestroyExtraInfoPanel(player);
            var container = UI.CreateElementContainer("ExtraSpellInfo", BackgroundColor1, 0.370f + " " + 0.120f, 0.600f + " " + 0.200f);

            UI.CreateText(ref container, "ExtraSpellInfo", UI.Color(Red, 1f), text, 28, "0.20 0.15", "0.80 0.95");

            CuiHelper.AddUi(player, container);

            timer.Once(2, () =>
            {
                DestroyExtraInfoPanel(player);
            });
        }

        private void DestroyExtraInfoPanel(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "ExtraSpellInfo");
        }

        private void SetActiveLine(BasePlayer player, int spell)
        {
            DestroyExpLine(player);

            var container = UI.CreateElementContainer("ExpContainer", BackgroundColor1, 0.010f + " " + 0.025f, 0.300f + " " + 0.105f);

            if(spell == (int)Spells.Rage)
            {
                UI.CreatePanel(ref container, "ExpContainer", UI.Color(Green, 1), 0.02f + " " + 0.095f, 0.17f + " " + 0.105f);
            }
            else if(spell == (int)Spells.Shield)
            {
                UI.CreatePanel(ref container, "ExpContainer", UI.Color(Green, 1), 0.18f + " " + 0.095f, 0.33f + " " + 0.105f);
            }
            else if(spell == (int)Spells.Critical)
            {
                UI.CreatePanel(ref container, "ExpContainer", UI.Color(Green, 1), 0.34f + " " + 0.095f, 0.49f + " " + 0.105f);
            }
            else if (spell == (int)Spells.Vampiric)
            {
                UI.CreatePanel(ref container, "ExpContainer", UI.Color(Green, 1), 0.50f + " " + 0.095f, 0.65f + " " + 0.105f);
            }
            else if (spell == (int)Spells.Evasion)
            {
                UI.CreatePanel(ref container, "ExpContainer", UI.Color(Green, 1), 0.66f + " " + 0.095f, 0.81f + " " + 0.105f);
            }
            else if (spell == (int)Spells.Spike)
            {
                UI.CreatePanel(ref container, "ExpContainer", UI.Color(Green, 1), 0.82f + " " + 0.095f, 0.98f + " " + 0.105f);
            }

            CuiHelper.AddUi(player, container);
        }

        private void SetSpellCD(BasePlayer player, int sec)
        {
            DestroySpellCD(player);

            var container = UI.CreateElementContainer("CDContainer", BackgroundColor1, 0.010f + " " + 0.025f, 0.300f + " " + 0.105f);

            UI.CreateText(ref container, "CDContainer", UI.Color(Red, 1.0f), $"{sec}", 16, 0.02f + " " + 0.095f, 0.17f + " " + 0.105f, TextAnchor.UpperRight);
            UI.CreateText(ref container, "CDContainer", UI.Color(Red, 1.0f), $"{sec}", 26, 0.18f + " " + 0.095f, 0.34f + " " + 0.105f, TextAnchor.UpperRight);
            UI.CreateText(ref container, "CDContainer", UI.Color(Red, 1.0f), $"{sec}", 26, 0.34f + " " + 0.095f, 0.49f + " " + 0.105f, TextAnchor.UpperRight);
            UI.CreateText(ref container, "CDContainer", UI.Color(Red, 1.0f), $"{sec}", 26, 0.50f + " " + 0.095f, 0.65f + " " + 0.105f, TextAnchor.UpperRight);
            UI.CreateText(ref container, "CDContainer", UI.Color(Red, 1.0f), $"{sec}", 26, 0.66f + " " + 0.095f, 0.81f + " " + 0.105f, TextAnchor.UpperRight);
            UI.CreateText(ref container, "CDContainer", UI.Color(Red, 1.0f), $"{sec}", 26, 0.82f + " " + 0.095f, 0.98f + " " + 0.105f, TextAnchor.UpperRight);

            CuiHelper.AddUi(player, container);
        }

        private void DestroySpellCD(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "CDContainer");
        }

        private void DestroyExpLine(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "ExpContainer");
        }

        /// <summary>
        /// Обновляет динамическую состоявляющую интерфейса для игрока
        /// </summary>
        /// <param name="player">игрок</param>
        private void UpdateGUI(BasePlayer player)
        {
            DissectSpellPanels(player);
        }

        private void DissectSpellPanels(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, SpellImgsOverlayName);
        }

        /// <summary>
        /// Удаляет все связанные с модулем панели для игрока
        /// </summary>
        /// <param name="player">игрок</param>
        private void DissectPanels(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, SpellOverlayName);
        }
        #endregion

        #region Manipulating

        /// <summary>
        /// Выдает уровень к умению игрока
        /// </summary>
        /// <param name="player">игрок</param>
        /// <param name="spellId">идентификатор умения</param>
        /// <param name="level">уровень</param>
        private void GiveLevelToSpell(BasePlayer player, int spellId, int level)
        {
            if (!IsActiveSpell(player, spellId))
            {
                return;
            }

            ActiveSpells[player].Level.Seek(level);
        }

        /// <summary>
        /// Удаляет уровень у умения игрока
        /// </summary>
        /// <param name="player">игрок</param>
        /// <param name="spellId">идентификатор умения</param>
        /// <param name="resetExp">сбрасывать опыт ?</param>
        private void RemoveLevelFromSpell(BasePlayer player, int spellId, bool resetExp = true)
        {
            if (!IsActiveSpell(player, spellId))
            {
                return;
            }

            ActiveSpells[player].Level.Seek(_Config.MIN_LEVEL);

            RemoveExpFromSpell(player, spellId);
        }

        /// <summary>
        /// Добавляет опыта к указанному умению игрока
        /// </summary>
        /// <param name="player">игрок</param>
        /// <param name="spellId">идентификатор умения</param>
        /// <param name="exp">количество опыта</param>
        private void AddExpToSpell(BasePlayer player, int spellId, int exp)
        {
            if (!IsActiveSpell(player, spellId))
            {
                return;
            }

            int oldLevel = ActiveSpells[player].Level.Level;

            int multExp = 1;

            foreach (var perm in PermissionMultipliers)
            {
                if (permission.UserHasPermission(player.UserIDString, perm.Key))
                {
                    multExp = (exp * perm.Value);
                }
            }

            if (multExp == 1)
            {
                ActiveSpells[player].Level.GiveExp(exp);
            }
            else
            {
                ActiveSpells[player].Level.GiveExp(multExp);
            }

            if (oldLevel < ActiveSpells[player].Level.Level)
            {
                int level = ActiveSpells[player].Level.Level;
                ActiveSpells[player].Effect.UpdateBonus(level);

                DissectPanels(player);
                SetPanels(player);

                SendReply(player, $"<color=#ffa500ff>[RS]: </color><color=#008000ff>Умение</color>: '<color=#008080ff>{ActiveSpells[player].Data.Name}</color>' <color=#008000ff>успешно улучшено !</color>");
            }
        }

        private Spell GetSpellByID(BasePlayer player, int id)
        {
            Spell sp = null;
            foreach(var spell in OnlinePlayersStore[player].AllSpells)
            {
                if(spell.Data.Id == id)
                {
                    sp = spell;
                }
            }

            return sp;
        }

        /// <summary>
        /// Удаляет опыт у умения указанного игрока
        /// </summary>
        /// <param name="player">игрок</param>
        /// <param name="spellId">идентификатор умения</param>
        private void RemoveExpFromSpell(BasePlayer player, int spellId)
        {
            if (!IsActiveSpell(player, spellId))
            {
                return;
            }

            ActiveSpells[player].Level.Seek(0, ActiveSpells[player].Level.GetNeededExp());
        }

        /// <summary>
        /// Добавляет умение к игроку
        /// </summary>
        /// <param name="player">игрок</param>
        /// <param name="spellId">идентификатор умения</param>
        /// <returns></returns>
        private bool AddSpellToPlayer(BasePlayer player, int spellId)
        {
            if (player == null)
            {
                PrintError("Plugin received at a null player object");

                return false;
            }
            if (IsActiveSpell(player))
            {
                PrintWarning($"Player({player.displayName}) has been isset a active spell");

                return false;
            }

            if (SpellsSwitcher.ContainsKey(player))
            {
                if (SpellsSwitcher[player].IsSpellSwitched)
                {
                    SendReply(player, $"<color=#ffa500ff>[RS]: </color><color=#ff0000ff>Нельзя так быстро переключать умения. Повторите через {SpellsSwitcher[player].ExpireSeconds} секунд</color>");

                    return false;
                }
            }
            else
            {
                SpellsSwitcher.Add(player, new SwitcheableData());

                return false;
            }

            SpellPlayerStore store = GetFromStore(player);
            if (store == null)
            {
                PrintError($"Plugin received a null store object from player({player.displayName}). Repeating ...");

                //Repeat loading for player.
                return false;
            }

            Spell spell = GetSpellFromStore(player, store, spellId);
            if (spell == null)
            {
                spell = SpellFactory.CreateByID(spellId);
                if (spell == null)
                {
                    PrintError($"Plugin created a null object from spell ID (ID: {spellId})");

                    return false;
                }
                else
                {
                    //PrintWarning($"Spell: '{spell.Data.Name}' created new for player({player.displayName}) from plugin !");

                    AddToStore(player, spellId, spell);
                }
            }

            int sec = 6;
            timer.Repeat(ONE_SECOND_IMIT, sec, () =>
            {
                if (SpellsSwitcher.ContainsKey(player))
                {
                    SpellsSwitcher[player].IsSpellSwitched = true;
                    SpellsSwitcher[player].ExpireSeconds = sec;
                }

                SetSpellCD(player, sec);
                sec--;
            });

            timer.Once(sec, () =>
            {
                DestroySpellCD(player);

                SpellsSwitcher[player].IsSpellSwitched = false;
            });

            store.ActiveSpell = spell;
            ActiveSpells.Add(player, spell);

            SetActiveLine(player, spell.Data.Id);

            return true;
        }

        /// <summary>
        /// Удаляет умение из активных для указанного игрока
        /// </summary>
        /// <param name="player">игрок</param>
        /// <returns></returns>
        private bool RemoveSpellFromPlayer(BasePlayer player)
        {
            if (!IsActiveSpell(player))
            {
                return false;
            }

            if(ActiveSpells[player].Data.IsSwitched)
            {
                return false;
            }

            OnlinePlayersStore[player].ActiveSpell = null;
            ActiveSpells.Remove(player);
            return true;
        }

        /// <summary>
        /// Переключает текущее умение игрока на указанное
        /// </summary>
        /// <param name="player">игрок</param>
        /// <param name="spellId">идентификатор умения</param>
        /// <returns></returns>
        private bool SwitchSpellFromPlayer(BasePlayer player, int spellId)
        {
            RemoveSpellFromPlayer(player);

            return AddSpellToPlayer(player, spellId);
        }
        #endregion

        #region Checkers
        /// <summary>
        /// Возвращает логическое представление результата проверки
        /// активного умения персонажа
        /// </summary>
        /// <param name="player">игрок</param>
        /// <returns></returns>
        private bool IsActiveSpell(BasePlayer player)
        {
            if (ActiveSpells.ContainsKey(player))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Возвращает логическое представление результата проверки
        /// активного умения персонажа по указанному идентификатору
        /// </summary>
        /// <param name="player">игрок</param>
        /// <param name="id">идентификатор умения</param>
        /// <returns></returns>
        private bool IsActiveSpell(BasePlayer player, int id)
        {
            if (ActiveSpells.ContainsKey(player))
            {
                if (ActiveSpells[player].Data.Id == id)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Возвращает логическое представление результата проверки
        /// активного умения персонажа по указанному типу умения
        /// </summary>
        /// <param name="player">игрок</param>
        /// <param name="type">тип умения</param>
        /// <returns></returns>
        private bool IsActiveSpell(BasePlayer player, SpellType type)
        {
            if (ActiveSpells.ContainsKey(player))
            {
                if (ActiveSpells[player].Effect.EffectType == type)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region Commands

        /// <summary>
        /// Команда для активации умения
        /// </summary>
        /// <param name="arg"></param>
        [ConsoleCommand("skill.add")]
        private void CmdConsoleAddSpell(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player();
            if (player == null)
            {
                return;
            }

            if (arg.Args.Length > 1)
            {
                int spellId = -1;
                if (Int32.TryParse(arg.Args[0], out spellId))
                {
                    if (spellId != -1)
                    {
                        if (IsActiveSpell(player, spellId))
                        {
                            string spellName = OnlinePlayersStore[player].ActiveSpell.Data.Name;
                            RemoveSpellFromPlayer(player);

                            SendReply(player, $"<color={_Config.CHAT_PREFIX_COLOR}>{_Config.CHAT_PREFIX}</color>Умение {spellName} успешно деактивировано");

                            return;
                        }

                        if (!AddSpellToPlayer(player, spellId))
                        {
                            SendReply(player, $"<color={_Config.CHAT_PREFIX_COLOR}>{_Config.CHAT_PREFIX}</color>У вас уже есть активное умение. Используйте spell.add.force для переключения");
                        }
                        else
                        {
                            SendReply(player, $"<color={_Config.CHAT_PREFIX_COLOR}>{_Config.CHAT_PREFIX}</color>Вы успешно активировали умение '{OnlinePlayersStore[player].ActiveSpell.Data.Name}'");
                        }
                    }
                }
                else
                {
                    SendReply(player, $"<color={_Config.CHAT_PREFIX_COLOR}>{_Config.CHAT_PREFIX}</color>Некорректный идентификатор умения");
                }
            }
            else
            {
                SendReply(player, $"<color={_Config.CHAT_PREFIX_COLOR}>{_Config.CHAT_PREFIX}</color>Некорректная команда для использования bind. Example: bind spell.add 'id'");

                return;
            }
        }

        /// <summary>
        /// Команда для переключения умения
        /// </summary>
        /// <param name="arg"></param>
        [ConsoleCommand("skill.add.force")]
        private void CmdConsoleAddSpellForce(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg?.Player();
            if (player == null)
            {
                return;
            }

            if (arg.Args.Length > 1)
            {
                int spellId = -1;
                if (Int32.TryParse(arg.Args[0], out spellId))
                {
                    if (spellId != -1)
                    {
                        if (IsActiveSpell(player, spellId))
                        {
                            SendReply(player, $"<color=#ffa500ff>[RS]: </color>Умение '<color=#008080ff>{ActiveSpells[player].Data.Name}</color>' уже используется");

                            return;
                        }

                        if (SpellsSwitcher.ContainsKey(player))
                        {
                            if (SpellsSwitcher[player].IsSpellSwitched)
                            {
                                SendReply(player, $"<color=#ffa500ff>[RS]: </color><color=#ff0000ff>Нельзя так быстро переключать умения. Повторите через {SpellsSwitcher[player].ExpireSeconds} секунд</color>");

                                return;
                            }
                        }

                        if (!SwitchSpellFromPlayer(player, spellId))
                        {
                            //SendReply(player, $"<color={_Config.CHAT_PREFIX_COLOR}>{_Config.CHAT_PREFIX}</color>Ошибка при активации умения. [0x1DFC223D]");
                        }
                        else
                        {
                            SendReply(player, $"<color=#ffa500ff>[RS]: </color>Вы переключились на умение: '<color=#008080ff>{OnlinePlayersStore[player].ActiveSpell.Data.Name}</color>'");
                            SendReply(player, $"<color=#ffa500ff>[RS]: </color><color=#008080ff>{OnlinePlayersStore[player].ActiveSpell.Data.Name}</color> - {OnlinePlayersStore[player].ActiveSpell.Data.Description}");
                        }
                    }
                }
            }
            else
            {
                SendReply(player, $"<color={_Config.CHAT_PREFIX_COLOR}>{_Config.CHAT_PREFIX}</color>Некорректная команда для использования bind. Example: bind spell.add.force 1");

                return;
            }
        }

        /// <summary>
        /// Команда для отмены действия текущего эффекта
        /// </summary>
        /// <param name="args"></param>
        [ConsoleCommand("skill.remove")]
        private void ConsoleCmdSpellRemove(ConsoleSystem.Arg args)
        {
            BasePlayer player = args?.Player();
            if (player == null)
            {
                PrintError("Plugin received a null object for appending command");

                return;
            }

            if (args.Args.Length > 1)
            {
                int spellId = -1;
                if (Int32.TryParse(args.Args[0], out spellId))
                {
                    if (spellId != -1)
                    {
                        if (RemoveSpellFromPlayer(player))
                        {
                            SendReply(player, $"<color={_Config.CHAT_PREFIX_COLOR}>{_Config.CHAT_PREFIX}</color>Активное умение отключено");
                        }
                        else
                        {
                            PrintError("Incorrect type for canceling spell OR not active spells");
                        }
                    }
                }
            }
            else
            {
                SendReply(player, $"<color={_Config.CHAT_PREFIX_COLOR}>{_Config.CHAT_PREFIX}</color>Некорректная команда для использования bind. Example: bind spell.add.force 1");

                return;
            }
        }

        [ChatCommand("spell.help")]
        void cmdChatHelp(BasePlayer player, string command, string[] args)
        {
            if(player == null)
            {
                return;
            }

            if(OnlinePlayersStore.ContainsKey(player))
            {
                foreach(var spell in OnlinePlayersStore[player].AllSpells)
                {
                    SendReply(player, $"<color=#ffa500ff>[RS]: </color> <color=#008080ff>{spell.Data.Name}</color>(<color=#ff0000ff>{spell.Data.Id}</color>) <bind <VK> skill.add[.force] {spell.Data.Id}>");
                }
            }
        }
        #endregion
    }
}
