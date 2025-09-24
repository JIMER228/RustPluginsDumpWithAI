using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConVar;
using Facepunch.Extend;
using Facepunch.Models.Database;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Fragments","https://discord.gg/9vyTXsJyKR","1.0")]
    public class Fragments : RustPlugin
    {
        #region var

        [PluginReference] private Plugin ImageLibrary,BMenu,BroadcastSystem;

        #endregion

        #region config

        class Fragment
        {
            public string Name;
            public string Key;
            public int Parts;
            public string Url;
            public string Command;
            public string Description;
            public ulong SkinId = 1358334523;
        }

         static Configuration config = new Configuration();

        class Configuration
        {
            [JsonProperty("Фрагменты")] 
            public List<Fragment> Fragments;
            

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    Fragments = new List<Fragment>
                    {
                        new Fragment
                        {
                            Name = "Test",
                            Key = "frag",
                            Command = "kick userid",
                            Parts = 5,
                            Url = "https://i.ibb.co/gPjD2c6/image.png",
                            Description = "Test descriprition"
                        },
                        new Fragment
                        {
                            Name = "Test",
                            Key = "fragone",
                            Command = "kick userid",
                            Parts = 5,
                            Url = "https://i.ibb.co/gPjD2c6/image.png",
                            Description = "Test descriprition"
                        },
                        new Fragment
                        {
                            Name = "Test",
                            Key = "fragtwo",
                            Command = "kick userid",
                            Parts = 5,
                            Url = "https://i.ibb.co/gPjD2c6/image.png",
                            Description = "Test descriprition"
                        },
                        new Fragment
                        {
                            Name = "Test",
                            Key = "fragthree",
                            Command = "kick userid",
                            Parts = 5,
                            Url = "https://i.ibb.co/gPjD2c6/image.png",
                            Description = "Test descriprition"
                        },
                    }
                };
            }
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) PrintWarning("NULL");
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintError($"Не удалось найти конфигурацию 'oxide/config/{Name}', Создание конфига!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region data

        class PlayerData
        {
            public string Key;
            public int Amount;
        }

        private string _path = "Fragments";

        Dictionary<ulong,List<PlayerData>>_database = new Dictionary<ulong, List<PlayerData>>();

        void CreateNewPlayerData(BasePlayer player)
        {
            _database.Add(player.userID,new List<PlayerData>());
            foreach (var fragment in config.Fragments)
            {
                _database[player.userID].Add(new PlayerData
                {
                    Key = fragment.Key,
                    Amount = 0
                });
            }
        }

        void ReadData()
        {
            _database = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, List<PlayerData>>>(_path);
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(_path,_database);
        }

        #endregion

        #region hooks

        void Unload()
        {
            SaveData();
        }

        void OnServerInitialized()
        {
            timer.Once(11f, InitImages);
            ReadData();
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
            timer.Once(120f,MakeBackup);
            
        }
        
        void MakeBackup()
        {
            SaveData();
            timer.Once(600f, MakeBackup);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (!_database.ContainsKey(player.userID))
                CreateNewPlayerData(player);
            
        }
        
        void InitImages()
        {
            foreach (var fragment in config.Fragments)
            {
                ImageLibrary.Call("AddImage", fragment.Url, fragment.Key);
            }
            
        }

        #endregion

        #region FragmentHooks

        bool CanPlayerClaim(BasePlayer player, string key)
        {
            Fragment fragment = config.Fragments.Find(p => p.Key == key);
            if (fragment == null) return false;
            if (_database[player.userID].Find(p => p.Key == key).Amount / fragment.Parts<1)
                return false;
            else
                return true;
            
        }

        [ChatCommand("gaf")]
        void GiveAllFragments(BasePlayer player, string commands, string[] args)
        {
            foreach (var fragment in config.Fragments)
            {
                GiveFragments(player.userID,fragment.Key,50);
            }
        }
        void ClaimFragmants(BasePlayer player, string key)
        {
            if (!CanPlayerClaim(player, key))
                return;
            Fragment fragment = config.Fragments.Find(p => p.Key == key);
            if (fragment == null) return;
            _database[player.userID].Find(p => p.Key == key).Amount -= fragment.Parts;
            string command = fragment.Command.Replace("userid", player.UserIDString);
            Server.Command(command);
            Effect x = new Effect("assets/bundled/prefabs/fx/repairbench/itemrepair.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(x, player.Connection);
            MainUI(player);
        }

        void GiveFragments(ulong playerId, string key, int amount)
        {
            var data = _database[playerId].Find(p => p.Key == key);
            if (data == null)
            {
                PrintToChat("Data null");
                return;
            }
            BasePlayer player = BasePlayer.FindByID(playerId);

            string sound = "assets/bundled/prefabs/fx/notice/stack.world.fx.prefab";
            BroadcastSystem?.Call("SendCustonNotification", player,"ВЫ ОБНАРУЖИЛИ ТЕХНОЛОГИЮ","Обрывок чертежа технологии был добавлен в раздел убежище.",sound,5f);

            data.Amount += amount;
            
        }
        #endregion

        

        #region UI

        private string _layer = "FragUI";
        private double ImgWeight = 250;

        void MainUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, _layer);
            BMenu.Call("SetActiveSubButton", player, "fragment");
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
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0.3"},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = $"0 595",OffsetMax = $"820 650"}
            }, _layer, _layer + ".Header");
            
            container.Add(new CuiPanel
            {
                Image = {Color = HexToCuiColor("#33322d")},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = $"255 595",OffsetMax = $"555 599"}
            }, _layer, _layer + ".line");
            
            container.Add(new CuiLabel
            {
                Text = {Text = "ФРАГМЕНТЫ ТЕХНОЛОГИЙ",FontSize = 20,Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "1 1"}
            }, _layer + ".Header", _layer + ".Header" + ".Text");

            double starty = 605 - ImgWeight - 40;
            double startx = 35;
            int i = 1;
            foreach (var fragment in config.Fragments)
            {
                if (i == 4)
                {
                    startx = 35;
                    starty = 605 - ImgWeight - 20 - ImgWeight - 45;
                }
                
                PlayerData pdata = _database[player.userID].Find(p => p.Key == fragment.Key);
                container.Add(new CuiPanel
                {
                    Image = {Color = "0 0 0 0"},
                    //Image = {Color = HexToCuiColor("#525252d7")},
                    RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0", OffsetMin = $"{startx} {starty}",OffsetMax = $"{startx+ImgWeight} {starty+ImgWeight+20}"}
                }, _layer, _layer + fragment.Key);
                /*container.Add(new CuiButton
                {
                    Button = {Color = HexToCuiColor("#cf1d1d"),Command = $"fdesk {fragment.Key}",Material = "assets/icons/greyout.mat"},
                    Text = {Text = "Подробнее",FontSize = 12,Align = TextAnchor.MiddleCenter},
                    RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = $"{ImgWeight} 20"}
                }, _layer + fragment.Key, _layer + fragment.Key + ".Btn");*/
                /*if (pdata.Amount/fragment.Parts>=1)
                {
                    container.Add(new CuiButton
                    {
                        Button = {Color = HexToCuiColor("#820101"),Command = $"rewardfragplayer {fragment.Key}",Material = "assets/icons/greyout.mat"},
                        Text = {Text = $"Собрать. ({pdata.Amount}/{fragment.Parts})",FontSize = 12,Align = TextAnchor.MiddleCenter},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = $"{ImgWeight} 40"}
                    }, _layer + fragment.Key, _layer + fragment.Key + ".Btn");
                }
                else
                {
                    container.Add(new CuiLabel
                    {
                        Text = {Text = $"{pdata.Amount}/{fragment.Parts}",Align = TextAnchor.MiddleCenter},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = $"{ImgWeight} 40"}
                    }, _layer + fragment.Key, _layer + fragment.Key + ".Amount");
                }*/
                
                /*container.Add(new CuiElement
                {
                    Parent = _layer + fragment.Key,
                    Name = _layer + fragment.Key+".Img",
                    Components =
                    {
                        new CuiRawImageComponent{Png = (string) ImageLibrary.Call("GetImage",fragment.Key)},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",AnchorMax = "1 1"
                        }
                    }
                });*/
                container.Add(new CuiElement
                {
                    Parent = _layer + fragment.Key,
                    Name = _layer + fragment.Key+".Img",
                    Components =
                    {
                        new CuiRawImageComponent{Png = (string) ImageLibrary.Call("GetImage",fragment.Key)},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = $"{ImgWeight} {ImgWeight+20}"
                        }
                    }
                });
                if (pdata.Amount/fragment.Parts>=1)
                {
                    container.Add(new CuiPanel
                    {
                        Image = {Color = "0 0 0 0.98"},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 60",OffsetMax = $"{ImgWeight-0} 99"}
                    }, _layer + fragment.Key, _layer + fragment.Key + ".Amount");
                    container.Add(new CuiLabel
                    {
                        Text = {Text = $"СОБРАНО: {pdata.Amount} ИЗ {fragment.Parts}\n Привилегии действуют 24 часа",Align = TextAnchor.MiddleCenter},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "1 1",OffsetMin = "0 0",OffsetMax = $"0 0"}
                    }, _layer + fragment.Key+ ".Amount", _layer + fragment.Key + ".Amount"+".X");
                    container.Add(new CuiButton
                    {
                        Button = {Color = "0.294 0.38 0.168 1",Command = $"rewardfragplayer {fragment.Key}",Material = "assets/icons/greyout.mat"},
                        Text = {Text = $"СОБРАТЬ",FontSize = 20,Align = TextAnchor.MiddleCenter,Color = "0.647 0.917 0.188 1"},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "40 15",OffsetMax = $"{ImgWeight-40} 55"}
                    }, _layer + fragment.Key, _layer + fragment.Key + ".Btn");
                }
                else
                {
                    container.Add(new CuiPanel
                    {
                        Image = {Color = "0 0 0 0.98"},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 60",OffsetMax = $"{ImgWeight-0} 99"}
                    }, _layer + fragment.Key, _layer + fragment.Key + ".Amount");
                    container.Add(new CuiLabel
                    {
                        Text = {Text = $"СОБРАНО: {pdata.Amount} ИЗ {fragment.Parts}\n Привилегии действуют 24 часа",Align = TextAnchor.MiddleCenter},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "1 1",OffsetMin = "0 0",OffsetMax = $"0 0"}
                    }, _layer + fragment.Key+ ".Amount", _layer + fragment.Key + ".Amount"+".X");
                }
                startx += ImgWeight + 10;
                i++;
            }
            CuiHelper.AddUi(player, container);
            
        }

        private string _descLayer = "DescUI";
        void DescriptionUI(BasePlayer player,string key)
        {
            CuiHelper.DestroyUi(player, _descLayer);

            Fragment fragment = config.Fragments.Find(k => k.Key == key);
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = {Color = HexToCuiColor("#000000fc")},
                RectTransform = {AnchorMin = "0.5 0.5",AnchorMax = "0.5 0.5",OffsetMin = "-100 -100",OffsetMax = "100 100"}
            }, _layer, _descLayer);
            container.Add(new CuiLabel
            {
                Text = {Text = fragment.Description,Align = TextAnchor.MiddleCenter,FontSize = 16}
            }, _descLayer, _descLayer + ".Text");
            container.Add(new CuiButton
            {
                Button = {Close = _descLayer,Color = "1 0 0 0.9",Material = "assets/icons/greyout.mat"},
                Text = {Text = "X",Align = TextAnchor.MiddleCenter},
                RectTransform = {AnchorMin = "0.9 0.9",AnchorMax = "0.98 0.98"}
            }, _descLayer, _descLayer + ".Close");
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region ItemFragments
        
        

        #region Barrels

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!(entity is LootContainer) || !(info.InitiatorPlayer is BasePlayer) || UnityEngine.Random.Range(0, 100) > 1)
                return;
            CreateFragmentDrop(entity.transform.position);
        }
        void CreateFragmentDrop(Vector3 position)
        {
            Fragment fragment = config.Fragments.GetRandom();
            Item item = ItemManager.CreateByPartialName("glue", 1, fragment.SkinId);
            item.info.displayName = $"{fragment.Name}(Фрагмент)";
            item.info.displayDescription = $"{fragment.Name}(Фрагмент)";
            item.DropAndTossUpwards(position);
        }

        #endregion

        #region LootContainers

        private void OnLootSpawn(StorageContainer container)
        {
            if (UnityEngine.Random.Range(0, 100) > 99)
            {
                CreateFragmentContainer(container);
            }
        }

        void CreateFragmentContainer(StorageContainer container)
        {
            Fragment fragment = config.Fragments.GetRandom();
            Item item = ItemManager.CreateByPartialName("glue", 1, fragment.SkinId);
            item.info.displayName = $"{fragment.Name}(Фрагмент)";
            item.info.displayDescription = $"{fragment.Name}(Фрагмент)";
            item.MoveToContainer(container.inventory);
        }
        #endregion
        
        object OnItemPickup(Item item, BasePlayer player)
        {
            List<ulong>skinIds = new List<ulong>();
            foreach (var var in config.Fragments)
            {
                skinIds.Add(var.SkinId);
            }
            if (skinIds.Contains(item.skin))
            {
                Fragment fragment = config.Fragments.Find(p => p.SkinId == item.skin);
                GiveFragments(player.userID,fragment.Key,item.amount);
                item.Remove();
                return false;
            }
            return null;
        }
        
        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
        {
            List<ulong>skinIds = new List<ulong>();
            foreach (var var in config.Fragments)
            {
                skinIds.Add(var.SkinId);
            }
            if (skinIds.Contains(item.skin))
            {
                Fragment fragment = config.Fragments.Find(p => p.SkinId == item.skin);
                GiveFragments(playerLoot.baseEntity.userID,fragment.Key,item.amount);
                item.Remove();
                return false;
            }
            
            return null;
        }

        #endregion

        #region commands

        [ChatCommand("fragment")]
        void OpenUI(BasePlayer player)
        {
            BMenu.Call("SetPage", player.userID, "frag");
            MainUI(player);
        }

        [ConsoleCommand("fdesk")]
        void ShowDescription(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
            {
                return;
            }
            DescriptionUI(arg.Player(),arg.Args[0]);
        }

        [ConsoleCommand("rewardfragplayer")]
        void PlayerTryGetReward(ConsoleSystem.Arg arg)
        {
           
            if (arg.Player() == null || !arg.HasArgs(1)) return;
            BasePlayer player = arg.Player();
            if (CanPlayerClaim(player,arg.Args[0]))
            {
                ClaimFragmants(player,arg.Args[0]);
            }
        }
        [ConsoleCommand("fragmentadplayer")]
        void AddFragmentsToPlayer(ConsoleSystem.Arg arg)
        {
            

            if (!arg.HasArgs(3)) return;
            
            GiveFragments(Convert.ToUInt64(arg.Args[0]),arg.Args[1],arg.Args[2].ToInt());
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

        #endregion
    }
}