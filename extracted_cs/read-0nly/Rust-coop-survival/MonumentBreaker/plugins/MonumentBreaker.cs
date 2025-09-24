
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
	[Info("MonumentBreaker", "obsol", "0.0.1")]
	[Description("For breaking monuments")]
	public class MonumentBreaker : CovalencePlugin
	{
		
		#region config
		public ConfigData config;
		public class ConfigData
		{
			[JsonProperty("prefabswaps", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string,string> Prefabswaps = new  Dictionary<string,string>();
			[JsonProperty("ProcessTargets", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string,string> ProcessTargets = new  Dictionary<string,string>();
			[JsonProperty("ignorelist", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<string> ignorelist = new  List<string>();
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
		
        
		void OnServerInitialized()
        {
			LoadConfig();
			if(config.ProcessTargets.ContainsKey(World.Url.ToLower())){
				string target = config.ProcessTargets[World.Url.ToLower()];
				SerializeMonumentHierarchy(target);
				RecomposeMonument(target);				
				ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "quit");	//		
			}
		}
        
		public class PrefabInfo{
			
			public string Name = "";
			public Vector3 Position = new Vector3(0,0,0);
			public Quaternion Rotation = Quaternion.identity;
			public Vector3 Scale = new Vector3(0,0,0);
			public string nearestPrefab = "";
			public string[] Components = new string[] {};
			public List<PrefabInfo> Children = new List<PrefabInfo>();
		}
		#region chatcmds //
		// Breaker.SerializeMonumentHierarchy compound.prefab
		[Command("Breaker.SerializeMonumentHierarchy")]
        void SerializeMonumentHierarchy_cmd(IPlayer player, string command, string[] args)
        {
			SerializeMonumentHierarchy(args[0]);
		}
        [Command("Breaker.DemoMonumentSwap")]
        void DemoMonumentSwap_cmd(IPlayer player, string command, string[] args)
        {
			DemoMonumentSwap(args[0]);
			
		}
        [Command("Breaker.ExtractLayers")]
        void ExtractLayers_cmd(IPlayer player, string command, string[] args)
        {
			ExtractLayers();
		}
		// Breaker.RecomposeMonument compound.prefab
        [Command("Breaker.RecomposeMonument")]
        void RecomposeMonument_cmd(IPlayer player, string command, string[] args)
        {
			RecomposeMonument(args[0]);
		}
		
        [Command("Breaker.SaveConfig")]//
        void SaveConfig_cmd(IPlayer player, string command, string[] args)
        {
			SaveConfig();
		}
        [Command("Breaker.LoadConfig")]//
        void LoadConfig_cmd(IPlayer player, string command, string[] args)
        {
			LoadConfig();
		}
		#endregion
		
		#region util
		
		public string NearestPrefab(string s){
			string clean = System.Text.RegularExpressions.Regex.Replace(s, "\\s\\(\\d+\\)","").ToLower();
			string result = "";
			if(config.Prefabswaps.ContainsKey(clean)){
				return config.Prefabswaps[clean];
			}
			foreach(string key in StringPool.toNumber.Keys){
				string keylower = key.ToLower();
				if(keylower.StartsWith("assets/") && keylower.Contains("/"+clean+".prefab")){
					result=key;
				}
			}
			if(result==""){
				foreach(string key in StringPool.toNumber.Keys){
					string keylower = key.ToLower();
					if(keylower.StartsWith("assets/") && keylower.Contains("/"+clean)){
						result=key;
					}
				}				
			}
			if(result==""){
				result="assets/bundled/prefabs/modding/volumes_and_triggers/hurt/damage_trigger_low.prefab:"+clean;
			}
			return result;
		}
		
		public List<Dictionary<string,object>> recursiveTransform(int level, Transform t){
			string json = "";
			if(level<3){
				Console.ForegroundColor = ConsoleColor.Red;
			}else{
				Console.ForegroundColor = ConsoleColor.DarkRed;			
			}
			System.Console.Write("#");
			List<Dictionary<string,object>> result = new List<Dictionary<string,object>>();
			foreach(Transform t1 in t){
				Dictionary<string,object> resultChild = new Dictionary<string,object>();
				if(t1.transform.name.ToLower().Contains("christmas")||t1.transform.name.ToLower().Contains("xmas")||config.ignorelist.Contains(t1.transform.name)) continue;
				resultChild.Add("Name",t1.transform.name);
				resultChild.Add("Position",t1.transform.position);
				resultChild.Add("Rotation",t1.transform.rotation);
				resultChild.Add("Scale",t1.transform.localScale);
				resultChild.Add("nearestPrefab",NearestPrefab(t1.transform.name));
				resultChild.Add("Components",new List<string>()); 
				foreach(Component c in t.gameObject.GetComponents<Component>()){
					(resultChild["Components"] as List<string>).Add(c.GetType().ToString());
				}
				resultChild.Add("Children",recursiveTransform(level+1,t1));
				result.Add(resultChild);
			}
			return result;				
			
		}
		public void recursiveList(int level, List<Dictionary<string,object>> t){
			foreach(Dictionary<string,object> dc in t){
				Puts(new String(' ',level)+"Name:"+dc["Name"]);
				Puts(new String(' ',level)+"Pos:"+dc["Pos"]);
				Puts(new String(' ',level)+"Rot:"+dc["Rot"]);
				Puts(new String(' ',level)+"Children:");
				recursiveList(level+1,dc["Children"] as List<Dictionary<string,object>>);
			}
		}
		#endregion
		
		public void SerializeMonumentHierarchy(string s = "compound.prefab"){
			List<Dictionary<string,object>> result = new List<Dictionary<string,object>>();
			UnityEngine.Object[] dvs = GameObject.FindObjectsOfType<Transform>();
			bool skipped = false;
			foreach(Transform bp in dvs.ToArray()){
				if(!skipped){skipped=true;continue;}
				if(bp.transform.name.Contains(s)){
					Prefab.Load(bp.transform.name);
					Console.ForegroundColor = ConsoleColor.Cyan;
					System.Console.WriteLine("Scanning monument :"+bp.transform.name+" at "+bp.transform.position);
					result.AddRange(recursiveTransform(1,bp.transform));
					Console.ForegroundColor = ConsoleColor.Green;
					Console.BackgroundColor = ConsoleColor.Green;
					System.Console.WriteLine("!");
					Console.ResetColor();
				}
			}
			string json = JsonConvert.SerializeObject(result,Formatting.Indented, 
				new JsonSerializerSettings 
				{ 
						ReferenceLoopHandling = ReferenceLoopHandling.Ignore
				});
			System.IO.Directory.CreateDirectory("_MonumentHierarchies");
			System.IO.File.WriteAllText("_MonumentHierarchies\\"+s+".json", json);
		}
		
		public List<PrefabInfo> RecomposeMonument(string s){
			string json ="";
			try{
				json = System.IO.File.ReadAllText("_MonumentHierarchies\\"+s+".json");
			}
			catch(Exception e){
				Puts("Can't read monument extract");
				return new List<PrefabInfo>();
			}
			List<PrefabInfo> result = JsonConvert.DeserializeObject<List<PrefabInfo>>(json);
			Puts("Result loaded! First element:"+result[0].Name);
			List<ProtoBuf.PrefabData> prefabDatas = RecursiveSpawn(result);
			
			World.Serialization.world.prefabs = new System.Collections.Generic.List<global::ProtoBuf.PrefabData>();
			World.Serialization.world.prefabs.AddRange(prefabDatas);
			System.IO.Directory.CreateDirectory("_Broken");
			World.Serialization.Save("_Broken\\"+s+".map");
			
			return result;
		}
		
		public List<ProtoBuf.PrefabData> RecursiveSpawn(List<PrefabInfo> lpi){
			List<ProtoBuf.PrefabData> result = new List<ProtoBuf.PrefabData>();
			foreach(PrefabInfo pi in lpi){
				if(pi.nearestPrefab!=""){
					
					string prefabName = pi.nearestPrefab.Split(":")[0];
					uint prefabID = 0;
					StringPool.toNumber.TryGetValue(prefabName, out prefabID);
					
					ProtoBuf.PrefabData pfd = new ProtoBuf.PrefabData();
					pfd.category="BrokenMonument";
					pfd.id = prefabID;
					pfd.position = new ProtoBuf.VectorData(pi.Position.x,pi.Position.y,pi.Position.z);
					pfd.rotation = new ProtoBuf.VectorData(pi.Rotation.eulerAngles.x,pi.Rotation.eulerAngles.y,pi.Rotation.eulerAngles.z);
					pfd.scale = new ProtoBuf.VectorData(pi.Scale.x,pi.Scale.y,pi.Scale.z);
					result.Add(pfd);
				}
				if(pi.Children.Count>0){
					result.AddRange(RecursiveSpawn(pi.Children));
				}else{
					
				}
			}
			return result;
		}
				
		public void DemoMonumentSwap(string s = "assets/bundled/prefabs/autospawn/monument/medium/bandit_town.prefab"){
			
			uint compID = 0;
			StringPool.toNumber.TryGetValue("assets/bundled/prefabs/autospawn/monument/medium/bandit_town.prefab", out compID);
			Puts("Compound ID:"+compID.ToString());////
			ProtoBuf.PrefabData pfd = new ProtoBuf.PrefabData();
			pfd.category="moument";
			pfd.id = compID;
			pfd.position = new ProtoBuf.VectorData(0,0,0);
			pfd.rotation = new ProtoBuf.VectorData(0,0,0);
			pfd.scale = new ProtoBuf.VectorData(1,1,1);
			World.Serialization.world.prefabs = new System.Collections.Generic.List<global::ProtoBuf.PrefabData>();
			World.Serialization.world.prefabs.Add(pfd);
			System.IO.Directory.CreateDirectory("_Broken");
			World.Serialization.Save("_Broken\\demo.map");//
			
		}
		
		public void ExtractLayers(){	
			System.IO.Directory.CreateDirectory("_Layers");
			foreach(ProtoBuf.MapData md in World.Serialization.world.maps){
				try{
					if(md.name.Contains("\\") ||md.name.Contains("+")|md.name.Contains("=")){
						System.IO.File.WriteAllBytes(".\\_Layers\\"+BitConverter.ToString(Convert.FromBase64String(md.name))+".bin", md.data);
					}else{							
						System.IO.File.WriteAllBytes(".\\_Layers\\"+md.name+".bin", md.data);
					}
				}
				catch(Exception ex){
					System.IO.File.WriteAllBytes(md.name+".\\_Layers\\"+".bin", md.data);						
				}
			}
		}
	}
}