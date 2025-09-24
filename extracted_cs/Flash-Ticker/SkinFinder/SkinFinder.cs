using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using UnityEngine;
using Network;

namespace Oxide.Plugins
{
    [Info("SkinFinder", "RustFlash", "1.0.0")]
    [Description("Displays skin information for items hit with a hammer")]
    public class SkinFinder : RustPlugin
    {
        private const string UsePermission = "skinfinder.use";
        private const string BabyBlueHex = "#89CFF0";

        void Init()
        {
            permission.RegisterPermission(UsePermission, this);
        }

        object OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (player == null || !permission.UserHasPermission(player.UserIDString, UsePermission))
                return null;

            Item activeItem = player.GetActiveItem();
            if (activeItem == null || activeItem.info.shortname != "hammer")
                return null;

            BaseCombatEntity entity = info.HitEntity as BaseCombatEntity;
            if (entity == null)
                return null;

            NextTick(() =>
            {
                if (entity == null || entity.IsDestroyed)
                    return;

                ulong skinID = entity.skinID;
                string itemName = entity.ShortPrefabName;
                NetworkableId netID = entity.net.ID;

                string message;
                if (skinID == 0)
                {
                    message = $"<color={BabyBlueHex}>SkinFinder:</color> This object has no custom skin!";
                }
                else
                {
                    message = $"<color={BabyBlueHex}>SkinFinder:</color> Information found:\n" +
                              $"<color={BabyBlueHex}>Shortname:</color> {itemName}\n" +
                              $"<color={BabyBlueHex}>SkinID:</color> {skinID}\n" +
                              $"<color={BabyBlueHex}>NetID:</color> {netID}";
                }

                player.ChatMessage(message);
            });

            return null;
        }
    }
}