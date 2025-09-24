using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("F15AirStrike", "Fruster", "1.0.7")]
    [Description("F15AirStrike")]
    class F15AirStrike : CovalencePlugin
    {
        [PluginReference] Plugin HomingMissiles;
        private class F15Targets : FacepunchBehaviour
        {
            public BasePlayer initiator;
            public Timer attackTimer;
            public int startAttackDistance = 600;
            public int endAttackDistance = 70;
            public List<BaseEntity> targetsList = new List<BaseEntity>();
            public List<BaseEntity> whiteList = new List<BaseEntity>();
            public bool attack = true;
            public Vector3 endPoint = Vector3.zero;
            public Vector3 mainTarget = Vector3.zero;
        }

        private class F15Rocket : FacepunchBehaviour
        {
            public Timer rocketTimer;
            public Vector3 targetPosition = Vector3.zero;
        }

        private List<BasePlayer> playersList = new List<BasePlayer>();
        private const string rocketPrefab = "assets/content/vehicles/mlrs/rocket_mlrs.prefab";
        private const string supplySignalPrefab = "assets/prefabs/tools/supply signal/supplysignal.weapon.prefab";
        private const string grenadeSupplySignalPrefab = "assets/prefabs/tools/supply signal/grenade.supplysignal.deployed.prefab";
        private const string grenadeFlashBangPrefab = "assets/prefabs/weapons/flashbang/grenade.flashbang.deployed.prefab";

        private ConfigData Configuration;
        private float rateOfFire = 0.4f;

        class ConfigData
        {
            [JsonProperty("Rockets speed")]
            public int rocketsSpeed = 150;

            [JsonProperty("Attack radius")]
            public int attackRadius = 150;

            [JsonProperty("Damage scale (1 = 100%)")]
            public float damageScale = 1;
            [JsonProperty("Rate of fire(number of shots per second)")]
            public float rateOfFire = 2.5f;
            [JsonProperty("Time to trigger a signal grenade")]
            public float fuseTime = 3f;
            [JsonProperty("Grenade throw force scaling")]
            public float forceScale = 1f;

            [JsonProperty("Damage to buildings")]
            public bool damageBuildings = false;

            [JsonProperty("Attack the initiator")]
            public bool initiatorTargeting = true;

            [JsonProperty("Attack the players")]
            public bool playersTargeting = true;

            [JsonProperty("Attack NPCs")]
            public bool NPCsTargeting = true;
            [JsonProperty("Attack the place where the signal supply was thrown (attacks only in the area, not aiming at players and NPCs)")]
            public bool areaTargeting = false;
            [JsonProperty("Attack accuracy (used only when attacking in an area)")]
            public int attackAccuracy = 30;
            [JsonProperty("Use homing missiles(need HomingMissiles plugin)")]
            public bool useHomingMissiles = false;
        }

        void SaveConfig() => Config.WriteObject(Configuration, true);

        void LoadConfig()
        {
            Configuration = Config.ReadObject<ConfigData>();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            Configuration = new ConfigData();
            SaveConfig();
        }

        private void OnServerInitialized()
        {
            LoadConfig();
            if (Configuration.fuseTime < 0.1f)
                Configuration.fuseTime = 0.1f;

            rateOfFire = 1f / Configuration.rateOfFire;
        }

        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            HeldEntity heldEntity = player.GetHeldEntity();
            if (heldEntity != null && heldEntity.name == supplySignalPrefab)
            {
                if (heldEntity.skinID == 2982472774)
                {
                    if (!playersList.Contains(player))
                        playersList.Add(player);
                }
                else if (playersList.Contains(player))
                {
                    playersList.Remove(player);
                }
            }
        }

        private void OnExplosiveDropped(BasePlayer player, BaseEntity entity, ThrownWeapon item)
        {
            OnExplosiveSpawn(player, entity);
        }

        private void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            OnExplosiveSpawn(player, entity);
        }

        private void OnExplosiveSpawn(BasePlayer player, BaseEntity entity)
        {
            if (playersList.Contains(player) && entity.name == grenadeSupplySignalPrefab)
            {
                var entity1 = GameManager.server.CreateEntity(grenadeFlashBangPrefab, entity.transform.position);
                var explosive = entity1 as Flashbang;
                explosive.timerAmountMin = Configuration.fuseTime;
                explosive.timerAmountMax = Configuration.fuseTime;
                entity1.Spawn();

                Rigidbody rig = entity.GetComponent<Rigidbody>();
                Rigidbody rig1 = entity1.GetComponent<Rigidbody>();
                rig1.velocity = rig.velocity * Configuration.forceScale;
                entity.Kill();
                timer.Once(Configuration.fuseTime - 0.01f, () => CallF15AirStrike(entity1.transform.position, player));
            }
        }

        private void CallF15AirStrike(Vector3 pos, BasePlayer player)
        {
            Vector3 spawnPosition = Vector3.zero;
            Vector3 destination = Vector3.zero;

            int radius = 1500;

            int randX = Random.Range(-1, 2);
            int randZ = Random.Range(-1, 2);

            if (randX + randZ == 0)
                randX = 1;

            spawnPosition = new Vector3(pos.x + randX * radius, 500, pos.z + randZ * radius);

            string prefab = "assets/scripts/entity/misc/f15/f15e.prefab";
            var entity = GameManager.server.CreateEntity(prefab, spawnPosition);
            entity.Spawn();

            F15 f15 = entity as F15;
            f15.turnRate = 0;
            F15Targets targets = entity.gameObject.AddComponent<F15Targets>();
            targets.mainTarget = pos;
            targets.initiator = player;

            if (Configuration.playersTargeting)
            {
                foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
                {
                    if (Vector3.Distance(activePlayer.transform.position, pos) < Configuration.attackRadius)
                        targets.targetsList.Add(activePlayer);
                }

                if (!Configuration.initiatorTargeting && player != null)
                {
                    targets.targetsList.RemoveAll(item => item == player);
                }
            }

            if (Configuration.NPCsTargeting)
            {
                foreach (BaseEntity item in BaseNetworkable.serverEntities)
                {
                    if (item.PrefabName.Contains("/npcplayer/humannpc/") && Vector3.Distance(item.transform.position, pos) < Configuration.attackRadius)
                        targets.targetsList.Add(item);
                }
            }

            targets.attackTimer = timer.Repeat(rateOfFire, (int)(24f / rateOfFire), () =>
            {
                if (entity != null)
                {
                    float distance = Vector3.Distance(entity.transform.position, targets.endPoint);

                    if (distance < targets.startAttackDistance)
                    {
                        if (distance < targets.endAttackDistance)
                            targets.attackTimer.Destroy();

                        if (distance > targets.endAttackDistance)
                            if (!Configuration.areaTargeting)
                            {
                                if (targets.targetsList.Count > 0 && targets.targetsList[0] != null)
                                {
                                    Vector3 targetPos = targets.targetsList[0].transform.position + Vector3.up * 5;
                                    SpawnRocket(entity, targetPos, targets.targetsList[0]);
                                    targets.targetsList.RemoveAt(0);
                                }
                            }
                            else
                            {
                                Vector3 targetPos = targets.mainTarget + new Vector3(UnityEngine.Random.Range(-Configuration.attackAccuracy, Configuration.attackAccuracy), 5, UnityEngine.Random.Range(-Configuration.attackAccuracy, Configuration.attackAccuracy));
                                SpawnRocket(entity, targetPos, null);
                            }
                    }
                }
            });

            timer.Once(0.01f, () =>
            {
                if (entity != null)
                {
                    entity.transform.position = new Vector3(spawnPosition.x, entity.transform.position.y, spawnPosition.z);
                    entity.transform.LookAt(new Vector3(pos.x, entity.transform.position.y, pos.z));
                    destination = entity.transform.forward * 9999f;
                    targets.endPoint = new Vector3(pos.x, entity.transform.position.y, pos.z) + entity.transform.forward * 100;
                }
            });

            timer.Once(120, () =>
            {
                if (entity != null)
                    entity.Kill();
            });
        }

        private void SpawnRocket(BaseEntity entity, Vector3 targetPos, BaseEntity target)
        {

            if (Configuration.useHomingMissiles)
            {
                Vector3 vel = new Vector3(targetPos.x - entity.transform.position.x, targetPos.y - entity.transform.position.y, targetPos.z - entity.transform.position.z);
                vel = Vector3.Normalize(vel) * 100;
                HomingMissiles?.Call("LaunchHomingMissile", entity.transform.position - entity.transform.forward * 100, targetPos, vel, target);
            }
            else
            {
                var rocket = GameManager.server.CreateEntity(rocketPrefab, entity.transform.position - entity.transform.forward * 100);
                rocket.Spawn();
                F15Targets tar = entity.GetComponent<F15Targets>();
                rocket.creatorEntity = tar.initiator;
                TimedExplosive explosive = rocket as TimedExplosive;
                Rust.DamageTypeEntry dam = new Rust.DamageTypeEntry();
                dam.type = Rust.DamageType.Hunger;
                dam.amount = 3f;
                explosive.damageTypes.Add(dam);

                F15Rocket rct = rocket.gameObject.AddComponent<F15Rocket>();
                rct.targetPosition = targetPos;

                Vector3 direction = Vector3.Normalize(rct.targetPosition - rocket.transform.position);
                rocket.SendMessage("InitializeVelocity", direction * Configuration.rocketsSpeed);

                rct.rocketTimer = timer.Repeat(1f, 10, () =>
                {
                    if (rocket != null)
                    {
                        if (Vector3.Distance(rocket.transform.position, rct.targetPosition) > Configuration.rocketsSpeed)
                        {
                            direction = Vector3.Normalize(rct.targetPosition - rocket.transform.position);
                            rocket.SendMessage("InitializeVelocity", direction * Configuration.rocketsSpeed);
                        }
                        else
                        {
                            rct.rocketTimer.Destroy();
                        }
                    }
                });
            }
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (info.WeaponPrefab != null && info.WeaponPrefab.PrefabName == rocketPrefab && info.damageTypes.Get(Rust.DamageType.Hunger) > 0)
            {
                info.damageTypes.ScaleAll(Configuration.damageScale);

                if (!Configuration.damageBuildings && entity is DecayEntity)
                    info.damageTypes.ScaleAll(0);
            }
        }

        private object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (PlayerCheck(player))
                playersList.Remove(player);
            return null;
        }

        private bool PlayerCheck(BasePlayer player)
        {
            return playersList.Contains(player);
        }

        [Command("adminCallF15")]
        private void AdminCallF15(IPlayer iplayer)
        {
            BasePlayer player = iplayer.Object as BasePlayer;
            if (player != null && player.IsAdmin)
            {
                Vector3 playerPosition = player.transform.position;
                CallF15AirStrike(playerPosition, player);
                player.ChatMessage("You just called an airstrike to a point with coordinate " + playerPosition.ToString());
            }
        }

        [Command("GiveF15AirStrikeSignal")]
        private void GiveF15AirStrikeSignal(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;

            if (player && !player.IsAdmin)
            {
                iplayer.Reply("You don't have permission to use this command.");
                return;
            }

            if (args.Length < 1)
            {
                player?.ChatMessage("Syntax: /GiveAirStrikeSignal <Player or steamID> <number of items>");
                Puts("Syntax: /GiveAirStrikeSignal <Player or steamID> <number of items>");
                return;
            }

            string playerName = args[0].ToLower();
            BasePlayer targetPlayer = BasePlayer.activePlayerList.FirstOrDefault(p => p._name.ToLower().Contains(playerName));
            if (targetPlayer == null)
            {
                player?.ChatMessage("No such player found");
                Puts("No such player found");
                return;
            }

            int amount = 1;
            if (args.Length > 1)
            {
                int parsedAmount;
                if (int.TryParse(args[1], out parsedAmount))
                {
                    amount = parsedAmount;
                }
            }

            GiveItemToPlayer(targetPlayer, amount);
        }

        private void GiveItemToPlayer(BasePlayer player, int amount)
        {
            ItemDefinition itemDefinition = ItemManager.FindItemDefinition("supply.signal");
            if (itemDefinition != null)
            {
                for (int i = 0; i < amount; i++)
                {
                    Item item = ItemManager.Create(itemDefinition, 1, 2982472774);
                    item.name = "SUPPLY SIGNAL F15";
                    player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
                }
            }
        }

    }
}