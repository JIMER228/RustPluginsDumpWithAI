using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System;
using Rust;

namespace Oxide.Plugins
{
    [Info("Drug Business", "Nod Js", "0.44")]
    [Description("A unique drug concept for roleplayers")]
    public class DrugBusiness : RustPlugin
    {
        #region Plugin References
        [PluginReference]
        private Plugin ImageLibrary, Economics, ServerRewards, StackModifier, CustomSkinsStacksFix, Loottable;
        #endregion

        #region Config
        Configuration config;

        class Configuration
        {
            [JsonProperty(PropertyName = "Basic Settings")]
            public BasicSettings basicSettings = new BasicSettings();

            [JsonProperty(PropertyName = "VPN Settings")]
            public VpnSettings vpnSettings = new VpnSettings();

            [JsonProperty(PropertyName = "Drug Settings")]
            public DrugSettings drugSettings = new DrugSettings();

            [JsonProperty(PropertyName = "Drone Settings")]
            public DroneSettings droneSettings = new DroneSettings();

            [JsonProperty(PropertyName = "Crafting Settings")]
            public CraftingSettings craftingSettings = new CraftingSettings();

            [JsonProperty(PropertyName = "Dealer Settings")]
            public DealerSettings dealerSettings = new DealerSettings();

            public class BasicSettings
            {
                [JsonProperty(PropertyName = "Hours From The Dealers Will Be Available")]
                public float hoursFrom = 0f;

                [JsonProperty(PropertyName = "Hours Till The Dealers Will Be Available")]
                public float hoursTill = 24f;

                [JsonProperty(PropertyName = "Loading Screen Update Rate (Seconds) [Don't make it too small]")]
                public float loadingTime = 0.035f;

                [JsonProperty(PropertyName = "Loading Screen Anchor Shifting [Left - Right]")]
                public double anchorShift = 0.001;
            }

            public class VpnSettings
            {
                [JsonProperty(PropertyName = "Item Info")]
                public VPNItem itemInfo = new VPNItem();

                public class VPNItem
                {
                    [JsonProperty(PropertyName = "Display Name")]
                    public string DisplayName = "VPN Flash Drive";

                    [JsonProperty(PropertyName = "Skin ID")]
                    public ulong SkinID = 2645299478;

                    [JsonProperty(PropertyName = "Require In Belt")]
                    public bool RequireInBelt = true;

                    [JsonProperty(PropertyName = "Spawn Command")]
                    public string SpawnCommand = "db.vpn";
                }
            }

            public class DrugSettings
            {
                [JsonProperty(PropertyName = "Use (Economics / Server Rewards / Scraps)")]
                public string use = "economics";

                [JsonProperty(PropertyName = "Currency Symbol")]
                public string currencySymbol = "$";

                [JsonProperty(PropertyName = "Marijuana Seeds")]
                public DrugSeed marijuana = new DrugSeed();

                [JsonProperty(PropertyName = "Peruvian Coca Seeds")]
                public DrugSeed preuvianCoca = new DrugSeed();

                public class DrugSeed
                {
                    [JsonProperty(PropertyName = "Cost per seed")]
                    public double Cost = 40;
                }
            }

            public class DroneSettings
            {
                [JsonProperty(PropertyName = "Drone Spawn Distance (Default: 210)")]
                public int droneSpawnDistance = 210;

                [JsonProperty(PropertyName = "Drone Spawn Height (Default: 78)")]
                public int droneSpawnHeight = 78;

                [JsonProperty(PropertyName = "Drone Delivery Height (Default: 40)")]
                public int droneDeliveryHeight = 40;
            }

            public class CraftingSettings
            {
                [JsonProperty(PropertyName = "Cocaine Leaves Required")]
                public int cocaineLeavesRequired = 5;

                [JsonProperty(PropertyName = "Cocaine Powder Required")]
                public int cocainePowderRequired = 10;

                [JsonProperty(PropertyName = "Cannabis Required")]
                public int cannabisRequired = 5;

                [JsonProperty(PropertyName = "Marijuana Required")]
                public int marijuanaRequired = 10;
            }

            public class DealerSettings
            {
                [JsonProperty(PropertyName = "Maximum Dealers at a time (will pick random from the data)")]
                public int maximumDealers = 3;

                [JsonProperty(PropertyName = "Dealer Name")]
                public string dealerName = "Dealer";

                [JsonProperty(PropertyName = "Announce when the dealer arrive/leave")]
                public bool announce = false;

