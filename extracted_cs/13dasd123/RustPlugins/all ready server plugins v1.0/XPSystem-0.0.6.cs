using Newtonsoft.Json.Linq;
using System;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Linq;
using Oxide.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using Oxide.Core.Configuration;
using VLB;
using System.Globalization;
using Oxide.Core.Libraries;
using UnityEngine;
		   		 		  						  	   		  		  		   					  	  			  			 
namespace Oxide.Plugins
{
    [Info("XPSystem", "Sempai#3239", "0.0.6")]
	[Description("Добавляет на сервер XP Систему, благодаря которой, игроки смогут получать баланс на магазин за фарм.")]
    class XPSystem : RustPlugin
    {
		
		        
        		
		bool LogsPlayer = true;

        protected override void LoadDefaultConfig()
        { 
			PrintWarning(
			                           "Благодарю за приобретение плагина от разработчика плагина: " +
			                           "mabe. Если будут какие-то вопросы, писать - vk.com/zaebokuser");
            Settings = Configuration.GetNewCong();
        } 

        void ProcessItem(BasePlayer player, Item item)
        {
            switch (item.info.shortname)
            {
                case "wood":
                    XPS[player.userID].XP += Settings.GathersSetting.WoodGathers;
                    return;
                    break;
                case "stones":
                    XPS[player.userID].XP += Settings.GathersSetting.StonesGathers;
                    return;
                    break;
                case "metal.ore":
                    XPS[player.userID].XP += Settings.GathersSetting.MetalOreGathers;
                    return;
                    break;
                case "sulfur.ore":
                    XPS[player.userID].XP += Settings.GathersSetting.SulfurGathers;
                    return;
                    break;
                case "hq.metal.ore":
                    XPS[player.userID].XP += Settings.GathersSetting.HQMGathers;
                    return;
                    break;
            }
        }
		
