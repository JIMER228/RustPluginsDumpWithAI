using Oxide.Core;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
	[Info("VehicleManager", "rostov114", "0.1.4")]
	class VehicleManager : RustPlugin
	{
		#region Variables
		public static VehicleManager Instance;
		public string vehicleShortPrefabName = "box.wooden.large";
		public List<VehicleInfo> _vehicles = new List<VehicleInfo>()
		{
			new VehicleInfo("boat", "assets/content/vehicles/boats/rowboat/rowboat.prefab", 1789554931),
			new VehicleInfo("rhib", "assets/content/vehicles/boats/rhib/rhib.prefab", 1789555583),
			new VehicleInfo("minicopter", "assets/content/vehicles/minicopter/minicopter.entity.prefab", 1789556466),
			new VehicleInfo("balloon", "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab", 1789557339),
			new VehicleInfo("sedan", "assets/content/vehicles/sedan_a/sedantest.entity.prefab", 1789556977),
			new VehicleInfo("horse", "assets/rust.ai/nextai/testridablehorse.prefab", 1773898864),
			new VehicleInfo("ch47", "assets/prefabs/npc/ch47/ch47.entity.prefab", 1771792500),
			new VehicleInfo("scraptransportheli", "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab", 1856165291)
		};
		#endregion

		#region VehicleInfo Class
		public class VehicleInfo
		{
			public string shortname;
			public string prefab;
			public ulong skinID;

			public VehicleInfo(string shortname, string prefab, ulong skinID)
			{
				this.shortname	= shortname;
				this.prefab		= prefab;
				this.skinID		= skinID;
			}
			
			public string Give(BasePlayer player, string shortname)
			{
				Item item = ItemManager.CreateByName(shortname, 1, this.skinID);
				item.name = Instance._(player, this.shortname);

				if (item != null)
				{
					player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
					return item.name;
				}

				return null;
			}

			public void Spawn(Vector3 position, Quaternion rotation = default(Quaternion))
			{
				BaseEntity entity = GameManager.server.CreateEntity(this.prefab, position, rotation) as BaseEntity;

				if (entity != null)
					entity.Spawn();
			}

			public static VehicleInfo FindByShortname(string shortname)
			{
				List<VehicleInfo> _vehicle = Instance._vehicles.Where(v => v.shortname == shortname).ToList();
				if (_vehicle != null && _vehicle.Count == 1)
					return _vehicle.Last();

				return null;
			}

			public static VehicleInfo FindByPrefab(string prefab)
			{
				List<VehicleInfo> _vehicle = Instance._vehicles.Where(v => v.prefab == prefab).ToList();
				if (_vehicle != null && _vehicle.Count == 1)
					return _vehicle.Last();

				return null;
			}

			public static VehicleInfo FindBySkinID(ulong skinID)
			{
				List<VehicleInfo> _vehicle = Instance._vehicles.Where(v => v.skinID == skinID).ToList();
				if (_vehicle != null && _vehicle.Count == 1)
					return _vehicle.Last();

				return null;
			}

			public static List<string> ShortNameList() // Я хлебушек, мне так можно
			{
				List<string> _vehicles = new List<string>();
				foreach (VehicleInfo _vehicle in Instance._vehicles)
					_vehicles.Add(_vehicle.shortname);

				return _vehicles;
			}
		}
		#endregion

		#region Language
		private void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["rhib"] = "RHIB",
				["minicopter"] = "MiniCopter",
				["boat"] = "Boat",
				["balloon"] = "Air balloon",
				["sedan"] = "Car",
				["horse"] = "Horse",
				["scraptransportheli"] = "Transport Helicopter",
				["ch47"] = "Chinook",

				["ConsoleSyntax"] = "Syntax: vehicle.give <steamid|username> <{0}> [shortname]",
				["PlayerNotFound"] = "Player '{0}' not found!",
				["VehicleNotFound"] = "Vehicle '{0}' not found!",
				["ShortnameNotFound"] = "Item shortname '{0}' not found!",
				["SuccessfullyGive"] = "Transport '{0}' successfully give to '{1}'",
			}, this);

			lang.RegisterMessages(new Dictionary<string, string>
			{
				["rhib"] = "Военный катер",
				["minicopter"] = "MiniCopter",
				["boat"] = "Лодка",
				["balloon"] = "Воздушный шар",
				["sedan"] = "Автомобиль",
				["horse"] = "Лошадь",
				["scraptransportheli"] = "Транспортный вертолет",
				["ch47"] = "Чинук",

				["ConsoleSyntax"] = "Синтаксис: vehicle.give <steamid|username> <{0}> [shortname]",
				["PlayerNotFound"] = "Игрок с никнеймом '{0}' не найден!",
				["VehicleNotFound"] = "Транспорт с названием '{0}' не найден!",
				["ShortnameNotFound"] = "Item с shortname '{0}' не найден!",
				["SuccessfullyGive"] = "Транспорт '{0}' успешно выдан игроку '{1}'",
			}, this, "ru");
		}

		private string _(BasePlayer player, string key, params object[] args)
		{
			return string.Format(lang.GetMessage(key, this, player?.UserIDString), args);
		}
		#endregion

		#region Init
		private void OnServerInitialized()
		{
			Instance = this;

			foreach (BasePlayer player in BasePlayer.activePlayerList)
				foreach (Item item in player.inventory.AllItems())
					this.ChangeVehicleName(item, player);
		}

		private void Unload()
		{
			Instance = null;
		}
		#endregion

		#region Oxide Hooks
		private void OnPlayerInit(BasePlayer player) 
		{
			player.inventory.AllItems().ToList().ForEach(it => this.ChangeVehicleName(it, player));
		}

		private void OnItemAddedToContainer(ItemContainer container, Item item) 
		{
			if (container == null || item == null || container.playerOwner == null)
				return;

			this.ChangeVehicleName(item, container.playerOwner);
		}

		private object OnGiveSoldItem(NPCVendingMachine vending, Item soldItem, BasePlayer buyer) 
		{
			if (soldItem.skin > 0)
			{
				VehicleInfo vehicle = VehicleInfo.FindBySkinID(soldItem.skin);
				if (vehicle != null)
				{
					vehicle.Give(buyer, this.vehicleShortPrefabName);
					return false;
				}
			}

			return null;
		}

		private void OnEntityBuilt(Planner plan, GameObject obj)
		{
			BaseEntity entity = obj.GetComponent<BaseEntity>(); 
			if (entity != null && entity.ShortPrefabName == this.vehicleShortPrefabName)
			{
				VehicleInfo vehicle = VehicleInfo.FindBySkinID(entity.skinID);
				if (vehicle == null)
					return;

				vehicle.Spawn(entity.transform.position, entity.transform.rotation * Quaternion.Euler(0, 90, 0));
				NextTick(() =>
				{
					entity.Kill();
				});
			}
		}

		private object OnHammerHit(BasePlayer player, HitInfo info) 
		{
			if (player == null || info == null || info?.HitEntity == null) 
				return null; 

			if (info.HitEntity is BaseVehicle && player.serverInput.IsDown(BUTTON.FIRE_SECONDARY))
			{
				VehicleInfo vehicle = VehicleInfo.FindByPrefab(info.HitEntity.PrefabName);
				if (vehicle == null)
					return null;

				if (Interface.Oxide.CallHook("CanPickupVehicle", player, info.HitEntity) != null)
					return true;

				Interface.Oxide.CallHook("OnPickupVehicle", player, info.HitEntity);
				vehicle.Give(player, this.vehicleShortPrefabName);

				NextTick(() =>
				{
					info.HitEntity.Kill();
				});
			}

			return null;
		}
		#endregion

		#region Console Hooks
		[ConsoleCommand("vehicle.give")]
		void ConsoleCommand_vehicleshop_give(ConsoleSystem.Arg arg)
		{
			BasePlayer p = arg?.Player() ?? null; 
			if (p != null && !p.IsAdmin) 
				return;

			if (arg.Args == null || arg.Args.Length < 2)
			{
				SendReply(arg, _(p, "ConsoleSyntax", string.Join("|",  VehicleInfo.ShortNameList())));
				return;
			}

			BasePlayer player = BasePlayer.Find(arg.Args[0]);
			if (player == null)
			{
				SendReply(arg, _(p, "PlayerNotFound", arg.Args[0]));
				return;
			}

			VehicleInfo vehicle = VehicleInfo.FindByShortname(arg.Args[1]);
			if (vehicle == null)
			{
				SendReply(arg, _(p, "VehicleNotFound", arg.Args[1]));
				SendReply(arg, _(p, "ConsoleSyntax", string.Join("|",  VehicleInfo.ShortNameList())));
				return;
			}

			string shortname = this.vehicleShortPrefabName;
			if (arg.Args.Length == 3)
			{
				ItemDefinition info = ItemManager.FindItemDefinition(arg.Args[2]);
				if (info == null) 
				{
					SendReply(arg, _(p, "ShortnameNotFound", arg.Args[2]));
					return;
				}

				shortname = arg.Args[2];
			}

			SendReply(arg, _(p, "SuccessfullyGive", vehicle.Give(player, shortname), player.displayName));
		}
		#endregion

		#region Helpers
		private void ChangeVehicleName(Item item, BasePlayer player) 
		{
			if (item == null || player == null || item.info.shortname != this.vehicleShortPrefabName || item.skin == 0)
				return;

			VehicleInfo vehicle = VehicleInfo.FindBySkinID(item.skin);
			if (vehicle == null)
				return;

			item.name = _(player, vehicle.shortname);
		}
		#endregion
	}
}