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
using UnityEngine; 
using UnityEngine.AI; 
using  UnityEditor; 
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
	using Oxide.Core.Plugins;
using System.Threading;
	using Oxide.Core;
	using Newtonsoft.Json;//

namespace Oxide.Plugins
{
	[Info("StateImprovement", "obsol", "0.0.1")]
	[Description("For breaking monuments")]
	public class StateImprovement : CovalencePlugin
	{
		
		#region config
		public ConfigData config;
		public static float findNextZone = 300;
		public class ConfigData
		{
			
			[JsonProperty("findNextZone", ObjectCreationHandling = ObjectCreationHandling.Replace)]			
			public float findNextZone = 300;
			
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
			StateImprovement.findNextZone=config.findNextZone;
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
		
		[PluginReference]
		private Plugin MonuNet;
		public static StateImprovement _instance= null;
		public static AssociationAIZ getNextZone(AIInformationZone aiz){
			return StateImprovement._instance.getNextZone_local(aiz);
		}
			
		public AssociationAIZ getNextZone_local(AIInformationZone aiz){
			if (MonuNet != null) // check if plugin is loaded
			{
				return new AssociationAIZ(MonuNet.Call<List<System.Object>>("Find_Adjacent_Simplified", aiz));
			}
			return new AssociationAIZ();
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
			public AssociationAIZ(List<System.Object> packed){
				if(packed.Count()==3&&((List<int>)packed[2]).Count()==4&&packed[0] is AIInformationZone){
					a=(AIInformationZone)packed[0];
					b=(AIInformationZone)packed[1];
					a_path=(int)((List<int>)packed[2])[0];
					a_idx=(int)((List<int>)packed[2])[1];
					b_path=(int)((List<int>)packed[2])[2];
					b_idx=(int)((List<int>)packed[2])[3];
				}
			}
		}
		
		#region States
		public class CustomFollowPathState : BaseAIBrain.BaseFollowPathState
		{
			float TimeOnPath = 0f;
			public static float TimeLimit = 300;
			public static float TimeLimitMax = 600;
			HumanNPC self = null;
			public Vector3 lastPosition= new Vector3(0,0,0);
			public float timeAtPoint = 0;
			public static float stuckLimit = 5;
			public AIMovePointPath path;
			public StateStatus status;
			public AIMovePoint currentTargetPoint;
			public float currentWaitTime;
			private float currentLookTime = 0;
			private float next_zone = 0;
			private AIMovePointPath.PathDirection pathDirection;
			public int currentNodeIndex;
			public AIInformationZone current_zone;
			
			public static bool debugMode = false;
			public static BaseAIBrain debugEntity = null;
			
			public void debugState(string text, BaseAIBrain entity){
				if(debugMode){
					if(debugEntity==null){
						debugEntity=entity;
					}
					if(debugEntity!=entity) return;
					System.Console.ForegroundColor = ConsoleColor.Red;
					System.Console.WriteLine(text);
					System.Console.ResetColor();
					
				}
			}
		
			public CustomFollowPathState(){
			}
			
			public void setNextPath(BaseAIBrain brain, BaseEntity entity){
				
				// Get zone
				AIInformationZone forPoint = self.GetInformationZone(entity.transform.position);
				AIMovePointPath best = brain.Navigator.Path;
				
				// If there's a zone and paths, check for best path
				if(forPoint!=null||forPoint.paths.Count()>=0){					
					
					
					// Find best path you can reach
					foreach (AIMovePointPath aizPath in forPoint.paths)
					{
						if(best== brain.Navigator.Path || Vector3.Distance(aizPath.transform.position,entity.transform.position) < Vector3.Distance(best.transform.position,entity.transform.position))
						{
							NavMeshPath navMeshPath = new NavMeshPath();
							if(NavMesh.CalculatePath(entity.transform.position, aizPath.transform.position, 25, navMeshPath)){
								if(navMeshPath.status == NavMeshPathStatus.PathComplete)
								{
									best=aizPath;
								}
							}
							else{
								brain.Navigator.Agent.Warp(entity.transform.position);
							}
						}
					}
				}
				// If same path just flip
				if(best==brain.Navigator.Path){
					this.pathDirection = (this.pathDirection==AIMovePointPath.PathDirection.Forwards?AIMovePointPath.PathDirection.Backwards:AIMovePointPath.PathDirection.Forwards);
					//System.Console.ForegroundColor = ConsoleColor.Cyan;
					//System.Console.WriteLine("Flipping path");
					//System.Console.ResetColor();
				}else{
					//System.Console.ForegroundColor = ConsoleColor.Cyan;
					//System.Console.WriteLine("Changing path");
					//System.Console.ResetColor();
				}
				
				brain.Navigator.Path = best;
				
				
			
			}
			
