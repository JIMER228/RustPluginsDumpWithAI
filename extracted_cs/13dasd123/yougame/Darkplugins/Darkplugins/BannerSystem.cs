// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("BannerSystem", "https://discord.gg/dNGbxafuJn", "1.0.1")]
    public class BannerSystem : RustPlugin
    {
        #region eNum

        private enum GiveAction
        {
            Default, // 0
            Random, // 1
            None, // 2 - не используется
            
            Give // 3 - не используется
        }

        #endregion

        #region Classes

        private class Banner
        {
            [JsonProperty("Ссылка на изображение баннера")]
            public string URL;
            //[JsonProperty("Описание способа получения баннера")]
            //public string Description;

            [JsonProperty("Действие для получение баннера (0 - по умолчанию, 1 - выпадает в рандоме, 3 - по привилегии)")]
            public GiveAction ReasonAction;
            [JsonProperty("Название пермишена для получения баннера")]
            public string Amount;
        }

        private class PlayerBanner
        {
            [JsonProperty("Текущий баннер")] 
            public string ActiveBanner;
            [JsonProperty("Доступные баннеры")] 
            public List<string> Banners = new List<string>();

            [JsonProperty("Последняя рулетка")] 
            public double NextRoulette;
        }
        
        [JsonProperty("Название слоя нового баннера")]
        private string LayerNew = "UI_NewBanner";
        [JsonProperty("Название слоя меню")]
        private string LayerMenu = "UI_MenuBanner";
        
        [JsonProperty("Список доступных для игроков баннеров")]
        private Dictionary<string, Banner> bannerDictionary = new Dictionary<string, Banner>
        {
            ["first"] = new Banner
            {
                //Description = "Первое описание",
                ReasonAction = GiveAction.Default,
                URL = "https://i.imgur.com/wXufK6L.jpg",
                Amount = ""
            }
        };
        [JsonProperty("Список игроков и их баннеров")]
        private Dictionary<ulong, PlayerBanner> playerBanners = new Dictionary<ulong, PlayerBanner>();

        #endregion

        #region Hooks


        private void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (!player.userID.IsSteamId()) return;
            try
            {
                if (info.damageTypes.GetMajorityDamageType() == DamageType.Suicide || info.damageTypes.GetMajorityDamageType() == DamageType.Generic || !(info?.Initiator is BasePlayer) || player.GetComponent<NPCPlayer>() != null || info.InitiatorPlayer.GetComponent<NPCPlayer>() != null)
                    return;
            
                //if (player.userID == 76561198115317493)
                //    GiveBanner(info.InitiatorPlayer.userID, "ban.streamsniper");
            
                DrawGUI(player, info.InitiatorPlayer, info?.Weapon?.GetItem().info.displayName.english, Math.Round(Vector3.Distance(player.transform.position, info.InitiatorPlayer.transform.position)).ToString());

            }
            catch(NullReferenceException)
            {}
        }
        
        private void OnPlayerRespawn(BasePlayer player) => CuiHelper.DestroyUi(player, LayerNew);
        private void OnPlayerRespawned(BasePlayer player) => CuiHelper.DestroyUi(player, LayerNew);
        
        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        
        private void GetConfig<T>(string Key, ref T var)
        {
            if (Config[Key] != null)
            {
                var = Config.ConvertValue<T>(Config[Key]);
            }
            else
            {
                Config[Key] = var;
            }
        }
        
        protected override void LoadDefaultConfig()
        {
            GetConfig("1. Настройки соответстия группы -> красивое имя", ref playerGroups);
            
            Config.Save();
        }
        
        private void OnServerInitialized()
        {
            LoadDefaultConfig();
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("Banners/PlayerList"))
                playerBanners = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerBanner>>("Banners/PlayerList");
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("Banners/BannerList"))
                bannerDictionary = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, Banner>>("Banners/BannerList");
            else
            {
                Interface.Oxide.DataFileSystem.WriteObject("Banners/BannerList", bannerDictionary);
                OnServerInitialized();
                return;
            }
            
            foreach (var check in bannerDictionary)
                ImageLibrary.Call("AddImage", check.Value.URL, check.Key);

            foreach (var check in bannerDictionary.Where(p => p.Value.ReasonAction == GiveAction.Give && !p.Value.Amount.Contains("null")))
                permission.RegisterPermission(check.Value.Amount, this);
            
           // TryGetBanners();
            int newBanners = bannerDictionary.Count(p => p.Value.ReasonAction == GiveAction.None);
            
            if (newBanners > 0)
            {
                PrintError($"Необходимо настроить {newBanners} баннеров.");
            }

            foreach (var check in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(check);
            }
            MakeRoulette();
        }

        private void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Banners/BannerList", bannerDictionary);
            Interface.Oxide.DataFileSystem.WriteObject("Banners/PlayerList", playerBanners);
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (playerBanners.ContainsKey(player.userID)) return;
            var playerBanner = new PlayerBanner();
            playerBanner.Banners = bannerDictionary.Where(p => p.Value.ReasonAction == GiveAction.Default)
                .Select(p => p.Key)
                .ToList();
            playerBanner.ActiveBanner = playerBanner.Banners.GetRandom();
            playerBanner.NextRoulette = CurrentTime();
            playerBanners.Add(player.userID, playerBanner);
        }

        #endregion

        #region Functions

        private void MakeRoulette()
        {
            for (var index = 0; index < BasePlayer.activePlayerList.Count; index++)
            {
                var check = BasePlayer.activePlayerList[index];
                if (!(playerBanners[check.userID].NextRoulette - CurrentTime() < 0)) continue;
                GiveBanner(check.userID, bannerDictionary
                    .Where(p => p.Value.ReasonAction == GiveAction.Random)
                    .ToList()
                    .GetRandom().Key);
                playerBanners[check.userID].NextRoulette = CurrentTime() + 86400;
            }

            timer.Once(300, MakeRoulette);
        }

        private void GiveBanner(ulong playerId, string name)
        {
            if (playerBanners[playerId].Banners.Contains(name))
                return;
            
            playerBanners[playerId].Banners.Add(name);
            
            var player = BasePlayer.FindByID(playerId);
            if (player != null && player.IsConnected)
                DrawNewBanner(player, name);
        }

        private void TakeBanner(ulong playerId, string name)
        {
            if (playerBanners[playerId].Banners.Contains(name))
                playerBanners[playerId].Banners.Remove(name);
            else
            {
                return;
            }

            if (playerBanners[playerId].ActiveBanner == name)
                playerBanners[playerId].ActiveBanner = playerBanners[playerId].Banners.GetRandom();
            
            BasePlayer player = BasePlayer.FindByID(playerId);
            if (player != null && player.IsConnected)
                SendReply(player, "У вас забрали один из ваших баннеров, возможно, у вас кончилась привилегия!");
        }

        #endregion

        #region Commands

        [ChatCommand("banner")]
        private void cmdChatBanner(BasePlayer player)
        {
            foreach (var check in bannerDictionary.Where(p => p.Value.ReasonAction == GiveAction.Give && !p.Value.Amount.Contains("null")))
            {
                if (permission.UserHasPermission(player.UserIDString,check.Value.Amount))
                    GiveBanner(player.userID, check.Key);
                else 
                    TakeBanner(player.userID, check.Key);
            }
            
            int cooldown = (int) (playerBanners[player.userID].NextRoulette - CurrentTime());
            if (cooldown > 0)
            {
                SendReply(player, $"Вы сможете получить случайный баннер через: <color=#81B67A>{FormatTime(TimeSpan.FromSeconds(cooldown), maxSubstr:2)}</color>");
            }
            ChooseBanner(player);
        }

        [ConsoleCommand("givealllol")]
        private void cmdTest(ConsoleSystem.Arg args)
        {
            if (args.Player() != null && !args.Player().IsAdmin)
                return;
            
            foreach (var check in bannerDictionary)
            {
                GiveBanner(args.Player().userID, check.Key);
            }
        }

        [ConsoleCommand("UI_ChooseBanner")]
        private void consoleChooseBanner(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
            {
                PrintWarning("Вы используете команду через консоль!");
                return;
            }

            string[] args = arg.Args;

            if (args.Length == 0)
                return;
            
            if (args[0].ToLower() == "change")
            {
                ChooseBanner(player, Math.Min(Convert.ToInt32(args[1]), playerBanners[player.userID].Banners.Count - 1));
            }
            if (args[0].ToLower() == "set")
            {
                playerBanners[player.userID].ActiveBanner = playerBanners[player.userID].Banners[Convert.ToInt32(args[1])];
                ChooseBanner(player);
            }
        }
        
        [ConsoleCommand("banner")]
        private void cmdConsole(ConsoleSystem.Arg args)
        {
            if (args.Player() != null)
                return;

            if (args.Args.Length != 3)
                return;

            string action = args.Args[0].ToLower();
            string who = args.Args[1];
            string banner = args.Args[2];
            
            switch (action)
            {
                case "add":
                {
                    if (who == "all")
                    {
                        foreach (var p in BasePlayer.activePlayerList)
                        {
                            GiveBanner(p.userID, banner);
                        }
                        PrintWarning($"Всем был выдан баннер: {banner}");
                    }
                    else
                    {
                        GiveBanner(ulong.Parse(who), banner);
                        PrintWarning($"Игроку {who} выдан баннер: {banner}");
                    }
                    break;
                }
                case "remove":
                {
                    if (who == "all")
                    {
                        foreach (var p in BasePlayer.activePlayerList)
                        {
                            TakeBanner(p.userID, banner);
                        }
                        PrintWarning($"У всех забрали баннер: {banner}");
                    }
                    else
                    {
                        TakeBanner(ulong.Parse(who), banner);
                        PrintWarning($"У игрока {who} забрали баннер: {banner}");
                    }
                    break;
                }
                default:
                {
                    PrintError("Неизвестное действие!");
                    return;
                }
            }
        }
        

        #endregion

        #region GUI
        
        private Dictionary<string, string> playerGroups = new Dictionary<string, string>
        {
            ["admin"] = "Администратор",
            ["default"] = "Выживший"
        };

        private string GetTag(BasePlayer player)
        {
            string tag = "Выживший";
            foreach (var check in playerGroups)
            {
                if (permission.UserHasGroup(player.UserIDString, check.Key))
                {
                    tag = check.Value;
                    return tag;
                }
            }

            return tag;
        }

        [PluginReference] private Plugin ImageLibrary;
        private void ChooseBanner(BasePlayer player, int index = -1)
        {
            var container = new CuiElementContainer();
            var playerBanner = this.playerBanners[player.userID];
            
            if (index == -1)
            {
                index = playerBanner.Banners.IndexOf(playerBanner.ActiveBanner);
                
                CuiHelper.DestroyUi(player, LayerMenu);

                container.Add(new CuiPanel
                {
                    /* Главная панель */
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0.2916667 0.3148148", AnchorMax = "0.2916667 0.3148148", OffsetMax = "533 266"},
                    Image = { Color = "0 0 0 0", Sprite = "assets/content/ui/ui.background.tile.psd" }
                }, "Hud", LayerMenu);

                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "-100 -100", AnchorMax = "100 100"},
                    Button = { Sprite = "assets/content/ui/ui.background.tiletex.psd", Color = "0 0 0 0" , Close = LayerMenu},
                    Text = {Text = ""}
                }, LayerMenu);

                container.Add(new CuiElement
                {
                    Parent = LayerMenu,
                    Components =
                    {
                        new CuiRawImageComponent {Color = HexToRustFormat("#58585861"), Sprite = "assets/content/ui/ui.background.tile.psd"},
                        new CuiRectTransformComponent {AnchorMin = "0 0.20", AnchorMax = "1 1"}
                    }
                });
    
                container.Add(new CuiElement
                {
                    Parent = LayerMenu,
                    Name = LayerMenu + ".Header",
                    Components =
                    {
                        new CuiRawImageComponent { Color = HexToRustFormat("#81B67BFF"), Sprite = "assets/content/ui/ui.background.tile.psd"},
                        new CuiRectTransformComponent { AnchorMin = "0 0.8426808", AnchorMax = "1 1.012894" },
                        //new CuiOutlineComponent { Color = "#6B6B6BFF"), Distance = "0 2" }
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = LayerMenu + ".Header",
                    Components =
                    {
                        new CuiTextComponent { Text = "<size=26>ПАНЕЛЬ УПРАВЛЕНИЯ БАННЕРОМ</size>", Color = HexToRustFormat("#3B5738FF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter},
                        new CuiRectTransformComponent { AnchorMin = "0 0.3", AnchorMax = "1 1" },
                        new CuiOutlineComponent { Color = HexToRustFormat("#3B5738FF"), Distance = "0.155 0.155" }
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = LayerMenu + ".Header",
                    Components =
                    {
                        new CuiTextComponent { Text = $"Статистика открытых баннеров: {playerBanner.Banners.Count} из {bannerDictionary.Count} доступных!", FontSize = 14, Color = HexToRustFormat("#3B5738f1"), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                        new CuiRectTransformComponent { AnchorMin = "0.03181809 0.0", AnchorMax = "0.9681819 0.47" },
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = LayerMenu,
                    Name = LayerMenu + ".Banner",
                    Components =
                    {
                        new CuiImageComponent { Color = /*"#A4A4A4FF")*/ "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = "0.01777265 0.3387191", AnchorMax = "0.9777274 0.823827" },
                    }
                });
            }

            double width = (float) 1 / playerBanner.Banners.Count;
            CuiHelper.DestroyUi(player, LayerMenu + ".Position");
            
            container.Add(new CuiElement
            {
                Parent = LayerMenu,
                Name = LayerMenu + ".Position",
                Components =
                {
                    new CuiRawImageComponent { Color = HexToRustFormat("#D6D6D6FF"), Sprite = "assets/content/ui/ui.background.tile.psd" },
                    new CuiRectTransformComponent { AnchorMin = $"{Math.Max(0, width * index)} 0.20", AnchorMax = $"{Math.Min(1, width * (index + 1))} 0.205" },
                }
            });

            CuiHelper.DestroyUi(player, LayerMenu + ".Banner.Sized");
            
            container.Add(new CuiElement
            {
                FadeOut = 0.3f,
                Parent = LayerMenu + ".Banner",
                Name = LayerMenu + ".Banner.Sized",
                Components =
                {
                    new CuiRawImageComponent { FadeIn = 0.3f, Png = (string) ImageLibrary.Call("GetImage", playerBanner.Banners[index]), Sprite = "assets/content/ui/ui.background.tile.psd" },
                    new CuiRectTransformComponent { AnchorMin = "0.004405677 0.01754272", AnchorMax = "0.9955943 0.9824579" }
                }
            });

            container.Add(new CuiElement
            {
                /* Отрисовываем аватарку пользователя */
                Parent = LayerMenu + ".Banner.Sized",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", player.UserIDString), Sprite = "assets/content/ui/ui.background.tile.psd" },
                    new CuiRectTransformComponent { AnchorMin = "0.01119532 0.05", AnchorMax = "0.223925 0.95" },
                    new CuiOutlineComponent { Distance = "2 2", Color = HexToRustFormat("#979797FF") }
                }
            });

            container.Add(new CuiElement
            {
                /* Отрисовываем ранг пользователя */
                Parent = LayerMenu + ".Banner.Sized",
                Components =
                {
                    new CuiTextComponent { Text = player.displayName, FontSize = 45, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0.2527497 0.3888876", AnchorMax = "0.9911312 0.8240748" },
                    new CuiOutlineComponent { Distance = "1 1", Color = HexToRustFormat("#343434FF") }
                }
            });

            container.Add(new CuiElement
            {
                /* Отрисовываем ранг пользователя */
                Parent = LayerMenu + ".Banner.Sized",
                Components =
                {
                    new CuiTextComponent { Text = $"<b>{GetTag(player)}</b>", FontSize = 24, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0.2560357 0.1287543", AnchorMax = "1 0.425052" },
                    new CuiOutlineComponent { Distance = "0.155 0.155", Color = HexToRustFormat("#343434FF") }
                }
            });

            CuiHelper.DestroyUi(player, LayerMenu + ".Banner.Left");
            CuiHelper.DestroyUi(player, LayerMenu + ".Banner.Middle");
            CuiHelper.DestroyUi(player, LayerMenu + ".Banner.Right");
            
            // Начинаем отрисовывать ебучие кнопки

            if (index - 1 >= 0)
            {
                container.Add(new CuiElement
                {
                    Parent = LayerMenu,
                    Name = LayerMenu + ".Banner.Left",
                    Components =
                    {
                        new CuiRawImageComponent { Color = HexToRustFormat("#D6D6D6FF"), Sprite = "assets/content/ui/ui.background.tile.psd" },
                        new CuiRectTransformComponent { AnchorMin = "0.3281818 0.225", AnchorMax = "0.3700000 0.324" },
                        new CuiOutlineComponent { Distance = "0 2", Color = HexToRustFormat("#565656FF") }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = LayerMenu + ".Banner.Left",
                    Components =
                    {
                        new CuiTextComponent { Text = "<", FontSize = 20, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter},
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                    }
                });
            
                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"UI_ChooseBanner change {index-1}" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = "" }
                }, LayerMenu + ".Banner.Left");
            }
            
            // Начинаем отрисовывать ебучие кнопки

            container.Add(new CuiElement
            {
                Parent = LayerMenu,
                Name = LayerMenu + ".Banner.Middle",
                Components =
                {
                    new CuiRawImageComponent { Color = HexToRustFormat("#b2b2b24A"), Sprite = "assets/content/ui/ui.background.tile.psd" },
                    new CuiRectTransformComponent { AnchorMin = "0.3754545 0.225", AnchorMax = "0.6245455 0.324" },
                   // new CuiOutlineComponent { Distance = "0 2", Color = HexToRustFormat("#565656FF") }
                }
            });

            string text = index == playerBanner.Banners.IndexOf(playerBanner.ActiveBanner) ? "ТЕКУЩИЙ" : "ВЫБРАТЬ";
            string command = text == "ВЫБРАТЬ" ? $"UI_ChooseBanner set {index}" : "";
            container.Add(new CuiElement
            {
                FadeOut = 0.4f,
                Parent = LayerMenu + ".Banner.Middle",
                Components =
                {
                    new CuiTextComponent { FadeIn = 0.4f, Text = text, FontSize = 20,  Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                }
            });
            
            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = command },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = "" }
            }, LayerMenu + ".Banner.Middle");
            
            // Начинаем отрисовывать ебучие кнопки
            
            if (index + 2 <= playerBanner.Banners.Count)
            {
                container.Add(new CuiElement
                {
                    Parent = LayerMenu,
                    Name = LayerMenu + ".Banner.Right",
                    Components =
                    {
                        new CuiRawImageComponent { Color = HexToRustFormat("#b2b2b24A"), Sprite = "assets/content/ui/ui.background.tile.psd" },
                        new CuiRectTransformComponent { AnchorMin = "0.6299996 0.225", AnchorMax = "0.671818 0.324" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = LayerMenu + ".Banner.Right",
                    Components =
                    {
                        new CuiTextComponent { Text = $">", FontSize = 20,  Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                    }
                });
            
                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"UI_ChooseBanner change {index+1}" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = "" }
                }, LayerMenu + ".Banner.Right");
            }
            

            CuiHelper.AddUi(player, container);
        }
        
        private void DrawGUI(BasePlayer target, BasePlayer player, string weapon, string distance)
        {
            CuiHelper.DestroyUi(target, LayerNew);
            
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                /* Главная панель */
                CursorEnabled = false,
                RectTransform = { AnchorMin = "0.004165918 0.7861201", AnchorMax = "0.004165918 0.7861201", OffsetMax = "613 150" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", LayerNew);

            container.Add(new CuiElement
            {
                /* Задний план */
                Parent = LayerNew,
                Name = LayerNew + ".Banner.Sized",
                Components =
                {
                    new CuiRawImageComponent { FadeIn = 5f, Png = (string) ImageLibrary.Call("GetImage", playerBanners[player.userID].ActiveBanner)},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });
            container.Add(new CuiElement
            {
                /* Отрисовываем аватарку пользователя */
                Parent = LayerNew + ".Banner.Sized",
                Components =
                {
                    new CuiRawImageComponent { FadeIn = 5f, Png = (string) ImageLibrary.Call("GetImage", player.UserIDString) },
                    new CuiRectTransformComponent { AnchorMin = "0.01119532 0.03703609", AnchorMax = "0.223925 0.9429639" },
                    new CuiOutlineComponent { Distance = "2 2", Color = HexToRustFormat("#979797FF") }
                }
            });

            container.Add(new CuiElement
            {
                /* Отрисовываем ранг пользователя */
                Parent = LayerNew + ".Banner.Sized",
                Components =
                {
                    new CuiTextComponent { FadeIn = 5f, Text = player.displayName, FontSize = 45, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0.2527497 0.3888876", AnchorMax = "0.9911312 0.8240748" },
                    new CuiOutlineComponent { Distance = "1 1", Color = HexToRustFormat("#343434FF") }
                }
            });

            container.Add(new CuiElement
            {
                /* Отрисовываем ранг пользователя */
                Parent = LayerNew + ".Banner.Sized",
                Components =
                {
                    new CuiTextComponent { FadeIn = 5f, Text = $"<b>{GetTag(player)}</b>", FontSize = 24, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0.2560357 0.1287543", AnchorMax = "1 0.425052" },
                    new CuiOutlineComponent { Distance = "0.155 0.155", Color = HexToRustFormat("#343434FF") }
                }
            });

            container.Add(new CuiElement
            {
                /* Отрисовываем ранг пользователя */
                Parent = LayerNew + ".Banner.Sized",
                Components =
                {
                    new CuiTextComponent { FadeIn = 5f, Text = $"ВЫ БЫЛИ УБИТЫ ЭТИМ ИГРОКОМ ИЗ ОРУЖИЯ {weapon.ToUpper()} ({distance} М.)", FontSize = 18, Align = TextAnchor.UpperCenter, Font = "robotocondensed-regular.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 -0.5", AnchorMax = "1 -0.05" },
                    //new CuiOutlineComponent { Distance = "0.155 0.155", Color = "#343434FF") }
                }
            });

            CuiHelper.AddUi(target, container);
        }

        private void DrawNewBanner(BasePlayer player, string name)
        {
            CuiHelper.DestroyUi(player, LayerNew);
            
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "0.3822916 0.8016625", AnchorMax = "0.6166667 0.9424033" },
                Image = { Color = "0 0 0 0" }
            }, "Hud", LayerNew);

    
            container.Add(new CuiElement
            {
                FadeOut = 1f,
                Parent = LayerNew,
                Name = LayerNew + ".Header",
                Components =
                {
                    new CuiImageComponent { Color = HexToRustFormat("#81B67AFF") },
                    new CuiRectTransformComponent { AnchorMin = "0 0.7226816", AnchorMax = "0.996 1.001001" }
                }
            });
                

            container.Add(new CuiElement
            {
                FadeOut = 1f,
                Parent = LayerNew + ".Header",
                Name = LayerNew + ".Header.Text",
                Components =
                {
                    new CuiTextComponent { Text = "<size=18>ДОСТУПЕН НОВЫЙ БАННЕР</size>", Color = HexToRustFormat("#373737FF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                    new CuiOutlineComponent { Color = HexToRustFormat("#373737FF"), Distance = "0.155 0.155" }
                }
            });
            

            container.Add(new CuiElement
            {
                FadeOut = 1f,
                Parent = LayerNew,
                Name = LayerNew + ".Png",
                Components =
                {
                    new CuiRawImageComponent { FadeIn = 1f, Png = (string) ImageLibrary.Call("GetImage", name) },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0.9955943 0.7038949" }
                }
            });

            CuiHelper.AddUi(player, container);

            timer.Once(5, () =>
            {
                CuiHelper.DestroyUi(player, LayerNew + ".Header");
                CuiHelper.DestroyUi(player, LayerNew + ".Header.Text");
                CuiHelper.DestroyUi(player, LayerNew + ".Png");
            });
            timer.Once(7, () => CuiHelper.DestroyUi(player, LayerNew));
        }

        #endregion

        #region Utils
/*
        private void TryGetBanners()
        {
            PrintWarning("Поиск новых баннеров:");
            
            int i = 0;
            string[] findFiles = Interface.Oxide.DataFileSystem.GetFiles("Banners/Banners");
            foreach (var check in findFiles)
            {
                string fullName = check.Split('\\')[check.Split('\\').Count() - 1].Replace(".png", "");
                string normalName = fullName.Split('+')[0];
                string urlName = fullName.Split('+')[1];
                if (!bannerDictionary.ContainsKey("ban." + normalName))
                {
                    bannerDictionary.Add("ban." + normalName, new Banner
                    {
                        ReasonAction = GiveAction.None,
                        Amount = "null",
                        
                        Description = "Баннер был только что добавлен, поэтому к нему ещё нету описания",
                        URL = "https://i.imgur.com/" + urlName + ".png"
                    }); 
                    i++;
                    
                    PrintWarning($"[{i}] Добавлено: ban.{normalName}");  
                }
            }
        }
*/
        #endregion
        
        public static string FormatTime(TimeSpan time, int maxSubstr = 5, string language = "ru")
        {
            string result = string.Empty;
            switch (language)
            {
                case "ru":
                    int i = 0;
                    if (time.Days != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";
                        
                        result += $"{Format(time.Days, "дней", "дня", "день")}";
                        i++;
                    }

                    if (time.Hours != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Hours, "часов", "часа", "час")}";
                        i++;
                    }

                    if (time.Minutes != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Minutes, "минут", "минуты", "минута")}";
                        i++;
                    }

                    if (time.Seconds != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Seconds, "секунд", "секунды", "секунда")}";
                        i++;
                    }

                        break;
                case "en":
                    result = string.Format( "{0}{1}{2}{3}",
                        time.Duration().Days > 0 ? $"{time.Days:0} day{( time.Days == 1 ? String.Empty : "s" )}, " : string.Empty,
                        time.Duration().Hours > 0 ? $"{time.Hours:0} hour{( time.Hours == 1 ? String.Empty : "s" )}, " : string.Empty,
                        time.Duration().Minutes > 0 ? $"{time.Minutes:0} minute{( time.Minutes == 1 ? String.Empty : "s" )}, " : string.Empty,
                        time.Duration().Seconds > 0 ? $"{time.Seconds:0} second{( time.Seconds == 1 ? String.Empty : "s" )}" : string.Empty );

                    if (result.EndsWith( ", " )) result = result.Substring( 0, result.Length - 2 );

                    if (string.IsNullOrEmpty( result )) result = "0 seconds";
                    break;
            }
            return result;
        }
        
        public static long TimeToSeconds(string time )
        {
            time = time.Replace( " ", "" ).Replace( "d", "d " ).Replace( "h", "h " ).Replace( "m", "m " ).Replace( "s", "s " ).TrimEnd( ' ' );
            var arr = time.Split( ' ' );
            long seconds = 0;
            foreach (var s in arr)
            {
                var n = s.Substring( s.Length - 1, 1 );
                var t = s.Remove( s.Length - 1, 1 );
                int d = int.Parse( t );
                switch (n)
                {
                    case "s":
                        seconds += d;
                        break;
                    case "m":
                        seconds += d * 60;
                        break;
                    case "h":
                        seconds += d * 3600;
                        break;
                    case "d":
                        seconds += d * 86400;
                        break;
                }
            }
            return seconds;
        }
        
        private static string Format(int units, string form1, string form2, string form3 )
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }

        
        private BasePlayer FindPlayer(string nameOrId)
        {
            foreach (var check in BasePlayer.activePlayerList)
            {
                if (check.displayName.ToLower().Contains(nameOrId.ToLower()) || check.userID.ToString() == nameOrId)
                    return check;
            }

            return null;
        }

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

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

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
    }
}