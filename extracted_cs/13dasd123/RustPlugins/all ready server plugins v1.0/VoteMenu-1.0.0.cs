using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("VoteMenu", "BadMandarin", "1.0.0")]
    [Description("Система голосований")]
    class VoteMenu : RustPlugin
    {
        #region Classes
        private class Votes
        {
            public string Question;
            public string VoteEnd;
            public int VotesYes = 0;
            public int VotesNo = 0;
            public List<ulong> VotedPlayers = new List<ulong>();
            public Votes(string q, string end)
            {
                Question = q;
                VoteEnd = end;
            }
        }
        #endregion

        #region Variables
        private List<Votes> config;
        private string UI_Layer = "UI_VoteMenu";
        #endregion

        #region Oxide
        void OnServerInitialized()
        {
            PrintWarning("Plugin initialized! Author: BadMandarin");

            if (Interface.Oxide.DataFileSystem.ExistsDatafile("VoteMenu/Config"))
                config = Interface.Oxide?.DataFileSystem?.ReadObject<List<Votes>>("VoteMenu/Config");
            else
            {
                config = new List<Votes>()
                {
                    new Votes("Любишь раст?", DateTime.Now.AddDays(5).Date.ToShortDateString()),
                    new Votes("Любишь лошадок?", DateTime.Now.AddDays(15).Date.ToShortDateString())
                };
                Interface.Oxide.DataFileSystem.WriteObject("VoteMenu/Config", config);
            }
        }

        void Unload()
        {
            config.Clear();
        }
        #endregion

        #region Interface

        private void LoadVoteMenu(BasePlayer player, int page = 0)
        {
            CuiElementContainer container = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = true,
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-250 -200", OffsetMax = "250 240"
                        },
                        Image = {Color = GetColor("#000000", 0.3f), Material = "assets/content/ui/uibackgroundblur.mat" }
                    }, "Overlay", UI_Layer
                },
                {
                    new CuiButton
                    {
                        RectTransform = {AnchorMin = "-100{DarkPluginsID} -100{DarkPluginsID}", AnchorMax = "100{DarkPluginsID} 100{DarkPluginsID}" },
                        Button = { Color = "0 0 0 0", Close = UI_Layer },
                        Text = { Text = "" }
                    }, UI_Layer
                },
            };
            int c = 0;
            int y = 0;
            int d = 97;
            for(int i = 0; i < (config.Count - 4 * page < 4?config.Count - 4 * page : 4); i++)
            {
                c = i + 4 * page;
                Votes v = config.ElementAt(c);
                if (DateTime.Parse(v.VoteEnd) < DateTime.Now) continue;
                container.Add(new CuiElement
                {
                    Parent = UI_Layer,
                    Name = $"{UI_Layer}.Question.{c}",
                    Components =
                        {
                            new CuiImageComponent
                            {
                                Color = GetColor("#6E6E6E", 0.5f), Material = "assets/content/ui/uibackgroundblur.mat"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 1",
                                AnchorMax = $"1 1",
                                OffsetMin = $"10 {-97 - y*d}",
                                OffsetMax = $"-10 {-10 - y*d}"
                            },
                        }
                });

                container.Add(new CuiElement
                {
                    Parent = $"{UI_Layer}.Question.{c}",
                    Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"{v.Question}", Color = GetColor("#FFFFFF", 1f), Align = TextAnchor.MiddleLeft

                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 1",
                                OffsetMin = $"10 0",
                                OffsetMax = $"0 0"
                            },
                            new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                        }
                });

                container.Add(new CuiElement
                {
                    Parent = $"{UI_Layer}.Question.{c}",
                    Name = $"{UI_Layer}.ButtonNo.{c}",
                    Components =
                        {
                            new CuiImageComponent
                            {
                                Color = GetColor("#FA5858", 1f), Material = "assets/content/ui/uibackgroundblur.mat"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"1 1",
                                AnchorMax = $"1 1",
                                OffsetMin = $"-100 -40",
                                OffsetMax = $"-10 -10"
                            }
                        }
                });

                container.Add(new CuiElement
                {
                    Parent = $"{UI_Layer}.ButtonNo.{c}",
                    Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"{(v.VotedPlayers.Contains(player.userID)?$"{v.VotesNo}":"НЕТ")}", Color = GetColor("#FFFFFF", 1f), Align = TextAnchor.MiddleCenter

                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 1",
                                OffsetMin = $"0 0",
                                OffsetMax = $"0 0"
                            },
                            new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                        }
                });
                if (!v.VotedPlayers.Contains(player.userID))
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { Color = "0 0 0 0", Command = $"UI_VoteMenu {c} no" },
                        Text = { Text = "" }
                    }, $"{UI_Layer}.ButtonNo.{c}");
                }

                container.Add(new CuiElement
                {
                    Parent = $"{UI_Layer}.Question.{c}",
                    Name = $"{UI_Layer}.ButtonYes.{c}",
                    Components =
                        {
                            new CuiImageComponent
                            {
                                Color = GetColor("#82FA58", 1f), Material = "assets/content/ui/uibackgroundblur.mat"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"1 1",
                                AnchorMax = $"1 1",
                                OffsetMin = $"-200 -40",
                                OffsetMax = $"-110 -10"
                            }
                        }
                });

                container.Add(new CuiElement
                {
                    Parent = $"{UI_Layer}.ButtonYes.{c}",
                    Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"{(v.VotedPlayers.Contains(player.userID)?$"{v.VotesYes}":"ДА")}", Color = GetColor("#FFFFFF", 1f), Align = TextAnchor.MiddleCenter

                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 1",
                                OffsetMin = $"0 0",
                                OffsetMax = $"0 0"
                            },
                            new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                        }
                });
                if (!v.VotedPlayers.Contains(player.userID))
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { Color = "0 0 0 0", Command = $"UI_VoteMenu {c} yes" },
                        Text = { Text = "" }
                    }, $"{UI_Layer}.ButtonYes.{c}");
                }

                container.Add(new CuiElement
                {
                    Parent = $"{UI_Layer}.Question.{c}",
                    Name = $"{UI_Layer}.Date.{c}",
                    Components =
                        {
                            new CuiImageComponent
                            {
                                Color = GetColor("#000000", 0.5f)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"1 0",
                                AnchorMax = $"1 0",
                                OffsetMin = $"-200 10",
                                OffsetMax = $"-10 40"
                            }
                        }
                });

                container.Add(new CuiElement
                {
                    Parent = $"{UI_Layer}.Date.{c}",
                    Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"Завершится: {v.VoteEnd}", Color = GetColor("#FFFFFF", 1f), Align = TextAnchor.MiddleCenter

                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 1",
                                OffsetMin = $"0 0",
                                OffsetMax = $"0 0"
                            },
                            new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                        }
                });
                y++;
            }

            container.Add(new CuiElement
            {
                Parent = $"{UI_Layer}",
                Name = $"{UI_Layer}.Pages",
                Components =
                        {
                            new CuiImageComponent
                            {
                                Color = GetColor("#6E6E6E", 0.5f), Material = "assets/content/ui/uibackgroundblur.mat"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0.5 0",
                                AnchorMax = $"0.5 0",
                                OffsetMin = $"-80 10",
                                OffsetMax = $"80 45"
                            }
                        }
            });

            container.Add(new CuiElement
            {
                Parent = $"{UI_Layer}.Pages",
                Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"<     {page+1}     >", Color = GetColor("#FFFFFF", 1f), FontSize = 30, Align = TextAnchor.MiddleCenter

                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 1",
                                OffsetMin = $"0 0",
                                OffsetMax = $"0 0"
                            },
                            new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                        }
            });
            if (page > 0)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.5 1" },
                    Button = { Color = "0 0 0 0", Command = $"UI_VoteMenu {page-1}" },
                    Text = { Text = "" }
                }, $"{UI_Layer}.Pages");
            }


            if (config.Count() - 4 * page > 4)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = $"UI_VoteMenu {page + 1}" },
                    Text = { Text = "" }
                }, $"{UI_Layer}.Pages");
            }

            CuiHelper.DestroyUi(player, UI_Layer);
            CuiHelper.AddUi(player, container);

        }
        #endregion

        #region Commands
        [ConsoleCommand("UI_VoteMenu")]
        private void CMD_TakePrize(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length == 0) return;
            int res = 0;
            if (!Int32.TryParse(arg.Args[0], out res)) return;

            if (arg.Args.Length == 1)
            {
                LoadVoteMenu(arg.Player(), res);
                return;
            }

            if(arg.Args[1] == "yes")
            {
                config[res].VotesYes++;
                config[res].VotedPlayers.Add(arg.Player().userID);
                Interface.Oxide.DataFileSystem.WriteObject("VoteMenu/Config", config);
                LoadVoteMenu(arg.Player());
            }
            else
            {
                config[res].VotesNo++;
                config[res].VotedPlayers.Add(arg.Player().userID);
                Interface.Oxide.DataFileSystem.WriteObject("VoteMenu/Config", config);
                LoadVoteMenu(arg.Player());
            }
        }
        [ChatCommand("vote")]
        void Chat_Vote(BasePlayer player, string command, string[] args)
        {
            LoadVoteMenu(player);
        }
        #endregion

        #region Utils
        public static string GetColor(string hex, float alpha = 1f)
        {
            var color = ColorTranslator.FromHtml(hex);
            var r = Convert.ToInt16(color.R) / 255f;
            var g = Convert.ToInt16(color.G) / 255f;
            var b = Convert.ToInt16(color.B) / 255f;
            //var a = Convert.ToInt16(color.A) / {DarkPluginsID}f;

            return $"{r} {g} {b} {alpha}";
        }
        #endregion
    }
}
