using System.Net;
using System.Threading;
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
using Oxide.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Oxide.Plugins
{
	[Info("FactionCommand", "obsol", "0.0.1")]
	[Description("Server debugging tool for modders")]
	public class FactionCommand : RustPlugin
	{		
	/*
	
	https://docs.microsoft.com/en-us/dotnet/api/system.net.httplistener?view=net-6.0
	https://dotnettutorials.net/lesson/multithreading-in-csharp/
	https://www.base64decode.org/
	
	*/
		
		static Thread t1;
		static HttpListener listener = new HttpListener();
		static bool keepLooping = true;
		private static Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
		private static void SendChatMsg(BasePlayer pl, string msg) =>
		_rustPlayer.Message(pl, msg,  "<color=#00ff00>[FactionCommand]</color>", 0, Array.Empty<object>());
		static byte[,,] cellGrid;
		static List<OrderCell> orders = new List<OrderCell>();
		static string lastResult = "";
		
		
		public class OrderCell{
			public int faction;
			public Vector3Int position;
			public int targetCount;
			public float timeOut;
		}
		void OnServerInitialized(bool initial)
		{
			cellGrid = (byte[,,])Interface.CallHook("getGrid");
			timer.Every(1f, () =>
			{
				while(orders.Count>0){
					OrderCell oc = orders.ToArray()[0];
					UnityEngine.Debug.Log("Got Orders" + oc.position.ToString());
					orders.Remove(oc);
					Interface.CallHook("addOrderExternal",(oc.faction==0?BaseCombatEntity.Faction.Bandit:BaseCombatEntity.Faction.Scientist),oc.position,oc.targetCount,oc.timeOut,false);
				}
				cellGrid = (byte[,,])Interface.CallHook("getGrid");
			});
			ConsoleSystem.Command result;
			if (ConsoleSystem.Index.Server.Dict.TryGetValue("server.seed", out result))
			{
				ConVar.Server.seed = 776;//(UnityEngine.Random.Range(0f,1f)>0.5f?776:(UnityEngine.Random.Range(0f,1f)>0.5f?2700:63594));
				Puts(((char)27)+"[94m"+"Picked seed:"+ConVar.Server.seed.ToString());
				result.Saved=true;
				string contents = ConsoleSystem.SaveToConfigString(true);
				System.IO.File.WriteAllText(ConVar.Server.GetServerFolder("cfg") + "/serverauto.cfg", contents);
				global::ServerUsers.Save();
			}
		}
		void Loaded(){
			serverLoop();
			ConsoleSystem.Command result;
			if (ConsoleSystem.Index.Server.Dict.TryGetValue("server.seed", out result))
			{
				ConVar.Server.seed = 776;//(UnityEngine.Random.Range(0f,1f)>0.5f?776:(UnityEngine.Random.Range(0f,1f)>0.5f?2700:63594));
				Puts(((char)27)+"[94m"+"Picked seed:"+ConVar.Server.seed.ToString());
				result.Saved=true;
				string contents = ConsoleSystem.SaveToConfigString(true);
				System.IO.File.WriteAllText(ConVar.Server.GetServerFolder("cfg") + "/serverauto.cfg", contents);
				global::ServerUsers.Save();
			}
			
		}
		void Unload()
		{
			t1.Abort();
			listener.Stop();
			ConsoleSystem.Command result;
			if (ConsoleSystem.Index.Server.Dict.TryGetValue("server.seed", out result))
			{
				ConVar.Server.seed = 776;//(UnityEngine.Random.Range(0f,1f)>0.5f?776:(UnityEngine.Random.Range(0f,1f)>0.5f?2700:63594));
				Puts(((char)27)+"[94m"+"Picked seed:"+ConVar.Server.seed.ToString());
				result.Saved=true;
				string contents = ConsoleSystem.SaveToConfigString(true);
				System.IO.File.WriteAllText(ConVar.Server.GetServerFolder("cfg") + "/serverauto.cfg", contents);
				global::ServerUsers.Save();
			}
		}
		struct GridCell{
			
			public bool isWater;
			public bool isMonument;
			public bool isShop;
			public bool isAirdrop;
			
			public bool isScientist;
			public bool isBandit;
			public bool isSciAggro;
			public bool isBanAggro;
			
			public byte sciCount;
			public byte banCount;
			
			public byte sciTarget;
			public byte banTarget;
			
			public ushort sciTimeout;
			public ushort banTimeout;
			
			public void setFlags(byte flags){
				isWater = (flags & (byte)0x80)==(byte)0x80;
				isMonument = (flags & (byte)0x40)==(byte)0x40;
				isShop = (flags & (byte)0x20)==(byte)0x20;
				isAirdrop = (flags & (byte)0x10)==(byte)0x10;
				isScientist = (flags & (byte)8)==(byte)8;
				isBandit = (flags & (byte)4)==(byte)4;
				isSciAggro = (flags & (byte)2)==(byte)2;
				isBanAggro = (flags & (byte)1)==(byte)1;
			}
			public byte getFlags(){
				
				return (byte)((isWater?0x80:0)+(isMonument?0x40:0)+(isShop?0x20:0)+(isAirdrop?0x10:0)+(isScientist?8:0)+(isBandit?4:0)+(isSciAggro?2:0)+(isBanAggro?1:0));
			}
			public byte[] pack(){
				byte[] result = new byte[9];
				result[0]=getFlags();
				result[1]=sciCount;
				result[2]=banCount;
				result[3]=sciTarget;
				result[4]=banTarget;
				byte[] i =BitConverter.GetBytes(sciTimeout);
				result[5]=i[0];
				result[6]=i[1];
				i = BitConverter.GetBytes(banTimeout);
				result[7]=i[0];
				result[8]=i[1];
				return result;
			}
			public void unpack(byte[] result){
				setFlags(result[0]);
				sciCount=result[1];
				banCount=result[2];
				sciTarget=result[3];
				banTarget=result[4];
				byte[] shortRes = new byte[2];
				shortRes[0]= result[5];
				shortRes[1]= result[6];
				sciTimeout = BitConverter.ToUInt16(result, 5);
				banTimeout = BitConverter.ToUInt16(result, 7);
			}
			
		}
		bool? CanUseGesture(BasePlayer bp, GestureConfig gc){
			
			Puts("Checking gesture: "+gc.gestureName.translated +" : " + gc.gestureId.ToString());
			return null;
		}
		static void serverLoop(){
			
			UnityEngine.Debug.Log(HttpListener.IsSupported.ToString());
            t1 = new Thread(SimpleListenerExample){
                Name = "ServerThread"
            };
			t1.Start();
		}
		
		static string getMapString(){
			string s = "\n";
			s += "==============================================================================================";
			s += "\n";
			if(cellGrid==null){
				return "SATTELITE LINK LOST";
			}
			for(int i = cellGrid.GetLength(0)-1; i> 0;i--){
				string s1 = "";
				string s2= "";
				string s3="";
				for(int j = 0; j< cellGrid.GetLength(1);j++){
					GridCell gc = new GridCell();
					byte[] cell = new byte[9];
					for(int k = 0;k<9;k++){
						cell[k]=cellGrid[j,i,k];
					}
					gc.unpack(cell);
					
					if(gc.isWater){
					  s1+=(gc.isMonument||gc.isShop||gc.isBanAggro?"╭"+(gc.isMonument?"@":" ")+(gc.isShop?"$":" ")+(gc.isBanAggro?"#":" ")+"╮":"     ");
					  s2+=(gc.isMonument||gc.isShop||gc.isBanAggro?"│"+(gc.banCount>0?gc.banCount.ToString("000"):"   ")+"│":" "+(gc.banCount>0?gc.banCount.ToString("000"):"   ")+" ");
					  s3+=(gc.isMonument||gc.isShop||gc.isBanAggro?"╰"+(gc.banTarget>0?gc.banTarget.ToString("000"):"   ")+"╯":" "+(gc.banTarget>0?gc.banTarget.ToString("000"):"   ")+" ");
						
					}else{
					  s1+="┏"+(gc.isMonument?"@":" ")+(gc.isShop?"$":" ")+(gc.isBanAggro?"#":" ")+"┓";
					  s2+="┣"+(gc.banCount>0?gc.banCount.ToString("000"):"   ")+"┨";
					  s3+="┗"+(gc.banTarget>0?gc.banTarget.ToString("000"):"   ")+"┛";
					}
				}	
				s+=s1+"\n"+s2+"\n"+s3+"\n";
			} 
			s += "\n";
			s += "==============================================================================================";
			s += "\n";
			for(int i = cellGrid.GetLength(0)-1; i> 0;i--){
				string s1 = "";
				string s2= "";
				string s3="";
				for(int j = 0; j< cellGrid.GetLength(1);j++){
					GridCell gc = new GridCell();
					byte[] cell = new byte[9];
					for(int k = 0;k<9;k++){
						cell[k]=cellGrid[j,i,k];
					}
					gc.unpack(cell);
					
					if(gc.isWater){
					  s1+=(gc.isMonument||gc.isShop||gc.isSciAggro?"╭"+(gc.isMonument?"@":" ")+(gc.isShop?"$":" ")+(gc.isSciAggro?"#":" ")+"╮":"     ");
					  s2+=(gc.isMonument||gc.isShop||gc.isSciAggro?"│"+(gc.sciCount>0?gc.sciCount.ToString("000"):"   ")+"│":" "+(gc.sciCount>0?gc.sciCount.ToString("000"):"   ")+" ");
					  s3+=(gc.isMonument||gc.isShop||gc.isSciAggro?"╰"+(gc.sciTarget>0?gc.sciTarget.ToString("000"):"   ")+"╯":" "+(gc.sciTarget>0?gc.sciTarget.ToString("000"):"   ")+" ");
						
					}else{
					  s1+="┏"+(gc.isMonument?"@":" ")+(gc.isShop?"$":" ")+(gc.isSciAggro?"#":" ")+"┓";
					  s2+="┣"+(gc.sciCount>0?gc.sciCount.ToString("000"):"   ")+"┨";
					  s3+="┗"+(gc.sciTarget>0?gc.sciTarget.ToString("000"):"   ")+"┛";
					}
				}	
				s+=s1+"\n"+s2+"\n"+s3+"\n";
			}
			s += "\n";
			s += "==============================================================================================";
			return s;
		}
		public static void SimpleListenerExample()
		{
			if (!HttpListener.IsSupported)
			{
				Console.WriteLine ("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
				return;
			}
			listener.Prefixes.Add("http://*:25947/");
			listener.Start();
			while(keepLooping){
				Console.WriteLine("Listening...");
				// Note: The GetContext method blocks while waiting for a request.
				HttpListenerContext context = listener.GetContext();
				HttpListenerRequest request = context.Request;
				// Obtain a response object.
				HttpListenerResponse response = context.Response;
				response.AppendHeader("Access-Control-Allow-Origin","*");
				response.AppendHeader("Access-Control-Allow-Headers","*");
				response.AppendHeader("Content-Type","text/json; charset=utf-8");
				string responseString = "";
				if(request.RawUrl.ToString().ToLower().Contains("/order")){
					System.IO.Stream body = request.InputStream;
					System.Text.Encoding encoding = request.ContentEncoding;
					System.IO.StreamReader reader = new System.IO.StreamReader(body, encoding);
					string s = reader.ReadToEnd();
					var stuff = JObject.Parse(s);
					string position = (string)((JToken)stuff)["position"];
					int amount = (int)((JToken)stuff)["amount"];
					int timeout = (int)((JToken)stuff)["timeout"];
					int faction = (int)((JToken)stuff)["faction"];
					lastResult=s;
					if(position!=null){
						string[] positionSplit = position.Replace(" ","").Split(',');
						lastResult=position;
						if(positionSplit.Length==3){
							//lastResult=positionSplit[0];
							int x = 0;
							int y = 0;
							int z = 0;
							int.TryParse(positionSplit[0], out x);
							int.TryParse(positionSplit[1], out y);
							int.TryParse(positionSplit[2], out z);
							Console.WriteLine(x.ToString()+":"+y.ToString()+":"+z.ToString());
							OrderCell oc = new OrderCell();
							oc.position = new Vector3Int(x,y,z);
							lastResult=oc.position.ToString();
							oc.timeOut = timeout;
							oc.targetCount = amount;
							oc.faction = faction;
							orders.Add(oc);
							
						}
						else{
							responseString="Bad vector";
						}
					}
					else{
						responseString="No vector";
					}
				}
				else{
					var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
					var text = JsonConvert.SerializeObject(cellGrid, settings);
					// Construct a response.
					
					responseString = (text==null?"":(string)text);
					
				}
				byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
				// Get a response stream and write the response to it.
				response.ContentLength64 = buffer.Length;
				System.IO.Stream output = response.OutputStream;
				output.Write(buffer,0,buffer.Length);
				// You must close the output stream.
				output.Close();
			}
			listener.Stop();
		}
		
        [ConsoleCommand("fact.last")]
		private void GetLastServerCommand(ConsoleSystem.Arg args){
			Puts(lastResult);
		}
		
		
		[ConsoleCommand("fact.stopServer")] void stopServer(IPlayer player, string command, string[] args){
			keepLooping=false;
		}
		[Command("order")] void setOrderToPlayerPos(IPlayer player, string command, string[] args){
			
			BasePlayer bp = (BasePlayer)player.Object;
			BaseCombatEntity.Faction bce = bp.faction;
			 int number = 5;

			 bool success = (args[0]!=null?int.TryParse(args[0], out number):false);
			 if (!success)
			 {
				number = 5;
			 }
			 int timeout = 120;

			 success |= (args[1]!=null?int.TryParse(args[1], out timeout):false);
			 if (!success)
			 {
				timeout = 120;
			 }
			Interface.CallHook("addOrderExternal",bce,bp.transform.position,number,timeout,false);
			
		}
		
		void OnCorpsePopulate(HumanNPC hn, NPCPlayerCorpse co){
			//keepLooping=false;
			if(hn.faction == BaseCombatEntity.Faction.Bandit || hn.faction == BaseCombatEntity.Faction.Scientist){
				int amount = UnityEngine.Random.Range(1,4);
				int timeout = UnityEngine.Random.Range(20,60);
				Puts("Calling Reinforcements: "+amount.ToString()+" for "+timeout.ToString()+" at "+co.transform.position.ToString());
				Interface.CallHook("addOrderExternal",hn.faction,co.transform.position,amount,timeout,true);
			}
			
		}
		
        [ConsoleCommand("fact.set")]
		private void SetOrderServerCommand(ConsoleSystem.Arg args){
			 int number = 5;
			string[] arg = args.Args;
			Puts(arg[0]);
			 bool success = (arg[0]!=null?int.TryParse(arg[0], out number):false);
			 if (!success)
			 {
				number = 5;
			 }
			 int timeout = 120;

			 success |= (arg[1]!=null?int.TryParse(arg[1], out timeout):false);
			 if (!success)
			 {
				timeout = 120;
			 }
			 int x = 0;
			 int y = 30;
			 int z = 0;
			 success |= (arg[2]!=null?int.TryParse(arg[2], out x):false);
			 if (!success)
			 {
				x = 0;
			 }
			 success |= (arg[3]!=null?int.TryParse(arg[3], out y):false);
			 if (!success)
			 {
				y = 30;
			 }
			 success |= (arg[4]!=null?int.TryParse(arg[4], out z):false);
			 if (!success)
			 {
				z = 0;
			 }
			
			Interface.CallHook("addOrderExternal",BaseCombatEntity.Faction.Bandit,new Vector3(x,y,z),number,timeout,false);
		}
        [ConsoleCommand("fact.balance")]
		private void FetchBalanceServerCommand(ConsoleSystem.Arg arg){
			string s = "";
			float x = (float)Interface.CallHook("getBalance",BaseCombatEntity.Faction.Bandit);
			float y = (float)Interface.CallHook("getBalance",BaseCombatEntity.Faction.Scientist);
			float z = (float)Interface.CallHook("getBalance",BaseCombatEntity.Faction.Player);
			s+= "Bandit: "+x.ToString()+"\n";
			s+= "Scientist: "+y.ToString()+"\n";
			s+= "Scientist: "+z.ToString()+"\n";
			Puts(s);
			
			
		}
		
		
        [ConsoleCommand("fact.fetch")]
		private void FetchGridServerCommand(ConsoleSystem.Arg arg){
			Puts("Called");
			cellGrid = (byte[,,])Interface.CallHook("getGrid");
			
			string s = "\n";
			s += "==============================================================================================";
			s += "\n";
			if(cellGrid==null){
				Puts("Oops, empty grid");
				return;
			}
			for(int i = cellGrid.GetLength(0)-1; i> 0;i--){
				string s1 = "";
				string s2= "";
				string s3="";
				for(int j = 0; j< cellGrid.GetLength(1);j++){
					GridCell gc = new GridCell();
					byte[] cell = new byte[9];
					for(int k = 0;k<9;k++){
						cell[k]=cellGrid[j,i,k];
					}
					gc.unpack(cell);
					
					if(gc.isWater){
					  s1+=(gc.isMonument||gc.isShop||gc.isBanAggro?"╭"+(gc.isMonument?"@":" ")+(gc.isShop?"$":" ")+(gc.isBanAggro?"#":" ")+"╮":"     ");
					  s2+=(gc.isMonument||gc.isShop||gc.isBanAggro?"│"+(gc.banCount>0?gc.banCount.ToString("000"):"   ")+"│":" "+(gc.banCount>0?gc.banCount.ToString("000"):"   ")+" ");
					  s3+=(gc.isMonument||gc.isShop||gc.isBanAggro?"╰"+(gc.banTarget>0?gc.banTarget.ToString("000"):"   ")+"╯":" "+(gc.banTarget>0?gc.banTarget.ToString("000"):"   ")+" ");
						
					}else{
					  s1+="┏"+(gc.isMonument?"@":" ")+(gc.isShop?"$":" ")+(gc.isBanAggro?"#":" ")+"┓";
					  s2+="┣"+(gc.banCount>0?gc.banCount.ToString("000"):"   ")+"┨";
					  s3+="┗"+(gc.banTarget>0?gc.banTarget.ToString("000"):"   ")+"┛";
					}
				}	
				s+=s1+"\n"+s2+"\n"+s3+"\n";
			} 
			s += "\n";
			s += "==============================================================================================";
			s += "\n";
			for(int i = cellGrid.GetLength(0)-1; i> 0;i--){
				string s1 = "";
				string s2= "";
				string s3="";
				for(int j = 0; j< cellGrid.GetLength(1);j++){
					GridCell gc = new GridCell();
					byte[] cell = new byte[9];
					for(int k = 0;k<9;k++){
						cell[k]=cellGrid[j,i,k];
					}
					gc.unpack(cell);
					
					if(gc.isWater){
					  s1+=(gc.isMonument||gc.isShop||gc.isSciAggro?"╭"+(gc.isMonument?"@":" ")+(gc.isShop?"$":" ")+(gc.isSciAggro?"#":" ")+"╮":"     ");
					  s2+=(gc.isMonument||gc.isShop||gc.isSciAggro?"│"+(gc.sciCount>0?gc.sciCount.ToString("000"):"   ")+"│":" "+(gc.sciCount>0?gc.sciCount.ToString("000"):"   ")+" ");
					  s3+=(gc.isMonument||gc.isShop||gc.isSciAggro?"╰"+(gc.sciTarget>0?gc.sciTarget.ToString("000"):"   ")+"╯":" "+(gc.sciTarget>0?gc.sciTarget.ToString("000"):"   ")+" ");
						
					}else{
					  s1+="┏"+(gc.isMonument?"@":" ")+(gc.isShop?"$":" ")+(gc.isSciAggro?"#":" ")+"┓";
					  s2+="┣"+(gc.sciCount>0?gc.sciCount.ToString("000"):"   ")+"┨";
					  s3+="┗"+(gc.sciTarget>0?gc.sciTarget.ToString("000"):"   ")+"┛";
					}
				}	
				s+=s1+"\n"+s2+"\n"+s3+"\n";
			}
			s += "\n";
			s += "==============================================================================================";
			Puts("finished");
			Puts(s);
			
		}
		
		object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info){
				if(info.HitEntity==null||info.Initiator==null) return null;
				if(info.HitEntity.gameObject==null||info.Initiator.gameObject==null) return null;
				if (info.damageTypes.Has(Rust.DamageType.Heat) ){	
					if(info.HitEntity is SkyLantern){
						ZoneLantern(info.HitEntity as SkyLantern, info);
					}	
				}
				return null;
			}
		
		void ZoneLantern(SkyLantern sl, HitInfo hitInfo){
			Item note = sl.inventory.FindItemsByItemName("note");
			if(!(note!=null && (hitInfo.Initiator is BasePlayer) && note.text !=null)){
				return;
			}
			
			string[] lines = note.text.ToLower().Split('\n');
			if(lines[0].Replace("\r","").Replace("!","").Replace(" ","").ToLower()=="help"){
				Dictionary<string,string> AIZSettings = new Dictionary<string,string>();
				if(lines!=null&&lines.Count()>1){
					foreach(string line in lines){
						string[] entries = line.Replace("\r","").Split(':');
						if(entries != null && entries.Count()==2)
						{
							if(AIZSettings.ContainsKey(entries[0])){
								AIZSettings[entries[0]]=entries[1];
							}else{
								AIZSettings.Add(entries[0],entries[1]);
							}
						}else if(entries.Count()==1 && entries[0]!="zone"){
							if(AIZSettings.ContainsKey(entries[0])){
								AIZSettings[entries[0]]="";
							}else{
								AIZSettings.Add(entries[0],"");
							}

						}
					}
				}else{
					string[] entries = note.text.ToLower().Split(':');
					if(entries!=null){
						if(entries.Count()==2)
						{
							if(AIZSettings.ContainsKey(entries[0])){
								AIZSettings[entries[0]]=entries[1];
							}else{
								AIZSettings.Add(entries[0],entries[1]);
							}
						}else if(entries.Count()==1 && entries[0]!="zone"){
							if(AIZSettings.ContainsKey(entries[0])){
								AIZSettings[entries[0]]="";
							}else{
								AIZSettings.Add(entries[0],"");
							}

						}
					}
				}
				BaseCombatEntity.Faction faction = ((BaseCombatEntity)hitInfo.Initiator).faction;
				int amount = 5;
				int timeout = 300;
				if(AIZSettings.ContainsKey("send")){int.TryParse(AIZSettings["send"],out amount);}
				if(AIZSettings.ContainsKey("time")){int.TryParse(AIZSettings["time"],out timeout);}
				if(hitInfo.Initiator is BasePlayer) SendChatMsg((hitInfo.Initiator as BasePlayer),"Called "+amount.ToString()+" to "+((int)(hitInfo.Initiator.transform.position.x/100)).ToString()+" "+((int)(hitInfo.Initiator.transform.position.y/100)).ToString()+" for "+timeout.ToString()+" seconds");
				
				Interface.CallHook("addOrderExternal",faction,hitInfo.Initiator.transform.position,amount,timeout,false);
			}
		}
	}
	
}