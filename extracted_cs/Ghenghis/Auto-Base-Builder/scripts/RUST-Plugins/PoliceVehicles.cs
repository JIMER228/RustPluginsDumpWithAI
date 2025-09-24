using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core;
using ConVar;
using Mono.Cecil.Cil;
using static CombatLog;

namespace Oxide.Plugins
{
    [Info("PoliceVehicles", "HexOptimal", "1.5.0")]
    [Description("Allow the spawning of police vehicles")]
    internal class PoliceVehicles : RustPlugin
    {
        #region Config
        [PluginReference]
        private Plugin SpawnModularCar;
        [PluginReference]
        private Plugin VehicleDeployedLocks;

        public string rhibprefab = "assets/content/vehicles/boats/rhib/rhib.prefab";
        public string bluelightprefab = "assets/prefabs/deployable/playerioents/lights/flasherlight/electric.flasherlight.deployed.prefab";
        public string buttonPrefab = "assets/prefabs/io/electric/switches/pressbutton/pressbutton.prefab";
        public string strobelight = "assets/content/props/strobe light/strobelight.prefab";
        public string phoneprefab = "assets/prefabs/voiceaudio/telephone/telephone.deployed.prefab";
        public string minicopterPrefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab";
        public string attackHeliPrefab = "assets/content/vehicles/attackhelicopter/attackhelicopter.entity.prefab";
        public string SpherePrefab = "assets/prefabs/visualization/sphere.prefab";
        public string SearchLightPrefab = "assets/prefabs/deployable/search light/searchlight.deployed.prefab";
        public string scrapheliPrefab = "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab";
        public string boombox = "assets/prefabs/voiceaudio/boombox/boombox.deployed.prefab";
        public string snowmobile = "assets/content/vehicles/snowmobiles/tomahasnowmobile.prefab";
        public string trainPrefab = "assets/content/vehicles/trains/locomotive/locomotive.entity.prefab";
        public string tugboatPrefab = "assets/content/vehicles/boats/tugboat/tugboat.prefab";
        public string computerStationPrefab = "assets/prefabs/deployable/computerstation/computerstation.deployed.prefab";
        public string metalDoorPrefab = "assets/prefabs/building/door.hinged/door.hinged.metal.prefab";
        public string cellWallPrefab = "assets/prefabs/building/wall.frame.cell/wall.frame.cell.prefab";
        public string cellGatePrefab = "assets/prefabs/building/wall.frame.cell/wall.frame.cell.gate.prefab";
        public string metalRoofPrefab = "assets/prefabs/building/floor.grill/floor.grill.prefab";
        private static readonly int GlobalLayerMask = LayerMask.GetMask("Construction", "Default", "Deployed", "Resource", "Terrain", "Water", "World");

        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Police car fuel amount on spawn")]
            public int policeCarFuel = 500;
            [JsonProperty(PropertyName = "Police transport vehicle fuel amount on spawn")]
            public int policeTransportFuel = 500;
            [JsonProperty(PropertyName = "Police snowmobile vehicle fuel amount on spawn")]
            public int policeSnowMobileFuel = 500;
            [JsonProperty(PropertyName = "Police heli fuel amount on spawn")]
            public int policeHeliFuel = 500;
            [JsonProperty(PropertyName = "Police heli large fuel amount on spawn")]
            public int policeHeliLargeFuel = 500;
            [JsonProperty(PropertyName = "Police attack heli fuel amount on spawn")]
            public int policeHeliAttackFuel = 500;
            [JsonProperty(PropertyName = "Police boat fuel amount on spawn")]
            public int policeBoatFuel = 500;
            [JsonProperty(PropertyName = "Police tugboat fuel amount on spawn")]
            public int policeTugboatFuel = 500;
            [JsonProperty(PropertyName = "Police train fuel amount on spawn")]
            public int policeTrainFuel = 500;
            [JsonProperty(PropertyName = "Lock police car engine parts")]
            public bool lockCar = true;
            [JsonProperty(PropertyName = "Lock police transport vehicle engine parts")]
            public bool lockTransport = true;
            [JsonProperty(PropertyName = "Police car engine parts tier")]
            public int carTier = 3;
            [JsonProperty(PropertyName = "Police Transport engine parts tier")]
            public int transportTier = 3;
            [JsonProperty(PropertyName = "Lock police car fuel")]
            public bool policeCarFuelLock = true;
            [JsonProperty(PropertyName = "Lock police transport fuel")]
            public bool policeTransportFuelLock = true;
            [JsonProperty(PropertyName = "Lock police snowmobile fuel")]
            public bool policeSnowmobileFuelLock = true;
            [JsonProperty(PropertyName = "Lock police heli fuel")]
            public bool policeHeliFuelLock = true;
            [JsonProperty(PropertyName = "Lock police heli large fuel")]
            public bool policeHeliLargeFuelLock = true;
            [JsonProperty(PropertyName = "Lock police boat fuel")]
            public bool policeBoatFuelLock = true;
            [JsonProperty(PropertyName = "Lock police train fuel")]
            public bool policeTrainFuelLock = true;
            [JsonProperty(PropertyName = "Police Heli spawn spotlight")]
            public bool heliSpotLight = true;
            [JsonProperty(PropertyName = "Police Boat spawn spotlight")]
            public bool boatSpotLight = true;
            [JsonProperty(PropertyName = "Broadcast message when vehicles spawn")]
            public bool broadcastspawn = false;
            [JsonProperty(PropertyName = "Siren radio station")]
            public string radioStnUrl = "http://stream.zeno.fm/7cyp2kaxhchvv";
        }

