// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿// Reference: 0Harmony
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Core;
using System.Collections.Generic;
using System.Linq;
using Network;
using System;
using Oxide.Core.Libraries.Covalence;
using System.Globalization;
using System.IO;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Steamworks.ServerList;
using Rust;
using ProtoBuf;
using Oxide.Core.Libraries;
using CompanionServer.Handlers;
using System.Runtime;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using static ConsoleSystem;
using System.Reflection;
using System.Collections;
using System.Net.Http.Headers;
using Oxide.Game.Rust.Libraries;
using UnityEngine.AI;
using UnityEngine.UIElements;
using Instancing;
using UnityEngine.PlayerLoop;
using Time = UnityEngine.Time;
using Harmony;
using System.Reflection.Emit;

namespace Oxide.Plugins
{
    [Info("Rust Arena", "Billy Joe", "1.0.2")]
    [Description("A combat arena plugin allowing for multiple PvP arenas with players.")]

    public class RustArena : CovalencePlugin
    {
        #region Arena Trigger
        private List<GameObject> _arenaTriggers = new List<GameObject>();
        private class ArenaTrigger : FacepunchBehaviour
        {
            public int arenaId = -1;

            private void OnTriggerEnter(Collider col)
            {
                BasePlayer player = col?.GetComponentInParent<BasePlayer>();
                if (player == null) return;
                if (Instance.CheckPlayerInventory(player))
                {
                    player.ChatMessage("<color=#990000>Your Inventory MUST BE EMPTY to so you use Arena Event</color>!");
                    return;
                }
                if (Instance.FindArenaMap(Instance._save.ArenaData[arenaId].mapName) == -1) { player.ChatMessage("This arena does not have a map setup, contact an admin."); return; }
                if (Instance._save.ArenaData[arenaId].vipArena && !Instance.permission.UserHasPermission(player.UserIDString, "rustarena.vip"))
                {
                    if (string.IsNullOrEmpty(Instance.ArenaInfo[arenaId].arenaOwner))
                    {
                        player.ChatMessage("This is a vip arena, you do not have vip.");
                        return;
                    }
                }
                Instance.TeamSelectUi(player, arenaId);
            }

            private void OnTriggerExit(Collider col)
            {
                BasePlayer player = col?.GetComponentInParent<BasePlayer>();
                if (player == null) return;

                CuiHelper.DestroyUi(player, "TeamSelection");
                Instance.playerData[player.UserIDString].currentUi = "";
            }
        }      
        #endregion

        #region Classes
        public class RustArenaPlayer
        {
            public string currentUi = "";
            public int ActiveMenu;

            public bool arenaText = true;
            public Timer arenaTextTimer = null;

            // Settings Menu
            public int WLPage, DWLPage, ABPage, AUBPage, TWLPage, TDWLPage, SAPage, RRPage = 1;
            public PlayerTeams selectedTeam = PlayerTeams.TeamA;

            // Arena Info
            public int arenaId = -1;
            public PlayerTeams team = PlayerTeams.None;
            public bool canTakeDamage = false;

            // Player Status
            public bool isOut = false;
            public bool isReady = false;

            // Players Stats
            public int RoundDamage = 0;
            public int RoundKills = 0;
            public bool died = false;
            //public Vector3 PlayerOldLocation;
        }

        public class WeaponKit
        {
            public string KitName;
            public List<string> weaponShortName;
            public List<WeaponAmmo> weaponAmmo;
            public List<AttachmentInfo> attachments;
        }

        public class AttachmentInfo
        {
            public int weapon;
            public string attachmentShortName;
        }

        public class WeaponAmmo
        {
            public string ammoShortName;
            public int ammoCount;
        }

        public class ArmorKit
        {
            public ArmorKits Helmet;
            public ArmorKits Chest;
            public ArmorKits Gloves;
            public ArmorKits Pants;
            public ArmorKits Boots;
        }

        public class ArmorKits
        {
            public string armorName;
            public string armorShortName;
            public ArmorType armorType;
        }

        public class ArenaSettings
        {
            public string arenaOwner;
            public bool isPublic = true;
            public bool antiGhost;
            public bool onlyHeadshot;
            public bool friendlyFire;
            public bool allowPickup = true;
            public bool cleanupEntities = true;
            public bool scopes;
            public bool teamWl;
            public bool sidesSwitched;
            public int roundCount = 10;
            public ActiveKit ActiveKit;
            public PlayerTeams roundWinner = PlayerTeams.None;
            public bool roundStarted;
            public bool isStarting;
            public HashSet<GameObject> Objects = new HashSet<GameObject>();
            public HashSet<BaseEntity> Items = new HashSet<BaseEntity>();
            public List<string> ArenaWhitelistedIDs;
            public List<string> ArenaBannedIDs;
            public Dictionary<PlayerTeams, int> Score;
            public Dictionary<PlayerTeams, List<string>> TeamWhitelist;
            public Dictionary<PlayerTeams, List<string>> TeamPlayers;
            public Dictionary<PlayerTeams, string> TeamNames;
        }

        public class ActiveKit
        {
            public WeaponKit WeaponKit;
            public ArmorKit ArmorKit;
            public int WoodWalls;
            public int Medkits;
            public int Syrimges;
            public int Bandages;
        }

        public class Arena
        {
            public int arenaId;
            public string zone = "";
            public bool vipArena = false;
            public string mapName = "";
        }

        public class MapData
        {
            public string mapName = "";
            public string mapImage = string.Empty;
            public Dictionary<PlayerTeams, Vector3> spawns = new Dictionary<PlayerTeams, Vector3>();
        }
        #endregion

        #region Defines
        private static HarmonyInstance _harmony;
        public static RustArena Instance;
        private const string Permcreatearena = "rustarena.create";
        private const string Permdeletearena = "rustarena.delete";
        private const string Permsetspawns = "rustarena.setspawns";
        private const string Permsetenter = "rustarena.setentrance";

        private const string SidebarPanelActiveColor = "0.04841581 0.5112139 0.6037736 0.8";
        private const string SidebarPanelUnactiveColor = "0 0 0 0";
        private const string SettingsToggleActiveColor = "0.1294118 0.3568628 0.214585 0.8";
        private const string SettingsToggleUnactiveColor = "0.3584906 0.1302065 0.1302065 0.8";
        private const string KitActiveColor = "0.1294118 0.3568628 0.214585 0.8";
        private const string KitUnactiveColor = "0.07450981 0.07450981 0.07450981 0.8";

        // Search Boxes
        private Dictionary<string, string> _teamANameInput = new Dictionary<string, string>();
        private Dictionary<string, string> _teamBNameInput = new Dictionary<string, string>();
        public Dictionary<string, RustArenaPlayer> playerData = new Dictionary<string, RustArenaPlayer>();
        public Dictionary<Connection, int> arenaConnections = new Dictionary<Connection, int>();
        public Dictionary<int, ArenaSettings> ArenaInfo = new Dictionary<int, ArenaSettings>();
        public Timer arenaText;
        ArenaSettings arenaSettings;
        #endregion

        #region ENums
        public enum PlayerTeams
        {
            TeamA,
            TeamB,
            Spectator,
            None
        }

