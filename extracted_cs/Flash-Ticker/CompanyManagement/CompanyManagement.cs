using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using System;

namespace Oxide.Plugins
{
    [Info("CompanyManagement", "RustFlash", "1.0.0")]
    public partial class CompanyManagement : RustPlugin
    {
        #region Fields

        private StoredData storedData;
        private WarehouseData warehouseData;
        private const string CompanyPermission = "companymanagement.use";
        private const string UIMainName = "CompanyUI";
        private Dictionary<string, string> activeUIs = new Dictionary<string, string>();
        private Dictionary<string, string> playerInput = new Dictionary<string, string>();
        private Dictionary<string, Company> Companies = new Dictionary<string, Company>();
        private ConfigData configData;
        private Timer salaryTimer;

        #endregion

        #region Hooks

        void Init()
        {
            permission.RegisterPermission(CompanyPermission, this);
            LoadData();
        }

        void Unload()
        {
            if (salaryTimer != null && !salaryTimer.Destroyed)
                salaryTimer.Destroy();
            
            foreach (var player in BasePlayer.activePlayerList)
            {
                CloseUI(player);
            }
        }
        
		void OnServerInitialized()
        {
            salaryTimer = timer.Every(3600f, () => ProcessSalaryPayments());
        }

        #endregion

        #region Commands

