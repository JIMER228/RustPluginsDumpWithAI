// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZoneSell", "CASHR#6906", "1.0.4")]
    internal class ZoneSell : RustPlugin
    {
        #region Static
        [PluginReference] private Plugin ImageLibrary,ServerRewards, Economics, ZoneManager;

        private static ZoneSell _;
        private Configuration _config;
        private ActiveController active;
        private List<string> ZoneIsBuy = new List<string>();
        #endregion

        #region Config

        private class Configuration
        {
            [JsonProperty("Price Settings")] public PriceSettings price = new PriceSettings();
            [JsonProperty("Currency name")] public string Currency = "RC";
            
            internal class PriceSettings
            {
                [JsonProperty("Use ServerRewards")] public bool ServerRewards = false;
                [JsonProperty("Use Economics")] public bool Economics = false;
                [JsonProperty("Use an item as payment?")]
                public bool ItemPrice = false;
                
                [JsonProperty("ItemShortName")] public string shortName = "";
                [JsonProperty("Item SkinID")] public ulong SkinID = 0;
            }
            
            [JsonProperty("The number of days for the player to be evicted")]
            public int TimeToEvicted = 5;

            [JsonProperty(PropertyName = "Zone Settings", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, ZoneSettings> ZoneList = new Dictionary<string, ZoneSettings>()
            {
                ["ZONENAME"] = new ZoneSettings()
            };

            [JsonProperty("Limit purchases per player")]
            public int ZoneLimit = 2;

            [JsonProperty("Limit Message")] public string Msg = "You have reached the land purchase limit";


            internal class ZoneSettings
            {
                [JsonProperty("Cost of the zone(Item amount or balance)")] public int Cost = 100;
                [JsonProperty("Zone image")] public string Image = "https://i.imgur.com/XIWhXKP.jpg";
                [JsonProperty("Zone descriptions")] public string Desc = "THIS IS AN APPROXIMATE DESCRIPTION FOR THE ZONE BEING SOLD"; 
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

        #region OxideHooks

        private void OnServerInitialized()
        {
            _ = this;
            PrintError("|-----------------------------------|");
            PrintWarning($"|  Plugin {Title} v{Version} is loaded  |");
            PrintWarning("|          Author: CASHR     |");
            PrintWarning("|          VK: vk.com/cashr         |");
            PrintWarning("|          Discord: CASHR#6906      |");
            PrintWarning("|          Email: pipnik99@gmail.com      |");
            PrintError("|-----------------------------------|");
            LoadData();
            foreach (var check in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(check);
            }
            foreach (var check in _config.ZoneList)
            {
                ImageLibrary?.Call("AddImage", check.Value.Image, check.Value.Image);
            }
            UpdateListZone();
            active = ServerMgr.Instance.gameObject.AddComponent<ActiveController>();
            //	ShowUI(BasePlayer.activePlayerList.First(),0);


        }
       
        private void UpdateListZone()
        {
            foreach (var check in _data)
            {
                if (check.Value.ZoneName.Count > 0)
                {
                    foreach (var zone in check.Value.ZoneName)
                    {
                        ZoneIsBuy.Add(zone);
                    }
                }
            }
        }
        
        private void Unload()
        {
            SaveData();
            UnityEngine.Object.Destroy(active);
            _ = null;
        }

        private void OnItemDeployed(Deployer deployer, BaseEntity entity, BaseEntity slotEntity)
        {
            if (entity == null || deployer.GetDeployable()?.slot != BaseEntity.Slot.Lock) return;

            var zoneIDS = ZoneManager?.Call<string[]>("GetEntityZoneIDs", entity);
            if (zoneIDS?.Length == 0) return;
            CheckEntity(slotEntity, zoneIDS[0]);
        }


        private void OnEntityEnterZone(string ZoneID, BuildingBlock entity)
        {
           CheckEntity(entity,ZoneID);
        }

        private bool CheckEntity(BaseEntity entity, string ZoneID)
        {
            if (entity == null || entity.OwnerID == 0) return false;
            var userID = entity.OwnerID;
            if (!_data.ContainsKey(userID)) return false;
            if (!_data[userID].ZoneName.Contains(ZoneID))
            {
                entity.Invoke(entity.KillMessage, 0.1f);
                var player = BasePlayer.FindByID(entity.OwnerID);
                if (player != null)
                {
                    player.GiveItem(ItemManager.CreateByName("wood", 50));
                    player.ChatMessage("You need to buy a zone before you build here");
                }
                return true;
            }

            return false;
        }
        private void OnEntityEnterZone(string ZoneID, DecayEntity entity)
        {
            CheckEntity(entity, ZoneID);
        }
        #endregion


        #region Function


        #region Data


        private Dictionary<ulong, Data> _data;

        private class Data
        {
            public List<string> ZoneName = new List<string>();
            public DateTime LastActivity = DateTime.Now;
        }

        private void LoadData()
        {
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile($"{Name}/PlayerData"))
                _data = new Dictionary<ulong, Data>();
            else
                _data = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Data>>(
                    $"{Name}/PlayerData");
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/PlayerData", _data);


            if (_data == null)
                _data = new Dictionary<ulong, Data>();
        }

        private void OnServerSave()
        {
            SaveData();
        }

        private void SaveData()
        {
            if (_data != null)
                Interface.Oxide.DataFileSystem.WriteObject($"{Name}/PlayerData", _data);
        }


        private void OnPlayerConnected(BasePlayer player)
        {
            if (_data.ContainsKey(player.userID))
            {
                _data[player.userID].LastActivity = DateTime.Now;
            }
            else
            {
                _data.Add(player.userID, new Data());

            }
        }

        [ChatCommand("buyzone")]
        private void cmdChatzone(BasePlayer player, string command, string[] args)
        {
           // Debug.Log("buyzone");
            //if (!string.IsNullOrEmpty(_data[player.userID].ZoneName) && !player.IsAdmin) return;
            ShowUI(player);
        }

        private void ShowUI(BasePlayer player, int page = 0)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = {Color = "0 0 0 0.3607843", Material = "assets/content/ui/uibackgroundblur.mat"},
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-375.427 -229.056",
                    OffsetMax = "426.586 240.425"
                }
            }, "Hud", "Panel_4789");

            container.Add(new CuiButton
            {
                Button = {Color = "0.3867925 0.3867925 0.3867925 1"},
                Text =
                {
                    Text = "LIST OF ZONES AVAILABLE FOR PURCHASE", Font = "robotocondensed-bold.ttf", FontSize = 30,
                    Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-401.004 182.342",
                    OffsetMax = "401.006 234.74"
                }
            }, "Panel_4789", "Button_3960");
            container.Add(new CuiButton
            {
                Button = {Color = "1 1 1 0", Close = "Panel_4789"},
                Text =
                {
                    Text = "X", Font = "robotocondensed-bold.ttf", FontSize = 35, Align = TextAnchor.MiddleCenter,
                    Color = "1 0 0 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "347.325 183.54",
                    OffsetMax = "397.325 233.54"
                }
            }, "Panel_4789", "Button_8837");
            var a = 1;
            double posx = -381.878;
            double posy = -15.021;
            double wight = 225.844;
            double height = 183.899;
            foreach (var check in _config.ZoneList.Skip(page * 6).Take(6))
            {
                container.Add(new CuiButton
                {
                    Button = {Color = "0.9716981 0.9716981 0.9716981 0"},
                    Text =
                    {
                        Text = "   ", Font = "robotocondensed-regular.ttf", FontSize = 14,
                        Align = TextAnchor.MiddleCenter,
                        Color = "0 0 0 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{posx} {posy}",
                        OffsetMax = $"{posx + wight} {posy + height}"
                    }
                }, "Panel_4789", "ParentLayer");
                container.Add(new CuiElement
                {
                    Name = "Image_9950",
                    Parent = "ParentLayer",
                    Components =
                    {
                        new CuiRawImageComponent
                            {Color = "1 1 1 1", Png = (string) ImageLibrary?.Call("GetImage", check.Value.Image)},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-112.92 -50.155",
                            OffsetMax = "112.92 91.95"
                        }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "Label_2411",
                    Parent = "ParentLayer",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = check.Value.Desc,
                            Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },
                        new CuiOutlineComponent {Color = "0 0 0 0.5", Distance = "1 -1"},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-112.919 -50.152",
                            OffsetMax = "112.921 91.948"
                        }
                    }
                });

                var color = ZoneIsBuy.Contains(check.Key) ? "1 1 1 0.3" : "0.2741634 0.5754717 0.3336971 1";
                var cmdToButton = ZoneIsBuy.Contains(check.Key) ? "1" : $"UI_ZONESELL BUY {check.Key}";
                var textToBtn = ZoneIsBuy.Contains(check.Key) ? "SOLD" : "BUY";
                container.Add(new CuiButton
                {
                    Button = {Color = color, Command = cmdToButton},
                    Text =
                    {
                        Text = textToBtn, Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-112.92 -91.95",
                        OffsetMax = "0 -50.155"
                    }
                }, "ParentLayer", "Button_3582");

                container.Add(new CuiButton
                {
                    Button = {Color = "0.2269491 0.2937111 0.6415094 1", Command = $"UI_ZONESELL SHOW {check.Key}"},
                    Text =
                    {
                        Text = "SHOW IN MAP", Font = "robotocondensed-bold.ttf", FontSize = 15,
                        Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "0.002 -91.95",
                        OffsetMax = "112.922 -50.155"
                    }
                }, "ParentLayer", "Button_3582 (1)");

                a++;
                posx += wight + 40;

                if (a == 4)
                {
                    posx = -381.878;
                    posy -= height + 20;

                }
            }

            if (page > 0)
            {
                container.Add(new CuiButton
                {
                    Button = {Color = "0 0 0 0.4039216", Command = $"UI_ZONESELL PAGE {page - 1}"},
                    Text =
                    {
                        Text = "<", Font = "robotocondensed-bold.ttf", FontSize = 27, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-68.293 -274.624",
                        OffsetMax = "-19.656 -238.576"
                    }
                }, "Panel_4789", "Button_4514");
            }


            if ( (page + 1) * 6 < _config.ZoneList.Count)
            {
                container.Add(new CuiButton
                {
                    Button = {Color = "0 0 0 0.4039216", Command = $"UI_ZONESELL PAGE {page + 1}"},
                    Text =
                    {
                        Text = ">", Font = "robotocondensed-bold.ttf", FontSize = 27, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "24.852 -274.624",
                        OffsetMax = "73.489 -238.576"
                    }
                }, "Panel_4789", "Button_4514 (1)");
            }

            container.Add(new CuiElement
            {
                Name = "Label_9595",
                Parent = "Panel_4789",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = $"{page + 1}", Font = "robotocondensed-bold.ttf", FontSize = 27, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },
                    new CuiOutlineComponent {Color = "0 0 0 0.5", Distance = "1 -1"},
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19.656 -274.624",
                        OffsetMax = "24.852 -238.576"
                    }
                }
            });

            CuiHelper.DestroyUi(player, "Panel_4789");
            CuiHelper.AddUi(player, container);
        }