			public override void StateLeave(BaseAIBrain brain, BaseEntity entity)
			{
				base.StateLeave(brain, entity);
				//If we've been on path too long, find best path
				if(TimeOnPath >  TimeLimit){
					TimeOnPath=0f;
					setNextPath(brain,entity);
				}
				if(currentTargetPoint!=null){
					currentTargetPoint.ClearIfUsedBy(entity);
				}
				pathDirection = UnityEngine.Random.Range(0f,1f)>0.5f?AIMovePointPath.PathDirection.Forwards:AIMovePointPath.PathDirection.Backwards;
			}

			public void NextZoneProcessing(BaseAIBrain brain, BaseEntity entity){
				if(next_zone>StateImprovement.findNextZone){
					next_zone=0;
					//AIInformationZone source, out AIInformationZone target, out int path, out int idx
					AIInformationZone target = null;
					int path = -1;
					int idx = -1;
					StateImprovement.AssociationAIZ result = new AssociationAIZ();
					MonumentInfo mi = null;
					try{
						result = StateImprovement.getNextZone(current_zone);
						if(result.a==null&&result.b==null){
							if(StateImprovement._instance.MonuNet!=null){
								mi = StateImprovement._instance.MonuNet.Call<MonumentInfo>("Find_Associated_Monument", current_zone);
								if(mi==null){
									System.Console.ForegroundColor= System.ConsoleColor.Red;
									System.Console.WriteLine(" No monument at "+entity.transform.position+" | "+entity.transform.name);
									System.Console.ResetColor();								
								}
								else{
									System.Console.ForegroundColor= System.ConsoleColor.Red;
									System.Console.WriteLine(" Failing monument: "+mi.displayPhrase.english+" | "+entity.transform.name);
									System.Console.ResetColor();
								}
							}
							return ;
						}else{		
							if(StateImprovement._instance.MonuNet!=null){
								mi = StateImprovement._instance.MonuNet.Call<MonumentInfo>("Find_Associated_Monument", current_zone);	
								if(mi==null){			
									System.Console.ForegroundColor= System.ConsoleColor.Green;
									System.Console.WriteLine(" No monument at "+entity.transform.position+" | "+entity.transform.name);									
									System.Console.ResetColor();								
								}
								else{
									System.Console.ForegroundColor= System.ConsoleColor.Green;
									System.Console.WriteLine(" Found monument:"+mi.displayPhrase.english+" | "+entity.transform.name);
								}
									if(result.b!=null){
										current_zone=result.b;
										brain.Navigator.Path=null;
										this.path = null;
									}
									System.Console.ResetColor();	
							}
						
						}
					}
					catch(Exception e){
						System.Console.ForegroundColor= System.ConsoleColor.DarkRed;
						System.Console.WriteLine(" ERROR FOR "+entity.transform.position+" | "+entity.transform.name);
						System.Console.WriteLine("CZ : "+(current_zone==null?"N":"Y")+" | A: "+(result.a==null?"N":"Y")+" | B: "+(result.b==null?"N":"Y")+" | MonInf :"+(mi==null?"N":"Y")+"["+(mi==null?"---":mi.displayPhrase.english)+"]");
						
						System.Console.ResetColor();	
						
					}
				}
			}
			public bool SetFirstZone(BaseAIBrain brain, BaseEntity entity)
			{
				if(current_zone==null){
					current_zone = AIInformationZone.GetForPoint(entity.ServerPosition, true);
					if (current_zone == null)
					{
						return false;
					}
				}
				return true;
			}
			public bool SetPath(BaseAIBrain brain, BaseEntity entity)
			{
				this.path = brain.Navigator.Path;
				if (this.path == null)
				{
					this.path = brain.Navigator.Path = current_zone.GetNearestPath(entity.ServerPosition);
					if (this.path == null)
					{
						if(current_zone.movePoints.Count>0){
							AIMovePointPath aimpp =  current_zone.gameObject.GetComponent<AIMovePointPath>();
							if(aimpp==null){
								aimpp = current_zone.gameObject.AddComponent<AIMovePointPath>();
							}
							aimpp.Points = new List<AIMovePoint>(current_zone.movePoints.ToArray());
							this.path = brain.Navigator.Path = aimpp;
						}
						if (this.path == null)
						{
							return false;
						}
					}
				}
				try{
					//Find a point
					this.currentNodeIndex = this.path.FindNearestPointIndex(entity.ServerPosition);
					this.currentTargetPoint = this.path.FindNearestPoint(entity.ServerPosition);
				}catch(Exception e){
					if(StateImprovement._instance.MonuNet!=null){
						MonumentInfo mi = StateImprovement._instance.MonuNet.Call<MonumentInfo>("Find_Associated_Monument", current_zone);	
						if(mi==null){			
							System.Console.ForegroundColor= System.ConsoleColor.DarkCyan;
							System.Console.WriteLine(" No monument at "+entity.transform.position+" for "+entity.transform.name+"| ");
							
							System.Console.ResetColor();								
						}
						else{		
							System.Console.ForegroundColor= System.ConsoleColor.DarkCyan;
							System.Console.WriteLine(" Found monument:"+mi.displayPhrase.english+" for "+entity.transform.name+"| ");
							
							System.Console.ResetColor();									
						}
					}
					System.Console.WriteLine("IDX: "+currentNodeIndex+" | LEN: "+this.path.Points.Count);//			
				}
				if (this.currentTargetPoint == null)
				{
					return false;
				}
				//Get next index
				currentTargetPoint.ClearIfUsedBy(entity);
				
				int i = 0;
				//While next point is busy, pick next next point (up to path length)
				do{
					//Get next index
					
					this.currentNodeIndex = this.path.GetNextPointIndex(this.currentNodeIndex, ref this.pathDirection);
					this.currentTargetPoint = this.path.GetPointAtIndex(this.currentNodeIndex);
					i++;
				}while(i<this.path.Points.Count() && this.currentTargetPoint.InUse());
				
				currentTargetPoint.SetUsedBy(entity,20f);
				return true;
			}
			public override void StateEnter(BaseAIBrain brain, BaseEntity entity)
			{
				NextZoneProcessing(brain,entity);
				debugState("Start Follow Path",brain);
				if(self==null) 
					self= entity as HumanNPC;
					
				timeAtPoint=0;
				lastPosition = entity.transform.position;
				
				this.status = StateStatus.Error;
				brain.Navigator.SetBrakingEnabled(false);
				if(!SetFirstZone(brain,entity)){
					return;
				}
				//Set the path
				if(!SetPath(brain,entity)){
					return;
				}
				
				this.status = StateStatus.Running;
				
				// figure out necessary speed
				this.currentWaitTime = 0f;
				float distance = Vector3.Distance(this.currentTargetPoint.transform.position,entity.ServerPosition);
				BaseNavigator.NavigationSpeed speed = BaseNavigator.NavigationSpeed.Slowest;
				if(distance < 25){
					speed = BaseNavigator.NavigationSpeed.Slow;
				}
				else if(distance < 50){
					speed = BaseNavigator.NavigationSpeed.Normal;
				}
				else{
					speed = BaseNavigator.NavigationSpeed.Fast;
				}
				
				// Lock for self and head to point
				currentTargetPoint.SetUsedBy(entity,20f);
				brain.Navigator.SetDestination(this.currentTargetPoint.transform.position, speed, 0f, 0f);
			}
			
