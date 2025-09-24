using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("Clans", "https://discord.gg/9vyTXsJyKR", "1.0.0")]
    public class Clans : RustPlugin
    {
        //TODO: Work with PlayerDatabase

        //TODO: Displaying the top clans on the screen

        #region Fields

        [PluginReference] private Plugin ImageLibrary,Profile;

        //private const string Layer = "UI.Clans";
        private const string Layer = "UI.Clans";

        //private const string ModalLayer = "SubContent_UI";
        private const string ModalLayer = "UI.Clans.Modal";

        private static Clans _instance;

        private readonly List<ItemDefinition> StandartItems = new List<ItemDefinition>();

        private Coroutine ActionAvatars;

        private readonly Dictionary<uint, ulong> Looters = new Dictionary<uint, ulong>();

        #region Colors

        private string Color1;
        private string Color2;
        private string Color3;
        private string Color4;
        private string Color5;
        private string Color6;

        #endregion

        #endregion

        #region Config

        private static Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Default Avatar")]
            public string DefaultAvatar = "https://i.imgur.com/nn7Lcm2.png";

            [JsonProperty(PropertyName = "Commands", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Commands = new List<string> {"clan", "clans"};

            [JsonProperty(PropertyName = "Minimum clan tag characters")]
            public int TagMin = 2;

            [JsonProperty(PropertyName = "Maximum clan tag characters")]
            public int TagMax = 6;

            [JsonProperty(PropertyName = "Maximum clan description characters")]
            public int DescriptionMax = 256;

            [JsonProperty(PropertyName = "Clan tag in player name")]
            public bool TagInName = true;

            [JsonProperty(PropertyName = "Automatic team creation")]
            public bool AutoTeamCreation = true;

            [JsonProperty(PropertyName = "Allow players to leave their clan by using Rust's leave team button")]
            public bool ClanTeamLeave = true;

            [JsonProperty(PropertyName =
                "Allow players to kick members from their clan using Rust's kick member button")]
            public bool ClanTeamKick = true;

            [JsonProperty(PropertyName =
                "Allow players to invite other players to their clan via Rust's team invite system")]
            public bool ClanTeamInvite = true;

            [JsonProperty(PropertyName = "Allow players to promote other clan members via Rust's team promote button")]
            public bool ClanTeamPromote = true;

            [JsonProperty(PropertyName = "Friendly Fire Default Value")]
            public bool FriendlyFire;

            [JsonProperty(PropertyName = "Top refresh rate")]
            public float TopRefreshRate = 15f;

            [JsonProperty(PropertyName = "Default value for the resource standarts")]
            public int DefaultValStandarts = 100000;

            [JsonProperty(PropertyName = "Steampowered API key")]
            public string SteamWebApiKey =
                "!!! You can get it HERE > https://steamcommunity.com/dev/apikey < and you need to insert HERE !!!";

            [JsonProperty(PropertyName = "Chat Settings")]
            public ChatSettings ChatSettings = new ChatSettings
            {
                Enabled = true, TagFormat = "<color=#4B68FF>[{tag}]</color>"
            };

            [JsonProperty(PropertyName = "Permission Settings")]
            public PermissionSettings PermissionSettings = new PermissionSettings
            {
                UseClanCreating = false,
                ClanCreating = "clans.cancreate",
                UseClanJoining = false,
                ClanJoining = "clans.canjoin",
                UseClanLeave = false,
                ClanLeave = "clans.canleave",
                UseClanDisband = false,
                ClanDisband = "clans.candisband",
                UseClanKick = false,
                ClanKick = "clans.cankick"
            };

            [JsonProperty(PropertyName = "Alliance Settings")]
            public AllianceSettings AllianceSettings = new AllianceSettings
            {
                Enabled = true, UseFF = true, DefaultFF = false
            };

            [JsonProperty(PropertyName = "Purge Settings")]
            public PurgeSettings PurgeSettings = new PurgeSettings
            {
                Enabled = true, OlderThanDays = 14, ListPurgedClans = true, WipeOnNewSave = false
            };

            [JsonProperty(PropertyName = "Limit Settings")]
            public LimitSettings LimitSettings = new LimitSettings
            {
                MemberLimit = 8, ModeratorLimit = 2, AlliancesLimit = 2
            };

            [JsonProperty(PropertyName = "Resources", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Resources = new List<string>
            {
                "stones",
                "sulfur.ore",
                "metal.ore",
                "hq.metal.ore",
                "wood"
            };

            [JsonProperty(PropertyName = "Score Table (shortname - score)",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, float> ScoreTable = new Dictionary<string, float>
            {
                ["kills"] = 1,
                ["deaths"] = -1,
                ["stone-ore"] = 0.1f,
                ["supply_drop"] = 3f,
                ["crate_normal"] = 0.3f,
                ["crate_elite"] = 0.5f,
                ["bradley_crate"] = 5f,
                ["heli_crate"] = 5f,
                ["bradley"] = 10f,
                ["helicopter"] = 15f,
                ["barrel"] = 0.1f,
                ["scientistnpc"] = 0.5f,
                ["heavyscientist"] = 2f,
                ["sulfur.ore"] = 0.5f,
                ["metal.ore"] = 0.5f,
                ["hq.metal.ore"] = 0.5f,
                ["stones"] = 0.5f,
                ["cupboard.tool.deployed"] = 1f
            };

            [JsonProperty(PropertyName = "Available items for resource standarts",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> AvailableStandartItems = new List<string>
            {
                "gears",
                "metalblade",
                "metalpipe",
                "propanetank",
                "roadsigns",
                "rope",
                "sewingkit",
                "sheetmetal",
                "metalspring",
                "tarp",
                "techparts",
                "riflebody",
                "semibody",
                "smgbody",
                "fat.animal",
                "cctv.camera",
                "charcoal",
                "cloth",
                "crude.oil",
                "diesel_barrel",
                "gunpowder",
                "hq.metal.ore",
                "leather",
                "lowgradefuel",
                "metal.fragments",
                "metal.ore",
                "scrap",
                "stones",
                "sulfur.ore",
                "sulfur",
                "targeting.computer",
                "wood"
            };

            [JsonProperty(PropertyName = "Blocked Words", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> BlockedWords = new List<string> {"admin", "mod", "owner"};

            [JsonProperty(PropertyName = "Pages", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<PageSettings> Pages = new List<PageSettings>
            {
                new PageSettings {ID = 0, Key = "aboutclan", Enabled = true, Permission = string.Empty},
                new PageSettings {ID = 1, Key = "memberslist", Enabled = true, Permission = string.Empty},
                new PageSettings {ID = 2, Key = "clanstop", Enabled = true, Permission = string.Empty},
                new PageSettings {ID = 3, Key = "playerstop", Enabled = true, Permission = string.Empty},
                new PageSettings {ID = 4, Key = "resources", Enabled = true, Permission = string.Empty},
                new PageSettings {ID = 6, Key = "skins", Enabled = true, Permission = string.Empty},
                new PageSettings {ID = 5, Key = "playerslist", Enabled = true, Permission = string.Empty},
                new PageSettings {ID = 7, Key = "alianceslist", Enabled = true, Permission = string.Empty}
            };

            [JsonProperty(PropertyName = "Interface")]
            public InterfaceSettings UI = new InterfaceSettings
            {
                Color1 = "#0E0E10",
                Color2 = "#ff4a4a",
                Color3 = "#161617",
                Color4 = "#913131",
                Color5 = "#303030",
                Color6 = "#FF4B4B",
                ValueAbbreviation = true,
                TopClansColumns =
                    new List<ColumnSettings>
                    {
                        new ColumnSettings
                        {
                            Width = 75,
                            Key = "top",
                            LangKey = TopTitle,
                            TextAlign = TextAnchor.MiddleCenter,
                            TitleFontSize = 10,
                            FontSize = 12,
                            TextFormat = "#{0}"
                        },
                        new ColumnSettings
                        {
                            Width = 165,
                            Key = "name",
                            LangKey = NameTitle,
                            TextAlign = TextAnchor.MiddleCenter,
                            TitleFontSize = 12,
                            FontSize = 12,
                            TextFormat = "{0}"
                        },
                        new ColumnSettings
                        {
                            Width = 70,
                            Key = "leader",
                            LangKey = LeaderTitle,
                            TextAlign = TextAnchor.MiddleCenter,
                            TitleFontSize = 12,
                            FontSize = 12,
                            TextFormat = "{0}"
                        },
                        new ColumnSettings
                        {
                            Width = 90,
                            Key = "members",
                            LangKey = MembersTitle,
                            TextAlign = TextAnchor.MiddleCenter,
                            TitleFontSize = 12,
                            FontSize = 12,
                            TextFormat = "{0}"
                        },
                        new ColumnSettings
                        {
                            Width = 80,
                            Key = "score",
                            LangKey = ScoreTitle,
                            TextAlign = TextAnchor.MiddleCenter,
                            TitleFontSize = 12,
                            FontSize = 12,
                            TextFormat = "{0}"
                        }
                    },
                TopPlayersColumns = new List<ColumnSettings>
                {
                    new ColumnSettings
                    {
                        Width = 75,
                        Key = "top",
                        LangKey = TopTitle,
                        TextAlign = TextAnchor.MiddleCenter,
                        TitleFontSize = 10,
                        FontSize = 12,
                        TextFormat = "#{0}"
                    },
                    new ColumnSettings
                    {
                        Width = 185,
                        Key = "name",
                        LangKey = NameTitle,
                        TextAlign = TextAnchor.MiddleCenter,
                        TitleFontSize = 12,
                        FontSize = 12,
                        TextFormat = "{0}"
                    },
                    new ColumnSettings
                    {
                        Width = 70,
                        Key = "kills",
                        LangKey = KillsTitle,
                        TextAlign = TextAnchor.MiddleCenter,
                        TitleFontSize = 12,
                        FontSize = 12,
                        TextFormat = "{0}"
                    },
                    new ColumnSettings
                    {
                        Width = 70,
                        Key = "resources",
                        LangKey = ResourcesTitle,
                        TextAlign = TextAnchor.MiddleCenter,
                        TitleFontSize = 12,
                        FontSize = 12,
                        TextFormat = "{0}"
                    },
                    new ColumnSettings
                    {
                        Width = 80,
                        Key = "score",
                        LangKey = ScoreTitle,
                        TextAlign = TextAnchor.MiddleCenter,
                        TitleFontSize = 12,
                        FontSize = 12,
                        TextFormat = "{0}"
                    }
                }
            };

            [JsonProperty(PropertyName = "Skins Settings")]
            public SkinsSettings Skins = new SkinsSettings
            {
                ItemSkins = new Dictionary<string, List<ulong>>
                {
                    ["metal.facemask"] = new List<ulong>(),
                    ["hoodie"] = new List<ulong>(),
                    ["metal.plate.torso"] = new List<ulong>(),
                    ["pants"] = new List<ulong>(),
                    ["roadsign.kilt"] = new List<ulong>(),
                    ["shoes.boots"] = new List<ulong>(),
                    ["rifle.ak"] = new List<ulong>(),
                    ["rifle.bolt"] = new List<ulong>()
                },
                CanCustomSkin = true,
                Permission = string.Empty
            };
        }

        private class SkinsSettings
        {
            [JsonProperty(PropertyName = "Item Skins", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, List<ulong>> ItemSkins;

            [JsonProperty(PropertyName = "Can players install custom skins?")]
            public bool CanCustomSkin;

            [JsonProperty(PropertyName = "Permission to install custom skin")]
            public string Permission;
        }

        private class InterfaceSettings
        {
            [JsonProperty(PropertyName = "Color 1")]
            public string Color1;

            [JsonProperty(PropertyName = "Color 2")]
            public string Color2;

            [JsonProperty(PropertyName = "Color 3")]
            public string Color3;

            [JsonProperty(PropertyName = "Color 4")]
            public string Color4;

            [JsonProperty(PropertyName = "Color 5")]
            public string Color5;

            [JsonProperty(PropertyName = "Color 6")]
            public string Color6;

            [JsonProperty(PropertyName = "Use value abbreviation?")]
            public bool ValueAbbreviation;

            [JsonProperty(PropertyName = "Top Clans Columns", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ColumnSettings> TopClansColumns;

            [JsonProperty(PropertyName = "Top Players Columns",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ColumnSettings> TopPlayersColumns;
        }

        private class ColumnSettings
        {
            [JsonProperty(PropertyName = "Width")] public float Width;

            [JsonProperty(PropertyName = "Lang Key")]
            public string LangKey;

            [JsonProperty(PropertyName = "Key")] public string Key;

            [JsonProperty(PropertyName = "Text Format")]
            public string TextFormat;

            [JsonProperty(PropertyName = "Text Align")] [JsonConverter(typeof(StringEnumConverter))]
            public TextAnchor TextAlign;

            [JsonProperty(PropertyName = "Title Font Size")]
            public int TitleFontSize;

            [JsonProperty(PropertyName = "Font Size")]
            public int FontSize;

            public string GetKey(int top, string values)
            {
                switch (Key)
                {
                    case "top":
                        return string.Format(TextFormat, top);

                    default:
                        return string.Format(TextFormat, values);
                }
            }
        }

        private class PageSettings
        {
            [JsonProperty(PropertyName = "ID (DON'T CHANGE)")]
            public int ID;

            [JsonProperty(PropertyName = "Key (DON'T CHANGE)")]
            public string Key;

            [JsonProperty(PropertyName = "Enabled?")]
            public bool Enabled;

            [JsonProperty(PropertyName = "Permission")]
            public string Permission;
        }

        private class LimitSettings
        {
            [JsonProperty(PropertyName = "Member Limit")]
            public int MemberLimit;

            [JsonProperty(PropertyName = "Moderator Limit")]
            public int ModeratorLimit;

            [JsonProperty(PropertyName = "Alliances Limit")]
            public int AlliancesLimit;
        }

        private class PurgeSettings
        {
            [JsonProperty(PropertyName = "Enable clan purging")]
            public bool Enabled;

            [JsonProperty(PropertyName = "Purge clans that havent been online for x amount of day")]
            public int OlderThanDays;

            [JsonProperty(PropertyName = "List purged clans in console when purging")]
            public bool ListPurgedClans;

            [JsonProperty(PropertyName = "Wipe clans on new map save")]
            public bool WipeOnNewSave;
        }

        private class ChatSettings
        {
            [JsonProperty(PropertyName = "Enable clan tags in chat?")]
            public bool Enabled;

            [JsonProperty(PropertyName = "Tag format")]
            public string TagFormat;
        }

        private class PermissionSettings
        {
            [JsonProperty(PropertyName = "Use permission to create a clan")]
            public bool UseClanCreating;

            [JsonProperty(PropertyName = "Permission to create a clan")]
            public string ClanCreating;

            [JsonProperty(PropertyName = "Use permission to join a clan")]
            public bool UseClanJoining;

            [JsonProperty(PropertyName = "Permission to join a clan")]
            public string ClanJoining;

            [JsonProperty(PropertyName = "Use permission to kick a clan member")]
            public bool UseClanKick;

            [JsonProperty(PropertyName = "Clan kick permission")]
            public string ClanKick;

            [JsonProperty(PropertyName = "Use permission to leave a clan")]
            public bool UseClanLeave;

            [JsonProperty(PropertyName = "Clan leave permission")]
            public string ClanLeave;

            [JsonProperty(PropertyName = "Use permission to disband a clan")]
            public bool UseClanDisband;

            [JsonProperty(PropertyName = "Clan disband permission")]
            public string ClanDisband;
        }

        private class AllianceSettings
        {
            [JsonProperty(PropertyName = "Enable clan alliances")]
            public bool Enabled;

            [JsonProperty(PropertyName = "Enable friendly fire (allied clans)")]
            public bool UseFF;

            [JsonProperty(PropertyName = "Default friendly firy value")]
            public bool DefaultFF;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
        }

        #endregion

        #region Data

        private List<ClanData> ClansList = new List<ClanData>();

        private Dictionary<ulong, PlayerData> PlayersList = new Dictionary<ulong, PlayerData>();

        private void SaveData()
        {
            SaveClans();

            SavePlayers();
        }

        private void SaveClans()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/ClansList", ClansList);
        }

        private void SavePlayers()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/PlayersList", PlayersList);
        }

        private void LoadData()
        {
            try
            {
                ClansList = Interface.Oxide.DataFileSystem.ReadObject<List<ClanData>>($"{Name}/ClansList");
                PlayersList =
                    Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerData>>($"{Name}/PlayersList");
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            if (ClansList == null) ClansList = new List<ClanData>();
            if (PlayersList == null) PlayersList = new Dictionary<ulong, PlayerData>();
        }

        private class PlayerData
        {
            [JsonProperty(PropertyName = "Display Name")]
            public string DisplayName;

            [JsonProperty(PropertyName = "Last Login")]
            public DateTime LastLogin;

            [JsonProperty(PropertyName = "Friendly Fire")]
            public bool FriendlyFire;

            [JsonProperty(PropertyName = "Ally Friendly Fire")]
            public bool AllyFriendlyFire;

            [JsonProperty(PropertyName = "Stats", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, float> Stats = new Dictionary<string, float>();

            [JsonProperty(PropertyName = "Invites", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, InviteData> Invites = new Dictionary<string, InviteData>();

            #region Stats

            [JsonIgnore]
            public float Kills
            {
                get
                {
                    float kills;
                    Stats.TryGetValue("kills", out kills);
                    return float.IsNaN(kills) || float.IsInfinity(kills) ? 0 : kills;
                }
            }

            [JsonIgnore]
            public float Deaths
            {
                get
                {
                    float deaths;
                    Stats.TryGetValue("deaths", out deaths);
                    return float.IsNaN(deaths) || float.IsInfinity(deaths) ? 0 : deaths;
                }
            }

            [JsonIgnore]
            public float KD
            {
                get
                {
                    var kd = Kills / Deaths;
                    return float.IsNaN(kd) || float.IsInfinity(kd) ? 0 : kd;
                }
            }

            [JsonIgnore]
            public float Resources
            {
                get
                {
                    var resources = Stats.Where(x => _config.Resources.Contains(x.Key)).Sum(x => x.Value);
                    return float.IsNaN(resources) || float.IsInfinity(resources) ? 0 : resources;
                }
            }

            [JsonIgnore]
            public float Score
            {
                get
                {
                    return (float) Math.Round(Stats.Where(x => _config.ScoreTable.ContainsKey(x.Key))
                        .Sum(x => x.Value * _config.ScoreTable[x.Key]));
                }
            }

            public float GetValue(string key)
            {
                float val;
                Stats.TryGetValue(key, out val);
                return float.IsNaN(val) || float.IsInfinity(val) ? 0 : Mathf.Round(val);
            }

            public float GetTotalFarm(ClanData clan)
            {
                return (float) Math.Round(clan.ResourceStandarts.Values.Sum(check =>
                {
                    var result = GetValue(check.ShortName) / check.Amount;
                    return result > 1 ? 1 : result;
                }) / clan.ResourceStandarts.Count, 3);
            }

            #endregion
        }

        private class InviteData
        {
            [JsonProperty(PropertyName = "Inviter Name")]
            public string InviterName;

            [JsonProperty(PropertyName = "Inviter Id")]
            public ulong InviterId;
        }

        private class ClanData
        {
            [JsonProperty(PropertyName = "Clan Tag")]
            public string ClanTag;

            [JsonProperty(PropertyName = "Avatar")]
            public string Avatar;

            [JsonProperty(PropertyName = "Leader ID")]
            public ulong LeaderID;

            [JsonProperty(PropertyName = "Leader Name")]
            public string LeaderName;

            [JsonProperty(PropertyName = "Description")]
            public string Description;

            [JsonProperty(PropertyName = "Creation Time")]
            public DateTime CreationTime;

            [JsonProperty(PropertyName = "Last Online Time")]
            public DateTime LastOnlineTime;

            [JsonProperty(PropertyName = "Moderators", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ulong> Moderators = new List<ulong>();

            [JsonProperty(PropertyName = "Members", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ulong> Members = new List<ulong>();

            [JsonProperty(PropertyName = "Invited Players", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<ulong, ulong> InvitedPlayers = new Dictionary<ulong, ulong>();

            [JsonProperty(PropertyName = "Resource Standarts", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<int, ResourceStandart> ResourceStandarts = new Dictionary<int, ResourceStandart>();

            [JsonProperty(PropertyName = "Skins", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, ulong> Skins = new Dictionary<string, ulong>();

            [JsonProperty(PropertyName = "Alliances", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Alliances = new List<string>();

            [JsonProperty(PropertyName = "Alliance Invites", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, ulong> AllianceInvites = new Dictionary<string, ulong>();

            [JsonProperty(PropertyName = "Incoming Alliances", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> IncomingAlliances = new List<string>();

            [JsonProperty(PropertyName = "Team ID")]
            public ulong TeamID;

            [JsonIgnore] public int Top;

            [JsonIgnore]
            private RelationshipManager.PlayerTeam Team =>
                RelationshipManager.ServerInstance.FindTeam(TeamID) ?? CreateTeam();

            #region Info

            public bool IsOwner(string userId)
            {
                return IsOwner(ulong.Parse(userId));
            }

            public bool IsOwner(ulong userId)
            {
                return LeaderID == userId;
            }

            public bool IsModerator(string userId)
            {
                return IsModerator(ulong.Parse(userId));
            }

            public bool IsModerator(ulong userId)
            {
                return Moderators.Contains(userId) || IsOwner(userId);
            }

            public bool IsMember(string userId)
            {
                return IsMember(ulong.Parse(userId));
            }

            public bool IsMember(ulong userId)
            {
                return Members.Contains(userId);
            }

            #endregion

            public static ClanData CreateNewClan(string clanTag, BasePlayer leader)
            {
                var clan = new ClanData
                {
                    ClanTag = clanTag,
                    LeaderID = leader.userID,
                    LeaderName = leader.displayName,
                    Avatar = _config.DefaultAvatar,
                    Members = new List<ulong> {leader.userID},
                    CreationTime = DateTime.Now,
                    LastOnlineTime = DateTime.Now,
                    Top = _instance.TopClans.Count + 1
                };

                #region Invites

                _instance.GetPlayerData(leader)?.Invites.Clear();

                _instance.ClansList.ForEach(x => x.InvitedPlayers.Remove(leader.userID));

                #endregion

                _instance.ClansList.Add(clan);

                if (_config.TagInName) leader.displayName = $"[{clanTag}] {leader.displayName}";

                if (_config.AutoTeamCreation) clan.CreateTeam();

                ClanCreate(clanTag);
                return clan;
            }

            #region Main

            public void Disband()
            {
                var memberUserIDs = Members.Select(x => x.ToString()).ToList();
                ClanDisbanded(memberUserIDs);
                ClanDisbanded(ClanTag, memberUserIDs);

                Members.ToList().ForEach(member => Kick(member, true));

                ClanDestroy(ClanTag);

                _instance?.ClansList.ForEach(clan =>
                {
                    clan.Alliances.Remove(ClanTag);
                    clan.AllianceInvites.Remove(ClanTag);
                    clan.IncomingAlliances.Remove(ClanTag);
                });

                if (_config.AutoTeamCreation)
                    Team?.members.ToList()
                        .ForEach(member =>
                        {
                            Team.RemovePlayer(member);

                            var player = RelationshipManager.FindByID(member);
                            if (player != null) player.ClearTeam();
                        });

                _instance?.ClansList.Remove(this);
            }

            public void Join(BasePlayer player)
            {
                Members.Add(player.userID);

                if (_config.TagInName) player.displayName = $"[{ClanTag}] {player.displayName}";

                if (_config.AutoTeamCreation)
                {
                    player.Team?.RemovePlayer(player.userID);

                    Team?.AddPlayer(player);
                }

                if (Members.Count >= _config.LimitSettings.MemberLimit)
                {
                    InvitedPlayers.Clear();

                    foreach (var member in _instance.PlayersList.Values) member.Invites.Remove(ClanTag);
                }

                _instance.ClansList.ForEach(x => x.InvitedPlayers.Remove(player.userID));
                _instance.GetPlayerData(player.userID)?.Invites.Clear();

                ClanMemberJoined(player.UserIDString, ClanTag);

                ClanUpdate(ClanTag);
            }

            public void Kick(ulong target, bool disband = false)
            {
                Members.Remove(target);
                Moderators.Remove(target);

                if (_config.TagInName)
                {
                    var data = _instance.GetPlayerData(target);
                    if (data == null) return;

                    var player = BasePlayer.FindByID(target);
                    if (player != null) player.displayName = data.DisplayName;
                }

                if (!disband)
                {
                    if (_config.AutoTeamCreation && Team != null) Team.RemovePlayer(target);

                    if (Members.Count == 0)
                    {
                        Disband();
                    }
                    else
                    {
                        if (LeaderID == target) SetLeader((Moderators.Count > 0 ? Moderators : Members).GetRandom());
                    }
                }

                ClanMemberGone(target.ToString(), Members.Select(x => x.ToString()).ToList());

                ClanMemberGone(target.ToString(), ClanTag);

                ClanUpdate(ClanTag);
            }

            public void SetModer(ulong target)
            {
                if (!Moderators.Contains(target)) Moderators.Add(target);

                ClanUpdate(ClanTag);
            }

            public void UndoModer(ulong target)
            {
                Moderators.Remove(target);

                ClanUpdate(ClanTag);
            }

            public void SetLeader(ulong target)
            {
                var data = _instance.GetPlayerData(target);
                if (data != null) LeaderName = data.DisplayName;

                LeaderID = target;

                if (_config.AutoTeamCreation) Team.SetTeamLeader(target);

                ClanUpdate(ClanTag);
            }

            #endregion

            #region Additionall

            public RelationshipManager.PlayerTeam CreateTeam()
            {
                var team = RelationshipManager.ServerInstance.CreateTeam();
                team.teamLeader = LeaderID;
                AddPlayer(LeaderID, team);

                TeamID = team.teamID;

                return team;
            }

            public void AddPlayer(ulong member, RelationshipManager.PlayerTeam team = null)
            {
                if (team == null) team = Team;

                if (!team.members.Contains(member)) team.members.Add(member);

                if (member == LeaderID) team.teamLeader = LeaderID;

                RelationshipManager.ServerInstance.playerToTeam[member] = team;

                var player = RelationshipManager.FindByID(member);
                if (player != null)
                {
                    if (player.Team != null && player.Team.teamID != team.teamID)
                    {
                        player.Team.RemovePlayer(player.userID);
                        player.ClearTeam();
                    }

                    player.currentTeam = team.teamID;

                    team.MarkDirty();
                    player.SendNetworkUpdate();
                }
            }

            public float Score()
            {
                return Members.Sum(member => _instance.GetPlayerData(member).Score);
            }

            public string Scores()
            {
                return GetValue(Score());
            }

            public float GetTotalFarm()
            {
                return (float) Math.Round(
                    Members.Sum(member => _instance.GetPlayerData(member).GetTotalFarm(this)) / Members.Count, 3);
            }

            public JObject ToJObject()
            {
                return new JObject
                {
                    ["tag"] = ClanTag,
                    ["description"] = Description,
                    ["owner"] = LeaderID,
                    ["moderators"] = new JArray(Moderators),
                    ["members"] = new JArray(Members),
                    ["invitedallies"] = new JArray(InvitedPlayers.Keys.Select(x => x.ToString()))
                };
            }

            public void SetSkin(string shortName, ulong skin)
            {
                Skins[shortName] = skin;

                foreach (var player in Players)
                foreach (var item in player.inventory.AllItems())
                    if (item.info.shortname == shortName)
                        ApplySkinToItem(item, skin);
            }

            public string GetParams(string value)
            {
                switch (value)
                {
                    case "name":
                        return ClanTag;
                    case "leader":
                        return LeaderName;
                    case "members":
                        return Members.Count.ToString();
                    case "score":
                        return Scores();
                    default:
                        return Math.Round(Members.Sum(member => _instance.GetPlayerData(member).GetValue(value)))
                            .ToString(CultureInfo.InvariantCulture);
                }
            }

            #endregion

            #region Utils

            [JsonIgnore]
            public IEnumerable<BasePlayer> Players
            {
                get { return Members.Select(BasePlayer.FindByID).Where(player => player != null); }
            }

            #endregion
        }

        private class ResourceStandart
        {
            public string ShortName;

            public int Amount;
        }

        private PlayerData GetPlayerData(BasePlayer player)
        {
            return GetPlayerData(player.userID);
        }

        private PlayerData GetPlayerData(ulong userId)
        {
            if (!PlayersList.ContainsKey(userId))
            {
                PlayersList.Add(userId, new PlayerData());

                PlayersList[userId].FriendlyFire = _config.FriendlyFire;
            }

            return PlayersList[userId];
        }

        #region Stats

        private void AddToStats(ulong member, string shortName, int amount = 1)
        {
            if (!member.IsSteamId() || !PlayerHasClan(member)) return;

            var data = GetPlayerData(member);
            if (data == null) return;

            if (data.Stats.ContainsKey(shortName))
                data.Stats[shortName] += amount;
            else
                data.Stats.Add(shortName, amount);
        }

        private float GetStatsValue(ulong member, string shortname)
        {
            var data = GetPlayerData(member);
            if (data == null) return 0;

            switch (shortname)
            {
                case "total":
                {
                    return data.Score;
                }
                case "kd":
                {
                    return data.KD;
                }
                case "resources":
                {
                    return data.Resources;
                }
                default:
                {
                    float result;
                    return data.Stats.TryGetValue(shortname, out result) ? result : 0;
                }
            }
        }

        private int GetTop(ulong member, int mode = 0)
        {
            var i = 1;

            foreach (var player in PlayersList.OrderByDescending(x =>
            {
                switch (mode)
                {
                    case 2:
                        return x.Value.KD;
                    case 1:
                        return x.Value.Resources;
                    default:
                        return x.Value.Score;
                }
            }))
            {
                if (player.Key == member) return i;

                i++;
            }

            return i;
        }

        #endregion

        #endregion

        #region Hooks

        private void Init()
        {
            _instance = this;

            LoadData();

            if (!_config.ClanTeamLeave) Unsubscribe(nameof(OnTeamLeave));

            if (!_config.ClanTeamKick) Unsubscribe(nameof(OnTeamKick));

            if (!_config.ClanTeamInvite) Unsubscribe(nameof(OnTeamInvite));

            if (!_config.ClanTeamPromote) Unsubscribe(nameof(OnTeamPromote));

            PurgeClans();
        }

        private void OnServerInitialized()
        {
            LoadImages();

            foreach (var player in BasePlayer.activePlayerList) OnPlayerConnected(player);

            AddCovalenceCommand(_config.Commands.ToArray(), nameof(CmdClans));

            FillingStandartItems();

            HandleTop();

            LoadSkins();

            LoadColors();

            if (_config.ChatSettings.Enabled)
                Interface.CallHook("API_RegisterThirdPartyTitle", this,
                    new Func<IPlayer, string>(BetterChat_FormattedClanTag));

            FillingTeams();

            RegisterPermissions();

            Puts($"Loaded {ClansList.Count} clans!");

            timer.Every(_config.TopRefreshRate, HandleTop);
        }

        private void OnServerSave()
        {
            timer.In(Random.Range(2f, 10f), SaveClans);
            timer.In(Random.Range(2f, 10f), SavePlayers);
        }

        private void Unload()
        {
            if (ActionAvatars != null) ServerMgr.Instance.StopCoroutine(ActionAvatars);

            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.DestroyUi(player, ModalLayer);

                if (_config.TagInName)
                {
                    var data = GetPlayerData(player);
                    if (data != null) player.displayName = data.DisplayName;
                }
            }

            SaveData();

            _instance = null;
            _config = null;
        }

        private void OnNewSave(string filename)
        {
            if (!_config.PurgeSettings.WipeOnNewSave) return;

            ClansList.Clear();
            PlayersList.Clear();

            SaveData();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null || !player.userID.IsSteamId()) return;

            GetAvatar(player.UserIDString,
                avatar => ImageLibrary?.Call("AddImage", avatar, $"avatar_{player.UserIDString}"));

            var data = GetPlayerData(player);
            if (data == null) return;

            data.DisplayName = player.displayName;
            data.LastLogin = DateTime.Now;

            var clan = FindClanByID(player.userID);
            if (clan == null) return;

            clan.LastOnlineTime = DateTime.Now;

            if (_config.TagInName) player.displayName = $"[{clan.ClanTag}] {player.displayName}";

            if (_config.AutoTeamCreation) clan.AddPlayer(player.userID);
        }

        #region Stats

        #region Kills

        private void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null || player.ShortPrefabName == "player" && !player.userID.IsSteamId())
                return;

            var attacker = info.InitiatorPlayer;
            if (attacker == null || !attacker.userID.IsSteamId() || IsTeammates(player.userID, attacker.userID)) return;

            if (player.userID.IsSteamId())
            {
                AddToStats(attacker.userID, "kills");
                AddToStats(player.userID, "deaths");
            }
            else
            {
                AddToStats(attacker.userID, player.ShortPrefabName);
            }
        }

        #endregion

        #region Gather

        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            OnGather(player, item.info.shortname, item.amount);
        }

        private void OnCropGather(GrowableEntity plant, Item item, BasePlayer player)
        {
            OnGather(player, item.info.shortname, item.amount);
        }

        private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            OnGather(player, item.info.shortname, item.amount);
        }

        private void OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            OnGather(player, item.info.shortname, item.amount);
        }

        private void OnGather(BasePlayer player, string shortname, int amount)
        {
            if (player == null || string.IsNullOrEmpty(shortname) || amount <= 0) return;

            AddToStats(player.userID, shortname, amount);
        }

        #endregion

        #region Loot

        private void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            ulong id = 0U;
            if (container.entityOwner != null)
                id = container.entityOwner.OwnerID;
            else if (container.playerOwner != null) id = container.playerOwner.userID;

            if (!Looters.ContainsKey(item.uid)) Looters.Add(item.uid, id);
        }

        private void CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot,
            int amount)
        {
            if (item == null || playerLoot == null) return;

            var player = playerLoot.GetComponent<BasePlayer>();
            if (player == null) return;

            if (!(item.GetRootContainer()?.entityOwner is LootContainer)) return;

            if (targetContainer == 0 && targetSlot == -1) AddToStats(player.userID, item.info.shortname, item.amount);
        }

        private void OnItemPickup(Item item, BasePlayer player)
        {
            if (item == null || player == null) return;

            if (Looters.ContainsKey(item.uid))
            {
                if (Looters[item.uid] != player.userID)
                {
                    AddToStats(player.userID, item.info.shortname, item.amount);
                    Looters.Remove(item.uid);
                }
            }
            else
            {
                Looters.Add(item.uid, player.userID);
            }
        }

        #endregion

        #region Entity Death

        private readonly Dictionary<uint, BasePlayer> _lastHeli = new Dictionary<uint, BasePlayer>();

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var helicopter = entity as BaseHelicopter;
            if (helicopter != null && helicopter.net != null && info.InitiatorPlayer != null)
                _lastHeli[helicopter.net.ID] = info.InitiatorPlayer;

            var player = entity as BasePlayer;
            if (player == null) return;

            var initiatorPlayer = info.InitiatorPlayer;
            if (initiatorPlayer == null || player == initiatorPlayer) return;

            var data = GetPlayerData(initiatorPlayer.userID);
            if (data == null) return;

            var clan = FindClanByID(initiatorPlayer.userID);
            if (clan == null) return;

            if (!data.FriendlyFire && clan.IsMember(player.userID))
            {
                info.damageTypes.ScaleAll(0);

                Reply(player, CannotDamage);
            }

            if (!data.AllyFriendlyFire && clan.Alliances.Select(FindClanByTag).Any(x => x.IsMember(player.userID)))
            {
                info.damageTypes.ScaleAll(0);

                Reply(player, AllyCannotDamage);
            }
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;

            if (entity is BaseHelicopter)
            {
                if (_lastHeli.ContainsKey(entity.net.ID))
                {
                    var basePlayer = _lastHeli[entity.net.ID];
                    if (basePlayer != null) AddToStats(basePlayer.userID, "helicopter");
                }

                return;
            }

            var player = info.InitiatorPlayer;
            if (player == null) return;

            if (entity is BradleyAPC)
                AddToStats(player.userID, "bradley");
            else if (entity.name.Contains("barrel"))
                AddToStats(player.userID, "barrel");
            else if (_config.ScoreTable.ContainsKey(entity.ShortPrefabName))
                AddToStats(player.userID, entity.ShortPrefabName);
        }

        #endregion

        #endregion

        #region Skins

        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (container == null || item == null) return;

            var player = container.GetOwnerPlayer();
            if (player == null) return;

            var clan = FindClanByID(player.userID);
            if (clan == null) return;

            if (_config.Skins.ItemSkins.ContainsKey(item.info.shortname) && clan.Skins.ContainsKey(item.info.shortname))
            {
                var skin = clan.Skins[item.info.shortname];

                if (item.info.category == ItemCategory.Attire)
                {
                    if (container == player.inventory.containerWear) ApplySkinToItem(item, skin);
                }
                else
                {
                    ApplySkinToItem(item, skin);
                }
            }

            if (Looters.ContainsKey(item.uid))
            {
                if (container.playerOwner != null)
                    if (Looters[item.uid] != container.playerOwner.userID)
                    {
                        AddToStats(player.userID, item.info.shortname, item.amount);
                        Looters.Remove(item.uid);
                    }
            }
            else if (container.playerOwner != null)
            {
                Looters.Add(item.uid, container.playerOwner.userID);
            }
        }

        #endregion

        #region Team

        private void OnTeamLeave(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            if (team == null || player == null) return;

            FindClanByID(player.userID)?.Kick(player.userID);
        }

        private void OnTeamKick(RelationshipManager.PlayerTeam team, BasePlayer player, ulong target)
        {
            if (team == null || player == null) return;

            FindClanByID(player.userID)?.Kick(target);
        }

        private void OnTeamInvite(BasePlayer inviter, BasePlayer target)
        {
            if (inviter == null || target == null) return;

            SendInvite(inviter, target.userID);
        }

        private void OnTeamPromote(RelationshipManager.PlayerTeam team, BasePlayer newLeader)
        {
            if (team == null || newLeader == null) return;

            FindClanByID(team.teamLeader)?.SetLeader(newLeader.userID);
        }

        #endregion

        #endregion

        #region Commands

        private void CmdClans(IPlayer cov, string command, string[] args)
        {
            var player = cov?.Object as BasePlayer;
            if (player == null) return;

            if (args.Length == 0)
            {
                MainUi(player, First: true);
                return;
            }

            switch (args[0])
            {
                case "create":
                {
                    if (args.Length < 2)
                    {
                        SendReply(player, $"Error syntax! Use: /{command} {args[0]} <clan tag>");
                        return;
                    }

                    if (_config.PermissionSettings.UseClanCreating &&
                        !string.IsNullOrEmpty(_config.PermissionSettings.ClanCreating) &&
                        !permission.UserHasPermission(player.UserIDString, _config.PermissionSettings.ClanCreating))
                    {
                        Reply(player, NoPermCreateClan);
                        return;
                    }

                    if (PlayerHasClan(player.userID))
                    {
                        Reply(player, AlreadyClanMember);
                        return;
                    }

                    var tag = string.Join(" ", args.Skip(1));
                    if (string.IsNullOrEmpty(tag) || tag.Length < _config.TagMin || tag.Length > _config.TagMax)
                    {
                        Reply(player, ClanTagLimit, _config.TagMin, _config.TagMax);
                        return;
                    }

                    tag = tag.Replace(" ", "");

                    if (_config.BlockedWords.Exists(word => tag.Contains(word, CompareOptions.OrdinalIgnoreCase)))
                    {
                        Reply(player, ContainsForbiddenWords);
                        return;
                    }

                    var clan = FindClanByTag(tag);
                    if (clan != null)
                    {
                        Reply(player, ClanExists);
                        return;
                    }

                    clan = ClanData.CreateNewClan(tag, player);
                    if (clan == null) return;

                    Reply(player, ClanCreated, tag);
                    break;
                }

                case "disband":
                {
                    var clan = FindClanByID(player.userID);
                    if (clan == null)
                    {
                        Reply(player, NotClanMember);
                        return;
                    }

                    if (!clan.IsOwner(player.userID))
                    {
                        Reply(player, NotClanLeader);
                        return;
                    }

                    if (_config.PermissionSettings.UseClanDisband &&
                        !string.IsNullOrEmpty(_config.PermissionSettings.ClanDisband) &&
                        !permission.UserHasPermission(player.UserIDString, _config.PermissionSettings.ClanDisband))
                    {
                        Reply(player, NoPermDisbandClan);
                        return;
                    }

                    clan.Disband();
                    Reply(player, ClanDisbandedTitle);
                    break;
                }

                case "leave":
                {
                    var clan = FindClanByID(player.userID);
                    if (clan == null)
                    {
                        Reply(player, NotClanMember);
                        return;
                    }

                    if (_config.PermissionSettings.UseClanLeave &&
                        !string.IsNullOrEmpty(_config.PermissionSettings.ClanLeave) &&
                        !permission.UserHasPermission(player.UserIDString, _config.PermissionSettings.ClanLeave))
                    {
                        Reply(player, NoPermLeaveClan);
                        return;
                    }

                    clan.Kick(player.userID);
                    Reply(player, ClanLeft);
                    break;
                }

                case "promote":
                {
                    if (args.Length < 2)
                    {
                        SendReply(player, $"Error syntax! Use: /{command} {args[0]} <name/steamid>");
                        return;
                    }

                    var clan = FindClanByID(player.userID);
                    if (clan == null)
                    {
                        Reply(player, NotClanMember);
                        return;
                    }

                    if (!clan.IsOwner(player.userID))
                    {
                        Reply(player, NotClanLeader);
                        return;
                    }

                    var target = covalence.Players.FindPlayer(args[1]);
                    if (target == null)
                    {
                        Reply(player, PlayerNotFound, args[1]);
                        return;
                    }

                    if (clan.IsModerator(target.Id))
                    {
                        Reply(player, ClanAlreadyModer, target.Name);
                        return;
                    }

                    if (clan.Moderators.Count >= _config.LimitSettings.ModeratorLimit)
                    {
                        Reply(player, ALotOfModers);
                        return;
                    }

                    clan.SetModer(ulong.Parse(target.Id));
                    Reply(player, PromotedToModer, target.Name);
                    break;
                }

                case "demote":
                {
                    if (args.Length < 2)
                    {
                        SendReply(player, $"Error syntax! Use: /{command} {args[0]} <name/steamid>");
                        return;
                    }

                    var clan = FindClanByID(player.userID);
                    if (clan == null)
                    {
                        Reply(player, NotClanMember);
                        return;
                    }

                    if (!clan.IsOwner(player.userID))
                    {
                        Reply(player, NotClanLeader);
                        return;
                    }

                    var target = covalence.Players.FindPlayer(args[1]);
                    if (target == null)
                    {
                        Reply(player, PlayerNotFound, args[1]);
                        return;
                    }

                    if (!clan.IsModerator(target.Id))
                    {
                        Reply(player, NotClanModer, target.Name);
                        return;
                    }

                    clan.UndoModer(ulong.Parse(target.Id));
                    Reply(player, DemotedModer, target.Name);
                    break;
                }

                case "invite":
                {
                    if (args.Length < 2)
                    {
                        SendReply(player, $"Error syntax! Use: /{command} {args[0]} <name/steamid>");
                        return;
                    }

                    var clan = FindClanByID(player.userID);
                    if (clan == null)
                    {
                        Reply(player, NotClanMember);
                        return;
                    }

                    var target = covalence.Players.FindPlayer(args[1]);
                    if (target == null)
                    {
                        Reply(player, PlayerNotFound, args[1]);
                        return;
                    }

                    SendInvite(player, ulong.Parse(target.Id));
                    break;
                }

                case "withdraw":
                {
                    if (args.Length < 2)
                    {
                        SendReply(player, $"Error syntax! Use: /{command} {args[0]} <name/steamid>");
                        return;
                    }

                    var clan = FindClanByID(player.userID);
                    if (clan == null)
                    {
                        Reply(player, NotClanMember);
                        return;
                    }

                    var target = covalence.Players.FindPlayer(args[1]);
                    if (target == null)
                    {
                        Reply(player, PlayerNotFound, args[1]);
                        return;
                    }

                    WithdrawInvite(player, ulong.Parse(target.Id));
                    break;
                }

                case "kick":
                {
                    if (args.Length < 2)
                    {
                        SendReply(player, $"Error syntax! Use: /{command} {args[0]} <name/steamid>");
                        return;
                    }

                    if (_config.PermissionSettings.UseClanKick &&
                        !string.IsNullOrEmpty(_config.PermissionSettings.ClanKick) &&
                        !permission.UserHasPermission(player.UserIDString, _config.PermissionSettings.ClanKick))
                    {
                        Reply(player, _config.PermissionSettings.ClanKick);
                        return;
                    }

                    var clan = FindClanByID(player.userID);
                    if (clan == null)
                    {
                        Reply(player, NotClanMember);
                        return;
                    }

                    var target = covalence.Players.FindPlayer(args[1]);
                    if (target == null)
                    {
                        Reply(player, PlayerNotFound, args[1]);
                        return;
                    }

                    if (!clan.IsModerator(player.userID))
                    {
                        Reply(player, NotModer);
                        return;
                    }

                    clan.Kick(ulong.Parse(target.Id));
                    Reply(player, SuccsessKick, target.Name);

                    var targetPlayer = target.Object as BasePlayer;
                    if (targetPlayer != null) Reply(targetPlayer, WasKicked);
                    break;
                }

                case "allyinvite":
                {
                    if (args.Length < 2)
                    {
                        SendReply(player, $"Error syntax! Use: /{command} {args[0]} <clanTag>");
                        return;
                    }

                    var clan = FindClanByID(player.userID);
                    if (clan == null)
                    {
                        Reply(player, NotClanMember);
                        return;
                    }

                    if (!clan.IsModerator(player.userID))
                    {
                        Reply(player, NotModer);
                        return;
                    }

                    var targetClan = FindClanByTag(args[1]);
                    if (targetClan == null)
                    {
                        Reply(player, ClanNotFound, args[1]);
                        return;
                    }

                    AllySendInvite(player, targetClan.ClanTag);
                    break;
                }

                case "allywithdraw":
                {
                    if (args.Length < 2)
                    {
                        SendReply(player, $"Error syntax! Use: /{command} {args[0]} <clanTag>");
                        return;
                    }

                    var clan = FindClanByID(player.userID);
                    if (clan == null)
                    {
                        Reply(player, NotClanMember);
                        return;
                    }

                    if (!clan.IsModerator(player.userID))
                    {
                        Reply(player, NotModer);
                        return;
                    }

                    var targetClan = FindClanByTag(args[1]);
                    if (targetClan == null)
                    {
                        Reply(player, ClanNotFound, args[1]);
                        return;
                    }

                    AllyWithdrawInvite(player, targetClan.ClanTag);
                    break;
                }

                case "allyaccept":
                {
                    if (args.Length < 2)
                    {
                        SendReply(player, $"Error syntax! Use: /{command} {args[0]} <clanTag>");
                        return;
                    }

                    var clan = FindClanByID(player.userID);
                    if (clan == null)
                    {
                        Reply(player, NotClanMember);
                        return;
                    }

                    if (!clan.IsModerator(player.userID))
                    {
                        Reply(player, NotModer);
                        return;
                    }

                    var targetClan = FindClanByTag(args[1]);
                    if (targetClan == null)
                    {
                        Reply(player, ClanNotFound, args[1]);
                        return;
                    }

                    AllyAcceptInvite(player, targetClan.ClanTag);
                    break;
                }

                case "allycancel":
                {
                    if (args.Length < 2)
                    {
                        SendReply(player, $"Error syntax! Use: /{command} {args[0]} <clanTag>");
                        return;
                    }

                    var clan = FindClanByID(player.userID);
                    if (clan == null)
                    {
                        Reply(player, NotClanMember);
                        return;
                    }

                    if (!clan.IsModerator(player.userID))
                    {
                        Reply(player, NotModer);
                        return;
                    }

                    var targetClan = FindClanByTag(args[1]);
                    if (targetClan == null)
                    {
                        Reply(player, ClanNotFound, args[1]);
                        return;
                    }

                    AllyCancelInvite(player, targetClan.ClanTag);
                    break;
                }

                case "allyrevoke":
                {
                    if (args.Length < 2)
                    {
                        SendReply(player, $"Error syntax! Use: /{command} {args[0]} <clanTag>");
                        return;
                    }

                    var clan = FindClanByID(player.userID);
                    if (clan == null)
                    {
                        Reply(player, NotClanMember);
                        return;
                    }

                    if (!clan.IsModerator(player.userID))
                    {
                        Reply(player, NotModer);
                        return;
                    }

                    var targetClan = FindClanByTag(args[1]);
                    if (targetClan == null)
                    {
                        Reply(player, ClanNotFound, args[1]);
                        return;
                    }

                    AllyRevoke(player, targetClan.ClanTag);
                    break;
                }

                case "description":
                {
                    if (args.Length < 2)
                    {
                        SendReply(player, $"Error syntax! Use: /{command} {args[0]} <description>");
                        return;
                    }

                    var description = string.Join(" ", args.Skip(1));
                    if (string.IsNullOrEmpty(description)) return;

                    if (description.Length > _config.DescriptionMax)
                    {
                        Reply(player, MaxDescriptionSize);
                        return;
                    }

                    var clan = FindClanByID(player.userID);
                    if (clan == null)
                    {
                        Reply(player, NotClanMember);
                        return;
                    }

                    if (!clan.IsOwner(player.userID))
                    {
                        Reply(player, NotClanLeader);
                        return;
                    }

                    clan.Description = description;
                    Reply(player, SetDescription);
                    break;
                }

                default:
                {
                    var msg = Msg(player, Help);

                    var clan = FindClanByID(player.userID);
                    if (clan != null)
                    {
                        if (clan.IsModerator(player.userID)) msg += Msg(player, ModerHelp);

                        if (clan.IsOwner(player.userID)) msg += Msg(player, AdminHelp);
                    }

                    SendReply(player, msg);
                    break;
                }
            }
        }

        [ConsoleCommand("UI_Clans")]
        private void CmdConsoleClans(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player();
            if (player == null || !arg.HasArgs()) return;

            switch (arg.Args[0])
            {
                case "close_ui":
                {
                    ClanCreating.Remove(player);
                    break;
                }

                case "page":
                {
                    int page;
                    if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out page)) return;

                    var zPage = 0;
                    if (arg.HasArgs(3)) int.TryParse(arg.Args[2], out zPage);

                    var search = string.Empty;
                    if (arg.HasArgs(4))
                    {
                        search = string.Join(" ", arg.Args.Skip(3));

                        if (string.IsNullOrEmpty(search)) return;
                    }

                    MainUi(player, page, zPage, search);
                    break;
                }

                case "inputpage":
                {
                    int pages, page, zPage;
                    if (!arg.HasArgs(4) || !int.TryParse(arg.Args[1], out pages) ||
                        !int.TryParse(arg.Args[2], out page) || !int.TryParse(arg.Args[3], out zPage))
                        return;

                    if (zPage < 0) zPage = 0;

                    if (zPage >= pages) zPage = pages - 1;

                    MainUi(player, page, zPage);
                    break;
                }

                case "changeavatar":
                {
                    if (!arg.HasArgs(2)) return;

                    var url = string.Join(" ", arg.Args.Skip(1));
                    if (string.IsNullOrEmpty(url) || !Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
                        return;

                    var clan = FindClanByID(player.userID);
                    if (clan == null || !clan.IsOwner(player.userID)) return;

                    clan.Avatar = url;
                    ImageLibrary.Call("AddImage", url, url);

                    MainUi(player);
                    break;
                }

                case "invite":
                {
                    if (!arg.HasArgs(2)) return;

                    switch (arg.Args[1])
                    {
                        case "accept":
                        {
                            if (!arg.HasArgs(3)) return;

                            var tag = string.Join(" ", arg.Args.Skip(2));
                            if (string.IsNullOrEmpty(tag)) return;

                            AcceptInvite(player, tag);
                            break;
                        }
                        case "cancel":
                        {
                            if (!arg.HasArgs(3)) return;

                            var tag = string.Join(" ", arg.Args.Skip(2));
                            if (string.IsNullOrEmpty(tag)) return;

                            CancelInvite(player, tag);
                            break;
                        }
                        case "send":
                        {
                            ulong targetId;
                            if (!arg.HasArgs(3) || !ulong.TryParse(arg.Args[2], out targetId)) return;

                            SendInvite(player, targetId);

                            MainUi(player, 5);
                            break;
                        }

                        case "withdraw":
                        {
                            ulong targetId;
                            if (!arg.HasArgs(3) || !ulong.TryParse(arg.Args[2], out targetId)) return;

                            WithdrawInvite(player, targetId);

                            MainUi(player, 65);
                            break;
                        }
                    }

                    break;
                }

                case "allyinvite":
                {
                    if (!arg.HasArgs(2)) return;

                    switch (arg.Args[1])
                    {
                        case "accept":
                        {
                            if (!arg.HasArgs(3)) return;

                            AllyAcceptInvite(player, arg.Args[2]);
                            break;
                        }

                        case "cancel":
                        {
                            if (!arg.HasArgs(3)) return;

                            AllyCancelInvite(player, arg.Args[2]);
                            break;
                        }

                        case "send":
                        {
                            if (!arg.HasArgs(3)) return;

                            AllySendInvite(player, arg.Args[2]);

                            MainUi(player, 71);
                            break;
                        }

                        case "withdraw":
                        {
                            if (!arg.HasArgs(3)) return;

                            AllyWithdrawInvite(player, arg.Args[2]);

                            MainUi(player, 71);
                            break;
                        }

                        case "revoke":
                        {
                            if (!arg.HasArgs(3)) return;

                            AllyRevoke(player, arg.Args[2]);

                            MainUi(player, 7);
                            break;
                        }
                    }

                    break;
                }

                case "createclan":
                {
                    if (arg.HasArgs(2))
                        switch (arg.Args[1])
                        {
                            case "name":
                            {
                                if (!arg.HasArgs(3)) return;

                                var tag = string.Join(" ", arg.Args.Skip(2));
                                if (string.IsNullOrEmpty(tag) || tag.Length < _config.TagMin ||
                                    tag.Length > _config.TagMax)
                                {
                                    Reply(player, ClanTagLimit, _config.TagMin, _config.TagMax);
                                    return;
                                }

                                ClanCreating[player].Tag = tag;
                                break;
                            }
                            case "avatar":
                            {
                                if (!arg.HasArgs(3)) return;

                                var avatar = string.Join(" ", arg.Args.Skip(2));
                                if (string.IsNullOrEmpty(avatar)) return;

                                ClanCreating[player].Avatar = avatar;
                                break;
                            }
                            case "create":
                            {
                                var clanTag = ClanCreating[player].Tag;
                                if (!ClanCreating.ContainsKey(player) || string.IsNullOrEmpty(clanTag)) return;

                                if (_config.PermissionSettings.UseClanCreating &&
                                    !string.IsNullOrEmpty(_config.PermissionSettings.ClanCreating) &&
                                    !permission.UserHasPermission(player.UserIDString,
                                        _config.PermissionSettings.ClanCreating))
                                {
                                    Reply(player, NoPermCreateClan);
                                    return;
                                }

                                var clan = FindClanByTag(clanTag);
                                if (clan != null)
                                {
                                    Reply(player, ClanExists);
                                    return;
                                }

                                clan = ClanData.CreateNewClan(clanTag, player);
                                if (clan == null) return;

                                clanTag = clanTag.Replace(" ", "");

                                if (_config.BlockedWords.Exists(word =>
                                    clanTag.Contains(word, CompareOptions.OrdinalIgnoreCase)))
                                {
                                    Reply(player, ContainsForbiddenWords);
                                    return;
                                }

                                if (!string.IsNullOrEmpty(ClanCreating[player].Avatar))
                                    ImageLibrary.Call("AddImage", ClanCreating[player].Avatar,
                                        ClanCreating[player].Avatar);

                                ClanCreating.Remove(player);
                                Reply(player, ClanCreated, clanTag);
                                return;
                            }
                        }

                    CreateClanUi(player);
                    break;
                }

                case "edititem":
                {
                    int slot;
                    if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out slot)) return;

                    SelectItemUi(player, slot);
                    break;
                }

                case "selectpages":
                {
                    int slot, page, amount;
                    if (!arg.HasArgs(4) || !int.TryParse(arg.Args[1], out slot) ||
                        !int.TryParse(arg.Args[2], out page) || !int.TryParse(arg.Args[3], out amount))
                        return;

                    var search = string.Empty;
                    if (arg.HasArgs(5)) search = string.Join(" ", arg.Args.Skip(4));

                    SelectItemUi(player, slot, page, amount, search);
                    break;
                }

                case "setamountitem":
                {
                    int slot, amount;
                    if (!arg.HasArgs(3) || !int.TryParse(arg.Args[1], out slot) ||
                        !int.TryParse(arg.Args[2], out amount) || amount <= 0)
                        return;

                    var clan = FindClanByID(player.userID);
                    if (clan == null || !clan.IsOwner(player.userID)) return;

                    if (clan.ResourceStandarts.ContainsKey(slot)) clan.ResourceStandarts[slot].Amount = amount;

                    SelectItemUi(player, slot, amount: amount);
                    break;
                }

                case "selectitem":
                {
                    int slot, amount;
                    if (!arg.HasArgs(4) || !int.TryParse(arg.Args[1], out slot) ||
                        !int.TryParse(arg.Args[3], out amount))
                        return;

                    var shortName = arg.Args[2];
                    if (string.IsNullOrEmpty(shortName)) return;

                    var clan = FindClanByID(player.userID);
                    if (clan == null || !clan.IsOwner(player.userID)) return;

                    if (clan.ResourceStandarts.ContainsKey(slot))
                    {
                        clan.ResourceStandarts[slot].ShortName = shortName;
                        clan.ResourceStandarts[slot].Amount = amount;
                    }
                    else
                    {
                        clan.ResourceStandarts.Add(slot, new ResourceStandart {Amount = amount, ShortName = shortName});
                    }

                    MainUi(player, 4);
                    break;
                }

                case "editskin":
                {
                    int slot;
                    if (!arg.HasArgs(2)) return;

                    var page = 0;
                    if (arg.HasArgs(3)) int.TryParse(arg.Args[2], out page);

                    SelectSkinUi(player, arg.Args[1], page);
                    break;
                }

                case "setskin":
                {
                    ulong skin;
                    if (!arg.HasArgs(3) || !ulong.TryParse(arg.Args[2], out skin)) return;

                    var clan = FindClanByID(player.userID);
                    if (clan == null) return;

                    clan.SetSkin(arg.Args[1], skin);

                    SelectSkinUi(player, arg.Args[1]);
                    break;
                }

                case "selectskin":
                {
                    ulong skin;
                    if (!arg.HasArgs(3) || !ulong.TryParse(arg.Args[2], out skin)) return;

                    var clan = FindClanByID(player.userID);
                    if (clan == null) return;

                    clan.SetSkin(arg.Args[1], skin);

                    MainUi(player, 6);
                    break;
                }

                case "showprofile":
                {
                    ulong target;
                    if (!arg.HasArgs(2) || !ulong.TryParse(arg.Args[1], out target)) return;

                    ProfileUi(player, target);
                    break;
                }

                case "showclanprofile":
                {
                    ulong target;
                    if (!arg.HasArgs(2) || !ulong.TryParse(arg.Args[1], out target)) return;

                    ClanMemberProfileUi(player, target);
                    break;
                }

                case "moder":
                {
                    if (!arg.HasArgs(2)) return;

                    var clan = FindClanByID(player.userID);
                    if (clan == null) return;

                    switch (arg.Args[1])
                    {
                        case "set":
                        {
                            ulong target;
                            if (!arg.HasArgs(3) || !ulong.TryParse(arg.Args[2], out target)) return;

                            if (clan.Moderators.Count >= _config.LimitSettings.ModeratorLimit)
                            {
                                CuiHelper.DestroyUi(player, Layer);

                                Reply(player, ALotOfModers);
                                return;
                            }

                            clan.SetModer(target);

                            ClanMemberProfileUi(player, target);
                            break;
                        }

                        case "undo":
                        {
                            ulong target;
                            if (!arg.HasArgs(3) || !ulong.TryParse(arg.Args[2], out target)) return;

                            clan.UndoModer(target);

                            ClanMemberProfileUi(player, target);
                            break;
                        }
                    }

                    break;
                }

                case "leader":
                {
                    if (!arg.HasArgs(2)) return;

                    var clan = FindClanByID(player.userID);
                    if (clan == null) return;

                    switch (arg.Args[1])
                    {
                        case "tryset":
                        {
                            ulong target;
                            if (!arg.HasArgs(3) || !ulong.TryParse(arg.Args[2], out target)) return;

                            AcceptSetLeader(player, target);
                            break;
                        }

                        case "set":
                        {
                            ulong target;
                            if (!arg.HasArgs(3) || !ulong.TryParse(arg.Args[2], out target)) return;

                            clan.SetLeader(target);

                            ClanMemberProfileUi(player, target);
                            break;
                        }
                    }

                    break;
                }

                case "kick":
                {
                    ulong target;
                    if (!arg.HasArgs(2) || !ulong.TryParse(arg.Args[1], out target)) return;

                    if (_config.PermissionSettings.UseClanKick &&
                        !string.IsNullOrEmpty(_config.PermissionSettings.ClanKick) &&
                        !permission.UserHasPermission(player.UserIDString, _config.PermissionSettings.ClanKick))
                    {
                        Reply(player, _config.PermissionSettings.ClanKick);
                        return;
                    }

                    var clan = FindClanByID(player.userID);
                    if (clan == null) return;

                    clan.Kick(target);
                    break;
                }

                case "showclan":
                {
                    if (!arg.HasArgs(2)) return;

                    var tag = arg.Args[1];
                    if (string.IsNullOrEmpty(tag)) return;

                    ClanProfileUi(player, tag);
                    break;
                }

                case "ff":
                {
                    int page;
                    if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out page)) return;

                    var data = GetPlayerData(player.userID);
                    if (data == null) return;

                    data.FriendlyFire = !data.FriendlyFire;

                    MainUi(player, page);
                    break;
                }

                case "allyff":
                {
                    int page;
                    if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out page)) return;

                    var data = GetPlayerData(player.userID);
                    if (data == null) return;

                    data.AllyFriendlyFire = !data.AllyFriendlyFire;

                    MainUi(player, page);
                    break;
                }

                case "description":
                {
                    if (!arg.HasArgs(2)) return;

                    var description = string.Join(" ", arg.Args.Skip(1));
                    if (string.IsNullOrEmpty(description)) return;

                    if (description.Length > _config.DescriptionMax)
                    {
                        Reply(player, MaxDescriptionSize);
                        return;
                    }

                    var clan = FindClanByID(player.userID);
                    if (clan == null)
                    {
                        Reply(player, NotClanMember);
                        return;
                    }

                    if (!clan.IsOwner(player.userID))
                    {
                        Reply(player, NotClanLeader);
                        return;
                    }

                    clan.Description = description;

                    MainUi(player);

                    Reply(player, SetDescription);
                    break;
                }
            }
        }

        [ConsoleCommand("clans.loadavatars")]
        private void CmdConsoleLoadAvatars(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;

            StartLoadingAvatars();
        }

        [ConsoleCommand("clans.refreshtop")]
        private void CmdRefreshTop(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;

            HandleTop();
        }

        [ConsoleCommand("clans.refreshskins")]
        private void CmdConsoleRefreshSkins(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;

            foreach (var itemSkin in _config.Skins.ItemSkins) itemSkin.Value.Clear();

            LoadSkins();

            Puts(
                $"{_config.Skins.ItemSkins.Sum(x => x.Value.Count)} skins for {_config.Skins.ItemSkins.Count} items uploaded successfully!");
        }

        #endregion

        #region Interface

        private void MainUi(BasePlayer player, int page = 0, int zPage = 0, string search = "", bool First = false)
        {
            #region Fields

            var xSwitch = 0f;
            var ySwitch = 0f;
            var Height = 0f;
            var Width = 0f;
            var Margin = 0f;
            var AmountOnString = 0;
            var Strings = 0;
            var totalAmount = 0;

            var clan = FindClanByID(player.userID);

            var data = GetPlayerData(player);

            #endregion

            var container = new CuiElementContainer();

            #region Background

            if (First)
            {
                CuiHelper.DestroyUi(player, Layer);

                container.Add(
                    new CuiPanel
                    {
                        //RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                        Image = {Color = "0 0 0 0", Material = ""},
                        CursorEnabled = false,
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0",OffsetMin = "465 115",OffsetMax = "1145 550"}
                }, "SubContent_UI", Layer);

                /*container.Add(
                    new CuiButton
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                        Text = {Text = ""},
                        Button = {Color = "0 0 0 0", Close = Layer,Command = "chat.say /profile"}
                    }, Layer);*/
            }

            #endregion

            #region Main

            container.Add(
                new CuiPanel
                {
                    Image = {Color = Color1},
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0",OffsetMin = "465 115",OffsetMax = "1145 550"}
                }, "SubContent_UI", Layer + ".Main");

            #region Header

            HeaderUi(ref container, player, clan, page, Msg(player, ClansMenuTitle));

            #endregion

            #region Menu

            MenuUi(ref container, player, page, clan);

            #endregion

            #region Content

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "195 0", OffsetMax = "0 -55"
                    },
                    Image = {Color = "0 0 0 0"}
                }, Layer + ".Main", Layer + ".Second.Main");

            container.Add(
                new CuiPanel {RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"}, Image = {Color = "0 0 0 0"}},
                Layer + ".Second.Main", Layer + ".Content");

            if (clan != null || page == 45)
                switch (page)
                {
                    case 0:
                    {
                        #region Title

                        container.Add(
                            new CuiLabel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = "2.5 -30",
                                    OffsetMax = "225 0"
                                },
                                Text =
                                {
                                    Text = Msg(player, AboutClan),
                                    Align = TextAnchor.MiddleLeft,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 12,
                                    Color = "1 1 1 1"
                                }
                            }, Layer + ".Content");

                        #endregion

                        #region Avatar

                        container.Add(new CuiElement
                        {
                            Parent = Layer + ".Content",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = ImageLibrary.Call<string>("GetImage", clan.Avatar)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = "0 -170",
                                    OffsetMax = "140 -30"
                                }
                            }
                        });

                        if (clan.IsOwner(player.userID))
                        {
                            #region Change avatar

                            container.Add(
                                new CuiButton
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = "0 -200",
                                        OffsetMax = "140 -175"
                                    },
                                    Text =
                                    {
                                        Text = Msg(player, ChangeAvatar),
                                        Align = TextAnchor.MiddleCenter,
                                        Font = "robotocondensed-regular.ttf",
                                        FontSize = 12,
                                        Color = "1 1 1 1"
                                    },
                                    Button = {Color = Color2, Command = $"UI_Clans changeavatar {search}"}
                                }, Layer + ".Content");

                            #endregion

                            #region Input URL

                            container.Add(
                                new CuiPanel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = "0 -230",
                                        OffsetMax = "140 -205"
                                    },
                                    Image = {Color = Color3}
                                }, Layer + ".Content", Layer + ".Avatar.Input");

                            container.Add(
                                new CuiLabel
                                {
                                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                                    Text =
                                    {
                                        Text = string.IsNullOrEmpty(search)
                                            ? Msg(player, EnterLink)
                                            : $"{search}",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 12,
                                        Font = "robotocondensed-regular.ttf",
                                        Color = "1 1 1 0.55"
                                    }
                                }, Layer + ".Avatar.Input");

                            container.Add(new CuiElement
                            {
                                Parent = Layer + ".Avatar.Input",
                                Components =
                                {
                                    new CuiInputFieldComponent
                                    {
                                        FontSize = 12,
                                        Align = TextAnchor.MiddleCenter,
                                        Font = "robotocondensed-regular.ttf",
                                        Command = $"UI_Clans page {page} 0 ",
                                        Color = "1 1 1 0.95",
                                        CharsLimit = 128
                                    },
                                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                                }
                            });

                            #endregion

                            #region Disband
                            
                            if (clan.IsOwner(player.userID))
                            container.Add(new CuiButton
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = "0 -250",
                                    OffsetMax = "140 -235"
                                },
                                Text =
                                {
                                    Text = "Расформировать",
                                    Align = TextAnchor.MiddleCenter,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 12,
                                    Color = "1 1 1 1"
                                },
                                Button = {Color = Color2, Command = $"clan disband",Close = Layer}
                            }, Layer + ".Content");

                            #endregion
                        }

                        #endregion

                        #region Clan Name

                        container.Add(
                            new CuiLabel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = "160 -50",
                                    OffsetMax = "400 -30"
                                },
                                Text =
                                {
                                    Text = $"{clan.ClanTag}",
                                    Align = TextAnchor.MiddleLeft,
                                    Font = "robotocondensed-bold.ttf",
                                    FontSize = 16,
                                    Color = "1 1 1 1"
                                }
                            }, Layer + ".Content");

                        #endregion

                        #region Clan Leader

                        container.Add(
                            new CuiPanel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = "160 -105",
                                    OffsetMax = "460 -75"
                                },
                                Image = {Color = Color3}
                            }, Layer + ".Content", Layer + ".Clan.Leader");

                        container.Add(
                            new CuiLabel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"
                                },
                                Text =
                                {
                                    Text = Msg(player, LeaderTitle),
                                    Align = TextAnchor.MiddleLeft,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 10,
                                    Color = "1 1 1 1"
                                }
                            }, Layer + ".Clan.Leader");

                        container.Add(
                            new CuiLabel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "10 0", OffsetMax = "0 0"
                                },
                                Text =
                                {
                                    Text = $"{clan.LeaderName}",
                                    Align = TextAnchor.MiddleLeft,
                                    Font = "robotocondensed-bold.ttf",
                                    FontSize = 12,
                                    Color = "1 1 1 1"
                                }
                            }, Layer + ".Clan.Leader");

                        #endregion

                        #region Farm

                        container.Add(
                            new CuiPanel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = "160 -165",
                                    OffsetMax = "460 -135"
                                },
                                Image = {Color = Color3}
                            }, Layer + ".Content", Layer + ".Clan.Farm");

                        container.Add(
                            new CuiLabel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"
                                },
                                Text =
                                {
                                    Text = Msg(player, GatherTitle),
                                    Align = TextAnchor.MiddleLeft,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 10,
                                    Color = "1 1 1 1"
                                }
                            }, Layer + ".Clan.Farm");

                        var progress = clan.GetTotalFarm();

                        if (progress > 0)
                            container.Add(
                                new CuiPanel
                                {
                                    RectTransform = {AnchorMin = "0 0", AnchorMax = $"{progress} 0.95"},
                                    Image = {Color = Color2}
                                }, Layer + ".Clan.Farm", Layer + ".Clan.Farm.Progress");

                        container.Add(
                            new CuiLabel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "-5 0"
                                },
                                Text =
                                {
                                    Text = $"{(progress > 0 ? Math.Round(progress * 100f) : 0)}%",
                                    Align = TextAnchor.MiddleCenter,
                                    Font = "robotocondensed-bold.ttf",
                                    FontSize = 12,
                                    Color = "1 1 1 1"
                                }
                            }, Layer + ".Clan.Farm");

                        #endregion

                        #region Rating

                        container.Add(
                            new CuiPanel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = "160 -225",
                                    OffsetMax = "300 -195"
                                },
                                Image = {Color = Color3}
                            }, Layer + ".Content", Layer + ".Clan.Rating");

                        container.Add(
                            new CuiLabel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"
                                },
                                Text =
                                {
                                    Text = Msg(player, RatingTitle),
                                    Align = TextAnchor.MiddleLeft,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 10,
                                    Color = "1 1 1 1"
                                }
                            }, Layer + ".Clan.Rating");

                        container.Add(
                            new CuiLabel
                            {
                                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                                Text =
                                {
                                    Text = $"{clan.Top}",
                                    Align = TextAnchor.MiddleCenter,
                                    Font = "robotocondensed-bold.ttf",
                                    FontSize = 12,
                                    Color = "1 1 1 1"
                                }
                            }, Layer + ".Clan.Rating");

                        #endregion

                        #region Members

                        container.Add(
                            new CuiPanel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = "320 -225",
                                    OffsetMax = "460 -195"
                                },
                                Image = {Color = Color3}
                            }, Layer + ".Content", Layer + ".Clan.Members");

                        container.Add(
                            new CuiLabel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"
                                },
                                Text =
                                {
                                    Text = Msg(player, MembersTitle),
                                    Align = TextAnchor.MiddleLeft,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 10,
                                    Color = "1 1 1 1"
                                }
                            }, Layer + ".Clan.Members");

                        container.Add(
                            new CuiLabel
                            {
                                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                                Text =
                                {
                                    Text = $"{clan.Members.Count}",
                                    Align = TextAnchor.MiddleCenter,
                                    Font = "robotocondensed-bold.ttf",
                                    FontSize = 12,
                                    Color = "1 1 1 1"
                                }
                            }, Layer + ".Clan.Members");

                        #endregion

                        #region Task

                        container.Add(
                            new CuiPanel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 0",
                                    AnchorMax = "0 0",
                                    OffsetMin = "0 10",
                                    OffsetMax = "460 90"
                                },
                                Image = {Color = Color3}
                            }, Layer + ".Content", Layer + ".Clan.Task");

                        container.Add(
                            new CuiLabel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"
                                },
                                Text =
                                {
                                    Text = Msg(player, DescriptionTitle),
                                    Align = TextAnchor.MiddleLeft,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 10,
                                    Color = "1 1 1 1"
                                }
                            }, Layer + ".Clan.Task");

                        container.Add(
                            new CuiLabel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5"
                                },
                                Text =
                                {
                                    Text = string.IsNullOrEmpty(clan.Description)
                                        ? Msg(player, NotDescription)
                                        : $"{clan.Description}",
                                    Align = TextAnchor.MiddleCenter,
                                    Font = "robotocondensed-bold.ttf",
                                    FontSize = 12,
                                    Color = "1 1 1 1"
                                }
                            }, Layer + ".Clan.Task");

                        if (clan.IsOwner(player.userID))
                            container.Add(new CuiElement
                            {
                                Parent = Layer + ".Clan.Task",
                                Components =
                                {
                                    new CuiInputFieldComponent
                                    {
                                        FontSize = 12,
                                        Align = TextAnchor.MiddleCenter,
                                        Font = "robotocondensed-regular.ttf",
                                        Command = "UI_Clans description ",
                                        Color = "1 1 1 0.95",
                                        CharsLimit = _config.DescriptionMax
                                    },
                                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                                }
                            });

                        #endregion

                        break;
                    }

                    case 1:
                    {
                        AmountOnString = 2;
                        Strings = 8;
                        totalAmount = AmountOnString * Strings;
                        ySwitch = 0f;
                        Height = 35f;
                        Width = 237.5f;
                        Margin = 5f;

                        var z = 1;

                        var availablePlayers = clan.Members.FindAll(member =>
                            {
                                var memberData = GetPlayerData(member);
                                if (memberData == null) return false;

                                return string.IsNullOrEmpty(search) || search.Length <= 2 ||
                                       memberData.DisplayName.StartsWith(search) ||
                                       memberData.DisplayName.Contains(search) ||
                                       memberData.DisplayName.EndsWith(search);
                            })
                            .ToArray();

                        foreach (var member in availablePlayers.Skip(zPage * totalAmount).Take(totalAmount))
                        {
                            xSwitch = z % AmountOnString == 0 ? Margin + Width : 0;

                            container.Add(
                                new CuiPanel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"{xSwitch} {ySwitch - Height}",
                                        OffsetMax = $"{xSwitch + Width} {ySwitch}"
                                    },
                                    Image = {Color = Color3}
                                }, Layer + ".Content", Layer + $".Player.{member}");

                            container.Add(new CuiElement
                            {
                                Parent = Layer + $".Player.{member}",
                                Components =
                                {
                                    new CuiRawImageComponent
                                    {
                                        Png = ImageLibrary.Call<string>("GetImage", $"avatar_{member}")
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 0",
                                        AnchorMax = "0 0",
                                        OffsetMin = "0 0",
                                        OffsetMax = $"{Height} {Height}"
                                    }
                                }
                            });

                            #region Display Name

                            container.Add(
                                new CuiLabel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 0.5",
                                        AnchorMax = "0 1",
                                        OffsetMin = "40 1",
                                        OffsetMax = "95 0"
                                    },
                                    Text =
                                    {
                                        Text = Msg(player, NameTitle),
                                        Align = TextAnchor.LowerLeft,
                                        Font = "robotocondensed-regular.ttf",
                                        FontSize = 10,
                                        Color = "1 1 1 1"
                                    }
                                }, Layer + $".Player.{member}");

                            container.Add(
                                new CuiLabel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 0",
                                        AnchorMax = "0 0.5",
                                        OffsetMin = "40 0",
                                        OffsetMax = "100 -1"
                                    },
                                    Text =
                                    {
                                        Text = $"{GetPlayerData(member)?.DisplayName}",
                                        Align = TextAnchor.UpperLeft,
                                        Font = "robotocondensed-bold.ttf",
                                        FontSize = 10,
                                        Color = "1 1 1 1"
                                    }
                                }, Layer + $".Player.{member}");

                            #endregion

                            #region SteamId

                            container.Add(
                                new CuiLabel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 0.5",
                                        AnchorMax = "0 1",
                                        OffsetMin = "95 1",
                                        OffsetMax = "210 0"
                                    },
                                    Text =
                                    {
                                        Text = Msg(player, SteamIdTitle),
                                        Align = TextAnchor.LowerLeft,
                                        Font = "robotocondensed-regular.ttf",
                                        FontSize = 10,
                                        Color = "1 1 1 1"
                                    }
                                }, Layer + $".Player.{member}");

                            container.Add(
                                new CuiLabel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 0",
                                        AnchorMax = "0 0.5",
                                        OffsetMin = "95 0",
                                        OffsetMax = "210 -1"
                                    },
                                    Text =
                                    {
                                        Text = $"{member}",
                                        Align = TextAnchor.UpperLeft,
                                        Font = "robotocondensed-bold.ttf",
                                        FontSize = 10,
                                        Color = "1 1 1 1"
                                    }
                                }, Layer + $".Player.{member}");

                            #endregion

                            #region Button

                            container.Add(
                                new CuiButton
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "1 0.5",
                                        AnchorMax = "1 0.5",
                                        OffsetMin = "-45 -8",
                                        OffsetMax = "-5 8"
                                    },
                                    Text =
                                    {
                                        Text = Msg(player, ProfileTitle),
                                        Align = TextAnchor.MiddleCenter,
                                        Font = "robotocondensed-regular.ttf",
                                        FontSize = 10,
                                        Color = "1 1 1 1"
                                    },
                                    Button = {Color = Color2, Command = $"UI_Clans showclanprofile {member}"}
                                }, Layer + $".Player.{member}");

                            #endregion

                            if (z % AmountOnString == 0) ySwitch = ySwitch - Height - Margin;

                            z++;
                        }

                        #region Search

                        container.Add(
                            new CuiPanel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0.5 0",
                                    AnchorMax = "0.5 0",
                                    OffsetMin = "-140 20",
                                    OffsetMax = "60 55"
                                },
                                Image = {Color = Color4}
                            }, Layer + ".Content", Layer + ".Search");

                        container.Add(
                            new CuiLabel
                            {
                                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                                Text =
                                {
                                    Text =
                                        string.IsNullOrEmpty(search) ? Msg(player, SearchTitle) : $"{search}",
                                    Align = TextAnchor.MiddleCenter,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 12,
                                    Color = "1 1 1 0.65"
                                }
                            }, Layer + ".Search");

                        container.Add(new CuiElement
                        {
                            Parent = Layer + ".Search",
                            Components =
                            {
                                new CuiInputFieldComponent
                                {
                                    FontSize = 12,
                                    Align = TextAnchor.MiddleCenter,
                                    Font = "robotocondensed-regular.ttf",
                                    Command = $"UI_Clans page {page} 0 ",
                                    Color = "1 1 1 0.95",
                                    CharsLimit = 32
                                },
                                new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                            }
                        });

                        #endregion

                        #region Pages

                        container.Add(
                            new CuiButton
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0.5 0",
                                    AnchorMax = "0.5 0",
                                    OffsetMin = "65 20",
                                    OffsetMax = "100 55"
                                },
                                Text =
                                {
                                    Text = Msg(player, BackPage),
                                    Align = TextAnchor.MiddleCenter,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 12,
                                    Color = "1 1 1 1"
                                },
                                Button =
                                {
                                    Color = Color4,
                                    Command = zPage != 0 ? $"UI_Clans page {page} {zPage - 1} {search}" : ""
                                }
                            }, Layer + ".Content");

                        container.Add(
                            new CuiButton
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0.5 0",
                                    AnchorMax = "0.5 0",
                                    OffsetMin = "105 20",
                                    OffsetMax = "140 55"
                                },
                                Text =
                                {
                                    Text = Msg(player, NextPage),
                                    Align = TextAnchor.MiddleCenter,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 12,
                                    Color = "1 1 1 1"
                                },
                                Button =
                                {
                                    Color = Color2,
                                    Command = availablePlayers.Length > (zPage + 1) * totalAmount
                                        ? $"UI_Clans page {page} {zPage + 1} {search}"
                                        : ""
                                }
                            }, Layer + ".Content");

                        #endregion

                        break;
                    }

                    case 2:
                    {
                        #region Title

                        container.Add(
                            new CuiLabel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = "2.5 -30",
                                    OffsetMax = "225 0"
                                },
                                Text =
                                {
                                    Text = Msg(player, TopClansTitle),
                                    Align = TextAnchor.MiddleLeft,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 12,
                                    Color = "1 1 1 1"
                                }
                            }, Layer + ".Content");

                        #endregion

                        #region Head

                        ySwitch = 0;

                        _config.UI.TopClansColumns.ForEach(column =>
                        {
                            container.Add(
                                new CuiLabel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"{ySwitch} -50",
                                        OffsetMax = $"{ySwitch + column.Width} -30"
                                    },
                                    Text =
                                    {
                                        Text = Msg(player, column.LangKey),
                                        Align = column.TextAlign,
                                        Font = "robotocondensed-regular.ttf",
                                        FontSize = column.TitleFontSize,
                                        Color = "1 1 1 1"
                                    }
                                }, Layer + ".Content");

                            ySwitch += column.Width;
                        });

                        #endregion

                        #region Table

                        ySwitch = -50;
                        Height = 37.5f;
                        Margin = 2.5f;
                        totalAmount = 7;

                        var i = 0;
                        foreach (var topClan in TopClans.Skip(zPage * totalAmount).Take(totalAmount))
                        {
                            var top = zPage * totalAmount + i + 1;

                            container.Add(
                                new CuiPanel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"0 {ySwitch - Height}",
                                        OffsetMax = $"480 {ySwitch}"
                                    },
                                    Image = {Color = Color3}
                                }, Layer + ".Content", Layer + $".TopClan.{i}");

                            var localSwitch = 0f;
                            _config.UI.TopClansColumns.ForEach(column =>
                            {
                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "0 1",
                                            OffsetMin = $"{localSwitch} 0",
                                            OffsetMax = $"{localSwitch + column.Width} 0"
                                        },
                                        Text =
                                        {
                                            Text = $"{column.GetKey(top, topClan.GetParams(column.Key))}",
                                            Align = column.TextAlign,
                                            Font = "robotocondensed-bold.ttf",
                                            FontSize = column.FontSize,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".TopClan.{i}");

                                localSwitch += column.Width;
                            });

                            container.Add(
                                new CuiButton
                                {
                                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                                    Text = {Text = ""},
                                    Button =
                                    {
                                        Color = "0 0 0 0",
                                        Command = topClan == clan
                                            ? "UI_Clans page 0"
                                            : $"UI_Clans showclan {topClan.ClanTag}"
                                    }
                                }, Layer + $".TopClan.{i}");

                            ySwitch = ySwitch - Height - Margin;

                            i++;
                        }

                        #endregion

                        #region Pages

                        PagesUi(ref container, player, (int) Math.Ceiling((double) TopClans.Count / totalAmount), page,
                            zPage);

                        #endregion

                        break;
                    }

                    case 3:
                    {
                        #region Title

                        container.Add(
                            new CuiLabel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = "2.5 -30",
                                    OffsetMax = "225 0"
                                },
                                Text =
                                {
                                    Text = Msg(player, TopPlayersTitle),
                                    Align = TextAnchor.MiddleLeft,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 12,
                                    Color = "1 1 1 1"
                                }
                            }, Layer + ".Content");

                        #endregion

                        #region Head

                        ySwitch = 0;

                        _config.UI.TopPlayersColumns.ForEach(column =>
                        {
                            container.Add(
                                new CuiLabel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"{ySwitch} -50",
                                        OffsetMax = $"{ySwitch + column.Width} -30"
                                    },
                                    Text =
                                    {
                                        Text = Msg(player, column.LangKey),
                                        Align = column.TextAlign,
                                        Font = "robotocondensed-regular.ttf",
                                        FontSize = column.TitleFontSize,
                                        Color = "1 1 1 1"
                                    }
                                }, Layer + ".Content");

                            ySwitch += column.Width;
                        });

                        #endregion

                        #region Table

                        ySwitch = -50;
                        Height = 37.5f;
                        Margin = 2.5f;
                        totalAmount = 7;

                        var i = 0;
                        foreach (var topPlayer in TopPlayers.Skip(zPage * totalAmount).Take(totalAmount))
                        {
                            var top = zPage * totalAmount + i + 1;

                            container.Add(
                                new CuiPanel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"0 {ySwitch - Height}",
                                        OffsetMax = $"480 {ySwitch}"
                                    },
                                    Image = {Color = Color3}
                                }, Layer + ".Content", Layer + $".TopPlayer.{i}");

                            var localSwitch = 0f;
                            _config.UI.TopPlayersColumns.ForEach(column =>
                            {
                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "0 1",
                                            OffsetMin = $"{localSwitch} 0",
                                            OffsetMax = $"{localSwitch + column.Width} 0"
                                        },
                                        Text =
                                        {
                                            Text =
                                                $"{column.GetKey(top, topPlayer.Value.GetParams(column.Key))}",
                                            Align = column.TextAlign,
                                            Font = "robotocondensed-bold.ttf",
                                            FontSize = column.FontSize,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".TopPlayer.{i}");

                                localSwitch += column.Width;
                            });

                            container.Add(
                                new CuiButton
                                {
                                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                                    Text = {Text = ""},
                                    Button = {Color = "0 0 0 0", Command = $"UI_Clans showprofile {topPlayer.Key}"}
                                }, Layer + $".TopPlayer.{i}");

                            ySwitch = ySwitch - Height - Margin;

                            i++;
                        }

                        #endregion

                        #region Pages

                        PagesUi(ref container, player, (int) Math.Ceiling((double) TopPlayers.Count / totalAmount),
                            page, zPage);

                        #endregion

                        break;
                    }

                    case 4:
                    {
                        AmountOnString = 4;
                        Strings = 3;
                        totalAmount = AmountOnString * Strings;

                        Height = 115;
                        Width = 115;
                        Margin = 5;

                        xSwitch = 0;
                        ySwitch = 0;

                        if (clan.IsOwner(player.userID))
                        {
                            for (var i = 0; i < totalAmount; i++)
                            {
                                var founded = clan.ResourceStandarts.ContainsKey(i);

                                container.Add(
                                    new CuiPanel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 1",
                                            AnchorMax = "0 1",
                                            OffsetMin = $"{xSwitch} {ySwitch - Height}",
                                            OffsetMax = $"{xSwitch + Width} {ySwitch}"
                                        },
                                        Image = {Color = founded ? Color3 : Color4}
                                    }, Layer + ".Content", Layer + $".ResourсeStandart.{i}");

                                if (founded)
                                {
                                    var standart = clan.ResourceStandarts[i];
                                    if (standart == null) continue;

                                    container.Add(new CuiElement
                                    {
                                        Parent = Layer + $".ResourсeStandart.{i}",
                                        Components =
                                        {
                                            new CuiRawImageComponent
                                            {
                                                Png = ImageLibrary.Call<string>("GetImage",
                                                    standart.ShortName)
                                            },
                                            new CuiRectTransformComponent
                                            {
                                                AnchorMin = "0.5 1",
                                                AnchorMax = "0.5 1",
                                                OffsetMin = "-30 -70",
                                                OffsetMax = "30 -10"
                                            }
                                        }
                                    });

                                    #region Progress Text

                                    var done = data.GetValue(standart.ShortName);

                                    if (done < standart.Amount)
                                    {
                                        container.Add(
                                            new CuiLabel
                                            {
                                                RectTransform =
                                                {
                                                    AnchorMin = "0.5 1",
                                                    AnchorMax = "0.5 1",
                                                    OffsetMin = "-55 -85",
                                                    OffsetMax = "55 -75"
                                                },
                                                Text =
                                                {
                                                    Text = Msg(player, LeftTitle),
                                                    Align = TextAnchor.MiddleLeft,
                                                    Font = "robotocondensed-regular.ttf",
                                                    FontSize = 10,
                                                    Color = "1 1 1 0.35"
                                                }
                                            }, Layer + $".ResourсeStandart.{i}");

                                        container.Add(
                                            new CuiLabel
                                            {
                                                RectTransform =
                                                {
                                                    AnchorMin = "0.5 1",
                                                    AnchorMax = "0.5 1",
                                                    OffsetMin = "-55 -100",
                                                    OffsetMax = "55 -85"
                                                },
                                                Text =
                                                {
                                                    Text = $"{done} / {standart.Amount}",
                                                    Align = TextAnchor.MiddleCenter,
                                                    Font = "robotocondensed-bold.ttf",
                                                    FontSize = 12,
                                                    Color = "1 1 1 1"
                                                }
                                            }, Layer + $".ResourсeStandart.{i}");
                                    }

                                    #endregion

                                    #region Progress Bar

                                    container.Add(
                                        new CuiPanel
                                        {
                                            RectTransform =
                                            {
                                                AnchorMin = "0 0",
                                                AnchorMax = "1 0",
                                                OffsetMin = "0 0",
                                                OffsetMax = "0 10"
                                            },
                                            Image = {Color = Color4}
                                        }, Layer + $".ResourсeStandart.{i}",
                                        Layer + $".ResourсeStandart.{i}.Progress");

                                    var progress = done < standart.Amount ? done / standart.Amount : 1f;
                                    if (progress > 0)
                                        container.Add(
                                            new CuiPanel
                                            {
                                                RectTransform = {AnchorMin = "0 0", AnchorMax = $"{progress} 0.95"},
                                                Image = {Color = Color2}
                                            }, Layer + $".ResourсeStandart.{i}.Progress");

                                    #endregion

                                    #region Edit

                                    if (clan.IsOwner(player.userID))
                                        container.Add(
                                            new CuiButton
                                            {
                                                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                                                Text = {Text = ""},
                                                Button = {Color = "0 0 0 0", Command = $"UI_Clans edititem {i}"}
                                            }, Layer + $".ResourсeStandart.{i}");

                                    #endregion
                                }
                                else
                                {
                                    container.Add(
                                        new CuiLabel
                                        {
                                            RectTransform =
                                            {
                                                AnchorMin = "0.5 1",
                                                AnchorMax = "0.5 1",
                                                OffsetMin = "-30 -70",
                                                OffsetMax = "30 -10"
                                            },
                                            Text =
                                            {
                                                Text = "?",
                                                Align = TextAnchor.MiddleCenter,
                                                FontSize = 24,
                                                Font = "robotocondensed-bold.ttf",
                                                Color = "1 1 1 0.5"
                                            }
                                        }, Layer + $".ResourсeStandart.{i}");

                                    container.Add(
                                        new CuiButton
                                        {
                                            RectTransform =
                                            {
                                                AnchorMin = "0 0",
                                                AnchorMax = "1 0",
                                                OffsetMin = "0 0",
                                                OffsetMax = "0 25"
                                            },
                                            Text =
                                            {
                                                Text = Msg(player, EditTitle),
                                                Align = TextAnchor.MiddleCenter,
                                                Font = "robotocondensed-regular.ttf",
                                                FontSize = 10,
                                                Color = "1 1 1 1"
                                            },
                                            Button = {Color = Color2, Command = $"UI_Clans edititem {i}"}
                                        }, Layer + $".ResourсeStandart.{i}");
                                }

                                if ((i + 1) % AmountOnString == 0)
                                {
                                    xSwitch = 0;
                                    ySwitch = ySwitch - Height - Margin;
                                }
                                else
                                {
                                    xSwitch += Width + Margin;
                                }
                            }
                        }
                        else
                        {
                            var z = 1;
                            foreach (var standart in clan.ResourceStandarts)
                            {
                                container.Add(
                                    new CuiPanel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 1",
                                            AnchorMax = "0 1",
                                            OffsetMin = $"{xSwitch} {ySwitch - Height}",
                                            OffsetMax = $"{xSwitch + Width} {ySwitch}"
                                        },
                                        Image = {Color = Color3}
                                    }, Layer + ".Content", Layer + $".ResourсeStandart.{z}");

                                container.Add(new CuiElement
                                {
                                    Parent = Layer + $".ResourсeStandart.{z}",
                                    Components =
                                    {
                                        new CuiRawImageComponent
                                        {
                                            Png = ImageLibrary.Call<string>("GetImage",
                                                standart.Value.ShortName)
                                        },
                                        new CuiRectTransformComponent
                                        {
                                            AnchorMin = "0.5 1",
                                            AnchorMax = "0.5 1",
                                            OffsetMin = "-30 -70",
                                            OffsetMax = "30 -10"
                                        }
                                    }
                                });

                                #region Progress Text

                                var done = data.GetValue(standart.Value.ShortName);

                                if (done < standart.Value.Amount)
                                {
                                    container.Add(
                                        new CuiLabel
                                        {
                                            RectTransform =
                                            {
                                                AnchorMin = "0.5 1",
                                                AnchorMax = "0.5 1",
                                                OffsetMin = "-55 -85",
                                                OffsetMax = "55 -75"
                                            },
                                            Text =
                                            {
                                                Text = Msg(player, LeftTitle),
                                                Align = TextAnchor.MiddleLeft,
                                                Font = "robotocondensed-regular.ttf",
                                                FontSize = 10,
                                                Color = "1 1 1 0.35"
                                            }
                                        }, Layer + $".ResourсeStandart.{z}");

                                    container.Add(
                                        new CuiLabel
                                        {
                                            RectTransform =
                                            {
                                                AnchorMin = "0.5 1",
                                                AnchorMax = "0.5 1",
                                                OffsetMin = "-55 -100",
                                                OffsetMax = "55 -85"
                                            },
                                            Text =
                                            {
                                                Text = $"{done} / {standart.Value.Amount}",
                                                Align = TextAnchor.MiddleCenter,
                                                Font = "robotocondensed-bold.ttf",
                                                FontSize = 12,
                                                Color = "1 1 1 1"
                                            }
                                        }, Layer + $".ResourсeStandart.{z}");
                                }

                                #endregion

                                #region Progress Bar

                                container.Add(
                                    new CuiPanel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "1 0",
                                            OffsetMin = "0 0",
                                            OffsetMax = "0 10"
                                        },
                                        Image = {Color = Color4}
                                    }, Layer + $".ResourсeStandart.{z}", Layer + $".ResourсeStandart.{z}.Progress");

                                var progress = done < standart.Value.Amount ? done / standart.Value.Amount : 1f;
                                if (progress > 0)
                                    container.Add(
                                        new CuiPanel
                                        {
                                            RectTransform = {AnchorMin = "0 0", AnchorMax = $"{progress} 0.95"},
                                            Image = {Color = Color2}
                                        }, Layer + $".ResourсeStandart.{z}.Progress");

                                #endregion

                                if (z % AmountOnString == 0)
                                {
                                    xSwitch = 0;
                                    ySwitch = ySwitch - Height - Margin;
                                }
                                else
                                {
                                    xSwitch += Width + Margin;
                                }

                                z++;
                            }
                        }

                        break;
                    }

                    case 5:
                    {
                        AmountOnString = 2;
                        Strings = 8;
                        totalAmount = AmountOnString * Strings;
                        ySwitch = 0f;
                        Height = 35f;
                        Width = 237.5f;
                        Margin = 5f;

                        var z = 1;
                        var availablePlayers = covalence.Players.All.Where(member =>
                            {
                                if (FindClanByID(member.Id) != null) return false;

                                var memberData = GetPlayerData(ulong.Parse(member.Id));
                                if (memberData != null && memberData.Invites.ContainsKey(clan.ClanTag)) return false;

                                return string.IsNullOrEmpty(search) || search.Length <= 2 ||
                                       member.Name.StartsWith(search) || member.Name.Contains(search) ||
                                       member.Name.EndsWith(search);
                            })
                            .ToArray();

                        foreach (var member in availablePlayers.Skip(zPage * totalAmount).Take(totalAmount))
                        {
                            xSwitch = z % AmountOnString == 0 ? Margin * 2 + Width : Margin;

                            container.Add(
                                new CuiPanel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"{xSwitch} {ySwitch - Height}",
                                        OffsetMax = $"{xSwitch + Width} {ySwitch}"
                                    },
                                    Image = {Color = Color3}
                                }, Layer + ".Content", Layer + $".Player.{member.Id}");

                            container.Add(new CuiElement
                            {
                                Parent = Layer + $".Player.{member.Id}",
                                Components =
                                {
                                    new CuiRawImageComponent
                                    {
                                        Png = ImageLibrary.Call<string>("GetImage", $"avatar_{member.Id}")
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 0",
                                        AnchorMax = "0 0",
                                        OffsetMin = "0 0",
                                        OffsetMax = $"{Height} {Height}"
                                    }
                                }
                            });

                            #region Display Name

                            container.Add(
                                new CuiLabel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 0.5",
                                        AnchorMax = "0 1",
                                        OffsetMin = "40 1",
                                        OffsetMax = "110 0"
                                    },
                                    Text =
                                    {
                                        Text = Msg(player, NameTitle),
                                        Align = TextAnchor.LowerLeft,
                                        Font = "robotocondensed-regular.ttf",
                                        FontSize = 10,
                                        Color = "1 1 1 1"
                                    }
                                }, Layer + $".Player.{member.Id}");

                            container.Add(
                                new CuiLabel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 0",
                                        AnchorMax = "0 0.5",
                                        OffsetMin = "40 0",
                                        OffsetMax = "95 -1"
                                    },
                                    Text =
                                    {
                                        Text = $"{member.Name}",
                                        Align = TextAnchor.UpperLeft,
                                        Font = "robotocondensed-bold.ttf",
                                        FontSize = 10,
                                        Color = "1 1 1 1"
                                    }
                                }, Layer + $".Player.{member.Id}");

                            #endregion

                            #region SteamId

                            container.Add(
                                new CuiLabel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 0.5",
                                        AnchorMax = "0 1",
                                        OffsetMin = "95 1",
                                        OffsetMax = "210 0"
                                    },
                                    Text =
                                    {
                                        Text = Msg(player, SteamIdTitle),
                                        Align = TextAnchor.LowerLeft,
                                        Font = "robotocondensed-regular.ttf",
                                        FontSize = 10,
                                        Color = "1 1 1 1"
                                    }
                                }, Layer + $".Player.{member.Id}");

                            container.Add(
                                new CuiLabel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 0",
                                        AnchorMax = "0 0.5",
                                        OffsetMin = "95 0",
                                        OffsetMax = "210 -1"
                                    },
                                    Text =
                                    {
                                        Text = $"{member.Id}",
                                        Align = TextAnchor.UpperLeft,
                                        Font = "robotocondensed-bold.ttf",
                                        FontSize = 10,
                                        Color = "1 1 1 1"
                                    }
                                }, Layer + $".Player.{member.Id}");

                            #endregion

                            #region Button

                            container.Add(
                                new CuiButton
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "1 0.5",
                                        AnchorMax = "1 0.5",
                                        OffsetMin = "-45 -8",
                                        OffsetMax = "-5 8"
                                    },
                                    Text =
                                    {
                                        Text = Msg(player, InviteTitle),
                                        Align = TextAnchor.MiddleCenter,
                                        Font = "robotocondensed-regular.ttf",
                                        FontSize = 10,
                                        Color = "1 1 1 1"
                                    },
                                    Button = {Color = Color2, Command = $"UI_Clans invite send {member.Id}"}
                                }, Layer + $".Player.{member.Id}");

                            #endregion

                            if (z % AmountOnString == 0) ySwitch = ySwitch - Height - Margin;

                            z++;
                        }

                        #region Search

                        container.Add(
                            new CuiPanel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0.5 0",
                                    AnchorMax = "0.5 0",
                                    OffsetMin = "-140 20",
                                    OffsetMax = "60 55"
                                },
                                Image = {Color = Color4}
                            }, Layer + ".Content", Layer + ".Search");

                        container.Add(
                            new CuiLabel
                            {
                                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                                Text =
                                {
                                    Text =
                                        string.IsNullOrEmpty(search) ? Msg(player, SearchTitle) : $"{search}",
                                    Align = TextAnchor.MiddleCenter,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 12,
                                    Color = "1 1 1 0.65"
                                }
                            }, Layer + ".Search");

                        container.Add(new CuiElement
                        {
                            Parent = Layer + ".Search",
                            Components =
                            {
                                new CuiInputFieldComponent
                                {
                                    FontSize = 12,
                                    Align = TextAnchor.MiddleCenter,
                                    Font = "robotocondensed-regular.ttf",
                                    Command = $"UI_Clans page {page} 0 ",
                                    Color = "1 1 1 0.95",
                                    CharsLimit = 32
                                },
                                new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                            }
                        });

                        #endregion

                        #region Pages

                        container.Add(
                            new CuiButton
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0.5 0",
                                    AnchorMax = "0.5 0",
                                    OffsetMin = "65 20",
                                    OffsetMax = "100 55"
                                },
                                Text =
                                {
                                    Text = Msg(player, BackPage),
                                    Align = TextAnchor.MiddleCenter,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 12,
                                    Color = "1 1 1 1"
                                },
                                Button =
                                {
                                    Color = Color4,
                                    Command = zPage != 0 ? $"UI_Clans page {page} {zPage - 1} {search}" : ""
                                }
                            }, Layer + ".Content");

                        container.Add(
                            new CuiButton
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0.5 0",
                                    AnchorMax = "0.5 0",
                                    OffsetMin = "105 20",
                                    OffsetMax = "140 55"
                                },
                                Text =
                                {
                                    Text = Msg(player, NextPage),
                                    Align = TextAnchor.MiddleCenter,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 12,
                                    Color = "1 1 1 1"
                                },
                                Button =
                                {
                                    Color = Color2,
                                    Command = availablePlayers.Length > (zPage + 1) * totalAmount
                                        ? $"UI_Clans page {page} {zPage + 1} {search}"
                                        : ""
                                }
                            }, Layer + ".Content");

                        #endregion

                        break;
                    }

                    case 6:
                    {
                        AmountOnString = 4;
                        Strings = 3;
                        totalAmount = AmountOnString * Strings;

                        Height = 110;
                        Width = 110;
                        Margin = 5;

                        xSwitch = 0;
                        ySwitch = 0;

                        var isOwner = clan.IsOwner(player.userID);

                        var i = 0;
                        foreach (var item in _config.Skins.ItemSkins.Keys.Skip(totalAmount * zPage).Take(totalAmount))
                        {
                            container.Add(
                                new CuiPanel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"{xSwitch} {ySwitch - Height}",
                                        OffsetMax = $"{xSwitch + Width} {ySwitch}"
                                    },
                                    Image = {Color = Color3}
                                }, Layer + ".Content", Layer + $".SkinItem.{i}");

                            container.Add(new CuiElement
                            {
                                Parent = Layer + $".SkinItem.{i}",
                                Components =
                                {
                                    new CuiRawImageComponent
                                    {
                                        Png = GetItemImage(item,
                                            clan.Skins.ContainsKey(item) ? clan.Skins[item] : 0)
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = isOwner ? "0.5 1" : "0.5 0.5",
                                        AnchorMax = isOwner ? "0.5 1" : "0.5 0.5",
                                        OffsetMin = isOwner ? "-30 -70" : "-30 -30",
                                        OffsetMax = isOwner ? "30 -10" : "30 30"
                                    }
                                }
                            });

                            #region Edit

                            if (isOwner)
                                container.Add(
                                    new CuiButton
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "1 0",
                                            OffsetMin = "0 0",
                                            OffsetMax = "0 25"
                                        },
                                        Text =
                                        {
                                            Text = Msg(player, EditTitle),
                                            Align = TextAnchor.MiddleCenter,
                                            Font = "robotocondensed-regular.ttf",
                                            FontSize = 10,
                                            Color = "1 1 1 1"
                                        },
                                        Button = {Color = Color2, Command = $"UI_Clans editskin {item}"}
                                    }, Layer + $".SkinItem.{i}");

                            #endregion

                            if ((i + 1) % AmountOnString == 0)
                            {
                                xSwitch = 0;
                                ySwitch = ySwitch - Height - Margin - Margin;
                            }
                            else
                            {
                                xSwitch += Width + Margin;
                            }

                            i++;
                        }

                        #region Pages

                        PagesUi(ref container, player,
                            (int) Math.Ceiling((double) _config.Skins.ItemSkins.Keys.Count / totalAmount), page, zPage);

                        #endregion

                        break;
                    }

                    case 7:
                    {
                        if (clan.Alliances.Count == 0)
                        {
                            container.Add(
                                new CuiLabel
                                {
                                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                                    Text =
                                    {
                                        Text = Msg(player, NoAllies),
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 34,
                                        Font = "robotocondensed-bold.ttf",
                                        Color = Color5
                                    }
                                }, Layer + ".Content");
                        }
                        else
                        {
                            AmountOnString = 2;
                            Strings = 8;
                            totalAmount = AmountOnString * Strings;
                            ySwitch = 0f;
                            Height = 35f;
                            Width = 237.5f;
                            Margin = 5f;

                            var z = 1;

                            foreach (var alliance in clan.Alliances.Skip(zPage * totalAmount).Take(totalAmount))
                            {
                                xSwitch = z % AmountOnString == 0 ? Margin + Width : 0;

                                var allianceClan = FindClanByTag(alliance);
                                if (allianceClan == null) continue;

                                container.Add(
                                    new CuiPanel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 1",
                                            AnchorMax = "0 1",
                                            OffsetMin = $"{xSwitch} {ySwitch - Height}",
                                            OffsetMax = $"{xSwitch + Width} {ySwitch}"
                                        },
                                        Image = {Color = Color3}
                                    }, Layer + ".Content", Layer + $".Player.{alliance}");

                                container.Add(new CuiElement
                                {
                                    Parent = Layer + $".Player.{alliance}",
                                    Components =
                                    {
                                        new CuiRawImageComponent
                                        {
                                            Png = ImageLibrary.Call<string>("GetImage",
                                                $"{allianceClan.Avatar}")
                                        },
                                        new CuiRectTransformComponent
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "0 0",
                                            OffsetMin = "0 0",
                                            OffsetMax = $"{Height} {Height}"
                                        }
                                    }
                                });

                                #region Display Name

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0.5",
                                            AnchorMax = "0 1",
                                            OffsetMin = "40 1",
                                            OffsetMax = "110 0"
                                        },
                                        Text =
                                        {
                                            Text = Msg(player, NameTitle),
                                            Align = TextAnchor.LowerLeft,
                                            Font = "robotocondensed-regular.ttf",
                                            FontSize = 10,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Player.{alliance}");

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "0 0.5",
                                            OffsetMin = "40 0",
                                            OffsetMax = "95 -1"
                                        },
                                        Text =
                                        {
                                            Text = $"{allianceClan?.ClanTag}",
                                            Align = TextAnchor.UpperLeft,
                                            Font = "robotocondensed-bold.ttf",
                                            FontSize = 10,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Player.{alliance}");

                                #endregion

                                #region SteamId

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0.5",
                                            AnchorMax = "0 1",
                                            OffsetMin = "95 1",
                                            OffsetMax = "210 0"
                                        },
                                        Text =
                                        {
                                            Text = Msg(player, MembersTitle),
                                            Align = TextAnchor.LowerLeft,
                                            Font = "robotocondensed-regular.ttf",
                                            FontSize = 10,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Player.{alliance}");

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "0 0.5",
                                            OffsetMin = "95 0",
                                            OffsetMax = "210 -1"
                                        },
                                        Text =
                                        {
                                            Text = $"{allianceClan.Members.Count}",
                                            Align = TextAnchor.UpperLeft,
                                            Font = "robotocondensed-bold.ttf",
                                            FontSize = 10,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Player.{alliance}");

                                #endregion

                                #region Button

                                container.Add(
                                    new CuiButton
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "1 0.5",
                                            AnchorMax = "1 0.5",
                                            OffsetMin = "-45 -8",
                                            OffsetMax = "-5 8"
                                        },
                                        Text =
                                        {
                                            Text = Msg(player, ProfileTitle),
                                            Align = TextAnchor.MiddleCenter,
                                            Font = "robotocondensed-regular.ttf",
                                            FontSize = 10,
                                            Color = "1 1 1 1"
                                        },
                                        Button = {Color = Color2, Command = $"UI_Clans showclan {alliance}"}
                                    }, Layer + $".Player.{alliance}");

                                #endregion

                                if (z % AmountOnString == 0) ySwitch = ySwitch - Height - Margin;

                                z++;
                            }

                            #region Pages

                            container.Add(
                                new CuiButton
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0.5 0",
                                        AnchorMax = "0.5 0",
                                        OffsetMin = "-37.5 20",
                                        OffsetMax = "-2.5 55"
                                    },
                                    Text =
                                    {
                                        Text = Msg(player, BackPage),
                                        Align = TextAnchor.MiddleCenter,
                                        Font = "robotocondensed-regular.ttf",
                                        FontSize = 12,
                                        Color = "1 1 1 1"
                                    },
                                    Button =
                                    {
                                        Color = Color4,
                                        Command = zPage != 0 ? $"UI_Clans page {page} {zPage - 1} {search}" : ""
                                    }
                                }, Layer + ".Content");

                            container.Add(
                                new CuiButton
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0.5 0",
                                        AnchorMax = "0.5 0",
                                        OffsetMin = "2.5 20",
                                        OffsetMax = "37.5 55"
                                    },
                                    Text =
                                    {
                                        Text = Msg(player, NextPage),
                                        Align = TextAnchor.MiddleCenter,
                                        Font = "robotocondensed-regular.ttf",
                                        FontSize = 12,
                                        Color = "1 1 1 1"
                                    },
                                    Button =
                                    {
                                        Color = Color2,
                                        Command = clan.Alliances.Count > (zPage + 1) * totalAmount
                                            ? $"UI_Clans page {page} {zPage + 1} {search}"
                                            : ""
                                    }
                                }, Layer + ".Content");

                            #endregion
                        }

                        break;
                    }

                    case 45:
                    {
                        if (data.Invites.Count == 0)
                        {
                            container.Add(
                                new CuiLabel
                                {
                                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                                    Text =
                                    {
                                        Text = Msg(player, NoInvites),
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 34,
                                        Font = "robotocondensed-bold.ttf",
                                        Color = Color5
                                    }
                                }, Layer + ".Content");
                        }
                        else
                        {
                            ySwitch = 0f;
                            Height = 48.5f;
                            Margin = 5f;
                            totalAmount = 7;

                            foreach (var invite in data.Invites.Skip(zPage * totalAmount).Take(totalAmount))
                            {
                                container.Add(
                                    new CuiPanel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 1",
                                            AnchorMax = "0 1",
                                            OffsetMin = $"0 {ySwitch - Height}",
                                            OffsetMax = $"450 {ySwitch}"
                                        },
                                        Image = {Color = Color3}
                                    }, Layer + ".Content", Layer + $".Invite.{invite.Key}");

                                var targetClan = FindClanByTag(invite.Key);
                                if (targetClan != null && !string.IsNullOrEmpty(targetClan.Avatar))
                                    container.Add(new CuiElement
                                    {
                                        Parent = Layer + $".Invite.{invite.Key}",
                                        Components =
                                        {
                                            new CuiRawImageComponent
                                            {
                                                Png = ImageLibrary.Call<string>("GetImage",
                                                    targetClan.Avatar)
                                            },
                                            new CuiRectTransformComponent
                                            {
                                                AnchorMin = "0 0",
                                                AnchorMax = "0 0",
                                                OffsetMin = "0 0",
                                                OffsetMax = $"{Height} {Height}"
                                            }
                                        }
                                    });

                                #region Clan Name

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0.5",
                                            AnchorMax = "0 1",
                                            OffsetMin = "55 1",
                                            OffsetMax = "135 0"
                                        },
                                        Text =
                                        {
                                            Text = Msg(player, ClanInvitation),
                                            Align = TextAnchor.LowerLeft,
                                            Font = "robotocondensed-regular.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Invite.{invite.Key}");

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "0 0.5",
                                            OffsetMin = "55 0",
                                            OffsetMax = "135 -1"
                                        },
                                        Text =
                                        {
                                            Text = $"{invite.Key}",
                                            Align = TextAnchor.UpperLeft,
                                            Font = "robotocondensed-bold.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Invite.{invite.Key}");

                                #endregion

                                #region Inviter

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0.5",
                                            AnchorMax = "0 1",
                                            OffsetMin = "160 1",
                                            OffsetMax = "315 0"
                                        },
                                        Text =
                                        {
                                            Text = Msg(player, InviterTitle),
                                            Align = TextAnchor.LowerLeft,
                                            Font = "robotocondensed-regular.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Invite.{invite.Key}");

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "0 0.5",
                                            OffsetMin = "160 0",
                                            OffsetMax = "315 -1"
                                        },
                                        Text =
                                        {
                                            Text = $"{invite.Value.InviterName}",
                                            Align = TextAnchor.UpperLeft,
                                            Font = "robotocondensed-bold.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Invite.{invite.Key}");

                                #endregion

                                #region Buttons

                                container.Add(
                                    new CuiButton
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "1 0.5",
                                            AnchorMax = "1 0.5",
                                            OffsetMin = "-95 -12.5",
                                            OffsetMax = "-15 12.5"
                                        },
                                        Text =
                                        {
                                            Text = Msg(player, AcceptTitle),
                                            Align = TextAnchor.MiddleCenter,
                                            Font = "robotocondensed-regular.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        },
                                        Button =
                                        {
                                            Color = Color2,
                                            Command = $"UI_Clans invite accept {invite.Key}",
                                            Close = Layer
                                        }
                                    }, Layer + $".Invite.{invite.Key}");

                                container.Add(
                                    new CuiButton
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "1 0.5",
                                            AnchorMax = "1 0.5",
                                            OffsetMin = "-185 -12.5",
                                            OffsetMax = "-105 12.5"
                                        },
                                        Text =
                                        {
                                            Text = Msg(player, CancelTitle),
                                            Align = TextAnchor.MiddleCenter,
                                            Font = "robotocondensed-regular.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        },
                                        Button =
                                        {
                                            Color = Color6,
                                            Command = $"UI_Clans invite cancel {invite.Key}",
                                            Close = Layer
                                        }
                                    }, Layer + $".Invite.{invite.Key}");

                                #endregion

                                ySwitch = ySwitch - Height - Margin;
                            }

                            #region Pages

                            PagesUi(ref container, player,
                                (int) Math.Ceiling((double) data.Invites.Count / totalAmount), page, zPage);

                            #endregion
                        }

                        break;
                    }

                    case 65:
                    {
                        if (clan.InvitedPlayers.Count == 0)
                        {
                            container.Add(
                                new CuiLabel
                                {
                                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                                    Text =
                                    {
                                        Text = Msg(player, NoInvites),
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 34,
                                        Font = "robotocondensed-bold.ttf",
                                        Color = Color5
                                    }
                                }, Layer + ".Content");
                        }
                        else
                        {
                            ySwitch = 0f;
                            Height = 48.5f;
                            Margin = 5f;
                            totalAmount = 7;

                            foreach (var invite in clan.InvitedPlayers.Skip(zPage * totalAmount).Take(totalAmount))
                            {
                                container.Add(
                                    new CuiPanel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 1",
                                            AnchorMax = "0 1",
                                            OffsetMin = $"0 {ySwitch - Height}",
                                            OffsetMax = $"450 {ySwitch}"
                                        },
                                        Image = {Color = Color3}
                                    }, Layer + ".Content", Layer + $".Invite.{invite.Key}");

                                container.Add(new CuiElement
                                {
                                    Parent = Layer + $".Invite.{invite.Key}",
                                    Components =
                                    {
                                        new CuiRawImageComponent
                                        {
                                            Png = ImageLibrary.Call<string>("GetImage",
                                                $"avatar_{invite.Key}")
                                        },
                                        new CuiRectTransformComponent
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "0 0",
                                            OffsetMin = "0 0",
                                            OffsetMax = $"{Height} {Height}"
                                        }
                                    }
                                });

                                #region Player Name

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0.5",
                                            AnchorMax = "0 1",
                                            OffsetMin = "75 1",
                                            OffsetMax = "195 0"
                                        },
                                        Text =
                                        {
                                            Text = Msg(player, PlayerTitle),
                                            Align = TextAnchor.LowerLeft,
                                            Font = "robotocondensed-regular.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Invite.{invite.Key}");

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "0 0.5",
                                            OffsetMin = "75 0",
                                            OffsetMax = "195 -1"
                                        },
                                        Text =
                                        {
                                            Text = $"{GetPlayerData(invite.Key)?.DisplayName}",
                                            Align = TextAnchor.UpperLeft,
                                            Font = "robotocondensed-bold.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Invite.{invite.Key}");

                                #endregion

                                #region Inviter

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0.5",
                                            AnchorMax = "0 1",
                                            OffsetMin = "195 1",
                                            OffsetMax = "315 0"
                                        },
                                        Text =
                                        {
                                            Text = Msg(player, InviterTitle),
                                            Align = TextAnchor.LowerLeft,
                                            Font = "robotocondensed-regular.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Invite.{invite.Key}");

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "0 0.5",
                                            OffsetMin = "195 0",
                                            OffsetMax = "315 -1"
                                        },
                                        Text =
                                        {
                                            Text = $"{GetPlayerData(invite.Value)?.DisplayName}",
                                            Align = TextAnchor.UpperLeft,
                                            Font = "robotocondensed-bold.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Invite.{invite.Key}");

                                #endregion

                                #region Buttons

                                container.Add(
                                    new CuiButton
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "1 0.5",
                                            AnchorMax = "1 0.5",
                                            OffsetMin = "-185 -12.5",
                                            OffsetMax = "-15 12.5"
                                        },
                                        Text =
                                        {
                                            Text = Msg(player, CancelTitle),
                                            Align = TextAnchor.MiddleCenter,
                                            Font = "robotocondensed-regular.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        },
                                        Button =
                                        {
                                            Color = Color6, Command = $"UI_Clans invite withdraw {invite.Key}"
                                        }
                                    }, Layer + $".Invite.{invite.Key}");

                                #endregion

                                ySwitch = ySwitch - Height - Margin;
                            }

                            #region Pages

                            PagesUi(ref container, player,
                                (int) Math.Ceiling((double) clan.InvitedPlayers.Count / totalAmount), page, zPage);

                            #endregion
                        }

                        break;
                    }

                    case 71: //ally invites
                    {
                        if (clan.AllianceInvites.Count == 0)
                        {
                            container.Add(
                                new CuiLabel
                                {
                                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                                    Text =
                                    {
                                        Text = Msg(player, NoInvites),
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 34,
                                        Font = "robotocondensed-bold.ttf",
                                        Color = Color5
                                    }
                                }, Layer + ".Content");
                        }
                        else
                        {
                            ySwitch = 0f;
                            Height = 48.5f;
                            Margin = 5f;
                            totalAmount = 7;

                            foreach (var invite in clan.AllianceInvites.Skip(zPage * totalAmount).Take(totalAmount))
                            {
                                var targetClan = FindClanByTag(invite.Key);
                                if (targetClan == null) continue;

                                container.Add(
                                    new CuiPanel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 1",
                                            AnchorMax = "0 1",
                                            OffsetMin = $"0 {ySwitch - Height}",
                                            OffsetMax = $"450 {ySwitch}"
                                        },
                                        Image = {Color = Color3}
                                    }, Layer + ".Content", Layer + $".Invite.{invite.Key}");

                                container.Add(new CuiElement
                                {
                                    Parent = Layer + $".Invite.{invite.Key}",
                                    Components =
                                    {
                                        new CuiRawImageComponent
                                        {
                                            Png = ImageLibrary.Call<string>("GetImage",
                                                $"{targetClan.Avatar}")
                                        },
                                        new CuiRectTransformComponent
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "0 0",
                                            OffsetMin = "0 0",
                                            OffsetMax = $"{Height} {Height}"
                                        }
                                    }
                                });

                                #region Title

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0.5",
                                            AnchorMax = "0 1",
                                            OffsetMin = "75 1",
                                            OffsetMax = "195 0"
                                        },
                                        Text =
                                        {
                                            Text = Msg(player, ClanTitle),
                                            Align = TextAnchor.LowerLeft,
                                            Font = "robotocondensed-regular.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Invite.{invite.Key}");

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "0 0.5",
                                            OffsetMin = "75 0",
                                            OffsetMax = "195 -1"
                                        },
                                        Text =
                                        {
                                            Text = $"{targetClan.ClanTag}",
                                            Align = TextAnchor.UpperLeft,
                                            Font = "robotocondensed-bold.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Invite.{invite.Key}");

                                #endregion

                                #region Inviter

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0.5",
                                            AnchorMax = "0 1",
                                            OffsetMin = "195 1",
                                            OffsetMax = "315 0"
                                        },
                                        Text =
                                        {
                                            Text = Msg(player, InviterTitle),
                                            Align = TextAnchor.LowerLeft,
                                            Font = "robotocondensed-regular.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Invite.{invite.Key}");

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "0 0.5",
                                            OffsetMin = "195 0",
                                            OffsetMax = "315 -1"
                                        },
                                        Text =
                                        {
                                            Text = $"{GetPlayerData(invite.Value)?.DisplayName}",
                                            Align = TextAnchor.UpperLeft,
                                            Font = "robotocondensed-bold.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Invite.{invite.Key}");

                                #endregion

                                #region Buttons

                                container.Add(
                                    new CuiButton
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "1 0.5",
                                            AnchorMax = "1 0.5",
                                            OffsetMin = "-185 -12.5",
                                            OffsetMax = "-15 12.5"
                                        },
                                        Text =
                                        {
                                            Text = Msg(player, CancelTitle),
                                            Align = TextAnchor.MiddleCenter,
                                            Font = "robotocondensed-regular.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        },
                                        Button =
                                        {
                                            Color = Color6,
                                            Command = $"UI_Clans allyinvite withdraw {invite.Key}"
                                        }
                                    }, Layer + $".Invite.{invite.Key}");

                                #endregion

                                ySwitch = ySwitch - Height - Margin;
                            }

                            #region Pages

                            PagesUi(ref container, player,
                                (int) Math.Ceiling((double) clan.AllianceInvites.Count / totalAmount), page, zPage);

                            #endregion
                        }

                        break;
                    }

                    case 72: //incoming ally
                    {
                        if (clan.IncomingAlliances.Count == 0)
                        {
                            container.Add(
                                new CuiLabel
                                {
                                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                                    Text =
                                    {
                                        Text = Msg(player, NoInvites),
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 34,
                                        Font = "robotocondensed-bold.ttf",
                                        Color = Color5
                                    }
                                }, Layer + ".Content");
                        }
                        else
                        {
                            ySwitch = 0f;
                            Height = 48.5f;
                            Margin = 5f;
                            totalAmount = 7;

                            foreach (var invite in clan.IncomingAlliances.Skip(zPage * totalAmount).Take(totalAmount))
                            {
                                var targetClan = FindClanByTag(invite);
                                if (targetClan == null) continue;

                                container.Add(
                                    new CuiPanel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 1",
                                            AnchorMax = "0 1",
                                            OffsetMin = $"0 {ySwitch - Height}",
                                            OffsetMax = $"450 {ySwitch}"
                                        },
                                        Image = {Color = Color3}
                                    }, Layer + ".Content", Layer + $".Invite.{invite}");

                                container.Add(new CuiElement
                                {
                                    Parent = Layer + $".Invite.{invite}",
                                    Components =
                                    {
                                        new CuiRawImageComponent
                                        {
                                            Png = ImageLibrary.Call<string>("GetImage",
                                                $"{targetClan.Avatar}")
                                        },
                                        new CuiRectTransformComponent
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "0 0",
                                            OffsetMin = "0 0",
                                            OffsetMax = $"{Height} {Height}"
                                        }
                                    }
                                });

                                #region Title

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0.5",
                                            AnchorMax = "0 1",
                                            OffsetMin = "75 1",
                                            OffsetMax = "195 0"
                                        },
                                        Text =
                                        {
                                            Text = Msg(player, ClanTitle),
                                            Align = TextAnchor.LowerLeft,
                                            Font = "robotocondensed-regular.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Invite.{invite}");

                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "0 0.5",
                                            OffsetMin = "75 0",
                                            OffsetMax = "195 -1"
                                        },
                                        Text =
                                        {
                                            Text = $"{targetClan.ClanTag}",
                                            Align = TextAnchor.UpperLeft,
                                            Font = "robotocondensed-bold.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".Invite.{invite}");

                                #endregion

                                #region Buttons

                                container.Add(
                                    new CuiButton
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "1 0.5",
                                            AnchorMax = "1 0.5",
                                            OffsetMin = "-95 -12.5",
                                            OffsetMax = "-15 12.5"
                                        },
                                        Text =
                                        {
                                            Text = Msg(player, AcceptTitle),
                                            Align = TextAnchor.MiddleCenter,
                                            Font = "robotocondensed-regular.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        },
                                        Button =
                                        {
                                            Color = Color2,
                                            Command = $"UI_Clans allyinvite accept {invite}",
                                            Close = Layer
                                        }
                                    }, Layer + $".Invite.{invite}");

                                container.Add(
                                    new CuiButton
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "1 0.5",
                                            AnchorMax = "1 0.5",
                                            OffsetMin = "-185 -12.5",
                                            OffsetMax = "-105 12.5"
                                        },
                                        Text =
                                        {
                                            Text = Msg(player, CancelTitle),
                                            Align = TextAnchor.MiddleCenter,
                                            Font = "robotocondensed-regular.ttf",
                                            FontSize = 12,
                                            Color = "1 1 1 1"
                                        },
                                        Button =
                                        {
                                            Color = Color6,
                                            Command = $"UI_Clans allyinvite cancel {invite}",
                                            Close = Layer
                                        }
                                    }, Layer + $".Invite.{invite}");

                                #endregion

                                ySwitch = ySwitch - Height - Margin;
                            }

                            #region Pages

                            PagesUi(ref container, player,
                                (int) Math.Ceiling((double) clan.IncomingAlliances.Count / totalAmount), page, zPage);

                            #endregion
                        }

                        break;
                    }
                }
            else
                container.Add(
                    new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                        Text =
                        {
                            Text = Msg(player, NotMemberOfClan),
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 34,
                            Font = "robotocondensed-bold.ttf",
                            Color = Color5
                        }
                    }, Layer + ".Content");

            if (page == 2)
            {
                CuiHelper.DestroyUi(player, Layer + ".Content");
                #region Title

                        container.Add(
                            new CuiLabel
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = "2.5 -30",
                                    OffsetMax = "225 0"
                                },
                                Text =
                                {
                                    Text = Msg(player, TopClansTitle),
                                    Align = TextAnchor.MiddleLeft,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 12,
                                    Color = "1 1 1 1"
                                }
                            }, Layer + ".Content");

                        #endregion

                        #region Head

                        ySwitch = 0;

                        _config.UI.TopClansColumns.ForEach(column =>
                        {
                            container.Add(
                                new CuiLabel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"{ySwitch} -50",
                                        OffsetMax = $"{ySwitch + column.Width} -30"
                                    },
                                    Text =
                                    {
                                        Text = Msg(player, column.LangKey),
                                        Align = column.TextAlign,
                                        Font = "robotocondensed-regular.ttf",
                                        FontSize = column.TitleFontSize,
                                        Color = "1 1 1 1"
                                    }
                                }, Layer + ".Content");

                            ySwitch += column.Width;
                        });

                        #endregion

                        #region Table

                        ySwitch = -50;
                        Height = 37.5f;
                        Margin = 2.5f;
                        totalAmount = 7;

                        var i = 0;
                        foreach (var topClan in TopClans.Skip(zPage * totalAmount).Take(totalAmount))
                        {
                            var top = zPage * totalAmount + i + 1;

                            container.Add(
                                new CuiPanel
                                {
                                    RectTransform =
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"0 {ySwitch - Height}",
                                        OffsetMax = $"480 {ySwitch}"
                                    },
                                    Image = {Color = Color3}
                                }, Layer + ".Content", Layer + $".TopClan.{i}");

                            var localSwitch = 0f;
                            _config.UI.TopClansColumns.ForEach(column =>
                            {
                                container.Add(
                                    new CuiLabel
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin = "0 0",
                                            AnchorMax = "0 1",
                                            OffsetMin = $"{localSwitch} 0",
                                            OffsetMax = $"{localSwitch + column.Width} 0"
                                        },
                                        Text =
                                        {
                                            Text = $"{column.GetKey(top, topClan.GetParams(column.Key))}",
                                            Align = column.TextAlign,
                                            Font = "robotocondensed-bold.ttf",
                                            FontSize = column.FontSize,
                                            Color = "1 1 1 1"
                                        }
                                    }, Layer + $".TopClan.{i}");

                                localSwitch += column.Width;
                            });

                            container.Add(
                                new CuiButton
                                {
                                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                                    Text = {Text = ""},
                                    Button =
                                    {
                                        Color = "0 0 0 0",
                                        Command = topClan == clan
                                            ? "UI_Clans page 0"
                                            : $"UI_Clans showclan {topClan.ClanTag}"
                                    }
                                }, Layer + $".TopClan.{i}");

                            ySwitch = ySwitch - Height - Margin;

                            i++;
                        }

                        #endregion

                        #region Pages

                        PagesUi(ref container, player, (int) Math.Ceiling((double) TopClans.Count / totalAmount), page,
                            zPage);

                        #endregion
            }

            #endregion

            #endregion

            CuiHelper.DestroyUi(player, Layer + ".Main");
            //CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
            //Profile.Call("DrawSupplyButtonsMain", player, "openclans");
        }

        private void SelectItemUi(BasePlayer player, int slot, int page = 0, int amount = 0, string search = "",
            bool First = false)
        {
            #region Fields

            var clan = FindClanByID(player.userID);

            var itemsList = StandartItems.FindAll(item =>
                string.IsNullOrEmpty(search) || search.Length <= 2 || item.shortname.Contains(search) ||
                item.displayName.english.Contains(search));

            if (amount == 0)
                amount = clan.ResourceStandarts.ContainsKey(slot)
                    ? clan.ResourceStandarts[slot].Amount
                    : _config.DefaultValStandarts;

            var amountOnString = 6;
            var strings = 3;
            var totalAmount = amountOnString * strings;

            var Height = 115f;
            var Width = 110f;
            var Margin = 10f;

            var constSwitchX = -(amountOnString * Width + (amountOnString - 1) * Margin) / 2f;

            var xSwitch = constSwitchX;
            var ySwitch = -75f;

            #endregion

            var container = new CuiElementContainer();

            #region Background

            if (First)
            {
                CuiHelper.DestroyUi(player, Layer);

                container.Add(
                    new CuiPanel
                    {
                        //RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                        Image = {Color = "0 0 0 0", Material = ""},
                        CursorEnabled = false,
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0",OffsetMin = "465 115",OffsetMax = "1145 550"}
                }, "SubContent_UI", Layer);

                /*container.Add(
                    new CuiButton
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                        Text = {Text = ""},
                        Button = {Color = "0 0 0 0", Close = Layer,Command = "chat.say /profile"}
                    }, Layer);*/
            }

            #endregion

            #region Main

            container.Add(
                new CuiPanel {RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"}, Image = {Color = Color1}}, Layer,
                Layer + ".Main");

            #region Header

            container.Add(
                new CuiPanel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 -55", OffsetMax = "0 0"},
                    Image = {Color = Color3}
                }, Layer + ".Main", Layer + ".Header");

            #region Title

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "25 0", OffsetMax = "0 0"},
                    Text =
                    {
                        Text = Msg(player, SelectItemTitle),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 14,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Header");

            #endregion

            #region Search

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0.5",
                        AnchorMax = "0 0.5",
                        OffsetMin = "160 -17.5",
                        OffsetMax = "410 17.5"
                    },
                    Image = {Color = Color4}
                }, Layer + ".Header", Layer + ".Header.Search");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                    Text =
                    {
                        Text = string.IsNullOrEmpty(search) ? Msg(player, SearchTitle) : $"{search}",
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 12,
                        Color = "1 1 1 0.65"
                    }
                }, Layer + ".Header.Search");

            container.Add(new CuiElement
            {
                Parent = Layer + ".Header.Search",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf",
                        Command = $"UI_Clans selectpages {slot} 0 {amount} ",
                        Color = "1 1 1 0.95",
                        CharsLimit = 32
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });

            #endregion

            #region Amount

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-35 -17.5",
                        OffsetMax = "95 17.5"
                    },
                    Image = {Color = Color4}
                }, Layer + ".Header", Layer + ".Header.Amount");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                    Text =
                    {
                        Text = $"{amount}",
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 12,
                        Color = "1 1 1 0.65"
                    }
                }, Layer + ".Header.Amount");

            container.Add(new CuiElement
            {
                Parent = Layer + ".Header.Amount",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf",
                        Command = $"UI_Clans setamountitem {slot} ",
                        Color = "1 1 1 0.95",
                        CharsLimit = 32
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });

            #endregion

            #region Pages

            container.Add(
                new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0.5",
                        AnchorMax = "0 0.5",
                        OffsetMin = "415 -17.5",
                        OffsetMax = "450 17.5"
                    },
                    Text =
                    {
                        Text = Msg(player, BackPage),
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    },
                    Button =
                    {
                        Color = Color4,
                        Command = page != 0 ? $"UI_Clans selectpages {slot} {page - 1} {amount} {search}" : ""
                    }
                }, Layer + ".Header");

            container.Add(
                new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0.5",
                        AnchorMax = "0 0.5",
                        OffsetMin = "455 -17.5",
                        OffsetMax = "490 17.5"
                    },
                    Text =
                    {
                        Text = Msg(player, NextPage),
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    },
                    Button =
                    {
                        Color = Color2,
                        Command = itemsList.Count > (page + 1) * totalAmount
                            ? $"UI_Clans selectpages {slot} {page + 1} {amount} {search}"
                            : ""
                    }
                }, Layer + ".Header");

            #endregion

            #region Close

            container.Add(
                new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "1 0.5",
                        AnchorMax = "1 0.5",
                        OffsetMin = "-35 -12.5",
                        OffsetMax = "-10 12.5"
                    },
                    Text =
                    {
                        Text = Msg(player, CloseTitle),
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 10,
                        Color = "1 1 1 1"
                    },
                    Button = {Color = Color2, Command = "UI_Clans page 4"}
                }, Layer + ".Header");

            #endregion

            #endregion

            #region Items

            var i = 1;
            foreach (var def in itemsList.Skip(page * totalAmount).Take(totalAmount))
            {
                container.Add(
                    new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.5 1",
                            AnchorMax = "0.5 1",
                            OffsetMin = $"{xSwitch} {ySwitch - Height}",
                            OffsetMax = $"{xSwitch + Width} {ySwitch}"
                        },
                        Image =
                        {
                            Color = clan.ResourceStandarts.ContainsKey(slot) &&
                                    clan.ResourceStandarts[slot].ShortName == def.shortname
                                ? Color4
                                : Color3
                        }
                    }, Layer + ".Main", Layer + $".Item.{i}");

                container.Add(new CuiElement
                {
                    Parent = Layer + $".Item.{i}",
                    Components =
                    {
                        new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", def.shortname)},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 1",
                            AnchorMax = "0.5 1",
                            OffsetMin = "-35 -80",
                            OffsetMax = "35 -10"
                        }
                    }
                });

                container.Add(
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = "0 25"
                        },
                        Text =
                        {
                            Text = Msg(player, SelectTitle),
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 10,
                            Color = "1 1 1 1"
                        },
                        Button = {Color = Color2, Command = $"UI_Clans selectitem {slot} {def.shortname} {amount}"}
                    }, Layer + $".Item.{i}");

                if (i % amountOnString == 0)
                {
                    xSwitch = constSwitchX;
                    ySwitch = ySwitch - Height - Margin;
                }
                else
                {
                    xSwitch += Width + Margin;
                }

                i++;
            }

            #endregion

            #endregion

            CuiHelper.DestroyUi(player, Layer + ".Main");
            CuiHelper.AddUi(player, container);
        }

        private void CreateClanUi(BasePlayer player)
        {
            if (!ClanCreating.ContainsKey(player)) ClanCreating.Add(player, new CreateClanData());

            var clanTag = ClanCreating[player].Tag;
            var avatar = ClanCreating[player].Avatar;

            var container = new CuiElementContainer();

            #region Background

            container.Add(
                new CuiPanel
                {
                    //RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                    Image = {Color = "0 0 0 0", Material = ""},
                    CursorEnabled = false,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0",OffsetMin = "465 115",OffsetMax = "1145 550"}
            }, "SubContent_UI", Layer);

            container.Add(
                new CuiButton
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                    Text = {Text = ""},
                    Button = {Color = "0 0 0 0", Close = Layer,Command = "chat.say /profile"}
                }, Layer);

            #endregion

            #region Main

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-340 -215",
                        OffsetMax = "340 220"
                    },
                    Image = {Color = Color1}
                }, Layer, Layer + ".Main");

            #region Header

            HeaderUi(ref container, player, null, 0, Msg(player, ClanCreationTitle));

            #endregion

            #region Name

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0.5 1",
                        AnchorMax = "0.5 1",
                        OffsetMin = "-150 -140",
                        OffsetMax = "150 -110"
                    },
                    Image = {Color = Color3}
                }, Layer + ".Main", Layer + ".Clan.Creation.Name");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"},
                    Text =
                    {
                        Text = Msg(player, ClanNameTitle),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Clan.Creation.Name");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5"},
                    Text =
                    {
                        Text = string.IsNullOrEmpty(clanTag) ? Msg(player, EnterClanName) : $"{clanTag}",
                        FontSize = 10,
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        Color = "1 1 1 0.1"
                    }
                }, Layer + ".Clan.Creation.Name");

            container.Add(new CuiElement
            {
                Parent = Layer + ".Clan.Creation.Name",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        FontSize = 10,
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        Command = "UI_Clans createclan name ",
                        Color = "1 1 1 0.9",
                        CharsLimit = _config.TagMax
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5"
                    }
                }
            });

            #endregion

            #region Avatar

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0.5 1",
                        AnchorMax = "0.5 1",
                        OffsetMin = "-150 -210",
                        OffsetMax = "150 -180"
                    },
                    Image = {Color = Color3}
                }, Layer + ".Main", Layer + ".Clan.Creation.Avatar");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"},
                    Text =
                    {
                        Text = Msg(player, AvatarTitle),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Clan.Creation.Avatar");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5"},
                    Text =
                    {
                        Text = string.IsNullOrEmpty(avatar) ? Msg(player, UrlTitle) : $"{avatar}",
                        FontSize = 10,
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        Color = "1 1 1 0.1"
                    }
                }, Layer + ".Clan.Creation.Avatar");

            container.Add(new CuiElement
            {
                Parent = Layer + ".Clan.Creation.Avatar",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        FontSize = 10,
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        Command = "UI_Clans createclan avatar ",
                        Color = "1 1 1 0.9",
                        CharsLimit = 128
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5"
                    }
                }
            });

            #endregion

            #region Create Clan

            container.Add(
                new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-75 -295", OffsetMax = "75 -270"
                    },
                    Text =
                    {
                        Text = Msg(player, CreateTitle),
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    },
                    Button = {Color = Color2, Command = "UI_Clans createclan create", Close = Layer}
                }, Layer + ".Main");

            #endregion

            #endregion

            //CuiHelper.DestroyUi(player, Layer);
            CuiHelper.DestroyUi(player, Layer + ".Main");
            CuiHelper.AddUi(player, container);
        }

        private void HeaderUi(ref CuiElementContainer container, BasePlayer player, ClanData clan, int page,
            string headTitle, string backPage = "")
        {
            container.Add(
                new CuiPanel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 -45", OffsetMax = "0 0"},
                    Image = {Color = Color3}
                }, Layer + ".Main", Layer + ".Header");

            #region Title

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "12.5 0", OffsetMax = "0 0"},
                    Text =
                    {
                        Text = $"{headTitle}",
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 14,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Header");

            #endregion

            #region Close

            container.Add(
                new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-35 -37.5", OffsetMax = "-10 -12.5"
                    },
                    Text =
                    {
                        Text = Msg(player, CloseTitle),
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 10,
                        Color = "1 1 1 1"
                    },
                    Button = {Close = Layer, Color = Color2,Command = "chat.say /profile"}
                }, Layer + ".Header");

            #endregion

            #region Back

            var hasBack = !string.IsNullOrEmpty(backPage);

            if (hasBack)
                container.Add(
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "1 1",
                            AnchorMax = "1 1",
                            OffsetMin = "-65 -37.5",
                            OffsetMax = "-40 -12.5"
                        },
                        Text =
                        {
                            Text = Msg(player, BackPage),
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 12,
                            Color = "1 1 1 1"
                        },
                        Button = {Color = Color2, Command = $"{backPage}"}
                    }, Layer + ".Header");

            #endregion

            #region Invites

            if (clan != null && clan.IsModerator(player.userID))
            {
                if (page == 65 || page == 71 || page == 72)
                {
                    container.Add(
                        new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = "1 1",
                                AnchorMax = "1 1",
                                OffsetMin = "-470 -37.5",
                                OffsetMax = "-330 -12.5"
                            },
                            Text =
                            {
                                Text = Msg(player, AllyInvites),
                                Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 12,
                                Color = "1 1 1 1"
                            },
                            Button = {Color = page == 71 ? Color2 : Color4, Command = "UI_Clans page 71"}
                        }, Layer + ".Header");

                    container.Add(
                        new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = "1 1",
                                AnchorMax = "1 1",
                                OffsetMin = "-325 -37.5",
                                OffsetMax = "-185 -12.5"
                            },
                            Text =
                            {
                                Text = Msg(player, IncomingAllyTitle),
                                Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 12,
                                Color = "1 1 1 1"
                            },
                            Button = {Color = page == 72 ? Color2 : Color4, Command = "UI_Clans page 72"}
                        }, Layer + ".Header");

                    container.Add(
                        new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = "1 1",
                                AnchorMax = "1 1",
                                OffsetMin = "-180 -37.5",
                                OffsetMax = "-40 -12.5"
                            },
                            Text =
                            {
                                Text = Msg(player, ClanInvitesTitle),
                                Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 12,
                                Color = "1 1 1 1"
                            },
                            Button = {Color = page == 65 ? Color2 : Color4, Command = "UI_Clans page 65"}
                        }, Layer + ".Header");
                }
                else
                {
                    container.Add(
                        new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = "1 1",
                                AnchorMax = "1 1",
                                OffsetMin = $"{(hasBack ? -220 : -180)} -37.5",
                                OffsetMax = $"{(hasBack ? -70 : -40)} -12.5"
                            },
                            Text =
                            {
                                Text = Msg(player, InvitesTitle),
                                Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 12,
                                Color = "1 1 1 1"
                            },
                            Button = {Color = Color4, Command = "UI_Clans page 65"}
                        }, Layer + ".Header");
                }
            }

            #endregion

            #region Notify

            if (HasInvite(player))
            {
                container.Add(
                    new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = "1 1",
                            AnchorMax = "1 1",
                            OffsetMin = "-205 -37.5",
                            OffsetMax = "-40 -12.5"
                        },
                        Image = {Color = Color2}
                    }, Layer + ".Header", Layer + ".Header.Invite");

                container.Add(
                    new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 0", OffsetMax = "-5 0"
                        },
                        Text =
                        {
                            Text = Msg(player, InvitedToClan),
                            Align = TextAnchor.MiddleLeft,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 12,
                            Color = "1 1 1 1"
                        }
                    }, Layer + ".Header.Invite");

                container.Add(
                    new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 0", OffsetMax = "-5 0"
                        },
                        Text =
                        {
                            Text = Msg(player, NextPage),
                            Align = TextAnchor.MiddleRight,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 12,
                            Color = "1 1 1 1"
                        }
                    }, Layer + ".Header.Invite");

                container.Add(
                    new CuiButton
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                        Text = {Text = ""},
                        Button = {Color = "0 0 0 0", Command = "UI_Clans page 45"}
                    }, Layer + ".Header.Invite");
            }

            #endregion

            CuiHelper.DestroyUi(player, Layer + ".Header");
        }

        private void ClanMemberProfileUi(BasePlayer player, ulong target)
        {
            #region Fields

            var clan = FindClanByID(player.userID);
            if (clan == null) return;

            var data = GetPlayerData(target);
            if (data == null) return;

            #endregion

            var container = new CuiElementContainer();

            #region Background

            container.Add(
                new CuiPanel {RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"}, Image = {Color = "0 0 0 0"}},
                Layer + ".Second.Main", Layer + ".Content");

            #endregion

            #region Header

            HeaderUi(ref container, player, clan, 1, Msg(player, ClansMenuTitle), "UI_Clans page 1");

            #endregion

            #region Title

            container.Add(
                new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "2.5 -30", OffsetMax = "225 0"
                    },
                    Text =
                    {
                        Text = Msg(player, ProfileTitle),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content");

            #endregion

            #region Avatar

            container.Add(new CuiElement
            {
                Parent = Layer + ".Content",
                Components =
                {
                    new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", $"avatar_{target}")},
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -170", OffsetMax = "140 -30"
                    }
                }
            });

            #endregion

            #region Name

            container.Add(
                new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "160 -50", OffsetMax = "400 -30"
                    },
                    Text =
                    {
                        Text = $"{data.DisplayName}",
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 16,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content");

            #endregion

            #region Farm

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "160 -115", OffsetMax = "300 -85"
                    },
                    Image = {Color = Color3}
                }, Layer + ".Content", Layer + ".Content.Farm");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"},
                    Text =
                    {
                        Text = Msg(player, GatherTitle),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content.Farm");

            var progress = data.GetTotalFarm(clan);

            if (progress > 0)
                container.Add(
                    new CuiPanel
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = $"{progress} 0.9"}, Image = {Color = Color2}
                    }, Layer + ".Content.Farm", Layer + ".Content.Farm.Progress");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "-5 0"},
                    Text =
                    {
                        Text = $"{(progress > 0 ? Math.Round(progress * 100f) : 0)}%",
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content.Farm");

            #endregion

            #region Last played

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "320 -115", OffsetMax = "460 -85"
                    },
                    Image = {Color = Color3}
                }, Layer + ".Content", Layer + ".Content.LastPayed");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"},
                    Text =
                    {
                        Text = Msg(player, LastLoginTitle),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content.LastPayed");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "-5 0"},
                    Text =
                    {
                        Text = $"{data.LastLogin}",
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content.LastPayed");

            #endregion

            #region Owner Buttons

            if (clan.IsOwner(player.userID))
            {
                var isModerator = clan.IsModerator(target);
                container.Add(
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "170 -165", OffsetMax = "260 -140"
                        },
                        Text =
                        {
                            Text = isModerator ? Msg(player, DemoteModerTitle) : Msg(player, PromoteModerTitle),
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 10,
                            Color = "1 1 1 1"
                        },
                        Button =
                        {
                            Color = isModerator ? Color4 : Color2,
                            Command = isModerator
                                ? $"UI_Clans moder undo {target}"
                                : $"UI_Clans moder set {target}"
                        }
                    }, Layer + ".Content");

                container.Add(
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "270 -165", OffsetMax = "360 -140"
                        },
                        Text =
                        {
                            Text = Msg(player, PromoteLeaderTitle),
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 10,
                            Color = "1 1 1 1"
                        },
                        Button = {Color = Color4, Command = $"UI_Clans leader tryset {target}"}
                    }, Layer + ".Content");

                container.Add(
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "370 -165", OffsetMax = "460 -140"
                        },
                        Text =
                        {
                            Text = Msg(player, KickTitle),
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 10,
                            Color = "1 1 1 1"
                        },
                        Button =
                        {
                            Color = Color6, Command = player.userID != target ? $"UI_Clans kick {target}" : ""
                        }
                    }, Layer + ".Content");
            }

            #endregion

            #region Farm

            if (clan.ResourceStandarts.Count > 0)
            {
                #region Title

                container.Add(
                    new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "2.5 -200", OffsetMax = "225 -185"
                        },
                        Text =
                        {
                            Text = Msg(player, GatherRatesTitle),
                            Align = TextAnchor.MiddleLeft,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 10,
                            Color = "1 1 1 1"
                        }
                    }, Layer + ".Content");

                #endregion

                var ySwitch = -205f;
                var amountOnString = 6;

                var xSwitch = 0f;
                var Height = 75f;
                var Width = 75f;
                var Margin = 5f;

                var z = 1;
                foreach (var standart in clan.ResourceStandarts)
                {
                    container.Add(
                        new CuiPanel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 1",
                                AnchorMax = "0 1",
                                OffsetMin = $"{xSwitch} {ySwitch - Height}",
                                OffsetMax = $"{xSwitch + Width} {ySwitch}"
                            },
                            Image = {Color = Color3}
                        }, Layer + ".Content", Layer + $".Standarts.{z}");

                    container.Add(new CuiElement
                    {
                        Parent = Layer + $".Standarts.{z}",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = ImageLibrary.Call<string>("GetImage", standart.Value.ShortName)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.5 1",
                                AnchorMax = "0.5 1",
                                OffsetMin = "-20 -45",
                                OffsetMax = "20 -5"
                            }
                        }
                    });

                    #region Progress

                    var one = data.GetValue(standart.Value.ShortName);
                    var two = standart.Value.Amount;

                    progress = one / two;

                    container.Add(
                        new CuiPanel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = "0 5"
                            },
                            Image = {Color = Color4}
                        }, Layer + $".Standarts.{z}", Layer + $".Standarts.{z}.Progress");

                    if (progress > 0)
                        container.Add(
                            new CuiPanel
                            {
                                RectTransform = {AnchorMin = "0 0", AnchorMax = $"{progress} 0", OffsetMax = "0 5"},
                                Image = {Color = Color2}
                            }, Layer + $".Standarts.{z}");

                    container.Add(
                        new CuiLabel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = "0 20"
                            },
                            Text =
                            {
                                Text = $"{one}/<b>{two}</b>",
                                Align = TextAnchor.UpperCenter,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 10,
                                Color = "1 1 1 1"
                            }
                        }, Layer + $".Standarts.{z}");

                    #endregion

                    if (z % amountOnString == 0)
                    {
                        xSwitch = 0;
                        ySwitch = ySwitch - Margin - Height;
                    }
                    else
                    {
                        xSwitch += Margin + Width;
                    }

                    z++;
                }
            }

            #endregion

            CuiHelper.DestroyUi(player, Layer + ".Content");
            
            //CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }

        private void ProfileUi(BasePlayer player, ulong target)
        {
            var data = GetTopDataById(target);
            if (data == null) return;

            var container = new CuiElementContainer();

            var clan = FindClanByID(player.userID);

            #region Menu

            if (player.userID == target) MenuUi(ref container, player, 3, clan);

            #endregion

            #region Background

            container.Add(
                new CuiPanel {RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"}, Image = {Color = "0 0 0 0"}},
                Layer + ".Second.Main", Layer + ".Content");

            #endregion

            #region Header

            HeaderUi(ref container, player, clan, 3, Msg(player, ClansMenuTitle), "UI_Clans page 3");

            #endregion

            #region Title

            container.Add(
                new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "2.5 -30", OffsetMax = "225 0"
                    },
                    Text =
                    {
                        Text = Msg(player, ProfileTitle),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content");

            #endregion

            #region Avatar

            container.Add(new CuiElement
            {
                Parent = Layer + ".Content",
                Components =
                {
                    new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", $"avatar_{target}")},
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -170", OffsetMax = "140 -30"
                    }
                }
            });

            #endregion

            #region Name

            container.Add(
                new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "160 -50", OffsetMax = "400 -30"
                    },
                    Text =
                    {
                        Text = $"{data.Data.DisplayName}",
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 16,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content");

            #endregion

            #region Clan Name

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "160 -110", OffsetMax = "460 -80"
                    },
                    Image = {Color = Color3}
                }, Layer + ".Content", Layer + ".Content.ClanName");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"},
                    Text =
                    {
                        Text = Msg(player, ClanNameTitle),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content.ClanName");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "-5 0"},
                    Text =
                    {
                        Text = $"{FindClanByID(target)?.ClanTag}",
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content.ClanName");

            #endregion

            #region Rating

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "160 -165", OffsetMax = "300 -135"
                    },
                    Image = {Color = Color3}
                }, Layer + ".Content", Layer + ".Content.Rating");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"},
                    Text =
                    {
                        Text = Msg(player, RatingTitle),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content.Rating");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "-5 0"},
                    Text =
                    {
                        Text = $"{data.Top}",
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content.Rating");

            #endregion

            #region Score

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "320 -165", OffsetMax = "460 -135"
                    },
                    Image = {Color = Color3}
                }, Layer + ".Content", Layer + ".Content.Score");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"},
                    Text =
                    {
                        Text = Msg(player, ScoreTitle),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content.Score");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "-5 0"},
                    Text =
                    {
                        Text = $"{data.Data.Score}",
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content.Score");

            #endregion

            #region Kills

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -220", OffsetMax = "140 -190"
                    },
                    Image = {Color = Color3}
                }, Layer + ".Content", Layer + ".Content.Kills");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"},
                    Text =
                    {
                        Text = Msg(player, KillsTitle),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content.Kills");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "-5 0"},
                    Text =
                    {
                        Text = $"{data.Data.Kills}",
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content.Kills");

            #endregion

            #region Deaths

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "160 -220", OffsetMax = "300 -190"
                    },
                    Image = {Color = Color3}
                }, Layer + ".Content", Layer + ".Content.Deaths");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"},
                    Text =
                    {
                        Text = Msg(player, DeathsTitle),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content.Deaths");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "-5 0"},
                    Text =
                    {
                        Text = $"{data.Data.Deaths}",
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content.Deaths");

            #endregion

            #region KD

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "320 -220", OffsetMax = "460 -190"
                    },
                    Image = {Color = Color3}
                }, Layer + ".Content", Layer + ".Content.KD");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"},
                    Text =
                    {
                        Text = Msg(player, KDTitle),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content.KD");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "-5 0"},
                    Text =
                    {
                        Text = $"{data.Data.KD}",
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content.KD");

            #endregion

            CuiHelper.DestroyUi(player, Layer + ".Content");
            //CuiHelper.DestroyUi(player, Layer + ".Main");
            //CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }

        private void MenuUi(ref CuiElementContainer container, BasePlayer player, int page, ClanData clan = null)
        {
            var data = GetPlayerData(player);

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "10 10", OffsetMax = "185 380"
                    },
                    Image = {Color = Color3}
                }, Layer + ".Main", Layer + ".Menu");

            #region Pages

            var ySwitch = 0f;
            var Height = 35f;
            var Margin = 0f;

            foreach (var pageSettings in _config.Pages)
            {
                if (!pageSettings.Enabled || !string.IsNullOrEmpty(pageSettings.Permission) &&
                    !permission.UserHasPermission(player.UserIDString, pageSettings.Permission))
                    continue;

                switch (pageSettings.ID)
                {
                    case 5:
                        if (clan != null && !clan.IsModerator(player.userID)) continue;
                        break;
                    case 7:
                        if (!_config.AllianceSettings.Enabled) continue;
                        break;
                }

                container.Add(
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "1 1",
                            OffsetMin = $"0 {ySwitch - Height}",
                            OffsetMax = $"0 {ySwitch}"
                        },
                        Text =
                        {
                            Text = $"     {Msg(player, pageSettings.Key)}",
                            Align = TextAnchor.MiddleLeft,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 12,
                            Color = "1 1 1 1"
                        },
                        Button =
                        {
                            Color = pageSettings.ID == page ? HexToCuiColor(_config.UI.Color2, 33) : "0 0 0 0",
                            Command = $"UI_Clans page {pageSettings.ID}"
                        }
                    }, Layer + ".Menu", Layer + $".Menu.Page.{pageSettings.Key}");

                if (pageSettings.ID == page)
                    container.Add(
                        new CuiPanel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = "0 0", OffsetMax = "5 0"
                            },
                            Image = {Color = Color2}
                        }, Layer + $".Menu.Page.{pageSettings.Key}");

                ySwitch = ySwitch - Height - Margin;
            }

            #endregion

            #region Notify

            if (clan == null)
            {
                container.Add(
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-75 10", OffsetMax = "75 40"
                        },
                        Text =
                        {
                            Text = Msg(player, CreateClanTitle),
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 12,
                            Color = "1 1 1 1"
                        },
                        Button = {Color = Color2, Command = "UI_Clans createclan"}
                    }, Layer + ".Menu");
            }
            else
            {
                container.Add(
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-75 10", OffsetMax = "75 40"
                        },
                        Text =
                        {
                            Text = Msg(player, ProfileTitle),
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 12,
                            Color = "1 1 1 1"
                        },
                        Button = {Color = Color2, Command = $"UI_Clans showprofile {player.userID}"}
                    }, Layer + ".Menu");

                container.Add(
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.5 0",
                            AnchorMax = "0.5 0",
                            OffsetMin = "-75 50",
                            OffsetMax = $"{(_config.AllianceSettings.Enabled ? 15 : 75)} 80"
                        },
                        Text =
                        {
                            Text = Msg(player, FriendlyFireTitle),
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 12,
                            Color = "1 1 1 1"
                        },
                        Button = {Color = data.FriendlyFire ? Color2 : Color4, Command = $"UI_Clans ff {page}"}
                    }, Layer + ".Menu");

                if (_config.AllianceSettings.Enabled)
                    container.Add(
                        new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.5 0",
                                AnchorMax = "0.5 0",
                                OffsetMin = "20 50",
                                OffsetMax = "75 80"
                            },
                            Text =
                            {
                                Text = Msg(player, AllyFriendlyFireTitle),
                                Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 12,
                                Color = "1 1 1 1"
                            },
                            Button =
                            {
                                Color = data.AllyFriendlyFire ? Color4 : Color6,
                                Command = $"UI_Clans allyff {page}"
                            }
                        }, Layer + ".Menu");
            }

            #endregion

            CuiHelper.DestroyUi(player, Layer + ".Menu");
        }

        private void AcceptSetLeader(BasePlayer player, ulong target)
        {
            var container = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        //RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                        Image = {Color = HexToCuiColor(_config.UI.Color1, 99)},
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0",OffsetMin = "465 115",OffsetMax = "1145 550"}
                }, "SubContent_UI", ModalLayer
                },
                {
                    new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-70 40",
                            OffsetMax = "70 60"
                        },
                        Text =
                        {
                            Text = Msg(player, LeaderTransferTitle),
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-bold.ttf",
                            FontSize = 12,
                            Color = "1 1 1 1"
                        }
                    },
                    ModalLayer
                },
                {
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-70 10",
                            OffsetMax = "70 40"
                        },
                        Text =
                        {
                            Text = Msg(player, AcceptTitle),
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 10,
                            Color = "1 1 1 1"
                        },
                        Button = {Color = Color2, Command = $"UI_Clans leader set {target}", Close = ModalLayer}
                    },
                    ModalLayer
                },
                {
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-70 -22.5",
                            OffsetMax = "70 7.5"
                        },
                        Text =
                        {
                            Text = Msg(player, CancelTitle),
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 10,
                            Color = "1 1 1 1"
                        },
                        Button = {Color = HexToCuiColor(_config.UI.Color2, 33), Close = ModalLayer}
                    },
                    ModalLayer
                }
            };

            CuiHelper.DestroyUi(player, ModalLayer);
            /*CuiHelper.DestroyUi(player, Layer + ".Main");
            CuiHelper.DestroyUi(player, Layer);*/
            CuiHelper.AddUi(player, container);
        }

        private void ClanProfileUi(BasePlayer player, string clanTag)
        {
            var clan = FindClanByTag(clanTag);
            if (clan == null) return;

            var playerClan = FindClanByID(player.userID);

            var container = new CuiElementContainer();

            #region Menu

            MenuUi(ref container, player, 2, playerClan);

            #endregion

            #region Background

            container.Add(
                new CuiPanel {RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"}, Image = {Color = "0 0 0 0"}},
                Layer + ".Second.Main", Layer + ".Content");

            #endregion

            #region Header

            HeaderUi(ref container, player, playerClan, 2, Msg(player, ClansMenuTitle), "UI_Clans page 2");

            #endregion

            #region Title

            container.Add(
                new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "2.5 -30", OffsetMax = "225 0"
                    },
                    Text =
                    {
                        Text = Msg(player, AboutClan),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content");

            #endregion

            #region Avatar

            container.Add(new CuiElement
            {
                Parent = Layer + ".Content",
                Components =
                {
                    new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", clan.Avatar)},
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -170", OffsetMax = "140 -30"
                    }
                }
            });

            #endregion

            #region Clan Name

            container.Add(
                new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "160 -50", OffsetMax = "400 -30"
                    },
                    Text =
                    {
                        Text = $"{clan.ClanTag}",
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 16,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Content");

            #endregion

            #region Clan Leader

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "160 -105", OffsetMax = "460 -75"
                    },
                    Image = {Color = Color3}
                }, Layer + ".Content", Layer + ".Clan.Leader");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"},
                    Text =
                    {
                        Text = Msg(player, LeaderTitle),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Clan.Leader");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "10 0", OffsetMax = "0 0"},
                    Text =
                    {
                        Text = $"{clan.LeaderName}",
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Clan.Leader");

            #endregion

            #region Rating

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "160 -165", OffsetMax = "300 -135"
                    },
                    Image = {Color = Color3}
                }, Layer + ".Content", Layer + ".Clan.Rating");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"},
                    Text =
                    {
                        Text = Msg(player, RatingTitle),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Clan.Rating");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                    Text =
                    {
                        Text = $"{clan.Top}",
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Clan.Rating");

            #endregion

            #region Members

            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "320 -165", OffsetMax = "460 -135"
                    },
                    Image = {Color = Color3}
                }, Layer + ".Content", Layer + ".Clan.Members");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 20"},
                    Text =
                    {
                        Text = Msg(player, MembersTitle),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Clan.Members");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                    Text =
                    {
                        Text = $"{clan.Members.Count}",
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Clan.Members");

            #endregion

            #region Ally

            if (playerClan != null)
            {
                if (playerClan.IsModerator(player.userID) && !playerClan.IncomingAlliances.Contains(clanTag) &&
                    !playerClan.AllianceInvites.ContainsKey(clanTag))
                    container.Add(
                        new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 1",
                                AnchorMax = "0 1",
                                OffsetMin = "0 -200",
                                OffsetMax = "140 -175"
                            },
                            Text =
                            {
                                Text = Msg(player, SendAllyInvite),
                                Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 12,
                                Color = "1 1 1 1"
                            },
                            Button =
                            {
                                Color = Color2, Command = $"UI_Clans allyinvite send {clanTag}", Close = Layer
                            }
                        }, Layer + ".Content");

                if (playerClan.Alliances.Contains(clanTag))
                    container.Add(
                        new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 1",
                                AnchorMax = "0 1",
                                OffsetMin = "0 -200",
                                OffsetMax = "140 -175"
                            },
                            Text =
                            {
                                Text = Msg(player, AllyRevokeTitle),
                                Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 12,
                                Color = "1 1 1 1"
                            },
                            Button =
                            {
                                Color = Color6, Command = $"UI_Clans allyinvite revoke {clanTag}", Close = Layer
                            }
                        }, Layer + ".Content");
            }

            #endregion

            CuiHelper.DestroyUi(player, Layer + ".Content");
            //CuiHelper.DestroyUi(player, Layer + ".Main");
            //CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }

        private void SelectSkinUi(BasePlayer player, string shortName, int page = 0, bool First = false)
        {
            #region Fields

            var clan = FindClanByID(player.userID);

            var nowSkin = clan.Skins.ContainsKey(shortName) ? clan.Skins[shortName] : 0;

            var amountOnString = 6;
            var strings = 3;
            var totalAmount = amountOnString * strings;

            var Height = 115f;
            var Width = 110f;
            var Margin = 10f;

            var constSwitchX = -(amountOnString * Width + (amountOnString - 1) * Margin) / 2f;

            var xSwitch = constSwitchX;
            var ySwitch = -75f;

            var canCustomSkin = _config.Skins.CanCustomSkin && (string.IsNullOrEmpty(_config.Skins.Permission) ||
                                                                permission.UserHasPermission(player.UserIDString,
                                                                    _config.Skins.Permission));

            #endregion

            var container = new CuiElementContainer();

            #region Background

            if (First)
            {
                CuiHelper.DestroyUi(player, Layer);

                container.Add(
                    new CuiPanel
                    {
                        //RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                        Image = {Color = "0 0 0 0", Material = ""},
                        CursorEnabled = false,
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0",OffsetMin = "465 115",OffsetMax = "1145 550"}
                }, "SubContent_UI", Layer);

                container.Add(
                    new CuiButton
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                        Text = {Text = ""},
                        Button = {Color = "0 0 0 0", Close = Layer,Command = "chat.say /profile"}
                    }, Layer);
            }

            #endregion

            #region Main

            container.Add(
                new CuiPanel {RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"}, Image = {Color = Color1}}, Layer,
                Layer + ".Main");

            #region Header

            container.Add(
                new CuiPanel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 -55", OffsetMax = "0 0"},
                    Image = {Color = Color3}
                }, Layer + ".Main", Layer + ".Header");

            #region Title

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "25 0", OffsetMax = "0 0"},
                    Text =
                    {
                        Text = Msg(player, SelectSkinTitle),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 14,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".Header");

            #endregion

            #region Enter Skin

            if (canCustomSkin)
            {
                container.Add(
                    new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0.5",
                            AnchorMax = "0 0.5",
                            OffsetMin = "160 -17.5",
                            OffsetMax = "410 17.5"
                        },
                        Image = {Color = Color4}
                    }, Layer + ".Header", Layer + ".Header.EnterSkin");

                container.Add(
                    new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                        Text =
                        {
                            Text = nowSkin == 0 ? Msg(player, EnterSkinTitle) : $"{nowSkin}",
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 12,
                            Color = "1 1 1 0.65"
                        }
                    }, Layer + ".Header.EnterSkin");

                container.Add(new CuiElement
                {
                    Parent = Layer + ".Header.EnterSkin",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            FontSize = 12,
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf",
                            Command = $"UI_Clans setskin {shortName} ",
                            Color = "1 1 1 0.95",
                            CharsLimit = 32
                        },
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                    }
                });
            }

            #endregion

            #region Pages

            xSwitch = canCustomSkin ? 415 : 160;

            container.Add(
                new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0.5",
                        AnchorMax = "0 0.5",
                        OffsetMin = $"{xSwitch} -17.5",
                        OffsetMax = $"{xSwitch + 35} 17.5"
                    },
                    Text =
                    {
                        Text = Msg(player, BackPage),
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    },
                    Button =
                    {
                        Color = Color4, Command = page != 0 ? $"UI_Clans editskin {shortName} {page - 1}" : ""
                    }
                }, Layer + ".Header");

            xSwitch += 40;

            container.Add(
                new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0.5",
                        AnchorMax = "0 0.5",
                        OffsetMin = $"{xSwitch} -17.5",
                        OffsetMax = $"{xSwitch + 35} 17.5"
                    },
                    Text =
                    {
                        Text = Msg(player, NextPage),
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    },
                    Button =
                    {
                        Color = Color2,
                        Command = _config.Skins.ItemSkins[shortName].Count > (page + 1) * totalAmount
                            ? $"UI_Clans editskin {shortName} {page + 1}"
                            : ""
                    }
                }, Layer + ".Header");

            #endregion

            #region Close

            container.Add(
                new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "1 0.5",
                        AnchorMax = "1 0.5",
                        OffsetMin = "-35 -12.5",
                        OffsetMax = "-10 12.5"
                    },
                    Text =
                    {
                        Text = Msg(player, CloseTitle),
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 10,
                        Color = "1 1 1 1"
                    },
                    Button = {Color = Color2, Command = "UI_Clans page 6"}
                }, Layer + ".Header");

            #endregion

            #endregion

            #region Items

            xSwitch = constSwitchX;

            var i = 1;
            foreach (var def in _config.Skins.ItemSkins[shortName].Skip(page * totalAmount).Take(totalAmount))
            {
                container.Add(
                    new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.5 1",
                            AnchorMax = "0.5 1",
                            OffsetMin = $"{xSwitch} {ySwitch - Height}",
                            OffsetMax = $"{xSwitch + Width} {ySwitch}"
                        },
                        Image = {Color = nowSkin == def ? Color4 : Color3}
                    }, Layer + ".Main", Layer + $".Item.{i}");

                container.Add(new CuiElement
                {
                    Parent = Layer + $".Item.{i}",
                    Components =
                    {
                        new CuiRawImageComponent {Png = GetItemImage(shortName, def)},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 1",
                            AnchorMax = "0.5 1",
                            OffsetMin = "-35 -80",
                            OffsetMax = "35 -10"
                        }
                    }
                });

                container.Add(
                    new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = "0 25"
                        },
                        Text =
                        {
                            Text = Msg(player, SelectTitle),
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 10,
                            Color = "1 1 1 1"
                        },
                        Button = {Color = Color2, Command = $"UI_Clans selectskin {shortName} {def}"}
                    }, Layer + $".Item.{i}");

                if (i % amountOnString == 0)
                {
                    xSwitch = constSwitchX;
                    ySwitch = ySwitch - Height - Margin;
                }
                else
                {
                    xSwitch += Width + Margin;
                }

                i++;
            }

            #endregion

            #endregion

            CuiHelper.DestroyUi(player, Layer + ".Main");
            //CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }

        private void PagesUi(ref CuiElementContainer container, BasePlayer player, int pages, int page, int zPage)
        {
            container.Add(
                new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-25 10", OffsetMax = "25 35"
                    },
                    Image = {Color = Color4}
                }, Layer + ".Content", Layer + ".Pages");

            container.Add(
                new CuiLabel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                    Text =
                    {
                        Text = $"{zPage + 1}",
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 12,
                        Color = "1 1 1 0.65"
                    }
                }, Layer + ".Pages");

            container.Add(new CuiElement
            {
                Parent = Layer + ".Pages",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf",
                        Command = $"UI_Clans inputpage {pages} {page} ",
                        Color = "1 1 1 0.95",
                        CharsLimit = 32
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });

            container.Add(
                new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-55 10", OffsetMax = "-30 35"
                    },
                    Text =
                    {
                        Text = Msg(player, BackPage),
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    },
                    Button = {Color = Color4, Command = zPage != 0 ? $"UI_Clans page {page} {zPage - 1}" : ""}
                }, Layer + ".Content");

            container.Add(
                new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "30 10", OffsetMax = "55 35"
                    },
                    Text =
                    {
                        Text = Msg(player, NextPage),
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 12,
                        Color = "1 1 1 1"
                    },
                    Button =
                    {
                        Color = Color2, Command = pages > zPage + 1 ? $"UI_Clans page {page} {zPage + 1}" : ""
                    }
                }, Layer + ".Content");
        }

        #endregion

        #region Utils

        private void RegisterPermissions()
        {
            if (_config.PermissionSettings.UseClanCreating &&
                !string.IsNullOrEmpty(_config.PermissionSettings.ClanCreating) &&
                !permission.PermissionExists(_config.PermissionSettings.ClanCreating))
                permission.RegisterPermission(_config.PermissionSettings.ClanCreating, this);

            if (_config.PermissionSettings.UseClanJoining &&
                !string.IsNullOrEmpty(_config.PermissionSettings.ClanJoining) &&
                !permission.PermissionExists(_config.PermissionSettings.ClanJoining))
                permission.RegisterPermission(_config.PermissionSettings.ClanJoining, this);

            if (_config.PermissionSettings.UseClanKick && !string.IsNullOrEmpty(_config.PermissionSettings.ClanKick) &&
                !permission.PermissionExists(_config.PermissionSettings.ClanKick))
                permission.RegisterPermission(_config.PermissionSettings.ClanKick, this);

            if (_config.PermissionSettings.UseClanLeave &&
                !string.IsNullOrEmpty(_config.PermissionSettings.ClanLeave) &&
                !permission.PermissionExists(_config.PermissionSettings.ClanLeave))
                permission.RegisterPermission(_config.PermissionSettings.ClanLeave, this);

            if (_config.PermissionSettings.UseClanDisband &&
                !string.IsNullOrEmpty(_config.PermissionSettings.ClanDisband) &&
                !permission.PermissionExists(_config.PermissionSettings.ClanDisband))
                permission.RegisterPermission(_config.PermissionSettings.ClanDisband, this);

            if (_config.Skins.CanCustomSkin && !string.IsNullOrEmpty(_config.Skins.Permission) &&
                !permission.PermissionExists(_config.Skins.Permission))
                permission.RegisterPermission(_config.Skins.Permission, this);

            _config.Pages.ForEach(page =>
            {
                if (!string.IsNullOrEmpty(page.Permission) && !permission.PermissionExists(page.Permission))
                    permission.RegisterPermission(page.Permission, this);
            });
        }

        private void LoadColors()
        {
            Color1 = HexToCuiColor(_config.UI.Color1);
            Color2 = HexToCuiColor(_config.UI.Color2);
            Color3 = HexToCuiColor(_config.UI.Color3);
            Color4 = HexToCuiColor(_config.UI.Color4);
            Color5 = HexToCuiColor(_config.UI.Color5);
            Color6 = HexToCuiColor(_config.UI.Color6);
        }

        private void PurgeClans()
        {
            if (_config.PurgeSettings.Enabled)
            {
                var toRemove = Pool.GetList<ClanData>();

                ClansList.ForEach(clan =>
                {
                    if (DateTime.Now.Subtract(clan.CreationTime).Days > _config.PurgeSettings.OlderThanDays)
                        toRemove.Add(clan);
                });

                if (_config.PurgeSettings.ListPurgedClans)
                {
                    var str = string.Join("\n",
                        toRemove.Select(clan =>
                            $"Purged - [{clan.ClanTag}] | Owner: {clan.LeaderID} | Last Online: {clan.LastOnlineTime}"));
                    ;
                    if (!string.IsNullOrEmpty(str)) Puts(str);
                }

                toRemove.ForEach(clan => ClansList.Remove(clan));

                Pool.FreeList(ref toRemove);
            }
        }

        private void LoadImages()
        {
            timer.In(5, () =>
            {
                if (!ImageLibrary)
                {
                    PrintWarning("IMAGE LIBRARY IS NOT INSTALLED");
                }
                else
                {
                    var imagesList = new Dictionary<string, string>();

                    var itemIcons = new List<KeyValuePair<string, ulong>>();

                    _config.Resources.ForEach(item => itemIcons.Add(new KeyValuePair<string, ulong>(item, 0)));

                    foreach (var itemSkin in _config.Skins.ItemSkins)
                        itemSkin.Value.ForEach(skin =>
                            itemIcons.Add(new KeyValuePair<string, ulong>(itemSkin.Key, skin)));

                    _config.AvailableStandartItems.ForEach(item =>
                        itemIcons.Add(new KeyValuePair<string, ulong>(item, 0)));

                    imagesList[_config.DefaultAvatar] = _config.DefaultAvatar;

                    ClansList.ForEach(clan =>
                    {
                        if (!string.IsNullOrEmpty(clan.Avatar)) imagesList[clan.Avatar] = clan.Avatar;
                    });

                    if (itemIcons.Count > 0) ImageLibrary?.Call("LoadImageList", Title, itemIcons, null);

                    ImageLibrary?.Call("ImportImageList", Title, imagesList, 0UL, true);
                }
            });
        }

        private void FillingTeams()
        {
            if (_config.AutoTeamCreation)
            {
                RelationshipManager.maxTeamSize = _config.LimitSettings.MemberLimit;

                ClansList.ForEach(clan =>
                {
                    clan.CreateTeam();

                    clan.Members.ForEach(member => clan.AddPlayer(member));
                });
            }
        }

        private static string HexToCuiColor(string HEX, float Alpha = 100)
        {
            if (string.IsNullOrEmpty(HEX)) HEX = "#FFFFFF";

            var str = HEX.Trim('#');
            if (str.Length != 6) throw new Exception(HEX);
            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);

            return $"{(double) r / 255} {(double) g / 255} {(double) b / 255} {Alpha / 100f}";
        }

        private string BetterChat_FormattedClanTag(IPlayer player)
        {
            var clan = FindClanByID(player.Id);
            return clan == null ? string.Empty : $"{_config.ChatSettings.TagFormat.Replace("{tag}", clan.ClanTag)}";
        }

        private bool IsTeammates(ulong player, ulong friend)
        {
            return player == friend ||
                   RelationshipManager.ServerInstance.FindPlayersTeam(player)?.members?.Contains(friend) == true ||
                   FindClanByID(player)?.IsMember(friend) == true;
        }

        #region Avatar

        private readonly Regex Regex = new Regex(@"<avatarFull><!\[CDATA\[(.*)\]\]></avatarFull>");

        private void GetAvatar(string userId, Action<string> callback)
        {
            if (callback == null) return;

            try
            {
                webrequest.Enqueue($"http://steamcommunity.com/profiles/{userId}?xml=1", null, (code, response) =>
                {
                    if (code != 200 || response == null) return;

                    var avatar = Regex.Match(response).Groups[1].ToString();
                    if (string.IsNullOrEmpty(avatar)) return;

                    callback.Invoke(avatar);
                }, this);
            }
            catch (Exception e)
            {
                PrintError($"{e.Message}");
            }
        }

        private void StartLoadingAvatars()
        {
            Puts("Loading avatars started!");

            ActionAvatars = ServerMgr.Instance.StartCoroutine(LoadAvatars());
        }

        private IEnumerator LoadAvatars()
        {
            foreach (var player in covalence.Players.All)
            {
                GetAvatar(player.Id, avatar => ImageLibrary?.Call("AddImage", avatar, $"avatar_{player.Id}"));

                yield return CoroutineEx.waitForSeconds(0.5f);
            }

            Puts("Uploading avatars is complete!");
        }

        #endregion

        private void FillingStandartItems()
        {
            _config.AvailableStandartItems.ForEach(shortName =>
            {
                var def = ItemManager.FindItemDefinition(shortName);
                if (def == null) return;

                StandartItems.Add(def);
            });
        }

        private static void ApplySkinToItem(Item item, ulong Skin)
        {
            item.skin = Skin;
            item.MarkDirty();

            var heldEntity = item.GetHeldEntity();
            if (heldEntity == null) return;

            heldEntity.skinID = Skin;
            heldEntity.SendNetworkUpdate();
        }

        private static string GetValue(float value)
        {
            if (!_config.UI.ValueAbbreviation) return Mathf.Round(value).ToString(CultureInfo.InvariantCulture);

            var t = string.Empty;
            while (value > 1000)
            {
                t += "K";
                value /= 1000;
            }

            return Mathf.Round(value) + t;
        }

        #endregion

        #region API

        private static void ClanCreate(string tag)
        {
            Interface.CallHook("OnClanCreate", tag);
        }

        private static void ClanUpdate(string tag)
        {
            Interface.CallHook("OnClanUpdate", tag);
        }

        private static void ClanDestroy(string tag)
        {
            Interface.CallHook("OnClanDestroy", tag);
        }

        private static void ClanDisbanded(List<string> memberUserIDs)
        {
            Interface.CallHook("OnClanDisbanded", memberUserIDs);
        }

        private static void ClanDisbanded(string tag, List<string> memberUserIDs)
        {
            Interface.CallHook("OnClanDisbanded", tag, memberUserIDs);
        }

        private static void ClanMemberJoined(string userID, string tag)
        {
            Interface.CallHook("OnClanMemberJoined", userID, tag);
        }

        private static void ClanMemberGone(string userID, List<string> memberUserIDs)
        {
            Interface.CallHook("OnClanMemberGone", userID, memberUserIDs);
        }

        private static void ClanMemberGone(string userID, string tag)
        {
            Interface.CallHook("OnClanMemberGone", userID, tag);
        }

        private ClanData FindClanByID(string userId)
        {
            return FindClanByID(ulong.Parse(userId));
        }

        private ClanData FindClanByID(ulong userId)
        {
            return ClansList.Find(clan => clan.IsMember(userId));
        }

        private ClanData FindClanByTag(string tag)
        {
            return ClansList.Find(clan => clan.ClanTag == tag);
        }

        private bool PlayerHasClan(ulong userId)
        {
            return FindClanByID(userId) != null;
        }

        private bool IsClanMember(ulong playerId, ulong otherId)
        {
            var clan = FindClanByID(playerId);
            return clan != null && clan.IsMember(otherId);
        }

        private JObject GetClan(string tag)
        {
            return FindClanByTag(tag)?.ToJObject();
        }

        private string GetClanOf(BasePlayer target)
        {
            return GetClanOf(target.userID);
        }

        private string GetClanOf(string target)
        {
            return GetClanOf(ulong.Parse(target));
        }

        private string GetClanOf(ulong target)
        {
            return FindClanByID(target)?.ClanTag;
        }

        private JArray GetAllClans()
        {
            return new JArray(ClansList.Select(x => x.ToJObject()));
        }

        private List<string> GetClanMembers(string target)
        {
            return GetClanMembers(ulong.Parse(target));
        }

        private List<string> GetClanMembers(ulong target)
        {
            return FindClanByID(target)?.Members.Select(x => x.ToString()).ToList();
        }

        #endregion

        #region Invites

        #region Players

        private void SendInvite(BasePlayer inviter, ulong target)
        {
            if (inviter == null) return;

            var clan = FindClanByID(inviter.userID);
            if (clan == null) return;

            if (!clan.IsModerator(inviter.userID))
            {
                Reply(inviter, NotModer);
                return;
            }

            if (clan.Members.Count >= _config.LimitSettings.MemberLimit)
            {
                Reply(inviter, ALotOfMembers);
                return;
            }

            var targetClan = FindClanByID(target);
            if (targetClan != null)
            {
                Reply(inviter, HeAlreadyClanMember);
                return;
            }

            var data = GetPlayerData(target);
            if (data == null) return;

            if (data.Invites.ContainsKey(clan.ClanTag) || clan.InvitedPlayers.ContainsKey(target))
            {
                Reply(inviter, AlreadyInvitedInClan);
                return;
            }

            var inviterName = $"{GetPlayerData(inviter)?.DisplayName ?? inviter.Connection.username}";
            data.Invites.Add(clan.ClanTag, new InviteData {InviterId = inviter.userID, InviterName = inviterName});

            clan.InvitedPlayers.Add(target, inviter.userID);

            Reply(inviter, SuccessInvited, data.DisplayName, clan.ClanTag);

            var targetPlayer = BasePlayer.FindByID(target);
            if (targetPlayer != null) Reply(targetPlayer, SuccessInvitedSelf, inviterName, clan.ClanTag);
        }

        private void AcceptInvite(BasePlayer player, string tag)
        {
            if (player == null || string.IsNullOrEmpty(tag)) return;

            if (_config.PermissionSettings.UseClanJoining &&
                !string.IsNullOrEmpty(_config.PermissionSettings.ClanJoining) &&
                !permission.UserHasPermission(player.UserIDString, _config.PermissionSettings.ClanJoining))
            {
                Reply(player, NoPermJoinClan);
                return;
            }

            var data = GetPlayerData(player);
            if (data == null) return;

            var clan = FindClanByID(player.userID);
            if (clan != null)
            {
                Reply(player, AlreadyClanMember);
                return;
            }

            clan = FindClanByTag(tag);
            if (clan == null)
            {
                data.Invites.Remove(tag);
                return;
            }

            if (clan.Members.Count >= _config.LimitSettings.MemberLimit)
            {
                Reply(player, ALotOfMembers);
                return;
            }

            InviteData inviteData;
            if (!data.Invites.TryGetValue(tag, out inviteData)) return;

            clan.Join(player);
            Reply(player, ClanJoined, clan.ClanTag);

            var inviter = BasePlayer.FindByID(inviteData.InviterId);
            if (inviter != null) Reply(inviter, WasInvited, GetPlayerData(player).DisplayName);

            data.Invites.Clear();
        }

        private void CancelInvite(BasePlayer player, string tag)
        {
            if (player == null || string.IsNullOrEmpty(tag)) return;

            var data = GetPlayerData(player);
            if (data == null) return;

            FindClanByTag(tag)?.InvitedPlayers.Remove(player.userID);

            InviteData inviteData;
            if (!data.Invites.TryGetValue(tag, out inviteData)) return;

            FindClanByTag(tag)?.InvitedPlayers.Remove(player.userID);

            data.Invites.Remove(tag);

            Reply(player, DeclinedInvite, tag);

            var inviter = BasePlayer.FindByID(inviteData.InviterId);
            if (inviter != null) Reply(inviter, DeclinedInviteSelf);
        }

        private void WithdrawInvite(BasePlayer inviter, ulong target)
        {
            var clan = FindClanByID(inviter.userID);
            if (clan == null) return;

            if (!clan.IsModerator(inviter.userID))
            {
                Reply(inviter, NotModer);
                return;
            }

            var data = GetPlayerData(target);
            if (data == null) return;

            if (!clan.InvitedPlayers.ContainsKey(target))
            {
                Reply(inviter, DidntReceiveInvite, data.DisplayName);
                return;
            }

            var clanInviter = clan.InvitedPlayers[target];
            if (clanInviter != inviter.userID)
            {
                var clanInviterPlayer = BasePlayer.FindByID(clanInviter);
                if (clanInviterPlayer != null)
                    Reply(clanInviterPlayer, YourInviteDeclined, data.DisplayName, GetPlayerData(inviter).DisplayName);
            }

            data.Invites.Remove(clan.ClanTag);
            clan.InvitedPlayers.Remove(target);

            var targetPlayer = BasePlayer.FindByID(target);
            if (targetPlayer != null) Reply(targetPlayer, CancelledInvite, clan.ClanTag);

            Reply(inviter, CancelledYourInvite, data.DisplayName);
        }

        private bool HasInvite(BasePlayer player)
        {
            if (player == null) return false;

            var data = GetPlayerData(player);
            if (data == null) return false;

            return data.Invites.Count > 0;
        }

        #endregion

        #region Alliances

        private void AllySendInvite(BasePlayer player, string clanTag)
        {
            if (player == null || string.IsNullOrEmpty(clanTag)) return;

            var clan = FindClanByID(player.userID);
            if (clan == null) return;

            if (!clan.IsModerator(player.userID))
            {
                Reply(player, NotModer);
                return;
            }

            var targetClan = FindClanByTag(clanTag);
            if (targetClan == null) return;

            if (clan.Alliances.Count >= _config.LimitSettings.AlliancesLimit ||
                targetClan.Alliances.Count >= _config.LimitSettings.AlliancesLimit)
            {
                Reply(player, ALotOfAlliances);
                return;
            }

            if (clan.AllianceInvites.ContainsKey(clanTag))
            {
                Reply(player, AllInviteExist);
                return;
            }

            if (targetClan.Alliances.Contains(clan.ClanTag))
            {
                Reply(player, AlreadyAlliance);
                return;
            }

            if (clan.IncomingAlliances.Contains(clanTag))
            {
                AllyAcceptInvite(player, clanTag);
                return;
            }

            targetClan.IncomingAlliances.Add(clan.ClanTag);
            clan.AllianceInvites.Add(clanTag, player.userID);

            clan.Members.FindAll(member => member != player.userID)
                .ForEach(member => Reply(BasePlayer.FindByID(member), AllySendedInvite, player.displayName,
                    targetClan.ClanTag));

            Reply(player, YouAllySendedInvite, targetClan.ClanTag);

            targetClan.Members.ForEach(member =>
                Reply(BasePlayer.FindByID(member), SelfAllySendedInvite, clan.ClanTag));
        }

        private void AllyAcceptInvite(BasePlayer player, string clanTag)
        {
            if (player == null || string.IsNullOrEmpty(clanTag)) return;

            var clan = FindClanByID(player.userID);
            if (clan == null) return;

            var targetClan = FindClanByTag(clanTag);
            if (targetClan == null) return;

            if (clan.Alliances.Count >= _config.LimitSettings.AlliancesLimit ||
                targetClan.Alliances.Count >= _config.LimitSettings.AlliancesLimit)
            {
                Reply(player, ALotOfAlliances);
                return;
            }

            if (!clan.IncomingAlliances.Contains(targetClan.ClanTag))
            {
                Reply(player, NoFoundInviteAlly, targetClan.ClanTag);
                return;
            }

            targetClan.IncomingAlliances.Remove(clan.ClanTag);
            targetClan.AllianceInvites.Remove(clan.ClanTag);

            clan.AllianceInvites.Remove(targetClan.ClanTag);
            clan.IncomingAlliances.Remove(targetClan.ClanTag);

            clan.Alliances.Add(targetClan.ClanTag);
            targetClan.Alliances.Add(clan.ClanTag);

            clan.Members.ForEach(
                member => Reply(BasePlayer.FindByID(member), AllyAcceptInviteTitle, targetClan.ClanTag));
            targetClan.Members.ForEach(
                member => Reply(BasePlayer.FindByID(member), AllyAcceptInviteTitle, clan.ClanTag));
        }

        private void AllyCancelInvite(BasePlayer player, string clanTag)
        {
            if (player == null || string.IsNullOrEmpty(clanTag)) return;

            var clan = FindClanByID(player.userID);
            if (clan == null) return;

            var targetClan = FindClanByTag(clanTag);
            if (targetClan == null) return;

            clan.IncomingAlliances.Remove(targetClan.ClanTag);
            targetClan.AllianceInvites.Remove(clan.ClanTag);

            clan.Members.ForEach(member => Reply(BasePlayer.FindByID(member), RejectedInviteTitle, targetClan.ClanTag));
            targetClan.Members.ForEach(member =>
                Reply(BasePlayer.FindByID(member), SelfRejectedInviteTitle, clan.ClanTag));
        }

        private void AllyWithdrawInvite(BasePlayer player, string clanTag)
        {
            if (player == null || string.IsNullOrEmpty(clanTag)) return;

            var clan = FindClanByID(player.userID);
            if (clan == null) return;

            var targetClan = FindClanByTag(clanTag);
            if (targetClan == null) return;

            clan.AllianceInvites.Remove(clanTag);
            targetClan.IncomingAlliances.Remove(clanTag);

            clan.Members.ForEach(member => Reply(BasePlayer.FindByID(member), WithdrawInviteTitle, targetClan.ClanTag));
            targetClan.Members.ForEach(member =>
                Reply(BasePlayer.FindByID(member), SelfWithdrawInviteTitle, clan.ClanTag));
        }

        private void AllyRevoke(BasePlayer player, string clanTag)
        {
            if (player == null || string.IsNullOrEmpty(clanTag)) return;

            var clan = FindClanByID(player.userID);
            if (clan == null) return;

            var targetClan = FindClanByTag(clanTag);
            if (targetClan == null) return;

            if (!clan.Alliances.Contains(clanTag))
            {
                Reply(player, NoAlly, clanTag);
                return;
            }

            clan.Alliances.Remove(targetClan.ClanTag);
            targetClan.Alliances.Remove(clan.ClanTag);

            clan.Members.ForEach(member => Reply(BasePlayer.FindByID(member), SelfBreakAlly, targetClan.ClanTag));

            targetClan.Members.ForEach(member => Reply(BasePlayer.FindByID(member), BreakAlly, clan.ClanTag));
        }

        private bool HasAllyInvite(ClanData clan, string clanTag)
        {
            return clan?.AllianceInvites.ContainsKey(clanTag) ?? false;
        }

        private bool HasAllyIncomingInvite(ClanData clan, string clanTag)
        {
            return clan?.IncomingAlliances.Contains(clanTag) ?? false;
        }

        #endregion

        #endregion

        #region Clan Creating

        private readonly Dictionary<BasePlayer, CreateClanData> ClanCreating =
            new Dictionary<BasePlayer, CreateClanData>();

        private class CreateClanData
        {
            public string Tag;

            public string Avatar;
        }

        #endregion

        #region Rating

        private List<ClanData> TopClans = new List<ClanData>();

        private readonly Dictionary<ulong, TopPlayerData> TopPlayers = new Dictionary<ulong, TopPlayerData>();

        private class TopPlayerData
        {
            public readonly ulong UserId;

            public int Top;

            public readonly PlayerData Data;

            public float Score()
            {
                return Data.Score;
            }

            public string GetParams(string value)
            {
                switch (value)
                {
                    case "name":
                        return Data.DisplayName;
                    case "score":
                        return GetValue(Score());
                    case "resources":
                        return GetValue(Data.Resources);
                    default:
                        return GetValue(Data.GetValue(value));
                }
            }

            public TopPlayerData(KeyValuePair<ulong, PlayerData> x)
            {
                UserId = x.Key;
                Data = x.Value;
            }
        }

        private TopPlayerData GetTopDataById(ulong target)
        {
            TopPlayerData data;
            return TopPlayers.TryGetValue(target, out data) ? data : null;
        }

        private void HandleTop()
        {
            var topPlayers = PlayersList.Select(x => new TopPlayerData(x)).ToList();

            #region Players

            topPlayers.Sort((x, y) => y.Score().CompareTo(x.Score()));

            for (var i = 0; i < topPlayers.Count; i++)
            {
                var member = topPlayers[i];

                member.Top = i + 1;

                TopPlayers[member.UserId] = member;
            }

            #endregion

            #region Clans

            TopClans = ClansList;
            TopClans.Sort((x, y) => y.Score().CompareTo(x.Score()));

            for (var i = 0; i < TopClans.Count; i++) TopClans[i].Top = i + 1;

            #endregion
        }

        #endregion

        #region Item Skins

        private void LoadSkins()
        {
            var any = false;
            _config.Skins.ItemSkins.ToList()
                .FindAll(itemSkin => itemSkin.Value.Count == 0)
                .ForEach(itemSkin =>
                {
                    _config.Skins.ItemSkins[itemSkin.Key] =
                        ImageLibrary?.Call<List<ulong>>("GetImageList", itemSkin.Key) ??
                        new List<ulong>();

                    any = true;
                });

            if (any) SaveConfig();
        }

        private string GetItemImage(string shortname, ulong skinID = 0)
        {
            if (skinID > 0)
                if (ImageLibrary.Call<bool>("HasImage", shortname, skinID) == false &&
                    ImageLibrary.Call<Dictionary<string, object>>("GetSkinInfo", shortname, skinID) == null)
                {
                    if (string.IsNullOrEmpty(_config.SteamWebApiKey) || _config.SteamWebApiKey.Length != 32)
                    {
                        PrintError("Steam Web API key not set! Check the configuration!");
                        return ImageLibrary.Call<string>("GetImage", shortname);
                    }

                    webrequest.Enqueue("https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/",
                        $"key={_config.SteamWebApiKey}&itemcount=1&publishedfileids%5B0%5D={skinID}",
                        (code, response) =>
                        {
                            if (code != 200 || response == null)
                            {
                                PrintError(
                                    $"Image failed to download! Code HTTP error: {code} - Image Name: {shortname} - Image skinID: {skinID} - Response: {response}");
                                return;
                            }

                            var sr = JsonConvert.DeserializeObject<SteampoweredResult>(response);
                            if (sr == null || sr.response.result == 0 || sr.response.resultcount == 0)
                            {
                                PrintError(
                                    $"Image failed to download! Error: Parse JSON response - Image Name: {shortname} - Image skinID: {skinID} - Response: {response}");
                                return;
                            }

                            foreach (var publishedfiled in sr.response.publishedfiledetails)
                                ImageLibrary.Call("AddImage", publishedfiled.preview_url, shortname, skinID);
                        }, this, RequestMethod.POST);

                    return ImageLibrary.Call<string>("GetImage", "LOADING");
                }

            return ImageLibrary.Call<string>("GetImage", shortname, skinID);
        }

        #endregion

        #region SteampoweredAPI

        private class SteampoweredResult
        {
            public Response response;

            public class Response
            {
                [JsonProperty("result")] public int result;

                [JsonProperty("resultcount")] public int resultcount;

                [JsonProperty("publishedfiledetails")] public List<PublishedFiled> publishedfiledetails;

                public class PublishedFiled
                {
                    [JsonProperty("publishedfileid")] public ulong publishedfileid;

                    [JsonProperty("result")] public int result;

                    [JsonProperty("creator")] public string creator;

                    [JsonProperty("creator_app_id")] public int creator_app_id;

                    [JsonProperty("consumer_app_id")] public int consumer_app_id;

                    [JsonProperty("filename")] public string filename;

                    [JsonProperty("file_size")] public int file_size;

                    [JsonProperty("preview_url")] public string preview_url;

                    [JsonProperty("hcontent_preview")] public string hcontent_preview;

                    [JsonProperty("title")] public string title;

                    [JsonProperty("description")] public string description;

                    [JsonProperty("time_created")] public int time_created;

                    [JsonProperty("time_updated")] public int time_updated;

                    [JsonProperty("visibility")] public int visibility;

                    [JsonProperty("banned")] public int banned;

                    [JsonProperty("ban_reason")] public string ban_reason;

                    [JsonProperty("subscriptions")] public int subscriptions;

                    [JsonProperty("favorited")] public int favorited;

                    [JsonProperty("lifetime_subscriptions")]
                    public int lifetime_subscriptions;

                    [JsonProperty("lifetime_favorited")] public int lifetime_favorited;

                    [JsonProperty("views")] public int views;

                    [JsonProperty("tags")] public List<Tag> tags;

                    public class Tag
                    {
                        [JsonProperty("tag")] public string tag;
                    }
                }
            }
        }

        #endregion

        #region Lang

        private const string ClansMenuTitle = "ClansMenuTitle",
            AboutClan = "AboutClan",
            ChangeAvatar = "ChangeAvatar",
            EnterLink = "EnterLink",
            LeaderTitle = "LeaderTitle",
            GatherTitle = "GatherTitle",
            RatingTitle = "RatingTitle",
            MembersTitle = "MembersTitle",
            DescriptionTitle = "DescriptionTitle",
            NameTitle = "NameTitle",
            SteamIdTitle = "SteamIdTitle",
            ProfileTitle = "ProfileTitle",
            InvitedToClan = "InvitedToClan",
            BackPage = "BackPage",
            NextPage = "NextPage",
            TopClansTitle = "TopClansTitle",
            TopPlayersTitle = "TopPlayersTitle",
            TopTitle = "TopTitle",
            ScoreTitle = "ScoreTitle",
            KillsTitle = "KillsTitle",
            DeathsTitle = "DeathsTitle",
            KDTitle = "KDTitle",
            ResourcesTitle = "ResourcesTitle",
            LeftTitle = "LeftTitle",
            EditTitle = "EditTitle",
            InviteTitle = "InviteTitle",
            SearchTitle = "SearchTitle",
            ClanInvitation = "ClanInvitation",
            InviterTitle = "InviterTitle",
            AcceptTitle = "AcceptTitle",
            CancelTitle = "CancelTitle",
            PlayerTitle = "PlayerTitle",
            ClanTitle = "ClanTitle",
            NotMemberOfClan = "NotMemberOfClan",
            SelectItemTitle = "SelectItemTitle",
            CloseTitle = "CloseTitle",
            SelectTitle = "SelectTitle",
            ClanCreationTitle = "ClanCreationTitle",
            ClanNameTitle = "ClanNameTitle",
            EnterClanName = "EnterClanName",
            AvatarTitle = "AvatarTitle",
            UrlTitle = "UrlTitle",
            CreateTitle = "CreateTitle",
            LastLoginTitle = "LastLoginTitle",
            DemoteModerTitle = "DemoteModerTitle",
            PromoteModerTitle = "PromoteModerTitle",
            PromoteLeaderTitle = "PromoteLeaderTitle",
            KickTitle = "KickTitle",
            GatherRatesTitle = "GatherRatesTitle",
            CreateClanTitle = "CreateClanTitle",
            FriendlyFireTitle = "FriendlyFireTitle",
            AllyFriendlyFireTitle = "AllyFriendlyFireTitle",
            InvitesTitle = "InvitesTitle",
            AllyInvites = "AllyInvites",
            ClanInvitesTitle = "ClanInvitesTitle",
            IncomingAllyTitle = "IncomingAllyTitle",
            LeaderTransferTitle = "LeaderTransferTitle",
            SelectSkinTitle = "SelectSkinTitle",
            EnterSkinTitle = "EnterSkinTitle",
            NotModer = "NotModer",
            SuccsessKick = "SuccsessKick",
            WasKicked = "WasKicked",
            NotClanMember = "NotClanMember",
            NotClanLeader = "NotClanLeader",
            AlreadyClanMember = "AlreadyClanMember",
            ClanTagLimit = "ClanTagLimit",
            ClanExists = "ClanExists",
            ClanCreated = "ClanCreated",
            ClanDisbandedTitle = "ClanDisbandedTitle",
            ClanLeft = "ClanLeft",
            PlayerNotFound = "PlayerNotFound",
            ClanNotFound = "ClanNotFound",
            ClanAlreadyModer = "ClanAlreadyModer",
            PromotedToModer = "PromotedToModer",
            NotClanModer = "NotClanModer",
            DemotedModer = "DemotedModer",
            FFOn = "FFOn",
            AllyFFOn = "AllyFFOn",
            FFOff = "FFOff",
            AllyFFOff = "AllyFFOff",
            Help = "Help",
            ModerHelp = "ModerHelp",
            AdminHelp = "AdminHelp",
            HeAlreadyClanMember = "HeAlreadyClanMember",
            AlreadyInvitedInClan = "AlreadyInvitedInClan",
            SuccessInvited = "SuccessInvited",
            SuccessInvitedSelf = "SuccessInvitedSelf",
            ClanJoined = "ClanJoined",
            WasInvited = "WasInvited",
            DeclinedInvite = "DeclinedInvite",
            DeclinedInviteSelf = "DeclinedInviteSelf",
            DidntReceiveInvite = "DidntReceiveInvite",
            YourInviteDeclined = "YourInviteDeclined",
            CancelledInvite = "CancelledInvite",
            CancelledYourInvite = "CancelledYourInvite",
            CannotDamage = "CannotDamage",
            AllyCannotDamage = "AllyCannotDamage",
            SetDescription = "SetDescription",
            MaxDescriptionSize = "MaxDescriptionSize",
            NotDescription = "NotDescription",
            ContainsForbiddenWords = "ContainsForbiddenWords",
            NoPermCreateClan = "NoPermCreateClan",
            NoPermJoinClan = "NoPermJoinClan",
            NoPermKickClan = "NoPermKickClan",
            NoPermLeaveClan = "NoPermLeaveClan",
            NoPermDisbandClan = "NoPermDisbandClan",
            NoAllies = "NoAllies",
            NoInvites = "NoInvites",
            AllInviteExist = "AllInviteExist",
            AlreadyAlliance = "AlreadyAlliance",
            AllySendedInvite = "AllySendedInvite",
            YouAllySendedInvite = "YouAllySendedInvite",
            SelfAllySendedInvite = "SelfAllySendedInvite",
            NoFoundInviteAlly = "NoFoundInviteAlly",
            AllyAcceptInviteTitle = "AllyAcceptInviteTitle",
            RejectedInviteTitle = "RejectedInviteTitle",
            SelfRejectedInviteTitle = "SelfRejectedInviteTitle",
            WithdrawInviteTitle = "WithdrawInviteTitle",
            SelfWithdrawInviteTitle = "SelfWithdrawInviteTitle",
            SendAllyInvite = "SendAllyInvite",
            CancelAllyInvite = "CancelAllyInvite",
            WithdrawAllyInvite = "WithdrawAllyInvite",
            ALotOfMembers = "ALotOfMembers",
            ALotOfModers = "ALotOfModers",
            ALotOfAlliances = "ALotOfAlliances",
            NextBtn = "NextBtn",
            BackBtn = "BackBtn",
            NoAlly = "NoAlly",
            BreakAlly = "BreakAlly",
            SelfBreakAlly = "SelfBreakAlly",
            AllyRevokeTitle = "AllyRevokeTitle";

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(
                new Dictionary<string, string>
                {
                    [ClansMenuTitle] = "Clans menu",
                    [AboutClan] = "About Clan",
                    [ChangeAvatar] = "Change avatar",
                    [EnterLink] = "Enter link",
                    [LeaderTitle] = "Leader",
                    [GatherTitle] = "Gather",
                    [RatingTitle] = "Rating",
                    [MembersTitle] = "Members",
                    [DescriptionTitle] = "Description",
                    [NameTitle] = "Name",
                    [SteamIdTitle] = "SteamID",
                    [ProfileTitle] = "Profile",
                    [InvitedToClan] = "You were invited to the clan",
                    [BackPage] = "<",
                    [NextPage] = ">",
                    [TopClansTitle] = "Top Clans",
                    [TopPlayersTitle] = "Top Players",
                    [TopTitle] = "Top",
                    [ScoreTitle] = "Score",
                    [KillsTitle] = "Kills",
                    [DeathsTitle] = "Deaths",
                    [KDTitle] = "K/D",
                    [ResourcesTitle] = "Resources",
                    [LeftTitle] = "Left",
                    [EditTitle] = "Edit",
                    [InviteTitle] = "Invite",
                    [SearchTitle] = "Search...",
                    [ClanInvitation] = "Clan invitation",
                    [InviterTitle] = "Inviter",
                    [AcceptTitle] = "Accept",
                    [CancelTitle] = "Cancel",
                    [PlayerTitle] = "Player",
                    [ClanTitle] = "Clan",
                    [NotMemberOfClan] = "You are not a member of a clan :(",
                    [SelectItemTitle] = "Select item",
                    [CloseTitle] = "✕",
                    [SelectTitle] = "Select",
                    [ClanCreationTitle] = "Clan creation",
                    [ClanNameTitle] = "Clan name",
                    [EnterClanName] = "Enter clan name",
                    [AvatarTitle] = "Avatar",
                    [UrlTitle] = "http://...",
                    [CreateTitle] = "Create",
                    [LastLoginTitle] = "Last login",
                    [DemoteModerTitle] = "Demote moder",
                    [PromoteModerTitle] = "Promote moder",
                    [PromoteLeaderTitle] = "Promote leader",
                    [KickTitle] = "Kick",
                    [GatherRatesTitle] = "Gather rates",
                    [CreateClanTitle] = "Create a clan",
                    [FriendlyFireTitle] = "Friendly Fire",
                    [AllyFriendlyFireTitle] = "Ally FF",
                    [InvitesTitle] = "Invites",
                    [AllyInvites] = "Ally Invites",
                    [ClanInvitesTitle] = "Clan Invites",
                    [IncomingAllyTitle] = "Incoming Ally",
                    [LeaderTransferTitle] = "Leadership Transfer Confirmation",
                    [SelectSkinTitle] = "Select skin",
                    [EnterSkinTitle] = "Enter skin...",
                    [NotModer] = "You are not a clan moderator!",
                    [SuccsessKick] = "You have successfully kicked player '{0}' from the clan!",
                    [WasKicked] = "You have been kicked from the clan :(",
                    [NotClanMember] = "You are not a member of a clan!",
                    [NotClanLeader] = "You are not a clan leader!",
                    [AlreadyClanMember] = "You are already a member of the clan!",
                    [ClanTagLimit] = "Clan tag must contain from {0} to {1} characters!",
                    [ClanExists] = "Clan with that tag already exists!",
                    [ClanCreated] = "Clan '{0}' has been successfully created!",
                    [ClanDisbandedTitle] = "You have successfully disbanded the clan",
                    [ClanLeft] = "You have successfully left the clan!",
                    [PlayerNotFound] = "Player `{0}` not found!",
                    [ClanNotFound] = "Clan `{0}` not found!",
                    [ClanAlreadyModer] = "Player `{0}` is already a moderator!",
                    [PromotedToModer] = "You've promoted `{0}` to moderator!",
                    [NotClanModer] = "Player `{0}` is not a moderator!",
                    [DemotedModer] = "You've demoted `{0}` to Moderator!",
                    [FFOn] = "Friendly Fire turned <color=#7FFF00>on</color>!",
                    [AllyFFOn] = "Ally Friendly Fire turned <color=#7FFF00>on</color>!",
                    [FFOff] = "Friendly Fire turned <color=#FF0000>off</color>!",
                    [AllyFFOff] = "Ally Friendly Fire turned <color=#FF0000>off</color>!",
                    [Help] =
                        "Available commands:\n/clan - display clan menu\n/clan create \n/clan leave - Leave your clan\n/clan ff - Toggle friendlyfire status",
                    [ModerHelp] =
                        "\nModerator commands:\n/clan invite <name/steamid> - Invite a player\n/clan withdraw <name/steamid> - Cancel an invite\n/clan kick <name/steamid> - Kick a member\n/clan allyinvite <clanTag> - Offer the clan an alliance\n/clan allywithdraw <clanTag> - Cancel the offer of an alliance of clans\n/clan allyaccept <clanTag> - Accept the offer of an alliance with the clan\n/clan allycancel <clanTag> - Cancel the offer of an alliance with the clan\n/clan allyrevoke <clanTag> - Revoke an allyiance with the clan",
                    [AdminHelp] =
                        "\nOwner commands:\n/clan promote <name/steamid> - Promote a member\n/clan demote <name/steamid> - Demote a member\n/clan disband - Disband your clan",
                    [HeAlreadyClanMember] = "The player is already a member of the clan.",
                    [AlreadyInvitedInClan] = "The player has already been invited to your clan!",
                    [SuccessInvited] = "You have successfully invited the player '{0}' to the '{1}' clan",
                    [SuccessInvitedSelf] = "Player '{0}' invited you to the '{1}' clan",
                    [ClanJoined] = "Congratulations! You have joined the clan '{0}'.",
                    [WasInvited] = "Player '{0}' has accepted your invitation to the clan!",
                    [DeclinedInvite] = "You have declined an invitation to join the '{0}' clan",
                    [DeclinedInviteSelf] = "Player '{0}' declined the invitation to the clan!",
                    [DidntReceiveInvite] = "Player `{0}` did not receive an invitation from your clan",
                    [YourInviteDeclined] = "Your invitation to player '{0}' to the clan was declined by `{1}`",
                    [CancelledInvite] = "Clan '{0}' canceled the invitation",
                    [CancelledYourInvite] = "You canceled the invitation to the clan for the player '{0}'",
                    [CannotDamage] = "You cannot damage your clanmates! (<color=#7FFF00>/clan ff</color>)",
                    [AllyCannotDamage] =
                        "You cannot damage your ally clanmates! (<color=#7FFF00>/clan allyff</color>)",
                    [SetDescription] = "You have set a new clan description",
                    [MaxDescriptionSize] = "The maximum number of characters for describing a clan is {0}",
                    [NotDescription] = "Clan leader didn't set description",
                    [ContainsForbiddenWords] = "The title contains forbidden words!",
                    [NoPermCreateClan] = "You do not have permission to create a clan",
                    [NoPermJoinClan] = "You do not have permission to join a clan",
                    [NoPermKickClan] = "You do not have permission to kick clan members",
                    [NoPermLeaveClan] = "You do not have permission to leave this clan",
                    [NoPermDisbandClan] = "You do not have permission to disband this clan",
                    [NoAllies] = "Unfortunately\nYou have no allies :(",
                    [NoInvites] = "No invitations :(",
                    [AllInviteExist] = "Invitation has already been sent to this clan",
                    [AlreadyAlliance] = "You already have an alliance with this clan",
                    [AllySendedInvite] = "'{0}' invited the '{1}' clan to join an alliance",
                    [YouAllySendedInvite] = "You invited the '{0}' clan to join an alliance",
                    [SelfAllySendedInvite] = "Clan '{0}' invited you to join an alliance",
                    [NoFoundInviteAlly] = "'{0}' clan invitation not found",
                    [AllyAcceptInviteTitle] = "You have formed an alliance with the '{0}' clan",
                    [RejectedInviteTitle] = "Your clan has rejected an alliance offer from the '{0}' clan",
                    [SelfRejectedInviteTitle] = "'{0}' clan rejects the alliance proposal",
                    [WithdrawInviteTitle] =
                        "Your clan has withdrawn an invitation to an alliance with the '{0}' clan",
                    [SelfWithdrawInviteTitle] = "'{0}' clan withdrew invitation to alliance",
                    [SendAllyInvite] = "Send Invite",
                    [CancelAllyInvite] = "Cancel Invite",
                    [WithdrawAllyInvite] = "Withdraw Invite",
                    [ALotOfMembers] = "The clan has the maximum amount of players!",
                    [ALotOfModers] = "The clan has the maximum amount of moderators!",
                    [ALotOfAlliances] = "The clan has the maximum amount of alliances!",
                    [NextBtn] = "▼",
                    [BackBtn] = "▲",
                    [NoAlly] = "You have no alliance with the '{0}' clan",
                    [SelfBreakAlly] = "Your clan has breaking its alliance with the '{0}' clan",
                    [BreakAlly] = "Clan '{0}' broke an alliance with your clan",
                    [AllyRevokeTitle] = "Revoke Ally",
                    ["aboutclan"] = "About Clan",
                    ["memberslist"] = "Members",
                    ["clanstop"] = "Top Clans",
                    ["playerstop"] = "Top Players",
                    ["resources"] = "Gather Rates",
                    ["skins"] = "Skins",
                    ["playerslist"] = "Players List",
                    ["alianceslist"] = "Aliances"
                }, this);
        }

        private string Msg(BasePlayer player, string key, params object[] obj)
        {
            return string.Format(lang.GetMessage(key, this, player.UserIDString), obj);
        }

        private void Reply(BasePlayer player, string key, params object[] obj)
        {
            if (player == null) return;

            SendReply(player, Msg(player, key, obj));
        }

        #endregion

        #region Convert

        [ConsoleCommand("clans.convert")]
        private void CmdConsoleConvertOldClans(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;

            ConvertOldClans();
        }

        private void ConvertOldClans()
        {
            OldStoredData oldClans = null;

            try
            {
                oldClans = Interface.Oxide.DataFileSystem.ReadObject<OldStoredData>("Clans");
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            if (oldClans == null) return;

            foreach (var check in oldClans.clans)
            {
                var newClan = new ClanData
                {
                    ClanTag = check.Key,
                    LeaderID = check.Value.OwnerID,
                    LeaderName = covalence.Players.FindPlayer(check.Value.OwnerID.ToString())?.Name,
                    Avatar = _config.DefaultAvatar,
                    Members = check.Value.ClanMembers.Keys.ToList(),
                    Moderators = check.Value.ClanMembers.Where(x => x.Value.Role == MemberRole.Moderator)
                        .Select(x => x.Key)
                        .ToList(),
                    Top = TopClans.Count
                };

                if (_config.AutoTeamCreation)
                {
                    var leader = BasePlayer.FindByID(check.Value.OwnerID) ??
                                 BasePlayer.FindSleeping(check.Value.OwnerID);
                    if (leader != null) newClan.CreateTeam();
                }

                ClansList.Add(newClan);
            }

            Puts($"{oldClans.clans.Count} clans was converted!");
        }

        private class OldStoredData
        {
            public readonly Hash<string, OldClan> clans = new Hash<string, OldClan>();

            public int timeSaved;

            public Hash<ulong, List<string>> playerInvites = new Hash<ulong, List<string>>();
        }

        private class OldClan
        {
            public string Tag;

            public string Description;

            public readonly ulong OwnerID;

            public double CreationTime;

            public double LastOnlineTime;

            public readonly Hash<ulong, OldMember> ClanMembers = new Hash<ulong, OldMember>();

            public HashSet<string> Alliances = new HashSet<string>();

            public Hash<string, double> AllianceInvites = new Hash<string, double>();

            public HashSet<string> IncomingAlliances = new HashSet<string>();

            public string TagColor = string.Empty;
        }

        private class OldMember
        {
            public string DisplayName = string.Empty;

            public MemberRole Role;

            public bool MemberFFEnabled;

            public bool AllyFFEnabled;
        }

        private enum MemberRole
        {
            Owner,
            Council,
            Moderator,
            Member
        }

        #endregion
    }
}