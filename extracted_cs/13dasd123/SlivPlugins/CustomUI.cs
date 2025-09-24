// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using Oxide.Core;
using Newtonsoft.Json.Serialization;
using System.Collections;
using System.IO;
using System.Linq;
namespace Oxide.Plugins
{
    [Info("CustomUI", "craft", "1.0.4")]
    [Description("This plugin was fixed by Инкуб to order [Rust Plugin Sliv]: https://discord.gg/pFgKw6Dyyq")]
    class CustomUI : RustPlugin
    {
        private ConfigData configData;
        void Unload()
        {
            _ic.OnDestroy();
            foreach (BasePlayer current in BasePlayer.activePlayerList) { CuiHelper.DestroyUi(current, "CustomUI"); }
        }
        private GameObject _ib;
        private ImageCache _ic;

        private string UL = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar;

        private CuiElement TP(string P, string S, string Min = "0 0", string Max = "1 1")
        {
            return CJ(P, GT(S), Min, Max);
        }
        private string GT(string N)
        {
            string R;
            if (_ic.TS.TryGetValue(N, out R)) return R;
            return null;
        }
        private void Init()
        {
            LoadVariables();
        }
        void OnServerInitialized()
        {
            DM();

        }
        private CuiElement CJ(string P, string U, string Min = "0 0", string Max = "1 1")
        {
            var E = new CuiElement();
            var I = new CuiRawImageComponent { Png = U, Color = "1 1 1 1" };
            var R = new CuiRectTransformComponent { AnchorMin = Min, AnchorMax = Max };
            E.Components.Add(I);
            E.Components.Add(R);
            E.Name = CuiHelper.GetGuid();
            E.Parent = P;
            return E;
        }

        private void DM()
        {
            _ib = new GameObject();
            _ic = _ib.AddComponent<ImageCache>();
            _ic.TS.Clear();
            HashSet<string> list = new HashSet<string>();
            if (!string.IsNullOrEmpty(configData.BackURL))
            {
                if (!list.Contains(configData.BackURL))
                {
                    list.Add(configData.BackURL);
                    _ic.GI(configData.BackURL, UL + configData.BackURL);
                }
            }
            for (int i = 0; i < configData.AnNiset.Count; i++)
            {
                var Png = configData.AnNiset[i].AnURL;
                if (!string.IsNullOrEmpty(Png))
                {
                    if (!list.Contains(Png))
                    {
                        list.Add(Png);
                        _ic.GI(Png, UL + Png);
                    }
                }
            }
        }
        private class ImageCache : MonoBehaviour
        {
            public Dictionary<string, string> TS = new Dictionary<string, string>();
            class Queue
            {
                public string U { get; set; }
                public string N { get; set; }
            }
            public void OnDestroy()
            {
                foreach (var value in TS.Values)
                {
                    uint entityId = Convert.ToUInt32(value);
                    NetworkableId networkableId = new NetworkableId(entityId);
                    FileStorage.server.RemoveEntityNum(networkableId, uint.MaxValue);
                }
            }

            public void GI(string n, string u)
            {
                Queue Q = new Queue { U = u, N = n };
                StartCoroutine(WR(Q));
            }
            IEnumerator WR(Queue Q)
            {
                using (var www = new WWW(Q.U))
                {
                    yield return www;
                    if (string.IsNullOrEmpty(www.error))
                    {
                        TS.Add(Q.N, FileStorage.server.Store(www.bytes, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString());
                    }
                }
            }

        }


        
        class ConfigData
        {

            [JsonProperty(PropertyName = "Title text size")]
            public int bitizsize;

            [JsonProperty(PropertyName = "Title text content")]
            public string biaotineirong;

            [JsonProperty(PropertyName = "Minimum heading offset")]
            public string biaotiMin;

            [JsonProperty(PropertyName = "Maximum heading offset")]
            public string biaotiMix;


            [JsonProperty(PropertyName = "The minimum deviation of the main background position")]
            public string ZBackUMin;

            [JsonProperty(PropertyName = "Maximum deviation of main background position")]
            public string ZBackUMix;

            [JsonProperty(PropertyName = "Main background image")]
            public string BackURL;

            [JsonProperty(PropertyName = "Main background color")]
            public string BackYS;

            [JsonProperty(PropertyName = "Close button text size")]
            public int GuanBiWzsize;

            [JsonProperty(PropertyName = "Close button text")]
            public string GuanBiWz;

            [JsonProperty(PropertyName = "Close button color")]
            public string GuanBiYS;

            [JsonProperty(PropertyName = "Close button minimum offset")]
            public string GuanBiMin;

            [JsonProperty(PropertyName = "Maximum offset of the close button")]
            public string GuanBiMix;



            [JsonProperty(PropertyName = "= = = = = = = = = = = = = [ Content setting ] = = = = = = = = = = = = =")]
            public List<AnNiuData> AnNiset;

        }

