// Requires: Cordyceps
// Requires: Navmesher
// Requires: AIZLiberator
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
	[Info("VehicleHandler", "obsol", "0.2.1")]
	[Description("")]
	public class VehicleHandler : RustPlugin{
		
		Cordyceps cordy;
		AIZLiberator aizlib;
		public static bool AIZRoam = true;
		void Loaded(){
			
			cordy = (Cordyceps)Manager.GetPlugin("Cordyceps");
			aizlib = (AIZLiberator)Manager.GetPlugin("AIZLiberator");
			cordy.WalkableOnly = false;
			loadStates();
			
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
				CustomBaseRoamState frs= new CustomBaseRoamState();
				CustomChaseState fcs= new CustomChaseState();
				cordy.AssignHumanState(s, frs.StateType, frs);
				cordy.AssignHumanState(s, fcs.StateType, fcs);
			}			
		}
		
		public class CustomChaseState : AIZLiberator.CustomChaseState{
			public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity)
			{ 
				base.StateEnter(brain,entity);
				if(((entity as BasePlayer).GetMountedVehicle() !=null)){
					if((entity as BasePlayer).GetMountedVehicle().IsMoving())
						return;
					((entity as BasePlayer).GetMounted() as BaseMountable).DismountPlayer(entity as BasePlayer, true);	

				}	
			}
			
		}//
		
		public class CustomBaseRoamState : AIZLiberator.CustomBaseRoamState{
			public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity)
			{ 
				if(!entity is BasePlayer){return;}
				BaseEntity[] results = new BaseEntity[3];
				BaseEntity.Query.Server.GetInSphere(entity.transform.position, 3, results,new Func<global::BaseEntity, bool>(VehicleHandler.isVehicle));
				foreach(BaseMountable bm in results){
					if(bm==null || !bm.IsOn()){continue;}
					bm.AttemptMount(entity as BasePlayer, false);
					if((entity as BasePlayer).GetMountedVehicle()!=null){
						if((entity as BasePlayer).GetMountedVehicle() is BaseVehicle){
							if(((entity as BasePlayer).GetMountedVehicle() as BaseVehicle).IsDriver(entity as BasePlayer)){
								((entity as BasePlayer).GetMountedVehicle() as BaseVehicle).SwapSeats(entity as BasePlayer);							
							}
						}
						break;
					}
				}	
				if(((entity as BasePlayer).GetMountedVehicle() !=null) && ((entity as BasePlayer).GetMountedVehicle() as BaseVehicle).IsDriver(entity as BasePlayer)){
					((entity as BasePlayer).GetMounted() as BaseMountable).DismountPlayer(entity as BasePlayer, true);	

				}	
				if(AIZRoam){
					base.StateEnter(brain,entity);
				}
			}
			
			public override void StateLeave(global::BaseAIBrain brain, global::BaseEntity entity)
			{ 
				if(!entity is BasePlayer){return;}
				BaseEntity[] results = new BaseEntity[3];
				BaseEntity.Query.Server.GetInSphere(entity.transform.position, 3, results,new Func<global::BaseEntity, bool>(VehicleHandler.isVehicle));
				foreach(BaseMountable bm in results){
					if(bm==null || !bm.IsOn()){continue;}
					bm.AttemptMount(entity as BasePlayer, false);
					if((entity as BasePlayer).GetMountedVehicle()!=null){
						if((entity as BasePlayer).GetMountedVehicle() is BaseVehicle){
							if(((entity as BasePlayer).GetMountedVehicle() as BaseVehicle).IsDriver(entity as BasePlayer)){
								((entity as BasePlayer).GetMountedVehicle() as BaseVehicle).SwapSeats(entity as BasePlayer);							
							}
						}
						break;
					}
				}	
				if(((entity as BasePlayer).GetMountedVehicle() !=null) && ((entity as BasePlayer).GetMountedVehicle() as BaseVehicle).IsDriver(entity as BasePlayer)){
					((entity as BasePlayer).GetMounted() as BaseMountable).DismountPlayer(entity as BasePlayer, true);	

				}	
				if(AIZRoam){
					base.StateLeave(brain,entity);
				}
			}
		}
		
		public static bool isVehicle(BaseEntity be){
			return be is BaseMountable;
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
		//