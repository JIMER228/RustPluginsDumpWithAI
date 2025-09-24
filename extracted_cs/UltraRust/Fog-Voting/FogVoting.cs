using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Fog Voting", "Ultra", "1.1.2")]
    [Description("Initializes voting to remove fog from the environment")]

    class FogVoting : RustPlugin
    {
        #region Fields

        bool isVotingOpen = false;
        Timer fogCheckTimer;
        Timer votingTimer;
        Timer votingPanelRefreshTimer;
        DateTime votingStart;
        Dictionary<string, bool> currentVoting = new Dictionary<string, bool>();

        // CUI panels
        static CuiPanel votingPanel;
        string votingPanelName = "votingPanelName";
        string fogYesPanelName = "fogYesPanelName";
        string fogNoPanelName = "fogNoPanelName";
        string votingScorePanelName = "votingScorePanelName";


        #endregion

        #region Oxide Hooks

        void OnServerInitialized()
        {
            InitCUI();
            CheckCurrentFog();
        }

        void Unload()
        {
            DestroyVotingPanel();

            DestroyTimer(fogCheckTimer);
            DestroyTimer(votingTimer);
            DestroyTimer(votingPanelRefreshTimer);
        }

        #endregion

        #region Chat Commands

        [ChatCommand("fogyes")]
        void FogYes(BasePlayer player)
        {
            if (!isVotingOpen) return;
            if (currentVoting.ContainsKey(player.UserIDString))
            {
                currentVoting[player.UserIDString] = true;
            }
            else
            {
                currentVoting.Add(player.UserIDString, true);
            }
        }

        [ChatCommand("fogno")]
        void FogNo(BasePlayer player)
        {
            if (!isVotingOpen) return;
            if (currentVoting.ContainsKey(player.UserIDString))
            {
                currentVoting[player.UserIDString] = false;
            }
            else
            {
                currentVoting.Add(player.UserIDString, false);
            }
        }

        [ChatCommand("setfog")]
        void SetFog(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin)
            {
                float fogValue = 0;
                if (args.Length == 1 && float.TryParse(args[0], out fogValue))
                {
                    if (fogValue >= 0F && fogValue <= 1F)
                    {
                        ConsoleSystem.Run(ConsoleSystem.Option.Server, $"weather.fog {fogValue}");
                    }
                }
            }
        }

        [ChatCommand("checkfog")]
        void CheckFog(BasePlayer player, string command, string[] args)
        {
            if (isVotingOpen) return;
            if (player.IsAdmin)
            {
                CheckCurrentFog();
            }
        }

        #endregion

        #region Core

        void CheckCurrentFog()
        {
            DestroyTimer(fogCheckTimer);

            foreach (var player in BasePlayer.activePlayerList)
            {
                if (Climate.GetFog(player.transform.position) > configData.FogLimit)
                {
                    OpenVoting();
                    return;
                }
            }

            fogCheckTimer = timer.Once(configData.FogCheckInterval, () => CheckCurrentFog());
        }

        void OpenVoting()
        {
            isVotingOpen = true;
            currentVoting = new Dictionary<string, bool>();
            votingStart = DateTime.UtcNow;
            ShowVotingPanel();
            votingTimer = timer.Once(configData.VotingDuration, () => CloseVoting());
        }

        void CloseVoting()
        {
            isVotingOpen = false;
            DestroyVotingPanel();

            if (currentVoting.Count > 0 && currentVoting.Where(w => w.Value).Count() < currentVoting.Where(w => !w.Value).Count())
            {
                ConsoleSystem.Run(ConsoleSystem.Option.Server, "weather.fog 0");
                fogCheckTimer = timer.Once(configData.FogCheckInterval, () => CheckCurrentFog());
            }
            else
            {
                // longer interval for another check after unsuccessful voting to remove fog
                fogCheckTimer = timer.Once(configData.FogCheckInterval * 10, () => CheckCurrentFog());
            }
        }

        void DestroyTimer(Timer timer)
        {
            timer?.DestroyToPool();
            timer = null;
        }

        void DestroyVotingPanel()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, votingPanelName);
            }
        }

        #endregion

        #region Config

        private ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Fog value to start voting 0.0 - 1.0")]
            public float FogLimit;

            [JsonProperty(PropertyName = "Interval to check the current fog (seconds)")]
            public float FogCheckInterval;

            [JsonProperty(PropertyName = "Voting duration (seconds)")]
            public float VotingDuration;
        }

        protected override void LoadConfig()
        {
            try
            {
                base.LoadConfig();
                configData = Config.ReadObject<ConfigData>();
                if (configData == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                LoadDefaultConfig();
            }

            if (configData.FogLimit < 0 || configData.FogLimit > 1) configData.FogLimit = 0.1F;
            if (configData.FogCheckInterval < 1) configData.FogCheckInterval = 60;
            if (configData.VotingDuration < 10) configData.VotingDuration = 30;

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            configData = new ConfigData()
            {
                FogLimit = 0.3F,
                FogCheckInterval = 60,
                VotingDuration = 30
            };
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(configData, true);
            base.SaveConfig();
        }

        #endregion

        #region CUI
        
        void InitCUI()
        {
            votingPanel = new CuiPanel
            {
                CursorEnabled = false,
                RectTransform =
                    {
                        AnchorMin = $"0.4 0.87",
                        AnchorMax = $"0.6 0.91"
                    },
                Image =
                 { Color = "0 0 0 0.8" }
            };
        }

        void ShowVotingPanel()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, votingPanelName);

                if (isVotingOpen)
                {
                    var container = new CuiElementContainer();
                    container.Add(votingPanel, name: votingPanelName);

                    // fog yes 
                    container.Add(GetPanel(anchorMin: "0 0", anchorMax: "0.2 1"), votingPanelName, name: fogYesPanelName);
                    container.Add(GetLabel("/fog<color=#89b38a>yes</color>", align: TextAnchor.MiddleRight), fogYesPanelName);

                    // voting score 
                    container.Add(GetPanel(anchorMin: "0.2 0", anchorMax: "0.8 1"), votingPanelName, name: votingScorePanelName);
                    container.Add(GetLabel($"{currentVoting.Where(w => w.Value).Count()} : {currentVoting.Where(w => !w.Value).Count()}", align: TextAnchor.MiddleCenter, size: 18), votingScorePanelName);

                    // fog no 
                    container.Add(GetPanel(anchorMin: "0.8 0", anchorMax: "1 1"), votingPanelName, name: fogNoPanelName);
                    container.Add(GetLabel("/fog<color=#89b38a>no</color>", align: TextAnchor.MiddleLeft), fogNoPanelName);

                    // progress bar
                    float progress = (float)(DateTime.UtcNow - votingStart).TotalSeconds / configData.VotingDuration;
                    container.Add(GetPanel(anchorMin: "0 0", anchorMax: $"{progress} 0.1", color: "1 1 1 0.4"), votingPanelName, name: fogNoPanelName);

                    CuiHelper.AddUi(player, container);
                }
            }

            if(isVotingOpen) votingPanelRefreshTimer = timer.Once(2F, () => ShowVotingPanel());
        }

        CuiPanel GetPanel(string anchorMin = "0 0", string anchorMax = "1 1", string color = "0.1 0.1 0.1 0", bool cursorEnabled = false)
        {
            return new CuiPanel
            {
                CursorEnabled = cursorEnabled,
                RectTransform =
                {
                    AnchorMin = anchorMin,
                    AnchorMax = anchorMax
                },
                Image = { Color = color }
            };
        }

        CuiLabel GetLabel(string text, int size = 14, string anchorMin = "0.05 0.02", string anchorMax = "0.98 0.9", TextAnchor align = TextAnchor.MiddleCenter, string color = "1 1 1 1", string font = "robotocondensed-regular.ttf")
        {
            return new CuiLabel
            {
                Text =
                    {
                        Text = text,
                        FontSize = size,
                        Align = align,
                        Color = color,
                        Font = font
                    },
                RectTransform =
                    {
                        AnchorMin = anchorMin,
                        AnchorMax = anchorMax
                    },
            };
        }
       
        #endregion
    }
}