        class AnNiuData
        {
            [JsonProperty(PropertyName = "Image file name")]
            public string AnURL;

            [JsonProperty(PropertyName = "Image minimum shift")]
            public string tpMin;

            [JsonProperty(PropertyName = "Maximum image offset")]
            public string tpMix;

            [JsonProperty(PropertyName = "Button text size")]
            public int AnWeiZi;

            [JsonProperty(PropertyName = "Button background color")]
            public string Anyanse;

            [JsonProperty(PropertyName = "Button text content")]
            public string AnText;

            [JsonProperty(PropertyName = "Minimum button offset")]
            public string AnMin;

            [JsonProperty(PropertyName = "Button most offset")]
            public string AnMix;

            [JsonProperty(PropertyName = "Button command")]
            public string AnCommand;

            [JsonProperty(PropertyName = "Description size")]
            public int SMWeiZi;

            [JsonProperty(PropertyName = "Description text content")]
            public string SMText;

            [JsonProperty(PropertyName = "Description minimum offset")]
            public string SMMin;

            [JsonProperty(PropertyName = "Explain the maximum offset")]
            public string SMMix;

        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                bitizsize = 25,

                biaotineirong = "<color=#FF9900>[ Custom ui ]</color>",

                biaotiMin = "0 .89",
                biaotiMix = "1 .98",

                ZBackUMin = "0.025 0.05",
                ZBackUMix = "0.975 0.95",
                BackURL = "",
                BackYS = "0 0 0 .5",
                GuanBiWzsize = 15,
                GuanBiWz = "close",
                GuanBiYS = "1 0 0 .7",
                GuanBiMin = "0.02 0.01",
                GuanBiMix = "0.98 0.05",

                AnNiset = new List<AnNiuData>
                {
                    new AnNiuData
                    {
                        Anyanse = "0 0 0 .5",
                        AnURL = "an01.png",
                        
                        AnWeiZi = 14,
                        AnText = "Button",


                        tpMin = ".04 .6",
                        tpMix = ".24 .85",

                        SMMin = ".04 .55",
                        SMMix = ".24 .6",

                        AnMin = ".08 .5",
                        AnMix = ".2 .55",

                        AnCommand = "chat.say /bz",
                        SMWeiZi = 16,
                        SMText = "<color=#FFFF99>[ text ]</color>",

                    },

                    new AnNiuData
                    {
                        Anyanse = "0 0 0 .5",
                        AnURL = "an02.png",

                        AnWeiZi = 14,
                        AnText = "Button",


                        tpMin = ".28 .6",
                        tpMix = ".48 .85",

                        SMMin = ".28 .55",
                        SMMix = ".48 .6",

                        AnMin = ".32 .5",
                        AnMix = ".44 .55",

                        AnCommand = "chat.say /bz",
                        SMWeiZi = 16,
                        SMText = "<color=#FFFF99>[ text ]</color>",

                    },

                    new AnNiuData
                    {
                        Anyanse = "0 0 0 .5",
                        AnURL = "an01.png",

                        AnWeiZi = 14,
                        AnText = "Button",


                        tpMin = ".52 .6",
                        tpMix = ".72 .85",

                        SMMin = ".52 .55",
                        SMMix = ".72 .6",

                        AnMin = ".56 .5",
                        AnMix = ".68 .55",

                        AnCommand = "chat.say /bz",
                        SMWeiZi = 16,
                        SMText = "<color=#FFFF99>[ text ]</color>",

                    },


                    new AnNiuData
                    {
                        Anyanse = "0 0 0 .5",
                        AnURL = "an01.png",

                        AnWeiZi = 14,
                        AnText = "Button",


                        tpMin = ".76 .6",
                        tpMix = ".96 .85",

                        SMMin = ".76 .55",
                        SMMix = ".96 .6",

                        AnMin = ".8 .5",
                        AnMix = ".92 .55",

                        AnCommand = "chat.say /bz",
                        SMWeiZi = 16,
                        SMText = "<color=#FFFF99>[ text ]</color>",

                    },






                    new AnNiuData
                    {
                        Anyanse = "0 0 0 .5",
                        AnURL = "an01.png",

                        AnWeiZi = 14,
                        AnText = "Button",


                        tpMin = ".04 .2",
                        tpMix = ".24 .45",

                        SMMin = ".04 .15",
                        SMMix = ".24 .2",

                        AnMin = ".08 .1",
                        AnMix = ".2 .15",

                        AnCommand = "chat.say /bz",
                        SMWeiZi = 16,
                        SMText = "<color=#FFFF99>[ text ]</color>",

                    },

                    new AnNiuData
                    {
                        Anyanse = "0 0 0 .5",
                        AnURL = "an01.png",

                        AnWeiZi = 14,
                        AnText = "Button",


                        tpMin = ".28 .2",
                        tpMix = ".48 .45",

                        SMMin = ".28 .15",
                        SMMix = ".48 .2",

                        AnMin = ".32 .1",
                        AnMix = ".44 .15",

                        AnCommand = "chat.say /bz",
                        SMWeiZi = 16,
                        SMText = "<color=#FFFF99>[ text ]</color>",

                    },

                    new AnNiuData
                    {
                        Anyanse = "0 0 0 .5",
                        AnURL = "an01.png",

                        AnWeiZi = 14,
                        AnText = "Button",


                        tpMin = ".52 .2",
                        tpMix = ".72 .45",

                        SMMin = ".52 .15",
                        SMMix = ".72 .2",

                        AnMin = ".56 .1",
                        AnMix = ".68 .15",

                        AnCommand = "chat.say /bz",
                        SMWeiZi = 16,
                        SMText = "<color=#FFFF99>[ text ]</color>",

                    },


                    new AnNiuData
                    {
                        Anyanse = "0 0 0 .5",
                        AnURL = "an01.png",

                        AnWeiZi = 14,
                        AnText = "Button",


                        tpMin = ".76 .2",
                        tpMix = ".96 .45",

                        SMMin = ".76 .15",
                        SMMix = ".96 .2",

                        AnMin = ".8 .1",
                        AnMix = ".92 .15",

                        AnCommand = "chat.say /bz",
                        SMWeiZi = 16,
                        SMText = "<color=#FFFF99>[ text ]</color>",

                    },
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        CuiPanel CreatePanel(string anchorMin, string anchorMax, bool SB = false, string color = "0 0 0 0")
        { return new CuiPanel 
		{ 
		Image = {
                    Color = "0 0 0 0.95",
                    Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                }, 
		RectTransform = { AnchorMin = "0.025 0.05", AnchorMax = "0.975 0.95" }, CursorEnabled = SB }; }


        CuiButton DMButton(string dz, string ml, string Zx, string zd, string wb = null, string ys = "0 0 0 0", int dx = 12)
        { return new CuiButton { Button = { Close = dz, Command = ml, Color = ys }, RectTransform = { AnchorMin = Zx, AnchorMax = zd }, Text = { Text = wb, FontSize = dx, Align = TextAnchor.MiddleCenter } }; }


        [ChatCommand("ctui")]
        void CustomUIAnNiuUIcall(BasePlayer player)
        {
            CustomUIAnNiuUI(player);
        }

        void CustomUIAnNiuUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "CustomUI");
            var elements = new CuiElementContainer();
            var bzGui = elements.Add(CreatePanel(configData.ZBackUMin, configData.ZBackUMix, true), "Overlay", "CustomUI");
            var playerCount = BasePlayer.activePlayerList.Count;
            var gametime = TOD_Sky.Instance.Cycle.DateTime.ToString("<B>HH:mm</B>");

