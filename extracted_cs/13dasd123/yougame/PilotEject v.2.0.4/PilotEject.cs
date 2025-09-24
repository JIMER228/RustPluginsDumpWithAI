using System;
using System.Collections.Generic; 
using Oxide.Core; 
using Oxide.Core.Configuration; 
using Oxide.Core.Plugins; 
using UnityEngine; 
using Random = UnityEngine.Random; 

namespace Oxide.Plugins 
{
	[Info("PilotEject", "redBDGR", "2.0.4")] 
	[Description("A special helicopter event")]
	class PilotEject : RustPlugin 
	{
		[PluginReference] Plugin GUIAnnouncements;

		private bool useGUIAnnouncements; 
		private static PilotEject plugin; 
		private const string permissionName = "piloteject.admin"; 
		private float arrowHeight = 200f; 
		private float arrowLength = 120f;
		private float chanceOfOccuring = 0.20f;
		private bool changed;
		private bool inboundMessage = true;
		private bool killMSG = true;
		private float maxEventTime = 3600f;
		private float maxTimeToEvent = 600.0f;
		private float minEventTime = 1800f;
		private float minimumSpawnHeight = 20f; 
		private bool minimumSpawnHeightEnabled;
		private float minTimeToEvent = 300.0f;
		private bool pilotArrow = true; 
		private float pilotLifeLength = 600f; 
		private int customCratesToDrop = 3;
		private bool randomEventEnabled = true;
		private int minHelicoptersBeforeNextEvent = 0;
		private float pilotFallSpeed = 1;
		
		private Timer repeat;
		private StoredData storedData; 
		private DynamicConfigFile InventoryData;

		private float time;
		private bool timedEventEnabled = true; 
		private bool truePVEPilotDamageable;
		private int heliCount = 0;

		private Dictionary<string, PilotInfo> pilotData = new Dictionary<string, PilotInfo>();
		private List<BaseHelicopter> helis = new List<BaseHelicopter>();
		private List<EjectedPlayer> pilots = new List<EjectedPlayer>();

		private class StoredData 
		{
			public Dictionary<string, PilotInfo> pilotData = new Dictionary<string, PilotInfo>();
		}

		private class PilotInfo 
		{
			public string displayName;
			public List<ItemInfo> beltItems;
			public List<ItemInfo> clothingItems;
			public List<ItemInfo> inventoryItems;
			public float chance; 
			public float health; 
			public bool dropItems; 
			public bool wounded; 
		} 
		
		private class ItemInfo 
		{
			public int amount;
			public float chance;
			public string shortname;
			public ulong skinId;
		}

		private void SaveData() 
		{
			storedData.pilotData = pilotData;
			InventoryData.WriteObject(storedData);
		}

		private void LoadData() 
		{
			try 
			{
				storedData = InventoryData.ReadObject<StoredData>();
				pilotData = storedData.pilotData; 
			}
			catch 
			{
				Puts("Failed to load data, creating new file");
				storedData = new StoredData(); 
			} 
		} 
		
