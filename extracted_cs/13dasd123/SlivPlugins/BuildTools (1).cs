// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿// #define TESTING

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using Global = Rust.Global;
using Time = UnityEngine.Time;
using Utility = Oxide.Core.Utility;

#if TESTING
using System.Diagnostics;
#endif

namespace Oxide.Plugins
{
	[Info("Build Tools", "Mevent", "1.5.27")]
	public class BuildTools : RustPlugin
	{
		#region Fields

		[PluginReference] private Plugin
			ImageLibrary = null,
			NoEscape = null,
			Clans = null,
			Friends = null,
			PersonalVaultDoor = null,
			Notify = null,
			UINotify = null;

		private const string Layer = "UI.BuildTools";

		private const string CrosshairLayer = "UI.BuildTools.Sight";

		private static BuildTools _instance;

		private enum Types
		{
			None = 0,
			Remove = 5,
			Down = 6,
			Wood = 1,
			Stone = 2,
			Metal = 3,
			TopTier = 4
		}

		private const string PermFree = "buildtools.free";

		private const string HammerShortname = "hammer";

		private const string ToolGunShortname = "toolgun";
		
		private const string BuldingPlannerShortname = "building.planner";

		private Dictionary<string, string> _shortPrefabNamesToItem = new Dictionary<string, string>();

		private bool _needImageLibrary;

		private bool _enabledImageLibrary;

		private Dictionary<int, Mode> _modeByType = new Dictionary<int, Mode>();

		#endregion

		#region Config

		private static Configuration _config;

		private class Configuration
		{
			[JsonProperty(PropertyName = "Remove Commands")]
			public string[] RemoveCommands = {"remove"};

			[JsonProperty(PropertyName = "Upgrade Commands")]
			public string[] UpgradeCommands = {"up", "building.upgrade"};

			[JsonProperty(PropertyName = "Downgrade Commands")]
			public string[] DowngradeCommands = {"down", "down.grade"};

			[JsonProperty(PropertyName = "Work with Notify?")]
			public bool UseNotify = true;

			[JsonProperty(PropertyName = "Work with PersonalVaultDoor?")]
			public bool UsePersonalVaultDoor = true;

			[JsonProperty(PropertyName = "Use hammer hit actions on entity?")]
			public bool UseHammer = true;

			[JsonProperty(PropertyName = "Switching between modes with a middle click?")]
			public bool SwitchModesMiddleClick = false;
			
