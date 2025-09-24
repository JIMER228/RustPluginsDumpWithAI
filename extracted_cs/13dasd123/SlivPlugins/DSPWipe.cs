using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Oxide.Plugins
{
    [Info("Discord Server Panel Wipe", "Aimon", "1.0.0")]
    [Description("Adds a wipe functionality to discord server panel.")]
    public class DSPWipe : CovalencePlugin
    {
        #region Global Variables
        private static string oldslash = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "/" : "\\";
        private static string newslash = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\\" : "/";
        private const string wipePerm = "dspwipe.mapwipe";
        private string backupconfig = "";
        private readonly string identityfolder = File.Exists(Path.Combine(Rust.Application.installPath, $"server/{ConVar.Server.identity}/").Replace(oldslash, newslash))? Path.Combine(Rust.Application.installPath, $"server/{ConVar.Server.identity}/").Replace(oldslash, newslash): File.Exists(Path.Combine(Rust.Application.installPath, $"cfg/server.cfg").Replace(oldslash, newslash)) ? Rust.Application.installPath.Replace(oldslash, newslash): "";
        private int oldmapseed;
        private int oldmapsize;
        private List<string> lines;
        #endregion Global Variables
        #region Configuration
        private ConfigData _configData;
        private int wipestate = 0;
        private class ConfigData
        {
            public string LogFileName = "DSPWipe";

            [JsonProperty(PropertyName = "Log to console (true/false)")]
            public bool LogToConsole = true;

            [JsonProperty(PropertyName = "Backup (true/false)")]
            public bool backup = false;
        }
        protected override void LoadConfig()
        {
            try
            {
                base.LoadConfig();
                _configData = Config.ReadObject<ConfigData>();
                SaveConfig(_configData);
            }
            catch (Exception)
            {
                PrintError(Lang("ConfigIssue"));
                return;
            }
        }
        private void OnServerInitialized()
        {
            oldmapseed = ConVar.Server.seed;
            oldmapsize = ConVar.Server.worldsize;
            RegisterCommands();
            RegisterPerms();
        }

        protected override void LoadDefaultConfig()
        {
            Puts(Lang("NewConfig"));
            _configData = new ConfigData();
            SaveConfig(_configData);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }


        private void Loaded()
        {
        }
        private void Unload()
        {
            if (identityfolder != "")
            {
                if (wipestate != 0)
                {
                    lines = new List<string>(Directory.GetFiles(identityfolder, "*.sav*"));
                    string backupDirectory = $"{identityfolder}Backup";
                    if (_configData.backup)
                        if (!Directory.Exists(backupDirectory))
                            Directory.CreateDirectory(backupDirectory);
                    foreach (string savFile in lines)
                    {
                        if (_configData.backup)
                            File.Copy(savFile, $"{backupDirectory}{newslash}{Path.GetFileName(savFile)}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm")}");

                        File.Delete(savFile);
                    }
                    if (_configData.backup)
                        if (_configData.LogToConsole)
                        {
                            PrintWarning($"Server has backedup your map files in {backupDirectory}");
                        }
                }
            }
        }
        #endregion Configuration
        #region Commands

        public void RegisterCommands()
        {
            AddCovalenceCommand("dspmap", nameof(WipeCommand));
        }

        public void RegisterPerms()
        {
            permission.RegisterPermission(wipePerm, this);
        }

        #endregion Commands
        #region GameCommands
        private void WipeCommand(IPlayer player, string command, string[] args)
        {
            // No Permission
            if (!player.HasPermission(wipePerm))
            {
                player.Reply("No Permission");
                return;
            }
            if (args.Length == 0)
            {
                if (wipestate != 0)
                {
                    MapWipe(oldmapsize, oldmapseed, _configData.backup, true);
                    player.Reply("Sucessfully Canceled wipe request!");
                }
                else
                {
                    player.Reply("Syntax error!");
                }
                return;
            }
            if (args.Length == 1)
            {
                int seed;
                if (int.TryParse(args[0], out seed))
                {
                    if (MapWipe(oldmapsize, seed, _configData.backup, false))
                        player.Reply($"Successfully sent map wipe with {oldmapsize} world size {seed} seed and backup {_configData.backup}!");
                    else
                        player.Reply($"Error with DSP WIPE, Please check console!");
                }
                else
                {
                    player.Reply("Syntax error!");
                }
                return;
            }
            if (args.Length == 2)
            {
                int seed;
                int mapsize;
                if (int.TryParse(args[0], out seed))
                    if (int.TryParse(args[1], out mapsize))
                    {
                        if(MapWipe(mapsize, seed, _configData.backup, false))
                            player.Reply($"Successfully sent map wipe with {mapsize} world size {seed} seed and backup {_configData.backup}!");
                        else
                            player.Reply($"Error with DSP WIPE, Please check console!");
                    }
                    else
                    {
                        player.Reply("Syntax error!");
                    }
                return;
            }
            if (args.Length == 3)
            {
                int seed;
                int mapsize;
                int back;
                if (int.TryParse(args[0], out seed))
                    if (int.TryParse(args[1], out mapsize))
                        if (int.TryParse(args[2], out back))
                        {
                            MapWipe(mapsize, seed, back == 1 ? true : false, false);
                        }
                return;
            }
            return;
        }

        #endregion GameCommands
        #region Functionality
        #endregion Functionality
        #region Helper Methods
        #endregion Helper Methods
        #region API
        private bool MapWipe(int worldsize,int seed,bool backupparam,bool cancel)
        {
            bool backup = backupparam;
            string configPath = Path.Combine(identityfolder, "cfg\\", "server.cfg").Replace(oldslash, newslash);
            int ws = cancel ? oldmapsize : worldsize;
            int se = cancel ? oldmapseed : seed;
            try
            {
                if (_configData.backup)
                File.Copy(configPath, $"{configPath.Replace("server.cfg",$"serverbackup_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm")}.cfg")}");
                File.Copy(configPath, $"{configPath.Replace("server.cfg", $"serverbackup.cfg")}",true);
                backupconfig = configPath.Replace("server.cfg", $"serverbackup.cfg");
                string fileContents = cancel?File.ReadAllText(configPath.Replace("server.cfg", $"serverbackup.cfg")):File.ReadAllText(configPath);
                lines = new List<string>(fileContents.Split(new[] { Environment.NewLine }, StringSplitOptions.None));
                bool world = false;
                    bool sd = false;
                // Process each line as needed
                if (!cancel)
                {
                    for(int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].StartsWith("server.worldsize") || lines[i].StartsWith("//server.worldsize"))
                        {
                            lines[i] = $"server.worldsize {ws}";
                            world = true;
                        }
                        if (lines[i].StartsWith("server.seed") || lines[i].StartsWith("//server.seed"))
                        {
                            lines[i] = $"server.seed {se}";
                            sd = true;
                        }
                    }
                    if (!world)
                    {
                        lines.Add($"server.worldsize {ws}");
                    }
                    if (!sd)
                    {
                        lines.Add($"server.seed {se}");
                    }
                }
                else
                {
                    if (backupconfig != "")
                        File.Delete(configPath.Replace("server.cfg", $"serverbackup.cfg"));
                }
                fileContents = string.Join(Environment.NewLine, lines);
                File.WriteAllText(configPath, fileContents);
                wipestate = cancel ? 0 : 1;
                Log(_configData.LogFileName, cancel ? "Canceled" : "Success", ws,se);

                return true;
                
            }
            catch (Exception ex)
            {
                wipestate = 0;
                Log(_configData.LogFileName, "Error", ex.Message);
                return false;
            }
        }
        #endregion
        #region Localization

        private void Log(string filename, string key, params object[] args)
        {
            if (_configData.LogToConsole)
            {
                Puts($"[{DateTime.Now}] {Lang(key, args)}");
            }

            LogToFile(filename, $"[{DateTime.Now}] {Lang(key, args)}", this);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Error"] = "Something went wrong.. {0}",
                ["Success"] = "Successfully changed to world size {0} map seed {1}.",
                ["Cancel"] = "Successfully canceled map wipe to world size {0} map seed {1}."
            }, this, "en");
        }

        private string Lang(string key, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this), args);
        }

        #endregion Localization
    }
}