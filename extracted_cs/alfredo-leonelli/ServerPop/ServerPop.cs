using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("ServerPop", "AnotherPanda", "1.0.0")]
    [Description("Shows in chat how many players and optionally how many admins are connected.")]

    public class ServerPop : CovalencePlugin
    {
        private List<string> populationCommands = new List<string>();
        private bool showAdminCount = true;

        #region Configuration

        protected override void LoadDefaultConfig()
        {
            Config["PopCommands"] = new List<string> { "pop", "players" };
            Config["ShowAdminCount"] = true;
            SaveConfig();
        }

        private void LoadConfigValues()
        {
            populationCommands = GetConfig("PopCommands", new List<string> { "pop", "players" });
            showAdminCount = GetConfig("ShowAdminCount", true);
            SaveConfig();
        }

        private T GetConfig<T>(string key, T defaultValue)
        {
            if (Config[key] == null)
            {
                Config[key] = defaultValue;
                return defaultValue;
            }

            try
            {
                return (T)Convert.ChangeType(Config[key], typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        private List<string> GetConfig(string key, List<string> defaultValue)
        {
            if (Config[key] is List<object> objList)
            {
                return objList.Select(o => o.ToString()).ToList();
            }

            Config[key] = defaultValue;
            return defaultValue;
        }

        #endregion

        #region Hooks

        private void Init()
        {
            LoadConfigValues();

            foreach (var cmd in populationCommands)
                AddCovalenceCommand(cmd.ToLower(), nameof(CommandShowPopulation));
        }

        private void CommandShowPopulation(IPlayer player, string command, string[] args)
        {
            int totalPlayers = players.Connected.Count();
            string message = $"There are {totalPlayers} player(s) connected to the server.";

            if (showAdminCount)
            {
                int adminCount = players.Connected.Count(p => p.IsAdmin);
                message += $" Admin(s) online: {adminCount}";
            }

            server.Broadcast(message);
        }

        #endregion
    }
}