		private void LoadVariables() 
		{
			minTimeToEvent = Convert.ToSingle(GetConfig("Settings", "Min Time Until Pilot Eject", 300.0f));
			maxTimeToEvent = Convert.ToSingle(GetConfig("Settings", "Max Time Until Pilot Eject", 600.0f));
			killMSG = Convert.ToBoolean(GetConfig("Settings", "Pilot Death Message Enabled", true));
			pilotLifeLength = Convert.ToSingle(GetConfig("Settings", "Pilot Life Length", 600f)); 
			minimumSpawnHeight = Convert.ToSingle(GetConfig("Settings", "Pilot Minimum Spawn Height", 20f));
			minimumSpawnHeightEnabled = Convert.ToBoolean(GetConfig("Settings", "Min Spawn Height Enabled", false));
			inboundMessage = Convert.ToBoolean(GetConfig("Settings", "Inbound Message", true));
			customCratesToDrop = Convert.ToInt32(GetConfig("Settings", "Number of Crates to Spawn", 3));
			truePVEPilotDamageable = Convert.ToBoolean(GetConfig("Settings", "(TruePVP) Pilot Is Damageable", true));
			useGUIAnnouncements = Convert.ToBoolean(GetConfig("Settings", "Use GUIAnnouncements", false));
			pilotArrow = Convert.ToBoolean(GetConfig("Arrow Settings", "Arrow Above Pilot", true));
			arrowHeight = Convert.ToSingle(GetConfig("Arrow Settings", "Arrow Height", 200f));
			arrowLength = Convert.ToSingle(GetConfig("Arrow Settings", "Arrow Length (seconds)", 120f)); 
			timedEventEnabled = Convert.ToBoolean(GetConfig("Timed Event Settings", "Timed Helicopter Enabled", false));
			minEventTime = Convert.ToSingle(GetConfig("Timed Event Settings", "Min Time Until Next Event", 1800f)); 
			maxEventTime = Convert.ToSingle(GetConfig("Timed Event Settings", "Max Time Until Next Event", 3600f)); 
			randomEventEnabled = Convert.ToBoolean(GetConfig("Random Event Settings", "Chance of helicopter being event heli (external helicopters only)", true));
			chanceOfOccuring = Convert.ToSingle(GetConfig("Random Event Settings", "Chance Of Occuring", 0.20f));
			minHelicoptersBeforeNextEvent = Convert.ToInt32(GetConfig("Random Event Settings", "Min Helicopters Between Random Event", 0));

			if (!changed) return;
			SaveConfig();
			changed = false; 
		} 
		
		protected override void LoadDefaultConfig() 
		{
			Config.Clear(); 
			LoadVariables(); 
		} 
		
		private void Init() 
		{
			plugin = this;
			LoadVariables();
			InventoryData = Interface.Oxide.DataFileSystem.GetFile("PilotEject");
			LoadData(); 
			
			permission.RegisterPermission(permissionName, this);
			if (timedEventEnabled) timer.Repeat(Random.Range(minEventTime, maxEventTime), 0, CallBrokenHeli);

			lang.RegisterMessages(new Dictionary<string, string> 
			{
                ["Heli Malfunctioned"] = "Патрульный вертолёт вышел из строя! Пилоту пришлось спрыгнуть с вертолёта на парашюте. Найди его и забери его лут!",
                ["No Permission"] = "У вас нет доступа к этой команде.",
                ["Pilot Killed"] = "Пилот был убит игроком <color=#ffd479>{0}</color>",
                ["Pilot Inventory Set"] = "Инвентарь пилота успешно сохранён.",
                ["Malfunction Timer Warning (Console)"] = "Патрульный вертолёт выйдет из строя через {0} секунд",
                ["Broken Heli Inbound"] = "Вертолёт сильно повреждён!",
				["setpilot Invalid Syntax"] = "Неправильный синтаксис!\nДоступная команда:\n/setpilot <add/remove/set> <name>"
			}, this);

			foreach (BasePlayer player in BasePlayer.activePlayerList)
			if (permission.UserHasPermission(player.UserIDString, "playerwake.notify")) player.ChatMessage("msg"); 
		} 
		
		private void Unload() { } 
		private void OnServerSave() 
		{
			SaveData(); 
		}

		private void OnEntityDeath(BaseCombatEntity entity, HitInfo info) 
		{
			if (!entity || info == null) return;
			BaseHelicopter heli = entity.GetComponent<BaseHelicopter>();
			if (heli) 
			{
				if (helis.Contains(heli)) helis.Remove(heli); 
				return; 
			} 
			EjectedPlayer pilot = entity.GetComponent<EjectedPlayer>(); 
			if (pilot == null) return;
			BasePlayer player = info.InitiatorPlayer;
			if (player) 
				if (killMSG) 
				{
					if (useGUIAnnouncements) 
						SendGlobaGUIAnnouncement(string.Format(msg("Pilot Killed"), player.displayName));
					else 
						rust.BroadcastChat(null, string.Format(msg("Pilot Killed"), player.displayName)); 
				} 
		} 
		
