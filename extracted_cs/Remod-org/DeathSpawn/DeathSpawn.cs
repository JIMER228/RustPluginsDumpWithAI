using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Death Spawn Points", "RFC1920", "0.0.1")]
    class DeathSpawn : CovalencePlugin
    {
        const string DGUI = "deathspawn.gui";
        Dictionary<string, Vector3> teleport = new Dictionary<string, Vector3>();

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        void Init()
        {
            FindMonuments();
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            timer.Once(4, () => ShowGUI(player, null, null));
        }

        void Unload()
        {
            foreach(var pl in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(pl, DGUI);
            }
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["respawnat"] = "Respawn At:",
                ["random"] = "Random",
                ["outpost"] = "Outpost",
                ["bandit"] = "Bandit"
            }, this);
        }

        [Command("plmove")]
        private void MovePlayer(IPlayer iplayer, string command, string[] args)
        {
            if (iplayer.Health.Equals(0))
            {
                var player = iplayer.Object as BasePlayer;

                if (args.Length > 0)
                {
                    CuiHelper.DestroyUi(player, DGUI);
                    player.RespawnAt(teleport[args[0]], new Quaternion());
                }
                else
                {
                    CuiHelper.DestroyUi(player, DGUI);
                    player.Respawn();
                }
            }
        }

        [Command("plg")]
        private void ShowGUI(BasePlayer player, string command, string[] args)
        {
            if (command == "plg" && !player.IsAdmin) return;

            CuiHelper.DestroyUi(player, DGUI);

            CuiElementContainer container = UI.Container(DGUI, UI.Color("334b33", 1f), "0.824 0.1", "0.948 0.17", true, "Overlay");
            UI.Label(ref container, DGUI, UI.Color("#ffffff", 1f), Lang("respawnat"), 14, "0.1 0.5", "0.9 0.9");
            UI.Button(ref container, DGUI, UI.Color("#337733", 1f), Lang("outpost"), 12, "0.05 0.1", "0.35 0.4", $"plmove outpost");
            UI.Button(ref container, DGUI, UI.Color("#337733", 1f), Lang("bandit"), 12, "0.37 0.1", "0.665 0.4", $"plmove bandit");
            UI.Button(ref container, DGUI, UI.Color("#337733", 1f), Lang("random"), 12, "0.69 0.1", "0.94 0.4", $"plmove ");
            CuiHelper.AddUi(player, container);
        }

        void FindMonuments()
        {
            string name = null;
            int i = 0;
            foreach (MonumentInfo monument in UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
            {
                name = Regex.Match(monument.name, @"\w{6}\/(.+\/)(.+)\.(.+)").Groups[2].Value.Replace("_", " ").Replace(" 1", "").Titleize();

                if(monument.name.Contains("compound"))
                {
                    i++;
                    List<BaseEntity> ents = new List<BaseEntity>();
                    Vis.Entities(monument.transform.position, 50, ents);
                    foreach(BaseEntity entity in ents)
                    {
                        if(entity.PrefabName.Contains("piano"))
                        {
                            Vector3 outpost = entity.transform.position + new Vector3(1f, 0.1f, 1f);
                            if (teleport.ContainsKey("outpost")) teleport["outpost"] = outpost;
                            else teleport.Add("outpost", outpost);
                        }
                    }
                }
                else if(monument.name.Contains("bandit"))
                {
                    i++;
                    List<BaseEntity> ents = new List<BaseEntity>();
                    Vis.Entities(monument.transform.position, 50, ents);
                    foreach(BaseEntity entity in ents)
                    {
                        if(entity.PrefabName.Contains("workbench"))
                        {
                            Vector3 bandit = Vector3.Lerp(monument.transform.position, entity.transform.position, 0.45f) + new Vector3(0, 1.5f, 0);
                            if (teleport.ContainsKey("bandit")) teleport["bandit"] = bandit;
                            else teleport.Add("bandit", bandit);
                        }
                    }
                }
                if (i > 1) break;
            }
        }

        public static class UI
        {
            public static CuiElementContainer Container(string panel, string color, string min, string max, bool useCursor = false, string parent = "Overlay")
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = { Color = color },
                            RectTransform = {AnchorMin = min, AnchorMax = max},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panel
                    }
                };
                return container;
            }
            public static void Label(ref CuiElementContainer container, string panel, string color, string text, int size, string min, string max, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = min, AnchorMax = max }
                },
                panel);

            }
            public static void Button(ref CuiElementContainer container, string panel, string color, string text, int size, string min, string max, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0f },
                    RectTransform = { AnchorMin = min, AnchorMax = max },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }
            public static string Color(string hexColor, float alpha)
            {
                if(hexColor.StartsWith("#"))
                {
                    hexColor = hexColor.Substring(1);
                }
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
    }
}
