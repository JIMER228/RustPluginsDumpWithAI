// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
//Requires: RustNET
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using Network;

namespace Oxide.Plugins
{
    [Info("RemoteTurrets", "k1lly0u", "0.2.12", ResourceId = 0)]
      //  Слив плагинов server-rust by Apolo YouGame
    class RemoteTurrets : RustPlugin
    {
        #region Fields    
        [PluginReference] Plugin RocketTurrets;

        private StoredData storedData;
        private DynamicConfigFile data;
        
        private static RemoteTurrets ins;
        private static LinkManager linkManager;
        private static int layerMask;

        private List<TurretManager> turretManagers = new List<TurretManager>();
        
        private bool wipeData;

        const string setTurrets = "remoteturrets.set";
        const string RTUI_Overlay = "RTUI_Overlay";
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            permission.RegisterPermission(setTurrets, this);

            foreach (string key in configData.Options.Max.Keys)
            {
                if (permission.PermissionExists(key, this))
                    continue;
                permission.RegisterPermission(key, this);
            }

            lang.RegisterMessages(Messages, this);

            data = Interface.Oxide.DataFileSystem.GetFile("RustNET/remoteturrets");
            linkManager = new LinkManager();

            layerMask = LayerMask.GetMask("Default", "Water", "Deployed", "AI", "Vehicle_Movement", "World", "Player_Server", "Construction", "Terrain", "Tree");            
        }

        private void OnServerInitialized()
        {
            ins = this;            
            LoadData();

            if (wipeData)
            {
                storedData = new StoredData();
                SaveData();
            }

            LoadDefaultImages();

            InitializeAllLinks();
            RustNET.RegisterModule(Title, this);
        }    

        private void OnPlayerDisconnected(BasePlayer player)
        {
            Controller controller = player.GetComponent<Controller>();
            if (controller != null)
            {
                LinkManager.TurretLink link = linkManager.GetLinkOf(controller);
                if (link != null)
                    link.CloseLink(controller);
            }            
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

        private object OnPlayerTick(BasePlayer player, PlayerTick msg, bool wasPlayerStalled)
        {
            Controller controller = player.GetComponent<Controller>();
            if (controller != null)
                return false;
            return null;
        }

        private void OnEntityKill(BaseNetworkable networkable)
        {
            if (networkable != null)
                linkManager.OnEntityDeath(networkable);
        }        
       
        private void OnNewSave(string filename) => wipeData = true;

        private void OnServerSave() => SaveData();

        private void Unload()
        {
            SaveData();

            linkManager.DestroyAllLinks();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, RTUI_Overlay);

            ins = null;
            linkManager = null;
        }
        #endregion

        #region Functions
        private void InitializeAllLinks()
        {
            for (int i = storedData.registeredTurrets.Length - 1; i >= 0; i--)
            {
                TurretManager.TurretData turretData = storedData.registeredTurrets.ElementAt(i);

                AutoTurret turret = BaseEntity.serverEntities.Find(turretData.turretId) as AutoTurret;
                if (turret == null || !RustNET.linkManager.IsValidTerminal(turretData.terminalId))                
                    continue;                

                LinkManager.TurretLink link = linkManager.GetLinkOf(turretData.terminalId);
                if (link == null)
                    link = new LinkManager.TurretLink(turretData.terminalId, turret, turretData.turretName);
                else link.AddTurretToLink(turret, turretData.turretName);
            }           
        }

        private int GetMaxTurrets(ulong playerId)
        {
            int max = 0;
            foreach (var entry in configData.Options.Max)
            {
                if (permission.UserHasPermission(playerId.ToString(), entry.Key))
                {
                    if (max < entry.Value)
                        max = entry.Value;
                }
            }
            return max;
        }

        private void OnRocketTurretCreated(AutoTurret turret)
        {
            TurretManager manager = turret.GetComponent<TurretManager>();
            if (manager != null)
            {
                manager.CheckRocketTurret();
            }
        }
        #endregion

        #region RustNET Integration
        private void DestroyAllLinks() => linkManager.DestroyAllLinks();        

