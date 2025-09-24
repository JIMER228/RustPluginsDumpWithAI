/*

  "FactionScores": {
    "10645866045":{
		"Default":0,
		"Player-Aligned":0,
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Carnivores":0,
		"Herbivores":0
	},
    "Default":{
		"Default":0,
		"Player-Aligned":0,
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Carnivores":0,
		"Herbivores":0
	},
    "Player-Aligned":{
		"Default":0,
		"Player-Aligned":0,
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Carnivores":0,
		"Herbivores":0
	},
    "Player":{
		"Default":0,
		"Player-Aligned":0,
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Carnivores":0,
		"Herbivores":0
	},
    "Scientist":{
		"Default":0,
		"Player-Aligned":0,
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Carnivores":0,
		"Herbivores":0
	},
    "Dwellers":{
		"Default":0,
		"Player-Aligned":0,
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Carnivores":0,
		"Herbivores":0
	},
    "Bandits":{
		"Default":0,
		"Player-Aligned":0,
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Carnivores":0,
		"Herbivores":0
	},
    "Carnivores":{
		"Default":0,
		"Player-Aligned":0,
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Carnivores":0,
		"Herbivores":0
	},
    "Herbivores":{
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Animals":0,
		"Herbivores":0
	}
  },
  
When damage
	personal score - damage (if player)
	enemy score + damage/10 (if player)
	ally score - damage/10 (if player)
	interfaction score - damage/100
	if enemy on player
		enemy faction + damage/1000
On sale
	if NPC shop, scrap to faction bank
		scrap /100 to faction score
		scrap /1000 to interfaction score
	
	If not, get HumanNPC in 50 meters, if no enemies, split proportional to alliance counts
	
On timer
	if populations < minimums, spawn on road
	if populations < maximum, if budget, spawn on road
	


	
*/

// Requires: Omninasty
// Requires: Boids
#region using
	using Convert = System.Convert;
	using Network;
	using Newtonsoft.Json;
	using System;
	using System.Collections.Generic;
	using System.Collections;
	using System.Linq;
	using System.Text;
	using Oxide.Core.Libraries.Covalence;
	using Oxide.Plugins;
	using Oxide.Core.Plugins;
	using Oxide.Core;
	using UnityEngine; 
	using UnityEngine.SceneManagement;
	using UnityEngine.AI;
	using Rust.Ai;
	using Oxide.Ext.RustEdit;
	using Oxide.Ext.RustEdit.NPC;
#endregion

namespace Oxide.Plugins{
	[Info("FactionHandler", "obsol", "0.2.1")]
	[Description("Sticks a shop on every human npc")]
	public class FactionHandler : RustPlugin{

		public static Omninasty omninasty;
		public static Boids boids;
		void Loaded(){
			omninasty = (Omninasty)Manager.GetPlugin("Omninasty");
			boids = (Boids)Manager.GetPlugin("Boids");
			LoadConfig();
			foreach(string key1 in config.FactionScores.Keys.ToArray()){
				foreach(string key2 in config.FactionScores[key1].Keys.ToArray()){
					config.FactionScores[key1][key2]=((int)(config.FactionScores[key1][key2]*100))/100.0f;
					config.FactionScores[key1][key2]=config.FactionScores[key1][key2]*0.9f;
				}
			}
		}
		void Unload(){
			SaveConfig();
		}
		//
		
