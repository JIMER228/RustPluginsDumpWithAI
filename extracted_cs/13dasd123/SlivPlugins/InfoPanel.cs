// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using WebSocketSharp;

namespace Oxide.Plugins
{
	[Info("InfoPanel", "Sempai#3239", "1.0.1")]
	public class InfoPanel : RustPlugin
	{
		#region Fields

		[PluginReference] private Plugin ImageLibrary, WLReward;

		private const string Layer = "UI.Menu";

		#endregion

		#region Config

		private Configuration _config;

		private class Configuration
		{
			[JsonProperty(PropertyName = "Title")] public string Title = "ДОБРО ПОЖАЛОВАТЬ НА WELOVE RUST MAIN";

			[JsonProperty(PropertyName = "Logo Image")]
			public string Logo = "https://i.imgur.com/KjkhHrm.png";

			[JsonProperty(PropertyName = "Background Image")]
			public string Background = "https://i.imgur.com/Zfvyp1Z.png";

			[JsonProperty(PropertyName = "Notify Image")]
			public string Notify = "https://i.imgur.com/ZvBufFr.png";

			[JsonProperty(PropertyName = "Buttons", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<Button> Buttons = new List<Button>
			{
				new Button
				{
					Title = "НАБОРЫ",
					Command = "chat.say /kits",
					Image = string.Empty
				},
				new Button
				{
					Title = "КРАФТЫ",
					Command = "chat.say /craft",
					Image = string.Empty
				},
				new Button
				{
					Title = "BATTLEPASS",
					Command = "chat.say /pass",
					Image = string.Empty
				},
				new Button
				{
					Title = "ВАЙП БЛОК",
					Command = "chat.say /block",
					Image = string.Empty
				},
				new Button
				{
					Title = "ЖАЛОБЫ",
					Command = "chat.say /report",
					Image = string.Empty
				}
			};

			[JsonProperty(PropertyName = "Mini Buttons", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<Button> MiniButtons = new List<Button>
			{
				new Button
				{
					Title = string.Empty,
					Command = "chat.say /store",
					Image = "https://i.imgur.com/s84DMbk.png"
				},
				new Button
				{
					Title = string.Empty,
					Command = "chat.say /info",
					Image = "https://i.imgur.com/ywUyI9N.png"
				}
			};
		}

		private class Button
		{
			[JsonProperty(PropertyName = "Title")] public string Title;

			[JsonProperty(PropertyName = "Image (for mini buttons)")]
			public string Image;

			[JsonProperty(PropertyName = "Command")]
			public string Command;
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

		private PluginData _data;

		private void SaveData()
		{
			Interface.Oxide.DataFileSystem.WriteObject(Name, _data);
		}

		private void LoadData()
		{
			try
			{
				_data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name);
			}
			catch (Exception e)
			{
				PrintError(e.ToString());
			}

			if (_data == null) _data = new PluginData();
		}

		private class PluginData
		{
			[JsonProperty(PropertyName = "Hided Players", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<ulong> HidedPlayers = new List<ulong>();

			public bool IsHided(BasePlayer player)
			{
				return HidedPlayers.Contains(player.userID);
			}

			public bool ChangeStatus(BasePlayer player)
			{
				if (player == null) return false;

				if (IsHided(player))
				{
					HidedPlayers.Remove(player.userID);
					return false;
				}

				HidedPlayers.Add(player.userID);
				return true;
			}
		}

		#endregion

		#region Hooks

		private void OnServerInitialized()
		{
			PrintWarning("\n-----------------------------\n " +" Author - Sempai#3239\n " +" \n " +" Forum - https://topplugin.ru/\n " +" Discord - https://discord.gg/5DPTsRmd3G\n" +"-----------------------------");  
			LoadData();

			if (!ImageLibrary)
			{
				PrintWarning("IMAGE LIBRARY IS NOT INSTALLED.");
			}
			else
			{
				ImageLibrary.Call("AddImage", _config.Background, _config.Background);
				ImageLibrary.Call("AddImage", _config.Logo, _config.Logo);
				ImageLibrary.Call("AddImage", _config.Notify, _config.Notify);

				_config.MiniButtons.ForEach(btn =>
				{
					if (!btn.Image.IsNullOrEmpty())
						ImageLibrary.Call("AddImage", btn.Image, btn.Image);
				});
			}

			foreach (var player in BasePlayer.activePlayerList)
				OnPlayerConnected(player);

			AddCovalenceCommand("hide", nameof(CmdMenuHide));
		}

		private void Unload()
		{
			foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, Layer);

			SaveData();
		}

		private void OnPlayerConnected(BasePlayer player)
		{
			if (player == null) return;

			MainUi(player);

			RefreshOnline();
		}

		private void OnPlayerDisconnected(BasePlayer player)
		{
			timer.In(0.21f, RefreshOnline);
		}

		#endregion

		#region Commands

		private void CmdMenuHide(IPlayer cov, string command, string[] args)
		{
			var player = cov?.Object as BasePlayer;
			if (player == null) return;

			_data.ChangeStatus(player);

			MainUi(player);
		}

		#endregion

		#region Interface

		private void MainUi(BasePlayer player)
		{
			var container = new CuiElementContainer();

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1"},
				Image = {Color = "0 0 0 0"}
			}, "Overlay", Layer);

			#region Main

			if (!_data.IsHided(player))
			{
				container.Add(new CuiElement
				{
					Name = Layer + ".Main",
					Parent = Layer,
					Components =
					{
						new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", _config.Background)},
						new CuiRectTransformComponent
						{
							AnchorMin = "0 0", AnchorMax = "1 1",
							OffsetMin = "40.75 -64", OffsetMax = "435 -9.5"
						}
					}
				});

				#region Line

				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "0 0",
						OffsetMin = "32 5",
						OffsetMax = "34 47"
					},
					Image = {Color = "1 1 1 1"}
				}, Layer + ".Main");

				#endregion

				#region Title

				container.Add(new CuiLabel
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "0 0",
						OffsetMin = "37.5 35",
						OffsetMax = "250 50"
					},
					Text =
					{
						Text = $"{_config.Title}",
						Align = TextAnchor.LowerLeft,
						Font = "robotocondensed-bold.ttf",
						FontSize = 12,
						Color = "1 1 1 1"
					}
				}, Layer + ".Main");

