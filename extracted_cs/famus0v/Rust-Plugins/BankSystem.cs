using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{
    [Info("BankSystem", "Kira", "1.0.1")]
    [Description("Economic system")]
    public class BankSystem : RustPlugin
    {
        #region [Vars]

        [PluginReference] private Plugin ImageLibrary;
        private static WaitForSeconds Wait = new WaitForSeconds(0.1f); 
        private const string UIParent = "UI.BankSystem.Hud"; 
        private Vector3 pos1 = new Vector3(-1922.1f, 5.1f, 1449.6f);
        private Quaternion rot1 = new Quaternion(0.0f, 0.7f, 0.0f, -0.7f);
        private Vector3 pos2 = new Vector3(1290.8f, 5.7f, -1396.0f);
        private Quaternion rot2 = new Quaternion(0.0f, 0.4f, 0.0f, 0.9f); 
        private VendingMachine _vendingMachine1;
        private VendingMachine _vendingMachine2;
        private BaseEntity Box1;
        private Vector3 BoxPos1 = new Vector3(-1929.1f, 5.3f, 1473.1f);
        private Quaternion BoxRot1 = Quaternion.Euler(0, 0, 0);
        private BaseEntity Box2;
        private Vector3 BoxPos2 = new Vector3(1307.3f, 6.0f, -1378.7f);
        private Quaternion BoxRot2 = new Quaternion(0.0f, 0.9f, 0.0f, 0.3f);
        private const string PREFAB_CODE_LOCK = "assets/prefabs/locks/keypad/lock.code.prefab";
        private const string PREFAB_BOX = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab";

        #endregion

        #region [Configuraton] / [Конфигурация]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "BankSystem - Config")]
            public BankSystemCFG BankSystem = new BankSystemCFG();

            public class BankSystemCFG
            {
                [JsonProperty(PropertyName = "[Налог при смерти] (%)")]
                public int DeathNALOG = 20;
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                BankSystem = new ConfigData.BankSystemCFG
                {
                    DeathNALOG = 20
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Файл конфигурации поврежден (или не существует), создан новый!");
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region [Lang]

        protected override void LoadDefaultMessages()
        {
            var ru = new Dictionary<string, string>
            {
                ["ENTERCOINS"] = "Введите число шреков",
                ["ENTERNUMBER"] = "Введите корректное число",
                ["ADDBALANCE"] = "Баланс пополнен",
                ["NOMONEY"] = "Недостаточно шреков",
                ["BALANCE"] = "На вашем балансе {1} шреков",
                ["ENTER"] = "ВВЕДИТЕ"
            };

            var en = new Dictionary<string, string>
            {
                ["ENTERCOINS"] = "Enter the number of shreks",
                ["ENTERNUMBER"] = "Enter the correct number",
                ["ADDBALANCE"] = "The balance is replenished",
                ["NOMONEY"] = "Not enough shreks",
                ["BALANCE"] = "There are {1} shreks on your balance",
                ["ENTER"] = "ENTER"
            };
            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        #endregion

        #region [DrawUI]

        private void DrawUI_Trade(BasePlayer player)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = true,
                        Image =
                        {
                            Color = "0 0 0 0"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    },
                    "Hud", "UI.BankSystem"
                }
            };

            ui.Add(new CuiButton
            {
                Button =
                {
                    Close = "UI.BankSystem",
                    Color = "0 0 0 0"
                },
                Text = {Text = " "},
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "UI.BankSystem");

            ui.Add(new CuiElement
            {
                Name = "UI.BankSystem.Trade",
                Parent = "UI.BankSystem",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage("UI.BankSystem.Trade")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.309179 0.2230871",
                        AnchorMax = "0.6908209 0.7769129"
                    }
                }
            });

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1.00 0.84 0.00 0.55",
                    FontSize = 16,
                    Text = $"x{GetMoneyCount(player)}"
                },
                RectTransform =
                {
                    AnchorMin = "0.3601163 0.5969682",
                    AnchorMax = "0.5962129 0.672204"
                }
            }, "UI.BankSystem.Trade");

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1.00 0.84 0.00 0.55",
                    FontSize = 16,
                    Text = $"x{GetBalance(player.userID)}"
                },
                RectTransform =
                {
                    AnchorMin = "0.3601163 0.06531323",
                    AnchorMax = "0.5962129 0.1405491"
                }
            }, "UI.BankSystem.Trade");

            CuiHelper.AddUi(player, ui);
            DrawUI_Input1(player);
            DrawUI_Input2(player);
        }

        private void DrawUI_Input1(BasePlayer player)
        {
            var ui = new CuiElementContainer();

            ui.Add(new CuiElement
            {
                Name = $"{UIParent}.Input1",
                Parent = "UI.BankSystem.Trade",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Command = "BankSystem.Trade +",
                        Align = TextAnchor.MiddleCenter,
                        Color = "0.39 0.40 0.43 1.00",
                        Text = lang.GetMessage("ENTER", this, player.UserIDString)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0742103 0.5969681",
                        AnchorMax = "0.3103082 0.6722033"
                    }
                }
            });

            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_Input2(BasePlayer player)
        {
            var ui = new CuiElementContainer();

            ui.Add(new CuiElement
            {
                Name = $"{UIParent}.Input2",
                Parent = "UI.BankSystem.Trade",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Command = "BankSystem.Trade -",
                        Align = TextAnchor.MiddleCenter,
                        Color = "0.39 0.40 0.43 1.00",
                        Text = lang.GetMessage("ENTER", this, player.UserIDString)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.6337423 0.06531322",
                        AnchorMax = "0.8698384 0.1405475"
                    }
                }
            });

            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_CardMoney(BasePlayer player)
        {
            var ui = new CuiElementContainer
            {
                new CuiElement
                {
                    Name = UIParent,
                    Parent = "Hud",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0.57 0.55 0.55 0.37"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0",
                            AnchorMax = "0.5 0",
                            OffsetMin = "185 18",
                            OffsetMax = "250 78"
                        }
                    }
                },
                new CuiElement
                {
                    Name = $"{UIParent}.IC",
                    Parent = UIParent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("UI.BankSystem.IC64")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.2179487 0.1944444",
                            AnchorMax = "0.7820513 0.8055556"
                        }
                    }
                }
            };

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Text = $"x{GetBalance(player.userID)}",
                    FontSize = 13,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.55"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 0.2777778"
                }
            }, UIParent);

            CuiHelper.DestroyUi(player, UIParent);
            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Hooks] / [Крюки]

        // ReSharper disable once UnusedMember.Local
        private object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null) return null;
            if (!_database.Clients.ContainsKey(player.userID)) return null;
            var db = _database.Clients[player.userID];
            if (db.Balance > 0)
                SetBalance(player.userID,
                    Convert.ToInt32((double) db.Balance / 100 * (100 - _config.BankSystem.DeathNALOG)));
            return null;
        }

        // ReSharper disable once UnusedMember.Local
        private bool CanUseVending(BasePlayer player, VendingMachine machine)
        {
            switch (machine.skinID)
            {
                case 2556:
                    DrawUI_Trade(player);
                    return false;
                default:
                    return true;
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            LoadData();
            ServerMgr.Instance.StartCoroutine(CheckAllInDataBase());
            ImageLibrary.Call("AddImage", "https://i.imgur.com/aL9Ii8o.png", "UI.BankSystem.Trade");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/qPiXX09.png", "UI.BankSystem.IC64");
            ServerMgr.Instance.StartCoroutine(LoadHud());
            SpawnNpc(pos1, rot1, 1);
            SpawnNpc(pos2, rot2, 2);
            Box1 = GameManager.server.CreateEntity(PREFAB_BOX, BoxPos1, BoxRot1);
            Box1.skinID = 4001;
            Box1.Spawn();
            if (_database.BalanceOCG > 1)
                ItemManager.CreateByName("ducttape", _database.BalanceOCG, 2808484201)
                    .MoveToContainer(Box1.GetComponent<StorageContainer>().inventory);
            Box2 = GameManager.server.CreateEntity(PREFAB_BOX, BoxPos2, BoxRot2);
            Box2.skinID = 4002;
            Box2.Spawn();
            if (_database.BalanceWSARMY > 1)
                ItemManager.CreateByName("ducttape", _database.BalanceWSARMY, 2808484201)
                    .MoveToContainer(Box2.GetComponent<StorageContainer>().inventory);

            CodeLock(Box1);
            CodeLock(Box2);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            switch (entity.skinID)
            {
                case 4001:
                {
                    _database.BalanceOCG = 0;
                    break;
                }
                case 4002:
                {
                    _database.BalanceWSARMY = 0;
                    break;
                }
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerConnected(BasePlayer player)
        {
            CheckInDataBase(player.userID);
            DrawUI_CardMoney(player);
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            SaveData();
            ServerMgr.Instance.StartCoroutine(UnloadHud());
            _vendingMachine1.Kill();
            _vendingMachine2.Kill();
            if (Box1 != null) Box1.Kill();
            if (Box2 != null) Box2.Kill();
        }

        private void CodeLock(BaseEntity box)
        {
            var codeLock = GameManager.server.CreateEntity(PREFAB_CODE_LOCK) as CodeLock;
            if (codeLock == null) return;
            codeLock.SetParent(box, box.GetSlotAnchorName(BaseEntity.Slot.Lock));
            codeLock.OwnerID = box.OwnerID;
            codeLock.Spawn();
            codeLock.SetFlag(BaseEntity.Flags.Locked, true);
            codeLock.code = Random.Range(1000, 10000).ToString();
            codeLock.SetFlag(BaseEntity.Flags.Locked, true);
            box.SetSlot(BaseEntity.Slot.Lock, codeLock);
        }

        #endregion

        #region [ConsoleCommands]

        [ConsoleCommand("banksystem.givebalance")]
        // ReSharper disable once UnusedMember.Local
        private void BankSystem_GiveBalance(ConsoleSystem.Arg args)
        {
            if (args.Player() != null) return;
            if (args.Args.Length <= 1)
            {
                PrintWarning("Error, information entered incorrectly");
                return;
            }

            var player = Convert.ToUInt64(args.Args[0]);
            if (!_database.Clients.ContainsKey(player))
            {
                PrintWarning($"Player [{player}] not found, check ID");
                return;
            }

            var money = args.Args[1].ToInt();
            GiveBalance(player, money);
        }

        [ConsoleCommand("banksystem.trade")]
        // ReSharper disable once UnusedMember.Local
        private void BankSystem_Trade(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (args.Args[0] == "+")
            {
                if (player.inventory.FindItemID("ducttape") == null) return;
                var item = player.inventory.FindItemID("ducttape");
                if (args.Args.Length < 2)
                {
                    player.ChatMessage(lang.GetMessage("ENTERCOINS", this, player.UserIDString));
                    return;
                }

                if (!args.Args[1].IsNumeric())
                {
                    player.ChatMessage(lang.GetMessage("ENTERNUMBER", this, player.UserIDString));
                    return;
                }

                if ((player.inventory.FindItemID("ducttape").amount > 0 &
                     player.inventory.FindItemID("ducttape").skin == 2808484201))
                {
                    var count = args.Args[1].ToInt();

                    player.ChatMessage(count.ToString());
                    if (count <= item.amount & count != 0)
                    {
                        GiveBalance(player.userID, count);
                        var amount = item.amount - count;
                        item.DoRemove();
                        if (amount > 0)
                        {
                            var newitem = ItemManager.CreateByName("ducttape", amount, 2808484201);
                            newitem.name = "SHREK";
                            player.GiveItem(newitem);
                        }

                        player.ChatMessage(lang.GetMessage("ADDBALANCE", this, player.UserIDString));
                        CuiHelper.DestroyUi(player, "UI.BankSystem");
                    }
                    else
                    {
                        player.ChatMessage(lang.GetMessage("NOMONEY", this, player.UserIDString));
                    }
                }
            }
            else
            {
                if (!args.Args[1].IsNumeric())
                {
                    player.ChatMessage(lang.GetMessage("ENTERNUMBER", this, player.UserIDString));
                    return;
                }

                var balance = GetBalance(player.userID);
                if (balance > 0)
                {
                    var count = args.Args[1].ToInt();
                    var item = ItemManager.CreateByName("ducttape", count);
                    item.name = "Shrek";
                    item.skin = 2808484201;
                    player.GiveItem(item);
                    TakeBalance(player.userID, count);
                }
                else
                {
                    player.ChatMessage(
                        lang.GetMessage("BALANCE", this, player.UserIDString).Replace("{1}", $"{balance}"));
                }
            }

            CuiHelper.DestroyUi(player, "UI.BankSystem");
        }

        [ConsoleCommand("banksystem.setbalance")]
        // ReSharper disable once UnusedMember.Local
        private void BankSystem_SetBalance(ConsoleSystem.Arg args)
        {
            if (args.Player() != null) return;
            if (args.Args.Length <= 1)
            {
                PrintWarning($"Error, information entered incorrectly");
                return;
            }

            var player = Convert.ToUInt64(args.Args[0]);
            if (!_database.Clients.ContainsKey(player))
            {
                PrintWarning($"Player [{player}] not found, check ID");
                return;
            }

            var money = args.Args[1].ToInt();
            SetBalance(player, money);
        }

        [ConsoleCommand("banksystem.takebalance")]
        // ReSharper disable once UnusedMember.Local
        private void BankSystem_TakeBalance(ConsoleSystem.Arg args)
        {
            if (args.Player() != null) return;
            if (args.Args.Length <= 1)
            {
                PrintWarning($"Error, information entered incorrectly");
                return;
            }

            var player = Convert.ToUInt64(args.Args[0]);
            if (!_database.Clients.ContainsKey(player))
            {
                PrintWarning($"Player [{player}] not found, check ID");
                return;
            }

            var money = args.Args[1].ToInt();
            TakeBalance(player, money);
        }

        [ConsoleCommand("banksystem.givemoney")]
        // ReSharper disable once UnusedMember.Local
        private void BankSystem_GiveMoney(ConsoleSystem.Arg args)
        {
            if (args.Player() != null) return;
            if (args.Args.Length <= 1)
            {
                PrintWarning("Error, information entered incorrectly");
                return;
            }

            var player = Convert.ToUInt64(args.Args[0]);
            if (!_database.Clients.ContainsKey(player))
            {
                PrintWarning($"Player [{player}] not found, check ID");
                return;
            }

            var count = args.Args[1].ToInt();
            var item = ItemManager.CreateByName("ducttape", count);
            item.name = "SHREK";
            item.skin = 2808484201;
            BasePlayer.FindByID(player).GiveItem(item);
        }

        // ReSharper disable once UnusedMember.Local
        private void SpawnNpc(Vector3 pos, Quaternion rot, int num)
        {
            var npc = GameManager.server.CreateEntity(
                "assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab",
                pos) as VendingMachine;
            npc.shopName = "BankMachine";
            npc.skinID = 2556;
            npc.transform.rotation = rot;
            npc.Spawn();
            npc.SendNetworkUpdate();

            switch (num)
            {
                case 1:
                    _vendingMachine1 = npc;
                    break;
                case 2:
                    _vendingMachine2 = npc;
                    break;
            }
        }

        #endregion

        #region [DataBase]

        private StoredData _database = new StoredData();

        private class StoredData
        {
            public Dictionary<ulong, BankClient> Clients = new Dictionary<ulong, BankClient>();

            public class BankClient
            {
                public int Balance;
            }

            public int BalanceOCG;
            public int BalanceWSARMY;
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _database);

        private void LoadData()
        {
            try
            {
                _database = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception)
            {
                _database = new StoredData();
            }
        }

        #endregion

        #region [Helpers]

        private IEnumerator LoadHud()
        {
            foreach (var player in BasePlayer.activePlayerList) DrawUI_CardMoney(player);
            yield return 0;
        }

        private IEnumerator UnloadHud()
        {
            foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, "UI.BankSystem.Hud");
            yield return 0;
        }

        private int GetMoneyCount(BasePlayer player)
        {
            return player.inventory.AllItems()
                .Where(item => item.info.shortname == "ducttape" & item.skin == 2808484201).Sum(item => item.amount);
        }

        private string GetImage(string name)
        {
            return (string) ImageLibrary?.Call("GetImage", name);
        }

        private void CheckInDataBase(ulong player)
        {
            if (_database.Clients.ContainsKey(player)) return;
            _database.Clients.Add(player, new StoredData.BankClient());
        }

        private IEnumerator CheckAllInDataBase()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CheckInDataBase(player.userID);
                yield return Wait;
            }

            yield return 0;
        }

        #endregion

        #region [API]

        private void GiveBalance(ulong player, int money)
        {
            CheckInDataBase(player);
            _database.Clients[player].Balance += money;
            if (BasePlayer.FindByID(player) != null)
            {
                var obj = BasePlayer.FindByID(player);
                DrawUI_CardMoney(obj);
                switch (obj.Team.teamName)
                {
                    case "OCGBirch":
                        _database.BalanceOCG += (money / 100) * 20;
                        ItemManager.CreateByName("ducttape", _database.BalanceOCG, 2808484201)
                            .MoveToContainer(Box1.GetComponent<StorageContainer>().inventory);
                        break;
                    case "WSARMY":
                        _database.BalanceOCG += (money / 100) * 20;
                        ItemManager.CreateByName("ducttape", _database.BalanceOCG, 2808484201)
                            .MoveToContainer(Box2.GetComponent<StorageContainer>().inventory);
                        break;
                }
            }
        }

        private void SetBalance(ulong player, int money)
        {
            CheckInDataBase(player);
            _database.Clients[player].Balance = money;
            if (_database.Clients[player].Balance <= 0) _database.Clients[player].Balance = 0;
            if (BasePlayer.FindByID(player) != null) DrawUI_CardMoney(BasePlayer.FindByID(player));
        }

        private void TakeBalance(ulong player, int money)
        {
            CheckInDataBase(player);
            _database.Clients[player].Balance -= money;
            if (_database.Clients[player].Balance <= 0) _database.Clients[player].Balance = 0;
            if (BasePlayer.FindByID(player) != null)
            {
                var obj = BasePlayer.FindByID(player);
                DrawUI_CardMoney(obj);
                switch (obj.Team.teamName)
                {
                    case "OCGBirch":
                        _database.BalanceOCG += (money / 100) * 20;
                        ItemManager.CreateByName("ducttape", _database.BalanceOCG, 2808484201)
                            .MoveToContainer(Box1.GetComponent<StorageContainer>().inventory);
                        break;
                    case "WSARMY":
                        _database.BalanceOCG += (money / 100) * 20;
                        ItemManager.CreateByName("ducttape", _database.BalanceOCG, 2808484201)
                            .MoveToContainer(Box2.GetComponent<StorageContainer>().inventory);
                        break;
                }
            }
        }

        private int GetBalance(ulong player)
        {
            var balance = 0;
            if (_database.Clients.ContainsKey(player)) balance = _database.Clients[player].Balance;
            return balance;
        }

        #endregion
    }
}