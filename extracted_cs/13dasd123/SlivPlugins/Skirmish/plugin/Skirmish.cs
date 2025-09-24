// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿//Requires: EventHelper
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/* 1.0.1
 * Added API Calls: 
- void Skimish_StartRegistration(string activeArena, bool isFFA);
- void Skimish_EndGame();
- void Skimish_StartGame(string activeArena, bool isFFA);
 * Changed the message order in OnEntityTakeDamage.
 * Added a new UI to start and end the game. Type /sk
 */

namespace Oxide.Plugins
{
    [Info("Skirmish", "imthenewguy", "1.0.5")]
    [Description("This plugin was fixed by Инкуб to order [Rust Plugin Sliv]: https://discord.gg/pFgKw6Dyyq")]
    class Skirmish : RustPlugin
    {
        #region Config

        private Configuration config;
        public class Configuration
        {
            [JsonProperty("Maximum hits a player can take before they are out")]
            public int max_hits = 7;

            [JsonProperty("Maximum ammo capacity of a snowball launcher in the event")]
            public int max_ammo = 10;

            [JsonProperty("Minimum players required to start a game")]
            public int minimum_players = 2;

            [JsonProperty("How many seconds do the players have to register to player before we start")]
            public float seconds_before_game_starts = 300;

            [JsonProperty("Chance for a game to be FFA instead of TDM [%]")]
            public int ffa_chance = 40;

            [JsonProperty("Maximum time that the game can run for (seconds)")]
            public float max_time = 1800f;

            [JsonProperty("Sound effect")]
            public string damage_sound = "assets/prefabs/deployable/barricades/effects/damage.prefab";

            [JsonProperty("Reward dead players on the winning team")]
            public bool reward_dead = true;

            [JsonProperty("Free for all suit [shortname]")]
            public string ffa_suit = "hazmatsuit.nomadsuit";

            [JsonProperty("Team death match - team 1 suit [shortname]")]
            public string tdm1_suit = "hazmatsuit_scientist_peacekeeper";

            [JsonProperty("Team death match - team 2 suit [shortname]")]
            public string tdm2_suit = "hazmatsuit_scientist";

            [JsonProperty("Clear all data on map wipe")]
            public bool clear_data = true;

            [JsonProperty("Run the game automatically with EventHelper?")]
            public bool use_event_helper = true;

            [JsonProperty("How often should we start a game (if not being managed through EventHelper)")]
            public float auto_start_time = 7200;

            [JsonProperty("Clear NPCs from around the arena when the game starts?")]
            public bool delete_npcs = false;

            [JsonProperty("Max distance from anchor entities that NPCs will be deleted from on game start.")]
            public float delete_max_distance = 100f;

            [JsonProperty("Commands to prevent when a player are at the event")]
            public string[] prevent_commands = { "kit" };

            [JsonProperty("Automatically give players their items back after leaving [if running auto kit set to false]")]
            public bool kit_back_on_death = true;

            [JsonProperty("Announce when players join the event")]
            public bool announce_on_join = true;

            [JsonProperty("Maximum number of participants allowed per round (0 == no limit)")]
            public int max_players = 32;

            [JsonProperty("Prize table")]
            public List<PrizeInfo> prizes = new List<PrizeInfo>();

            [JsonProperty("Arena setups - Make sure your entity combinations and arena names are unique")]
            public Dictionary<string, ArenaInfo> arenas = new Dictionary<string, ArenaInfo>();

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
            config.arenas = default_Arenas;
            config.prizes = defaultPrizes;
        }

