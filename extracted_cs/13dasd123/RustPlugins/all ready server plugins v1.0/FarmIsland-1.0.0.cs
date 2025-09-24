using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
   [Info("FarmIsland", "CASHR#6906", "1.0.0")]
    internal class FarmIsland : RustPlugin
    {
        #region Static

        private Configuration _config;
        private bool IsEvent;

        #endregion

        #region Config

        private class Configuration
        {
          
            [JsonProperty("Радиус от центральной точки")]
            public readonly float Radius = 50f;
            [JsonProperty("Центрая точка(координаты)")]
            public readonly string CenterPosition = "(-90.8, 100.0, 215.8)";
            [JsonProperty("SkinID карты")] public ulong SkinID = 0;

            [JsonProperty(PropertyName = "Ящик где спавнится карта/шанс",ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string,float> BoxList = new Dictionary<string, float>()
            {
                ["crate_elite"] = 10,
                ["crate_normal"] = 10
            };
            
            [JsonProperty("Количество камней которые должны заспавниться")]
            public readonly int OreAmount = 20;
            
            
            [JsonProperty(PropertyName = "Список префабов камней", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public readonly List<string> OreList = new List<string>()
            {
                "assets/bundled/prefabs/autospawn/resource/ores/metal-ore.prefab",
                "assets/bundled/prefabs/autospawn/resource/ores/sulfur-ore.prefab",
                "assets/bundled/prefabs/autospawn/resource/ores/stone-ore.prefab"
            };
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

        private void OnServerInitialized()
        {
            LoadConfig();
            var list = new List<OreResourceEntity>();
            Vis.Entities(_config.CenterPosition.ToVector3(), _config.Radius, list);
            foreach (var check in list)
            {
                check?.Kill();
            }
        }

        private object OnCardSwipe(CardReader cardReader, Keycard card, BasePlayer player)
        {
            if (InZone(player) && card.skinID != _config.SkinID) return false;
            if (!InZone(player) || card.skinID != _config.SkinID) return null;
            card.GetItem().UseItem();
            SpawnOre();
            cardReader.Invoke((cardReader.GrantCard), 0.5f);
            return null;
        }

        private void OnEntitySpawned(OreResourceEntity entity)
        {
            if (entity == null) return;
            if (InZone(entity) && !IsEvent)
                entity.Kill();
        }

        
        private void OnLootEntity(BasePlayer player, LootContainer entity)
        {
            if (player == null || entity == null || !_config.BoxList.ContainsKey(entity.ShortPrefabName)) return;
            if (entity.OwnerID != 0) return;
            var chance = Core.Random.Range(0, 100);
            if (!(chance <= _config.BoxList[entity.ShortPrefabName])) return;
            var item = ItemManager.CreateByName("keycard_red", 1, _config.SkinID);
            item.name = "Космическая карта";
            item.MoveToContainer(entity.inventory);
            entity.OwnerID = player.userID;

        }
        #endregion
 

        #region Function

        [ChatCommand("getcard")]
        private void cmdChatgetcard(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            var item = ItemManager.CreateByName("keycard_red", 1, _config.SkinID);
            item.name = "Космическая карта";
            player.GiveItem(item);
        }
        
        private void SpawnOre()
        {
			IsEvent = true;
            for (var i = 0; i < _config.OreAmount; i++)
            {
                var oreprefab = _config.OreList.GetRandom();
                var ore =
                    GameManager.server.CreateEntity(oreprefab, RandomCircle(_config.CenterPosition.ToVector3(),Core.Random.Range(2, 20)));
                if (ore == null)
				{
					PrintError("руда null");
					 continue;
				}
                ore.Spawn();
            }
			IsEvent = false;
        }

        private bool InZone(BaseEntity entity)
        {
            return Vector3.Distance(entity.transform.position, _config.CenterPosition.ToVector3()) <= _config.Radius;
        }

        
        private static float GetGroundPosition(Vector3 pos)
        {
            var y = TerrainMeta.HeightMap.GetHeight(pos);
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(pos.x, pos.y + 5f, pos.z), Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask( "Terrain", "World", "Default", "Construction", "Deployed")) && !hit.collider.name.Contains("rock_cliff"))
                return Mathf.Max(hit.point.y, y);
            return y;
        }

        private static Vector3 RandomCircle(Vector3 center, float radius = 2)
        {
            var ang = UnityEngine.Random.value * 360;
            Vector3 pos;
            pos.x = center.x + radius * Mathf.Sin(ang * Mathf.Deg2Rad);
            pos.z = center.z + radius * Mathf.Cos(ang * Mathf.Deg2Rad);
            pos.y = center.y;
            pos.y = GetGroundPosition(pos);
            return pos;
        }
        #endregion


    }
}