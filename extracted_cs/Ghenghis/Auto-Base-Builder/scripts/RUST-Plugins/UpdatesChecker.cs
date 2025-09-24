// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Linq;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core.Libraries;

//  V1.1.2
//  Added catch for errors where version number contains letters.
//  User is notified of plugin name and version being skipped.

namespace Oxide.Plugins
{
    [Info("UpdatesChecker", "Steenamaroo", "1.1.2", ResourceId = 36)]

    class UpdatesChecker : RustPlugin
    {
        List<Root> a;
        List<Plugin> PlugList = new List<Plugin>();

        public class Root
        {
            public int id { get; set; }
            public string title { get; set; }
            public string version { get; set; }
            public string fileName { get; set; }
        }


        void OnServerInitialized()
        {
            configData.CheckIntervalMinutes = Mathf.Max(10, configData.CheckIntervalMinutes); 
            GetFiles();
            timer.Repeat(configData.CheckIntervalMinutes * 60, 0, () => GetFiles());
        }

        void GetFiles()
        {
            PlugList = plugins.GetAll().Where(x => !x.IsCorePlugin).OrderBy(x => x.Name).ToList();
            webrequest.Enqueue("https://www.codefling.com/db?category=2", null, GetPlugin, this);
        }

        List<string> errors = new List<string>();

        class test
        {
            public string result;
        }

        private void GetPlugin(int code, string response) 
        {
            if (response != null && code == 200)
            {
                a = JsonConvert.DeserializeObject<List<Root>>(response, new JsonSerializerSettings { Error = (se, ev) => ev.ErrorContext.Handled = true });

                if (a == null)
                {
                    PrintWarning("Unable to retreive data from Codefling.com");
                    return;
                }

                foreach (var entry in a.ToList())
                {
                    if (entry.fileName == null)
                        continue;
                    entry.fileName = entry.fileName.Replace(".cs", "");
                }

                List<string> Updates = new List<string>();

                foreach (var loaded in PlugList.Where(x => !configData.Ignore.Contains(x.Name))) 
                {
                    if (!configData.Authors.ContainsKey(loaded.Author)) 
                        configData.Authors.Add(loaded.Author, true);

                    foreach (var entry in a.Where(entry => entry.fileName == loaded.Name))
                        if (S2V(entry.fileName, entry.version) > loaded.Version && configData.Authors[loaded.Author])
                                Updates.Add(entry.fileName);
                }

                if (Updates.Count == 1)
                {
                    PrintWarning($"Codefling has an update available for {Updates[0]}.");
                    SendDiscordMessage($"{Updates[0]}", true);
                }
                else if (Updates.Count != 0)
                {
                    float delay = 0.1f;
                    PrintWarning("Codefling has updates for the following plugins.");
                    string discordmsg = "";
                    for (int i = 0; i < Updates.Count; i++)
                    {
                        if (discordmsg.Length > 220)
                        {
                            SendDiscordMessage(discordmsg, false, delay, delay == 0.1f);
                            discordmsg = "";
                            delay += 1f; 
                        }
                        PrintWarning($"{i + 1} : {Updates[i]}"); 
                        discordmsg += $"\n{i + 1} : {Updates[i]}";
                    }
                    SendDiscordMessage(discordmsg, false, delay, delay == 0.1f);
                }
                SaveConf();  
            }
            else
                PrintWarning("Unable to contact Codefling.com"); 
        }

        private void OnPluginLoaded(Plugin plugin)
        {
            if (a == null)
                return;
            if (configData.Ignore.Contains(plugin.Name))
                return;
            if (!configData.Authors.ContainsKey(plugin.Author))
                configData.Authors.Add(plugin.Author, true);
            foreach (var entry in a.Where(entry => entry.fileName == plugin.Name))
                if (S2V(entry.fileName, entry.version) > plugin.Version && configData.Authors[plugin.Author])
                    PrintWarning($"Codefling has an update available for {entry.fileName}.");
        }

        Core.VersionNumber S2V(string fileName, string v)
        {
            string[] parts = v.Split('.');

            if (parts.Length != 3)
                return new Core.VersionNumber(0, 0, 0);
            
            try
            {
                return new Core.VersionNumber(Convert.ToInt16(parts[0]), Convert.ToInt16(parts[1]), Convert.ToInt16(parts[2]));
            }
            catch
            {
                PrintWarning($"Skipping plugin {fileName} : Non standard version number {v}.");
                return new Core.VersionNumber(0, 0, 0);
            }
        }

        void Init() => LoadConfigVariables();

        private ConfigData configData;

        class ConfigData
        {
            public int CheckIntervalMinutes = 60;
            public string DiscordWebhookAddress = "";
            public List<string> DiscordWebhookAddresses = new List<string>();
            public List<string> Ignore = new List<string>();
            public Dictionary<string, bool> Authors = new Dictionary<string, bool>();
        }

        public string WarningNotice = "No longer used - Use DiscordWebhookAddresses (below) instead.";
        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();
             
            if (configData.DiscordWebhookAddress != string.Empty && configData.DiscordWebhookAddress != WarningNotice && (configData.DiscordWebhookAddresses.Count == 0 || !configData.DiscordWebhookAddresses.Contains(configData.DiscordWebhookAddress)))
            {
                configData.DiscordWebhookAddresses.Add(configData.DiscordWebhookAddress);
                configData.DiscordWebhookAddress = "No longer used - Use DiscordWebhookAddresses (below) instead.";
            }
            SaveConf();  
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            configData = new ConfigData(); 
            SaveConf();   
        }

        void SaveConf()
        {
            configData.Authors = configData.Authors.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
            Config.WriteObject(configData, true);
        }

        public void SendDiscordMessage(string message, bool single, float delay = 0.1f, bool first = true) 
        {
            try
            {
                timer.Once(delay, () =>
                {
                    var embeds = new List<Embed>();
                    embeds.Add(new Embed() { title = message, description = "https://www.codefling.com" });
                    Dictionary<string, object> stuff = new Dictionary<string, object>()
                    {
                        { "embeds", embeds },
                        { "avatar_url", "https://pbs.twimg.com/profile_images/1345024685541621761/sc75KlbU_400x400.jpg"}, 
                        { "content", single ? "Codefling has an update for" : first ? "Codefling has updates for" : ""},
                        { "username", "Codefling Updates Checker"}
                    };
                    foreach (var entry in configData.DiscordWebhookAddresses)
                        if (entry.Contains("discord.com/api/webhooks")) 
                            webrequest.Enqueue(entry, JsonConvert.SerializeObject(stuff), Callback, this, RequestMethod.POST, new Dictionary<string, string> { ["Content-Type"] = "application/json" });
                });
            }
            catch { }
        }

        public class Embed
        {
            public string title;
            public string description;
            public string url;
        }

        public void Callback(int code, string response)  
        {
        }

        public class FileParameter 
        {
            public byte[] File { get; set; }
            public string FileName { get; set; }
            public string ContentType { get; set; }
            public FileParameter(byte[] file) : this(file, null) { }
            public FileParameter(byte[] file, string filename) : this(file, filename, null) { } 
            public FileParameter(byte[] file, string filename, string contenttype)
            {
                File = file;
                FileName = filename;
                ContentType = contenttype;
            }
        }
    }
} 