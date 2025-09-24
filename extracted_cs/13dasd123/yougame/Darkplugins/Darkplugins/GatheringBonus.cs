
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("GatheringBonus","Baks","1.2.0")]
    public class GatheringBonus : RustPlugin
    {
        private Dictionary<string, BonusRes> toolConfig = new Dictionary<string, BonusRes>
        {
            ["hammer.salvaged"] = new BonusRes
            {
                resourceName = "sulur",
                resourceAmount = {DarkPluginsID},
                resourceChance = 5f
            }
        };
        class BonusRes
        {
            public string resourceName;
            public float resourceChance;
            public int resourceAmount;
        }
        
        #region Init

        
        private string gitem;
        
        #endregion

        void Loaded()
        {
            LoadDefaultConfig();
            PrintWarning("Confugurations loaded");

            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("BonusTools"))
            {
                Interface.Oxide.DataFileSystem.WriteObject("BonusTools",toolConfig);
            }
            else
            {
                toolConfig = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, BonusRes>>("BonusTools");
            }
            
        }
        


        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!(entity is BasePlayer))
                return;
            
            BasePlayer player = entity as BasePlayer;

            Item cItem = player.GetActiveItem();

            string shortname = cItem.info.shortname;

            if (toolConfig.ContainsKey(cItem.info.shortname))
            {
                if (Oxide.Core.Random.Range(1f,100f) > toolConfig[shortname].resourceChance) return;
                if (player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count > 0)
                    ItemManager.CreateByPartialName(toolConfig[shortname].resourceName,toolConfig[shortname].resourceAmount).MoveToContainer(player.inventory.containerMain);
                else
                    ItemManager.CreateByPartialName(toolConfig[shortname].resourceName,toolConfig[shortname].resourceAmount).Drop(player.transform.position, Vector3.down);
            }
            
        }

    }
}