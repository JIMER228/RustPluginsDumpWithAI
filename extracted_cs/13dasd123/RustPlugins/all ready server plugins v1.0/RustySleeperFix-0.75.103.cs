using System.Collections.Generic;
using UnityEngine;

#pragma warning disable IDE0018
namespace Oxide.Plugins
{
    [Info("RustySleeperFix", "__red && Hougan", "0.75.103")]
    class RustySleeperFix : RustPlugin
    {
        public class RustySleeperFixConfig
        {
            public bool EnableSleeperDefense { get; set; }

            public static RustySleeperFixConfig Prototype()
            {
                return new RustySleeperFixConfig()
                {
                    EnableSleeperDefense = true,
                };
            }
        }

        private RustySleeperFixConfig m_Config;

        protected override void LoadDefaultConfig()
        {
            m_Config = RustySleeperFixConfig.Prototype();

            PrintWarning("Creating default a configuration file ...");
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();

            m_Config = Config.ReadObject<RustySleeperFixConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(m_Config);
        }
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["message_after_spawn"] = "<color=#49b50a>Сработала защита от просвета потолков</color>",
            }, this);
        }

        private void   OnPlayerRespawned(BasePlayer player)
        {
            if (player == null) return;
            if (!m_Config.EnableSleeperDefense) return;
            if (player.IsReceivingSnapshot)
            {
                NextTick(() =>
                {
                    OnPlayerRespawned(player);
                    return;
                });
            }

            player.EndSleeping();
            //SendReply(player, lang.GetMessage("message_after_spawn", this));

            return;
        }
        private object OnPlayerSpawn(BasePlayer player)
        {
            if (player == null) return null;
            if (!m_Config.EnableSleeperDefense) return null;
            if (player.IsReceivingSnapshot)
            {
                NextTick(() =>
                {
                    OnPlayerSpawn(player);
                    return;
                });
            }

            player.EndSleeping();
            //SendReply(player, lang.GetMessage("message_after_spawn", this));

            return null;
        }
        private void   OnPlayerInit(BasePlayer player)
        {
            if (player == null) return;
            if (!m_Config.EnableSleeperDefense) return;
            if (player.IsReceivingSnapshot)
            {
                NextTick(() =>
                {
                    OnPlayerInit(player);
                    return;
                });
            }

            player.EndSleeping();
            //SendReply(player, lang.GetMessage("message_after_spawn", this));

            return;
        }
    }
}
