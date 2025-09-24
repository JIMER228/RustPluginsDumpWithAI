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
using System.Globalization;

namespace Oxide.Plugins
{
	[Info("MonuNet", "obsol", "0.0.1")]
	[Description("Abstracts roads as AI zones and links zones together for interzone navigation")]
	public class MonuNet : CovalencePlugin
	{
		public static float INTERSECTION_DISTANCE = 50f;
		public static float CONNECTION_DISTANCE = 50f;
		public static float CONNECTION_STEP_SCALE = 1.5f;
		public static int CONNECTION_STEP_MAX_LOOP = 3;
		public bool hasRun=false;
		public static Type[] move_entity_types={
			typeof(Recycler),
			typeof(RepairBench),
			typeof(ResearchTable),
			typeof(VendingMachine),
			typeof(NPCShopKeeper), 
			typeof(LootContainer), 
			typeof(BaseCardGameEntity)
		};

		public static string[] move_radial_names={
			"mining_quarry",
			"pumpjack"
		};


		public static string[] move_names = {"wooden_walkway_large_edge_corner",
			"wooden_ledge_corner",
			"shopfront",
			"portacabin_building",
			"wooden_cabin",
			"marketplace_terminal",
			"supermarket_shelves",
			"water_drum",
			"desk",
			"cardboard_box",
			"metal_crate",
			"hobobarrel",
			"boattownbarrel",
			"wooden_crate"
		};

		public static string[] cover_names = {"barricade.concrete",
			"barricade.cover.wood",
			"barricade.metal",
			"barricade.sandbags",
			"barricade.stone",
			"barricade.wood",
			"barricade_concrete",
			"barricade_sandbags",
			"barricade_stones",
			"barricade_metal",
			"barricade_wood",
			"v_road_vehicles",
			"electrical_box",
			"busstop",
			"rock_formation_medium",
			"formation_large",
			"boattownbarrel",
			"rowboat_static",
			"metal_crate",
			"cardboardBox",
			"cardboard_box",
			"wooden_crate",
			"water_drum",
			"diesel_engine",
			"forklift",
			"gas_canister_pallet",
			"gas_pump",
			"junkyard_stack",
			"large_industrial_prop",
			"liquid_gas_tank",
			"marketplace_terminal",
			"cubicle_wall",
			"shipping_container",
			"supermarket_counter",
			"supermarket_shelves",
			"tire_stack",
			"trash_bag_pile",
			"pallet_stacks",
			"creosote_bush",
			"portacabin_building",
			"wooden_cabin",
			"truck_cabin",
			"truck_trailer",
			"truck_wreck",
			"door.hinged.bunker",
			"door.hinged.industrial",
			"door.hinged.nms_",
			"door.hinged.underwater",
			"door.hinged.wood",
			"door.hinged.metal",
			"door.hinged.shipping",
			"door.hinged.toptier",
			"door.hinged.tugboat",
			"desk",
			"substation",
			"pipe_pile",
			"scientist_cover",
			"bulk_frame",
			"fuel_tank",
			"bush"
		};

		public static Dictionary<Vector2Int, Dictionary<Vector3Int,Vector3>> cover_candidate_v3 =
			new Dictionary<Vector2Int, Dictionary<Vector3Int,Vector3>>();
		public static Dictionary<Vector2Int, Dictionary<Vector3Int,Vector3>> move_candidate_v3 =
			new Dictionary<Vector2Int, Dictionary<Vector3Int,Vector3>>();
		public static Dictionary<Vector2Int, Dictionary<Vector3Int,Vector3>> move_radial_candidate_v3 =
			new Dictionary<Vector2Int, Dictionary<Vector3Int,Vector3>>();
		
		public static Dictionary<MonumentInfo,Dictionary<string,List<Vector3>>> monument_candidates = 
			new Dictionary<MonumentInfo,Dictionary<string,List<Vector3>>>();
		
		void OnTerrainInitialized(){
			ScanGameObjects();
			hasRun=true;
		}
		
