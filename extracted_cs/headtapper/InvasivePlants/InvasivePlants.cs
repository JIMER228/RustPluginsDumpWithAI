using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("InvasivePlants", "headtapper", "1.0.4")]
    [Description("Seed and clone planting controller. Requires players to plant seeds and clones in planter boxes.")]
    class InvasivePlants : RustPlugin
    {
        private const string BypassPlanterCheckPermission = "invasiveplants.bypass";

        private PluginConfig Configuration;

        #region Config

        class PluginConfig
        {
            public bool EnableChatMessage;
            public bool ReturnItem;
            public List<string> ItemIgnoreListShortnames;
            public bool CallOnDenyPlantHook;
        }

        protected override void LoadDefaultConfig()
        {
            PluginConfig config = new PluginConfig{
                EnableChatMessage = true,
                ReturnItem = true,
                ItemIgnoreListShortnames = new List<string>(),
                CallOnDenyPlantHook = false
            };
            Config.WriteObject(config, true);
        }

        #endregion

        #region Initialization

        void Init()
        {
            permission.RegisterPermission(BypassPlanterCheckPermission, this);
            Configuration = Config.ReadObject<PluginConfig>();
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                {"requiresPlanter", "You may only plant {type} inside of a planter!" }
            }, this, "en");
        }

        #endregion

        #region Hooks

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            var player = plan.GetOwnerPlayer();
            if (player == null)
                return;

            if (permission.UserHasPermission(player.UserIDString, BypassPlanterCheckPermission))
                return;

            var plant = go.GetComponent<GrowableEntity>();
            if (plant == null)
                return;

            var sourceItem = player.GetActiveItem();

            NextTick(() =>
            {
                if (plant.GetParentEntity() is PlanterBox)
                    return;

                if (sourceItem == null)
                    return;

                foreach (string ignoredShortname in Configuration.ItemIgnoreListShortnames)
                {
                    if (sourceItem.info.shortname.Equals(ignoredShortname))
                        return;
                }

                if (Configuration.EnableChatMessage)
                    player.ChatMessage(lang.GetMessage("requiresPlanter", this, player.UserIDString).Replace("{type}", sourceItem.info.displayName.english));

                GrowableGenes originalGenes = plant.Genes;
                plant.Kill(BaseNetworkable.DestroyMode.None);

                if (Configuration.CallOnDenyPlantHook)
                    Interface.Call("OnDenyPlant", player, plant.SourceItemDef.shortname, sourceItem.info.displayName.english);

                if (!Configuration.ReturnItem)
                    return;

                var returnItem = ItemManager.CreateByName(sourceItem.info.shortname, 1);
                if (returnItem != null)
                {
                    if (returnItem.info.shortname.Contains("clone"))
                    {
                        GrowableGeneEncoding.EncodeGenesToItem(GrowableGeneEncoding.EncodeGenesToInt(originalGenes), returnItem);
                    }
                    player.inventory.GiveItem(returnItem);
                }
            });
        }

        #endregion
    }
}
