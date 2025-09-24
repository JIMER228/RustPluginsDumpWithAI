// #define DEBUG
using System.Collections.Generic;
using System;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Text;
using System.Linq;
using Facepunch;
using Rust;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
	[Info("Personal Heli", "topplugin.ru", "1.1.4")]
	[Description("Calls heli to player and his team, with loot/damage and minig lock")]
	class PersonalHeli : RustPlugin
	{
		#region CONSTANTS
		const string permUse = "personalheli.use";
		const string permConsole = "personalheli.console";
		const float HelicopterEntitySpawnRadius = 10.0f;
		private static System.Random Rnd = new System.Random();

		private bool init;

		#endregion
		#region DEPENDENCIES
		[PluginReference]
		Plugin Friends, Clans;
		#endregion
		#region CONFIG
		public class PluginConfig
		{
			[JsonPropertyAttribute("Основные настройки", Order = 0)]
			public MainSettings MainBlock = new MainSettings();
			[JsonProperty("Настройки участников", Order = 1)]
			public TargetSettings TargetBlock = new TargetSettings();
			[JsonProperty("Настройки вертолета", Order = 2)]
			public Dictionary<string, HeliSettings> Heli = new Dictionary<string, HeliSettings>();

			public static PluginConfig GetNewConfig()
			{
				PluginConfig newConfig = new PluginConfig();
				newConfig.MainBlock = new MainSettings()
				{
					ChatCommand = "callheli",
					ChatLeaveCommand = "stopheli",
					MaxHeliCount = 1,
					Cooldowns = new Dictionary<string, int>(){
						{"personalheli.elite",300},
						{"personalheli.vip",600}
					},
					ResetCooldownsOnWipe = true,
					RetireOnAllTeamDead = true,
					LifeTimeMinutes = 15,
					BulletAccuracy = 2f
				};
				newConfig.TargetBlock = new TargetSettings()
				{
					UseFriends = true,
					UseTeams = true,
					UseClans = false
				};
				newConfig.Heli = new Dictionary<string, HeliSettings>();
				//EASY
				newConfig.Heli.Add("easy", new HeliSettings()
				{
					DifficultName = "<color=green>Легкая</color>",
					Speed = 30f,
					Health = new HeliSettings.HealthSettings()
					{
						HPBase = 10000f,
						HPFront = 900f,
						HPRear = 500f
					},
					Damage = new HeliSettings.DamageSettings()
					{
						Multiplier = 0.8f,
						ArmorMultiplier = 0.8f,
						Turrets = new HeliSettings.DamageSettings.TurretConfig()
						{
							BulletDamage = 11f,
							BulletSpeed = 350,
							FireSpeed = 0.07f,
							FireLenght = 2f,
							FireBetween = 4f,
							MaxDistance = 100f
						},
						Rockets = new HeliSettings.DamageSettings.RocketConfig()
						{
							MaxHeliRockets = 12,
							TimeBetweenRockets = 0.4f,
							BulletDamage = 175f,
							ExplosionDamage = 100f,
							ExplosionRadius = 6f
						}
					},
					Loot = new HeliSettings.LootSettings()
					{
						MaxLootCrates = 3,
						MemorizeTeamOnCall = true,
						RemoveFireFromCrates = true,
						DenyCratesLooting = true,
						DenyGibsMining = true,
						ProfileName = "easy"
					},
					ItemAction = new HeliSettings.HeliItem()
					{
						action = "unwrap",
						shortname = "xmas.present.large",
						skin = 2306487582,
						name = "Вызов легкой вертушки"
					}
				});
				//DEFAULT
				newConfig.Heli.Add("default", new HeliSettings()
				{
					DifficultName = "<color=yellow>Обычная</color>",
					Speed = 42f,
					Health = new HeliSettings.HealthSettings()
					{
						HPBase = 10000f,
						HPFront = 900f,
						HPRear = 500f
					},
					Damage = new HeliSettings.DamageSettings()
					{
						Multiplier = 1f,
						ArmorMultiplier = 1f,
						Turrets = new HeliSettings.DamageSettings.TurretConfig()
						{
							BulletDamage = 11f,
							BulletSpeed = 350,
							FireSpeed = 0.1f,
							FireLenght = 3f,
							FireBetween = 4f,
							MaxDistance = 150f
						},
						Rockets = new HeliSettings.DamageSettings.RocketConfig()
						{
							MaxHeliRockets = 12,
							TimeBetweenRockets = 0.3f,
							BulletDamage = 175f,
							ExplosionDamage = 100f,
							ExplosionRadius = 6f
						}
					},
					Loot = new HeliSettings.LootSettings()
					{
						MaxLootCrates = 4,
						MemorizeTeamOnCall = true,
						RemoveFireFromCrates = true,
						DenyCratesLooting = true,
						DenyGibsMining = true,
						ProfileName = "default"
					},
					ItemAction = new HeliSettings.HeliItem()
					{
						action = "unwrap",
						shortname = "xmas.present.large",
						skin = 2306496332,
						name = "Вызов обычной вертушки"
					}
				});
				//HARD
				newConfig.Heli.Add("hard", new HeliSettings()
				{
					DifficultName = "<color=red>Сложная</color>",
					Speed = 45f,
					Health = new HeliSettings.HealthSettings()
					{
						HPBase = 20000f,
						HPFront = 3000f,
						HPRear = 1500f
					},
					Damage = new HeliSettings.DamageSettings()
					{
						Multiplier = 1.3f,
						ArmorMultiplier = 0.8f,
						Turrets = new HeliSettings.DamageSettings.TurretConfig()
						{
							BulletDamage = 11f,
							BulletSpeed = 350,
							FireSpeed = 0.07f,
							FireLenght = 2f,
							FireBetween = 4f,
							MaxDistance = 100f
						},
						Rockets = new HeliSettings.DamageSettings.RocketConfig()
						{
							MaxHeliRockets = 12,
							TimeBetweenRockets = 0.4f,
							BulletDamage = 175f,
							ExplosionDamage = 100f,
							ExplosionRadius = 6f
						}
					},
					Loot = new HeliSettings.LootSettings()
					{
						MaxLootCrates = 5,
						MemorizeTeamOnCall = true,
						RemoveFireFromCrates = true,
						DenyCratesLooting = true,
						DenyGibsMining = true,
						ProfileName = "hard"
					},
					ItemAction = new HeliSettings.HeliItem()
					{
						action = "unwrap",
						shortname = "xmas.present.large",
						skin = 2306497849,
						name = "Вызов сложной вертушки"
					}
				});
				return newConfig;
			}

			public class HeliSettings
			{
				[JsonProperty("Наименование сложности", Order = 0)]
				public string DifficultName = "";
				[JsonProperty("Скорость полета", Order = 1)]
				public float Speed = 42f;
				
				[JsonProperty("Настройки прочности", Order = 2)]
				public HealthSettings Health = new HealthSettings();
				[JsonProperty("Настройки урона", Order = 3)]
				public DamageSettings Damage = new DamageSettings();
				[JsonProperty("Настройки лута", Order = 4)]
				public LootSettings Loot = new LootSettings();
				[JsonProperty("Настройки предмета для вызова вертушки", Order = 5)]
				public HeliItem ItemAction = new HeliItem();

				public class HeliItem
				{
					[JsonProperty("Вид события (unwrap|consume)", Order = 0)]
					public string action = "unwrap";
					[JsonProperty("Shortname предмета для вызова вертушки", Order = 1)]
					public string shortname = "xmas.present.large";
					[JsonProperty("Скин для предмета вызова вертушки", Order = 2)]
					public ulong skin = 2306487582;
					[JsonProperty("Наименование предмета", Order = 3)]
					public string name = "Вызов вертушки";
				}
				public class HealthSettings
				{
					[JsonProperty("Количество ХП корпуса", Order = 0)]
					public float HPBase = 10000f;
					[JsonProperty("Количество ХП переднего винта", Order = 1)]
					public float HPFront = 900f;
					[JsonProperty("Количество ХП заднего винта", Order = 2)]
					public float HPRear = 500f;
				}

				public class LootSettings
				{
					[JsonProperty("Количество ящиков с дропом", Order = 0)]
					public int MaxLootCrates = 4;
					[JsonProperty("Позволить вертолету запоминать кто его атаковал для последующего доступа к луту", Order = 1)]
					public bool MemorizeTeamOnCall = false;
					[JsonProperty("Убрать огонь с ящиков после падения вертолета", Order = 2)]
					public bool RemoveFireFromCrates = true;
					[JsonProperty("Ограничить доступ к луту тем кто не участвовал в сражении", Order = 3)]
					public bool DenyCratesLooting = true;
					[JsonProperty("Ограничить доступ к лутанию частей вертолета тем кто не участвовал в сражении", Order = 4)]
					public bool DenyGibsMining = true;
					[JsonProperty("Название файла профиля лута (в папке /Data/PersonalHeli/", Order = 5)]
					public string ProfileName = "default";

				}
				public class DamageSettings
				{
					[JsonProperty("Глобальный модификатор урона от вертолета", Order = 0)]
					public float Multiplier = 1f;
					[JsonProperty("Глобальный модификатор урона по вертолету", Order = 1)]
					public float ArmorMultiplier = 1f;
					[JsonProperty("Настройки пулемета", Order = 2)]
					public TurretConfig Turrets = new TurretConfig();
					[JsonProperty("Настройки ракетного залпа", Order = 3)]
					public RocketConfig Rockets = new RocketConfig();

					public class RocketConfig
					{
						[JsonProperty("Максимальное количество выпускаемых ракет", Order = 0)]
						public int MaxHeliRockets = 12;
						[JsonProperty("Интервал времени между запуском ракет", Order = 1)]
						public float TimeBetweenRockets = 0.2f;
						[JsonProperty("Урон при попадании ракеты", Order = 2)]
						public float BulletDamage = 175f;
						[JsonProperty("Урон от взрыва", Order = 3)]
						public float ExplosionDamage = 100f;
						[JsonProperty("Радиус распространения огня", Order = 4)]
						public float ExplosionRadius = 6f;
					}
					public class TurretConfig
					{
						[JsonProperty("Урон от пуль с пулемета", Order = 0)]
						public float BulletDamage = 11f;
						[JsonProperty("Скорость пули", Order = 1)]
						public int BulletSpeed = 350;
						[JsonProperty("Скорострельность пулемета", Order = 2)]
						public float FireSpeed = 0.1f;
						[JsonProperty("Длина пулеметной очереди", Order = 3)]
						public float FireLenght = 3f;
						[JsonProperty("Интервал между очередями", Order = 4)]
						public float FireBetween = 4f;
						[JsonProperty("Максимальная дистанция атаки", Order = 5)]
						public float MaxDistance = 150f;
					}
				}
			}

			public class MainSettings
			{
				[JsonProperty("Чат команда вызова вертолета на себя", Order = 0)]
				public string ChatCommand = "callheli";
				[JsonProperty("Чат команда отказа от запущенного вертолета", Order = 1)]
				public string ChatLeaveCommand = "helistop";
				[JsonProperty("Время ожидания до повторного вызова вертолета в секундах", Order = 2)]
				public Dictionary<string, int> Cooldowns = new Dictionary<string, int>();
				[JsonProperty("Максимальное количество активных вертолетов", Order = 3)]
				public int MaxHeliCount = 1;
				[JsonProperty("Максимальное время полета вертолетов в минутах (дефолт - 15)", Order = 4)]
				public int LifeTimeMinutes = 15;
				[JsonProperty("Точность вертолетов", Order = 5)]
				public float BulletAccuracy = 2f;
				[JsonProperty("Сбрасывать ожидания вызовов всех игроков при вайпе", Order = 6)]
				public bool ResetCooldownsOnWipe = true;
				[JsonProperty("В случае сметри всех атакующих вертолет улетает (иначе будет атаковать после возраждения)", Order = 7)]
				public bool RetireOnAllTeamDead = true;
			}

			public class TargetSettings
			{
				[JsonProperty("Позволять друзьям атаковать вертолет (использовать систему друзей)", Order = 0)]
				public bool UseFriends = true;
				[JsonProperty("Позволять тимейтам атаковать вертолет", Order = 1)]
				public bool UseTeams = true;
				[JsonProperty("Позволять соклановцам атаковать вертолет (использовать систему кланов)", Order = 2)]
				public bool UseClans = true;
			}
		}

		private PluginConfig config;

		public PluginConfig.HeliSettings GetHeliSettings(string difficult = "default")
		{
			if (string.IsNullOrEmpty(difficult)) return null;
			if (!config.Heli.ContainsKey(difficult)) return null;
			return config.Heli[difficult];
		}

		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				config = Config.ReadObject<PluginConfig>();
				if ((config?.MainBlock == null) || (config?.TargetBlock == null) || (config?.Heli == null)) LoadDefaultConfig();
			}
			catch
			{
				LoadDefaultConfig();
			}
			NextTick(SaveConfig);
		}

		protected override void LoadDefaultConfig() => config = PluginConfig.GetNewConfig();
		protected override void SaveConfig() => Config.WriteObject(config);
		#endregion

		#region STORED DATA

		public Dictionary<string, LootProfile> LootProfiles = new Dictionary<string, LootProfile>();

		private LootProfile LoadProfileData(string ProfileName = "default")
		{
			LootProfile LP;// = new LootProfile(){};
			try
			{
				LP = Interface.Oxide.DataFileSystem.ReadObject<LootProfile>($"PersonalHeli/{ProfileName}");
				if (LP == null)
				{
					PrintWarning($"Loot profile `{ProfileName}` was created in oxide/data/personalheli/ with default parameters. Please check it!");
					LP = LP.GetDefaultLoot();
					Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject($"PersonalHeli/{ProfileName}", LP);
				}
				else if (LP.Loots == null || LP.Loots.Count == 0)
				{
					PrintWarning($"Loot profile `{ProfileName}` was created in oxide/data/personalheli/ with default parameters. Please check it!");
					LP = LP.GetDefaultLoot();
					Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject($"PersonalHeli/{ProfileName}", LP);
				}
				return LP;
			}
			catch
			{
				PrintError($"File oxide/data/personalheli/{ProfileName} is missing or contains errors");
				return null;
			}
			return null;
		}

		public class LootProfile
		{
			[JsonProperty("Минимум предметов в контейнере", Order = 5)]
			public int MinAmount;
			[JsonProperty("Максимум предметов в контейнере", Order = 6)]
			public int MaxAmount;
			[JsonProperty("Список лута для контейнера", Order = 7)]
			public List<LootPack> Loots = new List<LootPack>();

			public class LootPack
			{
				[JsonProperty("Название лута", Order = 0)]
				public string LootName;
				[JsonProperty("Минимальное количество", Order = 1)]
				public int MinAmount;
				[JsonProperty("Максимальное количество", Order = 2)]
				public int MaxAmount;
				[JsonProperty("Скин предмета (если не нужно то 0", Order = 3)]
				public ulong SkinID = 0;
				[JsonProperty("Уникальное имя (если не нужно то null", Order = 4)]
				public string AddName = null;
			}
			public LootProfile GetDefaultLoot()
			{
				return new LootProfile
				{
					MinAmount = 2,
					MaxAmount = 4,
					Loots = new List<LootProfile.LootPack>()
					{
						new LootProfile.LootPack() { LootName = "ammo.rifle", MinAmount = 64, MaxAmount = 128, SkinID=0,AddName=null},
						new LootProfile.LootPack() { LootName = "ammo.rifle.explosive", MinAmount = 16, MaxAmount = 64, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "ammo.rifle.incendiary", MinAmount = 64, MaxAmount = 128, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "ammo.rifle.hv", MinAmount = 64, MaxAmount = 128, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "weapon.mod.holosight", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "weapon.mod.silencer", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "weapon.mod.lasersight", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "weapon.mod.small.scope", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "weapon.mod.8x.scope", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "weapon.mod.flashlight", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "lmg.m249", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "explosive.timed", MinAmount = 1, MaxAmount = 2, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "ammo.rocket.basic", MinAmount = 1, MaxAmount = 3, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "targeting.computer", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "ammo.rocket.fire", MinAmount = 1, MaxAmount = 5, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "ammo.rocket.hv", MinAmount = 1, MaxAmount = 3, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "ammo.pistol", MinAmount = 128, MaxAmount = 256, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "ammo.pistol.hv", MinAmount = 64, MaxAmount = 128, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "ammo.pistol.fire", MinAmount = 64, MaxAmount = 128, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "wall.window.bars.toptier", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "shotgun.spas12", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "rifle.m39", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "rifle.lr300", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "door.hinged.toptier", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "floor.ladder.hatch", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "techparts", MinAmount = 5, MaxAmount = 12, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "door.double.hinged.toptier", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "rifle.l96", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "pistol.m92", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "smg.mp5", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "rifle.ak", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "explosives", MinAmount = 1, MaxAmount = 2, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "cctv.camera", MinAmount = 1, MaxAmount = 2, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "smg.2", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
						new LootProfile.LootPack() { LootName = "wall.frame.garagedoor", MinAmount = 1, MaxAmount = 1, SkinID=0,AddName=null },
					}
				};
			}
		}
		public class StoredData
		{
			public Dictionary<ulong, CallData> CallDatas = new Dictionary<ulong, CallData>();

			public class CallData
			{
				public DateTime LastCall = DateTime.MinValue;
				public bool CanCallNow(int cooldown)
				{
					return DateTime.Now.Subtract(LastCall).TotalSeconds > cooldown;
				}

				public int SecondsToWait(int cooldown)
				{
					return (int)Math.Round(cooldown - DateTime.Now.Subtract(LastCall).TotalSeconds);
				}

				public void OnCall()
				{
					LastCall = DateTime.Now;
				}
			}

			public CallData GetForPlayer(BasePlayer player)
			{
				if (!CallDatas.ContainsKey(player.userID))
				{
					CallDatas[player.userID] = new CallData();
				}

				return CallDatas[player.userID];
			}
		}
		private void SaveData()
		{
			if (storedData != null)
			{
				Interface.Oxide.DataFileSystem.WriteObject($"PersonalHeli/{Name}", storedData, true);
			}
		}
		private void LoadData()
		{
			storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>($"PersonalHeli/{Name}");
			if (storedData == null)
			{
				storedData = new StoredData();
				SaveData();
			}
		}
		private StoredData storedData;
		#endregion
		#region L10N
		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["NoPermission"] = "You have no permission to use this command",
				["Cooldown"] = "Helicopter call is on cooldown, time remaining: {0}",
				["LootDenied"] = "You are forbidden to loot this crate, it belongs to: {0}",
				["DamageDenied"] = "You are forbidden to damage this helicopter, it was called by: {0}",
				["MiningDenied"] = "You are forbidden to mine this debris, it belongs to: {0}",
				["Friends"] = "their friends",
				["Team"] = "their team",
				["Clan"] = "their clan",
				["CmdUsage"] = "Invalid format, usage: personalheli.call {{steamId}}",
				["InvalidSteamId"] = "{0} is invalid Steam ID",
				["PlayerNotFound"] = "Player with id {0} was not found",
				["PlayerCalled"] = "Personal helicopter is called for {0}\nDifficult is {1}",
				["PlayerCanceled"] = "Personal helicopter is stoped for {0}. Difficult is: {1}",
				["HeliNotFound"] = "No personal helicopter found. Perhaps you didn't call it",
				["LimitHeli"] = "You can't call a helicopter. Reached the maximum limit",
				["KillHeli"] = "Player {0} kill Helicopter with difficult {1}"
			}, this);

			lang.RegisterMessages(new Dictionary<string, string>
			{
				["NoPermission"] = "У Вас нет прав на использование этой команды",
				["Cooldown"] = "Вызов вертолета в кулдауне, ждать осталось: {0}",
				["HeliSuccess"] = "Вертолет был вызван",
				["LootDenied"] = "Вам запрещено лутать этот ящик, его могут лутать: {0}",
				["DamageDenied"] = "Вам запрещено наносить урон этому вертолету, его вызвал: {0}",
				["MiningDenied"] = "Вам запрещено добывать эти обломки, их могут добывать: {0}",
				["Friends"] = "его друзья",
				["Team"] = "его команда",
				["Clan"] = "его клан",
				["CmdUsage"] = "Неправильный формат, использование: personalheli.call {{steamId}}",
				["InvalidSteamId"] = "{0} не является Steam ID",
				["PlayerNotFound"] = "Игрок с ID {0} не найден",
				["PlayerCalled"] = "Вертолет был вызван для {0}\nСложность: {1}",
				["PlayerCanceled"] = "Вертолет был отозван игроком {0}. В ближайшее время вертолет улетит.\nСложность: {1}",
				["HeliNotFound"] = "Не найдено персонального вертолета. Возможно вы его не вызывали",
				["LimitHeli"] = "Вы не можете вызвать вертолет. Достигнут лимит. Попробуйте позднее.",
				["KillHeli"] = "Игрок {0} уничтожил вертолет\nСложность: {1}"
			}, this, "ru");
		}
		private string GetMsg(string key, string userId, params object[] args)
		{
			return string.Format(lang.GetMessage(key, this, userId), args);
		}
		#endregion
		#region HOOKS
		private void Init()
		{
			permission.RegisterPermission(permUse, this);
			permission.RegisterPermission(permConsole, this);
			foreach (string perms in config.MainBlock.Cooldowns.Keys)
			{
				permission.RegisterPermission(perms, this);
			}
			cmd.AddChatCommand(config.MainBlock.ChatCommand, this, CmdCallHeli);
			cmd.AddChatCommand(config.MainBlock.ChatLeaveCommand, this, CmdLeaveMeAlone);
			LoadData();
		}

		public int GetCooldown(string playerId)
		{
			int def = 2700;
			foreach (KeyValuePair<string, int> perms in config.MainBlock.Cooldowns)
			{
				if (permission.UserHasPermission(playerId, perms.Key)) return perms.Value;
			}
			return def;
		}

		private void OnServerInitialized()
		{
			ConVar.PatrolHelicopter.bulletAccuracy = config.MainBlock.BulletAccuracy;
			ConVar.PatrolHelicopter.lifetimeMinutes = config.MainBlock.LifeTimeMinutes;

			//ДОБАВЛЯЕМ ПАРАМЕТРЫ ЛУТА
			foreach (PluginConfig.HeliSettings HeliSets in config.Heli.Values)
			{
				if (!string.IsNullOrEmpty(HeliSets.Loot.ProfileName))
				{
					LootProfile profile = LoadProfileData(HeliSets.Loot.ProfileName);
					if (profile == null)
					{
						PrintError("Missing loot profile");
						return;
					}
					LootProfiles.Add(HeliSets.Loot.ProfileName, profile);
				}
			}
			init = true;
		}

		private void Unload()
		{
			foreach (var personal in UnityEngine.Object.FindObjectsOfType<PersonalComponent>())
			{
				UnityEngine.Object.Destroy(personal);
			}
			SaveData();
		}
		private void OnNewSave()
		{
			if (config.MainBlock.ResetCooldownsOnWipe)
			{
				storedData = new StoredData();
				SaveData();
			}
		}

		private void OnServerSave()
		{
			SaveData();
		}


		private void OnEntityKill(BaseEntity entity)
		{
			if (entity == null || entity.gameObject == null) return;
			if (!(entity is BaseHelicopter)) return;
			InvokePersonal<PersonalHeliComponent>(entity.gameObject, personalHeli => personalHeli.OnKill());
		}

		private object CanLootEntity(BasePlayer player, StorageContainer container)
		{
			return InvokePersonal<PersonalCrateComponent, object>(container?.gameObject, personalCrate => {
				var result = personalCrate.CanInterractWith(player);
				if (result == false)
				{
					SendReply(player, GetMsg("LootDenied", player.UserIDString, GetPlayerOwnerDescription(player, personalCrate.Player)));
					return false;
				}
				return null;
			});
		}

		public void BroadcastMessage(string Message)
		{
			foreach (var p in BasePlayer.activePlayerList)
			{
				SendReply(p, Message);
			}
			return;
		}

		private void OnEntitySpawned(BaseNetworkable entity)
		{
			if (!init || entity == null || entity.IsDestroyed || entity.gameObject == null) return;
			var prefabname = entity?.ShortPrefabName ?? string.Empty;
			var longprefabname = entity?.PrefabName ?? string.Empty;
			if (string.IsNullOrEmpty(prefabname) || string.IsNullOrEmpty(longprefabname)) return;
			if (prefabname.Contains("rocket_heli"))
			{
				TimedExplosive explosion = entity as TimedExplosive;
				PluginConfig.HeliSettings HeliSet = GetHeliSettings("default");
				//Не смог определить каким вертолетом выпущены ракеты
				if (explosion == null || explosion.IsDestroyed || explosion.gameObject == null) return; //super ultra extra safe null checking
				if (HeliSet.Damage.Rockets.MaxHeliRockets < 1) explosion.Kill();
				else
				{
					explosion.explosionRadius = HeliSet.Damage.Rockets.ExplosionRadius;


					var dmgTypes = explosion?.damageTypes ?? null;

					if (dmgTypes != null && dmgTypes.Count > 0)
					{
						for (int i = 0; i < dmgTypes.Count; i++)
						{
							var dmg = dmgTypes[i];
							if (dmg == null) continue;
							if (dmg.type == Rust.DamageType.Blunt) dmg.amount = HeliSet.Damage.Rockets.BulletDamage;
							if (dmg.type == Rust.DamageType.Explosion) dmg.amount = HeliSet.Damage.Rockets.ExplosionDamage;
						}
					}
				}
			}
		}

		private object OnHelicopterTarget(HelicopterTurret turret, BaseCombatEntity entity)
		{
			if (turret == null || entity == null) return null;
			return InvokePersonal<PersonalHeliComponent, object>(turret?._heliAI?.helicopterBase?.gameObject, personalHeli => {
				var result = personalHeli.CanInterractWith(entity);
				return result ? null : (object)false;
			});
		}

		private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
		{
			if (info == null || entity == null) return;

			if (entity is BaseHelicopter)
			{
				PersonalHeliComponent Heli = (entity as BaseHelicopter)?.GetComponent<PersonalHeliComponent>() ?? null;
				if (Heli == null) return;
				BroadcastMessage(GetMsg("KillHeli", Heli.Player.UserIDString, $"<color=#63ff64>{Heli.Player.displayName}</color>", Heli.HeliSets.DifficultName));
				InvokePersonal<PersonalHeliComponent>(entity.gameObject, personalHeli => personalHeli.OnDeath());

			}
			if (!config.MainBlock.RetireOnAllTeamDead)
			{
				return;
			}
			if (!(entity is BasePlayer))
			{
				return;
			}
			NextTick(() => {
				foreach (var heli in PersonalHeliComponent.ActiveHelis)
				{
					heli.OnPlayerDied(entity as BasePlayer);
				}
			});
		}
		object OnItemAction(Item item, string action, BasePlayer player)
		{
			if (item == null) return null;
			foreach (KeyValuePair<string, PluginConfig.HeliSettings> h in config.Heli)
			{
				PluginConfig.HeliSettings heli = h.Value;
				if (heli == null || heli.ItemAction == null) continue;
				if (action != heli.ItemAction.action || item.skin != heli.ItemAction.skin) continue;
				if (CallHeliForPlayer(player, h.Key, true, true))
				{
					item.UseItem(1);
				}
				else return false;
			}
			return null;
		}


		//Множитель урона вертолета
		private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
		{
			if (entity == null || info?.HitEntity == null) return;
			if (info.Initiator is BaseHelicopter && entity is BasePlayer)
			{
				BaseHelicopter Heli = info.Initiator as BaseHelicopter;
				if (Heli == null) return;
				//Пытаемся понять что за верт нас дамажет
				PersonalComponent HeliC = Heli?.GetComponent<PersonalComponent>() ?? null;
				if (HeliC == null) return;
				if (entity is NPCPlayer) return;
				float multiplier = HeliC.HeliSets.Damage.Multiplier;
				if (multiplier != 1f && multiplier >= 0)
				{
					info?.damageTypes?.ScaleAll(multiplier);
					return;
				}
			}
			if (info.Initiator is BasePlayer && entity is BaseHelicopter)
			{
				BaseHelicopter Heli = entity as BaseHelicopter;
				if (Heli == null) return;
				//Пытаемся понять по какому верту мы дамажем
				PersonalComponent HeliC = Heli?.GetComponent<PersonalComponent>() ?? null;
				if (HeliC == null) return;
				if (info.Initiator is NPCPlayer) return;
				
				float Armormultiplier = HeliC.HeliSets.Damage.ArmorMultiplier;
				if (Armormultiplier != 1f && Armormultiplier >= 0)
				{
					info?.damageTypes?.ScaleAll(Armormultiplier);
				}//Фиксируем ХП у вертолета  для защиты от регена
				if (Heli.health > HeliC.LastHealth) 
				{
					PrintWarning($"Regeniration is stoped! {Heli.health} > {HeliC.LastHealth}");
					Heli.Hurt(100000f);
					Heli.AdminKill();
					return;
					//Heli.SendNetworkUpdate();
					//Heli.SendNetworkUpdateImmediate(false);
				}
				HeliC.LastHealth = Heli.health;
			}
		}

		private object OnPlayerAttack(BasePlayer attacker, HitInfo info)
		{
			if (attacker == null || info?.HitEntity == null) return null;
			//При ударении по частям вертолета
			if (info.HitEntity is ServerGib && info.WeaponPrefab is BaseMelee)
			{
				return InvokePersonal<PersonalGibComponent, object>(info?.HitEntity?.gameObject, personalGib => {
					var result = personalGib.CanInterractWith(attacker);
					if (result == false)
					{
						SendReply(info.InitiatorPlayer, GetMsg("MiningDenied", info.InitiatorPlayer.UserIDString, GetPlayerOwnerDescription(info.InitiatorPlayer, personalGib.Player)));
						return false;
					}
					return null;
				});
			}

			return InvokePersonal<PersonalHeliComponent, object>(info?.HitEntity?.gameObject, personalHeli => {
				var result = personalHeli.CanInterractWith(attacker);
				if (result == false)
				{
					SendReply(info.InitiatorPlayer, GetMsg("DamageDenied", info.InitiatorPlayer.UserIDString, GetPlayerOwnerDescription(info.InitiatorPlayer, personalHeli.Player)));
					return false;
				}
				return null;
			});
		}

		private static bool IsHeliBox(LootContainer container)
		{
			if (container == null)
				return false;

			if (container.ShortPrefabName == "heli_crate")
				return true;

			return false;
		}

		private object CanHelicopterStrafeTarget(PatrolHelicopterAI heliAi, BasePlayer target)
		{
			if (heliAi == null) return null;
			if ((heliAi?._currentState ?? PatrolHelicopterAI.aiState.DEATH) == PatrolHelicopterAI.aiState.DEATH) return false;
			return InvokePersonal<PersonalHeliComponent, object>(heliAi?.helicopterBase?.gameObject, personalHeli => {
				return personalHeli.CanInterractWith(target) ? null : (object)false;
			});
		}

		private object CanHelicopterStrafe(PatrolHelicopterAI heliAi)
		{
			if (heliAi == null) return null;
			if ((heliAi?._currentState ?? PatrolHelicopterAI.aiState.DEATH) == PatrolHelicopterAI.aiState.DEATH) return false;
			return null;
		}

		private object CanHelicopterTarget(PatrolHelicopterAI heliAi, BasePlayer player)
		{
			if (heliAi == null) return null;
			if ((heliAi?._currentState ?? PatrolHelicopterAI.aiState.DEATH) == PatrolHelicopterAI.aiState.DEATH) return false;
			return InvokePersonal<PersonalHeliComponent, object>(heliAi?.helicopterBase?.gameObject, personalHeli => {
				return personalHeli.CanInterractWith(player) ? null : (object)false;
			});
		}

		#endregion

		private void UpdateHeli(BaseHelicopter heli, bool justCreated = false)
		{
			if (heli == null || heli.IsDestroyed || heli.IsDead()) return;
			PersonalComponent HeliC = heli?.GetComponent<PersonalComponent>() ?? null;
			if (HeliC == null) return;
			heli._maxHealth = HeliC.HeliSets.Health.HPBase;
			heli.startHealth = HeliC.HeliSets.Health.HPBase;
			heli.SetMaxHealth(heli.startHealth);
			if (justCreated) heli.InitializeHealth(HeliC.HeliSets.Health.HPBase, HeliC.HeliSets.Health.HPBase);
			heli.maxCratesToSpawn = HeliC.HeliSets.Loot.MaxLootCrates;
			heli.bulletDamage = HeliC.HeliSets.Damage.Turrets.BulletDamage;
			heli.bulletSpeed = HeliC.HeliSets.Damage.Turrets.BulletSpeed;
			var weakspots = heli.weakspots;
			
			if (weakspots != null && weakspots.Length > 1)
			{
				if (justCreated)
				{
					weakspots[0].health = HeliC.HeliSets.Health.HPFront;
					weakspots[1].health = HeliC.HeliSets.Health.HPRear;
				}
				weakspots[0].maxHealth = HeliC.HeliSets.Health.HPFront;
				weakspots[1].maxHealth = HeliC.HeliSets.Health.HPRear;
			}
			var heliAI = heli?.GetComponent<PatrolHelicopterAI>() ?? null;
			if (heliAI == null) return;
			heliAI.maxSpeed = Mathf.Clamp(HeliC.HeliSets.Speed, 0.1f, 125);
			heliAI.timeBetweenRockets = Mathf.Clamp(HeliC.HeliSets.Damage.Rockets.TimeBetweenRockets, 0.1f, 1f);
			heliAI.numRocketsLeft = Mathf.Clamp(HeliC.HeliSets.Damage.Rockets.MaxHeliRockets, 0, 48);
			UpdateTurrets(heliAI);
			heli.SendNetworkUpdateImmediate(justCreated);
		}

		private void UpdateTurrets(PatrolHelicopterAI helicopter)
		{
			if (helicopter == null || helicopter.leftGun == null || helicopter.rightGun == null) return;
			PersonalComponent HeliC = helicopter?.GetComponent<PersonalComponent>() ?? null;
			if (HeliC == null) return;
			helicopter.leftGun.fireRate = (helicopter.rightGun.fireRate = HeliC.HeliSets.Damage.Turrets.FireSpeed);
			helicopter.leftGun.timeBetweenBursts = (helicopter.rightGun.timeBetweenBursts = HeliC.HeliSets.Damage.Turrets.FireBetween);
			helicopter.leftGun.burstLength = (helicopter.rightGun.burstLength = HeliC.HeliSets.Damage.Turrets.FireLenght);
			helicopter.leftGun.maxTargetRange = (helicopter.rightGun.maxTargetRange = HeliC.HeliSets.Damage.Turrets.MaxDistance);
		}

		private bool CallHeliForPlayer(BasePlayer player, string difficult = "default", bool check = false, bool admin = false)
		{
			if (check)
			{

				if (!admin && !permission.UserHasPermission(player.UserIDString, permUse))
				{
					SendReply(player, GetMsg("NoPermission", player.UserIDString));
					return false;
				}
				if (!CheckHeli())
				{
					SendReply(player, GetMsg("LimitHeli", player.UserIDString));
					return false;
				}
				StoredData.CallData callData = storedData.GetForPlayer(player);
				if (!callData.CanCallNow(GetCooldown(player.UserIDString)))
				{
					SendReply(player, GetMsg("Cooldown", player.UserIDString, TimeSpan.FromSeconds(callData.SecondsToWait(GetCooldown(player.UserIDString)))));
					return false;
				}
			}
			var playerPos = player.transform.position;
			float mapWidth = (TerrainMeta.Size.x / 2) - 50f;
			var heliPos = new Vector3(
				playerPos.x < 0 ? -mapWidth : mapWidth,
				30,
				playerPos.z < 0 ? -mapWidth : mapWidth
			);

			if (!config.Heli.ContainsKey(difficult))
			{
				player.ChatMessage("Неверно указана сложность вертолета");
				return false;
			}
			BaseHelicopter heli = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(), new Quaternion(), true) as BaseHelicopter;
			if (!heli) return false;
			PatrolHelicopterAI heliAI = heli?.GetComponent<PatrolHelicopterAI>() ?? null;
			if (heliAI == null) return false;
			heli.Spawn();
			heli.transform.position = heliPos;
			heli.SendNetworkUpdate();
			var component = heli.gameObject.AddComponent<PersonalHeliComponent>();
			component.Init(this, heli, player, config.Heli[difficult]);
			UpdateHeli(heli, true);
			foreach (var p in BasePlayer.activePlayerList)
			{
				SendReply(p, GetMsg("PlayerCalled", p.UserIDString, $"<color=#63ff64>{player.displayName}</color>", config.Heli[difficult].DifficultName));
			}
			return true;
		}
		#region API
		private bool IsPersonal(BaseHelicopter heli) => InvokePersonal<PersonalHeliComponent, object>(heli?.gameObject, (comp) => true) == null ? false : true;

		#endregion
		private bool CheckHeli()
		{
			int count = BaseNetworkable.serverEntities.Count(x =>
			{
				var entity = x as BaseHelicopter;
				if (entity != null)
				{
					return true;
				}
				return false;
			}
			);
			if (count >= config.MainBlock.MaxHeliCount) return false;
			return true;
		}

		[ConsoleCommand("personalheli.call")]
		private void CmdCallHeliConsole(ConsoleSystem.Arg arg)
		{
			if (arg.Player() != null)
			{
				if (!permission.UserHasPermission(arg.Player().UserIDString, permConsole))
				{
					PrintToConsole(arg.Player(), GetMsg("NoPermission", arg.Player().UserIDString));
					return;
				}
			}
			Action<string> printToConsole;
			if (arg.Player() == null)
			{
				printToConsole = (str) => Puts(str);
			}
			else
			{
				printToConsole = (str) => PrintToConsole(arg.Player(), str);
			}

			string UserId = arg.Player() == null ? "" : arg.Player().UserIDString;
			if (!arg.HasArgs())
			{
				printToConsole(GetMsg("CmdUsage", UserId));
				return;
			}

			if (!arg.Args[0].IsSteamId())
			{
				printToConsole(GetMsg("InvalidSteamId", UserId, arg.Args[0]));
				return;
			}

			var player = BasePlayer.FindByID(ulong.Parse(arg.Args[0]));
			if (player == null)
			{
				player = BasePlayer.FindSleeping(ulong.Parse(arg.Args[0]));
			}

			if (player == null)
			{
				printToConsole(GetMsg("PlayerNotFound", UserId, arg.Args[0]));
				return;
			}
			string difficult = "default";
			if (arg.Args.Length > 1) difficult = arg.Args[1];
			if (CallHeliForPlayer(player, difficult, false, true))
			{
				printToConsole(GetMsg("PlayerCalled", UserId, player.displayName, difficult));
			}
		}


		[ConsoleCommand("giveCall")]
		void GiveHeliConsoleCommand(ConsoleSystem.Arg arg)
		{
			if (arg.Player() != null)
			{
				if (!(arg.Player().IsAdmin) && !permission.UserHasPermission(arg.Player().UserIDString, permConsole))
				{
					PrintToConsole(arg.Player(), GetMsg("NoPermission", arg.Player().UserIDString));
					return;
				}
			}
			if (arg == null || arg.Args?.Length < 2)
			{
				return;
			}
			BasePlayer player = BasePlayer.Find(arg.Args[0]);
			if (player == null)
			{
				PrintError($"[Error] Player {arg.Args[0]} not found");
				return;
			}
			string difficult = arg.Args[1];
			if (!config.Heli.ContainsKey(difficult))
			{
				PrintError("Difficult not found");
				return;
			}
			int count = 1;
			if (arg.Args?.Length > 2) if (!int.TryParse(arg.Args[2], out count)) count = 1;
			PluginConfig.HeliSettings.HeliItem act = config.Heli[difficult].ItemAction;
			if (act == null)
			{
				PrintError("ItemAction for this Difficult not found");
				return;
			}
			Item item = ItemManager.CreateByName(act.shortname, count, act.skin);
			item.name = act.name;
			GiveItem(item, player);
		}

		private static void GiveItem(Item item, Vector3 position)
		{
			if (item == null) return;
			item.Drop(position, Vector3.down);
		}

		private void GiveItem(Item item, BasePlayer player)
		{
			if (item == null) return;
			player.GiveItem(item);
			Puts($"Item was gave successfully to {player.displayName}");
		}
		
		private void CmdCallHeli(BasePlayer player, string cmd, string[] argv)
		{
			if (!permission.UserHasPermission(player.UserIDString, permUse))
			{
				SendReply(player, GetMsg("NoPermission", player.UserIDString));
				return;
			}
			if (!CheckHeli())
			{
				SendReply(player, GetMsg("LimitHeli", player.UserIDString));
				return;
			}
			StoredData.CallData callData = storedData.GetForPlayer(player);
			if (!callData.CanCallNow(GetCooldown(player.UserIDString)))
			{
				SendReply(player, GetMsg("Cooldown", player.UserIDString, TimeSpan.FromSeconds(callData.SecondsToWait(GetCooldown(player.UserIDString)))));
				return;
			}
			string difficult = "default";
			if (argv.Length > 0) difficult = argv[0];
			if (CallHeliForPlayer(player, difficult))
			{
				callData.OnCall();
			}
		}
		
		private void CmdLeaveMeAlone(BasePlayer player, string cmd, string[] argv)
		{
			if (!permission.UserHasPermission(player.UserIDString, permUse))
			{
				SendReply(player, GetMsg("NoPermission", player.UserIDString));
				return;
			}
			//Ищем вертолеты, который вы запустили
			foreach (PersonalHeliComponent personal in UnityEngine.Object.FindObjectsOfType<PersonalHeliComponent>())
			{
				if (player==personal.Player){		
					personal.LeaveMeAlone(); 
					return;
				}
			}
			SendReply(player, GetMsg("HeliNotFound", player.UserIDString));
		}
		private string GetPlayerOwnerDescription(BasePlayer player, BasePlayer playerOwner)
		{
			StringBuilder result = new StringBuilder($"<color=#63ff64>{playerOwner.displayName}</color>");
			if (config.TargetBlock.UseFriends && Friends != null)
			{
				result.Append($", {GetMsg("Friends", player.UserIDString)}");
			}
			if (config.TargetBlock.UseTeams)
			{
				result.Append($", {GetMsg("Team", player.UserIDString)}");
			}
			if (config.TargetBlock.UseClans)
			{
				result.Append($", {GetMsg("Clan", player.UserIDString)}");
			}
			return result.ToString();
		}

		private T InvokePersonal<C, T>(GameObject obj, Func<C, T> action) where C : PersonalComponent
		{
			var comp = obj?.GetComponent<C>();
			if (comp == null) return default(T);
			return action(comp);
		}
		private void InvokePersonal<C>(GameObject obj, Action<C> action) where C : PersonalComponent => InvokePersonal<C, object>(obj, comp => { action(comp); return null; });

		abstract class PersonalComponent : FacepunchBehaviour
		{
			protected PersonalHeli Plugin;
			protected string difficult;
			protected PluginConfig Config => Plugin.config;
			public PluginConfig.HeliSettings HeliSets;
			public List<BasePlayer> SavedTeam;
			public BasePlayer Player;
			public BaseHelicopter Heli;
			public float LastHealth  = 1000000f;

			public void Init(PersonalHeli plugin, BaseHelicopter heli, BasePlayer player, PluginConfig.HeliSettings HeliSet)
			{
				Player = player;
				Plugin = plugin;
				Heli = heli;
				HeliSets = HeliSet;
				OnInitChild();
			}

			protected virtual void OnInitChild() { }

			public virtual bool CanInterractWith(BaseEntity target)
			{
				if (HeliSets.Loot.MemorizeTeamOnCall && SavedTeam != null)
				{
					return SavedTeam.Contains(target as BasePlayer);
				}

				if (!(target is BasePlayer) || target is NPCPlayer)
				{
					return false;
				}

				if (target == Player)
				{
					return true;
				}

				if (Plugin.config.TargetBlock.UseFriends)
				{
					if (AreFriends(target as BasePlayer))
					{
						return true;
					}
				}

				if (Plugin.config.TargetBlock.UseTeams)
				{
					if (AreSameTeam(target as BasePlayer))
					{
						return true;
					}
				}

				if (Plugin.config.TargetBlock.UseClans)
				{
					if (AreSameClan(target as BasePlayer))
					{
						return true;
					}
				}

				return false;
			}

			protected bool AreSameClan(BasePlayer basePlayer)
			{
				if (Plugin.Clans == null)
				{
					return false;
				}
				var playerClan = Plugin.Clans.Call<string>("GetClanOf", Player);
				var otherPlayerClan = Plugin.Clans.Call<string>("GetClanOf", basePlayer);
				if (playerClan == null || otherPlayerClan == null)
				{
					return false;
				}

				return playerClan == otherPlayerClan;
			}

			protected bool AreSameTeam(BasePlayer otherPlayer)
			{
				if (Player.currentTeam == 0UL || otherPlayer.currentTeam == 0UL)
				{
					return false;
				}

				return Player.currentTeam == otherPlayer.currentTeam;
			}

			protected bool AreFriends(BasePlayer otherPlayer)
			{
				if (Plugin.Friends == null)
				{
					return false;
				}
				return Plugin.Friends.Call<bool>("AreFriends", Player.userID, otherPlayer.userID);
			}

			private void OnDestroy()
			{
				OnDestroyChild();
			}
			protected virtual void OnDestroyChild() { }
		}
		class PersonalCrateComponent : PersonalComponent
		{
			private LootContainer Crate;
			private void Awake()
			{
				Crate = GetComponent<LootContainer>();
				if (Crate == null)
				{
					return;
				}
			}

			public void FillLoot(PersonalHeli.LootProfile Loot)
			{

				Crate.inventory.Clear();
				ItemManager.DoRemoves();
				if (Loot == null) return;

				var amountLoots = Rnd.Next(Loot.MinAmount, Loot.MaxAmount + 1);

				for (int ii = 0; ii < amountLoots; ii++)
				{
					var itemCfg = Loot.Loots.OrderBy(x => Rnd.Next()).FirstOrDefault();
					var amount = Rnd.Next(itemCfg.MinAmount, itemCfg.MaxAmount + 1);
					if (amount <= 0) continue;

					var item = ItemManager.CreateByName(itemCfg.LootName, amount, itemCfg.SkinID);
					if (!string.IsNullOrEmpty(itemCfg.AddName)) item.name = itemCfg.AddName;
					if (item == null) continue;

					if (Crate.inventory.capacity < Crate.inventory.itemList.Count() + 1)
						Crate.inventory.capacity++;

					item.MoveToContainer(Crate.inventory, -1, true);
				}

				Crate.inventory.capacity = Crate.inventory.itemList.Count;
			}
			protected override void OnDestroyChild()
			{
				if (Crate != null && Crate.IsValid() && !Crate.IsDestroyed)
				{
					Crate.Kill();
				}
			}
		}
		class PersonalGibComponent : PersonalComponent
		{
			private HelicopterDebris Gib;
			private void Awake()
			{
				Gib = GetComponent<HelicopterDebris>();
			}
			protected override void OnDestroyChild()
			{
				if (Gib != null && Gib.IsValid() && !Gib.IsDestroyed)
				{
					Gib.Kill();
				}
			}
		}
		class PersonalHeliComponent : PersonalComponent
		{
			private const int MaxHeliDistanceToPlayer = 140;
			public bool isKilled = false;
			public static List<PersonalHeliComponent> ActiveHelis = new List<PersonalHeliComponent>();
			private BaseHelicopter Heli;
			private PatrolHelicopterAI HeliAi => Heli.GetComponent<PatrolHelicopterAI>();


			private void Awake()
			{
				Heli = this.GetComponent<BaseHelicopter>();
			}
			protected override void OnInitChild()
			{
				HeliAi.State_Move_Enter(Player.transform.position + new Vector3(UnityEngine.Random.Range(10f, 50f), 20f, UnityEngine.Random.Range(10f, 50f)));
				InvokeRepeating(new Action(UpdateTargets), 5.0f, 5.0f);
				if (HeliSets.Loot.MemorizeTeamOnCall)
				{
					SavedTeam = GetAllPlayersInTeam();
				}
				ActiveHelis.Add(this);
#if DEBUG
				InvokeRepeating(new Action(TraceState), 5.0f, 5.0f);
#endif
			}
#if DEBUG
			private void TraceState()
			{
				Plugin.Server.Broadcast($"helicopter: {Heli.transform.position}: {HeliAi._currentState.ToString()}");
				Plugin.Server.Broadcast(string.Join(", ", HeliAi._targetList.Select(tg => tg.ply.displayName)));
				Plugin.Server.Broadcast($"heli at destionation {Vector3.Distance(Heli.transform.position, HeliAi.destination)}");
			}
#endif
			private void UpdateTargets()
			{
				if (HeliAi._targetList.Count == 0)
				{
					List<BasePlayer> team = HeliSets.Loot.MemorizeTeamOnCall ? SavedTeam : GetAllPlayersInTeam();
					foreach (var player in team)
					{
						if (player != null && player.IsConnected)
						{
							HeliAi._targetList.Add(new PatrolHelicopterAI.targetinfo(Player, Player));
						}
					}
				}

				if (HeliAi._targetList.Count == 1 && HeliAi._targetList[0].ply == Player &&
					Vector3Ex.Distance2D(Heli.transform.position, Player.transform.position) > MaxHeliDistanceToPlayer)
				{
					if (HeliAi._currentState != PatrolHelicopterAI.aiState.MOVE || Vector3Ex.Distance2D(HeliAi.destination, Player.transform.position) > MaxHeliDistanceToPlayer)
					{
						HeliAi.ExitCurrentState();
						var heliTarget = Player.transform.position.XZ() + Vector3.up * 250;
						RaycastHit hit;
						if (Physics.SphereCast(Player.transform.position.XZ() + Vector3.up * 600, 50, Vector3.down, out hit, 1500, Layers.Solid))
						{
							heliTarget = hit.point + Vector3.up * 20;
						}
#if DEBUG
						Plugin.Server.Broadcast($"Forcing helicopter {Heli.transform.position} to player {Player.displayName}, pos {heliTarget}");
#endif
						HeliAi.State_Move_Enter(heliTarget);
					}
				}
			}

			protected override void OnDestroyChild()
			{
				CancelInvoke(new Action(UpdateTargets));
#if DEBUG
				CancelInvoke(new Action(TraceState));
#endif
				if (Heli != null && Heli.IsValid() && !Heli.IsDestroyed)
				{
					Heli.Kill();
				}

				ActiveHelis.Remove(this);
			}

			private List<BasePlayer> GetAllPlayersInTeam()
			{
				var fullTeam = new List<BasePlayer>();
				foreach (var player in BasePlayer.activePlayerList)
				{
					if (player == Player)
					{
						fullTeam.Add(player);
					}
					else if ((Config.TargetBlock.UseFriends && AreFriends(player)) ||
						  Plugin.config.TargetBlock.UseClans && AreSameClan(player) ||
						  Plugin.config.TargetBlock.UseTeams && AreSameTeam(player))
					{
						fullTeam.Add(player);
					}
				}

				return fullTeam;
			}

			public void OnDeath()
			{
				isKilled = true;
			}

			public void OnKill()
			{
				if (!isKilled) return;
				var crates = Facepunch.Pool.GetList<LootContainer>();
				Vis.Entities(Heli.transform.position, HelicopterEntitySpawnRadius, crates);

				if (!Plugin.LootProfiles.ContainsKey(HeliSets.Loot.ProfileName)) return;
				PersonalHeli.LootProfile Loot = Plugin.LootProfiles[HeliSets.Loot.ProfileName];
				if (Loot == null) return;
				foreach (var crate in crates)
				{
					var component = crate.gameObject.AddComponent<PersonalCrateComponent>();
					component.Init(Plugin, Heli, Player, this.HeliSets);
					component.FillLoot(Loot);
					if (HeliSets.Loot.MemorizeTeamOnCall)
					{
						component.SavedTeam = SavedTeam;
					}
					if (HeliSets.Loot.RemoveFireFromCrates)
					{
						if (crate is LockedByEntCrate)
						{
							(crate as LockedByEntCrate).lockingEnt?.ToBaseEntity()?.Kill();
						}
					}
				}
				Facepunch.Pool.FreeList(ref crates);
				var gibs = Facepunch.Pool.GetList<HelicopterDebris>();
				Vis.Entities(Heli.transform.position, HelicopterEntitySpawnRadius, gibs);
				foreach (var gib in gibs)
				{
					var component = gib.gameObject.AddComponent<PersonalGibComponent>();
					component.Init(Plugin, Heli, Player, this.HeliSets);
					if (HeliSets.Loot.MemorizeTeamOnCall)
					{
						component.SavedTeam = SavedTeam;
					}
				}
				Facepunch.Pool.FreeList(ref gibs);

			}
			public void LeaveMeAlone(){				
				CancelInvoke(new Action(UpdateTargets));
				CancelInvoke(new Action(OnKill));
				foreach(BasePlayer player in GetAllPlayersInTeam()){
					if (player.IsConnected)
						player.ChatMessage(Plugin.GetMsg("PlayerCanceled", player.UserIDString,Player.displayName, HeliSets.DifficultName));
				}
				Player = null;
				HeliAi.Retire();
			}

			public void OnPlayerDied(BasePlayer player)
			{
				if (!Config.MainBlock.RetireOnAllTeamDead)
				{
					return;
				}
				if (CanInterractWith(player))
				{
					bool allTeamDied = true;
					List<BasePlayer> team = HeliSets.Loot.MemorizeTeamOnCall ? SavedTeam : GetAllPlayersInTeam();
					foreach (var member in team)
					{
						if (!member.IsDead())
						{
							allTeamDied = false;
							break;
						}
					}
					if (allTeamDied)
					{
						CancelInvoke(new Action(UpdateTargets));
						HeliAi.Retire();
					}
				}
			}
		}
	}
}