			public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
			{
				next_zone+=delta;
				/*
				debugState("Thinking",brain);				
				if (this.status == global::StateStatus.Error)
				{
					debugState("  Error return",brain);
					return this.status;
				}
				if (!brain.Navigator.Moving)
				{
					debugState("  Stuck or moving",brain);
					if (this.currentWaitTime <= 0f && this.currentTargetPoint.HasLookAtPoints())
					{
						debugState("    Look At Point",brain);
						global::UnityEngine.Transform randomLookAtPoint = this.currentTargetPoint.GetRandomLookAtPoint();
						if (randomLookAtPoint != null)
						{
							brain.Navigator.SetFacingDirectionOverride(global::UnityEngine.Vector3Ex.Direction2D(randomLookAtPoint.transform.position, entity.ServerPosition));
						}
					}
					if (this.currentTargetPoint.WaitTime > 0f)
					{
						this.currentWaitTime += delta;
					}
					if (this.currentTargetPoint.WaitTime <= 0f || this.currentWaitTime >= this.currentTargetPoint.WaitTime)
					{
						debugState("    Done waiting at point",brain);
						brain.Navigator.ClearFacingDirectionOverride();
						this.currentWaitTime = 0f;
						int num = this.currentNodeIndex;
						this.currentNodeIndex = this.path.GetNextPointIndex(this.currentNodeIndex, ref this.pathDirection);
						this.currentTargetPoint = this.path.GetPointAtIndex(this.currentNodeIndex);
						if ((!(this.currentTargetPoint != null) || this.currentNodeIndex != num) && (this.currentTargetPoint == null || !brain.Navigator.SetDestination(this.currentTargetPoint.transform.position, global::BaseNavigator.NavigationSpeed.Slow, 0f, 0f)))
						{
							debugState("      Error pathing in think - Target point: "+(this.currentTargetPoint == null).ToString()+" \n      Target Index and old : "+this.currentNodeIndex.ToString()+":"+num.ToString(), brain);
							return global::StateStatus.Error;
						}
					}
				}
				else if (this.currentTargetPoint != null)
				{
					debugState("  Goto point",brain);
					brain.Navigator.SetDestination(this.currentTargetPoint.transform.position, global::BaseNavigator.NavigationSpeed.Slow, 1f, 0f);
				}
				return global::StateStatus.Running;
				/*/
				
				debugState("Thinking",brain);
				TimeOnPath+=delta;
				currentLookTime+=delta;
				
				// If position changed, reset stuck time, otherwise increment
				if(entity.transform.position!=lastPosition){
					timeAtPoint=0;
				}
				else{
					timeAtPoint+=delta;
				}
				if (this.status == StateStatus.Error)
				{
					this.currentWaitTime = 0f;
					return this.status;
				}
				
				// If we're over the stuck limit we're stuck
				bool isStuck=timeAtPoint>stuckLimit;
				
				// If we're done moving or we're stuck
				if (!brain.Navigator.Moving||isStuck)
				{
					debugState("Stuck or not moving",brain);
					//If we're stuck flip direction
					if(isStuck){
						debugState("We're stuck",brain);
						pathDirection = UnityEngine.Random.Range(0f,1f)>0.5f?AIMovePointPath.PathDirection.Forwards:AIMovePointPath.PathDirection.Backwards;
						//Reset wait time
						this.currentWaitTime = 0f;
						return StateStatus.Error;
					}
					
					// If the point has lookat points, look at them
					if (this.currentWaitTime <= 0f && this.currentTargetPoint.HasLookAtPoints())
					{
						debugState("Look At Point",brain);
						UnityEngine.Transform randomLookAtPoint = this.currentTargetPoint.GetRandomLookAtPoint();
						if (randomLookAtPoint != null)
						{
							brain.Navigator.SetFacingDirectionOverride(UnityEngine.Vector3Ex.Direction2D(randomLookAtPoint.transform.position, entity.ServerPosition));
						}
					} 
					
					// If not, look at a sorta forward location
					else if(currentLookTime>10){
						
						debugState("Random look",brain);
						currentLookTime=0;
						float x=UnityEngine.Random.Range(-1.5f,1.5f);
						float y=UnityEngine.Random.Range(-1.5f,1.5f);
						brain.Navigator.SetFacingDirectionOverride(UnityEngine.Vector3Ex.Direction2D(entity.transform.position+entity.transform.forward+new Vector3(x,0,y), entity.ServerPosition));
					}
					
					//  Increment wait time
					this.currentWaitTime += delta;
					
					// If it`s time to change path, return finished
					if(TimeOnPath >  TimeLimit){
						debugState("Done with path, finishing",brain);
						brain.Navigator.ClearFacingDirectionOverride();
						return StateStatus.Finished;
					}
					
					// If point has no wait time or we're over the wait time
					if (this.currentTargetPoint.WaitTime <= 0f || this.currentWaitTime >= this.currentTargetPoint.WaitTime)
					{
						debugState("Done waiting at point",brain);
						//Allow freelook
						brain.Navigator.ClearFacingDirectionOverride();
						
						//Reset wait time
						this.currentWaitTime = 0f;
						
						//Take note of current index
						int num = this.currentNodeIndex;
						
						currentTargetPoint.ClearIfUsedBy(entity);
						
						return StateStatus.Finished;
					}else{
						currentTargetPoint.SetUsedBy(entity,20f);						
					}
				}
				// If still moving and there's a point, move to point
				else if (this.currentTargetPoint != null)
				{
					currentTargetPoint.SetUsedBy(entity,20f);
					debugState("Continuing Path",brain);
				}
				// Note last positino and keep running
				
				debugState("Still running at "+entity.transform.position.ToString(),brain);
				lastPosition=entity.transform.position;
				return StateStatus.Running;
				//*/
			}
				


		}
		public class CustomRoamState : BaseAIBrain.BaseRoamState
		{
			float TimeOnPath = 0f;
			public static float TimeLimit = 300;
			public static float TimeLimitMax = 600;
			HumanNPC self = null;
			public Vector3 lastPosition= new Vector3(0,0,0);
			public float timeAtPoint = 0;
			public static float stuckLimit = 5;
			public AIMovePointPath path;
			public StateStatus status;
			public AIMovePoint currentTargetPoint;
			public float currentWaitTime;
			private float currentLookTime = 0;
			private float next_zone = 3000;
			private AIMovePointPath.PathDirection pathDirection;
			public int currentNodeIndex;
			public AIInformationZone current_zone;
			
