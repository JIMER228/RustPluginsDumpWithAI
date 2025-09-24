using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Network;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using UnityEngine;
using Net = Network.Net;

namespace Oxide.Plugins
{
    [Info("BrightNight", "Kira", "1.0.2")]
    [Description("Clear night for Rust")]
    public class BrightNight : RustPlugin
    {
        #region [Vars]

        [PluginReference] private Plugin ModifiedClothing, AlertSystem;
        private TOD_Sky Sky;
        private static BrightNight _;
        private NightCore Core;
        private static WaitForSeconds wait = new WaitForSeconds(0.05f);

        #endregion

        #region [Configuraton] / [Конфигурация]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "BrightNight - Config")]
            public NightCFG BrightNight = new NightCFG();

            public class NightCFG
            {
                [JsonProperty(PropertyName = "[Белая ночь] Контраст (0.0-1.0)")]
                public string Contrast;

                [JsonProperty(PropertyName = "[Белая ночь] Яркость (0.0-1.0)")]
                public string Bright;

                [JsonProperty(PropertyName = "ID оповещения")]
                public int AlertID;
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                BrightNight = new ConfigData.NightCFG
                {
                    Contrast = "0.4",
                    Bright = "1"
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Файл конфигурации поврежден (или не существует), создан новый!");
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region [Core]

        public class NightCore : MonoBehaviour
        {
            private int NightCount;
            private bool Switch;
            public bool IsClear;
            public bool ObjIsSpawned;

            private void Awake()
            {
                InvokeRepeating(nameof(Timer), 1f, 1f);
            }

            public void Timer()
            {
                if (_.Sky.IsNight)
                {
                    if (!Switch)
                    { 
                        NightCount++;
                        Switch = true;
                    }

                    if (NightCount == 1)
                    {
                        _.nh(1f, 0.4f);
                        IsClear = true;  
                        if (!ObjIsSpawned)
                        {
                            _.ModifiedClothing.Call("IsClearNight", true);
                            _.AlertSystem.Call("AlertAll", _._config.BrightNight.AlertID);
                            ServerMgr.Instance.StartCoroutine(SpawnEffects());
                            ObjIsSpawned = true;
                        }
                    }
                }

                if (!_.Sky.IsDay) return;
                if (NightCount == 1) NightCount = 0;
                if (ObjIsSpawned)
                {
                    ServerMgr.Instance.StartCoroutine(DeSpawnEffects());
                    ObjIsSpawned = false;
                }

                _.nh(1f, 1f);
                Switch = false;
                IsClear = false;
                _.ModifiedClothing.Call("IsClearNight", false);
            }

            private static IEnumerator SpawnEffects()
            {
                _.PrintToChat("Начата белая ночь");
                var list = FindObjectsOfType<OreResourceEntity>();
                foreach (var obj in list)
                {
                    if (obj.ShortPrefabName.Contains("sulfur-ore") || obj.ShortPrefabName.Contains("metal-ore"))
                    {
                        var prefab = GameManager.server.CreateEntity(
                            "assets/prefabs/deployable/playerioents/lights/sirenlight/electric.sirenlight.deployed.prefab");
                        prefab.transform.position = new Vector3(0, -0.5f, 0);
                        prefab.SetFlag(BaseEntity.Flags.Reserved8, true);
                        prefab.SetParent(obj);
                        prefab.Spawn();
                        prefab.SendNetworkUpdate();
                    }

                    yield return wait;
                }

                yield return 0;
            }

            private static IEnumerator DeSpawnEffects()
            {
                var list = FindObjectsOfType<SirenLight>();
                foreach (var obj in list)
                {
                    if (obj.GetParentEntity() is OreResourceEntity) obj.Kill();
                    yield return wait;
                }

                yield return 0;
            }

            private void OnDestroy()
            {
                _.nh(1f, 1f);
                StartCoroutine(DeSpawnEffects());
            }
        }

        #endregion

        #region [Hooks]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            _ = this;
            Sky = TOD_Sky.Instance;
            Core = ServerMgr.Instance.gameObject.AddComponent<NightCore>();
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            UnityEngine.Object.Destroy(Core);
        }

        // ReSharper disable once UnusedMember.Local

        #endregion

        #region [Helpers]

        private void nh(float bright, float contrast)
        {
            Sky.Atmosphere.Brightness = 1;
            Sky.Atmosphere.Contrast = 1.5f;
            Sky.Clouds.Coverage = 0;
            Sky.Clouds.Size = 0;
            Sky.UpdateAmbient();
            Sky.UpdateReflection();
            var ds = new Dictionary<string, string>
            {
                ["weather.atmosphere_brightness"] = $"{bright}",
                ["weather.atmosphere_contrast"] = $"{contrast}",
                ["weather.atmosphere_directionality"] = "1",
                ["weather.atmosphere_mie"] = "-1",
                ["weather.atmosphere_rayleigh"] = "1",
                ["weather.fog"] = "0",
                ["weather.clear_chance"] = "2",
                ["weather.dust_chance"] = "0",
                ["weather.overcast_chance"] = "0",
                ["weather.wind"] = "0",
                ["weather.cloud_attenuation"] = "0.1",
                ["weather.cloud_brightness"] = "1",
                ["weather.cloud_coloring"] = "0.25",
                ["weather.cloud_opacity"] = "1",
                ["weather.cloud_saturation"] = "0",
                ["weather.cloud_scattering"] = "1",
                ["weather.cloud_sharpness"] = "1",
                ["weather.cloud_size"] = "1"
            };
            UpdatePlayerWeather(ref ds);
            Sky.Moon.MeshContrast = 1f;
            Sky.Moon.MeshBrightness = 1000;
            Sky.Moon.HaloBrightness = 1000f;
            Sky.UpdateReflection();
        }

        private static void UpdatePlayerWeather(ref Dictionary<string, string> weatherVars)
        {
            if (!Net.sv.write.Start()) return;
            var list = Facepunch.Pool.GetList<Connection>();
            list.AddRange(Net.sv.connections.Where(connection => connection.connected));

            var list2 = Facepunch.Pool.GetList<KeyValuePair<string, string>>();
            list2.AddRange(weatherVars.ToList());
            Net.sv.write.PacketID(Message.Type.ConsoleReplicatedVars);
            Net.sv.write.Int32(list2.Count);
            foreach (var item in list2)
            {
                Net.sv.write.String(item.Key);
                Net.sv.write.String(item.Value);
            }

            Net.sv.write.Send(new SendInfo(list));
            Facepunch.Pool.FreeList(ref list2);
            Facepunch.Pool.FreeList(ref list);
        }

        #endregion
    }
}