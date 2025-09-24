using System.Collections;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SleepingSettings", "Kira", "1.0.0")]
    [Description("Sleeping settings for WarLand")]
    public class SleepingSettings : RustPlugin
    {
        #region [Vars]

        private WaitForSeconds wait = new WaitForSeconds(0.1f);

        #endregion

        #region [Configuraton] / [Конфигурация]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "SleepingSettings - Config")]
            public SleepingSettingsCFG SleepingSettings = new SleepingSettingsCFG();

            public class SleepingSettingsCFG
            {
                [JsonProperty(PropertyName = "Здоровья спальника")]
                public float baghealth = 50f;
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                SleepingSettings = new ConfigData.SleepingSettingsCFG
                {
                    baghealth = 50f
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Файл конфигурации поврежден (или не существует), создан новый!");
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            ServerMgr.Instance.StartCoroutine(ProcessBags());
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntityBuilt(Planner plan, GameObject go)
        { 
            if (plan == null || go == null) return;
            if (go.ToBaseEntity() == null) return; 
            var ent = go.ToBaseEntity();
            if (ent is global::SleepingBag)
                ent.gameObject.GetComponent<global::SleepingBag>().SetHealth(_config.SleepingSettings.baghealth);
        }

        private IEnumerator ProcessBags()
        {
            foreach (var ent in UnityEngine.Object.FindObjectsOfType<global::SleepingBag>())
            {
                var bag = ent.gameObject.GetComponent<global::SleepingBag>();
                if (bag.health > _config.SleepingSettings.baghealth)
                    bag.SetHealth(_config.SleepingSettings.baghealth);
                yield return wait;
            }

            yield return 0;
        }
    }
}