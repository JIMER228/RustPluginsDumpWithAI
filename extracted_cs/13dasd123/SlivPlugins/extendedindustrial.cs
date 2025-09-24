#if CARBON
using HarmonyLib;
#else
// Reference: 0Harmony
using Harmony;
#endif
/*
 ▄▄▄▄    ███▄ ▄███▓  ▄████  ▄▄▄██▀▀▀▓█████▄▄▄█████▓
▓█████▄ ▓██▒▀█▀ ██▒ ██▒ ▀█▒   ▒██   ▓█   ▀▓  ██▒ ▓▒
▒██▒ ▄██▓██    ▓██░▒██░▄▄▄░   ░██   ▒███  ▒ ▓██░ ▒░
▒██░█▀  ▒██    ▒██ ░▓█  ██▓▓██▄██▓  ▒▓█  ▄░ ▓██▓ ░ 
░▓█  ▀█▓▒██▒   ░██▒░▒▓███▀▒ ▓███▒   ░▒████▒ ▒██▒ ░ 
░▒▓███▀▒░ ▒░   ░  ░ ░▒   ▒  ▒▓▒▒░   ░░ ▒░ ░ ▒ ░░   
▒░▒   ░ ░  ░      ░  ░   ░  ▒ ░▒░    ░ ░  ░   ░    
 ░    ░ ░      ░   ░ ░   ░  ░ ░ ░      ░    ░      
 ░             ░         ░  ░   ░      ░  ░                                                  
 */
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("extendedindustrial", "bmgjet", "1.0.4")]
    [Description("Adds Industrial Adapter To Other Deployables")]
    class extendedindustrial : RustPlugin
    {
        public static extendedindustrial plugin;
#if CARBON
        private Harmony _harmony;
#else
        private HarmonyInstance _harmony;
#endif
        //Permissions
        private string permPlace = "extendedindustrial.place"; // players can place a adapater on what they are looking at if they have permission for that entity
        private string permSpawn = "extendedindustrial.spawn"; //With this perm players can spawn a adapater on what they are looking if it has a container
        private string permAttach = "extendedindustrial.attach"; //With this perm adapter will be spawn on all entitys the player has perms for
        private string permAll = "extendedindustrial.all"; //With this perm you can use all items
        private string permPlanterBox = "extendedindustrial.planterbox"; //Grants planterbox adapter
        private string permFlameTurret = "extendedindustrial.flameturret"; //Grants flameturret adapter
        private string permGunTrap = "extendedindustrial.guntrap";//Grants guntrap adapter
        private string permSnowMachine = "extendedindustrial.snowmachine";//Grants snowmachine adapter
        private string permFogMachine = "extendedindustrial.fogmachine";//Grants fogmachine adapter
        private string permSamSite = "extendedindustrial.samsite";//Grants samesite adapter
        private string permFuelGenerator = "extendedindustrial.fuelgenerator";//Grants fuelgenerator adapter
        private string permAutoTurret = "extendedindustrial.autoturret";//Grants autoturret adapter
        private string permDropBox = "extendedindustrial.dropbox";//Grants dropbox adapter
        private string permComposter = "extendedindustrial.composter";//Grants composter adapter
        private string permHitchTrough = "extendedindustrial.hitchtrough";//Grants hitchtrough adapter
        private string permRecycler = "extendedindustrial.recycler";//Grants recycler adapter
        private string permMixingTable = "extendedindustrial.mixingtable";//Grants mixingtable adapter
        private string permStash = "extendedindustrial.stash";//Grants stash adapter
        private string permLight = "extendedindustrial.tunalight";//Grants tunalight adapter
        private string permHobo = "extendedindustrial.hobobarrel";//Grants hobobarrel adapter
        private string permFirePit = "extendedindustrial.skull_fire_pit";//Grants skull_fire_pit adapter
        private string permCampfire = "extendedindustrial.campfire";//Grants campfire adapter
        private string permFurnace = "extendedindustrial.furnace";//Grants old furnace adapter
        private string permFireplace = "extendedindustrial.fireplace";//Grants fireplace adapter
        private string permHAB = "extendedindustrial.hab_storage";//Grants hotairbloon storage adapter
        //Cui Data
        private string CUIData = "H4sIAAAAAAAEAKWPTwvCMAzFv8rIeRZbO527e9hJEHcaO5RZtbimoyviEL+7qYh/boLk0vzykr5XXwGV1VDAqlxDCr3yGgO167P2nRoJ7fQQvBsr8xK1zvYOSTdAUV8hjH08UKEJ4woPBjWrSlZaddAPcec8jafJo+CWvjY2ug1br3DYO29JqrA90ssgzZjMEybnb6oukWYiYdkMbk0883Refvr+wyFPYjFJyARtzQ6KCZ/KJV+IRSZSGE4GIxRLyXMheZb/mCXG/s5BH8UMzR0Bhv6MfwEAAA==";

        //Oxide Hooks
        private void Init()
        {
            plugin = this;
            //Register Permissions
            permission.RegisterPermission(permPlace, this);
            permission.RegisterPermission(permAttach, this);
            permission.RegisterPermission(permAll, this);
            permission.RegisterPermission(permPlanterBox, this);
            permission.RegisterPermission(permFlameTurret, this);
            permission.RegisterPermission(permGunTrap, this);
            permission.RegisterPermission(permSnowMachine, this);
            permission.RegisterPermission(permFogMachine, this);
            permission.RegisterPermission(permSamSite, this);
            permission.RegisterPermission(permFuelGenerator, this);
            permission.RegisterPermission(permAutoTurret, this);
            permission.RegisterPermission(permDropBox, this);
            permission.RegisterPermission(permComposter, this);
            permission.RegisterPermission(permHitchTrough, this);
            permission.RegisterPermission(permRecycler, this);
            permission.RegisterPermission(permMixingTable, this);
            permission.RegisterPermission(permStash, this);
            permission.RegisterPermission(permLight, this);
            permission.RegisterPermission(permHobo, this);
            permission.RegisterPermission(permFirePit, this);
            permission.RegisterPermission(permCampfire, this);
            permission.RegisterPermission(permFurnace, this);
            permission.RegisterPermission(permFireplace, this);
            permission.RegisterPermission(permSpawn, this);
            permission.RegisterPermission(permHAB, this);
            CUIData = Encoding.UTF8.GetString(Facepunch.Utility.Compression.Uncompress(Convert.FromBase64String(CUIData)));// Unpack CUI
#if CARBON
            _harmony = new Harmony(Name + "Patch");
            _harmony.PatchAll();
#else
            _harmony = HarmonyInstance.Create(Name + "PATCH");
            Type[] patchType =
            {
                AccessTools.Inner(typeof(extendedindustrial), "QueueNull"),
                AccessTools.Inner(typeof(extendedindustrial), "IndustrialStorageAdaptorPatch"),
                AccessTools.Inner(typeof(extendedindustrial), "GetActiveItemPatch"),
            };
            foreach (var t in patchType)
            {
                new PatchProcessor(_harmony, t, HarmonyMethod.Merge(t.GetHarmonyMethods())).Patch();
            }
#endif
        }

        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList) { CuiHelper.DestroyUi(player, "EIO"); } //Remove any CUI
            _harmony.UnpatchAll(Name + "PATCH"); //Remove harmony
            plugin = null;
        }

        //Block damage to spawned in parts
        private object OnEntityTakeDamage(BaseCombatEntity baseent, HitInfo info) { if (baseent is IndustrialStorageAdaptor && baseent.skinID == 12345678901) { return true; } return null; }

        private object CanPickupEntity(BasePlayer player, BaseCombatEntity baseent)
        {
            if (baseent is IndustrialStorageAdaptor && baseent.skinID == 12345678901) { return false; }  //Blocks picking up adapters with this set skinid
            foreach (BaseEntity be in baseent.children) { if (be is IndustrialStorageAdaptor) { player.inventory.GiveItem(ItemManager.CreateByItemID(-1049172752)); } break; } //Return any storage Adapters that were attached
            return null;
        }

        //Hooks for the adapter on the stash to be hidden
        private void OnStashHidden(StashContainer stash) { if (stash.skinID == 1111111111) { StashAdapter(stash); } }
        private void OnStashExposed(StashContainer stash) { if (stash.skinID == 1111111111) { StashAdapter(stash); } }

        private void OnEntitySpawned(BaseEntity baseent)
        {
            //Catch the spawn and add parts on targeted bits if player has the permission
            string pid = baseent.OwnerID.ToString(); //Convert to string once
            if (baseent.OwnerID != 0 && permission.UserHasPermission(pid, permAttach)) { Attach(pid, baseent, true, false); } //Dont target server entitys or players with out attach perm
        }

        [ChatCommand("addadapter")] //Chat Command
        private void CommandSpawnAdapter(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, permSpawn)) //check permissions
            {
                try
                {
                    BaseEntity be = FindEntity(player.eyes.HeadRay()); //Find what player is looking at
                    if (be == null) { player.ChatMessage("No Storages Found!"); return; } //If null message
                    bool added = false; //Bool to hold if anything was found
                    foreach (BaseEntity c in be.children) //Check each child attached to the base entity
                    {
                        bool spawn = true;
                        if (c is StorageContainer)
                        {
                            foreach (IndustrialStorageAdaptor sa in c.children) //Make sure it doesnt already have an adapter
                            {
                                if (sa != null)
                                {
                                    spawn = false;
                                    break;
                                }
                            }
                            if (spawn) //Only add adapter if there isnt one
                            {
                                player.ChatMessage("Added Storage Adapter To " + c.ToString());
                                added = true;
                                SpawnPart(c, UnityEngine.Quaternion.Euler(0, 0, 0), new UnityEngine.Vector3(0f, 0f, 0f));
                            }
                        }
                    }
                    if (!added) { player.ChatMessage("No free spots to add an adapter."); } //Message that nothing was added
                }
                catch { player.ChatMessage("No Storages Found!"); }
                return;
            }
            player.ChatMessage("Dont have permission.");
        }

        //Functions
        private BaseEntity FindEntity(Ray ray)
        {
            //Ray cast to find BaseEntity
            RaycastHit hit;
            var raycast = UnityEngine.Physics.Raycast(ray, out hit, 10, -1);
            BaseEntity entity = raycast ? hit.GetEntity() : null;
            if (entity == null) { return null; }
            return entity;
        }

        private bool Attach(string pid, BaseEntity baseent, bool protect, bool checkonly)
        {
            //Check conditions, spawn adapters and grant pickup or destruction protection where valid
            if (baseent is PlanterBox && HasPermission(pid, permPlanterBox)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(0, 0, 0), new UnityEngine.Vector3(0f, 0f, 0f), protect); } return true; }
            else if (baseent is StashContainer && HasPermission(pid, permPlanterBox)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(0, 0, 0), new UnityEngine.Vector3(0f, 0f, 0f), protect); } baseent.skinID = 1111111111; return true; }
            else if (baseent is FlameTurret && HasPermission(pid, permFlameTurret)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(0, 0, 0), new UnityEngine.Vector3(0f, 0f, 0f), protect); } return true; }
            else if (baseent is GunTrap && HasPermission(pid, permGunTrap)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(0, 0, 0), new UnityEngine.Vector3(0f, 0f, 0f), protect); } return true; }
            else if (baseent is SnowMachine && HasPermission(pid, permSnowMachine)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(0, 90, 0), new UnityEngine.Vector3(0f, 0f, -0.5f), protect); } return true; }
            else if (baseent is FogMachine && HasPermission(pid, permFogMachine)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(0, 90, 0), new UnityEngine.Vector3(0f, 0f, -0.2f), protect); } return true; }
            else if (baseent is SamSite && HasPermission(pid, permSamSite)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(90, 90, 180), new UnityEngine.Vector3(-0.90f, 0.48f, 0.0f), protect); } return true; }
            else if (baseent is FuelGenerator && HasPermission(pid, permFuelGenerator)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(0, 0, 90), new UnityEngine.Vector3(0f, 0.52f, 0f), protect); } return true; }
            else if (baseent is AutoTurret && HasPermission(pid, permAutoTurret)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(0, 0, 180), new UnityEngine.Vector3(0f, 0.20f, 0f), protect); } return true; }
            else if (baseent is DropBox && HasPermission(pid, permDropBox)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(270, 0, 0), new UnityEngine.Vector3(0f, -0.22f, -0.31f), protect); } return true; }
            else if (baseent is Composter && HasPermission(pid, permComposter)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(0, 0, 0), new UnityEngine.Vector3(0f, 1.66f, 0f), protect); } return true; }
            else if (baseent is HitchTrough && HasPermission(pid, permHitchTrough)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(270, 90, 180), new UnityEngine.Vector3(1.02f, 0.48f, 0.11f), protect); } return true; }
            else if (baseent is Recycler && HasPermission(pid, permRecycler)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(0, 0, 180), new UnityEngine.Vector3(-0.25f, 0.42f, 0.1f), protect); } return true; }
            else if (baseent is MixingTable && HasPermission(pid, permMixingTable)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(0, 0, 180), new UnityEngine.Vector3(0f, 0.82f, 0.1f), protect); } return true; }
            else if (baseent is BaseOven)
            {
                switch (baseent.ShortPrefabName)
                {
                    case "tunalight.deployed": if (HasPermission(pid, permLight)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(270, 180, 0), new UnityEngine.Vector3(0f, 0.0f, 0.1f), protect); } return true; } break;
                    case "hobobarrel.deployed": if (HasPermission(pid, permHobo)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(270, 180, 0), new UnityEngine.Vector3(0f, 0.2f, 0.3f), protect); } return true; } break;
                    case "skull_fire_pit": if (HasPermission(pid, permFirePit)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(0, 0, 0), new UnityEngine.Vector3(0f, 0.0f, 0.6f), protect); } return true; } break;
                    case "campfire": if (HasPermission(pid, permCampfire)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(0, 0, 0), new UnityEngine.Vector3(0f, 0.0f, 0.5f), protect); } return true; } break;
                    case "furnace": if (HasPermission(pid, permFurnace)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(270, 180, 0), new UnityEngine.Vector3(-0.05f, 0.32f, 0.5f), protect); } return true; } break;
                    case "fireplace.deployed": if (HasPermission(pid, permFireplace)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(270, 180, 0), new UnityEngine.Vector3(0f, 0.28f, 0.45f), protect); } return true; } break;
                }
            }
            else if (baseent is StorageContainer)
            {
                switch (baseent.ShortPrefabName)
                {
                    case "hab_storage": if (HasPermission(pid, permHAB)) { if (!checkonly) { SpawnPart(baseent, UnityEngine.Quaternion.Euler(90, 0, 0), new UnityEngine.Vector3(0, 0f, 0), protect); } return true; } break;
                }
            }
            return false;
        }

        private bool HasPermission(string userid, string perm) { return permission.UserHasPermission(userid, permAll) || permission.UserHasPermission(userid, perm); } //Check has permission or all permissions

        private void StashAdapter(StashContainer stash)
        {
            foreach (BaseEntity baseEntity in stash.children) //Adjust adapter on hidden stash actions
            {
                if (baseEntity is IndustrialStorageAdaptor)
                {
                    baseEntity.SetFlag(BaseEntity.Flags.Reserved8, true, true, true);
                    baseEntity.transform.localPosition = new Vector3(0f, (stash.IsHidden() ? -0.3f : 0f), 0f);
                    baseEntity.SendNetworkUpdateImmediate();
                    return;
                }
            }
        }

        private BaseEntity CanPlace(BasePlayer player, BaseEntity be, bool protect, bool checkonly)
        {
            if (be == null) { return null; } //No entity passed
            if (be.children.Count == 0 && plugin.Attach(player.UserIDString, be, protect, checkonly)) { return be; } //Attach to passed entity
            foreach (BaseEntity b in be.children) { if (b.children.Count == 0 && plugin.Attach(player.UserIDString, b, protect, checkonly)) { return b; } } //Attach to a child of passed entity
            return null; //Nothing valid found
        }

        private BaseEntity SpawnPart(BaseEntity parent, Quaternion rotoffset, Vector3 posoffset, bool protect = true)
        {
            //Spawns in parts of the quarry
            BaseEntity baseent = GameManager.server.CreateEntity("assets/prefabs/deployable/playerioents/industrialadaptors/storageadaptor.deployed.prefab", parent.transform.position, parent.transform.rotation);
            baseent.OwnerID = parent.OwnerID;
            if (protect) { baseent.skinID = 12345678901; }//Set a skinid we use to check for modded adapters.
            baseent.Spawn();
            //Remove componants to stop it breaking
            foreach (var mesh in baseent.GetComponentsInChildren<MeshCollider>()) { UnityEngine.Object.DestroyImmediate(mesh); }
            UnityEngine.Object.DestroyImmediate(baseent.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(baseent.GetComponent<GroundWatch>());
            //Parent it to base entity
            baseent.SetParent(parent, true, true);
            //Adjust offset positions
            if (rotoffset != Quaternion.Euler(0, 0, 0)) { baseent.transform.rotation = baseent.transform.rotation * rotoffset; }
            if (posoffset != new Vector3(0, 0, 0)) { baseent.transform.localPosition = posoffset; }
            baseent.SendNetworkUpdateImmediate();
            return baseent;
        }

        private void CreateTip(string msg, BasePlayer player, int time = 10)
        {
            if (player == null) { return; }
            player.SendConsoleCommand("gametip.hidegametip"); //Remove old tip that might still be shown
            player.SendConsoleCommand("gametip.showgametip", msg); //Create new tip
            timer.Once(time, () => player?.SendConsoleCommand("gametip.hidegametip")); //Remove tip after delay
        }

        private BaseEntity FindBaseEntity(Ray ray)
        {
            //Ray cast to find BaseEntity
            RaycastHit hit;
            var raycast = UnityEngine.Physics.Raycast(ray, out hit, 3, -1);
            BaseEntity entity = raycast ? hit.GetEntity() : null;
            if (entity != null) { return entity; }
            return null;
        }

        public class AdapterPlacer : MonoBehaviour
        {
            public BasePlayer player;
            private float UIDelay = 0; //Variable used to slow down method in fixedupdate
            private bool CUIed = false; //Variable to stop excessive network data from cui updates.
            public void OnDestroy() { if (player != null) { CuiHelper.DestroyUi(player, "EIO"); } } //Remove CUI
            public void FixedUpdate()
            {
                try //Try and catch incase something in the future changes to prevent null references
                {
                    if (player == null || plugin == null || player.GetActiveItem() == null || player?.GetActiveItem().info.itemid != -1049172752) { Destroy(this); }  //Destroy component when conditions are no longer meet
                    else if (player.serverInput.WasJustReleased(BUTTON.FIRE_PRIMARY)) //Catch mouse fire button
                    {
                        if (player.IsBuildingBlocked() && !player.IsAdmin) { plugin.CreateTip("Building Blocked!", player); return; } //Check player isnt building blocked
                        if (plugin.CanPlace(player, plugin.FindBaseEntity(player.eyes.HeadRay()), false, false) != null)//Find ent player is looking at
                        {
                            Effect.server.Run("assets/prefabs/deployable/playerioents/industrialconveyor/effects/industrial-conveyor-deploy.prefab", player.PivotPoint(), player.transform.up, null, true);
                            player.inventory.Take(null, -1049172752, 1); //Remove adapter from player inventory if it attached
                            player.ChatMessage("You Placed A Storage Adapter");
                            if (player.inventory.GetAmount(-1049172752) == 0) { Destroy(this); return; } //Checks player has any adapters left
                        }
                    }
                    if (UnityEngine.Time.time >= UIDelay) //Delay UI Updates
                    {
                        UIDelay = UnityEngine.Time.time + 1f; //Only check for updates every sec (fixedupdate runs 50hz)
                        if (plugin.CanPlace(player, plugin.FindBaseEntity(player.eyes.HeadRay()), false, true) != null) { if (!CUIed) { CuiHelper.AddUi(player, CuiHelper.FromJson(plugin.CUIData)); CUIed = true; } }//Send CUI if they dont have it already
                        else { if (CUIed) { CuiHelper.DestroyUi(player, "EIO"); CUIed = false; } } //Remove CUI
                    }
                }
                catch { Destroy(this); } //Something went wrong so remove CUI
            }
        }

        //Harmony
        [HarmonyPatch(typeof(BasePlayer), nameof(BasePlayer.GetActiveItem))]
        public static class GetActiveItemPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Item __result, BasePlayer __instance)
            {
                if (__result?.info?.itemid == null || __instance == null) { return; }
                if (__result.info.itemid == -1049172752 && plugin.permission.UserHasPermission(__instance.UserIDString, plugin.permPlace)) //Check for storage adapter and that player has permission
                {
                    AdapterPlacer AP = __instance.GetComponent<AdapterPlacer>(); //Check if player has component
                    if (AP == null) { AP = __instance.gameObject.AddComponent<AdapterPlacer>(); } //Add component if player doesnt have it
                    AP.player = __instance; //Set player variable in component
                }
            }
        }

        [HarmonyPatch(typeof(IndustrialStorageAdaptor), "get_Container")]
        public static class IndustrialStorageAdaptorPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(ref ItemContainer __result, IndustrialStorageAdaptor __instance)
            {
                if (__instance.GetParentEntity() is ContainerIOEntity)
                {
                    __result = (__instance.GetParentEntity() as ContainerIOEntity).inventory;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ServerMgr), "Update")]
        public static class QueueNull
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> list = instructions.ToList<CodeInstruction>();
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].opcode == OpCodes.Ldstr && list[i].operand as string == "Server Exception: IndustrialEntity.RunQueue")
                    {
                        list[i].opcode = OpCodes.Nop;
                        list[i + 1].opcode = OpCodes.Nop;
                        list[i + 2].opcode = OpCodes.Nop;
                        list[i + 3].opcode = OpCodes.Nop;
                        break;
                    }
                }
                return list;
            }
        }
    }
}