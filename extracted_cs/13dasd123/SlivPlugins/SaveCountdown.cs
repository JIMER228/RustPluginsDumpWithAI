using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Save Countdown", "Dana", "1.1.2")]
    [Description("Broadcasts a countdown in chat and as a GUI for server saves.")]
    public class SaveCountdown : RustPlugin
    {
        #region Private Fields
        private PluginConfig _pluginConfig;
        private Timer _startTime, _endTimer, _counterTimer;
        private readonly Dictionary<ulong, Timer> _textTimers = new Dictionary<ulong, Timer>();
        #endregion Private Fields

        #region Hooks        

        void OnServerSave()
        {
            if (_pluginConfig.Config.ChatSaveStartEnabled)
                Server.Broadcast(Lang(Messages.ChatSaveStart));

            if (_pluginConfig.Config.GuiSaveStartEnabled)
                ShowGui(Lang(Messages.GuiSaveStart));

            if (_pluginConfig.Config.CountdownSeconds > 0)
            {
                var remainingTime = ConVar.Server.saveinterval - _pluginConfig.Config.CountdownSeconds;
                _startTime?.Destroy();
                _startTime = timer.Once(remainingTime, () =>
                {
                    if (_pluginConfig.Config.ChatSaveCountdownEnabled)
                    {
                        var counter = _pluginConfig.Config.CountdownSeconds;
                        _counterTimer?.Destroy();
                        _counterTimer = timer.Repeat(1, counter, () =>
                        {
                            if (_pluginConfig.Config.ChatSaveStartEnabled)
                                Server.Broadcast(Lang(Messages.ChatSaveCountdown, null, counter));
                            if (_pluginConfig.Config.GuiSaveStartEnabled)
                                ShowGui(Lang(Messages.GuiSaveCountdown, null, counter));
                            counter--;
                        });
                    }
                    else
                    {
                        if (_pluginConfig.Config.ChatSaveStartEnabled)
                            Server.Broadcast(Lang(Messages.ChatSaveCountdown, null, _pluginConfig.Config.CountdownSeconds));
                        if (_pluginConfig.Config.GuiSaveStartEnabled)
                            ShowGui(Lang(Messages.GuiSaveCountdown, null, _pluginConfig.Config.CountdownSeconds));
                    }
                });
            }

            if (_pluginConfig.Config.SaveCompleteSeconds > 0)
            {
                _endTimer?.Destroy();
                _endTimer = timer.Once(_pluginConfig.Config.SaveCompleteSeconds, () =>
                {
                    if (_pluginConfig.Config.ChatSaveStartEnabled)
                        Server.Broadcast(Lang(Messages.ChatSaveComplete));

                    if (_pluginConfig.Config.GuiSaveStartEnabled)
                        ShowGui(Lang(Messages.GuiSaveComplete));
                });
            }
        }
        protected override void LoadConfig()
        {
            var configPath = $"{Manager.ConfigPath}/{Name}.json";
            var newConfig = new DynamicConfigFile(configPath);
            if (!newConfig.Exists())
            {
                LoadDefaultConfig();
                newConfig.Save();
            }
            try
            {
                newConfig.Load();
            }
            catch (Exception ex)
            {
                RaiseError("Failed to load config file (is the config file corrupt?) (" + ex.Message + ")");
                return;
            }

            newConfig.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = newConfig.ReadObject<PluginConfig>();
            if (_pluginConfig.Config == null)
            {
                _pluginConfig.Config = new SaveCountdownConfig
                {
                    SaveCompleteSeconds = 10,
                    CountdownSeconds = 10,
                    GuiTextVisibilitySeconds = 3,
                    GuiSaveCountdownEnabled = true,
                    GuiSaveStartEnabled = true,
                    GuiSaveCompleteEnabled = true,
                    ChatSaveCountdownEnabled = true,
                    ChatSaveStartEnabled = true,
                    ChatSaveCompleteEnabled = true
                };
            }

            newConfig.WriteObject(_pluginConfig);
            PrintWarning("Config Loaded");
        }
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Messages.ChatSaveCountdown] = "Server Save will Start in {0}",
                [Messages.ChatSaveStart] = "Save Started",
                [Messages.ChatSaveComplete] = "Server Save Completed",
                [Messages.GuiSaveCountdown] = "Server Save will Start\n{0}",
                [Messages.GuiSaveStart] = "Save Started",
                [Messages.GuiSaveComplete] = "Server Save Completed"
            }, this);
        }
        void Unload()
        {
            _startTime?.Destroy();
            _counterTimer?.Destroy();
            _endTimer?.Destroy();
            foreach (var textTimer in _textTimers.Values)
            {
                textTimer?.Destroy();
            }
        }
        #endregion Hooks

        #region Commands

        #endregion Commands

        #region Methods

        public void ShowGui(string text)
        {
            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                ShowGui(basePlayer, text);
            }
        }
        public void ShowGui(BasePlayer player, string text)
        {
            var mainContainer = Ui.Container(Ui.Panels.Warning, Ui.Color(Ui.ColorCode.Black, 0f), Ui.GetMin(0, 0), Ui.GetMax(0, 0), parent: Ui.Panels.Under);
            Ui.OutlineLabel(ref mainContainer, Ui.Panels.Warning, text, 27, Ui.GetMin(0, 0), Ui.GetMax(0, 100),
                textColor: Ui.Color(Ui.ColorCode.SpanishOrange, 100), align: TextAnchor.UpperCenter);

            if (_textTimers.ContainsKey(player.userID))
            {
                _textTimers[player.userID]?.Destroy();
                _textTimers.Remove(player.userID);
            }

            Ui.ClearAllMenus(player);
            CuiHelper.AddUi(player, mainContainer);

            _textTimers[player.userID] = timer.In(_pluginConfig.Config.GuiTextVisibilitySeconds, () =>
            {
                Ui.ClearAllMenus(player);
                _textTimers.Remove(player.userID);
            });
        }
        public string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        #endregion Methods

        #region Classes
        private class PluginConfig
        {
            public SaveCountdownConfig Config { get; set; }
        }

        private class SaveCountdownConfig
        {
            [JsonProperty(PropertyName = "Countdown In Seconds")]
            public int CountdownSeconds { get; set; } = 10;

            [JsonProperty(PropertyName = "Save Complete In Seconds")]
            public int SaveCompleteSeconds { get; set; } = 10;

            [JsonProperty(PropertyName = "GUI - Text Visibility Duration - In Seconds")]
            public int GuiTextVisibilitySeconds { get; set; } = 3;

            [JsonProperty(PropertyName = "GUI - Save Start - Enabled")]
            public bool GuiSaveStartEnabled { get; set; } = true;

            [JsonProperty(PropertyName = "GUI - Save Countdown - Enabled")]
            public bool GuiSaveCountdownEnabled { get; set; } = true;

            [JsonProperty(PropertyName = "GUI - Save Complete - Enabled")]
            public bool GuiSaveCompleteEnabled { get; set; } = true;

            [JsonProperty(PropertyName = "Chat - Save Start - Enabled")]
            public bool ChatSaveStartEnabled { get; set; } = true;

            [JsonProperty(PropertyName = "Chat - Save Countdown - Enabled")]
            public bool ChatSaveCountdownEnabled { get; set; } = true;

            [JsonProperty(PropertyName = "Chat - Save Complete - Enabled")]
            public bool ChatSaveCompleteEnabled { get; set; } = true;
        }
        private static class Messages
        {
            public const string ChatSaveCountdown = "Chat - Save Countdown";
            public const string ChatSaveStart = "Chat - Save Start";
            public const string ChatSaveComplete = "Chat - Save Complete";
            public const string GuiSaveCountdown = "GUI - Save Countdown";
            public const string GuiSaveStart = "GUI - Save Start";
            public const string GuiSaveComplete = "GUI - Save Complete";
        }
        public static class Ui
        {
            public static float GetMinX(float left, int width = 1920) => left / width;
            public static float GetMaxX(float right, int width = 1920) => 1 - right / width;
            public static float GetMinY(float bottom, int height = 1080) => bottom / height;
            public static float GetMaxY(float top, int height = 1080) => 1 - top / height;
            public static string GetMin(float left, float bottom, int width = 1920, int height = 1080) => $"{GetMinX(left, width)} {GetMinY(bottom, height)}";
            public static string GetMax(float right, float top, int width = 1920, int height = 1080) => $"{GetMaxX(right, width)} {GetMaxY(top, height)}";

            public static CuiElementContainer Container(Panels panel, string color, string min, string max, bool useCursor = false, bool useBlur = false, Panels parent = Panels.Overlay)
            {
                var container = new CuiElementContainer
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color, Material = useBlur ? "assets/content/ui/uibackgroundblur.mat" : "Assets/Icons/IconMaterial.mat"},
                            RectTransform = {AnchorMin = min, AnchorMax = max},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent == Panels.HudMenu ? "Hud.Menu" : parent.ToString(),
                        panel.ToString()
                    }
                };
                return container;
            }
            public static void OutlineLabel(ref CuiElementContainer container, Panels panel, string text, int size, string min, string max, string font = Fonts.RobotoCondensedRegular,
                string textColor = "1.0 1.0 1.0 1.0", TextAnchor align = TextAnchor.MiddleCenter, float fadeIn = 0, string outlineDistance = "0.5 0.5", string outlineColor = "0 0 0 1.0")
            {
                CuiElement textElement = new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel.ToString(),
                    FadeOut = fadeIn,
                    Components =
                {
                    new CuiTextComponent
                    {
                        Text = text,
                        FontSize = (int)(size * 0.7f),
                        Align = align,
                        FadeIn = fadeIn,
                        Font = font,
                        Color = textColor
                    },
                    new CuiOutlineComponent
                    {
                        Distance = outlineDistance,
                        Color = outlineColor
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = min,
                        AnchorMax = max
                    }
                }
                };
                container.Add(textElement);
            }
            public static string Color(string hexColor, float alpha = 1)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.Substring(1);
                var red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                var green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                var blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
            public static void ClearAllMenus(BasePlayer player)
            {
                foreach (var name in Enum.GetNames(typeof(Panels)))
                {
                    CuiHelper.DestroyUi(player, name);
                }
            }
            public enum Panels
            {
                Overall,
                Overlay,
                Hud,
                HudMenu,
                Under,
                Warning
            }
            public class ColorCode
            {
                public const string Black = "#000000", White = "#FFFFFF";
                public const string SpanishOrange = "#E26C16";
            }
            public class Fonts
            {
                public const string DroidSansMono = "DroidSansMono.ttf";
                public const string PermanentMarker = "PermanentMarker.ttf";
                public const string RobotoCondensedBold = "RobotoCondensed-Bold.ttf";
                public const string RobotoCondensedRegular = "RobotoCondensed-Regular.ttf";
            }
        }
        #endregion Classes
    }
}