using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("HeavyOilRigEvent", "Cahnu", "1.0.7")]
    class HeavyOilRigEvent : CovalencePlugin
    {
        #region Variables 
        [PluginReference] Plugin TruePVE;
        private bool _isPVE() => TruePVE != null;

        private const string _oilRigPrefab = "assets/bundled/prefabs/autospawn/monument/offshore/oilrig_1.prefab";
        private const string _oilRigScientistPrefab = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab";
        private const string _oilRigHeavyScientistPrefab = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_heavy.prefab";
        private const string _HackableCrateLoot = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab";
        private const string _BradleyApcPrefab = "assets/prefabs/npc/m2bradley/bradleyapc.prefab";
        private const string _autoTurretPlayer = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab";
        private const string _mapMarkerPrefab = "assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab";
        private const string _buttonPrefab = "assets/prefabs/locks/keypad/lock.code.prefab";

        private PluginConfiguration _pluginConfiguration;

        private HashSet<ulong> _npcCodeRevealNetIds = new HashSet<ulong>();
        private HashSet<BaseEntity> _spawnedScientists = new HashSet<BaseEntity>();
        private HashSet<BaseEntity> _spawnedHackableCrates = new HashSet<BaseEntity>();
        private HashSet<BaseEntity> _spawnedBradleys = new HashSet<BaseEntity>();
        private HashSet<BaseEntity> _spawnedTurrets = new HashSet<BaseEntity>();

        private GameObject _oilRigInstance;
        private VendingMachineMapMarker _mapMarker;
        private BaseEntity _codeLock;

        private string _randomCode = string.Empty;

        private bool _isEventRunning;
        private bool _turretsDisabled;
        private bool _isPlayerAtRig;

        private Coroutine _playerAtRigCoroutine;
        private Coroutine _runEventCoroutine;
        private Coroutine _eventTimerCoroutine;
        private Coroutine _eventStateCoroutine;

        private int eventSecondsTime;
        #endregion

        #region Commands
        [Command("HeavyOilStart")]
        private void cmdHeavyOilStart(IPlayer player, string command)
        {
            if (!player.IsAdmin)
                return;

            ServerMgr.Instance.StartCoroutine(RunHeavyOilRigEvent(true));
        }

        [Command("HeavyOilStop")]
        private void cmdHeavyOilStop(IPlayer player, string command)
        {
            if (!player.IsAdmin || !_isEventRunning)
                return;

            StopHeavyOilEvent();
        }

        [Command("HoPOS")]
        private void CmdGetLocalRigPosition(IPlayer player)
        {
            if (!player.IsAdmin)
                return;

            if (!_oilRigInstance)
            {
                player.Reply("no oil rig found on map: ");
                return;
            }

            var position = (player.Object as BasePlayer).transform.position;
            var locoPosition = _oilRigInstance.transform.InverseTransformPoint(position);
            player.Reply("position is: " + locoPosition);
        }
        #endregion

        #region Core
        public enum LootTableType
        {
            Default = 0,
            AlphaLoot = 1,
            Other = 2,
        }

        void CacheRig()
        {
            if (TerrainMeta.Path == null || TerrainMeta.Path.Monuments == null || TerrainMeta.Path.Monuments.Count == 0)
            {
                ServerConsole.print(lang.GetMessage("ErrorLocatingOilRig", this));
                return;
            }

            foreach (var item in TerrainMeta.Path.Monuments)
            {
                //OilrigAI -small
                //OilrigAI2 -large
                if (item.gameObject.name == "OilrigAI2")
                {
                    _oilRigInstance = item.gameObject;
                    break;
                }
            }

            if (!_oilRigInstance)
                ServerConsole.print(lang.GetMessage("ErrorLocatingOilRig", this));

        }

        void OnServerInitialized(bool initial)
        {
            _pluginConfiguration = Config.ReadObject<PluginConfiguration>();
            CacheRig();

            if (_isPVE())
            {
                Subscribe(nameof(CanEntityBeTargeted));
                Subscribe(nameof(CanEntityTakeDamage));
            }

            if (_pluginConfiguration.AutoStart)
                _runEventCoroutine = ServerMgr.Instance.StartCoroutine(RunHeavyOilRigEvent());
        }

        void Unload()
        {
            StopHeavyOilEvent();

            StopCoroutine(_runEventCoroutine);
            StopCoroutine(_playerAtRigCoroutine);
            StopCoroutine(_eventTimerCoroutine);
        }

        private void StopCoroutine(Coroutine coroutine)
        {
            if (coroutine != null)
                ServerMgr.Instance.StopCoroutine(coroutine);
        }

        private int ConnectedPlayersCount()
        {
            var count = 0;

            foreach (var item in players.Connected)
            {
                count++;
            }

            return count;
        }

        private void SetupHeavyOilRig(bool isManual)
        {
            if (!_oilRigInstance)
                return;

            if (_isEventRunning)
            {
                ServerConsole.print(lang.GetMessage("EventAlreadyInProgress", this));
                return;
            }

            if (ConnectedPlayersCount() < _pluginConfiguration.MinimumNumberOfPlayers && !isManual)
            {
                ServerConsole.print(lang.GetMessage("MinimumPlayersNotMet", this));
                return;
            }

            if (_pluginConfiguration.BroadcastStartMessage)
                server.Broadcast(lang.GetMessage("HeavyOilRigStartedBroadcast", this));

            _isEventRunning = true;

            DeployScientists();
            DeployHackableCrates();
            DeployBradley();
            DeployAutoTurrets();
            DeployMapMarker();
            DeployCodeLock();
            SetCodeLockRevealNpc();


            if (isManual)
                ServerConsole.print(lang.GetMessage("HeavyOilRigStartedManual", this));
            else
                ServerConsole.print(lang.GetMessage("HeavyOilRigStarted", this));

            _eventTimerCoroutine = ServerMgr.Instance.StartCoroutine(UpdateEventTime());

            Interface.Oxide.CallHook("HeavyOilRigEventStarted");
        }

        private IEnumerator RunHeavyOilRigEvent(bool isManual = false)
        {
            if (!isManual)
            {
                while (true)
                {
                    var waitTime = UnityEngine.Random.Range(_pluginConfiguration.Minimum * 60, (_pluginConfiguration.Maximum * 60) + 1);

                    yield return CoroutineEx.waitForSeconds(waitTime);

                    if (_pluginConfiguration.SkipEventIfPlayersOnRigAlready && isPlayerAtRig())
                        continue;

                    SetupHeavyOilRig(isManual);
                }
            }
            else
            {
                SetupHeavyOilRig(isManual);
            }
        }

        private IEnumerator UpdateEventTime()
        {
            eventSecondsTime = 0;

            while (_isEventRunning)
            {
                if (eventSecondsTime > (_pluginConfiguration.EventTimeSpan * 60))
                {
                    StopHeavyOilEvent();
                    break;
                }

                yield return CoroutineEx.waitForSeconds(1);

                eventSecondsTime++;
            }
        }

        private bool isPlayerAtRig()
        {
            LayerMask mask = 1 << LayerMask.NameToLayer("Player (Server)");

            var colliders = Physics.OverlapBox(
                _oilRigInstance.transform.position,
                new Vector3(100, 100, 130),
                Quaternion.identity,
                mask);

            foreach (var item in colliders)
            {
                var player = item.gameObject.GetComponent<BasePlayer>();
                if (player && !player.IsNpc)
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator EndEventAfterTime()
        {
            yield return CoroutineEx.waitForSeconds(_pluginConfiguration.EventTimeSpan);

            StopHeavyOilEvent();
        }

        private void StopHeavyOilEvent()
        {
            KillEntity(_mapMarker);
            KillEntity(_codeLock);

            foreach (var item in _spawnedScientists)
            {
                KillEntity(item);
            }

            foreach (var item in _spawnedHackableCrates)
            {
                KillEntity(item);
            }

            foreach (var item in _spawnedBradleys)
            {
                KillEntity(item);
            }

            foreach (var item in _spawnedTurrets)
            {
                KillEntity(item);
            }

            _spawnedScientists.Clear();
            _spawnedHackableCrates.Clear();
            _spawnedBradleys.Clear();
            _spawnedTurrets.Clear();
            _turretsDisabled = false;
            _isEventRunning = false;

            Interface.Oxide.CallHook("HeavyOilRigEventStopped");
        }

        private void KillEntity(BaseEntity entity)
        {
            if (entity != null && !entity.IsDestroyed)
                entity.Kill();
        }

        private void DeployMapMarker()
        {
            _mapMarker = GameManager.server.CreateEntity(_mapMarkerPrefab) as VendingMachineMapMarker;
            _mapMarker.transform.position = _oilRigInstance.transform.position;
            _mapMarker.Spawn();

            _mapMarker.StartCoroutine(UpdateMapMarkerTime());
        }

        private IEnumerator UpdateMapMarkerTime()
        {
            while (_isEventRunning)
            {
                yield return CoroutineEx.waitForSeconds(1f);

                var timeLeft = (_pluginConfiguration.EventTimeSpan * 60) - eventSecondsTime;

                _mapMarker.markerShopName = lang.GetMessage("OilRigMarkerName", this)
                    + " - " + FormatTime(timeLeft) + " remaining";

                _mapMarker.SendNetworkUpdate();
            }
        }

        private string FormatTime(int timeLeft)
        {
            return timeLeft < 60
                ? timeLeft.ToString() + " Seconds"
                : (timeLeft / 60).ToString() + " Minutes";
        }

        private void DeployAutoTurrets()
        {
            if (!_pluginConfiguration.SpawnAutoTurrets)
                return;

            foreach (var location in _pluginConfiguration.AutoTurretSpawnLocations)
            {
                var entity = SpawnEntity(_autoTurretPlayer, location.position, true);
                var turret = entity as AutoTurret;
                var gunToLoad = ItemManager.FindItemDefinition("rifle.ak");
                var weaponItem = ItemManager.Create(gunToLoad);
                weaponItem.MoveToContainer(turret.inventory, 0);

                var heldWeapon = weaponItem.GetHeldEntity() as BaseProjectile;
                heldWeapon.primaryMagazine.contents = 0;

                turret.UpdateAttachedWeapon();
                turret.CancelInvoke(turret.UpdateAttachedWeapon);

                var weapon = turret.GetAttachedWeapon();
                weapon.primaryMagazine.contents = weapon.primaryMagazine.capacity;

                var ammoToLoad = gunToLoad.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.ammoType;

                for (var i = 0; i < 6; i++)
                {
                    ItemManager.Create(ammoToLoad, 128).MoveToContainer(turret.inventory, i + 1);
                }

                turret.SetFlag(BaseEntity.Flags.Reserved8, true);
                turret.InitiateStartup();
                turret.UpdateFromInput(11, 0);

                _spawnedTurrets.Add(turret);
            }
        }

        private void DeployScientists()
        {
            foreach (var location in _pluginConfiguration.HeavyScientistLocations)
            {
                _spawnedScientists.Add(SpawnEntity(_oilRigHeavyScientistPrefab, location));
            }
        }

        private BaseEntity SpawnEntity(string prefabPath, Vector3 localPosition, bool destroyGroundCheck = false, bool setparent = false)
        {
            var entity = GameManager.server.CreateEntity(prefabPath);
            entity.transform.position = _oilRigInstance.transform.TransformPoint(localPosition);

            if (setparent)
            {
                entity.SetParent(_mapMarker, true);
            }

            if (destroyGroundCheck)
            {
                UnityEngine.Object.DestroyImmediate(entity.GetComponent<DestroyOnGroundMissing>());
                UnityEngine.Object.DestroyImmediate(entity.GetComponent<GroundWatch>());
            }

            entity.Spawn();

            return entity;
        }

        private void DeployBradley()
        {
            if (!_pluginConfiguration.SpawnBradley)
                return;

            foreach (var location in _pluginConfiguration.APCBradleySpawnLocations)
            {
                var bradley = SpawnEntity(_BradleyApcPrefab, location);

                foreach (var item in bradley.GetComponentsInChildren<Transform>())
                {
                    if (item.gameObject.name.Contains("Wheel"))
                    {
                        item.gameObject.SetActive(false);
                    }
                }

                _spawnedBradleys.Add(bradley);
            }
        }

        private void DeployHackableCrates()
        {
            foreach (var location in _pluginConfiguration.HackableCrateSpawnLocations)
            {
                var crate = SpawnEntity(_HackableCrateLoot, location);
                _spawnedHackableCrates.Add(crate);

                foreach (var lootItem in _pluginConfiguration.SpecialLootItems)
                {
                    var itemData = lootItem.itemData;
                    //sample loot item
                    if (itemData.ItemId == 0)
                        continue;

                    var randomRoll = UnityEngine.Random.Range(0f, 100f);
                    var shouldSpawn = randomRoll < lootItem.ChanceOfDrop;

                    if (!shouldSpawn)
                        continue;

                    var item = ItemManager.CreateByItemID(itemData.ItemId,
                            UnityEngine.Random.Range(itemData.MinimumStackSize, itemData.MaximumStackSize + 1), itemData.SkinId);

                    var container = (crate as LootContainer).inventory;

                    if (container.capacity == container.itemList.Count)
                        container.capacity += 1;

                    if (!item.MoveToContainer(container))
                        item.Remove();
                }
            }
        }

        private void DeployCodeLock()
        {
            if (_pluginConfiguration.TurretsCanOnlyBeDestroyed)
                return;

            _codeLock = SpawnEntity(_buttonPrefab, new Vector3(-4.4f, 33f, -4.6f), false, true);
            var codelock = _codeLock as CodeLock;
            _randomCode = GetRandomCodelockCode();
            Puts(_randomCode.ToString());
            codelock.code = _randomCode;
            codelock.SetFlag(BaseEntity.Flags.Locked, true);
            codelock.transform.localRotation = Quaternion.Euler(new Vector3(0f, -45f, 0f));

            _codeLock.StartCoroutine(TurretsDisabledWhenCodeUnlock());
        }

        private string GetRandomCodelockCode()
        {
            return UnityEngine.Random.Range(1000, 9999).ToString();
        }

        private void SetCodeLockRevealNpc()
        {
            if (_pluginConfiguration.TurretsCanOnlyBeDestroyed)
                return;

            List<NPCPlayer> validNpcs = new List<NPCPlayer>();

            foreach (var child in _spawnedScientists)
            {
                var npcPlayer = child as NPCPlayer;
                if (npcPlayer && child.transform.position.y < _codeLock.transform.position.y)
                    validNpcs.Add(npcPlayer);
            }

            for (int i = 0; i < _pluginConfiguration.NumberOfNPCsWithCode; i++)
            {
                if (validNpcs.Count == 0 || i > 4)
                    break;

                var randomNpc = Core.Random.Range(0, validNpcs.Count);
                _npcCodeRevealNetIds.Add(validNpcs[randomNpc].net.ID.Value);
                validNpcs.RemoveAt(randomNpc);
            }
        }

        private bool DoesEntityExist(BaseEntity entity, IEnumerable<BaseEntity> entitesToSearch)
        {
            if (entity == null || !entity.IsValid())
                return false;

            foreach (var spawnedTurret in entitesToSearch)
            {
                if (spawnedTurret.IsValid() 
                    && spawnedTurret.net.ID == entity.net.ID)
                    return true;
            }

            return false;
        }

        private IEnumerator TurretsDisabledWhenCodeUnlock()
        {
            while (_codeLock && _codeLock.HasFlag(BaseEntity.Flags.Locked) && !_turretsDisabled)
            {
                yield return CoroutineEx.waitForSeconds(1);
            }

            foreach (var turret in _spawnedTurrets)
            {
                var turretObj = turret as AutoTurret;
                turretObj.UpdateFromInput(0, 0);
                turretObj.SetFlag(BaseEntity.Flags.Reserved8, false);
                turretObj.SendNetworkUpdateImmediate();
            }

            _turretsDisabled = true;
        }

        private void ClearTurretLootOnDeath(AutoTurret turret)
        {
            var container = turret.inventory;
            while (container.itemList.Count > 0)
            {
                Item item = container.itemList[0];
                item.RemoveFromContainer();
                item.Remove(0f);
            }
        }

        #endregion

        #region Oxide
        protected override void LoadDefaultConfig()
        {
            base.Config.WriteObject(PluginConfiguration.GetDefault(), true);
        }

        void OnCorpsePopulate(BasePlayer player, PlayerCorpse corpse)
        {
            if (!_isEventRunning || !_npcCodeRevealNetIds.Contains(player.net.ID.Value))
                return;

            corpse.StartCoroutine(SpawnNoteDelay(corpse));
        }

        private IEnumerator SpawnNoteDelay(PlayerCorpse corpse)
        {
            yield return CoroutineEx.waitForSeconds(1);

            var item = ItemManager.CreateByItemID(1414245162, 1);
            item.text = String.Format(lang.GetMessage("TurnOffAutoTurretsMsg", this), _randomCode);

            var container = corpse.containers[0];

            if (container.capacity == container.itemList.Count)
                corpse.containers[0].capacity += 1;

            if (!item.MoveToContainer(corpse.containers[0]))
                item.Remove();
        }

        object OnTurretTarget(AutoTurret turret, BaseCombatEntity entity)
        {
            if (entity == null || turret == null || !_isEventRunning || _pluginConfiguration.AutoTurretsCanAttackNPCs || !entity.IsNpc)
                return null;

            if (DoesEntityExist(turret, _spawnedTurrets))
                return false;

            return null;
        }

        object OnTurretAuthorize(AutoTurret turret, BasePlayer player)
        {
            if (!_isEventRunning || turret == null || player == null)
                return null;

            if (DoesEntityExist(turret, _spawnedTurrets))
            {
                player.ChatMessage(lang.GetMessage("CantAuthAutoTurret", this));
                return false;
            }

            return null;
        }

        private object CanEntityBeTargeted(BasePlayer player, BaseEntity entity)
        {
            if (!_isEventRunning || player == null || (entity as AutoTurret) == null)
                return null;

            if (DoesEntityExist(entity, _spawnedTurrets))
                return true;

            return null;
        }

        private object CanEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (!_isEventRunning || hitInfo == null || entity == null)
                return null;

            var turret = entity as AutoTurret ?? hitInfo.Initiator as AutoTurret;

            if (turret && DoesEntityExist(turret, _spawnedTurrets))
                return true;

            return null;
        }

        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (!_isEventRunning || _pluginConfiguration.AllowTurretLootingOnCodeLockDisable || player == null || entity == null || (entity as AutoTurret) == null)
                return;

            if (DoesEntityExist(entity, _spawnedTurrets))
            {
                player.ChatMessage(lang.GetMessage("CantLootAutoTurret", this));
                NextTick(player.EndLooting);
            }
        }
        #endregion

        #region Hooks 
        object OnCustomLootContainer(uint netID)
        {
            if (!_isEventRunning)
                return null;

            foreach (var crate in _spawnedHackableCrates)
            {
                if (crate.net.ID.Value == netID)
                    return CanPopulateLoot((crate as LootContainer));
            }

            return null;
        }

        object CanPopulateLoot(LootContainer container)
        {
            if (!_isEventRunning || !container || !container.IsValid())
                return null;

            foreach (var crate in _spawnedHackableCrates)
            {
                if (crate.IsValid() 
                    && crate.net.ID == container.net.ID 
                    && _pluginConfiguration.LootTableType != LootTableType.AlphaLoot)
                {
                    return false;
                }
            }

            return null;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!entity || !_isEventRunning || _pluginConfiguration.AllowTurretLootingOnDestroy)
                return;


            foreach (var item in _spawnedTurrets)
            {
                if (item.IsValid() && entity.net.ID == item.net.ID)
                    ClearTurretLootOnDeath(item as AutoTurret);
            }
        }
        #endregion

        #region Configuration
        private sealed class PluginConfiguration
        {
            [JsonProperty(PropertyName = "Should the event start automatically(false if you only want manually start with HeavyOilRigStart command")]
            public bool AutoStart = true;

            [JsonProperty(PropertyName = "Minimum time between events(minutes):")]
            public int Minimum { get; set; } = 60;

            [JsonProperty(PropertyName = "Maximum time between events(minutes):")]
            public int Maximum { get; set; } = 180;

            [JsonProperty(PropertyName = "Minimum number of players before event will start")]
            public int MinimumNumberOfPlayers = 4;

            [JsonProperty(PropertyName = "_Which loot table should this plugin use? 0 - Default, 1 - Alpha Loot, 2 - Other(Magic Loot, Better Loot)")]
            public LootTableType LootTableType = LootTableType.Default;

            [JsonProperty(PropertyName = "If this is true it will spawn autoturrets on the oil rig.")]
            public bool SpawnAutoTurrets = true;

            [JsonProperty(PropertyName = "If this is true auto turrets will target and kill NPCs as well as players on the oil rig.")]
            public bool AutoTurretsCanAttackNPCs = false;

            [JsonProperty(PropertyName = "If this is true it will spawn the APC Bradley on the oil rig.")]
            public bool SpawnBradley = true;

            [JsonProperty(PropertyName = "Allow turrets to be looted after they are disabled. (Each turret has an AK and full stacks of ammo)")]
            public bool AllowTurretLootingOnCodeLockDisable = false;

            [JsonProperty(PropertyName = "Allow turrets to be looted if they are destroyed")]
            public bool AllowTurretLootingOnDestroy = false;

            [JsonProperty(PropertyName = "How long should the event go on for?(minutes)")]
            public int EventTimeSpan = 45;

            [JsonProperty(PropertyName = "Skip event if players are already on rig?")]
            public bool SkipEventIfPlayersOnRigAlready = false;

            [JsonProperty(PropertyName = "Broadcast start message to players?")]
            public bool BroadcastStartMessage = true;

            [JsonProperty(PropertyName = "Controls how many NPCs will have the note on death (max 4).")]
            public int NumberOfNPCsWithCode = 1;

            [JsonProperty(PropertyName = "Remove code lock & Note code Drop(turrets can only be destroyed)")]
            public bool TurretsCanOnlyBeDestroyed = false;

            [JsonProperty(PropertyName = "This is for special items you want added to the hackable crates outside of loot tables")]
            public List<LootItem> SpecialLootItems = new List<LootItem>();

            [JsonProperty(PropertyName = "Start locations for heavy scientists")]
            public HashSet<Vector3> HeavyScientistLocations = new HashSet<Vector3>();

            [JsonProperty(PropertyName = "Start locations for APC Bradley")]
            public HashSet<Vector3> APCBradleySpawnLocations = new HashSet<Vector3>();

            [JsonProperty(PropertyName = "Start locations for AutoTurrets")]
            public HashSet<ItemTransformData> AutoTurretSpawnLocations = new HashSet<ItemTransformData>();

            [JsonProperty(PropertyName = "Start locations for HackableCrates")]
            public HashSet<Vector3> HackableCrateSpawnLocations = new HashSet<Vector3>();

            public static PluginConfiguration GetDefault()
            {
                return new PluginConfiguration()
                {
                    HeavyScientistLocations = GetDefaultScientistLocations(),
                    APCBradleySpawnLocations = GetDefaultBradleyLocations(),
                    AutoTurretSpawnLocations = GetDefaultAutoTurretLocations(),
                    HackableCrateSpawnLocations = GetDefaultHackableCrateLocations(),
                    SpecialLootItems = SampleLootItem(),
                };
            }
        }

        private static HashSet<Vector3> GetDefaultScientistLocations()
        {
            return new HashSet<Vector3>()
            {
                //boat ramp
                new Vector3() {  x = -17.9f, y = 1.1f, z = 3.8f },
                new Vector3() { x = -19.1f, y = 1.1f, z = -3.2f },
                new Vector3() { x = -5.1f, y = 46.4f, z = 69.5f },

                //first level
                new Vector3() { x = -0.2f, y = 9.9f, z = -24.7f },
                new Vector3() { x = -0.9f, y = 9.9f, z = 24.8f },

                //heli pad
                new Vector3() { x = 13.2f, y = 45.1f, z = 7.8f },
                new Vector3() { x = 11.2f, y = 45.1f, z = 4.6f },
                new Vector3() { x = 11.2f, y = 45.1f, z = 1.3f },

                //third floor
                new Vector3() { x = 21.7f, y = 22.6f, z = -9.9f },
                new Vector3() { x = 19.2f, y = 22.6f, z = 9.9f },

                //top floor
                new Vector3() { x = -15.7f, y = 36.1f, z = -40.5f },

                //random towers at top
                new Vector3() { x = -12.5f, y = 40.0f, z = 31.3f },
                new Vector3() { x = -12.4f, y = 40.0f, z = -4.6f },
                new Vector3() { x = -12.4f, y = 40.6f, z = 7.6f},
                new Vector3() { x = 4.9f, y = 45.3f, z = 26.6f },
                new Vector3() { x = 4.9f, y = 45.3f, z = 15.1f },
                new Vector3() { x = 17.1f, y = 39.9f, z = -34.0f },
            };
        }

        private static HashSet<Vector3> GetDefaultHackableCrateLocations()
        {
            return new HashSet<Vector3>()
                {
                    new Vector3() { x = 19.1f, y = 45.1f, z = 5.2f },
                    new Vector3() { x = 27.1f, y = 45.1f, z = 13.3f },
                    new Vector3() { x = 28.0f, y = 45.1f, z = -3.3f },
                };
        }

        private static HashSet<Vector3> GetDefaultBradleyLocations()
        {
            return new HashSet<Vector3>()
                {
                    new Vector3() { x = 35.8f, y = 45.1f,z = 3.5f},
                };
        }

        private static HashSet<ItemTransformData> GetDefaultAutoTurretLocations()
        {
            return new HashSet<ItemTransformData>()
            {
                //boat level
               new ItemTransformData() { position = { x = -13.8f, y = 3.0f,z = 31.3f } },
               new ItemTransformData() { position = { x = -13.9f, y = 3.0f,z = -31.2f } },
               new ItemTransformData() { position = { x = 13.5f, y = 3.0f,z = -31.1f } },
               new ItemTransformData() { position = { x = 13.8f, y = 3.0f,z = 31.1f } },

               //helipad
               new ItemTransformData() { position = { x = 19.4f, y = 45.2f,z = 11.7f } },
               new ItemTransformData() { position = { x = 22.8f, y = 45.2f,z = -5.3f } },
               new ItemTransformData() { position = { x = -8.7f, y = 37.7f,z = 3.9f } },
               new ItemTransformData() { position = { x = -4.7f, y = 39.6f,z = -27.0f } },
               new ItemTransformData() { position = { x = 9.1f, y = 39.6f,z = -20.6f } },
               new ItemTransformData() { position = { x = 14.6f, y = 40.6f,z = -1.6f } },
               new ItemTransformData() { position = { x = 34.1f, y = 45.2f,z = -4.2f } },
               new ItemTransformData() { position = { x = 33.7f, y = 45.2f,z = 14.4f } },
               new ItemTransformData() { position = { x = -22.0f, y = 46.2f,z = 24.4f } },
            };
        }

        private static List<LootItem> SampleLootItem()
        {
            return new List<LootItem>()
            {
                new LootItem()
                {
                    ChanceOfDrop = 50,
                    itemData = new ItemSpawnData()
                    {
                        ItemId = 0,
                        MaximumStackSize = 1,
                        MinimumStackSize = 5,
                        SkinId = 0
                    }
                }
            };
        }

        class ItemTransformData
        {
            public Vector3 position;
            public Vector3 rotation;
        }

        class LootItem
        {
            [JsonProperty(PropertyName = "percentage chance for it to appear in a hackable crate (IE 30 = 30% chance)")]
            public int ChanceOfDrop = 30;
            public ItemSpawnData itemData;
        }

        class ItemSpawnData
        {
            [JsonProperty(PropertyName = "The Item Id:")]
            public int ItemId;
            [JsonProperty(PropertyName = "Random amount minimum:")]
            public int MinimumStackSize;
            [JsonProperty(PropertyName = "Random amount maximum:")]
            public int MaximumStackSize;
            [JsonProperty(PropertyName = "skin id for the item")]
            public ulong SkinId;
        }
        #endregion

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["OilRigMarkerName"] = "Heavy Oil Rig",
                ["HeavyOilRigStarted"] = "Started Heavy Oil Rig Event.",
                ["HeavyOilRigStartedManual"] = "Heavy Oil Rig Event was manually started.",
                ["ErrorLocatingOilRig"] = "error locating large oil rig on map.Events will not start.",
                ["MinimumPlayersNotMet"] = "Skipping Heavy Oil Rig Event - Minimum player count not met.",
                ["HeavyOilRigStartedBroadcast"] = "<color=#b5440b>A heavy oil rig is being formed at Large Oil Rig.</color>",
                ["EventAlreadyInProgress"] = "Heavy Oil Rig Event is already in progress. The current event must end before a new one can start.",
                ["TurnOffAutoTurretsMsg"] = "The code to turn off the autoturrets is {0}."
                + Environment.NewLine
                + Environment.NewLine
                + "Make sure this doesn't get into the wrong hands.",
                ["CantLootAutoTurret"] = "Auto turrets on the heavy oil rig can't be looted.",
                ["CantAuthAutoTurret"] = "You can't authorize on a heavy oil rig turret.",
            }, this);
        }
#endregion
    }
}
