using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using Facepunch.Extend;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("CarManager", "https://discord.gg/dNGbxafuJn", "1.0.1")]
    public class CarManager : RustPlugin
    {
        public static bool IsDebug = false;

        [PluginReference("EntityDrop")]
        Plugin EntityDrop;

        static string _GUI_Speedometer => $"{_plugin.Title}.{_plugin.Author}.{_plugin.Version}.Speedometer";
        static string _GUI_HealPanel => $"{_plugin.Title}.{_plugin.Author}.{_plugin.Version}.HealPanel";
        static CarManager _plugin = null;
        void Loaded()
        {
            _plugin = this;
            LoadConfig();
            TimeActive(ConfigData.SpawnTime, BaseSedan.CreateCar);
        }
        void OnServerInitialized()
        {
            foreach (var car in Resources.FindObjectsOfTypeAll<BaseCar>())
                if (car?.isSpawned == true)
                    car.Kill();
            LoadAll_BaseSedan();
        }
        static void SaveAll_BaseSedan()
        {
            List<BaseSedan.JsonSedan> list = new List<BaseSedan.JsonSedan>();
            foreach (var sedan in sedans)
            {
                try { BaseSedan.JsonSedan json = new BaseSedan.JsonSedan(sedan); list.Add(json); } catch {  }
            }
            Interface.GetMod().DataFileSystem.WriteObject("CarsData", list);
        }
        static List<BaseSedan> sedans = new List<BaseSedan>();
        static void LoadAll_BaseSedan()
        {
            if (!Interface.GetMod().DataFileSystem.ExistsDatafile("CarsData")) return;
            List<BaseSedan.JsonSedan> sedans = (Interface.GetMod().DataFileSystem.ReadObject<List<BaseSedan.JsonSedan>>("CarsData") ?? new List<BaseSedan.JsonSedan>());
            foreach (var car in sedans) BaseSedan.CreateCar(car);
        }
        void TimeActive(List<string> time, Action action)
        {
            string IsDouble = "";
            timer.Repeat(0.5f, 0, () =>
            {
                try
                {
                    string TimeNow = DateTime.Now.ToString("HH:mm:ss");
                    if (IsDouble == TimeNow) return;
                    IsDouble = TimeNow;
                    if (!time.Contains(TimeNow)) return;
                    action.Invoke();
                }
                catch (Exception e)
                {
                    PrintError($"Error in TimeActive:\n{e.Source}\n{e.StackTrace}\n{e.Message}");
                }
            });
        }
        public class ConfigData
        {
            public static List<string> SpawnPositions = new List<string>();
            public static float MaxHealthInCar { get { return _plugin.GetConfig("MaxHealthInCar", 600); } set { _plugin.SetConfig(true, "MaxHealthInCar", value); _plugin.SaveConfig(); } }
            public static float DefaultMaxDamage { get { return _plugin.GetConfig("DefaultMaxDamage", 10); } set { _plugin.SetConfig(true, "DefaultMaxDamage", value); _plugin.SaveConfig(); } }
            public static float DefaultMinDamage { get { return _plugin.GetConfig("DefaultMinDamage", 5); } set { _plugin.SetConfig(true, "DefaultMinDamage", value); _plugin.SaveConfig(); } }
            public static Dictionary<string, string> MaxMinDamageByType { get { return _plugin.GetConfig("MaxMinDamageByType (max, min)", new Dictionary<DamageType, float>() { [DamageType.Generic] = 20, [DamageType.Hunger] = 0, [DamageType.Thirst] = 0, [DamageType.Cold] = 0, [DamageType.Drowned] = 20, [DamageType.Heat] = 0, [DamageType.Bleeding] = 0, [DamageType.Poison] = 1, [DamageType.Suicide] = 1000, [DamageType.Bullet] = 20, [DamageType.Slash] = 10, [DamageType.Blunt] = 1, [DamageType.Fall] = 10, [DamageType.Radiation] = 0, [DamageType.Bite] = 2, [DamageType.Stab] = 0, [DamageType.Explosion] = 40, [DamageType.RadiationExposure] = 0, [DamageType.ColdExposure] = 0, [DamageType.Decay] = 1, [DamageType.ElectricShock] = 10, [DamageType.Arrow] = 5, [DamageType.LAST] = 0 }.ToDictionary(item => item.Key.ToString(), item => "0, 0").OrderBy(i => i.Key).ToDictionary(i => i.Key, i => i.Value)); } set { _plugin.SetConfig(true, "MaxMinDamageByType (max, min)", value); _plugin.SaveConfig(); } }
            public static Dictionary<string, string> MaxMinDamageByWeapon { get { Dictionary<string, string> dict = new Dictionary<string, string>(); foreach (var item in Resources.FindObjectsOfTypeAll<AttackEntity>()) if (!dict.ContainsKey(item.ShortPrefabName)) dict.Add(item.ShortPrefabName, "0, 0"); return _plugin.GetConfig("MaxMinDamageByWeapon (max, min)", dict.OrderBy(i => i.Key).ToDictionary(i => i.Key, i => i.Value)); } set { _plugin.SetConfig(true, "MaxMinDamageByWeapon (max, min)", value); _plugin.SaveConfig(); } }
            public static List<string> SpawnTime { get { return _plugin.GetConfig("SpawnTime (HH:MM:SS)", new List<string> { "16:00:00" }); } set { _plugin.SetConfig(true, "SpawnTime (HH:MM:SS)", value); _plugin.SaveConfig(); } }
            public static Dictionary<string, float> RepairCost { get { return _plugin.GetConfig("RepairCost (ItemNameOrID, Count)", new Dictionary<string, float> { ["metal_ore.item"] = 500f }); } set { _plugin.SetConfig(true, "RepairCost (ItemNameOrID, Count)", value); _plugin.SaveConfig(); } }

            public static bool CreateEffectBoom { get { return _plugin.GetConfig("CreateEffectBoom", true); } set { _plugin.SetConfig(true, "CreateEffectBoom", value); _plugin.SaveConfig(); } }
            public static bool DropFuel { get { return _plugin.GetConfig("DropFuel", true); } set { _plugin.SetConfig(true, "DropFuel", value); _plugin.SaveConfig(); } }
            public static bool DropTruck { get { return _plugin.GetConfig("DropTruck", true); } set { _plugin.SetConfig(true, "DropTruck", value); _plugin.SaveConfig(); } }


            //public static List<string> SpawnPositions { get { return _plugin.GetConfig("SpawnPositions", new List<string>()); } set { _plugin.SetConfig(true, "SpawnPositions", value); _plugin.SaveConfig(); } }
            //public static List<string> SpawnPositions { get { return _plugin.GetConfig("SpawnPositions", new List<string>()); } set { _plugin.SetConfig(true, "SpawnPositions", value); _plugin.SaveConfig(); } }
            public static void LoadAndSaveAll()
            {
                SpawnPositions = _plugin.GetConfig("SpawnPositions (position, rotation)", new List<string>());
                _plugin.SetConfig(true, "SpawnPositions (position, rotation)", SpawnPositions); _plugin.SaveConfig();
                MaxHealthInCar = MaxHealthInCar;
                DefaultMaxDamage = DefaultMaxDamage;
                DefaultMinDamage = DefaultMinDamage;
                MaxMinDamageByType = MaxMinDamageByType;
                MaxMinDamageByWeapon = MaxMinDamageByWeapon;
                SpawnTime = SpawnTime;
                RepairCost = RepairCost;
                CreateEffectBoom = CreateEffectBoom;
                DropFuel = DropFuel;
                DropTruck = DropTruck;
            }
        }
        new void LoadConfig()
        {
            ConfigData.LoadAndSaveAll();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            PrintError("No config file found, generating a new one.");
        }
        object OnItemPickup(Item item, BasePlayer player) => (item?.GetWorldEntity()?.GetParentEntity()?.GetComponent<BaseSedan>()?.UseItem(player, item) ?? false) ? (object)false : null;
        void OnLootEntityEnd(BasePlayer player, StorageContainer container) => StorageManager.Close(player, container);
        void OnEntityKill(BaseNetworkable entity) => entity?.GetComponent<BaseSedan>()?.KillChilds();
        public static ItemContainer CreateContainer(Dictionary<int, Item> items, int capacity)
        {
            ItemContainer container = new ItemContainer();
            container.ServerInitialize(null, capacity);
            foreach (var item in items)
                item.Value.MoveToContainer(container, item.Key);
            if (container.uid == 0) container.GiveUID();
            return container;
        }
        public static ItemContainer CreateContainer(int capacity, params Item[] items) => CreateContainer(items.Select((val, num) => new KeyValuePair<int, Item>(num, val)).Where(item => item.Value != null).ToDictionary(val => val.Key, val => val.Value), capacity);
        //new Action<BasePlayer>((player) => sedan.OpenStorage(player))
        void SetConfig(bool replace, string key, object value)
        {
            if (replace || Config.Get(key) == null) Config.Set(key, value);
        }
        void SetConfig(string key, object value) => SetConfig(false, key, value);
        T GetConfig<T>(string key, T defaultVal)
        {
            if (Config.Get(key) == null)
            {
                SetConfig(key, defaultVal);
                return defaultVal;
            }
            return (T)Convert.ChangeType(Config.Get<T>(key), typeof(T));
        }
     /* Dictionary<DamageType, float> damages = new Dictionary<DamageType, float>()
           {
               [DamageType.Generic] = 20,
               [DamageType.Hunger] = 0,
               [DamageType.Thirst] = 0,
               [DamageType.Cold] = 0,
               [DamageType.Drowned] = 20,
               [DamageType.Heat] = 0,
               [DamageType.Bleeding] = 0,
               [DamageType.Poison] = 1,
               [DamageType.Suicide] = 1000,
               [DamageType.Bullet] = 20,
               [DamageType.Slash] = 10,
               [DamageType.Blunt] = 1,
               [DamageType.Fall] = 10,
               [DamageType.Radiation] = 0,
               [DamageType.Bite] = 2,
               [DamageType.Stab] = 0,
               [DamageType.Explosion] = 40,
               [DamageType.RadiationExposure] = 0,
               [DamageType.ColdExposure] = 0,
               [DamageType.Decay] = 1,
               [DamageType.ElectricShock] = 10,
               [DamageType.Arrow] = 5,
               [DamageType.LAST] = 0
           }; */
        void SetHit(HitInfo info)
        {
            if (IsDebug) Debug.Log($"{info.damageTypes.Total()} : {info.damageTypes.GetMajorityDamageType().ToString()}");
            DamageType type = info.damageTypes.GetMajorityDamageType();
            if (IsDebug) Debug.Log(0);
            Dictionary<string, string> MaxMinDamageByWeapon = ConfigData.MaxMinDamageByWeapon;
            if (IsDebug) Debug.Log(1);
            if (info?.Weapon?.ShortPrefabName != null)
                if (MaxMinDamageByWeapon?.ContainsKey(info?.Weapon?.ShortPrefabName) ?? false)
                {
                    if (IsDebug) Debug.Log(2);
                    info.damageTypes.Set(type, RandomRange(MaxMinDamageByWeapon[info.Weapon.ShortPrefabName]));
                    if (IsDebug) Debug.Log(3);
                    return;
                }
            if (IsDebug) Debug.Log(4);
            Dictionary<string, string> MaxMinDamageByType = ConfigData.MaxMinDamageByType;
            if (IsDebug) Debug.Log(5);
            if (MaxMinDamageByType?.ContainsKey(type.ToString()) ?? false)
            {
                if (IsDebug) Debug.Log(6);
                info.damageTypes.Set(type, RandomRange(MaxMinDamageByType[type.ToString()]));
                if (IsDebug) Debug.Log(7);
                return;
            }
            if (IsDebug) Debug.Log(8);
            info.damageTypes.Set(type, RandomRange(ConfigData.DefaultMaxDamage, ConfigData.DefaultMinDamage));
            if (IsDebug) Debug.Log(9);
        }
        static float RandomRange(float max, float min) => UnityEngine.Random.Range(Mathf.Min(max, min), Mathf.Max(max, min));
        static int RandomRange(int max, int min) => UnityEngine.Random.Range(Mathf.Min(max, min), Mathf.Max(max, min));
        static float RandomRange(string minmax)
        {
            float min = float.Parse(minmax.Replace(", ", " ").Split(' ')[0]);
            float max = float.Parse(minmax.Replace(", ", " ").Split(' ')[1]);
            return RandomRange(min, max);
        }
        static T RandomValue<T>(IEnumerable<T> list) where T : class
        {
            if (list == null) return null;
            if (list.Count() < 1) return null;
            return list.ElementAt(UnityEngine.Random.Range(0, list.Count()));
        }
        static Vector3 ToVector3(string str) => new Vector3(float.Parse(str.Split(' ')[0]), float.Parse(str.Split(' ')[1]), float.Parse(str.Split(' ')[2]));
        static string ToString(Vector3 vector3) => $"{vector3.x} {vector3.y} {vector3.z}";
        [ChatCommand("spawn.car")] void ChatCommand_SpawnCar(BasePlayer player, string command, string[] args) => BaseSedan.CreateCar(player.transform.position);
        [ChatCommand("spawn.par")] void ChatCommand_SpawnPar(BasePlayer player, string command, string[] args) => EntityDrop.Call("Spawn", BaseSedan.CreateCar(player.transform.position), player.transform.position);
        [ChatCommand("add.spawn.car")] void ChatCommand_AddSpawnCar(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            ConfigData.SpawnPositions.Add($"{ToString(player.transform.position)}, {ToString(player.transform.rotation.eulerAngles)}");
            _plugin.SetConfig(true, "SpawnPositions (position, rotation)", ConfigData.SpawnPositions); _plugin.SaveConfig();
        }
        [ConsoleCommand("spawn.car")] void ConsoleCommand_Spawn_Car(ConsoleSystem.Arg arg)
        {
            if (arg?.IsAdmin != true) return;
            switch (arg?.Args?.Count())
            {
                case 3:
                    float x = 0;
                    float y = 0;
                    float z = 0;
                    if (!(float.TryParse(arg.Args[0], out x) && float.TryParse(arg.Args[1], out y) && float.TryParse(arg.Args[2], out z))) return;
                    BaseSedan.CreateCar(new Vector3(x, y, z));
                    return;
                case 1:
                    BasePlayer player = Player.FindById(arg.Args[0]);
                    if (player == null) return;
                    BaseSedan.CreateCar(player.transform.position + new Vector3(0, 100, 0));
                    return;
            }
        }
        static T Clone<T>(T obj)
        {
            var inst = obj.GetType().GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return (T)inst?.Invoke(obj, null);
        }
        static List<T> Ts<T>(params T[] param) => param.ToList();
        void Unload()
        {
            SaveAll_BaseSedan();
            foreach (var car in sedans.ToArray())
                car?.Kill();
        }
        static Put MoveTo<Copy, Put>(Copy copy)where Copy : Component where Put : Copy
        {
            Put put = copy.gameObject.GetComponent<Put>() ?? copy.gameObject.AddComponent<Put>();
            foreach (System.Reflection.FieldInfo field in typeof(Copy).GetFields()) field.SetValue(put, field.GetValue(copy));
            UnityEngine.Object.Destroy(copy.gameObject.GetComponent<Copy>());
            return put;
        }
        static void Invoke<T>(T obj, string method, params object[] param) => typeof(T).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(obj, param);
        static V GetValue<T, V>(T obj, string field) => (V)typeof(T).GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).GetValue(obj);
        static void SetValue<T, V>(T obj, string field, V value) => typeof(T).GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).SetValue(obj, value);
        public class EntityCreator
        {
            public static BaseEntity InstantiateEntity(string type, Vector3 pos, Quaternion rot = default(Quaternion), bool spawn = true)
            {
                rot = rot == default(Quaternion) ? Quaternion.identity : rot;
                string[] array = GameManifest.Current.entities.Where((x) => GetFileNameWithoutExtension(x).Contains(type, System.Globalization.CompareOptions.IgnoreCase)).Select((x) => x.ToLower()).ToArray();
                if (array.Length == 0) return null;
                if (array.Length > 1) array[0] = array.FirstOrDefault((string x) => string.Compare(GetFileNameWithoutExtension(x), type, StringComparison.OrdinalIgnoreCase) == 0);
                BaseEntity baseEntity = GameManager.server.CreateEntity(array[0], pos, rot, true);
                if (baseEntity == null) return null;
                if (spawn) baseEntity.Spawn();
                return baseEntity;
            }
            public static string GetFileNameWithoutExtension(string path)
            {
                string filename = path.Split('/').LastOrDefault();
                string ret = "";
                for (int i = 0; i < filename.Split('.').Length - 1; i++) ret = ret + (ret == "" ? "" : ".") + filename.Split('.')[i];
                return ret;
            }
            public static List<string> GetPrefabs() => GameManifest.Current.entities.Select((x) => GetFileNameWithoutExtension(x).ToLower()).Distinct().ToList();
        }
        //static List<BaseSedan> bsedans = new List<BaseSedan>();
        public static class DDraw
        {
            public static void Line(BasePlayer player, float fDuration, Color color, Vector3 vPos, Vector3 vPosB) => player.SendConsoleCommand("ddraw.line", fDuration, color, vPos, vPosB);
            public static void Text(BasePlayer player, float fDuration, Color color, Vector3 vPos, string message) => player.SendConsoleCommand("ddraw.text", fDuration, color, vPos, message);
            public static void Line(float fDuration, Color color, Vector3 vPos, Vector3 vPosB)
            {
                foreach (var player in _plugin.Player.Players) player.SendConsoleCommand("ddraw.line", fDuration, color, vPos, vPosB);
            }
            public static void Text(float fDuration, Color color, Vector3 vPos, string message)
            {
                foreach (var player in _plugin.Player.Players) player.SendConsoleCommand("ddraw.text", fDuration, color, vPos, message);
            }
            public static void HealPanel(BasePlayer player, float value, float maxvalue, float FadeIn = 0, float FadeOut = 0)
            {
                CuiElementContainer cuis = UI.CreateElementContainer(_GUI_HealPanel, "0.2352941 0.5882353 0.2352941 1", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-74, 191), new Vector2(74, 199), false, FadeIn, FadeOut);

                new UILabel("1 1 1 1", $"{Mathf.FloorToInt(value)} / {Mathf.FloorToInt(maxvalue)}", 11, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 8f), new Vector2(-1f, 15f), TextAnchor.MiddleRight, FadeIn, FadeOut).Add(cuis, _GUI_HealPanel);
                new UIPanel("1 1 1 1", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2((value * 148) / maxvalue, 0f), false, FadeIn, FadeOut).Add(cuis, _GUI_HealPanel);

                string Json = cuis.ToJson();

                CuiHelper.DestroyUi(player, _GUI_HealPanel);
                CuiHelper.AddUi(player, Json);
            }
            public static void DelHealPanel(BasePlayer player, float value = 0, float maxvalue = 0, float FadeOut = 0)
            {
                if (FadeOut != 0) HealPanel(player, value, maxvalue, 0, FadeOut);
                CuiHelper.DestroyUi(player, _GUI_HealPanel);
            }
        }
        public class BaseSedan: BaseCar
        {
            public BasePlayer Driver => HasMountPoints() ? mountPoints.Count() > 0 ? mountPoints[0]?.mountable?.GetMounted() : null : GetMounted();
            public float Speed => ((Vector3.Distance(this.transform.position, lastPosition) / 1000) / (0.5f / (60f * 60f)));
            public float curSpeed = 0;
            Vector3 lastPosition = new Vector3(0, 0, 0);
            BasePlayer lastDriver = null;

            public StorageManager FuelInventory = null;
            public StorageManager TrunkInventory = null;
            public Item[] items = new Item[12] { null, null, null, null, null, null, null, null, null, null, null, null };
            
            public List<Timer> timers = new List<Timer>();

            public Vector3 LastPosition;
            public Vector3 LastRotation;

            public int FuelWater = 0;
            public int FuelCount = 0;
            public static string FuelItem = "crude_oil";//"fuel.lowgrade";

            BaseEntity fireEntity = null;
            string firePrefab = "oilfireball2";

            void Start()
            {
                sedans.Add(this);
                LastPosition = this.transform.position;
                LastRotation = this.transform.rotation.eulerAngles;
                lastPosition = this.transform.position;
                timers.Add(_plugin.timer.Repeat(0.5f, 0, () => TryCatch(UpdateSpeed)));
                timers.Add(_plugin.timer.Repeat(0.5f, 0, () => TryCatch(UpdateFuel)));
                timers.Add(_plugin.timer.Repeat(0.5f, 0, () => TryCatch(UpdateHeal)));
                timers.Add(_plugin.timer.Repeat(0.5f, 0, () => TryCatch(UpdateShow)));
                if (IsDebug) Debug.Log("Started!");
            }
            public new void FixedUpdate()
            {
                base.FixedUpdate();
                if (HasFlag(Flags.Reserved1)) SetFlag(Flags.Reserved1, (FuelWater + FuelCount * 120 > 0) ? IsMounted() : false, false);
            }
            public override void PlayerServerInput(InputState inputState, BasePlayer player)
            {
                if (FuelWater + FuelCount * 120 > 0)
                {
                    DriverInput(inputState, player);
                }
                else
                {
                    gasPedal = 0f;
                    brakePedal = 30f;
                    if (inputState.IsDown(BUTTON.LEFT))
                    {
                        steering = -60f;
                    }
                    else if (inputState.IsDown(BUTTON.RIGHT))
                    {
                        steering = 60f;
                    }
                    else
                    {
                        steering = 0f;
                    }
                }
            }
            public new void DriverInput(InputState inputState, BasePlayer player)
            {
                if (inputState.IsDown(BUTTON.FORWARD))
                {
                    gasPedal = EngineInWater() ? 0 : 100;
                    brakePedal = 0f;
                }
                else if (inputState.IsDown(BUTTON.BACKWARD))
                {
                    gasPedal = -30f;
                    brakePedal = 0f;
                }
                else
                {
                    gasPedal = 0f;
                    brakePedal = 30f;
                }
                if (inputState.IsDown(BUTTON.LEFT))
                {
                    steering = -60f;
                }
                else if (inputState.IsDown(BUTTON.RIGHT))
                {
                    steering = 60f;
                }
                else
                {
                    steering = 0f;
                }
            }
            public bool UseItem(BasePlayer player, Item item)
            {
                if (IsDebug) _plugin.Server.Broadcast("UseItem start");
                foreach (var child in childs)
                {
                    if (IsDebug) _plugin.Server.Broadcast($"Check start: {child.Value}({child.Key}) == {item.GetWorldEntity()} = {child.Value == item.GetWorldEntity()}");
                    if (child.Value == item.GetWorldEntity())
                        switch (child.Key)
                        {
                            case ChildType.None: return true;
                            case ChildType.Fuel: FuelInventory.Open(player); return true;
                            case ChildType.Trunk: TrunkInventory.Open(player); return true;
                        }
                }
                if (IsDebug) _plugin.Server.Broadcast("UseItem null");
                return false;
            }
            public void UpdateSpeed()
            {
                BasePlayer player = Driver;
                curSpeed = Speed;
                lastPosition = transform.position;
                if (lastDriver != player)
                {
                    try { CuiHelper.DestroyUi(lastDriver, _GUI_Speedometer); } catch { }
                    lastDriver = null;
                }
                if (player == null) return;
                CuiElementContainer cuis = UI.CreateElementContainer(_GUI_Speedometer, "0 0 0 0.3921569", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-150f, 130f), new Vector2(0f, 170f), false);
                new UIPanel("1 1 0 0.4705882", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-70f, -15f), new Vector2(70f, 15f), false).Add(cuis, _GUI_Speedometer);
                new UILabel("0 0 0 1", $"Speed: {Mathf.FloorToInt(curSpeed)} KPH\nFuel: {FuelCount}.{FuelWater} ITEM", 12, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-70f, -15f), new Vector2(70f, 15f), TextAnchor.MiddleCenter).Add(cuis, _GUI_Speedometer);
                string Json = cuis.ToJson();
                CuiHelper.DestroyUi(player, _GUI_Speedometer);
                CuiHelper.AddUi(player, Json);
                lastDriver = player;
            }
            public void UpdateFuel()
            {
                int FuelRemove = Mathf.FloorToInt(curSpeed);
                int FuelAll = FuelWater + FuelCount * 120;
                FuelAll = (FuelAll - FuelRemove > 0) ? (FuelAll - FuelRemove) : 0;
                FuelCount = FuelAll / 120;
                FuelWater = FuelAll - FuelCount * 120;
            }
            public void UpdateHeal()
            {
                if (_health < 80)
                {
                    if (fireEntity == null)
                    {
                        fireEntity = EntityCreator.InstantiateEntity(firePrefab, this.transform.TransformPoint(Vector3.forward * 2));
                        fireEntity.GetComponent<FireBall>().damagePerSecond = 0.5f;
                        fireEntity.GetComponent<FireBall>().radius = 2f;
                        fireEntity.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                    }
                    fireEntity.transform.position = this.transform.TransformPoint(Vector3.forward * 2);
                }
                else
                {
                    fireEntity?.Kill();
                    fireEntity = null;
                }
            }
            public virtual bool EngineInWater() => TerrainMeta.WaterMap.GetHeight(this.transform.position) > (this.transform.position.y + 2);
            public override void Hurt(HitInfo info)
            {
                UnityEngine.Assertions.Assert.IsTrue(base.isServer, "This should be called serverside only");
                if (this.IsDead())
                {
                    return;
                }
                using (TimeWarning.New("Hurt( HitInfo )", 50L))
                {
                    float health = this.health;
                    this.ScaleDamage(info);
                    if (info.PointStart != Vector3.zero)
                    {
                        for (int i = 0; i < this.propDirection.Length; i++)
                        {
                            if (!(this.propDirection[i].extraProtection == null))
                            {
                                if (!this.propDirection[i].IsWeakspot(base.transform, info))
                                {
                                    this.propDirection[i].extraProtection.Scale(info.damageTypes, 1f);
                                }
                            }
                        }
                    }
                    info.damageTypes.Scale(Rust.DamageType.Arrow, ConVar.Server.arrowdamage);
                    info.damageTypes.Scale(Rust.DamageType.Bullet, ConVar.Server.bulletdamage);
                    info.damageTypes.Scale(Rust.DamageType.Slash, ConVar.Server.meleedamage);
                    info.damageTypes.Scale(Rust.DamageType.Blunt, ConVar.Server.meleedamage);
                    info.damageTypes.Scale(Rust.DamageType.Stab, ConVar.Server.meleedamage);
                    info.damageTypes.Scale(Rust.DamageType.Bleeding, ConVar.Server.bleedingdamage);
                    _plugin.SetHit(info);
                    if (Interface.CallHook("IOnBaseCombatEntityHurt", this, info) != null)
                    {
                        return;
                    }
                    this.health = health - info.damageTypes.Total();
                    base.SendNetworkUpdate(global::BasePlayer.NetworkQueue.Update);
                    if (ConVar.Global.developer > 1)
                    {
                        if (IsDebug) Debug.Log(string.Concat(new object[]
                        {
                    "[Combat]".PadRight(10),
                    base.gameObject.name,
                    " hurt ",
                    info.damageTypes.GetMajorityDamageType(),
                    "/",
                    info.damageTypes.Total(),
                    " - ",
                    this.health.ToString("0"),
                    " health left"
                        }));
                    }
                    this.lastDamage = info.damageTypes.GetMajorityDamageType();
                    this.lastAttacker = info.Initiator;
                    if (this.lastAttacker != null)
                    {
                        global::BaseCombatEntity baseCombatEntity = this.lastAttacker as global::BaseCombatEntity;
                        if (baseCombatEntity != null)
                        {
                            baseCombatEntity.lastDealtDamageTime = UnityEngine.Time.time;
                        }
                    }
                    global::BaseCombatEntity baseCombatEntity2 = this.lastAttacker as global::BaseCombatEntity;
                    if (this.markAttackerHostile && baseCombatEntity2 != null && baseCombatEntity2 != this)
                    {
                        baseCombatEntity2.MarkHostileFor(60f);
                    }
                    if (this.lastDamage != Rust.DamageType.Decay)
                    {
                        this.lastAttackedTime = UnityEngine.Time.time;
                        if (this.lastAttacker != null)
                        {
                            this.LastAttackedDir = (this.lastAttacker.ServerPosition - this.ServerPosition).normalized;
                        }
                    }
                    if (this.health <= 0f)
                    {
                        this.Die(info);
                    }
                    global::BasePlayer initiatorPlayer = info.InitiatorPlayer;
                    if (initiatorPlayer)
                    {
                        if (this.IsDead())
                        {
                            initiatorPlayer.stats.combat.Log(info, health, this.health, "killed");
                        }
                        else
                        {
                            initiatorPlayer.stats.combat.Log(info, health, this.health, null);
                        }
                    }
                    if (IsDebug) Debug.Log($"{ShortPrefabName} [{Health()}|{MaxHealth()}]");
                    UpdateHeal();
                    UpdateShow();
                }
            }
            public new List<ItemAmount> RepairCost(float value)
            {
                List<ItemAmount> list = new List<ItemAmount>();
                foreach (ItemAmount itemAmount in ConfigData.RepairCost.Select(i => new ItemAmount(ItemManager.itemList.Where(d => d.name == i.Key).FirstOrDefault(), i.Value)))
                {
                    int num = Mathf.RoundToInt(itemAmount.amount * RepairCostFraction() * value);
                    if (IsDebug) Debug.Log($"[Item:{itemAmount.itemDef.name}] {itemAmount.amount} * {RepairCostFraction()} * {value} = {num}");
                    if (num > 0) list.Add(new ItemAmount(itemAmount.itemDef, num));
                }
                return list;
            }
            public override void DoRepair(BasePlayer player)
            {
                if (Interface.CallHook("OnStructureRepair", (BaseCombatEntity)this, player) != null) return;
                if (IsDebug) Debug.Log($"{SecondsSinceAttacked} <= 30f ? {SecondsSinceAttacked <= 30f}\n{this.MaxHealth()} - {this.health} <= 0 ? {this.MaxHealth() - this.health <= 0f}\n{this.MaxHealth() - this.health} / {this.MaxHealth()} <= 0 ? {this.MaxHealth()}");
                if (SecondsSinceAttacked <= 30f)
                {
                    OnRepairFailed();
                    return;
                }
                float num = this.MaxHealth() - this.health;
                float num2 = num / this.MaxHealth();
                if (num <= 0f || num2 <= 0f)
                {
                    OnRepairFailed();
                    return;
                }
                List<global::ItemAmount> list = RepairCost(num2);
                if (list == null) return;
                if (IsDebug) Debug.Log($"RepairCost Items:\n..{string.Join("\n..", list.Select(item => $"{item.itemDef.name}*{item.amount}"))}");
                float num3 = list.Sum((ItemAmount x) => x.amount);

                if (num3 > 0f)
                {
                    float num4 = list.Min((global::ItemAmount x) => Mathf.Clamp01((float)player.inventory.GetAmount(x.itemid) / x.amount));
                    if (IsDebug) Debug.Log($"{num4} -> {Mathf.Min(num4, 50f / num)}");
                    num4 = Mathf.Min(num4, 50f / num);
                    if (num4 <= 0f)
                    {
                        this.OnRepairFailed();
                        return;
                    }
                    int num5 = 0;
                    foreach (global::ItemAmount itemAmount in list)
                    {
                        int amount = Mathf.CeilToInt(num4 * itemAmount.amount);
                        int num6 = player.inventory.Take(null, itemAmount.itemid, amount);
                        if (num6 > 0)
                        {
                            num5 += num6;
                            player.Command("note.inv", new object[] { itemAmount.itemid, num6 * -1 });
                        }
                    }
                    float num7 = (float)num5 / num3;
                    this.health += num * num7;
                    base.SendNetworkUpdate(global::BasePlayer.NetworkQueue.Update);
                }
                else
                {
                    this.health += num;
                    base.SendNetworkUpdate(global::BasePlayer.NetworkQueue.Update);
                }
                if (this.health >= this.MaxHealth())
                {
                    this.OnRepairFinished();
                }
                else
                {
                    this.OnRepair();
                }
            }
            public override void Die(HitInfo info = null)
            {
                if (IsDead())
                {
                    return;
                }
                sedans.Remove(this);
                FuelInventory.CloseAll();
                TrunkInventory.CloseAll();

                foreach (var timer in timers) timer.Destroy();
                fireEntity?.Kill();

                Vector3 position = transform.position;
                List<List<Vector3>> exps = new List<List<Vector3>>()
                {
                    Ts<Vector3>(new Vector3(0, 0, 0)),
                    Ts<Vector3>(new Vector3(0, 0, -1), new Vector3(0, 0, 1), new Vector3(0, 0, 0), new Vector3(0, -1, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(-1, 0, 0))
                };
                if (ConfigData.CreateEffectBoom)
                    for (int i = 0; i < exps.Count(); i++)
                        foreach (var pos in exps[i])
                            _plugin.timer.Once(i * 0.5f, () => Explosion(position + pos));
                if (ConfigData.DropFuel) foreach (var item in FuelInventory.FuncOpen.Invoke().itemList) item.CreateWorldObject(transform.position);
                if (ConfigData.DropTruck) foreach (var item in TrunkInventory.FuncOpen.Invoke().itemList) item.CreateWorldObject(transform.position);
                base.Die(info);
            }
            static void Explosion(Vector3 position)
            {
                Effect.server.Run("assets/prefabs/weapons/rocketlauncher/effects/rocket_explosion.prefab", position, Vector3.up);
                DamageUtil.RadiusDamage(null, null, position, 10, 20, new List<Rust.DamageTypeEntry>() { new Rust.DamageTypeEntry() { amount = 50, type = Rust.DamageType.Explosion } }, 1075980544, true);
            }

            List<string> aSeePlayers = new List<string>();
            public void UpdateShow()
            {
                List<string> bSeePlayers = new List<string>();
                foreach (var player in _plugin.Player.Players)
                {
                    if (GetPlayerSeat(player) != -1) continue;
                    if (CanWatchEntity(player, 5))
                    {
                        bSeePlayers.Add(player.UserIDString);
                        DDraw.HealPanel(player, Health(), MaxHealth());
                    }
                }
                foreach (var player in aSeePlayers.Where(a => !bSeePlayers.Contains(a)).Select(a => _plugin.Player.FindById(a)).Where(a => a != null))
                {
                    DDraw.DelHealPanel(player, Health(), MaxHealth());
                }
                aSeePlayers = bSeePlayers;
            }
            public bool CanWatchEntity(BasePlayer player, float distance)
            {
                foreach (var hit in Physics.RaycastAll(player.eyes.HeadRay(), distance))
                    if (hit.collider == _collider)
                        return true;
                return false;
            }

            public void KillChilds()
            {
                foreach (var ent in this.childs)
                    ent.Value?.Kill();
            }

            public static BaseSedan CreateCar(Vector3 position)
            {
                BaseEntity entity = MoveTo<BaseCar, BaseSedan>(EntityCreator.InstantiateEntity("sedantest.entity", position, default(Quaternion), false).gameObject.GetComponent<BaseCar>());
                entity.GetComponent<BaseCar>().ShowHealthInfo = false;
                entity.GetComponent<BaseCar>().startHealth = ConfigData.MaxHealthInCar;
                entity.GetComponent<BaseCar>()._maxHealth = ConfigData.MaxHealthInCar;
                entity.GetComponent<BaseCar>()._health = ConfigData.MaxHealthInCar;
                entity.Spawn();
                CreateChilds(entity.GetComponent<BaseSedan>());
                if (IsDebug) Debug.LogError("..." + string.Join("\n...", entity.GetComponents<Component>().Select(comp => comp.GetType().Name).ToArray()));
                return entity as BaseSedan;
            }
            public static void CreateCar(JsonSedan sedan)
            {
                BaseEntity entity = MoveTo<BaseCar, BaseSedan>(EntityCreator.InstantiateEntity("sedantest.entity", new Vector3(0, 0, 0), default(Quaternion), false).gameObject.GetComponent<BaseCar>());
                entity.Spawn();
                CreateChilds(entity.GetComponent<BaseSedan>());
                sedan.BaseSedan(entity.gameObject.GetComponent<BaseSedan>());
            }
            public static void CreateCar()
            {
                string val = RandomValue(ConfigData.SpawnPositions);
                if (val == null) return;
                string[] pair = val.Replace(", ", ",").Split(',');
                if (pair == null || pair.Count() != 2) return;
                BaseEntity entity = MoveTo<BaseCar, BaseSedan>(EntityCreator.InstantiateEntity("sedantest.entity", ToVector3(pair[0]), Quaternion.Euler(ToVector3(pair[1])), false).gameObject.GetComponent<BaseCar>());
                entity.GetComponent<BaseCar>().startHealth = ConfigData.MaxHealthInCar;
                entity.GetComponent<BaseCar>()._maxHealth = ConfigData.MaxHealthInCar;
                entity.GetComponent<BaseCar>()._health = ConfigData.MaxHealthInCar;
                entity.Spawn();
                CreateChilds(entity.GetComponent<BaseSedan>());
            }
            public Dictionary<ChildType, BaseEntity> childs = new Dictionary<ChildType, BaseEntity>();

            public enum ChildType
            {
                None,
                Fuel,
                Trunk,
            }

            public static void CreateChilds(BaseSedan sedan)
            {
                sedan.childs.Add(ChildType.Fuel, CreateItem("Sedan fuel", sedan, ItemManager.Create(ItemManager.itemList.Where(i => i.name.Contains("rocket_launcher.item")).FirstOrDefault(), 1, 0), new Vector3(0.9f, 1.1f, -1.1f), new Vector3(0, 270, 0)));
                sedan.childs.Add(ChildType.Trunk, CreateItem("Sedan trunk", sedan, ItemManager.Create(ItemManager.itemList.Where(i => i.name.Contains("rocket_launcher.item")).FirstOrDefault(), 1, 0), new Vector3(0f, 1f, -2.2f), new Vector3(0, 0, 0)));
                //sedan.childs.Add(ChildType.Trunk, CreateBox(sedan, new Vector3(0, 1.1f, -2), new Vector3(0, 0, 0)));

                sedan.FuelInventory = new StorageManager(() => sedan.transform.position - new Vector3(0, 1, 0), () =>
                {
                    ItemContainer container = sedan.FuelCount > 0 ? CreateContainer(1, ItemManager.Create(ItemManager.itemList.Where(item => item.name.ToLower().Contains(FuelItem)).First(), sedan.FuelCount)) : CreateContainer(1);
                    container.canAcceptItem = new Func<Item, int, bool>((item, slot) => { return item.info.name.ToLower().Contains(FuelItem); });
                    sedan.FuelCount = 0;
                    return container;
                }, (container) =>
                {
                    sedan.FuelCount = 0;
                    if (container.itemList.Count > 0)
                        foreach (Item item in container.itemList ?? new List<Item>())
                            if ((item?.info?.name ?? "").Contains(FuelItem))
                                sedan.FuelCount += item.amount;
                });
                sedan.TrunkInventory = new StorageManager(() => sedan.transform.position - new Vector3(0, 1, 0), () =>
                    {
                        ItemContainer container = CreateContainer(12, sedan.items);
                        sedan.items = new Item[12] { null, null, null, null, null, null, null, null, null, null, null, null };
                        return container;
                    }, (container) =>
                    {
                        sedan.items = new Item[12] { null, null, null, null, null, null, null, null, null, null, null, null };
                        for (int i = 0; i < 12; i++)
                            foreach (var item in container.itemList.ToArray())
                                if (item.position == i)
                                {
                                    sedan.items[i] = item;
                                    if (IsDebug) Debug.Log($"{i} = {sedan?.items[i]}");
                                }
                    });

            }
            public struct JsonSedan
            {
                public string Position;
                public string Rotation;
                public int FuelWater;
                public int FuelCount;
                public string FirePrefab;
                public float MaxHealth;
                public float Health;
                public string[] items;
                public JsonSedan(BaseSedan sedan)
                {
                    sedan.FuelInventory.CloseAll();
                    sedan.TrunkInventory.CloseAll();
                    this.Position = CarManager.ToString(sedan.transform.position); if (IsDebug) Debug.LogWarning(0);
                    this.Rotation = CarManager.ToString(sedan.transform.rotation.eulerAngles); if (IsDebug) Debug.LogWarning(1);
                    this.FuelWater = sedan.FuelWater; if (IsDebug) Debug.LogWarning(2);
                    this.FuelCount = sedan.FuelCount; if (IsDebug) Debug.LogWarning(3);
                    this.FirePrefab = sedan.firePrefab; if (IsDebug) Debug.LogWarning(5);
                    this.MaxHealth = sedan.MaxHealth(); if (IsDebug) Debug.LogWarning(6);
                    this.Health = sedan.Health(); if (IsDebug) Debug.LogWarning(7);
                    this.items = JsonItems.Items(sedan.items);
                }
                public void BaseSedan(BaseSedan sedan)
                {
                    sedan.transform.position = CarManager.ToVector3(Position);
                    sedan.transform.Rotate(CarManager.ToVector3(Rotation));
                    sedan.FuelWater = FuelWater;
                    sedan.FuelCount = FuelCount;
                    sedan.firePrefab = FirePrefab;
                    sedan.startHealth = MaxHealth;
                    sedan._maxHealth = MaxHealth;
                    sedan._health = Health;
                    sedan.items = JsonItems.Items(items);
                }
                public struct JsonItems
                {
                    public static string[] Items(Item[] items) => items.Select(item => item == null ? null : new string(ProtoBuf.Item.SerializeToBytes(item.Save()).Select(c => (char)c).ToArray())).ToArray();
                    public static Item[] Items(string[] items) => items.Select(item => { if (item == null) return null; Item g = new Item(); g.Load(ProtoBuf.Item.Deserialize(item.Select(c => (byte)c).ToArray())); return g; }).ToArray();
                }
            }
        }
        public static BaseEntity CreateItem(string code, BaseEntity parent, Item item, Vector3 lposition, Vector3 lrotation)
        {
            item.RemoveFromWorld(); if (IsDebug) Debug.Log("1");
            BaseEntity entity = item.CreateWorldObject(parent.transform.position); if (IsDebug) Debug.Log("2");
            UnityEngine.Object.Destroy(entity.GetComponent<Rigidbody>()); if (IsDebug) Debug.Log("3");
            entity.SetParent(parent); if (IsDebug) Debug.Log("4");
            entity.transform.position = parent.transform.position; if (IsDebug) Debug.Log("5");
            entity.transform.localPosition = lposition; if (IsDebug) Debug.Log("6");
            entity.transform.localRotation = Quaternion.Euler(lrotation); if (IsDebug) Debug.Log("7");
            item.name = code;
            return entity;
        }
     /*public static BaseEntity CreateBox(BaseEntity parent, Vector3 lposition, Vector3 lrotation)
        {
            BaseEntity entity = EntityCreator.InstantiateEntity("box.wooden.large", new Vector3(0, 0, 0), Quaternion.Euler(new Vector3(0, 0, 0)), true);
            if (entity?.GetComponent<Rigidbody>() != null) UnityEngine.Object.Destroy(entity.GetComponent<Rigidbody>());
            if (IsDebug) Debug.Log("3");
            entity.SetParent(parent); if (IsDebug) Debug.Log("4");
            entity.transform.position = parent.transform.position; if (IsDebug) Debug.Log("5");
            entity.transform.localPosition = lposition; if (IsDebug) Debug.Log("6");
            entity.transform.localRotation = Quaternion.Euler(lrotation); if (IsDebug) Debug.Log("7");
            return entity;
        }*/
        public static void TryCatch(Action action)
        {
            try { action.Invoke(); } catch { }
        }
        public class UIButton
        {
            public string _color;
            public string _text;
            public string _command;
            public Vector2 _aMin;
            public Vector2 _aMax;
            public Vector2 _oMin;
            public Vector2 _oMax;
            public int _size;
            public TextAnchor _align;
            public string _textColor;
            public float FadeIn = 0;
            public float FadeOut = 0;
            public UIButton(string color, string text, string command, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, int size = 14, TextAnchor align = TextAnchor.MiddleCenter, string textColor = "0 0 0 1", float FadeIn = 0, float FadeOut = 0)
            {
                _color = color;
                _text = text;
                _command = command;
                _aMin = aMin;
                _aMax = aMax;
                _oMin = oMin;
                _oMax = oMax;
                _size = size;
                _align = align;
                _textColor = textColor;
                this.FadeIn = FadeIn;
                this.FadeOut = FadeOut;
            }
            public virtual void Add(CuiElementContainer container, string panel) => UI.CreateButton(container, panel, _color, _text, _command, _aMin, _aMax, _oMin, _oMax, _size, _align, _textColor, FadeIn, FadeOut);
        }
        public class UIPanel
        {
            public string _color;
            public Vector2 _aMin;
            public Vector2 _aMax;
            public Vector2 _oMin;
            public Vector2 _oMax;
            public bool _cursor;
            public float FadeIn = 0;
            public float FadeOut = 0;
            public UIPanel(string color, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, bool cursor, float FadeIn = 0, float FadeOut = 0)
            {
                _color = color;
                _aMin = aMin;
                _aMax = aMax;
                _oMin = oMin;
                _oMax = oMax;
                _cursor = cursor;
                this.FadeIn = FadeIn;
                this.FadeOut = FadeOut;
            }
            public virtual void Add(CuiElementContainer container, string panel) => UI.CreatePanel(container, panel, _color, _aMin, _aMax, _oMin, _oMax, _cursor, FadeIn, FadeOut);
        }
        public class UILabel
        {
            public string _color;
            public string _text;
            public int _size;
            public Vector2 _aMin;
            public Vector2 _aMax;
            public Vector2 _oMin;
            public Vector2 _oMax;
            public TextAnchor _align;
            public float FadeIn = 0;
            public float FadeOut = 0;
            public UILabel(string color, string text, int size, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, TextAnchor align, float FadeIn = 0, float FadeOut = 0)
            {
                _color = color;
                _text = text;
                _size = size;
                _aMin = aMin;
                _aMax = aMax;
                _oMin = oMin;
                _oMax = oMax;
                _align = align;
                this.FadeIn = FadeIn;
                this.FadeOut = FadeOut;
            }
            public virtual void Add(CuiElementContainer container, string panel) => UI.CreateLabel(container, panel, _color, _text, _size, _aMin, _aMax, _oMin, _oMax, _align, FadeIn, FadeOut);
        }
        public class UIInputField
        {
            public string _color;
            public string _text;
            public int _size;
            public string _command;
            public bool _isPassword;
            public Vector2 _aMin;
            public Vector2 _aMax;
            public Vector2 _oMin;
            public Vector2 _oMax;
            public TextAnchor _align;
            public UIInputField(string color, string text, string command, bool isPassword, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, int size, TextAnchor align)
            {
                _color = color;
                _text = text;
                _size = size;
                _command = command;
                _isPassword = isPassword;
                _aMin = aMin;
                _aMax = aMax;
                _oMin = oMin;
                _oMax = oMax;
                _align = align;
            }
            public virtual void Add(CuiElementContainer container, string panel) => UI.CreateInputField(container, panel, _color, _text, _command, _isPassword, _aMin, _aMax, _oMin, _oMax, _size, _align);
        }
     /*public class UIScrollBar
              {
                  public string _color;
                  public string _icolor;
                  public float _size;
                  public float _value;
                  public Vector2 _aMin;
                  public Vector2 _aMax;
                  public Vector2 _oMin;
                  public Vector2 _oMax;
                  public UIScrollBar(string color, string icolor, float size, float value, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
                  {
                      _color = color;
                      _icolor = icolor;
                      _size = size;
                      _value = value;
                      _aMin = aMin;
                      _aMax = aMax;
                      _oMin = oMin;
                      _oMax = oMax;
                  }
                  public virtual void Create(BasePlayer player, string panel)
                  {
                      CuiElementContainer cuis = UI.CreateElementContainer(panel, _color, _aMin, _aMax, _oMin, _oMax, true);

                      float M = (1 - _size);
                      float H = (_oMax.y - _oMin.y) * M;

                      new UIPanel(_icolor, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, H * _value), new Vector2(0f, -(H - H * _value)), true).Add(cuis, panel);

                      string Json = cuis.ToJson();
                      CuiHelper.DestroyUi(player, panel);
                      CuiHelper.AddUi(player, Json);
                  }
              }*/
        public static class UI
        {
            public static CuiElementContainer CreateElementContainer(string panelName, string color, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, bool cursor = false, float fadein = 0, float fadeout = 0) => CreateElementContainer(panelName, color, $"{aMin.x} {aMin.y}", $"{aMax.x} {aMax.y}", $"{oMin.x} {oMin.y}", $"{oMax.x} {oMax.y}", cursor, fadein, fadeout);
            public static CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, string oMin, string oMax, bool useCursor = false, float fadein = 0, float fadeout = 0)
            {
                var NewElement = new CuiElementContainer()
                    {
                        {
                            new CuiPanel { Image = {Color = color, FadeIn = fadein}, FadeOut = fadeout, RectTransform = {AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax}, CursorEnabled = useCursor },
                            "Overlay",//new CuiElement().Parent,
                            panelName
                        }
                    };
                return NewElement;
            }
            public static void CreatePanel(CuiElementContainer container, string panel, string color, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, bool cursor = false, float fadein = 0, float fadeout = 0) => CreatePanel(container, panel, color, $"{aMin.x} {aMin.y}", $"{aMax.x} {aMax.y}", $"{oMin.x} {oMin.y}", $"{oMax.x} {oMax.y}", cursor, fadein, fadeout);
            public static void CreatePanel(CuiElementContainer container, string panel, string color, string aMin, string aMax, string oMin, string oMax, bool cursor = false, float fadein = 0, float fadeout = 0)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color, FadeIn = fadein },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax },
                    CursorEnabled = cursor,
                    FadeOut = fadeout
                },
                panel);
            }
            public static void CreateLabel(CuiElementContainer container, string panel, string color, string text, int size, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 0, float fadeout = 0) => CreateLabel(container, panel, color, text, size, $"{aMin.x} {aMin.y}", $"{aMax.x} {aMax.y}", $"{oMin.x} {oMin.y}", $"{oMax.x} {oMax.y}", align, fadein, fadeout);
            public static void CreateLabel(CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string oMin, string oMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 0, float fadeout = 0)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FadeIn = fadein, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax },
                    FadeOut = fadeout
                },
                panel);
            }
            public static void CreateButton(CuiElementContainer container, string panel, string color, string text, string command, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, int size = 14, TextAnchor align = TextAnchor.MiddleCenter, string textColor = "0 0 0 1", float fadein = 0, float fadeout = 0) => CreateButton(container, panel, color, text, command, $"{aMin.x} {aMin.y}", $"{aMax.x} {aMax.y}", $"{oMin.x} {oMin.y}", $"{oMax.x} {oMax.y}", size, align, textColor, fadein, fadeout);
            public static void CreateButton(CuiElementContainer container, string panel, string color, string text, string command, string aMin, string aMax, string oMin, string oMax, int size = 14, TextAnchor align = TextAnchor.MiddleCenter, string textColor = "0 0 0 1", float fadein = 0, float fadeout = 0)
            {
                container.Add(new CuiButton()
                {
                    Button = { Color = color, FadeIn = fadein, Command = command },
                    Text = { Text = text, FadeIn = fadein, FontSize = size, Align = align, Color = textColor },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax },
                    FadeOut = fadeout
                }, panel);
            }
            public static void CreateInputField(CuiElementContainer container, string panel, string color, string text, string command, bool isPassword, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, int size = 14, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 0, float fadeout = 0) => CreateInputField(container, panel, color, text, command, isPassword, $"{aMin.x} {aMin.y}", $"{aMax.x} {aMax.y}", $"{oMin.x} {oMin.y}", $"{oMax.x} {oMax.y}", size, align, fadein, fadeout);
            public static void CreateInputField(CuiElementContainer container, string panel, string color, string text, string command, bool isPassword, string aMin, string aMax, string oMin, string oMax, int size = 14, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 0, float fadeout = 0)
            {
                Add(container, new CuiInputField()
                {
                    InputField = { Color = color, Command = command, Text = text, IsPassword = isPassword, Align = align, FontSize = size },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax },
                    FadeOut = fadeout
                }, panel);
            }
            public static string Color(Color color) => $"{color.r} {color.g} {color.b} {color.a}";
            public static string Color(string hexColor, int alpha)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.TrimStart('#');
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);

                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {(double)alpha / 255}";
            }
            public static string Add(CuiElementContainer container, CuiInputField inputField, string parent = "Hud", string name = null)
            {
                if (string.IsNullOrEmpty(name))
                {
                    name = CuiHelper.GetGuid();
                }
                container.Add(new CuiElement
                {
                    Name = name,
                    Parent = parent,
                    FadeOut = inputField.FadeOut,
                    Components = { inputField.InputField, inputField.RectTransform }
                });
                return name;
            }
            public class CuiInputField
            {
                public CuiInputFieldComponent InputField { get; } = new CuiInputFieldComponent();
                public CuiRectTransformComponent RectTransform { get; } = new CuiRectTransformComponent();
                public float FadeOut { get; set; }
            }
        }

        public class StorageManager
        {
            public static Dictionary<StorageContainer, StorageManager> containers = new Dictionary<StorageContainer, StorageManager>();
            public StorageContainer Container = null;
            public Func<ItemContainer> FuncOpen;
            public Action<ItemContainer> ActionClosed;
            public Func<Vector3> Position;
            public List<BasePlayer> Users = new List<BasePlayer>();
            public StorageManager(Func<Vector3> position, Func<ItemContainer> opened, Action<ItemContainer> closed = null)
            {
                Creator(position, opened, closed);
            }
            public StorageManager(Func<Vector3> position, Func<ItemContainer> opened, Action closed)
            {
                Creator(position, opened, (container) => closed.Invoke());
            }
            public StorageManager(Vector3 position, Func<ItemContainer> opened, Action<ItemContainer> closed = null)
            {
                Creator(() => position, opened, closed);
            }
            public StorageManager(Vector3 position, Func<ItemContainer> opened, Action closed)
            {
                Creator(() => position, opened, (container) => closed.Invoke());
            }
            void Creator(Func<Vector3> position, Func<ItemContainer> opened, Action<ItemContainer> closed)
            {
                FuncOpen = opened;
                ActionClosed = closed;
                Position = position;
            }
            public void Open(BasePlayer player)
            {
                if (Users.Count() <= 0 || Container == null)
                {
                    Container?.Kill();
                    var boxContainer = EntityCreator.InstantiateEntity("box.wooden.large", player.transform.position/*Position.Invoke()*/, Quaternion.Euler(new Vector3(0, 0, 0)), false) as StorageContainer;

                    boxContainer.GetComponent<DestroyOnGroundMissing>().enabled = false;
                    boxContainer.GetComponent<GroundWatch>().enabled = false;
                    boxContainer.GetComponent<BoxCollider>().enabled = false;
                    
                    boxContainer.transform.position = player.transform.position; //Position.Invoke();

                    if (!boxContainer) return;

                    StorageContainer view = boxContainer as StorageContainer;
                    view.limitNetworking = true;
                    view.inventorySlots = 2;
                    player.EndLooting();

                    view.enableSaving = false;
                    view.Spawn();
                    view.inventory = FuncOpen?.Invoke();
                    Container = view;
                    if (!containers.ContainsKey(Container)) containers.Add(Container, this);
                }
                if (!Users.Contains(player))
                {
                    _plugin.timer.In(0.1f, () => { Container.PlayerOpenLoot(player); Users.Add(player); if (IsDebug) Debug.Log("OPEN!"); });
                }
            }
            public void Close(BasePlayer player)
            {
                if (IsDebug) Debug.Log($"Closed! {Users.Contains(player)}");
                if (!Users.Contains(player)) return;
                Users.Remove(player);
                player.EndLooting();
                if (Users.Count() <= 0)
                {
                    if (Container == null) return;
                    ActionClosed?.Invoke(Container.inventory);
                    Container.inventory = new ItemContainer();
                    Container.Kill();
                    if (containers.ContainsKey(Container)) containers.Remove(Container);
                    Container = null;
                }
            }
            public void CloseAll()
            {
                foreach (var user in Users.ToArray())
                    Close(user);
            }
            public static void Close(BasePlayer player, StorageContainer container)
            {
                if (IsDebug) _plugin.Server.Broadcast($"Player {player?.ToString() ?? "NULL"} closed container {container?.ToString() ?? "NULL"}: {containers.ContainsKey(container)}");
                if (containers.ContainsKey(container)) containers[container].Close(player);
            }
        }
    }
}