			public static bool debugMode = false;
			public static BaseAIBrain debugEntity = null;
			
			public void debugState(string text, BaseAIBrain entity){
				if(debugMode){
					if(debugEntity==null){
						debugEntity=entity;
					}
					if(debugEntity!=entity) return;
					System.Console.ForegroundColor = ConsoleColor.Red;
					System.Console.WriteLine(text);
					System.Console.ResetColor();
					
				}
			}
			
			public CustomRoamState(){
			}
			
			public void setNextPath(BaseAIBrain brain, BaseEntity entity){
				
				// Get zone
				AIInformationZone forPoint = self.GetInformationZone(entity.transform.position);
				AIMovePointPath best = brain.Navigator.Path;
				
				// If there's a zone and paths, check for best path
				if(forPoint!=null||forPoint.paths.Count()>=0){					
					
					
					// Find best path you can reach
					foreach (AIMovePointPath aizPath in forPoint.paths)
					{
						if(best== brain.Navigator.Path || Vector3.Distance(aizPath.transform.position,entity.transform.position) < Vector3.Distance(best.transform.position,entity.transform.position))
						{
							NavMeshPath navMeshPath = new NavMeshPath();
							if(NavMesh.CalculatePath(entity.transform.position, aizPath.transform.position, 25, navMeshPath)){
								if(navMeshPath.status == NavMeshPathStatus.PathComplete)
								{
									best=aizPath;
								}
							}
							else{
								brain.Navigator.Agent.Warp(entity.transform.position);
							}
						}
					}
				}
				// If same path just flip
				if(best==brain.Navigator.Path){
					this.pathDirection = (this.pathDirection==AIMovePointPath.PathDirection.Forwards?AIMovePointPath.PathDirection.Backwards:AIMovePointPath.PathDirection.Forwards);
					System.Console.ForegroundColor = ConsoleColor.Cyan;
					///System.Console.WriteLine("Flipping path");
					System.Console.ResetColor();
				}else{
					System.Console.ForegroundColor = ConsoleColor.Cyan;
					//System.Console.WriteLine("Changing path");
					System.Console.ResetColor();
				}
				
				brain.Navigator.Path = best;
				
				
			
			}
			
