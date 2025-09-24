

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
using Oxide.Game.Rust.Cui;
using CompanionServer.Handlers;
#endregion
namespace Oxide.Plugins{// Requires: Navmesher
	[Info("FactionSystem", "obsol", "0.2.1")]
	[Description("Turns NPC violence on and controls their targeting per faction system. adds feature to vending machines that allows buying faction allegiance with ID tags dropped by players and NPCs. Allegiance comes with faction perks.")]
	public class FactionSystem: CovalencePlugin{
		
		List<string> Factions= new List<String>(){"Bandit","Scientist","Tunnel Dweller", "Underwater Dweller", "Gas Rats"};
		public class VendorPanel{
			
			
		}
		
		public class Faction{
			/*
				Invokes timer that spawns entities at patrol points of zones under faction control.
				Each NPC and player has invisble shopkeep.
			*/
			public string name;
			public HashSet<Candidate> member_entity_names = new HashSet<Candidate>();
			public List<ZoneAssignment> homes = new List<ZoneAssignment>();
			public HashSet<ZoneAssignment> monuments = new HashSet<ZoneAssignment>();
			public HashSet<ZoneAssignment> assignments = new HashSet<ZoneAssignment>();
			public HashSet<FactionAlignment> alignments = new HashSet<FactionAlignment>();
			public static HashSet<Faction> all = new HashSet<Faction>();
			public HashSet<ProtoBuf.VendingMachine.SellOrder> economy=new HashSet<ProtoBuf.VendingMachine.SellOrder>();
			public HashSet<LoadoutEntry> loadouts=new HashSet<LoadoutEntry>();
			//public string name3;
			
			public class Candidate{
				public string rank;
				public string faction;
				//1 Major - Company > 3-7 Lieutenant - Platoons > 2- 4 Squad Leader > 2 (Fire) Team Leaders > 4 Infantry (9*3=27*5=135)
				//1 Corporation > 3-7 Branches manager > 2-4 Regional Managers > 2 Store Manager > 4 Staff
				
				public HashSet<string> entities = new HashSet<string>(); //entity names for spawn
				public HashSet<string> child_ranks = new HashSet<string>(); //names of rank of entities below
				public HashSet<string> parent_ranks = new HashSet<string>(); //names of rank of entities above
				
				public int population_limit;
				
				public bool spawn(bool sub_entities = false){
					//TODO: Implement spawn logic. 
					return false;
				}
				public class Instance(){
					public NPCPlayer player = null;
					public NPCPlayer parent = null;
					public string source_candidate;
					public string current_candidate;
					public float subspawn_timeout=-1;
					public HashSet<NPCPlayer> children = new HashSet<NPCPlayer>();
					ZoneAssignment current_assignment;
					
				
					public NPCPlayer spawn(string entity_name){
						//TODO: Implement spawn logic. 
						return player;
					}	
				}
			}
			
		}
		
		public class LoadoutEntry{
			public string name;
			public float likelihood = 1f;
			public string rank;
			public List<ProtoBuf.Item> belt = new List<ProtoBuf.Item>();
			public List<ProtoBuf.Item> clothing = new List<ProtoBuf.Item>();
			public List<ProtoBuf.Item> bag = new List<ProtoBuf.Item>();
			public List<ProtoBuf.Item> backpack = new List<ProtoBuf.Item>();			
		}
		
		public class FactionAlignment{
			public ulong id;
			public string name;
			public BasePlayer player;
			public float hostileTimeout;
			public float level;
			public int balance;
			public int cost;
			public FactionAlignment(BasePlayer target_player){
				id=target_player.userID;
				name=target_player.displayName;
				player=target_player;
				hostileTimeout=0;
				level=0;
				cost=21;
			}
			public int increment_level(){
				cost=(int)Mathf.Round(cost*1.6f);
				return cost;
			}
			public float update_balance(int amount){
				balance+=amount;
				int sign = (int)Mathf.Sign(balance);
				while(balance*sign>cost){					
					level+=sign;
					balance-=cost*sign;
				}
				return level;
			}
			public static implicit operator ulong(FactionAlignment d){ //returns id
				return d.id;
			}
			public static implicit operator string(FactionAlignment d){ //returns name
				return d.name;
			}
			public static implicit operator float(FactionAlignment d){ //returns level
				
				return (hostileTimeout< UnityEngine.Time.realtimeSinceStartup? d.level : -1);
			}
			public static implicit operator int(FactionAlignment d){ //returns balance
				
				return balance;
			}
		}
		
		//TODO come up with tranit behavior
		/*
		public class AizTransit{
			public class TransitPoint : AIMovePoint{
				public AIInformationZone from_zone;
				public AIInformationZone to_zone;
				public AIMovePath connection_from;
				public int connectionFromIndex;
				public AIMovePath.PathDirection connectionFromDirection;
			}
		}*/
		
		public class ZoneAssignment{
			public static Dictionary<Faction,HashSet<ZoneAssignment>> all_assignments = new  Dictionary<Faction,HashSet<ZoneAssignment>>();
			public static Dictionary<ZoneAssignment,HashSet<Faction>> all_assignments_inverse = new  Dictionary<ZoneAssignment,HashSet<Faction>>();
			public HashSet<NPCPlayer> assigned_players = new HashSet<NPCPlayer>();
			public AIInformationZone assigned_zone;
			public Candidate rank_needed;
			public int amount_needed;
			public float assignment_timeout;
			//public List<AIInformationZone> path_to_zone = new List<AIInformationZone>();
			
			
		}
		
		
	}
}