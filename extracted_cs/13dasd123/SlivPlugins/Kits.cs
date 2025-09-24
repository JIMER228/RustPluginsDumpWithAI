// #define TESTING

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Plugins.KitsExtensionMethods;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
	[Info("Kits", "Mevent", "1.2.16")]
	public class Kits : RustPlugin
	{
		#region Fields

		[PluginReference] private Plugin
			ImageLibrary = null,
			CopyPaste = null,
			Notify = null,
			UINotify = null,
			NoEscape = null;

		private static Kits _instance;

		private const string Layer = "UI.Kits";

		private const string InfoLayer = "UI.Kits.Info";

		private const string EditingLayer = "UI.Kits.Editing";

		private const string ModalLayer = "UI.Kits.Modal";

		private bool _enabledImageLibrary;

		private readonly Dictionary<BasePlayer, List<Kit>> _openGUI = new Dictionary<BasePlayer, List<Kit>>();

		private readonly Dictionary<ulong, Dictionary<string, object>> _kitEditing =
			new Dictionary<ulong, Dictionary<string, object>>();

		private readonly Dictionary<ulong, Dictionary<string, object>> _itemEditing =
			new Dictionary<ulong, Dictionary<string, object>>();

		private readonly Dictionary<string, List<KeyValuePair<int, string>>> _itemsCategories =
			new Dictionary<string, List<KeyValuePair<int, string>>>();

		private readonly List<BasePlayer> _toRemove = new List<BasePlayer>();

		private const string PermAdmin = "Kits.admin";

		private int _lastKitID;

		#endregion

		#region Config

		private Configuration _config;

		private class Configuration
		{
			[JsonProperty(PropertyName = "Automatic wipe on wipe")]
			public bool AutoWipe = false;

			[JsonProperty(PropertyName = "Save given kits (via the kits.givekit command) on wipe?")]
			public bool SaveGivenKitsOnWipe = false;

			[JsonProperty(PropertyName = "Default Kit Color")]
			public string KitColor = "#A0A935";

			[JsonProperty(PropertyName = "Work with Notify?")]
			public bool UseNotify = true;

			[JsonProperty(PropertyName = "Use NoEscape? (Raid/Combat block)")]
			public bool UseNoEscape = false;

			[JsonProperty(PropertyName = "Use Raid Blocked?")]
			public bool UseRaidBlock = true;

			[JsonProperty(PropertyName = "Use Combat Blocked?")]
			public bool UseCombatBlock = true;

			[JsonProperty(PropertyName = "Can admins edit? (by flag)")]
			public bool FlagAdmin = true;

			[JsonProperty(PropertyName = "Whitelist for NoEscape",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<string> NoEscapeWhiteList = new List<string>
			{
				"kit name 1",
				"kit name 2"
			};

			[JsonProperty(PropertyName = "Commands", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public string[] Commands = {"kit", "kits"};

			[JsonProperty(PropertyName = "Economy")]
			public EconomyConf Economy = new EconomyConf
			{
				Type = EconomyType.Plugin,
				AddHook = "Deposit",
				BalanceHook = "Balance",
				RemoveHook = "Withdraw",
				Plug = "Economics",
				ShortName = "scrap",
				DisplayName = string.Empty,
				Skin = 0
			};

			[JsonProperty(PropertyName = "Rarity Settings", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<RarityColor> RarityColors = new List<RarityColor>
			{
				new RarityColor(40, "#A0A935")
			};

			[JsonProperty(PropertyName = "Auto Kits", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<string> AutoKits = new List<string>
			{
				"autokit", "autokit_vip", "autokit_premium"
			};

			[JsonProperty(PropertyName = "Getting an auto kit 1 time?")]
			public bool OnceAutoKit = false;

			[JsonProperty(PropertyName = "Allow to enable/disable autokit?")]
			public bool UseChangeAutoKit = false;

			[JsonProperty(PropertyName = "Permission to enable/disable autokit")]
			public string ChangeAutoKitPermission = "kits.changeautokit";

			[JsonProperty(PropertyName = "Update the kits menu during permissions operations?")]
			public bool OnPermissionsUpdate = false;

			[JsonProperty(PropertyName = "Logs")] public LogInfo Logs = new LogInfo
			{
				Console = true,
				File = true
			};

			[JsonProperty(PropertyName = "Show Number?")]
			public bool ShowNumber = true;

			[JsonProperty(PropertyName = "Show No Permission Description?")]
			public bool ShowNoPermDescription = true;

			[JsonProperty(PropertyName = "Show All Kits?")]
			public bool ShowAllKits = false;

			[JsonProperty(PropertyName = "Show the kit when the number of uses is up?")]
			public bool ShowUsesEnd = false;

			[JsonProperty(PropertyName = "CopyPaste Parameters",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<string> CopyPasteParameters = new List<string>
			{
				"deployables", "true", "inventories", "true"
			};

			[JsonProperty(PropertyName = "Block in Building Block?")]
			public bool BlockBuilding = false;

			[JsonProperty(PropertyName = "NPC Kits", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, NpcKitsData> NpcKits = new Dictionary<string, NpcKitsData>
			{
				["1234567"] = new NpcKitsData
				{
					Description = "Free Kits",
					Kits = new List<string>
					{
						"kit_one",
						"kit_two"
					}
				},
				["7654321"] = new NpcKitsData
				{
					Description = "VIPs Kits",
					Kits = new List<string>
					{
						"kit_three",
						"kit_four"
					}
				}
			};

			[JsonProperty(PropertyName = "Description")]
			public MenuDescription Description = new MenuDescription
			{
				AnchorMin = "0 0", AnchorMax = "1 0",
				OffsetMin = "0 -55", OffsetMax = "0 -5",
				Enabled = true,
				Color = new IColor("#0E0E10", 100),
				FontSize = 18,
				Font = "robotocondensed-bold.ttf",
				Align = TextAnchor.MiddleCenter,
				TextColor = new IColor("#FFFFFF", 100),
				Description = string.Empty
			};

			[JsonProperty(PropertyName = "Info Kit Description")]
			public DescriptionSettings InfoKitDescription = new DescriptionSettings
			{
				AnchorMin = "0.5 1", AnchorMax = "0.5 1",
				OffsetMin = "-125 -55", OffsetMax = "125 -5",
				Enabled = true,
				Color = new IColor("#0E0E10", 100),
				FontSize = 18,
				Font = "robotocondensed-bold.ttf",
				Align = TextAnchor.MiddleCenter,
				TextColor = new IColor("#FFFFFF", 100)
			};

			[JsonProperty(PropertyName = "Interface")]
			public UserInterface UI = new UserInterface
			{
				Height = 455,
				Width = 640,
				KitHeight = 165,
				KitWidth = 135f,
				Margin = 10f,
				KitsOnString = 4,
				Strings = 2,
				YIndent = -100f,
				DisplayName = new DisplayNameSettings
				{
					AnchorMin = "0.5 1",
					AnchorMax = "0.5 1",
					OffsetMin = "-45 -75",
					OffsetMax = "45 0",
					Enabled = true
				},
				Image = new InterfacePosition
				{
					AnchorMin = "0.5 1",
					AnchorMax = "0.5 1",
					OffsetMin = "-32 -75",
					OffsetMax = "32 -11"
				},
				KitAvailable = new InterfacePosition
				{
					AnchorMin = "0 1",
					AnchorMax = "1 1",
					OffsetMin = "0 -100",
					OffsetMax = "0 -75"
				},
				KitAmount = new KitAmountSettings
				{
					AnchorMin = "0.5 1",
					AnchorMax = "0.5 1",
					OffsetMin = "-125",
					OffsetMax = "-120",
					Width = 115
				},
				KitCooldown = new InterfacePosition
				{
					AnchorMin = "0.5 1",
					AnchorMax = "0.5 1",
					OffsetMin = "-32.5 -125",
					OffsetMax = "32.5 -105"
				},
				KitSale = new InterfacePosition
				{
					AnchorMin = "0.5 1",
					AnchorMax = "0.5 1",
					OffsetMin = "-32.5 -115",
					OffsetMax = "32.5 -95"
				},
				KitAmountCooldown = new InterfacePosition
				{
					AnchorMin = "0 1",
					AnchorMax = "1 1",
					OffsetMin = "0 -120",
					OffsetMax = "0 -95"
				},
				NoPermission = new InterfacePosition
				{
					AnchorMin = "0 1",
					AnchorMax = "1 1",
					OffsetMin = "0 -100",
					OffsetMax = "0 -75"
				},
				CloseAfterReceive = true,
				Logo = new LogoConf
				{
					AnchorMin = "0 0.5",
					AnchorMax = "0 0.5",
					OffsetMin = "10 -20",
					OffsetMax = "50 20",
					Enabled = false,
					Image = string.Empty
				},
				ColorOne = new IColor("#161617", 100),
				ColorTwo = new IColor("#0E0E10", 100),
				ColorThree = new IColor("#4B68FF", 100),
				ColorFour = new IColor("#303030", 100),
				ColorFive = new IColor("#0E0E10", 98),
				ColorSix = new IColor("#161617", 80),
				ColorSeven = new IColor("#4B68FF", 50),
				ColorRed = new IColor("#FF4B4B", 100),
				ColorWhite = new IColor("#FFFFFF", 100)
			};

			[JsonProperty(PropertyName = "Custom Title for Kits")]
			public CustomTitles CustomTitles = new CustomTitles
			{
				Enabled = false,
				KitTitles = new Dictionary<string, CustomTitles.KitTitle>
				{
					["custom_kit"] = new CustomTitles.KitTitle
					{
						Enabled = false,
						Titles = new Dictionary<string, CustomTitles.TitleConf>
						{
							["NoPermissionDescription"] = new CustomTitles.TitleConf
							{
								Enabled = false,
								Messages = new Dictionary<string, string>
								{
									["en"] = "You don't have permission to get this kit",
									["fr"] = "Vous n'avez pas l'autorisation d'obtenir ce kit"
								}
							},
							["KitAvailable"] = new CustomTitles.TitleConf
							{
								Enabled = false,
								Messages = new Dictionary<string, string>
								{
									["en"] = "KIT AVAILABLE\nTO RECEIVE",
									["fr"] = "KIT DISPONIBLE\nPOUR RECEVOIR"
								}
							}
						}
					},
					["second_custom_kit"] = new CustomTitles.KitTitle
					{
						Enabled = false,
						Titles = new Dictionary<string, CustomTitles.TitleConf>
						{
							["NoPermissionDescription"] = new CustomTitles.TitleConf
							{
								Enabled = false,
								Messages = new Dictionary<string, string>
								{
									["en"] = "You don't have permission to get this kit",
									["fr"] = "Vous n'avez pas l'autorisation d'obtenir ce kit"
								}
							},
							["KitAvailable"] = new CustomTitles.TitleConf
							{
								Enabled = false,
								Messages = new Dictionary<string, string>
								{
									["en"] = "KIT AVAILABLE\nTO RECEIVE",
									["fr"] = "KIT DISPONIBLE\nPOUR RECEVOIR"
								}
							}
						}
					}
				}
			};

			[JsonProperty(PropertyName = "Kits hidden in the interface",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public string[] KitsHidden =
			{
				"Enter the name of the kit here",
				"Example of a string for the second kit"
			};

			public VersionNumber Version;
		}

		private class LogoConf : InterfacePosition
		{
			[JsonProperty(PropertyName = "Enabled")]
			public bool Enabled;

			[JsonProperty(PropertyName = "Image")] public string Image;
		}

		private class CustomTitles
		{
			[JsonProperty(PropertyName = "Enabled")]
			public bool Enabled;

			[JsonProperty(PropertyName = "Kit Titles (kit name – settings)",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, KitTitle> KitTitles = new Dictionary<string, KitTitle>();

			public class TitleConf
			{
				[JsonProperty(PropertyName = "Enabled")]
				public bool Enabled;

				[JsonProperty(PropertyName = "Text (language - text)",
					ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public Dictionary<string, string> Messages = new Dictionary<string, string>();

				public string GetMessage(BasePlayer player = null)
				{
					if (Messages.Count == 0)
						throw new Exception("The use of custom titles is enabled, but there are no messages!");

					var userLang = "en";
					if (player != null) userLang = _instance.lang.GetLanguage(player.UserIDString);

					string message;
					if (Messages.TryGetValue(userLang, out message))
						return message;

					if (Messages.TryGetValue("en", out message))
						return message;

					return Messages.ElementAt(0).Value;
				}
			}

			public class KitTitle
			{
				[JsonProperty(PropertyName = "Enabled")]
				public bool Enabled;

				[JsonProperty(PropertyName = "Titles (key – settings)",
					ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public Dictionary<string, TitleConf> Titles = new Dictionary<string, TitleConf>();
			}
		}

		private enum EconomyType
		{
			Plugin,
			Item
		}

		private class EconomyConf
		{
			[JsonProperty(PropertyName = "Type (Plugin/Item)")] [JsonConverter(typeof(StringEnumConverter))]
			public EconomyType Type;

			[JsonProperty(PropertyName = "Plugin name")]
			public string Plug;

			[JsonProperty(PropertyName = "Balance add hook")]
			public string AddHook;

			[JsonProperty(PropertyName = "Balance remove hook")]
			public string RemoveHook;

			[JsonProperty(PropertyName = "Balance show hook")]
			public string BalanceHook;

			[JsonProperty(PropertyName = "ShortName")]
			public string ShortName;

			[JsonProperty(PropertyName = "Display Name (empty - default)")]
			public string DisplayName;

			[JsonProperty(PropertyName = "Skin")] public ulong Skin;

			public double ShowBalance(BasePlayer player)
			{
				switch (Type)
				{
					case EconomyType.Plugin:
					{
						var plugin = _instance?.plugins?.Find(Plug);
						if (plugin == null) return 0;

						return Math.Round(Convert.ToDouble(plugin.Call(BalanceHook, player.userID)));
					}
					case EconomyType.Item:
					{
						return ItemCount(player.inventory.AllItems(), ShortName, Skin);
					}
					default:
						return 0;
				}
			}

			public void AddBalance(BasePlayer player, double amount)
			{
				switch (Type)
				{
					case EconomyType.Plugin:
					{
						var plugin = _instance?.plugins?.Find(Plug);
						if (plugin == null) return;

						switch (Plug)
						{
							case "BankSystem":
							case "ServerRewards":
							case "IQEconomic":
								plugin.Call(AddHook, player.userID, (int) amount);
								break;
							default:
								plugin.Call(AddHook, player.userID, amount);
								break;
						}

						break;
					}
					case EconomyType.Item:
					{
						var am = (int) amount;

						var item = ToItem(am);
						if (item == null) return;

						player.GiveItem(item);
						break;
					}
				}
			}

			public bool RemoveBalance(BasePlayer player, double amount)
			{
				switch (Type)
				{
					case EconomyType.Plugin:
					{
						if (ShowBalance(player) < amount) return false;

						var plugin = _instance?.plugins.Find(Plug);
						if (plugin == null) return false;

						switch (Plug)
						{
							case "BankSystem":
							case "ServerRewards":
							case "IQEconomic":
								plugin.Call(RemoveHook, player.userID, (int) amount);
								break;
							default:
								plugin.Call(RemoveHook, player.userID, amount);
								break;
						}

						return true;
					}
					case EconomyType.Item:
					{
						var playerItems = player.inventory.AllItems();
						var am = (int) amount;

						if (ItemCount(playerItems, ShortName, Skin) < am) return false;

						Take(playerItems, ShortName, Skin, am);
						return true;
					}
					default:
						return false;
				}
			}

			private static int ItemCount(Item[] items, string shortname, ulong skin)
			{
				return items.Where(item =>
						item.info.shortname == shortname && !item.isBroken && (skin == 0 || item.skin == skin))
					.Sum(item => item.amount);
			}

			private static void Take(IEnumerable<Item> itemList, string shortname, ulong skinId, int iAmount)
			{
				var num1 = 0;
				if (iAmount == 0) return;

				var list = Pool.GetList<Item>();

				foreach (var item in itemList)
				{
					if (item.info.shortname != shortname ||
					    (skinId != 0 && item.skin != skinId) || item.isBroken) continue;

					var num2 = iAmount - num1;
					if (num2 <= 0) continue;
					if (item.amount > num2)
					{
						item.MarkDirty();
						item.amount -= num2;
						break;
					}

					if (item.amount <= num2)
					{
						num1 += item.amount;
						list.Add(item);
					}

					if (num1 == iAmount)
						break;
				}

				foreach (var obj in list)
					obj.RemoveFromContainer();

				Pool.FreeList(ref list);
			}

			private Item ToItem(int amount)
			{
				var item = ItemManager.CreateByName(ShortName, amount, Skin);
				if (item == null)
				{
					Debug.LogError($"Error creating item with ShortName: '{ShortName}'");
					return null;
				}

				if (!string.IsNullOrEmpty(DisplayName)) item.name = DisplayName;

				return item;
			}
		}

		private class UserInterface
		{
			[JsonProperty(PropertyName = "Height")]
			public float Height;

			[JsonProperty(PropertyName = "Width")] public float Width;

			[JsonProperty(PropertyName = "Kit Height")]
			public float KitHeight;

			[JsonProperty(PropertyName = "Kit Width")]
			public float KitWidth;

			[JsonProperty(PropertyName = "Margin")]
			public float Margin;

			[JsonProperty(PropertyName = "Kits On String")]
			public int KitsOnString;

			[JsonProperty(PropertyName = "Strings")]
			public int Strings;

			[JsonProperty(PropertyName = "Y Indent")]
			public float YIndent;

			[JsonProperty(PropertyName = "Display Name Settings")]
			public DisplayNameSettings DisplayName;

			[JsonProperty(PropertyName = "Image Settings")]
			public InterfacePosition Image;

			[JsonProperty(PropertyName = "Kit Available Settings")]
			public InterfacePosition KitAvailable;

			[JsonProperty(PropertyName = "Kit Amount Settings")]
			public KitAmountSettings KitAmount;

			[JsonProperty(PropertyName = "Kit Cooldown Settings")]
			public InterfacePosition KitCooldown;

			[JsonProperty(PropertyName = "Kit Sale Settings")]
			public InterfacePosition KitSale;

			[JsonProperty(PropertyName = "Kit Cooldown Settings (with amount)")]
			public InterfacePosition KitAmountCooldown;

			[JsonProperty(PropertyName = "No Permission Settings")]
			public InterfacePosition NoPermission;

			[JsonProperty(PropertyName = "Close the interface after receiving a kit?")]
			public bool CloseAfterReceive;

			[JsonProperty(PropertyName = "Logo Settings")]
			public LogoConf Logo;

			[JsonProperty(PropertyName = "Color 1")]
			public IColor ColorOne;

			[JsonProperty(PropertyName = "Color 2")]
			public IColor ColorTwo;

			[JsonProperty(PropertyName = "Color 3")]
			public IColor ColorThree;

			[JsonProperty(PropertyName = "Color 4")]
			public IColor ColorFour;

			[JsonProperty(PropertyName = "Color 5")]
			public IColor ColorFive;

			[JsonProperty(PropertyName = "Color 6")]
			public IColor ColorSix;

			[JsonProperty(PropertyName = "Color 7")]
			public IColor ColorSeven;

			[JsonProperty(PropertyName = "Color Red")]
			public IColor ColorRed;

			[JsonProperty(PropertyName = "Color White")]
			public IColor ColorWhite;
		}

		private class DisplayNameSettings : InterfacePosition
		{
			[JsonProperty(PropertyName = "Enabled")]
			public bool Enabled;
		}

		private class KitAmountSettings : InterfacePosition
		{
			[JsonProperty(PropertyName = "Width")] public float Width;
		}

		private class NpcKitsData
		{
			[JsonProperty(PropertyName = "Description")]
			public string Description;

			[JsonProperty(PropertyName = "Kits", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<string> Kits;
		}

		private class InterfacePosition
		{
			public string AnchorMin;

			public string AnchorMax;

			public string OffsetMin;

			public string OffsetMax;
		}

		private class DescriptionSettings : InterfacePosition
		{
			[JsonProperty(PropertyName = "Enabled")]
			public bool Enabled;

			[JsonProperty(PropertyName = "Background Color")]
			public IColor Color;

			[JsonProperty(PropertyName = "Font Size")]
			public int FontSize;

			[JsonProperty(PropertyName = "Font")] public string Font;

			[JsonProperty(PropertyName = "Align")] [JsonConverter(typeof(StringEnumConverter))]
			public TextAnchor Align;

			[JsonProperty(PropertyName = "Text Color")]
			public IColor TextColor;

			public void Get(ref CuiElementContainer container, string parent, string name = null,
				string description = null)
			{
				if (!Enabled || string.IsNullOrEmpty(description)) return;

				if (string.IsNullOrEmpty(name))
					name = CuiHelper.GetGuid();

				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = AnchorMin, AnchorMax = AnchorMax,
						OffsetMin = OffsetMin, OffsetMax = OffsetMax
					},
					Image = {Color = Color.Get()}
				}, parent, name);

				container.Add(new CuiLabel
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
					Text =
					{
						Text = $"{description}",
						Align = Align,
						Font = Font,
						FontSize = FontSize,
						Color = TextColor.Get()
					}
				}, name);
			}
		}

		private class MenuDescription : DescriptionSettings
		{
			[JsonProperty(PropertyName = "Description")]
			public string Description;
		}

		private class IColor
		{
			[JsonProperty(PropertyName = "HEX")] public string HEX;

			[JsonProperty(PropertyName = "Opacity (0 - 100)")]
			public float Alpha;

			public string Get()
			{
				if (string.IsNullOrEmpty(HEX)) HEX = "#FFFFFF";

				var str = HEX.Trim('#');
				if (str.Length != 6) throw new Exception(HEX);
				var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
				var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
				var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);

				return $"{(double) r / 255} {(double) g / 255} {(double) b / 255} {Alpha / 100}";
			}

			public IColor(string hex, float alpha)
			{
				HEX = hex;
				Alpha = alpha;
			}
		}

		private class LogInfo
		{
			[JsonProperty(PropertyName = "To Console")]
			public bool Console;

			[JsonProperty(PropertyName = "To File")]
			public bool File;
		}

		private class RarityColor
		{
			[JsonProperty(PropertyName = "Chance")]
			public int Chance;

			[JsonProperty(PropertyName = "Color")] public string Color;

			public RarityColor(int chance, string color)
			{
				Chance = chance;
				Color = color;
			}
		}

		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				_config = Config.ReadObject<Configuration>();
				if (_config == null) throw new Exception();

				if (_config.Version < Version)
					UpdateConfigValues();

				SaveConfig();
			}
			catch
			{
				PrintError("Your configuration file contains an error. Using default configuration values.");
				LoadDefaultConfig();
			}
		}

		protected override void SaveConfig()
		{
			Config.WriteObject(_config);
		}

		protected override void LoadDefaultConfig()
		{
			_config = new Configuration();
		}

		private void UpdateConfigValues()
		{
			PrintWarning("Config update detected! Updating config values...");

			var baseConfig = new Configuration();

			if (_config.Version != default(VersionNumber))
			{
				if (_config.Version < new VersionNumber(1, 0, 25))
					_config.UI.KitSale = baseConfig.UI.KitSale;

				if (_config.Version < new VersionNumber(1, 2, 0)) StartConvertOldData();

				if (_config.Version < new VersionNumber(1, 2, 13))
				{
					_config.UI.ColorOne = new IColor(Config["Color 1"].ToString(), 100);
					_config.UI.ColorTwo = new IColor(Config["Color 2"].ToString(), 100);
					_config.UI.ColorThree = new IColor(Config["Color 3"].ToString(), 100);
					_config.UI.ColorFour = new IColor(Config["Color 4"].ToString(), 100);
					_config.UI.ColorRed = new IColor(Config["Color Red"].ToString(), 100);
					_config.UI.ColorWhite = new IColor(Config["Color White"].ToString(), 100);
				}
			}

			_config.Version = Version;
			PrintWarning("Config update completed!");
		}

		#endregion

		#region Data

		private PluginData _data;

		private List<ulong> _disablesAutoKits = new List<ulong>();

		private void SaveKits()
		{
			Interface.Oxide.DataFileSystem.WriteObject($"{Name}/Kits", _data);
		}

		private void SaveDisabledAutoKits()
		{
			if (!_config.UseChangeAutoKit) return;

			Interface.Oxide.DataFileSystem.WriteObject($"{Name}/DisabledAutoKits", _disablesAutoKits);
		}

		private void LoadData()
		{
			try
			{
				_data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>($"{Name}/Kits");

				if (_config.UseChangeAutoKit)
					_disablesAutoKits =
						Interface.Oxide.DataFileSystem.ReadObject<List<ulong>>(
							$"{Name}/DisabledAutoKits");
			}
			catch (Exception e)
			{
				PrintError(e.ToString());
			}

			if (_data == null) _data = new PluginData();
			if (_disablesAutoKits == null) _disablesAutoKits = new List<ulong>();
		}

		private class PluginData
		{
			[JsonProperty(PropertyName = "Kits", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<Kit> Kits = new List<Kit>();
		}

		private class Kit
		{
			[JsonIgnore] public int ID;

			[JsonProperty(PropertyName = "Name")] public string Name;

			[JsonProperty(PropertyName = "Display Name")]
			public string DisplayName;

			[JsonProperty(PropertyName = "Color")] public string Color;

			[JsonProperty(PropertyName = "Permission")]
			public string Permission;

			[JsonProperty(PropertyName = "Description")]
			public string Description;

			[JsonProperty(PropertyName = "Image")] public string Image;

			[JsonProperty(PropertyName = "Hide")] public bool Hide;

			[JsonProperty(PropertyName = "ShowInfo")] [DefaultValue(true)]
			public bool ShowInfo;

			[JsonProperty(PropertyName = "Amount")]
			public int Amount;

			[JsonProperty(PropertyName = "Cooldown")]
			public double Cooldown;

			[JsonProperty(PropertyName = "Wipe Block")]
			public double CooldownAfterWipe;

			[JsonProperty(PropertyName = "Use Building")]
			public bool UseBuilding;

			[JsonProperty(PropertyName = "Building")]
			public string Building;

			[JsonProperty(PropertyName = "Enable sale")]
			public bool Sale;

			[JsonProperty(PropertyName = "Selling price")]
			public int Price;

			[JsonProperty(PropertyName = "Items", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<KitItem> Items;

			[JsonProperty(PropertyName = "Use commands on receiving?")]
			public bool UseCommandsOnReceiving;

			[JsonProperty(PropertyName = "Commands on receiving (via '|')",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public string CommandsOnReceiving;

			public void Get(BasePlayer player)
			{
				Items?.ForEach(item => item?.Get(player));

				UseCommands(player);
			}

			public void UseCommands(BasePlayer player)
			{
				if (UseCommandsOnReceiving && !string.IsNullOrWhiteSpace(CommandsOnReceiving))
				{
					var command = CommandsOnReceiving.Replace("\n", "|")
						.Replace("%steamid%", player.UserIDString, StringComparison.OrdinalIgnoreCase)
						.Replace("%username%", player.displayName, StringComparison.OrdinalIgnoreCase);

					foreach (var check in command.Split('|'))
						_instance?.Server.Command(check);
				}
			}

			[JsonIgnore] public Dictionary<int, KitItem> dictMainItems = new Dictionary<int, KitItem>();

			[JsonIgnore] public Dictionary<int, KitItem> dictBeltItems = new Dictionary<int, KitItem>();

			[JsonIgnore] public Dictionary<int, KitItem> dictWearItems = new Dictionary<int, KitItem>();

			[JsonIgnore] public int beltCount;

			[JsonIgnore] public int wearCount;

			[JsonIgnore] public int mainCount;

			public void Update()
			{
				LoadContainers();

				GenerateJObject();
			}

			private void LoadContainers()
			{
				dictWearItems.Clear();
				dictBeltItems.Clear();
				dictMainItems.Clear();

				beltCount = 0;
				wearCount = 0;
				mainCount = 0;

				Items.ForEach(item =>
				{
					switch (item.Container)
					{
						case "wear":
							dictWearItems[item.Position] = item;

							wearCount++;
							break;
						case "belt":
							dictBeltItems[item.Position] = item;

							beltCount++;
							break;
						case "main":
							dictMainItems[item.Position] = item;

							mainCount++;
							break;
					}
				});
			}

			public KitItem GetItemByContainerAndPosition(string container, int position)
			{
				KitItem item;
				switch (container)
				{
					case "wear":
					{
						return dictWearItems.TryGetValue(position, out item) ? item : null;
					}
					case "belt":
					{
						return dictBeltItems.TryGetValue(position, out item) ? item : null;
					}
					default:
					{
						return dictMainItems.TryGetValue(position, out item) ? item : null;
					}
				}
			}

			#region JObject

			[JsonIgnore] private JObject _jObject;

			[JsonIgnore]
			internal JObject ToJObject
			{
				get
				{
					if (_jObject == null)
					{
						GenerateJObject();
					}

					return _jObject;
				}
			}

			private void GenerateJObject()
			{
				_jObject = new JObject
				{
					["Name"] = DisplayName,
					["Description"] = Description,
					["RequiredPermission"] = Permission,
					["MaximumUses"] = Amount,
					["Cost"] = Price,
					["IsHidden"] = Hide,
					["CopyPasteFile"] = Building,
					["KitImage"] = Image,
					["MainItems"] = new JArray(),
					["WearItems"] = new JArray(),
					["BeltItems"] = new JArray()
				};

				foreach (var kitItem in dictMainItems.Values)
					(_jObject["MainItems"] as JArray).Add(kitItem.ToJObject);

				foreach (var kitItem in dictWearItems.Values)
					(_jObject["WearItems"] as JArray).Add(kitItem.ToJObject);

				foreach (var kitItem in dictBeltItems.Values)
					(_jObject["BeltItems"] as JArray).Add(kitItem.ToJObject);
			}

			#endregion
		}

		private enum KitItemType
		{
			Item,
			Command
		}

		private class KitItem
		{
			[JsonProperty(PropertyName = "Type")] [JsonConverter(typeof(StringEnumConverter))]
			public KitItemType Type;

			[JsonProperty(PropertyName = "Command")]
			public string Command;

			[JsonProperty(PropertyName = "ShortName")]
			public string ShortName;

			[JsonProperty(PropertyName = "DisplayName")]
			public string DisplayName;

			[JsonProperty(PropertyName = "Amount")]
			public int Amount;

			[JsonProperty(PropertyName = "Blueprint")]
			public int Blueprint;

			[JsonProperty(PropertyName = "SkinID")]
			public ulong SkinID;

			[JsonProperty(PropertyName = "Container")]
			public string Container;

			[JsonProperty(PropertyName = "Condition")]
			public float Condition;

			[JsonProperty(PropertyName = "Chance")]
			public int Chance;

			[JsonProperty(PropertyName = "Position", DefaultValueHandling = DefaultValueHandling.Populate)]
			[DefaultValue(-1)]
			public int Position;

			[JsonProperty(PropertyName = "Image")] public string Image;

			[JsonProperty(PropertyName = "Weapon")]
			public Weapon Weapon;

			[JsonProperty(PropertyName = "Content", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<ItemContent> Content;

			[JsonProperty(PropertyName = "Text")] public string Text;

			public void Get(BasePlayer player)
			{
				if (Chance < 100 && Random.Range(0, 100) > Chance) return;

				switch (Type)
				{
					case KitItemType.Item:
					{
						GiveItem(player, BuildItem(),
							Container == "belt" ? player.inventory.containerBelt :
							Container == "wear" ? player.inventory.containerWear : player.inventory.containerMain);
						break;
					}
					case KitItemType.Command:
					{
						ToCommand(player);
						break;
					}
				}
			}

			private void ToCommand(BasePlayer player)
			{
				var command = Command.Replace("\n", "|")
					.Replace("%steamid%", player.UserIDString, StringComparison.OrdinalIgnoreCase).Replace("%username%",
						player.displayName, StringComparison.OrdinalIgnoreCase);

				foreach (var check in command.Split('|')) _instance?.Server.Command(check);
			}

			private static void GiveItem(BasePlayer player, Item item, ItemContainer cont = null)
			{
				if (item == null) return;
				var inv = player.inventory;

				var moved = item.MoveToContainer(cont, item.position) || item.MoveToContainer(cont) ||
				            item.MoveToContainer(inv.containerMain);
				if (!moved)
				{
					if (cont == inv.containerBelt)
						moved = item.MoveToContainer(inv.containerWear);
					if (cont == inv.containerWear)
						moved = item.MoveToContainer(inv.containerBelt);
				}

				if (!moved)
					item.Drop(player.GetCenter(), player.GetDropVelocity());
			}

			[JsonIgnore] private ItemDefinition _itemDefinition;

			[JsonIgnore]
			public ItemDefinition ItemDefinition
			{
				get
				{
					if (_itemDefinition == null) _itemDefinition = ItemManager.FindItemDefinition(ShortName);

					return _itemDefinition;
				}
			}

			private Item BuildItem()
			{
				var item = ItemManager.Create(ItemDefinition, Mathf.Max(Amount, 1), SkinID);
				item.condition = Condition;

				item.position = Position;

				if (Blueprint != 0)
					item.blueprintTarget = Blueprint;

				if (!string.IsNullOrEmpty(DisplayName))
					item.name = DisplayName;

				if (!string.IsNullOrEmpty(Text))
					item.text = Text;

				if (Weapon != null)
				{
					var heldEntity = item.GetHeldEntity();
					if (heldEntity != null)
					{
						heldEntity.skinID = SkinID;

						var baseProjectile = heldEntity as BaseProjectile;
						if (baseProjectile != null && !string.IsNullOrEmpty(Weapon.ammoType))
						{
							baseProjectile.primaryMagazine.contents = Weapon.ammoAmount;
							baseProjectile.primaryMagazine.ammoType =
								ItemManager.FindItemDefinition(Weapon.ammoType);
						}

						heldEntity.SendNetworkUpdate();
					}
				}

				Content?.ForEach(cont =>
				{
					var newCont = ItemManager.CreateByName(cont.ShortName, cont.Amount);
					newCont.condition = cont.Condition;
					newCont.MoveToContainer(item.contents);
				});

				return item;
			}

			public static KitItem FromOld(ItemData item, string container)
			{
				var newItem = new KitItem
				{
					Content =
						item.Contents?.Select(x =>
								new ItemContent {ShortName = x.Shortname, Condition = x.Condition, Amount = x.Amount})
							.ToList() ?? new List<ItemContent>(),
					Weapon = new Weapon {ammoAmount = item.Ammo, ammoType = item.Ammotype},
					Container = container,
					SkinID = item.Skin,
					Command = string.Empty,
					Chance = 100,
					Blueprint = string.IsNullOrEmpty(item.BlueprintShortname) ? 0 : 1,
					Condition = item.Condition,
					Amount = item.Amount,
					ShortName = item.Shortname,
					Type = KitItemType.Item,
					Position = item.Position
				};

				return newItem;
			}

			[JsonIgnore] private int _itemId = -1;

			[JsonIgnore]
			public int itemId
			{
				get
				{
					if (_itemId == -1)
						UpdateItemID();
					return _itemId;
				}
			}

			[JsonIgnore] private ICuiComponent _image;

			public CuiElement GetImage(string aMin, string aMax, string oMin, string oMax, string parent,
				string name = null)
			{
				if (_image == null)
					GenerateNewImage();

				return new CuiElement
				{
					Name = string.IsNullOrEmpty(name) ? CuiHelper.GetGuid() : name,
					Parent = parent,
					Components =
					{
						_image,
						new CuiRectTransformComponent
						{
							AnchorMin = aMin, AnchorMax = aMax,
							OffsetMin = oMin, OffsetMax = oMax
						}
					}
				};
			}

			private void GenerateNewImage()
			{
				if (_instance._enabledImageLibrary && !string.IsNullOrEmpty(Image))
					_image = new CuiRawImageComponent
					{
						Png = _instance.ImageLibrary.Call<string>("GetImage", Image)
					};
				else
					_image = new CuiImageComponent
					{
						ItemId = itemId,
						SkinId = SkinID
					};
			}

			private void UpdateItemID()
			{
				_itemId = ItemManager.FindItemDefinition(ShortName)?.itemid ?? -1;
			}

			public void Update()
			{
				UpdateItemID();

				GenerateNewImage();

				GenerateJObject();
			}

			#region JObject

			[JsonIgnore] private JObject _jObject;

			[JsonIgnore]
			public JObject ToJObject
			{
				get
				{
					if (_jObject == null)
					{
						GenerateJObject();
					}

					return _jObject;
				}
			}

			private void GenerateJObject()
			{
				_jObject = new JObject
				{
					["Shortname"] = ShortName,
					["DisplayName"] = DisplayName,
					["SkinID"] = SkinID,
					["Amount"] = Amount,
					["Condition"] = Condition,
					["MaxCondition"] = Condition,
					["IsBlueprint"] = Blueprint != 0,
					["Ammo"] = Weapon?.ammoAmount,
					["AmmoType"] = Weapon?.ammoType,
					["Text"] = Text,
					["Contents"] = new JArray()
				};

				Content?.ForEach(x =>
				{
					(_jObject["Contents"] as JArray)?.Add(new JObject
					{
						["Shortname"] = x.ShortName,
						["DisplayName"] = string.Empty,
						["SkinID"] = 0,
						["Amount"] = x.Amount,
						["Condition"] = x.Condition,
						["MaxCondition"] = x.Condition,
						["IsBlueprint"] = false,
						["Ammo"] = 0,
						["AmmoType"] = string.Empty,
						["Text"] = string.Empty,
						["Contents"] = new JArray()
					});
				});
			}

			#endregion
		}

		private class Weapon
		{
			public string ammoType;

			public int ammoAmount;
		}

		private class ItemContent
		{
			public string ShortName;

			public float Condition;

			public int Amount;
		}

		#endregion

		#region Hooks

		private void Init()
		{
			_instance = this;

			LoadData();

			if (!_config.OnPermissionsUpdate)
			{
				Unsubscribe(nameof(OnUserPermissionGranted));
				Unsubscribe(nameof(OnUserPermissionRevoked));
			}
		}

		private void OnServerInitialized()
		{
			LoadImages();

			LoadKits();

			FixItemsPositions();

			FillCategories();

			RegisterPermissions();

			RegisterCommands();

			timer.Every(1, HandleUi);
		}

		private void OnServerSave()
		{
			timer.In(Random.Range(2, 7), PlayerData.Save);
		}

		private void Unload()
		{
			if (_wipePlayers != null)
				ServerMgr.Instance.StopCoroutine(_wipePlayers);

			foreach (var player in BasePlayer.activePlayerList)
			{
				CuiHelper.DestroyUi(player, Layer);
				CuiHelper.DestroyUi(player, InfoLayer);
				CuiHelper.DestroyUi(player, EditingLayer);
				CuiHelper.DestroyUi(player, ModalLayer);

				PlayerData.SaveAndUnload(player.UserIDString);
			}

			_instance = null;
		}

		#region Wipe

		private void OnNewSave(string filename)
		{
			if (!_config.AutoWipe) return;

			DoWipePlayers();
		}

		#endregion

		private void OnPlayerRespawned(BasePlayer player)
		{
			if (player == null || (_config.UseChangeAutoKit && _disablesAutoKits.Contains(player.userID))) return;

			var kits = GetAutoKits(player);
			if (kits.Count == 0)
				return;

			player.inventory.Strip();

			if (_config.OnceAutoKit)
				kits.LastOrDefault()?.Get(player);
			else
				kits.ForEach(kit => kit.Get(player));
		}

		private void OnPlayerDisconnected(BasePlayer player)
		{
			_openGUI.Remove(player);
			_kitEditing.Remove(player.userID);
			_itemEditing.Remove(player.userID);

			PlayerData.SaveAndUnload(player.UserIDString);
		}

		private void OnPlayerDeath(BasePlayer player, HitInfo info)
		{
			if (player == null) return;

			CuiHelper.DestroyUi(player, Layer);

			OnPlayerDisconnected(player);
		}

		private void OnUseNPC(BasePlayer npc, BasePlayer player)
		{
			if (npc == null || player == null || !_config.NpcKits.ContainsKey(npc.UserIDString)) return;

			MainUi(player, npc.userID, first: true);
		}

		#region Image Library

		private void OnPluginLoaded(Plugin plugin)
		{
			if (plugin.Name == "ImageLibrary") _enabledImageLibrary = true;
		}

		private void OnPluginUnloaded(Plugin plugin)
		{
			if (plugin.Name == "ImageLibrary") _enabledImageLibrary = false;
		}

		#endregion

		#region Permissions

		private void OnUserPermissionGranted(string id, string permName)
		{
			UpdateOpenedUI(id, permName);
		}

		private void OnUserPermissionRevoked(string id, string permName)
		{
			UpdateOpenedUI(id, permName);
		}

		#endregion

		#endregion

		#region Commands

		private void CmdOpenKits(IPlayer cov, string command, string[] args)
		{
			var player = cov?.Object as BasePlayer;
			if (player == null) return;

			if (_enabledImageLibrary == false)
			{
				SendNotify(player, NoILError, 1);

				BroadcastILNotInstalled();
				return;
			}

			if (args.Length == 0)
			{
				MainUi(player, first: true);
				return;
			}

			switch (args[0])
			{
				case "help":
				{
					Reply(player, KitsHelp, command);
					break;
				}

				case "list":
				{
					Reply(player, KitsList,
						string.Join(", ", GetAvailableKits(player).Select(x => $"'{x.DisplayName}'")));
					break;
				}

				case "remove":
				{
					if (!IsAdmin(player)) return;

					var name = string.Join(" ", args.Skip(1));
					if (string.IsNullOrEmpty(name))
					{
						SendNotify(player, KitNotFound, 1, name);
						return;
					}

					var kit = GetAvailableKits(player)?.Find(x => x.DisplayName == name);
					if (kit == null)
					{
						SendNotify(player, KitNotFound, 1, name);
						return;
					}

					_data.Kits.Remove(kit);
					SaveKits();

					SendNotify(player, KitRemoved, 0, name);
					break;
				}

				case "autokit":
				{
					if (!_config.UseChangeAutoKit) return;

					if (!string.IsNullOrEmpty(_config.ChangeAutoKitPermission) &&
					    !cov.HasPermission(_config.ChangeAutoKitPermission))
					{
						ErrorUi(player, Msg(player, NoPermission));
						return;
					}

					bool enabled;
					if (_disablesAutoKits.Contains(player.userID))
					{
						_disablesAutoKits.Remove(player.userID);

						enabled = true;
					}
					else
					{
						_disablesAutoKits.Add(player.userID);

						enabled = false;
					}

					if (enabled)
						SendNotify(player, ChangeAutoKitOn, 0);
					else
						SendNotify(player, ChangeAutoKitOff, 1);

					SaveDisabledAutoKits();
					break;
				}

				default:
				{
					var name = string.Join(" ", args);
					if (string.IsNullOrEmpty(name))
					{
						SendNotify(player, KitNotFound, 1, name);
						return;
					}

					var kit = GetAvailableKits(player, checkAmount: false)
						.Find(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase) ||
						           string.Equals(x.DisplayName, name, StringComparison.InvariantCultureIgnoreCase));
					if (kit == null)
					{
						SendNotify(player, KitNotFound, 1, name);
						return;
					}

					GiveKitToPlayer(player, kit, chat: true);
					break;
				}
			}
		}

		[ConsoleCommand("UI_Kits")]
		private void CmdKitsConsole(ConsoleSystem.Arg arg)
		{
			var player = arg?.Player();
			if (player == null || !arg.HasArgs()) return;

			switch (arg.Args[0])
			{
				case "close":
				{
					_openGUI.Remove(player);

					StopEditing(player);
					break;
				}

				case "stopedit":
				{
					StopEditing(player);
					break;
				}

				case "main":
				{
					var targetId = 0UL;
					if (arg.HasArgs(2))
						ulong.TryParse(arg.Args[1], out targetId);

					var page = 0;
					if (arg.HasArgs(3))
						int.TryParse(arg.Args[2], out page);

					var showAll = false;
					if (arg.HasArgs(4))
						bool.TryParse(arg.Args[3], out showAll);

					MainUi(player, targetId, page, showAll);
					break;
				}

				case "infokit":
				{
					int page, kitId;
					if (!arg.HasArgs(3) || !int.TryParse(arg.Args[1], out page) ||
					    !int.TryParse(arg.Args[2], out kitId)) return;

					var kit = FindKitByID(kitId);
					if (kit == null) return;

					StopEditing(player);

					InfoKitUi(player, page, kit);
					break;
				}

				case "givekit":
				{
					int kitId, page;
					ulong targetId;
					bool showAll;
					if (!arg.HasArgs(5) ||
					    !int.TryParse(arg.Args[1], out kitId) ||
					    !ulong.TryParse(arg.Args[2], out targetId) ||
					    !int.TryParse(arg.Args[3], out page) ||
					    !bool.TryParse(arg.Args[4], out showAll)) return;

					var kit = FindKitByID(kitId);
					if (kit == null) return;

					GiveKitToPlayer(player, kit, targetId: targetId, page: page, showAll: showAll);
					break;
				}

				case "editkit":
				{
					if (!IsAdmin(player)) return;

					int page;
					if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out page)) return;

					bool creating;
					if (!arg.HasArgs(3) || !bool.TryParse(arg.Args[2], out creating)) return;

					var kitId = -1;
					if (arg.HasArgs(4))
						int.TryParse(arg.Args[3], out kitId);

					if (arg.HasArgs(5) && (!arg.HasArgs(6) || string.IsNullOrEmpty(arg.Args[5])))
						return;

					if (arg.HasArgs(6))
					{
						var key = arg.Args[4];
						var value = arg.Args[5];

						if (_kitEditing.ContainsKey(player.userID) && _kitEditing[player.userID].ContainsKey(key))
						{
							object newValue = null;

							switch (key)
							{
								case "ShowInfo":
								case "Hide":
								case "AutoKit":
								case "Sale":
								{
									bool result;
									if (value == "delete")
										newValue = default(bool);
									else if (bool.TryParse(value, out result)) newValue = result;
									break;
								}
								case "Amount":
								case "Price":
								{
									int result;
									if (value == "delete")
										newValue = default(int);
									else if (int.TryParse(value, out result))
										newValue = result;
									break;
								}
								case "Cooldown":
								case "CooldownAfterWipe":
								{
									double result;
									if (value == "delete")
										newValue = default(double);
									else if (double.TryParse(value, out result))
										newValue = result;
									break;
								}
								case "Description":
								case "DisplayName":
								{
									newValue = value == "delete" ? string.Empty : string.Join(" ", arg.Args.Skip(5));
									break;
								}
								default:
								{
									newValue = value == "delete" ? string.Empty : value;
									break;
								}
							}

							if (_kitEditing[player.userID][key] != null &&
							    _kitEditing[player.userID][key].Equals(newValue))
								return;

							_kitEditing[player.userID][key] = newValue;
						}
					}

					EditingKitUi(player, page, creating, kitId);
					break;
				}

				case "takeitem":
				{
					if (!IsAdmin(player)) return;

					int page, kitId, slot;
					if (!arg.HasArgs(6) ||
					    !_itemEditing.ContainsKey(player.userID) ||
					    !int.TryParse(arg.Args[1], out page) ||
					    !int.TryParse(arg.Args[3], out kitId) ||
					    !int.TryParse(arg.Args[4], out slot))
						return;

					var container = arg.Args[2];

					_itemEditing[player.userID]["ShortName"] = arg.Args[5];
					_itemEditing[player.userID]["SkinID"] = 0UL;

					EditingItemUi(player, page, kitId, slot, container);
					break;
				}

				case "selectitem":
				{
					if (!IsAdmin(player)) return;

					int kitId, slot;
					if (!arg.HasArgs(4) ||
					    !_itemEditing.ContainsKey(player.userID) ||
					    !int.TryParse(arg.Args[2], out kitId) ||
					    !int.TryParse(arg.Args[3], out slot))
						return;

					var container = arg.Args[1];

					var selectedCategory = string.Empty;
					if (arg.HasArgs(5))
						selectedCategory = arg.Args[4];

					var page = 0;
					if (arg.HasArgs(6))
						int.TryParse(arg.Args[5], out page);

					var input = string.Empty;
					if (arg.HasArgs(7))
						input = string.Join(" ", arg.Args.Skip(6));

					SelectItem(player, kitId, slot, container, selectedCategory, page, input);
					break;
				}

				case "startedititem":
				{
					if (!IsAdmin(player)) return;

					int page, kitId, slot;
					if (!arg.HasArgs(5) ||
					    !int.TryParse(arg.Args[1], out page) ||
					    !int.TryParse(arg.Args[3], out kitId) ||
					    !int.TryParse(arg.Args[4], out slot)) return;

					var container = arg.Args[2];

					EditingItemUi(player, page, kitId, slot, container, true);
					break;
				}

				case "edititem":
				{
					if (!IsAdmin(player)) return;

					int page, kitId, slot;
					if (!arg.HasArgs(7) ||
					    !int.TryParse(arg.Args[1], out page) ||
					    !int.TryParse(arg.Args[3], out kitId) ||
					    !int.TryParse(arg.Args[4], out slot)) return;

					var container = arg.Args[2];

					var key = arg.Args[5];
					var value = arg.Args[6];

					if (_itemEditing.ContainsKey(player.userID) && _itemEditing[player.userID].ContainsKey(key))
					{
						object newValue = null;

						switch (key)
						{
							case "Type":
							{
								KitItemType type;
								if (value == "delete")
									newValue = default(KitItemType);
								else if (Enum.TryParse(value, out type))
									newValue = type;
								break;
							}
							case "Command":
							{
								newValue = value == "delete" ? string.Empty : string.Join(" ", arg.Args.Skip(6));
								break;
							}
							case "DisplayName":
							{
								newValue = value == "delete" ? string.Empty : string.Join(" ", arg.Args.Skip(6));
								break;
							}
							case "ShortName":
							{
								if (value == "delete")
								{
									newValue = string.Empty;
								}
								else
								{
									newValue = value;
									_itemEditing[player.userID]["SkinID"] = 0UL;
								}

								break;
							}
							case "SkinID":
							{
								ulong result;
								if (value == "delete")
									newValue = default(ulong);
								else if (ulong.TryParse(value, out result))
									newValue = result;
								break;
							}
							case "Amount":
							case "Blueprint":
							case "Chance":
							{
								int result;
								if (value == "delete")
									newValue = default(int);
								else if (int.TryParse(value, out result))
									newValue = result;
								break;
							}
						}

						if (_itemEditing[player.userID][key]?.Equals(newValue) == true)
							return;

						_itemEditing[player.userID][key] = newValue;
					}

					EditingItemUi(player, page, kitId, slot, container);
					break;
				}

				case "saveitem":
				{
					if (!IsAdmin(player)) return;

					int page, kitId, slot;
					if (!arg.HasArgs(5) ||
					    !int.TryParse(arg.Args[1], out page) ||
					    !int.TryParse(arg.Args[2], out kitId) ||
					    !int.TryParse(arg.Args[3], out slot)) return;

					var container = arg.Args[4];
					if (string.IsNullOrEmpty(container)) return;

					var editing = _itemEditing[player.userID];
					if (editing == null) return;

					var kit = FindKitByID(kitId);
					if (kit == null) return;

					var item = kit.GetItemByContainerAndPosition(container, slot);
					var hasItem = item != null;
					var newItem = item == null || editing["ShortName"].ToString() != item.ShortName;

					if (item == null)
						item = new KitItem();

					item.Type = (KitItemType) editing["Type"];
					item.Command = editing["Command"].ToString();
					item.Container = editing["Container"].ToString();
					item.ShortName = editing["ShortName"].ToString();
					item.DisplayName = editing["DisplayName"].ToString();
					item.Amount = (int) editing["Amount"];
					item.Blueprint = (int) editing["Blueprint"];
					item.Chance = (int) editing["Chance"];
					item.SkinID = (ulong) editing["SkinID"];
					item.Position = (int) editing["Position"];

					if (newItem)
					{
						var info = ItemManager.FindItemDefinition(item.ShortName);
						if (info != null)
							item.Condition = info.condition.max;
					}

					if (!hasItem) kit.Items.Add(item);

					item.Update();

					kit.Update();

					StopEditing(player);

					SaveKits();

					InfoKitUi(player, page, kit);
					break;
				}

				case "removeitem":
				{
					if (!IsAdmin(player)) return;

					int page, kitId, slot;
					if (!arg.HasArgs(5) ||
					    !int.TryParse(arg.Args[1], out page) ||
					    !int.TryParse(arg.Args[2], out kitId) ||
					    !int.TryParse(arg.Args[3], out slot)) return;

					var editing = _itemEditing[player.userID];
					if (editing == null) return;

					var kit = FindKitByID(kitId);
					if (kit == null) return;

					var item = kit.GetItemByContainerAndPosition(arg.Args[4], slot);
					if (item != null)
						kit.Items.Remove(item);

					kit.Update();

					StopEditing(player);

					SaveKits();

					InfoKitUi(player, page, kit);
					break;
				}

				case "savekit":
				{
					if (!IsAdmin(player)) return;

					bool creating;
					int page, kitId;
					if (!arg.HasArgs(4) ||
					    !int.TryParse(arg.Args[1], out page) ||
					    !bool.TryParse(arg.Args[2], out creating) ||
					    !int.TryParse(arg.Args[3], out kitId)) return;

					var editing = _kitEditing[player.userID];
					if (editing == null) return;

					Kit kit;
					if (creating)
					{
						kit = new Kit
						{
							ID = ++_lastKitID,
							Name = (string) editing["Name"],
							DisplayName = (string) editing["DisplayName"],
							Description = (string) editing["Description"],
							Color = (string) editing["Color"],
							Permission = (string) editing["Permission"],
							Image = (string) editing["Image"],
							Hide = Convert.ToBoolean(editing["Hide"]),
							ShowInfo = Convert.ToBoolean(editing["ShowInfo"]),
							Amount = Convert.ToInt32(editing["Amount"]),
							Cooldown = Convert.ToDouble(editing["Cooldown"]),
							CooldownAfterWipe = Convert.ToDouble(editing["CooldownAfterWipe"]),
							Sale = Convert.ToBoolean(editing["Sale"]),
							Price = Convert.ToInt32(editing["Price"]),
							Items = new List<KitItem>(),
							UseCommandsOnReceiving = false,
							CommandsOnReceiving = string.Empty
						};
						
						_data.Kits.Add(kit);
						
						var kitIndex = _data.Kits.IndexOf(kit);
				
						_kitByName[kit.Name] = kitIndex;
						_kitByID[kit.ID] = kitIndex;
					}
					else
					{
						kit = FindKitByID(kitId);
						if (kit == null) return;

						var oldName = kit.Name;
						
						kit.Name = (string) editing["Name"];
						kit.DisplayName = (string) editing["DisplayName"];
						kit.Description = (string) editing["Description"];
						kit.Color = (string) editing["Color"];
						kit.Permission = (string) editing["Permission"];
						kit.Image = (string) editing["Image"];
						kit.Hide = Convert.ToBoolean(editing["Hide"]);
						kit.ShowInfo = Convert.ToBoolean(editing["ShowInfo"]);
						kit.Amount = Convert.ToInt32(editing["Amount"]);
						kit.Cooldown = Convert.ToDouble(editing["Cooldown"]);
						kit.CooldownAfterWipe = Convert.ToDouble(editing["CooldownAfterWipe"]);
						kit.Sale = Convert.ToBoolean(editing["Sale"]);
						kit.Price = Convert.ToInt32(editing["Price"]);

						if (oldName != kit.Name)
						{
							_kitByName.Remove(oldName);
							
							_kitByName[kit.Name] = _data.Kits.IndexOf(kit);
						}
					}

					var autoKit = Convert.ToBoolean(editing["AutoKit"]);
					if (autoKit)
					{
						if (!_config.AutoKits.Contains(kit.Name))
						{
							_config.AutoKits.Add(kit.Name);
							SaveConfig();
						}
					}
					else
					{
						_config.AutoKits.Remove(kit.Name);
						SaveConfig();
					}

					StopEditing(player);

					var perm = kit.Permission.ToLower();
					if (!string.IsNullOrEmpty(perm) && !permission.PermissionExists(perm))
						permission.RegisterPermission(perm, this);

					if (!string.IsNullOrEmpty(kit.Image))
						ImageLibrary?.Call("AddImage", kit.Image, kit.Image);

					SaveKits();

					MainUi(player, page: page);
					break;
				}

				case "removekit":
				{
					if (!IsAdmin(player)) return;

					int page, kitId;
					if (!arg.HasArgs(3) ||
					    !int.TryParse(arg.Args[1], out page) ||
					    !int.TryParse(arg.Args[2], out kitId)) return;

					var kit = FindKitByID(kitId);
					if (kit == null) return;

					_kitByName.Remove(kit.Name);
					_kitByID.Remove(kit.ID);
					_data.Kits.Remove(kit);
					
					SaveKits();

					MainUi(player, page: page);
					break;
				}

				case "frominv":
				{
					if (!IsAdmin(player)) return;

					int page, kitId;
					if (!arg.HasArgs(3) ||
					    !int.TryParse(arg.Args[1], out page) ||
					    !int.TryParse(arg.Args[2], out kitId)) return;

					var kit = FindKitByID(kitId);
					if (kit == null) return;

					var kitItems = GetPlayerItems(player);
					if (kitItems == null) return;

					kit.Items = kitItems;

					kit.Update();

					SaveKits();

					InfoKitUi(player, page, kit);
					break;
				}
			}
		}

		[ConsoleCommand("kits.resetkits")]
		private void CmdKitsReset(ConsoleSystem.Arg arg)
		{
			if (!(arg.IsServerside || arg.IsAdmin)) return;

			_data.Kits.Clear();

			SaveKits();

			SendReply(arg, "Plugin successfully reset");
		}

		[ConsoleCommand("kits.wipe")]
		private void CmdKitsWipe(ConsoleSystem.Arg arg)
		{
			if (!(arg.IsServerside || arg.IsAdmin)) return;

			DoWipePlayers();

			SendReply(arg, "Players data successfully wiped");
		}

		[ConsoleCommand("kits.give")]
		private void CmdKitsGive(ConsoleSystem.Arg arg)
		{
			if (!(arg.IsServerside || arg.IsAdmin)) return;

			if (!arg.HasArgs(2))
			{
				SendReply(arg, $"Error syntax! Use: {arg.cmd.FullName} [name/steamid] [kitname]");
				return;
			}

			var target = BasePlayer.Find(arg.Args[0]);

			if (target == null)
			{
				SendReply(arg, $"Player '{arg.Args[0]}' not found!");
				return;
			}

			var kit = FindKitByName(arg.Args[1]);
			if (kit == null)
			{
				SendReply(arg, $"Kit '{arg.Args[1]}' not found!");
				return;
			}

			kit.Items.ForEach(item => item.Get(target));

			SendReply(arg, $"Player '{arg.Args[0]}' successfully received a kit '{arg.Args[1]}'");

			Interface.CallHook("OnKitRedeemed", target, kit.Name);
			Log(target, kit.Name);
		}

		[ConsoleCommand("kits.givekit")]
		private void CmdKitsGiveKit(ConsoleSystem.Arg arg)
		{
			if (!(arg.IsServerside || arg.IsAdmin)) return;

			if (!arg.HasArgs(2))
			{
				SendReply(arg, $"Error syntax! Use: {arg.cmd.FullName} [name/steamid] [kitname] [amount]");
				return;
			}

			var steamIdOrName = arg.Args[0];

			var target = BasePlayer.FindAwakeOrSleeping(steamIdOrName);
			if (target == null && !steamIdOrName.IsSteamId())
			{
				SendReply(arg, $"Player '{steamIdOrName}' not found!");
				return;
			}

			var kit = FindKitByName(arg.Args[1]);
			if (kit == null)
			{
				SendReply(arg, $"Kit '{arg.Args[1]}' not found!");
				return;
			}

			var amount = 1;
			if (arg.HasArgs(3))
				int.TryParse(arg.Args[2], out amount);

			var playerData = PlayerData.GetOrCreateKitData(steamIdOrName, kit.Name);
			if (playerData == null) return;

			playerData.HasAmount += amount;

			SendReply(arg, $"Player '{steamIdOrName}' successfully received a kit '{arg.Args[1]}' ({amount} pcs)");

			Log(target, kit.Name);
		}

		#endregion

		#region Interface

		private const ulong DEFAULT_MAIN_TARGETID = 0;
		private const int DEFAULT_MAIN_PAGE = 0;
		private const bool DEFAULT_MAIN_SHOWALL = false;

		private void MainUi(BasePlayer player,
			ulong targetId = DEFAULT_MAIN_TARGETID,
			int page = DEFAULT_MAIN_PAGE,
			bool showAll = DEFAULT_MAIN_SHOWALL,
			bool first = false)
		{
			#region Fields

			var isAdmin = IsAdmin(player);

			var totalAmount = _config.UI.KitsOnString * _config.UI.Strings;

			var constSwitch = -(_config.UI.KitsOnString * _config.UI.KitWidth +
			                    (_config.UI.KitsOnString - 1) * _config.UI.Margin) / 2f;

			var xSwitch = constSwitch;
			var ySwitch = _config.UI.YIndent;

			var allKits = GetAvailableKits(player, targetId.ToString(), showAll, gui: true);

			// Fix zero-page
			if (allKits.Count > page * totalAmount == false)
				page = Mathf.Max(0, page - 1);

			var kitsList = allKits.SkipAndTake(page * totalAmount, totalAmount);

			_openGUI[player] = kitsList;

			#endregion

			var container = new CuiElementContainer();

			#region Background

			if (first)
			{
				CuiHelper.DestroyUi(player, Layer);

				container.Add(new CuiPanel
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
					Image =
					{
						Color = "0 0 0 0.9",
						Material = "assets/content/ui/uibackgroundblur.mat"
					},
					CursorEnabled = true
				}, "Overlay", Layer);

				container.Add(new CuiButton
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
					Text = {Text = ""},
					Button =
					{
						Color = "0 0 0 0",
						Close = Layer,
						Command = "UI_Kits close"
					}
				}, Layer);
			}

			#endregion

			#region Main

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
					OffsetMin = $"-{_config.UI.Width / 2f} -{_config.UI.Height / 2f}",
					OffsetMax = $"{_config.UI.Width / 2f} {_config.UI.Height / 2f}"
				},
				Image =
				{
					Color = _config.UI.ColorTwo.Get()
				}
			}, Layer, Layer + ".Main");

			#region Header

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "1 1",
					OffsetMin = "0 -50",
					OffsetMax = "0 0"
				},
				Image = {Color = _config.UI.ColorOne.Get()}
			}, Layer + ".Main", Layer + ".Header");

			if (_config.UI.Logo.Enabled && !string.IsNullOrEmpty(_config.UI.Logo.Image))
				container.Add(new CuiElement
				{
					Parent = Layer + ".Header",
					Components =
					{
						new CuiRawImageComponent
						{
							Png = ImageLibrary.Call<string>("GetImage", _config.UI.Logo.Image)
						},
						new CuiRectTransformComponent
						{
							AnchorMin = _config.UI.Logo.AnchorMin,
							AnchorMax = _config.UI.Logo.AnchorMax,
							OffsetMin = _config.UI.Logo.OffsetMin,
							OffsetMax = _config.UI.Logo.OffsetMax
						}
					}
				});

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 0", AnchorMax = "1 1",
					OffsetMin = "10 0",
					OffsetMax = "0 0"
				},
				Text =
				{
					Text = Msg(player, MainTitle),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-bold.ttf",
					FontSize = 14,
					Color = _config.UI.ColorWhite.Get()
				}
			}, Layer + ".Header");

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "1 1", AnchorMax = "1 1",
					OffsetMin = "-35 -37.5",
					OffsetMax = "-10 -12.5"
				},
				Text =
				{
					Text = Msg(player, Close),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-bold.ttf",
					FontSize = 10,
					Color = _config.UI.ColorWhite.Get()
				},
				Button =
				{
					Close = Layer,
					Color = _config.UI.ColorThree.Get(),
					Command = "UI_Kits close"
				}
			}, Layer + ".Header");

			if (IsAdmin(player))
				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = "1 1", AnchorMax = "1 1",
						OffsetMin = "-140 -37.5",
						OffsetMax = "-45 -12.5"
					},
					Text =
					{
						Text = Msg(player, CreateKit),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 10,
						Color = _config.UI.ColorWhite.Get()
					},
					Button =
					{
						Color = _config.UI.ColorTwo.Get(),
						Command = $"UI_Kits editkit {page} True"
					}
				}, Layer + ".Header");

			#endregion

			#region Second Header

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "0 1",
					OffsetMin = "10 -85",
					OffsetMax = "110 -60"
				},
				Text =
				{
					Text = Msg(player, ListKits),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = _config.UI.ColorWhite.Get()
				}
			}, Layer + ".Main");

			#region Checkbox

			if (IsAdmin(player))
				CheckBoxUi(ref container,
					Layer + ".Main",
					Layer + ".ShowAll",
					"0 1", "0 1",
					"90 -77.5",
					"100 -67.5",
					showAll,
					$"UI_Kits main {targetId} 0 {!showAll}",
					Msg(player, ShowAll)
				);

			#endregion

			#region Pages

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "1 1", AnchorMax = "1 1",
					OffsetMin = "-132.5 -82.5",
					OffsetMax = "-72.5 -60"
				},
				Text =
				{
					Text = Msg(player, Back),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = _config.UI.ColorWhite.Get()
				},
				Button =
				{
					Color = _config.UI.ColorOne.Get(),
					Command = page != 0 ? $"UI_Kits main {targetId} {page - 1} {showAll}" : ""
				}
			}, Layer + ".Main");

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "1 1", AnchorMax = "1 1",
					OffsetMin = "-70 -82.5",
					OffsetMax = "-10 -60"
				},
				Text =
				{
					Text = Msg(player, Next),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = _config.UI.ColorWhite.Get()
				},
				Button =
				{
					Color = _config.UI.ColorThree.Get(),
					Command = allKits.Count > (page + 1) * totalAmount
						? $"UI_Kits main {targetId} {page + 1} {showAll}"
						: ""
				}
			}, Layer + ".Main");

			#endregion

			#endregion

			#region Kits

			if (allKits.Count == 0)
				container.Add(new CuiLabel
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin = "0 25", OffsetMax = "0 -85"
					},
					Text =
					{
						Text = Msg(player, NotAvailableKits),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-bold.ttf",
						FontSize = 20,
						Color = "1 1 1 0.45"
					}
				}, Layer + ".Main");
			else
				for (var i = 0; i < kitsList.Count; i++)
				{
					var kit = kitsList[i];

					var number = page * totalAmount + i + 1;

					container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = "0.5 1", AnchorMax = "0.5 1",
								OffsetMin = $"{xSwitch} {ySwitch - _config.UI.KitHeight}",
								OffsetMax = $"{xSwitch + _config.UI.KitWidth} {ySwitch}"
							},
							Image =
							{
								Color = "0 0 0 0"
							}
						}, Layer + ".Main", Layer + $".Kit.{kit.ID}.Main");

					container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = "0 0", AnchorMax = "1 1",
								OffsetMin = "0 30", OffsetMax = "0 0"
							},
							Image =
							{
								Color = _config.UI.ColorOne.Get()
							}
						}, Layer + $".Kit.{kit.ID}.Main", Layer + $".Kit.{kit.ID}.Main.Background");

					#region Image

					if (_enabledImageLibrary && !string.IsNullOrEmpty(kit.Image))
						container.Add(new CuiElement
						{
							Parent = Layer + $".Kit.{kit.ID}.Main",
							Components =
							{
								new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", kit.Image)},
								new CuiRectTransformComponent
								{
									AnchorMin = _config.UI.Image.AnchorMin,
									AnchorMax = _config.UI.Image.AnchorMax,
									OffsetMin = _config.UI.Image.OffsetMin,
									OffsetMax = _config.UI.Image.OffsetMax
								}
							}
						});

					#endregion

					#region Name

					if (_config.ShowNumber)
						container.Add(new CuiLabel
							{
								RectTransform =
								{
									AnchorMin = "0.5 1", AnchorMax = "0.5 1",
									OffsetMin = "-45 -75",
									OffsetMax = "45 0"
								},
								Text =
								{
									Text = $"#{number}",
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-bold.ttf",
									FontSize = 60,
									Color = _config.UI.ColorFour.Get()
								}
							}, Layer + $".Kit.{kit.ID}.Main");

					if (_config.UI.DisplayName.Enabled)
						container.Add(new CuiLabel
							{
								RectTransform =
								{
									AnchorMin = _config.UI.DisplayName.AnchorMin,
									AnchorMax = _config.UI.DisplayName.AnchorMax,
									OffsetMin = _config.UI.DisplayName.OffsetMin,
									OffsetMax = _config.UI.DisplayName.OffsetMax
								},
								Text =
								{
									Text = $"{kit.DisplayName}",
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-bold.ttf",
									FontSize = 16,
									Color = "1 1 1 1"
								}
							}, Layer + $".Kit.{kit.ID}.Main");

					#endregion

					#region Line

					container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = "0 0", AnchorMax = "1 0",
								OffsetMin = "0 0", OffsetMax = "0 2"
							},
							Image = {Color = HexToCuiColor(kit.Color)}
						}, Layer + $".Kit.{kit.ID}.Main.Background");

					#endregion

					#region Give Kit

					if (isAdmin)
						container.Add(new CuiButton
							{
								RectTransform =
								{
									AnchorMin = "0 1", AnchorMax = "0 1",
									OffsetMin = $"0 -{_config.UI.KitHeight}",
									OffsetMax = $"{_config.UI.KitWidth - 30} -{_config.UI.KitHeight - 25}"
								},
								Text =
								{
									Text = Msg(player, KitTake),
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-regular.ttf",
									FontSize = 10,
									Color = "1 1 1 1"
								},
								Button =
								{
									Color = _config.UI.ColorOne.Get(),
									Command = $"UI_Kits givekit {kit.ID} {targetId} {page} {showAll}"
								}
							}, Layer + $".Kit.{kit.ID}.Main");
					else if (kit.ShowInfo)
						container.Add(new CuiButton
							{
								RectTransform =
								{
									AnchorMin = "0 1", AnchorMax = "0 1",
									OffsetMin = $"0 -{_config.UI.KitHeight}",
									OffsetMax = $"{_config.UI.KitWidth - 30} -{_config.UI.KitHeight - 25}"
								},
								Text =
								{
									Text = Msg(player, KitTake),
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-regular.ttf",
									FontSize = 10,
									Color = "1 1 1 1"
								},
								Button =
								{
									Color = _config.UI.ColorOne.Get(),
									Command = $"UI_Kits givekit {kit.ID} {targetId} {page} {showAll}"
								}
							}, Layer + $".Kit.{kit.ID}.Main");
					else
						container.Add(new CuiButton
							{
								RectTransform =
								{
									AnchorMin = "0 1", AnchorMax = "0 1",
									OffsetMin = $"0 -{_config.UI.KitHeight}",
									OffsetMax = $"{_config.UI.KitWidth} -{_config.UI.KitHeight - 25}"
								},
								Text =
								{
									Text = Msg(player, KitTake),
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-regular.ttf",
									FontSize = 10,
									Color = "1 1 1 1"
								},
								Button =
								{
									Color = _config.UI.ColorOne.Get(),
									Command = $"UI_Kits givekit {kit.ID} {targetId} {page} {showAll}"
								}
							}, Layer + $".Kit.{kit.ID}.Main");

					#endregion

					#region Info

					if (isAdmin)
						container.Add(new CuiButton
							{
								RectTransform =
								{
									AnchorMin = "0 1", AnchorMax = "0 1",
									OffsetMin = $"{_config.UI.KitWidth - 25} -{_config.UI.KitHeight}",
									OffsetMax = $"{_config.UI.KitWidth} -{_config.UI.KitHeight - 25}"
								},
								Text =
								{
									Text = Msg(player, KitInfo),
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-regular.ttf",
									FontSize = 18,
									Color = "1 1 1 1"
								},
								Button =
								{
									Color = _config.UI.ColorOne.Get(),
									Command = $"UI_Kits infokit {page} {kit.ID}"
								}
							}, Layer + $".Kit.{kit.ID}.Main");
					else if (kit.ShowInfo)
						container.Add(new CuiButton
							{
								RectTransform =
								{
									AnchorMin = "0 1", AnchorMax = "0 1",
									OffsetMin = $"{_config.UI.KitWidth - 25} -{_config.UI.KitHeight}",
									OffsetMax = $"{_config.UI.KitWidth} -{_config.UI.KitHeight - 25}"
								},
								Text =
								{
									Text = Msg(player, KitInfo),
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-regular.ttf",
									FontSize = 18,
									Color = "1 1 1 1"
								},
								Button =
								{
									Color = _config.UI.ColorOne.Get(),
									Command = $"UI_Kits infokit {page} {kit.ID}"
								}
							}, Layer + $".Kit.{kit.ID}.Main");

					#endregion

					RefreshKitUi(ref container, player, kit);

					if ((i + 1) % _config.UI.KitsOnString == 0)
					{
						xSwitch = constSwitch;
						ySwitch = ySwitch - _config.UI.KitHeight - _config.UI.Margin;
					}
					else
					{
						xSwitch += _config.UI.Margin + _config.UI.KitWidth;
					}
				}

			#endregion

			#region Description

			NpcKitsData npcKit;
			var description = targetId == 0
				? _config.Description.Description
				: _config.NpcKits.TryGetValue(targetId.ToString(), out npcKit)
					? npcKit.Description
					: string.Empty;

			_config.Description.Get(ref container, Layer + ".Main", null, description);

			#endregion

			#endregion

			CuiHelper.DestroyUi(player, Layer + ".Main");
			CuiHelper.AddUi(player, container);
		}

		private void InfoKitUi(BasePlayer player, int page, Kit kit)
		{
			var container = new CuiElementContainer();

			#region Fields

			var Size = 70f;
			var Margin = 5f;

			var ySwitch = -125f;
			var amountOnString = 6;
			var constSwitch = -(amountOnString * Size + (amountOnString - 1) * Margin) / 2f;

			var total = 0;

			#endregion

			#region Background

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Image =
				{
					Color = _config.UI.ColorFive.Get()
				}
			}, "Overlay", InfoLayer);

			#endregion

			#region Header

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "0.5 1", AnchorMax = "0.5 1",
					OffsetMin = "112.5 -140", OffsetMax = "222.5 -115"
				},
				Text =
				{
					Text = Msg(player, ComeBack),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = _config.UI.ColorThree.Get(),
					Command = "UI_Kits stopedit",
					Close = InfoLayer
				}
			}, InfoLayer);

			#region Change Button

			if (IsAdmin(player))
			{
				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0.5 1", AnchorMax = "0.5 1",
						OffsetMin = "-12.5 -140", OffsetMax = "102.5 -115"
					},
					Image = {Color = "0 0 0 0"}
				}, InfoLayer, InfoLayer + ".Btn.Change");

				CreateOutLine(ref container, InfoLayer + ".Btn.Change", _config.UI.ColorThree.Get(), 1);

				container.Add(new CuiButton
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
					Text =
					{
						Text = Msg(player, Edit),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 10,
						Color = "1 1 1 1"
					},
					Button =
					{
						Color = "0 0 0 0",
						Command = $"UI_Kits editkit {page} {false} {kit.ID}",
						Close = InfoLayer
					}
				}, InfoLayer + ".Btn.Change");
			}

			#endregion

			#endregion

			#region Main

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0.5 1", AnchorMax = "0.5 1",
					OffsetMin = $"{constSwitch} {ySwitch - 15f}", OffsetMax = $"0 {ySwitch}"
				},
				Text =
				{
					Text = Msg(player, ContainerMain),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-regular.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				}
			}, InfoLayer);

			ySwitch -= 20f;

			var xSwitch = constSwitch;

			for (var slot = 0; slot < amountOnString * 4; slot++)
			{
				var kitItem = kit.GetItemByContainerAndPosition("main", slot);

				InfoItemUi(ref container, player,
					slot,
					$"{xSwitch} {ySwitch - Size}",
					$"{xSwitch + Size} {ySwitch}",
					kit,
					kitItem,
					total,
					"main", page);

				if ((slot + 1) % amountOnString == 0)
				{
					xSwitch = constSwitch;
					ySwitch = ySwitch - Size - Margin;
				}
				else
				{
					xSwitch += Size + Margin;
				}

				total++;
			}

			#endregion

			#region Wear

			ySwitch -= 5f;

			amountOnString = 7;

			constSwitch = -(amountOnString * Size + (amountOnString - 1) * Margin) / 2f;

			xSwitch = constSwitch;

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0.5 1", AnchorMax = "0.5 1",
					OffsetMin = $"{constSwitch} {ySwitch - 15f}", OffsetMax = $"0 {ySwitch}"
				},
				Text =
				{
					Text = Msg(player, ContainerWear),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-regular.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				}
			}, InfoLayer);

			ySwitch -= 20f;

			for (var slot = 0; slot < amountOnString; slot++)
			{
				var kitItem = kit.GetItemByContainerAndPosition("wear", slot);

				InfoItemUi(ref container, player,
					slot,
					$"{xSwitch} {ySwitch - Size}",
					$"{xSwitch + Size} {ySwitch}",
					kit,
					kitItem,
					total,
					"wear", page);

				if ((slot + 1) % amountOnString == 0)
				{
					xSwitch = constSwitch;
					ySwitch = ySwitch - Size - Margin;
				}
				else
				{
					xSwitch += Size + Margin;
				}

				total++;
			}

			#endregion

			#region Belt

			ySwitch -= 5f;

			amountOnString = 6;

			constSwitch = -(amountOnString * Size + (amountOnString - 1) * Margin) / 2f;

			xSwitch = constSwitch;

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0.5 1", AnchorMax = "0.5 1",
					OffsetMin = $"{constSwitch} {ySwitch - 15f}", OffsetMax = $"0 {ySwitch}"
				},
				Text =
				{
					Text = Msg(player, ContainerBelt),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-regular.ttf",
					FontSize = 12,
					Color = "1 1 1 1"
				}
			}, InfoLayer);

			ySwitch -= 20f;

			for (var slot = 0; slot < amountOnString; slot++)
			{
				var kitItem = kit.GetItemByContainerAndPosition("belt", slot);

				InfoItemUi(ref container, player,
					slot,
					$"{xSwitch} {ySwitch - Size}",
					$"{xSwitch + Size} {ySwitch}",
					kit,
					kitItem,
					total,
					"belt", page);

				if ((slot + 1) % amountOnString == 0)
				{
					xSwitch = constSwitch;
					ySwitch = ySwitch - Size - Margin;
				}
				else
				{
					xSwitch += Size + Margin;
				}

				total++;
			}

			#endregion

			#region Description

			_config.InfoKitDescription.Get(ref container, InfoLayer, null, kit.Description);

			#endregion

			CuiHelper.DestroyUi(player, InfoLayer);
			CuiHelper.AddUi(player, container);
		}

		private void EditingKitUi(BasePlayer player, int page, bool creating, int kitId = -1)
		{
			#region Dictionary

			if (!_kitEditing.ContainsKey(player.userID))
			{
				if (kitId != -1)
				{
					var kit = FindKitByID(kitId);
					if (kit == null) return;

					_kitEditing.Add(player.userID, new Dictionary<string, object>
					{
						["Name"] = kit.Name,
						["DisplayName"] = kit.DisplayName,
						["Color"] = kit.Color,
						["Permission"] = kit.Permission,
						["Description"] = kit.Description,
						["Image"] = kit.Image,
						["Hide"] = kit.Hide,
						["ShowInfo"] = kit.ShowInfo,
						["Amount"] = kit.Amount,
						["Cooldown"] = kit.Cooldown,
						["CooldownAfterWipe"] = kit.CooldownAfterWipe,
						["Sale"] = kit.Sale,
						["Price"] = kit.Price,
						["AutoKit"] = _config.AutoKits.Contains(kit.Name)
					});
				}
				else
				{
					_kitEditing.Add(player.userID, new Dictionary<string, object>
					{
						["Name"] = CuiHelper.GetGuid(),
						["DisplayName"] = "My Kit",
						["Color"] = _config.KitColor,
						["Permission"] = $"{Name}.default",
						["Description"] = string.Empty,
						["Image"] = string.Empty,
						["Hide"] = true,
						["ShowInfo"] = true,
						["Amount"] = 0,
						["Cooldown"] = 0.0,
						["CooldownAfterWipe"] = 0.0,
						["Sale"] = false,
						["Price"] = 0,
						["AutoKit"] = false
					});
				}
			}

			#endregion

			var container = new CuiElementContainer();

			#region Main

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Image =
				{
					Color = _config.UI.ColorFive.Get()
				}
			}, "Overlay", EditingLayer);

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
					OffsetMin = "-260 -230",
					OffsetMax = "260 255"
				},
				Image =
				{
					Color = _config.UI.ColorTwo.Get()
				}
			}, EditingLayer, EditingLayer + ".Main");

			#region Header

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "1 1",
					OffsetMin = "0 -50",
					OffsetMax = "0 0"
				},
				Image = {Color = _config.UI.ColorOne.Get()}
			}, EditingLayer + ".Main", EditingLayer + ".Header");

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 0", AnchorMax = "1 1",
					OffsetMin = "10 0",
					OffsetMax = "0 0"
				},
				Text =
				{
					Text = Msg(player, CreateOrEditKit),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-bold.ttf",
					FontSize = 14,
					Color = _config.UI.ColorWhite.Get()
				}
			}, EditingLayer + ".Header");

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "1 1", AnchorMax = "1 1",
					OffsetMin = "-35 -37.5",
					OffsetMax = "-10 -12.5"
				},
				Text =
				{
					Text = Msg(player, Close),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-bold.ttf",
					FontSize = 10,
					Color = _config.UI.ColorWhite.Get()
				},
				Button =
				{
					Close = EditingLayer,
					Color = _config.UI.ColorThree.Get(),
					Command = "UI_Kits close"
				}
			}, EditingLayer + ".Header");

			if (IsAdmin(player))
				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = "1 1", AnchorMax = "1 1",
						OffsetMin = "-140 -37.5",
						OffsetMax = "-45 -12.5"
					},
					Text =
					{
						Text = Msg(player, MainMenu),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 10,
						Color = "1 1 1 1"
					},
					Button =
					{
						Color = _config.UI.ColorTwo.Get(),
						Command = $"{_config.Commands.GetRandom()}",
						Close = EditingLayer
					}
				}, EditingLayer + ".Header");

			#endregion

			#region Fields

			var ySwitch = -60f;
			var Height = 60f;
			var Width = 225f;
			var xMargin = 35f;
			var yMargin = 10f;

			var i = 1;
			foreach (var obj in _kitEditing[player.userID]
				         .Where(x => !(x.Value is bool)))
			{
				var xSwitch = i % 2 == 0 ? xMargin / 2f : -Width - xMargin / 2f;

				EditFieldUi(player, ref container, EditingLayer + ".Main", EditingLayer + $".Editing.{i}",
					$"{xSwitch} {ySwitch - Height}",
					$"{xSwitch + Width} {ySwitch}",
					$"UI_Kits editkit {page} {creating} {kitId} {obj.Key} ",
					obj);

				if (i % 2 == 0) ySwitch = ySwitch - Height - yMargin;

				i++;
			}

			#region Hide

			var hide = !(_kitEditing[player.userID]["Hide"] is bool && (bool) _kitEditing[player.userID]["Hide"]);

			CheckBoxUi(ref container, EditingLayer + ".Main", EditingLayer + ".Editing.Hide", "0.5 1", "0.5 1",
				$"{-Width - xMargin / 2f} {ySwitch - 10}",
				$"{-Width - xMargin / 2f + 10} {ySwitch}",
				hide,
				$"UI_Kits editkit {page} {creating} {kitId} Hide {hide}",
				Msg(player, EnableKit)
			);

			#endregion

			#region Auto Kit

			var autoKit = _kitEditing[player.userID]["AutoKit"] is bool && (bool) _kitEditing[player.userID]["AutoKit"];

			CheckBoxUi(ref container, EditingLayer + ".Main", EditingLayer + ".Editing.AutoKit", "0.5 1", "0.5 1",
				$"{-Width - xMargin / 2f + 80} {ySwitch - 10}",
				$"{-Width - xMargin / 2f + 90} {ySwitch}",
				autoKit,
				$"UI_Kits editkit {page} {creating} {kitId} AutoKit {!autoKit}",
				Msg(player, AutoKit)
			);

			#endregion

			#region Sale

			var sale = _kitEditing[player.userID]["Sale"] is bool && (bool) _kitEditing[player.userID]["Sale"];

			CheckBoxUi(ref container, EditingLayer + ".Main", EditingLayer + ".Editing.Sale", "0.5 1", "0.5 1",
				$"{-Width - xMargin / 2f + 160} {ySwitch - 10}",
				$"{-Width - xMargin / 2f + 170} {ySwitch}",
				sale,
				$"UI_Kits editkit {page} {creating} {kitId} Sale {!sale}",
				Msg(player, EnabledSale)
			);

			#endregion

			#region Show Info

			var showInfo = Convert.ToBoolean(_kitEditing[player.userID]["ShowInfo"]);

			CheckBoxUi(ref container, EditingLayer + ".Main", EditingLayer + ".Editing.ShowInfo", "0.5 1", "0.5 1",
				$"{-Width - xMargin / 2f + 240} {ySwitch - 10}",
				$"{-Width - xMargin / 2f + 250} {ySwitch}",
				showInfo,
				$"UI_Kits editkit {page} {creating} {kitId} ShowInfo {!showInfo}",
				Msg(player, KitShowInfo)
			);

			#endregion

			#endregion

			ySwitch -= 35f;

			#region Buttons

			#region Save Kit

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "0 1",
					OffsetMin = $"15 {ySwitch - 25}",
					OffsetMax = $"115 {ySwitch}"
				},
				Text =
				{
					Text = Msg(player, SaveKit),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = _config.UI.ColorThree.Get(),
					Command = $"UI_Kits savekit {page} {creating} {kitId}",
					Close = EditingLayer
				}
			}, EditingLayer + ".Main");

			#endregion

			#region Add From Inventory

			if (!creating)
				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = "0.5 1", AnchorMax = "0.5 1",
						OffsetMin = $"-100 {ySwitch - 25}",
						OffsetMax = $"100 {ySwitch}"
					},
					Text =
					{
						Text = Msg(player, CopyItems),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 10,
						Color = "1 1 1 1"
					},
					Button =
					{
						Color = HexToCuiColor("#50965F"),
						Command = $"UI_Kits frominv {page} {kitId}",
						Close = EditingLayer
					}
				}, EditingLayer + ".Main");

			#endregion

			#region Remove Kit

			if (!creating)
				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = "1 1", AnchorMax = "1 1",
						OffsetMin = $"-115 {ySwitch - 25}",
						OffsetMax = $"-15 {ySwitch}"
					},
					Text =
					{
						Text = Msg(player, RemoveKit),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 10,
						Color = "1 1 1 1"
					},
					Button =
					{
						Color = _config.UI.ColorRed.Get(),
						Command = $"UI_Kits removekit {page} {kitId}",
						Close = EditingLayer
					}
				}, EditingLayer + ".Main");

			#endregion

			#endregion

			#endregion

			CuiHelper.DestroyUi(player, EditingLayer);
			CuiHelper.AddUi(player, container);
		}

		private void EditingItemUi(BasePlayer player, int page, int kitId, int slot, string itemContainer,
			bool First = false)
		{
			var container = new CuiElementContainer();

			#region Dictionary

			if (!_itemEditing.ContainsKey(player.userID))
			{
				var kit = FindKitByID(kitId);
				if (kit == null) return;

				var item = kit.GetItemByContainerAndPosition(itemContainer, slot);
				if (item != null)
					_itemEditing.Add(player.userID, new Dictionary<string, object>
					{
						["Type"] = item.Type,
						["Command"] = item.Command,
						["Container"] = item.Container,
						["ShortName"] = item.ShortName,
						["DisplayName"] = item.DisplayName,
						["Amount"] = item.Amount,
						["Blueprint"] = item.Blueprint,
						["SkinID"] = item.SkinID,
						["Chance"] = item.Chance,
						["Position"] = item.Position
					});
				else
					_itemEditing.Add(player.userID, new Dictionary<string, object>
					{
						["Type"] = KitItemType.Item,
						["Container"] = itemContainer,
						["Command"] = string.Empty,
						["ShortName"] = string.Empty,
						["DisplayName"] = string.Empty,
						["Amount"] = 1,
						["Blueprint"] = 0,
						["SkinID"] = 0UL,
						["Chance"] = 100,
						["Position"] = slot
					});
			}

			#endregion

			var edit = _itemEditing[player.userID];

			#region Background

			if (First)
			{
				CuiHelper.DestroyUi(player, EditingLayer);

				container.Add(new CuiPanel
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
					Image = {Color = _config.UI.ColorSix.Get()},
					CursorEnabled = true
				}, "Overlay", EditingLayer);
			}

			#endregion

			#region Main

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
					OffsetMin = "-260 -240",
					OffsetMax = "260 250"
				},
				Image =
				{
					Color = _config.UI.ColorTwo.Get()
				}
			}, EditingLayer, EditingLayer + ".Main");

			#region Header

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "1 1",
					OffsetMin = "0 -50",
					OffsetMax = "0 0"
				},
				Image = {Color = _config.UI.ColorOne.Get()}
			}, EditingLayer + ".Main", EditingLayer + ".Header");

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 0", AnchorMax = "1 1",
					OffsetMin = "10 0",
					OffsetMax = "0 0"
				},
				Text =
				{
					Text = Msg(player, EditingTitle),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-bold.ttf",
					FontSize = 14,
					Color = _config.UI.ColorWhite.Get()
				}
			}, EditingLayer + ".Header");

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "1 1", AnchorMax = "1 1",
					OffsetMin = "-35 -37.5",
					OffsetMax = "-10 -12.5"
				},
				Text =
				{
					Text = Msg(player, Close),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-bold.ttf",
					FontSize = 10,
					Color = _config.UI.ColorWhite.Get()
				},
				Button =
				{
					Close = EditingLayer,
					Color = _config.UI.ColorThree.Get(),
					Command = $"UI_Kits infokit {page} {kitId}"
				}
			}, EditingLayer + ".Header");

			#endregion

			#region Type

			var type = edit["Type"] as KitItemType? ?? KitItemType.Item;

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "0.5 1", AnchorMax = "0.5 1",
					OffsetMin = "10 -110",
					OffsetMax = "115 -80"
				},
				Text =
				{
					Text = Msg(player, ItemName),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = type == KitItemType.Item ? _config.UI.ColorThree.Get() : _config.UI.ColorSeven.Get(),
					Command = $"UI_Kits edititem {page} {itemContainer} {kitId} {slot} Type {KitItemType.Item}"
				}
			}, EditingLayer + ".Main");

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "0.5 1", AnchorMax = "0.5 1",
					OffsetMin = "135 -110",
					OffsetMax = "240 -80"
				},
				Text =
				{
					Text = Msg(player, CmdName),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = type == KitItemType.Command ? _config.UI.ColorThree.Get() : _config.UI.ColorSeven.Get(),
					Command = $"UI_Kits edititem {page} {itemContainer} {kitId} {slot} Type {KitItemType.Command}"
				}
			}, EditingLayer + ".Main");

			#endregion

			#region Command

			EditFieldUi(player, ref container, EditingLayer + ".Main", CuiHelper.GetGuid(),
				"-240 -110",
				"0 -60",
				$"UI_Kits edititem {page} {itemContainer} {kitId} {slot} Command ",
				new KeyValuePair<string, object>("Command", edit["Command"]));

			#endregion

			#region Item

			var shortName = (string) edit["ShortName"];

			#region Image

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0.5 1", AnchorMax = "0.5 1",
					OffsetMin = "-240 -265", OffsetMax = "-105 -130"
				},
				Image = {Color = _config.UI.ColorOne.Get()}
			}, EditingLayer + ".Main", EditingLayer + ".Image");

			if (!string.IsNullOrEmpty(shortName))
				container.Add(new CuiElement
				{
					Parent = EditingLayer + ".Image",
					Components =
					{
						new CuiImageComponent
						{
							ItemId = ItemManager.FindItemDefinition(shortName)?.itemid ?? 0,
							SkinId = (ulong) edit["SkinID"]
						},
						new CuiRectTransformComponent
						{
							AnchorMin = "0 0", AnchorMax = "1 1",
							OffsetMin = "10 10", OffsetMax = "-10 -10"
						}
					}
				});

			#endregion

			#region ShortName

			EditFieldUi(player, ref container, EditingLayer + ".Main", CuiHelper.GetGuid(),
				"-85 -190",
				"70 -130",
				$"UI_Kits edititem {page} {itemContainer} {kitId} {slot} ShortName ",
				new KeyValuePair<string, object>("ShortName", edit["ShortName"]));

			#endregion

			#region Skin

			EditFieldUi(player, ref container, EditingLayer + ".Main", CuiHelper.GetGuid(),
				"85 -190",
				"240 -130",
				$"UI_Kits edititem {page} {itemContainer} {kitId} {slot} SkinID ",
				new KeyValuePair<string, object>("SkinID", edit["SkinID"]));

			#endregion

			#region Select Item

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "0.5 1", AnchorMax = "0.5 1",
					OffsetMin = "-85 -265",
					OffsetMax = "55 -235"
				},
				Text =
				{
					Text = Msg(player, BtnSelect),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = _config.UI.ColorThree.Get(),
					Command = $"UI_Kits selectitem {itemContainer} {kitId} {slot}"
				}
			}, EditingLayer + ".Main");

			#endregion

			#region Blueprint

			var bp = edit["Blueprint"] as int? ?? 0;
			CheckBoxUi(ref container,
				EditingLayer + ".Main",
				CuiHelper.GetGuid(),
				"0.5 1", "0.5 1",
				"65 -255",
				"75 -245",
				bp == 1,
				$"UI_Kits edititem {page} {itemContainer} {kitId} {slot} Blueprint {(bp == 0 ? 1 : 0)}",
				Msg(player, BluePrint)
			);

			#endregion

			#region Amount

			EditFieldUi(player, ref container, EditingLayer + ".Main", CuiHelper.GetGuid(),
				"-240 -345",
				"-7.5 -285",
				$"UI_Kits edititem {page} {itemContainer} {kitId} {slot} Amount ",
				new KeyValuePair<string, object>("Amount", edit["Amount"]));

			#endregion

			#region Chance

			EditFieldUi(player, ref container, EditingLayer + ".Main", CuiHelper.GetGuid(),
				"7.5 -345",
				"240 -285",
				$"UI_Kits edititem {page} {itemContainer} {kitId} {slot} Chance ",
				new KeyValuePair<string, object>("Chance", edit["Chance"]));

			#endregion

			#region Display Name

			EditFieldUi(player, ref container, EditingLayer + ".Main", CuiHelper.GetGuid(),
				"-240 -425",
				"240 -365",
				$"UI_Kits edititem {page} {itemContainer} {kitId} {slot} DisplayName ",
				new KeyValuePair<string, object>("DisplayName", edit["DisplayName"]));

			#endregion

			#endregion

			#region Save Button

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "0.5 0", AnchorMax = "0.5 0",
					OffsetMin = "-90 10",
					OffsetMax = $"{(slot == -1 ? 90 : 55)} 40"
				},
				Text =
				{
					Text = Msg(player, BtnSave),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = _config.UI.ColorThree.Get(),
					Command = $"UI_Kits saveitem {page} {kitId} {slot} {itemContainer}",
					Close = EditingLayer
				}
			}, EditingLayer + ".Main");

			#endregion

			#region Remove Item

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "0.5 0", AnchorMax = "0.5 0",
					OffsetMin = "60 10",
					OffsetMax = "90 40"
				},
				Text =
				{
					Text = Msg(player, RemoveItem),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = _config.UI.ColorRed.Get(),
					Command = $"UI_Kits removeitem {page} {kitId} {slot} {itemContainer}",
					Close = EditingLayer
				}
			}, EditingLayer + ".Main");

			#endregion

			#endregion

			CuiHelper.DestroyUi(player, EditingLayer + ".Main");
			CuiHelper.AddUi(player, container);
		}

		private void SelectItem(BasePlayer player, int kitId, int slot, string itemContainer,
			string selectedCategory = "", int page = 0, string input = "")
		{
			if (string.IsNullOrEmpty(selectedCategory)) selectedCategory = _itemsCategories.FirstOrDefault().Key;

			var container = new CuiElementContainer();

			#region Background

			container.Add(new CuiButton
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Text = {Text = ""},
				Button =
				{
					Close = ModalLayer,
					Color = _config.UI.ColorSix.Get()
				}
			}, "Overlay", ModalLayer);

			#endregion

			#region Main

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
					OffsetMin = "-260 -270",
					OffsetMax = "260 280"
				},
				Image =
				{
					Color = _config.UI.ColorTwo.Get()
				}
			}, ModalLayer, ModalLayer + ".Main");

			#region Categories

			var amountOnString = 4;
			var Width = 120f;
			var Height = 25f;
			var xMargin = 5f;
			var yMargin = 5f;

			var constSwitch = -(amountOnString * Width + (amountOnString - 1) * xMargin) / 2f;
			var xSwitch = constSwitch;
			var ySwitch = -15f;

			var i = 1;
			foreach (var category in _itemsCategories)
			{
				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = "0.5 1", AnchorMax = "0.5 1",
						OffsetMin = $"{xSwitch} {ySwitch - Height}",
						OffsetMax = $"{xSwitch + Width} {ySwitch}"
					},
					Text =
					{
						Text = $"{category.Key}",
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 10,
						Color = "1 1 1 1"
					},
					Button =
					{
						Color = selectedCategory == category.Key
							? _config.UI.ColorThree.Get()
							: _config.UI.ColorOne.Get(),
						Command = $"UI_Kits selectitem {itemContainer} {kitId} {slot}  {category.Key}"
					}
				}, ModalLayer + ".Main");

				if (i % amountOnString == 0)
				{
					ySwitch = ySwitch - Height - yMargin;
					xSwitch = constSwitch;
				}
				else
				{
					xSwitch += xMargin + Width;
				}

				i++;
			}

			#endregion

			#region Items

			amountOnString = 5;

			var strings = 4;
			var totalAmount = amountOnString * strings;

			ySwitch = ySwitch - yMargin - Height - 10f;

			Width = 85f;
			Height = 85f;
			xMargin = 15f;
			yMargin = 5f;

			constSwitch = -(amountOnString * Width + (amountOnString - 1) * xMargin) / 2f;
			xSwitch = constSwitch;

			i = 1;

			var canSearch = !string.IsNullOrEmpty(input) && input.Length > 2;

			var temp = canSearch
				? _itemsCategories
					.SelectMany(x => x.Value)
					.Where(x => x.Value.StartsWith(input) || x.Value.Contains(input) || x.Value.EndsWith(input))
				: _itemsCategories[selectedCategory];

			var itemsAmount = temp.Count;
			var items = temp.SkipAndTake(page * totalAmount, totalAmount);

			items.ForEach(item =>
			{
				container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "0.5 1", AnchorMax = "0.5 1",
							OffsetMin = $"{xSwitch} {ySwitch - Height}",
							OffsetMax = $"{xSwitch + Width} {ySwitch}"
						},
						Image = {Color = _config.UI.ColorOne.Get()}
					}, ModalLayer + ".Main", ModalLayer + $".Item.{item}");

				container.Add(new CuiElement
				{
					Parent = ModalLayer + $".Item.{item}",
					Components =
					{
						new CuiImageComponent
						{
							ItemId = item.Key
						},
						new CuiRectTransformComponent
						{
							AnchorMin = "0 0", AnchorMax = "1 1",
							OffsetMin = "5 5", OffsetMax = "-5 -5"
						}
					}
				});

				container.Add(new CuiButton
					{
						RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
						Text = {Text = ""},
						Button =
						{
							Color = "0 0 0 0",
							Command = $"UI_Kits takeitem {page} {itemContainer} {kitId} {slot} {item.Value}",
							Close = ModalLayer
						}
					}, ModalLayer + $".Item.{item}");

				if (i % amountOnString == 0)
				{
					xSwitch = constSwitch;
					ySwitch = ySwitch - yMargin - Height;
				}
				else
				{
					xSwitch += xMargin + Width;
				}

				i++;
			});

			#endregion

			#region Search

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0.5 0", AnchorMax = "0.5 0",
					OffsetMin = "-90 10", OffsetMax = "90 35"
				},
				Image = {Color = _config.UI.ColorThree.Get()}
			}, ModalLayer + ".Main", ModalLayer + ".Search");

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 0", AnchorMax = "1 1",
					OffsetMin = "10 0", OffsetMax = "0 0"
				},
				Text =
				{
					Text = canSearch ? $"{input}" : Msg(player, ItemSearch),
					Align = canSearch ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = canSearch ? "1 1 1 0.8" : "1 1 1 1"
				}
			}, ModalLayer + ".Search");

			container.Add(new CuiElement
			{
				Parent = ModalLayer + ".Search",
				Components =
				{
					new CuiInputFieldComponent
					{
						FontSize = 10,
						Align = TextAnchor.MiddleLeft,
						Command = $"UI_Kits selectitem {itemContainer} {kitId} {slot} {selectedCategory} 0 ",
						Color = "1 1 1 0.95",
						CharsLimit = 150,
						NeedsKeyboard = true
					},
					new CuiRectTransformComponent
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin = "10 0", OffsetMax = "0 0"
					}
				}
			});

			#endregion

			#region Pages

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "0 0", AnchorMax = "0 0",
					OffsetMin = "10 10",
					OffsetMax = "80 35"
				},
				Text =
				{
					Text = Msg(player, Back),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = _config.UI.ColorOne.Get(),
					Command = page != 0
						? $"UI_Kits selectitem {itemContainer} {kitId} {slot} {selectedCategory} {page - 1} {input}"
						: ""
				}
			}, ModalLayer + ".Main");

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "1 0", AnchorMax = "1 0",
					OffsetMin = "-80 10",
					OffsetMax = "-10 35"
				},
				Text =
				{
					Text = Msg(player, Next),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = _config.UI.ColorThree.Get(),
					Command = itemsAmount > (page + 1) * totalAmount
						? $"UI_Kits selectitem {itemContainer} {kitId} {slot} {selectedCategory} {page + 1} {input}"
						: ""
				}
			}, ModalLayer + ".Main");

			#endregion

			#endregion

			CuiHelper.DestroyUi(player, ModalLayer);
			CuiHelper.AddUi(player, container);
		}

		private void ErrorUi(BasePlayer player, string msg)
		{
			var container = new CuiElementContainer
			{
				{
					new CuiPanel
					{
						RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
						Image = {Color = _config.UI.ColorFive.Get()},
						CursorEnabled = true
					},
					"Overlay", ModalLayer
				},
				{
					new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "0.5 0.5",
							AnchorMax = "0.5 0.5",
							OffsetMin = "-127.5 -75",
							OffsetMax = "127.5 140"
						},
						Image = {Color = _config.UI.ColorRed.Get()}
					},
					ModalLayer, ModalLayer + ".Main"
				},
				{
					new CuiLabel
					{
						RectTransform =
						{
							AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 -165", OffsetMax = "0 0"
						},
						Text =
						{
							Text = "XXX",
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-bold.ttf",
							FontSize = 120,
							Color = _config.UI.ColorWhite.Get()
						}
					},
					ModalLayer + ".Main"
				},
				{
					new CuiLabel
					{
						RectTransform =
						{
							AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 -175", OffsetMax = "0 -155"
						},
						Text =
						{
							Text = $"{msg}",
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-regular.ttf",
							FontSize = 12,
							Color = _config.UI.ColorWhite.Get()
						}
					},
					ModalLayer + ".Main"
				},
				{
					new CuiButton
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = "0 30"
						},
						Text =
						{
							Text = Msg(player, BtnClose),
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-regular.ttf",
							FontSize = 12,
							Color = _config.UI.ColorWhite.Get()
						},
						Button = {Color = HexToCuiColor("#CD3838"), Close = ModalLayer}
					},
					ModalLayer + ".Main"
				}
			};

			CuiHelper.DestroyUi(player, ModalLayer);
			CuiHelper.AddUi(player, container);
		}

		private void EditFieldUi(BasePlayer player, ref CuiElementContainer container,
			string parent,
			string name,
			string oMin,
			string oMax,
			string command,
			KeyValuePair<string, object> obj)
		{
			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0.5 1", AnchorMax = "0.5 1",
					OffsetMin = $"{oMin}",
					OffsetMax = $"{oMax}"
				},
				Image =
				{
					Color = "0 0 0 0"
				}
			}, parent, name);

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "1 1",
					OffsetMin = "0 -20", OffsetMax = "0 0"
				},
				Text =
				{
					Text = $"{obj.Key}",
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = "1 1 1 1"
				}
			}, name);

			container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin = "0 0", OffsetMax = "0 -20"
					},
					Image = {Color = "0 0 0 0"}
				}, name, $"{name}.Value");

			CreateOutLine(ref container, $"{name}.Value", _config.UI.ColorOne.Get());

			container.Add(new CuiElement
			{
				Parent = $"{name}.Value",
				Components =
				{
					new CuiInputFieldComponent
					{
						FontSize = 12,
						Align = TextAnchor.MiddleLeft,
						Command = $"{command}",
						Color = "1 1 1 0.4",
						CharsLimit = 150,
						NeedsKeyboard = true,
						Text = $"{obj.Value}",
						Font = "robotocondensed-regular.ttf"
					},
					new CuiRectTransformComponent
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin = "10 0", OffsetMax = "0 0"
					}
				}
			});

			container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = "1 1", AnchorMax = "1 1",
						OffsetMin = "-30 -40", OffsetMax = "0 0"
					},
					Text =
					{
						Text = Msg(player, EditRemoveField),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 14,
						Color = _config.UI.ColorOne.Get()
					},
					Button =
					{
						Color = "0 0 0 0",
						Command = $"{command}delete"
					}
				}, $"{name}.Value");
		}

		private void CheckBoxUi(ref CuiElementContainer container, string parent, string name, string aMin, string aMax,
			string oMin, string oMax, bool enabled,
			string command, string text)
		{
			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = aMin, AnchorMax = aMax,
					OffsetMin = oMin,
					OffsetMax = oMax
				},
				Image = {Color = "0 0 0 0"}
			}, parent, name);

			CreateOutLine(ref container, name, _config.UI.ColorThree.Get(), 1);

			if (enabled)
				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 1"
					},
					Image = {Color = _config.UI.ColorThree.Get()}
				}, name);


			container.Add(new CuiButton
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Text = {Text = ""},
				Button =
				{
					Color = "0 0 0 0",
					Command = $"{command}"
				}
			}, name);

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "1 0.5", AnchorMax = "1 0.5",
					OffsetMin = "5 -10",
					OffsetMax = "100 10"
				},
				Text =
				{
					Text = $"{text}",
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-regular.ttf",
					FontSize = 10,
					Color = _config.UI.ColorWhite.Get()
				}
			}, name);
		}

		private void InfoItemUi(ref CuiElementContainer container, BasePlayer player,
			int slot,
			string oMin,
			string oMax,
			Kit kit,
			KitItem kitItem, int total, string itemContainer, int page)
		{
			container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0.5 1", AnchorMax = "0.5 1",
						OffsetMin = $"{oMin}",
						OffsetMax = $"{oMax}"
					},
					Image =
					{
						Color = _config.UI.ColorOne.Get()
					}
				}, InfoLayer, InfoLayer + $".Item.{total}");

			if (kitItem != null)
			{
				container.Add(kitItem.GetImage("0 0", "1 1", "10 10", "-10 -10", InfoLayer + $".Item.{total}"));

				container.Add(new CuiLabel
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "1 1",
							OffsetMin = "2.5 3.5", OffsetMax = "-2.5 -2.5"
						},
						Text =
						{
							Text = $"x{kitItem.Amount}",
							Align = TextAnchor.LowerRight,
							Font = "robotocondensed-regular.ttf",
							FontSize = 10,
							Color = "1 1 1 1"
						}
					}, InfoLayer + $".Item.{total}");

				var color = _config.RarityColors.Find(x => x.Chance == kitItem.Chance);
				if (color != null)
				{
					container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = "0 0", AnchorMax = "1 0",
								OffsetMin = "0 0", OffsetMax = "0 2"
							},
							Image =
							{
								Color = HexToCuiColor(color.Color)
							}
						}, InfoLayer + $".Item.{total}");

					container.Add(new CuiLabel
						{
							RectTransform =
							{
								AnchorMin = "0 0", AnchorMax = "1 1",
								OffsetMin = "2.5 2.5", OffsetMax = "-2.5 -2.5"
							},
							Text =
							{
								Text = $"{kitItem.Chance}%",
								Align = TextAnchor.UpperLeft,
								Font = "robotocondensed-regular.ttf",
								FontSize = 10,
								Color = "1 1 1 1"
							}
						}, InfoLayer + $".Item.{total}");
				}
			}

			if (IsAdmin(player))
				container.Add(new CuiButton
					{
						RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
						Text = {Text = ""},
						Button =
						{
							Color = "0 0 0 0",
							Command =
								$"UI_Kits startedititem {page} {itemContainer} {kit.ID} {slot}",
							Close = InfoLayer
						}
					}, InfoLayer + $".Item.{total}");
		}

		private void RefreshKitUi(ref CuiElementContainer container, BasePlayer player, Kit kit)
		{
			var playerData = PlayerData.GetOrCreateKitData(player.UserIDString, kit.Name);
			if (playerData == null) return;

			CuiHelper.DestroyUi(player, Layer + $".Kit.{kit.ID}");

			container.Add(new CuiPanel
				{
					RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1"},
					Image = {Color = "0 0 0 0"}
				}, Layer + $".Kit.{kit.ID}.Main", Layer + $".Kit.{kit.ID}");

			if (_config.ShowAllKits && _config.ShowNoPermDescription && !string.IsNullOrEmpty(kit.Permission) &&
			    !player.HasPermission(kit.Permission))
			{
				container.Add(new CuiLabel
					{
						RectTransform =
						{
							AnchorMin = _config.UI.NoPermission.AnchorMin,
							AnchorMax = _config.UI.NoPermission.AnchorMax,
							OffsetMin = _config.UI.NoPermission.OffsetMin, OffsetMax = _config.UI.NoPermission.OffsetMax
						},
						Text =
						{
							Text = Msg(player, kit.Name, NoPermissionDescription),
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-regular.ttf",
							FontSize = 10,
							Color = _config.UI.ColorFour.Get()
						}
					}, Layer + $".Kit.{kit.ID}");
				return;
			}

			if (playerData.HasAmount > 0)
			{
				#region Title

				container.Add(new CuiLabel
					{
						RectTransform =
						{
							AnchorMin = _config.UI.KitAmountCooldown.AnchorMin,
							AnchorMax = _config.UI.KitAmountCooldown.AnchorMax,
							OffsetMin = _config.UI.KitAmountCooldown.OffsetMin,
							OffsetMax = _config.UI.KitAmountCooldown.OffsetMax
						},
						Text =
						{
							Text = Msg(player, kit.Name, KitYouHave),
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-bold.ttf",
							FontSize = 12,
							Color = "1 1 1 1"
						}
					}, Layer + $".Kit.{kit.ID}");

				#endregion

				#region Points

				var amount = Mathf.Min(playerData.HasAmount, 9);

				var width = amount == 1
					? _config.UI.KitAmount.Width
					: _config.UI.KitAmount.Width / amount * 0.9f;

				var margin = (_config.UI.KitAmount.Width - width * amount) / (amount - 1);

				var xSwitch = -(_config.UI.KitAmount.Width / 2f);

				for (var i = 0; i < amount; i++)
				{
					container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = _config.UI.KitAmount.AnchorMin,
								AnchorMax = _config.UI.KitAmount.AnchorMax,
								OffsetMin = $"{xSwitch} {_config.UI.KitAmount.OffsetMin}",
								OffsetMax = $"{xSwitch + width} {_config.UI.KitAmount.OffsetMax}"
							},
							Image =
							{
								Color = HexToCuiColor(kit.Color)
							}
						}, Layer + $".Kit.{kit.ID}");

					xSwitch += width + margin;
				}

				#endregion
			}
			else
			{
				var currentTime = GetCurrentTime();

				bool isCooldown;
				if ((isCooldown = kit.Cooldown > 0 && playerData.Cooldown > currentTime)
				    ||
				    (kit.CooldownAfterWipe > 0 && LeftWipeBlockTime(kit.CooldownAfterWipe) > 0))
				{
					var time = isCooldown
						? TimeSpan.FromSeconds(playerData.Cooldown - currentTime)
						: TimeSpan.FromSeconds(LeftWipeBlockTime(kit.CooldownAfterWipe));

					if (kit.Amount > 0)
					{
						container.Add(new CuiLabel
							{
								RectTransform =
								{
									AnchorMin = _config.UI.KitAmountCooldown.AnchorMin,
									AnchorMax = _config.UI.KitAmountCooldown.AnchorMax,
									OffsetMin = _config.UI.KitAmountCooldown.OffsetMin,
									OffsetMax = _config.UI.KitAmountCooldown.OffsetMax
								},
								Text =
								{
									Text = $"{FormatShortTime(time)}",
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-bold.ttf",
									FontSize = 12,
									Color = "1 1 1 1"
								}
							}, Layer + $".Kit.{kit.ID}", Layer + $".Kit.{kit.ID}.Cooldown");
					}
					else
					{
						container.Add(new CuiPanel
							{
								RectTransform =
								{
									AnchorMin = _config.UI.KitCooldown.AnchorMin,
									AnchorMax = _config.UI.KitCooldown.AnchorMax,
									OffsetMin = _config.UI.KitCooldown.OffsetMin,
									OffsetMax = _config.UI.KitCooldown.OffsetMax
								},
								Image = {Color = HexToCuiColor(kit.Color)}
							}, Layer + $".Kit.{kit.ID}", Layer + $".Kit.{kit.ID}.Cooldown");

						container.Add(new CuiLabel
							{
								RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
								Text =
								{
									Text = $"{FormatShortTime(time)}",
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-bold.ttf",
									FontSize = 12,
									Color = "1 1 1 1"
								}
							}, Layer + $".Kit.{kit.ID}.Cooldown");
					}
				}
				else
				{
					if (kit.Sale)
					{
						container.Add(new CuiPanel
							{
								RectTransform =
								{
									AnchorMin = _config.UI.KitSale.AnchorMin, AnchorMax = _config.UI.KitSale.AnchorMax,
									OffsetMin = _config.UI.KitSale.OffsetMin, OffsetMax = _config.UI.KitSale.OffsetMax
								},
								Image = {Color = HexToCuiColor(kit.Color)}
							}, Layer + $".Kit.{kit.ID}", Layer + $".Kit.{kit.ID}.Sale");

						container.Add(new CuiLabel
							{
								RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
								Text =
								{
									Text = Msg(player, kit.Name, PriceFormat, kit.Price),
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-bold.ttf",
									FontSize = 12,
									Color = "1 1 1 1"
								}
							}, Layer + $".Kit.{kit.ID}.Sale");
					}
					else
					{
						container.Add(new CuiLabel
							{
								RectTransform =
								{
									AnchorMin = _config.UI.KitAvailable.AnchorMin,
									AnchorMax = _config.UI.KitAvailable.AnchorMax,
									OffsetMin = _config.UI.KitAvailable.OffsetMin,
									OffsetMax = _config.UI.KitAvailable.OffsetMax
								},
								Text =
								{
									Text = Msg(player, kit.Name, KitAvailableTitle),
									Align = TextAnchor.MiddleCenter,
									Font = "robotocondensed-regular.ttf",
									FontSize = 10,
									Color = _config.UI.ColorFour.Get()
								}
							}, Layer + $".Kit.{kit.ID}");
					}
				}

				if (kit.Amount > 0)
				{
					var amount = Mathf.Min(kit.Amount, 9);

					var hasAmount = kit.Amount > 9 ? 9 * playerData.Amount / kit.Amount : playerData.Amount;

					var width = amount == 1
						? _config.UI.KitAmount.Width
						: _config.UI.KitAmount.Width / amount * 0.9f;

					var margin = (_config.UI.KitAmount.Width - width * amount) / (amount - 1);

					var xSwitch = -(_config.UI.KitAmount.Width / 2f);

					for (var i = 0; i < amount; i++)
					{
						container.Add(new CuiPanel
							{
								RectTransform =
								{
									AnchorMin = _config.UI.KitAmount.AnchorMin,
									AnchorMax = _config.UI.KitAmount.AnchorMax,
									OffsetMin = $"{xSwitch} {_config.UI.KitAmount.OffsetMin}",
									OffsetMax = $"{xSwitch + width} {_config.UI.KitAmount.OffsetMax}"
								},
								Image =
								{
									Color = i < hasAmount ? HexToCuiColor(kit.Color) : _config.UI.ColorTwo.Get()
								}
							}, Layer + $".Kit.{kit.ID}");

						xSwitch += width + margin;
					}
				}
			}
		}

		#endregion

		#region Kit Helpers

		private bool GiveKitToPlayer(BasePlayer player, Kit kit,
			bool force = false,
			bool chat = false,
			ulong targetId = DEFAULT_MAIN_TARGETID,
			int page = DEFAULT_MAIN_PAGE,
			bool showAll = DEFAULT_MAIN_SHOWALL, bool usingUI = true)
		{
			if (player == null || kit == null) return false;

			if (Interface.Oxide.CallHook("canRedeemKit", player) != null)
				return false;

			var currentTime = GetCurrentTime();

			var playerData = PlayerData.GetOrCreateKitData(player.UserIDString, kit.Name);

			if (force == false)
			{
				if ((playerData == null ||
				     playerData.HasAmount <= 0) &&
				    !string.IsNullOrEmpty(kit.Permission) &&
				    !player.HasPermission(kit.Permission))
				{
					SendNotifyOrUI(player, NoPermission, 1, chat);
					return false;
				}

				if (_config.BlockBuilding && !player.CanBuild())
				{
					SendNotifyOrUI(player, BBlocked, 1, chat);
					return false;
				}

				if (kit.CooldownAfterWipe > 0)
				{
					var leftTime = LeftWipeBlockTime(kit.CooldownAfterWipe);
					if (leftTime > 0)
					{
						SendNotifyOrUI(player, KitCooldown, 1, chat,
							FormatShortTime(TimeSpan.FromSeconds(leftTime)));
						return false;
					}
				}

				if (_config.UseNoEscape && !_config.NoEscapeWhiteList.Contains(kit.Name))
				{
					if (_config.UseRaidBlock && RaidBlocked(player))
					{
						SendNotifyOrUI(player, NoEscapeCombatBlocked, 1, chat);
						return false;
					}

					if (_config.UseCombatBlock && CombatBlocked(player))
					{
						SendNotifyOrUI(player, NoEscapeCombatBlocked, 1, chat);
						return false;
					}
				}

				if (playerData != null &&
				    playerData.HasAmount <= 0)
				{
					if (kit.Amount > 0 && playerData.Amount >= kit.Amount)
					{
						SendNotifyOrUI(player, KitLimit, 1, chat);
						return false;
					}

					if (kit.Cooldown > 0 && playerData.Cooldown > currentTime)
					{
						SendNotifyOrUI(player, KitCooldown, 1, chat,
							FormatShortTime(TimeSpan.FromSeconds(playerData.Cooldown - currentTime)));
						return false;
					}
				}

				var totalCount = kit.beltCount + kit.wearCount + kit.mainCount;
				if (player.inventory.containerBelt.capacity - player.inventory.containerBelt.itemList.Count <
				    kit.beltCount ||
				    player.inventory.containerWear.capacity - player.inventory.containerWear.itemList.Count <
				    kit.wearCount ||
				    player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count <
				    kit.mainCount)
					if (totalCount > player.inventory.containerMain.capacity -
					    player.inventory.containerMain.itemList.Count)
					{
						SendNotifyOrUI(player, NotEnoughSpace, 1, chat);
						return false;
					}

				if ((playerData == null || playerData.HasAmount <= 0) && kit.Sale &&
				    !_config.Economy.RemoveBalance(player, kit.Price))
				{
					SendNotifyOrUI(player, NotMoney, 1, chat);
					return false;
				}

				if (kit.UseBuilding && CopyPaste != null && !string.IsNullOrEmpty(kit.Building))
				{
					var success = CopyPaste?.Call("TryPasteFromSteamId", player.userID, kit.Building,
						_config.CopyPasteParameters.ToArray());
					if (success is string)
					{
						SendNotifyOrUI(player, BuildError, 1, chat);
						return false;
					}
				}
			}

			if (!force)
				ServerMgr.Instance.StartCoroutine(GiveKitItems(player, kit));
			else
				FastGiveKitItems(player, kit);

			kit.UseCommands(player);

			if (!force && playerData != null)
			{
				if (playerData.HasAmount > 0)
				{
					playerData.HasAmount -= 1;
				}
				else
				{
					if (kit.Amount > 0) playerData.Amount += 1;

					if (kit.Cooldown > 0)
						playerData.Cooldown = GetCurrentTime() + GetCooldown(kit.Cooldown, player);
				}
			}

			SendNotify(player, KitClaimed, 0, kit.DisplayName);

			Interface.CallHook("OnKitRedeemed", player, kit.Name);

			Log(player, kit.Name);

			if (usingUI)
			{
				if (_config.UI.CloseAfterReceive)
				{
					_openGUI.Remove(player);

					CuiHelper.DestroyUi(player, Layer);
				}
				else
				{
					MainUi(player, targetId, page, showAll);
				}
			}

			return true;
		}

		private const int _itemsPerTick = 10;

		private IEnumerator GiveKitItems(BasePlayer player, Kit kit)
		{
			for (var index = 0; index < kit.Items.Count; index++)
			{
				kit.Items[index]?.Get(player);

				if (index % _itemsPerTick == 0)
					yield return CoroutineEx.waitForEndOfFrame;
			}
		}

		private void FastGiveKitItems(BasePlayer player, Kit kit)
		{
			kit.Items.ForEach(item => item?.Get(player));
		}

		private double GetCooldown(double cooldown, BasePlayer player)
		{
			var cd = Interface.CallHook("OnKitCooldown", player, cooldown);
			return cd != null ? Convert.ToDouble(cd) : cooldown;
		}

		private List<KitItem> GetPlayerItems(BasePlayer player)
		{
			var kititems = new List<KitItem>();

			player.inventory.containerWear.itemList.ForEach(item =>
			{
				if (item == null || item.IsLocked()) return;
				kititems.Add(ItemToKit(item, "wear"));
			});

			player.inventory.containerMain.itemList.ForEach(item =>
			{
				if (item == null || item.IsLocked()) return;
				kititems.Add(ItemToKit(item, "main"));
			});

			player.inventory.containerBelt.itemList.ForEach(item =>
			{
				if (item == null || item.IsLocked()) return;
				kititems.Add(ItemToKit(item, "belt"));
			});

			return kititems;
		}

		private KitItem ItemToKit(Item item, string container)
		{
			var kitem = new KitItem
			{
				Amount = item.amount,
				Container = container,
				SkinID = item.skin,
				Blueprint = item.blueprintTarget,
				ShortName = item.info.shortname,
				DisplayName = !string.IsNullOrEmpty(item.name) ? item.name : string.Empty,
				Condition = item.condition,
				Weapon = null,
				Content = null,
				Chance = 100,
				Command = string.Empty,
				Position = item.position,
				Text = item.text
			};

			if (item.info.category == ItemCategory.Weapon)
			{
				var weapon = item.GetHeldEntity() as BaseProjectile;
				if (weapon != null)
					kitem.Weapon = new Weapon
					{
						ammoType = weapon.primaryMagazine.ammoType.shortname,
						ammoAmount = weapon.primaryMagazine.contents
					};
			}

			if (item.contents != null)
				kitem.Content = item.contents.itemList.Select(cont => new ItemContent
				{
					Amount = cont.amount,
					Condition = cont.condition,
					ShortName = cont.info.shortname
				}).ToList();

			return kitem;
		}

		#endregion

		#region Utils

		#region Find Kits

		private Dictionary<string, int> _kitByName = new Dictionary<string, int>();
		
		private Dictionary<int, int> _kitByID = new Dictionary<int, int>();

		private Kit FindKitByName(string name)
		{
			int index;
			return _kitByName.TryGetValue(name, out index) ? _data.Kits[index] : null;
		}
		
		private Kit FindKitByID(int id)
		{
			int index;
			return _kitByID.TryGetValue(id, out index) ? _data.Kits[index] : null;
		}
		
		#endregion
		
		#region Wipe

		private Coroutine _wipePlayers;

		private IEnumerator StartOnAllPlayers(string[] players, Action<string> callback = null)
		{
			for (var i = 0; i < players.Length; i++)
			{
				callback?.Invoke(players[i]);

				if (i % 10 == 0)
					yield return CoroutineEx.waitForFixedUpdate;
			}

			_wipePlayers = null;
		}

		private void DoWipePlayers()
		{
			try
			{
				var players = PlayerData.GetFiles();
				if (players != null && players.Length > 0)
				{
					_wipePlayers =
						ServerMgr.Instance.StartCoroutine(StartOnAllPlayers(players,
							PlayerData.DoWipe));

					_usersData?.Clear();
				}
			}
			catch (Exception e)
			{
				PrintError($"[On Server Wipe] in wipe players, error: {e.Message}");
			}
		}

		#endregion

		private void LoadKits()
		{
			for (var index = 0; index < _data.Kits.Count; index++)
			{
				var kit = _data.Kits[index];
				
				kit.Update();
				kit.ID = _lastKitID++;
				
				_kitByName[kit.Name] = index;
				_kitByID[kit.ID] = index;
			}
		}

		private void RegisterPermissions()
		{
			permission.RegisterPermission(PermAdmin, this);

			if (!permission.PermissionExists(_config.ChangeAutoKitPermission))
				permission.RegisterPermission(_config.ChangeAutoKitPermission, this);

			_data.Kits.ForEach(kit =>
			{
				var perm = kit.Permission;

				if (!string.IsNullOrEmpty(perm) && !permission.PermissionExists(perm))
					permission.RegisterPermission(perm, this);
			});
		}

		private void RegisterCommands()
		{
			AddCovalenceCommand(_config.Commands, nameof(CmdOpenKits));
		}

		private bool RaidBlocked(BasePlayer player)
		{
			return Convert.ToBoolean(NoEscape?.Call("IsRaidBlocked", player) ?? false);
		}

		private bool CombatBlocked(BasePlayer player)
		{
			return Convert.ToBoolean(NoEscape?.Call("IsCombatBlocked", player) ?? false);
		}

		private void StopEditing(BasePlayer player)
		{
			_itemEditing.Remove(player.userID);
			_kitEditing.Remove(player.userID);
		}

		private void FillCategories()
		{
			ItemManager.itemList.ForEach(item =>
			{
				var itemCategory = item.category.ToString();

				var kvp = new KeyValuePair<int, string>(item.itemid, item.shortname);

				if (_itemsCategories.ContainsKey(itemCategory))
				{
					if (!_itemsCategories[itemCategory].Contains(kvp))
						_itemsCategories[itemCategory].Add(kvp);
				}
				else
				{
					_itemsCategories.Add(itemCategory, new List<KeyValuePair<int, string>> {kvp});
				}
			});
		}

		private void HandleUi()
		{
			_toRemove.Clear();

			foreach (var check in _openGUI)
			{
				var player = check.Key;
				if (player == null || !player.IsConnected)
				{
					_toRemove.Add(player);
					continue;
				}

				var container = new CuiElementContainer();

				check.Value.ForEach(kit => RefreshKitUi(ref container, player, kit));

				CuiHelper.AddUi(player, container);
			}

			_toRemove.ForEach(x => _openGUI.Remove(x));
		}

		private void FixItemsPositions()
		{
			_data.Kits.ForEach(kit =>
			{
				var positions = new Dictionary<string, int>
				{
					["belt"] = 0,
					["main"] = 0,
					["wear"] = 0
				};

				kit.Items.ForEach(item =>
				{
					if (positions.ContainsKey(item.Container) && item.Position == -1)
					{
						item.Position = positions[item.Container];

						positions[item.Container] += 1;
					}
				});
			});

			SaveKits();
		}

		private void LoadImages()
		{
			if (!ImageLibrary)
			{
				BroadcastILNotInstalled();
			}
			else
			{
				_enabledImageLibrary = true;

				var imagesList = new Dictionary<string, string>();

				_data.Kits.ForEach(kit =>
				{
					if (!string.IsNullOrEmpty(kit.Image))
						imagesList.TryAdd(kit.Image, kit.Image);

					kit.Items.ForEach(item =>
					{
						if (!string.IsNullOrEmpty(item.Image))
							imagesList.TryAdd(item.Image, item.Image);
					});
				});

				if (_config.UI.Logo.Enabled && !string.IsNullOrEmpty(_config.UI.Logo.Image))
					imagesList.TryAdd(_config.UI.Logo.Image, _config.UI.Logo.Image);

				ImageLibrary?.Call("ImportImageList", Title, imagesList, 0UL, true);
			}
		}

		private void BroadcastILNotInstalled()
		{
			for (var i = 0; i < 5; i++) PrintError("IMAGE LIBRARY IS NOT INSTALLED.");
		}

		private static string HexToCuiColor(string hex, float alpha = 100)
		{
			if (string.IsNullOrEmpty(hex)) hex = "#FFFFFF";

			var str = hex.Trim('#');
			if (str.Length != 6) throw new Exception(hex);
			var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
			var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
			var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);

			return $"{(double) r / 255} {(double) g / 255} {(double) b / 255} {alpha / 100f}";
		}

		private static string FormatShortTime(TimeSpan time)
		{
			return time.ToShortString();
		}

		private static void CreateOutLine(ref CuiElementContainer container, string parent, string color,
			float size = 2)
		{
			container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0 0",
						AnchorMax = "1 0",
						OffsetMin = $"{size} 0",
						OffsetMax = $"-{size} {size}"
					},
					Image = {Color = color}
				},
				parent);
			container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0 1",
						AnchorMax = "1 1",
						OffsetMin = $"{size} -{size}",
						OffsetMax = $"-{size} 0"
					},
					Image = {Color = color}
				},
				parent);
			container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "0 1",
						OffsetMin = "0 0",
						OffsetMax = $"{size} 0"
					},
					Image = {Color = color}
				},
				parent);
			container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "1 0",
						AnchorMax = "1 1",
						OffsetMin = $"-{size} 0",
						OffsetMax = "0 0"
					},
					Image = {Color = color}
				},
				parent);
		}

		private List<Kit> GetAvailableKits(BasePlayer player, string targetId = "0", bool showAll = false,
			bool checkAmount = true, bool gui = false)
		{
			return showAll && IsAdmin(player)
				? _data.Kits
				: _data.Kits.FindAll(kit =>
				{
					if (kit.Hide || (gui && _config.KitsHidden.Contains(kit.Name)))
						return false;

					var data = PlayerData.GetOrCreateKitData(player.UserIDString, kit.Name);

					if (targetId != "0" && !(_config.NpcKits.ContainsKey(targetId) &&
					                         _config.NpcKits[targetId].Kits.Contains(kit.Name)))
						return false;

					if (checkAmount)
						if (!_config.ShowUsesEnd)
							if (!(kit.Amount == 0 || (kit.Amount > 0 &&
							                          (data?.Amount ?? 0) < kit.Amount)))
								return false;

					return _config.ShowAllKits ||
					       string.IsNullOrEmpty(kit.Permission) ||
					       permission.UserHasPermission(player.UserIDString, kit.Permission) ||
					       (data != null && data.HasAmount > 0);
				});
		}

		private List<Kit> GetAutoKits(BasePlayer player)
		{
			return _data.Kits
				.FindAll(kit => kit.Name == "autokit" || (_config.AutoKits.Contains(kit.Name) &&
				                                          (string.IsNullOrEmpty(kit.Permission) ||
				                                           player.HasPermission(kit.Permission))));
		}


		private int SecondsFromWipe()
		{
			return (int) DateTime.UtcNow
				.Subtract(SaveRestore.SaveCreatedTime.ToUniversalTime()).TotalSeconds;
		}

		private double LeftWipeBlockTime(double cooldown)
		{
			var leftTime = cooldown - SecondsFromWipe();
			return Math.Max(leftTime, 0);
		}

		private double UnBlockTime(double amount)
		{
			return TimeSpan.FromTicks(SaveRestore.SaveCreatedTime.ToUniversalTime().Ticks).TotalSeconds + amount;
		}

		private static double GetCurrentTime()
		{
			return TimeSpan.FromTicks(DateTime.UtcNow.Ticks).TotalSeconds;
		}

		private bool IsAdmin(BasePlayer player)
		{
			return player != null && ((player.IsAdmin && _config.FlagAdmin) || player.HasPermission(PermAdmin));
		}

		private void UpdateOpenedUI(string id, string permName)
		{
			if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(permName) ||
			    !_data.Kits.Exists(x => x.Permission.Equals(permName))) return;

			var player = BasePlayer.Find(id);
			if (player == null) return;

			if (_openGUI.ContainsKey(player)) MainUi(player);
		}

		#endregion

		#region Log

		private void Log(BasePlayer player, string kitname)
		{
			if (player == null) return;

			var text = $"{player.displayName}[{player.UserIDString}] - Received Kit: {kitname}";

			if (_config.Logs.Console)
				Puts(text);

			if (_config.Logs.Console)
				LogToFile(Name, $"[{DateTime.Now}] {text}", this);
		}

		#endregion

		#region Lang

		private const string
			NoILError = "NoILError",
			KitShowInfo = "KitShowInfo",
			KitYouHave = "KitYouHave",
			EditRemoveField = "EditRemoveField",
			ChangeAutoKitOn = "ChangeAutoKitOn",
			ChangeAutoKitOff = "ChangeAutoKitOff",
			NoEscapeCombatBlocked = "NoEscapeCombatBlocked",
			NoEscapeRaidBlocked = "NoEscapeRaidBlocked",
			NotMoney = "NotMoney",
			PriceFormat = "PriceFormat",
			KitExist = "KitExist",
			KitNotExist = "KitNotExist",
			KitRemoved = "KitRemoved",
			AccessDenied = "AccessDenied",
			KitLimit = "KitLimit",
			KitCooldown = "KitCooldown",
			KitCreate = "KitCreate",
			KitClaimed = "KitClaimed",
			NotEnoughSpace = "NotEnoughtSpace",
			NotifyTitle = "NotifyTitle",
			Close = "Close",
			MainTitle = "MainTitle",
			Back = "Back",
			Next = "Next",
			NotAvailableKits = "NoAvailabeKits",
			CreateKit = "CreateKit",
			ListKits = "ListKits",
			ShowAll = "ShowAll",
			KitInfo = "KitInfo",
			KitTake = "KitGet",
			ComeBack = "ComeBack",
			Edit = "Edit",
			ContainerMain = "ContainerMain",
			ContainerWear = "ContaineWear",
			ContainerBelt = "ContainerBelt",
			CreateOrEditKit = "CreateOrEditKit",
			MainMenu = "MainMenu",
			EnableKit = "EnableKit",
			AutoKit = "AutoKit",
			EnabledSale = "EnabledSale",
			SaveKit = "SaveKit",
			CopyItems = "CopyItems",
			RemoveKit = "RemoveKit",
			EditingTitle = "EditingTitle",
			ItemName = "ItemName",
			CmdName = "CmdName",
			BtnSelect = "BtnSelect",
			BluePrint = "BluePrint",
			BtnSave = "BtnSave",
			ItemSearch = "ItemSearch",
			BtnClose = "BtnClose",
			KitAvailableTitle = "KitAvailable",
			KitsList = "KitsList",
			KitsHelp = "KitsHelp",
			KitNotFound = "KitNotFound",
			RemoveItem = "RemoveItem",
			NoPermission = "NoPermission",
			BuildError = "BuildError",
			BBlocked = "BuildingBlocked",
			NoPermissionDescription = "NoPermissionDescription";

		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				[KitExist] = "Kit with the same name already exist",
				[KitCreate] = "You have created a new kit - {0}",
				[KitNotExist] = "This kit doesn't exist",
				[KitRemoved] = "Kit {0} was removed",
				[AccessDenied] = "Access denied",
				[KitLimit] = "Usage limite reached",
				[KitCooldown] = "You will be able to use this kit after: {0}",
				[NotEnoughSpace] = "Can't redeem kit. Not enought space",
				[KitClaimed] = "You have claimed kit - {0}",
				[NotifyTitle] = "KITS",
				[Close] = "✕",
				[MainTitle] = "Kits",
				[Back] = "Back",
				[Next] = "Next",
				[NotAvailableKits] = "NO KITS AVAILABLE FOR YOU :(",
				[CreateKit] = "Create Kit",
				[ListKits] = "List of kits",
				[ShowAll] = "Show all",
				[KitInfo] = "i",
				[KitTake] = "Take",
				[ComeBack] = "Come back",
				[Edit] = "Edit",
				[ContainerMain] = "Main",
				[ContainerWear] = "Wear",
				[ContainerBelt] = "Belt",
				[CreateOrEditKit] = "Create/Edit Kit",
				[MainMenu] = "Main menu",
				[EnableKit] = "Enable kit",
				[AutoKit] = "Auto kit",
				[EnabledSale] = "Enable sale",
				[SaveKit] = "Save kit",
				[CopyItems] = "Copy items from inventory",
				[RemoveKit] = "Remove kit",
				[EditingTitle] = "Item editing",
				[ItemName] = "Item",
				[CmdName] = "Command",
				[BtnSelect] = "Select",
				[BluePrint] = "Blueprint",
				[BtnSave] = "Save",
				[ItemSearch] = "Item search",
				[BtnClose] = "CLOSE",
				[KitAvailableTitle] = "KIT AVAILABLE\nTO RECEIVE",
				[KitsList] = "List of kits: {0}",
				[KitsHelp] =
					"KITS HELP\n- /{0} help - get help with kits\n- /{0} list - get a list of available kits\n- /{0} [name] - get the kit",
				[KitNotFound] = "Kit '{0}' not found",
				[RemoveItem] = "✕",
				[NoPermission] = "You don't have permission to get this kit",
				[BuildError] = "Can't place the building here",
				[BBlocked] = "Cannot do that while building blocked.",
				[NoPermissionDescription] = "PURCHASE THIS KIT AT\nSERVERNAME.GG",
				[PriceFormat] = "{0}$",
				[NotMoney] = "You don't have enough money!",
				[NoEscapeRaidBlocked] = "You cannot take this kit when you are raid blocked",
				[NoEscapeCombatBlocked] = "You cannot take this kit when you are combat blocked",
				[ChangeAutoKitOn] = "You have enabled autokits",
				[ChangeAutoKitOff] = "You have disabled autokits",
				[EditRemoveField] = "✕",
				[KitYouHave] = "YOU HAVE",
				[KitShowInfo] = "Show Info",
				[NoILError] = "The plugin does not work correctly, contact the administrator!"
			}, this);

			lang.RegisterMessages(new Dictionary<string, string>
			{
				[KitExist] = "Набор с похожим названием уже существует",
				[KitCreate] = "Вы создали новый набор - {0}",
				[KitNotExist] = "Набор не найден",
				[KitRemoved] = "Набор {0} удалён",
				[AccessDenied] = "Доступ запрещён",
				[KitLimit] = "Достигнут лимит использования",
				[KitCooldown] = "Вы сможете использовать этот набор после: {0}",
				[NotEnoughSpace] = "Невозможно получить набор. Не достаточно места в инвентаре",
				[KitClaimed] = "Вы получили набор - {0}",
				[NotifyTitle] = "KITS",
				[Close] = "✕",
				[MainTitle] = "Наборы",
				[Back] = "Назад",
				[Next] = "Вперёд",
				[NotAvailableKits] = "ДЛЯ ВАС НЕТ ДОСТУПНЫХ НАБОРОВ :(",
				[CreateKit] = "Создать набор",
				[ListKits] = "Список наборов",
				[ShowAll] = "Показать все",
				[KitInfo] = "i",
				[KitTake] = "Получить",
				[ComeBack] = "Назад",
				[Edit] = "Редактировать",
				[ContainerMain] = "Основной",
				[ContainerWear] = "Одежда",
				[ContainerBelt] = "Пояс",
				[CreateOrEditKit] = "Создать/Изменить Набор",
				[MainMenu] = "Основное меню",
				[EnableKit] = "Включить",
				[AutoKit] = "Автокит",
				[EnabledSale] = "Включить продажу",
				[SaveKit] = "Сохранить набор",
				[CopyItems] = "Копировать предметы из инвентаря",
				[RemoveKit] = "Удалить набор",
				[EditingTitle] = "Редактирование предмета",
				[ItemName] = "Item",
				[CmdName] = "Command",
				[BtnSelect] = "Выбрать",
				[BluePrint] = "Blueprint",
				[BtnSave] = "Сохранить",
				[ItemSearch] = "Поиск предмета",
				[BtnClose] = "ЗАКРЫТЬ",
				[KitAvailableTitle] = "НАБОР ДОСТУПЕН\nДЛЯ ПОЛУЧЕНИЯ",
				[KitsList] = "Список наборов: {0}",
				[KitsHelp] =
					"ИНФОРМАЦИЯ О НАБОРАх\n- /{0} help - получить информацию о наборах\n- /{0} list - получить список доступных наборов\n- /{0} [name] - получить набор",
				[KitNotFound] = "Набор '{0}' не найден",
				[RemoveItem] = "✕",
				[NoPermission] = "У вас нет прав на получение этого набора",
				[BuildError] = "Здесть невозможность установить строение",
				[BBlocked] = "Получение набора в Building Block запрещено!",
				[NoPermissionDescription] = "КУПИТЕ ЭТОТ НАБОР НА\nSERVERNAME.GG",
				[PriceFormat] = "{0}$",
				[NotMoney] = "У вас недостаточно денег!",
				[NoEscapeRaidBlocked] = "У вас блокировка рейда! Вы не можете взять этот набор",
				[NoEscapeCombatBlocked] = "У вас блокировка боя! Вы не можете взять этот набор",
				[ChangeAutoKitOn] = "Вы включили автокиты",
				[ChangeAutoKitOff] = "Вы выключили автокиты",
				[EditRemoveField] = "✕",
				[KitYouHave] = "У ВАС ЕСТЬ",
				[KitShowInfo] = "ПОКАЗАТЬ ИНФО",
				[NoILError] = "Плагин работает некорректно, свяжитесь с администратором!"
			}, this, "ru");
		}

		private string Msg(string key, string userid = null, params object[] obj)
		{
			return string.Format(lang.GetMessage(key, this, userid), obj);
		}

		private string Msg(BasePlayer player, string key, params object[] obj)
		{
			return string.Format(lang.GetMessage(key, this, player.UserIDString), obj);
		}

		private void Reply(BasePlayer player, string key, params object[] obj)
		{
			SendReply(player, Msg(key, player.UserIDString, obj));
		}

		private string Msg(BasePlayer player, string kitName, string key, params object[] obj)
		{
			if (_config.CustomTitles.Enabled)
			{
				CustomTitles.KitTitle kitTitle;
				if (_config.CustomTitles.KitTitles.TryGetValue(kitName, out kitTitle) && kitTitle.Enabled)
				{
					CustomTitles.TitleConf titleConf;
					if (kitTitle.Titles.TryGetValue(key, out titleConf) && titleConf.Enabled)
					{
						var msg = titleConf.GetMessage(player);
						if (!string.IsNullOrEmpty(msg)) return string.Format(msg, obj);
					}
				}
			}

			return Msg(player, key, obj);
		}

		private void SendNotify(BasePlayer player, string key, int type, params object[] obj)
		{
			if (_config.UseNotify && (Notify != null || UINotify != null))
				Interface.Oxide.CallHook("SendNotify", player, type, Msg(player, key, obj));
			else
				Reply(player, key, obj);
		}

		private void SendNotifyOrUI(BasePlayer player, string key, int type, bool chat, params object[] obj)
		{
			if (_config.UseNotify && (Notify != null || UINotify != null))
				Interface.Oxide.CallHook("SendNotify", player, type, Msg(player, key, obj));
			else if (chat)
				Reply(player, key, obj);
			else
				ErrorUi(player, Msg(player, key, obj));
		}

		#endregion

		#region API

		private bool TryClaimKit(BasePlayer player, string name, bool usingUI)
		{
			return !string.IsNullOrEmpty(name) &&
			       GiveKitToPlayer(player, FindKitByName(name), false, usingUI: usingUI);
		}

		private void GetKitNames(List<string> list)
		{
			list.AddRange(GetAllKits());
		}

		private string[] GetAllKits()
		{
			return _data.Kits.Select(kit => kit.Name).ToArray();
		}

		private object GetKitInfo(string name)
		{
			var kit = FindKitByName(name);
			if (kit == null) return null;

			var obj = new JObject
			{
				["name"] = kit.Name,
				["permission"] = kit.Permission,
				["max"] = kit.Amount,
				["image"] = kit.Image,
				["hide"] = kit.Hide,
				["description"] = kit.Description,
				["cooldown"] = kit.Cooldown,
				["building"] = kit.Building,
				["authlevel"] = 0,
				["items"] = new JArray(kit.Items.Select(itemData => new JObject()
				{
					["amount"] = itemData.Amount,
					["container"] = itemData.Container,
					["itemid"] = itemData.itemId,
					["skinid"] = itemData.SkinID,
					["weapon"] = !string.IsNullOrEmpty(itemData.Weapon?.ammoType),
					["blueprint"] = itemData.Blueprint,
					["mods"] = new JArray(itemData.Content?.Select(x =>
						ItemManager.FindItemDefinition(x.ShortName).itemid) ?? new List<int>())
				}))
			};

			return obj;
		}

		private string[] GetKitContents(string name)
		{
			var kit = FindKitByName(name);
			if (kit == null) return null;

			var items = new List<string>();
			foreach (var item in kit.Items)
			{
				var itemstring = $"{item.ShortName}_{item.Amount}";
				if (item.Content.Count > 0)
					itemstring = item.Content.Aggregate(itemstring, (current, mod) => current + $"_{mod.ShortName}");

				items.Add(itemstring);
			}

			return items.ToArray();
		}

		private double GetKitCooldown(string name)
		{
			return FindKitByName(name)?.Cooldown ?? 0;
		}

		private double PlayerKitCooldown(ulong ID, string name)
		{
			return PlayerData.GetNotLoadKitData(ID.ToString(), name)?.Cooldown ?? 0.0;
		}

		private int KitMax(string name)
		{
			return FindKitByName(name)?.Amount ?? 0;
		}

		private double PlayerKitMax(ulong ID, string name)
		{
			return PlayerData.GetNotLoadKitData(ID.ToString(), name)?.Amount ?? 0;
		}

		private string KitImage(string name)
		{
			return FindKitByName(name)?.Image ?? string.Empty;
		}

		private string GetKitImage(string name)
		{
			return KitImage(name);
		}

		private string GetKitDescription(string name)
		{
			return FindKitByName(name)?.Description ?? string.Empty;
		}

		private int GetKitMaxUses(string name)
		{
			return FindKitByName(name)?.Amount ?? 0;
		}

		private int GetPlayerKitUses(ulong userId, string name)
		{
			return GetPlayerKitUses(userId.ToString(), name);
		}

		private int GetPlayerKitUses(string userId, string name)
		{
			var kitData = PlayerData.GetNotLoadKitData(userId, name);
			return kitData?.Amount ?? 0;
		}

		private void SetPlayerKitUses(ulong userId, string name, int amount)
		{
			var data = PlayerData.GetOrCreateKitData(userId.ToString(), name);
			if (data == null) return;

			data.Amount = amount;
		}

		private double GetPlayerKitCooldown(string userId, string name)
		{
			return GetPlayerKitCooldown(ulong.Parse(userId), name);
		}

		private double GetPlayerKitCooldown(ulong userId, string name)
		{
			var data = PlayerData.GetNotLoadKitData(userId.ToString(), name);
			if (data == null) return 0;

			return Mathf.Max((float) (data.Cooldown - GetCurrentTime()), 0f);
		}

		private void SetPlayerCooldown(ulong userId, string name, int amount)
		{
			var data = PlayerData.GetOrCreateKitData(userId.ToString(), name);
			if (data == null) return;

			data.Cooldown = GetCurrentTime() + GetCooldown(amount, RelationshipManager.FindByID(userId));
		}

		private bool GiveKit(BasePlayer player, string name, bool usingUI)
		{
#if TESTING

			Puts($"[GiveKit] player={player?.UserIDString}, name={name}, usingUI={usingUI}");
#endif
			return GiveKitToPlayer(player, FindKitByName(name), true, usingUI: usingUI);
		}

		private bool isKit(string name)
		{
			return IsKit(name);
		}

		private bool IsKit(string name)
		{
			return _data.Kits.Exists(x => x.Name == name);
		}

		private bool HasKitAccess(string userId, string name)
		{
			var kit = FindKitByName(name);
			if (kit == null)
				return false;

			return string.IsNullOrWhiteSpace(kit.Permission) || permission.UserHasPermission(userId, kit.Permission);
		}

		private int GetPlayerKitAmount(string userId, string name)
		{
			var kit = FindKitByName(name);
			if (kit == null)
				return 0;

			var playerData = PlayerData.GetNotLoadKitData(userId, name);
			if (playerData != null && playerData.HasAmount > 0)
				return playerData.HasAmount;

			return 0;
		}

		private JObject GetKitObject(string name)
		{
			return FindKitByName(name)?.ToJObject;
		}

		#endregion

		#region Convert

		#region uMod Kits

		[ConsoleCommand("kits.convert")]
		private void OldKitsConvert(ConsoleSystem.Arg arg)
		{
			if (!arg.IsAdmin) return;

			OldData oldKits = null;

			try
			{
				oldKits = Interface.Oxide.DataFileSystem.ReadObject<OldData>("Kits/kits_data");
			}
			catch (Exception e)
			{
				PrintError(e.ToString());
			}

			var amount = 0;

			oldKits?._kits.ToList().ForEach(oldKit =>
			{
				var kit = new Kit
				{
					ID = ++_lastKitID,
					Name = oldKit.Value.Name,
					DisplayName = oldKit.Value.Name,
					Permission = oldKit.Value.RequiredPermission,
					Amount = oldKit.Value.MaximumUses,
					Cooldown = oldKit.Value.Cooldown,
					Description = oldKit.Value.Description,
					Hide = oldKit.Value.IsHidden,
					Building = oldKit.Value.CopyPasteFile,
					Image = oldKit.Value.KitImage,
					Color = _config.KitColor,
					Items = new List<KitItem>()
				};

				foreach (var item in oldKit.Value.MainItems)
					kit.Items.Add(KitItem.FromOld(item, "main"));

				foreach (var item in oldKit.Value.WearItems)
					kit.Items.Add(KitItem.FromOld(item, "wear"));

				foreach (var item in oldKit.Value.BeltItems)
					kit.Items.Add(KitItem.FromOld(item, "belt"));

				_data.Kits.Add(kit);

				var kitIndex = _data.Kits.IndexOf(kit);
				
				_kitByName[kit.Name] = kitIndex;
				_kitByID[kit.ID] = kitIndex;
				
				amount++;
			});

			Puts($"{amount} kits was converted!");

			SaveKits();
		}

		private class OldData
		{
			[JsonProperty] public Dictionary<string, OldKitsData> _kits =
				new Dictionary<string, OldKitsData>(StringComparer.OrdinalIgnoreCase);
		}

		private class OldKitsData
		{
			public string Name;
			public string Description;
			public string RequiredPermission;

			public int MaximumUses;
			public int RequiredAuth;
			public int Cooldown;
			public int Cost;

			public bool IsHidden;

			public string CopyPasteFile;
			public string KitImage;

			public ItemData[] MainItems;
			public ItemData[] WearItems;
			public ItemData[] BeltItems;
		}

		private class ItemData
		{
			public string Shortname;

			public ulong Skin;

			public int Amount;

			public float Condition;

			public float MaxCondition;

			public int Ammo;

			public string Ammotype;

			public int Position;

			public int Frequency;

			public string BlueprintShortname;

			public ItemData[] Contents;
		}

		#endregion

		#region Old Data

		private void StartConvertOldData()
		{
			var data = LoadOldData();
			if (data != null)
				timer.In(0.3f, () =>
				{
					ConvertOldData(data);

					PrintWarning($"{data.Count} players was converted!");
				});
		}

		private Dictionary<ulong, Dictionary<string, OldKitData>> LoadOldData()
		{
			Dictionary<ulong, Dictionary<string, OldKitData>> players = null;
			try
			{
				players =
					Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, OldKitData>>>(
						$"{Name}/Data");
			}
			catch (Exception e)
			{
				PrintError(e.ToString());
			}

			return players ?? new Dictionary<ulong, Dictionary<string, OldKitData>>();
		}

		private void ConvertOldData(Dictionary<ulong, Dictionary<string, OldKitData>> players)
		{
			foreach (var check in players)
			{
				var userId = check.Key.ToString();

				var data = PlayerData.GetOrCreate(userId);

				foreach (var kitData in check.Value)
					data.Kits[kitData.Key] = new PlayerData.KitData
					{
						Amount = kitData.Value.Amount,
						Cooldown = kitData.Value.Cooldown,
						HasAmount = kitData.Value.HasAmount
					};

				PlayerData.SaveAndUnload(userId);
			}
		}

		#region Classes

		private class OldKitData
		{
			public int Amount;

			public double Cooldown;

			public int HasAmount;
		}

		#endregion

		#endregion

		#endregion

		#region Data 2.0

		private Dictionary<string, PlayerData> _usersData = new Dictionary<string, PlayerData>();

		private class PlayerData
		{
			#region Main

			#region Fields

			[JsonProperty(PropertyName = "Kits Data", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, KitData> Kits = new Dictionary<string, KitData>();

			#endregion

			#region Classes

			public class KitData
			{
				[JsonProperty(PropertyName = "Amount")]
				public int Amount;

				[JsonProperty(PropertyName = "Cooldown")]
				public double Cooldown;

				[JsonProperty(PropertyName = "HasAmount")]
				public int HasAmount;
			}

			#endregion

			#endregion

			#region Helpers

			private static string BaseFolder()
			{
				return "Kits" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar;
			}

			public static PlayerData GetOrLoad(string userId)
			{
				if (!userId.IsSteamId()) return null;

				return GetOrLoad(BaseFolder(), userId);
			}

			public static KitData GetOrLoadKitData(string userId,
				string kitName,
				bool addKit = true)
			{
				var data = GetOrLoad(userId);
				if (data == null) return null;

				KitData kitData;
				if (data.Kits.TryGetValue(kitName, out kitData))
					return kitData;

				return
					addKit
						? data.Kits[kitName] = new KitData()
						: null;
			}


			private static PlayerData GetOrLoad(string baseFolder, string userId, bool load = true)
			{
				PlayerData data;
				if (_instance._usersData.TryGetValue(userId, out data)) return data;

				try
				{
					data = ReadOnlyObject(baseFolder + userId);
				}
				catch (Exception e)
				{
					Interface.Oxide.LogError(e.ToString());
				}

				return load
					? _instance._usersData[userId] = data
					: data;
			}

			public static PlayerData GetOrCreate(string userId)
			{
				if (!userId.IsSteamId()) return null;

				return GetOrLoad(userId) ?? (_instance._usersData[userId] = new PlayerData());
			}

			public static KitData GetOrCreateKitData(string userId, string kitName)
			{
				var data = GetOrCreate(userId);
				if (data == null) return null;

				KitData kitData;
				if (data.Kits.TryGetValue(kitName, out kitData))
					return kitData;

				return data.Kits[kitName] = new KitData();
			}

			public static void Save()
			{
				foreach (var userId in _instance._usersData.Keys)
					Save(userId);
			}

			public static void Save(string userId)
			{
				PlayerData data;
				if (!_instance._usersData.TryGetValue(userId, out data))
					return;

				Interface.Oxide.DataFileSystem.WriteObject(BaseFolder() + userId, data);
			}

			public static void SaveAndUnload(string userId)
			{
				Save(userId);

				Unload(userId);
			}

			public static void Unload(string userId)
			{
				_instance._usersData.Remove(userId);
			}

			#endregion

			#region Utils

			public static string[] GetFiles()
			{
				return GetFiles(BaseFolder());
			}

			public static string[] GetFiles(string baseFolder)
			{
				try
				{
					var json = ".json".Length;
					var paths = Interface.Oxide.DataFileSystem.GetFiles(baseFolder);
					for (var i = 0; i < paths.Length; i++)
					{
						var path = paths[i];
						var separatorIndex = path.LastIndexOf(Path.DirectorySeparatorChar);

						// We have to do this since GetFiles returns paths instead of filenames
						// And other methods require filenames
						paths[i] = path.Substring(separatorIndex + 1, path.Length - separatorIndex - 1 - json);
					}

					return paths;
				}
				catch
				{
					return Array.Empty<string>();
				}
			}

			private static PlayerData ReadOnlyObject(string name)
			{
				return Interface.Oxide.DataFileSystem.ExistsDatafile(name)
					? Interface.Oxide.DataFileSystem.GetFile(name).ReadObject<PlayerData>()
					: null;
			}

			#endregion

			#region Wipe

			public static void DoWipe(string userId)
			{
				if (_instance?._config?.SaveGivenKitsOnWipe == true)
				{
					var data = GetNotLoad(userId);
					if (data == null)
					{
						return;
					}

					data.Kits.RemoveAll((key, value) => value.HasAmount <= 0);

					if (data.Kits.Count > 0)
					{
						foreach (var kitData in data.Kits)
						{
							kitData.Value.Amount = 0;
							kitData.Value.Cooldown = 0;
						}

						Interface.Oxide.DataFileSystem.WriteObject(BaseFolder() + userId, data);
					}
					else
					{
						Interface.Oxide.DataFileSystem.DeleteDataFile(BaseFolder() + userId);
					}
				}
				else
				{
					Interface.Oxide.DataFileSystem.DeleteDataFile(BaseFolder() + userId);
				}
			}

			#endregion

			#region All Players

			public static void StartAll(Action<PlayerData> action)
			{
				var users = GetFiles(BaseFolder());

				foreach (var userId in users)
				{
					var loaded = _instance._usersData.ContainsKey(userId);

					var data = GetOrLoad(userId);
					if (data == null) continue;

					action.Invoke(data);

					Save(userId);

					if (!loaded)
						Unload(userId);
				}
			}

			public static List<PlayerData> GetAll()
			{
				var users = GetFiles(BaseFolder());

				var list = new List<PlayerData>();

				foreach (var userId in users)
				{
					var data = GetNotLoad(userId);
					if (data == null) continue;

					list.Add(data);
				}

				return list;
			}

			public static PlayerData GetNotLoad(string userId)
			{
				return GetOrLoad(BaseFolder(), userId, false);
			}

			public static KitData GetNotLoadKitData(string userId, string kitName)
			{
				var data = GetNotLoad(userId);
				if (data == null) return null;

				KitData kitData;
				return data.Kits.TryGetValue(kitName, out kitData) ? kitData : null;
			}

			#endregion
		}

		#endregion
	}
}