			public override void StateLeave(BaseAIBrain brain, BaseEntity entity)
			{
				base.StateLeave(brain, entity);
				//If we've been on path too long, find best path
				if(TimeOnPath >  TimeLimit){
					TimeOnPath=0f;
					setNextPath(brain,entity);
				}
				if(currentTargetPoint!=null){
					currentTargetPoint.ClearIfUsedBy(entity);
				}
				pathDirection = UnityEngine.Random.Range(0f,1f)>0.5f?AIMovePointPath.PathDirection.Forwards:AIMovePointPath.PathDirection.Backwards;
			}

			public override void StateEnter(BaseAIBrain brain, BaseEntity entity)
			{
				if(next_zone>StateImprovement.findNextZone && current_zone!=null){
					next_zone=0;
					//AIInformationZone source, out AIInformationZone target, out int path, out int idx
					AIInformationZone target = null;
					int path = -1;
					int idx = -1;
					StateImprovement.AssociationAIZ result = StateImprovement.getNextZone(current_zone);
					if(result.a==null&&result.b==null){
						if(StateImprovement._instance.MonuNet!=null){
							MonumentInfo mi = StateImprovement._instance.MonuNet.Call<MonumentInfo>("Find_Associated_Monument", current_zone);
							if(mi==null){
								System.Console.ForegroundColor= System.ConsoleColor.Red;
								System.Console.WriteLine(" No monument at "+entity.transform.position+" | "+entity.transform.name);
								System.Console.ResetColor();								
							}
							else{
								System.Console.ForegroundColor= System.ConsoleColor.Red;
								System.Console.WriteLine(" Failing monument: "+mi.displayPhrase.english+" | "+entity.transform.name);
								System.Console.ResetColor();
							}
						}
						return ;
					}else{		
						if(StateImprovement._instance.MonuNet!=null){
								MonumentInfo mi = StateImprovement._instance.MonuNet.Call<MonumentInfo>("Find_Associated_Monument", current_zone);	
							if(mi==null){			
								System.Console.ForegroundColor= System.ConsoleColor.Green;
								System.Console.WriteLine(" No monument at "+entity.transform.position+" | "+entity.transform.name);		
								
							}							
							else{
								System.Console.ForegroundColor= System.ConsoleColor.Green;
								System.Console.WriteLine(" Found monument:"+mi.displayPhrase.english+" | "+entity.transform.name);
							}
							if(result.b!=null&&current_zone!=result.b){
								System.Console.ForegroundColor= System.ConsoleColor.Green;
								System.Console.WriteLine(" CHANGED ZONED FOR "+entity.transform.name);
								current_zone=result.b;
								brain.Navigator.Path=null;
								this.path = null;
							}
							System.Console.ResetColor();	
						}
					
					}
				}
				debugState("Start Follow Path",brain);
				if(self==null) 
					self= entity as HumanNPC;
					
				timeAtPoint=0;
				lastPosition = entity.transform.position;
				
				this.status = StateStatus.Error;
				brain.Navigator.SetBrakingEnabled(false);
				
				//Set the path
				this.path = brain.Navigator.Path;
				if (this.path == null)
				{
					current_zone = AIInformationZone.GetForPoint(entity.ServerPosition, true);
					if (current_zone == null)
					{
						return;
					}
					this.path = brain.Navigator.Path = current_zone.GetNearestPath(entity.ServerPosition);
					if (this.path == null)
					{
						return;
					}
				}
				try{
					//Find a point
					this.currentNodeIndex = this.path.FindNearestPointIndex(entity.ServerPosition);
					this.currentTargetPoint = this.path.FindNearestPoint(entity.ServerPosition);
				}catch(Exception e){
					if(StateImprovement._instance.MonuNet!=null){
						MonumentInfo mi = StateImprovement._instance.MonuNet.Call<MonumentInfo>("Find_Associated_Monument", current_zone);	
						if(mi==null){			
							System.Console.ForegroundColor= System.ConsoleColor.DarkCyan;
							System.Console.Write(" No monument at "+entity.transform.position+" for "+entity.transform.name+"| ");
							
							System.Console.ResetColor();								
						}
						else{		
							System.Console.ForegroundColor= System.ConsoleColor.DarkCyan;
							System.Console.Write(" Found monument:"+mi.displayPhrase.english+" for "+entity.transform.name+"| ");
							
							System.Console.ResetColor();									
						}
					}
					System.Console.WriteLine("IDX: "+currentNodeIndex+" | LEN: "+this.path.Points.Count);//
				}
				if (this.currentTargetPoint == null)
				{
					return;
				}
				//Get next index
				currentTargetPoint.ClearIfUsedBy(entity);
				
				int i = 0;
				//While next point is busy, pick next next point (up to path length)
				do{
					//Get next index
					
					this.currentNodeIndex = this.path.GetNextPointIndex(this.currentNodeIndex, ref this.pathDirection);
					this.currentTargetPoint = this.path.GetPointAtIndex(this.currentNodeIndex);
					i++;
				}while(i<this.path.Points.Count() && this.currentTargetPoint.InUse());
				
				currentTargetPoint.SetUsedBy(entity,20f);
				this.status = StateStatus.Running;
				
				// figure out necessary speed
				this.currentWaitTime = 0f;
				float distance = Vector3.Distance(this.currentTargetPoint.transform.position,entity.ServerPosition);
				BaseNavigator.NavigationSpeed speed = BaseNavigator.NavigationSpeed.Slowest;
				if(distance < 25){
					speed = BaseNavigator.NavigationSpeed.Slow;
				}
				else if(distance < 50){
					speed = BaseNavigator.NavigationSpeed.Normal;
				}
				else{
					speed = BaseNavigator.NavigationSpeed.Fast;
				}
				
				// Lock for self and head to point
				currentTargetPoint.SetUsedBy(entity,20f);
				brain.Navigator.SetDestination(this.currentTargetPoint.transform.position, speed, 0f, 0f);
			}
			
