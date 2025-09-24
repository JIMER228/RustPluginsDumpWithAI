using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Database;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Connection = Oxide.Core.Database.Connection;

namespace Oxide.Plugins
{
    [Info("DiscordBoost", "sami37", "1.0.6")]
    [Description("Allows players to connect to their discord boost account")]

    public class DiscordBoost : RustPlugin
    {
        #region Collection
        private Dictionary<ulong, string> Codes = new Dictionary<ulong, string>();
        private readonly Core.MySql.Libraries.MySql _mySql = Interface.GetMod().GetLibrary<Core.MySql.Libraries.MySql>();
        private Connection _mySqlConnection;
        private Timer time;
        #endregion

        #region Config
        Configuration config;

        class Configuration
        {
            [JsonProperty(PropertyName = "Permission")]
            public List<string> Permissions = new List<string>();

            [JsonProperty(PropertyName = "Refresh Time")]
            public int RefreshTime;

            [JsonProperty(PropertyName = "Settings")]
            public Settings Info = new Settings();

            [JsonProperty(PropertyName = "Authentication Code")]
            public AuthCode Code = new AuthCode();

            public class Settings
            {
                [JsonProperty(PropertyName = "Bot Token")]
                public string BotToken = string.Empty;

                [JsonProperty(PropertyName = "Discord server ID")]
                public string ServerID = string.Empty;

                [JsonProperty(PropertyName = "Oxide Group")]
                public string Group = "authenticated";

                [JsonProperty(PropertyName = "Chat Prefix")]
                public string ChatPrefix = "<color=#1874CD>(Auth)</color>";

                [JsonProperty(PropertyName = "Chat Icon (SteamID64)")]
                public ulong ChatIcon = 0;

                [JsonProperty(PropertyName = "IP Address")]
                public string DbAddress = "127.0.0.1";

                [JsonProperty(PropertyName = "Port")]
                public int Port = 0;

                [JsonProperty(PropertyName = "UserName")]
                public string Username = "root";

                [JsonProperty(PropertyName = "Password")]
                public string Password = "";

                [JsonProperty(PropertyName = "Database Name")]
                public string DbName = "";
            }

            public class AuthCode
            {
                [JsonProperty(PropertyName = "Code Lifetime (minutes)")]
                public int CodeLifetime = 60;

                [JsonProperty(PropertyName = "Code Length")]
                public int CodeLength = 5;

                [JsonProperty(PropertyName = "Lowercase")]
                public bool Lowercase = false;
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(config, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Code Generation"] = "Here is your code: <color=#1874CD>{0}</color>\n\n<color=#EE3B3B>What's next:</color>\n<color=#1874CD>1</color> - Join the Discord at \n<color=#1874CD>2</color> - PM your code to the bot called 'XXX'\n\nHere is the discord invite link - <color=#1874CD></color>",
                ["Code Expired"] = "Your code has <color=#EE3B3B>Expired!</color>",
                ["Authenticated"] = "Thank you for authenticating your account!",
                ["NotAnymoreOnDiscord"] = "You leaved the discord server, you don't have the perm anymore.",
                ["NotRegistered"] = "You may register your discord account first !",
                ["BotKeyNotSet"] = "The bot token has not been set, please ask an admin.",
                ["PermissionSet"] = "You have been granted with perm <color=#EE3B3B>{0}</color>.",
                ["NotPremium"] = "It don't seem you boosted yet the server.",
                ["PermissionNotSet"] = "The permissions list is empty, please ask an admin.",
                ["Already Authenticated"] = "You have already <color=#1874CD>authenticated</color> your account, no need to do it again!",
                ["Unable to find code"] = "Sorry, we couldn't find your code, please try to authenticate again, If you haven't generated a code, please type /auth in-game"
            }, this);
        }
        #endregion

