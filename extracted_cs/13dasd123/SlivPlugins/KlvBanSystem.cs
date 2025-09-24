// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("KlvBanSystem", "Sergioklv", "1.1.0")]
    [Description("Plugin for banning and unbanning players by SteamID and IP at the same time")]

    class KlvBanSystem : RustPlugin
    {
		// Changelog: Update 1.1.0 -> Fixed hooks and cleaning some code.
		
		// This plugin is verry simple so please don't hate me
		// Plugin created by: sergioklv
		// Discord: sergioklv
		// If some one want to update this plugin please upload it to discord: TheRustBay link: https://discord.gg/D4ZtKC8dgP
		
		// If you ban one player the IP and STEAMID gonna be added to oxide/data/KlvBanSystemBanSystem.json
		// if player join in the server with banned IP or Steamid player gonna be kicked with ban reason.
		// if player join with banned IP and new/other steam account player gonna be banned and kicked with first ban reason.
		// If player join with banned steamid but not with banned ip player gonna be banned and kicked by steamid and ip with first ban reason.
		
		// If you delete KlvBanSystem.json all players will be unbaned SO BE CAREFUL.
		// The plugin does not include IP unbanning
		// This plugin created because im boring
		// If you have any other idea to add to the plugin write to me.
		// If you see any bugs please contact me and i will update the plugin.
	
		
        private Dictionary<string, BanData> bannedPlayers = new Dictionary<string, BanData>();

        void Init()
        {
            LoadData();
            permission.RegisterPermission("klvbansystem.useplugin", this);
        }

        private void LoadData()
        {
            bannedPlayers = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, BanData>>("KlvBanSystem");
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("KlvBanSystem", bannedPlayers);
        }
        [ChatCommand("ban")]
        void ChatBanCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "klvbansystem.useplugin"))
            {
                SendReply(player, "You don't have permission to use this command.");
                return;
            }

            if (args.Length < 2)
            {
                SendReply(player, "Usage: /ban <PlayerName or SteamID> <Reason>");
                return;
            }

            string targetNameOrSteamID = args[0];
            string reason = string.Join(" ", args, 1, args.Length - 1);
            string targetSteamID = GetPlayerSteamID(targetNameOrSteamID);
            string targetIP = GetPlayerIP(targetNameOrSteamID);
			string playerName = GetPlayerName(targetNameOrSteamID);
            if (string.IsNullOrEmpty(targetSteamID) && string.IsNullOrEmpty(targetIP))
            {
                SendReply(player, $"Player {targetNameOrSteamID} not found.");
                return;
            }

            if (bannedPlayers.ContainsKey(targetSteamID))
            {
                SendReply(player, "Player is already banned.");
                return;
            }

            BanPlayer(targetSteamID, reason, targetIP, playerName);
            SaveData();

            SendReply(player, $"Player {targetNameOrSteamID} has been permanently banned. Reason: {reason}");
            KickBannedPlayer(targetNameOrSteamID, targetSteamID);
        }

        [ConsoleCommand("ban")]
        void ConsoleBanCommand(ConsoleSystem.Arg arg)
        {
			if (!arg.IsAdmin)
			{
				SendReply(arg, "You are not admin");
				return;
			}
            if (arg.Args.Length < 2)
            {
                SendReply(arg, "Usage: ban <PlayerName or SteamID> <Reason>");
                return;
            }

            string targetNameOrSteamID = arg.Args[0];
            string reason = string.Join(" ", arg.Args, 1, arg.Args.Length - 1);

            string targetSteamID = GetPlayerSteamID(targetNameOrSteamID);
            string targetIP = GetPlayerIP(targetNameOrSteamID);
			string playerName = GetPlayerName(targetNameOrSteamID);

            if (string.IsNullOrEmpty(targetSteamID) && string.IsNullOrEmpty(targetIP))
            {
                SendReply(arg, $"Player {targetNameOrSteamID} not found.");
                return;
            }

            if (bannedPlayers.ContainsKey(targetSteamID))
            {
                SendReply(arg, "Player is already banned.");
                return;
            }

            BanPlayer(targetSteamID, reason, targetIP, playerName);
            SaveData();

            SendReply(arg, $"Player {targetNameOrSteamID} has been permanently banned. Reason: {reason}");
            KickBannedPlayer(targetNameOrSteamID, targetSteamID);
        }

		[ChatCommand("unban")]
		void ChatUnbanCommand(BasePlayer player, string command, string[] args)
		{
            if (!permission.UserHasPermission(player.UserIDString, "klvbansystem.useplugin"))
            {
                SendReply(player, "You don't have permission to use this command.");
                return;
            }

			if (args.Length < 1)
			{
				SendReply(player, "Usage: /unban <PlayerName or SteamID> [name|steamid]");
				return;
			}

			string target = args[0];
			bool isName = true;

			if (args.Length > 1)
			{
				if (args[1].ToLower() == "steamid")
				{
					isName = false;
				}
			}

			if (UnbanPlayer(target, isName))
			{
				SendReply(player, $"Player {target} has been unbanned.");
			}
			else
			{
				SendReply(player, $"Player {target} is not banned.");
			}
		}

		[ConsoleCommand("unban")]
		void ConsoleUnbanCommand(ConsoleSystem.Arg arg)
		{
			if (!arg.IsAdmin)
			{
				SendReply(arg, "You are not admin");
				return;
			}
			if (arg.Args.Length < 1)
			{
				SendReply(arg, "Usage: unban <PlayerName or SteamID> [name|steamid]");
				return;
			}

			string target = arg.Args[0];
			bool isName = true;

			if (arg.Args.Length > 1)
			{
				if (arg.Args[1].ToLower() == "steamid")
				{
					isName = false;
				}
			}

			if (UnbanPlayer(target, isName))
			{
				SendReply(arg, $"Player {target} has been unbanned.");
			}
			else
			{
				SendReply(arg, $"Player {target} is not banned.");
			}
		}

		private void BanPlayer(string targetNameOrSteamID, string reason, string targetIP, string targetName)
		{
			DateTime banEndTime = DateTime.MaxValue; // Uisng this to ban permanently

			string targetSteamID = GetPlayerSteamID(targetNameOrSteamID);
			string playerName = GetPlayerName(targetName);

			bannedPlayers[targetSteamID] = new BanData
			{
				BanEndTime = banEndTime,
				Reason = reason,
				IP = targetIP,
				SteamID = targetSteamID,
				PlayerName = playerName
			};
		}
		
		private bool UnbanPlayer(string target, bool isName = true)
		{
			BanData banData = null;

			if (isName)
			{
				banData = bannedPlayers.Values.FirstOrDefault(data => data.PlayerName == target);
			}
			else
			{
				banData = bannedPlayers.Values.FirstOrDefault(data => data.SteamID == target);
			}

			if (banData != null)
			{
				bannedPlayers.Remove(banData.SteamID);
				SaveData();
				return true;
			}

			return false;
		}

        private string GetPlayerIP(string targetName)
        {
            var player = BasePlayer.Find(targetName);
            if (player != null)
            {
                var fullIP = player.net.connection.ipaddress;
                var ip = fullIP.Split(':')[0];
                return ip;
            }
            return null;
        }

        private string GetPlayerSteamID(string targetName)
        {
            var player = BasePlayer.Find(targetName);
            if (player != null)
            {
                return player.UserIDString;
            }
            return null;
        }
		private string GetPlayerName(string targetName)
		{
			var player = BasePlayer.Find(targetName);
			if (player != null)
			{
				return player.displayName;
			}
			return null;
		}
        private void KickBannedPlayer(string targetName, string targetSteamID)
        {
			
			string targetIP = GetPlayerIP(targetName);
			string banReason = GetBanReason(targetIP, targetSteamID);
            var playerToKick = BasePlayer.Find(targetName);
            if (playerToKick != null)
            {
                playerToKick.Kick($"You are banned. Reason: {banReason}");
            }
        }

        void OnPlayerConnected(BasePlayer player)
        {
            string targetName = player.displayName;
            string targetSteamID = player.UserIDString;
            string targetIP = GetPlayerIP(targetName);
            if (IsIPBanned(targetIP) || IsSteamIDBanned(targetSteamID))
            {
                string banReason = GetBanReason(targetIP, targetSteamID);
				rust.RunServerCommand($"ban {targetSteamID}  {banReason}");
				timer.Once(1.7f, () =>
				{
					player.Kick($"You are banned. Reason: {banReason}");
				});		
                Puts($"Player {targetName} with IP {targetIP} and SteamID {targetSteamID} tried to connect but is banned.");
				Puts($"This ip: {targetIP}. is banned! But if player tried to join with diferent account we are banning new acc: {targetSteamID} and {targetIP}");
            }
        }

        private bool IsIPBanned(string targetIP)
        {
            return bannedPlayers.Values.Any(data => data.IP == targetIP);
        }

		private bool IsSteamIDBanned(string targetSteamID)
		{
			return bannedPlayers.Values.Any(data => data.SteamID == targetSteamID);
		}
        private string GetBanReason(string targetIP, string targetSteamID)
        {
            BanData banData = bannedPlayers.Values.FirstOrDefault(data => data.IP == targetIP || data.SteamID == targetSteamID);
            if (banData != null)
            {
                return banData.Reason;
            }
            return "Banned";
        }		

        class BanData
        {
            public DateTime BanEndTime { get; set; }
            public string Reason { get; set; }
            public string IP { get; set; }
            public string SteamID { get; set; }
			public string PlayerName { get; set; }
        }
    }
}
