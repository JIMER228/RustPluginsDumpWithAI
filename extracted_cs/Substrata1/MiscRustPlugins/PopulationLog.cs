using System;
using Oxide.Core;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Population Log", "Substrata", "1.0.0")]
    [Description("Periodically log server population")]

    class PopulationLog : RustPlugin
    {
        void OnServerInitialized(bool initial)
        {
            LogPop();
            timer.Every(configData.logTime * 60, () => LogPop());
        }

        void LogPop()
        {
            string dateTime = configData.dateFormat != null ? DateTime.Now.ToString(configData.dateFormat) : DateTime.Now.ToString("MM/dd - HH:mm:ss");
            string message = string.Format(configData.logMessage, dateTime, BasePlayer.activePlayerList.Count, ConVar.Server.maxplayers);

            if (configData.logToFile)
                LogToFile("log", message, this);

            if (configData.logToConsole)
                Puts(message);
        }

        #region Config
        private ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Log Time (minutes)")]
            public int logTime = 30;
            [JsonProperty(PropertyName = "Log Message")]
            public string logMessage = "[{0}] {1}/{2} players online";
            [JsonProperty(PropertyName = "Date Format")]
            public string dateFormat = "MM/dd - HH:mm:ss";
            [JsonProperty(PropertyName = "Log To File")]
            public bool logToFile = true;
            [JsonProperty(PropertyName = "Log To Console")]
            public bool logToConsole = false;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                configData = Config.ReadObject<ConfigData>();
                if (configData == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => configData = new ConfigData();
        protected override void SaveConfig() => Config.WriteObject(configData);
        #endregion
    }
}