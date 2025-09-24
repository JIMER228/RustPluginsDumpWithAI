using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;
 
namespace Oxide.Plugins 
{ 
    [Info("XSkinMenu", "Monster.", "1.0.2")]
    class XSkinMenu : RustPlugin  
    {
		#region Reference
		
		[PluginReference] private Plugin ImageLibrary;
		
		#endregion
		
		#region Config 
		
		private SkinConfig config;

        private class SkinConfig  
        {		
			internal class GeneralSetting
			{
				[JsonProperty("Сгенерировать список скинов если он пустой в файле XSkinMenu/Skins")] public bool GenerateSkins;
				[JsonProperty("Проверять и добавлять новые скины принятые разработчиками или сделаные для твич дропсов")] public bool UpdateSkins;
			}			
			
			[JsonProperty("Общие настройки")]
			public GeneralSetting Setting = new GeneralSetting();
			[JsonProperty("Настройка категорий")]
            public Dictionary<string, List<string>> Category = new Dictionary<string, List<string>>();								
			
			public static SkinConfig GetNewConfiguration()
            {
                return new SkinConfig
                {
					Setting = new GeneralSetting
					{
						GenerateSkins = true,
						UpdateSkins = true
					},
					Category = new Dictionary<string, List<string>>
					{
						["weapon"] = new List<string> { "pistol.revolver", "pistol.semiauto", "pistol.python", "pistol.eoka", "shotgun.waterpipe", "shotgun.double", "shotgun.pump", "bow.hunting", "crossbow", "grenade.f1", "smg.2", "smg.thompson", "smg.mp5", "rifle.ak", "rifle.lr300", "lmg.m249", "rocket.launcher", "rifle.semiauto", "rifle.m39", "rifle.bolt", "rifle.l96", "longsword", "salvaged.sword", "knife.combat", "bone.club", "knife.bone" },
						["construction"] = new List<string> { "wall.frame.garagedoor", "door.double.hinged.toptier", "door.double.hinged.metal", "door.double.hinged.wood", "door.hinged.toptier", "door.hinged.metal", "door.hinged.wood", "barricade.concrete", "barricade.sandbags" },
						["item"] = new List<string> { "locker", "vending.machine", "fridge", "furnace", "table", "chair", "box.wooden.large", "box.wooden", "rug.bear", "rug", "sleepingbag" },
						["attire"] = new List<string> { "metal.facemask", "coffeecan.helmet", "riot.helmet", "bucket.helmet", "mask.balaclava", "burlap.headwrap", "hat.miner", "hat.beenie", "hat.boonie", "hat.cap", "mask.bandana", "metal.plate.torso", "roadsign.jacket", "roadsign.kilt", "roadsign.gloves", "burlap.gloves", "attire.hide.poncho", "jacket.snow", "jacket", "tshirt.long", "hoodie", "shirt.collared", "tshirt", "burlap.shirt", "attire.hide.vest", "shirt.tanktop", "attire.hide.helterneck", "pants", "burlap.trousers", "pants.shorts", "attire.hide.pants", "attire.hide.skirt", "shoes.boots", "burlap.shoes", "attire.hide.boots" },
						["tool"] = new List<string> { "fun.guitar", "jackhammer", "icepick.salvaged", "pickaxe", "stone.pickaxe", "rock", "hatchet", "stonehatchet", "explosive.satchel", "hammer" }
					}
				};
			}
        }
		 
		protected override void LoadConfig()
        {
            base.LoadConfig(); 
			 
			try
			{
				config = Config.ReadObject<SkinConfig>();
			}
			catch  
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		protected override void LoadDefaultConfig() => config = SkinConfig.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);
		
		#endregion		
		
		#region Data
		
	    internal class Data 
		{
			[JsonProperty("Смена скинов в инвентаре")] public bool ChangeSI = true;
			[JsonProperty("Смена скинов на предметах")] public bool ChangeSE = true;
			[JsonProperty("Смена скинов при крафте")] public bool ChangeSC = true;
			[JsonProperty("Смена скинов в инвентаре после удаления")] public bool ChangeSCL = true;
			[JsonProperty("Скины")] public Dictionary<string, ulong> Skins = new Dictionary<string, ulong>();
		}
		
