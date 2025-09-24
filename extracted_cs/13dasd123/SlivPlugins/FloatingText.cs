// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Newtonsoft.Json;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Libraries.Covalence;



namespace Oxide.Plugins
{
    [Info("Floating Text", "IO Rust Group", "1.0.1")]
    [Description("Create in-world floating text for admins.")]
    class FloatingText : CovalencePlugin
    {
        #region Initialization

        public static FloatingText ft;
        PluginConfig config;
        private GameObject textChecker;
        private List<DDrawData> floatingTextObjects = new List<DDrawData>();

        private void OnServerInitialized()
        {
            AddCovalenceCommand("addfloatingtext", "AddFloatingText");
            AddCovalenceCommand("findfloatingtext", "FindFloatingText");
            AddCovalenceCommand("removefloatingtext", "RemoveFloatingText");
            AddCovalenceCommand("refreshfloatingtext", "RefreshFloatingText");

            CreateTextHost();
            ft = this;
        }

        #endregion

        #region Commands

        private void AddFloatingText(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) { PrivateMessage(player, GetLang("notAdmin")); return; }

            var bPlayer = player.Object as BasePlayer;
            DateTime datetimeends = DateTime.Now;
            var text = "Default Floating Text";
            var size = 25f;
            int minutes = 0;
            var hexColor = "#FFFFFF";
            var drawDistance = 10;
            string message = "addTextSuccess";
            bool isTimed = false;

            Vector3 pos = new Vector3(
                    bPlayer.ServerPosition.x,
                    bPlayer.ServerPosition.y,
                    bPlayer.ServerPosition.z
                ) + config.generalSettings.PositionOffset;

            try
            {
                int argCount = 0;
                float argSize = 0;
                if (!args[1].Contains("#") && int.TryParse(args[0], out minutes)) argCount++;
                size = float.Parse(args[argCount]); argCount++;
                hexColor = args[argCount]; argCount++;
                drawDistance = Int32.Parse(args[argCount]); argCount++;
                text = string.Join(" ", args.Skip(argCount).ToArray());
            }
            catch
            {
                PrivateMessage(player, GetLang("addTextBadFormatting")); return;
            }

            if (minutes > 0)
            {
                datetimeends = datetimeends.AddMinutes(minutes);
                isTimed = true;
                message = "addTimedTextSuccess";
            }

            CreateFloatingTextObject(text, size, hexColor, drawDistance, pos, datetimeends, isTimed);
            PrivateMessage(player, GetLang(message));
        }

        private void RemoveFloatingText(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) { PrivateMessage(player, GetLang("notAdmin")); return; }

            if (args.Length != 1)
            {
                PrivateMessage(player, GetLang("removeTextBadID")); return;
            }

            int textIndex;
            try
            {
                textIndex = Int32.Parse(args[0]);
            }
            catch
            {
                PrivateMessage(player, GetLang("removeTextBadID")); return;
            }

            if (textIndex > floatingTextObjects.Count - 1 || textIndex < 0) { PrivateMessage(player, GetLang("removeTextBadID")); return; }

