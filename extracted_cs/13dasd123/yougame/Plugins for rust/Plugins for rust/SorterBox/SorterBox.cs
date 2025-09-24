using System.Collections.Generic; 
using System; 
using System.Linq; 
using UnityEngine; 
using Oxide.Core.Plugins; 
using Oxide.Core.Configuration; 
using Oxide.Game.Rust.Cui; 
using Oxide.Core;  

namespace Oxide.Plugins 
{	
	[Info("SorterBox", "S1m0n", "1.2.0")]  
	class SorterBox : RustPlugin 
	{
		[PluginReference] Plugin NoEscape;  
		
		SavedBoxes boxdata; 
		
		private DynamicConfigFile BOXDATA; 
		
		string TitleColor = "<color=orange>"; 
		string MsgColor = "<color=#A9A9A9>";  
		bool ready;  
		
		private Dictionary<string, Timer> timers = new Dictionary<string, Timer>(); 
		private Dictionary<ulong, info> UIInfo = new Dictionary<ulong, info>(); 
		
		class info 
		{
			public SortingBox box = null; 
			public StorageContainer cont = null; 
			public int page; 
		}  
		
		void Loaded() 
		{
			BOXDATA = Interface.Oxide.DataFileSystem.GetFile("SorterBox_Data"); 
			lang.RegisterMessages(messages, this); 
			ready = false; 
		}  
		void Unload() 
		{
			foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, PanelSorting); 
			
			var StorageBoxes = UnityEngine.Object.FindObjectsOfType<SortingBox>().ToList(); 
			
			if (StorageBoxes.Count > 0) 
				foreach (var obj in StorageBoxes) { GameObject.Destroy(obj); } 
			
			foreach (var timer in timers) timer.Value.Destroy(); 
			
			timers.Clear();
			SaveData();
		}  
		void OnPlayerDisconnected(BasePlayer player) 
		{
			CuiHelper.DestroyUi(player, PanelSorting); 
		}  
		void OnPlayerRespawned(BasePlayer player) 
		{
			CuiHelper.DestroyUi(player, PanelSorting); 
		}  
		void OnServerInitialized() 
		{
			LoadVariables(); 
			LoadData(); 
			
			permission.RegisterPermission(this.Title + ".allow", this); 
			
			timers.Add("info", timer.Once(900, () => InfoLoop())); 
			timers.Add("save", timer.Once(600, () => SaveLoop())); 
			timers.Add("box", timer.Once(10, () => InitializeBoxes())); 
		}  
		private void InitializeBoxes() 
		{
			if (timers.ContainsKey("box")) 
			{
				timers["box"].Destroy(); 
				timers.Remove("box"); 
			} 
			
			List<int> Badboxes = new List<int>(); 
			
			StorageContainer[] containers = StorageContainer.FindObjectsOfType<StorageContainer>(); 
			
			foreach (var entry in boxdata.boxes) 
			{
				var cont = containers.Where(k => k.transform.localPosition == GetVector3(entry.Value.Location)).Select(k => k).FirstOrDefault(); 
				
				if (cont == null) 
				{
					Badboxes.Add(entry.Key); 
					continue;												
				} 
				
				cont.gameObject.AddComponent<SortingBox>(); 
				cont.gameObject.GetComponent<SortingBox>().BoxCategory = boxdata.boxes[entry.Key].BoxCategory; 
				cont.gameObject.GetComponent<SortingBox>().SortedItems = boxdata.boxes[entry.Key].SortedItems; 
				cont.gameObject.GetComponent<SortingBox>().index = entry.Key;
			} 
			
			ready = true; 
		}
		private void InitializeBox(StorageContainer cont, int index) 
		{
			if (cont == null) 
			{
				boxdata.boxes.Remove(index); 
				return; 
			} 
			
			cont.gameObject.AddComponent<SortingBox>(); 
			cont.gameObject.GetComponent<SortingBox>().BoxCategory = boxdata.boxes[index].BoxCategory; 
			cont.gameObject.GetComponent<SortingBox>().SortedItems = boxdata.boxes[index].SortedItems; 
			cont.gameObject.GetComponent<SortingBox>().index = index;
		}  
		
