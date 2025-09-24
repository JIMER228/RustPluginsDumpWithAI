using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealFuelMonitor", "Kira", "1.0.0")]
    [Description("Adds a UI that displays the amount of fuel in the vehicle")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ZealFuelMonitor : RustPlugin
    {
        #region [References] / [Ссылки]

        private static ZealFuelMonitor _;

        #endregion

        #region [Vars] / [Переменные]

        private const string UIMainLayer = "ZealFuelMonitor.UI.MainLayer";
        private const string Sharp = "assets/content/ui/ui.background.tile.psd";

        #endregion 

        #region [Lists] / [Списки]

        private readonly List<FuelMonitor> _fuelMonitors = new List<FuelMonitor>();

        #endregion
   
        #region UI

        private void DrawUI_Test(BasePlayer player)
        {
            var ui = new CuiElementContainer();

            ui.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 1"
                },
                RectTransform =
                {
                    AnchorMax = "0.302 0.099",
                    AnchorMin = "0.338 0.036"
                }
            }, "Overlay", "HUD");

            ui.Add(new CuiElement
            {
                Name = "img",
                Parent = "HUD",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Url =
                            "https://cdn.discordapp.com/attachments/847462584873779220/847467427843276890/Untitled-1.png",
                        Color = "0.47 0.48 0.49 1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.647 0.101",
                        AnchorMax = "0.682 0.164"
                    }
                }
            });

            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Static UI] / [Статичное UI]

        private static readonly CuiElementContainer UIParent = new CuiElementContainer
        {
            new CuiElement
            {
                Name = UIMainLayer,
                Parent = "Overlay",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#a696841C"),
                        Material = "assets/content/ui/ui.background.tile.psd",
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.8156254 0.02222205",
                        AnchorMax = "0.835417 0.174"
                    }
                }
            }, 
            new CuiElement
            {
                Name = "UI_Monitor_Icon",
                Parent = UIMainLayer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#a69684"),
                        Sprite = "assets/icons/iscooking.png" 
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1842117 0.03660318",
                        AnchorMax = "0.8421105 0.1891164"
                    }
                }
            }
        };

        #endregion

        #region [MonoBehaviours]

        private class FuelMonitor : MonoBehaviour
        {
            public BasePlayer player;
            public EntityFuelSystem FuelSystem;
            private ItemDefinition _fuelItem;
            private double _uiStep;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                _._fuelMonitors.Add(this); 
                _fuelItem = ItemManager.FindItemDefinition("lowgradefuel");
                _uiStep = 0.7198624 / _fuelItem.stackable;
                DrawUI_MainLayer();
                InvokeRepeating(nameof(UpdateMonitor), 1f, 1f);
            }

            private void UpdateMonitor()
            {
                if (FuelSystem.GetFuelContainer().IsDestroyed) Destroy(this);
                DrawUI_Monitor();
            }

            private void DrawUI_MainLayer()
            {
                CuiHelper.DestroyUi(player, UIMainLayer);
                CuiHelper.AddUi(player, UIParent);
                _.NextTick(UpdateMonitor);
            }

            private void DrawUI_Monitor()
            {
                float fuelCount = FuelSystem.GetFuelAmount();
                var ui = new CuiElementContainer
                {
                    new CuiElement
                    {
                        Name = "Fuel.Line.BG",
                        Parent = UIMainLayer,
                        Components =
                        {
                            new CuiImageComponent {Color = HexToRustFormat("#AB581861"), Material = Sharp},
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.13} {0.2379218}", AnchorMax = $"{0.85} {0.9577842}"
                            }
                        }
                    },
                    new CuiElement
                    {
                        Name = "Fuel.Line",
                        Parent = UIMainLayer,
                        Components =
                        {
                            new CuiImageComponent {Color = HexToRustFormat("#c8773a"), Material = Sharp},
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.13} {0.2379218}",
                                AnchorMax = $"{0.85} {0.2379218 + (_uiStep * fuelCount)}"
                            }
                        }
                    },
                    {
                        new CuiLabel
                        {
                            Text =
                            {
                                Align = TextAnchor.MiddleCenter,
                                Color = HexToRustFormat("#fbd0b6"),
                                FontSize = 11,
                                Text = $"{fuelCount}"
                            },
                            RectTransform = {AnchorMin = "0 0.2", AnchorMax = "1 0.5"}
                        },
                        UIMainLayer, "Fuel.Count.Text"
                    }
                };


                CuiHelper.DestroyUi(player, "Fuel.Line.BG");
                CuiHelper.DestroyUi(player, "Fuel.Count.Text");
                CuiHelper.DestroyUi(player, "Fuel.Line");
                CuiHelper.AddUi(player, ui);
            }

            private void OnDestroy()
            {
                _._fuelMonitors.Remove(this);
                CuiHelper.DestroyUi(player, UIMainLayer);
            }
        }

        #endregion

        #region [Hooks] / [Крюки]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            _ = this;
            ServerMgr.Instance.StartCoroutine(ReloadMonitor());
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntityMounted(BaseMountable entity, BasePlayer player)
        {
            var vehicle = entity.GetComponentInParent<BaseVehicle>();
            if (vehicle == null | vehicle.GetFuelSystem() == null) return;
            var component = player.gameObject.AddComponent<FuelMonitor>();
            component.FuelSystem = vehicle.GetFuelSystem();
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntityDismounted(BaseMountable entity, BasePlayer player)
        {
            var vehicle = entity.GetComponentInParent<BaseVehicle>();
            if (vehicle == null) return;
            var component = player.GetComponent<FuelMonitor>();
            if (component == null) return;
            UnityEngine.Object.Destroy(component);
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            ServerMgr.Instance.StartCoroutine(UnloadMonitor());
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private static IEnumerator ReloadMonitor()
        {
            foreach (var vehicle in UnityEngine.Object.FindObjectsOfType<BaseVehicle>())
            {
                if (vehicle.GetDriver() != null)
                {
                    var component = vehicle.GetDriver().gameObject.AddComponent<FuelMonitor>();
                    component.FuelSystem = vehicle.GetFuelSystem();
                }

                yield return CoroutineEx.waitForSeconds(0.2f);
            }
        }

        private IEnumerator UnloadMonitor()
        {
            foreach (var monitor in _fuelMonitors)
            {
                UnityEngine.Object.Destroy(monitor);
                yield return CoroutineEx.waitForSeconds(0.2f);
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
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        #endregion
    }
}