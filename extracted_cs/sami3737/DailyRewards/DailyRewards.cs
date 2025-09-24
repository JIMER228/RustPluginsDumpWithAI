using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Random = System.Random;

namespace Oxide.Plugins
{
    [Info("DailyRewards", "sami37", "1.0.2")]
    public class DailyRewards : RustPlugin
    {
        private List<object> Items = new List<object>();
        private Dictionary<ulong, List<object>> PlayerItemsList = new Dictionary<ulong, List<object>>();
        private Dictionary<ulong, List<ItemChoose>> playerItems = new Dictionary<ulong, List<ItemChoose>>();
        private bool Changed;
        private string MainGUIAnchorMax, MainGUIAnchorMin, MainCUIContainer = "DailyRewardCUIMain", BackgroundImage, BackgroundColor;
        private Dictionary<ulong, DateTime> PlayerData = new Dictionary<ulong, DateTime>();
        Random random = new Random();
        private Dictionary<ulong, int> itemError = new Dictionary<ulong, int>();
        static DateTime CurrentDateTime = DateTime.Today;
        private Dictionary<string, Timer> timers = new Dictionary<string, Timer>();
        private string userperm = "dailyrewards.allow", adminperm = "dailyrewards.admin";

        [PluginReference]
        Plugin ImageLibrary;

