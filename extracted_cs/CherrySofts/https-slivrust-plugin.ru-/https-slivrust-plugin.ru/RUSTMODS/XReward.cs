using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
     /* Плагин был разыгран дискордом https://discord.gg/8CWJadmA5w */ [Info("XReward", "https://discord.gg/8CWJadmA5w", "1.0.5")]
    class XReward : RustPlugin
    {
		
		[ConsoleCommand("reward_give")]
		private void ccmdItemGive(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();
			
			if (Cooldowns.ContainsKey(player))
				if (Cooldowns[player].Subtract(DateTime.Now).TotalSeconds >= 0) return;
			
			ItemGive(player, int.Parse(args.Args[0]), int.Parse(args.Args[1]), int.Parse(args.Args[2]), int.Parse(args.Args[3]));
			
			Cooldowns[player] = DateTime.Now.AddSeconds(0.75f);
		}
				
		[PluginReference] private Plugin ImageLibrary, RustStore;
		
		private void SaveData(BasePlayer player) => Interface.Oxide.DataFileSystem.WriteObject($"XReward/{player.userID}", StoredData[player.userID]);
		
		private void OnPlayerConnected(BasePlayer player)
		{
			if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }
			
			LoadData(player);
		}

        private class RewardConfig 
        {		
			[JsonProperty("Настройка GS")]
            public StoreSetting Store = new StoreSetting();
			
			internal class RewardSetting
            {
				[JsonProperty("Шортнейм предмета")] public string Shortname;
                [JsonProperty("Скин предмета")] public ulong SkinID;  				
                [JsonProperty("Имя предмета")] public string Name;  				
                [JsonProperty("Команда - %STEAMID%")] public string Command;  				
                [JsonProperty("Кол-во предмета")] public int Amount;  				
                [JsonProperty("Ссылка на кастомную картинку")] public string URLImage;  
                [JsonProperty("Описание награды")] public string Description;  
				
				public RewardSetting(string shortname, ulong skinid, string name, string command, int amount, string urlimage, string description)
                {
                    Shortname = shortname; SkinID = skinid; Name = name; Command = command; Amount = amount; URLImage = urlimage; Description = description;
                }
			}
			
			internal class StoreSetting
            {
				[JsonProperty("Секретный ключ GameStores")]
                public string SecretKey;				
				[JsonProperty("ID магазина GameStores")]
                public string IDStore;							
			}
			internal class GeneralSetting
			{
				[JsonProperty("Кол-во столбцов")] public int RewardColumn;
				[JsonProperty("Кол-во отображаемых категорий")] public int RewardCount;
				[JsonProperty("Кол-во категорий в столбце")] public int RewardColumnCount;
			}
			
			[JsonProperty("Общие настройки")]
			public GeneralSetting Setting = new GeneralSetting();

           	internal class CategorySetting
			{
				[JsonProperty("Сколько наград можно забрать из категории")] public int CountReward;  	
				
				[JsonProperty("Настройка списка ресурсов")]
			    public List<RewardSetting> Reward = new List<RewardSetting>();
			}			
			[JsonProperty("Настройка категорий")]
            public Dictionary<int, CategorySetting> Category;						
			
			public static RewardConfig GetNewConfiguration()
            {
                return new RewardConfig
                {
                    Setting = new GeneralSetting
					{
						RewardCount = 18,
						RewardColumn = 3,
						RewardColumnCount = 6
					},
					Store = new StoreSetting()
					{
						SecretKey = "",
						IDStore = ""
					}, 
					Category = new Dictionary<int, CategorySetting>
					{
						[3600] = new CategorySetting
						{
							CountReward = 1,
							Reward = new List<RewardSetting>
							{
								new RewardSetting("wall.frame.garagedoor", 0, "", "", 1, "", "ГАРАЖКА"),
							    new RewardSetting("furnace.large", 0, "", "", 1, "", "БОЛЬШАЯ ПЕЧКА"),
							    new RewardSetting("lantern", 0, "", "", 1, "", "СВЕТИЛЬНИК")	
							}
						},					
						[4000] = new CategorySetting
						{
							CountReward = 1,
							Reward = new List<RewardSetting>
							{
						    	new RewardSetting("electric.timer", 0, "", "", 1, "", "ТАЙМЕР"),
							    new RewardSetting("stocking.large", 0, "", "", 1, "", "БОЛЬШОЙ НОСОК"),
							    new RewardSetting("knife.combat", 0, "", "", 1, "", "БОЕВОЙ НОЖ")	
							}
						},					
						[4400] = new CategorySetting
						{
							CountReward = 1,
							Reward = new List<RewardSetting>
							{
							    new RewardSetting("weapon.mod.silencer", 0, "", "", 1, "", "ГЛУШИТЕЛЬ"),
							    new RewardSetting("pitchfork", 0, "", "", 1, "", "ВИЛЫ"),
							    new RewardSetting("ammo.nailgun.nails", 0, "", "", 32, "", "ГВОЗДИ")	
							}
						},						
						[4800] = new CategorySetting
						{
							CountReward = 1,
							Reward = new List<RewardSetting>
							{
							    new RewardSetting("wood", 0, "", "", 1000, "", "ДЕРЕВО"),
							    new RewardSetting("trap.landmine", 0, "", "", 1, "", "МИНА"),
							    new RewardSetting("generator.wind.scrap", 0, "", "", 1, "", "ВЕТРОГЕНЕРАТОР")
							}
						}
					}
				};
			}
        }
		private void Unload() => BasePlayer.activePlayerList.ToList().ForEach(SaveData);
		
		[ConsoleCommand("reward_give_s")]
		private void ccmdGiveMoney(ConsoleSystem.Arg args)
		{
			if (args.Player() != null) return;
			
			BasePlayer player = BasePlayer.FindByID(ulong.Parse(args.Args[0]));
			 
			switch(args.Args[1])
			{ 
				case "gs":
				{
					Balance(player.userID, double.Parse(args.Args[2]));
					break;
				} 
				case "ovh":
				{
					if (RustStore) RustStore?.CallHook("APIChangeUserBalance", player.userID, double.Parse(args.Args[2]), new Action<string>((result) =>
                    {    
                        if (result == "SUCCESS") return;
                    }));
					break;
				} 
			}
		}
		
		protected override void LoadConfig()
        {
            base.LoadConfig();
			
			try
			{
				config = Config.ReadObject<RewardConfig>();
			}
			catch
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		
		private void OnPlayerDisconnected(BasePlayer player)
		{
			if (StoredData.ContainsKey(player.userID))
			{
				StoredData[player.userID].TimePlay += Convert.ToInt32(player.Connection.GetSecondsConnected());
				SaveData(player);
			}
		}
		
				
		
        void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TimePlay"] = "NEED TO PLAY: {0}",				
                ["Title"] = "COOL SERVER REWARDS",				
                ["Info"] = "GET REWARDS WHILE PLAYING. YOU CAN COLLECT ONLY ONE AWARD FROM A CATEGORY!\n<size=10>YOU'RE PLAYING: {0}</size>"			
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TimePlay"] = "НУЖНО ИГРАТЬ: {0}",				
                ["Title"] = "НАГРАДЫ КРУТОГО СЕРВЕРА",				
                ["Info"] = "ПОЛУЧАЙТЕ НАГРАДЫ ЗА ВРЕМЯ ИГРЫ. ВЫ МОЖЕТЕ ЗАБРАТЬ ТОЛЬКО ОДНУ НАГРАДУ ИЗ КАТЕГОРИИ!\n<size=10>ВЫ ИГРАЕТЕ: {0}</size>"			
            }, this, "ru");            
        }
		   		 		  						  	   		  	 	 		  						  	   		  	   
		private void GUI(BasePlayer player, int Page = 0)
		{
			CuiHelper.DestroyUi(player, ".RewardD");
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "0 -17.5", OffsetMax = "0 -17.5" },
                Image = { Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".OverlayGUI", ".RewardD");
			
			int rewardcount = config.Setting.RewardCount;
			int countr = config.Category.Count - (Page * rewardcount) > rewardcount ? rewardcount : config.Category.Count - (Page * rewardcount);
			int county = Count(countr) - 1;
			int countx = county * 235;
			int rewardcolumncount = config.Setting.RewardColumnCount;
			int count = countr > rewardcolumncount ? rewardcolumncount : countr;
			int x = 0, y = 0, z = 0, h = 0;
			
			foreach(var reward in config.Category.Skip(Page * rewardcount))
			{
				int numberreward = h + (Page * config.Setting.RewardCount);
				double offset = (37.5 * count--) + (2.5 * count--);
				bool block = StoredData[player.userID].TimePlay + Convert.ToInt32(player.Connection.GetSecondsConnected()) < reward.Key;
				bool result = StoredData[player.userID].Reward[numberreward].Where(i => i.Equals(true)).Count() >= reward.Value.CountReward;
				
				container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{(-235 - countx - (county * 2.5)) + (x * 475)} {0 + offset - 75}", OffsetMax = $"{(235 - countx - (county * 2.5)) + (x * 475)} {0 + offset}" },
                    Image = { Color = "0.21 0.22 0.20 0.95" }
                }, ".RewardD", ".Reward");
				
				int countrl = reward.Value.Reward.Count - 1;
				int countxl = countrl * 75;
				
				foreach(var rewardl in reward.Value.Reward)
				{
					string image = rewardl.URLImage != String.Empty ? rewardl.Shortname + 41 : rewardl.SkinID != 0 ? rewardl.Shortname + 42 : rewardl.Shortname + 40;
					
					container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{(-75 - countxl - (countrl  * 2.5)) + (y * 155)} -32.5", OffsetMax = $"{(75 - countxl - (countrl * 2.5)) + (y * 155)} 5" },
                        Image = { Color = "0.31 0.32 0.30 0.95" }
                    }, ".Reward", ".RewardImage");										
					
					container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "2.5 2.5", OffsetMax = "35 35" },
                        Image = { Color = "0.41 0.42 0.40 0.95" }
                    }, ".RewardImage", ".Image");
					
					container.Add(new CuiElement
                    { 
                        Parent = ".Image",
                        Components =
                        {
					        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", image) },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2.5 2.5", OffsetMax = "-2.5 -2.5" }
                        }
                    });
					 
					container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "35 0", OffsetMax = "0 0" },
                        Text = { Text = rewardl.Description, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 10, Color = "0.75 0.75 0.75 0.5" }
                    }, ".RewardImage");
					
				    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Button = { Color = "0 0 0 0", Command = $"reward_give {numberreward} {y} {reward.Key} {Page}" },
                        Text = { Text = "" }
                    }, ".RewardImage");
					
					if (!result && StoredData[player.userID].Reward[numberreward][y])
					    container.Add(new CuiPanel
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                            Image = { Color = "0.21 0.25 0.20 0.9" }
                        }, ".RewardImage", ".BLOCKReward");
					
					y++;
				}

				if (block || result)
				    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Image = { Color = block ? "0.24 0.22 0.20 0.9" : "0.21 0.25 0.20 0.9" }
                    }, ".Reward", ".BLOCK");
					
				container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-120 10", OffsetMax = "120 30" },
                    Text = { Text = string.Format(lang.GetMessage("TimePlay", this, player.UserIDString), TimeSpan.FromSeconds(reward.Key)), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 15, Color = "1 1 1 0.75" }
                }, ".Reward");
				
				z++;
				h++;
				
				if (z == rewardcolumncount)
				{
					count = countr - rewardcolumncount > rewardcolumncount ? rewardcolumncount : countr - rewardcolumncount;
				    countr -= rewardcolumncount;
					
					z = 0;
					x++;
					
					if (x == config.Setting.RewardColumn)
						break;
				}
				
				y = 0;   
			}

         	bool back = Page != 0;
			bool next = config.Category.Count > ((Page + 1) * config.Setting.RewardCount);

			container.Add(new CuiButton
            {    
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 206.25", OffsetMax = "0 236.25" },
                Button = { Color = "0 0 0 0", Command = back ? $"page.xreward back {Page}" : "" },
                Text = { Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 26, Font = "robotocondensed-regular.ttf", Color = back ? "1 1 1 0.75" : "1 1 1 0.1" }
            }, ".RewardD");	
 
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-15 206.25", OffsetMax = "15 236.25" },
                Text = { Text = $"{Page + 1}", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 18, Color = "1 1 1 0.75" }
            }, ".RewardD");					
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "0 206.25", OffsetMax = "50 236.25" },
                Button = { Color = "0 0 0 0", Command = next ? $"page.xreward next {Page}" : "" },
                Text = { Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 26, Font = "robotocondensed-regular.ttf", Color = next ? "1 1 1 0.75" : "1 1 1 0.1" }
            }, ".RewardD");
			
			CuiHelper.AddUi(player, container);
		}
		
				
		
        void Balance(ulong userId, double amount) 
		{
			ApiRequestBalance(new Dictionary<string, string>() 
			{
				{"action", "moneys"},
				{"type", "plus"},
				{"steam_id", userId.ToString()},
				{"amount", amount.ToString()},
                {"mess", "Бонус за онлайн."}
			});
		}
		   		 		  						  	   		  	 	 		  						  	   		  	   
		private void ItemGive(BasePlayer player, int number, int number1, int timeplay, int Page)
		{
			var item = config.Category[timeplay];
			var item1 = item.Reward[number1];
			
			if (StoredData[player.userID].Reward[number][number1] || StoredData[player.userID].Reward[number].Where(i => i.Equals(true)).Count() >= item.CountReward || StoredData[player.userID].TimePlay + Convert.ToInt32(player.Connection.GetSecondsConnected()) < timeplay) return;
			
			Effect x = new Effect("assets/bundled/prefabs/fx/weapons/survey_charge/survey_charge_stick.prefab", player, 0, new Vector3(), new Vector3());
			
			if (item1.Command != String.Empty)
				Server.Command($"{item1.Command}".Replace("%STEAMID%", $"{player.userID}").Replace("%AMOUNT%", $"{item1.Amount}"));
			else
			{
				Item itemT = ItemManager.CreateByName(item1.Shortname, item1.Amount, item1.SkinID);
				itemT.name = item1.Name;
				
				player.GiveItem(itemT);
			}
			
			StoredData[player.userID].Reward[number][number1] = true;
			GUI(player, Page);
			SaveData(player);
			
			PrintWarning($"[ {player.displayName} | {player.userID} ] - ЗАБРАЛ НАГРАДУ ЗА ОНЛАЙН [{++number}] - [ {item1.Description} ]");
			EffectNetwork.Send(x, player.Connection);
		}
		
				
				
		private void OnServerInitialized()
		{
			PrintWarning("\n-----------------------------\n" +
			"     Author - Monster\n" +
			"     VK - vk.com/idannopol\n" +
			"     Discord - Monster#4837\n" +
			"     \n" +
			"-----------------------------");
			
			BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);
			timer.Every(180, () => { BasePlayer.activePlayerList.ToList().ForEach(SaveData); });
			
			foreach (var image in config.Category)
			{
				image.Value.Reward.ForEach(i =>
				{
					if (!ImageLibrary.Call<bool>("HasImage", i.Shortname + 40) && !ImageLibrary.Call<bool>("HasImage", i.Shortname + 41) && !ImageLibrary.Call<bool>("HasImage", i.Shortname + 42))
					{
						if (i.URLImage != String.Empty)
							ImageLibrary.Call("AddImage", i.URLImage, i.Shortname + 41);
						else if (i.SkinID != 0)
							ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getskin/{i.SkinID}/{40}", i.Shortname + 42);
						else
							ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getimage/{i.Shortname}/{40}", i.Shortname + 40);
					}
				});
			}
			
			InitializeLang();
		}
		
		[ConsoleCommand("page.xreward")]
		private void ccmdPage(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();
			
			int Page = int.Parse(args.Args[1]);
			
			switch (args.Args[0])
			{
				case "next":
				{
					GUI(player, Page + 1);	
					break;
				}						
				case "back":
				{
					GUI(player, Page - 1);
					break;
				}
			}
		}

		void ApiRequestBalance(Dictionary<string, string> args) 
		{
			string url = $"https://gamestores.ru/api?shop_id={config.Store.IDStore}&secret={config.Store.SecretKey}{string.Join("", args.Select(arg => $"&{arg.Key}={arg.Value}").ToArray())}";
			
			webrequest.EnqueueGet(url, (i, s) => 
			{ 
			    if (i == 201) 
				{
				    PrintWarning("Плагин не работает!");
				    Interface.Oxide.UnloadPlugin(Title);
				}
			}, this);
		}
		
				
				
        private RewardConfig config;
		
				
		[ChatCommand("reward")]
		void cmdOpenGUI(BasePlayer player)
		{
			if (Cooldowns.ContainsKey(player))
				if (Cooldowns[player].Subtract(DateTime.Now).TotalSeconds >= 0) return;
			
			BackgroundGUI(player);	

            Cooldowns[player] = DateTime.Now.AddSeconds(0.75f);			
		}
        protected override void SaveConfig() => Config.WriteObject(config);
		
				
		
		private void BackgroundGUI(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".OverlayB");
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
				CursorEnabled = true,
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-500 -260", OffsetMax = "500 290" },
                Image = { Color = "0.51 0.52 0.50 0.95", Material = "assets/icons/greyout.mat" }
            }, "Overlay", ".OverlayB");
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                Image = { Color = "0.21 0.22 0.20 0.95" }
            }, ".OverlayB", ".OverlayGUI");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "462.5 237.5", OffsetMax = "490 265" },
                Button = { Color = "1 1 1 0.75", Sprite = "assets/icons/close.png", Close = ".OverlayB" },
                Text = { Text = "" }
            }, ".OverlayB");
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-490 237.5", OffsetMax = "447.5 265" },
                Text = { Text = lang.GetMessage("Title", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 18, Color = "1 1 1 0.75" }
            }, ".OverlayGUI");
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-500 227.5", OffsetMax = "500 232.5" },
                Image = { Color = "0.51 0.52 0.50 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".OverlayGUI");			
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "452.5 227.5", OffsetMax = "457.5 275" },
                Image = { Color = "0.51 0.52 0.50 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".OverlayGUI");
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-325 -255", OffsetMax = "325 -220" },
                Text = { Text = string.Format(lang.GetMessage("Info", this, player.UserIDString), TimeSpan.FromSeconds(StoredData[player.userID].TimePlay + Convert.ToInt32(player.Connection.GetSecondsConnected()))), Align = TextAnchor.LowerCenter, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "0.75 0.75 0.75 0.4" }
            }, ".OverlayGUI");
			
			CuiHelper.AddUi(player, container);
			
			GUI(player, 0);
		}
		   		 		  						  	   		  	 	 		  						  	   		  	   
        		
				
		private class RewardData
        {			
			[JsonProperty("Наиграно времени (в сек.)")]
            public int TimePlay;
			[JsonProperty("Полученные наград")]
			public List<List<bool>> Reward = new List<List<bool>>();
		}
		
		private Dictionary<ulong, RewardData> StoredData = new Dictionary<ulong, RewardData>();

		private int Count(int count)
		{
			int rewardcount = config.Setting.RewardCount;
			if (count > rewardcount) count = rewardcount;
			int x = 0;

			while(count > 0)
			{
				count -= config.Setting.RewardColumnCount;
				x++;
			}
			
			return x;
		}
		   		 		  						  	   		  	 	 		  						  	   		  	   
		private void LoadData(BasePlayer player)
        {
            var Reward = Interface.Oxide.DataFileSystem.ReadObject<RewardData>($"XReward/{player.userID}");
		   		 		  						  	   		  	 	 		  						  	   		  	   
            if (!StoredData.ContainsKey(player.userID))
                StoredData.Add(player.userID, new RewardData());

            StoredData[player.userID] = Reward ?? new RewardData();
			
			if (StoredData[player.userID].Reward.Count == 0)
			    foreach(var i in config.Category)
					StoredData[player.userID].Reward.Add(new List<bool> { false, false, false });
        }
		protected override void LoadDefaultConfig() => config = RewardConfig.GetNewConfiguration();
		
				
		private Dictionary<BasePlayer, DateTime> Cooldowns = new Dictionary<BasePlayer, DateTime>();

        	}
}
