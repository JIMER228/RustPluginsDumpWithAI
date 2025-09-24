// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("Backup", "https://discord.gg/dNGbxafuJn", "0.0.1")]
    public class Backup : RustPlugin
    {
        #region Configuration

        private class Configuration
        {
            [JsonProperty("Список объектов для сохранения")]
            public List<string> BackupPath = new List<string>();
            [JsonProperty("Интервал сохранения объектов")]
            public int SaveInterval = 600;

            public static Configuration GetNewConf()
            {
                return new Configuration
                { 
                    BackupPath = new List<string>
                    {
                        "VKBot",
                        "VKBotReports",
                        "VKBotUsers",
                        "ChatSystem/Players",
                        "Teleportation/Homes" 
                    }
                };
            }
        }
 
        #endregion

        #region Variables

        private Configuration Settings;
        private Coroutine CurrentQueue = null;

        #endregion
         
        #region Init
        
        protected override void LoadConfig()
        { 
            base.LoadConfig();
            try
            {
                Settings = Config.ReadObject<Configuration>();
            }
            catch
            {
                PrintWarning($"Error reading config, creating one new config!");
                LoadDefaultConfig();
            }
            
            NextTick(SaveConfig);
        }
        protected override void LoadDefaultConfig() => Settings = Configuration.GetNewConf();
        protected override void SaveConfig() => Config.WriteObject(Settings);
 
        private void OnServerInitialized() => CurrentQueue = ServerMgr.Instance.StartCoroutine(FetchFiles());
        private void Unload() => ServerMgr.Instance.StopCoroutine(CurrentQueue);
        
        #endregion

        #region Functions

        private IEnumerator FetchFiles()
        {
            while (true)
            {
                yield return new WaitForSeconds(Settings.SaveInterval);

                var folderName = $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}";
                foreach (var check in Settings.BackupPath)
                {
                    if (!Interface.Oxide.DataFileSystem.ExistsDatafile(check))
                    {
                        PrintError($"Failed to backup file: {check} | File not found!");
                        continue;
                    } 
                    var obj = Interface.Oxide.DataFileSystem.ReadObject<object>(check);

                    var newName = $"Backup/{folderName.Replace("/", ".").Replace(":", "-")}/{check}"; 
                    Interface.Oxide.DataFileSystem.WriteObject(newName, obj); 
                    yield return new WaitForSeconds(0.05f);  
                }
                PrintWarning($"Backup completed! Saved to: Backup/{folderName.Replace("/", ".").Replace(":", "-")}");
            }
        }

        #endregion

        #region Utils
        
        private static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;

        #endregion
    }
}