        #region ConfigFunction
        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }

            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }

            return value;
        }

        #endregion

        class ItemChoose
        {
            public List<ItemDataDetails> itemList;
        }

        class ItemDataDetails
        {
            public ItemDefinition itemDef;
            public int quantity;
        }

        class ItemData
        {
            public string ItemName;
            public int MinQuantity;
            public int MaxQuantity;
        }

        void LoadData()
        {
            PlayerData = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, DateTime>>(Name);
        }

        void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject(Name, PlayerData);
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }

        void LoadVariables()
        {
            var itemData = new List<ItemData>();
            foreach (var item in ItemManager.itemList)
            {
                if (itemData.Find(x => x.ItemName == item.shortname) != null) continue;
                itemData.Add(
                    new ItemData
                    {
                        ItemName = item.shortname,
                        MinQuantity = 1,
                        MaxQuantity = 100
                    });
            }

            MainGUIAnchorMax = Convert.ToString(GetConfig("CUI", "AnchorMax", "1 1"));
            MainGUIAnchorMin = Convert.ToString(GetConfig("CUI", "AnchorMin", "0 0"));
            BackgroundImage = Convert.ToString(GetConfig("CUI", "Background Image", "https://images4.alphacoders.com/819/819162.jpg"));
            BackgroundColor = Convert.ToString(GetConfig("CUI", "Background Color", "0 0 0 0"));
            Items = GetConfig("Item", "Data", itemData) as List<object>;

            if (!Changed) return;
            SaveConfig();
            Changed = false;
        }

        void LoadImage()
        {
            if (ImageLibrary)
            {
                AddImage(BackgroundImage, "backgrounddailyreward");
                foreach (var entry in ItemManager.itemList)
                    AddImage(GetImageUrl(entry.shortname, 0, true), entry.shortname);
            }
        }

        void OnServerInitialized()
        {
            permission.RegisterPermission(userperm, this);
            permission.RegisterPermission(adminperm, this);
            if (timers.ContainsKey("imageloading"))
            {
                timers["imageloading"].Destroy();
                timers.Remove("imageloading");
            }

            LoadVariables();
            LoadData();
            NextTick(() =>
            {
                DateTime start = CurrentDateTime;
                DateTime end = start.AddDays(1);
                DateTime today = DateTime.Today;

                int value = DateTime.Compare(end.Date, today.Date);
                if (value <= 0)
                {
                    playerItems.Clear();
                    foreach (var player in BasePlayer.activePlayerList)
                        SendReply(player, lang.GetMessage("NewDay", this, player.UserIDString));
                }
            });
            LoadImage();
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUi(player);
            }

            SaveData();
        }

        void OnSave()
        {
            SaveData();
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(2, () => OnPlayerInit(player));
                return;
            }

            if (PlayerData == null || !PlayerData.ContainsKey(player.userID))
            {
                OpenUi(player);
            }
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Error", "To much error while generating ItemData, please check your config."},
                {"WrongItem","Item {0} doesn't exist."},
                {"OnGround", "You don't have enough space in your inventory, just dropped to your feets." },
                {"Inventory", "Just added the reward to your inventory."},
                {"CantPick", "You already choose a reward for today"},
                {"DataReset", "Data has been reset"},
                {"NoPerm", "You don't have the permission for this command"},
                {"NewDay", "It's a new day, grab your reward."}
            }, this);
        }

        void DestroyUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, MainCUIContainer);
        }

        ItemDataDetails GenerateItemData(BasePlayer player)
        {
            ItemDataDetails data = new ItemDataDetails();

            object value = null, value1 = null, value2 = null;
            if (Items != null)
            {
                if (!PlayerItemsList.ContainsKey(player.userID))
                    PlayerItemsList.Add(player.userID, Items);
                if (PlayerItemsList[player.userID].Count == 0)
                    PlayerItemsList[player.userID] = Items;
                var pick = random.Next(PlayerItemsList[player.userID].Count);
                if (PlayerItemsList[player.userID].Count >= pick - 1)
                {
                    var item = PlayerItemsList[player.userID][pick - 1] as Dictionary<string, object>;
                    item?.TryGetValue("ItemName", out value);
                    if (value == null)
                    {
                        if (!itemError.ContainsKey(player.userID))
                            itemError.Add(player.userID, 1);
                        else
                            itemError[player.userID] += 1;

                        if (itemError[player.userID] > 3)
                        {
                            PrintError(lang.GetMessage("Error", this));
                            itemError[player.userID] = 0;
                            return null;
                        }

                        return GenerateItemData(player);
                    }

                    ItemDefinition itemDef = ItemManager.FindItemDefinition(value.ToString());
                    if (itemDef == null)
                    {
                        PrintWarning(string.Format(lang.GetMessage("WrongItem", this), value));
                        if (itemError == null) itemError = new Dictionary<ulong, int>();
                        if (!itemError.ContainsKey(player.userID))
                            itemError.Add(player.userID, 1);
                        else
                            itemError[player.userID] += 1;

                        if (itemError[player.userID] > 3)
                        {
                            PrintError(lang.GetMessage("Error", this));
                            return null;
                        }

                        return GenerateItemData(player);
                    }

                    item.TryGetValue("MinQuantity", out value2);
                    item.TryGetValue("MaxQuantity", out value1);

                    data.itemDef = itemDef;
                }

                if (value2 != null && value1 != null) data.quantity = random.Next((int)value2, (int)value1);
                if (itemError != null && itemError.ContainsKey(player.userID))
                    itemError[player.userID] = 0;
                PlayerItemsList[player.userID].RemoveAll(x =>
                    (x as Dictionary<string, object>)?["ItemName"].ToString() == value?.ToString());
            }

            return data;
        }

        public string GetImageUrl(string shortname, ulong skin = 0, bool url = false)
        {
            return (string)ImageLibrary?.Call("GetImageURL", shortname, skin, url);
        }

        public string GetImage(string shortname, ulong skin = 0, bool returnUrl = false) => (string)ImageLibrary.Call("GetImage", shortname.ToLower(), skin, returnUrl);

        public bool AddImage(string url, string shortname, ulong skin = 0) => ImageLibrary != null && (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);

        public bool HasImage(string shortname, ulong skin = 0) => ImageLibrary != null && (bool)ImageLibrary?.Call("HasImage", shortname, skin);

        private string TryForImage(string shortname, ulong skin = 0, bool url = false)
        {
            if (shortname.Contains("http")) return shortname;
            if (skin != 0) skin = (ulong)ResourceId;
            return GetImage(shortname, skin, url);
        }

        private CuiElement CreateImage(string panelName, string png, string anchormin, string anchormax, bool url = false)
        {
            if (ImageLibrary && HasImage(png))
                png = TryForImage(png, 0, url);
            else if (png.StartsWith("http") || png.StartsWith("www"))
            { }
            else png = "http://i.imgur.com/xxQnE1R.png";

            var element = new CuiElement();
            CuiRawImageComponent image;
            if (png.StartsWith("http") || png.StartsWith("www"))
            {
                image = new CuiRawImageComponent
                {
                    Url = png,
                    Sprite = "assets/content/textures/generic/fulltransparent.tga"
                };
            }
            else
            {
                image = new CuiRawImageComponent
                {
                    Png = png,
                    Sprite = "assets/content/textures/generic/fulltransparent.tga"
                };
            }
            var rectTransform = new CuiRectTransformComponent
            {
                AnchorMin = anchormin,
                AnchorMax = anchormax
            };
            element.Components.Add(image);
            element.Components.Add(rectTransform);
            element.Parent = panelName;

            return element;
        }

        void OpenUi(BasePlayer player, string variable = "0")
        {
            if (PlayerData == null) PlayerData = new Dictionary<ulong, DateTime>();
            if (PlayerData.ContainsKey(player.userID))
            {
                DateTime start = PlayerData[player.userID];
                DateTime end = start.AddDays(1);
                DateTime today = DateTime.Today;

                int value = DateTime.Compare(end.Date, today.Date);
                if (value >= 0)
                {
                    SendReply(player, lang.GetMessage("CantPick", this, player.UserIDString));
                    return;
                }

                PlayerData.Remove(player.userID);
                playerItems.Remove(player.userID);
            }

            if (playerItems == null) playerItems = new Dictionary<ulong, List<ItemChoose>>();
            List<ItemChoose> itemList = new List<ItemChoose>();
            if (!playerItems.ContainsKey(player.userID))
            {
                playerItems.Add(player.userID, new List<ItemChoose>());

                itemList.Add(new ItemChoose()
                {
                    itemList = new List<ItemDataDetails>{
                        GenerateItemData(player),
                        GenerateItemData(player),
                        GenerateItemData(player)
                    }
                });
                itemList.Add(new ItemChoose()
                {
                    itemList = new List<ItemDataDetails>{
                        GenerateItemData(player),
                        GenerateItemData(player),
                        GenerateItemData(player)
                    }
                });
                itemList.Add(new ItemChoose()
                {
                    itemList = new List<ItemDataDetails>{
                        GenerateItemData(player),
                        GenerateItemData(player),
                        GenerateItemData(player)
                    }
                });
                playerItems[player.userID]= itemList;
            }

            var elementsContainer = new CuiElementContainer();

            elementsContainer.Add(new CuiPanel
            {
                Image =
                {
                    Color = BackgroundColor,
                    Sprite = "assets/content/textures/generic/fulltransparent.tga"
                },
                RectTransform =
                {
                    AnchorMin = MainGUIAnchorMin,
                    AnchorMax = MainGUIAnchorMax
                },
                CursorEnabled = true
            }, new CuiElement().Parent, MainCUIContainer);
            if (!string.IsNullOrEmpty(BackgroundImage))
            {
                var background = CreateImage(MainCUIContainer,
                    (BackgroundImage == "") ? "" :
                    HasImage("backgrounddailyreward") ? "backgrounddailyreward" : BackgroundImage, MainGUIAnchorMin,
                    MainGUIAnchorMax, true);
                elementsContainer.Add(background);
            }

            CuiElement backgroundImage;
            var count = 0;
            elementsContainer.Add(new CuiPanel()
            {
                Image =
                {
                    Color = "0.15 0.15 0.15 1",
                    Sprite = "assets/content/textures/generic/fulltransparent.tga",
                    Png = "https://www.beautycolorcode.com/57595d.png"
                },
                RectTransform =
                {
                    AnchorMin = "0.28 0.26",
                    AnchorMax = "0.73 0.85"
                }
            }, MainCUIContainer);
            elementsContainer.Add(new CuiButton
            {
                Text =
                {
                    Text = "",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter
                },
                Button =
                {
                    Close = MainCUIContainer,
                    Command = "choosepick 1",
                    Color = variable == "1" ? "0 0.5 0 0.7" : "0.5 0.5 0.5 0.6"
                },
                RectTransform =
                {
                    AnchorMin = "0.293 0.693",
                    AnchorMax = "0.717 0.832"
                }
            }, MainCUIContainer);

            elementsContainer.Add(new CuiButton
            {
                Text =
                {
                    Text = "",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter
                },
                Button =
                {
                    Close = MainCUIContainer,
                    Command = "choosepick 2",
                    Color = variable == "2" ? "0 0.5 0 0.7" : "0.5 0.5 0.5 0.6"
                },
                RectTransform =
                {
                    AnchorMin = "0.293 0.532",
                    AnchorMax = "0.717 0.671"
                }
            }, MainCUIContainer);

            elementsContainer.Add(new CuiButton
            {
                Text =
                {
                    Text = "",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter
                },
                Button =
                {
                    Close = MainCUIContainer,
                    Command = "choosepick 3",
                    Color = variable == "3" ? "0 0.5 0 0.7" : "0.5 0.5 0.5 0.6"
                },
                RectTransform =
                {
                    AnchorMin = "0.293 0.375",
                    AnchorMax = "0.717 0.514"
                }
            }, MainCUIContainer);
            if (playerItems != null && playerItems.ContainsKey(player.userID))
                foreach (var Items in playerItems[player.userID])
                {
                    var dataDetails = Items;
                    if (dataDetails?.itemList != null && dataDetails.itemList.Count >= 1)
                    {
                        var image = dataDetails.itemList.First() != null
                            ? $"{dataDetails.itemList.First()?.itemDef?.shortname}"
                            : "Item Not Found";
                        backgroundImage = CreateImage(MainCUIContainer, image, count == 0 ? "0.293 0.693" : count == 1 ? "0.293 0.532" : "0.293 0.375", count == 0 ? "0.371 0.832" : count == 1 ? "0.371 0.671" : "0.371 0.514", HasImage(image));
                        elementsContainer.Add(backgroundImage);
                    }

                    var value = count == 0 ? 1 : count == 1 ? 2 : 3;
                    elementsContainer.Add(new CuiButton
                    {
                        Text =
                        {
                            Text = dataDetails?.itemList?.First() != null
                                ? $"{dataDetails.itemList?.First()?.itemDef?.displayName.english} x {dataDetails.itemList?.First()?.quantity}"
                                : "Item Not Found",
                            FontSize = 20,
                            Align = TextAnchor.LowerCenter
                        },
                        Button =
                        {
                            Close = MainCUIContainer,
                            Command = $"choosepick {value}",
                            Color = "0.5 0.5 0.5 0"
                        },
                        RectTransform =
                        {
                            AnchorMin = count == 0 ? "0.293 0.693": count == 1 ? "0.293 0.532" : "0.293 0.375",
                            AnchorMax = count == 0 ? "0.371 0.832" : count == 1 ? "0.371 0.671" : "0.371 0.514"
                        }
                    }, MainCUIContainer);

                    if (dataDetails?.itemList != null && dataDetails.itemList.Count >= 2)
                    {
                        var image = playerItems[player.userID] != null && dataDetails.itemList[1] != null
                            ? dataDetails.itemList[1]?.itemDef?.shortname
                            : "Item Not Found";
                        backgroundImage = CreateImage(MainCUIContainer, image, count == 0 ? "0.465 0.693" : count == 1 ? "0.465 0.532" : "0.465 0.375", count == 0 ? "0.543 0.832" : count == 1 ? "0.543 0.671" : "0.543 0.514", HasImage(image));
                        elementsContainer.Add(backgroundImage);
                    }

                    elementsContainer.Add(new CuiButton
                    {
                        Text =
                        {
                            Text = dataDetails?.itemList?[1] != null
                                ? $"{dataDetails.itemList[1]?.itemDef?.displayName.english} x {dataDetails.itemList[1]?.quantity}"
                                : "Item Not Found",
                            FontSize = 20,
                            Align = TextAnchor.LowerCenter
                        },
                        Button =
                        {
                            Close = MainCUIContainer,
                            Command = $"choosepick {value}",
                            Color = "0.5 0.5 0.5 0"
                        },
                        RectTransform =
                        {
                            AnchorMin = count == 0 ? "0.465 0.693": count == 1 ? "0.465 0.532" : "0.465 0.375",
                            AnchorMax = count == 0 ? "0.543 0.832" : count == 1 ? "0.543 0.671" : "0.543 0.514"
                        }
                    }, MainCUIContainer);

                    if (dataDetails?.itemList != null && dataDetails.itemList.Count >= 3)
                    {
                        var image = playerItems[player.userID] != null
                            ? dataDetails.itemList[2]?.itemDef?.shortname
                            : "Item Not Found";
                        backgroundImage = CreateImage(MainCUIContainer, image, count == 0 ? "0.639 0.693" : count == 1 ? "0.639 0.532" : "0.639 0.375", count == 0 ? "0.717 0.832" : count == 1 ? "0.717 0.671" : "0.717 0.514", HasImage(image));
                        elementsContainer.Add(backgroundImage);
                    }

                    elementsContainer.Add(new CuiButton
                    {
                        Text =
                        {
                            Text = dataDetails?.itemList?[2] != null
                                ? $"{dataDetails.itemList?[2]?.itemDef?.displayName.english} x {dataDetails.itemList[2]?.quantity}"
                                : "Item Not Found",
                            FontSize = 20,
                            Align = TextAnchor.LowerCenter
                        },
                        Button =
                        {
                            Close = MainCUIContainer,
                            Command = $"choosepick {value}",
                            Color = "0.5 0.5 0.5 0"
                        },
                        RectTransform =
                        {
                            AnchorMin = count == 0 ? "0.639 0.693" : count == 1 ? "0.639 0.532" : "0.639 0.375",
                            AnchorMax = count == 0 ? "0.717 0.832" : count == 1 ? "0.717 0.671" : "0.717 0.514"
                        }
                    }, MainCUIContainer);

                    count++;
                }
            

            elementsContainer.Add(new CuiButton
            {
                Text =
                {
                    Text = "Accept",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter
                },
                Button =
                {
                    Close = MainCUIContainer,
                    Command = $"ChoosePickReward {variable}",
                    Color = "0 0.5 0 0.5"
                },
                RectTransform =
                {
                    AnchorMin = "0.293 0.280",
                    AnchorMax = "0.717 0.350"
                }
            }, MainCUIContainer);

            elementsContainer.Add(new CuiButton
            {
                Text =
                {
                    Text = "Close",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter
                },
                Button =
                {
                    Close = MainCUIContainer,
                    Command = "closedailyui",
                    Color = "0.5 0.5 0.5 0.2"
                },
                RectTransform =
                {
                    AnchorMin = "0.95 0.95",
                    AnchorMax = "1 1"
                }
            }, MainCUIContainer);

            CuiHelper.AddUi(player, elementsContainer);
        }

        [ConsoleCommand("ChoosePickReward")]
        void cmdChoosePickReward(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return;

            if (args.Args.Length == 0 || args.Args[0] == "0")
            {
                DestroyUi(player);
                OpenUi(player);
                return;
            }

            if (PlayerData == null) PlayerData = new Dictionary<ulong, DateTime>();
            if (PlayerData.ContainsKey(player.userID))
            {
                DateTime start = PlayerData[player.userID];
                DateTime end = start.AddDays(1);
                DateTime today = DateTime.Today;

                int value = DateTime.Compare(end.Date, today.Date);
                if (value >= 0)
                {
                    SendReply(player, lang.GetMessage("CantPick", this, player.UserIDString));
                    return;
                }
            }
            if (args.Args.Length > 0)
            {
                int pick = int.Parse(args.Args[0]);

                if (playerItems != null && playerItems.ContainsKey(player.userID))
                {
                    var item = playerItems[player.userID][pick - 1].itemList;
                    bool onground = false;
                    foreach(var items in item)
                    {
                        var itm = ItemManager.CreateByName(items.itemDef.shortname, items.quantity);
                        if (!itm.MoveToContainer(player.inventory.containerMain))
                        {
                            itm.Drop(player.eyes.position, player.eyes.BodyForward() * 2f);
                            if (!PlayerData.ContainsKey(player.userID))
                            {
                                PlayerData.Add(player.userID, DateTime.Today);
                            }

                            onground = true;
                        }
                        else
                        {
                            if (!PlayerData.ContainsKey(player.userID))
                            {
                                PlayerData.Add(player.userID, DateTime.Today);
                            }
                        }
                    }

                    if (onground)
                        SendReply(player, lang.GetMessage("OnGround", this, player.UserIDString));
                    else
                        SendReply(player, lang.GetMessage("Inventory", this, player.UserIDString));
                }
            }
        }

        [ConsoleCommand("closedailyui")]
        void cmdCloseCommand(ConsoleSystem.Arg args)
        {
            if (args.Player() != null)
            {
                DestroyUi(args.Player());
            }
        }

        [ConsoleCommand("choosepick")]
        void cmdchoosepickCommand(ConsoleSystem.Arg args)
        {
            if (args.Player() != null)
            {
                DestroyUi(args.Player());
                OpenUi(args.Player(), args.Args[0]);
            }
        }

        [ChatCommand("dailyreset")]
        void cmdReset(BasePlayer player, string cmd, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, adminperm))
            {
                PlayerData.Clear();
                playerItems.Clear();
                SendReply(player, lang.GetMessage("DataReset", this, player.UserIDString));
            }
            else
                SendReply(player, lang.GetMessage("NoPerm", this, player.UserIDString));
        }

        [ChatCommand("daily")]
        void cmdChat(BasePlayer player, string cmd, string[] args)
        {
            if(permission.UserHasPermission(player.UserIDString, userperm))
                OpenUi(player);
            else
                SendReply(player, lang.GetMessage("NoPerm", this, player.UserIDString));
        }
    }
}