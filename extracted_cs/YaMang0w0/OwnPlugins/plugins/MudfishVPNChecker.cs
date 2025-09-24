using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Mudfish VPN Checker", "YaMang -w-", "1.0.5")]
    [Description("Handles VPN using Mudfish VPN.")]
    public class MudfishVPNChecker : CovalencePlugin
    {
        private string mudfishAPIurl = "http://mudfish.net/api/staticnodes";
        private MudfishVPNClass _vpnCache = new MudfishVPNClass();
        private string bypassPerm = "mudfishvpnchecker.bypass";

        void OnServerInitialized(bool initial)
        {
            GetMudfish();

            timer.Every(60, () =>
            {
                GetMudfish();
            });
        }

        void OnUserConnected(IPlayer player)
        {
            if (permission.UserHasPermission(player.Id, bypassPerm)) return;

            if (_vpnCache == null) return;

            var find = _vpnCache.staticnodes.FirstOrDefault(x => x.ip == player.Address);

            if (find == null) return;

            switch (_config.generalSettings.punishment)
            {
                case Punishment.Kick:
                    player.Kick(Lang("Reason", player.Id));
                    break;
                case Punishment.Ban:
                    player.Ban(Lang("Reason", player.Id));
                    break;
                case Punishment.CustomCommands:
                    foreach (var command in _config.generalSettings.CustomCommands)
                    {
                        string cm = command;
                        cm = cm.Replace("$player.id", player.Id);

                        ConsoleSystem.Run(ConsoleSystem.Option.Server, command);
                    }
                    break;
            }
        }
        #region Helper
        private void GetMudfish()
        {
            webrequest.Enqueue(mudfishAPIurl, null, (code, response) =>
            {
                if (code != 200 || response == null)
                {
                    PrintWarning($"Couldn't get an answer from mudfish!");
                    return;
                }

                MudfishVPNClass vpnCheck = JsonConvert.DeserializeObject<MudfishVPNClass>(response);

                if (vpnCheck == null) return;
                else
                {
                    _vpnCache = vpnCheck;
                    Puts("Updated Mudfish Cache");
                }

            }, this, RequestMethod.GET);
        }
        #endregion
        #region Config        
        private ConfigData _config;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "General Settings")] public GeneralSettings generalSettings { get; set; }
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<ConfigData>();

            if (_config.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(_config, true);
        }

        protected override void LoadDefaultConfig() => _config = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                generalSettings = new GeneralSettings
                {
                    punishment = 0,
                    CustomCommands = new List<string>
                    {
                        "kick $player.id reason"
                    }
                },
                Version = Version
            };
        }

        public class GeneralSettings
        {
            [JsonProperty(PropertyName = "Punishment (0 - none, 1 - kick, 2 - ban, 3 - custmom commands)", Order = 2)] public Punishment punishment { get; set; }
            [JsonProperty(PropertyName = "Custom Commands", Order = 3)] public List<string> CustomCommands { get; set; }
        }

        public enum Punishment
        {
            None,
            Kick,
            Ban,
            CustomCommands
        }

        protected override void SaveConfig() => Config.WriteObject(_config, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");
            _config.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Lang

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "Reason", "Mudfish VPN not allowed" }

            }, this);
        }

        private string Lang(string key, string playerId, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this, playerId), args);
        }

        #endregion

        #region Class
        public class MudfishVPNClass
        {
            public List<Staticnode> staticnodes { get; set; }
            public int status { get; set; }
        }

        public class Staticnode
        {
            public int commercial_node { get; set; }
            public string hostname { get; set; }
            public string ip { get; set; }
            public string location { get; set; }
            public int sid { get; set; }
        }
        #endregion
    }
}
