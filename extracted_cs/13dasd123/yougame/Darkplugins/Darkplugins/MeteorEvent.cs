using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;
using Rust;
using Oxide.Core.Plugins;
using System;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("MeteorEvent", "https://discord.gg/dNGbxafuJn", "1.1.7")]
    public class MeteorEvent : RustPlugin
    {
        [PluginReference] private Plugin ServerRewards, Economics;
        private Dictionary<uint, MeteorData> meteorData = new Dictionary<uint, MeteorData>();
        private int largeMeteorCount = 0;
        private int purchaseEventCooldown = 0;
        private Dictionary<ulong, int> playerPurchaseCooldowns = new Dictionary<ulong, int>();
        private List<uint> meteors = new List<uint>();
        private List<uint> rocks = new List<uint>();
        private class MeteorData
        {
            public float scale;
            public string prefab;
        }

        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();
            Config.WriteObject(config);
            permission.RegisterPermission("meteorevent.admin", this);
            permission.RegisterPermission(config.showerPurchasePerm, this);
            permission.RegisterPermission(config.directShowerPurchasePerm, this);
            cmd.AddChatCommand(config.command, this, nameof(MeteorShowerChatCommand));
            cmd.AddConsoleCommand(config.command, this, nameof(MeteorShowerConsoleCommand));
            if (!config.meteorFix)
                Unsubscribe(nameof(OnPlayerConnected));
            if (!config.allowExplosiveDamage)
                Unsubscribe(nameof(OnWeaponFired));

        }

        private void OnServerInitialized()
        {
			PrintWarning("\n-----------------------------\n" +
            " Author - Sempai#3239\n" +
            " VK - https://vk.com/rustnastroika/n" +
            " Forum - https://whiteplugins.ru/n" +
            " Discord - https://discord.gg/5DPTsRmd3G/n" +
            "-----------------------------");
            if (config.eventTimer > 0)
            {
                timer.Every(config.eventTimer, () => {
                    if (BasePlayer.activePlayerList.Count == 0)
                        Puts("No online players. Skipping meteor shower.");
                    else if (config.timedEventBasedOnOnline)
                        MeteorShower(Convert.ToInt32(config.eventMeteorCount * BasePlayer.activePlayerList.Count), false, new Vector3(), true);
                    else
                        MeteorShower(Convert.ToInt32(config.eventMeteorCount), false, new Vector3(), true);
                    foreach (var meteor in meteors)
                        BaseNetworkable.serverEntities.Find(meteor)?.Kill();
                });
            }
            if ((config.directShowerAllowPurchasing && config.directShowerItemRequired == "RP" && ServerRewards == null && Economics == null) || (config.showerAllowPurchasing && config.showerItemRequired == "RP" && ServerRewards == null && Economics == null))
                PrintWarning("You've enabled purchasing Meteor Shower event, and set-up RP as requirement, but you don't have Economy plugin! Change RP to other item, or add Economy plugin, or plugin will print errors!");
            if (config.showerAllowPurchasing || config.directShowerAllowPurchasing)
            {
                timer.Once(10, () => {
                    if (purchaseEventCooldown > 0)
                        purchaseEventCooldown -= 10;
                    else if (purchaseEventCooldown < 0) purchaseEventCooldown = 0;
                    foreach (var player in playerPurchaseCooldowns.ToList())
                    {
                        if (playerPurchaseCooldowns[player.Key] > 0) playerPurchaseCooldowns[player.Key] -= 10;
                        else playerPurchaseCooldowns.Remove(player.Key);
                    }
                });
            }
        }

        private void Unload()
        {
            foreach (var meteor in meteors)
                BaseNetworkable.serverEntities.Find(meteor)?.Kill();
        }

        private void OnEntityKill(TimedExplosive explosive)
        {
            if (meteorData.ContainsKey(explosive.net.ID) && explosive.children != null && explosive.children.Count != 0 && explosive.children.Where(x => x.ShortPrefabName == "sphere").FirstOrDefault() != null)
            {
                Vector3 position = explosive.transform.position;
                if (position.y <= 0.5f) return;
                uint netid = explosive.net.ID;
                BaseEntity rock = GameManager.server.CreateEntity($"assets/bundled/prefabs/autospawn/resource/ores/{meteorData[netid].prefab}.prefab", position);
                SphereEntity sphere = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", position) as SphereEntity;
                sphere.Spawn();
                rock.SetParent(sphere);
                rock.Spawn();
                rock.transform.position = position;
                rock.SendNetworkUpdate();
                if (config.markerType.ToLower() == "explosion")
                {
                    MapMarkerExplosion marker = GameManager.server.CreateEntity("assets/prefabs/tools/map/explosionmarker.prefab", position) as MapMarkerExplosion;
                    marker.Spawn();
                    marker.SetParent(sphere);
                    marker.transform.position = position;
                    marker.SendNetworkUpdate();
                }
                else if (config.markerType.ToLower() == "normal")
                {
                    if (config.markerTextEnabled)
                    {
                        VendingMachineMapMarker vendingMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", position) as VendingMachineMapMarker;
                        vendingMarker.markerShopName = config.markerText;
                        vendingMarker.Spawn();
                        vendingMarker.SetParent(sphere);
                        vendingMarker.transform.position = position;
                        vendingMarker.SendNetworkUpdate();
                    }
                    MapMarkerGenericRadius marker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", position) as MapMarkerGenericRadius;
                    marker.alpha = config.markerAlpha;
                    Color color1 = new Color();
                    Color color2 = new Color();
                    ColorUtility.TryParseHtmlString(config.markerColor1, out color1);
                    ColorUtility.TryParseHtmlString(config.markerColor2, out color2);
                    marker.color1 = color1;
                    marker.color2 = color2;
                    marker.radius = config.markerRadius;
                    marker.Spawn();
                    marker.SetParent(sphere);
                    marker.transform.position = position;
                    marker.SendNetworkUpdate();
                    marker.SendUpdate();

                }
                meteors.Add(sphere.net.ID);
                rocks.Add(rock.net.ID);
                if (config.destroyTreesOnImpact)
                {
                    List<TreeEntity> trees = new List<TreeEntity>();
                    Vis.Entities(position, 25, trees);
                    foreach (var tree in trees)
                        tree.OnKilled(new HitInfo() { PointStart = position, PointEnd = tree.transform.position });
                }
                NextTick(() => {
                    sphere.LerpRadiusTo(meteorData[netid].scale, 100000);
                    sphere.SendNetworkUpdate();
                });
            }
            else if (explosive.GetParentEntity() != null && explosive.GetParentEntity().GetParentEntity() != null && explosive.GetParentEntity().GetParentEntity().ShortPrefabName == "sphere" && (explosive.GetParentEntity().GetParentEntity() as SphereEntity).lerpRadius == config.bigMeteorScale)
            {
                OreResourceEntity node = explosive.GetParentEntity() as OreResourceEntity;
                BasePlayer player = BasePlayer.FindByID(explosive.OwnerID);
                if (player == null) return;
                DamageNode(node, player, explosive.ShortPrefabName);
            }
            else if (explosive.GetComponent<ServerProjectile>() != null)
            {
                BasePlayer player = BasePlayer.FindByID(explosive.OwnerID);
                if (player == null) return;
                List<OreResourceEntity> nearbyOres = new List<OreResourceEntity>();
                Vis.Entities(explosive.transform.position, 1, nearbyOres);
                foreach (var node in nearbyOres)
                {
                    if (node.GetParentEntity() == null || node.GetParentEntity().ShortPrefabName != "sphere") continue;
                    if ((node.GetParentEntity() as SphereEntity).lerpRadius != config.bigMeteorScale) return;
                    DamageNode(node, player, explosive.ShortPrefabName);
                }
            }
        }

        private void OnEntityTakeDamage(OreResourceEntity rock, HitInfo info)
        {
            if (info == null) return;
            if (info.WeaponPrefab != null && info.WeaponPrefab.ShortPrefabName.Contains("rocket"))
            {
                info.damageTypes = new DamageTypeList();
                info.HitEntity = null;
                info.DoHitEffects = false;
            }
        }

        private void OnEntityTakeDamage(DecayEntity entity, HitInfo info)
        {
            if (info == null && info.WeaponPrefab == null && !info.WeaponPrefab.ShortPrefabName.Contains("rocket")) return;
            List<OreResourceEntity> rocks = new List<OreResourceEntity>();
            Vis.Entities(entity.transform.position, 5, rocks);
            if (rocks.Count > 0 && rocks[0].skinID == 554765)
            {
                if (!config.damageEntities && config.damageUnownedEntities)
                {
                    if (entity.OwnerID == 0)
                        info.damageTypes.ScaleAll(config.damageMultiplier);
                    else
                    {
                        info.damageTypes = new DamageTypeList();
                        info.HitEntity = null;
                        info.DoHitEffects = false;
                    }
                }
                else
                    info.damageTypes.ScaleAll(config.damageMultiplier);
            }
        }
        private void OnEntityTakeDamage(BasePlayer entity, HitInfo info)
        {
            if (info == null && info.WeaponPrefab == null && !info.WeaponPrefab.ShortPrefabName.Contains("rocket")) return;
            List<OreResourceEntity> rocks = new List<OreResourceEntity>();
            Vis.Entities(entity.transform.position, 5, rocks);
            if (rocks.Count > 0 && rocks[0].skinID == 554765)
            {
                if ((entity.userID > 1000000000 && !config.damagePlayers) || (entity.userID < 1000000000 && !config.damageBots))
                {
                    info.damageTypes = new DamageTypeList();
                    info.HitEntity = null;
                    info.DoHitEffects = false;
                }
            }
        }

        private void OnExplosiveThrown(BasePlayer player, BaseEntity entity) => entity.OwnerID = player.userID;

        private void OnRocketLaunched(BasePlayer player, BaseEntity entity) => entity.OwnerID = player.userID;

        private void OnEntityKill(OreResourceEntity rock)
        {
            if (rock.GetParentEntity() == null || rock.GetParentEntity().ShortPrefabName != "sphere" || rock.GetParentEntity().IsDestroyed) return;
            rock.GetParentEntity().Kill();
        }

        private void OnEntitySpawned(OreResourceEntity node)
        {
            if (node.GetParentEntity() == null || node.GetParentEntity().ShortPrefabName != "sphere") return;
            node.health = 350;
            node.UpdateNetworkStage();
            timer.Once(0.1f, () => {
                node.health = 500;
                node.UpdateNetworkStage();
            });
        }

        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
        {
            if (largeMeteorCount <= 0) return;
            if (projectile.primaryMagazine.ammoType != ItemManager.FindItemDefinition("ammo.rifle.explosive")) return;
            RaycastHit hit;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, config.maxDistanceAmmo))
            {
                OreResourceEntity hitRock = hit.GetCollider().ToBaseEntity() as OreResourceEntity;
                if (hitRock == null) return;
                SphereEntity sphere = hitRock.GetParentEntity() as SphereEntity;
                if (sphere == null) return;
                if (sphere.lerpRadius != config.bigMeteorScale) return;
                DamageNode(hitRock, player, "ammo.rifle.explosive");
            }
        }

        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            OreResourceEntity node = dispenser._baseEntity?.GetComponent<OreResourceEntity>();
            if (node == null || node.GetParentEntity() == null || node.GetParentEntity().ShortPrefabName != "sphere") return;
			SphereEntity sphere = node.GetParentEntity() as SphereEntity;
			if (sphere == null || sphere.lerpRadius == config.bigMeteorScale) return;
            item.amount = (int)(item.amount * config.meteorYieldMultiplier);
            if (config.scaleMeteorYield)
                item.amount = (int)(item.amount * sphere.lerpRadius);
        }

        private object OnMeleeAttack(BasePlayer player, HitInfo info)
        {
            if (player == null || info.HitEntity == null) return null;
            if (info.HitEntity.ShortPrefabName.Contains("-ore") && info.HitEntity.GetParentEntity() != null && info.HitEntity.GetParentEntity().ShortPrefabName == "sphere" && (info.HitEntity.GetParentEntity() as SphereEntity).lerpRadius == config.bigMeteorScale)
            {
                SendReply(player, Lang("NoBigMeteorDig", player.UserIDString));
                return false;
            }
            else if (info.HitEntity.ShortPrefabName.Contains("-ore") && info.HitEntity.GetParentEntity() != null && info.HitEntity.GetParentEntity().ShortPrefabName == "sphere")
            {
                foreach (var bonusItem in config.meteorBonusItems)
                {
                    if (Core.Random.Range(0f, 100f) <= bonusItem.chance)
                    {
                        Item bonus = ItemManager.CreateByName(bonusItem.shortname, bonusItem.amount, bonusItem.skinId);
                        bonus.name = bonusItem.displayName;
                        player.GiveItem(bonus);
                    }
                }
            }
            return null;
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot) || player.IsSleeping())
            {
                timer.Once(2, () => OnPlayerConnected(player));
                return;
            }
            timer.In(1, () => FixMeteors());
        }

        private void DamageNode(OreResourceEntity node, BasePlayer player, string prefabName)
        {
            if (node.IsDestroyed) return;
            if (!config.explosiveConfig.ContainsKey(prefabName)) return;
            float multiplier = 1;
            foreach (var damageConfig in config.explosiveConfig)
            {
                if (damageConfig.Key == prefabName)
                {
                    node.health -= damageConfig.Value.damageDealt;
                    multiplier = damageConfig.Value.resourceMultiplier;
                    break;
                }
            }
            node.UpdateNetworkStage();
            Item item = ItemManager.CreateByName("stones", (int)(config.stonePerHit * multiplier));
            if (node.ShortPrefabName == "metal-ore")
                item = ItemManager.CreateByName("metal.ore", (int)(config.metalPerHit * multiplier));
            else if (node.ShortPrefabName == "sulfur-ore")
                item = ItemManager.CreateByName("sulfur.ore", (int)(config.sulfurPerHit * multiplier));
            Interface.CallHook("OnDispenserGather", node.resourceDispenser, player, item);
            player.GiveItem(item);
            foreach (var bonusItem in config.bigMeteorBonusItems)
            {
                if (Core.Random.Range(0f, 100f) <= bonusItem.chance)
                {
                    Item bonus = ItemManager.CreateByName(bonusItem.shortname, bonusItem.amount, bonusItem.skinId);
                    bonus.name = bonusItem.displayName;
                    player.GiveItem(bonus);
                }
            }
            if (node.health <= 0)
            {
                if (node.ShortPrefabName == "metal-ore")
                {
                    Item hqm = ItemManager.CreateByName("hq.metal.ore", (int)(config.hqmPerHit * multiplier));
                    Interface.CallHook("OnDispenserGather", node.resourceDispenser, player, hqm);
                    player.GiveItem(hqm);
                }
                Effect.server.Run("assets/prefabs/misc/orebonus/effects/ore_finish.prefab", node.transform.position);
                node.Kill();
                largeMeteorCount--;
            }
        }

        private void MeteorShower(int meteorAmount = 20, bool directDrop = false, Vector3 directDropPos = new Vector3(), bool isTimed = false)
        {

            if (isTimed && meteorAmount > config.eventMaxMeteorCount) meteorAmount = config.eventMaxMeteorCount;
            if (directDrop)
                meteorAmount = 1;
            else
                foreach (var onlinePlayer in BasePlayer.activePlayerList)
                {
                    SendReply(onlinePlayer, Lang("ShowerBroadcast", onlinePlayer.UserIDString, meteorAmount));
                    if (config.enableSound)
                        Effect.server.Run(config.soundPrefab, onlinePlayer.transform.position);
                }
            int worldSize = (int)(World.Size / config.meteorSpread);
            for (int i = 0; i < meteorAmount; i++)
            {
                timer.Once(Core.Random.Range(0.1f, 5f), () => {
                    float randX = Core.Random.Range(-worldSize + 200, worldSize + 200);
                    float randZ = Core.Random.Range(-worldSize, worldSize);
                    Vector3 velocity = new Vector3(-12.1f, -13.4f, -0.2f);
                    if (directDrop)
                    {
                        float randPos = Core.Random.Range(-config.impactRadius, config.impactRadius);
                        randX = directDropPos.x + randPos;
                        randZ = directDropPos.z + randPos;
                        velocity = new Vector3(0, -18, 0);
                    }
                    TimedExplosive rocket = GameManager.server.CreateEntity("assets/prefabs/ammo/rocket/rocket_basic.prefab", new Vector3(randX, 450, randZ)) as TimedExplosive;
                    if (config.enableMlrs)
                        rocket = GameManager.server.CreateEntity("assets/content/vehicles/mlrs/rocket_mlrs.prefab", new Vector3(randX, 450, randZ)) as TimedExplosive;
                    BaseEntity rock = GameManager.server.CreateEntity("assets/bundled/prefabs/autospawn/resource/ores/stone-ore.prefab", new Vector3(randX, 450, randZ));
                    int otherOre = Core.Random.Range(0, config.stoneOreChance + config.metalOreChance + config.sulfurOreChance);
                    if (otherOre >= config.stoneOreChance && otherOre < config.stoneOreChance + config.metalOreChance)
                        rock = GameManager.server.CreateEntity("assets/bundled/prefabs/autospawn/resource/ores/metal-ore.prefab", new Vector3(randX, 450, randZ));
                    else if (otherOre >= config.stoneOreChance + config.metalOreChance)
                        rock = GameManager.server.CreateEntity("assets/bundled/prefabs/autospawn/resource/ores/sulfur-ore.prefab", new Vector3(randX, 450, randZ));
                    rock.skinID = 554765;
                    BaseEntity napalm = GameManager.server.CreateEntity("assets/prefabs/npc/m2bradley/oilfireball2.prefab", new Vector3(randX, 450, randZ));
                    SphereEntity sphere = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", new Vector3(randX, 450, randZ)) as SphereEntity;
                    rocket.timerAmountMin = 150;
                    rocket.timerAmountMax = 150;
                    ServerProjectile projectile = rocket.GetComponent<ServerProjectile>();
                    projectile.gravityModifier = Core.Random.Range(0.1f, 0.2f);
                    projectile.InitializeVelocity(velocity);
                    sphere.Spawn();
                    rocket.Spawn();
                    rock.Spawn();
                    napalm.Spawn();
                    float scale = Core.Random.Range(config.meteorScaleMin, config.meteorScaleMax);
                    if (Core.Random.Range(0, 100) < config.bigMeteorChance)
                    {
                        scale = config.bigMeteorScale;
                        largeMeteorCount++;
                    }
                    meteorData.Add(rocket.net.ID, new MeteorData() { prefab = rock.ShortPrefabName, scale = scale });
                    NextTick(() => {
                        sphere.SetParent(rocket);
                        sphere.transform.position = new Vector3(randX, 450, randZ);
                        sphere.SendNetworkUpdate();
                        napalm.SetParent(sphere);
                        napalm.transform.position = new Vector3(randX, 450, randZ);
                        napalm.GetComponent<Rigidbody>().isKinematic = true;
                        napalm.GetComponent<Collider>().enabled = false;
                        napalm.SendNetworkUpdate();
                        rock.SetParent(sphere);
                        rock.transform.position = new Vector3(randX, 450, randZ);
                        rock.SendNetworkUpdate();
                        sphere.LerpRadiusTo(scale, 100000);
                        sphere.SendNetworkUpdate();
                    });
                });
            }
        }

        private void FixMeteors()
        {
            foreach (var meteor in rocks)
            {
                OreResourceEntity mEntity = BaseNetworkable.serverEntities.Find(meteor) as OreResourceEntity;
                if (mEntity == null) continue;
                float health = mEntity.health;
                mEntity.health = 350;
                mEntity.UpdateNetworkStage();
                timer.Once(0.1f, () => {
                    mEntity.health = health;
                    mEntity.UpdateNetworkStage();
                });
            }
        }

        private void MeteorShowerChatCommand(BasePlayer player, string command, string[] args)
        {
            string item1 = ItemManager.FindItemDefinition(config.showerItemRequired)?.displayName?.english;
            string item2 = ItemManager.FindItemDefinition(config.directShowerItemRequired)?.displayName?.english;
            if (item1 == null)
                item1 = Lang("Money", player.UserIDString);
            if (item2 == null)
                item2 = Lang("Money", player.UserIDString);
            if (args.Length == 0)
            {
                if (permission.UserHasPermission(player.UserIDString, "meteorevent.admin"))
                    SendReply(player, Lang("MeteorHelp", player.UserIDString, config.command));
                else
                    SendReply(player, Lang("MeteorHelpUser", player.UserIDString, config.command, config.showerMeteorAmount, config.showerItemAmount, item1, config.directShowerItemAmount, item2));
                return;
            }
            else if (args.Length > 0 && args.Length <= 3)
            {
                if (args[0].ToLower() == "run")
                {
                    if (!permission.UserHasPermission(player.UserIDString, "meteorevent.admin"))
                    {
                        SendReply(player, Lang("NoPermission", player.UserIDString));
                        return;
                    }
                    int amount = 0;
                    if (args.Length == 2 && int.TryParse(args[1], out amount))
                    {
                        MeteorShower(amount);
                        SendReply(player, Lang("MeteorInfo", player.UserIDString, amount));
                    }
                    else
                        SendReply(player, Lang("WrongSyntax_1", player.UserIDString, config.command));
                }
                else if (args[0].ToLower() == "direct")
                {
                    if (!permission.UserHasPermission(player.UserIDString, "meteorevent.admin"))
                    {
                        SendReply(player, Lang("NoPermission", player.UserIDString));
                        return;
                    }
                    if (args.Length == 2)
                    {
                        BasePlayer directPlayer = IsValidPlayer(player, args[1]);
                        if (directPlayer != null)
                        {
                            SendReply(player, Lang("DirectMeteorInfo", player.UserIDString, directPlayer.displayName));
                            MeteorShower(1, true, directPlayer.transform.position);
                        }
                    }
                    else
                        SendReply(player, Lang("WrongSyntax_2", player.UserIDString, config.command));
                }
                else if (args[0].ToLower() == "kill")
                {
                    if (!permission.UserHasPermission(player.UserIDString, "meteorevent.admin"))
                    {
                        SendReply(player, Lang("NoPermission", player.UserIDString));
                        return;
                    }
                    SendReply(player, Lang("MeteorsKilled", player.UserIDString));
                    foreach (var meteor in meteors)
                        BaseNetworkable.serverEntities.Find(meteor)?.Kill();
                }
                else if (args[0].ToLower() == "buy")
                {
                    if (args.Length == 1)
                    {
                        SendReply(player, Lang("MeteorHelpUser", player.UserIDString, config.command, config.showerMeteorAmount, config.showerItemAmount, item1, config.directShowerItemAmount, item2));
                        return;
                    }
                    if (args.Length == 2 && args[1].ToLower() == "normal")
                    {
                        if (!permission.UserHasPermission(player.UserIDString, config.showerPurchasePerm))
                        {
                            SendReply(player, Lang("NoPermission", player.UserIDString));
                            return;
                        }
                        if (!config.showerAllowPurchasing)
                        {
                            SendReply(player, Lang("OptionDisabled", player.UserIDString));
                            return;
                        }
                        if (purchaseEventCooldown > 0)
                        {
                            SendReply(player, Lang("EventOnCooldown", player.UserIDString, purchaseEventCooldown));
                            return;
                        }
                        if (config.showerItemRequired == "RP")
                        {
                            if (config.usedEconomyPlugin.ToLower() == "serverrewards")
                            {
                                if (ServerRewards.Call<int>("CheckPoints", player.userID) >= config.showerItemAmount)
                                {
                                    ServerRewards.Call("TakePoints", player.userID, config.showerItemAmount);
                                    SendReply(player, Lang("PurchasedEvent", player.UserIDString, Lang("Money", player.UserIDString), config.showerItemAmount));
                                    purchaseEventCooldown = config.showerCooldown;
                                    MeteorShower(config.showerMeteorAmount);
                                }
                                else
                                    SendReply(player, Lang("NoRequiredItem", player.UserIDString, Lang("Money", player.UserIDString), config.showerItemAmount));
                            }
                            else if (config.usedEconomyPlugin.ToLower() == "economics")
                            {
                                if (Economics.Call<double>("Balance", player.userID) >= config.showerItemAmount)
                                {
                                    Economics.Call("Withdraw", player.userID, Convert.ToDouble(config.showerItemAmount));
                                    SendReply(player, Lang("PurchasedEvent", player.UserIDString, Lang("Money", player.UserIDString), config.showerItemAmount));
                                    purchaseEventCooldown = config.showerCooldown;
                                    MeteorShower(config.showerMeteorAmount);
                                }
                                else
                                    SendReply(player, Lang("NoRequiredItem", player.UserIDString, Lang("Money", player.UserIDString), config.showerItemAmount));
                            }
                        }
                        else
                        {
                            ItemDefinition item = ItemManager.FindItemDefinition(config.showerItemRequired);
                            if (TakeItem(player, config.showerItemRequired, config.showerItemAmount))
                            {
                                SendReply(player, Lang("PurchasedEvent", player.UserIDString, item.displayName.english, $"x{config.showerItemAmount}"));
                                purchaseEventCooldown = config.showerCooldown;
                                MeteorShower(config.showerMeteorAmount);
                            }
                            else
                                SendReply(player, Lang("NoRequiredItem", player.UserIDString, item.displayName.english, $"x{config.showerItemAmount}"));
                        }
                    }
                    else if (args.Length == 3 && args[1].ToLower() == "direct")
                    {
                        if (!permission.UserHasPermission(player.UserIDString, config.directShowerPurchasePerm))
                        {
                            SendReply(player, Lang("NoPermission", player.UserIDString));
                            return;
                        }
                        if (!config.directShowerAllowPurchasing)
                        {
                            SendReply(player, Lang("OptionDisabled", player.UserIDString));
                            return;
                        }
                        if (playerPurchaseCooldowns.ContainsKey(player.userID) && playerPurchaseCooldowns[player.userID] > 0)
                        {
                            SendReply(player, Lang("EventOnCooldown", player.UserIDString, playerPurchaseCooldowns[player.userID]));
                            return;
                        }
                        BasePlayer directPlayer = IsValidPlayer(player, args[2]);
                        if (directPlayer == null) return;
                        if (!config.directShowerAllowOthers && directPlayer != player)
                        {
                            SendReply(player, Lang("BuyDirectOnlyYou", player.UserIDString));
                            return;
                        }
                        if (config.directShowerItemRequired == "RP")
                        {
                            if (config.usedEconomyPlugin.ToLower() == "serverrewards")
                            {
                                if (ServerRewards.Call<int>("CheckPoints", player.userID) >= config.directShowerItemAmount)
                                {
                                    ServerRewards.Call("TakePoints", player.userID, config.directShowerItemAmount);
                                    SendReply(player, Lang("PurchasedDirectEvent", player.UserIDString, Lang("Money", player.UserIDString), config.directShowerItemAmount, directPlayer.displayName));
                                    playerPurchaseCooldowns.Add(player.userID, config.directShowerCooldown);
                                    MeteorShower(1, true, directPlayer.transform.position);
                                }
                                else
                                    SendReply(player, Lang("NoRequiredItem", player.UserIDString, Lang("Money", player.UserIDString), config.directShowerItemAmount));
                            }
                            else if (config.usedEconomyPlugin.ToLower() == "economics")
                            {
                                if (Economics.Call<double>("Balance", player.userID) >= config.directShowerItemAmount)
                                {
                                    Economics.Call("Withdraw", player.userID, Convert.ToDouble(config.directShowerItemAmount));
                                    SendReply(player, Lang("PurchasedDirectEvent", player.UserIDString, Lang("Money", player.UserIDString), config.directShowerItemAmount, directPlayer.displayName));
                                    playerPurchaseCooldowns.Add(player.userID, config.directShowerCooldown);
                                    MeteorShower(1, true, directPlayer.transform.position);
                                }
                                else
                                    SendReply(player, Lang("NoRequiredItem", player.UserIDString, Lang("Money", player.UserIDString), config.directShowerItemAmount));
                            }
                        }
                        else
                        {
                            ItemDefinition item = ItemManager.FindItemDefinition(config.directShowerItemRequired);
                            if (TakeItem(player, config.directShowerItemRequired, config.directShowerItemAmount))
                            {
                                SendReply(player, Lang("PurchasedDirectEvent", player.UserIDString, item.displayName.english, $"x{config.directShowerItemAmount}", directPlayer.displayName));
                                playerPurchaseCooldowns.Add(player.userID, config.directShowerCooldown);
                                MeteorShower(1, true, directPlayer.transform.position);
                            }
                            else
                                SendReply(player, Lang("NoRequiredItem", player.UserIDString, item.displayName.english, $"x{config.directShowerItemAmount}"));
                        }
                    }
                    else
                        SendReply(player, Lang("MeteorHelpUser", player.UserIDString, config.command, config.showerMeteorAmount, config.showerItemAmount, item1, config.directShowerItemAmount, item2));
                }
                else
                {
                    if (permission.UserHasPermission(player.UserIDString, "meteorevent.admin"))
                        SendReply(player, Lang("MeteorHelp", player.UserIDString, config.command));
                    else
                        SendReply(player, Lang("MeteorHelpUser", player.UserIDString, config.command, config.showerMeteorAmount, config.showerItemAmount, item1, config.directShowerItemAmount, item2));
                }
            }
            else
            {
                if (permission.UserHasPermission(player.UserIDString, "meteorevent.admin"))
                    SendReply(player, Lang("MeteorHelp", player.UserIDString, config.command));
                else
                    SendReply(player, Lang("MeteorHelpUser", player.UserIDString, config.command, config.showerMeteorAmount, config.showerItemAmount, item1, config.directShowerItemAmount, item2));
            }
        }

        private void MeteorShowerConsoleCommand(ConsoleSystem.Arg arg)
        {
            if ((arg.Player() != null && !permission.UserHasPermission(arg.Player().UserIDString, "meteorevent.admin")) || (arg.Player() == null && !arg.IsAdmin))
            {
                SendReply(arg, Lang("NoPermission"));
                return;
            }
            if (arg.Args == null)
            {
                SendReply(arg, Lang("MeteorHelp", null, config.command));
                return;
            }
            else if (arg.Args != null && arg.Args.Length <= 2)
            {
                if (arg.Args[0].ToLower() == "run")
                {
                    int amount = 0;
                    if (arg.Args.Length == 2 && int.TryParse(arg.Args[1], out amount))
                    {
                        MeteorShower(amount);
                        SendReply(arg, Lang("MeteorInfo", null, amount));
                    }
                    else
                        SendReply(arg, Lang("WrongSyntax_1", null, config.command));
                }
                else if (arg.Args[0].ToLower() == "direct")
                {
                    if (arg.Args.Length == 2)
                    {
                        ulong playerId = 0;
                        if (ulong.TryParse(arg.Args[1], out playerId))
                        {
                            BasePlayer directPlayer = BasePlayer.FindByID(playerId);
                            if (directPlayer == null)
                            {
                                SendReply(arg, Lang("NoPlayerFound", null, playerId));
                                return;
                            }
                            MeteorShower(1, true, directPlayer.transform.position);
                            SendReply(arg, Lang("DirectMeteorInfo", null, directPlayer.displayName));
                            return;
                        }
                        int playerCount = BasePlayer.activePlayerList.Where(x => x.displayName.Contains(arg.Args[1])).Count();
                        if (playerCount > 1)
                        {
                            SendReply(arg, Lang("SpecifyNickname", null, arg.Args[1]));
                            return;
                        }
                        else if (playerCount == 0)
                        {
                            SendReply(arg, Lang("NoPlayerFound", null, arg.Args[1]));
                            return;
                        }
                        BasePlayer idDirectPlayer = BasePlayer.activePlayerList.Where(x => x.displayName.Contains(arg.Args[1])).FirstOrDefault();
                        if (idDirectPlayer != null)
                        {
                            MeteorShower(1, true, idDirectPlayer.transform.position);
                            SendReply(arg, Lang("DirectMeteorInfo", null, idDirectPlayer.displayName));
                        }
                        else
                            SendReply(arg, Lang("NoPlayerFound", null, arg.Args[1]));
                    }
                    else
                        SendReply(arg, Lang("WrongSyntax_2", null, config.command));
                }
                else if (arg.Args[0].ToLower() == "kill")
                {
                    SendReply(arg, Lang("MeteorsKilled"));
                    foreach (var meteor in meteors)
                        BaseNetworkable.serverEntities.Find(meteor)?.Kill();
                }
                else
                    SendReply(arg, Lang("MeteorHelp", null, config.command));
            }
            else
                SendReply(arg, Lang("MeteorHelp", null, config.command));
        }

        private BasePlayer IsValidPlayer(BasePlayer player, string id)
        {
            ulong playerId = 0;
            if (ulong.TryParse(id, out playerId))
            {
                BasePlayer directPlayer = BasePlayer.FindByID(playerId);
                if (directPlayer == null)
                {
                    SendReply(player, Lang("NoPlayerFound", player.UserIDString, playerId));
                    return null;
                }
                return directPlayer;
            }
            int playerCount = BasePlayer.activePlayerList.Where(x => x.displayName.Contains(id)).Count();
            if (playerCount > 1)
            {
                SendReply(player, Lang("SpecifyNickname", player.UserIDString, id));
                return null;
            }
            else if (playerCount == 0)
            {
                SendReply(player, Lang("NoPlayerFound", player.UserIDString, id));
                return null;
            }
            BasePlayer idDirectPlayer = BasePlayer.activePlayerList.Where(x => x.displayName.Contains(id)).FirstOrDefault();
            if (idDirectPlayer != null)
                return idDirectPlayer;
            else
                SendReply(player, Lang("NoPlayerFound", player.UserIDString, id));
            return null;
        }

        private bool TakeItem(BasePlayer player, string shortname, int amount)
        {
            bool haveRequired = false;
            int inventoryAmount = 0;
            foreach (var item in player.inventory.AllItems())
            {
                if (item.info.shortname == shortname)
                {
                    inventoryAmount += item.amount;
                    if (inventoryAmount >= amount)
                    {
                        haveRequired = true;
                        break;
                    }
                }
            }
            if (!haveRequired)
                return false;
            int takenItems = 0;
            foreach (var item in player.inventory.AllItems())
            {
                if (item.info.shortname == shortname)
                {
                    if (takenItems < amount)
                    {
                        if (item.amount > amount - takenItems)
                        {
                            item.amount -= amount - takenItems;
                            item.MarkDirty();
                            break;
                        }
                        if (item.amount <= amount - takenItems)
                        {
                            takenItems += item.amount;
                            item.GetHeldEntity()?.Kill();
                            item.Remove();
                        }
                    }
                    else break;
                }
            }
            return true;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You don't have permission to use this command!",
                ["MeteorHelp"] = "Meteor Shower Commands:\n<color=#5c81ed>/{0} run <amount></color> - Starts meteor shower with <amount> meteors.\n<color=#5c81ed>/{0} direct <playerIdOrName></color> - Strikes one meteor to <playerIdOrName> location.\n<color=#5c81ed>/{0} kill</color> - Kills all meteors around the map.",
                ["WrongSyntax_1"] = "Wrong Syntax!\nUsage: <color=#5c81ed>/{0} run <amount></color>",
                ["WrongSyntax_2"] = "Wrong Syntax!\nUsage: <color=#5c81ed>/{0} direct <playerIdOrName></color>",
                ["MeteorInfo"] = "You've started a meteor shower with <color=#5c81ed>{0}</color> meteors!",
                ["DirectMeteorInfo"] = "You've directed one meteor to <color=#5c81ed>{0}'s</color> head!",
                ["NoPlayerFound"] = "We couldn't find player <color=#5c81ed>{0}</color>.",
                ["SpecifyNickname"] = "Write more specified nickname. We found more than 1 player with a nickname <color=#5c81ed>{0}</color>.",
                ["ShowerBroadcast"] = "Astrologers announced that a meteor shower would occur in a few seconds!\nThere will be approx. <color=#5c81ed>{0}</color> falling meteorites!",
                ["MeteorsKilled"] = "You've killed all meteors around the map!",
                ["NoBigMeteorDig"] = "You can't destroy large meteorite with melee tool. You need to use explosives!",
                ["MeteorHelpUser"] = "Meteor Shower Commands:\n<color=#5c81ed>/{0} buy normal </color> - Purchase meteor shower with {1} meteors for {2} {3}.\n<color=#5c81ed>/{0} buy direct <playerIdOrName></color> - Purchase meteor strike on <playerIdOrName> location for {4} {5}.",
                ["OptionDisabled"] = "This option is disabled!",
                ["EventOnCooldown"] = "Event is on cooldown! You need to wait <color=#5c81ed>{0}</color> seconds to start another event.",
                ["PurchasedEvent"] = "You've purchased Meteor Shower for <color=#5c81ed>{1} {0}</color>!",
                ["NoRequiredItem"] = "You don't have required item to purchase an event! You need <color=#5c81ed>{1} {0}</color>!",
                ["BuyDirectOnlyYou"] = "You can buy direct drops only on yourself!",
                ["PurchasedDirectEvent"] = "You've purchased direct Meteor Shower for <color=#5c81ed>{1} {0}</color>!",
                ["Money"] = "RP",
            }, this);
        }

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new PluginConfig(), true);
            NextTick(() => {
                config = Config.ReadObject<PluginConfig>();
                config.meteorBonusItems = new List<ItemConfig>()
                {
                    new ItemConfig() { shortname = "charcoal", amount = 150 },
                    new ItemConfig() { shortname = "wood", amount = 500 }
                };
                config.bigMeteorBonusItems = new List<ItemConfig>()
                {
                    new ItemConfig() { shortname = "charcoal", amount = 150 },
                    new ItemConfig() { shortname = "wood", amount = 500 }
                };
                config.explosiveConfig = new Dictionary<string, ExplosiveConfig>()
                {
                    { "explosive.timed.deployed", new ExplosiveConfig() },
                    { "rocket_basic", new ExplosiveConfig() },
                    { "ammo.rifle.explosive", new ExplosiveConfig() }
                };
                Config.WriteObject(config);
            });
        }

        private class PluginConfig
        {
            [JsonProperty("Command - Chat and Console Command")]
            public string command = "ms";

            [JsonProperty("Command - Used Economy Plugin (ServerRewards/Economics)")]
            public string usedEconomyPlugin = "ServerRewards";

            [JsonProperty("Command - Allow Purchasing Meteor Shower")]
            public bool showerAllowPurchasing = false;

            [JsonProperty("Command - Purchase Meteor Shower Permission")]
            public string showerPurchasePerm = "meteorevent.canshower";

            [JsonProperty("Command - Purchased Meteor Shower Meteors Amount")]
            public int showerMeteorAmount = 10;

            [JsonProperty("Command - Item/RP Required To Purchase Meteor Shower")]
            public string showerItemRequired = "scrap";

            [JsonProperty("Command - Amount/Price For Purchasing Meteor Shower")]
            public int showerItemAmount = 1000;

            [JsonProperty("Command - Cooldown After Purchasing Meteor Shower (in seconds)")]
            public int showerCooldown = 3600;

            [JsonProperty("Command - Allow Purchasing Direct Meteor Shower")]
            public bool directShowerAllowPurchasing = false;

            [JsonProperty("Command - Allow Purchasing Direct Meteor Shower At Other Players")]
            public bool directShowerAllowOthers = false;

            [JsonProperty("Command - Purchase Direct Meteor Shower Permission")]
            public string directShowerPurchasePerm = "meteorevent.candirectshower";

            [JsonProperty("Command - Item/RP Required To Purchase Direct Meteor Shower")]
            public string directShowerItemRequired = "scrap";

            [JsonProperty("Command - Amount/Price For Purchasing Direct Meteor Shower")]
            public int directShowerItemAmount = 250;

            [JsonProperty("Command - Cooldown After Purchasing Direct Meteor Shower (in seconds)")]
            public int directShowerCooldown = 3600;

            [JsonProperty("Marker - Map Marker Type (None/Normal/Explosion)")]
            public string markerType = "Explosion";

            [JsonProperty("Marker - Map Marker Alpha (Normal Only)")]
            public float markerAlpha = 0.75f;

            [JsonProperty("Marker - Map Marker Color #1 (Normal Only)")]
            public string markerColor1 = "#E01300";

            [JsonProperty("Marker - Map Marker Color #2 (Normal Only)")]
            public string markerColor2 = "#7D0B00";

            [JsonProperty("Marker - Map Marker Radius (Normal Only)")]
            public float markerRadius = 0.4f;

            [JsonProperty("Marker - Enable Map Marker Text (Normal Only)")]
            public bool markerTextEnabled = true;

            [JsonProperty("Marker - Map Marker Text (Normal Only)")]
            public string markerText = "Meteor Debris";

            [JsonProperty("Event Timer - Event Every X Seconds (0 to disable)")]
            public int eventTimer = 1800;

            [JsonProperty("Event Timer - Meteor Amount Based On Player Count")]
            public bool timedEventBasedOnOnline = true;

            [JsonProperty("Event Timer - Meteor Count (if based on player count - per player)")]
            public float eventMeteorCount = 4;

            [JsonProperty("Event Timer - Max Meteor Count")]
            public int eventMaxMeteorCount = 40;

            [JsonProperty("Direct Meteor - Randomized Impact Radius")]
            public float impactRadius = 1;

            [JsonProperty("All Meteors - MLRS As Sound Effect")]
            public bool enableMlrs = false;

            [JsonProperty("All Meteors - Destory Trees On Impact")]
            public bool destroyTreesOnImpact = true;

            [JsonProperty("All Meteors - Damage Entities")]
            public bool damageEntities = true;

            [JsonProperty("All Meteors - Damage Only Unowned Entities")]
            public bool damageUnownedEntities = false;

            [JsonProperty("All Meteors - Damage Players")]
            public bool damagePlayers = true;

            [JsonProperty("All Meteors - Damage Bots")]
            public bool damageBots = true;

            [JsonProperty("All Meteors - Damage Multiplier")]
            public float damageMultiplier = 4;

            [JsonProperty("All Meteors - Stone Ore Chance")]
            public int stoneOreChance = 50;

            [JsonProperty("All Meteors - Metal Ore Chance")]
            public int metalOreChance = 50;

            [JsonProperty("All Meteors - Sulfur Ore Chance")]
            public int sulfurOreChance = 50;

            [JsonProperty("All Meteors - Meteor Spread On Map (higher = smaller impact radius)")]
            public float meteorSpread = 2.5f;

            [JsonProperty("All Meteors - Enable Invisible Meteor Fix")]
            public bool meteorFix = true;

            [JsonProperty("All Meteors - Enable Sound Effect")]
            public bool enableSound = true;

            [JsonProperty("All Meteors - Sound Effect Prefab")]
            public string soundPrefab = "assets/prefabs/tools/pager/effects/beep.prefab";

            [JsonProperty("Normal Meteor - Maximum Scale")]
            public float meteorScaleMax = 2;

            [JsonProperty("Normal Meteor - Minimum Scale")]
            public float meteorScaleMin = 0.5f;

            [JsonProperty("Normal Meteor - Yield Multiplier")]
            public float meteorYieldMultiplier = 2;

            [JsonProperty("Normal Meteor - Scale Yield By Size")]
            public bool scaleMeteorYield = true;

            [JsonProperty("Big Meteor - Chance (0-100)")]
            public int bigMeteorChance = 10;

            [JsonProperty("Big Meteor - Scale")]
            public float bigMeteorScale = 7;

            [JsonProperty("Big Meteor - Stone Yield Per Hit")]
            public int stonePerHit = 1000;

            [JsonProperty("Big Meteor - Metal Yield Per Hit")]
            public int metalPerHit = 500;

            [JsonProperty("Big Meteor - Sulfur Yield Per Hit")]
            public int sulfurPerHit = 250;

            [JsonProperty("Big Meteor - HQM Yield Per Hit")]
            public int hqmPerHit = 10;

            [JsonProperty("Big Meteor - Explosive Config")]
            public Dictionary<string, ExplosiveConfig> explosiveConfig = new Dictionary<string, ExplosiveConfig>();

            [JsonProperty("Normal Meteor - Bonus Items")]
            public List<ItemConfig> meteorBonusItems = new List<ItemConfig>();

            [JsonProperty("Big Meteor - Bonus Items")]
            public List<ItemConfig> bigMeteorBonusItems = new List<ItemConfig>();

            [JsonProperty("Big Meteor - Explosive Ammo - Allow")]
            public bool allowExplosiveDamage = true;

            [JsonProperty("Big Meteor - Explosive Ammo - Max Distance From Rock")]
            public float maxDistanceAmmo = 50;
        }

        private class ExplosiveConfig
        {
            [JsonProperty("Damage Dealt")]
            public float damageDealt = 50;

            [JsonProperty("Resource Multiplier")]
            public float resourceMultiplier = 1;
        }

        private class ItemConfig
        {
            [JsonProperty("Chance (0-100)")]
            public float chance = 2.5f;

            [JsonProperty("Shortname")]
            public string shortname;

            [JsonProperty("Amount")]
            public int amount = 1;

            [JsonProperty("Skin ID")]
            public ulong skinId = 0;

            [JsonProperty("Display Name")]
            public string displayName = "";
        }
    }
