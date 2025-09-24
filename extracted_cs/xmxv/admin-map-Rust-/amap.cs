using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
    {
        [Info("Admin Map", "Melonoma", "1.0.1")]
        [Description("Displays all connected players on the map for admins.")]
        public class AdminMap : RustPlugin
        {
            // Define a variable to track whether the map markers are enabled or disabled
            private bool markersEnabled = false;

            // Define a function to toggle the map markers on or off
            private void ToggleMarkers(BasePlayer player)
            {
                // Check if the player has permission to toggle the markers
                if (!permission.UserHasPermission(player.UserIDString, "adminmap.toggle"))
                {
                    SendReply(player, "You do not have permission to use this command!");
                    return;
                }

                // Toggle the markers
                markersEnabled = !markersEnabled;

                // Send a message to the player indicating whether the markers are now on or off
                string message = markersEnabled ? "Map markers enabled!" : "Map markers disabled!";
                SendReply(player, message);
            }

            // Register the chat command to toggle the markers
            [ChatCommand("amap")]
            private void OnChatCommand(BasePlayer player, string command, string[] args)
            {
                if (command == "amap")
                {
                    if (args.Length == 0)
                    {
                        ToggleMarkers(player);
                    }
                    else
                    {
                        string subcommand = args[0];
                        if (subcommand == "on")
                        {
                            markersEnabled = true;
                            SendReply(player, "Map markers enabled!");
                        }
                        else if (subcommand == "off")
                        {
                            markersEnabled = false;
                            SendReply(player, "Map markers disabled!");
                        }
                        else
                        {
                            SendReply(player, "Invalid subcommand. Usage: /amap [on|off]");
                        }
                    }

                    return;
                }
            }

            // Get the map object
            private Map map = UnityEngine.Object.FindObjectOfType<Map>();

            // Define the user groups that are allowed to see the markers
            private readonly string[] allowedGroups = { "admin", "moderator" };

            // Loop through the players and add their location to the map if the markers are enabled
            private void DrawMapMarkers()
            {
                if (markersEnabled)
                {
                    List<BasePlayer> players = BasePlayer.activePlayerList.ToList();
                    foreach (BasePlayer player in players)
                    {
                        // Check if the player is in an allowed user group
                        bool hasPermission = false;
                        foreach (string allowedGroup in allowedGroups)
                        {
                            if (permission.UserHasGroup(player.UserIDString, allowedGroup))
                            {
                                hasPermission = true;
                                break;
                            }
                        }

                        if (hasPermission)
                        {
                            // Get the player's position
                            Vector3 position = player.transform.position;

                            // Add a marker to the map at the player's position
                            map.AddMarker(position, "Player", Color.white, false);
                        }
                    }
                }
                else
                {
                    // If the markers are disabled, remove all existing markers from the map
                    map.RemoveMarkers();
                }
            }

            // Call the DrawMapMarkers function periodically to update the markers
            private void OnServerInitialized()
            {
                timer.Repeat(5f, 0, () => DrawMapMarkers());
            }
        }
            private void OnMapMarkerRemoved(MapMarker mapMarker, BasePlayer player)
        {
                // Check if the player is authorized and if the map markers are enabled
                if (player.IsAdmin && markersEnabled)
            {
                // Teleport the player to the location of the removed map marker
                player.MovePosition(mapMarker.transform.position);
            }
        }
    }
