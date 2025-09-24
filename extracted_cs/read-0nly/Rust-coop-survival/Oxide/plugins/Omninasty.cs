
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
	[Info("Omninasty", "obsol", "0.2.1")]
	[Description("Makes NPCs and Animals attack any NPC/animal that doesn't share their prefab name")]
	public class Omninasty : RustPlugin{
		
		
		public Configuration config;
		public class Configuration
		{
			[JsonProperty("Factions", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<string> Factions = new List<string>();
			[JsonProperty("Assignments", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string,string> Assignments = new Dictionary<string,string>();
			[JsonProperty("Alliances", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string,List<string>> Alliances = new Dictionary<string,List<string>>();
			public string ToJson() => JsonConvert.SerializeObject(this);				
			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
			
		}
		protected override void LoadDefaultConfig(){
			config = new Configuration();//
			config.Factions.Add("Default");
			config.Factions.Add("Player-Aligned");
			config.Factions.Add("Player");
			config.Alliances.Add("Player-Aligned", new List<string>());
			config.Alliances["Player-Aligned"].Add("Player");
		}
		protected override void LoadConfig(){
			base.LoadConfig();
			try{
				config = Config.ReadObject<Configuration>();
				if (config == null) throw new JsonException();
				if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys)){
					
				}
			}
			catch(Exception e){
				LoadDefaultConfig();
				}
		}
		protected override void SaveConfig(){
			Config.WriteObject(config, true);
		}		
		private void OnServerInitialized()
        {
			LoadConfig();
		}
		
		[ConsoleCommand("omni.save")]
		private void surv_saveconfig(ConsoleSystem.Arg arg){
			SaveConfig();
			Puts("Omni Saved");
		}
		[ConsoleCommand("omni.load")]
		private void surv_loadconfig(ConsoleSystem.Arg arg){
			LoadConfig();
			Puts("Omni Loaded");
		}
		private void Unload()
		{
			SaveConfig();
		}
		
		
		void OnAIInitialize(BaseAIBrain brain)
		{
			if(brain!=null){
				brain.gameObject.GetComponent<BaseCombatEntity>().faction=BaseCombatEntity.Faction.Horror;
				brain.Senses.senseTypes = (EntityType)67;
				BaseEntity be = brain.GetBrainBaseEntity();
				brain.Senses.listenRange = Mathf.Min(brain.Senses.listenRange, 100);
				brain.Senses.visionCone = Mathf.Min(brain.Senses.visionCone, 100);
				brain.Senses.maxRange = Mathf.Min(brain.Senses.maxRange, 50);
				brain.Senses.targetLostRange = Mathf.Min(brain.Senses.targetLostRange, 100);
				brain.Senses.checkLOS = true;
				brain.Senses.checkVision = true;
				brain.gameObject.GetComponent<BaseCombatEntity>().SetMaxHealth(Mathf.Min(brain.gameObject.GetComponent<BaseCombatEntity>().health, 200));
			}
			
		}
		void OnEntitySpawned(HumanNPC be){
			
				if(!config.Assignments.ContainsKey(be.GetType().ToString())){
					if(be is BanditGuard){
						config.Assignments.Add(be.GetType().ToString(),"Player-Aligned");				
					}else if (be is BasePlayer||be is BaseNpc){
						config.Assignments.Add(be.GetType().ToString(),"Default");				
					}						
				}
		}
		void OnEntitySpawned(BaseNpc be){
			
				if(!config.Assignments.ContainsKey(be.GetType().ToString())){
					if(be is BanditGuard){
						config.Assignments.Add(be.GetType().ToString(),"Player-Aligned");				
					}else{
						config.Assignments.Add(be.GetType().ToString(),"Default");				
					}						
				}
		}
		bool? OnAICaresAbout(AIBrainSenses aibs, BasePlayer entity){
			if(entity!= aibs.owner)
				return CaresAbout(aibs,entity);
			else
				return false;
		}
		bool? OnAICaresAbout(AIBrainSenses aibs, BaseAnimalNPC entity){
			if(entity!= aibs.owner){
				return CaresAbout(aibs,entity);}
			else
				return false;
		}
		bool CaresAbout(AIBrainSenses aibs, BaseEntity entity){			
			if (entity == null||entity.Health() <= 0f){
				return false;
				
			}
			global::BaseCombatEntity baseCombatEntity = entity as global::BaseCombatEntity;
			global::BasePlayer basePlayer = entity as global::BasePlayer;	
			if (baseCombatEntity != null && baseCombatEntity.IsDead())
			{
				return false;
			}
			if (aibs.listenRange > 0f && baseCombatEntity != null && baseCombatEntity.TimeSinceLastNoise <= 1f && baseCombatEntity.CanLastNoiseBeHeard(aibs.owner.transform.position, aibs.listenRange))
			{
				return true;
			}
			float num = float.PositiveInfinity;
			if (baseCombatEntity != null && ConVar.AI.accuratevisiondistance)
			{
				num = Vector3.Distance(aibs.owner.transform.position, baseCombatEntity.transform.position);
				if (num > aibs.maxRange)
				{
					return false;
				}
			}
			Vector3 rhs = Vector3Ex.Direction(entity.transform.position, aibs.owner.transform.position);
			if (aibs.checkVision && Vector3.Dot((aibs.playerOwner != null) ? aibs.playerOwner.eyes.BodyForward() : aibs.owner.transform.forward, rhs) < aibs.visionCone){
			
				
				if (!aibs.ignoreNonVisionSneakers)
				{
					return false;
				}
				if (basePlayer != null && !basePlayer.IsNpc)
				{
					if (!ConVar.AI.accuratevisiondistance)
					{
						num = Vector3.Distance(aibs.owner.transform.position, basePlayer.transform.position);
					}
					if ((basePlayer.IsDucked() && num >= 4f) || num >= 15f)
					{
						return false;
					}
				}
			}
			if (aibs.checkLOS && aibs.ownerAttack != null)
			{
				bool flag = entity.IsVisible(aibs.owner.CenterPoint(), entity.CenterPoint(), float.PositiveInfinity);
				
				aibs.Memory.SetLOS(entity, flag);
				if (!flag)
				{
					return false;
				}
			}
			return true;
		}
		object OnNpcTargetSense(BaseEntity owner, BaseEntity ent, AIBrainSenses brainSenses){
			if(owner==null||ent==null ||brainSenses == null){return null;}
			global::IAISenses iaisenses = owner as global::IAISenses;
			global::BasePlayer basePlayer = ent as global::BasePlayer;	
			BaseNpc animal =ent as global::BaseNpc;
			if(basePlayer==animal){
				return owner;
			}
			bool flag = false;
			if (iaisenses != null && iaisenses.IsThreat(ent))
			{
				flag = true;
				if (brainSenses != null)
				{
					brainSenses.LastThreatTimestamp = UnityEngine.Time.realtimeSinceStartup;
				}
			}
			for (int i = 0; i < brainSenses.Memory.All.Count; i++)
			{
				if (brainSenses.Memory.All[i].Entity == ent)
				{
					Rust.AI.SimpleAIMemory.SeenInfo seenInfo = brainSenses.Memory.All[i];
					seenInfo.Position = ent.transform.position;
					seenInfo.Timestamp = Mathf.Max(UnityEngine.Time.realtimeSinceStartup, seenInfo.Timestamp);
					brainSenses.Memory.All[i] = seenInfo;
					return owner;//
				}
			}
			string selfFaction = (config.Assignments.ContainsKey(owner.GetType().ToString())?config.Assignments[owner.GetType().ToString()]:"Player");
			bool isplayerflag= basePlayer!=null && basePlayer.HasPlayerFlag(BasePlayer.PlayerFlags.Connected);
			string otherFaction = (isplayerflag && config.Assignments.ContainsKey(basePlayer.userID.ToString())?config.Assignments[basePlayer.userID.ToString()]:(config.Assignments.ContainsKey(ent.GetType().ToString())?config.Assignments[ent.GetType().ToString()]:"Player"));
			
			if(selfFaction==otherFaction || (config.Alliances[selfFaction].Contains(otherFaction)) || (isplayerflag && config.Alliances[selfFaction].Contains(basePlayer.userID.ToString()))){
				brainSenses.Memory.Friendlies.Add(ent);
				
			}else{
				brainSenses.Memory.Players.Add(ent);	
				brainSenses.Memory.Targets.Add(ent);
			}
		
			brainSenses.Memory.All.Add(new Rust.AI.SimpleAIMemory.SeenInfo
			{
				Entity = ent,
				Position = ent.transform.position,
				Timestamp = UnityEngine.Time.realtimeSinceStartup
			});
			return owner;
		}
		public string getFaction(BaseEntity be){
			if(be==null)return null;
			BasePlayer bp = be as BasePlayer;
			bool isOnline = bp!=null && bp.HasPlayerFlag(BasePlayer.PlayerFlags.Connected);
			if(isOnline){
				if(config.Assignments.ContainsKey(bp.userID.ToString())){
					return config.Assignments[bp.userID.ToString()];
				}else{
					config.Assignments.Add(bp.userID.ToString(),"Player");
					return config.Assignments[bp.userID.ToString()];
					
				}
				
			}else{
				if(config.Assignments.ContainsKey(be.GetType().ToString())){
					return config.Assignments[be.GetType().ToString()];
				}else{
					config.Assignments.Add(be.GetType().ToString(),null);
					return config.Assignments[be.GetType().ToString()];
				}
				
			}
		}
		public List<string> getAlliances(string faction){
			if(!config.Alliances.ContainsKey(faction))
				config.Alliances.Add(faction,new List<string>());
			return config.Alliances[faction];
		}
		public void addAlliance(string faction, string alliance){
			if(!config.Alliances.ContainsKey(faction))
				config.Alliances.Add(faction,new List<string>());
			if(!config.Alliances[faction].Contains(alliance))
				config.Alliances[faction].Add(alliance);
		}
		public void addAlliance(string faction, BasePlayer basePlayer){
			bool isplayerflag= basePlayer!=null && basePlayer.HasPlayerFlag(BasePlayer.PlayerFlags.Connected);
			string alliance = (isplayerflag?basePlayer.userID.ToString():basePlayer.GetType().ToString());
			if(!config.Alliances.ContainsKey(faction))
				config.Alliances.Add(faction,new List<string>());
			if(!config.Alliances[faction].Contains(alliance))
				config.Alliances[faction].Add(alliance);
		}
		public void deleteAlliance(string faction, string alliance){
			if(!config.Alliances.ContainsKey(faction))
				config.Alliances.Add(faction,new List<string>());
			if(config.Alliances[faction].Contains(alliance))
				config.Alliances[faction].Remove(alliance);
		}
		public void deleteAlliance(string faction, BasePlayer basePlayer){
			bool isplayerflag= basePlayer!=null && basePlayer.HasPlayerFlag(BasePlayer.PlayerFlags.Connected);
			string alliance = (isplayerflag?basePlayer.userID.ToString():basePlayer.GetType().ToString());
			if(!config.Alliances.ContainsKey(faction))
				config.Alliances.Add(faction,new List<string>());
			if(!config.Alliances[faction].Contains(alliance))
				config.Alliances[faction].Remove(alliance);
		}
		public void setFaction(BaseEntity be, string s){
			BasePlayer bp = be as BasePlayer;
			bool isOnline = bp!=null && bp.HasPlayerFlag(BasePlayer.PlayerFlags.Connected);
			if(isOnline){
				if(config.Assignments.ContainsKey(bp.userID.ToString())){
					config.Assignments[bp.userID.ToString()]=s;
				}else{
					config.Assignments.Add(bp.userID.ToString(),s);
				}
				
			}else{
				if(config.Assignments.ContainsKey(be.GetType().ToString())){
					config.Assignments[be.GetType().ToString()]=s;
				}else{
					config.Assignments.Add(be.GetType().ToString(),s);
				}
				
			}

		}
		
		
		public bool isFriend(BaseEntity be, BaseEntity be2){
			
			string selfFaction = getFaction(be);
			string otherFaction = getFaction(be2);
			
			BasePlayer basePlayer = be as BasePlayer;
			BasePlayer basePlayer2 = be2 as BasePlayer;
			bool isplayerflag= (basePlayer!=null && basePlayer.HasPlayerFlag(BasePlayer.PlayerFlags.Connected));
			bool isplayerflag2=(basePlayer2!=null && basePlayer2.HasPlayerFlag(BasePlayer.PlayerFlags.Connected));
			
			return !(selfFaction!=null&&otherFaction!=null)&&selfFaction==otherFaction || 
			( (config.Alliances.ContainsKey(selfFaction))&&(config.Alliances[selfFaction].Contains(otherFaction))  )|| 
			(isplayerflag2 && (config.Alliances.ContainsKey(selfFaction))&&config.Alliances[selfFaction].Contains(basePlayer2.userID.ToString())) || 
			(isplayerflag && (config.Alliances.ContainsKey(otherFaction))&&config.Alliances[otherFaction].Contains(basePlayer.userID.ToString()));
			
		}
		
		
		
		public static bool CanSeeTarget(BaseEntity observer,BaseEntity entity)
		{
			return !(entity == null) && entity.IsVisible(observer.GetEntity().CenterPoint(), entity.CenterPoint(), 50);
		}
	}
	
}