using System;
using UnityEngine;
using WebSocketSharp;
using Random = System.Random;

namespace Oxide.Plugins
{
    [Info("ZealFallingCars", "Kira", "1.0.0")]
    public class ZealFallingCars : RustPlugin
    {
        private const string Minicopter = "assets/content/vehicles/minicopter/minicopter.entity.prefab";

        private const string ScrapMinicopter =
            "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab";

        private const string RowBoat = "assets/content/vehicles/boats/rowboat/rowboat.prefab";
        private const string Rhib = "assets/content/vehicles/boats/rhib/rhib.prefab";
        private const string Parachute = "assets/prefabs/misc/parachute/parachute.prefab";

        public class FallEntity : MonoBehaviour
        {
            public BaseEntity entity;
            public BaseEntity parachute;
            public Rigidbody entityRigidbody;

            private void Awake()
            {
                entity = GetComponent<BaseEntity>();
                entityRigidbody = entity.GetComponent<Rigidbody>();
                AddParachute();
            }

            public void AddParachute()
            {
                entityRigidbody.useGravity = false;
                parachute = GameManager.server.CreateEntity(Parachute);
                parachute.SetParent(entity, "parachute_attach");
                parachute.Spawn();
                parachute.SendNetworkUpdateImmediate(true);
            }

            public void RemoveParachute()
            {
                entityRigidbody.useGravity = true;
                parachute.Kill();
                parachute.SendNetworkUpdateImmediate(true);
                parachute = null;
            }

            private void FixedUpdate()
            {
                if (WaterLevel.Test(entity.transform.position, false, entity)) Destroy(this);
                if (parachute.IsValid()) entity.transform.position -= new Vector3(0, 5f * Time.deltaTime, 0);
            }

            private void OnCollisionEnter()
            {
                Destroy(this);
            }

            private void OnDestroy()
            {
                RemoveParachute();
            }
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnExplosiveDropped(BasePlayer player, BaseEntity entity, ThrownWeapon item)
        {
            if (item.GetItem().info.shortname != "flare") return;
            var flare = item.GetItem();
            if (flare.skin == 0) return;
            CallDropper(flare.skin, player.transform.position);
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnExplosiveThrown(BasePlayer player, BaseEntity entity, ThrownWeapon item)
        {
            if (item.GetItem().info.shortname != "flare") return;
            var flare = item.GetItem();
            if (flare.skin == 0) return;
            CallDropper(flare.skin, player.transform.position);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntitySpawned(BaseVehicle vehicle)
        {
            if (vehicle == null) return;
            NextTick((() =>
            {
                var fuelsystem = vehicle.GetFuelSystem();
                if (fuelsystem == null) return;
                fuelsystem.AddStartingFuel(200);
            }));
        }

        private void CallDropper(ulong entity, Vector3 pos)
        {
            BaseEntity obj = null;
            var position = pos;
            pos = new Vector3(position.x + Core.Random.Range(-5, 5), position.y + 100,
                position.z + Core.Random.Range(-5, 5));
            switch (entity)
            {
                case 2162376657:
                    obj = GameManager.server.CreateEntity(ScrapMinicopter, pos);
                    break;
                case 1674659844:
                    obj = GameManager.server.CreateEntity(Minicopter, pos);
                    break;
                case 2:
                    obj = GameManager.server.CreateEntity(Rhib, pos);
                    break;
                case 2244621543:
                    obj = GameManager.server.CreateEntity(RowBoat, pos);
                    break;
            }

            if (obj != null)
            {
                obj.Spawn(); 
                obj.gameObject.AddComponent<FallEntity>();
            }
            else PrintError($"#1 {entity} {pos}");
        }

        [ConsoleCommand("give.fallcar")]
        // ReSharper disable once UnusedMember.Local
        private void GiveFallCar(ConsoleSystem.Arg args)
        {
            if (args.Args[0].IsNullOrEmpty() | args.Args[1].IsNullOrEmpty()) return;
            var player = BasePlayer.FindByID(Convert.ToUInt64(args.Args[0]));
            var item = ItemManager.CreateByName("flare");
            switch (args.Args[1])
            {
                case "minicopter":
                    item.skin = 1674659844;
                    item.name = "Вызов миникоптера";
                    player.GiveItem(item);
                    break;
                case "scraptransporthelicopter":
                    item.skin = 2162376657;
                    item.name = "Вызов транспортного вертолёта";
                    player.GiveItem(item);
                    break;
                case "rhib":
                    item.skin = 2;
                    item.name = "Вызов военной лодки";
                    player.GiveItem(item);
                    break;
                case "rowboat":
                    item.skin = 2244621543;
                    item.name = "Вызов обычной лодки";
                    player.GiveItem(item);
                    break;
            }
        }
    }
}