using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
	[Info("HW Loot Multiplier", "klauz24", "1.2.2"), Description("Simple loot multiplier for your Hurtworld server")]
	internal class HWLootMultiplier : HurtworldPlugin
	{
		private PlayerIdentity _owner;

		private Configuration _config;

		private int _dayNightMultiplier;

		private int _lastTimeChange = -1;

		private class Configuration
		{
			[JsonProperty(PropertyName = "Global multiplier")]
			public Global GlobalMultiplier = new Global();

			[JsonProperty(PropertyName = "Day/Night multiplier")]
			public DayNight DayNight = new DayNight();

			[JsonProperty(PropertyName = "Machines")]
			public Machines Machines = new Machines();

			[JsonProperty(PropertyName = "Resources")]
			public Resources Resources = new Resources();

			[JsonProperty(PropertyName = "Cases")]
			public Cases Cases = new Cases();

			[JsonProperty(PropertyName = "Events")]
			public Events Events = new Events();

			[JsonProperty(PropertyName = "Towns")]
			public Towns Towns = new Towns();

			public string ToJson() => JsonConvert.SerializeObject(this);

			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
		}

		public class Global
		{
			[JsonProperty(PropertyName = "Enable global multiplier")]
			public bool Enable = false;

			[JsonProperty(PropertyName = "Global multiplier")]
			public int Multiplier = 1;
		}

		private class DayNight
		{
			[JsonProperty(PropertyName = "Enable day/night multiplier")]
			public bool Enable = false;

			[JsonProperty(PropertyName = "Day multiplier")]
			public int Day = 1;

			[JsonProperty(PropertyName = "Night multiplier")]
			public int Night = 1;

			[JsonProperty(PropertyName = "Time check interval")]
			public int Interval = 5;
		}

		private class Machines
		{
			[JsonProperty(PropertyName = "Mining drills")]
			public int MiningDrills = 1;

			[JsonProperty(PropertyName = "Vehicles")]
			public int Vehicles = 1;
		}

		private class Resources
		{
			[JsonProperty(PropertyName = "Plants")]
			public int Plants = 1;

			[JsonProperty(PropertyName = "Gather")]
			public int Gather = 1;

			[JsonProperty(PropertyName = "Animals")]
			public int Animals = 1;

			[JsonProperty(PropertyName = "Pick up")]
			public int PickUp = 1;

			[JsonProperty(PropertyName = "Explodable mining rocks")]
			public int ExplodableMiningRock = 1;
		}

		private class Cases
		{
			[JsonProperty(PropertyName = "Red town case")]
			public int RedTownHardCase = 1;

			[JsonProperty(PropertyName = "Purple town case")]
			public int PurpleTownHardCase = 1;

			[JsonProperty(PropertyName = "Fragments case T1")]
			public int FragmentsGreen = 1;

			[JsonProperty(PropertyName = "Fragments case T2")]
			public int FragmentsBlue = 1;

			[JsonProperty(PropertyName = "Fragments case T3")]
			public int FragmentsYellow = 1;
		}

		private class Events
		{
			[JsonProperty(PropertyName = "Airdrop")]
			public int Airdrop = 1;

			[JsonProperty(PropertyName = "Loot frenzy")]
			public int LootFrenzy = 1;

			[JsonProperty(PropertyName = "Control town reward (Amount of cases)")]
			public int TownEvent = 1;
		}

		private class Towns
        {
			[JsonProperty(PropertyName = "Boxes")]
			public int Boxes = 1;

			[JsonProperty(PropertyName = "Blacklist")]
			public List<string> Blacklist = new List<string>()
			{
				"Full/NameKey/Here"
			};
		}

		protected override void LoadDefaultConfig() => _config = new Configuration();

		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				_config = Config.ReadObject<Configuration>();
				if (_config == null)
				{
					throw new JsonException();
				}

				if (!_config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
				{
					Puts("Configuration appears to be outdated; updating and saving");
					SaveConfig();
				}
			}
			catch
			{
				Puts($"Configuration file {Name}.json is invalid; using defaults");
				LoadDefaultConfig();
			}
		}

		protected override void SaveConfig()
		{
			Puts($"Configuration changes saved to {Name}.json");
			Config.WriteObject(_config, true);
		}

		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				{"Prefix", "<color=#A0BD7F>[HW Loot Multiplier]</color>"},
				{"Day", "Day has started, server rates has been changed to <color=#FFF85F>{0}x</color>."},
				{"Night", "Night has started, server rates has been changed to <color=#5187E3>{0}x</color>."}
			}, this);
		}

		private void OnServerInitialized()
		{
			if (_config.DayNight.Enable)
			{
				timer.Every(_config.DayNight.Interval, () => IsDayOrNight());
			}
		}

		private void OnPlantGather(GrowingPlantUsable plant, WorldItemInteractServer player, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(player.networkView.owner);
			HandleLoot(items, WhichMultiplierToUse(_config.Resources.Plants));
		}

		private void OnCollectiblePickup(LootOnPickup node, WorldItemInteractServer player, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(player.networkView.owner);
			HandleLoot(items, WhichMultiplierToUse(_config.Resources.PickUp));
		}

		private void OnDispenserGather(GameObject obj, HurtMonoBehavior player, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(player.networkView.owner);
			HandleLoot(items, WhichMultiplierToUse(_config.Resources.Gather));
		}

		private void OnDrillDispenserGather(GameObject obj, DrillMachine machine, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(machine.networkView.owner);
			HandleLoot(items, WhichMultiplierToUse(_config.Machines.MiningDrills));
		}

		private void OnAirdrop(GameObject obj, AirDropEvent airdrop, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(airdrop.networkView.owner);
			HandleLoot(items, WhichMultiplierToUse(_config.Events.Airdrop));
		}

		private void OnControlTownDrop(GameObject obj, ControlTownEvent townEvent, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(townEvent.networkView.owner);
			HandleLoot(items, WhichMultiplierToUse(_config.Events.TownEvent));
		}

		private void OnLootFrenzySpawn(GameObject obj, LootFrenzyTownEvent frenzyEvent, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(frenzyEvent.networkView.owner);
			HandleLoot(items, WhichMultiplierToUse(_config.Events.LootFrenzy));
		}

		private void OnMiningRockExplode(GameObject obj, ExplodableMiningRock rock, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(rock.networkView.owner);
			HandleLoot(items, WhichMultiplierToUse(_config.Resources.ExplodableMiningRock));
		}

		private void OnDisassembleVehicle(GameObject vehicle, VehicleStatManager vehicleStatManager, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(vehicleStatManager.networkView.owner);
			HandleLoot(items, WhichMultiplierToUse(_config.Machines.Vehicles));
		}

		private void OnEntityDropLoot(GameObject obj, List<ItemObject> items) => HandleLoot(items, WhichMultiplierToUse(_config.Resources.Animals));

		private void OnLootCaseOpen(ItemComponentLootCase lootCase, ItemObject obj, Inventory inv, List<ItemObject> items)
		{
			this._owner = GameManager.Instance.GetIdentity(inv.networkView.owner);
			var ltn = lootCase.LootTree.name;
			for (var i = 0; i < items.Count; i++)
			{
				var defaultStack = items[i].StackSize;
				if (ltn == "TownEventHardcaseLoot T1")
				{
					items[i].StackSize = defaultStack * WhichMultiplierToUse(_config.Cases.PurpleTownHardCase);
				}
				if (ltn == "TownEventHardcaseLoot")
				{
					items[i].StackSize = defaultStack * WhichMultiplierToUse(_config.Cases.RedTownHardCase);
				}
				if (ltn == "Fragments Tier 1")
				{
					items[i].StackSize = defaultStack * WhichMultiplierToUse(_config.Cases.FragmentsGreen);
				}
				if (ltn == "Fragments Tier 2")
				{
					items[i].StackSize = defaultStack * WhichMultiplierToUse(_config.Cases.FragmentsBlue);
				}
				if (ltn == "Fragments Tier 3")
				{
					items[i].StackSize = defaultStack * WhichMultiplierToUse(_config.Cases.FragmentsYellow);
				}
				items[i].InvalidateStack();
			}
		}

		private void OnEntitySpawned(HNetworkView data)
		{
			this._owner = GameManager.Instance.GetIdentity(data.owner);
			var name = data.gameObject.name;
			if (name == "GenericTownLootCacheServer(Clone)")
			{
				var inv = data.gameObject.GetComponent<Inventory>();
				if (inv != null)
				{
					for (var i = 0; i < inv.Capacity; i++)
					{
						var item = inv.GetSlot(i);
						if (!_config.Towns.Blacklist.Contains(item.GetNameKey()))
                        {
							var defaultStack = item.StackSize;
							item.StackSize = defaultStack * WhichMultiplierToUse(_config.Towns.Boxes);
							item.InvalidateStack();
						}
					}
				}
			}
		}

		private void HandleLoot(List<ItemObject> list, int multiplier)
		{
			for (var i = 0; i < list.Count; i++)
			{
				var defaultStack = list[i].StackSize;
				list[i].StackSize = defaultStack * multiplier;
				list[i].InvalidateStack();
			}
		}

		private int WhichMultiplierToUse(int value)
		{
			if (_config.GlobalMultiplier.Enable)
			{
				return _config.GlobalMultiplier.Multiplier;
			}
			else
			{
				if (_config.DayNight.Enable)
				{
					return _dayNightMultiplier;
				}
			}
			return value;
		}

		private void IsDayOrNight()
		{
			var isDay = TimeManager.Instance.GetIsDay();
			if (isDay)
			{
				_dayNightMultiplier = _config.DayNight.Day;
				HandleChatNotification(true);
			}
			else
			{
				_dayNightMultiplier = _config.DayNight.Night;
				HandleChatNotification(false);
			}
		}

		private void HandleChatNotification(bool isDay)
		{
			if (_lastTimeChange == -1)
			{
				SendNotification(isDay);
			}
			else
			{
				if (_lastTimeChange == 0 && isDay || _lastTimeChange == 1 && !isDay)
				{
					return;
				}
				SendNotification(isDay);
			}
		}

		private void SendNotification(bool isDay)
		{
			if (isDay)
			{
				Broadcast("Day", _config.DayNight.Day);
				_lastTimeChange = 0;
			}
			else
			{
				Broadcast("Night", _config.DayNight.Night);
				_lastTimeChange = 1;
			}
		}

		private void Broadcast(string str, params object[] args)
		{
			foreach (var session in GameManager.Instance.GetSessions().Values)
			{
				if (session != null)
				{
					hurt.SendChatMessage(session, GetLang(session, "Prefix"), string.Format(GetLang(session, str), args));
				}
			}
		}

		private string GetLang(PlayerSession session, string str) => lang.GetMessage(str, this, session.SteamId.ToString());
	}
}