/*

        private Dictionary<BasePlayer, SellSettings> ActiveSelles = new Dictionary<BasePlayer, SellSettings>();

        private class SellSettings
        {
            public BasePlayer buyer;
            public double Summ;
            public string ZoneName;
        }
        [ChatCommand("sellzone")]
        private void cmdChatsellzone(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 3)
            {
               Player.Message(player, GetMessage("MSG_ERRORCMD",player.UserIDString));
                return;
            }

            var TargetPlayer = BasePlayer.Find(args[0]);
            if (TargetPlayer == null || TargetPlayer == player)
            {
                Player.Message(player, GetMessage("MSG_PLAYERNOTFOUND",player.UserIDString));
                return;
            }

            int cost = 100000;
            if(int.TryParse(args[2], out cost))
            {
                var zoneName = args[1];
                if (!_data[player.userID].ZoneName.Contains(zoneName))
                {
                    Player.Message(player, GetMessage("MSG_ZONENOTFOUND",player.UserIDString));
                    return;
                }

                ActiveSelles[player] = new SellSettings()
                {
                    buyer = TargetPlayer,
                    Summ = cost,
                    ZoneName = zoneName
                };
                timer.In(15f, () =>
                {
                    ActiveSelles.Remove(player);
                });
                Player.Message(TargetPlayer, GetMessage("MSG_SENDSELLS",TargetPlayer.UserIDString, new[]{player.displayName,zoneName, cost.ToString(), _config.Currency}));
            }
            else
            {
                Player.Message(player, GetMessage("MSG_COSTNOTFOUND",player.UserIDString));
            }
            
        }

        [ChatCommand("zonebuy")]
        private void cmdChatbuyzone(BasePlayer player, string command, string[] args)
        {
            var Sells = GetActiveSell(player);
            if (Sells == null) return;
            var sellSettings = ActiveSelles[Sells];
            var zone = sellSettings.ZoneName;
            if (_data[player.userID].ZoneName.Contains(zone)) return;
            if (_data[player.userID].ZoneName.Count >= _config.ZoneLimit)
            {
                player.ChatMessage("You have reached the land purchase limit");
                return;
            }
            var cost = sellSettings.Summ;
            var balance = GetBalance(player);
            if (balance < cost)
            {
                CuiHelper.DestroyUi(player, "Panel_4789");
                return;
            }
            RemoveBalance(player,cost, balance);
            AddBalance(Sells,cost, GetBalance(Sells));
            _data[player.userID].ZoneName.Add(zone);
            UpdateListZone();
            Player.Message(player, GetMessage("MSG_ACCESBUY",player.UserIDString));
            Player.Message(Sells, GetMessage("MSG_ACCESSELL",Sells.UserIDString));
        }


        private BasePlayer GetActiveSell(BasePlayer player)
        {
            foreach (var check in ActiveSelles)
            {
                if (check.Value.buyer == player)
                    return check.Key;
            }
            return null;
        }
        [ChatCommand("zonelist")]
        private void cmdChatzonelist(BasePlayer player, string command, string[] args)
        {
            var zoneList = "";
            foreach (var check in _data[player.userID].ZoneName)
            {
                zoneList += check + "\n";
            }
            var msg = GetMessage("MSG_SHOWZONE", player.UserIDString, new String[]{zoneList});

            Player.Message(player, msg);
        }
*/
        [ConsoleCommand("UI_ZONESELL")]
        private void cmdConsoleUI_ZONESELL(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            switch (arg.Args[0])
            {
                case "EXIT":
                    if (!player.IsAdmin) return;
                    if (arg.Args.Length < 2)
                    {
                        PrintError("USE UI_ZONESELL EXIT STEAMID");
                        return;
                    }

                    ulong userID;
                    if (ulong.TryParse(arg.Args[1], out userID))
                    {
                        active.ZoneKill(userID);
                        player.ChatMessage("The zone is cleared");
                    }
                    break;
                case "PAGE":
                    ShowUI(player, int.Parse(arg.Args[1]));
                    break;
                case "BUY":
                {
                    var zone = _config.ZoneList[arg.Args[1]];
                    if (_data[player.userID].ZoneName.Contains(arg.Args[1])) return;
                    if (_data[player.userID].ZoneName.Count >= _config.ZoneLimit)
                    {
                        player.ChatMessage("You have reached the land purchase limit");
                        return;
                    }
                    var cost = zone.Cost;
                    var balance = GetBalance(player);
                    if (balance < cost)
                    {
                        CuiHelper.DestroyUi(player, "Panel_4789");
                        player.ChatMessage("You don't have enough money to buy this zone");
                        return;
                    }
                    RemoveBalance(player,cost, balance);
                    _data[player.userID].ZoneName.Add(arg.Args[1]);
                    UpdateListZone();
                    ShowUI(player);
                    player.ChatMessage($"You have successfully purchased the zone");
                    break;
            }

                case "SHOW":
                {
                    var position = ZoneManager.Call<Vector3>("GetZoneLocation", arg.Args[1]);
                    var rad = ZoneManager.Call("GetZoneRadius", arg.Args[1]);
                    var radius = rad == null ? 2 : (float) rad;
                
				  var mapmarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", position) as
                            MapMarkerGenericRadius;
                  if (mapmarker == null) return;
                  mapmarker.OwnerID = player.userID;
                  mapmarker.enableSaving = false;
                  mapmarker.Spawn();
                  mapmarker.radius = radius / 100;
                  mapmarker.alpha = 0.5f;
                  var color = new Color(1, 0.50f, 0.25f, 1);
                  var color2 = new Color(0, 0, 0, 0);
                  mapmarker.color1 = color;
                  mapmarker.color2 = color2;
                  mapmarker.SendUpdate();
                  mapmarker.Invoke(mapmarker.KillMessage, 15f);
                  player.ChatMessage($"Open the mini map to see the area on the map.The zone is located in the area of the {PosToMapCoords(position)} square");
                }

                    break;
            }
        }
       
        private string PosToMapCoords(Vector3 pos)
        {
            var alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            var coords = "";

            pos.z = -pos.z;
            pos += new Vector3(TerrainMeta.Size.x, 0, TerrainMeta.Size.z) * .5f;

            var cubeSize = 146.14f;

            var xCube = (int) (pos.x / cubeSize);
            var zCube = (int) (pos.z / cubeSize);

            var firstLetterIndex = (int) (xCube / alpha.Length) - 1;
            var firstLetter = "";
            if (firstLetterIndex >= 0)
                firstLetter = $"{alpha[firstLetterIndex]}";

            var xStr = $"{firstLetter}{alpha[xCube % 26]}";
            var zStr = $"{zCube}";


            return $"{xStr}{zStr}";
        }
        private object CanNetworkTo(MapMarkerGenericRadius entity, BasePlayer target)
        {            
            if(entity == null || target == null)return null;			
            if (target.userID != entity.OwnerID) return false;
            return null;
        }

        private void RemoveBalance(BasePlayer player, double summ, double lastBalance)
        {
            if (Economics && _config.price.Economics)
            {
                Economics.Call("SetBalance", player.UserIDString, lastBalance - summ);
                return;
            }
            if (ServerRewards && _config.price.ServerRewards)
            {
                ServerRewards.Call("TakePoints", player.UserIDString, summ);
                return;
            }
            if (_config.price.ItemPrice)
            { 
                RemoveItem(player.inventory.AllItems(), _config.price.shortName, _config.price.SkinID, (int)summ);
            }
        }
        private void AddBalance(BasePlayer player, double summ, double lastBalance)
        {
            if (Economics && _config.price.Economics)
            {
                Economics.Call("SetBalance", player.UserIDString, lastBalance + summ);
                return;
            }
            if (ServerRewards && _config.price.ServerRewards)
            {
                ServerRewards.Call("AddPoints", player.UserIDString, summ);
                return;
            }
            if (_config.price.ItemPrice)
            {
                var item = ItemManager.CreateByName(_config.price.shortName, (int)summ, _config.price.SkinID);
                player.GiveItem(item);
            }
        }
        
        private double GetBalance(BasePlayer player)
        {
            if (ServerRewards && _config.price.ServerRewards)
            {
                var bal = ServerRewards?.Call("CheckPoints", player.UserIDString);
                if (bal == null) return 0;
                return Convert.ToDouble(bal);
            }
            
            if (Economics && _config.price.Economics)
            {
                return Convert.ToDouble(Economics.Call("Balance", player.UserIDString));
            }

            if (_config.price.ItemPrice)
            {
                return CheckItem(player,_config.price.shortName,_config.price.SkinID);
            }
            return 0;
        }
        private void RemoveItem(IEnumerable<Item> itemList, string shortname, ulong skinId, int iAmount)
        {
            var num1 = 0;
            if (iAmount == 0) return;

            var list = Facepunch.Pool.GetList<Item>();

            foreach (var obj in itemList)
            {
                if (obj.info.shortname != shortname || obj.skin != skinId) continue;
                var num2 = iAmount - num1;
                if (num2 <= 0) continue;
                if (obj.amount > num2)
                {
                    obj.MarkDirty();
                    obj.amount -= num2;
                    num1 += num2;
                    break;
                }

                if (obj.amount <= num2)
                {
                    num1 += obj.amount;
                    list.Add(obj);
                }

                if (num1 == iAmount)
                    break;
            }

            foreach (var obj in list)
                obj.RemoveFromContainer();

            Facepunch.Pool.FreeList(ref list);
        }
        private int CheckItem(BasePlayer player, string shortname, ulong skinID)
        {
            var amount = 0;
            for (var i = 0; i < player.inventory.AllItems().Count(); i++)
            {
                var item = player.inventory.AllItems()[i];
                if (item.info.shortname == shortname && item.skin == skinID)
                    amount += item.amount;
            }

            return amount;
        }
        #endregion


        private class ActiveController : FacepunchBehaviour
        {
            private Coroutine cor;
            private void Awake()
            {
                cor = StartCoroutine(CheckTime());
            }

            private void OnDestroy()
            {
                StopCoroutine(cor);
            }

            private IEnumerator CheckTime()
            {
                yield return CoroutineEx.waitForSeconds(0.1f);
                
                foreach (var check in _._data.ToArray())
                {
                    if (check.Value.ZoneName.Count < 1) continue;
                    if((DateTime.Now - check.Value.LastActivity).TotalDays >= _._config.TimeToEvicted)
                        ZoneKill(check.Key);
                }

                yield return 0;
            }
            
         

            public void ZoneKill(ulong userID)
            {
                var entityList = _.ZoneManager.Call<List<BaseEntity>>("GetEntitiesInZone", _._data[userID].ZoneName);
                if (entityList != null)
                {
                    foreach (var VARIABLE in entityList)
                    {
                        if (VARIABLE as BuildingBlock || VARIABLE as DecayEntity)
                        {
                            VARIABLE.Kill();
                        }
                    }
                }
                _._data[userID].ZoneName = new List<string>();
            }
        }


        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["MSG_ERRORCMD"] = "Incorrect use of the command. Use it like this: /sellzone Player Name Zone Name price",
                ["MSG_SHOWZONE"] = "List of purchased zones:\n {0}",
                ["MSG_PLAYERNOTFOUND"] = "Player not found",
                ["MSG_SENDSELLS"] = "Player not found",
                ["MSG_ZONENOTFOUND"] = "You can't sell this zone",
                ["MSG_ACCESBUY"] = "You have successfully purchased the zone",
                ["MSG_ACCESSELL"] = "You have successfully sold the zone",
                ["MSG_COSTNOTFOUND"] = "Unknown amount",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["MSG_ERRORCMD"] = "Не правильное использование команды. Используйте так: /sellzone ИмяИгрока НазваниеЗоны цена",
                ["MSG_SHOWZONE"] = "Список купленных зон:\n {0}",
                ["MSG_PLAYERNOTFOUND"] = "Игрок не найден",
                ["MSG_SENDSELLS"] = "Игрок {0} хочет продать вам зону {1} за {2} {3}",
                ["MSG_ZONENOTFOUND"] = "Вы не можете продать эту зону",
                ["MSG_COSTNOTFOUND"] = "Неизвестная сумма",
                ["MSG_ACCESBUY"] = "Вы успешно купили зону",
                ["MSG_ACCESSELL"] = "Вы успешно продали зону",


            }, this, "ru");
        }

        private string GetMessage(string langKey, string steamID) => lang.GetMessage(langKey, this, steamID);

        private string GetMessage(string langKey, string steamID, params string[] args)
        {
            return (args.Length == 0)
                ? GetMessage(langKey, steamID)
                : string.Format(GetMessage(langKey, steamID), args);
        }
        #endregion
    }
}