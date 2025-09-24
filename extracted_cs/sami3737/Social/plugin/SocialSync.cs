using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Database;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Events;
using Oxide.Ext.Discord.Entities.Guilds;
using Oxide.Ext.Discord.Entities.Permissions;
using UnityEngine;
using Connection = Oxide.Core.Database.Connection;

namespace Oxide.Plugins
{
    [Info("SocialSync", "sami37", "1.1.3")]
    public class SocialSync : RustPlugin
    {
        [DiscordClient] private DiscordClient _client;

        private readonly Core.MySql.Libraries.MySql
            _mySql = Interface.GetMod().GetLibrary<Core.MySql.Libraries.MySql>();

        private static SocialSync _ss;
        private Connection _mySqlConnection;
        private Timer _time;
        private DateTime _lastUpdate;
        private bool _initialized;
        private DiscordRole _role;
        private DiscordGuild _guild;
        private string _hosteddiscordId;
        private Dictionary<ulong, DateTime> _playerDatasDateTimes = new Dictionary<ulong, DateTime>();
        private List<DiscordGuildMember> _dMembers = new List<DiscordGuildMember>();
        private Dictionary<string, string> _headers = new Dictionary<string, string>();


        private static string ApiMembersURL = "https://discordapp.com/api/v6/guilds/{0}/members?limit=1000";

        private List<Package> _packageList = new List<Package>();
        private List<PlayerData> _playerDataList = new List<PlayerData>();

        private class PlayerData
        {
            public ulong SteamId;
            public bool GaveDiscordReward;
            public bool GaveSteamReward;
            public bool GaveRole;
        }

        private class Package
        {
            public string Type;
            public List<string> Command;
        }

        private class DiscordGuildMember
        {
            public DiscordUser user { get; set; }
            public string nick { get; set; }
            public ulong[] roles { get; set; }
            public bool deaf { get; set; }
            public bool mute { get; set; }
        }

        private class DiscordUser
        {
            public ulong id { get; set; }
            public string username { get; set; }
            public string discriminator { get; set; }
            public string avatar { get; set; }
        }

        Configuration _config;

        class Configuration
        {
            [JsonProperty(PropertyName = "Refresh Time")]
            public int RefreshTime = 1;

            [JsonProperty(PropertyName = "Settings")]
            public Settings Info = new Settings();

            [JsonProperty(PropertyName = "Debug")] public bool Debug;

            public class Settings
            {
                [JsonProperty(PropertyName = "IP Address")]
                public string DbAddress = "127.0.0.1";

                [JsonProperty(PropertyName = "Port")] public int Port = 3306;

                [JsonProperty(PropertyName = "UserName")]
                public string Username = "root";

                [JsonProperty(PropertyName = "Password")]
                public string Password = "";

                [JsonProperty(PropertyName = "Database Name")]
                public string DbName = "";

                [JsonProperty(PropertyName = "Table Name")]
                public string TableName = "";

                [JsonProperty(PropertyName = "Discord Token")]
                public string DiscordToken = "";

                [JsonProperty(PropertyName = "Discord Roles")]
                public Snowflake DiscordRoles;

                [JsonProperty(PropertyName = "Role Setup", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<RoleInfo> RoleSetup = new List<RoleInfo>
                {
                    new RoleInfo
                    {
                        OxideGroup = "default",
                        DiscordRole = "Member"
                    },

                    new RoleInfo
                    {
                        OxideGroup = "vip",
                        DiscordRole = "Donator"
                    }
                };

                public class RoleInfo
                {
                    [JsonProperty(PropertyName = "Oxide Group")]
                    public string OxideGroup;

                    [JsonProperty(PropertyName = "Discord Role")]
                    public string DiscordRole;
                }

                [JsonProperty(PropertyName = "Delay between command")]
                public int DelayCommand = 1;

                [JsonProperty(PropertyName = "Column name on group gave")]
                public string gaveColumn = "gave";

            }
        }