		private readonly Dictionary<string, int> playerInfo = new Dictionary<string, int>();

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null) { return; }
            if (info == null) { return; }
            if (info.InitiatorPlayer == null) { return; }
            if (entity is BaseAnimalNPC)
            {
                BasePlayer player = info.InitiatorPlayer;
                XPS[player.userID].XP += Settings.GatherSettings.AnimalGather;
                return;
            }
            if (entity.PrefabName.Contains("barrel"))
            {
                BasePlayer player = info?.InitiatorPlayer;
                XPS[player.userID].XP += Settings.GatherSettings.BarrelGather;
                return;
            }
        }
        private const int V = 100;
		   		 		  						  	   		  		  		   					  	  			  			 
		
               
	    private class Configuration
        {
			[JsonProperty("Настройка интерфейса")]
            public GUI GUISettings = new GUI();
            public class API
            {
				[JsonProperty("APIKey (Секретный ключ)")] 
				public string SecretKey = "";
				[JsonProperty("ShopID (ИД магазина в сервисе)")]
				public string ShopID = "";
			}

            public class PermToCommand
			{
				[JsonProperty("(true - да/false - нет)")]
				public bool PermCommand = true; 
			}

            [JsonProperty("Предметы доступные для покупки")]
            public List<ShopItem> ShopItems;
			
			public class Balance
			{
				[JsonProperty("Сколько рублей на баланс магазина будет выдаваться")]
				public int Money = 5; 
			}
            [JsonProperty("Настройка кол-ва выдачи баланса игроку")]
            public Balance BalanceSettings = new Balance();

            public class GUIShop
			{
                [JsonProperty("Название проекта")] 
				public string FirstText = "ВНУТРИИГРОВОЙ МАГАЗИН <color=#a5e664>SKYPLUGINS.RU</color>";		
			}
            [JsonProperty("Настройка интерфейса в магазине")]
            public GUIShop GUIShopSettings = new GUIShop();

            public class ShopItem
            {
                [JsonProperty("Название предмета")]
                public string ShortName;
                [JsonProperty("Количество предмета при покупке")]
                public int Amount;
                [JsonProperty("Цена покупки")]
                public int Price;
            }
            [JsonProperty("Добавить ли ВНУТРИИГРОВОЙ магазин?")]
            public PermToCommand PermToCommandSettings = new PermToCommand();
			[JsonProperty("Настройка кол-ва выдачи XP с фарма ресурсов")]
            public Gather GatherSettings = new Gather();
			
			public class Gather
			{
				[JsonProperty("Сколько игроку будет выдаваться XP за подбирание ресурсов")]
				public float CollectiblePickup = 0.15f; //Settings.GatherSettings.CollectiblePickup
				[JsonProperty("Сколько игроку будет выдаваться XP за подбирание плантации")]
				public float CropGather = 0.15f; //Settings.GatherSettings.CropGather
				[JsonProperty("Сколько игроку будет выдаваться XP за фарм бочек")]
				public float BarrelGather = 0.5f; //Settings.GatherSettings.BarrelGather
				[JsonProperty("Сколько игроку будет выдаваться XP за фарм ящиков")]
				public float CrateGather = 0.5f; //Settings.GatherSettings.CrateGather
                [JsonProperty("Сколько игроку будет выдаваться XP за убийство животного")]
				public float AnimalGather = 0.5f; //Settings.GatherSettings.AnimalGather
            }
		   		 		  						  	   		  		  		   					  	  			  			 
            public class GathersOptions
			{
				[JsonProperty("Сколько игроку будет выдаваться XP за фарм дерева")]
				public float WoodGathers = 0.5f; //Settings.GathersSetting.WoodGathers
                [JsonProperty("Сколько игроку будет выдаваться XP за фарм обычного камня stones")]
				public float StonesGathers = 0.5f; //Settings.GathersSetting.StonesGathers
                [JsonProperty("Сколько игроку будет выдаваться XP за фарм металлического камня")]
				public float MetalOreGathers = 0.5f; //Settings.GathersSetting.MetalOreGathers
                [JsonProperty("Сколько игроку будет выдаваться XP за фарм серного камня")]
				public float SulfurGathers = 0.5f; //Settings.GathersSetting.SulfurGathers
                [JsonProperty("Сколько игроку будет выдаваться XP за фарм бонус МВК")]
				public float HQMGathers = 0.5f; //Settings.GathersSetting.HQMGathers						
			}
			
			public class GUI
			{
				[JsonProperty("Разрешение для использования команды /xp")]
				public string XpPermission = "xpsystem.use";
                [JsonProperty("Курс обмена XP на баланс магазина")] 
				public string XPToBalance = "Курс обмена XP - <color=#a5e664>100XP = 5RUB</color>\nна баланс в магазине.";
                [JsonProperty("Текст в основном блоке (возле аватарки)")]
                public string MainBlockText = "<color=#a5e664><b>XP</b></color> можно заработать путём:\n\n<color=#a5e664><b>—</b></color> Подбирания ресурсов\n<color=#a5e664><b>—</b></color> Фарма руды и деревьев\n<color=#a5e664><b>—</b></color> Выращивания еды (плантации)\n<color=#a5e664><b>—</b></color> Фарма бочек и ящиков\n<color=#a5e664><b>—</b></color> Охоты на людей и животных";	
                [JsonProperty("Текст во втором нижнем блоке")]
				public string SecondBlockText = "Перед выводом <color=#a5e664><b>XP</b></color> убедитесь, что Вы авторизованы в нашем магазине <color=#a5e664>GAMESTORES.RU</color>";				
			}

            public static Configuration GetNewCong()
            {
                return new Configuration
                {
                    ShopItems = new List<ShopItem>
                    {
                        new ShopItem
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            Price = 50
                        },
                        new ShopItem
                        {
                            ShortName = "stones",
                            Amount = 5000,
                            Price = 20
                        },
                    }
                };
            }
			
			[JsonProperty("Настройки API плагина")]
            public API APISettings = new API();
            [JsonProperty("Более подробная настройка кол-ва выдачи XP с фарма ресурсов и животных")]
            public GathersOptions GathersSetting = new GathersOptions();
		}
		
		private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            SaveData();
        }
        
        private void OnPlayerConnected(BasePlayer player)
        {
            if (!XPS.ContainsKey(player.userID))
                XPS.Add(player.userID, new Xp { XP = 0 });
			SaveData();

            if (XPS[player.userID].XP >= 100)
                    {
                        SendReply(player,
							$"На вашем счету насчитано более <color=#a5e664>100 XP</color>. Обменяйте их по команде <color=#a5e664>/XP</color>, либо потратьте в <color=#a5e664>XP</color> МАГАЗИНЕ.");
                    } 		
				else
                    {
                    }
        }

        		
				
		private void DrawGUI(BasePlayer player)
		{	
            if (player == null) return;

			CuiHelper.DestroyUi(player, Layer);
			var container = new CuiElementContainer();		
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0.65", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "Overlay", Layer);
			//Надпись
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.1 0.54", AnchorMax = "0.998 0.998" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = $"ОБМЕН ВАЛЮТЫ", Color = HexToRustFormat("#FFFFFF"), Font = "robotocondensed-regular.ttf", FontSize = 32, Align = TextAnchor.MiddleCenter }
            }, Layer);
			//Курс обмена XP на рубли
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.1507811 -0.06944446", AnchorMax = "0.9542188 0.66083329" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = Settings.GUISettings.XPToBalance, Color = HexToRustFormat("#FFFFFF"), Font = "robotocondensed-regular.ttf", FontSize = 15, Align = TextAnchor.MiddleCenter }
            }, Layer);
            //Инфо-блок возле аватарки №1
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.45 0.47", AnchorMax = $"0.66 0.713" },
                Button = { Color = "0 0 0 0.4" }, 
                Text = { Text = Settings.GUISettings.MainBlockText, Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, FadeIn = 1f }
            }, Layer);
            //Инфо-блок ниже №2
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.45 0.33", AnchorMax = $"0.66 0.453" },
                Button = { Color = "0 0 0 0.4" }, 
                Text = { Text = Settings.GUISettings.SecondBlockText, Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, FadeIn = 1f }
            }, Layer);
            //Ваш баланс
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3 0.4", AnchorMax = $"0.445 0.453" },
                Button = { Color = "0 0 0 0.4" },
                Text = { Text = $"Ваш баланс: <color=#a5e664>{XPS[player.userID].XP}</color>", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, FadeIn = 1f }
            }, Layer);
            //Аватарка до 560 строчки
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3 0.48", AnchorMax = $"0.46 0.73" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, FadeIn = 1f }
            }, Layer, "Avatar");

            container.Add(new CuiElement
            {
                Parent = "Avatar",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", player.UserIDString) },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0.898 0.898", OffsetMax = "0 0" }
                }
            });
            //Приветствие
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.281 0.48", AnchorMax = "0.463 0.995" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = $"Здравствуй, <color=#a5e664><b>{player.displayName}</b></color>!", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf", Color = "0.91 0.91 0.91 1" }
            }, Layer);
            //Кнопки до 581 строчки
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3 0.33", AnchorMax = "0.375 0.385" },
                Button = { Color = "0 0 0 0.4", Command = "xpsell", Close = Layer },
                Text = { Text = "ОБМЕНЯТЬ", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf", Color = "0.91 0.91 0.91 1", FadeIn = 1f }
            }, Layer);
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.381 0.33", AnchorMax = "0.445 0.385" },
                Button = { Color = "0 0 0 0.4", Close = Layer },
                Text = { Text = "ЗАКРЫТЬ", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, FadeIn = 1f }
            }, Layer);

            //Кнопка Магазин
            if (Settings.PermToCommandSettings.PermCommand){
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3 0.260", AnchorMax = $"0.445 0.315" },
                Button = { Color = "0 0 0 0.4", Command = "xpshop", Close = Layer },
                Text = { Text = $"<color=#a5e664>XP</color> МАГАЗИН", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, FadeIn = 2f }
            }, Layer);
            }
			
			CuiHelper.AddUi(player, container);
		}
			
		private Dictionary<ulong, Xp> XPS { get; set; }

        		
		        
        object OnCollectiblePickup(Item item, BasePlayer player)
        {
            XPS[player.userID].XP += Settings.GatherSettings.CollectiblePickup;
			
            return null;
        }

        public ulong lastDamageName;
        private string LayerInfo = "UI_INFO";
		private string Layer = "UI_XPSYSTEM";
		private static Configuration Settings = new Configuration();
		
        [ConsoleCommand("xpshop")]
        private void CmdXpShop(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            DrawSHOP(player);
        }

        [ConsoleCommand("xp.give")]
        private void CmdXpgv(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();

                BasePlayer target = BasePlayer.Find(args.Args[0]);
                if (target != null)
                {
                    if (XPS.ContainsKey(target.userID))
                    {
                        XPS[target.userID].XP += int.Parse(args.Args[1]);
                        Puts("Баланс успешно изменен");
                    }
                }
            
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

            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

		[ConsoleCommand("xpsell")]
        private void CmdXpSell(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();

                if (XPS[player.userID].XP >= 100)
                    {
                        SendReply(player,
							$"Перевод <color=#a5e664>XP в рубли</color> успешно произведён!\nПроверьте свой баланс в магазине.\n<color=#a5e664>В случае не поступления средств сообщите администрации!</color>");
                        XPS[player.userID].XP -= 100;
                        MoneyPlus(player.userID, Settings.BalanceSettings.Money);
                        return;
                    } 
					
				else
                    {
                        SendReply(player,
                            $"<color=#a5e664>Обмен не произведён!</color>\nДля обмена нужно иметь <color=#a5e664>100 XP</color>!\n У вас <color=#a5e664>{XPS[player.userID].XP} XP</color>!");
                        return;
                    }
        }
		   		 		  						  	   		  		  		   					  	  			  			 
        void MoneyPlus(ulong userId, int amount) {
			ApiRequestBalance(new Dictionary<string, string>() {
				{"action", "moneys"},
				{"type", "plus"},
				{"steam_id", userId.ToString()},
				{"amount", amount.ToString()},
                { "mess", "Обмен XP на рубли! Спасибо, что играете у нас!"}
			});
		}
        
        object OnCropGather(GrowableEntity plant, Item item, BasePlayer player)
        {
            XPS[player.userID].XP += Settings.GatherSettings.CropGather;
			
            return null;
        }
        
        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            ProcessItem(player, item);
            return;
        }
		
        
        
        [ChatCommand("xp")]
        private void CommandChatXp(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, Settings.GUISettings.XpPermission))
            {
                SendReply(player, "У вас <color=#a5e664>недостаточно</color> прав для использования этой команды!");
                return; 
            }
		   		 		  						  	   		  		  		   					  	  			  			 
            DrawGUI(player);
        }
        private string Layer2 = "UI_XPSYSTEMSHOP";
        [PluginReference] Plugin ImageLibrary;
		
		private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
			SaveData();
        }

        [ConsoleCommand("buy_ui")]
        void cmdConsoleBuy1(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "buy")
                {
                    if (args.HasArgs(2))
                    {
                        var item = Settings.ShopItems.FirstOrDefault(p => p.ShortName == args.Args[1]);
                        if (Item != null)
                        {
                            if (XPS[player.userID].XP >= item.Price)
                            {
                               XPS[player.userID].XP -= item.Price;
                            }
                            else
                            {
                                SendReply(player, $"У вас недостаточно <color=#a5e664>XP</color>. На вашем балансе: <color=#a5e664>{XPS[player.userID].XP}</color>");
                                return;
                            }
                            SendReply(player, $"Вы успешно обменяли <color=#a5e664>XP</color> на предмет.\nНазвание: <color=#a5e664>{item.ShortName}</color>\nКоличество: <color=#a5e664>{item.Amount}</color>\nОсталось XP: <color=#a5e664>{XPS[player.userID].XP}</color>", item.ShortName, item.Amount);
                            player.inventory.GiveItem(ItemManager.CreateByName(item.ShortName, item.Amount));
                        }
                    }
                }
            }
        }
		
        		
				
		protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                Settings = Config.ReadObject<Configuration>();
                if (Settings?.APISettings == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка в конфиге... Создаю новую конфигурацию!");
                LoadDefaultConfig();
            }
            
            NextTick(SaveConfig);
        }
		private readonly List<uint> crateInfo = new List<uint>();

		void ApiRequestBalance(Dictionary<string, string> args) {
			string url =
				$"https://gamestores.ru/api?shop_id={Settings.APISettings.ShopID}&secret={Settings.APISettings.SecretKey}{string.Join("", args.Select(arg => $"&{arg.Key}={arg.Value}").ToArray())}";
			webrequest.EnqueueGet(url,
				(i, s) => {
					if (i != 200 && i != 201) {
						PrintError($"{url}\nCODE {i}: {s}");

						if (LogsPlayer) {
							LogToFile("logError", $"({DateTime.Now.ToShortTimeString()}): {url}\nCODE {i}: {s}", this);
						}
					} else {
						if (LogsPlayer) {
							LogToFile("logWEB",
								$"({DateTime.Now.ToShortTimeString()}): "
							+ "Пополнение счета:"
							+ $"{string.Join(" ", args.Select(arg => $"{arg.Value}").ToArray()).Replace("moneys", "").Replace("plus", "")}",
								this);
						}
					}

					if (i == 201) {
						PrintWarning("Плагин не работает!");
						Interface.Oxide.UnloadPlugin(Title);
					}
				},
				this);
		}
        
        protected override void SaveConfig() => Config.WriteObject(Settings);
		
		public class Xp
		{
			[JsonProperty("Количество XP у игрока:")]
			public float XP { get; set; }
		}

		private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("XPSystem/Database", XPS);
        }
		
        //GUI магазина
        private void DrawSHOP(BasePlayer player, int page = 1)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();
            float gap = -0.0f;
            float width = 0.11f;
            float height = 0.19f;
            float startxBox = 0.050f;
            float startyBox = 0.90f - height;
            float xmin = startxBox;
            float ymin = startyBox;
            int current = 0;

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.65", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "Overlay", Layer);
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "", Color = HexToRustFormat("#B4B4B480"), Font = "robotocondensed-regular.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter }
            }, Layer, ".Layer");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = HexToRustFormat("#FFFFFF00"), Close = Layer },
                Text = { Text = "", Color = HexToRustFormat("#B4B4B480"), Font = "robotocondensed-regular.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter }
            }, ".Layer");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.91", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = Settings.GUIShopSettings.FirstText, Color = HexToRustFormat("#FFFFFF"), Font = "robotocondensed-regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter }
            }, ".Layer");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.34 0.03", AnchorMax = "0.407 0.13", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.03", Command = $"UI_ChangePage {page - 1}" },
                Text = { Text = $"<", Color = HexToRustFormat("#FFFFFF"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 30 }
            }, ".Layer");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.593 0.03", AnchorMax = "0.66 0.13", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.03", Command = $"UI_ChangePage {page + 1}" },
                Text = { Text = $">", Color = HexToRustFormat("#FFFFFF"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 30 }
            }, ".Layer");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.41 0.03", AnchorMax = "0.59 0.13", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.03", Command = $"UI_ChangePage {page + 1}" },
                Text = { Text = $"СТРАНИЦА: {page}", Color = HexToRustFormat("#FFFFFF"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 15 }
            }, ".Layer");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.41 0.03", AnchorMax = "0.59 0.18", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0"},
                Text = { Text = $"НА ВАШЕМ БАЛАНСЕ: <color=#a5e664>{XPS[player.userID].XP} XP</color>", Color = HexToRustFormat("#FFFFFF"), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 14 }
            }, ".Layer");

            foreach (var check in Settings.ShopItems.Skip((page - 1) * 32).Take(32))
            {
                container.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = xmin + " " + ymin,
                        AnchorMax = (xmin + width) + " " + (ymin + height *1),
                        OffsetMax = "-1 -1",
                        OffsetMin = "5 5",
                    },
                    Button = { Color = "0 0 0 0", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", Command = $"buy_ui buy {check.ShortName}" },
                    Text = { Text = $"", Align = TextAnchor.LowerRight, Font = "robotocondensed-regular.ttf", FontSize = 12 }
                }, ".Layer", $".{check.ShortName}");
                xmin += width + gap;

                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height + gap;
                }

                container.Add(new CuiElement
                {
                    FadeOut = 0.3f,
                    Parent = $".{check.ShortName}",
                    Components =
                    {
                        new CuiRawImageComponent { FadeIn = 0.3f, Png = (string) ImageLibrary.Call("GetImage", check.ShortName)},
                        new CuiRectTransformComponent { AnchorMin = "0.18 0.18", AnchorMax = "0.9 0.9" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 0.3" },
                    Button = { Color = "0 0 0 0" },
                    Text = { Text = $"<b><color=#a5e664>{check.Price} XP</color></b>\nКоличество: <b><color=#a5e664>{check.Amount}</color></b>", Color = HexToRustFormat("#FFFFFF"), Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.LowerLeft }
                }, $".{check.ShortName}");

                current++;

                if (current > 32)
                {
                    break;
                }
            }

            CuiHelper.AddUi(player, container);
        }
		   		 		  						  	   		  		  		   					  	  			  			 
        [ConsoleCommand("UI_ChangePage")]
        private void CmdConsolePage(ConsoleSystem.Arg args)
        {
            string name = args.Args[0];
            BasePlayer player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                int page = 1;
                if (int.TryParse(args.Args[0], out page) && page > 0 && (page - 1) * 15 <= Settings.ShopItems.Count)
                {
                    DrawSHOP(player, page);
                }
                else if (page == -999)
                {
                    CuiHelper.DestroyUi(player, Layer);
                }
            }
        }
		
        
        
        private void OnServerInitialized()
        {
			PrintWarning("\n-----------------------------\n" +
            " Author - Sempai#3239\n" +
            " VK - https://vk.com/rustnastroika/n" +
            " Forum - https://whiteplugins.ru/n" +
            " Discord - https://discord.gg/5DPTsRmd3G/n" +
            "-----------------------------");			
		
			permission.RegisterPermission(Settings.GUISettings.XpPermission, this);
			
			PrintWarning(
			                           "Благодарю за приобретение плагина от разработчика плагина: " +
			                           "mabe. Если будут какие-то вопросы, писать - vk.com/zaebokuser");
									   
			if (Interface.Oxide.DataFileSystem.ExistsDatafile("XPSystem/Database"))
            { 
                XPS = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Xp>>("XPSystem/Database");
            }
            else
            {
                XPS = new Dictionary<ulong, Xp>();
            }   
			
            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);        
            SaveData();
        }

		private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (!entity.ShortPrefabName.Contains("crate_") && entity.ShortPrefabName != "heli_crate")
                return;

            if (crateInfo.Contains(entity.net.ID))
                return;

            crateInfo.Add(entity.net.ID);
			XPS[player.userID].XP += Settings.GatherSettings.CrateGather;
        }

            }
}