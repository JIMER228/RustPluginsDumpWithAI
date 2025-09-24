// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Oxide.Plugins;
using static Oxide.Plugins.ModuleCarLicensePlate;

namespace ModuleCarLicensePlateUnitTest
{
    public class IsModuleCarChassisUnitTests
    {
        private const string TwoModuleCarChassis = "assets/content/vehicles/modularcar/car_chassis_2module.entity.prefab";
        private const string ThreeModuleCarChassis = "assets/content/vehicles/modularcar/car_chassis_3module.entity.prefab";
        private const string FourModuleCarChassis = "assets/content/vehicles/modularcar/car_chassis_4module.entity.prefab";

        [Fact]
        public void IsModuleCarChassis_WhenPrefabIsModuleCarChassis_ReturnsTrue()
        {
            Assert.True(ModuleCarLicensePlate.IsModuleCarChassisWrapper(TwoModuleCarChassis));
            Assert.True(ModuleCarLicensePlate.IsModuleCarChassisWrapper(ThreeModuleCarChassis));
            Assert.True(ModuleCarLicensePlate.IsModuleCarChassisWrapper(FourModuleCarChassis));
        }
    }
};

