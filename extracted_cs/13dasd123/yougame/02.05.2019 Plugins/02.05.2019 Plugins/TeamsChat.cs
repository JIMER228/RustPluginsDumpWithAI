using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("TeamsChat", "Paranormal", "0.1.0")]
      //  Слив плагинов server-rust by Apolo YouGame
    [Description("Enable private chat for player teams")]
    class TeamsChat : RustPlugin
    {
        #region TeamAPI
        private RelationshipManager.PlayerTeam GetPlayerTeam(BasePlayer player)
        {
            RelationshipManager.PlayerTeam playerTeam = RelationshipManager._instance.FindTeam(player.currentTeam);

            return playerTeam ?? null;
        }

        private List<ulong> GetPlayerTeammate(BasePlayer player)
        {
            if (player.currentTeam == 0)
                return null;
            return RelationshipManager._instance.FindTeam(player.currentTeam).members.ToList();
        }
        #endregion

        #region Helpers and Commands
        public void Broadcast(BasePlayer player, string message)
        {
            foreach (var teammate in GetPlayerTeammate(player))
            {
                var teamm = BasePlayer.FindByID(teammate);
                if (teamm != null)
                    teamm.ChatMessage($"<color={config.ChatPrefixColor}>{config.ChatPrefix}</color> {message}");
            }
        }

        void CmdChatTeamCommand(BasePlayer player, string command, string[] args)
        {
            var playerTeam = GetPlayerTeam(player);
            if (playerTeam == null)
            {
                player.ChatMessage(LangMessages("NotTeam", player));
                return;
            }
            if (playerTeam != null)
            {
                var status = playerTeam.GetLeader() == player ? $"({LangMessages("Leader", player)})" : $"({LangMessages("Member", player)})";
                Broadcast(player, $"<color={config.PlayerNameColor}>{player.displayName} {status}:</color> {string.Join(" ", args)}");
                return;
            }
        }
        #endregion

        #region Configuration and Lang
        private string LangMessages(string key, BasePlayer player = null) => lang.GetMessage(key, this, player?.UserIDString);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                {"NotTeam", "You do not have a team, first create it!\n<size=11>You can create it in the lower left corner of the inventory</size>"},
                {"Leader", "Leader"},
                {"Member", "Member" }
            }
           , this, "en");

            lang.RegisterMessages(new Dictionary<string, string>()
            {
                {"NotTeam", "У вас нету команды, сначала создайте её!\n<size=11>Создать её можно в левом нижнем углу инвентаря</size>"},
                {"Leader", "Лидер"},
                {"Member", "Участник" }
            }
            , this, "ru");
        }

        private class PluginConfig
        {
            [JsonProperty("Chat command")]
            public string Command;

            [JsonProperty("Player name color (#hex)")]
            public string PlayerNameColor;

            [JsonProperty("Chat-prefix color (#hex)")]
            public string ChatPrefixColor;

            [JsonProperty("Chat-prefix")]
            public string ChatPrefix;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    Command = "/tchat",
                    PlayerNameColor = "#48B6FF",
                    ChatPrefixColor = "#AADDFF",
                    ChatPrefix = "[Team]"
                };
            }
        }
        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private void OnServerInitialized()
        {
            cmd.AddChatCommand(config.Command.Replace("/", string.Empty), this, CmdChatTeamCommand);
            foreach(var player in BasePlayer.activePlayerList)
            {
                Broadcast(player, $"<color={config.PlayerNameColor}>Paranormal ({LangMessages("Leader", player)}):</color> This test messages");
                Broadcast(player, $"<color={config.PlayerNameColor}>Green ({LangMessages("Member", player)}):</color> This test reply");

            }
        }

        #endregion
    }
}