                [JsonProperty(PropertyName = "Default Outfit", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<ClothInfo> defaultOutfit = new List<ClothInfo>
                {
                    new ClothInfo
                    {
                        Item = "tshirt",
                        Skin = 838212999
                    },

                    new ClothInfo
                    {
                        Item = "pants",
                        Skin = 866948432
                    },

                    new ClothInfo
                    {
                        Item = "hat.beenie",
                        Skin = 814355387
                    },

                    new ClothInfo
                    {
                        Item = "shoes.boots",
                        Skin = 1889769313
                    },

                    new ClothInfo
                    {
                        Item = "mask.bandana",
                        Skin = 2312147084
                    }
                };

                [JsonProperty(PropertyName = "Weed Brick")]
                public Brick weedBrick = new Brick();

                [JsonProperty(PropertyName = "Cocaine Brick")]
                public Brick cocaineBrick = new Brick
                {
                    brickPrice = 160
                };

                public class Brick
                {
                    [JsonProperty(PropertyName = "Bricks Sell Quantity")]
                    public int bricksSellQuantity = 1;

                    [JsonProperty(PropertyName = "Time Taken For Single Trade (Seconds)")]
                    public int timeTaken = 30;

                    [JsonProperty(PropertyName = "Brick Selling Price")]
                    public int brickPrice = 120;
                }
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(config, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Classes and Stored Data
        private PlantsDataModel plantsData;
        private List<DealerInfo> dealersData = new List<DealerInfo>();
        private bool Spawned = false;

        private class PlantsDataModel
        {
            public List<ulong> currentPlants = new List<ulong>();
        }

        private class DealerInfo
        {
            public ulong NetworkID;
            public float PositionX, PositionY, PositionZ;
            public float RotationX, RotationY, RotationZ, RotationW;
            public string DealerName;
            public bool CurrentlyInUse;
            public List<ClothInfo> Outfit = new List<ClothInfo>();
        }

        private class ClothInfo
        {
            public string Item;
            public ulong Skin;
        }

        #endregion

        #region Time Related
        private System.Random random = new System.Random();
        private void Time_OnHour()
        {
            if (inTimeRange())
            {
                if (Spawned || dealersData.IsEmpty())
                    return;

                var randomIndexes = Enumerable.Range(0, dealersData.Count).OrderBy(x => random.Next()).Take(config.dealerSettings.maximumDealers).ToList();
                randomIndexes.ForEach(index =>
                {
                    var dealer = dealersData[index];

                    var npc = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", new Vector3(dealer.PositionX, dealer.PositionY, dealer.PositionZ), new Quaternion(dealer.RotationX, dealer.RotationY, dealer.RotationZ, dealer.RotationW)).ToPlayer();
                    npc.Spawn();

                    npc.displayName = dealer.DealerName;
                    dealer.Outfit.ForEach(outfit => npc.inventory.containerWear.AddItem(ItemManager.FindItemDefinition(outfit.Item), 1, skin: outfit.Skin));
                    npc.EnablePlayerCollider();

                    dealersData[index].NetworkID = npc.net.ID.Value;
                    dealersData[index].CurrentlyInUse = true;
                });

                if (config.dealerSettings.announce && !dealersData.IsEmpty())
                {
                    foreach (var player in BasePlayer.activePlayerList)
                        Message(player, "<color=#c45341>Dealers</color> <color=#fff2d9>working hours have started, and they have arrived. Check the</color> <color=#c45341>BlackMarket</color> <color=#fff2d9>to locate the dealers.</color>");
                }

                Spawned = true;
            }
            else
            {
                var removed = false;

                dealersData.ForEach(dealer =>
                {
                    var oldnpc = BaseNetworkable.serverEntities.Find(new NetworkableId(dealer.NetworkID)) as BasePlayer;
                    if (oldnpc != null)
                    {
                        oldnpc.KillMessage();
                        if (!removed)
                            removed = true;
                    }

                    dealer.CurrentlyInUse = false;
                });

                if (config.dealerSettings.announce && removed)
                {
                    foreach (var player in BasePlayer.activePlayerList)
                        Message(player, "<color=#c45341>Dealers</color> <color=#fff2d9>working hours have ended, and they have left.</color>");
                }

                Spawned = false;
            }

        }
        #endregion

        #region Hooks
        private void Init()
        {
            plantsData = Interface.Oxide.DataFileSystem.ReadObject<PlantsDataModel>(Name + "_plantsData");
            dealersData = Interface.Oxide.DataFileSystem.ReadObject<List<DealerInfo>>(Name + "_dealersData");

            cmd.AddConsoleCommand(config.vpnSettings.itemInfo.SpawnCommand, this, nameof(VPNSpawnConsole));
            cmd.AddChatCommand(config.vpnSettings.itemInfo.SpawnCommand, this, nameof(VPNSpawnChat));
            cmd.AddChatCommand("spawndealer", this, nameof(SpawnDealer));
            cmd.AddChatCommand("deletedealer", this, nameof(DeleteDealer));
            cmd.AddChatCommand("process", this, nameof(Process));

            cmd.AddConsoleCommand("db.availabledealers", this, nameof(AvailableDealers));
            cmd.AddConsoleCommand("db.locate", this, nameof(Locate));
            cmd.AddConsoleCommand("db.reachcheckout", this, nameof(CheckoutPageCommand));
            cmd.AddConsoleCommand("db.begindelivery", this, nameof(DeliveryCommand));
            cmd.AddConsoleCommand("db.inputfieldcmd", this, nameof(InputFieldCommand));
            cmd.AddConsoleCommand("db.updatequantity", this, nameof(UpdateQuantity));
            cmd.AddConsoleCommand("db.processoutput", this, nameof(ProcessOutput));
            cmd.AddConsoleCommand("db.packageoutput", this, nameof(PackageOutput));
            cmd.AddConsoleCommand("db.sellbricks", this, nameof(SellBricks));
            cmd.AddConsoleCommand("db.closeui", this, nameof(CloseUICommand));
        }

        private void OnPluginLoaded(Plugin plugin)
        {
            if (plugin == StackModifier || plugin == CustomSkinsStacksFix || plugin == Loottable)
                Unsubscribe(nameof(OnItemSplit));
        }

        private void OnServerInitialized(bool initial)
        {
            if (config.drugSettings.use.ToLower() == "server rewards" && ServerRewards == null)
                PrintError("Server Reward is configured as the debit plugin and not installed.");

            if (config.drugSettings.use.ToLower() == "economics" && Economics == null)
                PrintError("Economics is configured as the debit plugin and not installed.");

            if (StackModifier != null || CustomSkinsStacksFix != null || Loottable != null)
                Unsubscribe(nameof(OnItemSplit));


            if (ImageLibrary == null)
            {
                PrintError("Drug Business Depends on Image Library, Get it at (https://umod.org/plugins/image-library)");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }

            ImageLibrary.Call("AddImage", "https://i.imgur.com/1oHlscq.jpg", "db_loadScreen");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/9gNVJAY.png", "DB_OrderPage");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/2ZjKeWF.png", "db_marijuanaCheckout");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/SFgD3Bz.png", "db_cocaCheckout");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/vOEEMBb.jpg", "db_deliveryPage");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/6SDKvco.png", "db_processMarijuana");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/YwECSnh.png", "db_processCoca");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/PA1DHJB.png", "db_sellingInterface");

            TOD_Sky.Instance.Components.Time.OnHour += Time_OnHour;

            if (dealersData.IsEmpty())
                return;

            dealersData.ForEach(dealer =>
            {
                var oldnpc = BaseNetworkable.serverEntities.Find(new NetworkableId(dealer.NetworkID)) as BasePlayer;
                if (oldnpc != null)
                    oldnpc.KillMessage();

                dealer.CurrentlyInUse = false;
            });

            if (inTimeRange())
            {
                var randomIndexes = Enumerable.Range(0, dealersData.Count).OrderBy(x => random.Next()).Take(config.dealerSettings.maximumDealers).ToList();
                randomIndexes.ForEach(index =>
                {
                    var dealer = dealersData[index];

                    var npc = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", new Vector3(dealer.PositionX, dealer.PositionY, dealer.PositionZ), new Quaternion(dealer.RotationX, dealer.RotationY, dealer.RotationZ, dealer.RotationW)).ToPlayer();
                    npc.Spawn();

                    npc.displayName = dealer.DealerName;
                    dealer.Outfit.ForEach(outfit => npc.inventory.containerWear.AddItem(ItemManager.FindItemDefinition(outfit.Item), 1, skin: outfit.Skin));
                    npc.EnablePlayerCollider();

                    dealersData[index].NetworkID = npc.net.ID.Value;
                    dealersData[index].CurrentlyInUse = true;
                });

                Spawned = true;
            }

            /*if (!dealersData.IsEmpty())
                SaveData();*/
        }
        private void OnEntityMounted(ComputerStation computerStation, BasePlayer player)
        {
            var HasItem = false;

            if (!config.vpnSettings.itemInfo.RequireInBelt)
            {
                var inventoryItems = player.inventory.AllItems();
                var item = inventoryItems.FirstOrDefault(i => i.skin == config.vpnSettings.itemInfo.SkinID);
                if (item != null)
                    HasItem = true;
            }
            else
            {
                var containerItems = player.inventory.containerBelt.itemList;
                var item = containerItems.FirstOrDefault(i => i.skin == config.vpnSettings.itemInfo.SkinID);
                if (item != null)
                    HasItem = true;
            }

            if (HasItem)
                LoadUI(player);
        }