		void ScanGameObjects(){
			
			/*
			if(Road.ZoneCheck(tgtZone)||Warp.ZoneCheck(tgtZone)){
				
			}
			COVER TARGETS: lootcrates, counters, blue and red static barrels, junkpiles, car piles, tire piles, bushes, trees, pallet stacks, wood barrels, boats,spools, big ass tanks,
			sandbags, metal barricades,  Boxes, crates, metal crates, popup barricades
			MOVE TARGETS: Shops, cabins, loot barrels and crates, recyclers, gamba, deployables
			If near a chair during roam sit in it :3
			
			*/
			UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
			Dictionary<string,int> messages = new Dictionary<string,int>();
			float next_msg = UnityEngine.Time.realtimeSinceStartup;
			//string result = "";
			int obj_counter = 0;
			foreach(GameObject gameobj in allObjects){				
				//result+="Object:["+gameobj.transform.name+"]\n";
				float now = UnityEngine.Time.realtimeSinceStartup;
				obj_counter++;bool cover_flag = false;
				bool move_flag = false;
				bool move_radial_flag = false;
				for(int i = 0; i < cover_names.Length;i++){
					if(cover_flag) break;
					cover_flag |= gameobj.transform.name.ToLower().Contains(cover_names[i].ToLower());
					cover_flag |= gameobj.transform.name.ToLower().Contains(cover_names[i].ToLower().Replace("_","."));
					cover_flag |= gameobj.transform.name.ToLower().Contains(cover_names[i].ToLower().Replace("_","").Replace(".",""));
				}
				for(int i = 0; i < move_names.Length;i++){
					if(move_flag) break;
					move_flag |= gameobj.transform.name.ToLower().Contains(move_names[i].ToLower());
					move_flag |= gameobj.transform.name.ToLower().Contains(move_names[i].ToLower().Replace("_","."));
					move_flag |= gameobj.transform.name.ToLower().Contains(move_names[i].ToLower().Replace("_","").Replace(".",""));
				}
				for(int i = 0; i < move_radial_names.Length;i++){
					if(move_radial_flag) break;
					move_radial_flag |= gameobj.transform.name.ToLower().Contains(move_radial_names[i].ToLower());
					move_radial_flag |= gameobj.transform.name.ToLower().Contains(move_radial_names[i].ToLower().Replace("_","."));
					move_radial_flag |= gameobj.transform.name.ToLower().Contains(move_radial_names[i].ToLower().Replace("_","").Replace(".",""));
				}
				if(!cover_flag&&!move_flag&&!move_radial_flag){
					continue;
				}				
				Vector3 pos = gameobj.transform.position;
				Vector2Int grid_sqr = new Vector2Int((int)(pos.x/100f),(int)(pos.z/100f));
				Vector3Int grid_cell = new Vector3Int((int)(pos.x/1f),(int)(pos.y/1f),(int)(pos.z/1f));
				if(cover_flag){
					if(!cover_candidate_v3.ContainsKey(grid_sqr)){
						cover_candidate_v3.Add(grid_sqr,new Dictionary<Vector3Int,Vector3>());
					}
					if(!cover_candidate_v3[grid_sqr].ContainsKey(grid_cell)){
						cover_candidate_v3[grid_sqr].Add(grid_cell,pos);
					}
					else{
						cover_candidate_v3[grid_sqr][grid_cell]=pos;
					}
					if(messages.ContainsKey("Processed cover point")){
						messages["Processed cover point"]++;
					}
					else{
						messages.Add("Processed cover point",1);
					}
					
				}
				if(move_flag){
					if(!move_candidate_v3.ContainsKey(grid_sqr)){
						move_candidate_v3.Add(grid_sqr,new Dictionary<Vector3Int,Vector3>());
					}
					if(!move_candidate_v3[grid_sqr].ContainsKey(grid_cell)){
						move_candidate_v3[grid_sqr].Add(grid_cell,pos);
					}
					else{
						move_candidate_v3[grid_sqr][grid_cell]=pos;
					}
					if(messages.ContainsKey("Processed move point")){
						messages["Processed move point"]++;
					}
					else{
						messages.Add("Processed move point",1);
					}
				}
				if(move_radial_flag){
					if(!move_radial_candidate_v3.ContainsKey(grid_sqr)){
						move_radial_candidate_v3.Add(grid_sqr,new Dictionary<Vector3Int,Vector3>());
					}
					if(!move_radial_candidate_v3[grid_sqr].ContainsKey(grid_cell)){
						move_radial_candidate_v3[grid_sqr].Add(grid_cell,pos);
					}
					else{
						move_radial_candidate_v3[grid_sqr][grid_cell]=pos;
					}
					if(messages.ContainsKey("Processed move_radial point")){
						messages["Processed move_radial point"]++;
					}
					else{
						messages.Add("Processed move_radial point",1);
					}
				}
				if(now>next_msg){
					Puts("Progress on scanning for move and coverpoints:"+ ((int)((obj_counter / (float)allObjects.Length)*100)).ToString());
					foreach(string s in messages.Keys){
						Puts(s+" ["+messages[s]+"]");
					}
					messages.Clear();
					next_msg=now+10;
				}
				
			}
			int cover_count = 0;
			int move_count = 0;
			int move_radial_count = 0;
			foreach(Vector2Int k in cover_candidate_v3.Keys){
				cover_count+=cover_candidate_v3[k].Keys.Count();
			}
			foreach(Vector2Int k in move_candidate_v3.Keys){
				move_count+=move_candidate_v3[k].Keys.Count();
				
			}
			foreach(Vector2Int k in move_radial_candidate_v3.Keys){
				move_radial_count+=move_radial_candidate_v3[k].Keys.Count();
				
			}
			System.Console.ForegroundColor= System.ConsoleColor.DarkCyan;
			System.Console.WriteLine("Final counts!  Cover : "+cover_count+
				" | Move : "+move_count+"| Radial : "+move_radial_count);//
			System.Console.ResetColor();
			
			foreach(MonumentInfo mi in TerrainMeta.Path.Monuments){
				if(monument_candidates.ContainsKey(mi)){
					continue;
				}
				else{
					monument_candidates.Add(mi,new Dictionary<string,List<Vector3>>());
					monument_candidates[mi].Add("cover",new List<Vector3>());
					monument_candidates[mi].Add("move",new List<Vector3>());
					monument_candidates[mi].Add("move_radial",new List<Vector3>());
					Vector2Int mipos2 = new Vector2Int(((int)(mi.transform.position.x/100))-1,((int)(mi.transform.position.z/100))-1);
					for(int i = 0; i < 3; i++){
						for(int j = 0; j < 3; j++){
							Vector2Int this_key = new Vector2Int(mipos2.x+i,mipos2.y+j);
							if(cover_candidate_v3.ContainsKey(this_key)){
								foreach(Vector3Int key in cover_candidate_v3[this_key].Keys){
									if(mi.IsInBounds(cover_candidate_v3[this_key][key])){
										monument_candidates[mi]["cover"].Add(cover_candidate_v3[this_key][key]);
									}
									
								}
							}
							if(move_candidate_v3.ContainsKey(this_key)){
								foreach(Vector3Int key in move_candidate_v3[this_key].Keys){
									if(mi.IsInBounds(move_candidate_v3[this_key][key])){
										monument_candidates[mi]["move"].Add(move_candidate_v3[this_key][key]);
									}
									
								}
							}
							if(move_radial_candidate_v3.ContainsKey(this_key)){
								foreach(Vector3Int key in move_radial_candidate_v3[this_key].Keys){
									if(mi.IsInBounds(move_radial_candidate_v3[this_key][key])){
										monument_candidates[mi]["move_radial"].Add(move_radial_candidate_v3[this_key][key]);
									}
								}
							}
						}	
					}
					
				}
			}
			cover_candidate_v3.Clear();
			move_candidate_v3.Clear();
			move_radial_candidate_v3.Clear();
			System.GC.Collect();
			foreach(MonumentInfo mi in monument_candidates.Keys){
				string name = mi.displayPhrase.english;
				int covercount = monument_candidates[mi]["cover"].Count();
				int movecount = monument_candidates[mi]["move"].Count();
				int move_radialcount = monument_candidates[mi]["move_radial"].Count();
				System.Console.ForegroundColor= System.ConsoleColor.DarkMagenta;
				System.Console.WriteLine(name+" counts!  Cover : "+covercount+
					" | Move : "+movecount+"| Radial : "+move_radialcount);//
				System.Console.ResetColor();
			}
			/*
			System.IO.File.AppendAllText(@"C:\rust\objectfile2.txt",result);
			Dictionary<string,int> objects_found = new Dictionary<string,int>();//ReadAllLines //WriteAllLines 
			string[] all_lines = System.IO.File.ReadAllLines(@"C:\rust\objectfile2.txt");
			List<string> all_lines_unique = new List<string>();
			foreach(string s in all_lines){
				if(objects_found.ContainsKey(s)){
					objects_found[s]++;
				}
				else{
					objects_found.Add(s,1);
				}
			}
			foreach(string key in objects_found.Keys){
				all_lines_unique.Add(key);
			}
			System.IO.File.WriteAllLines(@"C:\rust\objectfile2.txt",all_lines_unique.ToArray());
			
			*/
		}
		//OnHealingItemUse if connected player add point to validation queue
		//other thread then does ray checks from point to grade cover level
		//if point over threshold, create cover point
		//If begin looting add movepoint
		public class Node{
			public List<Association> links = new  List<Association>();
			public static List<Node> all_nodes = new List<Node>();
			public AIInformationZone zone;
			public string name;
			public static bool ZoneCheck(AIInformationZone n){
				return false;
			}
			
