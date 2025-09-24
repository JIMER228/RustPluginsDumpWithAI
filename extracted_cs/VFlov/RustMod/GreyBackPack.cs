using JetBrains.Annotations;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using static ConVar.Inventory.SavedLoadout;

namespace Oxide.Plugins
{
    [Info("GreyBackPack","VSP","1.0.0")]
    public class GreyBackPack : RustPlugin
    {
        static GreyBackPack ins;
        private bool EnabledUI = true;
        private int Type = 1;
        object OnEntityGroundMissing(BaseEntity entity)
        {
            var container = entity as StorageContainer;
            if (container != null)
            {
                var opened = openedBackpacks.Values.Select(x => x.storage);
                if (opened.Contains(container)) return false;
            }
            return null;
        }
        public class BackpackBox : MonoBehaviour
        {
            public StorageContainer storage;
            BasePlayer owner;
            public void Init(StorageContainer storade, BasePlayer owner)
            {
                this.storage = storade;
                this.owner = owner;
            }
            public static BackpackBox Spawn(BasePlayer player, ulong ownerid, int size = 1)
            {
                player.EndLooting();
                var storage = SpawnContainer(player, size, false, ownerid);
                var box = storage.gameObject.AddComponent<BackpackBox>();
                box.Init(storage, player);
                return box;
            }
            static int rayColl = LayerMask.GetMask("Construction", "Deployed", "Tree", "Terrain", "Resource", "World", "Water", "Default", "Prevent Building");
            public static StorageContainer SpawnContainer(BasePlayer player, int size, bool die, ulong ownerid)
            {
                var pos = player.transform.position;
                if (die)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(new Ray(player.GetCenter(), Vector3.down), out hit, 1000, rayColl, QueryTriggerInteraction.Ignore))
                        pos = hit.point;
                    else
                        pos -= new Vector3(0, 100, 0);
                }
                return SpawnContainer(player, size, pos, ownerid);
            }
            private static StorageContainer SpawnContainer(BasePlayer player, int size, Vector3 position, ulong ownerid, ulong playerid = 5931008)
            {
                var storage = GameManager.server.CreateEntity("assets/prefabs/deployable/large wood storage/box.wooden.large.prefab") as StorageContainer;
                if (storage == null) return null;
                storage.transform.position = position;
                storage.panelName = "generic_resizable";
                ItemContainer container = new ItemContainer();
                container.ServerInitialize(null, !ownerid.IsSteamId() ? ins.GetBackpackSize(player.UserIDString) : ins.GetBackpackSize(ownerid.ToString()));
                if ((int)container.uid.Value == 0) container.GiveUID();
                storage.inventory = container;
                if (!storage) return null;
                storage.SendMessage("SetDeployedBy", player, (SendMessageOptions)1);
                storage.OwnerID = player.userID;
                storage.Spawn();
                return storage;
            }
            private void PlayerStoppedLooting(BasePlayer player)
            {
                ins.BackpackHide(player.userID);
            }
            public void Close()
            {
                ClearItems();
                storage.Kill();
                Destroy(this);
            }
            public void StartLoot()
            {
                storage.SetFlag(BaseEntity.Flags.Open, true, false);
                owner.inventory.loot.StartLootingEntity(storage,false);
                owner.inventory.loot.AddContainer(storage.inventory);
                owner.inventory.loot.SendImmediate();
                owner.ClientRPCPlayer(null, owner, "RPC_OpenLootPanel", storage.panelName);
                storage.DecayTouch();
                storage.SendNetworkUpdate();
            }
            public void Push(List<Item> items)
            {
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    storage.inventory.Insert(items[i]);
                    items[i].MoveToContainer(storage.inventory);
                }
            }
            public void ClearItem()
            {
                for (int i = 0; i < storage.inventory.itemList.Count; i++)
                    storage.inventory.itemList[i].Remove(0.1f);
            }
            public List<Item> GetItems => storage.inventory.itemList.Where(i => i != null).ToList();  
        }
        public Dictionary<ulong, BackpackBox> openedBackpacks = new Dictionary<ulong, BackpackBox>();
        public Dictionary<ulong, List<SavedItem>> savedBackpacks;
        public Dictionary<ulong, BaseEntity> visualBackpacks = new Dictionary<ulong, BaseEntity>();
        private string[] clrs = {
            "#ffffff", "#fffbf5", "#fff8ea", "#fff4e0", "#fff0d5", "#ffedcb", "#ffe9c1", "#ffe5b6", "#ffe2ac", "#ffdea1", "#ffda97", "#ffd78d", "#ffd382", "#ffcf78", "#ffcc6d", "#ffc863", "#ffc458", "#ffc14e", "#ffbd44", "#ffb939", "#ffb62f", "#ffb224", "#ffae1a", "#ffab10", "#ffa705", "#ffa200", "#ff9b00", "#ff9400", "#ff8d00", "#ff8700", "#ff8000", "#ff7900", "#ff7200", "#ff6c00", "#ff6500", "#ff5e00", "#ff5800", "#ff5100", "#ff4a00", "#ff4300", "#ff3d00", "#ff3600", "#ff2f00", "#ff2800", "#ff2200", "#ff1b00", "#ff1400", "#ff0d00", "#ff0700", "#ff0000"
        };
        public Color GetBPColor(int count, int max)
        {
            float n = max > 0 ? (float)clrs.Length / max : 0;
            var index = (int)(count * n);
            if (index > 0) index--;
            return HexToColor(clrs[index]);
            
        }
        public static Color HexToColor(string hex)
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
        void OnServerSave()
        {
            SaveBackpacks();
        }
        void SaveBackpacks() => backpacksFile.WriteObject(savedBackpacks);
        object CanMoveItem(Item item, PlayerInventory playerLoot, ItemContainerId targetContainer, int targetSlot, int amount)
        {
            if (item == null || playerLoot == null) return null;
            var player = playerLoot.GetComponent<BasePlayer>();
            if (player == null) return null;
            if (openedBackpacks.ContainsKey(player.userID))
            {
                var target = playerLoot.FindContainer(targetContainer)?.GetSlot(targetSlot);
                if (target != null && targetContainer != item.GetRootContainer().uid)
                {
                    if (openedBackpacks[player.userID].storage.OwnerID != player.userID && openedBackpacks[player.userID].storage.inventory.uid == targetContainer)
                    {
                        SendReply(player, $"<color=#AF5085>[Backpack]:</color> Запрещено переносить предметы в чужой рюкзак");
                        return false;
                    }
                }
            }
            return null;
        }
        bool IsBackpackContainer(ulong uid, ulong userId) => openedBackpacks.ContainsKey(userId) ? true : false;
        void OnEnitityDeath(BaseCombatEntity ent, HitInfo info)
        {
            if (!(ent is BasePlayer)) return;
            var player = (BasePlayer)ent;
            List<SavedItem> savedItems;
            List<Items> items = new List<Item>();
            if (savedBackpacks.TryGetValue(player.userID, out savedItems)) ;
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
            DroppedItemContainer droppedItemContainer = ItemContainer.Drop("assets/prefabs/misc/item drop/item_drop_backpack.prefab", player.transform.position + Vector3.up, Quaternion.identity, iContainer);
            if (droppedItemContainer != null)
            {
                droppedItemContainer.playerName = $"Рюкзак игрока <color=#FF8080>{player.displayName}</color>";
                droppedItemContainer.playerSteamID = player.userID;
                NextFrame(() =>
                {
                    if (droppedItemContainer?.IsDestroyed != false) return;
                    droppedItemContainer.CancelInvoke(droppedItemContainer.RemoveMe);
                    droppedItemContainer.playerSteamID = player.userID;
                });
                Effect.server.Run("assets/bundled/prefabs/fx/dig_effect.prefab", droppedItemContainer.transform.position);
            } 
        }
        void OnNewSave()
        {
            LoadBackpacks();
            savedBackpacks = new Dictionary<ulong, List<SavedItem>>();
            SavedBackpacks();
            PrintWarning("Wipe. Player backpacks clear");
        }
        void Loaded()
        {
            ins = this;
            LoadBackpacks();
        }
        private bool loaded = false;
        void OnServerInitialized()
        {
            LoadConfig();
            LoadConfigValues();
            InitFileManager();
            ServerMgr.Instance.StartCoroutine(m_FileManager.LoadFile("backpackImage", ImageURL));
            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnection);
        }
        void OnPlayerConnection(BasePlayer player)
        {
            if (!EnabledUI) return;
            DrawUI(player);
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
            foreach (var dt in Resources.FindObjectsOfTypeAll<StashContainer>()) dt.Kill();
            foreach (var ent in Resources.FindObjectsOfTypeAll<TimedExplosive>().Where(ent => ent.name == "backpack")) ent.KillMessage();
        }
        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;
            if (OpenedOtherBackpack.ContaiinsKey(plyer.userId))
                OpenOtherBackpack[player.userID].EndLooting();
            if (!EnabledUI) return;
            DrawUI(player);
        }
        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            var target = entity.GetComponent<BasePlayer>();
            if (target != null) ShowUIPlayer(player, target);
        }
        void BackpackShow(BasePlayer player, ulong target = 0)
        {
            if (InDuel(player)) return;
            if (BackpackHide(player.userID)) return;
            var canBackPack = Interface.Call("CanBackpack", player);
            if (canBackPack != null)
                return;
            var reply = 5792;
            if (player.inventory.loot?.entitySource != null) player.EndLooting();
            var backpackSize = GetBackpackSize(player.UserIDString);
            if (backpackSize == 0) return;
            timer.Once(0.1f, () =>
            {
                List<SavedItem> savedItems;
                List<Item> items = new List<Item>();
                if (target != 0 && savedBackpacks.TryGetValue(target, out savedItems)) items = RestoreItems(savedItems);
                if (target == 0 && savedBackpacks.TryGetValue(player.userID, out savedItems)) items = RestoreItems(savedItems);
                BackpackBox box = BackpackBox.Spawn(player, target, backpackSize);
                openedBackpacks.Add(player.userID, box);
                box.storage.OwnerID = target != 0 ? target : player.userID;
                if (box.GetComponent<StorageContainer>() != null)
                {
                    box.GetComponent<StorageContainer>().OwnerID = target != 0 ? target : player.userID;
                    box.GetComponent<StorageContainer>().SendNetworkUpdate();
                }
                if (items.Count > 0) box.Push(items);
                box.StartLoot();
            });
        }
        void OnPlayerLootEnd(PlayerLoot inventory)
        {
            var player = inventory.GetComponent<BasePlayer>();
            if (player != null) CuiHelper.DestroyUi(player, "backpack_playermain");
        }
        void ShowUIPlayer(BasePlayer player, BasePlayer target)
        {
            if (!EnabledMainBackpackLoot) return;
            CuiHelper.DestroyUi(player, "backpack_playermain");
            CuiElementContainer container = new CuiElementContainer();
            string gg = @"[{""name"":""backpack_playermain"",""parent"":""Overlay"",""components"":[{""type"":""UnityEngine.UI.Button"",""command"":""backpack"",""sprite"":""assets/icons/open.png"",""color"":""1 0.9744871 0.6829964 0.5376649""},{""type"":""NeedsCursor""},{""type"":""RectTransform"",""anchormin"":""0.65 0.03"",""anchormax"":""0.6874999 0.1027778"",""offsetmin"":""0 0"",""offsetmax"":""0 0""}]}]";
            CuiHelper.AddUi(player, gg);
        }
        public Dictionary<ulong, BasePlayer> OpenOtherBackpack = new Dictionary<ulong, BasePlayer>();
        [ConsoleCommand("backpack")]
        private void ConsoleOpenMainBackpack(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            ulong targetID = player.userID;
            if (OpenOtherBackpack.ContainsKey(targetID))
            {
                if (OpenOtherBackpack[targetID] != player)
                {
                    SendReply(player, $"<color=#AF5085>[Backpack]:</color> Рюкзак уже кто то лутает");
                    return;
                }
            }
            if (!OpenOtherBackpack.ContainsKey(targetID))
                OpenOtherBackpack.Add(targetID, player);
            else
                OpenOtherBackpack[targetID] = player;
            BackpackShow(player, targetID);
        }
        int GetBackpackSize(string userId)
        {
            return 24;
        }
        public bool BackpackHide(ulong userId)
        {
            BackpackBox box;
            if (!openedBackpacks.TryGetValue(userId, out box)) return false;
            openedBackpacks.Remove(userId);
            if (box == null) return false;
            var items = SaveItems(box.GetItems);
            var owner = box.GetComponent<StorageContainer>();
            if (OpenOtherBackpack.ContainsKey(owner.OwnerID))
                OpenOtherBackpack.Remove(owner.OwnerID);
            if (items.Count() > 0) savedBackpacks[owner.OwnerID] = SaveItems(box.GetItems);
            box.Close();
            var otherPlayer = BasePlayer.FindByID(owner.OwnerID);
            if (otherPlayer != null) DrawUI(otherPlayer);
            else 
                DrawUI(BasePlayer.FindByID(userId));
            return true;
        }
        void DrawUI(BasePlayer player)
        {
            if (!EnabledUI) return;
            if (!m_FileManager.IsFinished)
            {
                timer.Once(1f, () => DrawUI(player));
                return;
            }
            CuiHelper.DestroyUi(player, "backpack_playermain");
            List<SavedItem> savedItems;
            if (!savedBackpacks.TryGetValue(player.userID, out savedItems)) savedItems = new List<SavedItem>();
            var bpSize = GetBackpackSize(player.UserIDString);
            if (bpSize == 0) return;
            int backpackCount = savedItems?.Count ?? 0;
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "1 1 1 0.03",
                    Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                }
                ,
                RectTransform =
                {
                    AnchorMin = "0.5 0",
                    AnchorMax = "0.5 0",
                    OffsetMin = "-265 18",
                    OffsetMax = "-205 78"
                }
                ,
            }, "Overlay", "backpack.image");
            var AnchorType = (float)backpackCount / bpSize - 0.03f;
            string AnchorMax = "1 1";
            string alpha = "1";
            switch (Type)
            {
                case 1:
                    AnchorType = (float)Math.Min(backpackCount, bpSize) / bpSize - 0.03f;
                    AnchorMax = $"0.05 {AnchorType}";
                    break;
                case 2:
                    AnchorType = (float)backpackCount / bpSize - 0.03f;
                    AnchorMax = $"1 {AnchorType}";
                    alpha = "0.5";
                    break;
                case 3:
                    AnchorType = (float)backpackCount / bpSize - 0.03f;
                    AnchorMax = $"{AnchorType} 1";
                    alpha = "0.5";
                    break;
                default:
                    AnchorType = (float)Math.Min(backpackCount, bpSize) + bpSize - 0.03f;
                    AnchorMax = $"0.05 {AnchorType}";
                    break;
            }
            container.Add(new CuiPanel
            {
                Image = {
                    Color=SetColor(GetBPColor(backpackCount, bpSize), alpha), Material="assets/content/ui/uibackgroundblur-ingamemenu.mat"
                }
                ,
                RectTransform = {
                    AnchorMin="0 0", AnchorMax=$"{AnchorMax}"
                }
                ,
            }
            , "backpack.image");
            container.Add(new CuiElement
            {
                Parent = "backpack.image",
                Components = {
                    new CuiRawImageComponent {
                        Png=m_FileManager.GetPng("backpackImage"), Color="1 1 1 0.5"
                    }
                    , new CuiRectTransformComponent {
                        AnchorMin="0.1 0.25", AnchorMax="0.9 0.95"
                    }
                    ,
                }
                ,
            }
            );

        }
        string SetColor(Color color, string alpha) => $"{color.r} {color.g} {color.b} {alpha}";
        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "backpack_playermain");
        }
        public class SavedItem
        {
            public string shortname;
            public string name;
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
                name = item.name,
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
            if (flameThrower != null) iItem.flamefuel = flameThrower.ammo;
            Chainsaw chainsaw = item.GetHeldEntity()?.GetComponent<Chainsaw>();
            if (chainsaw != null) iItem.flamefuel = chainsaw.ammo;
            if (item.contents != null)
            {
                foreach (var mod in item.contents.itemList)
                {
                    if (mod.info.itemid != 0) iItem.mods.Add(SaveItem(mod));
                }
            }
            if (item.info.category.ToString() != "Weapon") return iItem;
                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon == null) return iItem;
            if (weapon.primaryMagazine == null) return iItem;
            iItem.amount = weapon.primaryMagazine.contents;
            iItem.ammotype = weapon.primaryMagazine.ammoType.shortname;
            iItem.weapon = true;
            return iItem;
        }
        List<Item> RestoreItems(List<SavedItem> sItems)
        {
            return sItems.Select(sItem =>
            {
                if (sItem.weapon) return BuildWeapon(sItem);
                return BuildItem(sItem);
            });
        }
        Item BuildItem(SavedItem sItem)
        {
            if (sItem.amount < 1) sItem.amount = 1;
            Item item = ItemManager.CreateByItemID(sItem.itemid, sItem.amount, sItem.skinid);
            if (!string.IsNullOrEmpty(sItem.name)) item.name = sItem.name;
        }
    }