#region Extension Methods

namespace Oxide.Plugins.KitsExtensionMethods
{
	// ReSharper disable ForCanBeConvertedToForeach
	// ReSharper disable LoopCanBeConvertedToQuery
	public static class ExtensionMethods
	{
		internal static Permission p;

		public static bool All<T>(this IList<T> a, Func<T, bool> b)
		{
			for (var i = 0; i < a.Count; i++)
				if (!b(a[i]))
					return false;
			return true;
		}

		public static int Average(this IList<int> a)
		{
			if (a.Count == 0) return 0;
			var b = 0;
			for (var i = 0; i < a.Count; i++) b += a[i];
			return b / a.Count;
		}

		public static T ElementAt<T>(this IEnumerable<T> a, int b)
		{
			using (var c = a.GetEnumerator())
			{
				while (c.MoveNext())
				{
					if (b == 0) return c.Current;
					b--;
				}
			}

			return default(T);
		}

		public static bool Exists<T>(this IEnumerable<T> a, Func<T, bool> b = null)
		{
			using (var c = a.GetEnumerator())
			{
				while (c.MoveNext())
					if (b == null || b(c.Current))
						return true;
			}

			return false;
		}

		public static T FirstOrDefault<T>(this IEnumerable<T> a, Func<T, bool> b = null)
		{
			using (var c = a.GetEnumerator())
			{
				while (c.MoveNext())
					if (b == null || b(c.Current))
						return c.Current;
			}

			return default(T);
		}

