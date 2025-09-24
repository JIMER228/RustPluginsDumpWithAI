using Oxide.Core;
using Oxide.Ext.LocalFiles;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Oxide.Plugins
{
    [Info("Timed SignArtist", "RFC1920", "1.0.6")]
    [Description("Update signs on a timer")]
    internal class TimeArtist : RustPlugin
    {
        [PluginReference]
        private readonly Plugin SignArtist;

        private const string permUse = "timeartist.use";
        private ConfigData configData;

        public Dictionary<ulong, ScheduleInfo> sSchedule = new Dictionary<ulong, ScheduleInfo>();
        private Timer scheduleTimer;

        public class ScheduleInfo
        {
            public int minutes = 5;
            public int ticks;
            public int index;
            public bool enabled = true;
            public SortedDictionary<int, string> urls;
        }

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Message(Lang(key, player.Id, args));
        private void LMessage(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["notauthorized"] = "You don't have permission to use this command.",
                ["addedsign"] = "Sign {0} added to schedule with defaults.  You will need to add urls with urla.",
                ["removedsign"] = "Sign {0} removed from schedule.",
                ["signinfo"] = "Sign info ({0}):\n{1}",
                ["minset"] = "Sign minutes/skip count set to {0}",
                ["enable"] = "Sign cycle set to {0}",
                ["urla"] = "Added URL {0} to sign.",
                ["urlr"] = "Removed URL {0} from sign.",
                ["urlc"] = "Sign category set to {0}.  Make sure there are some files there...",
                ["help"] = "TimeArtist:\n  Walk up to a sign and type /ta to add to the queue.\n  After adding a sign, type /ta urla https://sign.url/pic.png to add a URL.\n  Type, e.g. /ta urlr https://sign.url/pic.png to remove a specific URL.\n  Type /ta min 1-something to set the number of cycles to skip for rotation.\n  Type /ta enable to enable/disable.\n\nType /ta again to see the current status."
            }, this);
        }

        private void OnServerInitialized()
        {
            LoadConfigVariables();
            AddCovalenceCommand("ta", "cmdScheduleSign");
            permission.RegisterPermission(permUse, this);
            LoadData();
            RunSchedule();
        }

        private void LoadData()
        {
            sSchedule = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, ScheduleInfo>>(Name + "/schedule");
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name + "/schedule", sSchedule);
        }

        private void DoLog(string message)
        {
            if (configData.debug) Puts(message);
        }

        [Command("ta")]
        private void cmdScheduleSign(IPlayer iplayer, string command, string[] args)
        {
            if (!iplayer.HasPermission(permUse)) { Message(iplayer, "notauthorized"); return; }
            BasePlayer player = iplayer.Object as BasePlayer;
            Signage sign;

            if (args.Length > 0)
            {
                IsLookingAtSign(player, out sign);
                if (sign == null) return;

                if (!sSchedule.ContainsKey(sign.net.ID.Value)) return;
                ScheduleInfo current = sSchedule[sign.net.ID.Value];

                switch(args[0])
                {
                    case "remove":
                    case "rem":
                    case "delete":
                    case "del":
                        sSchedule.Remove(sign.net.ID.Value);
                        Message(iplayer, "removedsign", sign.net.ID.ToString());
                        break;
                    case "urlc":
                        // Switch sign to category mode, using only files from that category
                        if (args.Length > 1)
                        {
                            current.urls = new SortedDictionary<int, string>
                            {
                                { 99999, args[1] }
                            };
                            Message(iplayer, "urlc", args[1]);
                        }
                        break;
                    case "urla":
                        if (args.Length > 1)
                        {
                            if (current.urls.ContainsKey(99999)) current.urls.Remove(99999);
                            int l = current.urls.Count;
                            current.urls.Add(l, args[1]);
                            Message(iplayer, "urla", args[1]);
                        }
                        break;
                    case "urlr":
                        if (args.Length > 1 && current.urls.ContainsValue(args[1]))
                        {
                            if (current.urls.ContainsKey(99999)) current.urls.Remove(99999);
                            SortedDictionary<int, string> newlist = new SortedDictionary<int, string>();
                            int i = 0;
                            foreach (KeyValuePair<int, string> urls in current.urls)
                            {
                                if (urls.Value == args[1]) continue;
                                newlist.Add(i, urls.Value);
                                i++;
                            }
                            current.urls = newlist;
                            Message(iplayer, "urlr", args[1]);
                        }
                        break;
                    case "min":
                    case "skip":
                        if (args.Length > 1)
                        {
                            current.minutes = Convert.ToInt32(args[1]);
                            Message(iplayer, "minset", args[1]);
                        }
                        break;
                    case "enable":
                        if (args.Length > 1)
                        {
                            current.enabled = !current.enabled;
                            Message(iplayer, "enable", current.enabled.ToString());
                        }
                        break;
                }
                SaveData();
                return;
            }

            if (IsLookingAtSign(player, out sign))
            {
                if (sSchedule.ContainsKey(sign.net.ID.Value))
                {
                    ScheduleInfo signinfo = sSchedule[sign.net.ID.Value];
                    string output = "URLs:\n";
                    foreach (KeyValuePair<int, string> url in signinfo.urls)
                    {
                        output += $"  {url.Key}: {url.Value}\n";
                    }
                    output += $"Current url index = {signinfo.index}\n";
                    output += $"Cycle enable set to: {signinfo.enabled}\n";
                    output += $"Change every {signinfo.minutes} period ({configData.rotPeriod} seconds).\n";
                    output += $"(current skip count at {signinfo.ticks}).\n";
                    Message(iplayer, "signinfo", sign.net.ID.ToString(), output);
                }
                else
                {
                    sSchedule.Add(sign.net.ID.Value, new ScheduleInfo()
                    {
                        minutes = 5,
                        ticks = 0,
                        index = 0,
                        urls = new SortedDictionary<int, string>()
                    });
                    Message(iplayer, "addedsign", sign.net.ID.ToString());
                    SaveData();
                }
            }
            else
            {
                Message(iplayer, "help");
            }
        }

        private bool IsLookingAtSign(BasePlayer player, out Signage sign)
        {
            RaycastHit hit;
            sign = null;

            if (Physics.Raycast(player.eyes.HeadRay(), out hit, configData.distance))
            {
                sign = hit.GetEntity() as Signage;
            }

            return sign != null;
        }

        private void RunSchedule()
        {
            if (sSchedule == null) return;
            if (!configData.enabled) return;
            DoLog("Running schedule...");

            foreach (KeyValuePair<ulong, ScheduleInfo> signs in sSchedule)
            {
                signs.Value.ticks++;
                if (signs.Value.ticks >= signs.Value.minutes && signs.Value.urls.Count > 0)
                {
                    signs.Value.ticks = 0;
                    signs.Value.index++;
                    if (signs.Value.index >= signs.Value.urls.Count && !configData.UseLocalFiles) signs.Value.index = 0;
                    Signage sign = BaseNetworkable.serverEntities.Find(new NetworkableId(signs.Key)) as Signage;
                    if (sign != null)
                    {
                        string newurl = "";// signs.Value.urls[signs.Value.index];
                        if (configData.UseLocalFiles)
                        {
                            string category = "";
                            string fname = "";
                            if (signs.Value.urls.Count == 1 && signs.Value.urls.ContainsKey(99999))
                            {
                                category = signs.Value.urls[99999];
                                List<int> cats = LocalFilesExt.categories[category];
                                List<LocalFilesExt.FileMeta> fl = new List<LocalFilesExt.FileMeta>();
                                foreach (KeyValuePair<int, LocalFilesExt.FileMeta> file in LocalFilesExt.localFiles)
                                {
                                    if (cats.Contains(file.Key))
                                    {
                                        fl.Add(file.Value);
                                    }
                                }
                                if (fl.Count > 0)
                                {
                                    if (signs.Value.index >= fl.Count) signs.Value.index = 0;
                                    fname = fl[signs.Value.index].FileName;
                                }
                                else
                                {
                                    Puts("No files found in category");
                                }
                            }
                            else
                            {
                                fname = signs.Value.urls[signs.Value.index];
                            }
                            //DoLog($"Calling LocalFiles to get image {signs.Value.urls[signs.Value.index]} with name {fname}");
                            try
                            {
                                int index = LocalFilesExt.fileList[fname];
                                LocalFilesExt.FileMeta data = LocalFilesExt.localFiles[index];
                                newurl = "file://" + data.Dir + Path.DirectorySeparatorChar + data.FileName;
                                //Puts(newurl);
                                //DoLog($"Calling SignArtist to add {signs.Value.urls[signs.Value.index]}");
                                SignArtist.Call("API_SkinSign", null, sign, newurl, false);
                            }
                            catch
                            {
                                DoLog("Failed to find LocalFile");
                            }
                        }
                        else
                        {
                            if (signs.Value.ticks >= signs.Value.minutes && signs.Value.urls.Count > 0)
                            {
                                DoLog($"Calling SignArtist to add {signs.Value.urls[signs.Value.index]}");
                                SignArtist.Call("API_SkinSign", null, sign, signs.Value.urls[signs.Value.index], false);
                            }
                        }
                    }
                }
            }

            SaveData();
            scheduleTimer = timer.Once(configData.rotPeriod, RunSchedule);
        }

        #region config
        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData
            {
                enabled = true,
                debug = false,
                rotPeriod = 30f,
                distance = 3f,
                UseLocalFiles = true,
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
            public bool enabled;
            public bool debug;
            public float rotPeriod;
            public float distance;
            public bool UseLocalFiles;

            public VersionNumber Version;
        }
        #endregion
    }
}