			public virtual Vector3 GetNextPoint(int idx, BasePlayer player){
				Vector3 result = new Vector3();
				return result;
			}			
			public virtual int GetNearestPointIndex(){
				return -1;//
			}
			
			public static AssociationAIZ Find_Adjacent_Simplified(AIInformationZone source){
				foreach(Node n in all_nodes){
					if(n.zone==source){
						if(n.links.Count>0){
							Association pick = n.links[UnityEngine.Random.Range(0,n.links.Count())];
							return new AssociationAIZ(pick);
						}
						else{
							System.Console.ForegroundColor = ConsoleColor.DarkRed;
							System.Console.WriteLine(n.name + " has no associations");
							System.Console.ResetColor();
							return new AssociationAIZ();
						}
					}
				}
				System.Console.WriteLine(source.transform.position.ToString()+" is not in Nodes");
				return new AssociationAIZ();
				
			}
		}
		
		private MonumentInfo Find_Associated_Monument(AIInformationZone source){
			foreach(Monument mon in Monument.monuments){
				if(mon.zone==source){
					return mon.info as MonumentInfo;
				}
			}
			return null;
		}
		private List<System.Object> Find_Adjacent_Simplified(AIInformationZone source){
			List<System.Object> result = Node.Find_Adjacent_Simplified(source).result;			
			return result;
		}
		
