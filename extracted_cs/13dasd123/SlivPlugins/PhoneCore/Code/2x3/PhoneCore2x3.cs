using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("PhoneCore2x3", "MikeHawke", "1.0.3")]
    [Description("GTA Style In Game Phone")]
    class PhoneCore2x3 : RustPlugin
    {
       
       void Unload()
        { foreach (BasePlayer current in BasePlayer.activePlayerList) { CuiHelper.DestroyUi(current, "PhoneUI");} }

        void OnServerInitialized()
        { permission.RegisterPermission("phonecore2x3.show", this); LoadConfigVariables(); SaveConfig(configData); }

        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Use Permission phonecore2x3.show")]
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
            SaveConfig(configData); 
        }

        protected override void LoadDefaultConfig()
        { Puts("Creating new config file."); var config = new ConfigData(); SaveConfig(config); }

        void SaveConfig(ConfigData config)
        { Config.WriteObject(config, true); }

        [ChatCommand("phone")]
        void PhoneUIcall(BasePlayer player)
        {
            if (configData.useperm == true && !permission.UserHasPermission(player.userID.ToString(), "phonecore2x3.show"))
            { SendReply(player, "You do not have the permission to use this command"); return; }
            else { PhoneUI(player); }
        }
        
        void PhoneUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "PhoneUI");
            var elements = new CuiElementContainer();
            var PhoneUIPanel = elements.Add(new CuiPanel { Image = { Color = $"1 0.1 0.1 0" }, RectTransform = { AnchorMin = $"0.707 0.01", AnchorMax = $"0.832 0.426" }, CursorEnabled = true, FadeOut = 0.1f }, "Overlay", "PhoneUI");
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.BackURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.OneURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.75", AnchorMax = "0.469 0.917" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.TwoURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.525 0.75", AnchorMax = "0.838 0.917" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.ThreeURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.543", AnchorMax = "0.469 0.71" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.FourURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.525 0.543", AnchorMax = "0.838 0.71" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.FiveURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.156 0.34", AnchorMax = "0.469 0.507" } } });
            elements.Add(new CuiElement { Parent = "PhoneUI", Components = { new CuiRawImageComponent { Url = $"{configData.SixURL}", FadeIn = 0.8f }, new CuiRectTransformComponent { AnchorMin = "0.525 0.34", AnchorMax = "0.838 0.507" } } });
            if (configData.OneURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.onecom", Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.75", AnchorMax = "0.469 0.917" }, Text = { Text = "" } }, PhoneUIPanel);}
            if (configData.TwoURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.twocom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.525 0.75", AnchorMax = "0.838 0.917" }, Text = { Text = "" } }, PhoneUIPanel);}
            if (configData.ThreeURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.threecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.543", AnchorMax = "0.469 0.71" }, Text = { Text = "" } }, PhoneUIPanel);}
            if (configData.FourURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.fourcom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.525 0.543", AnchorMax = "0.838 0.71" }, Text = { Text = "" } }, PhoneUIPanel);}
            if (configData.FiveURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.fivecom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.156 0.34", AnchorMax = "0.469 0.507" }, Text = { Text = "" } }, PhoneUIPanel);}
            if (configData.SixURL != "https://i.imgur.com/6RjWdNd.png")
            {elements.Add(new CuiButton { Button = { Command = "phone.sixcom", Color = "1 0 0 0" }, RectTransform = { AnchorMin = "0.525 0.34", AnchorMax = "0.838 0.507" }, Text = { Text = "" } }, PhoneUIPanel);}
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

        [ConsoleCommand("phone.close")]
        private void Closebutton(ConsoleSystem.Arg arg)
        { CuiHelper.DestroyUi(arg.Player(), "PhoneUI"); }
    }
}