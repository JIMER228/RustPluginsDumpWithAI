// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Globalization;
using Rust;
using Facepunch;

namespace Oxide.Plugins
{
    [Info("SecurityCameras", "k1lly0u", "0.1.09", ResourceId = 0)]
    class SecurityCameras : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin Clans, Friends, ImageLibrary, LustyMap;

        private StoredData storedData;
        private RestoreData restoreData;
        private DynamicConfigFile data;

        static SecurityCameras ins;

        private List<CameraManager> cameraManagers = new List<CameraManager>();
        private List<Controller> controllers = new List<Controller>();
        private Dictionary<Colors, string> uiColors = new Dictionary<Colors, string>();

        static BuildingLinks linkManager;
        static bool isUnloading;
        static int layerPlcmnt;

        private bool wipeDetected;
        private bool isInitialized;       

        private byte[] signImage = null;

        const string permUse = "securitycameras.use";
        const string permIgnore = "securitycameras.ignorelimit";
       
        const string burlapSack = "assets/prefabs/misc/burlap sack/generic_world.prefab";
        const string chairPrefab = "assets/prefabs/deployable/chair/chair.deployed.prefab";
        const int cameraId = 1300054961;
        const int computerId = 1490499512;
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            isUnloading = false;

            permission.RegisterPermission(permUse, this);
            permission.RegisterPermission(permIgnore, this);

            foreach (string key in configData.Camera.Max.Keys)
            {
                if (permission.PermissionExists(key, this))
                    continue;
                permission.RegisterPermission(key, this);
            }

            lang.RegisterMessages(Messages, this);
            data = Interface.Oxide.DataFileSystem.GetFile("securitycamera_data");
            restoreData = new RestoreData();

