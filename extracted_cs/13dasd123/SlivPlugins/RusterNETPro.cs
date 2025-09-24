using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Oxide.Plugins
{
    [Info ( "RusterNET Pro", "Raul-Sorin Sorban", "1.0.1" )]
    [Description ( "DLC for Ruster.NET. Allows the ability to modify the core identity of the plugin (e.g logos, emotes, etc)." )]
    public class RusterNETPro : RustPlugin
    {
        #region Overrides

        public bool IsInitialized { get; set; }

        private void Loaded ()
        {
            if ( !IsInitialized ) return;

            if ( ConfigFile == null ) ConfigFile = new Core.Configuration.DynamicConfigFile ( $"{Manager.ConfigPath}{Path.DirectorySeparatorChar}{Name}.json" );

            if ( !ConfigFile.Exists () )
            {
                ConfigFile.WriteObject ( Config = new RootConfig () );
            }
            else
            {
                try
                {
                    Config = ConfigFile.ReadObject<RootConfig> ();
                }
                catch ( Exception exception )
                {
                    Puts ( $"Broken configuration: {exception.Message}" );
                }
            }
        }
        private void OnServerInitialized ()
        {
            IsInitialized = true;

            Loaded ();
        }
        private void OnServerSave ()
        {
            if ( !IsInitialized ) return;

            if ( Config != null ) ConfigFile.WriteObject ( Config );
        }

        #endregion

        private JObject GetConfig ()
        {
            if ( Config == null ) Loaded ();
            return Config == null ? null : JsonConvert.DeserializeObject<JObject> ( JsonConvert.SerializeObject ( Config ) );
        }

        #region Config 

        public Core.Configuration.DynamicConfigFile ConfigFile { get; set; }

        public new RootConfig Config { get; set; } = new RootConfig ();

        public class RootConfig
        {
            public string RusterNetworkLogo { get; set; } = string.Empty;
            public string RusterLogo { get; set; } = string.Empty;
            public string RusterMarketplaceLogo { get; set; } = string.Empty;
            public string RusterMarketplace2Logo { get; set; } = string.Empty;
            public string RusterFMLogo { get; set; } = string.Empty;
            public string RusterVerifiedTickIcon { get; set; } = string.Empty;
            public string RusterStoriesLogo { get; set; } = string.Empty;
            public string RusterStoriesFullLogo { get; set; } = string.Empty;
            public string RusterSkinName { get; set; } = string.Empty;
            public ulong RusterSkinId { get; set; } = 0UL;
            public string RusterMarketplace24hAdvertSkinName { get; set; } = string.Empty;
            public ulong RusterMarketplace24hAdvertSkinId { get; set; } = 0UL;
            public string RusterMarketplace1wAdvertSkinName { get; set; } = string.Empty;
            public ulong RusterMarketplace1wAdvertSkinId { get; set; } = 0UL;
            public string RusterBusinessCardSkinName { get; set; } = string.Empty;
            public ulong RusterBusinessCardSkinId { get; set; } = 0UL;

            public bool AppendEmojis { get; set; } = true;
            public Emoji [] Emojis { get; set; } = new Emoji [] { new Emoji { Name = "My Example", Shortname = "example", IconUrl = "" } };

            public class Emoji
            {
                public string Name { get; set; }
                public string Shortname { get; set; }
                public string IconUrl { get; set; }
            }
        }

        #endregion
    }
}
