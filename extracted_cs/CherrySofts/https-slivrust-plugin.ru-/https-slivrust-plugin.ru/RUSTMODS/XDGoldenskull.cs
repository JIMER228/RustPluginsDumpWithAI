// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using ConVar;
using Oxide.Core.Plugins;
using System.Linq;
using Oxide.Core;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("XDGoldenskull", "rustmods.ru", "1.0.11")]
    public class XDGoldenskull : RustPlugin
    {
        [PluginReference] Plugin IQChat;

        private static System.Random random = new System.Random();
        private void OnLootSpawn(LootContainer container)
        {
            if (container == null || lootContainerList == null)
                return;

            foreach (KeyValuePair<string, int> crate in lootContainerList)
            {
                if (container.PrefabName.Contains(crate.Key))
                {
                    if (random.Next(0, 100) >= (100 - crate.Value))
                    {
                        InvokeHandler.Instance.Invoke(() =>
                        {
                            if (container.inventory.capacity <= container.inventory.itemList.Count)
                            {
                                container.inventory.capacity = container.inventory.itemList.Count + 1;
                            }
                            Item item = (Item)CreateItem();
                            item?.MoveToContainer(container.inventory);
                        }, 0.21f);
                    }
                }
            }
        }

        [ConsoleCommand("goldenskul")]
        void FishCommand(ConsoleSystem.Arg arg)
        {

            BasePlayer player = BasePlayer.Find(arg.Args[0]);
            if (player == null || !player.IsConnected)
            {
                Puts("Игрок не найден");
                return;
            }
            int count = int.Parse(arg.Args[1]);
            config.CreateItem(player, Vector3.zero, count);
            SendChat(player, $"Вы успешно получили {config.DisplayName}");
            Puts($"Игроку выдана {config.DisplayName}");
        }

		   		 		  						  	   		   					  	  			  	 				   		 
        
        
        [ChatCommand("g.give")]
        private void cmdChatEmerald(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            config.CreateItem(player, Vector3.zero, 10);
        }

        
                private Item CreateItem()
        {
            return config.Copy(1);
        }
        private IEnumerable<KeyValuePair<string, int>> lootContainerList = null;
        private static void ItemRemovalThink(Item item, BasePlayer player, int itemsToTake)
        {
            if (item.amount == itemsToTake)
            {
                item.RemoveFromContainer();
                item.Remove();
            }
            else
            {
                item.amount = item.amount - itemsToTake;
                player.inventory.SendSnapshot();
            }
        }
		   		 		  						  	   		   					  	  			  	 				   		 
        object CanBeRecycled(Item item, Recycler recycler)
        {
            if (item == null)
                return false;
            if (item.info.shortname == ReplaceShortName && item.skin == config.ReplaceID)
                return true;
            return null;
        }
        public void SendChat(BasePlayer player, string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, "");
            else
                player.SendConsoleCommand("chat.add", channel, 0, Message);
        }

        
        private static Configuration config = new Configuration();

        object OnItemRecycle(Item item, Recycler recycler)
        {
            if (item.info.shortname == ReplaceShortName && item.skin == config.ReplaceID)
            {
                item.UseItem(1);
                int RandomItem = random.Next(config.itemsrec.Count);
                recycler.MoveItemToOutput(ItemManager.CreateByName(config.itemsrec[RandomItem], 1));
                return true;
            }
            return null;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Ошибка #6941" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }
        object CanRecycle(Recycler recycler, Item item)
        {
            if (item.info.shortname == ReplaceShortName && item.skin == config.ReplaceID)
                return true;
            return null;
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        
               
        void OnServerInitialized() => lootContainerList = config.cratelList.Concat(config.barellList);
        private class Configuration
        {

            public Item Copy(int amount = 1)
            {
                Item x = ItemManager.CreateByPartialName(ReplaceShortName, amount);
                x.skin = ReplaceID;
                x.name = DisplayName;
                x.info.stackable = StackItem;

                return x;
            }
            [JsonProperty("Из каких бочек будет падать и процент выпадения")]
            public Dictionary<string, int> barellList = new Dictionary<string, int>();
            [JsonProperty("Призы за переработку")]
            public List<string> itemsrec = new List<string>();

            public void CreateItem(BasePlayer player, Vector3 position, int amount)
            {
                Item x = ItemManager.CreateByPartialName(ReplaceShortName, amount);
                x.skin = ReplaceID;
                x.name = DisplayName;
                x.info.stackable = StackItem;

                if (player != null)
                {
                    if (player.inventory.containerMain.itemList.Count < 24)
                        x.MoveToContainer(player.inventory.containerMain);
                    else
                        x.Drop(player.transform.position, Vector3.zero);
                    return;
                }

                if (position != Vector3.zero)
                {
                    x.Drop(position, Vector3.down);
                    return;
                }
            }
            [JsonProperty("Призы за потрошения")]
            public List<string> itempot = new List<string>();
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    DisplayName = "Золотой череп",
                    StackItem = 5,
                    ReplaceID = 1683645276,
                    barellList = new Dictionary<string, int>
                    {
                        ["loot-barrel-1"] = 50,
                        ["loot-barrel-2"] = 20,
                    },
                    cratelList = new Dictionary<string, int>
                    {
                        ["bradley_crate"] = 50,
                        ["codelockedhackablecrate_oilrig"] = 20,
                        ["crate_elite"] = 20,
                    },
                    itemsrec = new List<string>
                    {
                        "weapon.mod.small.scope",
                        "rifle.ak",
                        "rifle.l96",
                        "smg.thompson",
                        "rifle.semiauto",
                        "pistol.revolver",
                        "rifle.lr300",
                    },
                    itempot = new List<string>
                    {
                        "shotgun.double",
                        "grenade.f1",
                        "smg.2",
                        "shotgun.pump",
                        "pistol.semiauto",
                        "pistol.python",
                        "weapon.mod.lasersight",
                        "weapon.mod.muzzlebrake",
                    },
                };
            }
            [JsonProperty("Скин ID черепа")]
            public ulong ReplaceID;
            [JsonProperty("Стак предмета")]
            public int StackItem;
		   		 		  						  	   		   					  	  			  	 				   		 
            public int GetItemId() => ItemManager.FindItemDefinition(ReplaceShortName).itemid;
            [JsonProperty("Отображаемое имя")]
            public string DisplayName;
            [JsonProperty("Из каких ящиков будет падать и процент выпадения")]
            public Dictionary<string, int> cratelList = new Dictionary<string, int>();
        }
        private const string ReplaceShortName = "skull.human";

        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (action == "crush" && item.skin == config.ReplaceID)
            {
                Item itemS = ItemManager.CreateByName(config.itempot[random.Next(config.itempot.Count)], 1, 0);
                player.GiveItem(itemS, BaseEntity.GiveItemReason.PickedUp);
                ItemRemovalThink(item, player, 1);
                Interface.CallHook("OnSkullOpen", player);
                return false;
            }
            return null;
        }
            }
}
