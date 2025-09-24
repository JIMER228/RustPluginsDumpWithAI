using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

/*
* Not tested with found multiple name in same server (multiple owner name)
* 
* Todos Update: 
* - Prevent delete on multiple player with same name found and returning admin message
* - create API function WipeOnBanned ( Wipe Foundation on player getting banned ) so other plugin (like PlayerAdministration) can use this function
*/

namespace Oxide.Plugins
{
    [Info("Foundation Wipe", "aahadr", "1.0.0")]
    [Description("Delete all foundations owned by a player with the given Steam ID or name.")]
    class FoundationWipe : RustPlugin
    {
        private const string PermissionWipe = "foundationwipe.use";
        private List<string> Foundation = new List<string> { "foundation", "foundation.triangle" };

        private void Init()
        {
            permission.RegisterPermission(PermissionWipe, this);
        }

        [ChatCommand("fwipe")]
        private void FwipeCommand(BasePlayer player, string command, string[] args)
        {

            if (args.Length == 0)
            {
                SendReply(player, "Usage: /fwipe <SteamID or Name>");
                return;
            }

            DoWipe(player, args[0]);
        }

        [ConsoleCommand("fwipe")]
        private void FwipeConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length != 1)
            {
                SendReply(arg, "Usage: fwipe <SteamID or Name>");
                return;
            }

            if (!permission.UserHasPermission(arg.Connection.userid.ToString(), PermissionWipe))
            {
                SendReply(arg, "You don't have permission to use this command.");
                return;
            }

            var targetPlayer = FindPlayer(arg.Args[0]);
            if (targetPlayer == null)
            {
                SendReply(arg, $"No player found with the name or Steam ID '{arg.Args[0]}'");
                return;
            }

            var foundationList = new List<BuildingBlock>();
            foreach (var entity in UnityEngine.Object.FindObjectsOfType<BuildingBlock>())
            {
                if (entity.OwnerID == targetPlayer.userID && Foundation.Contains(entity.ShortPrefabName))
                {
                    foundationList.Add(entity);
                }
            }

            foreach (var foundation in foundationList)
            {
                foundation.Kill();
            }

            SendReply(arg, $"Deleted {foundationList.Count} foundations owned by {targetPlayer.displayName}.");
        }


        private BasePlayer FindPlayer(string target)
        {
            ulong targetID;
            if (ulong.TryParse(target, out targetID))
            {
                var targetPlayer = BasePlayer.FindByID(targetID);
                if (targetPlayer != null)
                {
                    return targetPlayer;
                }
            }

            var targetPlayerByName = BasePlayer.Find(target);
            if (targetPlayerByName != null)
            {
                return targetPlayerByName;
            }

            return null;
        }


        private void DoWipe(BasePlayer player, string target)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionWipe))
            {
                SendReply(player, "You don't have permission to use this command.");
                return;
            }

            var targetPlayer = FindPlayer(target);
            if (targetPlayer == null)
            {
                SendReply(player, $"No player found with the name or Steam ID '{target}'");
                return;
            }

            var foundationList = new List<BuildingBlock>();
            foreach (var entity in UnityEngine.Object.FindObjectsOfType<BuildingBlock>())
            {
                if (entity.OwnerID == targetPlayer.userID && Foundation.Contains(entity.ShortPrefabName))
                {
                    foundationList.Add(entity);
                }
            }

            foreach (var foundation in foundationList)
            {
                foundation.Kill();
            }

            SendReply(player, $"Deleted {foundationList.Count} foundations owned by {targetPlayer.displayName}.");
        }


    }
}
