
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
using  UnityEditor; 
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
	using Oxide.Core.Plugins;
using System.Threading;
	using Oxide.Core;
	using Newtonsoft.Json;

namespace Oxide.Plugins
{
	[Info("CustomAIZones", "obsol", "0.0.1")]
	[Description("For breaking monuments")]
	public class CustomAIZones : CovalencePlugin
	{
		
		#region config
		public ConfigData config;
		public class ConfigData
		{
			[JsonProperty("version", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Oxide.Core.VersionNumber Version = default(VersionNumber);
		}
		protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<ConfigData>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
                else
                {
                    UpdateConfigValues();
                }
            }
            catch (Exception ex)
            {
                PrintError($"The configuration file is corrupted or missing. \n{ex}");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
			Puts("Version mismatch for config");
            config = new ConfigData();
            config.Version = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private void UpdateConfigValues()
        {
            if (config.Version < Version)
            {
                if (config.Version <= default(VersionNumber))
                {
					Puts("Version mismatch for config");
                }
                config.Version = Version;
            }
        }

        private bool GetConfigValue<T>(out T value, params string[] path)
        {
            var configValue = Config.Get(path);
            if (configValue == null)
            {
                value = default(T);
                return false;
            }
            value = Config.ConvertValue<T>(configValue);
            return true;
        }

		#endregion
		
		List<AIPoint> customPoints = new List<AIPoint>();
		List<AIMovePoint> customMovePoints = new List<AIMovePoint>();
		List<AICoverPoint> customCoverPoints = new List<AICoverPoint>();
		List<MonumentInfo> customMonuments = new List<MonumentInfo>();
		List<AIMovePointPath> allPaths = new List<AIMovePointPath>();
		
		private void OnServerInitialized()
        {
			GetCustomAIPoints();
			GetMonumentMarkers();//assets/bundled/prefabs/modding/volumes_and_triggers/monument_marker.prefab
		}
		public void GetCustomAIPoints(){//
			GameObject[] all = UnityEngine.Object.FindObjectsOfType(typeof(GameObject)) as GameObject[];
			foreach(GameObject go in all){
				if(go.transform.name.Contains("assets/rust.ai/cover/")){
					//Puts("Found point! " + go.transform.name);
					if(go.transform.name.Contains("oilrig_patrolpoints")){

						AIMovePoint movepoint = go.GetComponent<AIMovePoint>();
						if(movepoint ==null){
							movepoint=go.AddComponent<AIMovePoint>();
							
							//Puts("Movepoint added!");
						}
						movepoint.WaitTime = (go.transform.localScale.y*60);
						Puts("Found point! " + movepoint.WaitTime);
						if(!customMovePoints.Contains(movepoint)){
							customMovePoints.Add(movepoint);
							//Puts("Added Movepoint to processing :");
						}
						customPoints.Add(movepoint);
					}
					if(go.transform.name.Contains("oilrig_coverpoints")){
						
						AICoverPoint coverpoint = go.GetComponent<AICoverPoint>();
						if(coverpoint ==null){
							coverpoint=go.AddComponent<AICoverPoint>();
							//Puts("coverpoint added!");
						}
						if(!customCoverPoints.Contains(coverpoint)){
							customCoverPoints.Add(coverpoint);
							//Puts("Added coverpoint to processing");
						}
						customPoints.Add(coverpoint);
					}

				}
			}
			
		}
		
		class CustomAIZ : AIInformationZone{
			
		}
		
		//
		public void GetMonumentMarkers(){
			
			MonumentInfo[] Markers =  UnityEngine.Object.FindObjectsOfType(typeof(MonumentInfo)) as MonumentInfo[];
			AIInformationZone[] Zones = UnityEngine.Object.FindObjectsOfType(typeof(AIInformationZone)) as AIInformationZone[];
			Puts("Found "+Markers.Count()+" markers and "+Zones.Count()+" zones");
			foreach(MonumentInfo marker in Markers){	
			
				//Check if marker is already in a zone
				bool isInZone = false;
				CustomAIZ markerzone = marker.GetComponent<CustomAIZ>();
				foreach(AIInformationZone zone in Zones){
					if(markerzone!=null || (zone != markerzone && zone.bounds.Contains(marker.transform.position))){
						isInZone=true;
						Puts("Marker in zone");
						break;
					}
				}
				if(isInZone){continue;}
				Puts("Marker not in zone or self");
				
				//Ignore markers that aren't for AI otherwise add to custom monuments
				if(marker.transform.localScale == new Vector3(1,1,1)){continue;}
				if(!customMonuments.Contains(marker)){
					customMonuments.Add(marker);
					Puts("Marker added to custom monuments");//
				}
				
				//Vars we'll need
				Bounds monumentBounds = new Bounds(marker.transform.position,marker.transform.localScale);
				List<AIMovePoint> myMovePoints = new List<AIMovePoint>();
				
				//Parent points in bounds
				foreach(AIPoint point in customPoints){
					if(monumentBounds.Contains(point.transform.position)){
						Vector3 oldScale = point.transform.localScale;
						point.transform.SetParent(marker.transform);
						point.transform.localScale=oldScale;
						Puts("Point in bounds, parenting!:" + point.transform.localScale);
					}
					if(point is AIMovePoint){
						myMovePoints.Add(point as AIMovePoint);
					}
				}
				
				//Get or create AI Info zone, remove to readd on new start if existing 
				if(markerzone==null){	
					Puts("Adding new zone");
					markerzone = marker.gameObject.AddComponent<CustomAIZ>();
				}
				else{
					if(AIInformationZone.zones.Contains(markerzone)){
						AIInformationZone.zones.Remove(markerzone);
					}
				}
				//Make sure it has a grid
				AIInformationGrid newGrid = marker.gameObject.GetComponent<AIInformationGrid>();
				if(newGrid==null){
					newGrid = marker.gameObject.AddComponent<AIInformationGrid>();
				}
				markerzone.paths=getPaths(myMovePoints,marker);
				//Init zone
				markerzone.bounds=monumentBounds;
				markerzone.ShouldSleepAI=false;
				markerzone.Virtual=false;
				markerzone.Start();				
				
			}
		}
        [Command("ShowPoints")]
        void DemoMonumentSwap_cmd(IPlayer player, string command, string[] args)
        {
			foreach(AIPoint p in customPoints){
				global::ConsoleNetwork.BroadcastToAllClients("ddraw.sphere", new object[]
				{
					60,
					global::UnityEngine.Color.cyan,
					p.transform.position,
					1f
				});
			}
			foreach(AIMovePointPath path in allPaths){
				int i = 0;
				while(i<path.Points.Count()){
					int i2 = (i<path.Points.Count()-1?i+1:0);
					global::ConsoleNetwork.BroadcastToAllClients("ddraw.line", new object[]
					{
						60,
						global::UnityEngine.Color.green,
						path.Points[i].transform.position,
						path.Points[i2].transform.position
					});
					i++;
				}
			}
			
			
		}
		
		public List<AIMovePointPath> getPaths (List<AIMovePoint> allPoints, MonumentInfo marker){
			List<AIMovePoint> pointsToProcess = new List<AIMovePoint>(allPoints.ToArray());
			List<AIMovePoint> processedPoints = new List<AIMovePoint>();
			List<AIMovePointPath> result = new List<AIMovePointPath>();
			Dictionary<int,List<AIMovePoint>> Paths= new Dictionary<int,List<AIMovePoint>>();
			
			int maxSeek=allPoints.Count();
			
			foreach(AIMovePoint p in allPoints.ToArray()){
				int pPath = (int)Mathf.Round(p.transform.localScale.x*1000);
				if(!Paths.ContainsKey(pPath)){
					Paths.Add(pPath,new List<AIMovePoint>());
				}
				Paths[pPath].Add(p);
			}
			foreach(List<AIMovePoint> l in Paths.Values){
				if(l.Count()>0){
					Puts("Path"+l[0].transform.localScale.x+" has "+l.Count()+" points!");
					l.Sort(delegate(AIMovePoint x, AIMovePoint y)
						{
							return (x.transform.localScale.z>y.transform.localScale.z?
								1:-1);
						});
					
					AIMovePointPath path = l[0].gameObject.GetComponent<AIMovePointPath>();
					if(path==null){
						path = l[0].gameObject.AddComponent<AIMovePointPath>();
					}
					path.Points.Clear();
					path.Points.AddRange(l);
					result.Add(path);
				}
				
			}
			/*
			while(pointsToProcess.Count()>0){
				Puts("List has "+pointsToProcess.Count());
				AIMovePoint point = null;
				foreach(AIMovePoint p1 in pointsToProcess){
					if(p1.transform.localScale.y>1){
						Puts(p1.transform.localScale.y.ToString());
						point = p1;
					}
				}
				if(point==null){
					break;
				}
				AIMovePointPath path = point.gameObject.GetComponent<AIMovePointPath>();
				
				path.Points.Clear();
				
				path.AddPoint(point);
				pointsToProcess.Remove(point);		
				AIMovePoint fromPoint = point;
				int i = 0;
				while(i<maxSeek){
					/*
					List<AIMovePoint> pointsToProcessForPoint = new List<AIMovePoint>(pointsToProcess.ToArray());
					pointsToProcessForPoint.Sort(delegate(AIMovePoint x, AIMovePoint y)
					{
						return (Vector3.Distance(fromPoint.transform.position,x.transform.position)>Vector3.Distance(fromPoint.transform.position,y.transform.position)?
							1:-1);
					});
					AIMovePoint target = null;
					float targetScore = 0.9f;
					foreach(AIMovePoint p in pointsToProcessForPoint){
						if(p != fromPoint && (int)(p.localScale.x*100)==){
							Vector3 forward = fromPoint.transform.TransformDirection(Vector3.forward);
							Vector3 toOther = Vector3.Normalize(p.transform.position - fromPoint.transform.position);

							float thisScore = Vector3.Dot(forward, toOther);
							if(thisScore >= targetScore){
								target=p;
								targetScore = thisScore;//
								break;
							}
						}
					}
					if(target==null || target==fromPoint){
						break;
					}				
					if(pointsToProcessForPoint.Contains(target)){
						pointsToProcessForPoint.Remove(target);		
						pointsToProcess.Remove(target);	
					}		
					path.AddPoint(target);
					fromPoint=target;
					i++;
					*/
					/*
				}
					Puts("Path has "+path.Points.Count()+" points");
				if(path.Points.Count()<maxSeek && path.Points.Count()>1){
					result.Add(path);
					allPaths.Add(path);
				}
				
			}
			*/
			//shut up I`m still working on it//
			return result;
		}
		
	}
}