		private Dictionary<ulong, Data> StoredData = new Dictionary<ulong, Data>();
		public Dictionary<string, List<ulong>> StoredDataSkins = new Dictionary<string, List<ulong>>();
		
		private void LoadData(BasePlayer player)
		{ 
            var Data = Interface.Oxide.DataFileSystem.ReadObject<Data>($"XSkinMenu/UserSettings/{player.userID}");
            
            if (!StoredData.ContainsKey(player.userID)) 
                StoredData.Add(player.userID, new Data());	

            StoredData[player.userID] = Data ?? new Data();  
			
			if (StoredData[player.userID].Skins.Count == 0)
			    foreach(var skin in StoredDataSkins) 
					StoredData[player.userID].Skins.Add(skin.Key, 0);
		}  
		
		private void SaveData(BasePlayer player) => Interface.Oxide.DataFileSystem.WriteObject($"XSkinMenu/UserSettings/{player.userID}", StoredData[player.userID]);
		
	    private void Unload()
		{
			foreach (BasePlayer player in BasePlayer.activePlayerList)
			{
				SaveData(player);
				CuiHelper.DestroyUi(player, ".GUIS");
			}
		}
		
		#endregion		
		
		private Dictionary<BasePlayer, DateTime> Cooldowns = new Dictionary<BasePlayer, DateTime>();
		
		#region Commands
		
		[ChatCommand("skin")]
		private void cmdOpenGUI(BasePlayer player)
		{
			if(!permission.UserHasPermission(player.UserIDString, "xskinmenu.use"))
				SendReply(player, lang.GetMessage("NOPERM", this, player.UserIDString));
			else
			    GUI(player);		
		}
		
		[ChatCommand("skinentity")]
		private void cmdSetSkinEntity(BasePlayer player)
		{
			if(!permission.UserHasPermission(player.UserIDString, "xskinmenu.entity"))
			{
				SendReply(player, lang.GetMessage("NOPERM", this, player.UserIDString));
				return;
			}
			
			if(StoredData[player.userID].ChangeSE)
			{
			    RaycastHit rhit;
 
			    if (!Physics.Raycast(player.eyes.HeadRay(), out rhit, 3f, LayerMask.GetMask("Deployed", "Construction"))) return;
			    var entity = rhit.GetEntity();
			
                if (entity != null && entity.OwnerID == player.userID)
			    {
				    if(shortnamesEntity.ContainsKey(entity.ShortPrefabName))
				    {
				        var shortname = shortnamesEntity[entity.ShortPrefabName];
				        if(!StoredData[player.userID].Skins.ContainsKey(shortname)) return;
					
				        SetSkinEntity(player, entity, shortname);
				    }
			    }
			}
		}
		
		[ConsoleCommand("skin_c")]
		private void ccmdCategoryS(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();
			
			if(!permission.UserHasPermission(player.UserIDString, "xskinmenu.use")) 
			{
				SendReply(player, lang.GetMessage("NOPERM", this, player.UserIDString));
				return;
			}
			
			if (Cooldowns.ContainsKey(player))
                if (Cooldowns[player].Subtract(DateTime.Now).TotalSeconds >= 0) return;
			
			Effect x = new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3());
			
			switch(args.Args[0])
			{
				case "category":
				{
					CategoryGUI(player, int.Parse(args.Args[2]));
					ItemGUI(player, args.Args[1]);
					EffectNetwork.Send(x, player.Connection);
					break;
				}				
				case "skin":
				{ 
					SkinGUI(player, args.Args[1]);
					EffectNetwork.Send(x, player.Connection);
					break; 
				}				
				case "setskin":
				{ 
					string item = args.Args[1];
					ulong skin = ulong.Parse(args.Args[2]);
					
					if(!StoredData[player.userID].Skins.ContainsKey(item)) return;
					
					Effect y = new Effect("assets/prefabs/deployable/repair bench/effects/skinchange_spraypaint.prefab", player, 0, new Vector3(), new Vector3());
					StoredData[player.userID].Skins[item] = skin;
					
					if(!permission.UserHasPermission(player.UserIDString, "xskinmenu.inventory"))
						SendReply(player, lang.GetMessage("NOPERM", this, player.UserIDString));
					else
					    if(StoredData[player.userID].ChangeSI) SetSkinItem(player, item, skin);
					
					EffectNetwork.Send(y, player.Connection);
					break;
				}				
				case "clear":
				{
					Effect z = new Effect("assets/bundled/prefabs/fx/weapons/survey_charge/survey_charge_stick.prefab", player, 0, new Vector3(), new Vector3());
					
					string item = args.Args[1];
					StoredData[player.userID].Skins[item] = 0;
					
					CuiHelper.DestroyUi(player, $".I + {args.Args[2]}");
					if(StoredData[player.userID].ChangeSCL) SetSkinItem(player, item, 0);
					
					EffectNetwork.Send(z, player.Connection);
					break;
				}
			}
			
