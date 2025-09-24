
// Requires: Cordyceps
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
using System.IO;
using UnityEngine; 
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
using Oxide.Core.Plugins;
using Oxide.Core;

namespace Oxide.Plugins
{
	[Info("NPCRoam", "obsol", "0.0.1")]
	[Description("A baseline for swapping out custom AI state handlers without lobotomies.")]
	public class NPCRoam : CovalencePlugin
	{
		private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
		private void SendChatMsg(BasePlayer pl, string msg) => _rustPlayer.Message(pl, msg,  "<color=#00ff00>[NPCRoam]</color>", 0, Array.Empty<object>());			
		public Configuration config;
		//public LayerMask NPCLayers = 0; //Debug.Log(LayerMask.LayerToName(1));
		
		Cordyceps cordy;
		
		private void Loaded()
		{
			cordy = (Cordyceps)Manager.GetPlugin("Cordyceps");
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
				CustomChaseState ccs= new CustomChaseState();
				CustomRoam cr = new CustomRoam();
				CustomTryCover ctc= new CustomTryCover();
				cordy.AssignHumanState(s, ccs.StateType, ccs);
				cordy.AssignHumanState(s, cr.StateType, cr);
				cordy.AssignHumanState(s, ctc.StateType, ctc);
			}
		}
		
		public class Configuration
		{
			[JsonProperty("Name", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public string name = "";
			public string ToJson() => JsonConvert.SerializeObject(this);				
			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
			
		}
		
		protected override void LoadDefaultConfig() => config = new Configuration();//
		protected override void LoadConfig(){
			base.LoadConfig();
			try{
				config = Config.ReadObject<Configuration>();
				if (config == null) throw new JsonException();
				if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys)){
					LogWarning("Configuration appears to be outdated; updating and saving");SaveConfig();}
			}
			catch(Exception e){LogWarning($"Configuration file {Name}.json is invalid; using defaults");LogWarning(e.ToString());LoadDefaultConfig();}
		}
		
		protected override void SaveConfig(){
			LogWarning($"Configuration changes saved to {Name}.json");
			Config.WriteObject(config, true);
		}	
		
		private void OnServerInitialized()
        {
			LoadConfig();
		}
		
		class CustomRoam : global::BaseAIBrain<HumanNPC>.BaseRoamState
		{
			private global::StateStatus status = global::StateStatus.Error;
			Vector3 LastVector = new Vector3(0,-501,0);
			float LastWet = 0f;
			Vector3 Target = new Vector3(0,0,0);
			Vector3 Root = new Vector3(0,0,0);
			Vector3 Direction = new Vector3(0,0,0);
			Vector3 currentPosition = new Vector3(0,0,0);
			float startTime = 0;
			bool TurnDirection = false;
			float nextThinkTime = 0f;
			BaseNavigator.NavigationSpeed Speed = BaseNavigator.NavigationSpeed.Normal;
			bool hasWarped = false;
			bool stuck = false;
			public CustomRoam() : base(){
				TurnDirection = (Oxide.Core.Random.Range(0, 2) == 1);
				startTime = Time.realtimeSinceStartup;
				nextThinkTime=startTime+1;
			}
			public override void StateEnter(){
				base.StateEnter();
				Root = this.brain.transform.position;
				currentPosition = Root;
				Speed = BaseNavigator.NavigationSpeed.Normal;
				float currentWet = WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false);
				Direction =  (stuck?(-1*this.brain.transform.forward):this.brain.transform.forward);
				stuck=false;
				
				if(this.brain.GroupLeader!=null){
					Root=(this.brain.Events.Memory.Entity.Get(6)).transform.position;
					Target = this.brain.PathFinder.GetRandomPositionAround(Root+Direction,3f, 5f);
				}
				if(this.brain.OwningPlayer!=null){
					Root=(this.brain.OwningPlayer).transform.position;
					Target = this.brain.PathFinder.GetRandomPositionAround(Root+Direction,3f, 5f);
				}
				else {
					if(currentWet > 0f)	{
						Root = LastVector;
						Direction = (TurnDirection?this.brain.transform.right:(-1*this.brain.transform.right));
						Target = this.brain.PathFinder.GetRandomPositionAround(Root-(this.brain.transform.forward*5)+(Direction*5),4f, 5f);
						
						if (currentWet > 80f)
						{
								(this.brain.GetEntity() as BasePlayer)?.Hurt(10f);
								
						}
						
					}else{
						LastWet=currentWet;
						LastVector=this.brain.transform.position;
						Target = this.brain.PathFinder.GetRandomPositionAround(Root+(Direction*10),2f, 5f);						
					}
				}
				if(Vector3.Distance(Target,this.brain.transform.position)<5)
					Speed = BaseNavigator.NavigationSpeed.Slow;
				if(Vector3.Distance(Target,this.brain.transform.position)>15)
					Speed = BaseNavigator.NavigationSpeed.Fast;
				