		private void OnEntitySpawned(BaseNetworkable entity) 
		{
			BaseHelicopter heli = entity.GetComponent<BaseHelicopter>();
			if (!heli) 
			{
				PlayerCorpse corpse = entity.GetComponent<PlayerCorpse>();
				if (!corpse) return; 
				if (corpse.playerSteamID.ToString().StartsWith("76561198")) return;
				PilotInfo data = null; 
				try 
				{
					if (!pilotData.TryGetValue(corpse.playerName, out data)) return; 
				} 
				catch(Exception e) 
				{
					Puts($"Pilot {corpse.playerName} tried to spawn it's inventory but no inventory was found!"); 
					return; 
				} 
				timer.Once(0.5f, () => GiveItems(corpse, data)); 
				return; 
			} 
			
			if (heliCount != minHelicoptersBeforeNextEvent) 
			{
				heliCount++; 
				return; 
			} 
			timer.Once(0.1f, () => 
			{
				if (heli == null) return;
				if (helis.Contains(heli)) return; 
				if (randomEventEnabled) 
				{
					float rng = Random.Range(0f, 1f);
					if (rng > chanceOfOccuring) return;
					TriggerEventStart(heli);
					return; 
				}
				else 
				{
					TriggerEventStart(heli); 
				} 
			}); 
		} 
		
		private object CanEntityTakeDamage(BaseCombatEntity entity, HitInfo info) 
		{
			if (!truePVEPilotDamageable) return null;
			EjectedPlayer player = entity.GetComponent<EjectedPlayer>();
			if (player == null) return null;
			info.damageTypes.Scale(Rust.DamageType.Heat, 0);
			if (pilots.Contains(player)) return true;
			return null; 
		}

		object OnNpcPlayerResume(NPCPlayerApex player) 
		{
			EjectedPlayer pilot = player.GetComponent<EjectedPlayer>();
			if (!pilot) return null;
			if (pilot.isGrounded) return null;
			return false; 
		}

		private class EjectHelicopter : FacepunchBehaviour 
		{
			private BaseHelicopter heli;
			private List<Scientist> pilots = new List<Scientist>();
			private float eventTime;
			private bool eventTriggered;
			private bool isOverWater = true;
			private bool spawnHeightEnabled = plugin.minimumSpawnHeightEnabled;
			private float minSpawnHeight = plugin.minimumSpawnHeight;
			private void Awake() 
			{
				heli = GetComponent<BaseHelicopter>();
				eventTime = UnityEngine.Time.time + Random.Range(plugin.minTimeToEvent, plugin.maxTimeToEvent);
				plugin.Puts(string.Format(plugin.msg("Malfunction Timer Warning (Console)"), eventTime - UnityEngine.Time.time));
				if (plugin.inboundMessage) 
				{
					if (plugin.useGUIAnnouncements) 
						plugin.SendGlobaGUIAnnouncement(plugin.msg("Broken Heli Inbound"));
					else
						plugin.rust.BroadcastChat(null, plugin.msg("Broken Heli Inbound")); 
				}
				InvokeRepeating(UpdateWaterCheck, 10f, 1f); 
			}

			private void Update() 
			{
				if (eventTriggered == false) 
					if (UnityEngine.Time.time >= eventTime) 
					{
						if (isOverWater) return; StartEvent(); 
					} 
			} 
			
			private void OnDestroy() 
			{
				CancelInvoke(UpdateWaterCheck);
				foreach (Scientist pilot in pilots) 
				pilot.GetComponent<EjectedPlayer>().heliCrashPos = new Vector3(transform.position.x + UnityEngine.Random.Range(-5f, 5f), TerrainMeta.HeightMap.GetHeight(transform.position), transform.position.z + UnityEngine.Random.Range(-5f, 5f)); 
			}

			private void StartEvent() 
			{
				eventTriggered = true;
				if (heli) 
				{
					if (plugin.useGUIAnnouncements) 
						plugin.SendGlobaGUIAnnouncement(plugin.msg("Heli Malfunctioned")); 
					else 
						plugin.rust.BroadcastChat(null, plugin.msg("Heli Malfunctioned"));

					heli.Hurt(heli.health - 10.0f);
					heli.maxCratesToSpawn = plugin.customCratesToDrop;
					SpawnNPCs();
					heli.Hurt(heli.health);
				} 
			} 
			
