using System.Collections.Generic;
using System.Reflection;
using Rust;
using Oxide.Core;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RotatingPickups", "k1lly0u", "0.2.2")]
    class RotatingPickups : RustPlugin
    {
        #region Fields  
        [PluginReference]
        Plugin Arena, ZoneManager;

        static RotatingPickups instance;
        static int[] layerTypes = new int[] { 4, 8, 16, 21, 23, 25, 26 };
        private bool initialized;

        private Dictionary<string, ZoneInfo> dropZones = new Dictionary<string, ZoneInfo>();
        private List<ulong> disabledPickup = new List<ulong>();

        private static readonly Collider[] colBuffer = (Collider[])typeof(Vis).GetField("colBuffer", (BindingFlags.Static | BindingFlags.NonPublic))?.GetValue(null);
        #endregion

        #region Components      
        class ItemRotator : MonoBehaviour
        {            
            private BaseEntity entity;
            private Rigidbody rigidBody;
            private bool hasBegun;
            private bool collisionPickup;
            private bool isEventItem;

            private float secsToTake;
            void Awake()
            {
                entity = GetComponent<BaseEntity>();
                enabled = false;
                collisionPickup = instance.configData.CollisionPickup;
            }   
            void OnDestroy()
            {
                CancelInvoke();
                if (entity == null) return;
                if (rigidBody != null)
                {
                    rigidBody.useGravity = true;
                    rigidBody.isKinematic = false;
                }
            }        
            void FixedUpdate()
            {
                entity.transform.RotateAround(entity.transform.position, Vector3.up, secsToTake);                
                entity.transform.hasChanged = true;
            }            
            void OnCollisionEnter(Collision collision)
            {
                if (hasBegun || collision.gameObject == null) return;
               
                if (layerTypes.Contains(collision.gameObject.layer) || collision.gameObject.name.Contains("junk_pile"))
                {
                    hasBegun = true;
                    Invoke("BeginRotation", instance.configData.RotateIn);
                }
            }
            void OnTriggerEnter(Collider col)
            {
                if (!collisionPickup) return;
                if (col?.gameObject?.layer != 17) return;
                var player = col.GetComponentInParent<BasePlayer>();
                if (player != null)
                {
                    if (!isEventItem && instance.disabledPickup.Contains(player.userID)) return;
                    if (instance.Arena && (bool)instance.Arena.Call("IsPlayerDead", player)) return;

                    if (entity is WorldItem)
                    {
                        Item item = (entity as WorldItem).item;
                        if (item != null)
                        {
                            if (player.inventory.containerBelt.itemList.Count < 6 || player.inventory.containerMain.itemList.Count < 24 || (player.inventory.containerWear.itemList.Count < 6 && item.info.category == ItemCategory.Attire))
                            {
                                player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
                                Destroy(this);
                            }
                        }
                    }
                    if (entity is DroppedItemContainer && instance.configData.CollisionPickupContainer)
                    {
                        instance.PickupContainer(player, entity as DroppedItemContainer);
                    }
                }
            } 
            private void BeginRotation()
            {
                gameObject.layer = (int)Layer.Reserved1;

                rigidBody = entity.GetComponent<Rigidbody>();
                rigidBody.useGravity = false;
                rigidBody.isKinematic = true;
                rigidBody.detectCollisions = true;
                rigidBody.collisionDetectionMode = CollisionDetectionMode.Discrete;

                var collider = gameObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = instance.configData.TriggerRadius;

                string shortname = entity is DroppedItemContainer ? entity.ShortPrefabName : (entity as WorldItem).item.info.shortname;
                secsToTake = instance.configData.Speed;

                entity.transform.transform.eulerAngles = new Vector3(instance.rotateList.ContainsKey(shortname) ? instance.rotateList[shortname] : 0, 0, 0);
                entity.transform.position = entity.transform.position + (shortname.Contains("spear") ? Vector3.up * 2 : Vector3.up);
                enabled = true;
                if (instance.configData.MonitorHeight)
                    InvokeRepeating("CheckHeight", 3f, 3f);
            }  
            private void CheckHeight()
            {
                RaycastHit hit;
                if (Physics.Raycast(new Ray(entity.transform.position, Vector3.down), out hit, 100f, LayerMask.GetMask("Terrain", "Construction", "Deployed", "Water", "Debris", "World")))
                {
                    var point = hit.point;
                    if (Vector3.Distance(point, entity.transform.position) > 1)
                    {
                        Vector3 adjustment = Vector3.up;
                        if (entity is WorldItem)
                        {
                            if ((entity as WorldItem).item.info.shortname.Contains("spear"))
                                adjustment = Vector3.up * 2;
                        }
                        entity.transform.position = point + adjustment;
                    }
                }
            }
            public void SetEventItem()
            {
                collisionPickup = true;
                isEventItem = true;
            }      
        }
        class ZoneInfo
        {
            public Vector3 Position;
            public float Radius;
            public bool IsBox;
            public Vector3 Size;
            public Vector3 Rotation;
        }
        #endregion

        #region Oxide Hooks   
        void Loaded()
        {
            permission.RegisterPermission("rotatingpickups.zones", this);
            lang.RegisterMessages(Messages, this);
        }    
        void OnServerInitialized()
        {
            initialized = true;
            instance = this;
            LoadVariables();

            if (configData.ZonesOnly)
                FindAllZones();
        }
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!initialized) return;
            WorldItem worldItem = entity.GetComponent<WorldItem>();
            if (worldItem != null)
            {                
                if (!IsInZone(worldItem)) return;
                Item item = worldItem.item;
                if (item != null)
                {
                    if ((configData.ActiveTypes.ContainsKey(item.info.category) && configData.ActiveTypes[item.info.category]) || configData.Overrides.Contains(item.info.shortname))
                    {
                        NextTick(() =>
                            {
                                if (worldItem != null)
                                    worldItem.gameObject.AddComponent<ItemRotator>();
                            });
                    }
                }
            }
            DroppedItemContainer itemContainer = entity.GetComponent<DroppedItemContainer>();
            if (itemContainer != null && configData.UseDroppedContainer)
            {
                if (!IsInZone(itemContainer)) return;
                NextTick(() =>
                {
                    if (itemContainer != null)
                        itemContainer.gameObject.AddComponent<ItemRotator>();
                });
            }
        }
        void Unload()
        {
            var objects = UnityEngine.Object.FindObjectsOfType<ItemRotator>();
            if (objects != null)
                foreach (var obj in objects)
                    UnityEngine.Object.Destroy(obj);
        }
        #endregion

        #region Functions
        void FindAllZones()
        {
            if (!ZoneManager) return;
            foreach(var zoneId in configData.Zones)            
                GetZoneInfo(zoneId);            
        }
        void GetZoneInfo(string zoneId)
        {
            object success = ZoneManager?.Call("GetZoneLocation", zoneId);
            if (success is Vector3)
            {
                Vector3 position = (Vector3)success;
                success = ZoneManager?.Call("GetZoneSize", zoneId);
                if (success is Vector3 && (Vector3)success != Vector3.zero)
                {
                    Vector3 size = (Vector3)success;
                    success = ZoneManager.Call("ZoneFieldList", zoneId);
                    if (success == null) return;
                    Dictionary<string, string> fieldList = (Dictionary<string, string>)success;
                    if (fieldList.ContainsKey("rotation"))
                    {
                        string rotString = fieldList["rotation"];
                        dropZones.Add(zoneId, new ZoneInfo { Position = position, IsBox = true, Size = size, Rotation = ParseV3String(rotString) });                        
                    }
                    return;
                }
                else success = ZoneManager?.Call("GetZoneRadius", zoneId);
                if (success is float && (float)success != 0)
                    dropZones.Add(zoneId, new ZoneInfo { Position = position, Radius = (float)success });                
            }
        }
        Vector3 ParseV3String(string source)
        {
            string outString = source.Substring(1, source.Length - 2);
            string[] splitString = outString.Split("," [0]);
            return new Vector3(float.Parse(splitString[0]), float.Parse(splitString[1]), float.Parse(splitString[2]));
        }
        bool IsInZone(BaseEntity entity)
        {
            if (!configData.ZonesOnly) return true;
            foreach(var zone in dropZones)
            {
                if (!zone.Value.IsBox)
                {
                    if (Vector3.Distance(zone.Value.Position, entity.transform.position) < zone.Value.Radius)
                        return true;
                }
                else
                {
                    int entities = 0;
                    if (zone.Value.Size != Vector3.zero)
                        entities = Physics.OverlapBoxNonAlloc(zone.Value.Position, new Vector3(zone.Value.Size.x /2, zone.Value.Size.y / 2, zone.Value.Size.z / 2), colBuffer, Quaternion.Euler(zone.Value.Rotation));
                    for (var i = 0; i < entities; i++)
                    {
                        var target = colBuffer[i].GetComponentInParent<BaseEntity>();
                        colBuffer[i] = null;
                        if (target != null)                        
                            if (target == entity) return true;   
                    }
                }
            }           
            return false;
        }
        void PickupContainer(BasePlayer player, DroppedItemContainer source)
        {
            ItemContainer inventory = source.inventory;
            if (inventory != null)
            {                
                while(inventory.itemList.Count > 0)
                {
                    Item item = inventory.itemList[0];
                    if (!item.MoveToContainer(player.inventory.containerMain))                   
                        item.Drop(source.transform.position, Vector3.up);  
                    else player.Command("note.inv", new object[] { item.info.itemid, item.amount });
                }               
                source.Kill(BaseNetworkable.DestroyMode.None);
            }
        }
        void AddItemRotator(WorldItem worldItem)
        {
            NextTick(() =>
            {
                if (worldItem != null && !worldItem.GetComponent<ItemRotator>())
                {
                    ItemRotator rotator = worldItem.gameObject.AddComponent<ItemRotator>();
                    rotator.SetEventItem();
                }
            });
        }
        #endregion

        #region Commands
        [ChatCommand("ap")]
        void cmdAutoPickup(BasePlayer player, string command, string[] args)
        {
            if (!configData.CollisionPickup) return;

            if (disabledPickup.Contains(player.userID))
            {
                disabledPickup.Remove(player.userID);
                SendReply(player, msg("apEnabled", player.UserIDString));
            }
            else
            {
                disabledPickup.Add(player.userID);
                SendReply(player, msg("apDisabled", player.UserIDString));
            }
        }

        [ChatCommand("dropzone")]
        void cmdDropZone(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, "rotatingpickups.zones")) return;
            if (!ZoneManager)
            {
                SendReply(player, "ZoneManager is not installed! Unable to set drop zones.");
                return;
            }
            if (args.Length == 1 && args[0].ToLower() == "list")
            {
                string zoneList = "--- Лист Ид зон ---";
                foreach (var zoneId in configData.Zones)
                    zoneList += $"\n{zoneId}";
                SendReply(player, zoneList);
                return;
            }
            if (args.Length != 2)
            {
                SendReply(player, $"{Title}  v.{Version} -- Registered Zones");
                SendReply(player, "/dropzone add <zoneid> - Add zone id to zone list");
                SendReply(player, "/dropzone remove <zoneid> - Remove zone id from zone list");
                SendReply(player, "/dropzone list - List of all registered zones");
                return;
            }
            switch (args[0].ToLower())
            {
                case "add":
                    object success = ZoneManager?.Call("CheckZoneID", args[1]);
                    if (success != null)
                    {
                        configData.Zones.Add(args[1]);
                        GetZoneInfo(args[1]);
                        SaveConfig(configData);
                        SendReply(player, "You have successfully added this zone to the zone list..");
                        return;
                    }
                    else SendReply(player, "This is wrong zone id");
                    return;
                case "remove":
                    if (configData.Zones.Contains(args[1]))
                    {
                        if (dropZones.ContainsKey(args[1]))
                            dropZones.Remove(args[1]);
                        configData.Zones.Remove(args[1]);
                        SaveConfig(configData);
                        SendReply(player, "You have successfully removed this zone from the zone list..");
                        return;
                        
                    }
                    else SendReply(player, "The specified zone id was not found in the zone list");
                    return;
                default:
                    break;
            }
        }
        [ConsoleCommand("dropzone")]
        void ccmdDropZone(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            if (!ZoneManager)
            {
                SendReply(arg, "ZoneManager is not installed! Unable to set drop zones.");
                return;
            }
            if (arg.Args.Length == 1 && arg.Args[0].ToLower() == "list")
            {
                string zoneList = "--- Zones ---";
                foreach (var zoneId in configData.Zones)
                    zoneList += $"\n{zoneId}";
                SendReply(arg, zoneList);
                return;
            }
            if (arg.Args.Length != 2)
            {
                SendReply(arg, $"{Title}  v.{Version} -- Registered Zones");
                SendReply(arg, "dropzone add <zoneid> - Add zone id to zone list");
                SendReply(arg, "dropzone remove <zoneid> - Remove zone id from zone list");
                SendReply(arg, "dropzone list - List of all registered zones");
                return;
            }
            switch (arg.Args[0].ToLower())
            {
                case "add":
                    object success = ZoneManager?.Call("CheckZoneID", arg.Args[1]);
                    if (success != null)
                    {
                        configData.Zones.Add(arg.Args[1]);
                        GetZoneInfo(arg.Args[1]);
                        SaveConfig(configData);
                        SendReply(arg, "You have successfully added this zone to the zone list.");
                        return;
                    }
                    else SendReply(arg, "This is wrong zone id");
                    return;
                case "remove":
                    if (configData.Zones.Contains(arg.Args[1]))
                    {
                        if (dropZones.ContainsKey(arg.Args[1]))
                            dropZones.Remove(arg.Args[1]);
                        configData.Zones.Remove(arg.Args[1]);
                        SaveConfig(configData);
                        SendReply(arg, "You have successfully removed this zone from the zone list.");
                        return;

                    }
                    else SendReply(arg, "The specified zone id was not found in the zone list");
                    return;
                default:
                    break;
            }
        }
        #endregion

        #region Config      
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Allow collision pickup of items")]
            public bool CollisionPickup { get; set; }
            [JsonProperty(PropertyName = "Allow collision pickup of dropped item containers")]
            public bool CollisionPickupContainer { get; set; }
            [JsonProperty(PropertyName = "Rotate dropped item containers")]
            public bool UseDroppedContainer { get; set; }
            [JsonProperty(PropertyName = "Apply rotation in set zones only")]
            public bool ZonesOnly { get; set; }
            [JsonProperty(PropertyName = "Monitor height changes")]
            public bool MonitorHeight { get; set; }
            [JsonProperty(PropertyName = "Rotation speed")]
            public float Speed { get; set; }
            [JsonProperty(PropertyName = "Collision detection radius")]
            public float TriggerRadius { get; set; }
            [JsonProperty(PropertyName = "Time before rotation starts (seconds)")]
            public int RotateIn { get; set; }
            [JsonProperty(PropertyName = "Item types to rotate")]
            public Dictionary<ItemCategory, bool> ActiveTypes { get; set; }
            [JsonProperty(PropertyName = "Overrides - Items to rotate (ignores types)")]
            public List<string> Overrides { get; set; }
            [JsonProperty(PropertyName = "Zone IDs used for zone only rotation")]
            public List<string> Zones { get; set; }
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
                CollisionPickup = true,
                CollisionPickupContainer = true,
                ZonesOnly = false,
                TriggerRadius = 0.5f,
                MonitorHeight = true,
                Speed = 5f,
                UseDroppedContainer = true,
                RotateIn = 1,
                ActiveTypes = new Dictionary<ItemCategory, bool>
                {
                    {ItemCategory.Ammunition, true },
                    {ItemCategory.Attire, true },
                    {ItemCategory.Component, false },
                    {ItemCategory.Construction, false },
                    {ItemCategory.Food, true },
                    {ItemCategory.Items, false },
                    {ItemCategory.Medical, true },
                    {ItemCategory.Misc, false },
                    {ItemCategory.Resources, true },
                    {ItemCategory.Tool, true },
                    {ItemCategory.Traps, false },
                    {ItemCategory.Weapon, true },
                    {ItemCategory.Common, false }
                },
                Overrides = new List<string>(),
                Zones = new List<string>()
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion
                
        #region Localization
        string msg(string key, string playerId = null) => lang.GetMessage(key, this, playerId);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["apDisabled"] = "Auto-pickup is disabled.",
            ["apEnabled"] = "Auto-pickup enabled."
        };
        #endregion

        Dictionary<string, int> rotateList = new Dictionary<string, int>
        {
            { "knife.bone", 270 },
            { "rifle.ak", 90 },
            { "smg.2", 270 },
            { "shotgun.double", 90 },
            { "spear.wooden", 270 },
            { "spear.stone", 270 },
            { "smg.thompson", 270 },
            { "shotgun.waterpipe", 90 },
            { "building.planner", 270 },
            { "map", 270 },
            { "mask.bandana", 270 },
            { "hat.beenie", 270 },
            { "coffeecan.helmet", 270 },
            { "hammer.salvaged", 270 },
            { "axe.salvaged", 270 },
        };
    }
}
