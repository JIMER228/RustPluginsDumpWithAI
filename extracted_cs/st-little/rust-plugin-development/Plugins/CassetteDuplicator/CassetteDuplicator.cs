// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Cassette Duplicator", "st-little", "0.3.0")]
    [Description("Duplicate cassettes using a cassette recorder.")]
    public class CassetteDuplicator : RustPlugin
    {
        #region Fields

        private const string AdminPermission = "cassetteduplicator.admin";
        private const string DuplicatePermission = "cassetteduplicator.duplicate";
        private const string RestorePermission = "cassetteduplicator.restore";

        private readonly Dictionary<ulong, DuplicateCassette> DuplicateCassettes = new Dictionary<ulong, DuplicateCassette>();

        #endregion

        #region Oxide hooks

        private void Init()
        {
            RegisterPermissions();
        }

        #endregion

        #region Commands

        [ChatCommand("cassette")]
        private void CassetteCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                player.ChatMessage("Usage: /cassette copy|paste|save|restore");
                return;
            }

            var cassette = GetHeldCassette(player);
            if (cassette == null)
            {
                player.ChatMessage("You must have a cassette recorder with cassette in it.");
                return;
            }

            switch (args[0])
            {
                case "copy":
                    if (!HasDuplicatePermission(player.UserIDString))
                    {
                        player.ChatMessage("You don't have permission to use this command!");
                        break;
                    }
                    if (!CanCopyCassette(player.userID, cassette, HasAdminPermission(player.UserIDString)))
                    {
                        player.ChatMessage("Cassette cannot be copied.");
                        break;
                    }
                    CopyCassette(player.userID, cassette);
                    player.ChatMessage("Copied cassette.");
                    break;
                case "paste":
                    if (!HasDuplicatePermission(player.UserIDString))
                    {
                        player.ChatMessage("You don't have permission to use this command!");
                        break;
                    }
                    if (!CanPasteCassette(player.userID, cassette))
                    {
                        player.ChatMessage("Cassette cannot be pasted.");
                        break;
                    }
                    PasteCassette(player.userID, cassette);
                    player.ChatMessage("Pasted cassette.");
                    break;
                case "save":
                    if (!HasRestorePermission(player.UserIDString))
                    {
                        player.ChatMessage("You don't have permission to use this command!");
                        break;
                    }
                    if (!CanSaveAudio(player.userID, cassette, HasAdminPermission(player.UserIDString)))
                    {
                        player.ChatMessage("Audio data cannot be saved.");
                        break;
                    }
                    var savedAudioPath = SaveAudio(cassette);
                    if (savedAudioPath == null)
                    {
                        player.ChatMessage("Failed to save audio data.");
                    }
                    else
                    {
                        player.ChatMessage($"Saved audio data to {savedAudioPath}");

                    }
                    break;
                case "restore":
                    if (!HasRestorePermission(player.UserIDString))
                    {
                        player.ChatMessage("You don't have permission to use this command!");
                        break;
                    }
                    if (args.Length != 2)
                    {
                        player.ChatMessage("Usage: /cassette restore <file name>");
                    }
                    if (!CanRestoreAudio(player.userID, cassette, args[1], HasAdminPermission(player.UserIDString)))
                    {
                        player.ChatMessage("Audio data cannot be restored.");
                        break;
                    }
                    var restoreAudioPath = RestoreAudio(cassette, args[1]);
                    if (restoreAudioPath == null)
                    {
                        player.ChatMessage("Failed to restore audio data.");
                    }
                    else
                    {
                        player.ChatMessage($"Restored audio data from {restoreAudioPath}");
                    }
                    break;
                default:
                    player.ChatMessage("Usage: /cassette copy|paste|save|restore");
                    break;
            }
        }

        #endregion

        #region Duplicate feature

        private struct DuplicateCassette
        {
            public DuplicateCassette(uint audioId, ulong creatorSteamId, NetworkableId netID)
            {
                AudioId = audioId;
                CreatorSteamId = creatorSteamId;
                NetID = netID;
            }

            public uint AudioId { get; }
            public ulong CreatorSteamId { get; }
            public NetworkableId NetID { get; }
        }

        /// <summary>
        /// Verify that the cassette can be copied.
        /// </summary>
        /// <param name="userID">User ID to copy cassette.</param>
        /// <param name="cassette">Copy source cassette.</param>
        /// <param name="hasAdminPermission">The user copying the cassette has administrative privileges.</param>
        /// <returns>Returns true if the cassette can be copied.</returns>
        private bool CanCopyCassette(ulong userID, Cassette cassette, bool hasAdminPermission)
        {
            if (!IsRecorded(cassette)) { return false; }
            if (!hasAdminPermission && userID != cassette.CreatorSteamId) { return false; }

            return true;
        }

        /// <summary>
        /// Copy the cassette.
        /// </summary>
        /// <param name="userID">User ID to copy cassette.</param>
        /// <param name="cassette">Copy source cassette.</param>
        private void CopyCassette(ulong userID, Cassette cassette)
        {
            DuplicateCassettes[userID] = new DuplicateCassette(
                cassette.AudioId,
                cassette.CreatorSteamId,
                cassette.net.ID
                );

        }

        /// <summary>
        /// Verify that the cassette can be pasted.
        /// </summary>
        /// <param name="userID">User ID to paste the cassette.</param>
        /// <param name="cassette">Cassette to be pasted.</param>
        /// <returns>Returns true if the cassette can be pasted.</returns>
        private bool CanPasteCassette(ulong userID, Cassette cassette)
        {
            if (IsRecorded(cassette)) { return false; }
            if (!DuplicateCassettes.ContainsKey(userID)) { return false; }
            var duplicateCassette = DuplicateCassettes[userID];
            var data = FileStorage.server.Get(duplicateCassette.AudioId, FileStorage.Type.ogg, duplicateCassette.NetID);
            if (data == null) { return false; }
            if (!Cassette.IsOggValid(data, cassette)) { return false; }

            return true;
        }

        /// <summary>
        /// Paste the cassette.
        /// </summary>
        /// <param name="userID">User ID to paste the cassette.</param>
        /// <param name="cassette">Cassette to be pasted.</param>
        private void PasteCassette(ulong userID, Cassette cassette)
        {
            var duplicateCassette = DuplicateCassettes[userID];
            cassette.SetAudioId(duplicateCassette.AudioId, duplicateCassette.CreatorSteamId);
            // recorderToolItem.contents.itemList[0].info.shortname = cassetteInfo.ItemShortname;
        }

        #endregion

        #region Restore feature

        /// <summary>
        /// Verify that the audio data can be saved.
        /// </summary>
        /// <param name="userID">User ID to save audio data.</param>
        /// <param name="cassette">Cassette of saving source.</param>
        /// <param name="hasAdminPermission">The user saving the cassette has administrative privileges.</param>
        /// <returns>Returns true if audio data can be saved.</returns>
        private bool CanSaveAudio(ulong userID, Cassette cassette, bool hasAdminPermission)
        {
            if (!IsRecorded(cassette)) { return false; }
            var data = FileStorage.server.Get(cassette.AudioId, FileStorage.Type.ogg, cassette.net.ID);
            if (data == null) { return false; }
            if (!hasAdminPermission && userID != cassette.CreatorSteamId) { return false; }

            return true;
        }

        /// <summary>
        /// Save the audio data.
        /// </summary>
        /// <param name="cassette">Cassette of saving source.</param>
        /// <returns>Returns the path if audio data could be saved.</returns>
        private string? SaveAudio(Cassette cassette)
        {
            string fileName = $"{cassette.CreatorSteamId}_{cassette.AudioId}.ogg";
            string dataPath = Path.Combine(Application.temporaryCachePath, fileName);
            var data = FileStorage.server.Get(cassette.AudioId, FileStorage.Type.ogg, cassette.net.ID);

            try
            {
                File.WriteAllBytes(dataPath, data);
                return dataPath;
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Verify that the audio data can be restored.
        /// </summary>
        /// <param name="userID">User ID for restoring audio data.</param>
        /// <param name="cassette">Cassette to be restored.</param>
        /// <param name="fileName">File name of the audio file to be restored.</param>
        /// <param name="hasAdminPermission">The user who restores audio data has administrative privileges.</param>
        /// <returns>Returns true if audio data can be restored.</returns>
        private bool CanRestoreAudio(ulong userID, Cassette cassette, string fileName, bool hasAdminPermission)
        {
            if (IsRecorded(cassette)) { return false; }
            string[] splitFileName = fileName.Split('_');
            if (splitFileName.Length == 2 == false) { return false; }
            if (ulong.TryParse(splitFileName[0], out var creatorSteamId) == false) { return false; }
            if (!hasAdminPermission && userID != creatorSteamId) { return false; }
            string dataPath = Path.Combine(Application.temporaryCachePath, fileName);
            if (!File.Exists(dataPath)) { return false; }
            try
            {
                byte[] data = File.ReadAllBytes(dataPath);
                if (!Cassette.IsOggValid(data, cassette)) { return false; }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Restore the audio data.
        /// </summary>
        /// <param name="cassette">Cassette to be restored.</param>
        /// <param name="fileName">File name of the audio file to be restored.</param>
        /// <returns>Returns the path if audio data could be restored.</returns>
        private string? RestoreAudio(Cassette cassette, string fileName)
        {
            var creatorSteamId = ulong.Parse(fileName.Split('_')[0]);
            string dataPath = Path.Combine(Application.temporaryCachePath, fileName);

            try
            {
                byte[] data = File.ReadAllBytes(dataPath);
                uint audioId = FileStorage.server.Store(data, FileStorage.Type.ogg, cassette.net.ID);
                cassette.SetAudioId(audioId, creatorSteamId);

                return dataPath;
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                return null;
            }
        }

        #endregion

        #region Helpers

        private void RegisterPermissions()
        {
            permission.RegisterPermission(AdminPermission, this);
            permission.RegisterPermission(DuplicatePermission, this);
            permission.RegisterPermission(RestorePermission, this);
        }

        private bool HasAdminPermission(string playerId)
        {
            return permission.UserHasPermission(playerId, AdminPermission);
        }

        private bool HasDuplicatePermission(string playerId)
        {
            return HasAdminPermission(playerId) || permission.UserHasPermission(playerId, DuplicatePermission);
        }

        private bool HasRestorePermission(string playerId)
        {
            return HasAdminPermission(playerId) || permission.UserHasPermission(playerId, RestorePermission);
        }

        private bool IsRecorded(Cassette cassette)
        {
            return cassette.AudioId != 0 && cassette.CreatorSteamId != 0;
        }

        private Cassette? GetHeldCassette(BasePlayer player)
        {
            var recorderTool = player.GetHeldEntity() as RecorderTool;
            if (recorderTool == null) { return null; }

            if (!HasCassette(recorderTool)) { return null; }

            return recorderTool.cachedCassette;
        }

        private bool HasCassette(RecorderTool recorderTool)
        {
            return recorderTool.cachedCassette != null;
        }

        #endregion
    }
}