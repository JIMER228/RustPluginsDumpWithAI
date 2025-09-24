using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;

namespace Oxide.Plugins
{
    [Info("_Covalence_Template", "Author", "1.0.0")]
    [Description("Simple template for a Covalence (universal) plugin.")]
    public class _Covalence_Template : CovalencePlugin
    {
        private ConfigData FConfig;

        /// <summary>
        /// The config class
        /// </summary>
        private class ConfigData
        {
            [DefaultValue("Covalence")]
            [JsonProperty("Hello Text", DefaultValueHandling = DefaultValueHandling.Populate)]
            public string HelloText { get; set; }
        }

        void Loaded()
        {
            LoadConfig();
        }

        void OnServerInitialized()
        {
            Puts(lang.GetMessage("Hello Text Format", this), FConfig.HelloText);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try {
                FConfig = Config.ReadObject<ConfigData>();

                if (FConfig == null)
                    LoadDefaultConfig();
            } catch {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            FConfig = new ConfigData {
                HelloText = "Covalence"
            };
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string> {
                { "Hello Text Format", "Hello {0} World" }
            }, this, "en");
        }

        protected override void SaveConfig() => Config.WriteObject(FConfig);
    }
}
