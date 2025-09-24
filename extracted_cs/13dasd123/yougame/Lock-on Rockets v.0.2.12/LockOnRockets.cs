using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("LockOnRockets", "k1lly0u", "0.2.12", ResourceId = 0)]
    class LockOnRockets : RustPlugin
    {
        #region Fields  
        private bool debug = false;

        static LockOnRockets ins;
        private Dictionary<LockTypes, bool> lockTypes;

        private bool initialized;
        private Dictionary<ulong, LockOnPlayer> rocketeers = new Dictionary<ulong, LockOnPlayer>();
        private Dictionary<string, ItemDefinition> itemDefinitions = new Dictionary<string, ItemDefinition>();

        private static LayerMask layerMask;

        const string c4Explosion = "assets/prefabs/tools/c4/effects/c4_explosion.prefab";
        const string smokePrefab = "assets/bundled/prefabs/fx/smoke_signal_full.prefab";
        const string lockBeep = "assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab";
        const string rocketPrefab = "assets/prefabs/npc/patrol helicopter/rocket_heli.prefab";
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {            
            permission.RegisterPermission("lockonrockets.craft", this);
            rocketeers = new Dictionary<ulong, LockOnPlayer>();
            lang.RegisterMessages(Messages, this);

            layerMask = (1 << 29);
            layerMask |= (1 << 28);
            layerMask |= (1 << 18);
            layerMask = ~layerMask;
        }

        private void OnServerInitialized()
        {
            ins = this;

            itemDefinitions = ItemManager.itemList.ToDictionary(x => x.shortname);

            lockTypes = configData.LockOnTypes;
            initialized = true;

            ValidateCraftingConfig();

            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerInit(player);
        }

        private void OnPlayerInit(BasePlayer player)
        {
            player.gameObject.AddComponent<WeaponMonitor>();
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (rocketeers.ContainsKey(player.userID))
            {
                UnityEngine.Object.DestroyImmediate(player.GetComponent<LockOnPlayer>());
                rocketeers.Remove(player.userID);
            }
            UnityEngine.Object.Destroy(player.GetComponent<WeaponMonitor>());
        }

        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (!initialized) return;
            if (item.info.itemid == -17123659 && container.playerOwner != null)
            {
                SendReply(container.playerOwner, msg("inventory", container.playerOwner.UserIDString));
            }
        }

        private void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            if (player.GetComponent<LockOnPlayer>())
            {
                if (!player.GetComponent<LockOnPlayer>().HasTargetLocked()) return;
                if (entity.ShortPrefabName == "rocket_smoke")
                {
                    var rocket = GameManager.server.CreateEntity(rocketPrefab, entity.transform.position, entity.transform.rotation);
                    rocket.OwnerID = player.userID;
                    rocket.creatorEntity = player;
                    rocket.Spawn();

                    var projectile = rocket.GetComponent<ServerProjectile>();
                    var explosive = rocket.GetComponent<TimedExplosive>();

                    projectile.gravityModifier = 0;
                    explosive.damageTypes = new List<Rust.DamageTypeEntry>();
                    explosive.SetFuse(configData.DetonationTime);

                    if (!ins.configData.DisableSmokeEffects)
                        Effect.server.Run(smokePrefab, rocket.GetComponent<BaseEntity>(), 0, new Vector3(), new Vector3(), null, false);
                    rocket.GetComponent<ServerProjectile>().InitializeVelocity(Vector3.forward);

                    NextTick(() => entity.Kill());
                    rocketeers[player.userID].RocketFired(rocket);
                }
            }         
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (!initialized) return;
            var rocketeer = player.GetComponent<LockOnPlayer>();
            if (rocketeer != null)
            {
                if (input.WasJustPressed(BUTTON.FIRE_SECONDARY))
                {
                    rocketeer.isEnabled = true;
                }                
                else if (input.WasJustReleased(BUTTON.FIRE_SECONDARY))
                {
                    rocketeer.ClearTarget();
                    rocketeer.isEnabled = false;
                }       
            }
        }

        private void Unload()
        {
            ins = null;

            var components = UnityEngine.Object.FindObjectsOfType<LockOnPlayer>();
            if (components != null)
                foreach (var obj in components)
                    UnityEngine.Object.Destroy(obj);

            var monitors = UnityEngine.Object.FindObjectsOfType<WeaponMonitor>();
            if (monitors != null)
                foreach (var obj in monitors)
                    UnityEngine.Object.Destroy(obj);

            var rockets = UnityEngine.Object.FindObjectsOfType<HomingRocket>();
            if (rockets != null)
                foreach (var rocket in rockets)
                    UnityEngine.Object.Destroy(rocket);
        }
        #endregion

        #region UI        
        public class UI
        {
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool useCursor = false)
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
                        new CuiElement().Parent = "Overlay",
                        panelName
                    }
                };
                return NewElement;
            }           
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }                       
        }

        const string LockOnUI = "UI_LockOn";

        private void AddUI(BasePlayer player, string message)
        {
            var container = UI.CreateElementContainer(LockOnUI, "0 0 0 0", "0.55 0.45", "0.95 0.55");
            UI.CreateLabel(ref container, LockOnUI, "", message, 16, "0 0", "1 1", TextAnchor.MiddleLeft);
            CuiHelper.DestroyUi(player, LockOnUI);
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Components
        private class WeaponMonitor : MonoBehaviour
        {
            private BasePlayer player;
            private LockOnPlayer component;
            private bool canLockOn;

            private bool hasAmmo;
            private bool unloadedAmmo;            

            void Awake() => player = GetComponent<BasePlayer>();            
            void Update()
            {
                canLockOn = false;
                var activeItem = player.GetActiveItem();
                if (activeItem != null && activeItem.info.itemid == 442886268)
                {
                    BaseProjectile weapon = activeItem.GetHeldEntity() as BaseProjectile;
                    if (weapon != null)
                    {
                        if (weapon.primaryMagazine != null)
                        {
                            if (hasAmmo && weapon.primaryMagazine.contents == 0)
                                unloadedAmmo = true;
                            if (weapon.primaryMagazine.ammoType.itemid == -17123659 && weapon.primaryMagazine.contents > 0)
                            {
                                hasAmmo = true;
                                canLockOn = true;
                            }
                            else
                            {
                                hasAmmo = false;
                            }
                        }
                    }
                }
                if (canLockOn)
                {
                    if (component == null)
                    {                        
                        ins.rocketeers[player.userID] = component = player.gameObject.AddComponent<LockOnPlayer>();
                        ins.SendReply(player, msg("loaded", player.UserIDString));
                    }
                }
                else
                {
                    if (component != null)
                    {
                        component.isEnabled = false;
                        ins.rocketeers.Remove(player.userID);
                        DestroyImmediate(component);

                        if (unloadedAmmo)
                        {
                            ins.SendReply(player, msg("unloaded", player.UserIDString));
                            unloadedAmmo = false;
                        }
                    }
                }
            }
        }

        private class LockOnPlayer : MonoBehaviour
        {
            private BasePlayer player;
            private RaycastHit rayHit;
            private BaseEntity target;

            private float lockOnSeconds;

            private bool targetLocked;
            private bool isBeeping;
            public bool isEnabled;

            private bool openUI;
            private bool isDrawing;
            
            private void Awake()
            {                
                player = GetComponent<BasePlayer>();
                target = null;
                isEnabled = false;
            }

            private void OnDestroy()
            {
                CuiHelper.DestroyUi(player, LockOnUI);
            }

            public void Update()
            {
                if (isEnabled)
                    LockTarget();                
            }

            public void ClearTarget()
            {
                CancelInvoke();
                openUI = false;                
                isBeeping = false;
                isDrawing = false;
                lockOnSeconds = 0;
                target = null;
                targetLocked = false;
                CuiHelper.DestroyUi(player, LockOnUI);
            }

            public void RocketFired(BaseEntity rocket)
            {
                if (targetLocked && target != null)
                {
                    enabled = false;
                    isEnabled = false;                   
                    CancelInvoke();
                    var homing = rocket.gameObject.AddComponent<HomingRocket>();
                    homing.SetPlayer(player, target);
                    ClearTarget();
                }
                DestroyImmediate(this);
            }

            public bool HasTargetLocked() => targetLocked;

            private void LockTarget()
            {
                if (Physics.SphereCast(new Ray(player.eyes.position, (player.eyes.rotation * player.eyes.headRotation) * Vector3.forward), 0.25f, out rayHit, 2000, layerMask, QueryTriggerInteraction.Collide))
                {
                    var newTarget = rayHit.GetEntity();
                    if (newTarget != null)
                    {
                        if (newTarget.GetComponent<CargoPlane>())
                        {
                            if (!ins.lockTypes[LockTypes.Plane])
                                return;
                        }
                        else if (newTarget.GetComponent<BasePlayer>())
                        {
                            if (!ins.lockTypes[LockTypes.Player])
                                return;
                        }
                        else if (newTarget.GetComponent<BaseNpc>())
                        {
                            if (!ins.lockTypes[LockTypes.Animal])
                                return;
                        }
                        else if (newTarget.GetComponent<AutoTurret>() || newTarget.GetComponent<FlameTurret>() || newTarget.GetComponent<GunTrap>())
                        {
                            if (!ins.lockTypes[LockTypes.GunTraps])
                                return;
                        }
                        else if (newTarget.GetComponent<BradleyAPC>())
                        {
                            if (!ins.lockTypes[LockTypes.Tank])
                                return;
                        }
                        else if (newTarget.GetComponent<BaseHelicopter>() || newTarget.GetComponent<CH47Helicopter>())
                        {
                            if (!ins.lockTypes[LockTypes.Helicopter])
                                return;
                        }
                        else if (newTarget.GetComponent<BaseCar>())
                        {
                            if (!ins.lockTypes[LockTypes.Car])
                                return;
                        }
                        else if (newTarget.GetComponent<LootContainer>() || newTarget.GetComponent<StorageContainer>())
                        {
                            if (!ins.lockTypes[LockTypes.Loot])
                                return;
                        }
                        else if (newTarget.GetComponent<ResourceEntity>() || newTarget.GetComponent<TreeEntity>())
                        {
                            if (!ins.lockTypes[LockTypes.Resource])
                                return;
                        }
                        else if (newTarget.GetComponent<BuildingBlock>() || newTarget.GetComponent<SimpleBuildingBlock>())
                        {
                            if (!ins.lockTypes[LockTypes.Structure])
                                return;
                        }
                        else return;                       
                        
                        if (target != null && newTarget != target)
                        {
                            CancelInvoke();
                            lockOnSeconds = 0;

                            CuiHelper.DestroyUi(player, LockOnUI);
                            openUI = false;

                            isDrawing = false;
                            isBeeping = false;

                            target = newTarget;
                        }

                        if (!isBeeping)
                            Beep();

                        if (!openUI)
                        {
                            ins.AddUI(player, msg("aquiring", player.UserIDString));
                            openUI = true;
                        }

                        lockOnSeconds += Time.deltaTime;

                        if (lockOnSeconds >= ins.configData.TimeToLockOn)
                        {
                            target = newTarget;
                            targetLocked = true;
                            isEnabled = false;

                            ins.AddUI(player, msg("locked", player.UserIDString));
                        }
                        if (!isDrawing)
                        {
                            bool tempAdmin = false;
                            if (!player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin))
                            {
                                tempAdmin = true;
                                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                            }
                            player.SendConsoleCommand("ddraw.box", 0.2f, (targetLocked ? Color.green : Color.red), rayHit.point, 0.5f);

                            if (tempAdmin)
                                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);

                            isDrawing = true;
                            Invoke("StopDrawing", 0.2f);
                        }
                    }
                    else
                    {
                        ClearTarget();
                    }
                }
            }

            private void Beep()
            {
                isBeeping = true;
                Effect.server.Run(lockBeep, player.transform.position + player.transform.forward);
                Invoke("Beep", targetLocked ? 0.25f : 1f);
            }

            private void StopDrawing() => isDrawing = false;
        }

        private class HomingRocket : MonoBehaviour
        {
            private ServerProjectile rocket;

            private BaseEntity target;
            private BasePlayer player;            

            private float totalDistance;
            private float fraction;
            
            private void Awake()
            {
                rocket = GetComponent<ServerProjectile>();

                if (!ins.configData.DisableSmokeEffects)
                    Effect.server.Run(smokePrefab, rocket.GetComponent<BaseEntity>(), 0, new Vector3(), new Vector3(), null, false);
            }

            private void FixedUpdate()
            {               
                if (target == null) return;

                Vector3 targetPos = target.transform.position + new Vector3(0, target.bounds.center.y / 2, 0);

                float distance = Vector3.Distance(rocket.transform.position, targetPos);

                if (distance <= 0.25f)
                {
                    Destroy(this);                    
                    return;
                }
                Vector3 direction = (targetPos - rocket.transform.position).normalized;
                rocket.InitializeVelocity(direction * rocket.speed);

                var remaining = totalDistance - distance;
                if (remaining > 0 && totalDistance > 0)
                    fraction = remaining / totalDistance;
            }

            private void OnDestroy()
            {
                BaseEntity entity = rocket.GetComponent<BaseEntity>();

                if (entity != null && !entity.IsDestroyed)
                    entity.Kill();
                
                CancelInvoke();
                ins.RadiusDamage(player, entity, rocket.transform.position);
                Effect.server.Run(c4Explosion, rocket.transform.position);
            }
            
            public void SetPlayer(BasePlayer player, BaseEntity target)
            {
                if (ins.debug)
                {
                    if (player == null)
                        print($"[LockOnRockets] [ERROR] - Player is null");
                    if (target == null)
                        print($"[LockOnRockets] [ERROR] - Target is null");
                    if (rocket == null)
                        print($"[LockOnRockets] [ERROR] - Rocket is null");
                    if (ins.configData == null)
                        print($"[LockOnRockets] [ERROR] - ConfigData is null");
                    if (ins.configData.HelicopterLockModifiers == null)
                        print($"[LockOnRockets] [ERROR] - HelicopterLockModifiers is null");
                }
                this.player = player;
                this.target = target;

                rocket.speed = (target is BaseHelicopter || target is CH47Helicopter) ? ins.configData.RocketSpeed * ins.configData.HelicopterLockModifiers.SpeedModifier : ins.configData.RocketSpeed;
                totalDistance = Vector3.Distance(player.transform.position, target.transform.position);
                fraction = 0;

                if (!ins.configData.DisableRocketBeep)
                    Beep();
            }

            private void Beep()
            {
                Effect.server.Run(lockBeep, rocket.transform.position + Vector3.up);
                Invoke("Beep", 1f - fraction);
            }
        }
        #endregion

        #region Damage        
        private void RadiusDamage(BaseEntity attackingPlayer, BaseEntity weaponPrefab, Vector3 pos)
        {
            List<HitInfo> hitInfo = new List<HitInfo>();
            List<BaseCombatEntity> baseCombatEntities = Facepunch.Pool.GetList<BaseCombatEntity>();
            Vis.Entities(pos, 3.8f, baseCombatEntities);
            baseCombatEntities = baseCombatEntities.Distinct().ToList();
            for (int i = 0; i < baseCombatEntities.Count; i++)
            {
                BaseCombatEntity item = baseCombatEntities[i];
                if (item != null && !item.IsDestroyed)
                {
                    Vector3 closestPoint = item.ClosestPoint(pos);
                    float distance = Vector3.Distance(closestPoint, pos);
                    float damageScale = Mathf.Clamp01((distance - 1.5f) / (3.8f - 1.5f));
                    if (damageScale <= 1f)
                    {
                        float scale = 1f - damageScale;
                        HitInfo info = new HitInfo()
                        {
                            Initiator = attackingPlayer,
                            WeaponPrefab = weaponPrefab
                        };
                        info.damageTypes.Add(new List<Rust.DamageTypeEntry>
                        {
                            new Rust.DamageTypeEntry
                            {
                                amount = (item is BaseHelicopter || item is CH47Helicopter) ? configData.RocketDamage * configData.HelicopterLockModifiers.DamageModifier : configData.RocketDamage,
                                type = Rust.DamageType.Explosion
                            },
                            new Rust.DamageTypeEntry
                            {
                                amount = 75,
                                type = Rust.DamageType.Blunt
                            }
                        });
                        info.damageTypes.ScaleAll(scale);
                        info.HitPositionWorld = closestPoint;
                        info.HitNormalWorld = (pos - closestPoint).normalized;
                        info.PointStart = pos;
                        info.PointEnd = info.HitPositionWorld;
                        hitInfo.Add(info);
                    }
                }
            }
            for (int j = 0; j < baseCombatEntities.Count; j++)
            {
                BaseCombatEntity baseCombatEntity = baseCombatEntities[j];
                HitInfo info = hitInfo[j];

                if (baseCombatEntity is BaseHelicopter && (baseCombatEntity as BaseHelicopter).enabled && baseCombatEntity.health - info.damageTypes.Total() <= 0)                                         
                    (baseCombatEntity as BaseHelicopter).Die(info);                
                else
                {
                    baseCombatEntity.Hurt(info);
                    baseCombatEntity.DoHitNotify(info);
                }
            }
            Facepunch.Pool.FreeList(ref baseCombatEntities);
        }
        #endregion

        #region Crafting
        private void ValidateCraftingConfig()
        {
            foreach (ConfigData.CraftingItems craftingItem in configData.CraftCost)
            {
                ItemDefinition itemDefinition;
                if (!itemDefinitions.TryGetValue(craftingItem.Shortname, out itemDefinition))
                {
                    PrintError($"An invalid item shortname has been set in the crafting section of the config, crafting has been disabled: {craftingItem.Shortname}");
                    configData.EnableCrafting = false;
                }
            }
        }

        [ChatCommand("craft.lockon")]
        private void cmdCraftLockon(BasePlayer player, string command, string[] args)
        {
            if (!configData.EnableCrafting)
                return;

            if (!permission.UserHasPermission(player.UserIDString, "lockonrockets.craft"))
            {
                SendReply(player, msg("noPerms", player.UserIDString));
                return;                
            }

            int amount = 1;
            if (args.Length > 0)
            {
                if (!int.TryParse(args[0], out amount))                
                    amount = 1;                
            }

            if (!HasResourcesForCrafting(player, amount))
            {
                SendReply(player, string.Format(msg("notEnoughResources", player.UserIDString), GetRequiredResources()));
                return;
            }

            TakeAllResources(player, amount);

            player.GiveItem(ItemManager.CreateByItemID(-17123659, amount, 0), BaseEntity.GiveItemReason.PickedUp);

        }

        private bool HasResourcesForCrafting(BasePlayer player, int amount)
        {
            foreach (ConfigData.CraftingItems craftingItem in configData.CraftCost)
            {
                ItemDefinition itemDefinition = itemDefinitions[craftingItem.Shortname];
                
                if (!HasEnoughResources(player, itemDefinition.itemid, craftingItem.Amount * amount))
                    return false;
            }
            return true;
        }

        private bool HasEnoughResources(BasePlayer player, int itemid, int amount) => player.inventory.GetAmount(itemid) >= amount;

        private string GetRequiredResources()
        {
            string message = string.Empty;

            for (int i = 0; i < configData.CraftCost.Count; i++)
            {
                ItemDefinition itemDefinition = itemDefinitions[configData.CraftCost[i].Shortname];
                message += $"{configData.CraftCost[i].Amount} x {itemDefinition.displayName.english}";
                if (i < configData.CraftCost.Count - 1)
                    message += ", ";
            }
            return message;
        }

        private void TakeAllResources(BasePlayer player, int amount)
        {
            foreach (ConfigData.CraftingItems craftingItem in configData.CraftCost)
            {
                ItemDefinition itemDefinition = itemDefinitions[craftingItem.Shortname];

                TakeResources(player, itemDefinition.itemid, craftingItem.Amount * amount);
            }
        }

        private void TakeResources(BasePlayer player, int itemid, int amount) => player.inventory.Take(null, itemid, amount);
        #endregion

        #region Config 
        enum LockTypes { Helicopter, Tank, Plane, Player, Animal, Structure, Resource, Loot, Car, GunTraps }

        private ConfigData configData;
        private class ConfigData
        {
            public float TimeToLockOn { get; set; }
            public bool DisableSmokeEffects { get; set; }
            public bool DisableRocketBeep { get; set; }
            public float DetonationTime { get; set; }
            public float RocketSpeed { get; set; }
            public float RocketDamage { get; set; }
            public HelicopterMods HelicopterLockModifiers { get; set; }
            public Dictionary<LockTypes, bool> LockOnTypes { get; set; }
            public bool EnableCrafting { get; set; }
            public List<CraftingItems> CraftCost { get; set; }

            public class HelicopterMods
            {
                public float DamageModifier { get; set; }
                public float SpeedModifier { get; set; }
            }

            public class CraftingItems
            {
                public string Shortname { get; set; }
                public int Amount { get; set; }
            }

            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                DetonationTime = 30f,
                RocketSpeed = 40,
                DisableRocketBeep = false,
                DisableSmokeEffects = false,
                TimeToLockOn = 3,
                RocketDamage = 300,
                HelicopterLockModifiers = new ConfigData.HelicopterMods
                {
                    DamageModifier = 5.0f,
                    SpeedModifier = 2.5f
                },
                LockOnTypes = new Dictionary<LockTypes, bool>
                {
                    [LockTypes.Animal] = true,
                    [LockTypes.Plane] = true,
                    [LockTypes.Player] = true,
                    [LockTypes.Helicopter] = true,
                    [LockTypes.Structure] = true,
                    [LockTypes.Resource] = true,
                    [LockTypes.Loot] = true,
                    [LockTypes.Tank] = true,
                    [LockTypes.Car] = true,
                    [LockTypes.GunTraps] = true
                },
                EnableCrafting = false,
                CraftCost = new List<ConfigData.CraftingItems>
                {
                    new ConfigData.CraftingItems
                    {
                        Shortname = "ammo.rocket.basic",
                        Amount = 1
                    },
                    new ConfigData.CraftingItems
                    {
                        Shortname = "techparts",
                        Amount = 3
                    }
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new Core.VersionNumber(0, 2, 05))
            {
                configData.EnableCrafting = baseConfig.EnableCrafting;
                configData.CraftCost = baseConfig.CraftCost;
            }

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }       
        #endregion

        #region Localization
        static string msg(string key, string playerId = null) => ins.lang.GetMessage(key, ins, playerId);
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["locked"] = ">><color=#00E500> Target Locked </color><<" ,
            ["aquiring"] = ">><color=#E50000> Aquiring Target </color><<",
            ["unloaded"] = "<color=#939393>You have unloaded your </color><color=#C4FF00>lock-on rocket</color><color=#939393> from your launcher</color>",
            ["loaded"] = "<color=#939393>A </color><color=#C4FF00>lock-on rocket</color><color=#939393> is loaded in your rocket launcher!</color>",
            ["inventory"] = "<color=#939393>You have a </color><color=#C4FF00>lock-on rocket</color><color=#939393> in your inventory! To use it load the smoke rocket into your rocket launcher</color>",
            ["noPerms"] = "You do not have permission to use this command" ,
            ["notEnoughResources"] = "You do not have the required resources to craft a lock-on rocket\nResources required per rocket: {0}",
            ["craftSuccess"] = "You have crafted a lock-on rocket!"
        };
        #endregion
    }
}
