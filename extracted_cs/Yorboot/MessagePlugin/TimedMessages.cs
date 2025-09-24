using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using System;


namespace Oxide.Plugins
{
    [Info("Timed messages", "Royboot", "1.0.3")]
    [Description("Timed messages allows for customizable timed messages, and includes a admin broadcast command")]
    class TimedMessages : CovalencePlugin
    {
        private static PluginConfig? _config;
        private List<float>? _intervals;
        private List<List<string>>? _colors;
        private List<List<string>>? _messages;
        private List<MessageConfig>? _messageConfigLists;
        private static bool _isTimerRunning;
        private bool _isMessageRedAdminBroadcast;
        private int _adminBroadCastFontSize;
        private bool _isAdminBroadCastBold;
        private bool _isAdminBroadCastUnderlined;
        private bool _isAdminBroadcastItalic;
        private void Init()
        {
            //Register the permission needed for the broadcast command
            permission.RegisterPermission("timedmessages.admin", this);
            SetIsTimerRunning(false);
            //Load config values
            LoadConfig();
            _intervals = _config.Intervals;
            _colors = _config.Colors;
            _messages = _config.Messages;
            _messageConfigLists = _config.MessageConfigLists;
            _isAdminBroadcastItalic = _config.IsAdminBroadcastItalic;
            _isAdminBroadCastBold = _config.IsAdminBroadCastBold;
            _isMessageRedAdminBroadcast = _config.IsMessageRedAdminBroadcast;
            _adminBroadCastFontSize = _config.AdminBroadCastFontSize;
            _isAdminBroadCastUnderlined = _config.IsAdminBroadCastUnderlined;
            //do null checks to make sure everything is in order to start broadcasting messages
            NullChecks();
            StartTimers();
        }
        //set the state to false to prevent memory leaks
        private void OnServerShutdown()
        {
           SetIsTimerRunning(false);
        }
        //define getters and setters to cache the _isTimerRunning var
        private void SetIsTimerRunning(bool value)
        {
            _isTimerRunning = value;
        }

        private bool GetIsTimerRunning()
        {
            return _isTimerRunning;
        }
        //custom function to add in customizations to the Broadcasted messages
        private void BroadcastWipeMessage(List<string> messages,List<string> colors,MessageConfig messageConfig)
        {
            if (messageConfig == null)
            {
                throw new ArgumentNullException($"MessageConfig is null please check your config");
            }
            List<string> finalMessages = new List<string>();
            for (int i = 0; i < messages.Count; i++)
            {
                if (i < colors.Count)
                {
                    finalMessages.Add(ConvertStyleToStringBroadCast(messages[i],colors[i],messageConfig));
                }
            }

            if (finalMessages.Count > 0)
            {
                foreach (var message in finalMessages)
                {
                    server.Broadcast(message);
                }
            }else
            {
                Puts("There are no final messages to broadcast");
            }
        }
        

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config= Config.ReadObject<PluginConfig>();

            if (_config == null)
            {
                Puts("Failed to load configuration. loading default config");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }
    
        //method to start running all timers on multiple different intervals
        private void StartTimers()
        {
            if (_intervals?.Count != _messages?.Count)
            {
                throw new ArgumentException("Invalid number of intervals");
            }

            if (_messageConfigLists?.Count != _messages?.Count)
            {
                throw new ArgumentException("Invalid number of Configuration sets");
            }
            //a check to make sure this function does not get set multiple times
            if (!GetIsTimerRunning())
            {
                Puts("Starting timers");
                for (int i = 0; i < _intervals.Count; i++)
                {
                    if (i < _messages.Count && i <_colors.Count)
                    {
                        int index = i;
                        timer.Every(_intervals[index], () =>
                        {
                            BroadcastWipeMessage(_messages[index],_colors[index],_messageConfigLists[index]);
                        });
                    }
                }
                SetIsTimerRunning(true);
            }
        }
        
