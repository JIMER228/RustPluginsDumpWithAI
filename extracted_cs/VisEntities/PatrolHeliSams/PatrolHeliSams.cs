/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Facepunch;
using Newtonsoft.Json;
using Rust;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SamSite;

namespace Oxide.Plugins
{
    [Info("Patrol Heli Sams", "VisEntities", "1.0.0")]
    [Description("Turns sam sites into anti-heli defenses.")]
    public class PatrolHeliSams : RustPlugin
    {
        #region Fields

        private static PatrolHeliSams _plugin;
        private static Configuration _config;
        private static readonly HashSet<TargetableHeliComponent> _targetableHelis = new HashSet<TargetableHeliComponent>();

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Allow Monument Sam Sites To Engage")]
            public bool AllowMonumentSamSitesToEngage { get; set; }

            [JsonProperty("Targeting Range")]
            public float TargetingRange { get; set; }

            [JsonProperty("Rocket Flight Speed Multiplier")]
            public float RocketFlightSpeedMultiplier { get; set; }

            [JsonProperty("Rocket Damage Multiplier")]
            public float RocketDamageMultiplier { get; set; }

            [JsonProperty("Time Between Bursts Seconds")]
            public float TimeBetweenBurstsSeconds { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                AllowMonumentSamSitesToEngage = false,
                TargetingRange = 150f,
                RocketFlightSpeedMultiplier = 1.35f,
                RocketDamageMultiplier = 4f,
                TimeBetweenBurstsSeconds = 5f,
            };
        }

        #endregion Configuration

        #region Stored Data

        public class StoredData
        {
            [JsonProperty("Dummy")]
            public bool Dummy { get; set; }
        }