        private void OnLinkShutdown(int terminalId)
        {
            LinkManager.TurretLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)            
                link.OnLinkTerminated(false);
        }

        private void OnLinkDestroyed(int terminalId)
        {
            LinkManager.TurretLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)
                link.OnLinkTerminated(true);
        }

        private TurretManager[] GetAvailableTurrets(int terminalId)
        {
            LinkManager.TurretLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)            
                return link.managers.Where(x => x != null && x.turret != null && !x.turret.IsDestroyed).ToArray();            
            return new TurretManager[0];
        }

        private bool IsEntityEnabled(int terminalId, uint managerId)
        {
            LinkManager.TurretLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)
            {
                TurretManager manager = link.managers.Find(x => x.turret.net.ID == managerId);
                if (manager != null)                
                    return manager.IsEnabled();                
            }
            return false;
        }

        private void InitializeController(BasePlayer player, uint managerId, int terminalId)
        {
            LinkManager.TurretLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)
                link.InitiateLink(player, managerId);
        }

        private void ToggleAutomation(uint managerId, int terminalId, bool active)
        {
            LinkManager.TurretLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)
            {
                TurretManager manager = link.managers.Find(x => x.turret.net.ID == managerId);
                if (manager != null)                
                    manager.ToggleAutomation(active);                
            }
        }

        private void OpenInventory(BasePlayer player, uint managerId, int terminalId)
        {
            LinkManager.TurretLink link = linkManager.GetLinkOf(terminalId);
            if (link != null)
            {
                TurretManager manager = link.managers.Find(x => x.turret.net.ID == managerId);
                if (manager != null)                
                    RustNET.OpenInventory(player, manager.turret, manager.turret.inventory); 
            }
        }

        private string GetHelpString(ulong playerId, bool title) => title ? msg("UI.Help.Title", playerId) : msg("UI.Help", playerId);

        private bool AllowPublicAccess() => false;
        #endregion

        #region Components
        private class LinkManager
        {
            public List<TurretLink> links = new List<TurretLink>();
            
            public TurretLink GetLinkOf(TurretManager turret) => links.Find(x => x.managers.Contains(turret)) ?? null;

            public TurretLink GetLinkOf(Controller controller) => links.Find(x => x.controllers.Contains(controller)) ?? null;

            public TurretLink GetLinkOf(int terminalId) => links.Find(x => x.terminalId == terminalId) ?? null;

            public TurretLink GetLinkOf(AutoTurret turret)
            {
                TurretManager component = turret.GetComponent<TurretManager>();
                if (component == null)
                    return null;
                return GetLinkOf(component);
            }
            
            public void OnEntityDeath(BaseNetworkable networkable)
            {
                for (int i = links.Count - 1; i >= 0; i--)
                    links.ElementAt(i).OnEntityDeath(networkable);
            }

            public void DestroyAllLinks()
            {
                foreach (TurretLink link in links)                
                    link.OnLinkTerminated(false);  
            }

            public class TurretLink
            {
                public int terminalId { get; private set; }
                public List<Controller> controllers { get; private set; }
                public List<TurretManager> managers { get; private set; }

                public TurretLink() { }
                public TurretLink(int terminalId, AutoTurret turret, string turretName)
                {
                    this.terminalId = terminalId;
                    this.controllers = new List<Controller>();
                    this.managers = new List<TurretManager>();

                    AddTurretToLink(turret, turretName);
                    linkManager.links.Add(this);
                }

                public void AddTurretToLink(AutoTurret turret, string turretName)
                {
                    TurretManager manager = turret.gameObject.AddComponent<TurretManager>();
                    manager.turretName = turretName;
                    manager.terminalId = terminalId;
                    managers.Add(manager);
                    ins.turretManagers.Add(manager);

                    manager.SetTurretAutomation(true);
                }

                public void InitiateLink(BasePlayer player, uint managerId)
                {
                    TurretManager manager = managers.FirstOrDefault(x => x.turret.net.ID == managerId);
                    if (manager != null)
                    {                        
                        Controller controller = player.gameObject.AddComponent<Controller>();
                        controllers.Add(controller);
                        controller.InitiateLink(terminalId);
                        controller.SetTurretLink(this);
                        controller.SetSpectateTarget(managers.IndexOf(manager));
                    }
                }

                public void CloseLink(Controller controller, bool isDead = false)
                {
                    if (controller != null)
                    {                        
                        controllers.Remove(controller); 
                        controller.FinishSpectating(isDead);
                    }
                }               

                public void OnEntityDeath(BaseNetworkable networkable)
                {                    
                    TurretManager manager = networkable.GetComponent<TurretManager>();
                    if (manager != null && managers.Contains(manager))
                    {
                        if (manager.controller != null)
                        {
                            manager.controller.player.ChatMessage(ins.msg("Warning.TurretDestroyed", manager.controller.player.userID));
                            CloseLink(manager.controller);
                        }
                        ins.turretManagers.Remove(manager);
                        managers.Remove(manager);
                    }
                }

                public void OnLinkTerminated(bool isDestroyed)
                {
                    for (int i = controllers.Count - 1; i >= 0; i--)
                    {
                        Controller controller = controllers.ElementAt(i);
                        controller.player.ChatMessage(isDestroyed ? ins.msg("Warning.TerminalDestroyed", controller.player.userID) : ins.msg("Warning.TerminalShutdown", controller.player.userID));
                        CloseLink(controller);
                    }
                    
                    DestroyTurretManagers(isDestroyed);
                }
                
                private void DestroyTurretManagers(bool isDestroyed)
                {
                    foreach (TurretManager manager in managers)
                    {
                        if (isDestroyed)
                            ins.turretManagers.Remove(manager);

                        UnityEngine.Object.Destroy(manager);
                    }
                }
            }
        }

        private class Controller : RustNET.Controller
        {
            public TurretManager manager { get; private set; }

            private LinkManager.TurretLink link;
            private float nextShotTime;
            private float fireRate;

            private int spectateIndex;
            private bool switchingTargets;

            public override void Awake()
            {
                base.Awake();
                enabled = false;                
                fireRate = ins.configData.Turret.FireRate;                
            }

            private void Update()
            {
                if (player == null || player.serverInput == null || switchingTargets)
                    return;

                InputState input = player.serverInput;
                
                if (manager != null && manager.controller == this)
                {
                    manager.UpdateAimDirection(input);

                    if (input.WasJustPressed(BUTTON.FIRE_PRIMARY) || input.IsDown(BUTTON.FIRE_PRIMARY))
                        TryFireGun();
                }

                if (input.WasJustPressed(BUTTON.USE))
                {
                    enabled = false;
                    link.CloseLink(this);
                }
                else if (input.WasJustPressed(BUTTON.JUMP))
                    UpdateSpectateTarget(1);
                else if (input.WasJustPressed(BUTTON.DUCK))
                    UpdateSpectateTarget(-1);
            }

            public override void OnDestroy()
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
                CuiHelper.DestroyUi(player, uiAmmo);
                base.OnDestroy();
            }

            private void TryFireGun()
            {
                if (manager.isRocketTurret)
                {
                    ins.RocketTurrets.Call("TryFireRocket", manager.turret);
                }
                else
                {
                    if (UnityEngine.Time.realtimeSinceStartup >= nextShotTime)
                    {
                        manager.turret.EnsureReloaded();
                        if (manager.turret.HasAmmo())
                        {
                            manager.FireGun();
                            nextShotTime = UnityEngine.Time.realtimeSinceStartup + fireRate;                            
                            ins.CreateAmmoUI(player, this);
                        }
                    }
                }
            }            

            public void SetTurretLink(LinkManager.TurretLink link)
            {                
                this.link = link;
                BeginSpectating();                               
            }

            public void SetSpectateTarget(int spectateIndex)
            {
                this.spectateIndex = spectateIndex;
                manager = link.managers[spectateIndex];

                RustNET.MovePosition(player, manager.turret.transform.position, false);

                //if (manager.isDisabled)
                //{
                //    player.ChatMessage(ins.msg("Warning.IsDisabled", player.userID));
                //    return;
                //}
                if (manager.controller == null)
                {
                    manager.SetTurretStatus(true);
                    manager.SetController(this);
                    ins.CreateAmmoUI(player, this);
                }
                else player.ChatMessage(ins.msg("Warning.InUse", player.userID));

                CreateCameraOverlay();
            }

            public void UpdateSpectateTarget(int index = 0)
            {
                switchingTargets = true;
                player.Invoke(() => switchingTargets = false, 0.25f);

                int newIndex = spectateIndex + index;

                if (newIndex > link.managers.Count - 1)
                    newIndex = 0;
                else if (newIndex < 0)
                    newIndex = link.managers.Count - 1;

                if (spectateIndex == newIndex)
                    return;

                if (manager.controller == this)
                {
                    manager.SetController(null);
                    manager.SetTurretStatus(false);
                }
                manager = null;

                SetSpectateTarget(newIndex);
            }

            public void BeginSpectating()
            {                   
                player.inventory.Strip();

                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
                player.gameObject.SetLayerRecursive(10);
                player.CancelInvoke("InventoryUpdate");
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, true);

                if (Net.sv.write.Start())
                {
                    Net.sv.write.PacketID(Message.Type.EntityDestroy);
                    Net.sv.write.EntityID(player.net.ID);
                    Net.sv.write.UInt8((byte)BaseNetworkable.DestroyMode.None);
                    Net.sv.write.Send(new SendInfo(player.net.group.subscribers.Where(x => x.userid != player.userID).ToList()));
      //  Слив плагинов server-rust by Apolo YouGame
                }

                player.ChatMessage(ins.msg("Help.ControlInfo", player.userID));
      //  Слив плагинов server-rust by Apolo YouGame

                enabled = true;
            }

            public void FinishSpectating(bool isDead)
            {
                enabled = false;

                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
                player.gameObject.SetLayerRecursive(17);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
                player.InvokeRepeating("InventoryUpdate", 1f, 0.1f * UnityEngine.Random.Range(0.99f, 1.01f));
                player.Command("client.camoffset", new object[] { new Vector3(0, 1.2f, 0) });
                      
                if (manager.controller == this)
                {
                    manager.SetController(null);
                    manager.SetTurretStatus(false);
                }

                CuiHelper.DestroyUi(player, uiAmmo);
                CuiHelper.DestroyUi(player, RTUI_Overlay);

                if (!isDead)
                    Destroy(this);
            }

            public override void OnPlayerDeath(HitInfo info)
      //  Слив плагинов server-rust by Apolo YouGame
            {
                enabled = false;
                link.CloseLink(this, true);

                base.OnPlayerDeath(info);
            }

            private void CreateCameraOverlay()
            {
                if (!ins.configData.Remote.Overlay)
                    return;

                CuiElementContainer container = RustNET.UI.Container("0 0 0 0", "0 0", "1 1", false, "Under", RTUI_Overlay);
                RustNET.UI.Image(ref container, ins.GetImage("turretoverlay"), "0 0", "1 1", RTUI_Overlay);

                RustNET.UI.Panel(ref container, "0 0 0 0.4", "0.82 0.9", "0.96 0.94", RTUI_Overlay);
                RustNET.UI.Label(ref container, string.IsNullOrEmpty(manager.turretName) ? string.Format(ins.msg("UI.TurretName", player.userID), spectateIndex + 1) : manager.turretName, 18, "0.82 0.9", "0.96 0.94", TextAnchor.MiddleCenter, RTUI_Overlay);

                CuiHelper.DestroyUi(player, RTUI_Overlay);
                CuiHelper.AddUi(player, container);
            }
        }

        private class TurretManager : MonoBehaviour
        {
            public AutoTurret turret { get; private set; }
            public Controller controller { get; private set; }

            private Item ammoItem;

            public bool isBeingUsed { get; private set; }
            public bool isRocketTurret { get; private set; }
            public int terminalId { get; set; }
            public string turretName { get; set; }
            private bool wasOnline;
                        
            private Vector3 targetPos = Vector3.zero;
            private Vector3 offset = new Vector3(0, 0.3f, 0);

            private float aimCone;

            private void Awake()
            {
                turret = GetComponent<AutoTurret>();
                enabled = false;

                aimCone = ins.configData.Turret.AimCone;

                CheckRocketTurret();
            }

            public void CheckRocketTurret()
            {
                isRocketTurret = false;
                if (ins.RocketTurrets)
                {                    
                    if ((bool)ins.RocketTurrets.Call("IsRocketTurret", turret))
                    {
                        isRocketTurret = true;
                        ins.RocketTurrets.Call("ToggleTurretAutomation", turret, false);
                    }
                }
            }

            private void OnDestroy() => SetTurretStatus(false);

            public void SetTurretStatus(bool status)
            {
                if (!status)
                {
                    if (turret.IsInvoking(ClientAimDirection))
                        turret.CancelInvoke(ClientAimDirection);

                    turret.SetFlag(BaseEntity.Flags.On, wasOnline);

                    if (!turret.IsInvoking(turret.ServerTick))
                        turret.InvokeRepeating(turret.ServerTick, UnityEngine.Random.Range(0f, 1f), 0.015f);
                    if (!turret.IsInvoking(turret.SendAimDir))
                        turret.InvokeRandomized(turret.SendAimDir, UnityEngine.Random.Range(0f, 1f), 0.2f, 0.05f);
                    if (!turret.IsInvoking(turret.TargetScan))
                        turret.InvokeRandomized(turret.TargetScan, UnityEngine.Random.Range(0f, 1f), 1f, 0.1f);

                    if (wasOnline)
                        turret.InitiateStartup();
                    else turret.InitiateShutdown();                    
                }
                else
                {
                    wasOnline = turret.HasFlag(BaseEntity.Flags.On);

                    turret.enabled = false;
                    turret.CancelInvoke(turret.ServerTick);
                    turret.CancelInvoke(turret.SendAimDir);
                    turret.CancelInvoke(turret.TargetScan);

                    turret.InvokeRepeating(ClientAimDirection, 0f, 0.1f);                    
                }
            }

            public void SetTurretAutomation(bool status)
            {
                if (status)
                {
                    if (!turret.IsInvoking(turret.ServerTick))
                        turret.InvokeRepeating(turret.ServerTick, UnityEngine.Random.Range(0f, 1f), 0.015f);
                    if (!turret.IsInvoking(turret.SendAimDir))
                        turret.InvokeRandomized(turret.SendAimDir, UnityEngine.Random.Range(0f, 1f), 0.2f, 0.05f);
                    if (!turret.IsInvoking(turret.TargetScan))
                        turret.InvokeRandomized(turret.TargetScan, UnityEngine.Random.Range(0f, 1f), 1f, 0.1f);

                    turret.InitiateStartup();                    
                }
                else turret.InitiateShutdown();
            }

            public void SetController(Controller controller) => this.controller = controller;

            public void UpdateAimDirection(InputState input)
            {
                RaycastHit hit;
                if (Physics.Raycast(new Ray(turret.muzzlePos.transform.position, Quaternion.Euler(input.current.aimAngles) * Vector3.forward), out hit, 500f, layerMask))
                {
                    Vector3 targetPos = hit.point + offset;
                    Vector3 rotation = turret.gun_pitch.transform.InverseTransformPoint(turret.muzzlePos.transform.position);
                    rotation.x = 0;
                    rotation.z = 0;
                    Vector3 gunPitch = targetPos - (turret.gun_pitch.position + rotation);
                    turret.aimDir = gunPitch;
                    turret.UpdateAiming();
                }
            }

            public void FireGun()
            {
                RaycastHit raycastHit;
                Vector3 muzzlePos = turret.muzzlePos.transform.position - (turret.muzzlePos.forward * 0.25f);
                Vector3 forwardPos = turret.muzzlePos.transform.forward;
                float single = aimCone;
                Vector3 direction = Quaternion.Euler(UnityEngine.Random.Range(-aimCone * 0.5f, single * 0.5f), UnityEngine.Random.Range(-single * 0.5f, single * 0.5f), UnityEngine.Random.Range(-single * 0.5f, single * 0.5f)) * forwardPos;
                if (!Physics.Raycast(muzzlePos, direction, out raycastHit, 300f, 1084427009))
                {
                    targetPos = muzzlePos + (direction * 300f);
                }
                else
                {
                    targetPos = raycastHit.point;
                    if (raycastHit.collider)
                    {
                        BaseEntity baseEntity = raycastHit.collider.gameObject.ToBaseEntity();
                        if (baseEntity && baseEntity != this)
                        {
                            BaseCombatEntity baseCombatEntity = baseEntity as BaseCombatEntity;
                            float damage = 15f * UnityEngine.Random.Range(0.9f, 1.1f);
                            HitInfo hitInfo = new HitInfo(turret, baseEntity, Rust.DamageType.Bullet, damage, raycastHit.point);
      //  Слив плагинов server-rust by Apolo YouGame
                            if (!baseCombatEntity)
                                baseEntity.OnAttacked(hitInfo);
      //  Слив плагинов server-rust by Apolo YouGame
                            else
                            {
                                baseCombatEntity.OnAttacked(hitInfo);
      //  Слив плагинов server-rust by Apolo YouGame
                                if (baseCombatEntity is BasePlayer || baseCombatEntity is BaseNpc)
                                {
                                    HitInfo hitInfo1 = new HitInfo()
      //  Слив плагинов server-rust by Apolo YouGame
                                    {
                                        HitPositionWorld = raycastHit.point,
                                        HitNormalWorld = -direction,
                                        HitMaterial = StringPool.Get("Flesh")
                                    };
                                    Effect.server.ImpactEffect(hitInfo1);
      //  Слив плагинов server-rust by Apolo YouGame
                                }
                            }
                        }
                    }
                }
                turret.ClientRPC(null, "CLIENT_FireGun", targetPos);

                if (ammoItem == null || ammoItem.amount == 0)
                    FindAmmo();
                ConsumeAmmo();                
            }

            public bool IsEnabled()
            {
                if (controller == null)
                    return turret.HasFlag(BaseEntity.Flags.On) || turret.IsInvoking(turret.SetOnline);
                else return wasOnline;
            }

            public void ToggleAutomation(bool active)
            {
                if (controller == null)
                    SetTurretAutomation(active);
                else wasOnline = active;
            }

            private void ClientAimDirection() => turret.ClientRPC(null, "CLIENT_ReceiveAimDir", turret.aimDir);            

            private void FindAmmo()
            {
                foreach (Item item in turret.inventory.itemList)
                {
                    if (item.info.itemid != turret.ammoType.itemid || item.amount <= 0)
                        continue;
                    ammoItem = item;
                    return;
                }
            }

            private void ConsumeAmmo()
            {
                ammoItem.amount--;
                if (ammoItem.amount == 0)
                    FindAmmo();
            }

            public TurretData GetTurretData() => new TurretData(this);            

            public class TurretData
            {
                public uint turretId;
                public int terminalId;
                public string turretName;

                public TurretData() { }
                public TurretData(TurretManager manager)
                {
                    turretId = manager.turret.net.ID;
                    terminalId = manager.terminalId;
                    turretName = manager.turretName;
                }
            }
        }       
        #endregion
      
        #region UI   
        const string uiAmmo = "RT.AmmoUI";

        private void CreateAmmoUI(BasePlayer player, Controller controller)
        {
            CuiElementContainer container = RustNET.UI.Container(RustNET.UI.Color("#F2F2F2", 0.05f),"0.69 0.1", "0.83 0.135", false, "Overlay", uiAmmo);

            int ammoAmount = 0;
            foreach (var item in controller.manager.turret.inventory.itemList)
            {
                if (item.info.itemid == 815896488)
                    ammoAmount += item.amount;
            }

            RustNET.UI.Label(ref container, string.Format(msg("UI.Ammo", player.userID), ammoAmount), 12, "0.03 0", "1 1", TextAnchor.MiddleLeft, uiAmmo);
            DestroyUI(player);
            CuiHelper.AddUi(player, container);
        }

        private void DestroyUI(BasePlayer player) => CuiHelper.DestroyUi(player, uiAmmo);

        private void CreateConsoleWindow(BasePlayer player, int terminalId, int page)
        {
            CuiElementContainer container = RustNET.ins.GetBaseContainer(player, terminalId, Title);

            TurretManager[] entityIds = GetAvailableTurrets(terminalId);

            RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], "0.04 0.765", "0.96 0.8");
            RustNET.UI.Label(ref container, msg("UI.Select.Turret", player.userID), 12, "0.05 0.765", "0.5 0.8", TextAnchor.MiddleLeft);
            RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.MainMenu", player.userID), 11, "0.82 0.765", "0.96 0.8", $"rustnet.changepage {terminalId}");

            if (entityIds == null || entityIds.Length == 0)
                RustNET.UI.Label(ref container, msg("UI.NoTurrets", player.userID), 12, "0.05 0.5", "0.95 0.7");
            else
            {
                int count = 0;
                int startAt = page * 18;
                for (int i = startAt; i < (startAt + 18 > entityIds.Length ? entityIds.Length : startAt + 18); i++)
                {
                    TurretManager manager = entityIds.ElementAt(i);
                    RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {(0.725f - (count * 0.04f))}", $"0.96 {(0.755f - (count * 0.04f))}");
                    RustNET.UI.Label(ref container, string.IsNullOrEmpty(manager.turretName) ? string.Format(msg("UI.Turret", player.userID), count + 1) : $"> {manager.turretName}", 11, $"0.05 {0.725f - (count * 0.04f)}", $"0.31 {0.755f - (count * 0.04f)}", TextAnchor.MiddleLeft);

                    if (configData.Remote.ToggleEnabled)
                    {
                        bool isEnabled = manager.IsEnabled();
                        RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], isEnabled ? RustNET.msg("UI.Enable", player.userID) : RustNET.msg("UI.Disable", player.userID), 11, $"0.32 {0.725f - (count * 0.04f)}", $"0.53 {0.755f - (count * 0.04f)}", $"remoteturrets.toggle {manager.turret.net.ID} {terminalId} {page} {!isEnabled}");
                    }

                    if (configData.Remote.AccessInventory)                    
                        RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Inventory", player.userID), 11, $"0.54 {0.725f - (count * 0.04f)}", $"0.75 {0.755f - (count * 0.04f)}", $"remoteturrets.inventory {manager.turret.net.ID} {terminalId}");                    

                    if (configData.Remote.RemoteControl)
                        RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Control", player.userID), 11, $"0.76 {0.725f - (count * 0.04f)}", $"0.96 {0.755f - (count * 0.04f)}", $"remoteturrets.control {manager.turret.net.ID} {terminalId}");

                    count++;
                }

                int totalPages = entityIds.Length / 18;

                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Back", player.userID), 11, "0.3 0.01", "0.44 0.04", page > 0 ? $"rustnet.changepage {terminalId} {Title} {page - 1}" : "");
                RustNET.UI.Label(ref container, string.Format(RustNET.msg("UI.Page", player.userID), page + 1, totalPages + 1), 11, "0.44 0.01", "0.56 0.04");
                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Next", player.userID), 11, "0.56 0.01", "0.7 0.04", page + 1 <= totalPages ? $"rustnet.changepage {terminalId} {Title} {page + 1}" : "");
            }

            CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("remoteturrets.toggle")]
        private void ccmdToggle(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            if (!RustNET.linkManager.IsValidTerminal(arg.GetInt(1)))
            {
                CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);
                SendReply(player, RustNET.msg("Warning.TerminalDestroyed", player.userID));
                return;
            }

            ToggleAutomation(arg.GetUInt(0), arg.GetInt(1), arg.GetBool(3));
            RustNET.ins.DisplayToPlayer(player, arg.GetInt(1), Title, arg.GetInt(2));
        }

        [ConsoleCommand("remoteturrets.inventory")]
        private void ccmdAccessInventory(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);

            OpenInventory(player, arg.GetUInt(0), arg.GetInt(1));
        }

        [ConsoleCommand("remoteturrets.control")]
        private void ccmdControl(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            if (!RustNET.linkManager.IsValidTerminal(arg.GetInt(1)))
            {
                CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);
                SendReply(player, RustNET.msg("Warning.TerminalDestroyed", player.userID));
                return;
            }

            CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);
            InitializeController(player, arg.GetUInt(0), arg.GetInt(1));
        }
        #endregion

        #region Commands       
        [ChatCommand("rt")]
        void cmdRemote(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, setTurrets)) return;
            if (args.Length == 0)
            {
                SendReply(player, $"<color=#ce422b>{Title}</color><color=#939393>  v{Version}  -</color> <color=#ce422b>{Author} @ www.chaoscode.io</color>");
                SendReply(player, msg("Help.Main", player.userID));
                SendReply(player, msg("Help.Add", player.userID));
                SendReply(player, msg("Help.Remove", player.userID));
                SendReply(player, msg("Help.Name", player.userID));
                return;
            }
            
            BaseEntity entity = RustNET.FindEntityFromRay(player);

            if (entity == null || !(entity is AutoTurret))
            {
                SendReply(player, msg("Error.InvalidEntity", player.userID));
                return;
            }

            if (!RustNET.IsFriendlyPlayer(entity.OwnerID, player.userID))
            {
                SendReply(player, msg("Error.NotOwner", player.userID));
                return;
            }

            switch (args[0].ToLower())
            {                
                case "add":
                    {
                        if (args.Length < 2)
                        {
                            SendReply(player, msg("Error.TerminalID", player.userID));
                            SendReply(player, msg("Help.Add", player.userID));
                            return;
                        }
                        
                        if (entity.GetComponent<TurretManager>())
                        {
                            SendReply(player, msg("Error.AlreadyRegistered", player.userID));
                            return;
                        }

                        int terminalId;
                        if (!int.TryParse(args[1], out terminalId))
                        {
                            SendReply(player, msg("Error.TerminalID", player.userID));
                            return;
                        }

                        if (!RustNET.linkManager.IsValidTerminal(terminalId))
                        {
                            SendReply(player, msg("Error.RustNETID", player.userID));
                            return;
                        }

                        RustNET.LinkManager.Link link = RustNET.linkManager.GetLinkOf(terminalId);
                        if (link == null)
                        {
                            SendReply(player, msg("Error.NoLink", player.userID));
                            return;
                        }

                        BuildingManager.Building building = link.terminal.parentEntity.GetBuilding();
                        if (building == null)
                        {
                            SendReply(player, msg("Error.NoBuilding", player.userID));
                            return;
                        }

                        if (!building.GetDominatingBuildingPrivilege().IsAuthed(player))
                        {
                            SendReply(player, msg("Error.NoPrivilege", player.userID));
                            return;
                        }

                        if (Vector3.Distance(entity.transform.position, link.terminal.droppedItem.transform.position) > configData.Options.DistanceFromTerminal)
                        {
                            SendReply(player, msg("Error.FarDistance", player.userID));
                            return;
                        }

                        LinkManager.TurretLink turretLink = linkManager.GetLinkOf(terminalId);
                        if (turretLink == null)
                            turretLink = new LinkManager.TurretLink(terminalId, entity as AutoTurret, "");
                        else
                        {
                            int turretLimit = GetMaxTurrets(player.userID);
                            if (turretLink.managers.Count >= turretLimit)
                            {
                                SendReply(player, msg("Error.Limit", player.userID));
                                return;
                            }

                            turretLink.AddTurretToLink(entity as AutoTurret, "");
                        }

                        SendReply(player, msg("Success.Set", player.userID));
                        SaveData();
                    }
                    break;
                case "remove":                    
                    if (entity is AutoTurret)
                    {                        
                        if (!entity.GetComponent<TurretManager>())
                        {
                            SendReply(player, msg("Error.NotRegisteredTurret", player.userID));
                            return;
                        }

                        LinkManager.TurretLink link = linkManager.GetLinkOf(entity as AutoTurret);
                        if (link == null)
                        {
                            SendReply(player, msg("Error.NoLink", player.userID));
                            return;
                        }

                        TurretManager manager = entity.GetComponent<TurretManager>();
                        if (manager == null)
                        {
                            SendReply(player, msg("Error.NoTurretComponent", player.userID));
                            return;
                        }

                        if (manager.controller != null)
                            link.CloseLink(manager.controller);

                        link.managers.Remove(manager);
                        turretManagers.Remove(manager);
                        UnityEngine.Object.Destroy(manager);

                        SaveData();
                        SendReply(player, msg("Success.Remove", player.userID));
                        return;
                    }     
                    return;
                case "name":
                    {
                        if (args.Length < 2)
                        {
                            SendReply(player, msg("Error.NoNameSpecified", player.userID));
                            return;
                        }

                        TurretManager manager = RustNET.FindEntityFromRay(player)?.GetComponent<TurretManager>();
                        if (manager == null)
                        {
                            SendReply(player, msg("Error.NoEntity", player.userID));
                            return;
                        }

                        manager.turretName = args[1];

                        SendReply(player, string.Format(msg("Success.NameSet", player.userID), args[1]));
                    }
                    return;
                default:
                    SendReply(player, msg("Error.InvalidCommand", player.userID));
                    break;
            }           
        }
        #endregion  

        #region Image Management
        private void LoadDefaultImages(int attempts = 0)
        {
            if (attempts > 3)
            {
                PrintError("ImageLibrary not found. Unable to load camera overlay UI");
                configData.Remote.Overlay = false;
                return;
            }

            if (configData.Remote.Overlay && !string.IsNullOrEmpty(configData.Remote.OverlayImage)) 
                AddImage("turretoverlay", configData.Remote.OverlayImage);            

            if (!string.IsNullOrEmpty(configData.Remote.RustNETIcon))
                AddImage(Title, configData.Remote.RustNETIcon);
        }

        private void AddImage(string imageName, string fileName) => RustNET.ins.AddImage(imageName, fileName);

        private string GetImage(string name) => RustNET.ins.GetImage(name);
        #endregion

        #region Config        
        private ConfigData configData;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Turret Settings")]
            public TurretSettings Turret { get; set; }
            [JsonProperty(PropertyName = "Remote Settings")]
            public RemoteOptions Remote { get; set; }
            public OtherOptions Options { get; set; }

            public class TurretSettings
            {
                [JsonProperty(PropertyName = "Fire Rate - Time between shots in seconds")]
                public float FireRate { get; set; }
                [JsonProperty(PropertyName = "Aim Cone (Accuracy)")]
                public float AimCone { get; set; }
            }
            public class RemoteOptions
            {
                [JsonProperty(PropertyName = "Can players toggle turret automation")]
                public bool ToggleEnabled { get; set; }
                [JsonProperty(PropertyName = "Can players control the turret remotely")]
                public bool RemoteControl { get; set; }
                [JsonProperty(PropertyName = "Can players access the turret inventory")]
                public bool AccessInventory { get; set; }
                [JsonProperty(PropertyName = "Display camera overlay UI")]
                public bool Overlay { get; set; }
                [JsonProperty(PropertyName = "Camera overlay image URL")]
                public string OverlayImage { get; set; }
                [JsonProperty(PropertyName = "Turret icon URL for RustNET menu")]
                public string RustNETIcon { get; set; }
            }
            public class OtherOptions
            {
                [JsonProperty(PropertyName = "Maximum distance a turret controller can be set away from the terminal")]
                public float DistanceFromTerminal { get; set; }
                [JsonProperty(PropertyName = "Maximum allowed turrets per base (Permission | Amount)")]
                public Dictionary<string, int> Max { get; set; }
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
                Options = new ConfigData.OtherOptions
                {
                    DistanceFromTerminal = 50f,
                    Max = new Dictionary<string, int>
                    {
                        ["remoteturrets.set"] = 4,
                        ["remoteturrets.pro"] = 10
                    },
                },
                Turret = new ConfigData.TurretSettings
                {
                    AimCone = 4f,
                    FireRate = 0.115f
                },
                Remote = new ConfigData.RemoteOptions
                {
                    AccessInventory = true,
                    RemoteControl = true,
                    ToggleEnabled = true,
                    OverlayImage = "http://www.chaoscode.io/oxide/Images/RustNET/turretoverlay.png",
                    Overlay = true,
                    RustNETIcon = "http://www.chaoscode.io/oxide/Images/RustNET/turreticon.png"
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(0, 2, 0))
                configData = baseConfig;

            if (configData.Version < new VersionNumber(0, 2, 04))
                configData.Options.Max = baseConfig.Options.Max;

            if (configData.Version < new VersionNumber(0, 2, 10))            
                configData.Remote = baseConfig.Remote;            

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management
        private void SaveData()
        {
            storedData.registeredTurrets = turretManagers.Where(x => x != null && x.turret != null && !x.turret.IsDestroyed).Select(y => y.GetTurretData()).ToArray();
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
            public TurretManager.TurretData[] registeredTurrets = new TurretManager.TurretData[0];            
        }
       
        #endregion

        #region Localization
        string msg(string key, ulong playerId = 0U) => lang.GetMessage(key, this, playerId == 0U ? null : playerId.ToString());
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["Help.Main"] = "<color=#ce422b>/rustnet</color><color=#939393> - Display the help menu for using RustNET</color>",
            ["Help.Add"] = "<color=#ce422b>/rt add <terminal ID></color><color=#939393> - Set the turret you are looking at as a controlled turret</color>",
            ["Help.Remove"] = "<color=#ce422b>/rt remove</color><color=#939393> - Remove the remote for the turret you are looking at</color>",
            ["Help.ControlInfo"] = "<color=#939393>You can exit turret mode at any time by pressing <color=#ce422b>USE</color>, You can cycle through available turrets by pressing <color=#ce422b>JUMP</color> or <color=#ce422b>DUCK</color></color>",
      //  Слив плагинов server-rust by Apolo YouGame
            ["Help.Name"] = "<color=#ce422b>/rt name <name></color><color=#939393> - Set a name for the turret you are looking at</color>",
            ["Success.Set"] = "<color=#ce422b>Turret set as remote turret</color>",
            ["Success.Remove"] = "<color=#ce422b>You have successfuly removed this turrets remote</color>",
            ["Warning.InUse"] = "<color=#939393>This turret is already in use!</color>",
            ["Warning.TerminalDestroyed"] = "<color=#ce422b>The terminal has been destroyed!</color>",
            ["Warning.TurretDestroyed"] = "<color=#ce422b>The turret you were controlling has been destroyed!</color>",
            ["Warning.TerminalShutdown"] = "<color=#ce422b>The terminal has been shutdown</color>",
            ["Warning.IsDisabled"] = "<color=#ce422b>This turret is a specialty rocket turret that can not be operated!</color>",
            ["Error.InvalidEntity"] = "<color=#939393>You are not looking at a valid entity!</color>",
            ["Error.NotOwner"] = "<color=#939393>This turret does not belong to you!</color>",
            ["Error.AlreadyRegistered"] = "<color=#939393>This turret is already remotely controlled!</color>",           
            ["Error.TerminalID"] = "<color=#939393>You need to enter a valid terminal ID</color>",
            ["Error.RustNETID"] = "<color=#939393>Invalid terminal ID selected! You can find the terminal ID by opening the terminal</color>",
            ["Error.NoLink"] = "<color=#939393>[ERROR] Unable to find building link</color>",
            ["Error.NoBuilding"] = "<color=#939393>[ERROR] The selected terminal does not have a building</color>",
            ["Error.NoPrivilege"] = "<color=#939393>You do not have building privilege in the terminal building</color>",
            ["Error.FarDistance"] = "<color=#939393>This turret is too far away from the terminal</color>",
            ["Error.Limit"] = "<color=#939393>The specified terminal already has the maximum amount of connected remote turrets</color>",           
            ["Error.NotRegisteredTurret"] = "<color=#939393>The turret you are looking at has not been registered</color>",
            ["Error.NoTurretComponent"] = "<color=#939393>No automation component found on this turret</color>",
            ["Error.InvalidCommand"] = "<color=#939393>Invalid command! Type <color=#ce422b>/rt</color> for available commands</color>",
            ["Error.NoNameSpecified"] = "<color=#939393>You must enter a name for the turret!</color>",
            ["Error.NoEntity"] = "<color=#939393>You are not looking at a remote turret</color>",
            ["Success.NameSet"] = "<color=#939393>You have set the name of this turret to <color=#ce422b>{0}</color></color>",
            ["UI.Ammo"] = "AMMO :  <color=#ce422b>{0}</color>",
            ["UI.Select.Turret"] = "> <color=#28ffa6>Turrets</color> <",
            ["UI.Turret"] = "> Turret {0}",
            ["UI.TurretName"] = "Turret {0}",
            ["UI.NoTurrets"] = "No turrets registered to this terminal",
            ["UI.Help.Title"] = "> <color=#28ffa6>Turret Help Menu</color> <",
            ["UI.Help"] = "> To register a turret you will need the terminal ID noted above.\n\n> Creating a remote turret\nStep 1. Deploy a turret in or around your base.\nStep 2. Look at your turret and type <color=#28ffa6>/rt add <terminal ID></color> replacing <terminal ID> with the ID of the terminal you are using.\n\nYour turret is now registered to the terminal and can be accessed remotely via this control panel.\nYou can also toggle the turret on or off and access the turrets inventory.\n\n> Removing a turret and restoring it to default\nTo remove a turret look at it and type <color=#28ffa6>/rt remove</color>. This will remove its remote functionality and restore it to default",
        };
        #endregion        
    }
}
