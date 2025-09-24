// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Physics = UnityEngine.Physics;
using System.IO;

namespace Oxide.Plugins
{
    [Info("Backpack", "A0001", "1.1.3")]
    public class Backpack : RustPlugin
    {

        #region Ground missing fix

        object OnEntityGroundMissing(BaseEntity entity)
        {
            var container = entity as StorageContainer;
            if (container != null)
            {
                var opened = openedBackpacks.Values.Select(x => x.storage);
                if (opened.Contains(container))
                    return false;
            }
            return null;
        }

        #endregion

        #region Classes

        public class BackpackBox : MonoBehaviour
        {

            public StorageContainer storage;
            BasePlayer owner;

            public void Init(StorageContainer storage, BasePlayer owner)
            {
                this.storage = storage;
                this.owner = owner;
            }

            public static BackpackBox Spawn(BasePlayer player, int size = 1)
            {
                player.EndLooting();
                var storage = SpawnContainer(player, size, false);
                var box = storage.gameObject.AddComponent<BackpackBox>();
                box.Init(storage, player);
                return box;
            }

            static int rayColl = LayerMask.GetMask("Construction", "Deployed", "Tree", "Terrain", "Resource", "World", "Water", "Default", "Prevent Building");

            public static StorageContainer SpawnContainer(BasePlayer player, int size, bool die)
            {
                var pos = player.transform.position;
                if (die)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(new Ray(player.GetCenter(), Vector3.down), out hit, 1000, rayColl, QueryTriggerInteraction.Ignore))
                    {
                        pos = hit.point;
                    }
                }
                else
                {
                    pos -= new Vector3(0, 100, 0);
                }
                return SpawnContainer(player, size, pos);
            }

            private static StorageContainer SpawnContainer(BasePlayer player, int size, Vector3 position)
            {
                var storage = GameManager.server.CreateEntity("assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab") as StorageContainer;
                if (storage == null) return null;
                storage.transform.position = position;
                storage.panelName = "largewoodbox";
                ItemContainer container = new ItemContainer();
                container.ServerInitialize((Item)null, GetBackpackSize(player));
                if ((int)container.uid == 0)
                    container.GiveUID();
                storage.inventory = container;
                if (!storage) return null;
                storage.SendMessage("SetDeployedBy", player, (SendMessageOptions)1);
                storage.Spawn();
                return storage;
            }

            private void PlayerStoppedLooting(BasePlayer player)
            {
                Interface.Oxide.RootPluginManager.GetPlugin("Backpack").Call("BackpackHide", player.userID);
            }

            public void Close()
            {
                ClearItems();
                storage.Kill();
            }

            public void StartLoot()
            {
                storage.SetFlag(BaseEntity.Flags.Open, true, false);
                owner.inventory.loot.StartLootingEntity(storage, false);
                owner.inventory.loot.AddContainer(storage.inventory);
                owner.inventory.loot.SendImmediate();
                owner.ClientRPCPlayer(null, owner, "RPC_OpenLootPanel", storage.panelName);
                storage.DecayTouch();
                storage.SendNetworkUpdate();
            }

            public void Push(List<Item> items)
            {
                for (int i = items.Count - 1; i >= 0; i--)
                    items[i].MoveToContainer(storage.inventory);
            }

            public void ClearItems()
            {
                storage.inventory.itemList.Clear();
            }

