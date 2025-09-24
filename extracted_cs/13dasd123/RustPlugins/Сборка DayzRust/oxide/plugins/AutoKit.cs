using Newtonsoft.Json;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide;

namespace Oxide.Plugins
{
    [Info("AutoKit", "setfps", "1.0.0")]
    [Description("autokit при спавне дает предмет указанный в конфиге на 10 слотов")]
    class AutoKit : RustPlugin
    {
        [PluginReference] private Plugin AutorSetfps;
        private PluginConfig config;
        class PluginConfig
        {
            [JsonProperty(PropertyName = "Основные настройки")]
            public MainSettings mainSet { get; set; }

            public class MainSettings
            {
                [JsonProperty("id предмета номер 1")]
                public int ggg = 790921853;

                [JsonProperty("Кол-во данного предмета 1")]
                public int ddd = 1;

                [JsonProperty("id предмета номер 2")]
                public int aaa = -578028723;

                [JsonProperty("Кол-во данного предмета 2")]
                public int bbb = 2;

                [JsonProperty("id предмета номер 3")]
                public int vvv = 698310895;

                [JsonProperty("Кол-во данного предмета 3")]
                public int qqq = 4;

                [JsonProperty("id предмета номер 4")]
                public int ooo = 776005741;

                [JsonProperty("Кол-во данного предмета 4")]
                public int iii = 1;

                [JsonProperty("id предмета номер 5")]
                public int eee = -1440143841;

                [JsonProperty("Кол-во данного предмета 5")]
                public int ppp = 1;

                [JsonProperty("id предмета номер 6")]
                public int uuu = -1976561211;

                [JsonProperty("Кол-во данного предмета 6")]
                public int yyy = 1;

                [JsonProperty("id предмета номер 7")]
                public int eqq = 3655341;

                [JsonProperty("Кол-во данного предмета 7")]
                public int qee = 1000;

                [JsonProperty("id предмета номер 8")]
                public int eqr = -1289478934;

                [JsonProperty("Кол-во данного предмета 8")]
                public int rqe = 1;

                [JsonProperty("id предмета номер 9")]
                public int vhougana = 789892804;

                [JsonProperty("Кол-во данного предмета 9")]
                public int iz = 1;

                [JsonProperty("id предмета номер 10")]
                public int bbd = -1976561211;

                [JsonProperty("Кол-во данного предмета 10")]
                public int ddb = 1;
            }
        }
        protected override void LoadDefaultConfig()
        {
            var configData = new PluginConfig { mainSet = new PluginConfig.MainSettings() };
            Config.WriteObject(configData, true);
        }

        private void LoadVariables()
        {
            bool changed = false;
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            config = Config.ReadObject<PluginConfig>();
            if (config.mainSet == null) { config.mainSet = new PluginConfig.MainSettings(); changed = true; }
            Config.WriteObject(config, true);
            if (changed) PrintWarning("Конфиг обновлен.");
        }
        private void OnServerInitialized()
        {
            LoadVariables();
            if (!AutorSetfps)
            {

                PrintWarning("Autor setfps - vk.com/setfps");
                return;
            }
        }
        [HookMethod("Init")]
        private void Init()
        {
            {
                config = Config.ReadObject<PluginConfig>();
                Config.WriteObject(config);
            }
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            ItemManager.CreateByItemID(config.mainSet.ggg, config.mainSet.ddd);
            ItemManager.CreateByItemID(config.mainSet.aaa, config.mainSet.bbb);
            ///////////////////////////////////////////////////////////////////
            ItemManager.CreateByItemID(config.mainSet.vvv, config.mainSet.qqq);
            ItemManager.CreateByItemID(config.mainSet.eee, config.mainSet.ppp);
            ItemManager.CreateByItemID(config.mainSet.ooo, config.mainSet.iii);
            ItemManager.CreateByItemID(config.mainSet.uuu, config.mainSet.yyy);
            ///////////////////////////////////////////////////////////////////
            ItemManager.CreateByItemID(config.mainSet.eqq, config.mainSet.qee);
            ItemManager.CreateByItemID(config.mainSet.eqr, config.mainSet.rqe);
            ItemManager.CreateByItemID(config.mainSet.vhougana, config.mainSet.iz);
            ItemManager.CreateByItemID(config.mainSet.bbd, config.mainSet.ddb);
        }
    }
}