using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    //Define:FileOrder=20
    public partial class ExtraLoot
    {
        private static Configuration _config;

        public class Configuration
        {
            [JsonProperty(PropertyName = "ShortName -> Items")]
            public Dictionary<string, List<BaseItem>> Containers;


            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    Containers = new Dictionary<string, List<BaseItem>>
                    {
                        ["crate_normal"] = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                ShortName = "pistol.revolver",
                                Chance = 50,
                                AmountMin = 1,
                                AmountMax = 1,
                                SkinId = 0,
                                DisplayName = "Recipe",
                                IsBlueprint = true
                            },
                            new BaseItem
                            {
                                ShortName = "box.repair.bench",
                                Chance = 30,
                                AmountMin = 1,
                                AmountMax = 2,
                                SkinId = 1594245394,
                                DisplayName = "Recycler",
                                IsBlueprint = false
                            },
                            new BaseItem
                            {
                                ShortName = "autoturret",
                                Chance = 30,
                                AmountMin = 1,
                                AmountMax = 2,
                                SkinId = 1587601905,
                                DisplayName = "Sentry Turret",
                                IsBlueprint = false
                            },
                            new BaseItem
                            {
                                ShortName = "paper",
                                Chance = 30,
                                AmountMin = 1,
                                AmountMax = 1,
                                SkinId = 1602864474,
                                DisplayName = "Recipe",
                                IsBlueprint = false
                            },
                            new BaseItem
                            {
                                ShortName = "paper",
                                Chance = 30,
                                AmountMin = 1,
                                AmountMax = 1,
                                SkinId = 1602955228,
                                DisplayName = "Recipe",
                                IsBlueprint = false
                            },
                        },
                        ["crate_elite"] = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                ShortName = "paper",
                                Chance = 15,
                                AmountMin = 1,
                                AmountMax = 1,
                                SkinId = 1602864474,
                                DisplayName = "Recipe"
                            },
                            new BaseItem
                            {
                                ShortName = "paper",
                                Chance = 15,
                                AmountMin = 1,
                                AmountMax = 1,
                                SkinId = 1602955228,
                                DisplayName = "Recipe"
                            },
                        },
                    }
                };
            }
        }


        #region BoilerPlate

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) LoadDefaultConfig();
                SaveConfig();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PrintWarning("Creating new config file.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => _config = Configuration.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion
    }
}