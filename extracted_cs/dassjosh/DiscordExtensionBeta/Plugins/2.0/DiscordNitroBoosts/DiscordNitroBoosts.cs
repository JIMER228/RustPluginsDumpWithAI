using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord.Builders;
using Oxide.Ext.Discord.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Ext.Discord.Clients;
using Oxide.Ext.Discord.Connections;
using Oxide.Ext.Discord.Interfaces;
using Oxide.Ext.Discord.Libraries;

namespace Oxide.Plugins
{
    [Info("Discord Nitro Boosts", "Farkas", "1.1.0")]
    [Description("Adds discord boosters into an oxide group and removes them if their boost expires.")]
    public class DiscordNitroBoosts : CovalencePlugin, IDiscordPlugin
    {
        #region Global variables & Plugin References
        [PluginReference] private Plugin DiscordAuth, DiscordCore;
        public DiscordClient Client { get; set; }
        private readonly DiscordLink _link = GetLibrary<DiscordLink>();
        private readonly DiscordCommand _dcCommands = Interface.Oxide.GetLibrary<DiscordCommand>();
        private DiscordGuild _guild;
        private DiscordRole _boosterRole;
        #endregion

        #region Configuration
        private ConfigData _configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Bot token")]
            public string Token = "";
            [JsonProperty(PropertyName = "Discord Guild ID (optional if the bot is in one guild)")]
            public Snowflake Guild { get; set; }
            [JsonProperty(PropertyName = "Set custom status and activity for the discord bot")]
            public bool EnableCustomStatus = true;
            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Bot's activity type: (Game, Listening, Watching, Competing)")]
            public ActivityType ActivityType = ActivityType.Watching;
            [JsonProperty(PropertyName = "Bot's Status")]
            public string Status = "Nitro Boosters";
            [JsonProperty(PropertyName = "Send direct message to boosters on boost start and end")]
            public bool DirectMessageBoosters = false;
            [JsonProperty(PropertyName = "Embed's color")]
            public string Color = "#F47FFF";

            [JsonProperty(PropertyName = "Use with existing linking system (DiscordAuth or DiscordCore)")]
            public bool LinkingSystem = true;
            [JsonProperty(PropertyName = "Check users if they are boosting after every wipe.")]
            public bool WipeCheck = false;
            [JsonProperty(PropertyName = "Let users use !verify in DM")]
            public bool DMCommand = false;
            [JsonProperty(PropertyName = "Let users use !verify in guild channel")]
            public bool ChannelCommand = true;
            [JsonProperty(PropertyName = "ChannelID where players can use !verify (if the previous option is true, leave empty for any channel)")]
            public string ChannelID = "";

            [JsonProperty(PropertyName = "Oxide group's name")]
            public string OxideGroup = "nitro";
        }
        protected override void LoadConfig()
        {
            try
            {
                base.LoadConfig();
                _configData = Config.ReadObject<ConfigData>();
                SaveConfig(_configData);
            }
            catch (Exception)
            {
                PrintError(Lang("ConfigIssue"));
                return;
            }
        }
        void Init()
        {
            if (string.IsNullOrEmpty(_configData.Token))
            {
                PrintError(Lang("NoToken"));
                return;
            }

            if (string.IsNullOrEmpty(_configData.OxideGroup))
            {
                PrintError(Lang("NoOxideGroup"));
                return;
            }

            if (!_configData.LinkingSystem)
            {
                _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("DiscordNitroBoosts");
            }

            if (!permission.GroupExists(_configData.OxideGroup))
            {
                permission.CreateGroup(_configData.OxideGroup, _configData.OxideGroup, 0);
                Puts(Lang("GroupCreated"), _configData.OxideGroup);
            }
        }

