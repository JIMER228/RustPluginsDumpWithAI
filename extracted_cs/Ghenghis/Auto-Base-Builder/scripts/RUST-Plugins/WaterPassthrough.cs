// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using HarmonyLib;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;

// Updates
// [1.0.1]
// Bug Fix: Constant values ​​have been moved from the configuration file to the plugin.

namespace Oxide.Plugins
{
    [Info("Water Passthrough", "ArtiOM", "1.0.1")]
    [Description("Change the default water flow from containers to liquid.")]
    internal class WaterPassthrough : RustPlugin
    {

     public Dictionary<string, int> defaultVar = new Dictionary<string, int>() {
            {"waterbarrel", 12},
            {"water.pump.deployed", 12},
            {"poweredwaterpurifier.deployed", 12},
            {"poweredwaterpurifier.storage", 12},
            {"waterpurifier.deployed", 12},
            {"waterpurifier.storage", 12},
            {"water_catcher_large", 12},
            {"water_catcher_small", 6}
            };

     #region Config
        private static ConfigData config;

        private class PassthroughSettings
        {

            [JsonProperty(PropertyName = "Precent(true)/Amount(false)")]
            public bool type;

            [JsonProperty(PropertyName = "Value")]
            public double value;

        }
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Passthrough Settings")]
            public Dictionary<string, PassthroughSettings> passthroughSettings = new Dictionary<string, PassthroughSettings>();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigData
            {
                passthroughSettings = new Dictionary<string, PassthroughSettings>
                {
                    {"waterbarrel", new PassthroughSettings {type = true, value = 1} },
                    {"water.pump.deployed", new PassthroughSettings {type = true, value = 1} },
                    {"poweredwaterpurifier.deployed", new PassthroughSettings {type = true, value = 1} },
                    {"poweredwaterpurifier.storage", new PassthroughSettings {type = true, value = 1} },
                    {"waterpurifier.deployed", new PassthroughSettings {type = true, value = 1} },
                    {"waterpurifier.storage", new PassthroughSettings {type = true, value = 1} },
                    {"water_catcher_large", new PassthroughSettings {type = true, value = 1} },
                    {"water_catcher_small", new PassthroughSettings {type = true, value = 1} },
                }
            };
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        #endregion
     #region Hooks
        void Init()
        {
            LoadConfig();
        }

        void OnEntitySpawned(LiquidContainer container)
        {
            if(config.passthroughSettings.ContainsKey(container.ShortPrefabName))
                ChangeLiquidContainer(container);
        }

        void OnServerInitialized(bool initial)
        {
            Puts("Loading liquid container configuration! The server may be lagging!");

            int counter = 0;
            foreach (LiquidContainer container in BaseNetworkable.FindObjectsOfType<LiquidContainer>())
            {
                if (config.passthroughSettings.ContainsKey(container.ShortPrefabName))
                {
                    if(ChangeLiquidContainer(container))
                        counter++;
                }
#if DEBUG
                PrintWarning($"{container.ShortPrefabName}, {container.autofillTickAmount}, x{container.autofillTickRate}");
#endif
            }
            Puts($"Approved changes in {counter} containers.");
        }

        bool ChangeLiquidContainer(LiquidContainer container)
        {
            PassthroughSettings setup;
            if (config.passthroughSettings.TryGetValue(container.ShortPrefabName, out setup))
            {
                if(defaultVar.TryGetValue(container.ShortPrefabName, out int value))
                {
                    value = value * 2;
                    if (setup.type)
                    {
                        if (container.autofillTickAmount == (int)(setup.value * value))
                            return false;

                        container.autofillTickAmount = (int)(setup.value * value);
                        container.maxOutputFlow = (int)(container.autofillTickAmount / container.autofillTickRate);
                    }
                    else
                    {
                        if (container.autofillTickAmount == (int)setup.value)
                            return false;

                        container.autofillTickAmount = (int)setup.value;
                        container.maxOutputFlow = (int)(setup.value / container.autofillTickRate);
                    }   
                }
                else
                {
                    Puts("Const value is not set by plugin author! Contact to ArtiIOMI");
                }
            }
            return true;
        }
     #endregion
    }
}
 