		[Command("roadnet")] void navigation_scan_cmd(IPlayer player, string command, string[] args){	
			BasePlayer baseplayer = (BasePlayer)player.Object;
			if(baseplayer==null){
				return;
			}			

			if(args.Length>0){
				try{
				}
				catch(Exception e){
				}
			}
			foreach(Road r in Road.roads){
				foreach(AIMovePoint point in r.zone.paths[0].Points){
					UnityEngine.Color point_color = UnityEngine.Color.white;
					foreach(Association link in r.links){
						if(link.a_idx<r.zone.paths[0].Points.Count() && r.zone.paths[0].Points[link.a_idx]==point){
							point_color = UnityEngine.Color.green;
						}
						
					}
					ConsoleNetwork.SendClientCommand(baseplayer.Connection,"ddraw.text", new object[]
					{
						60,
						point_color,
						point.transform.position,
						"∙"
					});
				}
			}
		}
		[Command("monupts")] void monument_points(IPlayer player, string command, string[] args){	
			BasePlayer baseplayer = (BasePlayer)player.Object;
			if(baseplayer==null){
				return;
			}			

		
			foreach(MonumentInfo mi in monument_candidates.Keys){
				foreach(Vector3 v3 in monument_candidates[mi]["cover"]){
					UnityEngine.Color point_color = UnityEngine.Color.cyan;
					ConsoleNetwork.SendClientCommand(baseplayer.Connection,"ddraw.text", new object[]
					{
						60,
						point_color,
						v3,
						"x"
					});					
				
				}
				foreach(Vector3 v3 in monument_candidates[mi]["move"]){
					UnityEngine.Color point_color = UnityEngine.Color.white;
					ConsoleNetwork.SendClientCommand(baseplayer.Connection,"ddraw.text", new object[]
					{
						60,
						point_color,
						v3,
						"0"
					});
				}
				foreach(Vector3 v3 in monument_candidates[mi]["move_radial"]){
					UnityEngine.Color point_color = UnityEngine.Color.white;
					ConsoleNetwork.SendClientCommand(baseplayer.Connection,"ddraw.text", new object[]
					{
						60,
						point_color,
						v3,
						"0"
					});
				}
			}
		}
		//Override navigator to better handle roam points
		
		[Command("monunet")] void monument_scan_cmd(IPlayer player, string command, string[] args){	
			BasePlayer baseplayer = (BasePlayer)player.Object;
			if(baseplayer==null){
				return;
			}			

			if(args.Length>0){
				try{
				}
				catch(Exception e){
				}
			}
			foreach(Monument r in Monument.monuments){
				Puts("Scanning "+r.name);
				List<AIMovePoint> seen_move_points = new List<AIMovePoint>();
				foreach(AIMovePointPath aimpp in r.zone.paths){
					foreach(AIMovePoint point in aimpp.Points){
						UnityEngine.Color point_color = UnityEngine.Color.green;
						ConsoleNetwork.SendClientCommand(baseplayer.Connection,"ddraw.text", new object[]
						{
							60,
							point_color,
							point.transform.position,
							"0"
						});
						seen_move_points.Add(point);
					}
				}
				foreach(AIMovePoint aimp in r.zone.movePoints){
					if(seen_move_points.Contains(aimp)) continue;
					UnityEngine.Color point_color = UnityEngine.Color.yellow;
					ConsoleNetwork.SendClientCommand(baseplayer.Connection,"ddraw.text", new object[]
					{
						60,
						point_color,
						aimp.transform.position,
						"x"
					});
					seen_move_points.Add(aimp);
					
				}
				foreach(AICoverPoint aicp in r.zone.coverPoints){
					
					UnityEngine.Color point_color = UnityEngine.Color.cyan;
					ConsoleNetwork.SendClientCommand(baseplayer.Connection,"ddraw.text", new object[]
					{
						60,
						point_color,
						aicp.transform.position,
						"-"
					});
					
				}
				
			}
		}
		public class Warp : Node{
			public static List<Warp> warps = new List<Warp>();
			
			public Warp(){
				
				warps.Add(this);
				all_nodes.Add(this);
			}
			
			public new static bool ZoneCheck(AIInformationZone n){
				bool result = false;
				foreach(Warp w in warps){
					if(w.zone==n){
						result=true;
					}
				}
				return result;
			}
			public static void find_intersections(){
				
			}
			
		}
		public class Monument : Node{
			public static List<Monument> monuments = new List<Monument>();
			