        private Item OnItemSplit(Item item, int amount)
        {
            if (item.name != null) 
            {
                item.amount -= amount;
                var newItem = ItemManager.Create(item.info, amount, item.skin);
                newItem.name = item.name;
                newItem.MarkDirty();

                return newItem;
            }

            return null;
        }

        private void OnGrowableGathered(GrowableEntity plant, Item item, BasePlayer player)
        {
            if (plantsData.currentPlants.Contains(plant.net.ID.Value))
            {
                if (plant.ShortPrefabName == "hemp.entity")
                {
                    item.name = "Cannabis";
                    item.skin = 2647169612;
                }
                else
                {
                    item.name = "Coca Leaf";
                    item.skin = 2650674189;
                }
            }
        }

        private void OnEntityBuilt(Planner plan, GameObject gameObject)
        {
            var entity = gameObject.ToBaseEntity();
            if (plan.GetItem().skin != 2645313312 && plan.GetItem().skin != 2645313234)
                return;

            plantsData.currentPlants.Add(gameObject.GetComponent<GrowableEntity>().net.ID.Value);
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (!input.WasJustPressed(BUTTON.USE))
                return;

            var dealer = RelationshipManager.GetLookingAtPlayer(player);
            if (dealer == null)
                return;

            if (dealersData.Exists(d => d.NetworkID == dealer.net.ID.Value))
                SellingUI(player);

        }

        private object CanBeWounded(BasePlayer player)
        {
            if (dealersData.Exists(dealer => dealer.NetworkID == player.net.ID.Value))
                return true;

            return null;
        }

        private object CanLootPlayer(BasePlayer target) => CanBeWounded(target);

        private void OnEntityTakeDamage(BasePlayer player, HitInfo info)
        {
            if (!dealersData.Exists(dealer => dealer.NetworkID == player.net.ID.Value))
                return;

            info.damageTypes = new DamageTypeList();
            info.HitMaterial = 0;
            info.PointStart = Vector3.zero;
        }

        private object CanNpcAttack(BaseNpc npc, BasePlayer target)
        {
            if (dealersData.Exists(dealer => dealer.NetworkID == target.net.ID.Value))
                return false;

            return null;
        }

        private void Unload()
        {
            TOD_Sky.Instance.Components.Time.OnHour -= Time_OnHour;

            dealersData.ForEach(dealer =>
            {
                var oldnpc = BaseNetworkable.serverEntities.Find(new NetworkableId(dealer.NetworkID)) as BasePlayer;
                if (oldnpc != null)
                    oldnpc.KillMessage();

                dealer.CurrentlyInUse = false;
            });

            SaveData();
        }

        private void OnServerSave() => SaveData();

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name + "_plantsData", plantsData);
            Interface.Oxide.DataFileSystem.WriteObject(Name + "_dealersData", dealersData);
        }
        #endregion

        #region Commands
        private void VPNSpawnConsole(ConsoleSystem.Arg arg) => VPNSpawnChat(arg.Player(), arg.cmd.FullName, arg.Args);

        private void VPNSpawnChat(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;
            
            if (!player.IsAdmin)
                return;

            var item = ItemManager.CreateByItemID(609049394, skin: config.vpnSettings.itemInfo.SkinID);
            item.name = config.vpnSettings.itemInfo.DisplayName;

            player.GiveItem(item);
        }

        private void SpawnDealer(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
                return;

            /*if (config.dealerSettings.maximumDealers <= dealersData.Count(d => d.CurrentlyInUse))
            {
                for (int i = 0; i < dealersData.Count - 1; i++)
                {
                    var dealer = dealersData[i];
                    if (!dealer.CurrentlyInUse)
                        break;

                    var entity = BaseNetworkable.serverEntities.Find(new NetworkableId(dealer.NetworkID)) as BasePlayer;
                    if (entity != null)
                        entity.KillMessage();

                    dealersData[i].CurrentlyInUse = false;
                    Message(player, "New Dealer Spawned, Replaced with another one since the dealer limit was " + config.dealerSettings.maximumDealers);
                    break;
                }
            }*/

            var npc = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", player.transform.position, player.transform.rotation).ToPlayer();
            npc.Spawn();

            npc.displayName = args.Count() == 1 ? args[0] : config.dealerSettings.dealerName;
            config.dealerSettings.defaultOutfit.ForEach(outfit => npc.inventory.containerWear.AddItem(ItemManager.FindItemDefinition(outfit.Item), 1, skin: outfit.Skin));
            npc.EnablePlayerCollider();

            dealersData.Add(new DealerInfo
            {
                NetworkID = npc.net.ID.Value,
                PositionX = npc.transform.position.x, PositionY = npc.transform.position.y, PositionZ = npc.transform.position.z,
                RotationX = npc.transform.rotation.x, RotationY = npc.transform.rotation.y, RotationZ = npc.transform.rotation.z, RotationW = npc.transform.rotation.w,
                DealerName = args.Count() == 1 ? args[0] : config.dealerSettings.dealerName,
                CurrentlyInUse = true,
                Outfit = config.dealerSettings.defaultOutfit
            });

            Spawned = true;
            /*SaveData();*/
        }

        private void DeleteDealer(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
                return;

            var dealer = RelationshipManager.GetLookingAtPlayer(player);
            if (dealersData.Exists(d => d.NetworkID == dealer.net.ID.Value))
            {
                var dealerIndex = dealersData.FindIndex(d => d.NetworkID == dealer.net.ID.Value);
                if (dealerIndex != -1)
                {
                    if (args.Count() == 1 && args[0] == "perma")
                        dealersData.RemoveAt(dealerIndex);
                    else
                        dealersData[dealerIndex].CurrentlyInUse = false;
                }

                dealer.KillMessage();
            }
            else
            {
                Message(player, "<color=#fff2d9> Not looking at any</color> <color=#c45341>dealer!</color>");
            }

            /*SaveData();*/
        }

        private void Process(BasePlayer player, string command, string[] args)
        {
            var ray = new Ray(player.eyes.position, player.eyes.HeadForward());
            var entity = FindObject(ray, 2);
            if (entity != null)
            {
                if (entity.ShortPrefabName == "workbench3.deployed")
                    ProcessUI(player, "marijuana");
                else if (entity.ShortPrefabName == "mixingtable.deployed")
                    ProcessUI(player, "coca");
            }
            else
            {
                Message(player, "<color=#fff2d9>Not near a </color><color=#c45341>high tier workbench</color><color=#fff2d9> or a </color><color=#c45341>mixing table!</color>");
            }
        }
        #endregion

        #region UI

        private readonly string ParentElement = "db_UI";

