using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Easy Chat Commands", "Jake Rich", "1.0.0")]
    [Description("Easy add custom info commands")]

    class EasyChatCommands : RustPlugin
    {
        public Dictionary<string, string> chatCommands { get; set; }
        private const string configFileName = "EasyChatCommands";

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            Config.Clear();
            chatCommands = new Dictionary<string, string>();
            chatCommands.Add("examplemultilinecommand", "Use \n to create a newline. This is line1\nThis is line2.");
            chatCommands.Add("exampleinfocommand", "Example Reply, You need a comma here -->");
            chatCommands.Add("exampleinfocommand2", "No comma on the last item -->");
            foreach(KeyValuePair<string,string> keyValue in chatCommands)
            {
                Config[keyValue.Key] = keyValue.Value;
            }
            Config.Save();
        }

        void Init()
        {
            try
            {
                chatCommands = Config.ReadObject<Dictionary<string, string>>();
            }
            catch (Exception ex)
            {
                Puts($"ERROR: Config file is formatted incorrectly! EasyChatCommands are NOT LOADED!!!");
                return;
            }
            foreach(string command in chatCommands.Keys)
            {
                cmd.AddChatCommand(command, this, "OnChatInfoCommand");
                Puts("Added a command");
            }
            Puts("Loaded Chat Commands successfully.");
        }

        void OnChatInfoCommand(BasePlayer player, string command)
        {
            string reply;
            if (chatCommands.TryGetValue(command, out reply))
            {
                PrintToChat(player, reply);
            }
        }

    }
}