		[ConsoleCommand("fact.save")]
		private void surv_saveconfig(ConsoleSystem.Arg arg){
			SaveConfig();
			Puts("Faction Saved");
		}
		[ConsoleCommand("fact.load")]
		private void surv_loadconfig(ConsoleSystem.Arg arg){
			LoadConfig();
			Puts("Faction Loaded");
		}
		public int scrap = -932201673;
		public Dictionary<string,int> spawnCounts = new Dictionary<string,int>();
		public static Configuration config;
		public class Configuration{
			[JsonProperty("FactionScores", ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public Dictionary<string,Dictionary<string,float>> FactionScores = new Dictionary<string,Dictionary<string,float>>();
			[JsonProperty("FactionBanks", ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public Dictionary<string,float> FactionBanks = new Dictionary<string,float>();
			[JsonProperty("FactionSpawnEntities",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public Dictionary<string,List<string>> FactionSpawnEntities = new Dictionary<string,List<string>>();
			[JsonProperty("FactionSpawnLimits",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public Dictionary<string,int> FactionSpawnLimits = new Dictionary<string,int>();
			[JsonProperty("FactionSpawnMinimums",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public Dictionary<string,int> FactionSpawnMinimums = new Dictionary<string,int>();
			[JsonProperty("FactionSpawnCost",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public int FactionSpawnCost = 500;
			
			
			public string ToJson() => JsonConvert.SerializeObject(this);				
			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
		}
		protected override void LoadDefaultConfig() => config = new Configuration();
		protected override void LoadConfig(){
			base.LoadConfig();
			try{
				config = Config.ReadObject<Configuration>();
				if (config == null) throw new JsonException();
				if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys)){
					Puts("Configuration appears to be outdated; updating and saving");
					SaveConfig();
					}
			}
			catch{
				Puts($"Configuration file {Name}.json is invalid; using defaults");
				LoadDefaultConfig();
				
			}
		}
		protected override void SaveConfig(){
			Puts($"Configuration changes saved to {Name}.json");
			Config.WriteObject(config, true);
		}
		
		
		private void OnServerInitialized()//
        {
			
			timer.Every(150f,()=>{
				foreach(string faction in config.FactionScores.Keys){
					Puts("Trying to spawn for "+faction);
					spawnForFaction(faction);
				}				
			});
		}
		
		
		public bool spawnForFaction(string faction){
			if(!config.FactionSpawnEntities.ContainsKey(faction))
				config.FactionSpawnEntities.Add(faction,new List<string>());
			if(!config.FactionSpawnMinimums.ContainsKey(faction))
				config.FactionSpawnMinimums.Add(faction,10);
			if(!config.FactionBanks.ContainsKey(faction))
				config.FactionBanks.Add(faction,0);
			if(!config.FactionSpawnLimits.ContainsKey(faction))
				config.FactionSpawnLimits.Add(faction,50);
			if(!spawnCounts.ContainsKey(faction))
				spawnCounts.Add(faction,0);
			if(config.FactionSpawnEntities[faction].Count>0){
				int i = UnityEngine.Random.Range(0,config.FactionSpawnEntities[faction].Count);
				List<PathList> roads = TerrainMeta.Path.Roads;
				int j = UnityEngine.Random.Range(0,roads.Count);
				PathList road = roads[j];
				Vector3[] points = road.Path.Points;				
				int k = UnityEngine.Random.Range(0,points.Length);
				Vector3 position = points[k];
				RadialPoint(out position,points[k],points[k],20,30);
				string prefab = config.FactionSpawnEntities[faction][i];
				if(spawnCounts[faction]<config.FactionSpawnMinimums[faction]){
					ConVar.Entity.svspawn(prefab,position,new Vector3(0,0,0));
					Puts(faction+" spawned at "+position.ToString());					
				}else if (config.FactionBanks[faction] > config.FactionSpawnCost && spawnCounts[faction]<config.FactionSpawnLimits[faction]){
					ConVar.Entity.svspawn(prefab,position,new Vector3(0,0,0));
					Puts(faction+" spawned at "+position.ToString());
				}
				
			}
			return false;
		}
		
		
		
		object OnEntityDeath(BaseNpc entity, HitInfo info){
			string faction = omninasty.getFaction(entity);
			if(!config.FactionSpawnEntities.ContainsKey(faction))
				config.FactionSpawnEntities.Add(faction,new List<string>());
			if(!config.FactionSpawnEntities[faction].Contains(entity.ShortPrefabName))
				config.FactionSpawnEntities[faction].Add(entity.ShortPrefabName);
			if(!spawnCounts.ContainsKey(faction))
				spawnCounts.Add(faction,0);
			spawnCounts[faction]= Math.Max(spawnCounts[faction]-1,0);
			return null;
		}
		object OnEntityDeath(HumanNPC entity, HitInfo info){
			string faction = omninasty.getFaction(entity);
			if(!config.FactionSpawnEntities.ContainsKey(faction))
				config.FactionSpawnEntities.Add(faction,new List<string>());
			if(!config.FactionSpawnEntities[faction].Contains(entity.ShortPrefabName))
				config.FactionSpawnEntities[faction].Add(entity.ShortPrefabName);
			if(!spawnCounts.ContainsKey(faction))
				spawnCounts.Add(faction,0);
			spawnCounts[faction]= Math.Max(spawnCounts[faction]-1,0);
			
			return null;
		}
		object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info){
			if(info.HitEntity==null||info.Initiator==null) return null;
			if(info.HitEntity.gameObject==null||info.Initiator.gameObject==null) return null;
			bool? returnvar = true;
			#region squadding
				if(omninasty.isFriend(info.Initiator, info.HitEntity) )
				{
					if (info.damageTypes.Has(Rust.DamageType.Heat) ){
					
						if(info.HitEntity is HumanNPC){
							if(((HumanNPC)info.HitEntity).creatorEntity==null){
								((HumanNPC)info.HitEntity).creatorEntity=((BasePlayer)info.Initiator);
							}else{						
								((HumanNPC)info.HitEntity).creatorEntity=null;
							}
						}
						if(info.HitEntity is BaseAnimalNPC){
							if(((BaseAnimalNPC)info.HitEntity).creatorEntity==null){
								((BaseAnimalNPC)info.HitEntity).creatorEntity=((BasePlayer)info.Initiator);
							}else{						
								((BaseAnimalNPC)info.HitEntity).creatorEntity=null;
							}
						}
						return false;
					}	
				}
				#endregion squadding
			
			#region factionScores
				//If valid for faction variation
				if(isFaction(info.Initiator)&&isFaction(info.HitEntity)){
					//Get details to apply score
					string faction1 = omninasty.getFaction(info.Initiator);
					string faction2 = omninasty.getFaction(info.HitEntity);
					float damage = info.damageTypes.Total();
					//If player, added personal score logic
					if(isPlayer(info.Initiator)){
						//Player Info
						BasePlayer bp = info.Initiator as BasePlayer;
						string id = bp.userID.ToString();
						
						//Validate safe
						if(!config.FactionScores.ContainsKey(id))
							config.FactionScores.Add(id,new Dictionary<string,float>());
						if(!config.FactionScores.ContainsKey(faction2))
							config.FactionScores.Add(faction2,new Dictionary<string,float>());
						if(!config.FactionScores[id].ContainsKey(faction2))
							config.FactionScores[id].Add(faction2,0);
						
						//Apply direct score
						config.FactionScores[id][faction2]-=damage;
						
						//Apply relational score 
						foreach(string s in config.FactionScores[faction2].Keys){
							if(config.FactionScores[faction2][s]<0){
								if(!config.FactionScores[id].ContainsKey(s))
									config.FactionScores[id].Add(s,0);
								config.FactionScores[id][s]+=damage/10;								
							}else{
								if(!config.FactionScores[id].ContainsKey(s))
									config.FactionScores[id].Add(s,0);
								config.FactionScores[id][s]-=damage/10;										
							}
						}
						//Regen
						regenAlliances(bp);
					}
					//Ignore friendly fire
					if(faction1!=faction2){
						//Validate safe for interfaction
						if(!config.FactionScores.ContainsKey(faction1))
							config.FactionScores.Add(faction1,new Dictionary<string,float>());
						if(!config.FactionScores.ContainsKey(faction2))
							config.FactionScores.Add(faction2,new Dictionary<string,float>());
						if(!config.FactionScores[faction2].ContainsKey(faction1))
							config.FactionScores[faction2].Add(faction1,0f);
						if(!config.FactionScores[faction1].ContainsKey(faction2))
							config.FactionScores[faction1].Add(faction2,0f);					
						//Apply relational score 
						foreach(string s in config.FactionScores[faction2].Keys){
							if(s==faction1)continue;

							if(config.FactionScores[faction2][s]< 0){
								if(!config.FactionScores[faction1].ContainsKey(s))
									config.FactionScores[faction1].Add(s,0);
								config.FactionScores[faction1][s]+=damage/1000;								
							}else{
								if(!config.FactionScores[faction1].ContainsKey(s))
									config.FactionScores[faction1].Add(s,0);
								config.FactionScores[faction1][s]-=damage/1000;										
							}
						}
						//Apply and regen for interfaction
						config.FactionScores[faction1][faction2]-=damage/100;	
						config.FactionScores[faction2][faction1]+=damage/1000;	
						regenAlliances(faction1);
						regenAlliances(faction2);
					}
					
				}
				#endregion
			
			
			//set hostile
			float b = UnityEngine.Time.realtimeSinceStartup + 60f;
			if(returnvar==true)((BaseCombatEntity)info.Initiator).unHostileTime = Math.Max(((BaseCombatEntity)info.Initiator).unHostileTime , b);
			
			
			if((returnvar==null || returnvar == false ) && info.HitEntity is HumanNPC){
				return false;
			}else{
				return null;
			}
		}
		void OnEntitySpawned(HumanNPC be){
			string faction = omninasty.getFaction(be);
			if(!config.FactionSpawnEntities.ContainsKey(faction))
				config.FactionSpawnEntities.Add(faction,new List<string>());
			if(!config.FactionSpawnEntities[faction].Contains(be.ShortPrefabName))
				config.FactionSpawnEntities[faction].Add(be.ShortPrefabName);
			if(!spawnCounts.ContainsKey(faction))
				spawnCounts.Add(faction,0);
			spawnCounts[faction]++;
		}
		void OnEntitySpawned(BaseNpc be){
			string faction = omninasty.getFaction(be);
			if(!config.FactionSpawnEntities.ContainsKey(faction))
				config.FactionSpawnEntities.Add(faction,new List<string>());
			if(!config.FactionSpawnEntities[faction].Contains(be.ShortPrefabName))
				config.FactionSpawnEntities[faction].Add(be.ShortPrefabName);
			if(!spawnCounts.ContainsKey(faction))
				spawnCounts.Add(faction,0);
			spawnCounts[faction]++;
		}
		
		
		void OnOpenVendingShop(VendingMachine machine, BasePlayer player){
			if(machine is InvisibleVendingMachine){
				if(machine.transform.parent==null){return;}
				BaseAIBrain hn = machine.transform.parent.gameObject.GetComponent<BaseAIBrain>();
				if(hn.states!=null){
					if(hn.states[AIState.Roam]!=null){
						Boids.CustomBaseRoamState fbrs = hn.states[AIState.Roam] as Boids.CustomBaseRoamState;
						if(fbrs != null){
							fbrs.waitTime=60;
							fbrs.StateLeave(hn,machine.transform.parent.gameObject.GetComponent<BaseEntity>());
						}
					}					
				}			
			}
		}
		void OnVendingTransaction(VendingMachine machine, BasePlayer buyer, int sellOrderId, int numberOfTransactions)
		{
			string buyerFaction = omninasty.getFaction(buyer);
			
			string storeFaction="";
			
			string id = buyer.userID.ToString();
			int amount = 0;
			ProtoBuf.VendingMachine.SellOrder so = machine.sellOrders.sellOrders[sellOrderId];
			amount = (so.currencyID==scrap?so.currencyAmountPerItem:so.itemToSellAmount);
			
			if(machine is InvisibleVendingMachine &&machine.transform.parent!=null){
				storeFaction = omninasty.getFaction(machine.transform.parent.GetComponent<BaseEntity>());
			}else{
				BaseEntity[] results = new BaseEntity[10];
				BaseEntity.Query.Server.GetInSphere(machine.transform.position, 50, results,new Func<global::BaseEntity, bool>(isFaction));
				Dictionary<string,int> scores = new Dictionary<string,int>();
				foreach(BaseEntity be in results){
					string npcFaction = omninasty.getFaction(be);
					if(scores.ContainsKey(npcFaction)){
						scores[npcFaction]++;
					}else{
						scores[npcFaction]=1;
					}
				}
				
				foreach(string s in scores.Keys.ToArray()){
					List<string> alliances = omninasty.getAlliances(s);
					bool safe=true;
					foreach(string s2 in scores.Keys.ToArray()){
						if(s!=s2){
							safe=safe&&alliances.Contains(s2);
						}
					}
					if(safe && (storeFaction==""||scores[s]>scores[storeFaction]))
						storeFaction=s;
				}
			}
			Puts(buyerFaction+" is buying from "+storeFaction);
			if(storeFaction!="" && buyerFaction!=""){
				if(!config.FactionBanks.ContainsKey(storeFaction))
					config.FactionBanks.Add(storeFaction,0);
				
				if(!config.FactionScores.ContainsKey(id)){
					config.FactionScores.Add(id,new Dictionary<string,float>());
				}
				if(!config.FactionScores.ContainsKey(buyerFaction)){
					config.FactionScores.Add(buyerFaction,new Dictionary<string,float>());
				}
				if(!config.FactionScores.ContainsKey(storeFaction)){
					config.FactionScores.Add(storeFaction,new Dictionary<string,float>());
				}
				
				if(!config.FactionScores[id].ContainsKey(storeFaction)){
					config.FactionScores[id].Add(storeFaction,0);
				}
				if(!config.FactionScores[buyerFaction].ContainsKey(storeFaction)){
					config.FactionScores[buyerFaction].Add(storeFaction,0);
				}
				if(!config.FactionScores[storeFaction].ContainsKey(buyerFaction)){
					config.FactionScores[storeFaction].Add(buyerFaction,0);
				}
				
				config.FactionBanks[storeFaction]+=amount;
				config.FactionScores[id][storeFaction]+=amount/100f;
				config.FactionScores[buyerFaction][storeFaction]+=amount/1000f;
				config.FactionScores[storeFaction][buyerFaction]+=amount/1000f;
				regenAlliances(buyer);
				regenAlliances(buyerFaction);
				regenAlliances(storeFaction);
			}
			return;
		}
		bool isFaction(BaseEntity be){
			return (be is BasePlayer)||(be is BaseNpc);
		}
		bool isPlayer(BaseEntity be){
			BasePlayer basePlayer = be as BasePlayer;
			return basePlayer!=null && basePlayer.HasPlayerFlag(BasePlayer.PlayerFlags.Connected);
		}
		string regenAlliances(BasePlayer basePlayer){			
			if(isPlayer(basePlayer)){
				string id = basePlayer.userID.ToString();
				string max = regenAlliances(id);
				omninasty.setFaction(basePlayer,max);
				return max;
			}else{
				return regenAlliances(omninasty.getFaction(basePlayer));
			}
		}
		string regenAlliances(string faction){
			if(!config.FactionScores.ContainsKey(faction)){
				config.FactionScores.Add(faction,new Dictionary<string,float>());
			}
			string max = "Player";
			foreach(string s in config.FactionScores[faction].Keys){
				if(config.FactionScores[faction][s]>0){
					if(max=="Player" || config.FactionScores[faction][s] > config.FactionScores[faction][max])
						max=s;
					omninasty.addAlliance(s,faction);
				}
				else{
					omninasty.deleteAlliance(s,faction);
				}
			}
			return max;
		}
		static bool RadialPoint(out Vector3 outvect, Vector3 target, Vector3 self,float minDist = 5,float maxDist=8){
				bool destRes = false;
				float dist = UnityEngine.Random.Range(minDist,maxDist);
				float angle =UnityEngine.Random.Range(-360f,360f);
				float x = dist * Mathf.Cos(angle * Mathf.Deg2Rad);
				float y = dist * Mathf.Sin(angle * Mathf.Deg2Rad);
				Vector3 newPosition = target;
				newPosition.x += x;
				newPosition.z += y;
				newPosition.y = TerrainMeta.HeightMap.GetHeight(newPosition);
				outvect = newPosition;
				return true;
			
		}
	}
}