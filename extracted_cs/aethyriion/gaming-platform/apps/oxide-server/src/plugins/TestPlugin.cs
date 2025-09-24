using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Plugins;
using UnityEngine;
using Facepunch.System;

namespace Oxide.Plugins
{
    [Info("Test Plugin", "Cyntaax Gaming", "0.1.0")]
    [Description("Plugin to verify compiler is working")]
    public class TestPlugin : RustPlugin
    {
        private void Init()
        {
            Puts("Test Plugin loaded and running");
            this.Player.Players.get("");
        }
    }
}
