using System;
using System.Globalization;
using System.Linq;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealRestart", "Kira", "1.0.0")]
    public class ZealRestart : RustPlugin
    {
        private const string Sharp = "assets/content/ui/ui.background.tile.psd";
        private const string Blur = "assets/content/ui/uibackgroundblur.mat";
        private const string BlurInMenu = "assets/content/ui/uibackgroundblur-ingamemenu.mat";
        private const string Radial = "assets/content/ui/ui.background.transparent.radial.psd";
        private const string UILayer = "UI_Layer_Restart";

        public class RestartController : MonoBehaviour
        {
            private BasePlayer _player;
            public int time;

            private void Awake()
            {
                _player = GetComponent<BasePlayer>();
                time = 300;
                DrawUI_Layer(_player);
                DrawUI_RestartRemaning(_player, time);
                InvokeRepeating(nameof(Timer), 1f, 1f);
            }

            public void Timer()
            {
                if (time <= 0) Destroy(this);
                if (_player == null) Destroy(this);
                time--;
                DrawUI_RestartRemaning(_player, time);
            }

            private static void DrawUI_Layer(BasePlayer player)
            {
                var ui = new CuiElementContainer();

                ui.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image =
                    {
                        Color = HexToRustFormat("#D22424CD"),
                        Material = BlurInMenu
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0.8203704",
                        AnchorMax = "1 0.9045653"
                    }
                }, "Overlay", UILayer);
                ui.Add(new CuiElement
                {
                    Name = "IC1",
                    Parent = UILayer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Sprite = "assets/icons/warning_2.png"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.00625 0",
                            AnchorMax = "0.05360967 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#00000042"),
                            Distance = "0.5 0.5"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = "IC2",
                    Parent = UILayer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Sprite = "assets/icons/warning_2.png"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.9453034 0",
                            AnchorMax = "0.9926611 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#00000042"),
                            Distance = "0.5 0.5"
                        }
                    }
                });

                CuiHelper.DestroyUi(player, UILayer);
                CuiHelper.AddUi(player, ui);
            }

            private static void DrawUI_RestartRemaning(BasePlayer player, int time)
            {
                var ui = new CuiElementContainer();

                ui.Add(new CuiElement
                {
                    Name = UILayer + "TimerRemaning",
                    Parent = UILayer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 30,
                            Text = $"Внимание, рестарт произойдёт через {time} сек"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#00000042"),
                            Distance = "0.5 0.5"
                        }
                    }
                });

                var sound = new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0,
                    new Vector3(),
                    new Vector3());
                EffectNetwork.Send(sound, player.Connection);
                CuiHelper.DestroyUi(player, UILayer + "TimerRemaning");
                CuiHelper.AddUi(player, ui);
            }

            private void OnDestroy()
            {
                CuiHelper.DestroyUi(_player, UILayer);
            }
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList.Where(player => player.GetComponent<RestartController>() != null))
                UnityEngine.Object.Destroy(player.GetComponent<RestartController>());
        }

        [ConsoleCommand("restartt")]
        private void StartRestart(ConsoleSystem.Arg args)
        {
            if (args.Player().GetComponent<RestartController>() != null)
            {
                UnityEngine.Object.Destroy(args.Player().GetComponent<RestartController>());
                return;
            }

            args.Player().gameObject.AddComponent<RestartController>();
        }

        [ConsoleCommand("restartt.close")]
        private void StopRestart(ConsoleSystem.Arg args)
        {
            UnityEngine.Object.Destroy(args.Player().GetComponent<RestartController>());
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