using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Welcome GUI", "FOX RUST by VAMPIR", "2.0.0")]

	class WelcomeGUI : RustPlugin
	{ 
		private bool backroundimage;
		private bool Changed;
		private string text;
		private bool displayoneveryconnect;
		private string backroundimageurl;
		
		void Loaded()  
		{
			permission.RegisterPermission("welcomegui.usecmd", this);
			data = Interface.GetMod().DataFileSystem.ReadObject<Data>("WelcomeGUIdata");
			LoadVariables();
		}
		
		object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;  
        } 
		
		void LoadVariables() 
		{
			backroundimageurl = Convert.ToString(GetConfig("Фон", "Изображение", "https://i.ytimg.com/vi/yaqe1qesQ8c/maxresdefault.jpg"));
			backroundimage = Convert.ToBoolean(GetConfig("Фон", "Включить изображение", false));
			displayoneveryconnect = Convert.ToBoolean(GetConfig("Настройки", "Отображать при каждом подключении", true));
			text = Convert.ToString(GetConfig("Сообщение", "Формат сообщения", new List<string>{
      "<size=23>ПРОЕКТ <color=#DC143C>FOX RUST</color> приветствует тебя, <color=#FFFF00>{name}</color>!</size>",
      "На сервере разрешено играть не более 3 человек!",
      "— — — — — — — — — — — — — — — — — — — — — — — — — —— — — — — — — — — — — — — — — — — — — — — — — — — — — —",
      "<color=#00FF7F>Команды сервера</color> <color=#FF4500>/kit/home/tp/skill/bonus/report/friend add/store/ad/gr/dr/xp/grib</color>",
      "— — — — — — — — — — — — — — — — — — — — — — — — — —— — — — — — — — — — — — — — — — — — — — — — — — — — — —",
      "          <color=#00BFFF>Группа ВКонтакте</color> - <color=#DC143C>VK.COM/FOXRUST133</color>",
      "— — — — — — — — — — — — — — — — — — — — — — — — — —— — — — — — — — — — — — — — — — — — — — — — — — — — — —",
      "        <color=#00BFFF>Донат услуги </color>- <color=#DC143C>DARKMOON1.GAMESTORES.RU<</color>",
      "— — — — — — — — — — — — — — — — — — — — — — — — — —— — — — — — — — — — — — — — — — — — — — — — — — — — — —",
      "<color=#DC143C>ОЗНАКОМТЕСЬ С ПРАВИЛАМИ ИГРЫ </color>-<color=#1E90FF>https://vk.com/topic-128987145_39324130</color>",
      "— — — — — — — — — — — — — — — — — — — — — — — — — —— — — — — — — — — — — — — — — — — — — — — — — — — —— — —",
      "",
      "",
      "↓↓<color=#FFFF00>ЕСЛИ ТЫ ЧЕСТНЫЙ ИГРОК И ПРОЧИТАЛ ПРАВИЛА СЕРВЕРА ЖМИ КНОПКУ</color>↓↓"

			}));
			
			if (Changed)
			{
				SaveConfig();
				Changed = false;
			
			}	
		}
		
		protected override void LoadDefaultConfig()
		{
			Puts("Создание нового файла конфигурации! Доработка от RustPlugin.ru!");
			Config.Clear();
			LoadVariables();
		}

		class Data
		{
			public List<string> Players = new List<string>{};
		}


		Data data;

		void Unloaded()
		{
			foreach (BasePlayer current in BasePlayer.activePlayerList)
			{
				CuiHelper.DestroyUi(current, "WelcomeGUI");
			}
		}
		
		void UseUI(BasePlayer player, string msg)
		{ 
			var elements = new CuiElementContainer();

			var mainName = elements.Add(new CuiPanel
			{
				Image =
				{
					Color = "0.1 0.1 0.1 0.9"
				},
				RectTransform =
				{
					AnchorMin = "0 0",
					AnchorMax = "1 1"
				},
				CursorEnabled = true
			}, "Overlay", "WelcomeGUI"); 
			if(backroundimage == true)
			{
				elements.Add(new CuiElement
				{  
					Parent = "WelcomeGUI",
					Components =
					{
						new CuiRawImageComponent
						{
							Url = backroundimageurl,
							Sprite = "assets/content/textures/generic/fulltransparent.tga"
						}, 
						new CuiRectTransformComponent
						{
							AnchorMin = "0 0",
							AnchorMax = "1 1"
						}
					}
				});
			}				 
			var Agree = new CuiButton
            {
                Button =
                {
                    Close = mainName,
                    Color = "0.48 0.48 0.48 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.30 0.16",
					AnchorMax = "0.685 0.20"
                },
                Text =
                {
                    Text = "<color=#00FF7F>СОГЛАСЕН С ПРАВИЛАМИ</color>",
                    FontSize = 19,
                    Align = TextAnchor.MiddleCenter
                }
            };
			elements.Add(new CuiLabel
			{
				Text =
                {
					Text = msg.Replace("{name}", player.displayName), 
                    FontSize = 17,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0 0.12",
                    AnchorMax = "1 0.9"
                }
			}, mainName);
			elements.Add(Agree, mainName);
			CuiHelper.AddUi(player, elements);
		}

		[ChatCommand("info")]
		void cmdRulesTo(BasePlayer player, string cmd, string[] args)
		{
			if(!permission.UserHasPermission(player.userID.ToString(), "welcomegui.usecmd"))
			{
				SendReply(player, "У вас нет прав использовать эту команду!");
				return;
			}
			if(args.Length != 1)
			{
				SendReply(player, "Используйте: /welcometo \"target\" ");
				return;
			}
			BasePlayer target = BasePlayer.Find(args[0]);
			if(target == null)
			{
				SendReply(player, "Игрок не найден!");
				return;
			}
			string msg = "";
			foreach(var welcome in Config["Сообщение", "Формат сообщения"] as List<object>)
			msg = msg + welcome.ToString() + "\n";
			UseUI(target, msg.ToString());
			SendReply(player, "Отображается следующая информация <color=orange> " + target.displayName + "</color>");
			
		}		
			
		[ChatCommand("welcome")]
		void cmdRule(BasePlayer player, string cmd, string[] args)
		{
			string msg = "";
			foreach(var welcome in Config["Сообщение", "Формат сообщения"] as List<object>)
			msg = msg + welcome.ToString() + "\n";
			UseUI(player, msg.ToString());
		}

		void DisplayUI(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(1, () => DisplayUI(player));
            }
            else 
			{
				string steamId = Convert.ToString(player.userID);
				if(displayoneveryconnect == true)
				{
					string msg = "";
					foreach(var welcome in Config["Сообщение", "Формат сообщения"] as List<object>)
					msg = msg + welcome.ToString() + "\n";
					UseUI(player, msg.ToString());
				}
				else 
				{			
					if(data.Players.Contains(steamId)) return;
					string msg = "";
					foreach(var welcome in Config["Сообщение", "Формат сообщения"] as List<object>)
					msg = msg + welcome.ToString() + "\n";
					UseUI(player, msg.ToString());
					data.Players.Add(steamId);	
					Interface.GetMod().DataFileSystem.WriteObject("WelcomeGUIdata", data);
				}
            }
        }
		
		
		void OnPlayerInit(BasePlayer player)		
		{
			DisplayUI(player);		
		}
	}
}