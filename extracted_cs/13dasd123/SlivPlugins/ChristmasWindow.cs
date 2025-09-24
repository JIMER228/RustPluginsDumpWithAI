// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using UnityEngine;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("ChristmasWindow", "RustFlash", "1.0.0")]
    [Description("Enhances windows in Rust with Christmas lights.")]
    class ChristmasWindow : RustPlugin
    {
        const string metalShopfrontPrefab = "assets/prefabs/building/wall.frame.shopfront.metal.prefab";
        const string woodWindowPrefab = "assets/content/building/parts/static/wall.window.wood.prefab";
        const string xmasLightsPrefab = "assets/prefabs/misc/xmas/christmas_lights/xmas.lightstring.deployed.prefab";
        private static readonly Quaternion prefabRotation = Quaternion.Euler(0, 90, 0);

        void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            BaseEntity entity = gameObject.GetComponent<BaseEntity>();
            if (entity != null)
            {
                if (entity.ShortPrefabName == "wall.frame.shopfront.metal")
                {
                    SpawnXmasLights(entity, new Vector3(0f, 1.1f, 0f));
                    SpawnXmasLights(entity, new Vector3(0f, 2.2f, 0f)); 
                }
                else if (entity.ShortPrefabName == "wall.window.bars.wood")
                {
                    SpawnXmasLights(entity, new Vector3(0f, 0.1f, 0f));
                    SpawnXmasLights(entity, new Vector3(0f, 1.2f, 0f));
                }
                else if (entity.ShortPrefabName == "wall.window.bars.metal")
                {
                    SpawnXmasLights(entity, new Vector3(0f, 0.1f, 0f));
                    SpawnXmasLights(entity, new Vector3(0f, 1.2f, 0f));
                }
                else if (entity.ShortPrefabName == "wall.window.glass.reinforced")
                {
                    SpawnXmasLights(entity, new Vector3(0f, 0.1f, 0f));
                    SpawnXmasLights(entity, new Vector3(0f, 1.2f, 0f));
                }
                else if (entity.ShortPrefabName == "wall.window.bars.toptier")
                {
                    SpawnXmasLights(entity, new Vector3(0f, 0.1f, 0f));
                    SpawnXmasLights(entity, new Vector3(0f, 1.2f, 0f));
                }
            }
        }

        void SpawnXmasLights(BaseEntity entity, Vector3 position)
        {
            BaseEntity lightsEntity = GameManager.server.CreateEntity(xmasLightsPrefab, entity.transform.position, prefabRotation);
            if (lightsEntity == null) return;
            lightsEntity.SetParent(entity);
            lightsEntity.transform.localPosition = position;
            lightsEntity.Spawn();
        }
    }
}
