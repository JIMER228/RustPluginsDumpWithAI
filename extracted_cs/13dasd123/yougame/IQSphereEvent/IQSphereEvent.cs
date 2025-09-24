using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using System.Linq;
using System.Text;
using Oxide.Core.Plugins;
using ConVar;
using System.Collections;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Libraries;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("IQSphereEvent", "Mercury", "1.1.9")]
    [Description("Supported Discord - Mercury#5212")]
    class IQSphereEvent : RustPlugin
    {
        ///123 <summary>
        // - Исправил плагин после обновления игры от FP
        /// Обновление 1.1.x
        /// - Упростил мультиязычность в конфигурации
        /// - Исправил дублирование модов для NPC в кфг
        /// - Теперь при входе нового игрока - для него отобразится маркер (если мероприятие началось)
        /// </summary>

        #region Reference
        private const Boolean LanguageEn = false;

        [PluginReference] Plugin IQChat, ImageLibrary, NpcSpawn, PreventLooting, BetterNpc;

        #region IQChat
        public void SendChat(BasePlayer player, String Message, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            var Chat = config.referencePlugin.chatSettings;
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, Chat.CustomPrefix, Chat.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion

        #region ImageLibrary
        private String GetImage(String fileName, UInt64 skin = 0)
        {
            var imageId = (String)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return String.Empty;
        }
        public Boolean AddImage(String url, String shortname, UInt64 skin = 0) => (Boolean)ImageLibrary?.Call("AddImage", url, shortname, skin);
        public Boolean HasImage(String imageName) => (Boolean)ImageLibrary?.Call("HasImage", imageName);
        #endregion

        #endregion

        #region Vars
        private Boolean IsLootingEventTier3 = false;

        private Timer TimerPreStarted = null;

        private Timer TimerStopped = null;
        private Timer TimerPreStopped = null;

        private Timer TimerSpawnSoundAlarms = null;

        public System.Random Random = new System.Random();

        public static IQSphereEvent _;
        private static MonumentInfo monument;
        private TriggerZone TriggerZoneTier1 = null;
        private TriggerZone TriggerZoneAround = null;
        private Item CassetteAlarm;
        private Vector3 PositionDespawn;

        private Dictionary<BasePlayer, Coroutine> RoutinePlayer = new Dictionary<BasePlayer, Coroutine>();
        private Coroutine SpawnEffects = null;
        private Coroutine SpawnEvents = null;

        private List<BaseEntity> OtherEntity = new List<BaseEntity>();
        private List<BaseEntity> ElecticalEntity = new List<BaseEntity>();
        private Dictionary<ScientistNPC, Tier> nPCMonitors = new Dictionary<ScientistNPC, Tier>();
        private List<IOEntity> IOList = new List<IOEntity>();

        private List<PagerEntity> Pagers = new List<PagerEntity>();

        private List<BasePlayer> PlayerWarnings = new List<BasePlayer>();

        private MapMarkerGenericRadius MarkerZoneEvent;

        private Boolean EventStarted = false;
        enum StoryPerson
        {
            Information,
            Scientist,
            Helicopter,
            Chinook,
        }
        public enum FlyType
        {
            Helicopter,
            Chinoock
        }
        public enum Tier
        {
            Under,
            Around,
            Tier1,
            Tier2,
            Tier3,
        }
        public enum BehaviorChinook
        {
            Die,
            Leave
        }

        #endregion

        #region Serializer

        public Serializer SerializerMain = new Serializer();
        public class Serializer
        {
            public String JsonUI;
            public String JsonUIWarning;
            public String JsonUILabel;

            public Information BoomBox = new Information();
            public Information Triggers = new Information(); 

            public List<Information> ListSoundsAlarm = new List<Information>();
            public List<Information> LaserAlarm = new List<Information>();
            public List<Information> ListAlarm = new List<Information>();
            public List<Information> Lamps = new List<Information>();
            public List<Information> Sams = new List<Information>();
            public List<Information> Effects = new List<Information>();
            public List<Information> HelicopterPoints = new List<Information>(); 

            public List<BotInformation> Bots = new List<BotInformation>(); 
            internal class BotInformation
            {
                public Information Information = new Information();
                public Tier TierSpawn;
            }
            internal class Information
            {
                public String Name;
                public Vector3 Position;
                public Vector3 Rotation;
                public QuaternionFormul Formul = new QuaternionFormul();
                internal class QuaternionFormul
                {
                    public Single X;
                    public Single Y;
                    public Single Z;
                    public Single W;
                }
            }
        }

        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        public class Configuration
        {
            [JsonProperty(LanguageEn ? "Event Settings" : "Настройки мероприятия")]
            public EventSetting EventSettings = new EventSetting();
            [JsonProperty(LanguageEn ? "Configuring supported plugins" : "Настройка поддерживаемых плагинов")]
            public ReferencePlugin referencePlugin = new ReferencePlugin();
            [JsonProperty(LanguageEn ? "Setting up bots at the event (After updating the game 02.12 temporarily disabled!)" : "Настройка ботов на мероприятии (После обновления игры 02.12 временно отключены!)")]
            public SpawnBots BotsSetting = new SpawnBots();
            [JsonProperty(LanguageEn ? "Setting up the sphere" : "Настройка сферы")]
            public SphereSetting SphereSettings = new SphereSetting();
            [JsonProperty(LanguageEn ? "Setting up the plot" : "Настройка сюжета")]
            public StorySettings StorySetting = new StorySettings();
            internal class StorySettings
            {
               [JsonProperty(LanguageEn ? "Effects for the player at each notification on the walkie-talkie(one is selected randomly)" : "Эффекты для игрока при каждом уведомлении по рации(выбирается случайно один)")]
                public List<String> RadioSounds;
                [JsonProperty(LanguageEn ? "Setting up persons to accompany in the plot" : "Настройка персон для сопровождения в сюжете")]
                public PersonSettings Persons;

                internal class PersonSettings
                {
                     [JsonProperty(LanguageEn ? "Picture for scientists (PNG 32x32)" : "Картинка для ученых")]
                    public String ScientistPNG;
                     [JsonProperty(LanguageEn ? "Picture for a helicopter (PNG 32x32)" : "Картинка для вертолета")]
                    public String HelicopterPNG;
                     [JsonProperty(LanguageEn ? "Picture for chinook (PNG 32x32)" : "Картинка для чинука")]
                    public String ChinookPNG;
                    [JsonProperty(LanguageEn ? "An image for an information notification (PNG 32x32)" : "Картинка для информационного уведомления")]
                    public String InformationPNG;
                }
            }
            internal class ReferencePlugin
            {
                [JsonProperty(LanguageEn ? "IQChat: Setting up a chat" : "IQChat : Настройка чата")]
                public ChatSettings chatSettings = new ChatSettings();
                [JsonProperty(LanguageEn ? "BetterNpc : Setting betternpc" : "BetterNpc : Настройка BetterNpc")]
                public BetterNpcSettings BetterNpcSetting = new BetterNpcSettings();
                internal class BetterNpcSettings
                {
                    [JsonProperty(LanguageEn ? "When launching an event, should I remove NPCs from the Better Npc plugin on the sphere? (true - yes/false - no) (after the event is completed, they will be returned)" : "При запуске мероприятия удалять ли NPC из плагина BetterNpc на сфере? (true - да/false - нет) (после завершения мероприятия они будут возвращены)")]
                    public Boolean DestroyedNpc = false;
                }
                internal class ChatSettings
                {
                    [JsonProperty(LanguageEn ? "IQChat :Custom prefix in the chat" : "IQChat : Кастомный префикс в чате")]
                    public String CustomPrefix;
                    [JsonProperty(LanguageEn ? "IQChat : Custom avatar in the chat(If required)" : "IQChat : Кастомный аватар в чате(Если требуется)")]
                    public String CustomAvatar;
                }
            }
            internal class LootSetting
            {
                [JsonProperty(LanguageEn ? "Item name" : "Название предмета")]
                public String DisplayName;
                [JsonProperty(LanguageEn ? "" :"Shortname")]
                public String Shortname;
                [JsonProperty(LanguageEn ? "" :"SkinID")]
                public UInt64 SkinID;
                [JsonProperty(LanguageEn ? "Setting up the quantity" : "Настройка количества")]
                public CountSettings CountSetting = new CountSettings();
            }
            internal class CountSettings
            {
                [JsonProperty(LanguageEn ? "Minimum value" : "Минимальное значение")]
                public Int32 AmountMin;
                [JsonProperty(LanguageEn ? "Maximum value" : "Максимальное значение")]
                public Int32 AmountMax;
            }
            internal class SphereSetting
            {
               [JsonProperty(LanguageEn ? "Air defense settings on the sphere" : "Настрой ПВО на сфере")]
                public SamTurret SamSetting = new SamTurret();
               [JsonProperty(LanguageEn ? "Sound effects settings on the sphere" : "Настройки звуковых эффектов на сфере")]
                public BoomBoxSettings BoomBoxSetting = new BoomBoxSettings();
                [JsonProperty(LanguageEn ? "Radiation settings on the sphere" : "Настройки радиации на сфере")]
                public Radiation RadiationSettings = new Radiation();
                [JsonProperty(LanguageEn ? "Helicopter settings on the sphere" : "Настройки вертолета на сфере")]
                public Helicopter HelicopterSettings = new Helicopter();
                [JsonProperty(LanguageEn ? "Chinook settings on the sphere" : "Настройки чинука на сфере")]
                public Chinook ChinoockSettings = new Chinook();
                [JsonProperty(LanguageEn ? "Enable light signals on the sphere (true-yes/false-no)" : "Включить световые сигналы на сфере (true - да/false - нет)")]
                public Boolean UseAlarm;
                [JsonProperty(LanguageEn ? "Enable lasers inside the sphere(true-yes/false-no) (Sound signals must be enabled!)" : "Включить лазеры внутри сферы(true - да/false - нет) (Должны быть включены звуковые сигналы!)")]
                public Boolean UseLaser;
                [JsonProperty(LanguageEn ? "Turn on the lamps inside the sphere(true-yes/false-no)" : "Включить лампы внутри сферы(true - да/false - нет)")]
                public Boolean UseLamps;
                [JsonProperty(LanguageEn ? "Enable effects support(true-yes/false-no)" : "Включить поддержку эффектов(true - да/false - нет)")]
                public Boolean UseEffects;

                internal class Helicopter
                {
                   [JsonProperty(LanguageEn ? "Use a helicopter on a sphere (true-yes/false-no)" : "Использовать вертолет на сфере (true - да/false - нет)")]
                    public Boolean UseHelicopter;
                    [JsonProperty(LanguageEn ? "How many circles will the helicopter make around the sphere" : "Сколько кругов сделает вертолет вокруг сферы")]
                    public Int32 CountCircle;
                    [JsonProperty(LanguageEn ? "The maximum speed of the helicopter (Also affects the flight speed, if you are not sure about this setting, leave it as default)" : "Максимальная скорость вертолета (Так-же влияет на скорость полета, если не уверены в этой настройке, оставьте по умолчанию)")]
                    public Single MaxSpeed;
                    [JsonProperty(LanguageEn ? "Helicopter speed (Also affects the maximum flight speed, if you are not sure about this setting, leave it as default)" : "Скорость вертолета (Так-же влияет на максимальную скорость  полета, если не уверены в этой настройке, оставьте по умолчанию)")]
                    public Single MoveSpeed;
                    [JsonProperty(LanguageEn ? "Setting up loot in the helicopter boxes" : "Настройка лута в ящиках вертолета")]
                    public CrateLootSetting LootHelicopter = new CrateLootSetting();

                    internal class CrateLootSetting
                    {
                       [JsonProperty(LanguageEn ? "Use your own list of items in the helicopter boxes(true-yes/false-no)" : "Использовать собственный список предметов в ящиках вертолета(true - да/false - нет)")]
                        public Boolean UseCustomLoot;
                        [JsonProperty(LanguageEn ? "The number of boxes that will be spawned" : "Количество ящиков, которое заспавнится")]
                        public CountSettings AmountRandomCrate = new CountSettings();
                        [JsonProperty(LanguageEn ? "List of items in the boxes(will be selected randomly)" : "Список предметов в ящиках(будут выбираться случайно)")]
                        public List<LootSetting> LootSettings = new List<LootSetting>();
                    }
                }
                internal class Chinook
                {
                    [JsonProperty(LanguageEn ? "Use chinook on a sphere (true-yes/false-no)" : "Использовать чинук на сфере (true - да/false - нет)")]
                    public Boolean UseChinoock;
                    [JsonProperty(LanguageEn ? "Configuring Chinook Behaviors" : "Настройка поведениий чинука")]
                    public BehaviourChinoock BehaviorSettingsChinoock = new BehaviourChinoock();
                    internal class BehaviourChinoock
                    {
                        [JsonProperty(LanguageEn ? "Setting up the Chinook's fall on the sphere [It will fly and break right on the sphere]" : "Настройка падения чинука на сфере [Он пролетит и разобьется прям на сфере]")]
                        public DropCrateChinoock ChinoockDies = new DropCrateChinoock();
                        [JsonProperty(LanguageEn ? "Setting up the Chinook's flight at the sphere [It will fly over the sphere and fulfill some conditions that are configured below]" : "Настройка пролета чинука у сфере [Он пролетит над сферой и выполнит некоторые условия, которые настраиваются ниже]")]
                        public DropCrateChinoock ChinoockLeaves = new DropCrateChinoock();

                        internal class DropCrateChinoock
                        {
                            [JsonProperty(LanguageEn ? "Whether to reset the mailbox randomly(true-yes/false-no)" : "Сбрасывать ли случайным образом ящик(true - да/false - нет)")]
                            public Boolean UseDropCrateChinoock;
                           [JsonProperty(LanguageEn ? "Static chance of dropping a box from a Chinook" : "Статический шанс сброса ящика с чинука")]
                            public Int32 RareDropCrateChinoock;
                           [JsonProperty(LanguageEn ? "Setting up a random reset chance" : "Настройка случайного шанса сброса")]
                            public RandomSetting RandomSettings = new RandomSetting();
                           [JsonProperty(LanguageEn ? "Setting up loot in crate" : "Настройка лута в ящике")]
                            public CrateLootSetting CrateLootSettings = new CrateLootSetting();
                            internal class RandomSetting
                            {
                                [JsonProperty(LanguageEn ? "Use a random value to reset the mailbox (true-yes / false-no) (p.s. the static value will be ignored if this option is enabled)" : "Использовать случайное значение для сброса ящика(true - да/false - нет) (P.s статическое значение будет игнорироваться, если включен данный пункт)")]
                                public Boolean UseRandom;
                                [JsonProperty(LanguageEn ? "Setting values for randomness" : "Настройка значений для рандома")]
                                public CountSettings RandomRangeCount = new CountSettings();
                            }

                            internal class CrateLootSetting
                            {
                               [JsonProperty(LanguageEn ? "Use your own list of items in the Chinook box(true-yes/false-no)" : "Использовать собственный список предметов в ящике от чинука(true - да/false - нет)")]
                                public Boolean UseCustomLoot;
                                [JsonProperty(LanguageEn ? "List of items in the Chinook box(will be selected randomly)" : "Список предметов в ящике с чинука(будут выбираться случайно)")]
                                public List<LootSetting> LootSettings = new List<LootSetting>();
                            }
                        }
                    }
                }
                internal class Radiation
                {
                    [JsonProperty(LanguageEn ? "Enable radiation support on the sphere(true-yes/false-no)" : "Включить поддержку радиации на сфере(true - да/false - нет)")]
                    public Boolean UseRadiation;
                    [JsonProperty(LanguageEn ? "Starting radiation indicator" : "Стартовый показатель радиации")]
                    public Single StartingAmountRadiation;
                    [JsonProperty(LanguageEn ? "Radiation multiplier (radiation will increase by this indicator during the specified limit and time)" : "Множитель радиации (радиация будет увеличиваться на данный показатель в течение заданного лимита и времени)")]
                    public Single MultiplierRadiation;
                    [JsonProperty(LanguageEn ? "Radiation update time" : "Время обновления радиации")]
                    public Single SecondUpdate;
                    [JsonProperty(LanguageEn ? "To what level of radiation should it be increased" : "До какого показателя радиации увеличивать ее")]
                    public Single MaximumAmountRadiation;
                }
                internal class SamTurret
                {
                    [JsonProperty(LanguageEn ? "Enable air defense spawn(true-yes/false-no)" : "Включить спавн ПВО(true - да/false - нет)")]
                    public Boolean UseSam;
                   [JsonProperty(LanguageEn ? "Radius of detection of flying air defense objects" : "Радиус обнаружении летающих объектов ПВО")]
                    public Single Radius;
                    [JsonProperty(LanguageEn ? "The number of cartridges in one air defense system" : "Количество патрон в одной ПВО")]
                    public Single AmmoCount;
                }
                internal class BoomBoxSettings
                {
                   [JsonProperty(LanguageEn ? "Enable sound effects spawn(true-yes/false-no)" : "Включить спавн звуковых эффектов(true - да/false - нет)")]
                    public Boolean UseSoundAlarm;
                    [JsonProperty(LanguageEn ? "A link to a YouTube video with this sound (Note that it is taken only for the first 30 seconds and is looped in the effect)" : "Ссылка на видео YouTube с данным звуком (Учтите, берется лишь 30 первых секунд и зацикливается в эффекте)")]
                    public String YouTubeLink;
                    [JsonProperty(LanguageEn ? "Sound volume 0.1 - 1.5" : "Громкость звука 0.1 - 1.5")]
                    public Single Volume;
                }
            }
            internal class SpawnBots
            {
                [JsonProperty(LanguageEn ? "Setting up bots under the sphere" : "Настройка ботов под сферой")]
                public GeneralBotSphere UnderBots = new GeneralBotSphere();
               [JsonProperty(LanguageEn ? "Setting up bots around the sphere" : "Настройка ботов вокруг сферы")]
                public GeneralBotSphere AroundBots = new GeneralBotSphere();
                [JsonProperty(LanguageEn ? "Setting up bots inside the sphere" : "Настройка ботов внутри сферы")]
                public LevelBotSphere LevelBots = new LevelBotSphere();

                internal class LevelBotSphere
                {
                    [JsonProperty(LanguageEn ? "Setting up Tier 1 bots" : "Настройка ботов ТИР 1")]
                    public GeneralBotSphere Tier1 = new GeneralBotSphere();
                    [JsonProperty(LanguageEn ? "Setting up Tier 2 bots" : "Настройка ботов ТИР 2")]
                    public GeneralBotSphere Tier2 = new GeneralBotSphere();
                    [JsonProperty(LanguageEn ? "Setting up Tier 3 bots" : "Настройка ботов ТИР 3")]
                    public GeneralBotSphere Tier3 = new GeneralBotSphere();
                }
                internal class GeneralBotSphere
                {
                    [JsonProperty(LanguageEn ? "Enable spawn bots(true-yes/false-no)" : "Включить спавн ботов(true - да/false - нет)")]
                    public Boolean UseBots;
                    [JsonProperty(LanguageEn ? "Setting up the spawn of the number of NPCs" : "Настройка спавна количества NPC")]
                    public CountSettings CounSetting = new CountSettings();

                   [JsonProperty(LanguageEn ? "Health bots" : "ХП ботов")]
                    public Single HealthBot;
                   [JsonProperty(LanguageEn ? "The radius of visibility of bots" : "Радиус видимости ботов")]
                    public Single RadiusVisBots = 150f;
                    [JsonProperty(LanguageEn ? "The display name of the bots" : "Отображаемое имя ботов")]
                    public String DisplayNameBot;
                    [JsonProperty(LanguageEn ? "Roam Range" : "Дальность патрулирования местности")]
                    public float RoamRange = 30f;
                   [JsonProperty(LanguageEn ? "Chase Range" : "Дальность погони за целью")] 
                    public float ChaseRange = 90f;
                   [JsonProperty(LanguageEn ? "Attack Range Multiplier" : "Множитель радиуса атаки")] 
                    public float AttackRangeMultiplier = 2f;
                    [JsonProperty(LanguageEn ? "Sense Range" : "Радиус обнаружения цели")] 
                    public float SenseRange = 50f;
                    [JsonProperty(LanguageEn ? "Scale damage" : "Множитель урона")] 
                    public float DamageScale = 1f;
                    [JsonProperty(LanguageEn ? "Aim Cone Scale" : "Множитель разброса")] 
                    public float AimConeScale = 0.1f;
                    [JsonProperty(LanguageEn ? "Detect the target only in the NPC's viewing vision cone?" : "Обнаруживать цель только в углу обзора NPC?")] 
                    public bool CheckVisionCone = false;
                   [JsonProperty(LanguageEn ? "Vision Cone" : "Угол обзора")] 
                    public float VisionCone = 135f;
                   [JsonProperty(LanguageEn ? "Speed" : "Скорость")] 
                    public float Speed = 7f;


                   [JsonProperty(LanguageEn ? "Wear NPC" : "Одежда NPC")]
                    public List<ItemBot> WearNPC = new List<ItemBot>(6);
                   [JsonProperty(LanguageEn ? "NPC Weapon Variation" : "Вариация оружия NPC")]
                    public List<ItemBot> BeltNPC = new List<ItemBot>();
                   [JsonProperty(LanguageEn ? "Setting up a drop-out loot with an NPC" :"Настройка выпадаемого лута с NPC")]
                    public List<ItemDropBot> ItemDropLoot = new List<ItemDropBot>();

                    internal class ItemDropBot
                    {
                       [JsonProperty(LanguageEn ? "Setting loot" : "Настройка лута")]
                        public LootSetting LootSetting = new LootSetting();
                        [JsonProperty(LanguageEn ? "Chance of loot loss" : "Шанс выпадения лута")]
                        public Int32 RareDrop;
                    }
                    internal class ItemBot
                    {
                        [JsonProperty(LanguageEn ? "Shortname" : "Shortname")]
                        public String Shortname;
                        [JsonProperty(LanguageEn ? "SkinID" : "SkinID")]
                        public UInt64 SkinID;
                        [JsonProperty(LanguageEn ? "Mods weapon" : "Mods weapon")]
                        public List<String> Mods;
                    }
                }
            }
            internal class EventSetting
            {
                [JsonProperty(LanguageEn ? "Once in how many seconds to launch an event" : "Раз в сколько секунд запускать мероприятие")]
                public Int32 StartTime;
                [JsonProperty(LanguageEn ? "Use a timer to stop the event (true - yes/false - no) (If 'No', the event will end after the player opens one of the boxes at the top level)" : "Использовать таймер для остановки мероприятия (true - да/false - нет) (Если 'Нет', то мероприятие закончится после того, как игрок откроет один из ящиков на вверхнем уровне)")]
                public Boolean UseTimerStop = true;
                [JsonProperty(LanguageEn ? "How long will the event last(Recommended value : 2400 seconds)" : "Сколько будет длиться мероприятие(Рекомендуемое значение : 2400 секунд)")]
                public Int32 StopEventTime;
                [JsonProperty(LanguageEn ? "Remove players' backpacks after the end of the event (true - yes/false - no)" : "Удалять рюкзаки игроков после завершения мероприятия (true - да/false - нет)")]
                public Boolean DeleteBackpacksEventStop = false;
                [JsonProperty(LanguageEn ? "Setting up your mailboxes on the sphere" : "Настройка своих ящиков на сфере")]
                public CustomLootSphere LootSphereSetting;
                [JsonProperty(LanguageEn ? "Setting up the event coverage area" : "Настройка зоны действия мероприятия")]
                public SettingsZoneEvent SettingZoneEvent;
                [JsonProperty(LanguageEn ? "Setting up a marker on the G map" : "Настройка маркера на G карте")]
                public MapMarkerSetting MapMarkerSettings;
                [JsonProperty(LanguageEn ? "Additional configuration" : "Дополнительная настройка")]
                public OtherSetting OtherSettings;

                internal class OtherSetting
                {
                    [JsonProperty(LanguageEn ? "Notify the player if he entered the event area (true-yes/false-no)" : "Уведомлять игрока если он вошел в зону действия мероприятия(true - да/false - нет)")]
                    public Boolean UseAlertTriggerEnter;
                    [JsonProperty(LanguageEn ? "Configuring the pager" : "Настройка пейджера")]
                    public Pager PagerSettings;
                    internal class Pager
                    {
                        [JsonProperty(LanguageEn ? "Use a pager (it will be activated at the beginning of the event and before its completion(if the specified frequency is set))" : "Использовать пейджер (при начале мероприятия и до его завершения будет активирован(если задана указанная частота))")]
                        public Boolean UsePagerAlert;
                        [JsonProperty(LanguageEn ? "Pager frequency" : "Частота пейджера")]
                        public Int32 Frequency;
                    }
                }
                internal class MapMarkerSetting
                {
                    [JsonProperty(LanguageEn ? "Use a marker(true-yes/false-no)" : "Использовать маркер(true - да/false - нет)")]
                    public Boolean UseMarker;
                    [JsonProperty(LanguageEn ? "Marker color" : "Цвет маркера")]
                    public String ColorMarker;
                    [JsonProperty(LanguageEn ? "Marker transparency" : "Прозрачность маркера")]
                    public Single AlphaMarker;
                }
                internal class SettingsZoneEvent
                {
                    [JsonProperty(LanguageEn ? "Setting up team blocking in the event area" : "Настройка блокировки команд в зоне действия мероприятия")]
                    public BlockCommand BlockCommands;
                    internal class BlockCommand
                    {
                        [JsonProperty(LanguageEn ? "Use command blocking in the event area (true-yes/false-no)" : "Использовать блокировку команд в зоне действия мероприятия(true - да/false - нет)")]
                        public Boolean UseBlockCommand;
                        [JsonProperty(LanguageEn ? "List of chat teams that will be blocked in the event area" : "Список чат команд, которые будут заблокированы в зоне действия мероприятия")]
                        public List<String> Commands = new List<String>();
                    }
                }
                internal class CustomLootSphere
                {
                    [JsonProperty(LanguageEn ? "Use your own boxes on the sphere(true-yes/false-no)" : "Использовать собственные ящики на сфере(true - да/false - нет)")]
                    public Boolean UseCustomLoot;
                    [JsonProperty(LanguageEn ? "How many boxes to compare on the sphere" : "Сколько спавнить ящиков на сфере")]
                    public Int32 SpawnCrateCount;

                    [JsonProperty(LanguageEn ? "List of spawn boxes with settings(they will be selected randomly from the list)" : "Список ящиков для спавна с настройкой(будут выбираться рандомно из списка)")]
                    public List<CrateSettings> CrateList = new List<CrateSettings>();
                    internal class CrateSettings
                    {
                        [JsonProperty(LanguageEn ? "Use standard loot in the boxes (true - yes/false - no (configurable at this point, the list of items)" : "Использовать в ящиках стандартный лут (true - да/false - нет(настраивается в этом пункте, список предметов)")]
                        public Boolean UseDefaultLoot = false;
                        [JsonProperty(LanguageEn ? "Prefab crate" : "Префаб ящика")]
                        public String CratePrefab;
                        [JsonProperty(LanguageEn ? "Minimum and maximum amount of loot in one box(if you use custom loot)" : "Минимальное и максимальное количество лута в одном ящике(если вы используете свой лут)")]
                        public CountSettings CountSpawnItems = new CountSettings();
                        [JsonProperty(LanguageEn ? "List of items in the drawer" : "Список предметов в ящике")]
                        public List<LootSettings> ItemSettings = new List<LootSettings>();

                        internal class LootSettings
                        {
                            [JsonProperty(LanguageEn ? "Loot Settings" : "Настройка лута")]
                            public LootSetting LootSetting = new LootSetting();
                            [JsonProperty(LanguageEn ? "Chance of loot loss" : "Шанс выпадения лута")]
                            public Int32 RareDrop;
                        }
                    }
                }
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    #region Reference
                    referencePlugin = new ReferencePlugin
                    {
                        chatSettings = new ReferencePlugin.ChatSettings
                        {
                            CustomAvatar = "76561199206561118",
                            CustomPrefix = "[<color=#ff4948>Scientists</color>] "
                        },
                        BetterNpcSetting = new ReferencePlugin.BetterNpcSettings
                        {
                            DestroyedNpc = false,
                        },
                    },
                    #endregion

                    #region EventSetting
                    StorySetting = new StorySettings
                    {
                        Persons = new StorySettings.PersonSettings
                        {
                            ChinookPNG = "https://i.imgur.com/kv9JmP6.png",
                            ScientistPNG = "https://i.imgur.com/HgB63CL.png",
                            HelicopterPNG = "https://i.imgur.com/zWmpEX0.png",
                            InformationPNG = "https://i.imgur.com/Rf4C1yi.png",
                        },
                        RadioSounds = new List<String>
                        {
                            "assets/prefabs/npc/scientist/sound/aggro.prefab",
                            "assets/prefabs/npc/scientist/sound/chatter.prefab",
                            "assets/prefabs/npc/scientist/sound/death.prefab",
                            "assets/prefabs/npc/scientist/sound/responddeath.prefab",
                            "assets/prefabs/npc/scientist/sound/respondok.prefab",
                            "assets/prefabs/npc/scientist/sound/takecover.prefab",
                        }
                    },
                    EventSettings = new EventSetting
                    {
                        StartTime = 1200,
                        StopEventTime = 2400,
                        DeleteBackpacksEventStop = false,
                        UseTimerStop = true,
                        OtherSettings = new EventSetting.OtherSetting
                        {
                            UseAlertTriggerEnter = true,
                            PagerSettings = new EventSetting.OtherSetting.Pager
                            {
                                UsePagerAlert = true,
                                Frequency = 5997,
                            }
                        },
                        MapMarkerSettings = new EventSetting.MapMarkerSetting
                        {
                            UseMarker = true,
                            ColorMarker = "#c15338",
                            AlphaMarker = 0.6f,
                        },
                        SettingZoneEvent = new EventSetting.SettingsZoneEvent
                        {
                            BlockCommands = new EventSetting.SettingsZoneEvent.BlockCommand
                            {
                                UseBlockCommand = true,
                                Commands = new List<String>
                                {
                                    "kit",
                                    "tp",
                                    "tpa",
                                    "home",
                                    "store",
                                }
                            }
                        },
                        LootSphereSetting = new EventSetting.CustomLootSphere
                        {
                            SpawnCrateCount = 4,
                            UseCustomLoot = true,
                            CrateList = new List<EventSetting.CustomLootSphere.CrateSettings>
                            {
                                new EventSetting.CustomLootSphere.CrateSettings
                                {
                                    UseDefaultLoot = false,
                                    CountSpawnItems = new CountSettings
                                    {
                                        AmountMin = 1,
                                        AmountMax = 6,
                                    },
                                    CratePrefab = "assets/bundled/prefabs/radtown/crate_elite.prefab",
                                    ItemSettings = new List<EventSetting.CustomLootSphere.CrateSettings.LootSettings>
                                    {
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 100,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "wood",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 3000,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 50,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "rifle.ak",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 5,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "explosive.timed",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 3,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 30,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "supply.signal",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 2,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 10,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "explosive.satchel",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 2,
                                                    AmountMax = 5,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 7,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "rocket.launcher",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 15,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "rifle.lr300",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 8,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "multiplegrenadelauncher",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 12,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "lmg.m249",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 70,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "clatter.helmet",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 30,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "metal.plate.torso",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 20,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "metal.facemask",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 40,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "roadsign.kilt",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 90,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "ammo.rifle.explosive",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 5,
                                                    AmountMax = 30,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 18,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "ammo.rocket.basic",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 3,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 24,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "ammo.grenadelauncher.he",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 3,
                                                    AmountMax = 6,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 88,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "largemedkit",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 3,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 60,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "autoturret",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 60,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "coffin.storage",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 3,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 55,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "keycard_red",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 55,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "techparts",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 10,
                                                    AmountMax = 33,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 80,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "gears",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 10,
                                                    AmountMax = 33,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 30,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "riflebody",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 3,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 90,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "metal.fragments",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1000,
                                                    AmountMax = 5000,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 50,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "scrap",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 10,
                                                    AmountMax = 200,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 40,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "explosives",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 3,
                                                    AmountMax = 15,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 79,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "metal.refined",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 30,
                                                    AmountMax = 100,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 15,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "door.double.hinged.toptier",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 40,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "door.hinged.toptier",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 60,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "wall.external.high.stone",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 60,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "wall.frame.garagedoor",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 30,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "floor.ladder.hatch",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 30,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "wall.window.bars.toptier",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 30,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "wall.window.glass.reinforced",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                        new EventSetting.CustomLootSphere.CrateSettings.LootSettings
                                        {
                                            RareDrop = 2,
                                            LootSetting = new LootSetting
                                            {
                                                DisplayName = "",
                                                Shortname = "mining.quarry",
                                                SkinID = 0,
                                                CountSetting = new CountSettings
                                                {
                                                    AmountMin = 1,
                                                    AmountMax = 1,
                                                }
                                            }
                                        },
                                    }
                                }
                            }
                        }
                    },
                    #endregion

                    #region SphereSettings

                    SphereSettings = new SphereSetting
                    {
                        RadiationSettings = new SphereSetting.Radiation
                        {
                            UseRadiation = true,
                            StartingAmountRadiation = 30f,
                            SecondUpdate = 10f,
                            MaximumAmountRadiation = 60f,
                            MultiplierRadiation = 5f,
                        },
                        HelicopterSettings = new SphereSetting.Helicopter
                        {
                            CountCircle = 3,
                            MaxSpeed = 58f,
                            MoveSpeed = 28f,
                            UseHelicopter = true,
                            LootHelicopter = new SphereSetting.Helicopter.CrateLootSetting
                            {
                                UseCustomLoot = true,
                                AmountRandomCrate = new CountSettings
                                {
                                    AmountMin = 1,
                                    AmountMax = 4,
                                },
                                LootSettings = new List<LootSetting>
                                {
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "wood",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 3000,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "explosive.timed",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 3,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "supply.signal",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 3,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "explosive.satchel",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 2,
                                              AmountMax = 5,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "ammo.rifle.explosive",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 10,
                                              AmountMax = 30,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "ammo.rocket.basic",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 3,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "ammo.grenadelauncher.he",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 6,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "largemedkit",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 3,
                                              AmountMax = 5,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "coffin.storage",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 3,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "techparts",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 10,
                                              AmountMax = 30,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "gears",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 30,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "riflebody",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 5,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "metal.fragments",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1000,
                                              AmountMax = 5000,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "scrap",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 100,
                                              AmountMax = 300,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "explosives",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 5,
                                              AmountMax = 10,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "metal.refined",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 100,
                                              AmountMax = 500,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "rifle.ak",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "rocket.launcher",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "rifle.lr300",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "multiplegrenadelauncher",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "lmg.m249",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "clatter.helmet",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "metal.plate.torso",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "metal.facemask",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "roadsign.kilt",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "autoturret",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "keycard_red",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "door.double.hinged.toptier",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "door.hinged.toptier",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "wall.external.high.stone",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "wall.frame.garagedoor",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "floor.ladder.hatch",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "wall.window.bars.toptier",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "wall.window.glass.reinforced",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                    new LootSetting
                                    {
                                         DisplayName = "",
                                         SkinID = 0,
                                         Shortname = "mining.quarry",
                                         CountSetting = new CountSettings
                                         {
                                              AmountMin = 1,
                                              AmountMax = 1,
                                         },
                                    },
                                }
                            }
                        },
                        ChinoockSettings = new SphereSetting.Chinook
                        {
                            UseChinoock = true,
                            BehaviorSettingsChinoock = new SphereSetting.Chinook.BehaviourChinoock
                            {
                                ChinoockDies = new SphereSetting.Chinook.BehaviourChinoock.DropCrateChinoock
                                {

                                    UseDropCrateChinoock = true,
                                    RareDropCrateChinoock = 10,
                                    RandomSettings = new SphereSetting.Chinook.BehaviourChinoock.DropCrateChinoock.RandomSetting
                                    {
                                        UseRandom = true,
                                        RandomRangeCount = new CountSettings
                                        {
                                            AmountMin = 10,
                                            AmountMax = 50,
                                        },
                                    },
                                    CrateLootSettings = new SphereSetting.Chinook.BehaviourChinoock.DropCrateChinoock.CrateLootSetting
                                    {
                                        UseCustomLoot = true,
                                        LootSettings = new List<LootSetting>
                                        {
                                                new LootSetting
                                                    {
                                                         DisplayName = "",
                                                         SkinID = 0,
                                                         Shortname = "wood",
                                                         CountSetting = new CountSettings
                                                         {
                                                              AmountMin = 1,
                                                              AmountMax = 3000,
                                                         },
                                                    },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "explosive.timed",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 3,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "supply.signal",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 3,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "explosive.satchel",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 2,
                                                          AmountMax = 5,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "ammo.rifle.explosive",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 10,
                                                          AmountMax = 30,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "ammo.rocket.basic",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 3,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "ammo.grenadelauncher.he",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 6,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "largemedkit",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 3,
                                                          AmountMax = 5,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "coffin.storage",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 3,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "techparts",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 10,
                                                          AmountMax = 30,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "gears",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 30,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "riflebody",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 5,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "metal.fragments",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1000,
                                                          AmountMax = 5000,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "scrap",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 100,
                                                          AmountMax = 300,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "explosives",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 5,
                                                          AmountMax = 10,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "metal.refined",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 100,
                                                          AmountMax = 500,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "rifle.ak",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "rocket.launcher",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "rifle.lr300",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "multiplegrenadelauncher",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "lmg.m249",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "clatter.helmet",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "metal.plate.torso",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "metal.facemask",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "roadsign.kilt",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "autoturret",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "keycard_red",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "door.double.hinged.toptier",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "door.hinged.toptier",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "wall.external.high.stone",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "wall.frame.garagedoor",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "floor.ladder.hatch",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "wall.window.bars.toptier",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "wall.window.glass.reinforced",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "mining.quarry",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                             },
                                    },
                                },
                                ChinoockLeaves = new SphereSetting.Chinook.BehaviourChinoock.DropCrateChinoock
                                {

                                    UseDropCrateChinoock = true,
                                    RareDropCrateChinoock = 10,
                                    RandomSettings = new SphereSetting.Chinook.BehaviourChinoock.DropCrateChinoock.RandomSetting
                                    {
                                        UseRandom = true,
                                        RandomRangeCount = new CountSettings
                                        {
                                            AmountMin = 10,
                                            AmountMax = 50
                                        },
                                    },
                                    CrateLootSettings = new SphereSetting.Chinook.BehaviourChinoock.DropCrateChinoock.CrateLootSetting
                                    {
                                        UseCustomLoot = true,
                                        LootSettings = new List<LootSetting>
                                            {
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "wood",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 3000,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "explosive.timed",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 3,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "supply.signal",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 3,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "explosive.satchel",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 2,
                                                          AmountMax = 5,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "ammo.rifle.explosive",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 10,
                                                          AmountMax = 30,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "ammo.rocket.basic",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 3,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "ammo.grenadelauncher.he",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 6,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "largemedkit",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 3,
                                                          AmountMax = 5,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "coffin.storage",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 3,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "techparts",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 10,
                                                          AmountMax = 30,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "gears",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 30,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "riflebody",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 5,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "metal.fragments",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1000,
                                                          AmountMax = 5000,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "scrap",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 100,
                                                          AmountMax = 300,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "explosives",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 5,
                                                          AmountMax = 10,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "metal.refined",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 100,
                                                          AmountMax = 500,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "rifle.ak",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "rocket.launcher",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "rifle.lr300",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "multiplegrenadelauncher",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "lmg.m249",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "clatter.helmet",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "metal.plate.torso",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "metal.facemask",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "roadsign.kilt",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "autoturret",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "keycard_red",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "door.double.hinged.toptier",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "door.hinged.toptier",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "wall.external.high.stone",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "wall.frame.garagedoor",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "floor.ladder.hatch",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "wall.window.bars.toptier",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "wall.window.glass.reinforced",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                                new LootSetting
                                                {
                                                     DisplayName = "",
                                                     SkinID = 0,
                                                     Shortname = "mining.quarry",
                                                     CountSetting = new CountSettings
                                                     {
                                                          AmountMin = 1,
                                                          AmountMax = 1,
                                                     },
                                                },
                                            },
                                    },
                                }
                            }
                        },
                        SamSetting = new SphereSetting.SamTurret
                        {
                            UseSam = true,
                            AmmoCount = 10000,
                            Radius = 500f,
                        },
                        UseLaser = true,
                        UseAlarm = true,
                        UseLamps = true,
                        UseEffects = true,
                        BoomBoxSetting = new SphereSetting.BoomBoxSettings
                        {
                            UseSoundAlarm = true,
                            Volume = 1.0f,
                            YouTubeLink = "https://www.youtube.com/watch?v=4VDqu7Oa9rs&list=PLOJ0LdNc-6nLWZEFQmKF3dlrfQk0SbOAp",
                        }
                    },

                    #endregion

                    #region BotsSettings
                    BotsSetting = new SpawnBots
                    {
                        #region Under Bots
                        UnderBots = new SpawnBots.GeneralBotSphere
                        {
                            UseBots = true,
                            CounSetting = new CountSettings
                            {
                                AmountMin = 5,
                                AmountMax = 15,
                            },
                            DisplayNameBot = "Under Scientist",
                            HealthBot = 80,
                            RoamRange = 30f,
                            ChaseRange = 90f,
                            AttackRangeMultiplier = 2f,
                            AimConeScale = 1f,
                            CheckVisionCone = false,
                            DamageScale = 1f,
                            SenseRange = 50f,
                            Speed = 7f,
                            VisionCone = 135f,
                            RadiusVisBots = 150f,
                            WearNPC = new List<SpawnBots.GeneralBotSphere.ItemBot>
                            {
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "hoodie",
                                    SkinID = 2187105866,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "pants",
                                    SkinID = 2187107432,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "riot.helmet",
                                    SkinID = 1988565302,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "shoes.boots",
                                    SkinID = 1088000573,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "metal.plate.torso",
                                    SkinID = 1134374285,
                                },
                            },
                            BeltNPC = new List<SpawnBots.GeneralBotSphere.ItemBot>
                            {
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "rifle.lr300",
                                    SkinID = 1837475559,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "lmg.m249",
                                    SkinID = 0,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "rifle.semiauto",
                                    SkinID = 1845749582,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "rifle.ak",
                                    SkinID = 2437435853,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                            },
                            ItemDropLoot = new List<SpawnBots.GeneralBotSphere.ItemDropBot>
                            {
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "rifle.ak",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 1,
                                       },
                                    },
                                    RareDrop = 1,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "scrap",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 10,
                                           AmountMax = 60,
                                       },
                                    },
                                    RareDrop = 20,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "targeting.computer", 
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 3,
                                       },
                                    },
                                    RareDrop = 30,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "ammo.rifle",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 10,
                                           AmountMax = 60,
                                       },
                                    },
                                    RareDrop = 30,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "metal.facemask",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 1,
                                       },
                                    },
                                    RareDrop = 1,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "roadsign.kilt",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 1,
                                       },
                                    },
                                    RareDrop = 4,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "syringe.medical",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 3,
                                           AmountMax = 6,
                                       },
                                    },
                                    RareDrop = 40,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "antiradpills",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 70,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "largemedkit",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 13,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "bandage",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 2,
                                           AmountMax = 5,
                                       },
                                    },
                                    RareDrop = 58,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "riflebody",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 14,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "smgbody",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 15,
                                },
                            },
                        },
                        #endregion

                        #region Around Bots
                        AroundBots = new SpawnBots.GeneralBotSphere
                        {
                            UseBots = true,
                            CounSetting = new CountSettings
                            {
                                AmountMin = 10,
                                AmountMax = 20,
                            },
                            DisplayNameBot = "Araound Bot",
                            HealthBot = 65,
                            RoamRange = 30f,
                            ChaseRange = 90f,
                            AttackRangeMultiplier = 2f,
                            AimConeScale = 1f,
                            CheckVisionCone = false,
                            DamageScale = 1f,
                            SenseRange = 50f,
                            Speed = 7f,
                            VisionCone = 135f,
                            RadiusVisBots = 150f,
                            WearNPC = new List<SpawnBots.GeneralBotSphere.ItemBot>
                            {
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "hoodie",
                                    SkinID = 2187105866,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "pants",
                                    SkinID = 2187107432,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "riot.helmet",
                                    SkinID = 1988565302,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "shoes.boots",
                                    SkinID = 1088000573,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "metal.plate.torso",
                                    SkinID = 1134374285,
                                },
                            },
                            BeltNPC = new List<SpawnBots.GeneralBotSphere.ItemBot>
                            {
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "rifle.lr300",
                                    SkinID = 1837475559,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "lmg.m249",
                                    SkinID = 0,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "rifle.semiauto",
                                    SkinID = 1845749582,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "rifle.ak",
                                    SkinID = 2437435853,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                            },
                            ItemDropLoot = new List<SpawnBots.GeneralBotSphere.ItemDropBot>
                            {
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "rifle.ak",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 1,
                                       },
                                    },
                                    RareDrop = 3,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "scrap",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 10,
                                           AmountMax = 60,
                                       },
                                    },
                                    RareDrop = 30,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "targeting.computer",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 3,
                                       },
                                    },
                                    RareDrop = 49,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "ammo.rifle",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 10,
                                           AmountMax = 60,
                                       },
                                    },
                                    RareDrop = 45,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "metal.facemask",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 1,
                                       },
                                    },
                                    RareDrop = 3,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "roadsign.kilt",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 1,
                                       },
                                    },
                                    RareDrop = 8,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "syringe.medical",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 3,
                                           AmountMax = 6,
                                       },
                                    },
                                    RareDrop = 45,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "antiradpills",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 70,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "largemedkit",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 55,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "bandage",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 2,
                                           AmountMax = 5,
                                       },
                                    },
                                    RareDrop = 60,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "riflebody",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 10,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "smgbody",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 13,
                                },
                            },
                        },
                        #endregion

                        #region Level Bots
                        LevelBots = new SpawnBots.LevelBotSphere
                        {
                            #region Tier1
                            Tier1 = new SpawnBots.GeneralBotSphere
                            {
                                UseBots = true,
                                CounSetting = new CountSettings
                                {
                                    AmountMin = 3,
                                    AmountMax = 8,
                                },
                                DisplayNameBot = "Tier1 Bot",
                                HealthBot = 160,
                                RoamRange = 30f,
                                ChaseRange = 90f,
                                AttackRangeMultiplier = 2f,
                                AimConeScale = 1f,
                                CheckVisionCone = false,
                                DamageScale = 1f,
                                SenseRange = 50f,
                                Speed = 7f,
                                VisionCone = 135f,
                                RadiusVisBots = 150f,
                                WearNPC = new List<SpawnBots.GeneralBotSphere.ItemBot>
                                {
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "hoodie",
                                    SkinID = 2187105866,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "pants",
                                    SkinID = 2187107432,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "riot.helmet",
                                    SkinID = 1988565302,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "shoes.boots",
                                    SkinID = 1088000573,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "metal.plate.torso",
                                    SkinID = 1134374285,
                                },
                            },
                                BeltNPC = new List<SpawnBots.GeneralBotSphere.ItemBot>
                            {
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "rifle.lr300",
                                    SkinID = 1837475559,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "lmg.m249",
                                    SkinID = 0,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "rifle.semiauto",
                                    SkinID = 1845749582,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "rifle.ak",
                                    SkinID = 2437435853,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                            },
                                ItemDropLoot = new List<SpawnBots.GeneralBotSphere.ItemDropBot>
                                {
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "rifle.ak",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 1,
                                       },
                                    },
                                    RareDrop = 3,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "scrap",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 10,
                                           AmountMax = 60,
                                       },
                                    },
                                    RareDrop = 50,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "targeting.computer",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 3,
                                       },
                                    },
                                    RareDrop = 70,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "ammo.rifle",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 10,
                                           AmountMax = 60,
                                       },
                                    },
                                    RareDrop = 80,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "metal.facemask",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 1,
                                       },
                                    },
                                    RareDrop = 5,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "roadsign.kilt",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 1,
                                       },
                                    },
                                    RareDrop = 15,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "syringe.medical",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 3,
                                           AmountMax = 6,
                                       },
                                    },
                                    RareDrop = 60,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "antiradpills",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 30,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "largemedkit",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 33,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "bandage",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 2,
                                           AmountMax = 5,
                                       },
                                    },
                                    RareDrop = 80,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "riflebody",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 40,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "smgbody",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 60,
                                },
                            },
                            },

                            #endregion

                            #region Tier2
                            Tier2 = new SpawnBots.GeneralBotSphere
                            {
                                UseBots = true,
                                CounSetting = new CountSettings
                                {
                                    AmountMin = 3,
                                    AmountMax = 7,
                                },
                                DisplayNameBot = "Tier2 Bot",
                                HealthBot = 200,
                                RoamRange = 30f,
                                ChaseRange = 90f,
                                AttackRangeMultiplier = 2f,
                                AimConeScale = 1f,
                                CheckVisionCone = false,
                                DamageScale = 1f,
                                SenseRange = 50f,
                                Speed = 7f,
                                VisionCone = 135f,
                                RadiusVisBots = 150f,
                                WearNPC = new List<SpawnBots.GeneralBotSphere.ItemBot>
                                {
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "hoodie",
                                    SkinID = 2187105866,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "pants",
                                    SkinID = 2187107432,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "riot.helmet",
                                    SkinID = 1988565302,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "shoes.boots",
                                    SkinID = 1088000573,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "metal.plate.torso",
                                    SkinID = 1134374285,
                                },
                            },
                                BeltNPC = new List<SpawnBots.GeneralBotSphere.ItemBot>
                            {
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "rifle.lr300",
                                    SkinID = 1837475559,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "lmg.m249",
                                    SkinID = 0,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "rifle.semiauto",
                                    SkinID = 1845749582,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "rifle.ak",
                                    SkinID = 2437435853,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                            },
                                ItemDropLoot = new List<SpawnBots.GeneralBotSphere.ItemDropBot>
                                {
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "rifle.ak",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 1,
                                       },
                                    },
                                    RareDrop = 3,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "scrap",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 10,
                                           AmountMax = 60,
                                       },
                                    },
                                    RareDrop = 50,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "targeting.computer",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 3,
                                       },
                                    },
                                    RareDrop = 70,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "ammo.rifle",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 10,
                                           AmountMax = 60,
                                       },
                                    },
                                    RareDrop = 80,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "metal.facemask",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 1,
                                       },
                                    },
                                    RareDrop = 5,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "roadsign.kilt",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 1,
                                       },
                                    },
                                    RareDrop = 15,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "syringe.medical",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 3,
                                           AmountMax = 6,
                                       },
                                    },
                                    RareDrop = 60,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "antiradpills",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 30,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "largemedkit",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 33,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "bandage",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 2,
                                           AmountMax = 5,
                                       },
                                    },
                                    RareDrop = 80,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "riflebody",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 40,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "smgbody",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 60,
                                },
                                },
                            },
                            #endregion

                            #region Tier3
                            Tier3 = new SpawnBots.GeneralBotSphere
                            {
                                UseBots = true,
                                CounSetting = new CountSettings
                                {
                                    AmountMin = 7,
                                    AmountMax = 10,
                                },
                                DisplayNameBot = "Tier3 Bot",
                                HealthBot = 250,
                                RoamRange = 30f,
                                ChaseRange = 90f,
                                AttackRangeMultiplier = 2f,
                                AimConeScale = 1f,
                                CheckVisionCone = false,
                                DamageScale = 1f,
                                SenseRange = 50f,
                                Speed = 7f,
                                VisionCone = 135f,
                                RadiusVisBots = 150f,
                                WearNPC = new List<SpawnBots.GeneralBotSphere.ItemBot>
                                {
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "hoodie",
                                    SkinID = 2187105866,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "pants",
                                    SkinID = 2187107432,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "riot.helmet",
                                    SkinID = 1988565302,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "shoes.boots",
                                    SkinID = 1088000573,
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "metal.plate.torso",
                                    SkinID = 1134374285,
                                },
                                },
                                BeltNPC = new List<SpawnBots.GeneralBotSphere.ItemBot>
                                {
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "rifle.lr300",
                                    SkinID = 1837475559,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "lmg.m249",
                                    SkinID = 0,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "rifle.semiauto",
                                    SkinID = 1845749582,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                                new SpawnBots.GeneralBotSphere.ItemBot
                                {
                                    Shortname = "rifle.ak",
                                    SkinID = 2437435853,
                                    Mods = new List<string>{ "weapon.mod.flashlight" }
                                },
                            },
                            ItemDropLoot = new List<SpawnBots.GeneralBotSphere.ItemDropBot>
                            {
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "rifle.ak",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 1,
                                       },
                                    },
                                    RareDrop = 3,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "scrap",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 10,
                                           AmountMax = 60,
                                       },
                                    },
                                    RareDrop = 50,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "targeting.computer",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 3,
                                       },
                                    },
                                    RareDrop = 70,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "ammo.rifle",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 10,
                                           AmountMax = 60,
                                       },
                                    },
                                    RareDrop = 80,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "metal.facemask",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 1,
                                       },
                                    },
                                    RareDrop = 5,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "roadsign.kilt",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 1,
                                       },
                                    },
                                    RareDrop = 15,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "syringe.medical",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 3,
                                           AmountMax = 6,
                                       },
                                    },
                                    RareDrop = 60,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "antiradpills",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 30,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "largemedkit",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 33,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "bandage",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 2,
                                           AmountMax = 5,
                                       },
                                    },
                                    RareDrop = 80,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "riflebody",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 40,
                                },
                                new SpawnBots.GeneralBotSphere.ItemDropBot
                                {
                                    LootSetting = new LootSetting
                                    {
                                       DisplayName = "",
                                       Shortname = "smgbody",
                                       SkinID = 0,
                                       CountSetting = new CountSettings
                                       {
                                           AmountMin = 1,
                                           AmountMax = 2,
                                       },
                                    },
                                    RareDrop = 60,
                                },
                            },
                            }
                            #endregion
                        }
                        #endregion
                    }
                    #endregion
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
                NextTick(SaveConfig);
            }
            catch { PrintWarning("Error reading the configuration, you made a syntax error"); }
        }
        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Data
        void ReadData()
        {
            try { SerializerMain = Oxide.Core.Interface.GetMod().DataFileSystem.ReadObject<Serializer>("IQSphereEvent/GetPosition"); }
            catch { PrintWarning("Error reading the data file"); }
        }
        void WriteData() => Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSphereEvent/GetPosition", SerializerMain, true);

        #endregion

        #region Hooks

        object OnUserCommand(IPlayer player, string command, string[] args)
        {
            if (!config.EventSettings.SettingZoneEvent.BlockCommands.UseBlockCommand) return null;
            BasePlayer basePlayer = BasePlayer.FindByID(UInt64.Parse(player.Id));
            if (basePlayer == null) return null;
            if (config.EventSettings.SettingZoneEvent.BlockCommands.Commands.Contains(command) && basePlayer.HasFlag(BaseEntity.Flags.Reserved10))
            {
                SendChat(basePlayer, GetLang("CHAT_ALERT_BLOCK_COMMANDS", basePlayer.UserIDString, command));
                return false;
            }
            return null;
        }

        void OnRfFrequencyChanged(IRFObject obj, int frequency, BasePlayer player)
        {
            if (!config.EventSettings.OtherSettings.PagerSettings.UsePagerAlert) return;
            Int32 FreeQuency = config.EventSettings.OtherSettings.PagerSettings.Frequency;

            PagerEntity Pager = (PagerEntity)obj;
            if (Pager == null) return;

            if (Pagers.Contains(Pager) && frequency != FreeQuency)
                Pagers.Remove(Pager);
            else if(frequency == FreeQuency) 
            {
                if (!Pagers.Contains(Pager))
                    Pagers.Add(Pager);

                if (EventStarted)
                    Pager.SetFlag(BaseEntity.Flags.On, true);
            }
        }

        void Unload()
        {
            StopSphereEvent();
            WriteData();
            _ = null;
        }
        void Init()
        {
            ReadData();
            UnSubscribePlugin();
        }
        void OnNewSave(string filename)
        {
            SerializerMain = new Serializer();
            timer.Once(3f, () => { GetInformation(); });          
        }

        private void OnPlayerConnected(BasePlayer player) => UpdateMarker(player);

        void OnServerInitialized()
        {
            if (!NpcSpawn)
            {
                NextTick(() =>
                {
                    PrintError("You don't have NpcSpawn installed, read the ReadMe file\nNpcSpawn - https://drive.google.com/drive/folders/1-18L-mG7yiGxR-PQYvd11VvXC2RQ4ZCu");
                    Oxide.Core.Interface.Oxide.UnloadPlugin("IQSphereEvent");
                });
                return;
            }
            else if (NpcSpawn.Version < new Oxide.Core.VersionNumber(2, 0, 7))
            {
                NextTick(() => {
                    PrintError("You have an old version of NpcSpawn!\nplease update the plugin to the latest version (2.0.7 or higher) - ReadMe.txt");
                    Oxide.Core.Interface.Oxide.UnloadPlugin(Name);
                });
                return;
            }

            monument = TerrainMeta.Path.Monuments.FirstOrDefault(p => p.name.ToLower().Contains("sphere_tank"));

            if (!IsSphereExisting())
            {
                NextTick(() =>
                {
                    PrintError("Sphere not found, plugin disabled");
                    Oxide.Core.Interface.Oxide.UnloadPlugin("IQSphereEvent");
                });

                return;
            }

            if (!ImageLibrary)
            {
                NextTick(() =>
                {
                    PrintError("Image Library was not found, the plugin is disabled");
                    Oxide.Core.Interface.Oxide.UnloadPlugin("IQSphereEvent");
                });
                return;
            }

            if (SerializerMain == null || SerializerMain.JsonUI == null)
                timer.Once(3f, () => { GetInformation(); });

            ImageDownload();

            _ = this;
            PositionDespawn = GetPositionDespawn();

            NextTick(() => { ClearEntity(); });

            if (config.SphereSettings.BoomBoxSetting.UseSoundAlarm)
                CreatedCassette(config.SphereSettings.BoomBoxSetting.YouTubeLink, config.SphereSettings.BoomBoxSetting.Volume);

            PreStartedEvent();
        }
        void OnUserRespawned(IPlayer player)
        {
            BasePlayer p = BasePlayer.FindByID(UInt64.Parse(player.Id));
            if (p == null) return;
            CuiHelper.DestroyUi(p, "IQSPHERE_WARNING");
            CuiHelper.DestroyUi(p, "IQSPHERE_PANEL");
            if (p.HasFlag(BaseEntity.Flags.Reserved10))
                p.SetFlag(BaseEntity.Flags.Reserved10, false);

            if (RoutinePlayer.ContainsKey(p))
            {
                p.StopCoroutine(RoutinePlayer[p]);
                RoutinePlayer.Remove(p);
            }
        }
        void OnCorpsePopulate(ScientistNPC npc, NPCPlayerCorpse corpse)
        {
            if (npc == null || corpse == null || !nPCMonitors.ContainsKey(npc)) return;
            Tier TierNPC = nPCMonitors[npc];

            if (corpse != null)
            {
                Configuration.SpawnBots Bots = config.BotsSetting;
                Configuration.SpawnBots.GeneralBotSphere BotConfiguration = TierNPC == Tier.Around ? Bots.AroundBots :
                                                                            TierNPC == Tier.Under ? Bots.UnderBots :
                                                                            TierNPC == Tier.Tier1 ? Bots.LevelBots.Tier1 :
                                                                            TierNPC == Tier.Tier2 ? Bots.LevelBots.Tier2 :
                                                                            TierNPC == Tier.Tier3 ? Bots.LevelBots.Tier3 : null;
                if (BotConfiguration == null || BotConfiguration.ItemDropLoot == null || BotConfiguration.ItemDropLoot.Count == 0) return;
                corpse.containers[0].itemList.Clear();
                for (Int32 i = 0; i < BotConfiguration.ItemDropLoot.Count; i++)
                {
                    Configuration.SpawnBots.GeneralBotSphere.ItemDropBot Loot = BotConfiguration.ItemDropLoot[i];
                    if (IsRandom(Loot.RareDrop))
                    {
                        Item item = ItemManager.CreateByName(Loot.LootSetting.Shortname, GetRandom(Loot.LootSetting.CountSetting.AmountMin, Loot.LootSetting.CountSetting.AmountMax), Loot.LootSetting.SkinID);
                        if (item == null) return;
                        if (!String.IsNullOrEmpty(Loot.LootSetting.DisplayName))
                            item.name = Loot.LootSetting.DisplayName;

                        item.MoveToContainer(corpse.containers[0]);
                    }
                }
                corpse.containers[0].capacity = corpse.containers[0].itemList.Count;
                corpse.containers[1].capacity = 0;
                corpse.containers[2].capacity = 0;
                corpse.containers[0].MarkDirty();
                corpse.SendNetworkUpdate();

                OtherEntity.Add(corpse);
            }
        }
        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (config.EventSettings.UseTimerStop) return;
            if (player == null || entity == null || entity.skinID != 92929294944 || IsLootingEventTier3) return;
            IsLootingEventTier3 = true;
        }


        #endregion

        #region Auxiliary

        #region Clear Entity

        private void ClearEntity()
        {
            foreach (BaseEntity Electrical in ElecticalEntity.Where(x => x != null && !x.IsDestroyed))
                Electrical.Kill(); //BaseNetworkable.DestroyMode.None

            foreach (BaseEntity OtherEntity in OtherEntity.Where(x => x != null && !x.IsDestroyed))
                OtherEntity.Kill();

            timer.Once(5f, () =>
            {
                List<BaseEntity> obj = new List<BaseEntity>();
                Vis.Entities(new Vector3(monument.transform.position.x, monument.transform.position.y + 30f, monument.transform.position.z), 70f, obj, LayerMask.GetMask("Default", "Ragdoll"));

                IEnumerable<BaseEntity> EntList = !config.EventSettings.DeleteBackpacksEventStop ? obj.Where(x => !x.IsDestroyed && (x is DroppedItemContainer && (x as DroppedItemContainer).playerSteamID != 0 && !(x as DroppedItemContainer).playerSteamID.IsSteamId()))
                    : obj.Where(x => !x.IsDestroyed && (x is DroppedItemContainer && (x as DroppedItemContainer).playerSteamID != 0));
                foreach (BaseEntity entity in EntList)
                    entity.Kill();
            });

            foreach (KeyValuePair<ScientistNPC, Tier> NPC in nPCMonitors.Where(x => x.Key != null && (!x.Key.IsDestroyed || !x.Key.IsDead())))
                NPC.Key.Kill();

            if (PatrolHelicopter != null)
                PatrolHelicopter.Kill();

            if (ChinoockHelicopter != null)
                ChinoockHelicopter.Kill();

            if (TriggerZoneTier1 != null)
                TriggerZoneTier1.Kill();

            if (TriggerZoneAround != null)
                TriggerZoneAround.Kill();   
        }

        #endregion

        #region Electical Turn
        private void ElectricalAdd(BaseEntity entity) => ElecticalEntity.Add(entity);
        private void ElectricalTurn(Boolean TurnedStatus)
        {
            if (ElecticalEntity == null || ElecticalEntity.Count == 0) return;
            foreach (BaseEntity entity in ElecticalEntity.Where(ent => ent != null))
            {
                entity.SetFlag(BaseEntity.Flags.Reserved8, TurnedStatus);
                entity.SetFlag(BaseEntity.Flags.On, TurnedStatus);
                entity.SendNetworkUpdate();

                BoomBox boomBox = entity?.GetComponent<BoomBox>();
                if (boomBox != null && boomBox is BoomBox)
                    boomBox.ServerTogglePlay(true);
            }
        }
        #endregion

        #region Health Setter
        private void HealthSet(BaseEntity entity, Single Health = 50000f)
        {
            BaseCombatEntity CombatEntity = entity.GetComponent<BaseCombatEntity>();
            if (CombatEntity == null) return;
            CombatEntity.SetMaxHealth(Health);
            CombatEntity.SetHealth(Health);
            entity.SendNetworkUpdate();
        }
        #endregion

        #region Sam Spawn

        private void SpawnSamDefense()
        {
            if (!config.SphereSettings.SamSetting.UseSam) return;

            foreach (Serializer.Information SamInfo in SerializerMain.Sams)
            {
                if (SamInfo.Name.Contains("floor.grill.prefab"))
                {
                    BaseEntity Barricade = (BaseEntity)GameManager.server.CreateEntity(SamInfo.Name, monument.transform.TransformPoint(SamInfo.Position), GetMonumentRotation(SamInfo.Formul));
                    Barricade.GetComponent<StabilityEntity>().grounded = true;
                    Barricade.OwnerID = 92929294944;
                    Barricade.Spawn();
                    HealthSet(Barricade);
                    OtherEntity.Add(Barricade);
                }
                else
                {
                    SamSite Sam = (SamSite)GameManager.server.CreateEntity(SamInfo.Name, monument.transform.TransformPoint(SamInfo.Position), GetMonumentRotation(SamInfo.Formul));
                    Sam.SetFlag(BaseEntity.Flags.Reserved8, true);
                    Sam.SetFlag(BaseEntity.Flags.Busy, true);
                    Sam.SetFlag(BaseEntity.Flags.Locked, true);
                    Sam.OwnerID = 92929294944;
                    Sam.Spawn();
                    HealthSet(Sam);
                    Sam.vehicleScanRadius = config.SphereSettings.SamSetting.Radius;
                    ItemManager.CreateByName("ammo.rocket.sam", 10000).MoveToContainer(Sam.inventory);
                    OtherEntity.Add(Sam);
                }
            }
        }
        #endregion

        #region Alarm Signal Spawn
        private void SpawnAlarm()
        {
            if (!config.SphereSettings.UseAlarm) return;
            foreach (Serializer.Information Alarm in SerializerMain.ListAlarm)
            {
                String Prefab = Alarm.Name;

                BaseEntity AlarmEntity = (BaseEntity)GameManager.server.CreateEntity(Prefab, monument.transform.TransformPoint(Alarm.Position), GetMonumentRotation(Alarm.Formul));
                AlarmEntity.OwnerID = 92929294944;
                AlarmEntity.Spawn();
                AlarmEntity.SetFlag(BaseEntity.Flags.Reserved8, false);
                AlarmEntity.SetFlag(BaseEntity.Flags.On, false);
                HealthSet(AlarmEntity);
                ElectricalAdd(AlarmEntity);
            }
        }
        #endregion

        #region Alarm Sound Spawn

        private void SpawnSoundAlarm()
        {
            if (!config.SphereSettings.BoomBoxSetting.UseSoundAlarm) return;

            Serializer BomBox = SerializerMain;
            if (CassetteAlarm == null)
            {
                if (TimerSpawnSoundAlarms != null && !TimerSpawnSoundAlarms.Destroyed)
                {
                    TimerSpawnSoundAlarms.Destroy();
                    TimerSpawnSoundAlarms = null;
                }
                TimerSpawnSoundAlarms = timer.Once(5f, () => { SpawnSoundAlarm(); });
                return;
            }

            IOList.Clear();

            ElectricGenerator Generator = (ElectricGenerator)GameManager.server.CreateEntity("assets/prefabs/deployable/playerioents/generators/generator.small.prefab", monument.transform.TransformPoint(BomBox.BoomBox.Position), GetMonumentRotation(BomBox.BoomBox.Formul));
            Generator.OwnerID = 92929294944;
            Generator.Spawn();
            Generator.electricAmount = 1000;

            HealthSet(Generator);
            ElectricalAdd(Generator);

            DeployableBoomBox BoomBox = (DeployableBoomBox)GameManager.server.CreateEntity(BomBox.BoomBox.Name, monument.transform.TransformPoint(BomBox.BoomBox.Position), GetMonumentRotation(BomBox.BoomBox.Formul));
            BoomBox.OwnerID = 92929294944;
            BoomBox.Spawn();
            BoomBox.SetFlag(BaseEntity.Flags.Reserved8, false);
            BoomBox.SetFlag(BaseEntity.Flags.On, false);

            HealthSet(BoomBox);
            ElectricalAdd(BoomBox);

            Generator.outputs[0].connectedTo = new IOEntity.IORef();
            Generator.outputs[0].connectedTo.Set(BoomBox);
            Generator.outputs[0].connectedToSlot = 0;
            Generator.outputs[0].connectedTo.Init();

            BoomBox.inputs[0].connectedTo = new IOEntity.IORef();
            BoomBox.inputs[0].connectedTo.Set(Generator);
            BoomBox.inputs[0].connectedToSlot = 0;
            BoomBox.inputs[0].connectedTo.Init();

            BoomBox.MarkDirtyForceUpdateOutputs();
            BoomBox.SendNetworkUpdate();

            Generator.MarkDirtyForceUpdateOutputs();
            Generator.SendNetworkUpdate();

            CassetteAlarm.MoveToContainer(BoomBox.inventory);

            BoomBox.PowerUsageWhilePlaying = 0;
            BoomBox.BoxController.ConditionLossRate = 0;
            for (Int32 Alarm = 0; Alarm < BomBox.ListSoundsAlarm.Count; Alarm++)
            {
                var AlarmThis = BomBox.ListSoundsAlarm[Alarm];

                IOEntity AlarmThisEntity = (IOEntity)GameManager.server.CreateEntity(AlarmThis.Name, monument.transform.TransformPoint(AlarmThis.Position), GetMonumentRotation(AlarmThis.Formul));
                AlarmThisEntity.OwnerID = 92929294944;
                AlarmThisEntity.Spawn();
                AlarmThisEntity.SetFlag(BaseEntity.Flags.Reserved8, false);
                AlarmThisEntity.SetFlag(BaseEntity.Flags.On, false);
                HealthSet(AlarmThisEntity);
                ElectricalAdd(AlarmThisEntity);

                if (Alarm == 0)
                {
                    #region Connection Boombox

                    BoomBox.outputs[0].connectedTo = new IOEntity.IORef();
                    BoomBox.outputs[0].connectedTo.Set(AlarmThisEntity);
                    BoomBox.outputs[0].connectedToSlot = 0;
                    BoomBox.outputs[0].connectedTo.Init();

                    AlarmThisEntity.inputs[0].connectedTo = new IOEntity.IORef();
                    AlarmThisEntity.inputs[0].connectedTo.Set(BoomBox);
                    AlarmThisEntity.inputs[0].connectedToSlot = 0;
                    AlarmThisEntity.inputs[0].connectedTo.Init();

                    BoomBox.MarkDirtyForceUpdateOutputs();
                    BoomBox.SendNetworkUpdate();

                    AlarmThisEntity.MarkDirtyForceUpdateOutputs();
                    AlarmThisEntity.SendNetworkUpdate();

                    IOList.Add(AlarmThisEntity);
                    #endregion
                }
                else
                {
                    #region Connection Other

                    IOEntity AlarmLast = IOList[IOList.Count - 1];
                    ConnectedAlarm(AlarmLast, AlarmThisEntity);
                    IOList.Add(AlarmThisEntity);

                    #endregion
                }
            }
            BoomBox.BoxController.ServerTogglePlay(false);
        }

        private void CreatedCassette(String YouTubeLink, Single Volume = 1.5f)
        {
            webrequest.Enqueue($"https://api.skyplugins.ru/api/getsoundogg?url={YouTubeLink}&volume={Volume}", "", (code, response) =>
            {
                switch (code)
                {
                    case 200:
                        {
                            Item ItemCassette = ItemManager.CreateByName("cassette", 1, 0);
                            if (ItemCassette != null)
                            {
                                Cassette Cassette = ItemModAssociatedEntity<Cassette>.GetAssociatedEntity(ItemCassette, true);
                                if (Cassette != null)
                                {
                                    Cassette.MaxCassetteLength = 35f;
                                    Byte[] data = Convert.FromBase64String(response);
                                    UInt32 id = FileStorage.server.Store(data, FileStorage.Type.ogg, Cassette.net.ID, 0U);
                                    Cassette.SetAudioId(id, 0);
                                }
                                CassetteAlarm = ItemCassette;
                                PrintWarning("The sound was loaded successfully #584");
                            }
                            break;
                        }
                    case 0:
                        {
                            PrintError($"Time out waiting for API #373");
                            break;
                        }
                    case 500:
                        {
                            if (response == "long")
                            {
                                PrintWarning("The video length is too long #3232");  //
                                return;
                            }
                            if (response == "wrong-url")
                            {
                                PrintWarning("Invalid URL on the video #9685"); ///
                                return;
                            }
                            PrintWarning("Unknown error, please inform the developer #3323");  //
                            break;
                        }
                }
            }, this);
        }
        private void ConnectedAlarm(IOEntity AlarmOne, IOEntity AlarmTwo)
        {
            AlarmOne.outputs[0].connectedTo = new IOEntity.IORef();
            AlarmOne.outputs[0].connectedTo.Set(AlarmTwo);
            AlarmOne.outputs[0].connectedToSlot = 0;
            AlarmOne.outputs[0].connectedTo.Init();

            AlarmTwo.inputs[0].connectedTo = new IOEntity.IORef();
            AlarmTwo.inputs[0].connectedTo.Set(AlarmOne);
            AlarmTwo.inputs[0].connectedToSlot = 0;
            AlarmTwo.inputs[0].connectedTo.Init();

            AlarmOne.MarkDirtyForceUpdateOutputs();
            AlarmOne.SendNetworkUpdate();

            AlarmTwo.MarkDirtyForceUpdateOutputs();
            AlarmTwo.SendNetworkUpdate();
        }
        #endregion

        #region Alarm Laser Spawn

        private void SpawnLaserAlarm()
        {
            if (!config.SphereSettings.BoomBoxSetting.UseSoundAlarm || !config.SphereSettings.UseLaser) return;

            Serializer Config = SerializerMain;

            for (Int32 Alarm = 0; Alarm < Config.LaserAlarm.Count; Alarm++)
            {
                var AlarmThis = Config.LaserAlarm[Alarm];

                IOEntity AlarmThisEntity = (IOEntity)GameManager.server.CreateEntity(AlarmThis.Name, monument.transform.TransformPoint(AlarmThis.Position), GetMonumentRotation(AlarmThis.Formul));
                AlarmThisEntity.OwnerID = 92929294944;
                AlarmThisEntity.Spawn();
                HealthSet(AlarmThisEntity);
                ElectricalAdd(AlarmThisEntity);

                if (Alarm == 0)
                {
                    var AlarmLast = IOList[IOList.Count - 1];
                    #region Connection Boombox

                    AlarmLast.outputs[0].connectedTo = new IOEntity.IORef();
                    AlarmLast.outputs[0].connectedTo.Set(AlarmThisEntity);
                    AlarmLast.outputs[0].connectedToSlot = 0;
                    AlarmLast.outputs[0].connectedTo.Init();

                    AlarmThisEntity.inputs[0].connectedTo = new IOEntity.IORef();
                    AlarmThisEntity.inputs[0].connectedTo.Set(AlarmLast);
                    AlarmThisEntity.inputs[0].connectedToSlot = 0;
                    AlarmThisEntity.inputs[0].connectedTo.Init();

                    AlarmLast.MarkDirtyForceUpdateOutputs();
                    AlarmLast.SendNetworkUpdate();

                    AlarmThisEntity.MarkDirtyForceUpdateOutputs();
                    AlarmThisEntity.SendNetworkUpdate();

                    IOList.Add(AlarmThisEntity);

                    #endregion
                }
                else
                {
                    #region Connection Other

                    IOEntity AlarmLast = IOList[IOList.Count - 1];
                    ConnectedAlarm(AlarmLast, AlarmThisEntity);
                    IOList.Add(AlarmThisEntity);

                    #endregion
                }
            }
        }

        #endregion

        #region Lamps Spawn
        private void SpawnLamps()
        {
            if (!config.SphereSettings.UseLamps) return;

            foreach (Serializer.Information Lamps in SerializerMain.Lamps)
            {
                String Prefab = Lamps.Name;

                BaseEntity LampsEntity = (BaseEntity)GameManager.server.CreateEntity(Prefab, monument.transform.TransformPoint(Lamps.Position), GetMonumentRotation(Lamps.Formul));
                LampsEntity.SetFlag(BaseEntity.Flags.Reserved8, false);
                LampsEntity.SetFlag(BaseEntity.Flags.On, false);
                LampsEntity.OwnerID = 92929294944;
                LampsEntity.Spawn();
                HealthSet(LampsEntity);

                ElectricalAdd(LampsEntity);
            }
        }
        #endregion

        #region Effects Spawn

        private IEnumerator SpawnEffect(Int32 CountEffect = 0)
        {
            if (!config.SphereSettings.UseEffects) yield return null;

            IEnumerable<Serializer.Information> Effects = CountEffect > 0 ? SerializerMain.Effects.Where(x => x.Name.Contains("rocket")).OrderBy(r => Random.Next()).Take(CountEffect) : SerializerMain.Effects;
            foreach (Serializer.Information Effect in Effects)
            {
                Single WaitTime = Convert.ToSingle(Random.NextDouble());

                BaseEntity Entity = (BaseEntity)GameManager.server.CreateEntity(Effect.Name, monument.transform.TransformPoint(Effect.Position), GetMonumentRotation(Effect.Formul));
                Entity.Spawn();

                //FireBall fireBall = Entity as FireBall;
                //if (fireBall != null)
                //{
                    
                //}

                TimedExplosive timedExplosive = Entity as TimedExplosive;
                if (timedExplosive != null)
                    timedExplosive.Explode();
                else OtherEntity.Add(Entity);

                yield return CoroutineEx.waitForSeconds(WaitTime);
            }
        }

        #endregion

        #region Chinook Controller
        public CH47Helicopter ChinoockHelicopter;
        [ChatCommand("test.s")]
        private void Tests(BasePlayer player)
        {
            SpawnChinoock();
        }
        private void SpawnChinoock()
        {
            if (!config.SphereSettings.ChinoockSettings.UseChinoock) return;
            ChinoockHelicopter = (CH47Helicopter)GameManager.server.CreateEntity("assets/prefabs/npc/ch47/ch47scientists.entity.prefab", new Vector3(0f, monument.transform.position.y + 150f, 0f), new Quaternion(), true);
            if (ChinoockHelicopter == null) return;

            CH47HelicopterAIController ch47AI = ChinoockHelicopter?.GetComponent<CH47HelicopterAIController>() ?? null;
            if (ch47AI == null) return;
            ChinoockHelicopter.Spawn();
            ChinoockHelicopter.OwnerID = 92929294944;
            ChinoockHelicopter.skinID = 92929294944;

            BehaviorChinook behavior = GetBehaviorChinoock();
            Configuration.SphereSetting.Chinook.BehaviourChinoock.DropCrateChinoock behaviourChinoock = behavior == BehaviorChinook.Die ? config.SphereSettings.ChinoockSettings.BehaviorSettingsChinoock.ChinoockDies : config.SphereSettings.ChinoockSettings.BehaviorSettingsChinoock.ChinoockLeaves;
            Boolean DropCrate = CanDropCrateCh47(behaviourChinoock);
            List<Configuration.LootSetting> LootList = behaviourChinoock.CrateLootSettings.UseCustomLoot ? behaviourChinoock.CrateLootSettings.LootSettings : null;
            ChinoockHelicopter.gameObject.AddComponent<FlyController>().Init(FlyType.Chinoock, behavior, monument.transform, DropCrate, LootList);
            ChinoockHelicopter.gameObject.AddComponent<TriggerExplosive>().Init();
        }

        private Boolean CanDropCrateCh47(Configuration.SphereSetting.Chinook.BehaviourChinoock.DropCrateChinoock behaviourChinoock)
        {
            if (!behaviourChinoock.UseDropCrateChinoock) return false;

            if (behaviourChinoock.RandomSettings.UseRandom)
                return IsRandom(behaviourChinoock.RandomSettings.RandomRangeCount.AmountMin, behaviourChinoock.RandomSettings.RandomRangeCount.AmountMax);
            else return IsRandom(behaviourChinoock.RareDropCrateChinoock);
        }
        private BehaviorChinook GetBehaviorChinoock() => (BehaviorChinook)Random.Next(Enum.GetNames(typeof(BehaviorChinook)).Length);

        #endregion

        #region Patrol Helicopter Spawn
        public BaseHelicopter PatrolHelicopter;

        private void SpawnPatrolHelicopter()
        {
            Configuration.SphereSetting.Helicopter HeliConfig = config.SphereSettings.HelicopterSettings;
            if (!HeliConfig.UseHelicopter) return;

            PatrolHelicopter = (BaseHelicopter)GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(0f, monument.transform.position.y + 150f, 0f), new Quaternion(), true);
            if (PatrolHelicopter == null) return;

            if (HeliConfig.LootHelicopter.UseCustomLoot)
                PatrolHelicopter.maxCratesToSpawn = 0;

            PatrolHelicopterAI heliAI = PatrolHelicopter?.GetComponent<PatrolHelicopterAI>() ?? null;
            if (heliAI == null) return;
            PatrolHelicopter.Spawn();
            PatrolHelicopter.OwnerID = 92929294944;

            PatrolHelicopter.SendNetworkUpdate();
            PatrolHelicopter.gameObject.AddComponent<FlyController>().Init(FlyType.Helicopter, monument.transform, HeliConfig.CountCircle, HeliConfig.MaxSpeed, HeliConfig.MoveSpeed);
        }

        private void SpawnCratesPatrolHelicopter()
        {
            Configuration.SphereSetting.Helicopter HeliConfig = config.SphereSettings.HelicopterSettings;
            if (!HeliConfig.LootHelicopter.UseCustomLoot) return;

            Int32 NeedsCrate = GetRandom(HeliConfig.LootHelicopter.AmountRandomCrate.AmountMin, HeliConfig.LootHelicopter.AmountRandomCrate.AmountMax);
            for (Int32 CrateSpawn = 0; CrateSpawn < NeedsCrate; CrateSpawn++)
                DropCratesPatrolHelicopter();
        }
        private void DropCratesPatrolHelicopter()
        {
            Configuration.SphereSetting.Helicopter HeliConfig = config.SphereSettings.HelicopterSettings;

            Vector3 onUnitSphere2 = UnityEngine.Random.onUnitSphere;
            Vector3 pos = PatrolHelicopter.transform.position + new Vector3(0f, 1.5f, 0f) + onUnitSphere2 * UnityEngine.Random.Range(2f, 3f);
            Vector3 vector = PatrolHelicopter.myAI.GetLastMoveDir() * PatrolHelicopter.myAI.GetMoveSpeed() * 0.75f;

            BaseEntity Crate = GameManager.server.CreateEntity(PatrolHelicopter.crateToDrop.resourcePath, pos, Quaternion.LookRotation(onUnitSphere2), true);
            Crate.Spawn();
            LootContainer lootContainer = Crate as LootContainer;
            lootContainer.inventory.itemList.Clear();

            Int32 CountLootNeeds = GetRandom(1, 6);
            for (Int32 Items = 0; Items < CountLootNeeds; Items++)
            {
                Configuration.LootSetting CrateLoot = HeliConfig.LootHelicopter.LootSettings.GetRandom();

                Int32 ItemAmount = GetRandom(CrateLoot.CountSetting.AmountMin, CrateLoot.CountSetting.AmountMax);
                Item item = ItemManager.CreateByName(CrateLoot.Shortname, ItemAmount, CrateLoot.SkinID);
                if (!String.IsNullOrWhiteSpace(CrateLoot.DisplayName))
                    item.name = CrateLoot.DisplayName;

                item.MoveToContainer(lootContainer.inventory);
            }

            if (lootContainer)
                lootContainer.Invoke(new Action(lootContainer.RemoveMe), 1800f);

            Collider collider = Crate.GetComponent<Collider>();
            Rigidbody rigidbody = Crate.gameObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = true;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.mass = 2f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.velocity = vector + onUnitSphere2 * UnityEngine.Random.Range(1f, 3f);
            rigidbody.angularVelocity = Vector3Ex.Range(-1.75f, 1.75f);
            rigidbody.drag = 0.5f * (rigidbody.mass / 5f);
            rigidbody.angularDrag = 0.2f * (rigidbody.mass / 5f);
            FireBall fireBall = GameManager.server.CreateEntity(PatrolHelicopter.fireBall.resourcePath, default(Vector3), default(Quaternion), true) as global::FireBall;
            if (fireBall)
            {
                fireBall.SetParent(Crate, false, false);
                fireBall.Spawn();
                fireBall.GetComponent<Rigidbody>().isKinematic = true;
                fireBall.GetComponent<Collider>().enabled = false;
            }
            Crate.SendMessage("SetLockingEnt", fireBall.gameObject, SendMessageOptions.DontRequireReceiver);
        }

        #endregion

        void SpawnedZone()
        {
            Vector3 Position = monument.transform.TransformPoint(SerializerMain.Triggers.Position);
            TriggerZoneAround = new GameObject().AddComponent<TriggerZone>();
            TriggerZoneAround.Init(Position, 100f, Tier.Around);

            Boolean UseHelicopter = config.SphereSettings.HelicopterSettings.UseHelicopter;
            Boolean UseChinook = config.SphereSettings.ChinoockSettings.UseChinoock;
            TriggerZoneTier1 = new GameObject().AddComponent<TriggerZone>();
            TriggerZoneTier1.Init(Position, 26f, Tier.Tier1, UseHelicopter, UseChinook);

            Configuration.SphereSetting.Radiation Radiation = config.SphereSettings.RadiationSettings;
            if (Radiation.UseRadiation)
                TriggerZoneTier1.InitializeRadiationZone(Radiation.StartingAmountRadiation, Radiation.MultiplierRadiation, Radiation.MaximumAmountRadiation, Radiation.SecondUpdate);
        }
        public Int32 GetRandom(Int32 Min, Int32 Max) => Random.Next(Min, Max);
        private Boolean IsRandom(Int32 Rare) => Random.Next(0, 100) >= (100 - Rare);
        private Boolean IsRandom(Int32 Min, Int32 Max) => Random.Next(0, 100) >= (100 - GetRandom(Min, Max));
        private Vector3 GetPositionDespawn()
        {
            Single x = TerrainMeta.Size.x;
            Single y = 70f;
            Vector3 val = Vector3Ex.Range(-1f, 1f);
            val.y = 0f;
            val.Normalize();
            val *= x * 1f;
            val.y = y;

            return val;
        }

        Quaternion GetMonumentRotation(Serializer.Information.QuaternionFormul Formul)
        {
            Quaternion monumentQT = new Quaternion(Formul.X, Formul.Y, Formul.Z, Formul.W);
            return monumentQT;
        }

        private Boolean IsSphereExisting() => monument != null;
        private Vector3 RandomCircle(Vector3 center, Single radius)
        {
            Single ang = UnityEngine.Random.value * 360;
            Vector3 pos;
            pos.x = center.x + radius * Mathf.Sin(ang * Mathf.Deg2Rad);
            pos.y = center.y;
            pos.z = center.z + radius * Mathf.Cos(ang * Mathf.Deg2Rad);
            return pos;
        }

        #endregion

        #region API
        public Boolean IsEventStart() => EventStarted;

        #endregion

        #region Commands

        [ConsoleCommand("iqsp")]
        void IQSphereConsoleCommand(ConsoleSystem.Arg args)
        {
            BasePlayer admin = args.Player();
            if (admin != null) 
                if(!admin.IsAdmin)
                    return;

            CommandActionIQSP(admin, args.Args, true);
        }

        [ChatCommand("iqsp")]
        void IQSphereChatCommand(BasePlayer admin, String cmd, String[] args)
        {
            if (!admin.IsAdmin) return;
            CommandActionIQSP(admin, args, false);
        }

        void SendCommandAlert(BasePlayer admin, String Message, Boolean Console)
        {
            if (!Console)
                SendChat(admin, Message);
            else Puts(Message);
        }
        void CommandActionIQSP(BasePlayer admin, String[] args, Boolean Console)
        {
            if (args == null || args.Length < 1)
            {
                SendCommandAlert(admin, "\nEN : Syntax error!\nCommands :\niqsp start - standard start of the event in normal mode\niqsp quick.start - quick start of the event\niqsp stop - standard shutdown of the event\niqsp quick.stop - quick shutdown of the event\n\nRU :Ошибка синтаксиса!\nКоманды :\niqsp start - стандартный запуск мероприятия в нормальном режиме\niqsp quick.start - быстрый запуск мероприятия\niqsp stop - стандартное выключение мероприятия\niqsp quick.stop  - быстрое выключение мероприятия", Console);
                return;
            }
            String Action = args[0];

            if (String.IsNullOrWhiteSpace(Action))
            {
                SendCommandAlert(admin, "\nEN : Syntax error!\nCommands :\niqsp start - standard start of the event in normal mode\niqsp quick.start - quick start of the event\niqsp stop - standard shutdown of the event\niqsp quick.stop - quick shutdown of the event\n\nRU :Ошибка синтаксиса!\nКоманды :\niqsp start - стандартный запуск мероприятия в нормальном режиме\niqsp quick.start - быстрый запуск мероприятия\niqsp stop - стандартное выключение мероприятия\niqsp quick.stop  - быстрое выключение мероприятия", Console);
                return;
            }
            switch (Action)
            {
                case "start":
                    {
                        if (CassetteAlarm == null)
                        {
                            SendCommandAlert(admin, "\nEN : The sound is still initialized by the plugin, try again later\nRU : Звук еще инициализируется плагином, попробуйте позже", Console);
                            return;
                        }

                        if (EventStarted)
                        {
                            SendCommandAlert(admin, "\nEN : The event has already been launched\nRU : Мероприятие уже запущено!", Console);
                            return;
                        }

                        PreStartedController();
                        SendCommandAlert(admin, "\nEN : You have successfully launched the event in standard mode\nRU : Вы успешно запустили мероприятие в стандартном режиме", Console);
                        break;
                    }
                case "quick.start":
                    {
                        if (CassetteAlarm == null)
                        {
                            SendCommandAlert(admin, "\nEN : The sound is still initialized by the plugin, try again later\nRU : Звук еще инициализируется плагином, попробуйте позже", Console);
                            return;
                        }
                        if (EventStarted)
                        {
                            StopSphereEvent();

                            if (TimerPreStarted != null && !TimerPreStarted.Destroyed)
                            {
                                TimerPreStarted.Destroy();
                                TimerPreStarted = null;
                            }
                            StartSphereEvent();
                            SendCommandAlert(admin, "\nEN : You have successfully launched the event in fast mode, the previous event was disabled\nRU : Вы успешно запустили мероприятие в быстром режиме, прошлое мероприятие было отключено", Console);
                        }
                        else
                        {
                            StartSphereEvent();
                            SendCommandAlert(admin, "\nEN : You have successfully launched the event in fast mode\nRU : Вы успешно запустили мероприятие в быстром режиме", Console);
                        }
                        break;
                    }
                case "stop":
                    {
                        if (!EventStarted)
                        {
                            SendCommandAlert(admin, "\nEN : The event has not been launched yet\nRU : Мероприятие еще не было запущено", Console);
                            return;
                        }
                        PreStopSphereEvent();
                        SendCommandAlert(admin, "\nEN : You have successfully completed the event in the standard mode\nRU : Вы успешно завершили мероприятие в стандартном режиме", Console);
                        break;
                    }
                case "quick.stop":
                    {
                        if (!EventStarted)
                        {
                            SendCommandAlert(admin, "\nEN : The event has not been launched yet\nRU : Мероприятие еще не было запущено", Console);
                            return;
                        }

                        StopSphereEvent();
                        SendCommandAlert(admin, "\nEN : You have successfully disabled the event in quick mode\nRU : Вы успешно отключили мероприятие в быстром режиме", Console);
                        break;
                    }
            }
        }
        #endregion

        #region Metods

        #region Image

        private void ImageDownload()
        {
            Configuration.StorySettings.PersonSettings Images = config.StorySetting.Persons;

            if (!HasImage($"IQSE_{Images.ChinookPNG}"))
                AddImage(Images.ChinookPNG, $"IQSE_{Images.ChinookPNG}");
            if (!HasImage($"IQSE_{Images.HelicopterPNG}"))
                AddImage(Images.HelicopterPNG, $"IQSE_{Images.HelicopterPNG}");
            if (!HasImage($"IQSE_{Images.ScientistPNG}"))
                AddImage(Images.ScientistPNG, $"IQSE_{Images.ScientistPNG}");    
            if (!HasImage($"IQSE_{Images.InformationPNG}"))
                AddImage(Images.InformationPNG, $"IQSE_{Images.InformationPNG}");
            
            if (!HasImage($"IQSE_BACKGROUND_https://i.imgur.com/0HQWTR4.png"))
                AddImage("https://i.imgur.com/0HQWTR4.png", $"IQSE_BACKGROUND_https://i.imgur.com/0HQWTR4.png");            
            if (!HasImage($"IQSE_WARNING_https://i.imgur.com/1Ax2d5U.png"))
                AddImage("https://i.imgur.com/1Ax2d5U.png", $"IQSE_WARNING_https://i.imgur.com/0HQWTR4.png");
        }

        #endregion

        #region Bots

        private void SpawnBots(Configuration.SpawnBots.GeneralBotSphere BotPattern, Tier TierType)
        {
            if (!BotPattern.UseBots || SerializerMain.Bots == null || SerializerMain.Bots.Count == 0) return;
            List<Serializer.BotInformation> BotsList = SerializerMain.Bots.Where(bot => bot.TierSpawn == TierType).ToList();

            JArray arrayWear = new JArray();
            foreach (Configuration.SpawnBots.GeneralBotSphere.ItemBot item in BotPattern.WearNPC)
                arrayWear.Add(new JObject { ["ShortName"] = item.Shortname, ["Amount"] = 1, ["SkinID"] = item.SkinID, });
            JArray arrayBelt = new JArray();
            foreach (Configuration.SpawnBots.GeneralBotSphere.ItemBot item in BotPattern.BeltNPC)
                arrayBelt.Add(new JObject { ["ShortName"] = item.Shortname, ["Amount"] = 1, ["SkinID"] = item.SkinID, ["Mods"] = new JArray { item.Mods.Select(y => y) } });
            JObject configNpc = new JObject()
            {
                ["Name"] = BotPattern.DisplayNameBot,
                ["WearItems"] = arrayWear,
                ["BeltItems"] = arrayBelt,
                ["Kit"] = "",
                ["Health"] = BotPattern.HealthBot,
                ["RoamRange"] = BotPattern.RoamRange,
                ["ChaseRange"] = BotPattern.ChaseRange,
                ["DamageScale"] = BotPattern.DamageScale,
                ["AimConeScale"] = BotPattern.AimConeScale,
                ["DisableRadio"] = false,
                ["Stationary"] = false,
                ["CanUseWeaponMounted"] = false,
                ["CanRunAwayWater"] = true,
                ["Speed"] = BotPattern.Speed,
                ["Sensory"] = new JObject()
                {
                    ["AttackRangeMultiplier"] = BotPattern.AttackRangeMultiplier,
                    ["SenseRange"] = BotPattern.RadiusVisBots,
                    ["CheckVisionCone"] = BotPattern.CheckVisionCone,
                    ["MemoryDuration"] = 300f,
                    ["VisionCone"] = BotPattern.VisionCone,
                }
            };

            Int32 CountSpawn = GetRandom(BotPattern.CounSetting.AmountMin, BotPattern.CounSetting.AmountMax);
            for (int i = 0; i < CountSpawn; i++)
            {
                Serializer.BotInformation Bots = BotsList.GetRandom();
                Vector3 SpawnPosBot = monument.transform.TransformPoint(Bots.Information.Position);

                ScientistNPC npc = (ScientistNPC)NpcSpawn.Call("SpawnNpc", SpawnPosBot, configNpc);
                npc.OwnerID = 92929294944;

                nPCMonitors.Add(npc, Bots.TierSpawn);
            }
        }

        private void SpawnBotsPatterns()
        {
            Configuration.SpawnBots.GeneralBotSphere AroundPattern = config.BotsSetting.AroundBots;
            Configuration.SpawnBots.GeneralBotSphere UnderPattern = config.BotsSetting.UnderBots;
            Configuration.SpawnBots.GeneralBotSphere Tier1Pattern = config.BotsSetting.LevelBots.Tier1;
            Configuration.SpawnBots.GeneralBotSphere Tier2Pattern = config.BotsSetting.LevelBots.Tier2;
            Configuration.SpawnBots.GeneralBotSphere Tier3Pattern = config.BotsSetting.LevelBots.Tier3;

            SpawnBots(AroundPattern, Tier.Around);
            SpawnBots(UnderPattern, Tier.Under);
            SpawnBots(Tier1Pattern, Tier.Tier1);
            SpawnBots(Tier2Pattern, Tier.Tier2);
            SpawnBots(Tier3Pattern, Tier.Tier3);
        }

        #endregion

        #region Story

        #region UI

        private void StoryUI(StoryPerson Person, String LangKey)
        {
            Configuration.StorySettings.PersonSettings Images = config.StorySetting.Persons;
            String PNG = Person == StoryPerson.Scientist ? $"IQSE_{Images.ScientistPNG}" : Person == StoryPerson.Helicopter ? $"IQSE_{Images.HelicopterPNG}" : Person == StoryPerson.Chinook ? $"IQSE_{Images.ChinookPNG}" : null;
            String NAME = Person == StoryPerson.Scientist ? "STORY_PERSON_SCIENTIST" : Person == StoryPerson.Helicopter ? "STORY_PERSON_HELICOPTER" : Person == StoryPerson.Chinook ? "STORY_PERSON_CHINOOK" : null;
            String Path = String.Empty;

            String CorrectedJson = SerializerMain.JsonUI.Replace("%PNG%", GetImage(PNG)).Replace("%BACKGROUND%", GetImage("IQSE_BACKGROUND_https://i.imgur.com/0HQWTR4.png"));

            foreach (BasePlayer player in BasePlayer.activePlayerList.Where(p => p.HasFlag(BaseEntity.Flags.Reserved10)))
            {
                CorrectedJson = CorrectedJson.Replace("%NAME%", GetLang(NAME, player.UserIDString));

                CuiHelper.DestroyUi(player, "IQSPHERE_PANEL");
                CuiHelper.AddUi(player, CorrectedJson);

                Path = config.StorySetting.RadioSounds.GetRandom();
                Effect effect = new Effect(Path, player, 0, new Vector3(), new Vector3());
                EffectNetwork.Send(effect, player.Connection);

                Coroutine LabelStoryPlayer = player.StartCoroutine(StoryLabel(player, LangKey));

                if (RoutinePlayer.ContainsKey(player))
                {
                    player.StopCoroutine(RoutinePlayer[player]);
                    RoutinePlayer[player] = LabelStoryPlayer;
                }
                else RoutinePlayer.Add(player, LabelStoryPlayer);

            }   
        }

        private IEnumerator StoryLabel(BasePlayer player,String LangKey)
        {
            String Text = String.Empty;
            Int32 Symbols = 0;
            Int32 StringCount = 0;

            String Message = GetLang(LangKey, player.UserIDString);
            String[] MessageArray = Message.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            foreach (String SimpleString in MessageArray)
            {
                if ((MessageArray.Length >= StringCount && MessageArray[StringCount].Length >= 44 - Symbols))
                    StringCount++;

                foreach (Char Symbol in $"{SimpleString} ")
                {
                    if (StringCount >= 5)
                    {
                        Text = String.Empty;
                        StringCount = 0;
                        Symbols = 0;
                    }

                    Text = Text + Symbol;
                    Symbols++;

                    String CorrectedJson = SerializerMain.JsonUILabel.Replace("%TEXT%", Text);
                    CuiHelper.DestroyUi(player, "IQSPHERE_TEXT");
                    CuiHelper.AddUi(player, CorrectedJson);

                    yield return CoroutineEx.waitForSeconds(0.15f);
                }
                StringCount++;
            }
            yield return CoroutineEx.waitForSeconds(0.5f);
            CuiHelper.DestroyUi(player, "IQSPHERE_PANEL");

            player.StopCoroutine(StoryLabel(player, LangKey));
        }

        #endregion

        #endregion

        #region Crate Spawn
        private void SpawnCrates()
        {
            Configuration.EventSetting.CustomLootSphere LootSphere = config.EventSettings.LootSphereSetting;
            List<Vector3> CratePositions = SpawnCratePosition();

            for (Int32 CountCrate = 0; CountCrate < LootSphere.SpawnCrateCount; CountCrate++)
            {
                Configuration.EventSetting.CustomLootSphere.CrateSettings Crate = LootSphere.CrateList.GetRandom();

                BaseEntity CrateEntity = (BaseEntity)GameManager.server.CreateEntity(Crate.CratePrefab, CratePositions.GetRandom());
                CrateEntity.Spawn();
               // CrateEntity.OwnerID = 92929294944;
                CrateEntity.skinID = 92929294944;

                if (!Crate.UseDefaultLoot)
                {
                    LootContainer Container = CrateEntity.GetComponent<LootContainer>();
                    Container.inventory.itemList.Clear();

                    foreach (Configuration.EventSetting.CustomLootSphere.CrateSettings.LootSettings ItemCrate in Crate.ItemSettings.OrderBy(v => Random.Next()).Where(x => IsRandom(x.RareDrop)).Take(GetRandom(Crate.CountSpawnItems.AmountMin, Crate.CountSpawnItems.AmountMax)))
                    {
                        Int32 Amount = GetRandom(ItemCrate.LootSetting.CountSetting.AmountMin, ItemCrate.LootSetting.CountSetting.AmountMax);
                        Item ItemToCrate = ItemManager.CreateByName(ItemCrate.LootSetting.Shortname, Amount, ItemCrate.LootSetting.SkinID);
                        if (!String.IsNullOrWhiteSpace(ItemCrate.LootSetting.DisplayName))
                            ItemToCrate.name = ItemCrate.LootSetting.DisplayName;

                        ItemToCrate.MoveToContainer(Container.inventory);
                    }
                }

                OtherEntity.Add(CrateEntity);
            }
        }
        private List<Vector3> SpawnCratePosition()
        {
            List<Vector3> ResultPositions = new List<Vector3>();
            Vector3 Center = new Vector3(monument.transform.position.x, monument.transform.position.y + 71.6f, monument.transform.position.z);

            for (Int32 Try = 0; Try < 100; Try++)
            {
                Vector3 Position = RandomCircle(Center, 7.0f);

                if (ResultPositions.Count == 0)
                    ResultPositions.Add(Position);

                Int32 TryLook = 0;
                for (Int32 Check = 0; Check < ResultPositions.Count; Check++)
                    if (Vector3.Distance(ResultPositions[Check], Position) < 3f)
                        TryLook++;

                if (TryLook == 0)
                    ResultPositions.Add(Position);
            }
            return ResultPositions;
        }
        #endregion

        #region Warning
        private void WarningAlert(BasePlayer player)
        {
            if (!config.EventSettings.OtherSettings.UseAlertTriggerEnter) return;
            if (PlayerWarnings.Contains(player)) return;

            WarningUI(player, "WARNING_ALERT_TITLE");
            PlayerWarnings.Add(player);
        }
        private void WarningUI(BasePlayer player, String LangKey, Single TimeDestoryUI = 10f)
        {
            Configuration.StorySettings.PersonSettings Images = config.StorySetting.Persons;
            String CorrectedJson = SerializerMain.JsonUIWarning.Replace("%BACKGROUND%", GetImage("IQSE_WARNING_https://i.imgur.com/0HQWTR4.png")).Replace("%PNG%", GetImage($"IQSE_{Images.InformationPNG}")).Replace("%TEXT%", GetLang(LangKey, player.UserIDString));
            CuiHelper.DestroyUi(player, "IQSPHERE_WARNING");
            CuiHelper.AddUi(player, CorrectedJson);

            player.Invoke(() =>
            {
                CuiHelper.DestroyUi(player, "IQSPHERE_WARNING");
                player.CancelInvoke();
            }, TimeDestoryUI);
        }

        #endregion

        #region Markers

        private void ShowMarker()
        {
            if (!config.EventSettings.MapMarkerSettings.UseMarker) return;

            HideMarker();

            Configuration.EventSetting.MapMarkerSetting Marker = config.EventSettings.MapMarkerSettings;
            Vector3 Position = monument.transform.TransformPoint(SerializerMain.Triggers.Position);

            MarkerZoneEvent = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", Position) as MapMarkerGenericRadius;
            if (MarkerZoneEvent == null) return;

            MarkerZoneEvent.alpha = Marker.AlphaMarker;
            if (!ColorUtility.TryParseHtmlString(Marker.ColorMarker, out MarkerZoneEvent.color1))
            {
                MarkerZoneEvent.color1 = Color.black;
                _.PrintError($"Invalid map marker color1: {Marker.ColorMarker}");
            }
            if (!ColorUtility.TryParseHtmlString(Marker.ColorMarker, out MarkerZoneEvent.color2))
            {
                MarkerZoneEvent.color2 = Color.white;
                _.PrintError($"Invalid map marker color2: {Marker.ColorMarker}");
            }
            MarkerZoneEvent.name = "MarkerZoneEvent";
            MarkerZoneEvent.radius = 0.66f;
            MarkerZoneEvent.Spawn();
            MarkerZoneEvent.SendUpdate();
        }
        private void UpdateMarker(BasePlayer player)
        {
            NextTick(() =>
            {
                if (player == null || MarkerZoneEvent == null) return;

                MarkerZoneEvent.SendUpdate();
            });
        }
        private void HideMarker() => MarkerZoneEvent?.Kill();

        #endregion

        #endregion

        #region Status Event

        private void GetInformation()
        {
            try
            {
                JObject jsonQueryBody = new JObject(new JProperty("x", monument.transform.rotation.eulerAngles.x),new JProperty("y", monument.transform.rotation.eulerAngles.y), new JProperty("z", monument.transform.rotation.eulerAngles.z));
                String body = jsonQueryBody.ToString();

                webrequest.Enqueue($"https://iqsystem.skyplugins.ru/iqsphere/parse-object/z7YpaI6r", "MonumentRotation=" + body, (code, response) =>
                {
                    switch (code)
                    {
                        case 404:
                            {
                                PrintError($"ERROR #562  {response} | ERROR #:562  (Discord - Mercury#5212)"); 
                                break;
                            }
                        case 503:
                            {
                                PrintError($"ERROR #55623 Your plugin version is outdated!Upgrade to the latest version! (Discord - Mercury#5212)");
                                break;
                            }
                        case 200:
                            {
                                Serializer obj = JsonConvert.DeserializeObject<Serializer>(response);
                                SerializerMain = obj;
                                WriteData();

                                PrintWarning("#9945 Successful data acquisition");

                                break;
                            }
                    }
                }, this, RequestMethod.POST);
            }
            catch (Exception e)
            {
                PrintError($"ERROR #8573 An error occurred while connecting, please inform the developer Discord - Mercury#5212\nError : {e}");
            }
        }
        private void PreStartedEvent()
        {
            if (TimerPreStarted != null && !TimerPreStarted.Destroyed)
            {
                TimerPreStarted.Destroy();
                TimerPreStarted = null;
            }
            TimerPreStarted = timer.Once(config.EventSettings.StartTime, () => { PreStartedController(); });
        }
        private void PreStartedController(Int32 Try = 0)
        {
            if (EventStarted)
                return;

            Int32 TryStart = Try;

            if (TryStart == 3)
            {
                StartSphereEvent();
                return;
            }

            List<BasePlayer> PlayersVis = new List<BasePlayer>();
            Vis.Entities(monument.transform.position, 150f, PlayersVis, LayerMask.GetMask("Player (Server)"));

            if (PlayersVis != null && PlayersVis.Count != 0)
            {
                foreach (BasePlayer player in PlayersVis.Where(p => p.IsAlive() && !p.IsSleeping() && !p.IsNpc))
                    WarningUI(player, "WARNING_PRE_STARTED_ALERT_TITLE", 60f);

                TryStart++;
                if (TimerPreStarted != null && !TimerPreStarted.Destroyed)
                {
                    TimerPreStarted.Destroy();
                    TimerPreStarted = null;
                }
                TimerPreStarted = timer.Once(65f, () => PreStartedController(TryStart));
            }
            else StartSphereEvent();
        }
        private void StartSphereEvent()
        {
            if (plugins.Exists("BetterNpc"))
                if (config.referencePlugin.BetterNpcSetting.DestroyedNpc)
                    BetterNpc.Call("DestroyController", "The Dome");

            EventStarted = true;
            SubscribePlugin();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                SendChat(player, GetLang("CHAT_ALERT_START_SPHERE", player.UserIDString));

            SpawnEvents = ServerMgr.Instance.StartCoroutine(SpawnedEventElements());
        }

        private IEnumerator SpawnedEventElements()
        {
            SpawnedZone();
            yield return CoroutineEx.waitForSeconds(2f);
            SpawnBotsPatterns();
            yield return CoroutineEx.waitForSeconds(2f);
            SpawnAlarm();
            yield return CoroutineEx.waitForSeconds(2f);
            SpawnLamps();
            yield return CoroutineEx.waitForSeconds(2f);
            SpawnSoundAlarm();
            yield return CoroutineEx.waitForSeconds(2f);
            SpawnLaserAlarm();
            yield return CoroutineEx.waitForSeconds(2f);
            SpawnSamDefense();
            yield return CoroutineEx.waitForSeconds(2f);
            ShowMarker();
            yield return CoroutineEx.waitForSeconds(2f);

            if (config.EventSettings.OtherSettings.PagerSettings.UsePagerAlert)
                foreach (PagerEntity Pager in Pagers)
                    Pager.SetFlag(BaseEntity.Flags.On, true);

            yield return CoroutineEx.waitForSeconds(2f);

            /////
            if (config.EventSettings.UseTimerStop)
            {
                if (TimerPreStopped != null && !TimerPreStopped.Destroyed)
                {
                    TimerPreStopped.Destroy();
                    TimerPreStopped = null;
                }
                TimerPreStopped = timer.Once(config.EventSettings.StopEventTime, () => PreStopSphereEvent());
            }
            else
            {
                IsLootingEventTier3 = false;
                if (TimerPreStopped != null && !TimerPreStopped.Destroyed)
                {
                    TimerPreStopped.Destroy();
                    TimerPreStopped = null;
                }
                TimerPreStopped = timer.Every(60f, () =>
                {
                    if (!IsLootingEventTier3) return;
                    PreStopSphereEvent();
                    TimerPreStopped.Destroy();
                    TimerPreStopped = null;
                });
            }
        }
        private void UnSubscribePlugin()
        {
            Unsubscribe("OnCorpsePopulate");
            Unsubscribe("OnUserCommand");
            Unsubscribe("OnRfFrequencyChanged");
        }
        private void SubscribePlugin()
        {
            Subscribe("OnCorpsePopulate");
            if (config.EventSettings.SettingZoneEvent.BlockCommands.UseBlockCommand)
                Subscribe("OnUserCommand");
            if (config.EventSettings.OtherSettings.PagerSettings.UsePagerAlert)
                Subscribe("OnRfFrequencyChanged");
        }
        private void PreStopSphereEvent()
        {
            if (!EventStarted) return;
            TriggerZoneAround.InitializeRadiationZone(0f, 1f, 200f, 5f);

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                SendChat(player, GetLang("CHAT_ALERT_STOP_SPHERE", player.UserIDString));

            if (TimerStopped != null && !TimerStopped.Destroyed)
            {
                TimerStopped.Destroy();
                TimerStopped = null;
            }
            TimerStopped = timer.Once(600f, () => { StopSphereEvent(); });
        }
        private void StopSphereEvent()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "IQSPHERE_PANEL");
                CuiHelper.DestroyUi(player, "IQSPHERE_WARNING");

                if (EventStarted)
                    SendChat(player, GetLang("CHAT_ALERT_STOP_SPHERE_SUCCESS", player.UserIDString));

                if(player.HasFlag(BaseEntity.Flags.Reserved10))
                    player.SetFlag(BaseEntity.Flags.Reserved10, false);
            }

            EventStarted = false;
            UnSubscribePlugin();

            foreach(KeyValuePair<BasePlayer, Coroutine> Routine in RoutinePlayer.Where(x => x.Value != null))
                Routine.Key.StopCoroutine(Routine.Value);

            if (SpawnEffects != null)
            {
                ServerMgr.Instance.StopCoroutine(SpawnEffects);
                if (SpawnEffects != null)
                    SpawnEffects = null;
            }

            if (SpawnEvents != null)
            {
                ServerMgr.Instance.StopCoroutine(SpawnEvents);
                if (SpawnEvents != null)
                    SpawnEvents = null;
            }

            if (TimerPreStarted != null && !TimerPreStarted.Destroyed)
            {
                TimerPreStarted.Destroy();
                TimerPreStarted = null;
            }

            if (TimerPreStopped != null && !TimerPreStopped.Destroyed)
            {
                TimerPreStopped.Destroy();
                TimerPreStopped = null;
            }

            if (TimerSpawnSoundAlarms != null && !TimerSpawnSoundAlarms.Destroyed)
            {
                TimerSpawnSoundAlarms.Destroy();
                TimerSpawnSoundAlarms = null;
            }

            if (TimerStopped != null && !TimerStopped.Destroyed)
            {
                TimerStopped.Destroy();
                TimerStopped = null;
            }
            HideMarker();
            if (PlayerWarnings != null || PlayerWarnings.Count != 0)
                PlayerWarnings.Clear();

            if (config.EventSettings.OtherSettings.PagerSettings.UsePagerAlert)
            {
                foreach (PagerEntity Pager in Pagers)
                    Pager.SetFlag(BaseEntity.Flags.On, false);

                Pagers.Clear();
            }
            RoutinePlayer.Clear();
            ClearEntity();
            PreStartedEvent();

            if (plugins.Exists("BetterNpc"))
                if (config.referencePlugin.BetterNpcSetting.DestroyedNpc)
                    BetterNpc.Call("CreateController", "The Dome");
        }

        #endregion

        #region Triggers
        public class TriggerExplosive : FacepunchBehaviour
        {
            private void Awake()
            {
                gameObject.layer = (Int32)Rust.Layer.Reserved1;
                gameObject.name = "IQSPHEREEVENT_ZONE_EXPLOSIVE";
            }
            public void Init()
            {
                UpdateCollider();
                gameObject.SetActive(true);
                enabled = true;
            }
            private void OnTriggerEnter(Collider other)
            {
                TimedExplosive explosive = other.GetComponentInParent<TimedExplosive>();
                if (explosive == null) return;
                explosive.Explode();
            }
            private void OnDestroy()
            {
                Destroy(gameObject);
            }
            public void Kill()
            {
                Destroy(gameObject);
            }

            private void UpdateCollider()
            {
                var sphereCollider = gameObject.GetComponent<SphereCollider>();
                {
                    if (sphereCollider == null)
                    {
                        sphereCollider = gameObject.AddComponent<SphereCollider>();
                        sphereCollider.isTrigger = true;
                    }
                    sphereCollider.radius = 30f;
                }
            }
        }

        public class TriggerZone : FacepunchBehaviour
        {
            private TriggerRadiation Radiation;
            private Single MultiplierRadiation;
            private Single MaximumAmountRadiation;
            private Single SecondUpdate;
            private Boolean UseHelicopter;
            private Boolean UseChinook;

            private Single Radius;
            private Tier TierInfo;

            private Boolean ActivatedTier1 = false;

            private void Awake()
            {
                gameObject.layer = (Int32)Rust.Layer.Reserved1;
                gameObject.name = "IQSPHEREEVENT_ZONE";
            }
            public void Init(Vector3 Position, Single Radius, Tier TierInfo)
            {
                this.Radius = Radius;
                this.TierInfo = TierInfo;
                transform.position = Position;
                UpdateCollider();
                gameObject.SetActive(true);
                enabled = true;
            }

            public void Init(Vector3 Position, Single Radius, Tier TierInfo, Boolean UseHelicopter, Boolean UseChinook)
            {
                this.Radius = Radius;
                this.TierInfo = TierInfo;
                this.UseHelicopter = UseHelicopter;
                this.UseChinook = UseChinook;
                transform.position = Position;
                UpdateCollider();
                gameObject.SetActive(true);
                enabled = true;
            }

            private void OnTriggerEnter(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player == null || player.IsNpc || !player.userID.IsSteamId()) return;

                switch (TierInfo)
                {
                    case Tier.Tier1:
                        {
                            if (ActivatedTier1) return;
                            ActivatedTier1 = true;

                            _.SpawnEffects = ServerMgr.Instance.StartCoroutine(_.SpawnEffect());

                            Invoke(() =>
                            {
                                _.StoryUI(StoryPerson.Scientist, "STORY_ALERT_ATTACKED");
                                _.ElectricalTurn(true);
                            }, 3f);

                            if (Radiation != null)
                                Invoke(() =>
                                {
                                    _.StoryUI(StoryPerson.Scientist, "STORY_ALERT_RADIATION_TURN");
                                    UpdateRadiation();
                                }, 30f);

                            if (UseHelicopter)
                                Invoke(() =>
                                {
                                    _.StoryUI(StoryPerson.Scientist, "STORY_ALERT_CALL_HELI");
                                    _.SpawnPatrolHelicopter();
                                }, 100f);

                            if (config.EventSettings.LootSphereSetting.UseCustomLoot)
                                Invoke(() => { _.SpawnCrates(); }, 250f);

                            if (UseChinook)
                                Invoke(() =>
                                {
                                    _.StoryUI(StoryPerson.Scientist, "STORY_ALERT_CALL_CH47");
                                    _.SpawnChinoock();
                                }, 400f);
                            break;
                        }
                    case Tier.Around:
                        {
                            if (!player.HasFlag(BaseEntity.Flags.Reserved10))
                                player.SetFlag(BaseEntity.Flags.Reserved10, true);

                            _.WarningAlert(player);

                            if (_.TriggerZoneAround.Radiation != null)
                                _.WarningUI(player, "WARNING_PRE_STOPPED_ALERT_TITLE", 300f);
                            break;
                        }
                }
            }
            void OnTriggerExit(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player == null || player.IsNpc || !player.userID.IsSteamId()) return;

                switch (TierInfo)
                {
                    case Tier.Around:
                        {
                            if (player.HasFlag(BaseEntity.Flags.Reserved10))
                            {
                                player.SetFlag(BaseEntity.Flags.Reserved10, false);
                                if (_.RoutinePlayer.ContainsKey(player))
                                {
                                    player.StopCoroutine(_.RoutinePlayer[player]);
                                    _.RoutinePlayer.Remove(player);
                                }
                            }
                            CuiHelper.DestroyUi(player, "IQSPHERE_PANEL");

                            if (_.TriggerZoneAround.Radiation != null)
                            {
                                CuiHelper.DestroyUi(player, "IQSPHERE_WARNING");
                                player.CancelInvoke();
                            }

                            break;
                        }
                }
            }
            public void InitializeRadiationZone(Single StartRadiation, Single MultiplierRadiation, Single MaximumAmountRadiation, Single SecondUpdate)
            {
                this.MultiplierRadiation = MultiplierRadiation;
                this.MaximumAmountRadiation = MaximumAmountRadiation;
                this.SecondUpdate = SecondUpdate;

                Radiation = gameObject.AddComponent<TriggerRadiation>();
                Radiation.RadiationAmountOverride = StartRadiation;
                Radiation.interestLayers = LayerMask.GetMask("Player (Server)");
                Radiation.enabled = true;

                if (_.TriggerZoneAround.Radiation != null)
                    foreach (BasePlayer player in BasePlayer.activePlayerList.Where(p => p.HasFlag(BaseEntity.Flags.Reserved10)))
                    {
                        player.EnterTrigger(Radiation);
                        _.WarningUI(player, "WARNING_PRE_STOPPED_ALERT_TITLE", 300f);
                    }
            }
            public void UpdateRadiation()
            {
                if (Radiation.RadiationAmountOverride >= MaximumAmountRadiation) return;
                Radiation.RadiationAmountOverride += MultiplierRadiation;
                Invoke(() => UpdateRadiation(), SecondUpdate);
            }
            private void OnDestroy()
            {
                Destroy(gameObject);
            }
            public void Kill()
            {
                CancelInvoke();
                Destroy(gameObject);
            }

            private void UpdateCollider()
            {
                var sphereCollider = gameObject.GetComponent<SphereCollider>();
                {
                    if (sphereCollider == null)
                    {
                        sphereCollider = gameObject.AddComponent<SphereCollider>();
                        sphereCollider.isTrigger = true;
                    }
                    sphereCollider.radius = Radius;
                }
            }
        }

        #endregion

        #region FlyController

        public class FlyController : FacepunchBehaviour
        {
            private Transform MonumentPosition;
            private FlyType flyType;
            private Helicopter ThisHeli;
            private Chinoock ThisCh47;
            private class Helicopter
            {
                public PatrolHelicopterAI HeliAI;
                public Int32 IndexHelicopter;
                public Int32 CircleHelicopter;
                public Int32 MaximumCircle;
                public Single MaxSpeed;
                public Single MoveSpeed;
            }
            private class Chinoock
            {
                public BehaviorChinook BehaviorChinook;
                public Boolean DropCrate;
                public List<Configuration.LootSetting> LootList;
                public CH47HelicopterAIController CH47AI;
            }

            private void Controller()
            {
                switch (flyType)
                {
                    case FlyType.Helicopter:
                        {
                            HelicopterController();
                            break;
                        }
                    case FlyType.Chinoock:
                        {
                            ChinoockController();
                            break;
                        }
                }
            }
            private void Update() => Controller();

            #region Helicopter
            public void Init(FlyType flyType, Transform MonumentPosition, Int32 MaximumCircle = 0, Single MaxSpeed = 0, Single MoveSpeed = 0)
            {
                this.flyType = flyType;
                this.MonumentPosition = MonumentPosition;

                Helicopter HeliMain = new Helicopter();
                HeliMain.HeliAI = _.PatrolHelicopter?.GetComponent<PatrolHelicopterAI>() ?? null;
                ThisHeli = HeliMain;
                ThisHeli.MaximumCircle = MaximumCircle;
                ThisHeli.MaxSpeed = MaxSpeed;
                ThisHeli.MoveSpeed = MoveSpeed;
                ThisHeli.HeliAI.maxSpeed = MaxSpeed;
                ThisHeli.HeliAI.moveSpeed = MoveSpeed;

                Invoke(() => _.StoryUI(StoryPerson.Helicopter, "STORY_ALERT_HELI_START_FLY"), 30f);
            }

            private void HelicopterController()
            {
                if (_.PatrolHelicopter == null || ThisHeli.HeliAI == null) return;

                if (ThisHeli.HeliAI._currentState == PatrolHelicopterAI.aiState.DEATH)
                {
                    if (!_.PatrolHelicopter.HasFlag(BaseEntity.Flags.Reserved8))
                    {
                        _.PatrolHelicopter.SetFlag(BaseEntity.Flags.Reserved8, true);
                        _.SpawnCratesPatrolHelicopter();
                    }
                    return;
                }
                if (Vector3.Distance(MonumentPosition.position, ThisHeli.HeliAI.transform.position) < 700f && !_.PatrolHelicopter.HasFlag(BaseEntity.Flags.Reserved9))
                {
                    _.PatrolHelicopter.SetFlag(BaseEntity.Flags.Reserved9, true);
                    _.StoryUI(StoryPerson.Helicopter, "STORY_ALERT_HELI_PROCESS_FLY");
                }

                if (Vector3.Distance(MonumentPosition.position, ThisHeli.HeliAI.transform.position) < 100f && !_.PatrolHelicopter.HasFlag(BaseEntity.Flags.Reserved10))
                {
                    _.PatrolHelicopter.SetFlag(BaseEntity.Flags.Reserved10, true);
                    _.StoryUI(StoryPerson.Helicopter, "STORY_ALERT_HELI_STOP_FLY");
                }

                if (_.SerializerMain.HelicopterPoints.Count - 1 < ThisHeli.IndexHelicopter)
                {
                    if (ThisHeli.CircleHelicopter > ThisHeli.MaximumCircle)
                    {
                        if (Vector3.Distance(_.PatrolHelicopter.transform.position, _.PositionDespawn) > 50f)
                        {
                            ThisHeli.HeliAI.State_Move_Enter(_.PositionDespawn);

                            if (!_.PatrolHelicopter.HasFlag(BaseEntity.Flags.Reserved8))
                            {
                                _.PatrolHelicopter.SetFlag(BaseEntity.Flags.Reserved8, true);
                                Invoke(() => _.StoryUI(StoryPerson.Helicopter, "STORY_ALERT_HELI_DESPAWN"), 25f);
                            }
                        }
                        else _.PatrolHelicopter.Kill();
                        return;
                    }

                    ThisHeli.CircleHelicopter++;
                    ThisHeli.IndexHelicopter = 0;
                    HelicopterController();
                }
                Vector3 Position = _.SerializerMain.HelicopterPoints[ThisHeli.IndexHelicopter].Position + new Vector3(0f,20f,0f);
                if (Vector3.Distance(MonumentPosition.TransformPoint(Position), ThisHeli.HeliAI.transform.position) >= 10f || Vector3.Distance(MonumentPosition.TransformPoint(Position), ThisHeli.HeliAI.transform.position) < 3f)
                {
                    ThisHeli.HeliAI.State_Move_Enter(MonumentPosition.TransformPoint(Position));
                    return;
                }

                ThisHeli.IndexHelicopter++;
            }

            #endregion

            #region Chinook
            public void Init(FlyType flyType, BehaviorChinook behavior, Transform MonumentPosition, Boolean DropCrate = false, List<Configuration.LootSetting> LootList = null)
            {
                this.flyType = flyType;
                this.MonumentPosition = MonumentPosition;

                Chinoock CH47 = new Chinoock();
                CH47.CH47AI = _.ChinoockHelicopter?.GetComponent<CH47HelicopterAIController>() ?? null;
                ThisCh47 = CH47;
                ThisCh47.BehaviorChinook = behavior;
                ThisCh47.CH47AI.numCrates = 0;
                ThisCh47.LootList = LootList;
                ThisCh47.DropCrate = DropCrate;

                Invoke(() => _.StoryUI(StoryPerson.Chinook, "STORY_ALERT_CH47_PROCESS_FLY"), 30f);
            }

            private void ChinoockController()
            {
                if (ThisCh47.CH47AI == null) return;
                Vector3 Position = MonumentPosition.position + new Vector3(0f, 150f, 0f);
                switch (ThisCh47.BehaviorChinook)
                {
                    case BehaviorChinook.Die:
                        {
                            if (ThisCh47.CH47AI.health > 2700f)
                                ThisCh47.CH47AI.SetMoveTarget(Position);
                            else
                            {
                                ThisCh47.CH47AI.SetAltitudeProtection(false);
                                ThisCh47.CH47AI.SetMoveTarget(MonumentPosition.position);
                            }

                            if (Vector3.Distance(Position, ThisCh47.CH47AI.transform.position) < 250f && ThisCh47.CH47AI.health > 3500f)
                            {
                                ThisCh47.CH47AI.SetHealth(3000f);
                                ExplodeChinoock();
                            }
                            break;
                        }
                    case BehaviorChinook.Leave:
                        {
                            if (ThisCh47.CH47AI.HasFlag(BaseEntity.Flags.Reserved9))
                            {
                                ThisCh47.CH47AI.SetMoveTarget(_.PositionDespawn);
                                if (Vector3.Distance(_.PatrolHelicopter.transform.position, _.PositionDespawn) < 50f)
                                    ThisCh47.CH47AI.Kill();
                                return;
                            }
                            if (Vector3.Distance(Position, ThisCh47.CH47AI.transform.position) > 50f && !ThisCh47.CH47AI.HasFlag(BaseEntity.Flags.Reserved10))
                                ThisCh47.CH47AI.SetMoveTarget(Position);
                            else
                            {
                                if (!ThisCh47.CH47AI.HasFlag(BaseEntity.Flags.Reserved10))
                                {
                                    ThisCh47.CH47AI.SetFlag(BaseEntity.Flags.Reserved10, true);
                                    Invoke(() =>
                                    {
                                        if (ThisCh47.DropCrate)
                                            CreateCrate(ThisCh47.CH47AI.transform.position - new Vector3(3f, 3f, 0f));

                                        Invoke(() =>
                                        {
                                            String Message = ThisCh47.DropCrate ? "STORY_ALERT_CH47_BEHAVIOR_LEAVE_DROP_TRUE" : "STORY_ALERT_CH47_BEHAVIOR_LEAVE_DROP_FALSE";
                                            _.StoryUI(StoryPerson.Chinook, _.GetLang(Message));
                                            ThisCh47.CH47AI.SetFlag(BaseEntity.Flags.Reserved9, true);
                                        }, 10f);
                                    }, 10f);
                                }
                                ThisCh47.CH47AI.SetMoveTarget(MonumentPosition.position);
                                ThisCh47.CH47AI.SetLandingTarget(MonumentPosition.position);
                            }
                            break;
                        }
                }
            }

            private void ExplodeChinoock()
            {
                ThisCh47.CH47AI.GetComponent<Rigidbody>().mass = 10000f;
                TimedExplosive Entity = (TimedExplosive)GameManager.server.CreateEntity("assets/prefabs/tools/c4/explosive.timed.deployed.prefab", ThisCh47.CH47AI.transform.position);
                Entity.Spawn();
                if (Entity != null)
                {
                    Entity.Explode();
                    ThisCh47.CH47AI.GetComponent<Rigidbody>().mass = 500f;
                }

                if (ThisCh47.CH47AI.health > 600f)
                    Invoke(new Action(ExplodeChinoock), _.GetRandom(1, 6));

                if (ThisCh47.CH47AI.health < 2700f && ThisCh47.CH47AI.health > 2200f)
                    _.StoryUI(StoryPerson.Chinook, "STORY_ALERT_CH47_BEHAVIOR_DIE");

                if (ThisCh47.CH47AI.health < 1000f && ThisCh47.DropCrate)
                    Invoke(() => CreateCrate(ThisCh47.CH47AI.transform.position - new Vector3(3f, 3f, 0f)), _.GetRandom(1, 4));
            }

            private void CreateCrate(Vector3 position)
            {
                Quaternion rot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                HackableLockedCrate CrateEnt = GameManager.server.CreateEntity("assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab", position, rot, true) as HackableLockedCrate;
                CrateEnt.enableSaving = false;
                CrateEnt.GetComponent<Rigidbody>().useGravity = true;
                CrateEnt.Spawn();

                for (Int32 i = 0; i < 10; i++)
                {
                    BaseEntity Flare = (BaseEntity)GameManager.server.CreateEntity("assets/prefabs/tools/flareold/flare.deployed.prefab", _.RandomCircle(CrateEnt.transform.position, 2f));
                    Flare.Spawn();
                }

                if (ThisCh47.LootList != null && ThisCh47.LootList.Count != 0)
                {
                    CrateEnt.inventory.itemList.Clear();
                    List<Configuration.LootSetting> RandomItems = ThisCh47.LootList.OrderBy(x => _.Random.Next()).Take(_.GetRandom(1, ThisCh47.LootList.Count)).ToList();

                    for (Int32 i = 0; i < RandomItems.Count; i++)
                    {
                        Configuration.LootSetting Loot = RandomItems[i];

                        Item GiveItem = ItemManager.CreateByName(Loot.Shortname, _.GetRandom(Loot.CountSetting.AmountMin, Loot.CountSetting.AmountMax), Loot.SkinID);
                        if (!String.IsNullOrEmpty(Loot.DisplayName))
                            GiveItem.name = Loot.DisplayName;
                        GiveItem.MoveToContainer(CrateEnt.inventory);
                    }
                }
                CrateEnt.inventory.capacity = CrateEnt.inventory.itemList.Count;
                CrateEnt.inventory.MarkDirty();
                CrateEnt.SendNetworkUpdate();
            }

            #endregion

            private void OnDestroy()
            {
                Destroy(gameObject);
            }
            public void Kill()
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region Lang

        public static StringBuilder sb = new StringBuilder();
        public String GetLang(String LangKey, String userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }

        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CHAT_ALERT_START_SPHERE"] = "Scientists suspected unusual activity on the sphere and moved forward to study it, the project was given the name «SPHERE-0301»",
                ["CHAT_ALERT_STOP_SPHERE"] = "Scientists were destroyed at the facility «SPHERE-0301», the radiation level is out of control! Watch out!",
                ["CHAT_ALERT_STOP_SPHERE_SUCCESS"] = "Event «SPHERE-0301» It's over! Thank you for participating",

                ["CHAT_ALERT_BLOCK_COMMANDS"] = "You can't use the /{0} command in the event area, leave this area to use them again!",

                ["WARNING_ALERT_TITLE"] = "YOU ARE IN THE EVENT AREA",
                ["WARNING_PRE_STARTED_ALERT_TITLE"] = "<size=14><color=#f96b6b>ATTENTION!</color> The event begins, leave the sphere zone!</size>",
                ["WARNING_PRE_STOPPED_ALERT_TITLE"] = "<size=14><color=#f96b6b>ATTENTION!</color> The event is over, leave the sphere zone!</size>",

                ["STORY_ALERT_ATTACKED"] = "To the object under study «SPHERE-0301» an attack has been committed!",
                ["STORY_ALERT_RADIATION_TURN"] = "On the object «SPHERE-0301» there was an explosion, the saboteur blew up the fuel storage!Code «04 - chemical hazard», there are large vapors of non-processed oil at the facility",
                ["STORY_ALERT_CALL_HELI"] = "An object «SPHERE-0301» a combat unit has flown to you «HELI-372», hold on!",
                ["STORY_ALERT_CALL_CH47"] = "An object «SPHERE-0301» we are sending you a combat team of scientists «HEVY-5», expect a transport helicopter",

                ["STORY_ALERT_HELI_START_FLY"] = "We fly to the object «SPHERE-0301»",
                ["STORY_ALERT_HELI_PROCESS_FLY"] = "We fly up to the object",
                ["STORY_ALERT_HELI_STOP_FLY"] = "We are at the object!Let's explore the area!",
                ["STORY_ALERT_HELI_DESPAWN"] = "We have made fire support!We're going back to refuel, hold on «SPHERE-0301»",

                ["STORY_ALERT_CH47_PROCESS_FLY"] = "We fly up to the object",

                ["STORY_ALERT_CH47_BEHAVIOR_LEAVE_DROP_TRUE"] = "We dropped the box with ammunition, we are returning to the base!",
                ["STORY_ALERT_CH47_BEHAVIOR_LEAVE_DROP_FALSE"] = "We helped with combat support, we are returning to the base for refueling!",

                ["STORY_ALERT_CH47_BEHAVIOR_DIE"] = "We have a breakdown!The fuel tank exploded!We are trying to  make an emerg..cy lan..d.. on the o..ect «SPH.R..E-03..1»",
                ["STORY_ALERT_CH47_BEHAVIOR_DIE_TWO"] = "W...e l..ing co..r..!! The electronics a.e out of or..r",

                ["STORY_PERSON_SCIENTIST"] = "HEADQUARTERS",
                ["STORY_PERSON_HELICOPTER"] = "HELI-372",
                ["STORY_PERSON_CHINOOK"] = "HEVY-5",
                ["STORY_PERSON_INFORMATION"] = "INFORMATION",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CHAT_ALERT_START_SPHERE"] = "Ученые заподозрили необычную активность на сфере и выдвинулись его изучать проекту дали название «SPHERE-0301»",
                ["CHAT_ALERT_STOP_SPHERE"] = "Ученые были уничтожены на объекте «SPHERE-0301», уровень радиации вышел из под контроля! Берегитесь!",
                ["CHAT_ALERT_STOP_SPHERE_SUCCESS"] = "Мероприятие «SPHERE-0301» окончено! Спасибо за участие",

                ["CHAT_ALERT_BLOCK_COMMANDS"] = "Вы не можете использовать команду /{0} в зоне действия мероприятия, покиньте данную зону чтобы использовать их снова!",

                ["WARNING_ALERT_TITLE"] = "ВЫ НАХОДИТЕСЬ В ЗОНЕ МЕРОПРИЯТИЯ",
                ["WARNING_PRE_STARTED_ALERT_TITLE"] = "<size=14><color=#f96b6b>ВНИМАНИЕ!</color> Начинается мероприятие, покиньте зону сферы!</size>",
                ["WARNING_PRE_STOPPED_ALERT_TITLE"] = "<size=14><color=#f96b6b>ВНИМАНИЕ!</color> Мероприятие окончено, покиньте зону сферы!</size>",

                ["STORY_ALERT_ATTACKED"] = "На изучаемый объект «SPHERE-0301» было совершено нападение!",
                ["STORY_ALERT_RADIATION_TURN"] = "На объекте «SPHERE-0301» произошел взрыв, диверсант подорвал топливное хранилище!Код «04 - химическая опасность», на объекте большие испарения не переработанной нефти",
                ["STORY_ALERT_CALL_HELI"] = "Объект «SPHERE-0301» к вам вылетела боевая еденица «HELI-372», держитесь!",
                ["STORY_ALERT_CALL_CH47"] = "Объект «SPHERE-0301» направляем вам боевой отряд ученых «HEVY-5», ожидайте транспортный вертолет",

                ["STORY_ALERT_HELI_START_FLY"] = "Вылетаем на объект «SPHERE-0301»",
                ["STORY_ALERT_HELI_PROCESS_FLY"] = "Подлетаем к объекту",
                ["STORY_ALERT_HELI_STOP_FLY"] = "Находимся у объекта!Исследуем местность!",
                ["STORY_ALERT_HELI_DESPAWN"] = "Произвели огневую поддержку!Возвращаемся на дозаправку, дрежитесь «SPHERE-0301»",

                ["STORY_ALERT_CH47_PROCESS_FLY"] = "Подлетаем к объекту",

                ["STORY_ALERT_CH47_BEHAVIOR_LEAVE_DROP_TRUE"] = "Сбросили ящик с аммуницией, возвращаемся на базу!",
                ["STORY_ALERT_CH47_BEHAVIOR_LEAVE_DROP_FALSE"] = "Помогли боевой поддержкой, возвращаемся на базу на дозаправку!",

                ["STORY_ALERT_CH47_BEHAVIOR_DIE"] = "У нас поломка!Взорвался топливный бак!Пытаемся совершить экстре..ую по..д.. на о..ект «SPH.R..E-03..1»",
                ["STORY_ALERT_CH47_BEHAVIOR_DIE_TWO"] = "М.. т..ряем уп..р..авл..ние!! Электроника в...шла из ст..р...я",

                ["STORY_PERSON_SCIENTIST"] = "ШТАБ",
                ["STORY_PERSON_HELICOPTER"] = "HELI-372",
                ["STORY_PERSON_CHINOOK"] = "HEVY-5",
                ["STORY_PERSON_INFORMATION"] = "ИНФОРМАЦИЯ",

            }, this, "ru");
        }
        #endregion
    }
}