        #region Loading Screen
        private void LoadUI(BasePlayer player)
        {
            var elementContainer = new CuiElementContainer();
            elementContainer.Add(new CuiElement
            {
                Name = ParentElement,
                Parent = "Overlay",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "db_loadScreen")
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },

                    new CuiNeedsCursorComponent{}
                }
            });

            AddProgressBar(player);

            CuiHelper.DestroyUi(player, ParentElement);
            CuiHelper.AddUi(player, elementContainer);
        }

        private void AddProgressBar(BasePlayer player)
        {
            var progressBarElement = "db_ProgressBar";
            Timer progressBar = null;

            var elementContainer = new CuiElementContainer();
            var currentAnchor = 0.385;

            progressBar = timer.Every(config.basicSettings.loadingTime, () =>
            {
                if (currentAnchor >= 0.615)
                {
                    progressBar.Destroy();
                    CuiHelper.DestroyUi(player, progressBarElement);

                    OrderPage(player);
                    return;
                }

                elementContainer.Clear();

                elementContainer.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0.9529 0.4705 0 1",
                    },

                    RectTransform =
                    {
                        AnchorMin = "0.386 0.05",
                        AnchorMax = $"{currentAnchor} 0.061"
                    },
                }, "Overlay", progressBarElement);

                currentAnchor = currentAnchor + config.basicSettings.anchorShift;

                CuiHelper.DestroyUi(player, progressBarElement);
                CuiHelper.AddUi(player, elementContainer);
            });
        }
        #endregion

        #region OrderPage
        private void OrderPage(BasePlayer player, string dealerLocations = "")
        {
            var elementContainer = new CuiElementContainer();
            elementContainer.Add(new CuiElement
            {
                Name = ParentElement,
                Parent = "Overlay",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "DB_OrderPage")
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },

                    new CuiNeedsCursorComponent{}
                }
            });

            elementContainer.Add(new CuiLabel
            {
                Text =
                {
                    Text = config.drugSettings.currencySymbol + config.drugSettings.marijuana.Cost + GetDecimalAmount(config.drugSettings.marijuana.Cost),
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter
                },

                RectTransform =
                {
                    AnchorMin = "0.2942708 0.7694389",
                    AnchorMax = "0.390625 0.8416666"
                }

            }, ParentElement);

            elementContainer.Add(new CuiLabel
            {
                Text =
                {
                    Text = config.drugSettings.currencySymbol + config.drugSettings.preuvianCoca.Cost + GetDecimalAmount(config.drugSettings.preuvianCoca.Cost),
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter
                },

                RectTransform =
                {
                    AnchorMin = "0.2942708 0.3296316",
                    AnchorMax = "0.390625 0.4018616"
                }

            }, ParentElement);

            elementContainer.Add(new CuiButton
            {
                Button =
                {
                    Color = "1 1 1 0",
                    Command = "db.reachcheckout marijuana"
                },

                Text =
                {
                    Text = string.Empty
                },

                RectTransform =
                {
                    AnchorMin = "0.2942708 0.7046242",
                    AnchorMax = "0.390625 0.7666593"
                }

            }, ParentElement);

            elementContainer.Add(new CuiButton // Coca
            {
                Button =
                {
                    Color = "1 1 1 0",
                    Command = "db.reachcheckout coca"
                },

                Text =
                {
                    Text = string.Empty
                },

                RectTransform =
                {
                    AnchorMin = "0.2942708 0.2666667",
                    AnchorMax = "0.390625 0.3287037"
                }

            }, ParentElement);

            elementContainer.Add(new CuiButton
            {
                Button =
                {
                    Color = "1 1 1 0",
                    Command = "db.availabledealers"
                },

                Text =
                {
                    Text = string.Empty
                },

                RectTransform =
                {
                    AnchorMin = "0.02135418 0.03703703",
                    AnchorMax = "0.1541667 0.08611112"
                }

            }, ParentElement);

            elementContainer.Add(new CuiLabel
            {
                Text =
                {
                    Text = dealerLocations,
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = "0 0 0 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.1546875 0.03611111",
                    AnchorMax = "0.3541667 0.08611111"
                }

            }, ParentElement);

            elementContainer.Add(new CuiButton
            {
                Button =
                {
                    Color = "1 1 1 0",
                    Command = "db.locate"
                },

                Text =
                {
                    Text = string.Empty
                },

                RectTransform =
                {
                    AnchorMin = "0.3546875 0.03703704",
                    AnchorMax = "0.4359375 0.08611111"
                }

            }, ParentElement);

            elementContainer.Add(new CuiButton
            {
                Button =
                {
                    Color = "1 1 1 0",
                    Command = "db.closeui"
                },

                Text =
                {
                    Text = string.Empty
                },

                RectTransform =
                {
                    AnchorMin = "0.981 0.965",
                    AnchorMax = "0.993 0.986"
                }

            }, ParentElement);

            CuiHelper.DestroyUi(player, ParentElement);
            CuiHelper.AddUi(player, elementContainer);
        }
        #endregion

        #region CheckoutPage
        private void CheckoutPage(BasePlayer player, string image, double Cost, double TotalCost = 0.0, string input = "1")
        {
            if (TotalCost == 0.0)
                TotalCost = Cost;

            var elementContainer = new CuiElementContainer();
            elementContainer.Add(new CuiElement
            {
                Name = ParentElement,
                Parent = "Overlay",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", image)
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },

                    new CuiNeedsCursorComponent{}
                }
            });

            elementContainer.Add(new CuiLabel
            {
                Text =
                {
                    Text = player.displayName.ToUpper(),
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                },

                RectTransform =
                {
                    AnchorMin = "0.07083321 0.5027779",
                    AnchorMax = "0.2807293 0.5379629"
                }

            }, ParentElement);

            elementContainer.Add(new CuiLabel
            {
                Text =
                {
                    Text = $"{player.UserIDString.Substring(0, 4)} {player.UserIDString.Substring(4, 4)} {player.UserIDString.Substring(8, 4)} {player.UserIDString.Substring(12, 4)}",
                    FontSize = 32,
                    Align = TextAnchor.MiddleCenter,
                    Font = "RobotoCondensed-Regular.ttf"
                },

                RectTransform =
                {
                    AnchorMin = "0.05104166 0.5712963",
                    AnchorMax = "0.3458333 0.6259259"
                }

            }, ParentElement);

            elementContainer.Add(new CuiLabel
            {
                Text =
                {
                    Text = config.drugSettings.currencySymbol + GetBalance(player),
                    FontSize = 24,
                    Align = TextAnchor.MiddleLeft
                },

                RectTransform =
                {
                    AnchorMin = "0.7869791 0.925",
                    AnchorMax = "0.9473958 0.9768519"
                }

            }, ParentElement);

            elementContainer.Add(new CuiButton
            {
                Button =
                {
                    Color = "1 1 1 0",
                    Command = "db.closeui"
                },

                Text =
                {
                    Text = string.Empty
                },

                RectTransform =
                {
                    AnchorMin = "0.9713542 0.9509259",
                    AnchorMax = "0.9911458 0.9842593"
                }

            }, ParentElement);

            elementContainer.Add(new CuiElement
            {
                Parent = ParentElement,
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        CharsLimit = 3,
                        FontSize = 20,
                        Command = "db.inputfieldcmd " + image,
                        Text = input
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.7374973 0.7092593",
                        AnchorMax = "0.8401014 0.7453704"
                    }
                }
            });

            elementContainer.Add(new CuiButton
            {
                Button =
                {
                    Color = "1 1 1 0",
                    Command = $"db.begindelivery {TotalCost} {image}"
                },

                Text =
                {
                    Text = string.Empty
                },

                RectTransform =
                {
                    AnchorMin = "0.7760416 0.05",
                    AnchorMax = "0.95 0.1111111"
                }

            }, ParentElement);

            elementContainer.Add(new CuiLabel
            {
                Text =
                {
                    Text = config.drugSettings.currencySymbol + Cost + GetDecimalAmount(Cost),
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Font = "RobotoCondensed-Regular.ttf"
                },

                RectTransform =
                {
                    AnchorMin = "0.8447873 0.7092593",
                    AnchorMax = "0.9473915 0.7453704"
                }

            }, ParentElement);

            elementContainer.Add(new CuiLabel
            {
                Text =
                {
                    Text = config.drugSettings.currencySymbol + TotalCost + GetDecimalAmount(TotalCost),
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter,
                    Font = "RobotoCondensed-Regular.ttf"
                },

                RectTransform =
                {
                    AnchorMin = "0.8447917 0.3583333",
                    AnchorMax = "0.9494792 0.425"
                }

            }, ParentElement);

            CuiHelper.DestroyUi(player, ParentElement);
            CuiHelper.AddUi(player, elementContainer);
        }
        #endregion

        #region DeliveryPage
        private void DeliveryPage(BasePlayer player)
        {
            var elementcontainer = new CuiElementContainer();
            elementcontainer.Add(new CuiElement
            {
                Name = ParentElement,
                Parent = "Overlay",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "db_deliveryPage")
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },

                    new CuiNeedsCursorComponent()
                }
            });

            CuiHelper.DestroyUi(player, ParentElement);
            CuiHelper.AddUi(player, elementcontainer);

            timer.Once(8f, () =>
            {
                CloseUI(player);
                Message(player, "<color=#8ac763>Order Purchased</color><color=#fff2d9>,The Drone is on the way!</color>");
            });
        }
        #endregion

        #region ProcessUI
        private void ProcessUI (BasePlayer player, string type = "marijuana", int processQuantity = 1, int packagingQuantity = 1)
        {
            var elementcontainer = new CuiElementContainer();
            elementcontainer.Add(new CuiElement
            {
                Name = ParentElement,
                Parent = "Overlay",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = type == "marijuana" ? (string) ImageLibrary.Call("GetImage", "db_processMarijuana") : (string) ImageLibrary.Call("GetImage", "db_processCoca")
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },

                    new CuiNeedsCursorComponent()
                }
            });

            elementcontainer.Add(new CuiLabel
            {
                Text =
                {
                    Text = processQuantity.ToString(),
                    FontSize = 13,
                    Align = TextAnchor.MiddleCenter,
                },

                RectTransform =
                {
                    AnchorMin = "0.6 0.4888889",
                    AnchorMax = "0.7125 0.5157408"
                }
            }, ParentElement);

            elementcontainer.Add(new CuiButton //WeedP +
            {
                Button =
                {
                    Color = "1 1 1 0",
                    Command = $"db.updatequantity process {processQuantity} + {type}"
                },

                Text =
                {
                    Text = string.Empty
                },

                RectTransform =
                {
                    AnchorMin = "0.5859375 0.4916667",
                    AnchorMax = "0.5984375 0.5138889"
                }

            }, ParentElement);

            elementcontainer.Add(new CuiButton //WeedP -
            {
                Button =
                {
                    Color = "1 1 1 0",
                    Command = $"db.updatequantity process {processQuantity} - {type}"
                },

                Text =
                {
                    Text = string.Empty
                },

                RectTransform =
                {
                    AnchorMin = "0.7140625 0.4916667",
                    AnchorMax = "0.7265624 0.513889"
                }

            }, ParentElement);

            elementcontainer.Add(new CuiLabel
            {
                Text =
                {
                    Text = packagingQuantity.ToString(),
                    FontSize = 13,
                    Align = TextAnchor.MiddleCenter,
                },

                RectTransform =
                {
                    AnchorMin = "0.7947916 0.4916667",
                    AnchorMax = "0.9093749 0.5129629"
                }
            }, ParentElement);

            elementcontainer.Add(new CuiButton //WeedB +
            {
                Button =
                {
                    Color = "1 1 1 0",
                    Command = $"db.updatequantity packaging {packagingQuantity} + {type}"
                },

                Text =
                {
                    Text = string.Empty
                },

                RectTransform =
                {
                    AnchorMin = "0.78125 0.4916667",
                    AnchorMax = "0.79375 0.513889"
                }

            }, ParentElement);

            elementcontainer.Add(new CuiButton //WeedB -
            {
                Button =
                {
                    Color = "1 1 1 0",
                    Command = $"db.updatequantity packaging {packagingQuantity} - {type}"
                },

                Text =
                {
                    Text = string.Empty
                },

                RectTransform =
                {
                    AnchorMin = "0.9093751 0.4916667",
                    AnchorMax = "0.9218751 0.513889"
                }

            }, ParentElement);

            elementcontainer.Add(new CuiLabel
            {
                Text =
                {
                    Text = type == "marijuana" ? $"x{processQuantity * config.craftingSettings.cannabisRequired} Cannabis" : $"x{processQuantity * config.craftingSettings.cocaineLeavesRequired} Coca Leaves",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                },

                RectTransform =
                {
                    AnchorMin = "0.5854167 0.4037037",
                    AnchorMax = "0.7270833 0.4398148"
                }
            }, ParentElement);

            elementcontainer.Add(new CuiLabel
            {
                Text =
                {
                    Text = type == "marijuana" ? $"x{packagingQuantity * config.craftingSettings.marijuanaRequired} Marijuana" : $"x{packagingQuantity * config.craftingSettings.cocainePowderRequired} Cocaine Powder",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                },

                RectTransform =
                {
                    AnchorMin = "0.7807262 0.4037037",
                    AnchorMax = "0.9223928 0.4398148"
                }
            }, ParentElement);

            elementcontainer.Add(new CuiButton
            {
                Button =
                {
                    Color = "1 1 1 0",
                    Command = $"db.processoutput {type} {processQuantity}"
                },

                Text =
                {
                    Text = string.Empty
                },

                RectTransform =
                {
                    AnchorMin = "0.5859375 0.2805555",
                    AnchorMax = "0.7260417 0.3138889"
                }

            }, ParentElement);

            elementcontainer.Add(new CuiButton
            {
                Button =
                {
                    Color = "1 1 1 0",
                    Command = $"db.packageoutput {type} {packagingQuantity}"
                },

                Text =
                {
                    Text = string.Empty
                },

                RectTransform =
                {
                    AnchorMin = "0.781247 0.2805555",
                    AnchorMax = "0.9213512 0.3138889"
                }

            }, ParentElement);

            elementcontainer.Add(new CuiButton
            {
                Button =
                {
                    Color = "1 1 1 0",
                    Close = ParentElement
                },

                Text =
                {
                    Text = string.Empty
                },

                RectTransform =
                {
                    AnchorMin = "0.9354167 0.7564815",
                    AnchorMax = "0.9458333 0.7666665"
                }

            }, ParentElement);

            CuiHelper.DestroyUi(player, ParentElement);
            CuiHelper.AddUi(player, elementcontainer);
        }
        #endregion

        #region SellingUI
        private void SellingUI(BasePlayer player)
        {
            var elementcontainer = new CuiElementContainer();
            elementcontainer.Add(new CuiElement
            {
                Name = ParentElement,
                Parent = "Overlay",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "db_sellingInterface")
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },

                    new CuiNeedsCursorComponent()
                }
            });

            elementcontainer.Add(new CuiLabel
            {
                Text =
                {
                    Text = "$" + config.dealerSettings.weedBrick.brickPrice,
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                },

                RectTransform =
                {
                    AnchorMin = "0.7869791 0.2824074",
                    AnchorMax = "0.821875 0.3175926"
                }
            }, ParentElement);

            elementcontainer.Add(new CuiLabel
            {
                Text =
                {
                    Text = "$" + config.dealerSettings.cocaineBrick.brickPrice,
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                },

                RectTransform =
                {
                    AnchorMin = "0.7869791 0.2148146",
                    AnchorMax = "0.821875 0.2499998"
                }
            }, ParentElement);

            elementcontainer.Add(new CuiButton // weed sell
            {
                Button =
                {
                    Color = "1 1 1 0",
                    Command = "db.sellbricks weed"
                },

                Text =
                {
                    Text = string.Empty
                },

                RectTransform =
                {
                    AnchorMin = "0.9213542 0.2824074",
                    AnchorMax = "0.9427083 0.3175926"
                }

            }, ParentElement);

            elementcontainer.Add(new CuiButton // coca sell
            {
                Button =
                {
                    Color = "1 1 1 0",
                    Command = "db.sellbricks cocaine"
                },

                Text =
                {
                    Text = string.Empty
                },

                RectTransform =
                {
                    AnchorMin = "0.9213542 0.2148146",
                    AnchorMax = "0.9427083 0.2499998"
                }

            }, ParentElement);

            elementcontainer.Add(new CuiButton // close
            {
                Button =
                {
                    Color = "1 1 1 0",
                    Close = ParentElement
                },

                Text =
                {
                    Text = string.Empty
                },

                RectTransform =
                {
                    AnchorMin = "0.8447917 0.1342593",
                    AnchorMax = "0.8854167 0.1546296"
                }

            }, ParentElement);

            CuiHelper.DestroyUi(player, ParentElement);
            CuiHelper.AddUi(player, elementcontainer);
        }
        #endregion

        #region CloseUI
        private void CloseUI (BasePlayer player)
        {
            player.EnsureDismounted();
            CuiHelper.DestroyUi(player, ParentElement);
        }
        #endregion

        #endregion

        #region UI Commands
        private void CheckoutPageCommand(ConsoleSystem.Arg arg)
        {
            var checkoutPage = arg.Args[0];
            if (checkoutPage != null)
            {
                var player = arg.Player();
                if (player != null)
                {
                    if (checkoutPage == "marijuana")
                        CheckoutPage(player, "db_marijuanaCheckout", config.drugSettings.marijuana.Cost);
                    else if (checkoutPage == "coca")
                        CheckoutPage(player, "db_cocaCheckout", config.drugSettings.preuvianCoca.Cost);
                }
            }
        }

        private void InputFieldCommand(ConsoleSystem.Arg arg)
        {
            var input = arg.Args[1];
            if (Regex.IsMatch(input, @"^\d+$"))
            {
                var player = arg.Player();
                if (arg.Args[0] == "db_marijuanaCheckout")
                    CheckoutPage(player, "db_marijuanaCheckout", config.drugSettings.marijuana.Cost, config.drugSettings.marijuana.Cost * int.Parse(input), input);
                else
                    CheckoutPage(player, "db_cocaCheckout", config.drugSettings.preuvianCoca.Cost, config.drugSettings.preuvianCoca.Cost * int.Parse(input), input);
            }
        }

        private void DeliveryCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null)
            {
                var cost = arg.Args[0];
                if (cost != null)
                {
                    object success = false;

                    var costInt = int.Parse(cost);
                    switch (config.drugSettings.use.ToLower())
                    {
                        case "economics":
                            success = Economics?.Call("Withdraw", player.userID, double.Parse(cost));
                            break;
                        case "server rewards":
                            var points = ServerRewards?.Call("CheckPoints", player.userID);
                            if (points != null && (int) points >= costInt)
                                success = ServerRewards?.Call("TakePoints", player.userID, costInt);

                            success = false;
                            break;
                        case "scraps":
                            if (RemoveItem(player, "scrap", int.Parse(cost)))
                                success = true;

                            /*var itemScrap = player.inventory.FindItemID("scrap");
                            if (itemScrap != null && itemScrap.amount >= costInt)
                            {
                                itemScrap.UseItem(costInt);
                                success = true;
                            }*/

                            break;
                    }

                    if (success != null && ((bool) success))
                    {
                        if (arg.Args[1] == "db_marijuanaCheckout")
                            BeginDelivery(player, ItemManager.FindItemDefinition("seed.hemp"), double.Parse(cost) / config.drugSettings.marijuana.Cost, 2645313312, "Cannabis Seed");
                        else
                            BeginDelivery(player, ItemManager.FindItemDefinition("seed.red.berry"), double.Parse(cost) / config.drugSettings.preuvianCoca.Cost, 2645313234, "Coca Seed");

                        DeliveryPage(player);
                    }
                }
            }
        }

        private void CloseUICommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null)
            {
                CloseUI(player);
            }
        }

        private void UpdateQuantity(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            var currentQuantity = int.Parse(arg.Args[1]);

            if (arg.Args[2] == "+" && currentQuantity < 30)
                currentQuantity++;
            else if (arg.Args[2] == "-" && currentQuantity > 1)
                currentQuantity--;

            if (arg.Args[0] == "process")
            {
                ProcessUI(player, arg.Args[3], currentQuantity);
            }
            else
            {
                ProcessUI(player, arg.Args[3], packagingQuantity: currentQuantity);
            }
        }

        private void ProcessOutput(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            if (arg.Args[0] == "marijuana")
            {
                if (RemoveItem(player, "cannabis", int.Parse(arg.Args[1]) * config.craftingSettings.cannabisRequired))
                {
                    Message(player, "<color=#fff2d9>Processing</color> <color=#8ac763>Cannabis...</color>");
                    timer.In(3f, () =>
                    {
                        /*ProcessUI(player);*/
                        Message(player, $"<color=#fff2d9>Processed x{int.Parse(arg.Args[1]) * config.craftingSettings.cannabisRequired}</color> <color=#8ac763>Cannabis</color>");

                        var item = ItemManager.CreateByItemID(-804769727, int.Parse(arg.Args[1]), 2647175655); // fiber
                        item.name = "Marijuana";
                        player.GiveItem(item);

                        Message(player, $"<color=#fff2d9>x{arg.Args[1]} Marijuana</color> <color=#8ac763>Recieved...</color>");
                    });
                }
                else
                {
                    Message(player, $"<color=#fff2d9>You're missing </color> <color=#c45341>required items!</color>");
                }
            }
            else
            {
                if (RemoveItem(player, "coca leaf", int.Parse(arg.Args[1]) * config.craftingSettings.cocaineLeavesRequired))
                {
                    Message(player, "<color=#fff2d9>Processing</color> <color=#8ac763>Coca Leaves...</color>");
                    timer.In(3f, () =>
                    {
                        /*ProcessUI(player, "coca");*/
                        Message(player, $"<color=#fff2d9>Processed</color> x{int.Parse(arg.Args[1]) * config.craftingSettings.cocaineLeavesRequired} <color=#8ac763>Coca Leaves</color>");

                        var item = ItemManager.CreateByItemID(204391461, int.Parse(arg.Args[1]), 2650677009); // coal
                        item.name = "Cocaine Powder";
                        player.GiveItem(item);

                        Message(player, $"<color=#fff2d9>x{arg.Args[1]}</color> <color=#8ac763>Cocaine Powder</color> <color=#fff2d9>Recieved...</color>");
                    });
                }
                else
                {
                    Message(player, $"<color=#fff2d9>You're missing</color> <color=#c45341>required items!</color>");
                }
            }
        }

        private void PackageOutput(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            if (arg.Args[0] == "marijuana")
            {
                if (RemoveItem(player, "marijuana", int.Parse(arg.Args[1]) * config.craftingSettings.marijuanaRequired))
                {
                    Message(player, "<color=#fff2d9>Packaging</color> <color=#8ac763>Marijuana...</color>");
                    timer.In(6f, () =>
                    {
                        /*ProcessUI(player);*/
                        Message(player, $"<color=#fff2d9>Packaged x{int.Parse(arg.Args[1]) * config.craftingSettings.marijuanaRequired}</color> <color=#8ac763>Marijuana</color>");

                        var item = ItemManager.CreateByItemID(204391461, int.Parse(arg.Args[1]), 2647293042); //coal
                        item.name = "Weed Brick";
                        player.GiveItem(item, BaseEntity.GiveItemReason.Crafted);

                        Message(player, $"<color=#fff2d9>x{arg.Args[1]}</color> <color=#8ac763>Weed Brick</color> <color=#fff2d9>Recieved...</color>");
                    });
                }
                else
                {
                    Message(player, $"<color=#fff2d9>You're missing</color> <color=#c45341>required items!</color>");
                }
            }
            else
            {
                if (RemoveItem(player, "cocaine powder", int.Parse(arg.Args[1]) * config.craftingSettings.cocainePowderRequired))
                {
                    Message(player, "<color=#fff2d9>Packaging</color> <color=#8ac763>Cocaine Powder...</color>");
                    timer.In(6f, () =>
                    {
                        /*ProcessUI(player, "coca");*/
                        Message(player, $"<color=#fff2d9>Packaged x{int.Parse(arg.Args[1]) * config.craftingSettings.cocainePowderRequired}</color> <color=#8ac763>Cocaine Powder</color>");

                        var item = ItemManager.CreateByItemID(204391461, int.Parse(arg.Args[1]), 2650676937); //coal
                        item.name = "Cocaine Brick";
                        player.GiveItem(item);

                        Message(player, $"<color=#fff2d9>x{arg.Args[1]}</color> <color=#8ac763>Cocaine Brick</color> <color=#fff2d9>Recieved...</color>");
                    });
                }
                else
                {
                    Message(player, $"<color=#fff2d9>You're missing</color> <color=#c45341>required items!</color>");
                }
            }
        }

        private void SellBricks(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            var type = arg.Args[0];

            if (type == "weed")
            {
                if (RemoveItem(player, "weed brick", config.dealerSettings.weedBrick.bricksSellQuantity))
                {
                    Message(player, "<color=#fff2d9>Handing out the</color> <color=#8ac763>weed brick!</color>");
                    timer.In(2f, () => Message(player, "<color=#fff2d9>Dealer's checking the</color> <color=#8ac763>weed brick</color> <color=#fff2d9>quality - (please wait a while!)</color>"));

                    timer.In(10f, () =>
                    {
                        AddMoney(player, config.dealerSettings.weedBrick.brickPrice);
                        Message(player, "<color=#8ac763>Weed Brick</color> <color=#fff2d9>Sold!</color>");
                    });
                }
                else
                {
                    CuiHelper.DestroyUi(player, ParentElement);
                    Message(player, "<color=#fff2d9> You don't have any</color> <color=#c45341>weed brick!</color>");
                }
            }
            else
            {
                if (RemoveItem(player, "cocaine brick", config.dealerSettings.cocaineBrick.bricksSellQuantity))
                {
                    Message(player, "<color=#fff2d9>Handing out the</color> <color=#8ac763>cocaine brick!</color>");
                    timer.In(2f, () => Message(player, "<color=#fff2d9>Dealer's checking the</color> <color=#8ac763>cocaine brick</color> <color=#fff2d9>quality - (please wait a while!)</color>"));

                    timer.In(10f, () =>
                    {
                        AddMoney(player, config.dealerSettings.cocaineBrick.brickPrice);
                        Message(player, "<color=#8ac763>Cocaine Brick</color> <color=#fff2d9>Sold!</color>");
                    });
                }
                else
                {
                    CuiHelper.DestroyUi(player, ParentElement);
                    Message(player, "<color=#fff2d9>You don't have any</color> <color=#c45341>cocaine brick</color>");
                }
            }
        }

        private void AvailableDealers(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            var dealersLocation = string.Empty;

            if (inTimeRange())
            {
                foreach (var dealer in dealersData)
                {
                    if (dealer.CurrentlyInUse)
                        dealersLocation += $"{GetGrid(dealer.PositionX, dealer.PositionY, dealer.PositionZ)} | ";
                }
            }
            else
                dealersLocation = "No dealers available!";

            OrderPage(player, dealersLocation);
        }

        private void Locate(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            var anyAvailable = false;

            dealersData.ForEach(dealer =>
            {
                if (dealer.CurrentlyInUse)
                {
                    MapMarkerGenericRadius mapMarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", new Vector3(dealer.PositionX, dealer.PositionY, dealer.PositionZ)) as MapMarkerGenericRadius;

                    if (mapMarker != null)
                    {
                        mapMarker.alpha = 1f;
                        if (!ColorUtility.TryParseHtmlString("#8ac763", out mapMarker.color1))
                        {
                            mapMarker.color1 = Color.black;
                            PrintError($"Invalid map marker color1");
                        }

                        if (!ColorUtility.TryParseHtmlString("#8ac763", out mapMarker.color2))
                        {
                            mapMarker.color2 = Color.white;
                            PrintError($"Invalid map marker color2");
                        }

                        mapMarker.name = dealer.DealerName;
                        mapMarker.radius = 0.2f;
                        /*npcMarkers.Add(mapMarker);*/
                        mapMarker.Spawn();
                        mapMarker.SendUpdate();

                        if (!anyAvailable)
                            anyAvailable = true;
                    }

                    timer.In(60f, () => mapMarker?.KillMessage());
                }
            });

            CloseUI(player);
            if (anyAvailable)
                Message(player, "<color=#fff2d9>All available dealers</color> <color=#8ac763>located!</color>");
            else
                Message(player, "<color=#fff2d9>Dealers</color> <color=#c45341>Unavailable!</color>");
        }
        #endregion

        #region Drone Side
        private void BeginDelivery(BasePlayer player, ItemDefinition itemInfo, double quantity, ulong skinID, string name)
        {
            var baseMountable = player.GetMounted();
            if (baseMountable != null)
            {
                var droneSpawnPoint = new Vector3(baseMountable.ServerPosition.x, baseMountable.ServerPosition.y + config.droneSettings.droneSpawnHeight, baseMountable.ServerPosition.z + config.droneSettings.droneSpawnDistance);
                var deliveryPoint = new Vector3(baseMountable.ServerPosition.x, baseMountable.ServerPosition.y + config.droneSettings.droneDeliveryHeight, baseMountable.ServerPosition.z);

                var drone = (Drone)GameManager.server.CreateEntity("assets/prefabs/deployable/drone/drone.deployed.prefab", droneSpawnPoint);
                drone.yawSpeed = 1f;
                drone.uprightSpeed = 1f;
                drone.targetPosition = deliveryPoint;

                drone.GetComponentInChildren<Collider>().enabled = false;
                drone.Spawn();

                Timer TT = null;
                var Returning = false;
                TT = timer.Every(1f, () =>
                {
                    if (!Returning && Math.Floor(Vector3.Distance(drone.ServerPosition, deliveryPoint)) == 0)
                    {
                        var package = ItemManager.CreateByItemID(204970153);
                        package.name = "Package";
                        package.contents.AddItem(itemInfo, Convert.ToInt32(quantity), skinID);
                        package.contents.itemList[0].name = name;
                        package.Drop(drone.ServerPosition, Vector3.zero);

                        drone.targetPosition = droneSpawnPoint;
                        Returning = true;
                    }
                    else if (Returning && Math.Floor(Vector3.Distance(drone.ServerPosition, droneSpawnPoint)) == 0)
                    {
                        drone.Kill();
                        TT.Destroy();
                    }
                });
            }
        }
        #endregion

        #region Helpers
        private void AddMoney(BasePlayer player, int amount)
        {
            switch (config.drugSettings.use.ToLower())
            {
                case "economics":
                    Economics?.Call("Deposit", player.userID, Convert.ToDouble(amount));
                    break;
                case "server rewards":
                    ServerRewards?.Call("AddPoints", player.userID, amount);
                    break;
                case "scraps":
                    player.GiveItem(ItemManager.CreateByName("scrap", amount));
                    break;
            }
        }

        private bool RemoveItem(BasePlayer player, string item, int count)
        {
            /*var itemFound = player.inventory.AllItems().FirstOrDefault(i => i.name != null && i.name.ToLower() == item && i.amount >= count);
            if (itemFound != null)
            {
                *//*itemFound.amount -= count;*//*
                itemFound.UseItem(count);
                return true;
            }

            return false;*/

            var itemAmount = 0;
            player.inventory.AllItems().ToList().ForEach(i =>
            {
                if ((string.IsNullOrEmpty(i.name) && i.info.shortname == item) || (!string.IsNullOrEmpty(i.name) && i.name.ToLower() == item))
                    itemAmount = itemAmount + i.amount;
            });
            
            if (itemAmount >= count)
            {
                foreach (var i in player.inventory.AllItems())
                {
                    if ((string.IsNullOrEmpty(i.name) && i.info.shortname == item) || (!string.IsNullOrEmpty(i.name) && i.name.ToLower() == item))
                    {
                        if (i.amount >= count)
                        {
                            i.UseItem(count);
                            break;
                        }
                        else if (i.amount < count)
                        {
                            count = count - i.amount;
                            i.UseItem(i.amount);
                        }
                    }
                }

                return true;
            }

            return false;
        }

        private string GetBalance(BasePlayer player)
        {
            switch (config.drugSettings.use.ToLower())
            {
                case "economics":
                    var balance = (double)Economics?.Call("Balance", player.userID);
                    return balance + GetDecimalAmount(balance);
                case "server rewards":
                    return ((int) ServerRewards?.Call("CheckPoints", player.userID)) + ".00";
                case "scraps":
                    return player.inventory.GetAmount(-932201673) + ".00";
            }

            return "Invalid Currency";
        }

        private string GetGrid(float posX, float posY, float posZ)
        {
            char letter = 'A';
            var x = Mathf.Floor((posX + (ConVar.Server.worldsize / 2)) / 146.3f) % 26;
            var z = (Mathf.Floor(ConVar.Server.worldsize / 146.3f)) - Mathf.Floor((posZ + (ConVar.Server.worldsize / 2)) / 146.3f);
            letter = (char)(((int)letter) + x);
            return $"{letter}{z}";
        }

        private bool inTimeRange() // Credits - Rob Vary
        {
            var hour = TOD_Sky.Instance.Cycle.Hour;

            var start = config.basicSettings.hoursFrom;
            var end = config.basicSettings.hoursTill;

            if (start <= end)
            {
                return start <= hour && hour <= end;
            }
            else
            {
                return start <= hour || hour <= end;
            }
        }

        private static BaseEntity FindObject(Ray ray, float distance)
        {
            RaycastHit hit;
            return !Physics.Raycast(ray, out hit, distance) ? null : hit.GetEntity();
        }

        private string GetDecimalAmount(double amount)
            => (amount % 1) == 0 ? ".00" : string.Empty;

        private void Message(BasePlayer player, string message)
            => Player.Message(player, message, 76561199277220336);
        #endregion

    }
}