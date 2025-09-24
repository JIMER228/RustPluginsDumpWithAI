using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ReJack", "RFC1920", "1.0.1")]
    [Description("Field reloading of jackhammer")]
    class ReJack : RustPlugin
    {
        ConfigData configData;
        public static ReJack Instance = null;
        private const string permReJack = "rejack.use";
        List<ulong> activeJack = new List<ulong>();

        void OnServerInitialized()
        {
            Instance = this;
            LoadConfigVariables();
            AddCovalenceCommand("rejack", "RejackCmd");
            AddCovalenceCommand("repjack", "RepairCmd");
            permission.RegisterPermission(permReJack, this);
        }

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Message(Lang(key, player.Id, args));
        private void LMessage(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["notauthorized"] = "You don't have permission to use this command.",
                ["repaired"] = "Your jackhammer has been repaired.",
                ["refilled"] = "Your jackhammer has been refilled.",
                ["repairjack"] = "Jackhammer can be used for repair for {0} seconds.",
                ["repairjacked"] = "Jackhammer function back to normal",
                ["itemrepaired"] = "Item fully repaired"
            }, this);
        }

        [Command ("rejack")]
        private void RejackCmd(IPlayer iplayer, string command, string[] args)
        {
            if (iplayer == null) return;
            if (configData.Options.usePermission && !iplayer.HasPermission(permReJack))
            {
                Message(iplayer, "notauthorized");
                return;
            }

            var player = iplayer.Object as BasePlayer;
            foreach (Item item in player.inventory.containerBelt.itemList)
            {
                if(item.info.name.Equals("jackhammer.item"))
                {
                    if (configData.Options.repairFully) item.info.condition.max = 500;
                    Effect.server.Run("assets/bundled/prefabs/fx/repairbench/itemrepair.prefab", player.transform.position);
                    item.condition = item.info.condition.max;
                    item.MarkDirty();
                    Message(iplayer, "repaired");
                }
            }
        }

        [Command ("repjack")]
        private void RepairCmd(IPlayer iplayer, string command, string[] args)
        {
            if (iplayer == null) return;
            if (configData.Options.usePermission && !iplayer.HasPermission(permReJack))
            {
                Message(iplayer, "notauthorized");
                return;
            }

            var player = iplayer.Object as BasePlayer;
            player.gameObject.AddComponent<RepairJack>();
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            if (entity == null) return null;
            var player = hitinfo.Initiator as BasePlayer;
            if (player == null) return null;

            if (!activeJack.Contains(player.userID)) return null;

            if (entity.health >= entity.MaxHealth())
            {
                Message(player.IPlayer, "itemrepaired");
                return null;
            }

            entity.health += 20f;
            return true;
        }

        void Unload()
        {
            foreach(var pl in BasePlayer.activePlayerList)
            {
                UnityEngine.Object.Destroy(pl.gameObject.GetComponent<RepairJack>());
            }
        }

        class RepairJack : MonoBehaviour
        {
            BasePlayer player;
            float time;

            void Awake()
            {
                player = GetComponentInParent<BasePlayer>();
                Instance.activeJack.Add(player.userID);
                time = Instance.configData.Options.repjacktime;
                Instance.Message(player.IPlayer, "repairjack", time.ToString());
                Destroy(this, time);
            }

            void OnDestroy()
            {
                Instance.Message(player.IPlayer, "repairjacked");
                Instance.activeJack.Remove(player.userID);
            }
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || input == null) return;
            if (configData.Options.usePermission && !player.IPlayer.HasPermission(permReJack)) return;

            if(input.WasJustPressed(BUTTON.RELOAD))
            {
                var held = player.GetHeldEntity();
                if (held == null) return;

                if (held is Jackhammer)
                {
                    var item = player.GetActiveItem();
                    if (item == null) return;

                    Effect.server.Run("assets/prefabs/clothes/diving.tank/effects/tank_refill.prefab", player.transform.position);
                    if (configData.Options.repairFully) item.info.condition.max = 500;
                    item.condition = item.info.condition.max;
                    item.MarkDirty();
                    Message(player.IPlayer, "refilled");
                }
            }
        }

        #region config
        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            var config = new ConfigData
            {
                Options = new Options()
                {
                    usePermission = false,
                    repairFully = false,
                    repjacktime = 30f
                }
            };
            SaveConfig(config);
        }

        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();

            configData.Version = Version;
            SaveConfig(configData);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }

        public class ConfigData
        {
            public Options Options = new Options();
            public VersionNumber Version;
        }

        public class Options
        {
            public bool usePermission;
            public bool repairFully;
            public float repjacktime;
        }
        #endregion
    }
}
