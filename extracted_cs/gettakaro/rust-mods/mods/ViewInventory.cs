using System.Text;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins;

[Info("ViewInventory", "Takaro", "0.0.3")]
internal class ViewInventory : RustPlugin
{
    [ConsoleCommand("viewinventory")]
    private void CmdViewInventory(ConsoleSystem.Arg arg)
    {
        if (!arg.IsRcon) return;

        if (arg.Args == null || arg.Args.Length == 0)
        {
            arg.ReplyWith("[ViewInventory] Please provide a SteamID64");
            return;
        }

        string playerId = arg.Args[0];
        if (playerId == null)
        {
            arg.ReplyWith("[ViewInventory] Please provide a valid SteamID64");
            return;
        }

        BasePlayer player = BasePlayer.FindByID(ulong.Parse(playerId));
        if (player == null)
        {
            arg.ReplyWith("[ViewInventory] Could not find player with SteamID64: " + playerId);
            return;
        }

        string inventoryJson = ConvertItemsToJson(
            player.inventory.containerMain.itemList
                .Concat(player.inventory.containerBelt.itemList)
                .Concat(player.inventory.containerWear.itemList)
                .ToList()
        );

        CmdResponse response = new(player.displayName, player.UserIDString, player.net.ID.ToString(), inventoryJson);

        arg.ReplyWith("[ViewInventory] " + response.ToJson());
    }

    private static string ConvertItemsToJson(IReadOnlyCollection<Item>? items)
    {
        if (items == null || items.Count == 0)
            return "[]";

        StringBuilder jsonBuilder = new();
        jsonBuilder.Append("[");

        bool firstItem = true;
        foreach (Item? item in items)
        {
            if (!firstItem)
                jsonBuilder.Append(",");
            else
                firstItem = false;

            jsonBuilder.Append("{");
            jsonBuilder.Append($"\"itemName\": \"{item.info.shortname}\",");
            jsonBuilder.Append($"\"amount\": {item.amount}");

            if (item.hasCondition)
            {
                jsonBuilder.Append($",\"condition\": {item.condition}");
                jsonBuilder.Append($",\"maxCondition\": {item.maxCondition}");
            }

            jsonBuilder.Append("}");
        }

        jsonBuilder.Append("]");

        return jsonBuilder.ToString();
    }

    private class CmdResponse
    {
        public CmdResponse(string username, string userId, string netId, string inventory)
        {
            UserName = username;
            UserID = userId;
            NetworkID = netId;
            Inventory = inventory;
        }

        public string UserName { get; set; }
        public string UserID { get; set; }
        public string NetworkID { get; set; }
        public string Inventory { get; set; }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}