        protected override void LoadDefaultConfig()
        {
            Puts(Lang("NewConfig"));
            _configData = new ConfigData();
            SaveConfig(_configData);
        }

        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }

        #endregion

        #region Data Management
        StoredData _storedData;
        class StoredData
        {
            public Dictionary<string, string> UserData = new Dictionary<string, string>();
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("DiscordNitroBoosts", _storedData);
        }

        #endregion

        #region Hooks
        private void OnDiscordClientCreated()
        {
            if (_configData.LinkingSystem)
            {
                if (DiscordAuth == null && DiscordCore == null)
                {
                    PrintError(Lang("InstallLinkerPlugin"));
                    return;
                }
            }
            else
            {
                Unsubscribe(nameof(OnDiscordPlayerLinked));
                Unsubscribe(nameof(OnDiscordPlayerUnlinked));
            }
            Connect();
        }
        private void OnDiscordGuildMemberUpdated(GuildMember member, GuildMember oldMember, DiscordGuild guild)
        {
            if (_boosterRole == null)
            {
                return;
            }
            if (member.HasRole(_boosterRole) && !oldMember.HasRole(_boosterRole)) //started boosting
            {
                if (_configData.LinkingSystem)
                {
                    PlayerId steamID = _link.GetPlayerId(member.Id);
                    if (steamID.IsValid)
                    {
                        AddToNitroGroup(member.Id.ToString(), steamID.Id);
                    }
                }

                if (_configData.LinkingSystem)
                {
                    CreateDM(member, Lang("AccountsLinkedTitle"), Lang("AccountsLinkedContent"), _configData.Color);
                }
                else
                {
                    CreateDM(member, Lang("JustBoostedTitle"), Lang("JustBoostedContent"), _configData.Color);
                }
            }
            else if (!member.HasRole(_boosterRole) && oldMember.HasRole(_boosterRole)) //stopped boosting
            {
                RemoveFromNitroGroup(member.Id.ToString());
                CreateDM(member, Lang("NoLongerBoostingTitle"), Lang("NoLongerBoostingContent"), _configData.Color);
            }
        }
        private void OnDiscordGuildMemberRemoved(GuildMemberRemovedEvent member, DiscordGuild guild)
        {
            RemoveFromNitroGroup(member.User.Id.ToString());
        }
        void OnDiscordPlayerLinked(IPlayer player, DiscordUser user)
        {
            if (_boosterRole == null)
            {
                return;
            }
            _guild.GetMember(Client, user.Id).Then((GuildMember member) =>
            {
                if (member.HasRole(_boosterRole))
                {
                    permission.AddUserGroup(player.Id, _configData.OxideGroup);
                    CreateDM(member, Lang("AccountsLinkedTitle"), Lang("AccountsLinkedContent"), _configData.Color);
                }
            });
        }
        void OnDiscordPlayerUnlinked(IPlayer player, DiscordUser user)
        {
            if (_boosterRole == null)
            {
                return;
            }
            _guild.GetMember(Client, user.Id).Then((GuildMember member) =>
            {
                if (member.HasRole(_boosterRole) && player.BelongsToGroup(_configData.OxideGroup))
                {
                    permission.RemoveUserGroup(player.Id, _configData.OxideGroup);
                    CreateDM(member, Lang("AccountsUnLinkedTitle"), Lang("AccountsUnLinkedContent"), _configData.Color);
                }
            });
        }
        void OnDiscordGuildRoleCreated(DiscordRole role, DiscordGuild guild)
        {
            if(_boosterRole == null)
            {
                if (role.IsBoosterRole())
                {
                    _boosterRole = role;
                }
            }
        }
        void OnServerSave()
        {
            if (!_configData.LinkingSystem)
            {
                SaveData();
            }
        }
        void Unload()
        {
            if (!_configData.LinkingSystem)
            {
                SaveData();
            }
        }
        void OnNewSave(string filename) //check for users that aren't boosting anymore at each wipe. it is necessary, in case someone stops booosting when the server is offline
        {
            if (_configData.WipeCheck && _boosterRole != null)
            {
                string[] boosters = permission.GetUsersInGroup(_configData.OxideGroup);
                foreach (string booster in boosters)
                {
                    string steamID = booster.Split(' ')[0];
                    string discordID = null;
                    if (_configData.LinkingSystem)
                    {
                        discordID = (Snowflake)_link.GetDiscordId(steamID);
                    }
                    else
                    {
                        foreach (string dcid in _storedData.UserData.Keys)
                        {
                            if (_storedData.UserData[dcid] == steamID)
                            {
                                discordID = dcid;
                                break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(discordID))
                    {
                        permission.RemoveUserGroup(steamID, _configData.OxideGroup);
                    }
                    else
                    {
                        _guild.GetMember(Client, (Snowflake)discordID).Then((GuildMember member) =>
                        {
                            if (!member.HasRole(_boosterRole))
                            {
                                permission.RemoveUserGroup(steamID, _configData.OxideGroup);
                            }
                        });
                    }
                }
            }
        }
        #endregion

        #region Discord Bot Connection
        public void Connect() //starting bot
        {
            BotConnection discordSettings = new BotConnection();
            discordSettings.ApiToken = _configData.Token;
            discordSettings.Intents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.DirectMessages | GatewayIntents.GuildMessages;
            //discordSettings.LogLevel = DiscordLogLevel.Verbose; //remove the first comment for debugging
            Client.Connect(discordSettings);
        }
        private void OnDiscordGatewayReady(GatewayReadyEvent ready) //checking if the bot is invited
        {
            if (ready.Guilds.Count == 0)
            {
                PrintError(Lang("InviteBot"));
                Client.Disconnect();
                return;
            }

            DiscordGuild guild = null;

            if (ready.Guilds.Count == 1 && !ready.Guilds.Values.Contains(ready.Guilds[_configData.Guild]))
            {
                guild = ready.Guilds.Values.FirstOrDefault();
            }

            if (guild == null)
            {
                guild = ready.Guilds[_configData.Guild];
            }

            if (guild == null)
            {
                PrintError(Lang("SetGuildID"));
                Client.Disconnect();
                return;
            }

            _guild = guild;

            if (_configData.EnableCustomStatus)
            {
                Client.UpdateStatus(new UpdatePresenceCommand
                {
                    Activities = new List<DiscordActivity> { new DiscordActivity { Name = _configData.Status, Type = _configData.ActivityType } }
                });
            }

            if (_configData.DMCommand && !_configData.LinkingSystem)
            {
                _dcCommands.AddDirectMessageCommand("verify", this, nameof(VerifyCommand));
            }
            if (_configData.ChannelCommand && !_configData.LinkingSystem)
            {
                if (string.IsNullOrEmpty(_configData.ChannelID))
                {
                    _dcCommands.AddGuildCommand("verify", this, null, nameof(VerifyCommand));
                }
                else
                {
                    _dcCommands.AddGuildCommand("verify", this, new List<Snowflake> { (Snowflake)_configData.ChannelID }, nameof(VerifyCommand));
                }
            }

            Puts(Lang("Connected"));
        }
        void OnDiscordGuildMembersLoaded(DiscordGuild guild) //getting booster role
        {
            if(guild == _guild)
            {
                _boosterRole = _guild.GetBoosterRole();
                //custom role (for testing)
                /*PrintError("Custom role is on!!! Don't forget to remove it.");
                foreach (DiscordRole role in guild.Roles.Values)
                {
                    if (role.Id.ToString().Equals("964562165354860545", StringComparison.OrdinalIgnoreCase))
                    {
                        _boosterRole = role;
                        break;
                    }
                }*/
            }
        }
        #endregion

        #region Commands
        void VerifyCommand(DiscordMessage message, string cmd, string[] args)
        {
            if(message.GuildId != _guild.Id)
            {
                return;
            }
            string steamID = args[0];

            if (_boosterRole == null)
            {
                return;
            }
            GuildMember user = _guild.Members[message.Author.Id];

            if (user == null)
            {
                message.Reply(Client, CreateEmbed(Lang("NotInGuildTitle"), Lang("NotInGuildContent"), _configData.Color));
                return;
            }

            if (!steamID.IsSteamId())
            {
                message.Reply(Client, CreateEmbed(Lang("UnableToVerifyTitle"), Lang("UnableToVerifyContent"), _configData.Color));
                return;
            }

            _guild.GetMember(Client, user.Id).Then((GuildMember member) =>
            {
                if (member.HasRole(_boosterRole)) //boosting
                {
                    AddToNitroGroup(message.Author.Id.ToString(), steamID);
                    message.Reply(Client, CreateEmbed(Lang("SuccesfullyVerifiedTitle"), Lang("SuccesfullyVerifiedContent"), _configData.Color));
                }
                else //not boosting
                {
                    message.Reply(Client, CreateEmbed(Lang("NotBoostingTitle"), Lang("NotBoostingContent"), _configData.Color));
                }
            });
        }
        #endregion

        #region Helper Methods
        public void AddToNitroGroup(string discordID, string steamID)
        {
            if (_configData.LinkingSystem)
            {
                permission.AddUserGroup(steamID, _configData.OxideGroup);
            }
            else
            {
                _guild.GetMember(Client, (Snowflake)discordID).Then((GuildMember member) =>
                {
                    if (!member.HasRole(_boosterRole))
                    {
                        return;
                    }
                    if (_storedData.UserData.ContainsKey(discordID)) //already verified
                    {
                        permission.RemoveUserGroup(_storedData.UserData[discordID], _configData.OxideGroup);
                        _storedData.UserData[discordID] = steamID;
                        permission.AddUserGroup(steamID, _configData.OxideGroup);
                    }
                    else
                    {
                        permission.AddUserGroup(steamID, _configData.OxideGroup);
                        _storedData.UserData.Add(discordID, steamID);
                    }
                });
            }
        }
        public void RemoveFromNitroGroup(string discordID)
        {
            if (_configData.LinkingSystem)
            {
                var steamID = _link.GetPlayerId((Snowflake)discordID);
                if (steamID.IsValid)
                {
                    permission.RemoveUserGroup(steamID.Id, _configData.OxideGroup);
                }
            }
            else
            {
                string steamID;
                _storedData.UserData.TryGetValue(discordID, out steamID);
                if (steamID != null)
                {
                    permission.RemoveUserGroup(steamID, _configData.OxideGroup);
                    _storedData.UserData.Remove(discordID);
                }
            }
        }
        private DiscordEmbed CreateEmbed(string title, string message, string color)
        {
            return new DiscordEmbedBuilder()
                   .AddTitle(title)
                   .AddDescription(message)
                   .AddColor(color)
                   .Build();
        }
        private void CreateDM(GuildMember member, string title, string content, string color)
        {
            if (_configData.DirectMessageBoosters)
            {
                member.User.CreateDirectMessageChannel(Client).Then(DM => DM.CreateMessage(Client, CreateEmbed(title, content, color)));
            }
        }
        private string Lang(string key, string id = null) => lang.GetMessage(key, this, id);
        #endregion

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                //bot messages
                ["NotBoostingTitle"] = "You are not boosting the server yet.",
                ["NotBoostingContent"] = "You must boost the server if you want to receive the rewards for it.",
                ["NoLongerBoostingTitle"] = "You are no longer boosting our discord server.",
                ["NoLongerBoostingContent"] = "If you want to get access to the nitro features you have to boost our discord server again.",
                ["UnableToVerifyTitle"] = "You couldn't have been verified.",
                ["UnableToVerifyContent"] = "Please use !verify <SteamID64> (you can find your Steam 64 ID with this site: https://steamid.io/lookup/)",
                ["SuccesfullyVerifiedTitle"] = "You have been verified successfully.",
                ["SuccesfullyVerifiedContent"] = "Your in-game rewards for boosting are activated.",
                ["NotInGuildTitle"] = "You are not in the specified discord server.",
                ["NotInGuildContent"] = "Please join our discord server to continue.",
                ["JustBoostedTitle"] = "Thanks for boosting our discord server.",
                ["JustBoostedContent"] = "In order to receive your in-game permissions use !verify <your steamid64>",
                // with discordauth or discordcore messages
                ["AccountsLinkedTitle"] = "Thanks for boosting our discord server.",
                ["AccountsLinkedContent"] = "Your benefits are activated.",
                ["AccountsUnLinkedTitle"] = "You have unlinked your accounts.",
                ["AccountsUnLinkedContent"] = "In order to keep your perks you must link your accounts and boost our discord server.",
                //console messages
                ["Connected"] = "Discord bot connected.",
                ["GroupCreated"] = "An oxide group called {0} has been created.",
                ["SetGuildID"] = "Your Discord Bot appears to be in more than one discord guilds. Please set the Discord Guild ID in the config file.",
                ["NoServerID"] = "Please set the discord server id and reload the plugin to continue.",
                ["NoToken"] = "Please set the discord bot token and reload the plugin to continue.",
                ["NoOxideGroup"] = "Please set the oxide group in the config file.",
                ["NoLinkingType"] = "Please set the linking type in the config file.",
                ["InstallLinkerPlugin"] = "Please install/load DiscordAuth or DiscordCore or change the config option to false.",
                ["InviteBot"] = "Please invite the bot into your discord server and reload the plugin.",
                ["Errors"] = "Please make sure that your guildid is correct in the config file and the bot is in your discord server.",
                ["ConfigIssue"] = "Config file issue detected. Please delete file, or check syntax and fix.",
                ["NewConfig"] = "Creating new config file."
            }, this, "en");
        }
        #endregion
    }
}