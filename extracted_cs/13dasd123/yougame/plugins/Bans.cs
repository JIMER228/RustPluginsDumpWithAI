using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Network;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Bans", "wazzzup", "1.0.5")]
    [Description("Bans")]
    internal class Bans : RustPlugin
    {
        public static readonly string token = "vsUGajhkeMfJUo30mj2qrdKLwVlXV0Yu";
        public ConfigData configData;
        private readonly Dictionary<string, string> headers = new Dictionary<string, string>();
        public StoredData storedData;

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Title, storedData);
        }

        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData();
            SaveConfig(config);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        } 

        private void Init()
        {
            configData = Config.ReadObject<ConfigData>();
            SaveConfig(configData);
            try
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Title);
            }
            catch (Exception e)
            {
                storedData = new StoredData();
                PrintWarning($"error {e.Message}");
            }

            headers.Add("User-Agent", "DiscordBot (https://chaoscode.io, 1.0.0");
            headers.Add("Content-Type", "application/json");
            permission.RegisterPermission("bans.allow", this);
        }

        private void OnServerInitialized()
        {
            if (configData.checkPlayersOnStart)
            {
                PrintWarning("Checking activeplayers");
                foreach (var player in BasePlayer.activePlayerList.ToList()) OnClientAuth(player.net.connection);
            }
        }

        private void OnClientAuth(Connection connection)
        {
            if (!configData.useOVHBans)
            {
                if (storedData.bansCache.ContainsKey(connection.userid) &&
                    DateTime.UtcNow > storedData.bansCache[connection.userid])
                    UnBanConnection(connection.userid, false);
                if (connection.ownerid != 0 && storedData.bansCache.ContainsKey(connection.ownerid) &&
                    DateTime.UtcNow > storedData.bansCache[connection.ownerid])
                    UnBanConnection(connection.ownerid, false);
            }

            if (!ServerUsers.Is(connection.userid, ServerUsers.UserGroup.Banned)) GetBans(connection);
        } /*[ConsoleCommand("ban")] void BanCommand(ConsoleSystem.Arg arg) { if (arg?.Player() != null) return; BanProcess(null, arg.Args, false, arg); } [ConsoleCommand("banall")] void BanAllCommand(ConsoleSystem.Arg arg) { if (arg?.Player() != null) return; BanProcess(null, arg.Args, true, arg); } [ConsoleCommand("unban")] void UnBanCommand(ConsoleSystem.Arg arg) { if (arg?.Player() != null) return; UnbanProcess(null, arg.Args, arg); } [ChatCommand("banall")] void cmdBanallCommand(BasePlayer player, string command, string[] args) { BanProcess(player, args, true); } [ChatCommand("ban")] void cmdBanCommand(BasePlayer player, string command, string[] args) { BanProcess(player, args, false); } [ChatCommand("unban")] void cmdUnBanCommand(BasePlayer player, string command, string[] args) { UnbanProcess(player, args); }*/

        [ConsoleCommand("unalert")]
        private void UnAlertCommand(ConsoleSystem.Arg arg)
        {
            if (arg?.Player() != null) return;
            UnAlertProcess(null, arg.Args, arg);
        }

        [ConsoleCommand("alert")]
        private void AlertCommand(ConsoleSystem.Arg arg)
        {
            if (arg?.Player() != null) return;
            AlertProcess(null, arg.Args, arg);
        }

        [ChatCommand("unalert")]
        private void cmdUnAlertCommand(BasePlayer player, string command, string[] args)
        {
            UnAlertProcess(player, args);
        }

        [ChatCommand("alert")]
        private void cmdAlertCommand(BasePlayer player, string command, string[] args)
        {
            AlertProcess(player, args);
        }

        private void UnAlertProcess(BasePlayer player, string[] args, ConsoleSystem.Arg arg = null)
        {
            if (player != null && !permission.UserHasPermission(player.UserIDString, "bans.allow"))
            {
                if (arg != null) SendReply(arg, "no permission");
                else SendReply(player, "no permission");
                return;
            }

            if (args == null || args.Length < 1)
            {
                if (arg != null) SendReply(arg, "no args");
                else SendReply(player, "no args");
                return;
            }

            var id = 0UL;
            if (!ulong.TryParse(args[0], out id))
            {
                if (arg != null) SendReply(arg, $"cant find such player {args[0]}");
                else SendReply(player, $"cant find such player {args[0]}");
                return;
            }

            if (arg != null) SendUnAlert(id, "alert canceled by server console");
            else SendUnAlert(id, $"alert canceled by moder {player.displayName} {player.userID}");
        }

        private void AlertProcess(BasePlayer player, string[] args, ConsoleSystem.Arg arg = null)
        {
            if (player != null && !permission.UserHasPermission(player.UserIDString, "bans.allow"))
            {
                if (arg != null) SendReply(arg, "no permission");
                else SendReply(player, "no permission");
                return;
            }

            if (args == null || args.Length < 1)
            {
                if (arg != null) SendReply(arg, "no args");
                else SendReply(player, "no args");
                return;
            }

            var id = 0UL;
            if (!ulong.TryParse(args[0], out id) || id < 76560000000000000UL)
            {
                if (arg != null) SendReply(arg, $"cant find such player {args[0]}");
                else SendReply(player, $"cant find such player {args[0]}");
                return;
            }

            if (arg != null)
            {
                SendAlert(id, "alert by server console");
                SendReply(arg, "alert added");
            }
            else
            {
                SendAlert(id, $"alert by moder {player.displayName} {player.userID}");
                SendReply(player, "alert added");
            }
        }

        private void UnbanProcess(BasePlayer player, string[] args, ConsoleSystem.Arg arg = null)
        {
            if (player != null && !permission.UserHasPermission(player.UserIDString, "bans.allow"))
            {
                if (arg != null) SendReply(arg, "no permission");
                else SendReply(player, "no permission");
                return;
            }

            if (args == null || args.Length < 1)
            {
                if (arg != null) SendReply(arg, "no args");
                else SendReply(player, "no args");
                return;
            }

            var id = 0UL;
            ulong.TryParse(args[0], out id);
            var user = ServerUsers.Get(id); 
            if (user == null)
            {
                if (arg != null) SendReply(arg, $"cant find in online and offline players {args[0]}");
                else SendReply(player, $"cant find in online and offline players {args[0]}");
                return;
            }

            if (user.group != ServerUsers.UserGroup.Banned)
            {
                if (arg != null) SendReply(arg, $"User {user.username} {user.steamid} is not banned");
                else SendReply(player, $"User {user.username} {user.steamid} is not banned");
                return;
            }

            UnBanConnection(user.steamid);
            if (arg != null) SendReply(arg, $"User {user.username} {user.steamid} unbanned");
            else SendReply(player, $"User {user.username} {user.steamid} unbanned");
        }

        public BasePlayer FindPlayer(string strNameOrID)
        {
            var player = BasePlayer.activePlayerList.ToList().Find(x =>
                x.UserIDString == strNameOrID || x.displayName.Contains(strNameOrID, CompareOptions.OrdinalIgnoreCase));
            if (player == null)
                player = BasePlayer.sleepingPlayerList.ToList().Find(x =>
                    x.UserIDString == strNameOrID ||
                    x.displayName.Contains(strNameOrID, CompareOptions.OrdinalIgnoreCase));
            return player;
        }

        private void BanProcess(BasePlayer player, string[] args, bool banAll = false, ConsoleSystem.Arg arg = null)
        {
            if (player != null && !permission.UserHasPermission(player.UserIDString, "bans.allow"))
            {
                if (arg != null) SendReply(arg, "no permission");
                else SendReply(player, "no permission");
                return;
            }

            if (args == null || args.Length < 2)
            {
                if (arg != null) SendReply(arg, "no args");
                else SendReply(player, "no args");
                return;
            }

            var reason = args[1]; /*for (int i = 2; i < args.Length; i++) { reason += args.Args[i]; reason += " "; }*/
            double endDate = 0;
            if (args.Length > 2)
                if (!TryGetDateTime(args[2], out endDate))
                {
                    if (arg != null) SendReply(arg, "Invalid Time Format");
                    else SendReply(player, "Invalid Time Format");
                    return;
                }

            var target = FindPlayer(args[0]);
            if (target != null)
            {
                if (target.net?.connection != null)
                    BanConnection(target.net.connection, target.displayName, target.userID,
                        target.net.connection.ownerid, IpAddress(target.net.connection.ipaddress), reason, true,
                        endDate, banAll);
                else BanConnection(null, target.displayName, target.userID, 0, "", reason, true, endDate, banAll);
            }
            else
            {
                var id = 0UL;
                ulong.TryParse(args[0], out id);
                var user = ServerUsers.Get(id);
                if (user != null && user.group == ServerUsers.UserGroup.Banned)
                {
                    if (arg != null) SendReply(arg, $"User {user.username} {user.steamid} is already banned");
                    else SendReply(player, $"User {user.username} {user.steamid} is already banned");
                    return;
                }

                var covPlayer = covalence.Players.FindPlayer(args[0]);
                var username = covPlayer != null ? covPlayer.Name : "unnamed";
                BanConnection(null, username, id, 0, "", reason, true, endDate, banAll);
            }
        }

        private static string IpAddress(string ip)
        {
            return Regex.Replace(ip, @":{1}[0-9]{1}\d*", "");
        }

        private void GetBans(Connection connection)
        {
            try
            {
                var username = connection.username;
                var userid = connection.userid;
                var ownerid = connection.ownerid;
                var ip = IpAddress(connection.ipaddress);
                var request = $"token={token}&action=getbans&userid={userid}&ip={ip}";
                webrequest.EnqueuePost(configData.apiUrl, request,
                    (code, response) => ProcessRequest(code, response, connection, username, userid, ownerid, ip),
                    this);
            }
            catch (Exception e)
            {
                PrintWarning($"GetBans exception {e}");
            }
        }

        private void ProcessRequest(int code, string response, Connection connection, string username, ulong userid,
            ulong ownerid, string ip)
        {
            if (code != 200)
            {
                PrintWarning($"GetBans {code}: {response}");
                return;
            }

            var json = JObject.Parse(response);
            if (connection != null && json["ban"] != null)
                BanConnection(connection, username, userid, ownerid, ip, json["reason"].ToString(), false);
            if (json["alert"] != null) AlertConnection(username, userid, ip, json["reason"].ToString());
        } /*hook for OVH*/

        private void OnBanSystemUnban(ulong userid, ulong initiator)
        {
            if (configData.debug) LogToFile("debug", $"[{DateTime.Now}] unban {userid} {initiator}", this);
            var sendDb = true;
            if (storedData.bansCache.ContainsKey(userid))
            {
                storedData.bansCache.Remove(userid);
                SaveData();
            }

            if (sendDb)
            {
                var request = $"token={token}&action=unban&userid={userid}";
                webrequest.EnqueuePost(configData.apiUrl, request, (code, response) => { }, this);
            }
        }

        private void UnBanConnection(ulong userid, bool sendDb = true)
        {
            ServerUsers.Remove(userid);
            ServerUsers.Save();
            if (storedData.bansCache.ContainsKey(userid))
            {
                storedData.bansCache.Remove(userid);
                SaveData();
            }

            if (sendDb)
            {
                var request = $"token={token}&action=unban&userid={userid}";
                webrequest.EnqueuePost(configData.apiUrl, request, (code, response) => { }, this);
            }
        } /*hook for OVH*/

        private void OnBanSystemBan(ulong userid, ulong ownerid, string reason, uint time, ulong initiator,
            int singer)
        {
            if (configData.debug)
                LogToFile("debug", $"[{DateTime.Now}] ban {userid} {ownerid} {reason} {time} {initiator} {singer}", 
                    this);
            var banAll = singer == 0;
            var newBan = true;
            var username = "";
            var player = FindPlayer(userid.ToString());
            if (player != null) username = player.displayName;
            var utcNow = DateTime.UtcNow.AddSeconds(time);
            var now = DateTime.Now.AddSeconds(time);
            if (time > 0) storedData.bansCache.Add(userid, utcNow);
            if (newBan) SendBan(username, userid, reason, time, banAll);
            if (configData.bansChannel != "") SendBanDiscord(username, userid, reason);
            if (ownerid != 0 && ownerid != userid)
            {
                if (time > 0) storedData.bansCache.Add(ownerid, utcNow);
                if (newBan) SendBan(username, ownerid, $"Family share {userid}", time, banAll);
                if (configData.bansChannel != "") SendBanDiscord(username, ownerid, $"Family share {userid}");
            }

            if (time > 0) SaveData();
        }

        private void BanConnection(Connection connection, string username, ulong userid, ulong ownerid, string ip,
            string reason, bool newBan = true, double time = 0, bool banAll = false)
        {
            ServerUsers.Set(userid, ServerUsers.UserGroup.Banned, username, reason);
            var utcNow = DateTime.UtcNow.AddSeconds(time);
            var now = DateTime.Now.AddSeconds(time);
            if (time > 0) storedData.bansCache.Add(userid, utcNow);
            if (newBan) SendBan(username, userid, reason, time, banAll);
            if (configData.bansChannel != "") SendBanDiscord(username, userid, reason);
            if (ownerid != 0 && ownerid != userid)
            {
                ServerUsers.Set(ownerid, ServerUsers.UserGroup.Banned, username, $"Family share {userid}");
                if (time > 0) storedData.bansCache.Add(ownerid, utcNow);
                if (newBan) SendBan(username, ownerid, $"Family share {userid}", time, banAll);
                if (configData.bansChannel != "") SendBanDiscord(username, ownerid, $"Family share {userid}");
            }

            ServerUsers.Save();
            if (time > 0) SaveData();
            ConsoleNetwork.BroadcastToAllClients("chat.add", 0,
                string.Format(configData.banMessage, username, reason, now.ToString("dd.MM.yyyy HH:mm")));
            if (connection != null) Net.sv.Kick(connection, $"Banned: {reason} до {now.ToString("dd.MM.yyyy HH:mm")}");
        }

        private void SendBanDiscord(string username, ulong userid, string reason)
        {
            var message = $"```fix\\n{username} ({userid}) забанен: {reason}```";
            var json = "{" + string.Format("\"username\": \"{0}\", \"content\": \"{1}\"", configData.serverName,
                           message) + "}";
            webrequest.Enqueue(configData.bansChannel, json, (code, response) =>
            {
                if (code != 200 && code != 204) PrintWarning($"{code}: {response}");
            }, this, RequestMethod.POST, headers);
        }

        private void SendBan(string username, ulong userid, string reason, double time, bool banAll = false)
        {
            var request =
                $"token={token}&action=newban&server={configData.serverName}&userid={userid}&username={username}&reason={reason}&time={time}";
            if (banAll) request += "&banall=1";
            webrequest.EnqueuePost(configData.apiUrl, request, (code, response) => { }, this);
        }

        private void SendAlert(ulong userid, string reason)
        {
            var request = $"token={token}&action=newalert&userid={userid}&reason={reason}";
            webrequest.EnqueuePost(configData.apiUrl, request, (code, response) => { }, this);
        }

        private void SendUnAlert(ulong userid, string reason)
        {
            var request = $"token={token}&action=unalert&userid={userid}";
            webrequest.EnqueuePost(configData.apiUrl, request, (code, response) => { }, this);
        }

        private void AlertConnection(string username, ulong userid, string ip, string reason = "")
        {
            if (configData.alertChannel == "") return;
            var message = $"@everyone Сервер #{configData.serverId}\\n Зашел игрок {username} ({userid}) / ip {ip}";
            if (reason != "") message += $"\\nпричина: {reason}";
            var json = "{" + string.Format("\"username\": \"{0}\", \"content\": \"{1}\"", configData.serverName,
                           message) + "}";
            webrequest.Enqueue(configData.alertChannel, json, (code, response) =>
            {
                if (code != 200 && code != 204) PrintWarning($"{code}: {response}");
            }, this, RequestMethod.POST, headers);
        }

        private bool TryGetDateTime(string source, out double date)
        {
            int minutes = 0, hours = 0, days = 0;
            var m = new Regex(@"(\d+?)m", RegexOptions.IgnoreCase).Match(source);
            var h = new Regex(@"(\d+?)h", RegexOptions.IgnoreCase).Match(source);
            var d = new Regex(@"(\d+?)d", RegexOptions.IgnoreCase).Match(source);
            if (m.Success) minutes = Convert.ToInt32(m.Groups[1].ToString());
            if (h.Success) hours = Convert.ToInt32(h.Groups[1].ToString());
            if (d.Success) days = Convert.ToInt32(d.Groups[1].ToString());
            source = source.Replace(minutes + "m", string.Empty);
            source = source.Replace(hours + "h", string.Empty);
            source = source.Replace(days + "d", string.Empty);
            if (!string.IsNullOrEmpty(source) || !m.Success && !h.Success && !d.Success)
            {
                date = 0;
                return false;
            }

            date = new TimeSpan(days, hours, minutes, 0).TotalSeconds;
            return true;
        }

        public class StoredData
        {
            public Dictionary<ulong, DateTime> bansCache = new Dictionary<ulong, DateTime>();
        }

        public class ConfigData
        {
            public string alertChannel = "";
            public string apiUrl = "http://bans.bloodrust.ru/bans.php";
            public string banMessage = "SERVER Kickbanning {0} ({1} {2})";
            public string bansChannel = "";
            public bool checkPlayersOnStart = false;
            public bool debug = true;
            public int serverId = 0;
            public string serverName = "";
            public bool useOVHBans = true;
        }
    }
}