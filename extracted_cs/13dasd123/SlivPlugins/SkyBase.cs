// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections.Generic;
using System.Linq;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SkyBase", "CASHR#6906", "1.0.8")]
    internal class SkyBase : RustPlugin
    {
        #region Static
        private readonly List<BaseEntity> EntityToRemove = new List<BaseEntity>();
        [PluginReference] private Plugin ImageLibrary;
        private static SkyBase _;
        private const bool IsEn = true;
        private Configuration _config;
        private const string perm = "skybase.use";
        private List<DroneController> _droneControllers = new List<DroneController>();
        private Dictionary<BasePlayer, Dictionary<string, int>> ActiveCraftUI = new Dictionary<BasePlayer, Dictionary<string, int>>();
        
        private readonly List<Vector3> DronePosition = new List<Vector3>
        {
            new Vector3(0, -0.15f, 0),
            new Vector3(0.85f, 0f, 0.9f),
            new Vector3(-0.85f, 0f, -0.9f),
            new Vector3(-0.85f, 0f, 0.9f),
            new Vector3(0.85f, 0f, -0.9f),
            new Vector3(0f, -0.1f, 0.9f)
        };

        #endregion

        #region Config

        private class Configuration
        {


            [JsonProperty(PropertyName = IsEn ? "Number of drones on a square ceiling(Min 1: max 5)" : "Количество дронов на квадратном потолке(Min 1: max 5)",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public int DroneAmount = 5;
            
            [JsonProperty(PropertyName = IsEn ? "How many meters to lift the drone" : "На какую высоту поднимать дрон",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public readonly float PosToY = 100;

            [JsonProperty(PropertyName =IsEn ?  "Craft settings" : "Настройки крафта", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<CraftSettings> CraftList = new List<CraftSettings>()
            {
                new CraftSettings()
                {
                    displayName = IsEn ? "Twig Air Ceiling" :"Соломенный воздушный потолок",
                    gradeLevel = BuildingGrade.Enum.Twigs,
                    Prefab = "assets/prefabs/building core/floor/floor.prefab",
                    resultItem = new CraftSettings.ItemSettings()
                    {
                        Amount = 1,
                        Image = "https://i.imgur.com/rMRmZr6.png",
                        skinID = 3142103802,
                        ShortName = "drone"
                    } 
                },
                new CraftSettings()
                {
                    displayName = IsEn ? "Wood Air Ceiling" : "Деревянный воздушный потолок",
                    gradeLevel = BuildingGrade.Enum.Wood,
                    Prefab = "assets/prefabs/building core/floor/floor.prefab",
                    resultItem = new CraftSettings.ItemSettings()
                    {
                        Amount = 1,
                        Image = "https://i.imgur.com/GRo66E9.png",
                        skinID = 3142106108,
                        ShortName = "drone"
                    } 
                },
                new CraftSettings()
                {
                    displayName = IsEn ?"Stone Air Ceiling": "Каменный воздушный потолок",
                    gradeLevel = BuildingGrade.Enum.Stone,
                    Prefab = "assets/prefabs/building core/floor/floor.prefab",
                    resultItem = new CraftSettings.ItemSettings()
                    {
                        Amount = 1,
                        Image = "https://i.imgur.com/L1kacR1.png",
                        skinID = 3142106316,
                        ShortName = "drone"
                    } 
                },
                new CraftSettings()
                {
                    displayName = IsEn ?"Metal Air Ceiling": "Металлический воздушный потолок",
                    gradeLevel = BuildingGrade.Enum.Metal,
                    Prefab = "assets/prefabs/building core/floor/floor.prefab",
                    resultItem = new CraftSettings.ItemSettings()
                    {
                        Amount = 1,
                        Image = "https://i.imgur.com/0LgaHBt.png",
                        skinID = 3142106485,
                        ShortName = "drone"
                    } 
                },
                new CraftSettings()
                {
                    displayName = IsEn ?"TopTier Air Ceiling": "МВК воздушный потолок",
                    gradeLevel = BuildingGrade.Enum.TopTier,
                    Prefab = "assets/prefabs/building core/floor/floor.prefab",
                    resultItem = new CraftSettings.ItemSettings()
                    {
                        Amount = 1,
                        Image = "https://i.imgur.com/Vr4DORx.png",
                        skinID = 3136324504,
                        ShortName = "drone"
                    } 
                },
                 new CraftSettings()
                {
                    displayName = IsEn ?"Triangle Twig Air Ceiling": "Треугольный соломенный воздушный потолок",
                    gradeLevel = BuildingGrade.Enum.Twigs,
                    Prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab",
                    resultItem = new CraftSettings.ItemSettings()
                    {
                        Amount = 1,
                        Image = "https://i.imgur.com/DurDPfS.png",
                        skinID = 3142106672,
                        ShortName = "drone"
                    } 
                },
                new CraftSettings()
                {
                    displayName = IsEn ?"Triangle Wood Air Ceiling": "Треугольный деревянный воздушный потолок",
                    Prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab",
                    gradeLevel = BuildingGrade.Enum.Wood,
                    resultItem = new CraftSettings.ItemSettings()
                    {
                        Amount = 1,
                        Image = "https://i.imgur.com/3Hqz6qa.png",
                        skinID = 3142106876,
                        ShortName = "drone"
                    } 
                },
                new CraftSettings()
                {
                    Prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab",
                    displayName = IsEn ?"Triangle Stone Air Ceiling": "Треугольный каменный воздушный потолок",
                    gradeLevel = BuildingGrade.Enum.Stone,
                    resultItem = new CraftSettings.ItemSettings()
                    {
                        Amount = 1,
                        Image = "https://i.imgur.com/Z1pv8eV.png",
                        skinID = 3142107063,
                        ShortName = "drone"
                    } 
                },
                new CraftSettings()
                {
                    Prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab",
                    displayName = IsEn ?"Triangle Metal Air Ceiling": "Треугольный металл воздушный потолок",
                    gradeLevel = BuildingGrade.Enum.Metal,
                    resultItem = new CraftSettings.ItemSettings()
                    {
                        Amount = 1,
                        Image = "https://i.imgur.com/Iznzs0v.png",
                        skinID = 3142107226,
                        ShortName = "drone"
                    } 
                },
                new CraftSettings()
                {
                    Prefab = "assets/prefabs/building core/floor.triangle/floor.triangle.prefab",
                    displayName =IsEn ? "Triangle TopTier Air Ceiling" : "Треугольный МВК воздушный потолок",
                    gradeLevel = BuildingGrade.Enum.TopTier,
                    resultItem = new CraftSettings.ItemSettings()
                    {
                        Amount = 1,
                        Image = "https://i.imgur.com/2yRBwrG.png",
                        skinID = 3136324865,
                        ShortName = "drone"
                    } 
                },
            };


            internal class CraftSettings
            {
                [JsonProperty(PropertyName = IsEn ? "Craft name" : "Имя крафта", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public string displayName = "Air Ceiling";
                [JsonProperty(PropertyName = IsEn ? "Prefab to spawn" :"Префаб, который заспавнит дрон", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public string Prefab = "";
                [JsonProperty(PropertyName = IsEn ? "Grade level" :"Уровень улучшения префаба", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public BuildingGrade.Enum gradeLevel = BuildingGrade.Enum.Twigs;
                [JsonProperty(PropertyName = IsEn ? "The item you receive when crafting":"Предмет, который получится при крафте", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public ItemSettings resultItem = new ItemSettings()
                {
                    Image = "https://i.imgur.com/iWcfuu0.png",
                    ShortName = "drone",
                    Amount = 1,
                    skinID = 123321,
                };
                
                [JsonProperty(PropertyName = IsEn ? "List of resources required for crafting":"Список предметов для крафта", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<ItemSettings> ItemList = new List<ItemSettings>()
                {
                    new ItemSettings()
                    {
                        ShortName = "scrap",
                        Amount = 10,
                    },
                    new ItemSettings()
                    {
                        ShortName = "wood",
                        Amount = 1000,
                    },
                };

                internal class ItemSettings
                {
                
                    
                    [JsonProperty(PropertyName = "Shortname", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public string ShortName;

                    [JsonProperty(PropertyName = "Amount", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public int Amount = 1;

                    [JsonProperty(PropertyName = "SkinID", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public ulong skinID = 0;

                    [JsonProperty(PropertyName = IsEn ? "Custom image(If standard, leave it empty.)" :"Свое изображение, если стандартный - оставить пустым", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public string Image = "";
                }

            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
        }

        #endregion

        #region OxideHooks

     
        private void OnEntityKill(StabilityEntity entity)
        {
            if (entity == null || !entity.ShortPrefabName.Contains("floor")) return;
            _data.BuildListIDs.Remove(entity.net.ID.Value);
        }

        private void OnServerInitialized()
        {
            _ = this;
            _config.DroneAmount = _config.DroneAmount < 1 ? 1 : _config.DroneAmount > 5 ? 5 : _config.DroneAmount;
            SaveConfig();
            permission.RegisterPermission(perm, this);
            foreach (var check in BaseNetworkable.serverEntities.OfType<Drone>())
            {
                if (!IsNeedSkinID(check.skinID)) continue;
                if (check.IsValid())
                    check.Kill();
            }

            foreach (var check in BaseNetworkable.serverEntities.OfType<StabilityEntity>())
            {
                if (_data.BuildListIDs.Contains(check.net.ID.Value))
                {
                    check.grounded = true;
                    check.cachedStability = 100;
                    var drone = GameManager.server.CreateEntity("assets/prefabs/deployable/drone/drone.deployed.prefab",
                        check.transform.position) as Drone;
                    if (drone == null) continue;
                    var settings = GetSettings(check.PrefabName, check.GetComponent<BuildingBlock>().grade);
                    drone.Spawn();
                    drone.SetParent(check);
                    drone.transform.localPosition = DronePosition[5];
                    drone.transform.localRotation = Quaternion.identity;
                    drone.body.isKinematic = true;
                    drone.skinID = settings.resultItem.skinID;
                    drone.pickup.enabled = false;
                    drone.body.detectCollisions = false;
                    _.EntityToRemove.Add(drone);
                    if (check.ShortPrefabName.Contains("triangle")) continue;
                    drone.transform.localPosition = DronePosition[0];
                    for (var i = 1; i < _config.DroneAmount; i++)
                    {
                        var entity =
                            GameManager.server.CreateEntity("assets/prefabs/deployable/drone/drone.deployed.prefab",
                                drone.transform.position) as Drone;
                        entity.SetParent(drone);
                        entity.skinID = drone.skinID;
                        entity.transform.localPosition = DronePosition[i];
                        entity.body.detectCollisions = false;
                        entity.pickup.enabled = false;
                        entity.body.isKinematic = true;
                        entity.Spawn();
                    }
                }
            }

            foreach (var check in _config.CraftList)
            {
                AddImage(check.resultItem.Image);
                foreach (var item in check.ItemList)
                {
                    AddImage(item.Image);
                }
            }

            PrintError("|-----------------------------------|");
            PrintWarning($"|  Plugin {Title} v{Version} is loaded  |");
            PrintWarning("|          Discord: CASHR#6906      |");
            PrintError("|-----------------------------------|");
        }

        private object OnEntityTakeDamage(Drone entity, HitInfo info)
        {
            if (entity == null || info == null) return null;
            if (IsNeedSkinID(entity.skinID)) return false;
            return null;
        }
       /* private object CanPickupEntity(BasePlayer player, Drone entity)
        {
            if (IsNeedSkinID(entity.skinID)) return false;
            return null;
        }
*/
        private bool IsNeedSkinID(ulong skinID)
        {
            if (skinID == 0) return false;
            foreach (var check in _config.CraftList)
            {
                if (check.resultItem.skinID == skinID)
                    return true;
            }
            return false;
        }
        private static bool IsPositionOutsideMapGrid(Vector3 pos)
        {
            //for y/z, 0.0 is the bottom, like in GUIs

            var normalised = new Vector2(TerrainMeta.NormalizeX(pos.x), TerrainMeta.NormalizeZ(pos.z));

            if (normalised.x < 0F)
            {
                return true;
            }

            if (normalised.x > 1F)
            {
                return true;
            }

            if (normalised.y < 0F)
            {
                return true;
            }

            if (normalised.y > 1F)
            {
                return true;
            }

            return false;
        }
        private object CanRecycle( Recycler instance, Item slot )
        {
          if(IsNeedSkinID(slot.skin))return false;
          return null;
        }
        private void OnHammerHit(BasePlayer player, HitInfo info)
        {
            var entity = info?.HitEntity as StabilityEntity;
            if (entity == null || !entity.ShortPrefabName.Contains("floor")) return;
            if(_data.BuildListIDs.Contains(entity.net.ID.Value))return;
            var find = entity.GetBuilding()?.buildingBlocks.FirstOrDefault(p => p.cachedStability >= 1f);
            if(find != null && find.transform.position.y < entity.transform.position.y)return;
            if (IsPositionOutsideMapGrid(player.transform.position))
            {
                player.ChatMessage(GetMessage("NOBUILDONGRID", player.UserIDString));
                return;
            }
            RaycastHit hit;
            if (Physics.Raycast(entity.transform.position, Vector3.down, out hit, 20,
                    LayerMask.GetMask("Construction")))
                if (hit.GetEntity() != null)
                {
                    player.ChatMessage(GetMessage("NOUPGRADE", player.UserIDString));
                    return;
                }

            var settings = GetSettings(entity.PrefabName, entity.GetComponent<BuildingBlock>().grade);
            if (settings == null)
            {
                player.ChatMessage($"Error found settings to floor send info administrator:\n{entity.ShortPrefabName} {entity.GetComponent<BuildingBlock>().grade}");
                return;
            }
            var skinID = settings.resultItem.skinID;
            var amount = GetItemAmount(player, "drone", skinID);
            if (amount == 0)
            {
                player.ChatMessage(GetMessage("NOITEMS", player.UserIDString));
                return;
            }
            var drone = GameManager.server.CreateEntity("assets/prefabs/deployable/drone/drone.deployed.prefab",
                entity.transform.position) as Drone;
            drone.Spawn();
            _data.BuildListIDs.Add(entity.net.ID.Value);
            drone.skinID = skinID;
            drone.SetParent(entity);
            drone.transform.localPosition = DronePosition[5]; 
            drone.transform.localRotation = Quaternion.identity;
            drone.body.isKinematic = true;
            drone.body.detectCollisions = false;
            entity.grounded = true;
            entity.cachedStability = 1f;
            drone.pickup.enabled = false;
            Take(player.inventory.AllItems(), "drone", skinID, 1);
            entity.SendNetworkUpdate();
            _.EntityToRemove.Add(drone);
            if (entity.ShortPrefabName.Contains("triangle")) return;
            drone.transform.localPosition = DronePosition[0];
            for (var i = 1; i < _config.DroneAmount; i++)
            {
                var dopDrone =
                    GameManager.server.CreateEntity("assets/prefabs/deployable/drone/drone.deployed.prefab",
                        drone.transform.position) as Drone;
                dopDrone.SetParent(drone);
                dopDrone.skinID = drone.skinID;
                dopDrone.pickup.enabled = false;
                dopDrone.transform.localPosition = DronePosition[i];
                dopDrone.body.detectCollisions = false;
                dopDrone.body.isKinematic = true;
                dopDrone.Spawn();
            }
        }
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            var entity = go.ToBaseEntity() as Drone;
            if (entity == null || entity.skinID == 0) return;
            var player = plan.GetOwnerPlayer();
            if (player == null) return;
            var list = new List<BuildingBlock>();
            Vis.Entities(entity.transform.position, 5, list);
            if (!CheckForNearObjects(list, entity.transform.position, player, entity))
            {
                player.GiveItem(CreateDrone(1, entity.skinID));
                return;
            }

            list.Clear();
            Vis.Entities(entity.transform.position + new Vector3(0, _config.PosToY, 0), 10, list);
          //  var checkBlock = list.FirstOrDefault(p => p.cachedStability >= 1);
            if (!CheckForNearObjects(list, entity.transform.position + new Vector3(0, _config.PosToY, 0), player, entity))
            {
                player.GiveItem(CreateDrone(1, entity.skinID));
                return;
            }

            RaycastHit hit;
            if (Physics.Raycast(entity.transform.position, Vector3.up, out hit, _config.PosToY + 20,
                    LayerMask.GetMask("Construction")))
                if (hit.GetEntity() != null)
                {
                    NextTick(() => { entity.AdminKill(); });
                    player.ChatMessage(GetMessage("NOBUILD", player.UserIDString));
                    player.GiveItem(CreateDrone(1, entity.skinID));
                }

            entity.gameObject.AddComponent<DroneController>().SpawnDrone(entity.transform.position, entity.skinID, null);
        }

        private void Unload()
        {
            foreach (var check in _droneControllers)
            {
                UnityEngine.Object.Destroy(check);
            }

            _droneControllers = null;
            SaveData();
            foreach (var check in EntityToRemove)
                if (check.IsValid())
                    check.AdminKill();
            _ = null;
        }

        #endregion

        #region Function

        #region Data


        private  Data _data;

        private class Data
        {
            public List<ulong> BuildListIDs = new List<ulong>();
        }

        private void LoadData()
        {
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile($"MM_Data/{Name}/BuildList"))
                _data = new Data ();
            else
                _data = Interface.Oxide.DataFileSystem.ReadObject<Data>(
                    $"MM_Data/{Name}/BuildList");
            Interface.Oxide.DataFileSystem.WriteObject($"MM_Data/{Name}/BuildList", _data);

            if (_data == null)
                _data = new Data();
           
        }

        private void OnNewSave(string fileName)
        {
            LoadData();
            _data = new Data();
            SaveData();
        }
        private void Init()
        {
            LoadData();
        }

        private void OnServerSave()
        {
            SaveData();
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"MM_Data/{Name}/BuildList", _data);
        }
      

        #endregion
        private void DestroyAll<T>()
        {
            var objects = UnityEngine.Object.FindObjectsOfType(typeof(T));
            objects?.ToList().ForEach(UnityEngine.Object.Destroy);
        }

        private Configuration.CraftSettings GetSettings(string prefab, BuildingGrade.Enum grade)
        {
            foreach (var check in _config.CraftList)
            {
                if (check.gradeLevel == grade && check.Prefab == prefab)
                    return check;
            }

            return null;
        }
      
        private bool CheckForNearObjects(List<BuildingBlock> list, Vector3 position, BasePlayer player, Drone entity)
        {
            if (list.Any())
            {
                NextTick(entity.AdminKill);
                player.ChatMessage(GetMessage("NOBUILDACCESS", player.UserIDString));
                return false;
            }

            return true;
        }


        private class DroneController : FacepunchBehaviour
        {
            private Drone drone;
            private ulong SkinID = 0;
            private StabilityEntity floor;
            private BuildingBlock _buildingBlock;
            private bool _isDublicate;
            private bool _isMove;
            private bool _isTriangle;
            private Configuration.CraftSettings settings;
            private readonly List<Vector3> positionsToCreateDuplicate = new List<Vector3>
            {
                new Vector3(0.85f, 0f, 0.9f),
                new Vector3(-0.85f, 0f, -0.9f),
                new Vector3(-0.85f, 0f, 0.9f),
                new Vector3(0.85f, 0f, -0.9f)
            };

            private float PosToMove;
            private Vector3 StartPosition;

            private void Awake()
            {
                _._droneControllers.Add(this);
            }

            private void OnDestroy()
            {
                _._droneControllers.Remove(this);
            }

            private void Update()
            {
                if (!_isMove) return;
                if (PosToMove >= _._config.PosToY)
                {
                    _isMove = false;
                    drone.body.isKinematic = true;
                    drone.body.detectCollisions = false;
                    drone.transform.rotation = Quaternion.Euler(0, 0,0);
                    drone.pickup.enabled = false;
                 /*   if (_buildingBlock != null)
                    {
                      //  _buildingBlock.blockDefinition.HasMaleSockets()
                        foreach (var check in _buildingBlock.blockDefinition.allSockets)
                        {
                            var pos = _buildingBlock.transform.worldToLocalMatrix.MultiplyPoint3x4(check.transform.position);
                        }
                    }*/
                    floor = GameManager.server.CreateEntity(settings.Prefab, drone.transform.position + new Vector3(0, 0.1f, 0)) as StabilityEntity;
                    if (floor == null) return;
                    floor.grounded = true;
                    floor.OwnerID = drone.OwnerID;
                    floor.transform.rotation = Quaternion.Euler(drone.transform.rotation.eulerAngles);
                    floor.cachedStability = 1f; 
                
                    floor.AttachToBuilding(BuildingManager.server.NewBuildingID());
                    floor.Spawn();
                    _._data.BuildListIDs.Add(floor.net.ID.Value);

                    floor.GetComponent<BuildingBlock>()?.SetGrade(settings.gradeLevel);
                    floor.Heal(floor.MaxHealth());
                    drone.SetParent(floor);
                    drone.transform.localPosition = Vector3.zero - new Vector3(0, 0.15f, 0); 
                    if (_isTriangle)
                    {
                        drone.transform.localPosition = _.DronePosition[5]; 
                        drone.transform.localRotation = Quaternion.identity;
                    }
                    _.EntityToRemove.Add(drone);
                    drone.SetFlag(BaseEntity.Flags.On, false);
                    drone.SetFlag(BaseEntity.Flags.Reserved1, false);
                    drone.SetFlag(BaseEntity.Flags.Reserved2, false);
                    drone.SendNetworkUpdate();

                    foreach (var check in drone.GetComponentsInChildren<Drone>())  
                    {
                        check.SetFlag(BaseEntity.Flags.On, false);
                        check.SetFlag(BaseEntity.Flags.Reserved1, false);
                        check.SetFlag(BaseEntity.Flags.Reserved2, false);
                        check.SendNetworkUpdate();
                    }
                    Destroy(this);
                    return;
                }

                if (!_isTriangle &&_._config.PosToY / PosToMove < 2 && !_isDublicate)
                {
                    drone.transform.rotation = Quaternion.Euler(0, 0,0);
                    _isDublicate = true;
                    CreateDublicate();
                }

                PosToMove += Time.deltaTime * 5;
                drone.body.MovePosition(StartPosition + new Vector3(0, PosToMove, 0));
            }


            public void SpawnDrone(Vector3 pos, ulong skinID, BuildingBlock blockToParent)
            {
                SkinID = skinID;
                drone = GetComponent<Drone>();
                if (drone == null)
                {
                    if (!GetGround(ref pos))
                    {
                        Destroy(this);
                        return;
                    }

                    drone = GameManager.server.CreateEntity("assets/prefabs/deployable/drone/drone.deployed.prefab", pos) as Drone;
                    drone.Spawn();
                }

              //  _buildingBlock = blockToParent;
                drone.SetFlag(BaseEntity.Flags.On, true, true);
                drone.SetFlag(BaseEntity.Flags.Reserved1, true);
                drone.SetFlag(BaseEntity.Flags.Reserved2, true);
                StartPosition = pos;
                settings = _._config.CraftList.FirstOrDefault(p => p.resultItem.skinID == SkinID);
                if (settings == null)
                {
                    _.PrintError($"Error found settings to floor {SkinID}");
                    Destroy(this);
                    return;
                }
                _isTriangle = settings.Prefab.Contains("triangle");
                drone.SendNetworkUpdateImmediate(true);
                _isMove = true;
            }

            private bool GetGround(ref Vector3 pos)
            {
                var y = TerrainMeta.HeightMap.GetHeight(pos);
                RaycastHit hit;
                if (Physics.Raycast(pos, Vector3.down, out hit, Mathf.Infinity,
                        LayerMask.GetMask("Terrain", "World", "Default")))
                {
                    if (hit.collider.name.Contains("Ground") || hit.collider.name.Contains("Terrain"))
                        pos.y = Mathf.Max(hit.point.y, y);
                    return true;
                }

                return false;
            }

            private void CreateDublicate()
            {
                for (var i = 0; i < _._config.DroneAmount - 1; i++)
                {
                    var entity =
                        GameManager.server.CreateEntity("assets/prefabs/deployable/drone/drone.deployed.prefab",
                            drone.transform.position) as Drone;
                    entity.SetParent(drone);

                    //entity.OwnerID = drone.OwnerID;
                    entity.skinID = drone.skinID;
                    entity.pickup.enabled = false;
                    entity.SetFlag(BaseEntity.Flags.On, true, true);
                    entity.SetFlag(BaseEntity.Flags.Reserved1, true);
                    entity.SetFlag(BaseEntity.Flags.Reserved2, true);
                    entity.transform.localPosition = positionsToCreateDuplicate[i];
                    entity.body.isKinematic = true;
                    entity.body.detectCollisions = false;
                    entity.Spawn();
                }
            }
            
        }

        private void ShowMainUI(BasePlayer player, int page)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0.4528302 0.4289071 0.4289071 0.8196079", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"},
                RectTransform ={ AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-444 -554", OffsetMax = "-66.726 -33.03" }
            }, "Overlay", "Panel_7233");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 1" },
                RectTransform ={ AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-188.642 -39.699", OffsetMax = "188.638 -33.4" }
            }, "Panel_7233", "Panel_5492");

            container.Add(new CuiElement
            {
                Name = "Label_2356",
                Parent = "Panel_7233",
                Components =
                {
                    new CuiTextComponent { Text = GetMessage("UI_TITTLE", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },                   
                    new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-119.322 -33.405", OffsetMax = "119.318 0.485" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "0.6509434 0.2726593 0.2726593 1", Close = "Panel_7233"},
                Text = { Text = GetMessage("UI_CLOSE", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "119.321 -33.395", OffsetMax = "188.638 0" }
            }, "Panel_7233", "Button_8934");
            var posY = -130.343;
            var height = -46.7 - posY;
            foreach (var check in _config.CraftList.Skip(5* page).Take(5))
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1509434 0.141545 0.141545 0.7607843", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"},
                    RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = $"-183.2 {posY}", OffsetMax = $"182.171 {posY + height}" }
                }, "Panel_7233", "CraftBlock");
                posY -= height + 5;
                container.Add(new CuiElement
                {
                    Name = "Image_2613",
                    Parent = "CraftBlock",
                    Components =
                    {
                        new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage(check.resultItem.Image) },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-176.422 -35.181", OffsetMax = "-102.602 35.181" }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "Label_2848",
                    Parent = "CraftBlock",
                    Components =
                    {
                        new CuiTextComponent { Text = check.displayName, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-183.2 16.536", OffsetMax = "182.171 41.821" }
                    }
                });
                if (!ActiveCraftUI.ContainsKey(player))
                    ActiveCraftUI.Add(player, new Dictionary<string, int>());
                if (!ActiveCraftUI[player].ContainsKey(check.resultItem.Image))
                    ActiveCraftUI[player].Add(check.resultItem.Image, 1);


                var amountToCraft = ActiveCraftUI[player][check.resultItem.Image];
                var localItemPosX = -96.3;
                var localItemPosY = -14;
                var localWidth = -66.3 - localItemPosX;
                var localHeith = 16 - localItemPosY;
                var craftAllowed = true;
                foreach (var item in check.ItemList)
                {
                    var image = string.IsNullOrEmpty(item.Image) ? GetImage(item.ShortName) : GetImage(item.Image);
                    var needAmount = item.Amount * amountToCraft;
                    var inInventory = GetItemAmount(player, item.ShortName, item.skinID);
                    var colorText = "0 0 0 0";
                    if (inInventory < needAmount)
                    {
                        craftAllowed = false; 
                        colorText = "0.6509434 0.2726593 0.2726593 1";
                    }
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = colorText },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{localItemPosX} {localItemPosY}", OffsetMax = $"{localItemPosX + localWidth} {localItemPosY + localHeith}"  }
                    }, "CraftBlock", "Image_192");
                    container.Add(new CuiElement
                    {
                        Name = "Image_192s",
                        Parent = "Image_192",
                        Components =
                        {
                            new CuiRawImageComponent { Color = "1 1 1 1", Png = image },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                        }
                    });
                   
                    container.Add(new CuiElement
                    {
                        Name = "Label_661",
                        Parent = "Image_192",
                        Components =
                        {
                            new CuiTextComponent { Text = $"х{needAmount}", Font = "robotocondensed-bold.ttf", FontSize = 8, Align = TextAnchor.LowerRight, Color = "1 1 1 1" },
                            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-15 -15", OffsetMax = "15 0" }
                        }
                    });

                    localItemPosX += localWidth + 2;
                }

                var color = craftAllowed ? "0.372549 0.5450981 0.2392157 0.6980392" : "0.6509434 0.2726593 0.2726593 1";
                var cmd = craftAllowed ? $"UI_SKYBASE CRAFT {check.resultItem.Image} {amountToCraft} {page}" : "";
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = cmd},
                    Text = { Text = "CRAFT", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "104.587 -35.181", OffsetMax = "176.8 -15" }
                }, "CraftBlock", "Button_5522");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0 0 0 0.4039216" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-96.3 -35.181", OffsetMax = "104.59 -15" }
                }, "CraftBlock", "Panel_245");

                container.Add(new CuiElement
                {
                    Name = "Label_8213",
                    Parent = "Panel_245",
                    Components =
                    {
                        new CuiTextComponent { Text = GetMessage("UI_INPUT", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100.446 -10.091", OffsetMax = "43.174 10.09" }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "InputField_7650",
                    Parent = "Panel_245",
                    Components =
                    {
                        new CuiNeedsKeyboardComponent(),
                        new CuiInputFieldComponent { Text = $"{amountToCraft}", Command = $"UI_SKYBASE INPUT {check.resultItem.Image} {page}", Color = "1 1 1 1", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, CharsLimit = 0, IsPassword = false },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "40.373 -10.091", OffsetMax = "96.484 10.09" }
                    }
                });

            }

            if (page > 0)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = "0.1886792 0.1840512 0.1840512 1", Command = $"UI_SKYBASE PAGE {page - 1}" },
                    Text =
                    {
                        Text = "BACK", Font = "robotocondensed-bold.ttf", FontSize = 14,
                        Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.012", AnchorMax = "0.5 0.012", OffsetMin = "-82.071 0",
                        OffsetMax = "-3.001 24.984"
                    }
                }, "Panel_7233", "BACKPAGE");
            }

            if (page < 1)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = "0.1886792 0.1840512 0.1840512 1", Command = $"UI_SKYBASE PAGE {page + 1}" },
                    Text =
                    {
                        Text = "NEXT", Font = "robotocondensed-bold.ttf", FontSize = 14,
                        Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.012", AnchorMax = "0.5 0.012", OffsetMin = "3 0", OffsetMax = "82.07 24.984"
                    }
                }, "Panel_7233", "NextPage");
            }

            CuiHelper.DestroyUi(player, "Panel_7233");
            CuiHelper.AddUi(player, container);
        }

        [ChatCommand("craftdrone")]
        private void cmdChatcraftdrone(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm))
            {
                player.ChatMessage("You do not have the rights to use this command");
                return;
            }
            ShowMainUI(player, 0);
        }
        [ConsoleCommand("UI_SKYBASE")]
        private void cmdConsoleUI_SKYBASE(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            switch (arg.Args[0])
            {
                case "PAGE":
                {
                    ShowMainUI(player, int.Parse(arg.Args[1]));
                    break;
                }
                case "INPUT":
                {
                    if(arg.Args.Length < 3)return;
                    var amount = 1;
                    int.TryParse(arg.Args[3], out amount);
                    amount = amount <= 0 ? 1 : amount;
                    ActiveCraftUI[player][arg.Args[1]] = amount;
                    var page = int.Parse(arg.Args[2]);
                    ShowMainUI(player,page);
                    break;
                }
                case "CRAFT":
                {
                    var itemToCraft = _config.CraftList.FirstOrDefault(p => p.resultItem.Image.Contains(arg.Args[1]));
                    if (itemToCraft == null)
                    {
                        player.ChatMessage($"ERROR SKYBASE {arg.Args[1]}");
                        return;
                    }
                    var amount = int.Parse(arg.Args[2]);
                    var page = int.Parse(arg.Args[3]);
                    foreach (var check in itemToCraft.ItemList)
                    {
                        if (GetItemAmount(player, check.ShortName, check.skinID) < check.Amount  * amount) return;
                    }

                    foreach (var check in itemToCraft.ItemList)
                    {
                        Take(player.inventory.AllItems(), check.ShortName, check.skinID, check.Amount * amount);
                    }

                    var item = ItemManager.CreateByName(itemToCraft.resultItem.ShortName, itemToCraft.resultItem.Amount * amount, itemToCraft.resultItem.skinID);
                    item.name = itemToCraft.displayName;
                    player.GiveItem(item);
                    ShowMainUI(player,page);
                    break;
                }
            }
        }
       
        #endregion

        #region Helpers
        private void Take(IEnumerable<Item> itemList, string shortname, ulong skinId, int iAmount)
        {
            var num1 = 0;
            if (iAmount == 0) return;

            var list = Pool.GetList<Item>();

            foreach (var item in itemList)
            {
                if (item.info.shortname != shortname ||
                    skinId != 0 && item.skin != skinId) continue;

                var num2 = iAmount - num1;
                if (num2 <= 0) continue;
                if (item.amount > num2)
                {
                    item.MarkDirty();
                    item.amount -= num2;
                    num1 += num2;
                    break;
                }

                if (item.amount <= num2)
                {
                    num1 += item.amount;
                    list.Add(item);
                }

                if (num1 == iAmount)
                    break;
            }

            foreach (var obj in list)
                obj.RemoveFromContainer();

            Pool.FreeList(ref list);
        }  
        private Item CreateDrone(int amount = 1, ulong skinID = 0)
        {
           var item = ItemManager.CreateByName("drone", amount, skinID);
           item.name = "Air celling";
           return item;
        }
        private int GetItemAmount(BasePlayer player, string shortname, ulong skinID)
        {
            var amount = 0;
            for (var i = 0; i < player.inventory.AllItems().Count(); i++)
            {
                var item = player.inventory.AllItems()[i];
                if (item.info.shortname == shortname && item.skin == skinID)
                    amount += item.amount;
            }

            return amount;
        }
        private bool HasImage(string url) => ImageLibrary.Call<bool>("HasImage", url);
        private void AddImage(string url)=>ImageLibrary.Call<bool>("AddImage", url, url);
        private string GetImage(string url) => ImageLibrary.Call<string>("GetImage", url);

 
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string> 
            {
                ["NOUPGRADE"] = "Objects cannot be improved",
                ["NOITEMS"] = "There are not enough items to improve",
                ["NOBUILD"] = "Construction sites have been found above the don, the installation of a drone is not possible.",
                ["NOBUILDACCESS"] = "It is forbidden to put a drone near objects.",
                ["UI_TITTLE"] = "Air house crafting menu.",
                ["UI_CLOSE"] = "CLOSE",
                ["UI_INPUT"] = "Specify the quantity:",
                ["NOBUILDONGRID"] = "You can't build behind the map grid",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NOUPGRADE"] = "Нельзя улучшать объекты",
                ["NOITEMS"] = "Не хватает предметов для улучшения",
                ["NOBUILD"] = "Над дроном найдены строительные объекты, установка дрона не возможна.",
                ["NOBUILDACCESS"] = "Запрещено ставить дрона вблизи объектов.",
                ["UI_TITTLE"] = "Меню крафта воздушных домов.",
                ["UI_CLOSE"] = "ЗАКРЫТЬ",
                ["UI_INPUT"] = "Укажите количество:",
                ["NOBUILDONGRID"] = "Нельзя строиться за сеткой карты",
            }, this, "ru");
        }

        private string GetMessage(string langKey, string steamID) => lang.GetMessage(langKey, this, steamID);

        private string GetMessage(string langKey, string steamID, params object[] args)
        {
            return (args.Length == 0)
                ? GetMessage(langKey, steamID)
                : string.Format(GetMessage(langKey, steamID), args);
        }

        #endregion
    }
}                        }pkXGw	&k^S4fq̎V,7ZhɍUAs~[2v.f"m't)+<N&| KٽX8^#yl\qԷ?'D׏~EI;)6>˂WߴsDk<݄I?5\,k}PW
A:Ycy5&(0}6A*"Į-YņL4i&P`9^noZrQII@)I80%n:S7D_ ,Lj M~J&|dfDuE87oO.3JNLꦔ
wl1+k@K mZՔ.4Vv
a\tl5Vf|O)Oqĵ	[:%h+m|(Tkr%V_{B]LKy7w*쫋
osڲz6M%W{[>+]"PO;0'GUeD_npl}JCa|Z,
7+ kdOTa=/*Hw֧čE知|ch+9Hp#3-W|e9GUmvZQ0$0@JG	Q_MV" `y5ɗoRӚU>ǅ$$||QxO"ٰ_td>a/5qW~jx
. q# *FʏT8x;k]uwyde lY\mqbH<.Ѕd-X${l+
}krWB"vDje3ly-_wK	47N(->Gjo9ES3_Xބx* J9d@,P_T%aٿab@r K·mMN	O=ɩ$Sum|"WpwAmJ

#ʀc}w]u;$#rU(\^ǳ<Lr3W\zOJ/҃}7?NO3z=.k?'c-23BOGf
1$|~9i% ݡ:Os
QsncI7{ߒto^
tsM9%OBL3Rr'5f
u$R-@P4t-"ɖ4DKDJҷN8TLOzU3kI;
#siM}?^M&AwÀ9ti}M8@j۩Æ7uygAxK2/1 Dj	]p.LC	=|IRSGtp<XإT+Ocq1oPVIѽv0(CSj'a ޢ6ٚں6Dg@'(G1JHM*ZXx(X:mi(;Av09ی (d,cE҈<" "Vk~qS?(KH @$J=ktN$;"ӟ*'wZ qPoMFCwr ܸn:uw!yZ0W|ԜKkeU	q	?Yqv$J6899gi|?ՌXPIAR $pVvMŏ|yֲ LJ,XoIFiꑡ	Vg<k%0.0=gmF&19r7Y
mp#l}-KɊ$ޢY*k0X對x60c6;t{$>,}E܇-^4,(阭k"8'Ndf".4c>Q{!V%j8jVS3Ea"G|{Z|B+kȝ
*כ3?|Sl"P1nÑ]F9]NѶjU8>'+ ks`M*4S$5[`'>~2oMZXcT͞D8 vNGVe/Gfӿ^mWY-H1=
n&F`
4~S
$*8w:O*{'-d»c@Ul(
.9r[H-p!بc-h41]oȄ+Dڮpzg@4(I/fyk>	YC:#zB؄.\s:YC#,tp}$^\i|X
ʽS׸"ϐ)oo^~#h޲؞
d,56<k7aqQX[1Pt	c9v@0PyAqP4pХ:]1ܛ/܂껦Ғ9D3CΒC}8^{:VDeD|Yd]fs.1o**D~A2lbz	5^'Z"CKBO̒i+ыn)
{*W9(Xr3SB>ZVW+<jKzTґ-cT
%Yrl^S]*=RqfT +JV3œαJU[ ADvt81g!y4bAbqu?bĆ).s<x3*sA#;Ξ'jesk!#`:v:^vX\g&KE3m6HFn[o!r5#PA@zI6kDnbnQ;J\s*P̀'.PJގ)Yc_ГQ05|}!#;T6S^9^FnHl;	Z$GAfPnT(آďd,n7O.3m[+b;=}?W% have successfully received a backpack, the number of slots has been increased to : {0}",
                ["BACKPACK_REVOKE"] = "Your extra slots privilege expired, slots reduced to : {0}",
                ["BACKPACK_NULL"] = "You don't have a backpack available",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["BACKPACK_TITLE"] = "РЮКЗАК {0} СЛОТА(ОВ)",
                ["BACKPACK_IS_OPENED"] = "У вас уже открыт рюкзак!",
                ["BACKPACK_NO_INITIALIZE"] = "Плагин загружается, ожидайте, вскоре вы сможете открыть крафт!",
		   		 		  						  	   		  	  			  						  						  			 
                ["BACKPACK_GRANT"] = "Вы успешно получили рюкзак, количество слотов увеличено до : {0}",
                ["BACKPACK_REVOKE"] = "У вас истекла привилегия с дополнительными слотами, слоты уменьшились до : {0}",
                ["BACKPACK_NULL"] = "У вас нет доступного рюкзака",

            }, this, "ru");
        }
        public Dictionary<UInt64, BackpackInfo> _old_Backpacks = new Dictionary<UInt64, BackpackInfo>();
        static Item BuildWeapon(BackpackInfo.SavedItem sItem)
        {
            Item item = null;
            item = ItemManager.CreateByItemID(sItem.Itemid, 1, sItem.Skinid);
            item.position = sItem.TargetSlot;

            if (item.hasCondition)
            {
                item.condition = sItem.Condition;
                item.maxCondition = sItem.Maxcondition;
            }
		   		 		  						  	   		  	  			  						  						  			 
            if (sItem.Blueprint != 0)
                item.blueprintTarget = sItem.Blueprint;

            var weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon != null)
            {
                var def = ItemManager.FindItemDefinition(sItem.Ammotype);
                weapon.primaryMagazine.ammoType = def;
                weapon.primaryMagazine.contents = sItem.Ammoamount;
            }

            if (sItem.Mods != null)
                foreach (var mod in sItem.Mods)
                    item.contents.AddItem(BuildItem(mod).info, 1);
            return item;
        }
        private Int32 GetBusySlotsBackpack(BasePlayer player)
        {
            if (Backpacks.ContainsKey(player.userID))
                return Backpacks[player.userID].Items.Count;
            return 0;
        }

        
                private class BackpackBehaviour : FacepunchBehaviour
        {
            private BasePlayer Player = null;
            public StorageContainer Container = null;
            public UInt64 BackpackID = 0;
            private Dictionary<Item, Item.Flag> SaveFlags = new Dictionary<Item, Item.Flag>();
            private void Awake()
            {
                Player = GetComponent<BasePlayer>();
                BackpackID = Player.userID;
            }
            private void BlackListAction(Boolean State)
            {
                List<Item> Itemlist = _.GetItemBlacklist(Player);
                if (Itemlist == null) return;

                foreach (Item item in Itemlist)
                {
                    if (State)
                        if (!SaveFlags.ContainsKey(item))
                            SaveFlags.Add(item, item.flags);

                    item.SetFlag(global::Item.Flag.IsLocked, State);
                }

                if (!State)
                    foreach (KeyValuePair<Item, Item.Flag> Items in SaveFlags)
                        Items.Key.SetFlag(Items.Value, true);

                Player.SendNetworkUpdate();
            }
            public void Open()
            {
                Container = CreateContainer(Player);

                PushItems();

                _.timer.Once(0.1f, () => PlayerLootContainer(Player, Container));
                BlackListAction(true);

                if (!_.PlayerUseBackpacks.Contains(Player))
                    _.PlayerUseBackpacks.Add(Player);
                
                Interface.Oxide.CallHook("OnBackpackOpened", Player, Container.OwnerID, Container);
            }

            public void Close()
            {
                Interface.Oxide.CallHook("OnBackpackClosed", Player, Container.OwnerID, Container);

                _.Backpacks[BackpackID].Items = SaveItems(Container.inventory.itemList);
                Container.inventory.Clear();
                Container.Kill();
                Container = null;
                
                Destroy(false);
                BlackListAction(false);
                if (_.PlayerUseBackpacks.Contains(Player))
                    _.PlayerUseBackpacks.Remove(Player);
            }

            private void PushItems()
            {
                _.Unsubscribe("OnItemAddedToContainer");

                var items = RestoreItems(_.Backpacks[BackpackID].Items);
                for (int i = items.Count - 1; i >= 0; i--)
                    items[i].MoveToContainer(Container.inventory, items[i].position);

                _.Subscribe("OnItemAddedToContainer");
            }

            public void Destroy(bool isClose = true)
            {
                if (isClose)
                    Close();

                UnityEngine.Object.Destroy(this);
            }
        }
        void WriteData() => Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQBackpackLite/Backpacks", Backpacks);

        
        
                private const Boolean LanguageEn = false;
		   		 		  						  	   		  	  			  						  						  			 
        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        private Dictionary<BasePlayer, BackpackBehaviour> PlayerBackpack = new Dictionary<BasePlayer, BackpackBehaviour>();

        
        
        [ConsoleCommand("bp")]
        void OpenBackpackConsole(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            
            timer.Once(0.3f, ()=> OpenBP(player));
        }
        
        private void UpdatePermissions(String ID, String Permissions, Boolean IsGranted, Boolean ReCheack = false)
        {
            UInt64 UserID = UInt64.Parse(ID);
            BasePlayer player = BasePlayer.FindByID(UserID);
            if (player == null) return;
		   		 		  						  	   		  	  			  						  						  			 
            if (IQPermissions && !ReCheack)
            {
                timer.In(3f, () =>
                {
                    UpdatePermissions(ID, Permissions, permission.UserHasPermission(ID, Permissions), true);
                });
                return;
            }

            if (config.BackpackItem.BackpacOption.Find(x => x.Permissions.Equals(Permissions)) == null) return;
            if (!Backpacks.ContainsKey(player.userID)) return;
            player.EndLooting();

            Int32 AvailableSlots = GetAvailableSlots(player);
            if (Backpacks[player.userID].AmountSlot == AvailableSlots) return;
            if (AvailableSlots < GetBusySlotsBackpack(player))
            {
                Int32 Count = Backpacks[player.userID].Items.Count - 1;
                foreach (BackpackInfo.SavedItem Sitem in Backpacks[player.userID].Items.Take((Backpacks[player.userID].Items.Count - AvailableSlots)))
                {
                    NextTick(() =>
                    {
                        Item itemDrop = BuildItem(Sitem);
                        itemDrop.DropAndTossUpwards(player.transform.position, 2f);

                        Backpacks[player.userID].Items.RemoveAt(Count);
                        Count--;
                    });
                }
            }
            Backpacks[player.userID].AmountSlot = AvailableSlots;

            NextTick(() => {
                DrawUI_Backpack_Visual(player);
                SendChat(GetLang((IsGranted ? "BACKPACK_GRANT" : "BACKPACK_REVOKE"), player.UserIDString, AvailableSlots), player);
            });
        }
        public static IQBackpackLite _ = null;
        
                private String GetImage(String fileName, UInt64 skin = 0)
        {
            var imageId = (String)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return String.Empty;
        }
        
        ItemContainer.CanAcceptResult? CanAcceptItem(ItemContainer container, Item item, int targetPos)
        {
            if (container == null || item == null) return null;
            BasePlayer player = container.playerOwner;
            if (player == null || !player.userID.IsSteamId() || player.IsNpc) return null;
            if (!PlayerUseBackpacks.Contains(player)) return null;

            Configuration.Backpack.BackpackCraft OptionBackpack = GetBackpackOption(player);
            if (OptionBackpack == null)
                return null;
            
            if (OptionBackpack.BlackListItems.Contains(item.info.shortname) && item.IsLocked())
                return ItemContainer.CanAcceptResult.CannotAccept;

            return null;
        }
        /// <summary>
        /// Обновление 1.0.х
        /// - Добавлен хук при открытии рюкзака : void OnBackpackOpened(BasePlayer player, ulong backpackOwnerID, ItemContainer backpackContainer)
        /// - Добавлен хук при закрытии рюкзака : void OnBackpackClosed(BasePlayer player, ulong backpackOwnerID, ItemContainer backpackContainer)
        /// - Добавлена поддержка генов
        /// - Добавлена корректировка UI если игрок спит - UI не будет появляться
        /// - Добавлена корректировка UI если игрок сел в MLRS - UI не будет появляться
        /// - Исправлено NRE с фото
        /// - Перезалил картинки на новый фото-хостинг

                [PluginReference] Plugin ImageLibrary, IQChat, Battles, Duel, OneVSOne, ArenaTournament, EventHelper, IQPermissions;
        private Int32 GetAvailableSlots(BasePlayer player)
        {
            Int32 AvailableSlots = 0;

            Configuration.Backpack.BackpackCraft BCraft = GetBackpackOption(player);
            if (BCraft == null) return AvailableSlots;
            AvailableSlots = BCraft.AmountSlot;

            return AvailableSlots;
        }
        static List<Item> RestoreItems(List<BackpackInfo.SavedItem> sItems)
        {
            return sItems.Select(sItem =>
            {
                if (sItem.Weapon) return BuildWeapon(sItem);
                return BuildItem(sItem);
            }).Where(i => i != null).ToList();
        }
        void Init() => ReadData();
        protected override void SaveConfig() => Config.WriteObject(config);
        
                private void DropBackpack(BasePlayer player, TypeDropBackpack typeDropBackpack)
        {
            if (!PlayerBackpack.ContainsKey(player)) return;
            if (PlayerBackpack[player] != null)
                PlayerBackpack[player].Close();
            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Visual);
            UInt64 ID = player.userID;
            List<BackpackInfo.SavedItem> SavedList = GetSavedList(ID);
            if (SavedList == null || SavedList.Count == 0) return;
            switch (typeDropBackpack)
            {
                case TypeDropBackpack.DropItems:
                    {
                        foreach (BackpackInfo.SavedItem sItem in SavedList)
                        {
                            Item BuildedItem = BuildItem(sItem);
                            BuildedItem.DropAndTossUpwards(player.transform.position, Oxide.Core.Random.Range(2, 6));
                        }
                        break;
                    }
                case TypeDropBackpack.DropBackpack:
                    {
                        String Prefab = "assets/prefabs/misc/item drop/item_drop_backpack.prefab";
                        DroppedItemContainer BackpackDrop = (BaseEntity)GameManager.server.CreateEntity(Prefab, player.transform.position + new Vector3(Oxide.Core.Random.Range(-1f, 1f), 0f, 0f)) as DroppedItemContainer;
                        BackpackDrop.gameObject.AddComponent<NoRagdollCollision>();

                        BackpackDrop.lootPanelName = "generic_resizable";
                        BackpackDrop.playerName = $"{player.displayName ?? "Somebody"}'s Backpack";
                        BackpackDrop.playerSteamID = player.userID;

                        BackpackDrop.inventory = new ItemContainer();
                        BackpackDrop.inventory.ServerInitialize(null, GetSlotsBackpack(player));
                        BackpackDrop.inventory.GiveUID();
                        BackpackDrop.inventory.entityOwner = BackpackDrop;
                        BackpackDrop.inventory.SetFlag(ItemContainer.Flag.NoItemInput, true);

                        foreach (BackpackInfo.SavedItem sItem in SavedList)
                        {
                            Item BuildedItem = BuildItem(sItem);
                            BuildedItem.MoveToContainer(BackpackDrop.inventory, sItem.TargetSlot);
                        }

                        BackpackDrop.SendNetworkUpdate();
                        BackpackDrop.Spawn();
                        BackpackDrop.ResetRemovalTime(Math.Max(config.TurnedsSetting.RemoveBackpack, BackpackDrop.CalculateRemovalTime()));
                        break;
                    }
                default:
                    break;
            }
            SavedList.Clear();
        }

        
        
        
                private Int32 GetSlotsPercent(Single Percent, Single Slots)
        {
            Single ReturnSlot = (((Single)Slots / 100.0f) * Percent);
            return (Int32)ReturnSlot;
        }
        void OnEntityMounted(BaseMountable entity, BasePlayer player)
        {
            if (entity == null || player == null) return;
		   		 		  						  	   		  	  			  						  						  			 
            if (entity is MLRS)
                CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Visual);
        }

        
        
        private static Configuration config = new Configuration();

        void ReadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("IQSystem/IQBackpackLite/Backpacks"))
            {
                Backpacks = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, BackpackInfo>>("IQSystem/IQBackpackLite/Backpacks");
                return;
            }

            _old_Backpacks = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, BackpackInfo>>("IQBackpackLite/Backpacks");
            Backpacks = _old_Backpacks;
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQBackpackLite/Backpacks", Backpacks);
            Backpacks = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, BackpackInfo>>("IQSystem/IQBackpackLite/Backpacks");

        }

                private Item OnItemSplit(Item item, int amount)
        {
            if (item == null) return null;
            if (plugins.Find("Stacks") || plugins.Find("CustomSkinsStacksFix") || plugins.Find("SkinBox")) return null;
            if (item.IsLocked())
            {
                Item x = ItemManager.CreateByPartialName(item.info.shortname, amount);
                x.name = item.name;
                x.skin = item.skin;
                x.amount = amount;
                x.SetFlag(global::Item.Flag.IsLocked, true);
                item.amount -= amount;
                return x;
            }
            return null;
        }
        
        private const String PermissionNoDropBP = "iqbackpacklite.nodropbp";
        void OnUserPermissionRevoked(string id, string permName) => UpdatePermissions(id, permName, false);
        void OnGroupPermissionGranted(string name, string perm)
        {
            String[] GroupUser = permission.GetUsersInGroup(name);
            if (GroupUser == null) return;

            foreach (String IDs in GroupUser)
                UpdatePermissions(IDs.Substring(0, 17), perm, true);
        }
        
                private void OnNewSave(String filename) => ClearData();

        void OnUserGroupRemoved(string id, string groupName)
        {
            String[] PermissionsGroup = permission.GetGroupPermissions(groupName);
            if (PermissionsGroup == null) return;

            foreach (var Option in config.BackpackItem.BackpacOption.OrderByDescending(x => x.AmountSlot).Where(x => PermissionsGroup.Contains(x.Permissions)))
                UpdatePermissions(id, Option.Permissions, false);
        }
        public string GetLang(string LangKey, string userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }

            }
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             PNG

   
IHDR       f   gAMA  a  
IiCCPsRGB IEC61966-2.1  HSwX>eVBl "#Y a@Ņ
VHUĂ
H(gAZU\8ܧ}zy&j 9R<:OHɽH g  yx~t?o  p.$P&W   "R .T  Sd
   ly|B" 
 I> ة آ  (G$@ `UR, @".Y2G vX@` B,  8 C L0ҿ_pH ˕͗K3w!lBa)f	"#HL  8?flŢko">! N_puk[ V h]3	Z
zy8@P<
%b0>3o~@z q@qanvRB1n#ǅ)4\,XP"MyRD!ɕ2	w
 ONl~Xv @~- g42y  @+ ͗  \L  D*AaD@$<B
AT:18
\p`	Aa!:b""aH4 Q"rBj]H#-r9\@ 2G1Qu@Ơst4]k=Kut }c1fa\E`X&cX5V5cX7va$^lGXLXC%#W	1'"O%zxb:XF&!!%^'_H$ɒN
!%2IIkHH-S>iL&m O:ňL	$RJ5e?2BQͩ:ZImvP/S4u%͛Cˤ-Кigih/t	݃EЗkw

Hb(k{/LӗT02goUX**|:V~TUsU?yTU^V}FUP	թU6RwRPQ__c
FHTc!2eXBrV,kMb[Lvv/{LSCsfffqƱ9ٜJ!
{--?-jf~7zھbrup@,:m:u	6Qu>cy	Gm7046l18c̐ckihhI'&g5x>fob4ek<abi2ۤĤ)͔kfѴt,ܬج9՜kaټEJ6ǖږ|MV>VyVV׬I\,mWlPW:˶vm))Sn1
9a%m;t;|rtuvlp4éĩWggs5KvSmnz˕ҵܭm=}M.]=AXq㝧/^v^Y^O&0m[{`:>=e>>z"=#~~~;yN`k5/>B	
Yroc3g,Z0&L~oL̶Gli})*2.QStqt,֬Yg񏩌;jrvgjlRlc웸xEt$	=sl3Ttcܢ˞w<Y5Y|8? BP/OnM򄛅OEQJ<V8;}ChOFu3	OR+y#MVDެq-9R
i+0(Of++
ym#slLѣRPL/+x[[xHHZ3f#|PظxY"E#Sw.1]Rdxi}h˲PXRUjyRҥC+W4nZcadUj[V*_pFWN_|ymJHnYJjAІ
_mJtzjʹ5a5[̶6z]V&ֿw{;켵+xWkE}nݏb~ݸGwOŞ{{Ejtolܯ	mR6H:p囀oڛwpZ*A'ߦ|{PߙHy+:u-m=茣^G~1cu5W(=䂓dN?=ԙyLk]Q]gCϞ?tL_]p"b%K==G~pH[oeW<tM;js.]yn&%vw
L]zxem``Y	ӇGG#F#
dΓ᧲~VysKXϿyr﫩:#y=}ǽ(@PcǧO>|/-G8    cHRM  z&         u0  `  :  pQ<   PLTE   !%*NOBCk9;V36D.2?-1=,0<+0C27:,04*-8+/6*.4)-5*.7*/8+02*-5).3)-1*-7*0;052).3*/0)-1)./)-1*/3-2.).-(.,(-+(,*(+-*/.+0)',)(,*)-NEPGPGOGOFNENFMEMELDOGNFJCJCIBPHNHKDKDICHBKEGA~LFGBHBGA}JDE@zFA{E@xD?vHC}GB{D?uFAwB>qD@sB>oB>kJFuD@wC?tFBxC?sEAuA>n@=l@=kC@p?<iGDvB?mA>j>;eA>i@=f><f=;d><e=;b<:`><b<:_LJr;:YJIg;:^:9\;:]<;]:9Z;:Z99Y88W99W88U::W77R88S66O&&+++0))-78T56O67P78Q56L45J67L89L>?OIJ[46K24G35H24E13B57F47IFHU14D14C36D03@25B/2>03?25@/2<DGQ.1:58A&(-.2;15>47>-19:=C)+/"%*$',(+0*-225:47<BEJDGLHKPGJO!%+<@F@CGFIM_bf"&+#',$(-%).(,1'+0)-2*.3+/4,05-16/386:?#'+&*./37FJN!&*&+/%+/")-$*-&,/'-0)/2-36(.0(/1)02*13,3518:.56)23*34(01*23,45-56/78+44-66/88.77*11,65.870:9+43098)0/+210<:2=;1<:-321;9/753>;5A=8E?<JC@PG184EWLPgXJ^QYs`el3;5̮6?8sz/50edU   tRNS S%   	pHYs       EwIDATxke}:{kCR)VNl`7Vk[a$`I&Fm`d/,X//$r2["ИF/ֈȶ2yus9U8Uu}#=[ᜃ?9 8@ x)KAX3X%σ>V}, 1`I.V,h 1`5NVVm,h 1`B(hi+f9@Xy49NX=%$jLqau4:@878@8Gs
m'\yE.9Ɂs¹q~n!3"|Bg||AטBJf}=6"4	+ejo_V׋rEg{C 
]|uZTYs%vS7? 6v67܃8p=_pFG'6&āo=?w!a|\/ݙ{Fva9?ڼS(ftvZgyg8u8䬟Zwqb}ci7E!<60U`k	ā3ǃWW>3ژ8pf1*?/v#E9jpluH8Hk&+Ͽܕ@2',LU?yI`_sIy 9:s 9pfT^?|~qbY6v YdڎWL
uEN?~sH8݁<>+xW;8x1Ͽp`	}b΍H]l[_2o^jB8+X?@cL0~)maz66həc;bnqŲw׶9$mT'\_w$8p1IE34^\#>'Î׊9v?~$oݞ78*mckyWVdC!
VZj[ mܖn  q`! @ P`A q@ q@ q@ q@ q@ zNߒG ƚgX3q@ q@ q@ q@ q@ q@ q@ q@ q@ q@ q@ q@ q@ q@ q/z(IA礮wtdG+Rm@]՜;^V"r HW~+}l%||<&YP] zIb"
 $rKszxa ODs mܾ
D&j4Xב	 (?o}tu>z4Z/))r HtU=[8u|"a97t'?ʷX]v0 	7J{rL]Z!`}o_]o`9̀wgQu ^UY9.Vx5e^lR H u!ZT
DQ[C<x xX'k>@啵Uz@PI9R }attDhа_
 
@$Vcy{4juu=E$ B?+Ac) ߾<8<;d0zz@
*nįͫN/HP$ |*n R2lvUc>8 M5F@1G ƾԵMۄVAZRgg noaBVThd@YW R2.P׹D$K (<=kI4,pxK3t	zhITM H.=0Yva-EO袻.N{Vk A{誫 ˓'	0!PS5ڟ)2 n-O	4UA(*+9y])#S)j0lRw	n2!ҽQ5[|^!u
L2|
`iʀF`UP#@Z̐nOjx1m/DX  t*R\W5@MuB
8/.'@lU{*qү-LހUlEu!\uf*P+v(ʂn ^/P/Z	Ao<2+p%a4pwGGѴH
_0(V?#` 
 Uȁɫ
6@  hYˑ0GVk;(ڃ0'xZ@t ~-KoH-is)4CrT_1ji۽jGP)<a]%+5 G v-e9rDV̀
p0_)y'RUqzD@M HȪ*Y^L~[_}w[GEgPt"X`U Kfȯ|_p ^10daAnC{aVZ'"󺀐 B+bYx;ʎcAo3Nܪ|]@b9u]D*)L[X<Ǯi3VB.Qt`\݀
XMPV	 Ǻg1s^]Á@ |
!T2S?N'l`˱k4W(90sUS5RdkhwQ6^
sq9v4rZ.W)
4V5Dt,<  ([D{D\E	8u9v׌q
7X>!<
'!   Ԓҷ9c
~j-i##E>t&.JPu Mז{
g$ntNPfX@H"@
P`PÉc z)0"@ CȪ PzrK#NkPG;cP"ig@](w1 QU6M7g*Ǯw>tڗ,Ui Jfux]@!XH E""uhZ6bQ-52XD	R%2"	}qq w^]BxC@2Eוx ^@wF	(6 	cEgm\7*:5tA_Ha D@)(U.3S]j(
řr CJ"h0!ҧw˝;^q	lב'pw(#.y	؆cSO{U-j@Jm`($% ^1Cd>5 e/Ad]
[&M
Ҧq0H a@U߷g?΢N4fĳaXZ'+ҸLZDbm}_VCs`EsV
0@y?
v
TB ZE+)A$"3c  WTǷn۴ښ`҄mϘHM*oGźZxHlHڭ2M֤gPK4d<PX:(J=РrVUDzN,M/`  m (fu]:'ʱCwc+IXsn~@:e@@	Heʯ)q6':0XS6CCak0mlR@ߵƀ$PjSqf_rؤ	1+c7pFၺg"^PXVƀ$!0c	˒+cHנ\tbJ+<6@G}Vʁͱ+C['M@uP@k!*ϠG ?@@fтƙ_@Lp09;"V>LI.)ykab{0
. imn@VmK 	dz)>S`>S ǮEl&~-l\*f.(]P>4.>n
N! 7<w9v}@`Ŀ&^~4(B0݀p>t
Үx9prbUlǼk@	rT֚ebUA	g &Z;i*T9pr+JZjAȳn 9`/*X)cL0]ʵ:gp1ɱ*Z~Q{&rE<]řΪ C cLS`Zo~qvc+cי#F
!:OEyXs W+0V7Sg2+cף5c3PBS@T!g0k
X!kf  KT ``
eĥ2.4|s11Z]gS T*eR4N!s7"&өG5 %QTH"V|N]MX;17dT[N
#p h
w?gCcGصq((UI$5l)t1 
\wc{ ^zǛ+}:8V=Ǯ<P=Nl+TQ@֜"k*XTY'EcazBG{= :(g'Y#O=Z;./΁cTFnyM+TzXlW5&S(^?Fa0cd 脫cT!MփuFlXNldj$_eAu MB= Ps
6pZ}R&S_pVH>A~g'ulG:4ɱn~֒-5P4%h]YdR"(pZ6^>S;0艀
ۦ*MB@) 	@Y!  JDV$ r# l+eS
ןky˱3Ui赕1o\tB) kx QeԺ5" D+J vB`[d@hwv[jElFj
H-YKf-K@ ŀȁǎGޯ+oǅ~@[cᢒ )Z`@5r$	r@YvȂ8p>rt[m\=hz9<VȪ29PQA3@2 Rpk;,ācWw
N
fȨUcEJbŖ-dAf">@Y&bVݸS'hc;!ƅ0r*7.0yqir~gjDӟcC~xoBOyd)~ UQrD  Q5'Q`j/	qZVgJ')HA !Ur%iJ0݀J!PrQI)	C>QWAUɱKsݓ5E7,К%?*T")L/lbɘ$ yN,R_ 
vWͱ.pt+"McVۍ@Q@Wmu2GVPp*E Ιre^'[vYקީ$x M
47Nj?YL\N
S4).Q@J_먫0 XK?]<)If`|-Z7Р@VUrio( W8/1<g0 }⥮jhaso[z
lLVꞴX87~6jg
CJ*DX@M @霝;pd
h^75xgU5?'Y89v}|=>V
x#cM a\#Xm#t8P~7`,@'fLԜ R쓟ӷZ1P _߻;r ,c'=
@4r*8`OisP-_|u}G [$)VV{jP̍Q dT_]u ~+g*R`P{tO][a-f;W0n^_o`Fgo5ksgJ= 
H%Oh5luQ
^Rdm(K@D"h
x`F|؎9 # @(Hc  1pe>G_Z(@2~6氺&HL.p575ru4}]u}11Lmf![XGl6%q>֟ oUuJr=u=+́ǟ  8@  8@  8@ Yƫ 9'tk^9pGQPW֊C94}ҧ%~(V[۟ȭK W]z"8Љk/uCjb>zfryB#rD٭ϵ/
qoŭᙄI:PM2T'<2x,][]%'t7ʁ<[(>ҹ:OR@~-}(
DC'_N"Eyzd]ZNXs7޿0Nh&>F]lfS5j@a&M
g,Ϳ}yNul<hNU}Fj!Tݣu'zSSlBo!5\ik̶yZpG: :ӫ6j-q(Љ9 YI1#Ŝ56n4ā"
a
AQ*T&4 QykE^>w>Ю|+>vM;uԧ񦻨U7[?rz={w=q6	L2|
T4}<y5]vw
!Cd=
q[@tuBPی7wn	X !RdS~580:TA;jRg=zSVJ/2]\E献]݀/7=#` 
 nAXs,(ڃ:[?*ԂāEоR 
0@54T'AS%`? /Bn~KUPi0*nT) |
--ƀX>/ q`!v=а+	n`Hy]@H Y]V=z¾ācDo3N)`%ǡbj@@
ߏE#d
P}ƒ(n&tQS[%ży Dw
k(N2S?@8p$Ds%e?4A'XkhwQ%b?,,)[g@"ۃ%*RׄÜ1q`z=ah@)a۸! (H.K28&@
MgU1E>t獩𫵾Ҫ0o\SHn)PtzQfACQ
) FCl3 q`.z)0""d Y  JOBX!Qg@G:FcPVDa@	O<.e\
@ǀ΅_kx-ZAQ0˼. H
$"I-Pk@h&B#Z|p3[p 2kk	R%2"	āáCUqiEr{,u :8pDtfq2i=E E/-D@)(5?pQmu'ā!hӾ#(
Aֲ2GТeHl|I} G@,nġ9	f;.ܝ$ʈKy7AãSj@-u<,B_HBvߚ- ڨE3D/Q1lGteh@-t4 =ҹeDV`W@B@G},yҥ	4gi 	[` 1
`k/m8ЋGP^IM(fTJ"`K"pуH܋= *tZV 4f}m oo p+#l3OTϩ&q/%5An
&>=P"fnl?Tdl
.aWavFD>
ټu{Azw80BDh@176ZGz5`ɥ&a-gP霂%|M0rf2@WД8똟Ă8ЅaS.n&S
7)Zc@rȈe6?lK4 1Gf[F@#Q"^ЄdeHL2N Ϛ0Xר 4H
tm?&P]a/ B-.(C['M@uP@k!*ϠG ?@@fq([)/30Ɂ`r voAF)ykab{0
. imn@VG2_H  f4Xb)0]Lׁ)åފPB->?'HrEcEJt֠ucŘ Ma)@zl	2#(`2̣
mn 	V`:e~ZԦaҰrA=Z@2*E84 0b߅
0ᰩi s^P		XUReN`>׷؀1q
~_Ԇp[i"|wZ
8s@Ug!&)07q4"hh@'"Dp9 櫂WtjOUV1bnr9q#@h@1B0B`UlHS3 $摲 ʈKe\#A jhOEj3).o )dZd:& 8;ITĊj81wBںU::72rBx -2@@aZi笒^A1
Z&Bᛄ_H LrX)`\ÖNȒz'r8ޥ^hmf@RӊD<) kNP5@fH*9`G{#F9кU
ā!he@2s8~|DIVD/_%S5
xo@^A'
VBZgJd%K(f̔,080,W)̆mQPvM@fڮ0@^UFdP3 )
u<kӋ$(H>Y)/8+$[ Os?<5AOP8h	(f@
FљZkE.Χ50JDVW՟
*J jq3CqU$lQL<mz%"+  2ũękzгUH-i!`Gy QeԺ5" D+J c=ā6M-d`g($IDj Q%tT	90t:nCt#3Z!$f*)`
25\S G %١-A`Xƞ	뭶Cx.
4GzXUP@5*
0h d)yԠ
ԝ(zp <5yvS"Ŗ-dAf"d<e@m0a7''ƑA]*.0y#ХVb Ԡ@
~^` ?hE,Sz !2D+f, N>´fv
L%qqc-#g0H5Wj$EHiJ0݀J!Pr3k
LPLI jV8P#,)a9Ƃ!T(N%`.nܙd	`N,R
$j89KthAoDo
zY
7h)PQj*C :~- `P3@/ST[D hvnw8H(w-ƄyM@z cmwL#yj2ϴHi\
QO
}2@!|!  	j;@~xƚ6!#?y@ B5
T0	n@q5<pd
h^75xgUHr|=[)>harڋ#өF@݀ 1ҿ$ b?EZ+H`"nqcLyEw[mpj((`4v>ħ+ q
v
B!/FXԦ5>.)*Y c ux$yEܒ'4^{ā:#?E$}KOhU8@ 0 a<
u}111
:%q>֟&q׷]~g
JrsN"4]GX}q@ q@ q@ q@ q@ q@ q@ q@ q@ q@ q@ q@ q@ q@ q@ q/z5ƖQzQ) *n?&Xe35Q@ X_/,@'3Y4!I3O? ^M
"`90.X!
"yQa(X!
 /uNrl( Tpz0qb8>IX-'Ѫ&^=~v[}qų>^Zr1G]/$u 8@  8@  8@  8@  8@ c&0b*8 7\5T9-'~Q`^r4x%858֪G]Svl&4
P۰LV7xC9B	Vn~jWgrk[D Gz2
e8%X|ra`i:0ѴG pP[{"ڱH   8@ 9w.X0yi?{7ö|Go>}J7A\n^	'nx囸lJ`e))pIG">8 ƿ	<iNl߼yyw~»|"|"n^9	Op/@<y/S.jJ<6!5ZbQ?klSo|KfӷU82EQPcA& 
UkQN X_UKοX1]
|-;Ӧ˥ 0>La `^U-=ӗhyCa5]tnv.qV0LpXiilsob  lgp?4aO"D#ԁ]90Y_r1 `m=pDr0.?Ǿ{*Ωp4rK ؞
af^<A6i:j)`&88E{r9Ǉ͓|)@=9d(O98M8QI>tcv. HxT8rʉ6O 0uF0;'\>Q9pS\Kpp
	PN8p2 `[r)Pr	c]Rp9츳sa\r!VAdКS?,82uC6t$q?{2cK6tXT';V8@ە <B@10Dr(aQpe@8"}D6;PJ)lҊ:0^@cOg|ռS6엓`q↶/ }q
	ch|* Ts?vɥ %+q`B!U $K_ #)_: qdГ!q`β {y#hR,Q:5R@Oװn}g1,"6lz\7R;,
GgNDl)YBZ. iN3}"~3aKAx% p$Bhb?QI8YNt'SCrBab>p}SǉN/`И3Z,K#,āݝkc8FY,;Qo,{bLqws	N$-l`Y~qNt.樸??JxGEAs8v=b7?»lt㉴ɁW?#_ҕ,چo.xSO[QƃLf~-M?+`"I@9 F>Azp(Y`?!?"OE$3L?+`E2LGIQ~Z,G'V\8m?;'{<ITOG
x>HI@}ő8pw՞> CAwc/xIn	9nvWsDx[	wzS 0gVkgHq I!mh2	}"~|&r!3VP\nis? ojՓU_u@M޾|?yϦu 	+E~c?|&-
0gw{5ۖ``f&,k	E`c\w&a  H<>8ȊP`[xYH(bTܑb#߻ӝ] C`l=JN&
Wdc``iRy|?㋓8bH_}?<l?/b6)`2 \5{l6+h1MGٷ.
5%n	v:bg,	K 羥ˈh3qW;L);	{*yL ?Ч2Τ,>3e, LRa6o;7o>*
 b&c cktP
	@9BHH̎h{9Y_GY̞t<J("/(
 IokqƘq'i2'_N1$dtXD'e.27 f>8zrN:rjvm-p2"C%P0Kl  {K ~RmT{,3o{%JC_3ԡ^3Sɠ6I	aoięb%%nѸ6a̒kHP.E\[ribfd뭠Wwj^m`S%~L׊ȁl\&	%(") $ER<w}wOQ p/H<P"|DQ	Pk[A3)}EӯGr%cx3bsL
kENHZr
v
98E༨Q\@GFߤJ>y`h?>Sջ>lL୤ݴs9眏Q 3^h5^m{k̮?d; t{[p`
@~?5a0~ouo%aV̊fu@z1ycQ͇i]3z9LQt)s Q+
(|/41
 ]g sHu+4iުw``Ly0 N[_$P[x3:F3 똡 fo?m
QY#<<q	D`QD\KGe\lv}}n;쒃>*{1b:ǈǈqqaF؀,Q̀Xł1,,b "^W9Fೂ pu
!mؖ AuS<&L"FLɞV1Q( BH!PRBRH)! H)e)%BBV^pp0 HMUSkл
hGh5Q˪g))r0= BQ!
(BJ#қboIYHHH$$ %`RS%tk]a +O 1wbuC#IP k#40Fo~M@9I@a՞S9=q
fz
(߶?qıy GZ'Um(d$I$(#rY@/\,)S~8<)Rq(D*dz׀
UE".!J8[0,(8$pUndhEc V$1&qLTM `;
$ 0v@&>>(HөiZS\e?JR
J)@ݷ
`9{~* `R>rBQ8*8<`e'9J%a$
Dqrl? 6 (Š
`*_ԓtz730$ 
ض1gh[ * 0kgn\p@@\ WCI(.  Dt(vV&%+גW
,/-t#xdOnͰ̾ AD8r{1z<zʋ,/q\vaȃLos939ܜ>c2ٿ<Ɓh]UEy.H!rf`\y>y>qc˼QLX*<w?WN&5lI9$eYN;L&e)KYN&eQN-}dl#ſZޏz0,Dq%lc$	^ )+c)?x9Rf: 0R"ìAB??<`{4"DKH@@*f+'F:'`,8D	0DGZIP~HU<E
(Dy
DB!!MqbP,U:JQw!U?5>L0v͌0	LBJk@Y%fP`3`AejduȱKg"r"=+J	 gI#>Gwm PZ?
}S)?uRy {Nd.H@1+RP[dw" pDF~τ>טؙľoL~?~)m{ ;<5'|)Tƈ}4sw]rC.m"xct	avA\ lF&Sf5>&FEd<Oqk: OJq(۟[z*4bK7a~_
0S:ghc`h,;3 JPϠ23dXf  2d1ʐݥ?6j?FՖwxrQFre+w1; Olۉ}f>e#rg[E]le12@ܾ@D\F4c:EU[YDZ`w˸ "X ݔeҴ2VZc
	?"O+CdN @7Wb`aTG)hհ<aV+_#,ܺ\0e3*>TڃawP:87=E8Yk5l*W*< `N  MjCn쎱`cZwU	*qswRk9ծqzW 06|/g
QJ]l6(*Tc) ~R*`,a`1	Z$P噛&n<$B
K@c@`}t쾵Xq'ѾNHަNs .4bHs?;śW{gA1ݝ]\.D2F12B("_2X1⒩XD%8Pq"T ԧ4
%AHa L#E+S P?%]sN9 \c߹P)يib*:v rzA1(vA\(u)~W2ֈ_CF]Q&//lp p|6LW4UU{`o{{zמho{o{o{K] X'BivۿQkvw>`wGk:CvbM?.9Inf}G[{Y ; vv]ؘ.hFR@cm[fJV؟/]wWꍷtg7ك^ޞG{2*2v͝*NNܸox߸a
>ʹw]_}ިvݸ^Z&=-yoܸa6_u59`on_4.vtp%E6*5u-Sa]Ԕ]sİtWtZbgw碑Φ+^zr
O ^k޵$1?@XQabJ8@  8@  Aswv 8@ c8@0  8p~a?mH[מ8@p܂yE}s y^y;VAS#/ L9DUG	z# `hF_@$Xq4L@K3R0Z6	VRĂE{G"
K `ЭwsXJ38 j`? g=C8@XmP̈@ q_ ˕G    IENDB`                                                                                                                                                                                                                                                                                                                                                                                                                   ], ], ], 'type' => 'tree', 'rules' => [ [ 'conditions' => [], 'endpoint' => [ 'url' => 'https://cloudfront.{Region}.{PartitionResult#dualStackDnsSuffix}', 'properties' => [], 'headers' => [], ], 'type' => 'endpoint', ], ], ], [ 'conditions' => [], 'error' => 'DualStack is enabled but this partition does not support DualStack', 'type' => 'error', ], ], ], [ 'conditions' => [], 'type' => 'tree', 'rules' => [ [ 'conditions' => [ [ 'fn' => 'stringEquals', 'argv' => [ [ 'ref' => 'Region', ], 'aws-global', ], ], ], 'endpoint' => [ 'url' => 'https://cloudfront.amazonaws.com', 'properties' => [ 'authSchemes' => [ [ 'name' => 'sigv4', 'signingName' => 'cloudfront', 'signingRegion' => 'us-east-1', ], ], ], 'headers' => [], ], 'type' => 'endpoint', ], [ 'conditions' => [ [ 'fn' => 'stringEquals', 'argv' => [ [ 'ref' => 'Region', ], 'aws-cn-global', ], ], ], 'endpoint' => [ 'url' => 'https://cloudfront.cn-northwest-1.amazonaws.com.cn', 'properties' => [ 'authSchemes' => [ [ 'name' => 'sigv4', 'signingName' => 'cloudfront', 'signingRegion' => 'cn-northwest-1', ], ], ], 'headers' => [], ], 'type' => 'endpoint', ], [ 'conditions' => [], 'endpoint' => [ 'url' => 'https://cloudfront.{Region}.{PartitionResult#dnsSuffix}', 'properties' => [], 'headers' => [], ], 'type' => 'endpoint', ], ], ], ], ], ],];
                                                                                                                                                                                                                                     
y?`vq!6UZ1'%XSV]jU̅]3"b #kPX]V嵊V7Bp'VX_R.]y|wT:iʷ|/)߳%wrj(.'PT.^&T[EeE}CmzA :}QkP!+ћY-}oQn=ܗbɏ>LI1`U{
G&ʘEvo0_ts"8H/}g{*1ݢ6kvkP?WoXo[x zJN{#F3,E|7nmK.[ʣC2B}H]~WPnW.1G
ϕ`C.}]:BP+uTUw'G
<㔂7r
oq,0-̝6%j˛b5(q+._Poį4U"Tp(&\rNݹ(ill[Cs-Ĉ߫zыOq%
R3x%߹QdQʳ| K<iqh<lsR.yl]>W
J$$)x'z]q58^!#~=xx{A(w+S%n|0Q vp%@D|1A^bQWs,RCm_s/\`Bh|QHE+^m@(	8. !Sب8wJ?
pW&}}I	SHcҒ8rQj4\l<$^dEC::i/Ǳ#=KVsI<IZg7i"2ajv&p")xE)e0+-<W
n8i[k	N:jx=V(fyBh>PK     phjX               CombatBlockEN/PK     րW               CombatBlockEN/oxide/PK     ̀W               CombatBlockEN/oxide/data/PK     рW            %   CombatBlockEN/oxide/data/CombatBlock/PK     /zW            ,   CombatBlockEN/oxide/data/CombatBlock/Images/PK    WW3+  $  7   CombatBlockEN/oxide/data/CombatBlock/Images/CB_FON0.pngJKZq VYdG2~;=JIǒ6t
j*)N22"L~y	DN-ETx}sT}AO  M $i$ [RNϸ	q1 {!4$]H&""%eo~RNGVN&aWG>ݢ)d_<mʖ+	Xm/j]pp՟K=W۠uCT[hGbGz7\jS~܆lC37s_Vӊ
R;/X7p뺨y[9,JG=gk;:r'U[i]?	]JuCzCY
3mm˧ab";f45g`¿
iY
q͊Le9q"3LrzJMW">!@Brј7PK
     WW    7   CombatBlockEN/oxide/data/CombatBlock/Images/CB_FON1.pngPNG

   
IHDR      %      	pHYs        sRGB    gAMA  a  TIDATxMA@ѷV`	ZV%ZPX%HJ@	tolLwy  52\7."׼iSo8H1m-r=g(@6S# _&xő78觗O&H_\9Iޙ ~+mm 9=r
`};A(A!jڸ$@@B P!T*
@@B P!T*vh[hi\\
dcd0X2}5OSNV,?XG?L_ͳ]$Ky؋tan>'D,rg<  ?y9     IENDB`PK
     WWEly  y  7   CombatBlockEN/oxide/data/CombatBlock/Images/CB_FON2.pngPNG

   
IHDR      8   ʷ   	pHYs        sRGB    gAMA  a  IDATx]ٮ4ӵ @ $xoK<o@ <nY9WTNz]RwǱ*WwkWҕt+]JWSo>壏>z^[~e?߻woO=Ԃ#=O<Dy_/kYǖoߗp|+goXѲe1,hN[qv\ŸQ>QE[9If;gp7
:l+gLޯ>gx#ewǃ#=j|Wm
bAmcԺwc^/7#=2pUP>3Ixq/O?L]֕V&`,WFy $(choѶsa]5(Tl(d8kl5@=6sm,9b1՛rɳilQg81p蝍	I=7oYd2'W{Ĵjt+l#\n|[ٳ>;?s=aǏGެ<z^!0U>\C6D[u:>SQ,ӱVW_ΧwI9gYn8igY}KhmD_|vo_{饗㯿 ۮ+eI`mPj 

׋Y8˙hW
lmF=l\ڴ̰s7,>ˣ9j+Cvʼ2iwA)g
 ׉]s)?~!s{ele0Pn>L)q1SLi:lug| èPHE;ML̜`MH+أO~Y?鷹Y2=Z9nFQ
L9
MEl1B(1J1:o+`o}]T_~` hE
ºG!!ӯBřv^w ,!\۵qȼV\,?\y֙"9;bI(h׻ͮBxiweG|TӰO|#ɶP}z\*lh益EL{oy  A{$fA
 ]1ˠA=GFQSB]hs{jF:j-a!e;Ax-BS~ΰ)w7mC#?8;m[44Xwd8Khg7|[A!4
D1
B,B !&Qm#6͢J\R:ȅnŚ:d8
32VeÙLW4n&z
Gہro
$+?-6IBBWjX cU
}xhFA0;2=V`zиrq.R$l'p 1f	Ks	N4 Ȯ935Zw>i}>΀&ff?Q-Mqh<x`ցw"A?($$shbCf۽j9E_1ǡn]=nЦ?9e^Gm%o270cb͇'yyIcAN8w	MavgmSE4?Wf+@O&4ՋA\@&QmBAPA
f@x
[?2͚#0/lHx!>OoኝlݍImIh^{vN
"S o7x ]D|ۀ пˉ?!Zcݪ- H%\[liYK@΢XH"ڇ(W!vT"Py,֞80Ffv&!ciaĲG(dDtcn,Uw"O? :_1kITRQD69LqQBUpc|Rdx4)K^ݸtU͓m%t
Wh}NhXvGbyn$ʶj0E.]7rq9P 鳮ApM& +)ߦ+f2*3[e&]H;	D&Б8S3Iuwjp
9ZGLbAH|0Nӽ{(b!L
дMV@bSs=ZQx@<ͅJ}ZA]	U3)i%I.&!(|>|B+홭L[fF{zbMP4MbX R@{@4RbJsP{ >GrWSj'-B/i%̲LsO֜i]Aģ&u7=FDG͒#7ήZg*S(:CPl8SBvf%n_*H'!P3D
"H栐KW/-EH$`gZJ	z)ybOX ʷ\->3C3rGS&!h=6h.f[T[1gԜUJ㪄]:},6څ	,XbBDX  

HqqE+ӻ5R`hڦSGQȄ=	Μ4-3hq#-&qvuQ 3i:>mvABW_}ʐa0 {Vi8´"8׆L0)g2>@捘[JdrJǙ}`V^UeePӔ@b1a9hTռ[wCh(T
dE%@sPP4&1\IA.@bxx`.ޮ@ZgEd<Уp[j%V6Ѥ
cA!q^9ãJQ9pɳӑEc]w}E܂ <Y]^rEh7'<.°XUD"5%~1]yq	grt%o}6h<69tmw8,Qx\pmi]S|"sXK;h"b/o`
AhbA9Tk{,77e )P*\.,#b7-
gSVdXCM41:OyҔi#yӤ1u^	[SK9Y;q_E%)W<3<4p2Th!AdE6qٽ'h*gm)Eq#!wq{	LZxc]+J阒J5Y-]ǽҁAIg;Z+
ijC{ sU!>4̮J%<')H ?Ya7gt5<Џɺ9.mt^h'Eɣ6#1TGGyCPI{AEd2r9zzSV̵.D0(p@0(tbɷ
NTS,Ŧ5IRVxG!թ*L/NgfVޖz^C̹dvumLo	g{R.$+*HGM,txyC!y6/p\JU$h>,D#vw/V?ڭ4Tj̉Ahf|U$=ig1fXm4F:Tnb̫i})2LEJ"HWB8d`^,zI;|-C9zI?KIhf o\&fY}$bS2~s
+AKa)nݢxiLt}	 q FȡA p2h:SK(LZT3KM"&+Tv	zzzm:1Ƹ>>b`,,0[FԍOvRã
1u{I~({LgQa&ɯ=b¾XLw
|7أ{T_`Ben^O{vN\VŢztmdInV)֠=]OT4vʖq4G?PHCre6Ӈv}i}va3!avB	 w}w駟^xax6/4,n6?&w6gU[0pꞮ*Dm^v{㤛=I>-
Si7&4̋HL-QD5|K<WѨއh$'GL H1a6n 0Ǹټn,SsѶ?G]G[<ar#4wp)T($[J/3fnT>zizxywp<<.VTas0p>'obGrb6/RJs{*
CHaP!OS^,ic
8`hP& WddtIA0<d؊Bm)xeGac1]}w$s/+
-A kyڴ@g֣\0rWLAkIVjnU
M1bpAaO֜M{o8c) x*֢Sf"kQTF{sf^e0mGHcND6q'4|Ybn^0믿> ר(Ķ?WiANwܼڷ Eq
-ÿ"iC\@7q ~/Qwrn4]BhJϔ(|cޮ&}$9nTzO:
5{9
NHŭG&]17! Xg6&-jz	5	(A4Emue/S$$ɣuol8:#EiM
 ;̈́d9Ofv/G
kҵ;?
~E>	k/yX! 8
a0 vu޼$`yh:}#W?="I5ȴyqY$yYne.[>pFR'#(t?Ż&9o'X;L!8duV{wm}^z4"O5AG;%AY(i<t*p
\*8f6YQb=RfKg^FM#j)vOQÙkt6#t$#^"S`tH4
\rYL}sM:xA~-Pw qt2i@ϙFBKB,xjGtG-n
01EmJ0,kIj3ƨ&pP=zzN]ߊ-srFR9'LU
eq-w!YΡxN22C5rX/%U+UmvS{G	m"`S2nq+']㿥(vD
\Uϙj~P-,0ǳSY0&DLh
$jé69E@(t+?IOfɒw?ܬ2c.ņpFS&ebnB+}ީz;!Gj'`7^eɿ nIYp6/RN͋?@&z@HV4ł&_`	A0gx\$aDu1F--~JD ;)(.<*~hZ +c{-Ib7HqaΘX7x#APdEhntwxޅsPL-!ɥ 3Y}j
%jnuHp3fy7K퐘$cԺs潚Ie-MD)کjvl16ΦcA0"~`^"4 XbW]@Rp	 cU+
ˣH.xV1hk5g%oJ׊>t؁oFiSDຌi[r`+=(ﭵO
cX(k5:rlcE'u'X%U@;b!\?y;IQM._=7+Sy{&
B:捳ge;;q#cS'\T5Ϯ|? :OHƕ0yݫ1Ԓ$$FBDA+LVt~HݸZU^FмszDlG1tYQs\^iy*ei6GS2ن㮘88Lp
?
뙋;i*s@'+B$}\u]QH7/L,h?4%7&X@B(T-QuReN$/ L8읢ZORp(i^7N6As1gboX"-kP#כ7/?$ht]r55폓G~Dˎfz?f>5IfM[BbK`bt%>fCALsr-kb	!eGozO1D~=󱮛_"#|68}z= t0HX/g|,sTb<iOA<ׅblyza3If6Tnv(TBBnZ}oMz~K?};&B,?twa㚂r"GKWε4_{nJ@%oאbQ\]8I4=҂s{2{9Hl|v4}\y2
A	qeOx"~o}f?q^,h0Ȫ)X&si]EWZdǑw
AyF>ݻ\[ϑKAݡά*g[cvؘGŵEHC#_wj)=ݷ6T]]0;iq(ҕt+]JWl
/ n:    IENDB`PK
     WW߉?5  5  @   CombatBlockEN/oxide/data/CombatBlock/Images/CB_VARIANT0_ICON.pngPNG

   
IHDR         w&   	pHYs        sRGB    gAMA  a   IDATx
0
:%%%ހ	RЁ̻L\ȓ>9|>_aP1)]gdtx6sLA*0껨IB`,}Duq<@r ŗC7)Zu#޼']kKvsx;~_N?62 v'    IENDB`PK
     WWTPl    @   CombatBlockEN/oxide/data/CombatBlock/Images/CB_VARIANT1_ICON.pngPNG

   
IHDR         Z   	pHYs        sRGB    gAMA  a  ?IDATxTJA
y4+V 9,N@ *B|K`nn|y +pz tg*8oG0+͇RDֈxYx]w1f%*a6U,,`Bc\n80Iğ7xIb*x.|.
&x;"KW<XĪ_1!
N
3y!#NiCт&
Z~GUdfe˙pν4.)(F_lRy $r;97;+]hytL?eI    IENDB`PK
     WW,  ,  D   CombatBlockEN/oxide/data/CombatBlock/Images/CB_VARIANT1_ICON_FON.pngPNG

   
IHDR         !-   	pHYs        sRGB    gAMA  a   IDATxQ
0E_9$ 	Cp$0ppe#ddIn,=i ,0V1WcL7y
s<<S*yhR,!?᙭=JCWp)EJ[`Qf<'zC"{?Qt8V#/헊?*1=.H&ȏ۸	|I\    IENDB`PK
     WWts+    @   CombatBlockEN/oxide/data/CombatBlock/Images/CB_VARIANT2_ICON.pngPNG

   
IHDR   )   )   `    	pHYs        sRGB    gAMA  a  ,IDATxX;hTA+$Zd-d;1`!*XO%HbaM\nD(""6vj!hg!"X7Dx={ٙ&̝s}ofvQl̼
vp
RTg \KU>)KO?'p!y -U%8Я>d\$ @=PoDgb0AuݦB|
@r]&jCq@~iktH~PoCh.A~u%?Рz?đH!|D?5i׆[T0T'pkY0E1;8\h"O v_3FS=n&?f@8S:Ɯ0gO9؞5`pLS3D?^fR#>_ G\f?icWa{.hϱYbpfa$Q/jq4b2ZL?g%isp?/VLw$-6~#_9d\1qz8,8=wbеcz% k*%`@WLEOS
Pz@z^TQn\
IBM	=J2
NeH2Z[V;'WЬtr-yǵNѯNZLqSdbFE{JxЬqG1j|ѿcETC
ptUK;{پ:kh=\])15gȿCCQZ 7OR8x4:/]L    IENDB`PK     ݀W               CombatBlockEN/oxide/plugins/PK    hjX
qe'    *   CombatBlockEN/oxide/plugins/CombatBlock.csn3Ewiv:(
0R(]srݙ ^Do!M7bf^Nhm۝ns4iY^Ck9-Nz--nX(<ʤU}˷;`zк-6s4;k=muVmrnkjוY{m7<]oCi'%ɺ	O<aHn$)> s|`+-:>]w'nzłQPb5
cŪXԃA˭zfT`1 !fO«?HZug	Kvb:~^麭}>f5O_geǃk n;')Ldف؆>B;m(](^k;\;lFr)@.u
/x14 w ~^HI2JR ^؝zP;NZ=wxPb-
ֺN
BPb:V(؊-
-CQl ?'ĵYM7gM-PYNhCJTѴ[3 Ph&&+^kam}Z`W\.AX ~|'9%	QQ֪j]HX,D/S
$?J&4YL4e^K8d/v[[7pZa܀VOڼ A`~?aN&u}v0P|kb[ޮcOoq»V)Vn
KVc!9>p|~$&(*<9#8k	UpjDpST&E 7S.I@S`K
%I1\&P=UQt&ҫ9<9pfRVcC&/qņ%;O>)u!&iΨuntsvt[L]*l吒%E% 'E|St5r04SF65Ya{ebuhWװER&
(AFgXVͶȘ|ۜ<lJHV;Dm4QmhaIu]`T2@B0.fYbX_݆~7$9mbtV
W"|RJdُ,V^z	 k(l[>bڏz;"8Cm{ԑJ+SB1SV4XM!YQSF:^m9KIU_XXo}{$$XꀼՓuKX;X$iFfCIpiBn7f7Sb(g0h`Dج:AH|4W}i2^D2@ǬuЅi0ob
C2Ӵ5,
wOS+|b3SޯoNъ*",pL$n`Q>'kn`&8f'GN`qEeb3F+fװS'>=A'go_/@g;Ao cͦ/Toz_6gQ(02gb]<Pݱ,||~%.~-R
9DSv{W/y8* l]hk0@3B^l*Rg1i|AKMo7춹Ⴛ^DC8Zv	Μ}#
!`Ojl@ç᝴;6OQ$ىY zص8/Py _ u7RO(_!t{?ܟWI^~ġg/8:wh
422ƀy{f	"Td)`R^>	o<Mϭ9!P1~#\r5`w-`ea>ޒ=>1\D0-3K7wv˄#=z=6l>#)9N4Ƴť5"fsaw4e0+H%8Ng
C?7Xo1
sris|%t"7;M&`^}1"pA/d͗kt}ƌ2LrO<UأAӻ˙JU0똰{_!<,U;j&
=woyiL|ױM# {₣^xW+[
y|2e=%0IIy,K(o [KhIA6ҁ!hܺbX0!r(=A6诀sNfz88Q8\5l:^5ZJ 	X+s?}A=fŹ9 MNT=HZ2nPY:48LR~V@^#-G*[jevn=U
T@5hPa~~nf<*Ub堷2P 2j3
C>& \y!.áV
LT*safa _f4Ubs쁚N;pTfٟ+ʅ	}"^t!Inzx}dl[@y..,TgP&82#"6fs\5JpYGіohucz!czaczctesc <S
	3#诟Shė/I)TƓՉ-+by+2Ȕc%Ȩ@

jDH!1Qjδ` H~^yׄw:.{G#9FǤ
G	a
6-E׽ǄPώ(R
lB$HWWy]}t́%d%}#mrx<="5Qu90ȪA`nxjέ[8``SQt6pZD#$ BGߡ@;1n7?传VE,r}!dalj<;LLTIޜM֕ج4>p4`} 5򐥺X#nwG80w{?fU_h'c;\x
8=.]έMOhL_KY7>XX:u6m0!M2HpZ.0)&r<hߒcͩ|lMƽ GnunȌeV$VرLo.sWǠ;]>**~'5 I emjVr뻇	V!HdargAf<wP=C1>mH[sG7w&,Xso1k3/rD#qDDZrfP}݆ٸ
K,hrBtw Q& noGݨAG.NNM՝m@3)c?(bࠍGCoU^~z6
:/[>T_}/{n-uKꀢ;HQv뮽!@_[5iþ<\>07po-pRNa
T!SKdmH]HGNƢ[;QXwEٸe Sـyaw,ll޾[ۼs^

tev
P0P8HMM,fArӄ*vb>'gR
Rwq8>酵yChmٴVF֖w755?tآvUZE'2~X
-;l`-^mvǺ@71Emh):U9n׶Pvﮤ2(JOX-:me8|
Ty;Y[PB;+6/=v1)/ӽ:.Pōx
׭q,.%#N}ɓw=pR7~5B/mȐϬuQ{*Ձh4
ZZv~`}uc*M
UIW 0' IrN\T?{xf	HJ
·98RuKzij[rNV{8){Ax_ovAYd8=;r~#7rБa7rLRx5Z p4RvDu%2p8h{~bOQځĐhM#	6FY_LA|0%ɂDg(
㧠)6?"11	kGl,N2mNh
}JwV%hqߘI2W[
`	Z%.R0i'@YB)Lb|E=yD9~<uu1)Q/;1h *O ¿A" Lp^c^;u=pxkTvYN81 Q8ʔ]@vs|;8Y3 *$s*c
Y8
݉
wRI;-vu2|I)dV;̴8PV-2d?t"_>AS6uZx!3\&vQ̆SI~l3)6leOqN`RcǬLmGϢU'㹒Zt3W1~+jMpvӞ;$W{4hvND#>c,jzV"T8=˭&HY抈ΞTNHܡ?y^8Eϑ$V):ePkI4(7-I]q~4[F˜d1Pky
7Tѱ 'NSLe9HM_b.LŨ?`-_bKLwkiGZ{M̀Xyn)%I	9|9l]Sqen`qv0\Q4D"`CS& )Xk76XV	{<)~Å=r58sS9D	QD&DC6LR b
(hX!2z=CU`DL4CՏ}Z
2mI @1ɢZJVV;JM 
YJJ48548΂!a2Bocb6+Ղ+&h)mx{`Tkic`2Uqᑎ 	dh\ `%*O6`hDkBp4pB$re^ah}359_3qpK֣ے8堳w|/]viI#R"jN67TpDZ}~JF	Da?f7+(x)kM0ūL&ܝ%U1YAQX7=:jG&1YJ
v ۂkb;pZ.<D
7!/]K0I^ͨv>a"faw`M @	~F7
kS:&k"vl2_d6Ϲ0Rrta;ݡѕX`Kw肹g
żDګHC YTLe'<{"k<+DF@Dk$u)PHb2z)3r,cj:+ x&bYG`,E7JdEiI	@l:%Ѹh2X4^	62g~iJ&&@s|0&ʁ91]+G)
z@d^tߕ{1D72rJ.=nnXw9Fe"4H+rr3D&Hh@F@'<@j-<[>094-QB "?aǼp<8<Of7qa9lWS
k}{gVWs@q#*|i3J3*EDkwƼ
Mb8Y߰7?8q;'2,tu#X|A<)rP@C6E/_㶿i?[;
w]Z-PMn7ˍ i.@5jr)V
Όic#/z)^OzXa;ӛ{D(9cY,]ԑyh#,@:.\;`Dn|n,W-@D`<Rn9h&-(^BaL)"Y|U҅|:?SOs2rZʑ$sEz
wBJHʱр VwLEm'<ŧRRYpWnEYaf9h\(݈$X'҄KI!*qݔ
_FC>B:{nIX"x$Xf9OVRs+8O<1a>.spu JŨ)p
GheiR<lagEo"ܼG+x^|K`bfQsnP<cedﮎY1x8ɉoI`NhKXyK}HciJK[Z3G? EOyW.o)@s><"Jt	Ftџ4̸3h"b2}b8Hlx 
w$qD"J<]~̣IAtN	(mQ	DDI{Iݶq9/dax
%(A&}M̥eP&N"^t'aj)NƦ	H[6\gScvxS-z3byO0#++aҩ!O?g +`bX"~HA}$~c2h!&e^35+ qz) -Aygi%o):$lSXĬ,~ޝkY1m Cibz)mlNhG`mZRlcQ,Wm&4>7|^a7ȽFɐ1_)AO/S&CepL$iFcp|r-By)jCdEhZPmk0vl#bZU'g1Ekn:R
Lk2_>xȇfMwp vZN}-vMQj}ho"XwFmS,1)XΫ(ޣJެUERr}i^Je.wן[cOk;1ݽ][sɘ9l$XeZŵ2kUYQxbt)Đ] +h?KCbX]90>LTObA0R祉M|.Ec߮V\5N5'_>lcً0RG<ca [G}wW㢝1AkhkoZ/Oms"Ƀ!
 TLhF\FrHjy4ۋ?*S#ɢ,N[;1\ynn,jY,0gý_o
\Y
SnI7#]^;*u:^ .X\	UgJ9滿@
@>as`4OS4] q.
]r;VlÉq8c:cvc$ok# 3JjhݼoZuGDg#"9 !fmaZg 3ctֿ18;*xw:leފmhwş[Nƒtq{o+c
Wԟ۵[nZs}06Ǟ,/he5
%@{e'./+u7BL%fGϱRo>o@%8tECP;v,X<E@``ipnAb5AF1@f]=0I7'SBQWf?k (L_JiMg LLy	v.V)4VPuhnü姂Qk-]RL.,{V&`&ZJj*D\%$	3(ܘBg=eb0ZdIp#Wˈ5A/N7:b(hTae~D-cCBC<C&xI@5$1K"KPK^^נx;}Ki·]ˤI} G}J-S4|<&w<V$U3U/b_tns5+#8ʩtgu7r'1x
P%`3bMES-KZމn3Ad#upbN]v;1!W>'+bʆ7}&k9x߉`M9~ttwzsKu7(r+|v69{?`4g1pZCȌ9GtѷNAF<r&!F\ЭxkH=Ds8Y7o&o/#2]Wɟ̌$ul
kIҢGsmb:`, *.~P^G헔K$1=Eo7wX&pmi@F:~'mXAE|lu &g.oV0tj:V&춠Єьbo5 ]s tDJׅM^ަ5{
Rꫨ:X( iSp#dY뾯lvBPkOQOɄ:<P0OQ2b](ss<҅Sڎ±clXSƩ"PNۣK0nmSbe F-V-*7~1-*i6Bv 
sBV?kp3:0jaUkΓ<Yȫ
2)B[z#M=:wR<8ґr`x镳<;
w}t꤅;9#X$.E:]լ8p}OĹJ;z"=Lfv=;ۆrdd8]l<R
7Zb0̄k0cYB4]>%ȺqI3	ʩtQߐM>jPx[&ţ|NN^^K/yӄ y0C0vAǃe"%BP{N4uR,:x(Yh)anU:,$K^v|,6/a]}ot2:PbsrhEp*ve(lEA^A^,R$ƿ3Llq8J$E|7.dXo,@:̩mp1@;yR(
u	
{ ,qc1zdc2$%Kۂ %oY8o"{R@QX^B !`D[}aDμ0ˉ/ *PkӁo4
:Na Fk]ߟv:p3mo;wÝ8E^üF
Wa`nǵu+{HPK?      phjX             $              CombatBlockEN/
         r                PK?      րW             $          ,   CombatBlockEN/oxide/
         |(%                PK?      ̀W             $          ^   CombatBlockEN/oxide/data/
         dȼ(%                PK?      рW            % $             CombatBlockEN/oxide/data/CombatBlock/
         :(%                PK?      /zW            , $             CombatBlockEN/oxide/data/CombatBlock/Images/
         9gt                 PK?     WW3+  $  7 $           "  CombatBlockEN/oxide/data/CombatBlock/Images/CB_FON0.png
         +\DP7                PK? 
     WW    7 $           O  CombatBlockEN/oxide/data/CombatBlock/Images/CB_FON1.png
         +\DP7                PK? 
     WWEly  y  7 $           c  CombatBlockEN/oxide/data/CombatBlock/Images/CB_FON2.png
         ѡDP7                PK? 
     WW߉?5  5  @ $           1"  CombatBlockEN/oxide/data/CombatBlock/Images/CB_VARIANT0_ICON.png
         ºDP7                PK? 
     WWTPl    @ $           #  CombatBlockEN/oxide/data/CombatBlock/Images/CB_VARIANT1_ICON.png
         DP7                PK? 
     WW,  ,  D $           %  CombatBlockEN/oxide/data/CombatBlock/Images/CB_VARIANT1_ICON_FON.png
         eDP7                PK? 
     WWts+    @ $           Z'  CombatBlockEN/oxide/data/CombatBlock/Images/CB_VARIANT2_ICON.png
         HDP7                PK?      ݀W             $          O+  CombatBlockEN/oxide/plugins/
         8(%                PK?     hjX
qe'    * $           +  CombatBlockEN/oxide/plugins/CombatBlock.cs
         'r                PK        6S                                                                                                                                                                                                                                                                                                                                                                                                                                                                  = hideVersion ? Msg(player, UnHideBtn) : Msg(player, HideBtn),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 15,
						Color = _config.UI.Colors.Color4.Get
					},
					Button =
					{
						Color = "0 0 0 0",
						Command = "UI_SkipNight hide"
					}
				}, Layer);

				if (_config.UI.CloseBtn.Enabled)
					container.Add(new CuiButton
					{
						RectTransform =
						{
							AnchorMin = _config.UI.CloseBtn.AnchorMin,
							AnchorMax = _config.UI.CloseBtn.AnchorMax,
							OffsetMin = _config.UI.CloseBtn.OffsetMin,
							OffsetMax = _config.UI.CloseBtn.OffsetMax
						},
						Text =
						{
							Text = Msg(player, CloseBtn),
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-regular.ttf",
							FontSize = 15,
							Color = _config.UI.Colors.Color4.Get
						},
						Button =
						{
							Color = "0 0 0 0",
							Command = "UI_SkipNight close"
						}
					}, Layer);

				CuiHelper.DestroyUi(player, Layer);
				CuiHelper.APK
     ՈW               oxide/PK
     ̈W               oxide/data/PK
     ЈW               oxide/data/CombatBlock/PK
     .zW               oxide/data/CombatBlock/Images/PK
     ܈W               oxide/plugins/PK
     VWEly  y  )   oxide/data/CombatBlock/Images/CB_FON2.pngPNG

   
IHDR      8   ʷ   	pHYs        sRGB    gAMA  a  IDATx]ٮ4ӵ @ $xoK<o@ <nY9WTNz]RwǱ*WwkWҕt+]JWSo>壏>z^[~e?߻woO=Ԃ#=O<Dy_/kYǖoߗp|+goXѲe1,hN[qv\ŸQ>QE[9If;gp7
:l+gLޯ>gx#ewǃ#=j|Wm
bAmcԺwc^/7#=2pUP>3Ixq/O?L]֕V&`,WFy $(choѶsa]5(Tl(d8kl5@=6sm,9b1՛rɳilQg81p蝍	I=7oYd2'W{Ĵjt+l#\n|[ٳ>;?s=aǏGެ<z^!0U>\C6D[u:>SQ,ӱVW_ΧwI9gYn8igY}KhmD_|vo_{饗㯿 ۮ+eI`mPj 

׋Y8˙hW
lmF=l\ڴ̰s7,>ˣ9j+Cvʼ2iwA)g
 ׉]s)?~!s{ele0Pn>L)q1SLi:lug| èPHE;ML̜`MH+أO~Y?鷹Y2=Z9nFQ
L9
MEl1B(1J1:o+`o}]T_~` hE
ºG!!ӯBřv^w ,!\۵qȼV\,?\y֙"9;bI(h׻ͮBxiweG|TӰO|#ɶP}z\*lh益EL{oy  A{$fA
 ]1ˠA=GFQSB]hs{jF:j-a!e;Ax-BS~ΰ)w7mC#?8;m[44Xwd8Khg7|[A!4
D1
B,B !&Qm#6͢J\R:ȅnŚ:d8
32VeÙLW4n&z
Gہro
$+?-6IBBWjX cU
}xhFA0;2=V`zиrq.R$l'p 1f	Ks	N4 Ȯ935Zw>i}>΀&ff?Q-Mqh<x`ցw"A?($$shbCf۽j9E_1ǡn]=nЦ?9e^Gm%o270cb͇'yyIcAN8w	MavgmSE4?Wf+@O&4ՋA\@&QmBAPA
f@x
[?2͚#0/lHx!>OoኝlݍImIh^{vN
"S o7x ]D|ۀ пˉ?!Zcݪ- H%\[liYK@΢XH"ڇ(W!vT"Py,֞80Ffv&!ciaĲG(dDtcn,Uw"O? :_1kITRQD69LqQBUpc|Rdx4)K^ݸtU͓m%t
Wh}NhXvGbyn$ʶj0E.]7rq9P 鳮ApM& +)ߦ+f2*3[e&]H;	D&Б8S3Iuwjp
9ZGLbAH|0Nӽ{(b!L
дMV@bSs=ZQx@<ͅJ}ZA]	U3)i%I.&!(|>|B+홭L[fF{zbMP4MbX R@{@4RbJsP{ >GrWSj'-B/i%̲LsO֜i]Aģ&u7=FDG͒#7ήZg*S(:CPl8SBvf%n_*H'!P3D
"H栐KW/-EH$`gZJ	z)ybOX ʷ\->3C3rGS&!h=6h.f[T[1gԜUJ㪄]:},6څ	,XbBDX  

HqqE+ӻ5R`hڦSGQȄ=	Μ4-3hq#-&qvuQ 3i:>mvABW_}ʐa0 {Vi8´"8׆L0)g2>@捘[JdrJǙ}`V^UeePӔ@b1a9hTռ[wCh(T
dE%@sPP4&1\IA.@bxx`.ޮ@ZgEd<Уp[j%V6Ѥ
cA!q^9ãJQ9pɳӑEc]w}E܂ <Y]^rEh7'<.°XUD"5%~1]yq	grt%o}6h<69tmw8,Qx\pmi]S|"sXK;h"b/o`
AhbA9Tk{,77e )P*\.,#b7-
gSVdXCM41:OyҔi#yӤ1u^	[SK9Y;q_E%)W<3<4p2Th!AdE6qٽ'h*gm)Eq#!wq{	LZxc]+J阒J5Y-]ǽҁAIg;Z+
ijC{ sU!>4̮J%<')H ?Ya7gt5<Џɺ9.mt^h'Eɣ6#1TGGyCPI{AEd2r9zzSV̵.D0(p@0(tbɷ
NTS,Ŧ5IRVxG!թ*L/NgfVޖz^C̹dvumLo	g{R.$+*HGM,txyC!y6/p\JU$h>,D#vw/V?ڭ4Tj̉Ahf|U$=ig1fXm4F:Tnb̫i})2LEJ"HWB8d`^,zI;|-C9zI?KIhf o\&fY}$bS2~s
+AKa)nݢxiLt}	 q FȡA p2h:SK(LZT3KM"&+Tv	zzzm:1Ƹ>>b`,,0[FԍOvRã
1u{I~({LgQa&ɯ=b¾XLw
|7أ{T_`Ben^O{vN\VŢztmdInV)֠=]OT4vʖq4G?PHCre6Ӈv}i}va3!avB	 w}w駟^xax6/4,n6?&w6gU[0pꞮ*Dm^v{㤛=I>-
Si7&4̋HL-QD5|K<WѨއh$'GL H1a6n 0Ǹټn,SsѶ?G]G[<ar#4wp)T($[J/3fnT>zizxywp<<.VTas0p>'obGrb6/RJs{*
CHaP!OS^,ic
8`hP& WddtIA0<d؊Bm)xeGac1]}w$s/+
-A kyڴ@g֣\0rWLAkIVjnU
M1bpAaO֜M{o8c) x*֢Sf"kQTF{sf^e0mGHcND6q'4|Ybn^0믿> ר(Ķ?WiANwܼڷ Eq
-ÿ"iC\@7q ~/Qwrn4]BhJϔ(|cޮ&}$9nTzO:
5{9
NHŭG&]17! Xg6&-jz	5	(A4Emue/S$$ɣuol8:#EiM
 ;̈́d9Ofv/G
kҵ;?
~E>	k/yX! 8
a0 vu޼$`yh:}#W?="I5ȴyqY$yYne.[>pFR'#(t?Ż&9o'X;L!8duV{wm}^z4"O5AG;%AY(i<t*p
\*8f6YQb=RfKg^FM#j)vOQÙkt6#t$#^"S`tH4
\rYL}sM:xA~-Pw qt2i@ϙFBKB,xjGtG-n
01EmJ0,kIj3ƨ&pP=zzN]ߊ-srFR9'LU
eq-w!YΡxN22C5rX/%U+UmvS{G	m"`S2nq+']㿥(vD
\Uϙj~P-,0ǳSY0&DLh
$jé69E@(t+?IOfɒw?ܬ2c.ņpFS&ebnB+}ީz;!Gj'`7^eɿ nIYp6/RN͋?@&z@HV4ł&_`	A0gx\$aDu1F--~JD ;)(.<*~hZ +c{-Ib7HqaΘX7x#APdEhntwxޅsPL-!ɥ 3Y}j
%jnuHp3fy7K퐘$cԺs潚Ie-MD)کjvl16ΦcA0"~`^"4 XbW]@Rp	 cU+
ˣH.xV1hk5g%oJ׊>t؁oFiSDຌi[r`+=(ﭵO
cX(k5:rlcE'u'X%U@;b!\?y;IQM._=7+Sy{&
B:捳ge;;q#cS'\T5Ϯ|? :OHƕ0yݫ1Ԓ$$FBDA+LVt~HݸZU^FмszDlG1tYQs\^iy*ei6GS2ن㮘88Lp
?
뙋;i*s@'+B$}\u]QH7/L,h?4%7&X@B(T-QuReN$/ L8읢ZORp(i^7N6As1gboX"-kP#כ7/?$ht]r55폓G~Dˎfz?f>5IfM[BbK`bt%>fCALsr-kb	!eGozO1D~=󱮛_"#|68}z= t0HX/g|,sTb<iOA<ׅblyza3If6Tnv(TBBnZ}oMz~K?};&B,?twa㚂r"GKWε4_{nJ@%oאbQ\]8I4=҂s{2{9Hl|v4}\y2
A	qeOx"~o}f?q^,h0Ȫ)X&si]EWZdǑw
AyF>ݻ\[ϑKAݡά*g[cvؘGŵEHC#_wj)=ݷ6T]]0;iq(ҕt+]JWl
/ n:    IENDB`PK
     VW߉?5  5  2   oxide/data/CombatBlock/Images/CB_VARIANT0_ICON.pngPNG

   
IHDR         w&   	pHYs        sRGB    gAMA  a   IDATx
0
:%%%ހ	RЁ̻L\ȓ>9|>_aP1)]gdtx6sLA*0껨IB`,}Duq<@r ŗC7)Zu#޼']kKvsx;~_N?62 v'    IENDB`PK
     VWTPl    2   oxide/data/CombatBlock/Images/CB_VARIANT1_ICON.pngPNG

   
IHDR         Z   	pHYs        sRGB    gAMA  a  ?IDATxTJA
y4+V 9,N@ *B|K`nn|y +pz tg*8oG0+͇RDֈxYx]w1f%*a6U,,`Bc\n80Iğ7xIb*x.|.
&x;"KW<XĪ_1!
N
3y!#NiCт&
Z~GUdfe˙pν4.)(F_lRy $r;97;+]hytL?eI    IENDB`PK    VW+  ,  6   oxide/data/CombatBlock/Images/CB_VARIANT1_ICON_FON.pngsb``p	@,$ugGd1032̚RXİK %בac?DV cH۩D\/z`y#5|];S}3oYyYtϺilaxy大mlkm;Z$igdĴ>,r/tNL]dä^YCnQs=a<)뿝euF~mNK|';?z{kCk~/:& PK
     VWts+    2   oxide/data/CombatBlock/Images/CB_VARIANT2_ICON.pngPNG

   
IHDR   )   )   `    	pHYs        sRGB    gAMA  a  ,IDATxX;hTA+$Zd-d;1`!*XO%HbaM\nD(""6vj!hg!"X7Dx={ٙ&̝s}ofvQl̼
vp
RTg \KU>)KO?'p!y -U%8Я>d\$ @=PoDgb0AuݦB|
@r]&jCq@~iktH~PoCh.A~u%?Рz?đH!|D?5i׆[T0T'pkY0E1;8\h"O v_3FS=n&?f@8S:Ɯ0gO9؞5`pLS3D?^fR#>_ G\f?icWa{.hϱYbpfa$Q/jq4b2ZL?g%isp?/VLw$-6~#_9d\1qz8,8=wbеcz% k*%`@WLEOS
Pz@z^TQn\
IBM	=J2
NeH2Z[V;'WЬtr-yǵNѯNZLqSdbFE{JxЬqG1j|ѿcETC
ptUK;{پ:kh=\])15gȿCCQZ 7OR8x4:/]L    IENDB`PK    VW3+  $  )   oxide/data/CombatBlock/Images/CB_FON0.pngsb``p	қؕ
H2޿HqxD30p0#ì9  w'ud^9,鎾$vz8T0q1WQbAULB.9`b:uCMl˘[?W~w߮X/Ã.ގwfvŬ5==gsPlӟV>۶U1Y?o9s}_ҾvnX-lt,]*n}X3jw5gnsg[:X5Oo֝lU!c#><gsqe힭罖\gaR<ڹ~˰g=Pq=/kg_ƴ{U{͜e~|4dE3=b	~. PK    VW    )   oxide/data/CombatBlock/Images/CB_FON1.pngsb``p	'X
H7-;8<"Aa	 cqús2/tG_GYr!.!o\erq=,3cQ^8KrZ?qpd{+״t20<7:{WFۏǘc>wZY-$=9nms̂+_WX&yYX>]F75{2Ͼ]?sԽ
%rs,mMPq-GEYoxI!@Q` G(S#pO̙İ]MI:kMw?cǽW~YsOP]FoK'oS?NTl~ߨܿB`x3xsJh PK    K[Xl
'       oxide/plugins/CombatBlock.cs<koG_baFW,ꑘ҉ 8Miwf(Yqv'p.p>+~;aÞ_lĜ鮮N`{~57Fh{nп̷)Oo:Run=6-vN	zVN}-1W]6r֜Ξj>-fw\تjӵãeYNyUbd>Eɮd@Lk׸O@CЦ
7QتM(jMeR\Vda;:5z6om)sXڞ(; 
N{>ۃ"02a5
C(>!S`57d~Z؎|c<c5 :Ȓ͕Ghzʂv4 eFIG ]
ɮ9d{g E qvo\P !(6/(f1Mxd>@:EDuj[\er[ʤ4,A]+(EaL`X&4@Unx
&4TSq\4 bɻѤNJ4II^u&]}Ս{ O&(|'`Rs:5$QL_,KK1.~*
D)5.GpDVز9NR{,-%,; 8	nd=g(ޠb9FEB	i3{`nߤ.cYg󕽛BOXx:f^NGh6#!SBZ`)n1S25~kx,EvoJR6c)R6dH0&(HcŢ׆ZPQC)"TCadt99	%~Y%.
]OCǱbGdRJ//$d=tǶk-Ֆzi,84_lf5ű׬/-@H0$*J\`]	s!}Ȣ3Vgz { FZC^ᶃ0vèPf-u*۫&4>1yXQB \϶Hz@\]A=V&93l FB ыΓf`c])gd>#9P*	98FmZs)4UK-]x1kwF̧jwTJI:1a"?:>wL1~*Z(0@r;17kUf  v+;k+ϖw6kKV<۶IIz55%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!19 &1
Physics2DSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 3
  m_Gravity: {x: 0, y: -9.81}
  m_DefaultMaterial: {fileID: 0}
  m_VelocityIterations: 8
  m_PositionIterations: 3
  m_VelocityThreshold: 1
  m_MaxLinearCorrection: 0.2
  m_MaxAngularCorrection: 8
  m_MaxTranslationSpeed: 100
  m_MaxRotationSpeed: 360
  m_BaumgarteScale: 0.2
  m_BaumgarteTimeOfImpactScale: 0.75
  m_TimeToSleep: 0.5
  m_LinearSleepTolerance: 0.01
  m_AngularSleepTolerance: 2
  m_DefaultContactOffset: 0.01
  m_JobOptions:
    serializedVersion: 2
    useMultithreading: 0
    useConsistencySorting: 0
    m_InterpolationPosesPerJob: 100
    m_NewContactsPerJob: 30
    m_CollideContactsPerJob: 100
    m_ClearFlagsPerJob: 200
    m_ClearBodyForcesPerJob: 200
    m_SyncDiscreteFixturesPerJob: 50
    m_SyncContinuousFixturesPerJob: 50
    m_FindNearestContactsPerJob: 100
    m_UpdateTriggerContactsPerJob: 100
    m_IslandSolverCostThreshold: 100
    m_IslandSolverBodyCostScale: 1
    m_IslandSolverContactCostScale: 10
    m_IslandSolverJointCostScale: 10
    m_IslandSolverBodiesPerJob: 50
    m_IslandSolverContactsPerJob: 50
  m_AutoSimulation: 1
  m_QueriesHitTriggers: 1
  m_QueriesStartInColliders: 1
  m_CallbacksOnDisable: 1
  m_AutoSyncTransforms: 1
  m_AlwaysShowColliders: 0
  m_ShowColliderSleep: 1
  m_ShowColliderContacts: 0
  m_ShowColliderAABB: 0
  m_ContactArrowScale: 0.2
  m_ColliderAwakeColor: {r: 0.5686275, g: 0.95686275, b: 0.54509807, a: 0.7529412}
  m_ColliderAsleepColor: {r: 0.5686275, g: 0.95686275, b: 0.54509807, a: 0.36078432}
  m_ColliderContactColor: {r: 1, g: 0, b: 1, a: 0.6862745}
  m_ColliderAABBColor: {r: 1, g: 1, b: 0, a: 0.2509804}
  m_LayerCollisionMatrix: ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            7,\VO<o$"¶6v;m<?Ojkh{=!Jl}a>31%&Xv#/\Bz!9b#/(vqYĵӺZlzl[ضg'ǼAi|Y=|6.k	B5aݶƩ\Ak=RڵڙuֶN- >>ȃҔ
`V2fֹ"+?Ǎz1<r:v^+y1}j,N5LǺ#t1KG]5Y<~ACF
)=uk@>c>Wm;4!"{&y6F~'bc]zJcsD:\p5+k)++9
oRMEn!ۖ|ꈯ7ǀj\/5lDް^oT;;K~P??("*#Na\GF)bg<֗Uy0UTԕ>mĦT:Koh,PàQ\m l-ʞZ*6ܿzo˭jAhYg|	՝Yk_6z8yM5#c}+}u8~/
{p'2xâ˶Ԍy<T_l'sY,gاSs %!ܼ"Ca}ֈkOĵClb_3»'Ϭays
w,<[YxJᱵq`p|[ko۬1?^n uZg`FYmVmn˶p鵡oņzrmY8{<B3ퟮ	̺\Kc9qlf+og ?vO.J+,{?lwh8'ߚZ_ߠiܓˬynM\o\e܋$Cxi~{<~>NyT>_k~vzx-\aDa
eT@SП.u~uh_~55UșƱ(hf#k
NY)˶{,1Fȟ#\|&Y;z6۶͹紹ƸSB""K7Z:mD&vw^:<m8<֧ۜ3#Oܶ3{>kQQ/sፘHy/QG\0
z$hܙ
O@%)z|?smen144y餈+`X"-G;ֹx(-ɆLfkyf(okR®T\QS5cK*E͛{c'YQ0^./?en|1/l\c2;K`Om^쉈ɀKd%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!310 &1
UnityConnectSettings:
  m_ObjectHideFlags: 0
  m_Enabled: 0
  m_TestMode: 0
  m_TestEventUrl: 
  m_TestConfigUrl: 
  m_TestInitMode: 0
  CrashReportingSettings:
    m_EventUrl: https://perf-events.cloud.unity3d.com/api/events/crashes
    m_NativeEventUrl: https://perf-events.cloud.unity3d.com/symbolicate
    m_Enabled: 0
    m_CaptureEditorExceptions: 1
  UnityPurchasingSettings:
    m_Enabled: 0
    m_TestMode: 0
  UnityAnalyticsSettings:
    m_Enabled: 0
    m_InitializeOnStartup: 1
    m_TestMode: 0
    m_TestEventUrl: 
    m_TestConfigUrl: 
  UnityAdsSettings:
    m_Enabled: 0
    m_InitializeOnStartup: 1
    m_TestMode: 0
    m_IosGameId: 
    m_AndroidGameId: 
    m_GameIds: {}
    m_GameId: 
  PerformanceReportingSettings:
    m_Enabled: 0
                                                                                                                                                                                  >ckK`SHQ u'B̜9Ή2_UEBC嚯1^
f4<lu{XFPc|Gem}ֶbm$owݡnC_4\gܸ b,o[pL4?Om.9G.lF
-KInks
;+;yaLYYW8m;Koᇜ^nP;̉'iԄs` ğD8vGǋ9^5o?̟>Ɠc^WIjǆqy8w%NDxqNԦB'jwÉrod7A9{)_xWD/?u9?\$|RVKPYso^~g.b~?m31t$>msB_Ã7\s?mo3Tp'eZy{
a77?ePqrOYJw~ͼOX=km3u?SM.qGуmi(μ'l֟R/]9'*wAyX|ň0e:ТXzEn8<+(CyE~.ɓw<yέ3sY`n@|ܜlrczb#Opf.:>Z˃釰I
qggBIOB/hǹO-mb':T9-
DזyV=3YgT>uq<Xu%.bOęjøQ| <l-W'	pZVeY0)K<pʧ\day)+m7}
-X=E&Z>:È|;4BxV"Tq*B\O8OŖxm
6p/nbYU\duC1ci8Sa?f!<*>S.⛢SYnn^Oq'
ܿW>6UuZqşŉU<%P-w,
3&׸YTU݈[xνvf56ϱc:Ûd}{q$Q!|pb8<6+*sӬ{Zvwg1o/S'p3Ƴ6D<7QøykcU|iɜG=ҏ&Z^:3'xGU\lK$%K!Kve*AiFT7%S`w5[jV-Y` j5Vԇ"uܻI-Oޟz}ͽs̜9sΙsΜsS՗P$6sQk}v/C,]xo,S/k<Gi
itSm.KPWV)ԥ7yä/_Ν//amsq'+z[ini% 
&|˞%*(A[`cRO4/85Ρ(kGO	e3}w(8~Lj|R}1^Pj2}oMTjWzy_:0}޺<.^o2x_"4^`'I5Lwd

̇nɼݒyק`%{oo|^(Qx&mOuaH;#cV2НcR_p䶅cʝ-Tܢ4em|txE`]tV3dJ}I!kݵ9bBy=#^`6!aj털C1"?ƳcaN*q<?Qq<Yٮ5+<(/Yե**VlW)c]N!)kq";rdZ=/0
RB5Nb䆪zF5Alt+M\Q$dfCE}>q]Ip(r4|>}Z)#!{h$e`/{8mw{#F}y$=y3Ox4{KI8k7sbeqk><QfUNX2N	Kχ6.c#'Q>N>A{PGOxg~͕O4ixgN4U?)MGv^!'exc4Z~4.<*7hܫh&݂%x<իδ讙vTA(U{	^y{*&Z0Kb,2fT5S{V^A
*tdU3ܫz]1_$ZLWLo9,ko6U^ISg;r&xTݮX"ʫRT>i%eTz8yZjU| z.B.{jqǆ[?"(VϣGa`s`88[hhlx
Ӄ1N۱V<N}MMhL	4C_BS.>Ghtqq B͓cN
<p).n-waB;'q\@GğJ=@Γ]4WLz퇳~`?$A:k惢-SP59rslQӨߒ<yG1lBZr{V(Jmq+\1.)S`*.SF7J״cd|$<՝튅^"CIUKah]gV G_o.{?5ֹI{[<&kPҿ;(Yf<_X,s^vzU+i"㙚4)޻auw>"u%{$y7""auC4VKao)Ը0nA1nA~p҃)?p}u%xƾk0Vvd﻿
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.31105.61
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Assembly-CSharp-Editor", "Assembly-CSharp-Editor.csproj", "{F55B3A3D-4265-141B-7CE3-C3E92208253D}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{F55B3A3D-4265-141B-7CE3-C3E92208253D}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{F55B3A3D-4265-141B-7CE3-C3E92208253D}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{F55B3A3D-4265-141B-7CE3-C3E92208253D}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{F55B3A3D-4265-141B-7CE3-C3E92208253D}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {F1E2E7A8-EB3D-4093-9291-CBD546BDFCEE}
	EndGlobalSection
EndGlobal
                                                                                                                                                                                                                                                                                                                                                                                                    ov1w\ls\x6~49cVm
/!aSdm1dI?t)m$79m	@\90]1};aMOiqap.y-g/_`}r"?Q34_N"ڋ00.sKEF7tUA_ c<l~HբEo(3#E/fY0*~E`
B_uq[~'FA7$aL]kn1/DiMS,n^H
X(|35IFۻ(aܪb5*B;04dRt oWiT|=o"xu-97OMZFh#)&5y̑yyDl]K:xq*8G|I~BxBY n}c^0"	RFW;bޒ~.IvmvPkѯ	GCU'ǻRϼސ)]toʴ8}cGP ^!&]RzN&BhKbƋ(m-|N #`PPcld5>c%O͢|C^gLLuOX3=SKK#YΚF}QkϦ%J13*j--^^aYA92m9	d]H94:j$瀠"FG+Ȥ{_6|r'g/FO5(+^GՑ`_msZ6|=4Ev]4Z<Zj'4Z0c,qK@h؄rVp euL%P>͡hucZ
>iJ5~	#˿1J8bX̦,-;T0Zm[ǂszϵσjP I7֣ C-C3OX)1]5^VcBH8_=ϫԅdOE(}9	̎5ݸ՝a5~"3|zϲ'XxGr"x(/nFt߼Њ񛻌?($$$=3ￍԘ5OWGSqSD/|{pu¡2+`U{i+2
R~L=Kݔ_v-ht}	nax'M x~/<t		(;44M}G߾?_qyno?|88ߖ_rsck1ck1wتck\
m;	ho{*A-=ۂQuiAu(eI{)N/r^TwKǿsJ}/˷a{Fڒ^>VGd}
8,>CvgrJT0#_n\9lSȪI{D;=:[C~#ûW-V-{i _oS˥pyDfbvKă%Rxe lDMesa ŁA9卓@&O+A8,W4n92fz[]ʝ^D??
̫UUoR\=(ؗvNaU"dU;#56Nr6*0CMZhsb
<B|Fb35^gXҔ3mn"$QfXAs!V)ᗰvTK64U`ۛm^=%$`mΌ(ۢ9h7O#Fc39O-Qj{TmC5|)yD8~'M
riyl_Fvn;>7d~$D3TAvY);ưrXN{&إjɱ	c#h0<!]7el+<]TvM٬>Z6&=2ӫ[jO}:-L8W0qxE^ϼ;K1E)嶵N0~BQџkl~ue3.|t~tW,kᇏiuI%YוY0櫛|Ϟ7U2lW	BL2ouD$YvviV~C _}<EDVE0ʲܫ1Z8z&j_ӟ\?FK#YQ #O$Gǔ!.	y6V1dG
eR
蘱	9T{edoȣ4ȴ!!XmLR{-嶋v1G[٥}G1_!K٥$6˗V_D(0rծ"Q#ĴU55;֚c
4uV8V{1
J0䈌~Z17
8wj1dC%4Z:*謸>yy)vs;6rb|є\KL%&kPNG

   
IHDR   2   2   ?   sRGB    gAMA  a   	pHYs    od  IDAThCZy\U~/"ʞ &n
3(JfΘ5b5g:Knↀ

^VeW@9~.<9z΅W S_


|-[7nnYq3Vƭ[Kd2_/L	a,lԴ6BJYuuՑ%YXXKNծ*;w,~߻-	??M	멩ݎ8vEEETV^A>*z	=}}ԔLMLȼU+$`gOÆۻw&/V&U1Mˇ%$9z)2ݹsJKˉ c222RSjBK|nanN..ӓF{0l+ff]fDX S>̺{uw9{,ޡj2kْ Vvqv&_ozoƻ~=z_"EOh&Φ?z_LթTͦӊlN,ds MAߏGDА!O:98,{t  r^w ݇* 

ׯSFf&	
F]v}Ĺ&Z+;;
IMELfV%
,+/kl߹=z$YD{I'R؈dgk+LmǮ#ǏUk!WPci޽:	Av~(jY>q+7Nf]WZfȶ;c2gԆޡ!rR˰f-TRRRL͛7&#iݚ|})W0ݽwn˛Yo\.fjIsXn7|qMM
i5nee
 ] L8Kh忿fD+6@;XK-L05D<u"~޷!.Y?$`v쥬lSN,4rZKl	|+-AM64o\6B<9(}|kIc4!mYH`HmۖΙMۋkdY3gRn]ŵ!Q3C11`dd/Prq,#?ܴbawxchx.165@ÆQՕO{Gb2srr(jҲ|m|$9rإ˗<v
ƏN ժtqzZ*ma# ߟ$5wX>:r;ߚxHߚGɡˤFӆ]C 9V$UtJKA<4.bNSAOU	HMKN$"rM7KV4WK@-.)Q^)fY}2k44IΖZh!r%MzF8HD"8Ѯ.&9cФjK|\\rrs@AH7m3/fz'GGzQ	&Z~GqWI|j+uIڀIROT B܏fS'w/^"
x!iSyL5ܫNdړ߳=I#l@8Q 
;R]%⅖Y0cBq񂌺= Lҡ#G9
L
P^-++׫
DksE"gj Ѩi9mzY{y{uFa:<=:	yԯOQSBbR^.ϑ[UHg)ǫu	6Uc⸮I_y'
 %\(fM.(q`@]t8uA8۝wZٶc	M~gTlŞAX>J|/[.D/SM
|ʊ6o٢׼0y\@؎+/0zR0QM2EUH+=*?00?UGqqL،ɕ3!<($"VUUTv;)|Ym`̪;vb Z/AvO!PeXD6ui
䂼|3Q@G7[
K	s_7#O!An@fY1(66{q}d-B8fLB"ro h뗚FoO+֒F,,4C6EL%! HKTȡ+'o N&s}lnJRN @ *_,U18<6pJx̾"&˹];5]+
eM "ϋ䐘C=,qV4Z6lڀ4	D"ѩSA"wQGktQA 		*I@_//ں}(q㦲n@#>>8?.;߷ElcV텠6߀S}Oy\l.k%'l`` /ׂ4y}+Cd@3]HJfC!3_468P40	\58,ܓ4<ŠaưT}h˦ԩ0hW4k)M\QcLx8>Cq1S!B}0M@@,zNnVL}<O |hb8qGRR86rDVQ#{bbjrxrq10{ L6;Z= ә["f>C,А3q:P%7ƍ[2:kaP}:ibV_P(wHځu0ժrN~^?
fr`?;f4K]af;׷lB#!NFG$lE8I,,EO.ü9sjy6%hjdvbbYA+	\ a!\sl
ƗQk,Xbgk㗞!;;{	da+6>ލe6<{=V!W=+E	F2n6c˲@҂3-؆o7ѡGlZNWpoo7%J8/fANCTTTU}[:[.ohϞU?lm!pxT 6mɭcG@pŗ_={NA-YT֯O<v#Wv{Qp\QneFF216BX`a2ǆ:7~cM{=7_H+XA_ܽ#GMP B^ݺD%l5HO7Ӆsk"Bhv7x,q6z," ̇))#G-2QfPs!iȘ=sñ7g<<;<?xݮ={L33o	30Thꂂc<1k'Y2?5L?jwس781))̅KMZj 4ʦ0v躑a!,D'?AdnrrD5VsuTC{BK^̸/MPM7'c	aҿ9rCBg
k@_4b*    IENDB`                                                                                                                                                                                                                                                                                     PNG

   
IHDR   2   2   ?   sRGB    gAMA  a   	pHYs    od  iIDAThCZyPWCD@O@@0(rxFD!94UFK6[{+1F<6quc4IJxD4Qc57^r	
 {=43m1ݯw
4c$?<yXhQ*s<25
OL	GW滔:Vު65iZ[[Ç`@4ϠaÆ.˄Ư&WA.m#577SKK=x:::0ʊlm8<:4:*TKPS"s.<UUUQ}X $]}#aGΎhС4cZ/m7=&Xǟk_]GQeeMdii HG$H=ho($8~2t|>_i="$Y,{:f;N,-	U\]ÃFӔI+Dn.R(KA(,,$'Ӕ){KL["1M+?3jjDx{>1ɺ:
=FA'O)S(3+n{TPX(\L	<ʝ9,[Lf4(ᇆ7h[ҽ{5%<FPcc#]|ܽ+O2YӼW^'NPau1]\\̹sTQqKW
֎eo;wtLaõZ׮[tv>ٲ+ DaFEQeu5y}f0ATD_+ǀu&Ъw|1aa_ᓹe[[Z5	5n!ah-HnR`YS񍛔'}։g^"4~fFc>}&y=NHqr2 ы~:k4CQ V i(r|Ѐ!ǀ"P?A_§b.DX)wpS+|\M7.rrs a6oҲrqF.ÂPHhHPLF^,Kz	,П6~`X4Dal1 &~>yJ:"" c2KKK)uj|"]P[$q3hPbՀRnnp${!suˢj@AсStWt?jl>7\
Ʀ&԰l=^\,jbC.7w 2ZSCk\
:;~Ș+We_>kE8]^sN>cĨH=w R%e"N͌s5Sp!dk@3ƀMM"1a`ф"ଆdw_d/Y ]g	wyMwV"&r{mCCj8)M+8֖mT}8![&-[]q} BnUVq" aYǲn8'[$-AS]|/AAԏD}ä;$\>wqmҽ (|z"yaР5i
h?p<<&O9k^ s^;Ҿڼ>Y&M%%t@\]}p53-m8
+fɁ{ai-Gצ,W9ckd4@P^څQ8N"#ӧDfg28amߡ￠eXYQ!1Gگgε^W-̊hk?}&ZUxޏG2hN3ѼP4<wy,Zzc2q/'jH]QSSK85Cr["4"<\d,		+s3	D	h:t[7qn>DD[ZZaGTYV^.6@_oݪ'u
q`}p%s* 򼛛v;Nߊo
h =VHlHKZ*]j TƠ1H
IHw"k"3rN}'E|ڄ(܋;)1}RM,lGNa(J#ՀzJ\Rq窯 "Jiogg_@?E&Z!-R2O} :RcH@& #t
߅#sa'&z!
洬3a =2XNj]5Dr:9:%4 rjEE/.6c7<Ėz,qsPrhXaq5X$$8_L$ZW~ACrdf_3[ G.`W}XXVG eXavL^hLUm'W( wܱs+Vܹkp)ZtĞPY7~Xϯ[B.VrJ;uJo/o/G-Q}x-8A*^ꄀT޺uN	<sVb"c!

pS(,V @xAE,h2X`>~-)X~	ӧa66+kogLˬU[Yh
`mm 7*ah5V6*Vmˬ7"@ڤظ!AA+ &2xilDsE fI~'{Tp(^߀,Eȃ9If"7;f9|Sʢ5)`
@mX
x!i72 ̀ø[F30יŋ|*E@f똰l\L4k,ʀuFVIevePwQb8#
t!"aqYc#"D+ۜVc&DIgkؙTí(d`k3;q+%˸zliĸX`ޛn|=Dj:n&(.Wq%_ŲeK`z$߸yseXF	Y ,QA>w&V4 2hmx},M^I,.wY" ?pVII{o/O;ЈC:@1aхEEn%<jctQX~BT~2QtK`'nݶmԾYUTT"j2 I q'puv/P`="Kan^޴e+m.;AA:Г9T/ae[XXrp7D"b>e=&"_ZC=y~n]ęBJAGBԈY:f'l3(/fu_L`2'n/8z=}\Z ,ۙ4s	6sA׻*"2XPVlonnYM뚲2tɚ{t!wrru@@@gpp`g$)lۙL
?i23'a	?qt~C5i%    IENDB`                                                                                                                                                                                                                                                                                                            PNG

   
IHDR   2   2   ?   sRGB    gAMA  a   	pHYs    od  TIDAThCZiXTG@Q"
q
hh&L$&I1.,.ט8KpMT,.f&qTD@tЀ0Ta~3ߜ~:uoսU*l=͏9t=s48pqJtl"L?9|W
lKTUUC#[[[jںóϯ[9)Lh??7~7&0egwa>̬sT)
PMM
꒹Y5ّCrqnE>>>!pXǤG#AǴ%hgSiJJ4 ss3R".=]R席
/++&03n޼+;:|
,-L[Raw775^Zح.bmMeDIt`U˃ܑbQbձf5?@RPE#ƍC!ChQ"AϲWc.*Id:7f5+gg

C33˫B&CD^Ĥ7&'ݻw0kOլn)n;xD'FieoL{RN~~b<yx^qSSÛq^S!hW\z/2wY@0JOve؍	Н;w }B:CS!1R`js

1U$I
aCCWϞֶ-]/,PIc˖iiԩiTl*oM6LdTC-| fmvŷo	D[׮.Hjߞz)Q
AGi'Oǎ԰G^vJcy<"
/cvѴf
Jp
x'|Zf5

	rOq$YvK$`hcĂysm6R~` (x1|Pb<>:p֫-tD!p\%:͞5$)

H)p~ilѶ!9kl
}|@ڙt.eHΘ`sΜqcpmݚb^{Uaؒ/hmn]-Z4q_~e]jew.-hCq1'GgC`O޴3hRNS@ł<S%yO<BB"LCﳙ֥]"]>l ӥsg
ލ%M~/<쏊
RI8b݇
yjo}(%'ЍӞ=zH)RjSR$jkQ+tD`XZQtr@4/cHKOnH<r>=N!CQFfw%Ч!h}G]tSCD0C'uMD&OowJ	Zk:rH7P: xm/%k]z!-Y)S?nڹMq g!&޻^T_HS_֬n CL{GQC[l}?2*z(mpmJJig + G ͷ
$27nޔR!gfRËg
-ZJ66@_ I\N22aAFr/]"__ff]?EQqJl2Ot}(dxG-h~}*Aj?byWޞ5s7;X ]4M0;223 z-hl F]mP\yѿ$RB{v6zɭ}occ`6ĞǮ>`@S4DPu
})͇4vT"s)@;ϝOs/ۭ-Y'KC$t#눤qwP$9F4,1)N5SRa9!uO4~xn/?{O.7Wk?N02u3o.MuYѤ)6*KKK:|`}w]e!\ֻ$={a`صy˖1 6F]$r@yW[JAnb?WC5bM,_ZP#O	1`}D6!CA` l6l4(9
_O(3+Kē7mV~gcJV	NNVڜzDXD-Q1ou 5UJ5ThI='7D~zp]kuYwQ׸<= @CI*s$&T?uᡡ¢# <}@VjYXE"̰aCLk`V\	p!K>|qɓ" ƪwц1w$yBfϜYE.Br=,ꖒ#'͋+y
iq%>ٳE@yU]>"pdDX'DA=uGAA9qt[|}uxޭ. ޏ4%F) jI?M;M*/a'i.12p#G@"^[n3j/ 7E&yoLbT  Ș_\}%#ǏpEc B]v|כö"d#N%,,Я2&@*nE" 7qGRUnTd unǒH+aIKEzahx

'&oewߛD7!$:|к	_U0y$	n̒xq[ou&=],%fť͚v#W82y_JK5svvӖ-0fPTh	U3vLdz.zkkJ&,VVza-[U0rZ $U0"|DE+gg|˕4K'x>W***6U9.^*܉`_׎k]zuuuő|4Sz`RK.rk;t<oN7'|C
?mL    IENDB`                                                                                                                                                                                                                                                                                                                                 %r"&LF
>T	]dEn7og~}#].<e ףP:v|"򼰧w<~r616~l:ۺ6%WPKq#cm/?pqX?ra\TP:	R$yS$6bƴ؀st˦zĭ{@)y$ejXXuncyVga,\tnDAm3̵g%{t]5R߸]Hu-PEìg9#Ź+,Ͼv%đ>g$׿aRd&hJ=oPvA9Kݴ̗R>ĜP0yc&JRp럿>oƗϧ5VPE8hDLԼ24~TG(^ېrNoX_np>Op&uZdQc]GoJ!L!1HrSIGJYy^#1ԼPNG

   
IHDR   2   2   ?   sRGB    gAMA  a   	pHYs    od  IDAThCZy\i[%
)JTPm˘aduR:0|f 5DR!T*}u6-4{:<{{NoW_ZQ#+<yHK<BDh|}뱱˥eggѻwȬ+Wd;d{߻#	ɯ/]^ѣ1$''SjZ:effRVVPnn.%C##224$Ӳě,[Uervv~ߩc͛Ȥ^AP-e
9`)2%&&RJJ dhh@^ߋrdgW\jצݺ?Ա-E3_&'%=jmp'OQBB"eegI2d`` YWLի/_2\C3@*[!lWyф6hp|ʒ&Ħ2'$ĩӔͦSR?_rws#65z⅘1:!-)gCK:?fkmN/ӊ4JOX=b6:tAQƍ_iҔoG
<--+iCY/
-5mڄOYF ٣D#J$7bXl6[mW^k
@ɑN*V؛M	&}9Khroܠ֭Zj99NU/ʰ5IIKK[<(5);fAl@XG?4aɮjUs~C{*6h"膿Y(0M[槎n3(kboJ
5zQ|5Vp
7ӧO망iQk4}ɓ,C5r!h$v={
2ٜL4 TB@e8jǍM?:RS}hQB
"!__f@xbJa#{?zxݻ͒?
;lllL'}IgL'5|.Ұh%\DJ8 4.}&QX|"qzk ¥:vAH_M&fPr35UlmPO߯H]tVlf||<OIMǟ+E5ҝ؉zHtP4d@JKO3gytZ2:w+@Pa!d^b(l¡GxjjE|"PIfUFɡͤ #   {+yl˴$$#1jyd}.s~/
8g,K4nW"ZMJ|yD"="dӧє&Ga\
O(#Bq	/4n*uPla"=Z58cI@GޘH\Sݨ[.U}z@ys]HεjĿv5g;wsUaYh|5:ljjj]HTP3{{@A{CWI6b%Zd)vV	uY2sZ		]sFZ<]t~yA?
p_|8>J^;kɲUL2\©s-'!0K#`:h+xscd6 8+B+vázVh#uDԱe&XC.Ԣl+	BkO~\|yאַABpHx0v`-y]:wWQ66bFirx+W11),LCW" !nMV+Ծ@ ;{cU
g_5m+8CؚHm6GÀH@-aֆڷm+ Hg-PghM6Q<
zH~)-ͲyVV6}D;/JLJfM?R?*:Zg lrY1G!b >Fl>*] 
bQ
/_:od'M*U4PJ3!If)cOp!JC5?۾5)s"cQ0α6	-}j߮mu(YTb; Z(I53/:@iǌ)_ݮC8V^#	 	3fI˫
c9|(C""yiYWt%~hwp={Nj.ص[zu
!G
BW"#Ě]V-[ʱeMհO!B$־}}.L222u&L
ܾsW d~lNih
\"0^~f̞#>Eڈ@,@ɑ#Bqpp	5j8HhEg%pIZ߰B-x /]LGgW6M6@#x?FB븸TCD^Gu]\hŪ`ϱs	
$o޺%UtC v=;wGӾ"?Cm Y///,>F-&$2zKr|1RJkXC|Gs.M1C\lUl..C >+h7)B:o`W.5n:b
ݺ|*<{~i (1lЙP0k	M{I~wIK)~ߊj6O^M8ݴڵݿWbrD>@8LxL9or$|?GA.ϽgC!h}T{ÇI &&Mj;#ev
;T34B٣kz5HVp;޹u3\8-[%dc 8!˦4h@Zf5k9KHh⸱	%yUnziڵnV:*X *FA2EIjİaԨaC!0	]^
֗KΖTJG
GM>}z%Y
aǏ@NŵC>8Ӆ`JЀ8AC[H	BK-	 CSu_8m<=T!7WKyi_(Űr2!h_6fRU;G	wԉz/P۶9-q`8FX619BF%w7W:~|m֒cwA{JV/~hb_nYx(~,H+R:))/e@U\tG|!Z&9̌!tveA+KtnH̟3y ^*m\~n\֭rY袉;m.*4I8C@UxG#F>Q&Pȁ;`R[4oeZ^.6|>3,x:<o$#7bsi٢lUI? P4 af,D
Q/iNގKev쨿a AP?Yk~Z'aWW` KyMrwu_&DW0AFB>;fӉS>

pLN^#you (b_ww4ý̠
}}`ﰉfgD&04
_ee>q]
"""`aeV :֬ӻݻfVOypOhdffwXx~DDw>zP&"<:WyyM9OC@~TZ|`R&(7Nqz<oNgY{AkyL    IENDB`        PNG

   
IHDR   2   2   ?   sRGB    gAMA  a   	pHYs    od  
IDAThCZyXUe7ٔM@1@HE$&5LeQqRSiJq!mJQDp^dsr={՚?|{9{B+0>mmmc8H<yaΣG<N8fbbCa1Gĕ+n]j_ZRjWVQKKuBvvvýǷ&((*,4w&t?L	Lg_o~sn޼I*5ݾ}ꨱ1-,ܜlmlK.@n.:&6갨=<>aRw}ai=:MTU"T|t_hmmZjvEA{F*n4Fa,c^Y٭nXTRRJudmeEfffb5ݹcTcS EҜٳJ-_^ojfv0^l*Y]{>Al:6l&X}^y!>#@S0E8p =DGpsKs40H_0HIp]_M7o?,rm5,_.]u6Ree40&+72d0?Lf4EwSjjU>*~]9;.	jYS B.ݺѴOQO__ia@#H}i~W
V&KSGRZfg7S=='^ƼWL=FJhaǓ'MށL ]G22hQrr,St(lU55~1[lXP%-xW~lhbk_yW0^hd"I!h4-b%%] }}ɤv6][+V[	5sm۱>ߺM=wNH㞈^-OJ\ ]+Z`#]=y(ݝO_Jlǔ|H2npDI8>6QY
{{{*S
kd0РB6nؐ|N,A#`m?z"b:[=K&ʜY{eT]r#4Y@\PTd$
48GPbdq 髬䴥U)|U$kZN>=C&Htpΐ8I4p ;chp1gAlNgQQ; (W>S34W4h'Ms㊒5>՛
qqVn2
2}hTHA2];,^a|˖,tF]xo֙lQbtfRl=}|hYøtr;???3:õ5(O 98њpуI6Yڰک34q)}~<qNd2O=hO\pO	w77
%isHw~	s9+a8k@}0%%TQYAhN4d2rM01W\#ߺUN-	9k	{0sB
>\XX6sΜGrrω;4fcS[[שVJAy!c<HM%Q&WI2؛>Z6:fT"clZ&C pwXկ@y7cE `SJOZB~LChjЎΆynE50C!Gp.E#SXNk׮?LÇ
h 28fpи<C9!bhkB/k(`S /-+AV6Dĳ{wE$)i+Hdn.jjn#χ?(K?qs74{$c?hNap,KRM{Uwrr$zZQ׈uxz<6V8.ZY#CFY/<`~ݺv_IGu颼B9n#G/"*چEdnf
de.E7) f`;Vހd@i
a܌	z1L5|tgE>6lmm(9D~¨*%$H*<]8;;K23Dtosݻ΁'(wޢKh˶;+b;|IzH6~Z!m!@̐#tf
 Vu{6%;m!)19ΣOsDkk!t+D D+aA[ĴjߣaTL)/!<`C˃^rMe/OO]y>=|j`JDpCp僱@@05,J Lw` `Hv`@AFƌ
HGU4v%e ,ߩFEPU2G~1ntS$@&eB/Ϙ.t<%'*JSA#~u'LO>Bq4#AȬ`gMkW}@o6P$Æs^r_B p@#Z%~aO%^ފi ׊Pl,$<9^|yz?#} kDG]GI͚3إ޽5
V
@9Ɖ]_4<r`Y7nZvk
 dhA9!~jDx5qj/.eJ`p'- @ ZM={R7@
[FFlEC4_7N꺓en`Zkc8`ͅ;\v
Pq();U\EDx8:r+Q (uŜ D	YYg(eY:yrܹus7ecLYnXV.\^ss*wuqi_*/[YZQAQZEsަ hFX:k!P"R-j?~xO$Lfgq_WZ4FI/6Iކ )"$ o`hŲ3|mt('-A(_@2,|7<7mjR%(ቿz{{'/Z=r#2
DOX\1e˒.u^MI](zĈf_iCֲL`&]]ޒdcMLMMKҌ؅dMT1uE%E%
>[mfwŜA&92ƍ̴]ڙ&d-	1tuNnXy d
#9[h<q"#%>(e-t:wϭ۷[\/AGCTISԾ>|A30h)~	j#Gܶi}ִe
6 < hϯmJBBKq9'|=_L<MKO"Odffee\7گ(9L!-$\]
<
`-23n-0)lTx愝rT-7,|!/FGʣ    IENDB`                                                                                                                                                                                                                                                                                                                                                                                                                                                                               INDX( 	 
 T           (   P	        V i               dP    h X     YP   
 /G@rG@rHH@rG@r                    A d m i n M a p . c s P    h R     YP   
 's's̆̆       W               A r e n a . 7 z r c s 2P    h X     YP   
 HDvHDvɆOɆ                     A u t o F a r m . c s eP    h X     YP   
 KF
@rEr@r]@rEr@r       \              B e t t e r T C . c s ~P   *  n     YP   
 y"rV y"r>.
3̆3zˆ P     vD              c o n v o y - r u s t - p l u g i n . z i p i P     v     YP   
 cHC!sY!s
.ˆ2Pʆ !     !              D e f e n d a b l e _ B a s e s - 1 . 1 . 9 . z i p fP    h V     YP   
 x+@rx+@r#T@r+        |              
 E a r l y Q . d l l e 3P    x h     YP   
 @$;r@$;ry<r+xCɆ      
              g a s s t a t i o n e v e n t . z i p 6P   
  z     YP   
 0<r0<r髆ɆɆV        r              h a r b o r - e v e n t - r u s t - p l u g i n . z i p . o p cP    h X     YP   
 8@r8@r҈@r-CɆ P      O               P v e M o d e . z i p `     p ^     YP   
 ?CvxCv$dʆvsdʆ 0     (              R a n d o m R a i d s . c s 2 <P   
 x h     YP   
 r<r<rV<r+xCɆ      {              S h i p w r e c k - 1 . 1 . 2 . z i p jP    p Z     YP   
 ԔBrԔBr~Y(BrZğ p      !l               S k V p N i g h t . c s l u g Q    h X     YP   
 taܳʆaܳʆaܳʆ 
     
              S k y B a s e . z i p ^P     l     YP   
 uc(=r(=r=r*CɆ                    s p a c e - r u s t - p l u g i n . z i p . o =P    x d     YP   
 4|=r4|=r$}=r=?CɆ      d              S p u t n i k - 1 . 3 . 0 . z i p . o Q   
 x h     YP   
 
Ut
Ut$ʆ'Cʆ `      Z[               S t a t i c D i s p e n s e r s . c s 8P   
  l   V YP   
 =r=r)=r*CɆ @     
6              s u p e r m a r k e t - e v e n t . z i p . o hP    h R     YP   
 SArDcArArDcAr      6              T u g M e . c s n t - 4P     j     YP   
 hO>rhO>rOR>r-CɆ                    W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               hO>rhO>rOR>r-CɆ                    W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               hO>rOR>r-CɆ    V               W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               4P     j     YP   
 hO>rhO>rOR>r-CɆ                    W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               hO>rhO>rOR>r-CɆ                    W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               hO>rOR>r-CɆ                    W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               M e . c s n t - 4P     j     YP   
 hO>rhO>rV OR>r-CɆ                    W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               hO>rhO>rOR>r-CɆ                    W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               OR>r-CɆ                    W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               YP   
 ]KrtS9ɆS9Ɇ       B             	 Z o m b i . z i p                   ]KrtS9ɆS9Ɇ       B             	 Z o m b i . z i p   V                Z o m b i   ?Q   ! h T     YP   
 ]KrtS9ɆS9Ɇ       B             	 Z o m b i . z i p                                                                                                                                                                                                                                                                                                                                                                                         V PNG

   
IHDR   i         gAMA  a   sRGB   PLTEGpL        "   OQ#   *  KG*%.">8%    "!" 
 %')6.ED@>DCp     9&ܱõ  
!   4,*!-#1gr`i1$_oٍճ٬                              "   	"  	 
#" 

 " "
 '	 "&2()#!(## /%'!
*&
0 "  * !
!& %	""#)&"0',$'&% 
%+.#
EF# 2*. ,$%+!+2)!(7)*5- )" ,  -3%E?>4GAؐ
 1'NQVb92{׈لI   UtRNS  paA?41/^0\sjWL  IDATHǽSSYEaE]-޶LrCB.77$1@H"B1"	 %TP@Eb~8{=3w51q__~i͚5?}˪
f[`@Maa||q^p7˾vcpА!=zs.

		>ѓϦ7Oe<s%',,K:r1V
(4aL.w_9Jb ^)<Kzee=GIoL/ڐ[>Xgd$Hq
nPhamA.?yo($HtwS"&.&&THg|/ed^\\]КbK"ԑBB#rFLe"˝;4j ӧxqĂ#+BΧ{M%`JLӝH@] 4
d{d&Uﻺj4ÀMOy6'gMO+/938$ŋŒ|"Le%Ad00F=a컳l&k/8$Lt<w'nP<8U\j/TuA`	`lhmJU
kZsR|!I@}oO.=n/3.Ӎƌƪ\.)["*UR.eK)vÁ!!W4s.8I#4pYps233u}'M&XZZZtJ)OC,ݒ(\BZ%2PQ!2$GdTCPx0'30
 TZXUWTWy";ʇ[.2J+
eh-ydi! STZHV3o{-''Gs2Ug2eg
0%h,ZQԴ4JLFqOz=}E@Q2Z\XhFw5]鲍 Lu5pD"EE26dpf4r10q2+V(<NzLKeRLU
h-謉F cSpMpH@3>Qkfd{{;yxA܆3Up?nueH.φ"VYy^l]`<9 VBGk]=@3pv7Y۬둔=5{U|(ptPm*ۄ<
3m{Qxv^/0
+( TnXJ|p:a0ig0YaMa
m
xTpN`qrcRzl{bX= pehPQGg#蘴a
n.}oG/]tŊkzO
Xʈ9@Мs, fGnؼj-Ol[MaK"}8aX;Q#    IENDB`                                                                              R[mgګ*uϢ:~)S>cwIS޷CɁ/ԵO?[\oCoЭo\~ǝeUh?q]tڧ>us`}cګV̔'J>Wym2ϒ8e]{u?L~Gۿk_[k}}5~dw~WDѢu#݋Nq˲}U-
  PK:ol ȸ PK   YO            A  stalker_portal/deploy/src/ioncube/64/ioncube_loader_lin_5.3_ts.soux         UT D]{+Y]3d``RhÀĈR'.g2XAE($A

 R$Bh@DR0r@Q=s@*^kժOZ?llυGY\4E7w4w8*{z'q{7_n$x,dq.Kj<n'&|}3cL:~|UϪ[ls
cfƘ{os
cna̾fcs܉
cay1VeƘ12X<kw0e1Ƙ_Gj'c̥1.mc1'k^iy1ƘWcic>̍ڏ[;c>'1|[wGcƘ1vØ7-a71|φ1Wns_1_]f32[Ƙcly1vƘiYF
c>bِ!#Ƙ+e>ƼOc16|'Ƙ-yeƘ71cˌ1o3|Qr)y~
cd14cx
c<wƘ/ϡ
cdy=1ߑǯc.ۗlc̵Ƙ1_3Ƽۜq6Yc%ϗ]1Ƙ8V6=cL
c~