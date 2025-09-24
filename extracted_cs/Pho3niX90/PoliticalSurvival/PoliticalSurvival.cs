using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins {
    [Info("PoliticalSurvival", "Pho3niX90", "0.7.5")]
    [Description("Political Survival - Become the ruler, tax your subjects and keep them in line!")]
    class PoliticalSurvival : RustPlugin {
        bool firstRun = false;
        public bool DebugMode = false;
        Ruler ruler;
        PSConfig config;
        private Core.Libraries.Time _time = GetLibrary<Core.Libraries.Time>();
        static PoliticalSurvival _instance;
        private List<Ruler> rulerList = new List<Ruler>();
        string ConfigVersion = "0.0.4";


        #region Settings Class

        public class TaxSource {
            public bool DispenserGather;
            public bool CropGather;
            public bool DispenserBonus;
            public bool QuarryGather;
            public bool ExcavatorGather;
            public bool CollectiblePickup;
            public bool SurveyGather;

            public TaxSource createDefault() {
                DispenserGather = true;
                CropGather = true;
                DispenserBonus = true;
                QuarryGather = true;
                ExcavatorGather = true;
                CollectiblePickup = true;
                SurveyGather = true;
                return this;
            }
        }

        public class PSConfig {
            public string Version;
            public bool showWelcomeMsg;
            public int maxHelis;

            public int heliItemCost;
            public int heliItemCostQty;

            public bool broadcastRulerPosition;
            public int broadcastRulerPositionAfter;
            public int broadcastRulerPositionAfterPercentage;

            public int taxMin;
            public int taxMax;

            public TaxSource taxSource;

            public int worldSize;
            public bool chooseNewRulerOnDisconnect;
            public int chooseNewRulerOnDisconnectMinutes;
            public bool rulerCanChooseAnotherRuler = true;
        }

        public class Ruler {
            public Vector3 taxContainerVector3;
            public uint taxContainerID;
            public double tax;
            public ulong ruler;
            public string rulerName;
            public ulong rulerId;
            public uint rulerSince;
            public int resourcesGot;
            public string realm;

            public Ruler(Vector3 tcv4, uint txId, double tx, ulong rlr, string rlrname, string rlm, ulong rid) {
                taxContainerVector3 = tcv4;
                taxContainerID = txId;
                tax = tx;
                ruler = rlr;
                rulerName = rlrname;
                realm = rlm;
                rulerId = rid;
            }

            public Ruler() { }

            public int GetResourceCount() {
                return resourcesGot;
            }

            public Ruler SetRulerSince(uint since) {
                rulerSince = since;
                return this;
            }

            public Ruler SetResourcesGot(int amnt) {
                resourcesGot = amnt;
                return this;
            }

            public long GetRulerSince() {
                return rulerSince;
            }

            public Ruler SetTaxContainerVector3(Vector3 vec) {
                taxContainerVector3 = vec;
                return this;
            }

            public Vector3 GetTaxContainerVector3() {
                return taxContainerVector3;
            }
            public Ruler SetTaxContainerID(uint storage) {
                taxContainerID = storage;
                return this;
            }
            public uint GetTaxContainerID() {
                return taxContainerID;
            }
            public Ruler SetTaxLevel(double tx) {
                tax = tx;
                return this;
            }
            public double GetTaxLevel() {
                return tax;
            }
            public Ruler SetRuler(ulong rlr) {
                ruler = rlr;
                rulerId = rlr;
                rulerSince = (new Core.Libraries.Time()).GetUnixTimestamp();
                return this;
            }
            public ulong GetRuler() {
                return ruler;
            }
            public double GetRuleLengthInMinutes() {
                return (new Core.Libraries.Time().GetUnixTimestamp() - rulerSince) / 60.0;
            }
            public double GetRulerOfflineMinutes() {
                return _instance.rulerOfflineAt == 0 ? 0.0 : ((new Core.Libraries.Time().GetUnixTimestamp() - _instance.rulerOfflineAt) / 60.0);
            }
            public Ruler SetRulerName(string name) {
                rulerName = name;
                return this;
            }
            public string GetRulerName() {
                return rulerName;
            }
            public Ruler SetRealmName(string rlm) {
                realm = rlm;
                return this;
            }
            public string GetRealmName() {
                return realm;
            }
        }
        #endregion

        #region Components

        #region Heli Vars
        public int HeliLifeTimeMinutes = 5;
        public float HeliBaseHealth = 50000.0f;
        public float HeliSpeed = 50f;
        public float HeliSpeedMax = 200f;
        public int NumRockets = 50;
        public float ScanFrequencySeconds = 5;
        public float TargetVisible = 1000;
        public float MaxTargetRange = 300;
        public bool NotifyPlayers = true;
        public BasePlayer target;
        #endregion

        class HeliComponent : FacepunchBehaviour {
            private BaseHelicopter heli;
            private PatrolHelicopterAI AI;
            private bool isFlying = true;
            private bool isRetiring = false;
            float timer;
            float timerAdd;

            void Awake() {
                heli = GetComponent<BaseHelicopter>();
                AI = heli.GetComponent<PatrolHelicopterAI>();
                heli.startHealth = _instance.HeliBaseHealth;
                AI.maxSpeed = Mathf.Clamp(_instance.HeliSpeed, 0.1f, _instance.HeliSpeedMax);
                AI.numRocketsLeft = _instance.NumRockets;

                attachGuns(AI);
                timerAdd = (Time.realtimeSinceStartup + Convert.ToSingle(_instance.HeliLifeTimeMinutes * 60));
                InvokeRepeating("ScanForTargets", _instance.ScanFrequencySeconds, _instance.ScanFrequencySeconds);
            }

            void FixedUpdate() {
                timer = Time.realtimeSinceStartup;

                if (timer >= timerAdd && !isRetiring) {
                    isRetiring = true;
                }
                if (isRetiring && isFlying) {
                    CancelInvoke("ScanForTargets");
                    isFlying = false;
                    heliRetire();
                }
            }

            internal void ScanForTargets() {
                foreach (ulong targetSteamId in _instance.target.Team.members) {
                    BasePlayer teamMemberToAttack = BasePlayer.Find(targetSteamId.ToString());

                    if (teamMemberToAttack.IsConnected) {
                        UpdateTargets(teamMemberToAttack);
                        _instance.DebugLog("Heli target found " + teamMemberToAttack);
                    }
                    UpdateAi();
                }
            }

            void UpdateAi() {
                _instance.DebugLog("Heli updating AI");
                AI.UpdateTargetList();
                AI.MoveToDestination();
                AI.UpdateRotation();
                AI.UpdateSpotlight();
                AI.AIThink();
                AI.DoMachineGuns();
            }

            void UpdateTargets(BasePlayer Player) {
                AI._targetList.Add(new PatrolHelicopterAI.targetinfo((BaseEntity)Player, Player));
            }

            internal void attachGuns(PatrolHelicopterAI helicopter) {
                if (helicopter == null) return;
                var guns = new List<HelicopterTurret>();
                guns.Add(helicopter.leftGun);
                guns.Add(helicopter.rightGun);
                for (int i = 0; i < guns.Count; i++) {
                    // Leave these as hardcoded for now
                    var turret = guns[i];
                    turret.fireRate = 0.125f;
                    turret.timeBetweenBursts = 3f;
                    turret.burstLength = 3f;
                    turret.maxTargetRange = _instance.MaxTargetRange;
                }
            }

            internal void heliRetire() {
                AI.Retire();
            }

            public void UnloadComponent() {
                Destroy(this);
            }

            void OnDestroy() {
                CancelInvoke("ScanForTargets");
            }
        }
        #endregion

        #region Variables
        Dictionary<string, string> serverMessages;
        int worldSize = 3500;
        BasePlayer currentRuler;
        uint rulerOfflineAt = 0;
        private ILocator liveLocator = null;
        private ILocator locator = null;
        private bool Changed = false;
        protected Dictionary<string, Timer> Timers { get; } = new Dictionary<string, Timer>();
        #endregion

        private void Init() {
            config = Config.ReadObject<PSConfig>();
            LoadServerMessages();

            if (config != null && !config.Version.Equals(ConfigVersion)) {
                Puts("Config outdated, will update to new version.");
                config = UpgradeConfig(config.Version, ConfigVersion);
                SaveConfig();
            }
        }

        private void Loaded() {
            LoadRuler();
            _instance = this;

            Puts("Political Survival is starting...");
            if (ConVar.Server.worldsize == 0)
                Puts("WARNING: worldsize is reporting as 0, this is not possible and will default to config size. Please make sure the config has the correct size.");
            if (ConVar.Server.worldsize > 0) worldSize = ConVar.Server.worldsize;

            liveLocator = new RustIOLocator(worldSize);
            locator = new LocatorWithDelay(liveLocator, 60);


            if (ruler.GetRulerSince() == 0) {
                ruler.SetRulerSince(_time.GetUnixTimestamp());
                SaverRuler();
            }

            Puts("Realm name is " + ruler.GetRealmName());
            Puts("Tax level is " + ruler.GetTaxLevel());
            Puts("TaxChest is set " + !ruler.GetTaxContainerVector3().Equals(Vector3.negativeInfinity));
            Puts("Political Survival: Started");
            currentRuler = GetPlayer(ruler.GetRuler().ToString());
            Puts("Current ruler " + (currentRuler != null ? "is set" : "is null"));
            if (currentRuler != null) Puts("Ruler is " + ruler.GetRuler() + " (" + currentRuler.displayName + ")");

            Timers.Add("AdviseRulerPosition", timer.Repeat(Math.Max(config.broadcastRulerPositionAfter, 60), 0, () => AdviseRulerPosition()));

            SaverRuler();
            Puts($"Ruler offline at {rulerOfflineAt}");
            if (rulerOfflineAt != 0 || currentRuler == null || currentRuler.IsConnected) {
                if (config.chooseNewRulerOnDisconnect && (ruler.GetRulerOfflineMinutes() >= (1 * config.chooseNewRulerOnDisconnectMinutes) || (rulerOfflineAt == 0 && (currentRuler == null || !currentRuler.IsConnected)))) {
                    TryForceNewRuler(true);
                }
            }
        }

        void OnServerShutdown() {
            SaveConfig();
            SaverRuler();
        }

        void Unload() {
            Puts("Unload called");

            SaverRuler();

            foreach (Timer t in Timers.Values)
                t.Destroy();
            Timers.Clear();
        }

        void OnPlayerInit(BasePlayer player) {
            if (config.showWelcomeMsg) PrintToChat(player.displayName + " " + lang.GetMessage("PlayerConnected", this, player.UserIDString) + " " + ruler.GetRealmName());
            if (currentRuler != null && ruler.ruler == currentRuler.userID) {
                rulerOfflineAt = 0;
            }
        }

        void OnPlayerDisconnected(BasePlayer player, string reason) {
            if (config.showWelcomeMsg) PrintToChat(player.displayName + " " + lang.GetMessage("PlayerDisconnected", this, player.UserIDString) + " " + ruler.GetRealmName());
            if (currentRuler != null && player.userID == currentRuler.userID) {
                rulerOfflineAt = _time.GetUnixTimestamp();
                timer.Once(60 * config.chooseNewRulerOnDisconnectMinutes, () => {
                    if (rulerOfflineAt != 0)
                        TryForceNewRuler(true);
                });
            }
        }

        #region GatheringHooks
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item) {
            DebugLog("OnDispenserGather start");
            if (!config.taxSource.DispenserGather || dispenser == null || entity == null || Item == null || ruler.GetTaxContainerID() == 0) return;

            BasePlayer player = entity as BasePlayer;
            DebugLog("OnDispenserGather stage 2 " + item.flags.ToString() + " " + item.amount + " " + player.displayName);
            int netAmount = AddToTaxContainer(item, player.displayName);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }

        private void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item) {
            if (!config.taxSource.DispenserBonus) return;

            BasePlayer player = entity as BasePlayer;
            DebugLog("OnDispenserBonus start");
            int netAmount = AddToTaxContainer(item, player.displayName);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }

        void OnGrowableGather(GrowableEntity plant, Item item, BasePlayer player) {
            if (!config.taxSource.CropGather) return;

            DebugLog("OnPlantGather start");
            int netAmount = AddToTaxContainer(item, player.displayName);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }

        private void OnQuarryGather(MiningQuarry quarry, Item item) {
            DebugLog("OnQuarryGather start");
            if (!config.taxSource.QuarryGather) return;

            int netAmount = AddToTaxContainer(item, quarry.name);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }


        private void OnExcavatorGather(ExcavatorArm excavator, Item item) {
            DebugLog("OnExcavatorGather start");
            if (!config.taxSource.ExcavatorGather) return;

            int netAmount = AddToTaxContainer(item, excavator.name);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }

        private void OnCollectiblePickup(Item item, BasePlayer player) {
            DebugLog("OnCollectiblePickup start");
            if (!config.taxSource.CollectiblePickup) return;

            int netAmount = AddToTaxContainer(item, player.displayName);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }

        private void OnSurveyGather(SurveyCharge surveyCharge, Item item) {
            DebugLog("OnSurveyGather start");
            if (!config.taxSource.SurveyGather) return;

            int netAmount = AddToTaxContainer(item, surveyCharge.name);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }

        #endregion

        int AddToTaxContainer(Item item, string displayName) {
            if (!IsChestSet()) return -1;

            DebugLog("AddToTaxContainer start");
            if (item == null || ruler.GetTaxContainerID() == 0 || ruler.GetRuler() == 0 || ruler.GetTaxLevel() == 0 || ruler.GetTaxContainerVector3() == Vector3.negativeInfinity) return -1;

            ItemDefinition ToAdd = ItemManager.FindItemDefinition(item.info.itemid);
            int Tax = Convert.ToInt32(Math.Ceiling((item.amount * ruler.GetTaxLevel()) / 100));

            ItemContainer container = FindStorageContainer(ruler.GetTaxContainerID()).inventory;
            if (ToAdd != null && container != null) {
                if (item.CanMoveTo(container)) {
                    container.AddItem(ToAdd, Tax);
                    ruler.resourcesGot += Tax;
                }
            }

            DebugLog("User " + displayName + " gathered " + item.amount + " x " + item.info.shortname + ", and " + Tax + " was taxed");
            DebugLog("items added to tax container");
            return item.amount - Tax;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info) {

            if (entity == null) return;
            BasePlayer player = entity as BasePlayer;

            /* if (entity != null) { //tax oil
                 if (entity.ShortPrefabName == "oil_barrel") {
                     List<DroppedItem> ItemsDropped = new List<DroppedItem>();
                     Vis.Entities<DroppedItem>(entity.transform.position, 1, ItemsDropped);
                     foreach (DroppedItem ditem in ItemsDropped) {
                         if (ditem == null) return;
                         Item item = ditem?.item;
                         Puts($"Player {player.displayName} - {item.info.displayName} x {item.amount}");
                         item.amount = AddToTaxContainer(item, player.displayName);
                     }
                 }
             }*/

            if (player != null) {
                if (IsRuler(player.userID)) {
                    BasePlayer killer = null;
                    if (info != null)
                        killer = info.Initiator as BasePlayer;

                    if (killer != null && killer.userID != player.userID && !(killer is NPCPlayer)) {
                        SetRuler(killer);
                        PrintToChat(string.Format(lang.GetMessage("RulerMurdered", this), killer.displayName));
                    } else {
                        ruler.SetRuler(0).SetRulerName(null);
                        currentRuler = null;
                        PrintToChat(string.Format(lang.GetMessage("RulerDied", this)));
                    }
                    SaverRuler();
                }
            }
        }

        public void TryForceRuler() {
            if (currentRuler == null && TryForceNewRuler(false))
                PrintToChat("<color=#008080ff>{0}</color> has been made the new Ruler. Kill him!", currentRuler.displayName);
        }
        #region Commands
        [ConsoleCommand("ps.fnr")]
        private void SuicideHandler(ConsoleSystem.Arg arg) {
            TryForceRulerCmd(arg.Connection.player as BasePlayer, null, arg.Args);
        }
        [ChatCommand("fnr")]
        void TryForceRulerCmd(BasePlayer player, string command, string[] args) {

            if (!player.IsAdmin) {
                Puts($"Player {player.displayName} tried using fnr");
                return;
            }
            //if (player != null && !player.IsAdmin && !player.isServer && (!IsRuler(player.userID) || !config.rulerCanChooseAnotherRuler)) return;

            if (args.Length == 0) {
                if (TryForceNewRuler(true)) {
                    PrintToChat("<color=#008080ff>{0}</color> has been made the new Ruler. Kill him!", currentRuler.displayName);
                } else {
                    PrintToChat("Couldn't force a new ruler :(");
                }
            } else if (args.Length == 1) {
                BasePlayer ruler = null;
                try {
                    ruler = BasePlayer.Find(args[0]);
                } catch (Exception e) {
                    if (player != null)
                        PrintToChat(player, "ERR: " + lang.GetMessage("PlayerNotFound", this), args[0]);
                    return;
                }

                if (ruler == null) { PrintToChat(lang.GetMessage("PlayerNotFound", this), args[0]); return; }
                SetRuler(ruler);
                PrintToChat("<color=#008080ff>{0}</color> has been made the new Ruler. Kill him!", currentRuler.displayName);
            }
        }

        [ChatCommand("heli")]
        void HeliCommmand(BasePlayer player, string command, string[] args) {
            if (!IsRuler(player.userID)) { PrintToChat(player, "You aren't the boss"); return; }
            if (args.Length != 1) { PrintToChat(player, "Usage '/heli player' where player can also be partial name"); return; }

            BasePlayer playerToAttack = GetPlayer(args[0]);
            if (playerToAttack == null) { PrintToChat(player, lang.GetMessage("PlayerNotFound", this), args[0]); return; }

            Puts("Can afford heli?");
            if (!CanAffordheliStrike(player)) {
                PrintToChat(player, "Ordering a heli strike costs {0} {1}", config.heliItemCostQty, ItemManager.FindItemDefinition(config.heliItemCost).displayName.english); return;
            }

            int heliCount = UnityEngine.Object.FindObjectsOfType<BaseHelicopter>().Count();
            if (heliCount >= config.maxHelis) {
                PrintToChat(player, "Insufficient airspace for more than {0} helicopters, please wait for existing patrols to complete", config.maxHelis); return;
            }

            Puts("OrderheliStrike");
            OrderheliStrike(playerToAttack);
            PrintToChat(player, "The heli is inbound");
        }

        [ChatCommand("taxrange")]
        void AdmSetTaxChestCommand(BasePlayer player, string command, string[] args) {
            if (player.IsAdmin && args.Length == 2) {
                int taxMin = 0;
                int taxMax = 10;
                int.TryParse(args[0], out taxMin);
                int.TryParse(args[1], out taxMax);
                config.taxMin = taxMin;
                config.taxMax = taxMax;
                PrintToChat(player, $"Tax range set to Min:{config.taxMin}% - Max:{config.taxMax}%");
                SaveConfig();
                SaverRuler();
            }
        }

        [ChatCommand("settaxchest")]
        void SetTaxChestCommand(BasePlayer player, string command, string[] arguments) {
            if (!IsRuler(player.userID)) {
                SendReply(player, lang.GetMessage("RulerError", this, player.UserIDString));
                return;
            }
            var layers = LayerMask.GetMask("Deployed");
            RaycastHit hit = new RaycastHit();
            DebugLog("Test 1");
            if (Player != null && Physics.Raycast(player.eyes.HeadRay(), out hit, 50, layers)) {
                DebugLog("Test 2");
                BaseEntity entity = hit.GetEntity();
                if (entity != null && (entity.ShortPrefabName.Contains("box.wooden") || entity.ShortPrefabName.Contains("cupboard.tool.deployed"))) {
                    DebugLog("Test 3");
                    Vector3 boxPosition = entity.transform.position;
                    StorageContainer boxStorage = FindStorageContainer(boxPosition);

                    if (boxStorage != null) {
                        DebugLog("Test 4");
                        ruler.SetTaxContainerVector3(boxPosition).SetTaxContainerID(entity.net.ID);

                        if (entity.ShortPrefabName.Contains("box.wooden")) {
                            entity.skinID = 1482844040; //https://steamcommunity.com/sharedfiles/filedetails/?id=1482844040&searchtext=
                            entity.SendNetworkUpdate();
                        }

                        DebugLog("Chest set");
                        SaverRuler();
                        SendReply(player, lang.GetMessage("SetNewTaxChest", this, player.UserIDString));
                    }
                } else {
                    DebugLog("Looking at " + entity.ShortPrefabName);
                    SendReply(player, lang.GetMessage("SetNewTaxChestNotFound", this, player.UserIDString));
                    SendReply(player, lang.GetMessage("SettingNewTaxChest", this, player.UserIDString));
                }
            } else {
                SendReply(player, lang.GetMessage("SetNewTaxChestNotFound", this, player.UserIDString));
                SendReply(player, lang.GetMessage("SettingNewTaxChest", this, player.UserIDString));
            }
        }

        [ChatCommand("tax")]
        void InfoCommand2(BasePlayer player, string command, string[] arguments) {
            InfoCommand(player, command, arguments);
        }
        [ChatCommand("rinfo")]
        void InfoCommand(BasePlayer player, string command, string[] arguments) {
            string RulerName = string.Empty;

            if (ruler.GetRuler() > 0) {
                BasePlayer BaseRuler = BasePlayer.FindAwakeOrSleeping(ruler.GetRuler().ToString());
                RulerName = BaseRuler != null ? BaseRuler.displayName : lang.GetMessage("ClaimRuler", this, player.UserIDString);
            } else {
                RulerName = lang.GetMessage("ClaimRuler", this, player.UserIDString);
            }


            if (ruler.GetRuler() != 0) {
                SendReply(player, "<color=#008080ff>" + lang.GetMessage("InfoRuler", this, player.UserIDString) + ": </color>" + ruler.GetRulerName());
            } else {
                SendReply(player, lang.GetMessage("ClaimRuler", this, player.UserIDString) + ": " + ruler.GetRulerName());
            }
            SendReply(player, "<color=#008080ff>" + lang.GetMessage("InfoRealmName", this, player.UserIDString) + ": </color>" + ruler.GetRealmName());
            SendReply(player, "<color=#008080ff>" + lang.GetMessage("InfoTaxLevel", this, player.UserIDString) + ": </color>" + ruler.GetTaxLevel() + "%" + ((!IsChestSet()) ? " (0%, chest not set)" : ""));
            SendReply(player, "<color=#008080ff>" + lang.GetMessage("InfoRuleLength", this, player.UserIDString) + ": </color>" + Math.Round(ruler.GetRuleLengthInMinutes()) + " minutes");
            SendReply(player, "<color=#008080ff>" + lang.GetMessage("InfoResources", this, player.UserIDString) + ": </color>" + ruler.GetResourceCount());
            if (IsRuler(player.userID)) {
                SendReply(player, lang.GetMessage("SettingNewTaxChest", this, player.UserIDString));
                SendReply(player, string.Format(lang.GetMessage("InfoTaxCmd", this, player.UserIDString), config.taxMin, config.taxMax) + ": " + ruler.GetTaxLevel() + "%");
            }
        }

        [ChatCommand("claimruler")]
        void ClaimRuler(BasePlayer player, string command, string[] arguments) {
            if (currentRuler == null) {
                PrintToChat("<color=#008080ff><b>" + player.displayName + "</b></color> " + lang.GetMessage("IsNowRuler", this));
                SetRuler(player);
            }
        }

        [ChatCommand("settax")]
        void SetTaxCommand(BasePlayer player, string command, string[] args) {
            if (IsRuler(player.userID)) {
                double newTaxLevel = 0.0;
                if (double.TryParse(args[0], out newTaxLevel)) {
                    double oldTax = ruler.GetTaxLevel();
                    if (newTaxLevel == ruler.GetTaxLevel())
                        return;
                    Puts("Tax have been changed by " + player.displayName + " from " + ruler.GetTaxLevel() + " to " + newTaxLevel);
                    Puts($"Tax {config.taxMin} {config.taxMax}");
                    if (newTaxLevel > config.taxMax)
                        newTaxLevel = config.taxMax;
                    else if (newTaxLevel < config.taxMin)
                        newTaxLevel = config.taxMin;

                    SetTaxLevel(newTaxLevel);
                    PrintToChat(string.Format(lang.GetMessage("UpdateTaxMessage", this), oldTax, newTaxLevel));
                }
            } else
                SendReply(player, lang.GetMessage("RulerError", this, player.UserIDString));
        }
        //TODO ended here with case renaming to camelCase
        [ChatCommand("realmname")]
        void RealmNameCommand(BasePlayer player, string command, string[] arguments) {
            if (IsRuler(player.userID)) {
                string NewName = MergeParams(0, arguments);

                if (!String.IsNullOrEmpty(NewName)) {
                    SetRealmName(NewName);
                }
            } else
                SendReply(player, lang.GetMessage("RulerError", this, player.UserIDString));
        }

        [ChatCommand("rplayers")]
        void PlayersCommand(BasePlayer player, string command, string[] arguments) {
            StringBuilder builder = new StringBuilder();
            int playerCount = BasePlayer.activePlayerList.Count;

            builder.Append(string.Format(lang.GetMessage("OnlinePlayers", this), playerCount) + " ");
            List<string> players = new List<string>();

            foreach (BasePlayer pl in BasePlayer.activePlayerList) {
                players.Add("<color=#ff0000ff>" + pl.displayName + "</color>");
            }
            builder.Append(String.Join(", ", players));

            SendReply(player, builder.ToString());
        }
        #endregion

        bool IsPlayerOnline(string partialNameOrID) {
            return GetPlayer(partialNameOrID).IsConnected;
        }

        bool IsChestSet() {
            return ruler.taxContainerID > 0;
        }

        BasePlayer GetPlayer(string partialNameOrID) {
            return BasePlayer.Find(partialNameOrID);
        }

        string MergeParams(int start, string[] paramz) {
            var merged = new StringBuilder();
            for (int i = start; i < paramz.Length; i++) {
                if (i > start)
                    merged.Append(" ");
                merged.Append(paramz[i]);
            }

            return merged.ToString();
        }

        bool IsRuler(ulong steamId) {
            return currentRuler != null && currentRuler.userID == steamId;
        }

        public bool CanAffordheliStrike(BasePlayer player) {
            return player.inventory.GetAmount(config.heliItemCost) >= config.heliItemCostQty;
        }

        public void OrderheliStrike(BasePlayer playerToAttack) {
            // Deduct the cost
            if (currentRuler == null) currentRuler = GetPlayer(ruler.GetRuler().ToString());
            List<Item> collector = new List<Item>();
            currentRuler.inventory.Take(collector, config.heliItemCost, config.heliItemCostQty);

            Puts("Spawn the birdie");
            //spawn the birdie
            BaseHelicopter ent = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(), new Quaternion(), true) as BaseHelicopter;
            if (ent != null && playerToAttack != null) {
                target = playerToAttack;
                ent.GetComponent<PatrolHelicopterAI>().SetInitialDestination(playerToAttack.transform.position + new Vector3(0.0f, 10f, 0.0f), 0.25f);
                ent.Spawn();
                ent.gameObject.AddComponent<HeliComponent>();

                timer.Once(HeliLifeTimeMinutes * 60, () => ent.GetComponent<HeliComponent>().heliRetire());
            }
        }

        void SetRuler(BasePlayer bpruler) {
            Puts("New Ruler! " + bpruler.displayName);
            ruler
                .SetRuler(bpruler.userID)
                .SetRulerName(bpruler.displayName)
                .SetTaxContainerID(0)
                .SetTaxLevel(config.broadcastRulerPositionAfterPercentage)
                .SetTaxContainerVector3(Vector3.negativeInfinity)
                .SetRealmName(GetMsg("DefaultRealm"))
                .SetRulerSince(_time.GetUnixTimestamp())
                .SetResourcesGot(0);
            currentRuler = bpruler;
            SaverRuler();
        }

        void SetTaxLevel(double newTaxLevel) {
            ruler.SetTaxLevel(newTaxLevel);
            SaverRuler();
        }

        void SetRealmName(string newName) {
            if (newName.Length > 36)
                newName = newName.Substring(0, 36);
            PrintToChat(string.Format(lang.GetMessage("RealmRenamed", this), newName));
            ruler.SetRealmName(newName);
            SaverRuler();
        }

        StorageContainer FindStorageContainer(Vector3 position) {
            foreach (StorageContainer cont in StorageContainer.FindObjectsOfType<StorageContainer>()) {
                Vector3 ContPosition = cont.transform.position;
                if (ContPosition == position) {
                    Puts("Tax Container instance found: " + cont.GetEntity().GetInstanceID());
                    ruler.SetTaxContainerID(cont.net.ID);
                    return cont;
                }
            }
            return null;
        }

        StorageContainer FindStorageContainer(uint netid) {
            return (StorageContainer)BaseNetworkable.serverEntities.Find(netid);
        }

        #region Player Grid Coordinates and Locators
        public interface ILocator {
            string GridReference(Vector3 component, out bool moved);
        }

        public class RustIOLocator : ILocator {
            public RustIOLocator(int worldSize) {
                worldSize = (worldSize != 0) ? worldSize : (ConVar.Server.worldsize > 0) ? ConVar.Server.worldsize : 3500;
                translate = worldSize / 2f; //offset
                gridWidth = (worldSize * 0.0066666666666667f);
                scale = worldSize / gridWidth;
            }

            private readonly float translate;
            private readonly float scale;
            private readonly float gridWidth;

            public string GridReference(Vector3 pos, out bool moved) {
                float x = pos.x + translate;
                float z = pos.z + translate;

                int lat = (int)Math.Floor(x / scale); //letter
                char latChar = (char)('A' + lat);
                int lon = (int)Math.Round(gridWidth) - (int)Math.Floor(z / scale); //number
                moved = false; // We dont know, so just return false
                return string.Format("{0}{1}", latChar, lon);
            }
        }

        public class LocatorWithDelay : ILocator {
            public LocatorWithDelay(ILocator liveLocator, int updateInterval) {
                this.liveLocator = liveLocator;
                this.updateInterval = updateInterval;
            }

            private readonly ILocator liveLocator;
            private readonly int updateInterval;
            private readonly Dictionary<Vector3, ExpiringCoordinates> locations = new Dictionary<Vector3, ExpiringCoordinates>();

            public string GridReference(Vector3 pos, out bool moved) {
                ExpiringCoordinates item = null;
                bool m;

                if (locations.ContainsKey(pos)) {
                    item = locations[pos];
                    if (item.Expires < DateTime.Now) {
                        string location = liveLocator.GridReference(pos, out m);
                        item.GridChanged = item.Location != location;
                        item.Location = location;
                        item.Expires = DateTime.Now.AddSeconds(updateInterval);
                    }
                } else {
                    item = new ExpiringCoordinates();
                    item.Location = liveLocator.GridReference(pos, out m);
                    item.GridChanged = true;
                    item.Expires = DateTime.Now.AddSeconds(updateInterval);
                    locations.Add(pos, item);
                }

                moved = item.GridChanged;
                return item.Location;
            }

            class ExpiringCoordinates {
                public string Location { get; set; }
                public bool GridChanged { get; set; }
                public DateTime Expires { get; set; }
            }
        }
        #endregion

        #region Misc
        private MonumentInfo FindMonument(Vector3 pos) {
            MonumentInfo monumentClosest;

            foreach (var monument in TerrainMeta.Path.Monuments) {
                if (monument.name.Contains("oil", CompareOptions.IgnoreCase) || monument.name.Contains("cargo", CompareOptions.IgnoreCase)) {
                    float dist = Vector3.Distance(monument.transform.position, pos);
                    if (dist <= 80) {
                        monumentClosest = monument;
                        return monumentClosest;
                    }
                } else {
                    continue;
                }
            }
            return null;
        }
        #endregion

        #region Timers and Events
        void AdviseRulerPosition() {
            if (currentRuler != null && (config.broadcastRulerPosition || (config.broadcastRulerPositionAfterPercentage > 0 && ruler.GetTaxLevel() > config.broadcastRulerPositionAfterPercentage))) {
                bool moved;

                if (currentRuler == null) return;
                string rulerMonument = FindMonument(currentRuler.transform.position)?.displayPhrase.english;
                string rulerGrid = locator.GridReference(currentRuler.transform.position, out moved);
                string rulerCoords = rulerMonument != null && rulerMonument.Length > 0 ? rulerMonument : rulerGrid;

                if (moved)
                    PrintToChat(GetMsg("RulerLocation_Moved"), currentRuler.displayName, rulerCoords);
                else
                    PrintToChat(GetMsg("RulerLocation_Static"), currentRuler.displayName, rulerCoords);
            }
            /*
            if (config.chooseNewRulerOnDisconnect && (currentRuler == null && BasePlayer.activePlayerList.Count > 0 || ruler.GetRulerOfflineMinutes() > 0 || !currentRuler.IsConnected)) {
                timer.Once(60 * config.chooseNewRulerOnDisconnectMinutes, () => TryForceRuler());
            }
            */
        }

        public bool TryForceNewRuler(bool force) {
            if (currentRuler != null && !force) return false;
            BasePlayer player = GetRandomPlayer();
            if (player != null) {
                SetRuler(player);
                return true;
            }
            return false;
        }

        BasePlayer GetRandomPlayer() {
            ListHashSet<BasePlayer> players = BasePlayer.activePlayerList;
            int activePlayers = players.Count;
            if (activePlayers > 1) {
                return players[Core.Random.Range(0, activePlayers - 1)];
            }
            return activePlayers == 1 ? players.First() : null;
        }

        #endregion
        void SaverRuler() {
            Interface.Oxide.DataFileSystem.WriteObject<Ruler>("PoliticalSurvival", ruler, true);
        }

        void LoadRuler() {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("PoliticalSurvival")) {
                ruler = Interface.Oxide.DataFileSystem.ReadObject<Ruler>("PoliticalSurvival");
                Puts("ruler loaded");
            } else {
                Puts("Settings doesn't exist, creating default");
                ruler = new Ruler()
                .SetRuler(0)
                .SetRealmName(lang.GetMessage("DefaultRealm", this))
                .SetTaxLevel(0.0)
                .SetTaxContainerID(0)
                .SetResourcesGot(0)
                .SetTaxContainerVector3(Vector3.negativeInfinity);
                SaverRuler();
            }
        }

        T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));
        protected override void LoadDefaultConfig() {
            firstRun = true;
            Puts("Creating a new configuration file");
            Config.WriteObject(UpgradeConfig(), true);
            SaveConfig();
        }
        private PSConfig GetDefaultConfig() {
            return UpgradeConfig("", "");
        }
        PSConfig UpgradeConfig(string oldVersion = "", string newVersion = "") {

            if ((!oldVersion.Equals("") || !newVersion.Equals("")) && !oldVersion.Equals(newVersion)) {
                if (newVersion.Equals("0.0.2")) {
                    config.Version = ConfigVersion;
                    config.taxMin = 0;
                    config.taxMax = 35;
                    config.taxSource = new TaxSource().createDefault();
                    SaveConfig();
                    return config;
                }

                if (newVersion.Equals("0.0.4")) {
                    config.worldSize = 3500;
                    config.chooseNewRulerOnDisconnect = true;
                    config.chooseNewRulerOnDisconnectMinutes = 60;
                    config.rulerCanChooseAnotherRuler = true;
                    SaveConfig();
                    return config;
                }
            }
            return new PSConfig {
                Version = ConfigVersion,
                showWelcomeMsg = false,
                maxHelis = 2,
                heliItemCost = 13994,
                heliItemCostQty = 500,
                broadcastRulerPosition = false,
                broadcastRulerPositionAfter = 60,
                broadcastRulerPositionAfterPercentage = 10,
                taxMin = 0,
                taxMax = 35,
                taxSource = new TaxSource().createDefault(),
                worldSize = 3500,
                chooseNewRulerOnDisconnect = true,
                chooseNewRulerOnDisconnectMinutes = 60,
                rulerCanChooseAnotherRuler = true
            };
        }

        void DebugLog(string msg) {
            if (DebugMode) Puts(msg);
        }

        string GetMsg(string msg) => lang.GetMessage(msg, this);
        void LoadServerMessages() {
            serverMessages = new Dictionary<string, string>();
            serverMessages.Add("StartingInformation", "<color=yellow>Welcome to {0}</color>. If you are new, we run a custom plugin where you can become the server Ruler, tax players, and control the economy. Type <color=#008080ff>/rinfo</color> for more information.");
            serverMessages.Add("PlayerConnected", "has connected to");
            serverMessages.Add("PlayerDisconnected", "has disconnected from");
            serverMessages.Add("RulerDied", "<color=#ff0000ff>The Ruler has died!</color>");
            serverMessages.Add("RulerMurdered", "<color=#ff0000ff>The Ruler has been murdered by {0}, who is now the new Ruler.</color>");
            serverMessages.Add("RealmRenamed", "The realm has been renamed to <color=#008080ff>{0}</color>");
            serverMessages.Add("DefaultRealm", "Land of the cursed");
            serverMessages.Add("OnlinePlayers", "Online players ({0}):");
            serverMessages.Add("PrivateError", "is either offline or you typed the name wrong.");
            serverMessages.Add("PrivateFrom", "PM from");
            serverMessages.Add("PrivateTo", "PM sent to");
            serverMessages.Add("RulerError", "You need to be the Ruler to do that!");
            serverMessages.Add("SettingNewTaxChest", "Look at a Wooden box  or TC and type <color=#008080ff>/settaxchest</color>");
            serverMessages.Add("SetNewTaxChestNotFound", "You must look at a wooden box or TC to set tax chest");
            serverMessages.Add("SetNewTaxChest", "You have set the new tax chest.");
            serverMessages.Add("ClaimRuler", "There is no ruler! <color=#008080ff>/claimruler</color> to become the new Ruler!");
            serverMessages.Add("IsNowRuler", "is now the Ruler!");

            serverMessages.Add("InfoRuler", "Ruler");
            serverMessages.Add("InfoRealmName", "Realm Name");
            serverMessages.Add("InfoTaxLevel", "Tax level");
            serverMessages.Add("InfoRuleLength", "Rule Length");
            serverMessages.Add("InfoResources", "Resource received");
            serverMessages.Add("InfoTaxCmd", "Use <color=#008080ff>/settax {0}-{1}</color> to set tax level");

            serverMessages.Add("RulerLocation_Moved", "Ruler <color=#ff0000ff>{0}</color> is on the move, now at <color=#ff0000ff>{1}</color>.");
            serverMessages.Add("RulerLocation_Static", "Ruler <color=#ff0000ff>{0}</color> is camping out at <color=#ff0000ff>{1}</color>");
            serverMessages.Add("UpdateTaxMessage", "The ruler has changed the tax from <color=#ff0000ff>{0}%</color> to <color=#ff0000ff>{1}%</color>");
            serverMessages.Add("PlayerNotFound", "player \"{0}\" not found, or ambiguous");

            lang.RegisterMessages(serverMessages, this);
        }
    }
}