			public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
			{
				next_zone+=delta;
				/*
				debugState("Thinking",brain);				
				if (this.status == global::StateStatus.Error)
				{
					debugState("  Error return",brain);
					return this.status;
				}
				if (!brain.Navigator.Moving)
				{
					debugState("  Stuck or moving",brain);
					if (this.currentWaitTime <= 0f && this.currentTargetPoint.HasLookAtPoints())
					{
						debugState("    Look At Point",brain);
						global::UnityEngine.Transform randomLookAtPoint = this.currentTargetPoint.GetRandomLookAtPoint();
						if (randomLookAtPoint != null)
						{
							brain.Navigator.SetFacingDirectionOverride(global::UnityEngine.Vector3Ex.Direction2D(randomLookAtPoint.transform.position, entity.ServerPosition));
						}
					}
					if (this.currentTargetPoint.WaitTime > 0f)
					{
						this.currentWaitTime += delta;
					}
					if (this.currentTargetPoint.WaitTime <= 0f || this.currentWaitTime >= this.currentTargetPoint.WaitTime)
					{
						debugState("    Done waiting at point",brain);
						brain.Navigator.ClearFacingDirectionOverride();
						this.currentWaitTime = 0f;
						int num = this.currentNodeIndex;
						this.currentNodeIndex = this.path.GetNextPointIndex(this.currentNodeIndex, ref this.pathDirection);
						this.currentTargetPoint = this.path.GetPointAtIndex(this.currentNodeIndex);
						if ((!(this.currentTargetPoint != null) || this.currentNodeIndex != num) && (this.currentTargetPoint == null || !brain.Navigator.SetDestination(this.currentTargetPoint.transform.position, global::BaseNavigator.NavigationSpeed.Slow, 0f, 0f)))
						{
							debugState("      Error pathing in think - Target point: "+(this.currentTargetPoint == null).ToString()+" \n      Target Index and old : "+this.currentNodeIndex.ToString()+":"+num.ToString(), brain);
							return global::StateStatus.Error;
						}
					}
				}
				else if (this.currentTargetPoint != null)
				{
					debugState("  Goto point",brain);
					brain.Navigator.SetDestination(this.currentTargetPoint.transform.position, global::BaseNavigator.NavigationSpeed.Slow, 1f, 0f);
				}
				return global::StateStatus.Running;
				/*/
				
				debugState("Thinking",brain);
				TimeOnPath+=delta;
				currentLookTime+=delta;
				
				// If position changed, reset stuck time, otherwise increment
				if(entity.transform.position!=lastPosition){
					timeAtPoint=0;
				}
				else{
					timeAtPoint+=delta;
				}
				if (this.status == StateStatus.Error)
				{
					this.currentWaitTime = 0f;
					return this.status;
				}
				
				// If we're over the stuck limit we're stuck
				bool isStuck=timeAtPoint>stuckLimit;
				
				// If we're done moving or we're stuck
				if (!brain.Navigator.Moving||isStuck)
				{
					debugState("Stuck or not moving",brain);
					//If we're stuck flip direction
					if(isStuck){
						debugState("We're stuck",brain);
						pathDirection = UnityEngine.Random.Range(0f,1f)>0.5f?AIMovePointPath.PathDirection.Forwards:AIMovePointPath.PathDirection.Backwards;
						//Reset wait time
						this.currentWaitTime = 0f;
						return StateStatus.Error;
					}
					
					// If the point has lookat points, look at them
					if (this.currentWaitTime <= 0f && this.currentTargetPoint.HasLookAtPoints())
					{
						debugState("Look At Point",brain);
						UnityEngine.Transform randomLookAtPoint = this.currentTargetPoint.GetRandomLookAtPoint();
						if (randomLookAtPoint != null)
						{
							brain.Navigator.SetFacingDirectionOverride(UnityEngine.Vector3Ex.Direction2D(randomLookAtPoint.transform.position, entity.ServerPosition));
						}
					} 
					
					// If not, look at a sorta forward location
					else if(currentLookTime>10){
						
						debugState("Random look",brain);
						currentLookTime=0;
						float x=UnityEngine.Random.Range(-1.5f,1.5f);
						float y=UnityEngine.Random.Range(-1.5f,1.5f);
						brain.Navigator.SetFacingDirectionOverride(UnityEngine.Vector3Ex.Direction2D(entity.transform.position+entity.transform.forward+new Vector3(x,0,y), entity.ServerPosition));
					}
					
					//  Increment wait time
					this.currentWaitTime += delta;
					
					// If it`s time to change path, return finished
					if(TimeOnPath >  TimeLimit){
						debugState("Done with path, finishing",brain);
						brain.Navigator.ClearFacingDirectionOverride();
						return StateStatus.Finished;
					}
					
					// If point has no wait time or we're over the wait time
					if (this.currentTargetPoint.WaitTime <= 0f || this.currentWaitTime >= this.currentTargetPoint.WaitTime)
					{
						debugState("Done waiting at point",brain);
						//Allow freelook
						brain.Navigator.ClearFacingDirectionOverride();
						
						//Reset wait time
						this.currentWaitTime = 0f;
						
						//Take note of current index
						int num = this.currentNodeIndex;
						
						currentTargetPoint.ClearIfUsedBy(entity);
						
						return StateStatus.Finished;
					}else{
						currentTargetPoint.SetUsedBy(entity,20f);						
					}
				}
				// If still moving and there's a point, move to point
				else if (this.currentTargetPoint != null)
				{
					currentTargetPoint.SetUsedBy(entity,20f);
					debugState("Continuing Path",brain);
				}
				// Note last positino and keep running
				
				debugState("Still running at "+entity.transform.position.ToString(),brain);
				lastPosition=entity.transform.position;
				return StateStatus.Running;
				//*/
			}
				


		}

