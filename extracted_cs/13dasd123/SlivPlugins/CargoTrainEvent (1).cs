using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;
using System.Linq;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("CargoTrainEvent", "https://devplugins.ru/", VERSION)]
    [Description("Just like Cargo Ships - but with trains")]
    public class CargoTrainEvent : RustPlugin
    {
        private static CargoTrainEvent Instance;
        private static Timer EventTimer;

        [PluginReference]
        private Plugin Kits, FuelManager, TruePVE, DeathNotes, GUIAnnouncements;

        #region CONST & STATIC
        public const string VERSION = "1.0.11";
        public const ulong INTERNAL_OWNERID = 1337422;

        public const string PREFAB_DRIVER = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_cargo_turret_any.prefab";
        public const string PREFAB_SCIENTIST = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_cargo_turret_any.prefab";

        public const string PREFAB_TRAIN = "assets/content/vehicles/workcart/workcart.entity.prefab";

        public const string PREFAB_CRATE_LOCKED = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab";
        public const string PREFAB_CRATE_ELITE = "assets/bundled/prefabs/radtown/crate_elite.prefab";
        public const string PREFAB_CRATE_BRADLEY = "assets/prefabs/npc/m2bradley/bradley_crate.prefab";
        public const string PREFAB_CRATE_HELI = "assets/prefabs/npc/patrol helicopter/heli_crate.prefab";
        public const string PREFAB_CRATE_MARKER = "assets/prefabs/tools/map/cratemarker.prefab";
        public const string PREFAB_GENERIC_MARKER = "assets/prefabs/tools/map/genericradiusmarker.prefab";
        public const string PREFAB_RUG = "assets/prefabs/deployable/rug/rug.deployed.prefab";
        public const string PREFAB_SIRENLIGHT = "assets/prefabs/deployable/playerioents/lights/sirenlight/electric.sirenlight.deployed.prefab";
        public const string PREFAB_COUNTER = "assets/prefabs/deployable/playerioents/counter/counter.prefab";
        public const string PREFAB_ALARM = "assets/prefabs/deployable/playerioents/alarms/audioalarm.prefab";
        public const string PREFAB_CCTV = "assets/prefabs/deployable/cctvcamera/cctv_deployed.prefab";

        public const string FX_C4_EXPLOSION = "assets/prefabs/tools/c4/effects/c4_explosion.prefab";
        public const string FX_EXPLOSION_01 = "assets/bundled/prefabs/fx/explosions/explosion_01.prefab";
        public const string FX_EXPLOSION_02 = "assets/bundled/prefabs/fx/explosions/explosion_02.prefab";
        public const string FX_EXPLOSION_03 = "assets/bundled/prefabs/fx/explosions/explosion_03.prefab";

        public const ulong SKIN_SPLAT1 = 2451067899;

        public const int ITEM_SNOWBALL = -363689972;
        public const int ITEM_SUIT_HEAVY = -1772746857;
        public const int ITEM_LOWGRADE = -946369541;

        #endregion

        #region HOOKS
        private void Init()
        {
            Instance = null;
        }

        private void OnServerInitialized()
        {
            PrintWarning("\n-----------------------------\n" +
            "     Author - https://devplugins.ru/\n" +
            "     VK - https://vk.com/dev.plugin\n" +
            "     Discord - https://discord.gg/eHXBY8hyUJ\n" +
            "-----------------------------");
            Instance = this;
            lang.RegisterMessages(LangMessages, this);

            permission.RegisterPermission(PERM_ADMIN, this);

            LoadConfigData();
            ProcessConfigData();

            ConfigValues = new Dictionary<string, InteractiveConfigValue>();

            GenerateAllInteractiveConfigValues();

            SpecialTrain.TrainDriverNetIDToSpecialTrain = new Dictionary<ulong, SpecialTrain>();

            SpecialTrain.TrainNetIDToSpecialTrain = new Dictionary<ulong, SpecialTrain>();
            SpecialTrain.TrainNetIDToTrainEngine = new Dictionary<ulong, TrainEngine>();
            SpecialTrain.WorkcartEntityNetIDToSpecialTrain = new Dictionary<ulong, SpecialTrain>();

            SpecialTrain.PlatformTriggerToTrain = new Dictionary<TriggerBase, SpecialTrain>();

            SpecialTrain.UserIDToSpecialTrains = new Dictionary<ulong, List<SpecialTrain>>();

            //pre-cleanup

            foreach (var entity in BaseNetworkable.FindObjectsOfType<BaseEntity>().Where(e => e.OwnerID == INTERNAL_OWNERID))
            {
                entity.Kill(BaseNetworkable.DestroyMode.None);
            }

            foreach (var train in UnityEngine.Object.FindObjectsOfType<TrainEngine>())
            {
                OnEntitySpawned(train);
            }

            //broken from December 2021 forced wipe
            //FuelManagerCheck();
            TruePVECheck();
            DeathNotesCheck();

            ScheduleTrainEventAtRandom();
        }


        private void OnBookmarkControlStarted(ComputerStation station, BasePlayer player, string text, CCTV_RC cctv)
        {
            if (cctv == null) return;

            if (cctv.OwnerID != INTERNAL_OWNERID) return;

            var group = Network.Net.sv.visibility.GetGroup(cctv.transform.position);

            player.net.SwitchSecondaryGroup(group);

            Interface.CallHook("OnBookmarkControlStartedTrainCCTV", player, text, cctv);
        }

        private void OnEntitySpawned(TrainEngine train)
        {
            if (Instance == null) return;
            SpecialTrain.TrainNetIDToTrainEngine.Add(train.net.ID.Value, train);

            train.SetFlag(TrainEngine.Flag_AltColor, false);
            train.SendNetworkUpdateImmediate();
        }

        private void OnCrateHack(HackableLockedCrate crate)
        {
            if (Instance == null) return;

            if (crate.OwnerID != INTERNAL_OWNERID) return;

            if (!SpecialTrain.WorkcartEntityNetIDToSpecialTrain.ContainsKey(crate.net.ID.Value)) return;


            SpecialTrain.WorkcartEntityNetIDToSpecialTrain[crate.net.ID.Value].OnCrateHack(crate);

            Interface.CallHook("OnTrainCrateHack", SpecialTrain.WorkcartEntityNetIDToSpecialTrain[crate.net.ID.Value].Train, crate);

        }

        object OnCounterModeToggle(PowerCounter counter, BasePlayer player, bool wants)
        {
            if (Instance == null) return null;

            if (counter.OwnerID != INTERNAL_OWNERID) return null;

            return false;
        }

        private object CanBeWounded(BasePlayer player)
        {
            if (Instance == null) return null;
            if (!SpecialTrain.TrainDriverNetIDToSpecialTrain.ContainsKey(player.net.ID.Value))
            {
                return null;
            }

            //drivers can't be wounded
            return false;
        }
        private object OnTurretTarget(AutoTurret turret, ScientistNPC target)
        {
            if (Instance == null) return null;
            if (turret == null) return null;
            if (turret.net == null) return null;
            if (target == null) return null;
            if (target.net == null) return null;

            if (SpecialTrain.WorkcartEntityNetIDToSpecialTrain.ContainsKey(target.net.ID.Value))
            {
                return false;
            }

            return null;
        }

        private object CanBradleyApcTarget(BradleyAPC bradley, ScientistNPC target)
        {
            if (Instance == null) return null;
            if (bradley == null) return null;
            if (target == null) return null;
            if (target.net == null) return null;
            if (bradley.net == null) return null;

            if (SpecialTrain.WorkcartEntityNetIDToSpecialTrain.ContainsKey(target.net.ID.Value))
            {
                return false;
            }

            return null;
        }

        private object CanHelicopterTarget(PatrolHelicopterAI heli, BasePlayer target)
        {
            if (Instance == null) return null;
            if (heli == null) return null;
            if (target == null) return null;
            if (target.net == null) return null;

            if (SpecialTrain.WorkcartEntityNetIDToSpecialTrain.ContainsKey(target.net.ID.Value))
            {
                return false;
            }

            if (SpecialTrain.TrainDriverNetIDToSpecialTrain.ContainsKey(target.net.ID.Value))
            {
                return false;
            }

            return null;
        }

        private object OnNpcTarget(BaseNpc attacker, BasePlayer target)
        {
            if (Instance == null) return null;
            if (attacker == null) return null;
            if (target == null) return null;
            if (target.net == null) return null;
            if (attacker.net == null) return null;


            //Drivers will never be targets nor target anything and we can skip a lot of logic

            if (SpecialTrain.TrainDriverNetIDToSpecialTrain.ContainsKey(target.net.ID.Value))
            {
                return true;
            }

            if (SpecialTrain.TrainDriverNetIDToSpecialTrain.ContainsKey(attacker.net.ID.Value))
            {
                return true;
            }

            //attacker and target might or might not belong to the train

            bool attackerIsSpecial = SpecialTrain.WorkcartEntityNetIDToSpecialTrain.ContainsKey(attacker.net.ID.Value) || SpecialTrain.TrainDriverNetIDToSpecialTrain.ContainsKey(attacker.net.ID.Value);
            bool targetIsSpecial = SpecialTrain.WorkcartEntityNetIDToSpecialTrain.ContainsKey(target.net.ID.Value) || SpecialTrain.TrainDriverNetIDToSpecialTrain.ContainsKey(target.net.ID.Value);

            if (attackerIsSpecial && targetIsSpecial)
            {
                //specials don't target specials
                return true;
            }

            //none of them are special? outside of the scope of the plugin, then
            if (!(attackerIsSpecial || targetIsSpecial))
            {
                return null;
            }

            bool targetIsAlivePlayer = false;

            var alivePlayer = target as BasePlayer;

            if (alivePlayer != null)
            {
                targetIsAlivePlayer = !alivePlayer.IsNpc;
            }

            //at this point, the attacker can be special, the target can be special or the target is aliveplayer
            //second, who is the target? a tunnel dweller? then don't targe

            if (targetIsAlivePlayer)
            {
                //default rules
                return null;
            }
            else if (attackerIsSpecial)
            {
                //is the target a tunnel dweller?

                //not a special target, so let's see...
                if (!targetIsSpecial)
                {
                    if (target.PrefabName.Contains("tunneldweller"))
                    {
                        //let the targetting through
                        if (configData.ScientistsTargetTunnelDwellers)
                        {
                            //default targetting rules
                            return null;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else //nope it's not a tunnel dweller, so default targetting rules apply
                    {
                        return null;
                    }
                }
                else //special attacker AND special target, so don't let that happen
                {
                    return true;
                }
            }
            else //not a special attacker
            {
                if (targetIsSpecial)
                {
                    //something non special is trying to target something special.
                    //don't let it through
                    return true;
                }
            }

            //anything else: default targetting rules apply
            return null;
        }
        

        private void OnEntityKill(BaseEntity entity)
        {
            if (Instance == null) return;

            if (entity == null) return;

            if (entity.net == null) return;

            if (!SpecialTrain.WorkcartEntityNetIDToSpecialTrain.ContainsKey(entity.net.ID.Value)) return;

            //remove the entity from your stuff

            SpecialTrain.WorkcartEntityNetIDToSpecialTrain[entity.net.ID.Value].RemoveWorkcartEntity(entity.net.ID.Value);

        }

        private void OnEntityKill(TrainEngine train)
        {
            if (Instance == null) return;

            SpecialTrain.TrainNetIDToTrainEngine.Remove(train.net.ID.Value);


            if (IsTrainSpecial(train.net.ID.Value))
            {
                MakeTrainNormal(train.net.ID.Value);
            }

        }

        private void Unload()
        {
            if (Instance == null) return;

            foreach (var specialTrain in SpecialTrain.TrainNetIDToSpecialTrain.Values.ToList())
            {
                UnityEngine.Object.DestroyImmediate(specialTrain);
            }

            SpecialTrain.TrainDriverNetIDToSpecialTrain = null;

            SpecialTrain.TrainNetIDToSpecialTrain = null;
            SpecialTrain.TrainNetIDToTrainEngine = null;
            SpecialTrain.WorkcartEntityNetIDToSpecialTrain = null;

            SpecialTrain.PlatformTriggerToTrain = null;

            SpecialTrain.UserIDToSpecialTrains = null;

            if (EventTimer != null)
            {
                EventTimer.Destroy();
                EventTimer = null;
            }

            ConfigValues = null;

            Instance = null;
        }

        private void OnPlayerTruePVECleanup(BasePlayer player)
        {
            if (TruePVEIsLoaded)
            {
                if (SpecialTrain.UserIDToSpecialTrains.ContainsKey(player.userID))
                {
                    foreach (var train in SpecialTrain.UserIDToSpecialTrains[player.userID].ToList())
                    {
                        if (train != null)
                        {
                            if (train.PlayersInTruePVEBubble.ContainsKey(player.userID))
                            {
                                train.PlayersInTruePVEBubble.Remove(player.userID);
                            }
                        }
                    }

                    SpecialTrain.UserIDToSpecialTrains.Remove(player.userID);
                }
            }
        }

        private object OnDeathNotice(Dictionary<string, object> dic, string msg)
        {
            if (dic == null) return null;

            if (!dic.ContainsKey("HitInfo"))
            {
                return null;
            }

            var hit = dic["HitInfo"] as HitInfo;

            if (hit.InitiatorPlayer == null)
            {
                return null;
            }

            var victim = hit.HitEntity as BasePlayer;

            if (victim == null)
            {
                return null;
            }

            if (hit.InitiatorPlayer.displayName == Instance.configData.TrainDriverName)
            {
                return false;
            }

            if (victim.displayName == Instance.configData.TrainDriverName)
            {
                return false;
            }

            return null;
        }

        private object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (Instance == null) return null;

            OnPlayerTruePVECleanup(player);

            //ignore non-drivers here
            if (!SpecialTrain.TrainDriverNetIDToSpecialTrain.ContainsKey(player.net.ID.Value))
            {
                return null;
            }

            return SpecialTrain.TrainDriverNetIDToSpecialTrain[player.net.ID.Value].OnTrainDriverKilled(info);
        }

        private void OnPlayerDisconnect(BasePlayer player)
        {
            if (Instance == null) return;

            OnPlayerTruePVECleanup(player);
        }

        private object OnEntityTakeDamage(BaseCombatEntity combatEntity, HitInfo info)
        {
            if (Instance == null) return null;
            if (combatEntity == null) return null;
            if (combatEntity.net == null) return null;
            if (info == null) return null;

            if (SpecialTrain.WorkcartEntityNetIDToSpecialTrain.ContainsKey(combatEntity.net.ID.Value))
            {
                return SpecialTrain.WorkcartEntityNetIDToSpecialTrain[combatEntity.net.ID.Value].OnTrainEntityTakeDamage(combatEntity, info);
            }

            if (SpecialTrain.TrainDriverNetIDToSpecialTrain.ContainsKey(combatEntity.net.ID.Value))
            {
                return SpecialTrain.TrainDriverNetIDToSpecialTrain[combatEntity.net.ID.Value].OnTrainEntityTakeDamage(combatEntity, info);
            }

            //don't let them damage splats
            if (combatEntity.PrefabName == PREFAB_RUG && combatEntity.skinID == SKIN_SPLAT1)
            {
                return true;
            }

            return null;
        }

        private object OnEntityTakeDamage(TrainCar train, HitInfo info)
        {
            if (Instance == null) return null;
            if (SpecialTrain.TrainNetIDToSpecialTrain.ContainsKey(train.net.ID.Value))
            {
                return SpecialTrain.TrainNetIDToSpecialTrain[train.net.ID.Value].OnTrainEntityTakeDamage(train, info);
            }

            return null;
        }
        //broken from the December 2021 forced wipe
        /*
        private object OnFuelAbstract(EntityFuelSystem fuelSystem, object returnOnSuccess)
        {
            if (Instance == null) return null;

            if (fuelSystem.owner.OwnerID != INTERNAL_OWNERID) return null;

            if (!SpecialTrain.TrainNetIDToSpecialTrain.ContainsKey(fuelSystem.owner.net.ID.Value))
            {
                return null;
            }

            //no infinite fuel if the driver is dead

            if (!SpecialTrain.TrainNetIDToSpecialTrain[fuelSystem.owner.net.ID.Value].DriverIsAlive)
            {
                return null;
            }

            return returnOnSuccess;
        }

        private object OnFuelCheck(EntityFuelSystem fuelSystem) => OnFuelAbstract(fuelSystem, true);

        private object CanUseFuel(EntityFuelSystem fuelSystem, StorageContainer container, float seconds, float fuelUsedPerSecond) => OnFuelCheck(fuelSystem);

        object OnFuelAmountCheck(EntityFuelSystem fuelSystem, Item item) => OnFuelAbstract(fuelSystem, 1337);
        */
        #endregion

        #region PERMISSION
        public const string PERM_ADMIN = "cargotrainevent.admin";
        #endregion

        #region LANG
        public const string MSG_FUEL_MANAGER_PRESENT = "MSG_FUEL_MANAGER_PRESENT";
        public const string MSG_FUEL_MANAGER_NOT_PRESENT = "MSG_FUEL_MANAGER_NOT_PRESENT";
        public const string MSG_CHAT_PREFIX = "MSG_CHAT_PREFIX";
        public const string MSG_EVENT_SCHEDULED_IN = "MSG_EVENT_SCHEDULED_IN";
        public const string MSG_EVENT_SCHEDULE_DISABLED = "MSG_EVENT_SCHEDULE_DISABLED";
        public const string MSG_EVENT_STARTED = "MSG_EVENT_STARTED";
        public const string MSG_EVENT_UPDATE_POSITION = "MSG_EVENT_UPDATE_POSITION";
        public const string MSG_EVENT_OVER = "MSG_EVENT_OVER";
        public const string MSG_EVENT_DRIVER_DEAD = "MSG_EVENT_DRIVER_DEAD";
        public const string MSG_EVENT_CRATE_BEING_HACKED = "MSG_EVENT_CRATE_BEING_HACKED";
        public const string MSG_EVENT_SELF_DESTRUCT_INITIATED = "MSG_EVENT_SELF_DESTRUCT_INITIATED";
        public const string MSG_FORMAT_TIME_BOTH = "MSG_FORMAT_TIME_BOTH";
        public const string MSG_FORMAT_TIME_SEC = "MSG_FORMAT_TIME_SEC";
        public const string MSG_FORMAT_TIME_MIN = "MSG_FORMAT_TIME_MIN";
        public const string MSG_AVAILABLE_CAMERAS = "MSG_AVAILABLE_CAMERAS";
        public const string MSG_AVAILABLE_CAMERAS2 = "MSG_AVAILABLE_CAMERAS2";

        public const string MSG_VALUE_HAS_BEEN_SET = "MSG_VALUE_HAS_BEEN_SET";

        private const string MSG_CFG_DEFAULT = "MSG_CFG_DEFAULT";
        private const string MSG_CFG_RUNDOWN_FORMAT = "MSG_CFG_RUNDOWN_FORMAT";
        private const string MSG_CFG_DETAILS_FORMAT = "MSG_CFG_DETAILS_FORMAT";
        private const string MSG_CFG_NO_SETTING_FOUND = "MSG_CFG_NO_SETTING_FOUND";

        private Dictionary<string, string> LangMessages = new Dictionary<string, string>
        {
            [MSG_FUEL_MANAGER_PRESENT] = "Fuel Manager v. {0} loaded; handing over fuel handling",
            [MSG_FUEL_MANAGER_NOT_PRESENT] = "Fuel Manager not loaded, using default handling",
            [MSG_CHAT_PREFIX] = "<color=red>[Cargo Train Event]</color>",
            [MSG_EVENT_SCHEDULE_DISABLED] = "Not scheduling the next Cargo Train Event - the built-in event timer is disabled. You will need to run the command manually.",
            [MSG_EVENT_SCHEDULED_IN] = "The next Cargo Train Event has been scheduled to run {0} seconds (= {1} minutes) from now.",
            [MSG_EVENT_STARTED] = "Cargo Workcart inbound at <color=yellow>{0}</color>!",
            [MSG_EVENT_UPDATE_POSITION] = "Cargo Workcart is currently at <color=yellow>{0}</color>!",
            [MSG_EVENT_OVER] = "The Cargo Workcart event is over!",
            [MSG_EVENT_DRIVER_DEAD] = "The original driver of the Cargo Workcart has been taken out!",
            [MSG_EVENT_CRATE_BEING_HACKED] = "Somebody started hacking the locked crate on the Cargo Workcart!",
            [MSG_EVENT_SELF_DESTRUCT_INITIATED] = "The Cargo Workcart will self-destruct in {0}!",
            [MSG_FORMAT_TIME_BOTH] = "{0} m {1} s",
            [MSG_FORMAT_TIME_SEC] = "{0} s",
            [MSG_FORMAT_TIME_MIN] = "{0} m",
            [MSG_AVAILABLE_CAMERAS] = "The following CCTV identifiers have been assigned:{0}",
            [MSG_AVAILABLE_CAMERAS2] = "Event CCTVS: ",

            [MSG_VALUE_HAS_BEEN_SET] = "Config value <color=yellow>{0}</color> has been set to {1} by an admin.",
            [MSG_CFG_DEFAULT] = "Here's the current settings. Type <color=yellow>/te_cfg settingName</color> with no parameters to see the description, the current value and what the accepted arguments are. Type <color=yellow>/te_cfg settingName acceptedValue</color> to change the setting.",
            [MSG_CFG_RUNDOWN_FORMAT] = "/te_cfg <color=yellow>{0}</color> (currently: {1})",
            [MSG_CFG_DETAILS_FORMAT] = "<color=green>{0}</color>:\n{1} ({2})\nThis value is currently set to: {3}\n",
            [MSG_CFG_NO_SETTING_FOUND] = "No setting with that name has been found. Type /te_cfg to get a rundown.",
        };

        private static object ReusableObject;
        private static bool ReusableBool;
        private static string ReusableString;
        private static float ReusableFloat;

        private static bool ReusableBool2;

        private static string ReusableString2;
        private static float ReusableFloat2;
        private static int ReusableInt2;
        private static ulong ReusableUlong2;

        [PluginReference]
        private Plugin Notify;

        private void TellMessage(BasePlayer player, string message, bool alsoPrintWarning = false)
        {
            ReusableString2 = $"{MSG(MSG_CHAT_PREFIX)} {message}";

            if (player == null)
            {
                if (Instance.configData.UseChatMessages)
                {
                    Instance.PrintToChat(ReusableString2);
                }

                if (Instance.configData.UseNotifyPlugin)
                {
                    foreach (var playah in BasePlayer.activePlayerList)
                    {
                        if (!playah.IsConnected)
                        {
                            continue;
                        }

                        Notify?.Call("SendNotify", playah, (int)0, ReusableString2);
                    }
                }

                if (Instance.configData.UseGuiAnnouncements)
                {
                    GUIAnnouncements?.Call("CreateAnnouncement", ReusableString2, "Grey", "White", null, 0f, false, false, null, false);
                }

                if (alsoPrintWarning)
                {
                    Instance.PrintWarning(Instance.StripTags(ReusableString2));
                }
            }
            else
            {
                if (Instance.configData.UseChatMessages)
                {
                    player.ChatMessage(ReusableString2);
                }

                if (Instance.configData.UseNotifyPlugin)
                {
                    Notify?.Call("SendNotify", player, (int)0, ReusableString2);
                }

                if (Instance.configData.UseGuiAnnouncements)
                {
                    GUIAnnouncements?.Call("CreateAnnouncement", ReusableString2, "0 0 0 0", "1 1 0 1", player, 0f, false, false, null, false);
                }

                if (alsoPrintWarning)
                {
                    Instance.PrintWarning(Instance.StripTags($"{player.displayName} has been told: {ReusableString2}"));
                }
            }


        }


        private static string MSG(string msg, string userID = null, params object[] args)
        {
            if (args == null)
            {
                return Instance.lang.GetMessage(msg, Instance, userID);
            }
            else
            {
                return string.Format(Instance.lang.GetMessage(msg, Instance, userID), args);
            }

        }
        #endregion

        #region CONFIG
        public class ColorCode
        {
            public string hexValue;
            public UnityEngine.Color rustValue;
            public string rustString;
            public ColorCode(string hex)
            {
                hexValue = hex.ToUpper();

                if (hex.StartsWith("#"))
                {
                    hex = hex.Substring(1);
                }

                if (!hexValue.StartsWith("#"))
                {
                    hexValue = "#" + hexValue;
                }

                //extract the R, G, B
                float r=255F, g=0, b=0;

                try
                {
                    r = (float)short.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255;
                    g = (float)short.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255;
                    b = (float)short.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255;
                }
                catch
                {
                    Instance.PrintError($"ERROR: {hexValue} doesn't appear to be a valid colour! Defaulting to red.");
                    r = 255F;
                    g = 0;
                    b = 0;
                }

                rustValue = new UnityEngine.Color(r, g, b);
                rustString = $"{r} {g} {b}";
            }
        }

        public class InteractiveConfigValue
        {
            private Func<object> getter;
            private Action<object> setter;

            private Func<object, bool> validator;
            public Func<object, string> formatter;
            private Func<object, object> parser;

            public Type valueType;

            private double lowerLimit;
            private double upperLimit;

            //name also identifies it in the dic
            public string name;
            public string description;

            public InteractiveConfigValue(string name, string description, Func<object> getter, Action<object> setter, Type validatorType = null, double lowerLimit = double.NegativeInfinity, double upperLimit = double.PositiveInfinity)
            {
                this.getter = getter;
                this.setter = setter;
                this.name = name;
                this.description = description;
                this.lowerLimit = lowerLimit;
                this.upperLimit = upperLimit;

                formatter = FormatDefault;

                if (validatorType == null || validatorType == typeof(bool))
                {
                    validator = ValidateBool;
                    parser = ParseBool;
                    formatter = FormatBool;
                }
                else if (validatorType == typeof(float))
                {
                    validator = ValidateFloat;
                    parser = ParseFloat;
                }
                else if (validatorType == typeof(int))
                {
                    validator = ValidateInt;
                    parser = ParseInt;
                }
                else if (validatorType == typeof(ulong))
                {
                    validator = ValidateUlong;
                    parser = ParseUlong;
                }
                else if (validatorType == typeof(string))
                {
                    validator = ValidateString;
                    parser = ParseString;
                    formatter = FormatString;
                }

                valueType = validatorType;
            }

            public object GetSet
            {
                get
                {
                    return getter();
                }
                set
                {
                    //validate first
                    if (validator(value))
                    {
                        ReusableObject = parser(value);
                        setter(ReusableObject);
                        //tell the players

                        Instance.TellMessage(null, MSG(MSG_VALUE_HAS_BEEN_SET, null, name, formatter(ReusableObject)));
                        Instance.SaveConfigData();

                    }
                    else
                    {
                        ReusableString = upperLimit != double.PositiveInfinity || lowerLimit != double.NegativeInfinity ? $"The value for {name} is either too low or too high. Try {FormatNumericLimits()}" : $"Incorrect value for {name}. You need to enter a {FormatNumericLimits()}";

                        Instance.TellMessage(null, $"{ReusableString} \nThe value remains as {formatter(getter())}.");
                    }

                }
            }

            public string FormatAcceptable()
            {
                return valueType == typeof(bool) ? "logical values (<color=green>true</color> or <color=red>false</color>)" : valueType == typeof(string) ? FormatStrings() : FormatNumericLimits();
            }

            private string FormatNumericLimits()
            {
                ReusableString2 = valueType == typeof(float) ? "<color=green>fractions (like 1.2345)</color>" : "<color=purple>integers (like 12345)</color>";

                return $"{ReusableString2} between <color=red>{lowerLimit.ToString("0.00")}</color> and <color=blue>{upperLimit.ToString("0.00")}";
            }

            private string FormatStrings()
            {
                return "strings (like ThisIsAString) - if they have a space, in quotes (like \"This Is A String\")";
            }

            private string FormatString(object value)
            {
                return $"<color=yellow>{value?.ToString() ?? "NULL"}</color>";
            }

            private string FormatBool(object value)
            {
                return value.Equals(true) ? "<color=green>true</color>" : "<color=red>false</color>";
            }

            private object ParseBool(object value)
            {
                ReusableString2 = value.ToString().ToLower();

                ReusableBool2 = !(ReusableString2.Contains("f") || ReusableString2.Contains("0") || ReusableString2.Contains("no"));

                return ReusableBool2;
            }
            private bool ValidateBool(object value)
            {
                //whatever it is, it can be always treated as bool
                return true;
            }

            private object ParseFloat(object value)
            {
                //it's already in ReusableFloat! neat, huh.
                //since we're using it, it must've been validated, so it's still there eh.
                return ReusableFloat2;
            }

            private object ParseString(object value)
            {
                return ReusableString;
            }

            private object ParseInt(object value)
            {
                return ReusableInt2;
            }

            private object ParseUlong(object value)
            {
                return ReusableUlong2;
            }

            private bool ValidateFloat(object value)
            {
                if (float.TryParse(value.ToString(), out ReusableFloat2))
                {
                    return (ReusableFloat2 >= lowerLimit && ReusableFloat2 <= upperLimit);
                }
                else return false;
            }

            private bool ValidateInt(object value)
            {
                if (int.TryParse(value.ToString(), out ReusableInt2))
                {
                    return (ReusableInt2 >= lowerLimit && ReusableInt2 <= upperLimit);
                }
                else return false;
            }

            private bool ValidateUlong(object value)
            {
                if (ulong.TryParse(value.ToString(), out ReusableUlong2))
                {
                    return (ReusableUlong2 >= lowerLimit && ReusableUlong2 <= upperLimit);
                }
                else return false;
            }

            private bool ValidateString(object value)
            {
                ReusableString = value.ToString();

                if (ReusableString.ToLower() == "null")
                {
                    ReusableString = null;
                }
                return true;
            }

            private string FormatDefault(object value)
            {
                return $"<color=#00FFFF>{value.ToString()}</color>";
            }

        }

        private static Dictionary<string, InteractiveConfigValue> ConfigValues = null;

        private static void AddInteractiveConfigValue(string name, string description, Func<object> getter, Action<object> setter, Type validator = null, double lowerLimit = double.NegativeInfinity, double upperLimit = double.PositiveInfinity)
        {
            ConfigValues.Add(name, new InteractiveConfigValue(name, description, getter, setter, validator, lowerLimit, upperLimit));
        }

        
        private static void GenerateAllInteractiveConfigValues()
        {
            AddInteractiveConfigValue("EnableRandomEvent", $"If true, enable random Cargo Train Event with random timers based on your config. If false, you will have to run the console command \"trainevent\" manually", () => Instance.configData.EnableRandomEvent, val => { Instance.configData.EnableRandomEvent = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("EventDuration", $"How long the Cargo Train Event is going to last before initiating self-destruction timer (in seconds)", () => Instance.configData.EventDuration, val => { Instance.configData.EventDuration = (float)val; }, typeof(float), 1F, 10000F);

            AddInteractiveConfigValue("EventRandomTimerMin", $"Minimum time to wait before the next Cargo Train Event (in seconds)", () => Instance.configData.EventRandomTimerMin, val => { Instance.configData.EventRandomTimerMin = (float)val; }, typeof(float), 1F, 10000F);

            AddInteractiveConfigValue("EventRandomTimerMax", $"Maximum time to wait before the next Cargo Train Event (in seconds)", () => Instance.configData.EventRandomTimerMax, val => { Instance.configData.EventRandomTimerMax = (float)val; }, typeof(float), 1F, 10000F);

            AddInteractiveConfigValue("EventTrainAltitudeMin", $"Only idle carts with Y-position greater than this value (higher altitude) will be considered for the event. Use for no lower bound.", () => Instance.configData.EventTrainAltitudeMin, val => { Instance.configData.EventTrainAltitudeMin = (float)val; }, typeof(float), -10000F, 10000F);

            AddInteractiveConfigValue("EventTrainAltitudeMax", $"Only idle carts with Y-position lesser than this value (lower altitude) will be considered for the event. Use large values for no upper bound.", () => Instance.configData.EventTrainAltitudeMax, val => { Instance.configData.EventTrainAltitudeMax = (float)val; }, typeof(float), -10000F, 10000F);

            AddInteractiveConfigValue("SelfDestructTimer", $"The length of the self-destruction timer (in seconds)", () => Instance.configData.SelfDestructTimer, val => { Instance.configData.SelfDestructTimer = (float)val; }, typeof(float), 1F, 10000F);

            AddInteractiveConfigValue("SelfDestructDamageRadius", $"The radius of the self-destruction explosion (in meters)", () => Instance.configData.SelfDestructDamageRadius, val => { Instance.configData.SelfDestructDamageRadius = (float)val; }, typeof(float), 1F, 10000F);

            AddInteractiveConfigValue("SelfDestructDamageAmount", $"The amount of damage taken by entities within explosion radius (in HP)", () => Instance.configData.SelfDestructDamageAmount, val => { Instance.configData.SelfDestructDamageAmount = (float)val; }, typeof(float), 1F, 10000F);

            AddInteractiveConfigValue("HackingAddsSeconds", $"When a locked crate on the Train is hacked and the train is not self-destructing yet, postpone the self-destruct timer by this many seconds - or 0 to disable. 900 seconds (15 minut) is the normal duration of the crate.", () => Instance.configData.HackingAddsSeconds, val => { Instance.configData.HackingAddsSeconds = (float)val; }, typeof(float), 0F, 10000);

            AddInteractiveConfigValue("WhenAttackedStopFor", $"When the train (or any entity belonging it) is attacked, it will brake and wait for this amount of seconds before continuing along the track", () => Instance.configData.WhenAttackedStopFor, val => { Instance.configData.WhenAttackedStopFor = (float)val; }, typeof(float), 0F, 3600F);


            AddInteractiveConfigValue("DontStopOnTunnelDwellerAttack", $"If true, tunnel dwellers shooting at the Cargo Train won't make the NPC Driver stop", () => Instance.configData.DontStopOnTunnelDwellerAttack, val => { Instance.configData.DontStopOnTunnelDwellerAttack = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("ScientistsTargetTunnelDwellers", $"If true, scientists will actively target and shoot Tunnel Dwellers in their range", () => Instance.configData.ScientistsTargetTunnelDwellers, val => { Instance.configData.ScientistsTargetTunnelDwellers = val.Equals(true); }, typeof(bool));
            

            AddInteractiveConfigValue("TrainTopSpeed", $"The top speed of the Train (in meters/second), Rust default is 12", () => Instance.configData.TrainTopSpeed, val => { Instance.configData.TrainTopSpeed = (float)val; }, typeof(float), 1F, 60F);

            AddInteractiveConfigValue("DeadManSwitchStop", $"If true, when the Train Driver NPC is killed, the train comes to a stop", () => Instance.configData.DeadManSwitchStop, val => { Instance.configData.DeadManSwitchStop = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("CandidateHasToBeAtStation", $"If true, when deciding which idle train to man for the event, only consider trains flagged by Rust as \"At station\".", () => Instance.configData.CandidateHasToBeAtStation, val => { Instance.configData.CandidateHasToBeAtStation = val.Equals(true); }, typeof(bool));


            AddInteractiveConfigValue("TrainDriverKit", $"The name of the custom kit for the Train Driver - or NULL to restore default (Heavy Scientist suit)", () => Instance.configData.TrainDriverKit, val => { Instance.configData.TrainDriverKit = (string)val; }, typeof(string));


            AddInteractiveConfigValue("TrainScientistKit", $"The name of the custom kit for the Scientists - or NULL for no custom kit (so default blue hazmat + LR)", () => Instance.configData.TrainScientistKit, val => { Instance.configData.TrainScientistKit = (string)val; }, typeof(string));

            /*
            AddInteractiveConfigValue("TrainDriverName", $"The display name of the Train Driver - or NULL for no name", () => Instance.configData.TrainDriverName, val => { Instance.configData.TrainDriverName = (string)val; }, typeof(string));
            */

            AddInteractiveConfigValue("RadiationEnable", $"If true, the train will have a radiation bubble around it at all times, according to the RadiationTier and RadiationRadius", () => Instance.configData.RadiationEnable, val => { Instance.configData.RadiationEnable = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("RadiationRadius", $"If radiation is enabled, this is how far the bubble will reach from the centre of the train (in meters)", () => Instance.configData.RadiationRadius, val => { Instance.configData.RadiationRadius = (float)val; }, typeof(float), 0F, 3600F);

            AddInteractiveConfigValue("RadiationTier", $"Radiation tier, from 0 (minimal) to 3 (high)", () => Instance.configData.RadiationTier, val => { Instance.configData.RadiationTier = (int)val; }, typeof(int), 0, 3);

            AddInteractiveConfigValue("SplatsEnable", $"If true, the train will \"leak\" acid-green splats on the train track when the Train is on the move (acting as highly visible \"breadcrumbs\" to track the train down). Each one of them will despawn at random in the timeframe defined by SplatsLifetimeMin and SplatsLifetimeMax or when the plugin is reloaded.", () => Instance.configData.SplatsEnable, val => { Instance.configData.SplatsEnable = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("SplatsPerSecond", $"The frequency of splatting (on average); the higher it is (up to 3 splats pers second), the more splats it produces. WARNING: More than 1 is not recommended due to possible performance drop.", () => Instance.configData.SplatsPerSecond, val => { Instance.configData.SplatsPerSecond = (float)val; }, typeof(float), 0.01F, 3F);

            AddInteractiveConfigValue("SplatsLifetimeMin", $"The minimum time before a splat despawns (in seconds)", () => Instance.configData.SplatsLifetimeMin, val => { Instance.configData.SplatsLifetimeMin = (float)val; }, typeof(float), 1F, 1000F);

            AddInteractiveConfigValue("SplatsLifetimeMax", $"The maximum time before a splat despawns (in seconds)", () => Instance.configData.SplatsLifetimeMax, val => { Instance.configData.SplatsLifetimeMax = (float)val; }, typeof(float), 1F, 1000F);

            AddInteractiveConfigValue("NotifyWhenStarted", $"If true, when the event starts, a global message will appear in the chat, according to the format specified in your lang JSON", () => Instance.configData.NotifyWhenStarted, val => { Instance.configData.NotifyWhenStarted = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("NotifyWhenEnded", $"If true, when the event ends, a global message will appear in the chat, according to the format specified in your lang JSON", () => Instance.configData.NotifyWhenEnded, val => { Instance.configData.NotifyWhenEnded = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("NotifyAboutSelfDestruct", $"If true, when train initiates a self-destruct sequence, a global message will appear in the chat, according to the format specified in your lang JSON", () => Instance.configData.NotifyAboutSelfDestruct, val => { Instance.configData.NotifyAboutSelfDestruct = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("NotifyAboutCrateHacking", $"If true, when one of the locked crates on the train starts being hacked, a global message will appear in the chat, according to the format specified in your lang JSON", () => Instance.configData.NotifyAboutCrateHacking, val => { Instance.configData.NotifyAboutCrateHacking = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("NotifyAboutDriverDeath", $"If true, when the Train Driver NPC is killed, a global message will appear in the chat, according to the format specified in your lang JSON", () => Instance.configData.NotifyAboutDriverDeath, val => { Instance.configData.NotifyAboutDriverDeath = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("NotifyAboutPosition", $"If true, the train will announce its current position on the grid as a global message in the chat, according to the format specified in your lang JSON", () => Instance.configData.NotifyAboutPosition, val => { Instance.configData.NotifyAboutPosition = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("NotifyAboutPositionEvery", $"How often to announce the train's grid position (in seconds)", () => Instance.configData.NotifyAboutPositionEvery, val => { Instance.configData.NotifyAboutPositionEvery = (float)val; }, typeof(float), 1F, 1000F);

            AddInteractiveConfigValue("AttachTimeCounters", $"If true, the train will come equipped with power counters to display the remaining minutes and seconds before self destruction initiation (or when already initiated, minutes and seconds before actual self destruction). The position/rotation of the counters can be set with the 12 values below. ", () => Instance.configData.AttachTimeCounters, val => { Instance.configData.AttachTimeCounters = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("CamerasAreFunctional", $"If true, any cameras attached to the train will be powered and assigned an ID for the event. If false, they will only be used for decoration ", () => Instance.configData.CamerasAreFunctional, val => { Instance.configData.CamerasAreFunctional = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("CounterMinutePosX", $"If counters are enabled, this is the position X of the minute counter, relative to the Train", () => Instance.configData.CounterMinutePosX, val => { Instance.configData.CounterMinutePosX = (float)val; }, typeof(float));

            AddInteractiveConfigValue("CounterMinutePosY", $"If counters are enabled, this is the position Y of the minute counter, relative to the Train", () => Instance.configData.CounterMinutePosY, val => { Instance.configData.CounterMinutePosY = (float)val; }, typeof(float));

            AddInteractiveConfigValue("CounterMinutePosZ", $"If counters are enabled, this is the position Z of the minute counter, relative to the Train", () => Instance.configData.CounterMinutePosZ, val => { Instance.configData.CounterMinutePosX = (float)val; }, typeof(float));

            AddInteractiveConfigValue("CounterMinuteRotX", $"If counters are enabled, this is the rotation X of the minute counter, relative to the Train", () => Instance.configData.CounterMinuteRotX, val => { Instance.configData.CounterMinuteRotX = (float)val; }, typeof(float));

            AddInteractiveConfigValue("CounterMinuteRotY", $"If counters are enabled, this is the rotation Y of the minute counter, relative to the Train", () => Instance.configData.CounterMinuteRotY, val => { Instance.configData.CounterMinuteRotY = (float)val; }, typeof(float));

            AddInteractiveConfigValue("CounterMinuteRotZ", $"If counters are enabled, this is the rotation Z of the minute counter, relative to the Train", () => Instance.configData.CounterMinuteRotZ, val => { Instance.configData.CounterMinuteRotZ = (float)val; }, typeof(float));

            AddInteractiveConfigValue("CounterSecondPosX", $"If counters are enabled, this is the position X of the second counter, relative to the Train", () => Instance.configData.CounterSecondPosX, val => { Instance.configData.CounterSecondPosX = (float)val; }, typeof(float));

            AddInteractiveConfigValue("CounterSecondPosY", $"If counters are enabled, this is the position Y of the second counter, relative to the Train", () => Instance.configData.CounterSecondPosY, val => { Instance.configData.CounterSecondPosY = (float)val; }, typeof(float));

            AddInteractiveConfigValue("CounterSecondPosZ", $"If counters are enabled, this is the position Z of the second counter, relative to the Train", () => Instance.configData.CounterSecondPosZ, val => { Instance.configData.CounterSecondPosX = (float)val; }, typeof(float));

            AddInteractiveConfigValue("CounterSecondRotX", $"If counters are enabled, this is the rotation X of the second counter, relative to the Train", () => Instance.configData.CounterSecondRotX, val => { Instance.configData.CounterSecondRotX = (float)val; }, typeof(float));

            AddInteractiveConfigValue("CounterSecondRotY", $"If counters are enabled, this is the rotation Y of the second counter, relative to the Train", () => Instance.configData.CounterSecondRotY, val => { Instance.configData.CounterSecondRotY = (float)val; }, typeof(float));

            AddInteractiveConfigValue("CounterSecondRotZ", $"If counters are enabled, this is the rotation Z of the second counter, relative to the Train", () => Instance.configData.CounterSecondRotZ, val => { Instance.configData.CounterSecondRotZ = (float)val; }, typeof(float));

            AddInteractiveConfigValue("TruePVERadius", $"If TruePVE is loaded, this is the radius of the the PVP zone around the train. Set to 0F to disable PVP zone. If TruePVE is not loaded, this setting has no effect.", () => Instance.configData.TruePVERadius, val => { Instance.configData.TruePVERadius = (float)val; }, typeof(float), 0, 1000);

            AddInteractiveConfigValue("ScientistHealthMultiplier", $"Setting this value to 1 means the Scientists will have the normal expected amount of health (175 HP). Setting it to 0.5 will halve it, setting it to 2 will double it, etc.", () => Instance.configData.ScientistHealthMultiplier, val => { Instance.configData.ScientistHealthMultiplier = (float)val; }, typeof(float), 0.0001F, 100F);

            AddInteractiveConfigValue("MapMarkerEnable", $"If true, Cargo Workcarts will have map markers following them - even if there's no crates on the cart. Use the values below to define the look of the marker. It will take effect on the next event.", () => Instance.configData.MapMarkerEnable, val => { Instance.configData.MapMarkerEnable = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("MapMarkerColor1", $"Color 1 for the map marker, in the hexadecimal format (#000000 being black, #ffffff being white)", () => Instance.configData.MapMarkerColor1, val => { Instance.configData.MapMarkerColor1 = (string)val;}, typeof(string));

            AddInteractiveConfigValue("MapMarkerColor2", $"Color 2 for the map marker, in the hexadecimal format (#000000 being black, #ffffff being white)", () => Instance.configData.MapMarkerColor2, val => { Instance.configData.MapMarkerColor2 = (string)val; }, typeof(string));

            AddInteractiveConfigValue("MapMarkerAlpha", $"Transparency for the map marker (0 = fully transparent, 1 = fully opaque)", () => Instance.configData.MapMarkerAlpha, val => { Instance.configData.MapMarkerAlpha = (float)val;}, typeof(float), 0F, 1F);

            AddInteractiveConfigValue("MapMarkerRadius", $"Radius for the map marker, in meters", () => Instance.configData.MapMarkerRadius, val => { Instance.configData.MapMarkerRadius = (float)val; }, typeof(float), 0F, 100000F);

            AddInteractiveConfigValue("UseNotifyPlugin", $"If true, plugin-related chat notifications for all players will be sent using Mevent's Notify plugin. Can be used in conjunction with UseGUIAnnouncementsPlugin and UseChatMessages.", () => Instance.configData.UseNotifyPlugin, val => { Instance.configData.UseNotifyPlugin = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("UseGUIAnnouncementsPlugin", $"If true, plugin-related chat notifications for all players will be sent using the Gui Announcements plugin. Can be used in conjunction with UseChatMessages and UseNotifyPlugin.", () => Instance.configData.UseGuiAnnouncements, val => { Instance.configData.UseGuiAnnouncements = val.Equals(true); }, typeof(bool));

            AddInteractiveConfigValue("UseChatMessages", $"If true, plugin-related chat notifications for all players will be sent to the in-game chat. Can be used in conjunction with UseGUIAnnouncementsPlugin and UseNotifyPlugin.", () => Instance.configData.UseChatMessages, val => { Instance.configData.UseChatMessages = val.Equals(true); }, typeof(bool));
        }
        
        public class WorkcartEntityDefinition
        {
            public int ID;
            public string PrefabName;
            public float SpawnChance;
            public float HealthMultiplier;
            public bool Indestructible;
            public bool PickupEnabled;
            public bool IsLocked;
            public bool PowerUpImmediately;

            //will only consider spawning (following its chance) if all of the IDs in question are to be spawned
            public List<int> ReliesOnSpawningIDs;

            //will only consider spawning (following its chance) if none of the IDs in question are to be spawned
            public List<int> ConflictsWithSpawningIDs;

            //before actually doing anything, those two above need to be checked for conflicts

            public float LocalPosX;
            public float LocalPosY;
            public float LocalPosZ;

            public float LocalRotX;
            public float LocalRotY;
            public float LocalRotZ;

            public ulong SkinID;
        }
       
        public class ConfigData
        {
            public string Version = VERSION;

            public bool EnableRandomEvent = true; //if false, you gotta run the trainevent command manually

            public float EventDuration = 1800F; // 1800F; //half an hour

            public float EventRandomTimerMin = 3600F; //1 hour min
            public float EventRandomTimerMax = 7200F; //2 hours max

            public float EventTrainAltitudeMin = -10000F;
            public float EventTrainAltitudeMax = 10000F;

            public float SelfDestructTimer = 60F; //after the main timer is done, this is the final countdown
            public float SelfDestructDamageAmount = 2000F;
            public float SelfDestructDamageRadius = 12F;

            public float TrainTopSpeed = 10F; //12 is rust default
            public bool DeadManSwitchStop = true; //start braking the train when the NPC driver is dead

            public bool CandidateHasToBeAtStation = true;
            public string TrainDriverKit = null; //if null, default heavy suit will be used
            public string TrainDriverName = "Train Driver";

            public string TrainScientistKit = null;

            public float HackingAddsSeconds = 0F; //5 minutes

            public float WhenAttackedStopFor = 10F;
            public bool DontStopOnTunnelDwellerAttack = true;
            public bool ScientistsTargetTunnelDwellers = false;

            public bool RadiationEnable = true;
            public int RadiationTier = 2;
            public float RadiationRadius = 10F;

            public bool SplatsEnable = true;
            public float SplatsPerSecond = 1F;
            public float SplatsLifetimeMin = 60F;
            public float SplatsLifetimeMax = 300F;

            public bool NotifyWhenStarted = true;
            public bool NotifyWhenEnded = true;
            public bool NotifyAboutPosition = true;
            public float NotifyAboutPositionEvery = 180F;
            public bool NotifyAboutDriverDeath = true;
            public bool NotifyAboutCrateHacking = true;
            public bool NotifyAboutSelfDestruct = true;

            public bool AttachTimeCounters = true;
            public bool CamerasAreFunctional = true;

            public float CounterMinutePosX = 0.572F;
            public float CounterMinutePosY = 3.515F;
            public float CounterMinutePosZ = 4.357F;
            public float CounterMinuteRotX = 18.428F;
            public float CounterMinuteRotY = 180F;
            public float CounterMinuteRotZ = 0F;

            public float CounterSecondPosX = 0.750F;
            public float CounterSecondPosY = 3.515F;
            public float CounterSecondPosZ = 4.357F;
            public float CounterSecondRotX = 18.428F;
            public float CounterSecondRotY = 180F;
            public float CounterSecondRotZ = 0F;

            public bool MapMarkerEnable = true;

            public float MapMarkerUpdateRate = 1F;

            public float MapMarkerRadius = 0.5F;

            public string MapMarkerColor1 = "#ff0000";

            public string MapMarkerColor2 = "#ff0000";

            public float MapMarkerAlpha = 0.5F;

            public float TruePVERadius = 100F;

            public float ScientistHealthMultiplier = 1F;

            public bool UseNotifyPlugin = true;

            public bool UseChatMessages = true;

            public bool UseGuiAnnouncements = true;

            public Dictionary<int, WorkcartEntityDefinition> WorkcartEntities = new Dictionary<int, WorkcartEntityDefinition>();
        }

        public ConfigData configData;

        public const int ENTITY_1ST_LOCKED_CRATE = 0;
        public const int ENTITY_2ND_LOCKED_CRATE = 1;
        public const int ENTITY_3RD_LOCKED_CRATE = 2;

        public const int ENTITY_1ST_SCIENTIST = 3;
        public const int ENTITY_2ND_SCIENTIST = 4;
        public const int ENTITY_3RD_SCIENTIST = 5;

        public const int ENTITY_4TH_SCIENTIST = 6;
        public const int ENTITY_5TH_SCIENTIST = 7;
        public const int ENTITY_6TH_SCIENTIST = 8;
        public const int ENTITY_7TH_SCIENTIST = 9;
        public const int ENTITY_8TH_SCIENTIST = 10;
        public const int ENTITY_9TH_SCIENTIST = 11;
        public const int ENTITY_10TH_SCIENTIST = 12;
        public const int ENTITY_11TH_SCIENTIST = 13;
        public const int ENTITY_12TH_SCIENTIST = 14;

        public const int ENTITY_1ST_ELITE_CRATE = 15;
        public const int ENTITY_2ND_ELITE_CRATE = 16;
        public const int ENTITY_3RD_ELITE_CRATE = 17;

        public const int ENTITY_1ST_BRADLEY_CRATE = 18;
        public const int ENTITY_2ND_BRADLEY_CRATE = 19;
        public const int ENTITY_3RD_BRADLEY_CRATE = 20;

        public const int ENTITY_1ST_HELI_CRATE = 21;
        public const int ENTITY_2ND_HELI_CRATE = 22;
        public const int ENTITY_3RD_HELI_CRATE = 23;
        public const int ENTITY_4TH_HELI_CRATE = 24;

        public const int ENTITY_INTERIOR_SIREN_LIGHT = 25;
        public const int ENTITY_EXTERIOR_SIREN_LIGHT = 26;
        public const int ENTITY_AUDIO_ALARM = 27;

        public const int ENTITY_CAMERA_FRONT = 28;
        public const int ENTITY_CAMERA_REAR = 29;
        public const int ENTITY_CAMERA_SIDE = 30;
        public const int ENTITY_CAMERA_CABIN = 31;

        public void GenerateDefaultWorkcartEntities()
        {
            AddWorkcartEntityDefinition(ENTITY_1ST_LOCKED_CRATE, PREFAB_CRATE_LOCKED, 0.5F, 1F, -0.742F, 1.427F, -3.675F, 0F, 357F, 0F);
            AddWorkcartEntityDefinition(ENTITY_1ST_SCIENTIST, PREFAB_SCIENTIST, 0.5F, 2F, -0.742F, 2.845F, -3.675F, 0F, 223.095F, 0F, new List<int> { ENTITY_1ST_LOCKED_CRATE });

            AddWorkcartEntityDefinition(ENTITY_2ND_LOCKED_CRATE, PREFAB_CRATE_LOCKED, 0.5F, 1F, 0.670F, 1.427F, -3.784F, 0F, 0F, 0F);
            AddWorkcartEntityDefinition(ENTITY_2ND_SCIENTIST, PREFAB_SCIENTIST, 0.5F, 2F, 0.670F, 2.845F, -3.784F, 0F, 156.938F, 0F, new List<int> { ENTITY_2ND_LOCKED_CRATE });

            //this crate always spawns
            AddWorkcartEntityDefinition(ENTITY_3RD_LOCKED_CRATE, PREFAB_CRATE_LOCKED, 1F, 1F, 0.603F, 1.427F, -0.769F, 0F, 268F, 0F);
            AddWorkcartEntityDefinition(ENTITY_3RD_SCIENTIST, PREFAB_SCIENTIST, 0.5F, 2F, 0.603F, 2.845F, -0.769F, 0F, 93F, 0F, new List<int> { ENTITY_3RD_LOCKED_CRATE });

            AddWorkcartEntityDefinition(ENTITY_1ST_BRADLEY_CRATE, PREFAB_CRATE_BRADLEY, 0.5F, 1F, 0.684F, 1.538F, 2.328F, 0F, 0.8F, 0F);
            AddWorkcartEntityDefinition(ENTITY_2ND_BRADLEY_CRATE, PREFAB_CRATE_BRADLEY, 0.5F, 1F, 0.637F, 2.148F, 2.265F, 0F, 358F, 0F, new List<int> { ENTITY_1ST_BRADLEY_CRATE });

            //this one exists separately
            AddWorkcartEntityDefinition(ENTITY_3RD_BRADLEY_CRATE, PREFAB_CRATE_BRADLEY, 0.5F, 1F, 0.881F, 2.616F, 0.654F, 0F, 270F, 0F);

            AddWorkcartEntityDefinition(ENTITY_1ST_ELITE_CRATE, PREFAB_CRATE_ELITE, 0.5F, 1F, -0.087F, 1.538F, 0.895F, 0F, 270F, 0F);
            AddWorkcartEntityDefinition(ENTITY_2ND_ELITE_CRATE, PREFAB_CRATE_ELITE, 0.5F, 1F, -0.087F, 2.148F, 0.936F, 0F, 90F, 0F, new List<int> { ENTITY_1ST_ELITE_CRATE });
            AddWorkcartEntityDefinition(ENTITY_3RD_ELITE_CRATE, PREFAB_CRATE_ELITE, 0.5F, 1F, -0.087F, 2.739F, 0.859F, 0F, 90F, 0F, new List<int> { ENTITY_1ST_ELITE_CRATE, ENTITY_2ND_ELITE_CRATE });

            AddWorkcartEntityDefinition(ENTITY_1ST_HELI_CRATE, PREFAB_CRATE_HELI, 0.5F, 1F, 0.688F, 1.427F, -1.867F, 0F, 0F, 0F);
            AddWorkcartEntityDefinition(ENTITY_2ND_HELI_CRATE, PREFAB_CRATE_HELI, 0.5F, 1F, 0.688F, 2.016F, -1.867F, 0F, 0F, 0F, new List<int> { ENTITY_1ST_HELI_CRATE });

            AddWorkcartEntityDefinition(ENTITY_3RD_HELI_CRATE, PREFAB_CRATE_HELI, 0.5F, 1F, 0.688F, 1.427F, -2.735F, 0F, 0F, 0F);
            AddWorkcartEntityDefinition(ENTITY_4TH_HELI_CRATE, PREFAB_CRATE_HELI, 0.5F, 1F, 0.688F, 2.016F, -2.735F, 0F, 0F, 0F, new List<int> { ENTITY_3RD_HELI_CRATE });

            //toppest scientists
            AddWorkcartEntityDefinition(ENTITY_4TH_SCIENTIST, PREFAB_SCIENTIST, 0.5F, 2F, 0.678F, 3.804F, 3.890F);
            AddWorkcartEntityDefinition(ENTITY_5TH_SCIENTIST, PREFAB_SCIENTIST, 0.5F, 2F, 0.649F, 3.804F, 2.352F, 0F, 180F, 0F);

            //3 scientists protecting driver
            AddWorkcartEntityDefinition(ENTITY_6TH_SCIENTIST, PREFAB_SCIENTIST, 0.5F, 2F, -0.949F, 1.427F, 4.221F, 0F, 281.336F, 0F);
            AddWorkcartEntityDefinition(ENTITY_7TH_SCIENTIST, PREFAB_SCIENTIST, 0.5F, 2F, -0.746F, 1.427F, 3.234F, 0F, 255F, 0F);
            AddWorkcartEntityDefinition(ENTITY_8TH_SCIENTIST, PREFAB_SCIENTIST, 0.5F, 2F, -0.878F, 1.427F, 2.106F, 0F, 281F, 0F);

            //4 scientists in the middle
            AddWorkcartEntityDefinition(ENTITY_9TH_SCIENTIST, PREFAB_SCIENTIST, 0.5F, 2F, -0.621F, 1.427F, -0.707F, 0F, 236F, 0F);
            AddWorkcartEntityDefinition(ENTITY_10TH_SCIENTIST, PREFAB_SCIENTIST, 0.5F, 2F, -1.164F, 1.427F, -1.623F, 0F, 294F, 0F);
            AddWorkcartEntityDefinition(ENTITY_11TH_SCIENTIST, PREFAB_SCIENTIST, 0.5F, 2F, -0.433F, 1.427F, -1.945F, 0F, 263F, 0F);
            AddWorkcartEntityDefinition(ENTITY_12TH_SCIENTIST, PREFAB_SCIENTIST, 0.5F, 2F, -0.950F, 1.427F, -2.606F, 0F, 245F, 0F);

            //sirens + audio
            AddWorkcartEntityDefinition(ENTITY_INTERIOR_SIREN_LIGHT, PREFAB_SIRENLIGHT, 1F, 1F, 0.723F, 3.798F, 3.187F, 0F, 0F, 180F, null, null, 0, true, false, true, false);
            AddWorkcartEntityDefinition(ENTITY_EXTERIOR_SIREN_LIGHT, PREFAB_SIRENLIGHT, 1F, 1F, 0.723F, 3.837F, 3.187F, 0F, 0F, 0F, null, null, 0, true, false, true, false);
            AddWorkcartEntityDefinition(ENTITY_AUDIO_ALARM, PREFAB_ALARM, 1F, 1F, 0.851F, 3.209F, 1.716F, 0F, 180F, 270F, null, null, 0, true, false, true, false);

            AddWorkcartEntityDefinition(ENTITY_CAMERA_FRONT, PREFAB_CCTV, 1F, 1F, 0.723F, 3.700F, 4.371F, 0F, 0F, 0F, null, null, 0, false, false, false, true);
            AddWorkcartEntityDefinition(ENTITY_CAMERA_REAR, PREFAB_CCTV, 1F, 1F, 0.723F, 3.700F, 1.808F, 0F, 180F, 0F, null, null, 0, false, false, false, true);
            AddWorkcartEntityDefinition(ENTITY_CAMERA_SIDE, PREFAB_CCTV, 1F, 1F, -0.174F, 3.478F, 4.246F, 26.368F, 215.984F, 355.572F, null, null, 0, false, false, false, true);
            AddWorkcartEntityDefinition(ENTITY_CAMERA_CABIN, PREFAB_CCTV, 1F, 1F, -0.116F, 3.599F, 1.861F, 22.5F, 45F, 0F, null, null, 0, false, false, false, true);
        }

        //always the same: 1 locked crate 

        public void AddWorkcartEntityDefinition(int id, string prefabName, float spawnChance = 1F, float healthMultiplier = 1F, float posX = 0F, float posY = 0F, float posZ = 0F, float rotX = 0F, float rotY = 0F, float rotZ = 0F, List<int> reliesOnSpawningIDs = null, List<int> conflictsWithSpawningIDs = null, ulong skinID = 0, bool indestructible = false, bool pickupEnabled = false, bool isLocked = false, bool powerUpImmediately = false)
        {
            configData.WorkcartEntities.Add(id, new WorkcartEntityDefinition
            {
                ID = id,
                PrefabName = prefabName,
                SpawnChance = spawnChance,
                HealthMultiplier = healthMultiplier,
                Indestructible = indestructible,
                PickupEnabled = pickupEnabled,
                IsLocked = isLocked,
                PowerUpImmediately = powerUpImmediately,
                ReliesOnSpawningIDs = reliesOnSpawningIDs,
                ConflictsWithSpawningIDs = conflictsWithSpawningIDs,
                LocalPosX = posX,
                LocalPosY = posY,
                LocalPosZ = posZ,
                LocalRotX = rotX,
                LocalRotY = rotY,
                LocalRotZ = rotZ,
                SkinID = skinID,

            });
        }

        protected override void LoadDefaultConfig()
        {
            RestoreDefaultConfig();
        }
        private void ProcessConfigData()
        {
            bool dataNeedsSave = false;

            if (configData.Version != VERSION)
            {
                var oldVersion = configData.Version;

                //migrate from <1.0.8, some scientist prefab names have changed
                //if any of the entries contains the word "scientist", force it to the new one

                if (oldVersion != "1.0.8")
                {
                    Instance.PrintWarning($"\n\nConverting all the old Scientist prefab names in the config to the new ones ({PREFAB_SCIENTIST})...");

                    int countOld = 0;

                    foreach (var workcartEntityEntry in configData.WorkcartEntities)
                    {
                        if (workcartEntityEntry.Value.PrefabName.Contains("scientist"))
                        {
                            workcartEntityEntry.Value.PrefabName = PREFAB_SCIENTIST;
                            countOld++;
                        }
                    }
                    Instance.PrintWarning($"Successfully converted {countOld} prefab entries into new prefab names.\n\n");
                }


                configData.Version = VERSION;
                Instance.PrintWarning($"\n\nYou have succesfully updated from {oldVersion} to {VERSION}\n");


                dataNeedsSave = true;
            }

            //check for inconsistencies between relies and conflicts.
            //don't do it if either conflicts or relies are null

            Instance.PrintWarning("Checking for inconsistencies between relying/conflicting workcart entities...");

            bool inconsistenciesFound = false;

            IEnumerable<int> currentIntersect;

            //work on a copy
            foreach (var workcartEntityEntry in configData.WorkcartEntities.Values.ToList())
            {
                if (workcartEntityEntry.ConflictsWithSpawningIDs == null) continue;
                if (workcartEntityEntry.ReliesOnSpawningIDs == null) continue;


                currentIntersect = workcartEntityEntry.ConflictsWithSpawningIDs.Intersect(workcartEntityEntry.ReliesOnSpawningIDs).ToList();

                foreach (var integer in currentIntersect)
                {
                    Instance.PrintError($"INCONSISTENCY: Workcart Entity #{workcartEntityEntry.ID} ({workcartEntityEntry.PrefabName}) cannot both rely on and conflict with #{integer}!");
                    inconsistenciesFound = true;

                    configData.WorkcartEntities[workcartEntityEntry.ID].ReliesOnSpawningIDs.Remove(integer);
                    configData.WorkcartEntities[workcartEntityEntry.ID].ConflictsWithSpawningIDs.Remove(integer);
                }
            }

            if (inconsistenciesFound)
            {
                Instance.PrintWarning("Some inconsistencies were found and removed from config data.");
                dataNeedsSave = true;
            }
            else
            {
                Instance.PrintWarning("No inconsistencies found.");
            }

            if (dataNeedsSave)
            {
                SaveConfigData();
            }
        }
        private void RestoreDefaultConfig()
        {
            PrintWarning("Generating default config...");

            configData = new ConfigData();

            GenerateDefaultWorkcartEntities();
            
            SaveConfigData();
        }

        private void LoadConfigData()
        {
            PrintWarning("Loading configuration file...");
            try
            {
                configData = Config.ReadObject<ConfigData>();
                PrintWarning("Success.");
            }
            catch
            {
                PrintWarning("Loading failed, generating new...");
                RestoreDefaultConfig();

            }
            SaveConfigData();

        }
        private void SaveConfigData()
        {
            PrintWarning("Saving config...");
            Config.WriteObject(configData, true);
        }
        #endregion

        #region MONO
        public class BasePlayerHelper : MonoBehaviour
        {
            public SpecialTrain OwnerTrain;
            public BasePlayer Scientist;
            public Vector3 LocalPositionToKeep;

            public static float UPDATE_RATE = 1F;
            public float LastUpdate = UnityEngine.Time.realtimeSinceStartup + 1F;

            public void Prepare(SpecialTrain train, BasePlayer scientist)
            {
                OwnerTrain = train;
                Scientist = scientist;
                LocalPositionToKeep = scientist.transform.localPosition;
            }

            private void FixedUpdate()
            {
                if (UnityEngine.Time.realtimeSinceStartup <= LastUpdate + UPDATE_RATE) return;

                LastUpdate = UnityEngine.Time.realtimeSinceStartup;

                if (OwnerTrain == null) return;
                if (Scientist == null) return;

                if (!Scientist.HasParent())
                {
                    OwnerTrain.OnEntityEnterTrainTriggerReplacement(Scientist);
                    Scientist.SendNetworkUpdateImmediate();
                }
                else
                {
                    if (Vector3.Distance(LocalPositionToKeep, Scientist.transform.localPosition) > 0.5F)
                    {
                        Scientist.transform.localPosition = LocalPositionToKeep;
                        Scientist.transform.hasChanged = true;
                        Scientist.SendNetworkUpdateImmediate();
                    }
                }
            }
        }

        public class TruePVEBubble : MonoBehaviour
        {
            public SpecialTrain OwnerSpecialTrain;
            public SphereCollider SphereCollider;
            public Rigidbody SphereRigidbody;

            public BasePlayer LastPlayer;

            public void Prepare(SpecialTrain train)
            {
                gameObject.SetLayerRecursive((int)Rust.Layer.Reserved1);

                OwnerSpecialTrain = train;

                //add a collider, rigidbody, set the collider layer
                SphereRigidbody = gameObject.AddComponent<Rigidbody>();

                SphereRigidbody.detectCollisions = true;
                SphereRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

                SphereRigidbody.isKinematic = true;
                SphereRigidbody.useGravity = false;

                SphereCollider = gameObject.AddComponent<SphereCollider>();
                SphereCollider.isTrigger = true;
                SphereCollider.radius = Instance.configData.TruePVERadius;
            }

            private void OnTriggerEnter(Collider col)
            {
                LastPlayer = ColliderToPlayer(col);

                if (LastPlayer != null)
                {
                    if (!SpecialTrain.UserIDToSpecialTrains.ContainsKey(LastPlayer.userID))
                    {
                        SpecialTrain.UserIDToSpecialTrains.Add(LastPlayer.userID, new List<SpecialTrain>());
                    }

                    if (!SpecialTrain.UserIDToSpecialTrains[LastPlayer.userID].Contains(OwnerSpecialTrain))
                    {
                        SpecialTrain.UserIDToSpecialTrains[LastPlayer.userID].Add(OwnerSpecialTrain);
                    }

                    if (!OwnerSpecialTrain.PlayersInTruePVEBubble.ContainsKey(LastPlayer.userID))
                    {
                        OwnerSpecialTrain.PlayersInTruePVEBubble.Add(LastPlayer.userID, LastPlayer);
                        Interface.CallHook("OnPlayerEnterPVPBubble", OwnerSpecialTrain.Train, LastPlayer);
                    }
                }
            }

            private void OnTriggerExit(Collider col)
            {
                LastPlayer = ColliderToPlayer(col);

                if (LastPlayer != null)
                {
                    if (SpecialTrain.UserIDToSpecialTrains.ContainsKey(LastPlayer.userID))
                    {
                        if (SpecialTrain.UserIDToSpecialTrains[LastPlayer.userID].Contains(OwnerSpecialTrain))
                        {
                            SpecialTrain.UserIDToSpecialTrains[LastPlayer.userID].Remove(OwnerSpecialTrain);
                        }

                        //check if not the list is empty... if it is, delete the list entry entirely
                        if (SpecialTrain.UserIDToSpecialTrains[LastPlayer.userID].Count == 0)
                        {
                            SpecialTrain.UserIDToSpecialTrains.Remove(LastPlayer.userID);
                        }
                    }

                    if (OwnerSpecialTrain.PlayersInTruePVEBubble.ContainsKey(LastPlayer.userID))
                    {
                        OwnerSpecialTrain.PlayersInTruePVEBubble.Remove(LastPlayer.userID);
                        Interface.CallHook("OnPlayerExitPVPBubble", OwnerSpecialTrain.Train, LastPlayer);
                    }
                }
            }

            public virtual BasePlayer ColliderToPlayer(Collider col)
            {
                var maybeEntity = col.ToBaseEntity();
                if (maybeEntity == null)
                {
                    return null;
                }
                var maybePlayer = col.ToBaseEntity() as BasePlayer;
                return maybePlayer;
            }
        }



        public class SpecialTrain : MonoBehaviour
        {
            public float MovingForwardsScheduledAt;

            //drivers of special trains
            public static Dictionary<ulong, SpecialTrain> TrainDriverNetIDToSpecialTrain;

            //only special trains here
            public static Dictionary<ulong, SpecialTrain> TrainNetIDToSpecialTrain;

            //all trains here for fast lookup
            public static Dictionary<ulong, TrainEngine> TrainNetIDToTrainEngine;

            public static Dictionary<ulong, SpecialTrain> WorkcartEntityNetIDToSpecialTrain;

            public static Dictionary<TriggerBase, SpecialTrain> PlatformTriggerToTrain;

            public static Dictionary<ulong, List<SpecialTrain>> UserIDToSpecialTrains;

            //and keep all your workcart entities here too, just for yourself...
            public Dictionary<ulong, BaseEntity> WorkcartEntityNetIDToEntity = new Dictionary<ulong, BaseEntity>();
            public Dictionary<ulong, IOEntity> WorkcartIOEntityNetIDPoweredImmediately = new Dictionary<ulong, IOEntity>();
            public Dictionary<ulong, IOEntity> WorkcartIOEntityNetIDPoweredOnSelfDestruct = new Dictionary<ulong, IOEntity>();
            public Dictionary<ulong, CCTV_RC> WorkcartCCTVNetIDToCCTV = new Dictionary<ulong, CCTV_RC>();
            public Dictionary<BasePlayer, CCTV_RC> WorkcartCCTVPlayerUserIDToCamera = new Dictionary<BasePlayer, CCTV_RC>();
            public Dictionary<ulong, BasePlayer> PlayersInTruePVEBubble = new Dictionary<ulong, BasePlayer>();

            public PowerCounter MinuteCounter;
            public PowerCounter SecondCounter;

            //and this is for locked crate map markers
            public Dictionary<BaseEntity, BaseEntity> LockedCrateToMapMarker = new Dictionary<BaseEntity, BaseEntity>();

            public TrainEngine Train;
            public ulong TrainNetID;
            public ulong TrainDriverNetID;
            public ScientistNPC TrainDriver;

            public MapMarkerGenericRadius MapMarker;

            public TriggerBase TrainPlatformTrigger;

            public bool DriverIsAlive = true;

            public const float UPDATE_RATE_SHORT = 0.1F;
            public const float UPDATE_RATE_MEDIUM = 1F;

            public float LastUpdateShort = float.MinValue;
            public float LastUpdateMedium = float.MinValue;
            public float LastUpdateEverySecond = float.MinValue;
            public float LastUpdatePositionNotification = UnityEngine.Time.realtimeSinceStartup + Instance.configData.NotifyAboutPositionEvery;
            public Vector3 LastKnownPosition = Vector3.zero;

            public float SelfDestructInitiateAt;
            public float SelfDestructActuallyAt;


            public bool SelfDestructionInitiated = false;
            public bool SelfDestructionShouldHappenNow = false;

            public ulong PreviousOwnerID;
            public float PreviousTopSpeed;
            public bool PreviousGlobalBroadcast;
            public bool PreviousSyncPosition;
            public float PreviousRadiationProtection;
            public float PreviousRadiationExposureProtection;

            public bool TrainIsMoving = true;
            public bool WaitingToRestart = false;
            public bool TrainIsBraking = false;
            public bool BrakingIsUsingReverse = false;

            public GameObject RadiationGameObject;
            public SphereCollider RadiationCollider;
            public TriggerRadiation RadiationTrigger;

            public GameObject TruePVEBubbleGameObject;
            public TruePVEBubble TruePVEBubbleHelper;

            private Action _invokingCheckForHazards;

            public void CreateSplat()
            {
                var _recentSplat = GameManager.server.CreateEntity(PREFAB_RUG, transform.position.WithY(transform.position.y - 0.15F), transform.rotation) as BaseCombatEntity;
                _recentSplat.Spawn();
                _recentSplat.pickup.enabled = false;
                _recentSplat.OwnerID = INTERNAL_OWNERID;
                _recentSplat.skinID = SKIN_SPLAT1;
                _recentSplat.enableSaving = false;
                _recentSplat.EnableGlobalBroadcast(true);
                _recentSplat.syncPosition = true;
                _recentSplat.transform.eulerAngles = _recentSplat.transform.eulerAngles.WithY(UnityEngine.Random.Range(0F, 360F));
                _recentSplat.SendNetworkUpdateImmediate();

                _recentSplat.Invoke(() => { if (_recentSplat == null) return; if (_recentSplat.IsDestroyed) return; _recentSplat.Kill(BaseNetworkable.DestroyMode.None); }, UnityEngine.Random.Range(Instance.configData.SplatsLifetimeMin, Instance.configData.SplatsLifetimeMax));
            }

            public void Prepare(TrainEngine train)
            {
                this.Train = train;

                TrainPlatformTrigger = Train.platformParentTrigger;

                PlatformTriggerToTrain.Add(TrainPlatformTrigger, this);

                train.SetFlag(TrainEngine.Flag_AltColor, true);
                train.SendNetworkUpdateImmediate();

                this.TrainNetID = train.net.ID.Value;
                _invokingCheckForHazards = (Action)Delegate.CreateDelegate(typeof(Action), Train, "CheckForHazards");

                Train.SetHealth(Train.MaxHealth());

                PreviousOwnerID = Train.OwnerID;
                Train.OwnerID = INTERNAL_OWNERID;

                Instance.EnableInfiniteFuel(Train);

                MinuteCounter = SpawnEntityRelativeToTrain(PREFAB_COUNTER, Instance.configData.CounterMinutePosX, Instance.configData.CounterMinutePosY, Instance.configData.CounterMinutePosZ, Instance.configData.CounterMinuteRotX, Instance.configData.CounterMinuteRotY, Instance.configData.CounterMinuteRotZ, 1F) as PowerCounter;

                MakeStable(MinuteCounter, false, true, true);

                SecondCounter = SpawnEntityRelativeToTrain(PREFAB_COUNTER, Instance.configData.CounterSecondPosX, Instance.configData.CounterSecondPosY, Instance.configData.CounterSecondPosZ, Instance.configData.CounterSecondRotX, Instance.configData.CounterSecondRotY, Instance.configData.CounterSecondRotZ, 1F) as PowerCounter;

                MakeStable(SecondCounter, false, true, true);

                PreviousGlobalBroadcast = Train.globalBroadcast;
                PreviousSyncPosition = Train.syncPosition;

                PreviousRadiationProtection = Train.driverProtection.Get(Rust.DamageType.Radiation);
                PreviousRadiationExposureProtection = Train.driverProtection.Get(Rust.DamageType.RadiationExposure);

                if (Instance.configData.RadiationEnable)
                {
                    RadiationGameObject = new GameObject("TrainEventRadiation");
                    RadiationGameObject.layer = (int)Rust.Layer.Reserved1;
                    RadiationGameObject.SetActive(true);

                    RadiationGameObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
                    RadiationGameObject.transform.SetParent(transform, true);

                    RadiationCollider = RadiationGameObject.AddComponent<SphereCollider>();
                    RadiationCollider.radius = Instance.configData.RadiationRadius;
                    RadiationCollider.isTrigger = true;

                    /*
                    var rigid = RadiationGameObject.AddComponent<Rigidbody>();

                    rigid.useGravity = false;
                    rigid.isKinematic = true;

                    rigid.collisionDetectionMode = CollisionDetectionMode.Discrete;
                    rigid.detectCollisions = true;
                    */

                    RadiationTrigger = RadiationGameObject.AddComponent<TriggerRadiation>();
                    RadiationTrigger.radiationTier = (TriggerRadiation.RadiationTier)Instance.configData.RadiationTier;
                    RadiationTrigger.interestLayers = 131072;
                    RadiationTrigger.enabled = true;
                }

                Train.driverProtection.Add(Rust.DamageType.Radiation, 100F);
                Train.driverProtection.Add(Rust.DamageType.RadiationExposure, 100F);

                Train.EnableGlobalBroadcast(true);
                Train.syncPosition = true;

                PreviousTopSpeed = Train.maxSpeed;
                Train.maxSpeed = Instance.configData.TrainTopSpeed;

                TrainNetIDToSpecialTrain.Add(TrainNetID, this);

                SelfDestructInitiateAt = UnityEngine.Time.realtimeSinceStartup + Instance.configData.EventDuration;
                SelfDestructActuallyAt = UnityEngine.Time.realtimeSinceStartup + Instance.configData.EventDuration + Instance.configData.SelfDestructTimer;

                //driver
                TrainDriver = GameManager.server.CreateEntity(PREFAB_DRIVER, train.transform.position, train.transform.rotation, true) as ScientistNPC;
                TrainDriver.userID = (ulong) UnityEngine.Random.Range(0, int.MaxValue);
                TrainDriver.UserIDString = TrainDriver.userID.ToString();
                TrainDriver.Spawn();

                TrainDriver.displayName = Instance.configData.TrainDriverName;
                TrainDriver.SendNetworkUpdateImmediate();

                if (Instance.configData.TrainDriverKit == null || Instance.configData.TrainDriverKit == "")
                {
                    Instance.GivePlayerBuiltInVisualKit(TrainDriver);
                }
                else
                {
                    if (Instance.Kits)
                    {
                        GiveBotKit(TrainDriver, Instance.configData.TrainDriverKit);
                    }

                }

                TrainDriver.syncPosition = true;
                TrainDriver.EnableGlobalBroadcast(true);

                this.TrainDriverNetID = TrainDriver.net.ID.Value;

                TrainDriver.OwnerID = INTERNAL_OWNERID;

                TrainDriverNetIDToSpecialTrain.Add(TrainDriverNetID, this);

                var firstMountable = Train.GetComponentsInChildren<BaseVehicleSeat>().FirstOrDefault();

                //firstMountable.MountPlayer(TrainDriver);
                firstMountable.AttemptMount(TrainDriver, false);

                TruePVEPrepare();

                TrainStartMovingForwards();

                if (Instance.configData.MapMarkerEnable)
                {
                    MapMarker = SpawnEntityRelativeToTrain(PREFAB_GENERIC_MARKER, Vector3.zero, Vector3.zero) as MapMarkerGenericRadius;
                    MapMarker.color1 = new ColorCode(Instance.configData.MapMarkerColor1).rustValue;
                    MapMarker.color2 = new ColorCode(Instance.configData.MapMarkerColor2).rustValue;
                    MapMarker.alpha = Instance.configData.MapMarkerAlpha;
                    MapMarker.radius = Instance.configData.MapMarkerRadius;

                    MapMarker.SetParent(train, true, true);

                    MapMarker.EnableGlobalBroadcast(true);
                    MapMarker.syncPosition = true;

                    MapMarker.SendUpdate(true);

                    //MapMarker.gameObject.AddComponent<MapMarkerHelper>().Prepare(this, MapMarker);
                }

                Instance.NextTick(() =>
                {
                    PostPrepare();
                });
            }
            public void TruePVEPrepare()
            {
                if (!(Instance.TruePVEIsLoaded && Instance.configData.TruePVERadius > 0F))
                {
                    return;
                }

                TruePVEBubbleGameObject = new GameObject("TruePVEBubble");
                TruePVEBubbleGameObject.SetActive(true);

                TruePVEBubbleGameObject.transform.SetPositionAndRotation(Train.transform.position, Train.transform.rotation);
                TruePVEBubbleGameObject.transform.SetParent(Train.transform, true);

                TruePVEBubbleHelper = TruePVEBubbleGameObject.AddComponent<TruePVEBubble>();
                TruePVEBubbleHelper.Prepare(this);
            }

            public void PostPrepare()
            {
                //entities
                //first, make a "roll" for each entity individually.
                //if it's 1F no roll (just true), if it's 0F no roll, just false.
                //once you have the results, iterate over them.

                Dictionary<int, WorkcartEntityDefinition> finalDict = new Dictionary<int, WorkcartEntityDefinition>();
                List<WorkcartEntityDefinition> finalList = new List<WorkcartEntityDefinition>();
                List<int> allRollsSuccess = new List<int>();

                bool rollSuccess;

                //first, everybody rolls.
                foreach (var workcartEntity in Instance.configData.WorkcartEntities)
                {
                    if (workcartEntity.Value.SpawnChance == 1F)
                    {
                        rollSuccess = true;
                    }
                    else
                    {
                        if (workcartEntity.Value.SpawnChance == 0F)
                        {
                            rollSuccess = false;
                        }
                        else
                        {
                            rollSuccess = UnityEngine.Random.Range(0F, 1F) <= workcartEntity.Value.SpawnChance;
                        }
                    }

                    if (rollSuccess)
                    {
                        allRollsSuccess.Add(workcartEntity.Key);
                        finalDict.Add(workcartEntity.Key, workcartEntity.Value);
                    }

                }

                foreach (var workcartEntity in finalDict)
                {
                    bool atLeastOneProblemFound = false;

                    if (workcartEntity.Value.ConflictsWithSpawningIDs != null)
                    {
                        foreach (var conflict in workcartEntity.Value.ConflictsWithSpawningIDs)
                        {
                            if (allRollsSuccess.Contains(conflict))
                            {
                                //nope. we can't count this roll, as it conflicts with another successfull roll.
                                atLeastOneProblemFound = true;
                                break;
                            }
                        }
                    }

                    if (!atLeastOneProblemFound)
                    {
                        if (workcartEntity.Value.ReliesOnSpawningIDs != null)
                        {
                            foreach (var relies in workcartEntity.Value.ReliesOnSpawningIDs)
                            {
                                if (!allRollsSuccess.Contains(relies))
                                {
                                    //nope. we can't count this roll, as the roll relied on wasn't successful
                                    atLeastOneProblemFound = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!atLeastOneProblemFound)
                    {
                        //if everything goes well, add to the final list!
                        finalList.Add(workcartEntity.Value);
                    }

                }

                //we got them all? great!

                foreach (var workcartEntity in finalList)
                {
                    CreateWorkcartEntity(workcartEntity);
                }

                if (Instance.configData.NotifyWhenStarted)
                {
                    string finalMessage = MSG(MSG_EVENT_STARTED, null, PhoneController.PositionToGridCoord(Train.transform.position));

                    //inform about cameras
                    string cctvMessage = "";

                    if (WorkcartCCTVNetIDToCCTV.Count > 0)
                    {
                        cctvMessage = $" {MSG(MSG_AVAILABLE_CAMERAS2)}";

                        foreach (var cam in WorkcartCCTVNetIDToCCTV)
                        {
                            var sep = ", ";
                            if (cam.Key == WorkcartCCTVNetIDToCCTV.LastOrDefault().Key)
                            {
                                sep = ".";
                            }
                            cctvMessage += $"{cam.Value.rcIdentifier}{sep}";
                        }
                    }

                    Instance.TellMessage(null, $"{finalMessage}{cctvMessage}", true);

                }

                PowerStuffUp(true);

                Interface.CallHook("OnTrainEventStarted", Train);
            }

            private BaseEntity _newWorkcartEntity;
            private Vector3 _localPos;
            private Vector3 _localRot;
            private Vector3 _worldPos;
            private Vector3 _worldRot;

            public void OnCrateHack(HackableLockedCrate crate)
            {
                if (Instance.configData.NotifyAboutCrateHacking)
                {
                    Instance.TellMessage(null, MSG(MSG_EVENT_CRATE_BEING_HACKED));
                }

                //is it not too late?
                if (!SelfDestructionInitiated)
                {
                    if (Instance.configData.HackingAddsSeconds > 0F)
                    {
                        SelfDestructInitiateAt += Instance.configData.HackingAddsSeconds;
                        UpdateCurrentTimespan();
                        UpdateNeededCounters(true);
                    }
                }

            }

            private object _damageResult;
            private bool _shouldBrake;
            private BasePlayer _htnPlayer;

            public object OnTrainEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
            {
                if (entity == null) return null;
                if (info == null) return null;
                /*
                if (info.Initiator as TunnelDweller != null)
                {
                    return true;
                }*/

                _htnPlayer = entity as BasePlayer;


                if (_htnPlayer != null)
                {
                    //do NOT divide by 0

                    //if it's a driver it needs an extra buff times 10 now, jesus

                    bool isDriver = TrainDriverNetIDToSpecialTrain.ContainsKey(entity.net.ID.Value);

                    info.damageTypes.ScaleAll((isDriver ? 10F : 1F) *(1F / (Instance.configData.ScientistHealthMultiplier)));
                }

                if (SelfDestructionInitiated) return null;

                if (info.Initiator != null)
                {
                    if (WorkcartEntityNetIDToEntity.ContainsKey(info.Initiator.net.ID.Value))
                    {
                        //take damage fron entity-to-entity, but don't register it as requiring attention
                        return null;
                    }
                }

                //override result by default
                _damageResult = null;
                _shouldBrake = true;

                //train auto starts/stops only when the driver is still alive

                //don't do this if it's 0
                if (Instance.configData.WhenAttackedStopFor > 0F)
                {
                    if (!DriverIsAlive)
                    {
                        _damageResult = null;
                        _shouldBrake = false;
                    }
                    else
                    {
                        if (WaitingToRestart)
                        {
                            //if we're already stopped, delay re-starting of the train (restarting the counter)
                            MovingForwardsScheduledAt = UnityEngine.Time.realtimeSinceStartup + Instance.configData.WhenAttackedStopFor;
                            _damageResult = null;
                            _shouldBrake = false;
                        }
                        else
                        {
                            if (TrainIsBraking)
                            {
                                if (UnityEngine.Time.realtimeSinceStartup >= _canStopReversingAt)
                                {
                                    TrainFinalizeBraking();
                                    TrainStartMovingForwards();
                                }
                            }
                        }
                    }
                }
                else
                {
                    //if WhenAttackedStopFor is 0 or less
                    _shouldBrake = false;
                }

                if (Instance.configData.DontStopOnTunnelDwellerAttack)
                {
                    if (info.Initiator as TunnelDweller != null)
                    {
                        _shouldBrake = false;
                    }
                }


                if (_shouldBrake)
                {
                    if (!TrainIsBraking)
                    {
                        TrainInitiateBraking();
                    }
                }

                //never damage to trains...
                if (entity.net.ID.Value == TrainNetID)
                {
                    _damageResult = true;
                }
                //always damage to science bois
                else
                {
                    if (entity is BasePlayer)
                    {
                        _damageResult = null;
                    }
                }

                return _damageResult;
            }

            public void OnEntityEnterTrainTriggerReplacement(BaseEntity ent)
            {
                if (ent == null) return;
                if (Train == null) return;
                ent.SetParent(Train, true, true);

                if (Train.platformParentTrigger.entityContents == null)
                {
                    Train.platformParentTrigger.entityContents = new HashSet<BaseEntity>();
                }

                if (!Train.platformParentTrigger.entityContents.Contains(ent))
                {
                    Train.platformParentTrigger.entityContents.Add(ent);
                }

                if (Train.platformParentTrigger.entityContents != null && Train.platformParentTrigger.entityContents.Count == 1)
                {
                    Train.platformParentTrigger.InvokeRepeating("OnTick", 0f, 0f);
                }
            }

            public BaseEntity SpawnEntityRelativeToTrain(string prefabName, Vector3 position, Vector3 rotation, float healthMultiplier = 1F)
            {
                return SpawnEntityRelativeToTrain(prefabName, position.x, position.y, position.z, rotation.x, rotation.y, rotation.z, healthMultiplier);
            }

            public BaseEntity SpawnEntityRelativeToTrain(string prefabName, float posX, float posY, float posZ, float rotX, float rotY, float rotZ, float healthMultiplier = 1F)
            {
                _localPos = new Vector3(posX, posY, posZ);
                _localRot = new Vector3(rotX, rotY, rotZ);

                _worldPos = Train.transform.TransformPoint(_localPos);
                _worldRot = Train.transform.TransformDirection(_localRot);

                _newWorkcartEntity = GameManager.server.CreateEntity(prefabName, _worldPos, Quaternion.Euler(_worldRot), true);

                _newWorkcartEntity.Spawn();

                if (Instance.configData.TrainScientistKit != null)
                {
                    var _maybeNPC = _newWorkcartEntity as BasePlayer;

                    if (_maybeNPC != null)
                    {
                        //if (_maybeNPC.PrefabName != PREFAB_DRIVER)
                        {
                            if (Instance.Kits)
                            {
                                GiveBotKit(_maybeNPC, Instance.configData.TrainScientistKit);
                            }
                        }
                    }
                }

                /*
                var _maybeCombat = _newWorkcartEntity as BaseCombatEntity;

                if (_maybeCombat != null)
                {
                    _maybeCombat.InitializeHealth(_maybeCombat._maxHealth * healthMultiplier, _maybeCombat._maxHealth * healthMultiplier);

                    //_maybeCombat.SetMaxHealth(_maybeCombat._maxHealth * healthMultiplier);
                    //_maybeCombat.SetHealth(_maybeCombat._maxHealth*healthMultiplier);

                    //I need to figure all this shit out, so I'm just gonna leave that mess in

                    /*
                    var _maybeHTN = _maybeCombat as HTNPlayer;

                    if (_maybeHTN != null)
                    {
                        for (var i=0; i<_maybeHTN.baseProtection.amounts.Length; i++)
                        {
                            var prot = _maybeHTN.baseProtection.amounts[i];

                            Instance.PrintWarning($"BASE {i}: {prot}");
                        }

                        for (var i = 0; i < _maybeHTN.cachedProtection.amounts.Length; i++)
                        {
                            var prot = _maybeHTN.cachedProtection.amounts[i];

                            Instance.PrintWarning($"CACHED {i}: {prot}");
                        }

                        _maybeHTN.baseProtection.Multiply(healthMultiplier);
                        _maybeHTN.cachedProtection.Multiply(healthMultiplier);

                        for (var i = 0; i < _maybeHTN.baseProtection.amounts.Length; i++)
                        {
                            var prot = _maybeHTN.baseProtection.amounts[i];

                            Instance.PrintWarning($"AFTER MULTIPLYING BASE {i}: {prot}");
                        }

                        for (var i = 0; i < _maybeHTN.cachedProtection.amounts.Length; i++)
                        {
                            var prot = _maybeHTN.cachedProtection.amounts[i];

                            Instance.PrintWarning($"AFTER MULTIPLYING CACHED {i}: {prot}");
                        }
                    }
                    else
                    {

                    }
                    
                    
                    Instance.timer.Once(0.1F,() =>
                    {


                        _maybeCombat.SendNetworkUpdateImmediate();

                        var _maybeHTN = _maybeCombat as HTNPlayer;

                        if (_maybeHTN != null)
                        {
                            //Interface.Oxide.DataFileSystem.WriteObject($"CargoTrainEvent.DUMPEROO", _maybeHTN._aiDefinition.);
                        }

                    });
                    

                }
            */
                _newWorkcartEntity.syncPosition = true;
                _newWorkcartEntity.EnableGlobalBroadcast(true);

                return _newWorkcartEntity;
            }

            public void MakeStable(BaseEntity entity, bool pickupEnabled = false, bool indestructrible = true, bool powerImmediately = true)
            {
                entity.OwnerID = INTERNAL_OWNERID;

                var _maybeLockedCrate = entity as HackableLockedCrate;

                if (_maybeLockedCrate != null)
                {
                    _maybeLockedCrate.SendMessage("SetWasDropped", SendMessageOptions.DontRequireReceiver);
                    //remove current map marker...
                    _maybeLockedCrate.DestroyShared();

                    //and spawn a new one

                    var newMarker = GameManager.server.CreateEntity(PREFAB_CRATE_MARKER, _maybeLockedCrate.transform.position, Quaternion.identity);

                    newMarker.Spawn();

                    newMarker.syncPosition = true;
                    newMarker.EnableGlobalBroadcast(true);

                    newMarker.SetParent(_maybeLockedCrate);
                    newMarker.transform.localPosition = Vector3.zero;
                    newMarker.SendNetworkUpdate();

                    LockedCrateToMapMarker.Add(_maybeLockedCrate, newMarker);
                }
                else
                {
                    _maybeElectricStuff = entity as IOEntity;

                    if (_maybeElectricStuff != null)
                    {
                        if (powerImmediately)
                        {

                            WorkcartIOEntityNetIDPoweredImmediately.Add(_maybeElectricStuff.net.ID.Value, _maybeElectricStuff);
                        }
                        else
                        {
                            WorkcartIOEntityNetIDPoweredOnSelfDestruct.Add(_maybeElectricStuff.net.ID.Value, _maybeElectricStuff);
                        }

                        if (Instance.configData.CamerasAreFunctional)
                        {
                            var maybeCCTV = _maybeElectricStuff as CCTV_RC;
                            if (maybeCCTV != null)
                            {
                                maybeCCTV.isStatic = false;
                                maybeCCTV.hasPTZ = true;

                                PowerIOEntityOn(maybeCCTV);
                                maybeCCTV.UpdateIdentifier($"TRAIN{TrainNetID.ToString().Substring(TrainNetID.ToString().Length - 3)}{(char)(65 + WorkcartCCTVNetIDToCCTV.Count)}");
                                maybeCCTV.SendNetworkUpdateImmediate();

                                WorkcartCCTVNetIDToCCTV.Add(maybeCCTV.net.ID.Value, maybeCCTV);
                            }
                        }


                    }
                }
                


                var _groundWatch = entity.gameObject.GetComponent<GroundWatch>();

                if (_groundWatch != null)
                {
                    UnityEngine.Object.DestroyImmediate(_groundWatch);
                }

                var _destroyOnGroundMissing = entity.gameObject.GetComponent<DestroyOnGroundMissing>();

                if (_destroyOnGroundMissing != null)
                {
                    UnityEngine.Object.DestroyImmediate(_destroyOnGroundMissing);
                }
                entity.enableSaving = false;
                entity.EnableGlobalBroadcast(true);
                entity.syncPosition = true;

                entity.RemoveFromTriggers();

                //players don't need to do this
                //if (entity as BasePlayer == null)
                {
                    OnEntityEnterTrainTriggerReplacement(entity);
                    entity.transform.localPosition = _localPos;
                    entity.transform.localEulerAngles = _localRot;
                    entity.transform.hasChanged = true;
                    entity.SendNetworkUpdateImmediate();
                }

                //maybe we need to start ignoring colliders now?


                var _baseCombat = entity as BaseCombatEntity;

                if (_baseCombat != null)
                {
                    _baseCombat.pickup.enabled = pickupEnabled;
                }

                if (indestructrible)
                {
                    //to ignore damage given to it
                    entity.OwnerID = INTERNAL_OWNERID;
                }

                var _rigid = entity.gameObject.GetComponent<Rigidbody>();

                if (entity.PrefabName == PREFAB_CRATE_LOCKED)
                {
                    //this rigidbody needs to go.
                    if (_rigid != null)
                    {
                        DestroyImmediate(_rigid);
                    }
                }

            }

            public void CreateWorkcartEntity(WorkcartEntityDefinition definition)
            {
                try
                {
                    var entiteh = SpawnEntityRelativeToTrain(definition.PrefabName, definition.LocalPosX, definition.LocalPosY, definition.LocalPosZ, definition.LocalRotX, definition.LocalRotY, definition.LocalRotZ, definition.HealthMultiplier);

                    _newWorkcartEntity.skinID = definition.SkinID;

                    MakeStable(entiteh, definition.PickupEnabled, definition.Indestructible, definition.PowerUpImmediately);

                    var maybePlayer = entiteh as BasePlayer;

                    if (maybePlayer != null)
                    {
                        maybePlayer.gameObject.AddComponent<BasePlayerHelper>().Prepare(this, maybePlayer);
                    }

                    WorkcartEntityNetIDToSpecialTrain.Add(_newWorkcartEntity.net.ID.Value, this);
                    WorkcartEntityNetIDToEntity.Add(_newWorkcartEntity.net.ID.Value, _newWorkcartEntity);
                }
                catch (Exception e)
                {
                    Instance.PrintError($"ERROR: Something went wrong while trying to spawn {definition.PrefabName} - is the prefab name correct?\nMESSAGE:\n{e.Message}\nSTACK TRACE: {e.StackTrace}");
                }

            }

            public void RemoveWorkcartEntity(ulong entityNetID)
            {
                WorkcartEntityNetIDToSpecialTrain.Remove(entityNetID);
            }

            public void TrainEngineSetGear(TrainEngine.EngineSpeeds engineSpeed = TrainEngine.EngineSpeeds.Zero)
            {
                Train.SetThrottle(engineSpeed);
            }

            private float _canStopReversingAt;

            public void TrainInitiateBraking(float delayStop = 0F)
            {
                if (!TrainIsMoving) return;
                if (TrainIsBraking) return;

                TrainIsBraking = true;
                BrakingIsUsingReverse = true;
                TrainEngineSetGear(TrainEngine.EngineSpeeds.Rev_Hi);

                _canStopReversingAt = UnityEngine.Time.realtimeSinceStartup + delayStop;

                Interface.CallHook("OnTrainStartBraking", Train, delayStop);
            }

            public void TrainFinalizeBraking()
            {
                TrainIsBraking = false;
                BrakingIsUsingReverse = false;

                TrainEngineSetGear();
                TrainIsMoving = false;

                WaitingToRestart = true;
                MovingForwardsScheduledAt = UnityEngine.Time.realtimeSinceStartup + Instance.configData.WhenAttackedStopFor;

                Interface.CallHook("OnTrainFinalizeBraking", Train);
            }

            public void TrainStartMovingForwards()
            {
                TrainEngineStart();
                TrainEngineSetGear(TrainEngine.EngineSpeeds.Fwd_Hi);

                WaitingToRestart = false;
                TrainIsMoving = true;

                Interface.CallHook("OnTrainMoveAgain", Train);
            }

            public void TrainEngineStart()
            {
                if (Train.engineController.IsOn) return;
                if (Train.engineController.IsStarting) return;

                Train.engineController.TryStartEngine(TrainDriver);
            }

            public void TrainEngineStop()
            {
                if (!Train.engineController.IsOn) return;
                if (Train.engineController.IsStarting)
                {
                    Train.engineController.CancelEngineStart();
                }
                Train.engineController.StopEngine();
            }

            public object OnTrainDriverKilled(HitInfo info)
            {
                if (TrainDriver != null)
                {
                    //1.0.5 hotfix
                    //TrainDriver.inventory.Strip();
                }

                //don't message if the train's about to self-destruct, no point
                if (SelfDestructionShouldHappenNow) return null;

                Instance.DisableInfiniteFuel(Train);

                if (Instance.configData.DeadManSwitchStop)
                {
                    TrainInitiateBraking();
                }

                if (Instance.configData.NotifyAboutDriverDeath)
                {
                    Instance.TellMessage(null, MSG(MSG_EVENT_DRIVER_DEAD));
                }

                Interface.CallHook("OnTrainDriverDeath", Train, TrainDriver, info);

                DriverIsAlive = false;
                TrainDriverCleanup(true);

                return null;
            }

            public void WorkcartEntitiesCleanup()
            {
                if (MapMarker != null)
                {
                    if (!MapMarker.IsDestroyed)
                    {
                        MapMarker.Kill(BaseNetworkable.DestroyMode.None);
                    }
                }

                foreach (var workcartEntity in WorkcartEntityNetIDToEntity.Values.ToList())
                {
                    if (workcartEntity == null) continue;
                    if (workcartEntity.IsDestroyed) continue;

                    workcartEntity.Kill(BaseNetworkable.DestroyMode.Gib);
                }

                if (MinuteCounter != null)
                {
                    if (!MinuteCounter.IsDestroyed)
                    {
                        MinuteCounter.Kill(BaseNetworkable.DestroyMode.None);
                    }
                }

                if (SecondCounter != null)
                {
                    if (!SecondCounter.IsDestroyed)
                    {
                        SecondCounter.Kill(BaseNetworkable.DestroyMode.None);
                    }
                }
            }

            public void TrainDriverCleanup(bool wasJustKilled = false)
            {
                if (TrainDriverNetIDToSpecialTrain.ContainsKey(TrainDriverNetID))
                {
                    TrainDriverNetIDToSpecialTrain.Remove(TrainDriverNetID);
                }

                //don't kill the player twice!
                if (wasJustKilled) return;
                if (!DriverIsAlive) return;

                if (TrainDriver != null)
                {
                    if (!TrainDriver.IsDestroyed)
                    {
                        TrainDriver.Kill(BaseNetworkable.DestroyMode.None);
                    }
                }
            }

            private void FixedUpdate()
            {
                if (Train == null)
                {
                    DestroyImmediate(this);
                    return;
                }

                if (UnityEngine.Time.realtimeSinceStartup < LastUpdateShort + UPDATE_RATE_SHORT)
                {
                    return;
                }

                LastUpdateShort = UnityEngine.Time.realtimeSinceStartup;

                if (!DriverIsAlive)
                {
                    TrainIsMoving = !Train.IsStationary();
                }


                if (Instance.configData.SplatsEnable)
                {
                    if (TrainIsMoving)
                    {
                        if (UnityEngine.Random.Range(0F, 1F) < Instance.configData.SplatsPerSecond * UPDATE_RATE_SHORT)
                        {
                            CreateSplat();

                            /*
                            Network.Visibility.Group group;

                            group = Network.Net.sv.visibility.GetGroup(Train.transform.position);

                            foreach (var playerToCamera in WorkcartCCTVPlayerUserIDToCamera)
                            {
                                if (playerToCamera.Key.net.secondaryGroup == group) continue;
                                playerToCamera.Key.net.SwitchSecondaryGroup(group);
                            } */
                        }
                    }
                }

                if (TrainIsBraking)
                {
                    if (BrakingIsUsingReverse)
                    {
                        if (Train.rigidBody.velocity.magnitude <= 0.1F)
                        {
                            if (UnityEngine.Time.realtimeSinceStartup >= _canStopReversingAt)
                            {
                                TrainFinalizeBraking();
                            }
                        }
                    }
                }
                else
                {
                    if (WaitingToRestart)
                    {
                        if (DriverIsAlive)
                        {
                            if (UnityEngine.Time.realtimeSinceStartup >= MovingForwardsScheduledAt)
                            {
                                TrainStartMovingForwards();
                            }
                        }
                        else
                        {
                            WaitingToRestart = false;
                            TrainIsMoving = true;
                            TrainIsBraking = false;
                            BrakingIsUsingReverse = false;
                        }

                    }
                }

                if (DriverIsAlive)
                {
                    Train.SetTrackSelection(UnityEngine.Random.Range(0F, 1F) > 0.5F ? TrainTrackSpline.TrackSelection.Left : TrainTrackSpline.TrackSelection.Right);
                }

                if (!SelfDestructionInitiated)
                {
                    //check if you should self destruct
                    if (UnityEngine.Time.realtimeSinceStartup >= SelfDestructInitiateAt)
                    {
                        SelfDestructionInitiate();
                    }
                }
                else
                {
                    if (!SelfDestructionShouldHappenNow)
                    {
                        if (UnityEngine.Time.realtimeSinceStartup >= SelfDestructActuallyAt)
                        {
                            SelfDestructionActually();
                        }
                    }
                }

                if (SelfDestructionShouldHappenNow)
                {
                    return;
                }
                else
                {
                    if (UnityEngine.Time.realtimeSinceStartup < LastUpdateEverySecond + 1F)
                    {
                        return;
                    }

                    LastUpdateEverySecond = UnityEngine.Time.realtimeSinceStartup;

                    UpdateCurrentTimespan();

                    if (UnityEngine.Time.realtimeSinceStartup < LastUpdateMedium + UPDATE_RATE_MEDIUM)
                    {
                        return;
                    }

                    UpdateNeededCounters();

                    LastUpdateMedium = UnityEngine.Time.realtimeSinceStartup;

                    if (!DriverIsAlive)
                    {
                        TrainIsMoving = Train.engineController.IsOn && Train.CurThrottleSetting != TrainEngine.EngineSpeeds.Zero;
                    }

                    if (!TrainIsBraking)
                    {
                        if (DriverIsAlive && TrainIsMoving)
                        {
                            //is the train stuck at the end of the track? Revere
                            if (Train.transform.position == LastKnownPosition)
                            {
                                TrainInitiateBraking(5F);
                            }
                        }
                    }

                    LastKnownPosition = Train.transform.position;


                    Train.CancelInvoke(_invokingCheckForHazards);

                    if (SelfDestructionInitiated)
                    {
                        Train.SetFlag(TrainEngine.Flag_HazardAhead, true);
                    }
                }

                if (!Instance.configData.NotifyAboutPosition) return;

                if (UnityEngine.Time.realtimeSinceStartup < LastUpdatePositionNotification + Instance.configData.NotifyAboutPositionEvery)
                {
                    return;
                }

                LastUpdatePositionNotification = UnityEngine.Time.realtimeSinceStartup;

                if (Train.rigidBody.velocity.magnitude < 0.1F)
                {
                    //don't display a message if the train is stopped
                    return;
                }

                Instance.TellMessage(null, MSG(MSG_EVENT_UPDATE_POSITION, null, PhoneController.PositionToGridCoord(transform.position)));
            }

            private IOEntity _maybeElectricStuff;

            public void UpdateCurrentTimespan()
            {
                _timeSpan = TimeSpan.FromSeconds((SelfDestructionInitiated ? SelfDestructActuallyAt : SelfDestructInitiateAt) - UnityEngine.Time.realtimeSinceStartup);

                _sec = _timeSpan.Seconds;
                _min = _timeSpan.Minutes;

                if (_sec < 0) _sec = 0;
                if (_min < 0) _min = 0;
            }

            public void UpdateNeededCounters(bool forceMinutes = false)
            {
                if (!Instance.configData.AttachTimeCounters) return;

                if (MinuteCounter == null) return;
                if (SecondCounter == null) return;

                if (SecondCounter.counterNumber != _sec)
                {
                    if (SecondCounter.counterNumber == 59 || SecondCounter.counterNumber == 0 || forceMinutes)
                    {
                        if (MinuteCounter.counterNumber != _min)
                        {
                            UpdateParticularCounter(MinuteCounter, _min);
                        }

                    }

                    UpdateParticularCounter(SecondCounter, _sec);
                }
            }

            public void UpdateParticularCounter(PowerCounter counter, int newValue)
            {

                counter.targetCounterNumber = 0;
                counter.SetCounterNumber(newValue);
                //counter.SetFlag(BaseEntity.Flags.On, on, false, true);
                counter.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            }

            public void PowerStuffUp(bool immediately)
            {
                if (immediately)
                {
                    foreach (var ioentity in WorkcartIOEntityNetIDPoweredImmediately)
                    {
                        PowerIOEntityOn(ioentity.Value);
                    }
                    return;
                }

                foreach (var ioentity in WorkcartIOEntityNetIDPoweredOnSelfDestruct)
                {
                    PowerIOEntityOn(ioentity.Value);
                }

            }

            public void PowerIOEntityOn(IOEntity entity)
            {
                if (entity == null) return;

                entity.UpdateHasPower(100, 0);
                entity.SetFlag(BaseEntity.Flags.On, true, false, true);

                if (WorkcartCCTVNetIDToCCTV.ContainsKey(entity.net.ID.Value))
                {
                    var cctv = WorkcartCCTVNetIDToCCTV[entity.net.ID.Value];
                }
            }
            private TimeSpan _timeSpan;
            private int _sec;
            private int _min;

            public string GetFormattedTimeLeft()
            {
                /*
                _secS = $"{(_sec < 10 ? "0" : "")}{_sec}";
                _minS = $"{(_min < 10 ? "0" : "")}{_min}";
                */
                if (_sec == 0 && _min == 0)
                {
                    return "XX:XX";
                }
                else
                {
                    if (_sec != 0 && _min != 0)
                    {
                        return MSG(MSG_FORMAT_TIME_BOTH, null, _min, _sec);
                    }
                    else
                    {
                        if (_sec == 0)
                        {
                            return MSG(MSG_FORMAT_TIME_MIN, null, _min);
                        }
                        else
                        {
                            if (_min == 0)
                            {
                                return MSG(MSG_FORMAT_TIME_SEC, null, _sec);
                            }
                        }
                    }
                }

                return null;
            }

            public void SelfDestructionInitiate(bool becauseDriverDied = false)
            {
                if (becauseDriverDied)
                {
                    SelfDestructInitiateAt = UnityEngine.Time.realtimeSinceStartup;
                    SelfDestructActuallyAt = UnityEngine.Time.realtimeSinceStartup + Instance.configData.SelfDestructTimer;
                }

                SelfDestructionInitiated = true;

                if (Instance.configData.NotifyAboutSelfDestruct)
                {
                    UpdateCurrentTimespan();
                    Instance.TellMessage(null, MSG(MSG_EVENT_SELF_DESTRUCT_INITIATED, null, GetFormattedTimeLeft()));
                }

                PowerStuffUp(false);

                Interface.CallHook("OnTrainSelfDestructionInitiated", Train, Instance.configData.SelfDestructTimer, becauseDriverDied);

            }

            public void SelfDestructionActually()
            {
                SelfDestructionInitiated = false;
                SelfDestructionShouldHappenNow = true;

                Effect.server.Run(FX_C4_EXPLOSION, transform.position);
                Effect.server.Run(FX_EXPLOSION_01, transform.position);
                Effect.server.Run(FX_EXPLOSION_02, transform.position);
                Effect.server.Run(FX_EXPLOSION_03, transform.position);

                DamageUtil.RadiusDamage(null, null, transform.position, 0F, Instance.configData.SelfDestructDamageRadius, Instance.GetSelfDestructionDamage(), 1076005121, false);


                Instance.NextTick(() => Instance.MakeTrainNormal(TrainNetID));
            }

            private void OnDestroy()
            {
                TrainDriverCleanup();

                WorkcartEntitiesCleanup();

                if (TruePVEBubbleGameObject != null)
                {
                    Destroy(TruePVEBubbleGameObject);
                }

                /*
                if (TrainCrate != null)
                {
                    if (!TrainCrate.IsDestroyed)
                    {
                        TrainCrate.Kill(BaseNetworkable.DestroyMode.None);
                    }
                }*/
                if (RadiationTrigger != null)
                {
                    Destroy(RadiationTrigger);
                }
                if (RadiationCollider != null)
                {
                    Destroy(RadiationCollider);
                }
                if (RadiationGameObject != null)
                {
                    Destroy(RadiationGameObject);
                }

                if (Train != null)
                {
                    if (!Train.IsDestroyed)
                    {
                        Instance.DisableInfiniteFuel(Train);

                        Train.Invoke(() => Train.Kill(BaseNetworkable.DestroyMode.Gib), 0.2F);
                    }
                }


                TrainNetIDToSpecialTrain.Remove(TrainNetID);
            }
        }
        #endregion

        #region API/HELPERS
        private static void GiveBotKit(BasePlayer player, string kitName)
        {
            if (player == null)
            {
                return;
            }

            if (player.net == null)
            {
                return;
            }

            player.inventory.Strip();
            Instance.Kits?.Call("GiveKit", player, kitName);

            var htn = player as ScientistNPC;
            if (htn != null)
            {
                Item item = htn.inventory.containerBelt.GetSlot(0);
                if (item == null)
                    return;
                htn.svActiveItemID = item.uid;
                htn.UpdateActiveItem(new ItemId(item.uid.Value));
                var held = item.GetHeldEntity();
                if (held != null)
                    (held as HeldEntity)?.SetHeld(true);
                htn.inventory.UpdatedVisibleHolsteredItems();
                htn.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            }
            if (player == null)
                return;
            if (player is global::HumanNPC)
                (player as global::HumanNPC).EquipWeapon();
            if (player is NPCPlayer)
                ((NPCPlayer)player).EquipWeapon();
        }

        //TRUE PVE COMPATIBILITY
        //return true to let the damage through (make the method calling the hook return null)
        private object CanEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            if (Instance == null) return null;

            //Instance.PrintToChat($"{entity.ShortPrefabName} is taking damage...");

            var maybePlayerVictim = entity as BasePlayer;

            if (maybePlayerVictim == null) return null;

            bool victimIsInPVP = false;
            bool attackerIsInPVP = false;

            //check if the two players are near a special train...
            if (SpecialTrain.UserIDToSpecialTrains.ContainsKey(maybePlayerVictim.userID))
            {
                victimIsInPVP = true;
            }

            if (hitinfo.InitiatorPlayer != null)
            {
                if (SpecialTrain.UserIDToSpecialTrains.ContainsKey(hitinfo.InitiatorPlayer.userID))
                {
                    attackerIsInPVP = true;
                }
            }

            if (victimIsInPVP || attackerIsInPVP)
            {
                return true;
            }

            return null;
        }

        private void FuelManagerCheck()
        {
            timer.Once(0.1F, () =>
            {
                if (FuelManager?.IsLoaded ?? false)
                {
                    PrintWarning(MSG(MSG_FUEL_MANAGER_PRESENT, null, FuelManager.Version));
                    /*
                    Unsubscribe(nameof(CanUseFuel));
                    Unsubscribe(nameof(OnFuelAbstract));
                    Unsubscribe(nameof(OnFuelAmountCheck));
                    Unsubscribe(nameof(OnFuelCheck));
                    */
                }
                else
                {
                    /*
                    PrintWarning(MSG(MSG_FUEL_MANAGER_NOT_PRESENT));
                    Subscribe(nameof(CanUseFuel));
                    Subscribe(nameof(OnFuelAbstract));
                    Subscribe(nameof(OnFuelAmountCheck));
                    Subscribe(nameof(OnFuelCheck));
                    */
                }
            });
        }

        private void DeathNotesCheck()
        {
            /*
            timer.Once(0.5F, () =>
            {
                if (DeathNotes?.IsLoaded ?? false)
                {
                    Subscribe(nameof(OnDeathNotice));
                }
                else
                {
                    Unsubscribe(nameof(OnDeathNotice));
                }
            });
            /*/
            //nope, just unsubscribe
            Unsubscribe(nameof(OnDeathNotice));
        }

        public bool TruePVEIsLoaded;

        private void TruePVECheck()
        {
            timer.Once(0.1F, () =>
            {
                TruePVEIsLoaded = TruePVE?.IsLoaded ?? false;

                if (TruePVEIsLoaded)
                {
                    Subscribe(nameof(CanEntityTakeDamage));
                }
                else
                {
                    Unsubscribe(nameof(CanEntityTakeDamage));
                }
            });
        }

        public string StripTags(string inputString)
        {
            return System.Text.RegularExpressions.Regex.Replace(inputString, "<[^>]+>|</[^>]+>", string.Empty);
        }

        public Dictionary<string, ulong> BuiltInVisualKitWear = new Dictionary<string, ulong>
        {
            ["scientistsuit_heavy"] = 0,
        };

        public Dictionary<string, ulong> BuiltInVisualKitBelt = new Dictionary<string, ulong>();

        private List<Rust.DamageTypeEntry> GetSelfDestructionDamage()
        {

            return new List<Rust.DamageTypeEntry>
            {
                new Rust.DamageTypeEntry
                    {
                        amount = Instance.configData.SelfDestructDamageAmount,
                        type = Rust.DamageType.Explosion
                    }
            };
        }

        private void ScheduleTrainEventAtRandom()
        {
            if (configData.EnableRandomEvent)
            {
                ScheduleTrainEventIn(UnityEngine.Random.Range(configData.EventRandomTimerMin, configData.EventRandomTimerMax));
            }
            else
            {
                PrintWarning(MSG(MSG_EVENT_SCHEDULE_DISABLED, null));
            }
        }

        private void ScheduleTrainEventIn(float seconds)
        {
            if (EventTimer != null)
            {
                EventTimer.Destroy();
                EventTimer = null;
            }

            EventTimer = timer.Once(seconds, () =>
            {
                TrainEventRun();
                ScheduleTrainEventAtRandom();
            });

            PrintWarning(MSG(MSG_EVENT_SCHEDULED_IN, null, seconds.ToString("0.00"), (seconds / 60F).ToString("0.00")));
        }

        private void GivePlayerBuiltInVisualKit(BasePlayer player, bool clearInventoryBeforehand = true)
        {
            if (clearInventoryBeforehand)
            {
                player.inventory.Strip();
            }

            foreach (var entry in BuiltInVisualKitBelt)
            {
                ItemManager.CreateByName(entry.Key, 1, entry.Value)?.MoveToContainer(player.inventory.containerBelt);
            }

            foreach (var entry in BuiltInVisualKitWear)
            {
                ItemManager.CreateByName(entry.Key, 1, entry.Value)?.MoveToContainer(player.inventory.containerWear);
            }
        }

        private List<TrainEngine> GetEligibleTrains(bool skipAllChecks = false)
        {
            if (skipAllChecks)
            {
                return SpecialTrain.TrainNetIDToTrainEngine.Values.ToList();
            }

            List<TrainEngine> list = new List<TrainEngine>();
            foreach (var train in SpecialTrain.TrainNetIDToTrainEngine)
            {

                //ignore trains that are special
                if (IsTrainSpecial(train.Key))
                {
                    continue;
                }

                //ignore trains that have an engine on
                if (train.Value.engineController.IsOn)
                {
                    continue;
                }

                //ignore trains that have an engine starting
                if (train.Value.engineController.IsStarting)
                {
                    continue;
                }

                //ignore trains that have drivers
                if (train.Value.HasDriver())
                {
                    continue;
                }

                if (train.Value.transform.position.y < Instance.configData.EventTrainAltitudeMin)
                {
                    continue;
                }

                if (train.Value.transform.position.y > Instance.configData.EventTrainAltitudeMax)
                {
                    continue;
                }

                //ignore trains that are moving
                if (train.Value.rigidBody.velocity.magnitude != 0F) continue;

                //ignore trains that have any base entities in their platform trigger
                if (train.Value.platformParentTrigger.HasAnyEntityContents)
                {
                    if (train.Value.platformParentTrigger.entityContents.Count() > 0)
                    {
                        if (train.Value.platformParentTrigger.entityContents.Where(e => e is BasePlayer).Count() > 0)
                        {
                            continue;
                        }
                    }
                }

                if (train.Value.InSafeZone())
                {
                    continue;
                }

                list.Add(train.Value);
            }

            return list;
        }


        private bool TrainEventRun()
        {
            //pick a random train from the ones at station - if there are any!
            var candidates = GetEligibleTrains();

            if (candidates.Count == 0)
            {
                return false;
            }

            var randomCandidate = candidates[UnityEngine.Random.Range(0, candidates.Count)];

            return MakeTrainSpecial(randomCandidate);
        }

        private bool IsTrainSpecial(ulong trainNetID)
        {
            if (!SpecialTrain.TrainNetIDToSpecialTrain.ContainsKey(trainNetID)) return false;

            return true;
        }

        private bool MakeTrainSpecial(TrainEngine train)
        {
            if (train == null) return false;

            if (IsTrainSpecial(train.net.ID.Value))
            {
                return false;
            }

            train.gameObject.AddComponent<SpecialTrain>().Prepare(train);

            return true;
        }

        private void EnableInfiniteFuel(TrainEngine train)
        {
            if (Instance == null)
            {
                return;
            }

            if (train == null)
            {
                return;
            }

            if (train.net == null)
            {
                return;
            }

            var container = train.engineController.FuelSystem.fuelStorageInstance.Get(true);

            ItemManager.CreateByItemID(ITEM_LOWGRADE, int.MaxValue - 1, 0).MoveToContainer(container.inventory, -1, true);

            container.SetFlag(BaseEntity.Flags.Locked, true, false, true);
        }

        private void DisableInfiniteFuel(TrainEngine train)
        {
            if (Instance == null)
            {
                return;
            }

            if (train == null)
            {
                return;
            }

            if (train.net == null)
            {
                return;
            }

            var container = train.engineController.FuelSystem.fuelStorageInstance.Get(true);

            if (container == null)
            {
                return;
            }

            container.inventory.Clear();

            container.SetFlag(BaseEntity.Flags.Locked, false, false, true);
        }

        private bool MakeTrainNormal(ulong trainNetID)
        {

            if (!IsTrainSpecial(trainNetID))
            {
                return false;
            }

            Interface.CallHook("OnTrainEventEnded", SpecialTrain.TrainNetIDToSpecialTrain[trainNetID].Train);

            UnityEngine.Object.DestroyImmediate(SpecialTrain.TrainNetIDToSpecialTrain[trainNetID]);

            if (Instance.configData.NotifyWhenEnded)
            {
                TellMessage(null, MSG(MSG_EVENT_OVER));
            }


            return true;
        }
        #endregion

        #region CHAT
        [ChatCommand("te_cfg")]
        private void ChatCommandTeCFG(BasePlayer player, string command, string[] args)
        {
            if (!(player.IsAdmin || player.IsDeveloper || permission.UserHasPermission(player.UserIDString, PERM_ADMIN))) return;

            if (args.Length == 0)
            {
                //display all possible config keys and their values
                ReusableString = $"{MSG(MSG_CFG_DEFAULT, player.UserIDString)}\n";
                foreach (var entry in ConfigValues)
                {
                    ReusableString += $"{MSG(MSG_CFG_RUNDOWN_FORMAT, player.UserIDString, entry.Key, entry.Value.formatter(entry.Value.GetSet))}\n";
                }

                TellMessage(player, ReusableString);
            }
            else
            {

                if (ConfigValues.ContainsKey(args[0]))
                {
                    //has argument[1] been provided? if not, display the full description.
                    if (args.Length > 1)
                    {
                        ConfigValues[args[0]].GetSet = args[1];
                    }
                    else
                    {
                        TellMessage(player, $"{MSG(MSG_CFG_DETAILS_FORMAT, player.UserIDString, ConfigValues[args[0]].name, ConfigValues[args[0]].description, ConfigValues[args[0]].valueType, ConfigValues[args[0]].formatter(ConfigValues[args[0]].GetSet))}. Accepted values are {ConfigValues[args[0]].FormatAcceptable()}");
                    }

                }
                else
                {
                    TellMessage(player, MSG(MSG_CFG_NO_SETTING_FOUND, player.UserIDString));
                }
            }
        }
        #endregion

        #region CONSOLE
        [ConsoleCommand("dumpCFG")]
        private void ConsoleCommandDumpCFG(ConsoleSystem.Arg arg)
        {
            string buildie = "";

            foreach (var entry in ConfigValues)
            {
                buildie += $"/te_cfg <strong>{StripTags(entry.Key)}</strong> <em>[{StripTags(entry.Value.FormatAcceptable())}]\n{StripTags(entry.Value.description)} (DEFAULT: {entry.Value.GetSet})</em>\n\n";
            }

            Instance.PrintWarning(buildie);
        }

        [ConsoleCommand("trainevent_now_at")]
        private void ConsoleCommandTrainEventAt(ConsoleSystem.Arg arg)
        {
            bool playerIsNull = false;
            bool argsProvided = false;

            if (arg.Player() != null)
            {
                if (!(arg.Player().IsAdmin || arg.Player().IsDeveloper || permission.UserHasPermission(arg.Player().UserIDString, PERM_ADMIN))) return;
            }
            else
            {
                playerIsNull = true;
            }

            if (arg.HasArgs(3))
            {
                argsProvided = true;
            }

            Vector3 pos = Vector3.zero;

            if (argsProvided)
            {
                float x, y, z = float.MinValue;

                if (!float.TryParse(arg.Args[0], out x)) return;
                if (!float.TryParse(arg.Args[1], out y)) return;
                if (!float.TryParse(arg.Args[2], out z)) return;

                pos = new Vector3(x, y, z);
            }
            else
            {
                if (playerIsNull)
                {
                    PrintError("ERROR: If you're running this command from the console, you need to provide X Y Z values as args!");
                    return;
                }
                else
                {
                    pos = arg.Player().transform.position;
                }
            }            

            TrainEngine newTrain = GameManager.server.CreateEntity(PREFAB_TRAIN, pos) as TrainEngine;

            newTrain.Spawn();

            Instance.NextFrame(() =>
            {
                MakeTrainSpecial(newTrain);
            });
        }

        [ConsoleCommand("trainevent_now")]
        private void ConsoleCommandTrainEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null)
            {
                if (!(arg.Player().IsAdmin || arg.Player().IsDeveloper || permission.UserHasPermission(arg.Player().UserIDString, PERM_ADMIN))) return;
            }

            TrainEventRun();
        }

        [ConsoleCommand("trainevent_schedule")]
        private void ConsoleCommandTrainEventSchedule(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null)
            {
                if (!(arg.Player().IsAdmin || arg.Player().IsDeveloper || permission.UserHasPermission(arg.Player().UserIDString, PERM_ADMIN))) return;
            }

            if (arg.HasArgs())
            {
                float maybeFloat;
                if (float.TryParse(arg.Args[0], out maybeFloat))
                {
                    ScheduleTrainEventIn(maybeFloat);
                }
            }
            else
            {
                ScheduleTrainEventAtRandom();
            }

        }


        [ConsoleCommand("trainevent_now_nearest")]
        private void ConsoleCommandNearest(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
            {
                PrintError("ERROR: This command is supposed to be ran by an admin/moderator from the F1 console, not the server console!");
                return;
            }

            if (!(arg.Player().IsAdmin || arg.Player().IsDeveloper || permission.UserHasPermission(arg.Player().UserIDString, PERM_ADMIN))) return;

            var nearestTrain = GetEligibleTrains(true).OrderBy(t => Vector3.Distance(arg.Player().transform.position, t.transform.position));

            if (nearestTrain.Count() > 0)
            {
                var firstie = nearestTrain.First();
                MakeTrainSpecial(firstie);
            }
        }

        #endregion

        #region DEBUG
        //[ChatCommand("debug")]
        private void ChatCommandDebug(BasePlayer player, string command, string[] args)
        {
            player.ChatMessage($"CLIPPING: {(GamePhysics.CheckOBB(player.WorldSpaceBounds(), 1210122497, QueryTriggerInteraction.Ignore))}. EXCLUSION TRIGGER: {player.FindTrigger<TriggerParentExclusion>() != null}, SHOULD PARENT: {player.GetParentEntity()?.ShortPrefabName ?? "NO PARENT"}");
        }

        object OnEntityEnter(TriggerBase triggerBase, BaseEntity entity)
        {
            if (Instance == null)
            {
                return null;
            }

            if (entity == null)
            {
                return null;
            }

            if (entity.net == null)
            {
                return null;
            }

            if (!SpecialTrain.PlatformTriggerToTrain.ContainsKey(triggerBase)) return null;

            var maybePlayer = entity as BasePlayer;
            if (maybePlayer == null) return null;
            //if (maybePlayer.IsNpc) return null;

            SpecialTrain.PlatformTriggerToTrain[triggerBase].OnEntityEnterTrainTriggerReplacement(maybePlayer);

            maybePlayer.transform.hasChanged = true;
            maybePlayer.SendNetworkUpdateImmediate();

            return true;
        }

        #endregion
    }
}
