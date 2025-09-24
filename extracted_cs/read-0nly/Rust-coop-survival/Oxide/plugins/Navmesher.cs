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
using UnityEngine.AI; 
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
using Oxide.Core.Plugins;
using System.Threading;
using Oxide.Core;

namespace Oxide.Plugins
{
	[Info("Navmesher", "obsol", "0.0.1")]
	[Description("Re-bakes everything for Agent 0 and spawns everything Agent 0")]
	public class Navmesher : CovalencePlugin
	{			
		private Type navtype;
		void OnTerrainInitialized(){
			
			foreach (MonumentInfo monumentInfo in TerrainMeta.Path.Monuments)
			{
				if ( 
					!monumentInfo.transform.ToString().ToLower().Contains("oilrig") &&
					!monumentInfo.transform.ToString().ToLower().Contains("underwater")
				)
				{
					//Mark monuments as not having navmesh so agents fallback on greater navmesh
					Puts("Disabling navmesh for "+monumentInfo.transform.name);
					monumentInfo.HasNavmesh=false;
				}
            }
            NextFrame(scan1);
			NextFrame(scan3);
			
			//makes the ocean an obstacle so they can't walk into it - since it's tied to the ocean if the ocean is moved dynamically it should move with it (rising seas)
			GameObject ocean = WaterSystem.Ocean.Transform.gameObject;
            NavMeshObstacle nmo = ocean.GetComponent<NavMeshObstacle>();
            nmo = (nmo == null ? ocean.AddComponent<NavMeshObstacle>() : nmo);
            nmo.shape = NavMeshObstacleShape.Box;
            nmo.center = new Vector3(0, 0, 0);
            nmo.size = new Vector3(4000,0.2f,4000);
            nmo.carving = true;
            nmo.enabled = true;
        }
		void scan1(){
			//This tries to destroy monument navmeshes so that there isn't any conflicts between it and the greater navmesh
			//There are some exceptions that instead will be rebaked to agent 0
			Puts("-----------------SCAN 1-----------------");
			UnityEngine.Object[] GameobjectList = Resources.FindObjectsOfTypeAll(typeof(MonumentNavMesh));
			foreach(MonumentNavMesh go in GameobjectList){
				if( 
					!go.transform.ToString().ToLower().Contains("dungeon") &&
					!go.transform.ToString().ToLower().Contains("aiarena") 
				){
					if ( 
						!go.transform.ToString().ToLower().Contains("oilrig") &&
						!go.transform.ToString().ToLower().Contains("underwater")
					)
					{
						Puts("    Destroying "+go.transform.ToString().ToLower());
						//This doesn't seem to be used but just to be safe - if something wants this navmesh we want it to fail and fallback on the greater navmesh
						MonumentNavMesh.use_baked_terrain_mesh= false;
						UnityEngine.Object.Destroy(go);
					}
					else
					{
						Puts("Not Destroying "+go.transform.ToString().ToLower());
						/*
						go.NavMeshAgentTypeIndex = 0;
						go.agentTypeId = 0;
						//Flipping active resets necessary things to allow navmesh build (OnEnable on the class)
						go.gameObject.SetActive(false);
						go.HasBuildOperationStarted=false;
						go.gameObject.SetActive(true);
						//rebake
						go.UpdateNavMeshAndWait();
						*/
					}
				}
			}
			
		}
		
		void scan3(){
			//I forget what counts as a dynamic navmesh but update navmesh so it encompasses more than what was baked in natively
			Puts("-----------------SCAN 3-----------------");
			UnityEngine.Object[] GameobjectList = Resources.FindObjectsOfTypeAll(typeof(DynamicNavMesh));
			foreach(DynamicNavMesh go in GameobjectList){
				if( 
					!go.transform.ToString().ToLower().Contains("dungeon") &&
					!go.transform.ToString().ToLower().Contains("aiarena") 
				){
					if ( 
						!go.transform.ToString().ToLower().Contains("oilrig") &&
						!go.transform.ToString().ToLower().Contains("underwater")
					)
					{
						Puts("Scanning"+go.transform.ToString().ToLower());
						//We're all Agent 0 here!
						go.NavMeshAgentTypeIndex=0;
						go.agentTypeId=0;
						//Flipping active resets necessary things to allow navmesh build (OnEnable on the class)				
						go.gameObject.SetActive(false);
						go.HasBuildOperationStarted = false;
						go.gameObject.SetActive(true);
						//rebake
						go.UpdateNavMeshAndWait();
					}
				}
			}
			
		}
		void OnEntitySpawned(BaseNpc bn){
			//Navmesh fuckery for animal npc. No animals? Delete this.
			int oldID=0;
			if(bn.GetComponent<BaseNavigator>() ==null ) return;
			//Set navmesh agent id to 0
			if(bn.GetNavAgent!=null)
				bn.GetNavAgent.agentTypeID=0;
			BaseNavigator bnav=bn.GetComponent<BaseNavigator>();
			//Note the old ID in case we want to flip back 
			oldID = bnav.navMeshQueryFilter.agentTypeID;
			//Make sure navmesh is used
			bnav.SetNavMeshEnabled(true);
			//Set the agent id
			bnav.navMeshQueryFilter.agentTypeID= NavMesh.GetSettingsByIndex(0).agentTypeID;
			bnav.Agent.agentTypeID= NavMesh.GetSettingsByIndex(0).agentTypeID;
			//Set the filter mask to 25 (I forget what that ends up standing for)
			bnav.defaultAreaMask = 25;
			bnav.navMeshQueryFilter.areaMask= 25;
			//Resume naviation
            bnav.Resume();
			//Note current position
			Vector3 position = 	bn.transform.position;		
			//Try warping to it to get agent on navmesh. On fail, revert agent ID
			if(!bnav.Warp(position)){						
				bnav.Agent.agentTypeID=oldID;
			}
			//Try warping with original ID, if fail, kill
			if(!bnav.Warp(position)){	
				Puts(((char)27)+"[94m"+"Killing "+bn.transform.name);//
				bn.Kill();
			}
		}
		void OnEntitySpawned(NPCPlayer bn){
				BaseNavigator bnav = bn.GetComponent<BaseNavigator>();
				if(bnav==null)return;
				//Note the old ID in case we want to flip back 
				int oldID = bnav.Agent.agentTypeID;
				//Set the filter mask to 25 (I forget what that ends up standing for)
				bnav.defaultAreaMask = 25;
				bnav.navMeshQueryFilter.areaMask= 25;				
				//Set the agent id
				bnav.navMeshQueryFilter.agentTypeID= NavMesh.GetSettingsByIndex(0).agentTypeID;
				bnav.Agent.agentTypeID= NavMesh.GetSettingsByIndex(0).agentTypeID;
				
				//Try placing on navmesh. On fail, revert agent ID
				Vector3 position = 	bn.transform.position;	
				if(!bnav.PlaceOnNavMesh(0)){						
					bnav.Agent.agentTypeID=oldID;
				}
				//Try placing on navmesh with original ID. if fail, kill
				if(!bnav.PlaceOnNavMesh(0))
				{	
					bn.Kill();
				}				
		}
		private void OnServerInitialized()
        {
			UnityEngine.Object[] bandittrigger= Resources.FindObjectsOfTypeAll(typeof(TriggerBanditZone));
			//This whole thing makes TriggerBanditZone freak out and I don't know what it does anyways so delete it
			foreach(TriggerBanditZone hn in bandittrigger){
				try{GameObject.Destroy(hn);}catch(Exception e){}
				
			}
			scan1();
        }
			
	}
}
