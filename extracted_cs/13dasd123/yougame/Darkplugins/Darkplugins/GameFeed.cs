//Requires: ImageLibrary

using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using System.IO;
using Oxide.Core.Plugins;

namespace Oxide.Plugins {
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("GameFeed", "https://discord.gg/dNGbxafuJn", "1.1.1")]
    [Description("Ingame Feed API, could be used by other plugins")]
    /*
    Example usage from other plugin:

        [PluginReference]
        Plugin GameFeed;

        [ChatCommand("test")]
        private void testcmd(BasePlayer player, string cmd, string[] argv) {
            GameFeed?.Call("Broadcast", "Many example info words Many example info words Many example info words Many example info words ", "info");
            GameFeed?.Call("Broadcast", "You don't have permission to use this command, please contact to admins, and more text", "error");
            GameFeed?.Call("Broadcast", "it's an unknown style", "unknown style");
            GameFeed?.Call("Broadcast", "Patrol helicopter is inbound!", "warning");
            GameFeed?.Call("Broadcast", "You've reached level 4", "note");
            GameFeed?.Call("Broadcast", "inline <size=16><color=orange><b>Markup</b></color></size> also works", "note");

            BasePlayer.activePlayerList.ForEach(pl => {
                GameFeed?.Call("SendToPlayer", pl, $"Individual message to player <b><color=white>{pl.displayName}</color></b>, with duration 10 seconds", "warning", 10);
            });
        }

    */
    public class GameFeed : RustPlugin {
        [PluginReference]
        ImageLibrary ImageLibrary;

        public static GameFeed Instance;
        public const string permSee = "gamefeed.see";

        private Dictionary<BasePlayer, MessageQueue> messageQueues = new Dictionary<BasePlayer, MessageQueue>();

        #region CONFIG
        class PluginConfig {
            [JsonProperty("Accepted Window size (readonly)")]
            public Vector2 WindowSize { get { return new Vector2(1280, 720); } }
            public Vector2 MessageSize = new Vector2(356, 40);
            public Vector2 FeedOffset = new Vector2(10, 10);

            [JsonProperty("Transition Duration (may cause flickering)")]
            public float TransitionDuration = 0f;

            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty("HorizontalAlignment (Left, Right)")]
            public Horizontal HorizontalAlignment = Horizontal.Right;

            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty("VerticalAlignment (Top, Bottom)")]
            public Vertical VerticalAlignment = Vertical.Top;

            public int MaxMessages = 10;
            public float SpacingBetweenMessages = 4;
            public float DefaultMessageDurationSeconds = 7.0f;
            public float SpacingBetweenElements = 6;

            public Dictionary<string, Style> Styles = new Dictionary<string, Style> {
                ["Info"] = new Style {
                    IconUri = "info.png"
                },

                ["Error"] = new Style {
                    IconUri = "error.png",
                    TextFontSize = 16,
                    TextFontWeight = Style.FontWeight.Bold
                },

                ["Warning"] = new Style {
                    IconUri = "warning.png",
                    TextFontWeight = Style.FontWeight.Bold
                },

                ["Note"] = new Style {
                    IconUri = "note.png"
                }
            };

            public Style GetStyle(string name) {
                var matchingStyles = Styles.Where(entry => entry.Key.Equals(name, StringComparison.InvariantCultureIgnoreCase)).ToList();
                if (matchingStyles.Count == 0) {
                    return new Style();
                }

                return matchingStyles.First().Value;
            }

            [JsonIgnore]
            public Vector2 StartPosition {
                get {
                    float horizontal = this.FeedOffset.x;
                    float vertical = this.FeedOffset.y;
                    if (HorizontalAlignment == Horizontal.Right) {
                        horizontal = this.WindowSize.x - this.MessageSize.x - this.FeedOffset.x;
                    }
                    if (VerticalAlignment == Vertical.Top) {
                        vertical = this.WindowSize.y - this.MessageSize.y - this.FeedOffset.y;
                    }

                    return new Vector2(horizontal, vertical);
                }
            }

            [JsonIgnore]
            public Vector2 RelativeMessageSize {
                get {
                    return new Vector2(this.MessageSize.x / this.WindowSize.x, this.MessageSize.y / this.WindowSize.y);
                }
            }

            public enum Horizontal {
                Left,
                Right
            }

            public enum Vertical {
                Top,
                Bottom
            }
        }

        class Style {
            public string BackgroundColorHex = "#fff2df06";

            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty("FontWeight (Bold, Regular)")]
            public FontWeight TextFontWeight = FontWeight.Regular;
            public string TextColorHex = "#ffffffff";
            public int TextFontSize = 14;

            [JsonProperty("IconUri (keep empty, if not needed, URL or filename relative to oxide/data directory)")]
            public string IconUri = null;

            [JsonIgnore]
            public Color BackgroundColor {
                get {
                    return ColorFromHex(this.BackgroundColorHex);
                }
            }

            [JsonIgnore]
            public Color TextColor {
                get {
                    return ColorFromHex(this.TextColorHex);
                }
            }

            [JsonIgnore]
            public string IconUriResolved {
                get {
                    string dataDirectory = $"file://{Interface.Oxide.DataDirectory}{Path.DirectorySeparatorChar}{Instance.Name}{Path.DirectorySeparatorChar}";
                    if (!this.IconUri.StartsWith("http") && !this.IconUri.StartsWith("www") && !this.IconUri.StartsWith("file://"))
                        return $"{dataDirectory}{this.IconUri}";
                    return this.IconUri;
                }
            }

            private string _png = null;
            [JsonIgnore]
            public string Png {
                get {
                    return _png;
                }
                set {
                    _png = value;
                    Instance.ForceUpdateQueues();
                }
            }

            [JsonIgnore]
            public string Font {
                get {
                    return this.TextFontWeight == FontWeight.Bold ? "RobotoCondensed-Bold.ttf" : "robotocondensed-regular.ttf";
                }
            }

            [JsonIgnore]
            public bool UseIcon {
                get {
                    return !string.IsNullOrWhiteSpace(this.IconUri);
                }
            }

            public enum FontWeight {
                Bold,
                Regular
            }
        }

        protected override void LoadDefaultConfig() {
            Config.WriteObject(new PluginConfig(), true);
        }

        private PluginConfig _config;
        private static PluginConfig config {
            get {
                return Instance._config;
            }
        }

        #endregion

        #region UTIL
        private static Color ColorFromHex(string v) {
            Color color = new Color();
            ColorUtility.TryParseHtmlString(v, out color);
            return color;
        }

        public static string ColorToCuiString(Color c) {
            return $"{c.r} {c.g} {c.b} {c.a}";
        }
        #endregion

        #region API
        private void Broadcast(string text, string styleName, int duration = -1) {
            this.Broadcast(text, styleName, (float)duration);
        }

        private void Broadcast(string text, string styleName, float duration = -1) {
            if (duration <= 0) {
                duration = config.DefaultMessageDurationSeconds;
            }

            foreach (var entry in messageQueues) {
                entry.Value.Add(text, config.GetStyle(styleName), duration);
            }
        }

        private void SendToPlayer(BasePlayer player, string text, string styleName, int duration = -1) {
            this.SendToPlayer(player, text, styleName, (float)duration);
        }

        private void SendToPlayer(BasePlayer player, string text, string styleName, float duration = -1) {
            if (duration <= 0) {
                duration = config.DefaultMessageDurationSeconds;
            }

            if (messageQueues.ContainsKey(player)) {
                messageQueues[player].Add(text, config.GetStyle(styleName), duration);
            }
        }

        private void SetEnabled(BasePlayer player, bool enabled) {
            if (this.messageQueues.ContainsKey(player)) {
                this.messageQueues[player].enabled = enabled;
            }
        }
        #endregion

        #region HOOKS
        private void Init() {
            Instance = this;
            permission.RegisterPermission(permSee, this);
            _config = Config.ReadObject<PluginConfig>();
            Config.WriteObject(_config);
        }

        private void Loaded() {
            this.ValidateConfig();
        }

        private void OnServerInitialized() {
            this.LoadImages();
            BasePlayer.activePlayerList.Where(pl => permission.UserHasPermission(pl.UserIDString, permSee))
                .ToList()
                .ForEach(pl => {
                    messageQueues.Add(pl, new MessageQueue(pl));
                });
        }

        private void OnPlayerInit(BasePlayer player) {
            if (player is HTNPlayer) {
                return;
            }

            this.InitQueue(player);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason) {
            this.DestroyQueue(player);
        }

        private void Unload() {
            foreach (var queue in messageQueues) {
                queue.Value.Cleanup();
            }

            messageQueues.Clear();
        }

        #region PERMISSION HANDLING
        void OnGroupPermissionGranted(string name, string perm) => RefreshVisibility();
        void OnGroupPermissionRevoked(string name, string perm) => RefreshVisibility();
        void OnUserPermissionGranted(string id, string permName) => RefreshVisibility();
        void OnUserPermissionRevoked(string id, string permName) => RefreshVisibility();
        #endregion
        #endregion

        #region QUEUE CONTROL
        private void RefreshVisibility() {
            BasePlayer.activePlayerList.ForEach(pl => {
                if (permission.UserHasPermission(pl.UserIDString, permSee)) {
                    this.InitQueue(pl);
                } else {
                    this.DestroyQueue(pl);
                }
            });
        }

        private void DestroyQueue(BasePlayer player) {
            if (this.messageQueues.ContainsKey(player)) {
                this.messageQueues[player].Cleanup();
                this.messageQueues.Remove(player);
            }
        }

        private void InitQueue(BasePlayer player) {
            if (!permission.UserHasPermission(player.UserIDString, permSee)) {
                return;
            }

            if (!messageQueues.ContainsKey(player)) {
                this.messageQueues.Add(player, new MessageQueue(player));
            }
        }

        private void ForceUpdateQueues() {
            this.messageQueues.Values.ToList().ForEach(queue => queue.UpdateQueue(true));
        }
        #endregion


        private void ValidateConfig() {
            var duplicates = config.Styles.Keys.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            if (duplicates.Count > 0) {
                PrintWarning($"Found contains duplicating style names: {string.Join(", ", duplicates)}. All names should be unique");
            }
        }

        private void LoadImages() {
            config.Styles.Values.ToList()
                         .ForEach(style => {
                             if (style.UseIcon && style.Png == null) {
                                 if (ImageLibrary.HasImage(style.IconUri, 0)) {
                                     style.Png = ImageLibrary.GetImage(style.IconUri);
                                 } else {
                                     ImageLibrary.AddImage(style.IconUriResolved, style.IconUri, 0, () => {
                                         style.Png = ImageLibrary.GetImage(style.IconUri);
                                     });
                                 }
                             }
                         });
        }

        class MessageQueue {
            private bool _enabled = true;
            public bool enabled {
                get {
                    return _enabled;
                }
                set {
                    _enabled = value;
                    if (_enabled == false) {
                        Cleanup();
                    }
                }
            }

            private List<Message> messages = new List<Message>();
            private BasePlayer player;

            public MessageQueue(BasePlayer player) {
                this.player = player;
            }

            internal void Add(string text, Style style, float duration = -1) {
                if (!enabled) {
                    return;
                }

                var message = new Message(text, style);

                if (duration <= 0) {
                    duration = config.DefaultMessageDurationSeconds;
                }

                Instance.timer.Once(duration, () => {
                    messages
                        .Where(m => m.guid == message.guid)
                        .ToList()
                        .ForEach(m => {
                            m.Destroy(player);
                        });

                    messages.RemoveAll(m => m.guid == message.guid);
                    UpdateQueue();
                });

                messages.Insert(0, message);
                UpdateQueue();

                if (messages.Count >= config.MaxMessages) {
                    for (int i = config.MaxMessages; i < messages.Count; i++) {
                        messages[i].Destroy(player);
                    }
                    messages.RemoveRange(config.MaxMessages, messages.Count - config.MaxMessages);
                }
            }

            public void UpdateQueue(bool force = false) {
                var i = 0;
                messages.ForEach(el => el.setQueuePosition(i++));
                onFrame(force);
            }

            private void onFrame(bool force) {
                List<CuiElement> cuiElements = new List<CuiElement>();

                foreach (var message in messages) {
                    if (force || message.needRedraw) {
                        cuiElements.AddRange(message.OnFrame());
                    }
                }

                if (cuiElements.Count == 0) return;

                cuiElements.ForEach(m => CuiHelper.DestroyUi(this.player, m.Name));
                CuiHelper.AddUi(this.player, cuiElements);
            }

            internal void Cleanup() {
                messages.ForEach(m => m.Destroy(player));
                messages.Clear();
            }

            class Message {
                private string text;
                private Style style;
                public string guid;
                private Vector2 position = new Vector2(config.StartPosition.x, config.StartPosition.y);
                private int queuePosition = 0;
                public bool needRedraw { get; private set; }
                private float iconWidth {
                    get {
                        return config.MessageSize.y - config.SpacingBetweenElements * 2;
                    }
                }

                public Message(string text, Style style) {
                    this.text = text;
                    this.style = style;
                    this.guid = GetGuid();
                    this.needRedraw = true;
                }

                private List<CuiElement> renderMessage() {
                    CuiElementContainer container = new CuiElementContainer();
                    container.Add(this.renderBackground());
                    if (style.Png != null) {
                        container.Add(this.renderIcon());
                    }
                    container.Add(this.renderText());
                    return container;
                }

                private CuiElement renderBackground() {
                    CuiElement background = new CuiElement {
                        Name = this.guid,
                        Parent = "Hud",
                        FadeOut = config.TransitionDuration
                    };

                    CuiImageComponent backgroundComponent = new CuiImageComponent();

                    backgroundComponent.Color = ColorToCuiString(style.BackgroundColor);
                    backgroundComponent.FadeIn = config.TransitionDuration;

                    background.Components.Add(backgroundComponent);

                    var transform = new CuiRectTransformComponent();
                    Vector2 anchorMin = new Vector2(position.x / config.WindowSize.x, position.y / config.WindowSize.y);
                    Vector2 anchorMax = anchorMin + config.RelativeMessageSize;

                    transform.AnchorMin = $"{anchorMin.x} {anchorMin.y}";
                    transform.AnchorMax = $"{anchorMax.x} {anchorMax.y}";

                    background.Components.Add(transform);
                    return background;
                }

                private CuiElement renderText() {
                    var textElement = new CuiElement {
                        Name = $"{this.guid}.text",
                        Parent = this.guid,
                        FadeOut = config.TransitionDuration
                    };

                    CuiTextComponent textComponent = new CuiTextComponent();
                    textComponent.Align = TextAnchor.MiddleLeft;
                    textComponent.Color = ColorToCuiString(style.TextColor);
                    textComponent.Font = style.Font;
                    textComponent.FontSize = style.TextFontSize;
                    textComponent.Text = text;
                    textComponent.FadeIn = config.TransitionDuration;
                    textElement.Components.Add(textComponent);

                    var transform = new CuiRectTransformComponent();
                    Vector2 anchorMin = new Vector2(config.SpacingBetweenElements / config.MessageSize.x, 0);

                    if (style.Png != null) {
                        anchorMin.x += (config.SpacingBetweenElements + iconWidth) / config.MessageSize.x;
                    }

                    Vector2 anchorMax = new Vector2(1 - config.SpacingBetweenElements / config.MessageSize.x, 1);

                    transform.AnchorMin = $"{anchorMin.x} {anchorMin.y}";
                    transform.AnchorMax = $"{anchorMax.x} {anchorMax.y}";

                    textElement.Components.Add(transform);

                    return textElement;
                }

                private CuiElement renderIcon() {
                    CuiElement icon = new CuiElement {
                        Name = $"{this.guid}.icon",
                        Parent = this.guid,
                        FadeOut = config.TransitionDuration
                    };

                    CuiRawImageComponent iconComponent = new CuiRawImageComponent();
                    iconComponent.Png = style.Png;
                    iconComponent.FadeIn = config.TransitionDuration;

                    icon.Components.Add(iconComponent);

                    var transform = new CuiRectTransformComponent();
                    Vector2 anchorMin = new Vector2(config.SpacingBetweenElements / config.MessageSize.x, config.SpacingBetweenElements / config.MessageSize.y);
                    Vector2 anchorMax = new Vector2((config.SpacingBetweenElements + iconWidth) / config.MessageSize.x, (config.SpacingBetweenElements + iconWidth) / config.MessageSize.y);

                    transform.AnchorMin = $"{anchorMin.x} {anchorMin.y}";
                    transform.AnchorMax = $"{anchorMax.x} {anchorMax.y}";

                    icon.Components.Add(transform);
                    return icon;
                }

                public List<CuiElement> OnFrame() {
                    this.needRedraw = false;
                    return renderMessage();
                }

                public void setQueuePosition(int pos) {
                    this.needRedraw = true;
                    this.queuePosition = pos;
                    this.position = getTargetPosition();
                }

                private Vector2 getTargetPosition() {
                    Vector2 result = config.StartPosition;
                    int directionFactor = config.VerticalAlignment == PluginConfig.Vertical.Top ? -1 : 1;
                    result.y += directionFactor * (queuePosition * (config.SpacingBetweenMessages + config.MessageSize.y));
                    return result;
                }

                public void Destroy(BasePlayer player) {
                    CuiHelper.DestroyUi(player, guid);
                    CuiHelper.DestroyUi(player, $"{guid}.text");
                    CuiHelper.DestroyUi(player, $"{guid}.icon");
                }


                private string GetGuid() {
                    return $"{CuiHelper.GetGuid()}{(7328 % {DarkPluginsID})}";
                }
            }
        }

        #region DEBUG
        public static void Debug(string str) {
#if DEBUG
                Instance.Puts(str);
#endif
        }
        #endregion
    }
}
