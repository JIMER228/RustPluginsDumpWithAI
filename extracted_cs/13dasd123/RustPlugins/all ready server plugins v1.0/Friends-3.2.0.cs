using System.Text;
using ConVar;
using Oxide.Game.Rust;
using System;
using System.Collections.Generic;
using ProtoBuf;
using Oxide.Core;
using System.Linq;
using Oxide.Core.Plugins;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Friends", "Sempai#3239", "3.2.0")]
    [Description("Friends system rust")]
    public class Friends : RustPlugin
    {

                
        
        private bool HasFriend(ulong playerId, ulong friendId)
        {
            return RelationshipManager.ServerInstance.playerToTeam.ContainsKey(playerId) && RelationshipManager
                .ServerInstance.playerToTeam[playerId].members.Contains(friendId);
        }

        private void RemoveCupboardCache(BuildingPrivlidge buildingPrivlidge)
        {
            if (buildingPrivlidge == null || !buildingPrivlidge.OwnerID.IsSteamId()) return;
            if (_playerCupboardCache.ContainsKey(buildingPrivlidge.OwnerID) &&
                _playerCupboardCache[buildingPrivlidge.OwnerID].Contains(buildingPrivlidge))
                _playerCupboardCache[buildingPrivlidge.OwnerID].Remove(buildingPrivlidge);
        }
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
        
        
        private object OnSamSiteTarget(SamSite entity, BaseCombatEntity target)
        {
            BaseVehicle vehicle = target as BaseVehicle;
            if (ReferenceEquals(vehicle, null) || entity.OwnerID <= 0)
                return null;
            BasePlayer player = vehicle.GetDriver();
            if (player == null)
                return null;
            if(player.userID == entity.OwnerID)
                return false;
            if (HasFriend(entity.OwnerID, player.userID))
                return false;
            return null;
        }

        private bool IsFriend(ulong playerId, ulong friendId)
        {
            return HasFriend(playerId, friendId);
        }
        private bool WasFriendS(string playerS, string friendS) => HasFriend(ulong.Parse(playerS), ulong.Parse(friendS));

        
        
        
        private object CanUseLockedEntity(BasePlayer player, BaseLock @lock)
        {
            if (player == null || @lock == null || !@lock.IsLocked())
                return null;
            BaseEntity parentEntity = @lock.GetParentEntity();
            ulong ownerID = @lock.OwnerID.IsSteamId() ? @lock.OwnerID : parentEntity != null ? parentEntity.OwnerID : 0;
            if (!ownerID.IsSteamId() || ownerID == player.userID)
                return null;
            CodeLock codeLock1 = @lock as CodeLock;
            if (!ReferenceEquals(codeLock1, null))
            {
                if (HasFriend(ownerID, player.userID))
                {
                    List<ulong> whitelistPlayers = codeLock1.guestPlayers;
                    if (!whitelistPlayers.Contains(player.userID))
                        whitelistPlayers.Add(player.userID);
                }
                else
                {
                    List<ulong> whitelistPlayers = codeLock1.guestPlayers;
                    if (whitelistPlayers.Contains(player.userID))
                        whitelistPlayers.Remove(player.userID);
                }
            }

            if (@lock is KeyLock)
            {
                if (!HasFriend(ownerID, player.userID)) return null;
                return true;
            }

            return null;
        }
                [PluginReference] Plugin IQChat, TruePVE;
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
        
        
        private List<ulong> _friendFire = new List<ulong>();

        
        private BasePlayer FindOnlinePlayer(BasePlayer player, string nameOrID)
        {
            if (nameOrID.IsSteamId())
            {
                BasePlayer target = BasePlayer.FindByID(ulong.Parse(nameOrID));
                if (target != null) return target;
                SendChat(player, GetLang("TEAM_FOUND", player.UserIDString));
                return null;
            }

            List<BasePlayer> targets = BasePlayer.activePlayerList.Where(x => x.displayName == nameOrID).ToList();

            if (targets.Count() == 1)
                return targets[0];

            if (targets.Count() > 1)
            {
                string playersMore = "";
                foreach (BasePlayer plr in targets)
                    playersMore = playersMore + "\n" + plr.displayName + " - " + plr.UserIDString;
                SendChat(player, GetLang("TEAM_FOUND_MULTIPLE", player.UserIDString, playersMore));
                return null;
            }

            targets = BasePlayer.activePlayerList
                .Where(x => String.Equals(x.displayName, nameOrID, StringComparison.CurrentCultureIgnoreCase)).ToList();

            if (targets.Count() == 1)
                return targets[0];

            if (targets.Count() > 1)
            {
                string playersMore = "";
                foreach (BasePlayer plr in targets)
                    playersMore = playersMore + "\n" + plr.displayName + " - " + plr.UserIDString;
                SendChat(player, GetLang("TEAM_FOUND_MULTIPLE", player.UserIDString, playersMore));
                return null;
            }

            targets = BasePlayer.activePlayerList.Where(x => x.displayName.Contains(nameOrID)).ToList();

            if (targets.Count() == 1)
                return targets[0];

            if (targets.Count() > 1)
            {
                string playersMore = "";
                foreach (BasePlayer plr in targets)
                    playersMore = playersMore + "\n" + plr.displayName + " - " + plr.UserIDString;
                SendChat(player, GetLang("TEAM_FOUND_MULTIPLE", player.UserIDString, playersMore));
                return null;
            }

            targets = BasePlayer.activePlayerList.Where(x => x.displayName.ToLower().Contains(nameOrID.ToLower()))
                .ToList();

            if (targets.Count() == 1)
                return targets[0];

            if (targets.Count() > 1)
            {
                string playersMore = "";
                foreach (var plr in targets)
                    playersMore = playersMore + "\n" + plr.displayName + " - " + plr.UserIDString;
                SendChat(player, GetLang("TEAM_FOUND_MULTIPLE", player.UserIDString, playersMore));
                return null;
            }

            SendChat(player, GetLang("TEAM_FOUND", player.UserIDString));
            return null;
        }
        
        private readonly Dictionary<ulong, HashSet<BuildingPrivlidge>> _playerCupboardCache =
            new Dictionary<ulong, HashSet<BuildingPrivlidge>>();

        private string[] GetFriends(string playerS)
        {
            return GetFriends(ulong.Parse(playerS)).ToList().ConvertAll(f => f.ToString()).ToArray();
        }

        
        
        
        private void UpdateCupboardAuth(HashSet<BuildingPrivlidge> buildingPrivlidges, ulong ownerId)
        {
            if (buildingPrivlidges.Count <= 0) return;
            foreach (BuildingPrivlidge buildingPrivlidge in buildingPrivlidges)
            {
                if (buildingPrivlidge == null || buildingPrivlidge.IsDestroyed) continue;
                buildingPrivlidge.authorizedPlayers.Clear();
                List<PlayerNameID> team = GetTeamMembers(ownerId);
                if (team == null) continue;
                foreach (PlayerNameID friend in team)
                {
                    buildingPrivlidge.authorizedPlayers.Add(friend);
                }

                buildingPrivlidge.SendNetworkUpdate();
            }
        }
        
        private void OnPlayerConnected(BasePlayer player)
        {
            if (!_ffTeam.ContainsKey(player.userID))
                _ffTeam.Add(player.userID, false);
        }
        
        private void UpdateTeamAuthList(List<ulong> teamMembers)
        {
            if (teamMembers.Count <= 0) return;
            foreach (ulong member in teamMembers)
                UpdateAuthList(member);
        }

        
        
        private Dictionary<ulong, bool> _ffTeam = new Dictionary<ulong, bool>();

        private void SendChat(BasePlayer player, string message, string hexColorMsg = "#ffffff", string colortwo = "#fff770", string customAvatar = "", Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, message, _config.ChatPrefix, customAvatar, hexColorMsg);
            else player.SendConsoleCommand("chat.add", channel, 0, message);
        }

        private void OnServerInitialized()
        {
            StringBuilderInstance = new StringBuilder();
            RelationshipManager.maxTeamSize = _config.MaxTeamSize;
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
            Subscribes();

            foreach (BaseNetworkable serverEntity in BaseNetworkable.serverEntities)
            {
                BuildingPrivlidge buildingPrivlidge = serverEntity as BuildingPrivlidge;
                if (buildingPrivlidge != null)
                {
                    AddCupboardCache(buildingPrivlidge);
                }
            }
            foreach (string command in _config.ChatCommands)
                cmd.AddChatCommand(command, this, nameof(CmdTeamCommand));
        }

        
        
        private void OnTeamAcceptInvite(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            NextTick(() =>
            {
                if (team == null || player == null) return;
                if (team.members.Contains(player.userID))
                {
                    UpdateTeamAuthList(team.members);
                }
            });
        }

        private void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("FriendlyFireData", _ffTeam);

        private string[] GetFriendList(string playerS) => GetFriendList(ulong.Parse(playerS));

        private void OnTeamLeave(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            NextTick(() =>
            {
                if (team == null || player == null) return;
                if (!team.members.Contains(player.userID))
                {
                    UpdateTeamAuthList(new List<ulong>(team.members) { player.userID });
                }
            });
            SendChat(player, lang.GetMessage("TEAM_CUPBOARADDLEAVE", this));
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            return TruePVE != null ? null : CanEntityTakeDamage(entity, info);
        }
        private bool HadFriends(string playerS, string targetS) => HasFriend(ulong.Parse(playerS), ulong.Parse(targetS));

        private string[] IsFriendOf(string playerS)
        {
            ulong playerId = Convert.ToUInt64(playerS);
            ulong[] friends = IsFriendOf(playerId);
            return friends.ToList().ConvertAll(f => f.ToString()).ToArray();
        }

        private bool WereFriends(ulong player, ulong target) => HasFriend(player, target);
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
        private bool HasFriends(string playerS, string friendS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;
            ulong playerId = Convert.ToUInt64(playerS);
            ulong friendId = Convert.ToUInt64(friendS);
            return HasFriend(playerId, friendId);
        }
        private bool WereFriendsS(string playerS, string friendS) => HasFriend(ulong.Parse(playerS), ulong.Parse(friendS));

        
        
        private void Init()
        {
            Unsubscribes();
            LoadData();
        }
        
        private void UpdateAuthList(ulong ownerId)
        {
            if(!_playerCupboardCache.ContainsKey(ownerId)) return;
            if(_playerCupboardCache[ownerId].Count <= 0) return;
            UpdateCupboardAuth(_playerCupboardCache[ownerId], ownerId);
        }

        private void OnEntityKill(BuildingPrivlidge buildingPrivlidge) => RemoveCupboardCache(buildingPrivlidge);

        
        
        void CmdTeamCommand(BasePlayer player, string command, string[] arg)
        {
            if (arg == null || arg.Length == 0)
            {
                SendChat(player, GetLang("TEAM_INFO", player.UserIDString));
                return;
            }

            RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
            switch (arg[0])
            {
                case "invite":
                case "add":
                {
                    if (arg.Length != 2)
                    {
                        SendChat(player, GetLang("TEAM_NULLNICKNAME", player.UserIDString));
                        return;
                    }

                    BasePlayer target = FindOnlinePlayer(player, arg[1]);

                    if (team == null)
                    {
                        SendChat(player, GetLang("TEAM_NULL", player.UserIDString));
                        return;
                    }

                    if (target == null)
                    {
                        return;
                    }

                    if (target == player)
                    {
                        SendChat(player, GetLang("TEAM_NULLNICKNAMENULL", player.UserIDString));
                        return;
                    }

                    if (target.currentTeam != 0)
                    {
                        SendChat(player, GetLang("TEAM_ISCOMMAND", player.UserIDString, target.displayName));
                        return;
                    }

                    if (team.members.Count >= _config.MaxTeamSize)
                    {
                        SendChat(player, GetLang("TEAM_MAXTEAMSIZE", player.UserIDString));
                        return;
                    }

                    timer.Once(_config.InviteAcceptTime, () =>
                    {
                        if (!team.members.Contains(target.userID))
                        {
                            if (team == null)
                                player.ClearPendingInvite();
                            else
                                team.RejectInvite(target);

                            SendChat(player, GetLang("TEAM_TIMENULL", player.UserIDString));
                            SendChat(target, GetLang("TEAM_TIMENULS", target.UserIDString, player.displayName));
                        }
                    });
                    team.SendInvite(target);
                    team.MarkDirty();
                    SendChat(player, GetLang("TEAM_IVITE", player.UserIDString, target.displayName));
                    SendChat(target, GetLang("TEAM_INVITETARGET", target.UserIDString, player.displayName));
                    break;
                }
                case "ff":
                {
                    if (!_ffTeam[player.userID])
                    {
                        _ffTeam[player.userID] = true;
                        SendChat(player, GetLang("TEAM_FFON", player.UserIDString));
                    }
                    else
                    {
                        _ffTeam[player.userID] = false;
                        SendChat(player, GetLang("TEAM_FFOFF", player.UserIDString));
                    }

                    break;
                }
            }
        }

        private object CanEntityTakeDamage(BaseEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return null;
            BasePlayer target = entity as BasePlayer;
            BasePlayer attacker = info.Initiator as BasePlayer;
            if (attacker == null || target == null || attacker == target) return null;
            if (!HasFriend(attacker.userID, target.userID)) return null;
            if (_ffTeam[attacker.userID]) return null;
            if (_friendFire.Contains(attacker.userID)) return false;
            _friendFire.Add(attacker.userID);
            timer.Once(5f, () =>
            {
                if (_friendFire.Contains(attacker.userID))
                    _friendFire.Remove(attacker.userID);
            });
            SendChat(attacker, string.Format(lang.GetMessage("TEAM_FFATTACK", this), target.displayName));
            return false;
        }
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
        private void OnEntitySpawned(BuildingPrivlidge buildingPrivlidge) => AddCupboardCache(buildingPrivlidge, true);

        
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TEAM_FOUND"] = "Player not found",
                ["TEAM_FOUND_MULTIPLE"] = "Several players with this nickname were found\n{0}",
                ["TEAM_SENDINVITETILE"] = "FRIENDS SYSTEM",
                ["TEAM_SENDINVITE"] = "Player <color=#89f5bf>{0}</color>, sent you invitations to the team",
                ["TEAM_NULL"] = "You have not created a command.\nTo create it, click (<color=#3eb2f0>TAB</color>)",
                ["TEAM_NULLNICKNAME"] = "You have not entered a player's nickname!",
                ["TEAM_NULLNICKNAMENULL"] = "You cannot add yourself",
                ["TEAM_ISCOMMAND"] = "Player <color=#89f5bf>{0}</color> already a member of the team",
                ["TEAM_MAXTEAMSIZE"] = "There are no more places in the team",
                ["TEAM_TIMENULL"] = "Your invitation has been canceled, the time has passed to accept the request",
                ["TEAM_TIMENULS"] =
                    "Invitation from <color=#89f5bf>{0}</color> to join the team is canceled, the time has passed to accept the request",
                ["TEAM_IVITE"] = "You have successfully sent invitations to the player <color=#89f5bf>{0}</color>",
                ["TEAM_INVITETARGET"] =
                    "Player <color=#89f5bf>{0}</color>, sent you invitations to the team.\nTo accept or reject, click (<color=#3eb2f0>TAB</color>)",
                ["TEAM_CUPBOARCLEAR"] = "You kicked the time.\nHe was automatically discharged from the closets!",
                ["TEAM_CUPBOARADD"] =
                    "Your new friend (<color=#89f5bf>{0}</color>) successfully authorized in cabinets!",
                ["TEAM_CUPBOARADDLEAVE"] = "You left the team and were deauthorized in the closets",
                ["TEAM_FFON"] = "You <color=#64f578>included</color> damage by friends",
                ["TEAM_FFOFF"] = "You <color=#f03e3e>disconnected</color> damage by friends",
                ["TEAM_FFATTACK"] =
                    "Player: <color=#89f5bf>{0}</color> your friend!\nYou can't him <color=#ff9696>to kill</color>\nTo include damage on friends write / team ff",
                ["TEAM_INFO"] =
                    "In order to create a team, click (<color=#3eb2f0>TAB</color>)" +
                    "\nAnd click on the button (Create team)\n" +
                    "You can invite a player to the team through (<color=#3eb2f0>TAB</color>)\n" +
                    "1. <color=#46bec2>/Team add nick</color> - Invite to the team at a distance\n" +
                    "2. <color=#46bec2>/team ff</color> - Turns fire on and off for friends\n" +
                    "When a player is added to the team, he will be automatically authorized in <color=#89f5af>Turrets</color>,<color=#89f5af>cabinets</color>,<color=#89f5af>doorway</color>." +
                    "\nAlso, when deleting from friends, the player will <color=#ff9696>unauthorize</color>",


            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TEAM_FOUND"] = "Игрок не найден",
                ["TEAM_FOUND_MULTIPLE"] = "Найдено несколько игроков с таким ником\n{0}",
                ["TEAM_SENDINVITETILE"] = "СИСТЕМА ДРУЗЕЙ",
                ["TEAM_SENDINVITE"] = "Игрок <color=#89f5bf>{0}</color>, отправил вам приглашения в команду",
                ["TEAM_NULL"] = "У вас не создана команда.\nЧтобы ее создать нажмите (<color=#3eb2f0>TAB</color>)",
                ["TEAM_NULLNICKNAME"] = "Вы не указали ник игрока!",
                ["TEAM_NULLNICKNAMENULL"] = "Вы не можете добавить сами себя",
                ["TEAM_ISCOMMAND"] = "Игрок <color=#89f5bf>{0}</color> уже состоит в команде",
                ["TEAM_MAXTEAMSIZE"] = "Мест в команде больше нету",
                ["TEAM_TIMENULL"] = "Ваше приглашение отменено, истекло время на принятие запроса",
                ["TEAM_TIMENULS"] =
                    "Приглашение от <color=#89f5bf>{0}</color> на вступление в команду отменено, истекло время на принятие запроса",
                ["TEAM_IVITE"] = "Вы успешно отправили приглашения игроку <color=#89f5bf>{0}</color>",
                ["TEAM_INVITETARGET"] =
                    "Игрок <color=#89f5bf>{0}</color>, отправил вам приглащения в команду.\nЧтобы его принять или откланить нажмите (<color=#3eb2f0>TAB</color>)",
                ["TEAM_CUPBOARCLEAR"] = "Вы кикнули тимейта.\nЕго автоматически выписало из шкафов!",
                ["TEAM_CUPBOARADD"] = "Ваш новый друг (<color=#89f5bf>{0}</color>) успешно авторизован а шкафах!",
                ["TEAM_CUPBOARADDLEAVE"] = "Вы вышли из команды и были деавторизованы в шкафах",
                ["TEAM_FFON"] = "Вы <color=#64f578>включили</color> урон по друзьям",
                ["TEAM_FFOFF"] = "Вы <color=#f03e3e>отключили</color> урон по друзьям",
                ["TEAM_FFATTACK"] =
                    "Игрок: <color=#89f5bf>{0}</color> ваш друг!\nВы не можете его <color=#ff9696>убить</color>\nЧто бы включить урон по друзьям напишите /team ff",
                ["TEAM_INFO"] =
                    "Для того чтоб создать команду нажмите (<color=#3eb2f0>TAB</color>)" +
                    "\nИ нажмите на кнопку (Создать команду)\n" +
                    "Пригласить игрока в команду можно через (<color=#3eb2f0>TAB</color>)\n" +
                    "1. <color=#46bec2>/Team add ник</color> - Пригласить в команду на расcтоянии\n" +
                    "2. <color=#46bec2>/team ff</color> - Включает и выключает огонь по друзьям\n" +
                    "При добавлении игрока в команду, он будет автоматически авторизован в <color=#89f5af>Турелях</color>,<color=#89f5af>шкафах</color>,<color=#89f5af>дверях</color>." +
                    "\nТак же при удалении с друзей, игрок будет <color=#ff9696>деавторизирован</color>",


            }, this, "ru");
        }

        
        
        
        
        private object OnTurretTarget(AutoTurret turret, BaseCombatEntity target)
        {
            BasePlayer player = target as BasePlayer;
            if (ReferenceEquals(player, null) || turret.OwnerID <= 0) return null;
            if (HasFriend(turret.OwnerID, player.userID)) return false;
            return null;
        }

        
                
        
        private readonly List<string> _hooks = new List<string>
        {
            "OnTeamAcceptInvite",
            "OnTeamKick",
            "OnTeamLeave",
            "OnTeamDisbanded",
            "OnEntitySpawned",
            "OnEntityKill", 
            "OnSamSiteTarget", 
            "CanUseLockedEntity",
            "OnTurretTarget",
        };
        private bool HadFriend(ulong player, ulong target) => HasFriend(player, target);

        private bool AreFriends(ulong playerId, ulong friendId)
        {
            if (RelationshipManager.ServerInstance.playerToTeam.ContainsKey(playerId) &&
                RelationshipManager.ServerInstance.playerToTeam.ContainsKey(friendId))
            {
                return RelationshipManager.ServerInstance.playerToTeam[playerId].members.Contains(friendId) &&
                       RelationshipManager.ServerInstance.playerToTeam[friendId].members.Contains(playerId);
            }

            return false;
        }
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null)
                    throw new Exception();
                SaveConfig();
            }
            catch
            {
                for (int i = 0; i < 3; i++)
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        private bool WasFriend(ulong player, ulong target) => HasFriend(player, target);

        private class Configuration
        {
            [JsonProperty(PropertyName = "Использовать авторизацию друзей в замках?")]
            public bool CodeLockAuthUse = true;
            [JsonProperty(PropertyName = "Использовать авторизацию друзей в ПВО?")]
            public bool SamSiteAuthUse = true;
            [JsonProperty(PropertyName = "Время ожидания ответа на принятия приглашения в команду:")]
            public float InviteAcceptTime = 20f;
            
            [JsonProperty(PropertyName = "Использовать авторизацию друзей в турелях?")]
            public bool TurretAuthUse = true;
            [JsonProperty(PropertyName = "Максимальное количество друзей:")]
            public int MaxTeamSize = 3;
            
            [JsonProperty(PropertyName = "Префикс в чате (IQChat)")]
            public string ChatPrefix = "<color=#5cd6770>[Система друзей]</color>\n";
            [JsonProperty(PropertyName = "Чат команды")]
            public string[] ChatCommands = {"team", "ff", "friend"};
            [JsonProperty(PropertyName = "Использовать авторизацию друзей в шкафах?")]
            public bool CupboardAuthUse = true;
            
        }

        private string GetLang(string langKey, string userID = null, params object[] args)
        {
            StringBuilderInstance.Clear();
            if (args != null)
            {
                StringBuilderInstance.AppendFormat(lang.GetMessage(langKey, this, userID), args);
                return StringBuilderInstance.ToString();
            }
            return lang.GetMessage(langKey, this, userID);
        }
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
        private void LoadData() =>
            _ffTeam = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, bool>>("FriendlyFireData");

        private bool IsFriends(string playerS, string friendS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;
            ulong playerId = Convert.ToUInt64(playerS);
            ulong friendId = Convert.ToUInt64(friendS);
            return IsFriend(playerId, friendId);
        }

        private ulong[] IsFriendOf(ulong playerId)
        {
            HashSet<ulong> members = new HashSet<ulong>();
            foreach (string member in GetFriendList(playerId))
            {
                if (ulong.Parse(member) != playerId) members.Add(ulong.Parse(member));
            }
            return members.ToArray();
        }

        private ulong[] GetFriends(ulong playerId)
        {
            HashSet<ulong> members = new HashSet<ulong>();
            foreach (string member in GetFriendList(playerId))
            {
                if (ulong.Parse(member) != playerId) members.Add(ulong.Parse(member));
            }
            return members.ToArray();
        }

        
        
        private Configuration _config;

        private void Subscribes()
        {
            foreach (string hook in _hooks)
            {
                if (hook == "OnSamSiteTarget" && !_config.SamSiteAuthUse)
                {
                    continue;
                }

                if (hook == "OnTurretTarget" && !_config.TurretAuthUse)
                {
                    continue;
                }
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
                if (hook == "CanUseLockedEntity" && !_config.CodeLockAuthUse)
                {
                    continue;
                }
		   		 		  						  	   		  		 			  	 	 		   		 		  		 	
                if ((hook == "OnTeamAcceptInvite" || hook == "OnTeamKick" || hook == "OnTeamLeave" ||
                     hook == "OnTeamDisbanded" || hook == "OnEntitySpawned" || hook == "OnEntityKill") &&
                    !_config.CupboardAuthUse)
                {
                    continue;
                }

                Subscribe(hook);
            }
        }

        private void OnTeamKick(RelationshipManager.PlayerTeam team, BasePlayer player, ulong target)
        {
            NextTick(() =>
            {
                if (team == null) return;
                if (!team.members.Contains(target))
                {
                    UpdateTeamAuthList(new List<ulong>(team.members) { target });
                }
            });
            SendChat(player, lang.GetMessage("TEAM_CUPBOARCLEAR", this));
        }

        private void AddCupboardCache(BuildingPrivlidge buildingPrivlidge, bool nowSpawned = false)
        {
            if (buildingPrivlidge == null || !buildingPrivlidge.OwnerID.IsSteamId()) return;
            if (!_playerCupboardCache.ContainsKey(buildingPrivlidge.OwnerID))
                _playerCupboardCache.Add(buildingPrivlidge.OwnerID,
                    new HashSet<BuildingPrivlidge> { buildingPrivlidge });

            if (!_playerCupboardCache[buildingPrivlidge.OwnerID].Contains(buildingPrivlidge))
                _playerCupboardCache[buildingPrivlidge.OwnerID].Add(buildingPrivlidge);

            if (nowSpawned)
                UpdateCupboardAuth(new HashSet<BuildingPrivlidge> { buildingPrivlidge }, buildingPrivlidge.OwnerID);
        }

        private void Unload()
        {
            SaveData();
            StringBuilderInstance = null;
        } 

        
        
        private List<PlayerNameID> GetTeamMembers(ulong ownerId)
        {
            if (!RelationshipManager.TeamsEnabled()) return null;
            RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindPlayersTeam(ownerId);
            return playerTeam == null
                ? new List<PlayerNameID>
                {
                    new PlayerNameID
                        { userid = ownerId, username = RustCore.FindPlayerById(ownerId)?.displayName ?? string.Empty }
                }
                : playerTeam?.members.Select(userid => new PlayerNameID
                        { userid = userid, username = RustCore.FindPlayerById(userid)?.displayName ?? string.Empty })
                    .ToList();

        }

        private string[] GetFriendList(ulong playerId)
        {
            return RelationshipManager.ServerInstance.playerToTeam.ContainsKey(playerId)
                ? RelationshipManager.ServerInstance.playerToTeam[playerId].members.ConvertAll(f => f.ToString())
                    .ToArray()
                : Array.Empty<string>();
        }

        private bool AreFriends(string playerS, string friendS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;
            ulong playerId = ulong.Parse(playerS);
            ulong friendId = ulong.Parse(friendS);
            return AreFriends(playerId, friendId);
        }

        private void Unsubscribes()
        {
            foreach (string hook in _hooks)
            {
                Unsubscribe(hook);
            }
        }

        private void OnTeamDisbanded(RelationshipManager.PlayerTeam playerTeam)
        {
            if (playerTeam == null) return;
            UpdateTeamAuthList(playerTeam.members);
        }

        public static StringBuilder StringBuilderInstance;
            }
}
