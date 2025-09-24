using UnityEngine;
using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Rust;

namespace Oxide.Plugins {
	[Info("RustExtras, Items", "WeeDzCokie", "0.1.0")]
	[Description("Give yourself some nice items!")]

	class RustExtrasItems : RustPlugin {
		
		// Permission strings
		private const string PermGiveItem = "rustextrasitems.give";
		private const string PermRemoveItem = "rustextrasitems.remove";
		private const string PermResetItem = "rustextrasitems.reset";

		private void Loaded() {
			permission.RegisterPermission(PermGiveItem, this);
			permission.RegisterPermission(PermRemoveItem, this);
			permission.RegisterPermission(PermResetItem, this);
		}
		
		private bool hasPermission(BasePlayer player, string perm) {
			return isAdmin(player) || permission.UserHasPermission(player.UserIDString, perm);
		}
		private static bool isAdmin(BasePlayer player) {
			if (player?.net?.connection == null) {
				return true;
			}
			return player.net.connection.authLevel > 0;
		}
		
		//
		
		private void giveItem(BasePlayer player, Item item, int amount) {
			int stacks = (int)Math.Ceiling((double)amount / (double)item.info.stackable);
			ItemDefinition itemd = ItemManager.FindItemDefinition(item.info.itemid);
			for (int i = 0; i < stacks; i++) {
				item = ItemManager.Create(itemd, (amount > item.info.stackable) ? item.info.stackable : amount);
				amount -= item.info.stackable;
				player.inventory.GiveItem(item);
			}
		}
		
		//
		
		/*
			ChatCommands
		*/
		[ChatCommand("rei")]
		void cHelp(BasePlayer player, string cmd, string[] args) {
			player.ChatMessage("<color=yellow>RustExtras, Items [" + Version + "]</color>\n" +
			"Command list:\ngiveitem <player> <item> [amount]\n" +
			"removeitem <player> <item> [amount]\n" +
			"resetitems <player>");
		}
		[ChatCommand("rei.give")]
		void cGiveItem(BasePlayer playerCall, string cmd, string[] args) {
			if (!hasPermission(playerCall, PermGiveItem)) {
				playerCall.ChatMessage("No permission");
				return;
			}
			if (args.Length < 2) {
				return;
			}
			var player = BasePlayer.Find(args[0]);
			if (player == null) {
				playerCall.ChatMessage("Player not found");
				return;
			}
			int amount = 1;
			int.TryParse(args[2], out amount);
			Item item = ItemManager.CreateByName(args[1]);
			if (item != null) {
				giveItem(player, item, amount);
				player.ChatMessage("You recieved " + amount + " " + item.info.displayName.translated);
			}
		}
		
		[ChatCommand("rei.remove")]
		void cRemoveItem(BasePlayer playerCall, string cmd, string[] args) {
			if (!hasPermission(playerCall, PermGiveItem)) {
				playerCall.ChatMessage("No permission");
				return;
			}
			if (args.Length < 2) {
				return;
			}
			var player = BasePlayer.Find(args[0]);
			if (player == null) {
				playerCall.ChatMessage("Player not found");
				return;
			}
			int amount = 1;
			int.TryParse(args[2], out amount);
			Item item = ItemManager.CreateByName(args[1]);
			if (item != null) {
				player.inventory.Take(null, item.info.itemid, amount);
				player.ChatMessage("You lost " + amount + " " + item.info.displayName.translated);
			}
		}
		
		/*
			ConsoleCommands
		*/
		[ConsoleCommand("rei.give")]
		void ccGiveItem(ConsoleSystem.Arg arg) {
			var playerCall = arg.Player();
			if (!playerCall) {
				return;
			}
			if (!hasPermission(playerCall, PermGiveItem)) {
				playerCall.ConsoleMessage("No permission");
				return;
			}
			if (!arg.HasArgs(2)) {
				return;
			}
			var player = BasePlayer.Find(arg.Args[0]);
			if (player == null) {
				playerCall.ConsoleMessage("Player not found");
				return;
			}
			int amount = 1;
			if (arg.GetInt(2) != 0) {
				int.TryParse(arg.Args[2], out amount);
			}
			Item item = ItemManager.CreateByName(arg.Args[1]);
			if (item != null) {
				giveItem(player, item, amount);
				player.ChatMessage("You recieved " + amount + " " + item.info.displayName.translated);
			}
		}
		
		[ConsoleCommand("rei.remove")]
		void ccRemoveItem(ConsoleSystem.Arg arg) {
			var playerCall = arg.Player();
			if (!playerCall) {
				return;
			}
			if (!hasPermission(playerCall, PermGiveItem)) {
				playerCall.ConsoleMessage("No permission");
				return;
			}
			var player = BasePlayer.Find(arg.Args[0]);
			if (player == null) {
				playerCall.ConsoleMessage("Player not found");
				return;
			}
			int amount = 1;
			if (arg.GetInt(2) != 0) {
				int.TryParse(arg.Args[2], out amount);
			}
			var item = ItemManager.CreateByName(arg.Args[1]);
			if (item != null) {
				player.inventory.Take(null, item.info.itemid, amount);
				player.ChatMessage("You lost " + amount + " " + item.info.displayName.translated);
			}
		}
		
		[ConsoleCommand("rei.reset")]
		void ccResetItems(ConsoleSystem.Arg arg) {
			var playerCall = arg.Player();
			if (!playerCall) {
				return;
			}
			if (!hasPermission(playerCall, PermGiveItem)) {
				playerCall.ConsoleMessage("No permission");
				return;
			}
			var player = BasePlayer.Find(arg.Args[0]);
			if (player == null) {
				playerCall.ConsoleMessage("Player not found");
				return;
			}
			player.inventory.GiveDefaultItems();
			player.ChatMessage("Your items was reset.");
		}
	}
}