		public static int RemoveAll<T, V>(this IDictionary<T, V> a, Func<T, V, bool> b)
		{
			var c = new List<T>();
			using (var d = a.GetEnumerator())
			{
				while (d.MoveNext())
					if (b(d.Current.Key, d.Current.Value))
						c.Add(d.Current.Key);
			}

			c.ForEach(e => a.Remove(e));
			return c.Count;
		}

		public static IEnumerable<V> Select<T, V>(this IEnumerable<T> a, Func<T, V> b)
		{
			var c = new List<V>();
			using (var d = a.GetEnumerator())
			{
				while (d.MoveNext()) c.Add(b(d.Current));
			}

			return c;
		}

		public static List<TResult> Select<T, TResult>(this List<T> source, Func<T, TResult> selector)
		{
			if (source == null || selector == null) return new List<TResult>();

			var r = new List<TResult>(source.Count);
			for (var i = 0; i < source.Count; i++) r.Add(selector(source[i]));

			return r;
		}

		public static List<T> SkipAndTake<T>(this List<T> source, int skip, int take)
		{
			var index = Mathf.Min(Mathf.Max(skip, 0), source.Count);
			return source.GetRange(index, Mathf.Min(take, source.Count - index));
		}

		public static string[] Skip(this string[] a, int count)
		{
			if (a.Length == 0) return Array.Empty<string>();
			var c = new string[a.Length - count];
			var n = 0;
			for (var i = 0; i < a.Length; i++)
			{
				if (i < count) continue;
				c[n] = a[i];
				n++;
			}

			return c;
		}

