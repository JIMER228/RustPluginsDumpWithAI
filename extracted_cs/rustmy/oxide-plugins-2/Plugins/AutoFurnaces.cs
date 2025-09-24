namespace Oxide.Plugins
{
    [Info("AutoFurnaces", "Wulf/lukespragg", "1.1.3", ResourceId = 1140)]
    [Description("Automatically starts all furnaces after a server restart")]

    class AutoFurnaces : RustPlugin
    {
        void OnServerInitialized()
        {
            var furnaceCount = 0;
            var furnaceEmptyCount = 0;
            var furnaces = UnityEngine.Object.FindObjectsOfType<BaseOven>();
            if (furnaces == null) return;

            foreach (var furnace in furnaces)
            {
                var hasCookable = false;

                foreach (var item in furnace.inventory.itemList)
                    if (item.info.shortname.Equals("bearmeat") || item.info.shortname.Contains("ore") || item.info.shortname.Contains("raw"))
                        hasCookable = true;

                if (furnace.temperature == BaseOven.TemperatureType.Smelting && hasCookable)
                {
                    furnace.inventory.temperature = 1000f;
                    furnace.CancelInvoke("Cook");
                    furnace.InvokeRepeating("Cook", 0.5f, 0.5f);
                    furnace.SetFlag(BaseEntity.Flags.On, true);
                    furnaceCount++;
                }

                if (hasCookable == false) furnaceEmptyCount++;
            }

            Puts($"{furnaceCount} furnaces were automatically turned on");
            Puts($"{furnaceEmptyCount} furnaces were ignored as they had nothing cookable in them");
        }
    }
}
