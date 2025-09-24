/**
 * OxidationSmelting - Smelting controller
 * Copyright (C) 2021-2022 kasvoton [kasvoton@projectoxidation.com]
 *
 * All Rights Reserved.
 * DO NOT DISTRIBUTE THIS SOFTWARE.
 *
 * You should have received a copy of the EULA along with this software.
 * If not, see <https://projectoxidation.com/license/eula.txt>.
 *
 *
 *                #################################
 *               ###  I AM AVAILABLE FOR HIRING  ###
 *                #################################
 *
 * IF YOU WANT A CUSTOM PLUGIN FOR YOUR SERVER GET INTO CONTACT WITH ME SO
 * WE CAN DISCUSS YOUR NEED IN DETAIL. I CAN BUILD PLUGINS FROM SCRATCH OR
 * MODIFY EXISTING ONES DEPENDING ON THE COMPLEXITY.
 *
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("OxidationSmelting", "kasvoton", "1.4.8")]

	public class OxidationSmelting : RustPlugin
	{
		//
		// --- UMOD EVENTS -------------------------------------------------------------------------------------------------------
		//
		private void OnServerInitialized()
		{
			DefaultSettings();

			foreach (ItemDefinition Item in ItemManager.itemList)
			{
				foreach (ItemMod mod in Item.itemMods)
				{
					ItemModCookable Cookable = mod as ItemModCookable;
					if (Cookable != null)
					{
						SettingsObject.ProductSettings Info = null;
						if (Settings.Products.TryGetValue(Item.shortname, out Info) != false)
						{
							Info.Amount = (Info.Amount < 1) ? 1 : Info.Amount;
							Info.CookTime = (Info.CookTime < 0.5f) ? 0.5f : Info.CookTime;

							Backup.Products.Add(Item.shortname, new SettingsObject.ProductSettings
							{ Amount = Cookable.amountOfBecome, CookTime = Cookable.cookTime });

							Cookable.amountOfBecome = Info.Amount;
							Cookable.cookTime = Info.CookTime;
						}
					}

					ItemModBurnable Burnable = mod as ItemModBurnable;
					if (Burnable != null && Burnable.byproductItem != null)
					{
						SettingsObject.FuelSettings Info = null;
						if (Settings.Fuel.TryGetValue(Item.shortname, out Info) != false)
						{
							Backup.Fuel.Add(Item.shortname, new SettingsObject.FuelSettings
							{
								FuelAmount = Burnable.fuelAmount,
								ByProductChance = Burnable.byproductChance,
								ByProductAmount = Burnable.byproductAmount
							});

							Burnable.fuelAmount = Info.FuelAmount;
							Burnable.byproductAmount = Info.ByProductAmount;
							Burnable.byproductChance = 1 - Info.ByProductChance;
						}
					}
				}
			};
		}

		private void Unload()
		{
			foreach (KeyValuePair<string, SettingsObject.ProductSettings> kvp in Backup.Products)
			{
				ItemModCookable Cookable = ItemManager.FindItemDefinition(kvp.Key)?.GetComponent<ItemModCookable>();
				if (Cookable == null) continue;

				Cookable.amountOfBecome = kvp.Value.Amount;
				Cookable.cookTime = kvp.Value.CookTime;
			}

			foreach (KeyValuePair<string, SettingsObject.FuelSettings> kvp in Backup.Fuel)
			{
				ItemModBurnable Burnable = ItemManager.FindItemDefinition(kvp.Key)?.GetComponent<ItemModBurnable>();
				if (Burnable == null) continue;

				Burnable.fuelAmount = kvp.Value.FuelAmount;
				Burnable.byproductAmount = kvp.Value.ByProductAmount;
				Burnable.byproductChance = kvp.Value.ByProductChance;
			}
		}

		private void OnOvenCooked(BaseOven Oven, Item Fuel, BaseEntity Slot)
		{
			ItemModBurnable Burnable = Fuel?.info.GetComponent<ItemModBurnable>();
			SettingsObject.OvenSettings Info = null;

			if (Settings.Ovens.TryGetValue(Oven?.ShortPrefabName, out Info))
			{
				for (int x = 1; x < Info.Multiplier; x++)
				{
					Oven.inventory.OnCycle(0.5f);
					if (Slot) Slot.SendMessage("Cook", 0.5f, SendMessageOptions.DontRequireReceiver);

					Fuel.fuel -= 0.5f * (Oven.cookingTemperature / 200f);
					MethodInfo consumeFuel = typeof(BaseOven).GetMethod("ConsumeFuel", BindingFlags.NonPublic | BindingFlags.Instance);
					
					if (Fuel.fuel <= 0f) consumeFuel.Invoke(Oven, new object[] { Fuel, Burnable });
				}
			}
		}

		private object OnFuelConsume(BaseOven Oven, Item Fuel, ItemModBurnable Burnable)
        {
            if (Oven == null || !Oven.OwnerID.IsSteamId() ) return null;
            
			SettingsObject.OvenSettings Info = null;
			Settings.Ovens.TryGetValue(Oven?.ShortPrefabName, out Info);
			if (Info == null || !Info.NoFuelRequired) return null;

			if (!Oven.allowByproductCreation || Burnable.byproductItem == null
					|| UnityEngine.Random.Range(0f, 1f) <= Burnable.byproductChance) return false;

			Item Byproduct = ItemManager.Create(Burnable.byproductItem, Burnable.byproductAmount, 0UL);
			if (!Byproduct.MoveToContainer(Oven.inventory, -1, true))
			{
				Oven.OvenFull();
				Byproduct.Drop(Oven.inventory.dropPosition, Oven.inventory.dropVelocity, default(Quaternion));
			}

			Fuel.fuel = Burnable.fuelAmount;
			Fuel.MarkDirty();
			return true;
        }

		//
		// --- CONFIGURATION -----------------------------------------------------------------------------------------------------
		//
		private SettingsObject Settings;

		protected override void LoadConfig()
		{
			try
			{
				base.LoadConfig();
				Settings = Config.ReadObject<SettingsObject>();
				
				if (Settings == null || Settings.Version != Settings.Revision)
					WriteWarning($"Your config file needs to be updated to v{Version}.");
			}
			catch
			{
				LoadDefaultConfig();
			}
			finally
			{
				ValidateSettings();
				SaveConfig();
			}
		}

		protected override void LoadDefaultConfig()
		{
			PrintWarning($"Created a new default v{Version} config file");
			Settings = new SettingsObject();
			Settings.Version = Settings.Revision;
		}

		protected void DefaultSettings()
		{
			if (Settings.Products.Count > 0 || Settings.Fuel.Count > 0) return;
			if (ItemManager.itemList == null || ItemManager.itemList.Count == 0) return;

			Settings.Ovens.Add("furnace", new SettingsObject.OvenSettings());
			Settings.Ovens.Add("campfire", new SettingsObject.OvenSettings());
			Settings.Ovens.Add("bbq.deployed", new SettingsObject.OvenSettings());
			Settings.Ovens.Add("furnace.large", new SettingsObject.OvenSettings());
			Settings.Ovens.Add("refinery_small_deployed", new SettingsObject.OvenSettings());

			foreach (ItemDefinition Item in ItemManager.itemList)
			{
				foreach (ItemMod mod in Item.itemMods)
				{
					ItemModCookable Cookable = mod as ItemModCookable;
					if (Cookable != null)
					{
						Settings.Products.Add(Item.shortname, new SettingsObject.ProductSettings
						{ Amount = Cookable.amountOfBecome, CookTime = Cookable.cookTime });
					}

					ItemModBurnable Burnable = mod as ItemModBurnable;
					if (Burnable != null && Burnable.byproductItem != null)
					{
						Settings.Fuel.Add(Item.shortname, new SettingsObject.FuelSettings
						{
							FuelAmount = Burnable.fuelAmount,
							ByProduct = Burnable.byproductItem.shortname,
							ByProductAmount = Burnable.byproductAmount,
							ByProductChance = 1 - Burnable.byproductChance,
						});
					}
				}
			}

			PrintWarning($"Loaded v{Version} default config values");
			SaveConfig();
		}

		protected void ValidateSettings()
		{
			foreach(KeyValuePair<string, SettingsObject.OvenSettings> kvp in Settings.Ovens)
				kvp.Value.Multiplier = kvp.Value.Multiplier.Clamp(1, 100);

			foreach(KeyValuePair<string, SettingsObject.ProductSettings> kvp in Settings.Products)
			{
				kvp.Value.Amount = kvp.Value.Amount.Clamp(1, int.MaxValue);
				kvp.Value.CookTime = kvp.Value.CookTime.Clamp(0.5f, float.MaxValue);
			}

			foreach(KeyValuePair<string, SettingsObject.FuelSettings> kvp in Settings.Fuel)
			{
				kvp.Value.ByProductChance = kvp.Value.ByProductChance.Clamp(0f, 1f);
				kvp.Value.FuelAmount = kvp.Value.FuelAmount.Clamp(10f, float.MaxValue);
				kvp.Value.ByProductAmount = kvp.Value.ByProductAmount.Clamp(0, int.MaxValue);
			}
		}

		protected override void SaveConfig()
			=> Config.WriteObject(Settings, true);

		internal class SettingsObject
		{
			[JsonIgnore]
			public readonly VersionNumber Revision = new VersionNumber {
				Major = 1, Minor = 0, Patch = 0
			};
			
			public VersionNumber Version
			{ get; set; }

			public Dictionary<string, OvenSettings> Ovens
			{ get; set; } = new Dictionary<string, OvenSettings>();

			public Dictionary<string, ProductSettings> Products
			{ get; set; } = new Dictionary<string, ProductSettings>();

			public Dictionary<string, FuelSettings> Fuel
			{ get; set; } = new Dictionary<string, FuelSettings>();

			internal class OvenSettings
			{
				public int Multiplier
				{ get; set; } = 1;

				public bool NoFuelRequired
				{ get; set; } = false;
			}

			internal class ProductSettings
			{
				public float CookTime
				{ get; set; } = 10f;

				public int Amount
				{ get; set; } = 1;
			}

			internal class FuelSettings
			{
				public float FuelAmount
				{ get; set; }

				public string ByProduct;

				public int ByProductAmount
				{ get; set; }

				public float ByProductChance
				{ get; set; }
			}
		}

		//
		// --- CACHE -------------------------------------------------------------------------------------------------------------
		//
		private Cache Backup = new Cache();

		internal class Cache
		{
			public Dictionary<string, SettingsObject.ProductSettings> Products
			{ get; set; } = new Dictionary<string, SettingsObject.ProductSettings>();

			public Dictionary<string, SettingsObject.FuelSettings> Fuel
			{ get; set; } = new Dictionary<string, SettingsObject.FuelSettings>();
		}

		//
		// --- LOGGING -----------------------------------------------------------------------------------------------------------
		//
		internal void WriteInfo(string format, params object[] args)
			=> Puts("[INFO] " + format, args);

		internal void WriteWarning(string format, params object[] args)
			=> Puts("[WARNING] " + format, args);

		internal void WriteError(string format, params object[] args)
			=> Puts("[ERROR] " + format, args);
	}
}