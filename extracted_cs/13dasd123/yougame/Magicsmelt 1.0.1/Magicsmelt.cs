using System;
using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Magic Smelt", "Rust-Plugin.ru", "1.0.1")]
    class Magicsmelt : RustPlugin
    {
	    private List<ulong> ActiveUsers = new List<ulong>();
	    private int cMultiplier;
 
	    void Init()
	    {
		    permission.RegisterPermission("magicsmelt.tools", this);
		    permission.RegisterPermission("magicsmelt.inventory", this);
		    LoadDefaultConfig();
		    LoadDefaultMessages();
		    LoadData();
	    }

	    #region Gathering
	    
	    void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
	    {
		    BasePlayer player = entity.ToPlayer();
		    if(!permission.UserHasPermission(player.UserIDString, "magicsmelt.tools") || !ActiveUsers.Contains(player.userID))
			    return;

		    switch (item.info.shortname)
		    {
			    case "sulfur.ore":
				    Smelter(item, -1581843485);
				    break;
			    case "hq.metal.ore":
				    Smelter(item, 317398316);
				    break;
			    case "metal.ore":
				    Smelter(item, 69511070);
				    break;
			    case "wood":
				    Smelter(item, -1938052175);
				    break;
			    case "bearmeat":
				    Smelter(item, 1873897110);
				    break;
			    case "deermeat.raw":
				    Smelter(item, -1509851560);
				    break;
			    case "humanmeat.raw":
				    Smelter(item, 1536610005);
				    break;
			    case "meat.boar":
				    Smelter(item, -242084766);
				    break;
			    case "wolfmeat.raw":
				    Smelter(item, 813023040);
				    break;
			    case "chicken.raw":
				    Smelter(item, -1848736516);
				    break;
		    }
	    }
	    
	    void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
	    {
		    if(!permission.UserHasPermission(player.UserIDString, "magicsmelt.tools") || !ActiveUsers.Contains(player.userID))
			    return;
		    switch (item.info.shortname)
		    {
			    case "sulfur.ore":
				    Smelter(item, -1581843485);
				    break;
			    case "hq.metal.ore":
				    Smelter(item, 317398316);
				    break;
			    case "metal.ore":
				    Smelter(item, 69511070);
				    break;
			    case "wood":
				    Smelter(item, -1938052175);
				    break;
		    }
	    }
	    
	    void OnCollectiblePickup(Item item, BasePlayer player)
	    {
		    if(!permission.UserHasPermission(player.UserIDString, "magicsmelt.tools") || !ActiveUsers.Contains(player.userID))
			    return;
		    switch (item.info.shortname)
		    {
			    case "sulfur.ore":
				    Smelter(item, -1581843485);
				    break;
			    case "metal.ore":
				    Smelter(item, 69511070);
				    break;
			    case "wood":
				    Smelter(item, -1938052175);
				    break;
		    }
	    }

	    #endregion

	    #region Data & Config & Lang
        		
        private void LoadData() => ActiveUsers = Interface.Oxide.DataFileSystem.ReadObject<List<ulong>>("MagicSmelt");
        		
        void OnServerSave() => SaveData();
        		
        void Unload() => SaveData();
        		
        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("MagicSmelt", ActiveUsers);
		
	    protected override void LoadDefaultConfig()
	    {
		    Config["Charcoal Multiplier"] = cMultiplier = GetConfig("Charcoal Multiplier", 1);
		    SaveConfig();
	    }
	    
	    T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));    
	    
	    protected override void LoadDefaultMessages()
	    {
		    lang.RegisterMessages(new Dictionary<string, string>
		    {
			    ["M.PERM"]  = "<color=cyan>[Magic Tools]</color> У вас нет разрешения на использование <color=cyan>magic smelt</color>.",
			    ["M.TOOLS"] = "<color=cyan>[Magic Tools]</color> Магическая плавка с помощью инструментов <color=green>включена</color>.",
			    ["M.OFF"]   = "<color=cyan>[Magic Tools]</color> Магическая плавка <color=red>выключена</color>.",
			    ["M.INV"]   = "<color=cyan>[Magic Tools]</color> Твой инвентарь переплавлен <color=green>успешно</color>.",
		    }, this);
	    }
	    
	    #endregion

	    #region Commands
		   
	    [ChatCommand("msmelt")]
	    private void CmdTools(BasePlayer player)
	    {
		    if (!permission.UserHasPermission(player.UserIDString, "magicsmelt.tools"))
		    {
			    SendReply(player, lang.GetMessage("M.PERM", this, player.UserIDString));
			    return;
		    }
		    if (ActiveUsers.Contains(player.userID))
		    {
			    SendReply(player, lang.GetMessage("M.OFF", this, player.UserIDString));
			    ActiveUsers.Remove(player.userID);
			    return;
		    }
		    SendReply(player, lang.GetMessage("M.TOOLS", this, player.UserIDString));
		    ActiveUsers.Add(player.userID);
	    }
  
	    [ChatCommand("ismelt")]
	    private void SmeltCmd(BasePlayer player)
	    {
		    if (!permission.UserHasPermission(player.UserIDString, "magicsmelt.inventory"))
		    {
			    SendReply(player, lang.GetMessage("M.PERM", this, player.UserIDString));
		    	return;
	    	}
		    foreach (Item item in player.inventory.containerMain.itemList)
		    {
			    switch (item.info.shortname)
				{
					case "sulfur.ore":
						Smelter(item, -1581843485);
						break;
					case "hq.metal.ore":
						Smelter(item, 317398316);
						break;
					case "metal.ore":
						Smelter(item, 69511070);
						break;
					case "wood":
						Smelter(item, -1938052175);
						break;
					case "bearmeat":
						Smelter(item, 1873897110);
						break;
					case "deermeat.raw":
						Smelter(item, -1509851560);
						break;
					case "humanmeat.raw":
						Smelter(item, 1536610005);
						break;
					case "meat.boar":
						Smelter(item, -242084766);
						break;
					case "wolfmeat.raw":
						Smelter(item, 813023040);
						break;
					case "chicken.raw":
						Smelter(item, -1848736516);
						break;
					case "can.beans.empty":
						Smelter(item, 69511070);
						break;
					case "can.tuna.empty":
						Smelter(item, 69511070);
						break;
					case "crude.oil":
						Smelter(item, -946369541);
						break;
					case "fish.raw":
						Smelter(item, 1668129151);
						break;
				}
		    }
		    player.SendNetworkUpdate();
		    SendReply(player, lang.GetMessage("M.INV", this, player.UserIDString));
	    }

	    #endregion

	    #region Main function
	    
	    private void Smelter(Item item, int ID)
	    {
		    if (item.info.shortname == "wood")
			    item.amount = item.amount * cMultiplier;
		    if (item.info.shortname == "can.beans.empty")
			    item.amount = item.amount * 15;
		    if (item.info.shortname == "can.tuna.empty")
			    item.amount = item.amount * 10;
		    if (item.info.shortname == "crude.oil")
			    item.amount = item.amount * 4;
		    
			Item nitem = ItemManager.CreateByItemID(ID, item.amount);
			item.info = nitem.info;
	    }
	    
	    #endregion
    }
}