		public static List<T> Skip<T>(this IList<T> source, int count)
		{
			if (count < 0)
				count = 0;

			if (source == null || count > source.Count)
				return new List<T>();

			var result = new List<T>(source.Count - count);
			for (var i = count; i < source.Count; i++)
				result.Add(source[i]);
			return result;
		}

		public static Dictionary<T, V> Skip<T, V>(
			this IDictionary<T, V> source,
			int count)
		{
			var result = new Dictionary<T, V>();
			using (var iterator = source.GetEnumerator())
			{
				for (var i = 0; i < count; i++)
					if (!iterator.MoveNext())
						break;

				while (iterator.MoveNext()) result.Add(iterator.Current.Key, iterator.Current.Value);
			}

			return result;
		}

		public static List<T> Take<T>(this IList<T> a, int b)
		{
			var c = new List<T>();
			for (var i = 0; i < a.Count; i++)
			{
				if (c.Count == b) break;
				c.Add(a[i]);
			}

			return c;
		}

		public static Dictionary<T, V> Take<T, V>(this IDictionary<T, V> a, int b)
		{
			var c = new Dictionary<T, V>();
			foreach (var f in a)
			{
				if (c.Count == b) break;
				c.Add(f.Key, f.Value);
			}

			return c;
		}

		public static Dictionary<T, V> ToDictionary<S, T, V>(this IEnumerable<S> a, Func<S, T> b, Func<S, V> c)
		{
			var d = new Dictionary<T, V>();
			using (var e = a.GetEnumerator())
			{
				while (e.MoveNext()) d[b(e.Current)] = c(e.Current);
			}

			return d;
		}

