// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿// Author:  Kaidoz
// Filename: NoSteamHelper.cs
// Last update: 2019.10.07 2:06

using System.Collections.Generic;
using Network;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("NoSteamHelper", "Kaidoz", "1.0.0")]
    [Description("")]
    internal class NoSteamHelper : RustPlugin
    {
        private static List<DataPlayer> _players = new List<DataPlayer>();

        #region API

        private bool IsPlayerNoSteam(ulong steamid)
        {
            var player = FindPlayer(steamid);

            if (player == null)
            {
                Puts("Player no found");
                return false;
            }

            if (player.IsSteam())
                return false;

            return true;
        }

        #endregion

        private void InitData()
        {
            _players =
                Interface.Oxide.DataFileSystem.ReadObject<List<DataPlayer>>("NoSteamHelper/Players");
        }

        private static void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("NoSteamHelper/Players", _players);
        }

        private DataPlayer FindPlayer(ulong steamid)
        {
            foreach (var player in _players)
                if (player.SteamId == steamid)
                    return player;

            return null;
        }

        public class DataPlayer
        {
            public bool Steam;
            public ulong SteamId;

            public DataPlayer(ulong id, bool steam)
            {
                SteamId = id;
                Steam = steam;
            }

            public static void AddPlayer(ulong id, bool steam)
            {
                _players.Add(new DataPlayer(id, steam));
                SaveData();
            }

            public bool IsSteam()
            {
                return Steam;
            }
        }

        #region Hooks

        private void OnServerInitialized()
        {
            InitData();
        }

        private object OnSteamAuthFailed(Connection connection)
        {
            var dataPlayer = FindPlayer(connection.userid);
            if (dataPlayer != null)
            {
                if (dataPlayer.IsSteam())
                    return false;

                return null;
            }

            DataPlayer.AddPlayer(connection.userid, false);
            return null;
        }

        private void OnPlayerInit(BasePlayer player)
        {
            var dataPlayer = FindPlayer(player.userID);
            if (dataPlayer != null)
                return;

            DataPlayer.AddPlayer(player.userID, true);
        }

        #endregion
    }
}