			[JsonProperty(PropertyName = "Setting Modes", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<Mode> Modes = new List<Mode>
			{
				new Mode
				{
					Type = Types.Remove,
					Icon = "assets/icons/clear.png",
					Permission = string.Empty,
					Additional = true,
					UseSkins = false,
					Skins = new List<SkinConf>(),
				},
				new Mode
				{
					Type = Types.Wood,
					Icon = "assets/icons/level_wood.png",
					Permission = string.Empty,
					Additional = false,
					UseSkins = false,
					Skins = new List<SkinConf>(),
				},
				new Mode
				{
					Type = Types.Stone,
					Icon = "assets/icons/level_stone.png",
					Permission = string.Empty,
					Additional = false,
					UseSkins = true,
					Skins = new List<SkinConf>
					{
						new SkinConf
						{
							Enabled = true,
							LangKey = "SkinAdobe",
							Permission = string.Empty,
							Skin = 10220
						},
						new SkinConf
						{
							Enabled = true,
							LangKey = "SkinBrick",
							Permission = string.Empty,
							Skin = 10223
						},
						new SkinConf
						{
							Enabled = true,
							LangKey = "SkinBrutalist",
							Permission = string.Empty,
							Skin = 10225
						}
					}
				},
				new Mode
				{
					Type = Types.Metal,
					Icon = "assets/icons/level_metal.png",
					Permission = string.Empty,
					Additional = false,
					UseSkins = true,
					Skins = new List<SkinConf>
					{
						new SkinConf
						{
							Enabled = true,
							LangKey = "SkinContainer",
							Permission = string.Empty,
							Skin = 10221
						}
					},
				},
				new Mode
				{
					Type = Types.TopTier,
					Icon = "assets/icons/level_top.png",
					Permission = string.Empty,
					Additional = false,
					UseSkins = false,
					Skins = new List<SkinConf>(),
				},
				new Mode
				{
					Type = Types.Down,
					Icon = "assets/icons/demolish.png",
					Permission = string.Empty,
					Additional = false,
					UseSkins = false,
					Skins = new List<SkinConf>(),
				}
			};

			[JsonProperty(PropertyName = "Upgrade Settings")]
			public UpgradeSettings Upgrade = new UpgradeSettings
			{
				PermissionToAll = "buildtools.all",
				ActionTime = 30,
				Cooldown = 0,
				VipCooldown = new Dictionary<string, int>
				{
					["buildtools.vip"] = 0,
					["buildtools.premium"] = 0
				},
				AfterWipe = 0,
				VipAfterWipe = new Dictionary<string, int>
				{
					["buildtools.vip"] = 0,
					["buildtools.premium"] = 0
				},
				AmountPerTick = 5,
				NotifyRequiredResources = false,
				ShiftAttackToUpgradeAll = false,
				UpdateSkinsOnRightClick = true
			};

			[JsonProperty(PropertyName = "Remove Settings")]
			public RemoveSettings Remove = new RemoveSettings
			{
				PermissionToAll = "buildtools.all",
				ActionTime = 30,
				Cooldown = 0,
				VipCooldown = new Dictionary<string, int>
				{
					["buildtools.vip"] = 0,
					["buildtools.premium"] = 0
				},
				AfterWipe = 0,
				VipAfterWipe = new Dictionary<string, int>
				{
					["buildtools.vip"] = 0,
					["buildtools.premium"] = 0
				},
				Condition = new ConditionSettings
				{
					Default = true,
					Percent = false,
					PercentValue = 0
				},
				ReturnItem = true,
				ReturnPercent = 100,
				ReturnPercents = new Dictionary<string, float>
				{
					["buildtools.vip"] = 100,
					["buildtools.premium"] = 100
				},
				CanFriends = true,
				CanClan = true,
				CanTeams = true,
				RemoveByCupboard = false,
				RemoveItemsContainer = false,
				BlockedList = new Dictionary<string, List<IgnoredBlock>>
				{
					["shortname 1"] = new List<IgnoredBlock>
					{
						new IgnoredBlock
						{
							Skins = new List<string> {"*"},
							CanRemove = false,
							ReturnItem = false,
							ReturnPercent = 100
						}
					},
					["shortname 2"] = new List<IgnoredBlock>
					{
						new IgnoredBlock
						{
							Skins = new List<string> {"*"},
							CanRemove = false,
							ReturnItem = false,
							ReturnPercent = 100
						}
					},
					["shortname 3"] = new List<IgnoredBlock>
					{
						new IgnoredBlock
						{
							Skins = new List<string> {"*"},
							CanRemove = false,
							ReturnItem = false,
							ReturnPercent = 100
						}
					},
				},
				BlockCooldown = new ActionCooldown
				{
					Default = 36000,
					Permissions = new Dictionary<string, float>
					{
						["buildtools.vip"] = 34000,
						["buildtools.premium"] = 32000
					}
				},
				AmountPerTick = 5,
				ShiftAttackToRemoveAll = false,
				Vision = new RemoveSettings.VisionRemoval
				{
					Enabled = false,
					Permission = "buildtools.vision",
					Distance = 5f,
					Crosshair = new CrosshairSettings
					{
						Enabled = false,
						Size = 30f,
						Thickness = 4f,
						Color = new IColor("#FFFFFF"),
						DisplayType = "Overlay"
					}
				}
			};

			[JsonProperty(PropertyName = "Downgrade Settings")]
			public DowngradeSettings Downgrade = new DowngradeSettings
			{
				PermissionToAll = "buildtools.all",
				ActionTime = 30,
				Cooldown = 0,
				VipCooldown = new Dictionary<string, int>
				{
					["buildtools.vip"] = 0,
					["buildtools.premium"] = 0
				},
				AfterWipe = 0,
				VipAfterWipe = new Dictionary<string, int>
				{
					["buildtools.vip"] = 0,
					["buildtools.premium"] = 0
				},
				CanFriends = true,
				CanClan = true,
				CanTeams = true,
				BlockedList = new Dictionary<string, List<string>>
				{
					["shortname 1"] = new List<string>
					{
						"*"
					},
					["shortname 2"] = new List<string>
					{
						"*"
					},
					["shortname 3"] = new List<string>
					{
						"*"
					}
				},
				AmountPerTick = 5,
				ShiftAttackToDowngradeAll = false
			};

			[JsonProperty(PropertyName = "Block Settings")]
			public BlockSettings Block = new BlockSettings
			{
				UseNoEscape = true,
				UseClans = true,
				UseFriends = true,
				UseCupboard = true,
				NeedCupboard = false
			};

			[JsonProperty(PropertyName = "UI Settings")]
			public InterfaceSettings UI = new InterfaceSettings
			{
				DisplayType = "Overlay",
				Color1 = new IColor("#4B68FF"),
				Color2 = new IColor("#2C2C2C"),
				Color3 = new IColor("#B64040"),
				OffsetY = 0,
				OffsetX = 0,
				ProgressTitleTextSize = 12,
				ProgressTitleTextColor = new IColor("#FFFFFF", 60),
				SettingsBackgroundColor = new IColor("#0E0E10"),
				SettingsHeaderColor = new IColor("#161617"),
				SettingsImageBackgroundColor = new IColor("#161617"),
				SettingsSelectedColor = new IColor("#4B68FF"),
				SettingsNotSelectedColor = new IColor("#242425"),
				SettingsTextColor = new IColor("#FFFFFF"),
				Skins = new InterfaceSettings.SkinsUI
				{
					TabUpIndent = 15,
					TabHeight = 20,
					TabWidth = 80,
					TabMargin = 5,
					ImageLeftIndent = -22.5f,
					ImageUpIndent = 60,
					ImageHeight = 190,
					ImageWidth = 190,
					ModeUpIndent = 60,
					ModeHeight = 40,
					ModeWidth = 130,
					ModeMargin = 15
				}
			};

			[JsonProperty(PropertyName = "Active Item Settings")]
			public ActiveItemConf ActiveItem = new ActiveItemConf
			{
				Enabled = false,
				Items = new Dictionary<string, ActiveItemConf.ActiveItem>
				{
					[HammerShortname] = new ActiveItemConf.ActiveItem
					{
						Enabled = true,
						DefaultMode = Types.Remove,
						IgnoredSkins = new List<ulong>
						{
							1196009619u
						},
						SaveSelectedMode = false
					},
					[ToolGunShortname] = new ActiveItemConf.ActiveItem
					{
						Enabled = true,
						DefaultMode = Types.Remove,
						IgnoredSkins = new List<ulong>(),
						SaveSelectedMode = false
					},
					["building.planner"] = new ActiveItemConf.ActiveItem
					{
						Enabled = true,
						DefaultMode = Types.Stone,
						IgnoredSkins = new List<ulong>
						{
							1195976254u
						},
						SaveSelectedMode = false
					}
				}
			};

			public VersionNumber Version;
		}

		private class CrosshairSettings
		{
			[JsonProperty(PropertyName = "Enabled")]
			public bool Enabled;

			[JsonProperty(PropertyName = "Size")] public float Size;

			[JsonProperty(PropertyName = "Thickness")]
			public float Thickness;

			[JsonProperty(PropertyName = "Color")] public IColor Color;

			[JsonProperty(PropertyName = "Display type (Overlay/Hud)")]
			public string DisplayType;
		}

		private class ActiveItemConf
		{
			[JsonProperty(PropertyName = "Enabled")]
			public bool Enabled;

			[JsonProperty(PropertyName = "Items", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, ActiveItem> Items;

			public class ActiveItem
			{
				[JsonProperty(PropertyName = "Enabled")]
				public bool Enabled;

				[JsonProperty(PropertyName = "Default Mode")] [JsonConverter(typeof(StringEnumConverter))]
				public Types DefaultMode;

				[JsonProperty(PropertyName = "Ignored Skins", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public List<ulong> IgnoredSkins = new List<ulong>();

				[JsonProperty(PropertyName = "Save the selected mode when closing?")]
				public bool SaveSelectedMode;

				[JsonIgnore] private Mode _mode;

				public Mode GetMode(BasePlayer player, string item)
				{
#if TESTING
					Debug.Log($"[ActiveItem.GetMode] player={player.UserIDString}, item={item}");
#endif

					if (SaveSelectedMode)
					{
						var selectedMode = PlayerData.GetOrLoad(player.UserIDString)?.GetSelectedMode(item);
						if (selectedMode != null)
						{
#if TESTING
							Debug.Log($"[ActiveItem.GetMode] SaveSelectedMode.return: {selectedMode.Type}");
#endif
							return selectedMode;
						}
					}

#if TESTING
					Debug.Log($"[ActiveItem.GetMode] return default");
#endif
					return _mode;
				}

				public void SetMode(Mode mode)
				{
					_mode = mode;
				}
			}

			public void Init()
			{
				foreach (var item in Items.Values)
					item.SetMode(_instance.GetModeByType(item.DefaultMode));
			}
		}

		private class ActionCooldown
		{
			[JsonProperty(PropertyName = "Default")]
			public float Default;

			[JsonProperty(PropertyName = "Permissions", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, float> Permissions;

			public float GetCooldown(BasePlayer player)
			{
				var result = Default;

				foreach (var check in Permissions.Where(check =>
					         player.IPlayer.HasPermission(check.Key) && result < check.Value))
					result = check.Value;

				return result;
			}
		}

		private class InterfaceSettings
		{
			[JsonProperty(PropertyName = "Display type (Overlay/Hud)")]
			public string DisplayType;

			[JsonProperty(PropertyName = "Color 1")]
			public IColor Color1;

			[JsonProperty(PropertyName = "Color 2")]
			public IColor Color2;

			[JsonProperty(PropertyName = "Color 3")]
			public IColor Color3;

			[JsonProperty(PropertyName = "Offset Y")]
			public float OffsetY;

			[JsonProperty(PropertyName = "Offset X")]
			public float OffsetX;

			[JsonProperty(PropertyName = "Progress Title Text Size")]
			public int ProgressTitleTextSize;

			[JsonProperty(PropertyName = "Progress Title Text Color")]
			public IColor ProgressTitleTextColor;

			[JsonProperty(PropertyName = "Settings Background Color")]
			public IColor SettingsBackgroundColor;

			[JsonProperty(PropertyName = "Settings Header Color")]
			public IColor SettingsHeaderColor;

			[JsonProperty(PropertyName = "Settings Image Background Color")]
			public IColor SettingsImageBackgroundColor;

			[JsonProperty(PropertyName = "Settings Selected Color")]
			public IColor SettingsSelectedColor;

			[JsonProperty(PropertyName = "Settings Not Selected Color")]
			public IColor SettingsNotSelectedColor;

			[JsonProperty(PropertyName = "Settings Text Color")]
			public IColor SettingsTextColor;

			[JsonProperty(PropertyName = "Skins")] public SkinsUI Skins;

			public class SkinsUI
			{
				[JsonProperty(PropertyName = "Tab Up Indent")]
				public float TabUpIndent;

				[JsonProperty(PropertyName = "Tab Height")]
				public float TabHeight;

				[JsonProperty(PropertyName = "Tab Width")]
				public float TabWidth;

				[JsonProperty(PropertyName = "Tab Margin")]
				public float TabMargin;

				[JsonProperty(PropertyName = "Image Left Indent")]
				public float ImageLeftIndent;

				[JsonProperty(PropertyName = "Image Up Indent")]
				public float ImageUpIndent;

				[JsonProperty(PropertyName = "Image Height")]
				public float ImageHeight;

				[JsonProperty(PropertyName = "Image Width")]
				public float ImageWidth;

				[JsonProperty(PropertyName = "Mode Up Indent")]
				public float ModeUpIndent;

				[JsonProperty(PropertyName = "Mode Height")]
				public float ModeHeight;

				[JsonProperty(PropertyName = "Mode Width")]
				public float ModeWidth;

				[JsonProperty(PropertyName = "Mode Margin")]
				public float ModeMargin;
			}
		}

		private class IColor
		{
			[JsonProperty(PropertyName = "HEX")] public string Hex;

			[JsonProperty(PropertyName = "Opacity (0 - 100)")]
			public float Alpha;

			[JsonIgnore] private string _color;

			[JsonIgnore]
			public string Get
			{
				get
				{
					if (string.IsNullOrEmpty(_color))
						_color = GetColor();

					return _color;
				}
			}

			private string GetColor()
			{
				if (string.IsNullOrEmpty(Hex)) Hex = "#FFFFFF";

				var str = Hex.Trim('#');
				if (str.Length != 6) throw new Exception(Hex);
				var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
				var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
				var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);

				return $"{(double) r / 255} {(double) g / 255} {(double) b / 255} {Alpha / 100}";
			}

			public IColor()
			{
			}

			public IColor(string hex, float alpha = 100)
			{
				Hex = hex;
				Alpha = alpha;
			}
		}

		private class ConditionSettings
		{
			[JsonProperty(PropertyName = "Default (from game)")]
			public bool Default;

			[JsonProperty(PropertyName = "Use percent?")]
			public bool Percent;

			[JsonProperty(PropertyName = "Percent (value)")]
			public float PercentValue;
		}

		private class BlockSettings
		{
			[JsonProperty(PropertyName = "Work with NoEscape?")]
			public bool UseNoEscape;

			[JsonProperty(PropertyName = "Work with Clans? (clan members will be able to delete/upgrade)")]
			public bool UseClans;

			[JsonProperty(PropertyName = "Work with Friends? (friends will be able to delete/upgrade)")]
			public bool UseFriends;

			[JsonProperty(PropertyName = "Can those authorized in the cupboard delete/upgrade?")]
			public bool UseCupboard;

			[JsonProperty(PropertyName = "Is an upgrade/remove cupboard required?")]
			public bool NeedCupboard;
		}

		private abstract class TotalSettings
		{
			[JsonProperty(PropertyName = "Permission to modify all entities (/command all) ")]
			public string PermissionToAll;

			[JsonProperty(PropertyName = "Time of action")]
			public int ActionTime;

			[JsonProperty(PropertyName = "Cooldown (default | 0 - disable)")]
			public int Cooldown;

			[JsonProperty(PropertyName = "Cooldowns", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, int> VipCooldown;

			[JsonProperty(PropertyName = "Block After Wipe (default | 0 - disable)")]
			public int AfterWipe;

			[JsonProperty(PropertyName = "Block After Wipe", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, int> VipAfterWipe;

			public int GetCooldown(BasePlayer player)
			{
				return (from check in VipCooldown
					where player.IPlayer.HasPermission(check.Key)
					select check.Value).Prepend(Cooldown).Min();
			}

			public int GetWipeCooldown(BasePlayer player)
			{
				return (from check in VipAfterWipe
					where player.IPlayer.HasPermission(check.Key)
					select check.Value).Prepend(AfterWipe).Min();
			}

			public bool HasAllPermission(BasePlayer player)
			{
				return string.IsNullOrWhiteSpace(PermissionToAll) ||
				       _instance.permission.UserHasPermission(player.UserIDString, PermissionToAll);
			}
		}

		private class UpgradeSettings : TotalSettings
		{
			[JsonProperty(PropertyName = "Amount of upgrade entities per tick")]
			public int AmountPerTick;

			[JsonProperty(PropertyName =
				"Notify the player of the required resources for upgrading a building when they do not have enough resources")]
			public bool NotifyRequiredResources;

			[JsonProperty(PropertyName = "Press Shift + Attack to Upgrade All")]
			public bool ShiftAttackToUpgradeAll;

			[JsonProperty(PropertyName = "Updating building skins on right-click upgrade?")]
			public bool UpdateSkinsOnRightClick;

			[JsonProperty(PropertyName = "Skins Settings")]
			public SkinsInfo Skins = new SkinsInfo
			{
				Enabled = true,
				Icon = "assets/icons/gear.png",
				Images = new Dictionary<ulong, string>
				{
					[10220] = "https://i.ibb.co/GRjNqrq/ty1gZVS.png",
					[10221] = "https://i.ibb.co/N1ZtZ1h/CSTnZ9V.png",
					[10223] = "https://i.ibb.co/6yv2gCp/SGB52rr.png",
					[10225] = "https://i.ibb.co/HgjXDCp/512fx512fdpx2x.png",
				}
			};
		}

		private class SkinsInfo
		{
			[JsonProperty(PropertyName = "Enabled")]
			public bool Enabled;

			[JsonProperty(PropertyName = "Icon (assets/url)")]
			public string Icon;

			[JsonProperty(PropertyName = "Image for skins", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<ulong, string> Images = new Dictionary<ulong, string>();
		}

		private class SkinConf
		{
			[JsonProperty(PropertyName = "Enabled")]
			public bool Enabled;

			[JsonProperty(PropertyName = "Lang Key")]
			public string LangKey;

			[JsonProperty(PropertyName = "Permission")]
			public string Permission;

			[JsonProperty(PropertyName = "Skin")] public ulong Skin;
		}

		private class RemoveSettings : TotalSettings
		{
			[JsonProperty(PropertyName = "Blocked items to remove (prefab – settings)",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, List<IgnoredBlock>> BlockedList;

			[JsonProperty(PropertyName = "Return Item")]
			public bool ReturnItem;

			[JsonProperty(PropertyName = "Returnable Item Percentage")]
			public float ReturnPercent;

			[JsonProperty(PropertyName = "Percentages of returnable items (permission – percent)",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, float> ReturnPercents = new Dictionary<string, float>();

			[JsonProperty(PropertyName = "Can friends remove? (Friends)")]
			public bool CanFriends;

			[JsonProperty(PropertyName = "Can clanmates remove? (Clans)")]
			public bool CanClan;

			[JsonProperty(PropertyName = "Can teammates remove?")]
			public bool CanTeams;

			[JsonProperty(PropertyName = "Remove by cupboard? (those who are authorized in the cupboard can remove)")]
			public bool RemoveByCupboard;

			[JsonProperty(PropertyName = "Remove container with items?")]
			public bool RemoveItemsContainer;

			[JsonProperty(PropertyName = "Condition Settings")]
			public ConditionSettings Condition;

			[JsonProperty(PropertyName = "Block Cooldown After Spawn Settings")]
			public ActionCooldown BlockCooldown;

			[JsonProperty(PropertyName = "Amount of remove entities per tick")]
			public int AmountPerTick;

			[JsonProperty(PropertyName = "Press Shift + Attack to Remove All")]
			public bool ShiftAttackToRemoveAll;

			[JsonProperty(PropertyName = "Vision Removal")]
			public VisionRemoval Vision;

			public class VisionRemoval
			{
				[JsonProperty(PropertyName = "Enabled")]
				public bool Enabled;

				[JsonProperty(PropertyName = "Permission")]
				public string Permission;

				[JsonProperty(PropertyName = "Distance")]
				public float Distance;

				[JsonProperty(PropertyName = "Crosshair")]
				public CrosshairSettings Crosshair;
			}

			public float GetReturnPercent(BasePlayer player)
			{
				var result = ReturnPercent;

				foreach (var returnPercent in ReturnPercents)
					if (_instance?.permission?.UserHasPermission(returnPercent.Key, player.UserIDString) == true &&
					    returnPercent.Value > result)
						result = returnPercent.Value;

				return result;
			}

			[JsonIgnore] private Dictionary<string, Dictionary<string, IgnoredBlock>> _ignoredBlocks;

			public void Init()
			{
				_ignoredBlocks = new Dictionary<string, Dictionary<string, IgnoredBlock>>();

				foreach (var block in BlockedList)
				{
					_ignoredBlocks[block.Key] = new Dictionary<string, IgnoredBlock>();

					foreach (var ignoredBlock in block.Value)
					{
						foreach (var ignoredSkin in ignoredBlock.Skins)
						{
							IgnoredBlock check;
							if (!_ignoredBlocks[block.Key].TryGetValue(ignoredSkin, out check))
							{
								_ignoredBlocks[block.Key].Add(ignoredSkin, ignoredBlock);
							}
						}
					}
				}
			}

			public IgnoredBlock GetIgnoredBlock(BaseEntity entity)
			{
				Dictionary<string, IgnoredBlock> dictionary;
				if (!_ignoredBlocks.TryGetValue(entity.name, out dictionary)) return null;

				IgnoredBlock block;
				return
					dictionary.TryGetValue(entity.skinID.ToString(), out block) ? block :
					dictionary.TryGetValue("*", out block) ? block :
					null;
			}
		}

		private class DowngradeSettings : TotalSettings
		{
			[JsonProperty(PropertyName = "Blocked items to downgrade (prefab – skins, but '*' - all skins)",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, List<string>> BlockedList;

			[JsonProperty(PropertyName = "Can friends downgrade? (Friends)")]
			public bool CanFriends;

			[JsonProperty(PropertyName = "Can clanmates downgrade? (Clans)")]
			public bool CanClan;

			[JsonProperty(PropertyName = "Can teammates downgrade?")]
			public bool CanTeams;

			[JsonProperty(PropertyName = "Amount of downgrade entities per tick")]
			public int AmountPerTick;

			[JsonProperty(PropertyName = "Press Shift + Attack to Downgrade All")]
			public bool ShiftAttackToDowngradeAll;

			public bool IsIgnored(BaseEntity entity)
			{
				List<string> list;
				if (!BlockedList.TryGetValue(entity.name, out list))
					return false;
				return list.Contains("*") || list.Contains(entity.skinID.ToString());
			}
		}

		private class IgnoredBlock
		{
			[JsonProperty(PropertyName = "Skins (* – all)")]
			public List<string> Skins;

			[JsonProperty(PropertyName = "Can remove?")]
			public bool CanRemove;

			[JsonProperty(PropertyName = "Return Item")]
			public bool ReturnItem;

			[JsonProperty(PropertyName = "Returnable Item Percentage")]
			public float ReturnPercent;
		}

		private class Mode
		{
			[JsonProperty(PropertyName = "Icon (assets/url)")]
			public string Icon;

			[JsonProperty(PropertyName = "Type (Remove/Wood/Stone/Metal/TopTier)")]
			[JsonConverter(typeof(StringEnumConverter))]
			public Types Type;

			[JsonProperty(PropertyName = "Permission (ex: buildtools.1)")]
			public string Permission;

			[JsonProperty(PropertyName = "Default value for additional slot")]
			public bool Additional;

			[JsonProperty(PropertyName = "Enable skins?")]
			public bool UseSkins;

			[JsonProperty(PropertyName = "Skins", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<SkinConf> Skins = new List<SkinConf>();

			public List<SkinConf> GetSkins(BasePlayer player)
			{
				return Skins.FindAll(x =>
					x.Enabled && (string.IsNullOrEmpty(x.Permission) ||
					              _instance.permission.UserHasPermission(player.UserIDString, x.Permission)));
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
			catch (Exception ex)
			{
				PrintError("Your configuration file contains an error. Using default configuration values.");
				LoadDefaultConfig();
				Debug.LogException(ex);
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

			if (_config.Version != default(VersionNumber))
			{
				if (_config.Version < new VersionNumber(1, 3, 0))
					ConvertOldData();

				if (_config.Version < new VersionNumber(1, 5, 10))
				{
					_config.Remove.ReturnPercents = new Dictionary<string, float>
					{
						["buildtools.vip"] = 100,
						["buildtools.premium"] = 100
					};
				}

				if (_config.Version < new VersionNumber(1, 5, 14))
				{
					foreach (var mode in _config.Modes)
					{
						if (mode.Type == Types.Stone && mode.Skins.All(x => x.LangKey != "SkinBrick"))
						{
							mode.Skins.Add(new SkinConf
							{
								Enabled = true,
								LangKey = "SkinBrick",
								Permission = string.Empty,
								Skin = 10223
							});
						}
					}

					if (_config.Upgrade.Skins.Images.ContainsKey(10223) == false)
						_config.Upgrade.Skins.Images.TryAdd(10223, "https://i.ibb.co/6yv2gCp/SGB52rr.png");
				}

				if (_config.Version < new VersionNumber(1, 5, 17))
				{
					foreach (var activeItem in _config.ActiveItem.Items)
					{
						switch (activeItem.Key)
						{
							case HammerShortname:
							{
								activeItem.Value.IgnoredSkins = new List<ulong>
								{
									1196009619u
								};
								break;
							}
							case "building.planner":
							{
								activeItem.Value.IgnoredSkins = new List<ulong>
								{
									1195976254u
								};
								break;
							}
							default:
							{
								activeItem.Value.IgnoredSkins = new List<ulong>();
								break;
							}
						}
					}
				}

				if (_config.Version == new VersionNumber(1, 5, 18))
				{
					foreach (var activeItem in _config.ActiveItem.Items.Values)
						if (activeItem.IgnoredSkins == null)
							activeItem.IgnoredSkins = new List<ulong>();
				}

				if (_config.Version < new VersionNumber(1, 5, 20))
				{
					var oldValue = Config["Block Settings", "Is an upgrade/remove cupbaord required?"];
					if (oldValue != null)
					{
						_config.Block.NeedCupboard = Convert.ToBoolean(oldValue);
					}
				}

				if (_config.Version < new VersionNumber(1, 5, 21))
				{
					_config.Remove.BlockedList = new Dictionary<string, List<IgnoredBlock>>();

					var confObj = Config["Remove Settings", "Blocked items to remove (prefab)"];
					if (confObj != null)
					{
						var list = (IList) confObj;

						foreach (var block in list.Cast<string>().ToList())
						{
							_config.Remove.BlockedList.TryAdd(block, new List<IgnoredBlock>
							{
								new IgnoredBlock
								{
									Skins = new List<string> {"*"},
									CanRemove = false,
									ReturnItem = false,
									ReturnPercent = 100
								}
							});
						}
					}
				}

				if (_config.Version < new VersionNumber(1, 5, 12))
				{
					foreach (var mode in _config.Modes)
					{
						if (mode.Type == Types.Stone && mode.Skins.All(x => x.LangKey != "SkinBrutalist"))
						{
							mode.Skins.Add(new SkinConf
							{
								Enabled = true,
								LangKey = "SkinBrutalist",
								Permission = string.Empty,
								Skin = 10225
							});
						}
					}

					if (_config.Upgrade.Skins.Images.ContainsKey(10225) == false)
						_config.Upgrade.Skins.Images.TryAdd(10225, "https://i.ibb.co/HgjXDCp/512fx512fdpx2x.png");

					foreach (var check in _config.Upgrade.Skins.Images.ToArray())
					{
						string newValue;
						switch (check.Value)
						{
							case "https://i.imgur.com/ty1gZVS.png":
								newValue = "https://i.ibb.co/GRjNqrq/ty1gZVS.png";
								break;
							case "https://i.imgur.com/CSTnZ9V.png":
								newValue = "https://i.ibb.co/N1ZtZ1h/CSTnZ9V.png";
								break;
							case "https://i.imgur.com/SGB52rr.png":
								newValue = "https://i.ibb.co/6yv2gCp/SGB52rr.png";
								break;
							default:
								continue;
						}

						if (!string.IsNullOrWhiteSpace(newValue))
							_config.Upgrade.Skins.Images[check.Key] = newValue;
					}
				}
			}

			_config.Version = Version;
			PrintWarning("Config update completed!");
		}

		#endregion

		#region Data

		private EntitiesData _entitiesData;

		private void SaveEntities()
		{
			Interface.Oxide.DataFileSystem.WriteObject(Name + "_Entities", _entitiesData);
		}

		private void LoadEntities()
		{
			try
			{
				_entitiesData = Interface.Oxide.DataFileSystem.ReadObject<EntitiesData>(Name + "_Entities");
			}
			catch (Exception e)
			{
				PrintError(e.ToString());
			}

			if (_entitiesData == null) _entitiesData = new EntitiesData();
		}

		private class EntitiesData
		{
			[JsonProperty(PropertyName = "Entities", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<ulong, DateTime> Entities = new Dictionary<ulong, DateTime>();
		}

		#endregion

		#region Hooks

		private void Init()
		{
			_instance = this;

			LoadEntities();

			RegisterPermissions();

			RegisterCommands();

			_config.Remove.Init();

			UnsubscribeEntities();

#if TESTING
			StopwatchWrapper.OnComplete = DebugMessage;
#endif
		}

		private void OnServerInitialized()
		{
			LoadImages();

			TryToLoadPersonalVaultDoor();

			LoadItemPrefabs();

			ClearData();

			LoadCacheModes();

			if (_config.ActiveItem.Enabled)
				_config.ActiveItem.Init();
		}

		private void Unload()
		{
			if (_parsingSkins != null)
				ServerMgr.Instance.StopCoroutine(_parsingSkins);

			foreach (var player in BasePlayer.activePlayerList)
			{
				CuiHelper.DestroyUi(player, Layer);
				CuiHelper.DestroyUi(player, Layer + ".Settings");
				CuiHelper.DestroyUi(player, CrosshairLayer);

				PlayerData.SaveAndUnload(player.UserIDString);
			}

			Array.ForEach(_components.Values.ToArray(), build =>
			{
				if (build != null)
					build.Kill();
			});

			SaveEntities();

			_config = null;
			_instance = null;
		}

		private void OnPlayerDisconnected(BasePlayer player)
		{
			PlayerData.SaveAndUnload(player.UserIDString);
		}

		private void OnPlayerInput(BasePlayer player, InputState input)
		{
			if (player == null || input == null) return;
			
			if (_config.Remove.Vision?.Enabled == true && input.WasJustPressed(BUTTON.FIRE_PRIMARY))
			{
				var build = GetBuild(player.userID);
				if (build == null) return;

				var mode = build.GetMode();
				if (mode == null ||
				    mode.Type != Types.Remove ||
				    ActiveItemIsBuildingPlan(player)) return;

				if (!string.IsNullOrWhiteSpace(_config.Remove.Vision.Permission) &&
				    !permission.UserHasPermission(player.UserIDString, _config.Remove.Vision.Permission))
					return;

				var entity = GetLookEntity(player);
				if (entity == null)
				{
					SendNotify(player, NotFoundEntity, 1);
					return;
				}

				var cupboard = player.GetBuildingPrivilege();
				if (cupboard == null)
				{
					SendNotify(player, NoCupboard, 1);
					return;
				}

				if (!CanRemoveEntity(player, entity))
					return;

				var ignoredBlock = _config.Remove.GetIgnoredBlock(entity);
				if (ignoredBlock != null)
				{
					if (ignoredBlock.CanRemove == false)
					{
						_instance.SendNotify(player, CantRemove, 1);
						return;
					}
				}

				var data = PlayerData.GetOrCreate(player.UserIDString);

				var cooldown = _config.Remove.GetCooldown(player);
				if (cooldown > 0 && data.HasCooldown(Types.Remove, cooldown))
				{
					SendNotify(player, RemoveCanThrough, 1,
						data.LeftTime(Types.Remove, cooldown));
					return;
				}

				var blockWipe = _config.Remove.GetWipeCooldown(player);
				if (blockWipe > 0 && PlayerData.HasWipeCooldown(blockWipe))
				{
					SendNotify(player, RemoveCanThrough, 1,
						PlayerData.WipeLeftTime(blockWipe));
					return;
				}

				entity.Invoke(() => RemoveEntity(player, entity, ignoredBlock), 0.11f);

				data.LastRemove = DateTime.UtcNow;
				return;
			}

			if (_config.SwitchModesMiddleClick && input.WasJustPressed(BUTTON.FIRE_THIRD)) 
				GetBuild(player.userID)?.GoNext();
		}

		private object OnHammerHit(BasePlayer player, HitInfo info)
		{
			if (player == null || info == null) return null;

			var build = GetBuild(player.userID);
			if (build == null) return null;

			var mode = build.GetMode();
			if (mode == null ||
			    !ActiveItemIsHammerOrGunTools(player))
				return null;

			var entity = info.HitEntity as BaseCombatEntity;
			if (entity == null ||
			    entity.OwnerID == 0)
				return null;

			if (!player.CanBuild())
			{
				SendNotify(player, BuildingBlocked, 1);
				return true;
			}

			if (_config.Block.UseNoEscape && NoEscape != null && NoEscape.IsLoaded && IsRaidBlocked(player))
			{
				SendNotify(player, mode.Type == Types.Remove ? RemoveRaidBlocked : UpgradeRaidBlocked, 1);
				return true;
			}

			var cupboard = entity.GetBuildingPrivilege();
			if (_config.Block.NeedCupboard && cupboard == null)
			{
				SendNotify(player, CupboardRequired, 1);
				return true;
			}

			if (entity.OwnerID != player.userID) //NOT OWNER
			{
				var any =
					(_config.Block.UseFriends && IsFriends(player.OwnerID, entity.OwnerID)) ||
					(_config.Block.UseClans && IsClanMember(player.OwnerID, entity.OwnerID)) ||
					(_config.Block.UseCupboard && (cupboard == null || cupboard.IsAuthed(player)));

				if (!any)
				{
					SendNotify(player,
						mode.Type == Types.Remove ? CantRemove : mode.Type == Types.Down ? CantDowngrade : CantUpgrade,
						1);
					return true;
				}
			}

			switch (mode.Type)
			{
				case Types.Remove:
				{
					var cd = _config.Remove.BlockCooldown.GetCooldown(player);
					if (cd > 0)
					{
						DateTime created;
						if (_entitiesData.Entities.TryGetValue(entity.net.ID.Value, out created))
						{
							var leftTime = DateTime.Now.Subtract(created).TotalSeconds;
							if (leftTime > cd)
							{
								SendNotify(player, RemoveTimeLeft, 1, FormatTime(player, cd));
								return true;
							}
						}
					}

#if TESTING
					SayDebug($"[OnHammerHit.{player.UserIDString}] start check shift attack for remove");
#endif
					if (_config.Remove.ShiftAttackToRemoveAll && player.serverInput.WasDown(BUTTON.SPRINT))
					{
						RemoveAll(player);
						return true;
					}

					break;
				}
				case Types.Down:
				{
					var block = entity as BuildingBlock;
					if (block == null ||
					    (int) block.grade < 2)
					{
						SendNotify(player, CantDowngrade, 1);
						return true;
					}

					var grade = GetTypes(block.grade - 1);
					if (grade == Types.None)
					{
						SendNotify(player, CantDowngrade, 1);
						return true;
					}

					if (_config.Downgrade.ShiftAttackToDowngradeAll && player.serverInput.WasDown(BUTTON.SPRINT))
					{
						DowngradeAll(player, grade);
						return true;
					}

					break;
				}
				default:
				{
					var block = entity as BuildingBlock;
					if (block == null)
					{
						SendNotify(player, CantDowngrade, 1);
						return true;
					}

					if ((int) block.grade == (int) mode.Type)
					{
						var skin = PlayerData.GetOrLoad(player.UserIDString)?.GetSkin((int) GetEnum(mode.Type)) ?? 0UL;
						if (block.skinID == skin)
							return null;
					}

					if ((int) block.grade > (int) mode.Type)
					{
						SendNotify(player, CantDowngrade, 1);
						return true;
					}

					if (_config.Upgrade.ShiftAttackToUpgradeAll && player.serverInput.WasDown(BUTTON.SPRINT))
					{
						UpgradeAll(player, mode.Type);
						return true;
					}

					break;
				}
			}

			build.DoIt(entity);
			
#if TESTING
			SayDebug($"[{nameof(OnHammerHit)} do it ended!");
#endif
			return true;
		}

		private void OnEntityBuilt(Planner plan, GameObject go)
		{
			var player = plan.GetOwnerPlayer();
			if (player == null) return;

			var block = go.ToBaseEntity() as BuildingBlock;
			if (block == null) return;

			_entitiesData.Entities[block.net.ID.Value] = DateTime.Now;

			var build = GetBuild(player.userID);
			if (build == null) return;

			var mode = build.GetMode();
			if (mode == null || mode.Type == Types.Remove) return;

			build.DoIt(block);
		}

		private void OnEntityKill(BuildingBlock block)
		{
			if (block == null || block.net?.ID.IsValid != true) return;

			_entitiesData?.Entities?.Remove(block.net.ID.Value);
		}

		#region Plugin References

		private void OnPluginLoaded(Plugin plugin)
		{
			switch (plugin.Name)
			{
				case "PersonalVaultDoor":
					_hasPersonalVaultDoor = true;
					break;
				case "ImageLibrary":
					_enabledImageLibrary = true;
					break;
			}
		}

		private void OnPluginUnloaded(Plugin plugin)
		{
			switch (plugin.Name)
			{
				case "PersonalVaultDoor":
					_hasPersonalVaultDoor = false;
					break;
				case "ImageLibrary":
					_enabledImageLibrary = false;
					break;
			}
		}

		#endregion

		private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
		{
			if (player == null || player.IsNpc) return;

			ActiveItemConf.ActiveItem activeItem;
			if (newItem != null &&
			    _config.ActiveItem.Items.TryGetValue(newItem.info.shortname, out activeItem) &&
			    activeItem.Enabled &&
			    activeItem.IgnoredSkins?.Contains(newItem.skin) != true)
			{
				var build = AddOrGetBuild(player);
				if (build != null)
				{
					if (oldItem != null)
						SaveSelectedMode(player, build, oldItem);

					build.SetMode(activeItem.GetMode(player, newItem.info.shortname));
				}

				return;
			}

			if (oldItem != null &&
			    _config.ActiveItem.Items.TryGetValue(oldItem.info.shortname, out activeItem) &&
			    activeItem.Enabled &&
			    activeItem.IgnoredSkins?.Contains(oldItem.skin) != true)
			{
				CloseBuildingMode(player, oldItem);
			}
		}

		private object OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
		{
			if (block == null || player == null || (int) grade < 1) return null;

			var build = GetBuild(player.userID);
			if (build != null) return null;
			
			var data = PlayerData.GetOrLoad(player.UserIDString);
			if (data == null) return null;
			
			var selectedSkin = data.GetSkin((int) grade);
			if (selectedSkin == 0 || block.skinID == selectedSkin)
				return null;

			NextTick(() =>
			{
				if (block == null || block.IsDestroyed) return;
				block.ChangeGradeAndSkin(block.grade, selectedSkin, true);
			});

			return null;
		}

		#endregion

		#region Commands

		private void CmdRemove(IPlayer cov, string command, string[] args)
		{
			var player = cov.Object as BasePlayer;
			if (player == null) return;

			if (_needImageLibrary && _enabledImageLibrary == false)
			{
				SendNotify(player, NoILError, 1);

				BroadcastILNotInstalled();
				return;
			}

			var mode = GetModeByType(Types.Remove);
			if (mode == null || (!string.IsNullOrEmpty(mode.Permission) && !cov.HasPermission(mode.Permission)))
			{
				SendNotify(player, NoPermission, 1);
				return;
			}

			if (args.Length > 0 && args[0] == "all")
			{
				RemoveAll(player);
				return;
			}

			AddOrGetBuild(player).SetMode(mode);
		}

		private void CmdUpgrade(IPlayer cov, string command, string[] args)
		{
			var player = cov.Object as BasePlayer;
			if (player == null) return;

			if (_needImageLibrary && _enabledImageLibrary == false)
			{
				SendNotify(player, NoILError, 1);

				BroadcastILNotInstalled();
				return;
			}

			if (args.Length == 0)
			{
#if TESTING
				using (new StopwatchWrapper("Go Next building with 0 args took {0}ms."))
#endif
				{
					AddOrGetBuild(player).GoNext();
				}

				return;
			}

			switch (args[0])
			{
				case "all":
				{
					Types upgradeType;
					if (args.Length < 2 || ParseType(args[1], out upgradeType) == Types.None)
					{
						cov.Reply($"Error syntax! Use: /{command} {args[0]} [wood/stone/metal/toptier]");
						return;
					}

					UpgradeAll(player, upgradeType);
					break;
				}

				default:
				{
					Types type;
					if (ParseType(args[0], out type) != Types.None)
					{
						var modes = GetPlayerModes(player);
						if (modes == null) return;

						var mode = modes.Find(x => x.Type == type);
						if (mode == null || (!string.IsNullOrEmpty(mode.Permission) &&
						                     !cov.HasPermission(mode.Permission)))
						{
							SendNotify(player, NoPermission, 1);
							return;
						}
#if TESTING
						using (new StopwatchWrapper("Init building took {0}ms."))
#endif
						{
							var build = AddOrGetBuild(player);
							build.SetMode(mode);
						}
					}
					else
					{
#if TESTING
						using (new StopwatchWrapper("Go Next took {0}ms."))
#endif
						{
							AddOrGetBuild(player).GoNext();
						}
					}

					break;
				}
			}
		}

		private void CmdDowngrade(IPlayer cov, string command, string[] args)
		{
			var player = cov.Object as BasePlayer;
			if (player == null) return;

			if (_needImageLibrary && _enabledImageLibrary == false)
			{
				SendNotify(player, NoILError, 1);

				BroadcastILNotInstalled();
				return;
			}

			var mode = GetModeByType(Types.Down);
			if (mode == null || (!string.IsNullOrEmpty(mode.Permission) && !cov.HasPermission(mode.Permission)))
			{
				SendNotify(player, NoPermission, 1);
				return;
			}

			if (args.Length == 0)
			{
#if TESTING
				using (new StopwatchWrapper("Go Next for Downgrade with 0 args took {0}ms."))
#endif
				{
					AddOrGetBuild(player).SetMode(mode);
				}

				return;
			}

			switch (args[0])
			{
				case "all":
				{
					Types upgradeType;
					if (args.Length < 2 || ParseType(args[1], out upgradeType) == Types.None)
					{
						cov.Reply($"Error syntax! Use: /{command} {args[0]} [wood/stone/metal/toptier]");
						return;
					}

					DowngradeAll(player, upgradeType);
					break;
				}

				default:
				{
					AddOrGetBuild(player).SetMode(mode);
					break;
				}
			}
		}

		private void CmdParseBuildingSkins(IPlayer cov, string command, string[] args)
		{
			if ((cov.IsAdmin || cov.IsServer) == false) return;

			if (_parsingSkins != null)
			{
				cov.Reply("Parsing building skins in progress, wait for it");
				return;
			}

			TryParseBuildingSkins();
		}

		[ConsoleCommand("UI_Builder")]
		private void CmdConsoleBuilding(ConsoleSystem.Arg arg)
		{
			var player = arg.Player();
			if (player == null || !arg.HasArgs()) return;

			switch (arg.Args[0])
			{
				case "mode":
				{
					int index;
					if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out index)) return;

					var mode = GetPlayerModes(player)[index];
					if (mode == null) return;

					AddOrGetBuild(player)?.SetMode(mode);
					break;
				}

				case "open_settings":
				{
					SettingsUI(player, first: true);
					break;
				}

				case "settings_mode":
				{
					int type;
					if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out type)) return;

					SettingsUI(player, type);
					break;
				}

				case "settings_set":
				{
					int type;
					ulong skin;
					if (!arg.HasArgs(3) || !int.TryParse(arg.Args[1], out type) ||
					    !ulong.TryParse(arg.Args[2], out skin)) return;

					PlayerData.GetOrCreate(player.UserIDString)?.ChangeSkin(type, skin);

					SettingsUI(player, type);
					break;
				}

				case "close":
				{
					CloseBuildingMode(player, player.GetActiveItem());
					break;
				}
			}
		}

		#endregion

		#region Interface

		private void SettingsUI(BasePlayer player, int type = 1, bool first = false)
		{
			var container = new CuiElementContainer();

			var modes = GetPlayerModes(player, new[] {Types.None, Types.Remove, Types.Down});

			#region Background

			if (first)
			{
				CuiHelper.DestroyUi(player, Layer + ".Settings");

				container.Add(new CuiPanel()
				{
					RectTransform =
					{
						AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5"
					},
					Image =
					{
						Color = "0 0 0 0"
					},
					CursorEnabled = true
				}, "Overlay", Layer + ".Settings");
			}

			#endregion

			#region Main

			container.Add(new CuiPanel()
			{
				RectTransform =
				{
					AnchorMin = "0 0", AnchorMax = "1 1",
					OffsetMin = "-210 -167.5",
					OffsetMax = "210 167.5"
				},
				Image = {Color = _config.UI.SettingsBackgroundColor.Get}
			}, Layer + ".Settings", Layer + ".Settings.Main");

			#region Header

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0 1", AnchorMax = "1 1",
					OffsetMin = "0 0", OffsetMax = "0 50"
				},
				Image =
				{
					Color = _config.UI.SettingsHeaderColor.Get
				}
			}, Layer + ".Settings.Main", Layer + ".Settings.Header");

			container.Add(new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0 0", AnchorMax = "1 1",
					OffsetMin = "30 0", OffsetMax = "0 0"
				},
				Text =
				{
					Text = Msg(player, SkinChangerTitle),
					Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-bold.ttf",
					FontSize = 14,
					Color = _config.UI.SettingsTextColor.Get
				}
			}, Layer + ".Settings.Header");

			container.Add(new CuiButton
			{
				RectTransform =
				{
					AnchorMin = "1 1", AnchorMax = "1 1",
					OffsetMin = "-50 -37.5",
					OffsetMax = "-25 -12.5"
				},
				Text =
				{
					Text = Msg(player, CloseButton),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-bold.ttf",
					FontSize = 10,
					Color = _config.UI.SettingsTextColor.Get
				},
				Button =
				{
					Close = Layer + ".Settings",
					Color = _config.UI.SettingsSelectedColor.Get
				}
			}, Layer + ".Settings.Header");

			#endregion

			#region Modes

			var constSwitch =
				-(modes.Count * _config.UI.Skins.TabWidth + (modes.Count - 1) * _config.UI.Skins.TabMargin) / 2f;
			var xSwitch = constSwitch;

			foreach (var mode in modes)
			{
				container.Add(new CuiButton()
				{
					RectTransform =
					{
						AnchorMin = "0.5 1", AnchorMax = "0.5 1",
						OffsetMin = $"{xSwitch} {-_config.UI.Skins.TabUpIndent - _config.UI.Skins.TabHeight}",
						OffsetMax = $"{xSwitch + _config.UI.Skins.TabWidth} {-_config.UI.Skins.TabUpIndent}"
					},
					Text =
					{
						Text = Msg(player, $"SkinChanger_{mode.Type}"),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 14,
						Color = "1 1 1 1"
					},
					Button =
					{
						Color = ((Types) type == mode.Type
							? _config.UI.SettingsSelectedColor
							: _config.UI.SettingsNotSelectedColor).Get,
						Command = $"UI_Builder settings_mode {(int) mode.Type}"
					}
				}, Layer + ".Settings.Main");

				xSwitch = xSwitch + _config.UI.Skins.TabWidth + _config.UI.Skins.TabMargin;
			}

			#endregion

			#region Mode Params

			var data = PlayerData.GetOrCreate(player.UserIDString);

			var selectedSkin = data.GetSkin(type);

			var ySwitch = -_config.UI.Skins.ModeUpIndent;

			#region Default

			container.Add(new CuiButton()
			{
				RectTransform =
				{
					AnchorMin = "0.5 1", AnchorMax = "0.5 1",
					OffsetMin = $"{constSwitch} {ySwitch - _config.UI.Skins.ModeHeight}",
					OffsetMax = $"{constSwitch + _config.UI.Skins.ModeWidth} {ySwitch}"
				},
				Text =
				{
					Text = Msg(player, SkinChangerDefault),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-bold.ttf",
					FontSize = 14,
					Color = "1 1 1 1"
				},
				Button =
				{
					Color = selectedSkin == 0
						? _config.UI.SettingsSelectedColor.Get
						: _config.UI.SettingsNotSelectedColor.Get,
					Command = $"UI_Builder settings_set {type} 0"
				}
			}, Layer + ".Settings.Main", ".Settings.Skin.Default");

			ySwitch = ySwitch - _config.UI.Skins.ModeMargin - _config.UI.Skins.ModeHeight;

			#endregion

			#region List

			var nowMode = GetModeByType(type);
			if (nowMode != null && nowMode.UseSkins)
			{
				var skins = nowMode.GetSkins(player);

				foreach (var skin in skins)
				{
					container.Add(new CuiButton()
						{
							RectTransform =
							{
								AnchorMin = "0.5 1", AnchorMax = "0.5 1",
								OffsetMin = $"{constSwitch} {ySwitch - _config.UI.Skins.ModeHeight}",
								OffsetMax = $"{constSwitch + _config.UI.Skins.ModeWidth} {ySwitch}"
							},
							Text =
							{
								Text = Msg(player, skin.LangKey),
								Align = TextAnchor.MiddleCenter,
								Font = "robotocondensed-bold.ttf",
								FontSize = 14,
								Color = "1 1 1 1"
							},
							Button =
							{
								Color = selectedSkin == skin.Skin
									? _config.UI.SettingsSelectedColor.Get
									: _config.UI.SettingsNotSelectedColor.Get,
								Command = $"UI_Builder settings_set {type} {skin.Skin}"
							}
						}, Layer + ".Settings.Main", $".Settings.Skin.{skin.Skin}");

					ySwitch = ySwitch - _config.UI.Skins.ModeMargin - _config.UI.Skins.ModeHeight;
				}
			}

			#endregion

			#region Image

			string imageURL;
			if (_config.Upgrade.Skins.Images.TryGetValue(selectedSkin, out imageURL))
			{
				container.Add(new CuiElement()
				{
					Name = Layer + $".Settings.Preview.{selectedSkin}",
					Parent = Layer + ".Settings.Main",
					Components =
					{
						new CuiRawImageComponent()
						{
							Png = _instance.ImageLibrary?.Call<string>("GetImage", imageURL)
						},
						new CuiRectTransformComponent()
						{
							AnchorMin = "0.5 1", AnchorMax = "0.5 1",
							OffsetMin =
								$"{_config.UI.Skins.ImageLeftIndent} {-_config.UI.Skins.ImageUpIndent - _config.UI.Skins.ImageHeight}",
							OffsetMax =
								$"{_config.UI.Skins.ImageLeftIndent + _config.UI.Skins.ImageWidth} {-_config.UI.Skins.ImageUpIndent}"
						}
					}
				});
			}
			else
			{
				container.Add(new CuiPanel()
					{
						RectTransform =
						{
							AnchorMin = "0.5 1", AnchorMax = "0.5 1",
							OffsetMin =
								$"{_config.UI.Skins.ImageLeftIndent} {-_config.UI.Skins.ImageUpIndent - _config.UI.Skins.ImageHeight}",
							OffsetMax =
								$"{_config.UI.Skins.ImageLeftIndent + _config.UI.Skins.ImageWidth} {-_config.UI.Skins.ImageUpIndent}"
						},
						Image =
						{
							Color = _config.UI.SettingsImageBackgroundColor.Get
						}
					}, Layer + ".Settings.Main", Layer + $".Settings.Preview.{selectedSkin}");

				container.Add(new CuiLabel
					{
						RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
						Text =
						{
							Text = Msg(player, SkinChangerNoImageDescription),
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-bold.ttf",
							FontSize = 14,
							Color = "1 1 1 1"
						}
					}, Layer + $".Settings.Preview.{selectedSkin}");
			}

			#endregion

			#endregion

			#endregion

			#region Save

			container.Add(new CuiButton()
			{
				RectTransform =
				{
					AnchorMin = "0.5 0", AnchorMax = "0.5 0",
					OffsetMin = "-60 -20",
					OffsetMax = "60 20"
				},
				Text =
				{
					Text = Msg(player, SkinChangerSave),
					Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-bold.ttf",
					FontSize = 14,
					Color = _config.UI.SettingsTextColor.Get
				},
				Button =
				{
					Color = _config.UI.SettingsSelectedColor.Get,
					Close = Layer + ".Settings"
				}
			}, Layer + ".Settings.Main");

			#endregion

			CuiHelper.DestroyUi(player, Layer + ".Settings.Main");
			CuiHelper.AddUi(player, container);
		}

		#endregion

		#region Component

		private readonly Dictionary<ulong, BuildComponent> _components =
			new Dictionary<ulong, BuildComponent>();

		private BuildComponent GetBuild(ulong player)
		{
			BuildComponent build;
			return _components.TryGetValue(player, out build) ? build : null;
		}

		private BuildComponent AddOrGetBuild(BasePlayer player)
		{
			BuildComponent build;
			if (_components.TryGetValue(player.userID, out build))
				return build;

			build = player.gameObject.AddComponent<BuildComponent>();
			return build;
		}

		private class BuildComponent : FacepunchBehaviour
		{
			#region Fields

			private BasePlayer _player;

			private Mode _mode;

			private float _startTime;

			private readonly CuiElementContainer _container = new CuiElementContainer();

			private bool _started = true;

			private float _cooldown;

			#endregion

			#region Init

			private void Awake()
			{
				_player = GetComponent<BasePlayer>();

				_instance._components[_player.userID] = this;

				enabled = false;
			}

			public void SetMode(Mode mode)
			{
				if (mode == null)
					mode = GetPlayerModes(_player).FirstOrDefault();

				_mode = mode;

				_startTime = Time.time;

				_cooldown = GetCooldown();

				MainUi();

				enabled = true;

				_started = true;

				if (_mode?.Type == Types.Remove && _config.Remove.Vision?.Enabled == true &&
				    _config.Remove.Vision.Crosshair?.Enabled == true)
					CrosshairUi();
				else
					CuiHelper.DestroyUi(_player, CrosshairLayer);
			}

			#endregion

			#region Interface

			public void MainUi()
			{
				_container.Clear();

				_container.Add(new CuiPanel
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "0 0"},
					Image = {Color = "0 0 0 0"}
				}, _config.UI.DisplayType, Layer);

				#region Modes

				var modes = GetPlayerModes(_player);

				var width = 30f;
				var margin = 5f;
				var xSwitch = 15f + _config.UI.OffsetX;

				for (var i = 0; i < modes.Count; i++)
				{
					var mode = modes[i];

					_container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = "0 0", AnchorMax = "1 1",
								OffsetMin = $"{xSwitch} {15 + _config.UI.OffsetY}",
								OffsetMax = $"{xSwitch + width} {45 + _config.UI.OffsetY}"
							},
							Image =
							{
								Color = mode.Type == _mode.Type ? _config.UI.Color1.Get : _config.UI.Color2.Get
							}
						}, Layer, Layer + $".Mode.{i}");

					#region Icon

					if (mode.Icon.Contains("assets/icon"))
						_container.Add(new CuiPanel
							{
								RectTransform =
								{
									AnchorMin = "0 0", AnchorMax = "1 1",
									OffsetMin = "5 5",
									OffsetMax = "-5 -5"
								},
								Image =
								{
									Sprite = $"{mode.Icon}"
								}
							}, Layer + $".Mode.{i}");
					else
						_container.Add(new CuiElement
						{
							Parent = Layer + $".Mode.{i}",
							Components =
							{
								new CuiRawImageComponent
									{Png = _instance.ImageLibrary?.Call<string>("GetImage", mode.Icon)},
								new CuiRectTransformComponent
								{
									AnchorMin = "0 0", AnchorMax = "1 1",
									OffsetMin = "5 5",
									OffsetMax = "-5 -5"
								}
							}
						});

					#endregion

					_container.Add(new CuiButton
						{
							RectTransform =
							{
								AnchorMin = "0 0", AnchorMax = "1 1"
							},
							Text = {Text = ""},
							Button =
							{
								Color = "0 0 0 0",
								Command = $"UI_Builder mode {i}"
							}
						}, Layer + $".Mode.{i}");

					xSwitch += width + margin;

					if (i == 0)
						margin = 0f;
				}

				#endregion

				#region Settings

				if (_config.Upgrade.Skins.Enabled)
				{
					_container.Add(new CuiPanel()
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "1 1",
							OffsetMin = $"{15 + _config.UI.OffsetX} {50 + _config.UI.OffsetY}",
							OffsetMax = $"{45 + _config.UI.OffsetX} {80 + _config.UI.OffsetY}"
						},
						Image =
						{
							Color = _config.UI.Color2.Get
						}
					}, Layer, Layer + ".Btn.Settings");

					if (_config.Upgrade.Skins.Icon.Contains("assets/icon"))
					{
						_container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = "0 0", AnchorMax = "1 1",
								OffsetMin = "5 5",
								OffsetMax = "-5 -5"
							},
							Image =
							{
								Sprite = $"{_config.Upgrade.Skins.Icon}"
							}
						}, Layer + ".Btn.Settings");
					}
					else
					{
						_container.Add(new CuiElement
						{
							Parent = Layer + ".Btn.Settings",
							Components =
							{
								new CuiRawImageComponent
								{
									Png = _instance.ImageLibrary?.Call<string>("GetImage", _config.Upgrade.Skins.Icon)
								},
								new CuiRectTransformComponent
								{
									AnchorMin = "0 0", AnchorMax = "1 1",
									OffsetMin = "5 5",
									OffsetMax = "-5 -5"
								}
							}
						});
					}

					_container.Add(new CuiButton()
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "1 1"
						},
						Text = {Text = ""},
						Button =
						{
							Color = "0 0 0 0",
							Command = "UI_Builder open_settings"
						}
					}, Layer + ".Btn.Settings");
				}

				#endregion

				#region Update

				xSwitch = 15f;
				margin = 5f;

				if (_config.Upgrade.Skins.Enabled)
					xSwitch = xSwitch + width + margin;

				_container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin = $"{xSwitch + _config.UI.OffsetX} {50 + _config.UI.OffsetY}",
						OffsetMax = $"{170 + _config.UI.OffsetX} {80 + _config.UI.OffsetY}"
					},
					Image =
					{
						Color = _config.UI.Color2.Get
					}
				}, Layer, Layer + ".Panel");

				_container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "0 0",
						OffsetMin = "5 5", OffsetMax = "25 25"
					},
					Text =
					{
						Text = _instance.Msg(_player, CloseMenu),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 14,
						Color = "1 1 1 1"
					},
					Button =
					{
						Color = "0 0 0 0",
						Command = "UI_Builder close"
					}
				}, Layer + ".Panel");