        public List<PrizeInfo> defaultPrizes
        {
            get
            {
                return new List<PrizeInfo>()
                {
                    new PrizeInfo("scrap", 50, 200),
                    new PrizeInfo("wood", 1500, 5000),
                    new PrizeInfo("stones", 1500, 5000),
                    new PrizeInfo("sulfur", 1000, 2000),
                    new PrizeInfo("potato", 5, 20, 0, "tasty potatoes")
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }

                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    PrintToConsole("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                PrintToConsole($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            PrintToConsole($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        public class ArenaInfo
        {
            public bool enabled = true;
            public string Anchor_Entity_Main;
            public string Anchor_Entity_Secondary;
            public ulong Anchor_id;
            public List<Vector3> FFA_Spawns = new List<Vector3>();
            public List<Vector3> TDM_Spawns_1 = new List<Vector3>();
            public List<Vector3> TDM_Spawns_2 = new List<Vector3>();
        }

        public class PrizeInfo
        {
            public string shortname;
            public ulong skin;
            public string displayname;
            public int min_amount;
            public int max_amount;
            public PrizeInfo(string shortname, int min_amount, int max_amount, ulong skin = 0, string displayname = null)
            {
                this.shortname = shortname;
                this.skin = skin;
                this.min_amount = min_amount;
                this.max_amount = max_amount;
                this.displayname = displayname;
            }
        }

        #endregion        

        #region Data

        PlayerEntity pcdData;
        private DynamicConfigFile PCDDATA;

        void Init()
        {
            PCDDATA = Interface.Oxide.DataFileSystem.GetFile(this.Name);
            LoadData();
            permission.RegisterPermission("skirmish.admin", this);
            UnsubHooks();
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "paintball_splats");
                CuiHelper.DestroyUi(player, "SkirmishStartPanel");
            }            
            EndGame();
            EventHelper.Call("EMRemoveEvent", this.Name);
            SaveData();
        }

        void SaveData()
        {
            PCDDATA.WriteObject(pcdData);
        }

        void LoadData()
        {
            try
            {
                pcdData = Interface.Oxide.DataFileSystem.ReadObject<PlayerEntity>(this.Name);
            }
            catch
            {
                Puts(lang.GetMessage("PlayerDataLoading", this));
                pcdData = new PlayerEntity();
            }
        }

        class PlayerEntity
        {
            public Dictionary<ulong, PlayerInfo> pEntity = new Dictionary<ulong, PlayerInfo>();
            public Vector3 lobby_point;
            public Dictionary<string, ArenaInfo> arenas = new Dictionary<string, ArenaInfo>();
        }

        class TeleportInfo
        {
            public List<Vector3> ffa_destinations = new List<Vector3>();
            public Dictionary<int, List<Vector3>> tdm_destinations = new Dictionary<int, List<Vector3>>();
        }

        class PlayerInfo
        {
            public int prizes;
        }

        #endregion;

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["EliminatedYou"] = "You have been eliminated by <color=#DFF008>{0}</color>.",
                ["EliminatedOther"] = "You eliminated <color=#DFF008>{0}</color>.",
                ["UnclaimedPrizes"] = "You have unclaimed prizes from winning round(s) of skirmish. Type <color=#13ff00>/skprize</color> to redeem them.",
                ["MissingLobby"] = "Attempted to start skirmish up but we are missing lobby destination.",
                ["GameStarting"] = "A game of skirmish at the <color=#ffae00>{0}</color> will start in <color=#ffae00>{1}</color> seconds. Type <color=#13ff00>/skjoin</color> to join.\nGame Type: <color=#ffae00>{2}</color>",
                ["SkirmishStart60"] = "Skirmish starts in <color=#ffae00>60 seconds</color>. Type <color=#13ff00>/skjoin</color> to join.",
                ["SkirmishStarted"] = "Skirmish registration has closed.",
                ["SkirmishCancelledNoPlayers"] = "Cancelling game due to low player count.",
                ["SkirmishCancelledNoArenas"] = "Could not find arena data for {0}.",
                ["GameStarted"] = "Skirmish has started!",
                ["EndNoWinners"] = "There were no winners of the skirmish match as everyone died.",
                ["EndWinnerSolo"] = "<color=#13ff00>{0}</color> has won the round!",
                ["EndWinnerTeam"] = "<color=#13ff00>Team {0}</color> are the winners:\n- {1}",
                ["MsgWinner"] = "You received a prize for winning (Available: <color=#13ff00>{0}</color>). Type <color=#13ff00>/skprize</color> to redeem it.",
                ["MsgWinnerTeam"] = "You received a prize for winning (Available: {0}). Type /skprize to redeem it.",
                ["ClearedData"] = "Cleared data",
                ["CannotRedeem"] = "You cannot redeem these while in a match.",
                ["NoPrizes"] = "You do not have any prizes outstanding.",
                ["NullPrize"] = "Error - prize was null",
                ["PrizeReceived"] = "You received <color=#13ff00>{0}x {1}</color>.",
                ["NoGamesRunning"] = "No games running.",
                ["NotJoiningAllowed"] = "You are not allowed to join this game.",
                ["AlreadyRegistered"] = "You have already registed for this event.",
                ["ErrorJoining"] = "Could not join event. Check print out from EventHelper for more info.",
                ["GameAlreadyRunning"] = "Game already running",
                ["EndedMatch"] = "Ended match",
                ["UnloadingSkirmish"] = "Unloading Skirmish...",
                ["NoLobbyFoundConsole"] = "No lobby point found. Type /sksetlobby while in game to set one.",
                ["ConsoleFoundStoredAnchors"] = "Found and stored {0} arenas into memory.",
                ["AutoStartingConsole"] = "Auto starting Skirmish",
                ["AddingPrizesConsole"] = "Adding prizes",
                ["ClearingAI"] = "Clearing AI from arenas.",
                ["SetLobbyManually"] = "Set the lobby postion to {0}",
                ["AnchorPairFound"] = "Found the {0} anchor from saved data.",
                ["SearchingAnchors"] = "Existing anchor data for {0} is invalid. Searching for anchor entity.",
                ["AnchorFailed"] = "Could not acquire anchors for {0}",
                ["NewAnchorFound"] = "Found new arena: {0}. Adding to data.",
                ["NoArenaData"] = "No arena data could be found.",
                ["PlayerDataLoading"] = "Couldn't load player data, creating new Playerfile",
                ["UITitle"] = "Skirmish Event",
                ["UIisRunning"] = "Game Running",
                ["UIArena"] = "Arena",
                ["UIMode"] = "Mode",
                ["UIDelayTitle"] = "Delay before start",
                ["UIStart"] = "START",
                ["UIStop"] = "STOP",
                ["EventFull"] = "This event is full.",
                ["JoinedMessage"] = "<color=#fbff00>{0}</color> joined the <color=#ffa200>Skirmish</color> lobby <color=#fbff00>[</color>{1}<color=#fbff00>]</color>"
            }, this);
        }

        #endregion

        #region Globals

        class PaintballInfo
        {
            public int hits_taken;
            public int team;
        }

        Dictionary<BasePlayer, PaintballInfo> Participants = new Dictionary<BasePlayer, PaintballInfo>();
        List<BasePlayer> corpseWatch = new List<BasePlayer>();
        private List<PlayerCorpse> Corpses = new List<PlayerCorpse>();
        const string perm_admin = "skirmish.admin";
        bool isRunning;
        bool AllowJoin;
        bool isFFA;
        bool GameStarted;
        Timer GameDelayTimer;
        List<BasePlayer> team_1 = new List<BasePlayer>();
        List<BasePlayer> team_2 = new List<BasePlayer>();
        string active_arena;
        Dictionary<string, BaseNetworkable> anchor_entities = new Dictionary<string, BaseNetworkable>();

        [PluginReference]
        private Plugin EventHelper, NightVision, ImageLibrary;

        #endregion

        #region Hooks
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            LeaveEvent(player);
        }


        object CanMountEntity(BasePlayer player, BaseMountable entity)
        {
            if (Participants.ContainsKey(player)) return false;
            return null;
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (active_arena == null) return;
            if (entity != null && Vector3.Distance(entity.transform.position, anchor_entities[active_arena].transform.position) < 80)
            {
                if (entity is ScientistNPC || entity is BaseAnimalNPC) entity.KillMessage();
            }
        }

        void OnServerInitialized(bool initial)
        {
            
            if (config.arenas == null || config.arenas.Count == 0)
            {
                config.arenas = default_Arenas;
                SaveConfig();
            }
            if (!FoundAnchors())
            {
                Puts(lang.GetMessage("UnloadingSkirmish", this));
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            if (pcdData.lobby_point == Vector3.zero) Puts(lang.GetMessage("NoLobbyFoundConsole", this));
            EventHelper.Call("EMCreateEvent", this.Name, config.use_event_helper, true, true, true, config.kit_back_on_death, true, pcdData.lobby_point);
            EventHelper.Call("EMExternalPluginSettings", this.Name);
            EventHelper.Call("EMBlackListCommands", this.Name, config.prevent_commands);
            Puts(string.Format(lang.GetMessage("ConsoleFoundStoredAnchors", this), anchor_entities.Count));

            splats = new List<string>()
            {
                "splat1", "splat2", "splat3", "splat4", "splat5", "splat6"
            };

            ImageLibrary?.Call("AddImage", "https://gspics.org/images/2024/01/21/0lmvNN.png", "splat1");
            ImageLibrary?.Call("AddImage", "https://gspics.org/images/2024/01/21/0lmXHw.png", "splat2");
            ImageLibrary?.Call("AddImage", "https://gspics.org/images/2024/01/21/0lmgwh.png", "splat3");
            ImageLibrary?.Call("AddImage", "https://gspics.org/images/2024/01/21/0lm3Ga.png", "splat4");
            ImageLibrary?.Call("AddImage", "https://gspics.org/images/2024/01/21/0lmW8x.png", "splat5");
            ImageLibrary?.Call("AddImage", "https://gspics.org/images/2024/01/21/0lm6oQ.png", "splat6");
            if (!config.use_event_helper)
            {
                if (config.auto_start_time > 0) GameDelayTimer = timer.Every(config.auto_start_time, () =>
                {
                    Puts(lang.GetMessage("AutoStartingConsole", this));
                    StartRegistration();
                });
            }            
            if (config.prizes == null || config.prizes.Count == 0)
            {
                Puts(lang.GetMessage("AddingPrizesConsole", this));
                config.prizes = defaultPrizes;
                SaveConfig();
            }
            Puts(lang.GetMessage("ClearingAI", this));
            foreach (var arena in anchor_entities)
            {
                ClearAI(arena.Value.transform.position);
            }
        }

        void ClearAI(Vector3 pos)
        {
            if (!config.delete_npcs) return;
            var ai = BaseNetworkable.serverEntities.Where(x => (x is BaseAnimalNPC || x is ScientistNPC) && Vector3.Distance(x.transform.position, pos) < config.delete_max_distance).ToList();
            if (ai == null || ai.Count == 0) return;
            foreach (var entity in ai.ToList())
            {
                entity.KillMessage();
            }
        }

        object CanDropActiveItem(BasePlayer player)
        {
            if (Participants.ContainsKey(player)) return false;
            return null;
        }

        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (Participants.ContainsKey(player)) return false;
            return null;
        }

        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (!Participants.ContainsKey(player)) return;
            if (NightVision != null)
            {
                NightVision.Call("UnlockPlayerTime", player);
                permission.RevokeUserPermission(player.UserIDString, "nightvision.allowed");
            }
            corpseWatch.Add(player);
            Participants.Remove(player);
            CheckWin();
        }
        void OnNewSave(string filename)
        {
            pcdData.arenas.Clear();
            pcdData.lobby_point = Vector3.zero;
            if (config.clear_data)
            {
                ClearData();
            }
        }

        object OnEntityTakeDamage(BasePlayer player, HitInfo info)
        {
            PaintballInfo pi;
            if (player == null || info == null || !Participants.TryGetValue(player, out pi)) return null;
            var attacker = info.InitiatorPlayer;
            if (attacker == null || !Participants.ContainsKey(attacker)) return null;
            PaintballInfo api;
            if (!Participants.TryGetValue(attacker, out api) || (api.team == pi.team && !isFFA)) return null;
            var weapon = attacker.GetActiveItem();
            if (weapon == null) return null;
            if (weapon.info.shortname == "snowball" || weapon.info.shortname == "snowballgun")
            {
                pi.hits_taken++;
                if (pi.hits_taken >= config.max_hits)
                {
                    PrintToChat(player, string.Format(lang.GetMessage("EliminatedYou", this, player.UserIDString), attacker.displayName));
                    PrintToChat(attacker, string.Format(lang.GetMessage("EliminatedOther", this, player.UserIDString), player.displayName));
                    LeaveEvent(player);
                    CheckWin();                    
                }
                else
                {
                    SendSplats(player, pi.hits_taken);
                    EffectNetwork.Send(new Effect(config.damage_sound, player.transform.position, player.transform.position), player.net.connection);
                }
                return false;
            }
            return null;
        }

        void OnPlayerCorpseSpawned(BasePlayer player, PlayerCorpse corpse)
        {
            if (!isRunning) return;
            if (corpseWatch.Contains(player))
            {
                Corpses.Add(corpse);
                corpseWatch.Remove(player);
            }
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "nightvision.allowed")) permission.RevokeUserPermission(player.UserIDString, "nightvision.allowed");
            if (pcdData.pEntity.ContainsKey(player.userID) && pcdData.pEntity[player.userID].prizes > 0) PrintToChat(player, lang.GetMessage("UnclaimedPrizes", this, player.UserIDString));
            if (Convert.ToBoolean(EventHelper.Call("EMIsParticipating", player, this.Name)))
            {
                EventHelper.Call("EMPlayerLeaveEvent", player, this.Name);
            }
        }

        object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (Participants.ContainsKey(player)) return false;
            return null;
        }

        object CanLootEntity(BasePlayer player, LootableCorpse container)
        {
            if (Corpses.Contains(container) && !Participants.ContainsKey(player)) return false;
            return null;
        }

        #endregion

        #region Helpers

        void UnsubHooks()
        {
            Unsubscribe("OnEntityTakeDamage");
            Unsubscribe("CanDropActiveItem");
            Unsubscribe("OnItemAction");
            Unsubscribe("OnPlayerDeath");
            Unsubscribe("OnPlayerCorpseSpawned");
            Unsubscribe("CanEntityTakeDamage");
            Unsubscribe("CanMountEntity");
            Unsubscribe("CanLootEntity");
            Unsubscribe("OnExitZone");
            Unsubscribe("OnEnterZone");
            Unsubscribe("OnEntitySpawned");
            Unsubscribe("OnPlayerDisconnected");
        }

        void DeleteTimers()
        {
            foreach (var t in _timers)
            {
                if (!t.Destroyed) t.Destroy();
            }
            _timers.Clear();
        }

        void EndGame()
        {
            // Do end game stuff
            Interface.Call("Skimish_EndGame");
            DeleteTimers();
            if (Participants != null && Participants.Count > 0)
            {
                foreach (var player in Participants.Keys.ToList())
                {
                    if (NightVision != null)
                    {
                        NightVision.Call("UnlockPlayerTime", player);
                        permission.RevokeUserPermission(player.UserIDString, "nightvision.allowed");
                    }

                    if (player.IsAlive() && player.IsConnected)
                    {
                        EventHelper.Call("EMPlayerLeaveEvent", player, this.Name);
                    }
                    Participants.Remove(player);
                }
            }
            if (Corpses.Count > 0)
            {
                foreach (var corpse in Corpses.ToList())
                {
                    if (corpse != null) corpse.Kill();
                }
            }            
            Corpses.Clear();
            corpseWatch.Clear();
            if (BasePlayer.activePlayerList != null && BasePlayer.activePlayerList.Count > 0)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(player, "paintball_splats");
                }
            }     
            
            team_1.Clear();
            team_2.Clear();                       

            if (!config.use_event_helper)
            {
                if (GameDelayTimer != null && !GameDelayTimer.Destroyed) GameDelayTimer.Destroy();
                if (config.auto_start_time > 0) GameDelayTimer = timer.Every(config.auto_start_time, () =>
                {
                    Puts(lang.GetMessage("AutoStartingConsole", this));
                    StartRegistration();
                });
            }            
            UnsubHooks();
            active_arena = null;
            if (isRunning)
            {
                EventHelper.Call("EMEndEvent", this.Name);
                PrintToChat(lang.GetMessage("EndedMatch", this));
                isRunning = false;
            }
            
            AllowJoin = false;
            GameStarted = false;
        }

        List<Timer> _timers = new List<Timer>();        

        private void StartRegistration(float time_override = 0, string arena = null, string mode = null)
        {
            if (isRunning) return;
            if (GameDelayTimer != null && GameDelayTimer.Destroyed) GameDelayTimer.Destroy();

            var randomArena = pcdData.arenas.ToList().GetRandom();
            if (!string.IsNullOrEmpty(arena))
            {
                randomArena = new KeyValuePair<string, ArenaInfo>(arena, config.arenas[arena]);
            }

            if (pcdData.lobby_point == Vector3.zero || (randomArena.Value.FFA_Spawns == null && config.ffa_chance > 0) || ((randomArena.Value.TDM_Spawns_1.Count == 0 || randomArena.Value.TDM_Spawns_2.Count == 0) && config.ffa_chance < 100))
            {
                PrintToChat(lang.GetMessage("MissingLobby", this));
                return;
            }

            // Set the active arena key to active_arena var.
            
            active_arena = randomArena.Key;            
            Subscribe("OnPlayerDeath");
            Subscribe("OnPlayerCorpseSpawned");
            Subscribe("CanMountEntity");
            Subscribe("CanLootEntity");
            Subscribe("OnPlayerDisconnected");
            EventHelper.Call("EMStartEvent", this.Name);
            isRunning = true;
            AllowJoin = true;
            if (string.IsNullOrEmpty(mode))
            {
                if (config.ffa_chance == 100) isFFA = true;
                else if (config.ffa_chance == 0) isFFA = false;
                else if (UnityEngine.Random.Range(0, 100) < config.ffa_chance + 1) isFFA = true;
                else isFFA = false;
            }
            else
            {
                if (mode == "ffa") isFFA = true;
                else isFFA = false;
            }

            var str = isFFA ? "Free-For-All" : "Team Death Match";
            var time_to_start = time_override > 0 ? time_override : config.seconds_before_game_starts;
            PrintToChat(string.Format(lang.GetMessage("GameStarting", this), randomArena.Key, time_to_start, str));

            Interface.Call("Skimish_StartRegistration", active_arena, isFFA);

            if (time_to_start > 60)
            {
                _timers.Add(timer.Once(time_to_start - 60, () =>
                {
                    PrintToChat(lang.GetMessage("SkirmishStart60", this));
                    return;
                }));
            }
            _timers.Add(timer.Once(time_to_start, () =>
            {
                PrintToChat(lang.GetMessage("SkirmishStarted", this));
                StartGame();
            }));
        }

        void StartGame()
        {
            AllowJoin = false;
            Subscribe("OnEntityTakeDamage");
            Subscribe("CanDropActiveItem");
            Subscribe("OnItemAction");
            Subscribe("CanEntityTakeDamage");
            Subscribe("OnEntitySpawned");
            if (Participants.Count < config.minimum_players)
            {
                PrintToChat(lang.GetMessage("SkirmishCancelledNoPlayers", this));
                EndGame();
                return;
            }
            ArenaInfo arenaData;
            if (!pcdData.arenas.TryGetValue(active_arena, out arenaData))
            {
                Puts(string.Format(lang.GetMessage("SkirmishCancelledNoArenas", this), active_arena));
                EndGame();
                return;
            }
            Interface.Call("Skimish_StartGame", active_arena, isFFA);
            PrintToChat(lang.GetMessage("GameStarted", this));
            GameStarted = true;
            Puts(lang.GetMessage("ClearingAI", this));
            ClearAI(anchor_entities[active_arena].transform.position);
            if (isFFA)
            {
                List<Vector3> used = new List<Vector3>();
                foreach (var player in Participants.Keys)
                {
                    if (used.Count == arenaData.FFA_Spawns.Count) used.Clear();
                    Vector3 RandomPos = arenaData.FFA_Spawns.Where(x => !used.Contains(x)).ToList().GetRandom();
                    player.flyhackPauseTime = 10f;
                    Player.Teleport(player, RandomPos);
                    player.StartSleeping();
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
                    player.ClientRPCPlayer(null, player, "StartLoading");
                    player.SendEntityUpdate();
                    player.UpdateNetworkGroup();
                    player.SendNetworkUpdateImmediate(false);
                    GiveSnowballLauncher(player);
                }
            }
            else
            {
                System.Random rand = new System.Random();
                var lastGiven = 0;
                foreach (var player in Participants.OrderBy(x => rand.Next()).ToDictionary(item => item.Key, item => item.Value))
                {
                    if (lastGiven == 0)
                    {
                        player.Value.team = 1;
                        lastGiven++;
                    }
                    else if (lastGiven == 1)
                    {
                        player.Value.team = 2;
                        lastGiven++;
                    }
                    else
                    {
                        player.Value.team = 1;
                        lastGiven = 1;
                    }
                    player.Key.flyhackPauseTime = 10f;
                    if (player.Value.team == 1) Player.Teleport(player.Key, arenaData.TDM_Spawns_1.GetRandom());
                    else Player.Teleport(player.Key, arenaData.TDM_Spawns_2.GetRandom());
                    player.Key.StartSleeping();
                    player.Key.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
                    player.Key.ClientRPCPlayer(null, player.Key, "StartLoading");
                    player.Key.SendEntityUpdate();
                    player.Key.UpdateNetworkGroup();
                    player.Key.SendNetworkUpdateImmediate(false);
                    GiveSnowballLauncher(player.Key, player.Value.team);
                    if (player.Value.team == 1) team_1.Add(player.Key);
                    else team_2.Add(player.Key);
                }
            }
            _timers.Add(timer.Once(config.max_time, () =>
            {
                EndGame();
            }));
        }        

        void ClearData()
        {            
            pcdData.pEntity.Clear();
            SaveData();
        }

        private void LeaveEvent(BasePlayer player, bool manually_left = false, bool died = false)
        {
            if (Participants.ContainsKey(player)) Participants.Remove(player);
            if (NightVision != null)
            {
                NightVision.Call("UnlockPlayerTime", player);
                permission.RevokeUserPermission(player.UserIDString, "nightvision.allowed");
            }
            
            if (!died)
            {
                EventHelper.Call("EMPlayerLeaveEvent", player, this.Name, manually_left);
            }
                
            CuiHelper.DestroyUi(player, "paintball_splats");
            CheckWin();
        }

        void GiveSnowballLauncher(BasePlayer player, int team = 0)
        {
            var item = ItemManager.CreateByName("snowballgun", 1, 0);
            (item.GetHeldEntity() as BaseProjectile).primaryMagazine.capacity = config.max_ammo;
            player.GiveItem(item);

            for (int i = 0; i < 24 - player.inventory.containerMain.itemList.Count; i++)
            {
                player.GiveItem(ItemManager.CreateByName("snowball", 10));
            }

            if (team == 0)
            {
                ItemManager.CreateByName(config.ffa_suit, 1).MoveToContainer(player.inventory.containerWear);
            }
            else if (team == 1) ItemManager.CreateByName(config.tdm1_suit, 1).MoveToContainer(player.inventory.containerWear);
            else ItemManager.CreateByName(config.tdm2_suit, 1).MoveToContainer(player.inventory.containerWear);
        }

        void CheckWin()
        {
            if (!isRunning || !GameStarted) return;
            if (Participants.Count == 0)
            {
                PrintToChat(lang.GetMessage("EndNoWinners", this));
                EndGame();
                return;
            }
            if (isFFA)
            {
                if (Participants.Count == 1)
                {
                    var winner = Participants.Keys.First();
                    PrintToChat(string.Format(lang.GetMessage("EndWinnerSolo", this), winner.displayName));
                    PlayerInfo pi;
                    if (!pcdData.pEntity.TryGetValue(winner.userID, out pi)) pcdData.pEntity.Add(winner.userID, pi = new PlayerInfo());
                    PrintToChat(winner, string.Format(lang.GetMessage("MsgWinner", this, winner.UserIDString), pi.prizes));
                    pi.prizes++;
                    Interface.CallHook("SKWinner", winner);
                    EndGame();
                }
            }
            else
            {
                var team_1_count = 0;
                var team_2_count = 0;
                foreach (var player in Participants)
                {
                    if (player.Value.team == 1) team_1_count++;
                    else if (player.Value.team == 2) team_2_count++;
                }
                if (team_2_count <= 0 || team_1_count <= 0)
                {
                    var str = team_1_count <= 0 ? "2" : "1";
                    List<BasePlayer> winners;
                    if (config.reward_dead)
                    {
                        if (str == "1") winners = team_1;
                        else winners = team_2;
                    }
                    else winners = Participants.Keys.ToList();
                    PrintToChat(string.Format(lang.GetMessage("EndWinnerTeam", this), str, string.Join("\n- ", Participants.Keys.Select(x => x.displayName).ToList())));
                    foreach (var player in winners)
                    {
                        if (player == null) continue;
                        PlayerInfo pi;
                        if (!pcdData.pEntity.TryGetValue(player.userID, out pi)) pcdData.pEntity.Add(player.userID, pi = new PlayerInfo());
                        pi.prizes++;
                        PrintToChat(player, string.Format(lang.GetMessage("MsgWinner", this, player.UserIDString), pi.prizes));
                    }
                    Interface.CallHook("SKWinners", winners);
                    EndGame();
                }
            }
        }

        #endregion

        #region Chat Commands

        [ChatCommand("skcleardata")]
        void ClearSkirmishData(BasePlayer player)
        {
            ClearData();
            PrintToChat(player, lang.GetMessage("ClearedData", this, player.UserIDString));
        }

        [ChatCommand("sksetlobby")]
        void SetLobby(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_admin)) return;
            pcdData.lobby_point = player.transform.position;
            EventHelper.Call("EMUpdateLobby", this.Name, pcdData.lobby_point);
            PrintToChat(player, string.Format(lang.GetMessage("SetLobbyManually", this, player.UserIDString), player.transform.position));
        }

        [ChatCommand("skprize")]
        void RedeemReward(BasePlayer player)
        {
            if (Participants.ContainsKey(player))
            {
                PrintToChat(player, lang.GetMessage("CannotRedeem", this, player.UserIDString));
                return;
            }
            PlayerInfo pi;
            if (!pcdData.pEntity.TryGetValue(player.userID, out pi) || pi.prizes <= 0)
            {
                PrintToChat(player, lang.GetMessage("NoPrizes", this, player.UserIDString));
                return;
            }
            var prize = config.prizes.GetRandom();
            if (prize == null)
            {
                PrintToChat(player, lang.GetMessage("NullPrize", this, player.UserIDString));
                return;
            }
            var item = ItemManager.CreateByName(prize.shortname, UnityEngine.Random.Range(prize.min_amount, prize.max_amount + 1), prize.skin);
            if (item == null) return;
            if (prize.displayname != null) item.name = prize.displayname;
            player.GiveItem(item);
            PrintToChat(player, string.Format(lang.GetMessage("PrizeReceived", this, player.UserIDString), item.amount, item.name ?? item.info.displayName.english));
            pi.prizes--;
            if (pi.prizes == 0) pcdData.pEntity.Remove(player.userID);
        }

        [ChatCommand("skjoin")]
        void RegisterPlayer(BasePlayer player)
        {
            if (!isRunning)
            {
                PrintToChat(player, lang.GetMessage("NoGamesRunning", this, player.UserIDString));
                return;
            }

            if (!AllowJoin)
            {
                PrintToChat(player, lang.GetMessage("NotJoiningAllowed", this, player.UserIDString));
                return;
            }

            if (Participants.ContainsKey(player))
            {
                PrintToChat(player, lang.GetMessage("AlreadyRegistered", this, player.UserIDString));
                return;
            }

            if (config.max_players > 0 && Participants.Count >= config.max_players)
            {
                PrintToChat(player, lang.GetMessage("EventFull", this, player.UserIDString));
                return;
            }

            if (!Convert.ToBoolean(EventHelper.Call("EMEnrollPlayer", player, this.Name)))
            {
                PrintToChat(player, lang.GetMessage("ErrorJoining", this, player.UserIDString));
                return;
            }

            if (NightVision != null)
            {
                permission.GrantUserPermission(player.UserIDString, "nightvision.allowed", NightVision);
                NightVision.Call("LockPlayerTime", player, 10f);
            }            

            Participants.Add(player, new PaintballInfo());

            if (config.announce_on_join)
            {
                var str = config.max_players > 0 ? $"{Participants.Count}/{config.max_players}" : Participants.Count.ToString();
                PrintToChat(string.Format(lang.GetMessage("JoinedMessage", this), player.displayName, str));
            }
        }

        [ChatCommand("skleave")]
        void LeaveSkirmish(BasePlayer player)
        {
            if (Participants.ContainsKey(player))
            {
                LeaveEvent(player, true);
            }
        }

        [ChatCommand("skstart")]
        void StartSkirmishCMD(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_admin)) return;
            if (isRunning)
            {
                PrintToChat(player, lang.GetMessage("GameAlreadyRunning", this, player.UserIDString));
                return;
            }

            var time_override = 0f;
            if (args.Length == 1 && args[0].IsNumeric()) time_override = Convert.ToSingle(args[0]);

            if (config.use_event_helper)
            {
                EventHelper.Call("EMManuallyStarted", this.Name);
            }
            StartRegistration(time_override);
        }

        [ChatCommand("skend")]
        void EndSkirmishCMD(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_admin)) return;
            EndGame();
        }

        #endregion

        #region API

        object CanEntityTakeDamage(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null) return null;
            if (!player.IsNpc && player.userID.IsSteamId())
            {
                PaintballInfo pi;
                if (!Participants.TryGetValue(player, out pi)) return null;
                var attacker = info.InitiatorPlayer;
                if (attacker == null || attacker.IsNpc || !attacker.userID.IsSteamId()) return null;
                if (!Participants.ContainsKey(attacker)) return false;
                PaintballInfo api;
                if (!Participants.TryGetValue(attacker, out api)) return null;

                if (pi.team == api.team && !isFFA) return false;
                return false;
            }
            return null;
        }

        void EMEndGame(string eventName)
        {
            if (eventName == this.Name)
            {
                EndGame();
            }
        }

        void EMStartNextEvent(string eventName)
        {
            if (eventName == this.Name)
            {
                StartRegistration();
            }
        }

        #endregion

        #region Splats

        List<string> splats;

        void SendSplats(BasePlayer player, int quantity)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1254902 0.1254902 0.1254902 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-16.003 -16", OffsetMax = "15.997 16" }
            }, "Overlay", "paintball_splats");

            if (quantity >= 1)
            {
                container.Add(new CuiElement
                {
                    Name = "splat_1",
                    Parent = "paintball_splats",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 0.8", Png = (string)ImageLibrary?.Call("GetImage", splats.GetRandom()) },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "-501.997 13.337", OffsetMax = "-245.997 269.337" }
                }
                });
            }

            if (quantity >= 2)
            {
                container.Add(new CuiElement
                {
                    Name = "splat_2",
                    Parent = "paintball_splats",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 0.8", Png = (string)ImageLibrary?.Call("GetImage", splats.GetRandom()) },
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "246.004 13.677", OffsetMax = "502.004 269.677" }
                }
                });
            }

            if (quantity >= 3)
            {
                container.Add(new CuiElement
                {
                    Name = "splat_3",
                    Parent = "paintball_splats",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 0.8", Png = (string)ImageLibrary?.Call("GetImage", splats.GetRandom()) },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "-501.997 -269.997", OffsetMax = "-245.997 -13.997" }
                }
                });
            }

            if (quantity >= 4)
            {
                container.Add(new CuiElement
                {
                    Name = "splat_4",
                    Parent = "paintball_splats",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 0.8", Png = (string)ImageLibrary?.Call("GetImage", splats.GetRandom()) },
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "246.004 -269.657", OffsetMax = "502.004 -13.657" }
                }
                });
            }

            if (quantity >= 5)
            {
                container.Add(new CuiElement
                {
                    Name = "splat_5",
                    Parent = "paintball_splats",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 0.8", Png = (string)ImageLibrary?.Call("GetImage", splats.GetRandom()) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-261.997 -127.997", OffsetMax = "-5.997 128.003" }
                }
                });
            }

            if (quantity >= 6)
            {
                container.Add(new CuiElement
                {
                    Name = "splat_6",
                    Parent = "paintball_splats",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 0.8", Png = (string)ImageLibrary?.Call("GetImage", splats.GetRandom()) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "6.003 -127.997", OffsetMax = "262.003 128.003" }
                }
                });
            }

            CuiHelper.DestroyUi(player, "paintball_splats");
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Find Anchors
                
        bool FoundAnchors()
        {
            foreach (var anchor_pair in config.arenas)
            {
                if (!anchor_pair.Value.enabled) continue;
                ArenaInfo arenaData;
                if (pcdData.arenas.TryGetValue(anchor_pair.Key, out arenaData))
                {
                    NetworkableId anchorId = new NetworkableId(arenaData.Anchor_id);
                    var anchor = BaseNetworkable.serverEntities.Find(anchorId);
                    if (anchor != null)
                    {
                        Puts(string.Format(lang.GetMessage("AnchorPairFound", this), anchor_pair.Key));
                        if (!anchor_entities.ContainsKey(anchor_pair.Key)) anchor_entities.Add(anchor_pair.Key, anchor);
                        if (arenaData.FFA_Spawns.Count == 0) arenaData.FFA_Spawns = ConvertLocalsToWorld(anchor, anchor_pair.Value.FFA_Spawns);
                        if (arenaData.TDM_Spawns_1.Count == 0) arenaData.TDM_Spawns_1 = ConvertLocalsToWorld(anchor, anchor_pair.Value.TDM_Spawns_1);
                        if (arenaData.TDM_Spawns_2.Count == 0) arenaData.TDM_Spawns_2 = ConvertLocalsToWorld(anchor, anchor_pair.Value.TDM_Spawns_2);                        
                        continue;
                    }
                    else
                    {
                        Puts(string.Format(lang.GetMessage("SearchingAnchors", this), anchor_pair.Key));
                        arenaData.FFA_Spawns.Clear();
                        arenaData.TDM_Spawns_1.Clear();
                        arenaData.TDM_Spawns_2.Clear();
                        arenaData.Anchor_id = 0;
                    }                        
                }                

                List<BaseNetworkable> anchor_main = new List<BaseNetworkable>();
                List<BaseNetworkable> anchor_second = new List<BaseNetworkable>();
                foreach (var entity in BaseNetworkable.serverEntities)
                {
                    if (entity.PrefabName == anchor_pair.Value.Anchor_Entity_Main) anchor_main.Add(entity);
                    else if (entity.PrefabName == anchor_pair.Value.Anchor_Entity_Secondary) anchor_second.Add(entity);
                }
                if (anchor_main.Count == 0 || anchor_second.Count == 0)
                {
                    Puts(string.Format(lang.GetMessage("AnchorFailed", this), anchor_pair.Key));
                    continue;
                }
                var found_anchor = false;
                foreach (var main in anchor_main)
                {
                    foreach (var second in anchor_second)
                    {
                        if (Vector3.Distance(second.transform.position, main.transform.position) < 1)
                        {
                            // Saves the anchor information to a dictionary for use in running the games.
                            anchor_entities.Add(anchor_pair.Key, main);

                            // Saves the info from the config to the data file with the ID of the anchor entity.
                            ArenaInfo ai;
                            if (!pcdData.arenas.TryGetValue(anchor_pair.Key, out ai)) pcdData.arenas.Add(anchor_pair.Key, ai = new ArenaInfo());
                            ai.Anchor_id = main.net.ID.Value;
                            ai.FFA_Spawns = ConvertLocalsToWorld(main, anchor_pair.Value.FFA_Spawns);
                            ai.TDM_Spawns_1 = ConvertLocalsToWorld(main, anchor_pair.Value.TDM_Spawns_1);
                            ai.TDM_Spawns_2 = ConvertLocalsToWorld(main, anchor_pair.Value.TDM_Spawns_2);
                            ai.Anchor_Entity_Main = main.PrefabName;
                            ai.Anchor_Entity_Secondary = second.PrefabName;
                            found_anchor = true;
                            Puts(string.Format(lang.GetMessage("NewAnchorFound", this), anchor_pair.Key));
                            break;
                        }
                    }
                    if (found_anchor) break;
                }
            }
            if (anchor_entities.Count > 0)
            {
                if (pcdData.lobby_point == Vector3.zero && anchor_entities.ContainsKey("Warehouse Arena"))
                {
                    pcdData.lobby_point = ConvertLocalsToWorld(anchor_entities["Warehouse Arena"], new Vector3(0.742f, -16.106f, -1.204f));
                }
                return true;
            }
                
            Puts(lang.GetMessage("NoArenaData", this));
            return false;
        }

        List<Vector3> ConvertLocalsToWorld(BaseNetworkable anchor_entity, List<Vector3> locs)
        {
            var locations = new List<Vector3>();
            foreach (var loc in locs)
            {
                locations.Add(anchor_entity.transform.localToWorldMatrix.MultiplyPoint3x4(loc));
            }            
            return locations;
        }

        Vector3 ConvertLocalsToWorld(BaseNetworkable anchor_entity, Vector3 loc)
        {
            var pos = anchor_entity.transform.localToWorldMatrix.MultiplyPoint3x4(loc);
            return pos;
        }

        // Default arena data
        Dictionary<string, ArenaInfo> default_Arenas
        {
            get
            {
                return new Dictionary<string, ArenaInfo>() 
                {
                    ["Warehouse Arena"] = new ArenaInfo()
                    {
                        Anchor_Entity_Main = "assets/prefabs/misc/xmas/pookie/pookie_deployed.prefab",
                        Anchor_Entity_Secondary = "assets/prefabs/deployable/mailbox/mailbox.deployed.prefab",
                        FFA_Spawns = new List<Vector3>()
                        {
                            new Vector3(-18.09f, 1.567f, 25.083f),
                            new Vector3(-11.861f, 1.567f, 27.353f),
                            new Vector3(-7.896f, 1.567f, 27.404f),
                            new Vector3(-3.6f, 1.567f, 25.021f),
                            new Vector3(-1.249f, 1.578f, 15.091f),
                            new Vector3(-7.236f, 1.565f, 12.561f),
                            new Vector3(7.076f, 1.565f, 11.799f),
                            new Vector3(1.638f, 1.567f, 20.928f),
                            new Vector3(8.593f, 1.575f, 27.294f),
                            new Vector3(12.395f, 1.575f, 27.227f),
                            new Vector3(19.636f, 1.582f, 21.318f),
                            new Vector3(19.444f, 1.567f, 3.447f),
                            new Vector3(12.205f, 1.612f, -8.473f),
                            new Vector3(16.503f, 1.572f, -19.868f),
                            new Vector3(12.723f, 1.587f, -23.226f),
                            new Vector3(8.542f, 1.587f, -23.29f),
                            new Vector3(4.214f, 1.567f, -19.182f),
                            new Vector3(-1.611f, 1.571f, -20.288f),
                            new Vector3(-8.427f, 1.646f, -23.608f),
                            new Vector3(-11.825f, 1.567f, -23.566f),
                            new Vector3(-16.483f, 1.587f, -20.432f),
                            new Vector3(-16.656f, 1.587f, -13.179f),
                            new Vector3(-10.257f, 1.604f, -8.255f),
                            new Vector3(-19.483f, 1.567f, 3.004f),
                            new Vector3(-16.448f, 1.58f, 12.12f),
                            new Vector3(-16.551f, 1.567f, 21.461f),
                            new Vector3(-6.834f, 5.965f, -13.09f),
                            new Vector3(7.482f, 5.965f, 12.146f),
                            new Vector3(2.35f, 1.565f, -9.646f),
                            new Vector3(7.323f, 1.565f, -10.765f),
                            new Vector3(-6.814f, 1.565f, -14.052f),
                            new Vector3(-1.795f, 1.565f, -9.999f),
                            new Vector3(-5.631f, 1.565f, 11.559f),
                            new Vector3(0.314f, 4.58f, 2.797f),
                            new Vector3(0.064f, 4.565f, -7.918f)
                        },
                        TDM_Spawns_1 = new List<Vector3>()
                        {
                            new Vector3(19.119f, 1.567f, 24.503f),
                            new Vector3(16.676f, 1.567f, 24.496f),
                            new Vector3(16.342f, 1.567f, 27.073f),
                            new Vector3(18.975f, 1.567f, 26.811f)

                        },
                        TDM_Spawns_2 = new List<Vector3>()
                        {
                            new Vector3(-16.463f, 1.567f, 24.334f),
                            new Vector3(-16.594f, 1.567f, 26.78f),
                            new Vector3(-18.845f, 1.567f, 26.62f),
                            new Vector3(-18.935f, 1.567f, 24.128f)
                        }
                    },
                    ["Sewer Arena"] = new ArenaInfo()
                    {
                        Anchor_Entity_Main = "assets/prefabs/misc/xmas/pookie/pookie_deployed.prefab",
                        Anchor_Entity_Secondary = "assets/prefabs/deployable/chair/chair.deployed.prefab",
                        FFA_Spawns = new List<Vector3>()
                        {
                            new Vector3(-32.966f, -0.661f, 0.052f),
                            new Vector3(-28.292f, -0.694f, -22.544f),
                            new Vector3(-27.292f, -0.741f, 23.744f),
                            new Vector3(-22.03f, -2.096f, 14.357f),
                            new Vector3(-23.863f, -1.414f, -1.905f),
                            new Vector3(-21.762f, -2.079f, -14.928f),
                            new Vector3(-10.574f, -0.778f, -15.501f),
                            new Vector3(-9.371f, -0.739f, -4.939f),
                            new Vector3(0.004f, -0.774f, 10.399f),
                            new Vector3(0.694f, -0.705f, 16.884f),
                            new Vector3(-0.124f, -0.778f, 22.467f),
                            new Vector3(17.296f, -0.739f, 28.733f),
                            new Vector3(29.786f, -0.632f, 23.27f),
                            new Vector3(33.676f, -0.714f, 12.538f),
                            new Vector3(33.304f, -0.741f, -5.466f),
                            new Vector3(30.061f, -0.708f, -22.633f),
                            new Vector3(15.53f, -0.757f, -28.383f),
                            new Vector3(6.713f, -0.706f, -22.095f),
                            new Vector3(12.23f, -0.685f, -11.705f),
                            new Vector3(20.053f, -1.298f, -6.459f),
                            new Vector3(20.102f, -1.321f, 13.531f),
                            new Vector3(25.809f, -1.359f, 4.636f),
                            new Vector3(2.627f, -0.746f, 4.501f),
                            new Vector3(0.618f, -0.752f, 10.668f),
                            new Vector3(0.85f, -0.731f, -10.819f),
                            new Vector3(2.278f, -0.695f, -16.052f),
                            new Vector3(-9.461f, -0.652f, -28.131f)

                        },
                        TDM_Spawns_1 = new List<Vector3>()
                        {
                            new Vector3(-24.704f, -1.352f, -6.756f),
                            new Vector3(-24.729f, -1.351f, -1.602f),
                            new Vector3(-24.652f, -1.357f, 3.617f),
                            new Vector3(-19.113f, -1.345f, 6.184f),
                            new Vector3(-18.86f, -1.32f, 0.053f),
                            new Vector3(-18.937f, -1.328f, -6.04f)
                        },
                        TDM_Spawns_2 = new List<Vector3>()
                        {
                            new Vector3(25.696f, -1.363f, 5.834f),
                            new Vector3(25.55f, -1.374f, -2.212f),
                            new Vector3(25.423f, -1.368f, -6.464f),
                            new Vector3(19.949f, -1.316f, -5.366f),
                            new Vector3(19.882f, -1.309f, -0.527f),
                            new Vector3(20.149f, -1.334f, 5.067f)
                        }
                    },
                    ["Desert Arena"] = new ArenaInfo()
                    {
                        Anchor_Entity_Main = "assets/prefabs/misc/xmas/pookie/pookie_deployed.prefab",
                        Anchor_Entity_Secondary = "assets/prefabs/deployable/bbq/bbq.deployed.prefab",
                        FFA_Spawns = new List<Vector3>()
                        {
                            new Vector3(49.304f, 0.772f, -1.824f),
                            new Vector3(50.542f, 0.916f, 5.721f),
                            new Vector3(46.575f, 0.772f, 13.359f),
                            new Vector3(41.424f, 0.772f, 15.872f),
                            new Vector3(39.532f, 0.772f, 19.417f),
                            new Vector3(41.426f, 2.942f, 22.508f),
                            new Vector3(33.339f, 0.772f, 23.984f),
                            new Vector3(29.642f, 3.829f, 28.142f),
                            new Vector3(38.517f, 3.797f, 24.488f),
                            new Vector3(38.988f, 4.58f, 13.726f),
                            new Vector3(36.988f, 3.787f, 4.699f),
                            new Vector3(48.104f, 3.786f, -1.105f),
                            new Vector3(43.489f, 0.774f, -2.951f),
                            new Vector3(36.822f, 0.772f, -0.249f),
                            new Vector3(34.259f, 0.772f, -13.08f),
                            new Vector3(24.626f, 0.932f, -18.043f),
                            new Vector3(14.544f, 0.906f, -26.635f),
                            new Vector3(22.93f, 0.772f, -31.302f),
                            new Vector3(-5.6f, 1.056f, -40.053f),
                            new Vector3(-8.951f, 0.772f, -43.413f),
                            new Vector3(-15.548f, 0.945f, -47.678f),
                            new Vector3(-22.533f, 0.772f, -38.025f),
                            new Vector3(-21.656f, 0.793f, -30.258f),
                            new Vector3(-7.084f, 0.787f, -22.746f),
                            new Vector3(-18.522f, 0.77f, -15.311f),
                            new Vector3(-17.174f, 0.78f, -16.343f),
                            new Vector3(-16.884f, 3.757f, -9.636f),
                            new Vector3(-27.035f, 3.807f, -7.195f),
                            new Vector3(-37.507f, 3.813f, -13.302f),
                            new Vector3(-37.49f, 3.807f, -23.169f),
                            new Vector3(-30.719f, 4.875f, -30.759f),
                            new Vector3(-17.947f, 3.741f, -43.255f),
                            new Vector3(-11.67f, 3.745f, -35.293f),
                            new Vector3(-5.487f, 3.752f, -27.637f),
                            new Vector3(-15.192f, 4.544f, -21.648f),
                            new Vector3(-25.537f, 0.831f, 22.026f),
                            new Vector3(-16.74f, 0.772f, 17.832f),
                            new Vector3(1.791f, 1f, 29.396f),
                            new Vector3(8.047f, 0.903f, 19.793f),
                            new Vector3(10.741f, 3.91f, -17.545f),
                            new Vector3(14.167f, 1.001f, -20.412f),
                            new Vector3(8.444f, 0.942f, -16.891f),
                            new Vector3(2.581f, 1.186f, -4.433f)
                        },
                        TDM_Spawns_1 = new List<Vector3>()
                        {
                            new Vector3(-28.85f, 0.794f, -41.04f),
                            new Vector3(-32.41f, 0.808f, -38.196f),
                            new Vector3(-36.068f, 0.851f, -34.086f),
                            new Vector3(-39.506f, 0.899f, -29.531f)
                        },
                        TDM_Spawns_2 = new List<Vector3>()
                        {
                            new Vector3(46.913f, 0.871f, 21.827f),
                            new Vector3(48.607f, 0.806f, 18.84f),
                            new Vector3(50.524f, 0.858f, 15.336f),
                            new Vector3(51.836f, 0.982f, 8.864f)
                        }
                    }
                };
            }
        }

        #endregion

        #region CUI

        [ChatCommand("sk")]
        void SendSkirmishPanel(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_admin)) return;
            SkirmishPanel(player);
        }

        const string selectedCol = "1 0.8353394 0 1";
        const string defaultCol = "1 1 1 1";

        void SkirmishPanel(BasePlayer player, string selected_arena = "random", string selected_mode = "random", int selected_delay = 0)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.8823529" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-0.002 0.337", OffsetMax = "-0.002 -0.333" }
            }, "Overlay", "SkirmishStartPanel");

            container.Add(new CuiElement
            {
                Name = "SkirmishStartTitle",
                Parent = "SkirmishStartPanel",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UITitle", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-130 190.6", OffsetMax = "130 244.6" }
                }
            });

            var col = isRunning ? "5dff00" : "ff0000";

            container.Add(new CuiElement
            {
                Name = "SkirmishInfoGameRunning",
                Parent = "SkirmishStartTitle",
                Components = {
                    new CuiTextComponent { Text = $"{lang.GetMessage("UIisRunning", this, player.UserIDString)}: <color=#{col}>{isRunning}</color>", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-265 -27", OffsetMax = "-135 0" }
                }
            });
            container.Add(new CuiElement
            {
                Name = "SkirmishStartArenaTitle",
                Parent = "SkirmishStartPanel",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIArena", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-236.241 137.574", OffsetMax = "-163.759 170.226" }
                }
            });
            var offset = 0;
            foreach (var arena in config.arenas)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = "0.4056604 0.4056604 0.4056604 1", Command = $"skupdatemenu {arena.Key} {selected_mode} {selected_delay}" },
                    Text = { Text = arena.Key, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = selected_arena == arena.Key ? selectedCol : defaultCol },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-75 {-50.33 - offset}", OffsetMax = $"75 {-26.33 - offset}" }
                }, "SkirmishStartArenaTitle", "SkirmishStartArenaButton_");
                offset += 34;
            }
            container.Add(new CuiElement
            {
                Name = "SkirmishStartModeTitle",
                Parent = "SkirmishStartPanel",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIMode", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-36.241 137.574", OffsetMax = "36.241 170.226" }
                }
            });
            container.Add(new CuiButton
            {
                Button = { Color = "0.4056604 0.4056604 0.4056604 1", Command = $"skupdatemenu {selected_arena} ffa {selected_delay}" },
                Text = { Text = "FFA", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = selected_mode == "ffa" ? selectedCol : defaultCol },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -50.33", OffsetMax = "32 -26.33" }
            }, "SkirmishStartModeTitle", "SkirmishStartModeFFA");
            container.Add(new CuiButton
            {
                Button = { Color = "0.4056604 0.4056604 0.4056604 1", Command = $"skupdatemenu {selected_arena} tdm {selected_delay}" },
                Text = { Text = "TDM", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = selected_mode == "tdm" ? selectedCol : defaultCol },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -84.33", OffsetMax = "32 -60.33" }
            }, "SkirmishStartModeTitle", "SkirmishStartModeTDM");
            container.Add(new CuiElement
            {
                Name = "SkirmishStartDelayTitle",
                Parent = "SkirmishStartPanel",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIDelayTitle", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "125 137.574", OffsetMax = "275 170.226" }
                }
            });
            var delay = Convert.ToInt32(config.seconds_before_game_starts);
            container.Add(new CuiButton
            {
                Button = { Color = "0.4056604 0.4056604 0.4056604 1", Command = $"skupdatemenu {selected_arena} {selected_mode} {delay}" },
                Text = { Text = "Default", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = selected_delay == delay ? selectedCol : defaultCol },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -50.33", OffsetMax = "32 -26.33" }
            }, "SkirmishStartDelayTitle", "SkirmishStartDelayStartDefault");

            offset = 0;
            for (int i = 0; i < 5; i++)
            {
                delay = Convert.ToInt32((config.seconds_before_game_starts / 5) * i);
                if (delay == config.seconds_before_game_starts || delay < 1) continue;                
                container.Add(new CuiButton
                {
                    Button = { Color = "0.4056604 0.4056604 0.4056604 1", Command = $"skupdatemenu {selected_arena} {selected_mode} {delay}" },
                    Text = { Text = delay.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = selected_delay == delay ? selectedCol : defaultCol },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-32 {-84.33 - offset}", OffsetMax = $"32 {-60.33 - offset}" }
                }, "SkirmishStartDelayTitle", $"SkirmishStartDelayStart_{i}");
                offset += 34;
            }
            container.Add(new CuiButton
            {
                Button = { Color = "0.1299376 0.6320754 0.008944447 1", Command = $"skirmishstart {selected_arena} {selected_mode} {selected_delay}" },
                Text = { Text = lang.GetMessage("UIStart", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-84 -252.4", OffsetMax = "-20 -220.4" }
            }, "SkirmishStartPanel", "SkirmishStartStart");

            container.Add(new CuiButton
            {
                Button = { Color = "0.6313726 0.1213034 0.007843152 1", Command = $"skirmishstop" },
                Text = { Text = lang.GetMessage("UIStop", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "20 -252.4", OffsetMax = "84 -220.4" }
            }, "SkirmishStartPanel", "SkirmishStartStop");

            container.Add(new CuiButton
            {
                Button = { Color = "0.4039216 0.4039216 0.4039216 1", Command = "closeskirmishmenu" },
                Text = { Text = "X", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "188 205.6", OffsetMax = "212 229.6" }
            }, "SkirmishStartPanel", "SkirmishCloseButton");
            CuiHelper.DestroyUi(player, "SkirmishStartPanel");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("closeskirmishmenu")]
        void CloseSkirmishMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "SkirmishStartPanel");
        }

        [ConsoleCommand("skirmishstop")]
        void StopSkirmish(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "SkirmishStartPanel");
            if (isRunning) EndGame();
        }

        [ConsoleCommand("skupdatemenu")]
        void SelectArena(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            var arena = String.Join(" ", arg.Args.Take(arg.Args.Length - 2));
            var delay = Convert.ToInt32(arg.Args.Last());
            var mode = arg.Args.ElementAt(arg.Args.Length - 2);

            SkirmishPanel(player, arena, mode, delay);
        }

        [ConsoleCommand("skirmishstart")]
        void StartFromMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "SkirmishStartPanel");

            var arena = String.Join(" ", arg.Args.Take(arg.Args.Length - 2));
            if (string.IsNullOrEmpty(arena) || arena == "random") arena = config.arenas.Keys.ToList().GetRandom();
            var mode = arg.Args.ElementAt(arg.Args.Length - 2);
            if (string.IsNullOrEmpty(mode) || mode == "random")
            {
                if (UnityEngine.Random.Range(0, 101) < config.ffa_chance) mode = "ffa";
                else mode = "tdm";
            }
            var delay = Convert.ToSingle(arg.Args.Last());
            if (delay < 1) delay = config.seconds_before_game_starts;

            if (config.use_event_helper)
            {
                EventHelper.Call("EMManuallyStarted", this.Name);
            }

            StartRegistration(delay, arena, mode);
        }

        #endregion
    }
}