			Cooldowns[player] = DateTime.Now.AddSeconds(0.5f); 
		}
		
		[ConsoleCommand("skin_s")]
		private void ccmdSetting(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();
			
			if(!permission.UserHasPermission(player.UserIDString, "xskinmenu.setting")) 
			{
				SendReply(player, lang.GetMessage("NOPERM", this, player.UserIDString));
				return;
			}
			
			if (Cooldowns.ContainsKey(player))
                if (Cooldowns[player].Subtract(DateTime.Now).TotalSeconds >= 0) return;
			
			Effect x = new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3());
			
			switch(args.Args[0])
			{
				case "open":
				{
					SettingGUI(player);
					break;
				}				
				case "inventory":
				{
					StoredData[player.userID].ChangeSI = !StoredData[player.userID].ChangeSI;
					SettingGUI(player);
					break;
				}				
				case "entity":
				{
					StoredData[player.userID].ChangeSE = !StoredData[player.userID].ChangeSE;
					SettingGUI(player);
					break;
				}				
				case "craft":
				{
					StoredData[player.userID].ChangeSC = !StoredData[player.userID].ChangeSC;
					SettingGUI(player);
					break;
				}				
				case "clear":
				{
					StoredData[player.userID].ChangeSCL = !StoredData[player.userID].ChangeSCL;
					SettingGUI(player);
					break;
				}
			}
			
