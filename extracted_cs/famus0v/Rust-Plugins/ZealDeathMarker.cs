using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealDeathMarker", "Kira", "1.1.1")]
    [Description("Add marker on player Death")]
    public class ZealDeathMarker : RustPlugin
    {
        #region [Vars] / [Переменные]

        private const string GenericPrefab = "assets/prefabs/tools/map/genericradiusmarker.prefab";
        private const string VENDINGPREFAB = "assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab";

        #endregion

        #region [Configuration] / [Конфигурация]

        private static ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "ZealDeathMarker [Configuration]")]
            public DeathMarkerConfig DeathMarkerCfg = new DeathMarkerConfig();

            public class DeathMarkerConfig
            {
                [JsonProperty(PropertyName = "Time to remove marker (Seconds)")]
                public int Duration;

                [JsonProperty(PropertyName = "Marker color (HEX)")]
                public string Color;

                [JsonProperty(PropertyName = "Marker radius (default = 1)")]
                public float Radius;

                [JsonProperty(PropertyName = "Marker transparency (0.0f - 1.0f)")]
                public float Transparency;
            }
        }

        private static ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                DeathMarkerCfg = new ConfigData.DeathMarkerConfig
                {
                    Duration = 60,
                    Color = "#FFFFFF",
                    Radius = 1f,
                    Transparency = 1f
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
            PrintError("The config file is corrupted (or does not exist), a new one was created!");
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region [MonoBehaviours]

        public class DeathMarker : MonoBehaviour
        {
            private MapMarkerGenericRadius MARKER;
            private VendingMachineMapMarker VENDINGMARKER;
            public Vector3 position;
            public float step;

            private void Awake()
            {
                step = _config.DeathMarkerCfg.Radius / _config.DeathMarkerCfg.Duration;
                Invoke(nameof(CreateMarker), 0.1f);
            }

            private void CreateMarker()
            {
                MARKER = GameManager.server.CreateEntity(GenericPrefab, position)
                    .GetComponent<MapMarkerGenericRadius>();
                ColorUtility.TryParseHtmlString(_config.DeathMarkerCfg.Color, out MARKER.color1);
                MARKER.color2 = Color.black;
                MARKER.radius = _config.DeathMarkerCfg.Radius;
                MARKER.alpha = _config.DeathMarkerCfg.Transparency;
                MARKER.enableSaving = false;
                MARKER.Spawn();
                InvokeRepeating(nameof(UpdateMarker), 1f, 1f);
                CreateVendingMarker();
            }

            private void CreateVendingMarker()
            {
                VENDINGMARKER = GameManager.server.CreateEntity(VENDINGPREFAB, position)
                    .GetComponent<VendingMachineMapMarker>();
                VENDINGMARKER.markerShopName = "A PLAYER DIED HERE";
                VENDINGMARKER.enableSaving = false;
                VENDINGMARKER.Spawn();
            }

            private void UpdateMarker()
            {
                if (MARKER.radius <= 0) Delete();
                MARKER.radius -= step;
                MARKER.SendUpdate();
            }

            private void Delete()
            {
                MARKER.Kill();
                VENDINGMARKER.Kill();
                Destroy(this);
            }
        }
 
        #endregion

        #region [Hooks] / [Крюки]

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (IsNpc(player)) return null;
            if (info == null) return null;
            if (info.damageTypes.Has(DamageType.Suicide)) return null;
            var component = new GameObject().AddComponent<DeathMarker>();
            component.position = player.ServerPosition;
            return null;
        }


        public class ASD
        {
            public Dictionary<ulong, User> Users = new Dictionary<ulong, User>();

            public class User
            {
                public string name;
                public string pass;
            }
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private static bool IsNpc(BasePlayer player)
        {
            if (player.IsNpc) return true;
            if (!player.userID.IsSteamId()) return true;
            if (player is NPCPlayer) return true;
            return false;
        }

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');
            if (str.Length == 6)
                str += "FF";
            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        #endregion
    }
}