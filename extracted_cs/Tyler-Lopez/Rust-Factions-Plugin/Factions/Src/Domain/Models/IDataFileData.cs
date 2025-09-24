// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿namespace Oxide.Plugins
{
    using Oxide.Core.Configuration;
    interface IDataFileData
    {
        DynamicConfigFile File { get; set; }
    }
}
