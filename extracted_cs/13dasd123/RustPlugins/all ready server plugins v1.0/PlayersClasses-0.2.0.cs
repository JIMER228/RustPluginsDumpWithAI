// Reference: System.Drawing
using Oxide.Core;
using Oxide.Core.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using System;

namespace Oxide.Plugins
{
    [Info("PlayersClasses", "OxideBro", "0.2.0", ResourceId = 0)]
      //  Слив плагинов server-rust by Apolo YouGame
    class PlayersClasses : RustPlugin
    {
        #region Data
        static PlayersClasses instance;
        public Dictionary<ulong, PlayersData> Players = new Dictionary<ulong, PlayersData>();
        public List<ulong> Attacker = new List<ulong>();
        public List<ulong> Raider = new List<ulong>();
        public List<ulong> Building = new List<ulong>();

        bool init = false;
        public class PlayersData
        {
            public string Name;
            public int Killed;
            public int Death;
            public int Building;
            public int KilledStructure;
            public string GetClasses;
            public bool Chose;
        }

        void LoadData()
        {
            try
            {
                Players = players_File.ReadObject<Dictionary<ulong, PlayersData>>();
            }
            catch
            {
                Players = new Dictionary<ulong, PlayersData>();
            }
        }

        DynamicConfigFile players_File = Interface.Oxide.DataFileSystem.GetFile("PlayersClasses/Players");

        void SaveData()
        {
            players_File.WriteObject(Players);
        }

        private List<string> defaultPrefabs2 = new List<string>();

        private List<string> defaultPrefabs1 = new List<string>();

        private List<string> Weapons1 = new List<string>();
        private List<string> Weapons2 = new List<string>();

        private List<string> BlacklistWeapons = new List<string>();

        private List<string> PrivilageAttacker = new List<string>()
        {
            "o.grant user {user} privilage",
        };

        private List<string> PrivilageBuilding = new List<string>()
        {
            "o.grant user {user} privilage",
        };

        private List<string> PrivilageRaider = new List<string>()
        {
            "o.grant user {user} privilage",
        };

        #endregion

        #region Configuration
        private void LoadDefaultConfig()
        {
            var _defaultPrefabs2 = new List<object>()
            {
                "cupboard.tool.deployed",
                "furnace",
                "barricade.stone",
                "barricade.concrete",
                "floor.grill",
                "wall.external.high.wood",
                "furnace.large",
                "reactivetarget_deployed",
                "barricade.woodwire",
                "landmine",
                "ceilinglight.deployed",
                "sleepingbag",
                "spikes.floor",
                "water_catcher_large",
                "gates.external.high.wood",
                "wall.frame.garagedoor",
                "shutter.metal.embrasure.a",
                "shutter.metal.embrasure.b",
                "door.hinged.metal",
                "door.double.hinged.metal",
                "wall.frame.cell.gate",
                "wall.frame.cell",
                "door.closer",
                "workbench2.deployed",
                "dropbox",
                 "jackolantern.angry",
                "sleepingbag_leather_deployed",
                "barricade.wood",
                "lantern.deployed",
                "beartrap",
                "barricade.sandbags",
                "woodbox_deployed",
                "door.double.hinged.wood",
                "door.hinged.wood",
                "wall.frame.shopfront",
                "wall.window.bars.wood",
                "lock.key",
                "door.closer",
                "workbench1.deployed",
                "wall.window.bars.metal",
                "wall.frame.netting",
                "wall.frame.fence.gate",
                "wall.frame.fence",
                "sign.large.wood"
            };
           
            GetConfig("Постройка", "Разрешенные предметы для первого уровня Строителя (Запрещенное для нулевого)", ref _defaultPrefabs2);
            defaultPrefabs2 = _defaultPrefabs2.Select(p => p.ToString()).ToList();
            var _defaultPrefabs1 = new List<object>()
            {
                  "jackolantern.angry",
            "sleepingbag_leather_deployed",
            "barricade.wood",
            "lantern.deployed",
            "beartrap",
            "barricade.sandbags",
            "woodbox_deployed",
            "door.double.hinged.wood",
            "door.hinged.wood",
            "wall.frame.shopfront",
            "wall.window.bars.wood",
            "lock.key",
            "door.closer",
            "workbench1.deployed",
            "wall.frame.fence.gate",
            "wall.frame.fence",
            "sign.large.wood"
            };
           
            GetConfig("Постройка", "Разрешенные предметы для нулевого уровня Строителя", ref _defaultPrefabs1);
            defaultPrefabs1 = _defaultPrefabs1.Select(p => p.ToString()).ToList();
            var _defaultWeapons1 = new List<object>()
            {
                 "shotgun.double",
            "grenade.f1",
            "shotgun.pump",
            "pistol.revolver",
            "grenade.beancan",
            "pistol.semiauto",
            "pistol.python",
            "rifle.semiauto",
            "smg.thompson",
            "smg.mp5",
            "smg.2"
            };

            GetConfig("Оружие", "Разрешенное оружие для первого уровня Выживших (Запрещенное для нулевого)", ref _defaultWeapons1);
            Weapons1 = _defaultWeapons1.Select(p => p.ToString()).ToList();
            var _defaultWeapons2 = new List<object>()
            {
                "pistol.m92",
                "rifle.lr300",
                "rifle.ak",
                "rifle.bolt",
                "explosive.timed",
                "rocket.launcher",
                "lmg.m249",
                "shotgun.spas12"
            };

            GetConfig("Оружие", "Запрещенное оружие для первого и нулевого уровня Выживших", ref _defaultWeapons2);
            Weapons2 = _defaultWeapons2.Select(p => p.ToString()).ToList();
            var _WriteListExp = new List<object>()
            {
                "ammo.rocket.basic",
                "ammo.rocket.fire",
                "ammo.rocket.hv",
                "flamethrower"
            };

            GetConfig("Оружие", "Список запрещенных взрывных веществ для Выживших и Островитян", ref _WriteListExp);
            BlacklistWeapons = _WriteListExp.Select(p => p.ToString()).ToList();
            var _PrivilageAttacker = new List<object>()
            {
                   "ratescontroller.x3",
                  "default"
            };

            GetConfig("Привилегии", "Список дополнительных привилегий/групп для Выживших (Используйте привилегию либо название группы без доп. слов)", ref _PrivilageAttacker);

            PrivilageAttacker = _PrivilageAttacker.Select(p => p.ToString()).ToList();
            var _PrivilageBuilding = new List<object>()
            {
                   "ratescontroller.x3",
                  "default"
            };

            GetConfig("Привилегии", "Список дополнительных привилегий/групп для Островитян (Используйте привилегию либо название группы без доп. слов)", ref _PrivilageBuilding);

            PrivilageBuilding = _PrivilageBuilding.Select(p => p.ToString()).ToList();

            var _PrivilageRaider = new List<object>()
            {
                  "ratescontroller.x3",
                  "default"
            };
            
            GetConfig("Привилегии", "Список дополнительных привилегий/групп для Одиночек (Используйте привилегию либо название группы без доп. слов)", ref _PrivilageRaider);
            PrivilageRaider = _PrivilageRaider.Select(p => p.ToString()).ToList();

            GetConfig("Основное", "Частота обновлений боковой панели GUI", ref UpdateTime);
            SaveConfig();
        }

