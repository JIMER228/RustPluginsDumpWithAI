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
        private const bool IsEn = false;
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
}