		public class CustomTakeCoverState : ScientistBrain.TakeCoverState{
			
			public Vector3 lastPosition= new Vector3(0,0,0);
			public float timeAtPoint = 0;
			public static float stuckLimit = 5;
			
			public override void StateEnter(BaseAIBrain brain, BaseEntity entity)
			{
				lastPosition=entity.transform.position;
				timeAtPoint = 0;	
				this.status = StateStatus.Running;
				if (!this.StartMovingToCover(entity as HumanNPC))
				{
					this.status = StateStatus.Error;
				}
				
			}
			public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
			{
				if(lastPosition==entity.transform.position){
					timeAtPoint+=delta;
					if(timeAtPoint>stuckLimit){
						return StateStatus.Finished;
					}
				}
				lastPosition=entity.transform.position;
				timeAtPoint = 0;
				
				if (brain.Navigator.Moving)
				{
					FaceTarget();
					return StateStatus.Running;
				}
				return StateStatus.Finished;
			}
			private void FaceCoverFromEntity()
			{
				this.coverFromEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
				if (this.coverFromEntity == null)
				{
					this.brain.Navigator.ClearFacingDirectionOverride();
					return;
				}
				this.brain.Navigator.SetFacingDirectionEntity(this.coverFromEntity);
			}
			private void FaceTarget()
			{
				BaseEntity baseEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
				if (baseEntity == null)
				{
					FaceCoverFromEntity();
					return;
				}
				this.brain.Navigator.SetFacingDirectionEntity(baseEntity);
			}
			private bool StartMovingToCover(HumanNPC entity)
			{
				this.coverFromEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
				if (this.coverFromEntity == null)
				{
					return false;
				}
				UnityEngine.Vector3 vector = (this.coverFromEntity ? this.coverFromEntity.transform.position : (entity.transform.position + entity.LastAttackedDir * 30f));
				AIInformationZone informationZone = entity.GetInformationZone(entity.transform.position);
				if (informationZone == null)
				{
					return false;
				}
				float num = ((entity.SecondsSinceAttacked < 2f) ? 2f : 0f);
				float bestCoverPointMaxDistance = this.brain.Navigator.BestCoverPointMaxDistance;
				AICoverPoint bestCoverPoint = informationZone.GetBestCoverPoint(entity.transform.position, vector, num, bestCoverPointMaxDistance, entity, true);
				if (bestCoverPoint == null)
				{
					return false;
				}
				UnityEngine.Vector3 position = bestCoverPoint.transform.position;
				if (!this.brain.Navigator.SetDestination(position, BaseNavigator.NavigationSpeed.Normal, 0f, 0f))
				{
					return false;
				}
				this.FaceCoverFromEntity();
				this.brain.Events.Memory.AIPoint.Set(bestCoverPoint, 4);
				bestCoverPoint.SetUsedBy(entity);
				return true;
			}
			private StateStatus status = StateStatus.Error;
			private BaseEntity coverFromEntity;
	
		}	
		#endregion
		