            public List<Item> GetItems => storage.inventory.itemList.Where(i => i != null).ToList();

        }

        #endregion

        #region VARIABLES

        public Dictionary<ulong, BackpackBox> openedBackpacks = new Dictionary<ulong, BackpackBox>();
        public Dictionary<ulong, List<SavedItem>> savedBackpacks;
        public Dictionary<ulong, BaseEntity> visualBackpacks = new Dictionary<ulong, BaseEntity>();
        private List<ulong> guiCache = new List<ulong>();

        #endregion

        /// <summary>
        /// 2 2
        /// </summary>
        /// <param name="count"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public Color GetBPColor(int count, int max)
        {
            float n = max > 0 ? (float)clrs.Length / max : 0;
            var index = (int)(count * n);
            if (index > 0) index--;
            return hexToColor(clrs[index]);
        }

        public static Color hexToColor(string hex)
        {
            hex = hex.Replace("0x", "");
            hex = hex.Replace("#", "");
            byte a = 160;
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            if (hex.Length == 8)
            {
                a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return new Color32(r, g, b, a);
        }

        private string[] clrs =
        {
            "#ffffff",
            "#fffbf5",
            "#fff8ea",
            "#fff4e0",
            "#fff0d5",
            "#ffedcb",
            "#ffe9c1",
            "#ffe5b6",
            "#ffe2ac",
            "#ffdea1",
            "#ffda97",
            "#ffd78d",
            "#ffd382",
            "#ffcf78",
            "#ffcc6d",
            "#ffc863",
            "#ffc458",
            "#ffc14e",
            "#ffbd44",
            "#ffb939",
            "#ffb62f",
            "#ffb224",
            "#ffae1a",
            "#ffab10",
            "#ffa705",
            "#ffa200",
            "#ff9b00",
            "#ff9400",
            "#ff8d00",
            "#ff8700",
            "#ff8000",
            "#ff7900",
            "#ff7200",
            "#ff6c00",
            "#ff6500",
            "#ff5e00",
            "#ff5800",
            "#ff5100",
            "#ff4a00",
            "#ff4300",
            "#ff3d00",
            "#ff3600",
            "#ff2f00",
            "#ff2800",
            "#ff2200",
            "#ff1b00",
            "#ff1400",
            "#ff0d00",
            "#ff0700",
            "#ff0000"
        };


        #region DATA

        DynamicConfigFile backpacksFile = Interface.Oxide.DataFileSystem.GetFile("Backpack_Data");

        void LoadBackpacks()
        {
            try
            {
                savedBackpacks = backpacksFile.ReadObject<Dictionary<ulong, List<SavedItem>>>();
            }
            catch (Exception)
            {
                savedBackpacks = new Dictionary<ulong, List<SavedItem>>();
            }
        }

        void OnServerSave()
        {
            SaveBackpacks();
        }

        void SaveBackpacks() => backpacksFile.WriteObject(savedBackpacks);

        #endregion


        #region OXIDE HOOKS
        void OnEntityDeath(BaseCombatEntity ent, HitInfo info)
        {
            if (!(ent is BasePlayer)) return;
            var player = (BasePlayer)ent;
            if (InDuel(player)) return;
            BackpackHide(player.userID);
            if (PermissionService.HasPermission(player, BPIGNORE)) return;
            List<SavedItem> savedItems;
            List<Item> items = new List<Item>();
            if (savedBackpacks.TryGetValue(player.userID, out savedItems))
            {
                items = RestoreItems(savedItems);
                savedBackpacks.Remove(player.userID);
            }
            if (items.Count <= 0) return;
            if (DropWithoutBackpack)
            {
                foreach (var item in items)
                {
                    item.Drop(player.transform.position + Vector3.up, Vector3.up);
                }
                return;
            }
            var iContainer = new ItemContainer();
            iContainer.ServerInitialize(null, items.Count);
            iContainer.GiveUID();
            iContainer.entityOwner = player;
            iContainer.SetFlag(ItemContainer.Flag.NoItemInput, true);
            for (int i = items.Count - 1; i >= 0; i--)
                items[i].MoveToContainer(iContainer);
            DroppedItemContainer droppedItemContainer = ItemContainer.Drop("assets/prefabs/misc/item drop/item_drop_backpack.prefab", player.transform.position, Quaternion.identity, iContainer);

            if (droppedItemContainer != null)
            {
                droppedItemContainer.playerName = $"Backpack игрока <color=red>{player.displayName}</color>";
                droppedItemContainer.playerSteamID = player.userID;


                timer.Once(KillTimeout, () =>
               {
                   if (droppedItemContainer != null && !droppedItemContainer.IsDestroyed)
                       droppedItemContainer.Kill();
               });
                Effect.server.Run("assets/bundled/prefabs/fx/dig_effect.prefab", droppedItemContainer.transform.position);
            }
        }



        #region Config
        string BPIGNORE = "backpack.ignore";

        bool DropWithoutBackpack = false;

        float KillTimeout = 300f;

        static List<string> permisions = new List<string>();

        private void LoadConfigValues()
        {
            bool changed = false;
            if (GetConfig("Основные настройки", "При смерти игрока выкидывать вещи без рюкзака", ref DropWithoutBackpack))
            {
                changed = true;
            }
            if (GetConfig("Основные настройки", "Время удаления рюкзака после выпадения", ref KillTimeout))
            {
                changed = true;
            }
            if (GetConfig("Основные настройки", "Привилегия игнорирования выпадение рюкзака", ref BPIGNORE))
            {
                changed = true;
            }

            var _permisions = new List<object>()
            {
                {"backpack.size0"},
                {"backpack.size1"},
                {"backpack.size6"},
                {"backpack.size15"},
                {"backpack.size30"}
            };
            if (GetConfig("Основные настройки", "Список привилегий и размера рюкзака (backpack.size999 - где 999 это слоты, макс. 30)", ref _permisions))
            {
                Puts("Привилегии созданы, Backpack загружен!");
                changed = true;
            }

            permisions = _permisions.Select(p => p.ToString()).ToList();
            if (changed)
                SaveConfig();
        }

        private bool GetConfig<T>(string MainMenu, string Key, ref T var)
        {
            if (Config[MainMenu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[MainMenu, Key], typeof(T));
                return false;
            }
            Config[MainMenu, Key] = var;
            return true;
        }
        #endregion

        void Loaded()
        {
            LoadBackpacks();
        }

        private bool loaded = false;

        void OnServerInitialized()
        {
            InitFileManager();
            LoadConfig();
            LoadConfigValues();
            PermissionService.RegisterPermissions(this, permisions);
            PermissionService.RegisterPermissions(this, new List<string>() { BPIGNORE });
            ServerMgr.Instance.StartCoroutine(LoadImages());
            foreach (var player in BasePlayer.activePlayerList) DrawUI(player);
        }

        private Dictionary<string, string> images = new Dictionary<string, string>()
        {
            ["backpackImg"] = "http://i.imgur.com/dJs7pK3.png"
        };

        IEnumerator LoadImages()
        {
            foreach (var name in images.Keys.ToList())
            {
                yield return m_FileManager.StartCoroutine(m_FileManager.LoadFile(name, images[name]));
                images[name] = m_FileManager.GetPng(name);
            }
            loaded = true;
            foreach (var player in BasePlayer.activePlayerList) DrawUI(player);
        }

        void Unload()
        {
            var keys = openedBackpacks.Keys.ToList();
            for (int i = openedBackpacks.Count - 1; i >= 0; i--)
                BackpackHide(keys[i]);
            SaveBackpacks();
            foreach (var player in BasePlayer.activePlayerList) DestroyUI(player);

            UnityEngine.Object.Destroy(FileManagerObject);
        }

        void OnPreServerRestart()
        {
            foreach (var dt in Resources.FindObjectsOfTypeAll<StashContainer>())
                dt.Kill();
            foreach (var ent in Resources.FindObjectsOfTypeAll<TimedExplosive>().Where(ent => ent.name == "backpack"))
                ent.KillMessage();
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            DrawUI(player);
        }

        void OnPlayerAspectChanged(BasePlayer player)
        {
            DrawUI(player);
        }
        #endregion

        #region FUNCTIONS

        void BackpackShow(BasePlayer player)
        {
            if (InDuel(player)) return;
            if (BackpackHide(player.userID)) return;
            var reply = 1001;
            if (reply == 0) { }
            if (player.inventory.loot?.entitySource != null) return;

            var backpackSize = GetBackpackSize(player);
            if (backpackSize == 0) return;
            timer.Once(0.1f, () =>
           {
               if (!player.IsOnGround()) return;
               List<SavedItem> savedItems;
               List<Item> items = new List<Item>();
               if (savedBackpacks.TryGetValue(player.userID, out savedItems))
                   items = RestoreItems(savedItems);
               BackpackBox box = BackpackBox.Spawn(player, backpackSize);
               openedBackpacks.Add(player.userID, box);
               if (items.Count > 0)
                   box.Push(items);
               box.StartLoot();
           });
        }

        static int GetBackpackSize(BasePlayer player)
        {
            for (int i = permisions.Count - 1; i >= 0; i--)
                if (PermissionService.HasPermission(player, permisions[i]))
                    return Convert.ToInt32(permisions[i].Replace("backpack.size", ""));
            return 0;
        }


        [HookMethod("BackpackHide")]
        bool BackpackHide(ulong playerId)
        {
            BackpackBox box;
            if (!openedBackpacks.TryGetValue(playerId, out box)) return false;
            openedBackpacks.Remove(playerId);
            if (box == null) return false;
            var items = SaveItems(box.GetItems);
            if (items.Count > 0)
            {
                savedBackpacks[playerId] = SaveItems(box.GetItems);
            }
            else
            {
                savedBackpacks.Remove(playerId);
            }
            box.Close();
            var player = BasePlayer.FindByID(playerId);
            if (player)
                DrawUI(player);
            return true;
        }


        #endregion

        #region UI

        void DrawUI(BasePlayer player)
        {
            if (!m_FileManager.IsFinished)
            {
                timer.Once(0.1f, () => DrawUI(player));
                return;
            }
            if (!guiCache.Contains(player.userID))
            {
                guiCache.Add(player.userID);
            }
            List<SavedItem> savedItems;
            if (!savedBackpacks.TryGetValue(player.userID, out savedItems))
                savedItems = new List<SavedItem>();

            var bpSize = GetBackpackSize(player);
            if (bpSize == 0) return;
            int backpackCount = savedItems?.Count ?? 0;
            CuiHelper.DestroyUi(player, "backpack.btn");
            CuiHelper.DestroyUi(player, "backpack.text1");
            CuiHelper.DestroyUi(player, "backpack.text2");
            CuiHelper.DestroyUi(player, "backpack.image");
            CuiHelper.AddUi(player,
                bpGUI.Replace("{0}", backpackCount.ToString())
                    .Replace("{1}", bpSize.ToString())
                    .Replace("{3}", SetColor(GetBPColor(backpackCount, bpSize)))
                    .Replace("{4}", images["backpackImg"]));
        }
        string SetColor(Color color) => $"{color.r} {color.g} {color.b} 1";
        private string bpGUI = @"[{
	""name"": ""backpack.image"",
	""parent"": ""Overlay"",
	""components"": [{
		""type"": ""UnityEngine.UI.RawImage"",
		""sprite"": ""assets/content/textures/generic/fulltransparent.tga"",
		""color"": ""{3}"",
		""png"": ""{4}""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.29112 0.01944441"",
		""anchormax"": ""0.3416667 0.1027779"",
		""offsetmin"": ""0 0"",
		""offsetmax"": ""1 1""
	}]
}, {
	""name"": ""backpack.text1"",
	""parent"": ""backpack.image"",
	""components"": [{
		""type"": ""UnityEngine.UI.Text"",
		""text"": ""{0}"",
		""fontSize"": 13,
		""align"": ""MiddleCenter"",
		""color"": ""0 0 0 0.7058824""
	}, {
		""type"": ""UnityEngine.UI.Outline"",
		""color"": ""0.3568628 0.3568628 0.3568628 1"",
		""distance"": ""0.5 -0.5""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.220705 0.1333331"",
		""anchormax"": ""0.478305 0.4111103"",
		""offsetmin"": ""0 0"",
		""offsetmax"": ""1 1""
	}]
}, {
	""name"": ""backpack.text2"",
	""parent"": ""backpack.image"",
	""components"": [{
		""type"": ""UnityEngine.UI.Text"",
		""text"": ""{1}"",
		""fontSize"": 13,
		""align"": ""MiddleCenter"",
		""color"": ""0 0 0 0.7061956""
	}, {
		""type"": ""UnityEngine.UI.Outline"",
		""color"": ""0.3592146 0.3592146 0.3592146 1"",
		""distance"": ""0.5 -0.5""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.513671 0.1333331"",
		""anchormax"": ""0.7712711 0.4111103"",
		""offsetmin"": ""0 0"",
		""offsetmax"": ""1 1""
	}]
}, {
	""name"": ""backpack.btn"",
	""parent"": ""Overlay"",
	""components"": [{
		""type"": ""UnityEngine.UI.Button"",
		""command"": ""backpack.open"",
		""color"": ""1 1 1 0""
	}, {
		""type"": ""RectTransform"",
		""anchormin"": ""0.2937759 0.02592597"",
		""anchormax"": ""0.3383071 0.1013889"",
		""offsetmin"": ""0 0"",
		""offsetmax"": ""1 1""
	}]
}]";

        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "backpack.image");
        }

        #endregion

        #region COMMANDS
        [ChatCommand("bp")]
        void cmdBackpackShow(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            /*if (!player.IsAdmin)
            {
                player.ChatMessage("Рюкзак на тех работах, не беспокойтесь, ваши вещи не пропадут!");
                return;
            }*/
            player.EndLooting();
            NextTick(() => BackpackShow(player));
        }

        [ConsoleCommand("backpack.open")]
        void cmdOnBackPackShowClick(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            if (player.inventory.loot?.entitySource != null)
            {
                BackpackBox bpBox;
                if (openedBackpacks.TryGetValue(player.userID, out bpBox) &&
                    bpBox.gameObject == player.inventory.loot.entitySource.gameObject)
                {
                    return;
                }
                player.EndLooting();
                NextTick(() => BackpackShow(player));
                return;
            }
            else
            {
                BackpackShow(player);
            }
        }

        #endregion

        #region ITEM EXTENSION

        public class SavedItem
        {
            public string shortname;
            public int itemid;
            public float condition;
            public float maxcondition;
            public int amount;
            public int ammoamount;
            public string ammotype;
            public int flamefuel;
            public ulong skinid;
            public bool weapon;
            public int blueprint;
            public List<SavedItem> mods;
        }

        List<SavedItem> SaveItems(List<Item> items) => items.Select(SaveItem).ToList();

        SavedItem SaveItem(Item item)
        {
            SavedItem iItem = new SavedItem
            {
                shortname = item.info?.shortname,
                amount = item.amount,
                mods = new List<SavedItem>(),
                skinid = item.skin,
                blueprint = item.blueprintTarget
            };

            if (item.info == null) return iItem;

            iItem.itemid = item.info.itemid;
            iItem.weapon = false;
            if (item.hasCondition)
            {
                iItem.condition = item.condition;
                iItem.maxcondition = item.maxCondition;
            }
            FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
            if (flameThrower != null)
                iItem.flamefuel = flameThrower.ammo;
            if (item.info.category.ToString() != "Weapon") return iItem;
            BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon == null) return iItem;
            if (weapon.primaryMagazine == null) return iItem;
            iItem.ammoamount = weapon.primaryMagazine.contents;
            iItem.ammotype = weapon.primaryMagazine.ammoType.shortname;
            iItem.weapon = true;
            if (item.contents != null)
                foreach (var mod in item.contents.itemList)
                    if (mod.info.itemid != 0)
                        iItem.mods.Add(SaveItem(mod));
            return iItem;
        }

        List<Item> RestoreItems(List<SavedItem> sItems)
        {
            return sItems.Select(sItem =>
           {
               if (sItem.weapon) return BuildWeapon(sItem);
               return BuildItem(sItem);

           }).Where(i => i != null).ToList();
        }

        Item BuildItem(SavedItem sItem)
        {
            if (sItem.amount < 1) sItem.amount = 1;
            Item item = ItemManager.CreateByItemID(sItem.itemid, sItem.amount, sItem.skinid);
            item.blueprintTarget = sItem.blueprint;
            if (item.hasCondition)
            {
                item.condition = sItem.condition;
                item.maxCondition = sItem.maxcondition;
            }
            FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
            if (flameThrower)
                flameThrower.ammo = sItem.flamefuel;
            return item;
        }

        Item BuildWeapon(SavedItem sItem)
        {
            Item item = ItemManager.CreateByItemID(sItem.itemid, 1, sItem.skinid);

            if (item.hasCondition)
            {
                item.condition = sItem.condition;
                item.maxCondition = sItem.maxcondition;
            }
            var weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon != null)
            {
                var def = ItemManager.FindItemDefinition(sItem.ammotype);
                weapon.primaryMagazine.ammoType = def;
                weapon.primaryMagazine.contents = sItem.ammoamount;
            }

            if (sItem.mods != null)
                foreach (var mod in sItem.mods)
                    item.contents.AddItem(BuildItem(mod).info, 1);
            return item;
        }

        #endregion

        #region EXTERNAL CALLS

        [PluginReference]
        Plugin Duel;

        bool InDuel(BasePlayer player) => Duel?.Call<bool>("IsPlayerOnActiveDuel", player) ?? false;

        #endregion


        public static class PermissionService
        {
            public static Permission permission = Interface.GetMod().GetLibrary<Permission>();

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                if (player == null || string.IsNullOrEmpty(permissionName))
                    return false;

                var uid = player.UserIDString;
                if (permission.UserHasPermission(uid, permissionName))
                    return true;

                return false;
            }

            public static void RegisterPermissions(Plugin owner, List<string> permissions)
            {
                if (owner == null) throw new ArgumentNullException("owner");
                if (permissions == null) throw new ArgumentNullException("commands");

                foreach (var permissionName in permissions.Where(permissionName => !permission.PermissionExists(permissionName)))
                {
                    permission.RegisterPermission(permissionName, owner);
                }
            }
        }

        #region File Manager

        private GameObject FileManagerObject;
        private FileManager m_FileManager;

        /// <summary>
        /// Инициализация скрипта взаимодействующего с файлами сервера
        /// </summary>
        void InitFileManager()
        {
            FileManagerObject = new GameObject("MAP_FileManagerObject");
            m_FileManager = FileManagerObject.AddComponent<FileManager>();
        }

        class FileManager : MonoBehaviour
        {
            int loaded = 0;
            int needed = 0;

            public bool IsFinished => needed == loaded;
            const ulong MaxActiveLoads = 10;
            Dictionary<string, FileInfo> files = new Dictionary<string, FileInfo>();

            DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("Backpack/Images");

            private class FileInfo
            {
                public string Url;
                public string Png;
            }

            public void SaveData()
            {
                dataFile.WriteObject(files);
            }

            public string GetPng(string name) => files[name].Png;

            private void Awake()
            {
                files = dataFile.ReadObject<Dictionary<string, FileInfo>>() ?? new Dictionary<string, FileInfo>();
            }

            public IEnumerator LoadFile(string name, string url)
            {
                if (files.ContainsKey(name) && files[name].Url == url && !string.IsNullOrEmpty(files[name].Png)) yield break;
                files[name] = new FileInfo() { Url = url };
                needed++;
                yield return StartCoroutine(LoadImageCoroutine(name, url));
            }

            IEnumerator LoadImageCoroutine(string name, string url)
            {
                using (WWW www = new WWW(url))
                {
                    yield return www;
                    using (MemoryStream stream = new MemoryStream())
                    {
                        if (string.IsNullOrEmpty(www.error))
                        {
                            var entityId = CommunityEntity.ServerInstance.net.ID;
                            var crc32 = FileStorage.server.Store(www.bytes, FileStorage.Type.png, entityId).ToString();
                            files[name].Png = crc32;
                        }
                    }
                }
                loaded++;
            }
        }
        #endregion
    }
}
                   