			public LandmarkInfo info = null;
			public new static bool ZoneCheck(AIInformationZone n){
				bool result = false;
				foreach(Monument w in monuments){
					if(w.zone==n){
						result=true;
					}
				}
				return result;
			}
			public static void setup_monuments(){
				
				//*
				List<LandmarkInfo> seen_zones=new List<LandmarkInfo>();
				List<LandmarkInfo> reported_zones=new List<LandmarkInfo>();
				
				foreach(AIInformationZone globalzone in AIInformationZone.zones){
					if(globalzone.transform.name.ToLower().Contains("junkpile")||globalzone.transform.name.ToLower().Contains("roadnode")){
						continue;
					}
					LandmarkInfo best_candidate = null;
					float best_candidate_score = -1;
					foreach(LandmarkInfo mi in TerrainMeta.Path.Monuments){
					
						if(seen_zones.Contains(mi)||Road.ZoneCheck(globalzone)||Warp.ZoneCheck(globalzone)){
							continue;
						}
						bool contains = false;
						if(globalzone.PointInside(mi.transform.position)){
							contains = true;
						}
						/*else if(mi.IsInBounds(globalzone.transform.position)){
							contains = true;
						}*///
						if(!reported_zones.Contains(mi)){
							System.Console.ForegroundColor= System.ConsoleColor.DarkMagenta;
							System.Console.WriteLine("MonumentInfo ["+mi.displayPhrase.english+"]"+(mi is MonumentInfo? " bounds ["+(mi as MonumentInfo).Bounds.size+"]":"")+"!");//
							System.Console.ResetColor();
							reported_zones.Add(mi);
						}
						if(!contains){
							continue;
						}
						float distance = Vector3.Distance(mi.transform.position,globalzone.transform.position);
						if(best_candidate_score==-1||distance<best_candidate_score){							
							best_candidate=mi;
							best_candidate_score=distance;
							seen_zones.Add(mi);
						}
						
						
					}
					string candidate = globalzone.transform.name;
					string prefab_name = candidate;
					Transform current = globalzone.transform;
					string info_name = "";
					while(current!=null&&!candidate.Contains("prefab")&&!candidate.Contains("moon")){//LandmarkInfo
						candidate=current.transform.name;
						LandmarkInfo info2 = current.gameObject.GetComponent<LandmarkInfo>();
						if(info2!=null && info2.displayPhrase!=null && info2.displayPhrase.english!=""){
							info_name=info2.displayPhrase.english;
							candidate=info2.displayPhrase.english;
							break;
						}
						current=current.transform.parent;
					}
					string[] borken_name = candidate.Split("/");
					candidate=borken_name[borken_name.Length-1].Replace(".prefab","").Replace("_"," ");
					if(best_candidate!=null){
						 candidate = best_candidate.transform.name;
						if(best_candidate.displayPhrase!=null && best_candidate.displayPhrase.english!=""){
							candidate=best_candidate.displayPhrase.english.Replace("\n","");
						}
						new Monument(globalzone,best_candidate);
					}else{
						new Monument(globalzone,candidate);
					}
					System.Console.ForegroundColor= System.ConsoleColor.DarkCyan;
					System.Console.WriteLine("Created Monument ["+candidate+"] with potential name ["+prefab_name+"] or ["+info_name+"]!");//
					System.Console.ResetColor();
					
				}
				/*/
				foreach(AIInformationZone globalzone in AIInformationZone.zones){

					if(Road.ZoneCheck(globalzone)||Warp.ZoneCheck(globalzone)){
						continue;
					}
					MonumentInfo mi = globalzone.gameObject.GetComponentInChildren<MonumentInfo>();
					try{
						if(mi==null){
							mi =globalzone.transform.parent.gameObject.GetComponentInChildren<MonumentInfo>();//
						}
						if(mi==null){
							mi =globalzone.transform.parent.parent.gameObject.GetComponentInChildren<MonumentInfo>();//
						}
						if(mi==null){
							mi =globalzone.transform.parent.parent.parent.gameObject.GetComponentInChildren<MonumentInfo>();//
						}
					}catch(Exception e){}
					string candidate = globalzone.transform.name;
					if(mi!=null){
						if(mi.displayPhrase!=null){
							candidate=mi.transform.name;
						}
						else{
							candidate=mi.transform.name;
						}
					}
					System.Console.ForegroundColor= System.ConsoleColor.DarkCyan;
					System.Console.WriteLine("Created Monument "+candidate+"!");
					System.Console.ResetColor();
				}
				//*/
				
				foreach(MonumentInfo mi in TerrainMeta.Path.Monuments){
				
					if(!seen_zones.Contains(mi) && monument_candidates.ContainsKey(mi)){
						foreach(Vector3 v3 in monument_candidates[mi]["cover"]){
							GameObject new_cover = new GameObject();
							new_cover.transform.position=v3;
							new_cover.AddComponent<AICoverPoint>();
							new_cover.transform.SetParent(mi.transform);
						}
						foreach(Vector3 v3 in monument_candidates[mi]["move"]){
							GameObject new_cover = new GameObject();
							new_cover.transform.position=v3;
							new_cover.AddComponent<AIMovePoint>().WaitTime = (60);
							new_cover.transform.SetParent(mi.transform);
							
						}
						foreach(Vector3 v3 in monument_candidates[mi]["move_radial"]){
							GameObject new_cover = new GameObject();
							new_cover.transform.position=v3;
							new_cover.AddComponent<AIMovePoint>().WaitTime = (60);
							new_cover.transform.SetParent(mi.transform);
							
						}
					}
					AIInformationZone markerzone = mi.gameObject.AddComponent<AIInformationZone>();
					//Make sure it has a grid
					AIInformationGrid newGrid = mi.gameObject.GetComponent<AIInformationGrid>();
					if(newGrid==null){
						newGrid = mi.gameObject.AddComponent<AIInformationGrid>();
					}
					markerzone.bounds=mi.Bounds;
					markerzone.ShouldSleepAI=false;
					markerzone.Virtual=false;
					markerzone.Start();	
					new Monument(markerzone,mi);
				}
				connect_monuments();
			}
			static string[] subway_match = {"Link ","Straight ","Station "};
			static string[] lab_match = {"Moonpool "};
			static string[] oil_match = {"Oil Rig"};
			public static void connect_monuments(){
				List<Monument> subways = new List<Monument>();
				List<Monument> labs = new List<Monument>();
				List<Monument> oils = new List<Monument>();
				foreach(Monument m in monuments){
					bool is_found=false;
					foreach(string s in subway_match){
						if(m.name.StartsWith(s)){
							subways.Add(m);
							break;
						}
					}
					if(is_found){
						break;
					}
					foreach(string s in lab_match){
						if(m.name.StartsWith(s)){
							labs.Add(m);
							break;
						}
					}
					if(is_found){
						break;
					}
					foreach(string s in oil_match){
						if(m.name.StartsWith(s)){
							oils.Add(m);
							break;
						}
					}
					if(is_found){
						break;
					}
					Dictionary<Road,int> possible_candidates=new Dictionary<Road,int>();
					Road best_road = null;
					int best_point=-1;
					float best_distance=-1;

					foreach(Road r in Road.roads){
						List<AIMovePoint> points = r.zone.paths[0].Points;
						int road_best_point=-1;
						float road_best_distance=-1;
						for(int i = 0; i < points.Count();i++){
							float this_distance = Vector3.Distance(points[i].transform.position,m.zone.transform.position);
							if(road_best_distance==-1||this_distance<road_best_distance){
								road_best_point=i;
								road_best_distance=this_distance;
							}
						}
						if(m.zone.PointInside(points[road_best_point].transform.position)){
							possible_candidates.Add(r,road_best_point);							
						}
						if(best_distance==-1||road_best_distance<best_distance){
							best_road = r;
							best_point=road_best_point;
							best_distance=road_best_distance;							
						}
					}
					
					if(possible_candidates.Count()>0){
						foreach(Road winning_road in possible_candidates.Keys){
							bool can_be_reached = false;
							foreach(AIMovePoint aimp in m.zone.movePoints){
								UnityEngine.AI.NavMeshPath test_path= new UnityEngine.AI.NavMeshPath();
								UnityEngine.AI.NavMesh.CalculatePath(aimp.transform.position, 
								winning_road.zone.paths[0].Points[possible_candidates[winning_road]].transform.position, 
								25, 
								test_path);
								if(test_path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete){
									can_be_reached=true;
									break;
								}							
							}
							if(!can_be_reached){
								foreach(AIMovePointPath aimpp in m.zone.paths){
									foreach(AIMovePoint aimp2 in aimpp.Points){
										UnityEngine.AI.NavMeshPath test_path= new UnityEngine.AI.NavMeshPath();
										UnityEngine.AI.NavMesh.CalculatePath(aimp2.transform.position, 
										winning_road.zone.paths[0].Points[possible_candidates[winning_road]].transform.position, 
										25, 
										test_path);
										if(test_path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete){
											can_be_reached=true;
											break;
										}							
									}
								}
							}
							if(can_be_reached){
								Association assoc = new Association();
								assoc.a=m;
								assoc.b=winning_road;
								assoc.b_idx=possible_candidates[winning_road];
								m.links.Add(assoc);
								
								assoc = new Association();
								assoc.b=m;
								assoc.a=winning_road;
								assoc.a_idx=possible_candidates[winning_road];
								winning_road.links.Add(assoc);
								System.Console.ForegroundColor= System.ConsoleColor.Magenta;
								System.Console.WriteLine("Connected Monument ["+m.name+"] to road ["+winning_road.name+"] !");//
								System.Console.ResetColor();
							}
							
						}
					}else{
						bool can_be_reached = false;
						foreach(AIMovePoint aimp in m.zone.movePoints){
							UnityEngine.AI.NavMeshPath test_path= new UnityEngine.AI.NavMeshPath();
							UnityEngine.AI.NavMesh.CalculatePath(aimp.transform.position, 
							best_road.zone.paths[0].Points[best_point].transform.position, 
							25, 
							test_path);
							if(test_path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete){
								can_be_reached=true;
								break;
							}							
						}
						if(can_be_reached){
							Association assoc = new Association();
							assoc.a=m;
							assoc.b=best_road;
							assoc.b_idx=best_point;
							m.links.Add(assoc);
							
							assoc = new Association();
							assoc.b=m;
							assoc.a=best_road;
							assoc.a_idx=best_point;
							best_road.links.Add(assoc);
							System.Console.ForegroundColor= System.ConsoleColor.Magenta;
							System.Console.WriteLine("Connected Monument ["+m.name+"] to road ["+best_road.name+"] !");//
							System.Console.ResetColor();
						}
						
					}
				}
			
				foreach(Monument m in subways){
					foreach(Monument m2 in subways){
						if(m==m2) continue;
						
						Association assoc = new Association();
						assoc.a=m;
						assoc.b=m2;
						assoc.a_idx=assoc.b_idx=assoc.a_path=assoc.b_path=-1;
						m.links.Add(assoc);
					}
					if(m.links.Count==0) continue;
					m.links.Sort(delegate(Association x, Association y){
						return (int)Mathf.Sign(
							Vector3.Distance(m.zone.transform.position,y.b.zone.transform.position)-
							Vector3.Distance(m.zone.transform.position,x.b.zone.transform.position)
							);
					});
					System.Console.ForegroundColor= System.ConsoleColor.Cyan;
					System.Console.WriteLine("Distances! ["+Vector3.Distance(m.zone.transform.position,m.links[0].b.zone.transform.position)+
					"] vs ["+Vector3.Distance(m.zone.transform.position,m.links[m.links.Count-1].b.zone.transform.position)+"]");//
					System.Console.ResetColor();
					for(int i = m.links.Count-1;i>-1;i--){
						if(Vector3.Distance(m.zone.transform.position,m.links[i].b.zone.transform.position)==0){
							m.links.RemoveAt(i);
						}
					}
					while(m.links.Count>3){
						m.links.RemoveAt(0);
					}
					System.Console.ForegroundColor= System.ConsoleColor.Cyan;
					System.Console.WriteLine("Distances! ["+Vector3.Distance(m.zone.transform.position,m.links[0].b.zone.transform.position)+
					"] vs ["+Vector3.Distance(m.zone.transform.position,m.links[m.links.Count-1].b.zone.transform.position)+"]");//
					System.Console.ResetColor();
					
					
				}
				/*
				parts.Sort(delegate(Part x, Part y)
				{
					if (x.PartName == null && y.PartName == null) return 0;
					else if (x.PartName == null) return -1;
					else if (y.PartName == null) return 1;
					else return x.PartName.CompareTo(y.PartName);
				});
				*/
				
			}
			
