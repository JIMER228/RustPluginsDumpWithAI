using System;
using System.Collections.Generic;
using System.Globalization;
using ConVar;
using Facepunch;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Physics = UnityEngine.Physics;

namespace Oxide.Plugins
{
    [Info("ZealTest", "Kira", "1.0.0")]
    [Description("")]
    class ZealTest : RustPlugin
    {
        [PluginReference] Plugin NTeleportation, ImageLibrary;
        private string Sharp = "assets/content/ui/ui.background.tile.psd";
        private string Blur = "assets/content/ui/uibackgroundblur.mat";
        private string radial = "assets/content/ui/ui.background.transparent.radial.psd";
        private string regular = "robotocondensed-regular.ttf";

        private List<string> Tests = new List<string>
        {
            "1",
            "1",
            "1",
            "1",
        };

        void DrawUI_Test(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "UI_Test");
            CuiElementContainer Gui = new CuiElementContainer();
            float leftPosition = Tests.Count / 2f * -125 - (Tests.Count - 1) / 2f * 10;
            Gui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = $"0 0",
                    AnchorMax = $"1 1"
                }
            }, "Overlay", "UI_Test");

            int num = 0;
            foreach (var elem in Tests)
            {
                Gui.Add(new CuiElement
                {
                    Name = elem,
                    Parent = "UI_Test",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "1 1 1 1",
                            FadeIn = 1f + (num * 1f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.675",
                            AnchorMax = "0.5 0.675",
                            OffsetMin = $"{leftPosition} -62.5",
                            OffsetMax = $"{leftPosition + 125} 62.5"
                        }
                    }
                });
                leftPosition += 135;
            }

            CuiHelper.AddUi(player, Gui);
        }

        private static Vector3 GetLookAtPosition(BasePlayer d)
        {
            RaycastHit h;
            Physics.Raycast(d.eyes.HeadRay(), out h, 1000,
                LayerMask.GetMask("Construction", "Deployed", "Terrain", "Water", "World"));
            return h.point;
        }

        private static BaseEntity GetLookAtEntity(BasePlayer d)
        {
            RaycastHit h;
            Physics.Raycast(d.eyes.HeadRay(), out h, 1000,
                LayerMask.GetMask("Player (Server)"));
            return h.GetEntity();
        }

        void OnServerInitialized()
        {
            Test();
        }

        void Unload()
        {
            Fix();
        }

        private void Test()
        {
        }

        private void Fix()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.userID == 0)
                {
                    UnityEngine.Object.Destroy(player.GetComponent<BasePlayer>());
                }
            } 
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
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
    }
}