using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("WarriorShop", "Feverr", "1.0.0")]
    [Description("A shop system for Rust where players can purchase items with points and coins.")]
    public class WarriorShop : CovalencePlugin
    {
        private WarriorPointsManager pointsManager;
        private const string CommandShop = "shop";
        private const string ConfigFilePath = "carbon/configs/WarriorShop.json";
        private const string ItemDataFilePath = "carbon/data/WarriorShopItemData.json";
        private const string PointsDataFilePath = "carbon/data/WarriorPointsData.json";

        private Dictionary<string, int> playerAmounts = new Dictionary<string, int>();
        private Dictionary<string, int> playerPages = new Dictionary<string, int>();
        private Dictionary<string, string> playerSelectedNavButton = new Dictionary<string, string>();
        private Dictionary<string, string> playerSelectedGridButton = new Dictionary<string, string>();
        private Dictionary<string, List<ItemData>> categorizedItems = new Dictionary<string, List<ItemData>>();
        private Dictionary<string, ItemData> itemsDatabase = new Dictionary<string, ItemData>();
        private Dictionary<string, PlayerPoints> playerPoints = new Dictionary<string, PlayerPoints>();
        private Dictionary<string, List<ItemData>> playerSearchResults = new Dictionary<string, List<ItemData>>();

        private void Init()
        {
            AddCovalenceCommand(CommandShop, nameof(ShopCommand));
            AddCovalenceCommand("shop.showcategory", nameof(ShowCategoryCommand));
            AddCovalenceCommand("shop.warcoin", nameof(ShowWarCoinCommand));
            AddCovalenceCommand("shop.close", nameof(ShopCloseCommand));
            AddCovalenceCommand("shop.incrementpage", nameof(IncrementPageCommand));
            AddCovalenceCommand("shop.decrementpage", nameof(DecrementPageCommand));
            AddCovalenceCommand("shop.selectitem", nameof(SelectItemCommand));
            AddCovalenceCommand("shop.setquantity", nameof(SetQuantityCommand));
            AddCovalenceCommand("shop.check", nameof(CheckItemCommand));
            AddCovalenceCommand("shop.updateitem", nameof(UpdateItemCommand));
            AddCovalenceCommand("shop.searchinput", nameof(SearchInputCommand));

            // Register permissions
            permission.RegisterPermission("warriorshop.use", this);
            permission.RegisterPermission("warriorshop.admin", this);

            // Get the WarriorPointsManager plugin
            pointsManager = Interface.Oxide.RootPluginManager.GetPlugin("WarriorPointsManager") as WarriorPointsManager;

            if (pointsManager == null)
            {
                Interface.Oxide.LogError("WarriorPointsManager plugin not found!");
                return;
            }

            if (!CheckAndLoadItemData())
            {
                Interface.Oxide.LogError("Item data file not found or invalid format at " + ItemDataFilePath);
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
        }

        // Rest of the code remains unchanged...
    }
}

        private void LoadPointsData()
        {
            if (!File.Exists(PointsDataFilePath))
            {
                Interface.Oxide.LogError("Points data file not found at " + PointsDataFilePath);
                return;
            }

            var pointsDataJson = File.ReadAllText(PointsDataFilePath);
            try
            {
                playerPoints = JsonConvert.DeserializeObject<Dictionary<string, PlayerPoints>>(pointsDataJson);
                Interface.Oxide.LogInfo($"Loaded points data for {playerPoints.Count} players.");
            }
            catch (JsonException ex)
            {
                Interface.Oxide.LogError("Error deserializing points data: " + ex.Message);
            }
        }

        private void SavePointsData()
        {
            var pointsDataJson = JsonConvert.SerializeObject(playerPoints, Formatting.Indented);
            File.WriteAllText(PointsDataFilePath, pointsDataJson);
        }

        private bool CheckAndLoadItemData()
        {
            if (!File.Exists(ItemDataFilePath))
            {
                Interface.Oxide.LogError("Item data file not found at " + ItemDataFilePath);
                return false;
            }

            var itemDataJson = File.ReadAllText(ItemDataFilePath);
            try
            {
                var items = JsonConvert.DeserializeObject<List<ItemData>>(itemDataJson);
                if (items == null || items.Count == 0)
                {
                    Interface.Oxide.LogError("Item data file is empty or invalid format.");
                    return false;
                }

                foreach (var item in items)
                {
                    string itemTypeLower = item.Type.ToLower();
                    if (!categorizedItems.ContainsKey(itemTypeLower))
                    {
                        categorizedItems[itemTypeLower] = new List<ItemData>();
                    }
                    categorizedItems[itemTypeLower].Add(item);
                    itemsDatabase[item.Name.ToLower()] = item;
                }

                Interface.Oxide.LogInfo($"Loaded {items.Count} items into {categorizedItems.Count} categories.");

                foreach (var category in categorizedItems)
                {
                    Interface.Oxide.LogInfo($"Category: {category.Key}, Items: {category.Value.Count}");
                }

                return true;
            }
            catch (JsonException ex)
            {
                Interface.Oxide.LogError("Error deserializing item data: " + ex.Message);
                return false;
            }
        }

        private class ItemData
        {
            public string Name { get; set; }
            public string Shortname { get; set; }
            public string ItemID { get; set; }
            public string ImageURL { get; set; }
            public int StackSize { get; set; }
            public int Price { get; set; }
            public string Type { get; set; }
            public int Priority { get; set; }
        }

        private class PlayerPoints
        {
            public string Name { get; set; }
            public int Points { get; set; }
            public int Coins { get; set; } // New currency property
        }

        // commands/permissions
        [Command(CommandShop)]
        private void ShopCommand(IPlayer player, string command, string[] args)
        {
            player.Reply("Opening shop...");
            OpenShopGUI(player);
        }

        [Command("shop.searchinput")]
        private void SearchInputCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                player.Reply("Please enter a search term.");
                return;
            }

            var searchTerm = string.Join(" ", args).ToLower();
            var matchingItems = itemsDatabase.Values
                .Where(item => item.Name.ToLower().Contains(searchTerm) || item.Shortname.ToLower().Contains(searchTerm))
                .ToList();

            if (matchingItems.Count == 0)
            {
                player.Reply($"No items found for search term '{searchTerm}'.");
            }
            else
            {
                playerSearchResults[player.Id] = matchingItems;
                playerPages[player.Id] = 1; // Reset to the first page of search results
                UpdateShopContent(player, matchingItems);
            }
        }

        [Command("shop.showcategory")]
        private async void ShowCategoryCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length > 0)
            {
                var category = args[0].ToLower();
                playerSelectedNavButton[player.Id] = category;
                playerSelectedGridButton[player.Id] = null;
                playerPages[player.Id] = 1; // Reset page to 1 when a new category is selected

                Interface.Oxide.LogInfo($"Player {player.Id} selected category {category}");

                // Verify the selected category exists
                if (categorizedItems.ContainsKey(category))
                {
                    Interface.Oxide.LogInfo($"Category {category} exists with {categorizedItems[category].Count} items");
                }
                else
                {
                    Interface.Oxide.LogInfo($"Category {category} does not exist");
                    player.Reply($"Category '{category}' does not exist.");
                    return;
                }

                await LoadPageDataAsync(player);
                UpdateShopContent(player);
            }
        }

        [Command("shop.warcoin")]
        private async void ShowWarCoinCommand(IPlayer player, string command, string[] args)
        {
            string warCoinCategory = "wc"; // Ensure this matches the type used in the item data
            playerSelectedNavButton[player.Id] = warCoinCategory;
            playerSelectedGridButton[player.Id] = null;
            playerPages[player.Id] = 1; // Reset page to 1 when WarCoin category is selected
            Interface.Oxide.LogInfo($"Player {player.Id} selected WarCoin category");
            await LoadPageDataAsync(player);
            UpdateShopContent(player);
        }

        [Command("shop.close")]
        private void ShopCloseCommand(IPlayer player, string command, string[] args)
        {
            BasePlayer basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;

            CuiHelper.DestroyUi(basePlayer, "MainPanel");

            basePlayer.SendConsoleCommand("input.cursor.visible 0");
            basePlayer.SendConsoleCommand("input.cursor.enable false");
        }

        [Command("shop.incrementpage")]
        private async void IncrementPageCommand(IPlayer player, string command, string[] args)
        {
            if (playerPages.ContainsKey(player.Id))
            {
                var selectedCategory = playerSelectedNavButton[player.Id];
                if (categorizedItems.ContainsKey(selectedCategory))
                {
                    var items = categorizedItems[selectedCategory];
                    int nextPageStartIndex = playerPages[player.Id] * 21;
                    if (nextPageStartIndex < items.Count)
                    {
                        playerPages[player.Id]++;
                        playerSelectedGridButton[player.Id] = null;
                        Interface.Oxide.LogInfo($"Player {player.Id} incremented page to {playerPages[player.Id]}");
                        await LoadPageDataAsync(player);
                        UpdateShopContent(player);
                    }
                }
            }
        }

        [Command("shop.decrementpage")]
        private async void DecrementPageCommand(IPlayer player, string command, string[] args)
        {
            if (playerPages.ContainsKey(player.Id) && playerPages[player.Id] > 1)
            {
                playerPages[player.Id]--;
                playerSelectedGridButton[player.Id] = null;
                Interface.Oxide.LogInfo($"Player {player.Id} decremented page to {playerPages[player.Id]}");
                await LoadPageDataAsync(player);
                UpdateShopContent(player);
            }
        }

        [Command("shop.selectitem")]
        private void SelectItemCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length > 0)
            {
                var selectedItem = args[0];

                BasePlayer basePlayer = player.Object as BasePlayer;
                if (basePlayer == null) return;

                // Remove the previous selection border
                CuiHelper.DestroyUi(basePlayer, "SelectedItemBorderTop");
                CuiHelper.DestroyUi(basePlayer, "SelectedItemBorderBottom");
                CuiHelper.DestroyUi(basePlayer, "SelectedItemBorderLeft");
                CuiHelper.DestroyUi(basePlayer, "SelectedItemBorderRight");

                playerSelectedGridButton[player.Id] = selectedItem;
                Interface.Oxide.LogInfo($"Player {player.Id} selected item {selectedItem}");

                // Add a green border around the selected item
                var elements = new CuiElementContainer();

                List<ItemData> items;
                if (playerSearchResults.TryGetValue(player.Id, out var searchResults) && searchResults != null)
                {
                    items = searchResults;
                }
                else
                {
                    var selectedCategory = playerSelectedNavButton[player.Id];
                    items = categorizedItems[selectedCategory];
                }

                int startIndex = (playerPages[player.Id] - 1) * 21;

                for (int row = 0; row < 3; row++)
                {
                    for (int col = 0; col < 7; col++)
                    {
                        int itemIndex = startIndex + row * 7 + col;
                        if (itemIndex >= items.Count)
                        {
                            break;
                        }

                        var item = items[itemIndex];
                        if (item.Shortname == selectedItem)
                        {
                            // Adjusted values for better placement
                            float xMin = 0.223f + col * 0.106f;
                            float yMax = 0.761f - row * 0.2f;
                            float xMax = xMin + 0.11f;
                            float yMin = yMax - 0.18f;

                            // Top border
                            elements.Add(new CuiPanel
                            {
                                RectTransform = { AnchorMin = $"{xMin} {yMax - 0.005f}", AnchorMax = $"{xMax} {yMax}" },
                                Image = { Color = "0 1 0 1" }
                            }, "MainPanel", "SelectedItemBorderTop");

                            // Bottom border
                            elements.Add(new CuiPanel
                            {
                                RectTransform = { AnchorMin = $"{xMin} {yMin}", AnchorMax = $"{xMax} {yMin + 0.005f}" },
                                Image = { Color = "0 1 0 1" }
                            }, "MainPanel", "SelectedItemBorderBottom");

                            // Left border
                            elements.Add(new CuiPanel
                            {
                                RectTransform = { AnchorMin = $"{xMin} {yMin}", AnchorMax = $"{xMin + 0.003f} {yMax}" },
                                Image = { Color = "0 1 0 1" }
                            }, "MainPanel", "SelectedItemBorderLeft");

                            // Right border
                            elements.Add(new CuiPanel
                            {
                                RectTransform = { AnchorMin = $"{xMax - 0.003f} {yMin}", AnchorMax = $"{xMax} {yMax}" },
                                Image = { Color = "0 1 0 1" }
                            }, "MainPanel", "SelectedItemBorderRight");

                            break;
                        }
                    }
                }

                CuiHelper.AddUi(basePlayer, elements);
            }
        }

        [Command("shop.setquantity")]
        private void SetQuantityCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length > 0)
            {
                var quantityText = args[0];

                var sanitizedQuantityText = quantityText.Replace(",", "").Replace("x", "");

                if (!int.TryParse(sanitizedQuantityText, out var quantity))
                {
                    player.Reply("Invalid quantity.");
                    return;
                }

                if (!playerSelectedGridButton.TryGetValue(player.Id, out var selectedItem) || string.IsNullOrEmpty(selectedItem))
                {
                    player.Reply("No item selected.");
                    return;
                }

                BasePlayer basePlayer = player.Object as BasePlayer;
                if (basePlayer == null) return;

                if (!playerSelectedNavButton.TryGetValue(player.Id, out var selectedCategory))
                {
                    player.Reply("No category selected.");
                    return;
                }

                if (!categorizedItems.TryGetValue(selectedCategory, out var items))
                {
                    player.Reply("Selected category not found.");
                    return;
                }

                var item = items.FirstOrDefault(i => i.Shortname == selectedItem);

                if (item == null)
                {
                    player.Reply("Selected item not found in the database.");
                    return;
                }

                // Check if player has enough points or coins based on item type
                var playerData = pointsManager.GetPlayerPoints(basePlayer.UserIDString);
                if (playerData == null)
                {
                    player.Reply("Points data not found for your account.");
                    return;
                }

                int totalPrice = item.Price * quantity;
                if (item.Type.ToLower() == "wc")
                {
                    if (playerData.Coins < totalPrice)
                    {
                        player.Reply("You do not have enough coins to make this purchase.");
                        return;
                    }

                    // Subtract coins
                    pointsManager.AddCoins(basePlayer, -totalPrice);
                    player.Reply($"Bought {quantity} x {item.Name} for {totalPrice} Coins. Your remaining Coins: {playerData.Coins - totalPrice}");
                }
                else
                {
                    if (playerData.Points < totalPrice)
                    {
                        player.Reply("You do not have enough points to make this purchase.");
                        return;
                    }

                    // Subtract points
                    pointsManager.AddPoints(basePlayer, -totalPrice);
                    player.Reply($"Bought {quantity} x {item.Name} for {totalPrice} Points. Your remaining Points: {playerData.Points - totalPrice}");
                }

                GiveItem(basePlayer, item, quantity);
            }
        }

        [Command("shop.check")]
        private void CheckItemCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                player.Reply("Usage: /shop.check <item name or shortname>");
                return;
            }

            var input = args[0].ToLower();
            ItemData exactMatchItem = null;
            var processedItems = new HashSet<string>();

            // Check for exact match
            foreach (var item in itemsDatabase.Values)
            {
                if (item.Name.ToLower() == input || item.Shortname.ToLower() == input)
                {
                    exactMatchItem = item;
                    processedItems.Add(item.Name.ToLower());  // Add the item to processed items
                    break;
                }
            }

            if (exactMatchItem != null)
            {
                // Display exact match item details
                player.Reply($"Item: {exactMatchItem.Name}");
                player.Reply($"Shortname: {exactMatchItem.Shortname}");
                player.Reply($"ItemID: {exactMatchItem.ItemID}");
                player.Reply($"Price: {exactMatchItem.Price}");
                player.Reply($"Type: {exactMatchItem.Type}");
                player.Reply($"Priority: {exactMatchItem.Priority}");
            }
            else
            {
                // Check for partial matches
                var matchingItems = itemsDatabase.Values
                    .Where(item =>
                        (item.Name.ToLower().Contains(input) || item.Shortname.ToLower().Contains(input))
                        && !processedItems.Contains(item.Name.ToLower()))  // Ensure item is not already processed
                    .ToList();

                if (matchingItems.Count > 0)
                {
                    player.Reply("Multiple items match your input. Did you mean:");
                    foreach (var item in matchingItems)
                    {
                        player.Reply($"- {item.Name} (Shortname: {item.Shortname})");
                        processedItems.Add(item.Name.ToLower());  // Mark item as processed
                    }
                }
                else
                {
                    player.Reply("No items match your input.");
                }
            }
        }

        [Command("shop.updateitem")]
        private void UpdateItemCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length < 3)
            {
                player.Reply("Usage: /shop.updateitem <itemName> <field> <newValue>");
                return;
            }

            var itemName = args[0].ToLower();
            var field = args[1].ToLower();
            var newValue = args[2];

            if (!itemsDatabase.TryGetValue(itemName, out var item))
            {
                player.Reply($"Item '{itemName}' not found.");
                return;
            }

            switch (field)
            {
                case "shortname":
                    item.Shortname = newValue;
                    break;
                case "itemid":
                    item.ItemID = newValue;
                    break;
                case "imageurl":
                    item.ImageURL = newValue;
                    break;
                case "stacksize":
                    if (int.TryParse(newValue, out int stackSize))
                        item.StackSize = stackSize;
                    else
                        player.Reply("Invalid stack size.");
                    break;
                case "price":
                    if (int.TryParse(newValue, out int price))
                        item.Price = price;
                    else
                        player.Reply("Invalid price.");
                    break;
                case "type":
                    item.Type = newValue;
                    break;
                case "priority":
                    if (int.TryParse(newValue, out int priority))
                        item.Priority = priority;
                    else
                        player.Reply("Invalid priority.");
                    break;
                default:
                    player.Reply("Unknown field.");
                    return;
            }

            // Update the item in the categorizedItems dictionary
            string oldCategory = item.Type.ToLower();
            if (categorizedItems.ContainsKey(oldCategory))
            {
                categorizedItems[oldCategory].RemoveAll(i => i.Name.ToLower() == itemName);
            }

            string newCategory = item.Type.ToLower();
            if (!categorizedItems.ContainsKey(newCategory))
            {
                categorizedItems[newCategory] = new List<ItemData>();
            }
            categorizedItems[newCategory].Add(item);
            categorizedItems[newCategory] = categorizedItems[newCategory].OrderBy(i => i.Priority).ThenBy(i => i.Name).ToList();

            player.Reply($"Item '{itemName}' updated successfully.");
            SaveItemData();

            // Refresh the shop content to reflect the new item positions
            ClearAllCachedData();
        }

        private void ClearAllCachedData()
        {
            foreach (var player in players.Connected)
            {
                BasePlayer basePlayer = player.Object as BasePlayer;
                if (basePlayer == null) continue;

                // Destroy existing UI elements
                CuiHelper.DestroyUi(basePlayer, "MainPanel");

                // Clear cached selections and pages
                playerPages.Remove(player.Id);
                playerSelectedNavButton.Remove(player.Id);
                playerSelectedGridButton.Remove(player.Id);

                // Optionally, you can notify the player that the shop has been updated and needs to be reopened
                Server.Broadcast("The shop has been updated.");
            }
        }

        private void SortItems()
        {
            foreach (var category in categorizedItems.Keys.ToList())
            {
                categorizedItems[category] = categorizedItems[category]
                    .OrderBy(item => item.Priority)
                    .ThenBy(item => item.Name)
                    .ToList();
            }
        }

        private void SaveItemData()
        {
            var json = JsonConvert.SerializeObject(itemsDatabase.Values.ToList(), Formatting.Indented);
            File.WriteAllText(ItemDataFilePath, json);
        }

        private void GiveItem(BasePlayer player, ItemData item, int quantity)
        {
            int itemId;
            if (!int.TryParse(item.ItemID, out itemId))
            {
                player.ChatMessage("Invalid item ID.");
                return;
            }
            player.inventory.GiveItem(ItemManager.CreateByItemID(itemId, quantity));
        }

        private async Task LoadPageDataAsync(IPlayer player)
        {
            await Task.Run(() =>
            {
                // Simulate data loading (e.g., reading from a database or file)
                System.Threading.Thread.Sleep(100); // Simulate delay
            });
        }

        private void OpenShopGUI(IPlayer player)
        {
            BasePlayer basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;

            if (!playerPages.ContainsKey(player.Id))
            {
                playerPages[player.Id] = 1;
            }

            var elements = CreateMainUI(player);
            CuiHelper.AddUi(basePlayer, elements);

            // Enable the cursor for player interaction with UI
            basePlayer.SendConsoleCommand("input.cursor.visible 1");
            basePlayer.SendConsoleCommand("input.cursor.enable true");

            // Automatically select the "Weapons" category if no category is already selected
            if (!playerSelectedNavButton.ContainsKey(player.Id) || string.IsNullOrEmpty(playerSelectedNavButton[player.Id]))
            {
                playerSelectedNavButton[player.Id] = "weapons";
                ShowCategoryCommand(player, "shop.showcategory", new[] { "weapons" });
            }

            UpdateShopContent(player);
        }

        private CuiElementContainer CreateMainUI(IPlayer player)
        {
            var elements = new CuiElementContainer();
            var playerId = player.Id;

            Puts("Creating main UI elements");

            var mainPanel = new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0.1 0.1 0.1 0.9" },
                CursorEnabled = true
            };
            var mainPanelName = elements.Add(mainPanel, "Overlay", "MainPanel");

            var headerBackground = new CuiPanel
            {
                RectTransform = { AnchorMin = "0.02 0.86", AnchorMax = "0.98 0.96" },
                Image = { Color = "0.8 0.4 0.2 0.6" }
            };
            elements.Add(headerBackground, mainPanelName);

            var header = new CuiLabel
            {
                RectTransform = { AnchorMin = "0.02 0.86", AnchorMax = "0.98 0.96" },
                Text = { Text = "Warrior Shop", FontSize = 40, Align = TextAnchor.MiddleCenter }
            };
            elements.Add(header, mainPanelName);

            var navigationPanel = new CuiPanel
            {
                RectTransform = { AnchorMin = "0.02 0.01", AnchorMax = "0.22 0.86" },
                Image = { Color = "0.2 0.2 0.2 0.7" }
            };
            var navigationPanelName = elements.Add(navigationPanel, mainPanelName);

            var warCoinButtonColor = playerSelectedNavButton.ContainsKey(playerId) && playerSelectedNavButton[playerId] == "warcoin" ? "0.2 0.8 0.2 1.0" : "0.5 0.5 0.5 1.0";
            var warCoinButton = new CuiButton
            {
                RectTransform = { AnchorMin = "0.1 0.855", AnchorMax = "0.9 0.915" }, // Adjusted position
                Button = { Color = warCoinButtonColor, Command = "shop.warcoin" },
                Text = { Text = "<<<WarCoin>>>", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1.0 0.84 0.0 1.0" }
            };
            elements.Add(warCoinButton, navigationPanelName);

            // Add the search input field and label directly here
            var anchorMinX = 0.1f;
            var anchorMinY = 0.925f;
            var anchorMaxX = 0.9f;
            var anchorMaxY = 0.975f;

            Puts("Adding search input field");

            // Add a label above the input field
            var searchLabel = new CuiLabel
            {
                RectTransform = { AnchorMin = $"{anchorMinX} {anchorMaxY}", AnchorMax = $"{anchorMaxX} {anchorMaxY + 0.025f}" },
                Text = { Text = "Search:", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            };
            elements.Add(searchLabel, navigationPanelName);

            // Add a background panel for the input field with light gray color
            var backgroundPanel = new CuiPanel
            {
                RectTransform = { AnchorMin = $"{anchorMinX} {anchorMinY}", AnchorMax = $"{anchorMaxX} {anchorMaxY}" },
                Image = { Color = "0.83 0.83 0.83 0.6" } // Light gray background color with 100% opacity
            };
            elements.Add(backgroundPanel, navigationPanelName);

            // Add the input field
            var inputField = new CuiElement
            {
                Parent = navigationPanelName,
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Command = "shop.searchinput",
                        FontSize = 18,
                        Align = TextAnchor.MiddleLeft,
                        Text = ""
                    },
                    new CuiRectTransformComponent { AnchorMin = $"{anchorMinX} {anchorMinY}", AnchorMax = $"{anchorMaxX} {anchorMaxY}" },
                    new CuiNeedsCursorComponent(),
                    new CuiNeedsKeyboardComponent()
                }
            };
            elements.Add(inputField);

            var kitsButton = new CuiButton
            {
                RectTransform = { AnchorMin = "0.1 0.01", AnchorMax = "0.9 0.065" }, // Adjusted position
                Button = { Color = "0.5 0.5 0.5 1", Command = "shop.kits" }, // Updated command
                Text = { Text = "Kits", FontSize = 18, Align = TextAnchor.MiddleCenter }
            };
            elements.Add(kitsButton, navigationPanelName);

            string[] buttonLabels = { "Weapons", "Ammo", "Attire", "Tools", "Medical", "Food", "Construction", "Electrical", "Traps", "Components", "Misc", "Fun", "Resources" };
            float verticalPaddingFactor = 0.06f; // Adjust this value to change the vertical padding
            for (int i = 0; i < buttonLabels.Length; i++)
            {
                var button = new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = $"0.1 {0.795 - (i * verticalPaddingFactor)}", // Adjusted position
                        AnchorMax = $"0.9 {0.845 - (i * verticalPaddingFactor)}"  // Adjusted position
                    },
                    Button = { Color = "0.5 0.6 0.5 1.0", Command = $"shop.showcategory {buttonLabels[i].ToLower()}" },
                    Text = { Text = buttonLabels[i], FontSize = 18, Color = "1 1 1 .9", Align = TextAnchor.MiddleCenter }
                };
                elements.Add(button, navigationPanelName);
            }

            var shopPanel = new CuiPanel
            {
                RectTransform = { AnchorMin = "0.22 0.06", AnchorMax = "0.98 0.86" },
                Image = { Color = "0.3 0.3 0.3 0.3" }
            };
            elements.Add(shopPanel, mainPanelName);

            return elements;
        }

        private void UpdateShopContent(IPlayer player, List<ItemData> searchResults = null)
        {
            BasePlayer basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;

            CuiHelper.DestroyUi(basePlayer, "ShopContent");
            CuiHelper.DestroyUi(basePlayer, "PageHeader");
            CuiHelper.DestroyUi(basePlayer, "SelectedItemBorderTop");
            CuiHelper.DestroyUi(basePlayer, "SelectedItemBorderBottom");
            CuiHelper.DestroyUi(basePlayer, "SelectedItemBorderLeft");
            CuiHelper.DestroyUi(basePlayer, "SelectedItemBorderRight");

            var playerId = player.Id;
            var elements = new CuiElementContainer();
            var contentPanel = new CuiPanel
            {
                RectTransform = { AnchorMin = "0.22 0.06", AnchorMax = "0.98 0.86" },
                Image = { Color = "0 0 0 0" }
            };
            var contentPanelName = elements.Add(contentPanel, "MainPanel", "ShopContent");

            var items = searchResults ?? categorizedItems[playerSelectedNavButton[player.Id]].OrderBy(item => item.Priority).ToList();
            int startIndex = (playerPages[player.Id] - 1) * 21;

            float startX = 0.02f;
            float startY = 0.70f;
            float buttonWidth = 0.11f;
            float buttonHeight = 0.15f;
            float horizontalPadding = 0.03f;
            float verticalPadding = 0.1f;

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    int itemIndex = startIndex + row * 7 + col;
                    if (itemIndex >= items.Count)
                    {
                        break;
                    }

                    var item = items[itemIndex];

                    var itemImage = new CuiElement
                    {
                        Parent = contentPanelName,
                        Components =
                        {
                            new CuiRawImageComponent { Url = item.ImageURL },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{startX + (col * (buttonWidth + horizontalPadding))} " +
                                            $"{startY - (row * (buttonHeight + verticalPadding))}",
                                AnchorMax = $"{startX + (col * (buttonWidth + horizontalPadding)) + buttonWidth} " +
                                            $"{startY - (row * (buttonHeight + verticalPadding)) + buttonHeight}"
                            }
                        }
                    };
                    elements.Add(itemImage);

                    var itemButton = new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = $"{startX + (col * (buttonWidth + horizontalPadding))} {startY - (row * (buttonHeight + verticalPadding))}",
                            AnchorMax = $"{startX + (col * (buttonWidth + horizontalPadding)) + buttonWidth} {startY - (row * (buttonHeight + verticalPadding)) + buttonHeight}"
                        },
                        Button = { Color = "0 0 0 0", Command = $"shop.selectitem {item.Shortname}" },
                        Text = { Text = "", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    };
                    elements.Add(itemButton, contentPanelName);

                    // Red background panel for the price label
                    var priceLabelBackground = new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = $"{startX + (col * (buttonWidth + horizontalPadding))} " +
                                        $"{startY - (row * (buttonHeight + verticalPadding)) - 0.04f}",
                            AnchorMax = $"{startX + (col * (buttonWidth + horizontalPadding)) + buttonWidth} " +
                                        $"{startY - (row * (buttonHeight + verticalPadding)) - 0.04f + 0.03f}"
                        },
                        Image = { Color = "0.8 0.4 0.2 0.6" }
                    };
                    elements.Add(priceLabelBackground, contentPanelName);

                    // Determine currency type and label
                    var currencyLabel = item.Type.ToLower() == "wc" ? "Coins" : "WP";
                    var priceLabel = new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = $"{startX + (col * (buttonWidth + horizontalPadding))} " +
                                        $"{startY - (row * (buttonHeight + verticalPadding)) - 0.04f}",
                            AnchorMax = $"{startX + (col * (buttonWidth + horizontalPadding)) + buttonWidth} " +
                                        $"{startY - (row * (buttonHeight + verticalPadding)) - 0.04f + 0.03f}"
                        },
                        Text = { Text = $"Price: {item.Price} {currencyLabel}", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" } // White color text
                    };
                    elements.Add(priceLabel, contentPanelName);
                }
            }

            var pageHeaderLabel = new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.79", AnchorMax = ".57 0.87" },
                Text = { Text = "Page:", FontSize = 30, Color = "0.5 1.0 0.5 1.0", Align = TextAnchor.MiddleCenter }
            };
            elements.Add(pageHeaderLabel, "MainPanel", "PageHeaderLabel");

            var pageHeader = new CuiLabel
            {
                RectTransform = { AnchorMin = "0.46 0.79", AnchorMax = ".7 0.87" },
                Text = { Text = $"{playerPages[player.Id]}", FontSize = 30, Color = "1.0 1.0 1.0 1.0", Align = TextAnchor.MiddleCenter }
            };
            elements.Add(pageHeader, "MainPanel", "PageHeader");

            CuiHelper.AddUi(basePlayer, elements);
            AddTopLayerButtons(basePlayer);
        }

        private void AddTopLayerButtons(BasePlayer basePlayer)
        {
            var elements = new CuiElementContainer();

            var closeButton = new CuiButton
            {
                RectTransform = { AnchorMin = "0.85 0.02", AnchorMax = "0.95 0.07" },
                Button = { Color = "0.8 0.4 0.2 1", Command = "shop.close", Close = "MainPanel" },
                Text = { Text = "Close Shop", FontSize = 18, Align = TextAnchor.MiddleCenter }
            };
            elements.Add(closeButton, "MainPanel");

            var pageDecrementButton = new CuiButton
            {
                RectTransform = { AnchorMin = "0.23 0.81", AnchorMax = "0.38 .85" },
                Button = { Color = "0.5 0.5 0.5 1.0", Command = "shop.decrementpage" },
                Text = { Text = "<", FontSize = 20, Align = TextAnchor.MiddleCenter }
            };
            elements.Add(pageDecrementButton, "MainPanel");

            var pageIncrementButton = new CuiButton
            {
                RectTransform = { AnchorMin = "0.83 0.81", AnchorMax = ".98 0.85" },
                Button = { Color = "0.5 0.5 0.5 1.0", Command = "shop.incrementpage" },
                Text = { Text = ">", FontSize = 20, Align = TextAnchor.MiddleCenter }
            };
            elements.Add(pageIncrementButton, "MainPanel");

            AddQuantityButtons(elements, "MainPanel");

            CuiHelper.AddUi(basePlayer, elements);
        }

        private void AddQuantityButtons(CuiElementContainer elements, string parent)
        {
            string[] qtyLabels = { "x1", "x5", "x10", "x100", "x1,000" };
            float startX = 0.25f;
            float startY = 0.02f;
            float buttonWidth = 0.1f;
            float buttonHeight = 0.05f;
            float padding = 0.01f;

            for (int i = 0; i < qtyLabels.Length; i++)
            {
                var button = new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = $"{startX + (i * (buttonWidth + padding))} {startY}",
                        AnchorMax = $"{startX + (i * (buttonWidth + padding)) + buttonWidth} {startY + buttonHeight}"
                    },
                    Button = { Color = "0.2 0.8 0.8 0.8", Command = $"shop.setquantity {qtyLabels[i]}" },
                    Text = { Text = qtyLabels[i], FontSize = 20, Align = TextAnchor.MiddleCenter }
                };
                elements.Add(button, parent);
            }
        }
    }
}