			public static TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
			
			public Monument(AIInformationZone tgtZone, LandmarkInfo tgtInfo){
				zone=tgtZone;
				if(tgtInfo==null){					
					tgtInfo = zone.gameObject.AddComponent<MonumentInfo>();
					(tgtInfo as MonumentInfo).Bounds=zone.bounds;	
				}
				if(tgtInfo.displayPhrase==null){
					string[] split = zone.transform.name.Split("/");
					name=split[split.Length-1].Replace(".prefab","").Replace("."," ").Replace("_"," ").Replace("-"," ");
					tgtInfo.displayPhrase = new Translate.Phrase("","");
				}else{
					tgtInfo.displayPhrase.english = tgtInfo.displayPhrase.english.Replace("\n","");
					name=tgtInfo.displayPhrase.english;
				}
				name = textInfo.ToTitleCase(name.ToLower()); 
				name = System.Text.RegularExpressions.Regex.Replace(name, @"\s\w\w?\s", m => m.ToString().ToUpper()).Replace(" OF "," of ").Replace(" IN "," in ").Replace(" AS ", " as ");
				tgtInfo.displayPhrase.english = name;
				info=tgtInfo;
				monuments.Add(this);
				all_nodes.Add(this);
			}
			public Monument(AIInformationZone tgtZone, string tgtInfo){
				zone=tgtZone;
				name=tgtInfo;
				monuments.Add(this);
				all_nodes.Add(this);
				
				MonumentInfo customInfo = zone.gameObject.GetComponent<MonumentInfo>();
				if(customInfo==null){			
					customInfo = zone.gameObject.AddComponent<MonumentInfo>();		
					customInfo.Bounds=zone.bounds;	
				}
				if(customInfo.displayPhrase==null){					
					string[] split = zone.transform.name.Split("/");
					if(tgtInfo!=""){
						split=tgtInfo.Split("/");
					}
					name=split[split.Length-1].Replace(".prefab","").Replace("."," ").Replace("_"," ").Replace("-"," ");
					name = textInfo.ToTitleCase(name.ToLower()); 
					name = System.Text.RegularExpressions.Regex.Replace(name, @"\s\w\w?\s", m => m.ToString().ToUpper()).Replace(" OF "," of ").Replace(" IN "," in ").Replace(" AS ", " as ");
					customInfo.displayPhrase = new Translate.Phrase("",name);
				}else{
					customInfo.displayPhrase.english = customInfo.displayPhrase.english.Replace("\n","");
					name=customInfo.displayPhrase.english;
				}
				
				info=customInfo;
			}
			
		}
		public class Road : Node{
			PathList path;
			
