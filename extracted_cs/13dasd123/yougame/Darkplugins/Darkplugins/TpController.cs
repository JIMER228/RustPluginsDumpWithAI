using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using Facepunch.Extend;
using System.IO;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("TpController", "https://discord.gg/dNGbxafuJn", "1.1.21")]
    public class TpController : RustPlugin
    {
        [PluginReference("Friends")] Plugin Friends;
        [PluginReference("FriendSystem")] Plugin FriendSystem;
        [PluginReference("Clans")] Plugin Clans;
        [PluginReference("Economics")] Plugin Economics;
        [PluginReference("RustMap")] Plugin RustMap;

        static Plugin NoEscape => Interface.Oxide.GetLibrary<Oxide.Core.Libraries.Plugins>().Find("NoEscape");

        static class Debug { public static void Log(object message) { if (DEBUG) UnityEngine.Debug.Log(message); } }
        static bool DEBUG = false;
        static float GLOBAL_UI_SIZE => ConfigData.UI_GlobalSize.Value; //1.5f;

        static bool IsTPBlock(BasePlayer player, ref MessageInfo? message)
        {
            if (NoEscape?.CallHook("IsEscapeBlocked", player.UserIDString)?.ToString() == true.ToString()) return true;
            object obj = Interface.Call("CanTeleport", player);
            if (obj is string) { _plugin.Player.Message(player, obj.ToString()); return true; }
            if (obj is bool) { if (!(bool)obj) return true; }
            if (obj != null) return true;
            if (player.HasParent()) { message = MessageInfo.Cant_InParent; return true; }
            return false;
        }

        static string _GUI_TP_Menu => $"{_plugin.Title}.{_plugin.Author}.{_plugin.Version}.TP.Menu";
        static string _GUI_TP_Button => $"{_plugin.Title}.{_plugin.Author}.{_plugin.Version}.TP.Button";
        static string _GUI_TP_Info => $"{_plugin.Title}.{_plugin.Author}.{_plugin.Version}.TP.Info";
        public static bool IsDebug = true;
        static TpController _plugin = null;
        static string Perm_TPAdmin = "tpcontroller.admin";
        void Loaded()
        {
            _plugin = this;
            LoadConfig();
            DATA.Load();
            permission.RegisterPermission(Perm_TPAdmin, this);
        }
        static void TryCatch(Action _try, Action _catch = null) => TryCatch(_try, (Exception e) => _catch?.Invoke());
        static void TryCatch<T>(Action _try, Action<T> _catch = null) where T : Exception { try { _try.Invoke(); } catch (T e) { _catch?.Invoke(e); } }
        static void Foreach<T>(IEnumerable<T> _ienumerable, Action<T> _action) { foreach (T item in _ienumerable) _action.Invoke(item); }
        void OnServerInitialized()
        {
            List<string> _comps = new List<string>() { "TPR", "TPA", "TPATimer", "GUITP", "TPHome", };
            Foreach(BaseNetworkable.serverEntities, (ent) => TryCatch(() => (ent?.GetComponents<Component>() ?? new Component[0]).Where(comp => _comps.Contains(comp?.GetType()?.Name)).Select(comp => { UnityEngine.Object.Destroy(comp); return true; })));
            Foreach(ConfigData.ToHome_Permissions.Value, (perm) => { if (perm.Key != "default" && !permission.PermissionExists(perm.Key)) permission.RegisterPermission(perm.Key, this); });
            Foreach(ConfigData.ToHome_CountDay.Value, (perm) => { if (perm.Key != "default" && !permission.PermissionExists(perm.Key)) permission.RegisterPermission(perm.Key, this); });
            Foreach(ConfigData.ToHome_CDTime.Value, (perm) => { if (perm.Key != "default" && !permission.PermissionExists(perm.Key)) permission.RegisterPermission(perm.Key, this); });
            Foreach(ConfigData.ToHome_Time.Value, (perm) => { if (perm.Key != "default" && !permission.PermissionExists(perm.Key)) permission.RegisterPermission(perm.Key, this); });
            Foreach(ConfigData.ToUser_Time.Value, (perm) => { if (perm.Key != "default" && !permission.PermissionExists(perm.Key)) permission.RegisterPermission(perm.Key, this); });
            Foreach(ConfigData.ToUser_CDTime.Value, (perm) => { if (perm.Key != "default" && !permission.PermissionExists(perm.Key)) permission.RegisterPermission(perm.Key, this); });
            Foreach(ConfigData.ToUser_CountDay.Value, (perm) => { if (perm.Key != "default" && !permission.PermissionExists(perm.Key)) permission.RegisterPermission(perm.Key, this); });
            if (((Friends ?? FriendSystem) == null && Clans == null) && (ConfigData.FriendsKey.Value == 0 || ConfigData.FriendsKey.Value == 1)) PrintError("Plugin 'Friends/FriendSystem' or 'Clans' not loaded but in config setted!");
            if (Economics == null && (ConfigData.ToUser_Economics.Value || ConfigData.ToHome_Economics.Value)) PrintError("Plugin 'Economics' not loaded but in config setted!");
            if (!ConfigData.ToHome_DrawInMap.Value) return;
            if (RustMap == null) PrintError("Plugin 'RustMap' not loaded but in config setted!");
        }
        Dictionary<string, Vector3> GetPlayerHomes(string player) => ConfigData.ToHome_DrawInMap.Value ? DATA.GetHomes(player).ToDictionary(kv => kv.Key.ToString(), kv => kv.Value) : null;

        public class ConfigData
        {
            //Общие настройки
            public static ConfigItem<int> FriendsKey = new ConfigItem<int>(() => GetConfig("Настройки", "Друзья из (-2: OFF, -1: Team, 0: Team + Плагин Friends/FriendSystem/Clan, 1: Плагин Friends/FriendSystem/Clan)", 0), (value) => SetConfig(true, "Настройки", "Друзья из (-2: OFF, -1: Team, 0: Team + Плагин Friends/FriendSystem/Clan, 1: Плагин Friends/FriendSystem/Clan)", value));
            public static ConfigItem<bool> LogAdminTP = new ConfigItem<bool>(() => GetConfig("Логирование", "Команды администраторов", false), (value) => SetConfig(true, "Логирование", "Команды администраторов", value));
            public static ConfigItem<bool> LogPlayerTP = new ConfigItem<bool>(() => GetConfig("Логирование", "Тп к игроку", false), (value) => SetConfig(true, "Логирование", "Тп к игроку", value));
            public static ConfigItem<bool> LogPlayerHome = new ConfigItem<bool>(() => GetConfig("Логирование", "Тп на дом", false), (value) => SetConfig(true, "Логирование", "Тп на дом", value));

            //Игрок
            public static ConfigItem<bool> ToUser_CanTP = new ConfigItem<bool>(() => GetConfig("Игрок", "Разрешить телепортацию", true), (value) => SetConfig(true, "Игрок", "Разрешить телепортацию", value));
            public static ConfigItem<float> ToUser_CanYesTime = new ConfigItem<float>(() => GetConfig("Игрок", "Время ответа на запрос телепортации (в секундах)", 15), (value) => SetConfig(true, "Игрок", "Время ответа на запрос телепортации (в секундах)", value));
            public static ConfigItem<Dictionary<string, float>> ToUser_Time = new ConfigItem<Dictionary<string, float>>("Игрок", "Вермя на телепортацию ('Пермишен': Секунд)", new Dictionary<string, float>() { ["default"] = 15 });
            public static ConfigItem<Dictionary<string, float>> ToUser_CDTime = new ConfigItem<Dictionary<string, float>>(() => GetConfig("Игрок", "КД на телепортацию ('Пермишен': Минут)", new Dictionary<string, float>() { ["default"] = 15 }), (value) => SetConfig(true, "Игрок", "КД на телепортацию ('Пермишен': Минут)", value));
            public static ConfigItem<Dictionary<string, int>> ToUser_CountDay = new ConfigItem<Dictionary<string, int>>(() => GetConfig("Игрок", "Количество ТП в день(-1 - Бесконечно) ('Пермишен': Количество ТП)", new Dictionary<string, int>() { ["default"] = -1 }), (value) => SetConfig(true, "Игрок", "Количество ТП в день(-1 - Бесконечно) ('Пермишен': Количество ТП)", value));
            public static ConfigItem<bool> ToUser_CanTPDamage = new ConfigItem<bool>(() => !GetConfig("Игрок", "Отменять телепортацию при получении урона?", true), (value) => SetConfig(true, "Игрок", "Отменять телепортацию при получении урона?", !value));
            public static ConfigItem<bool> ToUser_CanTPInWater = new ConfigItem<bool>(() => !GetConfig("Игрок", "Отменять телепортацию при нахождении в воде?", true), (value) => SetConfig(true, "Игрок", "Отменять телепортацию при нахождении в воде?", !value));
            public static ConfigItem<bool> ToUser_CanTPOverWater = new ConfigItem<bool>(() => !GetConfig("Игрок", "Отменять телепортацию при нахождении над водой?", true), (value) => SetConfig(true, "Игрок", "Отменять телепортацию при нахождении над водой?", !value));
            public static ConfigItem<bool> ToUser_Economics = new ConfigItem<bool>(() => GetConfig("Игрок", "Использовать Economics", false), (value) => SetConfig(true, "Игрок", "Использовать Economics", value));
            public static ConfigItem<float> ToUser_EconomicsCash = new ConfigItem<float>(() => GetConfig("Игрок", "Economics цена за использование", 10f), (value) => SetConfig(true, "Игрок", "Economics цена за использование", value));
            public static ConfigItem<bool> ToUser_BuildingBlock = new ConfigItem<bool>("Игрок", "Разрешить телепортацию в BuildingBlock?", true);

            //Дом
            public static ConfigItem<bool> ToHome_CanTP = new ConfigItem<bool>(() => GetConfig("Дом", "Разрешить телепортацию", true), (value) => SetConfig(true, "Дом", "Разрешить телепортацию", value));
            public static ConfigItem<Dictionary<string, float>> ToHome_Time = new ConfigItem<Dictionary<string, float>>("Дом", "Вермя на телепортацию ('Пермишен': Секунд)", new Dictionary<string, float>() { ["default"] = 15 });
            public static ConfigItem<Dictionary<string, float>> ToHome_CDTime = new ConfigItem<Dictionary<string, float>>(() => GetConfig("Дом", "КД на телепортацию ('Пермишен': Минут)", new Dictionary<string, float>() { ["default"] = 15 }), (value) => SetConfig(true, "Дом", "КД на телепортацию ('Пермишен': Минут)", value));
            public static ConfigItem<Dictionary<string, int>> ToHome_Permissions = new ConfigItem<Dictionary<string, int>>(() => GetConfig("Дом", "Количество домов ('Пермишен': Количество)", new Dictionary<string, int>() { ["default"] = 5 }), (value) => SetConfig(true, "Дом", "Количество домов ('Пермишен': Количество)", value));
            public static ConfigItem<Dictionary<string, int>> ToHome_CountDay = new ConfigItem<Dictionary<string, int>>(() => GetConfig("Дом", "Количество ТП в день(-1 - Бесконечно) ('Пермишен': Количество ТП)", new Dictionary<string, int>() { ["default"] = -1 }), (value) => SetConfig(true, "Дом", "Количество ТП в день(-1 - Бесконечно) ('Пермишен': Количество ТП)", value));
            public static ConfigItem<bool> ToHome_CanTPDamage = new ConfigItem<bool>(() => !GetConfig("Дом", "Отменять телепортацию при получении урона?", true), (value) => SetConfig(true, "Дом", "Отменять телепортацию при получении урона?", !value));
            public static ConfigItem<bool> ToHome_CanTPInWater = new ConfigItem<bool>(() => !GetConfig("Дом", "Отменять телепортацию при нахождении в воде?", true), (value) => SetConfig(true, "Дом", "Отменять телепортацию при нахождении в воде?", !value));
            public static ConfigItem<bool> ToHome_CanTPOverWater = new ConfigItem<bool>(() => !GetConfig("Дом", "Отменять телепортацию при нахождении над водой?", true), (value) => SetConfig(true, "Дом", "Отменять телепортацию при нахождении над водой?", !value));
            public static ConfigItem<bool> ToHome_CanTPNotFoundation = new ConfigItem<bool>(() => !GetConfig("Дом", "Запретить ставить дом на земле?", true), (value) => SetConfig(true, "Дом", "Запретить ставить дом на земле?", !value));
            public static ConfigItem<bool> ToHome_CanTPNotFoundation_Your = new ConfigItem<bool>(() => !GetConfig("Дом", "Запретить ставить дом на чужом фундаменте?", true), (value) => SetConfig(true, "Дом", "Запретить ставить дом на чужом фундаменте?", !value));
            public static ConfigItem<bool> ToHome_Economics = new ConfigItem<bool>(() => GetConfig("Дом", "Использовать Economics", false), (value) => SetConfig(true, "Дом", "Использовать Economics", value));
            public static ConfigItem<float> ToHome_EconomicsCash = new ConfigItem<float>(() => GetConfig("Дом", "Economics цена за использование", 10f), (value) => SetConfig(true, "Дом", "Economics цена за использование", value));
            public static ConfigItem<bool> ToHome_DrawInMap = new ConfigItem<bool>("Дом", "Отображать дома на карте RustMap?", false);
            public static ConfigItem<bool> ToHome_BuildingBlock = new ConfigItem<bool>("Дом", "Разрешить телепортацию в BuildingBlock?", true);

            //UI
            public static ConfigItem<float> UI_GlobalSize = new ConfigItem<float>(() => GetConfig("UI", "Размер интерфейса(def 1)", 1.0f), (value) => SetConfig(true, "UI", "Размер интерфейса(def 1)", value));
            public static ConfigItem<int> UI_Radius = new ConfigItem<int>(() => GetConfig("UI", "Радиус круга из иконок", 100), (value) => SetConfig(true, "UI", "Радиус круга из иконок", value));

            public static ConfigItem<string> UI_Friend_Color = new ConfigItem<string>(() => GetConfig("UI", "Цвет UI круга друга rgba(от 0 до 1)", "0 0 0 1"), (value) => SetConfig(true, "UI", "Цвет UI круга друга rgba(от 0 до 1)", value));
            public static ConfigItem<string> UI_TPR_Color = new ConfigItem<string>(() => GetConfig("UI", "Цвет UI круга при отправке телепортации rgba(от 0 до 1)", "0 0 0 1"), (value) => SetConfig(true, "UI", "Цвет UI круга при отправке телепортации rgba(от 0 до 1)", value));
            public static ConfigItem<string> UI_TPA_Color = new ConfigItem<string>(() => GetConfig("UI", "Цвет UI круга при приеме телепортации rgba(от 0 до 1)", "0 0 0 1"), (value) => SetConfig(true, "UI", "Цвет UI круга при приеме телепортации rgba(от 0 до 1)", value));
            public static ConfigItem<string> UI_Home_TP_Color = new ConfigItem<string>(() => GetConfig("UI", "Цвет UI круга при телепортации домой rgba(от 0 до 1)", "0 0 0 1"), (value) => SetConfig(true, "UI", "Цвет UI круга при телепортации домой rgba(от 0 до 1)", value));
            public static ConfigItem<string> UI_Home_Get_Color = new ConfigItem<string>(() => GetConfig("UI", "Цвет UI круга дома rgba(от 0 до 1)", "0 0 0 1"), (value) => SetConfig(true, "UI", "Цвет UI круга дома rgba(от 0 до 1)", value));
            public static ConfigItem<string> UI_Home_Set_Color = new ConfigItem<string>(() => GetConfig("UI", "Цвет UI круга пустого дома rgba(от 0 до 1)", "0 0 0 1"), (value) => SetConfig(true, "UI", "Цвет UI круга пустого дома rgba(от 0 до 1)", value));
            public static ConfigItem<string> UI_Page_Color = new ConfigItem<string>(() => GetConfig("UI", "Цвет UI круга перехода на другую страницу rgba(от 0 до 1)", "0 0 0 1"), (value) => SetConfig(true, "UI", "Цвет UI круга перехода на другую страницу rgba(от 0 до 1)", value));
            public static ConfigItem<string> UI_Settings_Color = new ConfigItem<string>(() => GetConfig("UI", "Цвет UI круга настроек rgba(от 0 до 1)", "0 0 0 1"), (value) => SetConfig(true, "UI", "Цвет UI круга настроек rgba(от 0 до 1)", value));

            public static ConfigItem<string> UI_Home_TP_On_Image_Color = new ConfigItem<string>("UI", "Цвет UI картинки телепортации домой rgba(от 0 до 1)", "0 1 0 1");
            public static ConfigItem<string> UI_Home_TP_Off_Image_Color = new ConfigItem<string>("UI", "Цвет UI картинки отмены телепортации домой rgba(от 0 до 1)", "0.3921569 0.7254902 0.7254902 1");
            public static ConfigItem<string> UI_Home_Set_Image_Color = new ConfigItem<string>("UI", "Цвет UI картинки пустого дома rgba(от 0 до 1)", "0.3921569 0.7254902 0.7254902 1");
            public static ConfigItem<string> UI_Page_Image_Color = new ConfigItem<string>("UI", "Цвет UI картинки перехода на другую страницу rgba(от 0 до 1)", "0.3921569 0.7254902 0.7254902 1");
            public static ConfigItem<string> UI_Settings_On_Image_Color = new ConfigItem<string>("UI", "Цвет UI картинки настроек (галочка) rgba(от 0 до 1)", "0 1 0 1");
            public static ConfigItem<string> UI_Settings_Off_Image_Color = new ConfigItem<string>("UI", "Цвет UI картинки настроек (крестик) rgba(от 0 до 1)", "1 0 0 1");
            public static ConfigItem<string> UI_Yes_Image_Color = new ConfigItem<string>("UI", "Цвет UI картинки кнопки подтвердить rgba(от 0 до 1)", "0 1 0 1");
            public static ConfigItem<string> UI_No_Image_Color = new ConfigItem<string>("UI", "Цвет UI картинки кнопки отклонить rgba(от 0 до 1)", "1 0 0 1");
            //ConfigData.UI_Settings_On_Image_Color.Value

            //UI_Settings_Color
            public static void LoadAndSaveAll()
            {
                foreach (var Field in typeof(ConfigData).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    ConfigItem cObj = (ConfigItem)Field.GetValue(null);
                    Debug.Log($"{Field.FieldType.Name} {Field.Name} = {cObj.get.Invoke()}");
                    cObj.set.Invoke(cObj.get.Invoke());
                }
            }
            public static void SetConfig(bool replace, string group, string key, object value)
            {
                if (replace || _plugin.Config.Get(group, key) == null) _plugin.Config.Set(group, key, value);
                _plugin.Config.Save();
            }
            public static T GetConfig<T>(string group, string key, T defaultVal)
            {
                if (_plugin.Config.Get(group, key) == null)
                {
                    SetConfig(false, group, key, defaultVal);
                    return defaultVal;
                }
                return (T)Convert.ChangeType(_plugin.Config.Get<T>(group, key), typeof(T));
            }
            public class ConfigItem<T> : ConfigItem
            {
                public ConfigItem(string group, string key, T defaultValue) : this(() => GetConfig(group, key, defaultValue), (value) => SetConfig(true, group, key, value)) { }
                public ConfigItem(Func<T> get, Action<T> set)
                {
                    this.get = () => get.Invoke();
                    this.set = (value) => set.Invoke((T)value);
                }
                public T Value { get { return (T)get.Invoke(); } set { set.Invoke(value); } }
                public static explicit operator T(ConfigItem<T> obj) => obj.Value;
            }
            public abstract class ConfigItem
            {
                public Func<object> get;
                public Action<object> set;
            }
        }

        static float GetUserTime(BasePlayer player) => GetUserTime(player.UserIDString);
        static float GetUserTime(ulong player) => GetUserTime(player.ToString());
        static float GetUserTime(string player) => Mathf.Min(ConfigData.ToUser_Time.Value.Where(kv => _plugin.permission.UserHasPermission(player, kv.Key) || kv.Key == "default").Select(kv => kv.Value).ToArray());

        static float GetHomeTime(BasePlayer player) => GetHomeTime(player.UserIDString);
        static float GetHomeTime(ulong player) => GetHomeTime(player.ToString());
        static float GetHomeTime(string player) => Mathf.Min(ConfigData.ToHome_Time.Value.Where(kv => _plugin.permission.UserHasPermission(player, kv.Key) || kv.Key == "default").Select(kv => kv.Value).ToArray());

        static DecayEntity GetFoundation(Vector3 pos)
        {
            RaycastHit hit;
            if (Physics.Raycast(pos, Vector3.down, out hit, 1f))
            {
                var entity = hit.GetEntity();
                Debug.Log(entity?.ShortPrefabName ?? "NULL");
                switch (entity?.ShortPrefabName)
                {
                    case "foundation.triangle": return entity as BuildingBlock;
                    case "foundation": return entity as BuildingBlock;
                    case "floor": return entity as BuildingBlock;
                    case "floor.triangle": return entity as BuildingBlock;
                    case "rug.deployed": return entity as DecayEntity;
                    case "rug.bear.deployed": return entity as DecayEntity;
                }
            }
            return null;
        }
        static DecayEntity GetFoundationUp(Vector3 pos)
        {
            RaycastHit hit;
            if (Physics.Raycast(pos, Vector3.up, out hit, 3f))
            {
                var entity = hit.GetEntity();
                Debug.Log(entity?.ShortPrefabName ?? "NULL");
                switch (entity?.ShortPrefabName)
                {
                    case "foundation.triangle": return entity as BuildingBlock;
                    case "foundation": return entity as BuildingBlock;
                }
            }
            return null;
        }
        static bool CanTeleportFoundation(BasePlayer player, Vector3 position)
        {
            Debug.Log("1");
            if (ConfigData.ToHome_CanTPNotFoundation.Value && ConfigData.ToHome_CanTPNotFoundation_Your.Value) return true;
            Debug.Log("2");
            DecayEntity foundation = GetFoundation(position);
            Debug.Log("3");
            if (foundation == null) return ConfigData.ToHome_CanTPNotFoundation.Value;
            Debug.Log("4");
            if (((BuildingBlock)foundation)?.grade == BuildingGrade.Enum.Twigs) return false;
            if (GetFoundationUp(position)) return false;
            if (foundation?.GetBuildingPrivilege() == null) return true;
            Debug.Log("5");
            List<string> regClan = GetNotExcept(ClanOrFriends.GetClanMembers(player) ?? new List<string>(), foundation.GetBuildingPrivilege().authorizedPlayers.Select(kv => kv.userid.ToString())).ToList();
            List<string> regFriends = GetNotExcept(ClanOrFriends.GetClanMembers(player) ?? new List<string>(), foundation.GetBuildingPrivilege().authorizedPlayers.Select(kv => kv.userid.ToString())).ToList();
            return foundation.GetBuildingPrivilege().IsAuthed(player) ? true : regClan.Count() > 0 ? true : regFriends.Count() > 0;
        }
        static IEnumerable<TValue> GetNotExcept<TValue>(IEnumerable<TValue> first, IEnumerable<TValue> second) => first.Distinct().Where(item => second.Contains(item));
        public enum MessageInfo
        {
            None,

            Cant_InParent,

            CoolDown_Use,

            TPR_TP_Yes,
            TPR_TP_No,
            TPR_TP,
            TPR_Send_TP,
            TPR_Command_Info,
            TPR_Cant_Count_0,
            TPR_Cant_Count_More,
            TPR_Cant_ActiveTP,
            TPR_Cant_MaxCountDay,
            TPR_Cant_RaidBlock,
            TPR_Cant_Economics,

            TPA_TP_Yes,
            TPA_TP_No,
            TPA_TP,
            TPA_Send_TP,
            TPA_Cant_Count_More,
            TPA_Cant_Count_0,
            TPA_Cant_Count_1,
            TPA_Cant_ActiveCount_More,
            TPA_Cant_RaidBlock,

            TPC_Cant_Count_More,
            TPC_Cant_Count_0,
            TPC_Cant_Count_1,
            TPC_Cant_ActiveCount_More,

            SetHome_Command_Info,
            SetHome_Set,
            SetHome_CantSet_Found,
            SetHome_CantSet_MaxCount,
            SetHome_CantSet_IsntNum,
            SetHome_CantSet_CantBuild,
            SetHome_CantSet_Foundation,

            DelHome_Command_Info,
            DelHome_Del,
            DelHome_CantDel_NotFound,
            DelHome_CantDel_IsntNum,

            TPHome_CantTP_NotFound,
            TPHome_CantTP_ActiveTP,
            TPHome_CantTP_IsntNum,
            TPHome_CantTP_Reason,
            TPHome_CantTP_RaidBlock,
            TPHome_CantTP_Foundation_NotSet,
            TPHome_CantTP_MaxCountDay,
            TPHome_CantTP_Economics,
            TPHome_Time,
            TPHome_Time_Now,

            HomeC_NotFound,
            HomeC_C,

            TPAdmin_TP,
            TPAdmin_TP_Player,
            TPAdmin_NotFound,
            TPAdmin_FoundMore,

            Reason_Cancel,
            Reason_Damage,
            Reason_InWater,
            Reason_OverWater,
            Reason_InAir,
            Reason_NoFriend,
            Reason_BuildingBlock
        }
        protected override void LoadDefaultMessages()
        {
            Dictionary<string, string> ru = new Dictionary<string, string>();
            foreach (var kv in new Dictionary<MessageInfo, string>()
            {
                [MessageInfo.CoolDown_Use] = "Вы не можете сделать это! Осталось {sec}сек.",

                [MessageInfo.TPR_TP] = "Вы были телепортированы к <color=#ffa500>{name}</color>. Осталось телепортаций {count}",
                [MessageInfo.TPR_TP_Yes] = "Ваш запрос на телопортацию подтвержден игроком <color=#ffa500>{name}</color>. Вы будете телепортированы через <color=#ffa500>{sec}</color> секунд.",
                [MessageInfo.TPR_TP_No] = "Запрос на телопортацию <color=#ffa500>отклонен</color> по причине <color=#ffa500>{reason}</color>",
                [MessageInfo.TPR_Send_TP] = "Вы отправили запрос на телепортацию игроку <color=#ffa500>{name}</color>",
                [MessageInfo.TPR_Cant_Count_0] = "Игрок <color=#ffa500>не найден!</color>",
                [MessageInfo.TPR_Cant_Count_More] = "Найдено <color=#ffa500>несколько игроков</color>",
                [MessageInfo.TPR_Cant_ActiveTP] = "У вас уже есть активная телепортация! Отменить текущую: </color=#ffa500>/tpc</color>",
                [MessageInfo.TPR_Cant_MaxCountDay] = "Использовано <color=#ffa500>максимально количество</color> телепортаций в день!",
                [MessageInfo.TPR_Cant_RaidBlock] = "У вас <color=#ffa500>блок</color> на телепортацию!",
                [MessageInfo.TPR_Command_Info] = "Для телепортации используйте: <color=#ffa500>/tpr Ник/SteamID</color>",
                [MessageInfo.TPR_Cant_Economics] = "У вас недастаточно баланса на счету",

                [MessageInfo.TPA_TP] = "<color=#ffa500>{name}</color> был телепортирован к вам",
                [MessageInfo.TPA_TP_Yes] = "Вы приняли запрос игрока <color=#ffa500>{name}</color>",
                [MessageInfo.TPA_TP_No] = "Запрос на телопортацию к вам <color=#ffa500>отклонен</color> по причине <color=#ffa500>{reason}</color>",
                [MessageInfo.TPA_Send_TP] = "Игрок <color=#ffa500>{name}</color> отправил запрос на телепортацию к вам\nПропишите <color=#ffa500>/tpa</color>, чтобы принять запрос",
                [MessageInfo.TPA_Cant_Count_More] = "Найдено <color=#ffa500>несколько игроков!</color>",
                [MessageInfo.TPA_Cant_Count_0] = "Не найдено активных запросов!",
                [MessageInfo.TPA_Cant_Count_1] = "Не найдено активных запросов от игрока <color=#ffa500>{name}</color>",
                [MessageInfo.TPA_Cant_ActiveCount_More] = "Найдено несколько активных запросов! <color=#ffa500>Укажите Ник или SteamID отправителя!</color>",
                [MessageInfo.TPA_Cant_RaidBlock] = "У вас <color=#ffa500>блок</color> на телепортацию!",

                [MessageInfo.TPC_Cant_Count_More] = "Найдено <color=#ffa500>несколько</color> игроков!",
                [MessageInfo.TPC_Cant_Count_0] = "Не найдено активных запросов!",
                [MessageInfo.TPC_Cant_Count_1] = "Не найдено активных запросов от игрока {name}!",
                [MessageInfo.TPC_Cant_ActiveCount_More] = "Найдено несколько активных запросов! Укажите имя или SteamID отправителя!",

                [MessageInfo.SetHome_Command_Info] = "Используйте: <color=#ffa500>/sethome Название</color>",
                [MessageInfo.SetHome_Set] = "Вы создали дом под названием <color=#ffa500>{home}</color>",
                [MessageInfo.SetHome_CantSet_Found] = "Дом с таким названием уже существует!",
                [MessageInfo.SetHome_CantSet_MaxCount] = "Достигнуто <color=#ffa500>максимальное</color> количество точек дома",
                [MessageInfo.SetHome_CantSet_CantBuild] = "Вы <color=#ffa500>не можете</color> поставить свой дом тут!",
                [MessageInfo.SetHome_CantSet_IsntNum] = "Название дома может состоять <color=#ffa500>только из цифр</color>",
                [MessageInfo.SetHome_CantSet_Foundation] = "Под вами не ваш(друга) или нету <color=#ffa500>фундамента</color>",

                [MessageInfo.DelHome_Command_Info] = "Используйте: <color=#ffa500>/removehome Название дома</color>",
                [MessageInfo.DelHome_Del] = "Вы успешно удалили дом <color=#ffa500>{home}</color>",
                [MessageInfo.DelHome_CantDel_NotFound] = "Дом с таким названием <color=#ffa500>не найден!</color>",
                [MessageInfo.DelHome_CantDel_IsntNum] = "Название дома может состоять <color=#ffa500>только из цифр</color>",

                [MessageInfo.TPHome_CantTP_NotFound] = "Дом с таким названием <color=#ffa500>не найден!</color>",
                [MessageInfo.TPHome_CantTP_ActiveTP] = "У вас уже есть активная телепортация домой! Отменить текущую: <color=#ffa500>/homec</color>",
                [MessageInfo.TPHome_Time] = "Вы успешно начали телепорт домой! Вы будете телепортированы через <color=#ffa500>{sec}</color> секунд.",
                [MessageInfo.TPHome_Time_Now] = "Вы были телепортированы домой! Осталось телепортаций {count}",
                [MessageInfo.TPHome_CantTP_IsntNum] = "Название дома может состоять <color=#ffa500>только из цифр</color>",
                [MessageInfo.TPHome_CantTP_Reason] = "Активные телепортации домой <color=#ffa500>отклонены!</color> по причине <color=#ffa500>{reason}</color>",
                [MessageInfo.TPHome_CantTP_RaidBlock] = "У вас <color=#ffa500>блок</color> на телепортацию!",
                [MessageInfo.TPHome_CantTP_Foundation_NotSet] = "Под вашим домом не ваш(друга) или нету <color=#ffa500>фундамента</color>",
                [MessageInfo.TPHome_CantTP_MaxCountDay] = "Использовано <color=#ffa500>максимально количество</color> телепортаций домой в день!",
                [MessageInfo.TPHome_CantTP_Economics] = "У вас недастаточно баланса на счету",

                [MessageInfo.HomeC_NotFound] = "Активных телепортаций домой <color=#ffa500>не найдено!</color>",
                [MessageInfo.HomeC_C] = "Активные телепортации домой <color=#ffa500>отклонены!</color>",

                [MessageInfo.TPAdmin_TP] = "Вы были телепортированы к <color=#ffa500>{val}</color>",
                [MessageInfo.TPAdmin_TP_Player] = "Игрок <color=#ffa500>{player}</color> телепортирован к <color=#ffa500>{val}</color>",
                [MessageInfo.TPAdmin_NotFound] = "Игрок не найден!",
                [MessageInfo.TPAdmin_FoundMore] = "Найдено <color=#ffa500>несколько</color> игроков!",

                [MessageInfo.Reason_Cancel] = "Отмена",
                [MessageInfo.Reason_Damage] = "Получение урона",
                [MessageInfo.Reason_InWater] = "В воде",
                [MessageInfo.Reason_OverWater] = "Над водой",
                [MessageInfo.Reason_NoFriend] = "Не друг",
                [MessageInfo.Cant_InParent] = "В транспорте",
                [MessageInfo.Reason_BuildingBlock] = "BuildingBlock",
            }) ru.Add(kv.Key.ToString(), kv.Value);
            foreach (var kv in new Dictionary<KeyValuePair<TpSettings, bool>, string>()
            {
                [new KeyValuePair<TpSettings, bool>(TpSettings.Close_TP, false)] = "Закрыть при ТП",
                [new KeyValuePair<TpSettings, bool>(TpSettings.Close_Use, false)] = "Закрыть при USE",
                [new KeyValuePair<TpSettings, bool>(TpSettings.Whitelist_Fr, false)] = "TPC не друзей",
                [new KeyValuePair<TpSettings, bool>(TpSettings.AutoTPA_Fr, false)] = "Авто TPA друзей",

                [new KeyValuePair<TpSettings, bool>(TpSettings.Close_TP, true)] = "Закрыть меню после принятия/отклонения/отправки запроса",
                [new KeyValuePair<TpSettings, bool>(TpSettings.Close_Use, true)] = "Закрыть меню после каждого действия",
                [new KeyValuePair<TpSettings, bool>(TpSettings.Whitelist_Fr, true)] = "Принимать запросы только от друзей",
                [new KeyValuePair<TpSettings, bool>(TpSettings.AutoTPA_Fr, true)] = "Автоматически принимать запросы от друзей",
            }) ru.Add("TpSettings_" + (!kv.Key.Value ? "Title" : "Message") + "_" + kv.Key.Key.ToString().ToLower(), kv.Value);
            lang.RegisterMessages(ru, this, "ru");
            Dictionary<string, string> en = new Dictionary<string, string>();
            foreach (var kv in new Dictionary<MessageInfo, string>()
            {
                [MessageInfo.CoolDown_Use] = "You can't do this! Wait {sec}sec.",

                [MessageInfo.TPR_TP] = "You has been teleported to <color=#ffa500>{name}</color>. Teleport count {count}",
                [MessageInfo.TPR_TP_Yes] = "Your request for teleportation is confirmed by the player <color=#ffa500>{name}</color>. You will be teleported from <color=#ffa500>{sec}</color> sec.",
                [MessageInfo.TPR_TP_No] = "Request for teleporting <color=#ffa500> rejected </color> due to <color=#ffa500>{reason}</color>",
                [MessageInfo.TPR_Send_TP] = "You have sent a request for teleportation to the player <color=#ffa500>{name}</color>",
                [MessageInfo.TPR_Cant_Count_0] = "Player <color=#ffa500>not found!</color>",
                [MessageInfo.TPR_Cant_Count_More] = "Found <color=#ffa500>more than one player</color>",
                [MessageInfo.TPR_Cant_ActiveTP] = "You already have an active teleportation! Cancel: </color=#ffa500>/tpc</color>",
                [MessageInfo.TPR_Cant_MaxCountDay] = "Used <color=#ffa500>maximum number</color> of teleports per day!",
                [MessageInfo.TPR_Cant_RaidBlock] = "You have a <color=#ffa500>block</color> to teleport!",
                [MessageInfo.TPR_Command_Info] = "For teleportation use: <color=#ffa500>/tpr Nickname/SteamID</color>",
                [MessageInfo.TPR_Cant_Economics] = "Not have money!",

                [MessageInfo.TPA_TP] = "<color=#ffa500>{name}</color> was teleported to you",
                [MessageInfo.TPA_TP_Yes] = "You have accepted a player request <color=#ffa500>{name}</color>",
                [MessageInfo.TPA_TP_No] = "The request to teleport you <color=#ffa500>was rejected</color> due to <color=#ffa500>{reason}</color>",
                [MessageInfo.TPA_Send_TP] = "Player <color=#ffa500>{name}</color> sent a request for teleportation to you\nPlease write <color=#ffa500>/tpa</color> to accept the request",
                [MessageInfo.TPA_Cant_Count_More] = "Found <color=#ffa500>more than one player</color>",
                [MessageInfo.TPA_Cant_Count_0] = "No active queries found!",
                [MessageInfo.TPA_Cant_Count_1] = "No active requests found from player <color=#ffa500>{name}</color>",
                [MessageInfo.TPA_Cant_ActiveCount_More] = "Found more than one active query! <color=#ffa500>Enter the nickname or SteamID!</color>",
                [MessageInfo.TPA_Cant_RaidBlock] = "You have a <color=#ffa500>block</color> to teleport!",

                [MessageInfo.TPC_Cant_Count_More] = "Found <color=#ffa500>more than one player</color>",
                [MessageInfo.TPC_Cant_Count_0] = "No active queries found!",
                [MessageInfo.TPC_Cant_Count_1] = "No active requests found from player <color=#ffa500>{name}</color>",
                [MessageInfo.TPC_Cant_ActiveCount_More] = "Found more than one active query! <color=#ffa500>Enter the nickname or SteamID!</color>",

                [MessageInfo.SetHome_Command_Info] = "Use: <color=#ffa500>/sethome name</color>",
                [MessageInfo.SetHome_Set] = "You created a home with name <color=#ffa500>{home}</color>",
                [MessageInfo.SetHome_CantSet_Found] = "A home with this name already exists!",
                [MessageInfo.SetHome_CantSet_MaxCount] = "Reached <color=#ffa500>maximum</color> number of points at home",
                [MessageInfo.SetHome_CantSet_CantBuild] = "You <color=#ffa500>can't</color> set your home here!",
                [MessageInfo.SetHome_CantSet_IsntNum] = "Home name can only consist of <color=#ffa500>numbers</color>",
                [MessageInfo.SetHome_CantSet_Foundation] = "Under you is not your (friend's) or not <color=#ffa500>foundation</color>",

                [MessageInfo.DelHome_Command_Info] = "Use: <color=#ffa500>/removehome name</color>",
                [MessageInfo.DelHome_Del] = "You have successfully removed the home with name <color=#ffa500>{home}</color>",
                [MessageInfo.DelHome_CantDel_NotFound] = "A home with this name <color=#ffa500>not found!</color>",
                [MessageInfo.DelHome_CantDel_IsntNum] = "Home name can only consist of <color=#ffa500>numbers</color>",

                [MessageInfo.TPHome_CantTP_NotFound] = "A home with this name <color=#ffa500>not found!</color>",
                [MessageInfo.TPHome_CantTP_ActiveTP] = "You already have an active teleportation to home! Cancel: <color=#ffa500>/homec</color>",
                [MessageInfo.TPHome_Time] = "You have successfully started a teleport home! You will be teleported from <color=#ffa500>{sec}</color> sec.",
                [MessageInfo.TPHome_Time_Now] = "You were teleported home!",
                [MessageInfo.TPHome_CantTP_IsntNum] = "Home name can only consist of <color=#ffa500>numbers</color>",
                [MessageInfo.TPHome_CantTP_Reason] = "Active teleportation home <color=#ffa500>rejected!</color> due to <color=#ffa500>{reason}</color>",
                [MessageInfo.TPHome_CantTP_RaidBlock] = "You have a <color=#ffa500>block</color> to teleport! Teleport count {count}",
                [MessageInfo.TPHome_CantTP_Foundation_NotSet] = "Under you is not your (friend's) or not <color=#ffa500>foundation</color>",
                [MessageInfo.TPHome_CantTP_MaxCountDay] = "Used <color=#ffa500>maximum number</color> of teleports to home per day!",
                [MessageInfo.TPHome_CantTP_Economics] = "Not have money!",

                [MessageInfo.HomeC_NotFound] = "Active teleportation to home <color=#ffa500>not found!</color>",
                [MessageInfo.HomeC_C] = "Active teleportation to home <color=#ffa500>rejected!</color>",

                [MessageInfo.TPAdmin_TP] = "You has been teleported to <color=#ffa500>{val}</color>",
                [MessageInfo.TPAdmin_TP_Player] = "Player <color=#ffa500>{player}</color> teleported to <color=#ffa500>{val}</color>",
                [MessageInfo.TPAdmin_NotFound] = "Player not found!",
                [MessageInfo.TPAdmin_FoundMore] = "Found <color=#ffa500>more than one player</color>",

                [MessageInfo.Reason_Cancel] = "Cancel",
                [MessageInfo.Reason_Damage] = "Damage",
                [MessageInfo.Reason_InWater] = "In water",
                [MessageInfo.Reason_OverWater] = "Over water",
                [MessageInfo.Reason_NoFriend] = "Not friend",
                [MessageInfo.Cant_InParent] = "In transport",
                [MessageInfo.Reason_BuildingBlock] = "BuildingBlock",
            }) en.Add(kv.Key.ToString(), kv.Value);
            foreach (var kv in new Dictionary<KeyValuePair<TpSettings, bool>, string>()
            {
                [new KeyValuePair<TpSettings, bool>(TpSettings.Close_TP, false)] = "Close when TP",
                [new KeyValuePair<TpSettings, bool>(TpSettings.Close_Use, false)] = "Close when USE",
                [new KeyValuePair<TpSettings, bool>(TpSettings.Whitelist_Fr, false)] = "TPC if not friends",
                [new KeyValuePair<TpSettings, bool>(TpSettings.AutoTPA_Fr, false)] = "Auto TPA friends",

                [new KeyValuePair<TpSettings, bool>(TpSettings.Close_TP, true)] = "Close menu after accepting/rejecting/sending a request",
                [new KeyValuePair<TpSettings, bool>(TpSettings.Close_Use, true)] = "Close menu after each action",
                [new KeyValuePair<TpSettings, bool>(TpSettings.Whitelist_Fr, true)] = "Accept requests from friends only",
                [new KeyValuePair<TpSettings, bool>(TpSettings.AutoTPA_Fr, true)] = "Automatically accept requests from friends",
            }) en.Add("TpSettings_" + (!kv.Key.Value ? "Title" : "Message") + "_" + kv.Key.Key.ToString().ToLower(), kv.Value);
            lang.RegisterMessages(en, this, "en");
        }
        public static void Message(BasePlayer player, MessageInfo info, params KeyValuePair<string, string>[] args)
        {
            try
            {
                string message = _plugin.lang.GetMessage(info.ToString(), _plugin, player.UserIDString);
                //Debug.Log($"{info} -> {message}");
                //Debug.Log($"{string.Join("\n", _plugin.lang.GetMessages(_plugin.lang.GetLanguage(player.UserIDString), _plugin).Select(kv => $"{kv.Key}: {kv.Value}").ToArray())}");
                foreach (var arg in args) message = message.Replace(arg.Key, arg.Value);
                _plugin.Player.Message(player, message);
            }
            catch
            { }
        }
        public static string GetMessage(BasePlayer player, MessageInfo info, params KeyValuePair<string, string>[] args)
        {
            try
            {
                string message = _plugin.lang.GetMessage(info.ToString(), _plugin, player?.UserIDString);
                foreach (var arg in args) message = message.Replace(arg.Key, arg.Value);
                return message;
            }
            catch
            { }
            return null;
        }
        public static string GetMessage(BasePlayer player, TpSettings settings, bool IsTitle, params KeyValuePair<string, string>[] args)
        {
            try
            {
                string message = _plugin.lang.GetMessage("TpSettings_" + (IsTitle ? "Title" : "Message") + "_" + settings.ToString().ToLower(), _plugin, player?.UserIDString);
                foreach (var arg in args) message = message.Replace(arg.Key, arg.Value);
                return message;
            }
            catch
            { }
            return null;
        }
        new void LoadConfig()
        {
            ConfigData.LoadAndSaveAll();
            SaveConfig();
        }
        public List<BasePlayer> FindPlayers(string filter)
        {
            if (filter == "") return new List<BasePlayer>();
            List<BasePlayer> players = new List<BasePlayer>();
            foreach (var player in BasePlayer.activePlayerList)
            {
                Debug.Log($"FindPlayers[{player.UserIDString.ToLower()}|{player.displayName.ToLower()}|{filter.ToLower()}]");
                if (player.UserIDString.Contains(filter)) players.Add(player);
                if (player.displayName.ToLower().Contains(filter.ToLower())) players.Add(player);
                if (player.displayName.ToLower() == filter.ToLower()) return new List<BasePlayer> { player };
                if (player.UserIDString.ToLower() == filter.ToLower()) return new List<BasePlayer> { player };
            }
            return players;
        }
        public List<BasePlayer> FindSleepers(string filter)
        {
            if (filter == "") return new List<BasePlayer>();
            List<BasePlayer> sleepers = new List<BasePlayer>();
            foreach (var sleeper in BasePlayer.sleepingPlayerList)
            {
                Debug.Log($"FindSleepers[{sleeper.UserIDString.ToLower()}|{sleeper.displayName.ToLower()}|{filter.ToLower()}]");
                if (sleeper.UserIDString.Contains(filter)) sleepers.Add(sleeper);
                if (sleeper.displayName.ToLower().Contains(filter.ToLower())) sleepers.Add(sleeper);
                if (sleeper.displayName.ToLower() == filter.ToLower()) return new List<BasePlayer> { sleeper };
                if (sleeper.UserIDString.ToLower() == filter.ToLower()) return new List<BasePlayer> { sleeper };
            }
            return sleepers;
        }
        public List<T> FindObjects<T>(IEnumerable<T> enumerable, string filter, Func<T, string> func)
        {
            if (filter == "") return new List<T>();
            List<T> objs = new List<T>();
            foreach (var obj in enumerable)
            {
                if (func.Invoke(obj).Contains(filter)) objs.Add(obj);
                if (func.Invoke(obj).ToLower() == filter.ToLower()) return new List<T> { obj };
            }
            return objs;
        }
        static void PlayerInvoke(ConsoleSystem.Arg arg, Action<BasePlayer> action)
        {
            if (arg?.Player() != null) action?.Invoke(arg.Player());
        }
        public static void Teleport(BasePlayer player, Vector3 position)
        {
            //if (player.IsSleeping()) return;
            if (player.net?.connection != null) player.ClientRPCPlayer(null, player, "StartLoading");
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player)) BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
            player.MovePosition(position);
            if (player.net?.connection != null) player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
            if (player.net?.connection != null) player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            try { player.ClearEntityQueue(null); } catch { }
            player.SendFullSnapshot();
            //_plugin.timer.Once(0.1f, player.EndSleeping);
        }
        public abstract class ITeleport: MonoBehaviour
        {
            public abstract BasePlayer Player { get; }
            public abstract bool IsToUser { get; }
            public int lastHP = 0;
            public virtual bool CanTP(out MessageInfo message)
            {
                Vector3 pos = Player.transform.position;
                message = MessageInfo.None;
                if ((IsToUser ? ConfigData.ToUser_CanTPDamage : ConfigData.ToHome_CanTPDamage).Value ? false : (Mathf.FloorToInt(Player.health) < lastHP || Player.IsWounded()))
                {
                    message = MessageInfo.Reason_Damage;
                    return false;
                }
                if ((IsToUser ? ConfigData.ToUser_CanTPInWater : ConfigData.ToHome_CanTPInWater).Value ? false : Player.IsSwimming())
                {
                    message = MessageInfo.Reason_InWater;
                    return false;
                }
                if ((IsToUser ? ConfigData.ToUser_CanTPOverWater : ConfigData.ToHome_CanTPOverWater).Value ? false : TerrainMeta.HeightMap.GetHeight(pos) <= TerrainMeta.WaterMap.GetHeight(pos))
                {
                    message = MessageInfo.Reason_OverWater;
                    return false;
                }
                if ((IsToUser ? ConfigData.ToUser_BuildingBlock : ConfigData.ToHome_BuildingBlock).Value ? false : Player.IsBuildingBlocked())
                {
                    message = MessageInfo.Reason_BuildingBlock;
                    return false;
                }
                lastHP = Mathf.FloorToInt(Player.health);
                return true;
            }
        }
        #region PlayerToPlayer
        #region Commands
        [ChatCommand("tpr")]
        void ChatCommand_TPR(BasePlayer player, string command, string[] args)
        {
            Debug.Log("TPR.0");
            if (!ConfigData.ToUser_CanTP.Value) return;
            Debug.Log($"TPR.1.{Interface.Call("CanTeleport", player) ?? "NULL"}");
            object obj = Interface.Call("CanTeleport", player);
            if (obj is string) { Player.Message(player, obj.ToString()); return; }
            if (obj is bool) { if (!(bool)obj) return; }
            if (obj != null) return;
            Debug.Log("TPR.2");
            if (DATA.TpToPlayer_CD.ContainsKey(player.UserIDString))
            {
                Debug.Log("TPR.3");
                if (DATA.TpToPlayer_CD[player.UserIDString] > DateTime.Now.Ticks)
                {
                    Debug.Log("TPR.4");
                    Message(player, MessageInfo.CoolDown_Use, new KeyValuePair<string, string>("{sec}", Math.Round(new TimeSpan(DATA.TpToPlayer_CD[player.UserIDString] - DateTime.Now.Ticks).TotalSeconds, 1).ToString()));
                    Debug.Log("TPR.5");
                    return;
                }
                Debug.Log("TPR.6");
            }
            Debug.Log("TPR.7");
            if (args.Count() == 0)
            {
                Debug.Log("TPR.8");
                Message(player, MessageInfo.TPR_Command_Info);
                Debug.Log("TPR.9");
                return;
            }
            Debug.Log("TPR.10");
            List<BasePlayer> players = FindPlayers(string.Join(" ", args));
            Debug.Log("TPR.11");
            if (players.Count() > 1)
            {
                Debug.Log("TPR.12");
                Message(player, MessageInfo.TPR_Cant_Count_More);
                Debug.Log("TPR.13");
                return;
            }
            Debug.Log("TPR.14");
            if (players.Count() == 0)
            {
                Debug.Log("TPR.15");
                Message(player, MessageInfo.TPR_Cant_Count_0);
                Debug.Log("TPR.16");
                return;
            }
            Debug.Log("TPR.17");
            MessageInfo? message = null;
            if (IsTPBlock(player, ref message))
            {
                Debug.Log("TPR.18");
                Message(player, message ?? MessageInfo.TPR_Cant_RaidBlock);
                Debug.Log("TPR.19");
                return;
            }
            Debug.Log("TPR.20");
            if (players.Count() == 1)
            {
                Debug.Log("TPR.21");
                switch (TPR.TP(player, players.FirstOrDefault()))
                {
                    case 1: Debug.Log("TPR.22.1"); Message(player, MessageInfo.TPR_Cant_ActiveTP); Debug.Log("TPR.22.2"); return;
                    case 2: Debug.Log("TPR.23.1"); Message(player, MessageInfo.TPR_Cant_MaxCountDay); Debug.Log("TPR.23.2"); return;
                    case 3: Debug.Log("TPR.24.1"); Message(player, MessageInfo.TPR_Cant_Economics); Debug.Log("TPR.24.2"); return;
                    case 0: Debug.Log("TPR.25"); return;
                }
                Debug.Log("TPR.26");
                return;
            }
            Debug.Log("TPR.27");
        }
        [ChatCommand("tpa")]
        void ChatCommand_TPA(BasePlayer player, string command, string[] args)
        {
            if (!ConfigData.ToUser_CanTP.Value) return;
            List<BasePlayer> players = FindPlayers(string.Join(" ", args));
            if (players.Count() > 1)
            {
                Message(player, MessageInfo.TPA_Cant_Count_More);
                return;
            }
            MessageInfo? message = null;
            if (IsTPBlock(player, ref message))
            {
                Message(player, message ?? MessageInfo.TPA_Cant_RaidBlock);
                return;
            }
            switch (TPA.Yes(player, players.Count() > 0 ? players.FirstOrDefault() : null))
            {
                case 0: return;
                case 1: Message(player, MessageInfo.TPA_Cant_Count_0); return;
                case 2: Message(player, MessageInfo.TPA_Cant_ActiveCount_More); return;
                case 3: Message(player, MessageInfo.TPA_Cant_Count_1, new KeyValuePair<string, string>("{name}", (players.Count() > 0 ? players.FirstOrDefault() : null)?.displayName ?? "[НЕ УКАЗАНО]")); return;
            }
            return;
        }
        [ChatCommand("tpc")]
        void ChatCommand_TPC(BasePlayer player, string command, string[] args)
        {
            Debug.Log("TPC.0");
            if (!ConfigData.ToUser_CanTP.Value) return;
            Debug.Log("TPC.1");
            List<BasePlayer> players = FindPlayers(string.Join(" ", args));
            Debug.Log("TPC.2");
            if (players.Count() > 1)
            {
                Debug.Log("TPC.3");
                Message(player, MessageInfo.TPC_Cant_Count_More);
                Debug.Log("TPC.4");
                return;
            }
            Debug.Log("TPC.5");
            TPC(player, players.Count() > 0 ? players.FirstOrDefault() : null);
        }
        [ConsoleCommand("tp.tpr")] void ConsoleCommand_TPR(ConsoleSystem.Arg arg) => PlayerInvoke(arg, (player) => { if (DATA.GetSettings(player.UserIDString, TpSettings.Close_TP) || DATA.GetSettings(player.UserIDString, TpSettings.Close_Use)) GUITP.Stop(player); ChatCommand_TPR(player, "tpr", arg.Args); });
        [ConsoleCommand("tp.tpa")] void ConsoleCommand_TPA(ConsoleSystem.Arg arg) => PlayerInvoke(arg, (player) => { if (DATA.GetSettings(player.UserIDString, TpSettings.Close_TP) || DATA.GetSettings(player.UserIDString, TpSettings.Close_Use)) GUITP.Stop(player); ChatCommand_TPA(player, "tpa", arg.Args); });
        [ConsoleCommand("tp.tpc")] void ConsoleCommand_TPC(ConsoleSystem.Arg arg) => PlayerInvoke(arg, (player) => { if (DATA.GetSettings(player.UserIDString, TpSettings.Close_TP) || DATA.GetSettings(player.UserIDString, TpSettings.Close_Use)) GUITP.Stop(player); ChatCommand_TPC(player, "tpc", arg.Args); });
        #endregion
        #region Components
        public static void TPC(BasePlayer player1, BasePlayer player2)
        {
            Debug.Log("TPC.TPC.6");
            int retA = TPA.TPC(player1, player2);
            Debug.Log($"TPC.TPC.7.{retA}");
            if (retA == 0) return;
            Debug.Log($"TPC.TPC.8.{retA}");
            int retR = TPR.TPC(player1, player2);
            Debug.Log($"TPC.TPC.9.{retR}");
            if (retR == 0) return;
            Debug.Log($"TPC.TPC.10.{retR}");
            int retH = TPHome.HOMEC(player1);
            Debug.Log($"TPC.TPC.11.{retH}");
            if (retH == 0) return;
            Debug.Log($"TPC.TPC.12.{retH}");
            if (retA <= 1 && retR <= 1)
            {
                Debug.Log("TPC.TPC.13");
                Message(player1, MessageInfo.TPC_Cant_Count_0);
                Debug.Log("TPC.TPC.14");
                return;
            }
            Debug.Log($"TPC.TPC.15.{retA}");
            switch (retA)
            {
                case 2: Message(player1, MessageInfo.TPC_Cant_ActiveCount_More); return;
                case 3: Message(player1, MessageInfo.TPC_Cant_Count_1); return;
                default: return;
            }
        }
        public List<KeyValuePair<int, BasePlayer>> GetFriends(BasePlayer player)
        {
            try
            {
                List<KeyValuePair<int, BasePlayer>> ret = new List<KeyValuePair<int, BasePlayer>>();
                if (Clans != null) ret.AddRange(ClanOrFriends.GetClanMembers(player).Where(val => BasePlayer.Find(val)).Select(val => new KeyValuePair<int, BasePlayer>(1, BasePlayer.Find(val))));
                if (Friends ?? FriendSystem != null) ret.AddRange(ClanOrFriends.GetFriends(player).Where(val => BasePlayer.Find(val)).Select(val => new KeyValuePair<int, BasePlayer>(0, BasePlayer.Find(val))));
                return ret ?? new List<KeyValuePair<int, BasePlayer>>();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
                return new List<KeyValuePair<int, BasePlayer>>();
            }
        }
        public List<BasePlayer> GetFriendList(BasePlayer player)
        {
            List<BasePlayer> ret = new List<BasePlayer>();
            if (Clans != null) ret.AddRange(ClanOrFriends.GetClanMembers(player).Where(val => BasePlayer.Find(val)).Select(val => BasePlayer.Find(val)));
            if (Friends ?? FriendSystem != null) ret.AddRange(ClanOrFriends.GetFriends(player).Where(val => BasePlayer.Find(val)).Select(val => BasePlayer.Find(val)));
            return ret;
        }
        static class ClanOrFriends
        {
            public static List<string> GetClanMembersUmod(BasePlayer player)
            {
                var clanTag = _plugin.Clans.Call<string>("GetClanOf", player.userID);
                if (clanTag == null) return null;

                var resultRaw = _plugin.Clans.Call<JObject>("GetClan", clanTag);
                if (resultRaw == null) return null;

                var result = JsonConvert.DeserializeObject<ClanUModGetClanResult>(resultRaw.ToString());
                if (result == null) return null;

                var members = JsonConvert.DeserializeObject<Dictionary<string, string>>(result.members.ToString());
                return members != null ? new List<string>(members.Keys) : null;
            }
            class ClanUModGetClanResult
            {
                public string tag;
                public string owner;
                public JArray moderators;
                public JArray members;
                public JArray invited;
                public JArray allies;
                public JArray invitedallies;
            }
            public static List<string> GetClanMembersOvh(BasePlayer player)
            {
                var result = _plugin.Clans.Call<List<ulong>>("ApiGetClanMembers", player.userID);
                return result?.ConvertAll(i => i.ToString());
            }
            public static List<string> GetFriendsOvh(BasePlayer player)
            {
                var result = (List<ulong>)(_plugin.Friends ?? _plugin.FriendSystem).CallHook("ApiGetFriends", player.userID);
                return result?.ConvertAll(i => i.ToString());
            }
            public static List<string> GetFriends(BasePlayer player)
            {
                Debug.Log("TPDEBUG: 0");
                if (_plugin.FriendSystem != null) return _plugin.FriendSystem.Call<ulong[]>("GetFriend", player.userID).Select(val => val.ToString()).ToList();
                Debug.Log("TPDEBUG: 1");
                if (_plugin.Friends == null)
                {
                    UnityEngine.Debug.LogError("Plugin 'Friends' not loaded!");
                    return null;
                }
                Debug.Log("TPDEBUG: 2");
                if (_plugin.Friends.Author.ToLower().Contains("sanlerus")) return GetFriendsOvh(player);
                Debug.Log("TPDEBUG: 3");
                object ret = _plugin.Friends.Call("GetFriends", player.UserIDString) ?? _plugin.Friends.Call("GetFriends", player.userID);
                if (ret == null)
                {
                    UnityEngine.Debug.LogError("Plugin 'Friends' return NULL result!");
                    return null;
                }
                if (ret.GetType() == typeof(string[])) return new List<string>((string[])ret);
                if (ret.GetType() == typeof(ulong[])) return new List<string>(((ulong[])ret).Select(u => u.ToString()));
                if (ret.GetType() == typeof(List<ulong>)) return new List<string>(((List<ulong>)ret).Select(u => u.ToString()));
                if (ret.GetType() == typeof(List<string>)) return (List<string>)ret;
                UnityEngine.Debug.LogError($"Type '{ret?.GetType()?.FullName}' not supported!");
                return null;
            }
            public static List<string> GetClanMembers(BasePlayer player)
            {
                if (_plugin.Clans == null) return null;
                if (_plugin.Clans.Author.ToLower().Contains("k1lly0u")) return GetClanMembersUmod(player); //Clans by k1lly0u (uMod/Oxide)
                if (_plugin.Clans.Author.ToLower().Contains("sanlerus")) return GetClanMembersOvh(player); //Clans by Moscow.OVH
                return _plugin.Clans.Call<List<string>>("GetClanMembers", player.userID);
            }
        }
        ///<summary>Отправитель ТП</summary>
        public class TPR : ITeleport
        {
            /// <summary>Отменить ТП со стороны отправителя</summary>
            /// <param name="from">Отправитель</param>
            /// <param name="to">Получатель</param>
            /// <exception cref="0 - ТП отменено"></exception>
            /// <exception cref="1 - Не найдено активных ТП"></exception>
            /// <exception cref="2 - Несколько активных ТП и получатель не указан"></exception>
            /// <exception cref="3 - Получатель ТП не найден"></exception>
            public static int TPC(BasePlayer from, BasePlayer to)
            {
                Debug.Log("TPR.TPC.0");
                if (from.GetComponents<TPR>().Count() == 0) return 1;
                Debug.Log("TPR.TPC.1");
                if (from.GetComponents<TPA>().Count() == 1)
                {
                    Debug.Log("TPR.TPC.2");
                    from.GetComponent<TPR>().StopTP(GetMessage(from, MessageInfo.Reason_Cancel));
                    Debug.Log("TPR.TPC.3");
                    return 0;
                }
                Debug.Log("TPR.TPC.4");
                if (from.GetComponents<TPR>().Count() > 1 && to == null) return 2;
                Debug.Log($"TPR.TPC.5");
                Debug.Log($"TPR.TPC.DATA:");
                Debug.Log($"--from: {from ?? (object)"NULL"}");
                Debug.Log($"--from?.GetComponents<TPR>(): {from?.GetComponents<TPR>() ?? (object)"NULL"}");
                Debug.Log($"--from?.GetComponents<TPR>()?.ToArray(): {from?.GetComponents<TPR>()?.ToArray() ?? (object)"NULL"}");
                Debug.Log($"--to: {to ?? (object)"NULL"}");
                Debug.Log($"--to?.GetComponents<TPA>(): {to?.GetComponents<TPA>() ?? (object)"NULL"}");
                Debug.Log($"--to?.GetComponents<TPA>()?.ToArray(): {to?.GetComponents<TPA>()?.ToArray() ?? (object)"NULL"}");
                foreach (var tpr in from.GetComponents<TPR>().ToArray())
                {
                    Debug.Log("TPR.TPC.6 -> " + (tpr?.tpa?.Player?.userID.ToString() ?? "NULL") + " | " + (to?.userID.ToString() ?? "NULL"));
                    if (to == null || tpr.tpa.Player == to)
                    {
                        Debug.Log("TPR.TPC.7");
                        tpr.StopTP(GetMessage(tpr.Player, MessageInfo.Reason_Cancel));
                        Debug.Log("TPR.TPC.8");
                        return 0;
                    }
                    Debug.Log("TPR.TPC.9");
                }
                Debug.Log("TPR.TPC.10");
                return 3;
            }
            public Action actionTP = null;
            public Action actionTP_Yes = null;
            public Action<string> actionTP_No = null;

            /// <summary>Отправить ТП</summary>
            /// <param name="from">Отправитель</param>
            /// <param name="to">Получатель</param>
            /// <exception cref="0 - ТП отправленно"></exception>
            /// <exception cref="1 - Есть активный ТП"></exception>
            /// <exception cref="2 - Лимит на день использован"></exception>
            /// <exception cref="3 - Не хватает баланса"></exception>
            public static int TP(BasePlayer from, BasePlayer to)
            {
                if (from.GetComponent<TPR>()) return 1;
                if (!DATA.CanTpDay(from.UserIDString)) return 2;
                if (DATA.GetSettings(to.UserIDString, TpSettings.Whitelist_Fr))
                {
                    List<BasePlayer> playersTeam = new List<BasePlayer>();
                    switch (ConfigData.FriendsKey.Value)
                    {
                        case -1: playersTeam.AddRange(RelationshipManager.Instance.FindTeam(to.currentTeam)?.members?.Where(m => RelationshipManager.FindByID(m) != null)?.Select(m => RelationshipManager.FindByID(m))?.ToList() ?? new List<BasePlayer>()); break;
                        case 0:
                            playersTeam.AddRange(RelationshipManager.Instance.FindTeam(to.currentTeam)?.members?.Where(m => RelationshipManager.FindByID(m) != null)?.Select(m => RelationshipManager.FindByID(m))?.ToList() ?? new List<BasePlayer>());
                            playersTeam.AddRange(_plugin.GetFriendList(to)?.ToList() ?? new List<BasePlayer>());
                            break;
                        case 1: playersTeam.AddRange(_plugin.GetFriendList(to)?.ToList() ?? new List<BasePlayer>()); break;
                    }
                    if (!playersTeam.Contains(from))
                    {
                        Message(from, MessageInfo.TPR_TP_No, new KeyValuePair<string, string>("{name}", to.displayName), new KeyValuePair<string, string>("{reason}", GetMessage(from, MessageInfo.Reason_NoFriend)));
                        return 0;
                    }
                }
                if (!CanUseEconomics(from, ConfigData.ToUser_EconomicsCash.Value, withdraw: true)) return 3;
                TPR tpr = from.gameObject.AddComponent<TPR>();
                TPA tpa = to.gameObject.AddComponent<TPA>();
                tpa.tpr = tpr;
                tpr.tpa = tpa;
                Message(from, MessageInfo.TPR_Send_TP, new KeyValuePair<string, string>("{name}", to.displayName));
                Message(to, MessageInfo.TPA_Send_TP, new KeyValuePair<string, string>("{name}", from.displayName));

                tpr.actionTP = () => { Message(from, MessageInfo.TPR_TP, new KeyValuePair<string, string>("{name}", to.displayName), new KeyValuePair<string, string>("{count}", DATA.GetEndTpDay(from.UserIDString)?.ToString() ?? "∞")); };
                tpa.actionTP = () => { Message(to, MessageInfo.TPA_TP, new KeyValuePair<string, string>("{name}", from.displayName)); };

                tpr.actionTP_Yes = () => { Message(from, MessageInfo.TPR_TP_Yes, new KeyValuePair<string, string>("{name}", to.displayName), new KeyValuePair<string, string>("{sec}", Math.Round(GetUserTime(from), 2).ToString())); };
                tpa.actionTP_Yes = () => { Message(to, MessageInfo.TPA_TP_Yes, new KeyValuePair<string, string>("{name}", from.displayName)); };

                tpr.actionTP_No = (reason) => { CanUseEconomics(from, ConfigData.ToUser_EconomicsCash.Value, deposit: true); Message(from, MessageInfo.TPR_TP_No, new KeyValuePair<string, string>("{name}", to.displayName), new KeyValuePair<string, string>("{reason}", reason)); };
                tpa.actionTP_No = (reason) => { Message(to, MessageInfo.TPA_TP_No, new KeyValuePair<string, string>("{name}", from.displayName), new KeyValuePair<string, string>("{reason}", reason)); };
                return 0;
            }
            public TPA tpa;
            public override BasePlayer Player => GetComponent<BasePlayer>();
            public override bool IsToUser => true;

            void Start()
            {

            }
            public void FixedUpdate()
            {
                if (tpa == null || tpa?.Player?.IsDead() != false || Player?.IsDead() != false)
                {
                    StopTP(GetMessage(Player, MessageInfo.Reason_Cancel));
                    return;
                }
                MessageInfo message;
                if (CanTP(out message)) return;
                StopTP(GetMessage(Player, message));
            }
            public void StopTP(string reason)
            {
                actionTP_No?.Invoke(reason);
                tpa?.actionTP_No?.Invoke(reason);
                if (tpa != null) UnityEngine.Object.Destroy(tpa);
                UnityEngine.Object.Destroy(this);
            }

            // Check balance on multiple plugins and optionally withdraw money from the player
            static bool CanUseEconomics(BasePlayer player, double bypass, bool withdraw = false, bool deposit = false)
            {
                if (!ConfigData.ToUser_Economics.Value) return true;
                if (((double?)_plugin.Economics?.CallHook("Balance", player.UserIDString) ?? 0) < bypass) return false;
                if (withdraw == true) return (bool?)_plugin.Economics?.CallHook("Withdraw", player.userID, bypass) ?? false;
                if (deposit == true) return (bool?)_plugin.Economics?.CallHook("Deposit", player.userID, bypass) ?? false;
                return false;
            }
        }
        ///<summary>Получатель ТП</summary>
        public class TPA : ITeleport
        {
            public virtual bool IsTimer => false;

            private static float MaxCoolDown => ConfigData.ToUser_CanYesTime.Value;
            private float CoolDown = 0;

            /// <summary>Отменить ТП со стороны получателя</summary>
            /// <param name="from">Отправитель</param>
            /// <param name="to">Получатель</param>
            /// <exception cref="0 - ТП отменено"></exception>
            /// <exception cref="1 - Не найдено активных ТП"></exception>
            /// <exception cref="2 - Несколько активных ТП и отправитель не указан"></exception>
            /// <exception cref="3 - Отправитель ТП не найден"></exception>
            public static int TPC(BasePlayer to, BasePlayer from)
            {
                if (to.GetComponents<TPA>().Count() == 0) return 1;
                if (to.GetComponents<TPA>().Count() == 1)
                {
                    to.GetComponent<TPA>().StopTP(GetMessage(from, MessageInfo.Reason_Cancel));
                    return 0;
                }
                if (to.GetComponents<TPA>().Count() > 1 && from == null) return 2;
                foreach (var tpa in to.GetComponents<TPA>().ToArray())
                {
                    if (tpa.Player == from)
                    {
                        tpa.StopTP(GetMessage(from, MessageInfo.Reason_Cancel));
                        return 0;
                    }
                }
                return 3;
            }
            /// <summary>ринять тп</summary>
            /// <param name="from">Отправитель</param>
            /// <param name="to">Получатель</param>
            /// <exception cref="0 - ТП принято"></exception>
            /// <exception cref="1 - Не найдено активных ТП"></exception>
            /// <exception cref="2 - Несколько активных ТП и отправитель не указан"></exception>
            /// <exception cref="3 - Отправитель ТП не найден"></exception>
            public static int Yes(BasePlayer to, BasePlayer from)
            {
                if (to.GetComponents<TPA>().Count() == 0) return 1;
                if (to.GetComponents<TPA>().Count() == 1)
                {
                    to.GetComponent<TPA>().StartTP();
                    return 0;
                }
                if (to.GetComponents<TPA>().Count() > 1 && from == null) return 2;
                foreach (var tpa in to.GetComponents<TPA>().ToArray())
                {
                    if (tpa.IsTimer) continue;
                    if (tpa.Player == from)
                    {
                        tpa.StartTP();
                        return 0;
                    }
                }
                return 3;
            }
            void Start()
            {
                if (IsTimer) return;
                if (DATA.GetSettings(Player.UserIDString, TpSettings.AutoTPA_Fr))
                {
                    List<BasePlayer> playersTeam = new List<BasePlayer>();
                    switch (ConfigData.FriendsKey.Value)
                    {
                        case -1: playersTeam.AddRange(RelationshipManager.Instance.FindTeam(Player.currentTeam)?.members?.Where(m => RelationshipManager.FindByID(m) != null)?.Select(m => RelationshipManager.FindByID(m))?.ToList() ?? new List<BasePlayer>()); break;
                        case 0:
                            playersTeam.AddRange(RelationshipManager.Instance.FindTeam(Player.currentTeam)?.members?.Where(m => RelationshipManager.FindByID(m) != null)?.Select(m => RelationshipManager.FindByID(m))?.ToList() ?? new List<BasePlayer>());
                            playersTeam.AddRange(_plugin.GetFriendList(Player)?.ToList() ?? new List<BasePlayer>());
                            break;
                        case 1: playersTeam.AddRange(_plugin.GetFriendList(Player)?.ToList() ?? new List<BasePlayer>()); break;
                    }
                    if (playersTeam.Contains(tpr.Player))
                    {
                        Yes(Player, tpr.Player);
                        return;
                    }
                }
            }
            public Action actionTP = null;
            public Action actionTP_Yes = null;
            public Action<string> actionTP_No = null;

            public TPR tpr;
            public override BasePlayer Player => GetComponent<BasePlayer>();
            public override bool IsToUser => true;
            public void FixedUpdate()
            {
                if (tpr == null || tpr?.Player?.IsDead() != false || Player?.IsDead() != false || CoolDown >= MaxCoolDown)
                {
                    StopTP(GetMessage(Player, MessageInfo.Reason_Cancel));
                    return;
                }
                MessageInfo message;
                if (CanTP(out message)) CoolDown = CoolDown + ((this as TPATimer) ? 0 : Time.fixedDeltaTime);
                else StopTP(GetMessage(Player, message));
            }
            public void StopTP(string reason)
            {
                actionTP_No?.Invoke(reason);
                tpr?.actionTP_No?.Invoke(reason);
                if (tpr != null) UnityEngine.Object.Destroy(tpr);
                UnityEngine.Object.Destroy(this);
            }
            public void StartTP()
            {
                if (IsTimer) return;
                actionTP_Yes?.Invoke();
                tpr?.actionTP_Yes?.Invoke();

                TPATimer timer = gameObject.AddComponent<TPATimer>();
                timer.actionTP = actionTP;
                timer.actionTP_No = actionTP_No;
                timer.actionTP_Yes = actionTP_Yes;
                timer.tpr = tpr;
                tpr.tpa = timer;
                Destroy(this);
            }
        }
        ///<summary>Получатель ТП с таймером</summary>
        public class TPATimer : TPA
        {
            public override bool IsTimer => true;

            private float MaxCoolDown => GetUserTime(tpr.Player);// ConfigData.ToUser_Time.Value;
            private float CoolDown = 0;
            public new void FixedUpdate()
            {
                base.FixedUpdate();
                if (CoolDown >= MaxCoolDown)
                {
                    Teleport();
                    return;
                }
                CoolDown = CoolDown + Time.fixedDeltaTime;
            }
            public void Teleport()
            {
                if (tpr?.Player == null)
                {
                    StopTP(GetMessage(Player, MessageInfo.Reason_Cancel));
                    return;
                }
                DATA.AddTpDay(tpr.Player.UserIDString);
                if (ConfigData.LogPlayerTP.Value) Log.ToFile("PlayersTP", $"[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}] Player {tpr.Player.UserIDString}[{tpr.Player.displayName}] tpr-tpa to player {Player.UserIDString}[{Player.displayName}]");
                if (DATA.TpToPlayer_CD.ContainsKey(tpr.Player.UserIDString)) DATA.TpToPlayer_CD.Remove(tpr.Player.UserIDString);
                float cd = Mathf.Min(ConfigData.ToUser_CDTime.Value.Where(kv => _plugin.permission.UserHasPermission(tpr.Player.UserIDString, kv.Key) || kv.Key == "default").Select(kv => kv.Value).ToArray());
                DATA.TpToPlayer_CD.Add(tpr.Player.UserIDString, DateTime.Now.AddMinutes(cd).Ticks);
                DATA.Save();
                TpController.Teleport(tpr.Player, this.Player.transform.position);
                tpr.actionTP?.Invoke();
                this.actionTP?.Invoke();
                if (tpr != null) UnityEngine.Object.Destroy(tpr);
                UnityEngine.Object.Destroy(this);
            }
        }
        #endregion
        #endregion
        #region PlayerToHome
        #region Commands
        [ChatCommand("sethome")]
        void ChatCommand_SetHome(BasePlayer player, string command, string[] args)
        {
            if (!ConfigData.ToHome_CanTP.Value) return;
            if (args.Count() == 0)
            {
                Message(player, MessageInfo.SetHome_Command_Info);
                return;
            }
            int num = 0;
            if (!int.TryParse(args.FirstOrDefault(), out num))
            {
                Message(player, MessageInfo.SetHome_CantSet_IsntNum);
                return;
            }
            if (!DATA.Home_List.ContainsKey(player.UserIDString)) DATA.Home_List.Add(player.UserIDString, new Dictionary<int, Vector3>());
            if (DATA.GetHome(player.UserIDString, num) != null)
            {
                Message(player, MessageInfo.SetHome_CantSet_Found);
                return;
            }
            if (DATA.Home_List[player.UserIDString].Count() >= Mathf.Max(ConfigData.ToHome_Permissions.Value.Where(kv => permission.UserHasPermission(player.UserIDString, kv.Key) || kv.Key == "default").Select(kv => kv.Value).ToArray()))
            {
                Message(player, MessageInfo.SetHome_CantSet_MaxCount);
                return;
            }
            if (!player.CanBuild())
            {
                Message(player, MessageInfo.SetHome_CantSet_CantBuild);
                return;
            }
            Debug.Log("0.1");
            if (!CanTeleportFoundation(player, player.GetNetworkPosition()))
            {
                Debug.Log("0.2");
                Message(player, MessageInfo.SetHome_CantSet_Foundation);
                Debug.Log("0.3");
                return;
            }
            Debug.Log("0.4");
            DATA.AddHome(player.UserIDString, num, player.transform.position);
            Message(player, MessageInfo.SetHome_Set, new KeyValuePair<string, string>("{home}", num.ToString()));
            DATA.Save();
        }
        [ChatCommand("removehome")]
        void ChatCommand_RemoveHome(BasePlayer player, string command, string[] args)
        {
            if (!ConfigData.ToHome_CanTP.Value) return;
            if (args.Count() == 0)
            {
                Message(player, MessageInfo.DelHome_Command_Info);
                return;
            }
            int num = 0;
            if (!int.TryParse(args.FirstOrDefault(), out num))
            {
                Message(player, MessageInfo.DelHome_CantDel_IsntNum);
                return;
            }
            if (!DATA.Home_List.ContainsKey(player.UserIDString)) DATA.Home_List.Add(player.UserIDString, new Dictionary<int, Vector3>());
            if (DATA.GetHome(player.UserIDString, num) == null)
            {
                Message(player, MessageInfo.DelHome_CantDel_NotFound);
                return;
            }
            DATA.Home_List[player.UserIDString].Remove(num);
            DATA.Save();
            Message(player, MessageInfo.DelHome_Del, new KeyValuePair<string, string>("{home}", string.Join(" ", args)));
            DATA.Save();
        }
        [ChatCommand("home")]
        void ChatCommand_Home(BasePlayer player, string command, string[] args)
        {
            if (!ConfigData.ToHome_CanTP.Value) return;
            if (DATA.TpToHome_CD.ContainsKey(player.UserIDString))
            {
                if (DATA.TpToHome_CD[player.UserIDString] > DateTime.Now.Ticks)
                {
                    Message(player, MessageInfo.CoolDown_Use, new KeyValuePair<string, string>("{sec}", Math.Round(new TimeSpan(DATA.TpToHome_CD[player.UserIDString] - DateTime.Now.Ticks).TotalSeconds, 1).ToString()));
                    return;
                }
            }
            int num = 0;
            if (!int.TryParse(args.FirstOrDefault(), out num))
            {
                Message(player, MessageInfo.TPHome_CantTP_IsntNum);
                return;
            }
            MessageInfo? message = null;
            if (IsTPBlock(player, ref message))
            {
                Debug.Log("TPR.18");
                Message(player, message ?? MessageInfo.TPHome_CantTP_RaidBlock);
                return;
            }

            switch (TPHome.TP(player, num))
            {
                case 0: return;
                case 1: Message(player, MessageInfo.TPHome_CantTP_ActiveTP); return;
                case 2: Message(player, MessageInfo.TPHome_CantTP_NotFound); return;
                case 3: Message(player, MessageInfo.TPHome_CantTP_MaxCountDay); return;
                case 4: Message(player, MessageInfo.TPHome_CantTP_Foundation_NotSet); return;
                case 5: Message(player, MessageInfo.TPHome_CantTP_Economics); return;
            }
        }
        [ChatCommand("homec")]
        void ChatCommand_HomeC(BasePlayer player, string command, string[] args)
        {
            if (!ConfigData.ToHome_CanTP.Value) return;
            switch (TPHome.HOMEC(player))
            {
                case 0: return;
                case 1: Message(player, MessageInfo.HomeC_NotFound); return;
            }
        }
        [ConsoleCommand("tp.sethome")] void ConsoleCommand_SetHome(ConsoleSystem.Arg arg) => PlayerInvoke(arg, (player) => { if (DATA.GetSettings(player.UserIDString, TpSettings.Close_Use)) GUITP.Stop(player); ChatCommand_SetHome(player, "sethome", arg.Args); });
        [ConsoleCommand("tp.removehome")] void ConsoleCommand_RemoveHome(ConsoleSystem.Arg arg) => PlayerInvoke(arg, (player) => { if (DATA.GetSettings(player.UserIDString, TpSettings.Close_Use)) GUITP.Stop(player); ChatCommand_RemoveHome(player, "removehome", arg.Args); });
        [ConsoleCommand("tp.home")] void ConsoleCommand_Home(ConsoleSystem.Arg arg) => PlayerInvoke(arg, (player) => { if (DATA.GetSettings(player.UserIDString, TpSettings.Close_TP) || DATA.GetSettings(player.UserIDString, TpSettings.Close_Use)) GUITP.Stop(player); ChatCommand_Home(player, "home", arg.Args); });
        [ConsoleCommand("tp.homec")] void ConsoleCommand_HomeC(ConsoleSystem.Arg arg) => PlayerInvoke(arg, (player) => { if (DATA.GetSettings(player.UserIDString, TpSettings.Close_TP) || DATA.GetSettings(player.UserIDString, TpSettings.Close_Use)) GUITP.Stop(player); ChatCommand_HomeC(player, "homec", arg.Args); });
        #endregion
        #region Components
        public class TPHome : ITeleport
        {
            /// <summary>Отменить ТП домой</summary>
            /// <param name="player">Отправитель</param>
            /// <exception cref="0 - ТП домой отменено"></exception>
            /// <exception cref="1 - Активных тп нет"></exception>
            public static int HOMEC(BasePlayer player)
            {
                if (player.GetComponents<TPHome>().Count() == 0) return 1;
                foreach (var comp in player.GetComponents<TPHome>()) Destroy(comp);
                Message(player, MessageInfo.HomeC_C);
                return 0;
            }
            /// <summary>ТП домой</summary>
            /// <param name="player">Отправитель</param>
            /// <param name="home">Название дома</param>
            /// <exception cref="0 - ТП домой отправлено"></exception>
            /// <exception cref="1 - Активная телепортация домой"></exception>
            /// <exception cref="2 - Дом не найден!"></exception>
            /// <exception cref="3 - Лимит на день использован"></exception>
            /// <exception cref="4 - Не ваш фунедамент!"></exception>
            public static int TP(BasePlayer player, int home)
            {
                if (!DATA.CanHomeDay(player.UserIDString)) return 3;
                if (player.GetComponents<TPHome>().Count() >= 1) return 1;
                if (DATA.GetHome(player.UserIDString, home) == null) return 2;
                if (!CanTeleportFoundation(player, DATA.GetHome(player.UserIDString, home).Value)) return 4;
                if (!CanUseEconomics(player, ConfigData.ToHome_EconomicsCash.Value, withdraw: true)) return 5;
                TPHome tphome = player.gameObject.AddComponent<TPHome>();
                tphome.home = home;
                return 0;
            }
            public float MaxCoolDown => GetHomeTime(Player);// ConfigData.ToHome_Time.Value;
            public Vector3? Home => DATA.GetHome(Player?.UserIDString, home);
            public int home = -1;
            public override BasePlayer Player => GetComponent<BasePlayer>();
            public override bool IsToUser => false;
            public float CoolDown = 0;
            void Start() => Message(Player, MessageInfo.TPHome_Time, new KeyValuePair<string, string>("{sec}", Math.Round(MaxCoolDown, 2).ToString()));
            public void FixedUpdate()
            {
                if (Home == null || Player?.IsDead() != false || Player?.IsConnected != true)
                {
                    Message(Player, MessageInfo.TPHome_CantTP_NotFound);
                    StopTP();
                    return;
                }
                MessageInfo message;
                if (!CanTP(out message))
                {
                    Message(Player, MessageInfo.TPHome_CantTP_Reason, new KeyValuePair<string, string>("{reason}", GetMessage(Player, message)));
                    StopTP();
                    return;
                }
                if (CoolDown >= MaxCoolDown)
                {
                    Teleport();
                    return;
                }
                lastHP = Mathf.FloorToInt(Player.health);
                CoolDown = CoolDown + Time.fixedDeltaTime;
            }
            public void Teleport()
            {
                DATA.AddHomeDay(Player.UserIDString);
                if (ConfigData.LogPlayerTP.Value) Log.ToFile("HomeTP", $"[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}] Player {Player.UserIDString}[{Player.displayName}] tp to home {home} in position {Home}]");
                if (DATA.TpToHome_CD.ContainsKey(Player.UserIDString)) DATA.TpToHome_CD.Remove(Player.UserIDString);
                float cd = Mathf.Min(ConfigData.ToHome_CDTime.Value.Where(kv => _plugin.permission.UserHasPermission(Player.UserIDString, kv.Key) || kv.Key == "default").Select(kv => kv.Value).ToArray());
                DATA.TpToHome_CD.Add(Player.UserIDString, DateTime.Now.AddMinutes(cd).Ticks);
                DATA.Save();
                TpController.Teleport(Player, Home.Value);
                Message(Player, MessageInfo.TPHome_Time_Now, new KeyValuePair<string, string>("{count}", DATA.GetHomeDay(Player.UserIDString)?.ToString() ?? "∞"));
                UnityEngine.Object.Destroy(this);
            }
            public void StopTP()
            {
                try { CanUseEconomics(Player, ConfigData.ToHome_EconomicsCash.Value, deposit: true); } catch { }
                UnityEngine.Object.Destroy(this);
            }

            // Check balance on multiple plugins and optionally withdraw money from the player
            static bool CanUseEconomics(BasePlayer player, double bypass, bool withdraw = false, bool deposit = false)
            {
                if (!ConfigData.ToHome_Economics.Value) return true;
                if (((double?)_plugin.Economics?.CallHook("Balance", player.UserIDString) ?? 0) < bypass) return false;
                if (withdraw == true) return (bool?)_plugin.Economics?.CallHook("Withdraw", player.userID, bypass) ?? false;
                if (deposit == true) return (bool?)_plugin.Economics?.CallHook("Deposit", player.userID, bypass) ?? false;
                return false;
            }
        }
        #endregion
        #endregion
        #region AdminToAll
        static Dictionary<string, Vector3> tpbacks = new Dictionary<string, Vector3>();
        [ChatCommand("tp")]
        void ChatCommand_TP(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, Perm_TPAdmin)) return;
            switch (args.Count())
            {
                case 1:
                    {
                        List<BasePlayer> player1 = FindPlayers(args[0]);
                        if (player1.Count() == 0)
                        {
                            Message(player, MessageInfo.TPAdmin_NotFound);
                            return;
                        }
                        if (player1.Count() > 1)
                        {
                            Message(player, MessageInfo.TPAdmin_FoundMore);
                            return;
                        }
                        TPAdmin.TPYouToPlayer(player, player1.FirstOrDefault());
                    }
                    return;
                case 2:
                    {
                        List<BasePlayer> player1 = FindPlayers(args[0]);
                        List<BasePlayer> player2 = FindPlayers(args[1]);
                        if (player1.Count() == 0 || player2.Count() == 0)
                        {
                            Message(player, MessageInfo.TPAdmin_NotFound);
                            return;
                        }
                        if (player1.Count() > 1 || player2.Count() > 1)
                        {
                            Message(player, MessageInfo.TPAdmin_FoundMore);
                            return;
                        }
                        TPAdmin.TPPlayerToPlayer(player, player1.FirstOrDefault(), player2.FirstOrDefault());
                    }
                    return;
                case 3:
                    {
                        Vector3 position = new Vector3(0, 0, 0);
                        if (!float.TryParse(args[0], out position.x) || !float.TryParse(args[1], out position.y) || !float.TryParse(args[2], out position.z)) return;
                        TPAdmin.TPYouToPosition(player, position);
                    }
                    return;
                case 4:
                    {
                        List<BasePlayer> player1 = FindPlayers(args[0]);
                        Vector3 position = new Vector3(0, 0, 0);
                        if (player1.Count() == 0)
                        {
                            Message(player, MessageInfo.TPAdmin_NotFound);
                            return;
                        }
                        if (player1.Count() > 1)
                        {
                            Message(player, MessageInfo.TPAdmin_FoundMore);
                            return;
                        }
                        if (!float.TryParse(args[1], out position.x) || !float.TryParse(args[2], out position.y) || !float.TryParse(args[3], out position.z)) return;
                        TPAdmin.TPPlayerToPosition(player, player1.FirstOrDefault(), position);
                    }
                    return;
            }
        }
        [ChatCommand("stp")]
        void ChatCommand_STP(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, Perm_TPAdmin)) return;
            switch (args.Count())
            {
                case 1:
                    {
                        List<BasePlayer> player1 = FindSleepers(args[0]);
                        if (player1.Count() == 0)
                        {
                            Message(player, MessageInfo.TPAdmin_NotFound);
                            return;
                        }
                        if (player1.Count() > 1)
                        {
                            Message(player, MessageInfo.TPAdmin_FoundMore);
                            return;
                        }
                        TPAdmin.TPYouToPlayer(player, player1.FirstOrDefault());
                    }
                    return;
                case 2:
                    {
                        List<BasePlayer> player1 = FindSleepers(args[0]);
                        List<BasePlayer> player2 = FindPlayers(args[1]);
                        if (player1.Count() == 0 || player2.Count() == 0)
                        {
                            Message(player, MessageInfo.TPAdmin_NotFound);
                            return;
                        }
                        if (player1.Count() > 1 || player2.Count() > 1)
                        {
                            Message(player, MessageInfo.TPAdmin_FoundMore);
                            return;
                        }
                        TPAdmin.TPPlayerToPlayer(player, player1.FirstOrDefault(), player2.FirstOrDefault());
                    }
                    return;
            }
        }
        [ChatCommand("tpb")]
        void ChatCommand_Back(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, Perm_TPAdmin)) return;
            if (!tpbacks.ContainsKey(player.UserIDString))
            {
                player.ChatMessage("Back not found!");
                return;
            }
            TPAdmin.TPYouToPosition(player, tpbacks[player.UserIDString]);
        }
        public static class TPAdmin
        {
            /// <summary>
            /// [1]YOU PLAYER
            /// </summary>
            /// <param name="you"></param>
            /// <param name="player"></param>
            public static void TPYouToPlayer(BasePlayer you, BasePlayer player)
            {
                if (ConfigData.LogAdminTP.Value) Log.ToFile("AdminTP", $"[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}] Admin {you.UserIDString}[{you.displayName}] tp to player {player.UserIDString}[{player.displayName}]");
                tpbacks[you.UserIDString] = you.transform.position;
                TpController.Teleport(you, player.transform.position);
                Message(you, MessageInfo.TPAdmin_TP, new KeyValuePair<string, string>("{val}", $"\"Игрок {player.UserIDString}[{player.displayName}]\""));
            }
            /// <summary>
            /// [2]YOU PLAYER1 PLAYER2
            /// </summary>
            /// <param name="you"></param>
            /// <param name="player1"></param>
            /// <param name="player2"></param>
            public static void TPPlayerToPlayer(BasePlayer you, BasePlayer player1, BasePlayer player2)
            {
                if (ConfigData.LogAdminTP.Value) Log.ToFile("AdminTP", $"[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}] Admin {you.UserIDString}[{you.displayName}] tp player {player1.UserIDString}[{player1.displayName}] to player {player2.UserIDString}[{player2.displayName}]");
                TpController.Teleport(player1, player2.transform.position);
                Message(player1, MessageInfo.TPAdmin_TP, new KeyValuePair<string, string>("{val}", $"\"Игрок {player2.UserIDString}[{player2.displayName}]\""));
                Message(you, MessageInfo.TPAdmin_TP_Player, new KeyValuePair<string, string>("{val}", $"\"Игрок {player2.UserIDString}[{player2.displayName}]\""), new KeyValuePair<string, string>("{player}", $"{player1.UserIDString}[{player1.displayName}]"));
            }
            /// <summary>
            /// [3]YOU POSX POSY POSZ
            /// </summary>
            /// <param name="you"></param>
            /// <param name="position"></param>
            public static void TPYouToPosition(BasePlayer you, Vector3 position)
            {
                if (ConfigData.LogAdminTP.Value) Log.ToFile("AdminTP", $"[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}] Admin {you.UserIDString}[{you.displayName}] tp to position [{position.x}, {position.y}, {position.z}]");
                tpbacks[you.UserIDString] = position;
                TpController.Teleport(you, position);
                Message(you, MessageInfo.TPAdmin_TP, new KeyValuePair<string, string>("{val}", $"\"Позиция [{position.x}, {position.y}, {position.z}]\""));
            }
            /// <summary>
            /// [4]YOU PLAYER1 POSX POSY POSZ
            /// </summary>
            /// <param name="you"></param>
            /// <param name="player1"></param>
            /// <param name="position"></param>
            public static void TPPlayerToPosition(BasePlayer you, BasePlayer player1, Vector3 position)
            {
                if (ConfigData.LogAdminTP.Value) Log.ToFile("AdminTP", $"[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}] Admin {you.UserIDString}[{you.displayName}] tp player {player1.UserIDString}[{player1.displayName}] to position [{position.x}, {position.y}, {position.z}]");
                TpController.Teleport(player1, position);
                Message(player1, MessageInfo.TPAdmin_TP, new KeyValuePair<string, string>("{val}", $"\"Позиция [{position.x}, {position.y}, {position.z}]\""));
                Message(you, MessageInfo.TPAdmin_TP_Player, new KeyValuePair<string, string>("{val}", $"\"Позиция [{position.x}, {position.y}, {position.z}]\""), new KeyValuePair<string, string>("{player}", $"{player1.UserIDString}[{player1.displayName}]"));
            }
        }
        #endregion
        #region DATA
        public static class DATA
        {
            public static void Load()
            {
                TpToHome_CD = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, long>>($"{_plugin.Title}/CoolDown.Home");
                TpToPlayer_CD = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, long>>($"{_plugin.Title}/CoolDown.Player");
                Home_List = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, Dictionary<int, Vector3>>>($"{_plugin.Title}/List.Home");
                TpToPlayer_Count = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, KeyValuePair<long, int>>>($"{_plugin.Title}/CountTp.Player");
                TpToHome_Count = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, KeyValuePair<long, int>>>($"{_plugin.Title}/CountTp.Home");
                Player_Settings = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, Dictionary<string, bool>>>($"{_plugin.Title}/Settings.Player");
            }
            public static void AddHome(string SteamID, int Home, Vector3 position)
            {
                if (!DATA.Home_List.ContainsKey(SteamID)) DATA.Home_List.Add(SteamID, new Dictionary<int, Vector3>());
                Home_List[SteamID].Add(Home, position);
                DATA.Save();
            }

            public static Vector3? GetHome(string SteamID, int Num) => Home_List.ContainsKey(SteamID) ? (Home_List[SteamID].ContainsKey(Num) ? (Vector3?)Home_List[SteamID][Num] : null) : null;
            public static Dictionary<int, Vector3> GetHomes(string SteamID) => Home_List.ContainsKey(SteamID) ? Home_List[SteamID] : new Dictionary<int, Vector3>();
            static int GetCountDay(Dictionary<string, int> values, string SteamID) => Mathf.Max(values.Where(kv => _plugin.permission.UserHasPermission(SteamID, kv.Key) || kv.Key == "default").Select(kv => kv.Value).ToArray());
            public static bool CanTpDay(string SteamID) =>
                GetCountDay(ConfigData.ToUser_CountDay.Value, SteamID) == -1 ?
                    true
                    :
                    TpToPlayer_Count.ContainsKey(SteamID) ?
                        new DateTime(TpToPlayer_Count[SteamID].Key).Day == DateTime.Now.Day ?
                            TpToPlayer_Count[SteamID].Value < GetCountDay(ConfigData.ToUser_CountDay.Value, SteamID)
                            :
                            true
                        :
                        true;
            public static void AddTpDay(string SteamID)
            {
                if (GetCountDay(ConfigData.ToUser_CountDay.Value, SteamID) == -1) return;
                KeyValuePair<long, int> value = new KeyValuePair<long, int>(DateTime.MinValue.Ticks, 0);
                if (TpToPlayer_Count.ContainsKey(SteamID))
                {
                    value = TpToPlayer_Count[SteamID];
                    TpToPlayer_Count.Remove(SteamID);
                }
                value = (new DateTime(value.Key).Day == DateTime.Now.Day) ? new KeyValuePair<long, int>(DateTime.Now.Ticks, value.Value + 1) : new KeyValuePair<long, int>(DateTime.Now.Ticks, 0);
                TpToPlayer_Count.Add(SteamID, value);
                DATA.Save();
            }
            public static bool CanHomeDay(string SteamID) => GetCountDay(ConfigData.ToHome_CountDay.Value, SteamID) == -1 ? true : TpToHome_Count.ContainsKey(SteamID) ? new DateTime(TpToHome_Count[SteamID].Key).Day == DateTime.Now.Day ? TpToHome_Count[SteamID].Value < GetCountDay(ConfigData.ToHome_CountDay.Value, SteamID) : true : true;
            public static void AddHomeDay(string SteamID)
            {
                if (GetCountDay(ConfigData.ToHome_CountDay.Value, SteamID) == -1) return;
                KeyValuePair<long, int> value = new KeyValuePair<long, int>(DateTime.MinValue.Ticks, 0);
                if (TpToHome_Count.ContainsKey(SteamID))
                {
                    value = TpToHome_Count[SteamID];
                    TpToHome_Count.Remove(SteamID);
                }
                value = (new DateTime(value.Key).Day == DateTime.Now.Day) ? new KeyValuePair<long, int>(DateTime.Now.Ticks, value.Value + 1) : new KeyValuePair<long, int>(DateTime.Now.Ticks, 0);
                TpToHome_Count.Add(SteamID, value);
                DATA.Save();
            }
            public static int? GetHomeDay(string SteamID)
            {
                if (GetCountDay(ConfigData.ToHome_CountDay.Value, SteamID) == -1) return null;
                if (TpToHome_Count.ContainsKey(SteamID) && new DateTime(TpToHome_Count[SteamID].Key).Day == DateTime.Now.Day) return GetCountDay(ConfigData.ToHome_CountDay.Value, SteamID) - TpToHome_Count[SteamID].Value;
                return GetCountDay(ConfigData.ToHome_CountDay.Value, SteamID);
            }
            public static int? GetEndTpDay(string SteamID)
            {
                if (GetCountDay(ConfigData.ToUser_CountDay.Value, SteamID) == -1) return null;
                if (TpToPlayer_Count.ContainsKey(SteamID) && new DateTime(TpToPlayer_Count[SteamID].Key).Day == DateTime.Now.Day) return GetCountDay(ConfigData.ToUser_CountDay.Value, SteamID) - TpToPlayer_Count[SteamID].Value;
                return GetCountDay(ConfigData.ToUser_CountDay.Value, SteamID);
            }
            public static bool GetSettings(string SteamID, TpSettings setting) => Player_Settings.ContainsKey(SteamID) ? Player_Settings[SteamID].ContainsKey(setting.ToString()) ? Player_Settings[SteamID][setting.ToString()] : false : false;
            public static void SetSettings(string SteamID, TpSettings setting, bool value)
            {
                if (setting == TpSettings.None) return;
                if (!Player_Settings.ContainsKey(SteamID)) Player_Settings.Add(SteamID, new Dictionary<string, bool>());
                if (Player_Settings[SteamID].ContainsKey(setting.ToString())) Player_Settings[SteamID].Remove(setting.ToString());
                Player_Settings[SteamID].Add(setting.ToString(), value);
                Save();
            }
            public static Dictionary<string, long> TpToHome_CD = new Dictionary<string, long>();
            public static Dictionary<string, Dictionary<int, Vector3>> Home_List = new Dictionary<string, Dictionary<int, Vector3>>();
            public static Dictionary<string, long> TpToPlayer_CD = new Dictionary<string, long>();
            public static Dictionary<string, KeyValuePair<long, int>> TpToPlayer_Count = new Dictionary<string, KeyValuePair<long, int>>();
            public static Dictionary<string, KeyValuePair<long, int>> TpToHome_Count = new Dictionary<string, KeyValuePair<long, int>>();
            public static Dictionary<string, Dictionary<string, bool>> Player_Settings = new Dictionary<string, Dictionary<string, bool>>();
            public static void Save()
            {
                Interface.GetMod().DataFileSystem.WriteObject($"{_plugin.Title}/CoolDown.Home", TpToHome_CD);
                Interface.GetMod().DataFileSystem.WriteObject($"{_plugin.Title}/CoolDown.Player", TpToPlayer_CD);
                Interface.GetMod().DataFileSystem.WriteObject($"{_plugin.Title}/List.Home", Home_List);
                Interface.GetMod().DataFileSystem.WriteObject($"{_plugin.Title}/CountTp.Player", TpToPlayer_Count);
                Interface.GetMod().DataFileSystem.WriteObject($"{_plugin.Title}/CountTp.Home", TpToHome_Count);
                Interface.GetMod().DataFileSystem.WriteObject($"{_plugin.Title}/Settings.Player", Player_Settings);
            }
        }
        #endregion
        [ChatCommand("tpmenu")] void ChatCommand_TpMenu(BasePlayer player, string command, string[] args) => GUITP.StartOrStop(player, args.Count() > 0 ? args.FirstOrDefault() : null);
        [ConsoleCommand("tp.menu")] void ConsoleCommand_Use_Tp_Menu(ConsoleSystem.Arg arg) => PlayerInvoke(arg, (player) => { GUITP.StartOrStop(player, arg.HasArgs() ? arg.Args.FirstOrDefault() : null); });
        [ConsoleCommand("tp.set.settings")] void ConsoleCommand_Tp_Set_Settings(ConsoleSystem.Arg arg) => PlayerInvoke(arg, (player) => { DATA.SetSettings(player.UserIDString, Settings(string.Join(" ", arg.Args)), !DATA.GetSettings(player.UserIDString, Settings(string.Join(" ", arg.Args)))); });
        [ConsoleCommand("tp.info")] void ConsoleCommand_Tp_Info(ConsoleSystem.Arg arg) => PlayerInvoke(arg, (player) => { Info.Send(player, string.Join(" ", arg.Args), 4); });
        static class Info
        {
            static Dictionary<string, Timer> players = new Dictionary<string, Timer>();
            public static void Send(BasePlayer player, string message, float sec)
            {
                if (players.ContainsKey(player.UserIDString))
                {
                    players[player.UserIDString].Destroy();
                    players.Remove(player.UserIDString);
                }
                player.SendConsoleCommand("gametip.showgametip", message);
                players.Add(player.UserIDString, _plugin.timer.Once(sec, () => { player?.SendConsoleCommand("gametip.hidegametip"); players.Remove(player.UserIDString); }));
            }
        }
        //Закрыть меню после принятия/отклонения/отправки запроса
        //Закрыть меню после каждого действия
        //Принимать запросы только от друзей
        //Автоматически принимать запросы от друзей
        public enum TpSettings
        {
            None,
            Close_TP,
            Close_Use,
            Whitelist_Fr,
            AutoTPA_Fr
        }
        public TpSettings Settings(string settings)
        {
            Debug.Log(settings.ToLower());
            switch (settings.ToLower())
            {
                case "close_tp": return TpSettings.Close_TP;
                case "close_use": return TpSettings.Close_Use;
                case "whitelist_fr": return TpSettings.Whitelist_Fr;
                case "autotpa_fr": return TpSettings.AutoTPA_Fr;
            }
            Debug.Log("**" + settings.ToLower());
            return TpSettings.None;
        }
        static string GetDictSettings(TpSettings tpSettings, BasePlayer player, bool IsTitle) => GetMessage(player, tpSettings, IsTitle);
        public class InfoMessage
        {
            public static Dictionary<string, List<Timer>> timers = new Dictionary<string, List<Timer>>();
            static void UpdateTimeMessage(BasePlayer player, string message, float acolor = 1)
            {
                CuiElementContainer cuis = UI.CreateElementContainer(_GUI_TP_Info, "0.254717 0.254717 0.254717 " + (acolor * 0.3921569f).ToString(), new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 279), new Vector2(0, 309), false);
                new UIPanel("0.254717 0.254717 0.254717 " + (acolor * 0.3921569f).ToString(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200, -15), new Vector2(200, 15), false).Add(cuis, _GUI_TP_Info);
                new UILabel("1 1 1 " + (acolor * 1).ToString(), message, 15, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-500, -15), new Vector2(500, 15), TextAnchor.MiddleCenter).Add(cuis, _GUI_TP_Info);
                CuiHelper.DestroyUi(player, _GUI_TP_Info);
                CuiHelper.AddUi(player, cuis);

            }
            public static void Send(BasePlayer player, string message, float second)
            {
                if (timers.ContainsKey(player.UserIDString))
                {
                    foreach (var timer in timers[player.UserIDString])
                        timer.Destroy();
                    timers.Remove(player.UserIDString);
                }
                timers.Add(player.UserIDString, new List<Timer>());
                float timeDes = (second - 1f) < 0 ? 0 : (second - 1f);
                float u = 0;
                timers[player.UserIDString].Add(_plugin.timer.Repeat(0.04f, 25, () => { UpdateTimeMessage(player, message, u); u = u + 0.04f; }));
                timers[player.UserIDString].Add(_plugin.timer.Once(timeDes + 1f, () => { u = 1f; timers[player.UserIDString].Add(_plugin.timer.Repeat(0.04f, 25, () => { UpdateTimeMessage(player, message, u); u = u - 0.04f; })); timers[player.UserIDString].Add(_plugin.timer.Once(1.2f, () => CuiHelper.DestroyUi(player, _GUI_TP_Info))); }));
            }
        }
        public class GUITP : MonoBehaviour
        {
            BasePlayer Player => GetComponent<BasePlayer>();
            public static void StartOrStop(BasePlayer player, string page = null)
            {
                if (page == null)
                {
                    if (player.GetComponent<GUITP>() == null) player.gameObject.AddComponent<GUITP>();
                    else player.GetComponent<GUITP>().Stop();
                }
                else
                {
                    if (player.GetComponent<GUITP>() == null) player.gameObject.AddComponent<GUITP>();
                    switch (page.ToLower())
                    {
                        case "label": player.gameObject.GetComponent<GUITP>().page = PageType.Label; return;
                        case "pagefriends": player.gameObject.GetComponent<GUITP>().page = PageType.PageFriends; return;
                        case "settings": player.gameObject.GetComponent<GUITP>().page = PageType.Settings; return;
                        case "pageclan": player.gameObject.GetComponent<GUITP>().page = PageType.PageClan; return;
                    }
                }
            }
            public static void Stop(BasePlayer player) => player?.GetComponent<GUITP>()?.Stop();
            void Start()
            {
                lastData = RecreateGUILast(Player, lastData, page);
            }
            public PageType page = PageType.Label;
            public enum PageType
            {
                Label,
                PageFriends,
                Settings,
                PageClan,
            }

            void Stop()
            {
                CD = 1000;
                CuiHelper.DestroyUi(Player, _GUI_TP_Menu);
                CuiHelper.DestroyUi(Player, _GUI_TP_Button);
                Destroy(this);
            }
            float CD = 0;
            void FixedUpdate()
            {
                if (CD <= 0)
                {
                    lastData = RecreateGUILast(Player, lastData, page);
                    CD = 0.1f;
                }
                CD = CD - Time.fixedDeltaTime;
            }
            LastData lastData = null;
            static Vector2 GetPosition(float grade, float radius) => new Vector2(radius * Mathf.Cos(grade * 3.14f / 180), radius * Mathf.Sin(grade * 3.14f / 180));
            static LastData RecreateGUILast(BasePlayer player, LastData lastData, PageType page)
            {
                string Json = "{DarkPluginsID}";
                CuiElementContainer cuis = UI.CreateElementContainer(_GUI_TP_Button, "0 0 0 0", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f), true);
                new UIButton("1 1 1 0", "", $"tp.menu", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-640f, -360f), new Vector2(640f, 360f)).Add(cuis, _GUI_TP_Button);
                string Json1 = cuis.ToJson();
                cuis = UI.CreateElementContainer(_GUI_TP_Menu, "0 0 0 0", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-150f, -150f), new Vector2(150f, 150f), true, 0, 0);

                List<object> components = new List<object>();
                LastData newData = null;
                switch (page)
                {
                    case PageType.Label:
                        {
                            if (ConfigData.FriendsKey.Value != -2)
                            {
                                if (_plugin.Friends ?? _plugin.FriendSystem != null) components.Add(new CustomGUI("FriendTP", "tp.menu pagefriends", "0 0 0 0", ConfigData.UI_Page_Color.Value));
                                if (_plugin.Clans != null) components.Add(new CustomGUI("ClanTP", "tp.menu pageclan", "0 0 0 0", ConfigData.UI_Page_Color.Value));
                            }
                            List<int> tpHomes = player.GetComponents<TPHome>().Select(home => home.home).ToList();
                            for (int i = 1; i < Mathf.Max(ConfigData.ToHome_Permissions.Value.Where(kv => _plugin.permission.UserHasPermission(player.UserIDString, kv.Key) || kv.Key == "default").Select(kv => kv.Value).ToArray()) + 1; i++)
                                components.Add(DATA.GetHome(player.UserIDString, i) != null ? new KeyValuePair<bool, KeyValuePair<int, Vector3>>(tpHomes.Contains(i), new KeyValuePair<int, Vector3>(i, DATA.GetHome(player.UserIDString, i).Value)) : (object)i);
                            components.AddRange(player.GetComponents<TPA>());
                            components.AddRange(player.GetComponents<TPR>());
                            newData = new LastData(components.Count(), player.GetComponents<TPA>().Count(), player.GetComponents<TPR>().Count(), tpHomes.Count(), DATA.GetHomes(player.UserIDString).Count());
                            components.Add(new CustomGUI("Settigns", "tp.menu settings", ConfigData.UI_Page_Image_Color.Value, ConfigData.UI_Page_Color.Value, "assets/icons/gear.png"));
                        }
                        break;
                    case PageType.PageFriends:
                        {
                            components.Add(new CustomGUI("", "tp.menu label", ConfigData.UI_Page_Image_Color.Value, ConfigData.UI_Page_Color.Value, "assets/icons/exit.png"));
                            List<BasePlayer> playersFriend = new List<BasePlayer>();
                            switch (ConfigData.FriendsKey.Value)
                            {
                                case -2: break;
                                case -1: playersFriend.AddRange(RelationshipManager.Instance.FindTeam(player.currentTeam)?.members?.Where(m => RelationshipManager.FindByID(m) != null)?.Select(m => RelationshipManager.FindByID(m))?.ToList() ?? new List<BasePlayer>()); break;
                                case 0:
                                    playersFriend.AddRange(RelationshipManager.Instance.FindTeam(player.currentTeam)?.members?.Where(m => RelationshipManager.FindByID(m) != null)?.Select(m => RelationshipManager.FindByID(m))?.ToList() ?? new List<BasePlayer>());
                                    playersFriend.AddRange(_plugin.GetFriends(player)?.Where(v => v.Key == 0)?.Select(v => v.Value)?.ToList() ?? new List<BasePlayer>());
                                    break;
                                case 1: playersFriend.AddRange(_plugin.GetFriends(player)?.Where(v => v.Key == 0)?.Select(v => v.Value)?.ToList() ?? new List<BasePlayer>()); break;
                            }
                            playersFriend = playersFriend.Where(m => m != player && m.IsConnected).Distinct().ToList();
                            Dictionary<string, TPR> TPList = player.GetComponents<TPR>().Where(pl => playersFriend.Contains(pl.Player)).ToDictionary(dict => dict.Player.UserIDString, dict => dict);
                            Foreach(playersFriend, (playerTeam) => components.Add(TPList.ContainsKey(playerTeam.UserIDString) ? TPList[playerTeam.UserIDString] : (object)new CustomGUI(playerTeam.displayName, $"tp.tpr {playerTeam.UserIDString}", "0 0 0 0", ConfigData.UI_Friend_Color.Value)));
                            if (playersFriend.Count() == 0) components.Add(new CustomGUI("Друзей не найдено!", "", "0 0 0 0", ConfigData.UI_Friend_Color.Value));
                            newData = new LastData(components.Count(), playersFriend.Count(), TPList.Count());
                        }
                        break;
                    case PageType.PageClan:
                        {
                            components.Add(new CustomGUI("", "tp.menu label", ConfigData.UI_Page_Image_Color.Value, ConfigData.UI_Page_Color.Value, "assets/icons/exit.png"));
                            List<BasePlayer> playersClan = new List<BasePlayer>();
                            switch (ConfigData.FriendsKey.Value)
                            {
                                case -2: break;
                                case -1: break;
                                case 0: playersClan.AddRange(_plugin.GetFriends(player)?.Where(v => v.Key == 1)?.Select(v => v.Value)?.ToList() ?? new List<BasePlayer>()); break;
                                case 1: playersClan.AddRange(_plugin.GetFriends(player)?.Where(v => v.Key == 1)?.Select(v => v.Value)?.ToList() ?? new List<BasePlayer>()); break;
                            }
                            Json += $"{playersClan?.Count ?? -1}";
                            playersClan = playersClan.Where(m => m != player && m.IsConnected).ToList();
                            Dictionary<string, TPR> TPList = player.GetComponents<TPR>().Where(pl => playersClan.Contains(pl.Player)).ToDictionary(dict => dict.Player.UserIDString, dict => dict);
                            Foreach(playersClan, (playerTeam) => { components.Add(TPList.ContainsKey(playerTeam.UserIDString) ? TPList[playerTeam.UserIDString] : (object)new CustomGUI(playerTeam.displayName, $"tp.tpr {playerTeam.UserIDString}", "0 0 0 0", ConfigData.UI_Friend_Color.Value)); });
                            if (playersClan.Count() == 0) components.Add(new CustomGUI("Друзей не найдено!", "", "0 0 0 0", ConfigData.UI_Friend_Color.Value));
                            newData = new LastData(components.Count(), playersClan.Count(), TPList.Count());
                        }
                        break;
                    case PageType.Settings:
                        {
                            components.Add(new CustomGUI("", "tp.menu label", ConfigData.UI_Page_Image_Color.Value, ConfigData.UI_Page_Color.Value, "assets/icons/exit.png"));
                            components.Add(new KeyValuePair<TpSettings, bool>(TpSettings.AutoTPA_Fr, DATA.GetSettings(player.UserIDString, TpSettings.AutoTPA_Fr)));
                            components.Add(new KeyValuePair<TpSettings, bool>(TpSettings.Close_TP, DATA.GetSettings(player.UserIDString, TpSettings.Close_TP)));
                            components.Add(new KeyValuePair<TpSettings, bool>(TpSettings.Close_Use, DATA.GetSettings(player.UserIDString, TpSettings.Close_Use)));
                            components.Add(new KeyValuePair<TpSettings, bool>(TpSettings.Whitelist_Fr, DATA.GetSettings(player.UserIDString, TpSettings.Whitelist_Fr)));
                            newData = new LastData(components.Count(), DATA.GetSettings(player.UserIDString, TpSettings.AutoTPA_Fr) ? 1 : 0, DATA.GetSettings(player.UserIDString, TpSettings.Close_TP) ? 1 : 0, DATA.GetSettings(player.UserIDString, TpSettings.Close_Use) ? 1 : 0, DATA.GetSettings(player.UserIDString, TpSettings.Whitelist_Fr) ? 1 : 0);
                        }
                        break;
                }

                float Matrix = 360f / components.Count();
                for (int i = 0; i < components.Count(); i++)
                {
                    Vector2 vector2 = GetPosition(Matrix * i, ConfigData.UI_Radius.Value * GLOBAL_UI_SIZE);
                    object current = components[i];
                    if (current.GetType() == typeof(KeyValuePair<bool, KeyValuePair<int, Vector3>>)) CreateGUI_Home(cuis, _GUI_TP_Menu, vector2, ((KeyValuePair<bool, KeyValuePair<int, Vector3>>)current).Value.Key.ToString(), ((KeyValuePair<bool, KeyValuePair<int, Vector3>>)current).Key, ((KeyValuePair<bool, KeyValuePair<int, Vector3>>)current).Key ? ConfigData.UI_Home_TP_Color.Value : ConfigData.UI_Home_Get_Color.Value, GLOBAL_UI_SIZE);
                    if (current.GetType() == typeof(int)) CreateGUI_SetHome(cuis, _GUI_TP_Menu, vector2, current.ToString(), ConfigData.UI_Home_Set_Color.Value, GLOBAL_UI_SIZE);
                    if (current.GetType() == typeof(TPA) || current.GetType() == typeof(TPATimer)) CreateGUI_TPA(cuis, _GUI_TP_Menu, vector2, (current as TPA).tpr.Player.displayName, ConfigData.UI_TPA_Color.Value, GLOBAL_UI_SIZE);
                    if (current.GetType() == typeof(TPR)) CreateGUI_TPR(cuis, _GUI_TP_Menu, vector2, (current as TPR).tpa.Player.displayName, ConfigData.UI_TPR_Color.Value, GLOBAL_UI_SIZE);
                    if (current.GetType() == typeof(CustomGUI)) CreateCustomGUI(cuis, _GUI_TP_Menu, vector2, (CustomGUI)current, GLOBAL_UI_SIZE);
                    if (current.GetType() == typeof(KeyValuePair<TpSettings, bool>)) CreateGUI_Settings(cuis, _GUI_TP_Menu, vector2, ((KeyValuePair<TpSettings, bool>)current).Key, ((KeyValuePair<TpSettings, bool>)current).Value, ConfigData.UI_Settings_Color.Value, GLOBAL_UI_SIZE, player);
                }

                string Json2 = cuis.ToJson();
                if (newData == lastData) return newData;

                CuiHelper.DestroyUi(player, _GUI_TP_Menu);
                CuiHelper.DestroyUi(player, _GUI_TP_Button);
                CuiHelper.AddUi(player, Json1);
                CuiHelper.AddUi(player, Json2);
                return newData;
            }
            class LastData
            {
                public int[] Data;
                public LastData(params int[] data) { Data = data; }

                public override bool Equals(object obj) => base.Equals(obj);
                public override int GetHashCode() => base.GetHashCode();

                public static bool operator ==(LastData data1, LastData data2)
                {
                    if (data1?.Data == null || data2?.Data == null) return false;
                    if (data1.Data.Count() != data2.Data.Count()) return false;
                    for (int i = 0; i < data1.Data.Count(); i++)
                        if (data1.Data[i] != data2.Data[i])
                            return false;
                    return true;
                }
                public static bool operator !=(LastData data1, LastData data2)
                {
                    if (data1?.Data == null || data2?.Data == null) return true;
                    if (data1.Data.Count() != data2.Data.Count()) return true;
                    for (int i = 0; i < data1.Data.Count(); i++)
                        if (data1.Data[i] != data2.Data[i])
                            return true;
                    return false;
                }
            }

            static void CreateGUI_Home(CuiElementContainer cuis, string gui, Vector2 position, string home, bool IsTp, string bcolor, float UI_SIZE)
            {
                new UIPanel(bcolor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + -25f * UI_SIZE, position.y + -25f * UI_SIZE), new Vector2(position.x + 25f * UI_SIZE, position.y + 25f * UI_SIZE), true, 0, 0, "assets/icons/facebook.png").Add(cuis, gui);
                new UIButton(IsTp ? ConfigData.UI_Home_TP_On_Image_Color.Value : ConfigData.UI_Home_TP_Off_Image_Color.Value, $"{(home.Count() > 7 ? $"{home.Remove(6)}.." : home)}", $"tp.home {home}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + -25f * UI_SIZE, position.y + -25f * UI_SIZE), new Vector2(position.x + 25f * UI_SIZE, position.y + 25f * UI_SIZE), 10, TextAnchor.MiddleCenter, "1 1 1 1", 0, 0, "assets/icons/sleepingbag.png").Add(cuis, gui);
                if (IsTp) new UIButton(ConfigData.UI_Yes_Image_Color.Value, "", $"tp.homec {home}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + -15f * UI_SIZE, position.y + 10f * UI_SIZE), new Vector2(position.x + 15f * UI_SIZE, position.y + 40f * UI_SIZE), 10, TextAnchor.MiddleCenter, "0 0 0 1", 0, 0, "assets/icons/vote_down.png").Add(cuis, gui);
                new UIButton(ConfigData.UI_No_Image_Color.Value, "", $"tp.removehome {home}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + 12.5f * UI_SIZE, position.y + 12.5f * UI_SIZE), new Vector2(position.x + 25f * UI_SIZE, position.y + 25f * UI_SIZE), 14, TextAnchor.MiddleCenter, "0 0 0 1", 0, 0, "assets/icons/vote_down.png").Add(cuis, gui);
            }
            static void CreateGUI_SetHome(CuiElementContainer cuis, string gui, Vector2 position, string home, string bcolor, float UI_SIZE)
            {
                new UIPanel(bcolor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + -25f * UI_SIZE, position.y + -25f * UI_SIZE), new Vector2(position.x + 25f * UI_SIZE, position.y + 25f * UI_SIZE), true, 0, 0, "assets/icons/facebook.png").Add(cuis, gui);
                new UIButton(ConfigData.UI_Home_Set_Image_Color.Value, $"{(home.Count() > 7 ? $"{home.Remove(6)}.." : home)}", $"tp.sethome {home}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + -20 * UI_SIZE, position.y + -20 * UI_SIZE), new Vector2(position.x + 20 * UI_SIZE, position.y + 20 * UI_SIZE), 10, TextAnchor.MiddleCenter, "1 1 1 1", 0, 0, "assets/icons/add.png").Add(cuis, gui);
            }
            static void CreateGUI_TPA(CuiElementContainer cuis, string gui, Vector2 position, string Name, string bcolor, float UI_SIZE)
            {
                new UIPanel(bcolor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + -25f * UI_SIZE, position.y + -25f * UI_SIZE), new Vector2(position.x + 25f * UI_SIZE, position.y + 25f * UI_SIZE), true, 0, 0, "assets/icons/facebook.png").Add(cuis, gui);
                new UILabel("1 1 1 1", Name, 10, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + -25f * UI_SIZE, position.y + -25f * UI_SIZE), new Vector2(position.x + 25f * UI_SIZE, position.y + 25f * UI_SIZE), TextAnchor.MiddleCenter).Add(cuis, gui);
                new UIButton(ConfigData.UI_No_Image_Color.Value, "", $"tp.tpc {Name}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + -15f * UI_SIZE, position.y + 5f * UI_SIZE), new Vector2(position.x + 15f * UI_SIZE, position.y + 35f * UI_SIZE), 10, TextAnchor.MiddleCenter, "0 0 0 1", 0, 0, "assets/icons/vote_down.png").Add(cuis, gui);
                new UIButton(ConfigData.UI_Yes_Image_Color.Value, "", $"tp.tpa {Name}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + -15f * UI_SIZE, position.y + -35f * UI_SIZE), new Vector2(position.x + 15f * UI_SIZE, position.y + -5f * UI_SIZE), 10, TextAnchor.MiddleCenter, "0 0 0 1", 0, 0, "assets/icons/vote_up.png").Add(cuis, gui);
            }
            static void CreateGUI_TPR(CuiElementContainer cuis, string gui, Vector2 position, string Name, string bcolor, float UI_SIZE)
            {
                new UIPanel(bcolor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + -25f * UI_SIZE, position.y + -25f * UI_SIZE), new Vector2(position.x + 25f * UI_SIZE, position.y + 25f * UI_SIZE), true, 0, 0, "assets/icons/facebook.png").Add(cuis, gui);
                new UILabel("1 1 1 1", Name, 10, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + -25f * UI_SIZE, position.y + -25f * UI_SIZE), new Vector2(position.x + 25f * UI_SIZE, position.y + 25f * UI_SIZE), TextAnchor.MiddleCenter).Add(cuis, gui);
                new UIButton(ConfigData.UI_No_Image_Color.Value, "", $"tp.tpc {Name}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + -15f * UI_SIZE, position.y + 5f * UI_SIZE), new Vector2(position.x + 15f * UI_SIZE, position.y + 35f * UI_SIZE), 10, TextAnchor.MiddleCenter, "0 0 0 1", 0, 0, "assets/icons/vote_down.png").Add(cuis, gui);
            }
            struct CustomGUI
            {
                public string Text;
                public string Command;
                public string Sprite;
                public string ButColor;
                public string BColor;
                public CustomGUI(string Text, string Command, string ButColor, string BColor, string Sprite = null)
                {
                    this.Text = Text;
                    this.Command = Command;
                    this.Sprite = Sprite;
                    this.ButColor = ButColor;
                    this.BColor = BColor;
                }
            }
            static void CreateGUI_Settings(CuiElementContainer cuis, string gui, Vector2 position, TpSettings settings, bool value, string bcolor, float UI_SIZE, BasePlayer player)
            {
                new UIPanel(bcolor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + -25f * UI_SIZE, position.y + -25f * UI_SIZE), new Vector2(position.x + 25f * UI_SIZE, position.y + 25f * UI_SIZE), true, 0, 0, "assets/icons/facebook.png").Add(cuis, gui);
                new UIButton(value ? ConfigData.UI_Settings_On_Image_Color.Value : ConfigData.UI_Settings_Off_Image_Color.Value, GetDictSettings(settings, player, true), $"tp.set.settings {settings.ToString().ToLower()}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + -20f * UI_SIZE, position.y + -20f * UI_SIZE), new Vector2(position.x + 20f * UI_SIZE, position.y + 20f * UI_SIZE), 10, TextAnchor.MiddleCenter, "1 1 1 1", 0, 0, value ? "assets/icons/vote_up.png" : "assets/icons/vote_down.png").Add(cuis, gui);
                new UIButton("0 1 1 1", "", $"tp.info {GetDictSettings(settings, player, false)}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + 10f * UI_SIZE, position.y + 10f * UI_SIZE), new Vector2(position.x + 30f * UI_SIZE, position.y + 30f * UI_SIZE), 14, TextAnchor.MiddleCenter, "0 0 0 1", 0, 0, "assets/icons/connection.png").Add(cuis, gui);
            }
            static void CreateCustomGUI(CuiElementContainer cuis, string gui, Vector2 position, string Text, string Command, string butcolor, string bcolor, float UI_SIZE, string sprite = null)
            {
                new UIPanel(bcolor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + -25f * UI_SIZE, position.y + -25f * UI_SIZE), new Vector2(position.x + 25f * UI_SIZE, position.y + 25f * UI_SIZE), true, 0, 0, "assets/icons/facebook.png").Add(cuis, gui);
                new UIButton(butcolor, Text, Command, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(position.x + -20f * UI_SIZE, position.y + -20f * UI_SIZE), new Vector2(position.x + 20f * UI_SIZE, position.y + 20f * UI_SIZE), 10, TextAnchor.MiddleCenter, "1 1 1 1", 0, 0, sprite).Add(cuis, gui);
            }
            static void CreateCustomGUI(CuiElementContainer cuis, string gui, Vector2 position, CustomGUI custom, float UI_SIZE) => CreateCustomGUI(cuis, gui, position, custom.Text, custom.Command, custom.ButColor, custom.BColor, UI_SIZE, custom.Sprite);
        }
        protected override void LoadDefaultConfig() => PrintError("No config file found, generating a new one.");
        public static class Log { public static void ToFile(string FileName, string Text) => _plugin.LogToFile(FileName, Text, _plugin, true); }
        #region UI
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
            public string _sprite;
            public UIButton(string color, string text, string command, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, int size = 14, TextAnchor align = TextAnchor.MiddleCenter, string textColor = "0 0 0 1", float FadeIn = 0, float FadeOut = 0, string sprite = null)
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
                _sprite = sprite;
            }
            public virtual void Add(CuiElementContainer container, string panel) => UI.CreateButton(container, panel, _color, _text, _command, _aMin, _aMax, _oMin, _oMax, _size, _align, _textColor, FadeIn, FadeOut, _sprite);
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
            public string _sprite;
            public UIPanel(string color, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, bool cursor, float FadeIn = 0, float FadeOut = 0, string sprite = null)
            {
                _color = color;
                _aMin = aMin;
                _aMax = aMax;
                _oMin = oMin;
                _oMax = oMax;
                _cursor = cursor;
                this.FadeIn = FadeIn;
                this.FadeOut = FadeOut;
                _sprite = sprite;
            }
            public virtual void Add(CuiElementContainer container, string panel) => UI.CreatePanel(container, panel, _color, _aMin, _aMax, _oMin, _oMax, _cursor, FadeIn, FadeOut, _sprite);
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
        public static class UI
        {
            public static CuiElementContainer CreateElementContainer(string panelName, string color, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, bool cursor = false, float fadein = 0, float fadeout = 0, string sprite = null) => CreateElementContainer(panelName, color, $"{aMin.x} {aMin.y}", $"{aMax.x} {aMax.y}", $"{oMin.x} {oMin.y}", $"{oMax.x} {oMax.y}", cursor, fadein, fadeout, sprite);
            public static CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, string oMin, string oMax, bool useCursor = false, float fadein = 0, float fadeout = 0, string sprite = null)
            {
                var NewElement = sprite == null ? new CuiElementContainer()
                    {
                        {
                            new CuiPanel { Image = {Color = color, FadeIn = fadein}, FadeOut = fadeout, RectTransform = {AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax}, CursorEnabled = useCursor },
                            new CuiElement().Parent,//"Overlay",//
                            panelName
                        }
                    } : new CuiElementContainer()
                    {
                        {
                            new CuiPanel { Image = {Color = color, Sprite = sprite, FadeIn = fadein}, FadeOut = fadeout, RectTransform = {AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax}, CursorEnabled = useCursor },
                            new CuiElement().Parent,//"Overlay",//new CuiElement().Parent,
                            panelName
                        }
                    };
                return NewElement;
            }
            public static void CreatePanel(CuiElementContainer container, string panel, string color, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, bool cursor = false, float fadein = 0, float fadeout = 0, string sprite = null) => CreatePanel(container, panel, color, $"{aMin.x} {aMin.y}", $"{aMax.x} {aMax.y}", $"{oMin.x} {oMin.y}", $"{oMax.x} {oMax.y}", cursor, fadein, fadeout, sprite);
            public static void CreatePanel(CuiElementContainer container, string panel, string color, string aMin, string aMax, string oMin, string oMax, bool cursor = false, float fadein = 0, float fadeout = 0, string sprite = null)
            {
                container.Add(sprite == null ? new CuiPanel
                {
                    Image = { Color = color, FadeIn = fadein },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax },
                    CursorEnabled = cursor,
                    FadeOut = fadeout
                } : new CuiPanel
                {
                    Image = { Color = color, Sprite = sprite, FadeIn = fadein },
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
            public static void CreateButton(CuiElementContainer container, string panel, string color, string text, string command, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, int size = 14, TextAnchor align = TextAnchor.MiddleCenter, string textColor = "0 0 0 1", float fadein = 0, float fadeout = 0, string sprite = null) => CreateButton(container, panel, color, text, command, $"{aMin.x} {aMin.y}", $"{aMax.x} {aMax.y}", $"{oMin.x} {oMin.y}", $"{oMax.x} {oMax.y}", size, align, textColor, fadein, fadeout, sprite);
            public static void CreateButton(CuiElementContainer container, string panel, string color, string text, string command, string aMin, string aMax, string oMin, string oMax, int size = 14, TextAnchor align = TextAnchor.MiddleCenter, string textColor = "0 0 0 1", float fadein = 0, float fadeout = 0, string sprite = null)
            {
                container.Add(sprite == null ? new CuiButton()
                {
                    Button = { Color = color, FadeIn = fadein, Command = command },
                    Text = { Text = text, FadeIn = fadein, FontSize = size, Align = align, Color = textColor },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax, OffsetMin = oMin, OffsetMax = oMax },
                    FadeOut = fadeout
                } : new CuiButton()
                {
                    Button = { Color = color, Sprite = sprite, FadeIn = fadein, Command = command },
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
        #endregion
    }
}
