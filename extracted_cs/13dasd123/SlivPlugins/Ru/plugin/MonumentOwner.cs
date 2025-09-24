using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Plugins.MonumentOwnerExtensionMethods;
using Oxide.Game.Rust.Cui;
using System.Collections;
using Oxide.Core.Libraries;
using CompanionServer.Handlers;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("MonumentOwner", "jtedal", "1.4.1")]
    internal class MonumentOwner : RustPlugin
    {
        private const bool En = false;

        #region Config

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
            if (_config.PluginVersion < Version)
            {
                UpdateConfigValues();
            }
        }

        private void UpdateConfigValues()
        {
            Puts("Config update detected! Updating config values...");
            if (_config.PluginVersion < new VersionNumber(1, 3, 4))
            {
                _config.LootPlayerBackpack = true;
                _config.BossMonster = new BossMonsterConfig()
                {
                    StartKillingBoss = true,
                    Damage = 300.0f,
                    ScaleDamageBoss = 1.0f,
                    Cooldown = 1200
                };
                _config.ScaleDamageNPC = 1.0f;
            }
            if (_config.PluginVersion < new VersionNumber(1, 3, 7))
            {
                _config.Notifications.Notification_NoDamageTarget = true;
            }
            _config.PluginVersion = Version;
            Puts("Config update completed!");
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        public class BossMonsterConfig
        {
            [JsonProperty(En ? "The player will immediately receive the owner status if he deals N amount of damage to the boss:" : "Игрок немедленно получит статус владельца, если нанесет N кол-во урона боссу:")] public bool StartKillingBoss { get; set; }
            [JsonProperty(En ? "Amount damage:" : "Кол-во урона:")] public float Damage { get; set; }
            [JsonProperty(En ? "Damage ratios (per Boss) to calculate becoming owner of the monument" : "Коэффициенты ущерба (по Боссу) для расчета, чтобы стать владельцем монумента")] public float ScaleDamageBoss { get; set; }
            [JsonProperty(En ? "The time during which the player will not be able to deal damage to the boss after player has killed him:" : "Время, в течении которого игрок не сможет нанести урон босу, после того, как его убил:")] public float Cooldown { get; set; }
        }

        public class RulesForNoOwnerConfig
        {
            [JsonProperty(En ? "Can a non owner enter or stay in the zone:" : "Могут ли заходить или оставаться в зоне:")] public bool CanEnterNoOwner { get; set; }
            [JsonProperty(En ? "Can deal damage to NPCs if not an owner:" : "Могут ли наносить урон по NPC:")] public bool DamageNpc { get; set; }
            [JsonProperty(En ? "Can deal damage to tanks if not an owner:" : "Могут ли наносить урон по танку:")] public bool DamageTank { get; set; }
            [JsonProperty(En ? "Can deal damage to barrel if not an owner:" : "Могут ли наносить урон бочке:")] public bool DamageBarrel { get; set; }
            [JsonProperty(En ? "Can crates be looted if not an owner:" : "Могут ли открывать крейты:")] public bool LootCrate { get; set; }
            [JsonProperty(En ? "Can collectable items (diesel, cards) be pickup if not an owner:" : "Могут ли подбирать Collectable предметы (дизельки, карточки и т.д.):")] public bool PickupCollectableItems { get; set; }
            [JsonProperty(En ? "Can crates be hacked if not an owner:" : "Могут ли взламывать крейты:")] public bool HackCrate { get; set; }
            [JsonProperty(En ? "Can backpacks be looted if not an owner:" : "Могут ли открывать рюкзаки:")] public bool LootBackpacks { get; set; }
            [JsonProperty(En ? "Can recycler be used if not an owner:" : "Могут ли использовать переработчик:")] public bool UseRecycler { get; set; }
            [JsonProperty(En ? "Can NPCs target players if not an owner:" : "Могут ли NPC атаковать не владельцев монумента:")] public bool TargetNpc { get; set; }
            [JsonProperty(En ? "Can tanks target players if not an owner:" : "Может ли Bradley атаковать не владельцев монумента:")] public bool TargetBradley { get; set; }
        }

        public class RulesBecomeOwnerConfig
        {
            [JsonProperty(En ? "If True the player must meet ALL enabled conditions to receive Owner of the Monument status; If False the player must meet only one of the enabled conditions to receive Owner of the Monument status:" : "Укажите true, если игрок должен выполнить все включенные условия для получения статуса «Владелец монумента». Укажите false, если игрок должен выполнить одно из включенных условий для полкчения статуса «Владелец монумента».")] public bool AllTerms { get; set; }
            [JsonProperty(En ? "The player must stay in the monument zone for a this amount of time [seconds] To disable this parameter set the value to [-1]:" : "Игрок должен находится определенное кол-во времени [сек.] в зоне монумента (Чтобы отключить этот параметр, укажите значение [-1] ):")] public int TimeToGetOwner { get; set; }
            [JsonProperty(En ? "The player must deal this amount of damage [total damage] To disable this parameter set the value to [-1]:" : "Игрок должен нанести определенное кол-во урона (Чтобы отключить этот параметр, укажите значение [-1] ):")] public float Damage { get; set; }
            [JsonProperty(En ? "The player must open the crate:" : "Игрок должен открыть крейт:")] public bool OpenCrate { get; set; }
            [JsonProperty(En ? "The player must open a keycard security door:" : "Игрок должен открыть дверь ключ-картой:")] public bool OpenDoor { get; set; }
            [JsonProperty(En ? "The player must start a quarry:" : "Игрок должен запустить карьер (только для маленьких карьеров на карте):")] public bool StartQuarry { get; set; }
            [JsonProperty(En ? "The player must start Giant Excavator:" : "Игрок должен запустить экскаватор (только для Экскаватора):")] public bool StartExcavator { get; set; }
            [JsonProperty(En ? "The player will immediately receive the status of the owner, if he starts hacking the crate:" : "Игрок немедленно получит статус владельца, если начнет взламывать крейт:")] public bool StartHackCrate { get; set; }
        }

        public class TimersOutsideZone
        {
            [JsonProperty(En ? "The amount of time which the owner can leave the monument area and retain their title in seconds:" : "Время, в течение которого владелец монумента может покинуть зону монумента и сохранить титул [сек.]:")] public int TimeExitOwner { get; set; }
            [JsonProperty(En ? "Warning time for owner of status expiration in seconds:" : "Время предупреждения до истечения срока действия статуса владельца [сек.]:")] public int AlertTimeOutsideZone { get; set; }
        }

        public class TimersInsideZone
        {
            [JsonProperty(En ? "Timed duration which the player will retaion ownership of the monument in seconds:" : "Время, в течении которого игрок будет владельцем монумента [сек.]:")] public int TimeOwner { get; set; }
            [JsonProperty(En ? "Warning time for owner of status expiration in seconds:" : "Время предупреждения до истечения статуса владельца события [сек.]:")] public int AlertTimeInsideZone { get; set; }
        }

        public class MonumentConfig
        {
            [JsonProperty(En ? "Temporary rules for those inside the monument area." : "Временные правила для тех, кто внутри зоны монумента.")] public TimersInsideZone TimersInsideZone { get; set; }
            [JsonProperty(En ? "Temporary rules for those who left the monument area." : "Временные правила для тех, кто вышел из зоны монумента.")] public TimersOutsideZone TimersOutsideZone { get; set; }
            [JsonProperty(En ? "Cooldown timer after becoming monument owner during which the player cannot claim the title again in seconds." : "Время, в течении которого игрок не сможет стать владельцем монумента, после того как он был его владельцем [сек.]")] public double CooldownOwner { get; set; }
            [JsonProperty(En ? "Rules for obtaining monument owner status." : "Правила для получения статуса «Владелец монумента».")] public RulesBecomeOwnerConfig RulesBecomeOwner { get; set; }
            [JsonProperty(En ? "Monument rules if not monument owner." : "Правила для не владельцев монумента.")] public RulesForNoOwnerConfig RulesForNoOwner { get; set; }

        }

        public class NotificationsConfig
        {
            [JsonProperty(En ? "You are now the monument owner for {0} seconds!" : "Вы стали владельцем монумента на {0} сек!")] public bool Notification_YouOwnerMonument { get; set; }
            [JsonProperty(En ? "You have lost ownership of the monument!" : "Вы утратили статус владельца монумента!")] public bool Notification_YouNonOwnerMonument { get; set; }
            [JsonProperty(En ? "You have {0} seconds left to return to the monument area and retain monument owner status." : "У вас осталось {0} сек. чтобы вернуться в зону монумента и не потерять статус владельца")] public bool Notification_AlertTimeOutsideZone2 { get; set; }
            [JsonProperty(En ? "You have {0} seconds left before losing the monument owner status and being kicked from the area." : "У вас осталось {0} сек. до потери статуса владельца монумента. По окончанию, вы будете выгнаны из зоны.")] public bool Notification_AlertTimeInsideZone { get; set; }
            [JsonProperty(En ? "You have entered the monument area. Fulfill all of these conditions to become the owner:" : "Вы вошли в зону монумента. Выполните эти условия, чтобы стать владельцем:")] public bool Notification_EnteringZone1 { get; set; }
            [JsonProperty(En ? "You have entered the monument area. Fulfill one of these conditions to become the owner:" : "Вы вошли в зону монумента. Выполните одно из этих условия, чтобы стать владельцем:")] public bool Notification_EnteringZone2 { get; set; }
            [JsonProperty(En ? "   - You must be in the monument area for {0} seconds." : "   - Вы должны находится в зоне монумента {0} сек.")] public bool Notification_RuleTimeToGetZone { get; set; }
            [JsonProperty(En ? "   - You must deal {0} damage to NPCs or Bradley." : "   - Вы должны нанести {0} урона по NPC или Bradley.")] public bool Notification_RuleDamage { get; set; }
            [JsonProperty(En ? "   - You must open at least one crate." : "   - Вы должны открыть хотя бы один ящик.")] public bool Notification_RuleOpenCrate { get; set; }
            [JsonProperty(En ? "   - You must open at least one keycard security door." : "   - Вы должны открыть хотя бы одну дверь ключ-картой.")] public bool Notification_RuleOpenDoor { get; set; }
            [JsonProperty(En ? "   - You must start a quarry." : "   - Вы должны запустить карьер.")] public bool Notification_RuleStartQuarry { get; set; }
            [JsonProperty(En ? "   - You must run Giant Excavator." : "   - Вы должны запустить экскаватор.")] public bool Notification_RuleStartExcavator { get; set; }
            [JsonProperty(En ? "You have left the monument bounds, in order to retain owner status, return to the monument area within {0} seconds." : "Вы вышли из зоны монумента. Чтобы не потерять статус владельца вам необходимо вернуться в зону монумента в течении {0} сек.")] public bool Notification_AlertTimeOutsideZone1 { get; set; }
            [JsonProperty(En ? "You cannot hack the crate." : "Вы не можете начать взламывать ящик.")] public bool Notification_NoHackCrateEvent { get; set; }
            [JsonProperty(En ? "You cannot damage NPCs." : "Вы не можете наносить урон по NPC.")] public bool Notification_NoDamageScientist { get; set; }
            [JsonProperty(En ? "You cannot damage Bradley." : "Вы не можете наносить урон по Bradley.")] public bool Notification_NoDamageTank { get; set; }
            [JsonProperty(En ? "You cannot damage barrel." : "Вы не можете наносить урон по бочке.")] public bool Notification_NoDamageBarrel { get; set; }
            [JsonProperty(En ? "You cannot damage target." : "Вы не можете наносить урон по цели.")] public bool Notification_NoDamageTarget { get; set; }
            [JsonProperty(En ? "You cannot loot this." : "Вы не можете это залутать.")] public bool Notification_CanNotLoot { get; set; }
            [JsonProperty(En ? "You cannot open this." : "Вы не можете это открыть.")] public bool Notification_CanNotOpen { get; set; }
            [JsonProperty(En ? "You cannot enter the monument zone." : "Вы не можете зайти в зону монумента.")] public bool Notification_NoEnterMonument { get; set; }
            [JsonProperty(En ? "You cannot change the type of resource being mined." : "Вы не можете изменить тип добываемого ресурса.")] public bool Notification_CanNotResourceSet { get; set; }
            [JsonProperty(En ? "Your cooldown to become a monument owner is {0} seconds." : "Ваш кулдаун на получение статуса владельца монумента на этом РТ составляет: {0} сек.")] public bool Notification_EnteringZone3 { get; set; }
            [JsonProperty(En ? "You are already the owner of another monument. You can only own one monument at a time." : "Вы уже являетесь владельцем другого монумента. Одновременно можно быть владельцем только 1 монумента.")] public bool Notification_NoEnterAnotherOwner { get; set; }

        }

        public class PluginConfig
        {
            [JsonProperty(En ? "Zone dimmer (0 - remove the dimmer)" : "Затемнение зоны (0 - убрать затемнение)")] public int Darkening { get; set; }
            [JsonProperty(En ? "GUI text color for the Monument Owner" : "Цвет текста гуи для владельца монумента")] public string ColorTextForOwnerPanel { get; set; }
            [JsonProperty(En ? "GUI text color if not monument owner" : "Цвет текста гуи для не владельца монумента")] public string ColorTextForNoOwnerPanel { get; set; }
            [JsonProperty(En ? "Are administrator able to participate? [true/false]" : "Будут ли правила зоны распространяться на администратора? (Сможет ли он участвовать) [true/false]")] public bool AdminEntry { get; set; }
            [JsonProperty(En ? "Damage ratios (per NPC) to calculate becoming owner of the monument (Not applicable for \"BossMonster\")" : "Коэффициенты ущерба (по NPC) для расчета, чтобы стать владельцем монумента (Не применяется для \"BossMonster\")")] public float ScaleDamageNPC { get; set; }
            [JsonProperty(En ? "Damage ratios (by Bradley) to calculate becoming owner of the monument" : "Коэффициенты ущерба (по Bradley) для расчета, чтобы стать владельцем монумента")] public float ScaleDamageBradley { get; set; }
            [JsonProperty(En ? "Prevent a player from entering an monument area if they are the owner of another monument? [true/false]" : "Запрещать игроку входить внутрь зоны монумента, если он является владельцем другого монумента? [true/false]")] public bool NoEnterAnotherOwner { get; set; }
            [JsonProperty(En ? "Prevent a player from entering an monument area if he has a cooldown?" : "Запрещать игроку входить внутрь зоны монумента, если у него есть кулдаун? [true/false]")] public bool NoEnterPlayerWithCooldown { get; set; }
            [JsonProperty(En ? "Prevent a player from dealing damage to a target if he is outside the zone and the target is inside the zone? The parameter will be taken into account only when the monument has a neutral status." : "Запрещать игроку наносить урон по цели, если он находится за пределами зоны, а цель внутри зоны? Параметр будет учитываться только тогда, когда монумент имеет нейтральный статус. [true/false]")] public bool NoDamageOutsideZone { get; set; }
            [JsonProperty(En ? "Should the player be allowed to become the owner of another monument if he has fulfilled all the rules? (In this case, the owner's status on the previous monument will be lost for him or his teammate)" : "Следует ли разрешить игроку стать владельцем другого монумента, если он выполнил все правила? (При этом статус владельца на предыдущем монументе будет утерян для него или тимейта)")] public bool CanPlayerChangeMonument { get; set; }
            [JsonProperty(En ? "Can the player loot his backpack if he is in the locked monument zone" : "Может ли игрок лутать свой рюкзак, если он находится в зоне занятого монумента")] public bool LootPlayerBackpack { get; set; }
            [JsonProperty(En ? "Chat notifications" : "Уведомления в чате")] public NotificationsConfig Notifications { get; set; }
            [JsonProperty(En ? "Settings for plugin \"BossMonster\"" : "Настройки для плагина \"BossMonster\"")] public BossMonsterConfig BossMonster { get; set; }
            [JsonProperty(En ? "Configuration Version" : "Версия конфигурации")] public VersionNumber PluginVersion { get; set; }
            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    Darkening = 2,
                    ColorTextForOwnerPanel = "#8cc83c",
                    ColorTextForNoOwnerPanel = "#cd4632",
                    AdminEntry = false,
                    ScaleDamageNPC = 1.0f,
                    ScaleDamageBradley = 1.0f,
                    NoEnterAnotherOwner = false,
                    CanPlayerChangeMonument = true,
                    NoDamageOutsideZone = true,
                    NoEnterPlayerWithCooldown = true,
                    LootPlayerBackpack = true,

                    Notifications = new NotificationsConfig()
                    {
                        Notification_YouOwnerMonument = true,
                        Notification_YouNonOwnerMonument = true,
                        Notification_AlertTimeOutsideZone2 = true,
                        Notification_AlertTimeInsideZone = true,
                        Notification_EnteringZone1 = true,
                        Notification_EnteringZone2 = true,
                        Notification_RuleTimeToGetZone = true,
                        Notification_RuleDamage = true,
                        Notification_RuleOpenCrate = true,
                        Notification_RuleOpenDoor = true,
                        Notification_AlertTimeOutsideZone1 = true,
                        Notification_NoHackCrateEvent = true,
                        Notification_NoDamageScientist = true,
                        Notification_NoDamageTank = true,
                        Notification_NoDamageBarrel = true,
                        Notification_CanNotLoot = true,
                        Notification_NoEnterMonument = true,
                        Notification_EnteringZone3 = true,
                        Notification_NoEnterAnotherOwner = true,
                        Notification_RuleStartQuarry = true,
                        Notification_RuleStartExcavator = true,
                        Notification_CanNotResourceSet = true,
                        Notification_CanNotOpen = true,
                        Notification_NoDamageTarget = true
                    },
                    BossMonster = new BossMonsterConfig()
                    {
                        StartKillingBoss = true,
                        Damage = 300.0f,
                        ScaleDamageBoss = 1.0f
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
                ["YouOwnerMonument"] = "You have <color=#738d43>become</color> the owner of this monument for <color=#55aaff>{0} sec.</color>!",
                ["OwnerMonumentForAllPlayers"] = "Player <color=#738d43>{0}</color> became the owner of the <color=#738d43>{1} [{2}]</color> for <color=#55aaff>{3} sec.</color>!",
                ["NoOwnerMonumentForAllPlayers"] = "Player <color=#738d43>{0}</color> stopped being the owner of the <color=#738d43>{1} [{2}]</color>. Monument is <color=#55aaff>not busy</color>!",
                ["YouOwnerMonumentGUI"] = "You are the owner of monument.  Any actions are allowed",
                ["YouNonOwnerMonument"] = "You have <color=#ce3f27>lost</color> the status of monument owner!",
                ["YouNonOwnerMonumentGUI"] = "You are not the owner of monument. Some actions are limited",
                ["AlertTimeOutsideZone2"] = "You have <color=#55aaff>{0} sec.</color> left to return to the monument zone and not lose owner status",
                ["AlertTimeInsideZone"] = "You have <color=#55aaff>{0} sec.</color> left before losing the status of monument owner. After that, you will be teleported from the zone.",
                ["AlertTimeOutsideZone1"] = "You have <color=#ce3f27>left</color> the monument zone. To avoid losing the owner status, you need to return to the monument zone within <color=#55aaff>{0} sec.</color>.",
                ["EnteringZone1"] = "You have entered the <color=#738d43>monument</color> zone. Fulfill these <color=#55aaff>conditions</color> to become the owner:",
                ["EnteringZone2"] = "You have entered the <color=#738d43>monument</color> zone. Fulfill one of these <color=#55aaff>conditions</color> to become the owner:",
                ["EnteringZone3"] = "Your cooldown for obtaining the monument owner status in this event is: <color=#55aaff>{0} sec.</color>",
                ["RuleTimeToGetZone"] = "   - You <color=#738d43>must</color> stay in the monument zone for <color=#55aaff>{0} sec.</color>.",
                ["RuleDamage"] = "   - You <color=#738d43>must</color> deal <color=#55aaff>{0} damage</color> to NPCs or Bradley.",
                ["RuleOpenCrate"] = "   - You <color=#738d43>must</color> open at least <color=#55aaff>one crate</color>.",
                ["NoHackCrateMonument"] = "You <color=#ce3f27>cannot</color> start hacking the crate.",
                ["NoDamageScientist"] = "You <color=#ce3f27>cannot</color> deal damage to <color=#55aaff>NPCs</color>.",
                ["NoDamageTank"] = "You <color=#ce3f27>cannot</color> deal damage to <color=#55aaff>Bradley</color>.",
                ["NoDamageBarrel"] = "You <color=#ce3f27>cannot</color> deal damage to <color=#55aaff>barrel</color>.",
                ["NoDamageTarget"] = "You <color=#ce3f27>cannot</color> deal damage to <color=#55aaff>target</color> outside zone.",
                ["CanNotLoot"] = "You <color=#ce3f27>cannot</color> loot this.",
                ["CanNotOpen"] = "You <color=#ce3f27>cannot</color> open this.",
                ["NoEnterMonument"] = "You <color=#ce3f27>cannot</color> enter the monument zone.",
                ["CanNotResourceSet"] = "You <color=#ce3f27>cannot</color> change the type of harvested resource.",
                ["RuleOpenDoor"] = "   - You <color=#738d43>must</color> open at least one door with a <color=#55aaff>keycard</color>.",
                ["RuleStartQuarry"] = "   - You <color=#738d43>must</color> start the <color=#55aaff>quarry</color>.",
                ["RuleStartExcavator"] = "   - You <color=#738d43>must</color> start the <color=#55aaff>excavator</color>.",
                ["YouDontHaveCooldown"] = "You have <color=#ce3f27>no</color> cooldown on monuments.",
                ["YouHaveCooldown"] = "Your <color=#ce3f27>cooldown</color>: {0}",
                ["NoEnterAnotherOwner"] = "You or your teammate are already the <color=#738d43>owner</color> of another monument. You can only be the owner of 1 monument at a time."
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["YouOwnerMonument"] = "Вы <color=#738d43>стали</color> владельцем монумента на <color=#55aaff>{0} сек.</color>!",
                ["OwnerMonumentForAllPlayers"] = "Игрок <color=#738d43>{0}</color> стал владельцем <color=#738d43>{1} [{2}]</color> на <color=#55aaff>{3} sec.</color>!",
                ["NoOwnerMonumentForAllPlayers"] = "Игрок <color=#738d43>{0}</color> перестал быть владельцем <color=#738d43>{1} [{2}]</color>. Монумент <color=#55aaff>свободен</color>!",
                ["YouOwnerMonumentGUI"] = "Вы являетесь владельцем РТ. Любые действия разрешены",
                ["YouNonOwnerMonument"] = "Вы <color=#ce3f27>утратили</color> статус владельца монумента!",
                ["YouNonOwnerMonumentGUI"] = "Вы не являетесь владельцем РТ. Некоторые действия ограничены",
                ["AlertTimeOutsideZone2"] = "У вас осталось <color=#55aaff>{0} сек.</color> чтобы вернуться в зону монумента и не потерять статус владельца",
                ["AlertTimeInsideZone"] = "У вас осталось <color=#55aaff>{0} сек.</color> до потери статуса владельца монумента. По окончанию, вы будете выгнаны из зоны.",
                ["AlertTimeOutsideZone1"] = "Вы <color=#ce3f27>вышли</color> из зоны монумента. Чтобы не потерять статус владельца вам необходимо вернуться в зону монумента в течении <color=#55aaff>{0} сек.</color>",
                ["EnteringZone1"] = "Вы вошли в <color=#738d43>зону</color> монумента. Выполните эти <color=#55aaff>условия</color>, чтобы стать владельцем:",
                ["EnteringZone2"] = "Вы вошли в <color=#738d43>зону</color> монумента. Выполните одно из этих <color=#55aaff>условия</color>, чтобы стать владельцем:",
                ["EnteringZone3"] = "Ваш кулдаун на получение статуса владельца монумента на этом РТ составляет: <color=#55aaff>{0} сек.</color>",
                ["RuleTimeToGetZone"] = "   - Вы <color=#738d43>должны</color> находится в зоне монумента <color=#55aaff>{0} сек.</color>.",
                ["RuleDamage"] = "   - Вы <color=#738d43>должны</color> нанести <color=#55aaff>{0} урона</color> по NPC или Bradley.",
                ["RuleOpenCrate"] = "   - Вы <color=#738d43>должны</color> открыть хотя бы <color=#55aaff>один ящик</color>.",
                ["NoHackCrateMonument"] = "Вы <color=#ce3f27>не можете</color> начать взламывать ящик.",
                ["NoDamageScientist"] = "Вы <color=#ce3f27>не можете</color> наносить урон по <color=#55aaff>NPC</color>.",
                ["NoDamageTank"] = "Вы <color=#ce3f27>не можете</color> наносить урон по <color=#55aaff>Bradley</color>.",
                ["NoDamageBarrel"] = "Вы <color=#ce3f27>не можете</color> наносить урон по <color=#55aaff>бочке</color>.",
                ["NoDamageTarget"] = "Вы <color=#ce3f27>не можете</color> наносить урон по <color=#55aaff>цели</color> вне зоны.",
                ["CanNotLoot"] = "Вы <color=#ce3f27>не можете</color> это залутать.",
                ["CanNotOpen"] = "Вы <color=#ce3f27>не можете</color> это открыть.",
                ["NoEnterMonument"] = "Вы <color=#ce3f27>не можете</color>зайти в зону монумента.",
                ["CanNotResourceSet"] = "Вы <color=#ce3f27>не можете</color> изменить тип добываемого ресурса.",
                ["RuleOpenDoor"] = "   - Вы <color=#738d43>должны</color> открыть хотя бы одну дверь <color=#55aaff>ключ-картой</color>.",
                ["RuleStartQuarry"] = "   - Вы <color=#738d43>должны</color> запустить <color=#55aaff>карьер</color>.",
                ["RuleStartExcavator"] = "   - Вы <color=#738d43>должны</color> запустить <color=#55aaff>экскаватор</color>.",
                ["YouDontHaveCooldown"] = "У вас <color=#ce3f27>нет</color> кулдауна на монументах.",
                ["YouHaveCooldown"] = "Ваши <color=#ce3f27>кулдауны</color>: {0}",
                ["NoEnterAnotherOwner"] = "Вы или ваш тиммейт уже <color=#738d43>являетесь</color> владельцем другого монумента. Одновременно можно быть владельцем только 1 монумента."

            }, this, "ru");
        }

        private string GetMessage(string langKey, string userID) => lang.GetMessage(langKey, _here, userID);

        private string GetMessage(string langKey, string userID, params object[] args) => (args.Length == 0) ? GetMessage(langKey, userID) : string.Format(GetMessage(langKey, userID), args);

        #endregion Lang

        #region GUI

        internal string ConvertHexToRGBA(string hexColor)
        {
            Color color = new Color();

            if (ColorUtility.TryParseHtmlString(hexColor, out color))
            {
                float red = color.r;
                float green = color.g;
                float blue = color.b;
                float alpha = 1f;

                string rgbaString = string.Format("{0} {1} {2} {3}", red, green, blue, alpha);

                return rgbaString;
            }
            return null;
        }

        void GUI(BasePlayer player, string message)
        {
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = false,
            }, "Under", "MainInvisPanel");


            if (message == "Owner")
            {
                container.Add(new CuiElement
                {
                    Name = "TopBarBackground",
                    Parent = "MainInvisPanel",
                    Components =
                    {
                        new CuiImageComponent { Color = "0 0 0 0.8" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.94", AnchorMax = "0.5 0.94", OffsetMin = "-200 -10", OffsetMax = "200 10" }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "TopBarText",
                    Parent = "TopBarBackground",
                    Components =
                    {
                        new CuiTextComponent() { Color = ConvertHexToRGBA(_config.ColorTextForOwnerPanel), Text = GetMessage("YouOwnerMonumentGUI", player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 15 },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });
            }
            if (message == "NoOwner")
            {
                container.Add(new CuiElement
                {
                    Name = "TopBarBackground",
                    Parent = "MainInvisPanel",
                    Components =
                    {
                        new CuiImageComponent { Color = "0 0 0 0.8" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.94", AnchorMax = "0.5 0.94", OffsetMin = "-240 -10", OffsetMax = "240 10" }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "TopBarText",
                    Parent = "TopBarBackground",
                    Components =
                    {
                        new CuiTextComponent() { Color = ConvertHexToRGBA(_config.ColorTextForNoOwnerPanel), Text = GetMessage("YouNonOwnerMonumentGUI", player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FontSize = 15 },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });
            }

            CuiHelper.AddUi(player, container);
        }

        internal void DestroyGUI(BasePlayer player = null, HashSet<BasePlayer> InsidePlayers = null)
        {
            if (player != null) CuiHelper.DestroyUi(player, "MainInvisPanel");
            if (InsidePlayers != null)
            {
                foreach (BasePlayer locPlayer in InsidePlayers)
                {
                    CuiHelper.DestroyUi(locPlayer, "MainInvisPanel");
                }
            }
        }

        internal void CreateGUI(ulong Owner, BasePlayer player = null, HashSet<BasePlayer> InsidePlayers = null)
        {
            if (player != null)
            {
                DestroyGUI(player: player);
                if (IsTeam(player, Owner)) GUI(player, "Owner");
                if (!IsTeam(player, Owner)) GUI(player, "NoOwner");
            }
            if (InsidePlayers != null)
            {
                DestroyGUI(InsidePlayers: InsidePlayers);
                foreach (BasePlayer locPlayer in InsidePlayers)
                {
                    if (IsTeam(locPlayer, Owner)) GUI(locPlayer, "Owner");
                    if (!IsTeam(locPlayer, Owner)) GUI(locPlayer, "NoOwner");
                }
            }
        }

        #endregion GUI

        #region Data System

        private void WriteData()
        {
            DetermineRemainCooldown();
            Interface.Oxide.DataFileSystem.WriteObject("MM_Data/MonumentOwner/CooldownPlayers", _playersData);
        }

        private void LoadFiles()
        {
            _playersData = Interface.Oxide.DataFileSystem.ReadObject<HashSet<PlayerData>>("MM_Data/MonumentOwner/CooldownPlayers");
            foreach (PlayerData p in _playersData)
            {
                if (p.lastTimeForMonuments == null)
                {
                    _oldPlayersData = new HashSet<OldPlayerData>();
                    _oldPlayersData = Interface.Oxide.DataFileSystem.ReadObject<HashSet<OldPlayerData>>("MM_Data/MonumentOwner/CooldownPlayers");
                    _playersData.Clear();
                    break;
                }
            }
            ConvertValuesForDateTime();
        }

        #endregion Data System

        #region Oxide Hooks

        private void OnServerInitialized()
        {
            if (BossMonster != null) bosses = (HashSet<ScientistNPC>)BossMonster.Call("GetAllBosses");
            if (bosses == null) bosses = new HashSet<ScientistNPC>();

            LoadMonumentSettings();
            LoadEventSettings();
            LoadCustomMonumentSettings();

            UpdateAllDataFiles();

            _playersData = new HashSet<PlayerData>();
            
            ListAllMonumentsOnTheMap();
            ListAllCustomMonumentsOnTheMap();

            foreach (CargoShip cargo in BaseNetworkable.serverEntities.OfType<CargoShip>()) OnEntitySpawned(cargo);

            LoadFiles();

            _corountine = ServerMgr.Instance.StartCoroutine(TimerCanTimeOwner());

            Subscribe("OnEntitySpawned");
            Subscribe("OnEntityKill");
        }

        void OnEntityKill(CargoShip cargo)
        {
            ControllerMonument zone = _zones.FirstOrDefault(x => x.Name == "Cargo" && cargo.net.ID.Value.ToString() == x.ID);
            if (zone != null)
            {
                foreach (BasePlayer playerWithGUI in zone.InsidePlayers)
                {
                    DestroyGUI(player: playerWithGUI);
                }
                if (zone.Owner != 0) zone.DeleteOwnerInZone();
                _zones.Remove(zone);
            }
        }

        private static MonumentOwner _here;

        private void Init()
        {
            _here = this;
            Unsubscribe("OnEntitySpawned");
            Unsubscribe("OnEntityKill");
        }

        private void Unload()
        {
            WriteData();

            if (_corountine != null) ServerMgr.Instance.StopCoroutine(_corountine);

            foreach (ulong playerUserID in AllInsidePlayers.Keys)
            {
                BasePlayer playerWithGUI = BasePlayer.FindByID(playerUserID);
                DestroyGUI(player: playerWithGUI);
            }
            foreach (ControllerMonument i in _zones)
            {
                UnityEngine.Object.Destroy(i.gameObject);
            }
            _here = null;
        }

        void OnEntitySpawned(CargoShip cargo)
        {
            if (cargo == null || cargo.net == null)
            {
                PrintError("Cargo was tagged with an incorrect NetID. The creation of a zone for it has been discontinued. Contact the plugin developer [Monument Owner] with this problem");
                return;
            }
            foreach (MonumentInfo harborMonument in AllHarborsOnMap)
            {
                if (IsObjectInSphere(harborMonument.transform.position, 250.0f, cargo.transform.position)) return;
            }
            ControllerMonument zoneObject = new GameObject().AddComponent<ControllerMonument>();
            ListOfMonuments settings = _monumentSettings["Cargo"];
            if (settings != null && settings.Enabled) CreateZoneForCargo(settings, cargo, zoneObject);
        }

        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (AllInsidePlayers.ContainsKey(player.userID))
            {
                string id = AllInsidePlayers[player.userID];
                if (id != null)
                {
                    ControllerMonument zone = _zones.FirstOrDefault(x => x.ID == id);
                    if (zone != null)
                    {
                        zone.OnExitPlayer(player);
                    }
                }
            }
            return null;
        }

        object OnCardSwipe(CardReader cardReader, Keycard card, BasePlayer player)
        {
            if (IgnoreAdmin(player)) return null;
            if (card.accessLevel == cardReader.accessLevel)
            {
                ControllerMonument zone = _zones.FirstOrDefault(x => IsObjectInSphere(x.transform.position, x.Radius, cardReader.transform.position));
                if (zone != null)
                {
                    zone.IfPlayerCantBeOwner(player);

                    if (zone.Owner == 0)
                    {
                        if (zone.DicTermsOwner.ContainsKey(player.userID))
                        {
                            if (!PlayersWhoOpenedDoor.Contains(player.userID) && zone.monConfig.RulesBecomeOwner.OpenDoor)
                            {
                                zone.DicTermsOwner[player.userID]++;
                                PlayersWhoOpenedDoor.Add(player.userID);
                                zone.CanPlayerBecomeOwner(player);
                            }
                            return null;
                        }
                    }
                    if (zone.Owner != 0)
                    {
                        if (player.userID == zone.Owner || IsTeam(player, zone.Owner))
                        {
                            return null;
                        }
                    }
                    else return true;
                }
            }
            return null;
        }

        internal bool IsObjectInSphere(Vector3 sphereCenter, float sphereRadius, Vector3 objectPosition) => Vector3.Distance(sphereCenter, objectPosition) <= sphereRadius;

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (AllInsidePlayers.ContainsKey(player.userID))
            {
                string id = AllInsidePlayers[player.userID];
                if (id != null)
                {
                    ControllerMonument zone = _zones.FirstOrDefault(x => x.ID == id);
                    if (zone != null)
                    {
                        zone.OnExitPlayer(player);
                    }
                }
            }
        }

        private void OnEntityKill(ScientistNPC entity)
        {
            if (!entity.IsExists()) return;
            OnBossKill(entity);

            ControllerMonument controllerMon = _zones.FirstOrDefault(x => x.ScientistList.Contains(entity));
            if (controllerMon != null)
            {
                controllerMon.ScientistList.Remove(entity);
            }
        }

        #endregion Oxide Hooks

        #region Monuments

        private string Quarry { get; set; } = "assets/bundled/prefabs/static/miningquarry_static.prefab";
        private string Excavator { get; set; } = "assets/bundled/prefabs/autospawn/monument/large/excavator_1.prefab";
        private string Harbor_1 { get; set; } = "assets/bundled/prefabs/autospawn/monument/harbor/harbor_1.prefab";
        private string Harbor_2 { get; set; } = "assets/bundled/prefabs/autospawn/monument/harbor/harbor_2.prefab";

        private HashSet<string> QuarryList { get; set; } = new HashSet<string>()
        {
            "assets/bundled/prefabs/autospawn/monument/small/mining_quarry_a.prefab",
            "assets/bundled/prefabs/autospawn/monument/small/mining_quarry_b.prefab",
            "assets/bundled/prefabs/autospawn/monument/small/mining_quarry_c.prefab"
        };
        private Dictionary<string, Vector3> LocalСoordZoneForMonument { get; set; } = new Dictionary<string, Vector3>()
        {
            ["assets/bundled/prefabs/autospawn/monument/military_bases/desert_military_base_c.prefab"] = new Vector3(7.5f, 0f, 19f),
            ["assets/bundled/prefabs/autospawn/monument/military_bases/desert_military_base_b.prefab"] = new Vector3(0f, 0f, 6f),
            ["assets/bundled/prefabs/autospawn/monument/military_bases/desert_military_base_d.prefab"] = new Vector3(0f, 0f, -1.7f),
            ["assets/bundled/prefabs/autospawn/monument/large/water_treatment_plant_1.prefab"] = new Vector3(0f, 0f, -30f),
            ["assets/bundled/prefabs/autospawn/monument/large/airfield_1.prefab"] = new Vector3(0f, 0f, -26.673f),
            ["assets/bundled/prefabs/autospawn/monument/large/excavator_1.prefab"] = new Vector3(23.133f, 0f, -20.633f),
            ["assets/bundled/prefabs/autospawn/monument/large/powerplant_1.prefab"] = new Vector3(-23.401f, 0f, 0f),
            ["assets/bundled/prefabs/autospawn/monument/medium/junkyard_1.prefab"] = new Vector3(-12.407f, 0f, 9.618f),
            ["assets/bundled/prefabs/autospawn/monument/medium/radtown_small_3.prefab"] = new Vector3(0f, 0f, -17.107f),
            ["OilrigAI"] = new Vector3(14.847f, 0f, -12.318f),
            ["assets/bundled/prefabs/autospawn/monument/roadside/gas_station_1.prefab"] = new Vector3(0f, 0f, 12.687f),
            ["assets/bundled/prefabs/autospawn/monument/roadside/warehouse.prefab"] = new Vector3(0f, 0f, -9.360f)
        };

        private Dictionary<string, string> EntrancesUnderwaterLab { get; set; } = new Dictionary<string, string>()
        {
            ["assets/bundled/prefabs/autospawn/monument/underwater_lab/underwater_lab_a.prefab"] = "moonpool_1200x1500_1way",
            ["assets/bundled/prefabs/autospawn/monument/underwater_lab/underwater_lab_b.prefab"] = "moonpool_1200x1500_2way",
            ["assets/bundled/prefabs/autospawn/monument/underwater_lab/underwater_lab_c.prefab"] = "moonpool_1200x1500_3way",
            ["assets/bundled/prefabs/autospawn/monument/underwater_lab/underwater_lab_d.prefab"] = "moonpool_1200x1800_ladder",
        };

        private HashSet<string> ExcludedMonuments { get; set; } = new HashSet<string>()
        {
            "assets/bundled/prefabs/autospawn/monument/fishing_village/fishing_village_a.prefab",
            "assets/bundled/prefabs/autospawn/monument/fishing_village/fishing_village_b.prefab",
            "assets/bundled/prefabs/autospawn/monument/fishing_village/fishing_village_c.prefab",
            "assets/bundled/prefabs/autospawn/monument/small/stables_a.prefab",
            "assets/bundled/prefabs/autospawn/monument/small/stables_b.prefab",
            "assets/bundled/prefabs/autospawn/monument/medium/compound.prefab",
            "assets/bundled/prefabs/autospawn/monument/medium/bandit_town.prefab",
            "Water Well",
            "Abandoned Cabins",
            "Wild Swamp",
            "Ice Lake",
            "Train Tunnel",
            "Substation",
            "Mountain",
            ""
        };
        private HashSet<MonumentInfo> AllHarborsOnMap { get; set; } = new HashSet<MonumentInfo>();
        private HashSet<DungeonBaseInfo> labsThatAlreadyHaveZone { get; set; } = new HashSet<DungeonBaseInfo>();

        private static string GetNameMonument(MonumentInfo monument)
        {
            if (monument.name.Contains("harbor_1")) return "Small " + monument.displayPhrase.english.Replace("\n", string.Empty);
            if (monument.name.Contains("harbor_2")) return "Large " + monument.displayPhrase.english.Replace("\n", string.Empty);
            if (monument.name.Contains("desert_military_base_a")) return monument.displayPhrase.english.Replace("\n", string.Empty);
            if (monument.name.Contains("desert_military_base_b")) return monument.displayPhrase.english.Replace("\n", string.Empty);
            if (monument.name.Contains("desert_military_base_c")) return monument.displayPhrase.english.Replace("\n", string.Empty);
            if (monument.name.Contains("desert_military_base_d")) return monument.displayPhrase.english.Replace("\n", string.Empty);
            if (monument.name.Contains("underwater_lab_a")) return monument.displayPhrase.english.Replace("\n", string.Empty);
            if (monument.name.Contains("underwater_lab_b")) return monument.displayPhrase.english.Replace("\n", string.Empty);
            if (monument.name.Contains("underwater_lab_c")) return monument.displayPhrase.english.Replace("\n", string.Empty);
            if (monument.name.Contains("underwater_lab_d")) return monument.displayPhrase.english.Replace("\n", string.Empty);
            return monument.displayPhrase.english.Replace("\n", string.Empty);
        }

        private static void GetGlobalCoord(Transform Transform, Vector3 localPosition, out Vector3 globalPosition)
        {
            globalPosition = Transform.TransformPoint(localPosition);
        }

        public class ListOfMonuments
        {
            [JsonProperty(En ? "On/Off" : "Вкл/выкл")] public bool Enabled { get; set; }
            [JsonProperty(En ? "Position (For standard monuments and events, do not fill in this parameter)" : "Позиция (Для стандартных монументов и ивентов не заполняйте этот параметр)")] public string Position { get; set; }
            [JsonProperty(En ? "Radius" : "Радиус")] public float Radius { get; set; }
            [JsonProperty(En ? "Enable the display of the radius of the zone on the map" : "Включить отображение радиуса зоны на карте")] public bool RadiusOnTheMap { get; set; }
            [JsonProperty(En ? "Enable the display of the zone marker on the map" : "Включить отображение маркера зоны на карте")] public bool MarkerOnTheMap { get; set; }
            [JsonProperty(En ? "Monument settings" : "Настройки монумента")] public MonumentConfig MonumentSettings { get; set; }
        }

        private readonly Dictionary<string, ListOfMonuments> _monumentSettings = new Dictionary<string, ListOfMonuments>();
        private readonly Dictionary<string, string> _nameZones = new Dictionary<string, string>();

        private void LoadMonumentSettings()
        {
            foreach (string name in Interface.Oxide.DataFileSystem.GetFiles("MM_Data/MonumentOwner/Monuments/"))
            {
                string fileName = name.Split('/').Last().Split('.').First();
                ListOfMonuments settings = Interface.Oxide.DataFileSystem.ReadObject<ListOfMonuments>($"MM_Data/MonumentOwner/Monuments/{fileName}");
                if (settings != null)
                {
                    Puts($"File {fileName} has been loaded successfully!");
                    _monumentSettings.Add(fileName, settings);
                    _nameZones.Add(fileName, "Monuments");
                }
                else PrintError($"File {fileName} is corrupted and cannot be loaded!");
            }
        }

        private void LoadEventSettings()
        {
            foreach (string name in Interface.Oxide.DataFileSystem.GetFiles("MM_Data/MonumentOwner/Events/"))
            {
                string fileName = name.Split('/').Last().Split('.').First();
                ListOfMonuments settings = Interface.Oxide.DataFileSystem.ReadObject<ListOfMonuments>($"MM_Data/MonumentOwner/Events/{fileName}");
                if (settings != null)
                {
                    Puts($"File {fileName} has been loaded successfully!");
                    _monumentSettings.Add(fileName, settings);
                    _nameZones.Add(fileName, "Events");
                }
                else PrintError($"File {fileName} is corrupted and cannot be loaded!");
            }
        }

        private void LoadCustomMonumentSettings()
        {
            foreach (string name in Interface.Oxide.DataFileSystem.GetFiles("MM_Data/MonumentOwner/Custom Zones/"))
            {
                string fileName = name.Split('/').Last().Split('.').First();
                ListOfMonuments settings = Interface.Oxide.DataFileSystem.ReadObject<ListOfMonuments>($"MM_Data/MonumentOwner/Custom Zones/{fileName}");
                if (settings != null)
                {
                    Puts($"File {fileName} has been loaded successfully!");
                    _monumentSettings.Add(fileName, settings);
                    _nameZones.Add(fileName, "Custom Zones");
                }
                else PrintError($"File {fileName} is corrupted and cannot be loaded!");
            }
        }

        private void UpdateAllDataFiles()
        {
            foreach (KeyValuePair<string, ListOfMonuments> dic in _monumentSettings)
            {
                string value = _nameZones.FirstOrDefault(x => x.Key == dic.Key).Value;
                Interface.Oxide.DataFileSystem.WriteObject($"MM_Data/MonumentOwner/{value}/{dic.Key}", dic.Value);
            }
        }

        private void ListAllMonumentsOnTheMap()
        {
            foreach (MonumentInfo monument in TerrainMeta.Path.Monuments)
            {
                CheckMonument(monument);
            }
        }

        private void ListAllCustomMonumentsOnTheMap()
        {
            foreach (KeyValuePair<string, ListOfMonuments> monumentSettings in _monumentSettings)
            {
                if (monumentSettings.Value.Position != "" && monumentSettings.Value.Enabled)
                {
                    ControllerMonument zoneObject = new GameObject().AddComponent<ControllerMonument>();
                    CreateZoneForCustomMonument(monumentSettings.Value,  zoneObject, monumentSettings.Key);
                }
            }
        }

        internal void CheckMonument(MonumentInfo monument)
        {
            if (ExcludedMonuments.Contains(monument.name) || ExcludedMonuments.Contains(monument.displayPhrase.english)) return;
            
            string _nameFile = GetNameMonument(monument);
            
            if (monument.name == Harbor_1 || monument.name == Harbor_2) AllHarborsOnMap.Add(monument);
            
            ControllerMonument zoneObject = new GameObject().AddComponent<ControllerMonument>();

            if (_monumentSettings.ContainsKey(_nameFile))
            {
                ListOfMonuments settingsForMonument = _monumentSettings[_nameFile];
                if (settingsForMonument != null && settingsForMonument.Enabled) CreateZoneForMonument(settingsForMonument, monument, zoneObject, _nameFile);
            }
        }

        private void CreateZoneForMonument(ListOfMonuments settings, MonumentInfo monument, ControllerMonument zoneObject, string _nameFile)
        {
            zoneObject.PathName = monument.name;
            zoneObject.Name = _nameFile;
            zoneObject.PositionOnTheGrid = PhoneController.PositionToGridCoord(monument.transform.position);
            zoneObject.IsQuarry = QuarryList.Any(x => x == monument.name);
            if (Excavator == monument.name) zoneObject.IsExcavator = true;
            zoneObject.Radius = settings.Radius;
            zoneObject.RadiusOnTheMap = settings.RadiusOnTheMap;
            zoneObject.MarkerOnTheMap = settings.MarkerOnTheMap;
            zoneObject.monConfig = settings.MonumentSettings;
            zoneObject._rules = settings.MonumentSettings.RulesBecomeOwner;
            zoneObject._counterForRules = GetNumberOfRules(zoneObject._rules);
            if (LocalСoordZoneForMonument.ContainsKey(monument.name))
            {
                Vector3 pos;
                GetGlobalCoord(monument.transform, LocalСoordZoneForMonument[monument.name], out pos);
                zoneObject.transform.position = pos;
            }
            else if (_nameFile == "Underwater Lab")
            {
                zoneObject.transform.position = GetZonePosForUnderwaterLab(monument);
                if (zoneObject.transform.position == Vector3.zero)
                {
                    UnityEngine.Object.Destroy(zoneObject.gameObject);
                    return;
                }
            }
            else zoneObject.transform.position = monument.transform.position;
            zoneObject.ID = (zoneObject.transform.position.x + zoneObject.transform.position.y + zoneObject.transform.position.z).ToString();

            zoneObject.InitSphere();
            _zones.Add(zoneObject);

            Puts($"The zone has been created [Monument: {_nameFile} | Position to grid: {PhoneController.PositionToGridCoord(monument.transform.position)} | ID: {zoneObject.ID}] ");
        }
        
        private void CreateZoneForCargo(ListOfMonuments settings, CargoShip cargo, ControllerMonument zoneObject)
        {
            zoneObject.cargo = cargo;
            zoneObject.PathName = "cargoshiptest";
            zoneObject.Name = "Cargo";
            zoneObject.PositionOnTheGrid = PhoneController.PositionToGridCoord(cargo.transform.position);
            zoneObject.ID = cargo.net.ID.Value.ToString();
            zoneObject.Radius = settings.Radius;
            zoneObject.RadiusOnTheMap = settings.RadiusOnTheMap;
            zoneObject.MarkerOnTheMap = settings.MarkerOnTheMap;
            zoneObject.monConfig = settings.MonumentSettings;
            zoneObject._rules = settings.MonumentSettings.RulesBecomeOwner;
            zoneObject.transform.position = cargo.transform.position;
            zoneObject._counterForRules = GetNumberOfRules(zoneObject._rules);
            zoneObject.InitSphere();
            zoneObject.gameObject.transform.SetParent(cargo.transform);
            _zones.Add(zoneObject);

            Puts($"The zone has been created [Monument: Cargo | Position to grid: {PhoneController.PositionToGridCoord(cargo.transform.position)} | ID: {zoneObject.ID}] ");
        }

        private void CreateZoneForCustomMonument(ListOfMonuments settings, ControllerMonument zoneObject, string _nameFile)
        {
            zoneObject.PathName = "CustomZone";
            zoneObject.Name = _nameFile;
            zoneObject.PositionOnTheGrid = PhoneController.PositionToGridCoord(settings.Position.ToVector3());
            zoneObject.IsQuarry = false;
            zoneObject.IsExcavator = false;
            zoneObject.Radius = settings.Radius;
            zoneObject.RadiusOnTheMap = settings.RadiusOnTheMap;
            zoneObject.MarkerOnTheMap = settings.MarkerOnTheMap;
            zoneObject.monConfig = settings.MonumentSettings;
            zoneObject._rules = settings.MonumentSettings.RulesBecomeOwner;
            zoneObject._counterForRules = GetNumberOfRules(zoneObject._rules);
            zoneObject.transform.position = settings.Position.ToVector3();
            zoneObject.ID = (zoneObject.transform.position.x + zoneObject.transform.position.y + zoneObject.transform.position.z).ToString();

            zoneObject.InitSphere();
            _zones.Add(zoneObject);

            Puts($"The zone has been created [Monument: {_nameFile} | Position to grid: {PhoneController.PositionToGridCoord(settings.Position.ToVector3())} | ID: {zoneObject.ID}] ");
        }

        private Vector3 GetZonePosForUnderwaterLab(MonumentInfo monument)
        {
            int i = 0;
            float x = 0;
            float z = 0;

            foreach (DungeonBaseInfo baseModule in TerrainMeta.Path.DungeonBaseEntrances)
            {
                if (baseModule.name == EntrancesUnderwaterLab[monument.name] && !labsThatAlreadyHaveZone.Contains(baseModule))
                {
                    foreach (GameObject module in baseModule.Links)
                    {
                        i++;
                        x = x + module.transform.position.x;
                        z = z + module.transform.position.z;
                    }
                    labsThatAlreadyHaveZone.Add(baseModule);
                    return new Vector3(x / i, monument.transform.position.y - 5, z / i);
                }
            }
            return Vector3.zero;
        }

        private int GetNumberOfRules(RulesBecomeOwnerConfig Rules)
        {
            int i = 0;

            if (Rules.AllTerms)
            {
                if (Rules.TimeToGetOwner != -1) i++;
                if (Rules.Damage >= 0) i++;
                if (Rules.OpenCrate) i++;
                if (Rules.OpenDoor) i++;
                if (Rules.StartQuarry) i++;
                if (Rules.StartExcavator) i++;
                return i;
            }
            else return i + 1;
        }

        #endregion Monuments

        #region Helpers

        private Coroutine _corountine { get; set; } = null;
        internal IEnumerator TimerCanTimeOwner()
        {
            while (true)
            {
                foreach (KeyValuePair<ulong, string> player in PlayersWhoCompletedAllTerms.ToHashSet())
                {
                    ControllerMonument zone = _zones.FirstOrDefault(x => x.ID == player.Value);
                    if (CanTimeOwner(zone.ID, player.Key, zone.monConfig.CooldownOwner) && zone.Owner == 0)
                    {
                        zone.CanPlayerBecomeOwner(BasePlayer.FindByID(player.Key));
                    }
                }
                yield return CoroutineEx.waitForSeconds(1.0f);
            }
        }

        /// <summary>
        /// The method checks whether the rules should apply to the Admin.
        /// Метод проверяет должны ли правила распространяться на Админа.
        /// </summary>
        private bool IgnoreAdmin(BasePlayer player)
        {
            if (player.IsAdmin && !_here._config.AdminEntry) return true;
            else return false;
        }

        private bool IgnoreDamage(Transform targetPosition, BasePlayer attacker)
        {
            ControllerMonument zone1 = IsTargetInsideZone(targetPosition);
            ControllerMonument zone2 = IsPlayerOutsideZone(attacker);

            if (_config.NoDamageOutsideZone && (zone1 != zone2))
            {
                if (_config.Notifications.Notification_NoDamageTarget) PrintToChat(attacker, GetMessage("NoDamageTarget", attacker.UserIDString));
                return true;
            }
            else return false;
        }

        private ControllerMonument IsPlayerOutsideZone(BasePlayer player) => _zones.FirstOrDefault(x => IsObjectInSphere(x.transform.position, x.Radius, player.transform.position));
        
        private ControllerMonument IsTargetInsideZone(Transform target) => _zones.FirstOrDefault(x => IsObjectInSphere(x.transform.position, x.Radius, target.transform.position));
        
        private object CanPlayerBecomeOwnerImmediately(ControllerMonument zone, BasePlayer player)
        {
            if (CanTimeOwner(zone.ID, player.userID, zone.monConfig.CooldownOwner))
            {
                RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                if (playerTeam != null)
                {
                    ulong teamMember = playerTeam.members.FirstOrDefault(x => _here.AllOwners.ContainsKey(x));
                    if (teamMember != 0)
                    {
                        if (!_here._config.CanPlayerChangeMonument)
                        {
                            zone.SendMessageToChat(player, "NoEnterAnotherOwner");
                            return null;
                        }
                        else
                        {
                            ControllerMonument zone2 = _here._zones.FirstOrDefault(x => x.Owner == teamMember);
                            zone2.CancelInvoke(zone2.TimerOwner);
                            zone2.DeleteOwnerInZone();
                            zone.SetOwner(player);

                            return null;
                        }
                    }
                    else
                    {
                        zone.SetOwner(player);
                    }
                }
                else
                {
                    if (_here.AllOwners.ContainsKey(player.userID))
                    {
                        if (!_here._config.CanPlayerChangeMonument)
                        {
                            zone.SendMessageToChat(player, "NoEnterAnotherOwner");
                            return null;
                        }
                        else
                        {
                            ControllerMonument zone2 = _here._zones.FirstOrDefault(x => x.Owner == player.userID);
                            zone2.CancelInvoke(zone2.TimerOwner);
                            zone2.DeleteOwnerInZone();
                            zone.SetOwner(player);

                            return null;
                        }
                    }
                    else
                    {
                        zone.SetOwner(player);
                        return null;
                    }
                }
            }
            else
            {
                zone.SendMessageToChat(player, "EnteringZone3");
                return null;
            }
            return null;
        }

        #endregion Helpers

        #region Team
        [PluginReference] private readonly Plugin Friends, Clans;

        private bool IsTeam(BasePlayer player, ulong targetId)
        {
            if (player == null || targetId == 0) return false;
            if (player.userID == targetId) return true;
            if (player.currentTeam != 0)
            {
                RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                if (playerTeam == null) return false;
                if (playerTeam.members.Contains(targetId)) return true;
            }
            if (plugins.Exists("Friends") && (bool)Friends.Call("AreFriends", player.userID, targetId)) return true;
            if (plugins.Exists("Clans") && (Clans.Author == "k1lly0u" || Clans.Author == "Mevent") && (bool)Clans.Call("IsMemberOrAlly", player.UserIDString, targetId.ToString())) return true;
            return false;
        }

        #endregion Team

        #region Variables

        private const int TargetLayers = -805569537; /* ~(1 << 10 | 1 << 18 | 1 << 28 | 1 << 29) */
        internal HashSet<ControllerMonument> _zones { get; set; } = new HashSet<ControllerMonument>();

        internal HashSet<ulong> PlayersWhoOpenedDoor { get; set; } = new HashSet<ulong>();
        internal HashSet<ulong> PlayersWhoOpenedCrate { get; set; } = new HashSet<ulong>();
        internal HashSet<ulong> PlayersWhoStartQuarry { get; set; } = new HashSet<ulong>();
        internal HashSet<ulong> PlayersWhoStartExcavator { get; set; } = new HashSet<ulong>();
        internal Dictionary<ulong, string> AllOwners { get; set; } = new Dictionary<ulong, string>();
        internal Dictionary<ulong, string> PlayersWhoCompletedAllTerms { get; set; } = new Dictionary<ulong, string>();

        internal Dictionary<ulong, string> AllInsidePlayers { get; set; } = new Dictionary<ulong, string>();

        #endregion Variables

        #region Controller Monument

        internal class ControllerMonument : FacepunchBehaviour
        {
            internal string PathName { get; set; }
            internal string Name { get; set; }
            internal string PositionOnTheGrid { get; set; }
            internal string ID { get; set; }
            internal float Radius { get; set; }
            internal bool RadiusOnTheMap { get; set; }
            internal bool MarkerOnTheMap { get; set; }
            internal MonumentConfig monConfig { get; set; }

            private MapMarkerGenericRadius _mapmarker { get; set; }
            private VendingMachineMapMarker _vendingMarker { get; set; }

            internal RulesBecomeOwnerConfig _rules { get; set; }
            internal int _counterForRules { get; set; }

            internal ulong Owner { get; set; } = 0;
            internal string ownerName { get; set; } = "";
            internal int _timerExitOwner { get; set; } = 0;
            internal int _timerOwner { get; set; } = 0;

            internal bool IsQuarry { get; set; } = false;
            internal bool IsExcavator { get; set; } = false;
            internal bool CanPlayerOpenRecycler { get; set; } = false; // when monument is lock

            internal SphereCollider sphereCollider { get; set; }

            internal CargoShip cargo { get; set; }

            #region sets
            internal HashSet<ScientistNPC> ScientistList { get; set; } = new HashSet<ScientistNPC>();
            internal HashSet<ulong> TanksList { get; set; } = new HashSet<ulong>();
            internal HashSet<ulong> BackpacksList { get; set; } = new HashSet<ulong>();

            internal HashSet<BasePlayer> InsidePlayers { get; set; } = new HashSet<BasePlayer>();
            internal HashSet<ulong> Owners { get; set; } = new HashSet<ulong>();
            internal Dictionary<ulong, int> DicTermsOwner { get; set; } = new Dictionary<ulong, int>();
            internal Dictionary<ulong, int> PlayersTime { get; set; } = new Dictionary<ulong, int>();
            internal Dictionary<ulong, float> PlayersDamage { get; set; } = new Dictionary<ulong, float>();
            internal Dictionary<ulong, float> PlayersBossDamage { get; set; } = new Dictionary<ulong, float>();

            internal HashSet<SphereEntity> spheres { get; set; } = new HashSet<SphereEntity>();
            #endregion sets

            internal void OnDestroy()
            {
                CancelInvoke(TimerOwnerToReturnToZone);
                CancelInvoke(TimerForGetOwner);
                CancelInvoke(TimerOwner);
                CancelInvoke(UpdateMapMarker);

                if (_mapmarker.IsExists()) _mapmarker.Kill();
                if (_vendingMarker.IsExists()) _vendingMarker.Kill();

                foreach (SphereEntity sphere in spheres)
                {
                    if (sphere.IsExists())
                    {
                        sphere.Kill();
                    }
                }
            }

            internal void InitSphere()
            {
                gameObject.layer = 3;
                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = Radius;
                CreateDome();
            }

            private void CreateDome()
            {
                SpawnMapMarker();

                spheres = new HashSet<SphereEntity>();
                if (_here._config.Darkening == 0) return;
                if (Name.Contains("Cargo"))
                {
                    for (int i = 0; i < _here._config.Darkening; i++)
                    {
                        SphereEntity sphere = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab") as SphereEntity;
                        sphere.currentRadius = Radius * 2;
                        sphere.lerpSpeed = 0f;
                        sphere.enableSaving = false;
                        sphere.Spawn();
                        if (cargo != null) sphere.SetParent(cargo);
                        spheres.Add(sphere);
                    }
                }
                else
                {
                    for (int i = 0; i < _here._config.Darkening; i++)
                    {
                        SphereEntity sphere = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", transform.position) as SphereEntity;
                        sphere.currentRadius = Radius * 2;
                        sphere.lerpSpeed = 0f;
                        sphere.enableSaving = false;
                        sphere.Spawn();
                        spheres.Add(sphere);
                    }
                }
            }

            private void OnTriggerEnter(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player.IsPlayer())
                {
                    OnEnterPlayer(player);
                }
                ScientistNPC scientist = other.GetComponentInParent<ScientistNPC>();
                if (scientist != null)
                {
                    ScientistList.Add(scientist);
                }
                BradleyAPC bradley = other.GetComponentInParent<BradleyAPC>();
                if (bradley != null)
                {
                    TanksList.Add(bradley.net.ID.Value);
                }
                DroppedItemContainer backpack = other.GetComponentInParent<DroppedItemContainer>();
                if (backpack != null)
                {
                    BackpacksList.Add(backpack.playerSteamID);
                }
            }

            internal void OnEnterPlayer(BasePlayer player)
            {
                if (player.IsAdmin && !_here._config.AdminEntry) return;
                if (DenyPlayerEntry(player.userID))
                {
                    KickOutPlayer(player);
                    return;
                }
                if (Owner != 0 && !monConfig.RulesForNoOwner.CanEnterNoOwner && !_here.IsTeam(player, Owner))
                {
                    KickOutPlayer(player);
                    return;
                }
                AddPlayerToSets(player);
                if (Owner == 0)
                {
                    SendMessageToChat(player, "RulesBecomeOwner");

                    if (InsidePlayers.Count != 0 && monConfig.RulesBecomeOwner.TimeToGetOwner != -1) InvokeRepeating(TimerForGetOwner, 0f, 1f);
                }
                if (Owner != 0)
                {
                    if (player.userID == Owner && _timerExitOwner > 0)
                    {
                        CancelInvoke(TimerOwnerToReturnToZone);
                        _timerExitOwner = 0;
                    }
                    _here.CreateGUI(Owner, player: player);
                }
            }

            private bool DenyPlayerEntry(ulong playerUserID)
            {
                if (_here._config.NoEnterAnotherOwner && 
                    _here.AllOwners.ContainsKey(playerUserID) && 
                    _here.AllOwners[playerUserID] != ID) return true;

                if (CancelActionPlayerWithCooldown(playerUserID)) return true;

                return false;
            }

            internal bool CancelActionPlayerWithCooldown(ulong playerUserID)
            {
                if (_here.BossMonster != null)
                {
                    ScientistNPC boss = _here.bosses.FirstOrDefault(x => x.net != null && ScientistList.Contains(x));
                    if (boss != null)
                    {
                        if (!_here.CanTimeOwner(boss.displayName, playerUserID)) return true;
                        else return false;
                    }
                }
                if (_here._config.NoEnterPlayerWithCooldown && !_here.CanTimeOwner(ID, playerUserID, monConfig.CooldownOwner)) return true;
                else return false;
            }

            internal void AddPlayerToSets(BasePlayer player)
            {
                if (!DicTermsOwner.ContainsKey(player.userID)) DicTermsOwner.Add(player.userID, 0);
                if (!PlayersTime.ContainsKey(player.userID)) PlayersTime.Add(player.userID, 0);
                if (!PlayersDamage.ContainsKey(player.userID)) PlayersDamage.Add(player.userID, 0.0f);
                if (!PlayersBossDamage.ContainsKey(player.userID)) PlayersBossDamage.Add(player.userID, 0.0f);
                if (!InsidePlayers.Contains(player)) InsidePlayers.Add(player);
                if (!_here.AllInsidePlayers.ContainsKey(player.userID)) _here.AllInsidePlayers.Add(player.userID, ID);
            }

            internal void AddPlayerToSetsWithOwners(BasePlayer player)
            {
                if (!Owners.Contains(player.userID)) Owners.Add(Owner);
                if (!_here.AllOwners.ContainsKey(player.userID)) _here.AllOwners.Add(Owner, ID);
            }

            internal void RemovePlayerFromSets(BasePlayer player, bool allSets = false)
            {
                if (allSets)
                {
                    if (InsidePlayers.Contains(player)) InsidePlayers.Remove(player);
                    if (_here.AllInsidePlayers.ContainsKey(player.userID)) _here.AllInsidePlayers.Remove(player.userID);
                }
                if (DicTermsOwner.ContainsKey(player.userID)) DicTermsOwner.Remove(player.userID);
                if (PlayersTime.ContainsKey(player.userID)) PlayersTime.Remove(player.userID);
                if (PlayersDamage.ContainsKey(player.userID)) PlayersDamage.Remove(player.userID);
                if (PlayersBossDamage.ContainsKey(player.userID)) PlayersBossDamage.Remove(player.userID);
                if (_here.PlayersWhoCompletedAllTerms.ContainsKey(player.userID)) _here.PlayersWhoCompletedAllTerms.Remove(player.userID);
                if (_here.PlayersWhoOpenedCrate.Contains(player.userID)) _here.PlayersWhoOpenedCrate.Remove(player.userID);
                if (_here.PlayersWhoOpenedDoor.Contains(player.userID)) _here.PlayersWhoOpenedDoor.Remove(player.userID);
                if (_here.PlayersWhoStartQuarry.Contains(player.userID)) _here.PlayersWhoStartQuarry.Remove(player.userID);
            }

            internal void RemovePlayerFromSetsWithOwners(BasePlayer player)
            {
                if (Owners.Contains(player.userID)) Owners.Remove(Owner);
                if (_here.AllOwners.ContainsKey(player.userID)) _here.AllOwners.Remove(Owner);
            }

            internal void CancelResultsAllPlayers()
            {
                foreach (BasePlayer player in InsidePlayers)
                {
                    if (DicTermsOwner.ContainsKey(player.userID)) DicTermsOwner[player.userID] = 0;
                    if (!PlayersTime.ContainsKey(player.userID)) PlayersTime.Add(player.userID, 0);
                    if (PlayersDamage.ContainsKey(player.userID)) PlayersDamage[player.userID] = 0.0f;
                    if (PlayersBossDamage.ContainsKey(player.userID)) PlayersBossDamage[player.userID] = 0.0f;

                    if (_here.PlayersWhoOpenedCrate.Contains(player.userID)) _here.PlayersWhoOpenedCrate.Remove(player.userID);
                    if (_here.PlayersWhoOpenedDoor.Contains(player.userID)) _here.PlayersWhoOpenedDoor.Remove(player.userID);
                    if (_here.PlayersWhoStartQuarry.Contains(player.userID)) _here.PlayersWhoStartQuarry.Remove(player.userID);
                }
            }

            internal void SendMessageToChat(BasePlayer player, string theme)
            {
                if (theme == "RulesBecomeOwner")
                {
                    if (_rules.AllTerms && _here._config.Notifications.Notification_EnteringZone1) _here.PrintToChat(player, _here.GetMessage("EnteringZone1", player.UserIDString));
                    if (!_rules.AllTerms && _here._config.Notifications.Notification_EnteringZone2) _here.PrintToChat(player, _here.GetMessage("EnteringZone2", player.UserIDString));

                    if (_rules.TimeToGetOwner != -1 && _here._config.Notifications.Notification_RuleTimeToGetZone) _here.PrintToChat(player, _here.GetMessage("RuleTimeToGetZone", player.UserIDString, _rules.TimeToGetOwner));
                    if (_rules.Damage >= 0 && _here._config.Notifications.Notification_RuleDamage) _here.PrintToChat(player, _here.GetMessage("RuleDamage", player.UserIDString, _rules.Damage));
                    if (_rules.OpenCrate && _here._config.Notifications.Notification_RuleOpenCrate) _here.PrintToChat(player, _here.GetMessage("RuleOpenCrate", player.UserIDString));
                    if (_rules.OpenDoor && _here._config.Notifications.Notification_RuleOpenDoor) _here.PrintToChat(player, _here.GetMessage("RuleOpenDoor", player.UserIDString));

                    if (IsQuarry && _rules.StartQuarry && _here._config.Notifications.Notification_RuleStartQuarry) _here.PrintToChat(player, _here.GetMessage("RuleStartQuarry", player.UserIDString));

                    if (IsExcavator && _rules.StartExcavator && _here._config.Notifications.Notification_RuleStartExcavator) _here.PrintToChat(player, _here.GetMessage("RuleStartExcavator", player.UserIDString));
                }
                if (theme == "AlertTimeInsideZone" && _here._config.Notifications.Notification_AlertTimeInsideZone)
                {
                    _here.PrintToChat(player, _here.GetMessage("AlertTimeInsideZone", player.UserIDString, monConfig.TimersInsideZone.AlertTimeInsideZone));
                }
                if (theme == "AlertTimeOutsideZone1" && _here._config.Notifications.Notification_AlertTimeOutsideZone1)
                {
                    _here.PrintToChat(player, _here.GetMessage("AlertTimeOutsideZone1", player.UserIDString, _timerExitOwner));
                }
                if (theme == "AlertTimeOutsideZone2" && _here._config.Notifications.Notification_AlertTimeOutsideZone2)
                {
                    _here.PrintToChat(player, _here.GetMessage("AlertTimeOutsideZone2", player.UserIDString, monConfig.TimersOutsideZone.AlertTimeOutsideZone));
                }
                if (theme == "YouNonOwnerMonument" && _here._config.Notifications.Notification_YouNonOwnerMonument)
                {
                    _here.PrintToChat(player, _here.GetMessage("YouNonOwnerMonument", player.UserIDString));

                    foreach (BasePlayer pl in BasePlayer.activePlayerList)
                    {
                        if (player != pl) _here.PrintToChat(pl, _here.GetMessage("NoOwnerMonumentForAllPlayers", pl.UserIDString, player.displayName, Name, PositionOnTheGrid, monConfig.TimersInsideZone.TimeOwner));
                    }
                }
                if (theme == "YouOwnerMonument" && _here._config.Notifications.Notification_YouOwnerMonument)
                {
                    _here.PrintToChat(player, _here.GetMessage("YouOwnerMonument", player.UserIDString, monConfig.TimersInsideZone.TimeOwner));

                    foreach (BasePlayer pl in BasePlayer.activePlayerList)
                    {
                        if (player != pl) _here.PrintToChat(pl, _here.GetMessage("OwnerMonumentForAllPlayers", pl.UserIDString, player.displayName, Name, PositionOnTheGrid, monConfig.TimersInsideZone.TimeOwner));
                    }
                }
                if (theme == "NoEnterAnotherOwner" && _here._config.Notifications.Notification_NoEnterAnotherOwner)
                {
                    _here.PrintToChat(player, _here.GetMessage("NoEnterAnotherOwner", player.UserIDString));
                }
                if (theme == "EnteringZone3" && _here._config.Notifications.Notification_EnteringZone3)
                {
                    int coolDown = Convert.ToInt32(_here.GetPlayerCooldown(ID, player.userID, monConfig.CooldownOwner));
                    _here.PrintToChat(player, _here.GetMessage("EnteringZone3", player.UserIDString, coolDown));
                }
            }

            internal void IfPlayerCantBeOwner(BasePlayer player)
            {
                if (!_here.CanTimeOwner(ID, player.userID, monConfig.CooldownOwner))
                {
                    SendMessageToChat(player, "EnteringZone3");
                }
            }

            private void OnTriggerExit(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player.IsPlayer()) OnExitPlayer(player);
            }

            internal void OnExitPlayer(BasePlayer player)
            {
                if (player.IsAdmin && !_here._config.AdminEntry) return;
                _here.DestroyGUI(player: player);
                if (Owner == 0) RemovePlayerFromSets(player, true);
                if (player.userID == Owner)
                {
                    RemovePlayerFromSets(player, true);
                    WhoOwner(player);
                }
                if (player.userID != Owner) RemovePlayerFromSets(player, true);
                if (InsidePlayers.Count == 0) CancelInvoke(TimerForGetOwner);
                if (_here.PlayersWhoCompletedAllTerms.ContainsKey(player.userID)) _here.PlayersWhoCompletedAllTerms.Remove(player.userID);
            }

            internal void WhoOwner(BasePlayer player)
            {
                BasePlayer friend = InsidePlayers.FirstOrDefault(x => _here.IsTeam(x, Owner) && _here.CanTimeOwner(ID, x.userID, monConfig.CooldownOwner) && player != x);
                if (friend != null)
                {
                    RemovePlayerFromSetsWithOwners(player);
                    Owner = friend.userID;
                    AddPlayerToSetsWithOwners(friend);
                }
                else
                {
                    _timerExitOwner = monConfig.TimersOutsideZone.TimeExitOwner;
                    InvokeRepeating(TimerOwnerToReturnToZone, 1f, 1f);
                    SendMessageToChat(player, "AlertTimeOutsideZone1");
                }
            }

            internal void DeleteOwnerInZone()
            {
                BasePlayer player = BasePlayer.FindByID(Owner);
                if (player != null)
                {
                    _here.GetSetPlayerData(player, ID, "Monument");
                    RemovePlayerFromSetsWithOwners(player);
                    SendMessageToChat(player, "YouNonOwnerMonument");
                }
                Owner = 0;
                ownerName = "";
                _here.DestroyGUI(InsidePlayers: InsidePlayers);
                foreach (BasePlayer locPlayer in InsidePlayers) SendMessageToChat(locPlayer, "RulesBecomeOwner");
                if (monConfig.RulesBecomeOwner.TimeToGetOwner != -1) InvokeRepeating(TimerForGetOwner, 0f, 1f);
            }

            #region Timers

            internal void TimerForGetOwner()
            {
                foreach (ulong playerUserID in PlayersTime.Keys.ToHashSet())
                {
                    if (PlayersTime[playerUserID] != monConfig.RulesBecomeOwner.TimeToGetOwner) PlayersTime[playerUserID]++;
                    if (PlayersTime[playerUserID] == monConfig.RulesBecomeOwner.TimeToGetOwner)
                    {
                        BasePlayer player = BasePlayer.FindByID(playerUserID);
                        if (player == null) return;
                        PlayersTime.Remove(player.userID);
                        DicTermsOwner[player.userID]++;
                        IfPlayerCantBeOwner(player);
                        CanPlayerBecomeOwner(player);
                    }
                }
            }

            internal void TimerOwnerToReturnToZone()
            {
                _timerExitOwner--;
                if (monConfig.TimersOutsideZone.AlertTimeOutsideZone > 0 && _timerExitOwner == monConfig.TimersOutsideZone.AlertTimeOutsideZone)
                {
                    BasePlayer player = BasePlayer.FindByID(Owner);
                    if (player != null) SendMessageToChat(player, "AlertTimeOutsideZone2");
                }
                if (_timerExitOwner == 0)
                {
                    CancelInvoke(TimerOwnerToReturnToZone);
                    CancelInvoke(TimerOwner);
                    DeleteOwnerInZone();
                }
            }

            internal void TimerOwner()
            {
                _timerOwner--;
                _here.CreateGUI(Owner: Owner, InsidePlayers: InsidePlayers);
                if (monConfig.TimersInsideZone.AlertTimeInsideZone > 0 && _timerOwner == monConfig.TimersInsideZone.AlertTimeInsideZone)
                {
                    BasePlayer player = BasePlayer.FindByID(Owner);
                    if (player != null) SendMessageToChat(player, "AlertTimeInsideZone");
                }

                if (_timerOwner == 0)
                {
                    CancelInvoke(TimerOwner);
                    DeleteOwnerInZone();
                }
            }

            #endregion Timers

            internal void ClearPlayersWhoCompletedAllTerms()
            {
                foreach (ulong player in _here.PlayersWhoCompletedAllTerms.Keys.ToHashSet())
                {
                    if (_here.PlayersWhoCompletedAllTerms[player] == ID)
                    {
                        _here.PlayersWhoCompletedAllTerms.Remove(player);
                    }
                }
            }

            internal void KickPlayersOutOfTheZone()
            {
                if (!monConfig.RulesForNoOwner.CanEnterNoOwner)
                {
                    foreach (BasePlayer player in InsidePlayers)
                    {
                        if (_here.IsTeam(player, Owner)) continue;
                        else KickOutPlayer(player);
                    }
                }
            }

            internal void KickOutPlayer(BasePlayer player)
            {
                if (player.isMounted)
                {
                    BaseVehicle vehicle = player.GetMounted().VehicleParent();
                    if (vehicle != null)
                    {
                        vehicle.transform.rotation = Quaternion.Euler(vehicle.transform.eulerAngles.x, vehicle.transform.eulerAngles.y - 180f, vehicle.transform.eulerAngles.z);
                        vehicle.rigidBody.velocity *= -2f;
                        return;
                    }
                }
                Vector3 position = transform.position + ((player.transform.position.XZ3D() - transform.position.XZ3D()).normalized * (Radius + 10f));
                position.y = 500f;
                RaycastHit raycastHit;
                if (Physics.Raycast(position, Vector3.down, out raycastHit, 500f, TargetLayers, QueryTriggerInteraction.Ignore)) position.y = raycastHit.point.y;
                else position.y = TerrainMeta.HeightMap.GetHeight(position);
                player.MovePosition(position);
                player.ClientRPCPlayer(null, player, "ForcePositionTo", player.transform.position);
                player.SendNetworkUpdateImmediate();
                _here.PrintToChat(player, _here.GetMessage("NoEnterMonument", player.UserIDString));
            }

            internal void CanPlayerBecomeOwner(BasePlayer possibleOwner)
            {
                if (!possibleOwner.IsPlayer()) return;
                if (DicTermsOwner[possibleOwner.userID] == _counterForRules)
                {
                    if (!_here.PlayersWhoCompletedAllTerms.ContainsKey(possibleOwner.userID)) _here.PlayersWhoCompletedAllTerms.Add(possibleOwner.userID, ID);

                    if (_here.CanTimeOwner(ID, possibleOwner.userID, monConfig.CooldownOwner))
                    {
                        RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindTeam(possibleOwner.currentTeam);
                        if (playerTeam != null)
                        {
                            ulong teamMember = playerTeam.members.FirstOrDefault(x => _here.AllOwners.ContainsKey(x));
                            if (teamMember != 0)
                            {
                                if (!_here._config.CanPlayerChangeMonument)
                                {
                                    SendMessageToChat(possibleOwner, "NoEnterAnotherOwner");
                                    DicTermsOwner[possibleOwner.userID]--;
                                    return;
                                }
                                else
                                {
                                    ControllerMonument zone = _here._zones.FirstOrDefault(x => x.Owner == teamMember);
                                    zone.CancelInvoke(zone.TimerOwner);
                                    zone.DeleteOwnerInZone();
                                    SetOwner(possibleOwner);
                                }
                            }
                            else SetOwner(possibleOwner);
                        }
                        else
                        {
                            if (_here.AllOwners.ContainsKey(possibleOwner.userID))
                            {
                                if (!_here._config.CanPlayerChangeMonument)
                                {
                                    SendMessageToChat(possibleOwner, "NoEnterAnotherOwner");
                                    DicTermsOwner[possibleOwner.userID]--;
                                }
                                else
                                {
                                    ControllerMonument zone = _here._zones.FirstOrDefault(x => x.Owner == possibleOwner.userID);
                                    zone.CancelInvoke(zone.TimerOwner);
                                    zone.DeleteOwnerInZone();
                                    SetOwner(possibleOwner);
                                }
                            }
                            else SetOwner(possibleOwner);
                        }
                    }
                }
            }

            internal void SetOwner(BasePlayer owner)
            {
                Owner = owner.userID;
                ownerName = owner.displayName;
                KickPlayersOutOfTheZone();
                ClearPlayersWhoCompletedAllTerms();
                _here.CreateGUI(Owner, player: owner, InsidePlayers: InsidePlayers);
                CancelInvoke(TimerForGetOwner);
                CancelResultsAllPlayers();
                AddPlayerToSetsWithOwners(owner);
                SendMessageToChat(owner, "YouOwnerMonument");
                _timerOwner = monConfig.TimersInsideZone.TimeOwner;
                InvokeRepeating(TimerOwner, 0f, 1f);
            }

            internal void AddBossDamage(BasePlayer player, float damageHere)
            {
                if (Owner == 0 && InsidePlayers.Contains(player))
                {
                    if (_here._config.NoEnterAnotherOwner && _here._zones.Any(x => x.ID != ID && x.Owners.Contains(player.userID))) return;

                    if (PlayersBossDamage.ContainsKey(player.userID) && PlayersBossDamage[player.userID] >= _here._config.BossMonster.Damage) return;

                    if (PlayersBossDamage.ContainsKey(player.userID)) PlayersBossDamage[player.userID] += damageHere;
                    else PlayersBossDamage.Add(player.userID, damageHere);

                    if (PlayersBossDamage[player.userID] >= _here._config.BossMonster.Damage)
                    {
                        _here.CanPlayerBecomeOwnerImmediately(this, player);
                    }
                }
            }

            internal void AddDamage(BasePlayer player, float damageHere)
            {
                if (Owner == 0 && InsidePlayers.Contains(player))
                {
                    if (_here._config.NoEnterAnotherOwner && _here._zones.Any(x => x.ID != ID && x.Owners.Contains(player.userID))) return;
                    
                    if (PlayersDamage.ContainsKey(player.userID) && PlayersDamage[player.userID] >= monConfig.RulesBecomeOwner.Damage) return;
                    
                    if (PlayersDamage.ContainsKey(player.userID)) PlayersDamage[player.userID] += damageHere;
                    else PlayersDamage.Add(player.userID, damageHere);
                    
                    IfPlayerCantBeOwner(player);
                    if (!_here.CanTimeOwner(ID, player.userID, monConfig.CooldownOwner)) return;
                    
                    if (PlayersDamage[player.userID] >= monConfig.RulesBecomeOwner.Damage)
                    {
                        DicTermsOwner[player.userID]++;
                        CanPlayerBecomeOwner(player);
                    }
                }
            }

            internal void SpawnMapMarker()
            {
                if (cargo == null)
                {
                    if (MarkerOnTheMap)
                    {
                        _vendingMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", transform.position).GetComponent<VendingMachineMapMarker>();
                        _vendingMarker.markerShopName = "Owner: Nobody";
                        _vendingMarker.Spawn();
                    }

                    if (RadiusOnTheMap)
                    {
                        _mapmarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", transform.position).GetComponent<MapMarkerGenericRadius>();
                        _mapmarker.alpha = 0.5f;
                        _mapmarker.color1 = new Color(0.55f, 0.78f, 0.24f);
                        _mapmarker.radius = Radius / 153.8f;
                        _mapmarker.Spawn();
                    }
                }
                else if (cargo != null)
                {
                    if (MarkerOnTheMap)
                    {
                        _vendingMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", transform.position).GetComponent<VendingMachineMapMarker>();
                        _vendingMarker.markerShopName = "Owner: Nobody";
                        _vendingMarker.SetParent(cargo);
                        _vendingMarker.Spawn();
                        _vendingMarker.transform.localPosition = Vector3.zero;
                    }

                    if (RadiusOnTheMap)
                    {
                        _mapmarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", transform.position).GetComponent<MapMarkerGenericRadius>();
                        _mapmarker.alpha = 0.5f;
                        _mapmarker.color1 = new Color(0.55f, 0.78f, 0.24f);
                        _mapmarker.radius = Radius / 153.8f;
                        _mapmarker.SetParent(_vendingMarker);
                        _mapmarker.Spawn();
                        _mapmarker.transform.localPosition = Vector3.zero;
                    }
                }
                if (MarkerOnTheMap || RadiusOnTheMap) InvokeRepeating(UpdateMapMarker, 0, 1f);
            }

            internal void UpdateMapMarker()
            {
                if (Owner == 0)
                {
                    if (MarkerOnTheMap)
                    {
                        _vendingMarker.markerShopName = "Owner: Nobody";
                        _vendingMarker.SetFlag(BaseEntity.Flags.Busy, true);
                        _vendingMarker.SetFlag(BaseEntity.Flags.Reserved4, true);
                    }
                    if (RadiusOnTheMap) _mapmarker.color1 = new Color(0.55f, 0.78f, 0.24f);
                }
                if (Owner != 0)
                {
                    if (MarkerOnTheMap)
                    {
                        _vendingMarker.markerShopName = $"Owner: {ownerName} {_timerOwner} sec.";
                        _vendingMarker.SetFlag(BaseEntity.Flags.Busy, false);
                        _vendingMarker.SetFlag(BaseEntity.Flags.Reserved4, false);
                    }
                    if (RadiusOnTheMap) _mapmarker.color1 = new Color(0.8f, 0.27f, 0.2f);
                }
                if (MarkerOnTheMap) _vendingMarker.SendNetworkUpdate();
                if (RadiusOnTheMap)
                {
                    _mapmarker.SendUpdate();
                    _mapmarker.SendNetworkUpdate();
                }
            }
        }

        #endregion Controller Monument

        #region Time
        public class OldPlayerData
        {
            public ulong steamId { get; set; }
            public Dictionary<ulong, double> lastTime { get; set; }
        }

        public class PlayerData
        {
            public ulong steamId { get; set; }
            public Dictionary<string, double> lastTimeForMonuments { get; set; }
            public Dictionary<string, double> lastTimeForBosses { get; set; }
        }

        string message = "";

        private HashSet<PlayerData> _playersData { get; set; }
        private HashSet<OldPlayerData> _oldPlayersData { get; set; }

        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0);

        private static double CurrentTime => DateTime.UtcNow.Subtract(_epoch).TotalSeconds;

        double cooldownPlayer { get; set; }

        internal void GetSetPlayerData(BasePlayer player, string key, string type)
        {
            PlayerData playerData = _here._playersData.FirstOrDefault(x => x.steamId == player.userID);
            if (playerData == null)
            {
                if (type == "Boss")
                {
                    _here._playersData.Add(
                    new PlayerData
                    {
                        steamId = player.userID,
                        lastTimeForMonuments = new Dictionary<string, double> { },
                        lastTimeForBosses = new Dictionary<string, double>
                        {
                            [key] = CurrentTime
                        }
                    });
                }
                if (type == "Monument")
                {
                    _here._playersData.Add(
                    new PlayerData
                    {
                        steamId = player.userID,
                        lastTimeForMonuments = new Dictionary<string, double>
                        {
                            [key] = CurrentTime
                        },
                        lastTimeForBosses = new Dictionary<string, double> { },
                    });
                }
            }
            else
            {
                if (playerData.lastTimeForMonuments.ContainsKey(key)) playerData.lastTimeForMonuments[key] = CurrentTime;
                else playerData.lastTimeForMonuments.Add(key, CurrentTime);
            }
        }

        private bool CanTimeOwner(string MonumentID, ulong steamId, double cooldown)
        {
            PlayerData playerData = _playersData.FirstOrDefault(x => x.steamId == steamId);
            if (playerData == null) return true;
            if (playerData.lastTimeForMonuments.ContainsKey(MonumentID))
            {
                if (playerData.lastTimeForMonuments[MonumentID] + cooldown < CurrentTime) return true;
                else return false;
            }
            else return true;
        }

        private void DetermineRemainCooldown()
        {
            foreach (PlayerData playerData in _playersData)
            {
                foreach (KeyValuePair<string, double> keyValue in playerData.lastTimeForMonuments.ToHashSet())
                {
                    ControllerMonument zone = _zones.FirstOrDefault(x => x.ID == keyValue.Key);
                    if (zone != null)
                    {
                        if (CanTimeOwner(zone.ID, playerData.steamId, zone.monConfig.CooldownOwner)) playerData.lastTimeForMonuments[zone.ID] = 0;
                        if (!CanTimeOwner(zone.ID, playerData.steamId, zone.monConfig.CooldownOwner))
                        {
                            playerData.lastTimeForMonuments[zone.ID] = GetPlayerCooldown(zone.ID, playerData.steamId, zone.monConfig.CooldownOwner);
                        }
                    }
                }

                foreach (KeyValuePair<string, double> keyValue in playerData.lastTimeForBosses.ToHashSet())
                {
                    if (CanTimeOwner(keyValue.Key, playerData.steamId)) playerData.lastTimeForBosses[keyValue.Key] = 0;
                    if (!CanTimeOwner(keyValue.Key, playerData.steamId))
                    {
                        playerData.lastTimeForBosses[keyValue.Key] = GetPlayerCooldown(keyValue.Key, playerData.steamId);
                    }
                }
            }
        }

        private object DetermineRemainCooldownForChat(KeyValuePair<string, double> keyValue, PlayerData playerData)
        {
            message = "";

            ControllerMonument zone = _zones.FirstOrDefault(x => x.ID == keyValue.Key);
            if (zone != null)
            {
                if (CanTimeOwner(zone.ID, playerData.steamId, zone.monConfig.CooldownOwner)) return null;
                if (!CanTimeOwner(zone.ID, playerData.steamId, zone.monConfig.CooldownOwner))
                {
                    double value = GetPlayerCooldown(zone.ID, playerData.steamId, zone.monConfig.CooldownOwner);

                    message = message + $"{zone.Name} in {zone.PositionOnTheGrid}: <color=#55aaff>{value:F1}</color> <color=#55aaff>sec.</color>";

                    return message;
                }
                return null;
            }
            return null;
        }

        private void ConvertValuesForDateTime()
        {
            if (_oldPlayersData != null)
            {
                foreach (OldPlayerData oldPlayerData in _oldPlayersData)
                {
                    _playersData.Add(new PlayerData { steamId = oldPlayerData.steamId, lastTimeForMonuments = new Dictionary<string, double>(), lastTimeForBosses = new Dictionary<string, double>() });
                }
            }
            else
            {
                foreach (PlayerData playerData in _playersData)
                {
                    foreach (KeyValuePair<string, double> keyValue in playerData.lastTimeForMonuments.ToHashSet())
                    {
                        ControllerMonument zone = _zones.FirstOrDefault(x => x.ID == keyValue.Key);
                        if (zone != null)
                        {
                            if (playerData.lastTimeForMonuments[zone.ID] != 0)
                            {
                                playerData.lastTimeForMonuments[zone.ID] = (CurrentTime + playerData.lastTimeForMonuments[zone.ID]) - zone.monConfig.CooldownOwner;
                            }
                            if (playerData.lastTimeForMonuments[zone.ID] == 0)
                            {
                                playerData.lastTimeForMonuments[zone.ID] = CurrentTime - zone.monConfig.CooldownOwner;
                            }
                        }
                    }

                    foreach (KeyValuePair<string, double> keyValue in playerData.lastTimeForBosses.ToHashSet())
                    {
                        if (playerData.lastTimeForBosses[keyValue.Key] != 0)
                        {
                            playerData.lastTimeForBosses[keyValue.Key] = CurrentTime + playerData.lastTimeForBosses[keyValue.Key] - _config.BossMonster.Cooldown;
                        }
                        if (playerData.lastTimeForBosses[keyValue.Key] == 0)
                        {
                            playerData.lastTimeForBosses[keyValue.Key] = CurrentTime - _config.BossMonster.Cooldown;
                        }
                    }
                }
            }
        }

        private double GetPlayerCooldown(string MonumentID, ulong steamId, double cooldown)
        {
            PlayerData playerData = _playersData.FirstOrDefault(x => x.steamId == steamId);
            if (playerData != null && playerData.lastTimeForMonuments.ContainsKey(MonumentID))
            {
                cooldownPlayer = (playerData.lastTimeForMonuments[MonumentID] + cooldown) - CurrentTime;
                return cooldownPlayer;
            }
            return cooldownPlayer;
        }

        #endregion Time

        #region Commands

        [ChatCommand("mocd")]
        private void ShowListOfCooldowns(BasePlayer player)
        {
            string messageInChat = "";

            PlayerData playerData = _playersData.FirstOrDefault(x => x.steamId == player.userID);
            if (playerData != null)
            {
                foreach (KeyValuePair<string, double> keyValue in playerData.lastTimeForMonuments)
                {
                    if (DetermineRemainCooldownForChat(keyValue, playerData) == null)
                    {
                        continue;
                    }
                    if (DetermineRemainCooldownForChat(keyValue, playerData) != null)
                    {
                        messageInChat = messageInChat + "\n" + "   - " + Convert.ToString(DetermineRemainCooldownForChat(keyValue, playerData));
                    }
                }
                if (messageInChat == "")
                {
                    PrintToChat(player, GetMessage("YouDontHaveCooldown", player.UserIDString));
                    return;
                }
                PrintToChat(player, GetMessage("YouHaveCooldown", player.UserIDString, messageInChat));
            }
            if (playerData == null)
            {
                PrintToChat(player, GetMessage("YouDontHaveCooldown", player.UserIDString));
            }
        }

        [ConsoleCommand("mocdreset")]
        private void ResetPlayerCooldown(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
            {
                ulong playerUserID = Convert.ToUInt64(arg.Args[0]);
                foreach (PlayerData player in _playersData)
                {
                    if (player.steamId != playerUserID) continue;
                    else
                    {
                        player.lastTimeForMonuments.Clear();
                        Puts("The player's cooldown has been reset");
                    }
                }
            }
        }

        [ChatCommand("CreateCustomZone")]
        private void ChatCommandCreateCustomZone(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            if (args == null || args.Length == 0)
            {
                PrintToChat(player, "You <color=#ce3f27>didn't</color> write the name of the zone!");
                return;
            }
            string name = ""; foreach (string arg in args) name += arg != args.Last() ? arg + " " : arg;
            ListOfMonuments settings = new ListOfMonuments()
            {
                Enabled = false,
                Position = Convert.ToString(player.transform.position),
                Radius = 10,
                MonumentSettings = new MonumentConfig
                {
                    TimersInsideZone = new TimersInsideZone
                    { TimeOwner = 600, AlertTimeInsideZone = 60 },
                    TimersOutsideZone = new TimersOutsideZone
                    { TimeExitOwner = 120, AlertTimeOutsideZone = 30, },
                    CooldownOwner = 600,
                    RulesBecomeOwner = new RulesBecomeOwnerConfig
                    { AllTerms = true, TimeToGetOwner = 30, Damage = -1, OpenCrate = false, OpenDoor = true, StartQuarry = false, StartExcavator = false },

                    RulesForNoOwner = new RulesForNoOwnerConfig
                    { CanEnterNoOwner = true, DamageNpc = false, DamageTank = false, LootCrate = false, HackCrate = false, LootBackpacks = false, TargetNpc = true, TargetBradley = true }
                }
            };
            Interface.Oxide.DataFileSystem.WriteObject($"MM_Data/MonumentOwner/Custom Zones/{name}", settings);
            PrintToChat(player, $"You <color=#738d43>have successfully added</color> a new zone named <color=#55aaff>{name}</color>. You <color=#738d43>can edit</color> this zone in the file <color=#55aaff>MM_Data/MonumentOwner/Custom Zones/{name}</color>");
            _monumentSettings.Add(name, settings);
            Puts($"Custom location {name} has been successfully loaded!");
        }

        //[ConsoleCommand("mosetowner")]
        //private void SetOwnerForZone(ConsoleSystem.Arg arg)
        //{
        //    if (arg.Player() != null)
        //    {
        //        ControllerMonument zone = _zones.FirstOrDefault(x => IsObjectInSphere(x.transform.position, x.Radius, arg.Player().transform.position));
        //        if (zone != null)
        //        {
        //            zone.Owner = 1;
        //        }
        //    }
        //}

        //[ConsoleCommand("modeleteowner")]
        //private void DeleteOwnerForZone(ConsoleSystem.Arg arg)
        //{
        //    if (arg.Player() != null)
        //    {
        //        ControllerMonument zone = _zones.FirstOrDefault(x => IsObjectInSphere(x.transform.position, x.Radius, arg.Player().transform.position));
        //        if (zone != null)
        //        {
        //            zone.Owner = 0;
        //        }
        //    }
        //}

        #endregion Commands

        #region Buy Monument

        #endregion Buy Monument

        #region Discord Message

        private void SendDiscordMessage(string messageDis)
        {
            var payload = new
            {
                embeds = new[] { new { title = $"**{DateTime.Now.ToShortTimeString()}** **{DateTime.Now.Day}/{DateTime.Now.Month}/{DateTime.Now.Year}**", description =  messageDis } }
            };

            webrequest.Enqueue("https://discord.com/api/webhooks/1100004725165916240/uPr21A5wQ0Nt47-HbBhi-lc0ir7_0x5owRYlW9JiJTfpyHO8EhprhbRYVMa77ewdDs2v", JsonConvert.SerializeObject(payload), (code, response) =>
            {
                if (code != 200 && code != 204)
                {
                    if (response != null)
                    {
                        try
                        {
                            JObject json = JObject.Parse(response);
                            if (code != 429) PrintWarning($"Discord rejected that payload! Responded with \"{json["message"]}\" Code: {code}");
                        }
                        catch
                        {
                            PrintWarning($"Failed to get a valid response from discord! Error: \"{response}\" Code: {code}");
                        }
                    }
                    else PrintWarning($"Discord didn't respond (down?) Code: {code}");
                }
            }, this, RequestMethod.POST, _headers);

        }

        private readonly Dictionary<string, string> _headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" };

        #endregion Discord Message

        #region Loot

        internal HashSet<string> SatisfyingCrates { get; set; } = new HashSet<string>()
        {
            "assets/bundled/prefabs/radtown/crate_basic.prefab",
            "assets/bundled/prefabs/radtown/crate_elite.prefab",
            "assets/bundled/prefabs/radtown/crate_normal_2.prefab",
            "assets/bundled/prefabs/radtown/crate_normal_2_food.prefab",
            "assets/bundled/prefabs/radtown/crate_normal_2_medical.prefab",
            "assets/bundled/prefabs/radtown/crate_tools.prefab",
            "assets/bundled/prefabs/radtown/dmloot/dm res.prefab",
            "assets/bundled/prefabs/radtown/crate_mine.prefab",
            "assets/bundled/prefabs/radtown/dmloot/dm medical.prefab",
            "assets/bundled/prefabs/radtown/dmloot/dm food.prefab",
            "assets/bundled/prefabs/radtown/crate_underwater_basic.prefab",
            "assets/bundled/prefabs/radtown/crate_normal.prefab",
            "assets/bundled/prefabs/radtown/crate_underwater_advanced.prefab",
            "assets/bundled/prefabs/radtown/dmloot/dm c4.prefab",
            "assets/bundled/prefabs/radtown/dmloot/dm ammo.prefab",
            "assets/bundled/prefabs/radtown/dmloot/dm construction resources.prefab",
            "assets/bundled/prefabs/radtown/dmloot/dm construction tools.prefab",
            "assets/bundled/prefabs/radtown/foodbox.prefab",
            "assets/bundled/prefabs/radtown/vehicle_parts.prefab",
            "assets/bundled/prefabs/radtown/dmloot/dm tier1 lootbox.prefab",
            "assets/bundled/prefabs/radtown/underwater_labs/crate_elite.prefab",
            "assets/bundled/prefabs/radtown/underwater_labs/crate_ammunition.prefab",
            "assets/bundled/prefabs/radtown/dmloot/dm tier3 lootbox.prefab",
            "assets/bundled/prefabs/radtown/underwater_labs/crate_fuel.prefab",
            "assets/bundled/prefabs/radtown/dmloot/dm tier2 lootbox.prefab",
            "assets/bundled/prefabs/radtown/underwater_labs/crate_food_2.prefab",
            "assets/bundled/prefabs/radtown/underwater_labs/crate_food_1.prefab",
            "assets/bundled/prefabs/radtown/underwater_labs/crate_normal.prefab",
            "assets/bundled/prefabs/radtown/underwater_labs/crate_normal_2.prefab",
            "assets/bundled/prefabs/radtown/underwater_labs/crate_tools.prefab",
            "assets/bundled/prefabs/radtown/underwater_labs/crate_medical.prefab",
            "assets/bundled/prefabs/radtown/underwater_labs/tech_parts_2.prefab",
            "assets/bundled/prefabs/radtown/underwater_labs/vehicle_parts.prefab",
            "assets/bundled/prefabs/radtown/underwater_labs/tech_parts_1.prefab",
            "assets/bundled/prefabs/autospawn/resource/loot/trash-pile-1.prefab"
        };

        private bool NREzoneNull = false;

        private object CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (crate == null || !player.IsPlayer()) return null;
            if (IgnoreAdmin(player)) return null;
            ControllerMonument zone = _zones.FirstOrDefault(x => IsObjectInSphere(x.transform.position, x.Radius, crate.transform.position));
            if (zone != null)
            {
                if (zone.Owner != 0)
                {
                    if (zone.monConfig.RulesForNoOwner.HackCrate || IsTeam(player, zone.Owner)) return null;
                    else
                    {
                        if (_config.Notifications.Notification_NoHackCrateEvent) PrintToChat(player, GetMessage("NoHackCrateMonument", player.UserIDString));
                        return true;
                    }
                }
                else if (zone.Owner == 0 && zone.monConfig.RulesBecomeOwner.StartHackCrate)
                {
                    return CanPlayerBecomeOwnerImmediately(zone, player);
                }
                else return null;
            }
            else return null;
        }

        object OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
        {
            if (collectible == null || player == null) return null;
            if (IgnoreAdmin(player)) return null;

            foreach (ControllerMonument zone in _zones)
            {
                if (zone.Owner == 0 && IsObjectInSphere(zone.transform.position, zone.Radius, collectible.transform.position)) return null;

                if (zone.Owner != 0 && IsObjectInSphere(zone.transform.position, zone.Radius, collectible.transform.position))
                {
                    if (zone.monConfig.RulesForNoOwner.PickupCollectableItems || IsTeam(player, zone.Owner)) return null;
                    else
                    {
                        if (_config.Notifications.Notification_CanNotLoot) PrintToChat(player, GetMessage("CanNotLoot", player.UserIDString));
                        return true;
                    }
                }
            }
            return null;
        }

        object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (container == null || player == null) return null;
            if (IgnoreAdmin(player)) return null;
            foreach (ControllerMonument zone in _zones)
            {
                try
                {
                    if (zone.Owner == 0 && SatisfyingCrates.Contains(container.name) && IsObjectInSphere(zone.transform.position, zone.Radius, container.transform.position))
                    {
                        zone.IfPlayerCantBeOwner(player);
                        if (zone.DicTermsOwner.ContainsKey(player.userID))
                        {
                            if (!PlayersWhoOpenedCrate.Contains(player.userID) && zone.monConfig.RulesBecomeOwner.OpenCrate)
                            {
                                zone.DicTermsOwner[player.userID]++;
                                PlayersWhoOpenedCrate.Add(player.userID);
                                zone.CanPlayerBecomeOwner(player);
                            }
                        }
                        return null;
                    }
                    if (zone.Owner != 0 && IsObjectInSphere(zone.transform.position, zone.Radius, container.transform.position))
                    {
                        Recycler recycler = container as Recycler;
                        if (recycler != null && zone.monConfig.RulesForNoOwner.UseRecycler)
                        {
                            return null;
                        }
                        zone.IfPlayerCantBeOwner(player);
                        if (zone.monConfig.RulesForNoOwner.LootCrate || IsTeam(player, zone.Owner)) return null;
                        else
                        {
                            if (_config.Notifications.Notification_CanNotLoot) PrintToChat(player, GetMessage("CanNotLoot", player.UserIDString));
                            return true;
                        }
                    }
                }
                catch (NullReferenceException)
                {
                    NREzoneNull = true;
                    string messageDis = $"{ConVar.Server.hostname} || {_here.Version}\n Name Zone: {zone.Name} ID: {zone.ID}";

                    SendDiscordMessage(messageDis);
                }
            }
            if (NREzoneNull)
            {
                PrintWarning("The functionality of the plugin has been disrupted. There is no critical malfunction, but please report this message to the developer!");

                foreach (ControllerMonument zone in _zones.ToHashSet())
                {
                    if (zone == null)
                    {
                        _zones.Remove(zone);
                    }
                }

                NREzoneNull = false;
            }
            return null;
        }

        private object CanLootEntity(BasePlayer player, NPCPlayerCorpse corpse)
        {
            if (corpse == null || !player.IsPlayer()) return null;
            return CanLootCorpse(player, corpse);
        }

        private object CanLootEntity(BasePlayer player, DroppedItemContainer container)
        {
            if (container == null || !player.IsPlayer()) return null;

            if (container.name == "assets/prefabs/misc/item drop/item_drop_backpack.prefab")
            {
                return CanLootBackpacks(player, container);
            }
            else
            {
                return null;
            }
        }

        private object CanLootCorpse(BasePlayer player, NPCPlayerCorpse corpse)
        {
            if (IgnoreAdmin(player)) return null;
            ControllerMonument zone = _zones.FirstOrDefault(x => x.Owner != 0 && x.BackpacksList.Contains(corpse.playerSteamID));
            if (zone != null)
            {
                if (!IsObjectInSphere(zone.transform.position, zone.Radius, corpse.transform.position) || zone.monConfig.RulesForNoOwner.LootBackpacks || IsTeam(player, zone.Owner))
                {
                    return null;
                }
                else
                {
                    if (_config.Notifications.Notification_CanNotLoot) PrintToChat(player, GetMessage("CanNotLoot", player.UserIDString));
                    return true;
                }
            }
            return null;
        }

        private object CanLootBackpacks(BasePlayer player, DroppedItemContainer container)
        {
            if (IgnoreAdmin(player)) return null;
            ControllerMonument zone = _zones.FirstOrDefault(x => x.Owner != 0 && x.BackpacksList.Contains(container.playerSteamID));
            if (zone != null)
            {
                if (!IsObjectInSphere(zone.transform.position, zone.Radius, container.transform.position) || zone.monConfig.RulesForNoOwner.LootBackpacks || IsTeam(player, zone.Owner) || (container.playerSteamID == player.userID && _config.LootPlayerBackpack))
                {
                    return null;
                }
                else
                {
                    if (_config.Notifications.Notification_CanNotLoot) PrintToChat(player, GetMessage("CanNotLoot", player.UserIDString));
                    return true;
                }
            }
            return null;
        }

        private void OnCorpsePopulate(ScientistNPC entity, NPCPlayerCorpse corpse)
        {
            if (entity == null || corpse == null) return;
            
            ControllerMonument controllerMon = _zones.FirstOrDefault(x => x.ScientistList.Contains(entity));
            if (controllerMon != null)
            {
                controllerMon.ScientistList.Remove(entity);
                controllerMon.BackpacksList.Add(corpse.playerSteamID);
            }
        }

        private object OnLootLockedEntity(BasePlayer player, NPCPlayerCorpse corpse)
        {
            if (corpse == null || !player.IsPlayer()) return null;
            return CanLootCorpse(player, corpse) == null ? null : (object)false;
        }

        private object OnLootLockedEntity(BasePlayer player, DroppedItemContainer container)
        {
            if (container == null || !player.IsPlayer()) return null;
            return CanLootBackpacks(player, container) == null ? null : (object)false;
        }
        #endregion Loot

        #region Damage

        private object OnEntityTakeDamage(LootContainer barrel, HitInfo info)
        {
            BasePlayer attacker = info.InitiatorPlayer;
            if (!attacker.IsPlayer()) return null;
            if (IgnoreAdmin(attacker)) return null;
            if (IgnoreDamage(barrel.transform, attacker)) return true;

            ControllerMonument zone = _zones.FirstOrDefault(x => IsObjectInSphere(x.transform.position, x.Radius, barrel.transform.position));
            if (zone != null)
            {
                if (zone.Owner != 0)
                {
                    if (zone.monConfig.RulesForNoOwner.DamageBarrel || IsTeam(attacker, zone.Owner)) return null;
                    else
                    {
                        if (_config.Notifications.Notification_NoDamageBarrel) PrintToChat(attacker, GetMessage("NoDamageBarrel", attacker.UserIDString));
                        return true;
                    }
                }
                else if (zone.Owner == 0)
                {
                    if (zone.CancelActionPlayerWithCooldown(attacker.userID)) return true;
                    return null;
                }
            }
            else if (zone == null) return null;

            return null;
        }

        private object OnEntityTakeDamage(ScientistNPC npc, HitInfo info)
        {
            BasePlayer attacker = info.InitiatorPlayer;
            if (!attacker.IsPlayer()) return null;
            if (IgnoreAdmin(attacker)) return null;
            if (IgnoreDamage(npc.transform, attacker)) return true;

            ControllerMonument zone = _zones.FirstOrDefault(x => x.ScientistList.Contains(npc));
            if (zone != null)
            {
                if (bosses.Contains(npc))
                {
                    if (zone.CancelActionPlayerWithCooldown(attacker.userID)) return true;
                    if (!CanTimeOwner(npc.displayName, attacker.userID)) return true;
                    zone.AddBossDamage(attacker, info.damageTypes.Total() * _config.BossMonster.ScaleDamageBoss);
                }

                if (zone.Owner != 0)
                {
                    if (zone.monConfig.RulesForNoOwner.DamageNpc || IsTeam(attacker, zone.Owner)) return null;
                    else
                    {
                        if (_config.Notifications.Notification_NoDamageScientist) PrintToChat(attacker, GetMessage("NoDamageScientist", attacker.UserIDString));
                        return true;
                    }
                }
                else if (zone._rules.Damage == -1 && zone.Owner == 0)
                {
                    if (zone.CancelActionPlayerWithCooldown(attacker.userID)) return true;
                    return null;
                }
                else if (zone._rules.Damage >= 0 && zone.Owner == 0)
                {
                    if (zone.CancelActionPlayerWithCooldown(attacker.userID)) return true;
                    zone.AddDamage(attacker, info.damageTypes.Total() * _config.ScaleDamageNPC);
                    return null;
                }
            }

            return null;
        }

        private object OnEntityTakeDamage(BradleyAPC bradley, HitInfo info)
        {
            BasePlayer attacker = info.InitiatorPlayer;
            if (!attacker.IsPlayer()) return null;
            if (IgnoreAdmin(attacker)) return null;
            if (IgnoreDamage(bradley.transform, attacker)) return true;

            ControllerMonument zone = _zones.FirstOrDefault(x => x.TanksList.Contains(bradley.net.ID.Value));
            if (zone != null)
            {
                if (zone._rules.Damage == -1) return null;
                if (zone.Owner == 0 && zone._rules.Damage >= 0)
                {
                    if (zone.CancelActionPlayerWithCooldown(attacker.userID)) return true;
                    zone.AddDamage(attacker, info.damageTypes.Total() * _config.ScaleDamageBradley);
                    return null;
                }
                else
                {
                    if (zone.monConfig.RulesForNoOwner.DamageTank || IsTeam(attacker, zone.Owner)) return null;
                    else
                    {
                        if (_config.Notifications.Notification_NoDamageTank) PrintToChat(attacker, GetMessage("NoDamageTank", attacker.UserIDString));
                        return true;
                    }
                }
            }

            return null;
        }

        private object CanEntityTakeDamage(ScientistNPC npc, HitInfo info)
        {
            BasePlayer attacker = info.InitiatorPlayer;
            if (!attacker.IsPlayer()) return null;
            if (IgnoreAdmin(attacker)) return null;
            if (IgnoreDamage(npc.transform, attacker)) return false;

            ControllerMonument zone = _zones.FirstOrDefault(x => x.Owner != 0 && x.ScientistList.Contains(npc));
            if (zone != null)
            {
                if (zone.monConfig.RulesForNoOwner.DamageNpc || IsTeam(attacker, zone.Owner)) return null;
                else return false;
            }

            return null;
        }

        private object CanEntityTakeDamage(BradleyAPC bradley, HitInfo info)
        {
            BasePlayer attacker = info.InitiatorPlayer;
            if (!attacker.IsPlayer()) return null;
            if (IgnoreAdmin(attacker)) return null;
            if (IgnoreDamage(bradley.transform, attacker)) return false;

            ControllerMonument zone = _zones.FirstOrDefault(x => x.Owner != 0 && x.TanksList.Contains(bradley.net.ID.Value));
            if (zone != null)
            {
                if (zone.monConfig.RulesForNoOwner.DamageTank || IsTeam(attacker, zone.Owner)) return null;
                else return false;
            }

            return null;
        }

        private void OnEntityKill(BradleyAPC bradley)
        {
            if (bradley == null) return;
            ulong id = bradley.net.ID.Value;
            ControllerMonument zone = _zones.FirstOrDefault(x => x.TanksList.Contains(id));
            if (zone != null) zone.TanksList.Remove(id);
        }
        #endregion Damage

        #region Target
        private object OnNpcTarget(ScientistNPC attacker, BasePlayer player)
        {
            if (attacker == null || !player.IsPlayer()) return null;
            ControllerMonument controllerMon = _zones.FirstOrDefault(x => x.Owner != 0 && x.ScientistList.Contains(attacker));
            if (controllerMon != null)
            {
                if (controllerMon.monConfig.RulesForNoOwner.TargetNpc || IsTeam(player, controllerMon.Owner)) return null;
                else return true;
            }
            return null;
        }

        private object OnBotReSpawnNPCTarget(ScientistNPC npc, BasePlayer player) => OnNpcTarget(npc, player);

        private object OnCustomNpcTarget(ScientistNPC attacker, BasePlayer player)
        {
            if (attacker == null || !player.IsPlayer()) return null;
            ControllerMonument controllerMon = _zones.FirstOrDefault(x => x.Owner != 0 && x.ScientistList.Contains(attacker));
            if (controllerMon != null)
            {
                if (controllerMon.monConfig.RulesForNoOwner.TargetNpc || IsTeam(player, controllerMon.Owner)) return null;
                else return false;
            }
            return null;
        }

        private object CanBradleyApcTarget(BradleyAPC bradley, BasePlayer player)
        {
            if (bradley == null || !player.IsPlayer()) return null;
            ControllerMonument controllerMon = _zones.FirstOrDefault(x => x.Owner != 0 && x.TanksList.Contains(bradley.net.ID.Value));
            if (controllerMon != null)
            {
                if (controllerMon.monConfig.RulesForNoOwner.TargetBradley || IsTeam(player, controllerMon.Owner)) return null;
                else return false;
            }
            return null;
        }
        #endregion Target

        #region MiningQuarry

        void OnQuarryToggled(MiningQuarry quarry, BasePlayer player)
        {
            if (quarry.gameObject.name == _here.Quarry)
            {
                if (IgnoreAdmin(player)) return;
                ControllerMonument zone = _zones.FirstOrDefault(x => IsObjectInSphere(x.transform.position, x.Radius, quarry.transform.position));
                if (zone != null)
                {
                    zone.IfPlayerCantBeOwner(player);

                    if (zone.Owner == 0)
                    {
                        if (zone.DicTermsOwner.ContainsKey(player.userID))
                        {
                            if (!PlayersWhoStartQuarry.Contains(player.userID) && zone.monConfig.RulesBecomeOwner.StartQuarry && quarry.IsOn())
                            {
                                zone.DicTermsOwner[player.userID]++;
                                PlayersWhoStartQuarry.Add(player.userID);
                                zone.CanPlayerBecomeOwner(player);
                            }
                        }
                    }
                    if (zone.Owner != 0)
                    {
                        if (player.userID == zone.Owner || IsTeam(player, zone.Owner))
                        {
                            return;
                        }

                        if ((player.userID != zone.Owner || !IsTeam(player, zone.Owner)) && quarry.FuelCheck())
                        {
                            quarry.SetOn(!quarry.IsOn());
                        }
                    }
                }
            }
        }

        #endregion MiningQuarry

        #region Excavator

        object OnDieselEngineToggle(DieselEngine engine, BasePlayer player)
        {
            if (IgnoreAdmin(player)) return null;
            ControllerMonument zone = _zones.FirstOrDefault(x => IsObjectInSphere(x.transform.position, x.Radius, engine.transform.position));
            if (zone != null)
            {
                zone.IfPlayerCantBeOwner(player);

                if (zone.Owner == 0)
                {
                    if (zone.DicTermsOwner.ContainsKey(player.userID))
                    {
                        if (!PlayersWhoStartExcavator.Contains(player.userID) && zone.monConfig.RulesBecomeOwner.StartExcavator && engine.GetFuelAmount() > 0)
                        {
                            zone.DicTermsOwner[player.userID]++;
                            PlayersWhoStartExcavator.Add(player.userID);
                            zone.CanPlayerBecomeOwner(player);
                            return null;
                        }
                    }
                    return null;
                }
                if (zone.Owner != 0)
                {
                    if (player.userID == zone.Owner || IsTeam(player, zone.Owner))
                    {
                        return null;
                    }

                    if ((player.userID != zone.Owner || !IsTeam(player, zone.Owner)))
                    {
                        return true;
                    }
                }
            }
            return null;
        }

        object OnExcavatorResourceSet(ExcavatorArm arm, string resourceName, BasePlayer player)
        {
            if (IgnoreAdmin(player)) return null;
            ControllerMonument zone = _zones.FirstOrDefault(x => IsObjectInSphere(x.transform.position, x.Radius, arm.transform.position));
            if (zone != null)
            {
                if (zone.Owner == 0)
                {
                    return null;
                }
                if (zone.Owner != 0)
                {
                    if (player.userID == zone.Owner || IsTeam(player, zone.Owner))
                    {
                        return null;
                    }

                    if ((player.userID != zone.Owner || !IsTeam(player, zone.Owner)))
                    {
                        if (_config.Notifications.Notification_CanNotResourceSet) PrintToChat(player, GetMessage("CanNotResourceSet", player.UserIDString));
                        return true;
                    }
                }
                return null;
            }
            return null;
        }

        #endregion Excavator

        #region NTeleportation

        private void OnPlayerTeleported(BasePlayer player, Vector3 oldPos, Vector3 newPos)
        {
            if (!AllInsidePlayers.ContainsKey(player.userID) || !player.IsPlayer()) return;
            ControllerMonument zone = _zones.FirstOrDefault(x => x.InsidePlayers.Contains(player));
            if (zone != null)
            {
                zone.OnExitPlayer(player);
            }
        }

        #endregion NTeleportation

        #region API

        private bool HasZone(Vector3 posMonument)
        {
            if (posMonument != null)
            {
                ControllerMonument zone = _zones.FirstOrDefault(x => IsObjectInSphere(x.transform.position, x.Radius, posMonument));
                if (zone != null)
                {
                    return true;
                }
                else { return false; }
            }
            else { return false; }
        }

        private bool HasOwner(Vector3 posMonument)
        {
            if (posMonument != null)
            {
                ControllerMonument zone = _zones.FirstOrDefault(x => IsObjectInSphere(x.transform.position, x.Radius, posMonument));
                if (zone != null && zone.Owner != 0)
                {
                    return true;
                }
                else { return false; }
            }
            else { return false; }
        }

        private ulong GetOwnerID(Vector3 posMonument)
        {
            if (posMonument != null)
            {
                ControllerMonument zone = _zones.FirstOrDefault(x => IsObjectInSphere(x.transform.position, x.Radius, posMonument));
                if (zone != null)
                {
                    return zone.Owner;
                }
                else { return 0; }
            }
            else { return 0; }
        }

        private bool SetOwnerID(Vector3 posMonument, ulong userID)
        {
            if (posMonument != null && userID != 0)
            {
                ControllerMonument zone = _zones.FirstOrDefault(x => IsObjectInSphere(x.transform.position, x.Radius, posMonument));
                if (zone != null)
                {
                    if (zone.Owner == 0)
                    {
                        BasePlayer player = BasePlayer.FindByID(userID);
                        zone.SetOwner(player);
                        return true;
                    }
                    else if (zone.Owner != 0)
                    {
                        if (zone.Owner == userID) return true;
                        else return false;
                    }
                }
                return false;
            }
            return false;
        }

        private bool CanPlayerBecomeOwner(Vector3 posMonument, BasePlayer player)
        {
            if (posMonument != null && player != null)
            {
                ControllerMonument zone = _zones.FirstOrDefault(x => IsObjectInSphere(x.transform.position, x.Radius, posMonument));
                if (zone != null)
                {
                    if (CanTimeOwner(zone.ID, player.userID, zone.monConfig.CooldownOwner))
                    {
                        RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                        if (playerTeam != null)
                        {
                            ulong teamMember = playerTeam.members.FirstOrDefault(x => _here.AllOwners.ContainsKey(x));
                            if (teamMember != 0)
                            {
                                if (!_here._config.CanPlayerChangeMonument)
                                {
                                    return false;
                                }
                                else
                                {
                                    return true;
                                }
                            }
                            else return true;
                        }
                        else
                        {
                            if (_here.AllOwners.ContainsKey(player.userID))
                            {
                                if (!_here._config.CanPlayerChangeMonument) return false;
                                else return true;
                            }
                            else return true;
                        }
                    }
                    else return false; 
                }
                else return false;
            }
            else return false;
        }

        private bool RemoveZone(MonumentInfo monument)
        {
            if (monument != null)
            {
                ControllerMonument zone = _zones.FirstOrDefault(x => IsObjectInSphere(x.transform.position, x.Radius, monument.transform.position));
                if (zone != null)
                {
                    _zones.Remove(zone);
                    UnityEngine.Object.Destroy(zone.gameObject);
                    return true;
                }
                else { return false; }
            }
            else { return false; }
        }

        private bool CreateZone(MonumentInfo monument)
        {
            string nameMon = GetNameMonument(monument);

            if (monument != null && _monumentSettings.ContainsKey(nameMon))
            {
                ListOfMonuments monSettings = _monumentSettings[nameMon];
                if (monSettings.Enabled)
                {
                    CheckMonument(monument);
                    return true;
                }
                else return false;
            }
            else { return false; }
        }

        #endregion API

        #region BossMonster

        [PluginReference] private readonly Plugin BossMonster;

        HashSet<ScientistNPC> bosses { get; set; } = null;

        void OnBossSpawn(ScientistNPC boss)
        {
            bosses.Add(boss);
        }

        void OnBossKilled(ScientistNPC boss, BasePlayer attacker)
        {
            GetSetPlayerData(attacker, boss.displayName, "Boss");
            if (bosses.Contains(boss)) bosses.Remove(boss);
        }

        void OnBossKill(ScientistNPC boss)
        {
            if (bosses.Contains(boss)) bosses.Remove(boss);
        }

        object CanBossSpawn(string name, Vector3 pos)
        {
            ControllerMonument zone = _zones.FirstOrDefault(x => IsObjectInSphere(x.transform.position, x.Radius, pos));
            if (zone != null)
            {
                if (zone.Owner != 0) return true;
                else return null;
            }
            else return null;
        }

        private bool CanTimeOwner(string displayNameBoss, ulong steamId)
        {
            PlayerData playerData = _playersData.FirstOrDefault(x => x.steamId == steamId);
            if (playerData == null) return true;
            if (playerData.lastTimeForBosses.ContainsKey(displayNameBoss))
            {
                if (playerData.lastTimeForBosses[displayNameBoss] + _config.BossMonster.Cooldown < CurrentTime) return true;
                else return false;
            }
            else return true;
        }

        private double GetPlayerCooldown(string displayNameBoss, ulong steamId)
        {
            PlayerData playerData = _playersData.FirstOrDefault(x => x.steamId == steamId);
            if (playerData != null && playerData.lastTimeForBosses.ContainsKey(displayNameBoss))
            {
                cooldownPlayer = (playerData.lastTimeForBosses[displayNameBoss] + _config.BossMonster.Cooldown) - CurrentTime;
                return cooldownPlayer;
            }
            return cooldownPlayer;
        }

        #endregion BossMonster
    }
}

namespace Oxide.Plugins.MonumentOwnerExtensionMethods
{
    public static class ExtensionMethods
    {
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
        public static HashSet<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            HashSet<TSource> result = new HashSet<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) result.Add(enumerator.Current);
            return result;
        }
        public static TSource First<TSource>(this IList<TSource> source) => source[0];
        public static TSource Last<TSource>(this IList<TSource> source) => source[source.Count - 1];
        public static bool IsPlayer(this BasePlayer player) => player != null && player.userID.IsSteamId();

        public static bool IsExists(this BaseNetworkable entity) => entity != null && !entity.IsDestroyed;

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

        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
        {
            HashSet<TSource> result = new HashSet<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(enumerator.Current);
            return result;
        }

        public static HashSet<T> OfType<T>(this IEnumerable<BaseNetworkable> source)
        {
            HashSet<T> result = new HashSet<T>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (enumerator.Current is T) result.Add((T)(object)enumerator.Current);
            return result;
        }
    }
}