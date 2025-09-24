using System;
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


            [JsonProperty(PropertyName = IsEn ? "Number of drones on a square ceiling(Min 1: max 5)" : "ĞšĞ¾Ğ»Ğ¸Ñ‡ĞµÑÑ‚Ğ²Ğ¾ Ğ´Ñ€Ğ¾Ğ½Ğ¾Ğ² Ğ½Ğ° ĞºĞ²Ğ°Ğ´Ñ€Ğ°Ñ‚Ğ½Ğ¾Ğ¼ Ğ¿Ğ¾Ñ‚Ğ¾Ğ»ĞºĞµ(Min 1: max 5)",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public int DroneAmount = 5;
            
            [JsonProperty(PropertyName = IsEn ? "How many meters to lift the drone" : "ĞĞ° ĞºĞ°ĞºÑƒÑ Ğ²Ñ‹ÑĞ¾Ñ‚Ñƒ Ğ¿Ğ¾Ğ´Ğ½Ğ¸Ğ¼Ğ°Ñ‚ÑŒ Ğ´Ñ€Ğ¾Ğ½",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public readonly float PosToY = 100;

            [JsonProperty(PropertyName =IsEn ?  "Craft settings" : "ĞĞ°ÑÑ‚Ñ€Ğ¾Ğ¹ĞºĞ¸ ĞºÑ€Ğ°Ñ„Ñ‚Ğ°", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<CraftSettings> CraftList = new List<CraftSettings>()
            {
                new CraftSettings()
                {
                    displayName = IsEn ? "Twig Air Ceiling" :"Ğ¡Ğ¾Ğ»Ğ¾Ğ¼ĞµĞ½Ğ½Ñ‹Ğ¹ Ğ²Ğ¾Ğ·Ğ´ÑƒÑˆĞ½Ñ‹Ğ¹ Ğ¿Ğ¾Ñ‚Ğ¾Ğ»Ğ¾Ğº",
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
                    displayName = IsEn ? "Wood Air Ceiling" : "Ğ”ĞµÑ€ĞµĞ²ÑĞ½Ğ½Ñ‹Ğ¹ Ğ²Ğ¾Ğ·Ğ´ÑƒÑˆĞ½Ñ‹Ğ¹ Ğ¿Ğ¾Ñ‚Ğ¾Ğ»Ğ¾Ğº",
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
                    displayName = IsEn ?"Stone Air Ceiling": "ĞšĞ°Ğ¼ĞµĞ½Ğ½Ñ‹Ğ¹ Ğ²Ğ¾Ğ·Ğ´ÑƒÑˆĞ½Ñ‹Ğ¹ Ğ¿Ğ¾Ñ‚Ğ¾Ğ»Ğ¾Ğº",
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
                    displayName = IsEn ?"Metal Air Ceiling": "ĞœĞµÑ‚Ğ°Ğ»Ğ»Ğ¸Ñ‡ĞµÑĞºĞ¸Ğ¹ Ğ²Ğ¾Ğ·Ğ´ÑƒÑˆĞ½Ñ‹Ğ¹ Ğ¿Ğ¾Ñ‚Ğ¾Ğ»Ğ¾Ğº",
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
                    displayName = IsEn ?"TopTier Air Ceiling": "ĞœĞ’Ğš Ğ²Ğ¾Ğ·Ğ´ÑƒÑˆĞ½Ñ‹Ğ¹ Ğ¿Ğ¾Ñ‚Ğ¾Ğ»Ğ¾Ğº",
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
                    displayName = IsEn ?"Triangle Twig Air Ceiling": "Ğ¢Ñ€ĞµÑƒĞ³Ğ¾Ğ»ÑŒĞ½Ñ‹Ğ¹ ÑĞ¾Ğ»Ğ¾Ğ¼ĞµĞ½Ğ½Ñ‹Ğ¹ Ğ²Ğ¾Ğ·Ğ´ÑƒÑˆĞ½Ñ‹Ğ¹ Ğ¿Ğ¾Ñ‚Ğ¾Ğ»Ğ¾Ğº",
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
                    displayName = IsEn ?"Triangle Wood Air Ceiling": "Ğ¢Ñ€ĞµÑƒĞ³Ğ¾Ğ»ÑŒĞ½Ñ‹Ğ¹ Ğ´ĞµÑ€ĞµĞ²ÑĞ½Ğ½Ñ‹Ğ¹ Ğ²Ğ¾Ğ·Ğ´ÑƒÑˆĞ½Ñ‹Ğ¹ Ğ¿Ğ¾Ñ‚Ğ¾Ğ»Ğ¾Ğº",
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
                    displayName = IsEn ?"Triangle Stone Air Ceiling": "Ğ¢Ñ€ĞµÑƒĞ³Ğ¾Ğ»ÑŒĞ½Ñ‹Ğ¹ ĞºĞ°Ğ¼ĞµĞ½Ğ½Ñ‹Ğ¹ Ğ²Ğ¾Ğ·Ğ´ÑƒÑˆĞ½Ñ‹Ğ¹ Ğ¿Ğ¾Ñ‚Ğ¾Ğ»Ğ¾Ğº",
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
                    displayName = IsEn ?"Triangle Metal Air Ceiling": "Ğ¢Ñ€ĞµÑƒĞ³Ğ¾Ğ»ÑŒĞ½Ñ‹Ğ¹ Ğ¼ĞµÑ‚Ğ°Ğ»Ğ» Ğ²Ğ¾Ğ·Ğ´ÑƒÑˆĞ½Ñ‹Ğ¹ Ğ¿Ğ¾Ñ‚Ğ¾Ğ»Ğ¾Ğº",
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
                    displayName =IsEn ? "Triangle TopTier Air Ceiling" : "Ğ¢Ñ€ĞµÑƒĞ³Ğ¾Ğ»ÑŒĞ½Ñ‹Ğ¹ ĞœĞ’Ğš Ğ²Ğ¾Ğ·Ğ´ÑƒÑˆĞ½Ñ‹Ğ¹ Ğ¿Ğ¾Ñ‚Ğ¾Ğ»Ğ¾Ğº",
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
                [JsonProperty(PropertyName = IsEn ? "Craft name" : "Ğ˜Ğ¼Ñ ĞºÑ€Ğ°Ñ„Ñ‚Ğ°", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public string displayName = "Air Ceiling";
                [JsonProperty(PropertyName = IsEn ? "Prefab to spawn" :"ĞŸÑ€ĞµÑ„Ğ°Ğ±, ĞºĞ¾Ñ‚Ğ¾Ñ€Ñ‹Ğ¹ Ğ·Ğ°ÑĞ¿Ğ°Ğ²Ğ½Ğ¸Ñ‚ Ğ´Ñ€Ğ¾Ğ½", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public string Prefab = "";
                [JsonProperty(PropertyName = IsEn ? "Grade level" :"Ğ£Ñ€Ğ¾Ğ²ĞµĞ½ÑŒ ÑƒĞ»ÑƒÑ‡ÑˆĞµĞ½Ğ¸Ñ Ğ¿Ñ€ĞµÑ„Ğ°Ğ±Ğ°", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public BuildingGrade.Enum gradeLevel = BuildingGrade.Enum.Twigs;
                [JsonProperty(PropertyName = IsEn ? "The item you receive when crafting":"ĞŸÑ€ĞµĞ´Ğ¼ĞµÑ‚, ĞºĞ¾Ñ‚Ğ¾Ñ€Ñ‹Ğ¹ Ğ¿Ğ¾Ğ»ÑƒÑ‡Ğ¸Ñ‚ÑÑ Ğ¿Ñ€Ğ¸ ĞºÑ€Ğ°Ñ„Ñ‚Ğµ", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public ItemSettings resultItem = new ItemSettings()
                {
                    Image = "https://i.imgur.com/iWcfuu0.png",
                    ShortName = "drone",
                    Amount = 1,
                    skinID = 123321,
                };
                
                [JsonProperty(PropertyName = IsEn ? "List of resources required for crafting":"Ğ¡Ğ¿Ğ¸ÑĞ¾Ğº Ğ¿Ñ€ĞµĞ´Ğ¼ĞµÑ‚Ğ¾Ğ² Ğ´Ğ»Ñ ĞºÑ€Ğ°Ñ„Ñ‚Ğ°", ObjectCreationHandling = ObjectCreationHandling.Replace)]
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

                    [JsonProperty(PropertyName = IsEn ? "Custom image(If standard, leave it empty.)" :"Ğ¡Ğ²Ğ¾Ğµ Ğ¸Ğ·Ğ¾Ğ±Ñ€Ğ°Ğ¶ĞµĞ½Ğ¸Ğµ, ĞµÑĞ»Ğ¸ ÑÑ‚Ğ°Ğ½Ğ´Ğ°Ñ€Ñ‚Ğ½Ñ‹Ğ¹ - Ğ¾ÑÑ‚Ğ°Ğ²Ğ¸Ñ‚ÑŒ Ğ¿ÑƒÑÑ‚Ñ‹Ğ¼", ObjectCreationHandling = ObjectCreationHandling.Replace)]
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
                            new CuiTextComponent { Text = $"Ñ…{needAmount}", Font = "robotocondensed-bold.ttf", FontSize = 8, Align = TextAnchor.LowerRight, Color = "1 1 1 1" },
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
                ["NOUPGRADE"] = "ĞĞµĞ»ÑŒĞ·Ñ ÑƒĞ»ÑƒÑ‡ÑˆĞ°Ñ‚ÑŒ Ğ¾Ğ±ÑŠĞµĞºÑ‚Ñ‹",
                ["NOITEMS"] = "ĞĞµ Ñ…Ğ²Ğ°Ñ‚Ğ°ĞµÑ‚ Ğ¿Ñ€ĞµĞ´Ğ¼ĞµÑ‚Ğ¾Ğ² Ğ´Ğ»Ñ ÑƒĞ»ÑƒÑ‡ÑˆĞµĞ½Ğ¸Ñ",
                ["NOBUILD"] = "ĞĞ°Ğ´ Ğ´Ñ€Ğ¾Ğ½Ğ¾Ğ¼ Ğ½Ğ°Ğ¹Ğ´ĞµĞ½Ñ‹ ÑÑ‚Ñ€Ğ¾Ğ¸Ñ‚ĞµĞ»ÑŒĞ½Ñ‹Ğµ Ğ¾Ğ±ÑŠĞµĞºÑ‚Ñ‹, ÑƒÑÑ‚Ğ°Ğ½Ğ¾Ğ²ĞºĞ° Ğ´Ñ€Ğ¾Ğ½Ğ° Ğ½Ğµ Ğ²Ğ¾Ğ·Ğ¼Ğ¾Ğ¶Ğ½Ğ°.",
                ["NOBUILDACCESS"] = "Ğ—Ğ°Ğ¿Ñ€ĞµÑ‰ĞµĞ½Ğ¾ ÑÑ‚Ğ°Ğ²Ğ¸Ñ‚ÑŒ Ğ´Ñ€Ğ¾Ğ½Ğ° Ğ²Ğ±Ğ»Ğ¸Ğ·Ğ¸ Ğ¾Ğ±ÑŠĞµĞºÑ‚Ğ¾Ğ².",
                ["UI_TITTLE"] = "ĞœĞµĞ½Ñ ĞºÑ€Ğ°Ñ„Ñ‚Ğ° Ğ²Ğ¾Ğ·Ğ´ÑƒÑˆĞ½Ñ‹Ñ… Ğ´Ğ¾Ğ¼Ğ¾Ğ².",
                ["UI_CLOSE"] = "Ğ—ĞĞšĞ Ğ«Ğ¢Ğ¬",
                ["UI_INPUT"] = "Ğ£ĞºĞ°Ğ¶Ğ¸Ñ‚Ğµ ĞºĞ¾Ğ»Ğ¸Ñ‡ĞµÑÑ‚Ğ²Ğ¾:",
                ["NOBUILDONGRID"] = "ĞĞµĞ»ÑŒĞ·Ñ ÑÑ‚Ñ€Ğ¾Ğ¸Ñ‚ÑŒÑÑ Ğ·Ğ° ÑĞµÑ‚ĞºĞ¾Ğ¹ ĞºĞ°Ñ€Ñ‚Ñ‹",
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
}                        Ğ}şÚpµÙ¥Ík±XGw	¦&k^ü¸S¢í£¤ç4ÄfqÌ¸ÆV–Ü,7Ìå¾Zh¢ÉUÅAsŸ•~[2v©¦.f•«¬"m'ô•Ât)+¥è€<¿ÆN&ø¹´| KÙ½ÍX8^#ÖœyÊl\q¾ı©Ô·Š?'D¥×ÁÌù”ú~ÊşE×I·;)‘ÖÃ6‰>šË‚Wß´¥sÌDÛk<İ„IìãŒ?–Ë5\¬,î­kĞ}»‡ËPüW½ÕAù¨:Y­òcy5İ&İÒ(0ßó}6AĞï*"ÚÄ®-”ÏYçÅ†Lú¤Í4Šìi&¤úPô­`ô9^¹énë’şoüÀ‹ZÒÌòrÍÆæïÍQIøI@)I80ÌÁ%nñœ:S¨âÓ×û7D_øÏ ,¦¯LjÌ ¾µM¸~J&|šd—ÊfDˆuE8°®ãÊ7öÌoO.3­J°ÈN¥ÜLê¦”úwl1÷+kö@”ªK †›mZ¸–·«Õ”ÅõöŠ.Şá4¦Vv»Åa\ıötÀl‚û5àîVfˆ|OÊ)OqÄµ	[Êâƒõä:%h+m|(†“â—Tkr‘‘%»VÌ_{B]–LKy§7wü*ì«‹
osäİÚ²z¶ü6ƒM%àW{÷[å>ã+]"P‚Oã;ä­0Ÿ˜¥×'¦GUãôe­îD_n§ã¤ú™µæîpl}Ğ”‹²JõÈCa˜÷|Z,à¦7€á°Û÷+ œ“²¼k´ğÏ–ÁdOTa°¿=ÌĞ÷Í/şŒ*H«w©Ö§ù÷íÄEçŸ¥áô|ch+™9Hó‹áŒâp½#§3-¸W|ˆÀe±¡•ö9ŞG¬UmvùZ—Q0ñ$0 ¹ªÖ@J’G¬·¾²	Qß_MæV"Á `y5ßŞÉ—oø¶­ôÂåäõRúéÓš¤Æü£«¯ù¥–üU>Ç…µ$ğ$“Äº×|ô|QxÂOï—"Ù°©_td³>‡¬¿­a/5qõW~½´jx. qì#Ø *FÊ½ÌTù8x„;ßàk¥ã]Ğuwyde ³lY\m…q»ÊábHƒ‡º<Ï.Ğ…Ÿdó-â»‰X$Äòá{l¾ìõˆ+Ê
}k¯ÊrÑWB"v“Djğe3„lù­…yÓ-_wK	§Öı¡47NßøŞ(á¨->G„jĞo9ˆEªœS3_XŞ„¡xİ* åJ½°Œ¡§9÷™dÒù@Œ,˜ıéP_Tê%aáëØÒÙ¿ƒïúa‘bıùöş@Ş×rä ¶ŸÔK‚Â·êm©ıÒMNë	ä¸àˆ…€•O¼=«ñ‚ñÉ©$êˆSum|İÄå"ËWŠpwAmJé#Ê€å…×cšâ—}w]uÉ;$#rUş¢‹(\^Ç³Îóí<«ÒLŒr3W¤şˆ\zOJš/¨Òƒ}7¾?NÈO‰3zÄ=Ş.Äk?´©©'c-23èBOŸGôİfÁú…ş1£©‡ò$|~9i% êİ¡:Os¦Q¬s©øncI7Ñ{ß’”t°áo îé»^‹ÙtˆäñsM†9%üOˆBLèå3÷ÔRr„'5©û²f
uï$R-Àßü@‰Pó³Å4t-"¶‚É–4›ÃD¤ê÷KúúÓó†DJ®àªÒ·³ÿNè8­ÙÿTãLOùzU®3›káIá‡;²ÀÁ
#s€i¢M}?^Š²MË&AşºÊwÃ€„9‘tÇÄÑÔi¶´}M«à8Š@jÛ©Ã†»Ä7uıÅÜÒygAxKüî2/1² ²D´¹èjí	]páãôòÜ.ÎLÏC	=ŸŸÄ|IRSGŒtp<Ô›XğØ¥™Tâï+ÁÒç¾ÌOcŒ‹q1oáº˜²€PVIÑ½vò0÷€(C·Sj'äaìö ·•Ş¢Ğ6ÙšŸÚº6û´DÑg@'(G¤1JH–M*òÈÓZËXxî(XÊí:¦®mi(;×åA±v09½ÊÛŒôø ¯İ(ÙdÆ,cEÏÒˆâ<" ¡•"Vk~qŠı‡¨‹S?(š­´¹ŠKH @÷¶$J=òŠÎkŠtÔNì‘Ğ$”è;¬Ê"öÓŸ”*'Á‘˜¡Ïw¼ ò¦ÊŞZÃ qPêoÒç÷çªMÏFßCéµwéÆr Ü¸n:Ïuªw!yòõZ×È0ûÃßW‘ò|ö½µÔœK½ke£U	©q	áî?ßÿÙYôûq®İv×òÀ$J689ş9á”gi¨Ãÿ¥öì|?ÂÕŒXPIAŞRì îÒ$âøÔpöVvM¸®ÚÅ|‘¡yÖ² LJ,ÿøXo“òIFiê‘¡	Vg<¶ş¡k%0.úª0È=§úgmFŸ&‹Ò19r7Y‚›m°p#¾Çlø}-KİÉŠ$äŞ¢Y€“Ÿª­„*”Åk0Xå°¢xşëí¢²á60c6;Œütº{$>,}EÜ‡å‡-^¼‘4À¶,å(ßé˜­k"ƒ8µØ'¶¤èòNdf"Ğ.4ûc>Q{õÀ!V%Ùj8jıV‘„S3E‘a"G|¥{Z|âBÆ+ákÜ´–Èšâ*×›3?¢|«–è§SÛl"ŠP1»­nÃ‘‚]¾åÄF9]ğäNÑ¶jU8ŸŸ¬£>'+ ÑksŞáÁ‰Ò`M*†4S$¤½Ò5[‹ƒ`'æ³>¿àî~2¦o²±ÙM«ÚZX§cT¦Í¾²D8 v¶Ô˜N‹ò¾…GVe/Gf¦­¦è½ıÓ¿»^’Ùm½î€WY…«-H1Ç=Øö†Í
ÉäÁœÜn&¼F`
Á‚4~ªS’‘åØ$×*Ã¶×8‡»ªw:O¹*»{'-‰çd±şøÂ»c@×öèUl’(
.Ê9r[¹¢ÌòŞHÉ-°¿p!Ø¨c-Ëh•Ä4ô1]‡oÈ„+ºÎD‘Ú®p”©Ázg@4(ÿÍI°/­ˆ³fëè†ykÌ¥–>¥ª	ÛYCÈ:¨ö#¹ zàÕB§Ø„.\ºs:YCÿü­#,‘åtÙìp²ä}Ñ÷$^“Ğ\i|XÍÊ½S×¸"£éõ¾Ï¼)oÃo^~#ÎÌhŞ²Ø‘
d,56·’<k…º7±aqQÆX[1Pœt÷Òß	†ıªc9v‘ÄØ×@«á0PyAqöP¨‰‚ß4p¤Ğ¥â‹ñ:û]Ç1Ü›²Ãéè/¯ÓÜ‚ƒê»¦Ò’ğ9„D‹3£C¯úÎ’C¨}’8¸òŠ^{‚:VóD…eDÚÖ|§Yd]Üfès.ÂÛ1o**D~£êÈAà2lbz	5^'ÿ‰Z"·CK–BãO‰ÄÌ’ş×iÁ¨+Ñ‹Èü²šnü)å
Á{*W9(‹XÖÀr3SB>Z›ÇVW+<jŒÛKzT³³ğÒ‘-îcTäÆ
%ù¦Yrl^¯S]ˆ*=¾Rqfü’TÍÇ +ÌJÇVÜ3ÂóÅ“Î±ÎJ€U[°Œ ıAÌDÿÈvt81û·gÍ!ìy4ëbÊâ›õAbé´quİóùÏü?b›Ä†±)´.s<ªx3*ÉsA#š™Â;ÜÎ'jğœõ«îëesk™!¾#`:v¶‘:^«vÀ‡¬X\g&áKE–ĞÖÀ3m¬ŞÅ6ÃHFnÿˆ•Ğ[o!r‰™¹Õ5å#PA@zI6›k’DnıbnQ;JÓ\çsŒø*¼ŸŞPÍ€'.P˜ºÁ•Ãö”JŞ)YcŠš_Ğ“µ‹Q€05|ŸûîÖ}¶!#;T˜6°S‡ı™…^9…¦^œãFn¹è Hl‘•;ÕÁ	Zö$GAf»Éÿš¾à¿ÉöêPnËT(Ø¢Ä½d,n7Oí¥´.3–ª¸mÛ[‘+”¸b;=¬}?ÉW˜¯õŞ%ñµ have successfully received a backpack, the number of slots has been increased to : {0}",
                ["BACKPACK_REVOKE"] = "Your extra slots privilege expired, slots reduced to : {0}",
                ["BACKPACK_NULL"] = "You don't have a backpack available",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["BACKPACK_TITLE"] = "Ğ Ğ®ĞšĞ—ĞĞš {0} Ğ¡Ğ›ĞĞ¢Ğ(ĞĞ’)",
                ["BACKPACK_IS_OPENED"] = "Ğ£ Ğ²Ğ°Ñ ÑƒĞ¶Ğµ Ğ¾Ñ‚ĞºÑ€Ñ‹Ñ‚ Ñ€ÑĞºĞ·Ğ°Ğº!",
                ["BACKPACK_NO_INITIALIZE"] = "ĞŸĞ»Ğ°Ğ³Ğ¸Ğ½ Ğ·Ğ°Ğ³Ñ€ÑƒĞ¶Ğ°ĞµÑ‚ÑÑ, Ğ¾Ğ¶Ğ¸Ğ´Ğ°Ğ¹Ñ‚Ğµ, Ğ²ÑĞºĞ¾Ñ€Ğµ Ğ²Ñ‹ ÑĞ¼Ğ¾Ğ¶ĞµÑ‚Ğµ Ğ¾Ñ‚ĞºÑ€Ñ‹Ñ‚ÑŒ ĞºÑ€Ğ°Ñ„Ñ‚!",
		   		 		  						  	   		  	  			  						  						  			 
                ["BACKPACK_GRANT"] = "Ğ’Ñ‹ ÑƒÑĞ¿ĞµÑˆĞ½Ğ¾ Ğ¿Ğ¾Ğ»ÑƒÑ‡Ğ¸Ğ»Ğ¸ Ñ€ÑĞºĞ·Ğ°Ğº, ĞºĞ¾Ğ»Ğ¸Ñ‡ĞµÑÑ‚Ğ²Ğ¾ ÑĞ»Ğ¾Ñ‚Ğ¾Ğ² ÑƒĞ²ĞµĞ»Ğ¸Ñ‡ĞµĞ½Ğ¾ Ğ´Ğ¾ : {0}",
                ["BACKPACK_REVOKE"] = "Ğ£ Ğ²Ğ°Ñ Ğ¸ÑÑ‚ĞµĞºĞ»Ğ° Ğ¿Ñ€Ğ¸Ğ²Ğ¸Ğ»ĞµĞ³Ğ¸Ñ Ñ Ğ´Ğ¾Ğ¿Ğ¾Ğ»Ğ½Ğ¸Ñ‚ĞµĞ»ÑŒĞ½Ñ‹Ğ¼Ğ¸ ÑĞ»Ğ¾Ñ‚Ğ°Ğ¼Ğ¸, ÑĞ»Ğ¾Ñ‚Ñ‹ ÑƒĞ¼ĞµĞ½ÑŒÑˆĞ¸Ğ»Ğ¸ÑÑŒ Ğ´Ğ¾ : {0}",
                ["BACKPACK_NULL"] = "Ğ£ Ğ²Ğ°Ñ Ğ½ĞµÑ‚ Ğ´Ğ¾ÑÑ‚ÑƒĞ¿Ğ½Ğ¾Ğ³Ğ¾ Ñ€ÑĞºĞ·Ğ°ĞºĞ°",

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
        /// ĞĞ±Ğ½Ğ¾Ğ²Ğ»ĞµĞ½Ğ¸Ğµ 1.0.Ñ…
        /// - Ğ”Ğ¾Ğ±Ğ°Ğ²Ğ»ĞµĞ½ Ñ…ÑƒĞº Ğ¿Ñ€Ğ¸ Ğ¾Ñ‚ĞºÑ€Ñ‹Ñ‚Ğ¸Ğ¸ Ñ€ÑĞºĞ·Ğ°ĞºĞ° : void OnBackpackOpened(BasePlayer player, ulong backpackOwnerID, ItemContainer backpackContainer)
        /// - Ğ”Ğ¾Ğ±Ğ°Ğ²Ğ»ĞµĞ½ Ñ…ÑƒĞº Ğ¿Ñ€Ğ¸ Ğ·Ğ°ĞºÑ€Ñ‹Ñ‚Ğ¸Ğ¸ Ñ€ÑĞºĞ·Ğ°ĞºĞ° : void OnBackpackClosed(BasePlayer player, ulong backpackOwnerID, ItemContainer backpackContainer)
        /// - Ğ”Ğ¾Ğ±Ğ°Ğ²Ğ»ĞµĞ½Ğ° Ğ¿Ğ¾Ğ´Ğ´ĞµÑ€Ğ¶ĞºĞ° Ğ³ĞµĞ½Ğ¾Ğ²
        /// - Ğ”Ğ¾Ğ±Ğ°Ğ²Ğ»ĞµĞ½Ğ° ĞºĞ¾Ñ€Ñ€ĞµĞºÑ‚Ğ¸Ñ€Ğ¾Ğ²ĞºĞ° UI ĞµÑĞ»Ğ¸ Ğ¸Ğ³Ñ€Ğ¾Ğº ÑĞ¿Ğ¸Ñ‚ - UI Ğ½Ğµ Ğ±ÑƒĞ´ĞµÑ‚ Ğ¿Ğ¾ÑĞ²Ğ»ÑÑ‚ÑŒÑÑ
        /// - Ğ”Ğ¾Ğ±Ğ°Ğ²Ğ»ĞµĞ½Ğ° ĞºĞ¾Ñ€Ñ€ĞµĞºÑ‚Ğ¸Ñ€Ğ¾Ğ²ĞºĞ° UI ĞµÑĞ»Ğ¸ Ğ¸Ğ³Ñ€Ğ¾Ğº ÑĞµĞ» Ğ² MLRS - UI Ğ½Ğµ Ğ±ÑƒĞ´ĞµÑ‚ Ğ¿Ğ¾ÑĞ²Ğ»ÑÑ‚ÑŒÑÑ
        /// - Ğ˜ÑĞ¿Ñ€Ğ°Ğ²Ğ»ĞµĞ½Ğ¾ NRE Ñ Ñ„Ğ¾Ñ‚Ğ¾
        /// - ĞŸĞµÑ€ĞµĞ·Ğ°Ğ»Ğ¸Ğ» ĞºĞ°Ñ€Ñ‚Ğ¸Ğ½ĞºĞ¸ Ğ½Ğ° Ğ½Ğ¾Ğ²Ñ‹Ğ¹ Ñ„Ğ¾Ñ‚Ğ¾-Ñ…Ğ¾ÑÑ‚Ğ¸Ğ½Ğ³

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
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             ‰PNG

   IHDR    …   fì    gAMA  ±üa  
IiCCPsRGB IEC61966-2.1  H‰SwX“÷>ß÷eVBØğ±—l "#¬ÈY¢’ a„@Å…ˆ
VœHUÄ‚Õ
Hˆâ (¸gAŠˆZ‹U\8îÜ§µ}zïííû×û¼çœçüÎyÏ€&‘æ¢j 9R…<:ØOHÄÉ½€Hà æËÂgÅ  ğyx~t°?ü¯o  pÕ.$ÇáÿƒºP&W  ‘ à"çR È.TÈ È °S³d
 ”  ly|B" ª ìôI> Ø©“Ü Ø¢©  ™(G$@» `UR,ÀÂ  ¬@".À®€Y¶2G€½ vX@` €™B,Ì  8 CÍ L 0Ò¿à©_p…¸H ÀË•Í—KÒ3¸•Ğwòğàâ!âÂl±Ba)f	ä"œ—›#HçLÎ  ùÑÁş8?çæäáæfçlïôÅ¢şkğo">!ñßş¼Œ NÏïÚ_ååÖpÇ°u¿k©[ ÚV hßù]3Û	 Z
Ğzù‹y8ü@¡PÈ<
í%b¡½0ã‹>ÿ3áoà‹~öü@şÛzğ qš@™­À£ƒıqanv®RçËB1n÷ç#şÇ…ı)Ñâ4±\,ŠñX‰¸P"MÇy¹R‘D!É•âé2ñ–ı	“w ¬†OÀN¶µËlÀ~î‹XÒv @~ó-Œ‘ g42y÷  “¿ù@+ Í—¤ã  ¼è\¨”LÆ  D *°AÁ¬ÀœÁ¼ÀaD@$À<Bä€
¡–ATÀ:Øµ° šá´Á18çà\ëp`Â¼†	AÈa!:ˆbØ"Î™"aH4’€¤ éˆQ"ÅÈr¤©Bj‘]H#ò-r9\@úÛÈ 2ŠüŠ¼G1”²QÔu@¹¨ŠÆ sÑt4]€–¢kÑ´=€¶¢§ÑKèut }Šc€Ñ1fŒÙa\Œ‡E`‰X&ÇcåX5V5cX7vÀaï$‹€ì^„Âl‚GXLXC¨%ì#´ºW	ƒ„1Â'"“¨O´%zùÄxb:±XF¬&î!!%^'_“H$É’äN
!%2IIkHÛH-¤S¤>ÒiœL&ëmÉŞä²€¬ —‘·O’ûÉÃä·:ÅˆâL	¢$R¤”J5e?å¥Ÿ2B™ ªQÍ©Ôªˆ:ŸZIm vP/S‡©4uš%Í›CË¤-£ÕĞšigi÷h/étº	İƒE—Ğ—Òkèéçéƒôw†ƒÇHb(k{§·/™L¦Ó—™ÈT0×2™g˜˜oUX*ö*|‘Ê•:•V•~•çªTUsU?ÕyªT«U«^V}¦FU³Pã©	Ô«Õ©U»©6®ÎRwRPÏQ_£¾_ı‚úc²†…F †H£Tc·Æ!Æ2eñXBÖrVë,k˜Mb[²ùìLvûv/{LSCsªf¬f‘fæqÍÆ±àğ9ÙœJÎ!ÎÎ{--?-±Öj­f­~­7ÚzÚ¾ÚbírííëÚïup@,õ:m:÷u	º6ºQº…ºÛuÏê>Ócëyé	õÊõéİÑGõmô£õêïÖïÑ7046l18cğÌcèk˜i¸Ñğ„á¨Ëhº‘Äh£ÑI£'¸&î‡gã5x>f¬ob¬4ŞeÜk<abi2Û¤Ä¤Åä¾)Í”kšfºÑ´ÓtÌÌÈ,Ü¬Ø¬Éì9Õœka¾Ù¼Ûü…¥EœÅJ‹6‹Ç–Ú–|Ë–M–÷¬˜V>VyVõV×¬IÖ\ë,ëmÖWlPW››:›Ë¶¨­›­Äv›mßâ)Ò)õSnÚ1ìüì
ìšìí9öaö%ömöÏÌÖ;t;|rtuÌvlp¼ë¤á4Ã©Ä©ÃéWgg¡só5¦KË—v—Sm§Š§nŸzË•åîºÒµÓõ£›»›Ü­ÙmÔİÌ=Å}«ûM.›É]Ã=ïAôğ÷XâqÌã§›§Âóç/^v^Y^û½O³œ&Ö0mÈÛÄ[à½Ë{`:>=eúÎé>Æ>ŸzŸ‡¾¦¾"ß=¾#~Ö~™~üû;úËıø¿áyòñN`Áå½³k™¥5»/>B	Yr“oÀòùc3Üg,šÑÊZú0Ì&LÖ†Ïß~o¦ùLéÌ¶ˆàGlˆ¸i™ù})*2ª.êQ´Stqt÷,Ö¬äYûg½ñ©Œ¹;Ûj¶rvg¬jlRlcì›¸€¸ª¸x‡øEñ—t$	í‰äÄØÄ=‰ãsçlš3œäšT–tc®åÜ¢¹æéÎËw<Y5Y|8…˜—²?åƒ BP/Oå§nMò„›…OE¾¢¢Q±·¸J<’æV•ö8İ;}Cúh†OFuÆ3	OR+y‘’¹#óMVDÖŞ¬ÏÙqÙ-9”œ”œ£Ri–´+×0·(·Of++“äyæmÊ“‡Ê÷ä#ùsóÛl…LÑ£´R®PL/¨+x[[x¸H½HZÔ3ßfşêù#‚|½°P¸°³Ø¸xYñà"¿E»#‹Sw.1]RºdxiğÒ}ËhË²–ıPâXRUòjyÜòRƒÒ¥¥C+‚W4•©”ÉËn®ôZ¹ca•dUïj—Õ[V*•_¬p¬¨®ø°F¸æâWN_Õ|õymÚÚŞJ·ÊíëHë¤ën¬÷Y¿¯J½jAÕĞ†ğ­ñå_mJŞt¡zjõÍ´ÍÊÍ5a5í[Ì¶¬Ûò¡6£öz]ËVı­«·¾Ù&ÚÖ¿İw{óƒ;Şï”ì¼µ+xWk½E}õnÒî‚İbº¿æ~İ¸GwOÅ{¥{öEïëjtolÜ¯¿¿²	mR6H:på›€oÚ›íšwµpZ*ÂAåÁ'ß¦|{ãPè¡ÎÃÜÃÍß™·õëHy+Ò:¿u¬-£m =¡½ïèŒ£^G¾·ÿ~ï1ãcuÇ5W (=ñùä‚“ã§d§N?=Ô™Üy÷Lü™k]Q]½gCÏ?tîL·_÷ÉóŞç]ğ¼pô"÷bÛ%·K­=®=G~pıáH¯[oëe÷ËíW<®tôMë;ÑïÓújÀÕs×ø×.]Ÿy½ïÆì·n&İ¸%ºõøvöíw
îLÜ]zx¯ü¾Úıêúê´ş±eÀmàø`À`ÏÃYï	‡ş”ÿÓ‡áÒGÌGÕ#F#½òdÎ“á§²§ÏÊ~Vÿyës«çßıâûKÏXüØğù‹Ï¿®y©órï«©¯:Ç#Ç¼Îy=ñ¦ü­ÎÛ}ï¸ïºßÇ½™(ü@şPóÑúcÇ§ĞO÷>ç|şü/÷„óû-G8Ï    cHRM  z&  €„  ú   €è  u0  ê`  :˜  pœºQ<   PLTE   ÿÿÿ!%*ºNO‹BCk9;V36D.2?-1=,0<+0C27:,04*-8+/6*.4)-5*.7*/8+02*-5).3)-1*-7*0;052).3*/0)-1)./)-1*/3-2.).-(.,(-+(,*(+-*/.+0)',)(,*)-NEŒPG—PG•OG”OF’NENF‘MEMELD‹OGNFJC†JC…IBƒPHŒNH€KDŠKD‰IC„HBKE…GA~LF†GBHBGA}JDE@zFA{E@xD?vHC}GB{D?uFAwB>qD@sB>oB>kJFuD@wC?tFBxC?sEAuA>n@=l@=kC@p?<iGDvB?mA>j>;eA>i@=f><f=;d><e=;b<:`><b<:_LJr;:YJIg;:^:9\;:]<;]:9Z;:Z99Y88W99W88U::W77R88S66O&&+++0))-78T56O67P78Q56L45J67L89L>?OIJ[46K24G35H24E13B57F47IFHU14D14C36D03@25B/2>03?25@/2<DGQ.1:58A&(-.2;15>47>-19:=C)+/"%*$',(+0*-225:47<BEJDGLHKPGJO!%+<@F@CGFIM_bf"&+#',$(-%).(,1'+0)-2*.3+/4,05-16/386:?#'+&*./37FJN!&*&+/%+/")-$*-&,/'-0)/2-36(.0(/1)02*13,3518:.56)23*34(01*23,45-56/78+44-66/88.77*11,65.870:9+43098)0/+210<:2=;1<:-321;9/753>;5A=8E?<JC@PG184EWLPgXJ^QYs`eƒl3;5¦Ì®6?8„°Œs—z/50øedÿÿÿUÚñç   tRNSÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿ S÷%   	pHYs     šœ  EwIDATxœí½kŒeÇ}ø«:{ûök†C‰R’)ÑVµNl`7Vk[a¤ˆ$`Iìµ&F¾m`d/²„,²úàì‹ı´X/ïÈ/$rÀ2["Ğ˜ØF/Öˆ‘È¶“2é¡ÉyuÏÜîsÏ9U•õ8UçuÏíéÛÓ}ûÿ#çö=ïª[¿ó¯ÿ«ª˜áœƒ?ê9ˆâ 8@ ˆ„xş)ÑòKAXÄÜ3X¿€Ú%ĞÏƒ>V},èæ 1`ÅĞIƒ.V,hç 1`5ÑN‚VVm,há 1`•ÑB‚¦ˆ(°ÒhiŞˆ+f×9@Xy4š˜Ï9NX=Ô™÷%¬$jÍLqÃóˆ¼ûau´4ï:@87ğ8@8Gğ›ôs
¼m'á\äÀyEõÎóæ.Â9És÷ÖóúÂ¹ÉqàüÂÊ~nÎ!Œ3"œ¼ùâÂ™|B’g×Œâ‡||ö¹A×˜ÜBâÀJàõ»£­ü³fãµ}=6"4	ˆ+€¯ej¾ù××o_úÑùV×‹r³EöıÎÆg›{Cø 
œ]|uZüTûYö“s®%¬¾v£Sü7û?Ó±æ ùˆÎ6¾vÿÃİ6Àã7†Üƒ8p¦ñÚıÿ×=‡_pâÀ™F±İGà'ö¿6ÿ&Ä³Œ¯o=?çŒäşõ¹w!œa|õÎ\Àç/İ™{âÀFvaş9?ºşÚ¼S(ftvñZñ™gÓyg8»¸u8ä¬ŸZŸwqàìb}cĞiü7çğğE!<¼60U`kÎ	Ä3‹ıÇƒÍÃWì·WÂ>â3óÚ˜8pf1*ı­«¯?ÿŠşöÊó¯/v#âÀ™E9ó·®ì¼òüëÿájpâluH8³Hkê€&Á+Ï¿¾ÿÜ•à@2',LşUÁ•«û¯?ÿ¯şäù×÷¯ÔÍyÑI¬®`ÿ_¾ğsÿ²Iy 9°:¸‚à›Ís–± 9pfÑTõ^ùì?|õç~¸qbY6v œYd£ÚWıòÿïLÄ
uå±âÀ™EúN¸ıÊó¯ï?÷Ü~ƒsH8³Øİ¯<ÿú>€+xåùWîÁ;°õæœ‘8³x1õßğ«Ï¿şpåÊ`ÿõçƒ	Ş}bÎHœ]l[ÿ_2ßöÿµo^ŸÍjB8»˜úÎâ+¸ê¾XŸ›?@cLÎ0~)maäãz6î6hŒÉ™Çcñü¤á;bn–qàãÅ²˜wÊ×¶ç9$œmüTñ«ı'\¿_w$µ€8p¦1IúóEÄÅ3’Î4^\Ûë#Á¯É>'»àŒãµÃé€×ŠÃ9vÍ?°¸~$­ªÿoŞİ7‘8°*øµèƒÿmcçk°ñùyWV¯Íd­CøÚ!Û 
V¯İZj[Û ¯mÜ–ââ¼n  q`ÕğÕÑò³ìé!Ö @ P¼€`A Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ zNªß’¸„G’â ÆšgĞX3‚q€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€/zÁ«Ë(ÅIA¾´Ğéç¤®­wüµt¯dG+Ñé€R‡šûmÎ@]ÕœÃóëª×;^€¿²VÊ"rê äHäı¢ÕWÑ~+Ö}læ¾% ||<Ê&óY°P]›• ÂŠşzI™b"
öÎ $æ»rKszxµ«a O½²DìÒsêº ®mÜ¾ôù§DÇÏ&jéıí4X×‘ªùİï	 (?–o}®¿t‹Õu>¼z4ŠZ/¿)¶)rÅ Ht¶ÒUˆ=Û[×Å8ğ»¸u|" a9´7t'ê?¦…ğ¾ö·úÊ·X]“µÂv0 	Œ¨7×J{rãL]ÁZ!¬`}o_]ãÀo`î9ÃĞÍ€®wgQàu Ş^UİöY9ûé.V×Öúx5é“e^lØR« íH¯ÿ £÷Ôu!üZºT
D€èŸQ[CÏï<xí x¶Xëî'«k£>‘@êå•µšUz@­PI¸ ÿõç¥9R }¶»®ú’a¶áïÏæŸttDhĞ°_
— 
@$ìVğíâc…yãı{ã4‰juàÿÀu=°E$ òŠB?+AÀõc) äß¾<§ÈÃ8ğÖ<;d0zzı¶@š
€²·*êçnÄ¯¾Ğñì·ñ›Í«N/êHP$  |ĞÖ„*€nÿ¤ R¯ı½2lvÖUc®Ïî“>8¯óÄ M5÷ÎFõ@1G Æ¾Âá´£„‹ÔµM¶ÜóÛ„V“Aãöˆ€Z ßı–Rgg noµ–ŞaÖÊã¡ÀBVT—hîŸÙ±ğd@íY’W ˜Rã2.P×¹èD›$K (¦<=ĞkÿŠäIîä¿Öı‹4,pÖØxó‡ûK3ˆü¸Ât	¡z ¯hIÊTM H.=0¶Yv”çaê„‰-E‹O ¡è¢».·N{Vîk ¢¨½ÿAó{èª«Á ÜË“ù'	šà0!P×S5ùïÚŸ)û£2 ÈØŸn-ÑÀº¶‰´šOÀ¤©	4UAí(Œî*Ã+ì9y]¬š¿Ùú)#ÍSä)ºêj0ˆláÈR†w¨	†n2¨à!’ëŸÒ½Q5[|Ğ^Ì!uL2‡·Ğ|
`iÊ€F`ÚUP#@ZíÌnOúj³xÜğ1˜¬m§/DXÔ  Ût*ùÚÀR¶\Úæğ‹Wé‚5@MŸuB8 /.¼À'@ŠlœU{*qÒ¯¦œ‚Ÿ-ÒîL‡ùŞ€úîÀô½ªUlEuà¡Ğ!Ğ\uœf*P+v(Ê‚n ^/P/¶§Z	Aó§Ğo<2+ÿÀÉp ¿èµ°ğ%€ßş˜a4pw»·ÄGGÑ´Hºº_0(V?#`€  Uç·–ÈÜÉ«
6…@À Õ èhÿYãË‘0G¯éVk;(Úƒ•0'xZ@át€Š ©~ıë-¿Ko‰H-úis)ĞÖ4C«rT_1jiöÛ½¥jG‘P­)Êş<aé]%+5À‰ G İıí¯˜v-„eö9r ù«õ¸çDVÑÍ€pü 0—_ê)y'û†ª ßRUqzD@M¤º H³Èª»*óY§€œëû^Lò~·[_°Ë´}Àœ›†ÈwÌÉ[İêGEg¾P…–Ô£Êt¾"X`U K€f³ûèÈ¯¨°|°_pÊ —^10ÅÊd‘åaA†…®êìnùÙC{ aVÜöÁZ'"óº€ ²º¬B‡+ÄbYxè;ÔòÊc§ßü¢ÌA»o¸Ã3ìÚN™Üª‘¨|†]@ê‚‡b‰9u]D‡*´)‚L´[ÒX<Ç®‘i£3VÖB‡.­Q‘t©`Í\±İ€
XMPÔÁV	 ½Çºg1ïs^]—Ã¥@ˆ¥çØÍí ÛĞë®|î¿€‰¹¨Õ!T…´2 ­ŞS”Ñ?™Àõ¹N'l`‘»Ë±k4ø ¢W´(¢À9Üë¨0è‚sUĞS5RdàÇ…khwQåÜ6^
ÚsìÚqü9võ4rìZ.úáW)
ş4V¡5Dt,<È  •([D€ôŞ{°D\EŠ¥Ï	ŸŒ8u9v‹¢×Œèq7ÊXÓê>¡Ê!À<
¤'°!  © İÔ’ËÒ·9—À³c·êñ®
~ºjÓ-äiµ##ÏE>t&ê.ÁJğPu æM×–“{
g$Çnt“ºè«NP—™ËĞfĞX—@½H‘…"@ú
€‘öP`Pú¬EÅÇÍ³’c· z)à0×"ô•Á@–ÕÀ CÈª PzÂr„Û€Kıÿ#·NkİPôG;úŒ›†cPÖ"ig@]„(õÿ­wÁã1 QÆU6M7“g*Ç®¹w>tÚ—,¬çá´Uiµµ «Jfux]@ !À³X‘H E"“"éuÅhÎZİ´6²ÃbQ-5™€ªû„2X D¨°Àö	€R%2‚"	É}ñqqàìåØõ w^]BxCšö@¯2Eı×•™»x Â^@wF	(ë6 ”	îŒcâÀÊåØÍEg¯æÇm\7 *:5©tA_Haº¿ «D@)(ñÈU°™.3šS•“ñŸ½»]“£´j¶Í(±ç
Å™rö CŠJü"ì´h0ê!Ò§w¦ÈÍË;¬^Îéq„üêæ€	l×‘…æ '˜í¸pw’(#.yäİĞÂÇ	Ø†«™cç¡S§êO{±ÅU-j­ï@àJm`(´$´% Í^”1Cdì>ÛüÎ5 e…§œ/A¬d]
[€&M î
ëÒ¦†æ q0HÏ a@UİÊŞß·”ùÀ‹åèg?ÇÎ¢îçNº4fÄ³aXÌZ„Æ'ä+Ò¸LëZ¡DbÀ ım}Í_VÛîÃCs`EsìV
0@yš?®
v†­ŠTB ÷ÓZEĞí+)áåA$Á¯õ"ßÒí‚3šc ˆü¤Æîà «WÈTÇ·nÛ´Úš`õ€Ò„†mÏ˜HM€*Áoğ¨GÅº¦Z°xH¬l©HÚ­¦2èĞMÖ¤€¾Àg€«P‰˜ÁÀ›‚K4dÜ<‘÷PXá»:ú(Jšº=Ğ €rÑVUDzN¡Ê,M/`Ô Ã m (fu]ß:Š'äøú‚ÕÊ±Cwşcˆ–Ä+¬IXãsnëâª~@:§ e€¦@äÌ@	He€Ê¯ )ù³q6':ò0Xå»¡S6ÇC†Cak0ŠmåİlR@ßµÆ€$‚ı±Œ¸PæjSÿqf_rØ¤	Œ1£+c7pªÙFáºg°Õ"”^€ĞPXVÆ€Ê$‘!ê0ÿ¬™c™‘õ	Ë’+œcH€¤Ÿ½Á× í\t b€ÕJ³+ì<ÀÖÔ6¶@¦ÿG}¸VÊÕÍ±+C[ 'M@uP@µkÍ!*Ï G“ ¢œ?@ß@úf€Ñ‚¬—ÈÆ™_˜ÉÁä@Lp09à;"V>ÇõşLÔI°.)àykab{£¦0.À i»mØn@VºmæK 	dÀzâ©)°>ÅS`º>™®S¶Œñ†ç Ç®El&µøü~-Ğl¼\‘*°f¾.(]P¥³õ>¯Ã4¿.ÆÀÔ>n
£N!§ ÔñŒ7<w9v}¶@Ç`˜Ä¿¬®Ü&•^ã~Ê4ó(à¼BÂÚ0İ€p­>Át
øÒ®ªûx9prìb€UlàÇ¼ká®@¯	šrTÖšŒebUA	g€®½ &Zì;i*TÃÂæƒ9pÎrìÚ+âJZjAÈ³nÔí 9`/¨„€¬*X)¦öc§L0]ŸºÊµ‰:§góp¬1£•É±ëî*Z~Q{óû& ´rE<›]×Å™šÎªÎ C cÿL¦S`Zo~²q¿vc‰¨+œc×™#ĞĞF!€:Oë¬EˆàŞy×Xs ÌW+0®€éÔV7ğS•ÕgŒ2†€˜Ûñ•+œc×£×5c Í3PºÊóüB¡S@˜T!g0kX!àk‚™f  İë£KT ç`‚`
eÄ¥ä2.—4ö|Õsì¢1€¦1Z¸á]g¾S T*ÑeêRç¶4N!s7Õ"&Ó©G5 %À¹ŞQT¥H¢"V|N]MX­;17ØdT[¡Nç°õ»²–‘Æ#p h
ÓêwĞï?g•ôòÇÑC‚cGâÀÊçØµq¹‘(äàé¶(àU¾Ièù…À$‡•Æ5l)àtÁ1 \wc{ ^zòÇ›ƒ+õ}:½8V=Ç®µ­˜Á¬£<·P=Nl+TQ@Öœ¹ë "ék®€Ì*X‘TòÊãéYí‰§›'E™caœ“»­èzûÎá°B€G¸ğ€{½•«ƒ²—= ”:(™gë'Yİ#O=´Z;.ç¥ş/Îó’cTFÕêç½åönØyM»õ+T¥¹zX‹ĞlWº 5&S¯(^šß?¯FaÆ0c¦dé ÇøÃè„«œcTÆ×!MÖƒ®†uòF¸ÃlXN·¨¨Ç˜lÕd¨©íj$àµ_òe’AuŒ± ÎM«ŠB¯= ßP¯ÕÇs6õÉÏpZ}²R&S_pVH˜·>Aæ~Çg'ØÈulGõ:Ã4ãÀùÉ±ƒn~Ö’-¦‹¬˜‘¾5P£4àˆ¯%‹h]ÀºYäd–R®ĞÖÀ†"(pğZ¹6û^ê>Sœ«;0€©Ö±îÔÌè‰€æ¸
µÛ¦* MBÀÓ@) ‘	@Y!  JDV$ r# lû+eŠS×ŸkåÀyË±3Uièµ•‡£Å1o\t­B)À kİxı ¤QeåÔº 5" òD+Jß  êvB Çô`Î[ûñÚd@èhwv[·«j’El€³Fj
H-YKf-§¦úK@ Å€ÌÈññÇÏGƒàŞ¯ˆ+oÓÇ…¥ƒ~@ÖÔ[cá¢’Ö ¨)ƒZ`€@É5r$	r@Y’ÚvµÈ‚Å8p>rìt×[m‡ğ\=hz9<åV×Èª†2°¬9PQ€A3@¯Ê2 ÈRpk;‹,Äó‘cWw
ÚN Ğå
fÈ¨U¶¥có³E¤“­¬JbÅ–Ö-d•AfìÁĞÈë"Àô†>¬ƒ@Y&ôb¬Vİ¸S'hc€ñ;ó¶¡Úúƒ!ü¸ÀÆ…0rÍ*øª€¦ú7°.¨0¡ŠÊyŞqirçÌ‹~ıgjDÀ€ÓŸc÷¡·C¥~xı”oİÖBOøyĞdõÖ÷)½~ UŒœşQrD…ñçŠ  ¹´Q¦5®'ÜQ`jÜ/	qàî®ZİVòù®ºî×ë¡gJë˜')H…÷”A !©ÍU²òr–%™½¿i·J0Òİ€J!P½ûúrQç’I€Š)	€CÍ>ÚQWƒAÕø±UÉ±K»”ásİ“5E Õ7,Ğšÿ¤Ê%?¹*ù©°âT"íæë‚)L/àlbÉ˜$ ¤y”N,RŠ_˜£ âÀáèvWşİÍ±ÃÅ.³àpät+¯"Mc VÏÛ@®ÁQ@ÁW§mu2GVP¶p*E´ äÎ™re^úÅ'ß[­»§v¾ÓY×§Ş©¶Â$x M
4»7¢Nj?Y LÜ\¶ˆN
Sç4)•.èQ@Œù¿¯¶ü‚J€_ÜÙë¨«Á0» á‘X©»íK?Ö]×<µ)If`|‹-€Z7Ğ @¨ßVUrñioê¤ÀØß(ƒ ¤W8„/à ÷1<gî0 ù}â¥®ºjhÚÌõŸÿæíéaso[zlLÅVê´Î»ºX8ÇÎÁ‹›Ù»¿ò7~¢¯®6jôgCÃJ¼*DX£@M “@éœ•·;pèd£
hÂö^7 5”ÿôø‘ÏüxgUõï5Ğ?ğ™¯'YÃÀ8³9ví“}±´Ï|=ı>îV
x#cM Ìağ­\ã#ªXm#ÅÈtª€é8ƒP~7`,@'fLÃÔœ ƒRì“ŸòÓ·Z1P _ß»¢;rì ,šc'=
@4rì*8¯`OiÖsìP-_òŸ|âÓıu}ÿí¾G¨Î [×ú$)VªşîŠVÁÅ{Øjƒ“PÌìQÀÜÂÉ ÕãdØùT_]uæ ~+¹õ®gè*R`P{tO]•[a-ÂÀ‘f¯®;W‚0Çnü§è©Ç^œ_×o§¡`ªFgo¢¤¢òò5¯ksš¼gâ¨J½= „óê¤ÉHòŠ¦¹%³Oh¼ú5lÿàõÖuQàÕÉ‘ååÈş
^Rdm(K@D"ìhÀÌ
x`F|ÚØ‚9ò #óÆÍ Á@˜‡(Hìc ö± €1p eè>G_Z´(ÍÆ@2~êéç6şæ°º&ª¿‹HÔûL¿.pÓ°575ru4ê„}]§u}Ï1€ƒ1Lmfú!¹¹[XGÕğl6­À%€qÂ>ñáçÖŸï­æÂ ®o½¹Uôu¥§¹JrñÓÃÎ=u=+Í‡™ÇŸ° ˆâ 8@ ˆâ 8@ ˆâ 8@ –¾ÖíYÆ«º 9'›¸åtàké^9pG…şQ„P‡šÎáåôàWÖŠC94¨}Ò§Î%~“÷(V[—ÛŸÈ­–ÜŞİéæK ä©W–ˆ]z"›ô²€8Ğ‰k·/uCjb>zfr¬ÍyŒÔB#¹½£ı“®rDìÙ­ÏµÏ/q ‰oÜÅ­á™„‹I:PMßÒ2T'<³š£2xŠ,£ï]û[]%'tã7‚áçóĞÊ®¹<[(Ğä€èÜ>Ò¹:Á‚íOÀèã³îÌRâ@~-}(
DC'ó¬Ïì_éõNªî"èÿEıyzd]úìZ§N°ĞX³s„7Ş¿÷0N“h &Ğ>ı¥F]lf”S5j@a&M
g,Í¿}yN‘‰u¼µˆßlŞ<°½hNàU«}ÕFj!Tİ£uû'z°S‹Sl¾úBo!‰5\Ÿİ¬¶iâÁ–kÌ¶yZæpó·GÔ: ıî·”:Ó«6ÜŞj-½q †µòø(Ğ‰º9 ØYüIü1®#§º…ŒSÁÌ‚æ56n°¿4Äø"
a¡A˜ô®øèÌÛQÍ*Tæì&¼ğ4 QÔŞÿy¸÷ç¬kE¨á^>w¼>Ğ®Ô|+>vM†¥;uÔ§ïñ¦»¨©€Uó7[?r¤zÀ={£w¬=q 6à	L2‡·Ğ|
T4}ô<y5Ö]vw¨À›
!Cd¸=é«qàÈ[¤@èğ¯tÁš€u¸„B‡PÛŒ7Õìwn	X !Rdã¬ÚS‰“~5…8°0:TA÷;»åßjR g=z¸ÒS­VJƒ/2Éë]É\EçŒ®]İ€/˜7­”=#`€  n·AXs,ÂŞõşÚ(Úƒí³:[?€Õ*¤úõ¯·üœÔ‚ÄEĞ¾RÜ 
0@54ÓTóº×'A†·ØS%Ò`? °“²/âÀBìn¨‚~KU±P“i0*ôä¾nùTÓÖ) çú¾‰‹Î|¡
-©áœŞ-Æ€¯ÁXÀ Ùì>ú/ q`!v·üì¡=Ğ°+	nûŠ`­H‘y]@H Y]V=zÎÂ¾ÄcD»o¸Ã3ìÚNƒ)­‰Ê`Ğ%ôÇ¡›b j@Œ@
ßEÒ#d§P}½ôÆ’(n&tQS[%€ôëÅ¼Ïy D¯w¸è
´ıòk ¾(N´2 ­ŞS”Ñ?™„™¬@¿«˜8p$Ds¸×%à¡e±?4ìA'ªÅŞÜâX•àÇ…khwQ%bÒ¾ˆ‚?æ,ïà,ÂÖ)½[gö¯ô@Ù"¤÷şÛƒ%â*R¬×„ÃœĞ1q`zİ=®a•h¡@Ã)ÈÂ‰ĞaÛ¸! Ìï(H.Kß2ä8&@¨úşúßM·gÚU1E>tç©ğ«µ¾ÜìşöÒª0oº¶˜\ØSH˜n)PtzQëfá¢ÍACÕ™µQì
‰•à) FÚCºlú–3è q`.z)à0×"ô•Áæâ"ødÈ Yõ  JOBX¸…¸Ôÿ“¯ø!Qg@G:FÛè‹†cPÖúë»ÔVDéa@	³¾O<.e\­ìÓâ@¹—§×Ç€Î…_k«üx£ˆ-ZíAªQê0Ë¼è. ĞàÙ¬H
$"‘I‘ô-Pk@h‡&B#ˆZ²…|ßp3[p ªî2k¤úkâ°Àö	€R%2‚"	ÉùîÄäîÃ¡şCUöqÓ¨àiEı”úúˆşré{³,’‘óu Êş: 8pDtfûq›æ2‰µµi½ÕÒ=E Eæ/–-­àD@)(ñÈ5¼?pQ£mu'Ä!hˆÓ¾Ñ#Í(±ç
ÍåìAÖ²2šG Ğ¢Á¨ÿe„HŸŞl|ïI} ÌG†„üêæ@‚Ü,ÿnÄ¡9è	f;.Ü$ÊˆKy7´ğÄA¶áÃ£S§j£@-u<©,B_HœBvÙßš- ÍÚ¨ÒìE3DÆî³Í/Q­1¨l‚²œ¯Gtehš@İ-ú†ÛtÁĞ4î ¦×=–ÕÒ¹e‚DªêVöş¾µ`W@B@èG},yÒ¥	4giØ³– ¡ñ	ùŠ€[õĞÎ` 1
`Ğş–kæ/«m÷8Ğ‹†G •P^œØIìM(fŠT±Jä¾"`²K¬"èö•‰”pƒÑƒH‚ßÜ‹Äˆ=ˆ *tZV ÷4fˆ°ò×}¶­mõ oùo ‰Ôp+Á#lğĞ3OTÏ©&q ‘/æ%5•A‡n
°&ô>ì=P"fnÅl?Tdl
.aWav˜“FDˆ>
„Ù¼u{ Aå¢zw8ƒ°Î0B Dh@1›7æ6ZÔGzĞ5Ç`ˆ–É¥¬°&a-gÔP €¨úéœ‚%|M0rf ¤‚2@åWĞ”üÙ8ë˜ŸÄ‚8Ğ…aS²Æ.ãn&S¤
7) ïZc@ÁrÈˆe®6­?ÎlãK›4 Á1éGÃÀ©f[²Fƒğ@#Q Í"”^€Ğ„deH L2 N óÏš‘0–ÙÙXó×¨ 4H€¤Ÿ½
t¸m?à¢¬&Pš]a/à‰€ æ˜ËBÈôÿğ-›.ê(C[ 'M@uP@µkÍ!*Ï G“ ¢œ?@ß@úf€q([)/‘3¿0“ƒÉ˜à`r àvo‰A¨F)àykab{£¦0.À i»mØn@VãG2_H ƒ ¦f4ÑXŸbŠ)0]ŸL×)Ã¥ŞŠºĞP›ÉB->?·'HòrEªÉcÒÌ×E J tÖ Şçuc˜æ×Å˜ ˜ÚÇMaÔÁ)¤ÛÙâ@zl	§ÿ2¯#ğ(`æ”2ûÓÌ£€ó
¹m˜nÀÇ 	¸VŸ`:àe›¸Ï~Z±ìàøóÊÔ¦”œÃaÒ°rA=Z@2‰*EÀ84¬ 0Ñbß…Ã0á°©iˆıÀ€Šªİ¨Û sÀ^P		XU°ReÆN˜`º>µ×·‰Ø€à1q İ~Á–_Ô†p[Æi£"Èñ|wZ
8s@Ug€!€±ÿ&Ó)0­7¿™Áq4"ÛğhèÌhè£†@'‰õÖ"Dpï¼ë¬9 æ«‚•WÀtjûÿÀOUVŸ1Êbnr9q ±#@Íhóš@•1æù…B§€0©BÎ`UlHSÀ×3Í “$æ‘² ÎÁÀÊˆKÉe\ÒØó#A jh›OÀEjá3ß)ª.ôoíÒ —Æ)dî¦Z„Àd:õ¨&  8×;ŠªITÄŠ‡¥j‚8Ğ1wB‰ÚºUÀ::ÃÖ7ìû2rBÀx -2@@aZiúıç¬’^¹· A1
ZÑ&‰BŞøğ’á›„_H LrX)`\Ã–NëÈ’¨z‹ªà¥'r8Ş¥ş¸É^hmëf@RøÓŠ´Dˆìí<) kN‰ÜõP‘ô5×@f¬H*9`G{#ã½F9ĞºÊUÄ!he@÷Ì2¾s8Œ~|À…Üë­”½Äè¡ĞDæÙúIV÷ÈÓD/_¢—ó¦×%S5
xo¹ı…ö@^Aâ'VéBöZgšíJ´ÆÀdêõ%ÀK£„äµÂ(ÌfÌ”,0øœ80¡«ÍÌ,¤Wëä)Ì†ÍmQP°vM@f€šÚ®Æ0@^ûçğÃUF™dP3­‡ ôƒ)ôÚğuÔ<ƒ•k°Ó‹µ$‹(H­†>Y)“©/8+$Ì[Ÿ Os?¹<5†A®çOPóœ‚8Ğ¦Œhõ	(f¤@à­ÓFÑ™Z²ˆÖ¬kE.Î§”ë´50¡€ˆJD¼VW¸ÕŸšó*Jè ˜jëÄq3Cqè¯×ÇU$lQLº<m°ŠzÈ ¬%"+ ¹ ¶ıƒ2Å©–Ä™kzĞ³UÑæ„·öH-i!`Gyı ¤QeåÔº 5" òD+Jß  êc‹=¦Ä6¸¯M„öåÖ-d`g•¨($‹ØIDjÈ  ¦€ÔıQÉÁ¬%àtÀT	 90¦ØñÑÀt:nCt#…3Zç–µ!$f*)`‚š2¨5”\S G’ ”%Ù¡-A`XËàÆ	ºë­¶Cx.
4Gz¡XUÀP@ö€5*
0h Úğd)€™yÔ ˆÔ‚¶(€zp <§5yÜÏ‘vİS"òÅ–Ö-d•AfìÁĞÈë"Àdœû<°e™Ğâ@m0a7„¬ğ'ö'™Æ‘A]À*øª€¦º§°.¨0¡ŠÊy#Ğ¥™ŸVÁÌbÜîÒ Ô Ò@¿
¼~^¾`Ã ô„€?×hEÌ,£ïSzı ª!2¸DÉÆœ+f€, €äÒN>Â´fÁõ¢v
Lû%q †Ø÷¶ªq¬c-Âúò#¾g0H5WÉj$¡EæÂH™½¿i·J0Òİ€J!P½ûúr3k—LPLI jöÑÏ÷V™8Pƒğ#,¨)­¾aÖ9Æ‚„!ÀùıT(N%²Ğ`¾.˜ÂônÜ™dÌ	Ø`²”N,R
æ$j89İÊKthµœAoD¹oµ»‘ózYƒÊÖ7h)PÂQÀj*C :Õ~- ü`ÿP3â@/ıâSïT[áD³ šhvnÆášw8H(w—-¢Æ„ÂÔyMŠ@¥z c¾¢m¿ àwöú«L¨#áyj§ı2‹Ï´®HÛÖÖi›\¦ò
¸ÌQO
ŒÍ}¬2@êà€£À!|! Ç ½™	j˜;@~Ÿx©¿Æš6óÇ!œ#üæí?yİ@à® B„5
ÔT0	”n@q5¢<pèd£
hÂö^7 5”ÿôø‘ÏüxgUµÍHr Ï|=ıãÈ[)à­>Áha¶ˆrÚ‹„º€#Ó©¦àF@ùİ€± ˜1Ò¿æ$ ”bŸü”?EZ+H´àëï¿Ã÷Õ`‡Ô"³n¡qcLyEëÄÌw[mpjƒ¹É(ñ( ª`³İî4ÿv>õÄ§»+ªå q ¿•Üúv
¦B!à/FXÔ¦•ğ5“>.½)¦ª¤±*Y c ÂùuÂx$yEÓÜ’Ù'4^ı¶ğ‡{±§Ä¼:¹#’?ÕßE$ê}æ³KOÿh‰Ğ¶U8ğ@’ 0 a<·
Øß€ıu}Ï1€ƒ1 ï1ÓÉÍİ
àğÀ:ª†’„Œ‚È%€qÂ>ñáçÖŸï­&q ×·ŞÜò]~g¹JrñÓsN"4æÍ]GX}Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€@ Äq€/zÁ5Æ–Q‚zñQ—à‚) ˆı*n?&—Xœeƒ35ŞéQ—âÔ@ XŒ¯î_/,¯@'ƒë›3Y4åÀµ˜•«!Ißøó­3OåãÁ‚¸–?½Ôòœ ^M
"`90Ø.X!
à…"yõQ—áa(ŞX!
 /û×uNràúl•( ¼ğTò¨‹pz0q¹Übœ8>ÍIXôñÏ-·'Ñª±úè&^=Ë~¡vÜê[}ãÀÆêqàÅ³ìò>^ãÀZr1ÔõG]‚ÓŠÎ/$uç—â 8@ ˆâ 8@ ˆâ 8@ ˆâ 8@ ˆâ 8@ ˆ„óË¾‚c&†óË‚Å0¬b*ş’Ã8 7–\Œ“Ç5ĞÄT9-¹'~ñQ—àÔ`^¼³r4¹xÔ%858ÅÖªÖ•ÿíG]„SƒvÁlÅ&òâ4‰ÃPÛ°LVª7xƒÑ…C9ğB‘®	ŞÈVn~¥‡Àğ¹jWgrÏk[‡D G˜³úz2¾¿
¿—ŸÔe8%X|Şr¼aü`i:0Ñ´åGà pŸíP[{°"Ú±àH ¬œ·œ°² ˆâ 8@ ˆ„×9½ú¡w.X0­ºy¸i¾?‰{ğî“÷š7ØŞÛŞÃ¶Ú|Go>}ïJïó¶7ßA\n^¾	Œ³'ñnxÊå›¸lJ`¿àæeÿŒõ)°)póI¥ÈGØÀ">¢×8 ¬¹Æ¿	<i¾ŞÃÜóNİŞÛÆ€§lÜß¼¿y›¸¿y¿öäÍw~çÂ»¸|£"|ù"n^¾9ŞŞ‘õî§ß	O»p/Ã@<y/»Œ›¸¬˜S÷.èÇàæ“ïjJ<6Ã!€5ZèÔbQ?áklòö–S”o|û»ëKfıÂÓ·U¯„82®EŒQØPcAüò¥÷—Ó& €«ß
îşU¹ÌĞŞkQN¢ X”_UKÎ¿û·şıßX–Ä1øòãŸ]êıÏ
Š|ƒ-;óöŞÓ¦Ë¥ ¾0şåå>àLa ²¥§`^ùÖU÷-=ÇãÓ—hyC‡a¶á5µü¨û]‡tµnv.ãñòŸqV0LğpXiil²ü§½ıÎüsÎ®o¸¶äb  lgpñíå?ë4ÔÌaO"‹ğ¦û¶D#Ô]Î9Á0ÜY_r1 `¼m¾Üì=í¸páDr0Ğ.¸?ÿ”‡Ç¾ù{¹÷¬ãÂ*Î©p4äÀŞrK Ø
af^<‰Aº6iı„:j’§)`õ&º88EØ{rş9Ç‡Í“|ØéÆ)â@=9d¹(OÂÔ98M8Q÷íáI>ìtcvï.» HxT8…ràÊ‰øˆ6O 0uF0ˆ;'â\ß>Q9pŸô‹ÓÔ»àê‰Ú„SÕ\¹÷¨KpÎpŠú‚“µ	§PNÃ8p2ú áÑ`[r)PrÇ	c˜]°ìR„¸p9‘ì¸³s«¯ØaŞ\r!çVˆ„A¹dÏĞš«ŒS¤ìÏ?…°,8Í2±u¢CÈ6t$¼q?Ë®{2cKÈ6tXT'¤ù;Vƒ8@ŞÛ•Æ <³äBÔ@1ä“Å0»àDrï(ãÿaQpe@ó8œ"}àDÆ6;PJ„Ã)òlÒŠ:§0^@cĞO§ˆg|Õ¼³‹SÄ‹“˜ï‚àáù6ì—“™‹†`qŠâ†¶/ }à„q
ûÂ	ch|æ*ƒä áTs€æ?œ«vÉ¥è %+œq`B!½UÆ $K_Á ßĞ#Ã)ê¬_À:¤œ qàdßĞ“!†q`ûÎ²‹ {Öáy#hRáÀÂó,Q:›Œ5R@O×°¨¾¬½öâòn}Š²g1–­,"6l°°z¤\7R;,½ÛGgNDl)ç•›YœB»à„Z‡ú‹ë.¹ ¼iˆNÆ3}"~¯3a¶á²KAx”ö†Ë%— pï$Bhb ”?ÉQ§÷I8YÔNt´é'‹S˜CrBşaš„Âb >p’}ÁÆüSÇ‰ÓØN§¨/¸`şĞ˜3òZ,‹K˜æú˜û#²,–ÄİkûÓéíc½ç•Ç8ã›ÇFŠY,‰;ø±Qo,–{bŞLÕqøãwsä¶®	N$-æl`Y~ÂûqùN†õßtòïş.€¹û·ŠÑæ¨¸??JxÀGŸEïAsÑ8”‹vï¿=bÙÁÿ7“ß?àÂ»ùüÀl¿òtø•¬ã‰´ˆæÉ«W±?ù#_íŒÒ•,Ú†o.xÛïSù›Oü¥[Qò½óÎüÆƒ±à‘Lføù~-M‡ä?ï¿áÕ+`³"IŠìïã˜úÇ@9°¨ F>Azp(çY`»?!ÿ½?Î"ùìOöøE•$3L?šßê½áÕ+`E2ŞLGIQ~¨‡¤Z,G'üV\¾÷8ğØmÉïô÷ßáğ?úÄ;¿'±Ö{êÏ<•ÀŸÇIşTOGõ
x‘ŒŸ>”ÙHğæ»I@}Å‘8pí·çwçÕ> ‘C©ôAÎ×wú”‰Ûcäÿù/¥xò©ßÖIÜnÈ	€Ù9nvËø«WÀsD—ÿx[ıß	ş—wz‡ä€S 0gV¸kég«ßÿHq ŠI!ßûömçÿh2½·	É}"ŠÒÎûî~ÿÅ|&r¦Şü!è3¿ü±¿ŞVPÄ\à—niÂsŒ?ö§ €Ço²àjÕ“U_ù±Ïußä¼@¯M²ØŞ¾óİã¥”’¥ã|êş?ùíúyÏ¦å­u 	¾+Eş~ÇíŞü«cˆ?|&-
0°§ö“øgwâÍúË{5Û–``òÁf’³¯&,ÇèÒÀ­ík	îE¸`Âc®\µw&ÛĞa ¾ ØÁ­ÍHä³<Ïî>8ÈŠ¼P`£§şÊı[¡õxY©H(ŠbTÜ‘b#ßß»õûÁışÓûÓÙœ—ğ] C`l§=’JN&
£‡WdÁc``‡iR¼Çyœ|Ô?ã‹“8bÿ°HÆÿä_Ø}?õÿ¦˜•<âl?/ŠbÆ6)øÕÄÀ¾`ú2 \ÿ¦äƒ5©{l6+Ähƒó˜1ÈMìî¸ó÷GÙ·.ƒ
ÈŞı¾5%nÛ	”v:ñbïÍg,	Kÿì €ç¾¥ ãËˆ«h’™3·qWš;L)è;	{*yLéÿ ş?ş¼Ğ§2Î¤,°>3İÁ¿eÔ,Ö¼ øëLîÏRa”6•®oÊ;7oŞÏÔè>*
 bå&c ckßõ‡tP
	@Î9çBHğHÓßûƒÙÇİóÙÌh{9Y_G¾»àY…ñÌ¹÷à‰t<J“¤(Š"/Š¼(
 IÒÑÚ÷üÒûokqÆ˜üşqš¦ã'i2ş'_N1µú$dtXD'Ìeå.27 f÷×>8¾ÉízërñN:r›jvŸmËÍ-pÎ”2Ãíû"ÚÜCÆ%PŒ0KølóÎ÷  {K ~ì½RmTª{,Ø3o{%JùãñƒƒC©´ÁÀÖğ³_úØû3Ô¡Øã¿ş^ú3SÉ 6®¾èôIÒ	aåÀÔo¨iÄ™bö%’æ%nåÑ¸ê6•¸óaÌ’k‡HP¤—.E\ü¹Ú—à[r›ibfd¸ùë­ W¾÷Áw¥·j‚^Üï€ñ”m`ü÷¾ÈªøßSŸ%·~Lÿ›ş÷È×Š«¤È¯¼l«\&’ç	Ÿ%(") $ER<öÖw¯}¿wO±Q pè/H¦¸”§ÈÓ<ÍP°"|DQ	àÎèPÜk­Àî†Ò[A©3…ÿ)}EÓ¯Àê»ŞËG³r%—cx×Şæë3Èb¶sŠÙL¿Ä
kEâN˜HZïrÀ
vÀ
¦ŸÌ98×Eà¼¨±Q\Œª@âGF”ß¤ÚJ°ı> yÀ`¶´ºh¿›?´>SˆáşÇÕ»úİ>lÉÈİøL­à­¤øİ´œs®ŸÀ9çœøˆó‘şÎQ 3^h5¡Ò^Öm{k©§Ì®?ÿd­; t“{[ŒÁª¢€Ùp`
@~?5ÓÀa0~oºîïÚuÆæû¦’o%œ£à˜aVÌŠ‚³fu@ØÁzº1õycğ«ºQÍ‡i]ë3ğŞöêzû•9™ÁLQtúÏ)sà» QÜ+
 (î|¼à/4¶ŞËù—õ™1ŠÇ
İÆĞúÌüó ®¡û]g sHÓu+ğÌ4™¦‡iŞªw``Ly°×0 ìN–â[_$ÀÇP»à[x3Â:F3 ë˜¡ f˜¹¦o?mƒ
QY¬#ö<<q	D`Q£D\êöˆKGe\²¨Ãlv¡º‚Àá‡ŞÂŞ€í}ö}nÍó—;»Äì’ƒ½½ü¾ü…õ>*¾{1bı:ÇˆÇˆq¬¿Åqaºô›‰úÎFŒØ€±,ãQÌ€X€Å‚1Å,,bÁ â"^³ßWÀ9Fà³‚«» pu
¶!¹múØ–»ú ğAuÙÿ•S<åÀ&öÁLÃ"FŒˆÇLˆÉV÷1Q¢¢…( ‰BH!PRŠBRH)! H)e)%ŠBBÆV¦¼^pp0¨« ®H°­M‚USk—Ğ»
ÎùhÄGæğh¤5Q½Ëª›£€èg))rö0Œê·Ó= BQ!
Á”…(„”BJ¿#Ò›Øb–oIYHHH$$ %`RS%tók]a÷ +êOİû “ßğ1ÅÆwábuC€ƒ#IP œk#Œ40F£ıo~Ã·MŠ¡Á@9ğŸI@ìÀaÕS9=œ‡àq‰ìàf’³áz–Î(ìß¶?qÄ±Úàøy ßûGZ'Àşâ‰Um (’d„$I$‰çªÀèÿø‹ (#rYâ@Á/É\Àš,Š)¸”‡S~8•<)Rq(D*dz¿à×€UÜËEæ„".á!JˆŠ8[0,(8$pU‚ÿªn«dhEÄş‹c V$1&qœLTM `â;«
$ 0ßıŸÌv@Ğ&>‘>(€¢HÓ©iZ¤©È¤ÈS \ûe²ƒ?•¿óôJÿ§”R
J)@İ·Û
ê¾Ê`·ì9¹{Ú~* `ÿ®RüÅ>r¾¨BQÂ8„‚*„8<`e'9•çJ%ªĞÒa‚$
ÉÚD©q¢¾rl?âÇ ÛğàÒô6 (¦Å £ö
`*Õ_Ô“ñtz7Åìö3‘0$  ıø
Ø¶1g¨h[ * ª0kgn•\p@@”\–±À WÚú“œCI(.™‚”ú € Dt¨­¬‚•(vV&%+×’ÿó±Wê
,ê¹Ò/³Ò-«t³#xd¿OnÍ°Ì¾¹ ªADÑ8r{1‰ñz‰<Š¢ÑzñÊ‹”•¼,¹äò/şq¤¢ÿëç\–¥v˜Îaş™ÈƒLÛoŠsÉ9ç3Î9çÜœ>cœ2ñÙ¿û…—÷—<»Æ»÷¢h]·–¶U…ˆEy.¢H!r±ùŸf÷ÿ`”‰\äy>ÖÿÆy>ÎÇÈÇ¯çãqçãñc‘±•Ë¼ÌQæLßı£XÛ*ÉÖø…Ä<ùw?WN&“­5l•“I9™$“¤œ”eYNô§ş;™L&e)KYN&e¹QNÊ-}åd‚Øà˜l€œ#Å¿ZŞzÆ0,‡äàDßqÉ%¸ÄŞílÄàĞÙcÆøğ$ßÀÉ	^ùú²­ÿ àş)+c ”)Ô?xîŸßˆ‘§9Rf:¦ 0¨R€"é¨Ã¬ŠAµB©­?é?ã<`ö—ë{¸4"D‚KˆHéÁí‹@–@*fÚ+¬÷'F—:'`†Ñ,Š8D	Œ0D•GZIPˆ~HU‰<E
(Dy
DB!Šùš!¨MqbP,U:«°«JQùwş×!U?Ä5>LÌ0Â‰vÍŒü0	ŒLBJ»k@•ÕY%fP`3`¦ÊÒŞAejduÈ±KÕg"rƒÖ"=ƒˆ°Â+J	 „ıgôĞ†éIÃ#>º¸ŸGâÀÅw·m P±ZØê?
æ}S˜)?u‡Ù÷RyÛ ìí{ÕNd.è£ÙH@1+İRÅP½Òú[dşØÿõw÷Ï" ©÷pDF~ÜÏ„>×˜«îØ™Ä¾±oL~ı?”õ ö~)÷ám{¸ ê‚;<5'|¹õ)TÆˆ}¤û4ÏÔßsw’³]rC.¥´mª”Š"¥x¤çœct	avAœü\ ÊlF&S…fÌíš5í†>Á÷ô&F¶“ÁE“ƒd’<ÃOqÎk: O´Œ‡âJƒq¥(¦ÛŸ[‹Öz*4îbK™7éÌa~_°»ä—
Ø0S:gh„c`„Ùh„,Ó;3 JPÏ 2Œ3dæXfÎ Ö Œ2dŠ1Ê½©ï¾óİ¥ë?6j¬?FÕ–w‚xrËQ©Fr´e+wî1—;» ŞOlÛ‰È}…ûf>«eú#ƒrg[E]élÏe¬˜12@±Ü¾¢‘Í@‰D\ÆF»4c—Ğ: éîE¤»ù€ˆ¼ÀƒU[¶YD‹¨ZÌõìî`w¿Ë¸ˆ "‰X™¬ İ”eŒÒ´‡ˆ2VZc±¬
	ó?"O+CdšN @œÿ7æ‘Wÿƒb`aTG³‹)hÕ°–<®ĞaVƒÊ+_ş#­,ğìîÜº\èğ¯0¯eå3*í†>TÚƒ¥÷aÿwÿîP:÷À•Ï87=¼E8Y‘kşĞà5´ı£l*¡²W*¦< » ¾`€N¸³ ¢‘à ˜àM©jCÀnÅì±`cZw¿U	çÆÜğ*qÔìswõRÇk9æÕ®æqzú“W°»¢ 0Ğ6|/½g«
ùŞQJ]ô¢¿¸èl6(¥ÔÅ*T¬”ºc ¡²)ïš €ÂãÓ~R*Óş`,a`1Ë	Z$PÌå™›±&nä‰Í<·$BõŒ©ôKÀ@c@¼`Àş}tì¾µ²ê‰Xq'ĞÑ¾øë€NH®÷ùŞ¦Ÿ»ìíNs´ .4‡bHs•ü?;ØÅ›WÎ{g »áA1£İ]ì\‹.•¬DŒ2F—1€2ÊBÅ(¦œ"_2¨X1˜â’©XD%Ê8†PqÉ"è¼T ÈÔ§´¹öË4‡%¢ˆAHêa ¦Làõ#Eê+–ÈS P?ñ%]©sN…9 \cß¹P)ÙŠi®b*:vÇ ¨rzA1(vï¼Aˆú˜ù¨\Á¸(u¡¥)®~ÀWŒê2Öˆı_CóFå·ß]ŒQ›æ&/ã/¯½lüçp p¤|Å6ëæñLìµWåó´4ÉU½¦Ú¶ıìU{`o{ØÖ{÷¶íÿz×¹ho{o{o{ï™Ké] Xˆ'¢BÁi×vÉîÛ¿»£İ€QkvwÌ>ì`wGk:çCç¢ÙÅÎî®v­ïîîîîîbM?û.®¢9Iônïf}G[{î¶Y­¥ ; vv±‹]ìØ˜À.°ƒhF˜»R¬@c¾m¨[fçÍJVïØŸĞş‡İİ/]wWÿê·tg7üÙƒã»æ^Şö¦­GëÜÁ°»{Õ2¶*2vÍ»ó*N ”ÒÑÔNÜ¸ÁoÜx•ß¸a¿ë¯üÆ®¿Şàü†>Ê¹wĞ]_}Ş¨vİ¸»áŸ^ÙZ&î=ÛŞü†-®İëŠyÃî¾ÁoÜ¸a6_u59·Ğœùñì`on_4úÓ.vtÿp÷%ïE6*Ã5¼¤u-»SŸa´ğ]Ô”°]s¯áÄ°ĞÜtWtÛZÄbgwç¢‘Î¦+ÀÎÎ^zÉrÄ
çOÀïî ^¾kºîªÓŞµİá$1È?@XQa¾bÂJ‚8@ ˆâ 8@ ˆâ As wvÂŠƒä 8@ œc€8@0 ¥ğƒä 8p~a…?¯mÎHœ[¸×8@p ÎàÜ‚äÀyEõÒó–}„s ¯¹yë^ÂyõçşÏ;öVASóÎ#„ó‚°/ œ„íLúÀ9DíUç½G	«ˆz#×å ‘`åÑhâF_@$Xq4˜µL@K3R¬0ZŞñ6	V­R¾•Ä‚E{GßÁ"Á
¢KÕëâ ±`ÕĞ­ìws€X°Jè3÷ú8 ¢Áj`½?‡ ˆgü=C8@XmPÌˆ@ Äq€ğ_ ŞÿË•ÔßG    IEND®B`‚                                                                                                                                                                                                                                                                                                                                                                                                                   ], ], ], 'type' => 'tree', 'rules' => [ [ 'conditions' => [], 'endpoint' => [ 'url' => 'https://cloudfront.{Region}.{PartitionResult#dualStackDnsSuffix}', 'properties' => [], 'headers' => [], ], 'type' => 'endpoint', ], ], ], [ 'conditions' => [], 'error' => 'DualStack is enabled but this partition does not support DualStack', 'type' => 'error', ], ], ], [ 'conditions' => [], 'type' => 'tree', 'rules' => [ [ 'conditions' => [ [ 'fn' => 'stringEquals', 'argv' => [ [ 'ref' => 'Region', ], 'aws-global', ], ], ], 'endpoint' => [ 'url' => 'https://cloudfront.amazonaws.com', 'properties' => [ 'authSchemes' => [ [ 'name' => 'sigv4', 'signingName' => 'cloudfront', 'signingRegion' => 'us-east-1', ], ], ], 'headers' => [], ], 'type' => 'endpoint', ], [ 'conditions' => [ [ 'fn' => 'stringEquals', 'argv' => [ [ 'ref' => 'Region', ], 'aws-cn-global', ], ], ], 'endpoint' => [ 'url' => 'https://cloudfront.cn-northwest-1.amazonaws.com.cn', 'properties' => [ 'authSchemes' => [ [ 'name' => 'sigv4', 'signingName' => 'cloudfront', 'signingRegion' => 'cn-northwest-1', ], ], ], 'headers' => [], ], 'type' => 'endpoint', ], [ 'conditions' => [], 'endpoint' => [ 'url' => 'https://cloudfront.{Region}.{PartitionResult#dnsSuffix}', 'properties' => [], 'headers' => [], ], 'type' => 'endpoint', ], ], ], ], ], ],];
                                                                                                                                                                                                                                     ƒy?ÎöÓ`vòê³åqê!À6U‰ê—Z1â¡'¤Ğ%X¶ºşíSVáã•]jÒUÌ…]3¨©•"ìbÜ ­#ÉkõŞPXéò]¡VåµŠV7Bàœpª'VµßX_•R¬œ.¢‡‹¹]åy|w¹Tî’:ªöëìËiÙÊ·|Ì/)ß³%w´rj™Ú(.'PT•.^Ù&T¹[ëEe†¨äE}CğmúzA“ƒ °Ş¿:İ}Q•k´P!+¹Ñ›Yÿ·-}¼oºÔQéné=üÜ—€•bÎÉ>LIµ1`U{°ûG¯£&ÓıÒÊ˜E×³vo0Œ®Ÿ_¥tsè¦ï"¬8H/}g{*1ßéİ¢ü6šûË×ökvÚ©ŠkÚïPì?ıÏWŠoX€oø[æ†ûx¶ùöìåÿ zëJôNòï—ø{#ñ÷F3ßËó,ô¹»EùÜÎ|¦É7¾nmâ®ùùKåù.ôÏ[Ê£òCè‡÷”2ŠBÊËò}Hìµ]~™WPnãğŠW.1G 
Ï•¥`C.œ}]‚œœ:ßÅBPÖ+uT¸¥öUÑw¥'G
ƒ<ÅŸËã”‚7rğoq,0ß-Ì¯6Ë%j»Ë›ãb5ù(©q+¾.‹_PÅo®’÷Ä¯Í4¨U"§ÄTô–õîâp(&µ‡ïŞ\ôÕàßrµN–İ¹ê(i±ll[“Cs¾Èçô-¯Äˆß«zÑ‹Oqúù%òRğ3ãı‚´x%ß¹µQ­òêdQñÊ³¥¯|„Ò ÏñK¼ò<iq¿hì<®lÌsóRğ.ãŠÑÛyóªßˆ¸l]>×WÙ®ÂÁªJñ$$)x'‡‚z]ğ¾q£Ä5†«Èó8Æë^!#~½´²š˜=xx{Aê(w+SÅ%•nß|0×Q­Ú ¿vpœ%@óÚí•İDò¤ò|²Ûò1œ¶AéŠ^Úà¯b…QW¹sç€,R“Cm_äsô/\`›“B¥h|±ÈQHæôÁEèÁõæ+^Ùmÿ@Æş¥(è	¥ª8. ›ÀŸ!SØ¨8èwJÁ?
p”¾Wß&}ÁÌ}I¬	ûSôH·cÒ’ê8ğßòÄrQj×4¤\¤¹l<$ôŠİ^dÛí•EØèCò::®i/Â—ÆÇ±†‡Õ#¥=ÛÇKíáVsI<IêÙZñ¶g7iñ"2aj¹Øêv&pş¹ş")xEÇ)¯²­eÛ0Å+-<W
nÈá8iåªÖú·[kø	¯ùN:jxÏª=V(ÛÕf¹¨µyB…”h>ğåPK     phjX               CombatBlockEN/PK     Ö€‚W               CombatBlockEN/oxide/PK     Í€‚W               CombatBlockEN/oxide/data/PK     Ñ€‚W            %   CombatBlockEN/oxide/data/CombatBlock/PK     /ƒzW            ,   CombatBlockEN/oxide/data/CombatBlock/Images/PK    W”™WŒ3+Ø  $  7   CombatBlockEN/oxide/data/CombatBlock/Images/CB_FON0.pngÅJùKZq ÿÚV¬ãYèÆdG2æ~˜Œš;¢=öJIÇ’6©t®ÖjÙ*)ÌN2’ˆ2"Lë§~yÚ	¯D’®ÌNº-­è•İETÆëxı}àsñùT}ÆAO  €ÏM ½“$ï©×ÜişÙü$ ô[R€NÏ¸İ	q1 {æ!Ëı4$ ]H¤ú’&à""%eo~RNGVN&±ùşa¶W¬G>Ïİ¢)d°»_ôùË<mÊ–+î¨‹Î÷	¯X–mòÅ/j]pÍpÜÆÕŸéñKÉ=ĞWäøÛ uCüTá[”hGñbG¢úz§7¶\ÎÊjS¸ş±ÉÏ~Ü†÷´”lËôC’ÿ3¼úá7sÖ_VÓŠ™
éïìùRÍ¾ôú…/¢X²™Ú7pĞëº¨÷ÊÕy±°§[ëíµ©ô„Ô9²‹ê,JGÙí=gğk‡;:r'±U[i]?ÒÀ¸	šı]JµŞuC“zC¸Y¸
3mmÇË§Áa—Àb"Ïæ¶;ºf’45g`éÂ¿iY
qÍŠLe9q¤ë"º3™Á÷LrªÒúz÷JMßW‹"£º>!¼@BÀrÑ˜Ÿå7PK
     W”™Wõ’¶¿  ¿  7   CombatBlockEN/oxide/data/CombatBlock/Images/CB_FON1.png‰PNG

   IHDR   È   %   ‚¦Â   	pHYs     šœ   sRGB ®Îé   gAMA  ±üa  TIDATxíÜÑMA@Ñ·V`	ZÚV %ĞZPX%HJ–@	t€ol€…ñÇøÁ“Lwù½y¬„‰  à5µ›«Õê2·Ç\7¹.ÎË"×¼iš¯So8HÆ1ÎmÂàü-r=g(³Ã@6Sã#×  _&Éx÷ÂÅ‘7½†8è§—O»ö&HŞæöĞ_Ë\×9IÊŞ™ £€~+mm 9=®r»`°}±;A®(ÚA!èj¿Ú¸à$@…@ B P!¨T*
@…@ B P!¨T*vù h[hiš¦\\ĞdcÀdûâ0·XŸ2}5ÍOS‹íÇNV,?XÿGÒ?å£ÕıöL¬¢ó_¬Í³È]˜$ôKy¼Ø‹£øít÷an±>'ÈDáÜ,r•«gÆ<  ø?Øy9¿¨ ÔÔ    IEND®B`‚PK
     W”™WEly  y  7   CombatBlockEN/oxide/data/CombatBlock/Images/CB_FON2.png‰PNG

   IHDR   È   8   ¸Ê·‡   	pHYs     šœ   sRGB ®Îé   gAMA  ±üa  IDATxí]Ù®å4ÓµÃØÍÔ @ $®x¶ÿoóK<o€@ „ó<nY9µ¼W­TNŸzŸ‹]RwÇ±œ*W­ª²wkWºÒ•®t¥+]éJWúSo˜>øàƒå£>züµ×^[~şùçeÏ?ÿüò÷ß÷şù§ß»woÁñßÿíO=õÔ‚#ş=ùä“ËO<±Dy_ë/kYì±Ç–ÓéÔño½ß—•pòÇ|+gèo­÷XÔÑ²eŒ1,hçëı­ŞÚN[ûİúÃq¥¾Òv\Å¸¶úQ>·¾QÿE[Û9êãñIf;¯×gpÍû7¯Ä:l+®gï£LŞ¯³>ÆÄgx×#úewÇƒ#ß=Újü|W¶émÄı÷¹×Ób¿A©m½§cÔºòŒ¾wcß^×Æò/¾øâÿ7ù#=2úğÃçùÊpƒçŠUP¦À>óÌ3Ix×qã/´Õÿı÷ßÛÓO?½•¯‚²•­L]Ö•¾ËV&Ÿ÷Á¿`µÿù,ÏWFßêy $(Çÿâc·hoƒÑ¶s–Ça°ıµ]´5(Tlå(d½8êklíë5û@=ü‹6·s´Ïğ¹m,Ç9ÇÓb²¼‘ç1Ÿ»áÕ›r–É³ilQgè81Ápè	I=Ş7«êû²oŒYûçØd»2ËÇ'ıì¾õÖW{Ä´jtıÊ+¯l#\™n|ÿı÷[Ù³Ï>;?ÊsÏ=aèøÇ™œGÌŞ¬ËûĞ<Ùz´’^ƒé!0¼ÙUÿ°é>êã„§©Ğà\¯±CÃÌ6ÈÔÎD“[…œÁµÌëÛu:>SQÅ,Ó±¾µÙVW_ÅÎ§°éw‘Iä¡ıâ9gYÑn„8ig¶Y}KhmD_|ñÅvüöÛoÇ×_½İ{é¥—¶ã¯¿şºû ¿ıöÛ®­¿şú+Õ³¬eI›`¦ºéšmP‘j 
µ‡×‹Y8µË™ıhÆW
³l¶§m©ö‰öFÌò=³ËlİÂ\Ú´‚Ì°sÆ7†œ“û €Ê,>Ë£¿9j+‡C”±v×äÊ¼2ÓiãĞìwA)Şg÷Œ
 ÇÌ×‰ú]Ûsº„)?Àıû÷·ò~ø!•sğœ{e‚ñÇle0±PşçŸn³>L)œ“q1S£LiÕ:l›íug|Õ üÃ¨æPÓHÆêE;ML™Ìœ`úMH+ùŒØ£O~‡’Y¢î®?é·¹°ñY2¹¿=ZôÑ9nÿF·Q¥ù¸´ŸL9öçMEìùl“1ë¸B(ª1ŒJ¸¶1µĞ:›o€œ×+`oß}÷]ƒ‰µ‚£T÷—_~™ç` hœƒ¨‰EÂºøG!!Ó¯BÅ™v^w€ ,!\ÛµşqÈ¼Vš\,?\§yÀÖ™¹Ø"Ùôª9Ü;bÑI(h×»Í®ÚBÛâx”i¨•¢ïweú‚ÿGæÎ|TÓ°³O|¿#­É¶üP˜}ÔzÔ\*l±hÎïª¦ñEL¬÷Ş{oÃŸşyÂ ¬ãÁ £A{à$fİA¢ ]1Ë A =øG FQ¯šèĞSB]h‚s{jÕFŠÈ:ãÛıô¬’j-a€Éä!e;ªAx-BÀ¶æµôS™~Î°›)Ãw7mC ½µ#“­?ÿª­»8´;m[É4Æ4çXwd‡8óKhşÙgŸ7ß|³¿õÖ[A!4
…D1Ì
B,¢B !&QìmÂ#…ç6Í¢ŒJ\R®Ä:¸³È…‚ÿ¢nSÍú:‘ød8
”3­2Veóû ÉÃ™LÆÙW4Ãn&§©Âçz¨“÷GÛréoĞãÔ$+ö¡ÆÆ?Ä-£ğ6şIB BÎWjŞå¼Xéˆ ¤¯Øc¨ÙUÑ
âç}x°hFA0€;èòñÎé2ÆË=V`zĞ¸‰‡¤¾Ñrºq.ŞRĞÈ$l'úp ¾1‰˜f	ŒKŸsŒŠ	ØNÆ4ÍÈ ÊÈ®9ø¬3¨™5Zw>¯ãiòı¢Ïí}…¹™İÇ>Î€Ÿ&‡åff?ğ¾Qã-öM¦¶œ¢¦–÷ŸÍáöˆ‰qh–ƒ<xğ`Ö‰µwÏ"şA­Ì?($$â‚shbC˜fÛ½ºj¨9E_1Ç¡n]‹¤=n Ğ¦?¤Íüê9e^ÎşGm%o”27…ª0ó„cb›Í‡Œ'y±†y®ÌIÑcˆ¼A“§ƒN8ÚwÏñ	öMavgmS½óE4Èûï¿?¯Wf+@OÚ&Ö4ÜÎÕ‹µâA\@¯ŞÁ¤‚&QmBA‘àÛPÜAf…à@ÜûÄx‰
Í‹™Ìˆ˜[»?2Íš#÷0Û/lıÀHx!®›>Oo‘áŠàµl¯»–İ½I„mÙı—I ƒhİ^{¡vN
Î"S o™7xç¬ ]Dƒ|üñÇÛ€ Ğ¿üòË‰?!¬Zc¾İª-¶ ñH%³Š \½[ôli¤‚îÜY·K@Î¢ìXH"Ú‡‘(WÔÄ!ÑævTû"¼P“yÍ,Ö÷å¦Ìñî80FÔfv&!ÇæciaÿÓÄ²G(ìíˆd‡õDÛtcï†ƒ¤şn,UÛw"‚ëÓO?İ ú«¯¾:_Ô1ˆkéIT¹üºŠÅRQD6Œ9LªîqQ¬BÏŸUpcˆù|ÒRdÜã¡ùãx„×ş®4µ˜¶¢³¢â)K^£±µ³İ¸ßtÌæ¦UÍ“¾m%túÚ W˜³û‘°h}¿çÄî§èóıNh¼Xï¼óÎvGõbàyòçn¢$Ê¶£j0‚©E¢.]«¥›7rµÒq9çÿP§ é³®ApM&Ñó ¬›×+¶)Ãß¦ˆ+øf2‘›*Å3Ó[e&]óñH;	D&Ğ‘º8´ùS¥3Iu·wæéjµĞpÜ
Ü9ŞZ«´GLôbAƒ€àÅH­À|0’NÏÓ½{÷(×Øb!Œ¨ûL
Ğ´MV¤†ˆ@bSsË=Z Qxµ@ıì¢<³Í…J}ş¸§ZÈA´˜]é	ëâUâ3…)¦i%Iµ.ã&!(|>½š|—£ëìÜB+Ôí™­LÒ[fû£éŸ¥F{Šª§¾zöbM“P4M«èb¹XĞ Rƒ@{@‹¬Á4RbJsP{È ‘ö>ˆGÜôrW¯˜Sj—'-B/–i–%Ì²íÒLsOÖœi]©AÄ£¤Ş&u7=FİÄDÄGõ›Í’#†â½7Î®Zg*íôµ™SÎ(:»C™Pl·8SÈB«íÌÀvöf%·n¼_ÍÙ*ºH '‚!şP3DÍæ"H§æ ÀÍKW/Ì-ÄEHÑë$àÂÚôšåî`ÜgZJ´³	™z°ˆ)œyb–OX Ê·\->3ÌC¥3r˜GSû¨&!hŸ=§¬”«6½h.•f[ÓT[–©1gÔœU¥¯ÛÚJãª„¸Ÿ­µ]€±Â:‡},’6ËÚ…	,˜XôbÁÄB„D½XøØ é ï

Hq…ÂqEÃ+ÓÓ»5üR`ôhÚ¦S˜ª¸GQóìÈ„èâ=Û	…Îœ4±¨ºí-3hšqäÈè±‡#-—&ôq½îævuÓQÍ —3ù‘iâÕ:’øöÚ>µmÚvABÒâW_}•Ê¬aö€0 ş{ü£Viî8Â´"î8×†L0­)‰gñ2>¢ª@üÈÌæ˜ˆÎØÓóÀŸš[Jd¬rÁûJ˜Ç™„×}`ÒV¯•ºÚ^Ue—³eÏPÓ”@¹ßäb1ıa¿•9hT÷Õ¼[öÓwCƒhª‰®(T‚¹íÁdE%¤™@sPP4&¡1\»§IA¹.˜²Ø@bxx½§`„.Ş®@ZÚg½™EÓåd<ÎĞ£p[jı%§˜ìê²ÚıVï6Ñ¤cA!éqí^9Ã£JQ9pÉ³©Ó‘ÓEøc]wø}ÒE—Ü‚ Òƒ€<Yî]^ƒ¬š…®ÜrE¡âh7'<Š.Â°XUDÒ"³Š5À×%~1¨]Äüé¦êÓyq	gúrtÊÕë%ãoş}ª6h¦Åõ<º69tmwèµ8Ôô,ÇQx¼\pÇmõi]ÒäS|"ŸsØXK;ûh"æbÑÍûòË/oå¾`
ëAhb€A 9Tk{,7ëÉ7óeû )ƒP‹àÃè*š\.,â¹#b7«º¥‹°-âgâS±VÃÚÑdÀ”¬XÌÎCM41çš‰ä:OyÁÒ”iÖÕï#ı‘yÓ¤1êu^	ğÂÑÁÇ[«SKº¸ª9YÉ;ÒÔq_ÖÄ½ıöÛóEéæ%é‚)šW<˜3<4p‰2°›Tôh!öA–ÔdöE6qÀ‘Ù½Ç'Œ›h*ŠÌø§ègmèõè)ëêúşEàqã#®‚!õwqˆÂìÅìì¶{	²İ LİZxc]+Jé˜’çJ5YÏé-š]Óü£ÊÇ½»ÒAIgº;Z„+
ij¤C{ ¨sU!>4Ì®“äJáÈ%·<')H‡¦ ?Y’aµÊ7gàštÖ5<ĞøËÉº9.çmtæ¬^úh'EÉ£É§¼6#1‡¶¡¦TÔëGGíyí“CPÀ¯ÄµI{‰A»EŞ¤dß2–ré9zßzáòí¦SŒÿVÌµõÕ.D0±(„p@‹0ş£®(tbÀÉ·ûá¹
ÓNTS,Å¦»¹5¸ÅÏIRVxGÈÊøòì·ğ!æØÕ©*L/½æØøN»gfVŞ–zú^CÌ¹dû÷³»vÈëíÚu¬†mğÕLªo­	gê{ŒıR‚å.$+*HG ĞM,‡‚tx²ÖëyC¬!y6/p‡š\JU$×Ôøhî>,ÕD#ã¼vwî/V?»‚éÕÚ­4ìçTjÌ‰»œAõhfË|¾åºùóU$=îÏ×iêgö1ÈfXm4Fö:í˜T½×nbÅ³Ì«i})›š2LÒEëîJ²"HW²Bßò8dší`^,ºzIŠ;|-‡äCÓàÇ9zíæI?ÚK™ŞÜÂIhÒf ¶“ˆµ”»oµéô“\€ &f³Y}İ$bíS´…2~s
+™AÔKa)úíúnİ¢û­ÖxiLtØûí}	 éÀˆ¢«áÆq FÒÈ¡A p÷2İÂh:´SK(ÊÜLZT3KM"&+ÒT¢v	Øz°âzšzãœæûm:¶1Æ¸Í>> Œb¥İ`”äµâ,,³ù0à[FÔO¦vRÌÃ£˜†©
»Ä1uå÷{I~¬({ÀL‹•šgŒQa¬&×ûÉ¯=bÂ¾XLwç†ˆƒ|óÍ7Ø£·ÓÍ{ÿşıíÈT_á` B‚áe«†™n^O{¯vN\–ıV¥êÅ¢ĞøztmçdëÓInV)Ö è=»]OçTú4v›á–Ê–·²q4›G?ê…ÓİPš·«×ÜHC…råeì6Ó‡v}i}vùa­Æ›†3!ía±–vBŠ	 é¤wß}w®ùé§Ÿæ½^xax6/4Ì,n·È6?Ğ&w6ÑgU[0å„÷pŞÅıê®*íDŒmÌÌ^îvÒ{ã¤›=I©ÜÆ>å-ƒ¦†
Si7&š4Ãò‘Ì‹•HêÀLç-ùêQšäDŞÅ5ğ|†À¼KÜÂ¯í<·ùWÑ¨Ş‡Çh$'‡¶GL éH1a6¯nÑõ 0Ç¸„Ù¼î±ânŠ,Ss‹»¼óÆÑ¶?ìG]ÀG[â<„a‚rÅ#4wºp­¶Ãû)T($·[îJ/—3»«fnT>ÉzÚióêÊó­zxÖó³ywíp<ş<«´ù™Æ.VTêÎaĞs0p>'ïÊošbGËÅrb6/…ÌæÕÅRJñs»{*¾³‰Ë«Cî”HaP­!»ÀOS^,icşÑİ8`hPïõ¢&ŞÃõé Wédéğdt­ãÌIAÅù0<¤dØŠBŸ¹­Ÿ„ø¾m)Òx¥¼eGæÛaÂc1®]İÀ}ïw$›×s±¸/+
¡- A kyÚ´Ä@¡gôÂƒµÄÖ£\0…ršWLAÁ¹kIVjnUæ”
M1bŒ–¡»™pAŸaÅööOÖœM{ßo8­õcü) x*Ö¢³½SÎÀfÚ"kQTF‘Û{Êsêf^›eåµ0mÂGíHÿÓäc¿NDÛ6“q'4ú¬|‡Yçbn^0Èë¯¿>Ë Ò×¨ù(Ä¶?øWiÄAN‰wÜ¼Ú·­¶ ÕÀ¡¶¡E¯‡¹q-Ã¿À"iïC\¨…™Ø@7q İæë~Ğü/™QwÌr¿ün4]BhJÏ”¾÷(|cŸ¢Ş®&å³}Å$®9Øn·TzÏO“:
²5{9¿±NH½˜€À‹Å­G¹&]17£!ö Xg6¯&-jz	5	é(‹ëA4¤E¯™Ì›Ímu¡eô/ ‘ôS$$ªÉ£uoÈl8ë:Í#EÕiª…ÄM ;ì¹Í„d9…å´Oı®fä™v/À™•G»
ŒkÒµªï;²‹·?ä¹ªñ~¡E>	k÷/ºyµ“šXÀ!Ä Ü8´
aû0À ø‡«vuåŞ¼$`yh:}Œ#™W‘?ÛÃ=ş"ê®ÚI5È´ñyŒºqYÚ$yYneÍ.[“>¦p¹©F—îòğR†š'#Ç(è£‹t?ëÅ»î&÷Ôù9ÛoÅ'XÎ;µL!ç8ÈÍdÍuõV{wm}^zóê4"O5A‚¦ÖíG;–Èæ%AƒøŞYÕ(ğˆi<ätŞË*¹p¤ï\Ã*8Â¼Ÿf6æYQ´Îb­¢=·ÓÌR®fKg^FMµ#“j)vOQÍÃ™µµ´kút½6™…#§t$í¦š‡#^µ‘Â"íS¿™`£„tHÒŞ4ñ
\rYŞL}sM:³x¡A~üñÇ-P¸–wÅ ÕÆqtï2i‚@–ÿº©²Ï™FB³KµBü,ıxjûG›tÏíGû-‘Ên01EÜÛmØõJƒø0—æ,kI‹£j3Æ¨ı&p«ıõŞ½P=§z–zN]ßŠ¼-srÅF½R³è9'ŒÊLUeËq-w!Yñˆ¸íÎ¡xN‚2Õ2ÕC5…r²X/µ÷ã%UŞ+šU‹m²vS–{«ÈàG	‚š’¢m¥ºİ"éô`ˆS–ıš2´n³q+î'Û]ßã¿¥(v£D
ï\å–UÏ™jç~ËğÙ×P-,‚–0Ç³S³Y»—ƒ0İ©&ÜÕDÓL¸hŠé$îj¢ÛşğÃ©69Eº»§œ@(¸’Ğó®t+?œş’IO¦˜fÕÉ’ãş´wÙ?—´Ü¬2ìüc.Å†p•FºÍSå&˜eb¥nB¯+}íŞ©µz;Ÿ!Gj'`7ÛôÑ^€èeÉ¿ ìnï»µıÚŞÛIY»p6/RN˜Í‹Ÿ?@&–z±@HV„÷Ê4ÂæÅ‚&Ñ_™‚¦`ú	êA0ôgÙx\$ÉğaDï•‚u1ŸºFÒÇ-‘Š–›-~J³DµÌé µ;)Ï(¯Úè.<*„£¶­¢~öh•Z +¤c™{Ğú-Ibé7éİâ£H—qŞa˜ØêÎ˜XüÂ7Şx#ÅAPÆdEhnûƒtwx¯àŞ… ğÇsP‰ŠL-!¡É¥Û ÉÌ3èò´Y}j
%‚óËj—¼áÛnu£î°Hêp3“f£Óy7ÄÒK¤í˜—$Úcõ¹¿ÖÜÔºµs–®Íæ½šIeÜ-İM¼ÊD)Ú©‚€jvŞúlô1ìÙÄì6Î¦c´ñÎçûA0ñ"Âí~`^©‰¥Ï"é4­ üƒæXbW]û¢@ÀÌRpî	‹ æcU+
Ë£òÍâH.xÑîV÷1ûÍÂhkˆ¥5g°%oJ×Š>İtØoFÕi÷SDàºŒ£i[r­¦Ê`+õ=(ï­µÊÎOøcXŠ(ék§5˜:rlcŒE©'Çu'¼X%­¦U@;b!\â?¨yÊ;”İIQM.ÖÑ_¹=7•+SØyÎ{šî&‘
BùÇĞ:æ³geÈ;¥Ú;öq­€µÌ#ñcìS'´\T5…Ï®í€|? é:«O“HÆ•0ƒyµ¯Òİ«1ÖÓöØÔ’¥±è$Ğ$FºBDÒAú+·LVt‚‰–~Hİ¸Z—Øúî”U®^FÅù³Ğ¼Çsz¶üDÊlG1Š»ƒtYì¤£ÛQÁç°¸æîğs\ıì½é^iºy*ei–¶6G†S2®Ù†ã®˜8Ò8Lp›“
?ë™‹;i¤*ÊÎïsÑ@¡'+ÂÄB$}õ\u]QH7/L,hÄ?4%Ä7¯&éî§X@¥B®(T-¢QuReN$/–š L•ÉØúãœ8Úì¢İZOëRóˆ pû¢Ã(´i«Ô^7N6AsŒ1gbo¯åX·"-k“¨¦PÁ#ü†‹Ÿ…£Ÿ×›Ü7/–Üê¶?$h‚t]rË5éšÆĞ5éºí“¯G¯~DËˆûf‘z?ŞöÇf>5Ifª‰¶M[Ü”î¶BİbK½Ó`btö%Ï>fáCÌALsàr-kb	!˜eŸ´G¹ozOî§1³§DÅ~€=ôó±®›—£¾_î¦¸³"#éŸ|òÉ68ı}¸z=şÁ•… èĞtóêÏ0H¸X/ãÜög±|,ºsñ¡Tb­–‘<iOÅæ ¨ø£›A<à×…bl³ßÓyzÒa3–Ièúåóúf6©¤TÚênvş(TBô¹ÃBnZõ}ßá¥ŞoMzäı~K?}Œ;°ä&–î¬B,„?twa¤ãš‚á¿r"¡GKWä¯Îµ4±˜_Å{ÔnJ…@%o×à¡bùQ\]8ö±‚I4ÃÔæ=Ò‚§sš{2{9H–l|šÖÎv¬4ˆĞæÅâĞ}\…y2
AÙá	ØqÜÉe‚—ÌOÓx­"€~†oŠ}f?qóò^,hş0Èª)¶¯¬Xûïƒ¸‹Ûşè®&si]ºEWZdıÇ‘w
AÃy®ÏF>İ»\µ[ã®æÏ‘KŞAİ¡Î¬*g[cìvıØ˜Ğ¨GÅµE²çÇH¯C#_„wj)ÿ›ô=ğŞİ·6Tóìòşó]]0õ;iºq(¼Ò•®t¥+]éJWºÒÿœşló
/ nÂ:    IEND®B`‚PK
     W”™Wß‰Š?5  5  @   CombatBlockEN/oxide/data/CombatBlock/Images/CB_VARIANT0_ICON.png‰PNG

   IHDR         ©¬w&   	pHYs     šœ   sRGB ®Îé   gAMA  ±üa   ÊIDATx‘½Â0…Ï©ŒŠ:%%ÀÔ‰Â%Ş€–	RĞÌ»ğL\¤È“>9ú|>ÿÄâ½_aPœ1æ)üÃ¼á]gÿËdtxÑê6¹sLAÁ*0¡ÿïÖÔà¶àê»¨ŸIÃB–`,}ÁDuq<Ÿ@¤àr¾ˆæ ¦ÀªçÅ—¡£¥ÈC7)Zu#Ş¼'¾Àšİõ]kú¦­ãÍKvÛs±ğx;~_´N?62 ú“¾¨vÙ'€©·    IEND®B`‚PK
     W”™WTPlª  ª  @   CombatBlockEN/oxide/data/CombatBlock/Images/CB_VARIANT1_ICON.png‰PNG

   IHDR         ĞZüù   	pHYs     šœ   sRGB ®Îé   gAMA  ±üa  ?IDATxT½JAŞy€4‚¢+ëøÖV¾Áù––ó 9¬,ÒÄN°³“ÒÊğçš@ ’*B˜|Kæ`nnòûÁÇŞŞ|óíÎìŞyÇ ¢+pşzïûÎ tg*à8ƒ®£o”Gİ0©+Í‡µR‹ŠˆDüÖˆ·²xYx]w1€‰f%ñòœ*a“Ç6èU,ô³ã,`õ˜ò¶BcÅ\¢´n8ù¬¬0Iùä¶ÄŸ†É7x¼‹Ib˜´Á*x½.ñ|æ².“&xÈ;"ÖíK®W˜<ÉúÁX˜Äª_1å!ÑN3y!#ğNiÿC¹º¼‚™Ñ‚&Z~GUdfe¥Ë™pÎ½4É.ä)øş(ÏF_lRy ñ$r;97œ;«œãİş+]·hyü¥tL?eÈIĞò    IEND®B`‚PK
     W”™WªÊÑÔ,  ,  D   CombatBlockEN/oxide/data/CombatBlock/Images/CB_VARIANT1_ICON_FON.png‰PNG

   IHDR         !-   	pHYs     šœ   sRGB ®Îé   gAMA  ±üa   ÁIDATxí•QÂ0E_§ æ€9ÀÂ$ 	CÁp€$0¬Špp¹e#¦ddëûÛIn»,Í=i‘ ,·šÙ0V¦¹1WcL7y’åsÇ<<Sÿ*¶ÌyhR‚ò²ÿ,ß!?á™­=·JòÓ–—üğ¢CWp)EJ[`Qf¬‚<'z¸‚C"¢ƒ{?ÑQt8¼VÃÆ#/í—Š?*Ì•1=†¹.±¤Äò›´ÉòH&ÜÿßÈÅÛ¸ë	Âá|û¡ÚI\    IEND®B`‚PK
     W”™Wts¤+—  —  @   CombatBlockEN/oxide/data/CombatBlock/Images/CB_VARIANT2_ICON.png‰PNG

   IHDR   )   )   ¨` ö   	pHYs     šœ   sRGB ®Îé   gAMA  ±üa  ,IDATxíX;hTA½+„$Zd-ÕÂ•d;1`§ø!*X˜ÂO%Hba¡M\¢¸ÊnÜD£…•(""6vj!²‚hg!"X¨7šøDñƒ¨x=Ã{ÆÙ™÷î¬»›&ïíÌsÏŞ}ofvˆæQ¤lÌ¼—vpü¥R©Tg Ï\ƒKU>ä)KO²?À'àp!y ñ-àUğ%8ëĞ¯ø>æd\$ şš@ó¡à=–áŒPoD¨÷€¤ààg‘b0A«è¡uİ¦±À¡íó’ŒB|ÙîÁ¥@rŒÛ]&¿’j†¸C½Ÿ»q¹@~øikt™üH~Poú¹èChø.ØA~°æu™œ%?¼«›Ğ z–‘?¦Ä‘H´¼!|ØßD?5®iğ×†[T0ğT‚ğ'p…“kÃY0Eµƒó1â;µ8é\h"Oõ „v€_ñ‚Öï3FøöS=Án°&ÕÚ÷³?fÀÕÔ@8êSÍ:ğû¡ÂÆœÚ0„†gØO9Ø5Í`…ıpLS3€Díì?^¢fÉRà#öƒ>¨_ G\f?ôicW‚ãa{‰şØÇÁ.ºÏhÏ±ŸY›bp¿üfÄäıİaŞş$ƒæªQÒú¤/‹ŠÉjãqõ4¥b2ZLÉè?gò™%iÑÃèsp¹?Äö/¡´­V¯ãLŞw$-³6Åà~»Àà°#_9Îd\¥Š1qçz…ƒ®õşŸ8—Ñ,8á±=™ wÂbĞµczÇÒ%“å­ ˆkÕîã*˜%`@WLEOSPãz“ì³é@ğzí^TQ¡n\»´¸IBM	=J2ƒ®
N°ıeH2È£®Šª­Z[‚ÁVğ;'WĞ¬tŞr-yÇµ˜Nğ­Ñ¯NÇZL¶qõ¾SédbFÈE{Jx‹‘Ğ¬¨èqG´1j|§Ñ¿™ƒcETÀ˜¥Cpït€½£ˆøUü¡K;ú{Ù¾Ó¯:§¹º‚kh€¼=\]Ñ)Õ1¨5¨g¬‡æÈ¿•ƒC‡CQÇZğ 7ëOR8xö•Ÿ4:â/¤ù¨ËÏ]L    IEND®B`‚PK     İ€‚W               CombatBlockEN/oxide/plugins/PK    ™hjX
™qªe'  ëÀ  *   CombatBlockEN/oxide/plugins/CombatBlock.csíçn3Eğwò‹i˜Ãv:¡(0R(]ìsràøÌİ™ ˆ^Do!„„øMïåì7bf¶^³ÏNh‚À—Üm™™Ûínûíœ¡s¼4i¾Y«^«åÔC×kÙ9Ö-NÛñİz¬Ä--ïÀn¹ÛX(–·ë<Ê¤U¯}·íË·;“`zÍĞº-Ğõ6s4ê;ÉkÃ=ğmßu‚”¼­V÷ĞmÇrn±k»„Öj×•Y{m7<]oCi'%ÉºÓ	O<ÿaH–¹»î±ÃnˆÂ$³)Õ> s|`‡+-¯şğ:Ò>Ù‚]w¢'Ÿ˜œ¸¿ÖnzÅ‚Q¥Pb…5çñ·éàcÅªXåÂÔƒ“îAË­³zËfT`×1¤œ  —úÎ!ôfOÂ«¨„Ğ?õHÕZÛug	Kùî£vèˆb:~^éº­†ã³}—§¤—>¶½âf‘5—„ÇöO¯_±g«eŸ¸Çƒ¿İÈöëÆk ¼n;'Å)LİdÙçµØ†İ>ìB;ëm(ú]À(^ó±kö;\;lƒ€ğ¡FÁr)¡@Õ.uÚÎ/x1˜·í4›ï w ñ~Î^HãI2J€R¶Û ^–ØŞzP·;N‰ÕîZ=²Ã»ÙwxPb«-»ÖºNËBşPbë:íğV§(–ØŠ†-
-Cö®×õQlÚ ?'ÕÄµªYM7gM-PYN£hğ»CJ¼TıˆÑ´[3 Ph&Ü&+^Ìkº‡¦ßa»mó}³­Z`W\Á.AX ~|'¬9Ô%	Q”Q©Öªİj]HİX,D/”ÎS
–$?J&æƒ4™‚çYL4Ãe^K8™d¶È/v[[7püÚZ‰ñ·Ğö°¶a°¨ÀÜ€µ½µ»­V„‘OÄ·Ú¼ A×`†~?aÛNË&u}ävî°Û0P|kÇñ…úb¼[œ»Ş®cƒŠöOoqÂ»íV×)ÊV¼n˜
‡KVc!üš ëØ9>p|Ô~í$&(*<9‹“¼#Å8•íâkï¨¤ˆ¤	UöpjƒDpSTô&ÁE ß7›‚‹ˆSÀ.µ—„I˜ÀÌ@áêS`Kì²«%öI1ä\Ò&P•=U’Qå¤t&›Ò«9î<³¾9µpf“RVíú‘cCŠ&/š†qŠÕØÅ†Ø%‚;O>)u!&iÎ¨ªœu³Ûn¬œÖÖtsv¾t[ ÊL]*ûlıå’%E…Ö%¢¦ '¨¿ŠE|šâSÄt¿Ñ5r®‘0„Î4‘SFœ”Í6Ï5ñÉ¯Y¦aã{àe˜bu°€­hW¼×°ER¼Û&ö„(AFg­èXVŒ‚Í¶ÎÈ˜|ºÛœÂÅ<lJıĞHVˆ²;Dƒmë4»ãQÏm°haIu]`ÂTŸ2Ãî@–ÃB0½.Êf¹YbXÙÂ_ğ¯İ†~­‹¿7$²„9m¨¾btVW"|R¥JëdÙ,V^âz	 k(Ø˜å†l[¨>şbÕÚz;Å"Œ™8C­mçØ{Ô‘ÕJ’+SB‰1ãSVÉüèú4ïX¼ÿ¤µÂM‘¸ã„!ğYQÀS§”FÆ:Ö^m¹åøá9øÃKI±¼UÛ_ŞXßŞİßÚX¾o}{¯¦$°$»Xê€¼±ÁÕ“uKŒĞXóÜ;Şò¦ûX$iÌFfÊCIÚpiÔB£õØn7Šªf7À¢Sbå(–g‘¡0h`DØ¬:AH|4W°}i2^Œ¯D2@üÈÇ¬u”Ğ…Õóı¸Üò½ôÛiÑ0ôob…¨²C2Ó´˜5‚©,€
½wúO÷¾íıÒ“õ¾ìıÜû­÷Sï‡şÓğ÷+|bÅş3ıSÿ¹Ş¯½oú¯NÑŠ*",pL×$n`•Ùò’Q>'¾kn` &8fñÅÇ™Í'GN†`İqEeb3F·È+â¨f×°S'¸µ>·=Aå'ıgœŸûoô_òí¿Æàõ·Ş/@óıgé¬÷;Ğşü·ŸûÏAéoğ ‰ØôcïÍ¦/T“½oz_¨6¡ü·ıg‰Q’Ö(»ø¼™¾0Ë2İÇgÃb™û„–]<»–ÓñüPˆ¯İ±Ü,Ü³öí˜Ô|ÁÙû¼|ßû~ù­ÿ%÷Ÿ.şÿ~‡¤§á-R
½9”©DS’v{W /y8*õ l]hÌk²‡İ0¸@¢¡Ô3B¢^ƒl*RÄˆg1i|ºAKßîMµo7Öì¶¹«á‚»^¿„DC8Zv±­©ñ	¥úŠÎœ„¢á‚}Úñ½#÷À«ó©!`®Oj„l@†Ã§á´Ùı;§î6OQ$Ù‰¡ò´Y ïˆ¯z³âÓŞïØµ¤8€ø/Py ¯½¿ö_í¿ ´u÷7R¹ÀÛO(ùÀ³_!÷ÇtÅ{?¨ÜŸ è÷ıWI^~Ä¡ƒ¢ô”‡¾gĞòÏı×û/÷¾à8•ä:Ãwhß
4¶¾22ÀÆ€y³î¤ğœÛ{Üf»‘	Í"¦TádŠ)‚`êıRÜ^Ì>î 	o<MÏ­9!P1º~#ßÕ\rì5`¨wĞ-´µÊ`ea›>Ş’=¦Å>1‰\ŸD0ûº÷-¶3¦ØKÜ7ÛwvêË„êˆ#=›z=‘6lô>²¦ï#®ÁŸÁ”)õ9¬Nº4Æ³Å¥5"f›ˆsçä–aw4»e0+ôH%8N…úgğØCô?“Æ7œXúo’Êù¥ÿƒì¯1¶s³ris|%t"7×;Mîì&¸Â‘“áÑ`^½}1"¿pÚAã¸‚“·/ÆdƒÆ³Í—©kˆtš}ÆŒş2L‘í¹‘rO¨·<œUßØ£ğAÇÓ»Ë™J‰U§€0‡«Ùë˜°î{_ ¡¡™ûş!Î<Ö,ğéU;j±&ü=w÷´ƒÓoyiL|‰×±Mğ§Â# {â‚£Ã¯^x¢±W+±[»™yàğ|2óeõ=%ó0IÀéä™íõ¾„Iõìy,‡K(ıª­oàõ¹Ş× ê[‰KŠh¹š•IA…6›ÇÒ»!¿ã éÂh¬¾ÒÜºİbXï0!r( =A6è¯€ø¤sÑN”Ò÷›Ífà„÷Şù·z¾û88Qâ8½ƒ\¢5Œl:^÷¦ã5¢ZJî 	XÓóÕ+s´?î}A˜=ò†fZºÌ¨üŒòğ•9ú ½œMNT=ìHZ2ånPY:Ü48­LR~V@…ú^İ#-ÏGñ*[•Åjevn‘¥=U
¥T@5ğhPa~~nf¾<Ç*™U–¡ßùâb½å ·2P 2jà—3üü­ÊğìC>“&ë ‚³åÊâ\y!£.ˆÃ¡® ÛÇVîLÏT*ós³afaÀ š_˜™f•áĞ4ÇUb´sìšƒµN;p¹ªT«Õf²øÙŸ+ô•úÊ…	}å"„^t!Inz¦éŸxÏä‘}èÊñd¾lÑ[@yš.Ï.,TgP&82•¼#"6fsŒ\5Jâp¦«åùÙY–ò”GÎ³óÑ–oĞĞhœ™uĞüÕc¦z!c¦zac¦zcæü…µ¨´éteäŸªsc ±ê<S£¹
	3³•ü#è¯ŸS¨ö¿hÄ—/I±Ë)TƒÆ“¦•Õ‰Øâö-+Ëbøyï+2ºûÏóõ®È”£c°%ãâÒÈ¨¡@
jD‡÷Hˆ!Ü1Q¢jèÎ´Õø` éH~Óû»^ĞyÀİïı×„w²ÿ:.—¹ƒ–{¡G¢#9FÇ¤
G	a à6-îEé÷×½ßøÇ„ŸPÏ(RŒ‰®ÒçÕlœ¿B®â$€HâWäôùñWœƒây]ˆ}üètÍ%ç“çdı%}¿#märúÈxæòÉ<="5Qu9îÈ0ôÈªA•‚ê`°n¹xjÎ­€Öô[8``ğïS°QÔît6ÚpùZD#$ õÏB¥ÿåGŞÀ²ø†üß¡@;1¾án³7?ÿâ·ä¼ Vä«ı÷½ÎE,ŠÒrùê}’!dü‡alj¶<;LLTIŞœMÆßò¹ÙÄÖ•Ø¬4‘›ç¼>pï4`Ú} Şş5ò¥ºXï#nê¡wG80Äw ç{?àèf U‘_àh'cî;\xÌ8ëß=Ğ.]­Î­MOßhLÂ×_KY7>XX:u6m¤0©ƒ!Mø2ğH×õÆÔpZ¿à.0)&¥ÅŞûäŒı´rƒ<hß’cçÍ©|l MÜâÆ½ Gnu›nïÈŒô¡ùeV»˜±àû$Vş›™¨¯€×Ø±ÛLoÑ.s–W”‘µå»Ç ò…;]­>*ºÿ*~'ü5¾ I÷š÷¾ ‚Øäàe‹mjV›½rã¾ë»‡°	V!¾Hú‚d¿aÀßr—ôøgAf<àªñÂwP•ó=C1æÿ¯>mıHßÃá—Ü[sæG7w°èëè„&—¯£,Xsà»çšoª¹Ñ1k¡Ğ3ø/r¯´D¦ø#àqDDZõrfîÚòP¦œ}‰ğİ†ØÙ¸áÙ¾‡K,®“ëŸØãhérñÖBÿtğw ¾…Q& øÁŞnğoù×G¶İ¨AëÚÍG¥ä.ê¼N€ğÈ÷NˆMëÕ¶m@3)ö³c?ê(bÌà ëGCˆo¢U^~—üûğçz6®¾:/[>T_÷}Ï/¢{óšnËÁ-ÌuÏ÷»ğØKê€¢;…µ‚é¼HÏQv‚ë®½ö!ÕÀ³@_[ÈÇìû5§iÃ¾è<\¹>07ìĞààpŞ­šo¬¤ˆ-ñ€ÀpRNïña£T!‚SK£´dğmHƒ]êHG˜¤ôÆNÆ¢[;Qûğ”XİÁwEµÙ¸eã† €ÆSÙ€•yŒ‹¯äaõ†áû«›w¬,ï®ll®Ş¾¿¼º[Û¼sŸ^Ö×
¢tŸ×e¶ïû˜øvP0Pé8ÆH´M©­M,fArëášÓ„Äí·¨*vbÃ>²'Êg…R
Rëw®qŒ8>¤Şôùé…µ™Ùê«yC·hßÜm‹Ù´ÛæóVF»°•˜§ì×î¬íÖ–w7·±55?tØ¢vÉíUƒ¦ZE¥'2Ùñõ~·€X
§-;l†`»-Ü—Šä^m·vÇº@‹7úÎ1EÈmh)»ÒîÆ:U9Œ²²Š¯­ï¬n×¶Pvöï®¤2Ä(JËŒOX-:meÂ8|Ty;Yğ´[PB;+‘¨ñí×…ìã6†Ş/É=v¿1úªş)/Ó½ÿ:.PÀñÅxù
×­q“,.%š#´Nøş}¿É“–w¨È=“ñÑpR7¹ô~5äµ÷î½†B/ŠmÈÿ½ÄÏ¬ÓuäQÕ{*‹Õ½óhÿ­4ÓZZv~`Â}‹u¹cŒÖ*ıçM
UIW åË0¹' IµrN\ñ­T÷µ?{xåf	HJı—
ËæÄÂ©Î‡Ì98­ÛRÛãuKz Ãij¿[˜rNîVÏ{8½ûù®)¹{ìAx_o‡ ´ví‡¾AïÁÉøY¡dã®8=;rÜÏ~³#ş7rĞ‘—a7¨Ór²”LRx¢5¬Z òp4üRvèùòÀ©ÆDìuü°â%2ƒƒÓÙêpä8’h{Šó…~bOQÚÄëşhMŞ#	6FY_LA|0% ïÉ‚Dg(
ã§ µ¸•²ƒ)6?"¹11	kGlÇ,N2±mÁíN“êÅh}¨JŸwV%ä¹İhƒÊ¾ºèåqß˜¬I‚2ñWÈ[
`…©	Z%.æR0i'ù“@å¥ä¿ºŞó§YÇã¾çˆğóBæ)LbÚÇ|E=yû„êãD9~<‰…ÍÖğu¬èuå1)Qİàƒ/;1’hİ» ÚÊ*O Â¿A”£Ó"ü»É èšLpõ¸^¤ÉcüìšØô^;èuß=pŠxšÖk“ÑÔTvYN81¨ †ÈQĞ8Ê”]¨§@Úvs|»;”8Y3 ÎĞ†àå*$¤s*c
“¼¢Yä8İ‰˜ÄèwµæØRIÅ;¡-vu2|I)ºd‘ñV¤;¿ÏÀÌ´Öà8PVı”-2Åd?Út"”_>AƒS6uùZ¦xè!¹3\Š&v†ÑÙğ¶QÌ†‡ˆS²€’»I~ÀÍl3‡ Ÿ)6™Åle¸Oìä•qNæ`RcÇ¬L­œ«ÔmGÏ¢ÒU'ã¹’»ÉûËZëtá3W1~Ğ+j‚MpvåÓ;ù”çÙ$ÿW{4hµÆÀïçvNœD¦#ˆ„>c”,Šæğñj†­“ÜzV¼Ó"Tæ8ÄÃ=ÉË­–Ò&²ˆí¡§¢HÚYæŠˆÎĞTNH»Ü¡?ƒ­y^ÆÚõ8EŒÏ‘ˆ“‰$V):ŸéePkI›Â4(®7ëâ-ŞI]œq~Ğ4[Õøç°FËœ–d1PÏky7šüŒTÑ±è 'NSùLe9HM±á_bã.ÅL–ßâÅ¨–ï÷?È`-¤_bÓKô†¬€LÁÁwkÃi†GZ{Mˆ–ÙÕàÌ€ÿ®X·yn»)%I²	9|®9ôl]ÂSqe­nú÷À`qv0¸\Q4D«ñ¬"`CSì&•š‰ Á±)•Xk7œÇ6›XVò	{<Ê)›~Ã…=r ¯“5’8såSº9ƒD£Ş	QD&DCô6®õLR b
¶(¢hˆX„Ëş!2›z=CU`¾¥´DL4´C†ŠÕÖÍğ¶æñ}Zˆ
œ2£¢mI @µ…‰Ë1É¢´ñ¥ËZş÷J ù·VVèˆú’;ÃJMê 
‹£Œ†ïYJ«ç¦Jğä48†5™«48ÒÎ‚!²a2Bo¾cÌb¾¯6+Õ‚ˆ+&½™h)…mx{`ÍÍÈØTkiúc`ä2UqÉå„á‘ 	dùhŠâ¨ü\œ œ¥·`—%ï*çOç6`hDâkB£ÛŸºøpË4îpB¯$ÂrÓe»Û^‡õa—Ÿëh¤} ˜ÃË3‡şˆ5´€Á©á9¹_Öù3–qpKÒÖ£½ªøÛ’î8å ³w|/­ƒ]ØvƒÀiIã#âÏR¨"¯£jƒ›³N¶6Âù7T„‡Øp—DÖZ}ŞîœŠ~JFº	¼ÔDaİÏë?¨f7‘+§Ò(Üx)k¯MÆñûô†ÓÙõ0ïÅ«L&ƒÜ%UÖ1YAQXñİ7=:jªG&û1Yƒÿ´üJ´
v ÛÛ‚k¹„×b«Áµ;§pùZ.<Dâ
7œ¶³!/]ĞÖKŒ0I™^Í¨èvƒ>aã"f¹aw`M @	Ç~FßÁü‹7ÃÁkS:&k"vl2Àå_ˆ×Òdê6îğÏ¹0Rr®™½æta;İ¡ØÑ•„X`KÌwøè‚¹g©
–Å¼DÚ«HäC YíTLçËe…'Ğş<{"ák<+D£ªF”³@DÂƒÁÅk$îªu)‹£œÖPH“êÇb2¹zŞëàş³)3rı,cöéj°Òï:Ü+ †x&bšYG`,EÑåÏè7íJdEµé¢ièÀ³I	¼¬@lã‘Äï:ö„ó˜%Ø‰ îÑ¸–Äh§â2¾XØ4û•„^	Š62g~Úi£J&“&@sÌµ|0&Êï¡Ë91ìú]Œ+Gñ›)òÊzòÉ@d‡^t®ß•{…²1÷í–D“72ª¿rJ.=n¤¤n­XÀw‰9Fìİe"ˆ4H+»r‘rÊ3DŠ&Hh@FëÒ@ÒĞ'Ì<@j-‰<öı”[>Ò09›4Å-QBËŞ â¡"ô?aè‘Ôî¢Ç¼üÒpğ<8<Of7qa»‡9l¹¹WS«öô²k¾}{gVWöñs@šq#Î*…|Âãi3ö¡J3ÙÜïğ±*­˜ÈED–kwÆ¼MŸÜÅb–Ø8Yß°7?8q“;¯'Ì×2,ùtu#®X¸|ÍAŸ<)ÆàrP@C6Eå´/À ‰¶_¹˜ö«ã¶¿i?º[;ÃŒw]Z-PMn7Ë ´i.ä@­®¿5ì¹jrË)V
ÎŒiÕcå#/zäğ)^O­z¨ÀXa;üÓ›Š{ª¬œŠí¹‡²D(9šcƒY,]º†œ“şÔ‘½µ½yËöúÎÎşÊòöh#Ü,Úğ@‡:€.®Óğ¸™ç\¥;âÚú`Dén|n,¸¼W»-@İÛèD`‘Â<R´Øí¤nõÌ9ˆ¤h&—-‰Ş(^BÁ…aL)Ì"Yï|ÃUÊÊÔÒ…€á|:Çø?SO¡¬Ís2rZ¹®Ê‘÷ê$sEï§zwºŠÇB®JæHÄÊ±¤Ñ€Ä ŒV™wL„ê˜Em'<Å§ó¥ìRRYp•ÀŒWnEñYéafö»ê9¤h\(Ûı¦İˆÚú÷$X'¶ ÑÒ„êK”I!*šqøİ”_FC>ğ˜BÚÁ:Á{¨ªn…IêX­"xÜÄ$”›¥Xf9O¶‘‘V™©–Rs+”»8›»O¹Óå<1aŒş>¦¹•ë.ƒÈs®¤®pu ãõÙJø¶ØÅ¨)›İp³¹Ghã´e®ÎiR<l³“¬agEo²"ïÜ¼G+ëx^¬|ÈKşĞ`Œbf§QªsnP­ğ<¼”cedï®‡åİÕóàYÏé1ñœÏx8É‰ôoI`NhKXÌy¸‡K}HÂc´±İiJK[Z3G?¦ „©ˆóEOyÆWûã.o¸ó)@s£><Ø"JÛº®Š•t	»æFö€ÍtÉÑŸ4Ì¸3‘h"b2}’b¾8H²Äà‚Á›¥Ál‰x’ 
w$qDÔŠœºÇöñ¨"ğJ<]É~Ì£IAtN	É(£’—mQË	DDÂØ§IÖ{ò’ßIİ¶ù¡”öq‰9/ÜdƒÌa¹æåx°%Ô(A&Ü}M…òÌ¥e¶‹¹P&N"–^tË'aj¤”)NåÆ¦àø	H[6\g“Sc¯vıÀó×ÛxS-z3b÷óyO0#Ô+ı+œ›aÒ©µ!O?gé ¶Á»·+¿í²`Ëíú‘çßábÍÅĞX"Ñ~ÌHA}©$~c¹¦2¿h•!ÂìÕù&e^ıŞ35+ q´z÷) -AyÌögÆi%•o):º$Äl©ò™S®ğ˜XÄ¬,¯Ş~ËöæŞkY1m Cibz)õmlNÕhòG`»mŸ¼ªZRlc–Q,W‰mÑ&µ×Õ4>‘’›7ï„…|^a7°‰È½FÉ1š’Ò_)œ¥AO“‡±»ö/S&ç³C¢åepL›$ØiÍFªcê¤pÍ|Ær…-BäĞy•)jÌCdÑEˆhZµÊÓÓéãPÊmÌk0vlØ¹û#bZU'g1âšEkn:Á‰…RLk¾2_ÈêŒ>ˆxÈ‡¤‹şf¯M¾wà…p vZNã˜}»-vÿ„MQjÇ}h°o¹‡€"å”Xw¸F‹»mS,1)X©ÉÎ«(ÿöŞ£JôŞ¬U†¸ÕE«R©r÷}¬Ôi^«‘ì±Je¬.‹†ãw×ŸÒ[cOkµÕÍ;ÿ†‘Í1•İ½¼][¾s·ÌÉ˜º¸9l$‰³¦XeÎZ¬ÌÄÅµ2kÍUY•ç±QæÀxbtõ)»Ä†]ï¶ ½+©·hò?ÉÏKCbX]àŠà9Ÿã0>ÏL—©T‰ŠËO¼¦bAĞş˜0‰¯RŒç¥‰M|ê.E¿¸äcûß®ø¯©V¬Ê\5Nıô¬5³¸È'õ…¡ª_>éÏlcÍÙ‹0Rø¥äG“<cßìä­a´» [G¤ûª¹}wWãêã¢¯Êà1ëÿŸAµköÊhkoZ²/¸ÎOõŒÙöìèms"Éƒğ¿!õ TşLÂhÓF¥\æóFrµ°¸H«Şjy4Û‹?*™òùSıã»ç#É¢,¥N[£;ò1ı\ÖÊy¤nn,ûj’ŞYğ¾,0°ûgçªÃ½ç_¢–Ïo¡ü
çÒ¢¯®ï×ü\¼Ãæ¬¹Å†YÃûëŸïúSúnìI×åû7ÿ#]É^ò;*‚°‹u:ä^ .X\	UÊgºJ9•éÑæ»¿@
@>aùsÄ`4OSåïõ4]½¾ ’õË q´.§]rşÙî¦»«ÿ;œşV‡ÓÜÜl†Ã‰òÆq8±¦c:¿cúÏvcüÃ$óoökŒæ#àû Ê3£ÕJójŒÓğÌÂh• İ¼úù‡üœoZuGDõÏôgü¿#â"§µü‰™9Øí û!fª‰©maÚZœg¸ œÕ3ÙctÆùÖ¿ç1Á±8;Ã*xÙw:à‹ËÂle„åï¸ŞŠ™«Şm÷ğhwÅŸ²âı[†NÆ’t¼q’{î…o¬+’cäš
¤WçÔŸÛµ[nİİßZ¾s}£06ÇÇ‡ò,/ÿ†h´…eõÂ–¹­ÿª5½Ÿğ%Ì@úÅ{ÎÿòeåôŸ¹¬Ì'Ù‹.ù/­+óuÃß7“BîçêL†%›æfGš€Ï±«¡Ro>Ÿ÷ùoŞë@§%8tÏàãîEÃC¿P;v,˜ëòX<´EÙï@`Ş`ipønA¹ôbÉó›¼ñ5¸AöF1©@f]§İ=æ…0„I7Ğ'SîôB¢¨Q¢WóùfÛ•?í¢£Çk œ(óŒ¨L_¸–J×øiMŠg¾¶ Å¦Lü¹šL«yğç	¦v²—°¦.VÄ)4V¨’§Puh¡ÈnÃ¼¥å§‚Qkä-]RúLŠ‰°.,{V÷¹&şÜ`&ZJŒ–Œj*šD\©²Äü%£ğ$Ê	3(ëÜ˜B‘ˆgÇğ=ó´eb0ZşdIü£pì#WËˆò5A“¾/N7:b(hÌáTaµe»~ŠßD-©àèÑcCêBÕC<C&x ÃIŞ@¶…õ5İÈ$1‚úK"‰KªPKü·ê^^Ç× x¨å;}KiÌÎ‡îÍ]Ë¤†I„} G}JÇ-SáÔ4|<&w½<Vß$U3Uˆ‘/b_›¿Øtns5Ê+êš¦´Î#8€ÑÊ©tguä7²r'1x”ÌPÏ%ÿ›`°¹Ì3¦úb‰õMàE‚ø‰Sì-ºK½éµZŞ‰nâÀñ‡3‹AdÁ#»àu³pµbƒN]v;²ê1À!W>¡'‡³+b£Ê†¯ƒ†ñ’7¼©ü}Í¿&ñk¸òí9xúß‰øİ`şMÿ9¸âí~âtáù—tÇÜwòzsK¸uñ7(óüşr+áÕ|¯Òv±6è9¨ş”À{¿€?õŸ‹`4gµß÷1pò¶ZİCè‘ÈŒ9¥G—”tÑ·‰Nê†AF÷<±ïr&¡!Fô\Ğ­ãxkÂH=½Ds8Y7•o½¡è&ùoû/#«şˆ2¿]¢WıÉŸÌŒ$uú‚l
¥k£ÕIÒ¢ÎG¯smb€ÂÜûĞ:`,ÍÂ÷ ™*.ÿ~—Püï^G”áí—”ë¿KŒ$á¶1=E°€òo±¿Æ7ƒ¨Üw£·§„îX‹»&ùpmi@·–F:•ñæô~'mX÷AçEµ|úl·§•u &»g.¶oªV­ç0‡tj®Ù:Vå&˜±‰ˆÿÜì¶ ®çãœĞ„ÑŒáÆãbo¹æñ®5 Ş]‰Çsî tÑD—îJá×…ğùMö…^¦Ş¦5{“µ
Rê«¨ ©‹Ú:Xé(ä ÄiSâp†â#ï‰„d‰Y¡ë·ĞòÆë¾¯»–lï¸vBPk®OQ©OÉ„×:<P0OQ2¤²bëØ](ss°í<Ò…SëìääÚ¥âÂ±ëc¨lùXS†Æ©ë´"P‚¢¸×Nªó‹‚Û£ÛK0ˆ·nmSµªbeó §ğFÉ-ŸVÔ-*—7~1-*Ği6Bv 
ıƒsBÎV?«kpç3:¤0İj·aUïkÎ“<YÈ«©¥¡ÔĞ•2 ğÕ)B[Öz#MÃ=:wŞR<ç8Ò‘Ör`¦x›Ôé•³ï<ª;¸•Êwÿ}t°Æê¤…ğŒ;´ß9#öX$ê.E…:]ğÕ¬8p}µ÷ùO¦Ä¹J;z"=L–f…äv=;½¤Û†rõdd½´8]l<Rû7ÆZb×0Ì„k·0cÇY åB·³4™íï]>±õ©ï%ÈºqI3×	‘Ê©tšşQâš€ãäßM>jáúPx‘[“¢&Å£ŞÃ„ğƒ|NÛNÔâÑ^­^K¬/yÓ„ª ğyîİÎ®0øC0vA•Çƒe"š%BP{N4u¶‚¼˜¶R,:x©‰(ŠYh)aŒœn˜U±òéÂ:,€$º›K^vô¥|,6¯/a–]}o¯şt2:©PbüÀ”ü›srhEƒp˜*³vè×îçeæ(Š©l¥EúA^A¨µø^’,R¨å¸$Æ¿ŒªÂµ3LlŒq8‚J$å‘ö´E†È|òö¼Øì7â.dX‰ošã,ù”şìÈ@:¸Ôäëî·Ì©mı±på£1à²àÓ@ƒ;yèR(˜Øºü×ñu	
ù†{àã— ,qc1zñdc2$¥Ç%KŒ™Û‚ª ´%ùÎoñY8¶o"Œ{üR@QåX^œB º²!Ü`™ò…–D[œ}a ¾DÎ¼0“Ë‰Àñ²í´/äªù ¸*Pk¹Óş­€Âo4ï¡·éÙ:“ÿæ”N¬a Fk]ßŸƒ…v:p3m½oÖÍ¾w¼Ã8E^Ã¼F
Wa`ÁÇÕn¾ÇµÊÙuøÎ+À{õ¬ HˆÉ÷PK?      phjX             $              CombatBlockEN/
         éæ•ÚrÚ                PK?      Ö€‚W             $          ,   CombatBlockEN/oxide/
         |Ç(%Ú                PK?      Í€‚W             $          ^   CombatBlockEN/oxide/data/
         ŸdÈ¼(%Ú                PK?      Ñ€‚W            % $          •   CombatBlockEN/oxide/data/CombatBlock/
         î:Á(%Ú                PK?      /ƒzW            , $          Ø   CombatBlockEN/oxide/data/CombatBlock/Images/
         9±ágt Ú                PK?     W”™WŒ3+Ø  $  7 $           "  CombatBlockEN/oxide/data/CombatBlock/Images/CB_FON0.png
         +\¡DP7Ú                PK? 
     W”™Wõ’¶¿  ¿  7 $           O  CombatBlockEN/oxide/data/CombatBlock/Images/CB_FON1.png
         +\¡DP7Ú                PK? 
     W”™WEly  y  7 $           c  CombatBlockEN/oxide/data/CombatBlock/Images/CB_FON2.png
         Ñ¡DP7Ú                PK? 
     W”™Wß‰Š?5  5  @ $           1"  CombatBlockEN/oxide/data/CombatBlock/Images/CB_VARIANT0_ICON.png
         Âº¢DP7Ú                PK? 
     W”™WTPlª  ª  @ $           Ä#  CombatBlockEN/oxide/data/CombatBlock/Images/CB_VARIANT1_ICON.png
         †¤£DP7Ú                PK? 
     W”™WªÊÑÔ,  ,  D $           Ì%  CombatBlockEN/oxide/data/CombatBlock/Images/CB_VARIANT1_ICON_FON.png
         e¤DP7Ú                PK? 
     W”™Wts¤+—  —  @ $           Z'  CombatBlockEN/oxide/data/CombatBlock/Images/CB_VARIANT2_ICON.png
         H¤DP7Ú                PK?      İ€‚W             $          O+  CombatBlockEN/oxide/plugins/
         Ó8óÏ(%Ú                PK?     ™hjX
™qªe'  ëÀ  * $           ‰+  CombatBlockEN/oxide/plugins/CombatBlock.cs
         î'ƒÄÚrÚ                PK      ÷  6S                                                                                                                                                                                                                                                                                                                                                                                                                                                                  = hideVersion ? Msg(player, UnHideBtn) : Msg(player, HideBtn),
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
     Õˆ‚W               oxide/PK
     Ìˆ‚W               oxide/data/PK
     Ğˆ‚W               oxide/data/CombatBlock/PK
     .‹zW               oxide/data/CombatBlock/Images/PK
     Üˆ‚W               oxide/plugins/PK
     Vœ™WEly  y  )   oxide/data/CombatBlock/Images/CB_FON2.png‰PNG

   IHDR   È   8   ¸Ê·‡   	pHYs     šœ   sRGB ®Îé   gAMA  ±üa  IDATxí]Ù®å4ÓµÃØÍÔ @ $®x¶ÿoóK<o€@ „ó<nY9µ¼W­TNŸzŸ‹]RwÇ±œ*W­ª²wkWºÒ•®t¥+]éJWúSo˜>øàƒå£>züµ×^[~şùçeÏ?ÿüò÷ß÷şù§ß»woÁñßÿíO=õÔ‚#ş=ùä“ËO<±Dy_ë/kYì±Ç–ÓéÔño½ß—•pòÇ|+gèo­÷XÔÑ²eŒ1,hçëı­ŞÚN[ûİúÃq¥¾Òv\Å¸¶úQ>·¾QÿE[Û9êãñIf;¯×gpÍû7¯Ä:l+®gï£LŞ¯³>ÆÄgx×#úewÇƒ#ß=Újü|W¶émÄı÷¹×Ób¿A©m½§cÔºòŒ¾wcß^×Æò/¾øâÿ7ù#=2úğÃçùÊpƒçŠUP¦À>óÌ3Ix×qã/´Õÿı÷ßÛÓO?½•¯‚²•­L]Ö•¾ËV&Ÿ÷Á¿`µÿù,ÏWFßêy $(Çÿâc·hoƒÑ¶s–Ça°ıµ]´5(Tlå(d½8êklíë5û@=ü‹6·s´Ïğ¹m,Ç9ÇÓb²¼‘ç1Ÿ»áÕ›r–É³ilQgè81Ápè	I=Ş7«êû²oŒYûçØd»2ËÇ'ıì¾õÖW{Ä´jtıÊ+¯l#\™n|ÿı÷[Ù³Ï>;?ÊsÏ=aèøÇ™œGÌŞ¬ËûĞ<Ùz´’^ƒé!0¼ÙUÿ°é>êã„§©Ğà\¯±CÃÌ6ÈÔÎD“[…œÁµÌëÛu:>SQÅ,Ó±¾µÙVW_ÅÎ§°éw‘Iä¡ıâ9gYÑn„8ig¶Y}KhmD_|ñÅvüöÛoÇ×_½İ{é¥—¶ã¯¿şºû ¿ıöÛ®­¿şú+Õ³¬eI›`¦ºéšmP‘j 
µ‡×‹Y8µË™ıhÆW
³l¶§m©ö‰öFÌò=³ËlİÂ\Ú´‚Ì°sÆ7†œ“û €Ê,>Ë£¿9j+‡C”±v×äÊ¼2ÓiãĞìwA)Şg÷Œ
 ÇÌ×‰ú]Ûsº„)?Àıû÷·ò~ø!•sğœ{e‚ñÇle0±PşçŸn³>L)œ“q1S£LiÕ:l›íug|Õ üÃ¨æPÓHÆêE;ML™Ìœ`úMH+ùŒØ£O~‡’Y¢î®?é·¹°ñY2¹¿=ZôÑ9nÿF·Q¥ù¸´ŸL9öçMEìùl“1ë¸B(ª1ŒJ¸¶1µĞ:›o€œ×+`oß}÷]ƒ‰µ‚£T÷—_~™ç` hœƒ¨‰EÂºøG!!Ó¯BÅ™v^w€ ,!\ÛµşqÈ¼Vš\,?\§yÀÖ™¹Ø"Ùôª9Ü;bÑI(h×»Í®ÚBÛâx”i¨•¢ïweú‚ÿGæÎ|TÓ°³O|¿#­É¶üP˜}ÔzÔ\*l±hÎïª¦ñEL¬÷Ş{oÃŸşyÂ ¬ãÁ £A{à$fİA¢ ]1Ë A =øG FQ¯šèĞSB]h‚s{jÕFŠÈ:ãÛıô¬’j-a€Éä!e;ªAx-BÀ¶æµôS™~Î°›)Ãw7mC ½µ#“­?ÿª­»8´;m[É4Æ4çXwd‡8óKhşÙgŸ7ß|³¿õÖ[A!4
…D1Ì
B,¢B !&QìmÂ#…ç6Í¢ŒJ\R®Ä:¸³È…‚ÿ¢nSÍú:‘ød8
”3­2Veóû ÉÃ™LÆÙW4Ãn&§©Âçz¨“÷GÛréoĞãÔ$+ö¡ÆÆ?Ä-£ğ6şIB BÎWjŞå¼Xéˆ ¤¯Øc¨ÙUÑ
âç}x°hFA0€;èòñÎé2ÆË=V`zĞ¸‰‡¤¾Ñrºq.ŞRĞÈ$l'úp ¾1‰˜f	ŒKŸsŒŠ	ØNÆ4ÍÈ ÊÈ®9ø¬3¨™5Zw>¯ãiòı¢Ïí}…¹™İÇ>Î€Ÿ&‡åff?ğ¾Qã-öM¦¶œ¢¦–÷ŸÍáöˆ‰qh–ƒ<xğ`Ö‰µwÏ"şA­Ì?($$â‚shbC˜fÛ½ºj¨9E_1Ç¡n]‹¤=n Ğ¦?¤Íüê9e^ÎşGm%o”27…ª0ó„cb›Í‡Œ'y±†y®ÌIÑcˆ¼A“§ƒN8ÚwÏñ	öMavgmS½óE4Èûï¿?¯Wf+@OÚ&Ö4ÜÎÕ‹µâA\@¯ŞÁ¤‚&QmBA‘àÛPÜAf…à@ÜûÄx‰
Í‹™Ìˆ˜[»?2Íš#÷0Û/lıÀHx!®›>Oo‘áŠàµl¯»–İ½I„mÙı—I ƒhİ^{¡vN
Î"S o™7xç¬ ]Dƒ|üñÇÛ€ Ğ¿üòË‰?!¬Zc¾İª-¶ ñH%³Š \½[ôli¤‚îÜY·K@Î¢ìXH"Ú‡‘(WÔÄ!ÑævTû"¼P“yÍ,Ö÷å¦Ìñî80FÔfv&!ÇæciaÿÓÄ²G(ìíˆd‡õDÛtcï†ƒ¤şn,UÛw"‚ëÓO?İ ú«¯¾:_Ô1ˆkéIT¹üºŠÅRQD6Œ9LªîqQ¬BÏŸUpcˆù|ÒRdÜã¡ùãx„×ş®4µ˜¶¢³¢â)K^£±µ³İ¸ßtÌæ¦UÍ“¾m%túÚ W˜³û‘°h}¿çÄî§èóıNh¼Xï¼óÎvGõbàyòçn¢$Ê¶£j0‚©E¢.]«¥›7rµÒq9çÿP§ é³®ApM&Ñó ¬›×+¶)Ãß¦ˆ+øf2‘›*Å3Ó[e&]óñH;	D&Ğ‘º8´ùS¥3Iu·wæéjµĞpÜ
Ü9ŞZ«´GLôbAƒ€àÅH­À|0’NÏÓ½{÷(×Øb!Œ¨ûL
Ğ´MV¤†ˆ@bSsË=Z Qxµ@ıì¢<³Í…J}ş¸§ZÈA´˜]é	ëâUâ3…)¦i%Iµ.ã&!(|>½š|—£ëìÜB+Ôí™­LÒ[fû£éŸ¥F{Šª§¾zöbM“P4M«èb¹XĞ Rƒ@{@‹¬Á4RbJsP{È ‘ö>ˆGÜôrW¯˜Sj—'-B/–i–%Ì²íÒLsOÖœi]©AÄ£¤Ş&u7=FİÄDÄGõ›Í’#†â½7Î®Zg*íôµ™SÎ(:»C™Pl·8SÈB«íÌÀvöf%·n¼_ÍÙ*ºH '‚!şP3DÍæ"H§æ ÀÍKW/Ì-ÄEHÑë$àÂÚôšåî`ÜgZJ´³	™z°ˆ)œyb–OX Ê·\->3ÌC¥3r˜GSû¨&!hŸ=§¬”«6½h.•f[ÓT[–©1gÔœU¥¯ÛÚJãª„¸Ÿ­µ]€±Â:‡},’6ËÚ…	,˜XôbÁÄB„D½XøØ é ï

Hq…ÂqEÃ+ÓÓ»5üR`ôhÚ¦S˜ª¸GQóìÈ„èâ=Û	…Îœ4±¨ºí-3hšqäÈè±‡#-—&ôq½îævuÓQÍ —3ù‘iâÕ:’øöÚ>µmÚvABÒâW_}•Ê¬aö€0 ş{ü£Viî8Â´"î8×†L0­)‰gñ2>¢ª@üÈÌæ˜ˆÎØÓóÀŸš[Jd¬rÁûJ˜Ç™„×}`ÒV¯•ºÚ^Ue—³eÏPÓ”@¹ßäb1ıa¿•9hT÷Õ¼[öÓwCƒhª‰®(T‚¹íÁdE%¤™@sPP4&¡1\»§IA¹.˜²Ø@bxx½§`„.Ş®@ZÚg½™EÓåd<ÎĞ£p[jı%§˜ìê²ÚıVï6Ñ¤cA!éqí^9Ã£JQ9pÉ³©Ó‘ÓEøc]wø}ÒE—Ü‚ Òƒ€<Yî]^ƒ¬š…®ÜrE¡âh7'<Š.Â°XUDÒ"³Š5À×%~1¨]Äüé¦êÓyq	gúrtÊÕë%ãoş}ª6h¦Åõ<º69tmwèµ8Ôô,ÇQx¼\pÇmõi]ÒäS|"ŸsØXK;ûh"æbÑÍûòË/oå¾`
ëAhb€A 9Tk{,7ëÉ7óeû )ƒP‹àÃè*š\.,â¹#b7«º¥‹°-âgâS±VÃÚÑdÀ”¬XÌÎCM41çš‰ä:OyÁÒ”iÖÕï#ı‘yÓ¤1êu^	ğÂÑÁÇ[«SKº¸ª9YÉ;ÒÔq_ÖÄ½ıöÛóEéæ%é‚)šW<˜3<4p‰2°›Tôh!öA–ÔdöE6qÀ‘Ù½Ç'Œ›h*ŠÌø§ègmèõè)ëêúşEàqã#®‚!õwqˆÂìÅìì¶{	²İ LİZxc]+Jé˜’çJ5YÏé-š]Óü£ÊÇ½»ÒAIgº;Z„+
ij¤C{ ¨sU!>4Ì®“äJáÈ%·<')H‡¦ ?Y’aµÊ7gàštÖ5<ĞøËÉº9.çmtæ¬^úh'EÉ£É§¼6#1‡¶¡¦TÔëGGíyí“CPÀ¯ÄµI{‰A»EŞ¤dß2–ré9zßzáòí¦SŒÿVÌµõÕ.D0±(„p@‹0ş£®(tbÀÉ·ûá¹
ÓNTS,Å¦»¹5¸ÅÏIRVxGÈÊøòì·ğ!æØÕ©*L/½æØøN»gfVŞ–zú^CÌ¹dû÷³»vÈëíÚu¬†mğÕLªo­	gê{ŒıR‚å.$+*HG ĞM,‡‚tx²ÖëyC¬!y6/p‡š\JU$×Ôøhî>,ÕD#ã¼vwî/V?»‚éÕÚ­4ìçTjÌ‰»œAõhfË|¾åºùóU$=îÏ×iêgö1ÈfXm4Fö:í˜T½×nbÅ³Ì«i})›š2LÒEëîJ²"HW²Bßò8dší`^,ºzIŠ;|-‡äCÓàÇ9zíæI?ÚK™ŞÜÂIhÒf ¶“ˆµ”»oµéô“\€ &f³Y}İ$bíS´…2~s
+™AÔKa)úíúnİ¢û­ÖxiLtØûí}	 éÀˆ¢«áÆq FÒÈ¡A p÷2İÂh:´SK(ÊÜLZT3KM"&+ÒT¢v	Øz°âzšzãœæûm:¶1Æ¸Í>> Œb¥İ`”äµâ,,³ù0à[FÔO¦vRÌÃ£˜†©
»Ä1uå÷{I~¬({ÀL‹•šgŒQa¬&×ûÉ¯=bÂ¾XLwç†ˆƒ|óÍ7Ø£·ÓÍ{ÿşıíÈT_á` B‚áe«†™n^O{¯vN\–ıV¥êÅ¢ĞøztmçdëÓInV)Ö è=»]OçTú4v›á–Ê–·²q4›G?ê…ÓİPš·«×ÜHC…råeì6Ó‡v}i}vùa­Æ›†3!ía±–vBŠ	 é¤wß}w®ùé§Ÿæ½^xax6/4Ì,n·È6?Ğ&w6ÑgU[0å„÷pŞÅıê®*íDŒmÌÌ^îvÒ{ã¤›=I©ÜÆ>å-ƒ¦†
Si7&š4Ãò‘Ì‹•HêÀLç-ùêQšäDŞÅ5ğ|†À¼KÜÂ¯í<·ùWÑ¨Ş‡Çh$'‡¶GL éH1a6¯nÑõ 0Ç¸„Ù¼î±ânŠ,Ss‹»¼óÆÑ¶?ìG]ÀG[â<„a‚rÅ#4wºp­¶Ãû)T($·[îJ/—3»«fnT>ÉzÚióêÊó­zxÖó³ywíp<ş<«´ù™Æ.VTêÎaĞs0p>'ïÊošbGËÅrb6/…ÌæÕÅRJñs»{*¾³‰Ë«Cî”HaP­!»ÀOS^,icşÑİ8`hPïõ¢&ŞÃõé Wédéğdt­ãÌIAÅù0<¤dØŠBŸ¹­Ÿ„ø¾m)Òx¥¼eGæÛaÂc1®]İÀ}ïw$›×s±¸/+
¡- A kyÚ´Ä@¡gôÂƒµÄÖ£\0…ršWLAÁ¹kIVjnUæ”
M1bŒ–¡»™pAŸaÅööOÖœM{ßo8­õcü) x*Ö¢³½SÎÀfÚ"kQTF‘Û{Êsêf^›eåµ0mÂGíHÿÓäc¿NDÛ6“q'4ú¬|‡Yçbn^0Èë¯¿>Ë Ò×¨ù(Ä¶?øWiÄAN‰wÜ¼Ú·­¶ ÕÀ¡¶¡E¯‡¹q-Ã¿À"iïC\¨…™Ø@7q İæë~Ğü/™QwÌr¿ün4]BhJÏ”¾÷(|cŸ¢Ş®&å³}Å$®9Øn·TzÏO“:
²5{9¿±NH½˜€À‹Å­G¹&]17£!ö Xg6¯&-jz	5	é(‹ëA4¤E¯™Ì›Ímu¡eô/ ‘ôS$$ªÉ£uoÈl8ë:Í#EÕiª…ÄM ;ì¹Í„d9…å´Oı®fä™v/À™•G»
ŒkÒµªï;²‹·?ä¹ªñ~¡E>	k÷/ºyµ“šXÀ!Ä Ü8´
aû0À ø‡«vuåŞ¼$`yh:}Œ#™W‘?ÛÃ=ş"ê®ÚI5È´ñyŒºqYÚ$yYneÍ.[“>¦p¹©F—îòğR†š'#Ç(è£‹t?ëÅ»î&÷Ôù9ÛoÅ'XÎ;µL!ç8ÈÍdÍuõV{wm}^zóê4"O5A‚¦ÖíG;–Èæ%AƒøŞYÕ(ğˆi<ätŞË*¹p¤ï\Ã*8Â¼Ÿf6æYQ´Îb­¢=·ÓÌR®fKg^FMµ#“j)vOQÍÃ™µµ´kút½6™…#§t$í¦š‡#^µ‘Â"íS¿™`£„tHÒŞ4ñ
\rYŞL}sM:³x¡A~üñÇ-P¸–wÅ ÕÆqtï2i‚@–ÿº©²Ï™FB³KµBü,ıxjûG›tÏíGû-‘Ên01EÜÛmØõJƒø0—æ,kI‹£j3Æ¨ı&p«ıõŞ½P=§z–zN]ßŠ¼-srÅF½R³è9'ŒÊLUeËq-w!Yñˆ¸íÎ¡xN‚2Õ2ÕC5…r²X/µ÷ã%UŞ+šU‹m²vS–{«ÈàG	‚š’¢m¥ºİ"éô`ˆS–ıš2´n³q+î'Û]ßã¿¥(v£D
ï\å–UÏ™jç~ËğÙ×P-,‚–0Ç³S³Y»—ƒ0İ©&ÜÕDÓL¸hŠé$îj¢ÛşğÃ©69Eº»§œ@(¸’Ğó®t+?œş’IO¦˜fÕÉ’ãş´wÙ?—´Ü¬2ìüc.Å†p•FºÍSå&˜eb¥nB¯+}íŞ©µz;Ÿ!Gj'`7ÛôÑ^€èeÉ¿ ìnï»µıÚŞÛIY»p6/RN˜Í‹Ÿ?@&–z±@HV„÷Ê4ÂæÅ‚&Ñ_™‚¦`ú	êA0ôgÙx\$ÉğaDï•‚u1ŸºFÒÇ-‘Š–›-~J³DµÌé µ;)Ï(¯Úè.<*„£¶­¢~öh•Z +¤c™{Ğú-Ibé7éİâ£H—qŞa˜ØêÎ˜XüÂ7Şx#ÅAPÆdEhnûƒtwx¯àŞ… ğÇsP‰ŠL-!¡É¥Û ÉÌ3èò´Y}j
%‚óËj—¼áÛnu£î°Hêp3“f£Óy7ÄÒK¤í˜—$Úcõ¹¿ÖÜÔºµs–®Íæ½šIeÜ-İM¼ÊD)Ú©‚€jvŞúlô1ìÙÄì6Î¦c´ñÎçûA0ñ"Âí~`^©‰¥Ï"é4­ üƒæXbW]û¢@ÀÌRpî	‹ æcU+
Ë£òÍâH.xÑîV÷1ûÍÂhkˆ¥5g°%oJ×Š>İtØoFÕi÷SDàºŒ£i[r­¦Ê`+õ=(ï­µÊÎOøcXŠ(ék§5˜:rlcŒE©'Çu'¼X%­¦U@;b!\â?¨yÊ;”İIQM.ÖÑ_¹=7•+SØyÎ{šî&‘
BùÇĞ:æ³geÈ;¥Ú;öq­€µÌ#ñcìS'´\T5…Ï®í€|? é:«O“HÆ•0ƒyµ¯Òİ«1ÖÓöØÔ’¥±è$Ğ$FºBDÒAú+·LVt‚‰–~Hİ¸Z—Øúî”U®^FÅù³Ğ¼Çsz¶üDÊlG1Š»ƒtYì¤£ÛQÁç°¸æîğs\ıì½é^iºy*ei–¶6G†S2®Ù†ã®˜8Ò8Lp›“
?ë™‹;i¤*ÊÎïsÑ@¡'+ÂÄB$}õ\u]QH7/L,hÄ?4%Ä7¯&éî§X@¥B®(T-¢QuReN$/–š L•ÉØúãœ8Úì¢İZOëRóˆ pû¢Ã(´i«Ô^7N6AsŒ1gbo¯åX·"-k“¨¦PÁ#ü†‹Ÿ…£Ÿ×›Ü7/–Üê¶?$h‚t]rË5éšÆĞ5éºí“¯G¯~DËˆûf‘z?ŞöÇf>5Ifª‰¶M[Ü”î¶BİbK½Ó`btö%Ï>fáCÌALsàr-kb	!˜eŸ´G¹ozOî§1³§DÅ~€=ôó±®›—£¾_î¦¸³"#éŸ|òÉ68ı}¸z=şÁ•… èĞtóêÏ0H¸X/ãÜög±|,ºsñ¡Tb­–‘<iOÅæ ¨ø£›A<à×…bl³ßÓyzÒa3–Ièúåóúf6©¤TÚênvş(TBô¹ÃBnZõ}ßá¥ŞoMzäı~K?}Œ;°ä&–î¬B,„?twa¤ãš‚á¿r"¡GKWä¯Îµ4±˜_Å{ÔnJ…@%o×à¡bùQ\]8ö±‚I4ÃÔæ=Ò‚§sš{2{9H–l|šÖÎv¬4ˆĞæÅâĞ}\…y2
AÙá	ØqÜÉe‚—ÌOÓx­"€~†oŠ}f?qóò^,hş0Èª)¶¯¬Xûïƒ¸‹Ûşè®&si]ºEWZdıÇ‘w
AÃy®ÏF>İ»\µ[ã®æÏ‘KŞAİ¡Î¬*g[cìvıØ˜Ğ¨GÅµE²çÇH¯C#_„wj)ÿ›ô=ğŞİ·6Tóìòşó]]0õ;iºq(¼Ò•®t¥+]éJWºÒÿœşló
/ nÂ:    IEND®B`‚PK
     Vœ™Wß‰Š?5  5  2   oxide/data/CombatBlock/Images/CB_VARIANT0_ICON.png‰PNG

   IHDR         ©¬w&   	pHYs     šœ   sRGB ®Îé   gAMA  ±üa   ÊIDATx‘½Â0…Ï©ŒŠ:%%ÀÔ‰Â%Ş€–	RĞÌ»ğL\¤È“>9ú|>ÿÄâ½_aPœ1æ)üÃ¼á]gÿËdtxÑê6¹sLAÁ*0¡ÿïÖÔà¶àê»¨ŸIÃB–`,}ÁDuq<Ÿ@¤àr¾ˆæ ¦ÀªçÅ—¡£¥ÈC7)Zu#Ş¼'¾Àšİõ]kú¦­ãÍKvÛs±ğx;~_´N?62 ú“¾¨vÙ'€©·    IEND®B`‚PK
     Vœ™WTPlª  ª  2   oxide/data/CombatBlock/Images/CB_VARIANT1_ICON.png‰PNG

   IHDR         ĞZüù   	pHYs     šœ   sRGB ®Îé   gAMA  ±üa  ?IDATxT½JAŞy€4‚¢+ëøÖV¾Áù––ó 9¬,ÒÄN°³“ÒÊğçš@ ’*B˜|Kæ`nnòûÁÇŞŞ|óíÎìŞyÇ ¢+pşzïûÎ tg*à8ƒ®£o”Gİ0©+Í‡µR‹ŠˆDüÖˆ·²xYx]w1€‰f%ñòœ*a“Ç6èU,ô³ã,`õ˜ò¶BcÅ\¢´n8ù¬¬0Iùä¶ÄŸ†É7x¼‹Ib˜´Á*x½.ñ|æ².“&xÈ;"ÖíK®W˜<ÉúÁX˜Äª_1å!ÑN3y!#ğNiÿC¹º¼‚™Ñ‚&Z~GUdfe¥Ë™pÎ½4É.ä)øş(ÏF_lRy ñ$r;97œ;«œãİş+]·hyü¥tL?eÈIĞò    IEND®B`‚PK    Vœ™WªÊÑÔ+  ,  6   oxide/data/CombatBlock/Images/CB_VARIANT1_ICON_FON.pngëğsçå’âb``àõôp	Ò@,ÎÁ$ûugGd1·032ÌšRÁXäîÄ°îœÌK ‡%İÑ×‘ac?÷ŸDV ÿ §‹cHãÛ©¼‡D\ã—/zÖ`yàŠ#§óÁ‚‘5|]Ì;S•—¥¤¼ş}Û3o·ÎYÛÌúyYt¶Ïºi¶l§ax²yå¤§¼ÅÇml‚ÿkm;ÃZ™Ô$üiÓûŠög®•dµİîõé²Ä´é>,r/Ğtí÷ŠNàL]ÓdÃ¤^µ£ÉY‰CnQsµıÅÀ‹=a<‡)ë¿Şe¯uF~ª¡mÛN½K|š½åä'µ;’ÿïŸè?z{ÇkÎCk~/¼åôƒ§«ŸË:§„& PK
     Vœ™Wts¤+—  —  2   oxide/data/CombatBlock/Images/CB_VARIANT2_ICON.png‰PNG

   IHDR   )   )   ¨` ö   	pHYs     šœ   sRGB ®Îé   gAMA  ±üa  ,IDATxíX;hTA½+„$Zd-ÕÂ•d;1`§ø!*X˜ÂO%Hba¡M\¢¸ÊnÜD£…•(""6vj!²‚hg!"X¨7šøDñƒ¨x=Ã{ÆÙ™÷î¬»›&ïíÌsÏŞ}ofvˆæQ¤lÌ¼—vpü¥R©Tg Ï\ƒKU>ä)KO²?À'àp!y ñ-àUğ%8ëĞ¯ø>æd\$ şš@ó¡à=–áŒPoD¨÷€¤ààg‘b0A«è¡uİ¦±À¡íó’ŒB|ÙîÁ¥@rŒÛ]&¿’j†¸C½Ÿ»q¹@~øikt™üH~Poú¹èChø.ØA~°æu™œ%?¼«›Ğ z–‘?¦Ä‘H´¼!|ØßD?5®iğ×†[T0ğT‚ğ'p…“kÃY0Eµƒó1â;µ8é\h"Oõ „v€_ñ‚Öï3FøöS=Án°&ÕÚ÷³?fÀÕÔ@8êSÍ:ğû¡ÂÆœÚ0„†gØO9Ø5Í`…ıpLS3€Díì?^¢fÉRà#öƒ>¨_ G\f?ôicW‚ãa{‰şØÇÁ.ºÏhÏ±ŸY›bp¿üfÄäıİaŞş$ƒæªQÒú¤/‹ŠÉjãqõ4¥b2ZLÉè?gò™%iÑÃèsp¹?Äö/¡´­V¯ãLŞw$-³6Åà~»Àà°#_9Îd\¥Š1qçz…ƒ®õşŸ8—Ñ,8á±=™ wÂbĞµczÇÒ%“å­ ˆkÕîã*˜%`@WLEOSPãz“ì³é@ğzí^TQ¡n\»´¸IBM	=J2ƒ®
N°ıeH2È£®Šª­Z[‚ÁVğ;'WĞ¬tŞr-yÇµ˜Nğ­Ñ¯NÇZL¶qõ¾SédbFÈE{Jx‹‘Ğ¬¨èqG´1j|§Ñ¿™ƒcETÀ˜¥Cpït€½£ˆøUü¡K;ú{Ù¾Ó¯:§¹º‚kh€¼=\]Ñ)Õ1¨5¨g¬‡æÈ¿•ƒC‡CQÇZğ 7ëOR8xö•Ÿ4:â/¤ù¨ËÏ]L    IEND®B`‚PK    Vœ™WŒ3+¹  $  )   oxide/data/CombatBlock/Images/CB_FON0.pngëğsçå’âb``àõôp	Ò›Ø•ƒH2Ş¿şHqxD30pƒ0#Ã¬9 ¹â w'†uçd^9,é¾û¹ÿ$²åvzº8†T0¾½q1WØQ¤íb¤–§ŞAUÑL²B.9¾`ĞbĞ:âàuCèÒÁ¢ğMlË˜ô[îÆ?÷W~ßwß®÷X/ÃƒùÿÒ.›øŞÚÆwßâ¼ÏãùfvÒÅ¬5¡§×=«=ÚüÿõÆgıûs§­ÛPŞï‘¾él¶ÓŸV>Û¶¨îUüœ£1ŠùY?oÎ9¦s}_Ò¾—òûvnøX-ltº,¶ı]ìÿ*Ïn}X·3jßîÃïwİÿ5ùgnség»—[ûî:X5çOìãoÖ™µ§âlÃU!cÃ#><†ÂÎgsqeÎ¹ÇÛí­ç½–\ÿgaÿÏèæR‰öÄ…¨<«Ú¹şéÂƒŸî~ã×ûË°g=PqŒü«¶»…æ™=/kg¦_Æ´ô¼âğŞ{ñU–{¾œÍœe¶õÎÕ~æõ¹|û4ßdÛÉñE3„=±°Übíèº	©®~.ëœš PK    Vœ™Wõ’¶·  ¿  )   oxide/data/CombatBlock/Images/CB_FON1.pngëğsçå’âb``àõôp	Ò'€X•ƒH²7-;¤8<"‹¸…A˜‘aÖ	  cq»Ãºs2/–tG_G†ıÜYr!.!Œoï\ôerq¸¸=,3ªñcØÕŒQªì^ìÓ8Kò™rZ?ÿqpŞd¶òŸ{+×´t20<è7İ:{õÕWFÛÇ˜ïÔc>wZéúÌYëƒó-$=Ë9ôàn‘mºÆæÃÒsÌ‚+_WX¯&y²âû¡£Í÷¶Y¼X>]FÀ·ø75{2Ï¾É]ˆ?sİÒóÔ½™
‹ê´û%rÙs,m×íæMØP»ÑÚq“Æ-GEYûoíàx ÒèĞê°ÀI!@Q`… G‹“(S#†póO¾ÑÌ™³–ÅÄ°]àMI–:òû‘Á„íóØkMÏúw¿•?î¦cñŸÇ½ï’ıÓÅWÿ~óY³èsüš³›OÄÎPùâ]¹çF÷âoK¾'æño´S?áòğNÑTæÕé’Çl~ğÛß¨´Ü¿BáÊ`x3xºú¹¬sJh PK    àK[Xl
¤'  Á     oxide/plugins/CombatBlock.csÔ<koG’Ÿ©_ÑbÁaF¢äW,Ëê‘˜ÙÒ‰’÷­ ´8Miâáwf(YqÄv²»'ç½îpÀ.pÀá>+´~;aø®ªÃ_²lÄœé®®®ª®ª®î©îN`»{¤~„¬57¡¿™‹ã°Fh{nĞ¿Æü„¹Ì·)ˆOo—:öRuìn¨Š=÷6õÕÛ-vN¯š¿	zíVïÚƒN}–-1Wì]Ÿú6rêÖœÎí¦j>¡-f®w‚Ğ\ìØªjÓµÃ£e YN‘y‹…‡ŠUí†İbd>„EÉ®dÿ@Lk—†×¸³Œ¼O¸@CĞ¦–œ¸7QØª¹MÏ(jMŠeR´‚«Ôñ\¶°V›ÚdŠì‡a;¸:5zí6omú)­˜sºXÚ(´;»İ ‡Ñ’«ùN ÓÂ{>Ûƒ‘"0Õ2aü‰¦5
İ›C(ß> !S`57d~˜ZèØÅ|²c«’<èİc›5 :È’Í•‹úG×hÀÖzèÊ‚ñvì4´× ÆÂe‡FIGÓ ]
É®ç9d…º{ègÙÀĞï EÀ ôqèvÚÌoÕö\P Ñ!´(š6/(òfï1×ò‚Mxë¬Ùd>é@á–ï:“EÛDuj[\–erË[´ÍÊ¤ö‹û4,“A“]+(“E‡ºğ³Ôa„â¡L–˜Ş`X&4@Uè€nxÕÊç&4šTSq·ß\4µ ®b–¡É»ÍÊª±ÏÑ¤NÀJ®4»IŒI^ÓŞÓuü&µ]ı}Õ{ ï¿O&â(|ÕëœÂ'`âRs‘:Î5$êºQL_,KšK1.Å~•*ÓDÇ)5Ğ.GpŠàDVØ²Şè€9ï‘NÀüÚR™ˆ·ú{,¬-%,; ®·ã8	nd=gÆ(Ş ²‡b9F£EBù	üiŒ­3‡{ê`ßnß¤.İc¾Ygşó•½›BŞ£àÂı£OXx›:f¨^¼N˜‹Gh6#!üSBÀ³ÅZ»`¹ )n1±ˆS²ã25¤ª~åk®ìx,E…vo“·–èJô×RŠÀ¾¯6c)–æR6d£ H0&(ÓH˜çcÊÅ¢£×†ZPŸQC)¥¸"TCÆaÇÕdt99ƒ€Å	í%îí~Î¡´œš’Yå%‹´±ó.]–OCÇ±íbGdRJçË/•/$“ºdâ¦=tæÇ¶k-Õ–zİiÔù,è8èÊ4_ªÆlùf5äÅ±ı×¬/‘-í@HÆË0ğ©$*J\è`]	ë”sÂ!}¶â‘È¢”3¬¬º¢V§gz‘ßû ®Â{À F‡ZC¬É^á¶ƒ0Şv“âËÃ¨P¦f-uª*ÅÁªÛ«&˜4>1…‹yX×úàQ‹B îº\­óâÏ¶Hz@\Ê]AÀĞ=V&93l FB Ñ‹Î“éf™`cÿÿ]—ñ)˜ÿÎgªd¸­¹>#9««P*	9Åœäªç“Õ8’¼¢FmZs°)4UKõ-]Ÿx1kîw‡F‰Ì§j®³–wÀT³²’JI:1¢ıaÉ"?:>ŸwL1~*Z‘¡‰(¬³0ä¼ä@”rÚù;¶17kU‡ùáf ­¸  ”ƒ¨®Õvª+Ëë;k+ÕÏ–×w6k±–ÕKVèÜ<Û¶ÌI‡•Iz­5Ÿ5%YAML 1.1
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
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            ´Á7äÃşÍ¹,\¸VO<ÛoÛ$ŸÌ"Â¶6v;åËŠmôî<ë?Oj˜kñhÆÌ{”á=!Jl…ä}añ>”µÀº31¤%ĞÒ&—äX·áv#†/\ú‡Bïç¢Åzõ!9ˆíòb#/(vqñYÄµÓº¾Zlµ®ùzl[Ø¶g'Ç¼¥A¥i|îYçş=|ÿ6şş.k	BÌ5Š­aİ¶½Æ©\AÃkÛ=R„ ÜÚµÚ™uÖ¶½N¾å¢- ÖÒÁ³>İ>³ÈƒÒ”ë`VçöÌß2½fÁŒÇÖ¹"…‡+?åÉÇzƒ1<rŸÁÉà±:ÙvÄ^ïš+ •y£”1Ö}j,•N»5L´Çºô©#ô»¿t1KåG’Ó]5Y<Öêâ±~™ACÓFí)Ë=uÄÍkÅÁ¼æ@>ßİ»c>ôW°§¬¿µíôÎïm;œ¼4‹¿‘Ù!Ñâì"ÑÁ÷{&y6ŞÙÃ£F±~—×¢'¯×øşÏÃbcøı]züJcsD:äßĞ\½á®p¬ş¸5+kÊÍ)++9
oÙĞRMÛÜE©½n½!î†½Û–û|êˆ¯õ†¦7ÄÇ€jŠ\¢/¶ó¼”5lD“Ş°^oˆ¢²ô†øTú;;K¹ç~P??é¾("ü*éş#ŸØNÎÍa\´G¯ÔF®)bŒ‰·›¶»Åg±ù<Ö—Uy´Ş0İUôúTÑÜÔ••Úå>¡mÄ¦T:KohÊ,PÃ Q‹\¼´m l-Ê…Z*è¡6Ü¿ÅzoŸ³¯Ë­jĞAhÙ÷Yû ÜgÁ|	˜·Õ‡•¹YÖk_Û6®çÅz8ÈyM5#Ócà¼}±+}u˜8~Û/Ş
 {¡ğp±Æ'»œ2xÃ¢Õ¿Ë¶ıñÔŒˆğşığ­y<Öìâ±T×_l'sYÃ,gØ§ùSs½ÑßÂ %äßò!‚ÆÙÜ¼ÖĞ"â¨ı¾ÈÄí‘ÂCa}ÖÖˆkOÄµClæb_Øõ©µí¯3çÂ»£†—'Ï¬÷şªays
w„“Á±,’<İ[Y¨ÖêöÀxJîá±µƒqê`Êp|°[k›…oÛ¬1Î?^ö“³°‹nî œ½¬u–ÆZg`­³F²ÖY¹¬mVÓm·n¸Ë¶ñp”éµ¡oæÉÅ†Çzr—më¯÷Y—í€8í{<³®B«3íŸ®	Ìºñæ\æßK¹¢c¸9íq óªúlıáf+oõgĞ ?vO.£ïJ£+,ŞÁ¾ûÁ°ø{?¾lwûh8õ¢†'ûßšZáô_ëôøß ¬Éi—Ü“Ë¬y nMß\úo\eÜ‹Ÿ•û¡$ËôCãxiËĞ~è{<£~>ˆıNåyĞçT¦>ç_û ­k…~€vzx-\úaDì…¼‰·aÏÏe‰T@‹ùSĞŸ.‡‚˜Ïu£~uÅh§_­íô«£~55ûUÈ™æ‹ĞïŒƒÆ±ú(hfú#ÉkN‰¶Y)Ë¶Í{,Í1FÈŸš#Ò\|&öY¡;¨zâ6Û¶Í¹†ç´¹Æ¸ó“S§BıÓ""ÍK7Z×ß:ğ¶‹mçäóD&v¥ùÀøÓ¯…Åwş^:£Êá<òúm8±<Ö§Ûœ3#OÜ¶ ƒ3“{ëõûÄ>k®Q–™¶ğ¶ Qì‚çÖ/sá˜‹âHöí¾€y©/±QG\0“œå›—zæ$ö„ÍßhÜ™‡O@%)áz|?sÚmen¸¾­Œ1ÿë†Ç4¦¿4ÊÊÎyé¤ˆ¾¦+`†X"µô·ş-İG;ùÅÖ¹x¬Å(¼-ŸÍÉ†‰Lfk´©°y³f(oŒëkR‘Â®ÊTØ\ªQS5¸õÂcK¤*ŸíE…Í›ÿ{cÖ'ÂY­Q0ø’şú£Œ^‡.¦ø/?šù¾eßànÉ|1/lÑã˜ö\Ãcœ2Û;ÒÃK›à`Om^ì‰ˆ©ÍÉ€Kdã%YAML 1.1
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
                                                                                                                                                                                  ¿>ÀckK`ŒSœHòöQÖôç ©uæ·¹Ø'BÌœ¡9Î‰Û2Ã_UÇEBCšåš¯1^
f†4<çŒÙĞÄïlu{XFPæcœµøï¶í|G©em}Ö¶Ãb­¾Úm$owİ¡nC_4\g…Ü¸ b½ñ,Ìo[pL°4?äOùm.úü¹9Gïÿ.lF-K¹ô÷˜In½Æıks
;+;yéaèLŸY¢Y‹W8èŠÌèí¸§m;âêKo‚áèëá‡œëá±İ^nŞPÎÍ;Š¡òÌ‰˜'ÿi•³Ô„s`ùç÷ ÄŸìÃßDáæ8v¶G¹ßÇ‹·³•9÷·^5o¸?ÌŸ²>úÃÆ“c—âãÍ^WIj»üÇ†q¬Öy˜ìÛ8®¬­w¢%NÔèDxqNÔ¦œèB'jw¢“œ¨Ã‰rœoèd7¡Õ–‡›çAâ9Ö{ÿú)µÎı_x—Wì§D/‡£?íu9?Èà\$Í|ÄÛRVKÛòòÒÍşPYsŸ€o^èë~·g.—b¶İå¼~û?m»û3úØ1¼t³³Š$>³Şÿ«ms×B_÷ÃƒÚ7ğêø\—s?œóáó­ûŸ´mî¬o˜3¢‰Tåpç'eZ¸y{±õÒ
¼Ûa77çóä´?ÌÊeÖP§qrãOYìJÍw~ÔÍ¼İëOÁ¶X·=kÛm3ğu?SËM».õqÑÃë£GÑƒmi÷“Î(Î¼ÃÓÄÆô²'l»ÖŸRî/‚ú‡µ±ş]›©9'­„*wAyX|ËÅşÿúµÅˆ«ÔÇÍÜ0Œì¶e¾Õ:òïĞ¢X™z¨Eğn8<™õ+(¬©CyE~.ıÉ“™ÁŒ¯æ…Åw<yÎ­3sY`n@|èÜœµl÷rcº¢zb#O³pf.Ó:>Zö½‹›Ëƒâé‡°IqgÂgÛBÒâIˆOBùô/­â„hÕÇ¹O¨-äœm¡b'Œ:ÿT9ÿàÁ°-‘ùò¶Dæ¿Ù×–ˆü•yV=3YÜgİ÷T>¼u¯¥q<İÀêìX¯u¸ç%.ûbOÄ™úŞjÃ¸ã¨ûQ| ò‹è<¾l-ìéWŠ ¾'Ï	†pZVÊe±ƒY0)K<pÿÎÊ§\Œ­dayü)+ò”m‹7}-X§=EÜ&Z>â:Ãˆ|Ã;4BxÉVã"T„¿q*B\OÜ8¸ŞàOÅ–xmıá•
6p¿û/ˆnçb“Øë¹Y¤ÙÅU\dÏÇóu–—Cÿ1ËciŞë8SËü´a­?ÅÛfâ!¢öÀ<*>çS.ğâ›¢SÌYnn^šÏOš©qÓİÌ©ÊÑİ'Ü¿W”>š6UîuZq·ÅŸú¦Å‰èÎU§<%ğş†P-Êí¿øú¢ÿÃëåÿûõ»»ğw,Şê3£&×¸YTUİˆÿ­¾[‹xéØÍßÀœ‘Î½Ò÷¹øŠŸvçf56Ï±‚›c:Ã›dóÆ}{ÿqıÅöà$÷Q½!¥|ßpÜb8¶µ<6¼Êù+Óş*±™sáëã™Ó¬é™{Zævô™w–g1³oüƒ/çSù‹'p3ç™×Æ³6D<7œëQÃ¸yk¾İÁcÙUşÔ|iûÉœGà=Òú&°–Z^:3'xúÿÇŞßÇGU\ãøl²KÖ¼$–¨%K!ºKve¶ŞÈ*Ai¥ÆFT¬7%ÊS`w5ã°[°jµV-õY«Ÿ` j5ÆV­´‚Ô‡»ø"ÌïuÎÜ»»I-ŸOŞŸÏç÷z}ÿÍ½sÏÌœ9sÎ™sÎœsS¡Õ—P·Ò$¢Ó6sëêè´ÍQk}¶õvÊ®/C,]xªoé¿,ÃSĞ/k§<Gi
iÔÖtSùm¡.KğÌPWV°ÌÕ‰‹)ÿ±ÒÔ¥7yüÃ¤/_Î/×ÉØ/¦øamsqÁ'+ƒz°ß×åøëòˆ[iê„n•¦i% &£|Ë»–Ÿ¯ƒ%Äê*(ŸAéø[`c¬¹RO›»²¯Á²Ñõ4š/ï85ïÎ¡ãÚ(¿¨˜k¡ìîõˆGO	e3è×}øwè»(8~ŒLj|R}ù­Î1„‡­^°P±™ÆëÍj–2Ïò×}ğÕ÷oµ±MÎTÑjßWÌzyª_»:õÀ0Ä}Şº–<.^o2ÅxıÉ_ë"ë4^ÿ`òá†ä¯'’¿I5Læ¨wd™¿ò“¿
³’‹Ì‡nÉ¼İ’y××§`%™·{üoüo|ï^(ÿ”Åí•ÿQã¥õ±äx¶&mOuaÖú›ÛH«Ñò;ÏË#ºcV2ñĞÄcR_p¸Òä¶…cÊ-ÓÎTîŒÜ¢4µÏemÉ|txE`Ë]À¨ş‹tÑV3dJ}I!îkİµ9¢bßBy=Ğ#ĞË^`6‰´!a¸ñ×Áãæjìí„¸C§×‹1¤µ"?›Æ³¥cüaN*q<?–ãQq<Y”Ù®5ÄÒ+‹€<²(/¸èYóÕ¥òÕş*ù*ıVÂlW§±)Æíc…¼ª]ëN!ø)kqÅ";êrdÎZ=äá/ø0‚RBå5»ÂNùŠb‡ä†ªz½‹F5ÕA³×l·ŞtÊ+MŞ\Qâ$dfÂCÇÕîE©§‹¯®†}µŠ†>ï¢¬‚Òq]”Ip¡­…†(Şr4|>}ZŸ)Ï#ñóíƒ!›àıı{…h­È$e`/úèõıõòû¥ò{8¯mÂËÌwı{#¼µ‡FË}åy$=ë­yğ3Ox¥úº€€ß4À’{äºÛêKIí8Ÿk¯7sŒb·eqŸëŸk¯>æ<ÒQfUı¨NX2¥Nıá	KÏ‡6¦.cø#ñà¤ÊÈ'ÁQ­>ĞN>ÓØAıº{PGş™Ïõ‰œOxg°âí~„òëÍ•‘O4Åûixg­Nù4U?ü€¼ù)Mí¿Gô¦™v¢±¯ÒÌûŸü^ˆÄ!„¼ßñ'excİ4Z~ú4‰.İ<•*¬±7hÜ«¢®h&İ‚%Æ¯ù Øx€<£Õ«Î´è®™vT’¯î‘¶õA(ìUÂ{	^¬y{*èÿĞ&Z¥º0Kµ¨¦bäÅ,ƒ2µf³¯T©ø5SŞ{V©^¼üA§
ÇÓğ*tdU©3½Ü«z]1_$¦øZ¼êLWLo9,„k¬o6§U^¥ISgúÂ;‚ÃørÕµ¹Àğ&ïxTİ®X¤³ö"Ê«ÔRT½>¥i…%÷³e£Tı†z8·yZ½j¾ÅÏòUŸ| z™ô²€†¶.ÜBÙ.ıá{„èØï½jqÇ†ÿè[?"(ëVÂÏ£Gaìæ§`sŸ`88[ºhhãÆlä”xÓƒâ1N”áËôÆÛ±ñ”ÛV<™Œ˜‘N¥}¢MªMühLù	4ÒüCıË_Bî®‡S.>GöÊĞ²htğßqù›÷äĞq¯Óì ¥ì„ÄÕBÍ“cNŞÌ<¢©´pĞ)¸¯—ï.…ç³n-ßwaÑøB;å¶'ìq\@Gı»ş’ÄŸ †J¯±=@ÿ÷‚Î“]‡Ç4WLØúÖzí‡³Ë~`?$÷A:ıkæƒ¢ô-“¾SPÓÖó5¹9´ÃrslÅÍQÓ¨„ß’ÛãÍÒ<¢yÜ¶G1lBØZr{àV(Jm…òûq+ë\1ı.)Sá`Ìæ*¼„.SŞFƒª7¹ÛJ‘ ×´c¶¤Ëd|$<¡ÕŸíŠ…–«^"óñİCIæUKìahü]êgVÜ G§ÿ»ú_oî«ÚÑ.×{?5Ö¹ÈşëàI{[¿åé<µ&ÃÂkPÿÒ¿;(ÙôYæÙf<_X,sÈ^v‡‘½Á‘´zêU+i"¼ã™ššÆ4)Şñ ±Ş»a…uÌw>Å"u%{$‰y7""âaušCª4­V•KaŒoß)¯º›Ô¸0nAÌ1nA¤ß~pÅôÒƒ)?p}Éu%xÔıÆ¾Ékñ0ş‡ÔV­©Ÿv‡©d…ï»¿
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
                                                                                                                                                                                                                                                                                                                                                                                                    ov1w¾—íöÌÃó÷\ls\«íŸØæxø6~¨4Ùâ9c¤ĞV¼m/åŒ!aSdåmŸµıü1d·şÉIâ¶ç?t)åmşä$úó7Ã9’m	@£åç¹òˆş\90]1}ı;aÌMÚOi¤öqap­.¸€îy-gãØ/_`Üñ³÷¦·ä}r£šÆ"õ?ìQ34_†NØÃ"ó‰ÀÚ‹0å0­.õsá³K©¼ÙÕEùF7tUA—_ câ<l«~¥H«Ñç¹Õ¢Eo°(á3Œ‹#E/õ„fY0*~E`ÜáèB_u‹´¿q[ô~'ªF…°åAÑ7$ûĞìôaLõ]œk„Æn1/óâúÅDi¸MÆS¼,®ïnÀ^˜òH¯X(·|¿3Å5ÖI«íFÛ»(û‡ÑaÜªb„ë²5†±*BŒ;04dRtñ… ¿o¯äWÙi´üóˆşæÔT|ß¼=àŠéo"áÅxu-¯97èOMZFíĞh#æº)¯€&«û5yšÌ‘œyÅyD¿lª]KÎ:x¹q*€8G“ÀÑ|åŠI~õ­BxB×Y Çón‚}€µùcûø^ô·0"	RFW;åÏàbµŞ’~.I—ÿ³Øvm¡ì ÆvPökÑ¯	®¨Ôù†ŸGCñ’úUù–àı'Ç»RÅÏ¼†Ş)·]toÊ´„ü8‡­}¦cGïøP³ ^!å¶Ñ÷&É]RzN&BŸh®ıæÚKşºb¡Æ‹(·ãm-¡|N #×`¤PPµc lŞà®dû5ö¹>c%†OÍ¢|™Cã^ÕÎgLğLuOXâá3Š=SİÅKK#±àY®ÎšF¶}ÃQk½¼‚×ìÏ¦%J“×1ªÔ3*˜İàjµ”--^ú^a¿œ‹YA¸92Êmîß9	Ÿï¨d]HÚ94:j$åùç€ "•F®ÓG+÷È¤{_6|¬rà'™ågûıœ‹Ã/ø«‡ò¥FO5ú°(³ºø¥Ê+­^G¾…†–Õ‘`À_mñsZ6½|=4ôEv]4ZşÁØ<¢¿Zj'4Z0c,è–qÏK@¬›h¯Ø„×rV¥ÏpĞìŠ euL—%”ÏP>Í¡ïh´³ucZ
Ã>iJŠ5~Æ	÷ Ñ”ş#ãÈ×Ë¿1J8ŠşbXÌ¦Úó£ü,-;T0Zømıî[„Ç‚sá€zÏµÃÏƒjÀP· IğùÊ7Ö£îÚ ğC-C3ÓOX)„ù1]â5^VcBHÇ8_¤å‹ì­=ÇÏ«Ô… ¹d´çO©Eò(”¦}éú9	‰Ì¿›ç5İ¸£Õò¾øaƒš5¥¦~"å3|z‰ÆÏ²ğéÊ'X€éŞxå¶GïrÊ"xû(/ÑnFtœàß¼ßĞŠüñ›»ŒŸòŞ?($$„$=3°Ôï¿Ô˜ä§5ÀOWŸGôåSÒù©qŞùSDˆÄ/Œ|ıÏ{”puÂ¡‚2åú+Œ`U{i+ıÀÿî2âRœ¯~ãLË=’òKİ”—_vè-htò}	ãéÏùØnÿaxÇ'áM x—~/<ÏÃÛt§	ïÕ	¡ç±¿(;Àë4Ğèâ4M}µ¦ïGßß¾åÛï¹éØÚ?´ôØÚ_qŒãynù±µo¯?¶öÏã|—¬8¶öíÇ8ß–ğ±µ_rŒøÙsŒëõÕÊckÿĞ1â¿ıÆckÿÕ1ÎwØªckÿ\í÷¶ÿ
Ûm­;ª¾•±ı	ıh§o{¾*À—À€¸ÛA-=Ô×Û‚ı¿ú¾öûûQ´ßui€Ó¾ØA¹u(eÖI”{‡Ú)óN²Ã/ür^T‡‘‰wKç«ÉÇ¿ásJ€}×/Ë·aö{FçıÚ’^>ËûVGd}²ø8åÙû,>CvgÊràØJÙ»ÒT‘ß0#_n\û9lSÈªIæ{Dı¢;=:ı[C~Ê#ÅÃ»W-ñ³V-{i ˜ç©_™oSÂË¥÷pï©yDfb¯¡¼­vKÄƒ%Rxe ılD¹ÛM™×e÷Ès a ÅÃA9å“š@±&O+A8­,Wİ4´•n9üÁ2óøfü·zÕÑ[]ª¾³ªÊ^õD??õøÌ«ÚåUUÔo·R¿\=‘(á›ñØ—¯vNaU"¯£dU;İ#²”ğ5ò6æ½ë—Nr6ù*åÏ0CÉMıÔZéhŠÍ­sbèÂ<B«³ü|¶©ª¡™FbÁ35^g¯ŒˆàXÒ”3mˆnÏ"$«âQšfXàAs!ÓV)á—°“¹vÙT±ğŠ¾K6¬4U`Û›±m£^¹Êß=%$¤û£`×mÎŒ£(Û¢„9ÁáhÑò7OÌ#FÕcé3ª½9ªO-©QjŒªÅÒ{TÏmC5²|Ğ)yD¿ñ8~'MÀ¦
ôğriy• l_€Fv¼Ñn;ù¶>˜7¢d~$ŸD3íTÂ—£AvYï)Œî;…ÁÆ°¬rXNº{&Ø¥®×jÉ±	úÑc#h åÄï0³Ï<Õ!í]Ùóú³7šel¤ù+ùÍ<‡]T©v–MÙ¬>°Zİù6Ã&ö=†2‹§Ó«æ[jO¦¼àëµ}Ö:‰-L8‰×å²W0qÿå×‘xE^Ï¼—;‰ÆK1E)å¶µN…‰òÏÆÛÁò0~BQÑŸßkûƒş“ø«l~¡ñu‹eÙÎ3¾.íîí|©éöÛt~t¤W,Õøäkåá‡ñğãiu³IÊ%Y„Ô×•Y”0Úæ«›ùÜ|çÏ§ê7®U2˜lW	BˆL Ÿ2ˆou˜¶D$·YvÊv¥iV~C ß_}<EDV½ŞEù0Ê²üÜ«ö±Áæú1•õZ8´³z”&¯jØ_ÓŸû±â\?ìFÓK#YàQ –—ŒÉ#úO·“Íö$ö¯¼G›ø®Ç”!ç.•	y6V©ù½ç1d‰G
“e»üR¤ø‘â™òã¥è˜±¢¬	Õ9ˆ¾®‹T{edo°È£4ùÈ´!Á!áXmş°LR{-å¶‹¬v1–ïšGô[ÇÙ¥«}G®˜¾1_!ïKï‡‡åøÙ¥ö$–ŒË6¬ÔË—«V_D(‘0ÖrÕ®ß"QÛ#Ä´U55µë;Öšc’
4ÇuV8V{œ1®à¦­
J£å0¢®âäˆŒ~Z‰“1ù—í78üwjÌû1ÿ¥¼¬dŸùC­%Á•Áå4ZŞ:*è¬¸×>yy)vs;6®rìbòÙàÿ˜|Ç“Ñ”˜\ÓKLîè%&k‰PNG

   IHDR   2   2   ?ˆ±   sRGB ®Îé   gAMA  ±üa   	pHYs  Ã  ÃÇo¨d  €IDAThCåZy\•U~/‹"›Ê &n€¬
¸¯3¥(¥’JfÎ˜š5–¦Óbâ®5¹g:–¹K¥ân–¡â†€

^VeW@˜÷9÷~×Ëå.˜ÿÍ<¿ßáÛÎ÷÷9ïzÎ…şW S_¶|èÇ-”[7nnÜì¸Yq3áVÆ­œ›œ[·KÜâd2®_/L„	òa,· ¬¬l‡Ô´6òÜ«Â‚B‹òÊJYuuÕÕÕ‘¥…%YXXĞKNÕ®®*;wö,õñê~Ÿß»Î-†	ıÂÇ??M„	„óáÍë©©İ8ávõÚõ–EEETV^A>¤ªª*zòä	=}ú}ÉÄÔ”LMLÈ¼U+²´´$ëÖÖä`gOõÃ†ÎéÛ»w&/†ÛV&Uƒ1ÏM„…êË‡÷Ï%$ï9äz)ñ2İ¹s‡JKË‰ c222çRS¾§jõõõBKõ|nanN..ÎÔÙÓ“F{0lèÀ+ffæÛù½]âÅf¢ÙDX S>Ìº{÷şäu×w9õ{,Ş¡ªêj2kÙ’ŒŸ õ¤¶Vvqv&_ozoÆ»…~=zâÇ_ğı"EOıh&áÎ¦ò?z_LŒÕ©ØÓTÍ¦ÓŠÍ³¯¦lNµ,ds MÁAÈßßÆGDĞ!ƒO:98,ã{§•İtÂ   ¢¢rÑÚõ^Ùwà İ‡*„Äàšğôğ À 

 «×¯SFf&õ	¥“¿şF]»v}âÿøƒòòÄ¹&ğÍZö+;;
îIóæÌMéØñåELfŸ²‹Vè%Â,+/ÿòëÕklß¹“=z$´€YÓD{¶óI'RØˆádgk+LmÇ®İÂñ#Ç§Uk×ÒĞ!ƒ©WPÒÑcÇi÷Ş½:	A“Ğvÿ~ı(jşçYÜİ>æq÷+7NÃf]ÊËËW¬Z³fÈ¶;©æñc2gÇÔ†Ş¡!´réRúË°¡f-TşRRRLñçÎÑÍ›7…&û„„’““#µiİšü|})¤W0İ½wnË›¦éY·o“\.·ñóíá¿fõš´¨¨¨Ûâ´j„I˜sX·nı7“¶|ÿ½qMM™™™i5¥ÁÒÒÅÉÑÁnee‘ƒ½µæĞ
 ôâ] ¦áL8KÀ÷ÊÊÊhå¿¿fíD+ï6ú@;ÃX›K-Læ€0‚5ÓDº<uÆùó"~Ş·ßø!›“.Yè‹Ï?$`v»öì¥¬ìlåSNç,4rZKl	|+óÖ-A¼M›64oî\6B<ÓÌ9(öôúñç}¾|k¥âIc4!Âƒ„æåçÏØmYôà¾H`ÚH´mÛ–æÎ™M®íÛ‹kdíY3gR÷n]Åµ!äååQÂùâœ3ëC11Ú`Äddì/ûÄPrÊÕq,Ïå#á¦?Ü´ù»—bawÚxcÌhá¸x.õ16ÖŠ5à@Ã†Q½ÛÁÕ•¦O{G§b2srr(jñ£Ò²²…|m£|$ 9rØ¥Ë—œ<õ›‰v­±ÆNú˜ƒ €ğÕªÕtùÊq­şzZ*mıa›ğ# ‚ìßŸ†$®5wÅX>:rô˜;ßšªx¢€ŠHÆßšÈê³GÉ¡Ë¤€×FäÈÓ†®]C½§ 9’ÉõğöVŞÑô›üö$UtJK»A<Ó4.b¬N­€üŒSAûä–OUê	HMKóN¼’$"¤rMÀ7ú÷íKÖÖV4üÕW„K@ˆ-.)Q^)fY}2¤k4‘´´4¡IÀÖÎ–Z¶h!r¨÷´%MzF¦Œ«8çHÅİÆD"Ÿ8Ñ®µ¡.œ&ü9şcĞ¤äjÁK‚‚|\\¼÷rrsé@ÌA• HŒ7m3/áfz†¨’'GG¡‰ººzQ	è&ZÜı~GqWI„¶ã”|õj+ÄuIİÚ€òIÏÊÊR„OT± BìÜfS'w˜/^ù“¨"‡€üÂx!ûİiS…†ô©©yLÕ5ÕÜ«—Nó •¤”dËÚ“ß³Ç=I#ıäòÇûlúŠ@€³½8¢Q÷ öœ;R®]%â…–ĞŞYÎğ0»c¿üBqññ‚Œºùâ=ä LÒ¡#G›è9ñ
LË
‘P^µ-++×«D«k©©sE"ƒ¨gj ææîæÆÑ¨¾İôúÛÔi9émzïƒY{æy{uç’Fa:Ğ€÷<=:	¹¹yÔ¯OQSBbR^“¤è.Ï‘[U²ëÓHçô˜g³)ù„¹Ç«ÃäöuÔÄÈ	6üU‘‘cãâ¸®êIÃ_y•'
‰í %\¸(fÿM.(»qÅ÷`¥¥¥äèè@]»t¡8®’uAáô8õÂ‰ˆÛ»wZÁñôÙ¶cùùù	M¨~õÉgóéTl¬˜ÅA¨X…¿¼>J”’¦·|¿•/[.ÎD¯¯–/SM
|ÃÚÊŠ6oÙ¢×¼0y\@Ø¸–¤¶+/¯0zR«ˆ0ºğğQMŸ2EU–H¨¨¬¤+ÉÉâƒŸ=— *?0Ó0?É²¹šUGqq±òL˜ØŒéÓÉ•3½!<(¡Ş$"VUUÕTÇvİ¨;)€|Ym`Ìª»;vƒšb››© „Z/AúvOš! PeXâDÄ6ui
©ä‚¼‚|åÕ3ÀñQ@¢¤Gá7ûƒ™èïßä[¸¦åK	sûì_óè­7#•OŸ!øAñå•n@f†°Y1(6ãı6{°q}d-øB8²fLB"r²¾¾o èhë—ššF‘oO¯˜À”Ë+ùÖ’FÊ,­,4C©6¯áEæÖL%¹! HKÉTÈ¡İ+º'´o ÂN&‘sÖ}lnŞJR—N @õ *_,U18²<6¹¼¹pé’JxÌ¾ô¬ìÛ"&Ë¹];¨5×‘®í]+ÇÉeM€ "„ÙÏ‹ä˜C‡•=š,‰ıí”ÈúĞÎqÎøV4ÌÌZ6‰lÚ€4Á	D°¬"’äÑ©S¹µ•µA"wïQÏGµûúkátøÈQAàš é³		*ÍI@_//Úº}»(‘í±á»qã¦²—n@#¾>>8½ˆ?‘¸.;ß·ãªÖ¨üÂEl¤cVí… ¼–å…6ß€çÎSñƒÆù}èOy\ì‰ş‘l¹.k×î%ÊÏÏ'¹ÿÓl`` ì/×‚4ıã“yŠ}+Cd@3‡Ì]¬HJ¼³ÓÈ³fşC”!šÀ3„_˜468P£Ø4­0	\Ï5¸8·Ëàó,Ü“4Ä<¨ĞÅÙÅ £aÆ°TÅ}¼½hË¦Ô©“»0hËW4„k)Â—ÔMùá\ªœQƒücLx8>²Cq§1‘S¡Á!âB}0M@@,¢ĞÀñzN×nø†¸’æ„VL}<O”ñ |hÉòbë8qò¤áêå‚GRR’8×›¶6rDŸîVÜQ#Â{bbjrxÔÈáÎÎÎµrôøq10€{ú´ L¶¶6¢²;ZØ= Ó™üÖ["f>›C,ĞôÊé3q:·P%À7Æ[ÏşÍ2«:«køaPÿ}½½…:ôibÿVê“_P(ü¦wHˆØÚİÿuØ0ÕªÀÛrƒö¦Nù»Ğä~^?âº
f‡ïéÆr`?;f4œèKÅ]a†ÖÖÖÛf¼;­È×·‡Ø×lB#‰!”N÷F£ğ³ùÀG$lŞòEÿô³8‡Iö¶,,ÌEOâõŒ.€‚Ã¼9sjy¢6²¬ÂÉ%hjdvøûbõÕb³YŸ‰A+ğ	˜ˆº­¸—””¬Ú\ °àõŒ¼a!\ sül‡
»Æ—Qâ´®k,Xbgkã—‘Ù!;;›å{¶›¨	da+6£>Şe6<{=ƒ‚ÄVªô¾½½!W¡Ÿô=øÈÒ+E®Ñ	ÖF×ö¨°ï2ññünó6±¹c®‹‹Ë²¨ùŸİ@Ò‚3êÊ-ĞØ†o7Ñ¡ÃGùlÚÂ€‰­Z»NÔWpo÷Á»º€oâ7Æ%£J8“/fÙ•AçNCTTTöêU«øùöº}[Ş:[.çoh×ö´âÏUÖ?¼lm!ÖîpxÔT¨Ÿ ›6mÉ­cGÁ@pÅ—_ÑÎ={µN’˜AÂ…–-YTÖ¯OŸµ<v#W‡v{QpŒ\³üóQn§ãâeFF21ˆ6BXË`÷aÜÊ2»°¾Ç†Äö»ô:7~cM”öéİ{=7_ùH+XğAùù_ïÜ½»ûÁ#GMPéÒ B^İº‰ß¡D%l5İHO§›7Ó…sk"B„hvì†ÈÈ7îúxù,áq6ˆzĞ," âÌ‡ÕÉ))#æG-2»ÂQf¡‹Ps!iØÈÈ˜ó„=ısÎìÚ×ÃÃ±û7…ïŞŞg<·<è;<»Ÿî?x°İ®={L33o	30Thê‚‚€‘ÈcÆ<1º¢ƒk‡øÑ'üìY2€?5•L?²¼ÿ¨ªjê©ßwØ³7Ú81)Ùè)Ì…ÃKMZj 4Ê¦×0vôèº‘aÃÚÛÙ!™,çç·D‡çÀÙŠß'°µ?…óAèù‹“d·nÉrór©¤¤D†ò5VœùÚsuíááÑàããÕTïää”ÎïC{™€şBK^Ì¸ÕÀ¤ğ/MØPîÍMú7'ì”cß	a»ßÒ¿9¥rCB‰gá¯kÿ@ô_4båÂ‘Å*    IEND®B`‚                                                                                                                                                                                                                                                                                     ‰PNG

   IHDR   2   2   ?ˆ±   sRGB ®Îé   gAMA  ±üa   	pHYs  Ã  ÃÇo¨d  iIDAThCíZyPWÿ†C¹D@O@@0(rx±ÉF”D!º94UFK6[{¹+1F<6q“uc4I™J²xD4Qc5â‰7^ˆr	
   ì÷{Ó=ö43‰ÿmí¯ê1Óİ¯»¿ßw¿ÇĞÿ
4Òç£³³³ÄğÏc$?®<yXñhà¡åQÂã*s<25ŸOL„	„óGˆââîW®æ»””•:VŞª´×65iZ[[èáÃ‡ä`ï@ööö4À£«Ï ¦aÃ†Öªåû.óØË„ñç¯Æ¯&Âùã¥ËW®ŒüşàA¿‹—.÷®©©¡m#577SKK=xğ€:::0—¬¬­ÉÚÊŠìlmÉÁÁœú8‘»«<ŠŸ:¹4:*ªˆŸ·—Ç˜TŞñKğ‹‰°PÑüñúÉS§"÷ìİïs.û<UUUQ}½–X ²²²$ñ]Ò}úñèÑ#a¥GüİŞÎ¼½½hØĞ¡4cZÂø©/ØØØmæû¾7ö=&ÂXóÇŸ««k_]ÿé†áGQeeµ´¶’MïŞdii©›¨ ÙØØHG$¬¤H=ho„½½¼($8ˆ~Ÿ²¨2tôèı|ù>_£›i="Â$üYˆ¥,üÌİ{÷:f;N­,”-»	„U„Ü\]ÉÃÃƒÆF„Ó”I¥+DŸnüŒ.ääR»(KÁA(,,”æ$'Ó”)“{¸»¿ËçKÓL¢["ü‚1M+?Úğñ3»¿ù†jjŸDÖìóx¹€{Œ>Œî1Éºº:
=šFA'O¦ø)S(3+‹nóı½{õ¢¢ëÅTPX(\L	<³ãÊ•9––,ş[¯ïà•Lf·4Å(Ìá‡†7hµï¯ığ£¸Í[·Ò½{÷„ 5%<ú÷§§ÆFPcc#]º|…îÜ½+ÎO2Y÷‡Ó¼W^¦ã'NPaÑu1„]\\èÌ¹sTQqKÌW¢İÖ‰¡Ôeoñ÷û;¿wt¹LaÃµZí¿×®[÷tÚæ­Ôvÿ>Ù²¿+­ øDa¡¡F‚E¦Qeu5y²«}–f0AşTDå_+ «ùùÒÙÇ€ûu°Å&ÆÆĞªÔwŠü|ı1™é²Œaaí¸¬_¿á“¹Ÿù¥e[[›Z5	5n!ğahš-H˜ÈÙnR»`ÇYë™ßÄSñ›”›—'}¼Ö‰gë®^¹"—Â4~fFêc¤œ>}&y×î=–ÍìNÆHôqr2 Ñ‹ı~:kŸ4C§Q V“ i(™ì»r|ĞĞ€!ÒÕÇ€"Pƒ?A_ïÚÂ§ş©»bˆ.DXàñå)Ûwìp¨¹S+|\Mš7.’rrsõñ aÓ6o¡Ò²rqÜÕF.¾Ã‚PHhHˆPLFÃïÜıÍ^¶ÚÅÙ,Ïï¤Kzá	Ö,ĞŸ6~öÅ`¤Xø4¢øDàªal¾1 &~>yJ:"¡‚Â"¡ c€2KKK)uÕj‹ú††|ì"]P[$áÜùóq‡3hPèbÕ€¿Rnnp¹àüœä$‘‚{‚!şş”ôüsuèÒåË¢Òã¹j@A˜ËòÑï¾÷çStWtĞ?…j˜ôËl>7´Æ\
ğ÷ó¥Æ¦&Ô°ŒlÌ=ôãº^\,»jbC.Œ¾7w 2ZœSCÄ÷k\
:¹¤ğ;õ~¨´È˜+W¯e_È>kÊEüüü8]^sN>cÄ¨HÑ=wÂŞŞŞÂ’ ”R%¥e"NÍŒsØ5Sùpºî¬!‘äô¬dk@3Æ€‡ÃôMM"ãÈÚ ©ˆ1a¢`®ıúÑ„ñãô‚"«Íà¬†ûdò¨w_d/¤Y ]´·—§ø®Œg¦ïØ	wyMwV"Â&rå‘{ñ¢mCCƒşåj¸¸8‹Ö)¹­µM+û8²Ö–méT}û¶8¶±µ![&-[íü]èöq–»€}ßĞ× ï…BnUV’—§q" ˜“—«aYÇ²Ün8'[$¦¤¤´-÷AÊàSéÒİİ]|™áÃ/Àı¨ö˜‹ÆµÇÁAAÔ‰ÇÆDó}Ã¤;$\œé>wq±±mÒ½ (î|ö¸–z"ãyaäÜĞ 5ih?”ƒp<§Ç<„&O9kà^ ëêêÅs^;—Ò¾øœ¾Ú¼‰>Y¿&MŒ£›%%t­ @šı°\]}½p5´3-m­8
+fÓÈÎÉ{ai-ÖÒÀ¨’ÒG´×¦,W9üc†ğkd4@èP^¾²¦Ú… ¹QÌÌú‰¢8N¢"#éçÓ§éDf–¸g2·ö8a·mß¡ï¿ eX½YQŒ¦…!ó1ñG–Ú¯ªºÊgÎµ^û²W-ÌŠåËhk?}Ë&ZóUÂ‚xŞG2hùªÕâ‚Nûí3ôÑÚÑ¼¹¯PÊÂ…4Ş<ƒwÂÒ÷y,Z¸€z©c2îËËËq£/å'¸jµÚH‡]QSSK85Cƒr["©4"<\d,Œ		Š¶¨+ús½3àÌ	D	h:ıët„[–ëÅ7¤³Æqçn>DĞÊD[ZZéaG×îT´YV^.6Ô@àçç_“ˆÂoİª”ñÓÏ'Åu •»‡£Ü
©ã™®öÎéŒq`}ÄpÀ™ˆ%s™*„ ò¼››Èv€;¬ùàN§ßŠæoÍûh =VÊşHŸlÜHKŞZ*]j ±ÊTÆ 1H
ÙIHÍê¦¼Şwÿ"k˜"3rÄN‘}é'E³§|Ú„õ(šÜ‹;‘)1¾}ûRŞù³M,¯“l‘GûNa(†¢J#˜Õ€†zJÂ\Rqíçª¯òæ "°òÓJ¸i»oggÛÅ_•@?„E&ŒZ!-ÁÅ…R2Oç¾}ô›¦ ¯áê:RŠc™H¾Ï@Ÿ&ì š#‚t
ß…#saÛ'&z‚¨!¨æ´¬”3a‚ …„‡…Š=2XÂ‰Ÿ§Nñjà]Ü5€ö‘õDr†Ñ:9:™%4³ r×jËEE/.6†úôécĞ7ÉÀ<Ä–zå,¾qƒæ¾ô’˜s­ PôrhXaq5X$$8_ÏâL$“ûŸZW×~İACçÁërd¸¦¦f‘_œ3[´Æ Á‚GÔç¹.¡`óW}¢«X½»XVG eáXav—˜L^hLUõm'ˆ¸Wÿşî‚Ö(Æ wÜ±s—¾+VÜ¹k·p)¤ZtÄPY7ë~XÃßÏ¯“[ıBş.VrJ§Ş;uò¤Jo/o³/Göòè-Q}ßxó-ªªªÇÊ8Aà*á^ê„€Tö€–²ÛŞºÕuÃN	<sVb"âc‹îŒ!‘Œñ‘ã
‡pS(âå,VŠ ¬Ë@£xAÒÌçEë¡—,ş«h2X¸`>…Œ~-ö…Õ)ÉëXÆà~	Ó§aÒ6İ6Ñ+k«ogL¶ÑËËË¬U°ı[Yh`ËmË÷‡ÑmîÑ 7*à ah5ÊVÃ6*ö„ÍV›”ôÈİÍmË¬7Ò"@Ú¤Ø¸³!AAÂ¦¬+ &Ä2¼xiŠlƒD¶ĞÙsçEŒ ¸çfI©~'{TpÇ(^ß€,ˆE›¹ÍÈƒ9I³f"7¿¯;«ƒfØÈ9|SÊ¢…5ØÉÀ®¸)`‹›Øı@ÜÈmºXîÊx!i–¨72 şÍ€ûÃ¸[Fæ3•­0×™‹è’Å‹Ûùø|*¹µE@fë˜°°ıl¾ö¾\L¹4k,œ”ÂÊ€uFğ’V¹‘IevƒeP”’›wQºbøÒüÓññ”8#á¦Š
t!"aÕä‰qYc#"DÆ+»ÛœVc&DIgé›²kØ™TïØÃ­ (Ådí`k ‘œ3;©q+»‹«%ÂË¸z¿›ºlişÄ¸X¨¦È`İŞ›ƒn¤|ş¹Óõ=ó¹DƒjÀ:nª&(.«W¤Öq%_Å²eK—`zñÁà‡$ß¸ysÍÒeËËÌÔX±ÉF	Y ,QA>wÃ&V–Ğ4îóô 2¬‚hmªx}¤ è,áMï®^Ù½I,—.wY" ?pVIIé{o/Oõ;™¥±°Ğˆ—ğC¥:@Ğğ1aÜ›Ñ…EEn%°îÇ<¤jcÿàtÅQX¢~BTÔ~ß2é’QtK`Á'±é×nİ¶mÔ¾ßYUTT"j2 ²ö»°‡ ı®I qêÂ'¬Àpæuvç‹/¾P¼šßó±¸`="ğK°×ùan^Ş´e©+m.ää¡;ÆA£ÈÑÑA¬:‘ıĞ“¡9T/ae[XXrp£7ÿ¥ıùÄDü"b>Ÿ¿ ›e=&"ƒ_úZCƒö­=ûöy~•n]Ä™B›JİAGÀBÔˆäY³:f'Ïlä3(/½É×îëfu_L`2ø'ËëÜn/È8zÔ=}ûËìœ\‹Ô–‡Z ,êçëÛ™4sæÃé	Ï6s¯…A¿Ç×»ş©ü*"2XPäVlíÏonnúìY‹ììMáõëš²ò2tÉš{÷t»!¼ÖwrrìÈİu@@@gpp`g$×)l·ÀÛ™€ù¶×ˆˆL
?iÂ†2ª£ü3'ìaß	¥?q’æt…~æ”ÅÂßäÏÿC¢ÿ5iÅ÷%à…    IEND®B`‚                                                                                                                                                                                                                                                                                                            ‰PNG

   IHDR   2   2   ?ˆ±   sRGB ®Îé   gAMA  ±üa   	pHYs  Ã  ÃÇo¨d  TIDAThCíZiXTG½İ@ØQ"
‚ €£ â‚q‰
êhŒëh&™L$&ÆIŒ1.†,.×˜8‰KpMTÔ,.¸f&qTD@t ŠĞ€ ‚0÷T¿×aé~İı3ßœï«~µ½ª:uoÕ½U¯é*éÙlÔÕÕ=Íş‚9tæàÍÁ™ƒ=s¥4ò8œçpŠÃq•J…t³Ñl"L ?¢9ô¾|ùW—ìœòòóí‹
‹lKËÊTUU•ôğáC²³µ#[[[jãÚºÊÃÃ³ÜÏÏ¯¤«ç[üŞ9)Lh??7~7&0ŠÏegwşaß>ïÌ¬s–ÅÅÅTª)£ŠŠ
ª¬¬¤PMMê’¹…Y˜›“µ5ÙÙÙ‘CrqnE>>>µ¡!ƒóûõí›Ëí¥pXÇ¤î¡GÁ#áAõãÇ´Ÿù%hgÊSi§©¨¨ˆJJ4Ä ss3R«Õ".é=]¨­­Rªå¸­¹»»‘Ÿ¯/¿òü++›üŞ&ñ¢‰0™À‚3nŞ¼õÒòÕ+;:|„
‹¨²ªŠ¬,-ÉÌÌL[ñRª«aw77êŞ5€^™ZØ­Û.åübmMe˜D„It`U™ËƒÜ‘’bèÈQªbÕ±f5Áì?@RPEêÑ#ÆC!Chíâòç•ª„Q"ÜAÏ²²ò…ËW®¾c×.*¾…õIdÁ:Î7Ğf5¯+gg

êCï½33ÃË«ıB&³Cª¢ŠD¸Ñ^¥ÍâÄ¤å7&'Óİ»w…0kOÕ¬nö€şı)nş¼Ë;x¿Ëıî”Š›Ààˆ˜D'Fóiâ²eÃÖoL¦{÷ï“µ••ÉRğôğ N~~bñ×Ô<yÿ¾x‘ò¯^qS€õSÃ›ÂóúÓq±¹Ş^ŞS™Ì!©¸ôáÁÚhÊËW¬\µzòÚ/¿2»wïY™@Âœ¼““ğ¥ˆğ0JOÏàív¨e‹–Øöìı	åĞ;wÄ }B:¡CSÂÂø³¼!„1™ëR±†ˆ¼³ÿ`jì¬Ùsì

¯³1³U$Iõ¦aCC¨WÏäÖ¶-]/,¤¢ÆPI©–ˆcË–´ëÛíº²Óii‚Ô©ÓiTÅ¥lÓ*oM“ş6ıÍÍLd‚T¤C“-‡|­  fë¶mvÅ·o	¦D¢[×®””¸„’–.¦èÈHòjßzê)Q†A†Gê îêËiñ'‘OÇ¢ÌÔ°G¼^vìJ¡³™cy<ã¥"á
¼¾õÅÚ/Ûc‹…¸”vÔèÑ´fÕ
Jööp©x'|ÄZ»f5	‘rõ“™ŸŸOq$¨YÊñœv’ŠK$üÔéÓ÷§¦ª`èêÏhc€Ä‚ys¨m›6R~`İ (úğƒx1|˜”Ó˜Pìb<>úîû:pÖ«Ú-tD˜!Ôp‹¯\%•‚:Í5Ó$)¡H)Ãp~úišûşlÑ¶!€Œ9ûkl
êØÄğø¤¢é™}ş|@Ú™tÂ.eH¥àÁÎ˜ş†èØ`s°˜ÓÎœqcpmİšb^{UôaØ’/æäªØËhÇÉmn½]‹Ù-Z¼4qÚ_~e]ÅjeÈw‚ø“–.Ñ-hC€»q1'GgC`OàšòŞ´é3hÿÁƒRNS@Å‚ú<S·%yÃO<áBÜB"LÂ™Cï³™™Ö¥¼]"]>l¨ÑÁ ¨Ó¥sg
ìŞÕ%€üıM~/<ìŠë
RIÏ8«bƒİ‡Çİ
y²jõÏËËo}‹ı(%'Ğ½Ó=zH)ãÛRjSĞúR$ÇjkÉQ+tD‚ù`äXZªQtÇár@Ÿ4°¢/cHKOÇn„£µH—¼ü<ûr>Ùš=øN!CQFf¥ŸÍ¡w·Ç´%·‹€¾Ğ§!h}¢şø‘Gí]t£ÈÇSCD0C'şuŠÆMœD&OÏowî’Jµ¢şùÄ	Z¿ñk:rì˜H7òP†:¨‹Á x¢­úm£/%‹Ïkƒ®]»†Áz!-ÚY£)S?¨nÚ¹Mq‘ ¿èÙg!âÅ&•íŞ»—^ùëTŠ_HS_ƒÖ¬ı»n  âÈCêÄL{“¾ÿñGQ†ÉC[îîîôûlè}ÃíÿÜÁÃ?2ûÊÊ*z(m•Æpëömî¨JJig Î+ ‚ô’‘Gì Â™ÜşÍ·$27nŞ”RÆ!õg‡™ˆfâRò­àŠÃ‹½Êg
¸Ö-Z´Jš´…6Ñ6ú@_Æ I\ìN2‘2aşAFr/]"__úfËf]?öEQ†½ÂøqäÒJléÂ2Oät}ëïäè(òd«ºxG¶-h«~Ûè}*Aj«?búyğWŞ5Ësû7;X ¿]á4ü MÖéõ±0;ØÑ223Å z±-hl ¡F§Ù]ÉÉÉmÁPêÛ\ÊËËyÑ¿$ÚRB{v6¥ÈãñzÉ­ä±}¸occİ`6ÄÇ®´>`@°âSş4‰‚ƒ‚ÛDÊPuíè})“Í‡4¨ÜÒvå¸T"‚Šs·)@;°ïÏOsç/Û­¾-Yà¸Ê'KCÀ$øútÜ#ëˆ¤ó­q°wP$9ªßF4Ô,1)‰Nòùá³5ŸSöùR©a íŸ9!¥é®uùOŠ4~xøñö»nòË/?{ìøOŠ.7éWk?N 0 2Öu3õo.­­M“uÓYçÎÑ¤)6*KKK:|`ÿ}w·¶]˜Ôe!\äõ‘ÅÖ»$”¤‚º”={¥”a`ÀØµyË–‚1 Ú6FÒèàí]Ç$r@yõW[JÈàA…înîb?WÂÁÔC”›«¼5bM,_µZøPˆ#O	Œ¦1`}D…õñµ6§!‘CÁAÏæ`ëÙ —lë6l4(9¨Õú_ÓÒO“(3+KÄ“7mV~gc²îJV	NNVÆÑÍÚœzDXDÌ-Ì÷ŒQ†³€1©¤°ou 5UJ5ThÊäIôöŒ·Ä=â'Œ7¸åâDøã~ãßzpÌ]ËkuYwQ×¸Õõƒ<Ù= @ˆCI*ğs–$&T±?øu¢á¡¡Â¢# <}@ËV¬Òùj†€ñÀYŠ„·X›«E"Ì°ÌÁÁaCLÌkÅİÙ`áV\	p!âšœK >|ş§èqãéÄÉ“" ¼Æª…wÑ†1w$yBfÏœYÍç”ÏäE.£‰œ¹BrÏÀÀ=,¾ê–’#§„ãÿø'Í‹£+y¿iº¶q%>Ù³‘E@yõUïà]´¡è>Ûá"pdDX'ãDA=èuª˜½GAAÁº9±q¦¦ª¡—†t[|§÷Ş}‡‚úôu±xïŞ­Û.¤ µÁŞõƒ4¤ôñ¢%Fı) ÒğjïIË?M¼Áıà;M*ÒÁ ÏÎ/aŸ'iÎüØ.‡±12p¿#G¿@£"Âõ^ı€§[¶n§öí3j/ À7ÆE&Üy®oßùLb•TÔ ‰ ÜÈ˜_¯\ù„}%Ï#Ç«pEcŒ Bş]Øö¥v|ê®°×›Ã¶"Ûdü#N%,,Ğ¯ß2&±@*nE" 7•——ÿñ¼qŞGÿ¤R«U¢nTªñd unÇ’Hˆ+aI¬äşæKEzaÒhxàƒ

®'&oŞÜe÷wß›óúD7!$:|èĞº	ã_¼àÀıèU§ú0y$Ü	nÌ’xq†Í[ou&=]¬,Üæ’%¬fÓÅ¥Íšùvõè‘#ñˆW8ÿŒ¶–2yÜé_JK5svîŞİvÓ–-0fPƒÆöÁTh	¨…U3vLd™§‡çz.zŸËîkkÇïšJ&ƒ,ÓîVV¾zèğa—-[·™¥¥ŸU×ÀæğÀ0Ãr¨Z $êíåUù0"|DE+gg|‚ş˜Ë•­£4K'x ø>«ıW***‚Ù6¨ÓÒÒU9—.©®^»Š*ØÜ‰áœïà`_×½kŸº®]ıë‚z÷®uuuÅ‘ØÊš|ä4ÍSîz`RøK.”ûrÿæ„kÜ;át…Û<ùoNÙğ7'|¸ÂÏÿC¢ÿò
´?–ÔmL    IEND®B`‚                                                                                                                                                                                                                                                                                                                                 %r"Ì&ÓLF ¡±>Tâ	]dÃĞü©E¤Ün7µû¹ogœ~èìúèî}#£]³.ªÙ<e œ×£¦P:v·|"ò¼°§´Õõªw<~§ºr6ä1µÓí6~³l:ğğÛºª6Ÿ%ÿW…PKq#cmÁ/òÀ?¸°Èp˜ŸéqX™?Òğ¥à½räa\«TPí:´	Íë¬µR¾°©$ySÈ¥‹$6bŸÆ´Ø€s§‡tıØË¦zÄ­ƒ{@)yË$‚ğe¸j³êÙòãëXX¶úun™Öcy£ÒVg‡èaõ,\ÖñtânÆ÷¿ÃDA÷÷m3×Ìµ«gÔ%{tÙ]ü5Rß¸Ú]èÊÔÍHu-PƒEñÃ¬äßg9ê#Å¹+›,¡ÀÛÏ¾‡vê%Äö˜ÀÄ‘>¥gÛ$ã×¿ãaRd&hJ=oßşPvAíÿÊØ9Kİ´Ì—R>ÄœP0ğyc&ıÓÌJœR—ŸøĞp´ëŸ¿ù>ûo¥Æ—»ÍÏ§5æğˆ­VPEŞ8¨åhDªLì‰úÔ¼‘2ü4~âTG(¢^ÛrNoXÊ_np>Ûõó™‰OpÚËş&uÑZdğÖø†˜ÃåQÌcƒ]GoßJ©ÁŒ¢Ç!L–„!÷1“ÌäHŠÖrS†İçIGÈùJYyÜ÷^Ÿ#ì†1†Ô¼‰PNG

   IHDR   2   2   ?ˆ±   sRGB ®Îé   gAMA  ±üa   	pHYs  Ã  ÃÇo¨d  IDAThCİZy\i¾[%‘
)J²T’²¯Ÿ±ÏPömìË˜adŒuRÆ:ˆ0|fìÛÌ ëŸ5†DR!»²T²´*}÷uŸó6-çä4ş›ë÷{:ç<Ï{Ş÷¾{Nôoúõ£‘——W‰_ZñğæQ‡#+æ<y¤òHãÇãK<Bõôôğù£ñÑD˜€¿øğh|ïŞ}ëë±±ãâãÍË¥¦§ëeggÑ»wïÈ¬œ•+WªØÚdÛÛ;d¸¸¸¤¸»Ö{Æß»Æ#„	ıÉ¯ÿÿ˜èÅ/ƒ®]¿^ïÈÑ£Ñ1×Ê$''SjZ:effRVVåääPnn.®%C##224$Ó²eÉÌÌŒ,Ê[µUervv~ß©cûø–Í›ßáû…ğØÈ¤^ã¥A©‰°P-ùeâùšî9`)â2%&&RJJ± dhh@úúúò^êïå÷ïß‹–Şóûr¦¦dgW\j×¦İº?ïÔ±íÓ-ü½íòE¡3Àˆ_&'%=¾jmp'OQBB"eeg“I™2d`` ×YW®LöÕ«Ëû/_2ÁÖ\C3@*çí[!lW­y¸»Ñ„ñã6hp€—çò|²êÊ’¡&áÄ¦2‹…ï½'$ÄüÄ©Ó”Í¦S–Í»¸¹ºR?_rws#65zñâ…˜ˆ—1¦èè:!Œ’ŸÁ-Šš‚)‚§gCêïëK:´?fkm½€çN«/ÓŠá4JOÏX¼úÓ=ûöåbÄ6‡:t AúQ“ÆéÜ_çiÒ”oÄG€
åËË<Ö--+ÒÖíÛiÿÁCôæÍY/
Üó-û•µ•5mÚ„¦Oõ‹ªY³F “Ù£¾D#J$Â7õb³X²lÅÊ6[¶m£W¯^‰°k
@ÂÉÉ‘N²–*V´¤ØØ›M	„&}9ôíK¦ù•hroÙÜ íÖ­Z‘ÿœÙ÷j99NãçîU/ƒÊ°5€IÔIKK[´<(¨Ãæ­Ûè5ï );fAÀl@XG?‘ë4óaÃÉ®jUêÓûsªÎ~ÄCëõŠÏİ{ğ€âââ*6ôhà´"è†¿¿ÿY(•“0MËÈğ[µæ§›·n3(kb’oJ
¼›5¥¨˜z’ Qí|5VpŸ7ÑÓ§O©ë§iìèQùk4‚}êôò˜ïüøÉ“¥,C5õr!h$ÂvÑ÷÷={2ÙœL4 ªT©B¿@e8jÁÙÇMóüé?:R¯İÉËÓS}åßhÀQ©B…
"ä°!ƒ©__õŠfÀŒ‘ƒ@æ×ß÷xğÔbÕJa#Â{?züxü®İ»Í’Ÿ?“¦‰Ì
;lllL“'}I³gL'Ç5Ø|.Ò°ÁƒhùÒ%´\D¥J•„8 ûæë¯4.}&£ÇÄ÷ì¡«QÑıXê¥|"ÂqüzİúŸk ÄÂ¥:vAØØØH‡ƒŸ£‰_M&¿ïfPrò3²¶¶–Ç5UlmÕßPáèŸÇèÒåËêOÄß¯H]ºtVÒlf||<ùÎ×OIMÇŸ+ª—E5ÒÒæØ‰ãzHt±ÚP·‹ìî¢ù4dà@JKO—ù3gÏÒÿytğğZüã2:wş¼Ì+@Pøa!½äd©Àµ^½b¤(°¡ØlÂ¡ÃGœxj´jE…|"ÌPIfõUFÉ¡Í¤ #  ú {+y¯lË´î¿$$#ŸÀ1j²é¡yôè‘d}Á.s’ï~â/ìüœ
ò8Œgù,ÔK…4ÒèúnW"éõë×ZMJ|y€‚D"Ø="–dé™Ó§Ñ”¯&‘GƒáéaŠ–––\—£ÇO(‰#B°q	Ú/”4·nßÑã*uPÕla"¾=Z5µ¡8cI@†GŞ˜H\¶SÏîİ¨[×.êUâ‡İ‚¾}zË@ysí]»HÎµj‰ÿ Ä¿vã5g’ø¬°ÁÙ;wÿs¥šUaYñh|5:ºljjj‰±]HT¨P3{{ªáà@‡üA{Cö«WI6ãbø%Z¸d)ÕvV	®Ü¥ìuYã2½àsºZ‰ŒºªÇ	»	Ë]sŠFZÅÅÅÛ<ã]Ætì~âÎyAé?”ú
p«_Ÿ|8ƒŸ>J³¾÷§Æ^¨;kÉ²U‹L2\Â©s-'!‚û”0İK—#`:h+ò‰xscd™šš¦“6 8+B+v”Ã¡zVh#æúuDêÔ±ƒÌíe¡£¸îÁ&X‰C.ÊÎÔ¢lÊıû+	óBkO¤~\|œyï¬®A¤BpˆH…¨x0v•·°`€Ìóæ-y]:w¦WQ6”¡ƒŠ6şbÿùƒFi rúÛx+W‘Ú11)±,LCW" ‘‘!»Œn¯MëVâ+ĞÔ¾ıä@†À “š;{›cUù
ÍgÏ_È5èm”ğ­+8ŒCØšø¬Hm•––®Ÿó6GıñÃ€ğH‚ˆ@À±ÿ-aŞÖÖ†Ú·m+ó HŒg-ÁPgµhîM6œıQ<zH~)-¿Í²Æ…ˆyVV6½Ë}§ş¨ĞD¹ÖÇÉ;›Â/ÌëåËJLJ’ùfM?‘R?*:Zú ÷g½ÄùÍ lr¶ªYĞ1ÌğG!b ›ƒº>”âFl¬Ø>*] íb·Q/_²˜:´oÇdÒ'M‹é*U4Pç•J3™!ÑI¤f)cÆO¬pàĞ!©¯JC5’?Û¾5»)sæææ"´“£c¾Ïá¡ÈQ0­³çÎ±6œ	-‡}jß®mşu(Y‚×şTb;¬ ÷ŠºÁòZ(I53/—‡:¦´@ièËÇŒ)»_İ®šÌC8€V­^#Ñ	 	äé3fIË«ø
àÀcŞÜ9Ò|(€C"†"yœ­­©iY“W¯²tÎ%¸~hûwp°—İ={Nj¬.ÜîØµ[İ¿¦zuë
!GÖ†BW"#¥ÈÄš±±]àü‚˜V-[Ê±ªeM€Õ°O!Äã³B$Ö¾º}}.êL222u&‚LÜ¾sWœı “Á¡€d¹~ÃÏlNi¢h
»\"è0Ñò^»~ƒfÌ#>‡¨ EÀµÚˆ@Û,@çÈùÎÉ‘#ÍÂÜBq ¢pp”È	5j8HhEÆg%úp§IZß°¡ùB›-¸»Öxü ¤–/]L£GgW6M—6@#îîx?ŠFBë¸¸·²ªTëCD^öïGu]\hÅª`éÏ±sğ	
$ÕŞŞìÄÙoŞº%Uït¿©âC v=Ïí;w¤Ø¾›æGÓ¾™"™?şáC®¬äm Y///¨ÿ,>‹F˜İ-ö&“‹’¡$2ØáÏzö¤K’µr‡|1RJùæŞÍäîk¤X„C£|GƒÀ¸s÷.M›1“†C±\¾àlUÂl..áC Á>+÷Òhƒ7)«„ÛüşæÓB:¶o—`WÍ.ßÎ5¡n:b
İº|*»‚<‚¿{ÿ~¾i€ æ±Û(ß1¯lĞ™ĞPš0ék	M{‰Iá~¨wîş•óIKñµ)~ßŠ™j6£O¯^ğ­ª™¿M8áİ´ÙíÚµíïİ¿WbrD>@¸…€8LxšœL9or$É|?GA.©Ï½¸—gC!ƒü°ó×ßhÏŞ}T‡{‘Ã‡I €&&ûM“†šƒ¯±Éj;#ĞevïÑ‡;T34ÂBçìÙ£kz5H›Vp¾„;›Ş¹u3­\ö£8ó¦-[%dŞcí 8Ñ!éá¢ÿà¡ôË¦Í4h@Z¿f5ùúô‘k¾9KHàØhâ¸±²	%‘€ôóñyÏUön–ù‰zºi›Úµnîáæµ‰VŠ½:*X *FôAÔò›2EœIjÄ°aÔ¨aC!0€	¬]·^ÌÖ—ÆKÀÈÎÎ–ˆ•”ôTJœ‹¡G£GM>}z§ğÇ%ªY
a†é›Ç“ìáÑ@NÅµ¹ÂÀîC¹äÀ>8Ó…¦`JÈĞ€²»8AÃCø[¶HÁ	BÉK-É¾¥	 C‹éS§¾u°·_«8¹‚¢™m<=°úŞâT¤¨‰!7À¹ı“‚‚WKyÆi_Èñ(éûÅ°¡´rù2ñ!héç_6‰¡½¹³fRUÎ;˜G¦	°œwîÔ‰zõìà/PŒˆíÛ¶9Ë-q¸`8FX¼†6ü²1¿9BF‚­ğ%w7W:~ò”|§m›Ö’c ­ÛwˆùA{JV/Ö~ëßÏ½ÁhŞìb¦¢‘_øĞÎÎnÿœY±x(~Ğ,Hæò•+R:àÁ))/¥eÅ@¸U™\–tÈ£G|!Z‰¹&9ÂÜÌŒúúøÈ!İtvôeA+Ktn˜HÌŸçÿ’3y Ë¡^*m™ã\Îø~n\ÛÖ­órYè¢‰;ºmÇ.õê*4I8àC¶è‡@UxêG#F‘>¥–““Š¡Qã&PÈƒê;äñó`R[4o¾ŠeZ­^.†6|Ã>Ü3,ä¬ëx:ô¬¾¾<„oª¾‚$ş#7ÔbsiÙ¢…ªlUIÌÎ? P4¨ ¤¡a­f¤Â,ÚÖD
“æçÍQ/i„NŞKñeÛvì¨¿ÿĞaÃÇìğ ¢APÀ?ÀYƒâôk~Z'aWW`ƒ KìØyöMrwuŸ_’&èDà‡à—¢W£¢ºÍñ0AFæ¦B>;f………Ó‰S§>¸ë
ëëp¨LßNòöó^½ğ#yşou– ‰(à‡b_˜¹wÿşªÛwî4ºÃ½Ì ¨ÿè
}É¾}úäöóíî`ï°‰—fğšÎgD¥&04
_ee>qò¤õÎ]»""¯êç"ç°`Øae„V :Ö¬™çÓ»÷»İ»fV¶²ÂOĞy½p×ÿˆˆ¿Oàhdff¦wXx¸~DD¤Şí»wõ>zˆP¬‡Ö&†"ÒÂÂ<¯:W×ÎÎÎyîî®yM9OÙÚÚŞâïC»˜@~íTZ|‘‚`Rø—&(7ç¡ü›NÊqî„–µz×ë<ğoNgYøÒúş{Aôíkyìá÷L    IEND®B`‚        ‰PNG

   IHDR   2   2   ?ˆ±   sRGB ®Îé   gAMA  ±üa   	pHYs  Ã  ÃÇo¨d  ÆIDAThCíZyXUeÙ7Ù”M@1@ÜÀÜÑHE$³š¦&5—¦¬œÊLeQ´qRSÓÇi÷J³Æõq!mJQDp¡•í^dŸ÷÷İsğr=—{Õš?æ™ßó|Ü{ÎùÎ9ïï{÷ïBÿ+0‘>mmmİøc8HÁ<üy¸ğ°çaÎ£š‡ŠG‹<Nñ8fbb‚ã‡ÆCaáü1…GÄ•+nòòº]½j_ZRjW­V›Ô××QKKu±ëBvvvÔİÃ½ŞÇÇ·&((¨*,4øßwÇ&t€?L„	LâgÏ_¸¼oÿ~ÿÜsç­nŞ¼IÕ*5İ¾}›êêê¨±±‘š››1—Ì-,ÈÂÜœlml¨K—.äàè@n.®Ø:&6æê°¨¨Ëü¼=<>aRwğûÁ}a¡†ñÇÜúiÈî=ßúœÊ:M¥¥¥TU¥"€ÌÍÍÈÔÔT|—‡t_ûhmmZjåïv¶¶äííEA½{ÓÄñÊÇÄFŸ±¶¶ıœïÛ*n4Fa,øc^YÙ­ÖnX×çğ‘£TRRJuõõdmeEfffbµµ5İ¹cü‚‚TcS“ ìíåEıÃúÒœÙ³Jôë÷-_^Ìçojfv£ˆ0‰^l*Yø„]{öØ>šAõl:6l&X}Áô¡×^y…¶îØ!Ì>›#ÌÌ@S0E8p =”D£GÇôpsKçsÒ4½0H„_0H­®Iıpİú¸]_M7oÁ?‰,Øæñrm€Èú5«éÔé,òòò¤_.]¦u6Ree¥4Ã0ğÌ&ö+72d0½ıæü?¿©Lf—4Ew—SüĞğj•jåÊU«ã>şä*¾~]˜9;­.	 ®®jYS B.İºÑ´©OQO__i†a@#–¼H·ÊËé»}ûiá’ä~ùW
Vğû&KS¡—ßØG¥R¥®Z³fäg›7S=Û=Â'^¤øÆ¼WÿL=¼½…FJËÊhÄğaäÃÇ“'M¤ŞÒLã ã]G22hQrr¯‚Â‚å,SŒtù(álU55ó×~´1ö³Í[©¡¡lXP%-Èx†W~lìhb¤Õk×Ñ_ŞyW‰Ÿ0^hd"ÂI³ü!ûhÆ÷”œº4-b%Ëà%]î }™}âÄÉ¤¯ví6»][+V[	¬œ5smÛ±“>ßºMœÏ=wNáäHãˆ£^şş-´¤O«JÀ\ä ùâ«]ıùÔûš+¡‰™Z`#¯§¯]¿Ş=÷üy²²´”®(ÃÃİŞO_JlÇ”¶|…H„2np‚äDIı8¤>6äQ¡Yä{{{â* Skd0®—”Ğ B6nØŸœœ|Nº,ĞA#ü`¯múû?z"Äbİ:[=K&ùÊœYâ{ÊÒeT]rª#4šY@\PTd$48GPbÂdq¿± é«¬İä´¥¦UÕÕ)|ÜUº$ kZN>=òÀ¡C&Htp¸Î8ùI4p ;cªˆhúp1ïgAælNõgQQ‘Ô; €›ö´(WŒ>ÃòÑŞïöõâS34W4h'ÂM˜ôs»¾ŞãŠ’5‘>Õ›ñ‡¢qqÓßV¯«n2ï
2}‚‚hTôHòñéAÏ2™®];,®^açÿ|Ë–¶ÚÚÚÙ,Ÿƒt©ƒF]¸x±oÖ™lQbtfRl=}|hÿƒôı±ãÒYÃ¸tùr;???3:–Ãµ—È5(O” 9œœœÄâ¨8Ñšp•ÑƒãÅI†6‘¤íßïYÂÚ°âÚ©34q)‘•}–~<q’š¸NºÈd2O¦îİ=hüOˆ\“”˜pO®	w77
€%i€sHÊÛw~	s™®9+a¹ğˆ8››k‡•@}0åë%¥%TQYA¦hN4d2rMÿ°01§W\ê#ôßºUN-¬	¢Ë9kÂ	{0ËíŠsB
>˜\XX”6sÎœGrrÏ‰›;4fc£™S[[×©V°‚JA÷ø²y¾Ÿ¾Œ!ú–cÇ×ö<H®®®®ïªM%Q&WÈI2°Ø›>Zß6:fT"¿c·lZ‘ï«ù&CÚ àpõõwX˜æÕ¯@úÍy¯ÑÇ7´¸±cE `ĞÌÉÌS¢J¾œŸOêšZ–šB‰~LC£¢hjšĞÈÊÎ†y¡µn÷‘¢«Eö5ü0C‚!öGp.ˆEÑ#†SXßĞN¨k×®±?àèäLÃ‡•®h 2Ë8‘fpĞ¸˜—Ç<C‚9!™bhkB†Æé/ák(ş`S ğ/-+µAV6DÄ³{wšş§E$)´·ié+¨°¨Hdn¬´.jjn“#Ï‡?(¡¼¢‚K?ºqã¹s¥€ÜÒ74„Í{$ªc?hÌNŞÃaıp,Kí¢R©M›¥Ã{Uwrr$ŸˆzğZªQ×ˆuò¤xz<6V8§.ZY#C‡FÒÜY/‹Ê°²´•´<`~İºv£™Ó_¢Iñ„éÂGú††ŠáÏuÎé¢¼Bô9nø#±G/ÑÒÜ"*ÃÆÚ†‚‚E­dnfÎÅû
çd÷Òe‚˜.áE7)› ‡fÄÂ`Å;VßÂüŞ€ÅdˆÒ@¦i›ÃúìaÖÌÜŒşıã	±z1£¢éÛèÉÌLÑ5Â|tgE>6„lmm(ïçŸÅ9D§~ıÂ¨À*ôê%¢$HÁ´*«ªÄ<]8;;Kßî23Dt’ÃoÕÌÙs¾İ»·½¡ÑÎŒ'(øÇÂwŞ¢ë×KhË¶íÄÙ;Ø+†bØ;|Iz©€ì‡H„›6¬~¬Zó!mæç!@è÷è¶Ì#çtfËæ ›Vu{»6%;”m!)Á1¡‰9¯Î£O¹sDkkÆ÷!t+åD €D+«øaAû[ÉÄ´çjß£aTàL¤ˆûŠ˜€öÊ)/!<…`›C›ËƒÀ’^rMe°/OO¬…ˆÍò]y>=|j`JDpÌCÉäàpÚåƒ±@@ğ05½›€­¬,JÈ L­w` ˆ`¹Hv`@€ÊÁŞA¯FÆŒÁ
HGU4v%e ,×ß©ç÷İõ‡††FEÿPUªË2ñG~û1în¹¸tS$ßÁ@&eB/Ï˜.út<¹%ˆû'*JSæA#ˆ~Úu˜'LÙOš‡ƒ‰>BáÿÂşqÉ4#Şë’AÈ¬`gMkW}@oÏ“6¬ıP$°‘Ã†s^ér_B p@#¨µ½Z¥êĞóë¢—¿›·—ç%ş~ç´ía×O%Ş^ŞŠÑ­i ÷×Š‹© °Pl,$<9‰^|áyzñù?Ğ#}‚ŒÖ kDÛGŒ]øGâ¤IğÍš3‰òØ¥Ş½5ÍV@úêÜ9ÜÖÆ‰]÷‚‚â’_4<r¯`ÎY7nìZ´àêÇvk¨ dhûˆ±Añ9!~¼š¿jöíDx5¹äøçÄøqj/.Ÿe­ãJ‘àà`òğp'ô- @ •ZM={úR÷âØş‘7@
[Fºµ”®FììlEC¥4_ÊÂ7¦N™Òêæêº“enßñè`¬µZ½kŞócö8`‚Í…;\ÑvçèğPq»()à;••U\¢Ô‚EDx8:r”¾øò+Qº (ÌıuÅœ Â‰Ú‰’D	YYg(eY:yräÜ¹us7eÊşÜcÔLæYnXV.\¼ÄÅ^ss“*wuqi_™†Æ*/¯Â[YZQAQ¡ÈØZEs„Ş¦‚« hFßÆ X:k!°P¨"R-jš?~“xOº$ èLfÓg›·üqù_WZ4²Fà˜øíI/¿6IñŞ†¿• )"”âüƒ$ÈÎ oµ`hÅ²´3|mt('ô-AÛúñÁ­(Óå_ @ßå2µ”,¸|ş7ü<7mjRÍĞ%(á‰¿z{{§'/Z˜=r„È#ú2şïDOXš’\É1eË’.u€^£äõôõMI]²¸(zÄˆ¶f˜Ó‘´ñûà_éi©ÕC£¢Ö²Lë¥Ë÷À`ã&]]şŞ’dÿŒcÇMLMMÄKø¡ÒŒß³Ø…dMT1‰uü¾EÒ%E%>ª¸øú[¶mùfïwæÅœİAä·&ƒœ92ÆÛöÌ´§Êú†ö]Ú™&d-	¿1tuNnîø÷–¤XŸÉÎy æö°„d#9º¹¹Ò[óßhš<q"ş#â%>(e÷-¿t:wƒïîşæÏ­Û·[\¾œ/ÌàAıGCÀTôäI‰‰ÍS“Ô¾>¾Ÿò¥|­A3Ë0h)™~˜Ë	jÆá#GÜ¶ïØi–•}Ö´e†–‡6 ´< hÔßÏ¯mJBBKü„q·9áâ'èå|=_L¸<”M° ØÈÂÖşKœO"Odfšfee›\ÊÏ7ùõÚ¯(9L°!Š-$û¶\]¶………¶á<åááñßì`ú-2€‡3n-0)lâÿT¢xÈÿæ„rT¨±-ÿ›Óø7§ã,|!şı¦›/FGÊ£‡    IEND®B`‚                                                                                                                                                                                                                                                                                                                                                                                                                                                                               INDX( 	 õ T           (   P	  è      V ÚÚi     Ú          dP    h X     YP    /ò•G@rÚ˜G@rÚ™HĞH@rÚ˜G@rÚ À     ¹              A d m i n M a p . c s îP    h R     YP    ó“'âsÚó“'âsÚï÷Ì†Ú¥Ì†Ú ğ      Wï               A r e n a . 7 z r c s 2P    h X     YP    H¢İDvÚH¢İDvÚ›¯ôÉ†ÚÓOŸÉ†Ú       Æ              A u t o F a r m . c s eP    h X     YP    KF“@rÚEr“@rÚäº]”@rÚEr“@rÚ       \              B e t t e r T C . c s ~P   * € n     YP    y"¸ºrV y"¸ºrÚ>.
3Ì†Ú3ªzğË†Ú P     vD              c o n v o y - r u s t - p l u g i n . z i p i ñP    ˆ v     YP    cHC!sÚ¾Y¡!sÚÄ.ŞË†Ú2­P·Ê†Ú à!     Ğ!              D e f e n d a b l e _ B a s e s - 1 . 1 . 9 . z i p ÚfP    h V     YP    xÉ+æ@rÚxÉ+æ@rÚÒ#Tç@rÚ–+ú¯†Ú €       |              
 E a r l y Q . d l l e 3P    x h     YP    ø@$„;rÚø@$„;rÚyˆÅ<rÚ+x¯CÉ†Ú                    g a s s t a t i o n e v e n t . z i p 6P     z     YP    À0<rÚÀ0<rÚ°é«†É†ÚÇåó„É†V        r              h a r b o r - e v e n t - r u s t - p l u g i n . z i p . o p cP    h X     YP    8¿@rÚ8¿@rÚÒˆû@rÚ¾-­CÉ†Ú P      ‡O               P v e M o d e . z i p Î`     p ^     YP    ?‚ÖùCvÚÒxúCvÚÓ$dÊ†ÚÃvsdÊ†Ú 0     Ú(              R a n d o m R a i d s . c s 2 <P    x h     YP    ­r›<rÚ¹™›<rÚöV¹œ<rÚ+x¯CÉ†Ú €     Â{              S h i p w r e c k - 1 . 1 . 2 . z i p jP    p Z     YP    Ô”ŒBrÚÔ”ŒBrÚ~Y(BrÚZ†ÖgÌ†Ú p      !l               S k V p N i g h t . c s l u g Q    h X     YP    ›àõ¼tÚaÜ³¡Ê†ÚaÜ³¡Ê†ÚaÜ³¡Ê†Ú  
     ˜
              S k y B a s e . z i p ^P    € l     YP    uc(=rÚŠ(=rÚ¿ò=rÚ*®CÉ†Ú       ñ”              s p a c e - r u s t - p l u g i n . z i p . o =P    x d     YP    ®Ş4|=rÚ®Ş4|=rÚ$Á}=rÚ=?®CÉ†Ú ğ     dá              S p u t n i k - 1 . 3 . 0 . z i p . o Q    x h     YP    
UÁ¼tÚ
UÁ¼tÚ$çÁÊ†Ú'¼CÂÊ†Ú `      Z[               S t a t i c D i s p e n s e r s . c s 8P    € l   V YP    ‘ÃÏ=rÚ‘ÃÏ=rÚ)£Ñ=rÚ*®CÉ†Ú @     6              s u p e r m a r k e t - e v e n t . z i p . o hP    h R     YP    £SArÚDcArÚÏûArÚDcArÚ °     6®              T u g M e . c s n t - 4P    € j     YP    hŠ©O>rÚhŠ©O>rÚOR>rÚ¾-­CÉ†Ú      ä              W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               hŠ©O>rÚhŠ©O>rÚOR>rÚ¾-­CÉ†Ú      ä              W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               hŠ©O>rÚOR>rÚ¾-­CÉ†Ú    V ä              W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               4P    € j     YP    hŠ©O>rÚhŠ©O>rÚOR>rÚ¾-­CÉ†Ú      ä              W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               hŠ©O>rÚhŠ©O>rÚOR>rÚ¾-­CÉ†Ú      ä              W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               hŠ©O>rÚOR>rÚ¾-­CÉ†Ú      ä              W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               M e . c s n t - 4P    € j     YP    hŠ©O>rÚhŠ©O>rV OR>rÚ¾-­CÉ†Ú      ä              W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               hŠ©O>rÚhŠ©O>rÚOR>rÚ¾-­CÉ†Ú      ä              W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               OR>rÚ¾-­CÉ†Ú      ä              W a t e r E v e n t - 2 . 1 . 1 . z i p . o p               YP    ]Kr°†Ú‹Ôt°†ÚÁ—S9É†ÚîS9É†Ú       Bó             	 Z o m b i . z i p                   ]Kr°†Ú‹Ôt°†ÚÁ—S9É†ÚîS9É†Ú       Bó             	 Z o m b i . z i p   V                Z o m b i   ?„Q   ! h T     YP    ]Kr°†Ú‹Ôt°†ÚÁ—S9É†ÚîS9É†Ú       Bó             	 Z o m b i . z i p                                                                                                                                                                                                                                                                                                                                                                                         V ‰PNG

   IHDR   i      ¹àã   gAMA  ±üa   sRGB ®Îé  ñPLTEGpL ­¦ ¯ ¤ ¿ º µ ¡¤ ¼ Ä" ² « ´ÕOQ¦#½ £¥¶ · ¤ ¸*± ¬ œÜKG²*³%½."Ë>8×Îö©%¼ ¹ ¹ ¹ ¨"£!£"° 
© ¸%Å'À)Å6.ÍEDÃ@>ÑDCØpŠº » » ³ µ Á9&á¼ÔÜ±Ãµ ± Ÿš¨!›­ ¬ ™¹ ¶4,¬*!·-#Ê1égræ`iÄ1$Ô_oÙµÕ³ãÕÃïÙ¬×ÿ‹ ´ º ´ µ ¯ ¨ « ² § » ² ´ ¼ ¬­ ¬ ³ ½ ´ º ® ¶ ¼ ¹ ¤¥ ª ¦ ¦ ­¾  §­­ ¨"¨«¼  ®± ®	¤¸"² µ ·Ÿ	¬ § 
«#¨¢Ã"¶ ¢
² ¦° ª"¶ »"¥
º ³'¤	´ ¿"¦²&¡¼2(§©®º)²#¼!±(­Å#¦#° ´¶/%¶'®!¸*½&Ä0 ·"½ ¸ ¦°*¨ ª!ª!º&º ¿%	´¥"¯"¯#À)§Å&²"³0'º,©$¿'ª&Æ%§¢¦ «%³+Ë.º#
¡ÈEFÀ#£» µ2*». ³,$¶%¬+!°Ã+Ë2Â)¬!¾(Í7)­*¸5-³ Å)Â" É,§ « Á-Ê3%ÕE?Ô>4ÙGAØ´®
 ¹°1'ÌNQÑVbÁ92Ø{—×ˆ¬Ù„§±Iú   UtRNS öööööööööö ööööööpö÷ööa²Aïööüööööùö?ò4©ö´÷öööööööûöûûè®1/§^0\¥sîj¹¤Wûööööööööööööö¹L  şIDATHÇ½•çSSY‡EaE]…-ö²İŞ¶÷ŞëLrCB.77Å$1@H"B1¨"	 %T¥P@E‰¬bÛæî~Ú÷¦8ûøÌ{Î=3ç™÷wî¹™5ë1óqÃúå_®_ùõÖ~üiÍš5?ÿ²}Ëªëf¬[µ`ö‚@‚°ğğMaaó||±q^pğ7Ë¾ûvcpğÂĞ…!=„„†zsç.		ñú>Ñ“Ï¦7ÔOe´ªÚú×ö<ø÷ïs%Õ',,KÊ:r1V
”(4·³Ãí¦aLÃôæ.—ë¸wÿş‰_9J§óbâ ^Ö)˜â<Kzee=†G§ÓIoúL/¼Ú[ÁØØŞ>XòğğgÍÁ¶”d×$‘Hq
nè Ph™‰a¢mA.—?yo(‰«$‘Htw÷SÄ"&.&&†T™¦“Hœg|¦§Òë/Ôed¿ÖŞ^\\]óĞšbK"Ô‘‹BB#¿rF€¢LŒ©íe"ˆƒË¼;4jÏâ€‰›òèÓ§xqÄ‚Ç#ñ+§ÁB´ôâËşğÎ§{M¥–ƒ%¬`JL„ÚÓHõ@¥–]§ 4
Édê{™d&ÂUï»ºìjá4‡ÃáÃ€MOy6'öçgMOø×+/ùÃëé938•›»¸¶ûšµ¸$Å‹Å’|ä"Le‰„%‘ÊAd00F=a´…ì»³lè&Ÿïàƒk”Ã/ìâ“8£$L……t<øû…wùÆå'§nÔ×òP<8U\¼j/TuÉA`ìà	`l¦¶öhm­J¥ªUkµÍZsR…Ù|õ×!˜ÍI@…}¤ËoO²óìÓö®.»=Én·ÿ/¼óç3û.˜ÒÓÆŒÒÒÎÆª€\.‡)¹["Œ„*UR.eK)œĞĞëvÃ!ˆ!WÜ4s.ñ‚8¸I#4pèYÓp¾ÃŞs‹233u}'ûM&“ÑXZZZt¼JŞÙ)OC¹,İ’(˜\B¡Z§%•²Ù2šÆP«Qå!È˜2²$›GàdÂšÉTC’ûPxúÃû0'3‡0é
 “ÑTZ×XUWTW®y"«;Ê‡Ë[.¤°ÓØ2J+ÔehÍ-y d²ƒi!  STüZHV3ÑÅoùÂ{-''G—Ús2Ug2eg›êŠ—‚ÎÎ0%¸Ô½h,¶Z­Q…Ô´´4J“LFÁqñÖçO z=}EÊ@Q2”ZÑ\XhFÕÚw÷‡ç5]Òé²º LuÊò„5àp¹D"±ËE­E‹„Âè¨Û26ŞÄdpf4…ûÂĞr10¡Œüq½‚2À†ª+òÑæ¤ßV¿û(<N—šzLºììş‚şşşôKe‘R©L‹Åòàí€U«èhá-è¬‰F cSp…M‹¡pH„@3>QÁğ‚kóêfdñ{şğ{è;yxAî®Ü†3Up·¯?nµ¶u÷Še±ÀH.‡Ï†„Å"îVYy^†ãl®²Óæü]“——‡`–×Ò<9î¦ÑÀ¤ “‚¡ùÀŞÚVBÔÚ÷Gkëá]=@î™ªö3pÛv7î¶¶•´YÛ¬Öë‘””=5{’“‰ÚU|å(pÅàtíP©†‡‡mÃùù*Û„¦÷î±<¦Ñ3›m·{ÿQx§á˜v¦¶^ÖíôÒ/0
”û÷+÷Õ(Ïî ÄÄTíˆn¯ÇXµŒJ|¨pøò:ÚÚÚa0i‡g0YíaMÁaù
œ²ømŸé“ÓÀ”
xTûpN`qö¬rĞcRzl{Åb‘X= íp¹„eÂhPQGgšš®Ï#èè˜´a•£á²Ûn.“}êoÖGŸ/]ºtÅŠk¿zÂOÀ¢å‹æÏ€
ˆXóÊˆ ˆˆ 9@Ğœ•sæ, f†GnØ¼jóê-Ol[½íûM‘á‘áaKÂ"—¤}ö¸Öÿ8¬aX;Qú#    IEND®B`‚                                                                              ÷¿Rîøı[ım”gêÚ«ûå*ãuõõãÏ¢½:~)´S×>côwI¾S×Ş·ÕàCÉõõı/ÔµOıÒà»ëëŸ?‹[\çñoC¹oØĞ­òÆÂúã¥o¿\×~íÇeûUºöÓh?öúúúøq]ûtÚ§Ó>us`}ıñßÀc¯Ú«ûVÌ”'şJ>•Wı«ymÑõŸ2‹ï£Ï’íè¾8§óe]{u?å„LÙ~Gì™Û¿ªk_Ëı[k²}½îù£Ÿ¿}5·ç~µÕëdûw“ÎÜ~W×DÑ¢ı¦‹Ëõu#‚İ‹Nq÷Ë²}Uú-·ÿ
  ÿÿPK:olËç È¸ PK   YO            A  stalker_portal/deploy/src/ioncube/64/ioncube_loader_lin_5.3_ts.soux         UT ¬D]Üİ{˜+Y]ïÿÕ3d`˜©`Rh¸Ã€¼ÔÄˆ¨ŠR½»³'Íôî.º³g2€XàAÃE($A
 RÜ$Bh@Á¹DäR¢0ár¤@ÁQø=éõÍŞİßÉÛsşşõó@*¯¬^kÕªO­Z©ÎÎüê?ú‘ll˜ÕÏ…æGÌòYùŠûâ­Şæ\ß4ÍEÆ7w4w8*{‘áŸzş'qşù{7_n$âÉ÷x,¾dŸÖï°qâ÷.ßKjû“<ôn'¥øê±&¿|¡¿}3cL:¼üè¹~|ßUæÄãÍä÷ó…şöÍÍÿûÏª½[lsÿcfÆ˜{os¯cn³aÌ¾¼şfcÌs¥Ü‰ŸcaŒy•1æVòÚeÆ˜±1¦2ÆÜşXù¿<ökwÛ0æŸeû¯Œ1»Æ˜_•çÿGŸjŒù'cÌ¥æäÏŒ1.ÛmcÌ1'ÏkÒ^iŒy1æ»Æ˜Wc¾iŒùõå¸c>¾ÌÚ[Êó;c>'æŠı¬1æ¾Æ|[üı’³wÉóGcşİó§Æ˜¿1ÆÜvÃ˜7-û½aÌİ7Œ¹ŸÔÓ1Æ|Ï†1Wns…Ø_ëÃó1_]fÏ3ûÄ2[Æ˜×c¾lŒy¦1ævÆ˜ÿiŒYFî‚c>bøç—Ù¶¾!ö#Æ˜+eû>Æ¼O¶ÒócÌãŒ1Ï6Æ|Áó'Æ˜-y½eŒùßÆ˜‹7Œ¹ã±ñ»õ†1cÌËŒ1o3Æ|Qür)óy~§cdŒù¤1æÆ4äõÏcşx™›cşÈ³¹<äwæÆ˜§/Ï¡cîdŒy”ø=Œ1•íß‘Ç¯c¾.Û—lócÌµÆ˜1_3Æ¼Ûœÿq6ŒYc%Ï—§í]Œ1‡Æ˜ß8Vî6Œù=cL™c~