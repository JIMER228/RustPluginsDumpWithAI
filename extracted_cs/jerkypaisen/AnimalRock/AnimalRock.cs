using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("AnimalRock", "jerkypaisen", "1.0.0")]
    [Description("You can catch animals by attacking them with a Rock. However, it is not possible to catch a horse.")]
    class AnimalRock : RustPlugin
    {
        #region [Fields]
        private const string permUse = "animalRock.use";
        #endregion

        #region [Oxide Hooks]
        private void Init()
        {
            permission.RegisterPermission(permUse, this);
        }

        void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            if (info == null) return;
            if (!permission.UserHasPermission(attacker.UserIDString, permUse)) return;
            var sumonRock = Animals.FirstOrDefault(x => x.sumonRockSkinId == info.Weapon.skinID);
            if (sumonRock == null)
            {
                if (info.HitEntity == null) return;
                CatchAnimal(attacker, info);
            }
            else{
                SpawnAnimal(attacker, info, sumonRock);
            }
        }

        void OnMeleeThrown(BasePlayer player, Item item)
        {
            if (!permission.UserHasPermission(player.UserIDString, permUse)) return;
            var animalRock = Animals.FirstOrDefault(x => x.sumonRockSkinId == item.skin);
            if (animalRock != null)
            {
                item.Remove(2f);
            }
        }
        #endregion

        #region [Hooks]
        private void CatchAnimal(BasePlayer player, HitInfo info)
        {
            var heldItem = player?.GetActiveItem() ?? null;
            var hitAnimal = Animals.FirstOrDefault(x => x.displayName == info.HitEntity.ShortPrefabName);
            if (hitAnimal != null)
            {
                string sumonRockPrefabName = info.WeaponPrefab.ShortPrefabName;
                if (sumonRockPrefabName != "rock.entity") return;
                info.HitEntity.Kill();
                Item item = ItemManager.CreateByName("rock", 1, hitAnimal.sumonRockSkinId);
                if (item != null)
                {
                    item.name = hitAnimal.itemName;
                    player.GiveItem(item);
                }
                if (heldItem != null) heldItem.RemoveFromContainer();
            }
        }

        private void SpawnAnimal(BasePlayer player, HitInfo info, AnimalEntry ae)
        {
            var heldItem = player?.GetActiveItem() ?? null;
            if (heldItem != null)
            {
                heldItem.RemoveFromContainer();
            }
            Vector3 pos = info.HitPositionWorld;
            GameObject gameObject = Instantiate.GameObject(GameManager.server.FindPrefab(ae.prefab), pos, new Quaternion());
            gameObject.name = ae.prefab;
            SceneManager.MoveGameObjectToScene(gameObject, Rust.Server.EntityScene);
            UnityEngine.Object.Destroy(gameObject.GetComponent<Spawnable>());
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            BaseEntity entity = gameObject.GetComponent<BaseEntity>();
            entity.enableSaving = false;
            entity.Spawn();
        }
		#endregion

        #region [Classes]
        private class AnimalEntry
        {
            public ulong sumonRockSkinId;
            public string displayName;
            public string prefab;
            public string itemName;
        }
		#endregion

		#region [Data]
        private AnimalEntry[] Animals = new[]
        {
            new AnimalEntry
            {
                sumonRockSkinId = 2972231829,
                displayName = "chicken",
                prefab = "assets/rust.ai/agents/chicken/chicken.prefab",
                itemName = "Summon Chiken",
            },
            new AnimalEntry
            {
                sumonRockSkinId = 2972218156,
                displayName = "boar",
                prefab = "assets/rust.ai/agents/boar/boar.prefab",
                itemName = "Summon Boar",
            },
            new AnimalEntry
            {
                sumonRockSkinId = 2972230714,
                displayName = "stag",
                prefab = "assets/rust.ai/agents/stag/stag.prefab",
                itemName = "Summon Stag",
            },
            new AnimalEntry
            {
                sumonRockSkinId = 2972235993,
                displayName = "wolf",
                prefab = "assets/rust.ai/agents/wolf/wolf.prefab",
                itemName = "Summon Wolf",
            },
            new AnimalEntry
            {
                sumonRockSkinId = 2972228775,
                displayName = "bear",
                prefab = "assets/rust.ai/agents/bear/bear.prefab",
                itemName = "Summon Bear",
            },
            new AnimalEntry
            {
                sumonRockSkinId = 2972698694,
                displayName = "polarbear",
                prefab = "assets/rust.ai/agents/bear/polarbear.prefab",
                itemName = "Summon polarbear",
            }
        };
        #endregion
    }
}
