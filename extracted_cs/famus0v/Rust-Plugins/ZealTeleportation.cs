using System;
using System.Globalization;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealTeleportation", "Kira", "1.0.0")]
    public class ZealTeleportation : RustPlugin
    {
        private const string UIParentRfTeleporter = "UI_Parent_RFTeleporter";
        private const string Sharp = "assets/content/ui/ui.background.tile.psd";
        private const string Blur = "assets/content/ui/uibackgroundblur.mat";
        private const string BlurInMenu = "assets/content/ui/uibackgroundblur-ingamemenu.mat";
        private const string Radial = "assets/content/ui/ui.background.transparent.radial.psd";
        private const string Regular = "robotocondensed-regular.ttf";
        private static ZealTeleportation _;

        private static readonly CuiElementContainer UITeleportParent = new CuiElementContainer
        {
            {
                new CuiPanel
                {
                    CursorEnabled = false,
                    Image =
                    {
                        Color = "0 0 0 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5"
                    }
                },
                "Overlay", UIParentRfTeleporter
            }
        };

        private static readonly CuiLabel UIPoint = new CuiLabel
        {
            Text =
            {
                Align = TextAnchor.MiddleCenter,
                Color = HexToRustFormat("#e97015"),
                FontSize = 35,
                Text = "•",
                FadeIn = 0.1f
            }
        };  

        public class RfTeleporter : MonoBehaviour
        {
            public BasePlayer player;
            public Detonator rfObject; 
            public InputState InputState;
 
            private void Awake()
            { 
                player = GetComponent<BasePlayer>();
                InputState = player.serverInput;
                _.PrintToChat("Create");
                DrawUI_Parent();
            }

            public void DrawUI_Parent()
            {
                CuiHelper.AddUi(player, UITeleportParent);
            }

            public void DrawUI_Loading()
            {
                var ui = new CuiElementContainer();

                const int pointCount = 10;
                for (var pointNumber = 0; pointNumber <= pointCount; pointNumber++)
                {
                    CuiHelper.DestroyUi(player, $"{UITeleportParent}.Point.{pointNumber}");
                    const int r = 50;
                    const double c = (double) pointCount / 2;
                    var rad = pointNumber / c * 3.14;
                    var x = r * Math.Cos(rad);
                    var y = r * Math.Sin(rad);
                    
                    ui.Add(new CuiLabel
                    {
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#e97015"),
                            FontSize = 35,
                            Text = "•",
                            FadeIn = 0.1f * pointNumber
                        },
                        RectTransform =
                        {
                            AnchorMin = $"{x - 30} {y - 30}",
                            AnchorMax = $"{x + 30} {y + 30}"
                        }
                    }, UIParentRfTeleporter, $"{UITeleportParent}.Point.{pointNumber}");
                }

                CuiHelper.AddUi(player, ui);
            }

            private void FixedUpdate()
            {
                if (InputState.IsDown(BUTTON.FIRE_PRIMARY))
                {
                    DrawUI_Loading();
                    _.PrintWarning(DateTime.Now.ToString("O"));
                }
            }

            private void OnDestroy()
            {
                _.PrintToChat("Destroy");
                CuiHelper.DestroyUi(player, UIParentRfTeleporter);
            }
        }

        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (newItem == null)
            {
                if (player.GetComponent<RfTeleporter>() != null)
                    UnityEngine.Object.Destroy(player.GetComponent<RfTeleporter>());
                return;
            }

            if (player.GetActiveItem().info.shortname != "rf.detonator" & player.GetComponent<RfTeleporter>() != null)
                UnityEngine.Object.Destroy(player.GetComponent<RfTeleporter>());
            if (player.GetActiveItem().info.shortname != "rf.detonator") return;
            var component = player.gameObject.AddComponent<RfTeleporter>();
            var rfobj = player.GetHeldEntity().GetComponent<Detonator>();
            component.rfObject = rfobj;
            PrintToChat(rfobj.ToString());
        }

        private void OnServerInitialized()
        {
            _ = this;
            Unload();
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                if (player.GetComponent<RfTeleporter>() != null)
                    UnityEngine.Object.Destroy(player.GetComponent<RfTeleporter>());
        }

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
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }
    }
}