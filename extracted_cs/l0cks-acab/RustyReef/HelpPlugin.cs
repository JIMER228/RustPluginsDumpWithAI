using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("HelpPlugin", "herbs.acab", "1.0.0")]
    [Description("Displays a list of server features and commands when /help is typed.")]
    public class HelpPlugin : RustPlugin
    {
        private const string DataFileName = "HelpPluginData";
        private HelpData helpData;

        private class HelpData
        {
            public string WelcomeMessage { get; set; } = "Welcome to [color=blue]Rusty Reef[/color]! Below is a list of commands that you can use, be sure to join our discord for announcements and support.";
            public List<string> Commands { get; set; } = new List<string>
            {
                " - - - - - - - - - - - - - - - ",
                "/help - Display this help message",
                "/stats - Check your in-game stats",
                "/lookup playername - Check out someone's stats",
                "/clan - Manage your team",
                "/luckybox - Display the status of the luckybox",
                "- - - - - - - - - - - - - - - - "
            };
        }

        void Init()
        {
            LoadData();
        }

        [ChatCommand("help")]
        private void HelpCommand(BasePlayer player, string command, string[] args)
        {
            ShowHelp(player);
        }

        private void ShowHelp(BasePlayer player)
        {
            var message = helpData.WelcomeMessage + "\n";
            foreach (var command in helpData.Commands)
            {
                message += command + "\n";
            }

            SendReply(player, message);
        }

        private void LoadData()
        {
            helpData = Interface.Oxide.DataFileSystem.ReadObject<HelpData>(DataFileName);
            if (helpData == null)
            {
                helpData = new HelpData();
                SaveData();
            }
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(DataFileName, helpData);
        }

        [ConsoleCommand("helpplugin.reload")]
        private void ReloadData(ConsoleSystem.Arg arg)
        {
            LoadData();
            Puts("HelpPlugin data reloaded.");
        }
    }
}
