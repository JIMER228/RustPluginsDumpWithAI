//#define TESTING

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("Build Tools", "Mevent", "1.5.5")]
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

		private static BuildTools _instance;

		private enum Types
		{
			None = -1,
			Remove = 5,
			Wood = 1,
			Stone = 2,
			Metal = 3,
			TopTier = 4
		}

		private const string PermAll = "buildtools.all";

		private const string PermFree = "buildtools.free";

		private const string HammerShortname = "hammer";

		private const string ToolGunShortname = "toolgun";

		private Dictionary<string, string> _shortPrefabNamesToItem = new Dictionary<string, string>();

		#endregion

		#region Config

		private static Configuration _config;

		private class Configuration
		{
			[JsonProperty(PropertyName = "Remove Commands")]
			public readonly string[] RemoveCommands = {"remove"};

			[JsonProperty(PropertyName = "Upgrade Commands")]
			public readonly string[] UpgradeCommands = {"up", "building.upgrade"};

			[JsonProperty(PropertyName = "Work with Notify?")]
			public readonly bool UseNotify = true;

			[JsonProperty(PropertyName = "Work with PersonalVaultDoor?")]
			public readonly bool UsePersonalVaultDoor = true;

			[JsonProperty(PropertyName = "Setting Modes", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public readonly List<Mode> Modes = new List<Mode>
			{
				new Mode
				{
					Type = Types.Remove,
					Icon = "assets/icons/clear.png",
					Permission = string.Empty,
					Additional = true
				},
				new Mode
				{
					Type = Types.Wood,
					Icon = "assets/icons/level_wood.png",
					Permission = string.Empty,
					Additional = false
				},
				new Mode
				{
					Type = Types.Stone,
					Icon = "assets/icons/level_stone.png",
					Permission = string.Empty,
					Additional = false
				},
				new Mode
				{
					Type = Types.Metal,
					Icon = "assets/icons/level_metal.png",
					Permission = string.Empty,
					Additional = false
				},
				new Mode
				{
					Type = Types.TopTier,
					Icon = "assets/icons/level_top.png",
					Permission = string.Empty,
					Additional = false
				}
			};

			[JsonProperty(PropertyName = "Upgrade Settings")]
			public readonly UpgradeSettings Upgrade = new UpgradeSettings
			{
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
				AmountPerTick = 5
			};

			[JsonProperty(PropertyName = "Remove Settings")]
			public readonly RemoveSettings Remove = new RemoveSettings
			{
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
				CanFriends = true,
				CanClan = true,
				CanTeams = true,
				RemoveByCupboard = false,
				RemoveItemsContainer = false,
				BlockedList = new List<string>
				{
					"shortname 1",
					"shortname 2",
					"shortname 3"
				},
				BlockCooldown = new RemoveCooldown
				{
					Default = 36000,
					Permissions = new Dictionary<string, float>
					{
						["buildtools.vip"] = 34000,
						["buildtools.premium"] = 32000
					}
				},
				AmountPerTick = 5
			};

			[JsonProperty(PropertyName = "Block Settings")]
			public readonly BlockSettings Block = new BlockSettings
			{
				UseNoEscape = true,
				UseClans = true,
				UseFriends = true,
				UseCupboard = true,
				NeedCupboard = false
			};

			[JsonProperty(PropertyName = "UI Settings")]
			public readonly InterfaceSettings UI = new InterfaceSettings
			{
				Color1 = new IColor("#4B68FF"),
				Color2 = new IColor("#2C2C2C"),
				Color3 = new IColor("#B64040"),
				OffsetY = 0,
				OffsetX = 0
			};

			public VersionNumber Version;
		}

		private class RemoveCooldown
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
		}

		private class IColor
		{
			[JsonProperty(PropertyName = "HEX")] public string Hex;

			[JsonProperty(PropertyName = "Opacity (0 - 100)")]
			public readonly float Alpha;

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

			[JsonProperty(PropertyName = "Is an upgrade/remove cupbaord required?")]
			public bool NeedCupboard;
		}

		private abstract class TotalSettings
		{
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
		}

		private class UpgradeSettings : TotalSettings
		{
			[JsonProperty(PropertyName = "Amount of upgrade entities per tick")]
			public int AmountPerTick;
		}

		private class RemoveSettings : TotalSettings
		{
			[JsonProperty(PropertyName = "Blocked items to remove (prefab)",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<string> BlockedList;

			[JsonProperty(PropertyName = "Return Item")]
			public bool ReturnItem;

			[JsonProperty(PropertyName = "Returnable Item Percentage")]
			public float ReturnPercent;

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
			public RemoveCooldown BlockCooldown;

			[JsonProperty(PropertyName = "Amount of remove entities per tick")]
			public int AmountPerTick;
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

			var baseConfig = new Configuration();

			if (_config.Version == default(VersionNumber) || _config.Version < new VersionNumber(1, 3, 0))
				ConvertOldData();

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
			public readonly Dictionary<uint, DateTime> Entities = new Dictionary<uint, DateTime>();
		}

		#endregion

		#region Hooks

		private void Init()
		{
			_instance = this;

			PlayerData.Load();

			LoadEntities();

			RegisterPermissions();

			AddCovalenceCommand(_config.UpgradeCommands, nameof(CmdUpgrade));

			AddCovalenceCommand(_config.RemoveCommands, nameof(CmdRemove));

#if TESTING
			StopwatchWrapper.OnComplete = DebugMessage;
#endif
		}

		private void OnServerInitialized()
		{
			LoadImages();

			TryToLoadPersonalVaultDoor();

			LoadItemPrefabs();

			_entitiesData.Entities.ToList().RemoveAll(x => BaseNetworkable.serverEntities.Find(x.Key) == null);
		}

		private void Unload()
		{
			foreach (var player in BasePlayer.activePlayerList)
			{
				CuiHelper.DestroyUi(player, Layer);

				PlayerData.SaveAndUnload(player.UserIDString);
			}

			Array.ForEach(_components.Values.ToArray(), build =>
			{
				if (build != null)
					build.Kill();
			});

			PlayerData.Unload();

			SaveEntities();

			_config = null;
			_instance = null;
		}

		private void OnPlayerDisconnected(BasePlayer player)
		{
			PlayerData.SaveAndUnload(player.UserIDString);
		}

		private object OnHammerHit(BasePlayer player, HitInfo info)
		{
			if (player == null || info == null) return null;

			var build = GetBuild(player);
			if (build == null) return null;

			var mode = build.GetMode();
			if (mode == null) return null;

			if (!ActiveItemIsHammerOrGunTools(player))
				return null;

			var entity = info.HitEntity as BaseCombatEntity;
			if (entity == null || entity.OwnerID == 0) return null;

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
					(_config.Block.UseFriends && Friends != null && Friends.IsLoaded &&
					 IsFriends(player.OwnerID, entity.OwnerID)) ||
					(_config.Block.UseClans && Clans != null && Clans.IsLoaded &&
					 IsClanMember(player.OwnerID, entity.OwnerID)) ||
					(_config.Block.UseCupboard && (cupboard == null || cupboard.IsAuthed(player)));

				if (!any)
				{
					SendNotify(player, mode.Type == Types.Remove ? CantRemove : CantUpgrade, 1);
					return true;
				}
			}

			if (mode.Type == Types.Remove)
			{
				if (_config.Remove.BlockedList.Contains(entity.name))
				{
					SendNotify(player, CantRemove, 1);
					return true;
				}

				var cd = _config.Remove.BlockCooldown.GetCooldown(player);
				if (cd > 0)
				{
					DateTime created;
					if (_entitiesData.Entities.TryGetValue(entity.net.ID, out created))
					{
						var leftTime = DateTime.Now.Subtract(created).TotalSeconds;
						if (leftTime > cd)
						{
							SendNotify(player, RemoveTimeLeft, 1, FormatTime(player, cd));
							return true;
						}
					}
				}
			}
			else
			{
				var block = entity as BuildingBlock;
				if (block != null && (int) block.grade >= (int) mode.Type) return true;
			}

			build.DoIt(entity);
			return true;
		}

		private void OnEntityBuilt(Planner plan, GameObject go)
		{
			var player = plan.GetOwnerPlayer();
			if (player == null) return;

			var block = go.ToBaseEntity() as BuildingBlock;
			if (block == null) return;

			_entitiesData.Entities[block.net.ID] = DateTime.Now;

			var build = GetBuild(player);
			if (build == null) return;

			var mode = build.GetMode();
			if (mode == null || mode.Type == Types.Remove) return;

			build.DoIt(block);
		}

		private void OnEntityKill(BuildingBlock block)
		{
			if (block == null) return;

			_entitiesData.Entities.Remove(block.net.ID);
		}

		#endregion

		#region Commands

		private void CmdRemove(IPlayer cov, string command, string[] args)
		{
			var player = cov.Object as BasePlayer;
			if (player == null) return;

			var mode = _config.Modes.Find(x => x.Type == Types.Remove);
			if (mode == null || (!string.IsNullOrEmpty(mode.Permission) && !cov.HasPermission(mode.Permission)))
			{
				SendNotify(player, NoPermission, 1);
				return;
			}

			if (args.Length > 0 && args[0] == "all")
			{
				if (!cov.HasPermission(PermAll))
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
				if (cooldown > 0 && data.HasCooldown(false, cooldown))
				{
					SendNotify(player, RemoveCanThrough, 1,
						data.LeftTime(false, cooldown));
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
				if (entities.Count == 0 || entities.Any(x => !CanRemove(player, x)))
					return;

				Global.Runner.StartCoroutine(StartRemove(player, entities));

				SendNotify(player, SuccessfullyUpgrade, 0);
				return;
			}

			AddOrGetBuild(player).Init(mode);
		}

		private void CmdUpgrade(IPlayer cov, string command, string[] args)
		{
			var player = cov.Object as BasePlayer;
			if (player == null) return;

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
					if (!cov.HasPermission(PermAll))
					{
						SendNotify(player, NoPermission, 1);
						return;
					}

					Types upgradeType;
					if (args.Length < 2 || ParseType(args[1], out upgradeType) == Types.None)
					{
						cov.Reply($"Error syntax! Use: /{command} {args[0]} [wood/stone/metal/toptier]");
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
					if (cooldown > 0 && data.HasCooldown(false, cooldown))
					{
						SendNotify(player, UpgradeCanThrough, 1,
							data.LeftTime(false, cooldown));
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
							.Where(x =>
								x.grade <= grade &&
								x.CanChangeToGrade(grade, player))
							.ToList();
						if (buildingBlocks.Count == 0) return;
					}

					if (!cov.HasPermission(PermFree))
					{
						if (!CanAffordUpgrade(buildingBlocks, grade, player))
						{
							SendNotify(player, NotEnoughResources, 1);
							return;
						}

						PayForUpgrade(buildingBlocks, grade, player);
					}

					Global.Runner.StartCoroutine(StartUpgrade(buildingBlocks, grade));

					SendNotify(player, SuccessfullyUpgrade, 0);
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
							build.Init(mode);
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

					AddOrGetBuild(player)?.Init(mode);
					break;
				}

				case "close":
				{
					GetBuild(player)?.Kill();
					break;
				}
			}
		}

		#endregion

		#region Component

		private readonly Dictionary<BasePlayer, BuildComponent> _components =
			new Dictionary<BasePlayer, BuildComponent>();

		private BuildComponent GetBuild(BasePlayer player)
		{
			BuildComponent build;
			return _components.TryGetValue(player, out build) ? build : null;
		}

		private BuildComponent AddOrGetBuild(BasePlayer player)
		{
			BuildComponent build;
			if (_components.TryGetValue(player, out build))
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

				_instance._components[_player] = this;

				enabled = false;
			}

			public void Init(Mode mode)
			{
				if (mode == null)
					mode = GetPlayerModes(_player).FirstOrDefault();

				_mode = mode;

				_startTime = Time.time;

				_cooldown = GetCooldown();

				MainUi();

				enabled = true;

				_started = true;
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
				}, "Overlay", Layer);

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
									{Png = _instance.ImageLibrary.Call<string>("GetImage", mode.Icon)},
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

				#region Update

				_container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin = $"{15 + _config.UI.OffsetX} {50 + _config.UI.OffsetY}",
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
							$"{(_mode.Type == Types.Remove ? _instance.Msg(_player, RemoveTitle, GetLeftTime()) : _instance.Msg(_player, UpgradeTitle, _instance.Msg(_player, $"{_mode.Type}"), GetLeftTime()))}",
						Align = TextAnchor.MiddleLeft,
						Font = "robotocondensed-regular.ttf",
						FontSize = 12,
						Color = "1 1 1 0.6"
					}
				}, Layer + ".Update");

				#endregion

				#region Progress

				var progress = (Time.time - _startTime) / _cooldown;
				if (progress > 0)
				{
					var totalWidth = 155f * progress - 30f;

					_container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "0 0",
							OffsetMin = "-30 0", OffsetMax = $"{totalWidth} 2"
						},
						Image =
						{
							Color = _config.UI.Color3.Get
						}
					}, Layer + ".Update");
				}

				#endregion

				CuiHelper.DestroyUi(_player, Layer + ".Update");
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

				switch (_mode.Type)
				{
					case Types.Remove:
					{
						if (!CanRemove(_player, entity))
							return;

						var data = PlayerData.GetOrCreate(_player.UserIDString);

						var cooldown = _config.Remove.GetCooldown(_player);
						if (cooldown > 0 && data.HasCooldown(true, cooldown))
						{
							_instance.SendNotify(_player, RemoveCanThrough, 1,
								data.LeftTime(true, cooldown));
							return;
						}

						var blockWipe = _config.Remove.GetWipeCooldown(_player);
						if (blockWipe > 0 && PlayerData.HasWipeCooldown(blockWipe))
						{
							_instance.SendNotify(_player, RemoveCanThrough, 1,
								PlayerData.WipeLeftTime(blockWipe));
							return;
						}

						entity.Invoke(() => RemoveEntity(_player, entity), 0.11f);

						data.LastRemove = DateTime.UtcNow;
						break;
					}

					default:
					{
						var block = entity as BuildingBlock;
						if (block == null) return;

						var data = PlayerData.GetOrCreate(_player.UserIDString);

						var cooldown = _config.Upgrade.GetCooldown(_player);
						if (cooldown > 0 && data.HasCooldown(false, cooldown))
						{
							_instance.SendNotify(_player, UpgradeCanThrough, 1,
								data.LeftTime(false, cooldown));
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

						var grade = block.GetGrade(enumGrade);
						if (grade == null || !block.CanChangeToGrade(enumGrade, _player) ||
						    Interface.CallHook("OnStructureUpgrade", block, _player, enumGrade) != null ||
						    block.SecondsSinceAttacked < 30.0)
							return;

						if (!_player.IPlayer.HasPermission(PermFree))
						{
							if (!block.CanAffordUpgrade(enumGrade, _player))
							{
								_instance.SendNotify(_player, NotEnoughResources, 0);
								return;
							}

							block.PayForUpgrade(grade, _player);
						}

						UpgradeBuildingBlock(block, enumGrade);

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

				Init(mode);
			}

			public void GoNext()
			{
				var modes = GetPlayerModes(_player);
				if (modes == null) return;

				if (_mode == null)
				{
					_mode = modes.FindAll(x => x.Type != Types.Remove).FirstOrDefault();
					Init(_mode);
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

				Init(nextMode);
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

				_instance?._components.Remove(_player);

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

		private void LoadItemPrefabs()
		{
			foreach (var itemDefinition in ItemManager.GetItemDefinitions())
			{
				var entityPrefab = itemDefinition.GetComponent<ItemModDeployable>()?.entityPrefab?.resourcePath;
				if (string.IsNullOrEmpty(entityPrefab))
					continue;

				var shortPrefabName = Utility.GetFileNameWithoutExtension(entityPrefab);
				if (!string.IsNullOrEmpty(shortPrefabName) &&
				    !_shortPrefabNamesToItem.ContainsKey(shortPrefabName))
					_shortPrefabNamesToItem.Add(shortPrefabName, itemDefinition.shortname);
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
			RaycastHit RaycastHit;
			if (!Physics.Raycast(player.eyes.HeadRay(), out RaycastHit, 2.5f)) return null;
			return RaycastHit.GetEntity() as BaseCombatEntity;
		}

		private bool ActiveItemIsHammerOrGunTools(BasePlayer player)
		{
			var item = player.GetActiveItem()?.info.shortname ?? "null";
			return item == "hammer" || item == "toolgun";
		}

		private void RegisterPermissions()
		{
			permission.RegisterPermission(PermAll, this);

			permission.RegisterPermission(PermFree, this);

			_config.Modes.ForEach(mode => RegisterPermission(mode.Permission));

			foreach (var value in _config.Upgrade.VipCooldown.Keys) RegisterPermission(value);

			foreach (var value in _config.Upgrade.VipAfterWipe.Keys) RegisterPermission(value);

			foreach (var value in _config.Remove.VipCooldown.Keys) RegisterPermission(value);

			foreach (var value in _config.Remove.VipAfterWipe.Keys) RegisterPermission(value);

			foreach (var value in _config.Remove.BlockCooldown.Permissions.Keys) RegisterPermission(value);
		}

		private void RegisterPermission(string value)
		{
			if (!string.IsNullOrEmpty(value) && !permission.PermissionExists(value))
				permission.RegisterPermission(value, this);
		}

		private void LoadImages()
		{
			if (ImageLibrary == null || !ImageLibrary.IsLoaded)
			{
				PrintError("IMAGE LIBRARY IS NOT INSTALLED!");
			}
			else
			{
				var imagesList = new Dictionary<string, string>();

				_config.Modes.FindAll(mode => !mode.Icon.Contains("assets/icon")).ForEach(mode =>
				{
					if (!string.IsNullOrEmpty(mode.Icon) && !imagesList.ContainsKey(mode.Icon))
						imagesList.Add(mode.Icon, mode.Icon);
				});

				ImageLibrary?.Call("ImportImageList", Title, imagesList, 0UL, true);
			}
		}

		private static List<Mode> GetPlayerModes(BasePlayer player)
		{
			return _config.Modes.FindAll(x =>
				string.IsNullOrEmpty(x.Permission) ||
				_instance.permission.UserHasPermission(player.UserIDString, x.Permission));
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

		private static void RemoveEntity(BasePlayer player, BaseCombatEntity entity)
		{
			if (_config.UsePersonalVaultDoor && _instance._hasPersonalVaultDoor &&
			    _instance.IsPersonalVaultDoor(entity))
			{
				_instance.CheckHitPersonalVaultDoor(player, entity);
				return;
			}

			if (_config.Remove.ReturnItem)
				GiveRefund(entity, player);

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

			container.inventory.Drop("assets/prefabs/misc/item drop/item_drop.prefab", container.GetDropPosition(),
				container.Transform.rotation);
		}

		private static void DropContainer(ContainerIOEntity container)
		{
			if (container == null || container.inventory.itemList.Count < 1) return;

			container.inventory.Drop("assets/prefabs/misc/item drop/item_drop.prefab", container.GetDropPosition(),
				container.Transform.rotation);
		}

		private static bool CanRemove(BasePlayer player, BaseEntity entity)
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

			if (Interface.Call("CanRemove", player, entity) != null)
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

		private static void GiveRefund(BaseCombatEntity entity, BasePlayer player)
		{
			var shortPrefabName = entity.ShortPrefabName;

			if (!_instance._shortPrefabNamesToItem.TryGetValue(shortPrefabName, out shortPrefabName))
				shortPrefabName = Regex.Replace(entity.ShortPrefabName, "\\.deployed|_deployed", "");

			var item = ItemManager.CreateByName(shortPrefabName);
			if (item != null)
			{
				HandleCondition(ref item, player, entity);

				player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
				return;
			}

			entity.BuildCost()?.ForEach(value =>
			{
				var amount = Convert.ToInt32(_config.Remove.ReturnPercent < 100
					? value.amount * (_config.Remove.ReturnPercent / 100f)
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

		private static void UpgradeBuildingBlock(BuildingBlock block, BuildingGrade.Enum @enum)
		{
			if (block == null || block.IsDestroyed) return;

			block.SetGrade(@enum);
			block.SetHealthToMax();
			block.StartBeingRotatable();
			block.SendNetworkUpdate();
			block.UpdateSkin();
			block.ResetUpkeepTime();
			block.UpdateSurroundingEntities();

			block.GetBuilding()?.Dirty();
		}

		private bool CanAffordUpgrade(List<BuildingBlock> blocks, BuildingGrade.Enum @enum, BasePlayer player)
		{
			var dict = new Dictionary<int, int>(); // itemId - amount

			foreach (var itemAmount in blocks.SelectMany(block => block.GetGrade(@enum).costToBuild))
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

		private static void PayForUpgrade(List<BuildingBlock> blocks, BuildingGrade.Enum @enum, BasePlayer player)
		{
			var collect = new List<Item>();

			blocks.ForEach(block => block.GetGrade(@enum).costToBuild.ForEach(itemAmount =>
			{
				player.inventory.Take(collect, itemAmount.itemid, (int) itemAmount.amount);
				player.Command("note.inv " + itemAmount.itemid + " " + (float) ((int) itemAmount.amount * -1.0));
			}));

			foreach (var obj in collect)
				obj.Remove();
		}

		private IEnumerator StartUpgrade(List<BuildingBlock> blocks, BuildingGrade.Enum @enum)
		{
#if TESTING
			using (new StopwatchWrapper("Start Upgrade took {0}ms."))
#endif
			{
				for (var i = 0; i < blocks.Count; i++)
				{
					var block = blocks[i];
					if (block == null || block.IsDestroyed) continue;
#if TESTING
					using (new StopwatchWrapper("Upgrade block in StartUpgrade took {0}ms."))
#endif
					{
						UpgradeBuildingBlock(block, @enum);
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

				RemoveEntity(player, entity);

				if (i % _config.Remove.AmountPerTick == 0)
					yield return CoroutineEx.waitForFixedUpdate;
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

		private void OnPluginLoaded(Plugin plugin)
		{
			if (plugin.Name == "PersonalVaultDoor") _hasPersonalVaultDoor = true;
		}

		private void OnPluginUnloaded(Plugin plugin)
		{
			if (plugin.Name == "PersonalVaultDoor") _hasPersonalVaultDoor = false;
		}

		#endregion

		#endregion

		#region Lang

		private const string
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
				[NotEnoughResources] = "Not enough resources to upgrade!",
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
				["Wood"] = "wood",
				["Stone"] = "stone",
				["Metal"] = "metal",
				["TopTier"] = "HQM"
			}, this);

			lang.RegisterMessages(new Dictionary<string, string>
			{
				[NotEnoughResources] = "Недостаточно ресурсов для улучшения!",
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
				["Wood"] = "дерево",
				["Stone"] = "камень",
				["Metal"] = "метал",
				["TopTier"] = "МВК"
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
			player.ChatMessage(Msg(player, key, obj));
		}

		private void SendNotify(BasePlayer player, string key, int type, params object[] obj)
		{
			if (_config.UseNotify && (Notify != null || UINotify != null))
				Interface.Oxide.CallHook("SendNotify", player, type, Msg(player, key, obj));
			else
				Reply(player, key, obj);
		}

		#endregion

		#region Data 2.0

		#region Template

		private abstract class SplitDatafile<T> where T : SplitDatafile<T>, new()
		{
			public static Dictionary<string, T> LoadedData = new Dictionary<string, T>();

			protected static string[] GetFiles(string baseFolder)
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

			protected static void Save(string baseFolder, string filename)
			{
				T data;
				if (!LoadedData.TryGetValue(filename, out data))
					return;

				Interface.Oxide.DataFileSystem.WriteObject(baseFolder + filename, data);
			}

			protected static void SaveAndUnload(string baseFolder, string filename)
			{
				T data;
				if (!LoadedData.TryGetValue(filename, out data))
					return;

				Interface.Oxide.DataFileSystem.WriteObject(baseFolder + filename, data);

				LoadedData.Remove(filename);
			}

			protected static T GetOrLoad(string baseFolder, string filename)
			{
				T data;
				if (LoadedData.TryGetValue(filename, out data))
					return data;

				try
				{
					data = (T) ReadOnlyObject(baseFolder + filename);
				}
				catch (Exception e)
				{
					Interface.Oxide.LogError(e.ToString());
				}

				return LoadedData[filename] = data;
			}

			protected static T GetOrCreate(string baseFolder, string path)
			{
				return GetOrLoad(baseFolder, path) ?? (LoadedData[path] = new T());
			}

			private static object ReadOnlyObject(string name)
			{
				return Interface.Oxide.DataFileSystem.ExistsDatafile(name)
					? Interface.Oxide.DataFileSystem.GetFile(name).ReadObject<T>()
					: null;
			}
		}

		#endregion

		private class PlayerData : SplitDatafile<PlayerData>
		{
			#region Fields

			[JsonProperty(PropertyName = "Last Upgrade")]
			public DateTime LastUpgrade = new DateTime(1970, 1, 1, 0, 0, 0);

			[JsonProperty(PropertyName = "Last Remove")]
			public DateTime LastRemove = new DateTime(1970, 1, 1, 0, 0, 0);

			#endregion

			#region Main

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

			#endregion

			#region Utils

			public static readonly string BaseFolder =
				"BuildTools" + Path.DirectorySeparatorChar + "Players" + Path.DirectorySeparatorChar;

			public static string[] GetFiles()
			{
				return GetFiles(BaseFolder);
			}

			public static void Save(string filename)
			{
				Save(BaseFolder, filename);
			}

			public static void SaveAndUnload(string filename)
			{
				SaveAndUnload(BaseFolder, filename);
			}

			public static PlayerData GetOrLoad(string filename)
			{
				return GetOrLoad(BaseFolder, filename);
			}

			public static PlayerData GetOrCreate(string filename)
			{
				return GetOrCreate(BaseFolder, filename);
			}

			public static void Load()
			{
				LoadedData = new Dictionary<string, PlayerData>();
			}

			public static void Unload()
			{
				LoadedData = null;
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
			public readonly Dictionary<ulong, OldPlayerData> Players = new Dictionary<ulong, OldPlayerData>();
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

			PlayerData.LoadedData.Where(x => !players.Contains(x.Key))
				.ToList()
				.ForEach(data => { PlayerData.SaveAndUnload(data.Key); });
		}

		#endregion

		#endregion

		#endregion

		#region Testing functions

#if TESTING
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