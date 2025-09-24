using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Shop","https://discord.gg/9vyTXsJyKR","1.0")]
    public class Shop : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary,Economics,BMenu;
        

        #region var

        private string _textColor = "0.929 0.882 0.847 1";
        string Layer = "Shop_UI";

        #endregion

        #region config
        private static Configuration config = new Configuration();

        private class Configuration
        {
            public List<ShopItem> Items = new List<ShopItem>();

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    
                    Items = new List<ShopItem>
                    {
                        new ShopItem
                        {
                            Shortname = "sulfur",
                            Amount = 100,
                            Price = 1,
                            Category = "Resources"
                        },
                        new ShopItem
                        {
                            Shortname = "rifle.ak",
                            Amount = 1,
                            Price = 10,
                            Category = "Weapons"
                        }
                    }
                };
            }

        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) PrintWarning("NULL");
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Config Error 'oxide/config/{Name}', Create config!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);


        

        #endregion

        #region data

        Dictionary<ulong, int> BonusPoints = new Dictionary<ulong, int>();
        Dictionary<ulong, string> ActiveButton = new Dictionary<ulong, string>();
        Dictionary<string, int> PermPoints = new Dictionary<string, int>();

        List<string> Category = new List<string>()
        {
            "All",
            "Weapons",			
            "Ammunition",
            "Attire",
            "Tools",	
            "Resources",         
            "Construction",
            "Traps",  			
            "Electrical",
            "Meds",          
            "Food"			
        };

        Dictionary<ulong, List<string>> pFavorite = new Dictionary<ulong, List<string>>();
        Dictionary<ulong, double> playerTime = new Dictionary<ulong, double>();

        #endregion

        #region Item

        class ShopItem
        {
            [JsonProperty("Name")] public string Shortname;
            [JsonProperty("Amount of item")] public int Amount;
            [JsonProperty("Price")] public int Price;
            [JsonProperty("Category")] public string Category;
        }
        
        List<ShopItem>Default = new List<ShopItem>
        {
            new ShopItem
            {
                Shortname = "sulfur",
                Amount = 100,
                Price = 1,
                Category = "Resources"
            },
            new ShopItem
            {
                Shortname = "rifle.ak",
                Amount = 1,
                Price = 10,
                Category = "Weapons"
            }
            
        };
        
        void LoadShopItems()
        {
            //LoadDefaultConfig();
            
        }

        void SaveShopItems()
        {
            SaveConfig();
        }

        void InitPlayerFavorite(BasePlayer player)
        {
            if (!pFavorite.ContainsKey(player.userID))
            {
                pFavorite[player.userID] = new List<string>();
            }

            SaveFavorite();
        }

        void LoadFavorite()
        {
            pFavorite = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, List<string>>>("CShop/Favorite");
        }

        void SaveFavorite()
        {
            Interface.Oxide.DataFileSystem.WriteObject("CShop/Favorite", pFavorite);
        }
        #endregion

        #region oxide hooks

        void OnPlayerConnected(BasePlayer player)
        {
            InitPlayerFavorite(player);
        }
        
        void OnServerInitialized()
        {
            InitializeLang();
            //PrintWarning("ConfigLoaded");

            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("CShop/Favorite"))
            {
                SaveFavorite();
            }
            else
            {
                LoadFavorite();
            }
            foreach (var check in config.Items)
                ImageLibrary.Call("AddImage", $"https://rustlabs.com/img/items180/{check.Shortname}.png", check.Shortname);

            foreach (var check in BasePlayer.activePlayerList)
                OnPlayerConnected(check);

        }
        void OnItemSubmit(Item item, Mailbox mailbox, BasePlayer player)
        {
            if (boxes.Contains(mailbox))
            {
                SendReply(player,$"Write amount and price");
                bufferItem.Shortname = item.info.shortname;
                bufferPlayer.Add(player.userID);
                player.EndLooting();
            }
            
        }
        object OnPlayerChat(BasePlayer player, string message)
        {
            if (bufferPlayer.Contains(player.userID))
            {
                
                int amount = message.Split(' ')[0].ToInt();
                int price = message.Split(' ')[1].ToInt();
                bufferItem.Amount = amount;
                bufferItem.Price = price;
                config.Items.Add(bufferItem);
                //PrintWarning($"Creatod item to shop:Category: {bufferItem.Category} Name: {bufferItem.Shortname} Amount: {bufferItem.Amount} Price: {bufferItem.Price}");
                SaveShopItems();
                bufferItem = new ShopItem();
                return true;
            }
            else
            {
                return null;
            }
            return null;
        }

        void Unload()
        {
            SaveShopItems();
            //SavePerms();
            SaveConfig();
        }

        #endregion

        #region hooks
        
        double CurTime() => new TimeSpan(DateTime.UtcNow.Ticks).TotalSeconds;

        public static string FormatShortTime(TimeSpan time)
        {
            string result = string.Empty;
            result = $"{time.Hours.ToString("00")}:";
            result += $"{time.Minutes.ToString("00")}:";
            result += $"{time.Seconds.ToString("00")}";
            return result;
        }

        #endregion
        
        #region Commands

        [ChatCommand("os")]
        void CopyToCfg(BasePlayer player)
        {
            ShopUI(player);
        }
        [ChatCommand("b")]
        void CheckBalacne(BasePlayer player)
        {
            SendReply(player,Economics.Call("Balance",player.userID).ToString());
        }
        
        [ChatCommand("sdata")]
        void ForceWipeData(BasePlayer player)
        {
            if (!player.IsAdmin) return;
            int c =BonusPoints.Count;
            BonusPoints.Clear();
            SendReply(player,$"Wiped {c} players");
        }

        [ConsoleCommand("shop")]
        void ConsoleShop(ConsoleSystem.Arg args)
        {
            //PrintWarning(args.FullString);
            var player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "category")
                {
                    ActiveButton[player.userID] = args.Args[1];
                    ItemUI(player, args.Args[1]);
                }
                if (args.Args[0] == "all")
                {
                    var db = pFavorite[player.userID];
                    if (db.Contains(args.Args[1]))
                        db.Remove(args.Args[1]);
                    else
                    {
                        db.Add(args.Args[1]);
                        SaveFavorite();
                    }

                    

                    ItemUI(player, ActiveButton[player.userID]);
                }
                if (args.Args[0] == "skip")
                {
                    //PrintWarning(ActiveButton[player.userID]);
                    ItemUI(player, ActiveButton[player.userID], int.Parse(args.Args[1]));
                }
                if (args.Args[0] == "buy")
                {
                    /*if (player.inventory.containerMain.itemList.Count >= 24)
                    {
                        SendReply(player, "Not enough space");
                        return;
                    }*/
                    var check = config.Items.FirstOrDefault(z => z.Shortname == args.Args[1]);
                    double balance = (double)Economics.Call("Balance", player.userID);
                    if (balance >= check.Price)
                    {
                        if (player.inventory.containerMain.itemList.Count < 24)
                        {
                            var item = ItemManager.CreateByName(check.Shortname, check.Amount);
                            item.MoveToContainer(player.inventory.containerMain);
                        }
                        else
                        {
                            if (player.inventory.containerBelt.itemList.Count < 6)
                            {
                                var item = ItemManager.CreateByName(check.Shortname, check.Amount);
                                item.MoveToContainer(player.inventory.containerBelt);
                            }
                            else
                            {
                                var item = ItemManager.CreateByName(check.Shortname, check.Amount);
                                item.Drop(player.transform.position, Vector3.up);
                                SendReply(player,$"Предмет выброшен");
                            }
                        }
                        
                        Effect x = new Effect("assets/bundled/prefabs/fx/notice/stack.world.fx.prefab", player, 0, new Vector3(), new Vector3());
                        EffectNetwork.Send(x, player.Connection);
                        Economics.Call("SetBalance", player.UserIDString, balance - check.Price);
                        //UpdateBalance(player);
                        BMenu.Call("UpdateBalance", player);
                    }
                }
            }
        }

        #endregion
        

        #region AdminAdd
        ShopItem bufferItem = new ShopItem();
        List<ulong>bufferPlayer = new List<ulong>();

        private string boxPrefab = "assets/prefabs/deployable/dropbox/dropbox.deployed.prefab";
        private List<Mailbox> boxes = new List<Mailbox>();

        void CreateDropBox(BasePlayer player, string category)
        {
            Mailbox dropbox = GameManager.server.CreateEntity(boxPrefab,Vector3.zero) as Mailbox;
            dropbox.Spawn();
            player.net.subscriber.Subscribe(dropbox.net.@group);
            StartLooting(player,dropbox);
            bufferItem.Category = category;
            boxes.Add(dropbox);
        }
        public void StartLooting(BasePlayer player,Mailbox mailbox)
        {
            player.inventory.loot.StartLootingEntity(mailbox, false);
            player.inventory.loot.AddContainer(mailbox.inventory);
            player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", mailbox.panelName);
            mailbox.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            //player.inventory.loot.AddContainer(mailbox.customerInventory);
            player.inventory.loot.SendImmediate();
        }

        [ConsoleCommand("sadd")]
        void AdminAddItem(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (!player.IsAdmin) return;
            CreateDropBox(player,arg.Args[0]);
            
        }

        [ConsoleCommand("arpgive")]
        void AdminGiveRP(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (!player.IsAdmin || player == null)
            {
                return;
            }

            if (!arg.HasArgs())
            {
                arg.ReplyWith("use [name/steamid] [amount]");
                return;
            }

            if (arg.Args.Length<2)
            {
                
                PrintToConsole("use [name/steamid] [amount]");
                return;
            }

            if (arg.Args[0] == "me")
            {
                int pamount = Convert.ToInt32(arg.Args[1]);
                BonusPoints[player.userID] += pamount;
                PrintToConsole($"You successfully give {player.displayName} {pamount} Платины");
                return;
            }
            BasePlayer target = BasePlayer.Find(arg.Args[0]);
            /*ulong id = Convert.ToUInt64(arg.Args[0]);
            if (target == null)
            {
                target = BasePlayer.FindByID(id);
            }*/

            if (target == null)
            {
                PrintToConsole("Player doesn't exists! Try again");
            }

            int amount = Convert.ToInt32(arg.Args[1]);
            BonusPoints[target.userID] += amount;
            PrintToConsole($"You successfully give {target.displayName} {amount} Платины");
        }
        #endregion

        #region UI

        private string timeLayer = "Time_Layer";
        void TimeUI(BasePlayer player)
        {
            
            CuiHelper.DestroyUi(player, timeLayer);
            var container = new CuiElementContainer();
            
            CuiHelper.AddUi(player, container);
        }

        void ShopUI(BasePlayer player)
        {
            if (!BonusPoints.ContainsKey(player.userID))
            {
                BonusPoints.Add(player.userID,0);
            }
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();
            if (!ActiveButton.ContainsKey(player.userID))
            {
                ActiveButton[player.userID] = "Ammunition";
            }
            

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" },
            }, "SubContent_UI", Layer);
            

            /*container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.03 0.02", AnchorMax = $"0.25 0.07", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, Layer, "Balance");*/
            

            CuiHelper.AddUi(player, container);
            ItemUI(player, ActiveButton[player.userID]);
        }

        void ItemUI(BasePlayer player, string category, int page = 0)
        {
            CuiHelper.DestroyUi(player, "Item");
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" },
            }, Layer, "Item");

           
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "717 20",OffsetMax = "890 60"},
                    Button = { Color = HexToCuiColor("#34405e"), Command = config.Items.Where(p => p.Category == category).Count() > (page +1)*28?$"shop skip {page + 1}":"",Material = "assets/icons/greyout.mat" },
                    Text = { Text = "<b>ВПЕРЁД →</b>", Align = TextAnchor.MiddleCenter, FontSize = 22, Font = "robotocondensed-bold.ttf",Color = config.Items.Where(p => p.Category == category).Count() > (page +1)*28?HexToCuiColor("F4F4F4"):HexToCuiColor("#727273") }
                }, "Item");
            

            
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "28 20",OffsetMax = "205 60"},
                    Button = { Color = HexToCuiColor("#34405e"), Command = page>=1?$"shop skip {page - 1}":"",Material = "assets/icons/greyout.mat" },
                    Text = { Text = "<b>← НАЗАД</b>", Align = TextAnchor.MiddleCenter, FontSize = 22, Font = "robotocondensed-bold.ttf",Color = page>=1?HexToCuiColor("F4F4F4"):HexToCuiColor("#727273") }
                }, "Item");
            
            
            if (player.IsAdmin)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.4 0.02", AnchorMax = $"0.5 0.07", OffsetMax = "0 0" },
                    Button = { Color = "0.86 0.55 0.35 1", Command = $"sadd {category}",Close = "Menu"},
                    Text = { Text = "Add", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                }, "Item");
                
            }

            float width = 0f, height = 0.055f, startxBox = 0.028f, startyBox = 1f - height, xmin = startxBox, ymin = startyBox;
            //double width = 0f, height = 90, startxBox = 5, startyBox = 620 - height, xmin = startxBox, ymin = startyBox;
            foreach (var check in Category)
            {
                if (check == "All")
                    width = 0.05f;
                else
                    width = 0.1093f;
                /*if (check == "All")
                    width = 60;
                else
                    width = 60;*/

                var text = check == "All" ? "" : check;
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{xmin} {ymin}", AnchorMax = $"{xmin + width} {ymin + height * 1}", OffsetMin = "1 2", OffsetMax = "-1 -2" },
                    Button = { Color = "0.27 0.25 0.23 0.5", Command = $"shop category {check}" },
                    Text = { Text = lang.GetMessage(text,this),Color = _textColor,Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-bold.ttf" }
                }, "Item", "Color");
                //PrintWarning($"{text} : {lang.GetMessage(text,this)}");
                
                /*container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0 0", OffsetMin = $"{xmin} {ymin}", OffsetMax = $"{xmin + width} {ymin + height * 1}" },
                    Button = { Color = "0 0 0 0.5", Command = $"shop category {check}" },
                    Text = { Text = text, Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                }, "Item", "Color");*/

                var color = ActiveButton[player.userID] == check ? "0.17 0.41 0.57 0.5" : "0 0 0 0";
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Image = { Color = color },
                }, "Color");

                if (check == "All")
                {
                    container.Add(new CuiElement
                    {
                        Parent = "Color",
                        Components =
                        {
                            new CuiImageComponent { Sprite = "assets/icons/open.png", FadeIn = 0.5f, Color = "0.86 0.55 0.35 1" },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "7 7", OffsetMax = "-7 -7" }
                        }
                    });
                }

                xmin += width;
                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height;
                }
            }

            //float width1 = 0.1572f, height1 = 0.28f, startxBox1 = 0.028f, startyBox1 = 0.95f - height1, xmin1 = startxBox1, ymin1 = startyBox1;
            double width1 = 120, height1 = 120, startxBox1 = 28, startyBox1 = 570 - height1, xmin1 = startxBox1, ymin1 = startyBox1;
            if (ActiveButton[player.userID] == "All")
            {
                //PrintWarning(pFavorite[player.userID].Count.ToString() + " "+page);
                List<ShopItem>fav = new List<ShopItem>();
                foreach (var item in config.Items)
                {
                    if (pFavorite[player.userID].Contains(item.Shortname))
                    {
                        fav.Add(item);
                    }
                }
                foreach (var items in fav.Skip(page * 28).Take(28))
                {
                    foreach (var check in config.Items.Where(z => z.Shortname == items.Shortname))
                    {
                        if (pFavorite[player.userID].Contains(check.Shortname))
                        {
                            Puts(check.Shortname);
                            /*container.Add(new CuiPanel
                            {
                                RectTransform = { AnchorMin = $"{xmin1} {ymin1}", AnchorMax = $"{xmin1 + width1} {ymin1 + height1 * 1}", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                                Image = { Color = "1 1 1 0.15" }
                            }, "Item", "Items");*/
                            container.Add(new CuiPanel
                            {
                                RectTransform = { AnchorMin = "0 0" , AnchorMax = "0 0", OffsetMin = $"{xmin1} {ymin1}", OffsetMax = $"{xmin1 + width1} {ymin1 + height1 * 1}" },
                                Image = { Color = "1 1 1 0.15" }
                            }, "Item", "Items");

                            container.Add(new CuiElement
                            {
                                Parent = "Items",
                                Components =
                                {
                                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.Shortname), FadeIn = 0.5f },
                                    new CuiRectTransformComponent { AnchorMin = "0 0.3", AnchorMax = "1 1", OffsetMin = "30 18", OffsetMax = "-30 -5" }
                                }
                            });

                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0.8 0.79", AnchorMax = "1 0.99", OffsetMax = "0 0" },
                                Button = { Color = _textColor, Command = $"shop all {check.Shortname}" },
                                Text = { Text = "" }
                            }, "Items", "All");

                            var color = pFavorite[player.userID].Contains(check.Shortname) ? "0.86 0.55 0.35 1" : "1 1 1 1";
                           container.Add(new CuiElement
                            {
                                Parent = "All",
                                Components =
                                {
                                    new CuiImageComponent { Sprite = "assets/icons/open.png", FadeIn = 0.5f, Color = color },
                                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "3 3", OffsetMax = "-3 -3" }
                                }
                            });

                            container.Add(new CuiPanel
                            {
                                RectTransform = { AnchorMin = "0 0.2", AnchorMax = "1 0.43", OffsetMax = "0 0" },
                                Image = { Color = "1 1 1 0.6" },
                            }, "Items", "Name");

                            var item = ItemManager.CreateByItemID(ItemManager.FindItemDefinition(check.Shortname).itemid, check.Amount, 0);
                            container.Add(new CuiLabel
                            {
                                RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"0.8 1", OffsetMax = "0 0" },
                                Text = { Text = $"{item.info.displayName.english}",Color = "0 0 0 1", Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                            }, "Name");

                            container.Add(new CuiLabel
                            {
                                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.97 1", OffsetMax = "0 0" },
                                Text = { Text = $"x{check.Amount}",Color = _textColor, Align = TextAnchor.MiddleRight, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                            }, "Name");

                            container.Add(new CuiPanel
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.2", OffsetMax = "0 0" },
                                Image = { Color = "0.38 0.37 0.38 1" },
                            }, "Items", "Price");

                            container.Add(new CuiLabel
                            {
                                RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                                Text = { Text = $"{check.Price} Платины", Align = TextAnchor.MiddleLeft, FontSize = 14, Font = "robotocondensed-regular.ttf",Color = _textColor, }
                            }, "Price");

                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = $"0.74 0", AnchorMax = $"0.98 1", OffsetMax = "0 0" },
                                Button = { Color = _textColor, Command = $"shop buy {check.Shortname}",Sprite = "assets/icons/cart.png"},
                               Text = { Text = $"", Align = TextAnchor.MiddleCenter, FontSize = 17, Font = "robotocondensed-bold.ttf" }
                            }, "Price");

                            xmin1 += width1+10;
                            if (xmin1 + width1 >= 700)
                            {
                                xmin1 = startxBox1;
                                ymin1 -= height1+10;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var check in config.Items.Where(z => z.Category == ActiveButton[player.userID]).Skip(page * 28).Take(28))
                {
                    /*container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = $"{xmin1} {ymin1}", AnchorMax = $"{xmin1 + width1} {ymin1 + height1 * 1}", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                        Image = { Color = "1 1 1 0.15" }
                    }, "Item", "Items");*/
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0" , AnchorMax = "0 0", OffsetMin = $"{xmin1} {ymin1}", OffsetMax = $"{xmin1 + width1} {ymin1 + height1 * 1}" },
                        Image = { Color = "0.20 0.25 0.37 0.7" }
                    }, "Item", "Items");

                    container.Add(new CuiElement
                    {
                        Parent = "Items",
                        Components =
                        {
                            new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.Shortname), FadeIn = 0.5f },
                            new CuiRectTransformComponent { AnchorMin = "0 0.3", AnchorMax = "1 1", OffsetMin = "30 18", OffsetMax = "-30 -5" }
                        }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.8 0.79", AnchorMax = "1 0.99", OffsetMax = "0 0" },
                        Button = { Color = "1 1 1 0", Command = $"shop all {check.Shortname}" },
                        Text = { Text = "" }
                    }, "Items", "All");

                    var color = pFavorite[player.userID].Contains(check.Shortname) ? "0.86 0.55 0.35 1" : "1 1 1 1";
                    container.Add(new CuiElement
                    {
                        Parent = "All",
                        Components =
                        {
                            new CuiImageComponent { Sprite = "assets/icons/open.png", FadeIn = 0.5f, Color = color },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "3 3", OffsetMax = "-3 -3" }
                        }
                    });

                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0.2", AnchorMax = "1 0.43", OffsetMax = "0 0" },
                        Image = { Color = "1 1 1 0" },
                    }, "Items", "Name");

                    var item = ItemManager.CreateByItemID(ItemManager.FindItemDefinition(check.Shortname).itemid, 1, 0);
                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"0.8 1", OffsetMax = "0 0" },
                        Text = { Text = $"{item.info.displayName.english}",Color = "0 0 0 0", Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                    }, "Name");

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.97 1", OffsetMax = "0 0" },
                        Text = { Text = $"x{check.Amount}",Color = _textColor, Align = TextAnchor.MiddleRight, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                    }, "Name");

                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.2", OffsetMax = "0 0" },
                        Image = { Color = "0.34 0.33 0.31 0.9" },
                    }, "Items", "Price");

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                        Text = { Text = $"{check.Price} Платины", Align = TextAnchor.MiddleLeft, FontSize = 14, Font = "robotocondensed-regular.ttf",Color = _textColor, }
                    }, "Price");

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = $"0.76 0", AnchorMax = $"0.98 1", OffsetMax = "0 0" },
                        Button = { Color = _textColor, Command = $"shop buy {check.Shortname}",Sprite = "assets/icons/cart.png" },
                        Text = { Text = $"", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                    }, "Price");

                    xmin1 += width1+5;
                    
                    if (xmin1 + width1 >= 900)
                    {
                        //PrintError($"{xmin1 + width1}");
                        xmin1 = startxBox1;
                        ymin1 -= height1+5;
                    }
                }
            }

            CuiHelper.AddUi(player, container);
        }

        /*void UpdateBalance(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Balance");
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.03 0.02", AnchorMax = $"0.25 0.07", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, Layer, "Balance");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = $"Balance: {Economics.Call("Balance",player.userID)} Платины", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-regular.ttf" }
            }, "Balance");

            CuiHelper.AddUi(player, container);
        }
        */

        #endregion

        #region lang

        void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["All"] = "Всё",
                ["Weapons"] = "Оружие",				
                ["Ammunition"] = "Аммуниция",
                ["Attire"] = "Экипировка",
                ["Tools"] = "Снаряжение",				
                ["Resources"] = "Ресурсы",
                ["Construction"] = "Постройки",
                ["Electrical"] = "Электрика",
                ["Traps"] = "Защитные",						
                ["Meds"] = "Медицина",				
                ["Food"] = "Развлечение",
                ["Trap"] = "Защитные",				
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["All"] = "Всё",
                ["Weapons"] = "Оружие",				
                ["Ammunition"] = "Аммуниция",
                ["Attire"] = "Экипировка",
                ["Tools"] = "Снаряжение",				
                ["Resources"] = "Ресурсы",
                ["Construction"] = "Постройки",
                ["Electrical"] = "Электрика",
                ["Traps"] = "Защитные",						
                ["Meds"] = "Медицина",	
                ["Food"] = "Развлечение",			
            }, this, "ru");
        }

        #endregion
        
        private static string HexToCuiColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";
            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }
            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }
    }
}