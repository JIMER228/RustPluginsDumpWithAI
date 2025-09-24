using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;
using Layer = Rust.Layer;

namespace Oxide.Plugins
{
    [Info("ZealRaidManager", "Kira", "1.0.0")]
    public class ZealRaidManager : RustPlugin
    {
        #region [Reference] / [Ссылки]

        private static ZealRaidManager _;

        #endregion
 
        #region [Vars] / [Переменные]

        private StoredData _dataBase = new StoredData();

        private Dictionary<Vector3, RaidManagerRaidZone> _zones = new Dictionary<Vector3, RaidManagerRaidZone>();


        public Dictionary<string, float> UnblockEntities = new Dictionary<string, float>
        {
            ["assets/prefabs/deployable/barricades/barricade.concrete.prefab"] = 30,
            ["assets/prefabs/deployable/water catcher/water_catcher_large.prefab"] = 30,
            ["assets/prefabs/building/door.hinged/door.hinged.toptier.prefab"] = 5,
            ["assets/prefabs/building/wall.frame.shopfront/wall.frame.shopfront.prefab"] = 30,
            ["assets/prefabs/building/wall.external.high.wood/wall.external.high.wood.prefab"] = 30,
            ["assets/prefabs/building/wall.external.high.stone/wall.external.high.stone.prefab"] = 15,
            ["assets/prefabs/misc/xmas/icewalls/wall.external.high.ice.prefab"] = 15,
            ["assets/prefabs/building/gates.external.high/gates.external.high.wood/gates.external.high.wood.prefab"] = 30,
            ["assets/prefabs/building/gates.external.high/gates.external.high.stone/gates.external.high.stone.prefab"] = 15,
            ["assets/prefabs/building/wall.frame.garagedoor/wall.frame.garagedoor.prefab"] = 5,
            ["assets/prefabs/building/door.hinged/door.hinged.metal.prefab"] = 5,
            ["assets/prefabs/building/door.double.hinged/door.double.hinged.toptier.prefab"] = 5,
            ["assets/prefabs/building/door.double.hinged/door.double.hinged.wood.prefab"] = 30,
            ["assets/prefabs/building/door.double.hinged/door.double.hinged.metal.prefab"] = 5,
            ["assets/prefabs/deployable/barricades/barricade.cover.wood.prefab"] = 50,
            ["assets/prefabs/deployable/barricades/barricade.wood.prefab"] = 25,
            ["assets/prefabs/deployable/barricades/barricade.woodwire.prefab"] = 25,
            ["assets/prefabs/building/door.hinged/door.hinged.wood.prefab"] = 30,
            ["assets/prefabs/building/ladder.wall.wood/ladder.wooden.wall.prefab"] = 100,
            ["assets/prefabs/building/wall.window.bars/wall.window.bars.wood.prefab"] = 30,
            ["assets/prefabs/building/wall.window.shutter/shutter.wood.a.prefab"] = 50,
            ["assets/prefabs/deployable/barricades/barricade.stone.prefab"] = 30,
            ["assets/prefabs/misc/xmas/icewalls/icewall.prefab"] = 30,
            ["assets/prefabs/building/floor.ladder.hatch/floor.ladder.hatch.prefab"] = 5,
            ["assets/prefabs/deployable/water catcher/water_catcher_small.prefab"] = 20,
            ["assets/prefabs/deployable/barricades/barricade.metal.prefab"] = 5,
            ["assets/prefabs/building/wall.window.embrasure/shutter.metal.embrasure.b.prefab"] = 15,
            ["assets/prefabs/building/wall.frame.shopfront/wall.frame.shopfront.metal.prefab"] = 5,
            ["assets/prefabs/building/wall.window.embrasure/shutter.metal.embrasure.a.prefab"] = 15,
            ["assets/prefabs/building/wall.window.bars/wall.window.bars.metal.prefab"] = 5,
            ["assets/prefabs/deployable/barricades/barricade.sandbags.prefab"] = 50,
            ["assets/prefabs/building/floor.grill/floor.grill.prefab"] = 5,
            ["assets/prefabs/building/wall.frame.fence/wall.frame.fence.gate.prefab"] = 25,
            ["assets/prefabs/building/wall.frame.fence/wall.frame.fence.prefab"] = 25,
            ["assets/prefabs/building/watchtower.wood/watchtower.wood.prefab"] = 150,
            ["assets/prefabs/building/floor.triangle.grill/floor.triangle.grill.prefab"] = 5,
            ["assets/prefabs/building/floor.triangle.ladder.hatch/floor.triangle.ladder.hatch.prefab"] = 5,
            ["assets/prefabs/building/wall.frame.cell/wall.frame.cell.gate.prefab"] = 5,
            ["assets/prefabs/building/wall.frame.cell/wall.frame.cell.prefab"] = 5,
            ["assets/prefabs/building/wall.window.bars/wall.window.bars.toptier.prefab"] = 10,
            ["assets/prefabs/building/wall.window.reinforcedglass/wall.window.glass.reinforced.prefab"] = 10,
            ["assets/prefabs/deployable/furnace.large/furnace.large.prefab"] = 150,
            ["assets/prefabs/deployable/planters/planter.large.deployed.prefab"] = 50,
            ["assets/prefabs/deployable/large wood storage/box.wooden.large.prefab"] = 150,
            ["assets/prefabs/misc/xmas/stockings/stocking_large_deployed.prefab"] = 50,
            ["assets/prefabs/deployable/liquidbarrel/waterbarrel.prefab"] = 50,
            ["assets/prefabs/deployable/tier 2 workbench/workbench2.deployed.prefab"] = 50,
            ["assets/prefabs/deployable/tier 1 workbench/workbench1.deployed.prefab"] = 50,
            ["assets/prefabs/deployable/tier 3 workbench/workbench3.deployed.prefab"] = 50,
            ["assets/prefabs/deployable/composter/composter.prefab"] = 50,
            ["assets/prefabs/deployable/campfire/campfire.prefab"] = 50,
            ["assets/prefabs/misc/halloween/skull_fire_pit/skull_fire_pit.prefab"] = 50,
            ["assets/prefabs/deployable/bed/bed_deployed.prefab"] = 300,
            ["assets/prefabs/deployable/oil refinery/refinery_small_deployed.prefab"] = 100,
            ["assets/prefabs/deployable/furnace/furnace.prefab"] = 500,
            ["assets/prefabs/deployable/mailbox/mailbox.deployed.prefab"] = 50,
            ["assets/prefabs/deployable/repair bench/repairbench_deployed.prefab"] = 50,
            ["assets/prefabs/deployable/shelves/shelves.prefab"] = 250,
            ["assets/prefabs/deployable/sleeping bag/sleepingbag_leather_deployed.prefab"] = 200,
            ["assets/prefabs/deployable/research table/researchtable_deployed.prefab"] = 50,
            ["assets/prefabs/deployable/mixingtable/mixingtable.deployed.prefab"] = 50,
            ["assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab"] = 25,
            ["assets/prefabs/deployable/fridge/fridge.deployed.prefab"] = 25,
            ["assets/prefabs/deployable/locker/locker.deployed.prefab"] = 25,
            ["assets/prefabs/npc/sam_site_turret/sam_site_turret_deployed.prefab"] = 250,
            ["assets/prefabs/deployable/single shot trap/guntrap.deployed.prefabb"] = 75,
            ["assets/prefabs/deployable/bear trap/beartrap.prefab"] = 25,
            ["assets/prefabs/deployable/landmine/landmine.prefab"] = 25,
            ["assets/prefabs/npc/flame turret/flameturret.deployed.prefab"] = 75,
            ["assets/prefabs/misc/halloween/coffin/coffinstorage.prefab"] = 300,
            ["assets/prefabs/misc/halloween/graveyard_fence/graveyardfence.prefab"] = 25,
            ["assets/prefabs/deployable/fridge/fridge.deployed.prefab"] = 25,
            ["assets/prefabs/misc/halloween/cursed_cauldron/cursedcauldron.deployed.prefab"] = 50,
            ["assets/prefabs/npc/autoturret/autoturret_deployed.prefab"] = 250,
            ["assets/prefabs/deployable/playerioents/generators/solar_panels_roof/solarpanel.large.deployed.prefab"] = 100,
            ["assets/prefabs/deployable/playerioents/batteries/large/large.rechargable.battery.deployed.prefab"] = 25,
            ["assets/prefabs/deployable/computerstation/computerstation.deployed.prefab"] = 50,
            ["assets/prefabs/deployable/playerioents/generators/fuel generator/small_fuel_generator.deployed.prefab"] = 100,
            ["assets/prefabs/deployable/playerioents/batteries/medium/medium.rechargable.battery.deployed.prefab"] = 25,
            ["assets/prefabs/deployable/playerioents/generators/generator.small.prefab"] = 25,
            ["assets/prefabs/deployable/playerioents/poweredwaterpurifier/poweredwaterpurifier.deployed.prefab"] = 50
        };

