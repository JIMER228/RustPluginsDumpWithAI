using Facepunch;
using Oxide.Core.Configuration;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("HeliAirSpawned", "Yan", "0.1.0")]
	[Description("Настройка вылета чинука/аирдропа/вертолета")]
    class HeliAirSpawned : RustPlugin
    {
        #region Configuration
        public float MaxConfHeli;
        public float MinConfHeli;
        public float MaxConfAIR;
        public float MinConfAIR;
        public float MaxConfCHINUK;
        public float MinConfCHINUK;
        public float ConfigurationChinuk;
        public float ConfigurationAir;
        int CMinOHeli;
        int CMinOChinuk;
        int CMinOAir;
        bool AdminMessages;

        protected override void LoadDefaultConfig()
        {
            GetVariable(Config, "Частота вылета патрульного вертолёта максимально в минутах", out MaxConfHeli, 70);
            GetVariable(Config, "Частота вылета патрульного вертолёта минимально в минутах", out MinConfHeli, 60);
            GetVariable(Config, "Оповещать администратора о вылете самолёта или вертолёта", out AdminMessages, true);
            SaveConfig();
        }

        public static void GetVariable<T>(DynamicConfigFile config, string name, out T value, T defaultValue)
        {
            config[name] = value = config[name] == null ? defaultValue : (T)Convert.ChangeType(config[name], typeof(T));
        }

        #endregion

        void OnServerInitialized()
        {
            LoadDefaultConfig();

            var timerHeli = UnityEngine.Random.Range(MinConfHeli, MaxConfHeli) * 60;
            timer.Every(timerHeli, () => { SpawnHeli(); });
        }

        void SpawnHeli()
        {
            var heli = UnityEngine.Object.FindObjectsOfType<BaseHelicopter>().ToList();
            if (AdminMessages)
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.IsAdmin)
                    SendReply(player, $"Удалено активных Patrol Helicopter {heli.Count}, новый вертолёт вылетел");
            }
            foreach (var helic in heli)
                helic.Kill();

            if (BasePlayer.activePlayerList.Count >= CMinOHeli)
            {
                BaseEntity patrol = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(0, 0, 0), Quaternion.Euler(new Vector3(0, 0, 0)), true);
                patrol.Spawn();
            }
            else PrintWarning("НЕ хватает игроков для того что бы создать Patrol Helicopter");
        }

        void SpawnAir()
        {
            if (BasePlayer.activePlayerList.Count >= CMinOAir)
            {
                var planes = UnityEngine.Object.FindObjectsOfType<CargoPlane>().ToList();
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (player.IsAdmin)
                    SendReply(player, $"[HeliAirSpawned] Удалено активных Cargo Plane {planes.Count}, новый самолёт вылетел");
                }
                foreach (var plane in planes)
                    plane.Kill();
                BaseEntity cargo = GameManager.server.CreateEntity("assets/prefabs/npc/cargo plane/cargo_plane.prefab", new Vector3(0, 0, 0), Quaternion.Euler(new Vector3(0, 0, 0)), true);
                cargo.Spawn();
            }
            else
            {
                PrintWarning("НЕ хватает игроков для того что бы создать Cargo Plane");
            }
        }

        void SpawnChinuk()
        {
            if (BasePlayer.activePlayerList.Count >= CMinOAir)
            {
                var chinook = UnityEngine.Object.FindObjectsOfType<CH47Helicopter>().ToList();
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (player.IsAdmin)
                        SendReply(player, $"[HeliAirSpawned] Удалено активных Chinook {chinook.Count}, новый вертолёт вылетел");
                }
                foreach (var chin in chinook)
                    chin.Kill();

                Vector3 pos;
                pos.x = 0;
                pos.z = 0;
                pos.y = 0;
                pos = RandomDropPosition();
                pos.y = pos.y + 100f;
                BaseEntity chientity = GameManager.server.CreateEntity("assets/prefabs/npc/ch47/ch47scientists.entity.prefab", pos, Quaternion.Euler(new Vector3(0, 0, 0)), true);
                chientity.Spawn();
            }
            else
            {
                PrintWarning("НЕ хватает игроков для того что бы создать Chinook Heli");
            }
        }

        #region Spawn

        static float GetGroundPosition(Vector3 pos)
        {
            float y = TerrainMeta.HeightMap.GetHeight(pos);

            RaycastHit hit;
            if (Physics.Raycast(new Vector3(pos.x, pos.y + 200f, pos.z), Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask(new[] { "Terrain", "World", "Default", "Construction", "Deployed" })))
                return Mathf.Max(hit.point.y, y);

            return y;
        }

        SpawnFilter filter = new SpawnFilter();

        public Vector3 RandomDropPosition()
        {
            var vector = Vector3.zero;
            float num = 1000f, x = TerrainMeta.Size.x / 3;
            do
            {
                vector = Vector3Ex.Range(-x, x);
            }
            while (filter.GetFactor(vector) == 0f && (num -= 1f) > 0f);
            float max = TerrainMeta.Size.x / 2;

            float height = TerrainMeta.HeightMap.GetHeight(vector);

            vector.y = height;

            return vector;
        }

        List<int> BlockedLayers = new List<int> { (int)Layer.Water, (int)Layer.Construction, (int)Layer.Trigger, (int)Layer.Prevent_Building, (int)Layer.Deployed, (int)Layer.Tree };

        static int blockedMask = LayerMask.GetMask(new[] { "Player (Server)", "Trigger", "Prevent Building" });

        public Vector3 GetSafeDropPosition(Vector3 position)
        {
            RaycastHit hit;
            position.y += 200f;

            if (Physics.Raycast(position, Vector3.down, out hit))
            {
                if (hit.collider?.gameObject == null)
                    return Vector3.zero;

                if (BlockedLayers.Contains(hit.collider.gameObject.layer))
                {
                    position.y = Mathf.Max(hit.point.y, TerrainMeta.HeightMap.GetHeight(position));

                    var colliders = Pool.GetList<Collider>();
                    Vis.Colliders(position, 1, colliders, blockedMask, QueryTriggerInteraction.Collide);

                    bool blocked = colliders.Count > 0;

                    Pool.FreeList<Collider>(ref colliders);

                    if (!blocked)
                        return position;
                }
            }

            return Vector3.zero;
        }

        List<Vector3> monuments = new List<Vector3>();

        List<Vector3> Road = new List<Vector3>();

        public Vector3 GetEventPosition()
        {
            var eventPos = Vector3.zero;
            int maxRetries = 100;
            monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>().Select(monument => monument.transform.position).ToList();
            Road = UnityEngine.Object.FindObjectsOfType<GenerateRoadMeshes>().Select(road => road.transform.position).ToList();
            do
            {
                eventPos = GetSafeDropPosition(RandomDropPosition());

                foreach (var monument in monuments)
                {
                    if (Vector3.Distance(eventPos, monument) < 1f) // don't put the treasure chest near a monument
                    {
                        eventPos = Vector3.zero;
                        break;
                    }
                }

                foreach (var road in Road)
                {
                    Puts(eventPos.ToString());
                    break;
                }

            } while (eventPos == Vector3.zero && --maxRetries > 0);

            eventPos.y = GetGroundPosition(eventPos);


            if (eventPos.y > 30)
            {
                return GetEventPosition();
            }

            return eventPos;
        }

        #endregion
    }
}
