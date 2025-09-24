using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
	/*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("Referal System", "https://discord.gg/dNGbxafuJn", "0.1.3")]
	public class ReferalSystem : RustPlugin
	{
		#region Fields

		[PluginReference] private Plugin ImageLibrary;
		
		private static ReferalSystem _;
		private static Configuration Settings = Configuration.Generate();
		private static Hash<ulong, RefProfile> RefProfiles = new Hash<ulong, RefProfile>();

		#endregion

		#region Classes

		private class PlayerInfo
		{
			[JsonProperty("Отображаемое имя")] public string DisplayName;
			[JsonProperty("ID человека")] public ulong UserID;

			public PlayerInfo()
			{
			}

			public PlayerInfo(BasePlayer player)
			{
				DisplayName = player.displayName;
				UserID = player.userID;
			}
		}

		private class RefProfile
		{
			[JsonProperty("Информация об игроке")] public PlayerInfo PlayerInfo;

			[JsonProperty("Информация о пригласившем")]
			public PlayerInfo InviterInfo;

			[JsonProperty("Информация о приглашённых игроках")]
			public List<PlayerInfo> InvitedInfos;

			[JsonProperty("Получена ли награда")] public bool Collected;

			public RefProfile()
			{
			}

			public RefProfile(BasePlayer player)
			{
				PlayerInfo = new PlayerInfo(player);
				InviterInfo = null;
				InvitedInfos = new List<PlayerInfo>();
			}

			public RefProfile GetProfileByInfo(PlayerInfo info)
			{
				if (info == null) return null;

				var profile = RefProfiles.FirstOrDefault(p => p.Key == info.UserID).Value;
				return profile;
			}

			public RefProfile GetInviter()
			{
				if (InviterInfo == null) return null;

				return GetProfileByInfo(InviterInfo);
			}

			public List<RefProfile> GetInvited()
			{
				var output = new List<RefProfile>();
				foreach (var check in InvitedInfos)
				{
					var profile = GetProfileByInfo(check);
					if (profile == null) continue;

					output.Add(profile);
				}

				return output;
			}
		}

		private class RefReward
		{
			public class RefItem
			{
				[JsonProperty("Отображаемое имя предмета")]
				public string DisplayName;

				[JsonProperty("Короткое название предмета")]
				public string ShortName;

				[JsonProperty("Количество предмета")] public int Amount;

				[JsonProperty("SkinID предмета")] public ulong SkinID;
				[JsonProperty("Вызываемая команда")] public string ExecuteCommand;

				public void ProcessReward(BasePlayer player)
				{
					if (!string.IsNullOrEmpty(ShortName))
					{
						var item = ItemManager.CreateByPartialName(ShortName, Amount);
						item.skin = SkinID;

						if (!item.MoveToContainer(player.inventory.containerMain))
							item.Drop(player.transform.position, Vector3.down);
					}

					if (!string.IsNullOrEmpty(ExecuteCommand))
						_.Server.Command(ExecuteCommand.Replace("%STEAMID%", player.UserIDString));
				}
			}

			[JsonProperty("Предметы получаемые с этого набора")]
			public List<RefItem> RefItems = new List<RefItem>();

			public void ProcessRewards(BasePlayer player)
			{
				player.ChatMessage(
					"Вы получили <color=orange>награду</color>, спасибо что приглашаете игроков на сервер!");
				for (var i = 0; i < Settings.PrizeRandomAmount; i++) RefItems.GetRandom().ProcessReward(player);
			}
		}

		private class Configuration
		{
			[JsonProperty("Максимальное количество приглащенных игроков")]
			public int MaxInvitedAmount = 5;

			[JsonProperty("Количество случайных предметов за одного игрока")]
			public int PrizeRandomAmount = 3;

			[JsonProperty("Список возможных подарков для пригласившего")]
			public RefReward RewardsForInviter;

			[JsonProperty("Список возможных подарков для приглашенного")]
			public RefReward RewardsForInvited;

			public static Configuration Generate()
			{
				return new Configuration
				{
					MaxInvitedAmount = 8,
					RewardsForInviter = new RefReward
					{
						RefItems = new List<RefReward.RefItem>
						{
							new RefReward.RefItem
							{
								ShortName = "rifle.ak",
								Amount = 1,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							}
						}
					},
					RewardsForInvited = new RefReward
					{
						RefItems = new List<RefReward.RefItem>
						{
							new RefReward.RefItem
							{
								ShortName = "rifle.ak",
								Amount = 1,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							},
							new RefReward.RefItem
							{
								ShortName = "ammo.rifle",
								Amount = 150,
								SkinID = 1
							}
						}
					}
				};
			}
		}

		#endregion

		#region Initialization

		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				Settings = Config.ReadObject<Configuration>();
			}
			catch
			{
				PrintError("An error occurred reading the configuration file!");
				PrintError("Check it with any JSON Validator!");
				return;
			}

			SaveConfig();
		}

		protected override void LoadDefaultConfig()
		{
			Settings = Configuration.Generate();
		}

		protected override void SaveConfig()
		{
			Config.WriteObject(Settings);
		}

		private void OnServerInitialized()
		{
			_ = this;

			if (Interface.Oxide.DataFileSystem.ExistsDatafile(Name))
				RefProfiles = Interface.Oxide.DataFileSystem.ReadObject<Hash<ulong, RefProfile>>(Name);

			LoadImages();

			foreach (var player in BasePlayer.activePlayerList) OnPlayerConnected(player);
		}

		private void OnPlayerConnected(BasePlayer player)
		{
			if (!RefProfiles.ContainsKey(player.userID))
				RefProfiles.Add(player.userID, new RefProfile(player));
		}

		private void SaveData()
		{
			Interface.Oxide.DataFileSystem.WriteObject(Name, RefProfiles);
		}

		private void Unload()
		{
			SaveData();

			foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, Layer + ".Owner");

			_ = null;
			Settings = null;
			RefProfiles = null;
		}

		#endregion

		#region Commands

		[ChatCommand("ref")]
		private void CmdChatRef(BasePlayer player, string command, string[] args)
		{
			UI_DrawInterface(player);
		}

		[ConsoleCommand("RS_Controller")]
		private void CmdConsoleController(ConsoleSystem.Arg args)
		{
			var player = args.Player();
			if (player == null || !args.HasArgs()) return;

			switch (args.Args[0].ToLower())
			{
				case "show":
				{
					var container = new CuiElementContainer();

					container.Add(new CuiLabel
					{
						RectTransform =
							{AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "-250 -50", OffsetMax = "0 50"},
						Text =
						{
							FadeIn = 1f, Text = "для пригласившего", Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-regular.ttf", FontSize = 18, Color = "1 1 1 0.7"
						}
					}, Layer + ".Head");

					container.Add(new CuiLabel
					{
						RectTransform =
							{AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 -50", OffsetMax = "250 50"},
						Text =
						{
							FadeIn = 1f, Text = "для приглашённого", Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-regular.ttf", FontSize = 18, Color = "1 1 1 0.7"
						}
					}, Layer + ".Head");

					container.Add(new CuiLabel
					{
						RectTransform =
							{AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "-250 -375", OffsetMax = "250 -325"},
						Text =
						{
							FadeIn = 1f, Text = $"Вы получите {Settings.PrizeRandomAmount} случайных предмета",
							Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 18,
							Color = "1 1 1 0.7"
						}
					}, Layer + ".Head");

					var rewIndex = 0;
					float topPosition = -55;
					float leftPositionReward = 50;

					foreach (var rew in Settings.RewardsForInviter.RefItems)
					{
						rewIndex++;
						var rUi = CuiHelper.GetGuid();

						container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = "0 1", AnchorMax = "1 1",
								OffsetMin = $"{leftPositionReward - 137.5} {topPosition - 37.5}",
								OffsetMax = $"{leftPositionReward - 62.5} {topPosition + 37.5}"
							},
							Image = {FadeIn = 1f, Color = "1 1 1 0.2"}
						}, Layer + ".Head", Layer + rUi);

						if (rewIndex == 10)
						{
							container.Add(new CuiLabel
							{
								RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
								Text =
								{
									FadeIn = 1f, Text = "+" + (Settings.RewardsForInviter.RefItems.Count - 12),
									Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 40,
									Color = "1 1 1 0.6"
								}
							}, Layer + rUi);

							break;
						}

						if (!string.IsNullOrEmpty(rew.ShortName))
							container.Add(new CuiElement
							{
								Parent = Layer + rUi,
								Components =
								{
									new CuiRawImageComponent
										{FadeIn = 1f, Png = (string) ImageLibrary.Call("GetImage", rew.ShortName)},
									new CuiRectTransformComponent
										{AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2 2", OffsetMax = "-2 -2"}
								}
							});

						leftPositionReward -= 85;
						if (leftPositionReward <= -200)
						{
							leftPositionReward = 50;
							topPosition -= 80;
						}
					}

					rewIndex = 0;
					topPosition = -55;
					leftPositionReward = 150;

					foreach (var rew in Settings.RewardsForInvited.RefItems)
					{
						rewIndex++;
						var rUi = CuiHelper.GetGuid();

						container.Add(new CuiPanel
						{
							RectTransform =
							{
								AnchorMin = "0 1", AnchorMax = "1 1",
								OffsetMin = $"{leftPositionReward - 137.5} {topPosition - 37.5}",
								OffsetMax = $"{leftPositionReward - 62.5} {topPosition + 37.5}"
							},
							Image = {FadeIn = 1f, Color = "1 1 1 0.2"}
						}, Layer + ".Head", Layer + rUi);


						if (rewIndex == 10)
						{
							container.Add(new CuiLabel
							{
								RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
								Text =
								{
									FadeIn = 1f, Text = "+" + (Settings.RewardsForInviter.RefItems.Count - 12),
									Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 40,
									Color = "1 1 1 0.6"
								}
							}, Layer + rUi);

							break;
						}

						if (!string.IsNullOrEmpty(rew.ShortName))
							container.Add(new CuiElement
							{
								Parent = Layer + rUi,
								Components =
								{
									new CuiRawImageComponent
										{FadeIn = 1f, Png = (string) ImageLibrary.Call("GetImage", rew.ShortName)},
									new CuiRectTransformComponent
										{AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2 2", OffsetMax = "-2 -2"}
								}
							});

						leftPositionReward += 85;
						if (leftPositionReward >= 350)
						{
							leftPositionReward = 150;
							topPosition -= 80;
						}
					}

					CuiHelper.AddUi(player, container);
					break;
				}
				case "choose":
				{
					if (!args.HasArgs(2))
					{
						UI_DrawChoose(player);
						return;
					}

					var targetId = 0UL;
					if (!ulong.TryParse(args.Args[1], out targetId)) return;

					if (RefProfiles[player.userID].InviterInfo != null) return;

					if (RefProfiles[targetId].InviterInfo == RefProfiles[player.userID].PlayerInfo) return;

					RefProfiles[player.userID].InviterInfo = RefProfiles[targetId].PlayerInfo;
					RefProfiles[targetId].InvitedInfos.Add(RefProfiles[player.userID].PlayerInfo);

					player.ChatMessage(
						"Вы <color=orange>успешно выбрали</color>, награда находится у вас в инвентаре!");
					UI_DrawInterface(player);

					Settings.RewardsForInvited.ProcessRewards(player);
					var targetPlayer = BasePlayer.FindByID(targetId);
					if (targetPlayer != null && targetPlayer.IsConnected)
						targetPlayer.ChatMessage(
							$"<color=orange>{player.displayName}</color> теперь ваш реферал, вы можете забрать награду (/ref)");
					break;
				}
				case "collect":
				{
					if (!args.HasArgs(2)) return;

					var targetId = 0UL;
					if (!ulong.TryParse(args.Args[1], out targetId)) return;

					if (RefProfiles[player.userID].InvitedInfos.All(p => p.UserID != targetId)) return;

					Settings.RewardsForInviter.ProcessRewards(player);
					RefProfiles[targetId].Collected = true;
					UI_DrawInterface(player);
					break;
				}
			}
		}

		#endregion

		#region Interface

		private const string Layer = "UI.IRS2.Layer";

		private void UI_DrawChoose(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, Layer);
			var container = new CuiElementContainer();

			container.Add(new CuiPanel
			{
				CursorEnabled = true,
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
				Image = {Color = "0 0 0 0"}
			}, Layer + ".Owner", Layer);

			container.Add(new CuiButton
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
				Button = {Color = "0 0 0 0", Close = Layer + ".Owner"},
				Text = {Text = ""}
			}, Layer);

			float leftPosition = 0;
			float topPosition = 0;

			for (var i = 0; i < ConVar.Server.maxplayers; i++)
			{
				var check = BasePlayer.activePlayerList.ElementAtOrDefault(i);

				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0.04 1", AnchorMax = "0.04 1",
						OffsetMin = $"{0 + leftPosition} {topPosition - 60}",
						OffsetMax = $"{120 + leftPosition} {topPosition - 35}"
					},
					Image =
					{
						Color = check == null ? "1 1 1 0.1" :
							player.userID == check.userID ? "1 0.5 0.5 0.2" : "1 1 1 0.2"
					}
				}, Layer, Layer + player.userID);

				if (check != null)
				{
					container.Add(new CuiElement
					{
						Parent = Layer + player.userID,
						Components =
						{
							new CuiRawImageComponent
								{Png = (string) ImageLibrary.Call("GetImage", check.UserIDString)},
							new CuiRectTransformComponent
								{AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "2 2", OffsetMax = "23 23"}
						}
					});

					container.Add(new CuiLabel
					{
						RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "30 0", OffsetMax = "0 0"},
						Text =
						{
							Text = check.displayName, Align = TextAnchor.MiddleLeft,
							Font = "robotocondensed-regular.ttf"
						}
					}, Layer + player.userID);

					if (check.userID != player.userID)
						container.Add(new CuiButton
						{
							RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
							Button = {Color = "0 0 0 0", Command = $"RS_Controller choose {check.userID}"},
							Text = {Text = ""}
						}, Layer + player.userID);
				}


				leftPosition += 125;
				if (leftPosition >= 125 * 10)
				{
					topPosition -= 30;
					leftPosition = 0;
				}
			}

			CuiHelper.AddUi(player, container);
		}

		private void UI_DrawInterface(BasePlayer player)
		{
			var userInfo = RefProfiles.FirstOrDefault(p => p.Key == player.userID).Value;

			CuiHelper.DestroyUi(player, Layer + ".Owner");
			var container = new CuiElementContainer();

			container.Add(new CuiPanel
			{
				CursorEnabled = true,
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
				Image = {Color = "0 0 0 0.9"}
			}, "Overlay", Layer + ".Owner");

			container.Add(new CuiPanel
			{
				CursorEnabled = true,
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
				Image = {Color = "0 0 0 0"}
			}, Layer + ".Owner", Layer);

			container.Add(new CuiButton
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
				Button = {Color = "0 0 0 0", Close = Layer + ".Owner"},
				Text = {Text = ""}
			}, Layer);

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.25 0.5", AnchorMax = "0.25 0.8", OffsetMin = "-29 -58", OffsetMax = "-25 42"},
				Image = {Color = "1 1 1 0"}
			}, Layer, Layer + ".Head");

			container.Add(new CuiLabel
			{
				RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "-250 0", OffsetMax = "250 50"},
				Text =
				{
					FadeIn = 1f, Text = "Список доступных наград", Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-bold.ttf", FontSize = 24
				}
			}, Layer + ".Head");

			container.Add(new CuiButton
			{
				RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "-250 -25", OffsetMax = "250 30"},
				Button = {Color = "0 0 0 0", Close = Layer + ".Show", Command = "RS_Controller show"},
				Text =
				{
					FadeIn = 1f, Text = "Нажмите чтобы посмотреть", Align = TextAnchor.MiddleCenter,
					Font = "robotocondensed-regular.ttf", FontSize = 18, Color = "1 1 1 0.7"
				}
			}, Layer + ".Head", Layer + ".Show");

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.8", OffsetMin = "-2 75", OffsetMax = "2 -75"},
				Image = {Color = "1 1 1 0.2"}
			}, Layer);

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-75 -75", OffsetMax = "75 75"},
				Image = {Color = "1 1 1 0.2"}
			}, Layer, Layer + ".User");

			container.Add(new CuiElement
			{
				Parent = Layer + ".User",
				Components =
				{
					new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", player.UserIDString)},
					new CuiRectTransformComponent
						{AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5"}
				}
			});

			container.Add(new CuiButton
			{
				RectTransform = {AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-250 -75", OffsetMax = "-15 -15"},
				Button = {Color = "0 0 0 0", Close = Layer + ".Owner"},
				Text =
				{
					Text = "ЗАКРЫТЬ", Align = TextAnchor.UpperRight, Font = "robotocondensed-bold.ttf", FontSize = 24,
					Color = "1 1 1 0.2"
				}
			}, Layer);

			container.Add(new CuiLabel
			{
				RectTransform = {AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = "25 0", OffsetMax = "1000 0"},
				Text =
				{
					Color = "1 1 1 0.8", Text = "<size=24><b>Это ваш реферальный профиль</b></size>\n" +
					                            "<size=18>Приглашайте друзей и получайте награды</size>",
					Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf"
				}
			}, Layer + ".User");

			/*container.Add(new CuiLabel
			{
			    FadeOut = 1f,
			    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"25 0", OffsetMax = "500 50" },
			    Text = { Text = "Нажмите для просмотра наград", Align = TextAnchor.MiddleLeft, Font = "robotocondensed-bold.ttf", FontSize = 24, Color = "1 1 1 0.6"}
			}, Layer ,Layer + ".Show");*/

			/*container.Add(new CuiButton
			{
			    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = $"0 0", OffsetMax = "0 0" },
			    Button = { Color = "0 0 0 0", Close = Layer + ".Show", Command = "RS_Controller show"},
			    Text = { Text = "", Align = TextAnchor.MiddleLeft, Font = "robotocondensed-bold.ttf", FontSize = 24 }
			}, Layer + ".Show");*/

			var inviter = userInfo.GetInviter();

			var imageId = "UNKNOWNUSERID";
			var text = "<size=24><b>Вы не указали пригласившего вас игрока</b></size>\n" +
			           "<size=18>Нажмите на изображение, чтобы выбрать игрока</size>";
			if (inviter != null)
			{
				imageId = inviter.PlayerInfo.UserID.ToString();
				text = "<size=24><b>Это пригласивший вас игрок</b></size>\n" +
				       "<size=18>Он уведомлён о вашем выборе</size>";
			}

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.8", AnchorMax = "0.5 0.8", OffsetMin = "-75 -75", OffsetMax = "75 75"},
				Image = {Color = "1 1 1 0.2"}
			}, Layer, Layer + ".Inviter");

			container.Add(new CuiElement
			{
				Parent = Layer + ".Inviter",
				Components =
				{
					new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", imageId)},
					new CuiRectTransformComponent
						{AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5"}
				}
			});

			if (inviter == null)
				container.Add(new CuiButton
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
					Text = {Text = ""},
					Button = {Color = "0 0 0 0", Command = "RS_Controller choose"}
				}, Layer + ".Inviter");

			var leftPosition = Settings.MaxInvitedAmount / 2f * -110 - (Settings.MaxInvitedAmount - 1) / 2f * 25;
			var startPosition = leftPosition + 110 / 2f;

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.2", AnchorMax = "0.5 0.5", OffsetMin = $"{-2} 79", OffsetMax = $"{2} -75"},
				Image = {Color = "1 1 1 0.2"}
			}, Layer);

			/*container.Add(new CuiLabel
			{
			    RectTransform = {AnchorMin = "0 0", AnchorMax                                                                                                              = "1 0", OffsetMax            = "0 90"}, 
			    Text          = {Text      = $"Здесь отображены игроки, которых вы пригласили на сервер\nВы можете получать награду за их игру на сервере", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 16 }
			}, Layer);*/

			container.Add(new CuiLabel
			{
				RectTransform = {AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = "25 0", OffsetMax = "1000 0"},
				Text =
				{
					Color = "1 1 1 0.8", Text = text, Align = TextAnchor.MiddleLeft,
					Font = "robotocondensed-regular.ttf"
				}
			}, Layer + ".Inviter");

			for (var i = 0; i < Settings.MaxInvitedAmount; i++)
			{
				RefProfile invitedProfile = null;
				var invited = userInfo.InvitedInfos.ElementAtOrDefault(i);
				if (invited != null) invitedProfile = userInfo.GetProfileByInfo(invited);

				imageId = "UNKNOWNUSERID";
				if (invitedProfile != null)
					imageId = invitedProfile.PlayerInfo.UserID.ToString();

				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0.5 0.2", AnchorMax = "0.5 0.2", OffsetMin = $"{leftPosition + 53} 55",
						OffsetMax = $"{leftPosition + 57} 75"
					},
					Image = {Color = "1 1 1 0.2"}
				}, Layer);

				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0.5 0.2", AnchorMax = "0.5 0.2", OffsetMin = $"{leftPosition} -55",
						OffsetMax = $"{leftPosition + 110} 55"
					},
					Image = {Color = invitedProfile == null || invitedProfile.Collected ? "1 1 1 0.2" : "0.8 1 0.8 0.6"}
				}, Layer, Layer + $".Invited.{i}");

				container.Add(new CuiElement
				{
					Parent = Layer + $".Invited.{i}",
					Components =
					{
						new CuiRawImageComponent
							{FadeIn = 0f + i * 0.1f, Png = (string) ImageLibrary.Call("GetImage", imageId)},
						new CuiRectTransformComponent
							{AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5"}
					}
				});


				leftPosition += 135;
				if (invitedProfile == null) continue;

				/*container.Add(new CuiPanel
				{
				    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 -15", OffsetMax = "0 0"},
				    Image         = {Color     = "1 1 1 0.2"} 
				}, Layer + $".Invited.{i}", Layer + $".Invited.{i}.Progress");
				*/
				/*container.Add(new CuiPanel
				{
				    RectTransform = {AnchorMin = "0 0", AnchorMax = $"{currentProgress / 3} {1}", OffsetMax = "0 0"},
				    Image         = {Color     = "0.7 1 0.7 0.2"}
				}, Layer + $".Invited.{i}.Progress");*/

				if (!invitedProfile.Collected)
					container.Add(new CuiButton
					{
						RectTransform =
							{AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-10 -20", OffsetMax = "10 0"},
						Button =
						{
							Color = "0.6 1 0.6 0", Command = $"RS_Controller collect {invitedProfile.PlayerInfo.UserID}"
						},
						Text =
						{
							Text = "Заберите награду".ToUpper(), Align = TextAnchor.LowerCenter,
							Font = "robotocondensed-regular.ttf", FontSize = 14, Color = "1 1 1 0.7"
						}
					}, Layer + $".Invited.{i}");
			}

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0.5 0.2", AnchorMax = "0.5 0.2", OffsetMin = $"{startPosition - 2} 75",
					OffsetMax = $"{leftPosition - 78} 79"
				},
				Image = {Color = "1 1 1 0.2"}
			}, Layer);


			CuiHelper.AddUi(player, container);
		}

		#endregion
		
		#region Utils

		private int GetActiveReferals(BasePlayer player)
		{
			var amount = 1;

			if (RefProfiles.ContainsKey(player.userID)) amount = RefProfiles[player.userID].InvitedInfos.Count;

			return amount;
		}

		private void LoadImages()
		{
			timer.In(5, () =>
			{
				if (!ImageLibrary)
				{
					PrintWarning("IMAGE LIBRARY IS NOT INSTALLED!");
				}
				else
				{
					ImageLibrary.Call("AddImage", "https://i.imgur.com/nmWKdi8.png", "UNKNOWNUSERID");

					var itemIcons = new List<KeyValuePair<string, ulong>>();

					Settings.RewardsForInvited.RefItems.ForEach(item =>
					{
						if (!string.IsNullOrEmpty(item.ShortName) &&
						    !itemIcons.Exists(x => x.Key == item.ShortName && x.Value == item.SkinID))
							itemIcons.Add(new KeyValuePair<string, ulong>(item.ShortName, item.SkinID));
					});

					Settings.RewardsForInviter.RefItems.ForEach(item =>
					{
						if (!string.IsNullOrEmpty(item.ShortName) &&
						    !itemIcons.Exists(x => x.Key == item.ShortName && x.Value == item.SkinID))
							itemIcons.Add(new KeyValuePair<string, ulong>(item.ShortName, item.SkinID));
					});

					if (itemIcons.Count > 0) ImageLibrary?.Call("LoadImageList", Title, itemIcons, null);
				}
			});
		}

		#endregion
	}
