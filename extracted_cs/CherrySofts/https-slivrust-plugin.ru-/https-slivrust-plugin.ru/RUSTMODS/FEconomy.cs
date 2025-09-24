// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("FEconomy", "YourName", "2.1.0")]
    [Description("A flexible economy system using eggs as currency.")]
    public class FEconomy : CovalencePlugin
    {
        // Хранилище категорий товаров
        private Dictionary<string, Dictionary<string, int>> shopCategories = new Dictionary<string, Dictionary<string, int>>();

        // Константа для валюты
        private const string CurrencyItemShortname = "egg";

        // Хранилище дат последних наград
        private Dictionary<string, DateTime> lastRewardDates = new Dictionary<string, DateTime>();

        // Инициализация плагина
        private void Init()
        {
            LoadShopConfig();
            LoadRewardData();

            // Регистрация разрешений
            AddPermission("feconomy.admin", "Allows use of admin commands.");
            AddPermission("feconomy.use", "Allows use of basic commands.");

            Puts("FEconomy has been loaded!");
        }

        // Сохранение конфигурации магазина
        private void SaveShopConfig()
        {
            Config["ShopCategories"] = shopCategories;
            SaveConfig();
        }

        // Загрузка конфигурации магазина
        private void LoadShopConfig()
        {
            if (Config["ShopCategories"] == null)
            {
                Puts("ShopCategories section is missing in the configuration file. Creating a new one...");
                LoadDefaultConfig();
            }

            shopCategories = Config["ShopCategories"] as Dictionary<string, Dictionary<string, int>> ?? new Dictionary<string, Dictionary<string, int>>();
        }

        // Создание базового файла конфигурации
        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new configuration file...");

            var defaultShopCategories = GenerateCategorizedItems();

            Config.Clear();
            Config["ShopCategories"] = defaultShopCategories;
            SaveConfig();
        }

        // Метод для генерации всех предметов с разделением на категории
        private Dictionary<string, Dictionary<string, int>> GenerateCategorizedItems()
        {
            var categorizedItems = new Dictionary<string, Dictionary<string, int>>();

            foreach (var item in ItemManager.itemList)
            {
                if (item == null || string.IsNullOrEmpty(item.shortname)) continue;

                string category = GetItemCategory(item);

                if (!categorizedItems.ContainsKey(category))
                {
                    categorizedItems[category] = new Dictionary<string, int>();
                }

                categorizedItems[category][item.shortname] = 10; // Начальная цена в яйцах
            }

            return categorizedItems;
        }

        // Метод для определения категории предмета
        private string GetItemCategory(ItemDefinition item)
        {
            if (item.category == ItemCategory.Weapon) return "Weapons";
            if (item.shortname.Contains("ammo")) return "Ammunition";
            if (IsResource(item.shortname)) return "Resources";
            if (item.category == ItemCategory.Food) return "Food";
            if (item.category == ItemCategory.Medical) return "Medical";
            if (item.category == ItemCategory.Tool) return "Tools";
            if (IsClothing(item.shortname)) return "Clothing";
            if (IsBuildingMaterial(item.shortname)) return "Building";
            return "Miscellaneous";
        }

        // Метод для проверки, является ли предмет ресурсом
        private bool IsResource(string shortname)
        {
            var resources = new HashSet<string>
            {
                "wood",
                "stone",
                "metal.fragments",
                "sulfur",
                "hq.metal.ore",
                "metal.ore",
                "sulfur.ore"
            };

            return resources.Contains(shortname);
        }

        // Метод для проверки, является ли предмет одеждой
        private bool IsClothing(string shortname)
        {
            var clothing = new HashSet<string>
            {
                "hat.cap",
                "tshirt",
                "pants",
                "shoes.boots"
            };

            return clothing.Contains(shortname);
        }

        // Метод для проверки, является ли предмет строительным материалом
        private bool IsBuildingMaterial(string shortname)
        {
            var buildingMaterials = new HashSet<string>
            {
                "wall.wood",
                "door.hinged.wood",
                "foundation.wood",
                "floor.wood"
            };

            return buildingMaterials.Contains(shortname);
        }

        // Автоматическое создание файлов локализации
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"BalanceMessage", "Your current balance is: {0} eggs."},
                {"ShopEmpty", "The shop is currently empty."},
                {"CategoryListHeader", "Available categories in the shop:\n"},
                {"ShopUsage", "Usage: /shop <category>"},
                {"CategoryNotFound", "The category '{0}' does not exist."},
                {"ItemListHeader", "Items in category '{0}':\n"},
                {"BuyUsage", "Usage: /buy <category> <item>"},
                {"ItemNotFound", "The item '{0}' is not available in the specified category."},
                {"NotEnoughMoney", "You do not have enough eggs to buy this item. You need {0} more eggs."},
                {"PurchaseSuccess", "You have successfully purchased {0} for {1} eggs."},
                {"ErrorGivingItem", "An error occurred while trying to give you the item."},
                {"ItemCreationFailed", "This item could not be created."},
                {"NoPermission", "You do not have permission to use this command."},
                {"AddEggsUsage", "Usage: /addeggs <player> <amount>"},
                {"InvalidAmount", "Invalid amount specified."},
                {"PlayerNotFound", "Player '{0}' not found."},
                {"AddedEggs", "Added {0} eggs to {1}'s inventory."},
                {"ReceivedEggs", "You received {0} eggs from an admin."},
                {"AddEconomyItemUsage", "Usage: /addeconomyitem <category> <item> <price>"},
                {"InvalidPrice", "Invalid price specified."},
                {"ItemAlreadyExists", "The item '{0}' already exists in the category '{1}'."},
                {"ItemAddedToShop", "The item '{0}' has been added to the category '{1}' with a price of {2} eggs."},
                {"RemoveEconomyItemUsage", "Usage: /removeeconomyitem <category> <item>"},
                {"ItemNotFoundInCategory", "The item '{0}' does not exist in the category '{1}'."},
                {"ItemRemovedFromShop", "The item '{0}' has been removed from the category '{1}'."},
                {"SetPriceUsage", "Usage: /setprice <category> <item> <price>"},
                {"PriceUpdated", "The price of '{0}' in the category '{1}' has been updated to {2} eggs."},
                {"DailyRewardReceived", "You have received your daily reward of {0} eggs!"},
                {"DailyRewardAlreadyClaimed", "You have already claimed your daily reward today."}
            }, this, "en");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"BalanceMessage", "Ваш текущий баланс: {0} яиц."},
                {"ShopEmpty", "Магазин пуст."},
                {"CategoryListHeader", "Доступные категории в магазине:\n"},
                {"ShopUsage", "Использование: /shop <категория>"},
                {"CategoryNotFound", "Категория '{0}' не существует."},
                {"ItemListHeader", "Товары в категории '{0}':\n"},
                {"BuyUsage", "Использование: /buy <категория> <товар>"},
                {"ItemNotFound", "Товар '{0}' не доступен в указанной категории."},
                {"NotEnoughMoney", "У вас недостаточно яиц для покупки этого товара. Вам нужно еще {0} яиц."},
                {"PurchaseSuccess", "Вы успешно купили {0} за {1} яиц."},
                {"ErrorGivingItem", "Произошла ошибка при попытке выдать вам товар."},
                {"ItemCreationFailed", "Этот товар не может быть создан."},
                {"NoPermission", "У вас нет прав для использования этой команды."},
                {"AddEggsUsage", "Использование: /addeggs <игрок> <количество>"},
                {"InvalidAmount", "Указано неверное количество яиц."},
                {"PlayerNotFound", "Игрок '{0}' не найден."},
                {"AddedEggs", "Добавлено {0} яиц в инвентарь игрока {1}."},
                {"ReceivedEggs", "Вы получили {0} яиц от администратора."},
                {"AddEconomyItemUsage", "Использование: /addeconomyitem <категория> <товар> <цена>"},
                {"InvalidPrice", "Указана неверная цена."},
                {"ItemAlreadyExists", "Товар '{0}' уже существует в категории '{1}'."},
                {"ItemAddedToShop", "Товар '{0}' добавлен в категорию '{1}' с ценой {2} яиц."},
                {"RemoveEconomyItemUsage", "Использование: /removeeconomyitem <категория> <товар>"},
                {"ItemNotFoundInCategory", "Товар '{0}' не существует в категории '{1}'."},
                {"ItemRemovedFromShop", "Товар '{0}' удален из категории '{1}'."},
                {"SetPriceUsage", "Использование: /setprice <категория> <товар> <цена>"},
                {"PriceUpdated", "Цена товара '{0}' в категории '{1}' обновлена до {2} яиц."},
                {"DailyRewardReceived", "Вы получили ежедневную награду в размере {0} яиц!"},
                {"DailyRewardAlreadyClaimed", "Вы уже получили ежедневную награду сегодня."}
            }, this, "ru");
        }

        // Команда для проверки баланса
        [Command("balance")]
        private void CmdBalance(IPlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "feconomy.use"))
            {
                player.Reply(Lang("NoPermission", player.Id));
                return;
            }

            int balance = GetPlayerBalance(player);
            player.Reply(Lang("BalanceMessage", player.Id, balance));
        }

        // Метод для получения баланса игрока
        private int GetPlayerBalance(IPlayer player)
        {
            var basePlayer = BasePlayer.Find(player.Id);
            if (basePlayer == null) return 0;

            int totalEggs = 0;

            // Проверяем основной инвентарь
            foreach (var item in basePlayer.inventory.containerMain.itemList)
            {
                if (item.info.shortname == CurrencyItemShortname)
                {
                    totalEggs += item.amount;
                }
            }

            // Проверяем пояс
            foreach (var item in basePlayer.inventory.containerBelt.itemList)
            {
                if (item.info.shortname == CurrencyItemShortname)
                {
                    totalEggs += item.amount;
                }
            }

            return totalEggs;
        }

        // Команда для выдачи яиц игроку
        [Command("addeggs")]
        private void CmdAddEggs(IPlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "feconomy.admin"))
            {
                player.Reply(Lang("NoPermission", player.Id));
                return;
            }

            if (args.Length < 2)
            {
                player.Reply(Lang("AddEggsUsage", player.Id));
                return;
            }

            string targetName = args[0];
            int amount;

            if (!int.TryParse(args[1], out amount) || amount <= 0)
            {
                player.Reply(Lang("InvalidAmount", player.Id));
                return;
            }

            IPlayer target = players.FindPlayer(targetName);
            if (target == null)
            {
                player.Reply(Lang("PlayerNotFound", player.Id, targetName));
                return;
            }

            GiveCurrencyToPlayer(target, amount);
            player.Reply(Lang("AddedEggs", player.Id, amount, target.Name));
            target.Reply(Lang("ReceivedEggs", target.Id, amount));
        }

        // Метод для выдачи валюты игроку
        private void GiveCurrencyToPlayer(IPlayer player, int amount)
        {
            var basePlayer = BasePlayer.Find(player.Id);
            if (basePlayer == null) return;

            var item = ItemManager.CreateByName(CurrencyItemShortname, amount);
            if (item != null)
            {
                basePlayer.inventory.GiveItem(item);
            }
        }

        // Событие входа игрока
        void OnUserConnected(IPlayer player)
        {
            GiveDailyReward(player);
        }

        // Метод для выдачи ежедневной награды
        private void GiveDailyReward(IPlayer player)
        {
            string playerId = player.Id;

            // Если игрок еще не получал награду, добавляем его в словарь
            if (!lastRewardDates.ContainsKey(playerId))
            {
                lastRewardDates[playerId] = DateTime.MinValue;
            }

            // Получаем текущую дату и дату последней награды
            DateTime now = DateTime.UtcNow.Date;
            DateTime lastRewardDate = lastRewardDates[playerId].Date;

            // Проверяем, прошел ли день с момента последней награды
            if (now > lastRewardDate)
            {
                // Выдаем награду
                int rewardAmount = 10; // Количество яиц
                GiveCurrencyToPlayer(player, rewardAmount);

                // Обновляем дату последней награды
                lastRewardDates[playerId] = now;
                SaveRewardData();

                // Уведомляем игрока
                player.Reply(Lang("DailyRewardReceived", player.Id, rewardAmount));
            }
            else
            {
                // Уведомляем игрока, что награда уже получена
                player.Reply(Lang("DailyRewardAlreadyClaimed", player.Id));
            }
        }

        // Загрузка данных о наградах
        private void LoadRewardData()
        {
            lastRewardDates = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, DateTime>>("FEconomy_LastRewards") ?? new Dictionary<string, DateTime>();
        }

        // Сохранение данных о наградах
        private void SaveRewardData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("FEconomy_LastRewards", lastRewardDates);
        }

        // Команда для просмотра списка категорий
        [Command("categories")]
        private void CmdCategories(IPlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "feconomy.use"))
            {
                player.Reply(Lang("NoPermission", player.Id));
                return;
            }

            if (shopCategories.Count == 0)
            {
                player.Reply(Lang("ShopEmpty", player.Id));
                return;
            }

            string categoryList = Lang("CategoryListHeader", player.Id);
            foreach (var category in shopCategories.Keys)
            {
                categoryList += $"{category}\n";
            }

            player.Reply(categoryList);
        }

        // Команда для просмотра товаров в категории
        [Command("shop")]
        private void CmdShop(IPlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "feconomy.use"))
            {
                player.Reply(Lang("NoPermission", player.Id));
                return;
            }

            if (args.Length < 1)
            {
                player.Reply(Lang("ShopUsage", player.Id));
                return;
            }

            string categoryName = args[0];

            if (!shopCategories.ContainsKey(categoryName))
            {
                player.Reply(Lang("CategoryNotFound", player.Id, categoryName));
                return;
            }

            var items = shopCategories[categoryName];
            string itemList = Lang("ItemListHeader", player.Id, categoryName);

            foreach (var item in items)
            {
                itemList += $"{item.Key} - {item.Value} eggs\n";
            }

            player.Reply(itemList);
        }

        // Команда для покупки товара
        [Command("buy")]
        private void CmdBuy(IPlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "feconomy.use"))
            {
                player.Reply(Lang("NoPermission", player.Id));
                return;
            }

            if (args.Length < 2)
            {
                player.Reply(Lang("BuyUsage", player.Id));
                return;
            }

            string categoryName = args[0];
            string itemName = args[1].ToLower();

            if (!shopCategories.ContainsKey(categoryName))
            {
                player.Reply(Lang("CategoryNotFound", player.Id, categoryName));
                return;
            }

            var categoryItems = shopCategories[categoryName];

            if (!categoryItems.ContainsKey(itemName))
            {
                player.Reply(Lang("ItemNotFound", player.Id, itemName));
                return;
            }

            int itemPrice = categoryItems[itemName];
            int playerBalance = GetPlayerBalance(player);

            if (playerBalance < itemPrice)
            {
                player.Reply(Lang("NotEnoughMoney", player.Id, itemPrice - playerBalance));
                return;
            }

            RemoveCurrencyFromPlayer(player, itemPrice);
            GiveItem(player, itemName);

            player.Reply(Lang("PurchaseSuccess", player.Id, itemName, itemPrice));
        }

        // Метод для списания валюты у игрока
        private void RemoveCurrencyFromPlayer(IPlayer player, int amount)
        {
            var basePlayer = BasePlayer.Find(player.Id);
            if (basePlayer == null) return;

            int remainingAmount = amount;

            // Удаляем из основного инвентаря
            foreach (var item in basePlayer.inventory.containerMain.itemList)
            {
                if (item.info.shortname == CurrencyItemShortname)
                {
                    if (remainingAmount <= item.amount)
                    {
                        item.UseItem(remainingAmount);
                        return;
                    }

                    remainingAmount -= item.amount;
                    item.Remove();
                }
            }

            // Удаляем из пояса
            foreach (var item in basePlayer.inventory.containerBelt.itemList)
            {
                if (item.info.shortname == CurrencyItemShortname)
                {
                    if (remainingAmount <= item.amount)
                    {
                        item.UseItem(remainingAmount);
                        return;
                    }

                    remainingAmount -= item.amount;
                    item.Remove();
                }
            }
        }

        // Метод для выдачи предмета игроку
        private void GiveItem(IPlayer player, string itemName)
        {
            var basePlayer = BasePlayer.Find(player.Id);
            if (basePlayer == null)
            {
                player.Reply(Lang("ErrorGivingItem", player.Id));
                return;
            }

            var item = ItemManager.CreateByName(itemName, 1);
            if (item == null)
            {
                player.Reply(Lang("ItemCreationFailed", player.Id));
                return;
            }

            basePlayer.inventory.GiveItem(item);
        }

        // Команда для добавления товара в магазин
        [Command("addeconomyitem")]
        private void CmdAddEconomyItem(IPlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "feconomy.admin"))
            {
                player.Reply(Lang("NoPermission", player.Id));
                return;
            }

            if (args.Length < 3)
            {
                player.Reply(Lang("AddEconomyItemUsage", player.Id));
                return;
            }

            string category = args[0];
            string itemName = args[1].ToLower();
            int price;

            if (!int.TryParse(args[2], out price) || price <= 0)
            {
                player.Reply(Lang("InvalidPrice", player.Id));
                return;
            }

            if (!shopCategories.ContainsKey(category))
            {
                shopCategories[category] = new Dictionary<string, int>();
            }

            if (shopCategories[category].ContainsKey(itemName))
            {
                player.Reply(Lang("ItemAlreadyExists", player.Id, itemName, category));
                return;
            }

            shopCategories[category][itemName] = price;
            SaveShopConfig();

            player.Reply(Lang("ItemAddedToShop", player.Id, itemName, category, price));
        }

        // Команда для удаления товара из магазина
        [Command("removeeconomyitem")]
        private void CmdRemoveEconomyItem(IPlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "feconomy.admin"))
            {
                player.Reply(Lang("NoPermission", player.Id));
                return;
            }

            if (args.Length < 2)
            {
                player.Reply(Lang("RemoveEconomyItemUsage", player.Id));
                return;
            }

            string category = args[0];
            string itemName = args[1].ToLower();

            if (!shopCategories.ContainsKey(category))
            {
                player.Reply(Lang("CategoryNotFound", player.Id, category));
                return;
            }

            if (!shopCategories[category].ContainsKey(itemName))
            {
                player.Reply(Lang("ItemNotFoundInCategory", player.Id, itemName, category));
                return;
            }

            shopCategories[category].Remove(itemName);
            SaveShopConfig();

            player.Reply(Lang("ItemRemovedFromShop", player.Id, itemName, category));
        }

        // Команда для изменения цены товара
        [Command("setprice")]
        private void CmdSetPrice(IPlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "feconomy.admin"))
            {
                player.Reply(Lang("NoPermission", player.Id));
                return;
            }

            if (args.Length < 3)
            {
                player.Reply(Lang("SetPriceUsage", player.Id));
                return;
            }

            string category = args[0];
            string itemName = args[1].ToLower();
            int price;

            if (!int.TryParse(args[2], out price) || price <= 0)
            {
                player.Reply(Lang("InvalidPrice", player.Id));
                return;
            }

            if (!shopCategories.ContainsKey(category))
            {
                player.Reply(Lang("CategoryNotFound", player.Id, category));
                return;
            }

            if (!shopCategories[category].ContainsKey(itemName))
            {
                player.Reply(Lang("ItemNotFoundInCategory", player.Id, itemName, category));
                return;
            }

            shopCategories[category][itemName] = price;
            SaveShopConfig();

            player.Reply(Lang("PriceUpdated", player.Id, itemName, category, price));
        }

        // Метод для регистрации разрешений
        private void AddPermission(string name, string description)
        {
            if (!permission.PermissionExists(name, this))
            {
                permission.RegisterPermission(name, this);
            }
        }

        // Метод для проверки разрешений
        private bool HasPermission(IPlayer player, string permissionName)
        {
            return player.IsAdmin || permission.UserHasPermission(player.Id, permissionName);
        }

        // Метод для получения переведенного сообщения
        private string Lang(string key, string playerId, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this, playerId), args);
        }
    }
}