        public enum ArmorType
        {
            Headgear,
            Chest,
            Gloves,
            Pants,
            Boots,
            None
        }
        #endregion

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "<color=orange>[Rust Arena]</color> You do not have permission to use this command.",
                ["NoArena"] = "<color=orange>[Rust Arena]</color> You have tried to join a arena that has not been setup yet, contact an admin!",
                ["AlreadyInArena"] = "<color=orange>[Rust Arena]</color> You're already in an arena, please leave your current one first.",
                ["PrivateArena"] = "<color=orange>[Rust Arena]</color> This arena is private and you are not whitelisted, please contact the arena owner.",
                ["BannedFromArena"] = "<color=orange>[Rust Arena]</color> The arena owner has <color=red>banned</color> you from this arena, you can join this arena if you are unbanned or the arena closes.",
                ["JoinedArenaTeam"] = "<color=orange>[Rust Arena]</color> You have joined Arena {0}, and are currently on {1}.",
                ["PlayerJoinedArena"] = "<color=orange>[Rust Arena]</color> {0} has joined the Arena.",
                ["JoinedArenaNoTeam"] = "<color=orange>[Rust Arena]</color> You have joined Arena {0}, although you have not been whitelisted for any teams. Contact the arena owner to be able to join a team.",
                ["NotInArena"] = "<color=orange>[Rust Arena]</color> You're currently not in any arena, please join one before using this command.",
                ["LeftArena"] = "<color=orange>[Rust Arena]</color> {0} has left the arena.",
                ["NewArenaOwner"] = "<color=orange>[Rust Arena]</color> {0} is the new owner of the arena.",
                ["NoPlayersToTransfer"] = "<color=orange>[Rust Arena]</color> There is nobody in the arena to transfer to!",
                ["ReadyState"] = "<color=orange>[Rust Arena]</color> {0} is {1}",
                ["SetEntrance"] = "<color=orange>[Rust Arena]</color> You have set/updated the entrance for Arena {0}",
                ["SetLobbySpawn"] = "<color=orange>[Rust Arena]</color> You have set/updated the lobby spawn.",
                ["SetSpawnPoint"] = "<color=orange>[Rust Arena]</color> You have set/updated the spawn point for {0} in Arena {1}",
                ["CreatedArena"] = "<color=orange>[Rust Arena]</color> You have created Arena {0}, this has been saved to the data file.",
                ["DeletedArena"] = "<color=orange>[Rust Arena]</color> You have deleted Arena {0}, this has been saved to the data file.",
                ["InvalidID"] = "<color=orange>[Rust Arena]</color> There is no Arena {0} in the data file, please try a different ID.",
                ["HealedAllPlayers"] = "<color=orange>[Rust Arena]</color> Arena Owner has healed every player in the arena.",
                ["ClearedAllEntities"] = "<color=orange>[Rust Arena]</color> Arena Owner has cleared all entities in the arena.",
                ["SwitchedSides"] = "<color=orange>[Rust Arena]</color> Arena Owner has switched the teams sides of the arena.",
                ["CannotSwitch"] = "<color=orange>[Rust Arena]</color> You cannot switch sides while there is a round in-progress.",
                ["CanNotReset"] = "<color=orange>[Rust Arena]</color> Cannot reset the arena while there is a round in-progress.",
                ["ResetArena"] = "<color=orange>[Rust Arena]</color> Arena Owner has reset the entire arena.",
                ["AlreadyRoundInProg"] = "<color=orange>[Rust Arena]</color> You can not force start when there is a round in-progress.",
                ["EndedRoundForced"] = "<color=orange>[Rust Arena]</color> Arena Owner has forced stopped the round, no score has been updated.",
                ["ArenaForceStarted"] = "<color=orange>[Rust Arena]</color> Arena Owner has force started the round.",
                ["RoundStartingIn"] = "<color=orange>[Rust Arena]</color> Round Starting in: <color=green>{0} Sec(s)</color>",
                ["RoundStarted"] = "<color=orange>[Rust Arena]</color> <color=green>Round Started!</color>",
                ["RoundOver"] = "<color=orange>[Rust Arena]</color> {0} has won the round, round report available.",
                ["PlrBannedFromArena"] = "<color=orange>[Rust Arena]</color> {0} has been <color=red>banned</color> from the arena by the arena owner.",
                ["UnwhitelistedFromTeam"] = "<color=orange>[Rust Arena]</color> You have been removed as you are/were not on the whitelist for that team.",
                ["ArenaAlreadyExist"] = "<color=orange>[Rust Arena]</color> An arena with that ID already exist, please try again.",
                ["MissingArgs"] = "<color=orange>[Rust Arena]</color> The command you entered is missing aruments, please follow the commands provided on product page.",
                ["HostSwitchedArena"] = "<color=orange>[Rust Arena]</color> The host has switched the arena to {0}.",
                ["WonTheRound"] = "{0} HAS WON THE ROUND",
                ["HostEndedRound"] = "THERE IS NO ROUND WINNER",
                ["NoArenaToSwitch"] = "THERE ARE <color=red>NO ARENAS</color> AVAILABE",
                ["TeamAKilledTeamB"] = "<size=14><color=#3399ff> {0}</color> | <color=#cc0000>{1}</color></size>",
                ["TeamBKilledTeamA"] = "<size=14><color=#cc0000> {0}</color> | <color=#3399ff>{1}</color></size>",
                ["GameOver"] = "<color=orange>[Rust Arena]</color> <color=red>GAME OVER!</color> <color=#3399ff>{0}</color> have won the game, GG!.",
                ["NoPlayersOnOtherTeam"] = "<color=orange>[Rust Arena]</color> There must be a player on the other team for you to be able to start the arena.",
                ["NotWhitelistedForTeam"] = "<color=orange>[Rust Arena]</color> You are not whitelisted to switch to that team.",
                ["Ready"] = "Ready",
                ["NotReady"] = "Not Ready",
                ["ArenaIsPublic"] = "<color=orange>[Rust Arena]</color> Arena Owner has set the arena to public.",
                ["ArenaIsPrivate"] = "<color=orange>[Rust Arena]</color> Arena Owner has set the arena to private.",
                ["ArenaHeadshotOnly"] = "<color=orange>[Rust Arena]</color> Arena Owner has set the arena to headshot only.",
                ["ArenaNotHeadshot"] = "<color=orange>[Rust Arena]</color> Arena Owner has turned off headshot only.",
                ["ArenaIsFF"] = "<color=orange>[Rust Arena]</color> Arena Owner has turned on friendly-fire.",
                ["ArenaIsNotFF"] = "<color=orange>[Rust Arena]</color> Arena Owner has turned off friendly-fire.",
                ["ArenaScopes"] = "<color=orange>[Rust Arena]</color> Arena Owner has given everyone scopes.",
                ["ArenaNoScopes"] = "<color=orange>[Rust Arena]</color> Arena Owner has removed everyones scopes.",
                ["ArenaCleanupEnts"] = "<color=orange>[Rust Arena]</color> Arena Owner has turned on entity cleanup.",
                ["ArenaNoCleanupEnts"] = "<color=orange>[Rust Arena]</color> Arena Owner has turned off entity cleanup.",
                ["ArenaAllowPickup"] = "<color=orange>[Rust Arena]</color> Arena Owner has turned on player pickup.",
                ["ArenaNoPickup"] = "<color=orange>[Rust Arena]</color> Arena Owner has turned off player pickup.",
                ["ArenaTeamWL"] = "<color=orange>[Rust Arena]</color> Arena Owner has turned on team whitelist.",
                ["ArenaNoTeamWL"] = "<color=orange>[Rust Arena]</color> Arena Owner has turned off team whitelist.",
            }, this);
        }

        private string GetMessage(string langKey, string playerId = null, params object[] args) => string.Format(lang.GetMessage(langKey, this, playerId), args);
        private void SendMessage(IPlayer player, string langKey, params object[] args)
        {
            if (player.IsConnected)
                player.Message(GetMessage(langKey, player.Id, args));
        }
        private void SendMessageToArena(int arenaId, string langKey, params object[] args)
        {
            string message = GetMessage(langKey, args: args);
            if (string.IsNullOrEmpty(message) || arenaId == -1) return;

            foreach (BasePlayer player in PlayersInArena(arenaId, true))
            {
                if (player != null)
                    player.ChatMessage(message);
            }
        }
        #endregion

        #region Data
        public class SaveData
        {
            public Vector3 LobbySpawnpoint;
            public Dictionary<int, Arena> ArenaData = new Dictionary<int, Arena>();
            public List<MapData> ArenaMaps = new List<MapData>();
            public Dictionary<int, Vector3> ArenaEntrances = new Dictionary<int, Vector3>();
        }

        private SaveData _save;
        private void SaveArenaData() => Interface.Oxide.DataFileSystem.WriteObject("RustArena", _save);
        #endregion

        #region Config
        public class Skins
        {
            [JsonProperty(PropertyName = "Use Custom Skins")] public bool useCustomSkins;
            [JsonProperty(PropertyName = "Team A Skins")] public TeamOutfitSkins TeamASkins;
            [JsonProperty(PropertyName = "Team B Skins")] public TeamOutfitSkins TeamBSkins;
        }

        public class TeamOutfitSkins
        {
            [JsonProperty(PropertyName = "Metal Facemask Skin")] public ulong MetalFaceSkin;
            [JsonProperty(PropertyName = "Hoodie Skin")] public ulong HoodieSkin;
            [JsonProperty(PropertyName = "Metal Chest Skin")] public ulong MetalChestSkin;
            [JsonProperty(PropertyName = "Kilt Skin")] public ulong KiltSkin;
            [JsonProperty(PropertyName = "Pants Skin")] public ulong PantsSkin;
            [JsonProperty(PropertyName = "Boots Skin")] public ulong BootsSkin;
        }

        public class WeaponSettings
        {
            [JsonProperty(PropertyName = "Default Weapon Kit (0-24)")] public int DefaultWeaponKit;
            [JsonProperty(PropertyName = "Weapon Kits List")] public List<WeaponKit> WeaponKits;
        }

        public class AttireSettings
        {
            [JsonProperty(PropertyName = "Default Headgear Item (0-4)")] public int DefaultHeadger;
            [JsonProperty(PropertyName = "Default Chest Item (5-9)")] public int DefaultChest;
            [JsonProperty(PropertyName = "Default Gloves Item (10-14)")] public int DefaultGloves;
            [JsonProperty(PropertyName = "Default Headgear Item (15-20)")] public int DefaultPants = 15;
            [JsonProperty(PropertyName = "Default Boots Item (20-24)")] public int DefaultBoots = 20;
            [JsonProperty(PropertyName = "Armor Kits (5 Options each type)")] public List<ArmorKits> ArmorKits;
        }

        public class KitSettings
        {
            [JsonProperty(PropertyName = "Weapon Kit Settings")] public WeaponSettings weaponSettings;
            [JsonProperty(PropertyName = "Attire Kit Settings")] public AttireSettings attireSettings;
            [JsonProperty(PropertyName = "Max Syringes")] public int MaxSyringes;
            [JsonProperty(PropertyName = "Max Medkits")] public int MaxMedkits;
            [JsonProperty(PropertyName = "Max Bandages")] public int MaxBandages;
            [JsonProperty(PropertyName = "Max Wood Walls")] public int MaxWoodWalls;
        }

        static Configuration config;
        public class Configuration
        {
            [JsonProperty(PropertyName = "Server Always Day?")] public bool AlwaysDay;
            [JsonProperty(PropertyName = "Skin Settings")] public Skins skinSettings;
            [JsonProperty(PropertyName = "Kit Settings")] public KitSettings kitSettings;

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    AlwaysDay = false,
                    skinSettings = new Skins
                    {
                        useCustomSkins = true,
                        TeamASkins = new TeamOutfitSkins
                        {
                            MetalFaceSkin = 2432948498,
                            MetalChestSkin = 2432947351,
                            HoodieSkin = 2416648557,
                            PantsSkin = 2416647256,
                            KiltSkin = 1826194479,
                            BootsSkin = 2454376365,
                        },
                        TeamBSkins = new TeamOutfitSkins
                        {
                            MetalFaceSkin = 2105454370,
                            MetalChestSkin = 2105505757,
                            HoodieSkin = 2080975449,
                            PantsSkin = 2080977144,
                            KiltSkin = 2120628865,
                            BootsSkin = 2090776132,
                        }
                    },
                    kitSettings = new KitSettings
                    {
                        MaxSyringes = 10,
                        MaxMedkits = 2,
                        MaxBandages = 9,
                        MaxWoodWalls = 10,

                        weaponSettings = new WeaponSettings
                        {
                            //Interger for kit in list (0-24)
                            DefaultWeaponKit = 0,

                            //List of weapons, this is the max there can be. Feel free to change or remove as you wish.
                            WeaponKits = new List<WeaponKit>()
                            {
                                new WeaponKit { KitName = "AK", weaponShortName = new List<string>(){"rifle.ak"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.rifle", ammoCount = 250 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "AK BOLT", weaponShortName = new List<string>(){"rifle.ak","rifle.bolt"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.rifle", ammoCount = 250 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "AK L96", weaponShortName = new List<string>(){"rifle.ak","rifle.l96"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.rifle", ammoCount = 250 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "BOLT", weaponShortName = new List<string>(){"rifle.bolt"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.rifle", ammoCount = 250 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "LR300", weaponShortName = new List<string>(){"rifle.lr300"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.rifle", ammoCount = 250 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "LR BOLT", weaponShortName = new List<string>(){"rifle.lr300", "rifle.bolt"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.rifle", ammoCount = 250 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "LR L96", weaponShortName = new List<string>(){"rifle.lr300", "rifle.l96"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.rifle", ammoCount = 250 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "L96", weaponShortName = new List<string>(){"rifle.l96"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.rifle", ammoCount = 250 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "M249", weaponShortName = new List<string>(){"lmg.m249"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.rifle", ammoCount = 500 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "M39", weaponShortName = new List<string>(){"rifle.m39"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.rifle", ammoCount = 250 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "SEMI", weaponShortName = new List<string>(){"rifle.semiauto"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.rifle", ammoCount = 250 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "MP5", weaponShortName = new List<string>(){"smg.mp5"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.pistol", ammoCount = 250 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "THOMPSON", weaponShortName = new List<string>(){"smg.thompson"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.pistol", ammoCount = 250 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "CUSTOM SMG", weaponShortName = new List<string>(){"smg.2"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.pistol", ammoCount = 250 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "M92", weaponShortName = new List<string>(){"pistol.m92"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.pistol", ammoCount = 250 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "PYTHON", weaponShortName = new List<string>(){"pistol.python"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.pistol", ammoCount = 120 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "P250", weaponShortName = new List<string>(){"pistol.semiauto"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.pistol", ammoCount = 150 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "REVOLVER", weaponShortName = new List<string>(){"pistol.revolver"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.pistol", ammoCount = 120 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "SPAS-12", weaponShortName = new List<string>(){"shotgun.spas12"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.shotgun", ammoCount = 60 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "PUMP", weaponShortName = new List<string>(){"shotgun.pump"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.shotgun", ammoCount = 60 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "PIPE", weaponShortName = new List<string>(){"shotgun.waterpipe"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "ammo.shotgun", ammoCount = 60 } }, attachments = new List<AttachmentInfo>() {} },
                                new WeaponKit { KitName = "COMPOUND BOW", weaponShortName = new List<string>(){"bow.compound"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "arrow.wooden", ammoCount = 60 } }, attachments = new List<AttachmentInfo>() {} },
                                new WeaponKit { KitName = "CROSSBOW", weaponShortName = new List<string>(){"crossbow"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "arrow.wooden", ammoCount = 60 } }, attachments = new List<AttachmentInfo>() { new AttachmentInfo { weapon = 0, attachmentShortName = "weapon.mod.lasersight" } } },
                                new WeaponKit { KitName = "HUNTING BOW", weaponShortName = new List<string>(){"bow.hunting"}, weaponAmmo = new List<WeaponAmmo>() { new WeaponAmmo { ammoShortName = "arrow.wooden", ammoCount = 60 } }, attachments = new List<AttachmentInfo>() {} },
                                new WeaponKit { KitName = "MELEE", weaponShortName = new List<string>(){"longsword","mace","machete","salvaged.cleaver","salvaged.sword","knife.butcher"}, weaponAmmo = new List<WeaponAmmo>(){}, attachments = new List<AttachmentInfo>() {} }
                            },
                        },
                        attireSettings = new AttireSettings
                        {
                            DefaultHeadger = 0,
                            DefaultChest = 5,
                            DefaultGloves = 10,
                            DefaultPants = 15,
                            DefaultBoots = 20,
                            ArmorKits = new List<ArmorKits>()
                            {
                                new ArmorKits { armorName = "METAL FACEMASK", armorShortName = "metal.facemask", armorType = ArmorType.Headgear },
                                new ArmorKits { armorName = "COFFEE CAN", armorShortName = "coffeecan.helmet", armorType = ArmorType.Headgear },
                                new ArmorKits { armorName = "HEAVY HELMET", armorShortName = "heavy.plate.helmet", armorType = ArmorType.Headgear },
                                new ArmorKits { armorName = "WOLF HEADRESS", armorShortName = "hat.wolf", armorType = ArmorType.Headgear },
                                new ArmorKits { armorName = "GLOWING EYES", armorShortName = "gloweyes", armorType = ArmorType.Headgear },
                                new ArmorKits { armorName = "METAL CHEST", armorShortName = "metal.plate.torso", armorType = ArmorType.Chest },
                                new ArmorKits { armorName = "ROADSIGN JACKET", armorShortName = "roadsign.jacket", armorType = ArmorType.Chest },
                                new ArmorKits { armorName = "HEAVY JACKET", armorShortName = "heavy.plate.jacket", armorType = ArmorType.Chest },
                                new ArmorKits { armorName = "WOOD CHEST", armorShortName = "wood.armor.jacket", armorType = ArmorType.Chest },
                                new ArmorKits { armorName = "NO CHEST", armorShortName = "", armorType = ArmorType.Chest },
                                new ArmorKits { armorName = "TACTICAL GLOVES", armorShortName = "tactical.gloves", armorType = ArmorType.Gloves },
                                new ArmorKits { armorName = "ROADSIGN GLOVES", armorShortName = "roadsign.gloves", armorType = ArmorType.Gloves },
                                new ArmorKits { armorName = "LEATHER GLOVES", armorShortName = "burlap.gloves", armorType = ArmorType.Gloves },
                                new ArmorKits { armorName = "BURLAP GLOVES", armorShortName = "burlap.gloves", armorType = ArmorType.Gloves },
                                new ArmorKits { armorName = "NO GLOVES", armorShortName = "", armorType = ArmorType.Gloves },
                                new ArmorKits { armorName = "ROADSIGN KILT", armorShortName = "roadsign.kilt", armorType = ArmorType.Pants },
                                new ArmorKits { armorName = "HEAVY PANTS", armorShortName = "heavy.plate.pants", armorType = ArmorType.Pants },
                                new ArmorKits { armorName = "WOOD PANTS", armorShortName = "wood.armor.pants", armorType = ArmorType.Pants },
                                new ArmorKits { armorName = "HIDE PANTS", armorShortName = "attire.hide.helterneck", armorType = ArmorType.Pants },
                                new ArmorKits { armorName = "NO PANTS", armorShortName = "", armorType = ArmorType.Pants },
                                new ArmorKits { armorName = "BOOTS", armorShortName = "shoes.boots", armorType = ArmorType.Boots },
                                new ArmorKits { armorName = "HIDE BOOTS", armorShortName = "attire.hide.boots", armorType = ArmorType.Boots },
                                new ArmorKits { armorName = "BURLAP SHOES", armorShortName = "burlap.shoes", armorType = ArmorType.Boots },
                                new ArmorKits { armorName = "FROG BOOTS", armorShortName = "boots.frog", armorType = ArmorType.Boots },
                                new ArmorKits { armorName = "NO BOOTS", armorShortName = "", armorType = ArmorType.Boots }
                            },
                        },
                    }
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
                    LoadDefaultConfig();
                    SaveConfig();
                }
            }
            catch
            {
                PrintWarning($"Creating new config file.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Functions
        private string GetArenaInfo(string zone = "")
        {
            StringBuilder formated = new StringBuilder();

            int playerCount = 0;
            for (int i = 0; i < _save.ArenaData.Count; i++)
            {
                arenaSettings = ArenaInfo[i];
                if (arenaSettings == null || _save.ArenaData[i].zone != zone) continue;

                playerCount = GetPlayerCount(i);
                formated.Append(string.Format("<b><size=12><color=#ffffff>Arena {0}</color> - {1}</size></b>", i, playerCount == 0 ? "<color=#ff5a5a>EMPTY</color>" : $"<color=#2eff96>{playerCount} Player(s)</color>"));

                if (i < _save.ArenaData.Count)
                    formated.Append("\n");
            }

            return formated.ToString();
        }

        private int GetPlayerCount(int arenaId)
        {
            if (ArenaInfo.TryGetValue(arenaId, out arenaSettings))
                return arenaSettings.TeamPlayers[PlayerTeams.TeamA].Count + arenaSettings.TeamPlayers[PlayerTeams.TeamB].Count;

            return 0;
        }

        private string GetKitName(BasePlayer player)
        {
            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo == null) return string.Empty;

            if (ArenaInfo.TryGetValue(pInfo.arenaId, out arenaSettings))
            {
                if (string.IsNullOrEmpty(arenaSettings.ActiveKit.WeaponKit.KitName)) return string.Empty;
                return arenaSettings.ActiveKit.WeaponKit.KitName;
            }

            return string.Empty;
        }

        public List<Connection> GetArenaPlayers(BasePlayer player)
        {
            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo.arenaId == -1) return null;

            return arenaConnections.Where(pair => pair.Value == pInfo.arenaId).Select(pair => pair.Key).ToList();
        }

        private bool inSameArena(BasePlayer player, BasePlayer otherPlayer)
        {
            if (otherPlayer == null) return false;
            return playerData[player.UserIDString].arenaId == playerData[otherPlayer.UserIDString].arenaId;
        }

        private bool itemInArena(BasePlayer player, DroppedItem item)
        {
            RustArenaPlayer pInfo;
            if (playerData.TryGetValue(player.UserIDString, out pInfo))
            {
                if (pInfo.arenaId == -1 || !ArenaInfo.ContainsKey(pInfo.arenaId)) return false;
                return ArenaInfo[pInfo.arenaId].Items.Contains(item);
            }
            return false;
        }

        public bool inArena(BasePlayer player)
        {
            if (player == null || !playerData.ContainsKey(player.UserIDString)) return false;
            return playerData[player.UserIDString].arenaId != -1;
        }

        private bool IsArenaOwner(BasePlayer player)
        {
            RustArenaPlayer pInfo;
            if (playerData.TryGetValue(player.UserIDString, out pInfo))
            {
                if (!ArenaInfo.ContainsKey(pInfo.arenaId)) return false;
                return ArenaInfo[pInfo.arenaId].arenaOwner == player.UserIDString;
            }

            return false;
        }

        private void ClearPlayerStats(BasePlayer player, bool isLeaving = false, bool roundCleanup = false)
        {
            RustArenaPlayer pInfo;
            if (!playerData.TryGetValue(player.UserIDString, out pInfo)) return;

            pInfo.isOut = false;
            pInfo.isReady = false;
            pInfo.canTakeDamage = false;

            int arenaId = pInfo.arenaId;
            PlayerTeams team = pInfo.team;
            if (isLeaving)
            {
                pInfo.arenaId = -1;
                pInfo.team = PlayerTeams.None;
            }

            UpdateScoreboard(arenaId, team);
            if (roundCleanup)
            {
                pInfo.RoundDamage = 0;
                pInfo.RoundKills = 0;
                pInfo.died = false;
            }
        }

        private List<BasePlayer> PlayersInArena(int arenaId, bool everyone = false)
        {
            List<BasePlayer> playerList = new List<BasePlayer>();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (!playerData.ContainsKey(player.UserIDString) || playerData[player.UserIDString].arenaId != arenaId) continue;

                if (everyone)
                    playerList.Add(player);
                else
                {
                    if (playerData[player.UserIDString].team == PlayerTeams.Spectator) continue;
                    playerList.Add(player);
                }
            }

            return playerList;
        }

        private void ReadyCheck(int arenaId)
        {
            if (!ArenaInfo.TryGetValue(arenaId, out arenaSettings)) return;
            if (arenaSettings.TeamPlayers[PlayerTeams.TeamA].Count == 0 || arenaSettings.TeamPlayers[PlayerTeams.TeamB].Count == 0) return;

            int peopleReady = 0;
            List<BasePlayer> arenaPlayers = Pool.GetList<BasePlayer>();
            arenaPlayers.AddRange(PlayersInArena(arenaId));

            if (arenaPlayers.Count == 0)
            {
                Pool.FreeList(ref arenaPlayers);
                return;
            }

            foreach (BasePlayer player in arenaPlayers)
            {
                if (!playerData.ContainsKey(player.UserIDString)) continue;
                if (playerData[player.UserIDString].isReady)
                    peopleReady += 1;
            }

            if (peopleReady == arenaPlayers.Count)
            {
                if (arenaSettings.isStarting) return;
                ArenaInfo[arenaId].isStarting = true;
                StartRound(arenaId);
            }

            Pool.FreeList(ref arenaPlayers);
        }

        private void ClearArenaUIs(BasePlayer player, bool hud = true)
        {
            if (hud)
            {
                CuiHelper.DestroyUi(player, "ArenaHUDTop");
                CuiHelper.DestroyUi(player, "ArenaHUDSidebar");
                CuiHelper.DestroyUi(player, "NotReadyBack");
                CuiHelper.DestroyUi(player, "TeamSelection");
                CuiHelper.DestroyUi(player, "GameoverPanel");
                CuiHelper.DestroyUi(player, "TeamAScoreboardTop");
                CuiHelper.DestroyUi(player, "TeamAScoreboardBottom");
                CuiHelper.DestroyUi(player, "TeamBScoreboardTop");
                CuiHelper.DestroyUi(player, "TeamBScoreboardBottom");
            }

            CuiHelper.DestroyUi(player, "TransferOwnership");
            CuiHelper.DestroyUi(player, "TransferPlayersPanel");
            CuiHelper.DestroyUi(player, "ArenaSettings");
            CuiHelper.DestroyUi(player, "SidebarPanel");
            CuiHelper.DestroyUi(player, "SettingsPanel");
            CuiHelper.DestroyUi(player, "GeneralSettings");
            CuiHelper.DestroyUi(player, "WeaponsPanel");
            CuiHelper.DestroyUi(player, "ArmorPanel");
            CuiHelper.DestroyUi(player, "MiscItemsPanel");
            CuiHelper.DestroyUi(player, "WhitelistPanel");
            CuiHelper.DestroyUi(player, "DeWhitelistPanel");
            CuiHelper.DestroyUi(player, "ArenaBanPanel");
            CuiHelper.DestroyUi(player, "ArenaUnbanPanel");
            CuiHelper.DestroyUi(player, "TeamWhitelistPanel");
            CuiHelper.DestroyUi(player, "TeamDeWhitelistPanel");
            CuiHelper.DestroyUi(player, "SwitchMapPanel");
        }

        private void StartRound(int arenaId)
        {
            if (!ArenaInfo.TryGetValue(arenaId, out arenaSettings)) return;
            if (arenaSettings.Score[PlayerTeams.TeamA] >= arenaSettings.roundCount || arenaSettings.Score[PlayerTeams.TeamB] >= arenaSettings.roundCount)
            {
                foreach (BasePlayer player in PlayersInArena(arenaId))
                    ClearPlayerStats(player, false, true);

                ArenaInfo[arenaId].Score[PlayerTeams.TeamA] = 0;
                ArenaInfo[arenaId].Score[PlayerTeams.TeamB] = 0;
                UpdateScoreboard(arenaId, PlayerTeams.None);
            }

            foreach (BasePlayer player in PlayersInArena(arenaId))
            {
                if (player == null) continue;

                RustArenaPlayer pInfo = playerData[player.UserIDString];
                if (pInfo == null) continue;
                ClearArenaUIs(player);
            }

            int time = 4;
            timer.Repeat(1, 4, () =>
            {
                List<BasePlayer> playersInArena = PlayersInArena(arenaId, true);
                time = time - 1;
                foreach (BasePlayer player in playersInArena)
                    CountdownUi(player, time);

                if (time == 0)
                {
                    ArenaInfo[arenaId].roundWinner = PlayerTeams.None;
                    ArenaInfo[arenaId].roundStarted = true;
                    ArenaInfo[arenaId].isStarting = false;

                    foreach (BasePlayer player in playersInArena)
                    {
                        RustArenaPlayer pInfo = playerData[player.UserIDString];
                        if (pInfo == null) continue;

                        HealPlayer(player);
                        ClearPlayerStats(player, false, true);
                        pInfo.canTakeDamage = true;
                    }

                    UpdateScoreboard(arenaId, PlayerTeams.None);
                }
            });
        }

        private void EndRound(int arenaId, bool forced = false)
        {
            if (!ArenaInfo.TryGetValue(arenaId, out arenaSettings)) return;
            if (!arenaSettings.roundStarted || arenaSettings.isStarting) return;

            if (forced)
                SendMessageToArena(arenaId, "EndedRoundForced");
            else
            {
                if (arenaSettings.roundWinner != PlayerTeams.None)
                {
                    ArenaInfo[arenaId].Score[arenaSettings.roundWinner]++;
                    SendMessageToArena(arenaId, "RoundOver", ArenaInfo[arenaId].TeamNames[arenaSettings.roundWinner].ToUpper());
                }
            }

            ArenaInfo[arenaId].roundStarted = false;
            if (arenaSettings.cleanupEntities)
            {
                if (arenaSettings.Objects.Count > 0)
                {
                    foreach (GameObject obj in arenaSettings.Objects)
                    {
                        if (obj == null || !obj.GetComponent<BaseEntity>()) continue;
                        obj.GetComponent<BaseEntity>().Kill();
                    }

                    ArenaInfo[arenaId].Objects.Clear();
                }

                if (arenaSettings.Items.Count > 0)
                {
                    foreach (BaseEntity ent in arenaSettings.Items)
                    {
                        if (ent == null || ent.IsDestroyed) continue;
                        ent.Kill();
                    }

                    ArenaInfo[arenaId].Items.Clear();
                }
            }

            List<BasePlayer> playersInArena = Pool.GetList<BasePlayer>();
            playersInArena.AddRange(PlayersInArena(arenaId));

            foreach (BasePlayer player in playersInArena)
            {
                RustArenaPlayer pInfo = playerData[player.UserIDString];
                if (pInfo == null) continue;

                player.markAttackerHostile = false;
                player.MarkHostileFor(0);
                ClearArenaUIs(player);
                ClearPlayerStats(player);
                TeleportPlayer(player, GetSpawnpoint(arenaId, pInfo.team));
                HealPlayer(player);
                ArenaHudUi(player);
                SetupKit(player);
            }

            if (arenaSettings.Score[PlayerTeams.TeamA] == arenaSettings.roundCount)
                GameOver(arenaId, PlayerTeams.TeamA);
            else if (arenaSettings.Score[PlayerTeams.TeamB] == arenaSettings.roundCount)
                GameOver(arenaId, PlayerTeams.TeamB);
            else
                foreach (BasePlayer player in playersInArena)
                    ScoreboardUI(player);

            Pool.FreeList(ref playersInArena);
        }

        private bool RestartCheck(int arenaId)
        {
            int teamA = 0, teamB = 0;

            if (!ArenaInfo.TryGetValue(arenaId, out arenaSettings)) return true;

            if (arenaSettings.TeamPlayers[PlayerTeams.TeamA].Count == 0 && arenaSettings.TeamPlayers[PlayerTeams.TeamB].Count == 0) { ResetGame(null, arenaId, true); return false; }
            foreach (BasePlayer player in PlayersInArena(arenaId))
            {
                if (player == null) continue;
                RustArenaPlayer pInfo = playerData[player.UserIDString];
                if (pInfo == null) continue;

                if (pInfo.isOut && pInfo.team == PlayerTeams.TeamA)
                    teamA++;
                else if (pInfo.isOut && pInfo.team == PlayerTeams.TeamB)
                    teamB++;
            }

            if (teamA >= arenaSettings.TeamPlayers[PlayerTeams.TeamA].Count) { ArenaInfo[arenaId].roundWinner = PlayerTeams.TeamB; return true; } else if (teamB >= arenaSettings.TeamPlayers[PlayerTeams.TeamB].Count) { ArenaInfo[arenaId].roundWinner = PlayerTeams.TeamA; return true; }
            return false;
        }

        private void ResetGame(BasePlayer player, int arenaId, bool forced = false)
        {
            if (!ArenaInfo.TryGetValue(arenaId, out arenaSettings)) return;
            if (arenaSettings.roundStarted && !forced)
            {
                player.ChatMessage(GetMessage("CanNotReset"));
                return;
            }

            foreach (GameObject go in arenaSettings.Objects)
            {
                if (go == null) continue;
                go.GetComponent<BaseEntity>()?.Kill();
            }

            var teamPlayers = arenaSettings.TeamPlayers;
            ArenaInfo[arenaId] = GetDefaultSettings();
            if (!forced)
            {
                ArenaInfo[arenaId].arenaOwner = player.UserIDString;
                ArenaInfo[arenaId].TeamPlayers = teamPlayers;
                SendMessageToArena(arenaId, "ResetArena");
            }
        }

        private void SwitchSides(BasePlayer player, int arenaId)
        {
            if (ArenaInfo[arenaId].roundStarted)
            {
                player.ChatMessage(GetMessage("CannotSwitch"));
                return;
            }

            ArenaInfo[arenaId].sidesSwitched = !ArenaInfo[arenaId].sidesSwitched;
            foreach (BasePlayer plr in PlayersInArena(arenaId))
            {
                TeleportPlayer(plr, GetSpawnpoint(arenaId, playerData[plr.UserIDString].team));
                HealPlayer(plr);
            }

            SendMessageToArena(arenaId, "SwitchedSides");
        }

        private void GameOver(int arenaId, PlayerTeams winningTeam)
        {
            SendMessageToArena(arenaId, "GameOver", ArenaInfo[arenaId].TeamNames[winningTeam]);

            foreach (BasePlayer player in PlayersInArena(arenaId))
            {
                ArenaHudUi(player);
                ScoreboardUI(player);
                GameOverUI(player);
            }
        }

        public void ForceStartArena(BasePlayer player, int arenaId)
        {
            if (!ArenaInfo.TryGetValue(arenaId, out arenaSettings)) return;
            if (arenaSettings.TeamPlayers[PlayerTeams.TeamA].Count == 0 || arenaSettings.TeamPlayers[PlayerTeams.TeamB].Count == 0)
            {
                player.ChatMessage(GetMessage("NoPlayersOnOtherTeam"));
                return;
            }

            if (arenaSettings.roundStarted)
            {
                player.ChatMessage(GetMessage("AlreadyRoundInProg"));
                return;
            }

            if (arenaSettings.isStarting)
                return;

            foreach (BasePlayer plr in PlayersInArena(arenaId))
            {
                RustArenaPlayer plrInfo = playerData[plr.UserIDString];
                if (plrInfo == null) return;

                plrInfo.isReady = true;
                ReadyUi(plr);
                ClearArenaUIs(player);
            }

            UpdateScoreboard(arenaId, PlayerTeams.None);
            ReadyCheck(arenaId);
            SendMessageToArena(arenaId, "ArenaForceStarted");
        }

        private void TransferOwner(BasePlayer player, int arenaId, string playerid = "")
        {
            if (ArenaInfo[arenaId].arenaOwner != player.UserIDString || string.IsNullOrEmpty(playerid)) return;

            BasePlayer newOwner = BasePlayer.Find(playerid);
            if (newOwner == null)
            {
                LoadTransferPlayers(player);
                return;
            }

            RustArenaPlayer pInfo = playerData[newOwner.UserIDString];
            if (pInfo == null)
            {
                LoadTransferPlayers(player);
                return;
            }

            if (pInfo.arenaId != arenaId)
            {
                player.ChatMessage("That player is no longer in the arena.");
                LoadTransferPlayers(player);
                return;
            }

            ArenaInfo[arenaId].arenaOwner = newOwner.UserIDString;
            SendMessageToArena(arenaId, "NewArenaOwner", newOwner.displayName);
            CuiHelper.DestroyUi(player, "TransferOwnership");
            CuiHelper.DestroyUi(player, "TransferPlayersPanel");

            foreach (BasePlayer plyr in PlayersInArena(arenaId, true))
            {
                if (plyr == null) continue;
                ArenaHudUi(plyr);
            }
        }

        private void HealPlayer(BasePlayer player)
        {
            if (player == null) return;
            if (player.IsWounded())
                player.StopWounded();

            player.metabolism.hydration.value = 250;
            player.metabolism.calories.value = 500;
            player.metabolism.bleeding.value = 0;
            player.InitializeHealth(100, 100);
        }

        private void CreateEntrance(Vector3 pos, int arenaId)
        {
            GameObject entrance = new GameObject();
            entrance.transform.position = pos;
            entrance.transform.rotation = Quaternion.Euler(0f, 1f, 0f);

            Rigidbody rigidBody = entrance.AddComponent<Rigidbody>();
            rigidBody.isKinematic = true;
            rigidBody.useGravity = false;
            rigidBody.detectCollisions = true;
            rigidBody.collisionDetectionMode = CollisionDetectionMode.Discrete;

            ArenaTrigger trigger = entrance.AddComponent<ArenaTrigger>();
            trigger.arenaId = arenaId;

            BoxCollider boxcol = entrance.AddComponent<BoxCollider>();
            boxcol.isTrigger = true;
            boxcol.size = new Vector3(3f, 3f, 1f);

            entrance.gameObject.layer = (int)Rust.Layer.Reserved1;
            _arenaTriggers.Add(entrance);
        }

        private ArenaSettings GetDefaultSettings()
        {
            return new ArenaSettings
            {
                allowPickup = true,
                antiGhost = false,
                arenaOwner = string.Empty,
                isPublic = true,
                friendlyFire = false,
                onlyHeadshot = false,
                roundStarted = false,
                isStarting = false,
                cleanupEntities = true,
                teamWl = false,
                scopes = false,
                sidesSwitched = false,
                roundCount = 10,
                roundWinner = PlayerTeams.None,
                Score = new Dictionary<PlayerTeams, int>
                {
                    { PlayerTeams.TeamA, 0 },
                    { PlayerTeams.TeamB, 0 }
                },
                TeamPlayers = new Dictionary<PlayerTeams, List<string>>
                {
                    { PlayerTeams.TeamA, new List<string>() },
                    { PlayerTeams.TeamB, new List<string>() },
                    { PlayerTeams.Spectator, new List<string>() }
                },
                TeamWhitelist = new Dictionary<PlayerTeams, List<string>>
                {
                    { PlayerTeams.TeamA, new List<string>() },
                    { PlayerTeams.TeamB, new List<string>() }
                },
                ArenaBannedIDs = new List<string>() { },
                ArenaWhitelistedIDs = new List<string>() { },
                Objects = new HashSet<GameObject>(),
                Items = new HashSet<BaseEntity>(),
                TeamNames = new Dictionary<PlayerTeams, string>()
                {
                    { PlayerTeams.TeamA, "Team A" },
                    { PlayerTeams.TeamB, "Team B" }
                },

                ActiveKit = new ActiveKit
                {
                    WeaponKit = config.kitSettings.weaponSettings.WeaponKits[0],
                    ArmorKit = new ArmorKit
                    {
                        Helmet = config.kitSettings.attireSettings.ArmorKits[config.kitSettings.attireSettings.DefaultHeadger],
                        Chest = config.kitSettings.attireSettings.ArmorKits[config.kitSettings.attireSettings.DefaultChest],
                        Gloves = config.kitSettings.attireSettings.ArmorKits[config.kitSettings.attireSettings.DefaultGloves],
                        Pants = config.kitSettings.attireSettings.ArmorKits[config.kitSettings.attireSettings.DefaultPants],
                        Boots = config.kitSettings.attireSettings.ArmorKits[config.kitSettings.attireSettings.DefaultBoots]
                    },

                    WoodWalls = config.kitSettings.MaxWoodWalls,
                    Medkits = config.kitSettings.MaxMedkits,
                    Syrimges = config.kitSettings.MaxSyringes,
                    Bandages = config.kitSettings.MaxBandages
                }
            };
        }

        private void GenerateArenaData(int arenaId) => ArenaInfo.Add(arenaId, GetDefaultSettings());

        private PlayerTeams GetOtherTeam(PlayerTeams primaryTeam)
        {
            if (primaryTeam == PlayerTeams.TeamA)
                return PlayerTeams.TeamB;
            else
                return PlayerTeams.TeamA;
        }

        private Vector3 GetSpawnpoint(int arenaId, PlayerTeams team)
        {
            if (!(ArenaInfo.ContainsKey(arenaId))) return default(Vector3);
            int index = FindArenaMap(_save.ArenaData[arenaId].mapName);
            if (index == -1) return default(Vector3);

            if (team == PlayerTeams.Spectator) return _save.ArenaMaps[index].spawns[PlayerTeams.Spectator];
            if (ArenaInfo[arenaId].sidesSwitched) return _save.ArenaMaps[index].spawns[GetOtherTeam(team)];
            return _save.ArenaMaps[index].spawns[team];
        }

        private string GetActiveKit(BasePlayer player, ArmorType type, int index)
        {
            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo == null) return KitUnactiveColor;

            ActiveKit kit = ArenaInfo[pInfo.arenaId].ActiveKit;

            string color = KitUnactiveColor;
            switch (type)
            {
                case ArmorType.None:
                    if (config.kitSettings.weaponSettings.WeaponKits[index].KitName == kit.WeaponKit.KitName)
                        color = KitActiveColor;
                    break;
                case ArmorType.Headgear:
                    if (config.kitSettings.attireSettings.ArmorKits[index].armorName == kit.ArmorKit.Helmet.armorName)
                        color = KitActiveColor;
                    break;
                case ArmorType.Chest:
                    if (config.kitSettings.attireSettings.ArmorKits[index].armorName == kit.ArmorKit.Chest.armorName)
                        color = KitActiveColor;
                    break;
                case ArmorType.Gloves:
                    if (config.kitSettings.attireSettings.ArmorKits[index].armorName == kit.ArmorKit.Gloves.armorName)
                        color = KitActiveColor;
                    break;
                case ArmorType.Pants:
                    if (config.kitSettings.attireSettings.ArmorKits[index].armorName == kit.ArmorKit.Pants.armorName)
                        color = KitActiveColor;
                    break;
                case ArmorType.Boots:
                    if (config.kitSettings.attireSettings.ArmorKits[index].armorName == kit.ArmorKit.Boots.armorName)
                        color = KitActiveColor;
                    break;
            }

            return color;
        }

        private void FindAndRemoveItem(BasePlayer player, int arenaId)
        {
            if (!ArenaInfo.TryGetValue(arenaId, out arenaSettings)) return;

            Item[] items = player.inventory.AllItems();
            if (!arenaSettings.scopes)
                foreach (Item item in items.Where(x => x.info.category == ItemCategory.Weapon))
                {
                    if (item.info.shortname == "weapon.mod.small.scope" || item.info.shortname == "weapon.mod.holosight" || item.info.shortname == "weapon.mod.simplesight")
                        item.RemoveFromContainer();
                }

            int medsyringe = 0, medkit = 0, bandage = 0, woodwall = 0;
            bool medSyrEdited = false, bandageEdited = false, woodWallEdited = false;
            foreach (Item item in items)
            {
                switch (item.info.shortname)
                {
                    case "syringe.medical":
                        if (arenaSettings.ActiveKit.Syrimges == 0) { item.Remove(); continue; };
                        medsyringe += item.amount;
                        if (medsyringe <= arenaSettings.ActiveKit.Syrimges) continue;
                        if (medsyringe > arenaSettings.ActiveKit.Syrimges && !medSyrEdited && (medsyringe - arenaSettings.ActiveKit.Syrimges) % 2 != 0) { item.amount = 1; medSyrEdited = true; }
                        else item.Remove();
                        break;

                    case "largemedkit":
                        if (arenaSettings.ActiveKit.Medkits == 0) { item.Remove(); continue; };
                        medkit += item.amount;
                        if (medkit <= arenaSettings.ActiveKit.Medkits) continue;
                        item.Remove();
                        break;

                    case "bandage":
                        if (arenaSettings.ActiveKit.Bandages == 0) { item.Remove(); continue; };
                        bandage += item.amount;
                        if (bandage <= arenaSettings.ActiveKit.Bandages) continue;
                        if (bandage > arenaSettings.ActiveKit.Bandages && !bandageEdited)
                        {
                            int amount = bandage - arenaSettings.ActiveKit.Bandages;
                            item.amount -= amount;
                            bandageEdited = true;
                        }
                        else item.Remove();
                        break;

                    case "wall.external.high":
                        if (arenaSettings.ActiveKit.WoodWalls == 0) { item.Remove(); continue; };
                        woodwall += item.amount;
                        if (woodwall <= arenaSettings.ActiveKit.WoodWalls) continue;
                        if (woodwall > arenaSettings.ActiveKit.WoodWalls && !woodWallEdited)
                        {
                            int amount = woodwall - arenaSettings.ActiveKit.WoodWalls;
                            item.amount -= amount;
                            woodWallEdited = true;
                        }
                        else item.Remove();
                        break;
                }
            }

            if (woodwall < arenaSettings.ActiveKit.WoodWalls)
            {
                int amount = arenaSettings.ActiveKit.WoodWalls - woodwall;
                for (int i = 0; i < amount; i++)
                {
                    Item woodWallItem = ItemManager.CreateByName("wall.external.high", 1);
                    if (woodWallItem == null) continue;
                    if (!woodWallItem.MoveToContainer(player.inventory.containerMain)) woodWallItem.Remove();
                }
            }

            if (medsyringe < arenaSettings.ActiveKit.Syrimges)
            {
                int amount = arenaSettings.ActiveKit.Syrimges - medsyringe;
                for (int i = 0; i < amount; i++)
                {
                    Item syringeItem = ItemManager.CreateByName("syringe.medical", 1);
                    if (syringeItem == null) continue;
                    if (!syringeItem.MoveToContainer(player.inventory.containerMain)) syringeItem.Remove();
                }
            }

            if (medkit < arenaSettings.ActiveKit.Medkits)
            {
                int amount = arenaSettings.ActiveKit.Medkits - medkit;
                for (int i = 0; i < amount; i++)
                {
                    Item medkitItem = ItemManager.CreateByName("largemedkit", 1);
                    if (medkitItem == null) continue;
                    if (!medkitItem.MoveToContainer(player.inventory.containerMain)) medkitItem.Remove();
                }
            }

            if (bandage < arenaSettings.ActiveKit.Bandages)
            {
                int amount = arenaSettings.ActiveKit.Bandages - bandage;
                for (int i = 0; i < amount; i++)
                {
                    Item bandageItem = ItemManager.CreateByName("bandage", 1);
                    if (bandageItem == null) continue;
                    if (!bandageItem.MoveToContainer(player.inventory.containerMain)) bandageItem.Remove();
                }
            }
        }

        private void SetupKit(BasePlayer player, bool spectator = false)
        {
            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo == null) return;

            ArenaSettings arenaInfo = ArenaInfo[pInfo.arenaId];
            if (arenaInfo == null) return;

            ActiveKit kit = arenaInfo.ActiveKit;
            if (kit == null) return;

            CuiHelper.DestroyUi(player, "SkinBox_UI");
            CuiHelper.DestroyUi(player, "SkinBox_Popup");
            player.EndLooting();

            player.inventory.Strip();
            if (spectator || pInfo.isOut)
            {
                Item binoculars = ItemManager.CreateByName("tool.binoculars", 1);
                if (binoculars != null)
                    if (!binoculars.MoveToContainer(player.inventory.containerBelt, 0))
                        binoculars.Remove();

                Item hazmat = ItemManager.CreateByName("hazmatsuit", 1);
                if (hazmat != null)
                    if (!hazmat.MoveToContainer(player.inventory.containerWear, 0))
                        hazmat.Remove();

                return;
            }

            string kitName = GetKitName(player);
            if (string.IsNullOrEmpty(kitName)) return;

            #region Attire
            Item Headgear = null; Item Hoodie = null; Item Chest = null; Item Pants = null; Item Legs = null; Item Boots = null; Item Gloves = null;

            if (config.skinSettings.useCustomSkins)
            {
                if (kit.ArmorKit.Helmet.armorShortName == "metal.facemask")
                {
                    if (pInfo.team == PlayerTeams.TeamA)
                        Headgear = ItemManager.CreateByName(kit.ArmorKit.Helmet.armorShortName, 1, config.skinSettings.TeamASkins.MetalFaceSkin);
                    else if (pInfo.team == PlayerTeams.TeamB)
                        Headgear = ItemManager.CreateByName(kit.ArmorKit.Helmet.armorShortName, 1, config.skinSettings.TeamBSkins.MetalFaceSkin);
                }
                else
                {
                    if (kit.ArmorKit.Helmet.armorShortName != "")
                        Headgear = ItemManager.CreateByName(kit.ArmorKit.Helmet.armorShortName, 1);
                }

                if (kit.ArmorKit.Chest.armorShortName == "metal.plate.torso")
                {
                    if (pInfo.team == PlayerTeams.TeamA)
                        Chest = ItemManager.CreateByName(kit.ArmorKit.Chest.armorShortName, 1, config.skinSettings.TeamASkins.MetalChestSkin);
                    else if (pInfo.team == PlayerTeams.TeamB)
                        Chest = ItemManager.CreateByName(kit.ArmorKit.Chest.armorShortName, 1, config.skinSettings.TeamBSkins.MetalChestSkin);
                }
                else
                {
                    if (kit.ArmorKit.Chest.armorShortName != "")
                        Chest = ItemManager.CreateByName(kit.ArmorKit.Chest.armorShortName, 1);
                }

                if (kit.ArmorKit.Pants.armorShortName == "roadsign.kilt")
                {
                    if (pInfo.team == PlayerTeams.TeamA)
                        Legs = ItemManager.CreateByName(kit.ArmorKit.Pants.armorShortName, 1, config.skinSettings.TeamASkins.KiltSkin);
                    else if (pInfo.team == PlayerTeams.TeamB)
                        Legs = ItemManager.CreateByName(kit.ArmorKit.Pants.armorShortName, 1, config.skinSettings.TeamBSkins.KiltSkin);
                }
                else
                {
                    if (kit.ArmorKit.Pants.armorShortName != "")
                        Legs = ItemManager.CreateByName(kit.ArmorKit.Pants.armorShortName, 1);
                }


                if (kit.ArmorKit.Boots.armorShortName == "shoes.boots")
                {
                    if (pInfo.team == PlayerTeams.TeamA)
                        Boots = ItemManager.CreateByName(kit.ArmorKit.Boots.armorShortName, 1, config.skinSettings.TeamASkins.BootsSkin);
                    else if (pInfo.team == PlayerTeams.TeamB)
                        Boots = ItemManager.CreateByName(kit.ArmorKit.Boots.armorShortName, 1, config.skinSettings.TeamBSkins.BootsSkin);
                }
                else
                {
                    if (kit.ArmorKit.Boots.armorShortName != "")
                        Boots = ItemManager.CreateByName(kit.ArmorKit.Boots.armorShortName, 1);
                }
            }
            else
            {
                if (kit.ArmorKit.Helmet.armorShortName == "")
                    Headgear = ItemManager.CreateByName(kit.ArmorKit.Helmet.armorShortName, 1);

                if (kit.ArmorKit.Chest.armorShortName == "")
                    Chest = ItemManager.CreateByName(kit.ArmorKit.Chest.armorShortName, 1);

                if (kit.ArmorKit.Pants.armorShortName == "")
                    Legs = ItemManager.CreateByName(kit.ArmorKit.Pants.armorShortName, 1);

                if (kit.ArmorKit.Boots.armorShortName == "")
                    Boots = ItemManager.CreateByName(kit.ArmorKit.Boots.armorShortName, 1);
            }

            if (pInfo.team == PlayerTeams.TeamA)
                Hoodie = ItemManager.CreateByName("hoodie", 1, config.skinSettings.TeamASkins.HoodieSkin);
            else if (pInfo.team == PlayerTeams.TeamB)
                Hoodie = ItemManager.CreateByName("hoodie", 1, config.skinSettings.TeamBSkins.HoodieSkin);

            if (pInfo.team == PlayerTeams.TeamA)
                Pants = ItemManager.CreateByName("pants", 1, config.skinSettings.TeamASkins.PantsSkin);
            else if (pInfo.team == PlayerTeams.TeamB)
                Pants = ItemManager.CreateByName("pants", 1, config.skinSettings.TeamBSkins.PantsSkin);


            if (kit.ArmorKit.Gloves.armorShortName != "")
            {
                Gloves = ItemManager.CreateByName(kit.ArmorKit.Gloves.armorShortName, 1);
                if (Gloves != null)
                    if (!Gloves.MoveToContainer(player.inventory.containerWear, 6))
                        Gloves.Remove();
            };

            if (Headgear != null)
                if (!Headgear.MoveToContainer(player.inventory.containerWear, 0))
                    Headgear.Remove();

            if (Chest != null)
                if (!Chest.MoveToContainer(player.inventory.containerWear, 2))
                    Chest.Remove();

            if (Hoodie != null)
                if (!Hoodie.MoveToContainer(player.inventory.containerWear, 1))
                    Hoodie.Remove();

            if (Pants != null)
                if (!Pants.MoveToContainer(player.inventory.containerWear, 3))
                    Pants.Remove();

            if (Legs != null)
                if (!Legs.MoveToContainer(player.inventory.containerWear, 4))
                    Legs.Remove();

            if (Boots != null)
                if (!Boots.MoveToContainer(player.inventory.containerWear, 5))
                    Boots.Remove();
            #endregion

            #region Weapons, Attachments, and Ammo
            for (int i = 0; i < kit.WeaponKit.weaponShortName.Count; i++)
            {
                if (string.IsNullOrEmpty(kit.WeaponKit.weaponShortName[i])) continue;

                Item weapon = ItemManager.CreateByName(kit.WeaponKit.weaponShortName[i], 1);
                if (weapon == null) continue;

                BaseProjectile weaponObj = weapon.GetHeldEntity() as BaseProjectile;
                if (weaponObj != null)
                    weaponObj.primaryMagazine.contents = weaponObj.primaryMagazine.capacity;

                foreach (AttachmentInfo attachment in kit.WeaponKit.attachments)
                {
                    if (attachment.weapon != i) continue;
                    Item atachment = ItemManager.CreateByName(attachment.attachmentShortName);
                    if (atachment == null) continue;
                    if (!atachment.MoveToContainer(weapon.contents))
                        atachment.Remove();
                }

                if (!weapon.MoveToContainer(player.inventory.containerBelt))
                    if (!weapon.MoveToContainer(player.inventory.containerMain))
                        weapon.Remove();
            }

            for (int i = 0; i < kit.WeaponKit.weaponAmmo.Count; i++)
            {
                Item wepAmmo = ItemManager.CreateByName(kit.WeaponKit.weaponAmmo[i].ammoShortName, kit.WeaponKit.weaponAmmo[i].ammoCount);
                if (wepAmmo == null) continue;
                if (!wepAmmo.MoveToContainer(player.inventory.containerMain, -1, true, true))
                    wepAmmo.Remove();
            }

            for (int i = 0; i < kit.WoodWalls; i++)
            {
                Item woodwall = ItemManager.CreateByName("wall.external.high", 1);
                if (woodwall == null) continue;

                if (!woodwall.MoveToContainer(player.inventory.containerBelt))
                    if (!woodwall.MoveToContainer(player.inventory.containerMain))
                        woodwall.Remove();
            }
            for (int i = 0; i < kit.Syrimges; i++)
            {
                Item syringe = ItemManager.CreateByName("syringe.medical", 1);
                if (syringe == null) continue;

                if (!syringe.MoveToContainer(player.inventory.containerBelt))
                    if (!syringe.MoveToContainer(player.inventory.containerMain))
                        syringe.Remove();
            }
            for (int i = 0; i < kit.Medkits; i++)
            {
                Item medkit = ItemManager.CreateByName("largemedkit", 1);
                if (medkit == null) continue;

                if (!medkit.MoveToContainer(player.inventory.containerBelt))
                    if (!medkit.MoveToContainer(player.inventory.containerMain))
                        medkit.Remove();
            }
            for (int i = 0; i < kit.Bandages; i++)
            {
                Item bandage = ItemManager.CreateByName("bandage", 1);
                if (bandage == null) continue;

                if (!bandage.MoveToContainer(player.inventory.containerBelt))
                    if (!bandage.MoveToContainer(player.inventory.containerMain))
                        bandage.Remove();
            }
            #endregion

            #region Scopes
            if (arenaInfo.scopes)
            {
                Item FourTimesScope = ItemManager.CreateByName("weapon.mod.small.scope", 1);
                if (FourTimesScope != null)
                    if (!FourTimesScope.MoveToContainer(player.inventory.containerMain))
                        FourTimesScope.Remove();

                Item Holosight = ItemManager.CreateByName("weapon.mod.holosight", 1);
                if (Holosight != null)
                    if (!Holosight.MoveToContainer(player.inventory.containerMain))
                        Holosight.Remove();

                Item SimpleSight = ItemManager.CreateByName("weapon.mod.simplesight", 1);
                if (SimpleSight != null)
                    if (!SimpleSight.MoveToContainer(player.inventory.containerMain))
                        SimpleSight.Remove();
            }
            #endregion
        }

        private void TeamManagment(BasePlayer player, int arenaId, bool leaving = false)
        {
            if (player == null) return;
            if (leaving)
            {
                RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                if (team != null && team.members.Contains(player.userID))
                {
                    team.RemovePlayer(player.userID);
                    return;
                }
            }

            foreach (BasePlayer plr in PlayersInArena(arenaId))
            {
                if (plr == null || !playerData.ContainsKey(plr.UserIDString)) continue;
                if (playerData[plr.UserIDString].team == playerData[player.UserIDString].team)
                {
                    RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance.FindTeam(plr.currentTeam);
                    if (team == null) continue;

                    team.AddPlayer(player);
                    return;
                }
            }

            if (player.currentTeam == 0)
            {
                RelationshipManager.PlayerTeam myteam = RelationshipManager.ServerInstance.CreateTeam();
                if (myteam == null) return;

                if (myteam.AddPlayer(player))
                    myteam.SetTeamLeader(player.userID);
            }
        }

        private bool isInTeam(BasePlayer player, BasePlayer otherPlayer)
        {
            return playerData[player.UserIDString].team == playerData[otherPlayer.UserIDString].team;
        }

        private int GetPageCount(int count, BasePlayer player)
        {
            double result = (count + 34) / 35;
            bool isInt = result % 1 == 0;

            if (isInt)
            {
                return Convert.ToInt32(result);
            }
            else
            {
                double firstnumber = Math.Truncate(result);
                int returnnum = Convert.ToInt32(firstnumber);

                if (returnnum < 1)
                    return 1;
                else
                    return returnnum + 1;
            }
        }

        private void BanPlayer(BasePlayer player, string playerid)
        {
            string display = "";

            BasePlayer plr = BasePlayer.Find(playerid);
            if (plr == null)
                display = playerid;
            else
                display = plr.displayName;

            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo == null) return;

            if (plr != null && pInfo.arenaId == playerData[plr.UserIDString].arenaId)
            {
                PlayerTeams team = playerData[plr.UserIDString].team;
                int arenaID = pInfo.arenaId;
                ArenaInfo[pInfo.arenaId].TeamPlayers[playerData[plr.UserIDString].team].Remove(plr.UserIDString);
                TeamManagment(plr, pInfo.arenaId, true);
                ClearPlayerStats(plr, true, true);

                if (!ArenaInfo[arenaID].roundStarted && !ArenaInfo[arenaID].isStarting)
                    ReadyCheck(arenaID);

                if (ArenaInfo[arenaID].roundStarted || ArenaInfo[arenaID].isStarting)
                    if (RestartCheck(arenaID))
                        EndRound(arenaID);

                ClearArenaUIs(plr);
                UpdateScoreboard(arenaID, team);
                plr.ChatMessage(GetMessage("BannedFromArena"));
            }

            SendMessageToArena(pInfo.arenaId, "PlrBannedFromArena", display);
        }

        private void CheckWhitelist(BasePlayer player)
        {
            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo == null) return;

            if (!ArenaInfo.TryGetValue(pInfo.arenaId, out arenaSettings)) return;

            foreach (BasePlayer plr in PlayersInArena(pInfo.arenaId))
            {
                if (plr.UserIDString == arenaSettings.arenaOwner) continue;
                if (!arenaSettings.TeamWhitelist[PlayerTeams.TeamA].Contains(plr.UserIDString) && !arenaSettings.TeamWhitelist[PlayerTeams.TeamB].Contains(plr.UserIDString) && arenaSettings.teamWl)
                    UnwhitelistedPlayer(player, plr.UserIDString);
            }
        }

        private void UnwhitelistedPlayer(BasePlayer player, string playerid)
        {
            BasePlayer plr = BasePlayer.Find(playerid);
            if (plr == null) return;

            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo == null) return;

            if (pInfo.arenaId == playerData[plr.UserIDString].arenaId)
            {
                int arenaID = pInfo.arenaId;
                PlayerTeams team = pInfo.team;
                ArenaInfo[arenaID].TeamPlayers[pInfo.team].Remove(playerid);
                TeamManagment(plr, pInfo.arenaId, true);
                ClearPlayerStats(plr, false, true);

                if (!ArenaInfo[arenaID].roundStarted && !ArenaInfo[arenaID].isStarting) ReadyCheck(arenaID);
                if (ArenaInfo[arenaID].roundStarted || ArenaInfo[arenaID].isStarting)
                    if (RestartCheck(arenaID))
                        EndRound(arenaID);

                UpdateScoreboard(arenaID, team);
                TeleportPlayer(plr, GetSpawnpoint(arenaID, PlayerTeams.Spectator));
                HealPlayer(plr);
                SetupKit(plr, true);

                playerData[plr.UserIDString].team = PlayerTeams.Spectator;
                plr.ChatMessage(GetMessage("UnwhitelistedFromTeam"));
            }

        }

        private void SwitchMap(BasePlayer player, string mapName)
        {
            if (player == null || !playerData.ContainsKey(player.UserIDString)) return;

            mapName = mapName.Replace('|', ' ');
            int arenaId = playerData[player.UserIDString].arenaId;
            _save.ArenaData[arenaId].mapName = mapName;

            foreach (BasePlayer plr in PlayersInArena(arenaId, true))
            {
                bool spectator = playerData[plr.UserIDString].team == PlayerTeams.Spectator;
                ClearArenaUIs(plr);
                TeleportPlayer(plr, GetSpawnpoint(arenaId, playerData[plr.UserIDString].team));
                HealPlayer(plr);
                SetupKit(plr, spectator);
                ArenaHudUi(plr);
                if (spectator)
                    ScoreboardUI(player);

                plr.ChatMessage($"The arena owner has switched the map to {_save.ArenaData[arenaId].mapName}");
            }
        }

        private void DrawArenaText(BasePlayer player, bool isAdmin = false)
        {
            if (_save.ArenaEntrances.Count == 0) return;
            foreach (var arena in _save.ArenaEntrances)
            {
                if (!ArenaInfo.ContainsKey(arena.Key) || !_save.ArenaData.ContainsKey(arena.Key)) continue;
                string getPlayers = string.IsNullOrEmpty(ArenaInfo[arena.Key].arenaOwner) ? "<color=#660000> NO PLAYERS</color>" : $"<color=#004080> Team A: {ArenaInfo[arena.Key].TeamPlayers[PlayerTeams.TeamA].Count}</color>\n<color=#660000>Team B: {ArenaInfo[arena.Key].TeamPlayers[PlayerTeams.TeamB].Count}</color>";
                string formatted = string.Format("<b><color=##00804d> Arena {0}</color></b>\n<color=#1EC063>{1}</color>\n{2}", arena.Key, _save.ArenaData[arena.Key].mapName == string.Empty ? "NO MAP" : _save.ArenaData[arena.Key].mapName, getPlayers);
                player.SendConsoleCommand("ddraw.text", 1, Color.cyan, new Vector3(arena.Value.x, arena.Value.y + 2, arena.Value.z), formatted);
            }

            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin) && !isAdmin)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                player.SendNetworkUpdateImmediate();
            }

            return;
        }

        private void HandleArenaText(BasePlayer player, bool start = true)
        {
            if (player == null) return;

            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo.arenaTextTimer != null && !pInfo.arenaTextTimer.Destroyed)
                pInfo.arenaTextTimer?.Destroy();

            if (!start) return;           
            pInfo.arenaTextTimer = timer.Every(1f, () =>
            {
                if (player == null) return;
                if (Vector3.Distance(player.GetNetworkPosition(), _save.LobbySpawnpoint) > 30) return;
                if (!player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin))
                {
                    //player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                    //player.SendNetworkUpdateImmediate();
                    DrawArenaText(player, false);
                }
                else
                    DrawArenaText(player, true);
            });
        }

        private void TeleportPlayer(BasePlayer player, Vector3 location)
        {
            player.EnsureDismounted();
            if (player.HasParent())
                player.SetParent(null, true, true);

            if (player.IsConnected)
            {
                player.EndLooting();
                StartSleeping(player);
            }

            player.Teleport(location);
            if (player.IsConnected && !Net.sv.visibility.IsInside(player.net.group, location))
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
                player.ClientRPCPlayer(null, player, "StartLoading");
                player.SendEntityUpdate();
                player.UpdateNetworkGroup();
                player.SendNetworkUpdateImmediate(false);
            }

            player.metabolism.bleeding.value = 0;
            player.InitializeHealth(100, 100);
        }

        private void StartSleeping(BasePlayer player) //NTeleportation <3
        {
            if (!player.IsSleeping())
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
                player.sleepStartTime = Time.time;
                BasePlayer.sleepingPlayerList.Add(player);
                player.CancelInvoke("InventoryUpdate");
                player.CancelInvoke("TeamUpdate");
            }
        }
        #endregion

        #region Harmony
        private static class StopMoveOveride
        {
            internal static bool Prefix(BasePlayer ply, ref bool __result)
            {
                RustArenaPlayer pInfo;
                if (!Instance.playerData.TryGetValue(ply.UserIDString, out pInfo) || pInfo.arenaId == -1 || pInfo.team == PlayerTeams.Spectator || pInfo.isOut || Instance.ArenaInfo[pInfo.arenaId].roundStarted) return true;

                __result = false;
                return false;
            }
        }

        private static class AntihackOveride
        {
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var list = instructions.ToList();
                int layerMaskLine = list.FindLastIndex(i => i.opcode == OpCodes.Ldc_I4 && (int)i.operand == 429990145);
                if (layerMaskLine == -1) return list;

                // LayerMasks (8, 13, 16, 21) - Layer 21
                int newMask = 429990145 - 2097152;
                list[layerMaskLine] = new CodeInstruction(OpCodes.Ldc_I4, (int)newMask);
                return list;
            }
        }

        private static class IgnoreBullets
        {
            internal static bool Prefix(Effect effect)
            {
                BasePlayer player = BasePlayer.Find(effect.source.ToString());
                if (player == null) return true;

                if (Instance.inArena(player))
                {
                    SendToPlayers(player, effect);
                    return false;
                }

                return true;
            }
        }

        public static void SendToPlayers(BasePlayer player, Effect effect)
        {
            if (player == null) return;

            List<Connection> group = Instance.GetArenaPlayers(player);
            if (group == null || group.Count == 0) return;

            foreach (Connection oPlayer in group)
                EffectNetwork.Send(effect, oPlayer);
        }
        #endregion

        #region Hooks
        private void Init()
        {
            permission.RegisterPermission(Permcreatearena, this);
            permission.RegisterPermission(Permdeletearena, this);
            permission.RegisterPermission(Permsetspawns, this);
            permission.RegisterPermission(Permsetenter, this);

            AddCovalenceCommand("r", "ReadyCommand");
        }

        private void OnServerInitialized()
        {
            foreach (var arena in _save.ArenaData)
                GenerateArenaData(arena.Key);

            foreach (var entrance in _save.ArenaEntrances)
                CreateEntrance(entrance.Value, entrance.Key);
        }

        private void Loaded()
        {
            Instance = this;

            // Initialize Harmony
            /*if (_harmony == null) _harmony = HarmonyInstance.Create("com.BillyJoe.RustArena");
            _harmony.Patch(AccessTools.Method(typeof(AntiHack), "TestNoClipping"), transpiler: new HarmonyMethod(typeof(AntihackOveride), "Transpiler"));
            _harmony.Patch(AccessTools.Method(typeof(AntiHack), "ValidateMove"), new HarmonyMethod(typeof(StopMoveOveride), "Prefix"));
            _harmony.Patch(AccessTools.Method(typeof(EffectNetwork), "Send", new[] { typeof(Effect) }), new HarmonyMethod(typeof(IgnoreBullets), "Prefix"));*/

            _save = Interface.Oxide.DataFileSystem.ReadObject<SaveData>("RustArena");
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
        }

        private void Unload()
        {
            //_harmony.UnpatchAll("com.BillyJoe.RustArena");
            SaveArenaData();

            foreach (GameObject go in _arenaTriggers)
                UnityEngine.Object.DestroyImmediate(go);

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                OnLeaveArena(player);
                //HandleArenaText(player, false);
            }
        }

        void OnServerSave() => SaveArenaData();
        void OnPlayerConnected(BasePlayer player)
        {
            playerData.Add(player.UserIDString, new RustArenaPlayer());

            //if (player.IsDead())
            //    player.RespawnAt(_save.LobbySpawnpoint, default(Quaternion));

            //HandleArenaText(player);
        }

        /*void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            RustArenaPlayer pInfo;
            if (playerData.TryGetValue(player.UserIDString, out pInfo))
            {
                if (pInfo.arenaId != -1)
                    OnLeaveArena(player);

                player.inventory.Strip();
                player.Kill();
                playerData.Remove(player.UserIDString);
            }
        }*/

        private void OnEntitySpawned(DroppedItemContainer entity)
        {
            BasePlayer player = BasePlayer.FindByID(entity.playerSteamID);
            if (player == null) return;

            if (inArena(player))
                ArenaInfo[playerData[player.UserIDString].arenaId].Items.Add(entity);
        }

        private void OnEntityDeath(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;
            if (inArena(player))
            {
                RustArenaPlayer pInfo = playerData[player.UserIDString];
                pInfo.died = true;

                if (RestartCheck(pInfo.arenaId))
                    EndRound(pInfo.arenaId);

                player.Respawn();
            }
        }

        object OnEntityTakeDamage(BasePlayer player, HitInfo info)
        {
            if (player.IsNpc) return null;

            if (inArena(player))
            {
                RustArenaPlayer pInfo = playerData[player.UserIDString];
                if (!pInfo.canTakeDamage)
                {
                    info.damageTypes.ScaleAll(0); //check
                    return null;
                }

                BasePlayer attacker = info.InitiatorPlayer;
                if (attacker == null) return null;

                attacker.MarkHostileFor(0f);

                ArenaSettings arenaInfo = ArenaInfo[pInfo.arenaId];
                if (isInTeam(attacker, player) && !arenaInfo.friendlyFire)
                {
                    info.damageTypes.ScaleAll(0);
                    return true;
                }

                if (arenaInfo.onlyHeadshot)
                {
                    if (!info.isHeadshot)
                    {
                        info.damageTypes.ScaleAll(0);
                        return null;
                    }
                }

                RustArenaPlayer aInfo = playerData[attacker.UserIDString];
                if (aInfo == null) return null;

                aInfo.RoundDamage += Convert.ToInt32(info.damageTypes.Total());
                UpdateScoreboard(pInfo.arenaId, aInfo.team);
                return null;
            }
            return null;
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (!inArena(player)) return;

            NextTick(() =>
            {
                /*if (!inArena(player))
                {
                    RustArenaPlayer rp = playerData[player.UserIDString];
                    TeleportPlayer(player, rp.PlayerOldLocation);
                    //inventory backed
                    return;
                }*/

                player.Invoke(() =>
                {
                    RustArenaPlayer pInfo = playerData[player.UserIDString];
                    if (!ArenaInfo[pInfo.arenaId].roundStarted)
                    {
                        ClearArenaUIs(player);
                        ArenaHudUi(player);
                        SetupKit(player);
                        TeleportPlayer(player, GetSpawnpoint(pInfo.arenaId, pInfo.team));
                        HealPlayer(player);
                        return;
                    }

                    pInfo.isOut = true;
                    pInfo.canTakeDamage = false;
                    SetupKit(player, true);

                    TeleportPlayer(player, GetSpawnpoint(pInfo.arenaId, PlayerTeams.Spectator));
                    HealPlayer(player);
                    ArenaHudUi(player);
                    ScoreboardUI(player);
                    UpdateScoreboard(pInfo.arenaId, pInfo.team);

                    if (RestartCheck(pInfo.arenaId))
                        EndRound(pInfo.arenaId);
                }, 0);
            });
        }

        private object OnPlayerWound(BasePlayer player, HitInfo info)
        {
            if (player == null || player.IsNpc) return null;
            if (!ArenaInfo.TryGetValue(playerData[player.UserIDString].arenaId, out arenaSettings)) return null;

            player.GoToIncapacitated(info);
            if (!arenaSettings.allowPickup)
            {
                player.Die(info);
                return false;
            }

            return null;
        }

        object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null) return null;

            if (inArena(player))
            {
                if (!ArenaInfo.TryGetValue(playerData[player.UserIDString].arenaId, out arenaSettings)) return false;
                if (!arenaSettings.roundStarted) return true;
            }

            return null;
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            BasePlayer player = plan.GetOwnerPlayer();
            if (player == null || !inArena(player)) return;

            if (ArenaInfo.TryGetValue(playerData[player.UserIDString].arenaId, out arenaSettings))
                if (arenaSettings.roundStarted)
                    arenaSettings.Objects.Add(go);
        }

        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            BasePlayer attacker = info?.Initiator?.ToPlayer();
            if (attacker == null || attacker.IsNpc || attacker == player) return null;

            if (inArena(player))
            {
                RustArenaPlayer pInfo = playerData[player.UserIDString];
                RustArenaPlayer aInfo = playerData[attacker.UserIDString];
                aInfo.RoundKills += 1;

                if (pInfo.team == PlayerTeams.TeamB)
                    SendMessageToArena(pInfo.arenaId, "TeamAKilledTeamB", attacker.displayName, player.displayName);
                else
                    SendMessageToArena(pInfo.arenaId, "TeamBKilledTeamA", attacker.displayName, player.displayName);
            }

            return null;
        }

        private void OnItemDropped(Item item, BaseEntity entity)
        {
            BasePlayer player = item.GetOwnerPlayer();
            if (player == null || !inArena(player)) return;

            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (ArenaInfo.TryGetValue(pInfo.arenaId, out arenaSettings))
            {
                if (!arenaSettings.roundStarted)
                {
                    item.Remove();
                    return;
                }

                ArenaInfo[pInfo.arenaId].Items.Add(entity);
            }
        }

        private object CanNetworkTo(BasePlayer player, BasePlayer target)
        {
            if (inArena(player) && !inSameArena(player, target))
                return false;

            return null;
        }

        private object CanNetworkTo(DroppedItem item, BasePlayer player)
        {
            if (inArena(player) && !itemInArena(player, item))
                return false;

            return null;
        }

        private object CanNetworkTo(SimpleBuildingBlock wall, BasePlayer player)
        {
            if (wall == null || wall.ShortPrefabName != "wall.external.high.wood" || !inArena(player)) return null;

            int arenaID = playerData[player.UserIDString].arenaId;
            if (ArenaInfo.ContainsKey(arenaID) && ArenaInfo[arenaID].Objects.Contains(wall.gameObject)) return null;
            return false;
        }
        #endregion

        #region Commands

        [Command("lobby")]
        private void LobbyCMD(IPlayer iPlayer, string cmd, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null) return;

            if (inArena(player)) return;
            ItemCrafter crafter = player.inventory.crafting;
            crafter.CancelAll(true);
            if (Instance.CheckPlayerInventory(player))
            {
                player.ChatMessage("<color=#990000>Your Inventory MUST BE EMPTY to so you use Arena Event</color>!");
                return;
            }

            player.ChatMessage("<color=#FFFF00>Teleporting to Arena Lobby in 5 Sec.</color>");                     
            timer.Once(5, () =>
            {
                TeleportPlayerToLobby(player, _save.LobbySpawnpoint);
            });
            
        }

        [Command("setlobby")]
        private void SetLobbyCommand(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, Permsetspawns)) { player.ChatMessage("You do not have permission to use this command!"); return; }

            _save.LobbySpawnpoint = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
            SaveArenaData();

            SendMessage(iPlayer, "SetLobbySpawn");
        }

        [Command("setentrance")]
        private void SetEntranceCommand(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null || args.Length != 1) return;
            if (!permission.UserHasPermission(player.UserIDString, Permsetenter)) { player.ChatMessage("You do not have permission to use this command!"); return; }

            int arenaId = Convert.ToInt32(args[0]);
            Vector3 pos = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
            _save.ArenaEntrances[arenaId] = pos;
            SaveArenaData();

            CreateEntrance(pos, arenaId);
            SendMessage(iPlayer, "SetEntrance", arenaId);
        }

        private int FindArenaMap(string mapName)
        {
            return _save.ArenaMaps.FindIndex(x => x.mapName == mapName);
        }

        [Command("createmap")]
        private void creaatemapcmd(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null || args.Length != 1) return;

            if (!permission.UserHasPermission(player.UserIDString, Permcreatearena)) { player.ChatMessage("You do not have permission to use this command!"); return; }

            string mapName = args[0];
            if (string.IsNullOrEmpty(mapName)) { player.ChatMessage("Please enter a map name!"); return; }
            if (FindArenaMap(mapName) > -1) { player.ChatMessage("Already seems to be a map that exists with this name, try again!"); return; }

            _save.ArenaMaps.Add(new MapData
            {
                mapName = mapName,
                mapImage = string.Empty,
                spawns = new Dictionary<PlayerTeams, Vector3>()
            });

            player.ChatMessage($"You have created a map called {mapName}.");
        }

        [Command("deletemap")]
        private void deletemapcmd(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null || args.Length != 1) return;

            if (!permission.UserHasPermission(player.UserIDString, Permdeletearena)) { player.ChatMessage("You do not have permission to use this command!"); return; }

            string mapName = args[0];
            if (string.IsNullOrEmpty(mapName)) { player.ChatMessage("Please enter a map name!"); return; }

            int mapIndex = FindArenaMap(mapName);
            if (mapIndex == -1) { player.ChatMessage("Couldn't find a map with this name, try again!"); return; }

            _save.ArenaMaps.RemoveAt(mapIndex);
            player.ChatMessage($"You have removed a map called {mapName}.");
        }

        [Command("setspawn")]
        private void SetSpawnCommand(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null || args.Length != 2) return;

            if (!permission.UserHasPermission(player.UserIDString, Permsetspawns)) { player.ChatMessage("You do not have permission to use this command!"); return; }

            string mapName = args[0];
            if (string.IsNullOrEmpty(mapName)) { player.ChatMessage("Please enter a map name!"); return; }

            int teamId = Convert.ToInt32(args[1]);
            if (!(new int[] { 0, 1, 2 }.Contains(teamId))) { player.ChatMessage("Please enter a number 0-2 for the team spawn!"); return; }

            int mapIndex = FindArenaMap(mapName);
            if (mapIndex == -1) { player.ChatMessage("Couldn't find a map with this name, try again!"); return; }

            Vector3 pos = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
            _save.ArenaMaps.ElementAt(mapIndex).spawns[(PlayerTeams)teamId] = pos;
            player.ChatMessage($"You have set the spawn for {(PlayerTeams)teamId} on {_save.ArenaMaps.ElementAt(mapIndex).mapName}");
        }

        [Command("rustarena")]
        private void ArenaCommand(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null) return;

            RustArenaPlayer pInfo = playerData[player.UserIDString];
            switch (args[0])
            {
                case "join":
                    CuiHelper.DestroyUi(player, "TeamSelection");
                    if (args.Length < 3) return;

                    int arenaIdj = Convert.ToInt32(args[1]), teamId = Convert.ToInt32(args[2]);
                    if (!ArenaInfo.TryGetValue(arenaIdj, out arenaSettings))
                    {
                        SendMessage(iPlayer, "NoArena");
                        CuiHelper.DestroyUi(player, "TeamSelection");
                        return;
                    }

                    if (teamId == -1 || teamId > 2) { player.ChatMessage("Counldn't seem to find team, try again!"); return; }

                    bool switchingTeams = pInfo.arenaId == arenaIdj;
                    if (pInfo.arenaId != -1 && !switchingTeams) { player.ChatMessage("You are already in an arena, please leave first."); return; }

                    pInfo.arenaId = arenaIdj;
                    if (player.Connection != null)
                    {
                        if (arenaConnections.ContainsKey(player.Connection))
                            arenaConnections[player.Connection] = arenaIdj;
                        else
                            arenaConnections.Add(player.Connection, arenaIdj);
                    }

                    if (string.IsNullOrEmpty(arenaSettings.arenaOwner))
                        ArenaInfo[arenaIdj].arenaOwner = player.UserIDString;

                    if (switchingTeams)
                    {
                        ArenaInfo[arenaIdj].TeamPlayers[pInfo.team].Remove(player.UserIDString);
                        pInfo.isReady = false;
                        TeamManagment(player, arenaIdj, true);
                        UpdateScoreboard(arenaIdj, PlayerTeams.None);
                    }                    
                        //HandleArenaText(player, false);

                    if ((PlayerTeams)teamId == PlayerTeams.Spectator)
                    {
                        pInfo.team = PlayerTeams.Spectator;
                        TeleportPlayer(player, GetSpawnpoint(arenaIdj, PlayerTeams.Spectator));
                        SetupKit(player, true);
                        SendMessage(iPlayer, "JoinedArenaTeam", arenaIdj, pInfo.team.ToString());
                        SendMessageToArena(arenaIdj, "PlayerJoinedArena", player.displayName);
                    }
                    else
                    {
                        if (arenaSettings.teamWl && !arenaSettings.TeamWhitelist[(PlayerTeams)teamId].Contains(player.UserIDString))
                        {
                            if (pInfo.team != PlayerTeams.Spectator)
                            {
                                SendMessage(iPlayer, "NotWhitelistedForTeam", arenaIdj);
                                return;
                            }

                            pInfo.team = PlayerTeams.Spectator;
                            TeleportPlayer(player, GetSpawnpoint(arenaIdj, PlayerTeams.Spectator));
                            SetupKit(player, true);
                            SendMessage(iPlayer, "JoinedArenaNoTeam", arenaIdj);
                        }
                        else
                        {
                            pInfo.team = (PlayerTeams)teamId;
                            if (arenaSettings.roundStarted || arenaSettings.isStarting)
                            {
                                pInfo.isOut = true;
                                TeleportPlayer(player, GetSpawnpoint(arenaIdj, PlayerTeams.Spectator));
                                SetupKit(player, true);
                            }
                            else
                            {
                                TeleportPlayer(player, GetSpawnpoint(arenaIdj, pInfo.team));
                                SetupKit(player);
                            }

                            SendMessage(iPlayer, "JoinedArenaTeam", arenaIdj, ArenaInfo[arenaIdj].TeamNames[pInfo.team]);
                            TeamManagment(player, arenaIdj);
                        }
                    }

                    ArenaInfo[arenaIdj].TeamPlayers[pInfo.team].Add(player.UserIDString);
                    if (!switchingTeams)
                    {
                        ScoreboardUI(player);
                        Interface.CallHook("OnJoinArena", player);
                    }
                    ArenaHudUi(player);
                    UpdateScoreboard(arenaIdj, PlayerTeams.None);
                    break;

                case "create":
                    if (!permission.UserHasPermission(player.UserIDString, Permcreatearena)) { SendMessage(iPlayer, "NoPermission"); return; }
                    if (args.Length != 4) { SendMessage(iPlayer, "MissingArgs"); return; }

                    int arenaIdc = Convert.ToInt32(args[1]);
                    if (_save.ArenaData.ContainsKey(arenaIdc)) { SendMessage(iPlayer, "ArenaAlreadyExist"); return; }

                    string arenaMapName = args[2];
                    if (FindArenaMap(arenaMapName) == -1) { player.ChatMessage("There is no map that exists with this name!"); return; }

                    bool vipArena = Convert.ToBoolean(args[3]);
                    _save.ArenaData.Add(arenaIdc, new Arena
                    {
                        arenaId = arenaIdc,
                        vipArena = vipArena,
                        mapName = arenaMapName
                    });

                    GenerateArenaData(arenaIdc);
                    SendMessage(iPlayer, "CreatedArena", arenaIdc);
                    break;

                case "delete":
                    if (!permission.UserHasPermission(player.UserIDString, Permdeletearena)) { SendMessage(iPlayer, "NoPermission"); return; }

                    int arenaIdd = Convert.ToInt32(args[1]);
                    if (!ArenaInfo.ContainsKey(arenaIdd)) SendMessage(iPlayer, "InvalidID", arenaIdd);

                    _save.ArenaData.Remove(arenaIdd);
                    _save.ArenaEntrances.Remove(arenaIdd);
                    SendMessage(iPlayer, "DeletedArena", arenaIdd);
                    break;
            }
        }

        [Command("fs")]
        private void ForceStartCommand(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null) return;

            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo.arenaId == -1) return;

            if (!IsArenaOwner(player))
            {
                SendMessage(iPlayer, "NoPermission");
                return;
            }

            ForceStartArena(player, pInfo.arenaId);
        }

        [Command("leave")]
        private void LeaveCommand(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null) return;

            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo.arenaId == -1) return;

            OnLeaveArena(player);
            player.inventory.Strip();
        }

        private void OnLeaveArena(BasePlayer player)
        {
            if (player == null) return;

            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (!ArenaInfo.TryGetValue(pInfo.arenaId, out arenaSettings)) return;
            if (arenaSettings.TeamPlayers[pInfo.team].Contains(player.UserIDString))
                ArenaInfo[pInfo.arenaId].TeamPlayers[pInfo.team].Remove(player.UserIDString);

            int arenaID = pInfo.arenaId;
            TeamManagment(player, pInfo.arenaId, true);
            ClearPlayerStats(player, true, true);
            TeleportPlayer(player, _save.LobbySpawnpoint);

            if (!string.IsNullOrEmpty(arenaSettings.arenaOwner) && arenaSettings.arenaOwner == player.UserIDString)
            {
                List<BasePlayer> playersInArena = Pool.GetList<BasePlayer>();
                playersInArena.AddRange(PlayersInArena(arenaID));

                if (playersInArena.Contains(player))
                    playersInArena.Remove(player);

                BasePlayer newOwner = null;
                if (_save.ArenaData[arenaID].vipArena)
                {
                    foreach (BasePlayer aPlayer in playersInArena)
                    {
                        if (!permission.UserHasPermission(aPlayer.UserIDString, "rustarena.vip")) continue;
                        newOwner = aPlayer;
                        break;
                    }

                    if (newOwner == null)
                    {
                        foreach (BasePlayer nPlayer in playersInArena)
                        {

                            TeleportPlayer(nPlayer, _save.LobbySpawnpoint);
                            player.inventory.Strip();

                            ClearArenaUIs(nPlayer);
                            //HandleArenaText(nPlayer);
                            TeamManagment(nPlayer, arenaID, true);
                            ClearPlayerStats(nPlayer, true, true);

                            if (nPlayer.Connection != null && arenaConnections.ContainsKey(nPlayer.Connection))
                                arenaConnections.Remove(nPlayer.Connection);

                            if (playersInArena.Contains(nPlayer))
                                playersInArena.Remove(nPlayer);

                            nPlayer.ChatMessage("The arena owner has left, since its a vip arena it has been closed.");
                        }

                        if (player.Connection != null && arenaConnections.ContainsKey(player.Connection))
                            arenaConnections.Remove(player.Connection);

                        ClearArenaUIs(player);
                        ResetGame(player, arenaID, true);
                        return;
                    }
                    else
                    {
                        ArenaInfo[arenaID].arenaOwner = newOwner.UserIDString;
                        SendMessageToArena(arenaID, "NewArenaOwner", newOwner.displayName);
                        foreach (BasePlayer plyr in playersInArena)
                        {
                            if (plyr == null) continue;
                            ArenaHudUi(plyr);
                        }
                        return;
                    }
                }

                if (playersInArena.Count == 0)
                    ResetGame(player, arenaID, true);
                else
                {
                    if (newOwner == null) newOwner = playersInArena[0];
                    ArenaInfo[arenaID].arenaOwner = newOwner.UserIDString;
                    SendMessageToArena(arenaID, "NewArenaOwner", newOwner.displayName);
                    foreach (BasePlayer plyr in playersInArena)
                    {
                        if (plyr == null) continue;
                        ArenaHudUi(plyr);
                    }
                }

                Pool.FreeList(ref playersInArena);
            }

            if (!arenaSettings.roundStarted && !arenaSettings.isStarting)
                ReadyCheck(arenaID);

            if (arenaSettings.roundStarted || arenaSettings.isStarting)
                if (RestartCheck(arenaID))
                    EndRound(arenaID);

            if (player.Connection != null && arenaConnections.ContainsKey(player.Connection))
                arenaConnections.Remove(player.Connection);

            //HandleArenaText(player);
            SendMessageToArena(arenaID, "LeftArena", player.displayName);
            UpdateScoreboard(arenaID, PlayerTeams.None);
            ClearArenaUIs(player);
        }

        [Command("settings")]
        private void SettingsCommand(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null) return;

            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo.arenaId == -1)
            {
                SendMessage(iPlayer, "NotInArena");
                return;
            }

            if (!IsArenaOwner(player))
            {
                SendMessage(iPlayer, "NoPermission");
                return;
            }

            OpenSettingsMenu(player);
        }

        [Command("ready")]
        private void ReadyCommand(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null) return;

            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo.arenaId == -1 || ArenaInfo[pInfo.arenaId].roundStarted || pInfo.team == PlayerTeams.Spectator) return;
            pInfo.isReady = !pInfo.isReady;

            string readytext = pInfo.isReady ? lang.GetMessage("Ready", this) + "!" : lang.GetMessage("NotReady", this) + "!";
            SendMessageToArena(pInfo.arenaId, "ReadyState", player.displayName, readytext);
            ReadyUi(player);
            ReadyCheck(pInfo.arenaId);
            UpdateScoreboard(pInfo.arenaId, pInfo.team);
        }

        [Command("destroyui")]
        private void DestroyUiCommand(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null) return;

            RustArenaPlayer pInfo = playerData[player.UserIDString];
            switch (args[0])
            {
                case "TransferOwnership":
                    CuiHelper.DestroyUi(player, "TransferOwnership");
                    CuiHelper.DestroyUi(player, "TransferPlayersPanel");
                    break;

                case "ArenaSettings":
                    ClearArenaUIs(player, false);

                    foreach (BasePlayer plr in PlayersInArena(pInfo.arenaId))
                    {
                        ArenaHudUi(plr);
                        SetupKit(plr);
                        ScoreboardUI(plr);
                    }

                    break;

                default:
                    CuiHelper.DestroyUi(player, args[0]);
                    break;
            }
        }

        [Command("hudactions")]
        private void HudActionsCommand(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null) return;

            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo.arenaId == -1 || pInfo.team == PlayerTeams.None) return;

            switch (args[0])
            {
                case "changeteam":
                    TeamSelectUi(player, pInfo.arenaId, true);
                    break;

                case "settings":
                    if (!IsArenaOwner(player))
                    {
                        player.ChatMessage(GetMessage("NoPermission"));
                        return;
                    }

                    OpenSettingsMenu(player);
                    break;
            }
        }

        [Command("switchsettingmenu")]
        private void SwitchSettingMenuCmd(IPlayer iPlayer, string command, string[] args)
        {
            if (iPlayer.IsServer) return;
            BasePlayer player = iPlayer.Object as BasePlayer;

            RustArenaPlayer pInfo = playerData[player.UserIDString];
            switch (args[0])
            {
                case "general":
                    pInfo.ActiveMenu = 0;
                    break;
                case "loadout":
                    pInfo.ActiveMenu = 1;
                    break;
                case "awhitelist":
                    pInfo.ActiveMenu = 2;
                    break;
                case "arenabans":
                    pInfo.ActiveMenu = 3;
                    break;
                case "mapselection":
                    pInfo.ActiveMenu = 4;
                    break;
            }

            OpenSettingsMenu(player);
        }

        [Command("settingsmenucmds")]
        private void SettingMenuActionsCmd(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null) return;

            RustArenaPlayer pInfo = playerData[player.UserIDString];
            ArenaSettings arenaInfo = ArenaInfo[pInfo.arenaId];

            switch (args[0])
            {
                case "general":
                    switch (args[1])
                    {
                        case "private":
                            arenaInfo.isPublic = !arenaInfo.isPublic;

                            if (arenaInfo.isPublic) SendMessageToArena(pInfo.arenaId, "ArenaIsPublic");
                            else SendMessageToArena(pInfo.arenaId, "ArenaIsPrivate");
                            break;
                        case "headshot":
                            arenaInfo.onlyHeadshot = !arenaInfo.onlyHeadshot;

                            if (arenaInfo.onlyHeadshot) SendMessageToArena(pInfo.arenaId, "ArenaHeadshotOnly");
                            else SendMessageToArena(pInfo.arenaId, "ArenaNotHeadshot");
                            break;
                        case "friendlyfire":
                            arenaInfo.friendlyFire = !arenaInfo.friendlyFire;

                            if (arenaInfo.friendlyFire) SendMessageToArena(pInfo.arenaId, "ArenaIsFF");
                            else SendMessageToArena(pInfo.arenaId, "ArenaIsNotFF");
                            break;
                        case "scopes":
                            arenaInfo.scopes = !arenaInfo.scopes;

                            if (arenaInfo.scopes) SendMessageToArena(pInfo.arenaId, "ArenaScopes");
                            else SendMessageToArena(pInfo.arenaId, "ArenaNoScopes");

                            foreach (BasePlayer rPlayer in PlayersInArena(pInfo.arenaId)) SetupKit(rPlayer);
                            break;
                        case "clearentities":
                            arenaInfo.cleanupEntities = !arenaInfo.cleanupEntities;

                            if (arenaInfo.cleanupEntities) SendMessageToArena(pInfo.arenaId, "ArenaCleanupEnts");
                            else SendMessageToArena(pInfo.arenaId, "ArenaNoCleanupEnts");
                            break;
                        case "wounded":
                            arenaInfo.allowPickup = !arenaInfo.allowPickup;

                            if (arenaInfo.allowPickup) SendMessageToArena(pInfo.arenaId, "ArenaAllowPickup");
                            else SendMessageToArena(pInfo.arenaId, "ArenaNoPickup");
                            break;
                        case "teamwhitelist":
                            arenaInfo.teamWl = !arenaInfo.teamWl;

                            if (arenaInfo.teamWl) SendMessageToArena(pInfo.arenaId, "ArenaTeamWL");
                            else SendMessageToArena(pInfo.arenaId, "ArenaNoTeamWL");

                            if (arenaInfo.teamWl)
                                CheckWhitelist(player);

                            break;
                    }
                    break;
                case "generalact":
                    switch (args[1])
                    {
                        case "startround":
                            ForceStartArena(player, pInfo.arenaId);
                            return;
                        case "endround":
                            ClearArenaUIs(player, false);
                            EndRound(pInfo.arenaId, true);
                            return;
                        case "restartgame":
                            ResetGame(player, pInfo.arenaId);
                            break;
                        case "cleararena":
                            foreach (GameObject go in arenaInfo.Objects)
                                go.GetComponent<BaseEntity>()?.Kill();

                            arenaInfo.Objects.Clear();
                            SendMessageToArena(pInfo.arenaId, "ClearedAllEntities");
                            break;
                        case "healplayers":
                            foreach (BasePlayer fPlayer in PlayersInArena(pInfo.arenaId))
                            {
                                HealPlayer(fPlayer);
                                SendMessageToArena(pInfo.arenaId, "HealedAllPlayers");
                            }
                            break;
                        case "switchsides":
                            SwitchSides(player, pInfo.arenaId);
                            break;
                        case "transferowner":
                            TransferOwnership(player);
                            return;
                        case "transferownercomplete":
                            TransferOwner(player, pInfo.arenaId, args[2]);
                            break;
                        case "refreshkits":
                            foreach (BasePlayer rPlayer in PlayersInArena(pInfo.arenaId))
                                SetupKit(rPlayer);
                            break;
                    }
                    break;
                case "generalset":
                    switch (args[1])
                    {
                        case "rounddown":
                            if (arenaInfo.roundCount == 1) return;
                            arenaInfo.roundCount = arenaInfo.roundCount - 1;
                            break;
                        case "roundup":
                            if (arenaInfo.roundCount == 10) return;
                            arenaInfo.roundCount = arenaInfo.roundCount + 1;
                            break;
                    }

                    foreach (BasePlayer setplayer in PlayersInArena(pInfo.arenaId))
                    {
                        if (setplayer != player)
                            ArenaHudUi(setplayer);
                    }
                    break;

                case "loadoutset":
                    int index = Convert.ToInt32(args[1]);
                    ActiveKit kit = ArenaInfo[pInfo.arenaId].ActiveKit;
                    switch (args[2])
                    {
                        case "Weapons":
                            kit.WeaponKit = config.kitSettings.weaponSettings.WeaponKits[index];
                            LoadWeapons(player);
                            break;
                        case "Headgear":
                            kit.ArmorKit.Helmet = config.kitSettings.attireSettings.ArmorKits[index]; LoadArmor(player);
                            break;
                        case "Chest":
                            kit.ArmorKit.Chest = config.kitSettings.attireSettings.ArmorKits[index]; LoadArmor(player);
                            break;
                        case "Gloves":
                            kit.ArmorKit.Gloves = config.kitSettings.attireSettings.ArmorKits[index]; LoadArmor(player);
                            break;
                        case "Pants":
                            kit.ArmorKit.Pants = config.kitSettings.attireSettings.ArmorKits[index]; LoadArmor(player);
                            break;
                        case "Boots":
                            kit.ArmorKit.Boots = config.kitSettings.attireSettings.ArmorKits[index]; LoadArmor(player);
                            break;
                        case "Syringes":
                            if (kit.Syrimges == 0 && args[3] == "down" || kit.Syrimges == config.kitSettings.MaxSyringes && args[3] == "up") return;
                            if (args[3] == "up") kit.Syrimges += 1; else kit.Syrimges -= 1;
                            LoadMiscItems(player);
                            break;
                        case "Medkits":
                            if (kit.Medkits == 0 && args[3] == "down" || kit.Medkits == config.kitSettings.MaxMedkits && args[3] == "up") return;
                            if (args[3] == "up") kit.Medkits += 1; else kit.Medkits -= 1;
                            LoadMiscItems(player);
                            break;
                        case "Bandages":
                            if (kit.Bandages == 0 && args[3] == "down" || kit.Bandages == config.kitSettings.MaxBandages && args[3] == "up") return;
                            if (args[3] == "up") kit.Bandages += 1; else kit.Bandages -= 1;
                            LoadMiscItems(player);
                            break;
                        case "WoodWall":
                            if (kit.WoodWalls == 0 && args[3] == "down" || kit.WoodWalls == config.kitSettings.MaxWoodWalls && args[3] == "up") return;
                            if (args[3] == "up") kit.WoodWalls += 1; else kit.WoodWalls -= 1;
                            LoadMiscItems(player);
                            break;
                    }

                    return;
                case "awhitelist":
                    string wlID = args[1];
                    bool isWl = Convert.ToBoolean(args[2]);
                    if (!arenaInfo.ArenaWhitelistedIDs.Contains(wlID) && isWl) arenaInfo.ArenaWhitelistedIDs.Add(wlID); else if (arenaInfo.ArenaWhitelistedIDs.Contains(wlID) && !isWl) arenaInfo.ArenaWhitelistedIDs.Remove(wlID);
                    LoadArenaWhitelist(player);
                    LoadArenaWhitelist(player, false);
                    return;

                case "twhitelist":
                    string twlID = args[1];
                    bool istWl = Convert.ToBoolean(args[2]);
                    PlayerTeams team = (PlayerTeams)Convert.ToInt32(args[3]);
                    if (!arenaInfo.TeamWhitelist[team].Contains(twlID) && istWl)
                    {
                        arenaInfo.TeamWhitelist[team].Add(twlID);
                    }
                    else if (arenaInfo.TeamWhitelist[team].Contains(twlID) && !istWl)
                    {
                        arenaInfo.TeamWhitelist[team].Remove(twlID);
                        UnwhitelistedPlayer(player, twlID);
                    }

                    LoadTeamWhitelist(player, true, 1, team);
                    LoadTeamWhitelist(player, false, 1, team);
                    return;

                case "abans":
                    string banID = args[1];
                    bool isBan = Convert.ToBoolean(args[2]);
                    if (!arenaInfo.ArenaBannedIDs.Contains(banID) && isBan)
                    {
                        arenaInfo.ArenaBannedIDs.Add(banID);
                        BanPlayer(player, banID);
                    }
                    else
                    {
                        if (arenaInfo.ArenaBannedIDs.Contains(banID) && !isBan)
                            arenaInfo.ArenaBannedIDs.Remove(banID);
                    }

                    LoadArenaBans(player);
                    LoadArenaBans(player, false);
                    return;

                case "changeselteam":
                    PlayerTeams changedteam = (PlayerTeams)Convert.ToInt32(args[1]);
                    pInfo.selectedTeam = changedteam;
                    LoadTeamWhitelist(player, true, pInfo.TWLPage, changedteam);
                    LoadTeamWhitelist(player, false, pInfo.TDWLPage, changedteam);
                    break;

                case "changepage":
                    string option = args[1];
                    int page = Convert.ToInt32(args[2]);
                    switch (option)
                    {
                        case "whitelist":
                            pInfo.WLPage = page;
                            LoadArenaWhitelist(player, true, page);
                            break;

                        case "dewhitelist":
                            pInfo.DWLPage = page;
                            LoadArenaWhitelist(player, false, page);
                            break;

                        case "ban":
                            pInfo.ABPage = page;
                            LoadArenaBans(player, true, page);
                            break;

                        case "unban":
                            pInfo.AUBPage = page;
                            LoadArenaBans(player, false, page);
                            break;
                        case "twhitelist":
                            pInfo.TWLPage = page;
                            LoadTeamWhitelist(player, true, page, pInfo.selectedTeam);
                            break;

                        case "tdwhitelist":
                            pInfo.TDWLPage = page;
                            LoadTeamWhitelist(player, false, page, pInfo.selectedTeam);
                            break;
                        case "switcharena":
                            pInfo.SAPage = page;
                            LoadSwitchArena(player, page);
                            break;
                    }
                    return;

                case "switcharena":
                    SwitchMap(player, args[1]);
                    return;
            }

            LoadSettingsPanel(player);
        }

        [Command("rustarenainput")]
        private void RustArenaInputCmd(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null) return;

            if (args.Length <= 0) return;
            string optionargs = args[0];
            var option = _teamANameInput;

            switch (optionargs)
            {
                case "TeamAName":
                    option = _teamANameInput;
                    break;
                case "TeamBName":
                    option = _teamBNameInput;
                    break;
            }

            if (args.Length <= 1)
            {
                if (option.ContainsKey(player.UserIDString))
                    option.Remove(player.UserIDString);


                if (option == _teamANameInput) ArenaInfo[playerData[player.UserIDString].arenaId].TeamNames[PlayerTeams.TeamA] = "Team A";
                else if (option == _teamBNameInput) ArenaInfo[playerData[player.UserIDString].arenaId].TeamNames[PlayerTeams.TeamB] = "Team B";
                return;
            }

            if (option.ContainsKey(player.UserIDString))
                option[player.UserIDString] = args[1];
            else
                option.Add(player.UserIDString, args[1]);

            if (option == _teamANameInput)
                ArenaInfo[playerData[player.UserIDString].arenaId].TeamNames[PlayerTeams.TeamA] = option[player.UserIDString];
            else if (option == _teamBNameInput)
                ArenaInfo[playerData[player.UserIDString].arenaId].TeamNames[PlayerTeams.TeamB] = option[player.UserIDString];
        }
        #endregion

        #region UI

        #region Side Select
        private void TeamSelectUi(BasePlayer player, int arenaId, bool inArena = false)
        {
            CuiHelper.DestroyUi(player, "TeamSelection");
            if (!ArenaInfo.ContainsKey(arenaId)) return;

            ArenaSettings arenaInfo = ArenaInfo[arenaId];
            if (arenaInfo == null) return;

            if (!inArena)
            {
                if (playerData[player.UserIDString].arenaId != -1) { player.ChatMessage(GetMessage("AlreadyInArena")); return; }

                if (!arenaInfo.isPublic && !arenaInfo.ArenaWhitelistedIDs.Contains(player.UserIDString)) { player.ChatMessage(GetMessage("PrivateArena")); return; }
                if (arenaInfo.ArenaBannedIDs.Contains(player.UserIDString)) { player.ChatMessage(GetMessage("BannedFromArena")); return; }
            }

            playerData[player.UserIDString].currentUi = "TeamSelection";
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
            }, "Overlay", "TeamSelection");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.03921569 0.03921569 0.03921569 0.8", Material = "assets/content/ui/uibackgroundblur.mat" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
            }, "TeamSelection", "BlurPanel");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.09411765 0.09411765 0.09411765 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-201 17", OffsetMax = "201 41" }
            }, "TeamSelection", "TopPanel");

            container.Add(new CuiElement
            {
                Name = "QuestionText",
                Parent = "TopPanel",
                Components = {
                    new CuiTextComponent { Text = "What team would you like to join?", Font = "robotocondensed-regular.ttf", FontSize = 13, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "11 0", OffsetMax = "-211 0" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2 0.1960784 0.2 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-201 -87", OffsetMax = "201 17" }
            }, "TeamSelection", "MiddlePanel");

            container.Add(new CuiButton
            {
                Button = { Color = "0.06666667 0.06666667 0.06666667 1", Command = $"rustarena join {arenaId} 0" },
                Text = { Text = "TEAM A", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-186 5", OffsetMax = "-9 36" }
            }, "MiddlePanel", "TeamAButton");

            container.Add(new CuiButton
            {
                Button = { Color = "0.06603771 0.06603771 0.06603771 1", Command = $"rustarena join {arenaId} 1" },
                Text = { Text = "TEAM B", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "4 5", OffsetMax = "185 36" }
            }, "MiddlePanel", "TeamBButton");

            container.Add(new CuiButton
            {
                Button = { Color = "0.002313996 0.1727417 0.490566 1", Command = $"rustarena join {arenaId} 2" },
                Text = { Text = "SPECTATORS", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-186 -36", OffsetMax = "185 -6" }
            }, "MiddlePanel", "SpectatorsButton");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region HUD UI
        private void ArenaHudUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "ArenaHUDTop");
            CuiHelper.DestroyUi(player, "ArenaHUDSidebar");

            RustArenaPlayer pInfo = playerData[player.UserIDString];
            ArenaSettings arenaInfo = ArenaInfo[pInfo.arenaId];
            if (arenaInfo == null) return;

            if (!pInfo.isOut && arenaInfo.roundStarted) return;

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-150 -123.143", OffsetMax = "150 -48.143" }
            }, "Overlay", "ArenaHUDTop");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.03921569 0.03921569 0.03921569 0.8" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -10", OffsetMax = "50 10" }
            }, "ArenaHUDTop", "FirstToBackground");

            container.Add(new CuiElement
            {
                Name = "FirstToText",
                Parent = "FirstToBackground",
                Components = {
                    new CuiTextComponent { Text = $"FIRST TO {arenaInfo.roundCount}", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.009433985 0.009433985 0.009433985 0.8" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-150 17.5", OffsetMax = "-70 37.5" }
            }, "ArenaHUDTop", "YourTeamScoreTop");

            container.Add(new CuiElement
            {
                Name = "YourTeamScoreTopText",
                Parent = "YourTeamScoreTop",
                Components = {
                    new CuiTextComponent { Text = $"{(pInfo.team == PlayerTeams.Spectator ? arenaInfo.TeamNames[PlayerTeams.TeamA].ToUpper() : "YOUR TEAM")}", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1037736 0.1037736 0.1037736 0.7098039", Material = "assets/content/ui/uibackgroundblur.mat" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-150 -37", OffsetMax = "-70 17.5" }
            }, "ArenaHUDTop", "YourTeamScoreBottom");

            container.Add(new CuiElement
            {
                Name = "YourTeamScoreText",
                Parent = "YourTeamScoreBottom",
                Components = {
                    new CuiTextComponent { Text = $"{(pInfo.team == PlayerTeams.Spectator ? arenaInfo.Score[PlayerTeams.TeamA].ToString() : arenaInfo.Score[pInfo.team].ToString())}", Font = "robotocondensed-bold.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.009433985 0.009433985 0.009433985 0.8" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "70 17.5", OffsetMax = "150 37.5" }
            }, "ArenaHUDTop", "OtherTeamScoreTop");

            container.Add(new CuiElement
            {
                Name = "OtherTeamScoreTopText",
                Parent = "OtherTeamScoreTop",
                Components = {
                    new CuiTextComponent { Text = $"{(pInfo.team == PlayerTeams.Spectator ? arenaInfo.TeamNames[PlayerTeams.TeamB].ToUpper() : arenaInfo.TeamNames[GetOtherTeam(pInfo.team)].ToUpper())}", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1037736 0.1037736 0.1037736 0.7098039", Material = "assets/content/ui/uibackgroundblur.mat" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "70 -37.5", OffsetMax = "150 18" }
            }, "ArenaHUDTop", "OtherTeamScoreBottom");

            container.Add(new CuiElement
            {
                Name = "OtherTeamScoreBottomText",
                Parent = "OtherTeamScoreBottom",
                Components = {
                    new CuiTextComponent { Text = $"{(pInfo.team == PlayerTeams.Spectator ? arenaInfo.Score[PlayerTeams.TeamB].ToString() : arenaInfo.Score[GetOtherTeam(pInfo.team)].ToString())}", Font = "robotocondensed-bold.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-187.8 -200.3", OffsetMax = "-14.6 -88.5" }
            }, "Overlay", "ArenaHUDSidebar");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.009433985 0.009433985 0.009433985 0.8" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-86.6 36.834", OffsetMax = "86.6 55.899" }
            }, "ArenaHUDSidebar", "CurrentArenaTop");

            BasePlayer owner = BasePlayer.Find(ArenaInfo[pInfo.arenaId].arenaOwner);
            container.Add(new CuiElement
            {
                Name = "CurrentArenaText",
                Parent = "CurrentArenaTop",
                Components = {
                    new CuiTextComponent { Text = string.Format("Arena {0} | Owner: {1}", pInfo.arenaId, owner == null ? "No Owner" : owner.displayName), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 4", OffsetMax = "0 -3" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1226415 0.1226415 0.1226415 0.7098039", Material = "assets/content/ui/uibackgroundblur.mat" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-86.6 -55.9", OffsetMax = "86.6 36.834" }
            }, "ArenaHUDSidebar", "CurrentArenaBottom");

            container.Add(new CuiButton
            {
                Button = { Color = "0.03921569 0.03921569 0.03921569 0.6", Command = "hudactions changeteam" },
                Text = { Text = "CHANGE TEAM", Font = "robotocondensed-regular.ttf", FontSize = 7, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-75.825 -29.372", OffsetMax = "76.625 -6.628" }
            }, "CurrentArenaBottom", "ChangeTeamButton");

            container.Add(new CuiButton
            {
                Button = { Color = "0.03921569 0.03921569 0.03921569 0.6", Command = "hudactions settings" },
                Text = { Text = "SETTINGS", Font = "robotocondensed-regular.ttf", FontSize = 7, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-75.825 -8.915", OffsetMax = "76.625 13.829" }
            }, "CurrentArenaBottom", "SettingsButton");

            container.Add(new CuiButton
            {
                Button = { Color = "0.03921569 0.03921569 0.03921569 0.6", Command = "leave" },
                Text = { Text = "LEAVE ARENA", Font = "robotocondensed-regular.ttf", FontSize = 7, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-75.825 10.541", OffsetMax = "76.625 33.286" }
            }, "CurrentArenaBottom", "LeaveArenaButton");

            ReadyUi(player);
            CuiHelper.AddUi(player, container);
        }
        private void ReadyUi(BasePlayer player)
        {
            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo == null) return;

            CuiHelper.DestroyUi(player, "NotReadyBack");
            CuiElementContainer container = new CuiElementContainer();
            if (pInfo.team == PlayerTeams.Spectator)
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0 0.2627451 0.07365482 0.6" },
                    RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-187.805 -231.324", OffsetMax = "-13.995 -205.485" }
                }, "Overlay", "NotReadyBack");

                container.Add(new CuiElement
                {
                    Name = "NotReadyText",
                    Parent = "NotReadyBack",
                    Components = {
                        new CuiTextComponent { Text = "YOU ARE A SPECTATOR", Font = "robotocondensed-bold.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
                    }
                });
            }
            else
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = $"{(pInfo.isReady ? "0 0.2627451 0.07365482 0.6" : "0.2641509 0 0.01034271 0.6")}" },
                    RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-187.805 -231.324", OffsetMax = "-13.995 -205.485" }
                }, "Overlay", "NotReadyBack");

                container.Add(new CuiElement
                {
                    Name = "NotReadyText",
                    Parent = "NotReadyBack",
                    Components = {
                        new CuiTextComponent { Text = $"{(pInfo.isReady ? $"YOU ARE {lang.GetMessage("Ready", this).ToUpper()}" : $"YOU ARE {lang.GetMessage("NotReady", this).ToUpper()} | /r or /ready")}", Font = "robotocondensed-bold.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
                    }
                });
            }

            CuiHelper.AddUi(player, container);
        }
        private void CountdownUi(BasePlayer player, int time)
        {
            if (time == 3)
            {
                player.ChatMessage(GetMessage("RoundStartingIn", player.UserIDString, time.ToString()));
                CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
                }, "Hud", "CountdownTop");

                container.Add(new CuiElement
                {
                    Name = "RoundStartingInText",
                    Parent = "CountdownTop",
                    Components = {
                        new CuiTextComponent { Text = "ROUND STARTING IN:", Font = "robotocondensed-bold.ttf", FontSize = 22, Align = TextAnchor.LowerCenter, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 1", Distance = "1 1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-125.5 -158", OffsetMax = "125.5 -122" }
                    }
                });

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
                }, "Hud", "CountdownBottom");

                container.Add(new CuiElement
                {
                    Name = "RoundCountdownTime",
                    Parent = "CountdownBottom",
                    Components = {
                        new CuiTextComponent { Text = $"{time.ToString()}", Font = "robotocondensed-bold.ttf", FontSize = 22, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 1", Distance = "1 1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-125.5 -198", OffsetMax = "125.5 -162" }
                    }
                });

                CuiHelper.AddUi(player, container);
            }
            else
            {
                CuiHelper.DestroyUi(player, "CountdownBottom"); CuiHelper.DestroyUi(player, "RoundCountdownTime");

                if (time == 0)
                {
                    player.ChatMessage(GetMessage("RoundStarted"));
                    CuiHelper.DestroyUi(player, "CountdownTop"); CuiHelper.DestroyUi(player, "CountdownBottom");
                    if (playerData[player.UserIDString].team != PlayerTeams.Spectator)
                    {
                        CuiHelper.DestroyUi(player, "ArenaHUDTop"); CuiHelper.DestroyUi(player, "ArenaHUDSidebar"); CuiHelper.DestroyUi(player, "NotReadyBack");
                    }
                    CuiElementContainer container = new CuiElementContainer();
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "1 1 1 0" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
                    }, "Hud", "RoundStarted");

                    container.Add(new CuiElement
                    {
                        Name = "RoundStartedText",
                        Parent = "RoundStarted",
                        Components = {
                            new CuiTextComponent { Text = "ROUND STARTED!", Font = "robotocondensed-bold.ttf", FontSize = 22, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                            new CuiOutlineComponent { Color = "0 0 0 1", Distance = "1 1" },
                            new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-125.5 -198", OffsetMax = "125.5 -162" }
                        }
                    });
                    CuiHelper.AddUi(player, container);
                    timer.Once(3f, () =>
                    {
                        if (player == null) return;
                        CuiHelper.DestroyUi(player, "RoundStarted");
                    });
                }
                else
                {
                    player.ChatMessage(GetMessage("RoundStartingIn", player.UserIDString, time.ToString()));
                    CuiElementContainer container = new CuiElementContainer();
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "1 1 1 0" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
                    }, "Hud", "CountdownBottom");

                    container.Add(new CuiElement
                    {
                        Name = "RoundCountdownTime",
                        Parent = "CountdownBottom",
                        Components = {
                            new CuiTextComponent { Text = $"{time.ToString()}", Font = "robotocondensed-bold.ttf", FontSize = 22, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                            new CuiOutlineComponent { Color = "0 0 0 1", Distance = "1 1" },
                            new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-125.5 -198", OffsetMax = "125.5 -162" }
                        }
                    });
                    CuiHelper.AddUi(player, container);
                }
            }
        }
        #endregion

        #region Arena Settings
        private void OpenSettingsMenu(BasePlayer player)
        {
            if (!inArena(player)) return;
            ClearArenaUIs(player, false);

            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.5",
                    Material = "assets/content/ui/uibackgroundblur.mat"
                },

                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                },

                CursorEnabled = true
            }, "Overlay", "ArenaSettings");
            CuiHelper.AddUi(player, container);

            LoadSidebarPanel(player);
            LoadSettingsPanel(player);
        }
        private void LoadSidebarPanel(BasePlayer player)
        {
            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo == null) return;

            CuiHelper.DestroyUi(player, "SidebarPanel");
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.8",
                },

                RectTransform =
                {
                    AnchorMin = "0.08477072 0.09275015",
                    AnchorMax = "0.2683688 0.90725"
                },

                CursorEnabled = false
            }, "Overlay", "SidebarPanel");

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = "SETTINGS MANAGER",
                    FontSize = 24,
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-bold.ttf",
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.05289749 0.9148626",
                    AnchorMax = "0.9449798 0.9664958"
                },
            }, "SidebarPanel", "ArenaSettingsText");

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = $"ARENA {pInfo.arenaId}",
                    FontSize = 24,
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-bold.ttf",
                    Color = "0.04627984 0.6368895 0.754717 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.04910319 0.8614752",
                    AnchorMax = "0.9411855 0.9148632"
                },
            }, "SidebarPanel", "ArenaNumberText");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "switchsettingmenu general",
                    Color = $"{(pInfo.ActiveMenu == 0 ? SidebarPanelActiveColor : SidebarPanelUnactiveColor)}"
                },

                Text =
                {
                    Text = "GENERAL SETTINGS",
                    FontSize = 24,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.05394371 0.7281782",
                    AnchorMax = "0.94604 0.826058"
                },
            }, "SidebarPanel", "GeneralSettingsButton");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "switchsettingmenu loadout",
                    Color = $"{(pInfo.ActiveMenu == 1 ? SidebarPanelActiveColor : SidebarPanelUnactiveColor)}"
                },

                Text =
                {
                    Text = "ARENA LOADOUT",
                    FontSize = 24,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.05289141 0.6141275",
                    AnchorMax = "0.9449877 0.7120072"
                },
            }, "SidebarPanel", "ArenaLoadoutButton");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "switchsettingmenu awhitelist",
                    Color = $"{(pInfo.ActiveMenu == 2 ? SidebarPanelActiveColor : SidebarPanelUnactiveColor)}"
                },

                Text =
                {
                    Text = "ARENA WHITELIST",
                    FontSize = 24,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.05394371 0.4980017",
                    AnchorMax = "0.94604 0.5958815"
                },
            }, "SidebarPanel", "ArenaWhitelistButton");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "switchsettingmenu arenabans",
                    Color = $"{(pInfo.ActiveMenu == 3 ? SidebarPanelActiveColor : SidebarPanelUnactiveColor)}"
                },

                Text =
                {
                    Text = "ARENA BANS",
                    FontSize = 24,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.05393823 0.3772396",
                    AnchorMax = "0.9460345 0.4751194"
                },
            }, "SidebarPanel", "ArenaBansButton");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "switchsettingmenu mapselection",
                    Color = $"{(pInfo.ActiveMenu == 4 ? SidebarPanelActiveColor : SidebarPanelUnactiveColor)}"
                },

                Text =
                {
                    Text = "SWITCH MAP",
                    FontSize = 24,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.05773671 0.2552914",
                    AnchorMax = "0.949833 0.3531712"
                },
            }, "SidebarPanel", "SwitchMapButton");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "destroyui ArenaSettings",
                    Color = "0.1490196 0.1490196 0.1490196 0.8"
                },

                Text =
                {
                    Text = "CLOSE",
                    FontSize = 24,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.05773671 0.0147376",
                    AnchorMax = "0.949833 0.1126174"
                },
            }, "SidebarPanel", "CloseButton");

            CuiHelper.AddUi(player, container);
        }

        private void LoadSettingsPanel(BasePlayer player)
        {
            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo == null) return;

            CuiHelper.DestroyUi(player, "SettingsPanel");
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.5"
                },

                RectTransform =
                {
                    AnchorMin = "0.2683694 0.09275009",
                    AnchorMax = "0.9402989 0.9072499"
                },

                CursorEnabled = true
            }, "Overlay", "SettingsPanel");

            switch (pInfo.ActiveMenu)
            {
                #region General Settings
                case 0:
                    container.Add(new CuiPanel
                    {
                        Image =
                        {
                            Color = "0 0 0 0"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },

                        CursorEnabled = false
                    }, "SettingsPanel", "GeneralSettings");

                    container.Add(new CuiPanel
                    {
                        Image =
                        {
                            Color = "0 0 0 0"
                        },

                        RectTransform =
                        {
                            AnchorMin = "5.079221E-08 0.6956797",
                            AnchorMax = "1 0.9706686"
                        },

                        CursorEnabled = false
                    }, "GeneralSettings", "TogglesSection");

                    container.Add(new CuiLabel
                    {
                        Text =
                        {
                            Text = "TOGGLES",
                            FontSize = 26,
                            Align = TextAnchor.MiddleLeft,
                            Font = "robotocondensed-bold.ttf",
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.02617292 0.7355016",
                            AnchorMax = "0.2699255 0.923266"
                        },
                    }, "TogglesSection", "TogglesText");

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "settingsmenucmds general wounded",
                            Color =
                                $"{(ArenaInfo[pInfo.arenaId].allowPickup ? SettingsToggleActiveColor : SettingsToggleUnactiveColor)}"
                        },

                        Text =
                        {
                            Text = "ALLOW WOUNDED",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.2679779 0.1263065",
                            AnchorMax = "0.4910837 0.3711086"
                        },
                    }, "TogglesSection", "AllowWoundedButton");

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "settingsmenucmds general clearentities",
                            Color =
                                $"{(ArenaInfo[pInfo.arenaId].cleanupEntities ? SettingsToggleActiveColor : SettingsToggleUnactiveColor)}"
                        },

                        Text =
                        {
                            Text = "CLEAR ENTITIES",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.02623856 0.1263065",
                            AnchorMax = "0.2493443 0.3711086"
                        },
                    }, "TogglesSection", "ClearEntitiesButton");

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "settingsmenucmds general scopes",
                            Color =
                                $"{(ArenaInfo[pInfo.arenaId].scopes ? SettingsToggleActiveColor : SettingsToggleUnactiveColor)}"
                        },

                        Text =
                        {
                            Text = "SCOPES",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.7480241 0.428616",
                            AnchorMax = "0.97113 0.6734181"
                        },
                    }, "TogglesSection", "InfiniteAmmoButton");

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "settingsmenucmds general friendlyfire",
                            Color =
                                $"{(ArenaInfo[pInfo.arenaId].friendlyFire ? SettingsToggleActiveColor : SettingsToggleUnactiveColor)}"
                        },

                        Text =
                        {
                            Text = "FRIENDLY FIRE",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.5076741 0.428616",
                            AnchorMax = "0.7307799 0.6734181"
                        },
                    }, "TogglesSection", "FriendlyFireButton");

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "settingsmenucmds general headshot",
                            Color =
                                $"{(ArenaInfo[pInfo.arenaId].onlyHeadshot ? SettingsToggleActiveColor : SettingsToggleUnactiveColor)}"
                        },

                        Text =
                        {
                            Text = "HEADSHOT ONLY",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.2679779 0.428616",
                            AnchorMax = "0.4910837 0.6734181"
                        },
                    }, "TogglesSection", "HeadshotOnlyButton");

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "settingsmenucmds general private",
                            Color =
                                $"{(!ArenaInfo[pInfo.arenaId].isPublic ? SettingsToggleActiveColor : SettingsToggleUnactiveColor)}"
                        },

                        Text =
                        {
                            Text = "PRIVATE ARENA",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.02623856 0.428616",
                            AnchorMax = "0.2493443 0.6734181"
                        },
                    }, "TogglesSection", "PrivateArenaButton");

                    container.Add(new CuiPanel
                    {
                        Image =
                        {
                            Color = "0 0 0 0"
                        },

                        RectTransform =
                        {
                            AnchorMin = "5.079221E-08 0.3840702",
                            AnchorMax = "1 0.6590592"
                        },

                        CursorEnabled = false
                    }, "GeneralSettings", "ActionsSection");

                    container.Add(new CuiLabel
                    {
                        Text =
                        {
                            Text = "ACTIONS",
                            FontSize = 26,
                            Align = TextAnchor.MiddleLeft,
                            Font = "robotocondensed-bold.ttf",
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.02617292 0.7355016",
                            AnchorMax = "0.2699255 0.923266"
                        },
                    }, "ActionsSection", "ActionsText");

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "settingsmenucmds generalact startround",
                            Color = "0.07450981 0.07450981 0.07450981 0.8"
                        },

                        Text =
                        {
                            Text = "START ROUND",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.02623856 0.428616",
                            AnchorMax = "0.2493443 0.6734181"
                        },
                    }, "ActionsSection", "StartRoundButton");

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "settingsmenucmds generalact endround",
                            Color = "0.07450981 0.07450981 0.07450981 0.8"
                        },

                        Text =
                        {
                            Text = "END ROUND",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.2679779 0.428616",
                            AnchorMax = "0.4910837 0.6734181"
                        },
                    }, "ActionsSection", "EndRoundButton");

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "settingsmenucmds generalact restartgame",
                            Color = "0.07450981 0.07450981 0.07450981 0.8"
                        },

                        Text =
                        {
                            Text = "RESTART GAME",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.5076741 0.428616",
                            AnchorMax = "0.7307799 0.6734181"
                        },
                    }, "ActionsSection", "RestartGameButton");

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "settingsmenucmds generalact cleararena",
                            Color = "0.07450981 0.07450981 0.07450981 0.8"
                        },

                        Text =
                        {
                            Text = "CLEAR ARENA",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.7480242 0.428616",
                            AnchorMax = "0.9711299 0.6734181"
                        },
                    }, "ActionsSection", "ClearArenaButton");

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "settingsmenucmds generalact healplayers",
                            Color = "0.07450981 0.07450981 0.07450981 0.8"
                        },

                        Text =
                        {
                            Text = "HEAL PLAYERS",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.02617292 0.1263064",
                            AnchorMax = "0.2492787 0.3711085"
                        },
                    }, "ActionsSection", "HealPlayersButton");

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "settingsmenucmds generalact switchsides",
                            Color = "0.07450981 0.07450981 0.07450981 0.8"
                        },

                        Text =
                        {
                            Text = "SWITCH SIDES",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.267978 0.1263064",
                            AnchorMax = "0.4910837 0.3711085"
                        },
                    }, "ActionsSection", "SwitchSidesButton");

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "settingsmenucmds generalact refreshkits",
                            Color = "0.07450981 0.07450981 0.07450981 0.8"
                        },

                        Text =
                        {
                            Text = "REFRESH KITS",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.5076741 0.1263064",
                            AnchorMax = "0.7307799 0.3711085"
                        },
                    }, "ActionsSection", "RefreshKitsButton");

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "settingsmenucmds generalact transferowner",
                            Color = "0.07450981 0.07450981 0.07450981 0.8"
                        },

                        Text =
                        {
                            Text = "TRANSFER OWNER",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.7480242 0.1263064",
                            AnchorMax = "0.9711299 0.3711085"
                        },
                    }, "ActionsSection", "TransferOwnerButton");

                    container.Add(new CuiPanel
                    {
                        Image =
                        {
                            Color = "0 0 0 0"
                        },

                        RectTransform =
                        {
                            AnchorMin = "2.503135E-07 0.06599142",
                            AnchorMax = "1 0.3409803"
                        },

                        CursorEnabled = false
                    }, "GeneralSettings", "SettingsSection");

                    container.Add(new CuiLabel
                    {
                        Text =
                        {
                            Text = "SETTINGS",
                            FontSize = 26,
                            Align = TextAnchor.MiddleLeft,
                            Font = "robotocondensed-bold.ttf",
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.02617292 0.7355016",
                            AnchorMax = "0.2699255 0.923266"
                        },
                    }, "SettingsSection", "SettingsText");

                    container.Add(new CuiLabel
                    {
                        Text =
                        {
                            Text = "# OF ROUNDS",
                            FontSize = 22,
                            Align = TextAnchor.MiddleLeft,
                            Font = "robotocondensed-bold.ttf",
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.02433019 0.4696574",
                            AnchorMax = "0.169056 0.6574217"
                        },
                    }, "SettingsSection", "NumberOfRoundsText");

                    container.Add(new CuiPanel
                    {
                        Image =
                        {
                            Color = "0 0 0 0"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.0220902 0.2004211",
                            AnchorMax = "0.1690556 0.4452105"
                        },

                        CursorEnabled = false
                    }, "SettingsSection", "RoundCountPanel");

                    container.Add(new CuiPanel
                    {
                        Image =
                        {
                            Color = "0.1037736 0.1037736 0.1037736 0.6"
                        },

                        RectTransform =
                        {
                            AnchorMin = "2.245131E-05 -2.183435E-07",
                            AnchorMax = "1.000021 0.9999999"
                        },

                        CursorEnabled = false
                    }, "RoundCountPanel", "RoundCountBackground");

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "settingsmenucmds generalset rounddown",
                            Color = "0.0754717 0.0754717 0.0754717 1"
                        },

                        Text =
                        {
                            Text = "<",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0.2329492 1"
                        },
                    }, "RoundCountBackground", "PrevRoundCountButton");

                    container.Add(new CuiLabel
                    {
                        Text =
                        {
                            Text = $"{ArenaInfo[pInfo.arenaId].roundCount}",
                            FontSize = 24,
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-bold.ttf",
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.2692792 0.1164699",
                            AnchorMax = "0.7204914 0.8835144"
                        },
                    }, "RoundCountBackground", "RoundCountText");

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "settingsmenucmds generalset roundup",
                            Color = "0.0754717 0.0754717 0.0754717 1"
                        },

                        Text =
                        {
                            Text = ">",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.7670681 0",
                            AnchorMax = "1 1"
                        },
                    }, "RoundCountBackground", "NextRoundCountButton");

                    container.Add(new CuiLabel
                    {
                        Text =
                        {
                            Text = "TEAM A NAME",
                            FontSize = 26,
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-bold.ttf",
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.2140438 0.4696574",
                            AnchorMax = "0.4874052 0.6574217"
                        },
                    }, "SettingsSection", "TeamANameText");

                    container.Add(new CuiPanel
                    {
                        Image =
                        {
                            Color = "0.07450981 0.07450981 0.07450981 0.8"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.2140422 0.1971667",
                            AnchorMax = "0.4874041 0.4419561"
                        },

                        CursorEnabled = false
                    }, "SettingsSection", "TeamANamePanel");

                    container.Add(new CuiElement
                    {
                        Name = "TeamANameInputField",
                        Parent = "TeamANamePanel",
                        Components =
                        {
                            new CuiInputFieldComponent
                            {
                                Text = $"{ArenaInfo[pInfo.arenaId].TeamNames[PlayerTeams.TeamA]}",
                                CharsLimit = 8,
                                Color = "1 1 1 1",
                                IsPassword = false,
                                Command = "rustarenainput TeamAName",
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 12,
                                Align = TextAnchor.MiddleCenter,
                                NeedsKeyboard = true
                            },

                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "1 1"
                            }
                        }
                    });

                    container.Add(new CuiLabel
                    {
                        Text =
                        {
                            Text = "TEAM B NAME",
                            FontSize = 26,
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-bold.ttf",
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.5323584 0.47201",
                            AnchorMax = "0.8057199 0.6597743"
                        },
                    }, "SettingsSection", "TeamBName");

                    container.Add(new CuiPanel
                    {
                        Image =
                        {
                            Color = "0.07450981 0.07450981 0.07450981 0.8"
                        },

                        RectTransform =
                        {
                            AnchorMin = "0.5323567 0.1995192",
                            AnchorMax = "0.8057187 0.4443087"
                        },

                        CursorEnabled = false
                    }, "SettingsSection", "TeamBNamePanel");

                    container.Add(new CuiElement
                    {
                        Name = "TeamBNameInputField",
                        Parent = "TeamBNamePanel",
                        Components =
                        {
                            new CuiInputFieldComponent
                            {
                                Text = $"{ArenaInfo[pInfo.arenaId].TeamNames[PlayerTeams.TeamB]}",
                                CharsLimit = 8,
                                Color = "1 1 1 1",
                                IsPassword = false,
                                Command = "rustarenainput TeamBName",
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 12,
                                Align = TextAnchor.MiddleCenter,
                                NeedsKeyboard = true
                            },

                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "1 1"
                            }
                        }
                    });
                    break;
                    #endregion
            }

            CuiHelper.AddUi(player, container);

            // This has to be after UI as its being layered on-top
            switch (pInfo.ActiveMenu)
            {
                case 1:
                    LoadWeapons(player);
                    LoadArmor(player);
                    LoadMiscItems(player);
                    break;

                case 2:
                    LoadArenaWhitelist(player);
                    LoadArenaWhitelist(player, false);
                    break;

                case 3:
                    LoadArenaBans(player);
                    LoadArenaBans(player, false);
                    break;

                case 4:
                    LoadSwitchArena(player);
                    break;
            }
        }
        #endregion

        #region Load Kit Panels
        private void LoadWeapons(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "WeaponsPanel");
            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo == null) return;

            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },

                RectTransform =
                {
                    AnchorMin = "0.2683702 0.5838153",
                    AnchorMax = "0.9402973 0.8976253"
                },

                CursorEnabled = true
            }, "Overlay", "WeaponsPanel");

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = "WEAPONS",
                    FontSize = 26,
                    Align = TextAnchor.MiddleLeft,
                    Font = "robotocondensed-bold.ttf",
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.02617292 0.8132312",
                    AnchorMax = "0.2699255 0.9458264"
                },
            }, "WeaponsPanel", "WeaponsText");

            int index = 0; float minx = 0.026f; float miny = 0.677f; float maxx = 0.198f; float maxy = 0.782f;
            foreach (WeaponKit item in config.kitSettings.weaponSettings.WeaponKits)
            {
                if (index == 25) return;

                if (index != 0)
                {
                    int[] resetIndexes = new[] { 5, 10, 15, 20, 25 };
                    if (resetIndexes.Contains(index))
                    {
                        minx = 0.026f; maxx = 0.198f; miny -= 0.150f; maxy -= 0.150f;
                    }
                    else
                    {
                        minx += 0.196f; maxx += 0.196f;
                    }
                }

                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"settingsmenucmds loadoutset {index} Weapons",
                        Color = $"{GetActiveKit(player, ArmorType.None, index)}"
                    },

                    Text =
                    {
                        Text = $"{item.KitName}",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = $"{minx + " " + miny}",
                        AnchorMax = $"{maxx + " " + maxy}",
                    },
                }, "WeaponsPanel", "KitButton");

                index++;
            }

            CuiHelper.AddUi(player, container);
        }

        private void LoadArmor(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "ArmorPanel");
            var pInfo = playerData[player.UserIDString];
            if (pInfo == null) return;

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },

                RectTransform =
                {
                    AnchorMin = "0.2683703 0.2623865",
                    AnchorMax = "0.9402974 0.5761966"
                },

                CursorEnabled = true
            }, "Overlay", "ArmorPanel");

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = "ARMOR",
                    FontSize = 26,
                    Align = TextAnchor.MiddleLeft,
                    Font = "robotocondensed-bold.ttf",
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.02617292 0.8132312",
                    AnchorMax = "0.2699255 0.9458264"
                },
            }, "ArmorPanel", "ArmorText");

            var overallindex = 0; var headgearindex = 0; var chestindex = 0; var glovesindex = 0; var pantsindex = 0; var bootsindex = 0;
            var minx = 0.026f; var miny = 0.677f; var maxx = 0.198f; var maxy = 0.782f;

            foreach (var item in config.kitSettings.attireSettings.ArmorKits)
            {
                switch (item.armorType)
                {
                    case ArmorType.Headgear:
                        if (headgearindex != 0)
                        {
                            minx += 0.196f; maxx += 0.196f;
                        }
                        break;
                    case ArmorType.Chest:
                        if (chestindex == 0)
                        {
                            minx = 0.026f; maxx = 0.198f; miny -= 0.150f; maxy -= 0.150f;
                        }
                        else
                        {
                            minx += 0.196f; maxx += 0.196f;
                        }
                        break;
                    case ArmorType.Gloves:
                        if (glovesindex == 0)
                        {
                            minx = 0.026f; maxx = 0.198f; miny -= 0.150f; maxy -= 0.150f;
                        }
                        else
                        {
                            minx += 0.196f; maxx += 0.196f;
                        }
                        break;
                    case ArmorType.Pants:
                        if (pantsindex == 0)
                        {
                            minx = 0.026f; maxx = 0.198f; miny -= 0.150f; maxy -= 0.150f;
                        }
                        else
                        {
                            minx += 0.196f; maxx += 0.196f;
                        }
                        break;
                    case ArmorType.Boots:
                        if (bootsindex == 0)
                        {
                            minx = 0.026f; maxx = 0.198f; miny -= 0.150f; maxy -= 0.150f;
                        }
                        else
                        {
                            minx += 0.196f; maxx += 0.196f;
                        }
                        break;
                }



                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"settingsmenucmds loadoutset {overallindex} {item.armorType}",
                        Color = $"{GetActiveKit(player, item.armorType, overallindex)}"
                    },

                    Text =
                    {
                        Text = $"{item.armorName}",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = $"{minx + " " + miny}",
                        AnchorMax = $"{maxx + " " + maxy}",
                    },
                }, "ArmorPanel", "KitButton");

                switch (item.armorType)
                {
                    case ArmorType.Headgear:
                        headgearindex++;
                        break;
                    case ArmorType.Chest:
                        chestindex++;
                        break;
                    case ArmorType.Gloves:
                        glovesindex++;
                        break;
                    case ArmorType.Pants:
                        pantsindex++;
                        break;
                    case ArmorType.Boots:
                        bootsindex++;
                        break;
                }

                overallindex++;
            }

            CuiHelper.AddUi(player, container);
        }

        private void LoadMiscItems(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "MiscItemsPanel");
            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo == null) return;

            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },

                RectTransform =
                {
                    AnchorMin = "0.2683703 0.09274978",
                    AnchorMax = "0.9402974 0.2623903"
                },

                CursorEnabled = true
            }, "Overlay", "MiscItemsPanel");

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = "MISCELLANEOUS",
                    FontSize = 15,
                    Align = TextAnchor.MiddleLeft,
                    Font = "robotocondensed-bold.ttf",
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.02617292 0.8132312",
                    AnchorMax = "0.2699255 0.9458264"
                },
            }, "MiscItemsPanel", "ArmorText");

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = "MED SYRINGE",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-bold.ttf",
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.02522888 0.4659593",
                    AnchorMax = "0.1699547 0.7138668"
                },
            }, "MiscItemsPanel", "NumberOfSyringesText");

            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },

                RectTransform =
                {
                    AnchorMin = "0.02298889 0.1104827",
                    AnchorMax = "0.1699543 0.433681"
                },

                CursorEnabled = false
            }, "MiscItemsPanel", "SyringeCountPanel");

            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0.1037736 0.1037736 0.1037736 0.6"
                },

                RectTransform =
                {
                    AnchorMin = "2.245131E-05 -2.183435E-07",
                    AnchorMax = "1.000021 0.9999999"
                },

                CursorEnabled = false
            }, "SyringeCountPanel", "SyringeCountBackground");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "settingsmenucmds loadoutset -1 Syringes down",
                    Color = "0.0754717 0.0754717 0.0754717 0.8"
                },

                Text =
                {
                    Text = "<",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "1.558244E-05 1.222615E-06",
                    AnchorMax = "0.2329492 1.000053"
                },
            }, "SyringeCountBackground", "PrevSyringeCountButton");

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = $"{ArenaInfo[pInfo.arenaId].ActiveKit.Syrimges}",
                    FontSize = 24,
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-bold.ttf",
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.2692792 0.1164699",
                    AnchorMax = "0.7204914 0.8835144"
                },
            }, "SyringeCountBackground", "SyringeCountText");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "settingsmenucmds loadoutset -1 Syringes up",
                    Color = "0.0754717 0.0754717 0.0754717 0.8"
                },

                Text =
                {
                    Text = ">",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.7670681 1.23789E-06",
                    AnchorMax = "1.000002 1.000053"
                },
            }, "SyringeCountBackground", "NextSyringeCountButton");

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = "MEDKIT",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-bold.ttf",
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.1842882 0.4652556",
                    AnchorMax = "0.3312532 0.7131631"
                },
            }, "MiscItemsPanel", "NumberOfMedkitsText");

            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },

                RectTransform =
                {
                    AnchorMin = "0.1842874 0.1097789",
                    AnchorMax = "0.3312528 0.4329772"
                },

                CursorEnabled = false
            }, "MiscItemsPanel", "MedkitCountPanel");

            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0.1037736 0.1037736 0.1037736 0.6"
                },

                RectTransform =
                {
                    AnchorMin = "2.245131E-05 -2.183435E-07",
                    AnchorMax = "1.000021 0.9999999"
                },

                CursorEnabled = false
            }, "MedkitCountPanel", "MedkitCountBackground");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "settingsmenucmds loadoutset -1 Medkits down",
                    Color = "0.0754717 0.0754717 0.0754717 0.8"
                },

                Text =
                {
                    Text = "<",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "1.558244E-05 1.222615E-06",
                    AnchorMax = "0.2329492 1.000053"
                },
            }, "MedkitCountBackground", "PrevMedkitCountButton");

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = $"{ArenaInfo[pInfo.arenaId].ActiveKit.Medkits}",
                    FontSize = 24,
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-bold.ttf",
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.2692792 0.1164699",
                    AnchorMax = "0.7204914 0.8835144"
                },
            }, "MedkitCountBackground", "MedkitCountText");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "settingsmenucmds loadoutset -1 Medkits up",
                    Color = "0.0754717 0.0754717 0.0754717 0.8"
                },

                Text =
                {
                    Text = ">",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.7670681 1.23789E-06",
                    AnchorMax = "1.000002 1.000053"
                },
            }, "MedkitCountBackground", "NextMedkitCountButton");

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = "BANDAGE",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-bold.ttf",
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.3439195 0.4623351",
                    AnchorMax = "0.4908845 0.7102425"
                },
            }, "MiscItemsPanel", "NumberOfBandagesText");

            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },

                RectTransform =
                {
                    AnchorMin = "0.3439187 0.1068586",
                    AnchorMax = "0.4908841 0.4300569"
                },

                CursorEnabled = false
            }, "MiscItemsPanel", "BandageCountPanel");

            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0.1037736 0.1037736 0.1037736 0.6"
                },

                RectTransform =
                {
                    AnchorMin = "2.245131E-05 -2.183435E-07",
                    AnchorMax = "1.000021 0.9999999"
                },

                CursorEnabled = false
            }, "BandageCountPanel", "BandageCountBackground");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "settingsmenucmds loadoutset -1 Bandages down",
                    Color = "0.0754717 0.0754717 0.0754717 0.8"
                },

                Text =
                {
                    Text = "<",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "1.558244E-05 1.222615E-06",
                    AnchorMax = "0.2329492 1.000053"
                },
            }, "BandageCountBackground", "PrevBandageCountButton");

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = $"{ArenaInfo[pInfo.arenaId].ActiveKit.Bandages}",
                    FontSize = 24,
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-bold.ttf",
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.2692792 0.1164699",
                    AnchorMax = "0.7204914 0.8835144"
                },
            }, "BandageCountBackground", "BandageCountText");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "settingsmenucmds loadoutset -1 Bandages up",
                    Color = "0.0754717 0.0754717 0.0754717 0.8"
                },

                Text =
                {
                    Text = ">",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.7670681 1.23789E-06",
                    AnchorMax = "1.000002 1.000053"
                },
            }, "BandageCountBackground", "NextBandageCountButton");

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = "WOOD WALL",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-bold.ttf",
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.5077515 0.461114",
                    AnchorMax = "0.6547166 0.7090214"
                },
            }, "MiscItemsPanel", "NumberOfWoodWallText");

            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },

                RectTransform =
                {
                    AnchorMin = "0.5077506 0.1056377",
                    AnchorMax = "0.6547161 0.428836"
                },

                CursorEnabled = false
            }, "MiscItemsPanel", "WoodWallCountPanel");

            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0.1037736 0.1037736 0.1037736 0.6"
                },

                RectTransform =
                {
                    AnchorMin = "2.245131E-05 -2.183435E-07",
                    AnchorMax = "1.000021 0.9999999"
                },

                CursorEnabled = false
            }, "WoodWallCountPanel", "WoodWallCountBackground");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "settingsmenucmds loadoutset -1 WoodWall down",
                    Color = "0.0754717 0.0754717 0.0754717 0.8"
                },

                Text =
                {
                    Text = "<",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "1.558244E-05 1.222615E-06",
                    AnchorMax = "0.2329492 1.000053"
                },
            }, "WoodWallCountBackground", "PrevWoodWallCountButton");

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = $"{ArenaInfo[pInfo.arenaId].ActiveKit.WoodWalls}",
                    FontSize = 24,
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-bold.ttf",
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.2692792 0.1164699",
                    AnchorMax = "0.7204914 0.8835144"
                },
            }, "WoodWallCountBackground", "WoodWallCountText");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "settingsmenucmds loadoutset -1 WoodWall up",
                    Color = "0.0754717 0.0754717 0.0754717 0.8"
                },

                Text =
                {
                    Text = ">",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.7670681 1.23789E-06",
                    AnchorMax = "1.000002 1.000053"
                },
            }, "WoodWallCountBackground", "NextWoodWallCountButton");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Load Whitelist & Bans Panels
        void LoadArenaWhitelist(BasePlayer player, bool whitelist = true, int page = 1)
        {
            RustArenaPlayer pInfo = playerData[player.UserIDString];

            int pageCount = 1;
            if (whitelist)
                pageCount = GetPageCount(60, player);
            else
                pageCount = GetPageCount(60, player);

            int pagemin = ((page * 35) - 35);
            int pagemax = (page * 35) - 1;
            CuiElementContainer container = new CuiElementContainer();
            if (whitelist)
            {
                CuiHelper.DestroyUi(player, "WhitelistPanel");
                container.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0 0 0 0"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.2683702 0.4950959",
                        AnchorMax = "0.9402973 0.8976224"
                    },

                    CursorEnabled = true
                }, "Overlay", "WhitelistPanel");

                container.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = "WHITELIST PLAYER",
                        FontSize = 26,
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-bold.ttf",
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.02617292 0.8544021",
                        AnchorMax = "0.2699255 0.9577734"
                    },
                }, "WhitelistPanel", "WeaponsText");

                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = string.Format("settingsmenucmds changepage whitelist {0}", page == 1 ? page : page - 1),
                        Color = "0.04313726 0.4156863 0.4901961 1"
                    },

                    Text =
                    {
                        Text = "<",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.9006123 0.8759154",
                        AnchorMax = "0.9353557 0.9577735"
                    },
                }, "WhitelistPanel", "PrevPageButton");

                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = string.Format("settingsmenucmds changepage whitelist {0}", page == pageCount ? page : page + 1),
                        Color = "0.04313726 0.4156863 0.4901961 1"
                    },

                    Text =
                    {
                        Text = ">",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.9452134 0.8759154",
                        AnchorMax = "0.9799569 0.9577735"
                    },
                }, "WhitelistPanel", "NextPageButton");

                pInfo.WLPage = page;
            }
            else if (!whitelist)
            {
                CuiHelper.DestroyUi(player, "DeWhitelistPanel");
                container.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0 0 0 0"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.2683703 0.09275256",
                        AnchorMax = "0.9402974 0.4950953"
                    },

                    CursorEnabled = true
                }, "Overlay", "DeWhitelistPanel");

                container.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = "DEWHITELIST PLAYER",
                        FontSize = 26,
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-bold.ttf",
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.02617292 0.8544021",
                        AnchorMax = "0.2699255 0.9577734"
                    },
                }, "DeWhitelistPanel", "DeWhitelistText");

                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = string.Format("settingsmenucmds changepage dewhitelist {0}", page == 1 ? page : page - 1),
                        Color = "0.04313726 0.4156863 0.4901961 1"
                    },

                    Text =
                    {
                        Text = "<",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.9006123 0.8759154",
                        AnchorMax = "0.9353557 0.9577735"
                    },
                }, "DeWhitelistPanel", "PrevPageButton");

                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = string.Format("settingsmenucmds changepage dewhitelist {0}", page == pageCount ? page : page + 1),
                        Color = "0.04313726 0.4156863 0.4901961 1"
                    },

                    Text =
                    {
                        Text = ">",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.9452134 0.8759154",
                        AnchorMax = "0.9799569 0.9577735"
                    },
                }, "DeWhitelistPanel", "NextPageButton");

                pInfo.DWLPage = page;
            }

            int index = 0; float minx = 0.026f; float miny = 0.748f; float maxx = 0.198f; float maxy = 0.83f;
            if (whitelist)
            {
                foreach (BasePlayer plr in BasePlayer.activePlayerList)
                {
                    if (index < pagemin || index > pagemax) { index++; continue; };
                    if (plr.UserIDString == player.UserIDString || ArenaInfo[pInfo.arenaId].ArenaWhitelistedIDs.Contains(plr.UserIDString)) continue;

                    if (index != pagemin)
                    {

                        decimal d = ((decimal)index / 5.0m);
                        bool isInt = d % 1 == 0;
                        if (isInt)
                        {
                            minx = 0.026f; maxx = 0.198f; miny -= 0.114f; maxy -= 0.114f;
                        }
                        else
                        {
                            minx += 0.196f; maxx += 0.196f;
                        }
                    }

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"settingsmenucmds awhitelist {plr.UserIDString} true",
                            Color = "0.07450981 0.07450981 0.07450981 0.8"
                        },

                        Text =
                        {
                            Text = $"{plr.displayName}",
                            FontSize = 12,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = $"{minx + " " + miny}",
                            AnchorMax = $"{maxx + " " + maxy}",
                        },
                    }, "WhitelistPanel", "WLPlayerButton");

                    index++;
                }
            }
            else
            {
                foreach (string pid in ArenaInfo[pInfo.arenaId].ArenaWhitelistedIDs)
                {
                    if (index < pagemin || index > pagemax) { index++; continue; };
                    if (pid == player.UserIDString) continue;

                    BasePlayer plr = BasePlayer.Find(pid);
                    if (plr == null) { ArenaInfo[pInfo.arenaId].ArenaWhitelistedIDs.Remove(pid); continue; };

                    if (index != pagemin)
                    {

                        decimal d = ((decimal)index / 5.0m);
                        bool isInt = d % 1 == 0;
                        if (isInt)
                        {
                            minx = 0.026f; maxx = 0.198f; miny -= 0.114f; maxy -= 0.114f;
                        }
                        else
                        {
                            minx += 0.196f; maxx += 0.196f;
                        }
                    }

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"settingsmenucmds awhitelist {plr.UserIDString} false",
                            Color = "0.07450981 0.07450981 0.07450981 0.8"
                        },

                        Text =
                        {
                            Text = $"{plr.displayName}",
                            FontSize = 12,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = $"{minx + " " + miny}",
                            AnchorMax = $"{maxx + " " + maxy}",
                        },
                    }, "DeWhitelistPanel", "DWLPlayerButton");

                    index++;
                }
            }

            CuiHelper.AddUi(player, container);
        }

        void LoadTeamWhitelist(BasePlayer player, bool whitelist = true, int page = 1, PlayerTeams team = PlayerTeams.None)
        {
            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo == null) return;

            int pageCount = 1;
            if (whitelist)
                pageCount = GetPageCount(60, player);
            else
                pageCount = GetPageCount(60, player);

            int pagemin = ((page * 35) - 35); int pagemax = (page * 35) - 1;
            CuiElementContainer container = new CuiElementContainer();
            if (whitelist)
            {
                CuiHelper.DestroyUi(player, "TeamWhitelistPanel");
                container.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0 0 0 0"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.2683702 0.4950959",
                        AnchorMax = "0.9402973 0.8976224"
                    },

                    CursorEnabled = true
                }, "Overlay", "TeamWhitelistPanel");

                container.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = "WHITELIST PLAYER",
                        FontSize = 26,
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-bold.ttf",
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.02617292 0.8544021",
                        AnchorMax = "0.2699255 0.9577734"
                    },
                }, "TeamWhitelistPanel", "WLPlayerText");

                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = "settingsmenucmds changeselteam 0",
                        Color = string.Format("{0}", pInfo.selectedTeam == PlayerTeams.TeamA ? KitActiveColor : KitUnactiveColor)
                    },

                    Text =
                    {
                        Text = "TEAM A",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.7240908 0.8759154",
                        AnchorMax = "0.7981032 0.9577735"
                    },
                }, "TeamWhitelistPanel", "TeamAButton");

                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = "settingsmenucmds changeselteam 1",
                        Color = string.Format("{0}", pInfo.selectedTeam == PlayerTeams.TeamB ? KitActiveColor : KitUnactiveColor)
                    },

                    Text =
                    {
                        Text = "TEAM B",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.8078069 0.8743599",
                        AnchorMax = "0.881819 0.9577734"
                    },
                }, "TeamWhitelistPanel", "TeamBButton");

                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = string.Format("settingsmenucmds changepage twhitelist {0}", page == 1 ? page : page - 1),
                        Color = "0.04313726 0.4156863 0.4901961 1"
                    },

                    Text =
                    {
                        Text = "<",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.9006123 0.8759154",
                        AnchorMax = "0.9353557 0.9577735"
                    },
                }, "TeamWhitelistPanel", "PrevPageButton");

                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = string.Format("settingsmenucmds changepage twhitelist {0}", page == pageCount ? page : page + 1),
                        Color = "0.04313726 0.4156863 0.4901961 1"
                    },

                    Text =
                    {
                        Text = ">",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.9452134 0.8759154",
                        AnchorMax = "0.9799569 0.9577735"
                    },
                }, "TeamWhitelistPanel", "NextPageButton");

                pInfo.TWLPage = page;
            }
            else
            {
                CuiHelper.DestroyUi(player, "TeamDeWhitelistPanel");
                container.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0 0 0 0"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.2683703 0.09275256",
                        AnchorMax = "0.9402974 0.4950953"
                    },

                    CursorEnabled = true
                }, "Overlay", "TeamDeWhitelistPanel");

                container.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = "DEWHITELIST PLAYER",
                        FontSize = 26,
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-bold.ttf",
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.02617292 0.8544021",
                        AnchorMax = "0.2699255 0.9577734"
                    },
                }, "TeamDeWhitelistPanel", "DeWhitelistText");

                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = string.Format("settingsmenucmds changepage tdwhitelist {0}", page == 1 ? page : page - 1),
                        Color = "0.04313726 0.4156863 0.4901961 1"
                    },

                    Text =
                    {
                        Text = "<",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.9006123 0.8759154",
                        AnchorMax = "0.9353557 0.9577735"
                    },
                }, "TeamDeWhitelistPanel", "PrevPageButton");

                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = string.Format("settingsmenucmds changepage tdwhitelist {0}", page == pageCount ? page : page + 1),
                        Color = "0.04313726 0.4156863 0.4901961 1"
                    },

                    Text =
                    {
                        Text = ">",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.9452134 0.8759154",
                        AnchorMax = "0.9799569 0.9577735"
                    },
                }, "TeamDeWhitelistPanel", "NextPageButton");

                pInfo.TDWLPage = page;
            }

            int index = 0; float minx = 0.026f; float miny = 0.748f; float maxx = 0.198f; float maxy = 0.83f;
            if (whitelist)
            {
                foreach (BasePlayer plr in PlayersInArena(pInfo.arenaId, true))
                {
                    if (index < pagemin || index > pagemax) { index++; continue; };
                    if (ArenaInfo[pInfo.arenaId].TeamWhitelist[team].Contains(plr.UserIDString) || plr.UserIDString == ArenaInfo[pInfo.arenaId].arenaOwner) continue;

                    if (index != pagemin)
                    {

                        decimal d = ((decimal)index / 5.0m);
                        bool isInt = d % 1 == 0;
                        if (isInt)
                        {
                            minx = 0.026f; maxx = 0.198f; miny -= 0.114f; maxy -= 0.114f;
                        }
                        else
                        {
                            minx += 0.196f; maxx += 0.196f;
                        }
                    }

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"settingsmenucmds twhitelist {plr.UserIDString} true {(int)pInfo.selectedTeam}",
                            Color = "0.07450981 0.07450981 0.07450981 0.8"
                        },

                        Text =
                        {
                            Text = $"{plr.displayName}",
                            FontSize = 12,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = $"{minx + " " + miny}",
                            AnchorMax = $"{maxx + " " + maxy}",
                        },
                    }, "TeamWhitelistPanel", "WLPlayerButton");

                    index++;
                }
            }
            else
            {
                foreach (string pid in ArenaInfo[pInfo.arenaId].TeamWhitelist[team])
                {
                    if (index < pagemin || index > pagemax) { index++; continue; };

                    string display = "";
                    BasePlayer plr = BasePlayer.Find(pid);
                    if (plr == null) display = pid; else display = plr.displayName;

                    if (index != pagemin)
                    {

                        decimal d = ((decimal)index / 5.0m);
                        bool isInt = d % 1 == 0;
                        if (isInt)
                        {
                            minx = 0.026f; maxx = 0.198f; miny -= 0.114f; maxy -= 0.114f;
                        }
                        else
                        {
                            minx += 0.196f; maxx += 0.196f;
                        }
                    }

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"settingsmenucmds twhitelist {plr.UserIDString} false {(int)pInfo.selectedTeam}",
                            Color = "0.07450981 0.07450981 0.07450981 0.8"
                        },

                        Text =
                        {
                            Text = display,
                            FontSize = 12,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = $"{minx + " " + miny}",
                            AnchorMax = $"{maxx + " " + maxy}",
                        },
                    }, "TeamDeWhitelistPanel", "DeWLPlayerButton");

                    index++;
                }
            }

            CuiHelper.AddUi(player, container);
        }

        void LoadArenaBans(BasePlayer player, bool banned = true, int page = 1)
        {
            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo == null) return;

            int pageCount = 1;
            if (banned)
                pageCount = GetPageCount(60, player);
            else
                pageCount = GetPageCount(60, player);

            int pagemin = ((page * 35) - 35); int pagemax = (page * 35) - 1;
            CuiElementContainer container = new CuiElementContainer();
            if (banned)
            {
                CuiHelper.DestroyUi(player, "ArenaBanPanel");
                container.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0 0 0 0"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.2683702 0.4950959",
                        AnchorMax = "0.9402973 0.8976224"
                    },

                    CursorEnabled = true
                }, "Overlay", "ArenaBanPanel");

                container.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = "BAN PLAYER",
                        FontSize = 26,
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-bold.ttf",
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.02617292 0.8544021",
                        AnchorMax = "0.2699255 0.9577734"
                    },
                }, "ArenaBanPanel", "BanPlayerText");

                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = string.Format("settingsmenucmds changepage ban {0}", page == 1 ? page : page - 1),
                        Color = "0.04313726 0.4156863 0.4901961 1"
                    },

                    Text =
                    {
                        Text = "<",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.9006123 0.8759154",
                        AnchorMax = "0.9353557 0.9577735"
                    },
                }, "ArenaBanPanel", "PrevPageButton");

                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = string.Format("settingsmenucmds changepage ban {0}", page == pageCount ? page : page + 1),
                        Color = "0.04313726 0.4156863 0.4901961 1"
                    },

                    Text =
                    {
                        Text = ">",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.9452134 0.8759154",
                        AnchorMax = "0.9799569 0.9577735"
                    },
                }, "ArenaBanPanel", "NextPageButton");

                pInfo.ABPage = page;
            }
            else
            {
                CuiHelper.DestroyUi(player, "ArenaUnbanPanel");
                container.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0 0 0 0"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.2683703 0.09275256",
                        AnchorMax = "0.9402974 0.4950953"
                    },

                    CursorEnabled = true
                }, "Overlay", "ArenaUnbanPanel");

                container.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = "UNBAN PLAYER",
                        FontSize = 26,
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-bold.ttf",
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.02617292 0.8544021",
                        AnchorMax = "0.2699255 0.9577734"
                    },
                }, "ArenaUnbanPanel", "UnbanText");

                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = string.Format("settingsmenucmds changepage unban {0}", page == 1 ? page : page - 1),
                        Color = "0.04313726 0.4156863 0.4901961 1"
                    },

                    Text =
                    {
                        Text = "<",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.9006123 0.8759154",
                        AnchorMax = "0.9353557 0.9577735"
                    },
                }, "ArenaUnbanPanel", "PrevPageButton");

                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = string.Format("settingsmenucmds changepage unban {0}", page == pageCount ? page : page + 1),
                        Color = "0.04313726 0.4156863 0.4901961 1"
                    },

                    Text =
                    {
                        Text = ">",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.9452134 0.8759154",
                        AnchorMax = "0.9799569 0.9577735"
                    },
                }, "ArenaUnbanPanel", "NextPageButton");

                pInfo.AUBPage = page;
            }

            int index = 0; float minx = 0.026f; float miny = 0.748f; float maxx = 0.198f; float maxy = 0.83f;
            if (banned)
            {
                foreach (BasePlayer plr in PlayersInArena(pInfo.arenaId, true))
                {
                    if (index < pagemin || index > pagemax) { index++; continue; };
                    if (plr.UserIDString == player.UserIDString || ArenaInfo[pInfo.arenaId].ArenaBannedIDs.Contains(plr.UserIDString)) continue;

                    if (index != pagemin)
                    {

                        decimal d = ((decimal)index / 5.0m);
                        bool isInt = d % 1 == 0;
                        if (isInt)
                        {
                            minx = 0.026f; maxx = 0.198f; miny -= 0.114f; maxy -= 0.114f;
                        }
                        else
                        {
                            minx += 0.196f; maxx += 0.196f;
                        }
                    }

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"settingsmenucmds abans {plr.UserIDString} true",
                            Color = "0.07450981 0.07450981 0.07450981 0.8"
                        },

                        Text =
                        {
                            Text = $"{plr.displayName}",
                            FontSize = 12,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = $"{minx + " " + miny}",
                            AnchorMax = $"{maxx + " " + maxy}",
                        },
                    }, "ArenaBanPanel", "BanPlayerButton");

                    index++;
                }
            }
            else
            {
                foreach (string pid in ArenaInfo[pInfo.arenaId].ArenaBannedIDs)
                {
                    if (index < pagemin || index > pagemax) { index++; continue; };
                    if (pid == player.UserIDString) continue;

                    string display = "";
                    BasePlayer plr = BasePlayer.Find(pid);
                    if (plr == null) display = pid; else display = plr.displayName;

                    if (index != pagemin)
                    {

                        decimal d = ((decimal)index / 5.0m);
                        bool isInt = d % 1 == 0;
                        if (isInt)
                        {
                            minx = 0.026f; maxx = 0.198f; miny -= 0.114f; maxy -= 0.114f;
                        }
                        else
                        {
                            minx += 0.196f; maxx += 0.196f;
                        }
                    }

                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"settingsmenucmds abans {plr.UserIDString} false",
                            Color = "0.07450981 0.07450981 0.07450981 0.8"
                        },

                        Text =
                        {
                            Text = display,
                            FontSize = 12,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },

                        RectTransform =
                        {
                            AnchorMin = $"{minx + " " + miny}",
                            AnchorMax = $"{maxx + " " + maxy}",
                        },
                    }, "ArenaUnbanPanel", "UnbanPlayerButton");

                    index++;
                }
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Switch Arena Panel
        void LoadSwitchArena(BasePlayer player, int page = 1)
        {
            CuiHelper.DestroyUi(player, "SwitchMapPanel");
            RustArenaPlayer pInfo = playerData[player.UserIDString];

            CuiElementContainer container = new CuiElementContainer();
            if (_save.ArenaMaps.Count == 0)
            {
                container.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0 0 0 0.3411765"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.2683702 0.09275009",
                        AnchorMax = "0.9402973 0.9072499"
                    },

                    CursorEnabled = true
                }, "Overlay", "SwitchMapPanel");

                container.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = GetMessage("NoArenaToSwitch"),
                        FontSize = 30,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.09262161 0.4008802",
                        AnchorMax = "0.9073757 0.5991111"
                    },
                }, "SwitchMapPanel", "NoArenasText");

                CuiHelper.AddUi(player, container);
                return;
            }

            int pageCount = GetPageCount(_save.ArenaMaps.Count, player);
            int pagemin = ((page * 10) - 10); int pagemax = (page * 10) - 1;
            pInfo.SAPage = page;

            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.3411765"
                },

                RectTransform =
                {
                    AnchorMin = "0.2683702 0.09275009",
                    AnchorMax = "0.9402973 0.9072499"
                },

                CursorEnabled = true
            }, "Overlay", "SwitchMapPanel");

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = "MAPS",
                    FontSize = 26,
                    Align = TextAnchor.MiddleLeft,
                    Font = "robotocondensed-bold.ttf",
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.02617292 0.9162257",
                    AnchorMax = "0.2699255 0.9673118"
                },
            }, "SwitchMapPanel", "OpenArenaText");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = string.Format("settingsmenucmds changepage switcharena {0}", page == pageCount ? page : page - 1),
                    Color = "0.04313726 0.4156863 0.4901961 1"
                },

                Text =
                {
                    Text = "<",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.9006123 0.9268572",
                    AnchorMax = "0.9353557 0.9673116"
                },
            }, "SwitchMapPanel", "PrevPageButton");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = string.Format("settingsmenucmds changepage switcharena {0}", page == pageCount ? page : page + 1),
                    Color = "0.04313726 0.4156863 0.4901961 1"
                },

                Text =
                {
                    Text = ">",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.9452136 0.9268572",
                    AnchorMax = "0.979957 0.9673116"
                },
            }, "SwitchMapPanel", "NextPageButton");

            int index = 0; float minx = 0.022f; float miny = 0.52f; float maxx = 0.194f; float maxy = 0.834f;
            foreach (MapData mapData in _save.ArenaMaps)
            {
                if (mapData.mapName == _save.ArenaData[pInfo.arenaId].mapName) continue;
                if (index < pagemin || index > pagemax) { index++; continue; };
                if (index != pagemin)
                {
                    decimal d = ((decimal)index / 5.0m);
                    bool isInt = d % 1 == 0;
                    if (isInt)
                    {
                        minx = 0.022f; maxx = 0.194f; miny -= 0.396f; maxy -= 0.396f;
                    }
                    else
                    {
                        minx += 0.198f; maxx += 0.198f;
                    }
                }

                container.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0.245283 0.245283 0.245283 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = $"{minx + " " + miny}",
                        AnchorMax = $"{maxx + " " + maxy}",
                    },

                    CursorEnabled = false
                }, "SwitchMapPanel", "ArenaPanel");

                container.Add(new CuiElement
                {
                    Name = "ArenaImage",
                    Parent = "ArenaPanel",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "1 1 1 1",
                            Url = mapData.mapImage
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.04616445 0.3098734",
                            AnchorMax = "0.9538562 0.9604858"
                        }
                    }
                });

                container.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = mapData.mapName,
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "2.758946E-05 0.1768024",
                        AnchorMax = "1.000031 0.3098746"
                    },
                }, "ArenaPanel", "MapName");

                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"settingsmenucmds switcharena {mapData.mapName.Replace(' ', '|')}",
                        Color = "0.07450981 0.07450981 0.07450981 0.8"
                    },

                    Text =
                    {
                        Text = "SWITCH TO MAP",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    RectTransform =
                    {
                        AnchorMin = "3.4877E-07 -4.391477E-05",
                        AnchorMax = "1.00002 0.1767997"
                    },
                }, "ArenaPanel", "SwitchToArenaButton");

                index++;
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Scoreboard
        private void ScoreboardUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "TeamAScoreboardTop");
            CuiHelper.DestroyUi(player, "TeamAScoreboardBottom");
            CuiHelper.DestroyUi(player, "TeamBScoreboardTop");
            CuiHelper.DestroyUi(player, "TeamBScoreboardBottom");

            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo == null) return;

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0.005 -23.223", OffsetMax = "222.96 0.008" }
            }, "Overlay", "TeamAScoreboardTop");

            container.Add(new CuiElement
            {
                Name = "TeamName",
                Parent = "TeamAScoreboardTop",
                Components = {
                    new CuiTextComponent { Text = ArenaInfo[pInfo.arenaId].TeamNames[PlayerTeams.TeamA], Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "0 0.8430505 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-108.209 -7.718", OffsetMax = "-58.344 8.523" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Kills",
                Parent = "TeamAScoreboardTop",
                Components = {
                    new CuiTextComponent { Text = "Kills", Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-51.821 -7.718", OffsetMax = "-16.555 8.523" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Deaths",
                Parent = "TeamAScoreboardTop",
                Components = {
                    new CuiTextComponent { Text = "Deaths", Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10.909 -7.718", OffsetMax = "24.419 8.523" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Damage",
                Parent = "TeamAScoreboardTop",
                Components = {
                    new CuiTextComponent { Text = "Damage", Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "29.307 -7.718", OffsetMax = "70.202 8.523" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Ready",
                Parent = "TeamAScoreboardTop",
                Components = {
                    new CuiTextComponent { Text = "Ready", Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "74.73 -7.718", OffsetMax = "107.579 8.523" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-222.977 -23.222", OffsetMax = "-0.023 0" }
            }, "Overlay", "TeamBScoreboardTop");

            container.Add(new CuiElement
            {
                Name = "TeamName",
                Parent = "TeamBScoreboardTop",
                Components = {
                    new CuiTextComponent { Text = ArenaInfo[pInfo.arenaId].TeamNames[PlayerTeams.TeamB], Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "0 0.8430505 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-108.209 -7.718", OffsetMax = "-58.344 8.523" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Kills",
                Parent = "TeamBScoreboardTop",
                Components = {
                    new CuiTextComponent { Text = "Kills", Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-51.821 -7.718", OffsetMax = "-16.555 8.523" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Deaths",
                Parent = "TeamBScoreboardTop",
                Components = {
                    new CuiTextComponent { Text = "Deaths", Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10.909 -7.718", OffsetMax = "24.419 8.523" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Damage",
                Parent = "TeamBScoreboardTop",
                Components = {
                    new CuiTextComponent { Text = "Damage", Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "29.307 -7.718", OffsetMax = "70.202 8.523" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Ready",
                Parent = "TeamBScoreboardTop",
                Components = {
                    new CuiTextComponent { Text = "Ready", Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "74.73 -7.718", OffsetMax = "107.579 8.523" }
                }
            });
            CuiHelper.AddUi(player, container);
            LoadTeamA(player, pInfo.arenaId); LoadTeamB(player, pInfo.arenaId);
        }

        void UpdateScoreboard(int arenaId, PlayerTeams team)
        {
            if (!ArenaInfo.TryGetValue(arenaId, out arenaSettings)) return;
            switch (team)
            {
                case PlayerTeams.TeamA:
                    foreach (BasePlayer player in PlayersInArena(arenaId, true))
                    {
                        if (player == null || !playerData.ContainsKey(player.UserIDString)) continue;
                        if ((arenaSettings.roundStarted || arenaSettings.isStarting) && !playerData[player.UserIDString].isOut && playerData[player.UserIDString].team != PlayerTeams.Spectator) continue;
                        LoadTeamA(player, arenaId);
                    }

                    break;
                case PlayerTeams.TeamB:
                    foreach (BasePlayer player in PlayersInArena(arenaId, true))
                    {
                        if (player == null || !playerData.ContainsKey(player.UserIDString)) continue;
                        if ((arenaSettings.roundStarted || arenaSettings.isStarting) && !playerData[player.UserIDString].isOut && playerData[player.UserIDString].team != PlayerTeams.Spectator) continue;
                        LoadTeamB(player, arenaId);
                    }
                    break;

                case PlayerTeams.None:
                    foreach (BasePlayer player in PlayersInArena(arenaId, true))
                    {
                        if (player == null || !playerData.ContainsKey(player.UserIDString)) continue;
                        if ((arenaSettings.roundStarted || arenaSettings.isStarting) && !playerData[player.UserIDString].isOut && playerData[player.UserIDString].team != PlayerTeams.Spectator) continue;
                        LoadTeamA(player, arenaId);
                        LoadTeamB(player, arenaId);
                    }
                    break;
            }
        }

        void LoadTeamA(BasePlayer player, int arenaId)
        {
            CuiHelper.DestroyUi(player, "TeamAScoreboardBottom");

            List<BasePlayer> players = Pool.GetList<BasePlayer>();
            players.AddRange(PlayersInArena(arenaId));

            if (players.Count == 0)
            {
                Pool.FreeList(ref players);
                return;
            }

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -380.515", OffsetMax = "222.96 -23.225" }
            }, "Overlay", "TeamAScoreboardBottom");

            float miny = 162.755f; float maxy = 178.645f;
            int index = 0;

            for (int i = 0; i < players.Count; i++)
            {
                BasePlayer sPlayer = players[i];
                if (player == null) continue;

                RustArenaPlayer sInfo = playerData[sPlayer.UserIDString];
                if (sInfo.team != PlayerTeams.TeamA) continue;

                if (index != 0)
                {
                    miny -= 16.242f;
                    maxy -= 16.242f;
                }

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-111.481 {miny}", OffsetMax = $"111.479 {maxy}" }
                }, "TeamAScoreboardBottom", "TeamAPlayer");

                container.Add(new CuiElement
                {
                    Name = "TeamName",
                    Parent = "TeamAPlayer",
                    Components = {
                        new CuiTextComponent { Text = sPlayer.displayName, Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = string.Format("{0}", sInfo.isOut ? "1 0 0.03091812 1" : "0.3976066 1 0 1") },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-108.38 -8.121", OffsetMax = "-58.515 8.12" }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "Kills",
                    Parent = "TeamAPlayer",
                    Components = {
                        new CuiTextComponent { Text = sInfo.RoundKills.ToString(), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-51.821 -8.121", OffsetMax = "-16.555 8.12" }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "Deaths",
                    Parent = "TeamAPlayer",
                    Components = {
                        new CuiTextComponent { Text = string.Format("{0}", sInfo.died ? "1" : "0"), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-11.212 -8.121", OffsetMax = "24.117 8.12" }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "Damage",
                    Parent = "TeamAPlayer",
                    Components = {
                        new CuiTextComponent { Text = sInfo.RoundDamage.ToString(), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "29.305 -8.121", OffsetMax = "70.2 8.12" }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "Ready",
                    Parent = "TeamAPlayer",
                    Components = {
                        new CuiTextComponent { Text = string.Format("{0}", sInfo.isReady ? "X" : ""), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "74.73 -8.121", OffsetMax = "107.579 8.12" }
                    }
                });
                index++;
            }

            Pool.FreeList(ref players);
            CuiHelper.AddUi(player, container);
        }

        void LoadTeamB(BasePlayer player, int arenaId)
        {
            CuiHelper.DestroyUi(player, "TeamBScoreboardBottom");

            List<BasePlayer> players = Pool.GetList<BasePlayer>();
            players.AddRange(PlayersInArena(arenaId));
            if (players.Count == 0) { Pool.FreeList(ref players); return; }

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-222.975 -380.515", OffsetMax = "-0.025 -23.225" }
            }, "Overlay", "TeamBScoreboardBottom");

            float miny = 162.755f; float maxy = 178.645f;
            int index = 0;
            for (int i = 0; i < players.Count; i++)
            {
                BasePlayer sPlayer = players[i];
                if (player == null) continue;

                RustArenaPlayer sInfo = playerData[sPlayer.UserIDString];
                if (sInfo.team != PlayerTeams.TeamB) continue;

                if (index != 0)
                {
                    miny -= 16.242f;
                    maxy -= 16.242f;
                }

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-111.481 {miny}", OffsetMax = $"111.479 {maxy}" }
                }, "TeamBScoreboardBottom", "TeamBPlayer");

                container.Add(new CuiElement
                {
                    Name = "TeamName",
                    Parent = "TeamBPlayer",
                    Components = {
                        new CuiTextComponent { Text = sPlayer.displayName, Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = string.Format("{0}", sInfo.isOut ? "1 0 0.03091812 1" : "0.3976066 1 0 1") },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-108.38 -8.121", OffsetMax = "-58.515 8.12" }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "Kills",
                    Parent = "TeamBPlayer",
                    Components = {
                        new CuiTextComponent { Text = sInfo.RoundKills.ToString(), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-51.821 -8.121", OffsetMax = "-16.555 8.12" }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "Deaths",
                    Parent = "TeamBPlayer",
                    Components = {
                        new CuiTextComponent { Text = string.Format("{0}", sInfo.died ? "1" : "0"), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-11.212 -8.121", OffsetMax = "24.117 8.12" }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "Damage",
                    Parent = "TeamBPlayer",
                    Components = {
                        new CuiTextComponent { Text = sInfo.RoundDamage.ToString(), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "29.305 -8.121", OffsetMax = "70.2 8.12" }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "Ready",
                    Parent = "TeamBPlayer",
                    Components = {
                        new CuiTextComponent { Text = string.Format("{0}", sInfo.isReady ? "X" : ""), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "74.73 -8.121", OffsetMax = "107.579 8.12" }
                    }
                });

                index++;
            }

            Pool.FreeList(ref players);
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Game Over
        private void GameOverUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "GameoverPanel");

            RustArenaPlayer pInfo = playerData[player.UserIDString]; if (pInfo == null) return;
            ArenaSettings arenaInfo = ArenaInfo[pInfo.arenaId]; if (arenaInfo == null) return;

            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0", FadeIn = 1 },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
            }, "Overlay", "GameoverPanel");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                FadeOut = 1,
                Image = { Color = "0 0 0 0.7019608", FadeIn = 1, Material = "assets/content/ui/uibackgroundblur.mat" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
            }, "GameoverPanel", "BlurBackground");

            container.Add(new CuiElement
            {
                Name = "GameOverText",
                Parent = "GameoverPanel",
                Components = {
                    new CuiTextComponent { Text = $"GAME OVER | FIRST TO {arenaInfo.roundCount}", Font = "robotocondensed-bold.ttf", FontSize = 100, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FadeIn = 1 },
                    new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-612 -239", OffsetMax = "609 -66" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "TeamsResult",
                Parent = "GameoverPanel",
                Components = {
                    new CuiTextComponent { Text = string.Format("YOUR TEAM HAS {0}", arenaInfo.roundWinner == pInfo.team ? "WON" : "LOST"), Font = "robotocondensed-bold.ttf", FontSize = 60, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FadeIn = 1 },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-195 -92", OffsetMax = "195 81" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "YourTeamsScoreText",
                Parent = "GameoverPanel",
                Components = {
                    new CuiTextComponent { Text = "YOUR TEAMS SCORE:", Font = "robotocondensed-bold.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FadeIn = 1 },
                    new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "28 13", OffsetMax = "417 81" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "YourTeamsScore",
                Parent = "GameoverPanel",
                Components = {
                    new CuiTextComponent { Text = string.Format("{0}", arenaInfo.Score[pInfo.team].ToString().Length == 1 ? "0" + arenaInfo.Score[pInfo.team].ToString() : arenaInfo.Score[pInfo.team].ToString()), Font = "robotocondensed-bold.ttf", FontSize = 79, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FadeIn = 1 },
                    new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "28 -92", OffsetMax = "417 13" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "OtherTeamsScoreText",
                Parent = "GameoverPanel",
                Components = {
                    new CuiTextComponent { Text = "OTHER TEAMS SCORE:", Font = "robotocondensed-bold.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FadeIn = 1 },
                    new CuiRectTransformComponent { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-420 13", OffsetMax = "-31 81" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "OtherTeamsScore",
                Parent = "GameoverPanel",
                Components = {
                    new CuiTextComponent { Text = string.Format("{0}", arenaInfo.Score[GetOtherTeam(pInfo.team)].ToString().Length == 1 ? "0" + arenaInfo.Score[GetOtherTeam(pInfo.team)].ToString() : arenaInfo.Score[GetOtherTeam(pInfo.team)].ToString()), Font = "robotocondensed-bold.ttf", FontSize = 79, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FadeIn = 1 },
                    new CuiRectTransformComponent { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-420 -92", OffsetMax = "-31 13" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "0.05660379 0.05660379 0.05660379 0.6", FadeIn = 1, Command = "destroyui GameoverPanel" },
                Text = { Text = "CLOSE", Font = "robotocondensed-regular.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FadeIn = 1 },
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-612 70", OffsetMax = "609 124" }
            }, "GameoverPanel", "CloseButton");


            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Transfer Ownership
        private void TransferOwnership(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "TransferOwnership");

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.6", Material = "assets/content/ui/uibackgroundblur.mat" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
            }, "Overlay", "TransferOwnership");

            container.Add(new CuiElement
            {
                Name = "TransferOwnerText",
                Parent = "TransferOwnership",
                Components = {
                    new CuiTextComponent { Text = "WHO WOULD YOU LIKE TO TRANSFER OWNER TO?", Font = "robotocondensed-bold.ttf", FontSize = 32, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-609 292.351", OffsetMax = "612 341.649" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "0.5283019 0.05551251 0.0473478 0.8", Command = "destroyui TransferOwnership" },
                Text = { Text = "CANCEL", Font = "robotocondensed-bold.ttf", FontSize = 15, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "392 302.431", OffsetMax = "612 331.569" }
            }, "TransferOwnership", "CloseButton");

            CuiHelper.AddUi(player, container);
            LoadTransferPlayers(player);
        }

        private void LoadTransferPlayers(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "TransferPlayersPanel");

            RustArenaPlayer pInfo = playerData[player.UserIDString];
            if (pInfo == null) return;

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-609 -336.231", OffsetMax = "612 278.505" }
            }, "Overlay", "TransferPlayersPanel");

            float minx = -611f; float maxx = -391f;
            float miny = 277.1626f; float maxy = 306.3002f;

            int i = 0;
            foreach (BasePlayer transferPlayer in PlayersInArena(pInfo.arenaId, true))
            {
                if (player == null) continue;
                if (i != 0)
                {
                    int[] resetIndexs = { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 };
                    if (resetIndexs.Contains(i))
                    {
                        minx = -611f;
                        maxx = -391f;
                        miny -= 41.484f;
                        maxy -= 40.212f;
                    }
                    else
                    {
                        minx -= -247f;
                        maxx -= -248f;
                    }
                }

                container.Add(new CuiButton
                {
                    Button = { Color = "0.7333333 0.6235294 0.2352941 0.8", Command = $"settingsmenucmds generalact transferownercomplete {transferPlayer.UserIDString}" },
                    Text = { Text = transferPlayer.displayName, Font = "robotocondensed-bold.ttf", FontSize = 15, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{minx} {miny}", OffsetMax = $"{maxx} {maxy}" }
                }, "TransferPlayersPanel", "WLPlayerButton");

                i++;
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #endregion

        #region Extra

        private bool CheckPlayerInventory(BasePlayer player)
        {
            ItemContainer playerInventory = player.inventory.containerMain;
            ItemContainer playerWear = player.inventory.containerWear;
            ItemContainer playerHotBar = player.inventory.containerBelt;

            HashSet<string> bannedItem = new HashSet<string>();

            foreach (var container in new[] { playerInventory, playerWear, playerHotBar })
            {
                foreach (var item in container.itemList)
                {
                    bannedItem.Add(item.info.shortname);
                }
            }

            if (bannedItem.Count > 0)
            {
                return true;
            }
            return false;
        }

        private void TeleportPlayerToLobby(BasePlayer player, Vector3 location)
        {
            if (player == null) { return; }
            if (location == Vector3.zero) { return; }

            player.PauseFlyHackDetection(5f);
            player.PauseSpeedHackDetection(5f);
            player.UpdateActiveItem(default(ItemId));
            player.EnsureDismounted();
            player.Server_CancelGesture();

            if (player.HasParent())
            {
                player.SetParent(null, true, true);
            }

            if (player.IsConnected)
            {
                StartSleepingNow(player);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
                player.ClientRPCPlayer(null, player, "StartLoading", arg1: true);
            }
            var oldPos = player.transform.position;

            player.Teleport(location);

            if (player.IsConnected)
            {
                if (!player._limitedNetworking)
                {
                    player.UpdateNetworkGroup();
                    player.SendNetworkUpdateImmediate(false);
                }

                player.ClearEntityQueue(null);
                player.SendFullSnapshot();
                if (player.IsOnGround())
                {
                    NextTick(player.EndSleeping);
                }
            }

            if (!player._limitedNetworking)
            {
                player.ForceUpdateTriggers();
            }

            Interface.CallHook("OnPlayerTeleported", player, oldPos, location);
        }

        public void StartSleepingNow(BasePlayer player)
        {
            if (!player.IsSleeping())
            {
                Interface.CallHook("OnPlayerSleep", player);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, b: true);
                player.sleepStartTime = Time.time;
                BasePlayer.sleepingPlayerList.Add(player);
                player.CancelInvoke("InventoryUpdate");
                player.CancelInvoke("TeamUpdate");
                player.inventory.loot.Clear();
                player.inventory.containerMain.OnChanged();
                player.inventory.containerBelt.OnChanged();
                player.inventory.containerWear.OnChanged();
                player.Invoke("TurnOffAllLights", 0f);
                if (!player._limitedNetworking)
                {
                    player.EnablePlayerCollider();
                    player.RemovePlayerRigidbody();
                }
                else player.RemoveFromTriggers();
                player.SetServerFall(wantsOn: true);
            }
        }

        #endregion
    }
}



