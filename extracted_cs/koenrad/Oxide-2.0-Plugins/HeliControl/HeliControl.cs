using Oxide.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("HeliControl", "Koenrad", "1.0.7",  ResourceId = 1348)]
    class HeliControl : RustPlugin
    {			
		private string[] weaponList = {"rifle.bolt", "pistol.semiauto", "pistol.revolver", "pistol.eoka", "rifle.ak", "smg.2", "smg.thompson" };
		private Dictionary<string,string> englishnameToShortname = new Dictionary<string, string>();		//for finding shortnames
		StoredData storedData = new StoredData();
		private int last;
		/*--------------------------------------------------------------//
		//			Load up the default config on first use				//
		//--------------------------------------------------------------*/
		protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            Config.Clear();
            Config["DisableHeli"] = false;
            Config["ModifyDamageToHeli"] = true;
            Config["UseGlobalDamageModifier"] = true;
			Config["UsePermissions"] = false;
			Config["UseCustomLoot"] = true;
			Config["GlobalDamageMultiplier"] = 4.0;
			Config["HeliBulletDamageAmount"] = 20.0;
			Config["MainRotorHealth"] = 300.0;
			Config["TailRotorHealth"] = 150.0;
			Config["MainHealth"] = 10000.0;
			Config["MaxLootCratesToDrop"] = 4;
			Config["HeliSpeed"] = 25.0;
			Config["HeliAccuracy"] = 2.0;
			Config["rifle.bolt"] = 8.0;
			Config["pistol.semiauto"] = 5.0;
			Config["pistol.revolver"] = 5.0;
			Config["pistol.eoka"] = 5.0;
			Config["rifle.ak"] = 5.0;
			Config["smg.2"] = 5.0;
			Config["smg.thompson"] = 5.0;
            SaveConfig();
        }
		/*--------------------------------------------------------------//
		//						Initial Setup							//
		//--------------------------------------------------------------*/
		void Init()
        {
			//Backwards compatibility for config files:
            if (Config["UsePermissions"] == null)	//This config entry was added 1.0.3
			{
				Config["UsePermissions"] = false;
				SaveConfig();
			}
			if (Config["HeliSpeed"] == null)	//This config entry was added 1.0.4
			{
				Config["HeliSpeed"] = 25.0; 
				SaveConfig();
			}
			if (Config["HeliAccuracy"] == null)	//This config entry was added 1.0.4
			{
				Config["HeliAccuracy"] = 2.0; 
				SaveConfig();
			}
			
			if (Config["UseCustomLoot"] == null)	//This config entry was added 1.0.7
			{
				Config["UseCustomLoot"] = false; 
				SaveConfig();
			}
			
			if (Config["MainHealth"] == null)		//This config entry was added 1.0.7
			{
				Config["MainHealth"] = 10000; 
				SaveConfig();
			}
			
			if (Config["MainRotorHealth"] == null)	//This config entry was added 1.0.7
			{
				Config["MainRotorHealth"] = 300; 
				SaveConfig();
			}
			
			if (Config["TailRotorHealth"] == null)	//This config entry was added 1.0.7
			{
				Config["TailRotorHealth"] = 150; 
				SaveConfig();
			}
			
			if ((bool)Config["UsePermissions"])	//if userpermissions in config is true register new permission
			{
				if (!permission.PermissionExists("CanCallHeli"))
				{
					permission.RegisterPermission("CanCallHeli", this);
					Puts("CanCallHeli permission registered");
					Puts("Only users with CanCallHeli permission can use the callheli command");
				}
			}
			else	//not using permissions system
			{
				Puts("Not using permissions system,");
				Puts("Admins can use /callheli");
			}
			
			//Set heli accuracy
			heli.bulletAccuracy = float.Parse(Config["HeliAccuracy"].ToString());

			
			
        }




	/*----------------------------------------------------------------------------------------------------------------------------//
	//													HOOKS																	  //
	//----------------------------------------------------------------------------------------------------------------------------*/
				
		/*--------------------------------------------------------------//
		//					OnServerInitialized Hook					//
		//--------------------------------------------------------------*/
		void OnServerInitialized()
        {
            //Initialize the list of english to shortnames
			englishnameToShortname = new Dictionary<string, string>();
			List<ItemDefinition> ItemsDefinition = ItemManager.GetItemDefinitions();
            foreach (ItemDefinition itemdef in ItemsDefinition)
			{
				englishnameToShortname.Add (itemdef.displayName.english.ToString().ToLower(),itemdef.shortname.ToString());
            }
			
			//Get the saved drop list
			if ((bool)Config["UseCustomLoot"]) LoadSavedData();
        }
		
		/*--------------------------------------------------------------//
		//					OnEntitySpawned Hook						//
		//--------------------------------------------------------------*/
		void OnEntitySpawned(BaseNetworkable entity)
		{
			
			if (entity == null) return;
			if (entity.LookupShortPrefabName() == "heli_crate.prefab") 
			{
				/*-----------------------------------------------------------------------------//
				//Prefab Properties of heli_crate:											   //
				//string guid = c175b252164d05d42ade2fb672b97331							   //
				//uint hash = 2225886856													   //
				//string name = assets/bundled/prefabs/npc/patrol_helicopter/heli_crate.prefab //
				//-----------------------------------------------------------------------------*/
				//Puts("HELI CRATE SPAWNED MOTHER FUCKERRR");

				//-----------------------------------------------------------------------------//
				if ((bool)Config["UseCustomLoot"])
				{
					LootContainer heli_crate = (LootContainer)entity;
					int index;
					//var random;
					do
					{
					var random = new System.Random();
					index = random.Next(storedData.HeliInventoryLists.Count);
					} while(index == last && storedData.HeliInventoryLists.Count > 1);
					last = index;
					//Puts("Index: " + index.ToString());
					BoxInventory inv = storedData.HeliInventoryLists[index];
					heli_crate.inventory.itemList.Clear();
					foreach ( ItemDef itemDef in inv.lootBoxContents)
					{
						Item item = ItemManager.CreateByName(itemDef.name, itemDef.amount);
						item.MoveToContainer(heli_crate.inventory, -1, false);
					}
					heli_crate.inventory.MarkDirty();
				}
				//---------------------------------------------------------------------------*/
				
				/*----------------Fill up the dat file with default drop---------------------//
				//-----------------------used for development--------------------------------//
				try
				{
					LootContainer heli_crate = (LootContainer)entity;
					//Puts(heli_crate.inventory.itemList);
					List<Item> itemlist = heli_crate.inventory.itemList;
					//Puts("itemlist: " + itemlist.Count.ToString());
					BoxInventory temp = new BoxInventory();
					//Puts("started from the bottom now we're here");
					foreach (var item in itemlist)
					{
						ItemDef temp2 = new ItemDef(item.info.shortname, item.amount);
						//Puts("yep");
						temp.lootBoxContents.Add(temp2);
						//Puts("heh");
					}
					foreach (ItemDef itemdef in temp.lootBoxContents)
					{
						Puts("item: " + itemdef.name + " " + itemdef.amount);
					}

					storedData.HeliInventoryLists.Add(temp);
					//Puts(storedData.ToString());
				} 
				catch(NullReferenceException)
				{
					Puts("fuck me it's null again");
				}
				SaveData();
				//-------------------------------------------------------------------------*/
			}

			//994850627 is the prefabID of a heli.
			if (entity.prefabID == 994850627)
			{
				// Disable Helicopters
				if ((bool)Config["DisableHeli"])
				{
					entity.KillMessage();
					Puts("Helicopter destroyed!");
					return;
				}
				BaseHelicopter heli = (BaseHelicopter)entity;
				heli.maxCratesToSpawn = (int)Config["MaxLootCratesToDrop"];
				heli.bulletDamage = float.Parse(Config["HeliBulletDamageAmount"].ToString());
			}
		}
		
		/*--------------------------------------------------------------//
		//						OnPlayerAttack Hook						//
		//--------------------------------------------------------------*/
		void OnPlayerAttack(BasePlayer attacker, HitInfo hitInfo)
		{
			//994850627 is the prefabID of a Heli.
			if (attacker == null || hitInfo == null || hitInfo.HitEntity == null) return;
			if (hitInfo.HitEntity.prefabID == 994850627 )			//We hit a helicopter
			{
				if (!(bool)Config["ModifyDamageToHeli"]) return;	//Check if damage modification is on
				if ((bool)Config["UseGlobalDamageModifier"])		//Check for global modifier
				{ 
					hitInfo.damageTypes.ScaleAll(float.Parse(Config["GlobalDamageMultiplier"].ToString()));
					return;
				}
				
				string weaponName = hitInfo.Weapon.GetItem().info.shortname;	//weapon's shortname
				if (weaponList.Contains(weaponName))
				{
					hitInfo.damageTypes.ScaleAll(float.Parse(Config[weaponName].ToString()));
				}
				else
				{
					hitInfo.damageTypes.ScaleAll(float.Parse(Config["GlobalDamageMultiplier"].ToString()));
				}
			}
		}

	/*----------------------------------------------------------------------------------------------------------------------------//
	//												CHAT COMMANDS																  //
	//----------------------------------------------------------------------------------------------------------------------------*/
		/*--------------------------------------------------------------//
		//					Chat Command for callheli					//
		//--------------------------------------------------------------*/
		[ChatCommand("callheli")] 
        private void cmdCallToPlayer(BasePlayer player, string command, string[] args)
		{
			if (!canExecute(player)) return;
			if (args.Length == 0) 
			{
				call();
				SendReply(player, "Helicopter called.");
				return;
			}
			/*
			if (args.Length != 1) 
			{
				SendReply(player, "You need to specify a player name to call the helicopter to");
				return;
			}
			//*/
			BasePlayer target = FindPlayerByPartialName(args[0]);
			if (target == null) 
			{
				SendReply(player, "Could not find the specified player \"" + args[0] + "\".");
				return;
			}
			int num = 1;
			if (args.Length == 2) 
			{
				bool result = Int32.TryParse(args[1], out num);
				if (!result)
				{
					num = 1;
				}
				
			}
			
			callOther(target, num);
			SendReply(player, num.ToString() + " helicopter(s) called on " + target.displayName);
		}
		
		/*--------------------------------------------------------------//
		//					Chat Command for killheli					//
		//--------------------------------------------------------------*/
		[ChatCommand("killheli")] 
        private void cmdKillHeli(BasePlayer player, string command, string[] args)
		{
			if (!canExecute(player)) return;
			int numKilled = killAll();
			SendReply(player, numKilled.ToString() + " helicopter(s) were annihilated!");
		}
		
		/*--------------------------------------------------------------//
		//				Chat Command for getshortname					//
		//--------------------------------------------------------------*/
		[ChatCommand("getshortname")] 
        private void cmdGetShortName(BasePlayer player, string command, string[] args)
		{
			if (!canExecute(player)) return;
			
			if (args.Length == 0) 
			{
				SendReply(player, "Invalid argument");
				return;
			}
			string engName = "";// = args[0];
			if (args.Length > 1)
			{
				foreach (string arg in args)
				{
					engName = engName + arg + " ";
				}
				engName = engName.Substring(0, engName.Length - 1);
			}
			else
			{
				engName = args[0];
			}
			
			if (englishnameToShortname.ContainsKey(engName)) 
			{
				SendReply(player, "The shortname for \"" + engName + "\" is \"" + englishnameToShortname[engName] + "\"");
			}
			else
			{
				SendReply(player, "Item not found");	
			}
		}
		
		
		
	/*----------------------------------------------------------------------------------------------------------------------------//
	//													CONSOLE COMMANDS														  //
	//----------------------------------------------------------------------------------------------------------------------------*/
		
		/*--------------------------------------------------------------//
		//				Console Command for callheli					//
		//--------------------------------------------------------------*/
		[ConsoleCommand("callheli")]
        private void consoleCallHeli(ConsoleSystem.Arg arg)
        {
			if (arg.Args == null || arg.Args.Length == 0 )
			{
				call();
				Puts("Helicopter called!");
				return;
			}
			/*if (arg.Args.Length != 1) 
			{
				Puts("You need to specify a player name to call the helicopter to");
				return;
			}*/
			BasePlayer target = FindPlayerByPartialName(arg.Args[0]);
			if (target == null) 
			{
				Puts("Could not find the specified player \"" + arg.Args[0] + "\".");
				return;
			}
			int num = 1;
			if (arg.Args.Length == 2) 
			{
				bool result = Int32.TryParse(arg.Args[1], out num);
				if (!result)
				{
					num = 1;
				}
				
			}
			
			callOther(target, num);
			Puts("Helicopter called on " + target.displayName);
        }
		
		/*--------------------------------------------------------------//
		//				Console Command for getshortname				//
		//--------------------------------------------------------------*/
		[ConsoleCommand("getshortname")]
        private void consoleGetShortName(ConsoleSystem.Arg arg)
        {
			if (arg.Args.Length == 0) 
			{
				Puts("Invalid argument");
				return;
			}
			string engName = "";// = args[0];
			if (arg.Args.Length > 1)
			{
				foreach (string str in arg.Args)
				{
					engName = engName + str + " ";
				}
				engName = engName.Substring(0, engName.Length - 1);
			}
			else
			{
				engName = arg.Args[0];
			}
			
			if (englishnameToShortname.ContainsKey(engName)) 
			{
				Puts("\"" + engName + "\" is \"" + englishnameToShortname[engName] + "\"");
			}
			else
			{
				Puts("Item not found");	
			}
		}
		
		/*--------------------------------------------------------------//
		//				Console Command for killheli					//
		//--------------------------------------------------------------*/
		[ConsoleCommand("killheli")]
        private void consoleKillHeli(ConsoleSystem.Arg arg)
        {
			int numKilled = killAll();
			Puts(numKilled.ToString() + " helicopter(s) were annihilated!");
		}
		
	/*----------------------------------------------------------------------------------------------------------------------------//
	//													CORE FUNCTIONS															  //
	//----------------------------------------------------------------------------------------------------------------------------*/
		
		/*--------------------------------------------------------------//
		//				killAll - produces no loot drops					//
		//--------------------------------------------------------------*/
		private int killAll()
		{
			int count = 0;
			BaseHelicopter[] allHelicopters = UnityEngine.Object.FindObjectsOfType<BaseHelicopter>();
			foreach (BaseHelicopter helicopter in allHelicopters) 
			{
				helicopter.maxCratesToSpawn = 0;		//comment this line if you want loot drops with killheli
				helicopter.DieInstantly();
				count++;
			}
			return count;
		}
		
		/*--------------------------------------------------------------//
		//			callOther - call heli on other person				//
		//--------------------------------------------------------------*/
		private void callOther(BasePlayer target, int num)
		{
			int i = 0;
			while (i < num)
			{
				BaseEntity entity = GameManager.server.CreateEntity("assets/bundled/prefabs/npc/patrol_helicopter/PatrolHelicopter.prefab", new Vector3(), new Quaternion(), true);
				if (!(bool) ((UnityEngine.Object) entity)) return;
				PatrolHelicopterAI heliAI = entity.GetComponent<PatrolHelicopterAI>();
				heliAI.maxSpeed = float.Parse(Config["HeliSpeed"].ToString());		//helicopter speed
				entity.GetComponent<PatrolHelicopterAI>().SetInitialDestination(target.transform.position + new Vector3(0.0f, 10f, 0.0f), 0.25f);

				//Change the health & weakpoint(s) heath
				((BaseCombatEntity)entity).startHealth = float.Parse(Config["MainHealth"].ToString());
				var weakspots = ((BaseHelicopter)entity).weakspots;
				weakspots[0].maxHealth = float.Parse(Config["MainRotorHealth"].ToString());
				weakspots[0].health = float.Parse(Config["MainRotorHealth"].ToString());
				weakspots[1].maxHealth = float.Parse(Config["TailRotorHealth"].ToString());
				weakspots[1].health = float.Parse(Config["TailRotorHealth"].ToString());
				/*
				foreach (var weakspot in weakspots)
				{
					foreach (string str in weakspot.bonenames)
						Puts(str);
					Puts(weakspot.health.ToString());
				}
				Puts(((BaseCombatEntity)entity).StartHealth().ToString());
				Puts("health is " + ((BaseCombatEntity)entity).Health().ToString());
				//*/
				entity.Spawn(true);
				i++;
			}
		}
		
		/*--------------------------------------------------------------//
		//					call - call heli in general					//
		//--------------------------------------------------------------*/
		private void call(int num = 1)
		{
			int i = 0;
			while (i < num)
			{
				BaseEntity entity = GameManager.server.CreateEntity("assets/bundled/prefabs/npc/patrol_helicopter/PatrolHelicopter.prefab", new Vector3(), new Quaternion(), true);
				if (!(bool) ((UnityEngine.Object) entity))
					return;
				PatrolHelicopterAI heliAI = entity.GetComponent<PatrolHelicopterAI>();
				heliAI.maxSpeed = float.Parse(Config["HeliSpeed"].ToString());		//helicopter speed
								//Change the health & weakpoint(s) heath
				((BaseCombatEntity)entity).startHealth = float.Parse(Config["MainHealth"].ToString());
				var weakspots = ((BaseHelicopter)entity).weakspots;
				weakspots[0].maxHealth = float.Parse(Config["MainRotorHealth"].ToString());
				weakspots[0].health = float.Parse(Config["MainRotorHealth"].ToString());
				weakspots[1].maxHealth = float.Parse(Config["TailRotorHealth"].ToString());
				weakspots[1].health = float.Parse(Config["TailRotorHealth"].ToString());
				entity.Spawn(true);
				i++;
			}
		}
		
		/*--------------------------------------------------------------//
		//		canExecute - check if the player has permission			//
		//--------------------------------------------------------------*/
		private bool canExecute(BasePlayer player)
		{
			if ((bool)Config["UsePermissions"])	//check if permissions system is in use
			{
				if (!permission.UserHasPermission(player.userID.ToString(), "CanCallHeli")) 
				{
					SendReply(player, "You do not have permission for this command.");
					return false;
				}
			}
			else
			{
				if (!player.IsAdmin())			//if permissions system is not in use, check for admin
				{
					SendReply(player, "You do not have access to this command.");
					return false;
				}
			}
			return true;
		}
		
		/*--------------------------------------------------------------//
		//			  Find a player by name/partial name				//
		//				Thank You Whoever Wrote This					//
		//--------------------------------------------------------------*/
        private BasePlayer FindPlayerByPartialName(string name) 
		{
            if (string.IsNullOrEmpty(name))
                return null;
            BasePlayer player = null;
            name = name.ToLower();
            var allPlayers = BasePlayer.activePlayerList.ToArray();
            // Try to find an exact match first
            foreach (var p in allPlayers) 
			{
                if (p.displayName == name) 
				{
                    if (player != null)
                        return null; // Not unique
                    player = p;
                }
            }
            if (player != null)
                return player;
            // Otherwise try to find a partial match
            foreach (var p in allPlayers) 
			{
                if (p.displayName.ToLower().IndexOf(name) >= 0) 
				{
                    if (player != null)
                        return null; // Not unique
                    player = p;
                }
            }
            return player;
        }
		
	/*----------------------------------------------------------------------------------------------------------------------------//
	//												STORED DATA CLASSES															  //
	//----------------------------------------------------------------------------------------------------------------------------*/
		/*--------------------------------------------------------------//
		//	StoredData class - holds a list of BoxInventories			//
		//--------------------------------------------------------------*/
		class StoredData
        {
            public List<BoxInventory> HeliInventoryLists = new List<BoxInventory>();

            public StoredData()
            {
            }
        }

		/*--------------------------------------------------------------//
		//	BoxInventory class - represents heli_crate inventory		//
		//--------------------------------------------------------------*/
        class BoxInventory
        {
            public List<ItemDef> lootBoxContents = new List<ItemDef>();

            public BoxInventory() {}

            public BoxInventory(List<ItemDef> list)
            {
                lootBoxContents = list;
            }
			
			public BoxInventory(List<Item> list)
            {
                foreach (var item in list)
				{
					lootBoxContents.Add(new ItemDef(item.info.shortname, item.amount));
				}
            }
			
			public BoxInventory(string name, int amount) 
			{
				lootBoxContents.Add(new ItemDef(name, amount));
			}
			
			public int InventorySize()
			{
				return lootBoxContents.Count;
			}
			
            public List<ItemDef> GetlootBoxContents()
            {
                return lootBoxContents;
            }
        }
		/*--------------------------------------------------------------//
		//			ItemDef class - represents an item					//
		//--------------------------------------------------------------*/
		class ItemDef
		{
			public string name;
			public int amount;
			
			public ItemDef() {}
			
			public ItemDef(string name, int amount)
			{
				this.name = name;
				this.amount = amount;
			}
		}
	
		/*--------------------------------------------------------------//
		//			LoadSaveData - loads up the loot data				//
		//--------------------------------------------------------------*/
	    void LoadSavedData()
        {
            storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("HeliControlData");
			//Create a default data file if there was none:
			if (storedData == null)
			{
				PrintWarning("No Lootdrop Data!! Creating new file...");
				storedData = new StoredData();
				BoxInventory inv;
				inv = new BoxInventory("rifle.ak", 1);
				inv.lootBoxContents.Add(new ItemDef("ammo.rifle.hv", 128));
				storedData.HeliInventoryLists.Add(inv);
				
				inv = new BoxInventory("rifle.bolt", 1);
				inv.lootBoxContents.Add(new ItemDef("ammo.rifle.hv", 128));
				storedData.HeliInventoryLists.Add(inv);
				
				inv = new BoxInventory("explosive.timed", 3);
				inv.lootBoxContents.Add(new ItemDef("ammo.rocket.hv", 3));
				storedData.HeliInventoryLists.Add(inv);
				
				SaveData();
			}
        }
		
		/*--------------------------------------------------------------//
		//			  SaveData - really only used for testing			//
		//--------------------------------------------------------------*/
		void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("HeliControlData", storedData);
	}
}