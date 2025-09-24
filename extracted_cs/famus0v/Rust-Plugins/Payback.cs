using Oxide.Core;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using System;
using System.Linq;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Oxide.Core.Libraries.Covalence;
using Rust;

namespace Oxide.Plugins
{
    [Info("Payback", "1928Tommygun/Kira", "1.9.9")]
    [Description("Special Admin Commands To Mess With Cheaters")]
    public class Payback : RustPlugin
    {
#pragma warning disable CS0649
        [PluginReference] private Plugin Payback2;
#pragma warning restore CS0649

        private enum Card   
        { 
            Pacifism = 0, //zeros all outgoing damage from target player
            Butterfingers = 1, //on dealing damage to any player, chance to drop current held weapon.
            InstantKarma = 4, //reflects damage back to the player
            Dud = 5, //prevents damage to non-player entities
            DogDoo = 6, //landmine under target player when the access stashes
            BSOD = 7, //give target player fake BSOD
            Sit = 8, //force player to sit
            Naked = 9, //force player to drop everything they have in their inventory
            Camomo = 10, //apply a combination of abilities Camomo has selected
            HigherGround = 11, //target player is teleported 100m into the air
            Thirsty = 12, //target player becomes thirsty very quickly
            DrNo = 13, // target player no longer receives health from healing items
            Dana = 14, // steal target player's inventory and place it in your own
            Pinyata = 15, // target player's inventory explodes out of them when they die
            Rocketman, // strap target player to a rocket and launch them!
            NoRest, // No Rest For The Wicked! Force the player to respawn!
            ChickenChaser, // Spawns a horde of chickens with super speed that only attack target player!
            ViewLoot, // View target entities loot
            Burn, // Gives a player a flamethrower that will make his foes scream and burn
            Hammer, // hammer - gives target player a hammer that will destroy all the entities owned by the hammer's target
            Bag, // Bag - Print all players that have bagged target player in, and print all players that have been bagged.  Include "discord" after the command to log the results to discord
            Shocker, // Shocker - Shock target player to death.  Affects nearby players so be careful.  Make sure to disable it after use.

            //Cowboy, // Cowboy - Ride target player like a wild hog -> couldn't get this to work
            Masochist, // Masochist - Stop player from F1 killing themselves
            Emote, // Emote - options: 
            Shark, // Jaws - shark comes to eat the cheater
        }

        private Dictionary<Card, string> descriptions = new Dictionary<Card, string>()
        {
            {Card.Butterfingers, "% chance for target player to drop their weapon when damaging an enemy"},
            {Card.Dud, "target player deals no damage to NON-PLAYER entities.  Also prevents farming / tool use"},
            {
                Card.InstantKarma,
                "target player deals no damage to enemies and 35% of the damage is reflected back to them"
            },
            {
                Card.Pacifism,
                "target player deals no player damage to non-teammates; add 'silent' to not send a message about it to other players."
            },
            {Card.DogDoo, "landmine under target player when the access stashes"},
            {Card.BSOD, "target player receives a fake blue-screen-of-death"},
            {
                Card.Sit,
                "spawns a chair in front of you and forces the cheater to sit.  Doesn't let them get up and will place them back in if they die."
            },
            {Card.Naked, "force player to drop everything they have in their inventory"},
            {Card.Camomo, "apply a combination of abilities Camomo has selected [pf,bf,in,dog,dud,dr]"},
            {Card.HigherGround, "target player is teleported 100m into the air"},
            {Card.Thirsty, "target player becomes thirsty very quickly"},
            {Card.DrNo, "target player can no longer heal"},
            {Card.Dana, "steal target player's inventory and place it in your own"},
            {Card.Pinyata, "target player's inventory explodes out of them when they die"},
            {Card.Rocketman, "strap target player to a rocket and launch them!"},
            {Card.NoRest, "No Rest For The Wicked! Force the player to respawn when they die!"},
            {
                Card.ChickenChaser,
                "Spawns a horde of chickens with super speed that only attack target player! add 'wolf' 'stag' 'bear' or 'boar' after the command to change the animal"
            },
            {Card.ViewLoot, "View target player's loot"},
            {Card.Burn, "Gives a player a flamethrower that will make his foes scream and burn"},
            {
                Card.Hammer,
                "Gives admin a hammer that will destroy all the entities owned by the hammer's target.  Add -noloot to also delete the loot"
            },
            {
                Card.Bag,
                "Print all players that have bagged target player in, and print all players that have been bagged.  Include \"discord\" after the command to log the results to discord"
            },
            {
                Card.Shocker,
                "Shock target player to death.  Affects nearby players so be careful.  Make sure to disable it after use."
            },
            //{Card.Cowboy, "Cowboy - Ride target player like a wild hog" },
            {Card.Masochist, "Masochist - Stop player from F1 killing themselves"},
            {Card.Emote, "Emote - force player to do an emote. "},
            {Card.Shark, "shark comes to eat the cheater"},
        };

        Dictionary<string, Card> cardAliases = new Dictionary<string, Card>()
        {
            {"bf", Card.Butterfingers},
            {"dud", Card.Dud},
            {"in", Card.InstantKarma},
            {"pf", Card.Pacifism},
            {"dog", Card.DogDoo},
            {"bs", Card.BSOD},
            {"nk", Card.Naked},
            {"cumnum", Card.Camomo},
            {"hg", Card.HigherGround},
            {"th", Card.Thirsty},
            {"dr", Card.DrNo},
            {"dana", Card.Dana},
            {"steal", Card.Dana},
            {"pin", Card.Pinyata},
            {"rm", Card.Rocketman},
            {"nr", Card.NoRest},
            {"res", Card.NoRest},
            {"ch", Card.ChickenChaser},
            {"loot", Card.ViewLoot},
            {"bu", Card.Burn},
            {"ham", Card.Hammer},
            {"bg", Card.Bag},
            {"sh", Card.Shocker},
            //{ "cow", Card.Cowboy},
            {"ms", Card.Masochist},
            {"em", Card.Emote},
            {"jaws", Card.Shark},
        };

        private void GiveCard(ulong userID, Card card, string[] args = null, BasePlayer admin = null)
        {
            HashSet<Card> cards;
            if (!cardMap.TryGetValue(userID, out cards))
            {
                cards = new HashSet<Card>();
                cardMap[userID] = cards;
            }

            cards.Add(card);

            var player = BasePlayer.FindByID(userID);
            if (player == null) return;
            switch (card)
            {
                case Card.BSOD:
                {
                    var playPublic = args.Contains("public");
                    DoBSOD(player, playPublic);
                    break;
                }
                case Card.Sit:
                    DoSitCommand(player, admin);
                    break;
                case Card.Naked:
                    DoNakedCommand(player);
                    break;
                case Card.Camomo:
                    DoCamomoCommand(player);
                    break;
                case Card.HigherGround:
                    DoHigherGround(player);
                    break;
                case Card.Thirsty:
                    DoThirsty(player);
                    break;
                case Card.Dana:
                    DoDana(player, admin);
                    break;
                case Card.Pacifism:
                {
                    silentPacifism = false;
                    if (args != null)
                        if (args.Contains("silent"))
                            silentPacifism = true;
                    break;
                }
                case Card.Rocketman:
                    RocketManTarget(player);
                    break;
                case Card.NoRest:
                {
                    if (player.IsDead()) player.Respawn();
                    break;
                }
                case Card.ChickenChaser:
                    AdminSpawnChickens(admin, player, args);
                    TakeCard(player, Card.ChickenChaser);
                    break;
                case Card.ViewLoot:
                    ViewTargetPlayerInventory(player, admin);
                    TakeCard(player, Card.ViewLoot);
                    break;
                case Card.Burn:
                    GivePlayerFlamethrower(player);
                    break;
                case Card.Hammer:
                {
                    GiveAdminHammer(player);
                    if (args.Contains("noloot"))
                    {
                        flag_kill_no_loot = true;
                        PrintToPlayer(admin, $"Hammer set to remove loot!");
                    }
                    else flag_kill_no_loot = false;

                    break;
                }
                case Card.Shocker:
                    DoShocker(player, args, admin);
                    break;
                case Card.Emote:
                    DoEmote(player, args, admin);
                    break;
                case Card.Shark:
                    DoShark(player, args, admin);
                    break;
            }
        }

        private bool silentPacifism;

        private void DoShark(BasePlayer player, string[] args, BasePlayer admin = null)
        {
            Worker.StaticStartCoroutine(SharkCo2(player, args, admin));
        }

        // ReSharper disable once UnusedParameter.Local
        private IEnumerator SharkCo2(BasePlayer player, string[] args, BasePlayer admin = null)
        {
            TakeCard(player, Card.Shark);
            SimpleShark shark;
            const string sharkPrefab = "assets/rust.ai/agents/fish/simpleshark.prefab";

            var position = player.transform.position;
            var entity = GameManager.server.CreateEntity(sharkPrefab,
                position + new Vector3(-100, -100, -100));

            shark = entity as SimpleShark;
            entity.Spawn();

            shark.enabled = false;

            var playerForward = player.eyes.HeadForward();
            playerForward.y = 0;
            playerForward.Normalize();

            shark.transform.LookAt(position + Vector3.up * 100 + playerForward);

            yield return Worker.StaticStartCoroutine(MoveSharkCo(shark, player));
        }

        private const string sfx_watersplash = "assets/bundled/prefabs/fx/explosions/water_bomb.prefab";

        private IEnumerator MoveSharkCo(SimpleShark shark, BasePlayer player)
        {
            const float duration = 4f;
            var ts = Time.realtimeSinceStartup;
            var transform = player.transform;
            var position = transform.position;
            var sharkStartPos = position + Vector3.down * 30f;
            var playerStartPosition = position;
            var playerForward = player.eyes.HeadForward();
            playerForward.y = 0;
            playerForward.Normalize();

            var didSit = false;
            BaseEntity chair = null;
            float p;
            float y;
            while (Time.realtimeSinceStartup - ts < duration && player != null && shark != null)
            {
                p = (Time.realtimeSinceStartup - ts) / duration;
                y = Mathf.Sin(p * Mathf.PI);
                if (p < 0.24)
                {
                    var transform1 = player.transform;
                    var position1 = transform1.position;
                    playerStartPosition = position1 + Vector3.up * 10f;
                    shark.transform.LookAt(position1 + Vector3.up * 100 + playerForward);
                }
                else
                {
                    if (!didSit)
                    {
                        var position1 = player.transform.position;
                        Effect.server.Run(shark.bloodCloud.resourcePath, position1 + Vector3.up, Vector3.forward);
                        Effect.server.Run(sfx_watersplash, position1 + Vector3.up, Vector3.forward);

                        timer.Once(0.2f, () =>
                        {
                            if (player == null) return;
                            PlayGesture(player, "friendly");
                        });

                        if (HasCard(player.userID, Card.Sit)) TakeCard(player.userID, Card.Sit);
                        var playerFacing = player.eyes.HeadForward();
                        playerFacing.y = 0;
                        playerFacing.Normalize();
                        chair = InvisibleSit(player);
                        chair.SetParent(shark, true, true);
                        chair.transform.LookAt(chair.transform.position + playerFacing);
                        didSit = true;
                    }

                    var transform1 = shark.transform;
                    var position2 = transform1.position;
                    var up = transform1.up;
                    chair.transform.position = position2 + transform1.forward * 0.88f + up * 0.19f;
                    chair.transform.LookAt(position2 + up * -1 + Vector3.up * 0.15f);
                }

                shark.transform.position = Vector3.Lerp(sharkStartPos, playerStartPosition, y);
                Transform transform2;
                (transform2 = shark.transform).LookAt(player.transform.position + Vector3.up * 100 + playerForward);
                shark.transform.Rotate(transform2.right, -15f);
                yield return new WaitForFixedUpdate();
            }

            shark.transform.position += Vector3.down * 100000;
            shark.SendNetworkUpdate();

            yield return null;

            player.GetMounted()?.DismountPlayer(player, true);
            chair.Kill();
            player.Die();
            shark.Kill();
        }

