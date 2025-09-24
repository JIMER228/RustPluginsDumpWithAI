
using Oxide.Plugins;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json;
using Convert = System.Convert;
using Network;
using System.Linq;
using System.Text;
using Oxide.Core.Libraries.Covalence;
using UnityEngine; 
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using Rust.Ai;
using Oxide.Ext.RustEdit;
using Oxide.Ext.RustEdit.NPC;

namespace Oxide.Plugins
{
	[Info("Omnitrigger", "obsol", "0.0.1")]
	[Description("Allows trap triggers to react to animals and NPCs")]
	public class Omnitrigger : CovalencePlugin
	{
				
				
		Configuration config;
		public class Configuration
		{
			[JsonProperty("LayerMask", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public int LayerMask = 0;
			public string ToJson() => JsonConvert.SerializeObject(this);				
			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
			
		}
		protected override void LoadDefaultConfig(){
			config = new Configuration();//
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
		
		private void Unload()
		{
			SaveConfig();
		}
		object CanBuild(Planner planner, Construction prefab, Construction.Target target)
		{
			return null;
		}
		void OnEntitySpawned(BaseNetworkable entity)
		{
			BaseEntity be = (entity as BaseEntity);
			BaseAnimalNPC bn = (entity as BaseAnimalNPC);
			HumanNPC hn = (entity as HumanNPC);
			if(be==null)return;
			if(be is GunTrap){
					GunTrap gt = be as GunTrap;
					gt.trigger.interestLayers = gt.trigger.interestLayers|config.LayerMask;
			}else if(be is AutoTurret){
					AutoTurret gt = be as AutoTurret;//
					gt.targetTrigger.interestLayers = gt.targetTrigger.interestLayers|config.LayerMask;
			}else if(be is FlameTurret){
					FlameTurret gt = be as FlameTurret;
					gt.trigger.interestLayers = gt.trigger.interestLayers|config.LayerMask;
			}else if(be is TeslaCoil){
					TeslaCoil gt = be as TeslaCoil;
					gt.targetTrigger.interestLayers = gt.targetTrigger.interestLayers|config.LayerMask;
			}else if((be is BaseTrap)){
				TriggerBase btt = be.GetComponentInChildren<TriggerBase>();
				if(btt==null){
					btt = be.GetComponentInChildren<BaseTrapTrigger>();
				}
				if(btt==null){
					btt = be.GetComponentInChildren<BearTrapTrigger>();
				}
				if(btt!=null){
					btt.interestLayers = btt.interestLayers|config.LayerMask;
				}
			} else if((entity is BaseAnimalNPC) && bn.gameObject!=null){
				try{
					Rigidbody rb = bn.gameObject.GetComponent<Rigidbody>();
					if(rb==null){rb = bn.gameObject.GetComponentInChildren<Rigidbody>();}
					if(rb==null){rb=bn.gameObject.AddComponent<Rigidbody>();}
					BoxCollider bc = (bn.gameObject.GetComponent<BoxCollider>()? bn.gameObject.GetComponent<BoxCollider>():bn.gameObject.AddComponent<BoxCollider>());
					if(bc!=null){
						bc.size = new Vector3(1f,1f,1f);
						bc.center = new Vector3(0,0,0);
						bc.enabled = true;
					}
					if(rb!=null){
						rb.detectCollisions = true;
						rb.useGravity = false;	
						bn.ForceUpdateTriggers();	
					}					
					config.LayerMask = config.LayerMask | (1 << bn.gameObject.layer);
				}catch (Exception e){
					Puts(e.ToString());
				}
			
			} else if((entity is HumanNPC) && hn.gameObject!=null){
				try{
					Rigidbody rb = hn.gameObject.GetComponent<Rigidbody>();
					if(rb==null){rb = hn.gameObject.GetComponentInChildren<Rigidbody>();}
					if(rb==null){rb = hn.gameObject.AddComponent<Rigidbody>();}
					BoxCollider bc = hn.gameObject.AddComponent<BoxCollider>();
					bc.size = new Vector3(1f,1f,1f);
					bc.center = new Vector3(0,0,0);
					bc.enabled = true;
					rb.detectCollisions = true;
					rb.isKinematic = true;		
					rb.useGravity = false;	
					config.LayerMask = config.LayerMask | (1 << hn.gameObject.layer);
					hn.ForceUpdateTriggers();					
				}catch ( Exception e){
					Puts(e.ToString());
				}
			
			}
		}
		object OnTrapTrigger(BaseTrap trap, GameObject go)
		{
			return trap;
			return null;
		}
	
	}
}