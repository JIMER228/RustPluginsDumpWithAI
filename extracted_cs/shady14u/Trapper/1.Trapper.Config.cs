using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Oxide.Plugins
{
    //Define:FileOrder=20
    public partial class Trapper
    {
        private static Configuration _config;

        public class Configuration
        {
            [JsonProperty(PropertyName = "Reset Time")]
            public float ResetTime { get; set; } = 5f;
            [JsonProperty(PropertyName = "Trigger for Owner")]
            public bool HurtOwner { get; set; } = false;
            [JsonProperty(PropertyName = "Trigger for Friends")]
            public bool HurtFriends { get; set; } = false;
            [JsonProperty(PropertyName = "Trigger for Clan Mates")]
            public bool HurtClanMates { get; set; } = false;
            [JsonProperty(PropertyName = "Trigger for Authorized")]
            public bool HurtAuthed { get; set; } = false;
            [JsonProperty(PropertyName = "Ignore Projectiles")]
            public bool IgnoreProjectiles { get; set; } = false;
            

            public static Configuration DefaultConfig()
            {
                return new Configuration();
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