			private void UpdateWaterCheck() 
			{
				bool x = WaterLevel.GetWaterInfo(new Vector3(transform.position.x, TerrainMeta.HeightMap.GetHeight(transform.position), transform.position.z)).isValid;
				if (isOverWater == true && x == false) 
					if (UnityEngine.Time.time >= eventTime) 
						eventTime = UnityEngine.Time.time + 10f;
					isOverWater = x; 
			} 
			
			private void SpawnNPCs() 
			{
				foreach (var entry in plugin.pilotData) 
				CreatePilot(entry.Value); 
			} 
			
			private void CreatePilot(PilotInfo data) 
			{
				Vector3 pos = transform.position;
				pos = new Vector3(pos.x + UnityEngine.Random.Range(-5f, 5f), pos.y, pos.z + UnityEngine.Random.Range(-5f, 5f));
				if (spawnHeightEnabled) 
					if (pos.y < minSpawnHeight) 
						pos = new Vector3(pos.x, minSpawnHeight, pos.z);
					Scientist pilot = GameManager.server.CreateEntity("assets/prefabs/npc/scientist/scientist.prefab", pos).GetComponent<Scientist>();
					pilot.Pause();
					pilot.CommunicationRadius = 0f;
					pilot.Spawn();
					pilot.GetComponent<NPCPlayer>().enabled = false;
					pilots.Add(pilot);
					pilot.displayName = data.displayName;
					pilot.health = data.health;
					pilot.SendNetworkUpdateImmediate();
					GiveItems(pilot.GetComponent<BasePlayer>(), data);
					EjectedPlayer ep = pilot.gameObject.AddComponent<EjectedPlayer>();
					ep.data = data; 
			} 
			
			private void GiveItems(BasePlayer player, PilotInfo data) 
			{
				foreach (var item in data.clothingItems) 
				if (Random.Range(0f, 1f) < item.chance) 
				{
					var newitem = ItemManager.CreateByName(item.shortname, item.amount, item.skinId);
					if (newitem == null) continue;
					newitem.MoveToContainer(player.inventory.containerWear); 
				} 
			} 
		} 
		
		private class EjectedPlayer : FacepunchBehaviour 
		{
			private BasePlayer player;
			public NPCPlayerApex npc;
			public PilotInfo data;
			public bool isGrounded;
			public Vector3 groundPos;
			private BaseEntity chute;
			private float deathTime;
			public Vector3 heliCrashPos;
			private bool doDraws = plugin.pilotArrow;
			private float lifeLength = plugin.pilotLifeLength;
			private void Awake() 
			{
				player = GetComponent<BasePlayer>();
				npc = player.GetComponent<NPCPlayerApex>();
				plugin.pilots.Add(this);
				groundPos = new Vector3(transform.position.x, TerrainMeta.HeightMap.GetHeight(transform.position), transform.position.z);
				deathTime = UnityEngine.Time.time + lifeLength;
				InitChute();
			}
			private void Update() 
			{
				if (isGrounded == false) 
				{
					transform.position = new Vector3(transform.position.x, transform.position.y - 0.015f * plugin.pilotFallSpeed, transform.position.z);
					npc?.SendNetworkUpdateImmediate();
					if (Vector3.Distance(transform.position, groundPos) < 0.15f) 
					{
						OnLanded(); 
					} 
				} 
				else 
				{
					if (heliCrashPos != null && heliCrashPos != Vector3.zero) 
					{
						if (Vector3.Distance(transform.position, heliCrashPos) < 0.5f) 
						{
							CancelInvoke(() => npc.SetDestination(heliCrashPos)); 
						} 
						else 
							if (Vector3.Distance(transform.position, heliCrashPos) > 10f) 
							{
								heliCrashPos = new Vector3(transform.position.x + UnityEngine.Random.Range(-5f, 5f), TerrainMeta.HeightMap.GetHeight(transform.position), transform.position.z + UnityEngine.Random.Range(-5f, 5f));
								InvokeRepeating(() => npc.SetDestination(heliCrashPos), 0.2f, 5f); 
							} 
					} 
					if (UnityEngine.Time.time >= deathTime) 
					{
						KillPilot();
						return; 
					} 
				} 
			} 
			