		public static List<T> ToList<T>(this IEnumerable<T> a)
		{
			var b = new List<T>();
			using (var c = a.GetEnumerator())
			{
				while (c.MoveNext()) b.Add(c.Current);
			}

			return b;
		}

		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> a)
		{
			return new HashSet<T>(a);
		}

		public static List<T> Where<T>(this List<T> source, Predicate<T> predicate)
		{
			if (source == null)
				return new List<T>();

			if (predicate == null)
				return new List<T>();

			return source.FindAll(predicate);
		}

		public static List<T> Where<T>(this List<T> source, Func<T, int, bool> predicate)
		{
			if (source == null)
				return new List<T>();

			if (predicate == null)
				return new List<T>();

			var r = new List<T>();
			for (var i = 0; i < source.Count; i++)
				if (predicate(source[i], i))
					r.Add(source[i]);
			return r;
		}

		public static List<T> Where<T>(this IEnumerable<T> source, Func<T, bool> predicate)
		{
			var c = new List<T>();

			using (var d = source.GetEnumerator())
			{
				while (d.MoveNext())
					if (predicate(d.Current))
						c.Add(d.Current);
			}

			return c;
		}

		public static List<T> OfType<T>(this IEnumerable<BaseNetworkable> a) where T : BaseEntity
		{
			var b = new List<T>();
			using (var c = a.GetEnumerator())
			{
				while (c.MoveNext())
					if (c.Current is T)
						b.Add(c.Current as T);
			}

			return b;
		}