				#endregion

				#region Online

				container.Add(new CuiLabel
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "0 0",
						OffsetMin = "37.5 20",
						OffsetMax = "150 35"
					},
					Text =
					{
						Text = $"",
						Align = TextAnchor.UpperLeft,
						Font = "robotocondensed-bold.ttf",
						FontSize = 10,
						Color = "1 1 1 0.99"
					}
				}, Layer + ".Main", Layer + ".Online");

				#endregion

				#region Buttons

				var xSwitch = 37.5f;
				var Width = 47f;
				var Margin = 7f;

				for (var i = 0; i < _config.Buttons.Count; i++)
				{
					var button = _config.Buttons[i];

					container.Add(new CuiButton
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "0 0",
							OffsetMin = $"{xSwitch} 5",
							OffsetMax = $"{xSwitch + Width} 23"
						},
						Text =
						{
							Text = $"{button.Title}",
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-bold.ttf",
							FontSize = 10,
							Color = "1 1 1 0.95"
						},
						Button =
						{
							Command = $"{button.Command}",
							Color = "0 0 0 0.60"
						}
					}, Layer + ".Main", Layer + $".BTN.{i}");

					if (i != _config.Buttons.Count - 1)
						container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = "1 0", AnchorMax = "1 1",
								OffsetMin = "2.75 0",
								OffsetMax = "4.25 0"
							},
							Image = {Color = "1 1 1 1"}
						}, Layer + $".BTN.{i}");


					xSwitch += Width + Margin;
				}

				#endregion

				#region Mini Buttons

				xSwitch -= Margin;
				Width = 18f;
				Margin = 5f;

				for (var i = 0; i < _config.MiniButtons.Count; i++)
				{
					var button = _config.MiniButtons[i];

					container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "0 0",
							OffsetMin = $"{xSwitch - Width} 30",
							OffsetMax = $"{xSwitch} 48"
						},
						Image = {Color = "0 0 0 0.45"}
					}, Layer + ".Main", Layer + $".MiniButtons.{i}");

					if (!button.Image.IsNullOrEmpty())
						container.Add(new CuiElement
						{
							Parent = Layer + $".MiniButtons.{i}",
							Components =
							{
								new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", button.Image)},
								new CuiRectTransformComponent
								{
									AnchorMin = "0 0", AnchorMax = "1 1",
									OffsetMin = "2.5 2.5", OffsetMax = "-2.5 -2.5"
								}
							}
						});

					container.Add(new CuiButton
					{
						RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
						Text = {Text = ""},
						Button =
						{
							Color = "0 0 0 0",
							Command = $"{button.Command}"
						}
					}, Layer + $".MiniButtons.{i}");

					xSwitch = xSwitch - Width - Margin;
				}

				#endregion
			}

			#endregion

			#region Logo

			container.Add(new CuiElement
			{
				Name = Layer + ".Logo",
				Parent = Layer,
				Components =
				{
					new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", _config.Logo)},
					new CuiRectTransformComponent
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin = "10 -65", OffsetMax = "70 -10"
					}
				}
			});

			container.Add(new CuiButton
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Text = {Text = ""},
				Button =
				{
					Color = "0 0 0 0",
					Command = "hide"
				}
			}, Layer + ".Logo");

			#endregion

			CuiHelper.DestroyUi(player, Layer);
			CuiHelper.AddUi(player, container);

			ShowGeozarUi(player);
		}

		private void RefreshOnline()
		{
			var container = new CuiElementContainer
			{
				{
					new CuiLabel
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "37.5 20", OffsetMax = "150 35"
						},
						Text =
						{
							Text = $"",
							Align = TextAnchor.UpperLeft,
							Font = "robotocondensed-bold.ttf",
							FontSize = 10,
							Color = "1 1 1 0.99"
						}
					},
					Layer + ".Main", Layer + ".Online"
				}
			};

			foreach (var player in BasePlayer.activePlayerList)
			{
				if (_data.IsHided(player)) continue;

				CuiHelper.DestroyUi(player, Layer + ".Online");
				CuiHelper.AddUi(player, container);
			}
		}

		#endregion

		#region WLReward

		/*
		private void OnPlayerGeoRecieved(BasePlayer player)
		{
			if (player == null) return;

			CuiHelper.DestroyUi(player, Layer + ".WLReward");
		}
		*/
		
		private bool HasReward(BasePlayer player)
		{
			var result = WLReward?.Call("CanGet", player);
			if (result != null)
			return (bool)result;				
			return  false;
		}

		private void ShowGeozarUi(BasePlayer player)
		{
			var container = new CuiElementContainer();
			
			if (HasReward(player))
			{
				container.Add(new CuiElement()
				{
					Name = Layer + ".WLReward.IMG",
					Parent = Layer,
					Components =
					{
						new CuiRawImageComponent() {Png = ImageLibrary.Call<string>("GetImage", _config.Notify)},
						new CuiRectTransformComponent()
						{
							AnchorMin = "0 0", AnchorMax = "1 1",
							OffsetMin = "55 -22.5", OffsetMax = "70 -7.5"
						}
					}
				});

				if (!_data.IsHided(player))
				{
					container.Add(new CuiButton()
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = "0 0",
							OffsetMin = "235 30",
							OffsetMax = "296 48"
						},
						Text =
						{
							Text = "ЗАБРАТЬ БОНУС",
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-bold.ttf",
							FontSize = 8,
							Color = "1 1 1 1"
						},
						Button =
						{
							Color = "0.86 0.41 0.41 1",
							Command = "takegeo"
						}
					}, Layer + ".Main", Layer + ".WLReward.BTN");
				}
			}

			CuiHelper.DestroyUi(player, Layer + ".WLReward.IMG");
			CuiHelper.DestroyUi(player, Layer + ".WLReward.BTN");
			CuiHelper.AddUi(player, container);
		}
		
		#endregion
	}
}