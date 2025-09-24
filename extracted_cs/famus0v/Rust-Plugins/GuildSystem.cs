using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("GuildSystem", "Kira", "1.0.0")]
    public class GuildSystem : RustPlugin
    {
        #region [Vars] / [Переменные]

        [PluginReference] private Plugin RaidSystem;
        private StoredData _dataBase = new StoredData();
        private RelationshipManager.PlayerTeam Team1;
        private RelationshipManager.PlayerTeam Team2;
        private Vector3 pos1 = new Vector3(-1943.8f, 5.2f, 1456.6f);
        private Vector3 pos2 = new Vector3(1307.4f, 6.5f, -1385.0f);
        private const string UIParent = "UI.GuildSystem";

        #endregion

        #region [Lang]

        protected override void LoadDefaultMessages()
        {
            var ru = new Dictionary<string, string>
            {
                ["OCG"] = "'ОПГ' БЕРЁЗА",
                ["WSARMY"] = "АРМИЯ WS",
                ["JOIN"] = "Вы вошли в гильдию :"
            };

            var en = new Dictionary<string, string>
            {
                ["OCG"] = "'OPG' BIRCH",
                ["WSARMY"] = "WS ARMY",
                ["JOIN"] = "You have entered the guild :"
            };
            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        #endregion

        #region [UI] / [Отрисовка UI]

        private void DrawUI_Guild(BasePlayer player)
        {
            var ui = new CuiElementContainer();
            ui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = "0.13 0.13 0.15 0.99"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", UIParent);

            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0.18 0.18 0.20 1.00",
                    Close = UIParent,
                    Command = "guildsystem 2"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 14,
                    Text = lang.GetMessage("WSARMY", this, player.UserIDString)
                },
                RectTransform =
                {
                    AnchorMin = "0.3609375 0.4537037",
                    AnchorMax = "0.4860938 0.5462964"
                }
            }, UIParent);

            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0.18 0.18 0.20 1.00",
                    Close = UIParent,
                    Command = "guildsystem 1"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 14,
                    Text = lang.GetMessage("OCG", this, player.UserIDString)
                },
                RectTransform =
                {
                    AnchorMin = "0.5139062 0.4537037",
                    AnchorMax = "0.6390625 0.5462964"
                }
            }, UIParent);

            CuiHelper.DestroyUi(player, UIParent);
            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Hooks] / [Крюки]

        private void UpdateTeam(BasePlayer player, string guild)
        {
            player.ClearTeam();
            player.TeamUpdate();
            if (_dataBase.TeamPlayers1.ContainsKey(player.userID))
            {
                Team1.RemovePlayer(player.userID);
                _dataBase.TeamPlayers1.Remove(player.userID);
            }

            if (_dataBase.TeamPlayers2.ContainsKey(player.userID))
            {
                Team2.RemovePlayer(player.userID);
                _dataBase.TeamPlayers2.Remove(player.userID);
            }

            switch (guild.ToLower())
            {
                case "ocgbirch":
                    player.ChatMessage(
                        $"{lang.GetMessage("JOIN", this, player.UserIDString)} {lang.GetMessage("OCG", this, player.UserIDString)}");
                    _dataBase.TeamPlayers1.Add(player.userID, new StoredData.TeamPlayer
                    {
                        DisplayName = player._name,
                        SteamID = player.userID
                    });
                    NextFrame(() => Team1.AddPlayer(player));

                    if (!permission.UserHasPermission(player.UserIDString, $"{Name}.ocgbirch"))
                        permission.GrantUserPermission(player.UserIDString, $"{Name}.ocgbirch", this);
                    if (permission.UserHasPermission(player.UserIDString, $"{Name}.wsarmy"))
                        permission.RevokeUserPermission(player.UserIDString, $"{Name}.wsarmy");
                    break;
                case "wsarmy":
                    player.ChatMessage(
                        $"{lang.GetMessage("JOIN", this, player.UserIDString)} {lang.GetMessage("WSARMY", this, player.UserIDString)}");
                    _dataBase.TeamPlayers2.Add(player.userID, new StoredData.TeamPlayer
                    {
                        DisplayName = player._name,
                        SteamID = player.userID
                    });
                    NextFrame(() => Team2.AddPlayer(player));
                    if (!permission.UserHasPermission(player.UserIDString, $"{Name}.wsarmy"))
                        permission.GrantUserPermission(player.UserIDString, $"{Name}.wsarmy", this);
                    if (permission.UserHasPermission(player.UserIDString, $"{Name}.ocgbirch"))
                        permission.RevokeUserPermission(player.UserIDString, $"{Name}.ocgbirch");
                    break;
            }


            player.TeamUpdate();
        }

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            LoadData();
            RelationshipManager.ServerInstance.playerToTeam.Clear();
            CreateTeams();
            if (!permission.PermissionExists($"{Name}.ocgbirch"))
                permission.RegisterPermission($"{Name}.ocgbirch", this);
            if (!permission.PermissionExists($"{Name}.wsarmy"))
                permission.RegisterPermission($"{Name}.wsarmy", this);
            ServerMgr.Instance.StartCoroutine(RecoverTeams());
        }

        private void CreateTeams()
        {
            Team1 = RelationshipManager.ServerInstance.CreateTeam();
            Team1.SetTeamLeader(1);
            Team1.teamName = "OCGBirch";
            Team2 = RelationshipManager.ServerInstance.CreateTeam();
            Team2.SetTeamLeader(2);
            Team2.teamName = "WSARMY";
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            SaveData();
            foreach (var player in BasePlayer.activePlayerList) player.ClearTeam();
        }

        private IEnumerator RecoverTeams()
        {
            yield return new WaitForSeconds(1f);
            foreach (var tp in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(tp, UIParent);
                tp.ClearTeam();

                if (_dataBase.TeamPlayers1.ContainsKey(tp.userID))
                {
                    Team1.AddPlayer(tp);
                    tp.TeamUpdate();
                    tp.SendNetworkUpdate();
                }

                if (_dataBase.TeamPlayers2.ContainsKey(tp.userID))
                {
                    Team2.AddPlayer(tp);
                    tp.TeamUpdate();
                    tp.SendNetworkUpdate();
                }

                if (tp.Team == null)
                    DrawUI_Guild(tp);
            }

            yield return 0;
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private object OnTeamLeave(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            return true;
        }

        // ReSharper disable once UnusedMember.Local
        private object OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            if (!(attacker != null) || !(info.HitEntity is BasePlayer)) return null;
            if (info.HitEntity.IsNpc || attacker.IsNpc) return null;
            var player1 = attacker.ToPlayer();
            var player2 = info.HitEntity.ToPlayer();
            if (!player1.Team.members.Contains(player2.userID)) return null;
            if (player1.userID == player2.userID) return null;
            return false;
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerConnected(BasePlayer player)
        {
            if (_dataBase.TeamPlayers1.ContainsKey(player.userID) ||
                _dataBase.TeamPlayers2.ContainsKey(player.userID)) return;
            if (player.Team == null) NextFrame(() => DrawUI_Guild(player));
        }

        // ReSharper disable once UnusedMember.Local
        private object OnPlayerRespawn(BasePlayer player)
        {
            if (_dataBase.TeamPlayers1.ContainsKey(player.userID)) NextTick((() => player.Teleport(pos1)));
            if (_dataBase.TeamPlayers2.ContainsKey(player.userID)) NextTick((() => player.Teleport(pos2)));
            return null;
        }

        #endregion

        #region [Commands]

        [ConsoleCommand("guildsystem")]
        // ReSharper disable once UnusedMember.Local
        private void AddGuild1(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            player.ClearTeam();
            if (player.Team != null)
            {
                CuiHelper.DestroyUi(player, UIParent);
                return;
            }

            switch (args.Args[0])
            {
                case "1":
                    if (_dataBase.TeamPlayers1.ContainsKey(player.userID)) return;
                    if (Team1.members.Contains(player.userID)) return;
                    Team1.AddPlayer(player);
                    player.ChatMessage(
                        $"{lang.GetMessage("JOIN", this, player.UserIDString)} {lang.GetMessage("OCG", this, player.UserIDString)}");
                    _dataBase.TeamPlayers1.Add(player.userID, new StoredData.TeamPlayer
                    {
                        DisplayName = player.displayName,
                        SteamID = player.userID
                    });
                    break;
                case "2":
                    if (_dataBase.TeamPlayers2.ContainsKey(player.userID)) return;
                    if (Team2.members.Contains(player.userID)) return;
                    Team2.AddPlayer(player);
                    player.ChatMessage(
                        $"{lang.GetMessage("JOIN", this, player.UserIDString)} {lang.GetMessage("WSARMY", this, player.UserIDString)}");
                    _dataBase.TeamPlayers2.Add(player.userID, new StoredData.TeamPlayer
                    {
                        DisplayName = player.displayName,
                        SteamID = player.userID
                    });
                    break;
            }
        }

        [ConsoleCommand("guildsystem.move")]
        // ReSharper disable once UnusedMember.Local
        private void UpdateGuild(ConsoleSystem.Arg args)
        {
            if (args.Player() != null) return;
            var player = BasePlayer.Find(args.Args[0]);
            if (player == null)
            {
                PrintWarning("Игрок не найден");
                return;
            }

            switch (args.Args[1].ToLower())
            {
                case "ocgbirch":
                    UpdateTeam(player, "ocgbirch");
                    break;
                case "wsarmy":
                    UpdateTeam(player, "wsarmy");
                    break;
            }
        }

        #endregion

        #region [DataBase] / [База данных]

        public class StoredData
        {
            public Dictionary<ulong, TeamPlayer> TeamPlayers1 = new Dictionary<ulong, TeamPlayer>();
            public Dictionary<ulong, TeamPlayer> TeamPlayers2 = new Dictionary<ulong, TeamPlayer>();

            public class TeamPlayer
            {
                public string DisplayName;
                public ulong SteamID;
            }
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

        #endregion

        #region [API]

        // ReSharper disable once UnusedMember.Local
        private List<ulong> GetGuildPlayers(string guild)
        {
            var list = new List<ulong>();
            switch (guild)
            {
                case "OCGBirch":
                    list.AddRange(_dataBase.TeamPlayers1.Select(d => d.Key));
                    break;
                case "WSARMY":
                    list.AddRange(_dataBase.TeamPlayers2.Select(d => d.Key));
                    break;
            }

            return list;
        }

        // ReSharper disable once UnusedMember.Local
        private bool GetGuildPlayer(ulong id, ulong skinid)
        {
            var y = false;
            switch (skinid)
            {
                case 3001:
                    if (_dataBase.TeamPlayers1.ContainsKey(id)) y = true;
                    break;
                case 3002:
                    if (_dataBase.TeamPlayers2.ContainsKey(id)) y = true;
                    break;
            }

            return y;
        }

        // ReSharper disable once UnusedMember.Local
        private bool GetGuildPlayerRaid(ulong id, string guild)
        {
            var y = false;
            switch (guild)
            {
                case "OCGBirch":
                    if (_dataBase.TeamPlayers1.ContainsKey(id)) y = true;
                    break;
                case "WSARMY":
                    if (_dataBase.TeamPlayers2.ContainsKey(id)) y = true;
                    break;
            }

            return y;
        }

        #endregion
    }
}