				if (this.brain.Navigator.SetDestination(Target, Speed, 0f, 0f))
				{
					this.status = global::StateStatus.Running;
				}
				else{
					status = global::StateStatus.Error;					
					if(!hasWarped){
						this.brain.Navigator.Warp(this.brain.transform.position);
						hasWarped=true;
					}
				}
			}
			public override global::StateStatus StateThink(float delta)
			{
				if (this.status == global::StateStatus.Error)
					return this.status;
				if (this.brain.Navigator.Moving && Time.realtimeSinceStartup <(startTime+10))
					return global::StateStatus.Running;
				return global::StateStatus.Finished;
			}
			public override void StateLeave()
			{
				if(Vector3.Distance(currentPosition,this.brain.transform.position)<0.1f)stuck=true;
				base.StateLeave();
				
			}
		}
		
		class CustomTryCover : global::BaseAIBrain<HumanNPC>.BasicAIState
		{
			private global::StateStatus status = global::StateStatus.Error;
			
			public CustomTryCover():base(AIState.TakeCover){}
			
			public override void StateEnter(){
				base.StateEnter();
				if (this.brain.Navigator.SetDestination(this.brain.PathFinder.GetRandomPositionAround(this.brain.transform.position+(
					((-this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot).transform.right)*7)+
					(this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot).transform.forward*5)
					),3f, 7f), global::BaseNavigator.NavigationSpeed.Fast, 0f, 0f))
				{
					this.status = global::StateStatus.Running;
					return;
				}
			}
			public override global::StateStatus StateThink(float delta)
			{
				if (this.status == global::StateStatus.Error)
					return this.status;
				if (this.brain.Navigator.Moving)
					return global::StateStatus.Running;
				return global::StateStatus.Finished;
			}
			
		}
		
		class CustomChaseState : global::BaseAIBrain<global::HumanNPC>.BaseChaseState
		{
			Vector3 LastVector = new Vector3(0,-501,0);
			Vector3 Target = new Vector3(0,0,0);
			Vector3 Root = new Vector3(0,0,0);
			Vector3 Direction = new Vector3(0,0,0);
			float LastWet = 0f;
			float startTime = 0;
			bool TurnDirection = false;
			bool hasWarped = false;
			BaseNavigator.NavigationSpeed Speed = BaseNavigator.NavigationSpeed.Normal;
			public CustomChaseState() : base()
			{
				base.AgrresiveState = true;
				TurnDirection = (Oxide.Core.Random.Range(0, 2) == 1);
				startTime = Time.realtimeSinceStartup;
			}

			public override void StateLeave()
			{
				base.StateLeave();
						
				this.Stop();
			}

			public override void StateEnter()
			{
				base.StateEnter();
				this.status = global::StateStatus.Error;
				if (this.brain.PathFinder == null)
				{
					return;
				}
						
				if (WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false) > 0.8f)
				{
					(this.brain.GetEntity() as BasePlayer).Hurt(10f);
				}
				this.status = global::StateStatus.Running;
				this.nextPositionUpdateTime = 0f;
				global::BaseEntity baseEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
				if (baseEntity != null)
				{
					this.brain.Navigator.SetDestination(this.brain.PathFinder.GetRandomPositionAround(baseEntity.transform.position,5f, 10f), global::BaseNavigator.NavigationSpeed.Fast, 0.25f, 0f);
				}
			}

			private void Stop()
			{
				this.brain.Navigator.Stop();
				this.brain.Navigator.ClearFacingDirectionOverride();
			}

			public override global::StateStatus StateThink(float delta)
			{
				global::BaseEntity baseEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
				if (baseEntity == null)
				{
					return global::StateStatus.Error;
				}
				global::HumanNPC entity = base.GetEntity();
				float num = Vector3.Distance(baseEntity.transform.position, entity.transform.position);
				if (this.brain.Senses.Memory.IsLOS(baseEntity) || num <= 10f || base.TimeInState <= 5f)
				{
					this.brain.Navigator.SetFacingDirectionEntity(baseEntity);
				}
				else
				{
					this.brain.Navigator.ClearFacingDirectionOverride();
				}
				if (num <= 10f)
				{
					this.brain.Navigator.SetCurrentSpeed(global::BaseNavigator.NavigationSpeed.Normal);
				}
				else
				{
					this.brain.Navigator.SetCurrentSpeed(global::BaseNavigator.NavigationSpeed.Fast);
				}
				if (Time.time > this.nextPositionUpdateTime)
				{
					this.nextPositionUpdateTime = Time.time + UnityEngine.Random.Range(0.5f, 1f);			
					
					this.status = global::StateStatus.Running;
					float currentWet = WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false);
					if(currentWet > 0f)	{
						if (currentWet > 0.8f){
								(this.brain.GetEntity() as BasePlayer).Hurt(10f);
						}
						Root = LastVector;
						Direction = (TurnDirection?this.brain.transform.right:(-1*this.brain.transform.right));
						Target = this.brain.PathFinder.GetRandomPositionAround(Root-(this.brain.transform.forward*3)+(Direction*3),7f, 10f);
						if(Vector3.Distance(Target,this.brain.transform.position)<5)
							Speed = BaseNavigator.NavigationSpeed.Slow;
						if(Vector3.Distance(Target,this.brain.transform.position)>15)
							Speed = BaseNavigator.NavigationSpeed.Fast;
						
						if (this.brain.Navigator.SetDestination(Target, Speed, 0f, 0f))
						{
							this.status = global::StateStatus.Running;
							return this.status;
						}
						else{
							status = global::StateStatus.Error;
							if(!hasWarped){
								this.brain.Navigator.Warp(this.brain.transform.position);
								hasWarped=true;
							}
						}
					
					}else{
						LastWet=currentWet;
						LastVector=this.brain.transform.position;				
					}
				}
				if (this.brain.Navigator.Moving)
				{
					return global::StateStatus.Running;
				}
				return global::StateStatus.Finished;
				
				
			}

			private global::StateStatus status = global::StateStatus.Error;

			private float nextPositionUpdateTime;
		}


	}
}

