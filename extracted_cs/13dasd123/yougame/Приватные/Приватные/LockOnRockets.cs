// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("LockOnRockets", "S1m0n", "0.1.5")]
    class LockOnRockets : RustPlugin
    {
        #region Fields        
        static LockOnRockets ins;
        static Dictionary<LockTypes, bool> lockTypes;

        private bool initialized;
        private Dictionary<ulong, LockOnPlayer> rocketeers;

        const string c4Explosion = "assets/prefabs/tools/c4/effects/c4_explosion.prefab";
        const string smokePrefab = "assets/bundled/prefabs/fx/smoke_signal_full.prefab";
        const string lockBeep = "assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab";
        const string rocketPrefab = "assets/prefabs/ammo/rocket/rocket_basic.prefab";
        #endregion

        #region Oxide Hooks
        void Loaded()
        {            
            rocketeers = new Dictionary<ulong, LockOnPlayer>();
            lang.RegisterMessages(Messages, this);
        }
        void OnServerInitialized()
        {
            ins = this;
            LoadVariables();
            lockTypes = configData.LockOnTypes;
            initialized = true;
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerInit(player);
        }
        void OnPlayerInit(BasePlayer player)
        {
            player.gameObject.AddComponent<WeaponMonitor>();
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            if (rocketeers.ContainsKey(player.userID))
            {
                UnityEngine.Object.DestroyImmediate(player.GetComponent<LockOnPlayer>());
                rocketeers.Remove(player.userID);
            }
            UnityEngine.Object.Destroy(player.GetComponent<WeaponMonitor>());
        }
        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (!initialized) return;
            if (item.info.itemid == 1594947829 && container.playerOwner != null)
            {
                SendReply(container.playerOwner, msg("inventory", container.playerOwner.UserIDString));
            }
        }
        void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            if (!player.GetComponent<LockOnPlayer>() || !player.GetComponent<LockOnPlayer>().HasTargetLocked()) return;
            if (entity.ShortPrefabName == "rocket_smoke")
            {
                var rocket = GameManager.server.CreateEntity(rocketPrefab, entity.transform.position, entity.transform.rotation);
                rocket.OwnerID = player.userID;
                rocket.creatorEntity = player;                
                rocket.Spawn();
                rocket.GetComponent<ServerProjectile>().InitializeVelocity(Vector3.forward);                
                entity.Kill();
                rocketeers[player.userID].RocketFired(rocket);
            }            
        }
        void OnPlayerInput(BasePlayer player, InputState input)
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
        void Unload()
        {
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
        void AddUI(BasePlayer player, string message)
        {
            var container = UI.CreateElementContainer(LockOnUI, "0 0 0 0", "0.55 0.45", "0.95 0.55");
            UI.CreateLabel(ref container, LockOnUI, "", message, 16, "0 0", "1 1", TextAnchor.MiddleLeft);
            CuiHelper.DestroyUi(player, LockOnUI);
            CuiHelper.AddUi(player, container);
        }          
        #endregion

        #region Components
        class WeaponMonitor : MonoBehaviour
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
                if (activeItem != null && activeItem.info.itemid == 649603450)
                {
                    BaseProjectile weapon = activeItem.GetHeldEntity() as BaseProjectile;
                    if (weapon != null)
                    {
                        if (weapon.primaryMagazine != null)
                        {
                            if (hasAmmo && weapon.primaryMagazine.contents == 0)
                                unloadedAmmo = true;
                            if (weapon.primaryMagazine.ammoType.itemid == 1594947829 && weapon.primaryMagazine.contents > 0)
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
        class LockOnPlayer : MonoBehaviour
        {
            private BasePlayer player;
            private RaycastHit rayHit;
            private BaseEntity target;

            private float lockOnSeconds;

            private bool targetLocked;
            private bool isBeeping;
            public bool isEnabled;
            public bool isHelicopter;

            private bool openUI;
            private bool isDrawing;

            void Awake()
            {                
                player = GetComponent<BasePlayer>();
                target = null;
                isEnabled = false;
            }
            void OnDestroy()
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
                isHelicopter = false;
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
                    homing.SetPlayer(player, target, isHelicopter);
                    ClearTarget();
                }
                DestroyImmediate(this);
            }
            public bool HasTargetLocked() => targetLocked;
            void LockTarget()
            {
                if (Physics.SphereCast(new Ray(player.eyes.transform.position + (Vector3.up * 2), (player.eyes.rotation * player.eyes.headRotation) * Vector3.forward), 1f, out rayHit))
                {
                    var newTarget = rayHit.GetEntity();
                    if (newTarget != null)
                    {
                        switch (newTarget.GetType().ToString())
                        {
                            case "CargoPlane":
                                if (!lockTypes[LockTypes.Plane])
                                    return;
                                break;
                            case "BasePlayer":
                                if (!lockTypes[LockTypes.Player])
                                    return;
                                break;
                            case "BaseNPC":
                                if (!lockTypes[LockTypes.Animal])
                                    return;
                                break;
                            case "BaseHelicopter":
                            case "PatrolHelicopterAI":
                                if (!lockTypes[LockTypes.Helicopter])
                                    return;
                                isHelicopter = true;
                                break;
                            case "LootContainer":
                            case "StorageContainer":
                                if (!lockTypes[LockTypes.Loot])
                                    return;
                                break;
                            case "BaseResource":
                            case "TreeEntity":
                                if (!lockTypes[LockTypes.Resource])
                                    return;
                                break;
                            case "BuildingBlock":
                            case "SimpleBuildingBlock":
                                if (!lockTypes[LockTypes.Structure])
                                    return;
                                break;
                            default:
                                return;
                        }
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
                            player.SendConsoleCommand("ddraw.box", 0.2f, (targetLocked ? Color.green : Color.red), newTarget.transform.position + new Vector3(0, 0.25f, 0), 0.5f);
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
            void Beep()
            {
                isBeeping = true;
                Effect.server.Run(lockBeep, player.transform.position + player.transform.forward);
                Invoke("Beep", targetLocked ? 0.25f : 1f);
            }
            void StopDrawing() => isDrawing = false;
        }
        class HomingRocket : MonoBehaviour
        {
            private BaseEntity target;
            private BasePlayer player;
            private ServerProjectile rocket;
            private TimedExplosive explosive;

            private float totalDistance;
            private float fraction;

            private bool isHelicopter;
            
            void Awake()
            {
                rocket = GetComponent<ServerProjectile>();
                explosive = GetComponent<TimedExplosive>();

                rocket.gravityModifier = 0;
                rocket.speed = isHelicopter ? ins.configData.RocketSpeed * ins.configData.HelicopterLockModifiers.SpeedModifier : ins.configData.RocketSpeed;
                                
                explosive.damageTypes = new List<Rust.DamageTypeEntry> { new Rust.DamageTypeEntry { amount = ins.configData.RocketDamage, type = Rust.DamageType.Explosion }, new Rust.DamageTypeEntry { amount = 75, type = Rust.DamageType.Blunt } };
                explosive.explosionRadius = 3.8f;
                explosive.minExplosionRadius = 1;
                
                
                explosive.SetFuse(ins.configData.DetonationTime);

                if (!ins.configData.DisableSmokeEffects)
                    Effect.server.Run(smokePrefab, rocket.GetComponent<BaseEntity>(), 0, new Vector3(), new Vector3(), null, false);
            }
            void FixedUpdate()
            {               
                if (target == null) return;

                Vector3 direction = target.transform.position + new Vector3(0, 0.25f, 0) - rocket.transform.position;
                rocket.InitializeVelocity(direction);

                var remaining = totalDistance - Vector3.Distance(rocket.transform.position, target.transform.position);
                if (remaining > 0 && totalDistance > 0)
                    fraction = remaining / totalDistance;
            }
            void OnDestroy()
            {
                if (isHelicopter && Vector3.Distance(target.transform.position, rocket.transform.position) < 5)
                {
                    var helicopter = target as BaseHelicopter;
                    if (helicopter != null)
                    {
                        helicopter.Hurt(ins.configData.RocketDamage * ins.configData.HelicopterLockModifiers.DamageModifier, Rust.DamageType.Explosion, player);
                    }
                }
                Effect.server.Run(c4Explosion, rocket.transform.position);
                CancelInvoke();
            }
            public void SetPlayer(BasePlayer player, BaseEntity target, bool isHelicopter)
            {
                this.player = player;
                this.target = target;
                this.isHelicopter = isHelicopter;
                totalDistance = Vector3.Distance(player.transform.position, target.transform.position);
                fraction = 0;
                if (!ins.configData.DisableRocketBeep)
                    Beep();
            }
            void Beep()
            {
                Effect.server.Run(lockBeep, rocket.transform.position + Vector3.up);
                Invoke("Beep", 1f - fraction);
            }
        }
        #endregion
               
        #region Config 
        enum LockTypes { Helicopter, Plane, Player, Animal, Structure, Resource, Loot }       
        private ConfigData configData;
        class HelicopterMods
        {
            public float DamageModifier { get; set; }
            public float SpeedModifier { get; set; }
        }
        class ConfigData
        {
            public float TimeToLockOn { get; set; }
            public bool DisableSmokeEffects { get; set; }
            public bool DisableRocketBeep { get; set; }
            public float DetonationTime { get; set; }
            public float RocketSpeed { get; set; }
            public float RocketDamage { get; set; }
            public HelicopterMods HelicopterLockModifiers { get; set; } 
            public Dictionary<LockTypes, bool> LockOnTypes { get; set; }          
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {               
                DetonationTime = 30f,
                RocketSpeed = 40,
                DisableRocketBeep = false,
                DisableSmokeEffects = false,
                TimeToLockOn = 3,
                RocketDamage = 300,
                HelicopterLockModifiers = new HelicopterMods
                {
                    DamageModifier = 5.0f,
                    SpeedModifier = 2.5f
                },
                LockOnTypes = new Dictionary<LockTypes, bool>
                {
                    {LockTypes.Animal, true },
                    {LockTypes.Plane, true },
                    {LockTypes.Player, true },
                    {LockTypes.Helicopter, true },
                    {LockTypes.Structure, true },
                    {LockTypes.Resource, true },
                    {LockTypes.Loot, true }
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Localization
        static string msg(string key, string playerId = null) => ins.lang.GetMessage(key, ins, playerId);
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"locked", ">><color=#00E500> Цель  </color><<" },
            {"aquiring", ">><color=#E50000> Aquiring Target </color><<"},
            {"unloaded", "<color=#939393>You have unloaded your </color><color=#C4FF00>lock-on rocket</color><color=#939393> from your launcher</color>"},
            {"loaded", "<color=#939393>A </color><color=#C4FF00>lock-on rocket</color><color=#939393> is loaded in your rocket launcher!</color>"},
            {"inventory", "<color=#939393>You have a </color><color=#C4FF00>lock-on rocket</color><color=#939393> in your inventory! To use it load the smoke rocket into your rocket launcher</color>"}
        };
        #endregion
    }
}
