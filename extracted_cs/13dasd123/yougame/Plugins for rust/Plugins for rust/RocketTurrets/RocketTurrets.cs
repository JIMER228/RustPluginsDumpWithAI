using Oxide.Core;
using Oxide.Core.Configuration;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

//  TODO:
//          - Finish anti-air turrets MonoBehviour                                                                              ✓
//          - Heat seeking rockets?                                                                                             ✓
//          - Give the anti-air turrets a different aesthetic                                                                   ✓
//          - Handle the data for anti air turrets (loading / unloading)                                                        ✓
//          - Return items on pickup / downgrade back to normal turret                                                          ✓
//          - upgrades / different tiers for the rocket turret (increased rocket speed / shot speed / heatseeking rockets)
//          - Grenade turrets?

namespace Oxide.Plugins
{
    [Info("RocketTurrets", "redBDGR", "1.2.7")]
    [Description("Create rocket shooting turrets")]

    class RocketTurrets : RustPlugin
    {
        private bool Changed = false;
        private DynamicConfigFile _data;
        StoredData storedData;

        List<AutoTurret> rocketTurrets = new List<AutoTurret>();
        List<AutoTurret> antiairTurrets = new List<AutoTurret>();
        Dictionary<Item, ItemContainer> itemCache = new Dictionary<Item, ItemContainer>();
        List<ulong> turretIDs = new List<ulong>();
        List<ulong> antiAirIDs = new List<ulong>();
        static RocketTurrets plugin;
        public static LayerMask collLayers = UnityEngine.LayerMask.GetMask(new string[] { "Construction", "Deployed", "Tree", "Terrain", "Resource", "World", "Water", "Default", "AI" });
        
        // Config vars

        // Rocket turrets
        private float rocketFireRate = 1.2f;
        private float rocketLockonTime = 5f;
        private float minHitDistance = 10f;
        private float rocketLockonRadius = 40f;
        private float rocketSpeed = 20f;
        private uint rocketLauncherSkin = 0;
        private float explosionRadius = 4f;
        private bool explodeAtTarget = true;
        private int lockonSoundEveryxUpdate = 100;
        private bool requiresAmmo = true;

        // AA turrets
        private float aaFireRate = 1.2f;
        private float aaLockonTime = 5f;
        private float aaminHitDistance = 5f;
        private float aaLockonRadius = 150f;
        private float aarocketSpeed = 40f;
        private uint aaLauncherSkin = 0;
        private float aaexplosionRadius = 4f;
        private int aalockonSoundEveryxUpdate = 100;
        private float damageToHeli = 500f;
        private bool aaRequiresAmmo = true;

        // Global vars
        private bool returnCostOnPickup = false;
        private bool returnCostOnDowngrade = false;

        private const string permissionName = "rocketturrets.use";
        private const string permissionNameROCKET = "rocketturrets.rocket";
        private const string permissionNameANTIAIR = "rocketturrets.antiair";

        static Dictionary<string, object> RocketUpgradeCost()
        {
            var at = new Dictionary<string, object>();
            at.Add("rocket.launcher", 1);
            return at;
        }
        static Dictionary<string, object> AntiAirUpgradeCost()
        {
            var at = new Dictionary<string, object>();
            at.Add("targeting.computer", 1);
            at.Add("cctv.camera", 2);
            at.Add("rocket.launcher", 1);
            return at;
        }
        Dictionary<string, object> rocketUpgradeCost;
        Dictionary<string, object> antiAirUpgradeCost;

        class StoredData
        {
            public List<ulong> turretIDs = new List<ulong>();
            public List<ulong> antiAirIDs = new List<ulong>();
        }

        void Init()
        {
            plugin = this;
            permission.RegisterPermission(permissionName, this);
            permission.RegisterPermission(permissionNameROCKET, this);
            permission.RegisterPermission(permissionNameANTIAIR, this);
            LoadVariables();
        }