        private void NullChecks()
        {
            //run null checks to make sure no lists are left empty else throw a error with wich list is empty
            string failedCheck = null;

            if (_intervals.Count == 0) failedCheck = "_intervals";
            else if (_colors.Count == 0) failedCheck = "_colors";
            else if (_messages.Count == 0) failedCheck = "_messages";
            else if (_messages[0].Count == 0) failedCheck = "_messages[0]";
            else if(_messageConfigLists?.Count == 0) failedCheck = "_configLists";

            if (failedCheck != null)
            {
                throw new Exception($"Config option {failedCheck} has failed. Please check config.");
            }
        }
        private static PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                Messages = new List<List<string>>()
                {
                    new List<string>() {
                        "[Wipe Reminder] Server wipes every Friday at 5PM EST!",
                        "Join our Discord: <u><size=16>discord link here</size></u>",
                    },
                },
                Colors = new List<List<string>>()
                {
                    new List<string>()
                    {
                        "#ffa500",
                        "#00ffff",
                    }
                },
                Intervals = new List<float>()
                {
                    60.0f,
                },
                MessageConfigLists = new List<MessageConfig>()
                {
                    new MessageConfig()
                    {
                        broadcastMessageFontSize = 12,
                        makeMessagesItalic = false,
                        makeMessagesBold = false,
                        makeMessagesUnderlined = false,
                    }
                },
                IsAdminBroadcastItalic = false,
                IsAdminBroadCastBold = false,
                IsAdminBroadCastUnderlined = false,
                IsMessageRedAdminBroadcast = false,
                AdminBroadCastFontSize = 12,
            };
        }

        private string ConvertStyleToStringAdminBroadCast(string message)
        {
            // Apply the red text color
            if (_isMessageRedAdminBroadcast)
            {
                message = $"<color=#ff0000>{message}</color>";
            }

            // Apply an underline
            if (_isAdminBroadCastUnderlined)
            {
                message = $"<u>{message}</u>";
            }

            // Apply the bold effect
            if (_isAdminBroadCastBold)
            {
                message = $"<b>{message}</b>";
            }

            // Apply the italic styling effect
            if (_isAdminBroadcastItalic)
            {
                message = $"<i>{message}</i>";
            }

            // Apply the font size
            if (_adminBroadCastFontSize > 0)
            {
                message = $"<size={_adminBroadCastFontSize}>{message}</size>";
            }

            return message;
        }
        private string ConvertStyleToStringBroadCast(string message,string color,MessageConfig config)
        {
            //apply color
            if (color != string.Empty || color != "<color=>")
            {
                message = $"<color={color}>{message}</color>";
            }
            // Apply an underline
            if (config.makeMessagesUnderlined)
            {
                message = $"<u>{message}</u>";
            }

            // Apply the bold effect
            if (config.makeMessagesBold)
            {
                message = $"<b>{message}</b>";
            }

            // Apply the italic styling effect
            if (config.makeMessagesItalic)
            {
                message = $"<i>{message}</i>";
            }

            // Apply the font size
            if (_adminBroadCastFontSize > 0)
            {
                message = $"<size={config.broadcastMessageFontSize}>{message}</size>";
            }

            return message;
        }
        [Command("Broadcast")]
        private void BroadCast(IPlayer sender, string command, string[] args)
        {
            if (!sender.HasPermission("timedmessages.admin"))
            {
                sender.Message("You do not have permission to use this command this is a admin only command.");
                return;
            }
            var playerlist = BasePlayer.activePlayerList;
            string message = string.Join(" ", args);
            message = ConvertStyleToStringAdminBroadCast(message);
            if (args.Length == 0)
            {
                sender.Message("Usage: /broadcast <message>");
                return;
            }
            foreach (var player in playerlist)
            {
                if (player != null)
                {
                    if (args.Length > 0)
                    {
                        Puts($"message sent succesfuly to {player.displayName} message: {message}");
                        player.ChatMessage(message);
                    }
                    sender.Message($"Message send succesful {message}");
                }
            }
        }
    }
    

    public class PluginConfig
    {
        public List<MessageConfig> MessageConfigLists { get; set; }
        public List<List<string>> Messages { get; set; } = new List<List<string>>();
        public List<List<string>> Colors { get; set; } = new List<List<string>>();
        public List<float> Intervals { get; set; } = new List<float>();
        public bool IsMessageRedAdminBroadcast { get; set; } = false;
        public int AdminBroadCastFontSize { get; set; } = 12;
        public bool IsAdminBroadCastBold { get; set; } = false;
        public bool IsAdminBroadCastUnderlined { get; set; } = false;
        public bool IsAdminBroadcastItalic { get; set; } = false;
    }

    public class MessageConfig
    {
        public bool makeMessagesBold { get; set; }
        public bool makeMessagesUnderlined { get; set; }
        public bool makeMessagesItalic { get; set; }
        public int broadcastMessageFontSize { get; set; }
    }
}