        private bool GetConfig<T>(string MainMenu, string Key, ref T var)
        {
            if (Config[MainMenu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[MainMenu, Key], typeof(T));
                return false;
            }
            Config[MainMenu, Key] = var;
            return true;
        }

        private float UpdateTime = 5.0f;
        #endregion

        #region Oxide
       
        void OnNewSave()
        {
            LoadData();
            WipeData();
        }

        void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
      //  Слив плагинов server-rust by Apolo YouGame
        {
            if (victim == null) return;
            if (info == null) return;
            if (IsNPC(victim.ToPlayer()) || IsNPC(info.InitiatorPlayer)) return;
            var block = victim as BuildingBlock;
            if (info.Initiator == null) return;
            if (block && block.grade <= 0) return;
            if (!info.Initiator.ToPlayer()) return;
            BasePlayer v = victim.ToPlayer();
            BasePlayer k = info.InitiatorPlayer;


            if (InEvent(v) || InDuel(v)) return;

            BaseEntity e = victim.GetEntity();
            if (e == null) return;
            if (victim is BuildingBlock || victim is BaseEntity && !(victim is BasePlayer) && k is BasePlayer)
            {

                if (!Players.ContainsKey(k.userID))
                {
                    OnPlayerInit(k);
                }
                Players[k.userID].KilledStructure++;
            }
            if (v == null) return;
            if (k != null && k != v)
            {
                if (InEvent(k) || InDuel(k)) return;
                if (!Players.ContainsKey(k.userID))
                {
                    OnPlayerInit(k);
                }
                Players[k.userID].Killed++;
            }
            if (!Players.ContainsKey(v.userID))
            {
                OnPlayerInit(v);
            }
            if (!InDuel(v) || !IsNPC(v)) Players[v.userID].Death += 1;
        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
      //  Слив плагинов server-rust by Apolo YouGame
        {
            BasePlayer attacker = hitInfo?.Initiator as BasePlayer;
      //  Слив плагинов server-rust by Apolo YouGame
            if (hitInfo == null || attacker == null || entity == null) return null;
      //  Слив плагинов server-rust by Apolo YouGame
           
            if (IsNPC(attacker)) return null;
            PlayersData PlayersData;
            if (!Players.TryGetValue(attacker.userID, out PlayersData)) return null;
            if (entity is BuildingBlock && entity is BaseEntity)
            {
                if (entity == null) return null;
                var ClassAttacker = Players[attacker.userID].GetClasses;
                if (ClassAttacker == "Attacker")
                {
                    var dmgType = hitInfo.damageTypes.GetMajorityDamageType();
      //  Слив плагинов server-rust by Apolo YouGame
                    hitInfo.damageTypes.ScaleAll(0.5f);
      //  Слив плагинов server-rust by Apolo YouGame
                }
                if (ClassAttacker == "Building")
                {
                    var dmgType = hitInfo.damageTypes.GetMajorityDamageType();
      //  Слив плагинов server-rust by Apolo YouGame
                    hitInfo.damageTypes.ScaleAll(0.25f);
      //  Слив плагинов server-rust by Apolo YouGame
                }
                if (ClassAttacker == "Raider")
                {
                    var dmgType = hitInfo.damageTypes.GetMajorityDamageType();
      //  Слив плагинов server-rust by Apolo YouGame
                    hitInfo.damageTypes.ScaleAll(1.5f);
      //  Слив плагинов server-rust by Apolo YouGame
                }
            }
            BasePlayer victimPlayer = (entity as BasePlayer);
            if (victimPlayer == null) return null;

            if (entity is BasePlayer && hitInfo.Initiator is BasePlayer)
      //  Слив плагинов server-rust by Apolo YouGame
            {
                if (entity == null) return null;
                if (Players[attacker.userID].GetClasses == "Attacker")
                {
                    var dmgType = hitInfo.damageTypes.GetMajorityDamageType();
      //  Слив плагинов server-rust by Apolo YouGame
                    var attackerinfo = hitInfo.HitEntity.ToPlayer();
      //  Слив плагинов server-rust by Apolo YouGame
                    hitInfo.damageTypes.ScaleAll(1.5f);
      //  Слив плагинов server-rust by Apolo YouGame
                }

                if (Players[attacker.userID].GetClasses == "Building")
                {
                    var dmgType = hitInfo.damageTypes.GetMajorityDamageType();
      //  Слив плагинов server-rust by Apolo YouGame
                    hitInfo.damageTypes.ScaleAll(0.5f);
      //  Слив плагинов server-rust by Apolo YouGame
                    
                }

                if (Players[attacker.userID].GetClasses == "Raider")
                {
                    var dmgType = hitInfo.damageTypes.GetMajorityDamageType();
      //  Слив плагинов server-rust by Apolo YouGame
                    hitInfo.damageTypes.ScaleAll(0.75f);
      //  Слив плагинов server-rust by Apolo YouGame
                }
                if (IsNPC(victimPlayer)) return null;
                if (Players[victimPlayer.userID].GetClasses == "Attacker")
                {
                    var dmgType = hitInfo.damageTypes.GetMajorityDamageType();
      //  Слив плагинов server-rust by Apolo YouGame
                    hitInfo.damageTypes.ScaleAll(0.75f);
      //  Слив плагинов server-rust by Apolo YouGame
                }
                if (Players[victimPlayer.userID].GetClasses == "Building")
                {
                    var dmgType = hitInfo.damageTypes.GetMajorityDamageType();
      //  Слив плагинов server-rust by Apolo YouGame
                    hitInfo.damageTypes.ScaleAll(0.4f);
      //  Слив плагинов server-rust by Apolo YouGame
                }
                if (Players[victimPlayer.userID].GetClasses == "Raider")
                {
                    var dmgType = hitInfo.damageTypes.GetMajorityDamageType();
      //  Слив плагинов server-rust by Apolo YouGame
                    hitInfo.damageTypes.ScaleAll(0.6f);
      //  Слив плагинов server-rust by Apolo YouGame
                }
            }
            return null;
        }

        void OnDispenserBonus(ResourceDispenser disp, BasePlayer player, Item item)
        {
            double mod = 1f;
            if (Players[player.userID].GetClasses == "Attacker")
            {
                var rates = 0.50;
                var newAmount = (int)(item.amount * rates * mod);
                item.amount = newAmount > 1 ? newAmount : 1;
            }

            if (Players[player.userID].GetClasses == "Raider")
            {
                var rates = 0.75;
                var newAmount = (int)(item.amount * rates * mod);
                item.amount = newAmount > 1 ? newAmount : 1;
            }
            if (Players[player.userID].GetClasses == "Building")
            {
                var rates = 1.5f;
                var newAmount = (int)(item.amount * rates * mod);
                item.amount = newAmount > 1 ? newAmount : 1;
            }

        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!entity.ToPlayer()) return;
            double mod = 1f;
            if (Players[entity.ToPlayer().userID].GetClasses == "Attacker")
            {
                var rates = 0.50;
                var newAmount = (int)(item.amount * rates * mod);
                item.amount = newAmount > 1 ? newAmount : 1;
            }

            if (Players[entity.ToPlayer().userID].GetClasses == "Raider")
            {
                var rates = 0.75;
                var newAmount = (int)(item.amount * rates * mod);
                item.amount = newAmount > 1 ? newAmount : 1;
            }
            if (Players[entity.ToPlayer().userID].GetClasses == "Building")
            {
                var rates = 1.5f;
                var newAmount = (int)(item.amount * rates * mod);
                item.amount = newAmount > 1 ? newAmount : 1;
            }
        }
        private Timer mytimer;
        void OnServerInitialized()
        {
            instance = this;
            lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
            LoadData();
            LoadDefaultConfig();
            InitFileManager();
            CommunityEntity.ServerInstance.StartCoroutine(LoadImages());
            timer.Once(1f, () => instance.init = true);
            foreach (var player in BasePlayer.activePlayerList)
                mytimer = timer.Every(UpdateTime, () => CheckOnline(player));
            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerInit(player);
            }
          
        }

