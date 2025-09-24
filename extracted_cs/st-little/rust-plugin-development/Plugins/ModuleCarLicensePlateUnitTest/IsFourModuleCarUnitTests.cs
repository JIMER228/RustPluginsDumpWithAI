// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Oxide.Plugins;
using static Oxide.Plugins.ModuleCarLicensePlate;

namespace ModuleCarLicensePlateUnitTest
{
    public class IsFourModuleCarUnitTests
    {
        private const string SmallWoodSignPrefab = "assets/prefabs/deployable/signs/sign.small.wood.prefab";
        private const string TwoModuleCarSpawnedPrefab = "assets/content/vehicles/modularcar/2module_car_spawned.entity.prefab";
        private const string ThreeModuleCarSpawnedPrefab = "assets/content/vehicles/modularcar/3module_car_spawned.entity.prefab";
        private const string FourModuleCarSpawnedPrefab = "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab";

        private const string TwoModuleCar01Prefab = "assets/content/vehicles/modularcar/admin_prefabs/2_modules/car_2mod_01.prefab";
        private const string TwoModuleCar02Prefab = "assets/content/vehicles/modularcar/admin_prefabs/2_modules/car_2mod_02.prefab";
        private const string TwoModuleCar03Prefab = "assets/content/vehicles/modularcar/admin_prefabs/2_modules/car_2mod_03.prefab";
        private const string TwoModuleCar04Prefab = "assets/content/vehicles/modularcar/admin_prefabs/2_modules/car_2mod_04.prefab";
        private const string TwoModuleCar05Prefab = "assets/content/vehicles/modularcar/admin_prefabs/2_modules/car_2mod_05.prefab";
        private const string TwoModuleCar06Prefab = "assets/content/vehicles/modularcar/admin_prefabs/2_modules/car_2mod_06.prefab";
        private const string TwoModuleCar07Prefab = "assets/content/vehicles/modularcar/admin_prefabs/2_modules/car_2mod_07.prefab";
        private const string TwoModuleCar08Prefab = "assets/content/vehicles/modularcar/admin_prefabs/2_modules/car_2mod_08.prefab";
        private const string ThreeModuleCar01Prefab = "assets/content/vehicles/modularcar/admin_prefabs/3_modules/car_3mod_01.prefab";
        private const string ThreeModuleCar02Prefab = "assets/content/vehicles/modularcar/admin_prefabs/3_modules/car_3mod_02.prefab";
        private const string ThreeModuleCar03Prefab = "assets/content/vehicles/modularcar/admin_prefabs/3_modules/car_3mod_03.prefab";
        private const string ThreeModuleCar04Prefab = "assets/content/vehicles/modularcar/admin_prefabs/3_modules/car_3mod_04.prefab";
        private const string ThreeModuleCar05Prefab = "assets/content/vehicles/modularcar/admin_prefabs/3_modules/car_3mod_05.prefab";
        private const string ThreeModuleCar06Prefab = "assets/content/vehicles/modularcar/admin_prefabs/3_modules/car_3mod_06.prefab";
        private const string ThreeModuleCar07Prefab = "assets/content/vehicles/modularcar/admin_prefabs/3_modules/car_3mod_07.prefab";
        private const string ThreeModuleCar08Prefab = "assets/content/vehicles/modularcar/admin_prefabs/3_modules/car_3mod_08.prefab";
        private const string ThreeModuleCar09Prefab = "assets/content/vehicles/modularcar/admin_prefabs/3_modules/car_3mod_09.prefab";
        private const string ThreeModuleCar10Prefab = "assets/content/vehicles/modularcar/admin_prefabs/3_modules/car_3mod_10.prefab";
        private const string ThreeModuleCar11Prefab = "assets/content/vehicles/modularcar/admin_prefabs/3_modules/car_3mod_11.prefab";
        private const string ThreeModuleCar12Prefab = "assets/content/vehicles/modularcar/admin_prefabs/3_modules/car_3mod_12.prefab";
        private const string FourModuleCar01Prefab = "assets/content/vehicles/modularcar/admin_prefabs/4_modules/car_4mod_01.prefab";
        private const string FourModuleCar02Prefab = "assets/content/vehicles/modularcar/admin_prefabs/4_modules/car_4mod_02.prefab";
        private const string FourModuleCar03Prefab = "assets/content/vehicles/modularcar/admin_prefabs/4_modules/car_4mod_03.prefab";
        private const string FourModuleCar04Prefab = "assets/content/vehicles/modularcar/admin_prefabs/4_modules/car_4mod_04.prefab";
        private const string FourModuleCar05Prefab = "assets/content/vehicles/modularcar/admin_prefabs/4_modules/car_4mod_05.prefab";
        private const string FourModuleCar06Prefab = "assets/content/vehicles/modularcar/admin_prefabs/4_modules/car_4mod_06.prefab";
        private const string FourModuleCar07Prefab = "assets/content/vehicles/modularcar/admin_prefabs/4_modules/car_4mod_07.prefab";
        private const string FourModuleCar08Prefab = "assets/content/vehicles/modularcar/admin_prefabs/4_modules/car_4mod_08.prefab";
        private const string FourModuleCar09Prefab = "assets/content/vehicles/modularcar/admin_prefabs/4_modules/car_4mod_09.prefab";
        private const string FourModuleCar10Prefab = "assets/content/vehicles/modularcar/admin_prefabs/4_modules/car_4mod_10.prefab";
        private const string FourModuleCar11Prefab = "assets/content/vehicles/modularcar/admin_prefabs/4_modules/car_4mod_11.prefab";

