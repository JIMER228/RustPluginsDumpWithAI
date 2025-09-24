using System.Collections.Generic;
using Oxide.Core;
using Oxide.Plugins;
using Newtonsoft.Json;
using System.Linq;
using ConVar;
using UnityEngine;
using Oxide.Core.Plugins;
using System;
using Oxide.Game.Rust.Libraries;
using ProtoBuf;
using Oxide.Core.Libraries;



namespace Oxide.Plugins
{
    [Info("AutoBan", "Grejory", "0.1.9")]
    [Description("Bans users based on reports and reasons.")]
    class AutoBan : RustPlugin
    {
        [PluginReference] Plugin DiscordMessages, DiscordHooks;
        private ConfigData configData;
        class ConfigData
        {
            // Max number of allowed reports per user (set in config)
            [JsonProperty(PropertyName = "Max reports till user gets banned")]
            public int maxReports;

            [JsonProperty(PropertyName = "Reason given to banned user")]
            public string reportReason;

            // Valid report reasons (set in config)
            [JsonProperty(PropertyName = "Valid reasons *searches subject title and message of the report* ( To make sure report is scanned, add different variations of reason; like cheat, cheater, cheating, cheats, etc. )")]
            public List<string> validReasons = new List<string>();

            // Valid report types (set in config)
            [JsonProperty(PropertyName = "Valid report types")]
            public List<string> validTypes = new List<string>();

            [JsonProperty(PropertyName = "Scan type of F7 reports")]
            public bool scanTypeOfReport;

            [JsonProperty(PropertyName = "Broadcast ban to chat")]
            public bool broadcastBan;

            [JsonProperty(PropertyName = "Ignored Players *Steam ID's Only*")]
            public List<string> ignoredPlayers = new List<string>();

            [JsonProperty(PropertyName = "Timer for broadcasting bans to admins and console")]
            public int broadcastTimer;

            [JsonProperty(PropertyName = "Send bans to users with the AutoBan.viewbans permission based on timer ( In-Game )")]
            public bool sendBans;

            [JsonProperty(PropertyName = "Send reports to URL")]
            public bool sendToUrl;
            
            [JsonProperty(PropertyName = "URL to send reports to")]
            public string url;

            [JsonProperty(PropertyName = "Send reports to Discord")]
            public bool sendToDiscord;

            [JsonProperty(PropertyName = "Discord Webhook URL")]
            public string discordUrl;
            
        }

        StoredData storedData;
        class StoredData
        {
            public Dictionary<string, List<string>> reportedUsers = new Dictionary<string, List<string>>();
            public List<string> reports = new List<string>();
            public List<string> bans = new List<string>();
        }

        private bool LoadConfigVariables()
        {
            try
            {
                configData = Config.ReadObject<ConfigData>();
            }
            catch
            {
                return false;
            }
            SaveConfig(configData);
            return true;
        }


        void Init()
        {
            permission.RegisterPermission("AutoBan.report", this);
            permission.RegisterPermission("AutoBan.ignore", this);
            permission.RegisterPermission("AutoBan.viewreports", this);
            permission.RegisterPermission("AutoBan.ban", this);
            permission.RegisterPermission("AutoBan.unban", this);
            permission.RegisterPermission("AutoBan.viewbans", this);

            if (!LoadConfigVariables())
            {
                Puts("Config file issue detected. Please delete file, or check syntax and fix.");
                return;
            }
        }
        
        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            configData = new ConfigData();
            // Set default config values
            configData.maxReports = 5;
            configData.reportReason = "You have been reported too many times, and have been banned for precautionary measures, an admin will review this suspension soon.";
            configData.validReasons = new List<string>() { "cheating", "aimbot", "hacking", "esp", "teaming", "racism", "griefing", "walling", "doorcamping", "spawn killing" };
            configData.validTypes = new List<string>() { "cheat", "abusive", "name", "spam" };
            configData.broadcastTimer = 820;
            configData.scanTypeOfReport = true;
            configData.broadcastBan = true;
            configData.ignoredPlayers = new List<string>() { "76561198000000000" };
            configData.sendBans = false;
            configData.sendToUrl = false;
            configData.url = "http://example.com";
            configData.sendToDiscord = false;
            configData.discordUrl = "http://example.com";
            SaveConfig(configData);
        }

