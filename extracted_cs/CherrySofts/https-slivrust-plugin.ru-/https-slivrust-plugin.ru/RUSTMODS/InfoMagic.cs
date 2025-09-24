// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using UnityEngine.UI;

namespace Oxide.Plugins {
	[Info("InfoMagic", "rustmods.ru", "0.1.0")]
	public class InfoMagic : RustPlugin {

		[PluginReference] private Plugin ImageLibrary;

		#region Cfg
		public class DataConfig
		{
			[JsonProperty("Номер сервера")]
			public string numberservers;

			[JsonProperty("Команда для первой кнопки")]
			public string _command1;  

			[JsonProperty("Команда для второй кнопки")]
			public string _command2; 
			
			[JsonProperty("Картинка для первой кнопки")]
			public string _img1; 
			
			[JsonProperty("Картинка для второй кнопки")]
			public string _img2; 
			
		}
		#endregion
		
		#region Cfg2
		public DataConfig cfg;
		protected override void LoadConfig()
		{
			base.LoadConfig();
			cfg = Config.ReadObject<DataConfig>();
		}

		protected override void SaveConfig()
		{
			Config.WriteObject(cfg);
		}

		protected override void LoadDefaultConfig()
		{
			cfg = new DataConfig()
			{
				numberservers = "1",
				_command1     = "/store",
				_command2     = "/menu",
				_img1         = "https://cdn.discordapp.com/attachments/626472120519491607/777167052959776798/-2.png",
				_img2         = "https://cdn.discordapp.com/attachments/626472120519491607/777164063892439040/-1.png"
			};
		}
		#endregion

		void OnServerInitialized(bool initial)
		{
			#region ImageLibrary
			if (ImageLibrary == false)
			{
				PrintError("The plugin won't work if you don't download ImageLibrary");
				PrintWarning("https://umod.org/plugins/image-library");
				Interface.Oxide.UnloadPlugin(Title);
				return;
			}
			else 
			{
				ImageLibrary.Call("AddImage", cfg._img1, "storeimg");
				ImageLibrary.Call("AddImage", cfg._img2, "menuimg");
			}
			#endregion
			
			
			Puts($"-----------Plugin: {Title}------------");
			Puts($"-----------Author: {Author}-------------");
			Puts($"-----------Version: {Version}---------------");
			Puts("-------------OxideRussia----------------");
		}
		
		private void OnPlayerConnected(BasePlayer player)
		{
			string              Layer     = "FirstPage";
			bool                textdes   = false;
			CuiElementContainer Container = new CuiElementContainer();
			
			Container.Add(new CuiPanel
			{
				CursorEnabled = false,
				RectTransform = { AnchorMin = "0 0.9435185", AnchorMax = "0.1140625 0.9981483", OffsetMax = "0 0" },
				Image         = { Color     = "0 0 0 0" },
			}, "Overlay", Layer);
			
			Container.Add(new CuiElement
			{
				Name   = "Block1",
				Parent = Layer,
				Components =
				{
					new CuiImageComponent { Color             = "1.00 1.00 1.00 0.20" },
					new CuiRectTransformComponent { AnchorMin = "0 0.4067788", AnchorMax = "0.1674419 1.054543", OffsetMax = "0 0"}
				}
			});
			
			Container.Add(new CuiButton
			{
				RectTransform = { AnchorMin = "0.1767444 0.399999",AnchorMax = "0.344186 1.054543", OffsetMax = "0 0"},
				Button        = {Command    = $"chat.say {cfg._command1}", Color = "1.00 1.00 1.00 0.22" },
				Text          = { Text      = "" }
			}, Layer, "Button1");
			
			Container.Add(new CuiElement
			{
				Parent = "Button1",
				Components =
				{
					new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "storeimg") },
					new CuiRectTransformComponent { AnchorMin = "0.1388878 0.1666653",AnchorMax = "0.8055556 0.8055552", OffsetMax = "0 0"}
				}
			});
			
			Container.Add(new CuiButton
			{
				RectTransform = { AnchorMin = "0.3534886 0.399999",AnchorMax = "0.5162792 1.054543", OffsetMax = "0 0"},
				Button        = {Command    = $"chat.say {cfg._command2}", Color       = "1.00 1.00 1.00 0.22" },
				Text          = { Text      = "" }
			}, Layer, "Button2");
			
			Container.Add(new CuiElement
			{
				Parent = "Button2",
				Components =
				{
					new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "menuimg") },
					new CuiRectTransformComponent { AnchorMin = "0.142858 0.1666671",AnchorMax = "0.857142 0.8055581", OffsetMax = "0 0"}
				}
			});

			Container.Add(new CuiLabel
			{
				Text = {Text = cfg.numberservers, FontSize = 17, Font = "RobotoCondensed-Bold.ttf", Align = TextAnchor.MiddleCenter}
			},"Block1");
			
			CuiHelper.AddUi(player, Container);
		}

		[ChatCommand("delete64246427742sgfg")]
		private void DestroyCommand(BasePlayer player) 
		{
			CuiHelper.DestroyUi(player, "FirstPage");
		}
		
	}
}