        void Loaded()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                //chat
                ["No Permission"] = "You are not allowed to use this command!",
                ["Invalid Destination"] = "Invalid turret found!",
                ["Not Authed On Turret"] = "You are not authed on this turret!",
                ["Already A Rocket Turret"] = "This turret is already a rocket turret",
                ["Already A Normal Turret"] = "This turret is already a normal turret",
                ["Invalid Entity"] = "No turret was found",
                ["Invalid Items"] = "You do not have the required items to upgrade this turret! (/turret cost)",
                ["Help Menu Header"] = "<color=#00CD66>Welcome to the help center, listed below are some helpful commands and what they do:</color>",
                ["Help Menu line1"] = "- <color=#008B45>/turret rocket</color> (upgrades the turret you are looking at to a turret that shoots rockets)",
                ["Help Menu line2"] = "- <color=#008B45>/turret normal</color> (returns an upgraded turret back to a normal turret)",
                ["Help Menu line3"] = "- <color=#008B45>/turret antiair</color> (upgrades the turret you are looking at to an anti air turret)",
                ["Help Menu line4"] = "- <color=#008B45>/turret cost</color> (displays the cost of upgrading to a rocket turret)",
                ["Cost Menu Header (rocket)"] = "You will need the following items to upgrade to a rocket turret:",
                ["Cost Menu Header (anti-air)"] = "You will need the following items to upgrade to an anti-air turret:",
                ["Cost Menu Entry"] = "- <color=#00CD66>{0}</color> x <color=#00CD66>{1}</color>",

            }, this);

            _data = Interface.Oxide.DataFileSystem.GetFile("RocketTurrets");
        }

        void Unload()
        {
            foreach (var entry in GameObject.FindObjectsOfType<AutoTurret>())
            {
                RocketTurret rt = entry.GetComponent<RocketTurret>();
                if (rt)
                    rt.UnloadDestroy();
                AntiAirTurret at = entry.GetComponent<AntiAirTurret>();
                if (at)
                    at.UnloadDestroy();
                entry.allowedItem = ItemManager.FindItemDefinition("ammo.rifle");
            }
            SaveData();
        }

        void OnServerSave() => SaveData();

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }

        void LoadVariables()
        {
            // Rocket turret settings
            rocketFireRate = Convert.ToSingle(GetConfig("Turret Settings", "Rocket fire rate", 2f));
            rocketLockonTime = Convert.ToSingle(GetConfig("Turret Settings", "Lockon length", 5f));
            minHitDistance = Convert.ToSingle(GetConfig("Turret Settings", "Min shoot distance", 4f));
            rocketLockonRadius = Convert.ToSingle(GetConfig("Turret Settings", "Rocket lockon radius", 40f));
            rocketSpeed = Convert.ToSingle(GetConfig("Turret Settings", "Rocket speed", 20f));
            rocketLauncherSkin = Convert.ToUInt32(GetConfig("Turret Settings", "Default rocket model skin", 0));
            explosionRadius = Convert.ToSingle(GetConfig("Turret Settings", "Explosion radius", 4f));
            explodeAtTarget = Convert.ToBoolean(GetConfig("Turret Settings", "Explode at target pos", true));
            lockonSoundEveryxUpdate = Convert.ToInt32(GetConfig("Turret Settings", "Lockon sound every x updates", 100));
            requiresAmmo = Convert.ToBoolean(GetConfig("Turret Settings", "Requires Ammo", true));

            // AntiAir turret settings
            aaFireRate = Convert.ToSingle(GetConfig("AntiAir Settings", "Rocket fire rate", 2f));
            aaLockonTime = Convert.ToSingle(GetConfig("AntiAir Settings", "Lockon length", 5f));
            aaminHitDistance = Convert.ToSingle(GetConfig("AntiAir Settings", "Min shoot distance", 4f));
            aaLockonRadius = Convert.ToSingle(GetConfig("AntiAir Settings", "Lockon radius", 80f));
            aarocketSpeed = Convert.ToSingle(GetConfig("AntiAir Settings", "Rocket speed", 30f));
            aaLauncherSkin = Convert.ToUInt32(GetConfig("AntiAir Settings", "Default rocket model skin", 0));
            aaexplosionRadius = Convert.ToSingle(GetConfig("AntiAir Settings", "Explosion radius", 4f));
            aalockonSoundEveryxUpdate = Convert.ToInt32(GetConfig("AntiAir Settings", "Lockon sound every x updates", 100));
            damageToHeli = Convert.ToSingle(GetConfig("AntiAir Settings", "Damage to heli", 500f));
            aaRequiresAmmo = Convert.ToBoolean(GetConfig("AntiAir Settings", "Requires Ammo", true));

            // Costs
            rocketUpgradeCost = (Dictionary<string, object>)GetConfig("Turret Settings", "[ Cost to upgrade - Rocket tier ]", RocketUpgradeCost());
            antiAirUpgradeCost = (Dictionary<string, object>)GetConfig("AntiAir Settings", "[Cost to Upgrade - AntiAir Tier ]", AntiAirUpgradeCost());
            returnCostOnPickup = Convert.ToBoolean(GetConfig("General Settings", "Return upgrade cost on turret pickup", false));
            returnCostOnDowngrade = Convert.ToBoolean(GetConfig("General Settings", "Return upgrade cost on turret downgrade", false));

            if (!Changed) return;
            SaveConfig();
            Changed = false;
        }

        void OnServerInitialized()
        {
            LoadData();
            foreach(var entry in GameObject.FindObjectsOfType<AutoTurret>())
            {
                if (turretIDs.Contains(entry.net.ID))
                {
                    RocketTurret rt = entry.GetComponent<RocketTurret>();
                    if (rt)
                        rt.Destroy();
                    entry.gameObject.AddComponent<RocketTurret>();
                    rocketTurrets.Add(entry);
                }
                else if (antiAirIDs.Contains(entry.net.ID))
                {
                    AntiAirTurret at = entry.GetComponent<AntiAirTurret>();
                    if (at)
                        at.Destroy();
                    entry.gameObject.AddComponent<AntiAirTurret>();
                    antiairTurrets.Add(entry);
                }
                continue;
            }
        }

        void SaveData()
        {
            storedData.turretIDs = turretIDs;
            storedData.antiAirIDs = antiAirIDs;
            _data.WriteObject(storedData);
        }
        void LoadData()
        {
            try
            {
                storedData = _data.ReadObject<StoredData>();
                turretIDs = storedData.turretIDs;
                antiAirIDs = storedData.antiAirIDs;
            }
            catch
            {
                Puts("Failed to load data, creating new file");
                storedData = new StoredData();
            }
        }

        #region Rocket Turret Handler

        private bool IsRocketTurret(AutoTurret turret) => turret.GetComponent<RocketTurret>();
        private void ToggleTurretAutomation(AutoTurret turret, bool enabled) => turret.GetComponent<RocketTurret>().enabled = enabled;
        private void TryFireRocket(AutoTurret turret) => turret.GetComponent<RocketTurret>().TryFireRocket();

        class RocketTurret : MonoBehaviour
        {
            public float aimRadius = plugin.rocketLockonRadius;
            public float fireRate = plugin.rocketFireRate;
            public float rocketSpeed = plugin.rocketSpeed;
            public float minHitDistance = plugin.minHitDistance;
            public float lockonTime = plugin.rocketLockonTime;
            public uint rocketLauncherSkin = plugin.rocketLauncherSkin;
            public float explosionRadius = plugin.explosionRadius;
            public bool explodeOnTargetPos = plugin.explodeAtTarget;
            public int soundEveryxUpdates = plugin.lockonSoundEveryxUpdate;
            public bool requiresAmmo = plugin.requiresAmmo;

            private int updateCounter = 0;
            private bool isLockedOn = false;
            private bool isLockingOn = false;
            private float lockonFinish;
            private float nextShoot;
            private AutoTurret turret;
            public BaseCombatEntity target;
            public DroppedItem launcher_1;
            public DroppedItem rocket_1;
            public DroppedItem rocket_2;

            private void Awake()
            {
                turret = gameObject.GetComponent<AutoTurret>();
                InitAesthetics();
                if (!plugin.rocketTurrets.Contains(turret))
                    plugin.rocketTurrets.Add(turret);
                turret.sightRange = aimRadius;
                if (!plugin.turretIDs.Contains(turret.net.ID))
                    plugin.turretIDs.Add(turret.net.ID);
            }

            private void Update()
            {
                if (!turret.HasTarget())
                {
                    if (isLockedOn)
                        isLockedOn = false;
                    if (isLockingOn)
                        isLockingOn = false;
                }
                if (!isLockedOn)
                {
                    if (!isLockingOn)
                    {
                        lockonFinish = UnityEngine.Time.time + lockonTime;
                        isLockingOn = true;
                    }
                    else if (isLockingOn && UnityEngine.Time.time > lockonFinish)
                    {
                        LockToTarget(turret.target);
                        return;
                    }
                    else if (isLockingOn)
                    {
                        if (updateCounter == soundEveryxUpdates)
                        {
                            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab", turret.transform.position);
                            updateCounter = 0;
                            return;
                        }
                        updateCounter++;
                    }
                }
                else
                {
                    if (UnityEngine.Time.time < nextShoot)
                        return;
                    else
                    {
                        if (GetHitDistance(turret.muzzlePos.transform.position, turret.target.transform.position) < minHitDistance)
                            return;
                        var rocketItem = HasAmmo(turret);
                        if (rocketItem == null)
                            return;
                        FireRocket(turret, rocketItem);
                        nextShoot = UnityEngine.Time.time + fireRate;
                    }
                }
            }

            public void OnDestroy()
            {
                if (!launcher_1.IsDestroyed)
                    launcher_1.Kill();
                if (!rocket_1.IsDestroyed)
                    rocket_1.Kill();
                if (!rocket_2.IsDestroyed)
                    rocket_2.Kill();
            }

            public void Destroy()
            {
                if (plugin.rocketTurrets.Contains(turret))
                    plugin.rocketTurrets.Remove(turret);
                if (plugin.turretIDs.Contains(turret.net.ID))
                    plugin.turretIDs.Remove(turret.net.ID);
                Destroy(this);
            }

            public void UnloadDestroy()
            {
                if (plugin.rocketTurrets.Contains(turret))
                    plugin.rocketTurrets.Remove(turret);
                Destroy(this);
            }

            private Item HasAmmo(AutoTurret turret)
            {
                if (!requiresAmmo)
                    return ItemManager.CreateByItemID(1578894260, 1, 0);
                foreach (var item in turret.inventory.itemList)
                    if (item.info.itemid == 1578894260 || item.info.itemid == 1436532208 || item.info.itemid == 542276424)
                        return item;
                return null;
            }

            private void LockToTarget(BaseCombatEntity entity)
            {
                isLockedOn = true;
                isLockingOn = false;
                nextShoot = UnityEngine.Time.time + fireRate;
                target = entity;
            }

            private float GetHitDistance(Vector3 pos1, Vector3 pos2)
            {
                return Vector3.Distance(pos1, pos2);
            }

            private void InitAesthetics()
            {
                // Rocket Launcher
                launcher_1 = ItemManager.CreateByItemID(649603450, 1, this.rocketLauncherSkin).Drop(turret.transform.position, Vector3.zero, new Quaternion()).GetComponent<DroppedItem>();
                launcher_1.GetComponent<Rigidbody>().isKinematic = true;
                launcher_1.GetComponent<Rigidbody>().useGravity = false;
                launcher_1.allowPickup = false;
                launcher_1.CancelInvoke((Action)Delegate.CreateDelegate(typeof(Action), launcher_1, "IdleDestroy"));

                // rocket_1
                rocket_1 = ItemManager.CreateByItemID(1578894260, 1, 0).Drop(turret.transform.position, Vector3.zero, new Quaternion()).GetComponent<DroppedItem>();
                rocket_1.GetComponent<Rigidbody>().isKinematic = true;
                rocket_1.GetComponent<Rigidbody>().useGravity = false;
                rocket_1.allowPickup = false;
                rocket_1.CancelInvoke((Action)Delegate.CreateDelegate(typeof(Action), rocket_1, "IdleDestroy"));

                // rocket_2
                rocket_2 = ItemManager.CreateByItemID(1578894260, 1, 0).Drop(turret.transform.position, Vector3.zero, new Quaternion()).GetComponent<DroppedItem>();
                rocket_2.GetComponent<Rigidbody>().isKinematic = true;
                rocket_2.GetComponent<Rigidbody>().useGravity = false;
                rocket_2.allowPickup = false;
                rocket_2.CancelInvoke((Action)Delegate.CreateDelegate(typeof(Action), rocket_2, "IdleDestroy"));

                foreach (var entry in turret.gameObject.transform.GetAllChildren())
                {
                    if (entry.name == "weapon_socket")
                    {
                        // launcher rot
                        Vector3 launcherRot = entry.gameObject.transform.localRotation.eulerAngles;
                        launcherRot = new Vector3(launcherRot.x, launcherRot.y + 180, launcherRot.z);

                        // rocket rot
                        Vector3 rocketRot = entry.gameObject.transform.localRotation.eulerAngles;
                        rocketRot = new Vector3(rocketRot.x, rocketRot.y + 270, rocketRot.z);

                        // launcher_1
                        launcher_1.transform.SetParent(entry.gameObject.transform);
                        launcher_1.transform.localPosition = new Vector3(0, 0, 0);
                        launcher_1.transform.localRotation = Quaternion.Euler(launcherRot);

                        // rocket_1
                        rocket_1.transform.SetParent(entry.gameObject.transform);
                        rocket_1.transform.localPosition = new Vector3(0, -0.05f, 0.17f);
                        rocket_1.transform.localRotation = Quaternion.Euler(rocketRot);

                        // rocket_2
                        rocket_2.transform.SetParent(entry.gameObject.transform);
                        rocket_2.transform.localPosition = new Vector3(0, -0.05f, -0.17f);
                        rocket_2.transform.localRotation = Quaternion.Euler(rocketRot);
                    }
                }
            }

            public void TryFireRocket()
            {
                if (UnityEngine.Time.time < nextShoot)
                    return;
                else
                {
                    var rocketItem = HasAmmo(turret);
                    if (rocketItem == null)
                        return;
                    FireRocket(turret, rocketItem);
                    nextShoot = UnityEngine.Time.time + fireRate;
                }
            }

            void FireRocket(AutoTurret turret, Item rocketItem)
            {
                ServerProjectile rocket = CreateRocket(turret.muzzlePos.transform.position, rocketItem);
                TimedExplosive expl = rocket.gameObject.GetComponent<TimedExplosive>();

                // Explosion settings
                if (enabled && explodeOnTargetPos)
                {
                    float dist = Vector3.Distance(turret.muzzlePos.position, turret.target.transform.position);
                    float time = dist / rocketSpeed;
                    expl.SetFuse(time);
                }
                expl.explosionRadius = explosionRadius;

                // Rocket settings
                rocket.gravityModifier = 0f;
                rocket.speed = rocketSpeed;
                rocket.InitializeVelocity(turret.muzzlePos.transform.forward);
                Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/rocket_fire.prefab", turret.muzzlePos.position);
            }

            ServerProjectile CreateRocket(Vector3 launchPos, Item rocketItem)
            {
                if (requiresAmmo)
                {
                    if (rocketItem == null)
                        return null;
                    if (rocketItem.amount == 1)
                        rocketItem.RemoveFromContainer();
                    else
                        rocketItem.UseItem(1);
                }
                else
                    rocketItem.RemoveFromContainer();

                BaseEntity rocket = null;
                if (rocketItem.info.shortname == "ammo.rocket.basic")
                    rocket = GameManager.server.CreateEntity("assets/prefabs/ammo/rocket/rocket_basic.prefab", launchPos);
                else if (rocketItem.info.shortname == "ammo.rocket.fire")
                    rocket = GameManager.server.CreateEntity("assets/prefabs/ammo/rocket/rocket_fire.prefab", launchPos);
                else if (rocketItem.info.shortname == "ammo.rocket.hv")
                    rocket = GameManager.server.CreateEntity("assets/prefabs/ammo/rocket/rocket_hv.prefab", launchPos);
                rocket.Spawn();
                return rocket.GetComponent<ServerProjectile>();
            }
        }

        class AntiAirTurret : MonoBehaviour
        {
            public float aimRadius = plugin.aaLockonRadius;
            public float fireRate = plugin.aaFireRate;
            public float rocketSpeed = plugin.aarocketSpeed;
            public float minHitDistance = plugin.aaminHitDistance;
            public float lockonTime = plugin.aaLockonTime;
            public uint rocketLauncherSkin = plugin.aaLauncherSkin;
            public float explosionRadius = plugin.aaexplosionRadius;
            public int soundEveryxUpdates = plugin.aalockonSoundEveryxUpdate;
            public float damage = plugin.damageToHeli;
            public bool requiresAmmo = plugin.aaRequiresAmmo;

            private int updateCounter = 0;
            private bool isLockedOn = false;
            private bool isLockingOn = false;
            private float lockonFinish;
            private float nextShoot;
            private AutoTurret turret;
            private BaseEntity entity;
            public BaseCombatEntity target;

            // Models
            public DroppedItem launcher_1;
            public DroppedItem rocket_1;
            public DroppedItem rocket_2;
            public DroppedItem computer;
            public DroppedItem camera_1;
            public DroppedItem camera_2;

            private void Awake()
            {
                turret = gameObject.GetComponent<AutoTurret>();
                InitAesthetics();
                turret.SetIsOnline(false);
                if (!plugin.antiairTurrets.Contains(turret))
                    plugin.antiairTurrets.Add(turret);
                turret.sightRange = 0.1f;
                if (!plugin.antiAirIDs.Contains(turret.net.ID))
                    plugin.antiAirIDs.Add(turret.net.ID);
                entity = gameObject.GetComponent<BaseEntity>();

                var collider = entity.gameObject.AddComponent<SphereCollider>();
                collider.gameObject.layer = (int)Layer.Reserved1;
                collider.radius = aimRadius;
                collider.isTrigger = true;
            }

            private void Update()
            {
                if (target == null)
                {
                    if (isLockedOn)
                        isLockedOn = false;
                    if (isLockingOn)
                        isLockingOn = false;
                    return;
                }
                if (!isLockedOn)
                {
                    turret.target = target;
                    turret.UpdateFacingToTarget();;
                    if (!isLockingOn)
                    {
                        lockonFinish = UnityEngine.Time.time + lockonTime;
                        isLockingOn = true;
                    }
                    else if (isLockingOn && UnityEngine.Time.time > lockonFinish)
                    {
                        LockToTarget(turret.target);
                        return;
                    }
                    else if (isLockingOn)
                    {
                        if (updateCounter == soundEveryxUpdates)
                        {
                            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab", turret.transform.position);
                            updateCounter = 0;
                            return;
                        }
                        updateCounter++;
                    }
                }
                else
                {
                    if (UnityEngine.Time.time < nextShoot)
                        return;
                    else
                    {
                        if (GetHitDistance(turret.muzzlePos.transform.position, turret.target.transform.position) < minHitDistance)
                            return;
                        var rocketItem = HasAmmo(turret);
                        if (rocketItem == null)
                            return;
                        FireRocket(turret, rocketItem);
                        nextShoot = UnityEngine.Time.time + fireRate;
                    }
                }
            }

            private void OnTriggerEnter(Collider col)
            {
                if (col.name != "patrol_helicopter")
                    return;
                BaseCombatEntity heli = col.GetComponentInParent<BaseCombatEntity>();
                target = heli;
            }

            /*
            private void OnTriggerExit(Collider col)
            {
                if (col.name != "patrol_helicopter")
                    return;
                turret.target = null;
                target = null;
            }
            */

            public void OnDestroy()
            {
                if (!launcher_1.IsDestroyed)
                    launcher_1.Kill();
                if (!rocket_1.IsDestroyed)
                    rocket_1.Kill();
                if (!rocket_2.IsDestroyed)
                    rocket_2.Kill();
                if (!computer.IsDestroyed)
                    computer.Kill();
                if (!camera_1.IsDestroyed)
                    camera_1.Kill();
                if (!camera_2.IsDestroyed)
                    camera_2.Kill();
            }

            public void Destroy()
            {
                if (plugin.antiairTurrets.Contains(turret))
                    plugin.antiairTurrets.Remove(turret);
                if (plugin.turretIDs.Contains(turret.net.ID))
                    plugin.turretIDs.Remove(turret.net.ID);
                Destroy(this);
            }

            public void UnloadDestroy()
            {
                if (plugin.antiairTurrets.Contains(turret))
                    plugin.antiairTurrets.Remove(turret);
                Destroy(this);
            }

            private Item HasAmmo(AutoTurret turret)
            {
                if (!requiresAmmo)
                    return ItemManager.CreateByItemID(1578894260, 1, 0);
                foreach (var item in turret.inventory.itemList)
                    if (item.info.itemid == 1578894260 || item.info.itemid == 1436532208 || item.info.itemid == 542276424)
                        return item;
                return null;
            }

            private void LockToTarget(BaseCombatEntity entity)
            {
                isLockedOn = true;
                isLockingOn = false;
                nextShoot = UnityEngine.Time.time + fireRate;
                target = entity;
            }

            private float GetHitDistance(Vector3 pos1, Vector3 pos2)
            {
                return Vector3.Distance(pos1, pos2);
            }

            private void InitAesthetics()
            {
                // Rocket Launcher
                launcher_1 = ItemManager.CreateByItemID(649603450, 1, this.rocketLauncherSkin).Drop(turret.transform.position, Vector3.zero, new Quaternion()).GetComponent<DroppedItem>();
                launcher_1.GetComponent<Rigidbody>().isKinematic = true;
                launcher_1.GetComponent<Rigidbody>().useGravity = false;
                launcher_1.allowPickup = false;
                launcher_1.CancelInvoke((Action)Delegate.CreateDelegate(typeof(Action), launcher_1, "IdleDestroy"));

                // rocket_1
                rocket_1 = ItemManager.CreateByItemID(1578894260, 1, 0).Drop(turret.transform.position, Vector3.zero, new Quaternion()).GetComponent<DroppedItem>();
                rocket_1.GetComponent<Rigidbody>().isKinematic = true;
                rocket_1.GetComponent<Rigidbody>().useGravity = false;
                rocket_1.allowPickup = false;
                rocket_1.CancelInvoke((Action)Delegate.CreateDelegate(typeof(Action), rocket_1, "IdleDestroy"));

                // rocket_2
                rocket_2 = ItemManager.CreateByItemID(1578894260, 1, 0).Drop(turret.transform.position, Vector3.zero, new Quaternion()).GetComponent<DroppedItem>();
                rocket_2.GetComponent<Rigidbody>().isKinematic = true;
                rocket_2.GetComponent<Rigidbody>().useGravity = false;
                rocket_2.allowPickup = false;
                rocket_2.CancelInvoke((Action)Delegate.CreateDelegate(typeof(Action), rocket_2, "IdleDestroy"));

                // computer
                computer = ItemManager.CreateByItemID(1490499512, 1, 0).Drop(turret.transform.position, Vector3.zero, new Quaternion()).GetComponent<DroppedItem>();
                computer.GetComponent<Rigidbody>().isKinematic = true;
                computer.GetComponent<Rigidbody>().useGravity = false;
                computer.allowPickup = false;
                computer.CancelInvoke((Action)Delegate.CreateDelegate(typeof(Action), computer, "IdleDestroy"));

                // camera_1
                camera_1 = ItemManager.CreateByItemID(1300054961, 1, 0).Drop(turret.transform.position, Vector3.zero, new Quaternion()).GetComponent<DroppedItem>();
                camera_1.GetComponent<Rigidbody>().isKinematic = true;
                camera_1.GetComponent<Rigidbody>().useGravity = false;
                camera_1.allowPickup = false;
                camera_1.CancelInvoke((Action)Delegate.CreateDelegate(typeof(Action), camera_1, "IdleDestroy"));

                // camera_2
                camera_2 = ItemManager.CreateByItemID(1300054961, 1, 0).Drop(turret.transform.position, Vector3.zero, new Quaternion()).GetComponent<DroppedItem>();
                camera_2.GetComponent<Rigidbody>().isKinematic = true;
                camera_2.GetComponent<Rigidbody>().useGravity = false;
                camera_2.allowPickup = false;
                camera_2.CancelInvoke((Action)Delegate.CreateDelegate(typeof(Action), camera_2, "IdleDestroy"));

                Vector3 computerRot = turret.gameObject.transform.localRotation.eulerAngles;
                computerRot = new Vector3(computerRot.x, computerRot.y + 240, computerRot.z);
                computer.transform.SetParent(turret.gameObject.transform);
                computer.transform.localPosition = new Vector3(0.45f, 0, 0.25f);
                computer.transform.rotation = Quaternion.Euler(computerRot);


                foreach (var entry in turret.gameObject.transform.GetAllChildren())
                {
                    if (entry.name == "weapon_socket")
                    {
                        // launcher rot
                        Vector3 launcherRot = entry.gameObject.transform.localRotation.eulerAngles;
                        launcherRot = new Vector3(launcherRot.x, launcherRot.y + 180, launcherRot.z);

                        // rocket rot
                        Vector3 rocketRot = entry.gameObject.transform.localRotation.eulerAngles;
                        rocketRot = new Vector3(rocketRot.x, rocketRot.y + 270, rocketRot.z);

                        // launcher_1
                        launcher_1.transform.SetParent(entry.gameObject.transform);
                        launcher_1.transform.localPosition = new Vector3(0, 0, 0);
                        launcher_1.transform.localRotation = Quaternion.Euler(launcherRot);

                        // rocket_1
                        rocket_1.transform.SetParent(entry.gameObject.transform);
                        rocket_1.transform.localPosition = new Vector3(0, -0.05f, 0.17f);
                        rocket_1.transform.localRotation = Quaternion.Euler(rocketRot);

                        // rocket_2
                        rocket_2.transform.SetParent(entry.gameObject.transform);
                        rocket_2.transform.localPosition = new Vector3(0, -0.05f, -0.17f);
                        rocket_2.transform.localRotation = Quaternion.Euler(rocketRot);

                        // camera_1
                        camera_1.transform.SetParent(entry.gameObject.transform);
                        camera_1.transform.localPosition = new Vector3(0, 0.05f, -0.17f);
                        camera_1.transform.localRotation = Quaternion.Euler(rocketRot);

                        // camera_2
                        camera_2.transform.SetParent(entry.gameObject.transform);
                        camera_2.transform.localPosition = new Vector3(0, 0.05f, 0.17f);
                        camera_2.transform.localRotation = Quaternion.Euler(rocketRot);
                    }
                }
            }

            void FireRocket(AutoTurret turret, Item rocketItem)
            {
                ServerProjectile rocket = CreateRocket(turret.muzzlePos.transform.position, rocketItem);
                TimedExplosive expl = rocket.gameObject.GetComponent<TimedExplosive>();

                // Explosion settings
                float dist = Vector3.Distance(turret.muzzlePos.position, turret.target.transform.position);
                float time = dist / rocketSpeed;
                //expl.SetFuse(time);
                plugin.timer.Once((time - 0.1f), () =>
                {
                    if (target)
                        target.Hurt(damage);
                });
                expl.explosionRadius = explosionRadius;

                // Rocket settings
                rocket.gravityModifier = 0f;
                rocket.speed = rocketSpeed;
                rocket.InitializeVelocity(turret.muzzlePos.transform.forward);
                Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/rocket_fire.prefab", turret.muzzlePos.position);
                rocket.gameObject.AddComponent<HeatRocket>().target = target;
            }

            ServerProjectile CreateRocket(Vector3 launchPos, Item rocketItem)
            {
                if (requiresAmmo)
                {
                    if (rocketItem == null)
                        return null;
                    if (rocketItem.amount == 1)
                        rocketItem.RemoveFromContainer();
                    else
                        rocketItem.UseItem(1);
                }

                BaseEntity rocket = null;
                if (rocketItem.info.shortname == "ammo.rocket.basic")
                    rocket = GameManager.server.CreateEntity("assets/prefabs/ammo/rocket/rocket_basic.prefab", launchPos);
                else if (rocketItem.info.shortname == "ammo.rocket.fire")
                    rocket = GameManager.server.CreateEntity("assets/prefabs/ammo/rocket/rocket_fire.prefab", launchPos);
                else if (rocketItem.info.shortname == "ammo.rocket.hv")
                    rocket = GameManager.server.CreateEntity("assets/prefabs/ammo/rocket/rocket_hv.prefab", launchPos);
                rocket.Spawn();
                return rocket.GetComponent<ServerProjectile>();
            }
        }

        class HeatRocket : MonoBehaviour
        {
            ServerProjectile rocket;
            public BaseCombatEntity target;

            private void Awake()
            {
                rocket = gameObject.GetComponent<ServerProjectile>();
            }

            private void Update()
            {
                if (target == null)
                    return;

                Vector3 x = (target.transform.position - rocket.transform.position);
                rocket.InitializeVelocity(x);
            }
        }

        #endregion

        object OnTurretStartup(AutoTurret turret)
        {
            if (antiairTurrets.Contains(turret))
                return false;
            else
                return null;
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            BaseEntity entity = container.entityOwner;
            if (!entity) return;
            if (entity.ShortPrefabName != "autoturret_deployed") return;
            if (rocketTurrets.Contains(entity.GetComponent<AutoTurret>()))
            {
                if (item.info.shortname == "ammo.rocket.basic" || item.info.shortname == "ammo.rocket.fire" || item.info.shortname == "ammo.rocket.hv")
                    return;
                if (itemCache.ContainsKey(item))
                    item.MoveToContainer(itemCache[item]);
            }
        }

        void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            BasePlayer player = container.GetOwnerPlayer();
            if (player == null) return;
            if (!itemCache.ContainsKey(item))
            {
                itemCache.Add(item, container);
                timer.Once(0.05f, () => itemCache.Remove(item));
            }
        }

        void CanPickupEntity(BaseCombatEntity entity, BasePlayer player)
        {
            AutoTurret turret = entity.GetComponent<AutoTurret>();
            if (!turret)
                return;
            if (turret.GetComponent<RocketTurret>())
                ReturnItems(player.inventory.containerMain, rocketUpgradeCost);
            else if (turret.GetComponent<AntiAirTurret>())
                ReturnItems(player.inventory.containerMain, antiAirUpgradeCost);
            return;
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            AutoTurret turret = entity.GetComponent<AutoTurret>();
            if (!turret)
                return;
            RocketTurret rt = turret.GetComponent<RocketTurret>();
            if (!rt)
                return;
            turretIDs.Remove(turret.net.ID);
            rocketTurrets.Remove(turret);
        }

        [ChatCommand("turret")]
        void RocketTurretCMD(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permissionName))
            {
                player.ChatMessage(msg("No Permission", player.UserIDString));
                return;
            }
            if (args.Length == 0)
            {
                DoHelpMenu(player);
                return;
            }
            if (args.Length == 1)
            {
                if (args[0] == "cost")
                {
                    DoItemMenu(player);
                    return;
                }
                else if (args[0] == "help")
                {
                    DoHelpMenu(player);
                    return;
                }
                else if (args[0] == "rocket" || args[0] == "normal" || args[0] == "antiair")
                {
                    RaycastHit hit;
                    if (!UnityEngine.Physics.Raycast(player.eyes.HeadRay(), out hit, Mathf.Infinity, collLayers))
                    {
                        player.ChatMessage(msg("Invalid Destination", player.UserIDString));
                        return;
                    }
                    BaseEntity entity = hit.GetEntity();
                    if (entity == null)
                    {
                        player.ChatMessage(msg("Invalid Destination", player.UserIDString));
                        return;
                    }
                    if (entity.ShortPrefabName == "autoturret_deployed")
                    {
                        AutoTurret turret = entity.GetComponent<AutoTurret>();
                        if (!turret)
                        {
                            PrintError("Turret component was null!");
                            return;
                        }
                        if (!turret.IsAuthed(player))
                        {
                            player.ChatMessage(msg("Not Authed On Turret", player.UserIDString));
                            return;
                        }
                        if (args.Length > 0 && args[0] == "rocket")
                        {
                            if (!permission.UserHasPermission(player.UserIDString, permissionNameROCKET))
                            {
                                player.ChatMessage(msg("No Permission", player.UserIDString));
                                return;
                            }
                            if (rocketTurrets.Contains(turret))
                            {
                                player.ChatMessage(msg("Already A Rocket Turret"));
                                return;
                            }
                            if (!CanUpgade(player.inventory.containerMain, player.inventory.containerBelt, "rocket"))
                            {
                                player.ChatMessage(msg("Invalid Items", player.UserIDString));
                                return;
                            }
                            rocketTurrets.Add(turret);
                            turret.gameObject.AddComponent<RocketTurret>();
                            RocketTurret rt = turret.gameObject.GetComponent<RocketTurret>();
                            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab", turret.transform.position);
                            RemoveAmmo(turret.inventory);
                            turret.inventory.onlyAllowedItem = null;
                            if (!rt) return;
                        }
                        else if (args.Length > 0 && args[0] == "normal")
                        {
                            if (!rocketTurrets.Contains(turret) && !antiairTurrets.Contains(turret))
                            {
                                player.ChatMessage(msg("Already A Normal Turret", player.UserIDString));
                                return;
                            }
                            RocketTurret rt = turret.gameObject.GetComponent<RocketTurret>();
                            AntiAirTurret at = turret.gameObject.GetComponent<AntiAirTurret>();
                            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab", turret.transform.position);

                            if (returnCostOnDowngrade)
                            {
                                if (turret.GetComponent<RocketTurret>())
                                    ReturnItems(player.inventory.containerMain, rocketUpgradeCost);
                                else if (turret.GetComponent<AntiAirTurret>())
                                    ReturnItems(player.inventory.containerMain, antiAirUpgradeCost);
                            }

                            if (rt)
                                rt.Destroy();
                            if (at)
                                at.Destroy();

                            turret.inventory.onlyAllowedItem = ItemManager.FindItemDefinition("ammo.rifle");
                            if (rocketTurrets.Contains(turret))
                                rocketTurrets.Remove(turret);
                            else if (antiairTurrets.Contains(turret))
                                antiairTurrets.Remove(turret);
                        }
                        else if (args.Length > 0 && args[0] == "antiair")
                        {
                            if (!permission.UserHasPermission(player.UserIDString, permissionNameANTIAIR))
                            {
                                player.ChatMessage(msg("No Permission", player.UserIDString));
                                return;
                            }
                            if (antiairTurrets.Contains(turret))
                            {
                                player.ChatMessage(msg("Already an anti-air turret", player.UserIDString));
                                return;
                            }
                            if (!CanUpgade(player.inventory.containerMain, player.inventory.containerBelt, "antiair"))
                            {
                                player.ChatMessage(msg("Invalid Items", player.UserIDString));
                                return;
                            }
                            antiairTurrets.Add(turret);
                            turret.gameObject.AddComponent<AntiAirTurret>();
                            AntiAirTurret rt = turret.gameObject.GetComponent<AntiAirTurret>();
                            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab", turret.transform.position);
                            RemoveAmmo(turret.inventory);
                            turret.inventory.onlyAllowedItem = null;
                            if (!rt) return;
                        }
                    }
                    else
                    {
                        player.ChatMessage(msg("Invalid Entity", player.UserIDString));
                        return;
                    }
                    return;
                }
            }
        }

        void ReturnItems(ItemContainer container, Dictionary<string, object> dic)
        {
            foreach(var item in ItemManager.itemList)
                foreach(var entry in dic)
                    if (item.shortname == entry.Key)
                    {
                        Item newItem = ItemManager.CreateByName(entry.Key, (int)entry.Value, 0);
                        if (container.IsFull())
                            newItem.Drop(container.playerOwner.transform.position, container.playerOwner.transform.position, new Quaternion());
                        else
                            newItem.MoveToContainer(container);
                    }
        }

        void DoHelpMenu(BasePlayer player)
        {
            StringBuilder x = new StringBuilder();
            x.AppendLine(msg("Help Menu Header", player.UserIDString));
            if (permission.UserHasPermission(player.UserIDString, permissionName))
                x.AppendLine(msg("Help Menu line1", player.UserIDString));
            x.AppendLine(msg("Help Menu line2", player.UserIDString));
            if (permission.UserHasPermission(player.UserIDString, permissionNameANTIAIR))
                x.AppendLine(msg("Help Menu line3", player.UserIDString));
            x.AppendLine(msg("Help Menu line4", player.UserIDString));
            player.ChatMessage(x.ToString().TrimEnd());
        }

        void DoItemMenu(BasePlayer player)
        {
            StringBuilder x = new StringBuilder();
            if (permission.UserHasPermission(player.UserIDString, permissionName))
            {
                x.AppendLine(msg("Cost Menu Header (rocket)", player.UserIDString));
                foreach (var entry in rocketUpgradeCost)
                {
                    foreach (var item in ItemManager.itemList)
                        if (item.shortname == entry.Key)
                            x.AppendLine(string.Format(msg("Cost Menu Entry", player.UserIDString), item.displayName.english, entry.Value.ToString()));
                }
                x.AppendLine();
            }
            if (permission.UserHasPermission(player.UserIDString, permissionNameANTIAIR))
            {
                x.AppendLine(msg("Cost Menu Header (anti-air)", player.UserIDString));
                foreach (var entry in antiAirUpgradeCost)
                {
                    foreach (var item in ItemManager.itemList)
                        if (item.shortname == entry.Key)
                            x.AppendLine(string.Format(msg("Cost Menu Entry", player.UserIDString), item.displayName.english, entry.Value.ToString()));
                }
            }
            player.ChatMessage(x.ToString().TrimEnd());
        }

        void RemoveAmmo(ItemContainer container)
        {
            foreach(var item in container.itemList)
                if (item.info.shortname != "ammo.rocket.basic" || item.info.shortname != "ammo.rocket.fire" || item.info.shortname != "ammo.rocket.hv")
                    item.Drop(new Vector3(container.entityOwner.transform.position.x, container.entityOwner.transform.position.y + 1f, container.entityOwner.transform.position.z), Vector3.zero);
        }

        private bool CanUpgade(ItemContainer container, ItemContainer container2, string type)
        {
            Dictionary<string, int> itemsNeeded = new Dictionary<string, int>();
            Dictionary<Item, int> cache = new Dictionary<Item, int>();

            if (type == "rocket")
            {
                foreach (var entry in rocketUpgradeCost)
                    itemsNeeded.Add(entry.Key, Convert.ToInt32(entry.Value));
            }
            else if (type == "antiair")
            {
                foreach (var entry in antiAirUpgradeCost)
                    itemsNeeded.Add(entry.Key, Convert.ToInt32(entry.Value));
            }
            foreach(Item item in container.itemList)
            {
                if (itemsNeeded.ContainsKey(item.info.shortname))
                    if (itemsNeeded[item.info.shortname] > 0)
                    {
                        if (item.amount < itemsNeeded[item.info.shortname])
                        {
                            cache.Add(item, item.amount);
                            itemsNeeded[item.info.shortname] -= item.amount;
                        }
                        else if (item.amount > itemsNeeded[item.info.shortname])
                        {
                            cache.Add(item, itemsNeeded[item.info.shortname]);
                            itemsNeeded[item.info.shortname] = 0;
                        }
                        else
                        {
                            cache.Add(item, item.amount);
                            itemsNeeded[item.info.shortname] = 0;
                        }
                    }
            }
            foreach (Item item in container2.itemList)
            {
                if (itemsNeeded.ContainsKey(item.info.shortname))
                    if (itemsNeeded[item.info.shortname] > 0)
                    {
                        if (item.amount < itemsNeeded[item.info.shortname])
                        {
                            cache.Add(item, item.amount);
                            itemsNeeded[item.info.shortname] -= item.amount;
                        }
                        else if (item.amount > itemsNeeded[item.info.shortname])
                        {
                            cache.Add(item, itemsNeeded[item.info.shortname]);
                            itemsNeeded[item.info.shortname] = 0;
                        }
                        else
                        {
                            cache.Add(item, item.amount);
                            itemsNeeded[item.info.shortname] = 0;
                        }
                    }
            }
            foreach (var entry in itemsNeeded)
            {
                if (entry.Value > 0)
                    return false;
            }
            RemoveItems(cache, container);
            return true;
        }

        void RemoveItems(Dictionary<Item, int> dic, ItemContainer container)
        {
            foreach(var entry in dic)
            {
                if (entry.Key.amount == entry.Value)
                    entry.Key.RemoveFromContainer();
                else
                    entry.Key.UseItem(entry.Value);
            }
            return;
        }

        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }

        string msg(string key, string id = null) => lang.GetMessage(key, this, id);
    }
}