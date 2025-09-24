using System;
using UnityEngine;
using System.Text;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using ConVar;
using Oxide.Core;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Collections;
		   		 		  						  	   		  		 			   		 		  		  		   			
namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("IQCases", "https://discord.gg/dNGbxafuJn", "1.0.0")]
    [Description("Кейсы на ваш сервер | Cases to your server")]
    class IQCases : RustPlugin
    {
        internal class TrackerTimeData
        {
            public Int32 Time;
            public Int32 AmountTakes;
        }
        
                public class ItemSettings
        {
            [JsonProperty(LanguageEn ? "Is this a blueprint? (There should be a mode `Command - true | Thing - false : false`" : "Это чертеж? (Должен стоять режим `Команда - true | Вещь - false : false`")]
            public bool IsBlueprint;
            [JsonProperty(LanguageEn ? "Icon for the team" : "Иконка для команды")]
            public string URLCommand;
            [JsonProperty(LanguageEn ? "Minimum quantity for an item" : "Минимальное количество для предмета")]
            public int MinAmount;
            [JsonProperty(LanguageEn ? "Command - true | Item - false" : "Команда - true | Вещь - false")]
            public bool CommandUse;
            [JsonProperty(LanguageEn ? "Command" : "Команда")]
            public string Command;
            [JsonProperty(LanguageEn ? "Item shortname" : "Shortname предмета")]
            public string Shortname;
            [JsonProperty(LanguageEn ? "SkinID item" : "SkinID предмета")]
            public ulong SkinID;
            [JsonProperty(LanguageEn ? "Maximum quantity for an item" : "Максимальное количество для предмета")]
            public int MaxAmount;
        }

        
        
                void Metods_Open_Case(BasePlayer player,string CaseKey)
        {
            if (!UserAmountCase(player, CaseKey)) return;

            DataPlayer[player.userID][CaseKey]--;
            ServerMgr.Instance.StartCoroutine(Interface_Animation(player, CaseKey));
            Interface.Oxide.CallHook("OpenCase", player, config.CaseList[CaseKey].DisplayName);
        }

        [ConsoleCommand("iqcase")] 
        void AdminCommandCase(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin) return;
		   		 		  						  	   		  		 			   		 		  		  		   			
            switch (arg.Args[0].ToLower())
            {
                case "give":
                    {
                        ulong ID = ulong.Parse(arg.Args[1]);
                        BasePlayer player = BasePlayer.FindByID(ID);
                        string CaseKey = arg.Args[2];
                        int Amount = Convert.ToInt32(arg.Args[3]);
                        Metods_Case_Give(player.userID, CaseKey, Amount);
                        break;
                    }
                
            }
        }

        
        
        void Interface_My_Inventory(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_MY_INVENTORY);
            CuiHelper.DestroyUi(player, UI_TAKE_ITEM);
            CuiHelper.DestroyUi(player, UI_WHY_CASE);

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat("#282721E6"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, "ELEMENT_PANEL", UI_MY_INVENTORY);
		   		 		  						  	   		  		 			   		 		  		  		   			
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9046296", AnchorMax = "1 1" },
                Text = { Text = GetLang("UI_BTN_INVENTORY",player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, UI_MY_INVENTORY);

            CuiHelper.AddUi(player, container);

            Interface_My_Inventory_LoadeItems(player);
        }
        
        
        private void SetBalance(UInt64 userID, Int32 Balance)
        {
            if (IQEconomic != null)
                IQEconomic?.Call("API_SET_BALANCE", userID, Balance);
            else if (Economics != null)
                Economics?.Call("Deposit", userID, (Double)Balance);
        }
        
        
        [ChatCommand("case")]
        void ChatCommandCase(BasePlayer player)
        {
            Interface_Cases(player);
        }
        
                private static Configuration config = new Configuration();
        
                [JsonProperty(LanguageEn ? "Players' time date" : "Дата времени игроков")]
        public Dictionary<ulong, Dictionary<string, TrackerTimeData>> TrackerTimePlayer = new Dictionary<ulong, Dictionary<string, TrackerTimeData>>();

        
        
        void Metods_IQEconomicBuyCase(BasePlayer player,string CaseKey)
        {
            var Case = config.CaseList[CaseKey];
            var Data = DataPlayer[player.userID];
            if (!IsRemovedBalance(player.userID, Case.IQEconomicPrice))
            {
                SendChat(GetLang("IQECONOMIC_CASE_NO_MONEY", player.UserIDString, Case.DisplayName), player);
                return;
            }
            if (Data.ContainsKey(CaseKey))
                Data[CaseKey]++;
            else Data.Add(CaseKey, 1);

            RemoveBalance(player.userID, Case.IQEconomicPrice);
            SendChat(GetLang("IQECONOMIC_CASE_BUY_ACCESS", player.UserIDString, Case.DisplayName), player);
        }
        private IEnumerator DownloadImages()
        {
            PrintError("AddImages SkyPlugins.ru...");
		   		 		  						  	   		  		 			   		 		  		  		   			
            var Image = config.CaseList;
            foreach (var Img in Image)
                foreach (var ShortnameList in Img.Value.ItemSetting.Where(x => !x.CommandUse && !String.IsNullOrWhiteSpace(x.Shortname)))
                {
                    String URL = $"http://api.skyplugins.ru/api/getimage/{ShortnameList.Shortname}/256";
                    String KeyName = $"{ShortnameList.Shortname}_256px_case";
                    if (!HasImage(KeyName))
                        AddImage(URL, KeyName);

                    yield return new WaitForSeconds(0.04f);
                }
            yield return new WaitForSeconds(0.04f);

            PrintError("AddImages SkyPlugins.ru - completed..");
        }
        public Boolean HasImage(String imageName) => (Boolean)ImageLibrary?.Call("HasImage", imageName);
        void CachedImage(BasePlayer player)
        {
            var Image = config.CaseList;
            foreach (var Img in Image)
            {
                SendImage(player, Img.Key);
                foreach (var Item in Img.Value.ItemSetting)
                    SendImage(player, Item.Command);
            }
            SendImage(player,"LEFT_ARROW");
            SendImage(player,"RIGHT_ARROW");
        }

        [ConsoleCommand("case")]
        void SystemCommandCase(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            switch (arg.Args[0].ToLower())
            {
                case "take":
                    {
                        var CaseKey = arg.Args[1];
                        Interface_Take_Element(player, CaseKey);
                        break;
                    }
                case "whyitem":
                    {
                        var CaseKey = arg.Args[1];
                        Interface_Why_Case(player, CaseKey);
                        break;
                    }
                case "open":
                    {
                        var CaseKey = arg.Args[1];
                        Metods_Open_Case(player, CaseKey);
                        break;
                    }
                case "takeitem": 
                    {
                        CuiHelper.DestroyUi(player, STOP_BLOCK_INFO);
                        var CaseKey = arg.Args[1];
                        int ItemIndex = Convert.ToInt32(arg.Args[2]);
                        int ItemAmount = Convert.ToInt32(arg.Args[3]);
                        var Item = config.CaseList[CaseKey].ItemSetting[ItemIndex];
                        Metods_Take_Prize(player, Item, ItemAmount);
                        break;
                    }
                case "inventory":
                    { 
                        switch (arg.Args[1].ToLower())
                        {
                            case "open":
                                {
                                    Interface_My_Inventory(player);
                                    break;
                                }
                            case "take":
                                {
                                    int IndexSlot = Convert.ToInt32(arg.Args[2]);
                                    var inventoryClass = InventoryPlayer[player.userID][IndexSlot];
                                    Metods_Take_Inventory(player, inventoryClass);
                                    break;
                                }
                            case "page.controller":
                                {
                                    Int32 Page = (Int32)Convert.ToInt32(arg.Args[2]);
                                    String PageAction = (String)arg.Args[3];
                                    switch (PageAction)
                                    {
                                        case "next":
                                            {
                                                Interface_My_Inventory_LoadeItems(player, Page + 1);
                                                break;
                                            }
                                        case "back":
                                            {
                                                Interface_My_Inventory_LoadeItems(player, Page - 1);
                                                break;
                                            }
                                    }
                                    break;
                                }
                        }
                        break;
                    }
                case "iqeconomic":
                    {
                        switch (arg.Args[1].ToLower())
                        {
                            case "buy":
                                {
                                    var CaseKey = arg.Args[2];
                                    Metods_IQEconomicBuyCase(player, CaseKey);
                                    break;
                                }
                            case "sell":
                                {
                                    var CaseKey = arg.Args[2];
                                    Metods_IQEconomicSellCase(player, CaseKey);
                                    break;
                                }
                        }
                        break;
                    }
            }
        }
        
                private String GetImage(String fileName, UInt64 skin = 0)
        {
            var imageId = (String)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return String.Empty;
        }

        
                void Interface_Why_Case(BasePlayer player,string CaseKey)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_TAKE_ITEM);
            CuiHelper.DestroyUi(player, UI_WHY_CASE);

            var Case = config.CaseList[CaseKey];

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat("#282721E6"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, "ELEMENT_PANEL", UI_WHY_CASE);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9046296", AnchorMax = "1 1" },
                Text = { Text = GetLang("UI_CASE_WHY_CASE_TITLE", player.UserIDString, Case.DisplayName), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            },  UI_WHY_CASE);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = UI_WHY_CASE, Color = "0 0 0 0" },
                Text = { Text = "" }
            },  UI_WHY_CASE);

            int i = 0;
            int ItemCount = 0;
            float itemMinPosition = 219f;
            float itemWidth = 0.413646f - 0.28f; /// 
            float itemMargin = 0.439895f - 0.41f; /// 
            int itemCount = Case.ItemSetting.Count;
            float itemMinHeight = 0.7f; // 
            float itemHeight = 0.17f; /// 
            int ItemTarget = 6;

            if (itemCount > ItemTarget)
            {
                itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                itemCount -= ItemTarget;
            }
            else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;

            foreach (var Items in Case.ItemSetting)
            {
                int MinAmount = Items.MinAmount;
                int MaxAmount = Items.MaxAmount;

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{itemMinPosition} {itemMinHeight}", AnchorMax = $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}" },
                    Image = { FadeIn = 0.15f, Color = HexToRustFormat("#FFFFFF15") }
                },  UI_WHY_CASE, $"ITEM_{i}");

                if(Items.IsBlueprint)
                {
                    container.Add(new CuiElement
                    {
                        Parent = $"ITEM_{i}",
                        Components =
                        { 
                            new CuiRawImageComponent { Png = GetImage("blueprintbase") },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                        }
                    });
                }

                container.Add(new CuiElement
                {
                    Parent = $"ITEM_{i}",
                    Components =
                {
                    new CuiRawImageComponent { Png = GetImage(ImageGetItems(CaseKey, i)) },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"1 1"},
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.2" },
                    Text = { Text = $"<b>{MinAmount} - {MaxAmount}</b>", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
                },  $"ITEM_{i}");
		   		 		  						  	   		  		 			   		 		  		  		   			
                ItemCount++;
                i++;
                itemMinPosition += (itemWidth + itemMargin);
                if (ItemCount % ItemTarget == 0)
                {
                    itemMinHeight -= (itemHeight + (itemMargin * 2f));
                    if (itemCount > ItemTarget)
                    {
                        itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                        itemCount -= ItemTarget;
                    }
                    else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                }
            }

            CuiHelper.AddUi(player, container);
        }
        string GetRankName(string Key)
        {
            string Rank = string.Empty;
            if (!IQRankSystem) return Rank;
            return (string)IQRankSystem?.Call("API_GET_RANK_NAME", Key); 
        }

        void AddAllImage()
        {
            var Image = config.CaseList;
            foreach (var Img in Image)
            {
                AddImage(Img.Value.URLCase, Img.Key);

                foreach (var Item in Img.Value.ItemSetting)
                    AddImage(Item.URLCommand, Item.Command);
            }
            if (!HasImage("LEFT_ARROW"))
                AddImage("https://i.imgur.com/s3FmUnW.png", "LEFT_ARROW");
            if (!HasImage("RIGHT_ARROW"))
                AddImage("https://i.imgur.com/FMeWZWa.png", "RIGHT_ARROW");
        }
        bool API_IS_CASE_EXIST(string CaseKey)
        {
            if (config.CaseList.ContainsKey(CaseKey))
                return true;
            else return false;
        }

        void Interface_My_Inventory_LoadeItems(BasePlayer player, Int32 Page = 0)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, $"{UI_MY_INVENTORY}_CONTENT");
            CuiHelper.DestroyUi(player, $"PAGE_BACK");
            CuiHelper.DestroyUi(player, $"PAGE_NEXT");

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { FadeIn = 0.15f, Color = "0 0 0 0" }
            }, UI_MY_INVENTORY, $"{UI_MY_INVENTORY}_CONTENT");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = UI_MY_INVENTORY, Color = "0 0 0 0" },
                Text = { Text = "" }
            }, $"{UI_MY_INVENTORY}_CONTENT");

            if (InventoryPlayer[player.userID].Skip(40 * (Page + 1)).Count() > 0)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.8793216 0.008333333", AnchorMax = "1 0.05925926" },
                    Button = { Command = $"case inventory page.controller {Page} next", Color = "0 0 0 0" },
                    Text = { Text = GetLang("UI_CASE_NEXT_PAGE", player.UserIDString) }
                }, $"{UI_MY_INVENTORY}_CONTENT", "PAGE_NEXT");
            }

            if (Page != 0)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.04044669 0.008333333", AnchorMax = "0.1611252 0.05925926" },
                    Button = { Command = $"case inventory page.controller {Page} back", Color = "0 0 0 0" },
                    Text = { Text = GetLang("UI_CASE_BACK_PAGE", player.UserIDString) }
                }, $"{UI_MY_INVENTORY}_CONTENT", "PAGE_BACK");
            }

            for (int i = 0, x = 0, y = 0; i < 40; i++)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.03718204 + (x * 0.12)} {0.7638896 - (y * 0.17)}", AnchorMax = $"{0.1402479 + (x * 0.12)} {0.9101858 - (y * 0.17)}" },
                    Image = { FadeIn = 0.15f, Color = HexToRustFormat("#FFFFFF2E") }
                }, $"{UI_MY_INVENTORY}_CONTENT", $"SLOT_{i}");

                x++;
                if (x >= 8)
                {
                    y++;
                    x = 0;
                }
            }

            int slot = 0;
            foreach (var Inventory in InventoryPlayer[player.userID].Skip(40 * Page).Take(40))
            {
                if (Inventory.ItemClass.IsBlueprint)
                {
                    container.Add(new CuiElement
                    {
                        Parent = $"SLOT_{slot}",
                        Components =
                        {
                            new CuiRawImageComponent { Png = GetImage("blueprintbase") },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                        }
                    });
                }
		   		 		  						  	   		  		 			   		 		  		  		   			
                container.Add(new CuiElement
                {
                    Parent = $"SLOT_{slot}",
                    Components =
                    {
                        new CuiRawImageComponent { Png = GetImage(ImageGetItemsInventory(player.userID,Inventory.ItemClass)) },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"1 1"},
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.2" },
                    Text = { Text = $"<b>{Inventory.Amount}</b>", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
                }, $"SLOT_{slot}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Close = UI_MY_INVENTORY, Command = $"case inventory take {slot}", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, $"SLOT_{slot}");
                slot++;
            }

            CuiHelper.AddUi(player, container);
        }

        private void RemoveBalance(UInt64 userID, Int32 Balance)
        {
            if (IQEconomic != null)
                IQEconomic?.Call("API_REMOVE_BALANCE", userID, Balance);
            else if (Economics != null)
                Economics?.Call("Withdraw", userID, Convert.ToDouble(Balance));
        }

        void Metods_IQEconomicSellCase(BasePlayer player, string CaseKey)
        {
            var Case = config.CaseList[CaseKey];
            var Data = DataPlayer[player.userID];
            if(!UserAmountCase(player, CaseKey))
            {
                SendChat(GetLang("IQECONOMIC_CASE_NO_CASE_SELL", player.UserIDString), player);
                return;
            }
            Data[CaseKey]--;
            SetBalance(player.userID, Case.IQEconomicPriceSell);
            SendChat(GetLang("IQECONOMIC_CASE_SELL_ACCESS", player.UserIDString, Case.DisplayName, Case.IQEconomicPriceSell), player);
        }
        public static string UI_CASE_ANIMATION = "MAIN_UI_ANIMATION_USER";

        
        
        public void ShowReward(BasePlayer player, string CaseKey, List<int> PossibleAwards)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_CASE_SHOW_REWARD);
		   		 		  						  	   		  		 			   		 		  		  		   			
            var Case = config.CaseList[CaseKey];
            int VariblePrizeAmount = Case.VariblesPrize ? PossibleAwards.Count >= 3 ? 3 : PossibleAwards.Count : 1;
            string VariblePrizeLang = Case.VariblesPrize ? PossibleAwards.Count >= 3 ? "UI_CASE_SHOW_PRIZE_TAKE" : "UI_CASE_SHOW_PRIZE_TAKE" : "UI_CASE_SHOW_PRIZE_YOUR";

            container.Add(new CuiPanel
            {
                FadeOut = 0.2f,
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat("#282721E6"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, UI_TAKE_ITEM, UI_CASE_SHOW_REWARD);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage(VariblePrizeLang, this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            },  UI_CASE_SHOW_REWARD);

            if (config.GeneralSetting.InventoryClearWipe)
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.09" },
                    Text = { Text = lang.GetMessage("UI_CASE_SHOW_PRIZE_DROP_INVENTORY", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
                }, UI_CASE_SHOW_REWARD);
            }
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9", AnchorMax = "1 0.93" },
                Text = { Text = lang.GetMessage("UI_CASE_SHOW_PRIZE_DESC", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, UI_CASE_SHOW_REWARD);

            
            int ItemCount = 0;
            float itemMinPosition = 219f;
            float itemWidth = 0.413646f - 0.18f; /// 
            float itemMargin = 0.439895f - 0.41f; /// 
            int itemCount = Case.ItemSetting.Count;
            float itemMinHeight = 0.35f; // 
            float itemHeight = 0.3f; /// 
            int ItemTarget = VariblePrizeAmount;

            if (itemCount > ItemTarget)
            {
                itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                itemCount -= ItemTarget;
            }
            else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;

            
            for (int i = 0; i < VariblePrizeAmount; i++)
            {
                int RandomIndexPossible = PossibleAwards[UnityEngine.Random.Range(0, PossibleAwards.Count)];
                var Item = Case.ItemSetting[RandomIndexPossible];
                int RandomAmount = UnityEngine.Random.Range(Item.MinAmount, Item.MaxAmount);

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{itemMinPosition} {itemMinHeight}", AnchorMax = $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}" },
                    Image = { FadeIn = 0.15f, Color = HexToRustFormat("#FFFFFF10") }
                }, UI_CASE_SHOW_REWARD, $"PRIZE_{i}");

                if (Item.IsBlueprint)
                {
                    container.Add(new CuiElement
                    {
                        Parent = $"PRIZE_{i}",
                        Components =
                        {
                            new CuiRawImageComponent { Png = GetImage("blueprintbase") },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                        }
                    });
                }
		   		 		  						  	   		  		 			   		 		  		  		   			
                container.Add(new CuiElement
                {
                    Parent = $"PRIZE_{i}",
                    Components =
                    {
                        new CuiRawImageComponent { Png = GetImage(ImageGetItems(CaseKey,RandomIndexPossible)) },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"1 1"},
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = $"{RandomAmount}", Font = "robotocondensed-bold.ttf", Align = TextAnchor.LowerCenter, FadeIn = 0.3f }
                },  $"PRIZE_{i}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Close = UI_TAKE_ITEM, Command = $"case takeitem {CaseKey} {RandomIndexPossible} {RandomAmount}", Color = "0 0 0 0" },
                    Text = { Text = "" }
                },  $"PRIZE_{i}");

                
                ItemCount++;
                itemMinPosition += (itemWidth + itemMargin);
                if (ItemCount % ItemTarget == 0)
                {
                    itemMinHeight -= (itemHeight + (itemMargin * 2f));
                    if (itemCount > ItemTarget)
                    {
                        itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                        itemCount -= ItemTarget;
                    }
                    else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                }

                            } 

            CuiHelper.AddUi(player, container);
        }
        void RunEffect(BasePlayer player, string path)
        {
            Effect effect = new Effect();
            effect.Init(Effect.Type.Generic, player.transform.position, player.transform.forward, (Network.Connection)null);
            effect.pooledString = path; EffectNetwork.Send(effect, player.net.connection);
        }

        void Metods_Search_Case(BasePlayer player, string CrateName)
        {
            foreach (var Case in config.CaseList.Where(x => x.Value.UseDropList))
            {
                foreach (var Item in Case.Value.CasesDropList.Where(x => x.Key.Contains(CrateName)))
                {
                    if (!GetRandomDrop(Item.Value)) continue;
                    Metods_Case_Give(player.userID, Case.Key, 1);
                    break;
                }
            }
        }
        
        
        public bool GetRandomDrop(int RandomInt)
        {
            int Random = UnityEngine.Random.Range(0, 100);
            if (RandomInt >= Random)
                return true;
            else return false;
        }
        
                void Interface_Take_Element(BasePlayer player,string CaseKey)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_TAKE_ITEM);

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat("#282721E6"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            },  "ELEMENT_PANEL", UI_TAKE_ITEM);

            int CaseAmountUser = GetAmountCase(player, CaseKey);
            var Case = config.CaseList[CaseKey];
            string HexIcon = UserAmountCase(player,CaseKey) ? "#FFFFFFFF" : "#FFFFFF9E";
            
            string RankName = string.Empty;
            if (IQRankSystem)
                if (!String.IsNullOrWhiteSpace(Case.IQRankKey))
                    if (!IsRank(player.userID, Case.IQRankKey))
                        RankName = $"{GetRankName(Case.IQRankKey)}";

            string BtnCase = !String.IsNullOrWhiteSpace(Case.PermissionOpenCase) ? permission.UserHasPermission(player.UserIDString, Case.PermissionOpenCase) ? UserAmountCase(player, CaseKey) ? String.IsNullOrWhiteSpace(RankName) ? "UI_CASE_BTN_OPEN" : "UI_CASE_BTN_NO_RANK_OPEN" : "UI_CASE_BTN_NO_OPEN" : "UI_CASE_BTN_NO_PERM_OPEN_OPEN" : UserAmountCase(player, CaseKey) ? "UI_CASE_BTN_OPEN" : "UI_CASE_BTN_NO_OPEN";
            string BtnCaseCMD = !String.IsNullOrWhiteSpace(Case.PermissionOpenCase) ? permission.UserHasPermission(player.UserIDString, Case.PermissionOpenCase) ? UserAmountCase(player, CaseKey) ? String.IsNullOrWhiteSpace(RankName) ?  $"case open {CaseKey}" : "" : "" : "" : UserAmountCase(player, CaseKey) ? $"case open {CaseKey}" : "";

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.2002609 0.1833802", AnchorMax = "0.8330071 0.8592593" },
                Image = { FadeIn = 0.15f, Color = "0 0 0 0"}
            },  UI_TAKE_ITEM);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = UI_TAKE_ITEM, Color = "0 0 0 0" },
                Text = { Text = "" }
            }, UI_TAKE_ITEM);

            container.Add(new CuiElement
            {
                Parent = UI_TAKE_ITEM,
                Components =
                {
                    new CuiRawImageComponent { Png = GetImage(CaseKey),Color = HexToRustFormat(HexIcon) },
                    new CuiRectTransformComponent{ AnchorMin = "0.0670104 0.3164383", AnchorMax = $"0.4371135 0.8082191"},
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.2556702 0.8232866", AnchorMax = "0.9969074 0.9479442" },
                Text = { Text = $"<b>{Case.DisplayName.ToUpper()}</b>", Color = HexToRustFormat("#F7EAE0FF"), FontSize = 30, Font = "robotocondensed-bold.ttf", Align = TextAnchor.LowerLeft, FadeIn = 0.3f }
            }, UI_TAKE_ITEM);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.4154639 0.5410959", AnchorMax = "0.7412371 0.7246575" },
                Text = { Text = GetLang("UI_CASE_TAKE_DESCRIPTION",player.UserIDString, Case.DisplayName), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperLeft, FadeIn = 0.3f }
            }, UI_TAKE_ITEM);
		   		 		  						  	   		  		 			   		 		  		  		   			
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.4134021 0.3986301", AnchorMax = "0.843299 0.4972603" },
                Text = { Text = GetLang("UI_CASE_TAKE_AMOUNT_CASE", player.UserIDString, CaseAmountUser), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperLeft, FadeIn = 0.3f }
            },  UI_TAKE_ITEM);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.14124 0.2876713", AnchorMax = "0.4041264 0.3342465" }, 
                Button = { Command = BtnCaseCMD, Color = HexToRustFormat("#B51919C7"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                Text = { Text = GetLang(BtnCase, player.UserIDString, RankName), Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#E9D2C1FF") }
            },  UI_TAKE_ITEM);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.14124 0.2410958", AnchorMax = "0.4041264 0.2835616" },
                Button = { Command = $"case whyitem {CaseKey}", Color = HexToRustFormat("#4D3737C7"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                Text = { Text = lang.GetMessage("UI_CASE_BTN_WHY_CASE", this, player.UserIDString), Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#E9D2C1FF") }
            }, UI_TAKE_ITEM);
		   		 		  						  	   		  		 			   		 		  		  		   			
                        if (IQEconomic || Economics)
            {
                if (Case.IQEconomicUse)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.6371135 0.2876713", AnchorMax = "0.7989691 0.3342465" },
                        Button = { Close = UI_MAIN_UI, Command = $"case iqeconomic buy {CaseKey}", Color = HexToRustFormat("#607633FF"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                        Text = { Text = $"{Case.IQEconomicPrice}", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#91CA2EFF") }
                    }, UI_TAKE_ITEM, "BTN_ECONOMIC_BUY");


                    container.Add(new CuiElement
                    {
                        Parent = "BTN_ECONOMIC_BUY",
                        Components =
                            {
                                new CuiImageComponent { Sprite = "assets/icons/store.png", Color = HexToRustFormat("#91CA2EFF") },
                                new CuiRectTransformComponent{ AnchorMin = "0.02547757 0.1470587", AnchorMax = $"0.1783439 0.8529433"},
                            }
                    });
                }
                if(Case.IQEconomicUseSell)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.6371135 0.2410958", AnchorMax = "0.7989691 0.2835616" },
                        Button = { Close = UI_MAIN_UI, Command = $"case iqeconomic sell {CaseKey}", Color = HexToRustFormat("#900E0EC7"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                        Text = { Text = $"{Case.IQEconomicPriceSell}", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#91CA2EFF") }
                    }, UI_TAKE_ITEM, "BTN_ECONOMIC_SELL");

                    container.Add(new CuiElement
                    {
                        Parent = "BTN_ECONOMIC_SELL",
                        Components =
                            {
                                new CuiImageComponent { Sprite = "assets/icons/refresh.png", Color = HexToRustFormat("#B51919C7") },
                                new CuiRectTransformComponent{ AnchorMin = "0.02547757 0.1470587", AnchorMax = $"0.1783439 0.8529433"},
                            }
                    });
                }
            }
            
            CuiHelper.AddUi(player, container); 
        }
        bool API_IS_CASE_PLAYER(BasePlayer player, string CaseKey) => UserAmountCase(player, CaseKey);
        public Boolean AddImage(String url, String shortname, UInt64 skin = 0) => (Boolean)ImageLibrary?.Call("AddImage", url, shortname, skin);
        public string GetLang(string LangKey, string userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }
        [JsonProperty(LanguageEn ? "Player Inventory" : "Инвентарь игроков")]
        public Dictionary<ulong, List<Inventory>> InventoryPlayer = new Dictionary<ulong, List<Inventory>>();

        
        
        void Metods_Take_Inventory(BasePlayer player, Inventory inventoryClass) 
        {
            if (!InventoryPlayer[player.userID].Contains(inventoryClass)) return;

            var InventoryItem = inventoryClass.ItemClass;
            if (InventoryItem.CommandUse)
                rust.RunServerCommand(InventoryItem.Command.Replace("%STEAMID%", player.UserIDString));
            else
            {
                if (InventoryItem.IsBlueprint)
                {
                    ItemDefinition definition = ItemManager.FindItemDefinition(InventoryItem.Shortname);
		   		 		  						  	   		  		 			   		 		  		  		   			
                    Item itemBp = ItemManager.CreateByItemID(-996920608, inventoryClass.Amount);
                    if (itemBp.instanceData == null)
                        itemBp.instanceData = new ProtoBuf.Item.InstanceData();
                    itemBp.instanceData.ShouldPool = false;
                    itemBp.instanceData.blueprintAmount = 1;
                    itemBp.instanceData.blueprintTarget = definition.itemid;
                    itemBp.MarkDirty();
                    player.GiveItem(itemBp, BaseEntity.GiveItemReason.PickedUp);
                }
                else
                {
                    Item item = ItemManager.CreateByName(InventoryItem.Shortname, inventoryClass.Amount, InventoryItem.SkinID);
                    player.GiveItem(item);
                }
            }
            InventoryPlayer[player.userID].Remove(inventoryClass);
            SendChat(lang.GetMessage("CHAT_INVENTORY_TAKE_ACCESS", this, player.UserIDString), player);
        }

        public static StringBuilder sb = new StringBuilder();
        
        
                public static string UI_MAIN_UI = "MAIN_PLAYER_UI";
        void RegisteredDataUser(BasePlayer player)
        {
            if (!DataPlayer.ContainsKey(player.userID))
                DataPlayer.Add(player.userID, new Dictionary<string, int> { });
            if (!InventoryPlayer.ContainsKey(player.userID))
                InventoryPlayer.Add(player.userID,new List<Inventory> { });
            if (!TrackerTimePlayer.ContainsKey(player.userID))
                TrackerTimePlayer.Add(player.userID, new Dictionary<string, TrackerTimeData> { });
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
                PrintWarning(LanguageEn ? "Error #49" + $" of reading the configuration 'oxide/config/{Name}', creating a new configuration!" : "Ошибка #49" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию! #33");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }
        private class Configuration
        {
            [JsonProperty(LanguageEn ? "Plugin Settings" : "Настройки плагина")]
            public GeneralSettings GeneralSetting = new GeneralSettings();
            [JsonProperty(LanguageEn ? "Case settings [Each case is configured individually, you can use the functionality as your heart desires!]" : "Настройки кейсов [Каждый кейс настраивается индивидуально,вы можете использовать функционал как вашей душе угодно!]")]
            public Dictionary<string, Cases> CaseList = new Dictionary<string, Cases>();
            [JsonProperty(LanguageEn ? "Settings for collaboration with other plugins" : "Настройки совместной работы с другими плагинами")]
            public ReferenceSetting ReferenceSettings = new ReferenceSetting();
            internal class GeneralSettings
            {
                [JsonProperty(LanguageEn ? "Enable automatic inventory cleaning in WIPE" : "Включить автоматическую очистку инвентаря в WIPE")]
                public bool InventoryClearWipe;
            }

            internal class Cases
            {
                [JsonProperty(LanguageEn ? "Rights to open the case (if you give access to everyone, leave the field empty)" : "Права для открытия кейса(если давать доступ всем - оставляйте поле пустым)")]
                public string PermissionOpenCase;
                [JsonProperty(LanguageEn ? "Case name" : "Название кейса")]
                public string DisplayName;
                [JsonProperty(LanguageEn ? "Link to the case icon" : "Ссылка на иконку кейса")]
                public string URLCase;
                [JsonProperty(LanguageEn ? "IQRankSystem : With what rank will this case be available (if not necessary, leave the line empty)" : "IQRankSystem : С каким рангом будет доступен данный кейс(Если не нужно,оставляйте строку пустой)")]
                public string IQRankKey;
                [JsonProperty(LanguageEn ? "Case price(IQEconomic)" : "Цена на кейс(IQEconomic)")]
                public int IQEconomicPrice;
                [JsonProperty(LanguageEn ? "IQEconomic/Economics Use : Allow case purchase" : "IQEconomic/Economics Use : Разрешить покупку кейса")]
                public bool IQEconomicUse;
                [JsonProperty(LanguageEn ? "IQEconomic/Economics Use : Allow case sale" : "IQEconomic/Economics Use : Разрешить продажу кейса")]
                public bool IQEconomicUseSell;
                [JsonProperty(LanguageEn ? "How many coins will be given when selling the case(IQEconomic/Economics)" : "Сколько монет дадут при продаже кейса(IQEconomic/Economics)")]
                public int IQEconomicPriceSell;
                [JsonProperty(LanguageEn ? "Give case for time online" : "Выдавать кейс за время")]
                public bool GiveCaseFromPlaying;
                [JsonProperty(LanguageEn ? "After how many seconds to issue the case" : "Через сколько секунд выдавать кейс")]
                public int PlayingNormalFromGive;
                [JsonProperty(LanguageEn ? "How many cases to give out to the player in total during the time" : "Сколько всего кейсов выдавать игроку за время")]
                public int GiveCount;
                [JsonProperty(LanguageEn ? "Enable the drop of cases from the boxes with a chance" : "Включить выпадение кейсов из ящиков с шансом")]
                public bool UseDropList;
                [JsonProperty(LanguageEn ? "Drop cases and chance [box] = chance" : "Выпадение кейсов и шанс [ящик] = шанс")]
                public Dictionary<string, int> CasesDropList = new Dictionary<string, int>();
                [JsonProperty(LanguageEn ? "Enable the ability to select a reward (1 of 3) - true / Disable the option to choose a reward (there will be one prize) - false" : "Включить возможность выбрать награду(1 из 3) - true / Выключить возможность выбрать награду(будет один приз) - false")]
                public bool VariblesPrize;
                [JsonProperty(LanguageEn ? "Items that will fall out of cases" : "Предметы , которые будут выпадать из кейсов")]
                public List<ItemSettings> ItemSetting = new List<ItemSettings>();  
            }

            internal class ReferenceSetting
            {
                [JsonProperty(LanguageEn ? "IQChat : Chat Settings" : "IQChat : Настройки чата")]
                public ChatSetting ChatSettings = new ChatSetting();
                
                internal class ChatSetting
                {
                    [JsonProperty(LanguageEn ? "IQChat : Custom prefix in the chat" : "IQChat : Кастомный префикс в чате")]
                    public string CustomPrefix;
                    [JsonProperty(LanguageEn ? "IQChat : Custom avatar in the chat (if required)" : "IQChat : Кастомный аватар в чате(Если требуется)")]
                    public string CustomAvatar;
                    [JsonProperty(LanguageEn ? "IQChat : Use UI notifications" : "IQChat : Использовать UI уведомления")]
                    public bool UIAlertUse;
                }
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    GeneralSetting = new GeneralSettings
                    {
                        InventoryClearWipe = true,
                    },
                    CaseList = new Dictionary<string, Cases>
                    {
                        ["freecase"] = new Cases
                        {
                            DisplayName = LanguageEn ? "Freee" : "Халява",
                            PermissionOpenCase = "",
                            IQRankKey = "",
                            IQEconomicUseSell = false,
                            IQEconomicPriceSell = 0,
                            IQEconomicUse = true,
                            IQEconomicPrice = 10,
                            GiveCaseFromPlaying = true,
                            PlayingNormalFromGive = 60,
                            URLCase = LanguageEn ? "https://i.imgur.com/V8uoZHE.png" : "https://i.imgur.com/N93o4D2.png",
                            UseDropList = true,
                            CasesDropList = new Dictionary<string, int> 
                            {
                               ["crate_normal"] = 80, 
                               ["crate_basic"] = 10, 
                            },
                            VariblesPrize = true,
                            ItemSetting = new List<ItemSettings>
                            {
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 10,
                                    MaxAmount = 1000,
                                    Shortname = "wood",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 10,
                                    MaxAmount = 1000,
                                    Shortname = "wood",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = true,
                                    IsBlueprint = false,
                                    Command = "iqcase give %STEAMID% freecase 1",
                                    MinAmount = 1,
                                    MaxAmount = 1,
                                    Shortname = "wood",
                                    SkinID = 0,
                                    URLCommand = LanguageEn ? "https://i.imgur.com/V8uoZHE.png" : "https://i.imgur.com/N93o4D2.png",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = true,
                                    Command = "",
                                    MinAmount = 10,
                                    MaxAmount = 1000,
                                    Shortname = "rifle.ak",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 10,
                                    MaxAmount = 1000,
                                    Shortname = "wood",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = true,
                                    IsBlueprint = false,
                                    Command = "iqcase give %STEAMID% freecase 1",
                                    MinAmount = 3,
                                    MaxAmount = 3,
                                    Shortname = "wood",
                                    SkinID = 0,
                                    URLCommand = LanguageEn ? "https://i.imgur.com/V8uoZHE.png" : "https://i.imgur.com/N93o4D2.png",
                                },
                            },
                        },
                        ["resource"] = new Cases
                        {
                            DisplayName = LanguageEn ? "Resource" : "Ресурсы",
                            IQRankKey = "",
                            PermissionOpenCase = "iqcases.resource",
                            IQEconomicUseSell = true,
                            IQEconomicPriceSell = 5,
                            IQEconomicUse = true,
                            IQEconomicPrice = 10,
                            GiveCaseFromPlaying = false,
                            PlayingNormalFromGive = 60,
                            URLCase = LanguageEn ? "https://i.imgur.com/1aXGygD.png" : "https://i.imgur.com/TLYnMTo.png",
                            UseDropList = true,
                            CasesDropList = new Dictionary<string, int>
                            {
                                ["crate_normal"] = 80,
                                ["crate_basic"] = 10,
                            },
                            VariblesPrize = false,
                            ItemSetting = new List<ItemSettings>
                            {
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 10,
                                    MaxAmount = 1000,
                                    Shortname = "wood",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 10,
                                    MaxAmount = 99999,
                                    Shortname = "wood",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 10,
                                    MaxAmount = 1000,
                                    Shortname = "wood",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 1,
                                    MaxAmount = 3,
                                    Shortname = "rifle.lr300",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 1,
                                    MaxAmount = 3,
                                    Shortname = "rifle.lr300",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 1,
                                    MaxAmount = 3,
                                    Shortname = "rifle.lr300",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 1,
                                    MaxAmount = 3,
                                    Shortname = "rifle.lr300",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 1,
                                    MaxAmount = 3,
                                    Shortname = "rifle.lr300",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 1,
                                    MaxAmount = 3,
                                    Shortname = "rifle.lr300",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                            },
                        },
                        ["raider"] = new Cases
                        {
                            DisplayName = LanguageEn ? "Raider" : "Штурмовик",
                            IQRankKey = "",
                            PermissionOpenCase = "iqcases.raider",
                            IQEconomicUseSell = true,
                            IQEconomicPriceSell = 5,
                            IQEconomicUse = false,
                            IQEconomicPrice = 10,
                            GiveCaseFromPlaying = false,
                            PlayingNormalFromGive = 60,
                            URLCase = LanguageEn ? "https://i.imgur.com/E48Rch3.png" : "https://i.imgur.com/c0rRlwY.png",
                            UseDropList = false,
                            CasesDropList = new Dictionary<string, int>
                            {
                                ["crate_normal"] = 80,
                                ["crate_basic"] = 10,
                            },
                            VariblesPrize = true,
                            ItemSetting = new List<ItemSettings>
                            {
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 1,
                                    MaxAmount = 3,
                                    Shortname = "rifle.lr300",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 1,
                                    MaxAmount = 3,
                                    Shortname = "rifle.lr300",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 1,
                                    MaxAmount = 3,
                                    Shortname = "rifle.lr300",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 1,
                                    MaxAmount = 3,
                                    Shortname = "rifle.lr300",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 1,
                                    MaxAmount = 3,
                                    Shortname = "rifle.lr300",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 1,
                                    MaxAmount = 3,
                                    Shortname = "rifle.lr300",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 1,
                                    MaxAmount = 3,
                                    Shortname = "rifle.lr300",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 1,
                                    MaxAmount = 3,
                                    Shortname = "rifle.lr300",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 1,
                                    MaxAmount = 3,
                                    Shortname = "rifle.lr300",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                            },
                        },
                        ["components"] = new Cases
                        {
                            DisplayName = LanguageEn ? "Components" : "Компоненты",
                            IQRankKey = "",
                            PermissionOpenCase = "iqcases.component",
                            IQEconomicUseSell = true,
                            IQEconomicPriceSell = 5,
                            IQEconomicUse = true,
                            IQEconomicPrice = 10,
                            GiveCaseFromPlaying = false,
                            PlayingNormalFromGive = 60,
                            URLCase = LanguageEn ? "https://i.imgur.com/VqASdqV.png" : "https://i.imgur.com/HULzBVP.png",
                            UseDropList = false,
                            CasesDropList = new Dictionary<string, int>
                            {
                                ["crate_normal"] = 80,
                                ["crate_basic"] = 10,
                            },
                            VariblesPrize = true,
                            ItemSetting = new List<ItemSettings>
                            {
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 10,
                                    MaxAmount = 1000,
                                    Shortname = "scrap",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 10,
                                    MaxAmount = 1000,
                                    Shortname = "scrap",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 10,
                                    MaxAmount = 1000,
                                    Shortname = "scrap",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                            },
                        },
                        ["raider"] = new Cases
                        {
                            DisplayName = LanguageEn ? "Raiders" : "Рейдер",
                            IQRankKey = "",
                            PermissionOpenCase = "iqcases.raider",
                            IQEconomicUseSell = true,
                            IQEconomicPriceSell = 5,
                            IQEconomicUse = true,
                            IQEconomicPrice = 10,
                            GiveCaseFromPlaying = false,
                            PlayingNormalFromGive = 60,
                            URLCase = "https://i.imgur.com/uBTdXnC.png",
                            UseDropList = false,
                            CasesDropList = new Dictionary<string, int>
                            {
                                ["crate_normal"] = 80,
                                ["crate_basic"] = 10,
                            },
                            VariblesPrize = true,
                            ItemSetting = new List<ItemSettings>
                            {
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 10,
                                    MaxAmount = 1000,
                                    Shortname = "explosive.timed",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 10,
                                    MaxAmount = 1000,
                                    Shortname = "explosive.timed",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 10,
                                    MaxAmount = 1000,
                                    Shortname = "explosive.timed",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 10,
                                    MaxAmount = 1000,
                                    Shortname = "explosive.timed",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 10,
                                    MaxAmount = 1000,
                                    Shortname = "explosive.timed",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 10,
                                    MaxAmount = 1000,
                                    Shortname = "explosive.timed",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                                new ItemSettings
                                {
                                    CommandUse = false,
                                    IsBlueprint = false,
                                    Command = "",
                                    MinAmount = 10,
                                    MaxAmount = 1000,
                                    Shortname = "explosive.timed",
                                    SkinID = 0,
                                    URLCommand = "",
                                },
                            },
                        },
                    },
                    ReferenceSettings = new ReferenceSetting
                    {
                        ChatSettings = new ReferenceSetting.ChatSetting
                        {
                            CustomAvatar = "",
                            CustomPrefix = "[IQCASES]\n",
                            UIAlertUse = false,
                        },
                    }

                };
            }
        }
        
        void ReadData()
        {
            DataPlayer = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, int>>>("IQSystem/IQCases/IQCasesUser");
            InventoryPlayer = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, List<Inventory>>>("IQSystem/IQCases/IQInventoryUser");
            TrackerTimePlayer = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, TrackerTimeData>>>("IQSystem/IQCases/TrackerTimePlayer");
        }

        void Metods_Take_Prize(BasePlayer player, ItemSettings ItemSetting, int ItemAmount)
        {
            if (!ItemSetting.CommandUse)
            {
                var Inventory = InventoryPlayer[player.userID].FirstOrDefault(x => x.ItemClass.Shortname == ItemSetting.Shortname);
                if (Inventory != null)
                {
                    ItemDefinition Item = ItemManager.FindItemDefinition(ItemSetting.Shortname);
                    if (Item != null)
                        if (Item.category != ItemCategory.Weapon)
                        {
                            Inventory.Amount += ItemAmount;
                            return;
                        }
                }
            }   
            InventoryPlayer[player.userID].Add(new Inventory { ItemClass = ItemSetting, Amount = ItemAmount });
        }
        void OnNewSave(string filename)
        {
            if (config.GeneralSetting.InventoryClearWipe)
                if (InventoryPlayer != null)
                {
                    InventoryPlayer.Clear();
                    PrintWarning(LanguageEn ? "WIPE detected inventory has been successfully cleaned!" : "Обнаружен WIPE инвентарь был успешно очищен!");
                }
        }

        public class Inventory
        {
            [JsonProperty(LanguageEn ? "Information" : "Информация")]
            public ItemSettings ItemClass = new ItemSettings();
            [JsonProperty(LanguageEn ? "Amount" : "Количество")]
            public int Amount;
        }

        public int GetAmountCase(BasePlayer player, string CaseKey)
        {
            int CaseAmountUser = DataPlayer.ContainsKey(player.userID) ? DataPlayer[player.userID].ContainsKey(CaseKey) ? DataPlayer[player.userID][CaseKey] : 0 : 0;
            return CaseAmountUser;
        }
        public static string UI_CASE_SHOW_REWARD = "MAIN_UI_SHOW_REWARD";
        /// <summary>
        /// Обновление 1.0.0
        /// Изменено :
        /// - Изменена конфигурация, теперь в ней нет лишнего языка!
        /// - Изменена JsonProperty конфигурационного файла
        /// - Изменена JsonProperty дата-файла
        /// - Датафайл перенесен в новую папку IQSystem/IQCases
        /// - Изменен текст в сообщениях консоли
        /// Исправлено :
        /// - Исправлена загрузка изображений
        /// - Исправлена работа с хуком OnPlayerLoot
        /// Добавлено :
        /// - Добавлен .psd проект с кейсами - https://drive.google.com/file/d/1yFGdkrTdY5PAUp7AYAvOM3uMJcIpIbw8/view?usp=sharing
        /// - Добавлена возможность ограничить выдачу кейса за время игры до N количества
        /// - Добавлена поддержка Economics
        /// - Добавлена поддержка чертежей
        /// </summary>

                [PluginReference] Plugin IQChat, IQEconomic, ImageLibrary, IQRankSystem, Economics;

                public void SendChat(string Message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            var Chat = config.ReferenceSettings.ChatSettings;
            if (IQChat)
                if (Chat.UIAlertUse)
                    IQChat?.Call("API_ALERT_PLAYER_UI", player, Message);
                else IQChat?.Call("API_ALERT_PLAYER", player, Message, Chat.CustomPrefix, Chat.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }

        
        
                private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_BTN_INVENTORY"] = "<size=20><color=#E9D2C1FF><b>INVENTORY</b></color></size>",
                ["UI_TITLE_BALANCE"] = "<size=17><color=#E9D2C1FF><b>BALANCE:</b></color></size>",
                ["UI_TITLE_INFO"] = "<size=30><color=#E9D2C1FF><b>IQCASES</b></color></size>",
                ["UI_TITLE_INFO_DESCRIPTION"] = "<size=14><color=#CAAD9EFF><b>Find or buy cases to open them</b></color></size>",
                ["UI_CLOSE"] = "<size=30><color=#CAAD9EFF><b>CLOSE</b></color></size>",
                ["UI_AMOUNT_CASE"] = "<size=16>Amount: {0}</size>",
                ["UI_CASE_TAKE_AMOUNT_CASE"] = "<size=16><b><color=#4E95C7FF>Your number of cases</color> <color=#f5f4f4>{0} x</color></b></size>",
                ["UI_CASE_TAKE_DESCRIPTION"] = "<size=12><b><color=#F89504FF>In this menu you can open the case <color=#e3c393>{0}</color> or see what is in it\n\n*Case can be found or bought</color></b></size>",
                ["UI_CASE_BTN_OPEN"] = "<size=25><b>Open</b></size>",
                ["UI_CASE_BTN_NO_PERM_OPEN_OPEN"] = "<size=14><b>Dont permissions</b></size>",
                ["UI_CASE_BTN_NO_RANK_OPEN"] = "<size=14><b>You need a rank [{0}]</b></size>",
                ["UI_CASE_BTN_NO_OPEN"] = "<size=14><b>You have no cases</b></size>",
                ["UI_CASE_BTN_WHY_CASE"] = "<size=14><b>Why case?</b></size>",
                ["UI_CASE_WHY_CASE_TITLE"] = "<size=30><b>Items inside the case <color=#FFB243FF>{0}</color></b></size>",
                ["UI_CASE_ANIM_LABEL"] = "<size=30><b>UNPACK CASE...</b></size>",
                ["UI_CASE_ANIM_LABEL_TITLE"] = "<size=24><b>POSSIBLE AWARDS</b></size>",
                ["UI_CASE_SHOW_PRIZE_TAKE"] = "<size=30><b>CHOOSE A REWARD</b></size>",
                ["UI_CASE_SHOW_PRIZE_YOUR"] = "<size=30><b>YOUR REWARD</b></size>",
                ["UI_CASE_SHOW_PRIZE_DESC"] = "<size=16><b>Click on the reward to collect it</b></size>",
                ["UI_CASE_SHOW_PRIZE_DROP_INVENTORY"] = "<size=18><b>Your rewards will be stored in inventory\n*Inventory will be cleaned every WIPE</b></size>",

                ["CHAT_CASE_DROP_ACCESS"] = "Congratulations! You found a case {0}",
                ["CHAT_INVENTORY_TAKE_ACCESS"] = "You have successfully taken an item from inventory",

                ["IQECONOMIC_CASE_NO_MONEY"] = "You do not have enough money to buy a case {0}",
                ["IQECONOMIC_CASE_BUY_ACCESS"] = "You have successfully purchased a case {0}",
                ["IQECONOMIC_CASE_NO_CASE_SELL"] = "You do not have cases for sale",
                ["IQECONOMIC_CASE_SELL_ACCESS"] = "You successfully sold the case {0} and got {1} currencies",

                ["UI_CASE_NEXT_PAGE"] = "<size=30><b>NEXT</b></size>",
                ["UI_CASE_BACK_PAGE"] = "<size=30><b>BACK</b></size>",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_BTN_INVENTORY"] = "<size=20><color=#E9D2C1FF><b>ВАШ ИНВЕНТАРЬ</b></color></size>",
                ["UI_TITLE_BALANCE"] = "<size=17><color=#E9D2C1FF><b>БАЛАНС:</b></color></size>",
                ["UI_TITLE_INFO"] = "<size=30><color=#E9D2C1FF><b>IQCASES</b></color></size>",
                ["UI_TITLE_INFO_DESCRIPTION"] = "<size=14><color=#CAAD9EFF><b>Находи или покупай кейсы,чтобы открыть их</b></color></size>",
                ["UI_CLOSE"] = "<size=30><color=#CAAD9EFF><b>ЗАКРЫТЬ</b></color></size>",
                ["UI_CASE_TAKE_AMOUNT_CASE"] = "<size=16><b><color=#4E95C7FF>Ваше количество кейсов</color> <color=#f5f4f4>{0} x</color></b></size>",
                ["UI_CASE_TAKE_DESCRIPTION"] = "<size=12><b><color=#F89504FF>В этом меню вы можете открыть кейс <color=#e3c393>{0}</color> или просмотреть,что в нем находится\n\n*Кейс можно найти или купить</color></b></size>",
                ["UI_CASE_BTN_OPEN"] = "<size=14><b>Открыть</b></size>",
                ["UI_CASE_BTN_NO_PERM_OPEN_OPEN"] = "<size=14><b>У вас недостаточно прав</b></size>",
                ["UI_CASE_BTN_NO_RANK_OPEN"] = "<size=14><b>Вам нужен ранг [{0}]</b></size>",
                ["UI_CASE_BTN_NO_OPEN"] = "<size=14><b>У вас нет кейсов</b></size>",
                ["UI_CASE_BTN_WHY_CASE"] = "<size=14><b>Что внутри?</b></size>",
                ["UI_CASE_WHY_CASE_TITLE"] = "<size=30><b>Предметы находящиеся внутри кейса <color=#FFB243FF>{0}</color></b></size>",
                ["UI_CASE_ANIM_LABEL"] = "<size=30><b>РАСПАКОВКА КЕЙСА...</b></size>",
                ["UI_CASE_ANIM_LABEL_TITLE"] = "<size=24><b>ВОЗМОЖНАЯ НАГРАДА</b></size>",
                ["UI_CASE_SHOW_PRIZE_TAKE"] = "<size=30><b>ВЫБЕРИТЕ НАГРАДУ</b></size>",
                ["UI_CASE_SHOW_PRIZE_YOUR"] = "<size=30><b>ВАША НАГРАДА</b></size>",
                ["UI_CASE_SHOW_PRIZE_DESC"] = "<size=16><b>Нажмите на награду,чтобы забрать ее</b></size>",
                ["UI_CASE_SHOW_PRIZE_DROP_INVENTORY"] = "<size=18><b>Ваши награды будут храниться в инвентаре\n*Инвентарь будет очищаться каждый WIPE</b></size>",

                ["CHAT_CASE_DROP_ACCESS"] = "Поздравляем!Вы получили кейс {0}",
                ["CHAT_INVENTORY_TAKE_ACCESS"] = "Вы успешно забрали предмет из инвентаря",
		   		 		  						  	   		  		 			   		 		  		  		   			
                ["IQECONOMIC_CASE_NO_MONEY"] = "У вас недостаточно средств для покупки кейса {0}",
                ["IQECONOMIC_CASE_BUY_ACCESS"] = "Вы успешно приобрели кейс {0}",
                ["IQECONOMIC_CASE_NO_CASE_SELL"] = "У вас нет кейсов для продажи",
                ["IQECONOMIC_CASE_SELL_ACCESS"] = "Вы успешно продали кейс {0} и получили {1} валюты",

                ["UI_CASE_NEXT_PAGE"] = "<size=30><b>ВПЕРЕД</b></size>",
                ["UI_CASE_BACK_PAGE"] = "<size=30><b>НАЗАД</b></size>",

            }, this, "ru");
            PrintWarning(LanguageEn ? "The language file was uploaded successfully" : "Языковой файл загружен успешно");
        }
        
        
                public Dictionary<ulong, List<ItemSettings>> VariblesPrize = new Dictionary<ulong, List<ItemSettings>>();

        private Boolean IsRemovedBalance(UInt64 userID, Int32 Amount)
        {
            if (IQEconomic != null)
                return (Boolean)IQEconomic?.Call("API_IS_REMOVED_BALANCE", userID, Amount);
            else if (Economics != null)
                return GetBalance(userID) >= Amount;
            return false;
        }
        private const Boolean LanguageEn = false;
        public bool UserAmountCase(BasePlayer player,string CaseKey)
        {
            if (GetAmountCase(player,CaseKey) > 0) return true;
            else return false;
        }
        
                void API_GIVE_CASE(ulong userID, string CaseKey, int Amount) => Metods_Case_Give(userID, CaseKey, Amount);
        static Double CurrentTime => Facepunch.Math.Epoch.Current;

        
        
        public void TrackerTime(BasePlayer player)
        {
            foreach (var Case in config.CaseList.Where(Case => String.IsNullOrWhiteSpace(Case.Value.PermissionOpenCase) || permission.UserHasPermission(player.UserIDString, Case.Value.PermissionOpenCase)))
            {
                var User = TrackerTimePlayer[player.userID];
                if (!Case.Value.GiveCaseFromPlaying) continue;

                if (!User.ContainsKey(Case.Key))
                    User.Add(Case.Key, new TrackerTimeData { Time = (int)(Case.Value.PlayingNormalFromGive + CurrentTime), AmountTakes = 0 });
                else if (User[Case.Key].Time <= CurrentTime)
                {
                    if (User[Case.Key].AmountTakes >= Case.Value.GiveCount) return;
                    Metods_Case_Give(player.userID, Case.Key, 1);
                    User[Case.Key].Time = (int)(Case.Value.PlayingNormalFromGive + CurrentTime);
                    User[Case.Key].AmountTakes++;
                }

            }
        }
        public static string UI_MY_INVENTORY = "MAIN_UI_INVENTORY_USER";
        public string ImageGetItemsInventory(ulong userID,ItemSettings itemSettings)
        {
            string ReturnImage = itemSettings.CommandUse ? itemSettings.Command : $"{itemSettings.Shortname}_256px_case";
            return ReturnImage;
        }
        
                bool IsRank(ulong userID, string Key)
        {
            if (!IQRankSystem) return false;
            return (bool)IQRankSystem?.Call("API_GET_AVAILABILITY_RANK_USER", userID, Key);
        }
        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity == null || player == null) return;
            if (entity.GetComponent<StorageContainer>() == null) return;
            if (entity.OwnerID >= 7656000000) return;
            if (entity.skinID == 99909) return;

            Metods_Search_Case(player, entity.ShortPrefabName);
            entity.skinID = 99909;
        }
        void OnPlayerConnected(BasePlayer player)
        {
            RegisteredDataUser(player);
            CachedImage(player);
        }
        int API_GET_AMOUNT_CASE(BasePlayer player, string CaseKey) => GetAmountCase(player, CaseKey);
        public void SendImage(BasePlayer player, String imageName, UInt64 imageId = 0) => ImageLibrary?.Call("SendImage", player, imageName, imageId);

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();

        void Metods_Case_Give(ulong userID, string CaseKey, int Amount)
        {
            if (!DataPlayer[userID].ContainsKey(CaseKey))
                if (config.CaseList.ContainsKey(CaseKey))
                {
                    DataPlayer[userID].Add(CaseKey, Amount);
                    Puts(LanguageEn ? $"The player {userID} received a case - {CaseKey}" : $"Игрок {userID} получил кейс - {CaseKey}");
                }
                else PrintError(LanguageEn ? "No such case was found, check the key! If everything is correct, contact the developer Mercury#5212" : "#ERROR : Такого кейса не найдено,проверьте ключ! Если у вас все верно свяжитесь с разработчиком Mercury#5212");
            else
            {
                DataPlayer[userID][CaseKey] += Amount;
                Puts(LanguageEn ? $"The player {userID} received a case - {CaseKey}" : $"Игрок {userID} получил кейс - {CaseKey}");
                SendChat(GetLang("CHAT_CASE_DROP_ACCESS", userID.ToString(), config.CaseList[CaseKey].DisplayName), BasePlayer.FindByID(userID));
            }
        }
        public static string UI_WHY_CASE = "MAIN_UI_WHY_CASE";
        public static string UI_TAKE_ITEM = "MAIN_UI_TAKE_ITEM";
        public static string STOP_BLOCK_INFO = "STOP_BLOCK_INFOS";

        [JsonProperty(LanguageEn ? "Date of player cases" : "Дата кейсов игроков")]
        public Dictionary<ulong, Dictionary<string, int>> DataPlayer = new Dictionary<ulong, Dictionary<string, int>>();
        
        
        public IEnumerator Interface_Animation(BasePlayer player,string CaseKey)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_CASE_ANIMATION);

            int CountItems = config.CaseList[CaseKey].ItemSetting.Count >= 8 ? 8 : config.CaseList[CaseKey].ItemSetting.Count;
            List<int> PossibleAwards = new List<int>();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { FadeIn = 0.5f, Color = "0 0 0 0" }
            },  "INFO_PANEL", STOP_BLOCK_INFO);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { FadeIn = 0.5f, Color = "0 0 0 0" }
            },  UI_TAKE_ITEM);
		   		 		  						  	   		  		 			   		 		  		  		   			
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-40 + ((CountItems - 1) * -40)} -350", OffsetMax = $"{40 + ((CountItems - 1) * 40)} -265" },
                Image = { FadeIn = 0.5f, Color = HexToRustFormat("#444837C6"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, UI_TAKE_ITEM, UI_CASE_ANIMATION);


             container.Add(new CuiElement
            {
                Parent = UI_CASE_ANIMATION,
                Components =
                    {
                        new CuiRawImageComponent { Png = GetImage("RIGHT_ARROW"),Color = HexToRustFormat("#FFFFFF6A"), FadeIn = 1f},
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"0 0", OffsetMin = "-60 10", OffsetMax = "0 70"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = UI_CASE_ANIMATION,
                Components =
                    {
                        new CuiRawImageComponent { Png = GetImage("LEFT_ARROW"),FadeIn = 1.5f,Color = HexToRustFormat("#FFFFFF6A"), },
                        new CuiRectTransformComponent{ AnchorMin = "1 1", AnchorMax = $"1 1", OffsetMin = "0 -70", OffsetMax = "60 -10"},
                    }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",OffsetMin = $"{-40 + ((CountItems - 1) * -40)} 40", OffsetMax = $"{40 + ((CountItems - 1) * 40)} 70" },
                Text = { Text = lang.GetMessage("UI_CASE_ANIM_LABEL_TITLE",this,player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            },  UI_CASE_ANIMATION);
		   		 		  						  	   		  		 			   		 		  		  		   			
            CuiHelper.AddUi(player, container);

            RunEffect(player, "assets/bundled/prefabs/fx/notice/loot.start.fx.prefab");

            for(int i = 0; i < CountItems; i++)
            {
                RunEffect(player, "assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab");
                CuiElementContainer temp = new CuiElementContainer();

                int RandomIndex = UnityEngine.Random.Range(0, config.CaseList[CaseKey].ItemSetting.Count);
                var Items = config.CaseList[CaseKey].ItemSetting[RandomIndex];
                int VisualRandom = UnityEngine.Random.Range(Items.MinAmount, Items.MaxAmount);

                PossibleAwards.Add(RandomIndex);
		   		 		  						  	   		  		 			   		 		  		  		   			
                temp.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"{5 + (80 * i)} 5", OffsetMax = $"{75 + (80 * i)} 80" },
                    Image = { FadeIn = 0.15f, Color = HexToRustFormat("#E9D2C10E"), Material = "assets/content/ui/uibackgroundblur.mat" }
                }, UI_CASE_ANIMATION, $"ITEM_{i}");
		   		 		  						  	   		  		 			   		 		  		  		   			
                if (Items.IsBlueprint)
                {
                    temp.Add(new CuiElement
                    {
                        Parent = $"ITEM_{i}",
                        Components =
                        {
                            new CuiRawImageComponent { Png = GetImage("blueprintbase") },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                        }
                    });
                }
		   		 		  						  	   		  		 			   		 		  		  		   			
                temp.Add(new CuiElement
                {
                    Parent = $"ITEM_{i}",
                    Components =
                    {
                        new CuiRawImageComponent { Png = GetImage(ImageGetItems(CaseKey,RandomIndex)) },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"1 1"},
                    }
                });
		   		 		  						  	   		  		 			   		 		  		  		   			
                temp.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = $"{VisualRandom}", Font = "robotocondensed-bold.ttf", Align = TextAnchor.LowerCenter, FadeIn = 0.3f }
                }, $"ITEM_{i}");

                CuiHelper.AddUi(player, temp);

                yield return new WaitForSeconds(0.5f);
            }

            CuiElementContainer containerMore = new CuiElementContainer();
		   		 		  						  	   		  		 			   		 		  		  		   			
            containerMore.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { FadeIn = 0.5f, Color = HexToRustFormat("#00000024"), Material = "assets/content/ui/uibackgroundblur.mat" }
            },  UI_CASE_ANIMATION, "BLURING_PANEL");

            containerMore.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("UI_CASE_ANIM_LABEL",this,player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, $"BLURING_PANEL");

            CuiHelper.AddUi(player, containerMore);

            yield return new WaitForSeconds(0.5f);

            RunEffect(player, "assets/bundled/prefabs/fx/player/beartrap_clothing_rustle.prefab");
            ShowReward(player, CaseKey, PossibleAwards);
        }

                void Interface_Cases(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_MAIN_UI);

            container.Add(new CuiPanel
            {
                CursorEnabled = true,   
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat("#282A21C6"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, "Overlay", UI_MAIN_UI);

            
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.2020833 1" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat("#772424F0"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, UI_MAIN_UI, "INFO_PANEL");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.01546395 0.9351851", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("UI_TITLE_INFO",this,player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft, FadeIn = 0.3f }
            },  "INFO_PANEL", "TITLE_PANEL");
		   		 		  						  	   		  		 			   		 		  		  		   			
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.01546395 0.8611111", AnchorMax = "1 0.9462963" },
                Text = { Text = lang.GetMessage("UI_TITLE_INFO_DESCRIPTION", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperLeft, FadeIn = 0.3f }
            }, "INFO_PANEL", "TITLE_PANEL_DESCRIPTION");

            if (IQEconomic || Economics)
            {
                int Balance = GetBalance(player.userID);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.0876289 0.1546298", AnchorMax = "0.435567 0.2046296" },
                    Text = { Text = lang.GetMessage("UI_TITLE_BALANCE", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft, FadeIn = 0.3f }
                }, "INFO_PANEL", "TITLE_PANEL_BALANCE_TITLE");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5283506 0.1333335", AnchorMax = "0.7860826 0.225926" },
                    Text = { Text = $"{Balance}", FontSize = 30, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight, FadeIn = 0.3f }
                }, "INFO_PANEL", "TITLE_PANEL_BALANCE");
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.06701034 0.07870371", AnchorMax = $"0.9252577 0.1287039" },
                Button = { Command = "case inventory open", Color = HexToRustFormat("#4D3737C8"), Sprite =  "assets/content/ui/ui.background.tile.psd" },
                Text = { Text = lang.GetMessage("UI_BTN_INVENTORY", this, player.UserIDString), Color = HexToRustFormat("#E9D2C1FF"), Align = TextAnchor.MiddleCenter }
            }, "INFO_PANEL");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.06701034 0.01388889", AnchorMax = "0.9252577 0.06388889" },
                Button = { Close = UI_MAIN_UI, Color = HexToRustFormat("#4D3737C8"), Sprite = "assets/content/ui/ui.background.tile.psd" },
                Text = { Text = lang.GetMessage("UI_CLOSE", this, player.UserIDString), Color = HexToRustFormat("#CAAD9EFF"), Align = TextAnchor.MiddleCenter }
            },  "INFO_PANEL");
		   		 		  						  	   		  		 			   		 		  		  		   			
            
            
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.2015625 0", AnchorMax = "1 1" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat("#282A21C6"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, UI_MAIN_UI, "ELEMENT_PANEL");

            var CaseList = config.CaseList;
            int i = 0;
            int ItemCount = 0;
            float itemMinPosition = 219f;
            float itemWidth = 0.413646f - 0.196f; /// 
            float itemMargin = 0.439895f - 0.405f;
            int itemCount = CaseList.Count;
            float itemMinHeight = 0.65f;
            float itemHeight = 0.32f; /// 
            int ItemTarget = 4;

            if (itemCount > ItemTarget)
            {
                itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                itemCount -= ItemTarget;
            }
            else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
            foreach (var Case in CaseList)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{itemMinPosition} {itemMinHeight}", AnchorMax = $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}" },
                    Image = { FadeIn = 0.15f, Color = "0 0 0 0" }
                },  "ELEMENT_PANEL",$"CASE_{i}");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.8593802", AnchorMax = "1 1" }, 
                    Text = { Text = $"{Case.Value.DisplayName.ToUpper()}",Color = HexToRustFormat("#AAA39CFF"), FontSize = 24, Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperLeft, FadeIn = 0.3f }
                }, $"CASE_{i}");

                
                container.Add(new CuiElement
                {
                    Parent = $"CASE_{i}",
                    Components =
                    {
                        new CuiRawImageComponent { Png = GetImage(Case.Key) },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"1 1"},
                    }
                });
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = $"case take {Case.Key}", Color = "0 0 0 0" },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                },  $"CASE_{i}");

                ItemCount++;
                i++;
                itemMinPosition += (itemWidth + itemMargin);
                if (ItemCount % ItemTarget == 0)
                {
                    itemMinHeight -= (itemHeight + (itemMargin * 2f));
                    if (itemCount > ItemTarget)
                    {
                        itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                        itemCount -= ItemTarget;
                    }
                    else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                }
            }

            
            CuiHelper.AddUi(player, container);
        }
        private void OnServerInitialized()
        {
            AddAllImage();
            foreach (var p in BasePlayer.activePlayerList)
                OnPlayerConnected(p);

            ServerMgr.Instance.StartCoroutine(DownloadImages());

            timer.Every(300f, () =>
            {
                foreach (var p in BasePlayer.activePlayerList)
                    TrackerTime(p);
            });
        }

        public string ImageGetItems(string CaseKey, int ItemKey)
        {
            var Case = config.CaseList[CaseKey];
            var Item = Case.ItemSetting[ItemKey];
            string ReturnImage = Item.CommandUse ? Item.Command : $"{Item.Shortname}_256px_case";
            return ReturnImage;
        }
        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            sb.Clear();
            return sb.AppendFormat("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a).ToString();             
        }
        private Int32 GetBalance(UInt64 userID)
        {
            if (IQEconomic != null)
                return (Int32)IQEconomic?.Call("API_GET_BALANCE", userID);
            else if (Economics != null)
                return Convert.ToInt32((Double)Economics?.Call("Balance", userID));
            return 0;
        }
        void Unload()
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQCases/IQCasesUser", DataPlayer);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQCases/IQInventoryUser", InventoryPlayer);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQCases/TrackerTimePlayer", TrackerTimePlayer);

            ServerMgr.Instance.StopCoroutine(DownloadImages());
        }
        protected override void SaveConfig() => Config.WriteObject(config);
        
                private void Init()
        {
            ReadData();

            foreach (var Case in config.CaseList.Where(Case => !String.IsNullOrWhiteSpace(Case.Value.PermissionOpenCase) && !permission.PermissionExists(Case.Value.PermissionOpenCase, this)))
                permission.RegisterPermission(Case.Value.PermissionOpenCase, this);
        }
        void WriteData() => timer.Every(60f, () =>
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQCases/IQCasesUser", DataPlayer);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQCases/IQInventoryUser", InventoryPlayer);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQCases/TrackerTimePlayer", TrackerTimePlayer);
        });
        
    }
}