		public static int Sum<T>(this IList<T> a, Func<T, int> b)
		{
			var c = 0;
			for (var i = 0; i < a.Count; i++)
			{
				var d = b(a[i]);
				if (!float.IsNaN(d)) c += d;
			}

			return c;
		}

		public static T LastOrDefault<T>(this List<T> source)
		{
			if (source == null || source.Count == 0)
				return default(T);

			return source[source.Count - 1];
		}

		public static int Count<T>(this List<T> source, Func<T, bool> predicate)
		{
			if (source == null)
				return 0;

			if (predicate == null)
				return 0;

			var count = 0;
			for (var i = 0; i < source.Count; i++)
				checked
				{
					if (predicate(source[i])) count++;
				}

			return count;
		}

		public static TAccumulate Aggregate<TSource, TAccumulate>(this List<TSource> source, TAccumulate seed,
			Func<TAccumulate, TSource, TAccumulate> func)
		{
			if (source == null) throw new Exception("Aggregate: source is null");

			if (func == null) throw new Exception("Aggregate: func is null");

			var result = seed;
			for (var i = 0; i < source.Count; i++) result = func(result, source[i]);
			return result;
		}

		public static int Sum(this IList<int> a)
		{
			var c = 0;
			for (var i = 0; i < a.Count; i++)
			{
				var d = a[i];
				if (!float.IsNaN(d)) c += d;
			}

			return c;
		}

