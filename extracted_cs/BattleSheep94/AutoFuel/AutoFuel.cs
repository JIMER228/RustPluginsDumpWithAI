using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Auto Fuel", "redBDGR, edit by BattleSheep", "1.0.8")]
    [Description("Automatically fuels lights using fuel from the tool cupboard's inventory")]
    class AutoFuel : RustPlugin
    {
		private bool Changed;

		private bool useBarbeque;
		private bool useCampfires;
		private bool useCeilingLight;
		private bool useFurnace;
		private bool useLargeFurnace;
		private bool useJackOLantern;
		private bool useLantern;
		private bool useFogmachine;
		private bool useSearchLight;
		private bool useTunaCanLamp;
		private bool useFireplace;
		private bool useSkullFirepit;
		private bool useSmallOilRefinery;
		

		private bool dontRequireFuel;
		
		private string permissionName;
		
		private List<string> activeShortNames = new List<string>();

		#region Configuration

		protected override void LoadDefaultConfig()
		{
			Config.Clear();
			LoadVariables();
			DoStartupItemNames();
		}

		private void LoadVariables()
		{
			useBarbeque = Convert.ToBoolean(GetConfig("Types to autofuel", "Use Barbeque", false));
			useCampfires = Convert.ToBoolean(GetConfig("Types to autofuel", "Use Campfire", false));
			useCeilingLight = Convert.ToBoolean(GetConfig("Types to autofuel", "Use Ceiling Light", true));
			useFurnace = Convert.ToBoolean(GetConfig("Types to autofuel", "Use Furnace", false));
			useLargeFurnace = Convert.ToBoolean(GetConfig("Types to autofuel", "Use Large Furnace", false));
			useJackOLantern = Convert.ToBoolean(GetConfig("Types to autofuel", "Use JackOLanterns", true));
			useLantern = Convert.ToBoolean(GetConfig("Types to autofuel", "Use Lantern", true));
			useFogmachine = Convert.ToBoolean(GetConfig("Types to autofuel", "Use Fogmachine", true));
			useSearchLight = Convert.ToBoolean(GetConfig("Types to autofuel", "Use Search Light", true));
			useTunaCanLamp = Convert.ToBoolean(GetConfig("Types to autofuel", "Use Tuna Can Lamp", true));
			useFireplace = Convert.ToBoolean(GetConfig("Types to autofuel", "Use Fireplace", false));
			useSkullFirepit = Convert.ToBoolean(GetConfig("Types to autofuel", "Use Skull Fire Pit", false));
			useSmallOilRefinery = Convert.ToBoolean(GetConfig("Types to autofuel", "Use Small Oil Refinery", false));
			dontRequireFuel = Convert.ToBoolean(GetConfig("Settings", "Don't require fuel", false));
			
			permissionName = Convert.ToString(GetConfig("Settings", "permissionName", "autofuel.use"));

			if (!Changed) return;
			SaveConfig();
			Changed = false;
		}

		#endregion

		#region Oxide Hooks

		private void Init()
		{
			LoadVariables();
			DoStartupItemNames();
			if (!permission.PermissionExists(permissionName)) permission.RegisterPermission(permissionName, this);
		}

		private void OnEntitySpawned(BaseNetworkable entity)
		{
			if (entity.GetComponent<BaseEntity>()?.ShortPrefabName == "jackolantern.angry" || entity.GetComponent<BaseEntity>()?.ShortPrefabName == "jackolantern.happy")
				entity.GetComponent<BaseOven>().fuelType = ItemManager.FindItemDefinition("wood");
		}

		private Item OnFindBurnable(BaseOven oven)
		{
			if (!permission.UserHasPermission(oven.OwnerID.ToString(), permissionName))
				return null;
			if (oven.fuelType == null)
				return null;
			if (!activeShortNames.Contains(oven.ShortPrefabName))
				return null;
			if (HasFuel(oven))
				return null;
			DecayEntity decayEnt = oven.GetComponent<DecayEntity>();
			if (decayEnt == null)
				return null;
			BuildingPrivlidge priv = decayEnt.GetBuildingPrivilege();
			if (priv == null)
				return null;
			else
			{
				if (dontRequireFuel)
					return ItemManager.CreateByName(oven.fuelType.shortname, 1);
				Item fuelItem = GetFuel(priv, oven);
				if (fuelItem == null)
					return null;
				RemoveItemThink(fuelItem);
				ItemManager.CreateByName(oven.fuelType.shortname, 1)?.MoveToContainer(oven.inventory);
				return null;
			}
		}

		#endregion

		#region Methods

		private bool HasFuel(BaseOven oven)
		{
			return oven.inventory.itemList.Any(item => item.info == oven.fuelType);
		}

		private Item GetFuel(BuildingPrivlidge priv, BaseOven oven)
		{
			return priv.inventory?.itemList?.FirstOrDefault(item => item.info == oven.fuelType);
		}

		private static void RemoveItemThink(Item item)
		{
			if (item == null)
				return;
			if (item.amount == 1)
			{
				item.RemoveFromContainer();
				item.RemoveFromWorld();
			}
			else
			{
				item.amount = item.amount - 1;
				item.MarkDirty();
			}
		}

		private void DoStartupItemNames()
		{
			if (useBarbeque)
				activeShortNames.Add("bbq.deployed");
			if (useCampfires)
				activeShortNames.Add("campfire");
			if (useCeilingLight)
				activeShortNames.Add("ceilinglight.deployed");
			if (useFurnace)
				activeShortNames.Add("furnace");
			if (useLargeFurnace)
				activeShortNames.Add("furnace.large");
			if (useJackOLantern)
			{
				activeShortNames.Add("jackolantern.angry");
				activeShortNames.Add("jackolantern.happy");
			}
			if (useLantern)
				activeShortNames.Add("lantern.deployed");
			if (useFogmachine)
				activeShortNames.Add("fogmachine");
			if (useSearchLight)
				activeShortNames.Add("searchlight.deployed");
			if (useTunaCanLamp)
				activeShortNames.Add("tunalight.deployed");
			if (useFireplace)
				activeShortNames.Add("fireplace.deployed");
			if (useSkullFirepit)
				activeShortNames.Add("skull_fire_pit");
			if (useSmallOilRefinery)
				activeShortNames.Add("refinery_small_deployed");
		}

		private object GetConfig(string menu, string datavalue, object defaultValue)
		{
			var data = Config[menu] as Dictionary<string, object>;
			if (data == null)
			{
				data = new Dictionary<string, object>();
				Config[menu] = data;
				Changed = true;
			}
			object value;
			if (data.TryGetValue(datavalue, out value)) return value;
			value = defaultValue;
			data[datavalue] = value;
			Changed = true;
			return value;
		}

		#endregion
    }
}
