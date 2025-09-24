using System;
using System.IO;
using System.Net;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("All Logs", "Hougan", "1.0.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    public class AllLogs : RustPlugin
    {
        //[PluginReference] private Plugin VKSystem;
        
        private void OnUserApprove(Network.Connection connection)
        {
            string message = $"[{DateTime.Now.ToShortTimeString()}] Игрок {connection.username} [{connection.userid}]. IP: {connection.ipaddress}\n" +
                             $"Уровень авторизации: {connection.authLevel}";

            if (connection.authLevel > 0 || permission.UserHasGroup(connection.userid.ToString(), "admin") || permission.UserHasGroup(connection.userid.ToString(), "moder"))
            {
                bool isModer = permission.UserHasGroup(connection.userid.ToString(), "admin") || permission.UserHasGroup(connection.userid.ToString(), "moder");
                string additionalText = " модератор или администратор";
                //VKSystem.Call("HOOK__API_SendMessageToChat", 3,
                    //$"{connection.username} [{connection.userid}] [{connection.ipaddress}] -> '{connection.authLevel}' {(isModer ? additionalText : "")} присоединился");

                if (isModer)
                    message += "\nИгрок администратор или модератор";
            }
            LogToFile("Connections", message, this);
            return;
        }
        
        private void OnRconCommand(IPAddress ip, string command, string[] args)
        {
			PrintError($"");
            string fullArgs = "";
            foreach (var check in args)
                fullArgs += $"{check} ";
            PrintWarning($"[{ip.ToString()}] Запросил {command} с аргуменатми {fullArgs}");
            LogToFile("RCON", $"[{DateTime.Now.ToShortTimeString()}] [{ip.ToString()}] Запросил {command} с аргуменатми {fullArgs}", this);
        }
        
        private void OnServerCommand(ConsoleSystem.Arg arg)
        {
			PrintError($"");
            string ip = arg.Connection == null ? "Не удалось" : arg.Connection.ipaddress;
            string name = arg.Player() == null ? "Не удалось" : arg.Player().ToString();
            LogToFile("SCommands", $"[{DateTime.Now.ToShortTimeString()}] {name} -> {arg.cmd.FullName} {arg.FullString} [{ip}]", this);
            
            return;
        }
        
        private void OnUserPermissionGranted(string id, string perm)
        {
			PrintError($"");
            LogToFile("Permission", $"[{DateTime.Now.ToShortTimeString()}] {id} granted {perm}", this);
        }
        
        private void OnUserGroupAdded(string id, string name)
        {
			PrintError($"");
            LogToFile("Permission", $"[{DateTime.Now.ToShortTimeString()}] {id} added to {name}", this);
        }
    }
}