        private readonly List<string> _blockedCommands = new List<string>
            {"kit", "tpr", "home", "duel", "combat", "trade", "remove", "up", "store", "sethome"};

        #endregion

        #region [MonoBehaviours]

        public class RaidManagerRaidZone : MonoBehaviour
        {
            public int id;
            public BaseEntity ent;
            public Vector3 position;
            public List<ulong> players;
            private Dictionary<ulong, RaidManagerRaidBlock> _raidBlocks;
            public SphereCollider zone;
            public int raidTime = 240;
            public int currentTime;
            private StoredData _dataBase;

            private void Awake()
            {
                ent = gameObject.GetComponentInParent<BaseEntity>();
                _dataBase = _._dataBase;
                var o = gameObject;
                o.layer = (int) Layer.Reserved1;
                position = o.transform.position;
                players = new List<ulong>();
                currentTime = raidTime;
                _raidBlocks = new Dictionary<ulong, RaidManagerRaidBlock>();

                Invoke(nameof(InitializedZone), 1f);
            }

            public void Start_Timer()
            {
                InvokeRepeating(nameof(Timer), 1f, 1f);
            }

            public void Timer()
            {
                currentTime--;
                if (currentTime <= 0) Destroy(this);
            }

            public void Reload()
            {
                CancelInvoke(nameof(Start_Timer));
                currentTime = raidTime;
                foreach (var raidBlock in _raidBlocks)
                {
                    if (players.Contains(raidBlock.Value.player.userID))
                    {
                        raidBlock.Value.currentTime = currentTime;
                    }
                }

                Start_Timer();
            }

