using System;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using Facepunch.Extend;
using Network;


namespace Oxide.Plugins {
    [Info("PlayMovieButtons", "jerkypaisen", "1.0.0")]
    [Description("You can create a button to play an MP4 URL.")]
    // BaseMod; https://umod.org/plugins/toll-buttons by KajWithAJ
    class PlayMovieButtons : RustPlugin {
        private const string PermissionAdmin = "PlayMovieButtons.admin";

        private StoredData storedData = new StoredData();

        private void Init() {
            permission.RegisterPermission(PermissionAdmin, this);
        }

        private void OnServerInitialized() {
            LoadData();
        }

        private void Unload()
        {
            SaveData();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ButtonNotFound"] = "No button found",
                ["NoPermission"] = "You do not have permission to use this command.",
                ["NoButtonOwnership"] = "This button is not yours.",
                ["PlayMovieUrlSet"] = "A PlayMovieUrl of {0} was set for this button.",
                ["EmptyButton"] = "This button has no URL set.",
                ["InvalidUrl"] = "Provide a valid URL."
            }, this);
        }

        [ChatCommand("pmb")]
        private void ChatCmdCheckButton(BasePlayer player, string command, string[] args)
        {
            RaycastHit hit;
            var raycast = Physics.Raycast(player.eyes.HeadRay(), out hit, 2f, 2097409);
            BaseEntity button = raycast ? hit.GetEntity() : null;
            if (button == null || button as PressButton == null)
            {
                string message = lang.GetMessage("ButtonNotFound", this, player.UserIDString);
                player.ChatMessage(string.Format(message));
                return;
            }

            if (args.Length >= 1)
            {
                if (!permission.UserHasPermission(player.UserIDString, PermissionAdmin))
                {
                    string message = lang.GetMessage("NoPermission", this, player.UserIDString);
                    player.ChatMessage(string.Format(message));
                    return;
                }

                if (button.OwnerID != player.userID)
                {
                    player.ChatMessage(string.Format(lang.GetMessage("NoButtonOwnership", this, player.UserIDString)));
                    return;
                }
                string url = args[0];
                //if (!(url.EndsWith(".mp4") || url.EndsWith(".Mp4") || url.EndsWith(".MP4")))
                //{
                //    player.ChatMessage(string.Format(lang.GetMessage("InvalidUrl", this, player.UserIDString)));
                //    return;
                //}

                if (!storedData.PlayMovieButtons.ContainsKey(button.net.ID.Value))
                {
                    ButtonData buttonData = new ButtonData();
                    buttonData.movieURL = url;
                    buttonData.ownerID = player.UserIDString;
                    storedData.PlayMovieButtons.Add(button.net.ID.Value, buttonData);
                }
                else
                {
                    storedData.PlayMovieButtons[button.net.ID.Value].movieURL = url;
                }

                player.ChatMessage(string.Format(lang.GetMessage("PlayMovieUrlSet", this, player.UserIDString), url));
            }
            else
            {
                string url = CheckButtonUrl(button as PressButton);
                if (url.Length >= 1)
                {
                    player.ChatMessage(string.Format(lang.GetMessage("PlayMovieUrlSet", this, player.UserIDString), url));
                }
                else
                {
                    player.ChatMessage(string.Format(lang.GetMessage("EmptyButton", this, player.UserIDString)));
                }
            }
        }

        private object OnButtonPress(PressButton button, BasePlayer player)
        {
            if (button.OwnerID == 0) return null;

            string url = CheckButtonUrl(button);

            if (url.Length > 0) {
                player.Command("client.playvideo", url);
                return null;
            } else {
                return null;
            }
        }

        private string CheckButtonUrl(PressButton button) {
            if (!storedData.PlayMovieButtons.ContainsKey(button.net.ID.Value)) {
                return "";
            } else {
                return storedData.PlayMovieButtons[button.net.ID.Value].movieURL;
            }
        }


        private class StoredData
        {
            public readonly Dictionary<ulong, ButtonData> PlayMovieButtons = new Dictionary<ulong, ButtonData>();
        }

        private class ButtonData
        {
            public string ownerID = "";
            public string movieURL = "";
        }

        private void LoadData() => storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Name);

        private void SaveData() => Interface.GetMod().DataFileSystem.WriteObject(this.Name, storedData);

        private void OnServerSave() => SaveData();

        private void OnNewSave(string name)
        {
            PrintWarning("Map wipe detected - clearing PlayMovieButtons...");

            storedData.PlayMovieButtons.Clear();
            SaveData();
        }
    }
}
