// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("My TugBoat", "NooBlet", "1.2.1")]
    [Description("Gives players the ability to own TugBoats")]
    public class MyTugBoat : CovalencePlugin
    {
        #region Varibles

        private Dictionary<ulong, PlayerInfo> playerday = new Dictionary<ulong, PlayerInfo>();
      
        private DynamicConfigFile PlayerDay_Data = Interface.Oxide.DataFileSystem.GetDatafile("MyTugBoat_PlayerDay");
        private Dictionary<ulong, DateTime> FetchCooldownData = new Dictionary<ulong, DateTime>();
        private static PluginConfig Settings;
        PDData pdData;
        private Dictionary<ulong, ulong> LastDriver = new Dictionary<ulong, ulong>();
        public static MyTugBoat _plugin;
        string tugboatPrefab = "assets/content/vehicles/boats/tugboat/tugboat.prefab";
        private const string permUse = "MyTugBoat.use";
        private const string permFetch = "MyTugBoat.Fetch";
        private const string permVip = "MyTugBoat.Vip";

        #endregion Varibles

        #region Hooks

        void OnServerInitialized()
        {
            _plugin = this;
            LoadData();
            LoadConfig();
            permission.RegisterPermission(permUse, this);
            permission.RegisterPermission(permFetch, this);
            permission.RegisterPermission(permVip, this);
            UpdateTugBoats();
        }

      

        void Unload()
        {
            SaveData();
            foreach (var m in Minicopter.serverEntities)
            {
                var mini = m.GetComponent<Minicopter>();
                if(mini == null) { continue; }
                if (mini.HasComponent<MiniDock>()) { mini.GetComponent<MiniDock>().DoDestroy(); }
            }
        }

        private void OnNewSave(string filename)
        {
            PlayerDay_Data.Clear();
            PlayerDay_Data.Save();
            playerday.Clear();

        }

        void OnEntityBuilt(Planner plan, GameObject go)
        {
            var entity = go.ToBaseEntity();
            if (entity == null) { return; }
            var door = entity.GetComponent<Door>();
            var boat = go.GetComponentsInParent<Tugboat>();
            if (entity.ShortPrefabName.Contains("door") && boat != null && door != null)
            {
                if (door.ShortPrefabName.Contains("wood"))
                {
                    SetDoorHealth(door, Settings.doorSettings.WoodenDoor);
                }
                if (door.ShortPrefabName.Contains("metal"))
                {
                    SetDoorHealth(door, Settings.doorSettings.MetalDoor);
                }
                if (door.ShortPrefabName.Contains("toptier"))
                {
                    SetDoorHealth(door, Settings.doorSettings.ArmoredDoor);
                }
                if (door.ShortPrefabName.Contains("industrial"))
                {
                    SetDoorHealth(door, Settings.doorSettings.IndustrialDoor);
                }
            }
        }



        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if (player.IsConnected)
            {
                if (!playerday.ContainsKey(player.userID))
                {
                    playerday.Add(player.userID, new PlayerInfo { tugsettings = new Playertug { TugboatSpeed = 200000, TugBoatFuelPerSec = 0.33f }, DisplayName = player.displayName,  tugboatday = DateTime.Now.Day - 1, Day = DateTime.Now.Day });
                  
                }
                CheckDate(player);
            
            }
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (LastDriver.ContainsKey(player.userID))
            {
               foreach(var m in Minicopter.serverEntities)
                {
                    var mini = m as Minicopter;
                    if(mini.net.ID.Value == LastDriver[player.userID])
                    {
                        if (mini.HasComponent<MiniDock>()) { mini.GetComponent<MiniDock>().DoDestroy(); }
                    }
                }
            }
        }

        void OnPlayerConnected(BasePlayer player)
        {
           
        }
       
        void OnEntityMounted(BaseMountable entity, BasePlayer player)
        {
            if (entity == null) { Puts("no entity"); return; }
            if (player == null) { Puts("no player"); return; }
          
            if (entity.ShortPrefabName == "tugboatdriver")
            {
                var tugboat = entity.GetParentEntity().GetComponent<Tugboat>();
                if (tugboat.OwnerID == 0) { tugboat.OwnerID = player.userID; }
            }
            else if (entity.ShortPrefabName == "miniheliseat")
            {
                if (LastDriver.ContainsKey(entity.VehicleParent().net.ID.Value)) { LastDriver[entity.VehicleParent().net.ID.Value] = player.userID; }
                else { LastDriver.Add(entity.VehicleParent().net.ID.Value, player.userID); }
                var mini = entity.GetParentEntity() as Minicopter;
                if (!mini.gameObject.HasComponent<MiniDock>()) { mini.gameObject.AddComponent<MiniDock>();}
                MiniDock md = entity?.VehicleParent()?.gameObject?.GetComponent<MiniDock>();
                if (md != null) { md.UnLatch(); }
               
            }
        }
        void CanDismountEntity(BasePlayer player, BaseMountable entity)
        {
           
            if (entity.ShortPrefabName == "miniheliseat" && player == entity?.VehicleParent()?.GetDriver())
            {
                if (LastDriver.ContainsKey(entity.VehicleParent().net.ID.Value)) { LastDriver[entity.VehicleParent().net.ID.Value] = entity.VehicleParent().GetDriver().userID; }
                else { LastDriver.Add(entity.VehicleParent().net.ID.Value, entity.VehicleParent().GetDriver().userID); }
            }
        }
        object OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null || entity.net == null) { return null; }           

            if (entity is Minicopter)
            {
                MiniDock md = entity?.gameObject?.GetComponent<MiniDock>();
                if (md != null) { md.rigidbody.ResetInertiaTensor(); }
            }
           
            return null;
        }

        private void OnEntitySpawned(Minicopter minicopter)
        {
            NextFrame(() => 
            {
               
                if (!minicopter.gameObject.HasComponent<MiniDock>()) { minicopter.gameObject.AddComponent<MiniDock>(); }
            }); 
        } 

        void OnEntityDeath(Tugboat entity, HitInfo info)
        {
            var tugboat = entity.GetComponent<Tugboat>();
            if (tugboat != null)
            {
                if (tugboat.OwnerID.IsSteamId())
                {
                    playerday[tugboat.OwnerID].tugsettings.TugboatSpeed = 200000;
                    playerday[tugboat.OwnerID].tugsettings.TugBoatFuelPerSec = 0.33f;
                }
            }
        }
        object OnEntityTakeDamage(Tugboat tug, HitInfo info)
        {
            if (tug== null || info.Initiator == null) { return null; }

            if (Settings.Tugnodecay)
            {
                if (info.damageTypes.Has(Rust.DamageType.Decay))
                {
                    return false;
                }
            }
           
            if (Settings.Invincibletug)
            {
                return false;
            }
            return null;
        }



        #endregion Hooks

        #region Methods
        private void SetDoorHealth(Door door, float health)
        {
            door.SetMaxHealth(health);
            door.SetHealth(health);
           
        }
        void SendToast(BasePlayer player, string message)
        {
            player.ShowToast(GameTip.Styles.Blue_Normal, message);
        }
        private bool CheckisCooldown(ulong userID)
        {
            if (!FetchCooldownData.ContainsKey(userID)) { return false; }
            if (FetchCooldownData[userID] > DateTime.Now) { return true; }
            return false;
        }

        private string getTimeRemain(BasePlayer player)
        {
            if (!FetchCooldownData.ContainsKey(player.userID))
            {
                return "00:00";
            }
            TimeSpan ts = FetchCooldownData[player.userID] - DateTime.Now;

            var time = $"{ts:mm}:{ts:ss}";

            return time;
        }
        private static Vector3 GetPosFromPlayer(BasePlayer player, int distance)
        {

            Vector3 finalpos = player.eyes.position + player.eyes.HeadForward() * distance;
            finalpos.y = TerrainMeta.WaterMap.GetHeight(finalpos);
            return finalpos;
        }
        private Tugboat GetPlayerTug(BasePlayer player)
        {
            foreach (var t in Tugboat.serverEntities)
            {
                var tug = t.GetComponent<Tugboat>();
                if (tug != null && tug.OwnerID == player.userID)
                {
                    return tug;
                }
            }
            return null;
        }
        private Quaternion GetFixedRotationForPlayer(BasePlayer player) =>
           Quaternion.Euler(0, player.GetNetworkRotation().eulerAngles.y - 135, 0);
        private void UpdateTugBoats()
        {
            foreach (var t in Tugboat.serverEntities)
            {
                var tugboat = t.GetComponent<Tugboat>();
                if (tugboat == null || !tugboat.OwnerID.IsSteamId()) { continue; }
                if (playerday.ContainsKey(tugboat.OwnerID))
                {
                    tugboat.engineThrust = playerday[tugboat.OwnerID].tugsettings.TugboatSpeed;
                    tugboat.fuelPerSec = playerday[tugboat.OwnerID].tugsettings.TugBoatFuelPerSec;
                }
            }
            foreach (var m in Minicopter.serverEntities)
            {
                var mini = m.GetComponent<Minicopter>();
                if (mini == null) { continue; }
                if (mini.HasComponent<MiniDock>()) { mini.GetComponent<MiniDock>().DoDestroy(); }
            }
        }
        void SpawnTugBoat(BasePlayer player, string prefab, float speed, float fuelpersec,int fuelamount)
        {          
            var entity = GameManager.server.CreateEntity(prefab, player.eyes.position + player.eyes.HeadForward() * 40f, player.eyes.GetAimRotation());
            Vector3 position = entity.transform.position;
            position.y = TerrainMeta.WaterMap.GetHeight(position);
            var TB = entity?.GetComponent<Tugboat>();

            if(TerrainMeta.WaterMap.GetHeight(position) > TerrainMeta.HeightMap.GetHeight(position))
            {
                entity.transform.position = position;                

                var tugboat = TB;
                tugboat.SetMaxHealth(Settings.tugHealthSetting);
                tugboat.SetHealth(Settings.tugHealthSetting);
                tugboat.health = Settings.tugHealthSetting;
                tugboat.startHealth = Settings.tugHealthSetting;
                entity.Spawn();
                tugboat.OwnerID = player.userID;

                EntityFuelSystem fuelsys = TB.GetFuelSystem();
                var container = fuelsys?.fuelStorageInstance.Get(TB.isServer)?.GetComponent<StorageContainer>();
                if (container == null) { return; }

                var item = ItemManager.CreateByItemID(-946369541, fuelamount);
                if (item == null) { return; }
                item.MoveToContainer(container.inventory);
                TB.engineThrust = speed;
                TB.fuelPerSec = fuelpersec;
            }
            else
            {
                entity.Kill();
                player.ChatMessage("Spawn Position not in water");
            }
         
           
        }
        bool canspawntugboat(BasePlayer player)
        {
            if (hastugboat(player)) { return false; }
            if (!Settings.usedayrestriction) { return true; }
            if (DateTime.Now.Day != playerday[player.userID].tugboatday)
            {
                playerday[player.userID].tugboatday = DateTime.Now.Day;
                return true;
            }
            player.ChatMessage("Your tugboat has been spawned today");
            return false;
        }
        bool hastugboat(BasePlayer player)
        {
            foreach (var t in Tugboat.serverEntities)
            {
                var tugboat = t.GetComponent<Tugboat>();
                if (tugboat == null) continue;
                if (tugboat.IsBroken()) { continue; }
                // Puts(tugboat.OwnerID.ToString());
                if (tugboat.OwnerID == player.userID) { player.ChatMessage("You already have a TugBoat"); return true; }
            }
            return false;
        }

        private void SaveTugData(BasePlayer player, int tugboatSpeed, float tugBoatFuelPerSec, int startingFuelAmmount)
        {
            playerday[player.userID].tugsettings.TugboatSpeed = tugboatSpeed;
            playerday[player.userID].tugsettings.TugBoatFuelPerSec = tugBoatFuelPerSec;
        }

        private void CheckDate(BasePlayer player)
        {
            if (DateTime.Now.Day != playerday[player.userID].Day)
            {
                playerday[player.userID].Day = DateTime.Now.Day;
                return;
            }
        }


        private BasePlayer findPlayer(string name)
        {
            BasePlayer target = BasePlayer.FindAwakeOrSleeping(name);

            return target;
        }



        #endregion Methods

        #region Commands

        [Command("tugboat")]
        private void tugboatCommand(IPlayer iplayer, string command, string[] args)
        {
            var player = iplayer.Object as BasePlayer;
            if (permission.UserHasPermission(player.UserIDString,permUse))
            {
                if(args.Length < 1)
                {
                    if (player.IsAdmin)
                    {
                        if (Settings.AdminLimitSpawn)
                        {
                            if (canspawntugboat(player))
                            {
                                SpawnTugBoat(player, tugboatPrefab, Settings.tugSettings.admintug.TugboatSpeed, Settings.tugSettings.admintug.TugBoatFuelPerSec, Settings.tugSettings.admintug.StartingFuelAmmount);
                                SaveTugData(player, Settings.tugSettings.admintug.TugboatSpeed, Settings.tugSettings.admintug.TugBoatFuelPerSec, Settings.tugSettings.admintug.StartingFuelAmmount);
                                return;
                            }
                            else { return; }
                        }
                        SpawnTugBoat(player, tugboatPrefab, Settings.tugSettings.admintug.TugboatSpeed, Settings.tugSettings.admintug.TugBoatFuelPerSec, Settings.tugSettings.admintug.StartingFuelAmmount);
                        SaveTugData(player, Settings.tugSettings.admintug.TugboatSpeed, Settings.tugSettings.admintug.TugBoatFuelPerSec, Settings.tugSettings.admintug.StartingFuelAmmount);
                    }
                    else
                    {
                        if (canspawntugboat(player))
                        {
                            if (permission.UserHasPermission(player.UserIDString, permVip))
                            {
                                SpawnTugBoat(player, tugboatPrefab, Settings.tugSettings.viptug.TugboatSpeed, Settings.tugSettings.viptug.TugBoatFuelPerSec, Settings.tugSettings.viptug.StartingFuelAmmount);
                                SaveTugData(player, Settings.tugSettings.viptug.TugboatSpeed, Settings.tugSettings.viptug.TugBoatFuelPerSec, Settings.tugSettings.viptug.StartingFuelAmmount);
                                return;
                            }
                            SpawnTugBoat(player, tugboatPrefab, Settings.tugSettings.playertug.TugboatSpeed, Settings.tugSettings.playertug.TugBoatFuelPerSec, Settings.tugSettings.playertug.StartingFuelAmmount);
                            SaveTugData(player, Settings.tugSettings.playertug.TugboatSpeed, Settings.tugSettings.playertug.TugBoatFuelPerSec, Settings.tugSettings.playertug.StartingFuelAmmount);
                        }
                    }
                }
                else
                {
                    if (args[0] == "fetch" && permission.UserHasPermission(player.UserIDString,permFetch))
                    {
                       
                        if (!player.IsAdmin && CheckisCooldown(player.userID)) { player.ChatMessage(GetLang("CooldownMessage", player)); return; }
                        Tugboat tug = GetPlayerTug(player);
                        if(tug != null)
                        {
                            Vector3 position = GetPosFromPlayer(player,40);
                            position.y = TerrainMeta.WaterMap.GetHeight(position);

                            if (TerrainMeta.WaterMap.GetHeight(position) > TerrainMeta.HeightMap.GetHeight(position))
                            {
                                tug.rigidBody.velocity = Vector3.zero;
                                tug.transform.SetPositionAndRotation(position, GetFixedRotationForPlayer(player));
                                tug.UpdateNetworkGroup();
                                tug.SendNetworkUpdateImmediate();
                                if (!FetchCooldownData.ContainsKey(player.userID)) { FetchCooldownData.Add(player.userID, DateTime.Now.AddMinutes(Settings.FetchCooldown)); } else { FetchCooldownData[player.userID] = DateTime.Now.AddMinutes(Settings.FetchCooldown); }

                            }
                            else
                            {
                                player.ChatMessage("Fetch Position not in Water");
                            }
                        }
                        else
                        {
                            player.ChatMessage("You Dont own a Tug");
                        }

                    }
                    else
                    {
                        player.ChatMessage("You Dont have Permission to fetch your Tug");
                    }
                }
              
            }
            else
            {
                player.ChatMessage("You Dont have Permission to use this command");
            }
        }



        [Command("tugboatreset")]
        private void tugboatrestCommand(IPlayer iplayer, string command, string[] args)
        {
            var player = iplayer.Object as BasePlayer;
            if (!player.IsAdmin) { return; }
            foreach(var t in Tugboat.serverEntities)
            {
                var tug = t.GetComponent<Tugboat>();
                if(tug!= null)
                {
                    tug.OwnerID = 0;
                }
            }
        }



        #endregion Commands

            #region Config

            #region Config Classes

        public class TugSettings
        {
            public Playertug playertug { get; set; }
            public Viptug viptug { get; set; }
            public Admintug admintug { get; set; }
        }
        public class Playertug
        {
            public int TugboatSpeed { get; set; }
            public float TugBoatFuelPerSec { get; set; }
            public int StartingFuelAmmount { get; set; }

        }
        public class Viptug
        {
            public int TugboatSpeed { get; set; }
            public float TugBoatFuelPerSec { get; set; }
            public int StartingFuelAmmount { get; set; }

        }
        public class Admintug
        {
            public int TugboatSpeed { get; set; }
            public float TugBoatFuelPerSec { get; set; }
            public int StartingFuelAmmount { get; set; }

        }
        public class DoorHealth
        {
            [JsonProperty(PropertyName = "Wooden Door Health (default = 200) ")]
            public float WoodenDoor { get; set; }

            [JsonProperty(PropertyName = "Metal Door Health (default = 250) ")]
            public float MetalDoor { get; set; }

            [JsonProperty(PropertyName = "Industrial Door Health (default = 250) ")]
            public float IndustrialDoor { get; set; }

            [JsonProperty(PropertyName = "Armor Door Health (default = 1000) ")] 
            public float ArmoredDoor { get; set; }

        }
        #endregion Config Classes


        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config.WriteObject(DefaultConfig(), true);

            PrintWarning("Default Configuration File Created");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                Settings = Config.ReadObject<PluginConfig>();
                if (Settings == null)
                {
                    throw new JsonException();
                }

                if (!Settings.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }
        protected override void SaveConfig()
        {
            LogWarning($"Configuration changes saved to {Name}.json");
            Config.WriteObject(Settings, true);
        }

        private class PluginConfig
        {
            [JsonProperty(PropertyName = "1. Incincible Tug ")]
            public bool Invincibletug { get; set; }

            [JsonProperty(PropertyName = "2. Tug no decay")]
            public bool Tugnodecay { get; set; }

            [JsonProperty(PropertyName = "3. Fetch Cooldown in Minutes")]
            public int FetchCooldown { get; set; }

            [JsonProperty(PropertyName = "4. Limit Admins to Spawn Limits (dalse will allow admin to unlimited spawns)")]
            public bool AdminLimitSpawn { get; set; }

            [JsonProperty(PropertyName = "5. Use 24Hour spawn Restriction (False will alow spawn after tug death)")]
            public bool usedayrestriction { get; set; }

            [JsonProperty(PropertyName = "6. Group Settings")]
            public TugSettings tugSettings { get; set; }

            [JsonProperty(PropertyName = "7. Tug Health Settings (default = 3000)")]
            public float tugHealthSetting { get; set; }

            [JsonProperty(PropertyName = "8. Door Health Settings")]
            public DoorHealth doorSettings { get; set; }

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }
        private PluginConfig DefaultConfig()
        {

            return new PluginConfig
            {
                Invincibletug = false,
                Tugnodecay = false,
                FetchCooldown = 10,
                tugHealthSetting = 3000,
                usedayrestriction = true,
                AdminLimitSpawn = true,

                tugSettings = new TugSettings
                {
                    playertug = new Playertug
                    {
                        TugBoatFuelPerSec = 0.33f,
                        TugboatSpeed = 200000,
                        StartingFuelAmmount = 100,
                      
                  },
                  viptug = new Viptug
                  {
                        TugBoatFuelPerSec = 0.20f,
                        TugboatSpeed = 400000,
                        StartingFuelAmmount = 200,

                  },
                  admintug = new Admintug
                  {
                      TugBoatFuelPerSec = 0f,
                      TugboatSpeed = 800000,
                      StartingFuelAmmount = 1000,
                  }
                },
                
                doorSettings = new DoorHealth
                {
                    WoodenDoor = 200,
                    MetalDoor = 250,
                    IndustrialDoor = 250,
                    ArmoredDoor = 1000,
                }
            };
        }

      
        

        #endregion Config

        #region Classes

        class PDData
        {
            public Dictionary<ulong, PlayerInfo> PlayerDay_data = new Dictionary<ulong, PlayerInfo>();
        }
        class PlayerInfo
        {
            public string DisplayName;            
            public int Day;           
            public int tugboatday;
            public Playertug tugsettings;
        }

        public class MiniDock : MonoBehaviour
        {
            public BaseVehicle mini = null;
            public Tugboat boat = null;
            public Rigidbody rigidbody = null;
            public bool Docked = false;
            public bool landed = false;

            private void Awake()
            {
                mini = GetComponent<Minicopter>();
                rigidbody = GetComponent<Rigidbody>();
            }

            public void UnLatch()
            {
                landed = false;
                if (mini != null && !mini.IsDestroyed && mini.HasDriver() && Docked)
                {
                    landed = false;
                    BasePlayer player = BasePlayer.FindByID(_plugin.LastDriver[mini.net.ID.Value]);
                    UnStick();
                    _plugin.SendToast(player, "Minicopter UnDocked");
                }

            }

            void UnStick()
            {
                mini.SetParent(null, true, true);
                rigidbody.constraints = RigidbodyConstraints.None;
                rigidbody.detectCollisions = true;
                Docked = false;
            }

            void Stick()
            {
                Quaternion q = mini.transform.rotation;
                q.x = boat.transform.rotation.x;
                q.z = boat.transform.rotation.z;
                mini.transform.rotation = q;
                mini.SetParent(boat, true, true);
                rigidbody.constraints = RigidbodyConstraints.FreezePosition;
                rigidbody.detectCollisions = false;
                Docked = true;
            }

            public bool IsAuthed(ulong player)
            {
                foreach (BaseEntity baseEntity in boat.children)
                {
                    VehiclePrivilege vehiclePrivilege = baseEntity as VehiclePrivilege;
                    if (vehiclePrivilege != null) { return vehiclePrivilege.IsAuthed(player); }
                }
                return true;
            }

            void OnCollisionStay(Collision collision)
            {
                if (Docked || landed) { return; }
                Tugboat tb = collision.gameObject.GetComponent<Tugboat>();

                if (tb != null && !mini.HasDriver() && !mini.HasParent())
                {
                    boat = tb;
                    if (_plugin.LastDriver.ContainsKey(mini.net.ID.Value))
                    {
                        BasePlayer player = BasePlayer.FindByID(_plugin.LastDriver[mini.net.ID.Value]);
                        if (player != null && IsAuthed(player.userID))
                        {
                            landed = true;
                            Stick();
                            if (player != null) { _plugin.SendToast(player, "Minicopter Docked"); }
                            return;
                        }
                        landed = true;
                        if (player != null) { _plugin.SendToast(player, "Tug auth required to dock"); }
                        return;
                    }
                    else
                    {
                        this.DoDestroy();
                        return;
                    }
                    
                }
            }

            public void DoDestroy()
            {
                if (Docked) { UnStick(); }
                Destroy(this);
            }
        }

        #endregion Classes

        #region Data Management
        void SaveData()
        {
            pdData.PlayerDay_data = playerday;
            PlayerDay_Data.WriteObject(pdData);
            Puts("DayData saved");
        }
        void SaveLoop() => timer.Once(900, () => { SaveData(); SaveLoop(); });
        void LoadData()
        {
            try
            {
                pdData = PlayerDay_Data.ReadObject<PDData>();
                playerday = pdData.PlayerDay_data;
            }
            catch
            {
                Puts("Couldn't load player data, creating new datafile");
                pdData = new PDData();
            }
        }
        #endregion Data Management

        #region Lang

        private string GetLang(string key, BasePlayer player)
        {
            return lang.GetMessage(key, this)                
                .Replace("{timeremaining}", getTimeRemain(player))
                .Replace("{cooldowntotal}", Settings.FetchCooldown.ToString());


        }
        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CooldownMessage"] = "Tug Fetch is on a cooldown for you . {timeremaining} remaining from {cooldowntotal} Min's",

            }, this, "en");
        }

        #endregion Lang
    }
}