            public void InitializedZone()
            {
                if (_._zones.ContainsKey(position))
                {
                    Destroy(this);
                    return;
                }

                if (_.CheckZones(gameObject))
                {
                    _.PrintError("Попытка стаковать зоны (Временное сообщение)");
                    Destroy(this);
                    return;
                }

                var position1 = gameObject.transform.position;
                _._zones.Add(position, this);
                _._dataBase.RaidZones.Add(position1, new StoredData.RaidZone
                {
                    Players = players,
                    Position = position,
                    CurrentTime = currentTime
                });
                _dataBase.CountZones++;
                id = _dataBase.CountZones;

                zone = gameObject.AddComponent<SphereCollider>();
                zone.radius = 100;
                zone.isTrigger = true;
                zone.transform.position = position;
                zone.name = id.ToString();

                _.PrintWarning($"Инициализированна зона #{zone.name} | {zone.transform.position}");
                Start_Timer();
            }

            private void OnTriggerEnter(Collider collider)
            {
                var ent = collider.ToBaseEntity();
                if (ent == null) return;
                if (!ent.IsValid()) return;
                if (!(ent is BasePlayer)) return;
                var player = ent as BasePlayer;
                if (!players.Contains(player.userID) & _raidBlocks.ContainsKey(player.userID))
                {
                    _raidBlocks[player.userID].currentTime = currentTime;
                    player.ChatMessage("Вы вошли в зону рейда");
                    players.Add(player.userID);
                    return;
                }

                var raidBlock = player.gameObject.AddComponent<RaidManagerRaidBlock>();
                raidBlock.currentTime = currentTime;
                _raidBlocks.Add(player.userID, raidBlock);
                players.Add(player.userID);
                player.ChatMessage("Вы вошли в зону рейда");
            }

            private void OnTriggerExit(Collider collider)
            {
                var ent = collider.ToBaseEntity();
                if (ent == null) return;
                if (!ent.IsValid()) return;
                if (!(ent is BasePlayer)) return;
                var player = ent as BasePlayer;
                players.Remove(player.userID);
                player.ChatMessage("Вы вышли из зоны рейда");
            }