		#region Load
		
		Cordyceps cordy;

		public static string[] targets= new string[]{"assets/rust.ai/agents/npcplayer/humannpc/banditguard/npc_bandit_guard.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_any.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_lr300.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_mp5.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_pistol.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_shotgun.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_heavy.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_junkpile_pistol.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_peacekeeper.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_excavator.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roam.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roamtethered.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_patrol.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/tunneldweller/npc_tunneldweller.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/underwaterdweller/npc_underwaterdweller.prefab"};

		void OnEntitySpawned(HumanNPC hn){
			if(hn.Brain==null) return;
			if(hn.Brain.AIDesign==null) return;
			hn.Brain.SwitchToState(AIState.Idle,hn.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Idle).ID);
		}
		
        void Loaded(){
			
			cordy = (Cordyceps)Manager.GetPlugin("Cordyceps");
			loadStates();
			StateImprovement._instance=this;
		}
		void loadStates(){
			foreach(string s in targets){
				CustomTakeCoverState tcs= new CustomTakeCoverState();
				cordy.AssignHumanState(s, tcs.StateType, tcs);
				CustomFollowPathState fps= new CustomFollowPathState();
				cordy.AssignHumanState(s, fps.StateType, fps);
				CustomRoamState frs= new CustomRoamState();
				cordy.AssignHumanState(s, frs.StateType, frs);
			}			
		}
		
		#endregion
		
	}
}