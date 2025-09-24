using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("No Proxy", "Jacob", "1.0.0")]
    internal class NoProxy : RustPlugin
    {
        #region Configuration

        private class Configuration
        {
            public string Contact = "name@example.com";
            public bool Kick = true;

            public bool UseSlack;
            public string SlackChannel = "#vpn";

            public Configuration()
            {
                GetConfig(ref Contact, "General settings", "Contact email");
                GetConfig(ref UseSlack, "Notification settings", "Use Slack");
                GetConfig(ref SlackChannel, "Notification settings", "Slack channel");
                GetConfig(ref Kick, "General settings", "Kick");

                instance.SaveConfig();
            }

            private void GetConfig<T>(ref T variable, params string[] path)
            {
                if (path.Length == 0) return;

                if (instance.Config.Get(path) == null)
                {
                    SetConfig(ref variable, path);
                    instance.PrintWarning($"Added field to config: {string.Join("/", path)}");
                }

                variable = (T)Convert.ChangeType(instance.Config.Get(path), typeof(T));
            }

            private void SetConfig<T>(ref T variable, params string[] path) => instance.Config.Set(path.Concat(new object[] { variable }).ToArray());
        }

        protected override void LoadDefaultConfig() => PrintWarning("Generating new config file...");

        #endregion

        #region Fields

        private static NoProxy instance;

        private Configuration configuration;

        [PluginReference] private Plugin Slack;

        #endregion

        #region Localization 

        protected override void LoadDefaultMessages() => lang.RegisterMessages(new Dictionary<string, string>
        {
            {"Denied", "Sorry, you may not play on this server while using a VPN."},
            {"Slack Flag", ">*{0}* _is_ using a VPN. :warning:"}
        }, this);

        #endregion

        #region Methods

        private void CheckIP(BasePlayer player) => webrequest.EnqueueGet($"http://check.getipintel.net/check.php?ip={FormatIP(player.Connection.ipaddress)}&contact={configuration.Contact}&format=json&flags=m",
            (code, response) =>
            {
                if (code != 200 || string.IsNullOrEmpty(response)) return;
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(response)["result"];
#if DEBUG
                Puts($"check.getipintel.net for {player.displayName} ({FormatIP(player.Connection.ipaddress)}): {result}.");
#endif
                if (result != "1" || permission.UserHasPermission(player.UserIDString, "noproxy.exempt")) return;
                webrequest.EnqueueGet($"http://legacy.iphub.info/api.php?ip={FormatIP(player.Connection.ipaddress)}&showtype=4",
                    (anotherCode, anotherResponse) =>
                    {
                        if (code != 200 || string.IsNullOrEmpty(response)) return;
                        var anotherResult = JsonConvert.DeserializeObject<Dictionary<string, string>>(anotherResponse)["proxy"];
#if DEBUG
                        Puts($"legacy.iphub.info for {player.displayName} ({FormatIP(player.Connection.ipaddress)}): {anotherResult}.");
#endif
                        if (anotherResult != "1") return;
                        if (configuration.UseSlack) Slack?.CallHook("Message", lang.GetMessage("Slack Flag", this), configuration.SlackChannel);
                        if (!configuration.Kick) return;
                        Log($"{player.displayName} ({player.userID}) was kicked.");
                        player.Kick(lang.GetMessage("Denied", this, player.UserIDString));
                    }, this);

            }, this);

        private string FormatIP(string IP) => IP.Split(':')[0];

        private void Log(string message, params object[] args) => LogToFile("", string.Format($"[{DateTime.UtcNow}] {message}", args), this, false);

        #endregion

        #region Oxide Hooks

        private void OnServerInitialized()
        {
            instance = this;
            configuration = new Configuration();
            permission.RegisterPermission("noproxy.exempt", this);

            foreach (var player in BasePlayer.activePlayerList.Where(x => !permission.UserHasPermission(x.UserIDString, "noproxy.exempt")))
                CheckIP(player);
        }

        private void OnPlayerInit(BasePlayer player) => CheckIP(player);

        #endregion

    }
}
