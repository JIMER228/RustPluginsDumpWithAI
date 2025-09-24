// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("PhoneCoreMulti3x4", "MikeHawke", "1.0.3")]
    [Description("GTA Style In Game Phone")]
    class PhoneCoreMulti3x4 : RustPlugin
    {

        void Unload()
        { foreach (BasePlayer current in BasePlayer.activePlayerList) { CuiHelper.DestroyUi(current, "P1PhoneUI"); CuiHelper.DestroyUi(current, "P2PhoneUI"); CuiHelper.DestroyUi(current, "P3PhoneUI"); CuiHelper.DestroyUi(current, "P4PhoneUI"); } }
        
        void OnServerInitialized()
        { permission.RegisterPermission("phonecoremulti3x4.show", this);  LoadConfigVariables(); SaveConfig(configData); }

        private ConfigData configData;
        class ConfigData
        {
            public Global GlobalSettings = new Global();
            public P1Settings Page1Settings = new P1Settings();
            public P2Settings Page2Settings = new P2Settings();
            public P3Settings Page3Settings = new P3Settings();
            public P4Settings Page4Settings = new P4Settings();
            public Pages PageSettings = new Pages();
        }
        class Global
        {
            [JsonProperty(PropertyName = "Use Permission phonecoremulti3x4.show")]
            public bool useperm = false;
            [JsonProperty(PropertyName = "Background Image Url")]
            public string BackURL = "https://i.imgur.com/GY90fRP.png";
        }
        class Pages
        {
            [JsonProperty(PropertyName = "Page 2 Active")]
            public bool Page2 = false;
            [JsonProperty(PropertyName = "Page 3 Active")]
            public bool Page3 = false;
            [JsonProperty(PropertyName = "Page 4 Active")]
            public bool Page4 = false;
        }
        class P1Settings
        {   
            [JsonProperty(PropertyName = "Image Url in place One")]
            public string P1OneURL = "https://i.imgur.com/mnBkYwB.png";
            [JsonProperty(PropertyName = "Command For Place One")]
            public string P1OneCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Two")]
            public string P1TwoURL = "https://i.imgur.com/bQGcmWL.png";
            [JsonProperty(PropertyName = "Command For Place Two")]
            public string P1TwoCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Three")]
            public string P1ThreeURL = "https://i.imgur.com/WAY6j4A.png";
            [JsonProperty(PropertyName = "Command For Place Three")]
            public string P1ThreeCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Four")]
            public string P1FourURL = "https://i.imgur.com/rOLnNiY.png";
            [JsonProperty(PropertyName = "Command For Place Four")]
            public string P1FourCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Five")]
            public string P1FiveURL = "https://i.imgur.com/cMjS34N.png";
            [JsonProperty(PropertyName = "Command For Place Five")]
            public string P1FiveCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Six")]
            public string P1SixURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Six")]
            public string P1SixCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Severn")]
            public string P1SevernURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Severn")]
            public string P1SevernCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Eight")]
            public string P1EightURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Eight")]
            public string P1EightCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Nine")]
            public string P1NineURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Nine")]
            public string P1NineCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Ten")]
            public string P1TenURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Ten")]
            public string P1TenCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Eleven")]
            public string P1ElevenURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Eleven")]
            public string P1ElevenCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Twelve")]
            public string P1TwelveURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Twelve")]
            public string P1TwelveCommand = "";
        }
        class P2Settings
        {
            [JsonProperty(PropertyName = "Image Url in place One")]
            public string P2OneURL = "https://i.imgur.com/mnBkYwB.png";
            [JsonProperty(PropertyName = "Command For Place One")]
            public string P2OneCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Two")]
            public string P2TwoURL = "https://i.imgur.com/bQGcmWL.png";
            [JsonProperty(PropertyName = "Command For Place Two")]
            public string P2TwoCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Three")]
            public string P2ThreeURL = "https://i.imgur.com/WAY6j4A.png";
            [JsonProperty(PropertyName = "Command For Place Three")]
            public string P2ThreeCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Four")]
            public string P2FourURL = "https://i.imgur.com/rOLnNiY.png";
            [JsonProperty(PropertyName = "Command For Place Four")]
            public string P2FourCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Five")]
            public string P2FiveURL = "https://i.imgur.com/cMjS34N.png";
            [JsonProperty(PropertyName = "Command For Place Five")]
            public string P2FiveCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Six")]
            public string P2SixURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Six")]
            public string P2SixCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Severn")]
            public string P2SevernURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Severn")]
            public string P2SevernCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Eight")]
            public string P2EightURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Eight")]
            public string P2EightCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Nine")]
            public string P2NineURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Nine")]
            public string P2NineCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Ten")]
            public string P2TenURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Ten")]
            public string P2TenCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Eleven")]
            public string P2ElevenURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Eleven")]
            public string P2ElevenCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Twelve")]
            public string P2TwelveURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Twelve")]
            public string P2TwelveCommand = "";
        }
        class P3Settings
        {
            [JsonProperty(PropertyName = "Image Url in place One")]
            public string P3OneURL = "https://i.imgur.com/mnBkYwB.png";
            [JsonProperty(PropertyName = "Command For Place One")]
            public string P3OneCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Two")]
            public string P3TwoURL = "https://i.imgur.com/bQGcmWL.png";
            [JsonProperty(PropertyName = "Command For Place Two")]
            public string P3TwoCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Three")]
            public string P3ThreeURL = "https://i.imgur.com/WAY6j4A.png";
            [JsonProperty(PropertyName = "Command For Place Three")]
            public string P3ThreeCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Four")]
            public string P3FourURL = "https://i.imgur.com/rOLnNiY.png";
            [JsonProperty(PropertyName = "Command For Place Four")]
            public string P3FourCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Five")]
            public string P3FiveURL = "https://i.imgur.com/cMjS34N.png";
            [JsonProperty(PropertyName = "Command For Place Five")]
            public string P3FiveCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Six")]
            public string P3SixURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Six")]
            public string P3SixCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Severn")]
            public string P3SevernURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Severn")]
            public string P3SevernCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Eight")]
            public string P3EightURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Eight")]
            public string P3EightCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Nine")]
            public string P3NineURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Nine")]
            public string P3NineCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Ten")]
            public string P3TenURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Ten")]
            public string P3TenCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Eleven")]
            public string P3ElevenURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Eleven")]
            public string P3ElevenCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Twelve")]
            public string P3TwelveURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Twelve")]
            public string P3TwelveCommand = "";
        }
        class P4Settings
        {
            [JsonProperty(PropertyName = "Image Url in place One")]
            public string P4OneURL = "https://i.imgur.com/mnBkYwB.png";
            [JsonProperty(PropertyName = "Command For Place One")]
            public string P4OneCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Two")]
            public string P4TwoURL = "https://i.imgur.com/bQGcmWL.png";
            [JsonProperty(PropertyName = "Command For Place Two")]
            public string P4TwoCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Three")]
            public string P4ThreeURL = "https://i.imgur.com/WAY6j4A.png";
            [JsonProperty(PropertyName = "Command For Place Three")]
            public string P4ThreeCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Four")]
            public string P4FourURL = "https://i.imgur.com/rOLnNiY.png";
            [JsonProperty(PropertyName = "Command For Place Four")]
            public string P4FourCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Five")]
            public string P4FiveURL = "https://i.imgur.com/cMjS34N.png";
            [JsonProperty(PropertyName = "Command For Place Five")]
            public string P4FiveCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Six")]
            public string P4SixURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Six")]
            public string P4SixCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Severn")]
            public string P4SevernURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Severn")]
            public string P4SevernCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Eight")]
            public string P4EightURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Eight")]
            public string P4EightCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Nine")]
            public string P4NineURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Nine")]
            public string P4NineCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Ten")]
            public string P4TenURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Ten")]
            public string P4TenCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Eleven")]
            public string P4ElevenURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Eleven")]
            public string P4ElevenCommand = "";
            [JsonProperty(PropertyName = "Image Url in place Twelve")]
            public string P4TwelveURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Twelve")]
            public string P4TwelveCommand = "";
        }

        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();
            if (configData.Page1Settings.P1OneURL == "") configData.Page1Settings.P1OneURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page1Settings.P1TwoURL == "") configData.Page1Settings.P1TwoURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page1Settings.P1ThreeURL == "") configData.Page1Settings.P1ThreeURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page1Settings.P1FourURL == "") configData.Page1Settings.P1FourURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page1Settings.P1FiveURL == "") configData.Page1Settings.P1FiveURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page1Settings.P1SixURL == "") configData.Page1Settings.P1SixURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page1Settings.P1SevernURL == "") configData.Page1Settings.P1SevernURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page1Settings.P1EightURL == "") configData.Page1Settings.P1EightURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page1Settings.P1NineURL == "") configData.Page1Settings.P1NineURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page1Settings.P1TenURL == "") configData.Page1Settings.P1TenURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page1Settings.P1ElevenURL == "") configData.Page1Settings.P1ElevenURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page1Settings.P1TwelveURL == "") configData.Page1Settings.P1TwelveURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page2Settings.P2OneURL == "") configData.Page2Settings.P2OneURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page2Settings.P2TwoURL == "") configData.Page2Settings.P2TwoURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page2Settings.P2ThreeURL == "") configData.Page2Settings.P2ThreeURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page2Settings.P2FourURL == "") configData.Page2Settings.P2FourURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page2Settings.P2FiveURL == "") configData.Page2Settings.P2FiveURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page2Settings.P2SixURL == "") configData.Page2Settings.P2SixURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page2Settings.P2SevernURL == "") configData.Page2Settings.P2SevernURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page2Settings.P2EightURL == "") configData.Page2Settings.P2EightURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page2Settings.P2NineURL == "") configData.Page2Settings.P2NineURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page2Settings.P2TenURL == "") configData.Page2Settings.P2TenURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page2Settings.P2ElevenURL == "") configData.Page2Settings.P2ElevenURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page2Settings.P2TwelveURL == "") configData.Page2Settings.P2TwelveURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page3Settings.P3OneURL == "") configData.Page3Settings.P3OneURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page3Settings.P3TwoURL == "") configData.Page3Settings.P3TwoURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page3Settings.P3ThreeURL == "") configData.Page3Settings.P3ThreeURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page3Settings.P3FourURL == "") configData.Page3Settings.P3FourURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page3Settings.P3FiveURL == "") configData.Page3Settings.P3FiveURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page3Settings.P3SixURL == "") configData.Page3Settings.P3SixURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page3Settings.P3SevernURL == "") configData.Page3Settings.P3SevernURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page3Settings.P3EightURL == "") configData.Page3Settings.P3EightURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page3Settings.P3NineURL == "") configData.Page3Settings.P3NineURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page3Settings.P3TenURL == "") configData.Page3Settings.P3TenURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page3Settings.P3ElevenURL == "") configData.Page3Settings.P3ElevenURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page3Settings.P3TwelveURL == "") configData.Page3Settings.P3TwelveURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page4Settings.P4OneURL == "") configData.Page4Settings.P4OneURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page4Settings.P4TwoURL == "") configData.Page4Settings.P4TwoURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page4Settings.P4ThreeURL == "") configData.Page4Settings.P4ThreeURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page4Settings.P4FourURL == "") configData.Page4Settings.P4FourURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page4Settings.P4FiveURL == "") configData.Page4Settings.P4FiveURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page4Settings.P4SixURL == "") configData.Page4Settings.P4SixURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page4Settings.P4SevernURL == "") configData.Page4Settings.P4SevernURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page4Settings.P4EightURL == "") configData.Page4Settings.P4EightURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page4Settings.P4NineURL == "") configData.Page4Settings.P4NineURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page4Settings.P4TenURL == "") configData.Page4Settings.P4TenURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page4Settings.P4ElevenURL == "") configData.Page4Settings.P4ElevenURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.Page4Settings.P4TwelveURL == "") configData.Page4Settings.P4TwelveURL = "https://i.imgur.com/6RjWdNd.png";
            SaveConfig(configData);
        }

        protected override void LoadDefaultConfig()
        { Puts("Creating new config file."); var config = new ConfigData(); SaveConfig(config); }

        void SaveConfig(ConfigData config)
        { Config.WriteObject(config, true); }

         [ChatCommand("phone")]
        void PhoneUIcall(BasePlayer player, string cmd, string[] args)
        {
            if (configData.GlobalSettings.useperm == true && !permission.UserHasPermission(player.userID.ToString(), "phonecoremulti3x4.show"))
            { SendReply(player, "You do not have the permission to use this command"); return; }
            else
            {
                if (args.Length == 0)
                { P1PhoneUI(player); return; }
                switch (args[0].ToLower())
                {
                    case "1":
                        P1PhoneUI(player);
                        break;
                    case "2":
                        P2PhoneUI(player);
                        break;
                    case "3":
                        P3PhoneUI(player);
                        break;
                    case "4":
                        P4PhoneUI(player);
                        break;
                    default:
                        P1PhoneUI(player);
                        return;
                }
                return;
            }
        }

        void P1PhoneUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "P1PhoneUI");
            var elements = new CuiElementContainer();
            var P1PhoneUIPanel = elements.Add(new CuiPanel { Image = { Color = $"1 0.1 0.1 0" }, RectTransform = { AnchorMin = $"0.707 0.01", AnchorMax = $"0.832 0.426" }, CursorEnabled = true, FadeOut = 0.1f }, "Overlay", "P1PhoneUI");
            elements.Add(new CuiElement { Parent = "P1PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.GlobalSettings.BackURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" } } });
            elements.Add(new CuiElement { Parent = "P1PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page1Settings.P1OneURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.817", AnchorMax = "0.344 0.917" } } });
            elements.Add(new CuiElement { Parent = "P1PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page1Settings.P1TwoURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.817", AnchorMax = "0.594 0.917" } } });
            elements.Add(new CuiElement { Parent = "P1PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page1Settings.P1ThreeURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.817", AnchorMax = "0.844 0.917" } } });
            elements.Add(new CuiElement { Parent = "P1PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page1Settings.P1FourURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.683", AnchorMax = "0.344 0.783" } } });
            elements.Add(new CuiElement { Parent = "P1PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page1Settings.P1FiveURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.683", AnchorMax = "0.594 0.783" } } });
            elements.Add(new CuiElement { Parent = "P1PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page1Settings.P1SixURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.683", AnchorMax = "0.844 0.783" } } });
            elements.Add(new CuiElement { Parent = "P1PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page1Settings.P1SevernURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.55", AnchorMax = "0.344 0.65" } } });
            elements.Add(new CuiElement { Parent = "P1PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page1Settings.P1EightURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.55", AnchorMax = "0.594 0.65" } } });
            elements.Add(new CuiElement { Parent = "P1PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page1Settings.P1NineURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.55", AnchorMax = "0.844 0.65" } } });
            elements.Add(new CuiElement { Parent = "P1PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page1Settings.P1TenURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.417", AnchorMax = "0.344 0.517" } } });
            elements.Add(new CuiElement { Parent = "P1PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page1Settings.P1ElevenURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.417", AnchorMax = "0.594 0.517" } } });
            elements.Add(new CuiElement { Parent = "P1PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page1Settings.P1TwelveURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.417", AnchorMax = "0.844 0.517" } } });
            if (configData.Page1Settings.P1OneURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p1onecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.817", AnchorMax = "0.344 0.917" }, Text = { Text = "" } }, P1PhoneUIPanel);}
            if (configData.Page1Settings.P1TwoURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p1twocom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.817", AnchorMax = "0.594 0.917" }, Text = { Text = "" } }, P1PhoneUIPanel);}
            if (configData.Page1Settings.P1ThreeURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p1threecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.817", AnchorMax = "0.844 0.917" }, Text = { Text = "" } }, P1PhoneUIPanel);}
            if (configData.Page1Settings.P1FourURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p1fourcom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.683", AnchorMax = "0.344 0.783" }, Text = { Text = "" } }, P1PhoneUIPanel);}
            if (configData.Page1Settings.P1FiveURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p1fivecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.683", AnchorMax = "0.594 0.783" }, Text = { Text = "" } }, P1PhoneUIPanel);}
            if (configData.Page1Settings.P1SixURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p1sixcom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.683", AnchorMax = "0.844 0.783" }, Text = { Text = "" } }, P1PhoneUIPanel);}
            if (configData.Page1Settings.P1SevernURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p1severncom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.55", AnchorMax = "0.344 0.65" }, Text = { Text = "" } }, P1PhoneUIPanel);}
            if (configData.Page1Settings.P1EightURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p1eightcom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.55", AnchorMax = "0.594 0.65" }, Text = { Text = "" } }, P1PhoneUIPanel);}
            if (configData.Page1Settings.P1NineURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p1ninecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.55", AnchorMax = "0.844 0.65" }, Text = { Text = "" } }, P1PhoneUIPanel);}
            if (configData.Page1Settings.P1TenURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p1tencom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.417", AnchorMax = "0.344 0.517" }, Text = { Text = "" } }, P1PhoneUIPanel);}
            if (configData.Page1Settings.P1ElevenURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p1elevencom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.417", AnchorMax = "0.594 0.517" }, Text = { Text = "" } }, P1PhoneUIPanel);}
            if (configData.Page1Settings.P1TwelveURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p1twelvecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.417", AnchorMax = "0.844 0.517" }, Text = { Text = "" } }, P1PhoneUIPanel);}
            elements.Add(new CuiElement { Parent = "P1PhoneUI", Components = { new CuiRawImageComponent { Url = "https://i.imgur.com/0o3q7BC.png", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.425 0.07", AnchorMax = "0.581 0.153" } } });
            elements.Add(new CuiButton { Button = { Command = "phone.close", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.425 0.07", AnchorMax = "0.581 0.153" }, Text = { Text = "" } }, P1PhoneUIPanel);
            if(configData.PageSettings.Page2 == true)
            {
            elements.Add(new CuiElement { Parent = "P1PhoneUI", Components = { new CuiRawImageComponent { Url = "https://i.imgur.com/UNVv91b.png", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.738 0.07", AnchorMax = "0.894 0.153" } } });
            elements.Add(new CuiButton { Button = { Command = "phone.pageup1", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.738 0.07", AnchorMax = "0.894 0.153" }, Text = { Text = "" } }, P1PhoneUIPanel);
            }
            CuiHelper.AddUi(player, elements);
        }
        void P2PhoneUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "P2PhoneUI");
            var elements = new CuiElementContainer();
            var P2PhoneUIPanel = elements.Add(new CuiPanel { Image = { Color = $"1 0.1 0.1 0" }, RectTransform = { AnchorMin = $"0.707 0.01", AnchorMax = $"0.832 0.426" }, CursorEnabled = true, FadeOut = 0.1f }, "Overlay", "P2PhoneUI");
            elements.Add(new CuiElement { Parent = "P2PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.GlobalSettings.BackURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" } } });
            elements.Add(new CuiElement { Parent = "P2PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page2Settings.P2OneURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.817", AnchorMax = "0.344 0.917" } } });
            elements.Add(new CuiElement { Parent = "P2PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page2Settings.P2TwoURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.817", AnchorMax = "0.594 0.917" } } });
            elements.Add(new CuiElement { Parent = "P2PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page2Settings.P2ThreeURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.817", AnchorMax = "0.844 0.917" } } });
            elements.Add(new CuiElement { Parent = "P2PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page2Settings.P2FourURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.683", AnchorMax = "0.344 0.783" } } });
            elements.Add(new CuiElement { Parent = "P2PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page2Settings.P2FiveURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.683", AnchorMax = "0.594 0.783" } } });
            elements.Add(new CuiElement { Parent = "P2PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page2Settings.P2SixURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.683", AnchorMax = "0.844 0.783" } } });
            elements.Add(new CuiElement { Parent = "P2PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page2Settings.P2SevernURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.55", AnchorMax = "0.344 0.65" } } });
            elements.Add(new CuiElement { Parent = "P2PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page2Settings.P2EightURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.55", AnchorMax = "0.594 0.65" } } });
            elements.Add(new CuiElement { Parent = "P2PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page2Settings.P2NineURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.55", AnchorMax = "0.844 0.65" } } });
            elements.Add(new CuiElement { Parent = "P2PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page2Settings.P2TenURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.417", AnchorMax = "0.344 0.517" } } });
            elements.Add(new CuiElement { Parent = "P2PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page2Settings.P2ElevenURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.417", AnchorMax = "0.594 0.517" } } });
            elements.Add(new CuiElement { Parent = "P2PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page2Settings.P2TwelveURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.417", AnchorMax = "0.844 0.517" } } });
            if (configData.Page2Settings.P2OneURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p2onecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.817", AnchorMax = "0.344 0.917" }, Text = { Text = "" } }, P2PhoneUIPanel);}
            if (configData.Page2Settings.P2TwoURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p2twocom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.817", AnchorMax = "0.594 0.917" }, Text = { Text = "" } }, P2PhoneUIPanel);}
            if (configData.Page2Settings.P2ThreeURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p2threecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.817", AnchorMax = "0.844 0.917" }, Text = { Text = "" } }, P2PhoneUIPanel);}
            if (configData.Page2Settings.P2FourURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p2fourcom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.683", AnchorMax = "0.344 0.783" }, Text = { Text = "" } }, P2PhoneUIPanel);}
            if (configData.Page2Settings.P2FiveURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p2fivecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.683", AnchorMax = "0.594 0.783" }, Text = { Text = "" } }, P2PhoneUIPanel);}
            if (configData.Page2Settings.P2SixURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p2sixcom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.683", AnchorMax = "0.844 0.783" }, Text = { Text = "" } }, P2PhoneUIPanel);}
            if (configData.Page2Settings.P2SevernURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p2severncom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.55", AnchorMax = "0.344 0.65" }, Text = { Text = "" } }, P2PhoneUIPanel);}
            if (configData.Page2Settings.P2EightURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p2eightcom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.55", AnchorMax = "0.594 0.65" }, Text = { Text = "" } }, P2PhoneUIPanel);}
            if (configData.Page2Settings.P2NineURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p2ninecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.55", AnchorMax = "0.844 0.65" }, Text = { Text = "" } }, P2PhoneUIPanel);}
            if (configData.Page2Settings.P2TenURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p2tencom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.417", AnchorMax = "0.344 0.517" }, Text = { Text = "" } }, P2PhoneUIPanel);}
            if (configData.Page2Settings.P2ElevenURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p2elevencom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.417", AnchorMax = "0.594 0.517" }, Text = { Text = "" } }, P2PhoneUIPanel);}
            if (configData.Page2Settings.P2TwelveURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p2twelvecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.417", AnchorMax = "0.844 0.517" }, Text = { Text = "" } }, P2PhoneUIPanel);}
            elements.Add(new CuiElement { Parent = "P2PhoneUI", Components = { new CuiRawImageComponent { Url = "https://i.imgur.com/0o3q7BC.png", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.425 0.07", AnchorMax = "0.581 0.153" } } });
            elements.Add(new CuiButton { Button = { Command = "phone.close", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.425 0.07", AnchorMax = "0.581 0.153" }, Text = { Text = "" } }, P2PhoneUIPanel);
            elements.Add(new CuiElement { Parent = "P2PhoneUI", Components = { new CuiRawImageComponent { Url = "https://i.imgur.com/qbkEJD5.png", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.106 0.07", AnchorMax = "0.263 0.153" } } });
            elements.Add(new CuiButton { Button = { Command = "phone.pagedown2", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.106 0.07", AnchorMax = "0.263 0.153" }, Text = { Text = "" } }, P2PhoneUIPanel);
            if(configData.PageSettings.Page3 == true)
            {
            elements.Add(new CuiElement { Parent = "P2PhoneUI", Components = { new CuiRawImageComponent { Url = "https://i.imgur.com/UNVv91b.png", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.738 0.07", AnchorMax = "0.894 0.153" } } });
            elements.Add(new CuiButton { Button = { Command = "phone.pageup2", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.738 0.07", AnchorMax = "0.894 0.153" }, Text = { Text = "" } }, P2PhoneUIPanel);
            }
            CuiHelper.AddUi(player, elements);
        }
        void P3PhoneUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "P3PhoneUI");
            var elements = new CuiElementContainer();
            var P3PhoneUIPanel = elements.Add(new CuiPanel { Image = { Color = $"1 0.1 0.1 0" }, RectTransform = { AnchorMin = $"0.707 0.01", AnchorMax = $"0.832 0.426" }, CursorEnabled = true, FadeOut = 0.1f }, "Overlay", "P3PhoneUI");
            elements.Add(new CuiElement { Parent = "P3PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.GlobalSettings.BackURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" } } });
            elements.Add(new CuiElement { Parent = "P3PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page3Settings.P3OneURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.817", AnchorMax = "0.344 0.917" } } });
            elements.Add(new CuiElement { Parent = "P3PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page3Settings.P3TwoURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.817", AnchorMax = "0.594 0.917" } } });
            elements.Add(new CuiElement { Parent = "P3PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page3Settings.P3ThreeURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.817", AnchorMax = "0.844 0.917" } } });
            elements.Add(new CuiElement { Parent = "P3PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page3Settings.P3FourURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.683", AnchorMax = "0.344 0.783" } } });
            elements.Add(new CuiElement { Parent = "P3PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page3Settings.P3FiveURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.683", AnchorMax = "0.594 0.783" } } });
            elements.Add(new CuiElement { Parent = "P3PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page3Settings.P3SixURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.683", AnchorMax = "0.844 0.783" } } });
            elements.Add(new CuiElement { Parent = "P3PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page3Settings.P3SevernURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.55", AnchorMax = "0.344 0.65" } } });
            elements.Add(new CuiElement { Parent = "P3PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page3Settings.P3EightURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.55", AnchorMax = "0.594 0.65" } } });
            elements.Add(new CuiElement { Parent = "P3PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page3Settings.P3NineURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.55", AnchorMax = "0.844 0.65" } } });
            elements.Add(new CuiElement { Parent = "P3PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page3Settings.P3TenURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.417", AnchorMax = "0.344 0.517" } } });
            elements.Add(new CuiElement { Parent = "P3PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page3Settings.P3ElevenURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.417", AnchorMax = "0.594 0.517" } } });
            elements.Add(new CuiElement { Parent = "P3PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page3Settings.P3TwelveURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.417", AnchorMax = "0.844 0.517" } } });
            if (configData.Page3Settings.P3OneURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p3onecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.817", AnchorMax = "0.344 0.917" }, Text = { Text = "" } }, P3PhoneUIPanel);}
            if (configData.Page3Settings.P3TwoURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p3twocom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.817", AnchorMax = "0.594 0.917" }, Text = { Text = "" } }, P3PhoneUIPanel);}
            if (configData.Page3Settings.P3ThreeURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p3threecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.817", AnchorMax = "0.844 0.917" }, Text = { Text = "" } }, P3PhoneUIPanel);}
            if (configData.Page3Settings.P3FourURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p3fourcom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.683", AnchorMax = "0.344 0.783" }, Text = { Text = "" } }, P3PhoneUIPanel);}
            if (configData.Page3Settings.P3FiveURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p3fivecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.683", AnchorMax = "0.594 0.783" }, Text = { Text = "" } }, P3PhoneUIPanel);}
            if (configData.Page3Settings.P3SixURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p3sixcom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.683", AnchorMax = "0.844 0.783" }, Text = { Text = "" } }, P3PhoneUIPanel);}
            if (configData.Page3Settings.P3SevernURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p3severncom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.55", AnchorMax = "0.344 0.65" }, Text = { Text = "" } }, P3PhoneUIPanel);}
            if (configData.Page3Settings.P3EightURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p3eightcom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.55", AnchorMax = "0.594 0.65" }, Text = { Text = "" } }, P3PhoneUIPanel);}
            if (configData.Page3Settings.P3NineURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p3ninecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.55", AnchorMax = "0.844 0.65" }, Text = { Text = "" } }, P3PhoneUIPanel);}
            if (configData.Page3Settings.P3TenURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p3tencom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.417", AnchorMax = "0.344 0.517" }, Text = { Text = "" } }, P3PhoneUIPanel);}
            if (configData.Page3Settings.P3ElevenURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p3elevencom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.417", AnchorMax = "0.594 0.517" }, Text = { Text = "" } }, P3PhoneUIPanel);}
            if (configData.Page3Settings.P3TwelveURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p3twelvecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.417", AnchorMax = "0.844 0.517" }, Text = { Text = "" } }, P3PhoneUIPanel);}
            elements.Add(new CuiElement { Parent = "P3PhoneUI", Components = { new CuiRawImageComponent { Url = "https://i.imgur.com/0o3q7BC.png", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.425 0.07", AnchorMax = "0.581 0.153" } } });
            elements.Add(new CuiButton { Button = { Command = "phone.close", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.425 0.07", AnchorMax = "0.581 0.153" }, Text = { Text = "" } }, P3PhoneUIPanel);
            elements.Add(new CuiElement { Parent = "P3PhoneUI", Components = { new CuiRawImageComponent { Url = "https://i.imgur.com/qbkEJD5.png", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.106 0.07", AnchorMax = "0.263 0.153" } } });
            elements.Add(new CuiButton { Button = { Command = "phone.pagedown3", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.106 0.07", AnchorMax = "0.263 0.153" }, Text = { Text = "" } }, P3PhoneUIPanel);
            if(configData.PageSettings.Page4 == true)
            {
            elements.Add(new CuiElement { Parent = "P3PhoneUI", Components = { new CuiRawImageComponent { Url = "https://i.imgur.com/UNVv91b.png", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.738 0.07", AnchorMax = "0.894 0.153" } } });
            elements.Add(new CuiButton { Button = { Command = "phone.pageup3", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.738 0.07", AnchorMax = "0.894 0.153" }, Text = { Text = "" } }, P3PhoneUIPanel);
            }
            CuiHelper.AddUi(player, elements);
        }
        void P4PhoneUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "P4PhoneUI");
            var elements = new CuiElementContainer();
            var P4PhoneUIPanel = elements.Add(new CuiPanel { Image = { Color = $"1 0.1 0.1 0" }, RectTransform = { AnchorMin = $"0.707 0.01", AnchorMax = $"0.832 0.426" }, CursorEnabled = true, FadeOut = 0.1f }, "Overlay", "P4PhoneUI");
            elements.Add(new CuiElement { Parent = "P4PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.GlobalSettings.BackURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" } } });
            elements.Add(new CuiElement { Parent = "P4PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page4Settings.P4OneURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.817", AnchorMax = "0.344 0.917" } } });
            elements.Add(new CuiElement { Parent = "P4PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page4Settings.P4TwoURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.817", AnchorMax = "0.594 0.917" } } });
            elements.Add(new CuiElement { Parent = "P4PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page4Settings.P4ThreeURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.817", AnchorMax = "0.844 0.917" } } });
            elements.Add(new CuiElement { Parent = "P4PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page4Settings.P4FourURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.683", AnchorMax = "0.344 0.783" } } });
            elements.Add(new CuiElement { Parent = "P4PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page4Settings.P4FiveURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.683", AnchorMax = "0.594 0.783" } } });
            elements.Add(new CuiElement { Parent = "P4PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page4Settings.P4SixURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.683", AnchorMax = "0.844 0.783" } } });
            elements.Add(new CuiElement { Parent = "P4PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page4Settings.P4SevernURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.55", AnchorMax = "0.344 0.65" } } });
            elements.Add(new CuiElement { Parent = "P4PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page4Settings.P4EightURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.55", AnchorMax = "0.594 0.65" } } });
            elements.Add(new CuiElement { Parent = "P4PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page4Settings.P4NineURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.55", AnchorMax = "0.844 0.65" } } });
            elements.Add(new CuiElement { Parent = "P4PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page4Settings.P4TenURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.417", AnchorMax = "0.344 0.517" } } });
            elements.Add(new CuiElement { Parent = "P4PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page4Settings.P4ElevenURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.417", AnchorMax = "0.594 0.517" } } });
            elements.Add(new CuiElement { Parent = "P4PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.Page4Settings.P4TwelveURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.417", AnchorMax = "0.844 0.517" } } });
            if (configData.Page4Settings.P4OneURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p4onecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.817", AnchorMax = "0.344 0.917" }, Text = { Text = "" } }, P4PhoneUIPanel);}
            if (configData.Page4Settings.P4TwoURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p4twocom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.817", AnchorMax = "0.594 0.917" }, Text = { Text = "" } }, P4PhoneUIPanel);}
            if (configData.Page4Settings.P4ThreeURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p4threecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.817", AnchorMax = "0.844 0.917" }, Text = { Text = "" } }, P4PhoneUIPanel);}
            if (configData.Page4Settings.P4FourURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p4fourcom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.683", AnchorMax = "0.344 0.783" }, Text = { Text = "" } }, P4PhoneUIPanel);}
            if (configData.Page4Settings.P4FiveURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p4fivecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.683", AnchorMax = "0.594 0.783" }, Text = { Text = "" } }, P4PhoneUIPanel);}
            if (configData.Page4Settings.P4SixURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p4sixcom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.683", AnchorMax = "0.844 0.783" }, Text = { Text = "" } }, P4PhoneUIPanel);}
            if (configData.Page4Settings.P4SevernURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p4severncom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.55", AnchorMax = "0.344 0.65" }, Text = { Text = "" } }, P4PhoneUIPanel);}
            if (configData.Page4Settings.P4EightURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p4eightcom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.55", AnchorMax = "0.594 0.65" }, Text = { Text = "" } }, P4PhoneUIPanel);}
            if (configData.Page4Settings.P4NineURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p4ninecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.55", AnchorMax = "0.844 0.65" }, Text = { Text = "" } }, P4PhoneUIPanel);}
            if (configData.Page4Settings.P4TenURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p4tencom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.417", AnchorMax = "0.344 0.517" }, Text = { Text = "" } }, P4PhoneUIPanel);}
            if (configData.Page4Settings.P4ElevenURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p4elevencom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.417", AnchorMax = "0.594 0.517" }, Text = { Text = "" } }, P4PhoneUIPanel);}
            if (configData.Page4Settings.P4TwelveURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.p4twelvecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.417", AnchorMax = "0.844 0.517" }, Text = { Text = "" } }, P4PhoneUIPanel);}
            elements.Add(new CuiElement { Parent = "P4PhoneUI", Components = { new CuiRawImageComponent { Url = "https://i.imgur.com/0o3q7BC.png", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.425 0.07", AnchorMax = "0.581 0.153" } } });
            elements.Add(new CuiButton { Button = { Command = "phone.close", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.425 0.07", AnchorMax = "0.581 0.153" }, Text = { Text = "" } }, P4PhoneUIPanel);
            elements.Add(new CuiElement { Parent = "P4PhoneUI", Components = { new CuiRawImageComponent { Url = "https://i.imgur.com/qbkEJD5.png", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.106 0.07", AnchorMax = "0.263 0.153" } } });
            elements.Add(new CuiButton { Button = { Command = "phone.pagedown4", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.106 0.07", AnchorMax = "0.263 0.153" }, Text = { Text = "" } }, P4PhoneUIPanel);
            CuiHelper.AddUi(player, elements);
        }
        [ConsoleCommand("phone.pageup1")]
        private void Pup1(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P1PhoneUI"); P2PhoneUI(arg.Player()); }
        [ConsoleCommand("phone.pageup2")]
        private void Pup2(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P2PhoneUI"); P3PhoneUI(arg.Player()); }
        [ConsoleCommand("phone.pageup3")]
        private void Pup3(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P3PhoneUI"); P4PhoneUI(arg.Player()); }
        [ConsoleCommand("phone.pagedown4")]
        private void Pdow4(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P4PhoneUI"); P3PhoneUI(arg.Player()); }
        [ConsoleCommand("phone.pagedown3")]
        private void Pdow3(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P3PhoneUI"); P2PhoneUI(arg.Player()); }
        [ConsoleCommand("phone.pagedown2")]
        private void Pdow2(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P2PhoneUI"); P1PhoneUI(arg.Player()); }
        [ConsoleCommand("phone.p1onecom")]
        private void P1OneCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P1PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page1Settings.P1OneCommand); }
        [ConsoleCommand("phone.p1twocom")]
        private void P1TwoCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P1PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page1Settings.P1TwoCommand); }
        [ConsoleCommand("phone.p1threecom")]
        private void P1ThreeCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P1PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page1Settings.P1ThreeCommand); }
        [ConsoleCommand("phone.p1fourcom")]
        private void P1FourCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P1PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page1Settings.P1FourCommand); }
        [ConsoleCommand("phone.p1fivecom")]
        private void P1FiveCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P1PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page1Settings.P1FiveCommand); }
        [ConsoleCommand("phone.p1sixcom")]
        private void P1SixCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P1PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page1Settings.P1SixCommand); }
        [ConsoleCommand("phone.p1severncom")]
        private void P1SevernCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P1PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page1Settings.P1SevernCommand); }
        [ConsoleCommand("phone.p1eightcom")]
        private void P1EightCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P1PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page1Settings.P1EightCommand); }
        [ConsoleCommand("phone.p1ninecom")]
        private void P1NineCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P1PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page1Settings.P1NineCommand); }
        [ConsoleCommand("phone.p1tencom")]
        private void P1TenCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P1PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page1Settings.P1TenCommand); }
        [ConsoleCommand("phone.p1elevencom")]
        private void P1ElevenCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P1PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page1Settings.P1ElevenCommand); }
        [ConsoleCommand("phone.p1twelvecom")]
        private void P1TwelveCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P1PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page1Settings.P1TwelveCommand); }
         [ConsoleCommand("phone.p2onecom")]
        private void P2OneCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P2PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page2Settings.P2OneCommand); }
        [ConsoleCommand("phone.p2twocom")]
        private void P2TwoCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P2PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page2Settings.P2TwoCommand); }
        [ConsoleCommand("phone.p2threecom")]
        private void P2ThreeCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P2PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page2Settings.P2ThreeCommand); }
        [ConsoleCommand("phone.p2fourcom")]
        private void P2FourCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P2PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page2Settings.P2FourCommand); }
        [ConsoleCommand("phone.p2fivecom")]
        private void P2FiveCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P2PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page2Settings.P2FiveCommand); }
        [ConsoleCommand("phone.p2sixcom")]
        private void P2SixCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P2PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page2Settings.P2SixCommand); }
        [ConsoleCommand("phone.p2severncom")]
        private void P2SevernCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P2PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page2Settings.P2SevernCommand); }
        [ConsoleCommand("phone.p2eightcom")]
        private void P2EightCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P2PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page2Settings.P2EightCommand); }
        [ConsoleCommand("phone.p2ninecom")]
        private void P2NineCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P2PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page2Settings.P2NineCommand); }
        [ConsoleCommand("phone.p2tencom")]
        private void P2TenCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P2PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page2Settings.P2TenCommand); }
        [ConsoleCommand("phone.p2elevencom")]
        private void P2ElevenCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P2PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page2Settings.P2ElevenCommand); }
        [ConsoleCommand("phone.p2twelvecom")]
        private void P2TwelveCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P2PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page2Settings.P2TwelveCommand); }
         [ConsoleCommand("phone.p3onecom")]
        private void P3OneCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P3PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page3Settings.P3OneCommand); }
        [ConsoleCommand("phone.p3twocom")]
        private void P3TwoCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P3PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page3Settings.P3TwoCommand); }
        [ConsoleCommand("phone.p3threecom")]
        private void P3ThreeCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P3PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page3Settings.P3ThreeCommand); }
        [ConsoleCommand("phone.p3fourcom")]
        private void P3FourCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P3PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page3Settings.P3FourCommand); }
        [ConsoleCommand("phone.p3fivecom")]
        private void P3FiveCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P3PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page3Settings.P3FiveCommand); }
        [ConsoleCommand("phone.p3sixcom")]
        private void P3SixCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P3PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page3Settings.P3SixCommand); }
        [ConsoleCommand("phone.p3severncom")]
        private void P3SevernCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P3PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page3Settings.P3SevernCommand); }
        [ConsoleCommand("phone.p3eightcom")]
        private void P3EightCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P3PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page3Settings.P3EightCommand); }
        [ConsoleCommand("phone.p3ninecom")]
        private void P3NineCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P3PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page3Settings.P3NineCommand); }
        [ConsoleCommand("phone.p3tencom")]
        private void P3TenCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P3PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page3Settings.P3TenCommand); }
        [ConsoleCommand("phone.p3elevencom")]
        private void P3ElevenCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P3PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page3Settings.P3ElevenCommand); }
        [ConsoleCommand("phone.p3twelvecom")]
        private void P3TwelveCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P3PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page3Settings.P3TwelveCommand); }
         [ConsoleCommand("phone.p4onecom")]
        private void P4OneCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P4PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page4Settings.P4OneCommand); }
        [ConsoleCommand("phone.p4twocom")]
        private void P4TwoCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P4PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page4Settings.P4TwoCommand); }
        [ConsoleCommand("phone.p4threecom")]
        private void P4ThreeCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P4PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page4Settings.P4ThreeCommand); }
        [ConsoleCommand("phone.p4fourcom")]
        private void P4FourCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P4PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page4Settings.P4FourCommand); }
        [ConsoleCommand("phone.p4fivecom")]
        private void P4FiveCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P4PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page4Settings.P4FiveCommand); }
        [ConsoleCommand("phone.p4sixcom")]
        private void P4SixCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P4PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page4Settings.P4SixCommand); }
        [ConsoleCommand("phone.p4severncom")]
        private void P4SevernCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P4PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page4Settings.P4SevernCommand); }
        [ConsoleCommand("phone.p4eightcom")]
        private void P4EightCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P4PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page4Settings.P4EightCommand); }
        [ConsoleCommand("phone.p4ninecom")]
        private void P4NineCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P4PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page4Settings.P4NineCommand); }
        [ConsoleCommand("phone.p4tencom")]
        private void P4TenCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P4PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page4Settings.P4TenCommand); }
        [ConsoleCommand("phone.p4elevencom")]
        private void P4ElevenCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P4PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page4Settings.P4ElevenCommand); }
        [ConsoleCommand("phone.p4twelvecom")]
        private void P4TwelveCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "P4PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.Page4Settings.P4TwelveCommand); }
        [ConsoleCommand("phone.close")]
        private void Closebutton(ConsoleSystem.Arg arg)
        { CuiHelper.DestroyUi(arg.Player(), "P1PhoneUI"); CuiHelper.DestroyUi(arg.Player(), "P2PhoneUI"); CuiHelper.DestroyUi(arg.Player(), "P3PhoneUI"); CuiHelper.DestroyUi(arg.Player(), "P4PhoneUI"); }
    }
}