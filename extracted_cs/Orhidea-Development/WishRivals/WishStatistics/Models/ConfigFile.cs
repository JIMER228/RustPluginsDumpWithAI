// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using WishInfrastructure.Models;

namespace Oxide.Plugins
{
    public class ConfigFile
    {
        public LbClan[] lb_clans { get; set; }
        public DatabaseConfig DatabaseConfig { get; set; }
         
    }
}
