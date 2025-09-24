using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("Ender Chest", "https://discord.gg/dNGbxafuJn", "A.1.0")]
    public class EnderChest : RustPlugin
    {
        #region Classess

        private class Configuration
        {
            [JsonProperty("Номер скина для сундука")]
            public ulong SkinID; 

            [JsonProperty("Стандартный размер сундука для всех")]
            public int DefaultSize;
            [JsonProperty("Запретить открывать сундук на чужой территории")]
            public bool DisableCup;
            [JsonProperty("Размеры в зависимости от привилегии")]
            public Dictionary<string, int> CustomSizes = new Dictionary<string,int>();
            [JsonProperty("Запрещенные предметы в сундуке")]
            public List<string> BlockedItems = new List<string>();

            public static Configuration Generate()  
            {
                return new Configuration
                {
                    SkinID = 1615520878,
                    DefaultSize = 10,
                    CustomSizes = new Dictionary<string, int>
                    {
                        ["EnderChest.VIP"] = 5, 
                        ["EnderChest.Premium"] = 10
                    },
                    
                    BlockedItems = new List<string>
                    {
                        "rifle.ak"
                    }
                };
            }
        }

        private class SavedItem
        {
            public string DisplayName;
            public int Amount;
            public string ShortName;
            public ulong SkinID;

            public float Condition;
            public float MaxCondition;

            public bool IsWeapon;
            public int AmmoAmount = -1;
            
            public List<SavedItem> Modules = new List<SavedItem>();
            
            public SavedItem() { }
            public SavedItem(Item item)
            {
                Amount = item.amount;
                
                DisplayName = item.name;
                ShortName = item.info.shortname;
                SkinID = item.skin;

                Condition = item.condition;
                MaxCondition = item.maxCondition;

                var weaponComponent = item.GetHeldEntity()?.GetComponent<BaseProjectile>(); 
                IsWeapon = weaponComponent != null;
                
                if (weaponComponent != null)
                {
                    AmmoAmount = weaponComponent.primaryMagazine.contents;
                    
                    foreach (var check in item.contents.itemList)
                        Modules.Add(new SavedItem(check)); 
                }
            } 

            public Item ToItem()
            {
                Item item = ItemManager.CreateByName(ShortName, Amount, SkinID);
                item.name = DisplayName;
                item.condition = Condition;
                item.maxCondition = MaxCondition;

                foreach (var check in Modules)
                    check.ToItem().MoveToContainer(item.contents);

                if (IsWeapon)
                {
                    var weaponComponent = item.GetHeldEntity()?.GetComponent<BaseProjectile>();
                    if (weaponComponent != null)
                    {
                        weaponComponent.primaryMagazine.contents = AmmoAmount;
                    }
                }

                return item;
            }
        }

        private class EnderLooter : MonoBehaviour
        {
            public BasePlayer Player;
            public BaseEntity WoodBox;
            
            public void Awake()
            {
                Player = GetComponent<BasePlayer>(); 
            }

            public void Initialize(BaseEntity entity)
            {
                WoodBox = entity;
                WoodBox.SetFlag(BaseEntity.Flags.Locked, true);
                WoodBox.OwnerID = Player.userID;
                
                LoadBox();
                
                InvokeRepeating(nameof(ControlUpdate), 0f, 0.1f);
            }

            public void LoadBox()
            {
                var container = WoodBox.GetComponent<StorageContainer>();
                container.inventory.itemList.Clear();
                container.inventory.capacity = GetSizeForPlayer(Player); 
                
                var info = DataWorker.Base.SavedItems[Player.userID];
                foreach (var check in info)
                {
                    var result = check.ToItem();
                    if (!result.MoveToContainer(container.inventory))
                    {
                        result.Drop(WoodBox.transform.position + new Vector3(0, 1f, 0), Vector3.down); 
                    }
                }
                
                if (info.Count > container.inventory.capacity)
                    Player.ChatMessage($"<color=orange>Не хватает места</color>, некоторые предметы выброшены на землю!");
            }

            public void ControlUpdate()
            {
                if (Player.inventory.loot.entitySource == null || Player.inventory.loot.entitySource != WoodBox)
                {
                    Destroy(this);
                    return;
                }
            }

            public void OnDestroy()
            {
                CuiHelper.DestroyUi(Player, Layer);
                
                CloseBox(WoodBox, Player.userID); 
            }
        } 

        private class DataBase
        {
            public Dictionary<ulong, List<SavedItem>> SavedItems = new Dictionary<ulong, List<SavedItem>>();
        }

        private static class DataWorker
        {
            public static DataBase Base = new DataBase();
            
            public static DataBase ReadData() => Base = Interface.Oxide.DataFileSystem.ReadObject<DataBase>(_.Name) ?? new DataBase();
            public static void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(_.Name, Base);
        }

        #endregion

        #region Variables

        private static EnderChest _;
        private static Configuration Settings;

        #endregion

        #region Hooks
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                Settings = Config.ReadObject<Configuration>();
            }
            catch
            {
                PrintWarning($"Error reading config, creating one new config!");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => Settings = Configuration.Generate();
        protected override void SaveConfig()        => Config.WriteObject(Settings);

        private void OnServerInitialized()
        {
            _ = this;
            
            DataWorker.ReadData();
            foreach (var check in Settings.CustomSizes)
                permission.RegisterPermission(check.Key, this);
              
            BasePlayer.activePlayerList.ForEach(OnPlayerInit);
            timer.Every(360, DataWorker.SaveData); 
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        { 
            if (entity.skinID != Settings.SkinID) return;

            var obj = entity as StorageContainer; 
            if (obj == null) return;

            if (obj.inventory.itemList.Count > 0)
            {
                CloseBox(entity, entity.OwnerID);
                return;
            }
        }
        
        private ItemContainer.CanAcceptResult? CanAcceptItem(ItemContainer container, Item item, int targetPos)
        {
            var owner = item?.GetOwnerPlayer();
            if (owner == null) return null;

            if (!Settings.BlockedItems.Contains(item.info.shortname)) return null;
            
            var obj = owner.GetComponent<EnderLooter>();
            if (obj == null) return null;
            
            ShowNotification(owner, "ЭТОТ ПРЕДМЕТ ЗАПРЕЩЁН");
            return ItemContainer.CanAcceptResult.CannotAccept;  
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (!DataWorker.Base.SavedItems.ContainsKey(player.userID))
                DataWorker.Base.SavedItems.Add(player.userID, new List<SavedItem>());
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            var obj = player.GetComponent<EnderLooter>();
            if (obj == null) return;
            
            UnityEngine.Object.Destroy(obj); 
        }

        private void Unload()
        {
            DestroyAll<EnderLooter>();
            
            DataWorker.SaveData();
        }

        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (!entity.PrefabName.Contains("box.wooden.large") || entity.skinID != Settings.SkinID) return;

            var container = entity.GetComponent<StorageContainer>();
            if (container == null)
            {
                PrintError($"Something strange, killing entity...");
                entity.Kill();
                return;
            }

            var obj = player.GetComponent<EnderLooter>();
            if (obj != null)
            {
                PrintError("Unable to create component #1");
                return;
            }

            if (Settings.DisableCup && player.IsBuildingBlocked())
            {
                player.ChatMessage($"<color=orange>Запрещено</color> на чужой территории!");
                timer.Once(0.1f, player.EndLooting);
                return;
            }
            
            player.gameObject.AddComponent<EnderLooter>().Initialize(entity); 
        }

        #endregion
 
        #region Commands

        [ChatCommand("ec.create")]
        private void CmdChatCreate(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;

            var item = ItemManager.CreateByName("box.wooden.large", 50, Settings.SkinID);
            item.MoveToContainer(player.inventory.containerBelt);
            
            player.ChatMessage($"Предметы <color=orange>успешно</color> выданы");
        } 

        #endregion

        #region Interfaces

        private static string Layer = "UI.IRS1.Layer";
        private void ShowNotification(BasePlayer player, string text)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiLabel
            {
                FadeOut = 1f, 
                RectTransform = {AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "250 365", OffsetMax = "565 500" },
                Text = { FadeIn = 1f, Text = text.ToUpper(), Align = TextAnchor.MiddleRight, Font = "robotocondensed-regular.ttf", FontSize = 14, Color = "1 1 1 0.7"}
            }, "Overlay", Layer);

            CuiHelper.AddUi(player, container);
            
            timer.Once(3, () => CuiHelper.DestroyUi(player, Layer));
        }
 
        #endregion

        #region Methods
       
        public static void CloseBox(BaseEntity entity, ulong userId)
        {
            var container = entity.GetComponent<StorageContainer>();

            var info = DataWorker.Base.SavedItems[userId];
            info.Clear();
            
            foreach (var check in container.inventory.itemList)
                info.Add(new SavedItem(check)); 
            
            container.inventory.itemList.Clear();
            entity.SetFlag(BaseEntity.Flags.Locked, false);  
        }

        #endregion

        #region Utils

        private void DestroyAll<T>()
        {
            foreach (var check in UnityEngine.Object.FindObjectsOfType(typeof(T)))
                UnityEngine.Object.Destroy(check);
        }

        private static int GetSizeForPlayer(BasePlayer player)
        {
            foreach (var check in Settings.CustomSizes)
            {
                if (_.permission.UserHasPermission(player.UserIDString, check.Key))
                    return check.Value;
            }

            return Settings.DefaultSize;
        }

        #endregion
    }
}