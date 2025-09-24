
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
	[Info("Hitstyle", "obsol", "0.0.1")]
	[Description("Hit-only hostile that doesn't care about animals. Hit-only hostile > hit hostile > hitstile > hitstyle because it looks better. Never let me name things.")]
	public class Hitstyle : CovalencePlugin
	{
		private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
		private void SendChatMsg(BasePlayer pl, string msg) => _rustPlayer.Message(pl, msg,  "<color=#00ff00>[Cordyceps]</color>", 0, Array.Empty<object>());			
		public Configuration config;
		
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
        bool NPCHostile = true;
        object OnEntityMarkHostile(BaseCombatEntity entity, float duration)
        {
            return entity;
        }
        object CanEntityBeHostile(BaseCombatEntity entity)
        {
			if((entity as BasePlayer)) return (entity as BasePlayer).State.unHostileTimestamp > TimeEx.currentTimestamp;
			return (entity.unHostileTime > UnityEngine.Time.realtimeSinceStartup);
			
		}
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
			if(info.Initiator==null||info.HitEntity==null) return null;
            BaseCombatEntity attacker = (info.Initiator as BaseCombatEntity);
            BaseCombatEntity victim = (info.HitEntity as BaseCombatEntity);
            if(attacker==null||victim==null) return null;
			double currentTimestamp = TimeEx.currentTimestamp;
			double val = currentTimestamp + (double)(600);
			float b = UnityEngine.Time.realtimeSinceStartup + 600;
			if(((attacker.HasBrain)==true && !NPCHostile) || ((victim as BasePlayer)?.State.unHostileTimestamp - currentTimestamp>0 - 300) || ((victim.unHostileTime - UnityEngine.Time.realtimeSinceStartup - 300) > 0))  return null;
			if(attacker.HasBrain && (attacker as ScientistNPC == victim as ScientistNPC) && (attacker as BanditGuard == victim as BanditGuard)&& (attacker as UnderwaterDweller == victim as UnderwaterDweller)&& (attacker as TunnelDweller == victim as TunnelDweller)) return null; 
			attacker.unHostileTime = Mathf.Max(attacker.unHostileTime, b);
			if(attacker is BasePlayer){
				(attacker as BasePlayer).State.unHostileTimestamp = Math.Max((attacker as BasePlayer).State.unHostileTimestamp, val);
				(attacker as BasePlayer).DirtyPlayerState();
				double num = Math.Max((attacker as BasePlayer).State.unHostileTimestamp - currentTimestamp, 0.0);
				(attacker as BasePlayer).ClientRPCPlayer<float>(null, (attacker as BasePlayer), "SetHostileLength", (float)num);
			}
			return null;
        }
        //set ConVar.Sentry.targetall to true, this'll handle authed players. This allows npcs to also get shot. Requires IsNPC hack
        object OnTurretTarget(AutoTurret turret, BaseCombatEntity entity)
        {
            BasePlayer bp = (entity as BasePlayer);
            if(bp==null) return null;
            if(!turret.IsAuthed(bp)) return null;
			return entity;
        }
        object OnTurretCheckHostile(AutoTurret turret, BaseCombatEntity entity)
        {
			//Puts(entity.name+" is hostile? "+entity.IsHostile().ToString());
			if(entity==null)return null;
			if((entity as ScientistNPC && turret.gameObject.name.Contains("sentry.sci")) || (entity as BanditGuard && turret.gameObject.name.Contains("sentry.ban")) || (entity is BaseNpc)){
				return entity.IsHostile();//
			}
			return null;
        }
        object OnTurretCheckHostile(NPCAutoTurret turret, BaseCombatEntity entity)
        {
			//Puts(entity.name+" is hostile? "+entity.IsHostile().ToString());
			if(entity==null)return null;
			if((!(entity as ScientistNPC) && turret.gameObject.name.Contains("sentry.sci")) || (!(entity as BanditGuard) && turret.gameObject.name.Contains("sentry.ban"))|| (entity is BaseNpc)){
				return entity.IsHostile();//
			}
			return null;
        }
    }
}