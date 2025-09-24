using Oxide.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Command Walls", "hoppel", "1.0.0")]
    [Description("Allows you to bind chat/console commands to Building blocks / doors")]
    class CommandWalls : RustPlugin
    {
        #region Declaration
        StoredData storedData;
        Dictionary<uint, Commands> CmdWallsCache = new Dictionary<uint, Commands>();
        private const string permNameUse = "commandwall.use";
        private const string permNameAdmin = "commandwall.admin";
        #endregion

        #region Hooks
        void Init()
        {
            permission.RegisterPermission(permNameUse, this);
            permission.RegisterPermission(permNameAdmin, this);
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
            CmdWallsCache = storedData.CmdWalls;
        }

        void Unload()
        {
            storedData.CmdWalls = CmdWallsCache;
            Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);
        }

        void OnNewSave(string filename)
        {
            storedData.CmdWalls.Clear();
            CmdWallsCache.Clear();
            Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);
        }

        void OnServerSave()
        {
            storedData.CmdWalls = CmdWallsCache;
            Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);
        }

        void OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (info.HitEntity is BuildingBlock)
                if (CmdWallsCache.ContainsKey(info.HitEntity.net.ID))
                {
                    if (!permission.UserHasPermission(player.UserIDString, permNameUse))
                        return;
                    if (CmdWallsCache[info.HitEntity.net.ID].CommandType == "chat")
                        player.SendConsoleCommand("chat.say /" + CmdWallsCache[info.HitEntity.net.ID].Command);
                    else
                        player.SendConsoleCommand(CmdWallsCache[info.HitEntity.net.ID].Command);
                }
        }

        void OnDoorOpened(Door door, BasePlayer player)
        {
            if (CmdWallsCache.ContainsKey(door.net.ID))
            {
                if (!permission.UserHasPermission(player.UserIDString, permNameUse))
                    return;
                door.CloseRequest();
                if (CmdWallsCache[door.net.ID].CommandType == "chat")
                    player.SendConsoleCommand($"chat.say \"/{CmdWallsCache[door.net.ID].Command}\"");
                else
                    player.SendConsoleCommand(CmdWallsCache[door.net.ID].Command);
            }
        }

        enum ChatCommands
        {
            Info,
            Console,
            Chat,
            Remove
        }
        #endregion

        #region Command
        [ChatCommand("cw")]
        void cmdCW(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1 || !permission.UserHasPermission(player.UserIDString, permNameAdmin))
                return;
            RaycastHit hit;
            var hitObject = Physics.Raycast(player.eyes.HeadRay(), out hit, 5f);
            if (!hitObject)
                return;
            var buildingBlock = hit.GetEntity();
            if (buildingBlock is BuildingBlock || buildingBlock is Door)
            {
                var type = args[0];
                if (type == ChatCommands.Info.ToString().ToLower())
                    if (CmdWallsCache.ContainsKey(buildingBlock.net.ID))
                        SendReply(player, Lang("CWInfo", player.UserIDString, buildingBlock.net.ID.ToString(), CmdWallsCache[buildingBlock.net.ID].CommandType, CmdWallsCache[buildingBlock.net.ID].Command));
                else if (type == ChatCommands.Remove.ToString().ToLower())
                    if (CmdWallsCache.ContainsKey(buildingBlock.net.ID))
                        CmdWallsCache.Remove(buildingBlock.net.ID);

                if (args.Length < 2)
                    return;
                if (type == ChatCommands.Chat.ToString().ToLower() || type == ChatCommands.Console.ToString().ToLower())
                {
                    var blockID = buildingBlock.net.ID;
                    var _command = string.Join(" ", args.Skip(1).ToArray());
                    if (CmdWallsCache.ContainsKey(buildingBlock.net.ID))
                    {
                        CmdWallsCache[blockID].CommandType = type;
                        CmdWallsCache[blockID].Command = _command;
                    }
                    else
                    {
                        CmdWallsCache.Add(blockID, new Commands());
                        CmdWallsCache[blockID].CommandType = type;
                        CmdWallsCache[blockID].Command = _command;
                    }
                    SendReply(player, Lang("CWCommandAdded", player.UserIDString, type, _command));
                }
                else
                    SendReply(player, Lang("ErrorType", player.UserIDString));
            }
        }
        #endregion

        #region Data
        public class StoredData
        {
            public Dictionary<uint, Commands> CmdWalls = new Dictionary<uint, Commands>();

            public StoredData()
            {
            }
        }

        public class Commands
        {
            public string CommandType;
            public string Command;

            public Commands()
            {
            }
        }
        #endregion

        #region Lang
        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CWCommandAdded"] = "Added the following command as a \"{0}\" command: {1}",
                ["CWInfo"] = "ID: {0}\nCommand type: {1}\nCommand: {2}",
                ["ErrorType"] = "Invalid Type"
            }, this);
        }
        #endregion
    }
}
