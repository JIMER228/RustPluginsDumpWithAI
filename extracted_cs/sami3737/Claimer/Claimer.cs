// Reference: Oxide.Core.MySql
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Oxide.Core;
using UnityEngine;
using Oxide.Core.Database;

namespace Oxide.Plugins
{
    [Info("Claimer", "sami37", "0.0.1")]
    [Description("Claim your donate!")]
    public class Claimer : RustPlugin
    {
        private readonly Core.MySql.Libraries.MySql _mySql = Interface.GetMod().GetLibrary<Core.MySql.Libraries.MySql>();
        private Connection _mySqlConnection;
        private string db_name = "rust";

        private string ipadress = "127.0.0.1";
        private string password = "";
        private int port = 3306;
        private string user = "rust";

        #region Oxide Hooks

        private void ReadFromConfig<T>(string Key, ref T var)
        {
            if (Config[Key] != null)
            {
                var = (T)Convert.ChangeType(Config[Key], typeof(T));
            }
            Config[Key] = var;
        }
        private static List<BasePlayer> FindPlayersOnline(string nameOrIdOrIp)
        {
            var players = new List<BasePlayer>();
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
                else if (activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.IgnoreCase))
                    players.Add(activePlayer);
                else if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
            }
            return players;
        }
        private static List<BasePlayer> FindPlayersOffline(string nameOrIdOrIp)
        {
            var players = new List<BasePlayer>();
            foreach (var activePlayer in BasePlayer.sleepingPlayerList)
            {
                if (activePlayer.UserIDString.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
                else if (activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.IgnoreCase))
                    players.Add(activePlayer);
                else if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
            }
            return players;
        }

        private void OnServerInitialized()
        {
            permission.RegisterPermission("claimer.canclaim", this);
            permission.RegisterPermission("claimer.cantransform", this);
            permission.RegisterPermission("claimer.steamfind", this);
            ReadFromConfig("address", ref ipadress);
            ReadFromConfig("port", ref port);
            ReadFromConfig("db_name", ref db_name);
            ReadFromConfig("user", ref user);
            ReadFromConfig("password", ref password);
            SaveConfig();

            var messages = new Dictionary<string, string>
            {
                {
                    "Nothing", "There is nothing to take"
                },
                {
                    "Claimed", "You successfully claim {0} {1}."
                },
				{
					"NoPerm", "You don't have permission to do this."
				}
            };
            lang.RegisterMessages(messages, this);
        }

        private string GetMessage(string name, string sid = null)
        {
            return lang.GetMessage(name, this, sid);
        }

        [ChatCommand("claim")]
        private void ClaimCommand(BasePlayer player, string command, string[] args)
        {
            var playerid = player.userID;
            if (permission.UserHasPermission(playerid.ToString(), "claimer.canclaim"))
            {
                var packageClaimed = "";
                _mySqlConnection = _mySql.OpenDb(ipadress, port, db_name, user, password, this);
                var sql = Sql.Builder.Append("select * from payment where AccountUniqueNumber = '" + player.userID + "' AND Taked = 0 AND Status = 'Complete';");
                _mySql.Query(sql, _mySqlConnection, list =>
                {
                    if (list == null || list.Count == 0)
                    {
                        SendReply(player, GetMessage("Nothing"));
                        return;
                    }
                    foreach (var entry in list)
                    {
                        var inv = player.inventory.containerMain.capacity;
                        var length = player.inventory.containerMain.itemList.Count;
                        var available = inv - length;
                        var itemcount = entry["ItemName"].ToString().Split(',');
                        if (available < itemcount.Length)
                        {
                            SendReply(player, string.Format("You have {0} free space but you need {1} space to claim.", available, Convert.ToInt32(entry["Amount"])));
                            return;
                        }
                        var itemss = new StringBuilder();
                        var amountitem = new StringBuilder();
                        if (entry["ItemName"].ToString().Contains(","))
                        {
                            var items = entry["ItemName"].ToString().Split(',');
                            var amounts = entry["AmountSerie"].ToString().Split(',');
                            for (var i = 0; i < items.Length; i++)
                            {
                                var sb = new StringBuilder();
                                sb.AppendLine();
								if (((string) entry["ItemName"]).ToLower() == "evolution" ||
									((string) entry["ItemName"]).ToLower() == "donator" ||
									((string) entry["ItemName"]).ToLower() == "donator+")
								{
									sb.AppendFormat(" {0}\t{1}\t{2}", player.userID, entry["ItemName"], " 30d");
									sb.AppendLine();
									ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(),
										$"addgroup \t{sb.ToString()}");
									SendReply(player, string.Format(GetMessage("Claimed"), (string) entry["ItemName"], ""));
								}
								else
								{
									itemss.AppendFormat("{0}", items[i]);
									amountitem.AppendFormat("{0}", amounts[i]);
									sb.AppendFormat(" {0}\t{1}\t{2}", player.userID, items[i], amounts[i]);
									sb.AppendLine();
									int amou;
									int.TryParse(amounts[i], out amou);
									var definition = ItemManager.FindItemDefinition(items[i]);
									Item item = ItemManager.CreateByItemID(definition.itemid, amou);
									if(!item.MoveToContainer(player.inventory.containerMain))
										item.Drop(player.eyes.position, player.eyes.BodyForward()*2f);
									//player.inventory.GiveItem(ItemManager.CreateByItemID(definition.itemid, amou), pref);
									SendReply(player, string.Format(GetMessage("Claimed"), item.info.displayName.english, item.amount.ToString()));
								}
                            }
                        }
                        else
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine();
                            if (((string) entry["ItemName"]).ToLower() == "evolution" ||
                                ((string) entry["ItemName"]).ToLower() == "donator" ||
                                ((string) entry["ItemName"]).ToLower() == "donator+")
                            {
                                sb.AppendFormat(" {0}\t{1}\t{2}", player.userID, entry["ItemName"], " 30d");
                                sb.AppendLine();
                                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(),
                                    $"addgroup \t{sb.ToString()}");
								SendReply(player, string.Format(GetMessage("Claimed"), (string) entry["ItemName"], ""));
                            }
                            else
                            {
                                itemss.AppendFormat("{0}", entry["ItemName"]);
                                amountitem.AppendFormat("{0}", entry["Amount"]);
                                sb.AppendFormat(" {0}\t{1}\t{2}", player.userID, entry["ItemName"], entry["Amount"]);
                                sb.AppendLine();
								var definition = ItemManager.FindItemDefinition((string) entry["ItemName"]);
 								Item item = ItemManager.CreateByItemID(definition.itemid, (int) entry["Amount"]);
                                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(),
                                    $"inv.giveplayer\t{sb.ToString()}");
                                SendReply(player, string.Format(GetMessage("Claimed"), item.ToString(), entry["Amount"].ToString()));
                            }
                        }
                        var updatesql = Sql.Builder.Append("UPDATE payment SET Taked = 1 where PaymentID = '" + entry["PaymentID"] + "';");
                        _mySql.Update(updatesql, _mySqlConnection);
                    }
                });
            }
            else
            {
                SendReply(player, GetMessage("NoPerm"));
            }
        }

        [ChatCommand("steamfind")]
        private void steamfindCommand(BasePlayer player, string command, string[] args)
        {
			if(player.net?.connection?.authLevel == 0 && !permission.UserHasPermission(player.UserIDString, "claimer.steamfind")){
				SendReply(player, "You don't have permission to do it.");
				return;
			}
            if (args.Length != 1)
            {
                SendReply(player, "Please enter name or partial name.");
                return;
            }
            var targets = FindPlayersOnline(args[0]);
            var target = FindPlayersOffline(args[0]);
            if (targets.Count <= 0 && target.Count <= 0)
            {
                SendReply(player, "No player found.");
                return;
            }
            if (targets.Count >= 1)
            {
				SendReply(player, "=========");
				SendReply(player, "");
                for (int i = 0; i < targets.Count; i++)
                {
					// SendReply(player, string.Format("MultiplePlayers '{0}'", string.Join(", ", targets.ConvertAll(p => p.displayName).ToArray())));
                    SendReply(player, string.Format("Player '{0}' '{1}'", targets[i].displayName, targets[i].UserIDString));
                }
				SendReply(player, "");
				SendReply(player, "=========");
            }
            if (target.Count >= 1)
            {
				SendReply(player, "=========");
				SendReply(player, "");
                for (int i = 0; i < target.Count; i++)
                {
					// SendReply(player, string.Format("MultiplePlayers '{0}'", string.Join(", ", targets.ConvertAll(p => p.displayName).ToArray())));
                    SendReply(player, string.Format("Player '{0}' '{1}'", target[i].displayName, target[i].UserIDString));
                }
				SendReply(player, "");
				SendReply(player, "=========");
            }

        }

        [ChatCommand("transform")]
        private void TransformCommand(BasePlayer player, string command, string[] args)
        {
            var playerid = player.userID;
            if (permission.UserHasPermission(playerid.ToString(), "claimer.cantransform"))
            {
                var item = player.inventory.containerMain.FindItemByItemID(688032252);
                if (item != null)
                {
                    if (args.Length != 0)
                    {
                        int Num;
                        bool isNum = int.TryParse(args[0], out Num);
                        if (isNum)
                        {
                            int metals;
                            int.TryParse(args[0], out metals);
                            int metal = metals;
                            var deleted = item.amount - metal;
                            SendReply(player, String.Format("You transformed {0} metal fragments to {1} metal refined.", metal, (metal / 100)));
                            SendReply(player, String.Format("You got {0} metal fragments left.", deleted));
                            item.RemoveFromContainer();
                            item.Remove(0f);
                            player.inventory.GiveItem(ItemManager.CreateByItemID(374890416, (metals / 100)), player.inventory.containerMain);
                            if (deleted != 0)
                                player.inventory.GiveItem(ItemManager.CreateByItemID(688032252, deleted), player.inventory.containerMain);
                        }
                        else
                        {
                            var metal = item.amount % 100;
                            var deleted = item.amount - metal;
                            SendReply(player, String.Format("You transformed {0} metal fragments to {1} metal refined.", deleted, (item.amount / 100)));
                            SendReply(player, String.Format("You got {0} metal fragments left.", metal));
                            item.RemoveFromContainer();
                            item.Remove(0f);
                            player.inventory.GiveItem(ItemManager.CreateByItemID(374890416, (item.amount / 10)), player.inventory.containerMain);
                            if (metal != 0)
                                player.inventory.GiveItem(ItemManager.CreateByItemID(688032252, metal), player.inventory.containerMain);
                        }
                    }
                    else
                    {
                        var metal = item.amount % 100;
                        var deleted = item.amount - metal;
                        SendReply(player, String.Format("You transformed {0} metal fragments to {1} metal refined.", deleted, (item.amount / 100)));
                        SendReply(player, String.Format("You got {0} metal fragments left.", metal));
                        item.RemoveFromContainer();
                        item.Remove(0f);
                        player.inventory.GiveItem(ItemManager.CreateByItemID(374890416, (item.amount / 100)), player.inventory.containerMain);
                        if (metal != 0)
                            player.inventory.GiveItem(ItemManager.CreateByItemID(688032252, metal), player.inventory.containerMain);
                    }
                }
            }
            else
            {
                SendReply(player, GetMessage("NoPerm"));
            }
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file!");
            Config.Clear();
            Config["address"] = "127.0.0.1";
            Config["port"] = 3306;
            Config["db_name"] = "rust";
            Config["user"] = "root";
            Config["password"] = "";
            SaveConfig();
        }

        #endregion
    }
}