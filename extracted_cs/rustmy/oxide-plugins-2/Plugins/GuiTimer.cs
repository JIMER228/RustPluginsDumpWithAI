/*
TODO:
- Finish renaming and cleaning up variables and methods
- Consolidate var time and var formatted
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("GuiTimer", "Wulf/lukespragg", "2.0.0", ResourceId = 0)]
    [Description("A simple on-screen GUI timer, useful for events and such")]

    class GuiTimer : CovalencePlugin
    {
        #region Initialization

        readonly List<ulong> guiInfo = new List<ulong>();

        const string permUse = "guitimer.use";

        int currentTimer;
        Timer screent;
        string title;

        void Init()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            permission.RegisterPermission(permUse, this);
        }

        #endregion

        #region Configuration

        string panelColor;
        string panelPosition;
        string titleColor;
        string titlePosition;
        int titleSize;
        string timerColor;
        string timerPosition;
        int timerSize;

        protected override void LoadDefaultConfig()
        {
            Config["PanelColor"] = panelColor = GetConfig("PanelColor", "0.8 0.8 0.8 0.2");
            Config["PanelPosition"] = panelPosition = GetConfig("PanelPosition", "0.84 0.92, 0.987 0.98");
            Config["TimerColor"] = timerColor = GetConfig("TimerColor", "white");
            Config["TimerSize"] = timerSize = GetConfig("TimerSize", 14);
            Config["TimerPosition"] = timerPosition = GetConfig("TimerPosition", "0 0.1, 1 0.5");
            Config["TitleColor"] = titleColor = GetConfig("TitleColor", "white");
            Config["TitlePosition"] = titlePosition = GetConfig("TitlePosition", "0 0.1, 1 0.9");
            Config["TitleSize"] = titleSize = GetConfig("TitleSize", 12);
            Config.Save();
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandUsage"] = "Usage: timer # title (replace # with a valid number)",
                ["CurrentTimer"] = "The current timer is at {0}",
                ["NotAllowed"] = "Sorry, you can't use '{0}' right now",
                ["NoTimer"] = "There is no current timer",
                ["TimerEnded"] = "You have ended the current timer",
                ["TimerStarted"] = "You have started a new {0} timer"
            }, this);
        }

        #endregion

        #region Timer

        void CreateTimer(int seconds)
        {
            screent = timer.Repeat(1f, seconds, () =>
            {
                currentTimer = currentTimer - 1;
                RefreshUi();
                if (currentTimer == 0) timer.Once(1f, DestroyUi);
            });
        }

        #endregion

        #region Chat Commands

        [Command("timer", "global.timer")]
        void TimerChat(IPlayer player, string command, string[] args)
        {
            if (player.Id != "server_console" && !HasPermission(player.Id, permUse))
            {
                player.Reply(Lang("NotAllowed", player.Id, command));
                return;
            }

            var time = TimeSpan.FromSeconds(currentTimer);
            var formatted = $"{time.Days:00}d {time.Hours:00}h {time.Minutes:00}m {time.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');

            if (args.Length == 0)
            {
                player.Reply(screent != null ? Lang("CurrentTimer", player.Id, formatted) : Lang("NoTimer", player.Id));
                return;
            }

            if (args[0] == "destroy")
            {
                if (screent == null)
                {
                    player.Reply(Lang("NoTimer", player.Id));
                    return;
                }

                screent.Destroy();
                player.Reply(Lang("TimerEnded", player.Id));
                DestroyUi();
                return;
            }

            int.TryParse(args[0], out currentTimer);
            if (currentTimer <= 0)
            {
                player.Reply(Lang("CommandUsage", player.Id));
                return;
            }

            time = TimeSpan.FromSeconds(currentTimer);
            formatted = $"{time.Days:00}d {time.Hours:00}h {time.Minutes:00}m {time.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
            title = args.Length >= 1 ? string.Join(" ", args.Skip(1).ToArray()) : "Timer";
            CreateTimer(currentTimer);
            OpenUiAll();

            player.Reply(Lang("TimerStarted", player.Id, formatted));
        }

        #endregion

        #region GUI

        void OnPlayerInit(BasePlayer player)
        {
            if (guiInfo.Contains(player.userID)) return;

            if (screent == null && guiInfo.Contains(player.userID)) DestroyUii(player);
            else if (screent != null && !guiInfo.Contains(player.userID)) OpenUi(player);
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            if (guiInfo.Contains(player.userID)) DestroyUii(player);
        }

        void RefreshUi()
        {
            DestroyUi();
            OpenUiAll();
        }

        void OpenUiAll()
        {
            foreach (var player in BasePlayer.activePlayerList) OpenUi(player);
        }

        void OpenUi(BasePlayer player)
        {
            guiInfo.Add(player.userID);
            GuiCreate(player);
        }

        void GuiCreate(BasePlayer player)
        {
            var time = TimeSpan.FromSeconds(currentTimer);
            var formatted = $"{time.Days:00}d {time.Hours:00}h {time.Minutes:00}m {time.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');

            var element = new CuiElementContainer();
            var panel = element.Add(new CuiPanel
            {
                Image =
                {
                    Color = panelColor
                },
                RectTransform =
                {
                    AnchorMin = panelPosition.Split(',')[0],
                    AnchorMax = panelPosition.Split(',')[1].Trim(' ')
                },
                CursorEnabled = false
            }, "Hud.Under", "GuiTimer");
            element.Add(new CuiLabel
            {
                Text =
                {
                    Text = title,
                    FontSize = titleSize,
                    Align = TextAnchor.UpperCenter,
                    Color = titleColor.ToLower()
                },
                RectTransform =
                {
                    AnchorMin = titlePosition.Split(',')[0],
                    AnchorMax = titlePosition.Split(',')[1].Trim(' ')
                }
            }, panel);
            element.Add(new CuiLabel
            {
                Text =
                {
                    Text = formatted,
                    FontSize = timerSize,
                    Align = TextAnchor.LowerCenter,
                    Color = timerColor.ToLower()
                },
                RectTransform =
                {
                    AnchorMin = timerPosition.Split(',')[0],
                    AnchorMax = timerPosition.Split(',')[1].Trim(' ')
                }
            }, panel);

            CuiHelper.AddUi(player, element);
        }

        void DestroyUi()
        {
            foreach (var player in BasePlayer.activePlayerList) DestroyUii(player);
        }

        void DestroyUii(BasePlayer player)
        {
            guiInfo.Remove(player.userID);
            CuiHelper.DestroyUi(player, "GuiTimer");
        }

        void Unload() => DestroyUi();

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T) Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        bool HasPermission(string id, string perm) => permission.UserHasPermission(id, perm);

        #endregion
    }
}