        [ChatCommand("company")]
        void CompanyCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, CompanyPermission))
            {
                SendMessage(player, "Du hast keine Berechtigung, diesen Befehl zu verwenden.");
                return;
            }

            OpenUI(player);
        }

        #endregion

        #region Data Management

        private class StoredData
        {
            public Dictionary<string, Company> Companies = new Dictionary<string, Company>();
        }

        public class ConfigData
        {
            public string Currency { get; set; }
            public ulong CurrencySkinId { get; set; }
        }

        public class WarehouseData
        {
            public Dictionary<string, CompanyWarehouse> Warehouses = new Dictionary<string, CompanyWarehouse>();
        }

        public class CompanyWarehouse
        {
            public decimal Balance { get; set; } = 0;
            public List<WarehouseTransaction> Transactions = new List<WarehouseTransaction>();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                configData = Config.ReadObject<ConfigData>();
                if (configData == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            configData = new ConfigData
            {
                Currency = "scrap",
                CurrencySkinId = 0
            };
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(configData);
        }

        public class Company
        {
            public string Name { get; set; }
            public string OwnerId { get; set; }
            public Dictionary<string, string> Members = new Dictionary<string, string>();
     		public Dictionary<string, decimal> RankSalaries = new Dictionary<string, decimal>();
            public List<string> Ranks = new List<string>();
            public decimal Balance { get; set; } = 0;
            public List<WarehouseTransaction> Transactions = new List<WarehouseTransaction>();
        }

        public class WarehouseTransaction
        {
            public string PlayerId { get; set; }
            public string PlayerName { get; set; }
            public decimal Amount { get; set; }
            public DateTime Timestamp { get; set; }
            public string Type { get; set; }
        }

        void LoadData()
        {
            var dataPath = $"{Interface.Oxide.DataDirectory}/CompanyManagement";
            if (!System.IO.Directory.Exists(dataPath))
            {
                System.IO.Directory.CreateDirectory(dataPath);
            }

            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("CompanyManagement/data");
            if (storedData == null)
            {
                storedData = new StoredData();
                SaveData();
            }

            warehouseData = Interface.Oxide.DataFileSystem.ReadObject<WarehouseData>("CompanyManagement/warehouse");
            if (warehouseData == null)
            {
                warehouseData = new WarehouseData();
                SaveWarehouseData();
            }

            MigrateOldData();

            foreach (var warehouse in warehouseData.Warehouses.Values)
            {
                if (warehouse.Transactions == null)
                    warehouse.Transactions = new List<WarehouseTransaction>();
            }
        }

        void SaveWarehouseData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("CompanyManagement/warehouse", warehouseData);
        }

        void MigrateOldData()
        {
            foreach (var company in storedData.Companies)
            {
                if (!warehouseData.Warehouses.ContainsKey(company.Key))
                {
                    var warehouse = new CompanyWarehouse
                    {
                        Balance = company.Value.Balance,
                        Transactions = company.Value.Transactions
                    };
                    warehouseData.Warehouses[company.Key] = warehouse;

                    company.Value.Balance = 0;
                    company.Value.Transactions = null;
                }
            }
            SaveData();
            SaveWarehouseData();
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("CompanyManagement/data", storedData);
        }

        #endregion

        #region Company Management
        
        private void SendMessage(BasePlayer player, string message)
        {
            player.ChatMessage(message);
        }
        
        private Company GetPlayerCompany(BasePlayer player)
        {
            return storedData.Companies.Values.FirstOrDefault(c => 
                c.OwnerId == player.UserIDString || 
                c.Members.ContainsKey(player.UserIDString));
        }
        
        private void DeleteCompany(BasePlayer player)
        {
            var company = GetPlayerCompany(player);
            if (company == null)
            {
                SendMessage(player, "Du besitzt keine Firma!");
                return;
            }

            if (company.OwnerId != player.UserIDString)
            {
                SendMessage(player, "Nur der Besitzer kann die Firma löschen!");
                return;
            }

            foreach (var memberId in company.Members.Keys)
            {
                var memberPlayer = BasePlayer.Find(memberId);
                if (memberPlayer != null)
                {
                    SendMessage(memberPlayer, $"Die Firma '{company.Name}' wurde aufgelöst!");
                }
            }

            storedData.Companies.Remove(company.Name);
            if (warehouseData.Warehouses.ContainsKey(company.Name))
            {
                warehouseData.Warehouses.Remove(company.Name);
            }

            SaveData();
            SaveWarehouseData();

            SendMessage(player, $"Die Firma '{company.Name}' wurde erfolgreich gelöscht!");
        }
                
    	private void SetRankSalary(BasePlayer player, string rankName, decimal salary)
        {
            var company = GetPlayerCompany(player);
            if (company == null)
            {
                SendMessage(player, "Du besitzt keine Firma!");
                return;
            }

            if (company.OwnerId != player.UserIDString)
            {
                SendMessage(player, "Nur der Besitzer kann Gehälter festlegen!");
                return;
            }

            if (!company.Ranks.Contains(rankName))
            {
                SendMessage(player, "Dieser Rang existiert nicht!");
                return;
            }

            if (salary < 0)
            {
                SendMessage(player, "Das Gehalt kann nicht negativ sein!");
                return;
            }

            company.RankSalaries[rankName] = salary;
            SaveData();
            SendMessage(player, $"Gehalt für Rang '{rankName}' wurde auf {salary} {configData.Currency} festgelegt!");
        }
        
        private void ProcessSalaryPayments()
        {
            foreach (var company in storedData.Companies.Values)
            {
                if (!warehouseData.Warehouses.ContainsKey(company.Name))
                    continue;

                var warehouse = warehouseData.Warehouses[company.Name];
                decimal totalSalaries = 0;

                // Berechne die Gesamtsumme der Gehälter
                foreach (var member in company.Members)
                {
                    string rank = member.Value;
                    if (!string.IsNullOrEmpty(rank) && company.RankSalaries.ContainsKey(rank))
                    {
                        totalSalaries += company.RankSalaries[rank];
                    }
                }

                if (warehouse.Balance < totalSalaries)
                {
                    var owner = BasePlayer.Find(company.OwnerId);
                    if (owner != null)
                    {
                        SendMessage(owner, $"WARNUNG: Nicht genügend Geld für Gehaltszahlungen! Benötigt: {totalSalaries} {configData.Currency}");
                    }
                    continue;
                }

                foreach (var member in company.Members)
                {
                    string rank = member.Value;
                    if (!string.IsNullOrEmpty(rank) && company.RankSalaries.ContainsKey(rank))
                    {
                        decimal salary = company.RankSalaries[rank];
                        var playerObj = BasePlayer.Find(member.Key);
                        
                        if (playerObj != null)
                        {
                            Item payment = ItemManager.CreateByName(configData.Currency, (int)salary, configData.CurrencySkinId);
                            if (playerObj.inventory.GiveItem(payment))
                            {
                                warehouse.Balance -= salary;
                                warehouse.Transactions.Add(new WarehouseTransaction
                                {
                                    PlayerId = member.Key,
                                    PlayerName = playerObj.displayName,
                                    Amount = -salary,
                                    Timestamp = DateTime.Now,
                                    Type = "salary"
                                });
                                SendMessage(playerObj, $"Du hast dein Gehalt von {salary} {configData.Currency} erhalten!");
                            }
                            else
                            {
                                payment.Remove();
                                SendMessage(playerObj, "Dein Inventar ist voll! Gehalt konnte nicht ausgezahlt werden.");
                            }
                        }
                    }
                }
                SaveWarehouseData();
            }
        }
        
        private void SetPlayerRank(BasePlayer player, string targetName, string rankName)
        {
            var company = GetPlayerCompany(player);
            if (company == null)
            {
                SendMessage(player, "Du besitzt keine Firma!");
                return;
            }

            if (company.OwnerId != player.UserIDString)
            {
                SendMessage(player, "Nur der Besitzer kann Ränge zuweisen!");
                return;
            }

            var target = BasePlayer.Find(targetName);
            if (target == null)
            {
                SendMessage(player, "Spieler nicht gefunden!");
                return;
            }

            if (!company.Members.ContainsKey(target.UserIDString))
            {
                SendMessage(player, "Dieser Spieler ist kein Mitglied deiner Firma!");
                return;
            }

            if (!company.Ranks.Contains(rankName))
            {
                SendMessage(player, "Dieser Rang existiert nicht!");
                return;
            }

            company.Members[target.UserIDString] = rankName;
            SaveData();

            SendMessage(player, $"Der Rang von {target.displayName} wurde auf '{rankName}' geändert!");
            SendMessage(target, $"Dein Rang in der Firma '{company.Name}' wurde auf '{rankName}' geändert!");
        }
        
        private void AddRank(BasePlayer player, string rankName)
        {
            var company = GetPlayerCompany(player);
            if (company == null)
            {
                SendMessage(player, "Du besitzt keine Firma!");
                return;
            }

            if (company.OwnerId != player.UserIDString)
            {
                SendMessage(player, "Nur der Besitzer kann Ränge hinzufügen!");
                return;
            }

            if (company.Ranks.Contains(rankName))
            {
                SendMessage(player, "Dieser Rang existiert bereits!");
                return;
            }

            if (company.Ranks.Count >= 12)
            {
                SendMessage(player, "Du kannst maximal 12 Ränge erstellen! Lösche zuerst einen bestehenden Rang.");
                return;
            }

            company.Ranks.Add(rankName);
            SaveData();

            SendMessage(player, $"Rang '{rankName}' wurde erfolgreich hinzugefügt!");
        }

        private void RemoveRank(BasePlayer player, string rankName)
        {
            var company = GetPlayerCompany(player);
            if (company == null)
            {
                SendMessage(player, "Du besitzt keine Firma!");
                return;
            }

            if (company.OwnerId != player.UserIDString)
            {
                SendMessage(player, "Nur der Besitzer kann Ränge entfernen!");
                return;
            }

            if (!company.Ranks.Contains(rankName))
            {
                SendMessage(player, "Dieser Rang existiert nicht!");
                return;
            }

            company.Ranks.Remove(rankName);

            // Setze den Rang von Mitgliedern mit diesem Rang auf leer
            foreach (var member in company.Members.Where(m => m.Value == rankName).ToList())
            {
                company.Members[member.Key] = "";
            }

            SaveData();
            SendMessage(player, $"Rang '{rankName}' wurde erfolgreich entfernt!");
        }
        
        private void InvitePlayer(BasePlayer player, string targetName)
        {
            var company = GetPlayerCompany(player);
            if (company == null)
            {
                SendMessage(player, "Du besitzt keine Firma!");
                return;
            }

            if (company.OwnerId != player.UserIDString)
            {
                SendMessage(player, "Nur der Besitzer kann Mitglieder einladen!");
                return;
            }

            var target = BasePlayer.Find(targetName);
            if (target == null)
            {
                SendMessage(player, "Spieler nicht gefunden!");
                return;
            }

            if (company.Members.ContainsKey(target.UserIDString))
            {
                SendMessage(player, "Dieser Spieler ist bereits Mitglied der Firma!");
                return;
            }

            company.Members.Add(target.UserIDString, ""); // Fügt das Mitglied ohne Rang hinzu
            SaveData();

            SendMessage(player, $"{target.displayName} wurde zur Firma hinzugefügt!");
            SendMessage(target, $"Du wurdest zur Firma '{company.Name}' hinzugefügt!");
        }
        
    	private void KickPlayer(BasePlayer player, string targetName)
        {
            var company = GetPlayerCompany(player);
            if (company == null)
            {
                SendMessage(player, "Du besitzt keine Company!");
                return;
            }

            if (company.OwnerId != player.UserIDString)
            {
                SendMessage(player, "Nur der Besitzer kann Mitglieder entfernen!");
                return;
            }

            var target = BasePlayer.Find(targetName);
            if (target == null)
            {
                SendMessage(player, "Spieler nicht gefunden!");
                return;
            }

            if (!company.Members.ContainsKey(target.UserIDString))
            {
                SendMessage(player, "Dieser Spieler ist kein Mitglied deiner Company!");
                return;
            }

            company.Members.Remove(target.UserIDString);
            SaveData();
            SendMessage(player, $"{target.displayName} wurde aus der Company entfernt!");
            SendMessage(target, $"Du wurdest aus der Company '{company.Name}' entfernt!");
        }
        
        private void CreateCompany(BasePlayer player, string companyName)
        {
            if (storedData.Companies.Values.Any(c => c.OwnerId == player.UserIDString))
            {
                SendMessage(player, "Du besitzt bereits eine Company!");
                return;
            }

            if (storedData.Companies.ContainsKey(companyName))
            {
                SendMessage(player, "Eine Company mit diesem Namen existiert bereits!");
                return;
            }

            var company = new Company
            {
                Name = companyName,
                OwnerId = player.UserIDString
            };

            company.Members.Add(player.UserIDString, "");

            storedData.Companies.Add(companyName, company);
            warehouseData.Warehouses[companyName] = new CompanyWarehouse();

            SaveData();
            SaveWarehouseData();
            SendMessage(player, $"Company '{companyName}' wurde erfolgreich erstellt!");
        }


    	#endregion

        #region Warehouse Management

        void DepositMoney(BasePlayer player, decimal amount)
        {
            var company = GetPlayerCompany(player);
            if (company == null)
            {
                SendMessage(player, "Du bist in keiner Company!");
                return;
            }

            if (!warehouseData.Warehouses.ContainsKey(company.Name))
            {
                warehouseData.Warehouses[company.Name] = new CompanyWarehouse();
            }

            var warehouse = warehouseData.Warehouses[company.Name];

            Item currencyItem = FindCurrencyInInventory(player);
            if (currencyItem == null || currencyItem.amount < amount)
            {
                SendMessage(player, $"Du hast nicht genügend {configData.Currency}!");
                return;
            }

            currencyItem.amount -= (int)amount;
            if (currencyItem.amount <= 0)
                currencyItem.Remove();
            else
                currencyItem.MarkDirty();

            warehouse.Balance += amount;
            warehouse.Transactions.Add(new WarehouseTransaction
            {
                PlayerId = player.UserIDString,
                PlayerName = player.displayName,
                Amount = amount,
                Timestamp = DateTime.Now,
                Type = "deposit"
            });

            SaveWarehouseData();
            SendMessage(player, $"Du hast {amount} {configData.Currency} eingezahlt.");
            ShowWarehouseContent(player);
        }

        void WithdrawMoney(BasePlayer player, decimal amount)
        {
            var company = GetPlayerCompany(player);
            if (company == null)
            {
                SendMessage(player, "Du bist in keiner Company!");
                return;
            }

            if (!warehouseData.Warehouses.ContainsKey(company.Name))
            {
                warehouseData.Warehouses[company.Name] = new CompanyWarehouse();
            }

            var warehouse = warehouseData.Warehouses[company.Name];

            if (company.OwnerId != player.UserIDString)
            {
                SendMessage(player, "Nur der Owner kann Geld auszahlen!");
                return;
            }

            if (warehouse.Balance < amount)
            {
                SendMessage(player, "Nicht genügend Geld in der Company Kasse!");
                return;
            }

            Item currencyItem = ItemManager.CreateByName(configData.Currency, (int)amount, configData.CurrencySkinId);
            if (!player.inventory.GiveItem(currencyItem))
            {
                currencyItem.Remove();
                SendMessage(player, "Dein Inventar ist voll!");
                return;
            }

            warehouse.Balance -= amount;
            warehouse.Transactions.Add(new WarehouseTransaction
            {
                PlayerId = player.UserIDString,
                PlayerName = player.displayName,
                Amount = amount,
                Timestamp = DateTime.Now,
                Type = "withdraw"
            });

            SaveWarehouseData();
            SendMessage(player, $"Du hast {amount} {configData.Currency} ausgezahlt.");
            ShowWarehouseContent(player);
        }

        Item FindCurrencyInInventory(BasePlayer player)
        {
            var currency = configData.Currency;
            var skinId = configData.CurrencySkinId;

            foreach (Item item in player.inventory.containerMain.itemList)
            {
                if (item.info.shortname == currency && item.skin == skinId)
                    return item;
            }

            foreach (Item item in player.inventory.containerBelt.itemList)
            {
                if (item.info.shortname == currency && item.skin == skinId)
                    return item;
            }

            foreach (Item item in player.inventory.containerWear.itemList)
            {
                if (item.info.shortname == currency && item.skin == skinId)
                    return item;
            }

            return null;
        }

        #endregion

        #region UI

        void OpenUI(BasePlayer player)
        {
            CloseUI(player);
            var ui = CompanyUI.CreateUI();
            CuiHelper.AddUi(player, ui);
            activeUIs[player.UserIDString] = UIMainName;
        }

        void CloseUI(BasePlayer player)
        {
            if (activeUIs.ContainsKey(player.UserIDString))
            {
                CuiHelper.DestroyUi(player, activeUIs[player.UserIDString]);
                CuiHelper.DestroyUi(player, UIMainName + "_Content");
                activeUIs.Remove(player.UserIDString);
            }
        }

        [ConsoleCommand("company.ui")]
        void ConsoleCommand_UI(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            string subCommand = arg.GetString(0);

            switch (subCommand.ToLower())
            {
                case "open":
                    OpenUI(player);
                    break;
                case "close":
                    CloseUI(player);
                    break;
                case "company":
                    ShowCompanyContent(player);
                    break;
                case "ranks":
                    ShowRanksContent(player);
                    break;
                case "staff":
                    ShowStaffContent(player);
                    break;
                case "delete_company":
                    DeleteCompany(player);
                    CloseUI(player);
                    break;                    
                case "select_rank":
                    var rankTargetId = arg.GetString(1);
                    var rankToSet = arg.GetString(2);
                    var rankTargetPlayer = BasePlayer.Find(rankTargetId);
                    if (rankTargetPlayer != null)
                    {
                        SetPlayerRank(player, rankTargetPlayer.displayName, rankToSet);
                        ShowStaffContent(player);
                    }
                    break;
                case "add_rank":
                    if (playerInput.ContainsKey(player.UserIDString + "_rankname"))
                    {
                        AddRank(player, playerInput[player.UserIDString + "_rankname"]);
                        playerInput.Remove(player.UserIDString + "_rankname");
                        ShowRanksContent(player);
                    }
                    break;
                case "remove_rank":
                    string rankToRemove = arg.GetString(1);
                    RemoveRank(player, rankToRemove);
                    ShowRanksContent(player);
                    break;
                case "invite_member":
                    if (playerInput.ContainsKey(player.UserIDString + "_playername"))
                    {
                        InvitePlayer(player, playerInput[player.UserIDString + "_playername"]);
                        playerInput.Remove(player.UserIDString + "_playername");
                        ShowStaffContent(player);
                    }
                    break;
                case "kick_member":
                    string targetId = arg.GetString(1);
                    var target = BasePlayer.Find(targetId);
                    if (target != null)
                    {
                        KickPlayer(player, target.displayName);
                        ShowStaffContent(player);
                    }
                    break;
                case "input":
                    string inputField = arg.GetString(1);
                    string inputValue = arg.GetString(2);
                    playerInput[player.UserIDString + "_" + inputField] = inputValue;
                    break;
                case "show_ranks":
                    var ranksMember = arg.GetString(1);
                    ShowRanksForMember(player, ranksMember);
                    break;
                case "commands":
                    ShowCommandsContent(player);
                    break;
                case "move_rank_up":
                    MoveRankUp(player, arg.GetString(1));
                    break;
                case "move_rank_down":
                    MoveRankDown(player, arg.GetString(1));
                    break;
                case "create_company":
                    if (playerInput.ContainsKey(player.UserIDString + "_companyname"))
                    {
                        string companyName = playerInput[player.UserIDString + "_companyname"];
                        if (!string.IsNullOrWhiteSpace(companyName))
                        {
                            CreateCompany(player, companyName);
                            playerInput.Remove(player.UserIDString + "_companyname");
                            ShowCompanyContent(player);
                        }
                        else
                        {
                            SendMessage(player, "Bitte gib einen Company Namen ein!");
                        }
                    }
                    break;                    
                case "warehouse":
                    ShowWarehouseContent(player);
                    break;
                case "warehouse_withdraw":
                    if (playerInput.ContainsKey(player.UserIDString + "_amount"))
                    {
                        if (decimal.TryParse(playerInput[player.UserIDString + "_amount"], out decimal amount))
                        {
                            WithdrawMoney(player, amount);
                            playerInput.Remove(player.UserIDString + "_amount");
                        }
                        else
                            SendMessage(player, "Bitte gib einen gültigen Betrag ein!");
                    }
                    break;
                case "warehouse_deposit":
                    if (playerInput.ContainsKey(player.UserIDString + "_amount"))
                    {
                        if (decimal.TryParse(playerInput[player.UserIDString + "_amount"], out decimal amount))
                        {
                            DepositMoney(player, amount);
                            playerInput.Remove(player.UserIDString + "_amount");
                        }
                        else
                            SendMessage(player, "Bitte gib einen gültigen Betrag ein!");
                    }
                    break;
                case "set_salary":
                    string salaryRankName = arg.GetString(1);
                    if (decimal.TryParse(arg.GetString(2), out decimal salary))
                    {
                        SetRankSalary(player, salaryRankName, salary);
                        ShowRanksContent(player);
                    }
                    break;
            }
        }
        
        void ShowCompanyContent(BasePlayer player)
        {
            var company = GetPlayerCompany(player);
            CuiHelper.DestroyUi(player, UIMainName + "_Content");

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.02 0.02", AnchorMax = "0.98 0.98" }
            }, UIMainName + "_ContentPanel", UIMainName + "_Content");

            if (company == null)
            {
                container.Add(new CuiLabel
                {
                    Text = { Text = "Erstelle deine Company", FontSize = 28, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0.02 0.9", AnchorMax = "0.98 0.98" }
                }, UIMainName + "_Content");

                container.Add(new CuiLabel
                {
                    Text = { Text = "Gib einen Namen für deine Company, Gang oder Familie ein:", FontSize = 16, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.02 0.8", AnchorMax = "0.98 0.85" }
                }, UIMainName + "_Content");

                var inputPanel = UIMainName + "_InputPanel";
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.1 0.1 0.1 0.8" },
                    RectTransform = { AnchorMin = "0.02 0.7", AnchorMax = "0.8 0.75" }
                }, UIMainName + "_Content", inputPanel);

                container.Add(new CuiPanel
                {
                    Image = { Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.003 1" }
                }, inputPanel);

                container.Add(new CuiElement
                {
                    Parent = inputPanel,
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            CharsLimit = 30,
                            Command = $"company.ui input companyname ",
                            FontSize = 14,
                            Text = "Company Name eingeben..."
                        },
                        new CuiRectTransformComponent { AnchorMin = "0.02 0", AnchorMax = "0.98 1" }
                    }
                });

                var buttonPanel = UIMainName + "_CreateButton";
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.2 0.6 0.2 1" },
                    RectTransform = { AnchorMin = "0.82 0.7", AnchorMax = "0.98 0.75" }
                }, UIMainName + "_Content", buttonPanel);

                container.Add(new CuiPanel
                {
                    Image = { Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.003 1" }
                }, buttonPanel);

                container.Add(new CuiButton
                {
                    Button = { Command = "company.ui create_company", Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = "Erstellen", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, buttonPanel);
            }
            else
            {
                float itemHeight = 0.06f;
                float spacing = 0.01f;

                container.Add(new CuiLabel
                {
                    Text = { Text = company.Name, FontSize = 28, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0.02 0.9", AnchorMax = "0.98 0.98" }
                }, UIMainName + "_Content");

                var infoPanel = UIMainName + "_InfoPanel";
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.1 0.1 0.1 0.8" },
                    RectTransform = { AnchorMin = "0.02 0.7", AnchorMax = "0.98 0.85" }
                }, UIMainName + "_Content", infoPanel);

                container.Add(new CuiPanel
                {
                    Image = { Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.003 1" }
                }, infoPanel);

                var infoText = $"<size=16>Besitzer: {BasePlayer.Find(company.OwnerId)?.displayName ?? "Unbekannt"}\n";
                infoText += $"Mitglieder: {company.Members.Count}\n";
                infoText += $"Ränge: {company.Ranks.Count}/12</size>";

                container.Add(new CuiLabel
                {
                    Text = { Text = infoText, FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.02 0.02", AnchorMax = "0.98 0.98" }
                }, infoPanel);

                var columnsPanel = UIMainName + "_ColumnsPanel";
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.1 0.1 0.1 0.8" },
                    RectTransform = { AnchorMin = "0.02 0.2", AnchorMax = "0.98 0.65" }
                }, UIMainName + "_Content", columnsPanel);

                var leftColumn = columnsPanel + "_Left";
                container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.48 1" }
                }, columnsPanel, leftColumn);

                container.Add(new CuiLabel
                {
                    Text = { Text = "Mitglieder", FontSize = 16, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0.02 0.92", AnchorMax = "0.98 1" }
                }, leftColumn);

                var memberListPanel = leftColumn + "_List";
                container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0.02 0", AnchorMax = "0.98 0.9" }
                }, leftColumn, memberListPanel);

                var sortedMembers = company.Members
                    .Select(m => new
                    {
                        Member = m,
                        Player = BasePlayer.Find(m.Key),
                        IsOwner = m.Key == company.OwnerId
                    })
                    .OrderByDescending(x => x.IsOwner)
                    .ThenByDescending(x => x.Player != null)
                    .ThenBy(x => x.Member.Value)
                    .ToList();

                float memberStartY = 1f;

                foreach (var memberInfo in sortedMembers)
                {
                    var member = memberInfo.Member;
                    var memberPlayer = memberInfo.Player;
                    bool isOnline = memberPlayer != null;

                    var memberItemPanel = $"{memberListPanel}_{member.Key}";
                    container.Add(new CuiPanel
                    {
                        Image = { Color = memberInfo.IsOwner ? "0.2 0.2 0.2 0.8" : "0.15 0.15 0.15 0.8" },
                        RectTransform = { AnchorMin = $"0 {memberStartY - itemHeight}", AnchorMax = $"1 {memberStartY}" }
                    }, memberListPanel, memberItemPanel);

                    container.Add(new CuiPanel
                    {
                        Image = { Color = isOnline ? "0.2 0.8 0.2 1" : "0.8 0.2 0.2 1" },
                        RectTransform = { AnchorMin = "0.02 0.3", AnchorMax = "0.04 0.7" }
                    }, memberItemPanel);

                    string displayName = (isOnline ? memberPlayer.displayName : BasePlayer.FindAwakeOrSleeping(member.Key)?.displayName) ?? "Unbekannt";
                    if (memberInfo.IsOwner)
                    {
                        displayName += " [Owner]";
                    }
                    displayName += $" [{(isOnline ? "Online" : "Offline")}]";

                    container.Add(new CuiLabel
                    {
                        Text = { Text = displayName, FontSize = 12, Align = TextAnchor.MiddleLeft, Color = memberInfo.IsOwner ? "1 0.93 0 1" : "1 1 1 1" },
                        RectTransform = { AnchorMin = "0.06 0", AnchorMax = "0.6 1" }
                    }, memberItemPanel);

                    string rankDisplay = string.IsNullOrEmpty(member.Value) ? "Kein Rang" : member.Value;
                    container.Add(new CuiLabel
                    {
                        Text = { Text = rankDisplay, FontSize = 12, Align = TextAnchor.MiddleRight, Color = "0.7 0.7 0.7 1" },
                        RectTransform = { AnchorMin = "0.62 0", AnchorMax = "0.98 1" }
                    }, memberItemPanel);

                    memberStartY -= (itemHeight + spacing);
                }
                
                var rightColumn = columnsPanel + "_Right";
                container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0.52 0", AnchorMax = "1 1" }
                }, columnsPanel, rightColumn);

                container.Add(new CuiLabel
                {
                    Text = { Text = "Ränge", FontSize = 16, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0.02 0.92", AnchorMax = "0.98 1" }
                }, rightColumn);

                float rankStartY = 0.9f;

                foreach (var rank in company.Ranks)
                {
                    container.Add(new CuiLabel
                    {
                        Text = { Text = rank, FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                        RectTransform = { AnchorMin = $"0.02 {rankStartY - itemHeight}", AnchorMax = $"0.98 {rankStartY}" }
                    }, rightColumn);

                    rankStartY -= (itemHeight + spacing);
                }

                container.Add(new CuiPanel
                {
                    Image = { Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.503 1" }
                }, columnsPanel);

                if (company.OwnerId == player.UserIDString)
                {
                    var deletePanel = UIMainName + "_DeleteButton";
                    container.Add(new CuiPanel
                    {
                        Image = { Color = "0.8 0.2 0.2 1" },
                        RectTransform = { AnchorMin = "0.02 0.1", AnchorMax = "0.2 0.15" }
                    }, UIMainName + "_Content", deletePanel);

                    container.Add(new CuiPanel
                    {
                        Image = { Color = "1 0.93 0 1" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "0.003 1" }
                    }, deletePanel);

                    container.Add(new CuiButton
                    {
                        Button = { Command = "company.ui delete_company", Color = "0 0 0 0" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Text = { Text = "Company löschen", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                    }, deletePanel);
                }
            }

            CuiHelper.AddUi(player, container);
        }

        void ShowRanksContent(BasePlayer player)
        {
            var company = GetPlayerCompany(player);
            CuiHelper.DestroyUi(player, UIMainName + "_Content");

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.02 0.02", AnchorMax = "0.98 0.98" }
            }, UIMainName + "_ContentPanel", UIMainName + "_Content");

            if (company == null)
            {
                container.Add(new CuiLabel
                {
                    Text = { Text = "Keine Company", FontSize = 28, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0.02 0.9", AnchorMax = "0.98 0.98" }
                }, UIMainName + "_Content");

                container.Add(new CuiLabel
                {
                    Text = { Text = "Du musst erst eine Company erstellen oder einer Company beitreten.", FontSize = 16, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.02 0.8", AnchorMax = "0.98 0.85" }
                }, UIMainName + "_Content");
            }
            else
            {
                container.Add(new CuiLabel
                {
                    Text = { Text = $"Ränge ({company.Ranks.Count}/12)", FontSize = 28, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0.02 0.9", AnchorMax = "0.98 0.98" }
                }, UIMainName + "_Content");

                var mainPanel = UIMainName + "_MainPanel";
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.1 0.1 0.1 0.8" },
                    RectTransform = { AnchorMin = "0.02 0.2", AnchorMax = "0.98 0.85" }
                }, UIMainName + "_Content", mainPanel);

                container.Add(new CuiPanel
                {
                    Image = { Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.003 1" }
                }, mainPanel);

                // Header für die Spalten
                container.Add(new CuiLabel
                {
                    Text = { Text = "Position", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0.04 0.92", AnchorMax = "0.1 0.98" }
                }, mainPanel);

                container.Add(new CuiLabel
                {
                    Text = { Text = "Rang", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0.12 0.92", AnchorMax = "0.4 0.98" }
                }, mainPanel);

                container.Add(new CuiLabel
                {
                    Text = { Text = "Gehalt/Stunde", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0.42 0.92", AnchorMax = "0.7 0.98" }
                }, mainPanel);

                var rankListPanel = UIMainName + "_RankList";
                container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0.02 0.02", AnchorMax = "0.98 0.88" }
                }, mainPanel, rankListPanel);

                float itemHeight = 0.08f;  // Erhöht für zusätzlichen Platz
                float spacing = 0.01f;
                float currentY = 1f;

                for (int i = company.Ranks.Count - 1; i >= 0; i--)
                {
                    string rank = company.Ranks[i];
                    int displayRank = i + 1;

                    var rankPanel = $"{UIMainName}_Rank_{rank}";
                    container.Add(new CuiPanel
                    {
                        Image = { Color = "0.15 0.15 0.15 0.8" },
                        RectTransform = { AnchorMin = $"0 {currentY - itemHeight}", AnchorMax = $"1 {currentY}" }
                    }, rankListPanel, rankPanel);

                    // Position
                    container.Add(new CuiLabel
                    {
                        Text = { Text = $"#{displayRank}", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                        RectTransform = { AnchorMin = "0.02 0", AnchorMax = "0.1 1" }
                    }, rankPanel);

                    // Rangname
                    container.Add(new CuiLabel
                    {
                        Text = { Text = rank, FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                        RectTransform = { AnchorMin = "0.12 0", AnchorMax = "0.4 1" }
                    }, rankPanel);

                    // Gehaltsbereich
                    decimal currentSalary = 0;
                    if (company.RankSalaries != null && company.RankSalaries.ContainsKey(rank))
                    {
                        currentSalary = company.RankSalaries[rank];
                    }

                    if (company.OwnerId == player.UserIDString)
                    {
                        // Gehalts-Input für Owner
                        container.Add(new CuiElement
                        {
                            Parent = rankPanel,
                            Components =
                            {
                                new CuiInputFieldComponent
                                {
                                    Align = TextAnchor.MiddleLeft,
                                    CharsLimit = 10,
                                    Command = $"company.ui set_salary {rank} ",
                                    FontSize = 12,
                                    Text = currentSalary.ToString()
                                },
                                new CuiRectTransformComponent { AnchorMin = "0.42 0.2", AnchorMax = "0.55 0.8" }
                            }
                        });

                        container.Add(new CuiLabel
                        {
                            Text = { Text = configData.Currency, FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 0.7" },
                            RectTransform = { AnchorMin = "0.56 0", AnchorMax = "0.7 1" }
                        }, rankPanel);
                    }
                    else
                    {
                        // Nur Anzeige für normale Mitglieder
                        container.Add(new CuiLabel
                        {
                            Text = { Text = $"{currentSalary} {configData.Currency}", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                            RectTransform = { AnchorMin = "0.42 0", AnchorMax = "0.7 1" }
                        }, rankPanel);
                    }

                    if (company.OwnerId == player.UserIDString)
                    {
                        // Rang-Verwaltungsbuttons
                        if (i > 0)
                        {
                            container.Add(new CuiButton
                            {
                                Button = { Command = $"company.ui move_rank_up {rank}", Color = "0.2 0.6 0.2 1" },
                                RectTransform = { AnchorMin = "0.73 0.1", AnchorMax = "0.78 0.9" },
                                Text = { Text = "↓", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                            }, rankPanel);
                        }

                        if (i < company.Ranks.Count - 1)
                        {
                            container.Add(new CuiButton
                            {
                                Button = { Command = $"company.ui move_rank_down {rank}", Color = "0.2 0.6 0.2 1" },
                                RectTransform = { AnchorMin = "0.79 0.1", AnchorMax = "0.84 0.9" },
                                Text = { Text = "↑", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                            }, rankPanel);
                        }

                        var deleteButton = $"{rankPanel}_delete";
                        container.Add(new CuiPanel
                        {
                            Image = { Color = "0.8 0.2 0.2 1" },
                            RectTransform = { AnchorMin = "0.85 0.1", AnchorMax = "0.97 0.9" }
                        }, rankPanel, deleteButton);

                        container.Add(new CuiButton
                        {
                            Button = { Command = $"company.ui remove_rank {rank}", Color = "0 0 0 0" },
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                            Text = { Text = "X", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                        }, deleteButton);
                    }

                    currentY -= (itemHeight + spacing);
                }

                // Panel für neue Ränge
                if (company.OwnerId == player.UserIDString && company.Ranks.Count < 12)
                {
                    var createPanel = UIMainName + "_CreatePanel";
                    container.Add(new CuiPanel
                    {
                        Image = { Color = "0.1 0.1 0.1 0.8" },
                        RectTransform = { AnchorMin = "0.02 0.1", AnchorMax = "0.98 0.15" }
                    }, UIMainName + "_Content", createPanel);

                    container.Add(new CuiPanel
                    {
                        Image = { Color = "1 0.93 0 1" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "0.003 1" }
                    }, createPanel);

                    container.Add(new CuiElement
                    {
                        Parent = createPanel,
                        Components =
                        {
                            new CuiInputFieldComponent
                            {
                                Align = TextAnchor.MiddleLeft,
                                CharsLimit = 30,
                                Command = $"company.ui input rankname ",
                                FontSize = 12,
                                Text = "Rang Name eingeben..."
                            },
                            new CuiRectTransformComponent { AnchorMin = "0.02 0.2", AnchorMax = "0.8 0.8" }
                        }
                    });

                    container.Add(new CuiButton
                    {
                        Button = { Command = "company.ui add_rank", Color = "0.2 0.6 0.2 1" },
                        RectTransform = { AnchorMin = "0.82 0.2", AnchorMax = "0.98 0.8" },
                        Text = { Text = "Erstellen", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                    }, createPanel);
                }
            }

            CuiHelper.AddUi(player, container);
        }

        void ShowStaffContent(BasePlayer player)
        {
            var company = GetPlayerCompany(player);
            CuiHelper.DestroyUi(player, UIMainName + "_Content");

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.02 0.02", AnchorMax = "0.98 0.98" }
            }, UIMainName + "_ContentPanel", UIMainName + "_Content");

            if (company == null)
            {
                container.Add(new CuiLabel
                {
                    Text = { Text = "Keine Company", FontSize = 28, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0.02 0.9", AnchorMax = "0.98 0.98" }
                }, UIMainName + "_Content");

                container.Add(new CuiLabel
                {
                    Text = { Text = "Du musst erst eine Company erstellen oder einer Company beitreten.", FontSize = 16, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.02 0.8", AnchorMax = "0.98 0.85" }
                }, UIMainName + "_Content");
            }
            else
            {
                container.Add(new CuiLabel
                {
                    Text = { Text = $"Mitarbeiter ({company.Members.Count})", FontSize = 28, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0.02 0.9", AnchorMax = "0.98 0.98" }
                }, UIMainName + "_Content");

                var ownerPanel = UIMainName + "_OwnerPanel";
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.1 0.1 0.1 0.8" },
                    RectTransform = { AnchorMin = "0.02 0.8", AnchorMax = "0.98 0.85" }
                }, UIMainName + "_Content", ownerPanel);

                container.Add(new CuiPanel
                {
                    Image = { Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.003 1" }
                }, ownerPanel);

                var ownerPlayer = BasePlayer.Find(company.OwnerId);
                container.Add(new CuiLabel
                {
                    Text = { Text = $"Owner: {ownerPlayer?.displayName ?? "Unbekannt"}", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.02 0", AnchorMax = "0.98 1" }
                }, ownerPanel);

                var mainPanel = UIMainName + "_MainPanel";
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.1 0.1 0.1 0.8" },
                    RectTransform = { AnchorMin = "0.02 0.2", AnchorMax = "0.98 0.75" }
                }, UIMainName + "_Content", mainPanel);

                container.Add(new CuiPanel
                {
                    Image = { Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.003 1" }
                }, mainPanel);

                var staffListPanel = UIMainName + "_StaffList";
                container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0.02 0.02", AnchorMax = "0.98 0.98" }
                }, mainPanel, staffListPanel);

                float itemHeight = 0.08f;
                float spacing = 0.01f;
                float currentY = 1f;

                foreach (var member in company.Members)
                {
                    var memberPlayer = BasePlayer.Find(member.Key);
                    if (memberPlayer != null)
                    {
                        var memberPanel = $"{UIMainName}_Member_{member.Key}";
                        container.Add(new CuiPanel
                        {
                            Image = { Color = "0.15 0.15 0.15 0.8" },
                            RectTransform = { AnchorMin = $"0 {currentY - itemHeight}", AnchorMax = $"1 {currentY}" }
                        }, staffListPanel, memberPanel);

                        container.Add(new CuiLabel
                        {
                            Text = { Text = memberPlayer.displayName, FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                            RectTransform = { AnchorMin = "0.02 0", AnchorMax = "0.3 1" }
                        }, memberPanel);

                        string currentRank = member.Value;
                        container.Add(new CuiLabel
                        {
                            Text = { Text = string.IsNullOrEmpty(currentRank) ? "Kein Rang" : currentRank, FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                            RectTransform = { AnchorMin = "0.32 0", AnchorMax = "0.6 1" }
                        }, memberPanel);

                        if (company.OwnerId == player.UserIDString)
                        {
                            var rankButton = memberPanel + "_rank";
                            container.Add(new CuiPanel
                            {
                                Image = { Color = "0.2 0.6 0.2 1" },
                                RectTransform = { AnchorMin = "0.62 0.2", AnchorMax = "0.75 0.8" }
                            }, memberPanel, rankButton);

                            container.Add(new CuiButton
                            {
                                Button = { Command = $"company.ui show_ranks {member.Key}", Color = "0 0 0 0" },
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                                Text = { Text = "Rang", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                            }, rankButton);

                            var kickButton = memberPanel + "_kick";
                            container.Add(new CuiPanel
                            {
                                Image = { Color = "0.8 0.2 0.2 1" },
                                RectTransform = { AnchorMin = "0.77 0.2", AnchorMax = "0.9 0.8" }
                            }, memberPanel, kickButton);

                            container.Add(new CuiButton
                            {
                                Button = { Command = $"company.ui kick_member {member.Key}", Color = "0 0 0 0" },
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                                Text = { Text = "Kick", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                            }, kickButton);
                        }

                        currentY -= (itemHeight + spacing);
                    }
                }

                if (company.OwnerId == player.UserIDString)
                {
                    var invitePanel = UIMainName + "_InvitePanel";
                    container.Add(new CuiPanel
                    {
                        Image = { Color = "0.1 0.1 0.1 0.8" },
                        RectTransform = { AnchorMin = "0.02 0.1", AnchorMax = "0.98 0.15" }
                    }, UIMainName + "_Content", invitePanel);

                    container.Add(new CuiPanel
                    {
                        Image = { Color = "1 0.93 0 1" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "0.003 1" }
                    }, invitePanel);

                    container.Add(new CuiElement
                    {
                        Parent = invitePanel,
                        Components =
                        {
                            new CuiInputFieldComponent
                            {
                                Align = TextAnchor.MiddleLeft,
                                CharsLimit = 30,
                                Command = $"company.ui input playername ",
                                FontSize = 12,
                                Text = "Spielername eingeben..."
                            },
                            new CuiRectTransformComponent { AnchorMin = "0.02 0.2", AnchorMax = "0.8 0.8" }
                        }
                    });

                    container.Add(new CuiButton
                    {
                        Button = { Command = "company.ui invite_member", Color = "0.2 0.6 0.2 1" },
                        RectTransform = { AnchorMin = "0.82 0.2", AnchorMax = "0.98 0.8" },
                        Text = { Text = "Einladen", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                    }, invitePanel);
                }
            }

            CuiHelper.AddUi(player, container);
        }

        void ShowWarehouseContent(BasePlayer player)
        {
            var company = GetPlayerCompany(player);
            CuiHelper.DestroyUi(player, UIMainName + "_Content");

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.02 0.02", AnchorMax = "0.98 0.98" }
            }, UIMainName + "_ContentPanel", UIMainName + "_Content");

            if (company == null)
            {
                container.Add(new CuiLabel
                {
                    Text = { Text = "Keine Company", FontSize = 28, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0.02 0.9", AnchorMax = "0.98 0.98" }
                }, UIMainName + "_Content");

                container.Add(new CuiLabel
                {
                    Text = { Text = "Du musst erst eine Company erstellen oder einer Company beitreten.", FontSize = 16, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.02 0.8", AnchorMax = "0.98 0.85" }
                }, UIMainName + "_Content");

                CuiHelper.AddUi(player, container);
                return;
            }

            var warehouse = warehouseData.Warehouses.ContainsKey(company.Name)
                ? warehouseData.Warehouses[company.Name]
                : new CompanyWarehouse();

            if (warehouse.Transactions == null)
                warehouse.Transactions = new List<WarehouseTransaction>();

            var sortedTransactions = warehouse.Transactions
                .Where(t => t != null)
                .OrderByDescending(t => t.Timestamp)
                .ToList();

            container.Add(new CuiLabel
            {
                Text = { Text = $"Company Kasse: {warehouse.Balance} {configData.Currency}", FontSize = 28, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                RectTransform = { AnchorMin = "0.02 0.9", AnchorMax = "0.98 0.98" }
            }, UIMainName + "_Content");

            var mainPanel = UIMainName + "_MainPanel";
            container.Add(new CuiPanel
            {
                Image = { Color = "0.1 0.1 0.1 0.8" },
                RectTransform = { AnchorMin = "0.02 0.2", AnchorMax = "0.98 0.85" }
            }, UIMainName + "_Content", mainPanel);

            container.Add(new CuiPanel
            {
                Image = { Color = "1 0.93 0 1" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.003 1" }
            }, mainPanel);

            float startY = 0.95f;
            float itemHeight = 0.05f;
            float spacing = 0.01f;

            container.Add(new CuiLabel
            {
                Text = { Text = "Datum", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                RectTransform = { AnchorMin = "0.02 0.9", AnchorMax = "0.2 0.95" }
            }, mainPanel);

            container.Add(new CuiLabel
            {
                Text = { Text = "Spieler", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                RectTransform = { AnchorMin = "0.22 0.9", AnchorMax = "0.4 0.95" }
            }, mainPanel);

            container.Add(new CuiLabel
            {
                Text = { Text = "Betrag", FontSize = 12, Align = TextAnchor.MiddleRight, Color = "1 0.93 0 1" },
                RectTransform = { AnchorMin = "0.42 0.9", AnchorMax = "0.6 0.95" }
            }, mainPanel);

            startY = 0.85f;

            var actionPanel = UIMainName + "_ActionPanel";
            container.Add(new CuiPanel
            {
                Image = { Color = "0.1 0.1 0.1 0.8" },
                RectTransform = { AnchorMin = "0.02 0.1", AnchorMax = "0.98 0.15" }
            }, UIMainName + "_Content", actionPanel);

            container.Add(new CuiPanel
            {
                Image = { Color = "1 0.93 0 1" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.003 1" }
            }, actionPanel);

            container.Add(new CuiElement
            {
                Parent = actionPanel,
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        CharsLimit = 10,
                        Command = $"company.ui input amount ",
                        FontSize = 12,
                        Text = "Betrag eingeben..."
                    },
                    new CuiRectTransformComponent { AnchorMin = "0.02 0.2", AnchorMax = "0.6 0.8" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Command = "company.ui warehouse_deposit", Color = "0.2 0.6 0.2 1" },
                RectTransform = { AnchorMin = "0.62 0.2", AnchorMax = "0.75 0.8" },
                Text = { Text = "Einzahlen", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, actionPanel);

            if (company.OwnerId == player.UserIDString)
            {
                container.Add(new CuiButton
                {
                    Button = { Command = "company.ui warehouse_withdraw", Color = "0.8 0.2 0.2 1" },
                    RectTransform = { AnchorMin = "0.77 0.2", AnchorMax = "0.9 0.8" },
                    Text = { Text = "Auszahlen", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, actionPanel);
            }

            CuiHelper.AddUi(player, container);
        }

        void ShowCommandsContent(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UIMainName + "_Content");
            var container = new CuiElementContainer();

            container.Add(new CuiLabel
            {
                Text = { Text = "CompanyManagement by RustFlash", FontSize = 28, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                RectTransform = { AnchorMin = "0.02 0.9", AnchorMax = "0.98 0.98" }
            }, UIMainName + "_ContentPanel");

            var commandsText = "<size=16>";
            commandsText += $"* <color=#CC6600>CompanyManagement</color> - A RustFlash Plugin!\n";
            commandsText += "</size>";

            container.Add(new CuiLabel
            {
                Text = { Text = commandsText, FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.02 0.2", AnchorMax = "0.98 0.89" }
            }, UIMainName + "_ContentPanel");

            CuiHelper.AddUi(player, container);
        }

        void MoveRankUp(BasePlayer player, string rankName)
        {
            var company = GetPlayerCompany(player);
            if (company == null || company.OwnerId != player.UserIDString) return;

            int currentIndex = company.Ranks.IndexOf(rankName);
            if (currentIndex > 0)
            {
                string temp = company.Ranks[currentIndex - 1];
                company.Ranks[currentIndex - 1] = company.Ranks[currentIndex];
                company.Ranks[currentIndex] = temp;
                SaveData();
                ShowRanksContent(player);
            }
        }
       
      	void MoveRankDown(BasePlayer player, string rankName)
        {
            var company = GetPlayerCompany(player);
            if (company == null || company.OwnerId != player.UserIDString) return;

            int currentIndex = company.Ranks.IndexOf(rankName);
            if (currentIndex < company.Ranks.Count - 1)
            {
                string temp = company.Ranks[currentIndex + 1];
                company.Ranks[currentIndex + 1] = company.Ranks[currentIndex];
                company.Ranks[currentIndex] = temp;
                SaveData();
                ShowRanksContent(player);
            }
        }

        void ShowRanksForMember(BasePlayer player, string targetId)
        {
            var company = GetPlayerCompany(player);
            if (company == null || company.OwnerId != player.UserIDString) return;

            CuiHelper.DestroyUi(player, UIMainName + "_Content");
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.02 0.02", AnchorMax = "0.98 0.98" }
            }, UIMainName + "_ContentPanel", UIMainName + "_Content");

            var targetPlayer = BasePlayer.Find(targetId);
            container.Add(new CuiLabel
            {
                Text = { Text = $"Rang für {targetPlayer?.displayName ?? "Unbekannt"} auswählen", FontSize = 24, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                RectTransform = { AnchorMin = "0.02 0.9", AnchorMax = "0.98 0.98" }
            }, UIMainName + "_Content");

            var mainPanel = UIMainName + "_MainPanel";
            container.Add(new CuiPanel
            {
                Image = { Color = "0.1 0.1 0.1 0.8" },
                RectTransform = { AnchorMin = "0.02 0.2", AnchorMax = "0.98 0.85" }
            }, UIMainName + "_Content", mainPanel);

            container.Add(new CuiPanel
            {
                Image = { Color = "1 0.93 0 1" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.003 1" }
            }, mainPanel);

            float startY = 0.9f;
            float itemHeight = 0.08f;
            float spacing = 0.02f;

            foreach (var rank in company.Ranks)
            {
                var rankPanel = $"{UIMainName}_RankSelect_{rank}";
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.15 0.15 0.15 0.8" },
                    RectTransform = { AnchorMin = $"0.1 {startY - itemHeight}", AnchorMax = $"0.9 {startY}" }
                }, mainPanel, rankPanel);

                container.Add(new CuiButton
                {
                    Button = { Command = $"company.ui select_rank {targetId} {rank}", Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = rank, FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, rankPanel);

                startY -= (itemHeight + spacing);
            }

            container.Add(new CuiButton
            {
                Button = { Command = "company.ui staff", Color = "0.8 0.2 0.2 1" },
                RectTransform = { AnchorMin = "0.02 0.1", AnchorMax = "0.2 0.15" },
                Text = { Text = "Zurück", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, UIMainName + "_Content");

            CuiHelper.AddUi(player, container);
        }
        
        #endregion

        #region CompanyUI
        public partial class CompanyUI
        {
            public static CuiElementContainer CreateUI()
            {
                var container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0.9" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    CursorEnabled = true
                }, "Overlay", UIMainName);

                container.Add(new CuiLabel
                {
                    Text = { Text = "COMPANY", FontSize = 30, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.02 0.92", AnchorMax = "0.3 0.98" }
                }, UIMainName);

                AddMenuButton(container, "COMPANY", "0.02 0.8", "0.15 0.85", "company");
                AddMenuButton(container, "RANKS", "0.02 0.74", "0.15 0.79", "ranks");
                AddMenuButton(container, "STAFF", "0.02 0.68", "0.15 0.73", "staff");
                AddMenuButton(container, "WAREHOUSE", "0.02 0.62", "0.15 0.67", "warehouse");
                AddMenuButton(container, "RETURN", "0.02 0.2", "0.15 0.25", "close", true);

                var contentPanel = UIMainName + "_ContentPanel";
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.1 0.1 0.1 0.8" },
                    RectTransform = { AnchorMin = "0.17 0.02", AnchorMax = "0.98 0.85" }
                }, UIMainName, contentPanel);

                var infoButton = UIMainName + "_INFO";
                container.Add(new CuiButton
                {
                    Button = { Color = "0.1 0.1 0.1 0.8", Command = "company.ui open" },
                    RectTransform = { AnchorMin = "0.9 0.86", AnchorMax = "0.98 0.90" },
                    Text = { Text = "INFO", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, UIMainName, infoButton);

                container.Add(new CuiPanel
                {
                    Image = { Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.01 1" }
                }, infoButton);

                container.Add(new CuiPanel
                {
                    Image = { Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.003 1" }
                }, contentPanel);

                container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0.02 0.02", AnchorMax = "0.98 0.98" }
                }, contentPanel, UIMainName + "_Content");

                container.Add(new CuiLabel
                {
                    Text = { Text = "Commands", FontSize = 28, Align = TextAnchor.MiddleLeft, Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0.02 0.9", AnchorMax = "0.98 0.98" }
                }, UIMainName + "_Content");

                var commandsText = "<size=16>";
                commandsText += $"* <color=#CC6600>CompanyManagement</color> - A RustFlash Plugin\n";
                commandsText += "</size>";

                container.Add(new CuiLabel
                {
                    Text = { Text = commandsText, FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.02 0.2", AnchorMax = "0.98 0.89" }
                }, UIMainName + "_Content");

                return container;
            }

            private static void AddMenuButton(CuiElementContainer container, string text, string anchorMin, string anchorMax, string command, bool isReturn = false)
            {
                var buttonColor = isReturn ? "0.5 0.1 0.1 0.8" : "0.1 0.1 0.1 0.8";
                var buttonName = UIMainName + "_" + text;

                container.Add(new CuiButton
                {
                    Button = { Color = buttonColor, Command = $"company.ui {command}" },
                    RectTransform = { AnchorMin = anchorMin, AnchorMax = anchorMax },
                    Text = { Text = text, FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, UIMainName, buttonName);

                container.Add(new CuiPanel
                {
                    Image = { Color = "1 0.93 0 1" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.01 1" }
                }, buttonName);
            }

            public static CuiElementContainer CreateCompanyContent(Company company)
            {
                var container = new CuiElementContainer();

                if (company != null)
                {
                    container.Add(new CuiLabel
                    {
                        Text = { Text = company.Name, FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        RectTransform = { AnchorMin = "0.02 0.9", AnchorMax = "0.98 0.95" }
                    }, UIMainName + "_Content");

                    var infoText = $"Besitzer: {BasePlayer.Find(company.OwnerId)?.displayName ?? "Unbekannt"}\n";
                    infoText += $"Mitglieder: {company.Members.Count}\n";
                    infoText += $"Ränge: {company.Ranks.Count}/12";

                    container.Add(new CuiLabel
                    {
                        Text = { Text = infoText, FontSize = 18, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                        RectTransform = { AnchorMin = "0.02 0.7", AnchorMax = "0.98 0.85" }
                    }, UIMainName + "_Content");

                    AddActionButton(container, "Company löschen", "0.02 0.1", "0.2 0.15", "company.ui delete_company", "0.8 0.2 0.2 1");
                }
                else
                {
                    container.Add(new CuiLabel
                    {
                        Text = { Text = "Neue Company erstellen", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        RectTransform = { AnchorMin = "0.02 0.9", AnchorMax = "0.98 0.95" }
                    }, UIMainName + "_Content");

                    container.Add(new CuiLabel
                    {
                        Text = { Text = "Gib deiner Company einen Namen um sie zu erstellen:", FontSize = 16, Align = TextAnchor.UpperLeft, Color = "1 1 1 0.8" },
                        RectTransform = { AnchorMin = "0.02 0.8", AnchorMax = "0.98 0.85" }
                    }, UIMainName + "_Content");

                    AddInputField(container, "0.02 0.7", "0.7 0.75", "Company Name eingeben...", "companyname");
                    AddActionButton(container, "Company erstellen", "0.75 0.7", "0.98 0.75", "company.ui create_company", "0.2 0.6 0.2 1");
                }

                return container;
            }

            public static CuiElementContainer CreateRanksContent(Company company)
            {
                var container = new CuiElementContainer();

                if (company != null)
                {
                    container.Add(new CuiLabel
                    {
                        Text = { Text = "Rang Verwaltung", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        RectTransform = { AnchorMin = "0.02 0.9", AnchorMax = "0.98 0.95" }
                    }, UIMainName + "_Content");

                    float startY = 0.8f;
                    foreach (var rank in company.Ranks)
                    {
                        container.Add(new CuiLabel
                        {
                            Text = { Text = rank, FontSize = 16, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                            RectTransform = { AnchorMin = $"0.02 {startY}", AnchorMax = $"0.8 {startY + 0.05f}" }
                        }, UIMainName + "_Content");

                        AddActionButton(container, "X", $"0.85 {startY}", $"0.9 {startY + 0.05f}", $"company.ui remove_rank {rank}", "0.8 0.2 0.2 1");

                        startY -= 0.06f;
                    }

                    if (company.Ranks.Count < 12)
                    {
                        AddInputField(container, "0.02 0.1", "0.6 0.15", "Rang Name...", "rankname");
                        AddActionButton(container, "Rang erstellen", "0.65 0.1", "0.85 0.15", "company.ui add_rank", "0.2 0.6 0.2 1");
                    }
                }

                return container;
            }

            public static CuiElementContainer CreateStaffContent(Company company)
            {
                var container = new CuiElementContainer();

                if (company != null)
                {
                    container.Add(new CuiLabel
                    {
                        Text = { Text = "Mitarbeiter Verwaltung", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        RectTransform = { AnchorMin = "0 0.9", AnchorMax = "1 0.95" }
                    }, UIMainName + "_Content");

                    float startY = 0.8f;
                    foreach (var member in company.Members)
                    {
                        var memberPlayer = BasePlayer.Find(member.Key);
                        if (memberPlayer != null)
                        {
                            container.Add(new CuiLabel
                            {
                                Text = { Text = $"{memberPlayer.displayName} - {member.Value}", FontSize = 16, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                                RectTransform = { AnchorMin = $"0 {startY}", AnchorMax = $"0.7 {startY + 0.05f}" }
                            }, UIMainName + "_Content");

                            AddActionButton(container, "Rang", $"0.75 {startY}", $"0.85 {startY + 0.05f}", $"company.ui show_ranks {member.Key}", "0.2 0.6 0.2 1");
                            AddActionButton(container, "Kick", $"0.88 {startY}", $"0.95 {startY + 0.05f}", $"company.ui kick_member {member.Key}", "0.8 0.2 0.2 1");

                            startY -= 0.06f;
                        }
                    }

                    AddInputField(container, "0 0.1", "0.6 0.15", "Spielername...", "playername");
                    AddActionButton(container, "Einladen", "0.65 0.1", "0.85 0.15", "company.ui invite_member", "0.2 0.6 0.2 1");
                }

                return container;
            }

            public static void AddActionButton(CuiElementContainer container, string text, string anchorMin, string anchorMax, string command, string color)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command },
                    RectTransform = { AnchorMin = anchorMin, AnchorMax = anchorMax },
                    Text = { Text = text, FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, UIMainName + "_Content");
            }

            private static void AddInputField(CuiElementContainer container, string anchorMin, string anchorMax, string placeholder, string inputField)
            {
                var elementName = UIMainName + "_" + inputField;
                container.Add(new CuiElement
                {
                    Parent = UIMainName + "_Content",
                    Name = elementName,
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            CharsLimit = 30,
                            Color = "1 1 1 1",
                            Command = $"company.ui input {inputField} ",
                            FontSize = 14,
                            IsPassword = false,
                            Text = placeholder
                        },
                        new CuiRectTransformComponent { AnchorMin = anchorMin, AnchorMax = anchorMax }
                    }
                });
            }
        }
        #endregion
    }
}