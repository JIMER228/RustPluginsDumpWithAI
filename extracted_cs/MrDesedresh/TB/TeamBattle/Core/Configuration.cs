// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins.TeamBattle
{
    public static class Configuration
    {
        public const int MAX_ROOMS = 4;
        public const int MAX_TEAM_PLAYERS = 5;
        public const int DEFAULT_COUNTDOWN = 60;
        public const int FAST_COUNTDOWN = 10;
        public const float RESPAWN_DELAY = 5f;
        public const float GAME_DURATION = 300f;
        
        public static Dictionary<int, Dictionary<string, List<Vector3>>> ManualSpawnPoints { get; private set; }
        
        public static void Init(TeamBattle plugin)
        {
            ManualSpawnPoints = new Dictionary<int, Dictionary<string, List<Vector3>>>
            {
                [1] = new Dictionary<string, List<Vector3>>
                {
                    ["Team1"] = new List<Vector3>
                    {
                        new Vector3(68.46f, 5.01f, 47.67f),
                        new Vector3(69f, 5.01f, 36.81f),
                        new Vector3(68.5f, 5.01f, 54.51f),
                        new Vector3(65.28f, 5.01f, 68.76f),
                        new Vector3(67.28f, 5.01f, 25.14f)
                    },
                    ["Team2"] = new List<Vector3>
                    {
                        new Vector3(124.03f, 5f, 25.46f),
                        new Vector3(121.39f, 5f, 39.64f),
                        new Vector3(122.07f, 5f, 45.6f),
                        new Vector3(122.72f, 5f, 57.33f),
                        new Vector3(121.82f, 5f, 68.51f)
                    }
                },
                // Остальные комнаты...
            };
            
            plugin.permission.RegisterPermission("teambattle.admin", plugin);
            plugin.permission.RegisterPermission("teambattle.leave", plugin);
        }
    }
}