        void OnPlayerInit(BasePlayer player)
        {
            PlayersData PlayersData;
            if (!Players.TryGetValue(player.userID, out PlayersData))
            {
                PlayersData = new PlayersData();
                Players.Add(player.userID, new PlayersData()
                {
                    Chose = false,
                    GetClasses = "null",
                    KilledStructure = 0,
                    Killed = 0,
                    Death = 0,
                    Building = 0,
                    Name = player.displayName
                });
            }

            if (Players[player.userID].GetClasses == "null")
            {
                UIDraw(player);
            }
            if (Players[player.userID].GetClasses == "Attacker")
            {
                if (!Attacker.Contains(player.userID))
                {
                    Attacker.Add(player.userID);
                }
                DrawUI(player, Attacker.Count());
            }

            if (Players[player.userID].GetClasses == "Raider")
            {
                if (!Raider.Contains(player.userID))
                {
                    Raider.Add(player.userID);
                }
                DrawUI(player, Raider.Count());
            }

            if (Players[player.userID].GetClasses == "Building")
            {
                if (!Building.Contains(player.userID))
                {
                    Building.Add(player.userID);
                }
                DrawUI(player, Building.Count());
            }
        }

        void CheckOnline(BasePlayer player)
        {
            if (!Players.ContainsKey(player.userID))
            {
                OnPlayerInit(player);
                return;
            }

            if (Players[player.userID].GetClasses == "Attacker")
            {
                if (!Attacker.Contains(player.userID))
                {
                    Attacker.Add(player.userID);
                }
                DrawUI(player, Attacker.Count());
            }

            if (Players[player.userID].GetClasses == "Raider")
            {
                if (!Raider.Contains(player.userID))
                {
                    Raider.Add(player.userID);
                }
                DrawUI(player, Raider.Count());
            }

            if (Players[player.userID].GetClasses == "Building")
            {
                if (!Building.Contains(player.userID))
                {
                    Building.Add(player.userID);
                }
                DrawUI(player, Building.Count());
            }

        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (!Players.ContainsKey(player.userID)) return;

            if (Players[player.userID].GetClasses == "Attacker")
            {
                Attacker.Remove(player.userID);
            }

            if (Players[player.userID].GetClasses == "Raider")
            {
                Raider.Remove(player.userID);
            }

            if (Players[player.userID].GetClasses == "Building")
            {
                Building.Remove(player.userID);
            }
        }

        void OnServerSave()
        {
            SaveData();
        }

