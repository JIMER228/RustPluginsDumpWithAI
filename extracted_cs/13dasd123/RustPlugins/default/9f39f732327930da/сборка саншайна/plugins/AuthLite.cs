/* AuthLite: Pure and simple method to play Rust and no more
	
	Donations: https://estantaya.itch.io/zen-kingdom/purchase
	
	Pls support me. I am hosting-developing a new mmorpg!
	
	Recommended server config:
	app.port -1 (hide from server list)
	server.secure true (cuz if we not use easyanticheat system steam players not joined the server)
	server.encryption 1 (protected from packet manipulation)
	Recommended plugin :
	AuthMe by ShadowRemove (password protection)

	Normal authentication for steam players and custom for non-steam
	You can disable the entry of non-steam players by removing or deactivating the plugin

	Config ( AuthLite/Config.json file ):
	bypass true: Treat all players as non steam players
	noadmins true: All non steam players have their authLevel set to 0 during connection

	Warning: This plugin can bypasses EasyAnticheat, Steam and Rust Global Ban!
*/
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using Network;
using Oxide.Core;
using Oxide.Core.Libraries;
using UnityEngine;
using System.Diagnostics;
using System.Reflection;
using System.Globalization;

namespace Oxide.Plugins
{
    [Info("AuthLite", "DeathGX & ShadowRemove", "0.8.3")]
    [Description("Automatic id authentication")]
    public class AuthLite : RustPlugin
    {
		Dictionary<ulong,User> users=new Dictionary<ulong,User>();
		Config config=new Config();
		//int seedSize=4;//4 hex values == 4 bytes == int size
		string[] hexNumber=new string[]{"0","1","2","3","4","5","6","7","8","9","A","B","C","D","E","F"};
		
		class User {
			public string name;
			public string names;
			//public ulong id;
			public string steam_status;
		}
		class Config {
			//public int seed;
			public bool bypass;
			public bool noadmins;
			public Config() {
				//seed=0;
				bypass=false;//i want a cookie
				noadmins=false;//prevents privileges override
			}
		}
		
        void OnServerInitialized()
        {
			//ConVar.App.port=-1;//server companion
            users = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, User>>("AuthLite/Non-Steam");
            config = Interface.Oxide.DataFileSystem.ReadObject<Config>("AuthLite/Config");
			//if (config.seed==0) config.seed=UnityEngine.Random.Range(0,int.MaxValue/4);
			UnityEngine.Debug.Log("[AuthLite] read "+users.Count+" users");
        }
		string GenerateHex(int size) {
			string result="";
			for (int i=0;i<size;i++) {
				result+=hexNumber[Core.Random.Range(0,hexNumber.Length)];
				result+=hexNumber[Core.Random.Range(0,hexNumber.Length)];
			}
			return result;
		}
		void Save() 
        {
			UnityEngine.Debug.Log("[AuthLite] write "+users.Count+" users");
			Interface.Oxide.DataFileSystem.WriteObject("AuthLite/Non-Steam", users);
			Interface.Oxide.DataFileSystem.WriteObject("AuthLite/Config", config);
        }
		object OnUserApprove(Connection conn) {
			
			global::ConnectionAuth.m_AuthConnection.Add(conn);
			
			ConnectionAuth auth=GameObject.FindObjectOfType<ConnectionAuth>();
			auth.StartCoroutine(AuthRoutine(conn,auth));
			
            return "Kaidoz is dead?";//if this value is not null breaks the steam auth
        }
		public static IEnumerator FakeAuth(Connection connection) {
			
			connection.authStatus = "";
			if (!PlatformService.Instance.BeginPlayerSession(connection.userid, connection.token))
			{
				connection.authStatus="Steam Auth Failed";
				yield break;
			}
			
			FieldInfo listField = typeof(Auth_Steam).GetField("waitingList", BindingFlags.Static | BindingFlags.NonPublic);

			//global::Auth_Steam.waitingList.Add(connection);
			//List<Connection> waitingList = new List<Connection>();
			List<Connection> waitingList = (List<Connection>)listField.GetValue(null);
			waitingList.Add(connection);
			Stopwatch timeout = Stopwatch.StartNew();
			while (timeout.Elapsed.TotalSeconds < 30.0 && connection.active && !(connection.authStatus != ""))
			{
				yield return null;
			}
			//global::Auth_Steam.waitingList.Remove(connection);
			waitingList.Remove(connection);
			
			yield break;
		}
		public IEnumerator AuthRoutine(Connection connection,ConnectionAuth auth)
		{
			
			yield return auth.StartCoroutine(FakeAuth(connection));
			
			if (!connection.active)
			{
				yield break;
			}
			
			if (auth.IsAuthed(connection.userid))
			{
				global::ConnectionAuth.Reject(connection, "Player instance sync error, contact an admin or wait!", null);
				yield break;
			}
			
			if (connection.authStatus != "ok"||config.bypass) {
				
				Puts(connection.username+" ["+connection.userid+"] Auth status: "+connection.authStatus);
				
				//ulong newId=connection.userid+Convert.ToUInt64(config.seed);
				
				if (users.ContainsKey(connection.userid)) {
					//login
					UnityEngine.Debug.Log("[AuthLite] "+ users[connection.userid].name +" Login");
					
					if (!users[connection.userid].names.Contains(connection.username)) {
						users[connection.userid].names+=", "+connection.username;
						connection.username = users[connection.userid].name;
					}
				
				} else {
					//register
					UnityEngine.Debug.Log("[AuthLite] "+connection.username+" Registered");
					users[connection.userid]=new User();

					users[connection.userid].name=connection.username;
					users[connection.userid].names=connection.username;
					//users[newId].real_id=connection.userid;
					//connection.guid
				}
				users[connection.userid].steam_status=connection.authStatus;
				Save();
				connection.authStatus = "ok";
				//connection.userid=newId;
				
				if (config.noadmins) connection.authLevel=0;
			}
			
			yield return null;
			
			//global::EACServer.connection2status[connection] = 2;
			//global::EACServer.connection2status[connection] = 5;
			
			MethodInfo authLocal = typeof(EACServer).GetMethod("OnAuthenticatedLocal", BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo authRemote = typeof(EACServer).GetMethod("OnAuthenticatedRemote", BindingFlags.Static | BindingFlags.NonPublic);
			
			authLocal.Invoke(null, new object[]
			{
				connection
			});
			authRemote.Invoke(null, new object[]
			{
				connection
			});
			
			string text = ConVar.Server.censorplayerlist ? GenerateHex(4) : connection.username;
			PlatformService.Instance.UpdatePlayerSession(connection.userid, text);
			
			yield return null;
			//approve
			auth.Approve(connection);
			yield break;
		}
    }
}