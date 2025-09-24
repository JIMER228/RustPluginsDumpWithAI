using Newtonsoft.Json;
using Convert = System.Convert;
using Network;
using Oxide.Core.Plugins;
using Oxide.Core;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using Rust.Ai;
using System;
using UnityEngine; 
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;//

namespace Oxide.Plugins
{
	[Info("Food Waste", "obsol", "0.0.1")]
	[Description("Food wastes over time, with fridges and salt offering preservation methods.")]

	public class FoodWaste : CovalencePlugin
	{

        [PluginReference]
        private Plugin Backpacks;
		private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
		private void SendChatMsg(BasePlayer pl, string msg) =>
            _rustPlayer.Message(pl, msg,  "<color=#00ff00>[Food Waste]</color>", 0, Array.Empty<object>());
		float saltStep = 0.01f;
		int wasteTickThreshold = 1;
		int wasteTick = 0;
		private List<ItemContainer> containers = new List<ItemContainer>();
		private Dictionary<ItemContainer,float> containerLastUsed = new Dictionary<ItemContainer,float>();
		private Dictionary<WaterPurifier,float> purifierSaltStorage = new Dictionary<WaterPurifier,float>();
		float rotTick = 900;
		#region Configuration
        private Configuration config;
		[Command("fw_saveconfig")]
        private void surv_saveconfig(IPlayer player, string command, string[] args)
        {
			SaveConfig();
		}
		[Command("fw_loadconfig")]
        private void surv_loadconfig(IPlayer player, string command, string[] args)
        {
			LoadConfig();
		}

        class Configuration
        {
            // TODO: Add support for regex matching

