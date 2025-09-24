using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using TinyJSON;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RustMod", "VSP", "1.0.0")]
    [Description("My first plugin")]
    class FirstPlugin : RustPlugin
    {
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var player = info.InitiatorPlayer;
            if (player && player.IsAlive())
            {
                CuiHelper.DestroyUi(player, "hitMarker");
                string json = CUI_HitMarker;
                if (entity.health < (entity.health / 100 * 70))
                    json = CUI_HitMarker.Replace("1 0.8 0 1", "");
                if (entity.health < (entity.health / 100 * 30))
                    json = CUI_HitMarker.Replace("0.0 0.1 0.2 1", "");
                CuiHelper.AddUi(player, json);

            }
        }
        string CUI_HitMarker = @"[{""name"":""hitMarker"",""parent"":""Overlay"",""components"":[{""type"":""UnityEngine.UI.Image"",""sprite"":""assets/icons/loading.png"",""material"":"""",""color"":""0.627451 0.6588235 1 1""},{""type"":""RectTransform"",""anchormin"":""0.4807 0.465"",""anchormax"":""0.5390625 0.5694444"",""offsetmin"":""0 0"",""offsetmax"":""0 0""}]}]";
    }
    
}

