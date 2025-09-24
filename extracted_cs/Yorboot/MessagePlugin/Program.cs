
namespace Oxide.Plugins
{
    [Info("MessagePlugin", "Royboot", "0.1.0")]
    [Description("Makes epic stuff happen")]
    class MessagePlugin : CovalencePlugin
    {
        private static PluginConfig? _config;
        private readonly List<float> _intervals = _config.Intervals; // 3600 seconds = 1 hour
        private readonly List<string> _colors = _config.Colors;
        private readonly List<string> _messages = _config.Messages;
        private void Init()
        {
            timer.Every(_intervals[0], () =>
            {
                Puts("Message plugin enabled.");
                BroadcastWipeMessage(_messages,_colors);
            });
        }
        private void BroadcastWipeMessage(List<string> messages,List<string> colors)
        {
            
            const string closingTag = "</color>";    
            List<string> finalMessages = new List<string>();
            for (int i = 0; i < messages.Count; i++)
            {
                if (i < colors.Count)
                {
                    string colorTag =  "<color=#" + colors[i] + ">";
                    finalMessages.Add(colorTag+messages[i]+closingTag);
                }
            }
            foreach (var message in finalMessages)
            {
                server.Broadcast(message);
            }
        }
        

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config= Config.ReadObject<PluginConfig>();

            if (_config == null)
            {
                PrintWarning("Configuration file is missing or invalid. Loading default settings.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                Messages = new List<string>()
                {
                    "[Wipe Reminder] Server wipes every Friday at 5PM EST!",
                    "Join our Discord: <u><size=16>https://discord.gg/ZnEVyCtCQv</size></u>",
                },
                Colors = new List<string>()
                {
                    "<color=#ffa500>",
                    "<color=#00ffff>",
                },
                Intervals = new List<float>()
                {
                    3600f,
                }
            };
        }
    }
    

    class PluginConfig
    {
        public List<string> Messages { get; set; }
        public List<string> Colors = new List<string>();
        public List<float> Intervals;
    }
}