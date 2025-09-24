using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("PhoneCore3x4", "MikeHawke", "1.0.3")]
    [Description("GTA Style In Game Phone")]
    class PhoneCore3x4 : RustPlugin
    {

        void Unload()
        { foreach (BasePlayer current in BasePlayer.activePlayerList) { CuiHelper.DestroyUi(current, "PhoneUI"); } }

        void OnServerInitialized()
        { permission.RegisterPermission("phonecore3x4.show", this); LoadConfigVariables(); SaveConfig(configData); }

        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Use Permission phonecore3x4.show")]
            public bool useperm = false;
            [JsonProperty(PropertyName = "Background Image Url")]
            public string BackURL = "https://i.imgur.com/GY90fRP.png";

            [JsonProperty(PropertyName = "Image Url in place One")]
            public string OneURL = "https://i.imgur.com/mnBkYwB.png";
            [JsonProperty(PropertyName = "Command For Place One")]
            public string OneCommand = "";

            [JsonProperty(PropertyName = "Image Url in place Two")]
            public string TwoURL = "https://i.imgur.com/bQGcmWL.png";
            [JsonProperty(PropertyName = "Command For Place Two")]
            public string TwoCommand = "";

            [JsonProperty(PropertyName = "Image Url in place Three")]
            public string ThreeURL = "https://i.imgur.com/WAY6j4A.png";
            [JsonProperty(PropertyName = "Command For Place Three")]
            public string ThreeCommand = "";

            [JsonProperty(PropertyName = "Image Url in place Four")]
            public string FourURL = "https://i.imgur.com/rOLnNiY.png";
            [JsonProperty(PropertyName = "Command For Place Four")]
            public string FourCommand = "";

            [JsonProperty(PropertyName = "Image Url in place Five")]
            public string FiveURL = "https://i.imgur.com/cMjS34N.png";
            [JsonProperty(PropertyName = "Command For Place Five")]
            public string FiveCommand = "";

            [JsonProperty(PropertyName = "Image Url in place Six")]
            public string SixURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Six")]
            public string SixCommand = "";

            [JsonProperty(PropertyName = "Image Url in place Severn")]
            public string SevernURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Severn")]
            public string SevernCommand = "";

            [JsonProperty(PropertyName = "Image Url in place Eight")]
            public string EightURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Eight")]
            public string EightCommand = "";

            [JsonProperty(PropertyName = "Image Url in place Nine")]
            public string NineURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Nine")]
            public string NineCommand = "";

            [JsonProperty(PropertyName = "Image Url in place Ten")]
            public string TenURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Ten")]
            public string TenCommand = "";

            [JsonProperty(PropertyName = "Image Url in place Eleven")]
            public string ElevenURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Eleven")]
            public string ElevenCommand = "";

            [JsonProperty(PropertyName = "Image Url in place Twelve")]
            public string TwelveURL = "https://i.imgur.com/Vz4vVaw.png";
            [JsonProperty(PropertyName = "Command For Place Twelve")]
            public string TwelveCommand = "";

        }

        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();
            if (configData.OneURL == "") configData.OneURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.TwoURL == "") configData.TwoURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.ThreeURL == "") configData.ThreeURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.FourURL == "") configData.FourURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.FiveURL == "") configData.FiveURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.SixURL == "") configData.SixURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.SevernURL == "") configData.SevernURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.EightURL == "") configData.EightURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.NineURL == "") configData.NineURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.TenURL == "") configData.TenURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.ElevenURL == "") configData.ElevenURL = "https://i.imgur.com/6RjWdNd.png";
            if (configData.TwelveURL == "") configData.TwelveURL = "https://i.imgur.com/6RjWdNd.png";
            SaveConfig(configData);
        }

        protected override void LoadDefaultConfig()
        { Puts("Creating new config file."); var config = new ConfigData(); SaveConfig(config); }

        void SaveConfig(ConfigData config)
        { Config.WriteObject(config, true); }

        [ChatCommand("phone")]
        void PhoneUIcall(BasePlayer player)
        {
            if (configData.useperm == true && !permission.UserHasPermission(player.userID.ToString(), "phonecore3x4.show"))
            { SendReply(player, "You do not have the permission to use this command"); return; }
            else
            { PhoneUI(player); }
        }

        void PhoneUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "PhoneUI");
            var elements = new CuiElementContainer();
            var PhoneUIPanel = elements.Add(new CuiPanel { Image = { Color = $"1 0.1 0.1 0" }, RectTransform = { AnchorMin = $"0.707 0.01", AnchorMax = $"0.832 0.426" }, CursorEnabled = true, FadeOut = 0.1f }, "Overlay", "PhoneUI");
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.BackURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.OneURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.817", AnchorMax = "0.344 0.917" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.TwoURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.817", AnchorMax = "0.594 0.917" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.ThreeURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.817", AnchorMax = "0.844 0.917" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.FourURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.683", AnchorMax = "0.344 0.783" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.FiveURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.683", AnchorMax = "0.594 0.783" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.SixURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.683", AnchorMax = "0.844 0.783" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.SevernURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.55", AnchorMax = "0.344 0.65" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.EightURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.55", AnchorMax = "0.594 0.65" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.NineURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.55", AnchorMax = "0.844 0.65" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.TenURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.417", AnchorMax = "0.344 0.517" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.ElevenURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.406 0.417", AnchorMax = "0.594 0.517" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.TwelveURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.656 0.417", AnchorMax = "0.844 0.517" } } });
            if (configData.OneURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.onecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.817", AnchorMax = "0.344 0.917" }, Text = { Text = "" } }, PhoneUIPanel);}
            if (configData.TwoURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.twocom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.817", AnchorMax = "0.594 0.917" }, Text = { Text = "" } }, PhoneUIPanel);}
            if (configData.ThreeURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.threecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.817", AnchorMax = "0.844 0.917" }, Text = { Text = "" } }, PhoneUIPanel);}
            if (configData.FourURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.fourcom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.683", AnchorMax = "0.344 0.783" }, Text = { Text = "" } }, PhoneUIPanel);}
            if (configData.FiveURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.fivecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.683", AnchorMax = "0.594 0.783" }, Text = { Text = "" } }, PhoneUIPanel);}
            if (configData.SixURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.sixcom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.683", AnchorMax = "0.844 0.783" }, Text = { Text = "" } }, PhoneUIPanel);}
            if (configData.SevernURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.severncom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.55", AnchorMax = "0.344 0.65" }, Text = { Text = "" } }, PhoneUIPanel);}
            if (configData.EightURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.eightcom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.55", AnchorMax = "0.594 0.65" }, Text = { Text = "" } }, PhoneUIPanel);}
            if (configData.NineURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.ninecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.55", AnchorMax = "0.844 0.65" }, Text = { Text = "" } }, PhoneUIPanel);}
            if (configData.TenURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.tencom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.417", AnchorMax = "0.344 0.517" }, Text = { Text = "" } }, PhoneUIPanel);}
            if (configData.ElevenURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.elevencom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.406 0.417", AnchorMax = "0.594 0.517" }, Text = { Text = "" } }, PhoneUIPanel);}
            if (configData.TwelveURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.twelvecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.656 0.417", AnchorMax = "0.844 0.517" }, Text = { Text = "" } }, PhoneUIPanel);}
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = "https://i.imgur.com/0o3q7BC.png", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.663 0.07", AnchorMax = "0.819 0.153" } } });
            elements.Add(new CuiButton { Button = { Command = "phone.close", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.663 0.07", AnchorMax = "0.819 0.153" }, Text = { Text = "" } }, PhoneUIPanel);
            CuiHelper.AddUi(player, elements);
        }

        [ConsoleCommand("phone.onecom")]
        private void OneCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.OneCommand); }

        [ConsoleCommand("phone.twocom")]
        private void TwoCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.TwoCommand); }

        [ConsoleCommand("phone.threecom")]
        private void ThreeCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.ThreeCommand); }

        [ConsoleCommand("phone.fourcom")]
        private void FourCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.FourCommand); }

        [ConsoleCommand("phone.fivecom")]
        private void FiveCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.FiveCommand); }

        [ConsoleCommand("phone.sixcom")]
        private void SixCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.SixCommand); }

        [ConsoleCommand("phone.severncom")]
        private void SevernCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.SevernCommand); }
        [ConsoleCommand("phone.eightcom")]
        private void EightCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.EightCommand); }
        [ConsoleCommand("phone.ninecom")]
        private void NineCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.NineCommand); }
        [ConsoleCommand("phone.tencom")]
        private void TenCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.TenCommand); }
        [ConsoleCommand("phone.elevencom")]
        private void ElevenCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.ElevenCommand); }
        [ConsoleCommand("phone.twelvecom")]
        private void TwelveCom(ConsoleSystem.Arg arg)
        { if (arg.Player() == null) return; CuiHelper.DestroyUi(arg.Player(), "PhoneUI"); arg.Player().SendConsoleCommand("chat.say", "/" + configData.TwelveCommand); }


        [ConsoleCommand("phone.close")]
        private void Closebutton(ConsoleSystem.Arg arg)
        { CuiHelper.DestroyUi(arg.Player(), "PhoneUI"); }
    }
}