            if (DestroyFloatingTextObject(textIndex))
            {
                PrivateMessage(player, GetLang("removeTextSuccess")); return;
            }
            else
            {
                PrivateMessage(player, GetLang("removeTextBadID")); return;
            }
        }

        private void FindFloatingText(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) { PrivateMessage(player, GetLang("notAdmin")); return; }

            var bPlayer = player.Object as BasePlayer;

            if (bPlayer.GetComponent<FloatingTextFinder>() == null)
            {
                bPlayer.gameObject.AddComponent<FloatingTextFinder>().floatingTextObjects = this.floatingTextObjects;
                PrivateMessage(player, GetLang("findTextToggleOn"));
            }
            else if (bPlayer.GetComponent<FloatingTextFinder>() != null)
            {
                UnityEngine.Object.Destroy(bPlayer.GetComponent<FloatingTextFinder>());
                PrivateMessage(player, GetLang("findTextToggleOff"));
            }
        }

        private void RefreshFloatingText(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) { PrivateMessage(player, GetLang("notAdmin")); return; }

            LoadConfig();
            LoadData();
            UpdateComponentLists();
            PrivateMessage(player, GetLang("confirmRefresh"));
        }

        private void UpdateComponentLists()
        {
            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                if (basePlayer.GetComponent<FloatingTextFinder>() != null)
                {
                    basePlayer.GetComponent<FloatingTextFinder>().floatingTextObjects = this.floatingTextObjects;
                }
            }

            if (textChecker != null && textChecker.GetComponent<FloatingTextVisualizer>())
            {
                textChecker.GetComponent<FloatingTextVisualizer>().floatingTextObjects = this.floatingTextObjects;
            }
        }

        #endregion

        #region Create/Destroy Text

        /// <summary>
        /// Create a floating text object for players. Text ID's are automatically assigned.
        /// </summary>
        /// <param name="text">The displayed text string.</param>
        /// <param name="size">How big the text appears. ~10 is small, ~25 medium, 50+ is large.</param>
        /// <param name="hexColor">Web hexidecimal color. Parsed by ColorUtility.TryParseHtmlString() to match those requirements. Alpha channel does not work correctly.</param>
        /// <param name="drawDistance">How close a player needs to be to see the text. Players may run past values smaller than 5 before they load in. 10-20 is a good starting point.</param>
        /// <param name="position">World position text is drawn.</param>
        public void CreateFloatingTextObject(string text, float size, string hexColor, float drawDistance, Vector3 position, DateTime dateTime, bool IsTextTimed = false)
        {
            floatingTextObjects.Add(new DDrawData()
            {
                Text = text,
                Size = size,
                HexColor = hexColor,
                DrawDistance = drawDistance,
                Position = position,
                TimeEnds = dateTime,
                IsTimed = IsTextTimed,
            });
            UpdateComponentLists();
            SaveData();
        }

        /// <summary>
        /// Destroys a floating text object given its index in the floatingTextObject list.
        /// </summary>
        /// <param name="textId">Index in floatingTextObject list.</param>
        /// <returns>Index exists, floating text object deleted.</returns>
        public bool DestroyFloatingTextObject(int textId)
        {
            if (floatingTextObjects.Count < textId)
                return false;

            floatingTextObjects.RemoveAt(textId);
            UpdateComponentLists();
            SaveData();
            return true;
        }

        #endregion

        #region Configuration

        protected override void LoadDefaultConfig() => config = LoadBaseConfig();

        protected override void SaveConfig() => Config.WriteObject(config, true);

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<PluginConfig>();

                if (config == null)
                    throw new JsonException();

                if (config.Version < Version || config.Version > Version)
                {
                    LogWarning(GetLang("configUpdated"));
                    LoadDefaultConfig();
                }
                SaveConfig();
            }
            catch
            {
                LoadDefaultConfig();
                LogWarning(GetLang("confCorrupt"));
                SaveConfig();

            }
        }

        private PluginConfig LoadBaseConfig()
        {
            return new PluginConfig
            {
                generalSettings = new PluginConfig.GeneralSettings
                {
                    PositionOffset = new Vector3(0.0f, 1.0f, 0.0f)
                },
                chatSettings = new PluginConfig.ChatSettings
                {
                    Announce = true,
                    EnableChatTag = true,
                    ChatTagColor = "4A95CC",
                    ChatMessageColor = "C57039",
                },

                Version = Version
            };
        }

        class PluginConfig
        {
            [JsonProperty(PropertyName = "General Settings")]
            public GeneralSettings generalSettings { get; set; }

            [JsonProperty(PropertyName = "Chat Settings")]
            public ChatSettings chatSettings { get; set; }

            public class GeneralSettings
            {
                [JsonProperty("Default AddFloatingText position (offset from floor, [X,Y,Z])")]
                public Vector3 PositionOffset { get; set; }

                [JsonProperty("Refresh Duration")]
                public float RefreshDuration { get; set; } = 1f;

                [JsonProperty("Anti Flicker Duration")]
                public float AntiFlickerDuration { get; set; } = 1f;

            }

            public class ChatSettings
            {
                [JsonProperty("Announces Plugin Load/Unload")]
                public bool Announce { get; set; }

                [JsonProperty("Chat Tag Enabled")]
                public bool EnableChatTag { get; set; }

                [JsonProperty(PropertyName = "Chat Tag Color")]
                public string ChatTagColor { get; set; }

                [JsonProperty(PropertyName = "Chat Message Color")]
                public string ChatMessageColor { get; set; }

            }

            [JsonProperty(PropertyName = "Version: ")]
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        #endregion

        #region Data Files

        private void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile($"{Name}/FloatingTextObjects"))
            {
                floatingTextObjects = Interface.Oxide.DataFileSystem.ReadObject<List<DDrawData>>($"{Name}/FloatingTextObjects");
            }
            else
            {
                LogWarning(GetLang("creatingNewData"));
                floatingTextObjects = new List<DDrawData>();
                SaveData();
            }
        }

        private void SaveData()
        {
            if (floatingTextObjects != null)
            {
                Interface.Oxide.DataFileSystem.WriteObject($"{Name}/FloatingTextObjects", floatingTextObjects);
            }
            else
            {
                // Note to developers (or cheeky admins): This should literally never occur 
                // because floatingTextObjects is declared as an empty Dictionary (~ln.20) 
                // If this happens then who knows what is going wrong :) Maybe check if the
                // plugin has been modified and accidentally sets FloatingTectObjects to null?
                LogWarning(GetLang("calledWhileObjNull"));
            }

        }

        private void OnServerSave() => SaveData();

        #endregion

        #region Lang

        private string GetLang(string message) => lang.GetMessage(message, this);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["notAdmin"] = "You must be admin to use this command.",
                ["confCorrupt"] = "Config is corrupt.",
                ["configUpdated"] = "Different version detected, updating config.",
                ["creatingNewData"] = "Datafile not found! Creating new one.",
                ["nullAnnounce"] = "Announcement message null.",
                ["pmNull"] = "Private message null.",
                ["pluginLoaded"] = "Floating Text By <I/O> Rust Group Loaded.",
                ["pluginUnloaded"] = "Floating Text By <I/O> Rust Group Unloaded.",
                ["confirmRefresh"] = "Floating Text datafile has been reloaded.",
                ["calledWhileObjNull"] = "SaveData was called while FloatingTestObjects is null. This shouldn't happen! " + "Try a restart, then a fresh install. If problems persist, contact a developer.",
                ["addTextBadFormatting"] = "Formatting incorrect. /addfloatingtext [size] [#RRGGBB] [draw distance] [text] or /addfloatingtext [duration (minutes)] [size] [#RRGGBB] [draw distance] [text]. Note a draw distance smaller than 10 is not reccomended (players may run past it before it loads!).",
                ["addTextSuccess"] = "Text created!",
                ["addTimedTextSuccess"] = "Timed Text created!",
                ["findTextToggleOn"] = "All floating text has been highlighted. /findfloatingtext to toggle it off.",
                ["findTextToggleOff"] = "No longer highlighting floating text. /findfloatingtext to toggle it on.",
                ["removeTextBadID"] = "Formatting incorrect or invalid ID. /removefloatingtext [textid]. You can find the text ID using /findfloatingtext, or simply remove the text from the datafile.",
                ["removeTextSuccess"] = "Floating text object has been removed. Text objects may now have a new ID."
            }, this);
        }

        #endregion

        #region Oxide/Umod

        void Loaded()
        {
            LoadConfig();
            LoadData();

            if (config.chatSettings.Announce)
                SendAnnouncement(GetLang("pluginLoaded"));

            ft = this;
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.GetComponent<FloatingTextFinder>() != null)
                {
                    UnityEngine.Object.Destroy(player.GetComponent<FloatingTextFinder>());
                }
            }

            if (textChecker != null)
                if (textChecker.GetComponent<FloatingTextVisualizer>())
                    textChecker.GetComponent<FloatingTextVisualizer>().Deactivate();

            if (config.chatSettings.Announce)
                SendAnnouncement(GetLang("pluginUnloaded"));
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player.GetComponent<FloatingTextFinder>() != null)
            {
                UnityEngine.Object.Destroy(player.GetComponent<FloatingTextFinder>());
            }
        }

        #endregion

        #region Helpers

        private void SendAnnouncement(string message)
        {
            if (message == null)
            {
                LogWarning(GetLang("nullAnnounce"));
                return;
            }

            foreach (IPlayer player in players.Connected)
            {
                PrivateMessage(player, message);
            }
        }

        private void PrivateMessage(IPlayer player, string message)
        {
            if (message == null)
            {
                LogWarning(GetLang("pmNull"));
                return;
            }

            string tagColor = config.chatSettings.ChatTagColor;
            string msgColor = config.chatSettings.ChatMessageColor;
            string finalMessage = $"<color=#{msgColor}>{message}</color>";
            if (config.chatSettings.EnableChatTag)
            {
                finalMessage = $"<color=#{tagColor}>[Floating Text]</color> <color=#{msgColor}>{message}</color>";
            }

            player.Reply(finalMessage);
            return;
        }

        #endregion

        #region Behaviour

        public class FloatingTextVisualizer : MonoBehaviour
        {
            public List<DDrawData> floatingTextObjects;
            float refreshDuration = ft.config.generalSettings.RefreshDuration;
            float antiFlickerDuration = ft.config.generalSettings.AntiFlickerDuration;
           
            void Start()
            {
                if (floatingTextObjects == null)
                    throw new ArgumentNullException("Start()", "FloatingTextObjects is null. The developer has not assigned it when the component was added or the server has not started.");

                Activate();
            }

            void CheckVisibility()
            {
                foreach (var textObject in floatingTextObjects)
                {
                    if (textObject.IsTimed) { if (DateTime.Compare(DateTime.Now, textObject.TimeEnds) >= 1) { continue; } } //("{0} {1} {2}", earlier, sameas, layerthan); 
                    List<BasePlayer> players = new List<BasePlayer>();
                    Vis.Entities<BasePlayer>(textObject.Position, textObject.DrawDistance, players);
                    foreach (BasePlayer player in players)
                    {
                        if (player != null && player.IsConnected && !player.IsSleeping())
                        {
                            CreateFloatingText(player, textObject);
                        }
                    }
                }
            }

            private void CreateFloatingText(BasePlayer player, DDrawData textData)
            {
                if (player == null)
                    return;

                var textColor = Color.white;
                ColorUtility.TryParseHtmlString(textData.HexColor, out textColor);

                if (player.IsAdmin)
                {
                    player.SendConsoleCommand
                    (
                        "ddraw.text",
                        refreshDuration + antiFlickerDuration,
                        textColor,
                        textData.Position,
                        $"<size={textData.Size}>{textData.Text}</size>"
                    );
                }
                else
                {
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                    player.SendNetworkUpdateImmediate();
                    player.SendConsoleCommand
                        (
                            "ddraw.text",
                            refreshDuration + antiFlickerDuration,
                            textColor,
                            textData.Position,
                            $"<size={textData.Size}>{textData.Text}</size>"
                        );
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                    player.SendNetworkUpdateImmediate();
                }
            }

            public void Activate()
            {
                InvokeRepeating("CheckVisibility", 0, refreshDuration);
            }

            public void Deactivate()
            {
                UnityEngine.Object.Destroy(gameObject);
                Destroy(this);
            }
        }

        public class FloatingTextFinder : MonoBehaviour
        {
            private BasePlayer player;
            public List<DDrawData> floatingTextObjects;

            float refreshDuration = 2;

            void Start()
            {
                if (floatingTextObjects == null)
                    throw new ArgumentNullException("Start()", "FloatingTextObjects is null. The developer has not assigned it when the component was added.");

                player = gameObject.GetComponent<BasePlayer>();
                Activate();
            }

            void CreateFloatingText()
            {
                for (int i = 0; i < floatingTextObjects.Count; i++)
                {
                    DrawTextTracker(refreshDuration, floatingTextObjects[i].Position, i);
                }
            }

            private void DrawTextTracker(float duration, Vector3 textPosition, int textIndex)
            {
                if (player.IsAdmin)
                {
                    player.SendConsoleCommand
                    (
                        "ddraw.text",
                        duration,
                        Color.yellow,
                        textPosition += new Vector3(0f, 0.5f, 0f),
                        "<size=40>×</size>"
                    );
                    player.SendConsoleCommand
                    (
                        "ddraw.text",
                        duration,
                        Color.yellow,
                        textPosition += new Vector3(0f, 0.6f, 0f),
                        $"<size=20>{textIndex}</size>"
                    );
                }
                else
                {
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                    player.SendNetworkUpdateImmediate();
                    player.SendConsoleCommand
                    (
                        "ddraw.text",
                        duration,
                        Color.yellow,
                        textPosition += new Vector3(0f, 0.5f, 0f),
                        "<size=40>×</size>"
                    );
                    player.SendConsoleCommand
                    (
                        "ddraw.text",
                        duration,
                        Color.yellow,
                        textPosition += new Vector3(0f, 0.75f, 0f),
                        $"<size=20>{textIndex}</size>"
                    );
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                    player.SendNetworkUpdateImmediate();
                }
            }

            public void Activate()
            {
                if (player != null)
                {
                    InvokeRepeating("CreateFloatingText", 0, refreshDuration);
                }
            }
        }

        public class DDrawData
        {
            [JsonProperty(PropertyName = "Text")]
            public string Text { get; set; }

            [JsonProperty(PropertyName = "Size")]
            public float Size { get; set; }

            [JsonProperty(PropertyName = "Color (#RRGGBB)")]
            public string HexColor { get; set; }

            [JsonProperty(PropertyName = "Position (X,Y,Z)")]
            public Vector3 Position { get; set; }

            [JsonProperty("Draw Distance")]
            public float DrawDistance { get; set; }

            [JsonProperty("Time Ends")]
            public DateTime TimeEnds { get; set; }

            [JsonProperty("Is Timed")]
            public bool IsTimed { get; set; }
        }

        #endregion

        #region Other

        private void CreateTextHost()
        {
            var entity = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", new Vector3(0, TerrainMeta.HeightMap.GetHeight(new Vector3(0, 0, 0)) - 10, 0)).GetComponent<SphereEntity>();
            entity.currentRadius = 1;
            entity.lerpSpeed = 0f;
            textChecker = entity.gameObject;
            textChecker.AddComponent<FloatingTextVisualizer>().floatingTextObjects = this.floatingTextObjects;

            entity.Spawn();
        }

        #endregion
    }
}