			public static string[] road_name_patterns = {"Hgwy. $","$ St.","$ Blvd.","$ Rd."};
			public static List<Road> roads = new List<Road>();
			public static List<string> seen_names = new List<string>();
			
			public new static bool ZoneCheck(AIInformationZone n){
				bool result = false;
				foreach(Road w in roads){
					if(w.zone==n){
						result=true;
					}
				}
				return result;
			}
			
			
			public Road(PathList tgtPath){
				path=tgtPath;
				List<AIMovePoint> path_points = new List<AIMovePoint>();
				GameObject info_root = null;
				AIInformationZone markerzone =null;
				foreach(Vector3 point in tgtPath.Path.Points){
					GameObject pointGO = new GameObject();
					if(info_root==null){
						info_root=pointGO;
						markerzone = info_root.AddComponent<AIInformationZone>();
						//Make sure it has a grid
						AIInformationGrid newGrid =info_root.GetComponent<AIInformationGrid>();
						if(newGrid==null){
							newGrid = info_root.AddComponent<AIInformationGrid>();
						}		
					}else{
						pointGO.transform.SetParent(info_root.transform);				
					}

					pointGO.transform.position=point;
					pointGO.transform.name="RoadNode";
					AIMovePoint pointAIMP=pointGO.AddComponent<AIMovePoint>();
					path_points.Add(pointAIMP);		
				}
				markerzone.bounds=new Bounds(info_root.transform.position,new Vector3(1,1,1));
				AIMovePointPath aimpp = new AIMovePointPath();
				aimpp.Points=path_points;
				aimpp.LoopMode=(tgtPath.Path.Circular?AIMovePointPath.Mode.Loop:AIMovePointPath.Mode.Reverse);
				markerzone.paths.Add(aimpp);
				//Init zone
				markerzone.ShouldSleepAI=false;
				markerzone.Virtual=false;
				markerzone.Start();		
				this.zone=markerzone;
				roads.Add(this);
				all_nodes.Add(this);
				this.name = this.zone.paths[0].Points.Count().ToString();
				int index = UnityEngine.Random.Range(1, road_name_patterns.Length);
				string candidate = "";
				int collision_fix_count = 0;
				do{
					candidate = road_name_patterns[index].Replace("$",this.name+"A");	
					index = (index+1)%road_name_patterns.Length;
					collision_fix_count++;					
				}while(seen_names.Contains(candidate) && collision_fix_count<=road_name_patterns.Length);
				if(seen_names.Contains(candidate)){
					collision_fix_count = 0;
					index = UnityEngine.Random.Range(1, road_name_patterns.Length);
					do{
						candidate = road_name_patterns[index].Replace("$",this.name+"B");	
						index = (index+1)%road_name_patterns.Length;
						collision_fix_count++;					
					}while(seen_names.Contains(candidate) && collision_fix_count<=road_name_patterns.Length);
				}
				this.name=candidate;
				seen_names.Add(candidate);
				System.Console.ForegroundColor= System.ConsoleColor.Cyan;
				System.Console.WriteLine("Created Road "+this.name+"!");
				System.Console.ResetColor();
			}
			public static void setup_road_system(){				
				foreach(PathList path in TerrainMeta.Path.Roads){		
					Road r = new Road(path);
				}
				
				foreach(PathList path in TerrainMeta.Path.Powerlines){
					new Road(path);
				}
				foreach(PathList path in TerrainMeta.Path.Rails){
					new Road(path);
				}
				
				Road.find_intersections();
			}
			
