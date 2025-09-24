using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;
using System;
using UnityEngine;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("F1Spawn", "Colon Blow", "1.0.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    [Description("Allows use of F1 Item List Spawn")]
    class F1Spawn : CovalencePlugin
    {

        void Loaded()
        {
            permission.RegisterPermission("f1spawn.allowed", this);
            permission.RegisterPermission("f1spawn.bypassblacklist", this);
        }

        bool isAllowed(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        private PluginConfig config;

        private class PluginConfig
        {

            [JsonProperty("List of BlackListed items")]
            public List<string> BlackListedItems;

            public static PluginConfig DefaultConfig() => new PluginConfig()
            {
                BlackListedItems = new List<string>()
                    {
                        "Satchel Charge",
                        "Timed Explosive Charge",
                        "Beancan Grenade",
                        "F1 Grenade"
                    }
            };
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("New configuration file created!!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        /////////////////////////////////////////////////////////

        [Command("inventory.giveid")]
        void GiveIdCommand(IPlayer player, string command, string[] args)
        {
        }

        object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.cmd == null) return null;

            string command = arg.cmd.Name;

            if (command.Equals("giveid"))
            {

                BasePlayer player = arg.Player();
                if (!player) return true;
                if (isAllowed(player, "f1spawn.allowed") || isAllowed(player, "f1spawn.bypassblacklist") || player.net?.connection?.authLevel > 0)
                {

                    Item item = ItemManager.CreateByItemID(arg.GetInt(0), 1, 0);
                    if (item == null) return true;
                    if (!isAllowed(player, "f1spawn.bypassblacklist"))
                    {
                        if (config.BlackListedItems.Contains(item.info.displayName.english) || config.BlackListedItems.Contains(item.info.shortname))
                        {
                            if (player.net?.connection?.authLevel < 1) return false;
                        }
                    }

                    item.amount = arg.GetInt(1, 1);
                    if (!player.inventory.GiveItem(item, null))
                    {
                        item.Remove(0f);
                        return true;
                    }
                    player.Command("note.inv", new object[] { item.info.itemid, item.amount });
                    //Debug.Log(string.Concat(new object[] { "[F1Spawn] giving ", player.displayName, " ", item.amount, " x ", item.info.displayName.english }));
                    return true;
                }
            }
            return null;
        }
    }
}