				#region Icon

				if (_mode != null)
					_container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "0 0",
							OffsetMin = "5 50", OffsetMax = "25 25"
						},
						Image =
						{
							Color = "1 1 1 1",
							Sprite = $"{_mode.Icon}"
						}
					}, Layer + ".Panel");

				#endregion

				#endregion

				CuiHelper.DestroyUi(_player, Layer);
				CuiHelper.AddUi(_player, _container);
			}

			private void UpdateUi()
			{
				_container.Clear();

				_container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin = "30 0", OffsetMax = "0 0"
					},
					Image = {Color = "0 0 0 0"}
				}, Layer + ".Panel", Layer + ".Update");

				#region Text

				_container.Add(new CuiLabel
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin = "0 0", OffsetMax = "0 0"
					},
					Text =
					{
						Text =
							$"{(_mode.Type == Types.Remove ? _instance.Msg(_player, RemoveTitle, GetLeftTime()) : _mode.Type == Types.Down ? _instance.Msg(_player, DowngradeTitle, GetLeftTime()) : _instance.Msg(_player, UpgradeTitle, _instance.Msg(_player, $"{_mode.Type}"), GetLeftTime()))}",
						Align = TextAnchor.MiddleLeft,
						Font = "robotocondensed-regular.ttf",
						FontSize = _config.UI.ProgressTitleTextSize,
						Color = _config.UI.ProgressTitleTextColor.Get
					}
				}, Layer + ".Update");

				#endregion

				#region Progress

				var progress = (Time.time - _startTime) / _cooldown;
				if (progress > 0)
				{
					_container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "1 0",
							OffsetMin = "-30 0", OffsetMax = "0 2"
						},
						Image =
						{
							Color = "0 0 0 0"
						}
					}, Layer + ".Update", Layer + ".Update.Progress");

					_container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = $"{Mathf.Min(progress, 1f)} 1"
						},
						Image =
						{
							Color = _config.UI.Color3.Get
						}
					}, Layer + ".Update.Progress");
				}

				#endregion

				CuiHelper.DestroyUi(_player, Layer + ".Update");
				CuiHelper.AddUi(_player, _container);
			}

			private void CrosshairUi()
			{
				_container.Clear();

				_container.Add(new CuiPanel
				{
					RectTransform = {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5"},
					Image = {Color = "0 0 0 0"}
				}, _config.Remove.Vision.Crosshair.DisplayType, CrosshairLayer);

				// <-
				_container.Add(new CuiPanel()
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin =
							$"-{_config.Remove.Vision.Crosshair.Size} -{_config.Remove.Vision.Crosshair.Thickness / 2f}",
						OffsetMax = $"0 {_config.Remove.Vision.Crosshair.Thickness / 2f}"
					},
					Image =
					{
						Color = _config.Remove.Vision.Crosshair.Color.Get
					}
				}, CrosshairLayer);

				// ->
				_container.Add(new CuiPanel()
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin = $"0 -{_config.Remove.Vision.Crosshair.Thickness / 2f}",
						OffsetMax =
							$"{_config.Remove.Vision.Crosshair.Size} {_config.Remove.Vision.Crosshair.Thickness / 2f}"
					},
					Image =
					{
						Color = _config.Remove.Vision.Crosshair.Color.Get
					}
				}, CrosshairLayer);

				// /\
				_container.Add(new CuiPanel()
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin = $"-{_config.Remove.Vision.Crosshair.Thickness / 2f} 0",
						OffsetMax =
							$"{_config.Remove.Vision.Crosshair.Thickness / 2f} {_config.Remove.Vision.Crosshair.Size}"
					},
					Image =
					{
						Color = _config.Remove.Vision.Crosshair.Color.Get
					}
				}, CrosshairLayer);

				// \/
				_container.Add(new CuiPanel()
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin =
							$"-{_config.Remove.Vision.Crosshair.Thickness / 2f} -{_config.Remove.Vision.Crosshair.Size}",
						OffsetMax = $"{_config.Remove.Vision.Crosshair.Thickness / 2f} 0"
					},
					Image =
					{
						Color = _config.Remove.Vision.Crosshair.Color.Get
					}
				}, CrosshairLayer);

				CuiHelper.DestroyUi(_player, CrosshairLayer);
				CuiHelper.AddUi(_player, _container);
			}

			#endregion

			#region Update

			private void FixedUpdate()
			{
				if (!_started) return;

				var timeLeft = Time.time - _startTime;
				if (timeLeft > _cooldown)
				{
					Kill();
					return;
				}

				UpdateUi();
			}

			#endregion

			#region Main

			public void DoIt(BaseCombatEntity entity)
			{
				if (entity == null) return;

#if TESTING
				SayDebug($"[DoIt] for entity={entity.net.ID}");
#endif

				switch (_mode.Type)
				{
					case Types.Remove:
					{
						if (!CanRemoveEntity(_player, entity))
							return;

						var ignoredBlock = _config.Remove.GetIgnoredBlock(entity);
						if (ignoredBlock != null)
						{
							if (ignoredBlock.CanRemove == false)
							{
								_instance.SendNotify(_player, CantRemove, 1);
								return;
							}
						}

						var data = PlayerData.GetOrCreate(_player.UserIDString);

						var cooldown = _config.Remove.GetCooldown(_player);
						if (cooldown > 0 && data.HasCooldown(_mode.Type, cooldown))
						{
							_instance.SendNotify(_player, RemoveCanThrough, 1,
								data.LeftTime(_mode.Type, cooldown));
							return;
						}

						var blockWipe = _config.Remove.GetWipeCooldown(_player);
						if (blockWipe > 0 && PlayerData.HasWipeCooldown(blockWipe))
						{
							_instance.SendNotify(_player, RemoveCanThrough, 1,
								PlayerData.WipeLeftTime(blockWipe));
							return;
						}

						entity.Invoke(() => RemoveEntity(_player, entity, ignoredBlock), 0.11f);

						data.LastRemove = DateTime.UtcNow;
						break;
					}

					case Types.Down:
					{
						var block = entity as BuildingBlock;
						if (block == null) return;

						var grades = block.blockDefinition.grades;
						if (grades == null) return;

						var currentGrade = block.grade;
						if (currentGrade <= 0)
						{
							_instance.SendNotify(_player, CantDowngrade, 1);
							return;
						}

						var targetGrade = currentGrade - 1;
#if TESTING
						SayDebug(
							$"[DoIt] targetGrade={targetGrade} | check2={grades.Length <= (int) targetGrade} | check3={grades[(int) targetGrade] == null}");
#endif

						if (grades.Length <= (int) targetGrade ||
						    grades[(int) targetGrade] == null)
						{
							_instance.SendNotify(_player, CantDowngrade, 1);
							return;
						}

						var data = PlayerData.GetOrCreate(_player.UserIDString);

						var skin = data.GetSkin((int) targetGrade);

						DowngradeBlock(block, targetGrade, skin);

						Effect.server.Run(
							"assets/bundled/prefabs/fx/build/promote_" + targetGrade.ToString().ToLower() + ".prefab",
							block,
							0U, Vector3.zero, Vector3.zero);

						data.LastDowngrade = DateTime.UtcNow;
						break;
					}

					default:
					{
						var block = entity as BuildingBlock;
						if (block == null) return;
						
						var data = PlayerData.GetOrCreate(_player.UserIDString);

						var cooldown = _config.Upgrade.GetCooldown(_player);
						if (cooldown > 0 && data.HasCooldown(_mode.Type, cooldown))
						{
							_instance.SendNotify(_player, UpgradeCanThrough, 1,
								data.LeftTime(_mode.Type, cooldown));
							return;
						}

						var blockWipe = _config.Upgrade.GetWipeCooldown(_player);
						if (blockWipe > 0 && PlayerData.HasWipeCooldown(blockWipe))
						{
							_instance.SendNotify(_player, UpgradeCanThrough, 1,
								PlayerData.WipeLeftTime(blockWipe));
							return;
						}
						
						var enumGrade = GetEnum(_mode.Type);
						
						var skin = data.GetSkin((int) enumGrade);

						var grade = block.blockDefinition.GetGrade(enumGrade, skin);
						if (grade == null || !block.CanChangeToGrade(enumGrade, skin, _player) ||
						    Interface.CallHook("OnStructureUpgrade", block, _player, enumGrade) != null ||
						    block.SecondsSinceAttacked < 30.0)
							return;

						if (!_instance.permission.UserHasPermission(_player.UserIDString, PermFree))
						{
							if (!block.CanAffordUpgrade(enumGrade, skin, _player))
							{
								_instance.SendNotify(_player, NotEnoughResources, 0);
								return;
							}

							block.PayForUpgrade(grade, _player);
						}

						UpgradeBuildingBlock(block, enumGrade, skin);

						Effect.server.Run(
							"assets/bundled/prefabs/fx/build/promote_" + enumGrade.ToString().ToLower() + ".prefab",
							block,
							0U, Vector3.zero, Vector3.zero);

						data.LastUpgrade = DateTime.UtcNow;
						break;
					}
				}

				_startTime = Time.time;
			}

			#endregion

			#region Utils

			private int GetLeftTime()
			{
				return Mathf.RoundToInt(_startTime + _cooldown - Time.time);
			}

			public void OnChangedSetMode()
			{
				var mode = GetPlayerModes(_player).Find(x => x.Additional);
				if (mode == null)
				{
					GoNext();
					return;
				}

				_mode = mode;

				SetMode(mode);
			}

			public void GoNext()
			{
				var modes = GetPlayerModes(_player);
				if (modes == null) return;

				if (_mode == null)
				{
					_mode = modes.FindAll(x => x.Type != Types.Remove).FirstOrDefault();
					SetMode(_mode);
					return;
				}

				var i = 0;
				for (; i < modes.Count; i++)
				{
					var mode = modes[i];

					if (mode == _mode)
						break;
				}

				i++;

				var nextMode = modes.Count <= i ? modes[0] : modes[i];

				_mode = nextMode;

				SetMode(nextMode);
			}

			public Mode GetMode()
			{
				return _mode;
			}

			private float GetCooldown()
			{
				switch (_mode.Type)
				{
					case Types.Remove:
						return _config.Remove.ActionTime;
					default:
						return _config.Upgrade.ActionTime;
				}
			}

			#endregion

			#region Destroy

			private void OnDestroy()
			{
				CancelInvoke();

				CuiHelper.DestroyUi(_player, Layer);
				CuiHelper.DestroyUi(_player, CrosshairLayer);

				_instance?._components.Remove(_player.userID);

				Destroy(this);
			}

			public void Kill()
			{
				enabled = false;

				_started = false;

				DestroyImmediate(this);
			}

			#endregion
		}

		#endregion

		#region Utils

		private void CloseBuildingMode(BasePlayer player, Item oldItem)
		{
			var build = GetBuild(player.userID);
			if (build == null) return;

			SaveSelectedMode(player, build, oldItem);

			build.Kill();
		}

		private void SaveSelectedMode(BasePlayer player, BuildComponent build, Item oldItem)
		{
			if (player == null || build == null || oldItem == null) return;

			ActiveItemConf.ActiveItem oldActiveItem;
			if (_config.ActiveItem.Items.TryGetValue(oldItem.info.shortname, out oldActiveItem) &&
			    oldActiveItem.Enabled &&
			    oldActiveItem.IgnoredSkins?.Contains(oldItem.skin) != true)
			{
				if (oldActiveItem.SaveSelectedMode)
				{
					PlayerData.GetOrCreate(player.UserIDString)?.SaveSelectedMode(oldItem.info.shortname,
						build.GetMode()?.Type ?? Types.None);
				}
			}
		}

		#region Parse Building Skins

		private Coroutine _parsingSkins;

		private void TryParseBuildingSkins()
		{
			var id = StringPool.Get("assets/prefabs/building core/foundation/foundation.prefab");
			if (id == 0) return;

			var construction = PrefabAttribute.server.Find<Construction>(id);
			if (construction == null) return;

			var toParse = new List<KeyValuePair<Types, ulong>>();

			Array.ForEach(construction.grades, grade =>
			{
				var skin = grade.gradeBase.skin;
				if (skin > 999999 ||
				    _config.Upgrade.Skins.Images.ContainsKey(skin)) return;

				var type = GetTypes(grade.gradeBase.type);
				if (type == Types.None) return;

				toParse.Add(new KeyValuePair<Types, ulong>(type, skin));
			});

			Puts($"{toParse.Count} building skins parsing process started.");

			_parsingSkins = ServerMgr.Instance.StartCoroutine(StartParsingSkins(toParse));
		}

		private IEnumerator StartParsingSkins(List<KeyValuePair<Types, ulong>> skins)
		{
			foreach (var check in skins)
			{
				yield return ParseSkinImage(check.Key, check.Value);

				yield return CoroutineEx.waitForSeconds(1);
			}

			_parsingSkins = null;

			Puts($"{skins.Count} building skins were parsed!");
		}

		private const string imagePattern = @"<img id=""preview_image"" class=""item_def_image"" src=""(?<src>.*?)""";

		private const string titlePattern =
			@"<h2 class=""pageheader itemtitle"" style=""color: #35a3f1;"">(?<title>.*?)</h2>";

		private IEnumerator ParseSkinImage(Types type, ulong skin)
		{
			using (var www = UnityWebRequest.Get($"https://store.steampowered.com/itemstore/252490/detail/{skin}/"))
			{
				yield return www.SendWebRequest();

				if (www.isNetworkError || www.isHttpError)
				{
					PrintError($"Failed to download icon: {www.error}");
					www.Dispose();
					yield break;
				}

				var html = www.downloadHandler.text;

				var match = Regex.Match(html, imagePattern);
				if (!match.Success) yield break;

				var url = match.Groups["src"].Value;
				if (string.IsNullOrEmpty(url)) yield break;

				match = Regex.Match(html, titlePattern);
				if (!match.Success) yield break;

				var title = match.Groups["title"].Value;
				if (string.IsNullOrWhiteSpace(title)) yield break;

				if (_config.Upgrade.Skins.Images.TryAdd(skin, url))
				{
					var mode = _config.Modes.FirstOrDefault(x =>
						x.Type == type && x.Skins.All(modeSkin => modeSkin.Skin != skin));
					if (mode != null)
					{
						mode.Skins.Add(new SkinConf
						{
							Enabled = false,
							LangKey = $"Skin{title}",
							Permission = string.Empty,
							Skin = skin
						});

						lang.RegisterMessages(new Dictionary<string, string>
						{
							[$"Skin{title}"] = title
						}, this);
					}
				}

				ImageLibrary.Call("AddImage", url, url, 0UL);
			}
		}

		#endregion

		private void UpgradeAll(BasePlayer player, Types upgradeType)
		{
			if (!_config.Upgrade.HasAllPermission(player))
			{
				SendNotify(player, NoPermission, 1);
				return;
			}

			var cupboard = player.GetBuildingPrivilege();
			if (cupboard == null)
			{
				SendNotify(player, NoCupboard, 1);
				return;
			}

			if (!player.CanBuild())
			{
				SendNotify(player, BuildingBlocked, 1);
				return;
			}

			if (_config.Block.UseNoEscape && NoEscape != null && NoEscape.IsLoaded && IsRaidBlocked(player))
			{
				SendNotify(player, UpgradeRaidBlocked, 1);
				return;
			}

			var data = PlayerData.GetOrCreate(player.UserIDString);

			var cooldown = _config.Upgrade.GetCooldown(player);
			if (cooldown > 0 && data.HasCooldown(upgradeType, cooldown))
			{
				SendNotify(player, UpgradeCanThrough, 1,
					data.LeftTime(upgradeType, cooldown));
				return;
			}

			var blockWipe = _config.Upgrade.GetWipeCooldown(player);
			if (blockWipe > 0 && PlayerData.HasWipeCooldown(blockWipe))
			{
				SendNotify(player, UpgradeCanThrough, 1,
					PlayerData.WipeLeftTime(blockWipe));
				return;
			}

			var grade = GetEnum(upgradeType);

			List<BuildingBlock> buildingBlocks;
#if TESTING
			using (new StopwatchWrapper("Count building blocks took {0}ms."))
#endif
			{
				var building = cupboard.GetBuilding();
				if (building == null)
					return;

				buildingBlocks = building
					.buildingBlocks
					.Where(x => x.grade <= grade &&
					            x.CanChangeToGrade(grade, data.GetSkin((int) grade), player))
					.ToList();
				if (buildingBlocks.Count == 0) return;
			}

			var skin = data.GetSkin((int) grade);

			if (!permission.UserHasPermission(player.UserIDString, PermFree))
			{
				if (!CanAffordUpgrade(buildingBlocks, grade, skin, player))
				{
					if (_config.Upgrade.NotifyRequiredResources)
					{
						var calc = CalcUpgrade(buildingBlocks, grade, skin);
						if (calc != null && calc.Count > 0)
						{
							var value = calc.First();

							SendNotify(player, NotEnoughResourcesWithAmounts, 1,
								Msg(player, ResourcesWithAmounts, value.Value,
									Msg(player,
										ItemManager.FindItemDefinition(value.Key)?.displayName.english)));
							return;
						}
					}

					SendNotify(player, NotEnoughResources, 1);
					return;
				}

				PayForUpgrade(buildingBlocks, grade, skin, player);
			}

			Global.Runner.StartCoroutine(StartUpgrade(player, buildingBlocks, grade));

			SendNotify(player, SuccessfullyUpgrade, 0);
		}

		private void RemoveAll(BasePlayer player)
		{
			if (!_config.Remove.HasAllPermission(player))
			{
				SendNotify(player, NoPermission, 1);
				return;
			}

			var cupboard = player.GetBuildingPrivilege();
			if (cupboard == null)
			{
				SendNotify(player, NoCupboard, 1);
				return;
			}

			var data = PlayerData.GetOrCreate(player.UserIDString);

			var cooldown = _config.Remove.GetCooldown(player);
			if (cooldown > 0 && data.HasCooldown(Types.Remove, cooldown))
			{
				SendNotify(player, RemoveCanThrough, 1,
					data.LeftTime(Types.Remove, cooldown));
				return;
			}

			var blockWipe = _config.Remove.GetWipeCooldown(player);
			if (blockWipe > 0 && PlayerData.HasWipeCooldown(blockWipe))
			{
				SendNotify(player, RemoveCanThrough, 1,
					PlayerData.WipeLeftTime(blockWipe));
				return;
			}

			var building = cupboard.GetBuilding();
			if (building == null)
				return;

			var entities =
				BaseNetworkable.serverEntities
					.OfType<BaseCombatEntity>()
					.Where(x => !(x is BasePlayer) && x.GetBuildingPrivilege() == cupboard)
					.ToList();
			if (entities.Count == 0 || entities.Any(x => !CanRemoveEntity(player, x)))
				return;

			Global.Runner.StartCoroutine(StartRemove(player, entities));

			SendNotify(player, SuccessfullyRemove, 0);
		}

		private void DowngradeAll(BasePlayer player, Types downgradeType)
		{
			if (!_config.Downgrade.HasAllPermission(player))
			{
				SendNotify(player, NoPermission, 1);
				return;
			}

			var cupboard = player.GetBuildingPrivilege();
			if (cupboard == null)
			{
				SendNotify(player, NoCupboard, 1);
				return;
			}

			if (!player.CanBuild())
			{
				SendNotify(player, BuildingBlocked, 1);
				return;
			}

			if (_config.Block.UseNoEscape && IsRaidBlocked(player))
			{
				SendNotify(player, DowngradeRaidBlocked, 1);
				return;
			}

			var data = PlayerData.GetOrCreate(player.UserIDString);

			var cooldown = _config.Downgrade.GetCooldown(player);
			if (cooldown > 0 && data.HasCooldown(Types.Down, cooldown))
			{
				SendNotify(player, DowngradeCanThrough, 1,
					data.LeftTime(Types.Down, cooldown));
				return;
			}

			var blockWipe = _config.Downgrade.GetWipeCooldown(player);
			if (blockWipe > 0 && PlayerData.HasWipeCooldown(blockWipe))
			{
				SendNotify(player, DowngradeCanThrough, 1,
					PlayerData.WipeLeftTime(blockWipe));
				return;
			}

			var grade = GetEnum(downgradeType);
			var skin = data.GetSkin((int) grade);

			var building = cupboard.GetBuilding();
			if (building == null || !building.HasBuildingBlocks())
				return;

			var blocks = new List<BuildingBlock>();

			for (var i = 0; i < building.buildingBlocks.Count; i++)
			{
				var block = building.buildingBlocks[i];
				if (grade >= block.grade)
					continue;

				if (!CanDowngradeEntity(player, block, grade, skin))
				{
					blocks.Clear();
					return;
				}

				blocks.Add(block);
			}

			if (blocks.Count == 0)
			{
				blocks.Clear();

				SendNotify(player, CantDowngradeBuilding, 1);
				return;
			}

			Global.Runner.StartCoroutine(StartDowngrade(player, blocks, grade));

			SendNotify(player, SuccessfullyDowngrade, 0);
		}

		private void LoadCacheModes()
		{
			foreach (var mode in _config.Modes)
			{
				_modeByType[(int) mode.Type] = mode;
			}
		}

		private Mode GetModeByType(Types type)
		{
			return GetModeByType((int) type);
		}

		private Mode GetModeByType(int type)
		{
			Mode mode;
			return _modeByType.TryGetValue(type, out mode) ? mode : null;
		}

		private void ClearData()
		{
			var toRemove = Facepunch.Pool.GetList<ulong>();

			try
			{
				foreach (var dataEntity in _entitiesData.Entities)
					if (!BaseNetworkable.serverEntities.Contains(new NetworkableId(dataEntity.Key)))
						toRemove.Add(dataEntity.Key);

				toRemove?.ForEach(key => _entitiesData?.Entities?.Remove(key));
			}
			catch
			{
				// ignore
			}

			Facepunch.Pool.FreeList(ref toRemove);
		}

		private void LoadItemPrefabs()
		{
			foreach (var itemDefinition in ItemManager.GetItemDefinitions())
			{
				var entityPrefab = itemDefinition.GetComponent<ItemModDeployable>()?.entityPrefab?.resourcePath;
				if (string.IsNullOrEmpty(entityPrefab))
					continue;

				var shortPrefabName = Utility.GetFileNameWithoutExtension(entityPrefab);
				if (!string.IsNullOrEmpty(shortPrefabName))
					_shortPrefabNamesToItem.TryAdd(shortPrefabName, itemDefinition.shortname);
			}
		}

		private string FormatTime(BasePlayer player, float seconds)
		{
			var time = TimeSpan.FromSeconds(seconds);

			var result = string.Empty;

			if (time.Days != 0)
				result += $"{Format(time.Days, Msg(player, TimeDay), Msg(player, TimeDays))} ";

			if (time.Hours != 0)
				result += $"{Format(time.Hours, Msg(player, TimeHour), Msg(player, TimeHours))} ";

			if (time.Minutes != 0)
				result += $"{Format(time.Minutes, Msg(player, TimeMinute), Msg(player, TimeMinutes))} ";

			if (time.Seconds != 0)
				result += $"{Format(time.Seconds, Msg(player, TimeSecond), Msg(player, TimeSeconds))} ";

			return result;
		}

		private string Format(int units, string form1, string form2)
		{
			return units == 1 ? $"{units} {form1}" : $"{units} {form2}";
		}

		private BaseCombatEntity GetLookEntity(BasePlayer player)
		{
			RaycastHit hit;
			if (!Physics.Raycast(player.eyes.position, player.eyes.HeadForward(), out hit,
				    _config.Remove.Vision.Distance,
				    Layers.Construction,
				    QueryTriggerInteraction.Ignore)) return null;

			return hit.GetEntity() as BaseCombatEntity;
		}

		private bool ActiveItemIsHammerOrGunTools(BasePlayer player)
		{
			var item = player.GetActiveItem()?.info.shortname ?? "null";
			return item == HammerShortname || item == ToolGunShortname;
		}

		private bool ActiveItemIsBuildingPlan(BasePlayer player)
		{
			var item = player.GetActiveItem();
			return item != null && item.info.shortname == BuldingPlannerShortname;
		}

		private void RegisterPermissions()
		{
			permission.RegisterPermission(PermFree, this);

			RegisterPermission(_config.Upgrade.PermissionToAll);

			RegisterPermission(_config.Remove.PermissionToAll);

			RegisterPermission(_config.Downgrade.PermissionToAll);

			if (_config.Remove.Vision?.Enabled == true)
				RegisterPermission(_config.Remove.Vision.Permission);

			_config.Modes.ForEach(mode =>
			{
				RegisterPermission(mode.Permission);

				if (_config.Upgrade.Skins.Enabled && mode.UseSkins)
					foreach (var skin in mode.Skins)
						if (skin.Enabled)
							RegisterPermission(skin.Permission);
			});

			foreach (var value in _config.Upgrade.VipCooldown.Keys) RegisterPermission(value);

			foreach (var value in _config.Upgrade.VipAfterWipe.Keys) RegisterPermission(value);

			foreach (var value in _config.Remove.VipCooldown.Keys) RegisterPermission(value);

			foreach (var value in _config.Remove.VipAfterWipe.Keys) RegisterPermission(value);

			foreach (var value in _config.Remove.BlockCooldown.Permissions.Keys) RegisterPermission(value);

			foreach (var value in _config.Remove.ReturnPercents.Keys) RegisterPermission(value);
		}

		private void RegisterPermission(string value)
		{
			if (!string.IsNullOrEmpty(value) && !permission.PermissionExists(value))
				permission.RegisterPermission(value, this);
		}

		private void RegisterCommands()
		{
			AddCovalenceCommand(_config.UpgradeCommands, nameof(CmdUpgrade));

			AddCovalenceCommand(_config.RemoveCommands, nameof(CmdRemove));

			AddCovalenceCommand(_config.DowngradeCommands, nameof(CmdDowngrade));

			AddCovalenceCommand("buildtools.parse.skins", nameof(CmdParseBuildingSkins));
		}

		private void UnsubscribeEntities()
		{
			if (_config.ActiveItem.Enabled == false)
				Unsubscribe(nameof(OnActiveItemChanged));

			if (_config.Remove.Vision?.Enabled == false && _config.SwitchModesMiddleClick == false)
				Unsubscribe(nameof(OnPlayerInput));

			if (_config.Upgrade.UpdateSkinsOnRightClick == false)
				Unsubscribe(nameof(OnStructureUpgrade));

			if (_config.UseHammer == false)
				Unsubscribe(nameof(OnHammerHit));
		}

		private void LoadImages()
		{
			_needImageLibrary = true;

			if (ImageLibrary == null || !ImageLibrary.IsLoaded)
			{
				BroadcastILNotInstalled();
			}
			else
			{
				_enabledImageLibrary = true;

				var imagesList = new Dictionary<string, string>();

				_config.Modes.FindAll(mode => !mode.Icon.Contains("assets/icon")).ForEach(mode =>
				{
					if (!string.IsNullOrEmpty(mode.Icon))
						imagesList.TryAdd(mode.Icon, mode.Icon);
				});

				if (_config.Upgrade.Skins.Enabled)
				{
					if (!_config.Upgrade.Skins.Icon.Contains("assets/icon") &&
					    !string.IsNullOrEmpty(_config.Upgrade.Skins.Icon))
						imagesList.TryAdd(_config.Upgrade.Skins.Icon, _config.Upgrade.Skins.Icon);

					foreach (var image in _config.Upgrade.Skins.Images.Values)
						if (!string.IsNullOrEmpty(image))
							imagesList.TryAdd(image, image);
				}

				ImageLibrary?.Call("ImportImageList", Title, imagesList, 0UL, true);
			}
		}

		private void BroadcastILNotInstalled()
		{
			for (var i = 0; i < 5; i++) PrintError("IMAGE LIBRARY IS NOT INSTALLED.");
		}

		private static List<Mode> GetPlayerModes(BasePlayer player)
		{
			return _config.Modes.FindAll(mode =>
				string.IsNullOrEmpty(mode.Permission) ||
				_instance.permission.UserHasPermission(player.UserIDString, mode.Permission));
		}

		private static List<Mode> GetPlayerModes(BasePlayer player, Types[] ignoredTypes)
		{
			return _config.Modes.FindAll(x => !ignoredTypes.Contains(x.Type) &&
			                                  (string.IsNullOrEmpty(x.Permission) ||
			                                   _instance.permission.UserHasPermission(player.UserIDString,
				                                   x.Permission)));
		}

		private bool IsRaidBlocked(BasePlayer player)
		{
			return Convert.ToBoolean(NoEscape?.Call("IsRaidBlocked", player));
		}

		private bool IsClanMember(ulong playerID, ulong targetID)
		{
			return Convert.ToBoolean(Clans?.Call("HasFriend", playerID, targetID));
		}

		private bool IsFriends(ulong playerID, ulong friendId)
		{
			return Convert.ToBoolean(Friends?.Call("AreFriends", playerID, friendId));
		}

		private static bool IsTeammates(ulong player, ulong friend)
		{
			return RelationshipManager.ServerInstance.FindPlayersTeam(player)?.members?.Contains(friend) == true;
		}

		private static BuildingGrade.Enum GetEnum(Types type)
		{
			switch (type)
			{
				case Types.Wood:
					return BuildingGrade.Enum.Wood;
				case Types.Stone:
					return BuildingGrade.Enum.Stone;
				case Types.Metal:
					return BuildingGrade.Enum.Metal;
				case Types.TopTier:
					return BuildingGrade.Enum.TopTier;
				default:
					return BuildingGrade.Enum.None;
			}
		}

		private static Types GetTypes(BuildingGrade.Enum type)
		{
			switch (type)
			{
				case BuildingGrade.Enum.Wood:
					return Types.Wood;
				case BuildingGrade.Enum.Stone:
					return Types.Stone;
				case BuildingGrade.Enum.Metal:
					return Types.Metal;
				case BuildingGrade.Enum.TopTier:
					return Types.TopTier;
				default:
					return Types.None;
			}
		}

		private static void RemoveEntity(BasePlayer player, BaseCombatEntity entity,
			IgnoredBlock ignoredBlock)
		{
			if (_config.UsePersonalVaultDoor && _instance._hasPersonalVaultDoor &&
			    _instance.IsPersonalVaultDoor(entity))
			{
				_instance.CheckHitPersonalVaultDoor(player, entity);
				return;
			}

			if (ignoredBlock != null)
			{
				if (ignoredBlock.ReturnItem)
				{
					GiveRefund(entity, player, true, ignoredBlock.ReturnPercent);
				}
			}
			else
			{
				if (_config.Remove.ReturnItem)
					GiveRefund(entity, player);
			}

			if (_config.Remove.RemoveItemsContainer)
			{
				DropContainer(entity.GetComponent<StorageContainer>());
				DropContainer(entity.GetComponent<ContainerIOEntity>());
			}

			entity.Kill();
		}

		private static void DropContainer(StorageContainer container)
		{
			if (container == null || container.inventory.itemList.Count < 1) return;

			ItemContainer.Drop("assets/prefabs/misc/item drop/item_drop.prefab", container.GetDropPosition(),
				container.Transform.rotation, container.inventory);
		}

		private static void DropContainer(ContainerIOEntity container)
		{
			if (container == null || container.inventory.itemList.Count < 1) return;

			ItemContainer.Drop("assets/prefabs/misc/item drop/item_drop.prefab", container.GetDropPosition(),
				container.Transform.rotation, container.inventory);
		}

		private static bool CanRemoveEntity(BasePlayer player, BaseEntity entity)
		{
			if (entity.OwnerID == 0)
			{
				_instance.SendNotify(player, CantRemove, 1);
				return false;
			}

			if (!_config.Remove.RemoveItemsContainer)
			{
				var storageContainer = entity.GetComponent<StorageContainer>();
				if (storageContainer != null && storageContainer.inventory.itemList.Count > 0)
				{
					_instance.SendNotify(player, CRStorageNotEmpty, 1);
					return false;
				}

				var containerIO = entity.GetComponent<ContainerIOEntity>();
				if (containerIO != null && containerIO.inventory.itemList.Count > 0)
				{
					_instance.SendNotify(player, CRStorageNotEmpty, 1);
					return false;
				}
			}

			var combat = entity.GetComponent<BaseCombatEntity>();
			if (combat != null && combat.SecondsSinceAttacked < 30f)
			{
				_instance.SendNotify(player, CRDamaged, 1);
				return false;
			}

			if (Interface.CallHook("canRemove", player, entity) != null)
			{
				_instance.SendNotify(player, CRBeBlocked, 1);
				return false;
			}

			if (_config.Block.NeedCupboard && entity.GetBuildingPrivilege() == null)
			{
				_instance.SendNotify(player, CRBuildingBlock, 1);
				return false;
			}

			if (_config.Block.UseNoEscape && _instance.NoEscape != null && _instance.NoEscape.IsLoaded &&
			    _instance.IsRaidBlocked(player))
			{
				_instance.SendNotify(player, RemoveRaidBlocked, 1);
				return false;
			}

			if (player.userID != entity.OwnerID)
			{
				if (_config.Remove.RemoveByCupboard)
					return true;

				if (_config.Remove.CanClan && _instance.IsClanMember(player.userID, entity.OwnerID)) return true;

				if (_config.Remove.CanFriends && _instance.IsFriends(player.userID, entity.OwnerID)) return true;

				if (_config.Remove.CanTeams && IsTeammates(player.userID, entity.OwnerID)) return true;

				_instance.SendNotify(player, CRNotAccess, 1);
				return false;
			}

			return true;
		}

		private static bool CanDowngradeEntity(BasePlayer player, BuildingBlock entity, BuildingGrade.Enum grade,
			ulong skin)
		{
			if (entity.OwnerID == 0)
			{
				_instance.SendNotify(player, CantDowngrade, 1);
				return false;
			}

			var combat = entity.GetComponent<BaseCombatEntity>();
			if (combat != null && combat.SecondsSinceAttacked < 30f)
			{
				_instance.SendNotify(player, DowngradeDamaged, 1);
				return false;
			}

			if (Interface.CallHook("canDowngrade", player, entity) != null)
			{
				_instance.SendNotify(player, DowngradeBeBlocked, 1);
				return false;
			}

			var currentGrade = entity.grade;
			if (currentGrade <= 0)
			{
				_instance.SendNotify(player, CantDowngrade, 1);
				return false;
			}

			var grades = entity.blockDefinition.grades;
			if (grades == null) return false;

			var targetGrade = currentGrade - 1;
			if (grades.Length <= (int) targetGrade ||
			    grades[(int) targetGrade] == null)
			{
				_instance.SendNotify(player, CantDowngrade, 1);
				return false;
			}

			if (player.userID != entity.OwnerID)
			{
				if (_config.Downgrade.CanClan && _instance.IsClanMember(player.userID, entity.OwnerID)) return true;

				if (_config.Downgrade.CanFriends && _instance.IsFriends(player.userID, entity.OwnerID)) return true;

				if (_config.Downgrade.CanTeams && IsTeammates(player.userID, entity.OwnerID)) return true;

				_instance.SendNotify(player, DowngradeNotAccess, 1);
				return false;
			}

			return true;
		}

		private static void GiveRefund(BaseCombatEntity entity, BasePlayer player, bool usePercent = false, float mainPercent = 100)
		{
			var shortPrefabName = entity.ShortPrefabName;

			if (!_instance._shortPrefabNamesToItem.TryGetValue(shortPrefabName, out shortPrefabName))
				shortPrefabName = Regex.Replace(entity.ShortPrefabName, "\\.deployed|_deployed", "");

			var item = ItemManager.CreateByName(shortPrefabName, 1, entity.skinID);
			if (item != null)
			{
				HandleCondition(ref item, player, entity);

				player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
				return;
			}

			entity.BuildCost()?.ForEach(value =>
			{
				var percent = usePercent ? mainPercent : _config.Remove.GetReturnPercent(player);
				
				var amount = Convert.ToInt32(percent < 100
					? value.amount * (percent / 100f)
					: value.amount);

				item = ItemManager.Create(value.itemDef, amount);
				if (item == null) return;

				HandleCondition(ref item, player, entity);

				player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
			});
		}

		private static void HandleCondition(ref Item item, BasePlayer player, BaseCombatEntity entity)
		{
			if (_config.Remove.Condition.Default)
			{
				if (entity.pickup.setConditionFromHealth && item.hasCondition)
					item.conditionNormalized =
						Mathf.Clamp01(entity.healthFraction - entity.pickup.subtractCondition);
				entity.OnPickedUpPreItemMove(item, player);
			}

			if (_config.Remove.Condition.Percent)
				item.LoseCondition(item.maxCondition * (_config.Remove.Condition.PercentValue / 100f));
		}

		private static void UpgradeBuildingBlock(BuildingBlock block, BuildingGrade.Enum grade, ulong skin)
		{
			if (block == null || block.IsDestroyed) return;

#if TESTING
			SayDebug($"[UpgradeBuildingBlock] with block={block}, grade={grade}, skin={skin}");
#endif

			block.ChangeGradeAndSkin(grade, skin);
		}

		private static void DowngradeBlock(BuildingBlock block, BuildingGrade.Enum grade, ulong skin)
		{
			if (block == null || block.IsDestroyed) return;

#if TESTING
			SayDebug($"[DowngradeBlock] with block={block}, grade={grade}, skin={skin}");
#endif

			block.ChangeGradeAndSkin(grade, skin);
		}

		private bool CanAffordUpgrade(List<BuildingBlock> blocks,
			BuildingGrade.Enum targetGrade,
			ulong targetSkin,
			BasePlayer player)
		{
			var dict = new Dictionary<int, int>(); // itemId - amount

			foreach (var itemAmount in blocks.SelectMany(block =>
				         block.blockDefinition.GetGrade(targetGrade, targetSkin).CostToBuild(block.grade)))
			{
				int amount;
				if (!dict.TryGetValue(itemAmount.itemid, out amount))
					amount = player.inventory.GetAmount(itemAmount.itemid);

				if (amount < itemAmount.amount)
					return false;

				dict[itemAmount.itemid] = amount - Mathf.RoundToInt(itemAmount.amount);
			}

			return true;
		}

		private Dictionary<int, int> CalcUpgrade(List<BuildingBlock> blocks,
			BuildingGrade.Enum targetGrade,
			ulong targetSkin)
		{
			var dict = new Dictionary<int, int>(); // itemId - amount

			foreach (var itemAmount in blocks.SelectMany(block =>
				         block.blockDefinition.GetGrade(targetGrade, targetSkin).CostToBuild(block.grade)).ToList())
			{
				int amount;
				dict.TryGetValue(itemAmount.itemid, out amount);

				dict[itemAmount.itemid] = amount + Convert.ToInt32(itemAmount.amount);
			}

			return dict;
		}

		private static void PayForUpgrade(List<BuildingBlock> blocks, BuildingGrade.Enum targetGrade, ulong targetSkin,
			BasePlayer player)
		{
			var collect = new List<Item>();

			blocks.ForEach(block => block.blockDefinition.GetGrade(targetGrade, targetSkin).CostToBuild(block.grade)
				.ForEach(
					itemAmount =>
					{
						player.inventory.Take(collect, itemAmount.itemid, (int) itemAmount.amount);
						player.Command("note.inv " + itemAmount.itemid + " " +
						               (float) ((int) itemAmount.amount * -1.0));
					}));

			foreach (var obj in collect)
				obj.Remove();
		}

		private IEnumerator StartUpgrade(BasePlayer player, List<BuildingBlock> blocks, BuildingGrade.Enum grade)
		{
#if TESTING
			using (new StopwatchWrapper("Start Upgrade took {0}ms."))
#endif
			{
				var data = PlayerData.GetOrLoad(player.UserIDString);

				for (var i = 0; i < blocks.Count; i++)
				{
					var block = blocks[i];
					if (block == null || block.IsDestroyed) continue;
#if TESTING
					using (new StopwatchWrapper("Upgrade block in StartUpgrade took {0}ms."))
#endif
					{
						var skin = data?.GetSkin((int) grade) ?? 0;

						UpgradeBuildingBlock(block, grade, skin);
					}

					if (i % _config.Upgrade.AmountPerTick == 0)
						yield return CoroutineEx.waitForFixedUpdate;
				}
			}
		}

		private IEnumerator StartRemove(BasePlayer player, List<BaseCombatEntity> entities)
		{
			for (var i = 0; i < entities.Count; i++)
			{
				var entity = entities[i];
				if (entity == null || entity.IsDestroyed) continue;

				RemoveEntity(player, entity, _config.Remove.GetIgnoredBlock(entity));

				if (i % _config.Remove.AmountPerTick == 0)
					yield return CoroutineEx.waitForFixedUpdate;
			}
		}

		private IEnumerator StartDowngrade(BasePlayer player, List<BuildingBlock> blocks, BuildingGrade.Enum grade)
		{
#if TESTING
			using (new StopwatchWrapper("Start Downgrade took {0}ms."))
#endif
			{
				var data = PlayerData.GetOrLoad(player.UserIDString);

				for (var i = 0; i < blocks.Count; i++)
				{
					var block = blocks[i];
					if (block == null || block.IsDestroyed) continue;
#if TESTING
					using (new StopwatchWrapper("Upgrade block in StartDowngrade took {0}ms."))
#endif
					{
						var skin = data?.GetSkin((int) grade) ?? 0;

						DowngradeBlock(block, grade, skin);
					}

					if (i % _config.Downgrade.AmountPerTick == 0)
						yield return CoroutineEx.waitForFixedUpdate;
				}
			}
		}

		private static Types ParseType(string arg, out Types type)
		{
			Types upgradeType;
			if (Enum.TryParse(arg, true, out upgradeType))
			{
				type = upgradeType;
				return type;
			}

			int value;
			if (int.TryParse(arg, out value) && value > 0 && value < 6)
			{
				type = (Types) value;
				return type;
			}

			type = Types.None;
			return type;
		}

		#region PersonalVaultDoor

		private bool _hasPersonalVaultDoor;

		private void TryToLoadPersonalVaultDoor()
		{
			if (PersonalVaultDoor != null && PersonalVaultDoor.IsLoaded)
				_hasPersonalVaultDoor = true;
		}

		private bool IsPersonalVaultDoor(BaseEntity entity)
		{
			return PersonalVaultDoor?.Call<bool>("IsVaultDoor", entity.skinID) ?? false;
		}

		private void CheckHitPersonalVaultDoor(BasePlayer player, BaseEntity entity)
		{
			PersonalVaultDoor?.Call("CheckHit", player, entity);
		}

		#endregion

		#endregion

		#region Lang

		private const string
			NotFoundEntity = "NotFoundEntity",
			DowngradeTitle = "DowngradeTitle",
			CantDowngradeBuilding = "CantDowngradeBuilding",
			CantDowngrade = "CantDowngrade",
			DowngradeRaidBlocked = "DowngradeRaidBlocked",
			DowngradeNotAccess = "DowngradeNotAccess",
			DowngradeBuildingBlock = "DowngradeBuildingBlock",
			DowngradeBeBlocked = "DowngradeBeBlocked",
			DowngradeDamaged = "DowngradeDamaged",
			DowngradeCanThrough = "DowngradeCanThrough",
			SuccessfullyDowngrade = "SuccessfullyDowngrade",
			SkinChangerDefault = "SkinChangerDefault",
			SkinChangerSave = "SkinChangerSave",
			SkinChangerNoImageDescription = "SkinChangerNoImageDescription",
			SkinChangerTitle = "SkinChangerTitle",
			CloseButton = "CloseButton",
			ResourcesWithAmounts = "ResourcesWithAmounts",
			NotEnoughResourcesWithAmounts = "NotEnoughResourcesWithAmounts",
			NoILError = "NoILError",
			TimeSeconds = "TimeSeconds",
			TimeSecond = "TimeSecond",
			TimeMinutes = "TimeMinutes",
			TimeMinute = "TimeMinute",
			TimeHours = "TimeHours",
			TimeHour = "TimeHour",
			TimeDays = "TimeDays",
			TimeDay = "TimeDay",
			RemoveTimeLeft = "RemoveTimeLeft",
			CRNeedCupboard = "CRNeedCupboard",
			CRNotAccess = "CRNotAccess",
			CRBuildingBlock = "CRBuildingBlock",
			CRBeBlocked = "CRBeBlocked",
			CRStorageNotEmpty = "CRStorageNotEmpty",
			CRDamaged = "CRDamaged",
			SuccessfullyRemove = "SuccessfullyRemove",
			CloseMenu = "CloseMenu",
			UpgradeTitle = "UpgradeTitle",
			RemoveTitle = "RemoveTitle",
			UpgradeCanThrough = "UpgradeCanThrough",
			RemoveCanThrough = "RemoveCanThrough",
			NoPermission = "NoPermission",
			SuccessfullyUpgrade = "SuccessfullyUpgrade",
			NoCupboard = "NoCupboard",
			CupboardRequired = "CupboardRequired",
			RemoveRaidBlocked = "RemoveRaidBlocked",
			UpgradeRaidBlocked = "UpgradeRaidBlocked",
			BuildingBlocked = "BuildingBlocked",
			CantUpgrade = "CantUpgrade",
			CantRemove = "CantRemove",
			NotEnoughResources = "NotEnoughResources";

		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				[CloseButton] = "✕",
				[NotEnoughResources] = "Not enough resources to upgrade!",
				[NotEnoughResourcesWithAmounts] =
					"Not enough resources to upgrade! It takes {0} to upgrade a building.",
				[ResourcesWithAmounts] = "{0} {1}",
				[CantRemove] = "You cannot remove this entity.",
				[CantUpgrade] = "You cannot upgrade this entity.",
				[BuildingBlocked] = "You are building blocked",
				[UpgradeRaidBlocked] = "You cannot upgrade buildings <color=#81B67A>during a raid!</color>!",
				[RemoveRaidBlocked] = "You cannot upgrade or remove <color=#81B67A>during a raid!</color>!",
				[CupboardRequired] = "A Cupboard is required!",
				[NoCupboard] = "No cupboard found!",
				[SuccessfullyUpgrade] = "You have successfully upgraded a building",
				[NoPermission] = "You do not have permission to use this mode!",
				[UpgradeCanThrough] = "You can upgrade the building in: {0}s",
				[RemoveCanThrough] = "You can remove the building in: {0}s",
				[RemoveTitle] = "Remove in <color=white>{0}s</color>",
				[UpgradeTitle] = "Upgrade to {0} <color=white>{1}s</color>",
				[CloseMenu] = "✕",
				[SuccessfullyRemove] = "You have successfully removed a building",
				[CRDamaged] = "Can't remove: Server has disabled damaged objects from being removed.",
				[CRStorageNotEmpty] = "Can't remove: The entity storage is not empty.",
				[CRBeBlocked] = "Can't remove: An external plugin blocked the usage.",
				[CRBuildingBlock] = "Can't remove: Missing cupboard",
				[CRNotAccess] = "Can't remove: You don't have any rights to remove this.",
				[RemoveTimeLeft] = "Can't remove: The entity was built more than {0} ago.",
				[TimeDay] = "day",
				[TimeDays] = "days",
				[TimeHour] = "hour",
				[TimeHours] = "hours",
				[TimeMinute] = "minute",
				[TimeMinutes] = "minutes",
				[TimeSecond] = "second",
				[TimeSeconds] = "seconds",
				[NoILError] = "The plugin does not work correctly, contact the administrator!",
				[SkinChangerTitle] = "Settings",
				[SkinChangerNoImageDescription] = "Default skin",
				[SkinChangerSave] = "Save",
				[SkinChangerDefault] = "Default",
				[SuccessfullyDowngrade] = "You have successfully downgraded a building",
				[DowngradeCanThrough] = "You can downgrade the building in: {0}s",
				[DowngradeDamaged] = "Can't downgrade: Server has disabled damaged objects from being removed.",
				[DowngradeBeBlocked] = "Can't downgrade: An external plugin blocked the usage.",
				[DowngradeBuildingBlock] = "Can't downgrade: Missing cupboard",
				[DowngradeNotAccess] = "Can't downgrade: You don't have any rights to remove this.",
				[DowngradeRaidBlocked] = "You cannot downgrade buildings <color=#81B67A>during a raid!</color>!",
				[CantDowngrade] = "You cannot downgrade this entity.",
				[CantDowngradeBuilding] = "You cannot downgrade this building.",
				[DowngradeTitle] = "Downgrade in <color=white>{0}s</color>",
				[NotFoundEntity] = "Entity not found!",
				["Wood"] = "wood",
				["Stone"] = "stone",
				["Metal"] = "metal",
				["TopTier"] = "HQM",
				["SkinChanger_Wood"] = "Wood",
				["SkinChanger_Stone"] = "Stone",
				["SkinChanger_Metal"] = "Metal",
				["SkinChanger_TopTier"] = "HQM",
				["SkinAdobe"] = "Adobe",
				["SkinBrick"] = "Brick",
				["SkinContainer"] = "Container",
				["SkinBrutalist"] = "Brutalist"
			}, this);

			lang.RegisterMessages(new Dictionary<string, string>
			{
				[CloseButton] = "✕",
				[NotEnoughResources] = "Недостаточно ресурсов для улучшения!",
				[NotEnoughResourcesWithAmounts] =
					"Недостаточно ресурсов для улучшения! Для улучшения здания требуется {0}.",
				[ResourcesWithAmounts] = "{0} {1}",
				[CantRemove] = "Вы не можете удалить это строение.",
				[CantUpgrade] = "Вы не можете улучшить это строение.",
				[BuildingBlocked] = "Вы находитесь в зоне блокировки строительства",
				[UpgradeRaidBlocked] = "Вы не можете улучшать строения <color=#81B67A>во время рейда!</color>!",
				[RemoveRaidBlocked] = "Вы не можете удалять строения <color=#81B67A>во время рейда!</color>!",
				[CupboardRequired] = "Требуется шкаф с инструментами!",
				[NoCupboard] = "Шкаф с инструментами не найден!",
				[SuccessfullyUpgrade] = "Вы успешно улучшили строение",
				[NoPermission] = "У вас недостаточно разрешений, чтобы использовать этот режим!",
				[UpgradeCanThrough] = "Вы сможете улучшить строение через: {0}с",
				[RemoveCanThrough] = "Вы сможете удалить строение через: {0}с",
				[RemoveTitle] = "Удаление <color=white>{0}с</color>",
				[UpgradeTitle] = "Улучшение в {0} <color=white>{1}с</color>",
				[CloseMenu] = "✕",
				[SuccessfullyRemove] = "Вы успешно удалили строение",
				[CRDamaged] = "Не удается удалить: сервер отключил удаление поврежденных объектов.",
				[CRStorageNotEmpty] = "Не удается удалить: хранилище строения не является пустым.",
				[CRBeBlocked] = "Не удается удалить: внешний плагин заблокировал использование.",
				[CRBuildingBlock] = "Не удается удалить: отсутствует шкаф с инструментами.",
				[CRNotAccess] = "Не удается удалить: у вас нет доступа для удаляния этого строения.",
				[RemoveTimeLeft] = "Не удается удалить: строение создано более {0} назад.",
				[TimeDay] = "день",
				[TimeDays] = "дней",
				[TimeHour] = "час",
				[TimeHours] = "часов",
				[TimeMinute] = "минута",
				[TimeMinutes] = "минут",
				[TimeSecond] = "секунда",
				[TimeSeconds] = "секунд",
				[NoILError] = "Плагин работает некорректно, свяжитесь с администратором!",
				[SkinChangerTitle] = "Настройки",
				[SkinChangerNoImageDescription] = "Стандартный скин",
				[SkinChangerSave] = "Сохранить",
				[SkinChangerDefault] = "Стандартный",
				[SuccessfullyDowngrade] = "Вы успешно понизили класс строения",
				[DowngradeCanThrough] = "Вы сможете понизить класс строения через: {0}с",
				[DowngradeDamaged] =
					"Не удается выполнить понижение класса строения: сервер отключил удаление поврежденных объектов.",
				[DowngradeBeBlocked] =
					"Не удается выполнить понижение класса строения: внешний плагин заблокировал использование.",
				[DowngradeBuildingBlock] =
					"Не удается выполнить понижение класса строения: отсутствует шкаф с инструментами.",
				[DowngradeNotAccess] =
					"Не удается выполнить понижение класса строения: у вас нет доступа для удаляния этого строения.",
				[DowngradeRaidBlocked] = "Вы не можете понижать класс строения <color=#81B67A>во время рейда!</color>!",
				[CantDowngrade] = "Вы не можете понижать класс этого строения.",
				[CantDowngradeBuilding] = "Вы не можете понижать класс этого строения.",
				[DowngradeTitle] = "Понижение <color=white>{0}s</color>",
				[NotFoundEntity] = "Строение не найдено!",
				["Wood"] = "дерево",
				["Stone"] = "камень",
				["Metal"] = "метал",
				["TopTier"] = "МВК",
				["SkinAdobe"] = "Adobe",
				["SkinBrick"] = "Brick",
				["SkinContainer"] = "Container",
				["SkinBrutalist"] = "Brutalist"
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
			player.ChatMessage(Msg(key, player.UserIDString, obj));
		}

		private void SendNotify(BasePlayer player, string key, int type, params object[] obj)
		{
			if (_config.UseNotify && (Notify != null || UINotify != null))
				Interface.Oxide.CallHook("SendNotify", player, type, Msg(key, player.UserIDString, obj));
			else
				Reply(player, key, obj);
		}

		#endregion

		#region Data

		private Dictionary<string, PlayerData> _usersData = new Dictionary<string, PlayerData>();

		private class PlayerData
		{
			#region Fields

			[JsonProperty(PropertyName = "Last Upgrade")]
			public DateTime LastUpgrade = new DateTime(1970, 1, 1, 0, 0, 0);

			[JsonProperty(PropertyName = "Last Remove")]
			public DateTime LastRemove = new DateTime(1970, 1, 1, 0, 0, 0);

			[JsonProperty(PropertyName = "Last Downgrade")]
			public DateTime LastDowngrade = new DateTime(1970, 1, 1, 0, 0, 0);

			[JsonProperty(PropertyName = "Skins", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<int, ulong> Skins = new Dictionary<int, ulong>();

			[JsonProperty(PropertyName = "Selected Modes", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, Types> SelectedModes = new Dictionary<string, Types>();

			#endregion

			#region Main

			#region Selected Modes

			public Types GetSelectedType(string item)
			{
				Types type;
				return SelectedModes.TryGetValue(item, out type) ? type : Types.None;
			}

			public Mode GetSelectedMode(string item)
			{
				var type = GetSelectedType(item);
				return type == Types.None ? null : _instance.GetModeByType(type);
			}

			public void SaveSelectedMode(string item, Types type)
			{
				if (type == Types.None || string.IsNullOrWhiteSpace(item)) return;

				SelectedModes[item] = type;
			}

			#endregion

			public ulong GetSkin(int type)
			{
				ulong skin;

				Skins.TryGetValue(type, out skin);

				return skin;
			}

			public void ChangeSkin(int type, ulong skin)
			{
				if (skin != 0)
				{
					Skins[type] = skin;
				}
				else
				{
					Skins.Remove(type);
				}
			}

			public int LeftTime(Types type, int cooldown)
			{
				var time = GetLastTime(type);

				return (int) time.AddSeconds(cooldown).Subtract(DateTime.UtcNow).TotalSeconds;
			}

			private DateTime GetLastTime(Types type)
			{
				DateTime time;
				switch (type)
				{
					case Types.Remove:
						time = LastRemove;
						break;
					case Types.Down:
						time = LastDowngrade;
						break;
					default:
						time = LastUpgrade;
						break;
				}

				return time;
			}

			public bool HasCooldown(Types type, int cooldown)
			{
				var time = GetLastTime(type);

				return DateTime.UtcNow.Subtract(time).TotalSeconds < cooldown;
			}

			public static bool HasWipeCooldown(int cooldown)
			{
				return DateTime.UtcNow.Subtract(SaveRestore.SaveCreatedTime.ToUniversalTime()).TotalSeconds < cooldown;
			}

			public static int WipeLeftTime(int cooldown)
			{
				return (int) SaveRestore.SaveCreatedTime.ToUniversalTime().AddSeconds(cooldown)
					.Subtract(DateTime.UtcNow)
					.TotalSeconds;
			}

			#endregion

			#region Utils

			public static string BaseFolder() =>
				"BuildTools" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar;

			public static string[] GetFiles()
			{
				return GetFiles(BaseFolder());
			}

			public static void Save(string userId)
			{
				PlayerData data;
				if (!_instance._usersData.TryGetValue(userId, out data))
					return;

				Interface.Oxide.DataFileSystem.WriteObject(BaseFolder() + userId, data);
			}

			public static void Unload(string userId)
			{
				_instance?._usersData?.Remove(userId);
			}

			public static void SaveAndUnload(string userId)
			{
				Save(userId);

				Unload(userId);
			}

			public static PlayerData GetOrLoad(string userId)
			{
				return userId.IsSteamId() ? GetOrLoad(BaseFolder(), userId) : null;
			}

			public static PlayerData GetOrCreate(string userId)
			{
				if (!userId.IsSteamId()) return null;

				return GetOrLoad(userId) ?? (_instance._usersData[userId] = new PlayerData());
			}

			public static PlayerData GetOrLoad(string baseFolder, string userId, bool load = true)
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
		}

		#region Convert

		private void ConvertOldData()
		{
			var data = LoadOldPlayerData();

			ConvertOldPlayerData(data);

			ClearDataCache();
		}

		private OldPluginData LoadOldPlayerData()
		{
			OldPluginData data = null;
			try
			{
				data = Interface.Oxide.DataFileSystem.ReadObject<OldPluginData>(Name);
			}
			catch (Exception e)
			{
				PrintError(e.ToString());
			}

			return data;
		}

		private void ConvertOldPlayerData(OldPluginData data)
		{
			data.Players.ToList().ForEach(playerData =>
			{
				var newData = PlayerData.GetOrCreate(playerData.Key.ToString());

				newData.LastUpgrade = playerData.Value.LastUpgrade;
				newData.LastRemove = playerData.Value.LastRemove;
			});
		}

		#region Classes

		private class OldPluginData
		{
			[JsonProperty(PropertyName = "Players", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<ulong, OldPlayerData> Players = new Dictionary<ulong, OldPlayerData>();
		}

		private class OldPlayerData
		{
			[JsonProperty(PropertyName = "Last Upgrade")]
			public DateTime LastUpgrade = new DateTime(1970, 1, 1, 0, 0, 0);

			[JsonProperty(PropertyName = "Last Remove")]
			public DateTime LastRemove = new DateTime(1970, 1, 1, 0, 0, 0);

			public int LeftTime(bool remove, int cooldown)
			{
				var time = remove
					? LastRemove
					: LastUpgrade;
				return (int) time.AddSeconds(cooldown).Subtract(DateTime.UtcNow).TotalSeconds;
			}

			public bool HasCooldown(bool remove, int cooldown)
			{
				var time = remove
					? LastRemove
					: LastUpgrade;

				return DateTime.UtcNow.Subtract(time).TotalSeconds < cooldown;
			}

			public static bool HasWipeCooldown(int cooldown)
			{
				return DateTime.UtcNow.Subtract(SaveRestore.SaveCreatedTime.ToUniversalTime()).TotalSeconds < cooldown;
			}

			public static int WipeLeftTime(int cooldown)
			{
				return (int) SaveRestore.SaveCreatedTime.ToUniversalTime().AddSeconds(cooldown)
					.Subtract(DateTime.UtcNow)
					.TotalSeconds;
			}
		}

		#endregion

		#region Utils

		private void ClearDataCache()
		{
			var players = BasePlayer.activePlayerList.Select(x => x.UserIDString).ToList();

			_usersData.Where(x => !players.Contains(x.Key))
				.ToList()
				.ForEach(data => { PlayerData.SaveAndUnload(data.Key); });
		}

		#endregion

		#endregion

		#endregion

		#region Testing functions

#if TESTING
		private static void SayDebug(string message)
		{
			Debug.Log($"[BuildTools.Debug] {message}");
		}
		
		private void DebugMessage(string format, long time)
		{
			PrintWarning(format, time);
		}

		private class StopwatchWrapper : IDisposable
		{
			public StopwatchWrapper(string format)
			{
				Sw = Stopwatch.StartNew();
				Format = format;
			}

			public static Action<string, long> OnComplete { private get; set; }

			private string Format { get; }
			private Stopwatch Sw { get; }

			public long Time { get; private set; }

			public void Dispose()
			{
				Sw.Stop();
				Time = Sw.ElapsedMilliseconds;
				OnComplete(Format, Time);
			}
		}

#endif

		#endregion
	}
}