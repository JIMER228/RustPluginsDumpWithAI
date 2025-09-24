using System;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("VendingControl", "Kira", "1.0.1")]
    [Description("Настройка кд для VendingMachine")]
    public class VendingControl : RustPlugin
    {
        #region [Vars] / [Переменные]

        private const string PERMISSION_USE = "vendingcontrol.use";

        #endregion

        #region [Configuraton] / [Конфигурация]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "VendingControl - Config")]
            public VendingCFG VendingConfig = new VendingCFG();

            public class VendingCFG
            {
                [JsonProperty(PropertyName = "Время восстановления ресурсов")]
                public float time;
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                VendingConfig = new ConfigData.VendingCFG
                {
                    time = 1f
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

        #region [Hooks] / [Хуки]

        // ReSharper disable once UnusedMember.Local
        private void OnServerIntialized()
        {
            permission.RegisterPermission(PERMISSION_USE, this);
        }

        private void VendingRefill(Item soldItem, NPCVendingMachine vm)
        {
            if (vm == null || soldItem == null || soldItem.info == null)
                return;

            var item = ItemManager.Create(soldItem.info, soldItem.amount, soldItem.skin);
            if (soldItem.blueprintTarget != 0)
                item.blueprintTarget = soldItem.blueprintTarget;

            if (soldItem.instanceData != null)
                item.instanceData.dataInt = soldItem.instanceData.dataInt;

            NextTick(() =>
            {
                if (item == null)
                    return;

                if (vm == null || vm.IsDestroyed)
                {
                    item.Remove();
                    return;
                }

                vm.transactionActive = true;
                if (!item.MoveToContainer(vm.inventory))
                    item.Remove();

                vm.transactionActive = false;
                vm.FullUpdate();
            });
        }

        #endregion

        // ReSharper disable once UnusedMember.Local
        private void CanPurchaseItem(BasePlayer buyer, Item soldItem, Action<BasePlayer, Item> onItemPurchased,
            NPCVendingMachine vm)
        {
            if (!buyer.HasPlayerFlag(BasePlayer.PlayerFlags.SafeZone)) return;
            if (!permission.UserHasPermission(buyer.UserIDString, PERMISSION_USE)) return;
            timer.Once(_config.VendingConfig.time, () => VendingRefill(soldItem, vm));
        }
    }
}