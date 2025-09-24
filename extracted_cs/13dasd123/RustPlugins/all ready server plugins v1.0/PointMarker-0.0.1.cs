using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("PointMarker", "Hougan", "0.0.1")]
    public class PointMarker : RustPlugin
    {
        #region Classes

        private class Marker : MonoBehaviour
        {
            private static string Layer = "UI_Layer";
            List<BasePlayer> TeamMates = new List<BasePlayer>();
            public BasePlayer Player;

            public void Awake()
            {
                Player = this.GetComponent<BasePlayer>();
                
                CuiHelper.DestroyUi(Player, "MainLayer");
                CuiHelper.AddUi(Player, new List<CuiElement>
                {
                    {
                        new CuiElement
                        {
                            Parent     = "Hud",
                            Name       = "MainLayer",
                            Components =
                            {
                                new CuiImageComponent { Color = "0 0 0 0" },
                                new CuiRectTransformComponent { AnchorMin = $"{0.33} 0.95", AnchorMax = $"{0.67} 1", OffsetMin = "-5 -10", OffsetMax = "5 0" },
                            }
                        }
                    }
                });
            }


            public float TeamUpdate = 0f;
            public float LastUpdate = 0f;

            public void OnDestroy()
            {
                CuiHelper.DestroyUi(Player, "MainLayer");
            }
            
            public void Update()
            {
                LastUpdate += Time.deltaTime;
                TeamUpdate += Time.deltaTime;
                
                if (TeamUpdate > 1)
                {
                    TeamMates.Clear();
                    if (Player.currentTeam != 0)
                    {
                        foreach (var check in RelationshipManager._instance.FindTeam(Player.currentTeam).members)
                        {
                            if (check == Player.userID) continue;
                            var mate = BasePlayer.FindByID(check);
                            if (mate == null || !mate.IsConnected) continue;
                        
                            TeamMates.Add(mate);
                        }
                    } 

                    TeamUpdate = 0;
                }
                if (LastUpdate > config.DelayUpdate)
                {
                    LastUpdate = 0f;
                    CuiHelper.DestroyUi(Player, "MainLayer");
                    CuiHelper.AddUi(Player, new List<CuiElement>
                    {
                        {
                            new CuiElement
                            {
                                Parent     = "Hud",
                                Name       = "MainLayer",
                                Components =
                                {
                                    new CuiImageComponent { Color = "0 0 0 0" },
                                    new CuiRectTransformComponent { AnchorMin = $"{0.33} 0.95", AnchorMax = $"{0.67} 1", OffsetMin = "-5 -10", OffsetMax = "5 0" },
                                }
                            }
                        }
                    });
                    
                    int i = 0;
                    foreach (var check in TeamMates)
                    {
                        var playerRay = Player.eyes.BodyForward().XZ2D();
                        var curPos = Player.transform.position.XZ2D();
                        var tarPos = check.transform.position.XZ2D();

                        var x = Vector2.SignedAngle(playerRay, tarPos - curPos);
                    
                        float pos = x / 45 * -1;
                        if (x > 45) pos = -1;
                        else if (x < -45) pos = 1;
                        
                        CuiHelper.AddUi(Player, new List<CuiElement>
                        {
                            {
                                new CuiElement
                                {
                                    Parent   = "MainLayer",
                                    Name = Layer + check.userID,
                                    Components =
                                    {
                                        new CuiImageComponent { Color = HexToRustFormat(config.PlayerColors[i]) },
                                        new CuiRectTransformComponent { AnchorMin = $"{0.5 + pos / 2} 1", AnchorMax = $"{0.5 + pos / 2} 1", OffsetMin = "-5 -10", OffsetMax = "5 0" },
                                    }
                                }
                            }
                        });
                        
                        i++;
                    }
                }
                
            }
        }

        private class PlayerSetting
        {
            [JsonProperty("Отображать маркеры")]
            public bool DisplayMarkers;

            public PlayerSetting(bool display)
            {
                this.DisplayMarkers = display;
            }
        }

        private class Configuration
        {
            [JsonProperty("Интервал обновления, не рекомендуется ставить ниже чем 0.00{DarkPluginsID}")]
            public float DelayUpdate = 0.01f;
            [JsonProperty("Автоматически включать отображение маркеров для новых игроков")]
            public bool DefaultOn = true;
            [JsonProperty("Цвета для тиммейтов по порядку")]
            public List<string> PlayerColors = new List<string>();

            public static Configuration GetNewConfiguration()
            {
                return new Configuration { PlayerColors = new List<string> { "#4286f4FF", "#f44141FF", "#f49a41FF", "#f4f441FF", "#94f441FF", "#41f4caFF", "#9a41f4FF", "#f44182FF", } };
            }
        }
        
        #endregion

        #region Variables

        private static Configuration config;
        private static Dictionary<ulong, PlayerSetting> PlayerSettings = new Dictionary<ulong, PlayerSetting>();

        #endregion

        #region Initialization

        private void OnServerInitialized()
        {
            if (config.DelayUpdate < 0.01f) { PrintError($"Do not set 'DelayUpdate' smaller than '0.01', it can cause server performance issues!"); }
            permission.RegisterPermission("pointmarker.use", this);
            BasePlayer.activePlayerList.ForEach(OnPlayerInit);
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (config.DefaultOn) player.gameObject.AddComponent<Marker>();
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player.GetComponent<Marker>()) UnityEngine.Object.Destroy(player.GetComponent<Marker>());
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config?.PlayerColors == null) LoadDefaultConfig();
            }
            catch 
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Commands

        [ChatCommand("markers")]
        private void cmdChatMarkers(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "pointmarker.use")) return;
            if (args.Length == 0)
            {
                player.ChatMessage($"Вы не правильно используете команду!\n" +
                                   $" - /markers on -> включить отображение друзей на компасе\n" +
                                   $" - /markers off -> выключить отображение друзей на компасе");
                return;
            }

            switch (args[0].ToLower())
            {
                case "on":
                {
                    if (player.GetComponent<Marker>() != null)
                    {
                        player.ChatMessage("У вас уже включено отображение друзей на компасе!");
                        return;
                    }

                    player.gameObject.AddComponent<Marker>();
                    player.ChatMessage($"Вы успешно включили отображение друзей на компасе!");
                    break;
                }
                case "off":
                {
                    if (player.GetComponent<Marker>() == null)
                    {
                        player.ChatMessage($"У вас уже выключено отображение друзей на компасе!");
                        return;
                    }
                    
                    UnityEngine.Object.Destroy(player.GetComponent<Marker>());
                    player.ChatMessage($"Вы успешно выключили отображение друзей на компасе!");
                    break;
                }
            }
        }

        #endregion
        
        #region Utils
        
        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        #endregion

        private void Unload()
        {
            foreach (var check in UnityEngine.Object.FindObjectsOfType<Marker>())
                UnityEngine.Object.Destroy(check);
        }
    }
}