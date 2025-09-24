using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Steamworks;
using UnityEngine;
using Random = UnityEngine.Random;
using System.IO;
using Facepunch.Extend;
using UnityEngine.UI;

namespace Oxide.Plugins;

[Info("CaseSystem", "Forum: https://topplugin.ru Ds: alone_sempai Vk: https://vk.com/rustnastroika", "1.0.5")]
internal class CaseSystem : RustPlugin
{
    [PluginReference] private Plugin ServerRewards, Economics, IQEconomic, BankSystem;
    private Configuration _config;
    private readonly Dictionary<string, int> _shortNameToItemID = new();

    private const bool isEn = false;
    
    #region Config

    private class Configuration
    {

        [JsonProperty(PropertyName = isEn ? "Should I use the item issue limit?" : "Использовать лимит выдачи предметов?", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public bool isLimitReward = true;
        [JsonProperty(PropertyName = isEn ? "Setting up privileges for the item issue limit" : "Настройка привилегий для лимита выдачи предметов", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<string, int> PermList = new()
        {
            ["casesystem.default"] = 5,
            ["casesystem.vip"] = 7,
        };
        [JsonProperty(PropertyName =
            isEn
                ? "Economics Plugin(1 - Economics, 2 - Server Rewards, 3 - IQEconomic, 4 - Bank System, 5 - Cases)"
                : "Плагин экономики(1 - Economics, 2 - ServerRewards, 3 - IQEconomic, 4 - BankSystem, 5 - Cases)")]
        public int EconomyPlugin = 5;
        [JsonProperty(PropertyName = isEn ? "Color adjustment(chance-color)" : "Настройка цветов(шанс-цвет)",
            ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<int, string> ColorList = new()
        {
            [15] = "0.96 0.76 0.37 1.00",
            [40] = "1.00 0.29 0.30 1.00",
            [80] = "0.68 0.25 1.00 1.00",
            [100] = "0.24 0.59 1.00 1.00",
        };

        [JsonProperty(PropertyName =  isEn ? "List of awards" : "Список наград", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<ItemSettings> RewardList = new List<ItemSettings>()
        {
            new ItemSettings()
            {
                RewardID = "1",
                Amount = 1,
                Chance = 2,
                Command = "",
                Shortname = "minigun",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "2",
                Amount = 1,
                Chance = 5,
                Command = "",
                Shortname = "lmg.m249",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "3",
                Amount = 1,
                Chance = 10,
                Command = "",
                Shortname = "hmlmg",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "4",
                Amount = 1,
                Chance = 15,
                Command = "",
                Shortname = "rifle.l96",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "5",
                Amount = 1,
                Chance = 35,
                Command = "",
                Shortname = "rifle.lr300",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "6",
                Amount = 1,
                Chance = 35,
                Command = "",
                Shortname = "rifle.ak",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "7",
                Amount = 1,
                Chance = 30,
                Command = "",
                Shortname = "rifle.sks",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "8",
                Amount = 1,
                Chance = 30,
                Command = "",
                Shortname = "rifle.bolt",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "9",
                Amount = 1,
                Chance = 50,
                Command = "",
                Shortname = "smg.mp5",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "10",
                Amount = 1,
                Chance = 55,
                Command = "",
                Shortname = "rifle.semiauto",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "11",
                Amount = 1,
                Chance = 60,
                Command = "",
                Shortname = "smg.thompson",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "12",
                Amount = 1,
                Chance = 70,
                Command = "",
                Shortname = "pistol.m92",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "13",
                Amount = 1,
                Chance = 70,
                Command = "",
                Shortname = "pistol.prototype17",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "14",
                Amount = 1,
                Chance = 75,
                Command = "",
                Shortname = "pistol.semiauto",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "15",
                Amount = 1,
                Chance = 80,
                Command = "",
                Shortname = "pistol.python",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "16",
                Amount = 1,
                Chance = 95,
                Command = "",
                Shortname = "pistol.revolver",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "17",
                Amount = 1,
                Chance = 5,
                Command = "",
                Shortname = "electric.generator.small",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "18",
                Amount = 1,
                Chance = 10,
                Command = "",
                Shortname = "workbench3",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "19",
                Amount = 1,
                Chance = 10,
                Command = "",
                Shortname = "workbench2",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "20",
                Amount = 1,
                Chance = 15,
                Command = "",
                Shortname = "autoturret",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "21",
                Amount = 1,
                Chance = 25,
                Command = "",
                Shortname = "door.double.hinged.toptier",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "22",
                Amount = 1,
                Chance = 25,
                Command = "",
                Shortname = "door.hinged.toptier",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "23",
                Amount = 1,
                Chance = 35,
                Command = "",
                Shortname = "furnace.large",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "24",
                Amount = 1,
                Chance = 35,
                Command = "",
                Shortname = "small.oil.refinery",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "25",
                Amount = 1,
                Chance = 45,
                Command = "",
                Shortname = "water.catcher.small",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "26",
                Amount = 1,
                Chance = 45,
                Command = "",
                Shortname = "storage_barrel_b",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "27",
                Amount = 1,
                Chance = 45,
                Command = "",
                Shortname = "storage_barrel_c",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "28",
                Amount = 1,
                Chance = 45,
                Command = "",
                Shortname = "gunrack_stand",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "29",
                Amount = 1,
                Chance = 55,
                Command = "",
                Shortname = "gunrack_wide.horizontal",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "30",
                Amount = 1,
                Chance = 65,
                Command = "",
                Shortname = "flameturret",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "31",
                Amount = 1,
                Chance = 75,
                Command = "",
                Shortname = "furnace",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "32",
                Amount = 1,
                Chance = 75,
                Command = "",
                Shortname = "cupboard.tool.retro",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "33",
                Amount = 1,
                Chance = 10,
                Command = "",
                Shortname = "rocket.launcher",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "34",
                Amount = 1,
                Chance = 15,
                Command = "",
                Shortname = "ammo.rocket.basic",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "35",
                Amount = 1,
                Chance = 15,
                Command = "",
                Shortname = "ammo.rocket.mlrs",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "36",
                Amount = 1,
                Chance = 25,
                Command = "",
                Shortname = "ammo.rocket.hv",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "37",
                Amount = 1,
                Chance = 30,
                Command = "",
                Shortname = "ammo.grenadelauncher.he",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "38",
                Amount = 1,
                Chance = 35,
                Command = "",
                Shortname = "multiplegrenadelauncher",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "39",
                Amount = 1,
                Chance = 50,
                Command = "",
                Shortname = "ammo.rifle.explosive",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "40",
                Amount = 1,
                Chance = 60,
                Command = "",
                Shortname = "explosive.satchel",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "41",
                Amount = 1,
                Chance = 70,
                Command = "",
                Shortname = "grenade.beancan",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "42",
                Amount = 1,
                Chance = 5,
                Command = "",
                Shortname = "rocket.launcher.dragon",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "43",
                Amount = 1,
                Chance = 10,
                Command = "",
                Shortname = "coffin.storage",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "44",
                Amount = 1,
                Chance = 15,
                Command = "",
                Shortname = "legacyfurnace",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "45",
                Amount = 1,
                Chance = 25,
                Command = "",
                Shortname = "hobobarrel",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "46",
                Amount = 1,
                Chance = 35,
                Command = "",
                Shortname = "heavyscientistyoutooz",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "47",
                Amount = 1,
                Chance = 35,
                Command = "",
                Shortname = "hazmatyoutooz",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "48",
                Amount = 1,
                Chance = 60,
                Command = "",
                Shortname = "cursedcauldron",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "49",
                Amount = 1,
                Chance = 75,
                Command = "",
                Shortname = "abovegroundpool",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "50",
                Amount = 1,
                Chance = 80,
                Command = "",
                Shortname = "rail.road.planter",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "51",
                Amount = 1,
                Chance = 85,
                Command = "",
                Shortname = "wall.graveyard.fence",
                ItemImage = "",
                SkinID = 0
            },
            new ItemSettings()
            {
                RewardID = "52",
                Amount = 1,
                Chance = 95,
                Command = "",
                Shortname = "newyeargong",
                ItemImage = "",
                SkinID = 0
            }
        };
        [JsonProperty(PropertyName = isEn ? "Cases" : "Список кейсов", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<Case> CaseList = new()
        {
            new Case()
            {
                Id = "1",
                DisplayName = "Armory",
                Image = "https://unityplugincore.ru/imgs/images/2024/09/23/ORUZEINYI.png",
                Price = 100
            },
            new Case()
            {
                Id = "2",
                DisplayName = "Home",
                Image = "https://unityplugincore.ru/imgs/images/2024/09/23/DOMASNII.png",
              
                Price = 125
            },
            new Case()
            {
                Id = "3",
                DisplayName = "Raider",
                Image = "https://unityplugincore.ru/imgs/images/2024/09/23/REIDERSKII.png",
               
                Price = 150
            },
            new Case()
            {
                Id = "4",
                DisplayName = "DLC",
                Image = "https://unityplugincore.ru/imgs/images/2024/09/23/DLS.png",

                Price = 150
            }
        };

        internal class Case
        {
            [JsonProperty(PropertyName = isEn ? "Case ID" : "ID кейса", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string Id = "1";

            [JsonProperty(PropertyName = isEn ? "Case name" : "Название кейса", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string DisplayName = "New case";

            [JsonProperty(PropertyName = isEn ? "Image of the case" : "Картинка кейса", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string Image;

            [JsonProperty(PropertyName = isEn ? "Discription of the case" : "Описание кейса", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string Description = "There will be a description of your case here";

            [JsonProperty(PropertyName = isEn ? "Price of the case" : "Стоимость кейса", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public int Price = 100;

            [JsonProperty(PropertyName = isEn ? "List the case items" : "Список предметом (Указать только ID предмета из списка наград",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> ItemList = new List<string>()
            {
                "1",
                "2",
                "3",
                "4",
                "5",
                "6",
                "7",
                "8",
                "9",
                "10",
                "11",
                "12",
                "13",
                "14",
                "15",
                "16",
            };

        }

        internal class ItemSettings
        {
            [JsonProperty(PropertyName = isEn ? "Item ID (Indicated in the list of items in the case)" : "ID предмета (Указывается в списке предметов кейса)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string RewardID = Random.Range(0, 100000).ToString();
            [JsonProperty(PropertyName = isEn ? "Shortname" : "Shortname", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string Shortname = "scrap";

            [JsonProperty(PropertyName = isEn ? "Amount" : "Кол-во", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public int Amount = 1;

            [JsonProperty(PropertyName = isEn ? "Drop chance" : "Шанс выпадения", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public int Chance = 20;

            [JsonProperty(PropertyName = isEn ? "Name the item" : "Имя предмета", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string displayName = "";

            [JsonProperty(PropertyName = isEn ? "Price the item" : "Стоимость предмета", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public int Price;

            [JsonProperty(PropertyName = isEn ? "URL image the item" : "Картинка кастомного предмета", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string ItemImage;

            [JsonProperty(PropertyName = isEn ? "SkinID " : "SkinID ", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public ulong SkinID = 0;

            [JsonProperty(PropertyName = isEn ? "Console command" : "Команда которая должна выполниться",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string Command = "o.grant user %STEAMID% vip";
            
            [JsonIgnore] private ICuiComponent _image;

            public CuiElement GetImageNonCached(string aMin, string aMax, string oMin, string oMax, string parent,
                string name = null, float fadein = 0, bool update = false, float fadeOut = 0f)
            {
                    ICuiComponent _image = null;
                    if (!string.IsNullOrEmpty(ItemImage))
                        _image = new CuiRawImageComponent
                        {
                            Png = GuiManager.Get(ItemImage), FadeIn = fadein
                        };
                    else
                    {
                        var def = ItemManager.FindItemDefinition(Shortname);
                        if (def == null)
                        {
                            Debug.LogError($"[CaseSystem] Shortname {Shortname} is invalid!");
                            return new();
                        }
                        _image = new CuiImageComponent
                        {
                            ItemId = ItemManager.FindItemDefinition(Shortname).itemid,
                            SkinId = SkinID,
                            FadeIn = fadein
                        };
                    }

                return new CuiElement
                {
                    Name = string.IsNullOrEmpty(name) ? CuiHelper.GetGuid() : name,
                    Parent = parent,
                    FadeOut = fadeOut,
                    Update = update,
                    Components =
                    {
                        _image,
                        new CuiRectTransformComponent
                        {
                            AnchorMin = aMin, AnchorMax = aMax,
                            OffsetMin = oMin, OffsetMax = oMax
                        }
                    }
                };
            }
            
            public CuiElement GetImage(string aMin, string aMax, string oMin, string oMax, string parent,
                string name = null)
            {
                if (_image == null)
                {
                    if (!string.IsNullOrEmpty(ItemImage))
                        _image = new CuiRawImageComponent
                        {
                            Png = GuiManager.Get(ItemImage)
                        };
                    else
                    {
                        var def = ItemManager.FindItemDefinition(Shortname);
                        if (def == null)
                        {
                            Debug.LogError($"[CaseSystem] Shortname {Shortname} is invalid!");
                            return new();
                        }
                        _image = new CuiImageComponent
                        {
                            ItemId = ItemManager.FindItemDefinition(Shortname).itemid,
                            SkinId = SkinID,
                        };
                    }
                }

                return new CuiElement
                {
                    Name = string.IsNullOrEmpty(name) ? CuiHelper.GetGuid() : name,
                    Parent = parent,
                    Components =
                    {
                        _image,
                        new CuiRectTransformComponent
                        {
                            AnchorMin = aMin, AnchorMax = aMax,
                            OffsetMin = oMin, OffsetMax = oMax
                        }
                    }
                };
            }
        }
    }

    protected override void LoadConfig()
    {
        base.LoadConfig();
        try
        {
            _config = Config.ReadObject<Configuration>();
            if (_config == null) throw new Exception();
            SaveConfig();
        }
        catch
        {
            PrintError("Your configuration file contains an error. Using default configuration values.");
            LoadDefaultConfig();
        }
    }

    protected override void SaveConfig()
    {
        Config.WriteObject(_config);
    }

    protected override void LoadDefaultConfig()
    {
        _config = new Configuration();
    }

    #endregion

    #region Data

    private Dictionary<ulong, Data> _data;

    private class Data
    {
        public DateTime LastReset = DateTime.MinValue;
        public int AmountToUse = 0;
        public int Balance = 0;
        public Dictionary<string, int> CaseList = new();
        public List<string> Inventory = new();

        public bool FastOpenCase = false;
        public bool IsOpeningCase() => OpeningRoutine != null;
        [JsonIgnore] public Coroutine OpeningRoutine;
    }

    private void LoadData()
    {
        if (!Interface.Oxide.DataFileSystem.ExistsDatafile($"MM_Data/{Name}/PlayerData"))
            _data = new Dictionary<ulong, Data>();
        else
            _data = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Data>>(
                $"MM_Data/{Name}/PlayerData");
        Interface.Oxide.DataFileSystem.WriteObject($"MM_Data/{Name}/PlayerData", _data);

        if (_data == null)
            _data = new Dictionary<ulong, Data>();


        foreach (var check in _data.ToArray())
        {
            if (check.Value.Inventory.Count == 0 && check.Value.CaseList.Count == 0 && check.Value.Balance == 0)
            {
                _data.Remove(check.Key);
            }
        }

        foreach (var check in BasePlayer.activePlayerList) OnPlayerConnected(check);
        
        
        SaveData();
    }

    private void Init()
    {
        LoadData();
    }

    private void OnServerSave()
    {
        SaveData();
    }

    private void SaveData()
    {
        Interface.Oxide.DataFileSystem.WriteObject($"MM_Data/{Name}/PlayerData", _data);
    }


    private void OnPlayerConnected(BasePlayer player)
    {
        if (!_data.ContainsKey(player.userID))
            _data.Add(player.userID, new Data());
        var amount = GetAmount(player);
        if (_data[player.userID].LastReset.Date != DateTime.Now.Date &&amount > 0)
        {
            _data[player.userID].LastReset = DateTime.Now.Date;
            _data[player.userID].AmountToUse = amount;

        }
    }

    private int GetAmount(BasePlayer player)
    {
        int number = 0;
        foreach(var check in _config.PermList)
            if (permission.UserHasPermission(player.UserIDString, check.Key))
                number = Math.Max(number, check.Value);
        return number;
    }

    #endregion

    #region OxideHooks

    private void OnSectionChanged(BasePlayer player, string section)
    {
        if (_data.TryGetValue(player.userID, out var data))
            if (data.OpeningRoutine != null)
            {
                player.StopCoroutine(data.OpeningRoutine);
                data.OpeningRoutine = null;
            }
    }
    private Dictionary<string, Configuration.ItemSettings> RewardsCache = new();
    private void OnServerInitialized()
    {
        foreach (var check in ItemManager.itemList.OrderBy(p => p.category.ToString()))
        {
            _shortNameToItemID.Add(check.shortname, check.itemid);
        }
        foreach (var check in _config.PermList)
        {
            permission.RegisterPermission(check.Key, this);
        }
        // foreach (var check in _config.CaseList)
        // {
        //     if (!string.IsNullOrEmpty(check.Image))
        //     {
        //         ImageSettingsList.Add(new ImageSettings()
        //         {
        //             Name = check.Image,
        //             Url = check.Image,
        //         });
        //     }
        //     
        //    
        // }
        foreach (var x in _config.RewardList)
        {
            if (!RewardsCache.ContainsKey(x.RewardID))
                RewardsCache.Add(x.RewardID, x);
        }
        List<string> images = new();
        
        images.Add("item_background");
        
        foreach (var x in _config.CaseList)
            if (!string.IsNullOrEmpty(x.Image))
                images.Add(x.Image);
        foreach (var x in _config.RewardList)
            if (!string.IsNullOrEmpty(x.ItemImage))
                images.Add(x.ItemImage);
        
        
        GuiManager.LoadImages(images);
        // foreach (var item in _config.RewardList)
        // {
        //     if (!string.IsNullOrEmpty(item.ItemImage))
        //     {
        //         ImageSettingsList.Add(new ImageSettings()
        //         {
        //             Name = item.ItemImage,
        //             Url = item.ItemImage,
        //         });
        //     }
        // }
        SaveConfig();
        // DownloadImage();
        PrintWarning($"|  Plugin {Title} v{Version} is loaded  |");
    }

    // [ChatCommand("cases")]
    private void CmdChatcases(BasePlayer player, string command, string[] args)
    {
        ShowMainUI(player, 0);
    }

    private void Unload()
    {
        foreach (var x in BasePlayer.activePlayerList)
        {
            if (_data.TryGetValue(x.userID, out var data))
                if (data.OpeningRoutine != null)
                    x.StopCoroutine(data.OpeningRoutine);
        }
        
        GuiManager.Clear();
        SaveData();
    }

    #endregion

    #region Function

    [ConsoleCommand("UI_CASESS")]
    private void CmdConsoleUI_CASES(ConsoleSystem.Arg arg)
    {
        var player = arg.Player();

        switch (arg.Args[0])
        {
            case "SELL":
            {
                var id = arg.Args[1];
                var posId = arg.Args[2].ToInt();
                if(!_data[player.userID].Inventory.Contains(id))
                    return;
                var reward = _config.RewardList.FirstOrDefault(p => p.RewardID == id);
                if (reward == null)
                {
                    PrintWarning(isEn? $"The player has a reward {id}, but it is not in the config" : $"У игрока есть награда {id}, но в конфиге ее нет");
                    return;
                }
                EffectNetwork.Send(new("assets/prefabs/deployable/vendingmachine/effects/vending-machine-purchase-human.prefab", player, 0, new(), new()), player.Connection);
                _data[player.userID].Inventory.Remove(id);
                GiveBalance(player, reward.Price);

                if (posId != -2)
                {
                    UI_UpdateInventoryItems(player, posId);
                    UI_UpdateTargetedInventoryItem(player, _data[player.userID].Inventory.FirstOrDefault(), -1);
                }
                else
                {
                    if (int.TryParse(arg.Args.ElementAtOrDefault(3), out var index))
                    {
                        CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".sell" + $".{index}");
                        CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".buy" + $".{index}");
                    }
                    else
                    {
                        CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".sell" + $".1");
                        CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".buy" + $".1");
                    }
                }
                if (bool.TryParse(arg.Args.ElementAtOrDefault(3), out var returnToCaseItems))
                {
                    if (returnToCaseItems)
                    {
                        UI_DrawCaseItems(arg.Player(), _config.CaseList.FirstOrDefault(x => x.Id == arg.Args[4]));
                        UI_UpdateCaseOpenAmount(player, arg.Args[4], 1);
                        UI_DrawFastOpenMarker(player, arg.Args[4]);
                    }
                }
                break;
            }
            case "SKIP":
            {
                if (int.TryParse(arg.Args.ElementAtOrDefault(3), out var index))
                {
                    CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".sell" + $".{index}");
                    CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".buy" + $".{index}");
                }
                else
                {
                    CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".sell" + $".1");
                    CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".buy" + $".1");
                }

                if (bool.TryParse(arg.Args.ElementAtOrDefault(3), out var returnToCaseItems))
                {
                    if (returnToCaseItems)
                    {
                        UI_DrawCaseItems(arg.Player(), _config.CaseList.FirstOrDefault(x => x.Id == arg.Args[4]));
                        UI_UpdateCaseOpenAmount(player, arg.Args[4], 1);
                        UI_DrawFastOpenMarker(player, arg.Args[4]);
                    }
                }

                break;
            }
            case "TAKEREWARD":
            {
                if (IsRaidOrCombatBlocked(player))
                    return;
                
                var id = arg.Args[1];
                var itemId = arg.Args[2].ToInt();
                if(!_data[player.userID].Inventory.Contains(id))
                    return;
              
                var reward = _config.RewardList.FirstOrDefault(p => p.RewardID == id);
                if (reward == null)
                {
                        PrintWarning(isEn ? $"The player has a reward {id}, but it is not in the config" : $"У игрока есть награда {id}, но в конфиге ее нет");
                        return;
                }
                _data[player.userID].Inventory.Remove(id);
                var item = ItemManager.CreateByName(reward.Shortname, reward.Amount, reward.SkinID);
                if (item != null)
                {
                    player.GiveItem(item);
                }
                if(!string.IsNullOrEmpty(reward.Command))
                    rust.RunServerCommand(reward.Command.Replace("%STEAMID%", player.UserIDString));
                
                if (itemId != -2)
                {
                    UI_UpdateInventoryItems(player, itemId);
                    UI_UpdateTargetedInventoryItem(player, _data[player.userID].Inventory.FirstOrDefault(), -1);
                }
                else
                {
                    if (int.TryParse(arg.Args.ElementAtOrDefault(3), out var index))
                    {
                        CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".sell" + $".{index}");
                        CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".buy" + $".{index}");
                    }
                    else
                    {
                        CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".sell" + $".1");
                        CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".buy" + $".1");
                    }
                }
                break;
            }
        }
    }

    private double GetBalance(BasePlayer player)
    {
        return _config.EconomyPlugin switch
        {
            1 => Economics.Call<double>("Balance", player.userID.Get()),
            2 => ServerRewards.Call<int>("CheckPoints", player.userID.Get()),
            3 => IQEconomic.Call<int>("API_GET_BALANCE", player.userID.Get()),
            4 => BankSystem.Call<int>("Balance", player.userID.Get()),
            5 => _data[player.userID].Balance,
            _ => double.MinValue
        };
    }
    private bool IsRaidOrCombatBlocked(BasePlayer player)
    {
        var combatBlock = plugins.Find("CombatBlock");
        if (combatBlock)
            return (bool)combatBlock.Call("IsCombatBlocked", player) || (bool)combatBlock.Call("IsRaidBlocked", player, false);
			
        return false;
    }
    private void TakeBalance(BasePlayer player, double amount)
    {
        IQEconomic?.Call("API_REMOVE_BALANCE", player.userID.Get(), Convert.ToInt32(amount));
    }
    
    private void GiveBalance(BasePlayer player, double amount)
    {
        IQEconomic?.Call("API_SET_BALANCE", player.userID.Get(), Convert.ToInt32(amount));
    }
    private void UpdateBalance(BasePlayer player)
    {
        var container = new CuiElementContainer();
        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0.5215687 0.4470588 0.1843137 0.6" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-402.321 -15", OffsetMax = "-282.321 15" }
        }, "Topline_Place", "Balance_Panel");

        container.Add(new CuiElement
        {
            Name = "Balance_Text",
            Parent = "Balance_Panel",
            Components =
            {
                new CuiTextComponent
                {
                    Text = lang.GetMessage("UI_Balace", this, player.UserIDString) + $" $ {GetBalance(player)}", Font = "robotocondensed-bold.ttf",
                    FontSize = 14,
                    Align = TextAnchor.MiddleCenter, Color = "0.9490196 0.6745098 0.1411765 1"
                },

                new CuiRectTransformComponent
                    { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-60 -15", OffsetMax = "60 15" }
            }
        });
        CuiHelper.DestroyUi(player, "Balance_Panel");
        CuiHelper.AddUi(player, container);
        
    }
    private void ShowCaseInfo(BasePlayer player, Configuration.Case caseSettings, int buyAmount = 1, int openAmount = 1)
    {
        var container = new CuiElementContainer();
        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0 0 0 0" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-407.5 -200", OffsetMax = "407.5 185" }
        }, "Case_MainPlace", "Body_Place");
      
        container.Add(new CuiButton
        {
            Button = { Color = "0.3294118 0.5215687 0.1843137 0.6", Command = "chat.say /cases" },
            Text =
            {
                Text = lang.GetMessage("CasePage", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 14,
                Align = TextAnchor.MiddleCenter, Color = "0.5372549 0.7921569 0.3372549 1"
            },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "154.29 -15", OffsetMax = "234.29 15" }
        }, "Topline_Place", "CasePage_Button");
        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0 0 0 0.7" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400.75 3", OffsetMax = "400.75 188" }
        }, "Body_Place", "Case_TopPlace");

        container.Add(new CuiElement
        {
            Name = "Case_Image",
            Parent = "Case_TopPlace",
            Components =
            {
                new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageList[caseSettings.Image] },
                new CuiRectTransformComponent
                    { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-72.5 -62.5", OffsetMax = "72.5 82.5" }
            }
        });

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0 0 0 0.6" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-387.777 -50", OffsetMax = "-87.777 50" }
        }, "Case_TopPlace", "Panel_Discription");

        container.Add(new CuiElement
        {
            Name = "Label_CaseName",
            Parent = "Panel_Discription",
            Components =
            {
                new CuiTextComponent
                {
                    Text = caseSettings.DisplayName, Font = "robotocondensed-bold.ttf", FontSize = 14,
                    Align = TextAnchor.MiddleLeft, Color = "1 1 1 1"
                },

                new CuiRectTransformComponent
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-145.7 25.141",
                    OffsetMax = "-45.7 45.141"
                }
            }
        });

        container.Add(new CuiElement
        {
            Name = "Label_Discription",
            Parent = "Panel_Discription",
            Components =
            {
                new CuiTextComponent
                {
                    Text = caseSettings.Description,
                    Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleLeft,
                    Color = "0.9056604 0.9056604 0.9056604 1"
                },

                new CuiRectTransformComponent
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-145 -44.859", OffsetMax = "145 25.141"
                }
            }
        });

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0 0 0 0.6" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "87.777 -50", OffsetMax = "387.777 50" }
        }, "Case_TopPlace", "Panel_Purchase");

        container.Add(new CuiElement
        {
            Name = "Label_Purchase",
            Parent = "Panel_Purchase",
            Components =
            {
                new CuiTextComponent
                {
                    Text = lang.GetMessage("CaseBuyText", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 14,
                    Align = TextAnchor.MiddleLeft, Color = "1 1 1 1"
                },

                new CuiRectTransformComponent
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-145.7 25.141",
                    OffsetMax = "-45.7 45.141"
                }
            }
        });

        container.Add(new CuiElement
        {
            Name = "Label_Purchase_Discription",
            Parent = "Panel_Purchase",
            Components =
            {
                new CuiTextComponent
                {
                    Text = lang.GetMessage("CaseBuySelect", this, player.UserIDString), Font = "robotocondensed-bold.ttf",
                    FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1"
                },

                new CuiRectTransformComponent
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-145.7 10.141", OffsetMax = "145 25.141"
                }
            }
        });

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "1 1 1 0" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-145.7 -22.359", OffsetMax = "145 7.641" }
        }, "Panel_Purchase", "Panel_Cost");

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0.5215687 0.4470588 0.1843137 0.6" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "35.7 -10", OffsetMax = "140.7 10" }
        }, "Panel_Cost", "Panel_Cost");

        container.Add(new CuiElement
        {
            Name = "Label_Cost",
            Parent = "Panel_Cost",
            Components =
            {
                new CuiTextComponent
                {
                    Text = lang.GetMessage("Price1Case", this, player.UserIDString) + $" ${caseSettings.Price}", Font = "robotocondensed-bold.ttf", FontSize = 12,
                    Align = TextAnchor.MiddleCenter, Color = "0.9490196 0.6745098 0.1411765 1"
                },

                new CuiRectTransformComponent
                    { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-52.5 -10", OffsetMax = "52.5 10" }
            }
        });
        var color = buyAmount == 1 ? "0.5215687 0.4470588 0.1843137 0.8" : "0.5215687 0.4470588 0.1843137 0.6";
        container.Add(new CuiButton
        {
            Button = { Color = color, Command = $"UI_CASES CHANGEOPEN {caseSettings.Id} 1 {openAmount}" },
            Text =
            {
                Text = "1", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                Color = "0.9490196 0.6745098 0.1411765 1"
            },
            RectTransform =
                { AnchorMin = "-0.32 0.5", AnchorMax = "-0.32 0.5", OffsetMin = "-140 -10", OffsetMax = "-110 10" }
        }, "Panel_Cost", "Button_1");
        color = buyAmount == 2 ? "0.5215687 0.4470588 0.1843137 0.8" : "0.5215687 0.4470588 0.1843137 0.6";
        container.Add(new CuiButton
        {
            Button = { Color = color, Command = $"UI_CASES CHANGEOPEN {caseSettings.Id} 2 {openAmount}" },
            Text =
            {
                Text = "2", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                Color = "0.9490196 0.6745098 0.1411765 1"
            },
            RectTransform =
                { AnchorMin = "-0.32 0.5", AnchorMax = "-0.32 0.5", OffsetMin = "-105 -10", OffsetMax = "-75 10" }
        }, "Panel_Cost", "Button_2");
        color = buyAmount == 3 ? "0.5215687 0.4470588 0.1843137 0.8" : "0.5215687 0.4470588 0.1843137 0.6";
        container.Add(new CuiButton
        {
            Button = { Color = color, Command = $"UI_CASES CHANGEOPEN {caseSettings.Id} 3 {openAmount}" },
            Text =
            {
                Text = "3", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                Color = "0.9490196 0.6745098 0.1411765 1"
            },
            RectTransform =
                { AnchorMin = "-0.32 0.5", AnchorMax = "-0.32 0.5", OffsetMin = "-70 -10", OffsetMax = "-40 10" }
        }, "Panel_Cost", "Button_3");
        color = buyAmount == 4 ? "0.5215687 0.4470588 0.1843137 0.8" : "0.5215687 0.4470588 0.1843137 0.6";
        container.Add(new CuiButton
        {
            Button = { Color = color, Command = $"UI_CASES CHANGEOPEN {caseSettings.Id} 4 {openAmount}" },
            Text =
            {
                Text = "4", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                Color = "0.9490196 0.6745098 0.1411765 1"
            },
            RectTransform =
                { AnchorMin = "-0.32 0.5", AnchorMax = "-0.32 0.5", OffsetMin = "-35 -10", OffsetMax = "-5 10" }
        }, "Panel_Cost", "Button_4");
        color = buyAmount == 5 ? "0.5215687 0.4470588 0.1843137 0.8" : "0.5215687 0.4470588 0.1843137 0.6";
        container.Add(new CuiButton
        {
            Button = { Color = color, Command = $"UI_CASES CHANGEOPEN {caseSettings.Id} 5 {openAmount}" },
            Text =
            {
                Text = "5", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                Color = "0.9490196 0.6745098 0.1411765 1"
            },
            RectTransform =
                { AnchorMin = "-0.32 0.5", AnchorMax = "-0.32 0.5", OffsetMin = "0 -10", OffsetMax = "30 10" }
        }, "Panel_Cost", "Button_5");

        container.Add(new CuiButton
        {
            Button =
            {
                Color = "0.5215687 0.4470588 0.1843137 0.6",
                Command = $"UI_CASES BUY {caseSettings.Id} {buyAmount} {openAmount}"
            },
            Text =
            {
                Text = lang.GetMessage("BuyCase", this, player.UserIDString) + $" {buyAmount} " + lang.GetMessage("fortext", this, player.UserIDString) + $" ${caseSettings.Price * buyAmount}", Font = "robotocondensed-bold.ttf",
                FontSize = 12,
                Align = TextAnchor.MiddleCenter, Color = "0.9490196 0.6745098 0.1411765 1"
            },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-60 -46", OffsetMax = "60 -26" }
        }, "Panel_Purchase", "Button_Buy");
        _data[player.userID].CaseList.TryGetValue(caseSettings.Id, out var caseData);
        string textToOpen = caseData switch
        {
            1 => lang.GetMessage("1case", this, player.UserIDString),
            2 or 3 or 4 => lang.GetMessage("234case", this, player.UserIDString),
            _ => lang.GetMessage("5morecase", this, player.UserIDString)
        };

        var text = caseData > 0
            ? lang.GetMessage("InStock", this, player.UserIDString) + $" {caseData} {textToOpen}"
            : lang.GetMessage("NoneStock", this, player.UserIDString);
        var command = caseData < openAmount ? "" : $"UI_CASES OPEN {caseSettings.Id} {openAmount}";
        var commandColor = caseData < openAmount ? "0.5686275 0.1803922 0.1215686 0.6" : "0.3294118 0.5215687 0.1843137 0.8";
        var TextButtonColor = caseData < openAmount ? "0.8627451 0.4235294 0.3529412 1" : "0.5372549 0.7921569 0.3372549 1";
        textToOpen = openAmount == 1 ? lang.GetMessage("1case", this, player.UserIDString) : openAmount == 5 ? lang.GetMessage("5morecase", this, player.UserIDString) : lang.GetMessage("234case", this, player.UserIDString);

        container.Add(new CuiButton
        {
            Button = { Color = commandColor, Command = command },
            Text =
            {
                Text = lang.GetMessage("OpenCase", this, player.UserIDString) + $" {openAmount} {textToOpen}", Font = "robotocondensed-bold.ttf", FontSize = 12,
                Align = TextAnchor.MiddleCenter, Color = TextButtonColor
            },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-55 -87.5", OffsetMax = "55 -62.5" }
        }, "Case_TopPlace", "Button_Open");

        container.Add(new CuiElement
        {
            Name = "Label_MultiOpen",
            Parent = "Case_TopPlace",
            Components =
            {
                new CuiTextComponent
                {
                    Text = lang.GetMessage("OpenCases", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 10,
                    Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"
                },

                new CuiRectTransformComponent
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-337.507 -85", OffsetMax = "-257.78 -65"
                }
            }
        });
        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "1 1 1 0" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-257.78 -85", OffsetMax = "-87.78 -65" }
        }, "Case_TopPlace", "Panel_FastOpen");
        color = openAmount == 1 ? "0.3294118 0.5215687 0.1843137 0.8" : "0.3294118 0.5215687 0.1843137 0.6";
        container.Add(new CuiButton
        {
            Button = { Color = color, Command = $"UI_CASES CHANGEBUY {caseSettings.Id} {buyAmount} 1" },
            Text =
            {
                Text = "1", Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter,
                Color = "0.5372549 0.7921569 0.3372549 1"
            },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-85 -10", OffsetMax = "-55 10" }
        }, "Panel_FastOpen", "Button_Open 1");
        color = openAmount == 2 ? "0.3294118 0.5215687 0.1843137 0.8" : "0.3294118 0.5215687 0.1843137 0.6";
        container.Add(new CuiButton
        {
            Button = { Color = color, Command = $"UI_CASES CHANGEBUY {caseSettings.Id} {buyAmount} 2" },
            Text =
            {
                Text = "2", Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter,
                Color = "0.5372549 0.7921569 0.3372549 1"
            },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -10", OffsetMax = "-20 10" }
        }, "Panel_FastOpen", "Button_Open 2");
        color = openAmount == 3 ? "0.3294118 0.5215687 0.1843137 0.8" : "0.3294118 0.5215687 0.1843137 0.6";
        container.Add(new CuiButton
        {
            Button = { Color = color, Command = $"UI_CASES CHANGEBUY {caseSettings.Id} {buyAmount} 3" },
            Text =
            {
                Text = "3", Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter,
                Color = "0.5372549 0.7921569 0.3372549 1"
            },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-15 -10", OffsetMax = "15 10" }
        }, "Panel_FastOpen", "Button_Open 3");
        color = openAmount == 4 ? "0.3294118 0.5215687 0.1843137 0.8" : "0.3294118 0.5215687 0.1843137 0.6";
        container.Add(new CuiButton
        {
            Button = { Color = color, Command = $"UI_CASES CHANGEBUY {caseSettings.Id} {buyAmount} 4" },
            Text =
            {
                Text = "4", Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter,
                Color = "0.5372549 0.7921569 0.3372549 1"
            },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "20 -10", OffsetMax = "50 10" }
        }, "Panel_FastOpen", "Button_Open 4");
        color = openAmount == 5 ? "0.3294118 0.5215687 0.1843137 0.8" : "0.3294118 0.5215687 0.1843137 0.6";
        container.Add(new CuiButton
        {
            Button = { Color = color, Command = $"UI_CASES CHANGEBUY {caseSettings.Id} {buyAmount} 5" },
            Text =
            {
                Text = "5", Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter,
                Color = "0.5372549 0.7921569 0.3372549 1"
            },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "55 -10", OffsetMax = "85 10" }
        }, "Panel_FastOpen", "Button_Open 5");

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "1 1 1 0" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "87.775 -85", OffsetMax = "314.025 -65" }
        }, "Case_TopPlace", "FastOpen_ButtonPanel");

        commandColor = caseData > 0 ? "1 1 1 1" : "1 0 0 1";
        container.Add(new CuiElement
        {
            Name = "Label_1313",
            Parent = "Case_TopPlace",
            Components =
            {
                new CuiTextComponent
                {
                    Text = text, Font = "robotocondensed-bold.ttf", FontSize = 14,
                    Align = TextAnchor.MiddleCenter, Color = commandColor
                },
                new CuiRectTransformComponent
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-150 72.267", OffsetMax = "150 92.267"
                }
            }
        });

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0 0 0 0.6" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400.75 -188", OffsetMax = "400.75 -3" }
        }, "Body_Place", "Case_Compatible");

        container.Add(new CuiElement
        {
            Name = "ItemList_Text",
            Parent = "Case_Compatible",
            Components =
            {
                new CuiTextComponent
                {
                    Text = lang.GetMessage("CaseContent", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 12,
                    Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"
                },

                new CuiRectTransformComponent
                    { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-150 72.5", OffsetMax = "150 92.5" }
            }
        });

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "1 1 1 0" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-395 -92.5", OffsetMax = "395 72.5" }
        }, "Case_Compatible", "Item_List");
        var index = 0;
        var posx = -377.5;
        var width = -287.5 - posx;
        var posy = 2.0;
        var height = 82.5 - posy;
        var rewardList = _config.RewardList.Where(p => caseSettings.ItemList.Contains(p.RewardID));
        foreach (var check in rewardList.OrderBy(p => p.Chance))
        {
            index++;
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0.6980392" },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{posx} {posy}",
                    OffsetMax = $"{posx + width} {posy + height}"
                }
            }, "Item_List", "Item_1");

			container.Add(new CuiElement
			{
				Name = "Panel_5588",
				Parent = "Item_1",
				Components = {
								new CuiRawImageComponent { Color = GetColor(check.Chance), Png = ImageList["Chance_ItemImg"] },
								new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-42.5 -37.5", OffsetMax = "42.5 37.5" }
							}
			});

            // container.Add(new CuiPanel
            // {
                // CursorEnabled = false,
                // Image = { Color = GetColor(check.Chance) },
                // RectTransform =
                    // { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-42.5 -37.5", OffsetMax = "42.5 37.5" }
            // }, "Item_1", "Panel_5588");

            container.Add(new CuiElement
            {
                Name = "Image_Item",
                Parent = "Item_1",
                Components =
                {
                    !string.IsNullOrEmpty(check.ItemImage) ? new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageList[check.ItemImage] } : new CuiImageComponent
                    {
                        ItemId = _shortNameToItemID[check.Shortname], SkinId = check.SkinID
                    },
                    new CuiRectTransformComponent
                        { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -30", OffsetMax = "30 30" }
                }
            });

            posx += width + 5;
            if (index == 8)
            {
                posy -= height + 5;
                posx = -377.5;
            }
        }

        for (int i = index; i < 16; i++)
        {
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0.6980392" },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{posx} {posy}",
                    OffsetMax = $"{posx + width} {posy + height}"
                }
            }, "Item_List", "Item_9");
            posx += width + 5;
            if (index == 8)
            {
                posy -= height + 5;
                posx = -377.5;
            }
        }

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0 0 0 0.6" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-407.5 -225", OffsetMax = "407.5 -200" }
        }, "Case_MainPlace", "Footer_Place");

        container.Add(new CuiElement
        {
            Name = "Label_Footer",
            Parent = "Footer_Place",
            Components =
            {
                new CuiTextComponent
                {
                    Text = lang.GetMessage("ButtonTextCase", this, player.UserIDString),
                    Font = "robotocondensed-bold.ttf", FontSize = 8, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },

                new CuiRectTransformComponent
                    { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-300 -12.5", OffsetMax = "300 12.5" }
            }
        });

        CuiHelper.DestroyUi(player, "CasePage_Button");
        CuiHelper.DestroyUi(player, "Footer_Place");
        CuiHelper.DestroyUi(player, "Body_Place");
        CuiHelper.AddUi(player, container);
        UpdateBalance(player);
    }

    private string GetColor(int chance)
    {
        foreach (var check in _config.ColorList)
        {
            if (chance <= check.Key)
                return check.Value;
        }

        return "0 0 0 0.6";
    }

    private List<Configuration.ItemSettings> GetRewardList(IEnumerable<Configuration.ItemSettings> itemSettingsList, int amount)
    {
        var result = new List<Configuration.ItemSettings>();
        if (!itemSettingsList.Any() || amount <= 0)
            return result;

        var weightItems = itemSettingsList.Select(item => new { Item = item, Weight = item.Chance }).ToList();

        while (result.Count < amount)
        {
            var item = weightItems.GetRandom();
            
            if (Core.Random.Range(0f, 100f) > item.Weight)
                continue;

            result.Add(item.Item);
        }

        return result;
    }


    private void ShowMainUI(BasePlayer player, int page)
    {
        var container = new CuiElementContainer();
        container.Add(new CuiPanel
        {
            CursorEnabled = true,
            Image = { Color = "0 0 0 0.8" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-407.5 -225", OffsetMax = "407.5 225" }
        }, "Overlay", "Case_MainPlace");

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0 0 0 0.8" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-407.5 185", OffsetMax = "407.5 225" }
        }, "Case_MainPlace", "Topline_Place");

    
        container.Add(new CuiButton
        {
            Button = { Color = "0.1843137 0.4392157 0.5215687 0.6", Command = "UI_CASES INVENTORY" },
            Text =
            {
                Text = lang.GetMessage("Inventory", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 14,
                Align = TextAnchor.MiddleCenter, Color = "0.3411765 0.6352941 0.8 1"
            },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "238.6 -15", OffsetMax = "318.6 15" }
        }, "Topline_Place", "Inventory_Button");

        container.Add(new CuiButton
        {
            Button = { Color = "0.5686275 0.1803922 0.1215686 0.6", Close = "Case_MainPlace" },
            Text =
            {
                Text = lang.GetMessage("Close", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                Color = "0.8627451 0.4235294 0.3529412 1"
            },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "323 -15", OffsetMax = "403 15" }
        }, "Topline_Place", "Close_Button");
        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0 0 0 0" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-407.5 -200", OffsetMax = "407.5 185" }
        }, "Case_MainPlace", "Body_Place");
        var posx = -400.25;
        var width = -203.75 - posx;
        var posy = 3;
        var height = 188 - posy;
        var index = 1;
        foreach (var check in _config.CaseList.Skip(8* page).Take(8))
        {
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0.7" },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{posx} {posy}",
                    OffsetMax = $"{posx + width} {posy + height}"
                }
            }, "Body_Place", "Case_Card 1");

            container.Add(new CuiElement
            {
                Name = "Case_Image",
                Parent = "Case_Card 1",
                Components =
                {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageList[check.Image] },
                    new CuiRectTransformComponent
                        { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-67.55 -67.2", OffsetMax = "67.55 67.9" }
                }
            });

            if (_data[player.userID].CaseList.TryGetValue(check.Id, out var amount))
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.5215687 0.4470588 0.1843137 0.6" },
                    RectTransform =
                        { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-93.1 67.9", OffsetMax = "-33.1 85.9" }
                }, "Case_Card 1", "Case_Count");

                container.Add(new CuiElement
                {
                    Name = "Case_CountTxt",
                    Parent = "Case_Count",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = lang.GetMessage("Have", this, player.UserIDString) + $" {amount}", Font = "robotocondensed-bold.ttf", FontSize = 10,
                            Align = TextAnchor.MiddleCenter, Color = "0.9490196 0.6745098 0.1411765 1"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-14.6 -7.5",
                            OffsetMax = "27.5 7.5"
                        }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "Case_CountImg",
                    Parent = "Case_Count",
                    Components =
                    {
                        new CuiRawImageComponent { Color = "1 1 1 0.5019608", Png = ImageList["Case_CountImg"] },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-26.6 -6", OffsetMax = "-14.6 6"
                        }
                    }
                });
            }

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.5215687 0.4470588 0.1843137 0.6" },
                RectTransform =
                    { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "42 67.9", OffsetMax = "92 85.9" }
            }, "Case_Card 1", "Case_Price");

            container.Add(new CuiElement
            {
                Name = "Case_PriceTxt",
                Parent = "Case_Price",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = $"$ {check.Price}", Font = "robotocondensed-bold.ttf", FontSize = 10,
                        Align = TextAnchor.MiddleCenter, Color = "0.9490196 0.6745098 0.1411765 1"
                    },

                    new CuiRectTransformComponent
                        { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-20 -7.5", OffsetMax = "20 7.5" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1843137 0.4392157 0.5215687 0.8" },
                RectTransform =
                    { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -87.2", OffsetMax = "50 -67.2" }
            }, "Case_Card 1", "Case_Name");

            container.Add(new CuiElement
            {
                Name = "Case_NameTxt",
                Parent = "Case_Name",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = check.DisplayName, Font = "robotocondensed-bold.ttf", FontSize = 11,
                        Align = TextAnchor.MiddleCenter, Color = "0.3411765 0.6352941 0.8 1"
                    },

                    new CuiRectTransformComponent
                        { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -7.5", OffsetMax = "50 7.5" }
                }
            });
            container.Add(new CuiButton
            {
                Button = { Color = "0.5215687 0.4470588 0.1843137 0", Command = $"UI_CASES SHOW {check.Id}" },
                Text =
                {
                    Text = "", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                    Color = "0.9490196 0.6745098 0.1411765 1"
                },
                RectTransform =
                    { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, "Case_Card 1", "Button_1");
            posx += width + 5;
            if (index == 4)
            {
                index = 0;
                posy -= height + 5;
                posx = -400.25;
            }

            index++;
        }

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0 0 0 0.6" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-407.5 -225", OffsetMax = "407.5 -200" }
        }, "Case_MainPlace", "Footer_Place");

        container.Add(new CuiButton
        {
            Button = { Color = "0.3294118 0.5215687 0.1843137 0.8", Command = $"UI_CASES PAGE {page + 1}"},
            Text =
            {
                Text = lang.GetMessage("Next", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                Color = "0.5411765 0.8 0.3411765 1"
            },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "345 -10", OffsetMax = "405 10" }
        }, "Footer_Place", "Button_Next");

        container.Add(new CuiButton
        {
            Button = { Color = "0.3294118 0.5215687 0.1843137 0.8" , Command = $"UI_CASES PAGE {page - 1}"},
            Text =
            {
                Text = lang.GetMessage("Back", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                Color = "0.5411765 0.8 0.3411765 1"
            },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-405 -10", OffsetMax = "-345 10" }
        }, "Footer_Place", "Button_Back");

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0 0 0 0.0" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -10", OffsetMax = "50 10" }
        }, "Footer_Place", "Panel_Page");

        container.Add(new CuiElement
        {
            Name = "Label_PageCount",
            Parent = "Panel_Page",
            Components =
            {
                new CuiTextComponent
                {
                    Text = lang.GetMessage("Page", this, player.UserIDString) + $" {page + 1}", Font = "robotocondensed-bold.ttf", FontSize = 12,
                    Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"
                },

                new CuiRectTransformComponent
                    { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -10", OffsetMax = "50 10" }
            }
        });

        CuiHelper.DestroyUi(player, "Case_MainPlace");
        CuiHelper.AddUi(player, container);
        UpdateBalance(player);
    }

    private void ShowOpenCaseFast(BasePlayer player, Configuration.Case caseSettings, int caseAmount)
    {
        var container = new CuiElementContainer();
        // container.Add(new CuiPanel
        // {
        //     CursorEnabled = false,
        //     Image = { Color = "0 0 0 0" },
        //     RectTransform =
        //         { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-407.5 -200", OffsetMax = "407.5 185" }
        // }, "Case_MainPlace", "Body_Place");
        // container.Add(new CuiPanel
        // {
        //     CursorEnabled = false,
        //     Image = { Color = "0 0 0 0.8" },
        //     RectTransform =
        //     {
        //         AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400.75 -22.376",
        //         OffsetMax = "400.75 187.624"
        //     }
        // }, "Body_Place", "Case_Received");
        //
        // container.Add(new CuiElement
        // {
        //     Name = "Label_Prize",
        //     Parent = "Case_Received",
        //     Components =
        //     {
        //         new CuiTextComponent
        //         {
        //             Text = lang.GetMessage("Prise", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 12,
        //             Align = TextAnchor.MiddleCenter, Color = "0.9490196 0.6745098 0.1411765 1"
        //         },
        //
        //         new CuiRectTransformComponent
        //             { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100 87", OffsetMax = "100 105" }
        //     }
        // });
        //
        //
        // container.Add(new CuiPanel
        // {
        //     CursorEnabled = false,
        //     Image = { Color = "1 1 1 0" },
        //     RectTransform =
        //         { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 -105", OffsetMax = "400 87" }
        // }, "Case_Received", "Panel_Prize");

        var width = 157.5;
        var startPos = -78.75 - ((caseAmount - 1) * width + (caseAmount - 1) * 2) / 2;
        var rewardList = _config.RewardList.Where(p => caseSettings.ItemList.Contains(p.RewardID));
        var prizeList = GetRewardList(rewardList, caseAmount);
        for (int i = 0; i < caseAmount; i++)
        {
            var prize = prizeList[i];
            _data[player.userID].Inventory.Add(prize.RewardID);
            container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.05428431 0.05368458 0.0754717 0.5019608" },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{startPos} -95",
                        OffsetMax = $"{startPos + width} 95"
                    }
                }, "Panel_Prize", $"Prize_Plate{i}");
				
			container.Add(new CuiElement
				{
					Name = "Shadow_ItemChance",
					Parent = $"Prize_Plate{i}",
					Components = {
									new CuiRawImageComponent { Color = GetColor(prize.Chance), Png = ImageList["Shadow_ItemImg"] },
									new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-76.9 -73.5", OffsetMax = "76.9 93.5" }
								}
				});
				
            container.Add(new CuiElement
            {
                Name = "Prize_Name",
                Parent = $"Prize_Plate{i}",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = $"{prize.displayName} x{prize.Amount}", FadeIn = 5, Font = "robotocondensed-bold.ttf",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"
                    },

                    new CuiRectTransformComponent
                        { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-75 75", OffsetMax = "75 95" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Prize_Image",
                Parent = $"Prize_Plate{i}",
                Components =
                {
                    !string.IsNullOrEmpty(prize.ItemImage) ? new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageList[prize.ItemImage] } : new CuiImageComponent
                    {
                        ItemId = _shortNameToItemID[prize.Shortname], SkinId = prize.SkinID
                    },
                    new CuiRectTransformComponent
                        { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-62.5 -62.5", OffsetMax = "62.5 62.5" }
                }
            });

            container.Add(new CuiButton
                {
                    Button = { Color = "0.1843137 0.4392157 0.5215687 0.6", Close = $"Prize_Plate{i}", Command = $"UI_CASES SELL {prize.RewardID}"},
                    Text =
                    {
                        Text = lang.GetMessage("Sell", this, player.UserIDString) + $" {prize.Price}", Font = "robotocondensed-bold.ttf", FontSize = 9,
                        Align = TextAnchor.MiddleCenter, Color = "0.3411765 0.6352941 0.8 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-76.9 -95", OffsetMax = "-1.9 -75"
                    }
                }, $"Prize_Plate{i}", "Button_Sell");

            container.Add(new CuiButton
                {
                    Button = { Color = "0.3294118 0.5215687 0.1843137 0.6", Close = $"Prize_Plate{i}" },
                    Text =
                    {
                        Text = lang.GetMessage("PutSide", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 9,
                        Align = TextAnchor.MiddleCenter,
                        Color = "0.5372549 0.7921569 0.3372549 1"
                    },
                    RectTransform =
                        { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "1.9 -95", OffsetMax = "76.9 -75" }
                }, $"Prize_Plate{i}", "Button_Inventory");
            startPos += width + 2;
        }

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "1 1 1 0" },
            RectTransform ={ AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-395 -270.624", OffsetMax = "395 -105.624" }
        },"Case_Received","Item_List");
        var index = 0;
        var posx = -377.5;
         width = -287.5 - posx;
        var posy = 2.5;
        var height = 82.5 - posy;

        foreach (var check in rewardList.OrderBy(p => p.Chance))
        {
           
            index++;
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0.6980392" },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{posx} {posy}",
                    OffsetMax = $"{posx + width} {posy + height}"
                }
            }, "Item_List", "Item_1");

			container.Add(new CuiElement
			{
				Name = "Panel_5588",
				Parent = "Item_1",
				Components = {
								new CuiRawImageComponent { Color = GetColor(check.Chance), Png = ImageList["Chance_ItemImg"] },
								new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-42.5 -37.5", OffsetMax = "42.5 37.5" }
							}
			});
			
            // container.Add(new CuiPanel
            // {
                // CursorEnabled = false,
                // Image = { Color = GetColor(check.Chance) },
                // RectTransform =
                    // { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-42.5 -37.5", OffsetMax = "42.5 37.5" }
            // }, "Item_1", "Panel_5588");

            container.Add(new CuiElement
            {
                Name = "Image_Item",
                Parent = "Item_1",
                Components =
                {
                    !string.IsNullOrEmpty(check.ItemImage) ? new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageList[check.ItemImage] } : new CuiImageComponent
                    {
                        ItemId = _shortNameToItemID[check.Shortname], SkinId = check.SkinID
                    },
                    new CuiRectTransformComponent
                        { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -30", OffsetMax = "30 30" }
                }
            });

            posx += width + 5;
            if (index == 8)
            {
                posy -= height + 5;
                posx = -377.5;
            }
        }

        for (int i = index; i < 16; i++)
        {
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0.6980392" },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{posx} {posy}",
                    OffsetMax = $"{posx + width} {posy + height}"
                }
            }, "Item_List", "Item_9");
            posx += width + 5;
            if (index == 8)
            {
                posy -= height + 5;
                posx = -377.5;
            }
        }

        CuiHelper.DestroyUi(player, "Case_Received");
        CuiHelper.DestroyUi(player, "Body_Place");
        CuiHelper.AddUi(player, container);

    }

    private void ShowInventory(BasePlayer player, int page)
    {
        var container = new CuiElementContainer();
        container.Add(new CuiPanel
        {
            CursorEnabled = true,
            Image = { Color = "0 0 0 0.8" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-407.5 -225", OffsetMax = "407.5 225" }
        }, "Overlay", "Case_MainPlace");

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0 0 0 0.8" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-407.5 185", OffsetMax = "407.5 225" }
        }, "Case_MainPlace", "Topline_Place");

      

        container.Add(new CuiButton
        {
            Button = { Color = "0.3294118 0.5215687 0.1843137 0.6", Command = "chat.say /cases" },
            Text =
            {
                Text = lang.GetMessage("CasePage", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 14,
                Align = TextAnchor.MiddleCenter, Color = "0.5372549 0.7921569 0.3372549 1"
            },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "238.6 -15", OffsetMax = "318.6 15" }
        }, "Topline_Place", "CasePage_Button");

        container.Add(new CuiButton
        {
            Button = { Color = "0.5686275 0.1803922 0.1215686 0.6", Close = "Case_MainPlace" },
            Text =
            {
                Text = lang.GetMessage("Close", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                Color = "0.8627451 0.4235294 0.3529412 1"
            },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "323 -15", OffsetMax = "403 15" }
        }, "Topline_Place", "Close_Button");

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0 0 0 0" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-407.5 -200", OffsetMax = "407.5 185" }
        }, "Case_MainPlace", "Body_Place");

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0 0 0 0.2" },
            RectTransform =
            {
                AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400.75 -185.8", OffsetMax = "400.75 184.2"
            }
        }, "Body_Place", "Case_Compatible");

        container.Add(new CuiElement
        {
            Name = "ItemList_Text",
            Parent = "Case_Compatible",
            Components =
            {
                new CuiTextComponent
                {
                    Text = lang.GetMessage("ContentInventory", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 12,
                    Align = TextAnchor.LowerCenter, Color = "1 1 1 1"
                },

                new CuiRectTransformComponent
                    { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-150 165", OffsetMax = "150 185" }
            }
        });

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "1 1 1 0" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-395 -185", OffsetMax = "395 165" }
        }, "Case_Compatible", "Item_List");
        var index = 0;
        var posx = -377.5;
        var width = -287.5 - posx;
        var posy = 87.5;
        var height = 167.5 - posy;
        foreach (var check in _data[player.userID].Inventory.Skip(32 * page).Take(32))
        {
            index++;
            var item = _config.RewardList.FirstOrDefault(p => p.RewardID == check);
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0.6" },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{posx} {posy}",
                    OffsetMax = $"{posx + width} {posy + height}"
                }
            }, "Item_List", "Item_1 (1)");

            container.Add(new CuiElement
            {
                Name = "Panel_5588",
                Parent = "Item_1 (1)",
                Components = {
                    new CuiRawImageComponent { Color = GetColor(item.Chance), Png = ImageList["Chance_ItemImg"] },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-42.5 -37.5", OffsetMax = "42.5 37.5" }
                }
            });



            container.Add(new CuiElement
            {
                Name = "Image_Item",
                Parent = "Item_1 (1)",
                Components =
                {
                    !string.IsNullOrEmpty(item.ItemImage) ? new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageList[item.ItemImage] } : new CuiImageComponent
                    {
                        ItemId = _shortNameToItemID[item.Shortname], SkinId = item.SkinID
                    },
                    new CuiRectTransformComponent
                        { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -30", OffsetMax = "30 30" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Label_243",
                Parent = "Item_1 (1)",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = $"х{item.Amount}", Font = "robotocondensed-bold.ttf", FontSize = 14,
                        Align = TextAnchor.MiddleRight,
                        Color = "1 1 1 1"
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-42.5 -40", OffsetMax = "42.5 -20"
                    }
                }
            });
            container.Add(new CuiButton
            {
                Button = { Color = "0.3294118 0.5215687 0.1843137 0", Command = $"UI_CASES TAKEREWARD {check} {page}" },
                Text =
                {
                    Text = "", Font = "robotocondensed-bold.ttf", FontSize = 14,
                    Align = TextAnchor.MiddleCenter, Color = "0.5372549 0.7921569 0.3372549 1"
                },
                RectTransform =
                    { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, "Item_1 (1)", "TAKEREWARDBUTTON");
            posx += width + 5;
            if (index == 8 || index == 16 || index == 24)
            {
                posy -= height + 5;
                posx = -377.5;
            }
        }

        for (int i = index; i < 32; i++)
        {
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0.6980392" },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{posx} {posy}",
                    OffsetMax = $"{posx + width} {posy + height}"
                }
            }, "Item_List", "Item_9");
            posx += width + 5;
            if (i == 7 || i == 15 || i == 23)
            {
                posy -= height + 5;
                posx = -377.5;
            }
        }

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0 0 0 0.6" },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-407.5 -225", OffsetMax = "407.5 -200" }
        }, "Case_MainPlace", "Footer_Place");
        if (_config.isLimitReward)
        {
            var userAmount = GetAmount(player);
            container.Add(new CuiElement
            {
                Name = "Label_Footer",
                Parent = "Footer_Place",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = lang.GetMessage("CanTake1", this, player.UserIDString) +
                               $" {userAmount - _data[player.userID].AmountToUse}/{userAmount} " +
                               lang.GetMessage("CanTake2", this, player.UserIDString),
                        Font = "robotocondensed-bold.ttf", FontSize = 8, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 -12.5", OffsetMax = "400 12.5"
                    }
                }
            });
        }

        container.Add(new CuiButton
        {
            Button = { Color = "0.3294118 0.5215687 0.1843137 0.8", Command = $"UI_CASES INVENTORYPAGE {page + 1}"},
            Text =
            {
                Text = lang.GetMessage("Next", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                Color = "0.5411765 0.8 0.3411765 1"
            },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "345 -10", OffsetMax = "405 10" }
        }, "Footer_Place", "Button_Next");

        container.Add(new CuiButton
        {
            Button = { Color = "0.3294118 0.5215687 0.1843137 0.8" , Command = $"UI_CASES INVENTORYPAGE {page - 1}"},
            Text =
            {
                Text = lang.GetMessage("Back", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                Color = "0.5411765 0.8 0.3411765 1"
            },
            RectTransform =
                { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-405 -10", OffsetMax = "-345 10" }
        }, "Footer_Place", "Button_Back");

        CuiHelper.DestroyUi(player, "Case_MainPlace");
        CuiHelper.AddUi(player, container);
        UpdateBalance(player);
    }

    private readonly List<ImageSettings> ImageSettingsList = new()
    {
        new()
        {

            Name = "Case_Image",
            Url = "https://i.imgur.com/d6dlP9w.png"

        },
        new()
        {

            Name = "Case_CountImg",
            Url = "https://i.imgur.com/srLVbhO.png"

        },
		new()
        {

            Name = "Shadow_ItemImg",
            Url = "https://unityplugincore.ru/imgs/images/2024/07/18/65e3b9af2b06.png"

        },
		new()
        {

            Name = "Chance_ItemImg",
            Url = "https://unityplugincore.ru/imgs/images/2024/07/18/cad162c9925c.png"

        },
    };

  
    private readonly Dictionary<string, string> ImageList = new Dictionary<string, string>();

    private class ImageSettings
    {
        [JsonProperty(PropertyName = "Name", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public string Name;

        [JsonProperty(PropertyName = "Path", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public string Url;
    }

    private void DownloadImage()
    {
        var image = ImageSettingsList.FirstOrDefault(p => !ImageList.ContainsKey(p.Name));


        if (image == null)
        {
            Puts("Image upload completed");
            return;
        }

        ServerMgr.Instance.StartCoroutine(StartDownloadImage(image));
    }

    private IEnumerator StartDownloadImage(ImageSettings image)
    {
        var url = image.Url;
        using (var www = new WWW(url))
        {
            yield return www;
            if (www.error != null)
            {
                PrintError($"Failed to download image {image.Name}.Address [{image.Url}] invalid");
                ImageList.Add(image.Name, "");
            }
            else
            {
                var texture = www.texture;
                var png = FileStorage.server.Store(texture.EncodeToPNG(), FileStorage.Type.png,
                    CommunityEntity.ServerInstance.net.ID).ToString();
                ImageList.Add(image.Name, png);
            }

            DownloadImage();
        }
    }


    #region Lang



    protected override void LoadDefaultMessages()
    {
        lang.RegisterMessages(new Dictionary<string, string>
        {
            ["LimitItems"] = "You have reached the daily limit for the issuance of items.",
            ["NoBalance"] = "THERE ARE NOT ENOUGH FUNDS TO BUY",
            ["NoCase"] = "NOT ENOUGH CASES TO OPEN",
            ["UI_Balace"] = "BALANCE",
            ["CasePage"] = "CASES",
            ["CaseBuyText"] = "Buying cases",
            ["CaseBuySelect"] = "SELECT THE NUMBER OF CASES TO PURCHASE",
            ["Price1Case"] = "THE PRICE OF 1 CASE",
            ["BuyCase"] = "BUY",
            ["1case"] = "CASE",
            ["234case"] = "CASES",
            ["5morecase"] = "CASES",
            ["InStock"] = "IN STOCK",
            ["NoneStock"] = "YOU DO NOT HAVE THIS CASE IN STOCK",
            ["OpenCase"] = "OPEN",
            ["OpenCases"] = "OPEN SEVERAL CASES",
            ["CaseContent"] = "CONTENTS OF THE CASE",
            ["ButtonTextCase"] = "ALL ITEMS PUT ASIDE FROM THE CASES ARE PLACED IN THE PLUGIN'S INVENTORY, FROM WHERE YOU CAN TAKE THEM AT ANY TIME",
            ["Inventory"] = "INVENTORY",
            ["Close"] = "CLOSE",
            ["Have"] = "HAVE",
            ["Next"] = "NEXT",
            ["Back"] = "BACK",
            ["Prise"] = "YOUR PRIZE",
            ["Sell"] = "SELL FOR",
            ["PutSide"] = "PUT",
            ["Page"] = "PAGE",
            ["fortext"] = "FOR",
            ["ContentInventory"] = "INVENTORY CONTENTS",
            ["CanTake1"] = "YOU CAN PICK UP CASES FROM THE INVENTORY ON THE DAY",
            ["CanTake2"] = "ITEMS. YOU CAN INCREASE THIS LIMIT BY USING THE PRIVILEGE"
        }, this);

        lang.RegisterMessages(new Dictionary<string, string>
        {
            ["LimitItems"] = "Вы достигли дневного лимита на выдачу предметов.",
            ["NoBalance"] = "НЕДОСТАТОЧНО СРЕДСТВ ДЛЯ ПОКУПКИ",
            ["NoCase"] = "НЕДОСТАТОЧНО КЕЙСОВ ДЛЯ ОТКРЫТИЯ",
            ["UI_Balace"] = "БАЛАНС",
            ["CasePage"] = "КЕЙСЫ",
            ["CaseBuyText"] = "Покупка кейсов",
            ["CaseBuySelect"] = "ВЫБЕРИТЕ КОЛ-ВО КЕЙСОВ ДЛЯ ПОКУПКИ",
            ["Price1Case"] = "ЦЕНА 1 КЕЙСА",
            ["BuyCase"] = "КУПИТЬ",
            ["1case"] = "КЕЙС",
            ["234case"] = "КЕЙСА",
            ["5morecase"] = "КЕЙСОВ",
            ["InStock"] = "В НАЛИЧИИ",
            ["NoneStock"] = "У ВАС НЕТ В НАЛИЧИИ ДАННОГО КЕЙСА",
            ["OpenCase"] = "ОТКРЫТЬ",
            ["OpenCases"] = "ОТКРЫТЬ КЕЙСОВ",
            ["CaseContent"] = "СОДЕРЖИМОЕ КЕЙСА",
            ["ButtonTextCase"] = "ВСЕ ПРЕДМЕТЫ, ОТЛОЖЕННЫЕ ИЗ КЕЙСОВ, ПОМЕЩАЮТСЯ В ИНВЕНТАРЬ ПЛАГИНА, ОТКУДА ВЫ ИХ МОЖЕТЕ ВЗЯТЬ В ЛЮБОЙ МОМЕНТ",
            ["Inventory"] = "ИНВЕНТАРЬ",
            ["Close"] = "ЗАКРЫТЬ",
            ["Have"] = "ЕСТЬ",
            ["Next"] = "ВПЕРЕД",
            ["Back"] = "НАЗАД",
            ["Prise"] = "ВАШ ПРИЗ",
            ["Sell"] = "ПРОДАТЬ ЗА",
            ["PutSide"] = "ОТЛОЖИТЬ",
            ["Page"] = "СТРАНИЦА",
            ["fortext"] = "ЗА",
            ["ContentInventory"] = "СОДЕРЖИМОЕ ИНВЕНТАРЯ",
            ["CanTake1"] = "ИЗ ИНВЕНТАРЯ КЕЙСОВ ВЫ МОЖЕТЕ ЗАБРАТЬ В ДЕНЬ",
            ["CanTake2"] = "ПРЕДМЕТОВ. УВЕЛИЧИТЬ ЭТОТ ПРЕДЕЛ МОЖНО ПРИ ПОМОЩИ ПРИВИЛЕГИИ"
        }, this, "ru");
    }

    private string GetMessage(string langKey, string steamID)
    {
        return lang.GetMessage(langKey, this, steamID);
    }

    private void SendPlayerMessage(BasePlayer player, string langKey, params object[] args)
    {
        Player.Message(player, GetMessage(langKey, player.UserIDString, args));
    }

    private string GetMessage(string langKey, string steamID, params object[] args)
    {
        return args.Length == 0
            ? GetMessage(langKey, steamID)
            : string.Format(GetMessage(langKey, steamID), args);
    }

    #endregion

    #endregion
    
    
    #region xkrystalll mod
    
    #region Image loader
    private static class GuiManager
    {
        public static void Clear()
        {
            iconImageInfos.Clear();
            FailedLoad.Clear();
        }

        public static string Get(string key)
        {
            if (iconImageInfos.TryGetValue(key, out var id))
                return id.ToString();
            return "";
        }

        private static Dictionary<string, uint> iconImageInfos = new();

        private static List<string> FailedLoad = new();

        internal static void LoadImages(List<string> imageFiles)
        {
            foreach (var x in imageFiles)
            {
                if (iconImageInfos.ContainsKey(x))
                    continue;
                iconImageInfos.Add(x, 0);
            }

            ServerMgr.Instance.StartCoroutine(LoadIconsCoroutine());
        }

        private static IEnumerator LoadIconsCoroutine()
        {
            for (int i = 0; i < iconImageInfos.Count; i++)
            {
                var imageInfo = iconImageInfos.ElementAtOrDefault(i);
                string url = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar +
                             "/CaseSystem/Images/" +
                             imageInfo.Key + ".png";

                using (WWW www = new WWW(url))
                {
                    yield return www;

                    if (www.error != null)
                    {
                        FailedLoad.Add(imageInfo.Key);
                    }
                    else
                    {
                        var texture = www.texture;
                        var imageId = FileStorage.server.Store(texture.EncodeToPNG(), FileStorage.Type.png,
                            CommunityEntity.ServerInstance.net.ID);
                        iconImageInfos[imageInfo.Key] = imageId;
                        GameObject.DestroyImmediate(texture);
                    }
                }
            }

            if (FailedLoad.IsNullOrEmpty())
                yield break;
				
            Debug.LogError($"\n\nFailed for loading {FailedLoad.Count} images in plugin CaseSystem");
            Debug.LogError("__________________________________");
            for (int i = 0; i < FailedLoad.Count; i++)
                Debug.LogWarning($"[{i + 1}]" + $"{FailedLoad[i]}".PadLeft(31 - (i + 1 >= 10 ? 1 : 0), ' '));
            Debug.LogError("__________________________________\n\n");
					
        }
    }
    #endregion
    
    #region Animated case opening
    private const float UPDATE_INTERVAL = 0.016f;
    private const int WINNER_INDEX = 139;
    private readonly int[] REMOVE_AT_END_ANIMATION = new[] { 137, 138, 140, 141 };

    private const float PLAY_EFFECT_EVERY_OFFSET = 500;
    private const float SLOWER_K = 2;
    private readonly Dictionary<int, int> speedDictionary = new Dictionary<int, int>
    {
        [0] = 9500,
        [2000] = 9000,
        [4000] = 8500,
        [6000] = 8000,
        [8000] = 7500,
        [10000] = 7000,
        [12000] = 6500,
        [14000] = 6000,
        [16000] = 5500,
        [18000] = 5000,
        [20000] = 4500,
        [22000] = 4000,
        [24000] = 3500,
        [26000] = 3000,
        [28000] = 2500,
        [30000] = 2000,
        [31000] = 1800,
        [31500] = 1600,
        [32000] = 1500,
        [32500] = 1400,
        [33000] = 1300,
        [33500] = 1200,
        [34000] = 1000,
        [34500] = 600,
        [35000] = 400,
        [35790] = 200
    };


    private int GetSpeedFromDictionary(float currentOffset)
    {
        int closestValue = 200;
        int maxKey = int.MinValue;

        foreach (var kvp in speedDictionary)
        {
            if (currentOffset >= kvp.Key && kvp.Key > maxKey)
            {
                maxKey = kvp.Key;
                closestValue = kvp.Value;
            }
        }

        return closestValue;
    }
    private IEnumerator OpenRoutine(BasePlayer player, List<Configuration.ItemSettings> defs, float goal, string caseId)
    {
        float currentOffset = 0;
        float lastPlayedEffectOffset = 0;
        _data[player.userID].Inventory.Add(defs[WINNER_INDEX].RewardID);

        for (int i = 0; i <= 5; i++)
        {
            CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".sell" + $".{i}");
            CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".buy" + $".{i}");
        }

        
        CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".items.div");
        CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".pointer");
        EffectNetwork.Send(new Effect("assets/prefabs/npc/autoturret/effects/reload.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);
        
        yield return CoroutineEx.waitForSeconds(1.7f);
        
        
        UI_UpdateAnimatedOpeningItems(player, defs, 0);
        float playSoundCooldown = PLAY_EFFECT_EVERY_OFFSET * 3;
        
        while (currentOffset < goal)
        {
            float distanceToGoal = goal - currentOffset;

            float currentSpeed = Mathf.Max(200f, Mathf.Min(GetSpeedFromDictionary(currentOffset), distanceToGoal * distanceToGoal * 0.0007f));

            currentOffset += currentSpeed * UPDATE_INTERVAL;
            if (currentOffset > 25000)
                playSoundCooldown = PLAY_EFFECT_EVERY_OFFSET;
            if (currentOffset - lastPlayedEffectOffset > playSoundCooldown)
            {
                EffectNetwork.Send(new Effect("assets/prefabs/weapons/sawnoff_shotgun/effects/deploy2.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);
                lastPlayedEffectOffset = currentOffset;
            }
            UI_UpdateAnimatedOpeningItems(player, defs, Mathf.Min(currentOffset, goal));

            if (currentOffset >= goal)
            {
                break;
            }

            yield return CoroutineEx.waitForSeconds(UPDATE_INTERVAL);
        }

        UI_UpdateAnimatedOpeningItems(player, defs, goal);
        
        
        
        foreach (var x in REMOVE_AT_END_ANIMATION)
            CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".items.div" + $".{x}");
        CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".pointer");
        EffectNetwork.Send(new Effect("assets/prefabs/misc/casino/slotmachine/effects/payout_jackpot.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);
        
        
        yield return CoroutineEx.waitForSeconds(0.7f);

        
        var container = new CuiElementContainer();
        container.Add(new CuiElement
        {
            Parent = Layer + ".main.div" + ".opencase.div" + ".items.div" + $".{WINNER_INDEX}",
            Components = {
                new CuiTextComponent { Text = $"x{defs[WINNER_INDEX].Amount}", Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.LowerCenter, Color = GRAY, FadeIn = 0.5f  },
                new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30.119 -50.437", OffsetMax = "30.119 -38.103" }
            }
        });

        container.Add(new CuiButton()
        {
            Button = { Color = GREEN, FadeIn = 0.5f, Command = $"UI_CASESS SKIP {defs[WINNER_INDEX].RewardID} -2 true {caseId}"},
            Text = { Text = "ЗАБРАТЬ", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FadeIn = 0.5f },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-63 -112", OffsetMax = "67 -92" }
        }, Layer + ".main.div" + ".opencase.div", Layer + ".main.div" + ".opencase.div" + ".buy" + ".1");
        
        container.Add(new CuiButton()
        {
            Button = { Color = RED_COLOR, FadeIn = 0.5f, Command = $"UI_CASESS SELL {defs[WINNER_INDEX].RewardID} -2 true {caseId}" },
            Text = { Text = $"ПРОДАТЬ ЗА {defs[WINNER_INDEX].Price}\u274d", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 9, FadeIn = 0.5f },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-63 -135", OffsetMax = "67 -115" }
        }, Layer + ".main.div" + ".opencase.div", Layer + ".main.div" + ".opencase.div" + ".sell" + ".1");
        CuiHelper.AddUi(player, container);
        
        
        _data[player.userID].OpeningRoutine = null;
    }


    private void UI_UpdateAnimatedOpeningItems(BasePlayer player, List<Configuration.ItemSettings> items, float currentOffset)
    {
        if (currentOffset == 0)
            CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".items.div");
        var container = new CuiElementContainer();
        container.Add(new CuiElement()
        {
            Name = Layer + ".main.div" + ".opencase.div" + ".items.div",
            Parent = Layer + ".main.div" + ".opencase.div",
            Update = currentOffset != 0,
            Components =
            {
                new CuiScrollViewComponent
                {
                    Vertical = false,
                    Horizontal = true,
                    MovementType = ScrollRect.MovementType.Unrestricted,
                    Elasticity = 0,
                    Inertia = false,
                    DecelerationRate = 0,
                    ScrollSensitivity = 0,
                    ContentTransform = new()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                        OffsetMin = $"-{currentOffset} 0",
                        OffsetMax = "0 0"
                    },
                    HorizontalScrollbar = null,
                    VerticalScrollbar = null
                },
                new CuiRectTransformComponent()
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-302.73 -86.619", OffsetMax = "302.73 70.001"
                }
            }
        });
        
        if (currentOffset == 0)
        {
            float minx = -320.8051f;
            float maxx = -192.7287f;
            float miny = -80.436684f;
            float maxy = 50.30998f;

            int i = 0;

            foreach (var item in items)
            {
                container.Add(new CuiElement()
                {
                    Parent = Layer + ".main.div" + ".opencase.div" + ".items.div",
                    Name = Layer + ".main.div" + ".opencase.div" + ".items.div" + $".{i}",
                    Update = currentOffset != 0,
                    FadeOut = 0.5f,
                    Components =
                    {
                        new CuiRawImageComponent() { Png = GuiManager.Get("item_background"), Color = GetColor(item.Chance) },
                        new CuiRectTransformComponent() { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{minx} {miny}", OffsetMax = $"{maxx} {maxy}" }
                    }
                });
                container.Add(item.GetImageNonCached("0.5 0.5", "0.5 0.5", "-35.846 -35.846", "35.846 35.846", Layer + ".main.div" + ".opencase.div" + ".items.div" + $".{i}", update: currentOffset != 0, name: Layer + ".caseitem" + $".{i}.img", fadeOut:0.5f));
                // container.Add(new CuiLabel()
                //     {
                //         Text = { Text = $"{i}", Align = TextAnchor.MiddleCenter },
                //         RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
                //     }, Layer + ".main.div" + ".opencase.div" + ".items.div" + $".{i}");
                minx += 130.667f;
                maxx += 130.667f;
                i++;
            }

            container.Add(new CuiElement()
            {
                Name = Layer + ".main.div" + ".opencase.div" + ".pointer",
                Parent = Layer + ".main.div" + ".opencase.div",
                FadeOut = 0.5f,
                Components =
                {
                    new CuiImageComponent() { Color = ORANGE_COLOR },
                    new CuiRectTransformComponent() { AnchorMin = "0.5 0", AnchorMax = "0.5 1", OffsetMin = "-1 20", OffsetMax = "1 -65"}
                }
            });
        }

        CuiHelper.AddUi(player, container);
    }
    
    #endregion

    [PluginReference] private Plugin MenuBase;
    private string GetImage(string key)
    {
        return (string)MenuBase.Call("API_GetImage", key);
    }
		
    private int GetBalanceCoins(BasePlayer player)
    {
#if DEBUGSERVER
			return 100000;
#endif
        if (!IQEconomic)
            return -1;
        return (int)IQEconomic.Call("API_GET_BALANCE", player.UserIDString);
    }
    
    private const string GRADIENT_RIGHT = "assets/content/ui/ui.background.transparent.linearltr.tga";
    private const string GRADIENTDOWN_COLOR = "0 0 0 0.7";
		
    private const string WHITE_TRANSPARENT_BACKGROUND = "1 1 1 0.3";
    private const string ORANGE_COLOR = "0.9490196 0.5019608 0.05490196 1";
    private const string BACKGROUND_COLOR = "0.3568628 0.3568628 0.3568628 0.75";

    private const string TEXT_COLOR = "1 1 1 1";

    private const string RED_COLOR = "0.6901961 0.3490196 0.3490196 0.8";
    private const string GREEN = "0.247 0.933 0.0 0.26";
    private const string Layer = "ui.MenuBase.bg";
    private const string GRAY = "0.847 0.847 0.847 1";
    private void UI_DrawMain(BasePlayer player)
    {
        UI_DrawCasesInventory(player);
    }

    private int GetCaseAmount(BasePlayer player, string key)
    {
        if (!_data[player.userID].CaseList.TryGetValue(key, out var amount))
            return 0;
        return amount;
    }

    [ConsoleCommand("mb.case.fastopenswitch")]
    private void cmdFastOpenSwitch(ConsoleSystem.Arg arg)
    {
        if (arg.Player() == null)
            return;

        _data[arg.Player().userID].FastOpenCase = !_data[arg.Player().userID].FastOpenCase;
        EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", arg.Player(), 0, new Vector3(), new Vector3()), arg.Player().Connection);
        UI_UpdateCaseOpenAmount(arg.Player(), arg.Args[0], 1);
        UI_UpdateFastOpenMarker(arg.Player());
    }
    
    [ConsoleCommand("mb.case.opencase")]
    private void cmdOpenCase(ConsoleSystem.Arg arg)
    {
        if (arg.Player() == null || !arg.HasArgs(2))
            return;

        string caseId = arg.Args[0];
        if (!int.TryParse(arg.Args[1], out var amount))
            return;
        amount = Mathf.Clamp(amount, 1, 5);
        var player = arg.Player();

        if (GetCaseAmount(player, caseId) < amount)
            return;

        if (_data[player.userID].IsOpeningCase())
            return;
        
        var fastOpen = _data[player.userID].FastOpenCase;
        _data[player.userID].CaseList[caseId] -= !fastOpen ? 1 : amount;
        EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);
        UI_UpdateCaseAmount(player, caseId);
        UI_UpdateCaseOpenAmount(player, caseId, amount);
        if (fastOpen)
            OpenCaseFast(player, _config.CaseList.FirstOrDefault(x => x.Id == caseId), amount);
        else
            OpenCaseAnimation(player, _config.CaseList.FirstOrDefault(x => x.Id == caseId));
    }

    private void OpenCaseAnimation(BasePlayer player, Configuration.Case @case)
    {
        if (_data.TryGetValue(player.userID, out var data))
            if (data.IsOpeningCase())
                return;
        
        var rewardList = _config.RewardList.Where(p => @case.ItemList.Contains(p.RewardID));
        var prizeList = GetRewardList(rewardList, 150);
        _data[player.userID].OpeningRoutine = player.StartCoroutine(OpenRoutine(player, prizeList, 35810, @case.Id));
    }

    private void OpenCaseFast(BasePlayer player, Configuration.Case @case, int amountToOpen = 1)
    {
        for (int i = 0; i <= 5; i++)
        {
            CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".sell" + $".{i}");
            CuiHelper.DestroyUi(player, Layer + ".main.div" + ".opencase.div" + ".buy" + $".{i}");
        }
        
        var container = new CuiElementContainer();
        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "1 1 1 0" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-302.73 -96.619", OffsetMax = "302.73 60.001" }
        }, Layer + ".main.div" + ".opencase.div", Layer + ".main.div" + ".opencase.div" + ".items.div", Layer + ".main.div" + ".opencase.div" + ".items.div");

        float baseOffsetX = 110.667f;
        float baseOffsetY = 10f;
        float centerOffsetX = -baseOffsetX * (amountToOpen - 1) / 2;

        float fadeIn = 5f;

        var rewardList = _config.RewardList.Where(p => @case.ItemList.Contains(p.RewardID));
        var prizeList = GetRewardList(rewardList, amountToOpen);

        for (int i = 0; i < prizeList.Count; i++)
        {
            var item = prizeList[i];
            float offsetX = centerOffsetX + i * baseOffsetX;

            container.Add(new CuiElement()
            {
                Parent = Layer + ".main.div" + ".opencase.div" + ".items.div",
                Name = Layer + ".main.div" + ".opencase.div" + ".items.div" + $".{i}",
                Components =
                {
                    new CuiRawImageComponent() { Png = GuiManager.Get("item_background"), Color = GetColor(item.Chance), FadeIn = fadeIn },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = $"{offsetX - 50} {baseOffsetY - 50}",
                        OffsetMax = $"{offsetX + 50} {baseOffsetY + 50}"
                    }
                }
            });

            container.Add(item.GetImageNonCached("0.5 0.5", "0.5 0.5", "-30.846 -30.846", "30.846 30.846", Layer + ".main.div" + ".opencase.div" + ".items.div" + $".{i}", null, fadeIn));
            
            container.Add(new CuiElement
            {
                Parent = Layer + ".main.div" + ".opencase.div" + ".items.div" + $".{i}",
                Components = {
                    new CuiTextComponent { Text = $"x{item.Amount}", Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.LowerCenter, Color = GRAY, FadeIn = fadeIn  },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30.119 -42.437", OffsetMax = "30.119 -30.103" }
                }
            });
            container.Add(new CuiButton()
            {
                Button = { Color = GREEN, FadeIn = fadeIn, Command = $"UI_CASESS SKIP {item.RewardID} -2 {i}"},
                Text = { Text = "ЗАБРАТЬ", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FadeIn = fadeIn, FontSize = 12},
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -73", OffsetMax = "50 -53" }
            }, Layer + ".main.div" + ".opencase.div" + ".items.div" + $".{i}", Layer + ".main.div" + ".opencase.div" + ".buy" + $".{i}");
        
            container.Add(new CuiButton()
            {
                Button = { Color = RED_COLOR, FadeIn = fadeIn, Command = $"UI_CASESS SELL {item.RewardID} -2 {i}" },
                Text = { Text = $"ПРОДАТЬ ЗА {item.Price}\u274d", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 9, FadeIn = fadeIn },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -95", OffsetMax = "50 -75" }
            }, Layer + ".main.div" + ".opencase.div" + ".items.div" + $".{i}", Layer + ".main.div" + ".opencase.div" + ".sell" + $".{i}");
            CuiHelper.AddUi(player, container);
        }

        _data[player.userID].Inventory.AddRange(prizeList.Select(x => x.RewardID));
        
        CuiHelper.AddUi(player, container);
    }

    
    [ConsoleCommand("mb.case.drawcase")]
    private void cmdDrawCase(ConsoleSystem.Arg arg)
    {
        if (arg.Player() == null || !arg.HasArgs())
            return;

        string caseId = arg.Args[0];
        
        UI_DrawCase(arg.Player(), _config.CaseList.FirstOrDefault(x => x.Id == caseId));
        EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", arg.Player(), 0, new Vector3(), new Vector3()), arg.Player().Connection);
    }
    [ConsoleCommand("mb.case.open")]
    private void cmdCaseOpen(ConsoleSystem.Arg arg)
    {
        if (arg.Player() == null)
            return;
        
        UI_DrawMain(arg.Player());
    }

    [ConsoleCommand("mb.case.givecase")]
    private void cmdGiveCase(ConsoleSystem.Arg arg)
    {
        if (arg.Player() != null && !arg.Player().IsAdmin)
            return;
        
        string caseId = arg.Args[0];
        if (!int.TryParse(arg.Args[1], out var amount))
            return;
        if (!ulong.TryParse(arg.Args[2], out var userid))
            return;

        if (!_data.TryGetValue(userid, out var value))
            return;

        if (!value.CaseList.ContainsKey(caseId))
            value.CaseList.Add(caseId, 0);
        
        value.CaseList[caseId] += amount;
    }
    
    [ConsoleCommand("mb.case.buy")]
    private void cmdBuyCase(ConsoleSystem.Arg arg)
    {
        if (arg.Player() == null || !arg.HasArgs(2))
            return;

        string caseId = arg.Args[0];
        if (!int.TryParse(arg.Args[1], out var amount))
            return;

        var player = arg.Player();
        var balance = GetBalanceCoins(arg.Player());
        var @case = _config.CaseList.FirstOrDefault(x => x.Id == caseId);
        
        if (balance < @case.Price * amount)
            return;
        TakeBalance(player, @case.Price * amount);
        
        EffectNetwork.Send(new("assets/prefabs/deployable/vendingmachine/effects/vending-machine-purchase-human.prefab", player, 0, new(), new()), player.Connection);
        if (!_data[player.userID].CaseList.ContainsKey(caseId))
            _data[player.userID].CaseList.Add(caseId, 0);

        _data[player.userID].CaseList[caseId] += amount;
        UI_UpdateCaseAmount(player, caseId);
        UI_UpdateBalance(player);
        UI_UpdateCaseOpenAmount(player, caseId, 1);
    }
    
    [ConsoleCommand("mb.case.buyselector")]
    private void cmdBuyCaseSelector(ConsoleSystem.Arg arg)
    {
        if (arg.Player() == null || !arg.HasArgs(2))
            return;

        string caseId = arg.Args[0];
        if (!int.TryParse(arg.Args[1], out var amount))
        {
            return;
        }

        amount = Mathf.Clamp(amount, 1, 5);
        EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", arg.Player(), 0, new Vector3(), new Vector3()), arg.Player().Connection);
        UI_DrawBuyCases(arg.Player(), _config.CaseList.FirstOrDefault(x => x.Id == caseId), amount);
    }

    [ConsoleCommand("mb.case.openamount")]
    private void cmdOpenAmount(ConsoleSystem.Arg arg)
    {
        if (arg.Player() == null || !arg.HasArgs(2))
            return;
        
        string caseId = arg.Args[0];
        if (!int.TryParse(arg.Args[1], out var amount))
            return;
        EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", arg.Player(), 0, new Vector3(), new Vector3()), arg.Player().Connection);
        UI_UpdateCaseOpenAmount(arg.Player(), caseId, amount);
    }

    [ConsoleCommand("mb.case.drawcaseinventory")]
    private void cmdCasesList(ConsoleSystem.Arg arg)
    {
        if (arg.Player() == null)
            return;
        
        EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", arg.Player(), 0, new Vector3(), new Vector3()), arg.Player().Connection);
        cmdClose(arg);
        UI_DrawCasesInventory(arg.Player());
    }

    [ConsoleCommand("mb.case.drawplayerinventory")]
    private void cmdDrawPlayerInventory(ConsoleSystem.Arg arg)
    {
        if (arg.Player() == null)
            return;
        EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", arg.Player(), 0, new Vector3(), new Vector3()), arg.Player().Connection);

        cmdClose(arg);
        
        UI_DrawInventory(arg.Player());
    }

    [ConsoleCommand("mb.case.updatetargetitem")]
    private void cmdUpdateTargetItem(ConsoleSystem.Arg arg)
    {
        if (arg.Player() == null)
            return;
        
        EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", arg.Player(), 0, new Vector3(), new Vector3()), arg.Player().Connection);
        UI_UpdateTargetedInventoryItem(arg.Player(), arg.Args[0], arg.Args[1].ToInt());
    }

    private void UI_UpdateFastOpenMarker(BasePlayer player)
    {
        var flag = _data[player.userID].FastOpenCase;
        
        var container = new CuiElementContainer();
        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = flag ? ORANGE_COLOR : RED_COLOR, Sprite = "assets/icons/circle_closed_white.png" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{(flag ? 3.363 : -10.197)} -3.917", OffsetMax = $"{(flag ? 10.197 : -3.363)} 3.917" }
        }, Layer + ".main.div" + ".fastopen.div" + ".marker.rounded.div", Layer + ".main.div" + ".fastopen.div" + ".marker.rounded.div" + ".marker", Layer + ".main.div" + ".fastopen.div" + ".marker.rounded.div" + ".marker");
        CuiHelper.AddUi(player, container);
    }

    private void UI_DrawFastOpenMarker(BasePlayer player, string caseId)
    {
        var container = new CuiElementContainer();
        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "1 1 1 0" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "108.335 -15.565", OffsetMax = "267.422 15.005" }
        }, Layer + ".main.div", Layer + ".main.div" + ".fastopen.div", Layer + ".main.div" + ".fastopen.div");

        container.Add(new CuiButton
        {
            Button = { Color = GRADIENTDOWN_COLOR, Sprite = "assets/content/ui/ui.rounded.tga", Command = $"mb.case.fastopenswitch {caseId}"},
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-73.2 -5.8", OffsetMax = "-49.833 5.8" }
        }, Layer + ".main.div" + ".fastopen.div", Layer + ".main.div" + ".fastopen.div" + ".marker.rounded.div");

        

        container.Add(new CuiElement
        {
            Name = Layer + ".main.div" + ".fastopen.div" + ".text",
            Parent = Layer + ".main.div" + ".fastopen.div",
            Components = {
                new CuiTextComponent { Text = "Включить быстрое открытие?", Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-43.322 -15.285", OffsetMax = "99.922 15.285" }
            }
        });

        CuiHelper.AddUi(player, container);
        
        UI_UpdateFastOpenMarker(player);
    }
    
    private void UI_UpdateTargetedInventoryItem(BasePlayer player, string rewardKey, int rewardId = -1)
    {
        var container = new CuiElementContainer();
        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0.3568628 0.3568628 0.3568628 0.75" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-314.69 -229.23", OffsetMax = "314.69 -167.288" }
        }, Layer + ".main.div", Layer + ".main.div" + ".takepanel.div", Layer + ".main.div" + ".takepanel.div");
        CuiHelper.AddUi(player, container);
        
        if (string.IsNullOrEmpty(rewardKey) || rewardId == -1)
            return;
        
        var item = RewardsCache[rewardKey];
        var itemDef = ItemManager.FindItemDefinition(item.Shortname);
        

        container.Add(new CuiElement
        {
            Name = Layer + ".main.div" + ".takepanel.div" + ".name",
            Parent = Layer + ".main.div" + ".takepanel.div",
            Components = {
                new CuiTextComponent { Text = $"{(string.IsNullOrEmpty(item.displayName) ? itemDef.displayName.translated : item.displayName)} x{item.Amount}", Font = "robotocondensed-bold.ttf", FontSize = 21, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-296.4 -30.971", OffsetMax = "47.094 30.971" }
            }
        });

        container.Add(new CuiButton
        {
            Button = { Color = GREEN, Command = $"UI_CASESS TAKEREWARD {rewardKey} {rewardId}"},
            Text = { Text = "ЗАБРАТЬ", Font = "robotocondensed-regular.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "93.742 -14.406", OffsetMax = "189.458 14.406" }
        }, Layer + ".main.div" + ".takepanel.div", Layer + ".main.div" + ".takepanel.div" + ".take.btn");

        container.Add(new CuiButton
        {
            Button = { Color = RED_COLOR, Command = $"UI_CASESS SELL {rewardKey} {rewardId}"},
            Text = { Text = $"ПРОДАТЬ ЗА {item.Price}\u274d", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "192.942 -14.406", OffsetMax = "300.852 14.406" }
        }, Layer + ".main.div" + ".takepanel.div", Layer + ".main.div" + ".takepanel.div" + ".sell.btn");
        CuiHelper.AddUi(player, container);
    }

    [ConsoleCommand("mb.case.close")]
    private void cmdClose(ConsoleSystem.Arg arg)
    {
        if (arg.Player() == null)
            return;
        
        if (_data.TryGetValue(arg.Player().userID, out var data))
            if (data.OpeningRoutine != null)
            {
                arg.Player().StopCoroutine(data.OpeningRoutine);
                data.OpeningRoutine = null;
            }
    }
    private void UI_DrawInventory(BasePlayer player)
    {
        var container = new CuiElementContainer();
        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0.3568628 0.3568628 0.3568628 0" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-229.801 -229.232", OffsetMax = "399.579 229.228" }
        }, Layer, Layer + ".main.div", Layer + ".main.div");
        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0.3568628 0.3568628 0.3568628 0.75" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-314.69 -162.695", OffsetMax = "314.69 229.23" }
        }, Layer + ".main.div", Layer + ".main.div" + ".bg");
        container.Add(new CuiButton
        {
            Button = { Color = RED_COLOR, Close = Layer + ".blur", Command = "mb.case.close" },
            Text = { Text = "X", Font = "permanentmarker.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = TEXT_COLOR },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "285.139 199.674", OffsetMax = "314.69 229.226" }
        }, Layer + ".main.div", Layer + ".main.div" + ".close");
        container.Add(new CuiElement
        {
            Name = Layer + ".main.div" + ".label",
            Parent = Layer + ".main.div",
            Components = {
                new CuiTextComponent { Text = "ИНВЕНТАРЬ", Font = "robotocondensed-bold.ttf", FontSize = 27, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-272.556 176.994", OffsetMax = "71.556 229.226" }
            }
        });

        container.Add(new CuiButton
        {
            Button = { Color = WHITE_TRANSPARENT_BACKGROUND, Command = "mb.case.drawcaseinventory" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-302.555 192.696", OffsetMax = "-280.245 215.005" }
        }, Layer + ".main.div", Layer + ".main.div" + ".toInventory");

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "1 1 1 1", Sprite = "assets/icons/dir_left.png"},
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-7.252 -7.252", OffsetMax = "7.252 7.252" }
        }, Layer + ".main.div" + ".toInventory");

        container.Add(new CuiElement
        {
            Name = Layer + ".main.div" + ".description",
            Parent = Layer + ".main.div",
            Components = {
                new CuiTextComponent { Text = "Сюда попадают все ваши вещи, выбитые из кейсов\nВы можете их забрать в любой момент, здесь они сохраняются навсегда", Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-129.479 183.586", OffsetMax = "252.479 215.614" }
            }
        });
        
        CuiHelper.AddUi(player, container);
        
        UI_UpdateTargetedInventoryItem(player, _data[player.userID].Inventory.ElementAtOrDefault(0), -1);
        UI_UpdateInventoryItems(player, -1);
    }

    [ConsoleCommand("tet")]
    private void cmasdsaD(ConsoleSystem.Arg arg)
    {
        if (arg.Player().IsAdmin)
        {
            for (int i = 0; i < 35; i++)
                _data[arg.Player().userID].Inventory.Add(RewardsCache.ToList().GetRandom().Key);
        }
    }
    private void UI_UpdateInventoryItems(BasePlayer player, int updatePos = -1)
    {
        var container = new CuiElementContainer();
        if (updatePos != -1)
        {
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { FadeIn = 1f, Color = "0.5 1 0.5 0.2" },
                Text = { Text = "\u2714", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", Color = "0.7 1 0.7 1", FontSize = 22 }
            }, Layer + ".main.div" + ".items" + $".{updatePos}");
            CuiHelper.AddUi(player, container);
            return;
        }
        container.Add(new CuiElement()
        {
            Name = Layer + ".main.div" + ".items",
            DestroyUi = Layer + ".main.div" + ".items",
            Parent = Layer + ".main.div",
            Components =
            {
                new CuiScrollViewComponent
                {
                    Vertical = true,
                    Horizontal = false,
                    MovementType = ScrollRect.MovementType.Unrestricted,
                    Elasticity = 0,
                    Inertia = false,
                    DecelerationRate = 0,
                    ScrollSensitivity = 20,
                    ContentTransform = new()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1"
                    },
                    HorizontalScrollbar = null,
                    VerticalScrollbar = new()
                    {
                        Invert = false,
                        AutoHide = false,
                        HandleSprite = null,
                        Size = 2,
                        HandleColor = ORANGE_COLOR,
                        HighlightColor = ORANGE_COLOR,
                        PressedColor = ORANGE_COLOR,
                        TrackSprite = null,
                        TrackColor = "0 0 0 0.4"
                    }
                },
                new CuiRectTransformComponent()
                {
                    AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "22.229 -367.174", OffsetMax = "620.771 -52.006"
                }
            }
        });
        container.Add(new CuiElement()
        {
            Parent = Layer + ".main.div" + ".items",
            Components =
            {
                new CuiImageComponent() { Color = "0 0 0 0" },
                new CuiRectTransformComponent() { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -10000", OffsetMax = "10000 0"}
            }
        });
        
        
        float minx = 0.2318039f;
        float maxx = 79.3082f;
        float miny = -75.28664f;
        float maxy = -0.413353f;

        int i = 0;
        
        foreach (var x in _data[player.userID].Inventory)
        {
            if (i % 7 == 0 && i != 0)
            {
                minx = 0.2318039f;
                maxx = 79.3082f;
                miny -= 80.05f;
                maxy -= 80.05f;
            }

            if (!RewardsCache.TryGetValue(x, out var item))
                continue;

            var itemDef = ItemManager.FindItemDefinition(item.Shortname);
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{minx} {miny}", OffsetMax = $"{maxx} {maxy}" }
            }, Layer + ".main.div" + ".items", Layer + ".main.div" + ".items" + $".{i}" + ".sitplace");
            
            container.Add(new CuiElement()
            {
                Parent = Layer + ".main.div" + ".items" + $".{i}" + ".sitplace",
                Name = Layer + ".main.div" + ".items" + $".{i}",
                Components =
                {
                    new CuiRawImageComponent() { Png = GuiManager.Get("item_background"), Color = GetColor(item.Chance) },
                    new CuiRectTransformComponent() { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });
            
            container.Add(item.GetImage("0.5 0.5", "0.5 0.5", "-26.846 -26.846", "26.846 26.846",
                Layer + ".main.div" + ".items" + $".{i}"));

            container.Add(new CuiElement
            {
                Parent = Layer + ".main.div" + ".items" + $".{i}",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = (string.IsNullOrEmpty(item.displayName)
                            ? itemDef.displayName.translated
                            : item.displayName),
                        Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-39.538 25.437",
                        OffsetMax = "39.538 37.103"
                    }
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer + ".main.div" + ".items" + $".{i}",
                Components = {
                    new CuiTextComponent { Text = $"x{item.Amount}", Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = GRAY },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-39.538 -37.437", OffsetMax = "39.538 -25.103" }
                }
            });
            container.Add(new CuiButton()
                {
                    Button = { Color = "0 0 0 0", Command = $"mb.case.updatetargetitem {x} {i}" },
                    Text = { Text = ""},
                    RectTransform =
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1"
                    }
                }, Layer + ".main.div" + ".items" + $".{i}");
            
            minx += 84.43f;
            maxx += 84.43f;
            i++;
        }
    
        (container[0].Components[0] as CuiScrollViewComponent).ContentTransform.OffsetMin = $"0 {Mathf.Min(miny, -315.4366f)}";
        if (i / 7 <= 3)
        {
            (container[0].Components[0] as CuiScrollViewComponent).VerticalScrollbar = null;
        }
        CuiHelper.AddUi(player, container);
    }
    
    private void UI_UpdateCaseAmount(BasePlayer player, string id)
    {
        var container = new CuiElementContainer();
        container.Add(new CuiElement
        {
            Name = Layer + ".main.div" + ".case.div" + ".caseamount.div" + ".amount",
            DestroyUi = Layer + ".main.div" + ".case.div" + ".caseamount.div" + ".amount",
            Parent = Layer + ".main.div" + ".case.div" + ".caseamount.div",
            Components = {
                new CuiTextComponent { Text = $"У ВАС: {GetCaseAmount(player, id)}", Font = "robotocondensed-bold.ttf", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-56.476 -7.496", OffsetMax = "56.474 7.496" }
            }
        });
        CuiHelper.AddUi(player, container);
    }
    private void UI_DrawBuyCases(BasePlayer player, Configuration.Case @case, int currentAmount)
    {
        var balance = GetBalanceCoins(player);
        var container = new CuiElementContainer();
        container.Add(new CuiPanel
        {
	        CursorEnabled = false,
	        Image = { Color = "1 1 1 0" },
	        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "98.1 -40.106", OffsetMax = "254.5 32.361" }
        }, Layer + ".main.div" + ".case.div", Layer + ".main.div" + ".case.div" + ".buycases.div", Layer + ".main.div" + ".case.div" + ".buycases.div");

        container.Add(new CuiElement
        {
	        Name = Layer + ".main.div" + ".case.div" + ".buycases.div" + ".label",
	        Parent = Layer + ".main.div" + ".case.div" + ".buycases.div",
	        Components = {
					        new CuiTextComponent { Text = "ПРИОБРЕСТИ КЕЙСЫ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
					        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-78.2 20.685", OffsetMax = "78.2 36.234" }
				        }
        });

        container.Add(new CuiButton
        {
	        Button = { Color = balance < @case.Price * currentAmount ? RED_COLOR : GREEN, Command = $"mb.case.buy {@case.Id} {currentAmount}"},
	        Text = { Text = $"КУПИТЬ ЗА {@case.Price * currentAmount}\u274d", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
	        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-78.2 -8.612", OffsetMax = "78.2 20.685" }
        }, Layer + ".main.div" + ".case.div" + ".buycases.div", Layer + ".main.div" + ".case.div" + ".buycases.div" + ".buy.btn");

        container.Add(new CuiPanel
        {
	        CursorEnabled = false,
	        Image = { Color = "1 1 1 0" },
	        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-66.7 -36.234", OffsetMax = "67.076 -12.994" }
        }, Layer + ".main.div" + ".case.div" + ".buycases.div", Layer + ".main.div" + ".case.div" + ".buycases.div" + ".amountselector.div");

        float minx = -66.888f;
        float maxx = -43.648f;  
        float miny = -11.62003f;
        float maxy = 11.61997f;
        for (int i = 0; i < 5; i++) {

            container.Add(new CuiButton
            {
                Button = { Color = currentAmount == i + 1 ? ORANGE_COLOR : WHITE_TRANSPARENT_BACKGROUND, Command = $"mb.case.buyselector {@case.Id} {i + 1}"},
                Text = { Text = $"{i + 1}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{minx} {miny}", OffsetMax = $"{maxx} {maxy}" }
            }, Layer + ".main.div" + ".case.div" + ".buycases.div" + ".amountselector.div", Layer + ".main.div" + ".case.div" + ".buycases.div" + ".amountselector.div" + $".{i}");
		    minx -= -27.668f;
		    maxx -= -27.668f;
        }

        CuiHelper.AddUi(player, container);
    }
    private void UI_DrawCase(BasePlayer player, Configuration.Case @case)
    {
        var container = new CuiElementContainer();
        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0.3568628 0.3568628 0.3568628 0.75" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-229.801 -229.232", OffsetMax = "399.579 229.228" }
        }, Layer, Layer + ".main.div", Layer + ".main.div");
        container.Add(new CuiButton
        {
            Button = { Color = RED_COLOR, Close = Layer + ".blur", Command = "mb.case.close" },
            Text = { Text = "X", Font = "permanentmarker.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = TEXT_COLOR },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "285.139 199.674", OffsetMax = "314.69 229.226" }
        }, Layer + ".main.div", Layer + ".main.div" + ".close");
        container.Add(new CuiElement
        {
            Name = Layer + ".main.div" + ".label",
            Parent = Layer + ".main.div",
            Components = {
                new CuiTextComponent { Text = "КЕЙСЫ", Font = "robotocondensed-bold.ttf", FontSize = 27, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-277.323 176.992", OffsetMax = "66.788 229.225" }
            }
        });
        container.Add(new CuiButton
        {
            Button = { Color = ORANGE_COLOR, Command = "mb.case.drawplayerinventory" },
            Text = { Text = "" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-192.7 192.693", OffsetMax = "-93.087 214" }
        }, Layer + ".main.div", Layer + ".main.div" + ".inventory.btn");

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "1 1 1 1", Sprite = "assets/icons/backpack.png" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-42.384 -7.184", OffsetMax = "-28.016 7.184" }
        }, Layer + ".main.div" + ".inventory.btn");

        container.Add(new CuiElement
        {
            Parent = Layer + ".main.div" + ".inventory.btn",
            Components = {
                new CuiTextComponent { Text = "ИНВЕНТАРЬ", Font = "robotocondensed-regular.ttf", FontSize = 13, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-21.66 -11.154", OffsetMax = "50 11.154" }
            }
        });

        container.Add(new CuiButton
        {
            Button = { Color = WHITE_TRANSPARENT_BACKGROUND, Command = "mb.case.drawcaseinventory" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-302.555 192.696", OffsetMax = "-280.245 215.005" }
        }, Layer + ".main.div", Layer + ".main.div" + ".toInventory");

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "1 1 1 1", Sprite = "assets/icons/dir_left.png"},
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-7.252 -7.252", OffsetMax = "7.252 7.252" }
        }, Layer + ".main.div" + ".toInventory");
        
        
        
        
        container.Add(new CuiPanel
        {
	        CursorEnabled = false,
	        Image = { Color = "1 1 1 0.2" },
	        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-302.55 30.929", OffsetMax = "302.908 176.99" }
        }, Layer + ".main.div", Layer + ".main.div" + ".case.div");

        container.Add(new CuiPanel
        {
	        CursorEnabled = false,
	        Image = { Color = "0 0 0 0.5" },
	        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-273.1 -46.297", OffsetMax = "-93.171 45.8" }
        }, Layer + ".main.div" + ".case.div", Layer + ".main.div" + ".case.div" + ".about.div");

        container.Add(new CuiElement
        {
	        Name = Layer + ".main.div" + ".case.div" + ".about.div" + ".name",
	        Parent = Layer + ".main.div" + ".case.div" + ".about.div",
	        Components = {
					        new CuiTextComponent { Text = @case.DisplayName, Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
					        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-89.965 22.467", OffsetMax = "89.965 46.049" }
				        }
        });

        container.Add(new CuiElement
        {
	        Name = Layer + ".main.div" + ".case.div" + ".about.div" + ".description",
	        Parent = Layer + ".main.div" + ".case.div" + ".about.div",
	        Components = {
					        new CuiTextComponent { Text = @case.Description, Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
					        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-76.599 -33.982", OffsetMax = "76.599 22.467" }
				        }
        });

        container.Add(new CuiElement()
        {
            Parent = Layer + ".main.div" + ".case.div",
            Components =
            {
                new CuiRawImageComponent() { Png = GuiManager.Get(@case.Image) },
                new CuiRectTransformComponent() { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-56.474 -52.18", OffsetMax = "56.474 60.768" }
            }
        });

        container.Add(new CuiPanel
        {
	        CursorEnabled = false,
	        Image = { Color = "0 0 0 0.7" },
	        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-56.475 -71.212", OffsetMax = "56.475 -56.22" }
        }, Layer + ".main.div" + ".case.div", Layer + ".main.div" + ".case.div" + ".caseamount.div");

        CuiHelper.AddUi(player, container);
        
        UI_UpdateCaseAmount(player, @case.Id);
        UI_DrawBuyCases(player, @case, 1);
        UI_DrawCaseItems(player, @case);
        UI_UpdateCaseOpenAmount(player, @case.Id, 1);
        UI_UpdateBalance(player);
        UI_DrawFastOpenMarker(player, @case.Id);
    }

    private Configuration.ItemSettings GetItemData(string id)
    {
        if (!RewardsCache.TryGetValue(id, out var itemSettings))
            return new()
            {
                RewardID = "ERROR",
                Shortname = "coal",
                Amount = int.MaxValue,
                Chance = 0,
                displayName = null,
                Price = 0,
                ItemImage = null,
                SkinID = 0,
                Command = null
            };
        return itemSettings;
    }

    private void UI_UpdateCaseOpenAmount(BasePlayer player, string caseId, int amount)
    {
        var container = new CuiElementContainer();
        
        var caseAmount = GetCaseAmount(player, caseId);
        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "1 1 1 0" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-270.725 84.843", OffsetMax = "-115.164 98.2" }
        }, Layer + ".main.div" + ".opencase.div", Layer + ".main.div" + ".opencase.div" + ".amounttoopen.div", Layer + ".main.div" + ".opencase.div" + ".amounttoopen.div");

        container.Add(new CuiButton
        {
            Button = { Color = caseAmount >= amount ? GREEN : RED_COLOR, Command = caseAmount >= amount ? $"mb.case.opencase {caseId} {amount}" : ""},
            Text = { Text = amount > 1 ? "ОТКРЫТЬ КЕЙСЫ" : "ОТКРЫТЬ КЕЙС", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "105.511 -15.473", OffsetMax = "279.149 15.097" }
        }, Layer + ".main.div" + ".opencase.div" + ".amounttoopen.div", Layer + ".main.div" + ".opencase.div" + ".open.btn");
        CuiHelper.AddUi(player, container);
        container.Clear();
        
        if (!_data[player.userID].FastOpenCase)
        {
            return;
        }
        
        container.Add(new CuiElement
        {
            Parent = Layer + ".main.div" + ".opencase.div" + ".amounttoopen.div",
            Components = {
                new CuiTextComponent { Text = "ОТКРЫТЬ КЕЙСОВ", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-77.781 -10.53", OffsetMax = "0 10.53" }
            }
        });

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "1 1 1 0" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "1.562 -6.679", OffsetMax = "77.78 6.679" }
        }, Layer + ".main.div" + ".opencase.div" + ".amounttoopen.div", Layer + ".main.div" + ".opencase.div" + ".amounttoopen.div" + ".amount.div");

        float minx = -38.109f;
        float maxx = -24.751f;  
        float miny = -6.679f;
        float maxy = 6.679f;
        for (int i = 0; i < 5; i++) 
        {
            container.Add(new CuiButton
            {
                Button = { Color = i + 1 == amount ? ORANGE_COLOR : WHITE_TRANSPARENT_BACKGROUND, Command = i + 1 == amount ? "" : $"mb.case.openamount {caseId} {i + 1}" },
                Text = { Text = $"{i + 1}", Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{minx} {miny}", OffsetMax =  $"{maxx} {maxy}" }
            }, Layer + ".main.div" + ".opencase.div" + ".amounttoopen.div" + ".amount.div", Layer + ".main.div" + ".opencase.div" + ".amounttoopen.div" + ".amount.div" + $".{i + 1}");
            minx -= -15.63f;
            maxx -= -15.63f;
        }

        
        CuiHelper.AddUi(player, container);
    }
    private void UI_DrawCaseItems(BasePlayer player, Configuration.Case @case)
    {
        var container = new CuiElementContainer();
        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "1 1 1 0" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-302.551 -198.231", OffsetMax = "302.909 15.005" }
        }, Layer + ".main.div", Layer + ".main.div" + ".opencase.div", Layer + ".main.div" + ".opencase.div");
        container.Add(new CuiPanel
        {
	        CursorEnabled = false,
	        Image = { Color = "1 1 1 0" },
	        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-302.73 -106.619", OffsetMax = "302.73 50.001" }
        }, Layer + ".main.div" + ".opencase.div", Layer + ".main.div" + ".opencase.div" + ".items.div", Layer + ".main.div" + ".opencase.div" + ".items.div");

        float minx = -302.8051f;
        float maxx = -223.7287f;
        float miny = 3.436684f;
        float maxy = 78.30998f;

        int i = 0;

        foreach (var x in @case.ItemList)
        {
            if (i % 7 == 0 && i != 0)
            {
                minx = -302.8051f;
                maxx = -223.7287f;
                miny -= 81.746f;
                maxy -= 81.746f;
            }
            var item = GetItemData(x);

            container.Add(new CuiElement()
            {
                Parent = Layer + ".main.div" + ".opencase.div" + ".items.div",
                Name = Layer + ".main.div" + ".opencase.div" + ".items.div" + $".{x}",
                Components =
                {
                    new CuiRawImageComponent() { Png = GuiManager.Get("item_background"), Color = GetColor(item.Chance) },
                    new CuiRectTransformComponent() { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{minx} {miny}", OffsetMax = $"{maxx} {maxy}" }
                }
            });

            container.Add(item.GetImage("0.5 0.5", "0.5 0.5", "-26.846 -26.846", "26.846 26.846", Layer + ".main.div" + ".opencase.div" + ".items.div" + $".{x}"));

            container.Add(new CuiElement
            {
                Parent = Layer + ".main.div" + ".opencase.div" + ".items.div" + $".{x}",
                Components = {
                    new CuiTextComponent { Text = $"x{item.Amount}", Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.LowerLeft, Color = GRAY },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-35.119 -37.437", OffsetMax = "35.119 -25.103" }
                }
            });

            container.Add(new CuiElement
            {
                Parent = Layer + ".main.div" + ".opencase.div" + ".items.div" + $".{x}",
                Components = {
                    new CuiTextComponent { Text = $"{item.Chance}%", Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.LowerRight, Color = GRAY},
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-35.119 -37.437", OffsetMax = "35.119 -25.103" }
                }
            });
            minx += 86.667f;
            maxx += 86.667f;
            i++;
        }

        CuiHelper.AddUi(player, container);
    }

    private void UI_DrawCasesInventory(BasePlayer player)
    {
        var container = new CuiElementContainer();
        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0.3568628 0.3568628 0.3568628 0.75" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-229.801 -229.232", OffsetMax = "399.579 229.228" }
        }, Layer, Layer + ".main.div", Layer + ".main.div");
        container.Add(new CuiButton
        {
            Button = { Color = RED_COLOR, Close = Layer + ".blur", Command = "mb.case.close" },
            Text = { Text = "X", Font = "permanentmarker.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = TEXT_COLOR },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "285.139 199.674", OffsetMax = "314.69 229.226" }
        }, Layer + ".main.div", Layer + ".main.div" + ".close");
        container.Add(new CuiElement
        {
	        Name = Layer + ".main.div" + ".label",
	        Parent = Layer + ".main.div",
	        Components = {
					        new CuiTextComponent { Text = "КЕЙСЫ", Font = "robotocondensed-bold.ttf", FontSize = 27, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
					        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-303.556 176.994", OffsetMax = "40.556 229.226" }
				        }
        });

        container.Add(new CuiButton
        {
	        Button = { Color = WHITE_TRANSPARENT_BACKGROUND },
	        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-218.5 192.693", OffsetMax = "-196.322 214" }
        }, Layer + ".main.div", Layer + ".main.div" + ".about");

        container.Add(new CuiPanel
        {
	        CursorEnabled = false,
	        Image = { Color = "1 1 1 1", Sprite = "assets/icons/info.png"},
	        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-7.242 -7.242", OffsetMax = "7.242 7.242" }
        }, Layer + ".main.div" + ".about", Layer + ".main.div" + ".about" + ".sprite");

        container.Add(new CuiButton
        {
            Button = { Color = ORANGE_COLOR, Command = "mb.case.drawplayerinventory" },
            Text = { Text = "" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-192.7 192.693", OffsetMax = "-93.087 214" }
        }, Layer + ".main.div", Layer + ".main.div" + ".inventory.btn");

        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "1 1 1 1", Sprite = "assets/icons/backpack.png" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-42.384 -7.184", OffsetMax = "-28.016 7.184" }
        }, Layer + ".main.div" + ".inventory.btn");

        container.Add(new CuiElement
        {
            Parent = Layer + ".main.div" + ".inventory.btn",
            Components = {
                new CuiTextComponent { Text = "ИНВЕНТАРЬ", Font = "robotocondensed-regular.ttf", FontSize = 13, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-21.66 -11.154", OffsetMax = "50 11.154" }
            }
        });
        
        
        CuiHelper.AddUi(player, container);
        UI_DrawCases(player);
    }

    private void UI_UpdateBalance(BasePlayer player)
    {
        var container = new CuiElementContainer();
        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "0.6156863 0.6156863 0.6156863 0.5019608" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-82.964 195.417", OffsetMax = "15.564 212.803" }
        }, Layer + ".main.div", Layer + ".main.div" + ".coins.div", Layer + ".main.div" + ".coins.div");

        container.Add(new CuiElement()
        {
            Parent = Layer + ".main.div" + ".coins.div",
            Components =
            {
                new CuiTextComponent() { Text = "\u274d", FontSize = 12, Align = TextAnchor.MiddleCenter},
                new CuiRectTransformComponent() { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-48.144 -7.544", OffsetMax = "-33.056 7.544" }
            }
        });

        container.Add(new CuiElement
        {
            Name = Layer + ".main.div" + ".coins.div" + ".balance",
            Parent = Layer + ".main.div" + ".coins.div",
            Components = {
                new CuiTextComponent { Text = GetBalanceCoins(player).ToString(), Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-49.264 -8.693", OffsetMax = "49.265 8.694" }
            }
        });

        container.Add(new CuiButton
        {
            Button = { Color = "0.5254902 0.682353 0.4941177 1" },
            Text = { Text = "+", Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "31.877 -8.693", OffsetMax = "49.264 8.694" }
        }, Layer + ".main.div" + ".coins.div", Layer + ".main.div" + ".coins.div" + ".addbalance.btn");
        CuiHelper.AddUi(player, container);
    }

    private void UI_DrawCases(BasePlayer player)
    {
        var container = new CuiElementContainer();
        container.Add(new CuiPanel
        {
            CursorEnabled = false,
            Image = { Color = "1 1 1 0" },
            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-302.9 -164.417", OffsetMax = "300.265 181.24" }
        }, Layer + ".main.div", Layer + ".main.div" + ".items.div");

        float minx = -301.58f;
        float maxx = -159.6615f;
        float miny = 3.537514f;
        float maxy = 172.83f;

        int i = 0;

        foreach (var x in _config.CaseList)
        {
            if (i % 4 == 0 && i != 0)
            {
                minx = -301.58f;
                maxx = -159.6615f;
                miny -= 176.366f;
                maxy -= 176.366f;
            }

            var caseAmount = GetCaseAmount(player, x.Id);

            container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0.2", Command = $"mb.case.drawcase {x.Id}"},
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{minx} {miny}",
                        OffsetMax = $"{maxx} {maxy}"
                    }
                }, Layer + ".main.div" + ".items.div", Layer + ".main.div" + ".items.div" + $".{i}");
            container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0 0 0 0.7" },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.86 61.937",
                        OffsetMax = "-16.841 78.84"
                    }
                }, Layer + ".main.div" + ".items.div" + $".{i}",
                Layer + ".main.div" + ".items.div" + $".{i}" + ".amount.div");

            container.Add(new CuiElement
            {
                Parent = Layer + ".main.div" + ".items.div" + $".{i}" + ".amount.div",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = $"У ВАС: {caseAmount}", Font = "robotocondensed-regular.ttf", FontSize = 10,
                        Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-24.009 -8.451",
                        OffsetMax = "24.01 8.452"
                    }
                }
            });


            container.Add(new CuiElement()
            {
                Parent = Layer + ".main.div" + ".items.div" + $".{i}",
                Components =
                {
                    new CuiRawImageComponent() { Png = GuiManager.Get(x.Image) },
                    new CuiRectTransformComponent() { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-45.173 -35.873", OffsetMax = "45.173 54.473" }
                }
            });

            container.Add(new CuiElement
            {
                Name = Layer + ".main.div" + ".items.div" + $".{i}" + ".name",
                Parent = Layer + ".main.div" + ".items.div" + $".{i}",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = x.DisplayName, Font = "robotocondensed-regular.ttf", FontSize = 11,
                        Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-70.96 -59.5",
                        OffsetMax = "70.96 -35.874"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = Layer + ".main.div" + ".items.div" + $".{i}" + ".price",
                Parent = Layer + ".main.div" + ".items.div" + $".{i}",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = x.Price + "\u274d", Font = "robotocondensed-bold.ttf", FontSize = 16,
                        Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-70.961 -80.913",
                        OffsetMax = "70.959 -57.286"
                    }
                }
            });

            minx += 153.921f;
            maxx += 153.921f;
            i++;
        }

        CuiHelper.AddUi(player, container);
    }
    #endregion
}