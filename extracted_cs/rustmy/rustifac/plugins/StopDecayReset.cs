// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
namespace Oxide.Plugins {
    using System;
    using Oxide.Core;
    [Info("StopDecayReset", "Yi", "1.0")]
    public class StopDecayReset : RustPlugin {
        private void Loaded() {
            ConsoleSystem.Run.Server.Normal($"decay.scale 0");
        }
    }
}
