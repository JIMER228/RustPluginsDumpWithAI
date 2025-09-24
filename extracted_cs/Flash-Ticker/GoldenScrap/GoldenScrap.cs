using Oxide.Core.Plugins;
using System;

namespace Oxide.Plugins
{
    [Info("GoldenScrap", "RustFlash", "1.4.0")]
    [Description("Allows admins to give players Golden Scrap via the console or RCON")]
    class GoldenScrap : RustPlugin
    {
        private const ulong ScrapSkinId = 3239303050;

        private const string PermissionAdmin = "goldenscrap.admin";

        void Init()
        {
            permission.RegisterPermission(PermissionAdmin, this);
            cmd.AddConsoleCommand("inventory.give.goldenscrap", this, "GiveGoldenScrapCommand");
        }

        [ConsoleCommand("inventory.give.goldenscrap")]
        private void GiveGoldenScrapCommand(ConsoleSystem.Arg arg)
        {
            bool isServerOrRcon = arg.Connection == null;
            
            if (!isServerOrRcon)
            {
                var player = arg.Connection.player as BasePlayer;
                if (player == null)
                {
                    return;
                }

                if (!permission.UserHasPermission(player.UserIDString, PermissionAdmin))
                {
                    SendReply(arg, "Du hast keine Berechtigung, diesen Befehl zu verwenden.");
                    return;
                }
            }

            string targetIdentifier;
            int amount;

            if (isServerOrRcon)
            {
                if (arg.Args == null || arg.Args.Length < 2)
                {
                    Puts("Benutzung: inventory.give.goldenscrap \"Spielername/SteamID\" Menge");
                    return;
                }
                
                targetIdentifier = arg.Args[0];
                
                if (!int.TryParse(arg.Args[1], out amount) || amount <= 0)
                {
                    Puts("Ungültige Menge. Sie muss eine positive Zahl sein.");
                    return;
                }
            }
            else
            {
                if (arg.Args == null || arg.Args.Length < 3)
                {
                    SendReply(arg, "Benutzung: inventory.give.goldenscrap \"Spielername/SteamID\" GoldenScrap Menge");
                    return;
                }
                
                targetIdentifier = arg.Args[0];
                
                if (string.IsNullOrEmpty(arg.Args[1]) || !arg.Args[1].Equals("GoldenScrap", StringComparison.OrdinalIgnoreCase))
                {
                    SendReply(arg, "Das zweite Argument muss 'GoldenScrap' sein.");
                    return;
                }
                
                if (!int.TryParse(arg.Args[2], out amount) || amount <= 0)
                {
                    SendReply(arg, "Ungültige Menge. Sie muss eine positive Zahl sein.");
                    return;
                }
            }

            var targetPlayer = BasePlayer.Find(targetIdentifier);
            if (targetPlayer == null)
            {
                ulong steamId;
                if (ulong.TryParse(targetIdentifier, out steamId))
                {
                    targetPlayer = BasePlayer.FindByID(steamId);
                    if (targetPlayer == null)
                    {
                        var errorMsg = $"Kein Spieler mit dem Namen oder der SteamID '{targetIdentifier}' gefunden.";
                        if (isServerOrRcon)
                            Puts(errorMsg);
                        else
                            SendReply(arg, errorMsg);
                        return;
                    }
                }
                else
                {
                    var errorMsg = $"Kein Spieler mit dem Namen oder der SteamID '{targetIdentifier}' gefunden.";
                    if (isServerOrRcon)
                        Puts(errorMsg);
                    else
                        SendReply(arg, errorMsg);
                    return;
                }
            }

            var item = ItemManager.CreateByName("scrap", amount, ScrapSkinId);
            if (item != null)
            {
                targetPlayer.inventory.GiveItem(item);
                
                string successMessage = $"{amount} Golden Scrap an {targetPlayer.displayName} gegeben.";
                if (isServerOrRcon)
                    Puts(successMessage);
                else
                    SendReply(arg, successMessage);
                
                targetPlayer.ChatMessage($"Du hast {amount} Golden Scrap erhalten!");
            }
            else
            {
                var errorMsg = "Fehler beim Erstellen des Items.";
                if (isServerOrRcon)
                    Puts(errorMsg);
                else
                    SendReply(arg, errorMsg);
            }
        }

        private void SendReply(ConsoleSystem.Arg arg, string message)
        {
            var player = arg.Connection?.player as BasePlayer;
            if (player != null)
            {
                player.ChatMessage(message);
            }
        }
    }
}