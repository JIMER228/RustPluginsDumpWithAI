using ConVar;
using Oxide.Core.Plugins;
using UnityEngine;
using UnityEngine.AI;

namespace Oxide.Plugins
{
    [Info("ZealBigOre", "Kira", "1.0.0")]
    [Description("Большая руда")]
    public class ZealBigOre : RustPlugin
    {
        void OnServerInitialized()
        {
            permission.RegisterPermission("zealbigore.use", this);
            foreach (var obj in SphereEntity.FindObjectsOfType<SphereEntity>())
            {
                obj.Kill();
            }
            
            foreach (var obj in UnityEngine.Object.FindObjectsOfType<BaseHelicopter>())
            { 
                obj.Kill();
            }
        } 

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is OreHotSpot) 
            {
 
            } 
        }  
 
        [ConsoleCommand("spawnore")]   
        private void SpawnOre(ConsoleSystem.Arg args)
        { 
            BasePlayer player = args.Player();   
            SphereEntity sphere =
                GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab") as SphereEntity;
            sphere.currentRadius = 15; 
            sphere.lerpSpeed = 0;
            sphere.transform.position = player.transform.position;

            BaseEntity ore = GameManager.server.CreateEntity(
                "assets/bundled/prefabs/radtown/crate_normal_2.prefab");
            ore.SendNetworkUpdate();
            sphere.Spawn();
            ore.SetParent(sphere);
            ore.Spawn();
            timer.Repeat(0.001f, 100000, () =>
            {
                sphere.currentRadius = Random.Range(1, 10);
            });
        }

        [ConsoleCommand("hren")]
        private void Hren(ConsoleSystem.Arg args)
        {
            args.Player().Teleport(new Vector3(0, 0, 0));
        }
    }
}