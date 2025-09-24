using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using WebSocketSharp;

namespace Oxide.Plugins
{
    [Info("ServerPass", "Xavier", "1.0.1")]
    public class ServerPass : RustPlugin
    {

        [PluginReference] private Plugin ImageLibrary;
        
        
        private Dictionary<string, string> ImageDictionary = new Dictionary<string, string>()
        {
            ["ServerPass_mainEXP"] = "https://i.imgur.com/fNvf5jf.png",
            ["ServerPass_maintop"] = "https://i.imgur.com/MHEACz4.png",
            ["ServerPass_mainItem"] = "https://i.imgur.com/HtGIf07.png",
        };

        #region Data


        public class PlayerData
        {
            public int Level;
            public int Time;
            public int EXP;


            public List<int> _levelSelect = new List<int>();
        }



        public Dictionary<ulong, PlayerData> _playerSettings = new Dictionary<ulong, PlayerData>();

        #endregion
        
        
        
        #region Lang

        public static StringBuilder sb = new StringBuilder();
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
        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
                {
                    ["NoPermission"] = "Недостаточно прав",
                }
                , this, "ru");
            lang.RegisterMessages(new Dictionary<string, string>
                {
                    ["NoPermission"] = "Недостаточно прав",
                }
                , this);
        }

        #endregion



        #region Hooks


        void OnPlayerConnected(BasePlayer player)
        {
            if (!_playerSettings.ContainsKey(player.userID))
                _playerSettings.Add(player.userID, new PlayerData());
            
            GetAvatar(player.userID,
                avatar => ImageLibrary?.Call("AddImage", avatar, $"avatar_{player.UserIDString}"));
        }

        #endregion
        
        
        #region discord
         
         
        public class FancyMessage
        {
            public string content { get; set; }
            public bool tts { get; set; }
            public Embeds[] embeds { get; set; }

            public class Embeds
            {
                public string title { get; set; }
                public int color { get; set; }
                public List<Fields> fields { get; set; }

                public Embeds(string title, int color, List<Fields> fields)
                {
                    this.title = title;
                    this.color = color;
                    this.fields = fields;
                }
            }

            public FancyMessage(string content, bool tts, Embeds[] embeds)
            {
                this.content = content;
                this.tts = tts;
                this.embeds = embeds;
            }

            public string toJSON() => JsonConvert.SerializeObject(this);
        }

        public class Fields
        {
            public string name { get; set; }
            public string value { get; set; }
            public bool inline { get; set; }

            public Fields(string name, string value, bool inline)
            {
                this.name = name;
                this.value = value;
                this.inline = inline;
            }
        }
        

        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json"
        };

        private void Request(string url, string payload, Action<int> callback = null)
        {
            webrequest.Enqueue(url, payload, (code, response) =>
            {
                if (code != 200 && code != 204)
                {
                    if (response != null)
                    {
                        try
                        {
                            JObject json = JObject.Parse(response);
                            if (code == 429)
                            {
                                float seconds = float.Parse(Math.Ceiling((double)(int)json["retry_after"] / 1000).ToString());
                            }
                            else
                            {
                                PrintWarning($" Discord rejected that payload! Responded with \"{json["message"].ToString()}\" Code: {code}");
                            }
                        }
                        catch
                        {
                            PrintWarning($"Failed to get a valid response from discord! Error: \"{response}\" Code: {code}");
                        }
                    }
                    else
                    {
                        PrintWarning($"Discord didn't respond (down?) Code: {code}");
                    }
                }
                try
                {
                    callback?.Invoke(code);
                }
                catch (Exception ex)
                {
                    Interface.Oxide.LogException(" Request callback raised an exception!", ex);
                }
            }, this, Core.Libraries.RequestMethod.POST, _headers);
        }
        
        
        
        private void SendDiscordMsg(string msg)
        {
            List<Fields> fields = new List<Fields>
            {
                new Fields("Информация", msg, true),
            };

            FancyMessage newMessage = new FancyMessage(null, false, new FancyMessage.Embeds[1] { new FancyMessage.Embeds($"БОЕВОЙ ПРОПУСК:", 15427466, fields) });
            Request("https://discord.com/api/webhooks/956244809939578890/SxuyA0dIW_3sj4Kl_nCLQkh3vCpS9qs5HsuvFJsbZzOv7ySf89oWb1rdyrcHnXy2jcYs", newMessage.toJSON());
        }
        
        


        #endregion


        #region Functional
        
        
        
        private void SendNotify(BasePlayer player, String key, int type, params object[] obj)
        {
            player.ChatMessage(GetLang(key, player.UserIDString, obj));
        }


        void TimeHandler()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (_playerSettings.ContainsKey(player.userID))
                    _playerSettings[player.userID].Time++;
                if (_playerSettings[player.userID].Time >= 120)
                {
                    _playerSettings[player.userID].Time = 0;
                    GiveEXP(player, config.GiveEXP);
                }
            }
        }
        
        
        private List<uint> handledContainers = new List<uint>();

        object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (container == null) return null;
            if (container.OwnerID > 0) return null;
            if (handledContainers.Contains(container.net.ID)) return null;
            handledContainers.Add(container.net.ID);
            foreach (var key in config._listBooks)
            {
                if (key.Prefab.ContainsKey(container.ShortPrefabName))
                {
                    if (UnityEngine.Random.Range(0, 100) < key.Prefab[container.ShortPrefabName].Chance)
                    {
                        if (container.inventory.itemList.Count == container.inventory.capacity)
                        {
                            container.inventory.capacity++;
                        }
                        var item = ItemManager.CreateByName("xmas.present.large", 1, key.SkinID);
                        item.name = $"КНИЖКА С EXP ( {key.EXPGive} )";
                        item.MoveToContainer(container.inventory);
                    }
                }
            }
            return null;
        }
        
        private IEnumerator LoadIcons()
        {
            if (ImageLibrary.Call<bool>("IsReady"))
            {
                foreach (var img in ImageDictionary)
                {
                    if (!ImageLibrary.Call<bool>("HasImage", img.Value))
                    {
                        ImageLibrary.Call("AddImage", img.Value, img.Key);
                    }
                    yield return CoroutineEx.waitForSeconds(0.05f);
                }
                foreach (var img in config._listBooks)
                {
                    if (!ImageLibrary.Call<bool>("HasImage", img.ImageBook))
                    {
                        ImageLibrary.Call("AddImage", img.ImageBook, img.ImageBook);
                    }
                    yield return CoroutineEx.waitForSeconds(0.05f);
                }
                foreach (var img in config._levelSettings)
                {
                    if (string.IsNullOrEmpty(img.itemPrize.Image)) continue;
                    if (!ImageLibrary.Call<bool>("HasImage", img.itemPrize.Image))
                    {
                        ImageLibrary.Call("AddImage", img.itemPrize.Image, img.itemPrize.Image);
                    }
                    yield return CoroutineEx.waitForSeconds(0.05f);
                }
            }
            yield return 0;
        }
        
        public string HexToCuiColor(string HEX, float Alpha = 100)
        {
            if (string.IsNullOrEmpty(HEX)) HEX = "#FFFFFF";

            var str = HEX.Trim('#');
            if (str.Length != 6) throw new Exception(HEX);
            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);

            return $"{(double) r / 255} {(double) g / 255} {(double) b / 255} {Alpha / 100}";
        }


        void GiveEXP(BasePlayer player, int EXP)
        {
            if (_playerSettings.ContainsKey(player.userID))
            {
                var data = _playerSettings[player.userID];

                data.EXP += EXP;

                if (data.EXP >= config.EXPStart)
                {
                    if (data.EXP == config.EXPStart)
                    {
                        data.EXP = 0;
                    }
                    else if (data.EXP > config.EXPStart)
                    {
                        data.EXP -= 100;
                    }
                    var findLevel = config._levelSettings.FirstOrDefault(p => p.Level == data.Level + 1);
                    if (findLevel != null)
                    {
                        data.Level++;
                        if (data.Level == 10 || data.Level == 20 || data.Level == 35 || data.Level == 55 ||
                            data.Level == 70 || data.Level == 85 || data.Level == 100)
                        {
                            foreach (var target in BasePlayer.activePlayerList)
                            {
                                target.ChatMessage($"Игрок {player.displayName} достигает {data.Level} уровня в BattlePass и получает секретный приз!");
                            }
                        }
                    }
                }
            }
        }
        
        private readonly Regex Regex = new Regex(@"<avatarFull><!\[CDATA\[(.*)\]\]></avatarFull>");
        
        private void GetAvatar(ulong userId, Action<string> callback)
        {
            if (callback == null) return;

            webrequest.Enqueue($"http://steamcommunity.com/profiles/{userId}?xml=1", null, (code, response) =>
            {
                if (code != 200 || response == null)
                    return;

                var avatar = Regex.Match(response).Groups[1].ToString();
                if (avatar.IsNullOrEmpty())
                    return;

                callback.Invoke(avatar);
            }, this);
        }

        #endregion


        #region Initialized && Unload


        void OnServerInitialized()
        {
            try
            {
                _playerSettings = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, PlayerData>>(Name);
            }
            catch
            {
                _playerSettings = new Dictionary<ulong, PlayerData>();
            }
            
            LoadMessages();
            timer.Every(60, () =>
            {
                TimeHandler();
            });
            permission.RegisterPermission(config.Permission, this);
            ServerMgr.Instance.StartCoroutine(LoadIcons());

            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(player);
            }
        }

        void OnServerSave()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, _playerSettings);
        }


        void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, _playerSettings);
        }

        

        #endregion


        #region UI
        


        [ConsoleCommand("serverpass.opentab")]
        void OpenPassConsole(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            CuiHelper.DestroyUi(player, "StaticMenu");
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiElement 
            {
                Parent = "UI_MenuLayer",
                Name = "StaticMenu",
                Components =
                {
                    new CuiImageComponent { Color = "0 0 0 0"},
                    new CuiRectTransformComponent {AnchorMin = "0.125 0.08333331", AnchorMax = "0.8755208 0.8324074"}
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = "StaticMenu",
                Name = "StaticMenu" + $".Progress",
                Components =
                {
                    new CuiImageComponent {Color = HexToCuiColor("#1D1D1D", 70)},
                    new CuiRectTransformComponent
                        {AnchorMin = "0 0", AnchorMax = $"0.9979181 0.07045738"}
                }
            });
            container.Add(new CuiElement
            {
                Parent = "StaticMenu" + $".Progress",
                Components =
                {
                    new CuiImageComponent {Color = HexToCuiColor("#6C26DD", 70)},
                    new CuiRectTransformComponent
                        {AnchorMin = "0 0", AnchorMax = $"{(float)  _playerSettings[player.userID].EXP / 100} 1"}
                }
            });
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = $"{_playerSettings[player.userID].EXP} / 100 EXP до следующего уровня", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 14 }
            }, "StaticMenu" + $".Progress");
            
            container.Add(new CuiElement
            {
                Parent = "StaticMenu",
                Components =
                {
                    new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", $"avatar_{player.userID.ToString()}")},
                    new CuiRectTransformComponent { AnchorMin = $"0.02290077 0.6922126", AnchorMax = $"0.1721027 0.960445" }
                }
            });
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.1804303 0.6946847", AnchorMax = "0.4427481 0.960445", OffsetMax = "0 0" },
                Text = { Text = $"{player.displayName.ToUpper()}\n<size=12>{player.userID}</size>\n\nLEVEL {_playerSettings[player.userID].Level}", Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 14 }
            }, "StaticMenu");
            
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.6564885 0.8491966", AnchorMax = "0.9417071 0.9035847", OffsetMax = "0 0" },
                Text = { Text = $"СЕЗОННЫЙ ПРОПУСК", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 25 }
            }, "StaticMenu");
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.7619708 0.7181706", AnchorMax = "0.9417071 0.7453646", OffsetMax = "0 0" },
                Text = { Text = $"получай {config.GiveEXP} EXP каждые 2 часа  ", Align = TextAnchor.MiddleRight, Font = "robotocondensed-regular.ttf", FontSize = 11 }
            }, "StaticMenu");
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.6599584 0.5030903", AnchorMax = "0.7973629 0.5302843", OffsetMax = "0 0" },
                Text = { Text = $"Никнейм:", Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 9 }
            }, "StaticMenu");
                        container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.796669 0.5030903", AnchorMax = "0.9389313 0.5302843", OffsetMax = "0 0" },
                Text = { Text = $"Уровень:     ", Align = TextAnchor.MiddleRight, Font = "robotocondensed-regular.ttf", FontSize = 9 }
            }, "StaticMenu");
                        
                        
                        
            container.Add(new CuiElement
            {
                Parent = "StaticMenu",
                Components =
                {
                    new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "ServerPass_mainEXP")},
                    new CuiRectTransformComponent { AnchorMin = $"0.6564885 0.7503091", AnchorMax = $"0.9417071 0.8355995" }
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = $"0.6564885 0.7503091", AnchorMax = $"0.9417071 0.8355995"},
                Button =
                {
                    Color = "0 0 0 0",
                    Command = $""
                },
                Text =
                {
                    Text = $"до получение EXP: { 120 - _playerSettings[player.userID].Time} М.", Font = "robotocondensed-regular.ttf",
                    FontSize = 16, Align = TextAnchor.MiddleCenter
                }
            }, "StaticMenu");
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.6592643 0.5414091", AnchorMax = "0.9382374 0.5883808", OffsetMax = "0 0" },
                Text = { Text = $"РЕЙТИНГ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 20 }
            }, "StaticMenu");
            
            foreach (var check in _playerSettings.OrderByDescending(p => p.Value.Level).Select((i, t) => new {A = i, B = t}).Take(5))
            {
                container.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin =
                            $"{0.6599584 + check.B * 0.120 - Math.Floor((double) check.B / 1) * 1 * 0.120} {0.4363412 - Math.Floor((double) check.B / 1) * 0.05}",
                        AnchorMax =
                            $"{0.9382373 + check.B * 0.120 - Math.Floor((double) check.B / 1) * 1 * 0.120} {0.4981459 - Math.Floor((double) check.B / 1) * 0.05}",
                        OffsetMax = "0 0"
                    },
                    Button =
                    {
                        Color = "0 0 0 0",
                        Command = $"",
                    },
                    Text =
                    {
                        Text = $"", Align = TextAnchor.LowerRight, Font = "robotocondensed-bold.ttf", FontSize = 15
                    }
                }, "StaticMenu", "StaticMenu" + $".{check.B}.PlayerList");
                
                container.Add(new CuiElement
                {
                    Parent = "StaticMenu" + $".{check.B}.PlayerList",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "ServerPass_maintop")},
                        new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" }
                    }
                });
                
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = $"     {covalence.Players.FindPlayer(check.A.Key.ToString()).Name}", Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 12 }
                }, "StaticMenu" + $".{check.B}.PlayerList");
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = $"{check.A.Value.Level}     ", Align = TextAnchor.MiddleRight, Font = "robotocondensed-regular.ttf", FontSize = 12 }
                }, "StaticMenu" + $".{check.B}.PlayerList");
            }


            CuiHelper.AddUi(player, container);
            OpenPassUI(player, 1);
        }


        void OpenPassUI(BasePlayer player, int page)
        {
            CuiElementContainer container = new CuiElementContainer();
            
            CuiHelper.DestroyUi(player, "StaticMenu" + ".back");
            CuiHelper.DestroyUi(player, "StaticMenu" + ".next");
            
            
            int pagex = page + 1;
            
            

            container.Add(new CuiElement
            {
                Parent = "StaticMenu",
                Name = "StaticMenu" + ".next",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string)ImageLibrary?.Call("GetImage", "https://i.imgur.com/P5rDCQP.png"), Color = pagex > 0 && (pagex - 1) * 4 < config._levelSettings.Count ? HexToCuiColor("") : HexToCuiColor("#FFFFFF", 10)
                    },
                    new CuiRectTransformComponent
                        { AnchorMin = "0.5718245 0.0865266", AnchorMax = "0.6051347 0.1458591" },
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button =
                {
                    Color = "0 0 0 0",
                    Command = pagex > 0 && (pagex - 1) * 4 < config._levelSettings.Count
                        ? $"PassServer page {page + 1}"
                        : ""
                },
                Text =
                {
                    Text = "", Font = "robotocondensed-regular.ttf",
                    FontSize = 20, Align = TextAnchor.MiddleCenter
                }
            }, "StaticMenu" + ".next");
            container.Add(new CuiElement
            {
                Parent = "StaticMenu",
                Name = "StaticMenu" + ".back",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string)ImageLibrary?.Call("GetImage", "https://i.imgur.com/b4TUg0Z.png"), Color = page != 1 ? HexToCuiColor("") : HexToCuiColor("#FFFFFF", 10)
                    },
                    new CuiRectTransformComponent
                        { AnchorMin = "0.5329632 0.0865266", AnchorMax = "0.5662735 0.1458591" },
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button =
                {
                    Color = "0 0 0 0",
                    Command = page != 1
                        ? $"PassServer page {page - 1}"
                        : ""
                },
                Text =
                {
                    Text = "", Font = "robotocondensed-regular.ttf",
                    FontSize = 20, Align = TextAnchor.MiddleCenter
                }
            }, "StaticMenu" + ".back");
            
            
            for (int i = 0; i < 5; i++)
            {
                CuiHelper.DestroyUi(player, "StaticMenu" + $".{i}.ListItems");
            }

            foreach (var check in config._levelSettings.Select((i, t) => new {A = i, B = t - (page - 1) * 4}).Skip((page - 1) * 4).Take(4))
            {
                container.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin =
                            $"{0.02290077 + check.B * 0.1475 - Math.Floor((double) check.B / 5) * 5 * 0.1475} {0.1606922 - Math.Floor((double) check.B / 5) * 0.305}",
                        AnchorMax =
                            $"{0.1616933 + check.B * 0.1475 - Math.Floor((double) check.B / 5) * 5 * 0.1475} {0.4684796 - Math.Floor((double) check.B / 5) * 0.305}",
                        OffsetMax = "0 0"
                    },
                    Button =
                    {
                        Color = "0 0 0 0",
                        Command = "",
                    },
                    Text =
                    {
                        Text = $"", Align = TextAnchor.UpperLeft, Font = "robotocondensed-regular.ttf", FontSize = 15
                    }
                }, "StaticMenu", "StaticMenu" + $".{check.B}.ListItems");
                
                
                container.Add(new CuiElement
                {
                    Parent = "StaticMenu" + $".{check.B}.ListItems",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "ServerPass_mainItem")},
                        new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" }
                    }
                });
                
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.3895582", AnchorMax = "1 0.4819278", OffsetMax = "0 0" },
                    Text = { Text = $"LEVEL {check.A.Level}", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 11 }
                }, "StaticMenu" + $".{check.B}.ListItems");
                
                if (!string.IsNullOrEmpty(check.A.itemPrize.Image))
                {
                    container.Add(new CuiElement
                    {
                        Parent = "StaticMenu" + $".{check.B}.ListItems",
                        Components =
                        {
                            new CuiRawImageComponent
                                {Png = (string) ImageLibrary?.Call("GetImage", check.A.itemPrize.Image)},
                            new CuiRectTransformComponent {AnchorMin = "0.2449998 0.5461847", AnchorMax = "0.7499999 0.9357429"},
                        }
                    });
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Parent = "StaticMenu" + $".{check.B}.ListItems",
                        Components =
                        {
                            new CuiRawImageComponent
                                {Png = (string) ImageLibrary?.Call("GetImage", check.A.itemPrize.ShortName)},
                            new CuiRectTransformComponent {AnchorMin = "0.2449998 0.5461847", AnchorMax = "0.7499999 0.9357429"},
                        }
                    });
                }
                string command = "";
                string text = "";

                var find = _playerSettings[player.userID]._levelSelect.Any(p => p == check.A.Level);
                if (find)
                {
                    command = "";
                    text = "Получено";
                }
                
                if (!find && _playerSettings[player.userID].Level >= check.A.Level)
                {
                    command = $"PassServer givePrize {check.A.Level} {page}";
                    text = "Получить";
                }
                
                if (!find && _playerSettings[player.userID].Level < check.A.Level)
                {
                    command = $"";
                    text = "Недоступно";
                }
                
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 0.1927711"},
                    Button =
                    {
                        Color = "0 0 0 0",
                        Command = command
                    },
                    Text =
                    {
                        Text = text, Font = "robotocondensed-regular.ttf",
                        FontSize = 12, Align = TextAnchor.MiddleCenter
                    }
                }, "StaticMenu" + $".{check.B}.ListItems");
            }

            CuiHelper.AddUi(player, container);
        }
        

        #endregion




        #region ConsoleCommand


        [ConsoleCommand("PassServer")]
        void ConsoleMain(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();

            if (args.Args[0] == "page")
            {
                int page = 1;
                if (int.TryParse(args.Args[1], out page))
                {
                    OpenPassUI(player, page);
                }
            }
            else if (args.Args[0] == "givePrize")
            {
                var find = config._levelSettings.FirstOrDefault(p => p.Level == int.Parse(args.Args[1]));
                if (find != null)
                {
                    GivePrize(player, find);
                    int page = 1;
                    if (int.TryParse(args.Args[2], out page))
                    {
                        OpenPassUI(player, page);
                    }
                }
            }
        }

        #endregion


        
        void APIChangeUserBalance(ulong steam, int balanceChange, Action<string> callback)
        {
            plugins.Find("RustStore")?.CallHook("APIChangeUserBalance", steam, balanceChange, new Action<string>((result) =>
            {
                if (result == "SUCCESS")
                {
                    Interface.Oxide.LogDebug($"Баланс пользователя {steam} увеличен на {balanceChange}");
                    return;
                }
                Interface.Oxide.LogDebug($"Баланс не был изменен, ошибка: {result}");
            }));
        }


        void GivePrize(BasePlayer player, LevelSettings settings)
        {
            if (settings.itemPrize.TypePrize == 1)
            {
                var item = ItemManager.CreateByName(settings.itemPrize.ShortName, settings.itemPrize.Amount,
                    settings.itemPrize.SkinID);
                player.GiveItem(item);
            }
            _playerSettings[player.userID]._levelSelect.Add(settings.Level);
            if (settings.itemPrize.TypePrize == 2)
            {
                APIChangeUserBalance(player.userID, settings.itemPrize.Amount, null);
            }
            if (settings.itemPrize.TypePrize == 3)
            {
                SendDiscordMsg($"Игрок {player.displayName}({player.userID}) ( {settings.Level} )");
            }
        }
        
        
        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (item == null) return null;
            if (action != "unwrap") return null;
            if (player == null) return null;
            if (item.info.shortname != "xmas.present.large") return null;
            if (item.skin == 0) return null;
            if (!player.GetBuildingPrivilege())
            {
                player.ChatMessage("Поздравляем, ты нашел магическую книгу, для ее активации ты должен быть в зоне своего шкафа!");
                return false;
            }

            if (player.GetBuildingPrivilege().authorizedPlayers.FirstOrDefault(p => p.userid == player.userID) == null)
            {
                player.ChatMessage("Поздравляем, ты нашел магическую книгу, для ее активации ты должен быть в зоне своего шкафа!");
                return false; 
            }
            var find = config._listBooks.FirstOrDefault(p => p.SkinID == item.skin);
            if (find != null)
            {
                GiveEXP(player, find.EXPGive);
                if (item.amount > 1)
                    item.amount--;
                else item.RemoveFromContainer();
                return false;
            }
            return null;
        }
        
        
        
        [ConsoleCommand("giver.book")]
        void GiveCommands(ConsoleSystem.Arg args)
        {
            if (!args.IsAdmin) return;
            var target = args.Args[0];

            var targetPlayer = BasePlayer.Find(target);
            if (targetPlayer == null) return;
            ulong skinId = ulong.Parse(args.Args[1]);
            var find = config._listBooks.FirstOrDefault(p => p.SkinID == skinId);
            if (find == null) return;
            var item = ItemManager.CreateByName("xmas.present.large", 1, find.SkinID);
            item.name = $"КНИЖКА С EXP ( {find.EXPGive} )";
            targetPlayer.GiveItem(item);
        }
        
        
        
        
        
        
        
        
        #region Configuration


        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < new VersionNumber(1, 0, 1))
            {
                PrintWarning("Config update detected! Updating config values...");
                PrintWarning("Config update completed!");
            }

            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        


        public class ItemPrize
        {
            [JsonProperty("Тип предмета ( 1 - предмет, 2 - деньги на магазин, 3 - скин )")]
            public int TypePrize;

            [JsonProperty("ShortName предмета")] 
            public string ShortName;

            [JsonProperty("SkinID Предмета")] 
            public ulong SkinID;

            [JsonProperty("Количество предмета")] 
            public int Amount;

            [JsonProperty("Картинка предмета ( если оставить пустое поле - картинка будет браться с настройки ShortName )")]
            public string Image;
        }

        public class LevelSettings
        {
            [JsonProperty("Уровень")] 
            public int Level;

            [JsonProperty("Настройка приза")] 
            public ItemPrize itemPrize;
        }

        public class Drop
        {
            [JsonProperty("Минимальное количество выпадения")]
            public int Min;
            [JsonProperty("Максимальное количество выпадения")]
            public int Max;
            [JsonProperty("Шанс что он появится в луте")]
            public int Chance;
        }

        public class BookSettings
        {
            [JsonProperty("SkinID предмета")] 
            public ulong SkinID;

            [JsonProperty("Картинка предмета")] 
            public string ImageBook;

            [JsonProperty("Сколько он будет давать EXP при использовании")]
            public int EXPGive;

            [JsonProperty("Настройка выпадения предмета")]
            public Dictionary<string, Drop> Prefab;
            
        }


        private class PluginConfig
        {
            [JsonProperty("Пермишенс для открытия")]
            public String Permission;

            [JsonProperty("Сколько игроку нужно EXP что бы пройти 1 уровень?")]
            public int EXPStart;

            [JsonProperty("Сколько игроку будет падать опыта при отыгровке 2-ух часов?")]
            public int GiveEXP;
            

            [JsonProperty("Настройка уровней и их призов")]
            public List<LevelSettings> _levelSettings = new List<LevelSettings>();

            [JsonProperty("Настройка книг")] 
            public List<BookSettings> _listBooks = new List<BookSettings>();
            
            [JsonProperty("Verison Configuration")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    PluginVersion = new VersionNumber(),
                    Permission = "serverpass.use",
                    EXPStart = 100,
                    GiveEXP = 20,
                    _listBooks = new List<BookSettings>()
                    {
                        new BookSettings()
                        {
                            SkinID = 2783868504,
                            EXPGive = 15,
                            ImageBook = "https://i.imgur.com/MIk0beb.png",
                            Prefab = new Dictionary<string, Drop>()
                            {
                                ["crate_basic"] = new Drop()
                                {
                                    Max = 1,
                                    Min = 1,
                                    Chance = 25
                                }
                            },
                        },
                        new BookSettings()
                        {
                            SkinID = 2783868626,
                            EXPGive = 25,
                            ImageBook = "https://i.imgur.com/O6OJady.png",
                            Prefab = new Dictionary<string, Drop>()
                            {
                                ["crate_basic"] = new Drop()
                                {
                                    Max = 1,
                                    Min = 1,
                                    Chance = 25
                                }
                            },
                        },
                        new BookSettings()
                        {
                            SkinID = 2783868707,
                            EXPGive = 35,
                            ImageBook = "https://i.imgur.com/P2kKwEr.png",
                            Prefab = new Dictionary<string, Drop>()
                            {
                                ["crate_basic"] = new Drop()
                                {
                                    Max = 1,
                                    Min = 1,
                                    Chance = 25
                                }
                            },
                        },
                        new BookSettings()
                        {
                            SkinID = 2783868829,
                            EXPGive = 50,
                            ImageBook = "https://i.imgur.com/iPlFPqj.png",
                            Prefab = new Dictionary<string, Drop>()
                            {
                                ["crate_basic"] = new Drop()
                                {
                                    Max = 1,
                                    Min = 1,
                                    Chance = 25
                                }
                            },
                        },
                        new BookSettings()
                        {
                            SkinID = 2783868902,
                            EXPGive = 75,
                            ImageBook = "https://i.imgur.com/CTCJClU.png",
                            Prefab = new Dictionary<string, Drop>()
                            {
                                ["crate_basic"] = new Drop()
                                {
                                    Max = 1,
                                    Min = 1,
                                    Chance = 25
                                }
                            },
                        },
                        new BookSettings()
                        {
                            SkinID = 2783868978,
                            EXPGive = 100,
                            ImageBook = "https://i.imgur.com/9Nla1WA.png",
                            Prefab = new Dictionary<string, Drop>()
                            {
                                ["crate_basic"] = new Drop()
                                {
                                    Max = 15,
                                    Min = 10,
                                    Chance = 25
                                }
                            },
                        },
                    },
                    _levelSettings = new List<LevelSettings>()
                    {
                        new LevelSettings()
                        {
                            Level = 1,
                            itemPrize = new ItemPrize()
                            {
                                TypePrize = 1,
                                ShortName = "rifle.ak",
                                Amount = 1,
                                SkinID = 0,
                                Image = ""
                            }
                        },
                        new LevelSettings()
                        {
                            Level = 2,
                            itemPrize = new ItemPrize()
                            {
                                TypePrize = 1,
                                ShortName = "rifle.ak",
                                Amount = 1,
                                SkinID = 0,
                                Image = ""
                            }
                        },
                        new LevelSettings()
                        {
                            Level = 3,
                            itemPrize = new ItemPrize()
                            {
                                TypePrize = 1,
                                ShortName = "rifle.ak",
                                Amount = 1,
                                SkinID = 0,
                                Image = ""
                            }
                        },
                    }
                };
            }

            #endregion
        }
    }
}