        [HookMethod(DiscordExtHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            if (ready.Guilds.Count == 0)
            {
                PrintError("Your bot was not found in any discord servers. Please invite it to a server and reload the plugin.");
                return;
            }

            _guild = ready.Guilds.Values.FirstOrDefault();

            if (_guild == null)
            {
                PrintError("Failed to find a matching guild for the Discord Server Id. " +
                           "Please make sure your guild Id is correct and the bot is in the discord server.");
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(_config, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }

            if (Config["Settings"] != null && Config["Settings"].GetType() == typeof(Dictionary<string, object>))
            {
                if (((Dictionary<string, object>) Config["Settings"]).ContainsKey("Discord Key") &&
                    (string) ((Dictionary<string, object>) Config["Settings"])["Discord Key"] != "")
                {
                    _config.Info.DiscordToken =
                        (string) ((Dictionary<string, object>) Config["Settings"])["Discord Key"];
                    ((Dictionary<string, object>) Config["Settings"]).Remove("Discord Key");
                    Config.Save();
                }
            }

            SaveConfig();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"CantUse", "You can't use this command now, you must wait {0}."}
            }, this);
        }

        private void LoadData()
        {
            _packageList = Interface.Oxide.DataFileSystem.ReadObject<List<Package>>(Name);
            _playerDataList = Interface.Oxide.DataFileSystem.ReadObject<List<PlayerData>>(Name + "_Rewarded");

            if (_packageList == null || _packageList.Count == 0)
            {
                _packageList = new List<Package>
                {
                    new Package
                    {
                        Type = "Discord",
                        Command = new List<string>
                        {
                            "o.grant user {steamid} discord",
                            "o.usergroup add {username} discord"
                        }
                    },
                    new Package
                    {
                        Type = "Steam",
                        Command = new List<string>
                        {
                            "o.grant user {steamid} steam",
                            "o.usergroup add {username} steam"
                        }
                    }
                };
                SaveDefaultData();
            }

            if (_playerDataList == null)
                _playerDataList = new List<PlayerData>();
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name + "_Rewarded", _playerDataList);
        }

