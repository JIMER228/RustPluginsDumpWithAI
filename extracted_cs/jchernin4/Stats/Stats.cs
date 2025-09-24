using Oxide.Core;
using Oxide.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxide.Plugins {
	public class Stats : RustPlugin {
		private DynamicConfigFile statsFile;
		void Init() {
			statsFile = Interface.Oxide.DataFileSystem.GetDatafile("Stats");
		}

		object OnPlayerDeath(BasePlayer player, HitInfo info) {
			statsFile = Interface.Oxide.DataFileSystem.GetDatafile("Stats");
			BasePlayer killer = info?.Initiator as BasePlayer;
			if (killer == null || killer == player || player.IsNpc || killer.IsNpc) {
				return null;
			}
			statsFile[killer.UserIDString, "kills"] = (int)statsFile[killer.UserIDString, "kills"] + 1;
			statsFile[player.UserIDString, "deaths"] = (int)statsFile[player.UserIDString, "deaths"] + 1;
			statsFile.Save();

			return null;
		}

		void OnPlayerSpawn(BasePlayer player) {
			statsFile = Interface.Oxide.DataFileSystem.GetDatafile("Stats");
			statsFile[player.UserIDString, "name"] = player.displayName;
			statsFile[player.UserIDString, "kills"] = 0;
			statsFile[player.UserIDString, "deaths"] = 0;
			statsFile.Save();
		}


		[ChatCommand("stats")]
		void ShowStats(BasePlayer player, string command, string[] args) {
			statsFile = Interface.Oxide.DataFileSystem.GetDatafile("Stats");
			string statsMessage = "";

			statsMessage += "<color=#02f0e8>Kills: </color>" + statsFile[player.UserIDString, "kills"];
			statsMessage += "\n<color=#02f0e8>Deaths: </color>" + statsFile[player.UserIDString, "deaths"];

			rust.SendChatMessage(player, "", statsMessage);
		}

		/*[ChatCommand("topstats")]
		void ShowTopStats(BasePlayer player, string command, string[] args) {
			statsFile = Interface.Oxide.DataFileSystem.GetDatafile("Stats");
			List<KeyValuePair<string, object>> statsList = statsFile.ToList();
		}*/
	}
}
