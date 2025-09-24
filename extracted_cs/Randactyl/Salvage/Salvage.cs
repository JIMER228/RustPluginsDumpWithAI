using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins {
    [Info("Salvage", "Randactyl", "1.1.0")]
    [Description("Use /salvage to enable/disable salvage mode at repair benches.")]
    public class Salvage : RustPlugin {
        Dictionary<BasePlayer, bool> salvagers = new Dictionary<BasePlayer, bool>();
        Dictionary<BasePlayer, BaseEntity> benchMap = new Dictionary<BasePlayer, BaseEntity>();
        Dictionary<string, ItemBlueprint> blueprints = new Dictionary<string, ItemBlueprint>();

        void OnServerInitialized() {
            //build bp lookup
            blueprints = ItemManager.bpList.ToDictionary(itemBp => itemBp.targetItem.shortname, itemBp => itemBp);
        }

        [ChatCommand("salvage")]
        void cmdSalvage(BasePlayer player, string command, string[] args) {
            //if player is a salvager
            if(salvagers.ContainsKey(player)) {
                //remove them
                salvagers.Remove(player);
                player.ChatMessage("Salvaging is now OFF.");
            } else {
                //add them
                salvagers[player] = true;
                player.ChatMessage("Salvaging is now ON. Visit a repair bench to salvage items.");
            }
        }

        void OnLootEntity(BaseEntity source, BaseEntity target) {
            //check entities
            if(source == null || target == null) return;
            if(target.ShortPrefabName != "repairbench_deployed"
              && target.ShortPrefabName != "repairbench_static") return;

            BasePlayer player = (BasePlayer) source;

            //if player is a salvager
            if(salvagers.ContainsKey(player)) {
                //store player in bench map
                benchMap[player] = target;
                
                //send reminder message
                player.ChatMessage("Remember, you're salvaging!");
            }
        }
        
        void OnLootEntityEnd(BaseEntity source, BaseEntity target) {
            //check entity is repair bench
            if(source == null || target == null) return;
            if(target.ShortPrefabName != "repairbench_deployed"
              && target.ShortPrefabName != "repairbench_static") return;

            BasePlayer player = (BasePlayer) source;

            //if player is a salvager then remove from bench map
            if(salvagers.ContainsKey(player))
                benchMap.Remove(player);
        }

        void OnItemAddedToContainer(ItemContainer container, Item item) {
            //get container entity
            BaseEntity entity = container.entityOwner as BaseEntity;
            
            //check entity is repair bench
            if(entity == null) return;
            if(entity.ShortPrefabName != "repairbench_deployed"
              && entity.ShortPrefabName != "repairbench_static") return;

            //get player from bench map
            BasePlayer player = null;
            foreach(BasePlayer key in benchMap.Keys)
                if(benchMap[key] == entity) {
                    player = key;
                    break;
                }
            
            //check player
            if(player == null) return;
            
            //salvage item
            SalvageItem(player, item);
        }

        void SalvageItem(BasePlayer player, Item item) {
            //check player can craft item
        	bool canCraft = player.blueprints.CanCraft(item.info.itemid, 0);

            //if can't craft or broken, else salvage
        	if(!canCraft || (item.hasCondition && item.condition == 0)) {
        		player.ChatMessage("This item is not salvageable.");
        	} else {
                //remove the item from the container
        	    item.RemoveFromContainer();
                
                //get bp
        		ItemBlueprint itemBp = blueprints[item.info.shortname];
                
                //set ratios
        		double refundRatio = 0.5;
                //if item.hasCondition then conditionRatio == item.conditionNormalized else conditionRatio = 1
                double conditionRatio = item.hasCondition ? item.conditionNormalized : 1;
                
                /*
                    itemBp.amountToCreate is the number of items made by the act of crafting (3x 5.56)
                    ingredient.amount is the number of that material required (100 wood for campfire)
                    ingredient.itemDef.stackable is the max stack size of that ingredient
                */
                foreach(ItemAmount ingredient in itemBp.ingredients) {
                    //calculate refund amount
                    double refundAmount = ingredient.amount / itemBp.amountToCreate;
                    refundAmount = refundAmount * refundRatio * conditionRatio * item.amount;
                    refundAmount = Math.Ceiling(refundAmount);
                    if(refundAmount < 1)
                        refundAmount = 1;
                        
                    //calculate how many stacks to split the refund into (no super stacks)
                    double refundStacks = refundAmount / ingredient.itemDef.stackable;
                    if(refundStacks < 1)
                        refundStacks = 1;
                    
                    //refund ingredient
                    for(int i = 0; i < refundStacks; i++) {
                        int amt;
                        
                        //calculate how much to give in this stack
                        if (refundAmount <= ingredient.itemDef.stackable)
                            amt = (int)refundAmount;
                        else
                            amt = ingredient.itemDef.stackable;
                        
                        //reduce the pending refund amount by how much we're about to give out
                        refundAmount = refundAmount - amt;
                        
                        //create and give this stack
                        Item newItem = ItemManager.Create(ingredient.itemDef, amt);
                        player.GiveItem(newItem);
                    }
                }
        	}
        }
    }
}