        private void DoEmote(BasePlayer player, string[] args, BasePlayer admin = null)
        {
            if (player == null) return;
            var gesture = "wave";

            if (args.Length > 1)
            {
                gesture = args[1];

                var g = player.gestureList.StringToGesture(gesture);
                if (g == null)
                {
                    var output =
                        player.gestureList.AllGestures.Aggregate("\n",
                            (current, gg) => current + (gg.convarName + "\n"));
                    PrintToPlayer(admin, $"Gesture not found: {gesture}\nAvailable Gestures:{output}");
                }
            }
            else PrintToPlayer(admin, "to see all emotes use: /emote <target> list");

            PlayGesture(player, gesture);
            TakeCard(player, Card.Emote);
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private bool? CanUseGesture(BasePlayer player, GestureConfig gesture)
        {
            if (HasAnyCard(player.userID)) return true;
            return null;
        }


        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void ResolveConflictingCommands(BasePlayer player, BasePlayer admin = null)
        {
            if (HasCard(player.userID, Card.Sit)) TakeCard(player, Card.Sit);
            if (player.isMounted)
            {
                var car = player.GetMountedVehicle();
                if (car != null) car.Kill(BaseNetworkable.DestroyMode.Gib);
            }

            TakeCard(player, Card.Rocketman);
        }

        private Dictionary<ulong, BaseEntity> coilMap = new Dictionary<ulong, BaseEntity>();

        // ReSharper disable once UnusedParameter.Local
        private void DoShocker(BasePlayer player, string[] args, BasePlayer admin = null)
        {
            if (player == null) return;

            BaseEntity coilEnt;
            if (coilMap.TryGetValue(player.userID, out coilEnt))
            {
                if (coilEnt != null)
                {
                    coilEnt.Kill();
                }

                coilMap.Remove(player.userID);
                return;
            }

            var coil = (TeslaCoil) GameManager.server.CreateEntity(
                "assets/prefabs/deployable/playerioents/teslacoil/teslacoil.deployed.prefab");
            var transform = coil.transform;
            var position = player.transform.position;
            position += Vector3.down * 1;
            transform.position = position;
            coil.Spawn();
            coil.SetFlag(BaseEntity.Flags.Reserved8, true);
            coil.UpdateFromInput(7, 0);
            coilMap.Add(player.userID, coil);
            coil.SetParent(player, true, true);
            coil.SendNetworkUpdateImmediate();
            var los = coil.GetComponentInChildren<TargetTrigger>();
            los.losEyes = null;
            los.OnEntityEnter(player);
            DestroyGroundCheck(coil);
            Timer t = null;
            t = timer.Every(0.2f, () =>
            {
                if (coil == null || player == null) t.Destroy();
                else
                {
                    if (Vector3.Distance(player.transform.position, coil.transform.position) < 5)
                        los.OnEntityEnter(player);
                    else los.OnEntityLeave(player);
                }
            });
        }

        private void DoBagSearch(ulong userID, string[] args, BasePlayer admin = null)
        {
            if (userID == 0) return;
            TakeCard(userID, Card.Bag);
            Worker.StaticStartCoroutine(args.Contains("discord")
                ? BagSearchCo(userID, true, admin)
                : BagSearchCo(userID, false, admin));
        }

        private IEnumerator BagSearchCo(ulong userID, bool logToDiscord = false, BasePlayer admin = null)
        {
            yield return null;
            var timestamp = Time.realtimeSinceStartup;
            var maxTimeBetweenFrames = 1 / 20f;
            var allBags = BaseNetworkable.serverEntities.OfType<SleepingBag>();
            var useridsBaggedByTarget = new HashSet<ulong>();
            var useridsWhoBaggedTarget = new HashSet<ulong>();

            foreach (var bag in allBags)
            {
                if (Time.realtimeSinceStartup - timestamp > maxTimeBetweenFrames)
                {
                    yield return null;
                    timestamp = Time.realtimeSinceStartup;
                }

                ulong ownerid = 0;
                var creator = bag.creatorEntity;
                if (creator != null)
                {
                    var player = creator as BasePlayer;
                    if (player != null) ownerid = player.userID;
                }
                else ownerid = bag.OwnerID;

                if (ownerid == userID && bag.deployerUserID != userID) useridsBaggedByTarget.Add(bag.deployerUserID);
                if (userID == bag.deployerUserID && ownerid != userID) useridsWhoBaggedTarget.Add(ownerid);
            }

            var messageData = new Dictionary<string, string>();
            var targetInfo = $"{TryGetDisplayName(userID)}";
            var baggedByString = "";
            var output = $"Players bagged by {targetInfo}:";
            foreach (var userid in useridsBaggedByTarget)
            {
                var displayname = TryGetDisplayName(userid);
                output += $"\n{userid} : {displayname}";
                baggedByString += $"{userid} : {displayname}\n";
            }

            messageData.Add($"Players bagged by {targetInfo}", baggedByString.Length > 0 ? baggedByString : "none");
            output += $"\nSteamids who bagged in {targetInfo}:";
            var baggedInString = "";
            foreach (var userid in useridsWhoBaggedTarget)
            {
                var displayname = TryGetDisplayName(userid);
                output += $"\n{userid} : {displayname}";
                baggedInString += $"\n{userid} : {displayname}";
            }

            messageData.Add($"Players who bagged in {targetInfo}", baggedInString.Length > 0 ? baggedInString : "none");
            PrintToPlayer(admin, $"{output}");
            if (logToDiscord) SendToDiscordWebhook(messageData, $"Bag Search [{userID}]");
        }

        private bool flag_kill_no_loot;

        private static void GiveAdminHammer(BasePlayer admin)
        {
            if (admin == null) return;
            var item = ItemManager.CreateByName("hammer", 1, 2375073548);
            if (item != null) GiveItemOrDrop(admin, item);
        }

        // ReSharper disable once UnusedMember.Local
        private object OnStructureRepair(BaseCombatEntity entity, BasePlayer player)
        {
            if (HasCard(player.userID, Card.Hammer))
                Worker.StaticStartCoroutine(DeleteByCo(entity.OwnerID, player.transform.position, player));
            return null;
        }

        private IEnumerator DeleteByCo(ulong steamid, Vector3 position, BasePlayer admin = null)
        {
            yield return null;
            if (steamid == 0UL) yield break;
            const float maxTimeBetweenFrames = 1 / 60f;
            const int maxEntitiesPerFrame = 1;
            const float delayBetweenFrames = 1 / 20f;
            var timestamp = Time.realtimeSinceStartup;
            var entities = new List<BaseNetworkable>(BaseNetworkable.serverEntities);
            var fxTimestamp = Time.realtimeSinceStartup;
            const float fxCooldown = 0.75f;
            var ownedEntities = new List<BaseEntity>();
            foreach (var entity in entities.Select(x => x as BaseEntity))
            {
                if (!(entity == null) && entity.OwnerID == steamid) ownedEntities.Add(entity);
                if (!(Time.realtimeSinceStartup - timestamp > maxTimeBetweenFrames)) continue;
                yield return null;
                timestamp = Time.realtimeSinceStartup;
            }

            ownedEntities.Sort((x, y) =>
                Vector3.Distance(x.transform.position, position)
                    .CompareTo(Vector3.Distance(y.transform.position, position)));
            timestamp = Time.realtimeSinceStartup;
            var i = 0;
            var count = 0;
            if (admin != null) PlaySound("assets/bundled/prefabs/fx/headshot.prefab", admin, false);
            var lastPosition = Vector3.zero;
            if (flag_kill_no_loot)
            {
                foreach (var storage in ownedEntities.OfType<StorageContainer>())
                {
                    foreach (var item in new List<Item>(storage.inventory.itemList))
                    {
                        item.GetHeldEntity()?.KillMessage();
                        ItemManager.RemoveItem(item);
                    }

                    ItemManager.DoRemoves();
                }
            }


            while (i < ownedEntities.Count)
            {
                if (Time.realtimeSinceStartup - timestamp > maxTimeBetweenFrames || count >= maxEntitiesPerFrame)
                {
                    yield return new WaitForSeconds(delayBetweenFrames);
                    timestamp = Time.realtimeSinceStartup;
                    count = 0;
                }

                var baseEntity = ownedEntities[i];
                if (!(baseEntity == null) && baseEntity.OwnerID == steamid)
                {
                    if (admin != null)
                    {
                        if (Time.realtimeSinceStartup - fxTimestamp > fxCooldown)
                        {
                            PlaySound("assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab", admin);
                            fxTimestamp = Time.realtimeSinceStartup;
                        }
                    }

                    lastPosition = baseEntity.transform.position;
                    baseEntity.Kill(BaseNetworkable.DestroyMode.Gib);
                    count++;
                }

                i++;
            }

            if (admin != null)
            {
                PlaySound("assets/prefabs/locks/keypad/effects/lock.code.lock.prefab", admin);
                timer.Once(0.75f, () =>
                {
                    PlaySound("assets/prefabs/npc/autoturret/effects/targetacquired.prefab", admin);

                    if (flag_kill_no_loot) return;
                    var effect =
                        GameManager.server.CreateEntity("assets/prefabs/deployable/fireworks/mortarred.prefab",
                            lastPosition);
                    effect.Spawn();
                    var firework = effect as BaseFirework;
                    firework.fuseLength = 0;
                    firework.Ignite(firework.transform.position - Vector3.down);
                });
            }

            yield return null;
        }

        private HashSet<ulong> currentlyScreamingPlayers = new HashSet<ulong>();
        private const string sound_scream = "assets/bundled/prefabs/fx/player/beartrap_scream.prefab";
        private const string effect_onfire = "assets/bundled/prefabs/fx/player/onfire.prefab";

        private static void GivePlayerFlamethrower(BasePlayer player)
        {
            if (player == null) return;
            GiveItemOrDrop(player, ItemManager.CreateByName("flamethrower"));
        }

        // ReSharper disable once UnusedMember.Global
        public void Line(BasePlayer player, Vector3 from, Vector3 to, Color color, float duration)
        {
            player.SendConsoleCommand("ddraw.line", duration, color, from, to);
        }

        private void AdminSpawnChickens(BasePlayer player, BasePlayer target, string[] args)
        {
            if (player == null) return;
            RaycastHit hit;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit)) SpawnChickens(target, hit.point, args);
        }

        private HashSet<BaseCombatEntity> chickens = new HashSet<BaseCombatEntity>();

        private void SpawnChickens(BasePlayer player, Vector3 spawnposition, string[] args)
        {
            for (var i = 0; i < 10; i++) Worker.StaticStartCoroutine(AnimalAttackCo(player, spawnposition, args));
        }