        [Fact]
        public void IsFourModuleCar_WhenPrefabIsFourModuleCar_ReturnsTrue()
        {
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(TwoModuleCarSpawnedPrefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(TwoModuleCar01Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(TwoModuleCar02Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(TwoModuleCar03Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(TwoModuleCar04Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(TwoModuleCar05Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(TwoModuleCar06Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(TwoModuleCar07Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(TwoModuleCar08Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(ThreeModuleCarSpawnedPrefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(ThreeModuleCar01Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(ThreeModuleCar02Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(ThreeModuleCar03Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(ThreeModuleCar04Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(ThreeModuleCar05Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(ThreeModuleCar06Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(ThreeModuleCar07Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(ThreeModuleCar08Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(ThreeModuleCar09Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(ThreeModuleCar10Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(ThreeModuleCar11Prefab));
            Assert.Equal(false, ModuleCarLicensePlate.IsFourModuleCarWrapper(ThreeModuleCar12Prefab));
            Assert.Equal(true, ModuleCarLicensePlate.IsFourModuleCarWrapper(FourModuleCarSpawnedPrefab));
            Assert.Equal(true, ModuleCarLicensePlate.IsFourModuleCarWrapper(FourModuleCar01Prefab));
            Assert.Equal(true, ModuleCarLicensePlate.IsFourModuleCarWrapper(FourModuleCar02Prefab));
            Assert.Equal(true, ModuleCarLicensePlate.IsFourModuleCarWrapper(FourModuleCar03Prefab));
            Assert.Equal(true, ModuleCarLicensePlate.IsFourModuleCarWrapper(FourModuleCar04Prefab));
            Assert.Equal(true, ModuleCarLicensePlate.IsFourModuleCarWrapper(FourModuleCar05Prefab));
            Assert.Equal(true, ModuleCarLicensePlate.IsFourModuleCarWrapper(FourModuleCar06Prefab));
            Assert.Equal(true, ModuleCarLicensePlate.IsFourModuleCarWrapper(FourModuleCar07Prefab));
            Assert.Equal(true, ModuleCarLicensePlate.IsFourModuleCarWrapper(FourModuleCar08Prefab));
            Assert.Equal(true, ModuleCarLicensePlate.IsFourModuleCarWrapper(FourModuleCar09Prefab));
            Assert.Equal(true, ModuleCarLicensePlate.IsFourModuleCarWrapper(FourModuleCar10Prefab));
            Assert.Equal(true, ModuleCarLicensePlate.IsFourModuleCarWrapper(FourModuleCar11Prefab));
        }
    }
};

