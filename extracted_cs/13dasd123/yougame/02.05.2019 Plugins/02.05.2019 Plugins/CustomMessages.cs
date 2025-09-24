// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Rust;
using System;
using Oxide.Core;
using UnityEngine;
using System.Linq;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Libraries;
using Oxide.Core.Configuration;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("CustomMessages", "Ryamkk", "1.0.6")]
      //  Слив плагинов server-rust by Apolo YouGame
    class CustomMessages : RustPlugin
    {
		#region Configuration
		string ChatPrefix;
		bool GiveMessages;
		
        protected override void LoadDefaultConfig()
        {
            GetVariable(Config, "Названия префикса в чате", out ChatPrefix, "<color=#ffd479>ПРЕФИКС</color>: ");
			GetVariable(Config, "Cообщение игроку при выдаче", out GiveMessages, true);
            SaveConfig();
        }
        #endregion

		#region OxideCore
		object OnServerMessage(string m, string n) => m.Contains("gave") && n == "SERVER" ? (object)GiveMessages : null;
        object OnServerMessage(string message, string name, string color, ulong id, string m, string n)
        {
            rust.BroadcastChat(ChatPrefix.Substring(0, ChatPrefix.Length - 2), message); // or   ConsoleNetwork.BroadcastToAllClients("chat.add", objArray1);
            return true;
        }

        object OnMessagePlayer(ref string message, BasePlayer player)
        {
            switch (message)
            {
                case "Can't afford to place!":
					SendReply(player, ChatPrefix + GetMessage("Can't afford to place!", player));
                    return false;
                case "Your active item was broken!":
					SendReply(player, ChatPrefix + GetMessage("Your active item was broken!", player));
                    return false;
                case "Can't place: Building privilege":
					SendReply(player, ChatPrefix + GetMessage("Can't place: Building privilege", player));
                    return false;
                case "Can't place: Too far away":
					SendReply(player, ChatPrefix + GetMessage("Can't place: Too far away", player));
                    return false;
                case "Can't place: Not enough space":
					SendReply(player, ChatPrefix + GetMessage("Can't place: Not enough space", player));
                    return false;
                case "Can't loot - already in use":
					SendReply(player, ChatPrefix + GetMessage("Can't loot - already in use", player));
                    return false;
                case "It is locked...":
					SendReply(player, ChatPrefix + GetMessage("It is locked...", player));
                    return false;
                case "AntiHack!":
					SendReply(player, ChatPrefix + GetMessage("AntiHack!", player));
                    return false;
                case "Building is blocked!":
					SendReply(player, ChatPrefix + GetMessage("Building is blocked!", player));
                    return false;
				case "Stop being a cunt":
					SendReply(player, ChatPrefix + GetMessage("Stop being a cunt", player));
                    return false;
				case "Placing through rock":
					SendReply(player, ChatPrefix + GetMessage("Placing through rock", player));
                    return false;
				case "Not enough space":
					SendReply(player, ChatPrefix + GetMessage("Not enough space", player));
                    return false;
				case "No Error":
					SendReply(player, ChatPrefix + GetMessage("No Error", player));
                    return false;
                default:
                    if(message.Contains("Unknown command"))
                    {
                        SendReply(player, ChatPrefix + message.Replace("Unknown command", "Неизвестная команда"));
                        return false;
                    }

                    if (message.Contains("Can't place"))
                    {
                        SendReply(player, ChatPrefix + message.Replace("Can't place", "Ошибка установки"));
                        return false;
                    }

                    break;
            }

            //Puts(message);
            return null;
        }
		#endregion
		
		#region Oxide		
		void OnServerInitialized()
        {
            LoadDefaultConfig();
			LoadMessages();
        }
		#endregion
		
		#region Localization
        private void LoadMessages()
        {
			lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["Can't afford to place!"] = "Недостаточно ресурсов!",
				["Your active item was broken!"] = "Ваш активный предмет сломался!",
				["Can't place: Building privilege"] = "В зоне размещения строительство запрещено!",
				["Can't place: Too far away"] = "Объект находится слишком далеко!",
				["Can't place: Not enough space"] = "Недостаточно сводобного места!",
				["Can't loot - already in use"] = "Контейнер занят!",
				["It is locked..."] = "Объект защищен замком!",
				["AntiHack!"] = "Сработала система защиты сервера!",
				["Building is blocked!"] = "Строительство запрещено!",
				["Stop being a cunt"] = "Стак фундаментов запрещён!",
				["Placing through rock"] = "Установка фундаментов близко к горной местности запрещена!",
				["Not enough space"] = "Установка фундамента рядом с РТ запрещена!",
				["No Error"] = "Установка структуры дома без фундамента запрещена!",
            }, this, "ru");
			
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["Can't afford to place!"] = "Insufficient resources!", 
                ["Your active item was broken!"] = "Your active item has broken down!", 
                ["Can't place: Building privilege"] = "In the area of ​​construction, construction is prohibited!", 
                ["Can't place: Too far away"] = "The object is too far away!", 
                ["Can't place: Not enough space"] = "Not enough free space!", 
                ["Can't loot - already in use"] = "The container is busy!", 
                ["It is locked..."] = "The object is protected by a lock!", 
                ["AntiHack!"] = "The server protection system has worked!", 
                ["Building is blocked!"] = "Construction is prohibited!", 
                ["Stop being a cunt"] = "A stack of foundations is forbidden!", 
                ["Placing through rock"] = "Installation of foundations close to the mountainous terrain is prohibited!", 
                ["Not enough space"] = "Installation of a foundation near RT is prohibited!", 
                ["No Error"] = "Installation of the structure of the house without a foundation is prohibited!",
            }, this);
        }
		#endregion
		
		#region Helpers
        string GetMessage(string key, BasePlayer player, params string[] args) => String.Format(lang.GetMessage(key, this, player.UserIDString), args);
		T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));
        public static void GetVariable<T>(DynamicConfigFile config, string name, out T value, T defaultValue)
        {
            config[name] = value = config[name] == null ? defaultValue : (T)Convert.ChangeType(config[name], typeof(T));
        }
		#endregion
    }
}