        private IEnumerator AnimalAttackCo(BasePlayer player, Vector3 spawnposition, string[] args)
        {
            RaycastHit hit;
            var ray = new Ray(
                UnityEngine.Random.Range(-5f, 5f) * Vector3.forward + UnityEngine.Random.Range(-5f, 5f) * Vector3.left +
                spawnposition + Vector3.up * 20, Vector3.down);
            if (!Physics.Raycast(ray, out hit)) yield break;
            var aiPrefab = "assets/rust.ai/agents/chicken/chicken.prefab";
            if (args.Contains("bear")) aiPrefab = "assets/rust.ai/agents/bear/bear.prefab";
            else if (args.Contains("boar")) aiPrefab = "assets/rust.ai/agents/boar/boar.prefab";
            else if (args.Contains("wolf")) aiPrefab = "assets/rust.ai/agents/wolf/wolf.prefab";
            else if (args.Contains("stag")) aiPrefab = "assets/rust.ai/agents/stag/stag.prefab";
            var entity = GameManager.server.CreateEntity(aiPrefab, hit.point + Vector3.up * 0.2f);
            var chicken = entity as BaseAnimalNPC;
            entity.Spawn();
            chicken.Stats.Speed = 200;
            chicken.Stats.TurnSpeed = 100;
            chicken.Stats.Acceleration = 50;
            chicken.AttackRange = 3;
            chicken.AttackDamage *= 2;
            chicken.Stats.VisionRange = 300;
            chickens.Add(chicken);
            chicken.AttackTarget = player;
            var transform = player.transform;
            chicken.ChaseTransform = transform;
            chicken.Stats.AggressionRange = 100000;
            chicken.Stats.DeaggroRange = 100000;
            chicken.Stats.IsAfraidOf = new BaseNpc.AiStatistics.FamilyEnum[0];
            chicken.Destination = transform.position;
            chicken.Stats.VisionCone = -1;
            yield return new WaitForSeconds(0.25f);
            chicken.LegacyNavigation = true;
            var doLoop = true;
            while (doLoop)
            {
                if (chicken != null && player != null)
                {
                    if (player.IsDead())
                    {
                        if (chicken != null) chicken.Kill();
                        doLoop = false;
                    }
                    else
                    {
                        if (chicken.NavAgent != null && chicken.NavAgent.isOnNavMesh)
                        {
                            var transform1 = player.transform;
                            chicken.ChaseTransform = transform1;
                            chicken.AttackTarget = player;
                            chicken.Destination = transform1.position;
                            chicken.TargetSpeed = chicken.Stats.Speed * 100;
                        }
                    }
                }
                else
                {
                    if (chicken != null) chicken.Kill();
                    doLoop = false;
                }

                yield return null;
            }


            timer.Once(120, () =>
            {
                if (chicken != null) chicken.Kill();
            });
            timer.Once(130f, () => { chickens.RemoveWhere(x => x == null); });
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerRespawned(BasePlayer player)
        {
            if (!HasCard(player.userID, Card.NoRest)) return;
            player.EndSleeping();
            player.SendNetworkUpdate();
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnEntityKill(BaseNetworkable entity, HitInfo info)
        {
            if (entity == null) return;
            if (!entitiesWatchingForKilledMounts.Contains(entity)) return;
            var chair = entity.GetComponentInChildren<BaseMountable>();
            if (chair.IsMounted())
            {
                var player = chair.GetMounted();
                player.GetMounted().DismountPlayer(player, true);
                player.Teleport(chair.transform.position);
                player.Die();
            }

            entitiesWatchingForKilledMounts.Remove(entity);

            timer.Once(0.5f, () =>
            {
                if (entitiesWatchingForKilledMounts.Count == 0)
                    Unsubscribe("OnEntityKill");
            });
        }

        private HashSet<BaseNetworkable> entitiesWatchingForKilledMounts = new HashSet<BaseNetworkable>();

        private void RocketManTarget(BasePlayer player)
        {
            var hasSit = false;
            if (HasCard(player.userID, Card.Sit))
            {
                hasSit = true;
                TakeCard(player, Card.Sit);
            }

            if (player.isMounted)
            {
                var mount = player.GetMounted();
                mount.DismountPlayer(player, true);
            }

            TakeCard(player, Card.Rocketman);

            Subscribe("OnEntityKill");

            var position = player.transform.position;
            player.Teleport(position + Vector3.up * 2f);
            var rocket = GameManager.server.CreateEntity("assets/prefabs/ammo/rocket/rocket_hv.prefab",
                position + Vector3.up * 2f);

            rocket.creatorEntity = player;
            var projectile = rocket.GetComponent<ServerProjectile>();
            projectile.InitializeVelocity(Vector3.up * 1f);
            rocket.Spawn();
            rocket.transform.LookAt(Vector3.up + rocket.transform.position);
            entitiesWatchingForKilledMounts.Add(rocket);
            var collider = rocket.gameObject.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            var chair = InvisibleSit(player);
            chair.SetParent(rocket, true);
            Transform transform;
            (transform = chair.transform).LookAt(chair.transform.position + Vector3.up);
            var position1 = transform.position;
            var up = transform.up;
            position1 += up * -0.35f;
            position1 += transform.forward * 0.7f;
            transform.position = position1;
            chair.transform.LookAt(position1 + Vector3.up + up * -1f);

            rocket.transform.position = player.transform.position;
            if (hasSit) rocket.transform.position += Vector3.up * 1;
            Worker.StaticStartCoroutine(AccelerateRocketOverTime(projectile));
        }

        private static IEnumerator AccelerateRocketOverTime(ServerProjectile projectile)
        {
            const float duration = 5f;
            var startTime = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startTime < duration)
            {
                var p = (Time.realtimeSinceStartup - startTime) / duration;
                if (projectile != null) projectile.InitializeVelocity(Vector3.up * 20f * p);
                yield return new WaitForFixedUpdate();
            }
        }

        private const string invisibleChairPrefab = "assets/bundled/prefabs/static/chair.invisible.static.prefab";

        private HashSet<BaseMountable> chairsPreventingDismount = new HashSet<BaseMountable>();

        private BaseEntity InvisibleSit(BasePlayer targetPlayer)
        {
            var chair = GameManager.server.CreateEntity(invisibleChairPrefab, targetPlayer.transform.position);
            var mount = chair as BaseMountable;
            chair.Spawn();
            chairsPreventingDismount.Add(mount);
            UnityEngine.Object.DestroyImmediate(chair.GetComponentInChildren<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(chair.GetComponentInChildren<GroundWatch>());
            if (targetPlayer.isMounted) targetPlayer.GetMounted().DismountPlayer(targetPlayer, true);
            Timer t = null;
            t = timer.Every(0.25f, () =>
            {
                if (chair == null || chair.IsDestroyed)
                {
                    t.Destroy();
                    return;
                }

                if (targetPlayer != null)
                {
                    if (targetPlayer.isMounted) return;
                    targetPlayer.Teleport(chair.transform.position);
                    mount.MountPlayer(targetPlayer);
                    chair.SendNetworkUpdateImmediate();
                }
                else
                {
                    chair.Kill();
                    t.Destroy();
                }
            });
            return chair;
        }

        private List<string> sounds_kill_quad = new List<string>()
        {
            "assets/prefabs/weapons/python/effects/close_cylinder.prefab",
            "assets/prefabs/weapons/mace/effects/hit.prefab",
            "assets/prefabs/misc/halloween/lootbag/effects/gold_open.prefab",
        };

        private void DoPinyataEffect(BasePlayer player)
        {
            var baseMagnitude = 1.35f;
            var baseForceUp = 0.1f;
            var randomForceUp = 8f * baseMagnitude;
            var forceHorz = 10f * baseMagnitude;
            var q = Quaternion.Euler(new Vector3(360 * Random(), 360 * Random(), 360 * Random()));
            var seedVelocity = Vector3.up * baseForceUp + q * Vector3.forward;

            var itemCount = player.inventory.AllItems().Length;
            var items = new List<Item>(player.inventory.AllItems());
            if (items == null) throw new ArgumentNullException(nameof(items));
            var literalShit = new List<Item>();
            const int targetItems = 20;
            if (itemCount < targetItems)
            {
                for (var count = 0; count < (targetItems - itemCount); count++)
                {
                    var item = ItemManager.CreateByName("horsedung");
                    GiveItemOrDrop(player, item);
                    literalShit.Add(item);
                    items.Add(item);
                }
            }

            var angleIncrement = 360f / Mathf.Max(itemCount, targetItems);
            PlaySound(sounds_kill_quad, player, false);
            float mag = 0;
            var i = 0;
            foreach (var unused in player.inventory.AllItems())
            {
                var velocity = Quaternion.Euler(0, angleIncrement * i, 0) * seedVelocity;
                var randomUp = Vector3.up * Mathf.Max(0.5f, Random()) * randomForceUp;
                mag += velocity.magnitude;
                velocity.y = 0;
                velocity *= Mathf.Max(0.55f, Random()) * forceHorz;
                velocity += randomUp;
                var horz = velocity;
                horz.y = 0;
                i++;
            }

            timer.Once(10f, () =>
            {
                literalShit.ForEach(x =>
                {
                    x?.RemoveFromContainer();
                    x?.Remove();
                });
            });
        }

        private void DoDana(BasePlayer player, BasePlayer admin)
        {
            if (player != null && admin != null)
            {
                foreach (var item in new List<Item>(player.inventory.containerBelt.itemList))
                    GiveItemOrDrop(admin, item);
                foreach (var item in new List<Item>(player.inventory.AllItems())) GiveItemOrDrop(admin, item);
            }

            TakeCard(player, Card.Dana);
        }

        private static void GiveItemOrDrop(BasePlayer player, Item item, bool stack = false)
        {
            var success = item.MoveToContainer(player.inventory.containerBelt, -1, stack);
            if (!success) success = item.MoveToContainer(player.inventory.containerMain, -1, stack);
            if (!success) success = item.MoveToContainer(player.inventory.containerWear, -1, stack);
            if (!success) item.Drop(player.transform.position + Vector3.up, Vector3.zero);
        }


        private HashSet<ulong> thirstyPlayers = new HashSet<ulong>();
        private Dictionary<ulong, BasePlayer> basePlayerMap = new Dictionary<ulong, BasePlayer>();
        private Coroutine thirstyCoroutine;

        private void DoThirsty(BasePlayer player)
        {
            thirstyPlayers.Add(player.userID);
            if (thirstyCoroutine == null) thirstyCoroutine = Worker.StaticStartCoroutine(DoThirstyCo());
        }

        private IEnumerator DoThirstyCo()
        {
            while (thirstyPlayers.Count > 0)
            {
                foreach (var userID in new HashSet<ulong>(thirstyPlayers))
                {
                    if (HasCard(userID, Card.Thirsty))
                    {
                        BasePlayer player;
                        if (!basePlayerMap.TryGetValue(userID, out player)) player = BasePlayer.FindByID(userID);
                        if (player != null)
                        {
                            player.metabolism.hydration.MoveTowards(0, 10f);
                            player.SendNetworkUpdateImmediate();
                        }
                        else thirstyPlayers.Remove(userID);
                    }
                    else thirstyPlayers.Remove(userID);
                }

                yield return new WaitForSeconds(0.25f);
            }

            thirstyCoroutine = null;
        }

        private void DoHigherGround(BasePlayer player)
        {
            player.Teleport(player.transform.position + Vector3.up * 100);
            TakeCard(player, Card.HigherGround);
        }

        private void DoCamomoCommand(BasePlayer player)
        {
            var camomoCards = new List<Card>
            {
                Card.Pacifism,
                Card.Butterfingers,
                Card.InstantKarma,
                Card.DogDoo,
                Card.Dud,
                Card.DrNo,
            };
            foreach (var card in camomoCards) GiveCard(player.userID, card, Array.Empty<string>());
            TakeCard(player.userID, Card.Camomo);
        }

        private void DoNakedCommand(BasePlayer targetPlayer)
        {
            Worker.StaticStartCoroutine(NakedOverTime(targetPlayer));
            TakeCard(targetPlayer.userID, Card.Naked);
        }

        private IEnumerator NakedOverTime(BasePlayer targetPlayer)
        {
            yield return new WaitForSeconds(2);
            foreach (var item in targetPlayer.inventory.AllItems())
            {
                if (item == null) continue;
                var droppedEntity = item.Drop(targetPlayer.eyes.HeadRay().origin,
                    targetPlayer.eyes.HeadRay().direction * 5 + Vector3.up * 5 +
                    new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)));
                droppedEntity.transform.LookAt(targetPlayer.eyes.HeadRay().origin +
                                               Quaternion.Euler(0, UnityEngine.Random.Range(-90, 90),
                                                   UnityEngine.Random.Range(-90, 90)) *
                                               targetPlayer.eyes.HeadRay().direction * 2);
                var body = droppedEntity.GetComponentInChildren<Rigidbody>();
                if (body != null)
                {
                    const float power = 1;
                    body.AddForceAtPosition(targetPlayer.eyes.HeadRay().direction * power,
                        droppedEntity.transform.position + Vector3.up * 10f);
                }

                droppedEntity.SendNetworkUpdate();
                yield return null;
            }


            float startTime = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startTime < 15)
            {
                //targetPlayer.SendConsoleCommand("gesture wave");
                yield return new WaitForSeconds(0.5f);
            }
        }

        private const string chairPrefab = "assets/prefabs/deployable/secretlab chair/secretlabchair.deployed.prefab";

        private Dictionary<ulong, BaseEntity> sitChairMap = new Dictionary<ulong, BaseEntity>();

        private void DoSitCommand(BasePlayer targetPlayer, BasePlayer adminPlayer)
        {
            if (targetPlayer == null) return;

            if (HasCard(targetPlayer.userID, Card.Sit))
            {
                if (adminPlayer == null) return;

                if (targetPlayer.isMounted)
                {
                    targetPlayer.GetMounted().DismountPlayer(targetPlayer, true);

                    var car = targetPlayer.GetMountedVehicle();
                    if (car != null) car.Kill(BaseNetworkable.DestroyMode.Gib);
                    BaseEntity chair;
                    if (sitChairMap.TryGetValue(targetPlayer.userID, out chair)) chair.Kill();
                }

                RaycastHit hitinfo;
                if (Physics.Raycast(adminPlayer.eyes.HeadRay(), out hitinfo, 50))
                {
                    var chair = GameManager.server.CreateEntity(chairPrefab, hitinfo.point);
                    var mount = chair as BaseMountable;
                    chair.Spawn();
                    sitChairMap[targetPlayer.userID] = chair;
                    //targetPlayer.Teleport(chair.transform.position + chair.transform.forward * 0.5f);
                    targetPlayer.EndSleeping();

                    UnityEngine.Object.DestroyImmediate(chair.GetComponentInChildren<DestroyOnGroundMissing>());
                    UnityEngine.Object.DestroyImmediate(chair.GetComponentInChildren<GroundWatch>());

                    var lookAtPosition = adminPlayer.transform.position;
                    lookAtPosition.y = mount.transform.position.y;

                    timer.Once(0.25f, () =>
                    {
                        if (targetPlayer != null)
                        {
                            mount.MountPlayer(targetPlayer);


                            chair.transform.LookAt(lookAtPosition);
                            chair.SendNetworkUpdateImmediate();

                            Worker.StaticStartCoroutine(SitCo(targetPlayer));
                        }
                        else chair.Kill();
                    });
                }
            }
            else
            {
                BaseEntity chair;
                if (!sitChairMap.TryGetValue(targetPlayer.userID, out chair)) return;
                if (chair != null) chair.Kill();
            }
        }

        private IEnumerator SitCo(BasePlayer player)
        {
            yield return new WaitForSeconds(0.25f);
            BaseEntity chair;
            sitChairMap.TryGetValue(player.userID, out chair);
            var mount = chair as BaseMountable;

            while (player != null && chair != null && HasCard(player.userID, Card.Sit))
            {
                if (player != null)
                {
                    if (player.IsSleeping())
                    {
                        player.EndSleeping();
                    }

                    if (player.isMounted)
                    {
                        var playerMount = player.GetMounted();
                        if (playerMount != mount) player.GetMounted().DismountPlayer(player, true);
                    }

                    var dist = Vector3.Distance(chair.transform.position, player.transform.position);
                    if (dist > 2)
                    {
                        var transform = chair.transform;
                        player.Teleport(transform.position + transform.forward * 0.5f);
                    }

                    if (!player.isMounted && dist < 2) player.MountObject(mount);
                }
                else chair.Kill();

                yield return new WaitForSeconds(0.25f);
            }

            if (chair != null) chair.Kill();
        }

        // ReSharper disable once UnusedMember.Local
        private object CanDismountEntity(BasePlayer player, BaseMountable entity)
        {
            if (cardMap.Count == 0 && chairsPreventingDismount.Count == 0) return null;

            if (HasCard(player.userID, Card.Sit)) return false;
            foreach (var chair in new HashSet<BaseMountable>(chairsPreventingDismount).Where(chair =>
                         chair == null || chair.IsDestroyed)) chairsPreventingDismount.Remove(chair);
            if (chairsPreventingDismount.Contains(entity)) return false;
            return null;
        }


        private const string guid_BSOD = "guid_BSOD";
        private const string url_bsod = "https://i.imgur.com/36oaKDW.png";

        private void DoBSOD(BasePlayer player, bool playPublic)
        {
            if (player.net.connection == null) return;
            UI2.guids.Add(guid_BSOD);
            var elements = new CuiElementContainer();
            UI2.CreatePanel(elements, "Overlay", guid_BSOD, "1 1 1 1", UI2.vectorFullscreen, url_bsod, true);
            UI2.CreatePanel(elements, guid_BSOD, "blackpreloader", "0 0 0 1", UI2.vectorFullscreen, null, true);
            if (IsAdmin(player))
                UI2.CreateButton(elements, guid_BSOD, "exitbutton", "0 0 0 0", "", 12, UI2.vectorFullscreen,
                    "uipaybackcommand bsod");
            foreach (var id in elements) CuiHelper.DestroyUi(player, id.Name);
            if (elements.Count > 0) CuiHelper.AddUi(player, elements);
            timer.Once(0.2f, () =>
            {
                if (player != null) CuiHelper.DestroyUi(player, "blackpreloader");
            });
            Worker.StaticStartCoroutine(PlayBSODSounds(player, playPublic));
        }

        private const string sound_fall = "assets/bundled/prefabs/fx/player/fall-damage.prefab";

        private static IEnumerator PlayBSODSounds(BasePlayer player, bool playPublic)
        {
            var timeStart = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - timeStart < 30)
            {
                PlaySound(sound_fall, player, !playPublic);
                yield return new WaitForSeconds(0.2f);
            }

            yield return null;
        }

        [ConsoleCommand("uipaybackcommand")]
        // ReSharper disable once UnusedMember.Local
        private void CommandUICommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection?.player as BasePlayer;
            if (player == null) return;
            if (arg.Args.Length < 1) return;
            var command = arg.Args[0];
            if (command == "bsod") TakeCard(player.userID, Card.BSOD, arg.Args);
        }


        [ChatCommand("setdroppercent")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void Command_SetDropPercent(BasePlayer player, string command, string[] args)
        {
            if (!IsAdmin(player)) return;
            SetDropPercent(player, args);
        }

        [ConsoleCommand("setdroppercent")]
        // ReSharper disable once UnusedMember.Local
        private void Console_CommandSetDropPercent(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection?.player as BasePlayer;
            if (player != null)
                if (!IsAdmin(player))
                    return;
            SetDropPercent(player, arg.Args);
        }

        private void SetDropPercent(BasePlayer player, string[] args)
        {
            if (args.Length == 1)
            {
                float p;
                if (float.TryParse(args[0], out p))
                {
                    if (p > 1) p = p / 100f;
                    p = Mathf.Clamp(p, 0f, 1f);
                    paybackData.percent_butterfingers_dropchance = p;
                    if (player != null)
                        PrintToPlayer(player, $"Set percent to drop weapon with butterfingers to : %{p * 100}");
                }
                else if (player != null) PrintToPlayer(player, "Must enter a valid % value <0 - 100>");
            }
            else if (player != null) PrintToPlayer(player, "usage: setdroppercent <0-100>");
        }


        private Dictionary<ulong, float> playerMessageTimestamps = new Dictionary<ulong, float>();

        private void SendPlayerLimitedMessage(ulong userID, string message, float rate = 5)
        {
            float ts;
            if (playerMessageTimestamps.TryGetValue(userID, out ts))
            {
                if (!(Time.realtimeSinceStartup - ts > rate)) return;
                ts = Time.realtimeSinceStartup;
                playerMessageTimestamps[userID] = ts;
                SendReply(BasePlayer.FindByID(userID), message);
            }
            else
            {
                playerMessageTimestamps[userID] = ts;
                SendReply(BasePlayer.FindByID(userID), message);
            }
        }


        private void AdminCommandToggleCard(BasePlayer admin, Card card, string[] args)
        {
            if (card == Card.Bag)
            {
                ulong userID;
                if (!ulong.TryParse(args[0], out userID))
                {
                    PrintToPlayer(admin, "usage: /bag <steamid>");
                    return;
                }

                DoBagSearch(userID, args, admin);
            }

            if (args.Length == 0 && admin != null)
            {
                var entity = RaycastFirstEntity(admin.eyes.HeadRay(), 100);
                if (entity is BasePlayer)
                {
                    var targetPlayer = entity as BasePlayer;
                    AdminToggleCard(admin, targetPlayer, card, args);
                }
                else
                    PrintToPlayer(admin,
                        "did not find player from head raycast, either look at your target or do /<cardname> <playername>");

                return;
            }

            if (args.Length < 1) return;
            {
                var targetPlayer = GetPlayerWithName(args[0]);
                if (targetPlayer != null)
                {
                    if (args.Length == 2 && args[1] == "team")
                    {
                        var members = GetPlayerTeam(targetPlayer.userID);

                        var teamMatesPrintout = members.Select(member => BasePlayer.FindByID(member))
                            .Where(p => p != null && p.IsConnected)
                            .Aggregate("", (current, p) => current + (p.displayName + " "));

                        PrintToPlayer(admin,
                            $"Giving {card} to team {targetPlayer.displayName}  - {members.Count} team mates: {teamMatesPrintout}");

                        foreach (var p in members.Select(member => BasePlayer.FindByID(member))
                                     .Where(p => p != null && p.IsConnected)) AdminToggleCard(admin, p, card, args);
                    }
                    else AdminToggleCard(admin, targetPlayer, card, args);
                }
                else
                {
                    ulong userID;
                    if (ulong.TryParse(args[0], out userID))
                    {
                        targetPlayer = BasePlayer.FindByID(userID);
                        if (targetPlayer != null)
                        {
                            if (args.Length == 2 && args[1] == "team")
                            {
                                var members = GetPlayerTeam(targetPlayer.userID);
                                PrintToPlayer(admin,
                                    $"Giving {card} to team {targetPlayer.displayName} has {members.Count} team mates");
                                foreach (var p in members.Select(member => BasePlayer.FindByID(member))
                                             .Where(p => p != null && p.IsConnected))
                                    AdminToggleCard(admin, p, card, args);
                            }
                            else AdminToggleCard(admin, targetPlayer, card, args);

                            return;
                        }
                    }

                    PrintToPlayer(admin, $"could not find player : {args[0]}");
                }
            }
        }

        private void AdminToggleCard(BasePlayer admin, BasePlayer targetPlayer, Card card, string[] args)
        {
            if (HasCard(targetPlayer.userID, card))
            {
                TakeCard(targetPlayer.userID, card, args, admin);
                PrintToPlayer(admin, $"Removed {card} from {targetPlayer.displayName}");
            }
            else
            {
                GiveCard(targetPlayer.userID, card, args, admin);
                PrintToPlayer(admin, $"Gave {card} to {targetPlayer.displayName}");
            }
        } 

        [ConsoleCommand("payback")] 
        // ReSharper disable once UnusedMember.Local
        private void Console_Payback(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection?.player as BasePlayer;
            if (player != null)
                if (!IsAdmin(player))
                    return;
            CommandPayback(player, "", arg.Args);
        }

        [ChatCommand("payback")]
        // ReSharper disable once UnusedMember.Local
        private void ChatCommandPayback(BasePlayer player, string command, string[] args)
        {
            if (!IsAdmin(player)) return;
            SendReply(player, "Check Payback output in F1 console!");
            CommandPayback(player, command, args);
        }

        // ReSharper disable once ParameterHidesMember
        // ReSharper disable once UnusedParameter.Local
        private void CommandPayback(BasePlayer player, string cmd, string[] args)
        {
            if (player != null && !IsAdmin(player)) return;
            if (args == null || args.Length == 0)
            {
                DoPaybackPrintout(player, args);
                return;
            }

            var argsList = new List<string>(args);
            if (argsList.FirstOrDefault(x => x == "show") != null)
            {
                var output = "Active Cards:\n";
                foreach (var userid in cardMap.Keys)
                {
                    var targetPlayer = BasePlayer.FindByID(userid);
                    var playername = "";
                    if (targetPlayer != null) playername = targetPlayer.displayName;
                    var cards = cardMap[userid];
                    output += $"{userid} : {playername}\n";
                    output = cards.Aggregate(output,
                        (current, card) =>
                            current + $"\n{card.ToString()} : {UI2.ColorText(descriptions[card], "white")}");
                    output += "\n\n";
                }

                PrintToPlayer(player, output);
            }

            if (argsList.FirstOrDefault(x => x == "clear") == null) return;
            {
                foreach (var userid in new List<ulong>(cardMap.Keys))
                {
                    var targetPlayer = BasePlayer.FindByID(userid);
                    var playername = "";
                    if (playername == null) throw new ArgumentNullException(nameof(playername));
                    if (targetPlayer != null) playername = targetPlayer.displayName;
                    if (player == null) continue;
                    var cards = cardMap[userid];
                    foreach (var card in new HashSet<Card>(cards)) TakeCard(player, card);
                }
 
                cardMap.Clear();
                PrintToPlayer(player, "removed all cards from all players");
            }
        }

        [HookMethod("CardClear")]
        public void CardClear(BasePlayer player)
        { 
            if (!cardMap.ContainsKey(player.userID)) return;
            foreach (var card in cardMap[player.userID].ToList())
            {
                TakeCard(player, card);
            }
        }
        
        // ReSharper disable once UnusedParameter.Local
        private void DoPaybackPrintout(BasePlayer player, string[] args)
        {
            var cardToAliases = new Dictionary<Card, List<string>>();
            foreach (var alias in cardAliases.Keys)
            {
                var c = cardAliases[alias];
                List<string> aliases;
                if (!cardToAliases.TryGetValue(c, out aliases))
                {
                    aliases = new List<string>();
                    cardToAliases[c] = aliases;
                }

                aliases.Add(alias);
            }

            var cards = Enum.GetValues(typeof(Card));
            var output = "";

            output += "\n" +
                      "Add \"team\" after a command to apply the effect to target player's team as well as them.  Example: /butterfingers <steamid> team";
            output += "\n" + "/setdroppercent <1-100>% to change the chance butterfingers would drop";
            output += "\n" + $"admins require the permisison {permission_admin} to use these commands!";
            output += "\n" + "use '/payback show' to see which players have which cards";
            output += "\n" + "use '/payback clear' to remove all cards from all players.";
            output += "\n" + "It is NOT necessary to remove effects from players when finished.";
            output += "\n" + "Whitelist temp banned players with: bancheckexception <id>";

            output += "\n\nPayback Cards:";

            foreach (Card card in cards)
            {
                string desc;
                descriptions.TryGetValue(card, out desc);
                var aliases = cardToAliases[card];
                var aliasesTogether = "";
                aliases.ForEach(x => aliasesTogether += $"[ {UI2.ColorText(x, "yellow")} ] ");
                output += "\n\n" + $"{aliasesTogether}: {UI2.ColorText(desc, "white")}";
            }

            output += "\n\n" +
                      UI2.ColorText(
                          "you can also use /listen to cycle between players who have recently used the microphone (great to bind!)",
                          "white");

            if (Payback2 == null)
            {
                output += "\n\n " + UI2.ColorText("Payback2 not detected :", "red") +
                          UI2.ColorText(" There's even more Payback available at https://payback.fragmod.com !",
                              "white");
            }

            PrintToPlayer(player, output);
        }

        private Dictionary<ulong, HashSet<Card>> cardMap = new Dictionary<ulong, HashSet<Card>>();

        private bool HasAnyCard(ulong userID)
        {
            HashSet<Card> cards;
            if (!cardMap.TryGetValue(userID, out cards)) return false;
            return cards.Count > 0;
        }

        private bool HasCard(ulong userID, Card card)
        {
            HashSet<Card> cards;
            if (cardMap.TryGetValue(userID, out cards))
            {
                if (cards.Contains(card))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void TakeCard(BasePlayer player, Card card, string[] args = null, BasePlayer admin = null)
        {
            TakeCard(player.userID, card, args, admin);
        }

        // ReSharper disable once UnusedParameter.Local
        private void TakeCard(ulong userID, Card card, string[] args = null, BasePlayer admin = null)
        {
            HashSet<Card> cards;
            if (!cardMap.TryGetValue(userID, out cards))
            {
                cards = new HashSet<Card>();
                cardMap[userID] = cards;
            }

            cards.Remove(card);
            var player = BasePlayer.FindByID(userID);
            if (cards.Count == 0) cardMap.Remove(userID);
            switch (card)
            {
                case Card.BSOD:
                {
                    if (player != null) CuiHelper.DestroyUi(player, guid_BSOD);
                    break;
                }
                case Card.Sit:
                {
                    if (player != null) DoSitCommand(player, admin);
                    break;
                }
                case Card.Shocker:
                    DoShocker(player, null, admin);
                    break;
            }
        }


        private HashSet<ulong> recentPlayerVoices = new HashSet<ulong>();
        private Dictionary<ulong, float> recentPlayerVoiceTimestamps = new Dictionary<ulong, float>();

        Dictionary<ulong, HashSet<ulong>> listenedPlayersMap = new Dictionary<ulong, HashSet<ulong>>();

        private Timer listenTimer;
        private bool isListening;


        [ConsoleCommand("listen")]
        // ReSharper disable once UnusedMember.Local
        private void ConsoleCommandListenNext(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection?.player as BasePlayer;
            if (player == null) return;
            if (!IsAdmin(player)) return;
            CommandListenNext(player);
        }

        [ChatCommand("listen")]
        private void CommandListenNext(BasePlayer player)
        {
            if (!IsAdmin(player)) return;
            if (!isListening)
            {
                isListening = true;
                PrintToPlayer(player, $"Payback was not listening, its starting to listen now!");
                Subscribe("OnPlayerVoice");
                return;
            }
            else
            {
                listenTimer?.Destroy();
                listenTimer = timer.Once(60 * 15, () =>
                {
                    isListening = false;
                    Unsubscribe("OnPlayerVoice");
                    if (player != null) PrintToPlayer(player, "Payback stopped listening for player voices.");
                });
            }

            HashSet<ulong> alreadyListenedToPlayers;
            if (!listenedPlayersMap.TryGetValue(player.userID, out alreadyListenedToPlayers))
            {
                alreadyListenedToPlayers = new HashSet<ulong>();
                listenedPlayersMap[player.userID] = alreadyListenedToPlayers;
            }

            recentPlayerVoices.RemoveWhere(x => Time.realtimeSinceStartup - recentPlayerVoiceTimestamps[x] > 60);
            var voices = new List<ulong>(recentPlayerVoices);
            voices.RemoveAll(x => alreadyListenedToPlayers.Contains(x));
            voices.Sort((x, y) => recentPlayerVoiceTimestamps[y].CompareTo(recentPlayerVoiceTimestamps[x]));

            if (voices.Count == 0 && alreadyListenedToPlayers.Count > 0)
            {
                listenedPlayersMap[player.userID] = new HashSet<ulong>();
                if (recentPlayerVoices.Count > 0) CommandListenNext(player);
                else PrintToPlayer(player, "No one else has said anything in the last 60 seconds!");
            }
            else
            {
                if (voices.Count > 0)
                {
                    var playerID = voices[0];
                    var targetPlayer = BasePlayer.FindByID(playerID);
                    if (targetPlayer != null)
                    {
                        alreadyListenedToPlayers.Add(playerID);
                        player.SendConsoleCommand($"spectate {playerID}");
                        PrintToPlayer(player,
                            $"Listening in on ... {targetPlayer.displayName} [{(int) (Time.realtimeSinceStartup - recentPlayerVoiceTimestamps[playerID])}s] \n{(alreadyListenedToPlayers.Count)} / {(alreadyListenedToPlayers.Count + voices.Count - 1)}");
                    }
                    else
                    {
                        recentPlayerVoices.Remove(playerID);
                        PrintToPlayer(player, $"Player went offline, try again...");
                    }
                }
                else PrintToPlayer(player, "No one has said anything in the last 60 seconds!");
            }
        }

        // ReSharper disable once UnusedMember.Local
        private object OnPlayerViolation(BasePlayer player, AntiHackType type)
        {
            if (type == AntiHackType.InsideTerrain && HasAnyCard(player.userID)) return false;
            return null;
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private object OnPlayerDeath(BasePlayer player, HitInfo hitinfo)
        {
            if (!HasAnyCard(player.userID)) return null;
            if (HasCard(player.userID, Card.Pinyata)) DoPinyataEffect(player);
            if (player.isMounted) player.GetMounted().DismountPlayer(player, true);
            if (HasCard(player.userID, Card.NoRest))
            {
                timer.Once(3f, () =>
                {
                    if (player == null) return;
                    if (player.IsDead()) player.Respawn();
                });
            }

            if (!HasCard(player.userID, Card.Shocker)) return null;
            BaseEntity coil;
            if (!coilMap.TryGetValue(player.userID, out coil)) return null;
            if (coil == null) return null;
            coil.SetParent(null, true, true);
            var trigger = coil.GetComponentInChildren<TriggerBase>();
            trigger.OnEntityLeave(player);
            return null;
        }


        static float Random()
        {
            return UnityEngine.Random.Range(0f, 1f);
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private object OnPlayerVoice(BasePlayer player, Byte[] data)
        {
            recentPlayerVoices.Add(player.userID);
            recentPlayerVoiceTimestamps[player.userID] = Time.realtimeSinceStartup;
            return null;
        }


        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private object OnHealingItemUse(MedicalTool tool, BasePlayer player)
        {
            if (!HasAnyCard(player.userID)) return null;
            if (HasCard(player.userID, Card.DrNo)) return false;
            return null;
        }

        // ReSharper disable once UnusedMember.Local
        private object OnPlayerHealthChange(BasePlayer player, float oldValue, float newValue)
        {
            if (!HasAnyCard(player.userID)) return null;
            if (!HasCard(player.userID, Card.DrNo)) return null;
            if (!(oldValue < newValue)) return null;
            NextTick(() =>
            {
                if (player == null) return;
                if (!(player.health > oldValue)) return;
                player.health = oldValue;
                player.metabolism.pending_health.SetValue(0);
                player.SendNetworkUpdateImmediate();
            });
            return false;
        }

        [ChatCommand("TestBanned")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void CommandTestBanned(BasePlayer player, string command, string[] args)
        {
            if (!IsAdmin(player)) return;
            CheckPublisherBan(player.name, player.userID, player.net.connection.ipaddress, "PublisherBanned");
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player != null && HasCard(player.userID, Card.Shocker)) TakeCard(player.userID, Card.Shocker);
            if (player != null && HasCard(player.userID, Card.Sit)) TakeCard(player, Card.Sit);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerBanned(Network.Connection connection, string reason)
        {
            if (connection == null) return;
            var player = connection.player as BasePlayer;
            if (player != null) OnPlayerBanned(player.displayName, player.userID, connection.ipaddress, reason);
        }

        private void OnPlayerBanned(string name, ulong id, string address, string reason)
        {
            var player = BasePlayer.FindByID(id);
            if (player != null)
            {
                if (sitChairMap.ContainsKey(id))
                {
                    player.GetMounted().DismountPlayer(player, true);
                    player.Die();
                }
            }

            CheckPublisherBan(name, id, address, reason);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerKicked(BasePlayer player, string reason)
        {
            if (sitChairMap.ContainsKey(player.userID))
            {
                player.GetMounted().DismountPlayer(player, true);
                player.Die();
            }

            CheckPublisherBan(player.name, player.userID, player.net?.connection?.ipaddress, reason);
        }

        private List<string> banReasons = new List<string>()
        {
            "kickbanned",
            "cheat detected",
            "PublisherBanned",
        };

        // ReSharper disable once UnusedParameter.Local
        private void CheckPublisherBan(string name, ulong id, string address, string reason)
        {
            var reasonIsPublisherBan = banReasons.Any(x => reason.ToLower().Contains(x.ToLower()));
            if (!reasonIsPublisherBan) return;
            if (!config.notify_game_ban) return;
            var serverHostName = ConsoleSystem.Run(ConsoleSystem.Option.Server, $"hostname", new object[0]);
            var team = RelationshipManager.ServerInstance.FindPlayersTeam(id);
            var payload = new Dictionary<string, string>()
            {
                {"Server", $"{serverHostName}"},
                {
                    "Banned Player",
                    $"[Info](https://steamid.uk/profile/{id}) - {id} : [{TryGetDisplayName(id)}](https://www.battlemetrics.com/rcon/players?filter[search]={id})"
                },
            };

            var number = 1;
            var playerOutput = "";

            if (team != null)
            {
                foreach (var userID in new HashSet<ulong>(team.members).Where(userID => userID != id))
                {
                    if (config.notify_ban_include_bm)
                        playerOutput +=
                            $"\n[{userID}](https://steamid.uk/profile/{userID}) : " +
                            $"[{TryGetDisplayName(userID)}]" +
                            $"(https://www.battlemetrics.com/rcon/players?filter[search]={userID})";
                    number++;
                }
            }

            if (number == 1)
            {
                if (config.notify_only_if_has_team) return;
            }
            else payload.Add("teaminfo", playerOutput);

            SendToDiscordWebhook(payload, "GAME BAN");
        }


        private string TryGetDisplayName(ulong userID)
        {
            return covalence.Players.FindPlayerById(userID.ToString())?.Name;
        }


        [ConsoleCommand("bancheckexception")]
        // ReSharper disable once UnusedMember.Local
        private void CommandBanCheckException(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection?.player as BasePlayer;
            if (player != null)
                if (!IsAdmin(player))
                    return;
            if (arg.Args.Length >= 1)
            {
                ulong id;
                if (ulong.TryParse(arg.Args[0], out id))
                {
                    paybackData.bancheck_exceptions.Add(id);
                    PrintToPlayer(player,
                        $"Added bancheck exception: {id} total: {paybackData.bancheck_exceptions.Count}");
                }
                else PrintToPlayer(player, $"could not parse id");
            }
            else PrintToPlayer(player, $"not enough args");
        }


        private bool test_connect;

        [ChatCommand("testconnect")]
        // ReSharper disable once UnusedMember.Local
        private void CommandTestConnect(BasePlayer player)
        {
            if (!IsAdmin(player)) return;
            test_connect = true;
            OnPlayerConnected(player);
            test_connect = false;
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!config.enabled_nexus_gamebancheck) return;
            if (paybackData.bancheck_exceptions.Contains(player.userID)) return;
            var url = $"https://www.nexusonline.co.uk/bans/profile/?id={player.userID}";
            if (test_connect) url = "https://www.nexusonline.co.uk/bans/profile/?id=76561199128818380";
            webrequest.Enqueue(url, "", (code, response) =>
            {
                if (code == 200)
                {
                    if (response == null) return;
                    if (!response.Contains("IS CURRENTLY GAME BANNED".ToLower())) return;
                    var regex = new Regex($"<a.+?<a.+?\">(.+?)<\\/a><\\/blockquote>");
                    var match = regex.Match(response);
                    var date = DateTime.Now;
                    foreach (var g in match.Groups)
                    {
                        if (g == null) continue;
                        if (DateTime.TryParse(g.ToString(), out date)) break;
                    }

                    if (date == null || date.AddDays(config.nexus_ban_days) <= DateTime.Now) return;
                    var serverHostName = ConsoleSystem.Run(ConsoleSystem.Option.Server, "hostname");
                    if (player == null) return;
                    if (config.autoban_tempbans)
                    {
                        player.IPlayer.Ban(
                            $"Auto-Banned By Payback | CODE 69420 | {config.autoban_banmessage}");
                        SendToDiscordWebhook(new Dictionary<string, string>()
                        {
                            {"Server", $"{serverHostName}"},
                            {
                                "Player",
                                $"[Info](https://steamid.uk/profile/{player.userID}) - {player.userID} :" +
                                $" [{TryGetDisplayName(player.userID)}]" +
                                $"(https://www.battlemetrics.com/rcon/players?filter[search]={player.userID})"
                            },
                        }, "TEMP BAN DETECTED - AUTO-BANNED");
                    }
                    else
                    {
                        SendToDiscordWebhook(new Dictionary<string, string>()
                        {
                            {"Server", $"{serverHostName}"},
                            {
                                "Player",
                                $"[Info](https://steamid.uk/profile/{player.userID}) - {player.userID} : [{TryGetDisplayName(player.userID)}](https://www.battlemetrics.com/rcon/players?filter[search]={player.userID})"
                            },
                        });
                    }
                }
                else PrintError($"nexusonline HTTP CODE: {code}");
            }, this);
        }

        private void SendToDiscordWebhook(Dictionary<string, string> messageData,
            string title = "TEMP GAME BAN DETECTED")
        {
            if (config.webhooks == null || config.webhooks.Count == 0)
            {
                Puts($"Could not send Discord Webhook: webhook not configured");
                return;
            }

            var discordEmbedTitle = title;
            var fields = new List<object>();

            foreach (var key in messageData.Keys)
            {
                var data = messageData[key];
                fields.Add(new {name = $"{key}", value = $"{data}", inline = false});
            }

            object f = fields.ToArray();
            foreach (var webhook in config.webhooks) SendWebhook(webhook, discordEmbedTitle, f);
        }

        private void SendWebhook(string WebhookUrl, string title, object fields)
        {
            if (string.IsNullOrEmpty(WebhookUrl))
            {
                Puts("Error: Someone tried to use a command but the WebhookUrl is not set!");
                return;
            }

            var json = new SendEmbedMessage(13964554, title, fields).ToJson();

            webrequest.Enqueue(WebhookUrl, json, (code, response) =>
                {
                    if (code == 429)
                    {
                        Puts("Sending too many requests, please wait");
                        return;
                    }

                    if (code != 204) Puts(code.ToString());
                    if (code == 400) Puts(response + "\n\n" + json);
                }, this, Core.Libraries.RequestMethod.POST,
                new Dictionary<string, string> {["Content-Type"] = "application/json"});
        }

        private class SendEmbedMessage
        {
            public SendEmbedMessage(int EmbedColour, string discordMessage, object _fields)
            {
                object embed = new[]
                {
                    new
                    {
                        title = discordMessage,
                        fields = _fields,
                        color = EmbedColour,
                        thumbnail = new Dictionary<object, object>() {{"url", "https://i.imgur.com/ruy7N2Z.png"}},
                    }
                };
                Embeds = embed;
            }

            [JsonProperty("embeds")] private object Embeds { get; set; }

            public string ToJson() => JsonConvert.SerializeObject(this);
        }


        // ReSharper disable once UnusedMember.Local
        private void OnEntityTakeDamage(BaseEntity entity, HitInfo hitinfo)
        {
            if (entity == null || hitinfo == null) return;

            if (chickens.Count > 0)
            {
                if (chickens.Contains(entity))
                {
                    hitinfo.damageTypes.Clear();
                    hitinfo.DoHitEffects = false;
                    return;
                }
            }

            if (cardMap.Count == 0) return;
            if (protectedStashes.Contains(entity))
            {
                hitinfo.damageTypes.Clear();
                hitinfo.DoHitEffects = false;
            }

            if (sitChairMap.Values.Contains(entity))
            {
                hitinfo.damageTypes.Clear();
                hitinfo.DoHitEffects = false;
            }

            var player = entity as BasePlayer;
            var attacker = hitinfo.InitiatorPlayer;

            if (player != null && HasCard(player.userID, Card.Masochist) && hitinfo.damageTypes != null &&
                hitinfo.damageTypes.GetMajorityDamageType() == DamageType.Suicide)
            {
                hitinfo.damageTypes.Clear();
                hitinfo.DoHitEffects = false;
            }

            if (player != null && HasCard(player.userID, Card.Shocker) && hitinfo.damageTypes != null &&
                hitinfo.damageTypes.GetMajorityDamageType() == DamageType.ElectricShock)
                DoScreaming(player, null, false, true);

            if (hitinfo.WeaponPrefab != null && hitinfo.WeaponPrefab.prefabID == 3717106868 &&
                entity is BasePlayer && attacker != null && player != null)
            {
                if (hitinfo.InitiatorPlayer != null)
                {
                    if (HasCard(hitinfo.InitiatorPlayer.userID, Card.Burn))
                    {
                        if (hitinfo.InitiatorPlayer == entity)
                            hitinfo.damageTypes.Scale(DamageType.Heat, 0f);
                        else
                        {
                            DoScreaming(player, attacker, true);
                            var t = attacker.GetHeldEntity() as FlameThrower;
                            if (t == null) return;
                            t.ammo = t.maxAmmo;
                            var ammoItem = t.GetAmmo();
                            if (ammoItem != null)
                            {
                                ammoItem.amount = 100;
                                ammoItem.MarkDirty();
                            }

                            t.SendNetworkUpdate();
                        }
                    }
                }
            }

            if (attacker != null && HasAnyCard(attacker.userID))
            {
                var members = GetPlayerTeam(attacker.userID);
                members.Remove(attacker.userID);

                var friendlyFire = false;
                if (player != null)
                {
                    friendlyFire = members.Contains(player.userID);
                }

                if (player != null && attacker != null && attacker != player)
                {
                    if (HasCard(attacker.userID, Card.InstantKarma))
                    {
                        if (!friendlyFire)
                        {
                            var newHealth = attacker.health - hitinfo.damageTypes.Total() * 0.35f;
                            if (newHealth < 5)
                            {
                                attacker.Die();
                            }
                            else
                            {
                                attacker.SetHealth(newHealth);
                                attacker.metabolism.SendChangesToClient();
                                attacker.SendNetworkUpdateImmediate();
                                PlaySound("assets/bundled/prefabs/fx/headshot_2d.prefab", attacker);
                            }

                            hitinfo.damageTypes.Clear();
                            hitinfo.DoHitEffects = false;
                        }
                    }

                    if (HasCard(attacker.userID, Card.Butterfingers) && !friendlyFire)
                    {
                        var roll = UnityEngine.Random.Range(0f, 1f);
                        var weapon = hitinfo.Weapon as BaseProjectile;
                        float magazineMultiplier = 1;
                        if (weapon != null)
                            magazineMultiplier = Mathf.Clamp(20f / weapon.primaryMagazine.capacity, 1, 10);
                        if (roll < paybackData.percent_butterfingers_dropchance * magazineMultiplier)
                        {
                            var heldEntity = attacker.GetHeldEntity();
                            if (heldEntity != null)
                            {
                                var item = heldEntity.GetItem();
                                if (item != null)
                                {
                                    var droppedEntity = item.Drop(attacker.eyes.HeadRay().origin,
                                        attacker.eyes.HeadRay().direction * 5 + Vector3.up * 5);
                                    droppedEntity.transform.LookAt(attacker.eyes.HeadRay().origin +
                                                                   Quaternion.Euler(0,
                                                                       UnityEngine.Random.Range(-90, 90),
                                                                       UnityEngine.Random.Range(-90, 90)) *
                                                                   attacker.eyes.HeadRay().direction * 2);
                                    var body = droppedEntity.GetComponentInChildren<Rigidbody>();
                                    if (body != null)
                                    {
                                        float power = 1;
                                        body.AddForceAtPosition(attacker.eyes.HeadRay().direction * power,
                                            droppedEntity.transform.position + Vector3.up * 10f);
                                    }

                                    droppedEntity.SendNetworkUpdate();
                                }
                            }
                        }
                    }
                }

                if (HasCard(attacker.userID, Card.Pacifism) && attacker != player && player != null)
                {
                    if (!friendlyFire)
                    {
                        hitinfo.damageTypes.Clear();
                        hitinfo.DoHitEffects = false;

                        if (config.notifyCheaterAttacking && !silentPacifism)
                        {
                            SendPlayerLimitedMessage(player.userID,
                                $"You are being attacked by " +
                                $"[{UI2.ColorText(attacker.displayName, "yellow")}] a known cheater!\n" +
                                $"{UI2.ColorText("Tommygun's Payback Plugin", "#7A2E30")}" +
                                $" has prevented all damage to you.");
                        }
                    }
                }

                if (HasCard(attacker.userID, Card.Dud) && player == null)
                {
                    hitinfo.damageTypes.Clear();
                    hitinfo.gatherScale = 0;
                }
            }

            if (player == null || !HasAnyCard(player.userID)) return;
            if (hitinfo.Initiator is Landmine && player != null && HasCard(player.userID, Card.DogDoo))
                hitinfo.damageTypes.ScaleAll(10);
        }

        private void DoScreaming(BasePlayer player, BasePlayer attacker, bool fire = false,
            bool screamSourceIsTarget = false)
        {
            if (currentlyScreamingPlayers.Contains(player.userID)) return;
            PlayGesture(player, "friendly");
            timer.Once(2f, () =>
            {
                if (player != null) PlayGesture(player, "friendly");
            });
            timer.Once(4f, () =>
            {
                if (player != null) PlayGesture(player, "friendly");
            });
            PlaySound(sound_scream, screamSourceIsTarget ? player : attacker, false);
            if (fire) PlaySound(effect_onfire, player, false);
            currentlyScreamingPlayers.Add(player.userID);
            timer.Once(5f, () =>
            {
                if (player != null) currentlyScreamingPlayers.Remove(player.userID);
            });
        }

        private HashSet<BaseEntity> protectedStashes = new HashSet<BaseEntity>();

        // ReSharper disable once UnusedMember.Local
        private void OnStashExposed(StashContainer stash, BasePlayer player)
        {
            if (!HasCard(player.userID, Card.DogDoo)) return;
            if (protectedStashes.Contains(stash)) return;
            protectedStashes.Add(stash);
            timer.Once(2f, () =>
            {
                if (player == null) return;
                var entity = GameManager.server.CreateEntity(
                    "assets/prefabs/deployable/landmine/landmine.prefab", player.transform.position);
                var landmine = entity as Landmine;
                entity.Spawn();
                landmine.Arm();
                landmine.SendNetworkUpdateImmediate();
            });

            timer.Once(120f, () =>
            {
                if (stash != null && !stash.IsDead()) protectedStashes.Remove(stash);
            });
        }

        private static BasePlayer GetPlayerWithName(string displayName)
        {
            return BasePlayer.allPlayerList.FirstOrDefault(p =>
                p.displayName.ToLower().Contains(displayName.ToLower()));
        }

        private BaseEntity RaycastFirstEntity(Ray ray, float distance)
        {
            RaycastHit hit;
            return Physics.Raycast(ray.origin, ray.direction, out hit, distance) ? hit.GetEntity() : null;
        }

        private void Initialize()
        {
            Unsubscribe("OnPlayerVoice");
            Unsubscribe("OnEntityKill");
            LoadData();
            permission.RegisterPermission(permission_admin, this);
            var cards = Enum.GetValues(typeof(Card));
            foreach (Card card in cards) cardAliases[card.ToString().ToLower()] = card;
            foreach (var alias in cardAliases.Keys)
            {
                cmd.AddChatCommand(alias, this, nameof(GenericChatCommand));
                cmd.AddConsoleCommand(alias, this, nameof(GenericConsoleCommand));
            }

            timer.Once(0.5f, () =>
            {
                if (Payback2 == null)
                    Puts("\nPayback2 is not loaded! Check out the sequel to this plugin, " +
                         "there's a lot more Payback to be had at " +
                         "https://payback.fragmod.com !\n");
            });
        }

        private void GenericChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!IsAdmin(player)) return;
            var argsTogether = args.Aggregate("", (current, arg) => current + (arg + " "));
            Card card;
            if (cardAliases.TryGetValue(command.ToLower(), out card)) AdminCommandToggleCard(player, card, args);
        }

        private void GenericConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null) return;
            var player = arg.Connection?.player as BasePlayer;
            if (player != null)
                if (!IsAdmin(player))
                    return;
            if (arg.cmd == null) return;
            var argsTogether = "";
            if (arg.Args != null)
                argsTogether = arg.Args.Aggregate(argsTogether, (current, param) => current + (param + " "));
            var command = string.Empty;
            if (arg.cmd.Name != null) command = arg.cmd.Name;
            Card card;
            if (!cardAliases.TryGetValue(command.ToLower(), out card)) return;
            if (arg.Args == null) arg.Args = new string[0];
            AdminCommandToggleCard(player, card, arg.Args);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            Initialize();
        }

        private static List<string> _viewInventoryHooks = new List<string>
            {"OnLootEntityEnd", "CanMoveItem", "OnEntityDeath"};

        private void ViewTargetPlayerInventory(BasePlayer target, BasePlayer admin)
        {
            if (admin == null) return;
            if (admin.IsSpectating())
            {
                PrintToPlayer(admin,
                    $"{UI2.ColorText($"[PAYBACK WARNING] ", "yellow")} : {UI2.ColorText("cannot open target's inventory while spectating! you must respawn", "white")}");
                return;
            }

            PrintToPlayer(admin,
                $"{UI2.ColorText("[PAYBACK WARNING] ", "yellow")} : {UI2.ColorText("you must exit the F1 console immediately after using the command to view inventory", "white")}");
            ViewInvCmd(admin.IPlayer, "ViewInvCmd", new[] {$"{target.userID}"});
        }


        #region ViewInventoryCommands

        // ReSharper disable once UnusedParameter.Local
        private void ViewInvCmd(IPlayer iplayer, string command, string[] args)
        {
            var player = iplayer.Object as BasePlayer;
            if (player == null) return;
            if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
            {
                RaycastHit hitinfo;
                if (!Physics.Raycast(player.eyes.HeadRay(), out hitinfo, 3f, Layers.Server.Players))
                {
                    ChatMessage(iplayer, "NoPlayersFoundRayCast");
                    return;
                }

                var targetplayerhit = hitinfo.GetEntity().ToPlayer();
                if (targetplayerhit == null)
                {
                    ChatMessage(iplayer, "NoPlayersFoundRayCast");
                    return;
                }

                ViewInventory(player, targetplayerhit);
                return;
            }

            var target = FindPlayer(args[0]);
            if (target == null) return;
            var targetplayer = target.Object as BasePlayer;
            if (targetplayer == null) return;
            ViewInventory(player, targetplayer);
        }

        #endregion Commands

        #region Methods

        private List<LootableCorpse> _viewingcorpse = new List<LootableCorpse>();

        private void ViewInventory(BasePlayer player, BasePlayer targetplayer)
        {
            if (_viewingcorpse.Count == 0)
                SubscribeToHooks();

            player.EndLooting();

            var corpse = GetLootableCorpse(targetplayer.displayName);
            corpse.SendAsSnapshot(player.Connection);

            timer.Once(1f, () => { StartLooting(player, targetplayer, corpse); });
        }

        private static LootableCorpse GetLootableCorpse(string title = "")
        {
            var corpse =
                GameManager.server.CreateEntity(StringPool.Get(2604534927), Vector3.zero) as LootableCorpse;
            corpse.CancelInvoke(nameof(BaseCorpse.RemoveCorpse));
            corpse.syncPosition = false;
            corpse.limitNetworking = true;
            corpse.playerName = title;
            corpse.playerSteamID = 0;
            corpse.enableSaving = false;
            corpse.Spawn();
            corpse.SetFlag(BaseEntity.Flags.Locked, true);
            Buoyancy bouyancy;
            if (corpse.TryGetComponent(out bouyancy)) UnityEngine.Object.Destroy(bouyancy);
            Rigidbody ridgidbody;
            if (corpse.TryGetComponent(out ridgidbody)) UnityEngine.Object.Destroy(ridgidbody);
            return corpse;
        }

        private void StartLooting(BasePlayer player, BasePlayer targetplayer, LootableCorpse corpse)
        {
            player.inventory.loot.AddContainer(targetplayer.inventory.containerMain);
            player.inventory.loot.AddContainer(targetplayer.inventory.containerWear);
            player.inventory.loot.AddContainer(targetplayer.inventory.containerBelt);
            player.inventory.loot.entitySource = corpse;
            player.inventory.loot.PositionChecks = false;
            player.inventory.loot.MarkDirty();
            player.inventory.loot.SendImmediate();
            player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "player_corpse");
            _viewingcorpse.Add(corpse);
        }

        // ReSharper disable once UnusedMember.Local
        private void StartLootingContainer(BasePlayer player, ItemContainer container, LootableCorpse corpse)
        {
            player.inventory.loot.AddContainer(container);
            player.inventory.loot.entitySource = corpse;
            player.inventory.loot.PositionChecks = false;
            player.inventory.loot.MarkDirty();
            player.inventory.loot.SendImmediate();
            player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "player_corpse");
            _viewingcorpse.Add(corpse);
        }

        #endregion Methods

        #region Hooks

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnLootEntityEnd(BasePlayer player, LootableCorpse corpse)
        {
            if (!_viewingcorpse.Contains(corpse)) return;
            _viewingcorpse.Remove(corpse);
            if (corpse != null) corpse.Kill();
            if (_viewingcorpse.Count == 0) UnSubscribeFromHooks();
        }


        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnEntityDeath(LootableCorpse corpse, HitInfo info)
        {
            if (!_viewingcorpse.Contains(corpse)) return;
            _viewingcorpse.Remove(corpse);
            if (corpse != null) corpse.Kill();
            if (_viewingcorpse.Count == 0) UnSubscribeFromHooks();
        }

        #endregion Hooks

        #region Helpers

        private static IPlayer FindPlayer(string nameOrId)
        {
            return BasePlayer.activePlayerList
                .FirstOrDefault(x => x.UserIDString == nameOrId || x.displayName.Contains(nameOrId)).IPlayer;
        }

        // ReSharper disable once UnusedMember.Local
        private bool HasPerm(string id, string perm) => permission.UserHasPermission(id, perm);

        private string GetLang(string langKey, string playerId = null, params object[] args) =>
            string.Format(lang.GetMessage(langKey, this, playerId), args);

        private void ChatMessage(IPlayer player, string langKey, params object[] args)
        {
            if (player.IsConnected) player.Message(GetLang(langKey, player.Id, args));
        }

        private void UnSubscribeFromHooks()
        {
            foreach (var hook in _viewInventoryHooks) Unsubscribe(hook);
        }

        private void SubscribeToHooks()
        {
            foreach (var hook in _viewInventoryHooks) Subscribe(hook);
        }

        #endregion

        private static string filename_data => "Payback/Payback.dat";
        private DynamicConfigFile file_payback_data;
        public PaybackData paybackData = new PaybackData();

        public class PaybackData
        {
            public float percent_butterfingers_dropchance = 0.3f;
            public HashSet<ulong> bancheck_exceptions = new HashSet<ulong>();
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            Worker.GetSingleton()?.StopAllCoroutines();
            UnityEngine.Object.Destroy(Worker.GetSingleton());
            foreach (var player in BasePlayer.activePlayerList) UI2.ClearUI(player);
            SaveData();
        }

        private void SaveData()
        {
            file_payback_data.WriteObject(paybackData);
        }

        private void LoadData()
        {
            ReadDataIntoDynamicConfigFiles();
            LoadFromDynamicConfigFiles();
        }

        private void ReadDataIntoDynamicConfigFiles()
        {
            file_payback_data = Interface.Oxide.DataFileSystem.GetFile(filename_data);
        }

        private void LoadFromDynamicConfigFiles()
        {
            try
            {
                paybackData = file_payback_data.ReadObject<PaybackData>();
            }
            catch (Exception)
            {
                paybackData = new PaybackData();
            }
        }

        public const string permission_admin = "payback.admin";

        public bool IsAdmin(BasePlayer player)
        {
            return permission.UserHasPermission(player.Connection.userid.ToString(), permission_admin);
        }

        // ReSharper disable once UnusedMember.Local
        private void SetDespawnDuration(DroppedItem dropped, float seconds)
        {
            dropped.Invoke(dropped.IdleDestroy, seconds);
        }

        private static void DestroyGroundCheck(BaseEntity entity)
        {
            UnityEngine.Object.DestroyImmediate(entity.GetComponentInChildren<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(entity.GetComponentInChildren<GroundWatch>());
        }

        [ChatCommand("sound")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void SoundCommand(BasePlayer player, string command, string[] args)
        {
            if (!IsAdmin(player)) return;

            if (args.Length == 0)
            {
                SendReply(player, "/sound <asset>");
                return;
            }

            foreach (var sound in args) PlaySound(sound, player, false);
        }

        private void PrintToPlayer(BasePlayer player, string text)
        {
            if (player == null)
            {
                Puts($"{text}");
                return;
            }

            player.SendConsoleCommand($"echo {text}");
        }

        private HashSet<ulong> GetPlayerTeam(ulong userID)
        {
            var player = BasePlayer.FindByID(userID);

            var existingTeam = RelationshipManager.ServerInstance.FindPlayersTeam(player.userID);
            return existingTeam != null ? new HashSet<ulong>(existingTeam.members) : new HashSet<ulong>() {userID};
        }

        // ReSharper disable once UnusedMember.Global
        public void PlaySound(List<string> effects, BasePlayer player, Vector3 worldPosition, bool playlocal = true)
        {
            if (player == null) return;
            foreach (var sound in effects.Select(effect => new Effect(effect, worldPosition, Vector3.up)))
            {
                if (playlocal) EffectNetwork.Send(sound, player.net.connection);
                else EffectNetwork.Send(sound);
            }
        }

        private static void PlaySound(List<string> effects, BasePlayer player, bool playlocal = true)
        {
            if (player == null) return;
            foreach (var sound in effects.Select(effect =>
                         new Effect(effect, player, 0, Vector3.zero + Vector3.up * 0.5f, Vector3.forward)))
            {
                if (playlocal) EffectNetwork.Send(sound, player.net.connection);
                else EffectNetwork.Send(sound);
            }
        }

        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once UnusedMember.Local
        private void PlaySound(string effect, ListHashSet<BasePlayer> players, bool playlocal = true)
        {
            foreach (var player in players) PlaySound(effect, player, playlocal);
        }

        private static void PlaySound(string effect, BasePlayer player, bool playlocal = true,
            Vector3 posLocal = default(Vector3))
        {
            if (player == null) return;
            var sound = new Effect(effect, player, 0, Vector3.zero, Vector3.forward);
            if (posLocal != Vector3.zero)
                sound = new Effect(effect, player.transform.position + posLocal, Vector3.forward);
            if (playlocal) EffectNetwork.Send(sound, player.net.connection);
            else EffectNetwork.Send(sound);
        }

        private static void PlayGesture(BasePlayer target, string gestureName, bool canCancel = false)
        {
            if (target == null) return;
            if (target.gestureList == null) return;
            var gesture = target.gestureList.StringToGesture(gestureName);
            if (gesture == null) return;
            var saveCanCancel = gesture.canCancel;
            gesture.canCancel = canCancel;
            target.SendMessage("Server_StartGesture", gesture);
            gesture.canCancel = saveCanCancel;
        }

        public class Worker : MonoBehaviour
        {
            public static Worker GetSingleton()
            {
                if (_singleton != null) return _singleton;
                var worker = new GameObject {name = "Worker Singleton"};
                _singleton = worker.AddComponent<Worker>();
                return _singleton;
            }

            private static Worker _singleton;

            public static Coroutine StaticStartCoroutine(IEnumerator c)
            {
                return GetSingleton().StartCoroutine(c);
            }
        }


        #region Config

        // ReSharper disable once UnusedMember.Local
        private void Init()
        {
            LoadConfig();
        }

        private PluginConfig config;

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig();
            SaveConfig();
        }

        private class PluginConfig
        {
            [JsonProperty("Check temporary game bans and notify via Discord Webhook")]
            public bool enabled_nexus_gamebancheck = true;

            [JsonProperty("Only report temp bans younger than days")]
            public int nexus_ban_days = 60;

            [JsonProperty("Automatically ban players with a temp ban")]
            public bool autoban_tempbans = false;

            [JsonProperty("Ban appeal message")] public string autoban_banmessage = "";

            [JsonProperty("Notify Game Ban + Team")]
            public bool notify_game_ban = true;

            [JsonProperty("Only notify ban if has team")]
            public bool notify_only_if_has_team = false;

            [JsonProperty("Include bm links in ban notification")]
            public bool notify_ban_include_bm = false;

            [JsonProperty("These discord webhooks will get notified")]
            public List<string> webhooks = new List<string>();

            [JsonProperty("notify player being attacked by cheater")]
            public bool notifyCheaterAttacking = true;
        }

        #endregion Config

        private class UI2
        {
            public static Vector4 vectorFullscreen = new Vector4(0, 0, 1, 1);

            public static string ColorText(string input, string color)
            {
                return "<color=" + color + ">" + input + "</color>";
            }

            public static void ClearUI(BasePlayer player)
            {
                foreach (var guid in guids) CuiHelper.DestroyUi(player, guid);
            }

            private static Dictionary<ulong, HashSet<string>> dirtyMap = new Dictionary<ulong, HashSet<string>>();

            // ReSharper disable once UnusedMember.Local
            public static HashSet<string> GetDirtyBitsForPlayer(BasePlayer player)
            {
                if (player == null) return new HashSet<string>();
                if (!dirtyMap.ContainsKey(player.userID)) dirtyMap[player.userID] = new HashSet<string>();
                return dirtyMap[player.userID];
            }

            // ReSharper disable once UnusedType.Local
            public class Layout
            {
                private Vector2 startPosition;
                private Vector4 cellBounds;
                private Vector2 padding;
                private Vector4 cursor;
                private int maxRows;
                private int row;
                private int col;

                // ReSharper disable once UnusedMember.Local
                public void Init(Vector2 _startPosition, Vector4 _cellBounds, int _maxRows, Vector2 _padding)
                {
                    startPosition = _startPosition;
                    cellBounds = _cellBounds;
                    maxRows = _maxRows;
                    padding = _padding;
                    row = 0;
                    col = 0;
                }

                // ReSharper disable once UnusedMember.Local
                public void NextCell(Action<Vector4, int, int> populateAction)
                {
                    var cellX = startPosition.x + (col * (cellBounds.z + padding.x)) + padding.x / 2f;
                    var cellY = startPosition.y - (row * (cellBounds.w + padding.y)) - cellBounds.w - padding.y;
                    cursor = new Vector4(cellX, cellY, cellX, cellY);
                    populateAction(cursor, row, col);
                    row++;
                    if (row != maxRows) return;
                    row = 0;
                    col++;
                }

                // ReSharper disable once UnusedMember.Local
                public void Reset()
                {
                    row = 0;
                    col = 0;
                }
            }

            // ReSharper disable once UnusedMember.Local
            public static string ColorToHex(Color color)
            {
                return ColorUtility.ToHtmlStringRGB(color);
            }

            // ReSharper disable once UnusedMember.Local
            public static string HexToRGBAString(string hex)
            {
                Color color;
                ColorUtility.TryParseHtmlString("#" + hex, out color);
                var c = $"{color.r:0.000} {color.g:0.000} {color.b:0.000} {color.a:0.000}";
                return c;
            }

            // ReSharper disable once UnusedMember.Local
            public static Vector4 GetOffsetVector4(Vector2 offset)
            {
                return new Vector4(offset.x, offset.y, offset.x, offset.y);
            }

            // ReSharper disable once UnusedMember.Local
            public static Vector4 GetOffsetVector4(float x, float y)
            {
                return new Vector4(x, y, x, y);
            }

            // ReSharper disable once UnusedMember.Local
            public static Vector4 SubtractPadding(Vector4 input, float padding)
            {
                var verticalPadding = GetSquareFromWidth(padding);
                return new Vector4(input.x + padding / 2f, verticalPadding / 2f, input.z - padding / 2f,
                    input.w - verticalPadding / 2f);
            }

            private static float GetSquareFromWidth(float width, float aspect = 16f / 9f)
            {
                return width * aspect;
            }

            private static float GetSquareFromHeight(float height, float aspect = 16f / 9f)
            {
                return height * 1f / aspect;
            }

            // ReSharper disable once UnusedParameter.Local
            private static Vector4 MakeSquareFromWidth(Vector4 bounds, float aspect = 16f / 9f)
            {
                return new Vector4(bounds.x, bounds.y, bounds.z, bounds.y + GetSquareFromWidth(bounds.z - bounds.x));
            }

            // ReSharper disable once UnusedParameter.Local
            private static Vector4 MakeSquareFromHeight(Vector4 bounds, float aspect = 16f / 9f)
            {
                return new Vector4(bounds.x, bounds.y, bounds.x + GetSquareFromHeight(bounds.z - bounds.y), bounds.w);
            }

            // ReSharper disable once UnusedMember.Local
            public static Vector4 MakeRectFromWidth(Vector4 bounds, float ratio, float aspect = 16f / 9f)
            {
                var square = MakeSquareFromWidth(bounds, aspect);
                return new Vector4(square.x, square.y, square.z, square.y + (square.w - square.y) * ratio);
            }

            // ReSharper disable once UnusedMember.Local
            public static Vector4 MakeRectFromHeight(Vector4 bounds, float ratio, float aspect = 16f / 9f)
            {
                var square = MakeSquareFromHeight(bounds, aspect);
                return new Vector4(square.x, square.y, square.x + (square.z - square.x) * ratio, square.w);
            }

            public static HashSet<string> guids = new HashSet<string>();

            private static string GetMinUI(Vector4 panelPosition)
            {
                return panelPosition.x.ToString("0.####") + " " + panelPosition.y.ToString("0.####");
            }

            private static string GetMaxUI(Vector4 panelPosition)
            {
                return panelPosition.z.ToString("0.####") + " " + panelPosition.w.ToString("0.####");
            }

            // ReSharper disable once UnusedMember.Local
            public static string GetColorString(Vector4 color)
            {
                return color.x.ToString("0.####") + " " + color.y.ToString("0.####") + " " +
                       color.z.ToString("0.####") + " " + color.w.ToString("0.####");
            }

            // ReSharper disable once UnusedMember.Local
            public static CuiElement CreateInputField(CuiElementContainer container, string parent, string panelName,
                string message, int textSize, string color, Vector4 bounds, string command)
            {
                var element = new CuiElement
                {
                    Name = panelName,
                    Parent = parent,
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            Color = color,
                            Command = command,
                            FontSize = textSize,
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = GetMinUI(bounds),
                            AnchorMax = GetMaxUI(bounds),
                        }
                    }
                };
                container.Add(element);
                return element;
            }

            private static void CreateOutlineLabel(CuiElementContainer container, string parent, string panelName,
                string message, string color, int size, Vector4 bounds,
                TextAnchor textAlignment = TextAnchor.MiddleCenter, float fadeOut = 0, float fadeIn = 0,
                string outlineColor = "0 0 0 0.8", string outlineDistance = "0.7 -0.7")
            {
                container.Add(new CuiElement
                {
                    Name = panelName,
                    Parent = parent,
                    FadeOut = fadeOut,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = textAlignment,
                            Color = color,
                            FadeIn = fadeIn,
                            FontSize = size,
                            Text = message
                        },
                        new CuiOutlineComponent
                        {
                            Color = outlineColor,
                            Distance = outlineDistance,
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = GetMinUI(bounds),
                            AnchorMax = GetMaxUI(bounds),
                        }
                    }
                });
            }

            // ReSharper disable once UnusedMember.Local
            public static void CreateLabel(CuiElementContainer container, string parent, string panelName,
                string message, string color, int size, string aMin, string aMax,
                TextAnchor textAlignment = TextAnchor.MiddleCenter, float fadeIn = 0, float fadeOut = 0)
            {
                var label = new CuiLabel
                {
                    Text =
                    {
                        Text = message,
                        Align = textAlignment,
                        Color = color,
                        FontSize = size,
                        FadeIn = fadeIn
                    },
                    RectTransform =
                    {
                        AnchorMin = aMin,
                        AnchorMax = aMax
                    },
                    FadeOut = fadeOut
                };

                container.Add(label, parent, panelName);
            }

            // ReSharper disable once UnusedMethodReturnValue.Local
            public static CuiButton CreateButton(CuiElementContainer container, string parent, string panelName,
                string color, string text, int size, Vector4 bounds, string command,
                TextAnchor align = TextAnchor.MiddleCenter, string textColor = "1 1 1 1")
            {
                container.Add(new CuiElement
                {
                    Name = panelName,
                    Parent = parent,
                    Components =
                    {
                        new CuiButtonComponent
                        {
                            Color = color,
                            Command = command,
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = GetMinUI(bounds),
                            AnchorMax = GetMaxUI(bounds),
                        }
                    }
                });

                CreateOutlineLabel(container, panelName, "text", text, textColor, size, new Vector4(0, 0, 1, 1), align);
                return null;
            }


            // ReSharper disable once UnusedMethodReturnValue.Local
            public static CuiPanel CreatePanel(CuiElementContainer container, string parent, string panelName,
                string color, Vector4 bounds, string imageUrl = "", bool cursor = false, float fadeOut = 0,
                float fadeIn = 0, bool png = false, bool blur = false, bool outline = true)
            {
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    if (png)
                    {
                        if (outline)
                        {
                            container.Add(new CuiElement
                            {
                                Name = panelName,
                                Parent = parent,
                                FadeOut = fadeOut,
                                Components =
                                {
                                    new CuiRawImageComponent
                                    {
                                        Color = color,
                                        Png = imageUrl,
                                        FadeIn = fadeIn
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = GetMinUI(bounds),
                                        AnchorMax = GetMaxUI(bounds),
                                    },
                                    new CuiOutlineComponent
                                    {
                                        Color = "0 0 0 0.9",
                                        Distance = "0.7 -0.7",
                                    },
                                }
                            });
                        }
                        else
                        {
                            container.Add(new CuiElement
                            {
                                Name = panelName,
                                Parent = parent,
                                FadeOut = fadeOut,
                                Components =
                                {
                                    new CuiRawImageComponent
                                    {
                                        Color = color,
                                        Png = imageUrl,
                                        FadeIn = fadeIn
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = GetMinUI(bounds),
                                        AnchorMax = GetMaxUI(bounds),
                                    }
                                }
                            });
                        }
                    }
                    else
                    {
                        container.Add(new CuiElement
                        {
                            Name = panelName,
                            Parent = parent,
                            FadeOut = fadeOut,
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Color = color,
                                    Url = imageUrl,
                                    FadeIn = fadeIn
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = GetMinUI(bounds),
                                    AnchorMax = GetMaxUI(bounds),
                                }
                            }
                        });
                    }

                    return null;
                }
                else
                {
                    if (blur)
                    {
                        const string mat = "assets/content/ui/uibackgroundblur-ingamemenu.mat";
                        container.Add(new CuiElement
                        {
                            Name = panelName,
                            Parent = parent,
                            FadeOut = fadeOut,
                            Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = color,
                                    Material = mat,
                                    FadeIn = fadeIn
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = GetMinUI(bounds),
                                    AnchorMax = GetMaxUI(bounds),
                                }
                            }
                        });
                    }
                    else
                    {
                        var element = new CuiPanel
                        {
                            RectTransform =
                            {
                                AnchorMin = GetMinUI(bounds),
                                AnchorMax = GetMaxUI(bounds)
                            },
                            Image =
                            {
                                Color = color,
                                FadeIn = fadeIn
                            },
                            CursorEnabled = cursor,
                            FadeOut = fadeOut
                        };
                        container.Add(element, parent, panelName);
                        return element;
                    }

                    return null;
                }
            }
        }
    }
}