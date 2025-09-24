// Requires: Navmesher

#region using
	using Convert = System.Convert;
	using Network;
	using Newtonsoft.Json;
	using System;
	using System.Collections.Generic;
	using System.Collections;
	using System.Linq;
	using System.Text;
	using Oxide.Core.Libraries.Covalence;
	using Oxide.Plugins;
	using Oxide.Core.Plugins;
	using Oxide.Core;
	using UnityEngine; 
	using UnityEngine.SceneManagement;
	using UnityEngine.AI;
	using Rust.Ai;
	using Oxide.Ext.RustEdit;
	using Oxide.Ext.RustEdit.NPC;
using Oxide.Game.Rust.Cui;
using CompanionServer.Handlers;
#endregion
namespace Oxide.Plugins{
	[Info("TestPlatform", "obsol", "0.2.1")]
	[Description("Makes NPCs and Animals attack any NPC/animal that doesn't share their prefab name")]
	public class TestPlatform: CovalencePlugin{//
		/*
		static Dictionary<string,string> factionColorStrings = new Dictionary<string, string>(){
			{"tunneldweller",@"""color"": ""0.9 0.8 0.3 0.5"","},
			{"neutral",@"""color"": ""0.6 0.6 0.6 0.5"","},
			{"bandit",@"""color"": ""0.9 0.3 0.3 0.5"","},
			{"scientist",@"""color"": ""0.3 0.3 0.9 0.5"","}
		};
		
		static string FactionShopPanel(string s){
			return @"
						{
							""name"": """+s+@""",
							""parent"": ""Overlay"",
							""components"":
							[
								{
									""type"":""RectTransform"",
									""anchormin"": ""0.75 0.7"",
									""anchormax"": ""0.95 0.9""
								}
							]
						},";
		}
		static string GenerateFactionButton(string s, string text, int i,string color, string action, bool close=true){
			string buttonName = "Button"+((int)(UnityEngine.Random.Range(0f,1f)*99));
			return @"
				{
					""name"": """+buttonName+@""",
					""parent"": """+s+@""",
					""components"":
					[
						{
							""type"":""UnityEngine.UI.Button"",
							"+(close?@"""close"":"""+s+@""",":"")+@"
							"+(action!=null&&action!=""?@"""command"":"""+action+@""",":"")+@"
							"+color+@"
							""imagetype"": ""Tiled""
						},
						{
							""type"":""RectTransform"",
							""anchormin"": """+((i-1)*0.33).ToString()+@" 0.75"",
							""anchormax"": """+((i)*0.33).ToString()+@" 1""
						}
					]
				},
				{
					""parent"": """+buttonName+@""",
					""name"": ""buttonText"",
					""components"":
					[
						{
							""type"":""RectTransform"",
							""anchormin"": ""0 0"",
							""anchormax"": ""1 1""
						}
					]
				},
				{
					""parent"": """+buttonName+@""",
					""name"": ""buttonText"",
					""components"":
					[
						{
							""type"":""UnityEngine.UI.Text"",
							""text"":"""+text+@""",
							""fontSize"":12,
							""align"": ""MiddleCenter"",
						},
						{
							""type"":""RectTransform"",
							""anchormin"": ""0.1 0.1"",
							""anchormax"": ""0.9 0.9""
						}
					]
				},
			";
		}
		static string GenerateBalanceSection(string faction, string balance, string level){
			return @"{
				""parent"": """+faction+@""",
				""name"": ""buttonText"",
				""components"":
				[
					{
						""type"":""UnityEngine.UI.Text"",
						""text"":""Current balance: "+balance+@""",
						""fontSize"":12,
						""align"": ""MiddleCenter"",
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""0 0.2"",
						""anchormax"": ""1 0.35""
					}
				]
			},
			{
				""parent"": """+faction+@""",
				""name"": ""buttonText2"",
				""components"":
				[
					{
						""type"":""UnityEngine.UI.Text"",
						""text"":""Current level: "+level+@""",
						""fontSize"":12,
						""align"": ""MiddleCenter"",
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""0 0.35"",
						""anchormax"": ""1 0.45""
					}
				]
			},
			{
				""name"": ""BalanceCloseButton"",
				""parent"": """+faction+@""",
				""components"":
				[
					{
						""type"":""UnityEngine.UI.Button"",
						""close"":"""+faction+@""",
						""command"":""FactionSys.VendorTop"",
						""color"": ""0.6 0.6 0.6 0.5"",
						""imagetype"": ""Tiled""
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""0 0"",
						""anchormax"": ""1 0.2""
					}
				]
			},
			{
				""parent"": ""BalanceCloseButton"",
				""name"": ""buttonClose"",
				""components"":
				[
					{
						""type"":""UnityEngine.UI.Text"",
						""text"":""Back"",
						""fontSize"":12,
						""align"": ""MiddleCenter"",
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""0.1 0.1"",
						""anchormax"": ""0.9 0.9""
					}
				]
			},";
		}
		
		string FactionVendorTop = "["+FactionShopPanel("VendorTop")+
			GenerateFactionButton("VendorTop","Bandit",1,factionColorStrings["bandit"],"FactionSys.VendorFaction bandit")+
			GenerateFactionButton("VendorTop","Scientist",2,factionColorStrings["scientist"],"FactionSys.VendorFaction scientist")+
			GenerateFactionButton("VendorTop","Tunnel Dweller",3,factionColorStrings["tunneldweller"],"FactionSys.VendorFaction tunneldweller")+"]";
		string FactionVendorBandit = "["+FactionShopPanel("VendorBandit")+
			GenerateFactionButton("VendorBandit","Sell Neutral",1,factionColorStrings["neutral"],"FactionSys.SellID bandit neutral")+
			GenerateFactionButton("VendorBandit","Sell Scientist",2,factionColorStrings["scientist"],"FactionSys.SellID bandit scientist")+
			GenerateFactionButton("VendorBandit","Sell Tunnel Dweller",3,factionColorStrings["tunneldweller"],"FactionSys.SellID bandit tunneldweller")+
			GenerateBalanceSection("VendorBandit","000","000")+"]";
		string FactionVendorScientist = "["+FactionShopPanel("VendorBandit")+
			GenerateFactionButton("VendorBandit","Sell Bandit",1,factionColorStrings["bandit"],"FactionSys.SellID scientist bandit")+
			GenerateFactionButton("VendorBandit","Sell Neutral",2,factionColorStrings["neutral"],"FactionSys.SellID scientist neutral")+
			GenerateFactionButton("VendorBandit","Sell Tunnel Dweller",3,factionColorStrings["tunneldweller"],"FactionSys.SellID scientist tunneldweller")+
			GenerateBalanceSection("VendorBandit","000","000")+"]";
		string FactionVendorTunnel = "["+FactionShopPanel("VendorBandit")+
			GenerateFactionButton("VendorBandit","Sell Bandit",1,factionColorStrings["bandit"],"FactionSys.SellID tunneldweller bandit")+
			GenerateFactionButton("VendorBandit","Sell Scientist",2,factionColorStrings["scientist"],"FactionSys.SellID tunneldweller scientist")+
			GenerateFactionButton("VendorBandit","Sell Neutral",3,factionColorStrings["neutral"],"FactionSys.SellID tunneldweller neutral")+
			GenerateBalanceSection("VendorBandit","000","000")+"]";
			
			
		// Breaker.RecomposeMonument compound.prefab
        [Command("testplatform.test")]
		void testplatform(IPlayer player, string command, string[] args){//
			BasePlayer new_player = player.Object as BasePlayer;
			CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
				{
					connection = new_player.Connection
				}, null, "DestroyUI", "VendorBandit");	
			CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
			{
				connection = new_player.Connection
			}, null, "DestroyUI", "VendorTop");
			CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
			{
				connection = new_player.Connection
			}, null, "AddUI", FactionVendorTop);
			
		}
		
        [Command("FactionSys.SellID")]
		void SellID(IPlayer player, string command, string[] args){//
			BasePlayer new_player = player.Object as BasePlayer;		
			CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
				{
					connection = new_player.Connection
				}, null, "DestroyUI", "VendorBandit");	
			CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
				{
					connection = new_player.Connection
				}, null, "DestroyUI", "VendorTop");//
			
			if(args.Count()<2){return;}
			int amountSold = 0;
			Item tags = null;
			switch(args[1]){
				case "bandit":
					Puts("sell bandit");
					tags = new_player.inventory.FindItemByItemName("redidtag");
					break;
				case "scientist":
					tags = new_player.inventory.FindItemByItemName("blueidtag");
					break;
				case "tunneldweller":
					tags = new_player.inventory.FindItemByItemName("yellowidtag");
					break;
				case "neutral":
					tags = new_player.inventory.FindItemByItemName("grayidtag");
					break;
				default:break;
			}
			if(tags!=null){
				Puts("Selling Tag");
				if(tags.amount>1){
					tags.amount--;
					tags.MarkDirty();
				}else{
					Puts("Destroy Tag");
					tags.Remove(0);
				}
				amountSold++;
				Puts("Amount sold:"+amountSold);
			}
			PlayerFactionHandler pfh= new_player.gameObject.GetComponent<PlayerFactionHandler>();
			if(pfh==null){
				pfh=new_player.gameObject.AddComponent<PlayerFactionHandler>();
			}
			
			Dictionary<string,string> panels;
			
			switch(args[0]){
				case "bandit":		
					pfh.factions[args[0]].balance+=amountSold;
					Puts("Balance:"+pfh.factions[args[0]].balance);
					pfh.factions[args[0]].updateLevels();
					panels =generateFactionShopForPlayer(new_player);		
					CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
						{
							connection = new_player.Connection
						}, null, "AddUI",  panels["bandit"]);
					break;
				
				case "scientist":		
					pfh.factions[args[0]].balance+=amountSold;
					pfh.factions[args[0]].updateLevels();
					Puts("Balance:"+pfh.factions[args[0]].balance);
					panels =generateFactionShopForPlayer(new_player);		
					CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
						{
							connection = new_player.Connection
						}, null, "AddUI",  panels["scientist"]);
					break;
				
				case "tunneldweller":		
					pfh.factions[args[0]].balance+=amountSold;
					pfh.factions[args[0]].updateLevels();
					Puts("Balance:"+pfh.factions[args[0]].balance);
					panels =generateFactionShopForPlayer(new_player);		
					CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
						{
							connection = new_player.Connection
						}, null, "AddUI",  panels["tunneldweller"]);
					break;
				
				default:
					break;
			}
			
		}
		class PlayerFaction{
			public int balance=0;
			public int level=0;
			public int levelCost=10;
			public StorageContainer itemBank;
			
			public void updateLevels(){
				if(balance>=levelCost){
					balance-=levelCost;
					level++;
				}
			}
		}
		class PlayerFactionHandler:BaseMonoBehaviour{
			public Dictionary<string,PlayerFaction> factions = new Dictionary<string,PlayerFaction>(){
				{"bandit", new PlayerFaction()},
				{"scientist", new PlayerFaction()},
				{"tunneldweller", new PlayerFaction()},
			};
		}
		Dictionary<string,string> generateFactionShopForPlayer(BasePlayer bp){
			
			PlayerFactionHandler pfh= bp.gameObject.GetComponent<PlayerFactionHandler>();
			if(pfh==null){
				pfh=bp.gameObject.AddComponent<PlayerFactionHandler>();
			}
			
			bool hasBandit = bp.inventory.FindItemByItemName("redidtag")!=null;
			bool hasScientist = bp.inventory.FindItemByItemName("blueidtag")!=null;
			bool hasTunnel = bp.inventory.FindItemByItemName("yellowidtag")!=null;
			bool hasNeutral = bp.inventory.FindItemByItemName("grayidtag")!=null;
			
			
			return new Dictionary<string, string>(){{"bandit", "["+FactionShopPanel("VendorBandit")+
			(hasNeutral?GenerateFactionButton("VendorBandit","Sell Neutral",1,factionColorStrings["neutral"],"FactionSys.SellID bandit neutral"):"")+
			(hasScientist?GenerateFactionButton("VendorBandit","Sell Scientist",2,factionColorStrings["scientist"],"FactionSys.SellID bandit scientist"):"")+
			(hasTunnel?GenerateFactionButton("VendorBandit","Sell Tunnel Dweller",3,factionColorStrings["tunneldweller"],"FactionSys.SellID bandit tunneldweller"):"")+
			GenerateBalanceSection("VendorBandit",pfh.factions["bandit"].balance.ToString(),pfh.factions["bandit"].level.ToString())+"]"},
			{"scientist",  "["+FactionShopPanel("VendorBandit")+
			(hasBandit?GenerateFactionButton("VendorBandit","Sell Bandit",1,factionColorStrings["bandit"],"FactionSys.SellID scientist bandit"):"")+
			(hasNeutral?GenerateFactionButton("VendorBandit","Sell Neutral",2,factionColorStrings["neutral"],"FactionSys.SellID scientist neutral"):"")+
			(hasTunnel?GenerateFactionButton("VendorBandit","Sell Tunnel Dweller",3,factionColorStrings["tunneldweller"],"FactionSys.SellID scientist tunneldweller"):"")+
			GenerateBalanceSection("VendorBandit",pfh.factions["scientist"].balance.ToString(),pfh.factions["scientist"].level.ToString())+"]"},
			{"tunneldweller", "["+FactionShopPanel("VendorBandit")+
			(hasBandit?GenerateFactionButton("VendorBandit","Sell Bandit",1,factionColorStrings["bandit"],"FactionSys.SellID tunneldweller bandit"):"")+
			(hasScientist?GenerateFactionButton("VendorBandit","Sell Scientist",2,factionColorStrings["scientist"],"FactionSys.SellID tunneldweller scientist"):"")+
			(hasNeutral?GenerateFactionButton("VendorBandit","Sell Neutral",3,factionColorStrings["neutral"],"FactionSys.SellID tunneldweller neutral"):"")+
			GenerateBalanceSection("VendorBandit",pfh.factions["tunneldweller"].balance.ToString(),pfh.factions["tunneldweller"].level.ToString())+"]"}};
		}
		
        [Command("FactionSys.VendorFaction")]
		void VendorFaction(IPlayer player, string command, string[] args){//
			BasePlayer new_player = player.Object as BasePlayer;
			CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
				{
					connection = new_player.Connection
				}, null, "DestroyUI", "VendorBandit");	
			CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
				{
					connection = new_player.Connection
				}, null, "DestroyUI", "VendorTop");//
			
			if(args.Count()==0){return;}
			Dictionary<string,string> panels =generateFactionShopForPlayer(new_player);
			switch(args[0]){
				case "bandit":
					CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
					{
						connection = new_player.Connection
					}, null, "AddUI", panels["bandit"]);
					break;
				case "scientist":
					CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
					{
						connection = new_player.Connection
					}, null, "AddUI", panels["scientist"]);
					break;
				case "tunneldweller":
					CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
					{
						connection = new_player.Connection
					}, null, "AddUI", panels["tunneldweller"]);
					break;
				default:
					break;//
			}
			
		}
        [Command("FactionSys.VendorTop")]
		void FactionTop(IPlayer player, string command, string[] args){//
			BasePlayer new_player = player.Object as BasePlayer;
			CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
			{
				connection = new_player.Connection
			}, null, "AddUI", FactionVendorTop);
			
		}
		
		void Loaded(){
			foreach(BasePlayer p in BasePlayer.allPlayerList){
				CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
				{
					connection = p.Connection
				}, null, "DestroyUI", "VendorTop");//
					
				CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
				{
					connection = p.Connection
				}, null, "DestroyUI", "VendorTop");
				CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
					{
						connection = p.Connection
					}, null, "DestroyUI", "VendorTop");//
			}
			Puts("Loaded");
		}
		void Unload(){
			foreach(BasePlayer p in BasePlayer.allPlayerList){
				CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
				{
					connection = p.Connection
				}, null, "DestroyUI", "VendorTop");//
					
				CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
				{
					connection = p.Connection
				}, null, "DestroyUI", "VendorTop");
				CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
					{
						connection = p.Connection
					}, null, "DestroyUI", "VendorTop");//
				PlayerFactionHandler pfh= p.gameObject.GetComponent<PlayerFactionHandler>();
				if(pfh!=null){
					GameObject.Destroy(pfh);
				}
			}
			Puts("Loaded");
		}
		void OnVendingShopOpened(VendingMachine vm, BasePlayer player){
			CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
			{
				connection = player.Connection
			}, null, "AddUI", FactionVendorTop);
			Puts("Vending");
			return ;
			
		}	
		bool? OnLootEntityEnd(BasePlayer player,VendingMachine vm){ 
			CommunityEntity.ServerInstance.ClientRPCEx<string>(new Network.SendInfo
			{
				connection = player.Connection
			}, null, "DestroyUI", "VendorTop");//
			Puts("Vending done");
			return null;
		}
		*/
	}
}