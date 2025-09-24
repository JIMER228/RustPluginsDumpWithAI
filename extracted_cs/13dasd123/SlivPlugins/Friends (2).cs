// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using ConVar;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Friends", "GRACED", "2.9.83")]
    [Description("Friends system rust")]
    public class Friends : RustPlugin
    {
        [PluginReference] Plugin IQChat;

        #region Configuration
        private Configuration config;
        private class Configuration
        {
            public class Settings
            {
                [JsonProperty(PropertyName = "Максимальное количество друзей:")]
                public Int32 maxTeamSize = 3;
                [JsonProperty(PropertyName = "Время на принятия приглашения в команду:")]
                public float InviteTimer = 20f;
                [JsonProperty(PropertyName = "Префикс в чате (IQChat)")]
                public String ChatPrefix = "<color=#5cd61351>[Система друзей]</color>\n";
                [JsonProperty(PropertyName = "Чат команда")]
                public String ChatCommand = "team";
                [JsonProperty(PropertyName = "Авторизировать тимейтов в ПВО?")]
                public Boolean samSiteUse = true;
                [JsonProperty(PropertyName = "Авторизировать/деавторизировать тимейтов в шкафах?")]
                public Boolean cupboardUse = true;

            }
            [JsonProperty("Настройки")]
            public Settings settings = new Settings();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                    throw new Exception();
                SaveConfig();
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                LoadDefaultConfig();
            }
            SaveConfig();
        }


        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }
        #endregion
  
        #region Data
        private Dictionary<ulong, bool> FFTeam = new Dictionary<ulong, bool>();
        private void LoadData() => FFTeam = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, bool>>("FriendlyFireData");
        private void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("FriendlyFireData", FFTeam);
        #endregion

        #region Lang
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
                ["TEAM_TIMENULS"] = "Invitation from <color=#89f5bf>{0}</color> to join the team is canceled, the time has passed to accept the request",
                ["TEAM_IVITE"] = "You have successfully sent invitations to the player <color=#89f5bf>{0}</color>",
                ["TEAM_INVITETARGET"] = "Player <color=#89f5bf>{0}</color>, sent you invitations to the team.\nTo accept or reject, click (<color=#3eb2f0>TAB</color>)",
                ["TEAM_CUPBOARCLEAR"] = "You kicked the time.\nHe was automatically discharged from the closets!",
                ["TEAM_CUPBOARADD"] = "Your new friend (<color=#89f5bf>{0}</color>) successfully authorized in cabinets!",
                ["TEAM_CUPBOARADDLEAVE"] = "You left the team and were deauthorized in the closets",
                ["TEAM_FFON"] = "You <color=#64f578>included</color> damage by friends",
                ["TEAM_FFOFF"] = "You <color=#f03e3e>disconnected</color> damage by friends",
                ["TEAM_FFATTACK"] = "Player: <color=#89f5bf>{0}</color> your friend!\nYou can't him <color=#ff9696>to kill</color>\nTo include damage on friends write / team ff",
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
                ["TEAM_TIMENULS"] = "Приглашение от <color=#89f5bf>{0}</color> на вступление в команду отменено, истекло время на принятие запроса",
                ["TEAM_IVITE"] = "Вы успешно отправили приглашения игроку <color=#89f5bf>{0}</color>",
                ["TEAM_INVITETARGET"] = "Игрок <color=#89f5bf>{0}</color>, отправил вам приглащения в команду.\nЧтобы его принять или откланить нажмите (<color=#3eb2f0>TAB</color>)",
                ["TEAM_CUPBOARCLEAR"] = "Вы кикнули тимейта.\nЕго автоматически выписало из шкафов!",
                ["TEAM_CUPBOARADD"] = "Ваш новый друг (<color=#89f5bf>{0}</color>) успешно авторизован а шкафах!",
                ["TEAM_CUPBOARADDLEAVE"] = "Вы вышли из команды и были деавторизованы в шкафах",
                ["TEAM_FFON"] = "Вы <color=#64f578>включили</color> урон по друзьям",
                ["TEAM_FFOFF"] = "Вы <color=#f03e3e>отключили</color> урон по друзьям",
                ["TEAM_FFATTACK"] = "Игрок: <color=#89f5bf>{0}</color> ваш друг!\nВы не можете его <color=#ff9696>убить</color>\nЧто бы включить урон по друзьям напишите /team ff",
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

        #endregion

        #region HOOKS
        private void Init()
        {
            Unsubscribe(nameof(OnSamSiteTarget));
            UnsubscribeHook();
            LoadData();
        }
        private void OnServerInitialized()
        {
            foreach(BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);

            RelationshipManager.maxTeamSize = config.settings.maxTeamSize;
            cmd.AddChatCommand(config.settings.ChatCommand, this, nameof(teamadd));

            if(config.settings.samSiteUse)
                Subscribe(nameof(OnSamSiteTarget));
            if (config.settings.cupboardUse)
                SubscribeHook();
        }

        private void Unload() => SaveData();

        private object OnEntityTakeDamage(BasePlayer player, HitInfo info)
        {
            if (player == null || info.InitiatorPlayer == null)
                return null;
            if (player == info.InitiatorPlayer)
                return null;

            if (HasFriend(info.InitiatorPlayer.userID, player.userID))
            {
                if (!FFTeam[info.InitiatorPlayer.userID])
                {
                    SendChat(info.InitiatorPlayer, String.Format(lang.GetMessage("TEAM_FFATTACK", this), player.displayName));
                    return true;
                }
            }
            return null;
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!FFTeam.ContainsKey(player.userID))
                FFTeam.Add(player.userID, false);
        }
        object OnTeamAcceptInvite(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            AuthOnEntity(team.teamLeader, player);
            return null;
        }
        object OnTeamKick(RelationshipManager.PlayerTeam team, BasePlayer player, ulong target)
        {
            DeAuthOnEntity(player.userID, target);
            SendChat(player, lang.GetMessage("TEAM_CUPBOARCLEAR", this));
            return null;
        }

        object OnTeamLeave(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            DeAuthOnEntity(team.teamLeader, player.userID);
            SendChat(player, lang.GetMessage("TEAM_CUPBOARADDLEAVE", this));
            return null;
        }
        #endregion
     
        #region Commands
        void teamadd(BasePlayer player, string command, string[] arg)
        {
            if (arg == null || arg.Length == 0)
            {
                SendChat(player, lang.GetMessage("TEAM_INFO", this, player.UserIDString));
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
                            SendChat(player, lang.GetMessage("TEAM_NULLNICKNAME", this, player.UserIDString));
                            return;
                        }
                        BasePlayer target = FindOnlinePlayer(player ,arg[1]);

                        if (team == null)
                        {
                            SendChat(player, lang.GetMessage("TEAM_NULL", this, player.UserIDString));
                            return;
                        }
                        if (target == null)
                        {
                            SendChat(player, lang.GetMessage("TEAM_FOUND", this, player.UserIDString));
                            return;
                        }
                        if (target == player)
                        {
                            SendChat(player, lang.GetMessage("TEAM_NULLNICKNAMENULL", this, player.UserIDString));
                            return;
                        }
                        if (target.currentTeam != 0)
                        {
                            SendChat(player, String.Format(lang.GetMessage("TEAM_ISCOMMAND", this, player.UserIDString), target.displayName));
                            return;
                        }
                        if (team.members.Count >= config.settings.maxTeamSize)
                        {
                            SendChat(player, lang.GetMessage("TEAM_MAXTEAMSIZE", this, player.UserIDString));
                            return;
                        }
                        timer.Once(config.settings.InviteTimer, () =>
                        {
                            if (!team.members.Contains(target.userID))
                            {
                                if (team == null)
                                    player.ClearPendingInvite();
                                else
                                    team.RejectInvite(target);

                                SendChat(player, lang.GetMessage("TEAM_TIMENULL", this, player.UserIDString));
                                SendChat(target, String.Format(lang.GetMessage("TEAM_TIMENULS", this, player.UserIDString), player.displayName));
                            }
                        });
                        team.SendInvite(target);
                        team.MarkDirty();
                        SendChat(player, String.Format(lang.GetMessage("TEAM_IVITE", this, player.UserIDString), target.displayName));
                        SendChat(target, String.Format(lang.GetMessage("TEAM_INVITETARGET", this, player.UserIDString), player.displayName));     
                        break;
                    }
                case "ff":
                    {
                        if (!FFTeam[player.userID])
                        {
                            FFTeam[player.userID] = true;
                            SendChat(player, (lang.GetMessage("TEAM_FFON", this, player.UserIDString)));
                        }
                        else
                        {
                            FFTeam[player.userID] = false;
                            SendChat(player, (lang.GetMessage("TEAM_FFOFF", this, player.UserIDString)));
                        }
                        break;
                    }
            }
        }
        #endregion

        #region Turret/door auth/ cup
        #region CupBoardAuth
        private void DeAuthOnEntity(ulong player, ulong targets)
        {
            var playerEntities = GetPlayerEnitityByType(player);
            foreach (var entity in playerEntities)
            {
                if (entity.OwnerID == targets)
                    continue;
                entity.authorizedPlayers.RemoveAll(x => x.userid == targets);
                entity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            }
        }
        private void AuthOnEntity(ulong player, BasePlayer target)
        {
            var playerEntities = GetPlayerEnitityByType(player);
            foreach (var entity in playerEntities)
            {
                entity.authorizedPlayers.Add(new PlayerNameID
                {
                    userid = target.userID,
                    username = target.displayName
                });
                entity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            }
            BasePlayer player1 = BasePlayer.FindByID(player);
            if(player1 != null && player1.IsConnected)
             SendChat(player1, String.Format(lang.GetMessage("TEAM_CUPBOARADD", this, player.ToString()), target.displayName));
        }
        private List<BuildingPrivlidge> GetPlayerEnitityByType(ulong player)
        {
            var playerEntities = new List<BuildingPrivlidge>();
            var BList = BaseNetworkable.serverEntities.Where(ent => ent as BuildingPrivlidge && (ent as BuildingPrivlidge).OwnerID == player);

            foreach (object entity in BList)
            {
                var build = entity as BuildingPrivlidge;
                if (build != null)
                        playerEntities.Add(build);
            }

            return playerEntities;
        }
        #endregion

        private object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            if (privilege == null || player == null) return null;

            var count = privilege.authorizedPlayers.Count;
            if (count == 0) return null;

            if (count > RelationshipManager.maxTeamSize)
            {
                privilege.authorizedPlayers.RemoveRange(0, count - 1);
            }

            return null;
        }

        private object OnTurretTarget(AutoTurret turret, BaseCombatEntity targ)
        {
            if (!(targ is BasePlayer) || turret.OwnerID <= 0) return null;
            var player = (BasePlayer)targ;
            if (HasFriend(turret.OwnerID, player.userID)) return false;
            return null;
        }
        private object OnSamSiteTarget(SamSite entity, BaseCombatEntity target)
        {
            if (!(target is BaseVehicle) || entity.OwnerID <= 0)
                return null;
            BasePlayer player = (target as BaseVehicle).GetDriver();
            if (player == null)
                return null;
            if(player.userID == entity.OwnerID)
                return false;
            if (HasFriend(entity.OwnerID, player.userID))
                return false;
            return null;
        }
        private object CanUseLockedEntity(BasePlayer player, BaseLock @lock)
        {
            if (player == null || @lock == null || !@lock.IsLocked())
                return null;
            var parentEntity = @lock.GetParentEntity();
            var ownerID = @lock.OwnerID.IsSteamId() ? @lock.OwnerID : parentEntity != null ? parentEntity.OwnerID : 0;
            if (!ownerID.IsSteamId() || ownerID == player.userID)
                return null;
            if (@lock is CodeLock)
            {
                if (HasFriend(ownerID, player.userID))
                {
                    var codeLock = @lock as CodeLock;
                    var whitelistPlayers = (List<ulong>)codeLock.guestPlayers;
                    if (!whitelistPlayers.Contains(player.userID))
                        whitelistPlayers.Add(player.userID);
                }
                else
                {
                    var codeLock = @lock as CodeLock;
                    var whitelistPlayers = (List<ulong>)codeLock.guestPlayers;
                    if (whitelistPlayers.Contains(player.userID))
                        whitelistPlayers.Remove(player.userID);
                }
            }
            if (@lock is KeyLock)
            {
                if (HasFriend(ownerID, player.userID))
                {
                    if (@lock is KeyLock)
                        return true;
                }
            }
            return null;
        }

        #endregion

        #region API
        private bool HasFriend(ulong playerId, ulong friendId)
        {
            if (RelationshipManager.ServerInstance.playerToTeam.ContainsKey(playerId) && RelationshipManager.ServerInstance.playerToTeam[playerId].members.Contains(friendId))
                return true;
            else return false;
        }

        private bool HasFriendS(string playerS, string friendS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;
            var playerId = Convert.ToUInt64(playerS);
            var friendId = Convert.ToUInt64(friendS);
            return HasFriend(playerId, friendId);
        }

        private bool IsFriend(ulong playerId, ulong friendId)
        {
            return HasFriend(playerId, friendId);
        }

        private bool AreFriends(ulong playerId, ulong friendId)
        {
            if (RelationshipManager.ServerInstance.playerToTeam.ContainsKey(playerId) && RelationshipManager.ServerInstance.playerToTeam.ContainsKey(friendId))
            {
                return RelationshipManager.ServerInstance.playerToTeam[playerId].members.Contains(friendId) && RelationshipManager.ServerInstance.playerToTeam[friendId].members.Contains(playerId);
            }
            else return false;
        }

        private bool AreFriendsS(string playerS, string friendS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;
            ulong playerId = ulong.Parse(playerS);
            ulong friendId = ulong.Parse(friendS);
            return AreFriends(playerId, friendId);
        }

        private bool IsFriendS(string playerS, string friendS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;
            var playerId = Convert.ToUInt64(playerS);
            var friendId = Convert.ToUInt64(friendS);
            return IsFriend(playerId, friendId);
        }
        private bool HadFriendS(string playerS, string targetS) => HasFriend(ulong.Parse(playerS), ulong.Parse(targetS));
        private bool HadFriend(ulong player, ulong target) => HasFriend(player, target);

        private ulong[] GetFriends(ulong playerId)
        {
            HashSet<ulong> Members = new HashSet<ulong>();
            foreach (string member in GetFriendList(playerId))
            {
                if (ulong.Parse(member) != playerId) Members.Add(ulong.Parse(member));
            }
            return Members.ToArray();
        }

        private string[] GetFriendListS(string playerS) => GetFriendList(ulong.Parse(playerS));

        private string[] GetFriendsS(string playerS)
        {
            return GetFriends(ulong.Parse(playerS)).ToList().ConvertAll(f => f.ToString()).ToArray();
        }

        ulong[] IsFriendOf(ulong playerId)
        {
            HashSet<ulong> Members = new HashSet<ulong>();
            foreach (string member in GetFriendList(playerId))
            {
                if (ulong.Parse(member) != playerId) Members.Add(ulong.Parse(member));
            }
            return Members.ToArray();
        }

        private string[] IsFriendOfS(string playerS)
        {
            var playerId = Convert.ToUInt64(playerS);
            var friends = IsFriendOf(playerId);
            return friends.ToList().ConvertAll(f => f.ToString()).ToArray();
        }

        private bool WereFriends(ulong player, ulong target) => HasFriend(player, target);
        private bool WereFriendsS(string playerS, string friendS) => HasFriend(ulong.Parse(playerS), ulong.Parse(friendS));

        private bool WasFriend(ulong player, ulong target) => HasFriend(player, target);
        private bool WasFriendS(string playerS, string friendS) => HasFriend(ulong.Parse(playerS), ulong.Parse(friendS));

        private string[] GetFriendList(ulong playerId)
        {
            if (RelationshipManager.ServerInstance.playerToTeam.ContainsKey(playerId)) return RelationshipManager.ServerInstance.playerToTeam[playerId].members.ConvertAll(f => f.ToString()).ToArray();
            else return new string[0];
        }
        #endregion

        #region Help

        private BasePlayer FindOnlinePlayer(BasePlayer player, string nameOrID)
        {
            if (nameOrID.IsSteamId())
            {
                var target = BasePlayer.FindByID(ulong.Parse(nameOrID));
                if (target == null)
                {
                    SendChat(player, lang.GetMessage("TEAM_FOUND", this, player.UserIDString));
                    return null;
                }

                return target;
            }

            var targets = BasePlayer.activePlayerList.Where(x => x.displayName == nameOrID).ToList();

            if (targets.Count() == 1)
                return targets[0];

            if (targets.Count() > 1)
            {
                var PlayersMore = "";
                foreach (var plr in targets)
                    PlayersMore = PlayersMore + "\n" + plr.displayName + " - " + plr.UserIDString;
                SendChat(player, String.Format(lang.GetMessage("TEAM_FOUND_MULTIPLE", this, player.UserIDString), PlayersMore));
                return null;
            }

            targets = BasePlayer.activePlayerList.Where(x => x.displayName.ToLower() == nameOrID.ToLower()).ToList();

            if (targets.Count() == 1)
                return targets[0];

            if (targets.Count() > 1)
            {
                var PlayersMore = "";
                foreach (var plr in targets)
                    PlayersMore = PlayersMore + "\n" + plr.displayName + " - " + plr.UserIDString;
                SendChat(player, String.Format(lang.GetMessage("TEAM_FOUND_MULTIPLE", this, player.UserIDString), PlayersMore));
                return null;
            }

            targets = BasePlayer.activePlayerList.Where(x => x.displayName.Contains(nameOrID)).ToList();

            if (targets.Count() == 1)
                return targets[0];

            if (targets.Count() > 1)
            {
                var PlayersMore = "";
                foreach (var plr in targets)
                    PlayersMore = PlayersMore + "\n" + plr.displayName + " - " + plr.UserIDString;
                SendChat(player, String.Format(lang.GetMessage("TEAM_FOUND_MULTIPLE", this, player.UserIDString), PlayersMore));
                return null;
            }

            targets = BasePlayer.activePlayerList.Where(x => x.displayName.ToLower().Contains(nameOrID.ToLower())).ToList();

            if (targets.Count() == 1)
                return targets[0];

            if (targets.Count() > 1)
            {
                var PlayersMore = "";
                foreach (var plr in targets)
                    PlayersMore = PlayersMore + "\n" + plr.displayName + " - " + plr.UserIDString;
                SendChat(player, String.Format(lang.GetMessage("TEAM_FOUND_MULTIPLE", this, player.UserIDString), PlayersMore));
                return null;
            }

            SendChat(player, lang.GetMessage("TEAM_FOUND", this, player.UserIDString));
            return null;
        }
        private void UnsubscribeHook()
        {
            Unsubscribe(nameof(OnTeamAcceptInvite));
            Unsubscribe(nameof(OnTeamKick));
            Unsubscribe(nameof(OnTeamLeave));
        }
        private void SubscribeHook()
        {
            Subscribe(nameof(OnTeamAcceptInvite));
            Subscribe(nameof(OnTeamKick));
            Subscribe(nameof(OnTeamLeave));
        }
        public void SendChat(BasePlayer player, string Message, string HexColorMSG = "#ffffff", string colortwo = "#fff1351", string CustomAvatar = "", Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, config.settings.ChatPrefix, CustomAvatar, HexColorMSG);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion
    }
}