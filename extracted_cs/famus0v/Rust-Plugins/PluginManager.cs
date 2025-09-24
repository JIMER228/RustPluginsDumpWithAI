using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using WebSocketSharp;

namespace Oxide.Plugins
{
    [Info("PluginManager", "Luminos", "1.0.0")]
    [Description("ControlPanel plugin add-on")]
    public class PluginManager : RustPlugin
    {
        [PluginReference] private Plugin ControlPanel, ImageLibrary;

        private const string UIMenu = "UI.Menu";
        private const string UIMain = "UI.Main";
        private const string Regular = "robotocondensed-regular.ttf";
        private static readonly WaitForSeconds WaitForSeconds = new WaitForSeconds(0.1f);

        private readonly Dictionary<string, string> _images = new Dictionary<string, string>
        {
            ["UI.Category.PluginManager.Statistic.Background"] = "https://i.imgur.com/lI5XPs8.png",
            ["UI.Category.PluginManager.Plugin.Background"] = "https://i.imgur.com/8LTQDH2.png",
        };

        public void OpenUIPluginManager(ulong userID)
        {
            var pluginlist = plugins.GetAll();
            var parent = $"{UIMain}.Category.PluginManager";
            var player = BasePlayer.FindByID(userID);
            var ui = new CuiElementContainer();

            ui.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0.1609375 0",
                    AnchorMax = "1 1"
                }
            }, UIMain, parent);

            ui.Add(new CuiElement
            {
                Name = $"{parent}.Statistic.Background",
                Parent = parent,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "UI.Category.PluginManager.Statistic.Background")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.07262576 0.7407407",
                        AnchorMax = "0.9273055 0.9264207"
                    }
                }
            });

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "0.39 0.40 0.44 1.00",
                    FontSize = 15,
                    Text = "999"
                },
                RectTransform =
                {
                    AnchorMin = "0.1159955 0.7259018",
                    AnchorMax = "0.1443202 0.9203818"
                }
            }, $"{parent}.Statistic.Background");

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "0.39 0.40 0.44 1.00",
                    FontSize = 15,
                    Text = "999"
                },
                RectTransform =
                {
                    AnchorMin = "0.1159955 0.41174",
                    AnchorMax = "0.1443202 0.6062205"
                }
            }, $"{parent}.Statistic.Background");

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "0.39 0.40 0.44 1.00",
                    FontSize = 15,
                    Text = "999"
                },
                RectTransform =
                {
                    AnchorMin = "0.1159955 0.0925929",
                    AnchorMax = "0.1443202 0.287074"
                }
            }, $"{parent}.Statistic.Background");

            CuiHelper.DestroyUi(player, parent);
            CuiHelper.AddUi(player, ui);
            OpenUIPluginList(player, parent, pluginlist);
        }

        private void OpenUIPluginList(BasePlayer player, string parent, IEnumerable<Plugin> pluginlist)
        {
            var ui = new CuiElementContainer();

            int x = 0, y = 0, num;
            foreach (var plugin in pluginlist.Take(12))
            {
                
                if (x >= 4)
                {
                    x = 0;
                    y++;
                }

                ui.Add(new CuiElement
                {
                    Name = $"{parent}.Plugin.{plugin.Filename}",
                    Parent = parent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage", "UI.Category.PluginManager.Plugin.Background")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.0726257 + (x * 0.223)} {0.5537045 - (y * 0.185)}",
                            AnchorMax = $"{0.2602286 + (x * 0.223)} {0.6834134 - (y * 0.185)}"
                        }
                    }
                });

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "0.39 0.40 0.44 1.00",
                        FontSize = 15,
                        Text = plugin.Name
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0.7638123",
                        AnchorMax = "1 1"
                    }
                }, $"{parent}.Plugin.{plugin.Filename}");

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.UpperLeft,
                        Color = "0.39 0.40 0.44 0.75",
                        FontSize = 11,
                        Text = $"Description : {plugin.Description ?? "NULL"}\n" +
                               $"Version : {plugin.Version}"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.03 0.2569794",
                        AnchorMax = "0.97 0.6924273"
                    }
                }, $"{parent}.Plugin.{plugin.Filename}");

                x++;
            }

            CuiHelper.AddUi(player, ui);
        }

        #region [Hooks]

        private void OnServerInitialized()
        {
            ServerMgr.Instance.StartCoroutine(LoadImages());
        }

        #endregion

        [ConsoleCommand("control.manager.plugin")]
        private void Tasd(ConsoleSystem.Arg args)
        {
            OpenUIPluginManager(args.Player().userID);
        }

        #region [Helpers]

        private IEnumerator LoadImages()
        {
            foreach (var image in _images)
            {
                ImageLibrary.Call("AddImage", image.Value, image.Key);
                yield return WaitForSeconds;
            }

            yield return 0;
        }

        #endregion
    }
}