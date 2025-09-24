using Newtonsoft.Json;
using Rust;

namespace Oxide.Plugins
{
    [Info("DamageSettings", "Kira", "1.0.0")]
    [Description("DamageSettings for WarLand")]
    public class DamageSettings : RustPlugin
    {
        #region [Configuraton] / [Конфигурация]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "DamageSettings - Config")]
            public DamageSettingsCFG DamageSettings = new DamageSettingsCFG();

            public class DamageSettingsCFG
            {
                [JsonProperty(PropertyName = "Снижение урона на 40mm_grenade_he (0.0-1.0)")]
                public float procent = 1f;
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                DamageSettings = new ConfigData.DamageSettingsCFG
                {
                    procent = 1f
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
        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return null;
            switch (info.damageTypes.GetMajorityDamageType())
            {
                case DamageType.Blunt:
                    var item = info.WeaponPrefab.ShortPrefabName;
                    if (item == "40mm_grenade_he") info.damageTypes.ScaleAll(_config.DamageSettings.procent);
                    break;
            }

            return null;
        }
    }
}