		Vector3 GetVector3(XYZ xyz) 
		{
			return new Vector3 
			{
				x = xyz.x, 
				y = xyz.y, 
				z = xyz.z 
			}; 
		}  
		
		void OnLootEntity(BasePlayer player, BaseEntity entity) 
		{
			if (!ready || player == null || entity == null || entity as StorageContainer == null || !permission.UserHasPermission(player.UserIDString, this.Title + ".allow") || (configData.RequireTC && !CupAuthorized(player))) 
				return; 
			if (!configData.AllowALLContainers) 
				if (!entity.PrefabName.Contains("box")) 
					return; 
			if (!UIInfo.ContainsKey(player.userID)) 
				UIInfo.Add(player.userID, new info()); 
			if (NoEscape && NoEscape.Call("IsRaidBlocked", player) is bool && (bool)NoEscape.Call("IsRaidBlocked", player)) 
				return; 
			if (entity.GetComponent<SortingBox>() != null) 
				UIInfo[player.userID].box = entity.GetComponent<SortingBox>(); 
			else 
				UIInfo[player.userID].cont = entity as StorageContainer; SortingPanel(player); 
		}  
		void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity) 
		{
			if (entity == null || player == null || entity as StorageContainer == null) 
				return; 
			if (UIInfo.ContainsKey(player.userID)) 
			{
				UIInfo[player.userID].cont = null; 
				UIInfo[player.userID].box = null;
			} 
			
			CuiHelper.DestroyUi(player, PanelSorting); 
		}  
		
		static bool CupAuthorized(BasePlayer player) 
		{
			if (player == null) 
				return false; 
				
			List<BuildingPrivlidge> playerpriv = player.buildingPrivilege; 
			
			if (playerpriv == null || playerpriv.Count == 0) 
			{
				return false; 
			} 
			
			foreach (BuildingPrivlidge priv in playerpriv.ToArray()) 
			{
				List<ProtoBuf.PlayerNameID> authorized = priv.authorizedPlayers; 
				
				bool foundplayer = false; 
				
				foreach (ProtoBuf.PlayerNameID pni in authorized.ToArray()) 
				if (pni.userid == player.userID) 
					foundplayer = true; 
				if (!foundplayer) 
					return false; 
			} 
			return true; 
		}  
		private string PanelSorting = "PanelSorting";
		
		public class UI 
		{
			static public CuiElementContainer CreateMenuContainer(string panelName, string color, string aMin, string aMax, bool cursor = false) 
			{
				var NewElement = new CuiElementContainer() 
				{
					{
						new CuiPanel {
							Image = {Color = color}, 
							RectTransform = {AnchorMin = aMin, AnchorMax = aMax}, 
							CursorEnabled = cursor 
						}, new CuiElement().Parent = "Hud.Menu", panelName 
					} 
				}; 
				return NewElement; 
			}  
		
			static public void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false) 
			{
				container.Add(new CuiPanel { 
					Image = { Color = color }, 
					RectTransform = { AnchorMin = aMin, AnchorMax = aMax }, 
					CursorEnabled = cursor 
				}, panel); 
			} 
			static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter) 
			{
				container.Add(new CuiLabel { 
					Text = { Color = color, FontSize = size, Align = align, FadeIn = 1.0f, Text = text }, 
					RectTransform = { AnchorMin = aMin, AnchorMax = aMax } 
				}, panel); 
			}  
			static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter) 
			{
				container.Add(new CuiButton { 
					Button = { Color = color, Command = command, FadeIn = 1.0f }, 
					RectTransform = { AnchorMin = aMin, AnchorMax = aMax }, 
					Text = { Text = text, FontSize = size, Align = align } 
				}, panel); 
			}  
			static public void LoadImage(ref CuiElementContainer container, string panel, string img, string aMin, string aMax) 
			{
				container.Add(new CuiElement {
					Parent = panel, 
					Components = {
						new CuiRawImageComponent {Png = img, Sprite = "assets/content/textures/generic/fulltransparent.tga" }, 
						new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax } 
					} 
				}); 
			} 
		}  
		private void GetSendMSG(BasePlayer player, string message, string arg1 = "", string arg2 = "", string arg3 = "") 
		{
			string msg = string.Format(lang.GetMessage(message, this, player.UserIDString), arg1, arg2, arg3); 
			
			SendReply(player, TitleColor + lang.GetMessage("title", this, player.UserIDString) + "</color>" + MsgColor + msg + "</color>");
		}  
		
		private string GetMSG(string message, BasePlayer player = null, string arg1 = "", string arg2 = "", string arg3 = "") 
		{
			string p = null; 
			
			if (player != null) 
				p = player.UserIDString; 
			if (messages.ContainsKey(message)) 
				return string.Format(lang.GetMessage(message, this, p), arg1, arg2, arg3); 
			else 
				return message; 
		}  
		
		void SortingPanel(BasePlayer player) 
		{
			CuiHelper.DestroyUi(player, PanelSorting); 
			
			if (configData.RequireTC && !CupAuthorized(player)) 
				return; 
			if (!UIInfo.ContainsKey(player.userID)) 
				return; 
			
			var box = UIInfo[player.userID].box; UIInfo[player.userID].page = 0; 
			var element = UI.CreateMenuContainer(PanelSorting, "0 0 0 0", $"{configData.minx} {configData.miny}", $"{configData.maxx} {configData.maxy}"); 
			
			if (box == null) 
				UI.CreateButton(ref element, PanelSorting, "255 255 255 0.03", GetMSG("Добавить",player), 14, ".3 0.2", ".7 .8", $"UI_AS_AddBox"); 
			else 
			{
				UI.CreateButton(ref element, PanelSorting, "255 255 255 0.03", GetMSG("Удалить", player), 14, ".525 .1", ".8 .5", $"UI_AS_RemoveBox"); 
				UI.CreateButton(ref element, PanelSorting, "255 255 255 0.03", GetMSG("Выберите категорию", player), 12, ".05 0.6", ".475 .9", $"UI_AS_ChangeCategory"); 
				
				if (box.BoxCategory != null && box.BoxCategory.Count() > 0) 
					UI.CreateButton(ref element, PanelSorting, "255 255 255 0.03", GetMSG("Выберите предмет", player), 12, ".525 0.6", ".95 .9", $"UI_AS_ChangePage 0"); 
				
				UI.CreateButton(ref element, PanelSorting, "255 255 255 0.03", GetMSG("Сортировать", player), 13, ".2 .1", ".475 .5", $"UI_AS_Sort"); 
			} 
			
			CuiHelper.AddUi(player, element); 
		}  
		void ItemSelection(BasePlayer player) 
		{
			CuiHelper.DestroyUi(player, PanelSorting); 
			
			if (configData.RequireTC && !CupAuthorized(player)) 
				return; 
			if (!UIInfo.ContainsKey(player.userID)) 
				return; 
			
			var box = UIInfo[player.userID].box; 
			var element = UI.CreateMenuContainer(PanelSorting, "0 0 0 0", $"{configData.minx} {configData.miny}", $"{configData.maxx} {configData.maxy}"); 
			
			int entriesallowed = 14; 
			int page = UIInfo[player.userID].page; 
			
			List<ItemDefinition> Items = new List<ItemDefinition>(); 
			
			Items = ItemManager.GetItemDefinitions().Where(k => box.BoxCategory.Contains(k.category)).ToList();
			
			int shownentries = page * entriesallowed; 
			int i = 0; 
			int n = 0; 
			
			if (page == 0) 
			{
				foreach (var entry in box.BoxCategory) 
				{
					var pos = EntryPOS(n); 
					var color = "255 255 255 0.03"; 
					
					if (box.SortedItems.Contains($"{entry.ToString()}_ALL")) 
						color = "0.2 0.8 0.2 1"; 
						
					UI.CreateButton(ref element, PanelSorting, color, GetMSG("AllBlank",player, GetMSG(entry.ToString(), player)), 10, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_AS_SetBoxItem {entry.ToString()}"); 
					n++; 
					i++; 
				} 
			} 
			
			foreach (var entry in Items.OrderBy(k => k.displayName.translated)) 
			{
				i++; 
				
				if (i < shownentries + 1) 
					continue; 
				else if (i <= shownentries + entriesallowed) 
				{
					var pos = EntryPOS(n); 
					var color = "255 255 255 0.03"; 
					
					if (box.SortedItems.Contains(entry.shortname)) 
						color = "0.2 0.8 0.2 1"; 
						
					UI.CreateButton(ref element, PanelSorting, color, entry.displayName.translated, 10, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_AS_SetBoxItem {entry.itemid}"); 
					n++; 
				} 
			} 
			
			int remainingentries = Items.Count() - (page * entriesallowed); 
			
			if (page > 0) 
				UI.CreateButton(ref element, PanelSorting, "255 255 255 0.03", GetMSG("Назад", player), 12, "0.68 0.03", "0.835 0.19", $"UI_AS_ChangePage {page - 1}"); 
			if (remainingentries > entriesallowed) 
				UI.CreateButton(ref element, PanelSorting, "255 255 255 0.03", GetMSG("Далее", player), 12, "0.845 0.03", "1 0.19", $"UI_AS_ChangePage {page + 1}"); 
				
			CuiHelper.AddUi(player, element); 
		}   
		[ConsoleCommand("UI_AS_ChangePage")] 
		void cmdUI_AS_ChangePage(ConsoleSystem.Arg arg) 
		{ var player = arg.Connection.player as BasePlayer; if (player == null) return; if (!UIInfo.ContainsKey(player.userID) || UIInfo[player.userID].box == null) return; int page; if (!int.TryParse(arg.Args[0], out page)) page = 0; UIInfo[player.userID].page = page; ItemSelection(player); 
		}  
		[ConsoleCommand("UI_AS_ChangeCategory")] 
		void cmdUI_AS_ChangeCategory(ConsoleSystem.Arg arg) 
		{ var player = arg.Connection.player as BasePlayer; if (player == null) return; if (!UIInfo.ContainsKey(player.userID) || UIInfo[player.userID].box == null) return; var box = UIInfo[player.userID].box; CuiHelper.DestroyUi(player, PanelSorting); var i = 0; var element = UI.CreateMenuContainer(PanelSorting, "0 0 0 0", $"{configData.minx} {configData.miny}", $"{configData.maxx} {configData.maxy}"); foreach (var entry in Enum.GetValues(typeof(ItemCategory)).Cast<ItemCategory>().Where(k => k != ItemCategory.Search && k != ItemCategory.All && k != ItemCategory.Common).ToList()) { var pos = EntryPOS(i); var color = "255 255 255 0.03"; if (box.BoxCategory.Contains(entry)) color = "0.2 0.8 0.2 1"; UI.CreateButton(ref element, PanelSorting, color, GetMSG(entry.ToString(), player), 10, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_AS_SetCategory {entry.ToString()}"); i++; } CuiHelper.AddUi(player, element); 
		}  
		
		private float[] EntryPOS(int number) 
		{
			Vector2 position = new Vector2(0f, 0.83f); 
			Vector2 dimensions = new Vector2(0.32f, 0.16f); 
			
			float offsetY = 0; 
			float offsetX = 0; 
			
			if (number >= 0 && number < 5) 
			{
				offsetY = (-0.04f - dimensions.y) * number; 
			} 
			if (number > 4 && number < 10) 
			{
				offsetY = (-0.04f - dimensions.y) * (number - 5); 
				offsetX = (0.02f + dimensions.x); 
			} 
			if (number > 9 && number < 15) 
			{
				offsetY = (-0.04f - dimensions.y) * (number - 10); 
				offsetX = (0.02f + dimensions.x) * 2; 
			} 
			
			Vector2 offset = new Vector2(offsetX, offsetY); 
			Vector2 posMin = position + offset; 
			Vector2 posMax = posMin + dimensions; 
			
			return new float[] 
			{
				posMin.x, 
				posMin.y, 
				posMax.x, 
				posMax.y 
			};
		}   
		[ConsoleCommand("UI_AS_SetBoxItem")] 
		void cmdUI_AS_SetBoxItem(ConsoleSystem.Arg arg) 
		{
			var player = arg.Connection.player as BasePlayer; 
			
			if (player == null) 
				return; 
			if (!UIInfo.ContainsKey(player.userID) || UIInfo[player.userID].box == null) 
				return; 
				
			var box = UIInfo[player.userID].box; 
			
			int itemid; 
			
			if (!int.TryParse(arg.Args[0], out itemid)) 
			{
				ItemCategory cat = (ItemCategory)Enum.Parse(typeof(ItemCategory), arg.Args[0]); 
				
				if (boxdata.boxes[box.index].SortedItems.Contains($"{cat}_ALL")) 
					boxdata.boxes[box.index].SortedItems.Remove($"{cat}_ALL"); 
				else 
					boxdata.boxes[box.index].SortedItems.Add($"{cat}_ALL"); 
			} 
			else 
			{
				ItemDefinition itemdef = ItemManager.FindItemDefinition(itemid); 
				
				if (boxdata.boxes.ContainsKey(box.index)) 
				{
					if (boxdata.boxes[box.index].SortedItems.Contains(itemdef.shortname)) 
					{
						boxdata.boxes[box.index].SortedItems.Remove(itemdef.shortname); 
						
						if (boxdata.boxes[box.index].SortedItems.Count() < 1) 
							boxdata.boxes[box.index].SortedItems.Add($"{itemdef.category}_ALL"); 
					} 
					else
					{
						boxdata.boxes[box.index].SortedItems.Add(itemdef.shortname); 
						
						if (boxdata.boxes[box.index].SortedItems.Contains($"{itemdef.category}_ALL")) 
							boxdata.boxes[box.index].SortedItems.Remove($"{itemdef.category}_ALL"); 
					} 
				} 
			} 
			
			InitializeBox(box.GetComponent<StorageContainer>(), box.index); 
			SortingPanel(player); 
		}   
		[ConsoleCommand("UI_AS_SetCategory")] 
		void cmdUI_AS_SetCategory(ConsoleSystem.Arg arg) 
		{
			var player = arg.Connection.player as BasePlayer; 
			
			if (player == null) 
				return; 
			if (!UIInfo.ContainsKey(player.userID) || UIInfo[player.userID].box == null) 
				return; 
				
			var box = UIInfo[player.userID].box; 
			ItemCategory cat = (ItemCategory)Enum.Parse(typeof(ItemCategory), arg.Args[0]); 
			
			if (boxdata.boxes.ContainsKey(box.index)) 
			{
				if (boxdata.boxes[box.index].BoxCategory.Contains(cat)) 
					boxdata.boxes[box.index].BoxCategory.Remove(cat); 
				else 
					boxdata.boxes[box.index].BoxCategory.Add(cat); 
			} 
			
			InitializeBox(box.GetComponent<StorageContainer>(), box.index); 
			SortingPanel(player); 
		}   
		[ConsoleCommand("UI_AS_RemoveBox")] 
		void cmdUI_AS_RemoveBox(ConsoleSystem.Arg arg) 
		{
			var player = arg.Connection.player as BasePlayer; 
			
			if (player == null) 
				return; 
			if (!UIInfo.ContainsKey(player.userID) || UIInfo[player.userID].box == null) 
				return; 
			
			var box = UIInfo[player.userID].box; 
			
			if (boxdata.boxes.ContainsKey(box.index)) 
			{
				UIInfo[player.userID].cont = box.GetComponent<StorageContainer>(); 
				GameObject.Destroy(box); 
				UIInfo[player.userID].box = null;
				boxdata.boxes.Remove(box.index); 
			} 
			
			SortingPanel(player); 
		}  
		[ConsoleCommand("UI_AS_AddBox")] 
		void cmdUI_AS_AddBox(ConsoleSystem.Arg arg) 
		{
			var player = arg.Connection.player as BasePlayer; 
			
			if (player == null) 
				return; 
			if (!UIInfo.ContainsKey(player.userID) || UIInfo[player.userID].cont == null) 
				return; 
			
			var cont = UIInfo[player.userID].cont; 
			
			if (cont.GetComponent<SortingBox>() != null) 
				return; 
			
			int index = boxdata.boxes.Count() > 0 ? boxdata.boxes.Keys.Max()+1 : 0; 
			
			boxdata.boxes.Add(index, new BoxDetails 
			{
				Location = new XYZ 
				{
					x = cont.transform.position.x, 
					y = cont.transform.position.y,
					z = cont.transform.position.z 
				} 
			}); 
			
			InitializeBox(cont, index); 
			UIInfo[player.userID].box = cont.GetComponent<SortingBox>(); 
			SortingPanel(player); 
		}  
		[ConsoleCommand("UI_AS_Sort")] 
		void cmdUI_AS_Sort(ConsoleSystem.Arg arg) 
		{
			var player = arg.Connection.player as BasePlayer; 
			
			if (player == null) 
				return; 
			if (!UIInfo.ContainsKey(player.userID) || UIInfo[player.userID].box == null) 
				return; 
				
			List<StorageContainer> list = new List<StorageContainer>(); 
			Vis.Entities(player.transform.position, configData.SortingRadius, list); 
			
			foreach (var entry in list.Where(k => k.GetComponent<SortingBox>() != null).OrderBy(k=>Vector2.Distance(player.transform.position, k.transform.position))) 
			{
				List<Item> CurrentItems = new List<global::Item>(); 
				CurrentItems = player.inventory.containerMain.itemList.Where(k => entry.GetComponent<SortingBox>().BoxCategory.Contains(k.info.category) && (entry.GetComponent<SortingBox>().SortedItems.Count() < 1 || entry.GetComponent<SortingBox>().SortedItems.Contains(k.info.category+"_ALL") || entry.GetComponent<SortingBox>().SortedItems.Contains(k.info.shortname))).Select(k => k).ToList(); 
				
				if (configData.IncludeBeltInSorting) 
					CurrentItems.AddRange(player.inventory.containerBelt.itemList.Where(k => entry.GetComponent<SortingBox>().BoxCategory.Contains(k.info.category) && (entry.GetComponent<SortingBox>().SortedItems.Count() < 1 || entry.GetComponent<SortingBox>().SortedItems.Contains(k.info.category + "_ALL") || entry.GetComponent<SortingBox>().SortedItems.Contains(k.info.shortname))).Select(k => k).ToList()); 
					
				foreach (var item in CurrentItems) 
				if (!item.CanMoveTo(entry.inventory)) 
					continue; 
				else 
					item.MoveToContainer(entry.inventory); 
			} 
			foreach (var entry in list.Where(k => k.GetComponent<SortingBox>() != null)) 
			{
				List<Item> CurrentItems = entry.inventory.itemList.ToList(); 
				
				while (entry.inventory.itemList.Count > 0) 
					entry.inventory.itemList[0].RemoveFromContainer(); 
					
				foreach (var item in CurrentItems.Where(k => entry.GetComponent<SortingBox>().BoxCategory.Contains(k.info.category)).OrderBy(k =>k.info.category).ThenBy(k=>k.info.itemid)) item.MoveToContainer(entry.inventory); 
				foreach (var item in CurrentItems.Where(k => !entry.GetComponent<SortingBox>().BoxCategory.Contains(k.info.category)).OrderBy(k => k.info.category).ThenBy(k => k.info.itemid)) item.MoveToContainer(entry.inventory); 
			}  
		}  
		private void SaveLoop() 
		{
			if (timers.ContainsKey("save")) 
			{
				timers["save"].Destroy(); 
				timers.Remove("save"); 
			} 
			
			SaveData(); 
			
			timers.Add("save", timer.Once(600, () => SaveLoop())); 
		}  
		private void InfoLoop() 
		{
			if (timers.ContainsKey("info")) 
			{
				timers["info"].Destroy(); 
				timers.Remove("info"); 
			}
			if (configData.InfoInterval == 0) 
				return; 
				
			foreach (BasePlayer p in BasePlayer.activePlayerList) GetSendMSG(p, "ASInfo"); 
			
			timers.Add("info", timer.Once(configData.InfoInterval * 60, () => InfoLoop())); 
		}  
		void SaveData() 
		{
			BOXDATA.WriteObject(boxdata); 
		}  
		void LoadData() 
		{
			try 
			{
				boxdata = BOXDATA.ReadObject<SavedBoxes>(); 
				
				if (boxdata == null) 
					boxdata = new SavedBoxes(); 
			} 
			catch 
			{ 
				Puts("Couldn't load SorterBox Data, creating new datafile"); 
				boxdata = new SavedBoxes(); 
			} 
			
			if (boxdata.boxes == null) 
				boxdata.boxes = new Dictionary<int, BoxDetails>(); 
		}  
		class SortingBox : MonoBehaviour 
		{
			public List<ItemCategory> BoxCategory = new List<ItemCategory>(); 
			public List<string> SortedItems = new List<string>(); 
			
			public int index; 
		}  
		class SavedBoxes 
		{
			public Dictionary<int, BoxDetails> boxes = new Dictionary<int, BoxDetails>(); 
		}  
		class BoxDetails 
		{
			public XYZ Location;
			public List<ItemCategory> BoxCategory = new List<ItemCategory>(); 
			public List<string> SortedItems = new List<string>(); 
		}  
		class XYZ 
		{
			public float x; 
			public float y; 
			public float z; 
		}  
		
		float Default_minx = 0.646f; 
		float Default_miny = 0f; 
		float Default_maxx = 0.84f; 
		float Default_maxy = 0.14f;  
		
		private ConfigData configData; 
		
		class ConfigData 
		{
			public int InfoInterval { get; set; } 
			public float SortingRadius { get; set; } 
			public bool IncludeBeltInSorting { get; set; } 
			public bool AllowALLContainers { get; set; }
			public bool RequireTC { get; set; } 
			public float minx { get; set; } 
			public float miny { get; set; }
			public float maxx { get; set; } 
			public float maxy { get; set; } 
		} 
		
		private void LoadVariables() 
		{
			LoadConfigVariables(); 
			
			if (configData == null) 
				LoadDefaultConfig(); 
				SaveConfig(); 
			if (configData.maxx == new float() && configData.maxy == new float() && configData.minx == new float() && configData.miny == new float()) 
			{
				configData.minx = Default_minx; 
				configData.miny = Default_miny; 
				configData.maxx = Default_maxx; 
				configData.maxy = Default_maxy; 
				
				SaveConfig(configData); 
			} 
		} 
		protected override void LoadDefaultConfig() 
		{
			var config = new ConfigData 
			{
				InfoInterval = 15, 
				SortingRadius = 10f,
				
				IncludeBeltInSorting = false, 
				AllowALLContainers = false, 
				RequireTC = true, 
				
				minx = Default_minx, 
				miny = Default_miny, 
				maxx = Default_maxx, 
				maxy = Default_maxy, 
			}; 
			
			SaveConfig(config); 
		} 
		private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>(); 
		void SaveConfig(ConfigData config) => Config.WriteObject(config, true);  
		
		Dictionary<string, string> messages = new Dictionary<string, string>() 
		{
			{"title", "Сортировка: " }, 
			{"ASInfo", "Чтобы воспользоваться сортировщиком, откройте ящик в радиусе действия шкафа"}, 
			{"RemoveBox", "Удалить" }, 
			{"AddBox", "<size=20>Добавить</size>" }, 
			{"SelectCategory", "Выберите категорию" }, 
			{"Sort", "Сортировать" }, 
			{"SelectItems", "Выберите предмет" }, 
			{"AllBlank", "Все {0}" }, 
			{"Weapon","Оружие" }, 
			{"Construction","Строения" }, 
			{"Items","Предметы" }, 
			{"Resources","Ресурсы" }, 
			{"Attire","Одежда" }, 
			{"Tool","Инструменты" }, 
			{"Medical","Медикаменты" }, 
			{"Food","Еда" }, 
			{"Ammunition","Боеприпасы" }, 
			{"Traps","Ловушки" }, 
			{"Misc","Прочее" }, 
			{"Component","Компоненты" },  
		}; 
	}
} 