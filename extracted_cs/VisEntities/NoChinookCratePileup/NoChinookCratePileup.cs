/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Facepunch;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("No Chinook Crate Pileup", "VisEntities", "1.0.0")]
    [Description("Prevents multiple chinook crates from piling up in the same area.")]
    public class NoChinookCratePileup : RustPlugin
    {
        #region Fields

        private static NoChinookCratePileup _plugin;

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

        private void OnCrateLanded(HackableLockedCrate newCrate)
        {
            if (newCrate == null)
                return;

            List<HackableLockedCrate> nearbyCrates = Pool.Get<List<HackableLockedCrate>>();
            Vis.Entities(newCrate.transform.position, 1.5f, nearbyCrates);

            foreach (HackableLockedCrate crate in nearbyCrates)
            {
                if (crate == newCrate)
                    continue;

                if (crate != null && crate.hasLanded && !crate.IsBeingHacked())
                    crate.Kill();
            }

            Pool.FreeUnmanaged(ref nearbyCrates);
        }

        #endregion Oxide Hooks
    }
}