        void Unload()
        {
            Raider.Clear();
            Building.Clear();
            Attacker.Clear();
            SaveData();
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
            }
        }

        #endregion

        #region Core
        [PluginReference]
        Plugin EventManager;

        [PluginReference]
        Plugin Duel;

        bool InEvent(BasePlayer player)
        {
            try
            {
                bool result = (bool)EventManager?.Call("isPlaying", new object[] { player });
                return result;
            }
            catch
            {
                return false;
            }
        }

        bool InDuel(BasePlayer player) => Duel?.Call<bool>("IsPlayerOnActiveDuel", player) ?? false;

        public string GetPlayersClasses(ulong uid)
        {
            return Players[uid].GetClasses;
        }

        private string GetPlayerClasses(BasePlayer player, bool stat, bool rank = false, bool after = false, string Classes = "")
        {
            var classes1 = Players[player.userID].GetClasses;
            if (stat)
            {
                if (classes1 == "Attacker")
                    return "Выжившие";

                if (classes1 == "Building")
                    return "Островитяни";

                if (classes1 == "Raider")
                    return "Одиночки";
            }
            if (rank)
            {
                IOrderedEnumerable<KeyValuePair<ulong, PlayersData>> Top = from pair in Players where pair.Value.GetClasses == classes1 orderby (pair.Value.Death + pair.Value.KilledStructure + pair.Value.Building - pair.Value.Killed) descending select pair;
                int i = 1;

                foreach (KeyValuePair<ulong, PlayersData> pair in Top)
                {
                    if (pair.Key == player.userID)
                    {
                        return i.ToString();
                    }
                    i++;
                }
            }
            if (after)
            {
                IOrderedEnumerable<KeyValuePair<ulong, PlayersData>> Top = from pair in Players where pair.Value.GetClasses == Classes orderby (pair.Value.Death + pair.Value.KilledStructure + pair.Value.Building - pair.Value.Killed) descending select pair;
                int i = 0;

                foreach (KeyValuePair<ulong, PlayersData> pair in Top)
                {
                    i++;
                }
                return i.ToString();
            }

            return null;
        }

        void WipeData()
        {
            foreach (var player in Players)
            {
                if (Players[player.Key].GetClasses == "Attacker")
                {
                    foreach (var priv in PrivilageAttacker)
                    {
                        if (priv.ToLower().Contains("."))
                        {
                            rust.RunServerCommand($"o.revoke user {player.Key} {priv}");
                        }
                        else
                        {
                            rust.RunServerCommand($"o.usergroup remove {player.Key} {priv}");
                        }
                    }

                }
                if (Players[player.Key].GetClasses == "Building")
                {
                    foreach (var priv in PrivilageBuilding)
                    {
                        if (priv.ToLower().Contains("."))
                        {
                            rust.RunServerCommand($"o.revoke user {player.Key} {priv}");
                        }
                        else
                        {
                            rust.RunServerCommand($"o.usergroup remove {player.Key} {priv}");
                        }
                    }

                }
                if (Players[player.Key].GetClasses == "Raider")
                {
                    foreach (var priv in PrivilageRaider)
                    {
                        if (priv.ToLower().Contains("."))
                        {
                            rust.RunServerCommand($"o.revoke user {player.Key} {priv}");
                        }
                        else
                        {
                            rust.RunServerCommand($"o.usergroup remove {player.Key} {priv}");
                        }
                    }

                }

            }
            Players = new Dictionary<ulong, PlayersData>();
            PrintWarning("Прошел вайп, очистили data/PlayersClasses/Players а так же удалили все привилегии");
            Interface.Oxide.ReloadPlugin("PlayersClasses");
        }

        string GetPrivilage(BasePlayer player)
        {
            string chose = "";
            if (Players[player.userID].GetClasses == "Attacker")
            {
                chose = chose + "\t\tНаносимый урон по игрокам: +50%\n\t\tНаносимый урон по постройкам: -50%\n\t\tЗащита: +25%\n\t\tСбор ресурсов: -50%\n\t\tМаксимальная команда: 2 игрока";
            }
            if (Players[player.userID].GetClasses == "Building")
            {
                chose = chose + "\t\tНаносимый урон по игрокам: -75%\n\t\tНаносимый урон по постройкам: -75%\n\t\tЗащита: +75%\n\t\tСбор ресурсов: +50%\n\t\tМаксимальная команда: 3 игрока";
            }
            if (Players[player.userID].GetClasses == "Raider")
            {
                chose = chose + "\t\tНаносимый урон по игрокам: -50%\n\t\tНаносимый урон по постройкам: +50%\n\t\tЗащита: +50%\n\t\tСбор ресурсов: -25%\n\t\tМаксимальная команда: 2 игрока";
            }
            return chose;
        }

        void ReplaceItem(BasePlayer player, string shortname)
        {
            if (player == null) return;
            player.inventory.GiveItem(ItemManager.CreateByName(shortname, 1, 0));
        }

        private bool IsNPC(BasePlayer player)
        {
            if (player == null) return false;
            if (player is NPCPlayer) return true;
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L))
                return true;

            return false;
        }

        public BasePlayer FindBasePlayer(string nameOrUserId)
        {
            nameOrUserId = nameOrUserId.ToLower();
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.displayName.ToLower().Contains(nameOrUserId) || player.UserIDString == nameOrUserId)
                    return player;
            }
            foreach (var player in BasePlayer.sleepingPlayerList)
            {
                if (player.displayName.ToLower().Contains(nameOrUserId) || player.UserIDString == nameOrUserId)
                    return player;
            }
            return default(BasePlayer);
        }
        #endregion

        #region Commands

        [ConsoleCommand("classes_chose")]
        void CmdChoseClass(ConsoleSystem.Arg args)
        {
            var player = args.Connection.player as BasePlayer;
            if (player == null) return;
            if (args.Args == null) return;
            var chose = args.Args[0];

            Players[player.userID].Chose = true;
            Players[player.userID].GetClasses = chose;

            SendReply(player, Messages["Chose"], GetPlayerClasses(player, true), GetPrivilage(player));

            if (Players[player.userID].GetClasses == "Attacker")
            {
                foreach (var priv in PrivilageAttacker)
                {
                    if (priv.ToLower().Contains("."))
                    {
                        rust.RunServerCommand($"o.grant user {player.userID} {priv}");
                    }
                    else
                    {
                        rust.RunServerCommand($"o.usergroup add {player.userID} {priv}");
                    }
                }

            }
            if (Players[player.userID].GetClasses == "Building")
            {
                foreach (var priv in PrivilageBuilding)
                {
                    if (priv.ToLower().Contains("."))
                    {
                        rust.RunServerCommand($"o.grant user {player.userID} {priv}");
                    }
                    else
                    {
                        rust.RunServerCommand($"o.usergroup add {player.userID} {priv}");
                    }
                }

            }
            if (Players[player.userID].GetClasses == "Raider")
            {
                foreach (var priv in PrivilageRaider)
                {
                    if (priv.ToLower().Contains("."))
                    {
                        rust.RunServerCommand($"o.grant user {player.userID} {priv}");
                    }
                    else
                    {
                        rust.RunServerCommand($"o.usergroup add {player} {priv}");
                    }
                }

            }
            DestroyUI(player);
        }


        [ConsoleCommand("classes")]
        void CmdChoseClasses(ConsoleSystem.Arg args)
        {
            var player = args.Connection.player as BasePlayer;
            if (player == null) return;
            var clases = GetPlayerClasses(player, true);

            if (player.IsAdmin || !Players[player.userID].Chose)
            {
                UIDraw(player);
            }
            else
            {
                SendReply(player, Messages["NoChose"], clases);
            }
        }

        [ConsoleCommand("classes_nochose")]
        void CmdNoChoseClass(ConsoleSystem.Arg args)
        {
            var player = args.Connection.player as BasePlayer;
            if (player == null) return;
            player.Kick("Вы отказались от выбора");
        }

        [ChatCommand("mystats")]
        void ChatChatStats(BasePlayer player)
        {
            if (player == null) return;
            DrawStats(player);
        }

        [ChatCommand("globaltop")]
        void ChatChatTop(BasePlayer player)
        {
            if (player == null) return;
            DrawTOP(player);
        }

        [ConsoleCommand("drawstats")]
        private void DrawTop(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            DrawStats(player);
        }

        [ConsoleCommand("drawtop")]
        private void DrawStats(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            DrawTOP(player);
        }

        #endregion

        #region UI

        string UI = "[{\"name\":\"PlayersClasses_2\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0 0 0 0.788231\"},{\"type\":\"NeedsCursor\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayersClasses_3\",\"parent\":\"PlayersClasses_2\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.1294118 0.1294118 0.1529412 0.4431373\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.8320313\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayersClasses_4\",\"parent\":\"PlayersClasses_3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{welcome}\",\"fontSize\":18,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayersClasses_5\",\"parent\":\"PlayersClasses_2\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.26\",\"anchormax\":\"1 0.7500001\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayersClasses_6\",\"parent\":\"PlayersClasses_5\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.427451 0.1843137 0.1254902 0.4474251\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.09516837 0\",\"anchormax\":\"0.3016105 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"button\",\"parent\":\"PlayersClasses_6\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"classes_chose Attacker\",\"color\":\"0.427451 0.1843137 0.1254902 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01 0.01\",\"anchormax\":\"0.99 0.1\",\"offsetmin\":\"0.05 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"button2\",\"parent\":\"button\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Выбрать\",\"fontSize\":18,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3037587\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0.05 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"text\",\"parent\":\"PlayersClasses_6\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"ВЫЖИВШИЕ\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.5247875\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.9202804\",\"anchormax\":\"1 0.9946854\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"images\",\"parent\":\"PlayersClasses_6\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"png\":\"{Attacker}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.15 0.4021048\",\"anchormax\":\"0.85 0.9123087\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"textinfo\",\"parent\":\"PlayersClasses_6\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Наносимый урон по игрокам: +50%\nНаносимый урон по постройкам: -50%\nЗащита: +25%\nСбор ресурсов: -50%\nМаксимальная команда: 2 игрока\",\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3921701\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01063825 0.1124575\",\"anchormax\":\"0.9858159 0.3967899\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayersClasses_7\",\"parent\":\"PlayersClasses_5\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3882353 0.3803922 0.2901961 0.4363736\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.3967789 0\",\"anchormax\":\"0.6032211 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"text\",\"parent\":\"PlayersClasses_7\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"ОСТРОВИТЯНИ\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.5247875\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.9202804\",\"anchormax\":\"1 0.9946854\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"textinfo\",\"parent\":\"PlayersClasses_7\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Наносимый урон по игрокам: -75%\nНаносимый урон по постройкам: -75%\nЗащита: +75%\nСбор ресурсов: +50%\nМаксимальная команда: 3 игрока\",\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3921701\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01063825 0.1124575\",\"anchormax\":\"0.9858159 0.3967899\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"images\",\"parent\":\"PlayersClasses_7\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"png\":\"{Building}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.15 0.4021048\",\"anchormax\":\"0.85 0.9123087\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"button\",\"parent\":\"PlayersClasses_7\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"classes_chose Building\",\"color\":\"0.3882353 0.3803922 0.2901961 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01 0.01\",\"anchormax\":\"0.99 0.1\",\"offsetmin\":\"0.05 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"button3\",\"parent\":\"button\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Выбрать\",\"fontSize\":18,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3037587\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0.05 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayersClasses_8\",\"parent\":\"PlayersClasses_5\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.1294118 0.1333333 0.1529412 0.4470588\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6983895 0\",\"anchormax\":\"0.9048316 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"button\",\"parent\":\"PlayersClasses_8\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"classes_chose Raider\",\"color\":\"0.1333333 0.1372549 0.1529412 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01 0.01\",\"anchormax\":\"0.99 0.1\",\"offsetmin\":\"0.05 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"button1\",\"parent\":\"button\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Выбрать\",\"fontSize\":18,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3037587\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0.05 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"text\",\"parent\":\"PlayersClasses_8\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"ОДИНОЧКИ\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.5247875\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.9202804\",\"anchormax\":\"1 0.9946854\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"images\",\"parent\":\"PlayersClasses_8\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"png\":\"{Raider}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.15 0.4021048\",\"anchormax\":\"0.85 0.9123087\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"textinfo\",\"parent\":\"PlayersClasses_8\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Наносимый урон по игрокам: -50%\nНаносимый урон по постройкам: +50%\nЗащита: +50%\nСбор ресурсов: -25%\nМаксимальная команда: 2 игрока\",\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3921701\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01063825 0.1124575\",\"anchormax\":\"0.9858159 0.3967899\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"button_Exit\",\"parent\":\"PlayersClasses_2\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"classes_nochose\",\"color\":\"0.2705882 0 0 0.7857328\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.39 0.1523438\",\"anchormax\":\"0.61 0.2122396\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"text\",\"parent\":\"button_Exit\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Отказаться и выйти\",\"fontSize\":18,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]";

        string Online = "[{\"name\":\"Checkclass2\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"{color}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.8733529 0.65625\",\"anchormax\":\"0.9904832 0.9895833\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Checkclass3\",\"parent\":\"Checkclass2\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"png\":\"{images}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.1875001 0.5820312\",\"anchormax\":\"0.8562499 0.9908253\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Checkclass4\",\"parent\":\"Checkclass2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Игроки в сети: <color=RED>{0}</color>\",\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3921702\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01875001 0.4754875\",\"anchormax\":\"0.9875 0.5947536\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Checkclass4\",\"parent\":\"Checkclass2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"ТОП 5 В вашем классе\n\n<size=12>{text}</size>\",\"align\":\"UpperCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.391784\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01875001 0\",\"anchormax\":\"0.9875 0.464844\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"RectTransform\",\"anchormin\":\"0.07320644 0.1302083\",\"anchormax\":\"0.1464129 0.2604167\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"RectTransform\",\"anchormin\":\"0.07320644 0.1302083\",\"anchormax\":\"0.1464129 0.2604167\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]";

        string Stats = "[{\"name\":\"PlayerClasses_UIBackground\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.5176471 0.5294118 0.454902 0.8\"},{\"type\":\"NeedsCursor\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2 0.27\",\"anchormax\":\"0.8 0.73\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"title\",\"parent\":\"PlayerClasses_UIBackground\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"→ ДЕТАЛИ & СТАТИСТИКА\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3671983\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.002196185 0.8860564\",\"anchormax\":\"0.4048317 0.9898163\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"logotupe\",\"parent\":\"PlayerClasses_UIBackground\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"1 1 1 0.8606033\",\"png\":\"{logo}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.8660322 0.7179574\",\"anchormax\":\"0.9880429 0.9862469\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"titlegamer\",\"parent\":\"PlayerClasses_UIBackground\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"► ЛИЧНАЯ СТАТИСТИКА ИГРОКА {name}\",\"fontSize\":18,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3310119\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01439732 0.7377717\",\"anchormax\":\"0.6671548 0.8340127\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"Bpgamer\",\"parent\":\"PlayerClasses_UIBackground\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0009760509 0.1858016\",\"anchormax\":\"0.9978039 0.7229025\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bpgamer2\",\"parent\":\"Bpgamer\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3921569 0.3843137 0.2941177 0.823176\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.00979192 0.04705892\",\"anchormax\":\"0.3300693 0.9529411\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"bpgamer2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"● Ваш класс:\",\"fontSize\":17,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3803922\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0521515 0.7666699\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"bpgamer2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{classes}\",\"fontSize\":18,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3803922\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.5066687\",\"anchormax\":\"1 0.8400035\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"bpgamer2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"● Место в рейтинге:\",\"fontSize\":17,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3803922\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0521515 0.3133362\",\"anchormax\":\"1 0.5466671\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"bpgamer2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{rank}\",\"fontSize\":18,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3803922\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.006667741\",\"anchormax\":\"1 0.3400019\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bpgamer3\",\"parent\":\"Bpgamer\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.1294118 0.1294118 0.1568628 0.7900151\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.3398613 0.04705892\",\"anchormax\":\"0.6601387 0.9529411\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"bpgamer3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"● КДР & ДЕТАЛИ:\",\"fontSize\":17,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3803922\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0521515 0.7666699\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"bpgamer3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Убийств: {kill}\nСмертей: {death}\nПостроено строений: {building}\nУничтоженных строений: {structure}\nКДР: {kdr}\",\"fontSize\":16,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3803922\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.07006377 0.006667674\",\"anchormax\":\"1 0.8400035\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bpgamer3\",\"parent\":\"Bpgamer\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.4313726 0.1803922 0.1294118 0.8\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6699306 0.04705892\",\"anchormax\":\"0.990208 0.9529411\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"bpgamer3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"● TOP 5 в вашем классе\",\"fontSize\":17,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3803922\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0521515 0.7666699\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"bpgamer3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{top5}\",\"fontSize\":15,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3803922\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.06369454 0.006667674\",\"anchormax\":\"1 0.8400035\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"PlayerClasses_UIBackground\",\"components\":[{\"type\":\"RectTransform\",\"anchormin\":\"0.07320644 0.1302083\",\"anchormax\":\"0.1464129 0.2604167\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"exit\",\"parent\":\"PlayerClasses_UIBackground\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"close\":\"PlayerClasses_UIBackground\",\"color\":\"0.7647059 0 0 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.759883 0.04697051\",\"anchormax\":\"0.9917032 0.1546648\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"exit\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"✖ ВЫХОД\",\"fontSize\":16,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3803922\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"exit\",\"parent\":\"PlayerClasses_UIBackground\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"drawtop\",\"close\":\"PlayerClasses_UIBackground\",\"color\":\"0.7647059 0.3764706 0 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.008296941 0.04697053\",\"anchormax\":\"0.2401173 0.1546648\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"exit\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"➲ Общая статистика\",\"fontSize\":16,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3803922\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"PlayerClasses_UIBackground\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>Версия: {version}\n★ специально для ThunderRUST ★</b>\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4695303\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2425573 0.001811594\",\"anchormax\":\"0.7574427 0.2027853\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]";

        string Tops = "[{\"name\":\"PlayerClasses_UIBackgroundTOP3\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.5176471 0.5294118 0.454902 0.8\"},{\"type\":\"NeedsCursor\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2 0.1\",\"anchormax\":\"0.8 0.9\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP4\",\"parent\":\"PlayerClasses_UIBackgroundTOP3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>→ ДЕТАЛИ & СТАТИСТИКА</b>\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3671983\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.002196185 0.9182943\",\"anchormax\":\"0.4414349 0.9898163\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP5\",\"parent\":\"PlayerClasses_UIBackgroundTOP3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"►<b> ОБЩАЯ СТАТИСТИКА</b>\",\"fontSize\":18},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3310119\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01439732 0.841797\",\"anchormax\":\"0.6671548 0.8922526\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP6\",\"parent\":\"PlayerClasses_UIBackgroundTOP3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"►<b> СТАТИСТИКА ПО КЛАССАМ</b>\",\"fontSize\":18},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.3310119\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01439732 0.4039679\",\"anchormax\":\"0.6671548 0.4544245\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP7\",\"parent\":\"PlayerClasses_UIBackgroundTOP3\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0009760509 0.5504557\",\"anchormax\":\"0.9978039 0.8483074\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP8\",\"parent\":\"PlayerClasses_UIBackgroundTOP7\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.2941177 0.3380522 0.3921569 0.8196079\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.00979192 0.04705892\",\"anchormax\":\"0.3300693 0.9529411\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP9\",\"parent\":\"PlayerClasses_UIBackgroundTOP8\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"● Игроков в классах:\",\"fontSize\":17,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.410616\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0521515 0.7666699\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP10\",\"parent\":\"PlayerClasses_UIBackgroundTOP8\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Выжившие: {attackers}\nОстровитяни: {buildings}\nОдиночки: {raiders}\",\"fontSize\":18,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.436376\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.00531286\",\"anchormax\":\"1 0.8400035\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP11\",\"parent\":\"PlayerClasses_UIBackgroundTOP7\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.1568628 0.1465688 0.1294118 0.7882353\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.3398613 0.04705892\",\"anchormax\":\"0.6601387 0.9529411\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP12\",\"parent\":\"PlayerClasses_UIBackgroundTOP11\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"● ДЕТАЛИ:\",\"fontSize\":17,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4310146\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0521515 0.7666699\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP13\",\"parent\":\"PlayerClasses_UIBackgroundTOP11\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Всего убийств: {kill}\nВсего смертей: {death}\nПостроенных строений: {buildings}\nВсего уничтоженных строений: {structure}\",\"fontSize\":16,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4406369\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.05095547 0.006667674\",\"anchormax\":\"1 0.8400035\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP14\",\"parent\":\"PlayerClasses_UIBackgroundTOP7\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.4313726 0.1294118 0.3633865 0.8\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6699306 0.04705892\",\"anchormax\":\"0.990208 0.9529411\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP15\",\"parent\":\"PlayerClasses_UIBackgroundTOP14\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"● TOP 5 игроков сервера\",\"fontSize\":17,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4310146\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0521515 0.7666699\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP16\",\"parent\":\"PlayerClasses_UIBackgroundTOP14\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{top5server}\",\"fontSize\":15,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4516883\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.06369454 0.006667674\",\"anchormax\":\"1 0.8400035\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP31\",\"parent\":\"PlayerClasses_UIBackgroundTOP3\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"1 0.1036241 0.1036241 0.7237125\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.07 0.4999994\",\"anchormax\":\"0.93 0.505\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP2\",\"parent\":\"PlayerClasses_UIBackgroundTOP3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>Версия: ☄ {version}\n★ специально для ThunderRUST ★</b>\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.5247875\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2742801 0.006835941\",\"anchormax\":\"0.7586628 0.1223958\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP21\",\"parent\":\"PlayerClasses_UIBackgroundTOP3\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0009760509 0.1126307\",\"anchormax\":\"0.9978039 0.4104865\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP22\",\"parent\":\"PlayerClasses_UIBackgroundTOP21\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.4313726 0.1803922 0.1294118 0.8342232\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.00979192 0.04705892\",\"anchormax\":\"0.3300693 0.9529411\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP23\",\"parent\":\"PlayerClasses_UIBackgroundTOP22\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"● ТОП 5 Выживших:\",\"fontSize\":17,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.410616\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0521515 0.7666699\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP24\",\"parent\":\"PlayerClasses_UIBackgroundTOP22\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{top5attackers}\",\"fontSize\":18,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.436376\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.06879002 0.00531286\",\"anchormax\":\"1 0.8400035\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP25\",\"parent\":\"PlayerClasses_UIBackgroundTOP21\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.3921569 0.3843137 0.2941177 0.845279\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.3398613 0.04705892\",\"anchormax\":\"0.6601387 0.9529411\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP26\",\"parent\":\"PlayerClasses_UIBackgroundTOP25\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"● ТОП 5 Островитян:\",\"fontSize\":17,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4310146\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0521515 0.7666699\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP27\",\"parent\":\"PlayerClasses_UIBackgroundTOP25\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{top5buildings}\",\"fontSize\":16,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4406369\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.07006377 0.006667674\",\"anchormax\":\"1 0.8400035\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP28\",\"parent\":\"PlayerClasses_UIBackgroundTOP21\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.1294118 0.1294118 0.1568628 0.845279\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6699306 0.04705892\",\"anchormax\":\"0.990208 0.9529411\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP29\",\"parent\":\"PlayerClasses_UIBackgroundTOP28\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"● TOP 5 Одиночек:\",\"fontSize\":17,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4310146\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.0521515 0.7666699\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP30\",\"parent\":\"PlayerClasses_UIBackgroundTOP28\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{top5raiders}\",\"fontSize\":15,\"align\":\"MiddleLeft\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4516883\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.06369454 0.006667674\",\"anchormax\":\"1 0.8400035\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP17\",\"parent\":\"PlayerClasses_UIBackgroundTOP3\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"close\":\"PlayerClasses_UIBackgroundTOP3\",\"material\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.7647059 0 0 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.7598829 0.02807617\",\"anchormax\":\"0.9917032 0.09147136\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP18\",\"parent\":\"PlayerClasses_UIBackgroundTOP17\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"✖ ВЫХОД\",\"fontSize\":16,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4253246\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP19\",\"parent\":\"PlayerClasses_UIBackgroundTOP3\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"drawstats\",\"close\":\"PlayerClasses_UIBackgroundTOP3\",\"color\":\"0.7647059 0.3764706 0 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.008296941 0.02807616\",\"anchormax\":\"0.27306 0.09147137\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"PlayerClasses_UIBackgroundTOP20\",\"parent\":\"PlayerClasses_UIBackgroundTOP19\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"➲ ЛИЧНАЯ СТАТИСТИКА\",\"fontSize\":16,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4235294\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]";

        void DrawTOP(BasePlayer player)
        {
            if (player == null) return;

            string topAttacker = "";
            string topBuilding = "";
            string topRaider = "";
            string topAll = "";

            int Killed = 0;
            int Buildings = 0;
            int Deaths = 0;
            int Build = 0;
            //top All
            IOrderedEnumerable<KeyValuePair<ulong, PlayersData>> Top = from pair in Players orderby (pair.Value.Death + pair.Value.KilledStructure + pair.Value.Building - pair.Value.Killed) descending select pair;
            int i = 1;

            foreach (KeyValuePair<ulong, PlayersData> pair in Top)
            {
                string name = pair.Value.Name;
                if (name == null) name = "Неизвестный";
                int value = pair.Value.Killed+ pair.Value.KilledStructure + (pair.Value.Building / 3) - pair.Value.Death;
                Killed = Killed + pair.Value.Killed;
                Deaths = Deaths + pair.Value.Death;
                Buildings = Buildings + pair.Value.KilledStructure;
                Build = Build + pair.Value.Building;
                topAll = topAll + i.ToString() + ". " + name + " - " + value + "\n";
                i++;
                if (i > 5) break;
            }
            //end top All


            //Top Attacker

            IOrderedEnumerable<KeyValuePair<ulong, PlayersData>> TopAttacker = from pair in Players where pair.Value.GetClasses == "Attacker" orderby (pair.Value.Death + pair.Value.KilledStructure + pair.Value.Building - pair.Value.Killed) descending select pair;
            int i1 = 1;

            foreach (KeyValuePair<ulong, PlayersData> pair in TopAttacker)
            {
                string name = pair.Value.Name;
                if (name == null) name = "Неизвестный";
                int value = pair.Value.Killed+ pair.Value.KilledStructure + (pair.Value.Building / 3) - pair.Value.Death;
                topAttacker = topAttacker + i1.ToString() + ". " + name + " - " + value + "\n";
                i1++;
                if (i1 > 5) break;
            }

            //endtopattacker

            //top Building
            IOrderedEnumerable<KeyValuePair<ulong, PlayersData>> TopBuilding = from pair in Players where pair.Value.GetClasses == "Building" orderby (pair.Value.Death + pair.Value.KilledStructure + pair.Value.Building - pair.Value.Killed) descending select pair;
            int i2 = 1;

            foreach (KeyValuePair<ulong, PlayersData> pair in TopBuilding)
            {
                string name = pair.Value.Name;
                if (name == null) name = "Неизвестный";
                int value = pair.Value.Death + pair.Value.KilledStructure + pair.Value.Building - pair.Value.Killed;
                topBuilding = topBuilding + i2.ToString() + ". " + name + " - " + value + "\n";
                i2++;
                if (i2 > 5) break;
            }

            //endtopbuilding

            //Top Raider
            IOrderedEnumerable<KeyValuePair<ulong, PlayersData>> TopRaider = from pair in Players where pair.Value.GetClasses == "Raider" orderby (pair.Value.Death + pair.Value.KilledStructure + pair.Value.Building - pair.Value.Killed) descending select pair;
            int i3 = 1;

            foreach (KeyValuePair<ulong, PlayersData> pair in TopRaider)
            {
                string name = pair.Value.Name;
                if (name == null) name = "Неизвестный";
                int value = pair.Value.Killed+ pair.Value.KilledStructure + (pair.Value.Building / 3) - pair.Value.Death;
                topRaider = topRaider + i3.ToString() + ". " + name + " - " + value + "\n";
                i3++;
                if (i3 > 5) break;
            }
            //endtopraider


            CuiHelper.DestroyUi(player, "PlayerClasses_UIBackgroundTOP3");

            var pl = Players[player.userID];
            var rank = GetPlayerClasses(player, true);

            CuiHelper.AddUi(player, Tops
                .Replace("{attackers}", GetPlayerClasses(player, false, false, true, "Attacker")).Replace("{buildings}", GetPlayerClasses(player, false, false, true, "Building")).Replace("{raiders}", GetPlayerClasses(player, false, false, true, "Raider")).Replace("{name}", player.displayName)
                .Replace("{death}", Deaths.ToString())
                .Replace("{kill}", Killed.ToString())
                .Replace("{structure}", Buildings.ToString())
                .Replace("{building}", Build.ToString())
                .Replace("{rank}", $"Вы на: {rank} месте")
                .Replace("{top5server}", topAll)
                .Replace("{top5attackers}", topAttacker)
                .Replace("{top5buildings}", topBuilding)
                .Replace("{top5raiders}", topRaider)
                .Replace("{version}", Version.ToString()));
        }
        
        void DrawStats(BasePlayer player)
        {
            if (player == null) return;

            string topPvp = "";
            var Classes = Players[player.userID].GetClasses;
            IOrderedEnumerable<KeyValuePair<ulong, PlayersData>> Top = from pair in Players where pair.Value.GetClasses == Classes orderby (pair.Value.Death + pair.Value.KilledStructure + pair.Value.Building - pair.Value.Killed) descending select pair;
            int i = 1;

            foreach (KeyValuePair<ulong, PlayersData> pair in Top)
            {
                string name = pair.Value.Name;
                if (name == null) name = "Неизвестный";
                int value = pair.Value.Killed+ pair.Value.KilledStructure + (pair.Value.Building / 3) - pair.Value.Death;
                topPvp = topPvp + i.ToString() + ". " + name + " - " + value + "\n";
                i++;
                if (i > 5) break;
            }

            CuiHelper.DestroyUi(player, "PlayerClasses_UIBackground");

            var classes = GetPlayerClasses(player, true);
            var pl = Players[player.userID];
            double kdr = Math.Round(pl.Killed * 1f / pl.Death * 1f, 2);
            var rank = GetPlayerClasses(player, false, true);
            CuiHelper.AddUi(player, Stats
                .Replace("{logo}", Images[pl.GetClasses]).Replace("{name}", player.displayName).Replace("{classes}", classes)
                .Replace("{kill}", pl.Killed.ToString())
                .Replace("{death}", pl.Death.ToString())
                .Replace("{building}", pl.Building.ToString())
                .Replace("{structure}", pl.KilledStructure.ToString())
                .Replace("{kdr}", kdr.ToString("0.00"))
                .Replace("{rank}", $"Вы на: {rank} месте")
                .Replace("{version}", Version.ToString())
                .Replace("{top5}", topPvp));
        }


        void DrawUI(BasePlayer player, int online)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(1, () => DrawUI(player, online));
                return;
            }
            if (!init)
            {
                timer.In(1f, () => DrawUI(player, online));
                return;
            }
            string topPvp = "";
            var Classes = Players[player.userID].GetClasses;
            IOrderedEnumerable<KeyValuePair<ulong, PlayersData>> Top = from pair in Players where pair.Value.GetClasses == Classes orderby (pair.Value.Death + pair.Value.KilledStructure + pair.Value.Building - pair.Value.Killed) descending select pair;
            int i = 1;

            foreach (KeyValuePair<ulong, PlayersData> pair in Top)
            {
                string name = pair.Value.Name;
                if (name == null) name = "Неизвестный";
                int value = pair.Value.Killed+ pair.Value.KilledStructure + (pair.Value.Building / 3) - pair.Value.Death;
                topPvp = topPvp + i.ToString() + ". " + name + " - " + value + "\n";
                i++;
                if (i > 5) break;
            }
            string color = "";
            if (Classes == "Attacker") color = color + "0.427451 0.1843137 0.1254902 0.4474251";
            if (Classes == "Raider") color = color + "0.1294118 0.1333333 0.1529412 0.4470588";
            if (Classes == "Building") color = color + "0.3882353 0.3803922 0.2901961 0.4363736";


            CuiHelper.DestroyUi(player, "Checkclass2");
            CuiHelper.AddUi(player, Online
            .Replace("{images}", Images[Classes]).Replace("{0}", online.ToString()).Replace("{text}", topPvp).Replace("{color}", color));
        }

        void UIDraw(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(1, () => UIDraw(player));
                return;
            }
            if (!init)
            {
                timer.In(1f, () => UIDraw(player));
                return;
            }

            DestroyUI(player);
            CuiHelper.AddUi(player, UI
                .Replace("{Attacker}", Images["Attacker"])
                .Replace("{Building}", Images["Building"])
                .Replace("{Raider}", Images["Raider"])

                .Replace("{welcome}", Messages["Welcome"]).Replace("{name}", player.displayName));
        }

        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Checkclass2");
            CuiHelper.DestroyUi(player, "PlayersClasses_2");
            CuiHelper.DestroyUi(player, "PlayerClasses_UIBackground");
        }

        #endregion

        #region File Manager

        Dictionary<string, string> Images = new Dictionary<string, string>()
        {
            { "Attacker", "https://cdn1.savepice.ru/uploads/2018/6/1/5c0496f2fe440d505781187344fd957f-full.png" },
            { "Building", "https://cdn1.savepice.ru/uploads/2018/6/1/efbedcde6764aa565e6f67c5afc13c04-full.png" },
            { "Raider", "https://cdn1.savepice.ru/uploads/2018/6/1/3eafec68cf2fbdf9eeca12cb98f717f6-full.png" },

        };

        IEnumerator LoadImages()
        {
            foreach (var imgKey in Images.Keys.ToList())
            {
                yield return CommunityEntity.ServerInstance.StartCoroutine(
                    m_FileManager.LoadFile(imgKey, Images[imgKey]));
                Images[imgKey] = m_FileManager.GetPng(imgKey);
            }
        }

        private GameObject FileManagerObject;
        private FileManager m_FileManager;

        /// <summary>
        /// Инициализация скрипта взаимодействующего с файлами сервера
        /// </summary>
        void InitFileManager()
        {
            FileManagerObject = new GameObject("MAP_FileManagerObject");
            m_FileManager = FileManagerObject.AddComponent<FileManager>();
        }

        class FileManager : MonoBehaviour
        {
            int loaded = 0;
            int needed = 0;

            public bool IsFinished => needed == loaded;
            const ulong MaxActiveLoads = 10;
            Dictionary<string, FileInfo> files = new Dictionary<string, FileInfo>();
      //  Слив плагинов server-rust by Apolo YouGame

            private class FileInfo
      //  Слив плагинов server-rust by Apolo YouGame
            {
                public string Url;
                public string Png;
            }


            public string GetPng(string name) => files[name].Png;


            public IEnumerator LoadFile(string name, string url, int size = -1)
            {
                if (files.ContainsKey(name) && files[name].Url == url && !string.IsNullOrEmpty(files[name].Png)) yield break;
                files[name] = new FileInfo() { Url = url };
      //  Слив плагинов server-rust by Apolo YouGame
                needed++;

                yield return StartCoroutine(LoadImageCoroutine(name, url, size));

            }

            IEnumerator LoadImageCoroutine(string name, string url, int size = -1)
            {
                using (WWW www = new WWW(url))
                {
                    yield return www;
                    if (string.IsNullOrEmpty(www.error))
                    {
                        var bytes = size == -1 ? www.bytes : Resize(www.bytes, size);

                        var reply = 481;
                        if (reply == 0) { }
                        var entityId = CommunityEntity.ServerInstance.net.ID;
                        var crc32 = FileStorage.server.Store(bytes, FileStorage.Type.png, entityId).ToString();
                        files[name].Png = crc32;
                    }
                }
                loaded++;

            }

            static byte[] Resize(byte[] bytes, int size)
            {
                Image img = (Bitmap)(new ImageConverter().ConvertFrom(bytes));
                Bitmap cutPiece = new Bitmap(size, size);
                System.Drawing.Graphics graphic = System.Drawing.Graphics.FromImage(cutPiece);
                graphic.DrawImage(img, new Rectangle(0, 0, size, size), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel);
                graphic.Dispose();
                MemoryStream ms = new MemoryStream();
                cutPiece.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }

        #endregion

        #region API


        #endregion

        #region Messages

        Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"NoChose", "Вы состоите в класcе <color=#F0AE79>{0}</color>, ждите следующего вайпа!"},
            {"Welcome", "Приветствуем тебя {name}\nВыбери из списка ниже, какая роль тебе больше подходит<size=14>\n\t\t*каждая из ролей особенна и уникальна</size>"},
             {"NoUpgrade", "Извините, но Ваш уровень строителя, не позволяет улучшать объект выше текущего!"},
             {"NoBuildEntity", "Извините, но Ваш уровень строителя, не позволяет установить текущий объект, он возращен Вам в инвентарь!"},
             {"NoLockCode", "Извините, но Ваш уровень строителя, не позволяет использовать кодовый замок!"},
             {"DropWeapon", "Из за малаго опыта в стрельбе, от отдачи оружие вылетело у Вас из рук!"},
             {"CanEquipItem", "У Вас малый уровень в ношений взрывчатых веществ! Dзрывчатое вещество не может быть перенесено в активные слоты"},
             {"Chose", "Поздравляем! Вы выбрали класс {0}. Ваши приемущества: \n{1}\nЧтобы открыть общий рейтинг введите /globaltop, что бы ознакомиться со своей статистикой, введите /mystats"},
        };
        #endregion

    }
}
                
