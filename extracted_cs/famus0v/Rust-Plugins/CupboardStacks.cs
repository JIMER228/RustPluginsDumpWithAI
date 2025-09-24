using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CupboardStacks", "Kira", "1.0.0")]
    [Description("Настройка стаков в шкафу")]
    public class CupboardStacks : RustPlugin
    {
        #region [Vars] / [Переменные]

        private static WaitForSeconds Wait = new WaitForSeconds(0.1f);

        #endregion

        #region [Configuration] / [Конфигурация]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "CupboardStacks - Config")]
            public CupboardStacksCFG CupboardStacksSettings = new CupboardStacksCFG();

            public class CupboardStacksCFG
            {
                [JsonProperty(PropertyName = "Настройка стаков в шкафу")]
                public List<CustomStack> Stacks;
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                CupboardStacksSettings = new ConfigData.CupboardStacksCFG
                {
                    Stacks = new List<CustomStack>
                    {
                        new CustomStack
                        {
                            Shortname = "wood",
                            SkinID = 0,
                            Stacks = new Dictionary<string, int>
                            {
                                ["cupboardstacks.default"] = 10
                            }
                        }
                    }
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Файл конфигурации поврежден (или не существует), создан новый!");
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region [Classes] / [Классы]

        public class CustomStack
        {
            public string Shortname;
            public ulong SkinID;
            public Dictionary<string, int> Stacks = new Dictionary<string, int>();
        }

        #endregion


        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            ItemManager.FindItemDefinition("wood").stackable = 1000;
            var d = ItemManager.CreateByName("wood");
            d.info.stackable = 1000;
        }
    }
}