			EffectNetwork.Send(x, player.Connection);
			Cooldowns[player] = DateTime.Now.AddSeconds(0.5f);
		}
		
		[ConsoleCommand("page.xskinmenu")]
		private void ccmdPage(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();
			Effect x = new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3());
			
			string item = args.Args[2];
			int Page = int.Parse(args.Args[3]);
			
			switch (args.Args[0])
			{
				case "item":
				{
					switch(args.Args[1])
					{
				        case "next":
				        {
				        	ItemGUI(player, item, Page + 1);	
				        	break;
				        }						
				        case "back":
				        {
				        	ItemGUI(player, item, Page - 1);
				        	break;
				        }
					}
					break;
				}				
				case "skin":
				{
					switch(args.Args[1])
					{
				        case "next":
				        {
				        	SkinGUI(player, item, Page + 1);	
				        	break;
				        }						
				        case "back":
				        {
				    	    SkinGUI(player, item, Page - 1);
				    	    break;
				        }
					}
					break;
				}
			}
			
			EffectNetwork.Send(x, player.Connection);
		}
		
		[ConsoleCommand("xskin")]
		private void ccmdAdmin(ConsoleSystem.Arg args)
		{
			if (args.Player() == null || args.Player().IsAdmin)
			{
				string item = args.Args[1];
				
				if(!StoredDataSkins.ContainsKey(item))
				{
					PrintWarning($"Не найдено предмета <{item}> в списке!");
					return;
				}
				
				switch(args.Args[0])
				{
					case "add": 
					{
						ulong skinID = ulong.Parse(args.Args[2]);
							
						if(StoredDataSkins[item].Contains(skinID))
							PrintWarning($"Скин <{skinID}> уже есть в списке скинов предмета <{item}>!");
						else
						{
							StoredDataSkins[item].Add(skinID);
							PrintWarning($"Скин <{skinID}> успешно добавлен в список скинов предмета <{item}>!");
						}
						
						break;
					}					
					case "remove":
					{
						ulong skinID = ulong.Parse(args.Args[2]);
						
						if(StoredDataSkins[item].Contains(skinID))
						{
							StoredDataSkins[item].Remove(skinID);
							PrintWarning($"Скин <{skinID}> успешно удален из списка скинов предмета <{item}>!");
						}
						else
							PrintWarning($"Скин <{skinID}> не найден в списке скинов предмета <{item}>!");
						
						break;
					}					
					case "list": 
					{
						if(StoredDataSkins[item].Count == 0)
						{
							PrintWarning($"Список скинов предмета <{item}> пуст!");
							return;
						}
						
						string skinslist = $"Список скинов предмета <{item}>:\n";
						
						foreach(ulong skinID in StoredDataSkins[item])
						    skinslist += $"\n{skinID}";
						
						PrintWarning(skinslist);
						
						break;
					}					
					case "clearlist":
					{
						if(StoredDataSkins[item].Count == 0)
						{
							PrintWarning($"Список скинов предмета <{item}> уже пуст!");
							return;
						}
						else
						{
							StoredDataSkins[item].Clear();
							PrintWarning($"Список скинов предмета <{item}> успешно очищен!");
						} 
						
						break;  
					}					  
				}
				
				Interface.Oxide.DataFileSystem.WriteObject("XSkinMenu/Skins", StoredDataSkins);
			}
		}
		 
		#endregion		 
		
		private readonly Dictionary<string, string> shortnamesEntity = new Dictionary<string, string>();
		
		#region Hooks 
		
	    private void OnServerInitialized()
		{
			PrintWarning("\n-----------------------------\n" +
			"     Author - Monster\n" +
			"     VK - vk.com/idannopol\n" + 
			"     Discord - Monster#4837\n" +
			"     Config - v.2837\n" + 
			"-----------------------------"); 

			if (Interface.Oxide.DataFileSystem.ExistsDatafile("XSkinMenu/Skins"))
                StoredDataSkins = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, List<ulong>>>("XSkinMenu/Skins");			
			
			foreach (var category in config.Category)
			    foreach (var item in category.Value)
					if (!ImageLibrary.Call<bool>("HasImage", item + 150))
				        ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getimage/{item}/{150}", item + 150);
					
			foreach(var item in StoredDataSkins)
			    foreach(var skin in item.Value)
			        if (!ImageLibrary.Call<bool>("HasImage", $"{skin}" + 152) && !errorskins.ContainsKey(skin))
					    ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getskin/{skin}/{150}", $"{skin}" + 152);
				
			foreach (var item in ItemManager.GetItemDefinitions())
			{
				var prefab = item.GetComponent<ItemModDeployable>()?.entityPrefab?.resourcePath;
				if (string.IsNullOrEmpty(prefab)) continue;
				
				var shortPrefabName = Utility.GetFileNameWithoutExtension(prefab);
				if (!string.IsNullOrEmpty(shortPrefabName) && !shortnamesEntity.ContainsKey(shortPrefabName))
				    shortnamesEntity.Add(shortPrefabName, item.shortname);
			}
			
			if(true && StoredDataSkins.Count == 0) GenerateItems();   
			if(true) GenerateItems();
				
			BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);
			timer.Every(180, () => { BasePlayer.activePlayerList.ToList().ForEach(SaveData); });
			
			InitializeLang();
			permission.RegisterPermission("xskinmenu.use", this);
			permission.RegisterPermission("xskinmenu.setting", this);
			permission.RegisterPermission("xskinmenu.craft", this);
			permission.RegisterPermission("xskinmenu.entity", this);
			permission.RegisterPermission("xskinmenu.inventory", this);   
		}
		
		private void GenerateItems()
		{
			foreach (var pair in Rust.Workshop.Approved.All)
			{
				if (pair.Value == null || pair.Value.Skinnable == null) continue;
				
				ulong skinID = pair.Value.WorkshopdId; 
				
				string item = pair.Value.Skinnable.ItemName;
				if (item.Contains("lr300")) item = "rifle.lr300";
				
				if(!StoredDataSkins.ContainsKey(item))
				    StoredDataSkins.Add(item, new List<ulong>());
				
				if(!StoredDataSkins[item].Contains(skinID))
					StoredDataSkins[item].Add(skinID);
			}
			
			Interface.Oxide.DataFileSystem.WriteObject("XSkinMenu/Skins", StoredDataSkins);
		}
		
		private void OnPlayerConnected(BasePlayer player)
		{
			if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }		
			
			LoadData(player);
		}  
		
	    private void OnPlayerDisconnected(BasePlayer player)
		{
			if (StoredData.ContainsKey(player.userID)) 
			{   
				SaveData(player);
				StoredData.Remove(player.userID);
			}			
			  
			if (Cooldowns.ContainsKey(player)) 
				Cooldowns.Remove(player);  
		}
		
		public Dictionary<ulong, string> errorskins = new Dictionary<ulong, string>
		{
			[10180] = "hazmatsuit.spacesuit",
			[10189] = "door.hinged.industrial.a"
		};
		
		private void OnItemCraftFinished(ItemCraftTask task, Item item)
		{
			if(task.skinID == 0)
			{
				BasePlayer player = task.owner;
				
				if(!StoredData[player.userID].Skins.ContainsKey(item.info.shortname) || !permission.UserHasPermission(player.UserIDString, "xskinmenu.craft")) return;
				if(StoredData[player.userID].ChangeSC) SetSkinCraft(player, item);
			}
		} 
		
		private void SetSkinItem(BasePlayer player, string item, ulong skin) 
		{
			foreach (var i in player.inventory.FindItemIDs(ItemManager.FindItemDefinition(item).itemid))
			{
				if (i.skin == skin) continue;
				
				if(errorskins.ContainsKey(skin))
				{
					var erroritem = errorskins[skin];
					
					i.UseItem();
					Item newitem = ItemManager.CreateByName(erroritem);
					newitem.condition = i.condition;
					newitem.maxCondition = i.maxCondition;
					
					player.GiveItem(newitem); 
				}
				else
				{
                    i.skin = skin;
                    i.MarkDirty();

                    BaseEntity entity = i.GetHeldEntity();
                    if (entity != null)
                    {
                        entity.skinID = skin;
                        entity.SendNetworkUpdate();
                    }
				}
			}
		}
		
		private void SetSkinCraft(BasePlayer player, Item item)
		{
			string shortname = item.info.shortname;
			ulong skin = StoredData[player.userID].Skins[shortname];
			
			if (item.skin == skin) return;
			
			if(errorskins.ContainsKey(skin))
			{
				var erroritem = errorskins[skin];
					
				item.UseItem();
				Item newitem = ItemManager.CreateByName(erroritem);
					
				player.GiveItem(newitem);
			}
			else
		    {
                item.skin = skin; 
                item.MarkDirty();

                BaseEntity entity = item.GetHeldEntity();
                if (entity != null)
                {
                    entity.skinID = skin;
                    entity.SendNetworkUpdate();
				}
			}
		}		
		
		private void SetSkinEntity(BasePlayer player, BaseEntity entity, string shortname)
		{
			ulong skin = StoredData[player.userID].Skins[shortname];
			
			if(skin == entity.skinID || skin == 0) return;
			if(errorskins.ContainsKey(skin))
			{
				SendInfo(player, lang.GetMessage("ERRORSKIN", this, player.UserIDString));
				return;
			}
			
			entity.skinID = skin;
            entity.SendNetworkUpdate();
			Effect.server.Run("assets/prefabs/deployable/repair bench/effects/skinchange_spraypaint.prefab", entity.transform.localPosition);
		}
		
		#endregion		
		
		#region GUI
		
		private void GUI(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".GUIS");
            CuiElementContainer container = new CuiElementContainer(); 
			
			container.Add(new CuiPanel
            {
				CursorEnabled = true,
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-507.5 -260", OffsetMax = "507.5 290" },
                Image = { Color = "0.517 0.521 0.509 0.95", Material = "assets/icons/greyout.mat" }
            }, "Overlay", ".GUIS");
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                Image = { Color = "0.217 0.221 0.209 0.95" }
            }, ".GUIS", ".SGUI");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "470 237.5", OffsetMax = "497.5 265" },
                Button = { Color = "1 1 1 0.75", Sprite = "assets/icons/close.png", Close = ".GUIS" },
                Text = { Text = "" }
            }, ".SGUI");			
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-497.5 237.5", OffsetMax = "-470 265" },
                Button = { Color = "1 1 1 0.75", Sprite = "assets/icons/gear.png", Command = "skin_s open" },
                Text = { Text = "" }
            }, ".SGUI");
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-455 237.5", OffsetMax = "455 265" },
                Text = { Text = lang.GetMessage("TITLE", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 18, Color = "1 1 1 0.75" }
            }, ".SGUI");
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-507.5 227.5", OffsetMax = "507.5 232.5" },
                Image = { Color = "0.517 0.521 0.509 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".SGUI");				
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-507.5 177.5", OffsetMax = "507.5 182.5" },
                Image = { Color = "0.517 0.521 0.509 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".SGUI");			
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "460 227.5", OffsetMax = "465 275" },
                Image = { Color = "0.517 0.521 0.509 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".SGUI");			
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-465 227.5", OffsetMax = "-460 275" },
                Image = { Color = "0.517 0.521 0.509 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".SGUI");   
			
			CuiHelper.AddUi(player, container);
			
			CategoryGUI(player);
			if(config.Category.Count != 0) ItemGUI(player, config.Category.ElementAt(0).Key);
		}
		
		private void CategoryGUI(BasePlayer player, int page = 0)
		{
			CuiHelper.DestroyUi(player, ".SkinBUTTON");
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-497.5 187.5", OffsetMax = "497.5 222.5" },
                Image = { Color = "0 0 0 0" }
            }, ".SGUI", ".SkinBUTTON");
			
			int x = 0, count = config.Category.Count; 
			
			foreach(var category in config.Category)
			{
				string color = page == x ? "0.53 0.77 0.35 0.8" : "0 0 0 0";
				double offset = -(81 * count--) + -(2.5 * count--);

				container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{offset} -17.5", OffsetMax = $"{offset + 162} 17.5" },
                    Button = { Color = "0.517 0.521 0.509 0.5", Material = "assets/icons/greyout.mat", Command = $"skin_c category {category.Key} {x}" },
                    Text = { Text = lang.GetMessage(category.Key, this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 18, Color = "0.75 0.75 0.75 1" }
                }, ".SkinBUTTON", ".BUTTON");
 
			    container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMax = "0 1.5" },
                    Image = { Color = color, Material = "assets/icons/greyout.mat" }
                }, ".BUTTON");
				
				x++;
			}
			
			CuiHelper.AddUi(player, container);
		}
		
		private void ItemGUI(BasePlayer player, string category, int Page = 0)
		{
			CuiHelper.DestroyUi(player, ".SettingGUI");
			CuiHelper.DestroyUi(player, ".SkinGUI");
			CuiHelper.DestroyUi(player, ".ItemGUI");
            CuiElementContainer container = new CuiElementContainer(); 
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-502.5 -270", OffsetMax = "502.5 177.5" },
                Image = { Color = "0 0 0 0" }
            }, ".SGUI", ".ItemGUI");
			
			int x = 0, y = 0, z = 0;
			
			foreach(var item in config.Category[category].Skip(Page * 40))
			{		
			    container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-497.5 + (x * 100)} {123.25 - (y * 100)}", OffsetMax = $"{-402.5 + (x * 100)} {218.25 - (y * 100)}" },
                    Image = { Color = "0.517 0.521 0.509 0.5", Material = "assets/icons/greyout.mat" }
                }, ".ItemGUI", ".Item");
				
				container.Add(new CuiElement 
                {
                    Parent = ".Item",
                    Components =
                    {
					    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", item + 150) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" }
                    }
                });
				
				if(StoredDataSkins.ContainsKey(item) && StoredDataSkins[item].Count != 0)
				{
				    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Button = { Color = "0 0 0 0", Command = $"skin_c skin {item}" },
                        Text = { Text = "" }
                    }, ".Item");				    
				
				    if(StoredData[player.userID].Skins[item] != 0)
				        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-20 5", OffsetMax = "-5 20" },
                            Button = { Color = "1 1 1 0.75", Sprite = "assets/icons/clear.png", Command = $"skin_c clear {item} {z}" },
                            Text = { Text = "" }
                        }, ".Item", $".I + {z}");
				}
				
				x++;
				z++;
				
				if(x == 10)
				{
					x = 0;
					y++;
					
					if(y == 4)
						break;
				}
			}
			
			bool back = Page != 0;
			bool next = config.Category[category].Count > ((Page + 1) * 40);

			container.Add(new CuiButton
            {    
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -215.25", OffsetMax = "0 -185.25" },
                Button = { Color = "0 0 0 0", Command = back ? $"page.xskinmenu item back {category} {Page}" : "" },
                Text = { Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 26, Font = "robotocondensed-regular.ttf", Color = back ? "1 1 1 0.75" : "1 1 1 0.1" }
            }, ".ItemGUI");	
 
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-15 -215.25", OffsetMax = "15 -185.25" },
                Text = { Text = $"{Page + 1}", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 18, Color = "1 1 1 0.75" }
            }, ".ItemGUI");					
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "0 -215.25", OffsetMax = "50 -185.25" },
                Button = { Color = "0 0 0 0", Command = next ? $"page.xskinmenu item next {category} {Page}" : "" },
                Text = { Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 26, Font = "robotocondensed-regular.ttf", Color = next ? "1 1 1 0.75" : "1 1 1 0.1" }
            }, ".ItemGUI");
			
			CuiHelper.AddUi(player, container);
		}		
		
		private void SkinGUI(BasePlayer player, string item, int Page = 0)
		{
			CuiHelper.DestroyUi(player, ".SkinGUI");
            CuiElementContainer container = new CuiElementContainer(); 
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-502.5 -270", OffsetMax = "502.5 177.5" },
                Image = { Color = "0.217 0.221 0.209 1" }
            }, ".SGUI", ".SkinGUI");
			
			int x = 0, y = 0;
			ulong s = StoredData[player.userID].Skins[item];
			
			foreach(var skin in StoredDataSkins[item].Skip(Page * 40))
			{		
			    container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-497.5 + (x * 100)} {123.25 - (y * 100)}", OffsetMax = $"{-402.5 + (x * 100)} {218.25 - (y * 100)}" },
                    Image = { Color = s == skin ? "0.53 0.77 0.35 0.8" : "0.517 0.521 0.509 0.5", Material = "assets/icons/greyout.mat" }
                }, ".SkinGUI", ".Skin");
				
				container.Add(new CuiElement
                {
                    Parent = ".Skin",
                    Components =
                    {
					    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", errorskins.ContainsKey(skin) ? errorskins[skin] + 152 : $"{skin}152") },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" }
                    }
                });
				
				container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Command = $"skin_c setskin {item} {skin}" },
                    Text = { Text = "" }
                }, ".Skin");				
				
				x++;
				
				if(x == 10)
				{
					x = 0;
					y++;
					
					if(y == 4)
						break;
				}
			}
			
			bool back = Page != 0;
			bool next = StoredDataSkins[item].Count > ((Page + 1) * 40);

			container.Add(new CuiButton
            {    
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -215.25", OffsetMax = "0 -185.25" },
                Button = { Color = "0 0 0 0", Command = back ? $"page.xskinmenu skin back {item} {Page}" : "" },
                Text = { Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 26, Font = "robotocondensed-regular.ttf", Color = back ? "1 1 1 0.75" : "1 1 1 0.1" }
            }, ".SkinGUI");	
 
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-15 -215.25", OffsetMax = "15 -185.25" },
                Text = { Text = $"{Page + 1}", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 18, Color = "1 1 1 0.75" }
            }, ".SkinGUI");					
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "0 -215.25", OffsetMax = "50 -185.25" },
                Button = { Color = "0 0 0 0", Command = next ? $"page.xskinmenu skin next {item} {Page}" : "" },
                Text = { Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 26, Font = "robotocondensed-regular.ttf", Color = next ? "1 1 1 0.75" : "1 1 1 0.1" }
            }, ".SkinGUI");
			
			CuiHelper.AddUi(player, container);
		}
		
		private void SettingGUI(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".SettingGUI");
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-502.5 -270", OffsetMax = "502.5 177.5" },
                Image = { Color = "0.217 0.221 0.209 1" }
            }, ".SGUI", ".SettingGUI");
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-300 -85", OffsetMax = "300 -60" },
                Text = { Text = lang.GetMessage("SETINFO", this, player.UserIDString), Align = TextAnchor.LowerCenter, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "0.75 0.75 0.75 0.4" }
            }, ".SettingGUI");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = ".SettingGUI" },
                Text = { Text = "" }
            }, ".SettingGUI");
			
			container.Add(new CuiPanel
            {
				CursorEnabled = true,
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-292.5 -52.5", OffsetMax = "292.5 52.5" },
                Image = { Color = "0.517 0.521 0.509 0.95", Material = "assets/icons/greyout.mat" }
            }, ".SettingGUI", ".SGUIM");
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                Image = { Color = "0.217 0.221 0.209 0.95" }
            }, ".SGUIM", ".SGUIMM");
			
			Dictionary<string, bool> setting = new Dictionary<string, bool>
			{
				["inventory"] = StoredData[player.userID].ChangeSI,
				["entity"] = StoredData[player.userID].ChangeSE,
				["craft"] = StoredData[player.userID].ChangeSC,
				["clear"] = StoredData[player.userID].ChangeSCL
			};
			
			int x = 0, y = 0;
			
			foreach(var s in setting)
			{		
			    container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-282.5 + (x * 285)} {2.5 - (y * 45)}", OffsetMax = $"{-2.5 + (x * 285)} {42.5 - (y * 45)}" },
                    Image = { Color = "0.517 0.521 0.509 0.5", Material = "assets/icons/greyout.mat" }
                }, ".SGUIMM", ".SM");
				
				container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-135 -15", OffsetMax = "-105 15" },
                    Button = { Color = s.Value ? "0.53 0.77 0.35 0.8" : "1 0.4 0.35 0.8", Command = $"skin_s {s.Key}" },
                    Text = { Text = "" }
                }, ".SM");				
				
			    container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100 -15", OffsetMax = "135 15" },
                    Text = { Text = lang.GetMessage(s.Key, this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 16, Color = "0.75 0.75 0.75 1" }
                }, ".SM");
				
				x++;
				
				if(x == 2)
				{
					x = 0;
					y++;
				}
			}
			
			CuiHelper.AddUi(player, container);
		}  
		
		#endregion
		
		#region Message
		
		private void SendInfo(BasePlayer player, string message)
        {
            player.SendConsoleCommand("gametip.showgametip", message);
            timer.Once(5f, () => player.SendConsoleCommand("gametip.hidegametip"));
        }
		
		#endregion
		
		#region Lang
 
        private void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE"] = "COOL SERVER SKINS MENU",
				["SETINFO"] = "YOU CAN CUSTOMIZE THE MENU DEPENDING ON THE SITUATION!",
				["ERRORSKIN"] = "THE SKIN YOU CHOSE CAN BE CHANGED ONLY IN THE INVENTORY OR WHEN CRAFTING!",
				["NOPERM"] = "No permissions!",
                ["weapon"] = "WEAPON",
                ["construction"] = "CONSTRUCTION",
                ["item"] = "ITEM",
                ["attire"] = "ATTIRE",
                ["tool"] = "TOOL",
                ["inventory"] = "CHANGE SKIN IN INVENTORY",
                ["entity"] = "CHANGE SKIN ON OBJECTS", 
                ["craft"] = "CHANGE SKIN WHEN CRAFTING",
                ["clear"] = "CHANGE SKIN WHEN DELETING"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE"] = "МЕНЮ СКИНОВ DEMONIC RUST",
                ["SETINFO"] = "ВЫ МОЖЕТЕ КАСТОМНО НАСТРАИВАТЬ МЕНЮ В ЗАВИСИМОСТИ ОТ СИТУАЦИИ!",
                ["ERRORSKIN"] = "ВЫБРАННЫЙ ВАМИ СКИН МОЖНО ИЗМЕНИТЬ ТОЛЬКО В ИНВЕНТАРЕ ИЛИ ПРИ КРАФТЕ!",
				["NOPERM"] = "Недостаточно прав!",
                ["weapon"] = "ОРУЖИЕ",
                ["construction"] = "СТРОИТЕЛЬСТВО",
                ["item"] = "ПРЕДМЕТЫ",
                ["attire"] = "ОДЕЖДА",
                ["tool"] = "ИНСТРУМЕНТЫ",
                ["inventory"] = "ПОМЕНЯТЬ СКИН В ИНВЕНТАРЕ",
                ["entity"] = "ПОМЕНЯТЬ СКИН НА ПРЕДМЕТАХ",
                ["craft"] = "ПОМЕНЯТЬ СКИН ПРИ КРАФТЕ",
                ["clear"] = "ПОМЕНЯТЬ СКИН ПРИ УДАЛЕНИИ"
            }, this, "ru");
        }

        #endregion
	}
}