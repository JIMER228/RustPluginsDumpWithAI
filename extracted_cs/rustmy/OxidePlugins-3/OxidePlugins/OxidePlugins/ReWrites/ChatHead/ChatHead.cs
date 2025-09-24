using Oxide.Core;
using System.Collections.Generic;
using UnityEngine;


// ReSharper disable once CheckNamespace
namespace Oxide.Plugins
{
    [Info("ChatHead", "LeoCurtss", 0.3)]
    [Description("Displays chat messages above player")]

    // ReSharper disable once UnusedMember.Global
    class ChatHead : RustPlugin
    {
        #region Class Fields
        private StoredData _storedData; //Plugin Data
        private PluginConfig _pluginConfig; //Plugin Config

        private const string UsePermission = "chathead.use";
        private readonly Hash<ulong, Timer> _playerDisplayTimer = new Hash<ulong, Timer>();
        #endregion

        #region Setup & Loading
        // ReSharper disable once UnusedMember.Local
        private void Loaded()
        {
            LoadLang();

            _pluginConfig = ConfigOrDefault(Config.ReadObject<PluginConfig>());
            if (_pluginConfig.Prefix == null) PrintError("Loading config file failed. Using default config");
            else Config.WriteObject(_pluginConfig, true);

            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("Plugin");

            permission.RegisterPermission(UsePermission, this);
            SetupPlayers();
        }