        private bool LoadConfigVariables()
        {
            try
            {
                configData = Config.ReadObject<ConfigData>();
            }
            catch
            {
                return false;
            }
            SaveConfig(configData);
            return true;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            configData = new ConfigData();
            SaveConfig(configData);
        }

        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }


        #endregion
        #region Data

        StoredData storedData;
        class StoredData
        {
            public Dictionary<ulong, ulong> currentVehicle = new Dictionary<ulong, ulong>();
            public Dictionary<ulong, List<ulong>> currentVehicleUnlimited = new Dictionary<ulong, List<ulong>>();
            public HashSet<ulong> LightsActivated = new HashSet<ulong>();
        }

        void Loaded()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("PoliceVehicles");
            Interface.Oxide.DataFileSystem.WriteObject("PoliceVehicles", storedData);
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("PoliceVehicles", storedData);
        }

        void Unload()
        {
            SaveData();
        }

        void OnNewSave(string filename)
        {
            storedData.currentVehicle.Clear();
            storedData.currentVehicleUnlimited.Clear();
            storedData.LightsActivated.Clear();
            SaveData();
        }

        #endregion
        #region Commands

        [ChatCommand("policecar")]
        void policecar(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                SendReply(player, "You do not have permission to use that command");
                return;
            }
            if (storedData.currentVehicle.ContainsKey(player.userID))
            {
                SendReply(player, "You can only use one vehicle at a time, use /removevehicle to remove your current vehicle");
                return;
            }
            var policecar = SpawnPoliceCar(player);
            if (policecar == null) return;

            if(configData.policeCarFuelLock) lockFuel(policecar);

            spawnentity(policecar, buttonPrefab, new Vector3(0f, 1f, -1.5f), new Quaternion(0f, -0.707f, -0.707f, 0f));
            spawnentity(policecar, bluelightprefab, new Vector3(-0.6f, 2.05f, -0.3f), new Quaternion());
            spawnentity(policecar, bluelightprefab, new Vector3(0.6f, 2.05f, -0.3f), new Quaternion());
            spawnentity(policecar, bluelightprefab, new Vector3(-0.6f, 2.05f, -2.2f), new Quaternion());
            spawnentity(policecar, bluelightprefab, new Vector3(0.6f, 2.05f, -2.2f), new Quaternion());
            spawnentity(policecar, bluelightprefab, new Vector3(-0.4f, 0.55f, 2.3f), new Quaternion(0f, 90f, 90f, 0f));
            spawnentity(policecar, bluelightprefab, new Vector3(0.4f, 0.55f, 2.3f), new Quaternion(0f, 90f, 90f, 0f));
            spawnentity(policecar, bluelightprefab, new Vector3(0.8f, 0.52f, -2.4f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
            spawnentity(policecar, bluelightprefab, new Vector3(-0.8f, 0.52f, -2.4f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
            spawnentity(policecar, strobelight, new Vector3(-0.4f, 0.55f, 2.3f), new Quaternion());
            spawnentity(policecar, strobelight, new Vector3(0.4f, 0.55f, 2.3f), new Quaternion());
            spawnaudio(policecar, boombox, new Vector3(0f, 1.4f, 0.2f), new Quaternion(0f, 90f, 0f, 0f));
            broadcastSpawn(player, "police car");
            if (permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                storedData.currentVehicle.Add(player.userID, policecar.net.ID.Value);
            }
            else
            {
                if (storedData.currentVehicleUnlimited.ContainsKey(player.userID))
                {
                    storedData.currentVehicleUnlimited[player.userID].Add(policecar.net.ID.Value);
                }
                else
                { 
                    List<ulong> vehicles = new List<ulong>();
                    vehicles.Add(policecar.net.ID.Value);
                    storedData.currentVehicleUnlimited.Add(player.userID, vehicles);
                }
                
            }
        }
        [ChatCommand("policetransport")]
        void policetransport(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                SendReply(player, "You do not have permission to use that command");
                return;
            }
            if (storedData.currentVehicle.ContainsKey(player.userID))
            {
                SendReply(player, "You can only use one vehicle at a time, use /removevehicle to remove your current vehicle");
                return;
            }
            var policecar = SpawnPoliceTransport(player);
            if (policecar == null) return;

            if (configData.policeTransportFuelLock) lockFuel(policecar);

            spawnentity(policecar, buttonPrefab, new Vector3(0f, 1f, -0.8f), new Quaternion(0f, -0.707f, -0.707f, 0f));
            spawnentity(policecar, bluelightprefab, new Vector3(-0.6f, 2.05f, 0.5f), new Quaternion());
            spawnentity(policecar, bluelightprefab, new Vector3(0.6f, 2.05f, 0.5f), new Quaternion());
            spawnentity(policecar, bluelightprefab, new Vector3(-0.6f, 2.05f, -3f), new Quaternion());
            spawnentity(policecar, bluelightprefab, new Vector3(0.6f, 2.05f, -3f), new Quaternion());
            spawnentity(policecar, bluelightprefab, new Vector3(-0.4f, 0.55f, 3f), new Quaternion(0f, 90f, 90f, 0f));
            spawnentity(policecar, bluelightprefab, new Vector3(0.4f, 0.55f, 3f), new Quaternion(0f, 90f, 90f, 0f));
            spawnentity(policecar, bluelightprefab, new Vector3(0.8f, 0.52f, -3.15f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
            spawnentity(policecar, bluelightprefab, new Vector3(-0.8f, 0.52f, -3.15f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
            spawnentity(policecar, strobelight, new Vector3(-0.4f, 0.55f, 2.3f), new Quaternion());
            spawnentity(policecar, strobelight, new Vector3(0.4f, 0.55f, 2.3f), new Quaternion());
            spawnaudio(policecar, boombox, new Vector3(0f, 1.4f, 0.9f), new Quaternion(0f, 90f, 0f, 0f));
            broadcastSpawn(player, "police transporter");
            if (permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                storedData.currentVehicle.Add(player.userID, policecar.net.ID.Value);
            }
            else
            {
                if (storedData.currentVehicleUnlimited.ContainsKey(player.userID))
                {
                    storedData.currentVehicleUnlimited[player.userID].Add(policecar.net.ID.Value);
                }
                else
                {
                    List<ulong> vehicles = new List<ulong>();
                    vehicles.Add(policecar.net.ID.Value);
                    storedData.currentVehicleUnlimited.Add(player.userID, vehicles);
                }

            }
        }


        [ChatCommand("policesnowmobile")]
        void policesnowmobile(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                SendReply(player, "You do not have permission to use that command");
                return;
            }
            if (storedData.currentVehicle.ContainsKey(player.userID))
            {
                SendReply(player, "You can only use one vehicle at a time, use /removevehicle to remove your current vehicle");
                return;
            }
            RaycastHit hit;
            if (UnityEngine.Physics.Raycast(player.eyes.HeadRay(), out hit, Mathf.Infinity, GlobalLayerMask))
            {
                Vector3 position = hit.point + Vector3.up * 2f;
                BaseVehicle policesnowmobile = (BaseVehicle)GameManager.server.CreateEntity(snowmobile, position);
                if (policesnowmobile == null) return;
                policesnowmobile.OwnerID = player.userID;
                policesnowmobile.Spawn();
                policesnowmobile.GetFuelSystem().AddFuel(configData.policeSnowMobileFuel);

                addLock(policesnowmobile, player);
                if (configData.policeSnowmobileFuelLock) lockFuel(policesnowmobile);

                spawnentity(policesnowmobile, buttonPrefab, new Vector3(-0.22f, 1.65f, 0.02f), new Quaternion(1f, 0f, 0f, 0f));
                spawnentity(policesnowmobile, bluelightprefab, new Vector3(-0.25f, 0.4f, 0.7f), new Quaternion(0f, 90f, 90f, 0f));
                spawnentity(policesnowmobile, bluelightprefab, new Vector3(0.25f, 0.4f, 0.7f), new Quaternion(0f, 90f, 90f, 0f));
                spawnentity(policesnowmobile, bluelightprefab, new Vector3(-0.165f, 0.24f, -1.1f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
                spawnentity(policesnowmobile, bluelightprefab, new Vector3(0.165f, 0.24f, -1.1f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
                spawnentity(policesnowmobile, bluelightprefab, new Vector3(-0.05f, 0.42f, -1.25f), new Quaternion());
                spawnentity(policesnowmobile, bluelightprefab, new Vector3(0.05f, 0.42f, -1.25f), new Quaternion());
                spawnentity(policesnowmobile, bluelightprefab, new Vector3(-0.25f, 0.465f, 0.26f), new Quaternion());
                spawnentity(policesnowmobile, bluelightprefab, new Vector3(0.25f, 0.465f, 0.26f), new Quaternion());
                spawnentity(policesnowmobile, strobelight, new Vector3(-0.1f, 0.45f, 0.76f), new Quaternion(0,0,1,0));
                spawnentity(policesnowmobile, strobelight, new Vector3(0.1f, 0.45f, 0.76f), new Quaternion(0, 0, 1, 0));
                spawnaudio(policesnowmobile, boombox, new Vector3(0.22f, 0.68f, 0.02f), new Quaternion(0f, 90f, 0f, 0f));

                broadcastSpawn(player, "police Snowmobile");
                if (permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
                {
                    storedData.currentVehicle.Add(player.userID, policesnowmobile.net.ID.Value);
                }
                else
                {
                    if (storedData.currentVehicleUnlimited.ContainsKey(player.userID))
                    {
                        storedData.currentVehicleUnlimited[player.userID].Add(policesnowmobile.net.ID.Value);
                    }
                    else
                    {
                        List<ulong> vehicles = new List<ulong>();
                        vehicles.Add(policesnowmobile.net.ID.Value);
                        storedData.currentVehicleUnlimited.Add(player.userID, vehicles);
                    }

                }

            }
        }


        [ChatCommand("policeheli")]
        void policeheli(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                SendReply(player, "You do not have permission to use that command");
                return;
            }
            if (storedData.currentVehicle.ContainsKey(player.userID))
            {
                SendReply(player, "You can only use one vehicle at a time, use /removevehicle to remove your current vehicle");
                return;
            }
            RaycastHit hit;
            if (UnityEngine.Physics.Raycast(player.eyes.HeadRay(), out hit, Mathf.Infinity, GlobalLayerMask))
            {
                Vector3 position = hit.point + Vector3.up * 2f;
                BaseVehicle policeheli = (BaseVehicle)GameManager.server.CreateEntity(minicopterPrefab, position);
                if (policeheli == null) return;
                policeheli.OwnerID = player.userID;
                policeheli.Spawn();
                policeheli.GetFuelSystem().AddFuel(configData.policeHeliFuel);
                addLock(policeheli, player);

                //if (configData.policeHeliFuelLock) lockFuel(policeheli);

                spawnentity(policeheli, bluelightprefab, new Vector3(0, 1.2f, -2.0f), new Quaternion());
                spawnentity(policeheli, bluelightprefab, new Vector3(0, 2.2f, -0.02f), new Quaternion());
                spawnentity(policeheli, bluelightprefab, new Vector3(0f, 0.83f, -2.6f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
                spawnentity(policeheli, bluelightprefab, new Vector3(0f, 0.3f, 2.1f), new Quaternion(0f, 90f, 90f, 0f));
                spawnentity(policeheli, bluelightprefab, new Vector3(0f, 0.25f, -0.75f), new Quaternion(90f, 0f, 0f, 0f));
                spawnentity(policeheli, bluelightprefab, new Vector3(0f, 0.25f, 1.1f), new Quaternion(90f, 0f, 0f, 0f));
                spawnentity(policeheli, buttonPrefab, new Vector3(0, 0.25f, -0.4f), new Quaternion(0f, -0.707f, -0.707f, 0f));
                spawnaudio(policeheli, boombox, new Vector3(-0.22f, 0.9f, 0.85f), new Quaternion(0f, 90f, 0f, 0f));

                if (configData.heliSpotLight == true) {
                    SphereEntity helilightsphere = (SphereEntity)GameManager.server.CreateEntity(SpherePrefab, policeheli.transform.position, new Quaternion(0, 0, 0, 0), true);
                    if (helilightsphere == null) return;
                    RemoveColliderProtection(helilightsphere);
                    helilightsphere.Spawn();
                    helilightsphere.SetParent(policeheli);
                    helilightsphere.transform.localPosition = new Vector3(0, -100, 0);
                    SearchLight searchLight = GameManager.server.CreateEntity(SearchLightPrefab, helilightsphere.transform.position) as SearchLight;
                    if (searchLight == null) return;
                    RemoveColliderProtection(searchLight);
                    searchLight.Spawn();
                    searchLight.SetFlag(BaseEntity.Flags.On, true);
                    searchLight.SetParent(helilightsphere);
                    searchLight.transform.localPosition = new Vector3(0, 0, 0);
                    searchLight.transform.localRotation = Quaternion.Euler(new Vector3(-20, 180, 0));
                    helilightsphere.transform.localScale += new Vector3(0.9f, 0, 0);
                    helilightsphere.LerpRadiusTo(0.1f, 10f);
                    helilightsphere.transform.localPosition = new Vector3(0, 0.24f, 2.35f);
                    helilightsphere.SendNetworkUpdateImmediate();
                }

                broadcastSpawn(player, "police heli");
                if (permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
                {
                    storedData.currentVehicle.Add(player.userID, policeheli.net.ID.Value);
                }
                else
                {
                    if (storedData.currentVehicleUnlimited.ContainsKey(player.userID))
                    {
                        storedData.currentVehicleUnlimited[player.userID].Add(policeheli.net.ID.Value);
                    }
                    else
                    {
                        List<ulong> vehicles = new List<ulong>();
                        vehicles.Add(policeheli.net.ID.Value);
                        storedData.currentVehicleUnlimited.Add(player.userID, vehicles);
                    }

                }
            }
        }

        [ChatCommand("policehelilarge")]
        void policehelilarge(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                SendReply(player, "You do not have permission to use that command");
                return;
            }
            if (storedData.currentVehicle.ContainsKey(player.userID))
            {
                SendReply(player, "You can only use one vehicle at a time, use /removevehicle to remove your current vehicle");
                return;
            }
            RaycastHit hit;
            if (UnityEngine.Physics.Raycast(player.eyes.HeadRay(), out hit, Mathf.Infinity, GlobalLayerMask))
            {
                Vector3 position = hit.point + Vector3.up * 2f;
                BaseVehicle policehelilarge = (BaseVehicle)GameManager.server.CreateEntity(scrapheliPrefab, position);
                if (policehelilarge == null) return;
                policehelilarge.OwnerID = player.userID;
                policehelilarge.Spawn();
                policehelilarge.GetFuelSystem().AddFuel(configData.policeHeliLargeFuel);
                addLock(policehelilarge, player);

                if (configData.policeHeliLargeFuelLock) lockFuel(policehelilarge);

                spawnentity(policehelilarge, bluelightprefab, new Vector3(0f, 3.24f, -7.65f), new Quaternion());
                spawnentity(policehelilarge, bluelightprefab, new Vector3(-0.2f, 3.24f, -7.65f), new Quaternion());
                spawnentity(policehelilarge, bluelightprefab, new Vector3(0.5f, 2.85f, 2.4f), new Quaternion());
                spawnentity(policehelilarge, bluelightprefab, new Vector3(-0.5f, 2.85f, 2.4f), new Quaternion());
                spawnentity(policehelilarge, bluelightprefab, new Vector3(-1.2f, 0.8f, -3.06f), new Quaternion());
                spawnentity(policehelilarge, bluelightprefab, new Vector3(1.18f, 0.8f, -3.06f), new Quaternion());
                spawnentity(policehelilarge, bluelightprefab, new Vector3(0f, 0.6f, 3.65f), new Quaternion(90f, 0f, 0f, 0f));
                spawnentity(policehelilarge, bluelightprefab, new Vector3(0f, 0.6f, -3f), new Quaternion(90f, 0f, 0f, 0f));
                spawnentity(policehelilarge, bluelightprefab, new Vector3(0f, 1f, 4.47f), new Quaternion(0f, 90f, 90f, 0f));
                spawnentity(policehelilarge, buttonPrefab, new Vector3(0.005f, 2.2f, 2.8f), new Quaternion(1f, 0f, 0f, 0f));
                spawnentity(policehelilarge, phoneprefab, new Vector3(0f, 0.8f, 2f), new Quaternion(0f, 90f, 0f, 0f));
                spawnaudio(policehelilarge, boombox, new Vector3(-0.4f, 1.7f, 3.4f), new Quaternion(0f, 90f, 0f, 0f));
                broadcastSpawn(player, "large police heli");
                foreach (var children in policehelilarge.children)
                {
                    if (children.name == phoneprefab)
                    {
                        children.SetFlag(BaseEntity.Flags.Reserved8, true);
                    }
                }
                if (permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
                {
                    storedData.currentVehicle.Add(player.userID, policehelilarge.net.ID.Value);
                }
                else
                {
                    if (storedData.currentVehicleUnlimited.ContainsKey(player.userID))
                    {
                        storedData.currentVehicleUnlimited[player.userID].Add(policehelilarge.net.ID.Value);
                    }
                    else
                    {
                        List<ulong> vehicles = new List<ulong>();
                        vehicles.Add(policehelilarge.net.ID.Value);
                        storedData.currentVehicleUnlimited.Add(player.userID, vehicles);
                    }

                }
            }
        }

        [ChatCommand("policeheliattack")]
        void policeheliattack(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                SendReply(player, "You do not have permission to use that command");
                return;
            }
            if (storedData.currentVehicle.ContainsKey(player.userID))
            {
                SendReply(player, "You can only use one vehicle at a time, use /removevehicle to remove your current vehicle");
                return;
            }
            RaycastHit hit;
            if (UnityEngine.Physics.Raycast(player.eyes.HeadRay(), out hit, Mathf.Infinity, GlobalLayerMask))
            {
                Vector3 position = hit.point + Vector3.up * 2f;
                BaseVehicle policeheli = (BaseVehicle)GameManager.server.CreateEntity(attackHeliPrefab, position);
                if (policeheli == null) return;
                policeheli.OwnerID = player.userID;
                policeheli.Spawn();
                policeheli.GetFuelSystem().AddFuel(configData.policeHeliAttackFuel);
                addLock(policeheli, player);

                if (configData.policeHeliFuelLock) lockFuel(policeheli);

                spawnentity(policeheli, bluelightprefab, new Vector3(0, 2.44f, 0.5f), new Quaternion());// top light front
                spawnentity(policeheli, bluelightprefab, new Vector3(0, 2.44f, -0.26f), new Quaternion());// top light back
                spawnentity(policeheli, bluelightprefab, new Vector3(0f, 1.94f, -6.5f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));//rear light
                spawnentity(policeheli, bluelightprefab, new Vector3(0f, 0.9f, 1.6f), new Quaternion(0f, 90f, 90f, 0f));// front light
                spawnentity(policeheli, bluelightprefab, new Vector3(0f, 0.7f, -0.75f), new Quaternion(90f, 0f, 0f, 0f));//bottom front
                spawnentity(policeheli, bluelightprefab, new Vector3(0f, 0.7f, 1.1f), new Quaternion(90f, 0f, 0f, 0f));// bottom rear

                spawnentity(policeheli, buttonPrefab, new Vector3(0, 1f, -0.8f), new Quaternion(0f, -0.707f, -0.707f, 0f));
                spawnaudio(policeheli, boombox, new Vector3(-0.4f, 1f, 0.4f), new Quaternion(0f, 90f, 0f, 0f));

                broadcastSpawn(player, "police heli attack");
                if (permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
                {
                    storedData.currentVehicle.Add(player.userID, policeheli.net.ID.Value);
                }
                else
                {
                    if (storedData.currentVehicleUnlimited.ContainsKey(player.userID))
                    {
                        storedData.currentVehicleUnlimited[player.userID].Add(policeheli.net.ID.Value);
                    }
                    else
                    {
                        List<ulong> vehicles = new List<ulong>();
                        vehicles.Add(policeheli.net.ID.Value);
                        storedData.currentVehicleUnlimited.Add(player.userID, vehicles);
                    }

                }
            }
        }

        [ChatCommand("policeboat")]
        void policeboat(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                SendReply(player, "You do not have permission to use that command");
                return;
            }
            if (storedData.currentVehicle.ContainsKey(player.userID))
            {
                SendReply(player, "You can only use one vehicle at a time, use /removevehicle to remove your current vehicle");
                return;
            }
            RaycastHit hit;
            if (UnityEngine.Physics.Raycast(player.eyes.HeadRay(), out hit, Mathf.Infinity, GlobalLayerMask))
            {
                Vector3 position = hit.point + Vector3.up * 2f;
                var policeboat = GameManager.server?.CreateEntity(rhibprefab, position) as RHIB;
                if (policeboat == null) return;
                policeboat.Spawn();
                policeboat.AddFuel(configData.policeBoatFuel);
                addLock(policeboat, player);

                if (configData.policeBoatFuelLock) lockFuel(policeboat);

                spawnentity(policeboat, bluelightprefab, new Vector3(-0.1f, 3.345f, 0.66f), new Quaternion());
                spawnentity(policeboat, bluelightprefab, new Vector3(0f, 1.40f, -3.745f), new Quaternion());
                spawnentity(policeboat, buttonPrefab, new Vector3(0.5f, 2.45f, 0.35f), new Quaternion(1f, 0f, 0f, 0f));
                spawnentity(policeboat, bluelightprefab, new Vector3(0f, 1.77f, 4.15f), new Quaternion(0f, 90f, 90f, 0f));
                spawnentity(policeboat, bluelightprefab, new Vector3(0f, 1.80f, 4.15f), new Quaternion());
                spawnentity(policeboat, bluelightprefab, new Vector3(-1.2f, 1.31f, 0.66f), new Quaternion());
                spawnentity(policeboat, bluelightprefab, new Vector3(1.2f, 1.31f, 0.66f), new Quaternion());
                spawnentity(policeboat, bluelightprefab, new Vector3(-1.2f, 1.31f, -3.745f), new Quaternion());
                spawnentity(policeboat, bluelightprefab, new Vector3(1.2f, 1.31f, -3.745f), new Quaternion());
                spawnaudio(policeboat, boombox, new Vector3(-0.4f, 2f, 0.4f), new Quaternion(0f, 90f, 0f, 0f));

                if (configData.boatSpotLight == true) {
                    SphereEntity boatlightsphere = (SphereEntity)GameManager.server.CreateEntity(SpherePrefab, policeboat.transform.position, new Quaternion(0, 0, 0, 0), true);
                    if (boatlightsphere == null) return;
                    RemoveColliderProtection(boatlightsphere);
                    boatlightsphere.Spawn();
                    boatlightsphere.SetParent(policeboat);
                    boatlightsphere.transform.localPosition = new Vector3(0, 1.76f, 4.2f);
                    BaseEntity searchlight = GameManager.server.CreateEntity(SearchLightPrefab, boatlightsphere.transform.position) as BaseEntity;
                    if (searchlight == null) return;
                    RemoveColliderProtection(searchlight);
                    searchlight.Spawn();
                    searchlight.SetFlag(BaseEntity.Flags.On, true);
                    searchlight.SetParent(boatlightsphere);
                    searchlight.transform.localPosition = new Vector3(0, 0, 0);
                    boatlightsphere.LerpRadiusTo(0.1f, 10f);
                    boatlightsphere.transform.rotation = new Quaternion(0f, 90f, 0f, 0f);
                    boatlightsphere.SendNetworkUpdateImmediate();
                }

                broadcastSpawn(player, "police boat");
                if (permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
                {
                    storedData.currentVehicle.Add(player.userID, policeboat.net.ID.Value);
                }
                else
                {
                    if (storedData.currentVehicleUnlimited.ContainsKey(player.userID))
                    {
                        storedData.currentVehicleUnlimited[player.userID].Add(policeboat.net.ID.Value);
                    }
                    else
                    {
                        List<ulong> vehicles = new List<ulong>();
                        vehicles.Add(policeboat.net.ID.Value);
                        storedData.currentVehicleUnlimited.Add(player.userID, vehicles);
                    }

                }
            }
        }

        [ChatCommand("policetugboat")]
        void policetugboat(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                SendReply(player, "You do not have permission to use that command");
                return;
            }
            if (storedData.currentVehicle.ContainsKey(player.userID))
            {
                SendReply(player, "You can only use one vehicle at a time, use /removevehicle to remove your current vehicle");
                return;
            }
            RaycastHit hit;
            if (UnityEngine.Physics.Raycast(player.eyes.HeadRay(), out hit, Mathf.Infinity, GlobalLayerMask))
            {
                Vector3 position = hit.point + Vector3.up * 2f;
                var policeboat = GameManager.server?.CreateEntity(tugboatPrefab, position) as Tugboat;
                if (policeboat == null) return;
                policeboat.Spawn();
                policeboat.fuelSystem.AddFuel(configData.policeTugboatFuel);

                var availableSockets = PrefabAttribute.server.FindAll<Socket_Base>(policeboat.prefabID);
                foreach(var socket in availableSockets)
                { 
                    if(socket.socketName == "Tugboat/Shared/Sockets/doorway-female (1)/1" || socket.socketName == "Tugboat/Shared/Sockets/doorway-female/1" || socket.socketName == "Tugboat/Shared/Sockets/doorway-female (2)/1")
                    {
                        BaseEntity newEnt = GameManager.server.CreateEntity(metalDoorPrefab, Vector3.zero, Quaternion.identity, false);
                        if (newEnt == null)
                        {
                            Server.Broadcast("deployable null");
                            return;
                        }
                        newEnt.SetParent(policeboat, socket.socketName);
                        newEnt.transform.position = newEnt.transform.position + socket.worldPosition + new Vector3(0,-1,0);
                        if(socket.socketName == "Tugboat/Shared/Sockets/doorway-female (2)/1")
                        {
                            newEnt.transform.rotation = new Quaternion(0,90,0,90);
                        }

                        var deployable = Deployable.server.Find<Deployable>(newEnt.prefabID);
                        Effect.server.Run(deployable.placeEffect.resourcePath, newEnt.transform.TransformPoint(socket.worldPosition), newEnt.transform.up);
                        DecayEntity decayEntity = newEnt as DecayEntity;
                        if (decayEntity != null)
                        {
                            decayEntity.AttachToBuilding(policeboat.prefabID);
                        }
                        newEnt.OwnerID = policeboat.OwnerID;
                        newEnt.Spawn();
                        var code = GameManager.server.CreateEntity("assets/prefabs/locks/keypad/lock.code.prefab") as CodeLock;
                        if (code != null)
                        {
                            code.gameObject.Identity();
                            code.SetParent(newEnt, newEnt.GetSlotAnchorName(BaseEntity.Slot.Lock));
                            code.Spawn();
                            newEnt.SetSlot(BaseEntity.Slot.Lock, code);
                            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock-code-deploy.prefab",
                                code.transform.position);
                            code.whitelistPlayers.Add(player.userID);
                            code.SetFlag(BaseEntity.Flags.Locked, true);
                        }
                    }
                }

                addLock(policeboat, player);

                if (configData.policeBoatFuelLock) lockFuel(policeboat);


                spawnentity(policeboat, buttonPrefab, new Vector3(1.1f, 8.4f, 4.3f), new Quaternion(1f, 0f, 0f, 0f));

                spawnentity(policeboat, bluelightprefab, new Vector3(1.6f, 8.6f, 4f), new Quaternion());// top front left
                spawnentity(policeboat, bluelightprefab, new Vector3(-1.6f, 8.6f, 4f), new Quaternion());// top front 
                spawnentity(policeboat, bluelightprefab, new Vector3(1.9f, 8.6f, 0.5f), new Quaternion());// top rear left
                spawnentity(policeboat, bluelightprefab, new Vector3(-1.9f, 8.6f, 0.5f), new Quaternion());// top rear right
                spawnentity(policeboat, bluelightprefab, new Vector3(0f, 3.3f, 11.6f), new Quaternion(0f, 90f, 90f, 0f));//front faciing forward
                spawnentity(policeboat, bluelightprefab, new Vector3(0f, 3.4f, 11.6f), new Quaternion());//front facing up
                spawnentity(policeboat, bluelightprefab, new Vector3(-3.55f, 3f, 5.25f), new Quaternion()); //front side pillar right
                spawnentity(policeboat, bluelightprefab, new Vector3(3.55f, 3f, 5.25f), new Quaternion()); // front side pillar left
                spawnentity(policeboat, bluelightprefab, new Vector3(-4.2f, 2.55f, -0.8f), new Quaternion()); //middle side pillar right
                spawnentity(policeboat, bluelightprefab, new Vector3(4.2f, 2.55f, -0.8f), new Quaternion()); // middle side pillar left
                spawnentity(policeboat, bluelightprefab, new Vector3(-4.2f, 2.55f, -5.7f), new Quaternion()); //rear side pillar right
                spawnentity(policeboat, bluelightprefab, new Vector3(4.2f, 2.55f, -5.7f), new Quaternion()); // rear side pillar left
                spawnentity(policeboat, bluelightprefab, new Vector3(-1.5f, 2.85f, -10.5f), new Quaternion());// rear right
                spawnentity(policeboat, bluelightprefab, new Vector3(1.5f, 2.85f, -10.5f), new Quaternion());// rear left
                spawnentity(policeboat, bluelightprefab, new Vector3(0f, 2.85f, -10.76f), new Quaternion());// rear middle

                spawnentity(policeboat, computerStationPrefab, new Vector3(2.1f, 2f, -1.8f), new Quaternion(0f, -0.707f, 0f, 0.707f));// computer station
                spawnentity(policeboat, phoneprefab, new Vector3(2.1f, 2.75f, -2.3f), new Quaternion(0f, -0.707f, 0f, 0.707f));// phone on computer station
                spawnentity(policeboat, phoneprefab, new Vector3(-1f, 6.8f, 4.3f), new Quaternion(0f, 90f, 0f, 0f));// phone on bridgeW 
                spawnentity(policeboat, cellWallPrefab, new Vector3(1.35f, 1.6f, 2.5f), new Quaternion(0,0.707f, 0, -0.707f)) ;//prison wall
                BaseEntity prisonGate = GameManager.server.CreateEntity(cellGatePrefab, policeboat.transform.position);
                if (prisonGate == null) return;
                prisonGate.transform.localPosition = position;
                UnityEngine.Object.DestroyImmediate(prisonGate.GetComponent<DestroyOnGroundMissing>());
                UnityEngine.Object.DestroyImmediate(prisonGate.GetComponent<GroundWatch>());
                prisonGate.SetParent(policeboat);
                prisonGate.transform.localPosition = new Vector3(-1.35f, 1.6f, 2.5f);
                prisonGate.transform.localRotation = new Quaternion(0, 0.707f, 0, -0.707f);
                prisonGate.Spawn();
                policeboat.AddChild(prisonGate);
                prisonGate.SendNetworkUpdateImmediate();
                policeboat.SendNetworkUpdateImmediate();

                var prisonGateLock = GameManager.server.CreateEntity("assets/prefabs/locks/keypad/lock.code.prefab") as CodeLock;
                if (prisonGateLock != null)
                {
                    prisonGateLock.gameObject.Identity();
                    prisonGateLock.SetParent(prisonGate, prisonGate.GetSlotAnchorName(BaseEntity.Slot.Lock));
                    prisonGateLock.Spawn();
                    prisonGate.SetSlot(BaseEntity.Slot.Lock, prisonGateLock);
                    Effect.server.Run("assets/prefabs/locks/keypad/effects/lock-code-deploy.prefab",
                        prisonGateLock.transform.position);
                    prisonGateLock.whitelistPlayers.Add(player.userID);
                    prisonGateLock.SetFlag(BaseEntity.Flags.Locked, true);
                }


                foreach (var children in policeboat.children)
                {
                    if (children.name == phoneprefab)
                    {
                        children.SetFlag(BaseEntity.Flags.Reserved8, true);
                    }
                }
                broadcastSpawn(player, "police boat");
                if (permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
                {
                    storedData.currentVehicle.Add(player.userID, policeboat.net.ID.Value);
                }
                else
                {
                    if (storedData.currentVehicleUnlimited.ContainsKey(player.userID))
                    {
                        storedData.currentVehicleUnlimited[player.userID].Add(policeboat.net.ID.Value);
                    }
                    else
                    {
                        List<ulong> vehicles = new List<ulong>();
                        vehicles.Add(policeboat.net.ID.Value);
                        storedData.currentVehicleUnlimited.Add(player.userID, vehicles);
                    }

                }
            }
        }

        [ChatCommand("policetrain")]
        void policetrain(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                SendReply(player, "You do not have permission to use that command");
                return;
            }
            if (storedData.currentVehicle.ContainsKey(player.userID))
            {
                SendReply(player, "You can only use one vehicle at a time, use /removevehicle to remove your current vehicle");
                return;
            }
            RaycastHit hit;
            if (UnityEngine.Physics.Raycast(player.eyes.HeadRay(), out hit, Mathf.Infinity, GlobalLayerMask))
            {
                Vector3 position = hit.point + Vector3.up * 2f;
                var policeTrain = GameManager.server?.CreateEntity(trainPrefab, position) as TrainCar;
                if (policeTrain == null) return;
                policeTrain.Spawn();
                SendReply(player, "If train has not spawned there may be too many trains nearby");

                try
                {
                    policeTrain.FrontTrackSection.GetData().ToString();
                }
                catch (Exception ex)
                {
                    SendReply(player, "You are not close enough to tracks or not looking at tracks");
                }
                
                policeTrain.GetFuelSystem().AddFuel(configData.policeTrainFuel);
                addLock(policeTrain, player);

                if (configData.policeTrainFuelLock) lockFuel(policeTrain);

                spawnentity(policeTrain, buttonPrefab, new Vector3(0.6f, 3f, 3.95f), new Quaternion(0f, -0.707f, -0.707f, 0f));

                spawnentity(policeTrain, bluelightprefab, new Vector3(1.1f, 0.65f, -9.4f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
                spawnentity(policeTrain, bluelightprefab, new Vector3(-1.1f, 0.65f, -9.4f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
                spawnentity(policeTrain, bluelightprefab, new Vector3(1.1f, 0.6f, 9f), new Quaternion(0f, 90f, 90f, 0f));
                spawnentity(policeTrain, bluelightprefab, new Vector3(-1.1f, 0.6f, 9f), new Quaternion(0f, 90f, 90f, 0f));

                spawnentity(policeTrain, bluelightprefab, new Vector3(-0.5f, 4.6f, 5.65f), new Quaternion());
                spawnentity(policeTrain, bluelightprefab, new Vector3(-0.4f, 4.6f, 5.75f), new Quaternion());
                spawnentity(policeTrain, bluelightprefab, new Vector3(-0.3f, 4.6f, 5.85f), new Quaternion());

                spawnentity(policeTrain, bluelightprefab, new Vector3(0.5f, 4.6f, 5.65f), new Quaternion());
                spawnentity(policeTrain, bluelightprefab, new Vector3(0.4f, 4.6f, 5.75f), new Quaternion());
                spawnentity(policeTrain, bluelightprefab, new Vector3(0.3f, 4.6f, 5.85f), new Quaternion());

                spawnentity(policeTrain, bluelightprefab, new Vector3(-0.5f, 4.6f, -7.55f), new Quaternion());
                spawnentity(policeTrain, bluelightprefab, new Vector3(-0.4f, 4.6f, -7.65f), new Quaternion());
                spawnentity(policeTrain, bluelightprefab, new Vector3(-0.3f, 4.6f, -7.75f), new Quaternion());
                spawnentity(policeTrain, bluelightprefab, new Vector3(0.5f, 4.6f, -7.55f), new Quaternion());
                spawnentity(policeTrain, bluelightprefab, new Vector3(0.4f, 4.6f, -7.65f), new Quaternion());
                spawnentity(policeTrain, bluelightprefab, new Vector3(0.3f, 4.6f, -7.75f), new Quaternion());

                spawnentity(policeTrain, bluelightprefab, new Vector3(-1f, 0.6f, 9f), new Quaternion(0f, 90f, 90f, 0f));
                spawnentity(policeTrain, bluelightprefab, new Vector3(-0.9f, 0.6f, 9f), new Quaternion(0f, 90f, 90f, 0f));

                spawnentity(policeTrain, bluelightprefab, new Vector3(1f, 0.6f, 9f), new Quaternion(0f, 90f, 90f, 0f));
                spawnentity(policeTrain, bluelightprefab, new Vector3(0.9f, 0.6f, 9f), new Quaternion(0f, 90f, 90f, 0f));

                spawnentity(policeTrain, bluelightprefab, new Vector3(1.1f, 0.65f, -9.4f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
                spawnentity(policeTrain, bluelightprefab, new Vector3(1f, 0.65f, -9.4f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
                spawnentity(policeTrain, bluelightprefab, new Vector3(0.9f, 0.65f, -9.4f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
                
                spawnentity(policeTrain, bluelightprefab, new Vector3(-1f, 0.65f, -9.4f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
                spawnentity(policeTrain, bluelightprefab, new Vector3(-0.9f, 0.65f, -9.4f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));

                spawnentity(policeTrain, strobelight, new Vector3(-1f, 0.7f, 9f), new Quaternion());
                spawnentity(policeTrain, strobelight, new Vector3(1f, 0.7f, 9f), new Quaternion());
                spawnaudio(policeTrain, boombox, new Vector3(1.5f, 3.1f, 5.3f), new Quaternion(0f, 90f, 0f, 0f));
                broadcastSpawn(player, "police train");
                if (permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
                {
                    storedData.currentVehicle.Add(player.userID, policeTrain.net.ID.Value);
                }
                else
                {
                    if (storedData.currentVehicleUnlimited.ContainsKey(player.userID))
                    {
                        storedData.currentVehicleUnlimited[player.userID].Add(policeTrain.net.ID.Value);
                    }
                    else
                    {
                        List<ulong> vehicles = new List<ulong>();
                        vehicles.Add(policeTrain.net.ID.Value);
                        storedData.currentVehicleUnlimited.Add(player.userID, vehicles);
                    }

                }
            }
        }



            [ChatCommand("removevehicle")]
        void removevehicle(BasePlayer player)
        {
            if (!storedData.currentVehicle.ContainsKey(player.userID) && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                SendReply(player, "You do not currently have a vehicle");
                return;
            }
            if (permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                RaycastHit hit;
                BaseVehicle car = null;
                if (UnityEngine.Physics.Raycast(player.eyes.HeadRay(), out hit, 3))
                {
                    car = hit.GetEntity() as BaseVehicle;
                }

                if (car != null)
                {
                    KillVehicleCheck(player, car, car.net.ID.Value);
                }
                else
                {
                    SendReply(player, "You are not looking at a police vehicle");
                }
            }
            else
            {
                var foundentities = BaseVehicle.FindObjectsOfType<BaseVehicle>();
                var netID = storedData.currentVehicle[player.userID];
                foreach (var entity in foundentities)
                {
                    if (entity.net.ID.Value == netID)
                    {
                        KillVehicleCheck(player, entity, netID);  
                        return;
                    }
                }
            }
        }

        #endregion
        #region Calls
        void Init()
        {
            permission.RegisterPermission("PoliceVehicles.use", this);
            permission.RegisterPermission("PoliceVehicles.unlimited", this);
            if (!LoadConfigVariables())
            {
                Puts("Config file issue detected. Please delete file, or check syntax and fix.");
                return;
            }
        }
        private void OnServerInitialized()
        {
            if (SpawnModularCar == null)
            {
                Puts("SpawnModularCar is not loaded, get it at https://umod.org");
            }
        }
        void OnButtonPress(PressButton entity, BasePlayer player)
        {
            if (entity == null || player == null)
            {
                return;
            }
            var result = player.GetMounted();
            if (result == null)
            {
                return;
            }
            if (result.ShortPrefabName == "modularcardriverseat" || result.ShortPrefabName == "miniheliseat" || result.ShortPrefabName == "minihelipassenger" || result.ShortPrefabName == "transporthelipilot" || result.ShortPrefabName == "transporthelicopilot" || result.ShortPrefabName == "standingdriver" || result.ShortPrefabName == "smallboatpassenger" || result.ShortPrefabName == "snowmobiledriverseat" || result.ShortPrefabName == "locomotivedriver" || result.ShortPrefabName == "attackhelidriver" || result.ShortPrefabName == "tugboatdriver")
            {
                var mountedveh = player.GetMountedVehicle();
                if (storedData.LightsActivated.Contains(mountedveh.net.ID.Value))
                {
                    lightsOnOff(player, mountedveh, false);
                    storedData.LightsActivated.Remove(mountedveh.net.ID.Value);
                    return;
                }
                else
                {
                    lightsOnOff(player, mountedveh, true);
                    storedData.LightsActivated.Add(mountedveh.net.ID.Value);
                    return;
                }
            }
        }
        void OnEntityKill(BaseVehicle vehicle)
        {
            if (vehicle == null) return;
            //Puts("Yes Called");
            bool isInCurrentVehicle = false;
            foreach (var entity in storedData.currentVehicle.Values)
            {
                if (vehicle.net.ID.Value == entity)
                {
                    var myKey = storedData.currentVehicle.FirstOrDefault(x => x.Value == entity).Key;
                    storedData.LightsActivated.Remove(entity);
                    storedData.currentVehicle.Remove(myKey);
                    isInCurrentVehicle = true;
                    return;
                }
            }
            if (!isInCurrentVehicle)
            {
                foreach (var player in storedData.currentVehicleUnlimited.Keys)
                {
                    storedData.currentVehicleUnlimited[player].Remove(vehicle.net.ID.Value);
                    storedData.LightsActivated.Remove(vehicle.net.ID.Value);
                }
            }
        }
        #endregion
        #region Core

        void broadcastSpawn(BasePlayer player, string spawneditem)
        {
            if(player == null) return;
            if(configData.broadcastspawn == true)
            {
                Server.Broadcast("The police are on their way " + player.displayName + " has called in a " + spawneditem);
            }
            else
            {
                return;
            }
        }
        void spawnentity(BaseVehicle vehicle, string spawnentity, Vector3 position, Quaternion rotation)
        {
            BaseEntity entity = GameManager.server.CreateEntity(spawnentity, vehicle.transform.position);
            if (entity == null) return;
            entity.transform.localPosition = position;
            UnityEngine.Object.DestroyImmediate(entity.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(entity.GetComponent<GroundWatch>());
            entity.SetParent(vehicle);
            entity.transform.localPosition = position;
            entity.transform.localRotation = rotation;
            entity.Spawn();
            vehicle.AddChild(entity);
            entity.SendNetworkUpdateImmediate();
            vehicle.SendNetworkUpdateImmediate();
        }
        void spawnentitysocket(BaseVehicle vehicle, string spawnentity, Vector3 position, Quaternion rotation)
        {

            BaseEntity entity = GameManager.server.CreateEntity(spawnentity, vehicle.transform.position);
            if (entity == null) return;
            entity.transform.localPosition = position;
            UnityEngine.Object.DestroyImmediate(entity.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(entity.GetComponent<GroundWatch>());
            entity.SetParent(vehicle);
            entity.transform.localPosition = position;
            entity.transform.localRotation = rotation;
            entity.Spawn();
            vehicle.AddChild(entity);
            entity.SendNetworkUpdateImmediate();
            vehicle.SendNetworkUpdateImmediate();
        }

        void spawnaudio(BaseVehicle vehicle, string spawnentity, Vector3 position, Quaternion rotation)
        {
            SphereEntity boomBoxSphere = (SphereEntity)GameManager.server.CreateEntity(SpherePrefab, vehicle.transform.position, new Quaternion(0, 0, 0, 0), true);
            if (boomBoxSphere == null) return;
            RemoveColliderProtection(boomBoxSphere);
            boomBoxSphere.Spawn();
            boomBoxSphere.SetParent(vehicle);
            boomBoxSphere.transform.localPosition = new Vector3(0, 0, 0);
            Server.Command($"BoomBox.ServerUrlList Siren,{configData.radioStnUrl}");
            DeployableBoomBox boomBox = GameManager.server.CreateEntity(boombox, boomBoxSphere.transform.position) as DeployableBoomBox;
            if (boomBox == null) return;
            RemoveColliderProtection(boomBox);
            boomBox.Spawn();
            boomBox.SetParent(boomBoxSphere);
            boomBox.transform.localPosition = new Vector3(0, 0, 0);
            boomBox.UpdateHasPower(10, 3);
            boomBox.SetFlag(BaseEntity.Flags.Reserved8, true);
            boomBoxSphere.transform.localScale += new Vector3(0.9f, 0, 0);
            boomBoxSphere.LerpRadiusTo(0.2f, 100f);
            boomBoxSphere.transform.localPosition = position;
            boomBoxSphere.transform.localRotation = rotation;
            boomBoxSphere.SendNetworkUpdateImmediate();


        }

        void KillVehicleCheck(BasePlayer player, BaseVehicle entity, ulong netID)
        {
            storedData.LightsActivated.Remove(netID);
            if (storedData.currentVehicle.ContainsKey(player.userID))
            {
                storedData.currentVehicle.Remove(player.userID);
                KillVehicle(entity);
            }
            else if (storedData.currentVehicleUnlimited.ContainsKey(player.userID))
            {
                bool unlimitedvehicleremove = storedData.currentVehicleUnlimited[player.userID].Remove(netID);
                if (!unlimitedvehicleremove)
                {
                    SendReply(player, "This is not your vehicle");
                    return;
                }
                KillVehicle(entity);
            }
            else
            {
                SendReply(player, "This is not your vehicle");
                return;
            }
            SendReply(player, "Vehicle removed");
        }
        void KillVehicle(BaseVehicle entity)
        {
            Vector3 position = entity.transform.position;
            position.y = position.y - 50;
            entity.transform.position = position;
            entity.Kill(BaseVehicle.DestroyMode.None);
        }

        void lightsOnOff(BasePlayer player, BaseVehicle policecar, bool onOff)
        {
            int numlightson = 0;
            foreach (var children in policecar.children)
            {
                if (children.name == strobelight)
                {
                    children.SetFlag(BaseEntity.Flags.On, onOff);

                }
                else if (children.name == bluelightprefab)
                {
                    numlightson++;
                    if (numlightson >= 5)
                    {
                        children.SetFlag(BaseEntity.Flags.Reserved8, onOff);
                    }
                    else
                    {
                        timer.Once(0.8f, () =>
                        {
                            children.SetFlag(BaseEntity.Flags.Reserved8, onOff);
                        });
                    }

                }
                else if (children.name == SpherePrefab)
                {
                    var foundSearchLights = children.children.OfType<SearchLight>();
                    foreach (var entity in foundSearchLights)
                    {
                        if (onOff)
                        {
                            entity.UpdateHasPower(10, 1);
                            entity.SendNetworkUpdateImmediate();
                        }
                        else
                        {
                            entity.UpdateHasPower(0, 1);
                            entity.SendNetworkUpdateImmediate();
                        }
                    }
                }
            }
        }
        void RemoveColliderProtection(BaseEntity collider)
        {
            foreach (var meshCollider in collider.GetComponentsInChildren<MeshCollider>())
            {
                UnityEngine.Object.DestroyImmediate(meshCollider);
            }
            UnityEngine.Object.DestroyImmediate(collider.GetComponent<GroundWatch>());
        }
        #endregion
        #region API
           
        void addLock(BaseVehicle vehicle, BasePlayer player)
        {
            try
            {
                VehicleDeployedLocks.Call("API_DeployCodeLock", vehicle, player);
            }
            catch (Exception ex)
            {
                Puts("Locks plugin not installed, spawning without");
            }
        }
        ModularCar SpawnPoliceCar(BasePlayer player)
        {
            var car = SpawnModularCar.Call("API_SpawnPresetCar", player,
                new Dictionary<string, object>
                {
                    ["CodeLock"] = true,
                    ["KeyLock"] = false,
                    ["EnginePartsTier"] = configData.carTier,
                    ["FuelAmount"] = configData.policeCarFuel,
                    ["Modules"] = new object[] {
                        "vehicle.1mod.engine",
                        "vehicle.1mod.cockpit.with.engine",
                        "vehicle.1mod.rear.seats"
                    },
                }, new Action<ModularCar>(readyCar => OnCarReady(readyCar, "car"))
            ) as ModularCar;
            return car;
        }
        ModularCar SpawnPoliceTransport(BasePlayer player)
        {
            var car = SpawnModularCar.Call("API_SpawnPresetCar", player,
                new Dictionary<string, object>
                {
                    ["CodeLock"] = true,
                    ["KeyLock"] = false,
                    ["EnginePartsTier"] = configData.transportTier,
                    ["FuelAmount"] = configData.policeTransportFuel,
                    ["Modules"] = new object[] {
                        "vehicle.1mod.engine",
                        "vehicle.1mod.cockpit.with.engine",
                        "vehicle.1mod.passengers.armored",
                        "vehicle.1mod.passengers.armored"
                    },
                }, new Action<ModularCar>(readyCar => OnCarReady(readyCar, "transport"))
            ) as ModularCar;
            return car;
        }

        private void OnCarReady(ModularCar car, string type)
        {
            if (configData.lockCar == true && type == "car")
            {
                foreach (var module in car.AttachedModuleEntities)
                {
                    var engineContainer = (module as VehicleModuleEngine)?.GetContainer();
                    if (engineContainer != null)
                        engineContainer.inventory.SetLocked(true);
                }
            }
            else if (configData.lockTransport == true && type == "transport")
            {
                foreach (var module in car.AttachedModuleEntities)
                {
                    var engineContainer = (module as VehicleModuleEngine)?.GetContainer();
                    if (engineContainer != null)
                        engineContainer.inventory.SetLocked(true);
                }
            }
        }

        private void lockFuel(BaseVehicle vehicle)
        {
            var fuelSystems = vehicle.GetComponents<EntityFuelSystem>() as EntityFuelSystem[];

            foreach(var entity in fuelSystems)
            {
                if (entity == null) continue;
                entity.GetFuelContainer().inventory.SetLocked(true);
            }
        }
        #endregion
    }
}
 