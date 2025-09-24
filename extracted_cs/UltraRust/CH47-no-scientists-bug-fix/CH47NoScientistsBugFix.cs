using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CH47NoScientistsBugFix", "Ultra", "2.1.2")]
    [Description("CH47 respawns if it has spawned out of livable map")]

    class CH47NoScientistsBugFix : RustPlugin
    {
        bool initialized = false;
        int mapLimit = 0;
        Timer currentPositionLogTimer = null;

        #region Hooks

        void OnServerInitialized()
        {
            mapLimit = (ConVar.Server.worldsize / 2) * 3;
            if (mapLimit > 4000) mapLimit = 4000;
            initialized = true;
            Log($"OnServerInitialized(): MapSize: {ConVar.Server.worldsize} / MapLimit set to {mapLimit}", logType: LogType.INFO);
        }

        void OnEntitySpawned(BaseEntity Entity)
        {
            if (!initialized) return;
            if (Entity == null) return;

            if (Entity is CH47Helicopter)
            {
                CH47Helicopter ch47 = (CH47Helicopter)Entity;

                if (!IsInLivableArea(ch47.transform.position))
                {
                    Log($"CH47 spawned out liveable area", logType: LogType.WARNING);
                    Log($"{ch47.transform.position.x}|{ch47.transform.position.y}|{ch47.transform.position.z}", logType: LogType.WARNING);
                    timer.Once(1f, () => { ch47.Kill(); });
                    Vector3 newPostition = GetFixedPosition(ch47.transform.position);
                    timer.Once(2f, () => { SpawnCH47Helicopter(newPostition); });
                }
                else
                {
                    Log($"CH47 spawned in liveable area properly", logType: LogType.INFO);
                    Log($"{ch47.transform.position.x}|{ch47.transform.position.y}|{ch47.transform.position.z}", logType: LogType.INFO);
                }
            }
        }

        void Unload()
        {
            if (currentPositionLogTimer != null)
            {
                currentPositionLogTimer.Destroy();
            }
        }

        #endregion

        #region Core

        bool IsInLivableArea(Vector3 originalPosition)
        {
            if (originalPosition.x < -(mapLimit)) return false;
            if (originalPosition.x > mapLimit) return false;
            if (originalPosition.z < -(mapLimit)) return false;
            if (originalPosition.z > mapLimit) return false;
            return true;
        }

        Vector3 GetFixedPosition(Vector3 originalPosition)
        {
            Vector3 newPosition = originalPosition;
            if (originalPosition.x < -(mapLimit)) newPosition.x = -(mapLimit) + 100;
            if (originalPosition.x > mapLimit) newPosition.x = mapLimit - 100;
            if (originalPosition.z < -(mapLimit)) newPosition.z = -(mapLimit) + 100;
            if (originalPosition.z > mapLimit) newPosition.z = mapLimit - 100;
            return newPosition;
        }

        void SpawnCH47Helicopter(Vector3 position)
        {
            Unsubscribe(nameof(OnEntitySpawned));
            var ch47 = (CH47HelicopterAIController)GameManager.server.CreateEntity("assets/prefabs/npc/ch47/ch47scientists.entity.prefab", position);
            if (ch47 == null) return;
            ch47.Spawn();
            currentPositionLogTimer = timer.Repeat(15, 5, () => LogCurrentPosition(ch47));
            Subscribe(nameof(OnEntitySpawned));
            Log($"Standby CH47 spawned: {ch47.transform.position.x}|{ch47.transform.position.y}|{ch47.transform.position.z}", logType: LogType.INFO);            
        }

        void LogCurrentPosition(CH47Helicopter ch47)
        {
            if (ch47 == null || ch47.IsDestroyed) return;
            Log($"Current position: {ch47.transform.position.x}|{ch47.transform.position.y}|{ch47.transform.position.z} ", logType: LogType.INFO);
        }

        #endregion

        #region Config

        private ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "LogInFile")]
            public bool LogInFile;

            [JsonProperty(PropertyName = "LogInConsole")]
            public bool LogInConsole;
        }

        protected override void LoadConfig()
        {
            try
            {
                base.LoadConfig();
                configData = Config.ReadObject<ConfigData>();
                if (configData == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            configData = new ConfigData()
            {
                LogInFile = true,
                LogInConsole = true
            };
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(configData, true);
            base.SaveConfig();
        }

        #endregion

        #region Log

        void Log(string message, bool console = false, LogType logType = LogType.INFO, string fileName = "")
        {
            if (configData.LogInFile)
            {
                LogToFile(fileName, $"[{DateTime.Now.ToString("hh:mm:ss")}] {logType} > {message}", this);
            }

            if (configData.LogInConsole)
            {
                Puts($"{message.Replace("\n", " ")}");
            }
        }

        enum LogType
        {
            INFO = 0,
            WARNING = 1,
            ERROR = 2
        }

        #endregion
    }
}
