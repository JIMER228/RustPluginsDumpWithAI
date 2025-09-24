using Oxide.Core.Plugins;
using Oxide.Core;
using System.Collections.Generic;
using System.ComponentModel;

namespace Oxide.Plugins
{
    [Info("SpearOfLonginus", "jerky", "1.0.0")]
    [Description("Spear of Longinus plugin for Rust")]

    public class SpearOfLonginus : RustPlugin
    {
        private const string SpecialSpearShortname = "spear_wooden.entity";
        private const ulong SpecialSkinID = 3337606732;
        private List<string> excludedNPCNames = new List<string> { "A" }; // Configure it later.

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (info.InitiatorPlayer != null && info.Weapon == null && info.Weapon is BaseMelee)
            {
                BaseMelee weapon = info.Weapon as BaseMelee;
                if (weapon.skinID == SpecialSkinID && weapon.ShortPrefabName == SpecialSpearShortname)
                {
                    
                    if (entity is BasePlayer || entity is NPCPlayer)
                    {
                        BasePlayer target = entity as BasePlayer;
                        if (!excludedNPCNames.Contains(target.displayName))
                        {
                            float damage = entity.Health() / 2; // Configure it later.
                            entity.Hurt(damage, Rust.DamageType.Generic, info.InitiatorPlayer);
                            Effect.server.Run("assets/bundled/prefabs/fx/explosions/explosion_03.prefab", entity.transform.position); // Configure it later.
                        }
                    }
                }
            }
        }

        void OnMeleeThrown(BasePlayer player, Item item)
        {
            if (item.skin == SpecialSkinID)
            {
                BaseEntity entity = item.GetHeldEntity();
                item.Remove(0.5f);
                entity.SendNetworkUpdateImmediate();
            }
        }

        private void GiveSpecialSpearToPlayer(ulong steamID)
        {
            BasePlayer targetPlayer = BasePlayer.FindByID(steamID) ?? BasePlayer.FindSleeping(steamID);
            if (targetPlayer == null)
            {
                Puts("Player not found.");
                return;
            }

            Item item = ItemManager.CreateByName("spear.wooden", 1, SpecialSkinID);
            item.name = "Spear of Longinus";
            item.MarkDirty();

            var held = item.GetHeldEntity();
            if (held != null)
            {
                held.skinID = SpecialSkinID;
                held.SendNetworkUpdate();
            }

            targetPlayer.GiveItem(item);
            Puts($"Special spear given to {targetPlayer.displayName}.");
        }

        [ConsoleCommand("givespecialspear")]
        private void GiveSpecialSpearConsole(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length != 1)
            {
                arg.ReplyWith("givespecialspear <steamID>");
                return;
            }

            ulong steamID;
            if (!ulong.TryParse(arg.Args[0], out steamID))
            {
                arg.ReplyWith("Invalid SteamID.");
                return;
            }

            GiveSpecialSpearToPlayer(steamID);
        }

    }
}