        #region ChatCommands
        [ChatCommand("auth")]
        private void AuthCommand(BasePlayer player, string command, string[] args)
        {
            if(_mySqlConnection == null)
    			_mySqlConnection = _mySql.OpenDb(config.Info.DbAddress, config.Info.Port, config.Info.DbName, config.Info.Username, config.Info.Password, this);

            if (_mySqlConnection == null || _mySqlConnection.Con == null)
			{
				Puts("MySQL connection has failed. Please check your MySQL informations.");
				return;
			}
            var sqli = Sql.Builder.Append("SELECT discordid FROM `stats_player_discord` WHERE confirmed = 1 AND steamid = '" + player.UserIDString + "'");
            _mySql.Query(sqli, _mySqlConnection, listed =>
            {
                if (listed != null && listed.Count != 0)
                {
                    Message(player, "Already Authenticated");
                    _mySql.CloseDb(_mySqlConnection);
                    return;
                }

                if (Codes.ContainsKey(player.userID))
                {
                    Message(player, "Code Generation", Codes[player.userID]);
                    return;
                }

                var code = GenerateCode(config.Code.CodeLength, config.Code.Lowercase);

                Message(player, "Code Generation", code);
                Codes.Add(player.userID, code);

                timer.In(config.Code.CodeLifetime * 60, () =>
                {
                    if (Codes.ContainsKey(player.userID))
                    {
                        Codes.Remove(player.userID);
                        if (player.IsConnected)
                        {
                            Message(player, "Code Expired");
                        }
                    }
                });

                _mySql.Insert(
                Sql.Builder.Append(
                    "INSERT INTO stats_player_discord ( `steamid`, `discordid`, `code`) VALUES ( @0, @1, @2) ON DUPLICATE KEY UPDATE steamid = @0, discordid = @1, code = @2",
                    player.userID, code, code),
                _mySqlConnection);

                if(_mySqlConnection != null)
                    _mySql.CloseDb(_mySqlConnection);
            });
        }

