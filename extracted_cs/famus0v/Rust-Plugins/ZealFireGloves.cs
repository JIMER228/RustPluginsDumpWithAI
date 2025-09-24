using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealFireGloves", "Kira", "1.0.0")]
    public class ZealFireGloves : RustPlugin
    {
        public const string FirePrefab = "assets/bundled/prefabs/fireball_small.prefab";

        private class FireGloves : MonoBehaviour
        {
            public BasePlayer player;
            public BaseEntity flame;
            public BaseEntity gloves;
            public FireBall fire;
            public Vector3 position;
            public Quaternion rotation;

            private void Start()
            {
                player = GetComponent<BasePlayer>();
                gloves = player.inventory.containerWear.FindItemsByItemName("tactical.gloves").GetHeldEntity();
                position = new Vector3(0, 0, 0);
                rotation = Quaternion.identity;
                SpawnFireEffects();
            }

            private void SpawnFireEffects()
            {
                flame = GameManager.server.CreateEntity(FirePrefab, position, rotation);
                fire = flame.GetComponent<FireBall>();
                fire.generation = 10f;
                fire.radius = 10f;
                fire.tickRate = 10f;
                fire.SetParent(gloves, 0);
                flame?.Spawn();
            }

            private void OnDestroy()
            {
                flame.Kill();
                fire.Kill();
                flame.SendNetworkUpdate();
                fire.SendNetworkUpdate();
            }
        }

        private object CanWearItem(PlayerInventory inventory, Item item, int targetSlot)
        {
            if (item.info.shortname == "tactical.gloves")
            {
                PrintToChat("1");
                item.GetOwnerPlayer().gameObject.AddComponent<FireGloves>();
            }

            return null;
        }

        [ConsoleCommand("give.firegloves")]
        private void GiveFireGloves(ConsoleSystem.Arg args)
        {
            var item = ItemManager.CreateByName("tactical.gloves", 1, 5);
            args.Player().GiveItem(item);
        }

        [ConsoleCommand("delete.firegloves")]
        private void DeleteFireGloves(ConsoleSystem.Arg args)
        {
            UnityEngine.Object.Destroy(args.Player().GetComponent<FireGloves>());
        }
    }
}