			private void OnDestroy() 
			{
				if (chute) 
					chute.Kill();
				if (plugin.pilots.Contains(this)) 
					plugin.pilots.Remove(this);
				if (npc) 
					npc.displayName = data.displayName; 
			} 
			
			private void OnLanded() 
			{
				isGrounded = true;
				npc.Resume();
				transform.position = groundPos;
				chute?.Kill();
				if (data.wounded) 
					npc.StartWounded();
				else 
				{
					if (heliCrashPos != null && heliCrashPos != Vector3.zero) 
						InvokeRepeating(() => npc.SetDestination(heliCrashPos), 0.05f, 5f); 
				} 
				if (doDraws) 
					foreach (BasePlayer player in BasePlayer.activePlayerList) 
				plugin.DoDraws(player, transform.position); 
			} 
			
			public void KillPilot() 
			{
				if (player) 
					player.Kill(); 
			} 
			
			private void InitChute() 
			{
				chute = GameManager.server.CreateEntity("assets/prefabs/misc/parachute/parachute.prefab", new Vector3(0, 0, 0));
				chute.SetParent(player); 
				chute.Spawn(); 
			} 
		} 
		
		[ChatCommand("callbrokenheli")] 
		private void CallBrokenHeliCMD(BasePlayer player, string command, string[] args) 
		{
			if (!permission.UserHasPermission(player.UserIDString, permissionName)) 
			{
				player.ChatMessage(msg("No Permission", player.UserIDString)); 
				return; 
			} 
			CallBrokenHeli(); 
		}

		[ConsoleCommand("callbrokenheli")] 
		private void CallBrokeHeliCONSOLECMD(ConsoleSystem.Arg args) 
		{
			if (args.Connection != null) return; 
			CallBrokenHeli(); 
			Puts("Broken heli inbound");
		} 
		
		[ChatCommand("setpilot")] 
		private void SetpilotinventoryCMD(BasePlayer player, string command, string[] args) 
		{
			if (!permission.UserHasPermission(player.UserIDString, permissionName)) 
			{
				player.ChatMessage(msg("No Permission", player.UserIDString)); 
				return; 
			}
			if (args.Length != 2) 
			{
				player.ChatMessage(msg("setpilot Invalid Syntax", player.UserIDString));
				return; 
			}
			
			switch (args[0]) 
			{
				case "add": 
				if (pilotData.ContainsKey(args[1])) 
				{
					player.ChatMessage($"This pilot already exists. Use \"/setpilot set {args[1]}\" to change this pilots inventory"); 
					break; 
				} 
				pilotData.Add(args[1], new PilotInfo() 
				{
					chance = 1f,
					displayName = args[1],
					health = 100f,
					dropItems = true,
					beltItems = new List<ItemInfo>(),
					clothingItems = new List<ItemInfo>(),
					inventoryItems = new List<ItemInfo>(),
					wounded = false 
				});
				AddItemsToData(args[1], player);
				player.ChatMessage($"{args[1]} has been added!");
				break;

				case "remove": 
				if (!pilotData.ContainsKey(args[1])) 
				{
					player.ChatMessage($"{args[1]} does not exist!");
					break; 
				} 
				pilotData.Remove(args[1]); 
				player.ChatMessage($"{args[1]} has been removed!");
				break; 
				
				case "set":
				PilotInfo data;
				if (!pilotData.TryGetValue(args[1], out data)) 
				{
					player.ChatMessage($"{args[1]} does not exist!"); 
					break; 
				} 
				AddItemsToData(args[1], player);
				player.ChatMessage($"{args[1]}'s inventory has been set!");
				break; 
				default: 
				player.ChatMessage(msg("setpilot Invalid Syntax", player.UserIDString)); 
				break; 
			} 
			
			SaveData();
			LoadData(); 
		}

		private void SendGlobaGUIAnnouncement(string msg) 
		{
			foreach(BasePlayer player in BasePlayer.activePlayerList) 
			if (player.IsConnected) 
				GUIAnnouncements?.Call("CreateAnnouncement", msg, "Grey", "White", player); 
		} 
		
