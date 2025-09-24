using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Watchdog", "KajWithAJ", "1.2.0")]
    public class Watchdog : RustPlugin
    {
        private string webhookUrl = "";

        private void OnServerInitialized()
        {
            timer.Every(30f, () => PushUptime());
        }

        void Loaded() {
            LoadConfig();
        }

        protected override void LoadDefaultConfig() {
            Puts("Generating default config file");
            Config.Clear();
            Config["webhookUrl"] = "";
            SaveConfig();
        }

        void LoadConfig() {
            this.webhookUrl = (string)Config["webhookUrl"];
        }

        private void PushUptime()
        {
            if (webhookUrl != "") {
                var serverFPS = Performance.report.frameRate;
                var urlWithFPS = $"{webhookUrl}{serverFPS}";
                webrequest.EnqueueGet(urlWithFPS, (code, response) =>
                {
                    if (code != 200)
                    {
                        Puts($"Error while pushing uptime. HTTP Response Code: {code}. Message: {response}");
                    }
                }, this);
            } else {
                Puts($"Error while pushing uptime, webhookUrl is not configured in config");
            }
            
        }
    }
}