        #endregion Stored Data

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
        }

        private void Unload()
        {
            foreach (TargetableHeliComponent targetableHeli in _targetableHelis)
            {
                if (targetableHeli != null)
                    targetableHeli.DestroySelf();
            }

            _targetableHelis.Clear();
            _config = null;
            _plugin = null;
        }

        private void OnServerInitialized(bool isStartup)
        {
            foreach (PatrolHelicopter patrolHelicopter in BaseNetworkable.serverEntities.OfType<PatrolHelicopter>())
            {
                if (patrolHelicopter != null)
                    TargetableHeliComponent.Install(patrolHelicopter);
            }
        }

        private void OnEntitySpawned(PatrolHelicopter patrolHelicopter)
        {
            if (patrolHelicopter != null)
                TargetableHeliComponent.Install(patrolHelicopter);
        }

        private void OnEntityKill(PatrolHelicopter patrolHelicopter)
        {
            if (patrolHelicopter != null)
                TargetableHeliComponent.Detach(patrolHelicopter);
        }

        private void OnSamSiteTargetScan(SamSite samSite, List<ISamSiteTarget> results)
        {
            if (samSite == null)
                return;

            if (samSite.IsInDefenderMode())
                return;

            if (_targetableHelis.Count == 0)
                return;

            Vector3 samPosition = samSite.transform.position;
            float maximumSqr = _config.TargetingRange * _config.TargetingRange;

            foreach (TargetableHeliComponent targetableHeli in _targetableHelis)
            {
                if (targetableHeli == null)
                    continue;

                Vector3 difference = samPosition - targetableHeli.Position;
                if (difference.sqrMagnitude <= maximumSqr)
                {
                    results.Add(targetableHeli);
                }
            }
        }

        private object OnSamSiteTarget(SamSite samSite, TargetableHeliComponent targetableHeli)
        {
            if (samSite == null || targetableHeli == null)
                return null;

            bool canFire = false;

            if (samSite.staticRespawn)
            {
                canFire = _config.AllowMonumentSamSitesToEngage;
            }
            else
            {
                if (samSite.OwnerID != 0UL && PermissionUtil.HasPermission(FindPlayerById(samSite.OwnerID), PermissionUtil.USE))
                    canFire = true;
            }

            if (canFire)
                return null;
            else
                return (object)false;
        }

        private void CanSamSiteShoot(SamSite samSite)
        {
            TargetableHeliComponent targetableHeli = samSite.currentTarget as TargetableHeliComponent;
            if (targetableHeli == null)
                return;

            Vector3 predictedPoint = CalculateInterceptionPoint(targetableHeli.PatrolHelicopter, samSite, targetableHeli.EstimatedVelocity(), _config.RocketFlightSpeedMultiplier);

            Vector3 newDirection = predictedPoint - samSite.eyePoint.position;
            samSite.currentAimDir = newDirection.normalized;
        }

        private void OnEntityTakeDamage(PatrolHelicopter patrolHelicopter, HitInfo hitInfo)
        {
            if (patrolHelicopter == null || hitInfo == null)
                return;

            SamSite attacker = hitInfo.Initiator as SamSite;
            if (attacker == null)
                return;

            if (_config.RocketDamageMultiplier > 1f)
                hitInfo.damageTypes.ScaleAll(_config.RocketDamageMultiplier);
        }

        #endregion Oxide Hooks

        #region Targetable Heli Component

        public class TargetableHeliComponent : FacepunchBehaviour, ISamSiteTarget
        {
            public PatrolHelicopter PatrolHelicopter { get; private set; }
            private Transform _transform;

            public static TargetableHeliComponent Install(PatrolHelicopter patrolHelicopter)
            {
                TargetableHeliComponent existingTargetableHeli = patrolHelicopter.gameObject.GetComponent<TargetableHeliComponent>();

                if (existingTargetableHeli != null)
                    return existingTargetableHeli;

                TargetableHeliComponent targetableHeli = patrolHelicopter.gameObject.AddComponent<TargetableHeliComponent>();

                targetableHeli.Initialize();
                return targetableHeli;
            }

            public static void Detach(PatrolHelicopter patrolHelicopter)
            {
                TargetableHeliComponent component = patrolHelicopter.gameObject.GetComponent<TargetableHeliComponent>();

                if (component != null)
                    component.DestroySelf();
            }

            public void Initialize()
            {
                PatrolHelicopter = GetComponent<PatrolHelicopter>();
                _transform = PatrolHelicopter.transform;
                _targetableHelis.Add(this);
            }

            public void DestroySelf()
            {
                DestroyImmediate(this);
            }

            private void OnDestroy()
            {
                _targetableHelis.Remove(this);
            }

            #region ISamSiteTarget

            public bool isClient
            {
                get { return false; }
            }

            public Vector3 Position
            {
                get { return _transform.position; }
            }

            public SamTargetType SAMTargetType
            {
                get
                {
                    return new SamTargetType(_config.TargetingRange, _config.RocketFlightSpeedMultiplier, _config.TimeBetweenBurstsSeconds);
                }
            }

            public bool IsValidSAMTarget(bool isStaticSamSite)
            {
                if (isStaticSamSite)
                    return _config.AllowMonumentSamSitesToEngage;

                return true;
            }

            public Vector3 CenterPoint()
            {
                return PatrolHelicopter.CenterPoint();
            }

            public Vector3 GetWorldVelocity()
            {
                return PatrolHelicopter.GetWorldVelocity();
            }

            public bool IsVisible(Vector3 sourcePosition, float distance)
            {
                return PatrolHelicopter.IsVisible(sourcePosition, distance);
            }

            #endregion´ISamSiteTarget

            #region Helper Functions

            public Vector3 EstimatedVelocity()
            {
                PatrolHelicopter patrolHelicopter = PatrolHelicopter as PatrolHelicopter;
                if (patrolHelicopter == null)
                {
                    return Vector3.zero;
                }

                PatrolHelicopterAI heliAI = patrolHelicopter.myAI;
                if (heliAI == null)
                {
                    return Vector3.zero;
                }

                Vector3 direction = heliAI.GetLastMoveDir();
                float speed = heliAI.GetMoveSpeed();

                return direction.normalized * speed * 1.25f;
            }

            #endregion Helper Functions
        }

        #endregion Targetable Heli Component

        #region Helper Functions

        private static Vector3 CalculateInterceptionPoint(BaseEntity targetEntity, SamSite samSite, Vector3 targetVelocity, float speedMultiplier)
        {
            Vector3 targetPosition = targetEntity.CenterPoint();
            Vector3 displacement = targetPosition - samSite.eyePoint.position;

            ServerProjectile projectileComponent = samSite.projectileTest.Get().GetComponent<ServerProjectile>();

            float projectileSpeed = projectileComponent.speed * speedMultiplier;

            float a = Vector3.Dot(targetVelocity, targetVelocity) - projectileSpeed * projectileSpeed;
            float b = 2f * Vector3.Dot(displacement, targetVelocity);
            float c = Vector3.Dot(displacement, displacement);

            float chosenTime;

            if (Mathf.Abs(a) < 0.001f)
            {
                if (Mathf.Abs(b) < 0.001f)
                {
                    chosenTime = 0f;
                }
                else
                {
                    chosenTime = -c / b;
                }
            }
            else
            {
                float discriminant = b * b - 4f * a * c;
                if (discriminant < 0f)
                {
                    chosenTime = 0f;
                }
                else
                {
                    float sqrt = Mathf.Sqrt(discriminant);
                    float t1 = (-b + sqrt) / (2f * a);
                    float t2 = (-b - sqrt) / (2f * a);
                    chosenTime = Mathf.Max(Mathf.Min(t1, t2), 0f);
                }
            }

            Vector3 futureOffset = targetVelocity * chosenTime;
            return targetPosition + futureOffset;
        }

        public static BasePlayer FindPlayerById(ulong playerId)
        {
            return RelationshipManager.FindByID(playerId);
        }

        #endregion Helper Functions

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "patrolhelisams.use";
            private static readonly List<string> _permissions = new List<string>
            {
                USE,
            };

            public static string ConstructPermission(string suffix, bool addToList = true)
            {
                string perm = string.Join(".", nameof(PatrolHeliSams), suffix).ToLower();

                if (addToList && !_permissions.Contains(perm))
                    _permissions.Add(perm);

                return perm;
            }

            public static void AddPermission(string permission)
            {
                if (!_permissions.Contains(permission))
                    _permissions.Add(permission);
            }

            public static void RegisterPermissions()
            {
                foreach (string perm in _permissions)
                    _plugin.permission.RegisterPermission(perm, _plugin);
            }

            public static bool HasPermission(BasePlayer player, string permission)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permission);
            }
        }

        #endregion Permissions

        #region Commands

        [ConsoleCommand("sam.test")]
        private void cmdSamTest(ConsoleSystem.Arg conArgs)
        {
            BasePlayer player = conArgs.Player();
            if (player == null || !player.IsAdmin)
            {
                SendReply(player, "You must be an admin to use this command.");
                return;
            }

            const float heliSearchRadius = 300f;
            const float samSearchRadius = 50f;

            Vector3 playerPos = player.transform.position;

            List<BaseEntity> nearbyHelis = Pool.Get<List<BaseEntity>>();
            Vis.Entities(playerPos, heliSearchRadius, nearbyHelis, Layers.Mask.Default, QueryTriggerInteraction.Ignore);

            PatrolHelicopter nearestHeli = null;
            float nearestHeliDist = heliSearchRadius;

            foreach (BaseEntity entity in nearbyHelis)
            {
                PatrolHelicopter heli = entity as PatrolHelicopter;
                if (heli == null)
                    continue;

                float dist = Vector3.Distance(playerPos, heli.transform.position);
                if (dist < nearestHeliDist)
                {
                    nearestHeli = heli;
                    nearestHeliDist = dist;
                }
            }
            Pool.FreeUnmanaged(ref nearbyHelis);

            if (nearestHeli == null)
            {
                SendReply(player, $"No patrol helicopter within {heliSearchRadius} m.");
                return;
            }

            List<BaseEntity> nearbySamEntities = Pool.Get<List<BaseEntity>>();
            Vis.Entities(playerPos, samSearchRadius, nearbySamEntities, Layers.Mask.Deployed, QueryTriggerInteraction.Ignore);

            SamSite nearestSam = null;
            float nearestSamDist = samSearchRadius;

            foreach (BaseEntity entity in nearbySamEntities)
            {
                SamSite sam = entity as SamSite;
                if (sam == null || !sam.IsPowered())
                    continue;

                float dist = Vector3.Distance(playerPos, sam.transform.position);
                if (dist < nearestSamDist)
                {
                    nearestSam = sam;
                    nearestSamDist = dist;
                }
            }
            Pool.FreeUnmanaged(ref nearbySamEntities);

            if (nearestSam == null)
            {
                SendReply(player, $"No powered sam site within {samSearchRadius} m.");
                return;
            }

            Vector3 interceptPoint = CalculateInterceptionPoint(
                nearestHeli,
                nearestSam,
                nearestHeli.GetWorldVelocity(),
                _config.RocketFlightSpeedMultiplier);

            Vector3 fireDirection = (interceptPoint - nearestSam.eyePoint.position).normalized;

            nearestSam.FireProjectile(
                nearestSam.tubes[0].position,
                fireDirection,
                _config.RocketFlightSpeedMultiplier);

            SendReply(player, $"Test rocket fired: heli {nearestHeliDist:F1} m away; sam {nearestSamDist:F1} m away.");
        }

        #endregion Commands
    }
}