		public static bool HasPermission(this string a, string b)
		{
			if (p == null) p = Interface.Oxide.GetLibrary<Permission>();
			return !string.IsNullOrEmpty(a) && p.UserHasPermission(a, b);
		}

		public static bool HasPermission(this BasePlayer a, string b)
		{
			return a.UserIDString.HasPermission(b);
		}

		public static bool HasPermission(this ulong a, string b)
		{
			return a.ToString().HasPermission(b);
		}

		public static bool IsReallyConnected(this BasePlayer a)
		{
			return a.IsReallyValid() && a.net.connection != null;
		}

		public static bool IsKilled(this BaseNetworkable a)
		{
			return (object) a == null || a.IsDestroyed;
		}

		public static bool IsNull<T>(this T a) where T : class
		{
			return a == null;
		}

		public static bool IsNull(this BasePlayer a)
		{
			return (object) a == null;
		}

		public static bool IsReallyValid(this BaseNetworkable a)
		{
			return !((object) a == null || a.IsDestroyed || a.net == null);
		}

		public static void SafelyKill(this BaseNetworkable a)
		{
			if (a.IsKilled()) return;
			a.Kill();
		}

		public static bool CanCall(this Plugin o)
		{
			return o != null && o.IsLoaded;
		}

		public static bool IsInBounds(this OBB o, Vector3 a)
		{
			return o.ClosestPoint(a) == a;
		}