            elements.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.87 0.9", AnchorMax = "0.99 0.99" },
                Text = { Color = "1 1 1 0.7", Text = "<B>Time： </B>" + gametime, FontSize = 18, Align = TextAnchor.MiddleLeft }
            }, bzGui);

            elements.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.77 0.9", AnchorMax = "0.85 0.99" },
                Text = { Color = "1 1 1 0.7", Text = "<B>Online： </B>" + playerCount, FontSize = 18, Align = TextAnchor.MiddleLeft }
            }, bzGui);

            if (!string.IsNullOrEmpty(configData.BackURL))
            {
                elements.Add(TP(bzGui, configData.BackURL));
            }
            elements.Add(DMButton(null, null, configData.biaotiMin, configData.biaotiMix, configData.biaotineirong, "0 0 0 0", configData.bitizsize), bzGui);


            int Max_sl = configData.AnNiset.Count;
            for (int i = 0; i < Max_sl; i++)
            {
                var data = configData.AnNiset[i];
                if (!string.IsNullOrEmpty(data.AnURL))
                {
                    var anniu = elements.Add(DMButton(null, null, data.tpMin, data.tpMix), bzGui);
                    elements.Add(TP(anniu, data.AnURL));
                    elements.Add(DMButton(bzGui,$"UI_CustomUI_Cmd {data.AnCommand}", data.AnMin, data.AnMix, data.AnText, data.Anyanse, data.AnWeiZi), bzGui);
                    elements.Add(DMButton(null, null, data.SMMin, data.SMMix, data.SMText, "0 0 0 0", data.SMWeiZi), bzGui);
                }

            }


            elements.Add(DMButton(bzGui, null, configData.GuanBiMin, configData.GuanBiMix,configData.GuanBiWz,configData.GuanBiYS,configData.GuanBiWzsize), bzGui);

            CuiHelper.AddUi(player, elements);
        }
        [ConsoleCommand("UI_CustomUI_Cmd")]
        void cmdUI_CustomUI_Cmd(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;
            var cmd = "";
            cmd = string.Join(" ", arg.Args.Skip(0).ToArray());
            player.Command(cmd);
        }
    }
}