            [JsonProperty("saltStep", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public float saltStep = 0.5f;
            [JsonProperty("wasteTickThreshold", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public int wasteTickThreshold = 1;

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }

                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
				saltStep=config.saltStep;
				wasteTickThreshold=config.wasteTickThreshold;
            }
            catch
            {
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
			
        }

        protected override void SaveConfig()
        {
            LogWarning($"Configuration changes saved to {Name}.json");
			config.saltStep=saltStep;
			config.wasteTickThreshold=wasteTickThreshold;
            Config.WriteObject(config, true);
		}
        #endregion Configuration
		
		void processContainer(ItemContainer ic){
			List<Item> foods = new List<Item>();
			Item saltPile = null;
			foreach(Item it in ic.itemList){
				if(it.info.shortname.Contains(".raw") || it.info.shortname=="bearmeat" || it.info.shortname=="meat.boar"){
					foods.Add(it);	
				}
				if(it.info.shortname.Contains(".berry") && !it.info.shortname.Contains("clone")&& !it.info.shortname.Contains("seed") ){
					foods.Add(it);	
				}
				else if (it.name != null && it.name.ToLower()=="salt"){
					saltPile = it;
				}
			}			
			Item itemTarget = null;
			if(foods.Count>0){
				itemTarget = foods[UnityEngine.Random.Range(0,foods.Count)];
			}
			if(itemTarget!=null){
				Puts("Targeted");
				if(itemTarget.info.shortname.Contains(".raw") || itemTarget.info.shortname=="bearmeat" || itemTarget.info.shortname=="meat.boar"){
					if(saltPile==null){
						ic.Remove(itemTarget);
						ic.Insert(ItemManager.CreateByItemID(-751151717, itemTarget.amount, 0));
					}
					else{
						saltPile.amount+=-itemTarget.amount;
						if(saltPile.amount<1){									
							ic.Remove(saltPile);
						}
						else{
							saltPile.MarkDirty();
						}
						
						ic.Remove(itemTarget);
						Item jerky = ItemManager.CreateByItemID(-1848736516, itemTarget.amount, 0);
						jerky.name="Jerky";
						ic.Insert(jerky);
					}
				}
				else if(itemTarget.info.shortname.Contains(".berry")){
					if(saltPile==null){
						ic.Remove(itemTarget);
						ic.Insert(ItemManager.CreateByItemID(352130972, itemTarget.amount, 0));
					}
					else{
						saltPile.amount+=-itemTarget.amount;
						if(saltPile.amount<1){						
							ic.Remove(saltPile);
						}
						else{
							saltPile.MarkDirty();
						}
					}
					
				}
			}
		}
		
		private void OnServerInitialized()
        {
			LoadConfig();
			updateContainers();
			initContainers();
			/*
            timer.Every(14f, () => {
				if(containers.Count>0){
					int containerCount = 0;
					int containerThreshold = (int)(containers.Count()/12);
					foreach(ItemContainer ic in containers.OrderBy(a => Oxide.Core.Random.Range(0,5000)).ToList()){
						if(ic.entityOwner!=null){
							if(ic.entityOwner.ToString().Contains("fridge")){
								break;
							}
						}
						containerCount++;
						if(containerCount>containerThreshold){return;}
						processContainer(ic);
					}
					
				}
				
			});
            timer.Every(599f, () => {	
				updateContainers();
			});
			*/
		}
		private void updateContainers(){
			foreach(StorageContainer sc in GameObject.FindObjectsOfType<StorageContainer>()){
				if(!containers.Contains(sc.inventory)){containers.Add(sc.inventory);}
			}
		}
		private void initContainers(){
			foreach(StorageContainer sc in GameObject.FindObjectsOfType<StorageContainer>()){
				if(!containerLastUsed.ContainsKey(sc.inventory)){containerLastUsed.Add(sc.inventory, UnityEngine.Time.realtimeSinceStartup);}
			}
		}
		void OnWaterPurified(WaterPurifier waterPurifier, float timeCooked)
		{
			if(!purifierSaltStorage.ContainsKey(waterPurifier)){purifierSaltStorage[waterPurifier]=0.0f;}
			purifierSaltStorage[waterPurifier]+=saltStep;
		}
		object CanLootEntity(BasePlayer player, StorageContainer container)
		{
			Puts("CanLootEntity works!");
			if(container == null) return null;
			if(container is BaseOven) return null;
			bool processRot = false;
			int rotRolls=0;
			if(containerLastUsed.ContainsKey(container.inventory)){
				if(UnityEngine.Time.realtimeSinceStartup - containerLastUsed[container.inventory] > rotTick){
					rotRolls = (int)((UnityEngine.Time.realtimeSinceStartup - containerLastUsed[container.inventory]) / rotTick);
					processRot=true;
				}
			}else{
				containerLastUsed.Add(container.inventory,UnityEngine.Time.realtimeSinceStartup);
			}
			if(processRot){
				containerLastUsed[container.inventory]=UnityEngine.Time.realtimeSinceStartup;
				for(int i = 0; i < rotRolls;i++){
					processContainer(container.inventory);
				}
			}
			return null;
		}
		object OnOvenCook(BaseOven oven, Item item)
		{
			List<Item> salts = oven.inventory.FindItemsByItemID(-265876753);
			foreach(Item salt in salts){
				if(salt.name.ToLower() == "salt"){
					foreach(Item fruit in oven.inventory.itemList){
						if(fruit.info.shortname.Contains(".berry") && !fruit.info.shortname.Contains("clone")&& !fruit.info.shortname.Contains("seed") && fruit.amount > 9){
							fruit.amount += -10;
							if(fruit.amount<1){
								oven.inventory.Remove(fruit);
							}
							salt.amount+=-5;
							if(salt.amount<1){									
								oven.inventory.Remove(salt);
							}
							string[] parsedName = fruit.info.shortname.Split('.');
							string color = parsedName[parsedName.Count() - 2];
							Item jam = ItemManager.CreateByItemID(-1941646328, 1, 0);
							jam.name=color+" Berry Jam";
							oven.inventory.Insert(jam);
						}
					}
				}
			}			
			return null;
		}
	
		bool? OnIngredientsCollect(ItemCrafter itemCrafter, ItemBlueprint blueprint, ItemCraftTask task, int amount, BasePlayer player)
		{
		    List<Item> collect = new List<Item>();
			
			foreach(ItemAmount ingredient in blueprint.ingredients){
					CollectIngredient(itemCrafter,ingredient.itemid, (int) ingredient.amount * amount, collect, player);				
			}
			bool dropout = false;
			
			foreach(ItemAmount ingredient in blueprint.ingredients){
					bool ingredientFound = false;
					foreach(Item item in collect){
						ingredientFound = ingredientFound || (item.info.itemid ==ingredient.itemid);
					}
					if(!ingredientFound){dropout=true;}
			}
			task.potentialOwners = new List<ulong>();
			foreach (Item obj in collect)
			{
			  obj.CollectedForCrafting(player);
			  if (!task.potentialOwners.Contains(player.userID))
				task.potentialOwners.Add(player.userID);
			}
			task.takenItems = collect;
			if(dropout){
				task.cancelled=true;
				if (((task.takenItems == null ? 0 : (task.takenItems.Count > 0 ? 1 : 0))) != 0)
				{
				  foreach (Item takenItem in task.takenItems)
				  {
					if (takenItem != null && takenItem.amount > 0)
					{
					  if (takenItem.IsBlueprint() && (UnityEngine.Object) takenItem.blueprintTargetDef == (UnityEngine.Object) task.blueprint.targetItem)
						takenItem.UseItem(task.numCrafted);
					  if (takenItem.amount > 0 && !takenItem.MoveToContainer(player.inventory.containerMain))
					  {
						takenItem.Drop(player.inventory.containerMain.dropPosition + UnityEngine.Random.value * Vector3.down + UnityEngine.Random.insideUnitSphere, player.inventory.containerMain.dropVelocity);
						player.Command("note.inv", (object) takenItem.info.itemid, (object) -takenItem.amount);
					  }
					}
				  }
				}
			}
			return false;
		}
		
		  private void CollectIngredient(ItemCrafter ic, int item, int amount, List<Item> collect,BasePlayer player)
		  {
			foreach (ItemContainer container in ic.containers)
			{
			  amount -= Take(container, collect, item, amount, player);
			  if (amount <= 0)
				break;
			}
		  }
		  private int Take(ItemContainer ic, List<Item> collect, int itemid, int iAmount,BasePlayer player)
		  {
			int num1 = 0;
			if (iAmount == 0)
			  return num1;
			List<Item> list = Facepunch.Pool.GetList<Item>();
			foreach (Item obj in ic.itemList)
			{
			  if (obj.info.itemid == itemid && obj.name==null)
			  {
				int num2 = iAmount - num1;
				if (num2 > 0)
				{
				  if (obj.amount > num2)
				  {
					obj.MarkDirty();
					obj.amount -= num2;
					num1 += num2;
					Item byItemId = ItemManager.CreateByItemID(itemid);
					byItemId.amount = num2;
					byItemId.CollectedForCrafting(player);
					if (collect != null)
					{
					  collect.Add(byItemId);
					  break;
					}
					break;
				  }
				  if (obj.amount <= num2)
				  {
					num1 += obj.amount;
					list.Add(obj);
					collect?.Add(obj);
				  }
				  if (num1 == iAmount)
					break;
				}
			  }
			}
			foreach (Item obj in list)
			  obj.RemoveFromContainer();
			Facepunch.Pool.FreeList<Item>(ref list);
			return num1;
		  }
		
		
		bool? CanStackItem(Item item, Item targetItem)
		{
			if(item.name != targetItem.name || item.info.itemid != targetItem.info.itemid){return false;}
			return null;
		}
				
		void OnLootEntity(BasePlayer player, BaseEntity entity)
		{
			WaterPurifier wp = entity.GetComponent<WaterPurifier>();
			if(wp==null){return;}
			if(!purifierSaltStorage.ContainsKey(wp)){return;}
			Puts(purifierSaltStorage[wp].ToString());
			int saltAmount = (int)purifierSaltStorage[wp];
			
			if(saltAmount==0) {return;}
            Item item = ItemManager.CreateByItemID(-265876753, saltAmount, 2689553090);////
			item.name = "Salt";
			item.MarkDirty();
			purifierSaltStorage[wp]-=saltAmount;	
			player.inventory.GiveItem(item);
		}
		Item OnItemSplit(Item item, int amount)
		{
			if(item.name != null){
				item.amount -= amount;
				Item byItemId = ItemManager.CreateByItemID(item.info.itemid);
				byItemId.amount = amount;
				byItemId.skin = item.skin;
				byItemId.name = item.name;
				item.MarkDirty();
				byItemId.MarkDirty();
				return byItemId;
			}
			else{				
				return null;
			}
		}
	}
}