        [ChatCommand("boosted")]
        private void BoostCommand(BasePlayer player, string cmd, string[] args)
        {
            if (config.Permissions == null || config.Permissions.Count == 0)
            {
                Message(player, "PermissionNotSet");
                return;
            }

            try
            {
                if (_mySqlConnection == null)
                    _mySqlConnection = _mySql.OpenDb(config.Info.DbAddress, config.Info.Port, config.Info.DbName, config.Info.Username, config.Info.Password, this);

                if (_mySqlConnection == null || _mySqlConnection.Con == null)
                {
                    Puts("MySQL connection has failed. Please check your MySQL informations.");
                    return;
                }

                var sqli = Sql.Builder.Append("SELECT discordid FROM `stats_player_discord` WHERE steamid = {player.userID} AND confirmed = 1");
                _mySql.Query(sqli, _mySqlConnection, listed =>
                {
                    if (listed == null || listed.Count == 0)
                    {
                        Message(player, "NotRegistered");
                        _mySql.CloseDb(_mySqlConnection);
                        return;
                    }

                    var headers = new Dictionary<string, string>
                    {
                        {"Authorization", $"Bot {config.Info.BotToken}"},
                        {"Content-length", "0"},
                        {"User-Agent", "DiscordBoost sami37 1.0.0"},
                        {"Content-Type", "application/json"}
                    };
                    foreach (var dataTable in listed)
                    {
                        var listarray = dataTable.Values.ToArray();
                        var currentvalue = listarray[0];
                        webrequest.Enqueue(
                            $"https://discordapp.com/api/guilds/{config.Info.ServerID}/members/{currentvalue}",
                            "",
                            (code, response) =>
                            {
                                var res = JsonConvert.DeserializeObject<RootObject>(response);
                                if (res.user != null)
                                {
                                    if (!response.Contains("\"premium_since\": null"))
                                    {
                                        if (config.Permissions == null || config.Permissions.Count == 0)
                                        {
                                            Message(player, "PermissionNotSet");
                                            return;
                                        }

                                        foreach (var perm in config.Permissions)
                                        {
                                            permission.GrantUserPermission(player.UserIDString, perm, null);
                                            Message(player, "PermissionSet", perm);
                                        }
                                    }
                                    else
                                    {
                                        foreach (var perm in config.Permissions)
                                        {
                                            permission.RevokeUserPermission(player.UserIDString, perm);
                                        }

                                        Message(player, "NotPremium");
                                        if (_mySqlConnection != null) _mySql.CloseDb(_mySqlConnection);
                                    }
                                }
                            },
                            this,
                            RequestMethod.GET,
                            headers);
                    }
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

        #region Discord Hooks
        /*        private void Discord_MessageCreate(Message message)
                {
                    if (message.author.bot == true)
                        return;

                    Channel.GetChannel(Client, message.channel_id, dm =>
                    {
                        if (dm.type != ChannelType.DM)
                            return;

                        if (!Codes.ContainsValue(message.content))
                        {
                            dm.CreateMessage(Client, StripRichText(Lang("Unable to find code")));
                            return;
                        }

                        if (data.Players.ContainsValue(message.author.id))
                        {
                            dm.CreateMessage(Client, StripRichText(Lang("Already Authenticated")));
                            return;
                        }

                        dm.CreateMessage(Client, StripRichText(Lang("Authenticated")));

                        foreach (var steamid in Codes.Keys)
                        {
                            if (Codes[steamid] == message.content)
                            {
                                permission.AddUserGroup(steamid.ToString(), config.Info.Group);
                                data.Players.Add(steamid, message.author.id);
                            }
                        }

                    });
                }

                private void Discord_MemberRemoved(GuildMember member)
                {
                    var steamid = GetSteamOf(member.user.id);

                    if (steamid == default(ulong))
                        return;

                    Interface.Oxide.CallHook("OnUserLeft", steamid, member.user.id);

                    RemovePlayer(steamid);

                }

                private void Discord_MemberAdded(GuildMember member)
                {
                    var steamid = GetSteamOf(member.user.id);

                    if (steamid == default(ulong))
                        return;

                    permission.AddUserGroup(steamid.ToString(), config.Info.Group);
                }*/
        #endregion

        #region Oxide Hooks
        private void OnServerInitialized()
        {
            if (!permission.GroupExists(config.Info.Group) && !string.IsNullOrEmpty(config.Info.Group))
            {
                permission.CreateGroup(config.Info.Group, config.Info.Group, 0);
            }

            _mySqlConnection = _mySql.OpenDb(config.Info.DbAddress, config.Info.Port, config.Info.DbName, config.Info.Username, config.Info.Password, this);

            if (_mySqlConnection == null)
            {
                Puts("MySQL connection has failed. Please check your MySQL informations.");
                return;
            }

            if (config.RefreshTime <= 0)
            {
                if (_mySqlConnection != null) _mySql.CloseDb(_mySqlConnection);
                return;
            }
            time = timer.Every(config.RefreshTime, () =>
            {
                _mySql.Query(
                    Sql.Builder.Append("select s.* FROM stats_player_discord s join leavedclient on leavedclient.discordid = s.discordid", null), _mySqlConnection, list =>
                    {
                        if (list == null || list.Count == 0) return;
                        foreach (var datas in list)
                        {
                            RemovePlayer(ulong.Parse(datas["steamid"].ToString()));
                        }
                    });
                _mySql.Query(
                    Sql.Builder.Append("SELECT * FROM stats_player_discord", null), _mySqlConnection, list =>
                    {
                        if (list == null || list.Count == 0) return;
                        foreach (var datas in list)
                        {
                            permission.AddUserGroup(datas["steamid"].ToString(), config.Info.Group);
                        }
                    });

                if (_mySqlConnection != null)
                    _mySql.CloseDb(_mySqlConnection);
            });
        }

        private void Unload()
        {
            if (_mySqlConnection != null)
                _mySql.CloseDb(_mySqlConnection);
            timer.Destroy(ref time);
        }

        void RemovePlayer(ulong player)
        {
            var ppl = BasePlayer.FindByID(player);
            if (ppl != null)
                SendReply(ppl, lang.GetMessage("NotAnymoreOnDiscord", this, player.ToString()));
            foreach (var perm in config.Permissions)
                if (permission.UserHasPermission(player.ToString(), perm))
                    permission.RevokeUserPermission(player.ToString(), perm);
            permission.RemoveUserGroup(player.ToString(), config.Info.Group);
        }
        #endregion

        #region Helpers
        private string GenerateCode(int size, bool lowerCase)
        {
            StringBuilder builder = new StringBuilder();
            System.Random random = new System.Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            if (lowerCase)
                return builder.ToString().ToLower();
            return builder.ToString();
        }

        private string Lang(string key, string id = null, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this, id), args);
        }

        private void Message(BasePlayer player, string key, params object[] args)
        {
            Player.Message(player, Lang(key, player.UserIDString, args), config.Info.ChatPrefix, config.Info.ChatIcon);
        }

        private string StripRichText(string text)
        {
            var stringReplacements = new string[]
            {
                "<b>", "</b>",
                "<i>", "</i>",
                "</size>",
                "</color>"
            };

            var regexReplacements = new Regex[]
            {
                new Regex(@"<color=.+?>"),
                new Regex(@"<size=.+?>"),
            };

            foreach (var replacement in stringReplacements)
                text = text.Replace(replacement, string.Empty);

            foreach (var replacement in regexReplacements)
                text = replacement.Replace(text, string.Empty);

            return Formatter.ToPlaintext(text);
        }

        public class User
        {
            public string username { get; set; }
            public string discriminator { get; set; }
            public string id { get; set; }
            public string premium_type { get; set; }
            public DateTime premium_since { get; set; }
        }

        public class RootObject
        {
            public object nick { get; set; }
            public User user { get; set; }
            public List<string> roles { get; set; }
            public object premium_since { get; set; }
            public bool deaf { get; set; }
            public bool mute { get; set; }
            public DateTime joined_at { get; set; }
        }
        #endregion
    }
}