        private void SaveDefaultData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, _packageList);
        }

        private void Loaded()
        {
            _ss = this;
        }

        private void OnServerInitialized()
        {
            _time = timer.Every(_config.RefreshTime * 10, Social.UpdateSocial);
            LoadData();

            _headers.Add("Authorization", $"Bot {_config.Info.DiscordToken}");
            _headers.Add("User-Agent", "DiscordBot (https://www.rust-evolution.net, 1.0.0");
            _headers.Add("Content-Type", "application/json");

            ConnectClient();
            _lastUpdate = DateTime.Now;
            timer.Every(60, () =>
            {
                if ((DateTime.Now - _lastUpdate).TotalSeconds > 120)
                {
                    PrintWarning("Heartbeat timed out. Reconnecting");
                    CloseClient();
                    ConnectClient();
                }
            });

            if (_mySqlConnection == null)
                _mySqlConnection = _mySql.OpenDb(_config.Info.DbAddress, _config.Info.Port, _config.Info.DbName,
                    _config.Info.Username, _config.Info.Password, this);

            if (_mySqlConnection == null || _mySqlConnection.Con == null)
            {
                Puts("MySQL connection has failed. Please check your MySQL informations.");
                return;
            }

            var sqli = Sql.Builder.Append("DROP PROCEDURE IF EXISTS add_version_to_actor;" +
                                          "CREATE DEFINER = CURRENT_USER PROCEDURE add_version_to_actor() " +
                                          "BEGIN " +
                                          "DECLARE colName TEXT; " +
                                          "SELECT column_name INTO colName " +
                                          "FROM information_schema.columns " +
                                          "WHERE table_schema = '" + _config.Info.DbName + "'" +
                                          "AND table_name = '" + _config.Info.TableName + "'" +
                                          " AND column_name = '" + _config.Info.gaveColumn + "'; " +
                                          "IF colName is null THEN " +
                                          "ALTER TABLE " + _config.Info.TableName + " ADD `" + _config.Info.gaveColumn +
                                          "` TINYINT NOT NULL DEFAULT '0' AFTER `adressip`; " +
                                          "END IF; " +
                                          "END; " +
                                          "CALL add_version_to_actor; " +
                                          "DROP PROCEDURE add_version_to_actor;");
            _mySql.Update(sqli, _mySqlConnection);
            if (_mySqlConnection != null) _mySql.CloseDb(_mySqlConnection);

            _role = _guild.Roles[_config.Info.DiscordRoles];
        }

        private bool AddRoleToUser(Snowflake userId)
        {
            if (!DiscordOnline())
            {
                PrintWarning("Discord Status is offline.");
                return false;
            }

            if (_role == null)
                return false;
            PrintToConsole($"Adding role '{_role.Name}' to '{userId}'");
            _guild.AddGuildMemberRole(_client, userId, _role.Id);
            return true;
        }

        private bool AddRoleToUser(Snowflake userId, string roles)
        {
            DiscordRole CurrentRole;
            if (!DiscordOnline())
            {
                PrintWarning("Discord Status is offline.");
                return false;
            }

            CurrentRole = GetRole(roles);

            if (CurrentRole == null)
            {
                return false;
            }

            if (_config.Debug)
                Log($"Adding role '{CurrentRole.Name}' to '{userId}'");
            _guild.AddGuildMemberRole(_client, userId, CurrentRole.Id);
            return true;
        }

        private bool RemoveRoleToUser(Snowflake userId, string roles)
        {
            if (!DiscordOnline())
            {
                PrintWarning("Discord Status is offline.");
                return false;
            }

            if (_role == null)
                _role = GetRole(roles);
            if (_role == null)
                return false;
            PrintToConsole($"Adding role '{_role.Name}' to '{userId}'");
            _guild.RemoveGuildMemberRole(_client, userId, _role.Id);
            return true;
        }

        private DiscordRole GetRole(string nameOrId)
        {
            if (!DiscordOnline())
            {
                return null;
            }

            return _guild.Roles.FirstOrDefault(r => r.Value.Id == nameOrId || string.Equals(r.Value.Name, nameOrId, StringComparison.OrdinalIgnoreCase)).Value;
        }

        private bool DiscordOnline() => _initialized;

        private class Social : MonoBehaviour
        {
            public static void UpdateSocial()
            {
                _ss.PrintWarning("Updating social sync.");
                try
                {
                    if (_ss._mySqlConnection == null)
                        _ss._mySqlConnection = _ss._mySql.OpenDb(_ss._config.Info.DbAddress, _ss._config.Info.Port, _ss._config.Info.DbName,
                            _ss._config.Info.Username, _ss._config.Info.Password, _ss);

                    if (_ss._mySqlConnection == null || _ss._mySqlConnection.Con == null)
                    {
                        _ss.Puts("MySQL connection has failed. Please check your MySQL informations.");
                        return;
                    }

                    var sqli = Sql.Builder.Append("SELECT * FROM " + _ss._config.Info.TableName + " WHERE " +
                                                  _ss._config.Info.gaveColumn + " = 0");
                    _ss._mySql.Query(sqli, _ss._mySqlConnection, listed =>
                    {
                        if (listed == null || listed.Count == 0)
                        {
							if(_ss._config.Debug)
								_ss.Log("No user found");
                            _ss._mySql.CloseDb(_ss._mySqlConnection);
                            return;
                        }

                        if (_ss._config.Debug)
                            _ss.Log($"{listed.Count} users found.");
                        int skipped = 0, updated = 0, created = 0;
                        foreach (var dataTable in listed)
                        {
                            bool discord = false;
                            bool steam = false;
                            var listarray = dataTable.Values.ToArray();
                            ulong steamId;
                            ulong.TryParse(listarray[1].ToString(), out steamId);
                            Snowflake discordid;
                            Snowflake.TryParse(listarray[2].ToString(), out discordid);

                            int inSteamGroup;
                            int.TryParse(listarray[3].ToString(), out inSteamGroup);

                            BasePlayer ppl = BasePlayer.Find(steamId.ToString());
                            if (ppl == null)
                            {
                                ppl = BasePlayer.FindSleeping(steamId);
                                if (ppl != null && ppl.Connection == null)
                                {
                                    skipped++;
                                    continue;
                                }
                                skipped++;
                                continue;
                            }

                            if (_ss._playerDataList == null)
                                _ss._playerDataList = new List<PlayerData>();
                            PlayerData playerData = _ss._playerDataList.Find(x => x.SteamId == steamId);
                            if (playerData != null)
                            {
                                playerData = _ss._playerDataList.First(x => x.SteamId == steamId);

                                if (playerData.GaveDiscordReward && playerData.GaveSteamReward && playerData.GaveRole)
                                {
                                    skipped++;
                                    var sql = Sql.Builder.Append("UPDATE " + _ss._config.Info.TableName + " SET " +
                                                                 _ss._config.Info.gaveColumn + " = 1 WHERE steamid = '" +
                                                                 steamId + "'");
                                    _ss._mySql.Update(sql, _ss._mySqlConnection);
                                    continue;
                                }

                                if (playerData.GaveDiscordReward && playerData.GaveSteamReward && !playerData.GaveRole)
                                {
                                    if (_ss.AddRoleToUser(discordid))
                                        playerData.GaveRole = true;
                                    var sql = Sql.Builder.Append("UPDATE " + _ss._config.Info.TableName + " SET " +
                                                                 _ss._config.Info.gaveColumn + " = 1 WHERE steamid = '" +
                                                                 steamId + "'");
                                    _ss._mySql.Update(sql, _ss._mySqlConnection);
                                    continue;
                                }

                                if (!playerData.GaveDiscordReward && discordid != 0)
                                {
                                    var package = _ss._packageList?.Find(x => x.Type.ToLower() == "discord");
                                    if (package != null)
                                    {
                                        if (package.Command == null || package.Command.Count == 0)
                                        {
                                            _ss.PrintWarning("Command List for package discord in empty");
                                            continue;
                                        }

                                        foreach (var commands in package.Command)
                                        {
                                            if (commands.Contains("username"))
                                            {
                                                _ss.Server.Command(StringEx.Replace(commands, "{username}",
                                                    "\""+ppl.displayName+"\"", StringComparison.OrdinalIgnoreCase));
                                            }

                                            if (commands.Contains("steamid"))
                                            {
                                                _ss.Server.Command(StringEx.Replace(commands, "{steamid}",
                                                    ppl.UserIDString, StringComparison.OrdinalIgnoreCase));
                                            }
                                        }

                                        playerData.GaveDiscordReward = true;
                                        discord = true;
                                    }
                                }

                                if (!playerData.GaveSteamReward && inSteamGroup != 0)
                                {
                                    var package = _ss._packageList?.Find(x => x.Type.ToLower() == "steam");
                                    if (package != null)
                                    {
                                        if (package.Command == null || package.Command.Count == 0)
                                        {
                                            _ss.PrintWarning("Command List for package steam in empty");
                                            continue;
                                        }

                                        foreach (var commands in package.Command)
                                        {
                                            if (commands.Contains("username"))
                                            {
                                                _ss.Server.Command(StringEx.Replace(commands, "{username}",
                                                    "\"" + ppl.displayName + "\"", StringComparison.OrdinalIgnoreCase));
                                            }

                                            if (commands.Contains("steamid"))
                                            {
                                                _ss.Server.Command(StringEx.Replace(commands, "{steamid}",
                                                    ppl.UserIDString, StringComparison.OrdinalIgnoreCase));
                                            }
                                        }

                                        playerData.GaveSteamReward = true;
                                        steam = true;
                                    }
                                }

                                updated++;
                                _ss._playerDataList.RemoveAll(x => x.SteamId == steamId);
                                _ss._playerDataList.Add(playerData);
                                _ss.SaveData();
                            }
                            else
                            {
                                if (discordid != 0)
                                {
                                    var package = _ss._packageList?.Find(x => x.Type.ToLower() == "discord");
                                    if (package != null)
                                    {
                                        if (package.Command == null || package.Command.Count == 0)
                                        {
                                            _ss.PrintWarning("Command List for package discord in empty");
                                            continue;
                                        }

                                        foreach (var commands in package.Command)
                                        {
                                            if (commands.Contains("username"))
                                            {
                                                _ss.Server.Command(StringEx.Replace(commands, "{username}",
                                                    "\"" + ppl.displayName + "\"", StringComparison.OrdinalIgnoreCase));
                                            }

                                            if (commands.Contains("steamid"))
                                            {
                                                _ss.Server.Command(StringEx.Replace(commands, "{steamid}",
                                                    ppl.UserIDString, StringComparison.OrdinalIgnoreCase));
                                            }
                                        }

                                        discord = true;
                                    }
                                }

                                if (inSteamGroup != 0)
                                {
                                    var package = _ss._packageList?.Find(x => x.Type.ToLower() == "steam");
                                    if (package != null)
                                    {
                                        if (package.Command == null || package.Command.Count == 0)
                                        {
                                            _ss.PrintWarning("Command List for package steam in empty");
                                            continue;
                                        }

                                        foreach (var commands in package.Command)
                                        {
                                            if (commands.Contains("username"))
                                            {
                                                _ss.Server.Command(StringEx.Replace(commands, "{username}",
                                                    "\"" + ppl.displayName + "\"", StringComparison.OrdinalIgnoreCase));
                                            }

                                            if (commands.Contains("steamid"))
                                            {
                                                _ss.Server.Command(StringEx.Replace(commands, "{steamid}",
                                                    ppl.UserIDString, StringComparison.OrdinalIgnoreCase));
                                            }
                                        }

                                        steam = true;
                                    }
                                }

                                playerData = new PlayerData()
                                {
                                    SteamId = steamId,
                                    GaveSteamReward = steam,
                                    GaveDiscordReward = discord

                                };
                                _ss._playerDataList.Add(playerData);
                                created++;
                            }

                            if (discord && steam && discordid != 0)
                            {
                                if (!playerData.GaveRole)
                                    _ss.AddRoleToUser(discordid);
                            }
                        }

                        _ss.PrintWarning($"{created} new user(s), {skipped} skipped user(s), {updated} updated user(s)");
                    });
                    if (_ss._mySqlConnection != null) _ss._mySql.CloseDb(_ss._mySqlConnection);
                }
                catch (Exception e)
                {
                    _ss.Puts("Data table has not been created.");
                    if (_ss._mySqlConnection != null) _ss._mySql.CloseDb(_ss._mySqlConnection);
                }

                if (_ss._mySqlConnection != null) _ss._mySql.CloseDb(_ss._mySqlConnection);

            }
        }

        private void Unload()
        {
            timer.Destroy(ref _time);
            SaveData();
            CloseClient();
        }

        protected override void LoadDefaultConfig() => _config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(_config);

        private void ConnectClient()
        {
            if (string.IsNullOrEmpty(_config.Info.DiscordToken))
            {
                PrintWarning("Please enter your discord bot API key and reload the plugin");
                return;
            }


            timer.In(1f, () =>
            {
                DiscordSettings discordSettings = new DiscordSettings
                {
                    ApiToken = _config.Info.DiscordToken,
                    Intents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.DirectMessages |
                              GatewayIntents.GuildMessages
                };

                _client.Connect(discordSettings); // Create a new DiscordClient

                _initialized = true;

                timer.In(5f, () =>
                {
                    if (_client != null)
                        _hosteddiscordId = _client.Bot.Servers.FirstOrDefault().Value.Id;
                    _role = GetRole(_config.Info.DiscordRoles);

                    if (_role == null)
                        PrintWarning($"Tried to add a role to player that doesn't exist: '{_config.Info.DiscordRoles}'");
                    _dMembers = GetMembers();
                    _lastUpdate = DateTime.Now;
                });
            });
        }

        private void CloseClient()
        {
            _client.Disconnect();
            _initialized = false;
        }

        #region Discord Extension hook

        private bool HasGroup(string id, string groupName)
        {
            return permission.UserHasGroup(id, groupName);
        }

        private DiscordGuildMember GetGuildMember(string discordId)
        {
            return _dMembers.Find(member => member.user.id == ulong.Parse(discordId));
        }

        private void Log(string msg)
        {
            LogToFile("log", $"[{DateTime.Now}] {msg}", this);
        }

        private List<DiscordGuildMember> GetMembers()
        {
            webrequest.EnqueueGet(string.Format(ApiMembersURL, _hosteddiscordId), (code, response) =>
                {
                    if (code != 200)
                    {
                        Log($"GetMembers responded with {code}: {response}");
                        return;
                    }

                    _dMembers = JsonConvert.DeserializeObject<List<DiscordGuildMember>>(response);
                    if (_config.Debug)
                        Log("Discord Members" + response);
                }
                , this, _headers);
            return _dMembers;
        }

        private string[] GetGroups(string id)
        {
            return permission.GetUserGroups(id);
        }

        private bool UserHasRole(string discordId, string roleId)
        {
            var player = GetGuildMember(discordId);

            return player != null && player.roles.FirstOrDefault(x => x == ulong.Parse(roleId)) != 0;
        }

        private DiscordRole GetRoleByName(string roleName)
            => _guild.Roles.FirstOrDefault(searchRole => searchRole.Value.Name == roleName).Value;

        private void HandleRole(string id = "", string roleName = "", string oxideGroup = "", Snowflake discordId = default(Snowflake))
        {
            if (discordId == "0")
            {
                if (_config.Debug)
                    Log(
                        $"Social Sync discordId is null. SteamID : {id}, Role : {roleName}, Oxide Group : {oxideGroup}");
                return;
            }

            var roleByName = GetRoleByName(roleName);
            if (roleByName == null)
            {
                if (_config.Debug)
                    Puts(
                        $"Unable to find '{roleName}' discord role! ID : {discordId}, SteamID : {id}, Oxide Group : {oxideGroup}");
                return;
            }

            if (HasGroup(id, oxideGroup) && !UserHasRole(discordId, roleByName.Id))
            {
                if (_config.Debug)
                    Log(
                        $"Role {roleByName.Name} added to ID : {discordId}, SteamID : {id}, Oxide Group : {oxideGroup}");
                AddRoleToUser(discordId, roleByName.Id);
            }
            else if (!HasGroup(id, oxideGroup) && UserHasRole(discordId, roleByName.Id))
            {
                if (_config.Debug)
                    Log(
                        $"Role {roleByName.Name} removed from ID : {discordId}, SteamID : {id}, Oxide Group : {oxideGroup}");
                RemoveRoleToUser(discordId, roleByName.Id);
            }
        }

        private void HandleRole(IPlayer player, Snowflake discordId)
        {
            _config.Info.RoleSetup.ForEach(roleSetup =>
            {
                if (player != null)
                    GetGroups(player.Id).ToList().ForEach(playerGroup =>
                    {
                        if (roleSetup.OxideGroup == playerGroup)
                        {
                            if (discordId != "0")
                                HandleRole(player.Id, roleSetup.DiscordRole, playerGroup, discordId);
                        }
                    });
            });
        }

        private void DiscordSocket_WebSocketClosed(string reason, int code, bool clean)
        {
            PrintWarning("WebSocketClose Detected. Restarting Connection.");
            CloseClient();
            ConnectClient();
        }

        private void DiscordSocket_WebSocketErrored(Exception exception, string message)
        {
            PrintError($"WebSocketError: {exception}\n{message}");
            CloseClient();
            ConnectClient();
        }

        private void Discord_UnhandledEvent(JObject messageObject)
        {
            PrintError($"Unhandled Event: {messageObject}");
            CloseClient();
            ConnectClient();
        }

        private void DiscordSocket_HeartbeatSent()
        {
            _lastUpdate = DateTime.Now;
        }

        #endregion

        #region

        [ChatCommand("sync")]
        void CmdSyncRoles(BasePlayer player, string cmd, string[] args)
        {
            if (_playerDatasDateTimes == null)
                _playerDatasDateTimes = new Dictionary<ulong, DateTime>();
            if (_playerDatasDateTimes.ContainsKey(player.userID))
            {
                DateTime start = _playerDatasDateTimes[player.userID];
                DateTime end = start.AddMinutes(_config.Info.DelayCommand);
                DateTime today = DateTime.Now;
                today = new DateTime(today.Year, today.Month, today.Day, today.Hour, today.Minute, today.Second);

                int value = DateTime.Compare(end, today);
                if (value >= 0)
                {
                    SendReply(player, string.Format(lang.GetMessage("CantUse", this, player.userID.ToString()), value));
                    return;
                }
            }
            try
            {
                if (_mySqlConnection == null)
                    _mySqlConnection = _mySql.OpenDb(_config.Info.DbAddress, _config.Info.Port, _config.Info.DbName,
                        _config.Info.Username, _config.Info.Password, this);

                if (_mySqlConnection == null || _mySqlConnection.Con == null)
                {
                    Puts("MySQL connection has failed. Please check your MySQL informations.");
                    return;
                }

                var sqli = Sql.Builder.Append("SELECT * FROM " + _config.Info.TableName + " WHERE steamid = '" + player.userID + "'");
                _mySql.Query(sqli, _mySqlConnection, listed =>
                {
                    if (listed == null || listed.Count == 0)
                    {
                        _mySql.CloseDb(_mySqlConnection);
                        return;
                    }

                    foreach (var dataTable in listed)
                    {
                        var listarray = dataTable.Values.ToArray();
                        ulong steamId;
                        ulong.TryParse(listarray[1].ToString(), out steamId);
                        Snowflake discordid;
                        Snowflake.TryParse(listarray[2].ToString(), out discordid);

                        int inSteamGroup;
                        int.TryParse(listarray[3].ToString(), out inSteamGroup);
                        if (discordid != 0 && steamId != 0)
                        {
                            HandleRole(covalence.Players.FindPlayerById(player.userID.ToString()), discordid);
                        }
                    }
                    DateTime today = DateTime.Now;
                    if (!_playerDatasDateTimes.ContainsKey(player.userID))
                        _playerDatasDateTimes.Add(player.userID, new DateTime(today.Year, today.Month, today.Day, today.Hour, today.Minute, today.Second));
                });
                if (_mySqlConnection != null) _mySql.CloseDb(_mySqlConnection);
            }
            catch (Exception e)
            {
                Puts("Data table has not been created.");
                if (_mySqlConnection != null) _mySql.CloseDb(_mySqlConnection);
            }

            if (_mySqlConnection != null) _mySql.CloseDb(_mySqlConnection);

        }

        #endregion
    }
}