		private void AddItemsToData(string pilotName, BasePlayer player) 
		{
			PilotInfo data; 
			if (!pilotData.TryGetValue(pilotName, out data)) return; 
			data.inventoryItems.Clear();
			data.clothingItems.Clear();
			data.beltItems.Clear();
			foreach (var item in player.inventory.containerMain.itemList) 
			data.inventoryItems.Add(new ItemInfo 
			{
				shortname = item.info.shortname,
				amount = item.amount,
				chance = 1f,
				skinId = item.skin 
			});

			foreach (var item in player.inventory.containerBelt.itemList) 
			data.beltItems.Add(new ItemInfo 
			{
				shortname = item.info.shortname,
				amount = item.amount,
				chance = 1f,
				skinId = item.skin 
			});

			foreach (var item in player.inventory.containerWear.itemList) 
			data.clothingItems.Add(new ItemInfo 
			{
				shortname = item.info.shortname,
				amount = item.amount,
				chance = 1f,
				skinId = item.skin 
			}); 
		} 
		
		private void DoDraws(BasePlayer player, Vector3 startPos) 
		{
			if (player.IsAdmin) 
			{
				player.SendConsoleCommand("ddraw.arrow", arrowLength, Color.red, startPos + new Vector3(0f, 25f + arrowHeight, 0f), startPos + new Vector3(0f, 25f, 0f), 4.0f); 
			} 
			else 
			{
				player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
				player.SendNetworkUpdateImmediate();
				player.SendConsoleCommand("ddraw.arrow", arrowLength, Color.red, startPos + new Vector3(0f, 25f + arrowHeight, 0f), startPos + new Vector3(0f, 25f, 0f), 4.0f);
				player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
				player.SendNetworkUpdateImmediate(); 
			} 
		} 
		
		private void GiveItems(PlayerCorpse corpse, PilotInfo data) 
		{
			foreach (ItemContainer container in corpse.containers) 
			container.Clear(); 
			foreach (var item in data.inventoryItems) 
			if (Random.Range(0f, 1f) < item.chance) 
			{
				var newitem = ItemManager.CreateByName(item.shortname, item.amount, item.skinId);
				if (newitem == null) continue; 
				newitem.MoveToContainer(corpse.containers[0]); 
			} 
			foreach (var item in data.beltItems) 
			if (Random.Range(0f, 1f) < item.chance) 
			{
				var newitem = ItemManager.CreateByName(item.shortname, item.amount, item.skinId); 
				if (newitem == null) continue; 
				newitem.MoveToContainer(corpse.containers[2]); 
			} 
			foreach (var item in data.clothingItems) 
			if (Random.Range(0f, 1f) < item.chance) 
			{
				var newitem = ItemManager.CreateByName(item.shortname, item.amount, item.skinId);
				if (newitem == null) continue; 
				newitem.MoveToContainer(corpse.containers[1]); 
			} 
		}

		private void TriggerEventStart(BaseHelicopter heli) 
		{
			heli.gameObject.AddComponent<EjectHelicopter>();
			helis.Add(heli);
			heliCount = 0; 
		} 
		
		private void CallBrokenHeli() 
		{
			BaseEntity ent = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(0, 50f, 0));
			ent.GetComponent<PatrolHelicopterAI>().enabled = false;
			ent.transform.position = new Vector3(0, 50, 0);
			ent.GetComponent<PatrolHelicopterAI>().enabled = true;
			helis.Add((BaseHelicopter)ent);
			ent.Spawn();
			ent.gameObject.AddComponent<EjectHelicopter>();
		} 
		
		private object GetConfig(string menu, string datavalue, object defaultValue) 
		{
			var data = Config[menu] as Dictionary<string, object>;
			if (data == null) 
			{
				data = new Dictionary<string, object>();
				Config[menu] = data;
				changed = true; 
			} 
			object value; 
			if (data.TryGetValue(datavalue, out value)) return value; 
			value = defaultValue;
			data[datavalue] = value;
			changed = true;
			return value; 
		} 
		
		private string msg(string key, string id = null) 
		{
			return lang.GetMessage(key, this, id); 
		} 
		
		private bool IsDamagedHeli(BaseHelicopter heli) 
		{
			return helis.Contains(heli); 
		} 
	} 
}