/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

namespace Oxide.Plugins
{
    [Info("Always Inflated Balloons", "VisEntities", "1.0.0")]
    [Description("Quickly inflates hot air balloons when toggled on.")]
    public class AlwaysInflatedBalloons : RustPlugin
    {
        #region Fields

        private static AlwaysInflatedBalloons _plugin;

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

        private void OnHotAirBalloonToggled(HotAirBalloon balloon, BasePlayer player)
        {
            if (balloon == null || player == null)
                return;

            if (balloon.IsOn())
                InflateBalloon(balloon);
        }

        #endregion Oxide Hooks

        #region Balloon Inflation

        private void InflateBalloon(HotAirBalloon balloon)
        {
            balloon.inflationLevel = 1f;
            balloon.bounds = balloon.raisedBounds;
            balloon.SetFlag(BaseEntity.Flags.Reserved1, true, false, true);
            balloon.SetFlag(BaseEntity.Flags.Reserved2, true, false, true);
            balloon.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
        }

        #endregion Balloon Inflation
    }
}