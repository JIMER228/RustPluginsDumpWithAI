using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch.Extend;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Inventory","https://discord.gg/9vyTXsJyKR","1.0")]
    public class Inventory : RustPlugin
    {
        #region var

        [PluginReference] private Plugin ImageLibrary,BMenu;
        
        Dictionary<int,string> _category = new Dictionary<int, string>
        {
            
        };
        Dictionary<string,string> rarityColors = new Dictionary<string, string>
        {
            ["null"] = "0.0 0.0 0.0 0.4",
            ["default"] = "0.26 0.27 0.29 0.4",
            ["rare"] = "0.21 0.23 0.42 0.4",
            ["epic"] = "0.39 0.22 0.53 0.4",
            ["legendary"] = "0.92 0.83 0.68 0.4"
        };

        #endregion

        #region data

        Dictionary<ulong,Dictionary<int,string>>_playercategory = new Dictionary<ulong, Dictionary<int,string>>();
        void CheckOrAddCataegory(ulong userid,string category)
        {
            if (!_playercategory.ContainsKey(userid))
            {
                _playercategory.Add(userid,new Dictionary<int, string>
                {
                    [0] = category
                });
                return;
            }

            _playercategory[userid].Add(_playercategory[userid].Count,category);
            
            
            
            /*if (_category == null || _category == new Dictionary<int, string>())
            {
                //PrintWarning("Null category");
                _category[0] = category;
            }
            if (!_category.ContainsValue(category))
            {
                //PrintWarning("!Contains category");
                _category[_category.Count] = category;
            }*/
        }

        class PlayerItem
        {
            public string Shortname;
            
            public bool Blueprint;
            
            public bool IsCommand;
            
            public int Amount;
            
            public string Image = "";
            
            public string DisplayName;

            public ulong SkinId = 0;

            public int Id;

            public string Category;

            public string Rarity = "default";
        }
        
        Dictionary<ulong,List<PlayerItem>> _playerData = new Dictionary<ulong, List<PlayerItem>>();

        private string _filename = "ItemInv/PlayerData";
        void InitData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile(_filename))
            {
                ReadData();
            }
            SaveData();
        }

        void ReadData()
        {
            _playerData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, List<PlayerItem>>>(_filename);
            
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(_filename,_playerData);
        }

        void CreateNewPlayerData(BasePlayer player)
        {
            if (!_playerData.ContainsKey(player.userID))
            {
                _playerData.Add(player.userID,new List<PlayerItem>());
            }
        }
        
        
        #region ImageData

        void InitImages()
        {
            foreach (var values in _playerData.Values)
            {
                foreach (var item in values)
                {
                    if (item.IsCommand)
                    {
                        ImageLibrary.Call("AddImage", item.Image, item.Image);
                    }
                       
                }
                
            }
            
        }

        void InitImage(PlayerItem item)
        {
            if (!(bool)ImageLibrary.Call("HasImage",item.Image))
            {
                ImageLibrary.Call("AddImage", item.Image, item.Image);
            }
        }

        #endregion
        #endregion

        #region hooks

        void OnPlayerConnected(BasePlayer player)
        {
            if (!_playerData.ContainsKey(player.userID))
                CreateNewPlayerData(player);
            
        }
        
        void OnServerInitialized()
        {
            InitImages();
            InitData();
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
            timer.Once(120f,MakeBackup);
        }
        
        void MakeBackup()
        {
            SaveData();
            timer.Once(600f, MakeBackup);
        }
        
        void Unload()
        {
            SaveData();
        }

        
        void TakeFromInv(BasePlayer player, int Id,string category = "ВСЕ",int page = 0)
        {
            PlayerItem invItem = _playerData[player.userID].Find(p => p.Id == Id);
            
            if (invItem == null) return;
            
            if (invItem.IsCommand)
            {
                Server.Command(invItem.Shortname);
                
            }

            if (invItem.Blueprint)
            {
                Item item = ItemManager.CreateByPartialName(invItem.Shortname);
                Item create = ItemManager.CreateByItemID(-996920608);

                var info = ItemManager.FindItemDefinition(item.info.shortname);
                create.blueprintTarget = info.itemid;
                if (player.inventory.containerMain.capacity-player.inventory.containerMain.itemList.Count<1)
                {
                    create.DropAndTossUpwards(player.transform.position);
                }
                else
                {
                    create.MoveToContainer(player.inventory.containerMain);
                }
            }

            if (!invItem.Blueprint && !invItem.IsCommand)
            {
                Item item = ItemManager.CreateByPartialName(invItem.Shortname, invItem.Amount,invItem.SkinId);
                if (player.inventory.containerMain.capacity-player.inventory.containerMain.itemList.Count<1)
                {
                    item.DropAndTossUpwards(player.transform.position);
                }
                else
                {
                    item.MoveToContainer(player.inventory.containerMain);
                }
            }
            _playerData[player.userID].Remove(invItem);
            Effect x = new Effect("assets/bundled/prefabs/fx/notice/stack.world.fx.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(x, player.Connection);
            if (HasPage(player.userID,page,category))
            {
                MainUI(player,page,category);
            }
            else
            {
                MainUI(player,page-1,category);
            }
            //MainUI(player);
        }
        #endregion

        #region UI

        bool HasPage(ulong userid,int page, string category)
        {
            if (category == "ВСЕ")
            {
                int amount = _playerData[userid].Count;
                if (amount>page*21)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                int amount = 0;
                foreach (var playerItem in _playerData[userid])
                {
                    if (playerItem.Category == category)
                    {
                        amount++;
                    }
                }
                if (amount>page*21)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        void ReloadImage(List<PlayerItem> items)
        {
            List<string> exist = new List<string>();
            foreach (var var in items)
            {
                if (!exist.Contains(var.Image))
                {
                    ImageLibrary.Call("AddImage", var.Image, var.Image);
                    exist.Add(var.Image);
                }
            }
        }
        
        private string _layer = "InvUI";
        private double ImgWeight = 120;

        void MainUI(BasePlayer player, int page = 0, string category = "ВСЕ")
        {
            //_category = new Dictionary<int, string>();
            /*foreach (var var in _playerData[player.userID])
            {
                CheckOrAddCataegory(player.userID,var.Category);
            }*/
            
            /*if (category !=  "ВСЕ")
            {
                category = _category[category.ToInt()];
                
                
            }*/
            BMenu.Call("SetActiveSubButton", player, "openinv");
            CuiHelper.DestroyUi(player, _layer);
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "345 15",OffsetMax = "1265 660"},
            
            }, "Main_UI", "SubContent_UI");
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "1 1"}
            }, "SubContent_UI", _layer);
            double startmx = 0;
            List<PlayerItem> playerItems = _playerData[player.userID];
            ReloadImage(playerItems);
            List<string>pCategory= new List<string>();
            foreach (var item in playerItems)
            {
                
                if (!pCategory.Contains(item.Category))
                {
                    pCategory.Add(item.Category);
                    
                }
            }

            container.Add(new CuiButton
            {
                Button = {Close = _layer, Color = HexToCuiColor("#45413B"), Command = $"openinv 0"},
                Text = {Text = "ВСЕ", Color = "0.929 0.882 0.847 1", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf"},
                RectTransform =
                {
                    AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"{startmx} 565",
                    OffsetMax = $"{startmx + 3 * 12 + 3} 585"
                }
            }, _layer, _layer + $".Category");
            double fontsize = 15;
            startmx += 3*fontsize + 8;
            foreach (var var in pCategory)
            {
                container.Add(new CuiButton
                {
                    Button = {Close = _layer,Color = HexToCuiColor("#45413B"),Command = $"openinv 0 {var}"},
                    Text = {Text = var,Color = "0.929 0.882 0.847 1",Align = TextAnchor.MiddleCenter,FontSize = (int)fontsize},
                    RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = $"{startmx} 565",OffsetMax = $"{startmx+var.Length*fontsize+3} 585"}
                }, _layer, _layer + $".Category");
                startmx += var.Length*fontsize + 8;
            }
            
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = $"0 595",OffsetMax = $"920 650"}
            }, _layer, _layer + ".Header");
            container.Add(new CuiPanel
            {
                Image = {Color = HexToCuiColor("#33322d")},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = $"365 595",OffsetMax = $"555 599"}
            }, _layer, _layer + ".line");
            container.Add(new CuiLabel
            {
                Text = {Text = "УБЕЖИЩЕ",FontSize = 20,Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf"},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "1 1"}
            }, _layer + ".Header", _layer + ".Header" + ".Text");

            
            double starty = 580 - ImgWeight - 20-40;
            double startx = 0;

            int i = 0;
            
            if (category == "ВСЕ")
            {
                foreach (var pItem in _playerData[player.userID].Skip(21*page).Take(21))
                {
                    /*PrintWarning(_playerData[player.userID].Skip(21*page).Take(20*(page+1)).Count().ToString());
                    PrintWarning($"{21*page} : {21*(page+1)} = {21*(page+1)-21*page}");*/
                    
                    if (i == 7)
                    {
                        starty -= ImgWeight + 25;
                        startx = 0;
                        i = 0;
                    }
                    container.Add(new CuiPanel
                    {
                        Image = {Color = HexToCuiColor("#52525200")},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0", OffsetMin = $"{startx} {starty}",OffsetMax = $"{startx+ImgWeight} {starty+ImgWeight+20}"}
                    }, _layer, _layer + pItem.Id);
                    container.Add(new CuiButton
                    {
                        Button = {Color = "0.17 0.41 0.57 1",Command = $"takeitemfrominv {pItem.Id}"},
                        Text = {Text = "Взять",FontSize = 12,Align = TextAnchor.MiddleCenter,Color = "0.86 0.84 0.82 1"},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = $"{ImgWeight} 20"}
                    }, _layer + pItem.Id, _layer + pItem.Id + ".Btn");
                    if (pItem.Rarity != null && rarityColors.ContainsKey(pItem.Rarity))
                    {
                        container.Add(new CuiPanel
                        {
                            Image = {Color = (rarityColors[pItem.Rarity])},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 20",OffsetMax = $"{ImgWeight} 140"}
                        }, _layer + pItem.Id,_layer+pItem.Id+".Rarity");
                    }
                    
                    container.Add(new CuiLabel
                    {
                        Text = {Text = $"{pItem.DisplayName}",Align = TextAnchor.MiddleCenter},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 20",OffsetMax = $"{ImgWeight} 40"}
                    }, _layer +  pItem.Id, _layer +  pItem.Id + ".Amount");


                    if (pItem.Blueprint)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer +  pItem.Id,
                            Name = _layer +  pItem.Id+".Blueprint",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage","bp")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 40",OffsetMax = $"{ImgWeight} {ImgWeight+20}"
                                }
                            }
                        });
                        container.Add(new CuiElement
                        {
                            Parent = _layer+pItem.Id,
                            Name = _layer + pItem.Id+".Img",
                            Components =
                            {
                                new CuiRawImageComponent{Png = (string) ImageLibrary.Call("GetImage",pItem.Shortname)},
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 40",OffsetMax = $"{ImgWeight} {ImgWeight+20}"
                                }
                            }
                        });
                    }
                    else
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer+pItem.Id,
                            Name = _layer + pItem.Id+".Img",
                            Components =
                            {
                                new CuiRawImageComponent{Png = (string) ImageLibrary.Call("GetImage",pItem.Image)},
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 40",OffsetMax = $"{ImgWeight} {ImgWeight+20}"
                                }
                            }
                        });
                    }
                    
                    
                    startx += ImgWeight + 10;
                    i++;
                }
                /*if (_playerData[player.userID].Count > (page + 1)*21)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = $"0.85 0.03", AnchorMax = $"0.97 0.09", OffsetMax = "0 0" },
                        Button = { Color = HexToCuiColor("d12c2c"), Command = $"openinv {page + 1}"},
                        Text = { Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-regular.ttf" }
                    }, _layer);
                }

                if (page >= 1)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = $"0.72 0.03", AnchorMax = $"0.84 0.09", OffsetMax = "0 0" },
                        Button = { Color = HexToCuiColor("d12c2c"), Command = page >= 1 ? $"openinv {page - 1}" : "" },
                        Text = { Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-regular.ttf" }
                    }, _layer);
                }*/
                
                    container.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "730 20",OffsetMax = "890 80"},
                        Button = { Color = HexToCuiColor("#34405e"), Command = _playerData[player.userID].Count > (page + 1)*21 ?$"openinv {page + 1}":"",Material = "assets/icons/greyout.mat" },
                        Text = { Text = "<b>ВПЕРЁД →</b>", Align = TextAnchor.MiddleCenter, FontSize = 21, Font = "robotocondensed-regular.ttf",Color = _playerData[player.userID].Count > (page + 1)*21?HexToCuiColor("F4F4F4"):HexToCuiColor("#727273") }
                    }, _layer);
                

                
                    container.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 20",OffsetMax = "160 80"},
                        Button = { Color = page >= 1 ? HexToCuiColor("#34405e"):HexToCuiColor("#1C1C1C"), Command = page >= 1 ? $"openinv {page - 1}" : "",Material = "assets/icons/greyout.mat" },
                        Text = { Text = "<b>← НАЗАД</b>", Align = TextAnchor.MiddleCenter, FontSize = 21, Font = "robotocondensed-regular.ttf",Color = page >= 1 ?HexToCuiColor("F4F4F4"):HexToCuiColor("#727273") }
                    }, _layer);
                
            }

            else
            {
                List<PlayerItem> sorted = new List<PlayerItem>();
                foreach (var playerItem in playerItems)
                {
                    PrintWarning($"{playerItem.Category} == {category} ({category.ToInt()} {category})");
                    if (playerItem.Category == category)
                    {
                        PrintWarning($"{playerItem.Category} == {category} ({category.ToInt()} {category})");
                        //PrintWarning($"{category} {_category[category.ToInt()]}");
                        sorted.Add(playerItem);
                        
                    }
                }
                
                PrintWarning(sorted.Count.ToString());
                
                /*foreach (var pItem in sorted.Skip(21*page).Take(21*(page+1)))
                {
                    PrintError(startx.ToString());
                    if (i == 7)
                    {
                        PrintWarning("Next line");
                        starty -= ImgWeight + 25;
                        startx = 0;
                        i = 0;
                    }
                    container.Add(new CuiPanel
                    {
                        Image = {Color = HexToCuiColor("#52525200")},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0", OffsetMin = $"{startx} {starty}",OffsetMax = $"{startx+ImgWeight} {starty+ImgWeight+20}"}
                    }, _layer, _layer + pItem.Id);
                    container.Add(new CuiButton
                    {
                        Button = {Color = 0.17 0.41 0.57 1,Command = $"takeitemfrominv {pItem.Id}"},
                        Text = {Text = "Взять",FontSize = 12,Align = TextAnchor.MiddleCenter, Color = "0.86 0.84 0.82 1"},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = $"{ImgWeight} 20"}
                    }, _layer + pItem.Id, _layer + pItem.Id + ".Btn");
                    container.Add(new CuiLabel
                    {
                        Text = {Text = $"{pItem.DisplayName}",Align = TextAnchor.MiddleCenter},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 20",OffsetMax = $"{ImgWeight} 40"}
                    }, _layer +  pItem.Id, _layer +  pItem.Id + ".Amount");


                    if (pItem.Blueprint)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer +  pItem.Id,
                            Name = _layer +  pItem.Id+".Blueprint",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage","bp")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 40",OffsetMax = $"{ImgWeight} {ImgWeight+20}"
                                }
                            }
                        });
                    }
                    container.Add(new CuiElement
                    {
                        Parent = _layer+pItem.Id,
                        Name = _layer + pItem.Id+".Img",
                        Components =
                        {
                            new CuiRawImageComponent{Png = (string) ImageLibrary.Call("GetImage",pItem.Image)},
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 40",OffsetMax = $"{ImgWeight} {ImgWeight+20}"
                            }
                        }
                    });
                    startx += ImgWeight + 10;
                    i++;
                }*/
                foreach (var pItem in sorted.Skip(21*page).Take(21))
                {
                    
                    if (i == 7)
                    {
                        starty -= ImgWeight + 25;
                        startx = 0;
                        i = 0;
                    }
                    container.Add(new CuiPanel
                    {
                        Image = {Color = HexToCuiColor("#52525200")},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0", OffsetMin = $"{startx} {starty}",OffsetMax = $"{startx+ImgWeight} {starty+ImgWeight+20}"}
                    }, _layer, _layer + pItem.Id);
                    container.Add(new CuiButton
                    {
                        Button = {Color = "0.17 0.41 0.57 1",Command = $"takeitemfrominv {pItem.Id} {category} {page}"},
                        Text = {Text = "Взять",FontSize = 12,Align = TextAnchor.MiddleCenter,Color = "0.86 0.84 0.82 1"},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = $"{ImgWeight} 20"}
                    }, _layer + pItem.Id, _layer + pItem.Id + ".Btn");
                    if (pItem.Rarity != null && rarityColors.ContainsKey(pItem.Rarity))
                    {
                        container.Add(new CuiPanel
                        {
                            Image = {Color = (rarityColors[pItem.Rarity])},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 20",OffsetMax = $"{ImgWeight} 140"}
                        }, _layer + pItem.Id,_layer+pItem.Id+".Rarity");
                    }
                    
                    container.Add(new CuiLabel
                    {
                        Text = {Text = $"{pItem.DisplayName}",Align = TextAnchor.MiddleCenter},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 20",OffsetMax = $"{ImgWeight} 40"}
                    }, _layer +  pItem.Id, _layer +  pItem.Id + ".Amount");


                    if (pItem.Blueprint)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer +  pItem.Id,
                            Name = _layer +  pItem.Id+".Blueprint",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage","bp")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 40",OffsetMax = $"{ImgWeight} {ImgWeight+20}"
                                }
                            }
                        });
                        container.Add(new CuiElement
                        {
                            Parent = _layer+pItem.Id,
                            Name = _layer + pItem.Id+".Img",
                            Components =
                            {
                                new CuiRawImageComponent{Png = (string) ImageLibrary.Call("GetImage",pItem.Shortname)},
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 40",OffsetMax = $"{ImgWeight} {ImgWeight+20}"
                                }
                            }
                        });
                    }
                    else
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer+pItem.Id,
                            Name = _layer + pItem.Id+".Img",
                            Components =
                            {
                                new CuiRawImageComponent{Png = (string) ImageLibrary.Call("GetImage",pItem.Image)},
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 40",OffsetMax = $"{ImgWeight} {ImgWeight+20}"
                                }
                            }
                        });
                    }
                    
                    
                    startx += ImgWeight + 10;
                    i++;
                }
                if (sorted.Count > (page + 1)*21)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "730 20",OffsetMax = "890 80"},
                        Button = { Color = HexToCuiColor("#45413B"), Command = $"openinv {page + 1} {category}",Material = "assets/icons/greyout.mat" },
                        Text = { Text = "<b>ВПЕРЁД</b>", Align = TextAnchor.MiddleCenter, FontSize = 21, Font = "robotocondensed-regular.ttf",Color = HexToCuiColor("#a32222") }
                    }, _layer);
                }

                if (page >= 1)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 20",OffsetMax = "160 80"},
                        Button = { Color = HexToCuiColor("#45413B"), Command = page >= 1 ? $"openinv {page - 1} {category}" : "",Material = "assets/icons/greyout.mat" },
                        Text = { Text = "<b>НАЗАД</b>", Align = TextAnchor.MiddleCenter, FontSize = 21, Font = "robotocondensed-regular.ttf",Color = HexToCuiColor("#a32222") }
                    }, _layer);
                }
                /*if (sorted.Count > (page + 1)*21)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = $"0.85 0.03", AnchorMax = $"0.97 0.09", OffsetMax = "0 0" },
                        Button = { Color = HexToCuiColor("d12c2c"), Command = $"openinv {page + 1} {category}"},
                        Text = { Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-regular.ttf" }
                    }, _layer);
                }

                if (page >= 1)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = $"0.72 0.03", AnchorMax = $"0.84 0.09", OffsetMax = "0 0" },
                        Button = { Color = HexToCuiColor("d12c2c"), Command = page >= 1 ? $"openinv {page - 1} {category}" : "" },
                        Text = { Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-regular.ttf" }
                    }, _layer);
                }*/
            }
            
            
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region commands

        [ChatCommand("refreshInv")]
        void RefreshInv(BasePlayer player, string command, string[] args)
        {
            foreach (var inv in _playerData[player.userID])
            {
                _playerData[player.userID].Remove(inv);
            }
            SaveData();
        }

        [ChatCommand("openinv")]
        void OpenInvCommand(BasePlayer player)
        {
            BMenu.Call("SetPage", player.userID, "inv");
            MainUI(player);
        }

        [ConsoleCommand("openinv")]
        void PlayerOpenInventory(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            if (arg.HasArgs(2))
            {
                int page = Convert.ToInt32(arg.Args[0]);
                PrintWarning($"page - {page} | category - {arg.Args[1]}");
                MainUI(arg.Player(),page,arg.Args[1]);
                return;
            }
            if (arg.HasArgs(1))
            {
                int page = arg.Args[0].ToInt();
                
                MainUI(arg.Player(),arg.Args[0].ToInt());
                return;
            }
            
            MainUI(arg.Player());
            
        }

        [ConsoleCommand("takeitemfrominv")]
        void PlayerTakeItem(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null || !arg.HasArgs(1)) return;
            if (arg.HasArgs(3))
            {
                TakeFromInv(arg.Player(), arg.Args[0].ToInt(),arg.Args[1],arg.Args[2].ToInt());
            }
            else
            {
                TakeFromInv(arg.Player(),arg.Args[0].ToInt());
            }
            
        }

        #endregion

        #region API
        
        private void AddToInv(BasePlayer player, string shortname, bool isBluePrint, bool isCommand, int amount, string Image,
            string displayName,ulong skinId = 0,string category = "ВСЕ",string rare = "default")
        {
            
            PlayerItem item = new PlayerItem
            {
                Shortname = shortname,
                Amount = amount,
                Blueprint = isBluePrint,
                IsCommand = isCommand,
                DisplayName = displayName,
                Image = Image,
                SkinId = skinId,
                Id = _playerData[player.userID].Count + 1,
                Category = category,
                Rarity = rare
            };
            _playerData[player.userID].Add(item);
            if (item.IsCommand)
            {
                item.Shortname = item.Shortname.Replace("userid", player.UserIDString);
                InitImage(item);
            }
            //CheckOrAddCataegory(category);
           // PrintWarning($"Added item: {shortname} {isBluePrint} {isCommand} {isBluePrint} {displayName} {Image} {skinId} {category} {rare}\n Items:{_playerData[player.userID].Count}");
        }

        #endregion

        #region helper

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

        /*string[] CalcCategory(BasePlayer player)
        {
            foreach (var var in _playerData[player.userID])
            {
                if (!_category.Contains(var.Category))
                {
                    _category.
                }
            }
        }*/

        #endregion
    }
}