        void SaveConfig(ConfigData config)
        {

            Config.WriteObject(config, true);

        }
        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("AutoBan", storedData);
        }

        void Loaded()
        {
            checkingBans();
        }

        // make a function that i can include in other functions that will send the report to a url
        void sendReport(string report)
        {
            if (configData.sendToUrl)
            {
                webrequest.Enqueue(configData.url, report, (code, response) =>
                {
                    if (code != 200 || response == null)
                    {
                        Puts("Error sending report to URL");
                        return;
                    }
                    Puts($"Report sent to {configData.url} with response of {response}");
                }, this, RequestMethod.POST);
            }
        }

        void discordSend(string message) {
            if (configData.sendToDiscord) {
                SendMessage($"**AutoBan Alert:** *{message}*", configData.discordUrl);
            }
        }

        void OnPlayerReported(BasePlayer reporter, string targetName, string targetId, string subject, string message, string type)
        {
            bool isReportValid = false;
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("AutoBan");
            if (permission.UserHasPermission(reporter.UserIDString, "AutoBan.report"))
            {
                // Check if the report reason is valid
                for (int i = 0; i < configData.validReasons.Count; i++)
                {
                    if ((subject.ToUpper().Contains(configData.validReasons[i].ToUpper()) || message.ToUpper().Contains(configData.validReasons[i].ToUpper())) || (configData.scanTypeOfReport && type.ToUpper().Contains(configData.validReasons[i].ToUpper())))
                    {

                        if (!configData.ignoredPlayers.Contains(targetId))
                        {

                            // Add the reported user and the reporting user to the dictionary
                            if (storedData.reportedUsers.ContainsKey(targetId))
                            {
                                if (!storedData.reportedUsers[targetId].Contains(reporter.UserIDString))
                                {
                                    storedData.reportedUsers[targetId].Add(reporter.UserIDString);
                                    Puts($"AutoBan: report added, {reporter.UserIDString} reported {targetId} for: {subject} : {message}");
                                }
                                else
                                {
                                    Puts($"AutoBan: {reporter.UserIDString} already reported {targetId}");
                                    return;
                                }
                            }
                            else
                            {
                                storedData.reportedUsers.Add(targetId, new List<string>() { reporter.UserIDString });
                                Puts($"AutoBan: new report added, {reporter.UserIDString} reported {targetId} for: {subject} : {message}");
                            }


                            // Check if the reported user has reached the max number of allowed reports
                            if (storedData.reportedUsers[targetId].Count >= configData.maxReports)
                            {
                                if (configData.broadcastBan)
                                {
                                    // send a message to everyone saying user was banned for being reported too many times to chat
                                    Chat.Broadcast($"AutoBan is banning {targetName} for being reported too many times. An admin will review this ban shortly.");
                                }


                                // Ban the reported user
                                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"ban {targetId} \"{configData.reportReason}\"");
                                storedData.bans.Add($"{targetName} / {targetId} was banned for {configData.reportReason}");
                                discordSend($"{targetName} / {targetId} was banned for {configData.reportReason}");
                                SaveData();
                            }
                            storedData.reports.Add($"{reporter.displayName} / {reporter.UserIDString} reported {targetName} / {targetId} for: {subject} : {message}");
                            discordSend($"{reporter.displayName} / {reporter.UserIDString} reported {targetName} / {targetId} for: {subject} : {message}");
                            isReportValid = true;
                            SaveData();
                            sendReport($"AutoBan: report added, {reporter.UserIDString} reported {targetId} for: {subject} : {message}");
                            break;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                if (!isReportValid)
                {
                    Puts($"AutoBan: No valid reason found in report: {subject} : {message}");
                }
                return;


            }
            else
            {
                // does nothing if user doesnt have permission
                return;
            }
        }

        [ChatCommand("report")]
        private void report(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "AutoBan.report"))
            {
                if (args.Length == 0)
                {
                    SendReply(player, "Usage: /report <playername> <reason>");
                    return;
                }
                if (args.Length == 1)
                {
                    SendReply(player, "Usage: /report <playername> <reason>");
                    return;
                }
                if (args.Length == 2)
                {
                    storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("AutoBan");
                    string playerName = args[0];
                    int includedReasons = 0;
                    playerName.Replace("\"", "");
                    string reason = args[1];
                    reason.Replace("\"", "");
                    for (int h = 0; h < configData.validReasons.Count(); h++)
                    {
                        if (reason.ToUpper().Contains(configData.validReasons[h].ToUpper()))
                        {
                            includedReasons++;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    try
                    {
                        BasePlayer target = BasePlayer.Find(playerName);
                        if (target == player)
                        {
                            SendReply(player, "You can't report yourself.");
                            return;
                        }
                        if (configData.ignoredPlayers.Contains(target.UserIDString))
                        {
                            SendReply(player, "This player is ignored.");
                            return;
                        }
                        if (includedReasons == 0)
                        {
                            SendReply(player, "Report will not go thru AutoBans detection, an admin will still see this report.");
                            storedData.reports.Add($"{player.displayName} / {player.UserIDString} reported {target.displayName} / {target.UserIDString} for {reason}");
                            discordSend($"{player.displayName} / {player.UserIDString} reported {target.displayName} / {target.UserIDString} for {reason}");
                            SaveData();
                            sendReport($"AutoBan: report added, {player.UserIDString} reported {target.UserIDString} for: {reason}");
                            return;
                        }
                        if (storedData.reportedUsers.ContainsKey(target.UserIDString))
                        {
                            if (storedData.reportedUsers[target.UserIDString].Contains(player.UserIDString))
                            {
                                SendReply(player, "You have already reported this player.");
                                return;
                            }
                            else
                            {

                                storedData.reportedUsers[target.UserIDString].Add(player.UserIDString);
                                discordSend($"{player.displayName} / {player.UserIDString} reported {target.displayName} / {target.UserIDString} for {reason}");
                                storedData.reports.Add($"{player.displayName} / {player.UserIDString} reported {target.displayName} / {target.UserIDString} for: {reason}");
                                SaveData();
                                sendReport($"AutoBan: report added, {player.UserIDString} reported {target.UserIDString} for: {reason}");
                                SendReply(player, "You have reported " + target.displayName + " for " + reason);
                                if (storedData.reportedUsers[target.UserIDString].Count >= configData.maxReports)
                                {
                                    ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"ban {target.userID} \"{configData.reportReason}\"");
                                    storedData.bans.Add($"{target.displayName} / {target.UserIDString} was banned for {reason}");
                                    discordSend($"{target.displayName} / {target.UserIDString} was banned for {reason}");
                                    SaveData();
                                }
                                else
                                {
                                    SendReply(player, "This player has " + storedData.reportedUsers[target.UserIDString].Count + " reports.");
                                }
                            }
                        }
                        else
                        {
                            storedData.reportedUsers.Add(target.UserIDString, new List<string>() { player.UserIDString });
                            storedData.reports.Add($"{player.displayName} / {player.UserIDString} reported {target.displayName} / {target.UserIDString} for: {reason}");
                            discordSend($"{player.displayName} / {player.UserIDString} reported {target.displayName} / {target.UserIDString} for: {reason}");
                            SaveData();
                            sendReport($"AutoBan: report added, {player.UserIDString} reported {target.UserIDString} for: {reason}");
                            SendReply(player, "You have reported " + target.displayName + " for " + reason);
                            if (storedData.reportedUsers[target.UserIDString].Count >= configData.maxReports)
                            {
                                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"ban {target.userID} \"{configData.reportReason}\"");
                                storedData.bans.Add($"{target.displayName} / {target.UserIDString} was banned for {configData.reportReason}");
                                discordSend($"{target.displayName} / {target.UserIDString} was banned for {configData.reportReason}");
                                SaveData();
                            }
                            else
                            {
                                SendReply(player, "This player has " + storedData.reportedUsers[target.UserIDString].Count + " reports.");
                            }
                        }
                    }
                    catch
                    {
                        if (playerName.Contains("7656119") && playerName.Length >= 17)
                        {
                            string targetID = playerName;
                            
                            if (includedReasons == 0)
                            {
                                SendReply(player, "Report will not go thru AutoBans detection, an admin will still see this report.");
                                storedData.reports.Add($"{player.displayName} / {player.UserIDString} reported {targetID} for {reason}");
                                discordSend($"{player.displayName} / {player.UserIDString} reported {targetID} for {reason}");
                                SaveData();
                                sendReport($"AutoBan: report added, {player.UserIDString} reported {targetID} for: {reason}");
                                return;
                            }

                            if (storedData.reportedUsers.ContainsKey(targetID))
                            {
                                if (storedData.reportedUsers[targetID].Contains(player.UserIDString))
                                {
                                    SendReply(player, "You have already reported this player.");
                                    return;
                                }
                                else
                                {

                                    storedData.reportedUsers[targetID].Add(player.UserIDString);
                                    discordSend($"{player.displayName} / {player.UserIDString} reported {targetID} for {reason}");
                                    storedData.reports.Add($"{player.displayName} / {player.UserIDString} reported {targetID} for: {reason}");
                                    SaveData();
                                    sendReport($"AutoBan: report added, {player.UserIDString} reported {targetID} for: {reason}");
                                    SendReply(player, "You have reported " + targetID + " for " + reason);
                                    if (storedData.reportedUsers[targetID].Count >= configData.maxReports)
                                    {
                                        ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"ban {targetID} \"{configData.reportReason}\"");
                                        storedData.bans.Add($"{targetID} was banned for {reason}");
                                        discordSend($"{targetID} was banned for {reason}");
                                        SaveData();
                                    }
                                    else
                                    {
                                        SendReply(player, "This player has " + storedData.reportedUsers[targetID].Count + " reports.");
                                    }
                                }
                            }
                            else
                            {
                                storedData.reportedUsers.Add(targetID, new List<string>() { player.UserIDString });
                                storedData.reports.Add($"{player.displayName} / {player.UserIDString} reported {targetID} for: {reason}");
                                discordSend($"{player.displayName} / {player.UserIDString} reported {targetID} for: {reason}");
                                SaveData();
                                sendReport($"AutoBan: report added, {player.UserIDString} reported {targetID} for: {reason}");
                                SendReply(player, "You have reported " + targetID + " for " + reason);
                                if (storedData.reportedUsers[targetID].Count >= configData.maxReports)
                                {
                                    ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"ban {targetID} \"{configData.reportReason}\"");
                                    storedData.bans.Add($"{targetID} was banned for {configData.reportReason}");
                                    discordSend($"{targetID} was banned for {configData.reportReason}");
                                    SaveData();
                                }
                                else
                                {
                                    SendReply(player, "This player has " + storedData.reportedUsers[targetID].Count + " reports.");
                                }
                            }
                            return;
                        }
                        else
                        {
                            SendReply(player, "Player is not online, please use their SteamID64");
                            return;
                        }
                    }
                }
                else
                {
                    SendReply(player, "Wrap the username and reason in \"\" if it is 2 or more words. ");
                    return;
                }
            }
            else
            {
                SendReply(player, "You do not have permission to use this command.");
            }
        }

        [ConsoleCommand("getreports")]
        private void getreports()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("AutoBan");
            Puts("AutoBan: Viewing reports");
            for (int i = 0; i < storedData.reports.Count; i++)
            {
                Puts(storedData.reports[i]);
            }
        }

        [ConsoleCommand("getbans")]
        private void getbans()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("AutoBan");
            Puts("AutoBan: Viewing bans");
            for (int i = 0; i < storedData.bans.Count; i++)
            {
                Puts(storedData.bans[i]);
            }
        }

        private void checkingBans()
        {
            // make a timer for every 10 minutes
            timer.Repeat(configData.broadcastTimer, 0, () =>
                {
                    storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("AutoBan");
                    Puts("AutoBan: Viewing bans");
                    if (storedData.bans.Count == 0)
                    {
                        Puts("AutoBan: No bans found");
                        return;
                    }
                    for (int i = 0; i < storedData.bans.Count; i++)
                    {
                        Puts(storedData.bans[i]);
                    }
                    // get all users who have autoban.ban permission and send them current bans

                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        if (storedData.bans.Count == 0)
                        {
                            return;
                        }
                        if (permission.UserHasPermission(player.UserIDString, "AutoBan.viewbans") && configData.sendBans == true)
                        {
                            SendReply(player, "AutoBan: Viewing bans");
                            for (int i = 0; i < storedData.bans.Count; i++)
                            {
                                SendReply(player, storedData.bans[i]);
                            }
                        }
                    }
                });
        }

        [ChatCommand("viewreports")]
        private void viewreports(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "AutoBan.viewreports"))
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("AutoBan");
                SendReply(player, "Here are the current reports:");
                for (int i = 0; i < storedData.reports.Count; i++)
                {
                    SendReply(player, storedData.reports[i]);
                }


            }
        }

        [ChatCommand("showbans")]
        private void showbans(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "AutoBan.viewbans"))
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("AutoBan");
                SendReply(player, "Here are the current bans:");
                for (int i = 0; i < storedData.bans.Count; i++)
                {
                    SendReply(player, storedData.bans[i]);
                }
            }
        }

        [ChatCommand("unban")]
        private void unban(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "AutoBan.unban"))
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("AutoBan");
                if (args.Length == 0)
                {
                    SendReply(player, "Usage: /removeban <playername or ID>");
                    return;
                }
                string playerID = args[0];
                BasePlayer target = BasePlayer.Find(playerID);
                if (target == null)
                {
                    SendReply(player, "Player not found.");
                    return;
                }
                for (int i = 0; i < storedData.bans.Count; i++)
                {
                    if (storedData.bans[i].Contains(target.UserIDString))
                    {
                        storedData.bans.RemoveAt(i);
                        SaveData();
                        ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"unban {target.userID}");
                        SendReply(player, "unbanned " + target.UserIDString);
                        return;
                    }
                }
                SendReply(player, "Player not found.");
                return;

            }
        }

        [ChatCommand("ban")]
        private void ban(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "AutoBan.ban"))
            {

                if (args.Length == 0)
                {
                    SendReply(player, "Usage: /ban <playerId> <reason>");
                    return;
                }
                if (args.Length == 1)
                {
                    SendReply(player, "Usage: /ban <playerId> <reason>");
                    return;
                }
                if (args.Length == 2)
                {
                    string targetId = args[0];
                    string reason = args[1];
                    reason.Replace("\"", "");
                    try
                    {
                        BasePlayer target = BasePlayer.FindByID(Convert.ToUInt64(targetId));
                        if (target == null)
                        {
                            SendReply(player, "Player not found.");
                            return;
                        }
                        if (configData.ignoredPlayers.Contains(targetId))
                        {
                            SendReply(player, "This player is on the ignore list and cannot be banned.");
                            return;
                        }
                        if (permission.UserHasPermission(targetId, "AutoBan.ignore"))
                        {
                            SendReply(player, "This player has the ignore permission and cannot be banned.");
                            return;
                        }
                        ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"ban {targetId} \"{reason + " - " + player.displayName}\"");
                        SendReply(player, "Player " + target.displayName + " has been banned for: " + reason);
                        storedData.bans.Add($"{target.displayName} / {target.UserIDString} was banned for {reason}");
                        discordSend($"{target.displayName} / {target.UserIDString} was banned for {reason}");
                        SaveData();
                    } 
                    catch {
                        SendReply(player, "Player not found, make sure to use their SteamID64.");
                        return;
                    }
                }
                else
                {
                    SendReply(player, "Usage: /ban <playerId> <reason>");
                    return;
                }
            }
            else
            {
                SendReply(player, "You do not have permission to use this command.");
            }
        }

        // DiscordHooks plugin off of UMOD, created bt ctx, this is an updated version since his config does not work. 

        void SendMessage(string MessageText, string URL)
        {
            string payloadJson = JsonConvert.SerializeObject(new DiscordPayload()
            {
                MessageText = MessageText
            });

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");

            webrequest.EnqueuePost(URL, payloadJson, (code, response) => PostCallBack(code, response), this, headers);
            //webrequest.EnqueuePost(UrlWithAccessToken, payloadJson, (code, response) => PostCallBack(code, response), this);
        }



        void PostCallBack(int code, string response)
        {
            if (code != 200)
            {
                PrintWarning(String.Format("Discord Api responded with {0}: {1}", code, response));
            }
        }

        class DiscordPayload
        {
            [JsonProperty("content")]
            public string MessageText { get; set; }
        }

        //private void ConsoleLog(string condition, string stackTrace, LogType type)
        //{
        //    if (!string.IsNullOrEmpty(condition))
        //    {
        //        SendMessage($"[LOG] {condition}");
        //    }
        //}
    }
}


