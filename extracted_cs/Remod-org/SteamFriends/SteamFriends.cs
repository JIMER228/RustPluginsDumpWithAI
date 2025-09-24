#region License (GPL v2)
/*
    Copyright (c) 2025 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License v2.0.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion License (GPL v2)

// Reference: System.Net.Http
using Newtonsoft.Json;
using Oxide.Core;
using System.Collections.Generic;
using System.Net.Http;

namespace Oxide.Plugins
{
    [Info("SteamFriends", "RFC1920", "1.0.0")]
    [Description("Library plugin to lookup Steam friends")]
    internal class SteamFriends : RustPlugin
    {
        private ConfigData configData;
        private Dictionary<string, List<string>> steamFriends = new();

        private void DoLog(string message, int indent = 0)
        {
            if (configData.debug)
            {
                Puts("".PadLeft(indent, ' '));
            }
        }
        private void OnServerInitialized()
        {
            LoadConfigVariables();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (configData.steamApiKey?.Length > 0) GetSteamFriends(player);
        }

        #region Inbound_Hooks
        private List<string> GetFriends(BasePlayer player)
        {
            return steamFriends[player.UserIDString] ?? null;
        }

        private List<string> GetFriends(string UserIDString)
        {
            return steamFriends[UserIDString] ?? null;
        }

        private List<string> GetFriends(int UserID)
        {
            return steamFriends[UserID.ToString()] ?? null;
        }
        #endregion Inbound_Hooks

        private async void GetSteamFriends(BasePlayer player)
        {
            if (configData.steamApiKey == null) return;
            DoLog($"Getting Steam friends for {player?.UserIDString}");
            SteamFriendsAPI steamResponse = new();
            using HttpClient httpClient = new();
            //string request = $"http://api.steampowered.com/ISteamUser/GetFriendList/v0001/?key={configData.Options.steamApiKey}&steamid={player.UserIDString}&relationship=friend";
            string request = $"https://api.steampowered.com/ISteamUser/GetFriendList/v0001/?key={configData.steamApiKey}&steamid={player.UserIDString}&relationship=friend";
            using HttpRequestMessage httpReq = new(HttpMethod.Get, request);
            using HttpResponseMessage httpResponse = await httpClient.SendAsync(httpReq);

            if (httpResponse?.ReasonPhrase == "Unauthorized")
            {
                DoLog("Unauthorized - Player most likely has their profile set to private or has some exclusion set for their friends list.");
                return;
            }

            if (httpResponse?.IsSuccessStatusCode == true)
            {
                string responseString = await httpResponse.Content.ReadAsStringAsync();
                {
                    if (string.IsNullOrWhiteSpace(responseString))
                    {
                        return;
                    }
                    steamResponse = JsonConvert.DeserializeObject<SteamFriendsAPI>(responseString);
                }
            }

            List<string> friends = new();
            foreach (Friend friend in steamResponse?.friendslist)
            {
                DoLog($"Adding Steam friend for {player?.UserIDString} {friend.steamid}", 1);
                friends.Add(friend.steamid);
            }
            if (friends.Count > 0)
            {
                if (!steamFriends.ContainsKey(player?.UserIDString))
                {
                    steamFriends.Add(player?.UserIDString, friends);
                    return;
                }
                steamFriends[player?.UserIDString] = friends;
            }
        }

        public class Friend
        {
            public string steamid { get; set; }
            public string relationship { get; set; }
            public int friend_since { get; set; }
        }

        public class Friendslist
        {
            public List<Friend> friends { get; set; }

            public IEnumerator<Friend> GetEnumerator()
            {
                return friends.GetEnumerator();
            }
        }

        public class SteamFriendsAPI
        {
            public Friendslist friendslist { get; set; }
        }

        #region Config
        private class ConfigData
        {
            public string steamApiKey;
            public bool debug;
            public VersionNumber Version;
        }

        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();
            configData.Version = Version;
            SaveConfig(configData);
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new()
            {
                debug = false,
                Version = Version
            };

            SaveConfig(config);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
        #endregion Config
    }
}
