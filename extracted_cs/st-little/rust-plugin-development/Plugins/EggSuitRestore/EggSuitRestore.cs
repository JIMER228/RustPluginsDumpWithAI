using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Egg Suit Restore", "st-little", "0.1.0")]
    [Description("This plugin restore the egg suit from the image.")]
    class EggSuitRestore : RustPlugin
    {
        #region Fields

        private const string Permission = "eggsuitrestore.allow";
        private const string EggSuitShortName = "attire.egg.suit";
        private readonly Dictionary<ulong, Setting> PlayerSetting = new Dictionary<ulong, Setting>();

        #endregion

        #region Setting

        private class Setting
        {
            public ulong PlayerID;
            public bool IsEnabled;
            public string? Url;

            public Setting(ulong playerID, bool isEnabled = false, string? url = null)
            {
                PlayerID = playerID;
                IsEnabled = isEnabled;
                Url = url;
            }
        }

        #endregion

        #region Restore Process

        private IEnumerator ImageRestore(string url, BasePlayer player, PaintedItemStorageEntity instance, Item item)
        {
            player.ConsoleMessage($"Start downloading images for restoration.");
            player.ConsoleMessage($"Download URL ==> {url}");

            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                player.ConsoleMessage("Error downloading image.");
                player.ConsoleMessage("End Egg Suit restoration.");

                yield break;

            }

            Puts("Image download is complete.");

            byte[] array = ToPngImage(www.downloadHandler.data);

            instance._currentImageCrc = FileStorage.server.Store(array, FileStorage.Type.png, instance.net.ID);
            instance.SendNetworkUpdate();

            player.ConsoleMessage("Restoration is complete.");
            player.ConsoleMessage("End Egg Suit restoration.");
        }

        private static byte[] ToPngImage(byte[] data)
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(data);

            byte[] image = texture.EncodeToPNG();

            UnityEngine.Object.DestroyImmediate(texture);

            return image;
        }

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            permission.RegisterPermission(Permission, this);
        }

        private void OnItemPainted(PaintedItemStorageEntity instance, Item item, BasePlayer player, byte[] current)
        {

            if (item.info.shortname != EggSuitShortName) return;

            if (!permission.UserHasPermission(player.UserIDString, Permission)) return;

            var hasPlayerSetting = PlayerSetting.ContainsKey(player.userID);
            if (!hasPlayerSetting) return;

            var playerSetting = PlayerSetting[player.userID];
            if (!playerSetting.IsEnabled || playerSetting == null) return;

            player.ConsoleMessage("Start Egg Suit restoration.");

            instance.ClearContent();
            ServerMgr.Instance.StartCoroutine(ImageRestore(playerSetting.Url, player, instance, item));
        }

        #endregion

        #region Console Commands

        [ConsoleCommand("esr.info")]
        private void DisplayInfoConsoleCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
            {
                SendReply(arg, $"Error: Player not found.");
                return;
            }

            if (!permission.UserHasPermission(player.UserIDString, Permission))
            {
                SendReply(arg, "Error: You don't have permission to use this command!");
                return;
            }

            var playerSetting = PlayerSetting.ContainsKey(player.userID) ? PlayerSetting[player.userID] : new Setting(player.userID);
            PlayerSetting[player.userID] = playerSetting;

            SendReply(arg, $"Enabled ==> {playerSetting.IsEnabled}");
            SendReply(arg, $"DL URL  ==> {playerSetting.Url}");
        }

        [ConsoleCommand("esr.enabled")]
        private void SetEnabledConsoleCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
            {
                SendReply(arg, $"Error: Player not found.");
                return;
            }

            if (!permission.UserHasPermission(player.UserIDString, Permission))
            {
                SendReply(arg, "Error: You don't have permission to use this command!");
                return;
            }

            if (arg.Args == null || arg.Args.Length != 1 || (arg.Args[0] != "true" && arg.Args[0] != "false"))
            {
                SendReply(arg, $"Error: To enable Egg Suit Restore, pass true as the command argument.");
                SendReply(arg, $"Error: To disable Egg Suit Restore, pass false as the command argument.");
                return;
            }

            var playerSetting = PlayerSetting.ContainsKey(player.userID) ? PlayerSetting[player.userID] : new Setting(player.userID);
            playerSetting.IsEnabled = arg.Args[0] == "true";
            PlayerSetting[player.userID] = playerSetting;

            if (playerSetting.IsEnabled)
            {
                SendReply(arg, $"Egg Suit restoration is enabled.");
            }
            else
            {
                SendReply(arg, $"Egg Suit restoration is disabled.");
            }
        }

        [ConsoleCommand("esr.url")]
        private void SetUrlConsoleCommand(ConsoleSystem.Arg arg)
        {

            var player = arg.Connection.player as BasePlayer;
            if (player == null)
            {
                SendReply(arg, $"Error: Player not found.");
                return;
            }

            if (!permission.UserHasPermission(player.UserIDString, Permission))
            {
                SendReply(arg, "Error: You don't have permission to use this command!");
                return;
            }

            if (arg.Args == null || arg.Args.Length != 1)
            {
                SendReply(arg, $"Error: Specify the URL to download the image for restoration as the argument of the command.");
                return;
            }

            var playerSetting = PlayerSetting.ContainsKey(player.userID) ? PlayerSetting[player.userID] : new Setting(player.userID);
            playerSetting.Url = arg.Args[0];
            PlayerSetting[player.userID] = playerSetting;

            SendReply(arg, $"The URL has been specified.");
        }

        [ConsoleCommand("esr.clear")]
        private void ClearConsoleCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
            {
                SendReply(arg, $"Error: Player not found.");
                return;
            }

            if (!permission.UserHasPermission(player.UserIDString, Permission))
            {
                SendReply(arg, "Error: You don't have permission to use this command!");
                return;
            }

            PlayerSetting[player.userID] = new Setting(player.userID);

            SendReply(arg, $"Settings are cleared.");
        }

        #endregion
    }
}