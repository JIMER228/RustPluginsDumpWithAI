using System.Collections.Generic;


namespace Oxide.Plugins
{
    //Define:FileOrder=2
    public partial class GatherRewards
    {
     
        private PluginConfig _config;

        PluginConfig DefaultConfig()
        {
            var defaultConfig = new PluginConfig
            {
                Settings = new PluginSettings
                {
                    ChatEditCommand = "gatherrewards",
                    ConsoleEditCommand = "gatherrewards",
                    EditPermission = "gatherrewards.canedit",
                    ShowMessagesOnKill = true,
                    ShowMessagesOnGather = true,
                    UseEconomics = true,
                    UseServerRewards = false,
                    PluginPrefix = "<color=#00FFFF>[GatherRewards]</color>",
                    AwardOnlyOnFullHarvest = false,
                    AddMissingRewards = false,
                    UseUINotify = false,
                    UINotifyMessageType = 1
                },
                Rewards = new Dictionary<string, float>
                {
                    { PluginRewards.Player, 0 },
                    { PluginRewards.PlayerFriend, -25 },
                    { PluginRewards.ClanMember, -25 },
                    { PluginRewards.Ore, 25 },
                    { PluginRewards.Wood, 25 },
                    { PluginRewards.Stone, 25 },
                    { PluginRewards.Corn, 25 },
                    { PluginRewards.Hemp, 25 },
                    { PluginRewards.Mushroom, 25 },
                    { PluginRewards.Pumpkin, 25 },
                    { PluginRewards.TeamMember, -25 },
                    { PluginRewards.Suicide, -100 }
                }
            };

            foreach (GameManifest.PooledString str in GameManifest.Current.pooledStrings)
            {
                if (str.str.StartsWith("assets/rust.ai/agents"))
                {
                    if (str.str.Contains("-") || str.str.Contains("_"))
                    {
                        continue;
                    }

                    if (str.str.Contains("test"))
                    {
                        continue;
                    }

                    if (str.str.Contains("npc"))
                    {
                        continue;
                    }

                    var animal = str.str.Substring(str.str.LastIndexOf('/') + 1).Replace(".prefab", string.Empty);
                    if (animal.Contains("."))
                    {
                        continue;
                    }

                    defaultConfig.Rewards[UppercaseFirst(animal)] = 25;
                }
            }

            defaultConfig.Rewards["Scientist"] = 25;
            defaultConfig.Rewards["Murderer"] = 25;
            return defaultConfig;
        }


        private bool _configChanged;

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(DefaultConfig(), true);
            PrintWarning("New configuration file created.");
        }

        private void LoadConfigValues()
        {
            _config = Config.ReadObject<PluginConfig>();
            var defaultConfig = DefaultConfig();
            Merge(_config.Rewards, defaultConfig.Rewards);
            if (_config.Settings.Version != _version)
            {
                _config.Settings.Version = _version;
                _configChanged = true;
            } 
            if (!_configChanged) return;
            PrintWarning("Configuration file updated.");
            Config.WriteObject(_config);
        }

        private void Merge<T1, T2>(IDictionary<T1, T2> current, IDictionary<T1, T2> defaultDict)
        {
            foreach (var pair in defaultDict)
            {
                if (current.ContainsKey(pair.Key)) continue;
                current[pair.Key] = pair.Value;
                _configChanged = true;
            }
        }

    }
}