            public void OnDestroy()
            {
                _.PrintWarning($"Destroying {zone.name}");
                Destroy(zone);
                ent.Kill();
                _dataBase.RaidZones.Remove(position);
                _._zones.Remove(gameObject.transform.position);
                foreach (var block in _raidBlocks)
                    Destroy(block.Value);
            }
        }

        public class RaidManagerRaidBlock : MonoBehaviour
        {
            public BasePlayer player;
            public int currentTime;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                DrawUI_Layer();
                InvokeRepeating(nameof(Timer), 1f, 1f);
            }

            public void Timer()
            {
                DrawUI_UpdateLayer();
                currentTime--;
                if (currentTime <= 0) Destroy(this);
            }

            public void DrawUI_Layer()
            {
                CuiElementContainer ui = new CuiElementContainer();

                ui.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image =
                    {
                        Color = HexToRustFormat("#9C8E8217")
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.3807287 0.1527777",
                        AnchorMax = "0.604 0.1787037"
                    }
                }, "Hud", "UI_RaidBlock_Layer");

                CuiHelper.DestroyUi(player, "UI_RaidBlock_Layer");
                CuiHelper.AddUi(player, ui);
                DrawUI_UpdateLayer();
            }

            public void DrawUI_UpdateLayer()
            {
                var ui = new CuiElementContainer();
                const double step = 0.0033333333333333;
                ui.Add(new CuiElement
                {
                    Name = "UI_RaidBlock_UpdateLayer",
                    Parent = "UI_RaidBlock_Layer",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#9C3737AD")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = $"{step * currentTime} {0.95}"
                        }
                    }
                });
                ui.Add(new CuiElement
                {
                    Name = "UI_RaidBlock_Text",
                    Parent = "UI_RaidBlock_Layer",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("A8A6A5BE"),
                            FontSize = 15,
                            Text = $"БЛОКИРОВКА : {currentTime} сек"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#00000042"),
                            Distance = "0.5 0.5"
                        }
                    }
                });

                CuiHelper.DestroyUi(player, "UI_RaidBlock_Text");
                CuiHelper.DestroyUi(player, "UI_RaidBlock_UpdateLayer");
                CuiHelper.AddUi(player, ui);
            }

            private void OnDestroy()
            {
                CuiHelper.DestroyUi(player, "UI_RaidBlock_Layer");
                Destroy(this);
            }
        }

        #endregion

        #region [Hooks] / [Крюки]

        private void OnServerInitialized()
        {
            _ = this;
            LoadData();
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!(entity is StabilityEntity)) return;
            if (info.damageTypes.GetMajorityDamageType() == DamageType.Decay) return;
            if (entity.OwnerID == info.InitiatorPlayer.OwnerID) return;
            var block = entity as BuildingBlock;
            if (!ReferenceEquals(block, null) && block.currentGrade.gradeBase.type == BuildingGrade.Enum.Twigs) return;
            var cupboard = entity.GetBuildingPrivilege();
            if (cupboard == null) return;
            var cupboardPos = cupboard.transform.position;
            if (cupboard == null) return;
            if (_zones.ContainsKey(cupboardPos))
            {
                _zones[cupboardPos].Reload();
                return;
            }

            var zone = GameManager.server.CreateEntity(
                "assets/prefabs/deployable/playerioents/detectors/laserdetector/laserdetector.prefab",
                cupboardPos);
            zone.Spawn();

            timer.Once(Core.Random.Range(1, 2), (() =>
            {
                var component = zone.gameObject.AddComponent<RaidManagerRaidZone>();
                component.position = cupboardPos;
                var player = BasePlayer.FindByID(entity.OwnerID);
                if (player == null) return;
                player.ChatMessage("Вашу постройку начали рейдить, скорей возвращайся домой");
            }));
        }

        private object OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            if (!_blockedCommands.Contains(command) || player.GetComponent<RaidManagerRaidBlock>() == null) return null;
            player.ChatMessage("Во время рейда запрещено использование данной команды");
            return false;
        }

        private bool? OnStructureRepair(BaseCombatEntity entity, BasePlayer player)
        {
            if (player.GetComponent<RaidManagerRaidBlock>() == null) return null;
            player.ChatMessage("Во время рейда запрещено чинить строения");
            return false;
        }

        private bool? CanAffordUpgrade(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade)
        {
            if (player.GetComponent<RaidManagerRaidBlock>() == null) return null;
            player.ChatMessage("Во время рейда запрещено улучшать строения");
            return false;
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            var player = plan.GetOwnerPlayer();
            var entity = go.ToBaseEntity();

            if (player == null || player.GetComponent<RaidManagerRaidBlock>() == null) return;
            if (UnblockEntities.ContainsKey(entity.PrefabName))
            {
                entity.GetComponent<StabilityEntity>().health = UnblockEntities[entity.PrefabName];
                return;
            }

            var block = entity as BuildingBlock;
            if (!ReferenceEquals(block, null))
            {
                block.BuildCost().ForEach(p =>
                {
                    var item = ItemManager.Create(p.itemDef, (int) p.amount);
                    if (!player.inventory.GiveItem(item)) item.Drop(player.transform.position, Vector3.down);
                });
            }
            else if (entity is DecayEntity)
            {
                var item = ItemManager.GetItemDefinitions().FirstOrDefault(p =>
                    p.GetComponent<ItemModDeployable>()?.entityPrefab.resourcePath == entity.PrefabName);
                if (item == null) return;

                if (!player.inventory.GiveItem(ItemManager.Create(item)))
                    ItemManager.Create(item).Drop(player.transform.position, Vector3.down);
            }

            player.ChatMessage("Во время рейда запрещена постройка новых объектов");
            NextTick((() => entity.Kill()));
        }

        private void Unload()
        {
            _dataBase.CountZones = 0;
            _dataBase.RaidZones.Clear();
            ServerMgr.Instance.StartCoroutine(Save_Zones());
            ServerMgr.Instance.StartCoroutine(Destroy_RaidBlocks());
            SaveData();
        }

        #endregion

        #region [ConsoleCommands]

        #endregion

        #region [DataBase] / [База Данных]

        public class StoredData
        {
            public int CountZones;

            public readonly Dictionary<Vector3, RaidZone> RaidZones =
                new Dictionary<Vector3, RaidZone>();

            public class RaidZone
            {
                public Vector3 Position;
                public List<ulong> Players;
                public int CurrentTime;
            }
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private bool CheckZones(GameObject obj1)
        {
            var isStack = false;
            foreach (var zone in _zones.Where(zone => obj1.ToBaseEntity().Distance2D(zone.Key) <= 300f))
                isStack = true;


            return isStack;
        }

        public void Test()
        {
            foreach (var obj in UnityEngine.Object.FindObjectsOfType<LaserDetector>()) obj.Kill();
        }

        private IEnumerator Load_Zones()
        {
            foreach (var zone in _dataBase.RaidZones)
            {
                var obj = GameManager.server.CreateEntity(
                    "assets/prefabs/deployable/playerioents/detectors/laserdetector/laserdetector.prefab",
                    zone.Value.Position);
                var raidzone = obj.gameObject.AddComponent<RaidManagerRaidZone>();
                raidzone.name = zone.Key.ToString();
                raidzone.currentTime = zone.Value.CurrentTime;
                PrintWarning($"Восстановлена зона #{zone.Key}");
            }

            yield return 0;
        }

        private IEnumerator Save_Zones()
        {
            foreach (var zone in _zones)
            {
                if (_dataBase.RaidZones.ContainsKey(zone.Key))
                {
                    PrintWarning($"Сохранена и выгружена зона #{zone.Key}");
                    UnityEngine.Object.Destroy(zone.Value);
                }

                yield return new WaitForSeconds(0.5f);
            }

            yield return 0;
        }

        private static IEnumerator Destroy_RaidBlocks()
        {
            foreach (var plobj in BasePlayer.activePlayerList)
            {
                if (plobj.GetComponent<RaidManagerRaidBlock>() != null)
                    UnityEngine.Object.Destroy(plobj.GetComponent<RaidManagerRaidBlock>());
                yield return new WaitForSeconds(0.5f);
            }

            yield return 0;
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _dataBase);

        private void LoadData()
        {
            try
            {
                _dataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception)
            {
                _dataBase = new StoredData();
            }
        }

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');
            if (str.Length == 6)
                str += "FF";
            if (str.Length != 8)
            {
                throw new Exception(hex);
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        #endregion

        #region [API]

        [HookMethod("IsBlock")]
        public bool IsBlock(ulong userid)
        {
            var block = BasePlayer.FindByID(userid).GetComponent<RaidManagerRaidBlock>() != null;
            return block;
        }

        #endregion
    }
}