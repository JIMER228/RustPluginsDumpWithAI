// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

namespace Oxide.Plugins
{
    [Info("No Excavator Airdrops", "VisEntities", "1.0.0")]
    [Description("Disables supply drop requests triggered by excavators.")]
    public class NoExcavatorAirdrops : RustPlugin
    {
        #region Fields

        private static NoExcavatorAirdrops _plugin;

        #endregion Fields

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
        }

        private void Unload()
        {
            _plugin = null;
        }

        private object OnExcavatorSuppliesRequest(ExcavatorSignalComputer excavator, BasePlayer player)
        {
            if (excavator == null || player == null)
                return null;

            return true;
        }

        #endregion Oxide Hooks
    }
}