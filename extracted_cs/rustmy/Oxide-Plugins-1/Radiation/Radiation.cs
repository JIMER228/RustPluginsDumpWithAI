using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins {
    [Info("Radiation", "bmlee2", "0.0.1")]
    class Radiation : RustPlugin {
		private bool Changed;
		private bool radiation;
		private bool rad;
		private string auth;
		
		void Loaded() {
			LoadVariables();
		}
		
        void Init() {
			SetMode();
        }

        void Unload() {
        }
		
		object GetConfig(string menu, object defaultValue) {
			var data = Config[menu];
			if (data == null) {
				data = defaultValue;
				Config[menu] = data;
				Changed = true;
			}
			object value = Config[menu];
			return value;
		}
		
		void LoadVariables() {

			radiation = Convert.ToBoolean(GetConfig("Radiation", true));
			auth = GetConfig("Auth", 1).ToString();
			
			if (Changed) {
				SaveConfig();
				Changed = false;
			}
		}
		
		void SetMode() {
			radiation = Convert.ToBoolean(GetConfig("Radiation", true));
			auth = GetConfig("Auth", 1).ToString();
			
			if (radiation == true) {
				server.radiation = true;
			} else if (radiation == false) {
				server.radiation = false;
			}
		}
		
		void LoadDefaultConfig() {
			Puts("Radiation: Creating a new config file");
			Config.Clear();
			LoadVariables();
		}

        [ChatCommand("rad")]
        void cmdChatRadar(BasePlayer player, string command, string[] args) {
			if (player.net.connection.authLevel >= Convert.ToInt32(auth)) {
            if (args.Length == 1) {
				if (args[0] == "off") {
					if (server.radiation == true) {
						server.radiation = false;
						SendReply(player, "Radiation: Off");
						radiation = server.radiation;
						Config["Radiation"] = "false";
						SaveConfig();
					} else {
						SendReply(player, "Radiation is already off.");
					}
				}
				if (args[0] == "on") {
					if (server.radiation == false) {
						server.radiation = true;
						SendReply(player, "Radiation: On");
						radiation = server.radiation;
						Config["Radiation"] = "true";
						SaveConfig();
					} else {
						SendReply(player, "Radiation is already on.");
					}
				}
			} else if (args.Length == 0) {
				SendReply(player, "Radiation Active: " + Convert.ToString(server.radiation) + ".");
				SendReply(player, "To enable/disable, use /rad [on/off].");
			}
			} else {
				SendReply(player, "Sorry, but you don't have permission to do that!");
			}
        }
    }
} 