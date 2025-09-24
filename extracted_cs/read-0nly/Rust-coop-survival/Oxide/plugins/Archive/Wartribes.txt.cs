/*

top rank picks one of 3 modes - defend, sustain, expand
Next rank picks target monument
next rank picks pathing monuments
next rank picks path

Monuments have array of connected monuments with road segment array path between monuments
from point to point of road/monument to road/monument

class Rank

	executePathing
	
	Parent
	
	addStates();
	loadDesign();


class RankState : BasicAIState{
	public bool SwapHumanState(BaseAIBrain brain,AIState stateType,BaseAIBrain.BasicAIState state){/*

    [ServerVar(Help = "Show user info for players on server.")]
    public static void users(ConsoleSystem.Arg arg)
    {
*/

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
	[Info("Wartribes", "obsol", "0.0.1")]
	[Description("Runs in permanent idle, allowing custom think behavior to be injected easily")]
	public class Wartribes : CovalencePlugin
	{
		public static Wartribes INSTANCE;

		public class TribeNavigationPoint{
			
			public MonumentInfo monument_info;
			public AIInformationZone ai_zone;
			
			public float radius;
			public float local_center;
			public float world_center;
			
			public AIMovePointPath move_point_paths;
			public List<AIMovePoint> move_points;
			public List<Vector3> transit_points;
			public List<TribeNavigationConnection> connection_points;
			
		}
		public struct TribeNavigationConnection{
			public TribeNavigationPoint a;
			public TribeNavigationPoint b;
			public int a_index;
			public int b_index;
		}

		public void OnServerInitialized(){
			INSTANCE=this;
		}
		
		public class ItemStock{
			public int item_id;
			public int amount;
			public ulong skin;
			public List<ItemStock> contents;
			
			public Item spawn_item(){
				
			}
		}		
		public class TribeKit{
			public string kit_name
			public Tribe tribe;
			public Rank rank;
			public List<ItemStock> armor;
			public List<ItemStock> bag;
			public List<ItemStock> main;
			public List<ItemStock> belt;
			
			public void equip_player(BasePlayer bp){
				
			}
		}
		
		public class Tribe{
			public string name;
			public Dictionary<uint,float> player_relationships;
			public Dictionary<uint,string> player_ranks;
			public Dictionary<string,float> alliances;
			public List<TribeKit> tribe_kits;
			
			
			
		}
		
		public class Rank{
			public Tribe self;
			public Rank leader;
			public List<Rank> followers;
			public string name;
			public int level;
			public string token;
			public static Dictionary<Tribe,Dictionary<string,Rank>> TRIBE_RANKS = new Dictionary<Tribe,Dictionary<string,Rank>>();
			public List<TribeKit> preferred_kits;
			public List<States> brain_states;
			public List<AIStateContainer> state_containers;
			public List<NPCPlayer> rank_members;
			public RankOrder current_order;
			public Queue orders;
			
			
		}
		public struct RankOrderTarget{
			public AIInformationZone target_zone;
			public AIMovePointPath target_path;
			public AIMovePoint target_path;
			public List<AICoverPoint> target_cover;
			public int target_level;
			public int target_followers;
			public float start_time;
			public float end_time;
		}
		public class RankOrder{
			public RankOrderTarget target
			public List<Rank> active_followers;
			public static int max_order_depth = 3;
			
		}
		
		public class RankSetup{
			
		}
		
		public bool SwapState(BaseAIBrain brain,AIState stateType,BaseAIBrain.BasicAIState state){
			try{
				bool isInState = false;
				if (brain.CurrentState != null)
					if (brain.CurrentState.StateType == stateType)
					{
						brain.states[stateType].StateLeave(brain, brain.Navigator.BaseEntity);
						isInState = true;

                    }
				BaseAIBrain.BasicAIState state2 = (BaseAIBrain.BasicAIState)System.Activator.CreateInstance(state.GetType());
				
				System.Console.ForegroundColor = ConsoleColor.Cyan;
				System.Console.WriteLine(state.GetType().ToString() + " Set for "+brain.transform.name);
				System.Console.ResetColor();
				state2.brain = brain;
				brain.states[stateType]=state2;

                if(isInState) brain.SwitchToState(stateType, 0);
                return true;
			}catch(Exception e){
				Puts(e.ToString());
				return false;
			}
		}
	}
}