using System;
using Network;
using System.Linq;
using Facepunch;
using UnityEngine;
using System.Text;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("WoodShelter", "BlackLightning", "1.0.8")]
    [Description("Simple Wood Shelter to start with")]

    public class WoodShelter : CovalencePlugin
    {
        // Added Lantern as light source and config option for it.
        // Prevent Pickup of Lantern and Sleeping bags

        #region Load

        private List<ulong> deployedTC = new List<ulong>();
        private Dictionary<ulong, BaseEntity> deployedShelterFoundations = new Dictionary<ulong, BaseEntity>();
        private static ulong boxSkinID = 2060371585;
        private static ulong foundationSkinID = 2060371586;
        private static ulong wallSkinID = 2060371587;
        private static ulong tcSkinID = 2060371588;

        private const string permAdmin = "woodshelter.admin";
        private const string permCraft = "woodshelter.craft";
        private bool initComplete = false;

        private void Init()
        {
            permission.RegisterPermission(permAdmin, this);
            permission.RegisterPermission(permCraft, this);
        }

        private void OnServerInitialized()
        {
            deployedShelterFoundations.Clear();
            deployedTC.Clear();
            if (!initComplete) ProcessExistingShelters();
        }

        #endregion

        #region Configuration

        private static PluginConfig config;

        private class PluginConfig
        {
            public WoodShelterSettings shelterSettings { get; set; }

            public class WoodShelterSettings
            {
                [JsonProperty(PropertyName = "Placement - Prevent Placement of Wood Shelters if too close to another Tool Cupboard ? ")] public bool placementTCCheck { get; set; }
                [JsonProperty(PropertyName = "Placement - If enabled, distance to check for another Tool Cupboard before placement : ")] public float placementTCRadius { get; set; }
                [JsonProperty(PropertyName = "Placement - Prevent Placement of Wood Shelters if too close to other Building Blocks ? ")] public bool placementBlockCheck { get; set; }
                [JsonProperty(PropertyName = "Placement - If enabled, distance to check for other Building Blocks before placement : ")] public float placementBlockRadius { get; set; }
                [JsonProperty(PropertyName = "Spawn - Spawn sleeping bag when shelter spawns ? ")] public bool spawnSleepingBag { get; set; }
                [JsonProperty(PropertyName = "Spawn - Spawn lantern when shelter spawns ? ")] public bool spawnLantern { get; set; }
                [JsonProperty(PropertyName = "Spawn - Spawn Tool Cupboard when shelter spawns ? ")] public bool spawnCupboard { get; set; }
                [JsonProperty(PropertyName = "Spawn - Spawn Wood in Tool Cupboard if TC is set to spawn ? ")] public bool spawnCupboardWood { get; set; }
                [JsonProperty(PropertyName = "Spawn - Amount of Wood to spawn in TC if enabled : ")] public int cupboardWoodAmount { get; set; }
                [JsonProperty(PropertyName = "Spawn - Place Help note in TC when spawned ? ")] public bool helpNoteAdd { get; set; }
                [JsonProperty(PropertyName = "Craft - Amount of Materials needed to craft shelter : ")] public int materialsAmount { get; set; }
                [JsonProperty(PropertyName = "Craft - Item ID of Material needed to craft shelter (default wood) : ")] public int materialsItemID { get; set; }
                [JsonProperty(PropertyName = "Damage - Block damage to Shelters ? ")] public bool blockDamage { get; set; }
                [JsonProperty(PropertyName = "Damage - Allow Decay Damage ? ")] public bool allowDecay { get; set; }
                [JsonProperty(PropertyName = "Sleeping Bag - Reuse timer for shelter sleeping bag ? ")] public float bagTimer { get; set; }
                [JsonProperty(PropertyName = "Inventory Item - Name of Wood Shelter item in inventory : ")] public string woodShelterName { get; set; }
                [JsonProperty(PropertyName = "Help Note - Description Name of Help Note in TC : ")] public string helpNoteName { get; set; }
                [JsonProperty(PropertyName = "Help Note - Help note text : ")] public string helpNoteText { get; set; }
            }

            public static PluginConfig DefaultConfig() => new PluginConfig()
            {
                shelterSettings = new PluginConfig.WoodShelterSettings
                {
                    placementTCCheck = true,
                    placementTCRadius = 25f,
                    placementBlockCheck = true,
                    placementBlockRadius = 5f,
                    spawnSleepingBag = true,
                    spawnLantern = true,
                    spawnCupboard = true,
                    spawnCupboardWood = true,
                    cupboardWoodAmount = 10,
                    materialsAmount = 1000,
                    materialsItemID = -151838493,
                    blockDamage = true,
                    allowDecay = true,
                    bagTimer = 15f,
                    woodShelterName = "Wood Shelter",
                    helpNoteAdd = true,
                    helpNoteName = "Wood Shelter Help Notes",
                    helpNoteText = "A Simple Wood Shelter.\nType /shelter for help info.\n- Shelters take no damage.\n- Shelters cannot be built onto.\n- You cannot deploy a TC with a shelter spawned."
                }
            };
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("New configuration file created!!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            SaveConfig();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["notauthorized"] = "You are NOT Authorized to do that !!",
                ["craftedshelter"] = "You have Crafted a new Wood Shelter to inventory !!",
                ["spawnedshelter"] = "You have Spawned a new Wood Shelter to inventory !!",
                ["destroyed"] = "You have Destroyed your existing Shelter!!",
                ["notdestroyed"] = "You have NO shelters to destroy!!",
                ["deployerror"] = "Cannot do that ! You have deployed a TC or Shelter already, or are too close to another TC or Building Block",
                ["needmats"] = "You need the required Materials to craft that!"
            }, this);
        }

        #endregion

        #region Commands

        [Command("shelter")]
        private void cmdShelterHelp(IPlayer iplayer, string command, string[] args)
        {
            if (iplayer != null)
            {
                StringBuilder newHelpString = new StringBuilder();
                newHelpString.Append("<color=yellow>CHAT COMMANDS</color>\n");
                newHelpString.Append("<color=orange>/shelter </color> - shows help text to player in chat.\n");
                newHelpString.Append("<color=orange>/giveshelter </color> - gives player a shelter in inventory.\n");
                newHelpString.Append("<color=orange>/craftshelter </color> - craft a shelter from materials.\n");
                newHelpString.Append("<color=orange>/destroyshelter </color> - destroys your shelter if withing 10 ft.\n\n");
                newHelpString.Append("<color=yellow>F1 CONSOLE COMMANDS</color>\n");
                newHelpString.Append("<color=green>shelter </color> - shows help text to player in console.\n");
                newHelpString.Append("<color=green>giveshelter </color> - gives player a shelter in inventory.\n");
                newHelpString.Append("<color=green>craftshelter </color> - craft a shelter from materials.\n");
                newHelpString.Append("<color=green>destroyshelter </color> - destroys your shelter if withing 10 ft.\n");
                iplayer.Reply(newHelpString.ToString());
            }
        }

        [Command("giveshelter")]
        private void cmdGiveShelter(IPlayer iplayer, string command, string[] args)
        {
            if (iplayer.IsServer)
            {
                if (args.Length > 0)
                {
                    BasePlayer foundPlayer = BasePlayer.FindByID(Convert.ToUInt64(args[0]));
                    if (foundPlayer != null)
                    {
                        GiveStarterHome(foundPlayer);
                    }
                    else Puts("Could not locate player with ID : " + args[0]);
                    return;
                }
            }
            if (iplayer != null)
            {
                if (iplayer.HasPermission(permAdmin))
                {
                    var basePlayer = iplayer.Object as BasePlayer;
                    if (basePlayer != null)
                    {
                        GiveStarterHome(basePlayer);
                        iplayer.Message(lang.GetMessage("spawnedshelter", this, iplayer.Id));
                    }
                }
                else iplayer.Message(lang.GetMessage("notauthorized", this, iplayer.Id));
            }
        }

        [Command("craftshelter")]
        private void cmdCraftShelter(IPlayer iplayer, string command, string[] args)
        {
            if (iplayer != null)
            {
                if (iplayer.HasPermission(permCraft) || iplayer.HasPermission(permAdmin))
                {
                    CraftWoodShelter(iplayer);
                }
                else iplayer.Message(lang.GetMessage("notauthorized", this, iplayer.Id));
            }
        }

        [Command("destroyshelter")]
        private void cmdDestroyShelter(IPlayer iplayer, string command, string[] args)
        {
            if (iplayer != null)
            {
                if (iplayer.HasPermission(permCraft) || iplayer.HasPermission(permAdmin))
                {
                    DestroyPlayerShelter(iplayer);
                }
                else iplayer.Message(lang.GetMessage("notauthorized", this, iplayer.Id));
            }
        }

        #endregion

        #region Rust Hooks

        private void OnEntitySpawned(BaseEntity entity)
        {
            var cupboard = entity as BuildingPrivlidge;
            if (cupboard != null)
            {
                if (cupboard.skinID != tcSkinID) deployedTC.Add(cupboard.OwnerID);
            }

            var storageCon = entity as StorageContainer;
            if (storageCon != null)
            {
                if (!initComplete) return;
                if (storageCon.skinID == boxSkinID)
                {
                    ulong ownerid = storageCon.OwnerID;
                    Vector3 spawnposition = (Vector3)storageCon.transform.position + new Vector3(0f, 0.5f, 0f);
                    Quaternion spawnrotation = storageCon.transform.rotation;
                    timer.Once(1f, () => DestroyContainer(storageCon));

                    if (deployedTC.Contains(ownerid) || (!CanPlaceWoodShelter(spawnposition) || deployedShelterFoundations.ContainsKey(ownerid)))
                    {
                        var player = BasePlayer.FindByID(ownerid);
                        if (player != null) player.IPlayer.Message(lang.GetMessage("deployerror", this, player.IPlayer.Id));
                        GiveStarterHome(player);
                        return;
                    }
                    spawnrotation = new Quaternion(0f, spawnrotation.y, 0f, spawnrotation.w) * Quaternion.Euler(new Vector3(0f, 270f, 0f));
                    ServerMgr.Instance.StartCoroutine(SpawnWoodSelter(ownerid, spawnposition, spawnrotation));
                }
            }
        }

        private void DestroyContainer(StorageContainer container)
        {
            if (container != null) container.Kill(BaseNetworkable.DestroyMode.None);
        }

        private void OnDoorOpened(Door door, BasePlayer player)
        {
            if (door == null || player == null) return;
            if (door.skinID == wallSkinID && player.userID != door.OwnerID) door.CloseRequest();
        }

        private object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            if (prefab != null && prefab.ToString().Contains("cupboard.tool.deployed"))
            {
                var playerid = planner.GetComponent<HeldEntity>().GetOwnerPlayer().userID;
                if (deployedShelterFoundations.ContainsKey(playerid))
                {
                    var player = BasePlayer.FindByID(playerid);
                    if (player != null) player.IPlayer.Message(lang.GetMessage("deployerror", this, player.IPlayer.Id));
                    return false;
                }
            }
            if (target.entity != null && (target.entity.skinID == wallSkinID || target.entity.skinID == foundationSkinID)) return false;
            return null;
        }

        private bool CanDemolish(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade)
        {
            if (block.skinID == foundationSkinID || block.skinID == wallSkinID || block.skinID == tcSkinID) return false;
            return true;
        }

        private object CanPickupLock(BasePlayer player, BaseLock lockentity)
        {
            if (lockentity == null) return null;
            if (lockentity.skinID == wallSkinID) return false;
            return null;
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null) return;
            if (entity.skinID == foundationSkinID || entity.skinID == wallSkinID || entity.skinID == tcSkinID)
            {
                if (config.shelterSettings.allowDecay && hitInfo.damageTypes.GetMajorityDamageType() != null && hitInfo.damageTypes.GetMajorityDamageType().ToString() == "Decay") return;
                if (config.shelterSettings.blockDamage) hitInfo.damageTypes.ScaleAll(0);
            }
        }

        private void OnEntityKill(BaseEntity entity)
        {
            if (entity == null) return;
            var bBlock = entity as BuildingBlock;
            if (bBlock != null)
            {
                if (bBlock.skinID == foundationSkinID)
                {
                    deployedShelterFoundations.Remove(bBlock.OwnerID);
                }
                return;
            }
            var buildPriv = entity as BuildingPrivlidge;
            if (buildPriv != null)
            {
                if (buildPriv.skinID != tcSkinID) deployedTC.Remove(buildPriv.OwnerID);
            }

        }

        #endregion

        #region Hooks

        private void ProcessExistingShelters()
        {
            deployedTC.Clear();
            deployedShelterFoundations.Clear();
            var shelterList = UnityEngine.Object.FindObjectsOfType<BuildingBlock>();
            foreach (var entity in shelterList)
            {
                if (entity.skinID == foundationSkinID) deployedShelterFoundations.Add(entity.OwnerID, entity);
            }
            var tcList = UnityEngine.Object.FindObjectsOfType<BuildingPrivlidge>();
            foreach (var entity in tcList)
            {
                if (entity.skinID != tcSkinID) deployedTC.Add(entity.OwnerID);
            }
            initComplete = true;
            Puts("Processing of Shelters completed");
        }

        private void GiveStarterHome(BasePlayer player)
        {
            var item = ItemManager.CreateByItemID(833533164, 1, boxSkinID);
            item.name = "Wood Shelter";
            item.text = "A Simple Wood Shelter";
            player.inventory.GiveItem(item);
        }

        private void CraftWoodShelter(IPlayer iplayer)
        {
            var basePlayer = iplayer.Object as BasePlayer;
            if (basePlayer != null && basePlayer.inventory.GetAmount(config.shelterSettings.materialsItemID) >= config.shelterSettings.materialsAmount)
            {
                basePlayer.inventory.Take(null, config.shelterSettings.materialsItemID, config.shelterSettings.materialsAmount);
                basePlayer.Command("note.inv", config.shelterSettings.materialsItemID, -config.shelterSettings.materialsAmount);
                GiveStarterHome(basePlayer);
                basePlayer.Command("note.inv", new object[] { 1712070256, 1, "Wooden Shelter", 0 });
                iplayer.Message(lang.GetMessage("craftedshelter", this, iplayer.Id));
            }
            else iplayer.Message(lang.GetMessage("needmats", this, iplayer.Id));
        }

        private void DestroyPlayerShelter(IPlayer iplayer)
        {
            var basePlayer = iplayer.Object as BasePlayer;
            var removePlayer = false;
            var removeEntity = new BaseEntity();
            foreach (KeyValuePair<ulong, BaseEntity> kvp in deployedShelterFoundations)
            {
                if (kvp.Key == basePlayer.userID) removeEntity = kvp.Value as BaseEntity;
                removePlayer = true;
            }
            if (removePlayer)
            {
                List<BaseEntity> plist = Pool.GetList<BaseEntity>();
                Vis.Entities<BaseEntity>(removeEntity.transform.position, 3f, plist);
                foreach (BaseEntity foundEntity in plist)
                {
                    if (!foundEntity.IsDestroyed && (foundEntity.skinID == wallSkinID || foundEntity.skinID == foundationSkinID || foundEntity.skinID == tcSkinID)) foundEntity.Kill(BaseNetworkable.DestroyMode.None);
                }
                Pool.FreeList<BaseEntity>(ref plist);

                iplayer.Message(lang.GetMessage("destroyed", this, iplayer.Id));
            }
            else iplayer.Message(lang.GetMessage("notdestroyed", this, iplayer.Id));
        }

        private bool CanPlaceWoodShelter(Vector3 position)
        {
            if (config.shelterSettings.placementTCCheck)
            {
                List<BuildingPrivlidge> privlidgeList = Pool.GetList<BuildingPrivlidge>();
                Vis.Entities<BuildingPrivlidge>(position, config.shelterSettings.placementTCRadius, privlidgeList);

                foreach (BuildingPrivlidge privlidge in privlidgeList)
                {
                    return false;
                }
                Pool.FreeList<BuildingPrivlidge>(ref privlidgeList);
            }
            if (config.shelterSettings.placementBlockCheck)
            {
                List<BuildingBlock> buildingBlockList = Pool.GetList<BuildingBlock>();
                Vis.Entities<BuildingBlock>(position, config.shelterSettings.placementBlockRadius, buildingBlockList);

                foreach (BuildingBlock block in buildingBlockList)
                {
                    return false;
                }
                Pool.FreeList<BuildingBlock>(ref buildingBlockList);
            }

            return true;
        }

        #endregion

        #region Wood Shelter Entity

        private IEnumerator SpawnWoodSelter(ulong ownerID, Vector3 position, Quaternion rotation)
        {
            var prefabfoundation = "assets/prefabs/building core/foundation/foundation.prefab";
            var prefabwall = "assets/prefabs/building core/wall/wall.prefab";
            var prefabdoorway = "assets/prefabs/building core/wall.doorway/wall.doorway.prefab";
            var prefabfloor = "assets/prefabs/building core/floor/floor.prefab";
            var prefabdoor = "assets/prefabs/building/door.hinged/door.hinged.wood.prefab";
            var prefabsleepingbag = "assets/prefabs/deployable/sleeping bag/sleepingbag_leather_deployed.prefab";
            var prefabtoolcupboard = "assets/prefabs/deployable/tool cupboard/cupboard.tool.deployed.prefab";
            var prefablantern = "assets/prefabs/deployable/lantern/lantern.deployed.prefab";
            var buildingID = BuildingManager.server.NewBuildingID();

            yield return new WaitForEndOfFrame();
            SpawnPart(ownerID, prefabfoundation, foundationSkinID, position, rotation, new Vector3(0f, 0f, 0f), buildingID);
            yield return new WaitForEndOfFrame();
            if (config.shelterSettings.spawnSleepingBag)
            {
                SpawnPart(ownerID, prefabsleepingbag, wallSkinID, position + new Vector3(-0.65f, 0.1f, 0.6f), rotation, new Vector3(0f, 95f, 0f), buildingID);
                yield return new WaitForEndOfFrame();
            }
            SpawnPart(ownerID, prefabdoorway, wallSkinID, position + new Vector3(1.5f, 0f, 0f), rotation, new Vector3(0f, 0f, 0f), buildingID);
            yield return new WaitForEndOfFrame();
            SpawnPart(ownerID, prefabdoor, wallSkinID, position + new Vector3(1.5f, 0f, 0f), rotation, new Vector3(0f, 180f, 0f), buildingID);
            yield return new WaitForEndOfFrame();
            SpawnPart(ownerID, prefabwall, wallSkinID, position + new Vector3(-1.5f, 0f, 0f), rotation, new Vector3(0f, 180f, 0f), buildingID);
            yield return new WaitForEndOfFrame();
            SpawnPart(ownerID, prefabwall, wallSkinID, position + new Vector3(0f, 0f, -1.5f), rotation, new Vector3(0f, 90f, 0f), buildingID);
            yield return new WaitForEndOfFrame();
            SpawnPart(ownerID, prefabwall, wallSkinID, position + new Vector3(0f, 0f, 1.5f), rotation, new Vector3(0f, 270f, 0f), buildingID);
            yield return new WaitForEndOfFrame();
            SpawnPart(ownerID, prefabfloor, wallSkinID, position + new Vector3(0f, 3.0f, 0f), rotation, new Vector3(0f, 0, 0f), buildingID);
            yield return new WaitForEndOfFrame();
            if (config.shelterSettings.spawnCupboard)
            {
                SpawnPart(ownerID, prefabtoolcupboard, tcSkinID, position + new Vector3(-0.65f, 0.1f, -0.8f), rotation, new Vector3(0f, 0f, 0f), buildingID);
            }
            yield return new WaitForEndOfFrame();
            if (config.shelterSettings.spawnLantern)
            {
                SpawnPart(ownerID, prefablantern, 0, position + new Vector3(-1f, 0.1f, 0f), rotation, new Vector3(0f, 0, 0f), buildingID);
                yield return new WaitForEndOfFrame();
            }
        }

        private void SpawnPart(ulong ownerID, string prefabstring, ulong savedskinid, Vector3 savedposition, Quaternion savedrotation, Vector3 savedangles, uint buildingid)
        {
            var prefabname = (string)prefabstring;
            Vector3 pos = savedposition;
            Quaternion rot = savedrotation;
            BaseEntity entity = GameManager.server.CreateEntity(prefabname, pos, rot);
            entity.transform.position = pos;
            entity.transform.rotation = rot;
            entity.transform.localEulerAngles = savedangles;
            entity.OwnerID = ownerID;

            if (prefabname.Contains("foundation")) deployedShelterFoundations.Add(ownerID, entity);

            var buildingBlock = entity as BuildingBlock;
            if (buildingBlock != null)
            {
                buildingBlock.blockDefinition = PrefabAttribute.server.Find<Construction>(buildingBlock.prefabID);
                buildingBlock.SetGrade(BuildingGrade.Enum.Wood);
                buildingBlock.grounded = true;
                buildingBlock.SetHealthToMax();
                buildingBlock.blockDefinition.canRotateBeforePlacement = false;
                buildingBlock.blockDefinition.canRotateAfterPlacement = false;
            }

            var decayEntity = entity as DecayEntity;
            if (decayEntity != null)
            {
                decayEntity.AttachToBuilding(buildingid);
            }

            var stabilityEntity = entity as StabilityEntity;
            if (stabilityEntity != null)
            {
                stabilityEntity.grounded = true;
                stabilityEntity.InitializeSupports();
                stabilityEntity.UpdateStability();
            }

            var sleepingBag = entity as SleepingBag;
            if (sleepingBag != null)
            {
                sleepingBag.niceName = "Wooden Shelter";
                sleepingBag.deployerUserID = ownerID;
                sleepingBag.secondsBetweenReuses = config.shelterSettings.bagTimer;
                sleepingBag.canBePublic = false;
                sleepingBag.SendNetworkUpdateImmediate(true);
                sleepingBag.SendNetworkUpdate();
            }
            entity.skinID = savedskinid;
            entity.Spawn();

            var door = entity as Door;
            if (door != null)
            {
                var prefabdoorlock = "assets/prefabs/locks/keylock/lock.key.prefab";
                BaseEntity doorlock = GameManager.server.CreateEntity(prefabdoorlock, Vector3.zero);
                doorlock.skinID = wallSkinID;
                doorlock.OwnerID = ownerID;
                doorlock.gameObject.Identity();
                doorlock.SetParent(door, "lock");
                doorlock.OnDeployed(door, null, null);
                doorlock.Spawn();
                door.SetSlot(BaseEntity.Slot.Lock, doorlock);

                if (doorlock.GetComponent<KeyLock>())
                {
                    var keyLock = doorlock.GetComponent<KeyLock>();
                    keyLock.firstKeyCreated = true;
                    keyLock.SetFlag(BaseEntity.Flags.Locked, true);
                    keyLock.OwnerID = Convert.ToUInt64(ownerID);
                }
            }

            var baseCombat = entity as BaseCombatEntity;
            if (baseCombat != null)
            {
                var maxEntHealth = baseCombat.MaxHealth();
                baseCombat.health = maxEntHealth;
                baseCombat.pickup.enabled = false;
            }

            var cupboard = entity as BuildingPrivlidge;
            if (cupboard != null)
            {
                cupboard.authorizedPlayers.Add(new ProtoBuf.PlayerNameID
                {
                    userid = Convert.ToUInt64(ownerID),
                    username = "Player"
                });

                ItemContainer component1 = cupboard.GetComponent<StorageContainer>().inventory;
                if (config.shelterSettings.spawnCupboardWood)
                {
                    Item addWood = ItemManager.CreateByItemID(-151838493, config.shelterSettings.cupboardWoodAmount);
                    component1.itemList.Add(addWood);
                    addWood.parent = component1;
                    addWood.MarkDirty();
                }

                if (config.shelterSettings.helpNoteAdd)
                {
                    Item addNote = ItemManager.CreateByItemID(1414245162, 1, 0);
                    addNote.name = config.shelterSettings.helpNoteName;
                    addNote.text = config.shelterSettings.helpNoteText;
                    component1.itemList.Add(addNote);
                    addNote.parent = component1;
                    addNote.MarkDirty();
                }

                cupboard.GetComponent<StorageContainer>().onlyAcceptCategory = ItemCategory.All;
                cupboard.SendNetworkUpdate();
            }
        }

        #endregion

    }
}