            linkManager = new BuildingLinks();
        }

        private void OnServerInitialized()
        {
            ins = this;
            LoadData();

            layerPlcmnt = LayerMask.GetMask("Construction", "Default", "Deployed", "World", "Terrain");

            LoadDefaultImages();            

            foreach (var color in configData.Control.UIColors)
                uiColors.Add(color.Key, UI.Color(color.Value.Color, color.Value.Alpha));

            if (wipeDetected)
            {
                storedData = new StoredData();
                SaveData();
            }
            else ServerMgr.Instance.StartCoroutine(InitializeAllCameras());
        }

        private void OnNewSave(string filename) => wipeDetected = true;

        private void OnServerSave() => SaveData();

        private void OnPlayerDisconnected(BasePlayer player)
        {
            BuildingLinks.Link link = linkManager.GetLinkOf(player);
            if (link != null)            
                link.CloseLink(); 
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || input == null)
                return;

            if (player.GetComponent<Controller>())
                return;
            if (input.WasJustPressed(BUTTON.USE))
            {
                RaycastHit hit;
                if (Physics.SphereCast(player.eyes.position, 0.1f, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward, out hit, 3f))
                {
                    DroppedItem droppedItem = hit.GetEntity()?.GetComponent<DroppedItem>();
                    if (droppedItem != null && droppedItem.item?.info?.itemid == computerId)
                    {
                        BuildingLinks.Link link = linkManager.GetLinkOf(droppedItem);
                        if (link != null)
                            OnComputerInteraction(player, link);
                    }
                }
            }
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null)
                return;
            linkManager.OnEntityTakeDamage(entity, info); 
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null)
            {
                ItemPlacement placement = entity.GetComponent<ItemPlacement>();
                if (placement != null)
                    placement.OnPlayerDeath();
            }
        }

        private void OnEntityKill(BaseNetworkable networkable)
        {            
            if (networkable != null)
                linkManager.OnEntityDeath(networkable);                            
        }

        private object OnPlayerTick(BasePlayer player, PlayerTick msg, bool wasPlayerStalled)
        {
            Controller cameraController = player.GetComponent<Controller>();
            if (cameraController != null)
                return false;
            return null;
        }

        private object CanDismountEntity(BaseMountable mountable, BasePlayer player)
        {
            Controller controller = player.GetComponent<Controller>();
            if (controller != null)
                return false;
            return null;
        }

        private object OnPlayerCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();

            if (player != null && player.GetComponent<Controller>())
            {
                string text = arg.GetString(0, "text").ToLower();

                if (text.Length > 0 && text[0] == '/' && arg.cmd.FullName == "chat.say")
                {
                    return false;
                }
            }
            return null;
        }

        private void Unload()
        {
            isUnloading = true;
            SaveData();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, SCUI_Panel);
                CuiHelper.DestroyUi(player, SCUI_Overlay);
            }

            linkManager.DestroyAllLinks();
            
            controllers.Clear();
            ins = null;
            linkManager = null;
        }      
        #endregion

        #region Functions
        private IEnumerator InitializeAllCameras()
        {
            PrintWarning($"Initializing security camera entities");
            for (int i = 0; i < storedData.cameraData.Count(); i++)
            {
                CameraManager.CameraData cameraData = storedData.cameraData.ElementAt(i);
                
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.2f));

                InitializeCamera(cameraData);
            }
            PrintWarning($"Initializing camera controller entities");
            ServerMgr.Instance.StartCoroutine(FindRegisteredSignage(BaseNetworkable.serverEntities.Where(x => x is Signage).Cast<Signage>()));
        }

        private IEnumerator FindRegisteredSignage(IEnumerable<Signage> signage)
        {
            for (int i = 0; i < signage.Count(); i++)
            {
                Signage sign = signage.ElementAt(i);

                if (sign == null || sign.IsDestroyed || !storedData.signIds.Contains(sign.net.ID))
                    continue;

                yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.2f));

                InitializeSign(sign);
            }
            PrintWarning($"Entity initialization complete!");
        }

        private void InitializeCamera(CameraManager.CameraData cameraData)
        {
            CameraManager camera = SpawnDroppedItem(new Vector3(cameraData.position[0], cameraData.position[1], cameraData.position[2])).gameObject.AddComponent<CameraManager>();
            camera.SetRotation(new float[] { cameraData.baseRotation[0], cameraData.baseRotation[1], cameraData.baseRotation[2] });
        }

        private void InitializeSign(Signage sign)
        {
            BuildingLinks.Link link = linkManager.FindLinkedEntities(sign);

            if (!sign.HasFlag(BaseEntity.Flags.Locked))
                sign.SetFlag(BaseEntity.Flags.Locked, true);

            if (signImage != null)
                sign.textureID = FileStorage.server.Store(signImage, FileStorage.Type.png, sign.net.ID, 0);
            sign.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

            BaseEntity[] children = sign.children.Where(x => x.GetComponent<DroppedItem>())?.ToArray() ?? null;

            if (children != null)
            {
                for (int i = 0; i < children.Length; i++)
                {
                    sign.RemoveChild(children[i]);
                    children[i].Kill();
                }
            }

            Item item = ItemManager.CreateByItemID(computerId);
            BaseEntity worldEntity = item.CreateWorldObject(sign.transform.position);

            UnityEngine.Object.Destroy(worldEntity.GetComponent<Rigidbody>());
            UnityEngine.Object.Destroy(worldEntity.GetComponent<EntityCollisionMessage>());
            UnityEngine.Object.Destroy(worldEntity.GetComponent<PhysicsEffects>());

            DroppedItem droppedItem = worldEntity.GetComponent<DroppedItem>();
            droppedItem.allowPickup = false;
            droppedItem.CancelInvoke(droppedItem.IdleDestroy);
            droppedItem.SetParent(sign);
            droppedItem.transform.localPosition = new Vector3(0, 0, 0.3f);
            droppedItem.transform.rotation = Quaternion.Euler(0, sign.transform.eulerAngles.y + 180, 0);

            if (link != null)
                link.SetLinkController(sign, droppedItem);
        }

        private BaseEntity FindEntityFromRay(BasePlayer player)
        {
            Ray ray = new Ray(player.eyes.position, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, configData.Camera.Distance))
                return null;

            var hitEnt = hit.collider.GetComponentInParent<BaseEntity>();
            if (hitEnt != null)
                return hitEnt;
            return null;
        }

        private void OnComputerInteraction(BasePlayer player, BuildingLinks.Link link)
        {
            if (!permission.UserHasPermission(player.UserIDString, permUse))
            {
                SendReply(player, msg("noPerm", player.userID));
                return;
            }

            if (!player.IsBuildingAuthed())
            {
                SendReply(player, msg("notAuthed", player.userID));
                return;
            }

            if (link.IsInUse())            
            {
                SendReply(player, msg("controlInUse", player.userID));
                return;
            }

            link.OpenLink(player);
            DisplayToPlayer(player);
        }

        private BuildingManager.Building GetBuilding(BaseEntity entity)
        {
            BuildingManager.Building building = null;

            OBB obb = entity.WorldSpaceBounds();
            List<BuildingBlock> list = Pool.GetList<BuildingBlock>();
            Vis.Entities<BuildingBlock>(obb.position, 1.5f + obb.extents.magnitude, list, 2097152, QueryTriggerInteraction.Collide);

            if (list.Count > 0)            
                building = list[0].GetBuilding();            

            Pool.FreeList<BuildingBlock>(ref list);

            return building;
        }
               
        private bool IsValidSource(BasePlayer player, CameraManager camera)
        {            
            if (camera == null || camera.entity == null || camera.entity.IsDestroyed)
                return false;

            if (configData.Control.Authorized)
            {
                if (!player.IsBuildingAuthed())
                    return false;
            }
            
            return true;
        }

        private DroppedItem SpawnDroppedItem(Vector3 position)
        {
            BaseEntity worldEntity = CreateWorldObject(position);

            UnityEngine.Object.Destroy(worldEntity.GetComponent<Rigidbody>());
            UnityEngine.Object.Destroy(worldEntity.GetComponent<EntityCollisionMessage>());
            UnityEngine.Object.Destroy(worldEntity.GetComponent<PhysicsEffects>());

            DroppedItem droppedItem = worldEntity.GetComponent<DroppedItem>();
            droppedItem.allowPickup = false;
            droppedItem.CancelInvoke(droppedItem.IdleDestroy);            

            return droppedItem;
        }

        private BaseEntity CreateWorldObject(Vector3 pos)
        {
            Item item = ItemManager.CreateByItemID(cameraId);

            BaseEntity worldEntity = GameManager.server.CreateEntity(burlapSack, pos);
            WorldItem worldItem = worldEntity as WorldItem;
            if (worldItem != null)
                worldItem.InitializeItem(item);

            worldItem.enableSaving = false;
            worldEntity.Spawn();
            item.SetWorldEntity(worldEntity);
            return worldEntity;
        }

        private int GetMaxCameras(ulong playerId)
        {
            int max = 0;
            foreach(var entry in configData.Camera.Max)
            {
                if (permission.UserHasPermission(playerId.ToString(), entry.Key))
                {
                    if (max < entry.Value)
                        max = entry.Value;
                }                    
            }
            return max;
        }
        #endregion

        #region Components
        private class BuildingLinks
        {
            private List<Link> links = new List<Link>();

            public CameraManager[] GetCamerasOf(BasePlayer player) => links.Find(x => x.player == player)?.cameras.ToArray() ?? null;

            public CameraManager[] GetCamerasOf(Signage signage) => links.Find(x => x.signage == signage)?.cameras.ToArray() ?? null;

            public CameraManager[] GetCamerasOf(BuildingManager.Building building) => links.Find(x => x.building == building)?.cameras.ToArray() ?? null;

            public Link GetLinkOf(BuildingManager.Building building) => links.Find(x => x.building == building) ?? null;

            public Link GetLinkOf(Signage signage) => links.Find(x => x.signage == signage) ?? null;

            public Link GetLinkOf(CameraManager camera) => links.Find(x => x.cameras.Contains(camera)) ?? null;

            public Link GetLinkOf(DroppedItem computer) => links.Find(x => x.computer == computer) ?? null;

            public Link GetLinkOf(BasePlayer player) => links.Find(x => x.player == player) ?? null;

            public Link AddNewLink(BuildingManager.Building building, Signage sign, CameraManager[] cameras)
            {
                Link link = new Link(building, sign, cameras);
                links.Add(link);
                return link;
            }

            public void AddToLink(BuildingManager.Building building, CameraManager camera)
            {
                Link link = GetLinkOf(building);
                if (link != null)
                    link.cameras.Add(camera);
                else AddNewLink(building, null, new CameraManager[] { camera });                
            }

            public void RemoveFromLink(CameraManager camera)
            {
                Link link = GetLinkOf(camera);
                if (link != null)                
                    link.cameras.Remove(camera);
            }
            
            public Link FindLinkedEntities(Signage sign)
            {
                if (sign != null)
                {
                    BuildingManager.Building building = ins.GetBuilding(sign);
                    if (building != null)
                    {
                        Link link = GetLinkOf(building);
                        if (link == null)
                        {
                            CameraManager[] availableSources = ins.cameraManagers.Where(x => building.buildingBlocks.Contains(x.parent)).ToArray() ?? new CameraManager[0];
                            return AddNewLink(building, sign, availableSources);
                        }
                        else return link;
                    }
                }
                return null;
            }

            public void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
            {
                foreach (Link link in links)
                    link.OnEntityTakeDamage(entity, info);
            }

            public void OnEntityDeath(BaseNetworkable networkable)
            {
                for (int i = links.Count - 1; i >= 0; i--)                
                    links.ElementAt(i).OnEntityDeath(networkable);                
            }

            public void DestroyAllLinks()
            {
                foreach (Link link in links)
                    link.DestroyLink();
            }

            public class Link
            {
                public BasePlayer player { get; private set; }
                public Controller controller { get; private set; }

                public BuildingManager.Building building;
                public Signage signage;                
                public DroppedItem computer;
                public List<CameraManager> cameras;

                public Link() { }
                public Link(BuildingManager.Building building, Signage signage, CameraManager[] cameras)
                {
                    this.building = building;
                    this.signage = signage;
                    this.cameras = cameras.ToList();
                }

                public void OpenLink(BasePlayer player) => this.player = player;

                public void CloseLink(bool isFinishing = false)
                {
                    player = null;

                    if (controller != null)
                    {
                        if (!isUnloading)
                        {
                            if (ins.controllers.Contains(controller))
                                ins.controllers.Remove(controller);
                        }

                        if (!isFinishing)
                            controller.FinishSpectating();
                        controller = null;
                    }                    
                }

                public void InitializeController(BasePlayer player, int spectateIndex)
                {
                    CuiHelper.DestroyUi(this.player, SCUI_Panel);

                    controller = player.gameObject.AddComponent<Controller>();
                    ins.controllers.Add(controller);
                    controller.SetInitialTarget(this, spectateIndex);
                }
                
                public bool IsInUse() => controller != null || player != null;

                public void SetLinkController(Signage signage, DroppedItem computer)
                {
                    this.signage = signage;
                    this.computer = computer;
                }

                public void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
                {
                    if (controller != null)
                    {
                        if (entity == controller.player)
                        {
                            info.damageTypes = new DamageTypeList();
                            info.HitEntity = null;
                            info.HitMaterial = 0;
                            info.PointStart = Vector3.zero;                            
                        }
                        else
                        {
                            BaseMountable baseMountable = controller.player.GetMounted();
                            if (baseMountable != null && entity == baseMountable)
                            {
                                info.damageTypes = new DamageTypeList();
                                info.HitEntity = null;
                                info.HitMaterial = 0;
                                info.PointStart = Vector3.zero;
                            }
                        }
                    }                    
                }

                public void OnEntityDeath(BaseNetworkable networkable)
                {
                    CameraManager camera = networkable.GetComponent<CameraManager>();
                    if (camera != null && cameras.Contains(camera))
                        DestroyCamera(camera);

                    BuildingBlock buildingBlock = networkable.GetComponent<BuildingBlock>();
                    if (buildingBlock != null)
                    {
                        for (int i = cameras.Count - 1; i >=0; i--)
                        {
                            CameraManager camera1 = cameras.ElementAt(i);
                            if (camera1.parent == buildingBlock)
                                DestroyCamera(camera1);
                        }                        
                    }

                    Signage deadSignage = networkable.GetComponent<Signage>();
                    if (deadSignage != null && deadSignage == signage)
                        DestroySignage();                    
                }

                public void DestroyLink()
                {
                    CloseLink();

                    if (computer != null)
                    {
                        computer.DestroyItem();
                        if (!computer.IsDestroyed)
                            computer.Kill();
                        computer = null;
                    }
                    for (int i = cameras.Count - 1; i >= 0; i--)
                        UnityEngine.Object.Destroy(cameras.ElementAt(i));
                }

                public void DestroyCamera(CameraManager camera)
                {
                    cameras.Remove(camera);
                    ins.cameraManagers.Remove(camera);

                    if (controller != null)                    
                        controller.OnCameraDestroyed(camera);

                    UnityEngine.Object.Destroy(camera);
                }

                public void DestroySignage()
                {
                    DestroyComputer();                    

                    if (ins.storedData.signIds.Contains(signage.net.ID))
                        ins.storedData.signIds.Remove(signage.net.ID);

                    if (controller != null)
                        controller.OnSignageDestroyed();

                    signage = null;
                }   
                
                public void DestroyComputer()
                {
                    if (computer != null)
                    {
                        computer.DestroyItem();
                        if (!computer.IsDestroyed)
                            computer.Kill();
                        computer = null;
                    }

                }
            }
        }

        class ItemPlacement : MonoBehaviour
        {
            private BasePlayer player;
            private DroppedItem droppedItem;
            private BuildingBlock hitEntity = null;
            private bool isValidPlacement;

            private float placementDistance;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                enabled = false;

                placementDistance = ins.configData.Camera.Distance;
                SpawnDroppedItem();
                player.ChatMessage(ins.msg("placeHelp1", player.userID));
            }

            private void FixedUpdate()
            {
                Item activeItem = player.GetActiveItem();
                if (activeItem == null || activeItem.info.itemid != 1300054961)
                    CancelPlacement();

                isValidPlacement = false;
                
                InputState input = player.serverInput;
                Vector3 eyePosition = player.transform.position + (Vector3.up * 1.8f);

                RaycastHit hit;
                if (Physics.Raycast(new Ray(player.transform.position + (Vector3.up * 1.8f), Quaternion.Euler(input.current.aimAngles) * Vector3.forward), out hit, placementDistance, layerPlcmnt))
                {
                    droppedItem.transform.position = hit.point + (-droppedItem.transform.right * 0.1f);
                    droppedItem.transform.rotation = Quaternion.LookRotation(eyePosition - droppedItem.transform.position, Vector3.up) * Quaternion.Euler(0, 90, 0);

                    isValidPlacement = hitEntity = hit.GetEntity()?.GetComponent<BuildingBlock>();                    
                }
                else
                {
                    droppedItem.transform.position = new Ray(eyePosition, Quaternion.Euler(input.current.aimAngles) * Vector3.forward).GetPoint(2);
                    droppedItem.transform.rotation = Quaternion.LookRotation(eyePosition - droppedItem.transform.position, Vector3.up) * Quaternion.Euler(0, 90, 0);
                }

                if (input.WasJustPressed(BUTTON.FIRE_PRIMARY))
                {
                    if (!isValidPlacement)
                    {
                        player.ChatMessage(ins.msg("placeHelp2", player.userID));
                        return;
                    }
                    else PlaceCamera();
                }
                else if (input.WasJustPressed(BUTTON.FIRE_SECONDARY))
                    CancelPlacement();
            }

            private void SpawnDroppedItem()
            {
                droppedItem = ins.SpawnDroppedItem(player.transform.position);                
                enabled = true;
            }
                        
            private void CancelPlacement()
            {
                enabled = false;
                droppedItem.DestroyItem();
                droppedItem.Kill();
                player.ChatMessage(ins.msg("placeHelp3", player.userID));
                Destroy(this);
            }

            private void PlaceCamera()
            {
                player.ChatMessage(ins.msg("placeHelp4", player.userID));
                player.inventory.containerBelt.Take(null, cameraId, 1);
                CameraManager camera = droppedItem.gameObject.AddComponent<CameraManager>();                
                Destroy(this);
            }

            public void OnPlayerDeath()
            {
                CancelPlacement();
                Destroy(this);
            }
        }

        class CameraManager : MonoBehaviour
        {            
            public DroppedItem entity { get; private set; }
            public BuildingBlock parent { get; private set; }
            public Vector3 baseRotation { get; set; }

            private void Awake()
            {
                entity = GetComponent<DroppedItem>();
                enabled = false;
                baseRotation = entity.transform.eulerAngles;

                OBB obb = entity.WorldSpaceBounds();
                List<BuildingBlock> list = Pool.GetList<BuildingBlock>();
                Vis.Entities<BuildingBlock>(obb.position, 0.5f + obb.extents.magnitude, list, 2097152, QueryTriggerInteraction.Collide);

                if (list.Count > 0)                
                    parent = list[0];                
                else
                {
                    print($"[ERROR] Unable to find parent block for security camera");
                    Destroy(this, 1f);
                }
                Pool.FreeList<BuildingBlock>(ref list);

                if (parent != null)
                {
                    ins.cameraManagers.Add(this);
                    linkManager.AddToLink(parent.GetBuilding(), this);
                }
            }  
            
            private void OnDestroy()
            {
                if (entity != null)
                {
                    entity.DestroyItem();
                    if (!entity.IsDestroyed)
                        entity.Kill();
                }
            }

            public void SetRotation(float[] rotation)
            {
                baseRotation = new Vector3(rotation[0], rotation[1], rotation[2]);
                entity.transform.rotation = Quaternion.Euler(baseRotation);
            }
            
            public CameraData GetCameraData() => new CameraData(this);
                       
            public class CameraData
            {
                public float[] position;
                public float[] baseRotation;

                public CameraData() { }

                public CameraData(CameraManager camera)
                {
                    position = new float[] { camera.entity.transform.position.x, camera.entity.transform.position.y, camera.entity.transform.position.z };
                    baseRotation = new float[] { camera.baseRotation.x, camera.baseRotation.y, camera.baseRotation.z };
                }
            }
        }

        class Controller : MonoBehaviour
        {
            public BasePlayer player { get; private set; }
            private BaseMountable mountPoint;
            
            private BuildingLinks.Link link;

            private CameraManager[] availableSources;
            private CameraManager spectateTarget;
            private int spectateIndex = 0;
                     
            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                enabled = false;

                mountPoint = GameManager.server.CreateEntity(chairPrefab, player.transform.position) as BaseMountable;
                mountPoint.enableSaving = false;
                mountPoint.skinID = (ulong)1169930802;
                mountPoint.Spawn();   
            }

            private void OnDestroy()
            {
                if (mountPoint != null && !mountPoint.IsDestroyed)
                    mountPoint.Kill();

                if (!isUnloading)
                {                    
                    if (ins.controllers.Contains(this))
                        ins.controllers.Remove(this);
                }
            }

            private void FixedUpdate()
            {
                InputState input = player.serverInput;

                if (input.WasJustPressed(BUTTON.USE))
                {                    
                    FinishSpectating();
                    return;
                }                

                if (spectateTarget != null)
                {                   
                    Vector3 aimAngle = player.serverInput.current.aimAngles;
                    spectateTarget.entity.transform.rotation = Quaternion.Euler(aimAngle.x, aimAngle.y, 0) * Quaternion.Euler(0, 90, 0);                    
                }

                if (input.WasJustPressed(BUTTON.JUMP))
                    UpdateSpectateTarget(1);
                else if (input.WasJustPressed(BUTTON.DUCK))
                    UpdateSpectateTarget(-1);
            }

            public void BeginSpectating()
            {
                ins.LustyMap?.Call("DisableMaps", player);
                ins.restoreData.AddData(player);
                player.inventory.Strip();
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
                player.MountObject(mountPoint);                
            }

            public void FinishSpectating()
            {
                enabled = false;                
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
                player.DismountObject();
                player.EnsureDismounted();                
                ins.restoreData.RestorePlayer(player);
                ins.LustyMap?.Call("EnableMaps", player);
                CuiHelper.DestroyUi(player, SCUI_Overlay);
                link.CloseLink(true);
                Destroy(this);
            }

            public void SetInitialTarget(BuildingLinks.Link link, int spectateIndex)
            {
                this.link = link;
                GetSpectateTargets();

                if (availableSources.Length == 0)
                {
                    player.ChatMessage(ins.msg("noSources", player.userID));
                    Destroy(this);
                }
                else BeginSpectating();

                this.spectateIndex = spectateIndex;
                SetSpectateTarget();

                player.ChatMessage(ins.msg("controlInfo", player.userID));
                enabled = true;
            }
                       
            public void UpdateSpectateTarget(int index = 0)
            {
                spectateIndex = spectateIndex += index;

                if (spectateIndex > availableSources.Length - 1)
                    spectateIndex = 0;
                else if (spectateIndex < 0)
                    spectateIndex = availableSources.Length - 1;

                if (availableSources[spectateIndex] != spectateTarget)
                    SetSpectateTarget();               
            }

            public void SetSpectateTarget()
            {
                spectateTarget = availableSources[spectateIndex];
                mountPoint.transform.position = spectateTarget.entity.transform.position + (Vector3.down * 1.5f) + (-spectateTarget.entity.transform.right * 0.2f);
                mountPoint.transform.rotation = Quaternion.Euler(spectateTarget.baseRotation.x, spectateTarget.baseRotation.y, 0) * Quaternion.Euler(0, 270, 0);
                mountPoint.SendNetworkUpdate();
                CreateCameraOverlay();
            }

            private void GetSpectateTargets()
            {
                availableSources = link.cameras.Where(x => ins.IsValidSource(player, x)).ToArray();
                if (availableSources == null)
                    availableSources = new CameraManager[0];
            }

            private void CreateCameraOverlay()
            {
                if (!ins.configData.Camera.Overlay)
                    return;

                CuiElementContainer container = UI.Container("0 0 0 0", "0 0", "1 1", false, true, "Under");
                UI.Image(ref container, ins.GetImage("camoverlay"), "0 0", "1 1", true);
                UI.Panel(ref container, "0 0 0 0.4", "0.04 0.9", "0.18 0.94", true);
                UI.Label(ref container, "<color=red>REC</color>", 18, "0.04 0.9", "0.18 0.94", TextAnchor.MiddleCenter, true);

                UI.Panel(ref container, "0 0 0 0.4", "0.82 0.9", "0.96 0.94", true);
                UI.Label(ref container, string.Format(ins.msg("ui.Camera", player.userID), spectateIndex + 1), 18, "0.82 0.9", "0.96 0.94", TextAnchor.MiddleCenter, true);

                CuiHelper.DestroyUi(player, SCUI_Overlay);
                CuiHelper.AddUi(player, container);
            }

            public void OnCameraDestroyed(CameraManager camera)
            {
                GetSpectateTargets();

                if (availableSources.Length == 0)
                {
                    player.ChatMessage(ins.msg("noSources", player.userID));
                    FinishSpectating();                    
                    return;
                }

                if (camera == spectateTarget)
                {
                    player.ChatMessage(ins.msg("cameraDestroyed", player.userID));
                    UpdateSpectateTarget();
                }
            }            

            public void OnSignageDestroyed()
            {
                player.ChatMessage(ins.msg("controlDestroyed", player.userID));
                FinishSpectating();
            }             
        }
        #endregion

        #region UI
        const string SCUI_Panel = "SCUI_Panel";
        const string SCUI_Overlay = "SCUI_Overlay";

        public class UI
        {
            static public CuiElementContainer Container(string color, string aMin, string aMax, bool useCursor = false, bool isOverlay = false, string parent = "Overlay")
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
                        new CuiElement().Parent = parent,
                        isOverlay ? SCUI_Overlay : SCUI_Panel
                    }
                };
                return NewElement;
            }

            static public void Panel(ref CuiElementContainer container, string color, string aMin, string aMax, bool isOverlay = false, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                isOverlay ? SCUI_Overlay : SCUI_Panel);
            }

            static public void Label(ref CuiElementContainer container, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, bool isOverlay = false)
            {
                container.Add(new CuiLabel
                {
                    Text = { FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }

                },
                isOverlay ? SCUI_Overlay : SCUI_Panel);
            }

            static public void Button(ref CuiElementContainer container, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter, bool isOverlay = false)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0f },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                isOverlay ? SCUI_Overlay : SCUI_Panel);
            }
            static public void Image(ref CuiElementContainer container, string png, string aMin, string aMax, bool isOverlay = false)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = isOverlay ? SCUI_Overlay : SCUI_Panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png },
                        new CuiRectTransformComponent { AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }

            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.Substring(1);
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
        #endregion

        #region UI Generation
        enum Colors { Background, Panel, Button, Selected }
        private void DisplayToPlayer(BasePlayer player, int page = 0)
        {
            CuiElementContainer container = null;            
            CameraManager[] cameras = linkManager.GetCamerasOf(player).Where(x => IsValidSource(player, x)).ToArray();

            if (cameras == null || cameras.Length == 0)
            {
                container = UI.Container(uiColors[Colors.Background], "0.4 0.4", "0.6 0.6", true);
                UI.Label(ref container, msg("ui.Title", player.userID), 25, "0.05 0.7", "0.95 1");
                UI.Label(ref container, msg("ui.NoCameras", player.userID), 12, "0.05 0.5", "0.95 0.7");
                UI.Button(ref container, uiColors[Colors.Button], msg("ui.Close", player.userID), 12, "0.25 0.2", "0.75 0.45", "scui.close");
            }
            else
            {
                container = UI.Container(uiColors[Colors.Background], "0.35 0.15", "0.65 0.95", true);
                UI.Label(ref container, msg("ui.Title", player.userID), 25, "0.05 0.9", "0.95 1");
                UI.Button(ref container, uiColors[Colors.Selected], "✖", 15, "0.91 0.95", "0.99 0.99", "scui.close");
                UI.Label(ref container, msg("ui.Information", player.userID), 12, "0.05 0.85", "0.95 0.95");
                int count = 0;
                int startAt = page * 20;
                for (int i = startAt; i < (startAt + 20 > cameras.Length ? cameras.Length : startAt + 20); i++)
                {
                    CameraManager camera = cameras.ElementAt(i);
                    UI.Panel(ref container, uiColors[Colors.Panel], $"0.04 {(0.81f - (count * 0.04f)) + 0.005f}", $"0.96 {(0.85f - (count * 0.04f)) - 0.005f}");
                    UI.Label(ref container, string.Format(msg("ui.Camera", player.userID), count + 1), 11, $"0.05 {0.81f - (count * 0.04f) + 0.005f}", $"0.31 {0.85f - (count * 0.04f) - 0.005f}", TextAnchor.MiddleLeft);
                    
                    UI.Button(ref container, uiColors[Colors.Button], msg("ui.Control", player.userID), 11, $"0.76 {0.81f - (count * 0.04f) + 0.005f}", $"0.97 {0.85f - (count * 0.04f) - 0.005f}", $"scui.control {i}");

                    count++;
                }

                int totalPages = cameras.Length / 20;

                UI.Button(ref container, uiColors[Colors.Button], msg("ui.Back", player.userID), 11, "0.1 0.01", "0.35 0.04", page > 0 ? $"scui.changepage {page - 1}" : "");
                UI.Label(ref container, string.Format(msg("ui.Page", player.userID), page + 1, totalPages + 1), 11, "0.35 0.01", "0.65 0.04");
                UI.Button(ref container, uiColors[Colors.Button], msg("ui.Next", player.userID), 11, "0.65 0.01", "0.9 0.04", page + 1 <= totalPages ? $"scui.changepage {page + 1}" : "");
            }

            CuiHelper.DestroyUi(player, SCUI_Panel);
            CuiHelper.AddUi(player, container);
        }
        #endregion
                
        #region UI Commands 
       
        [ConsoleCommand("scui.changepage")]
        private void ccmdChangePage(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            DisplayToPlayer(player, arg.GetInt(0));
        }
               
        [ConsoleCommand("scui.control")]
        private void ccmdControl(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            if (player.GetComponent<Controller>())
                return;

            BuildingLinks.Link link = linkManager.GetLinkOf(player);
            if (link != null)
            {
                link.InitializeController(player, arg.GetInt(0));                
                CuiHelper.DestroyUi(player, SCUI_Panel);
            }
        }

        [ConsoleCommand("scui.close")]
        private void ccmdCloseUI(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            BuildingLinks.Link link = linkManager.GetLinkOf(player);
            if (link != null)            
                link.CloseLink();            

            CuiHelper.DestroyUi(player, SCUI_Panel);
        }
        #endregion

        #region Commands
        [ChatCommand("sc")]
        private void cmdSC(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permUse)) return;
            if (args.Length == 0)
            {
                SendReply(player, $"<color=#ce422b>{Title}</color><color=#939393>  v{Version}  -</color> <color=#ce422b>{Author} @ www.chaoscode.io</color>");
                SendReply(player, msg("help2", player.userID));
                SendReply(player, msg("help3", player.userID));
                SendReply(player, msg("help4", player.userID));
                SendReply(player, msg("help5", player.userID));
                return;
            }

            if (!player.IsBuildingAuthed())
            {
                SendReply(player, msg("noAuth", player.userID));
                return;
            }

            if (args[0].ToLower() == "controller")
            {
                Signage signage = FindEntityFromRay(player) as Signage;
                if (signage == null)
                {
                    SendReply(player, msg("noSign", player.userID));
                    return;
                }

                if (signage.OwnerID != player.userID)
                {
                    if (configData.Camera.Friends && !IsFriendlyPlayer(signage.OwnerID, player.userID))
                    {
                        SendReply(player, msg("notOwner", player.userID));
                        return;
                    }
                }

                BuildingLinks.Link link = null;

                if (args.Length >= 2 && args[1].ToLower() == "remove")
                {
                    link = linkManager.GetLinkOf(signage);
                    if (link == null || !storedData.signIds.Contains(signage.net.ID))
                    {
                        SendReply(player, msg("notController", player.userID));
                        return;
                    }
                    
                    link.DestroySignage();
                    SaveData();

                    SendReply(player, msg("removedController", player.userID));
                    player.GiveItem(ItemManager.CreateByItemID(computerId), BaseEntity.GiveItemReason.PickedUp);
                    return;
                }

                BuildingManager.Building building = GetBuilding(signage);

                if (building == null)
                {
                    SendReply(player, msg("noBuilding", player.userID));
                    return;
                }

                link = linkManager.GetLinkOf(building);
                if (link != null && link.signage != null)
                {
                    SendReply(player, msg("hasController", player.userID));
                    return;
                }

                if (storedData.signIds.Contains(signage.net.ID))
                {
                    SendReply(player, msg("isController", player.userID));
                    return;
                }

                if (configData.Control.RequireComputer)
                {
                    if (player.inventory.GetAmount(computerId) < 1)
                    {
                        SendReply(player, msg("needComputer", player.userID));
                        return;
                    }
                    player.inventory.Take(null, computerId, 1);
                }

                storedData.signIds.Add(signage.net.ID);

                InitializeSign(signage);

                SaveData();
                SendReply(player, msg("controllerSuccess", player.userID));
                return;
            }
                        
            switch (args[0].ToLower())
            {
                case "add":
                    {
                        Item activeItem = player.GetActiveItem();
                        if (activeItem == null || activeItem.info.itemid != cameraId)
                        {
                            SendReply(player, "You must have a camera in your hands to activate the placement tool!");
                            return;
                        }

                        int cameraCount = 0;

                        BuildingManager.Building building = player.GetBuildingPrivilege()?.GetBuilding();
                        if (building != null)
                            cameraCount = linkManager.GetCamerasOf(building)?.Length ?? 0;

                        int cameraLimit = GetMaxCameras(player.userID); 
                        if (!permission.UserHasPermission(player.UserIDString, permIgnore) && cameraCount >= cameraLimit)
                        {
                            SendReply(player, msg("cameraLimit", player.userID));
                            return;
                        }

                        player.gameObject.AddComponent<ItemPlacement>();
                        SendReply(player, msg("enabled", player.userID));
                    }
                    return;
                case "remove":
                    {
                        CameraManager camera  = FindEntityFromRay(player)?.GetComponent<CameraManager>();
                        if (camera == null)
                        {
                            SendReply(player, msg("noCamera", player.userID));
                            return;
                        }

                        BuildingLinks.Link link = linkManager.GetLinkOf(camera);
                        if (link != null)
                            link.DestroyCamera(camera);
                        else linkManager.OnEntityDeath(camera.entity);
                       
                        player.GiveItem(ItemManager.CreateByItemID(cameraId), BaseEntity.GiveItemReason.PickedUp);
                        SendReply(player, msg("disabled", player.userID));
                    }
                    return;                
                default:
                    SendReply(player, msg("invalidCommand", player.userID));
                    return;
            }
        }
        #endregion

        #region Friends
        private bool IsFriendlyPlayer(ulong playerId, ulong friendId)
        {
            if (playerId == friendId || IsFriend(playerId, friendId) || IsClanmate(playerId, friendId))
                return true;
            return false;
        }

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
            bool isFriend = (bool)Friends?.Call("AreFriends", playerID, friendID);
            return isFriend;
        }
        #endregion

        #region Image Management
        private void LoadDefaultImages(int attempts = 0)
        {
            if (attempts > 3)
            {
                PrintError("ImageLibrary not found. Unable to load camera overlay UI");
                configData.Camera.Overlay = false;
                return;
            }

            if (configData.Camera.Overlay && !string.IsNullOrEmpty(configData.Camera.OverlayImage))
            {
                if (!ImageLibrary)
                {
                    timer.In(5, ()=> LoadDefaultImages(++attempts));
                    return;
                }
                AddImage("camoverlay", configData.Camera.OverlayImage);
            }

            if (!string.IsNullOrEmpty(configData.Control.Sign) && signImage == null)
                Add(configData.Control.Sign);
        }

        private void AddImage(string imageName, string fileName) => ImageLibrary.Call("AddImage", fileName, imageName, 0U);

        private string GetImage(string name) => (string)ImageLibrary.Call("GetImage", name);
       
        private WWW info;
        private void Add(string url)
        {
            info = new WWW(url);
            TryDownloadImage();
        }

        private byte[] GetImageBytes(WWW www)
        {
            var tex = www.texture;
            
            byte[] img = tex.EncodeToPNG();
            return img;
        }

        private void TryDownloadImage()
        {
            if (!info.isDone)
            {
                timer.In(1, TryDownloadImage);
                return;
            }
            if (!string.IsNullOrEmpty(info.error))
            {
                PrintError(string.Format("Failed to load the SecurityCamera sign image! Error: {0}", info.error));
                return;
            }
            else signImage = GetImageBytes(info);
        }
        #endregion

        #region Teleportation
        private void MovePosition(BasePlayer player, Vector3 destination, bool sleep)
        {
            if (sleep)
            {
                if (player.net?.connection != null)
                    player.ClientRPCPlayer(null, player, "StartLoading");
                StartSleeping(player);
                player.MovePosition(destination);
                if (player.net?.connection != null)
                    player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
                if (player.net?.connection != null)
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
                player.UpdateNetworkGroup();
                player.SendNetworkUpdateImmediate(false);
                if (player.net?.connection == null) return;
                try { player.ClearEntityQueue(null); } catch { }
                player.SendFullSnapshot();
            }
            else
            {
                player.MovePosition(destination);
                player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
                player.SendNetworkUpdateImmediate();
                try { player.ClearEntityQueue(null); } catch { }
            }
        }
        private void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
                return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Control Options")]
            public ControlOptions Control { get; set; }
            [JsonProperty(PropertyName = "Camera Options")]
            public CameraOptions Camera { get; set; }

            public class CameraOptions
            {
                [JsonProperty(PropertyName = "Allow friends and clan members to place/remove cameras and controllers")]
                public bool Friends { get; set; }
                [JsonProperty(PropertyName = "Maximum allowed cameras per base (Permission | Amount)")]
                public Dictionary<string, int> Max { get; set; }
                [JsonProperty(PropertyName = "Camera placement and removal distance")]
                public int Distance { get; set; }
                [JsonProperty(PropertyName = "Display camera overlay UI")]
                public bool Overlay { get; set; }
                [JsonProperty(PropertyName = "Camera overlay image URL")]
                public string OverlayImage { get; set; }
            }
            public class ControlOptions
            {                
                [JsonProperty(PropertyName = "Only allow camera access to players authorized on the base TC")]
                public bool Authorized { get; set; }
                [JsonProperty(PropertyName = "Sign controller image URL")]
                public string Sign { get; set; }
                [JsonProperty(PropertyName = "Require a targeting computer to create a controller")]
                public bool RequireComputer { get; set; }
                [JsonProperty(PropertyName = "UI Panel Colors")]
                public Dictionary<Colors, UIColor> UIColors { get; set; }

                public class UIColor
                {
                    [JsonProperty(PropertyName = "Color (hex)")]
                    public string Color { get; set; }
                    [JsonProperty(PropertyName = "Alpha (0.0 - 1.0)")]
                    public float Alpha { get; set; }
                }
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
                Camera = new ConfigData.CameraOptions
                {
                    Distance = 4,
                    Friends = true,
                    Max = new Dictionary<string, int>
                    {
                        ["securitycameras.use"] = 4,
                        ["securitycameras.pro"] = 10
                    },
                    Overlay = true,
                    OverlayImage = "http://www.chaoscode.io/oxide/Images/camera.png"
                },
                Control = new ConfigData.ControlOptions
                {
                    Authorized = true,
                    Sign = "http://www.chaoscode.io/oxide/Images/aslsign.png",
                    RequireComputer = true,
                    UIColors = new Dictionary<Colors, ConfigData.ControlOptions.UIColor>
                    {
                        [Colors.Background] = new ConfigData.ControlOptions.UIColor
                        {
                            Alpha = 0.98f,
                            Color = "#2b2b2b"
                        },
                        [Colors.Panel] = new ConfigData.ControlOptions.UIColor
                        {
                            Alpha = 1f,
                            Color = "#404141"
                        },
                        [Colors.Button] = new ConfigData.ControlOptions.UIColor
                        {
                            Alpha = 0.9f,
                            Color = "#767676"
                        },
                        [Colors.Selected] = new ConfigData.ControlOptions.UIColor
                        {
                            Alpha = 0.9f,
                            Color = "#ce422b"
                        }
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

            if (configData.Version < new VersionNumber(0, 1, 05))
                configData.Camera.Max = baseConfig.Camera.Max;

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management
        private void SaveData()
        {
            storedData.cameraData = cameraManagers.Where(x => x != null).Select(x => x.GetCameraData()).ToArray();
            data.WriteObject(storedData);
        }

        private void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
        }

        private class StoredData
        {
            public CameraManager.CameraData[] cameraData = new CameraManager.CameraData[0];
            public List<uint> signIds = new List<uint>();            
        }

        public class RestoreData
        {
            public Hash<ulong, PlayerData> restoreData = new Hash<ulong, PlayerData>();

            public void AddData(BasePlayer player)
            {
                restoreData[player.userID] = new PlayerData(player);
            }

            public void RemoveData(ulong playerId)
            {
                if (HasRestoreData(playerId))
                    restoreData.Remove(playerId);
            }

            public bool HasRestoreData(ulong playerId) => restoreData.ContainsKey(playerId);

            public void RestorePlayer(BasePlayer player)
            {
                PlayerData playerData;
                if (restoreData.TryGetValue(player.userID, out playerData))
                {
                    if (player.IsSleeping() || player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
                    {
                        ins.timer.Once(1, () => RestorePlayer(player));
                        return;
                    }

                    playerData.SetStats(player);
                    ins.MovePosition(player, playerData.position, true);
                    RestoreAllItems(player, playerData);
                }
            }

            private void RestoreAllItems(BasePlayer player, PlayerData playerData)
            {
                if (player == null || !player.IsConnected)
                    return;

                if (RestoreItems(player, playerData.containerBelt, "belt") && RestoreItems(player, playerData.containerWear, "wear") && RestoreItems(player, playerData.containerMain, "main"))
                    RemoveData(player.userID);
            }

            private bool RestoreItems(BasePlayer player, ItemData[] itemData, string type)
            {
                ItemContainer container = type == "belt" ? player.inventory.containerBelt : type == "wear" ? player.inventory.containerWear : player.inventory.containerMain;

                for (int i = 0; i < itemData.Length; i++)
                {
                    ItemData data = itemData[i];
                    if (data.amount < 1)
                        continue;

                    Item item = CreateItem(data);
                    item.position = data.position;
                    item.SetParent(container);
                }
                return true;
            }

            private Item CreateItem(ItemData itemData)
            {
                var item = ItemManager.CreateByItemID(itemData.itemid, itemData.amount, itemData.skin);
                item.condition = itemData.condition;
                if (itemData.instanceData != null)
                    item.instanceData = itemData.instanceData;

                var weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    if (!string.IsNullOrEmpty(itemData.ammotype))
                        weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(itemData.ammotype);
                    weapon.primaryMagazine.contents = itemData.ammo;
                }
                if (itemData.contents != null)
                {
                    foreach (var contentData in itemData.contents)
                    {
                        var newContent = ItemManager.CreateByItemID(contentData.itemid, contentData.amount);
                        if (newContent != null)
                        {
                            newContent.condition = contentData.condition;
                            newContent.MoveToContainer(item.contents);
                        }
                    }
                }
                return item;
            }

            public class PlayerData
            {
                public float[] stats;
                public Vector3 position;
                public ItemData[] containerMain;
                public ItemData[] containerWear;
                public ItemData[] containerBelt;

                public PlayerData() { }

                public PlayerData(BasePlayer player)
                {
                    stats = GetStats(player);
                    position = player.transform.position;
                    containerBelt = GetItems(player.inventory.containerBelt).ToArray();
                    containerMain = GetItems(player.inventory.containerMain).ToArray();
                    containerWear = GetItems(player.inventory.containerWear).ToArray();
                }

                private IEnumerable<ItemData> GetItems(ItemContainer container)
                {
                    return container.itemList.Select(item => new ItemData
                    {
                        itemid = item.info.itemid,
                        amount = item.amount,
                        ammo = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.contents ?? 0,
                        ammotype = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null,
                        position = item.position,
                        skin = item.skin,
                        condition = item.condition,
                        instanceData = item.instanceData ?? null,
                        contents = item.contents?.itemList.Select(item1 => new ItemData
                        {
                            itemid = item1.info.itemid,
                            amount = item1.amount,
                            condition = item1.condition
                        }).ToArray()
                    });
                }

                private float[] GetStats(BasePlayer player) => new float[] { player.health, player.metabolism.hydration.value, player.metabolism.calories.value };

                public void SetStats(BasePlayer player)
                {
                    player.health = stats[0];
                    player.metabolism.hydration.value = stats[1];
                    player.metabolism.calories.value = stats[2];
                    player.metabolism.SendChangesToClient();
                }
            }

            public class ItemData
            {
                public int itemid;
                public ulong skin;
                public int amount;
                public float condition;
                public int ammo;
                public string ammotype;
                public int position;
                public ProtoBuf.Item.InstanceData instanceData;
                public ItemData[] contents;
            }
        }
        #endregion

        #region Localization
        string msg(string key, ulong playerId = 0U) => lang.GetMessage(key, this, playerId == 0U ? null : playerId.ToString());
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {            
            ["help2"] = "<color=#ce422b>/sc add</color><color=#939393> - Activates the camera placement tool. Requires a camera in your hands!</color>",
            ["help3"] = "<color=#ce422b>/sc remove</color><color=#939393> - Remove the camera you are looking at</color>",
            ["help4"] = "<color=#ce422b>/sc controller</color><color=#939393> - Turn the sign you are looking at into a control panel for your security cameras</color>",
            ["help5"] = "<color=#ce422b>/sc controller remove</color><color=#939393> - Removes the camera controller from the sign you are looking at</color>",
            ["noAuth"] = "<color=#ce422b>You require tool cupboard access to use these commands!</color>",
            ["noLight"] = "<color=#939393>Unable to find a searchlight!</color>",
            ["notOwner"] = "<color=#939393>This entity does not belong to you!</color>",
            ["cameraLimit"] = "<color=#939393>This building already has the maximum number of security cameras!</color>",
            ["enabled"] = "<color=#939393>You have <color=#ce422b>enabled</color> the camera placement tool!</color>",
            ["notAdded"] = "<color=#939393>You can only remove cameras you have added!</color>",
            ["disabled"] = "<color=#939393>You have <color=#ce422b>removed</color> this security camera!</color>",
            ["noPerm"] = "<color=#939393>You do not have the required permission</color>",
            ["controlInUse"] = "<color=#939393>This controller is in use!</color>",
            ["ui.Title"] = "Security Control Panel",
            ["ui.NoCameras"] = "This controller has no linked cameras",
            ["ui.Scan"] = "Scan",
            ["ui.Information"] = "From this panel you can remote access any camera attached to this building",
            ["ui.Camera"] = "Camera {0}",
            ["ui.Control"] = "Control",
            ["ui.Back"] = "Back",
            ["ui.Next"] = "Next",
            ["ui.Page"] = "Page {0} / {1}",
            ["ui.Close"] = "Close",
            ["noSources"] = "<color=#ce422b>No valid cameras available!</color>",
            ["controlInfo"] = "<color=#939393>Press <color=#ce422b>'JUMP'</color> and <color=#ce422b>'DUCK'</color> to cycle through available cameras.\nPress <color=#ce422b>'USE'</color> to exit the controller!</color>",
            ["controlDestroyed"] = "<color=#ce422b>The control panel has been destroyed!</color>",
            ["noSign"] = "<color=#939393>Unable to find a valid sign!</color>",
            ["noBuilding"] = "<color=#939393>The camera controller needs to be attached to the building the cameras are on</color>",
            ["hasController"] = "<color=#939393>This building already has a camera controller</color>",
            ["isController"] = "<color=#939393>This sign is already a camera controller</color>",
            ["controllerSuccess"] = "<color=#939393>Camera controller successfully setup! You can access the controller by interacting with the computer attached to the sign</color>",
            ["invalidCommand"] = "<color=#939393>Invalid command! Type <color=#ce422b>/sc</color> for available commands</color>",
            ["noCamera"] = "<color=#939393>You are not looking at a security camera</color>",
            ["cameraDestroyed"] = "<color=#ce422b>This camera has been destroyed!</color>",
            ["noLinks"] = "<color=#939393>This controller has no linked cameras</color>",
            ["notController"] = "<color=#939393>This sign is not a camera controller</color>",
            ["removedController"] = "<color=#939393>You have successfully removed this camera controller</color>",
            ["needComputer"] = "<color=#939393>You need a targeting computer to create the camera controller</color>",
            ["placeHelp1"] = "<color=#939393>Use the <color=#ce422b>fire</color> button to place the camera</color>",
            ["placeHelp2"] = "<color=#939393>Cameras can only be placed on building blocks</color>",
            ["placeHelp3"] = "<color=#ce422b>Camera placement cancelled!</color>",
            ["placeHelp4"] = "<color=#ce422b>Camera placed!</color>",
            ["notAuthed"] = "<color=#939393>You need to be authorized on the tool cupboard to access the camera controller</color>",
        };
        #endregion
    }
}
