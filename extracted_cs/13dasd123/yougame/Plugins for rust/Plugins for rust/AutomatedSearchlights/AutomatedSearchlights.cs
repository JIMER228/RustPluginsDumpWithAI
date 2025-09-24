// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System.Collections.Generic;
using Facepunch;
using Rust;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Reflection;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("AutomatedSearchlights", "k1lly0u", "0.1.6", ResourceId = 0)]
    class AutomatedSearchlights : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin Friends;
        [PluginReference] Plugin Clans;

        static AutomatedSearchlights ins;

        StoredData storedData;
        private DynamicConfigFile data;

        private Dictionary<ulong, List<uint>> lightCache = new Dictionary<ulong, List<uint>>();
        private List<Follower> searchLights = new List<Follower>();
        private bool wipeData;
        private bool isNight;

        private static LayerMask layerMask;
        private static FieldInfo secondsRemaining = typeof(SearchLight).GetField("secondsRemaining", (BindingFlags.Instance | BindingFlags.NonPublic));
        private static FieldInfo direction = typeof(SearchLight).GetField("aimDir", (BindingFlags.Instance | BindingFlags.NonPublic));

        const string perm = "automatedsearchlights.use";
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            lang.RegisterMessages(Messages, this);
            permission.RegisterPermission("automatedsearchlights.use", this);
            data = Interface.Oxide.DataFileSystem.GetFile("searchlight_data");
            ins = this;
        }
        void OnServerInitialized()
        {
            layerMask = (1 << 29);
            layerMask |= (1 << 18);
            layerMask = ~layerMask;

            LoadVariables();
            LoadData();

            if (wipeData)
            {
                PrintWarning("Map wipe detected! Clearing saved autolight data");
                lightCache.Clear();
                SaveData();
            }

            FindAllLights();
        }
        void OnEntityKill(BaseNetworkable networkable)
        {
            var searchLight = networkable.GetComponent<SearchLight>();
            if (searchLight == null) return;

            if (searchLight.GetComponent<Follower>())
            {
                foreach(var list in lightCache)
                {
                    if (list.Value.Contains(searchLight.net.ID))
                    {
                        list.Value.Remove(searchLight.net.ID);
                        UnityEngine.Object.Destroy(searchLight.GetComponent<Follower>());
                        return;
                    }
                }
            }
        }
        void OnNewSave(string filename) => wipeData = true;
        void Unload()
        {
            SaveData();
            var objects = UnityEngine.Object.FindObjectsOfType<Follower>();
            if (objects != null)
            {
                foreach (var obj in objects)
                    UnityEngine.Object.Destroy(obj);
            }            
        }
        void OnServerShutdown()
        {
            SaveData();
            var objects = UnityEngine.Object.FindObjectsOfType<Follower>();
            if (objects != null)
            {
                foreach (var obj in objects)
                    UnityEngine.Object.Destroy(obj);
            }            
        }
        void OnServerSave() => SaveData();
        #endregion

        #region Component
        class Follower : MonoBehaviour
        {
            private SearchLight entity;
            private SphereCollider collider;
            private ConfigData data;

            private List<BaseCombatEntity> inRange = new List<BaseCombatEntity>();
            private BaseCombatEntity target;
            
            private bool resetSearch = true;
            private float nextIdleAimTime;
            private float secondsTaken;

            private Vector3 centerPoint;
            private Vector3 rightPoint;
            private Vector3 leftPoint;

            private Aim lastAim = Aim.Center;

            private Vector3 currentAim = Vector3.zero;
            private Vector3 nextAim;

            private float flickerHealth;

            private void Awake()
            {
                entity = GetComponent<SearchLight>();
                data = ins.configData;

                enabled = false;

                if (!data.DetectionOptions.Animals)
                {
                    collider = entity.gameObject.AddComponent<SphereCollider>();
                    collider.gameObject.layer = (int)Layer.Reserved1;
                    collider.radius = data.DetectionOptions.Radius;
                    collider.isTrigger = true;
                }
                else InvokeRepeating("CheckEntites", 0.1f, 3f);

                if (!data.OtherOptions.ConsumeFuel)                                    
                    secondsRemaining.SetValue(entity, 20);

                if (data.OtherOptions.Flicker)
                    flickerHealth = entity.MaxHealth() * data.OtherOptions.FlickerHealth;

                SetAimPoints();
            }
            private void FixedUpdate()
            {                
                if (IsInUse()) return;
                if (!HasFuel()) return;
                if (target == null && inRange.Count > 0)                
                    FindValidTarget();

                if (!data.OtherOptions.Flicker)
                {
                    if (!entity.IsOn())
                        entity.SetFlag(BaseEntity.Flags.On, true);
                }
                else
                {
                    if (entity.health <= flickerHealth)
                    {
                        if (entity.health <= flickerHealth / 2)
                            entity.SetFlag(BaseEntity.Flags.On, UnityEngine.Random.Range(1, 5) != 2);
                        else entity.SetFlag(BaseEntity.Flags.On, UnityEngine.Random.Range(1, 10) != 2);
                    }
                }

                if (target == null)
                {
                    if (resetSearch || UnityEngine.Time.realtimeSinceStartup > nextIdleAimTime)
                    {
                        resetSearch = false;
                        nextIdleAimTime = UnityEngine.Time.realtimeSinceStartup + data.OtherOptions.SearchSpeed;

                        var currentRot = Quaternion.LookRotation((Vector3)direction.GetValue(entity));

                        RaycastHit rayHit;
                        if (Physics.Raycast(new Ray(entity.transform.position + (Vector3.up * 2), currentRot * Vector3.forward), out rayHit, 1000f, layerMask))
                            currentAim = rayHit.point;

                        nextAim = GetNext();

                        secondsTaken = 0;
                    }
                    secondsTaken = secondsTaken + UnityEngine.Time.deltaTime;
                    var single = Mathf.InverseLerp(0f, data.OtherOptions.SearchSpeed, secondsTaken);
                    entity.SetTargetAimpoint(Vector3.Lerp(currentAim, nextAim, single));

                    entity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                    return;                  
                }
                else
                {
                    if (CastRay(target.transform.position) == null)
                    {
                        ClearTarget();
                        return;
                    }
                    entity.SetTargetAimpoint(target.transform.position);
                    entity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                }                
            }
            private void OnTriggerEnter(Collider col)
            {
                var player = col.GetComponentInParent<BasePlayer>();
                if (player != null)
                {
                    if (inRange.Contains(player)) return;

                    if (!data.DetectionOptions.Friends && (entity.OwnerID == player.userID || ins.IsClanmate(entity.OwnerID, player.userID) || ins.IsFriend(entity.OwnerID, player.userID))) return;
                    inRange.Add(player);
                }
                var helicopter = col.GetComponentInParent<BaseHelicopter>();
                if (helicopter != null)
                {
                    if (data.DetectionOptions.Helicopters || inRange.Contains(helicopter)) return;
                    inRange.Add(helicopter);
                }
            }
           
            private void OnTriggerExit(Collider col)
            {
                var combatEnt = col.GetComponentInParent<BaseCombatEntity>();
                if (combatEnt == null)
                    return;
                if (inRange.Contains(combatEnt))
                    inRange.Remove(combatEnt);
                if (target == combatEnt)
                    ClearTarget();
            }
            private void OnDestroy()
            {
                if (data.DetectionOptions.Animals)
                    InvokeHandler.CancelInvoke(this, CheckEntites);
                if (entity.IsOn())
                    entity.SetFlag(BaseEntity.Flags.On, false);
                Destroy(collider);                
            }
            private void CheckEntites()
            {                
                inRange.Clear();
                List<BaseCombatEntity> entities = Pool.GetList<BaseCombatEntity>();
                Vis.Entities<BaseCombatEntity>(entity.transform.position, data.DetectionOptions.Radius, entities);
                foreach (var ent in entities)
                {
                    if (ent.IsDead() || ent.IsDestroyed) continue;
                    if (ent is BasePlayer)
                    {
                        var player = ent as BasePlayer;
                        if (player != null)
                        {
                            if (!data.DetectionOptions.Friends && (entity.OwnerID == player.userID || ins.IsClanmate(entity.OwnerID, player.userID) || ins.IsFriend(entity.OwnerID, player.userID))) return;
                            inRange.Add(player);
                        }
                    }
                    if (ent is BaseNpc)
                    {
                        var animal = ent as BaseNpc;
                        if (animal != null && data.DetectionOptions.Animals)
                        {
                            inRange.Add(animal);
                        }
                    }
                    if (ent is BaseHelicopter)
                    {
                        var helicopter = ent as BaseHelicopter;
                        if (helicopter != null && data.DetectionOptions.Helicopters)
                        {
                            inRange.Add(helicopter);
                        }
                    }
                }
                Pool.FreeList(ref entities);

                if (target != null && !inRange.Contains(target))
                    target = null;
            }
            public void SetAimPoints()
            {
                centerPoint = entity.transform.position + ((entity.transform.forward * 75) + (-entity.transform.up * 50));
                rightPoint = entity.transform.position + ((entity.transform.forward * 100) + (-entity.transform.up * 50) + (entity.transform.right * 100));
                leftPoint = entity.transform.position + ((entity.transform.forward * 100) + (-entity.transform.up * 50) + (-entity.transform.right * 100));
            }
            private void FindValidTarget()
            {               
                for (int i = 0; i < inRange.Count; i++)
                {
                    var combatEnt = inRange[i];  
                    if (combatEnt == null || combatEnt.IsDead() || combatEnt.IsDestroyed)
                    {
                        inRange.RemoveAt(i);
                        continue;
                    }
                   
                    BaseCombatEntity hitEntity = CastRay(combatEnt.transform.position);
                    if (hitEntity != null)
                    {   
                        target = hitEntity;
                        return;
                    }
                }
                target = null;
            }          
            private void ClearTarget()
            {
                target = null;
                if (data.OtherOptions.InactiveSearch)
                {
                    resetSearch = true;
                }
                else
                {
                    if (entity.IsOn())
                        entity.SetFlag(BaseEntity.Flags.On, false);
                }
            }
            private bool HasFuel()
            {
                if (data.OtherOptions.ConsumeFuel)
                {
                    Item slot = entity.inventory.GetSlot(0);
                    if (slot == null || slot.info != entity.fuelType)
                        return false;
                }
                else
                {
                    float remaining = (float)secondsRemaining.GetValue(entity);
                    secondsRemaining.SetValue(entity, remaining + Time.deltaTime);
                }
                return true;
            }
            private bool IsInUse()
            {
                if (entity.IsMounted() || entity.HasFlag(BaseEntity.Flags.Reserved5))
                {
                    if (target != null)
                        ClearTarget();
                    resetSearch = true;
                    return true;
                }
                return false;
            }
           
            private BaseCombatEntity CastRay(Vector3 targetPos)
            {
                RaycastHit rayHit;
                if (Physics.SphereCast(new Ray(entity.transform.position + (Vector3.up * 2), targetPos - entity.transform.position), 1f, out rayHit, 500, layerMask))
                {
                    var hitPlayer = rayHit.collider?.GetComponentInParent<BaseCombatEntity>();
                    if (hitPlayer != null)
                    {
                        return hitPlayer;                        
                    }
                }
                return null;
            }
            public void Toggle(bool active)
            {
                if (!active)
                {
                    if (data.DetectionOptions.Animals && IsInvoking("CheckEntities"))
                        CancelInvoke("CheckEntites");

                    this.enabled = false;
                    target = null;

                    if (entity != null && entity.IsOn())
                        entity.SetFlag(BaseEntity.Flags.On, false);             
                }
                else
                {
                    this.enabled = true;
                    target = null;

                    if (entity != null && !entity.IsOn())
                        entity.SetFlag(BaseEntity.Flags.On, true);

                    if (data.DetectionOptions.Animals && !IsInvoking("CheckEntities"))
                        InvokeRepeating("CheckEntites", 0.1f, 3f);
                }
            }
            enum Aim { Left, Center, Right };
            private Vector3 GetNext()
            {
                switch (lastAim)
                {
                    case Aim.Left:
                        lastAim = Aim.Right;
                        return rightPoint;
                    case Aim.Center:
                        lastAim = Aim.Right;
                        return rightPoint;
                    case Aim.Right:
                        lastAim = Aim.Left;
                        return leftPoint;
                    default:
                        return centerPoint;
                }
            }
        }
        #endregion

        #region Functions
        void FindAllLights()
        {
            var lightList = new List<uint>();
            foreach (var lightSet in lightCache)
                lightList.AddRange(lightSet.Value);

            var lights = UnityEngine.Object.FindObjectsOfType<SearchLight>().Distinct();
            if (lights != null)
            {
                foreach(var light in lights)
                {
                    if (lightList.Contains(light.net.ID))
                    {
                        if (light.GetComponent<Follower>())
                            UnityEngine.Object.DestroyImmediate(light.GetComponent<Follower>());
                        var sLight = light.gameObject.AddComponent<Follower>();
                        searchLights.Add(sLight);
                    }
                }
            }
                      
            MonitorTime();
        }
        SearchLight FindLightFromRay(BasePlayer player)
        {
            Ray ray = new Ray(player.eyes.position, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 20))
                return null;

            var hitEnt = hit.collider.GetComponentInParent<SearchLight>();
            if (hitEnt != null) 
                return hitEnt;            
            return null;
        }
        void MonitorTime()
        {
            if (!configData.OtherOptions.NightActive)
            {
                isNight = true;
                return;
            }
            var time = TOD_Sky.Instance.Cycle.Hour;
            var nightCheck = time >= configData.OtherOptions.NightStart || (time > 0 && time < configData.OtherOptions.NightEnd);

            if (nightCheck != isNight)
            {
                isNight = nightCheck;
                foreach (var light in searchLights)
                {
                    if (light != null)
                        light.Toggle(isNight);
                }
            }
           
            timer.In(5, MonitorTime);
        }
        #endregion

        #region Hooks
        private bool IsClanmate(ulong playerId, ulong friendId)
        {
            if (!Clans) return false;
            object playerTag = Clans?.Call("GetClanOf", playerId);
            object friendTag = Clans?.Call("GetClanOf", friendId);
            if ((playerTag is string && !string.IsNullOrEmpty((string)playerTag)) && (friendTag is string && !string.IsNullOrEmpty((string)friendTag)))
                if (playerTag == friendTag) return true;
            return false;
        }
        private bool IsFriend(ulong playerID, ulong friendID)
        {
            if (!Friends) return false;
            bool isFriend = (bool)Friends?.Call("IsFriend", playerID, friendID);
            return isFriend;
        }
        #endregion

        #region Commands
        [ChatCommand("sl")]
        void cmdSL(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm)) return;
            if (args.Length == 0)
            {
                SendReply(player, $"<color=#ce422b>{Title}</color><color=#939393>  v{Version}  -</color> <color=#ce422b>{Author} @ www.chaoscode.io</color>");
                SendReply(player, msg("help1", player.UserIDString));
                SendReply(player, msg("help2", player.UserIDString));
                SendReply(player, msg("help3", player.UserIDString));
                SendReply(player, msg("help4", player.UserIDString));
                return;
            }
            SearchLight entity = FindLightFromRay(player);
            if (entity == null)
            {
                SendReply(player, msg("noLight", player.UserIDString));
                return;
            }
            switch (args[0].ToLower())
            {
                case "add":

                    if (entity.OwnerID != player.userID && !ins.IsClanmate(entity.OwnerID, player.userID) && !ins.IsFriend(entity.OwnerID, player.userID))
                    {
                        SendReply(player, msg("noOwner", player.UserIDString));
                        return;
                    }

                    if (!lightCache.ContainsKey(player.userID))
                        lightCache[player.userID] = new List<uint>();

                    if (lightCache[player.userID].Count >= configData.ManagementOptions.MaxSet)
                    {
                        SendReply(player, msg("maxLights", player.UserIDString));
                        return;
                    }                    
                    if (entity.GetComponent<Follower>())
                    {
                        SendReply(player, msg("alreadyLight", player.UserIDString));
                        return;
                    }

                    lightCache[player.userID].Add(entity.net.ID);
                    var light = entity.gameObject.AddComponent<Follower>();
                    searchLights.Add(light);
                    light.Toggle(isNight);
                    SendReply(player, msg("enabled", player.UserIDString));
                    SaveData();
                    return;
                case "remove":                    
                    if (entity.OwnerID != player.userID && !ins.IsClanmate(entity.OwnerID, player.userID) && !ins.IsFriend(entity.OwnerID, player.userID))
                    {
                        SendReply(player, msg("noOwner", player.UserIDString));
                        return;
                    }
                    if (!entity.GetComponent<Follower>())
                    {
                        SendReply(player, msg("notLight", player.UserIDString));
                        return;
                    }
                    if (!lightCache.ContainsKey(player.userID) || !lightCache[player.userID].Contains(entity.net.ID))
                    {
                        SendReply(player, msg("notAdded", player.UserIDString));
                        return;
                    }
                    UnityEngine.Object.Destroy(entity.GetComponent<Follower>());
                    lightCache[player.userID].Remove(entity.net.ID);                    
                    SendReply(player, msg("disabled", player.UserIDString));
                    SaveData();
                    return;
                case "rotate":
                    if (entity.OwnerID != player.userID && !ins.IsClanmate(entity.OwnerID, player.userID) && !ins.IsFriend(entity.OwnerID, player.userID))
                    {
                        SendReply(player, msg("noOwner", player.UserIDString));
                        return;
                    }
                    if (!entity.GetComponent<Follower>())
                    {
                        SendReply(player, msg("notLight", player.UserIDString));
                        return;
                    }
                    if (!lightCache.ContainsKey(player.userID) || !lightCache[player.userID].Contains(entity.net.ID))
                    {
                        SendReply(player, msg("notYours", player.UserIDString));
                        return;
                    }
                    var searchLight = entity.GetComponent<Follower>();
                    searchLight.Toggle(false);
                    searchLight.enabled = false;

                    var yRot = player?.eyes?.rotation.eulerAngles.y ?? 0;
                    entity.transform.eulerAngles = new Vector3(0, yRot, 0);

                    searchLight.SetAimPoints();
                    searchLight.Toggle(isNight);
                    SendReply(player, msg("rotSet", player.UserIDString));
                    return;
                default:
                    break;
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class Detection
        {
            [JsonProperty(PropertyName = "Search Radius")]
            public float Radius { get; set; }
            [JsonProperty(PropertyName = "Detect Animals")]
            public bool Animals { get; set; }
            [JsonProperty(PropertyName = "Detect Enemies")]
            public bool Enemies { get; set; }
            [JsonProperty(PropertyName = "Detect Friends")]
            public bool Friends { get; set; }
            [JsonProperty(PropertyName = "Detect Helicopters")]
            public bool Helicopters { get; set; }
        }
        class Management
        {
            [JsonProperty(PropertyName = "Maximum autolights per user")]
            public int MaxSet { get; set; }
            [JsonProperty(PropertyName = "Allow friends to set lights")]
            public bool AllowFriends { get; set; }
            [JsonProperty(PropertyName = "Allow clan mates to set lights")]
            public bool AllowClans { get; set; }
        }
        class Options
        {
            [JsonProperty(PropertyName = "Lights consume fuel")]
            public bool ConsumeFuel { get; set; }
            [JsonProperty(PropertyName = "Flicker - Flicker when damaged")]
            public bool Flicker { get; set; }
            [JsonProperty(PropertyName = "Flicker - Light health to cause flicker")]
            public float FlickerHealth { get; set; }
            [JsonProperty(PropertyName = "Search mode - Activate when nothing detected")]
            public bool InactiveSearch { get; set; }
            [JsonProperty(PropertyName = "Search mode - Rotation speed")]
            public float SearchSpeed { get; set; }
            [JsonProperty(PropertyName = "Automated at night time only")]
            public bool NightActive { get; set; }
            [JsonProperty(PropertyName = "Night start time")]
            public float NightStart { get; set; }
            [JsonProperty(PropertyName = "Night end time")]
            public float NightEnd { get; set; }
        }
        class ConfigData
        {
            public Detection DetectionOptions { get; set; }
            public Management ManagementOptions { get; set; }
            public Options OtherOptions { get; set; }            
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
                DetectionOptions = new Detection
                {
                    Animals = true,
                    Enemies = true,
                    Friends = false,
                    Helicopters = true,
                    Radius = 25f
                },
                ManagementOptions = new Management
                {
                    AllowClans = true,
                    AllowFriends = true,
                    MaxSet = 3
                },
                OtherOptions = new Options
                {
                    ConsumeFuel = false,
                    Flicker = true,
                    FlickerHealth = 0.5f,
                    InactiveSearch = true,
                    SearchSpeed = 15f,
                    NightActive = true,
                    NightEnd = 8f,
                    NightStart = 19f
                }               
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Data Management
        void SaveData()
        {
            storedData.data = lightCache;
            data.WriteObject(storedData);
        }
        void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
                lightCache = storedData.data;
            }
            catch
            {
                storedData = new StoredData();
            }
        }
        class StoredData
        {
            public Dictionary<ulong, List<uint>> data = new Dictionary<ulong, List<uint>>();
        }
        #endregion

        #region Localization
        string msg(string key, string playerId = "") => lang.GetMessage(key, this, playerId);
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"help1", "<color=white>Все команды должны вводится при просмотре на прожектор!</color>" },
            {"help2", "<color=#ffd479>/sl add</color><color=white> - Включает прожектор в автоматическое состояние</color>" },
            {"help3", "<color=#ffd479>/sl remove</color><color=white> - Удалить автоматизацию с прожектора</color>" },
            {"help4", "<color=#ffd479>/sl rotate</color><color=white> - Переход прожектора на ручное управление</color>" },
            {"noLight", "<color=white>Не удалось найти прожектор!</color>"},
            {"noOwner", "<color=white>Этот прожектор не принадлежит вам!</color>"},
            {"maxLights", "<color=white>У вас уже достигнуто максимальное количество автоматизированных прожекторов!</color>"},
            {"alreadyLight", "<color=white>Этот прожектор уже автоматизирован!</color>"},
            {"enabled", "<color=white>Вы </color><color=green>включили</color><color=white> автоматизацию на прожекторе!</color>"},
            {"notLight", "<color=white>Этот прожектор не автоматизирован!</color>"},
            {"notAdded", "<color=white>Вы можете удалить тольке те прожекторы, которые установлены вами!</color>"},
            {"disabled", "<color=white>Вы </color><color=red>выключили</color><color=white> автоматизацию на прожекторе!</color>"},
            {"notYours", "<color=white>Вы можете регулировать тольке те прожекторы, которые установлены вами!</color>"},
            {"rotSet", "<color=white>Вращение настроено!</color>" }
        };
        #endregion
    }
}
