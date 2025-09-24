// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("LicenseCheck", "VooDoo", "1.0.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    [Description("")]
    public class LicenseCheck : CovalencePlugin
    {
        private const string SteamApi = "http://api.steampowered.com/";
        private const string SteamInfo = "ISteamUser/GetPlayerSummaries/v0002/?key={0}&steamids={1}";
      //  Слив плагинов server-rust by Apolo YouGame
        private const string SteamRecentlyGames = "IPlayerService/GetRecentlyPlayedGames/v0001/?key={0}&steamid={1}&format=json";

        private const string ApiKey = "";
        private const int NeedMinutes = 600;

        private T Deserialise<T>(string json) => JsonConvert.DeserializeObject<T>(json);

        private class PlayerInfo
      //  Слив плагинов server-rust by Apolo YouGame
        {
            [JsonProperty("response")]
            public Content Response;

            public class Content
            {
                [JsonProperty("players")]
                public Player[] Players;

                public class Player
                {
                    [JsonProperty("gameid")]
                    public int GameId;
                }
            }
        }

        private class RecentlyGames
        {
            [JsonProperty("response")]
            public Content Response;

            public class Content
            {
                [JsonProperty("games")]
                public Games[] games;

                public class Games
                {
                    [JsonProperty("appid")]
                    public int appid;

                    [JsonProperty("playtime_forever")]
                    public int playtime_forever;
                }
            }
        }

        private enum ResponseCode
        {
            Valid = 200,
            InvalidKey = 403,
            Unavailable = 503,
        }

        private bool IsValidRequest(ResponseCode code)
        {
            switch (code)
            {
                case ResponseCode.Valid:
                    return true;

                case ResponseCode.InvalidKey:
                    Puts("ErrorInvalidKey");
                    return false;

                case ResponseCode.Unavailable:
                    Puts("ErrorServiceUnavailable");
                    return false;

                default:
                    Puts(string.Format("ErrorUnknown", code.ToString()));
                    return false;
            }
        }


        void OnUserConnected(IPlayer player)
        {
            webrequest.Enqueue(string.Format(SteamApi + SteamInfo, ApiKey, player.Id), null, (code, response) =>
      //  Слив плагинов server-rust by Apolo YouGame
            {
                    if (!IsValidRequest((ResponseCode)code))
                    {
                        player.Kick($"Failed steam request");
                        LogToFile("failedrequest", $"{player.Name} kicked, rust failedwebrequest {player.Id}", this);
                        return;
                    }

                    PlayerInfo Info = Deserialise<PlayerInfo>(response);
      //  Слив плагинов server-rust by Apolo YouGame

                    switch (Info.Response.Players[0].GameId)
      //  Слив плагинов server-rust by Apolo YouGame
                    {
                        case 252490:
                            {
                                break;
                            }
                        case 480:
                            {
                                CheckHours(player);
                                break;
                            }
                    }            
            }, this);
        }

        void CheckHours(IPlayer player)
        {
            webrequest.Enqueue(string.Format(SteamApi + SteamRecentlyGames, ApiKey, player.Id), null, (code, response) =>
            {
                    if (!IsValidRequest((ResponseCode)code))
                    {
                        player.Kick($"Failed steam request");
                        LogToFile("failedrequest", $"{player.Name} kicked, 480 failedwebrequest {player.Id}", this);
                        return;
                    }

                    RecentlyGames RecentlyGames = Deserialise<RecentlyGames>(response);
                    Dictionary<int, int> Games = new Dictionary<int, int>();

                    foreach (var RecentlyGame in RecentlyGames.Response.games)
                    {
                        Games.Add(RecentlyGame.appid, RecentlyGame.playtime_forever);
                    }

                    if (!Games.ContainsKey(480) || Games[480] < NeedMinutes)
                    {
                        player.Kick($"You must have 10 hours to play on this server");
                    }
            }, this);
        }
    }
}
