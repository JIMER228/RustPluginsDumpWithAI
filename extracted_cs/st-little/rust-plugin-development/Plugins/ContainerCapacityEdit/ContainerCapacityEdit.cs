// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Container Capacity Edit", "st-little", "0.1.0")]
    [Description("Edit container capacity.")]
    public class ContainerCapacityEdit : RustPlugin
    {
        #region Configuration

        private Configuration _configuration;

        private class Configuration
        {
            public Dictionary<string, int> ContainerShortNames;
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                ContainerShortNames = new Dictionary<string, int>(),
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _configuration = Config.ReadObject<Configuration>();

                if (_configuration == null)
                    LoadDefaultConfig();
            }
            catch
            {
                PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => _configuration = GetDefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(_configuration);

        #endregion

        #region Oxide hooks

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (_configuration.ContainerShortNames.ContainsKey(item.info.shortname))
            {
                item.contents.capacity = _configuration.ContainerShortNames[item.info.shortname];
            }
        }

        #endregion
    }
}

