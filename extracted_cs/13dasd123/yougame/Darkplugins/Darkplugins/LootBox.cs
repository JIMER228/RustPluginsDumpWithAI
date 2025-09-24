using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("LootBox", "https://discord.gg/dNGbxafuJn", "1.1.1")]
    public class LootBox : RustPlugin
    {	
		object OnItemAction(Item item, string action, BasePlayer player)
		{
			if (_conf.Settings.caseshortname != item.info.shortname || action != "unwrap" || item.skin == 0U) return null;
			item.UseItem(1);
			foreach (var cs in _conf.Cases.Where(x => x.caseskin == item.skin))
			{
				var ritem = cs.itemlist.GetRandom();
				if (ritem.command == true)
				{
					var cmd = ritem.shortname;
					var days = UnityEngine.Random.Range(ritem.min, ritem.max+1).ToString();
					var privname = cmd.Split(' ')[2].ToUpper();
					cmd = cmd.Replace("{steamid}", player.userID.ToString());
					cmd = cmd.Replace("{random}", days);
					rust.RunServerCommand(cmd);
					PrintToChat(player, GetMsg("OpenedPrivilege", player).Replace("{cs.casename}", cs.casename).Replace("{privname}", privname).Replace("{days}", days));
				}
				else
				{
					Item present = ItemManager.CreateByName(ritem.shortname, UnityEngine.Random.Range(ritem.min, ritem.max));
					if (present == null)
					{
						PrintError($"Shortname error: {ritem.shortname}, player: {player.displayName}");
						return false;
					}
					player.GiveItem(present);
					PrintToChat(player, GetMsg("Opened", player).Replace("{cs.casename}", cs.casename).Replace("{present.info.displayName.english}", present.info.displayName.english).Replace("{present.amount}", present.amount.ToString()));
				}
			}
			return false;
		}
		
		List <LootContainer> chest = new List <LootContainer> ();
		
		void OnLootEntity(BasePlayer player, BaseEntity entity, Item item)
		{
			if (!(entity is LootContainer)) return;
			var container = (LootContainer)entity;
			if (chest.Contains(container)) return;
			chest.Add(container);
		    List <string> ItemsList = new List <string> ();
		    if (containers.Contains(container.ShortPrefabName))
			{
				if (UnityEngine.Random.Range(0f, 100f) <= _conf.Settings.chance) 
				{
					var itemContainer = container.inventory;
					foreach(var i1 in itemContainer.itemList)
					{
						ItemsList.Add(i1.info.shortname);
					}
					if (!ItemsList.Contains(_conf.Settings.caseshortname))
					{
						if (container.inventory.itemList.Count == container.inventory.capacity) container.inventory.capacity++;
						
						item = ItemManager.CreateByName(_conf.Settings.caseshortname, 1);
						
						if (item == null)
						{
							PrintError($"Bad SHORTNAME: {_conf.Settings.caseshortname}");
							return;
						}
						
						var rcase = _conf.Cases.GetRandom();
						
						var name = rcase.casename;
						var skin = rcase.caseskin;
						
						item.skin = skin;
						item.name = name;
								
						item.MoveToContainer(itemContainer);
						PrintToChat(player, GetMsg("Found", player).Replace("{item.name}", item.name));
					}
				}
		   }
		}
		
		List <string> containers = new List <string> ()
		{
			{"crate_basic"},
			{"crate_elite"},
			{"crate_normal"},
			{"crate_normal_2"},
			{"crate_tools"},
			{"crate_underwater_basic"},
			{"crate_underwater_advanced"},
			{"supply_drop"},
			{"codelockedhackablecrate"},
			{"bradley_crate"}
		};
		
		BasePlayer player2;
		
		[ChatCommand("gc")]
		private void givecase(BasePlayer player, string command, string[] args)
		{
			if (!player.IsAdmin) return;
			if (args.Length < 3)
			{
				PrintToChat(player, GetMsg("Syntax", player));
				return;
			}
			
			var nameorid = args[0].ToLower();
			
			foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
			{
				if (activePlayer.displayName.ToLower().Contains(nameorid) || activePlayer.UserIDString == nameorid) player2 = activePlayer;
			}
			
			if (player2 == null)
			{
				PrintToChat(player, GetMsg("PlayerNotFound", player));
				return;
			}
			
			var type = args[1];
			
			int count;
			
			bool ifcount = Int32.TryParse(args[2], out count);
			
			if (!ifcount)
			{
				PrintToChat(player, GetMsg("Syntax", player));
				return;
			}
			
			Item item = ItemManager.CreateByName(_conf.Settings.caseshortname, count);
		
			if (item == null)
			{
				PrintError("Config error! Bad SHORTNAME");
				PrintToChat(player, "Config error! Bad SHORTNAME");
				return;
			}
			
			foreach (var cs in _conf.Cases)
			{
				if (cs.casename.ToLower().Contains(type.ToLower()))
				{
					item.skin = cs.caseskin;
					item.name = cs.casename;
					break;
				}
			}
			
			if (item.skin == 0U)
			{
				PrintToChat(player, GetMsg("CaseNotFound", player));
				return;
			}
			
			PrintToChat(player, GetMsg("Gived", player).Replace("{item.name}", item.name).Replace("{count}", count.ToString()).Replace("{player2.displayName}", player2.displayName));
			
			PrintToChat(player2, GetMsg("Received", player).Replace("{item.name}", item.name).Replace("{count}", count.ToString()));
			
			player2.GiveItem(item);
		}
		
		/*[ChatCommand("ss")]
		private void cmdsetskin(BasePlayer player, string command, string[] args)
		{
			if (args[0] == null) return;
			var shit = Convert.ToUInt64(args[0]);
			Item item = player.GetActiveItem();
			PrintToChat(player, $"Вы установили на предмет скин ID {args[0]}");
			item.skin = shit;
		}*/

        private void Init()
        {
			LoadConfig();
			
			SaveConfig();
			
			lang.RegisterMessages(new Dictionary<string, string>
            {
				["Gived"] = "You give <color=#ffd700>{item.name} x {count}</color> to <color=#ffd700>{player2.displayName}</color>",
				["Opened"] = "You opened <color=#ffd700>{cs.casename}</color> and received <color=#ffd700>{present.info.displayName.english}</color> x <color=#ffd700>{present.amount}</color>",
				["OpenedPrivilege"] = "You opened <color=#ffd700>{cs.casename}</color> and received <color=#ffd700>{privname}</color> for <color=#ffd700>{days} days</color>",
				["Received"] = "Admin gives you <color=#ffd700>{item.name} x {count}</color>",
				["Syntax"] = "Syntax: <color=#ffd700>/gc <player> <casename> <count></color>",
				["PlayerNotFound"] = "Player not found!",
				["CaseNotFound"] = "Case not found!",
                ["Found"] = "You found <color=#ffd700>{item.name}</color>",
            }, this);
			
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Gived"] = "Вы выдали <color=#ffd700>{item.name} x {count}</color> игроку <color=#ffd700>{player2.displayName}</color>",
				["Opened"] = "Вы открыли <color=#ffd700>{cs.casename}</color> и получили <color=#ffd700>{present.info.displayName.english}</color> x <color=#ffd700>{present.amount}</color>",
				["OpenedPrivilege"] = "Вы открыли <color=#ffd700>{cs.casename}</color> и получили <color=#ffd700>{privname}</color> на <color=#ffd700>{days} дней</color>",
				["Received"] = "Администратор выдал Вам <color=#ffd700>{item.name} x {count}</color>",
				["Syntax"] = "Используйте: <color=#ffd700>/gc <player> <casename> <count></color>",
				["PlayerNotFound"] = "Игрок не найден!",
				["CaseNotFound"] = "Кейс не найден!",
                ["Found"] = "Вы нашли <color=#ffd700>{item.name}</color>",
            }, this, "ru");
        }
		
		string GetMsg(string key, BasePlayer player = null) => lang.GetMessage(key, this, player == null ? null : player.UserIDString);
		
		private _Conf _conf; 
		
        class _Conf
        {          
            [JsonProperty(PropertyName = "Настройки")]
            public Options Settings { get; set; }
			
			[JsonProperty(PropertyName = "Настройки кейсов")]
            public List<CaseConfig> Cases { get; set; }

			public class CaseConfig
			{
				[JsonProperty(PropertyName = "Название")]
				public string casename { get; set; }
				[JsonProperty(PropertyName = "Скин")]
				public ulong caseskin { get; set; }
				[JsonProperty(PropertyName = "Настройки лута")]
				public List<ItemConfig> itemlist { get; set; }
			}
			
			public class ItemConfig
			{
				[JsonProperty(PropertyName = "Предмет (false) или консольная команда (true)")]
				public bool command { get; set; }
				[JsonProperty(PropertyName = "Shortname предмета или выполняемая команда")]
				public string shortname { get; set; }
				[JsonProperty(PropertyName = "Минимальное количество")]
				public int min { get; set; }
				[JsonProperty(PropertyName = "Максимальное количество")]
				public int max { get; set; }
			}
			
            public class Options
            {
                [JsonProperty(PropertyName = "Шанс выпадения кейса")]
                public int chance { get; set; }
                [JsonProperty(PropertyName = "Shortname предмета-кейса (любой из подарков)")]
                public string caseshortname { get; set; }
            }
        }
		
        protected override void LoadConfig()
        {
            base.LoadConfig();
            _conf = Config.ReadObject<_Conf>();

            Config.WriteObject(_conf, true);
        }

        protected override void LoadDefaultConfig()
		{
			_conf = SetDefaultConfig();
			PrintWarning("Creating config-file...");
		}

        private _Conf SetDefaultConfig()
        {
            return new _Conf
            {
				Cases = new List<_Conf.CaseConfig>()
				{
					new _Conf.CaseConfig
					{
						casename = "Кейс с оружием",
						caseskin = 1438296711,
						itemlist = new List<_Conf.ItemConfig>()
						{
							new _Conf.ItemConfig { command = false, shortname = "rifle.ak", max = 1, min = 1 },
							new _Conf.ItemConfig { command = false, shortname = "rifle.lr300", max = 1, min = 1 },
							new _Conf.ItemConfig { command = false, shortname = "rifle.bolt", max = 1, min = 1 },
							new _Conf.ItemConfig { command = false, shortname = "rifle.l96", max = 1, min = 1 }
						},
					},
					new _Conf.CaseConfig
					{
						casename = "Кейс с ресурсами",
						caseskin = 1608121658,
						itemlist = new List<_Conf.ItemConfig>()
						{
							new _Conf.ItemConfig { command = false, shortname = "sulfur", max = 2500, min = 1250 },
							new _Conf.ItemConfig { command = false, shortname = "stones", max = 5000, min = 2500 },
							new _Conf.ItemConfig { command = false, shortname = "wood", max = 5000, min = 2500 },
							new _Conf.ItemConfig { command = false, shortname = "metal.fragments", max = 2500, min = 1250 }
						},
					},
					new _Conf.CaseConfig
					{
						casename = "Кейс с компонентами",
						caseskin = 1528783940,
						itemlist = new List<_Conf.ItemConfig>()
						{
							new _Conf.ItemConfig { command = false, shortname = "gears", max = 15, min = 5 },
							new _Conf.ItemConfig { command = false, shortname = "metalspring", max = 15, min = 5 },
							new _Conf.ItemConfig { command = false, shortname = "metalpipe", max = 15, min = 5 },
							new _Conf.ItemConfig { command = false, shortname = "roadsigns", max = 15, min = 5 }
						},
					},
					new _Conf.CaseConfig
					{
						casename = "Кейс с привилегиями",
						caseskin = 1438297472,
						itemlist = new List<_Conf.ItemConfig>()
						{
							new _Conf.ItemConfig { command = true, shortname = "say {steamid} - 1 - {random}", max = 7, min = 1 },
							new _Conf.ItemConfig { command = true, shortname = "say {steamid} - 2 - {random}", max = 7, min = 1 },
							new _Conf.ItemConfig { command = true, shortname = "say {steamid} - 3 - {random}", max = 7, min = 1 },
							new _Conf.ItemConfig { command = true, shortname = "say {steamid} - 4 - {random}", max = 7, min = 1 }
						},
					},
				},
				
                Settings = new _Conf.Options
                {
                    chance = 15,
                    caseshortname = "xmas.present.large"
                }              
            };
        }

        protected override void SaveConfig() => Config.WriteObject(_conf, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Updating config-file...");

            _Conf baseConfig = SetDefaultConfig();
        }
    }
}