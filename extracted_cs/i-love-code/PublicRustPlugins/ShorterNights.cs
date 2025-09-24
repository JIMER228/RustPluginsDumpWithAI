using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System;
using System.Collections;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using Network;
using Facepunch;
using System.IO;
using ProtoBuf;
using Newtonsoft.Json;
using System.Text;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust;
using Rust;

namespace Oxide.Plugins
{
    [Info("ShorterNights", "Audi", 0.1)]
    [Description("Fuck the darkness!")]

    public class ShorterNights : RustPlugin
    {
        private Timer _timeTimer;

        private const float MAX_DARK_TIME = 21.2f;
        private const float MIN_LIGHT_TIME = 6.1f;

        void Loaded ()
        {
            _timeTimer = timer.Every(10f, CheckDaytime);
        }

        private void CheckDaytime()
        {
            if (TOD_Sky.Instance.Cycle.Hour > MAX_DARK_TIME || TOD_Sky.Instance.Cycle.Hour < MIN_LIGHT_TIME)
            {
                TOD_Sky.Instance.Cycle.Hour = MIN_LIGHT_TIME;
            }
        }

        void Unload ()
        {

        }
    }
}