		public static bool IsHuman(this BasePlayer a)
		{
			return !(a.IsNpc || !a.userID.IsSteamId());
		}

		public static BasePlayer ToPlayer(this IPlayer user)
		{
			return user.Object as BasePlayer;
		}

		public static List<TResult> SelectMany<TSource, TResult>(this List<TSource> source,
			Func<TSource, List<TResult>> selector)
		{
			if (source == null || selector == null)
				return new List<TResult>();

			var result = new List<TResult>(source.Count);
			source.ForEach(i => selector(i).ForEach(j => result.Add(j)));
			return result;
		}

		public static IEnumerable<TResult> SelectMany<TSource, TResult>(
			this IEnumerable<TSource> source,
			Func<TSource, IEnumerable<TResult>> selector)
		{
			using (var item = source.GetEnumerator())
			{
				while (item.MoveNext())
					using (var result = selector(item.Current).GetEnumerator())
					{
						while (result.MoveNext()) yield return result.Current;
					}
			}
		}

		public static int Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
		{
			var sum = 0;

			using (var element = source.GetEnumerator())
			{
				while (element.MoveNext()) sum += selector(element.Current);
			}

			return sum;
		}

		public static double Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
		{
			var sum = 0.0;

			using (var element = source.GetEnumerator())
			{
				while (element.MoveNext()) sum += selector(element.Current);
			}

			return sum;
		}

		public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		{
			if (source == null) return false;

			using (var element = source.GetEnumerator())
			{
				while (element.MoveNext())
					if (predicate(element.Current))
						return true;
			}

			return false;
		}

		public static List<TSource> OrderByDescending<TSource, TKey>(this List<TSource> source,
			Func<TSource, TKey> keySelector, IComparer<TKey> comparer = null)
		{
			if (source == null) return new List<TSource>();

			if (keySelector == null) return new List<TSource>();

			if (comparer == null) comparer = Comparer<TKey>.Default;

			var result = new List<TSource>(source);
			var lambdaComparer = new ReverseLambdaComparer<TSource, TKey>(keySelector, comparer);
			result.Sort(lambdaComparer);
			return result;
		}

		internal sealed class ReverseLambdaComparer<T, U> : IComparer<T>
		{
			private IComparer<U> comparer;
			private Func<T, U> selector;

			public ReverseLambdaComparer(Func<T, U> selector, IComparer<U> comparer)
			{
				this.comparer = comparer;
				this.selector = selector;
			}

			public int Compare(T x, T y)
			{
				return comparer.Compare(selector(y), selector(x));
			}
		}
	}
}

#endregion Extension Methods