        private void SetupPlayers()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CheckPlayer(player);
            }
            Interface.Oxide.DataFileSystem.WriteObject("ChatHead", _storedData);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Register the lang messages
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private void LoadLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use this command"
            }, this);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// load the default config for this plugin
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
            Config.WriteObject(ConfigOrDefault(null), true);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Uses the values passed in from config. If any values are null it updates them with default values
        /// </summary>
        /// <param name="config">Config that has been loaded or null</param>
        /// <returns>Config using values passed in from config default values</returns>
        /// ////////////////////////////////////////////////////////////////////////
        private PluginConfig ConfigOrDefault(PluginConfig config)
        {
            return new PluginConfig
            {
                Prefix = config?.Prefix ?? "[<color=yellow>Chat Head</color>]",
                DisplayColor = config?.DisplayColor ?? "1 1 1 1",
                DisplayLengthInSeconds = config?.DisplayLengthInSeconds ?? 80,
                MessageFontSize = 25,
                PlayerDistanceLimit = 40
            };
        }
        #endregion

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerChat(ConsoleSystem.Arg arg)
        {
            BasePlayer player = (BasePlayer)arg.connection.player;
            if (player == null) return;
            if (!HasPermission(player, UsePermission)) return;

            PlayerSettings settings = _storedData.PlayerSettings[player.userID];
            if (settings == null || !settings.ShowChatAboveHead) return;
            string message = arg.Args[0];
            
            foreach (BasePlayer onlinePlayer in BasePlayer.activePlayerList)
            {
                DrawChatMessage(player, onlinePlayer, settings, message);
            }
        }

        private void DrawChatMessage(BasePlayer player, BasePlayer onlinePlayer, PlayerSettings settings, string message)
        {
            if (Vector3.Distance(player.transform.position, onlinePlayer.transform.position) > settings.DisplayDistance) return;
            Color color =  ColorEx.Parse(_pluginConfig.DisplayColor);

            onlinePlayer.SendConsoleCommand("ddraw.text", 0.1f, color, player.transform.position + new Vector3(0, 1.9f, 0), $"<size={_pluginConfig.MessageFontSize}>{message}</size>");
            //_playerDisplayTimer[player.userID]?.Destroy();
            _playerDisplayTimer[player.userID] = timer.Repeat(0.1f, _pluginConfig.DisplayLengthInSeconds * 10, () =>
            {
                onlinePlayer.SendConsoleCommand("ddraw.text", 0.1f, color, player.transform.position + new Vector3(0, 1.9f, 0), $"<size={_pluginConfig.MessageFontSize}>{message}</size>");
            });

        }

        #region Chat Command
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        [ChatCommand("chathead")]
        private void ChatHeadChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if(!HasPermission(player, UsePermission))
            {
                Chat(player, Lang("NoPermission", player.UserIDString));
                return;
            }

            CheckPlayer(player);

            if (args.Length == 0)
            {
                SendHelpText(player);
                return;
            }

            switch (args[0].ToLower())
            {
                case "1":
                case "on":
                    _storedData.PlayerSettings[player.userID].ShowChatAboveHead = true;
                    break;

                case "0":
                case "off":
                    _storedData.PlayerSettings[player.userID].ShowChatAboveHead = false;
                    break;

                case "distance":
                    HandleDistance(player, args);
                    break;

            }

            Interface.Oxide.DataFileSystem.WriteObject("ChatHead", _storedData);
            SendHelpText(player);
        }

        private void HandleDistance(BasePlayer player, string[] args)
        {
            if(args.Length != 2)
            {
                SendHelpText(player);
                return;
            }

            int distance;
            if(!int.TryParse(args[1], out distance))
            {
                Chat(player, "Invalid Distance");
                return;
            }

            if(distance > _pluginConfig.PlayerDistanceLimit)
            {
                Chat(player, "Distance cannot be greater than {0}", _pluginConfig.PlayerDistanceLimit);
                return;
            }

            _storedData.PlayerSettings[player.userID].DisplayDistance = distance;
        }

        #endregion

        #region Oxide Hooks
        // ReSharper disable once UnusedMember.Local
        void OnPlayerInit(BasePlayer player)
        {
            CheckPlayer(player);
            Interface.Oxide.DataFileSystem.WriteObject("ChatHead", _storedData);
        }
        #endregion

        #region Helper Methods
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Returns a message to the player with the plugin  prefix and the formated string
        /// </summary>
        /// <param name="player"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// ////////////////////////////////////////////////////////////////////////
        private void Chat(BasePlayer player, string format, params object[] args) => PrintToChat(player, $"{_pluginConfig.Prefix} {format}", args);

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// @credit to Exel80 - code from EasyVote
        /// </summary>
        /// <param name="key"></param>
        /// <param name="id"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        ///  //////////////////////////////////////////////////////////////////////////////////////
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Checks if the user has the given permissions
        /// </summary>
        /// <param name="player">Player to be checked</param>
        /// <param name="perm">Permission to check for</param>
        /// <returns></returns>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        private void CheckPlayer(BasePlayer player)
        {
            if (_storedData.PlayerSettings[player.userID] == null)
            {
                _storedData.PlayerSettings[player.userID] = new PlayerSettings { DisplayDistance = 20, ShowChatAboveHead = true };
            }
        }
        #endregion

        #region Classes
        private class PluginConfig
        {
            public string Prefix { get; set; }
            public string DisplayColor { get; set; }
            public int DisplayLengthInSeconds { get; set; }
            public int MessageFontSize { get; set; }
            public int PlayerDistanceLimit { get; set; }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class StoredData
        {
            public Hash<ulong, PlayerSettings> PlayerSettings = new Hash<ulong, PlayerSettings>();
        }

        private class PlayerSettings
        {
            public float DisplayDistance { get; set; }
            public bool ShowChatAboveHead { get; set; }
        }
        #endregion

        #region Help Text
        private void SendHelpText(BasePlayer player)
        {
            string message = "<color=#ff6961><size=18>ChatHead</size></color>\n" +
                             "<color=#DDDDDD>Command Usage:</color> <color=#ADD8E6>/chathead on/off</color>\n" +
                             "<color=#DDDDDD>Command Usage:</color> <color=#ADD8E6>/chathead distance {distance}</color>\n" +
                             $"ChatHead is {(_storedData.PlayerSettings[player.userID].ShowChatAboveHead ? "<color=#98fb98>Enabled</color>" : "<color=#ff6961>Disabled</color>")}\n" +
                             $"Your ChatHead can be seen from <color=#00ffa5>{_storedData.PlayerSettings[player.userID].DisplayDistance}m</color>";
            PrintToChat(player, message);
        }
        #endregion

    }
}