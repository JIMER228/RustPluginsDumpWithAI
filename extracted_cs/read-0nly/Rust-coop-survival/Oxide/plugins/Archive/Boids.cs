// Requires: AIZLiberator
// Requires: Cordyceps
// Requires: VehicleHandler
// Requires: Omninasty
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
	[Info("Boids", "obsol", "0.2.1")]
	[Description("Takes over roaming logic to allow for herding")]
	public class Boids : RustPlugin{
		Cordyceps cordy;
		VehicleHandler vehicleHandler;
		public static Omninasty omninasty;
		public static Dictionary<string,List<BoidPoint>> points = new Dictionary<string,List<BoidPoint>>();
		
		
		[ConsoleCommand("boid.save")]
		private void surv_saveconfig(ConsoleSystem.Arg arg){
			SaveConfig();
			Puts("Boid Saved");
		}
		[ConsoleCommand("boid.load")]
		private void surv_loadconfig(ConsoleSystem.Arg arg){
			LoadConfig();
			Puts("Boid Loaded");
		}
	
		public static int Count = 0;
		
		public static Dictionary<string,MonumentInfo> homes = new Dictionary<string,MonumentInfo>();
		
		public static Configuration config;
		public class Configuration{
			[JsonProperty("Attract",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float Attract = 200;
			[JsonProperty("Align",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float Align = 30;
			[JsonProperty("Repulse",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float Repulse = 3;
			[JsonProperty("PointAttract",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float PointAttract = 200;
			[JsonProperty("PointFactor",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float PointFactor = 2;
			[JsonProperty("AttractionFactor",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float AttractionFactor = 2;
			[JsonProperty("CohesionFactor",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float CohesionFactor = 2.5f;
			[JsonProperty("AttractOptimal",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float AttractOptimal = 50;
			[JsonProperty("AttractHome",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float AttractHome = 10;
			[JsonProperty("HomeOptimal",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float HomeOptimal = 20;
			
			
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
		
		
		public class BoidPoint{
			public string faction = "";
			public Vector3Int position;
			public Vector3 direction;
			public float attraction;
			public float redirect;
			public float timeout;
		}
		
		public void addPoint(string faction, Vector3 position, Vector3 direction, float attraction = 1, float redirect = 0, float timeout = 600){
			BoidPoint bp = new BoidPoint();
			bp.faction=faction;
			bp.position=new Vector3Int((int)position.x,(int)position.y,(int)position.z);
			bp.direction=direction;
			bp.attraction=attraction;
			bp.redirect=redirect;
			bp.timeout=timeout+ Time.time;
		}
		void OnServerInitialized(){
			
		}
		void Unload(){
			SaveConfig();
		}
		void Loaded(){
			
			cordy = (Cordyceps)Manager.GetPlugin("Cordyceps");
			omninasty = (Omninasty)Manager.GetPlugin("Omninasty");
			vehicleHandler = (VehicleHandler)Manager.GetPlugin("VehicleHandler");
			cordy.WalkableOnly = false;
			VehicleHandler.AIZRoam = false;
			loadStates();
			LoadConfig();
			
		}
		void loadStates(){
			string[] targets = new string[]{"assets/rust.ai/agents/npcplayer/humannpc/banditguard/npc_bandit_guard.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_any.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_lr300.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_mp5.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_pistol.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_shotgun.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_heavy.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_junkpile_pistol.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_peacekeeper.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roam.prefab"};
			foreach(string s in targets){
				CustomBaseRoamState cbrs= new CustomBaseRoamState();
				cordy.AssignHumanState(s, cbrs.StateType, cbrs);
			}			
		}
		
		public class CustomBaseRoamState : VehicleHandler.CustomBaseRoamState{
			public global::StateStatus status = global::StateStatus.Error;
			public global::AIMovePointPath path;
			public global::AIMovePoint currentTargetPoint;
			public float currentWaitTime;
			public Vector3 lastLocation = new Vector3(0,0,0);
			public float waitTime = 5;
			public float defaultWaitTime=5;
			public int stuckCount = 0;
			public int currentNodeIndex;
			public string faction="";
			public List<string> alliances = new List<string>();
			public CustomBaseRoamState() : base(){
			}
			public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity){
				base.StateEnter(brain,entity);
				MonumentInfo home;
				if(homes.ContainsKey(entity.GetType().ToString())){
					home = homes[entity.GetType().ToString()];
				}else{
					homes.Add(entity.GetType().ToString(), ((MonumentInfo[])Shuffle(TerrainMeta.Path.Monuments.ToArray()))[0]);
					home = homes[entity.GetType().ToString()];
				}
				
				float distanceToHome = Vector3.Distance(home.Bounds.center, entity.transform.position);
				Vector3 resultVector = entity.transform.forward.normalized+( (home.Bounds.center - entity.transform.position).normalized * (config.HomeOptimal/(Mathf.Abs(config.HomeOptimal - distanceToHome)+1) )*config.AttractHome);
				
				if(entity!=null && faction=="")
					faction = omninasty.getFaction(entity);
				alliances = omninasty.getAlliances(faction);
				
				List<BoidPoint> validPoints = new List<BoidPoint>();
				if(!points.ContainsKey(faction))
					points.Add(faction, new List<BoidPoint>());				
				foreach(BoidPoint bp in points[faction].ToArray()){
					if(bp.timeout< Time.time){
						points[faction].Remove(bp);
						continue;
					}
					if(Vector3.Distance(entity.transform.position, bp.position)<config.PointAttract){
						validPoints.Add(bp);
					}
					
				}	
				
				foreach(string alliance in alliances){
					if(!points.ContainsKey(alliance))
						points.Add(alliance, new List<BoidPoint>());		
					foreach(BoidPoint bp in points[alliance]){
						if(bp.timeout< Time.time){
							points[alliance].Remove(bp);
							continue;
						}
						if(Vector3.Distance(entity.transform.position, bp.position)<config.PointAttract){
							validPoints.Add(bp);
						}
					}
					
				}
				
				
				foreach(BoidPoint bp in validPoints){
					float distance = Vector3.Distance(entity.transform.position, bp.position);
					Vector3 direction = (bp.position - entity.transform.position).normalized * config.PointFactor * bp.attraction * (config.PointAttract/distance);
					Vector3 alignment = bp.direction.normalized * config.PointFactor * bp.redirect * (config.PointAttract/distance);
					resultVector+=direction+alignment;
				}
				
				
				
				this.brain.Navigator.ClearFacingDirectionOverride();
				int i=0;
				this.currentWaitTime =0;
				bool flag = false;
				if (WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false) > 0.8f)
				{
					(entity as BasePlayer).Hurt(10f);
					flag=true;
				}
				if(this.brain==null){
					return;
				}		
				if(this.brain.Navigator==null){
					return;}
				if(this.brain.Navigator.BaseEntity==null){
					return;
				}		
				if(!flag)
					lastLocation = this.brain.Navigator.BaseEntity.gameObject.transform.position;
				
				if(this.brain.Navigator.BaseEntity.creatorEntity){
					if(this.brain.Navigator.BaseEntity.creatorEntity.transform){
						RadialPoint(this.brain.Navigator,this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position);						
						return ;
					}
				}
				
				this.brain.Navigator.Path=null;
				
				foreach(BaseCombatEntity bce in brain.Senses.Memory.Friendlies){
					if(bce==null||bce.transform==null){continue;}
					float distance = Vector3.Distance(entity.transform.position,bce.transform.position);
					float AttractDistance = Mathf.Abs(distance-config.AttractOptimal)/config.AttractOptimal;
					float RepelDistance = (config.Repulse-distance)/config.Repulse;
					if(distance < config.Attract){
						resultVector+=(bce.transform.forward).normalized*config.CohesionFactor*((config.Attract-distance)/config.Attract);
						if(distance<config.Repulse){
							resultVector+=(entity.transform.position - bce.transform.position).normalized*RepelDistance*config.AttractionFactor;
						}else{
							resultVector+=(bce.transform.position - entity.transform.position).normalized*AttractDistance*config.AttractionFactor;
						}
						
					}
				}
				resultVector.y=0;
				resultVector+= resultVector.normalized;
				if(waitTime==defaultWaitTime){
					if(stuckCount>3){
						Vector3 position;
						RadialPoint(out position, this.brain.transform.position+(resultVector*3),this.brain.transform.position,1,2);
						stuckCount=0;
						this.brain.Navigator.Warp(position);
					}
					else{
						RadialPoint(this.brain.Navigator, (flag?lastLocation:this.brain.transform.position+(resultVector*7)),this.brain.transform.position,2,3);
					}
					
					}
				status=global::StateStatus.Running;
				return;
			
			}
			public override global::StateStatus StateThink(float delta,global::BaseAIBrain brain, global::BaseEntity entity)
			{
				this.currentWaitTime += delta;
				if(!(this.brain.Navigator.BaseEntity.creatorEntity==null)){
					if(!(this.brain.Navigator.BaseEntity.creatorEntity.transform==null)){
						if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 10f || this.currentWaitTime >= waitTime)
						{//
							return global::StateStatus.Finished;
						}else{
							return global::StateStatus.Running;
						}
					}
				}
				if (this.currentWaitTime >= waitTime)
				{///
					if(Vector3.Distance(lastLocation,this.brain.Navigator.BaseEntity.gameObject.transform.position)<2){
						stuckCount++;
					};
					return global::StateStatus.Finished;
				}else{
					return global::StateStatus.Running;
				}
				
			}
			
			public override void StateLeave(global::BaseAIBrain brain, global::BaseEntity entity)
			{ 
				waitTime=defaultWaitTime;
				base.StateLeave(brain,entity);
			}
			
		}
		public static System.Random rng = new System.Random(); 
		public static UnityEngine.Object[] Shuffle(UnityEngine.Object[] list,int seed)  
		{  
			int n = list.Length;  
			while (n > 1) {  
				n--;  
				int k = rng.Next(n + 1);  
				UnityEngine.Object v = list[k];  
				list[k] = list[n];  
				list[n] = v;  
			}  
			return list;
		}
		public static System.Object[] Shuffle(System.Object[] list)  
		{  
			int n = list.Length;  
			while (n > 1) {  
				n--;  
				int k = rng.Next(n + 1);  
				System.Object v = list[k];  
				list[k] = list[n];  
				list[n] = v;  
			}  
			return list;
		}
		

		
		static bool RadialPoint(BaseNavigator nav, Vector3 target, Vector3 self,float minDist = 5,float maxDist=8){
				bool destRes = false;
				float dist = UnityEngine.Random.Range(minDist,maxDist);
				float angle = (180+nav.transform.eulerAngles.y) + UnityEngine.Random.Range(-60f,60f);
				float x = dist * Mathf.Cos(angle * Mathf.Deg2Rad);
				float y = dist * Mathf.Sin(angle * Mathf.Deg2Rad);
				Vector3 newPosition = target;
				newPosition.x += x;
				newPosition.z += y;
				newPosition.y = TerrainMeta.HeightMap.GetHeight(newPosition);
				//newPosition.y = Terrain.activeTerrain.SampleHeight(newPosition);
				float distance = Vector3.Distance(newPosition,self);
				if (distance<5f){
					 destRes=nav.SetDestination(newPosition, global::BaseNavigator.NavigationSpeed.Slow, 0f, 0f);
					return destRes;
					}
				else if (distance<25f){
					 destRes=nav.SetDestination(newPosition, global::BaseNavigator.NavigationSpeed.Normal, 0f, 0f);
					return destRes;
					}
				else{
					destRes=nav.SetDestination(newPosition, global::BaseNavigator.NavigationSpeed.Fast, 0f, 0f);							
					return destRes;
					}							
				return destRes;	
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