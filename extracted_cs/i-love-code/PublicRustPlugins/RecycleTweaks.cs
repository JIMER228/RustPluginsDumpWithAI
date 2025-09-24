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
    [Info("RecycleTweaks", "Jake_Rich", 0.1)]
    [Description("Changes recycler to include all items")]

    public class RecycleTweaks : RustPlugin
    {
        object CanRecycle(Recycler recycler, Item item)
        {
            if (item.info.shortname.Contains("can.") && item.info.shortname.Contains(".empty"))
            {
                return true;
            }
            return null;
        }

        object OnRecycleItem(Recycler recycler, Item item)
        {
            bool shouldOverride = false;
            if (item.info.shortname.Contains("can.") && item.info.shortname.Contains(".empty"))
            { 
                int amount = Mathf.Clamp(item.amount, 1, 2);
                item.UseItem(amount);
                shouldOverride = true;
                SendToOutput(recycler, "metal.fragments", 15 * amount);
            }
            if (shouldOverride)
            {
                return true;
            }
            return null;
        }

        bool SendToOutput(Recycler recycler, string shortname, int amount)
        {
            Item item = ItemManager.CreateByName(shortname, amount);
            if (item!= null)
            {
                return recycler.MoveItemToOutput(item);
            }
            return false;
        }
    }
}