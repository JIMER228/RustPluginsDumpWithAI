#region License (GPL v3)
/*
    Road Finder - Show and list roads, and use road points
    Copyright (c)2021 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion License (GPL v3)

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core.Libraries.Covalence;
using System;
using Oxide.Core;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("RoadFinder", "RFC1920", "1.0.6")]
    [Description("Allows admins to show or teleport to roads, and devs to use road points.")]

    internal class RoadFinder : CovalencePlugin
    {
        private ConfigData configData;
        private const string permUse = "roadfinder.use";
        public static Dictionary<string, Road> roads = new Dictionary<string, Road>();
        public static Dictionary<string, Road> rivers = new Dictionary<string, Road>();
        public static Dictionary<string, Road> rails = new Dictionary<string, Road>();

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        private bool HasPerm(IPlayer player) => player.IsAdmin || permission.UserHasPermission(player.Id, permUse);

        public class Road
        {
            public List<Vector3> points = new List<Vector3>();
            public float width;
            public float offset;
            public int topo;
        }

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permUse, this);
            LoadConfigVariables();
            FindRoadsAndRivers();
            FindRails();
            FindTunnels();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                { "NoPermission", "You don't have permission to use this command." },
                { "IncorrectUsage", "Incorrect usage! /road|/river [list/name]" },
                { "DoesNotExist", "The road/river/rail '{0}' doesn't exist. Use '/road list' for a list of roads, /river for a list of rivers, and /rail for a list of rails" },
                { "RoadList", "<color=#00ff00>Roads:</color>\n{0}" },
                { "RiverList", "<color=#00ff00>Rivers:</color>\n{0}" },
                { "RailList", "<color=#00ff00>Rails:</color>\n{0}" },
                { "start", "Start" },
                { "end", "End" },
                { "TeleportingTo", "Teleporting to the {1} of : {0} in {2} seconds..." },
                { "TeleportedTo", "Teleported to the {1} of : {0}" }
            }, this);
        }

        [Command("rail")]
        private void railCommand(IPlayer player, string command, string[] args)
        {
            roadCommand(player, command, args);
        }

        [Command("river")]
        private void riverCommand(IPlayer player, string command, string[] args)
        {
            roadCommand(player, command, args);
        }

        [Command("road")]
        private void roadCommand(IPlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                Message(player, "NoPermission");
                return;
            }

            if (args.Length == 0)
            {
                Message(player, "IncorrectUsage");
                return;
            }

            if (string.Equals(args[0], "list", StringComparison.OrdinalIgnoreCase))
            {
                BasePlayer bplayer = player.Object as BasePlayer;
                if (command == "river")
                {
                    foreach (KeyValuePair<string, Road> r in rivers)
                    {
                        bplayer?.SendConsoleCommand("ddraw.text", configData.Options.ShowAllTextTime, Color.green, r.Value.points[0] + new Vector3(0, 1.5f, 0), $"<size=20>{r.Key} {Lang("start", null)}</size>");
                        bplayer?.SendConsoleCommand("ddraw.text", configData.Options.ShowAllTextTime, Color.blue, r.Value.points.Last() + new Vector3(0, 1.5f, 0), $"<size=20>{r.Key} {Lang("end", null)}</size>");
                    }
                    string roadlist = "";
                    foreach (KeyValuePair<string, Road> r in rivers)
                    {
                        roadlist += r.Key + ": " + r.Value.points[0].ToString() + "\n";
                    }
                    Message(player, "RiverList", roadlist);
                }
                else if (command == "rail")
                {
                    foreach (KeyValuePair<string, Road> r in rails)
                    {
                        bplayer?.SendConsoleCommand("ddraw.text", configData.Options.ShowAllTextTime, Color.green, r.Value.points[0] + new Vector3(0, 1.5f, 0), $"<size=20>{r.Key} {Lang("start", null)}</size>");
                        bplayer?.SendConsoleCommand("ddraw.text", configData.Options.ShowAllTextTime, Color.blue, r.Value.points.Last() + new Vector3(0, 1.5f, 0), $"<size=20>{r.Key} {Lang("end", null)}</size>");
                    }
                    string roadlist = "";
                    foreach (KeyValuePair<string, Road> r in rails)
                    {
                        roadlist += r.Key + ": " + r.Value.points[0].ToString() + "\n";
                    }
                    Message(player, "RailList", roadlist);
                }
                else
                {
                    foreach (KeyValuePair<string, Road> r in roads)
                    {
                        bplayer?.SendConsoleCommand("ddraw.text", configData.Options.ShowAllTextTime, Color.green, r.Value.points[0] + new Vector3(0, 1.5f, 0), $"<size=20>{r.Key} {Lang("start", null)}</size>");
                        bplayer?.SendConsoleCommand("ddraw.text", configData.Options.ShowAllTextTime, Color.blue, r.Value.points.Last() + new Vector3(0, 1.5f, 0), $"<size=20>{r.Key} {Lang("end", null)}</size>");
                    }
                    string roadlist = "";
                    foreach (KeyValuePair<string, Road> r in roads)
                    {
                        roadlist += r.Key + ": " + r.Value.points[0].ToString() + "\n";
                    }
                    Message(player, "RoadList", roadlist);
                }
                return;
            }

            int point = 0;
            string roadName = null;
            string send = "start";
            bool tp = true;

            if (string.Equals(args[0], "show", StringComparison.OrdinalIgnoreCase) && args.Length > 1)
            {
                BasePlayer bplayer = player.Object as BasePlayer;
                List<string> newargs = new List<string>(args);
                newargs.RemoveAt(0);
                roadName = string.Join(" ", newargs).ToLower().Titleize();
                roadName = FixRoadName(roadName, command == "river");

                if (command == "river")
                {
                    bplayer?.SendConsoleCommand("ddraw.text", configData.Options.ShowOneTextTime, Color.green, rivers[roadName].points[0] + new Vector3(0, 1.5f, 0), $"<size=20>{roadName} {Lang("start", null)}</size>");
                }
                else if (command == "rail")
                {
                    bplayer?.SendConsoleCommand("ddraw.text", configData.Options.ShowOneTextTime, Color.green, rails[roadName].points[0] + new Vector3(0, 1.5f, 0), $"<size=20>{roadName} {Lang("start", null)}</size>");
                }
                else
                {
                    bplayer?.SendConsoleCommand("ddraw.text", configData.Options.ShowOneTextTime, Color.green, roads[roadName].points[0] + new Vector3(0, 1.5f, 0), $"<size=20>{roadName} {Lang("start", null)}</size>");
                }

                if (configData.Options.ShowOneAllPoints)
                {
                    int i = -1;
                    if (command == "river")
                    {
                        foreach (Vector3 pt in rivers[roadName].points)
                        {
                            i++;
                            if (i == 0 || i == rivers[roadName].points.Count - 1) continue;
                            string d = Math.Round(Vector3.Distance(rivers[roadName].points[i], rivers[roadName].points[i - 1]), 2).ToString();
                            bplayer?.SendConsoleCommand("ddraw.text", 300, Color.yellow, pt + new Vector3(0, 1.5f, 0), $"<size=20>{i.ToString()} ({d})m</size>");
                        }
                    }
                    else if (command == "rail")
                    {
                        foreach (Vector3 pt in rails[roadName].points)
                        {
                            i++;
                            if (i == 0 || i == rails[roadName].points.Count - 1) continue;
                            string d = Math.Round(Vector3.Distance(rails[roadName].points[i], rails[roadName].points[i - 1]), 2).ToString();
                            bplayer?.SendConsoleCommand("ddraw.text", 300, Color.yellow, pt + new Vector3(0, 1.5f, 0), $"<size=20>{i.ToString()} ({d})m</size>");
                        }
                    }
                    else
                    {
                        foreach (Vector3 pt in roads[roadName].points)
                        {
                            i++;
                            if (i == 0 || i == roads[roadName].points.Count - 1) continue;
                            string d = Math.Round(Vector3.Distance(roads[roadName].points[i], roads[roadName].points[i - 1]), 2).ToString();
                            bplayer?.SendConsoleCommand("ddraw.text", 300, Color.yellow, pt + new Vector3(0, 1.5f, 0), $"<size=20>{i.ToString()} ({d})m</size>");
                        }
                    }
                }
                bplayer?.SendConsoleCommand("ddraw.text", configData.Options.ShowOneTextTime, Color.blue, roads[roadName].points.Last() + new Vector3(0, 1.5f, 0), $"<size=20>{roadName} {Lang("end",null)}</size>");
                tp = false;
            }
            else if (string.Equals(args[0], "start", StringComparison.OrdinalIgnoreCase))
            {
                List<string> newargs = new List<string>(args);
                newargs.RemoveAt(0);
                roadName = string.Join(" ", newargs).ToLower().Titleize();
                roadName = FixRoadName(roadName);
            }
            else if (string.Equals(args[0], "end", StringComparison.OrdinalIgnoreCase))
            {
                List<string> newargs = new List<string>(args);
                newargs.RemoveAt(0);
                roadName = string.Join(" ", newargs).ToLower().Titleize();
                roadName = FixRoadName(roadName);
                point = roads[roadName].points.Count - 1;
                send = "end";
            }
            else
            {
                roadName = string.Join(" ", args).ToLower().Titleize();
                roadName = FixRoadName(roadName, command == "river");
            }

            if (!roads.ContainsKey(roadName) && !rivers.ContainsKey(roadName) && !rails.ContainsKey(roadName))
            {
                Message(player, "DoesNotExist", roadName);
                return;
            }

            if (tp)
            {
                if (command == "river")
                {
                    GenericPosition pos = ToGeneric(rivers[roadName].points[point]);
                    Message(player, "TeleportingTo", roadName, send, "5");
                    pos.Y = TerrainMeta.HeightMap.GetHeight(rivers[roadName].points[point]);
                    timer.Once(5f, () => { player.Teleport(pos.X, pos.Y, pos.Z); Message(player, "TeleportedTo", send, roadName); });
                }
                else if (command == "rail")
                {
                    GenericPosition pos = ToGeneric(rails[roadName].points[point]);
                    Message(player, "TeleportingTo", roadName, send, "5");
                    pos.Y = TerrainMeta.HeightMap.GetHeight(rails[roadName].points[point]);
                    timer.Once(5f, () => { player.Teleport(pos.X, pos.Y, pos.Z); Message(player, "TeleportedTo", send, roadName); });
                }
                else
                {
                    GenericPosition pos = ToGeneric(roads[roadName].points[point]);
                    Message(player, "TeleportingTo", roadName, send, "5");
                    timer.Once(5f, () => { player.Teleport(pos.X, pos.Y, pos.Z); Message(player, "TeleportedTo", send, roadName); });
                }
            }
        }

        public string FixRoadName(string roadName, bool river = false, bool rail = false)
        {
            if (roadName.Length < 3)
            {
                if (river)
                {
                    roadName = "River " + roadName;
                }
                else if (rail)
                {
                    roadName = "Rail " + roadName;
                }
                else
                {
                    roadName = "Road " + roadName;
                }
            }
            return roadName;
        }

        private GenericPosition ToGeneric(Vector3 vec) => new GenericPosition(vec.x, vec.y, vec.z);

        #region InboundHooks
        private List<string> GetRoadNames() => roads.Keys.ToList();
        private Dictionary<string, Road> GetRoads() => roads;
        private Road GetRoad(string name) => roads[name];
        private List<Vector3> GetRoadPoints(string name) => roads[name].points;

        private List<string> GetRiverNames() => rivers.Keys.ToList();
        private Dictionary<string, Road> GetRivers() => rivers;
        private Road GetRiver(string name) => rivers[name];
        private List<Vector3> GetRiverPoints(string name) => rivers[name].points;

        private List<string> GetRailNames() => rails.Keys.ToList();
        private Dictionary<string, Road> GetRails() => rails;
        private Road GetRail(string name) => rails[name];
        private List<Vector3> GetRailPoints(string name) => rails[name].points;
        #endregion

        private void FindTunnels()
        {
            foreach(DungeonGridCell tunnel in UnityEngine.Object.FindObjectsOfType<DungeonGridCell>())
            {
//                Puts($"{tunnel.name}");
            }
        }

        private void FindRails()
        {
            List<PathList> raillist = typeof(TerrainPath).GetField("Rails", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)?.GetValue(TerrainMeta.Path) as List<PathList>;
            foreach (PathList rail in raillist)
            {
                if (rails.ContainsKey(rail.Name)) continue;
                string roadname = rail.Name;
                rails.Add(roadname, new Road()
                {
                    topo   = rail.Topology,
                    width  = rail.Width,
                    offset = rail.TerrainOffset
                });

                foreach (Vector3 point in rail.Path.Points)
                {
                    rails[roadname].points.Add(point);
                }
            }
        }

        private void FindRoadsAndRivers()
        {
            foreach (PathList x in TerrainMeta.Path.Roads)
            {
                if (roads.ContainsKey(x.Name)) continue;
                string roadname = x.Name;
                roads.Add(roadname, new Road()
                {
                    topo   = x.Topology,
                    width  = x.Width,
                    offset = x.TerrainOffset
                });

                foreach (Vector3 point in x.Path.Points)
                {
                    roads[roadname].points.Add(point);
                }
            }

            foreach (PathList x in TerrainMeta.Path.Rivers)
            {
                if (rivers.ContainsKey(x.Name)) continue;
                string roadname = x.Name;
                rivers.Add(roadname, new Road()
                {
                    topo   = x.Topology,
                    width  = x.Width,
                    offset = x.TerrainOffset
                });

                foreach (Vector3 point in x.Path.Points)
                {
                    rivers[roadname].points.Add(point);
                }
            }
        }

        #region config
        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData
            {
                Options = new Options()
                {
                    ShowAllTextTime = 30,
                    ShowOneTextTime = 60,
                    ShowOneAllPoints = true
                },
                Version = Version
            };
            SaveConfig(config);
        }

        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();

            configData.Version = Version;
            SaveConfig(configData);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }

        public class ConfigData
        {
            public Options Options;
            public VersionNumber Version;
        }

        public class Options
        {
            public float ShowAllTextTime;
            public float ShowOneTextTime;
            public bool ShowOneAllPoints;
        }
        #endregion
    }
}
