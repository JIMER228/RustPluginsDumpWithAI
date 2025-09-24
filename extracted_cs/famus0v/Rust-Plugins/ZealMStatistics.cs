using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealMStatistics", "Kira", "1.0.0")]
    public class ZealMStatistics : RustPlugin
    {
        #region [References] / [Ссылки]

        [PluginReference] private Plugin ZealKarma;

        #endregion

        #region [Vars] / [Переменные]

        private StoredData _dataBase = new StoredData();
        private ulong _lastDamagePlayer;

        #endregion

        #region [Hooks] / [Крюки]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            LoadData();
            Start_Process(CheckAllInDataBase());
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            SaveData();
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BaseHelicopter && info.Initiator is BasePlayer)
                _lastDamagePlayer = info.Initiator.ToPlayer().userID;
            if (entity is BradleyAPC && info.Initiator is BasePlayer)
                _lastDamagePlayer = info.Initiator.ToPlayer().userID;
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null) return;
            if (info == null) return;
            if (entity is BuildingBlock || entity is StabilityEntity)
            {
                if (!(info.Initiator is BasePlayer)) return;
                CheckInDataBase(info.InitiatorPlayer.userID);
                if (((BuildingBlock) entity).grade == BuildingGrade.Enum.Twigs) return;
                _dataBase.PlayersData[info.InitiatorPlayer.userID].ConstructionsDB.General++;
                TakeKarma(info.InitiatorPlayer.userID, 10);
                return;
            }

            switch (entity.prefabID)
            {
                case 4108440852:
                {
                    var killed = (BasePlayer) entity;
                    if (killed == null & IsNpc(killed)) return;
                    CheckInDataBase(killed.userID);
                    switch (info.Initiator?.prefabID)
                    {
                        case 4108440852:
                        {
                            var killer = (BasePlayer) info?.Initiator;
                            Add_DeathType(killer == null ? null : killer, killed, info);
                            if (killer != null & !IsNpc(killer)) Add_KillType(killer, killed, info);
                            return;
                        }
                        case 3312510084: // AutoTurret
                        case 1348746224: // GunTrap
                        case 1463807579: // LandTrap
                        case 4075317686: // FlameTurret
                        case 922529517: //  BearTrap
                        case 60725884: //   Tesla
                        {
                            Add_Kill_Trap(info?.Initiator, killed);
                            return;
                        }
                    }

                    return;
                }
                case 1799741974:
                {
                    if (!(info?.Initiator is BasePlayer)) return;
                    if (IsNpc(info.InitiatorPlayer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[info.InitiatorPlayer.userID].AnimalsDB.Bear++;
                    return;
                }
                case 502341109:
                {
                    if (!(info?.Initiator is BasePlayer)) return;
                    if (IsNpc(info.InitiatorPlayer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[info.InitiatorPlayer.userID].AnimalsDB.Boar++;
                    return;
                }
                case 152398164:
                {
                    if (!(info?.Initiator is BasePlayer)) return;
                    if (IsNpc(info.InitiatorPlayer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[info.InitiatorPlayer.userID].AnimalsDB.Chicken++;
                    return;
                }
                case 3880446623:
                {
                    if (!(info?.Initiator is BasePlayer)) return;
                    if (IsNpc(info.InitiatorPlayer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[info.InitiatorPlayer.userID].AnimalsDB.Horse++;
                    return;
                }
                case 1378621008:
                {
                    if (!(info?.Initiator is BasePlayer)) return;
                    if (IsNpc(info.InitiatorPlayer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[info.InitiatorPlayer.userID].AnimalsDB.Stag++;
                    return;
                }
                case 2144238755:
                {
                    if (!(info?.Initiator is BasePlayer)) return;
                    if (IsNpc(info.InitiatorPlayer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[info.InitiatorPlayer.userID].AnimalsDB.Wolf++;
                    return;
                }
                case 3029415845:
                {
                    var killer = BasePlayer.FindByID(_lastDamagePlayer);
                    if (killer == null || IsNpc(killer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[killer.userID].EntitiesDB.Helicopter++;
                    return;
                }
                case 1456850188:
                {
                    var killer = BasePlayer.FindByID(_lastDamagePlayer);
                    if (killer == null || IsNpc(killer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[killer.userID].EntitiesDB.Bradley++;
                    return;
                }
                case 966676416:
                case 555882409:
                case 3364121927:
                case 3269883781:
                case 3279100614:
                case 3438187947:
                {
                    var killer = info?.Initiator as BasePlayer;
                    if (killer == null || IsNpc(killer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[killer.userID].EntitiesDB.Barrels++;
                    return;
                }
                case 3421815773:
                {
                    var killer = info?.Initiator as BasePlayer;
                    if (killer == null || IsNpc(killer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[killer.userID].NpcDB.TunneldScientist++;
                    return;
                }
                case 2129041267:
                {
                    var killer = info?.Initiator as BasePlayer;
                    if (killer == null || IsNpc(killer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[killer.userID].NpcDB.HeavyScientist++;
                    return;
                }
                case 4223875851:
                {
                    var killer = info?.Initiator as BasePlayer;
                    if (killer == null || IsNpc(killer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[killer.userID].NpcDB.Scientist++;
                    return;
                }
                case 3312510084:
                {
                    if (!(info.Initiator is BasePlayer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[info.InitiatorPlayer.userID].ConstructionsDB.TrapsData.AutoTurret++;
                    return;
                }
                case 1348746224:
                {
                    if (!(info.Initiator is BasePlayer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[info.InitiatorPlayer.userID].ConstructionsDB.TrapsData.GunTrap++;
                    return;
                }
                case 1463807579:
                    if (!(info.Initiator is BasePlayer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[info.InitiatorPlayer.userID].ConstructionsDB.TrapsData.LandMine++;
                    return;
                case 4075317686:
                {
                    if (!(info.Initiator is BasePlayer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[info.InitiatorPlayer.userID].ConstructionsDB.TrapsData.FlameTurret++;
                    return;
                }
                case 922529517:
                {
                    if (!(info.Initiator is BasePlayer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[info.InitiatorPlayer.userID].ConstructionsDB.TrapsData.BearTrap++;
                    return;
                }
                case 60725884:
                {
                    if (!(info.Initiator is BasePlayer)) return;
                    CheckInDataBase(info.InitiatorPlayer.userID);
                    _dataBase.PlayersData[info.InitiatorPlayer.userID].ConstructionsDB.TrapsData.Tesla++;
                    return;
                }
            }

            SaveData();
        }

        // ReSharper disable once UnusedMember.Local
        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!(entity is BasePlayer)) return;
            var player = (BasePlayer) entity;
            CheckInDataBase(player.userID);
            var playerDB = _dataBase.PlayersData[player.userID];
            switch (item.info.shortname)
            {
                case "stones":
                    playerDB.ResourcesDB.Stones += item.amount;
                    break;
                case "wood":
                    playerDB.ResourcesDB.Wood += item.amount;
                    break;
                case "metal.ore":
                    playerDB.ResourcesDB.Metal += item.amount;
                    break;
                case "sulfur.ore":
                    playerDB.ResourcesDB.Sulfur += item.amount;
                    break;
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (dispenser.gatherType == ResourceDispenser.GatherType.Tree) TakeKarma(entity.ToPlayer().userID, 10);
            OnDispenserGather(dispenser, entity, item);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            CheckInDataBase(player.userID);
            var playerDB = _dataBase.PlayersData[player.userID];
            switch (item.info.shortname)
            {
                case "stones":
                    playerDB.ResourcesDB.Stones += item.amount;
                    break;
                case "wood":
                    playerDB.ResourcesDB.Wood += item.amount;
                    break;
                case "metal.ore":
                    playerDB.ResourcesDB.Metal += item.amount;
                    break;
                case "sulfur.ore":
                    playerDB.ResourcesDB.Sulfur += item.amount;
                    break;
                case "mushroom":
                    playerDB.CollectedDB.Mushrooms += item.amount;
                    break;
                case "cloth":
                    playerDB.CollectedDB.Cloth += item.amount;
                    break;
                case "corn":
                    playerDB.CollectedDB.Corn += item.amount;
                    break;
                case "potato":
                    playerDB.CollectedDB.Potato += item.amount;
                    break;
                case "pumpkin":
                    playerDB.CollectedDB.Pumpkin += item.amount;
                    break;
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            var ent = go.ToBaseEntity();
            if (plan.GetOwnerPlayer() == null) return;
            var player = plan.GetOwnerPlayer();
            CheckInDataBase(player.userID);
            var playerDB = _dataBase.PlayersData[player.userID];
            switch (ent.prefabID)
            {
                case 4038822397:
                case 2747504285:
                case 1267013032:
                case 3359110450:
                case 402225589:
                case 654911969:
                    playerDB.PlantedDB.Berry++;
                    GiveKarma(player.userID, 2.5f);
                    break;
                case 112964822:
                    playerDB.PlantedDB.Corn++;
                    GiveKarma(player.userID, 2.5f);
                    break;
                case 3587624038:
                    playerDB.PlantedDB.Hemp++;
                    GiveKarma(player.userID, 1f);
                    break;
                case 1524652375:
                    playerDB.PlantedDB.Pumpkin++;
                    GiveKarma(player.userID, 2.5f);
                    break;
                case 451737085:
                    playerDB.PlantedDB.Potato++;
                    GiveKarma(player.userID, 2.5f);
                    break;
            }
        }

        // ReSharper disable once UnusedMember.Local
        private object OnPlayerRevive(BasePlayer reviver, BasePlayer player)
        {
            GiveKarma(reviver.userID, 15f);
            return null;
        }

        #endregion

        #region [DataBase] / [База данных]

        public class StoredData
        {
            public Dictionary<ulong, PlayerData> PlayersData = new Dictionary<ulong, PlayerData>();

            public Dictionary<string, int> ItemData = new Dictionary<string, int>
            {
                ["ak47u.entity"] = 1545779598,
                ["bolt_rifle.entity"] = 1588298435,
                ["bone_club.entity"] = 1711033574,
                ["knife_bone.entity"] = 1814288539,
                ["butcherknife.entity"] = -194509282,
                ["candy_cane.entity"] = 1789825282,
                ["knife.combat.entity"] = 2040726127,
                ["compound_bow.entity"] = 884424049,
                ["crossbow.entity"] = 1965232394,
                ["smg.entity"] = 1796682209,
                ["double_shotgun.entity"] = -765183617,
                ["pistol_eoka.entity"] = -75944661,
                ["bow_hunting.entity"] = 1443579727,
                ["l96.entity"] = -778367295,
                ["longsword.entity"] = -1469578201,
                ["lr300.entity"] = -1812555177,
                ["m249.entity"] = -2069578888,
                ["m39.entity"] = 28201841,
                ["m92.entity"] = -852563019,
                ["mace.entity"] = -1966748496,
                ["machete.weapon"] = -1137865085,
                ["mp5.entity"] = 1318558775,
                ["mgl.entity"] = -1123473824,
                ["nailgun.entity"] = 1953903201,
                ["paddle.entity"] = 1491189398,
                ["pitchfork.entity"] = 1090916276,
                ["shotgun_pump.entity"] = 795371088,
                ["python.entity"] = 1373971859,
                ["pistol_revolver.entity"] = 649912614,
                ["salvaged_cleaver.entity"] = -1978999529,
                ["salvaged_sword.entity"] = 1326180354,
                ["pistol_semiauto.entity"] = 818877484,
                ["semi_auto_rifle.entity"] = -904863145,
                ["snowball.entity"] = -363689972,
                ["snowballgun.entity"] = 1103488722,
                ["spas12.entity"] = -41440462,
                ["spear_stone.entity"] = 1602646136,
                ["thompson.entity"] = -1758372725,
                ["shotgun_waterpipe.entity"] = -1367281941,
                ["spear_wooden.entity"] = 1540934679,
                ["cake.entity"] = 1973165031,
                ["chainsaw.entity"] = 1104520648,
                ["flashlight.entity"] = -196667575,
                ["hatchet.entity"] = -1252059217,
                ["jackhammer.entity"] = 1488979457,
                ["pickaxe.entity"] = -1302129395,
                ["rock.entity"] = 963906841,
                ["axe_salvaged.entity"] = -262590403,
                ["hammer_salvaged.entity"] = -1506397857,
                ["icepick_salvaged.entity"] = -1780802565,
                ["stonehatchet.entity"] = -1583967946,
                ["stone_pickaxe.entity"] = 171931394,
                ["torch.entity"] = 795236088,
                ["sickle.entity"] = -1368584029
            };

            public class KillData
            {
                public ulong Killed;
                public int Distance;
                public DateTime Time;
                public Gun GunData = new Gun();

                public class Gun
                {
                    public string Shortname;
                    public List<string> Modules;
                }
            }

            public class Type
            {
                public Dictionary<DamageType, int> Types = new Dictionary<DamageType, int>();
            }

            public class PlayerData
            {
                public General GeneralDB = new General();
                public Killed KilledDB = new Killed();
                public Deaths DeathsDB = new Deaths();
                public Animals AnimalsDB = new Animals();
                public Npc NpcDB = new Npc();
                public Resources ResourcesDB = new Resources();
                public Collected CollectedDB = new Collected();
                public Planted PlantedDB = new Planted();
                public Entities EntitiesDB = new Entities();
                public Constructions ConstructionsDB = new Constructions();
                public Crafted CraftedDB = new Crafted();

                public class General
                {
                    public int Kills;
                    public int Death;
                    public float PlayingTime;
                }

                public class Killed
                {
                    public int Sleeping;
                    public int Wounded;
                    public int Heat;
                    public int Explosion;
                    public int Arrows;

                    // ReSharper disable once CollectionNeverQueried.Global
                    public List<KillData> KilledData = new List<KillData>();
                    public Traps TrapsData = new Traps();

                    public class Traps
                    {
                        public int AutoTurret;
                        public int FlameTurret;
                        public int GunTrap;
                        public int BearTrap;
                        public int LandMine;
                        public int Tesla;
                    }
                }

                public class Deaths
                {
                    public int Sleeping;
                    public int Wounded;
                    public int Suicide;
                    public int Fall;
                    public int Heat;
                    public int Hunger;
                    public int Thirst;
                    public int Cold;
                    public int Bleeding;
                    public int Poison;
                    public int Explosion;
                    public int Radiation;
                    public int Drowned;
                    public int Arrows;
                    public int Helicopter;
                    public int Bradley;

                    // ReSharper disable once CollectionNeverQueried.Global
                    public List<KillData> DeathData = new List<KillData>();
                    public Traps TrapsData = new Traps();
                    public Type TypeData = new Type();

                    public class Traps
                    {
                        public int AutoTurret;
                        public int GunTrap;
                        public int FlameTurret;
                        public int BearTrap;
                        public int LandMine;
                        public int Tesla;
                    }
                }

                public class Animals
                {
                    public int Bear;
                    public int Boar;
                    public int Chicken;
                    public int Stag;
                    public int Horse;
                    public int Wolf;
                }

                public class Npc
                {
                    public int TunneldScientist;
                    public int HeavyScientist;
                    public int Scientist;
                }

                public class Resources
                {
                    public int Wood;
                    public int Stones;
                    public int Metal;
                    public int Sulfur;
                }

                public class Collected
                {
                    public int Mushrooms;
                    public int Corn;
                    public int Cloth;
                    public int Pumpkin;
                    public int Potato;
                }

                public class Planted
                {
                    public int Berry;
                    public int Hemp;
                    public int Pumpkin;
                    public int Corn;
                    public int Potato;
                }

                public class Entities
                {
                    public int Helicopter;
                    public int Bradley;
                    public int Barrels;
                }

                public class Constructions
                {
                    // ReSharper disable once MemberHidesStaticFromOuterClass
                    public int General;
                    public Traps TrapsData = new Traps();

                    public class Traps
                    {
                        public int AutoTurret;
                        public int GunTrap;
                        public int FlameTurret;
                        public int BearTrap;
                        public int LandMine;
                        public int Tesla;
                    }
                }

                public class Crafted
                {
                    public int Guns;
                    public int Teas;
                    public int Explosives;
                    public int Medication;
                    public int Bullets;
                    public int Tools;
                    public int Decor;
                    public int Misc;
                }
            }
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private IEnumerator CheckAllInDataBase()
        {
            foreach (var player in BasePlayer.activePlayerList) CheckInDataBase(player.userID);

            yield return 0;
        }

        private static void Start_Process(IEnumerator obj)
        {
            ServerMgr.Instance.StartCoroutine(obj);
        } 

        private static bool IsNpc(BasePlayer player)
        {
            if (player.IsNpc) return true;
            if (!player.userID.IsSteamId()) return true;
            if (player is NPCPlayer) return true;
            return false;
        }

        private void Add_KillType(BasePlayer killer, BasePlayer killed, HitInfo info)
        {
            CheckInDataBase(killer.userID);
            CheckInDataBase(killed.userID);
            var killerDB = _dataBase.PlayersData[killer.userID];
            killerDB.GeneralDB.Kills++;
            TakeKarma(killer.userID, 50);
            if (killed.IsSleeping()) killerDB.KilledDB.Sleeping++;
            if (killed.IsWounded()) killerDB.KilledDB.Wounded++;
            switch (info.damageTypes.GetMajorityDamageType())
            {
                case DamageType.Heat:
                    killerDB.KilledDB.Heat++;
                    break;
                case DamageType.Bullet:
                    killerDB.KilledDB.KilledData.Add(new StoredData.KillData
                    {
                        GunData = new StoredData.KillData.Gun
                        {
                            Shortname = info.Weapon.GetItem().info.shortname,
                            Modules = GetModules(info.Weapon.GetItem().contents)
                        },
                        Distance = Convert.ToInt32(info.ProjectileDistance),
                        Killed = killed.userID,
                        Time = DateTime.Now
                    });
                    break;
                case DamageType.Slash:
                case DamageType.Blunt:
                case DamageType.Stab:
                    killerDB.KilledDB.KilledData.Add(new StoredData.KillData
                    {
                        GunData = new StoredData.KillData.Gun
                        {
                            Shortname = info.WeaponPrefab.ShortPrefabName,
                            Modules = null
                        },
                        Distance = Convert.ToInt32(info.ProjectileDistance),
                        Killed = killed.userID,
                        Time = DateTime.Now
                    });
                    break;
                case DamageType.Explosion:
                    killerDB.KilledDB.Explosion++;
                    killerDB.KilledDB.KilledData.Add(new StoredData.KillData
                    {
                        GunData = new StoredData.KillData.Gun
                        {
                            Shortname = info.WeaponPrefab?.ShortPrefabName ?? "Explosion",
                            Modules = null
                        },
                        Distance = Convert.ToInt32(info.ProjectileDistance),
                        Killed = killed?.userID ?? 0,
                        Time = DateTime.Now
                    });
                    break;
                case DamageType.Arrow:
                    killerDB.KilledDB.Arrows++;
                    killerDB.KilledDB.KilledData.Add(new StoredData.KillData
                    {
                        GunData = new StoredData.KillData.Gun
                        {
                            Shortname = info.Weapon.GetItem().info.shortname,
                            Modules = GetModules(info.Weapon.GetItem().contents)
                        },
                        Distance = Convert.ToInt32(info.ProjectileDistance),
                        Killed = killed.userID,
                        Time = DateTime.Now
                    });
                    break;
                default:
                    PrintError("681 строка");
                    break;
            }
        }

        private void Add_DeathType(BasePlayer killer, BasePlayer killed, HitInfo info)
        {
            CheckInDataBase(killer.userID);
            CheckInDataBase(killed.userID);
            var killedDB = _dataBase.PlayersData[killed.userID];
            killedDB.GeneralDB.Death++;
            if (killed.IsSleeping()) killedDB.DeathsDB.Sleeping++;
            if (killed.IsWounded()) killedDB.DeathsDB.Wounded++;
            switch (info.damageTypes.GetMajorityDamageType())
            {
                case DamageType.Generic:
                    break;
                case DamageType.Hunger:
                    killedDB.DeathsDB.Hunger++;
                    break;
                case DamageType.Thirst:
                    killedDB.DeathsDB.Thirst++;
                    break;
                case DamageType.Cold:
                    killedDB.DeathsDB.Cold++;
                    break;
                case DamageType.Drowned:
                    killedDB.DeathsDB.Drowned++;
                    break;
                case DamageType.Heat:
                    killedDB.DeathsDB.Heat++;
                    break;
                case DamageType.Bleeding:
                    killedDB.DeathsDB.Bleeding++;
                    break;
                case DamageType.Poison:
                    killedDB.DeathsDB.Poison++;
                    break;
                case DamageType.Suicide:
                    killedDB.DeathsDB.Suicide++;
                    break;
                case DamageType.Bullet:
                    killedDB.DeathsDB.DeathData.Add(new StoredData.KillData
                    {
                        GunData = new StoredData.KillData.Gun
                        {
                            Shortname = info.Weapon.GetItem().info.shortname,
                            Modules = GetModules(info.Weapon.GetItem().contents)
                        },
                        Distance = Convert.ToInt32(info.ProjectileDistance),
                        Killed = killer.userID,
                        Time = DateTime.Now
                    });
                    break;
                case DamageType.Slash:
                case DamageType.Blunt:
                case DamageType.Stab:
                    killedDB.DeathsDB.DeathData.Add(new StoredData.KillData
                    {
                        GunData = new StoredData.KillData.Gun
                        {
                            Shortname = info.WeaponPrefab.ShortPrefabName,
                            Modules = null
                        },
                        Distance = Convert.ToInt32(info.ProjectileDistance),
                        Killed = killer.userID,
                        Time = DateTime.Now
                    });
                    break;
                case DamageType.Fall:
                    killedDB.DeathsDB.Fall++;
                    break;
                case DamageType.Radiation:
                    killedDB.DeathsDB.Radiation++;
                    break;
                case DamageType.Explosion:
                    killedDB.DeathsDB.Explosion++;
                    killedDB.DeathsDB.DeathData.Add(new StoredData.KillData
                    {
                        GunData = new StoredData.KillData.Gun
                        {
                            Shortname = info.WeaponPrefab?.ShortPrefabName ?? "Explosion",
                            Modules = null
                        },
                        Distance = Convert.ToInt32(info.ProjectileDistance),
                        Killed = killer?.userID ?? 0,
                        Time = DateTime.Now
                    });
                    break;
                case DamageType.Arrow:
                    killedDB.DeathsDB.Arrows++;
                    killedDB.DeathsDB.DeathData.Add(new StoredData.KillData
                    {
                        GunData = new StoredData.KillData.Gun
                        {
                            Shortname = info.Weapon.GetItem().info.shortname,
                            Modules = GetModules(info.Weapon.GetItem().contents)
                        },
                        Distance = Convert.ToInt32(info.ProjectileDistance),
                        Killed = killer.userID,
                        Time = DateTime.Now
                    });
                    break;
            }
        }

        private void Add_Kill_Trap(BaseEntity trap, BasePlayer killed)
        {
            
            PrintError(trap.prefabID.ToString());
            if (trap == null) return;
            if (killed == null) return;
            CheckInDataBase(trap.OwnerID);
            var killerDB = _dataBase.PlayersData[trap.OwnerID];
            if (killerDB == null) return;
            CheckInDataBase(killed.userID);
            var killedDB = _dataBase.PlayersData[killed.userID];
            if (killedDB == null) return;
            switch (trap.prefabID)
            {
                case 3312510084: //AutoTurret
                    killerDB.KilledDB.TrapsData.AutoTurret++;
                    killedDB.DeathsDB.TrapsData.AutoTurret++;
                    break;
                case 1348746224: //GunTrap 
                    killerDB.KilledDB.TrapsData.GunTrap++;
                    killedDB.DeathsDB.TrapsData.GunTrap++;
                    break;
                case 4075317686: //FlameTurret
                    killerDB.KilledDB.TrapsData.FlameTurret++;
                    killedDB.DeathsDB.TrapsData.FlameTurret++;
                    break;
                case 1463807579: //LandMine
                    killerDB.KilledDB.TrapsData.LandMine++;
                    killedDB.DeathsDB.TrapsData.LandMine++;
                    break;
                case 922529517: //BearTrap
                    killerDB.KilledDB.TrapsData.BearTrap++;
                    killedDB.DeathsDB.TrapsData.BearTrap++;
                    break;
                case 60725884: //Tesla
                    killerDB.KilledDB.TrapsData.Tesla++;
                    killedDB.DeathsDB.TrapsData.Tesla++;
                    break;
            }
        }

        private void CheckInDataBase(ulong player)
        {
            if (_dataBase.PlayersData.ContainsKey(player)) return;
            _dataBase.PlayersData.Add(player, new StoredData.PlayerData());
            PrintWarning($"{player} adding to DataBase");
        }

        private static List<string> GetModules(ItemContainer container)
        {
            return container?.itemList.Select(item => item.info.shortname).ToList();
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

        private void GiveKarma(ulong userID, float count)
        {
            ZealKarma?.Call("GiveKarma", userID, count);
            PrintToChat($"+{count}");
        }

        private void TakeKarma(ulong userID, float count)
        {
            ZealKarma?.Call("TakeKarma", userID, count);
            PrintToChat($"-{count}");
        }

        #endregion

        #region [API]

        [HookMethod("GetKills")]
        public int GetKills(ulong userID)
        {
            CheckInDataBase(userID);
            return _dataBase.PlayersData[userID].GeneralDB.Kills;
        }

        [HookMethod("GetDeaths")]
        public int GetDeaths(ulong userID)
        {
            CheckInDataBase(userID);
            return _dataBase.PlayersData[userID].GeneralDB.Death;
        }

        [HookMethod("GetPlayingTime")]
        public float GetPlayingTime(ulong userID)
        {
            CheckInDataBase(userID);
            return _dataBase.PlayersData[userID].GeneralDB.PlayingTime;
        }

        #endregion
    }
}