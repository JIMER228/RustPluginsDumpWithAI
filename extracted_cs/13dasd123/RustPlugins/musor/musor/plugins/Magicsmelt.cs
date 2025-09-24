using System;
using System.Collections.Generic;
using Oxide.Core;

//	Author info:
//   * Lomarine
//   * https://vk.com/id41504602

namespace Oxide.Plugins
{
    [Info("Magic Smelt", "Lomarine", "1.0.0")]
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
				    Smelter(item, -891243783);
				    break;
			    case "hq.metal.ore":
				    Smelter(item, 374890416);
				    break;
			    case "metal.ore":
				    Smelter(item, 688032252);
				    break;
			    case "wood":
				    Smelter(item, 1436001773);
				    break;
			    case "bearmeat":
				    Smelter(item, -2043730634);
				    break;
			    case "deermeat.raw":
				    Smelter(item, -202239044);
				    break;
			    case "humanmeat.raw":
				    Smelter(item, -991829475);
				    break;
			    case "meat.boar":
				    Smelter(item, 991728250);
				    break;
			    case "wolfmeat.raw":
				    Smelter(item, -1691991080);
				    break;
			    case "chicken.raw":
				    Smelter(item, 1734319168);
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
				    Smelter(item, -891243783);
				    break;
			    case "hq.metal.ore":
				    Smelter(item, 374890416);
				    break;
			    case "metal.ore":
				    Smelter(item, 688032252);
				    break;
			    case "wood":
				    Smelter(item, 1436001773);
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
				    Smelter(item, -891243783);
				    break;
			    case "metal.ore":
				    Smelter(item, 688032252);
				    break;
			    case "wood":
				    Smelter(item, 1436001773);
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
			    ["M.PERM"]  = "<color=cyan>[Magic Tools]</color> You do not have permission to use <color=cyan>magic smelt</color>.",
			    ["M.TOOLS"] = "<color=cyan>[Magic Tools]</color> Magic smelting with tools <color=green>activated</color>.",
			    ["M.OFF"]   = "<color=cyan>[Magic Tools]</color> Magic smelting <color=red>deacticated</color>.",
			    ["M.INV"]   = "<color=cyan>[Magic Tools]</color> Your inventory was smelled <color=green>successfully</color>.",
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
						Smelter(item, -891243783);
						break;
					case "hq.metal.ore":
						Smelter(item, 374890416);
						break;
					case "metal.ore":
						Smelter(item, 688032252);
						break;
					case "wood":
						Smelter(item, 1436001773);
						break;
					case "bearmeat":
						Smelter(item, -2043730634);
						break;
					case "deermeat.raw":
						Smelter(item, -202239044);
						break;
					case "humanmeat.raw":
						Smelter(item, -991829475);
						break;
					case "meat.boar":
						Smelter(item, 991728250);
						break;
					case "wolfmeat.raw":
						Smelter(item, -1691991080);
						break;
					case "chicken.raw":
						Smelter(item, 1734319168);
						break;
					case "can.beans.empty":
						Smelter(item, 688032252);
						break;
					case "can.tuna.empty":
						Smelter(item, 688032252);
						break;
					case "crude.oil":
						Smelter(item, 28178745);
						break;
					case "fish.raw":
						Smelter(item, -2078972355);
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