			public static void find_intersections(){
				Dictionary<Road,List<Road>> seen = new Dictionary<Road,List<Road>>();
				foreach(Road r in roads){
					seen.Add(r,new List<Road>());
					seen[r].Add(r);
					foreach(Road r2 in roads){
						if(seen.ContainsKey(r2)&&seen[r2].Contains(r)){			
						}
						else{
							seen[r].Add(r2);
							AIMovePoint closest_own_point = null;
							float closest_own_point_distance = -1f;
							AIMovePoint closest_own_point_partner=null;
							foreach(AIMovePoint point1 in r.zone.paths[0].Points){
								AIMovePoint closest_other_point = null;
								float closest_other_point_distance = -1f;
								foreach(AIMovePoint point2 in r2.zone.paths[0].Points){
									float this_distance = Vector3.Distance(point1.transform.position,point2.transform.position);
									if(this_distance<closest_other_point_distance||closest_other_point_distance==-1){
										closest_other_point_distance=this_distance;
										closest_other_point=point2;
									}
								}
								if(closest_own_point_distance == -1f||closest_other_point_distance<closest_own_point_distance){
									closest_own_point=point1;
									closest_own_point_partner=closest_other_point;
									closest_own_point_distance=closest_other_point_distance;
								}
							}
							if(closest_own_point_distance<INTERSECTION_DISTANCE){
								
								Association assoc= new Association();
								assoc.a=r;
								assoc.b=r2;
								assoc.a_idx=r.zone.paths[0].Points.IndexOf(closest_own_point);
								assoc.b_idx=r2.zone.paths[0].Points.IndexOf(closest_own_point_partner);
								System.Console.ForegroundColor= System.ConsoleColor.Green;
								System.Console.WriteLine("Created Association between "+r.name+" and "+r2.name+"!");
								System.Console.ResetColor();
								r.links.Add(assoc);
								
								assoc= new Association();
								assoc.b=r;
								assoc.a=r2;
								assoc.b_idx=r.zone.paths[0].Points.IndexOf(closest_own_point);
								assoc.a_idx=r2.zone.paths[0].Points.IndexOf(closest_own_point_partner);
								System.Console.ForegroundColor= System.ConsoleColor.Green;
								System.Console.WriteLine("Created Association between "+r2.zone.transform.name+" and "+r.zone.transform.name+"!");
								System.Console.ResetColor();
								r2.links.Add(assoc);
							}
						}						
					}
				}
			}
		}

		void OnServerInitialized(){
			
			Road.setup_road_system();
			if(!hasRun){
				ScanGameObjects();
			}
			Monument.setup_monuments();
		}
		void Unload(){
			if (AIInformationZone.zones != null)
			{
				foreach(Road r in Road.roads.ToArray()){
					AIInformationZone.zones.Remove(r.zone);
					AIMovePoint[] points_of_path = r.zone.paths[0].Points.ToArray();
					foreach(AIMovePoint aimp in points_of_path){
						GameObject.Destroy(aimp.gameObject);
					}
					Puts("Destroyed Road");//
				}
				foreach(Monument m in Monument.monuments.ToArray()){
					
				}
			}
		}

		public struct Association{
			public int a_path=-1;
			public int b_path=-1;
			public int a_idx=-1;
			public int b_idx=-1;
			public Node a=null;
			public Node b=null;	
			public Association(){
				
			}
		}
		public struct AssociationAIZ{
			public int a_path=-1;
			public int b_path=-1;
			public int a_idx=-1;
			public int b_idx=-1;
			public AIInformationZone a=null;
			public AIInformationZone b=null;	
			public AssociationAIZ(){
				
			}
			public AssociationAIZ(Association ass){
				a_path=ass.a_path;
				b_path=ass.b_path;
				a_idx=ass.a_idx;
				b_idx=ass.b_idx;
				a=ass.a.zone;
				b=ass.b.zone;				
				result.Add(a);
				result.Add(b);
				List<int> numbers = new List<int>();
				numbers.Add(a_path);
				numbers.Add(a_idx);
				numbers.Add(b_path);
				numbers.Add(b_idx);
				result.Add(numbers);
			}
			public List<System.Object> result = new List<System.Object>();//
		}
		public struct Transit{
			Association a;
			Association b;
			int current_idx=-1;
			public Transit(){
				
			}
		}
	}
}