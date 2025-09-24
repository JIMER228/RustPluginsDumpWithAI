using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("SimpleScrapShop", "djimbou", "1.2.5")]
    [Description("GUI-based scrap shop with categories and buttons.")]
    public class SimpleScrapShop : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;

        private const int ScrapItemId = -932201673;
        private const string MainUI = "ShopUI.Main";
        private const string CategoryUI = "ShopUI.Category";

        private Dictionary<string, List<ShopItem>> shopCategories = new Dictionary<string, List<ShopItem>>
        {
            ["Weapons"] = new List<ShopItem>
            {
                new ShopItem("Assault Rifle", "rifle.ak", 5000),
                new ShopItem("Custom SMG", "smg.2", 3000),
                new ShopItem("Python Revolver", "pistol.python", 2000),
                new ShopItem("Bolt Action Rifle", "rifle.bolt", 4500)
            },
            ["Ammo"] = new List<ShopItem>
            {
                new ShopItem("5.56 Rifle Ammo", "ammo.rifle", 100, 20),
                new ShopItem("Pistol Bullet", "ammo.pistol", 80, 20),
                new ShopItem("12 Gauge Buckshot", "ammo.shotgun", 120, 10)
            },
            ["Food"] = new List<ShopItem>
            {
                new ShopItem("Can of Beans", "can.beans", 150),
                new ShopItem("Cooked Chicken", "chicken.cooked", 70),
                new ShopItem("Pumpkin", "pumpkin", 50)
            },
            ["Building"] = new List<ShopItem>
            {
                new ShopItem("Wood", "wood", 10, 100),
                new ShopItem("Stone", "stones", 20, 100),
                new ShopItem("Metal Fragments", "metal.fragments", 50, 50),
                new ShopItem("Sheet Metal Door", "door.hinged.metal", 1000)
            }
        };

        [ChatCommand("shop")]
        private void CmdShop(BasePlayer player, string command, string[] args)
        {
            OpenMainMenu(player);
        }

        private void OpenMainMenu(BasePlayer player)
        {
            DestroyUI(player);

            CuiElementContainer container = new CuiElementContainer();
            string panel = container.Add(new CuiPanel
            {
                Image = { Color = "0.1 0.1 0.1 0.95" },
                RectTransform = { AnchorMin = "0.3 0.2", AnchorMax = "0.7 0.8" },
                CursorEnabled = true
            }, "Overlay", MainUI);

            // Title
            container.Add(new CuiLabel
            {
                Text = { Text = "SCRAP SHOP", FontSize = 20, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0.85", AnchorMax = "1 0.95" }
            }, panel);

            // Categories
            float buttonHeight = 0.75f;
            foreach (var category in shopCategories.Keys)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = "0.3 0.5 0.8 1", Command = $"cmd.showcategory {category}" },
                    RectTransform = { AnchorMin = $"0.1 {buttonHeight - 0.1}", AnchorMax = $"0.9 {buttonHeight}" },
                    Text = { Text = category, FontSize = 16, Align = TextAnchor.MiddleCenter }
                }, panel);
                buttonHeight -= 0.12f;
            }

            // Close button
            container.Add(new CuiButton
            {
                Button = { Color = "0.8 0.2 0.2 1", Command = "cmd.closeui", Close = MainUI },
                RectTransform = { AnchorMin = "0.3 0.05", AnchorMax = "0.7 0.1" },
                Text = { Text = "CLOSE", FontSize = 14, Align = TextAnchor.MiddleCenter }
            }, panel);

            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("cmd.showcategory")]
        private void CmdShowCategory(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || arg.Args == null || arg.Args.Length == 0) return;
            
            ShowCategoryUI(player, arg.Args[0]);
        }

        private void ShowCategoryUI(BasePlayer player, string category)
        {
            if (!shopCategories.ContainsKey(category)) return;

            DestroyUI(player);

            CuiElementContainer container = new CuiElementContainer();
            string panel = container.Add(new CuiPanel
            {
                Image = { Color = "0.1 0.1 0.1 0.95" },
                RectTransform = { AnchorMin = "0.2 0.1", AnchorMax = "0.8 0.9" },
                CursorEnabled = true
            }, "Overlay", CategoryUI);

            // Title
            container.Add(new CuiLabel
            {
                Text = { Text = category.ToUpper(), FontSize = 20, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0.9", AnchorMax = "1 0.95" }
            }, panel);

            // Items
            float itemPos = 0.8f;
            var items = shopCategories[category];
            
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                string buttonText = $"{item.Name} - {item.Price} scrap";
                if (item.Amount > 1) buttonText += $" (x{item.Amount})";

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2 0.6 0.2 1", Command = $"cmd.buy {category} {i}" },
                    RectTransform = { AnchorMin = $"0.1 {itemPos - 0.1}", AnchorMax = $"0.9 {itemPos}" },
                    Text = { Text = buttonText, FontSize = 14, Align = TextAnchor.MiddleLeft }
                }, panel);
                
                itemPos -= 0.12f;
            }

            // Back button
            container.Add(new CuiButton
            {
                Button = { Color = "0.3 0.5 0.8 1", Command = "cmd.shop" },
                RectTransform = { AnchorMin = "0.1 0.05", AnchorMax = "0.4 0.1" },
                Text = { Text = "BACK", FontSize = 14, Align = TextAnchor.MiddleCenter }
            }, panel);

            // Close button
            container.Add(new CuiButton
            {
                Button = { Color = "0.8 0.2 0.2 1", Command = "cmd.closeui", Close = CategoryUI },
                RectTransform = { AnchorMin = "0.6 0.05", AnchorMax = "0.9 0.1" },
                Text = { Text = "CLOSE", FontSize = 14, Align = TextAnchor.MiddleCenter }
            }, panel);

            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("cmd.closeui")]
        private void CmdCloseUI(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            DestroyUI(player);
        }

        [ConsoleCommand("cmd.shop")]
        private void CmdShopConsole(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            OpenMainMenu(player);
        }

        [ConsoleCommand("cmd.buy")]
        private void CmdBuy(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || arg.Args == null || arg.Args.Length < 2) return;
            
            string category = arg.Args[0];
            if (!int.TryParse(arg.Args[1], out int index) || !shopCategories.ContainsKey(category)) return;
            if (index < 0 || index >= shopCategories[category].Count) return;

            TryBuy(player, shopCategories[category][index]);
        }

        private void TryBuy(BasePlayer player, ShopItem item)
        {
            int scrapAmount = GetItemCount(player, ScrapItemId);
            if (scrapAmount < item.Price)
            {
                player.ChatMessage($"<color=red>You need {item.Price} scrap (you have {scrapAmount})</color>");
                return;
            }

            TakeItem(player, ScrapItemId, item.Price);
            player.GiveItem(ItemManager.CreateByName(item.ShortName, item.Amount));
            player.ChatMessage($"<color=green>Purchased {item.Name} x{item.Amount} for {item.Price} scrap!</color>");
        }

        private int GetItemCount(BasePlayer player, int itemId)
        {
            return player.inventory.containerMain.itemList
                .Concat(player.inventory.containerBelt.itemList)
                .Concat(player.inventory.containerWear.itemList)
                .Where(i => i.info.itemid == itemId)
                .Sum(i => i.amount);
        }

        private void TakeItem(BasePlayer player, int itemId, int amount)
        {
            int remaining = amount;
            var items = player.inventory.containerMain.itemList
                .Concat(player.inventory.containerBelt.itemList)
                .Concat(player.inventory.containerWear.itemList)
                .Where(i => i.info.itemid == itemId);

            foreach (var item in items)
            {
                int toTake = Mathf.Min(item.amount, remaining);
                item.UseItem(toTake);
                remaining -= toTake;
                if (remaining <= 0) break;
            }
        }

        private void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, MainUI);
            CuiHelper.DestroyUi(player, CategoryUI);
        }

        private class ShopItem
        {
            public string Name;
            public string ShortName;
            public int Price;
            public int Amount;

            public ShopItem(string name, string shortName, int price, int amount = 1)
            {
                Name = name;
                ShortName = shortName;
                Price = price;
                Amount = amount;
            }
        }
    }
}
