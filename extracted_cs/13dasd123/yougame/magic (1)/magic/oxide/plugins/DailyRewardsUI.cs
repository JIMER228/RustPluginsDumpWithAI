// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Epic.OnlineServices.UserInfo;
using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
// Removed DailyRewardsExtensionMethods dependency
using Rust;
using UnityEngine;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
	[Info("Daily Rewards UI", "Forum: https://topplugin.ru Ds: alone_sempai Vk: https://vk.com/rustnastroika", "1.0.5")]
	public class DailyRewardsUI : RustPlugin
	{
		[PluginReference] private Plugin
			AFKAPI = null,
			ImageLibrary = null,
			WipeBlock = null,
			NoEscape = null,
			Notify = null,
			UINotify = null,
			LangAPI = null;

		private static DailyRewardsUI _instance;

		private bool _enabledImageLibrary;

		private const bool LangRu = true;

		private const string
			Layer2 = "UI.DailyRewards",
			MainLayer = "UI.DailyRewards.Main",
			ModalLayer = "UI.DailyRewards.Modal",
			ModalMainLayer = "UI.DailyRewards.Modal.Main",
			SecondModalLayer = "UI.DailyRewards.Second.Modal",
			SecondModalMainLayer = "UI.DailyRewards.Second.Modal.Main",
			SelectItemModalLayer = "UI.DailyRewards.Select.Item.Modal",
			CMD_Main_Console = "UI_DailyRewards";

		private TimeZoneInfo _pluginTimeZone;

		private Dictionary<int, AwardInfo> _awardsByID = new();

		#region Config

		private static Configuration _config;

		private class Configuration
		{
			[JsonProperty(PropertyName = LangRu ? "Команды" : "Commands",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public string[] Commands = {"rewards", "daily"};

			[JsonProperty(PropertyName = LangRu ? "Включить работу с Notify?" : "Work with Notify?")]
			public bool UseNotify = true;

			[JsonProperty(PropertyName = LangRu ? "Включить работу с LangAPI?" : "Work with LangAPI?")]
			public bool UseLangAPI = true;

			[JsonProperty(PropertyName = LangRu ? "Вайпать при новом сохранении карты" : "Wipe on new map save")]
			public bool WipeOnNewSave = false;

			[JsonProperty(PropertyName = LangRu ? "Настройка задержки" : "Cooldown Settings")]
			public CooldownSettings Cooldown = new()
			{
				Enabled = true,
				DefaultCooldown = 60,
				Cooldowns = new Dictionary<string, float>
				{
					["dailyrewards.vip"] = 3100,
					["dailyrewards.premium"] = 2600
				},
				CheckAFK = false
			};

			[JsonProperty(PropertyName = LangRu ? "Ежедневные награды" : "Daily awards",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public SortedDictionary<int, AwardDayInfo> DailyAwards = new()
			{
				[1] = new AwardDayInfo
				{
					Enabled = true,
					Image = "https://i.ibb.co/7Gs1yFn/image.png",
					Title = "<b><size=12>ВЗРЫВЧАТКА</size></b>\nБум-бабах. Это точно\nнадо забирать",
					Description = "Получите стартовый набор предметов на 1 день.",
					StoreToStorage = true,
					Awards = new List<AwardInfo>
					{
						AwardInfo.CreateItem("pistol.revolver"),
						AwardInfo.CreateItem("ammo.pistol", 64)
					},
					IsSpecialDay = false
				},
				[2] = new AwardDayInfo
				{
					Enabled = true,
					Image = "https://i.ibb.co/RhL03Qn/image.png",
					Title = "GUNS",
					Description = "Усиление огневой мощи.",
					StoreToStorage = true,
					Awards = new List<AwardInfo>
					{
						AwardInfo.CreateItem("pistol.m92"),
						AwardInfo.CreateItem("ammo.pistol", 128)
					},
					IsSpecialDay = false
				},
				[3] = new AwardDayInfo
				{
					Enabled = true,
					Image = "https://i.ibb.co/1ZFQKS7/image.png",
					Title = "GUNS",
					Description = "Набор для дальнего боя.",
					StoreToStorage = true,
					Awards = new List<AwardInfo>
					{
						AwardInfo.CreateItem("rifle.semiauto"),
						AwardInfo.CreateItem("ammo.rifle", 64)
					},
					IsSpecialDay = false
				},
				[4] = new AwardDayInfo
				{
					Enabled = true,
					Image = "https://i.ibb.co/5BNXFPZ/image.png",
					Title = "GUNS",
					Description = "Продвинутый огнестрел.",
					StoreToStorage = true,
					Awards = new List<AwardInfo>
					{
						AwardInfo.CreateItem("rifle.m39"),
						AwardInfo.CreateItem("ammo.rifle", 128)
					},
					IsSpecialDay = false
				},
				[5] = new AwardDayInfo
				{
					Enabled = true,
					Image = "https://i.ibb.co/Cbz55sP/image.png",
					Title = "GUNS",
					Description = "Максимальная мощь.",
					StoreToStorage = true,
					Awards = new List<AwardInfo>
					{
						AwardInfo.CreateItem("rifle.ak"),
						AwardInfo.CreateItem("ammo.rifle", 128),
						AwardInfo.CreateItem("ammo.rifle", 128)
					},
					IsSpecialDay = false
				},
				[6] = new AwardDayInfo
				{
					Enabled = true,
					Image = "https://i.ibb.co/NZnjSFz/kuyura.png",
					Title = "CASH",
					Description = "Немного валюты на развитие.",
					StoreToStorage = true,
					Awards = new List<AwardInfo>
					{
						AwardInfo.CreateCommand("deposit %steamid% 100", 1, "https://i.ibb.co/NZnjSFz/kuyura.png")
					},
					IsSpecialDay = false
				},
				[7] = new AwardDayInfo
				{
					Enabled = true,
					Image = "https://i.ibb.co/NZnjSFz/kuyura.png",
					Title = "CASH",
					Description = "Большая награда за серию.",
					StoreToStorage = true,
					Awards = new List<AwardInfo>
					{
						AwardInfo.CreateCommand("deposit %steamid% 1000", 1, "https://i.ibb.co/NZnjSFz/kuyura.png")
					},
					IsSpecialDay = true
				}
			};

			[JsonProperty(PropertyName = LangRu ? "Настройки инвентаря" : "Inventory settings")]
			public InventorySettings Inventory = new()
			{
				Enabled = true,
				Title = "ЕЖЕДНЕВНЫЕ НАГРАДЫ",
				Description = "Здесь хранятся все предметы, полученные в ежедневных наградах, независимо от вайпа.\nНажмите, чтобы перейти в инвентарь.",
				InventoryTitle = "ИНВЕНТАРЬ НАГРАД",
				InventoryDescription = "Нажмите, что бы вернуться в раздел наград"
			};

			[JsonProperty(PropertyName = LangRu ? "Текстовые настройки" : "Text settings")]
			public TextSettings Texts = new()
			{
				ButtonReceive = "ПОЛУЧИТЬ",
				ButtonReceived = "ПОЛУЧЕНО",
				ButtonUnavailable = "НЕДОСТУПНО",
				ButtonTimeLeft = "%TIME_LEFT%"
			};

			[JsonProperty(PropertyName = LangRu ? "Бонусы по ролям" : "Role-based bonuses")]
			public List<RoleBonus> RoleBonuses = new()
			{
				new RoleBonus { Permission = "dailyrewards.vip", Multiplier = 1.25f },
				new RoleBonus { Permission = "dailyrewards.premium", Multiplier = 1.5f }
			};

			public ResetSettings Reset = new();

			public void BuildDefault30DaysIfEmpty()
			{
				if (DailyAwards != null && DailyAwards.Count > 0) return;
				DailyAwards = new SortedDictionary<int, AwardDayInfo>();

				for (int day = 1; day <= 30; day++)
				{
					var info = new AwardDayInfo
					{
						Enabled = true,
						Image = string.Empty,
						Title = $"ДЕНЬ {day}",
						Description = day % 7 == 0 ? "Особая награда за серию" : "Стандартная ежедневная награда",
						StoreToStorage = true,
						IsSpecialDay = (day % 7 == 0),
						Awards = new List<AwardInfo>()
					};

					if (day <= 5)
					{
						info.Awards.Add(AwardInfo.CreateItem("stone.pickaxe", 1));
						info.Awards.Add(AwardInfo.CreateItem("wood", 1000));
					}
					else if (day <= 10)
					{
						info.Awards.Add(AwardInfo.CreateItem("metal.fragments", 750));
						info.Awards.Add(AwardInfo.CreateItem("metal.refined", 25));
					}
					else if (day <= 15)
					{
						info.Awards.Add(AwardInfo.CreateItem("rifle.semiauto", 1));
						info.Awards.Add(AwardInfo.CreateItem("ammo.rifle", 120));
					}
					else if (day <= 20)
					{
						info.Awards.Add(AwardInfo.CreateItem("smg.2", 1));
						info.Awards.Add(AwardInfo.CreateItem("ammo.pistol", 150));
					}
					else if (day <= 25)
					{
						info.Awards.Add(AwardInfo.CreateItem("explosive.timed", 1));
					}
					else
					{
						info.Awards.Add(AwardInfo.CreateCommand("deposit %steamid% 500", 1));
					}

					DailyAwards[day] = info;
				}
			}
		}

		private class TextSettings
		{
			[JsonProperty(PropertyName = "Кнопка: Получить")] public string ButtonReceive = "ПОЛУЧИТЬ";
			[JsonProperty(PropertyName = "Кнопка: Получено")] public string ButtonReceived = "ПОЛУЧЕНО";
			[JsonProperty(PropertyName = "Кнопка: Недоступно")] public string ButtonUnavailable = "НЕДОСТУПНО";
			[JsonProperty(PropertyName = "Кнопка: Осталось времени (%TIME_LEFT%)")] public string ButtonTimeLeft = "%TIME_LEFT%";
		}

		private class CooldownSettings
		{
			[JsonProperty("Enabled")]
			public bool Enabled = true;

			[JsonProperty("Default Cooldown")]
			public float DefaultCooldown = 60;

			[JsonProperty("Cooldowns", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, float> Cooldowns = new();

			[JsonProperty("Check AFK")]
			public bool CheckAFK = false;

			public float GetCooldown(string playerId)
			{
				if (!Enabled) return 0;

				if (string.IsNullOrEmpty(playerId))
					return DefaultCooldown;

				var result = DefaultCooldown;

				if (Cooldowns != null && Cooldowns.Count > 0)
				{
					foreach (var kvp in Cooldowns)
					{
						if (_instance?.permission?.UserHasPermission(playerId, kvp.Key) == true)
						{
							// Choose the smallest cooldown among matched permissions
							result = Mathf.Min(result, kvp.Value);
						}
					}
				}

				return result;
			}
		}

		private class InventorySettings
		{
			[JsonProperty("Enabled")]
			public bool Enabled = true;

			[JsonProperty("Title")]
			public string Title = "ЕЖЕДНЕВНЫЕ НАГРАДЫ";

			[JsonProperty("Description")]
			public string Description = "Здесь хранятся все предметы, полученные в ежедневных наградах, независимо от вайпа.\nНажмите, чтобы перейти в инвентарь.";

			[JsonProperty("Inventory Title")]
			public string InventoryTitle = "ИНВЕНТАРЬ НАГРАД";

			[JsonProperty("Inventory Description")]
			public string InventoryDescription = "Нажмите, что бы вернуться в раздел наград";
		}

		private class RoleBonus
		{
			[JsonProperty(PropertyName = "Permission")] public string Permission;
			[JsonProperty(PropertyName = LangRu ? "Множитель" : "Multiplier")] public float Multiplier = 1.0f;
		}

		#endregion

		#region Interface
		private const string Layer = "ui.MenuBase.bg";

		#region UI Constants
		private const string WHITE_TRANSPARENT_BACKGROUND = "1 1 1 0.3";
		private const string ORANGE_COLOR = "0.9490196 0.5019608 0.05490196 1";
		private const string BACKGROUND_COLOR = "0.3568628 0.3568628 0.3568628 0.75";
		private const string GRADIENTDOWN_COLOR = "0 0 0 0.7";
		private const string TEXT_COLOR = "1 1 1 1";
		private const string GREEN = "0.247 0.933 0.0 0.26";
		private const string RED_COLOR = "0.6901961 0.3490196 0.3490196 0.8";
		#endregion

		private void UI_DrawBackground(BasePlayer player, bool full)
		{
			var container = new CuiElementContainer();
			container.Add(new CuiPanel
			{
				CursorEnabled = false,
				Image = { Color = BACKGROUND_COLOR },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-314.69 {(full ? -229.23 : -162.781)}", OffsetMax = "314.69 229.232" }
			}, Layer + ".main.div", Layer + ".main.div" + ".main.div.customshop");

			container.Add(new CuiPanel()
			{
				Image = {Color = "0 0 0 0"},
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 -65", OffsetMax = "0 -65"}
			}, Layer + ".main.div" + ".main.div.customshop", Layer + ".main.div" + ".main.div.customshop" + ".bg");

			container.Add(new CuiButton
			{
				Button = { Color = RED_COLOR, Close = Layer + ".blur" },
				Text = { Text = "X", Font = "permanentmarker.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = TEXT_COLOR },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "285.139 231.174", OffsetMax = "314.69 261" }
			}, Layer + ".main.div" + ".main.div.customshop" + ".bg", Layer + ".main.div" + ".close");

			CuiHelper.AddUi(player, container);
		}

		private void UI_DrawMain(BasePlayer player)
		{
			UI_DrawBackground(player, false);

			var container = new CuiElementContainer();
			container.Add(new CuiPanel
			{
				CursorEnabled = false,
				Image = { Color = "0.3568628 0.3568628 0.3568628 0.75" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-314.69 -197.23", OffsetMax = "314.69 -135.288" }
			}, Layer + ".main.div" + ".main.div.customshop" + ".bg", Layer + ".main.div" + ".takepanel.div", Layer + ".main.div" + ".takepanel.div");

			container.Add(new CuiElement
			{
				Name = Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".desc2",
				Parent = Layer + ".main.div" + ".main.div.customshop" + ".bg",
				Components = {
					new CuiTextComponent { Text = "Заходите на сервер каждый день, чтобы получать ценные награды.\nНо помните - при пропуске серия дней обнуляется", Font = "robotocondensed-regular.ttf", FontSize = 13, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
						new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-302.295 128.705", OffsetMax = "24.335 195.095" }
				}
			});
			CuiHelper.AddUi(player, container);

			UI_DrawItems(player);
		}

		private void UI_UpdateName(BasePlayer player, string name, string desc)
		{
			var container = new CuiElementContainer();
			container.Add(new CuiElement
			{
				Name = Layer + ".main.div" + ".main.div.customshop" + ".label",
				Parent = Layer + ".main.div" + ".main.div.customshop" + ".bg",
				DestroyUi = Layer + ".main.div" + ".main.div.customshop" + ".label",
				Components = {
					new CuiTextComponent { Text = name, Font = "robotocondensed-bold.ttf", FontSize = 27, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-301.932 205.767", OffsetMax = "66.788 258" }
				}
			});
			container.Add(new CuiElement
			{
				Name = Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".desc",
				Parent = Layer + ".main.div" + ".takepanel.div",
				DestroyUi = Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".desc",
				Components = {
					new CuiTextComponent { Text = desc, Font = "robotocondensed-regular.ttf", FontSize = 13, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-163.315 -25", OffsetMax = "163.315 27" }
				}
			});
			CuiHelper.AddUi(player, container);
		}

		private void UI_UpdateButton(BasePlayer player, bool state)
		{
			var container = new CuiElementContainer();
			container.Add(new CuiButton
			{
				Button =
				{
					Color = ORANGE_COLOR, Command = state ? "mb.daily.goback" : "mb.daily.inventory"
				},
				Text = { Text = state ? "ВЕРНУТЬСЯ" : "ИНВЕНТАРЬ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-302.29 -13.5885", OffsetMax = "-192.128 13.5885" }
			}, Layer + ".main.div" + ".takepanel.div", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".inventory.btn", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".inventory.btn");

			CuiHelper.AddUi(player, container);
		}

		private void UI_DrawItems(BasePlayer player)
		{
			UI_UpdateName(player, _config.Inventory.Title, _config.Inventory.Description);
			UI_UpdateButton(player, false);
			var container = new CuiElementContainer();
			container.Add(new CuiPanel
			{
				CursorEnabled = false,
				Image = { Color = "1 1 1 0" },
				RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "11.135 -309.973", OffsetMax = "615.565 -84.153" }
			}, Layer + ".main.div" + ".main.div.customshop" + ".bg", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div");

			var openedReward = GetOrCreateOpenedRewardPlayer(player);
			if (openedReward == null) return;

			var targetUI = openedReward.GetUI();
			var awards = openedReward.availableAwards;

			var data = PlayerData.GetOrCreate(player.userID);
			var diff = DateTime.Now - data.LastTake;
			if (diff.Days >= 2 && diff.Days < 600000)
			{
				data.ResetRewards();
			}
			float minx = -302.22f;
			float maxx = -191.6163f;
			float miny = -112.9098f;
			float maxy = 112.9102f;

			int i = 0;

			var timeToNextDaySeconds = data.GetTimeForNextDay(player);
			foreach (var award in awards.Skip(openedReward.currentPage * targetUI.AwardsOnLine).Take(targetUI.AwardsOnLine))
			{
				bool isSpecial = award.dayInfo.IsSpecialDay;
				bool isTaken = data.IsTaked(award.day);
				bool canTake = data.CanTake(player.userID, award.day);
				bool isNext = data.TakedDays.Count > 0 ? (data.TakedDays.Max() + 1 == award.day) : i == 0 ? true : false;

				var color = isTaken ?
					GREEN
					: canTake && isNext ?
						ORANGE_COLOR
						: isSpecial ?
							RED_COLOR
							: WHITE_TRANSPARENT_BACKGROUND ;

				container.Add(new CuiPanel
				{
					CursorEnabled = false,
					Image = { Color = "1 1 1 0" },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{minx} {miny}", OffsetMax = $"{maxx} {maxy}" },
				}, Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}");

				container.Add(new CuiPanel()
				{
					CursorEnabled = false,
					Image = { Color = "1 1 1 0.15" },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-55.3 -82.735", OffsetMax = "55.3 112.91" }
				}, Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}" + ".mai");

				container.Add(new CuiPanel
				{
					CursorEnabled = false,
					Image = { Color = color == ORANGE_COLOR ? "0.9490196 0.5019608 0.05490196 0.8" : color, Sprite = "Assets/Content/UI/UI.Gradient.Up.psd" },
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "-0.05 0" }
				}, Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}" + ".mai", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}" + ".main");

				container.Add(new CuiPanel
				{
					CursorEnabled = false,
					Image = { Color = color },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50.564 78.887", OffsetMax = "-5.763 94.2" }
				}, Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}" + ".main", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}" + ".main" + ".day.bg");

				container.Add(new CuiElement
				{
					Name = Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}" + ".main" + ".day.bg" + ".text",
					Parent = Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}" + ".main" + ".day.bg",
					Components = {
									new CuiTextComponent { Text = $"ДЕНЬ {award.day}", Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
									new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-22.401 -7.656", OffsetMax = "22.4 7.656" }
								}
				});

				container.Add(new CuiButton
				{
					Button = { Color = GRADIENTDOWN_COLOR, Command = $"mb.daily.whatis {award.day}" },
					Text = { Text = "" },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "36.388 78.888", OffsetMax = "51.701 94.2" }
				}, Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}" + ".main", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}" + ".main" + ".whatis");

				container.Add(new CuiElement
				{
					Name = Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}" + ".main" + ".whatis" + ".text",
					Parent = Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}" + ".main" + ".whatis",
					Components = {
									new CuiTextComponent { Text = "?", Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
									new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-7.657 -7.656", OffsetMax = "7.656 7.656" }
								}
				});

				container.Add(new CuiElement
				{
					Name = Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}" + ".main" + ".reward.img",
					Parent = Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}" + ".main",
					Components =
					{
						award.dayInfo.GetImage(),
						new CuiRectTransformComponent() { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-40 -40", OffsetMax = "40 40" }
					}
				});

				container.Add(new CuiElement
				{
					Name = Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}" + ".main" + ".name",
					Parent = Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}" + ".main",
					Components = {
									new CuiTextComponent { Text = award.dayInfo.GetTitle(), Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
									new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -97.823", OffsetMax = "50 -40.818" }
								}
				});

				var text = _config?.Texts?.ButtonReceive ?? "ПОЛУЧИТЬ";
				if (isTaken)
					text = _config?.Texts?.ButtonReceived ?? "ПОЛУЧЕНО";
				else if (isNext && !canTake && timeToNextDaySeconds > 0)
					text = _config?.Texts?.ButtonTimeLeft ?? "%TIME_LEFT%";
				else if (canTake)
					text = _config?.Texts?.ButtonReceive ?? "ПОЛУЧИТЬ";
				else
					text = _config?.Texts?.ButtonUnavailable ?? "НЕДОСТУПНО";

				container.Add(new CuiButton
				{
					Button = { Color = color, Command = canTake ? $"{CMD_Main_Console} take_day {award.day}" : ""},
					Text = { Text = "", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-55.3 -112.91", OffsetMax = "55.3 -85.639" }
				}, Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}" + ".take.btn");
				container.Add(new CuiElement()
				{
					Parent = Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}" + ".take.btn",
					Components =
					{
						new CuiTextComponent() { Text = text, Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
						new CuiCountdownComponent() { EndTime = 0, Interval = 1, StartTime = timeToNextDaySeconds, Step = 1, TimerFormat = timeToNextDaySeconds >= 3600 ? TimerFormat.HoursMinutesSeconds : TimerFormat.MinutesSeconds },
						new CuiRectTransformComponent() { AnchorMin = "0 0", AnchorMax = "1 1" }
					}
				});
				minx += 123.818f;
				maxx += 123.818f;
				i++;
			}

			// Add awards pagination
			CuiHelper.AddUi(player, container);
			UI_DrawAwardsPages(player, openedReward.currentPage, Mathf.CeilToInt((float)awards.Count / targetUI.AwardsOnLine));
		}

		private void UI_DrawAwardsPages(BasePlayer player, int page, int totalPages)
		{
			if (totalPages <= 1) return;

			var container = new CuiElementContainer();
			// place inside the top take panel bar, right-middle side
			container.Add(new CuiPanel()
			{
				Image = { Color = "0 0 0 0"},
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "200 -13.5", OffsetMax = "300 13.5" }
			}, Layer + ".main.div" + ".takepanel.div", Layer + ".main.div" + ".takepanel.div" + ".pagesawards", Layer + ".main.div" + ".takepanel.div" + ".pagesawards");

			container.Add(new CuiButton()
			{
				Button = { Color = GRADIENTDOWN_COLOR, Command = page > 0 ? $"mb.daily.page {page - 1}" : "" },
				Text = { Text = "<", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "-55 0"}
			}, Layer + ".main.div" + ".takepanel.div" + ".pagesawards");
			container.Add(new CuiButton()
			{
				Button = { Color = GRADIENTDOWN_COLOR, Command = (page + 1) < totalPages ? $"mb.daily.page {page + 1}" : ""},
				Text = { Text = ">", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "55 0"}
			}, Layer + ".main.div" + ".takepanel.div" + ".pagesawards");
			CuiHelper.AddUi(player, container);
		}

		private void UI_DrawInventory(BasePlayer player, int page = 0, bool isNew = false)
		{
			UI_UpdateName(player, _config.Inventory.InventoryTitle, _config.Inventory.InventoryDescription);
			UI_UpdateButton(player, true);
			var container = new CuiElementContainer();

			var data = PlayerData.GetOrCreate(player.userID);

			if (isNew)
				container.Add(new CuiPanel
				{
					CursorEnabled = false,
					Image = { Color = "1 1 1 0" },
					RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "11.135 -309.973", OffsetMax = "615.565 -84.153" }
				}, Layer + ".main.div" + ".main.div.customshop" + ".bg", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div");

			container.Add(new CuiPanel()
			{
				Image = { Color = "0 0 0 0"},
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1"}
			}, Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + ".container", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + ".container");
			float height = 90;
			float width = 90;

			float spacing = 13;
			int itemsPerRow = 5;
			int currentRow = 0;

			var items = data.GetStoredItems(page * 10, 10);

			int itemsCount = items.Count;

			for (int i = 0; i < itemsCount; i++)
			{
				var award = items[i];

				if ((i % itemsPerRow) == 0 && i > 0)
				{
					currentRow++;
				}
				float xPosition = (i % itemsPerRow * (width + spacing)) - 250;
				float yPosition = (currentRow * -(height + spacing)) + 30;

				container.Add(new CuiButton()
				{
					Button = { Color = "0 0 0 0.5", Material = "assets/content/ui/uibackgroundblur.mat", Command = $"{CMD_Main_Console} inventory give {award.Key} {i}"},
					Text = { Text = "" },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{xPosition} {yPosition}", OffsetMax = $"{xPosition + width} {yPosition + height}" },
				}, Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + ".container", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}");

				ShowAwardImage(award.Value, ref container, Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}",
					new()
					{
						AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -30", OffsetMax = "30 30"
					});

				container.Add(new CuiLabel()
					{
						Text = { Text = $"x{award.Value.Amount}", FontSize = 9, Font = "robotocondensed-regular.ttf", Align = TextAnchor.LowerRight, Color = "1 1 1 1" },
						RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "-5 0", OffsetMin = "0 3"}
					}, Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + $".{i}");
			}

			CuiHelper.AddUi(player, container);
		}

		private void UI_DrawDescription(BasePlayer player, AwardDayInfo info, int day)
		{
			var container = new CuiElementContainer();
			container.Add(new CuiButton()
			{
				Button = { Color = "0 0 0 0.5", Material = "assets/content/ui/uibackgroundblur.mat", Close = Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".blurdesc"},
				Text = { Text = "" },
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 -2", OffsetMax = "0 65"}
			}, Layer + ".main.div" + ".main.div.customshop" + ".bg", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".blurdesc", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".blurdesc");

			var descText = $"<b>ПРЕДМЕТЫ В НАГРАДЕ <color=orange>{day} ДЕНЬ</color></b>\n\n<size=11>Здесь отображены все награды, которые\n вы получите за данный день.</size>";
			if (!string.IsNullOrEmpty(info?.Description))
				descText += $"\n\n<size=11>{info.Description}</size>";

			container.Add(new CuiLabel()
			{
				Text = { Text = descText, FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-150 100", OffsetMax = "150 170"},
			}, Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".blurdesc", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".blurdesc" + ".desc");

			float width = 70;
			float height = 70;

			float spacing = 2;

			int itemCount = info.Awards.Count;

			int itemsPerRow = 7;
			int currentRow = 0;

			bool isOdd = itemCount % 2 == 1;

			for (int i = 0; i < itemCount; i++)
			{
				var award = info.Awards[i];
				float totalWidth = Mathf.Min(itemCount - currentRow * itemsPerRow, itemsPerRow) * (width + spacing) - spacing;
				float x = (i % itemsPerRow * (width + spacing) - totalWidth / 2) - (-8 + (isOdd ? 0 : 10));
				float y = currentRow * -(height + spacing);

				container.Add(new CuiButton()
					{
						Button = { Color = "0 0 0 0.7", Material = "assets/content/ui/uibackgroundblur.mat"},
						RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
							OffsetMin = $"{x} {y}",
							OffsetMax = $"{x + width} {y + height}",},
					}, Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".blurdesc", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".blurdesc" + $".{i}");

				ShowAwardImage(award, ref container, Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".blurdesc" + $".{i}",
					new()
					{
						AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -30", OffsetMax = "30 30"
					});

				container.Add(new CuiLabel()
				{
					Text = { Text = $"x{award.Amount}", FontSize = 9, Font = "robotocondensed-regular.ttf", Align = TextAnchor.LowerRight, Color = "1 1 1 1" },
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "-5 0", OffsetMin = "0 3"}
				}, Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".blurdesc" + $".{i}");
				if ((i + 1) % itemsPerRow == 0)
				{
					currentRow++;
				}
			}

			CuiHelper.AddUi(player, container);
		}

		private void UI_DrawPagesInventory(BasePlayer player, int page = 0)
		{
			var container = new CuiElementContainer();

			container.Add(new CuiPanel()
			{
				Image = { Color = "0 0 0 0"},
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -120", OffsetMax = "50 -90" }
			}, Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + ".pages", Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + ".pages");

			container.Add(new CuiButton()
			{
				Button = { Color = GRADIENTDOWN_COLOR, Command = page > 0 ? $"mb.daily.inventory.page {page - 1}" : "" },
				Text = { Text = "<", Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "-55 0"}
			}, Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + ".pages");
			container.Add(new CuiButton()
			{
				Button = { Color = GRADIENTDOWN_COLOR, Command = $"mb.daily.inventory.page {page + 1}"},
				Text = { Text = ">", Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "55 0"}
			}, Layer + ".main.div" + ".main.div.customshop" + ".bg" + ".items.div" + ".pages");
			CuiHelper.AddUi(player, container);
		}

		private void ShowAwardImage(AwardInfo item, ref CuiElementContainer container, string parent,
			CuiRectTransformComponent rectTransform)
		{
			if (_enabledImageLibrary && !string.IsNullOrEmpty(item.Image))
			{
				container.Add(new CuiElement
				{
					Name = parent + ".Image",
					DestroyUi = parent + ".Image",
					Parent = parent,
					Components =
					{
						new CuiRawImageComponent
						{
							Png = GetImage(item.Image)
						},
						rectTransform
					}
				});
			}
			else if (item.Definition != null)
			{
				container.Add(new CuiElement
				{
					Name = parent + ".Image",
					DestroyUi = parent + ".Image",
					Parent = parent,
					Components =
					{
						new CuiImageComponent
						{
							ItemId = item.Definition.itemid,
							SkinId = item.Skin
						},
						rectTransform
					}
				});
			}
			else
			{
				if (_enabledImageLibrary)
					container.Add(new CuiElement
					{
						Name = parent + ".Image",
						DestroyUi = parent + ".Image",
						Parent = parent,
						Components =
						{
							new CuiRawImageComponent
							{
								Png = GetImage(string.Empty)
							},
							rectTransform
						}
					});
			}
		}

		[ConsoleCommand("mb.daily.goback")]
		private void cmdGoBack(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null)
				return;

			UI_DrawItems(arg.Player());
		}

		[ConsoleCommand("mb.daily.inventory")]
		private void cmdInventory(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null)
				return;

			UI_DrawInventory(arg.Player(), 0, true);
			UI_DrawPagesInventory(arg.Player(), 0);
		}

		[ConsoleCommand("mb.daily.inventory.page")]
		private void cmdInvPage(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null || !arg.HasArgs(1))
				return;

			var page = arg.GetInt(0, -10);
			if (page == -10)
				return;

			if (page < 0)
				return;
			var data = PlayerData.GetOrCreate(arg.Player().userID).GetStoredItems(page * 10, 10);

			if (!data.Any())
				return;

			UI_DrawInventory(arg.Player(), page);
		}

		[ConsoleCommand("mb.daily.whatis")]
		private void cmdWhatIs(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null || !arg.HasArgs(1))
				return;

			if (!int.TryParse(arg.Args[0], out var day))
				return;
			var openedReward = GetOrCreateOpenedRewardPlayer(arg.Player());
			if (openedReward == null) return;

			var targetUI = openedReward.GetUI();
			var awards = openedReward.availableAwards;
			var d = awards.FirstOrDefault(x => x.day == day);

			if (d.dayInfo == null)
				return;

			UI_DrawDescription(arg.Player(), d.dayInfo, day);
		}

		[ConsoleCommand("mb.daily.closeui")]
		private void CmdClose(ConsoleSystem.Arg arg)
		{
			var player = arg.Player();
			if (player == null) return;
			CuiHelper.DestroyUi(player, Layer);
		}

		[ConsoleCommand("mb.daily.open")]
		private void cmdDailyOpen(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null)
				return;

			UI_DrawMain(arg.Player());
		}

		[ConsoleCommand("UI_DailyRewards")]
		private void CmdUIDaily(ConsoleSystem.Arg arg)
		{
			var player = arg.Player();
			if (player == null || !arg.HasArgs(1))
				return;

			var action = arg.Args[0];
			switch (action)
			{
				case "take_day":
				{
					if (!arg.HasArgs(2)) return;
					if (!int.TryParse(arg.Args[1], out var day) || day <= 0) return;

					var data = PlayerData.GetOrCreate(player.userID);
					if (!data.CanTake(player.userID, day))
						return;

					var awardTuple = GetAvailabeAwards(player.userID).FirstOrDefault(x => x.day == day);
					var dayInfo = awardTuple.dayInfo;
					if (dayInfo == null || dayInfo.Awards == null || dayInfo.Awards.Count == 0)
						return;

					foreach (var award in dayInfo.Awards)
					{
						var times = 1;
						// Apply highest role multiplier (rounded down to int, at least 1)
						if (_config.RoleBonuses != null && _config.RoleBonuses.Count > 0)
						{
							float best = 1.0f;
							foreach (var rb in _config.RoleBonuses)
							{
								if (!string.IsNullOrEmpty(rb.Permission) && permission.UserHasPermission(player.UserIDString, rb.Permission))
									best = Mathf.Max(best, rb.Multiplier);
							}
							times = Mathf.Max(1, Mathf.FloorToInt(best));
						}

						for (int i = 0; i < times; i++)
						{
							if (dayInfo.StoreToStorage)
								data.AddItemToStorage(award);
							else
								award.Get(player, 1);
						}
					}

					data.OnTake(day);

					UI_DrawItems(player);
					break;
				}
				case "inventory":
				{
					if (!arg.HasArgs(3)) return; // inventory give <guid> <index>
					var sub = arg.Args[1];
					if (sub != "give") return;

					var guid = arg.Args[2];
					if (string.IsNullOrEmpty(guid)) return;

					var data = PlayerData.GetOrCreate(player.userID);
					var awardId = data.GetItemID(guid);
					if (awardId == 0) return;

					if (!TryFindAwardByID(awardId, out var awardInfo)) return;

					awardInfo.Get(player, 1);
					data.OnGiveItem(guid);

					// Refresh inventory UI on the current page 0 by default
					UI_DrawInventory(player, 0, true);
					UI_DrawPagesInventory(player, 0);
					break;
				}
			}
		}
		#endregion

		#region Data Classes
		private class AwardDayInfo : Switchable
		{
			[JsonProperty(PropertyName = LangRu ? "Награды" : "Awards",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<AwardInfo> Awards;

			[JsonProperty(PropertyName = LangRu ? "Изображение" : "Image")]
			public string Image;

			[JsonProperty(PropertyName = LangRu ? "Заголовок" : "Title")]
			public string Title;

			[JsonProperty(PropertyName = LangRu ? "Описание" : "Description")]
			public string Description = string.Empty;

			[JsonProperty(PropertyName = LangRu ? "Кладть в склад вместо выдачи?" : "Store to storage instead of give?")]
			public bool StoreToStorage = true;

			[JsonProperty(PropertyName = LangRu ? "Требуется разрешение (день)" : "Required permission (day)")]
			public string RequiredPermission = string.Empty;

			[JsonProperty(PropertyName = LangRu ? "Требуемое время игры (мин)" : "Required played minutes")]
			public int RequiredPlayedMinutes = 0;

			[JsonProperty(PropertyName = LangRu ? "Особый день?" : "Is Special Day?")]
			public bool IsSpecialDay;

			public static AwardDayInfo GetDefault()
			{
				return new AwardDayInfo
				{
					Enabled = false,
					Awards = new List<AwardInfo>(),
					Image = string.Empty,
					Title = string.Empty,
					IsSpecialDay = false
				};
			}

			public string GetTitle()
			{
				if (!string.IsNullOrEmpty(Title))
					return Title;

				if (Awards.Count > 0)
				{
					var award = Awards[0];
					if (award != null && award.Definition != null)
						return award.Definition.category.ToString().ToUpper();
				}

				return string.Empty;
			}

			public ICuiComponent GetImage()
			{
				if (!string.IsNullOrEmpty(Image))
					return new CuiRawImageComponent
					{
						Png = _instance?.GetImage(Image)
					};

				if (Awards.Count > 0)
				{
					var award = Awards[0];
					if (award != null && award.Definition != null)
						return new CuiImageComponent
						{
							ItemId = award.Definition.itemid,
							SkinId = award.Skin
						};
				}

				return new CuiImageComponent
				{
					Color = "0 0 0 0"
				};
			}

			public List<AwardInfo> GetAvailableAwards(BasePlayer player, bool showAll)
			{
				if (showAll)
					return Awards;

				if (_instance == null)
					return Awards.FindAll(award => string.IsNullOrEmpty(award.Permission));

				return Awards.FindAll(award =>
					string.IsNullOrEmpty(award.Permission) ||
					_instance.permission.UserHasPermission(player.UserIDString, award.Permission));
			}
		}

		private class AwardInfo
		{
			#region Fields

			[JsonProperty(PropertyName = LangRu ? "Тип" : "Type")] [JsonConverter(typeof(StringEnumConverter))]
			public ItemType Type;

			[JsonProperty(PropertyName = "ID")] public int ID;

			[JsonProperty(PropertyName = LangRu ? "Разрешение (прим: dailyrewards.vip)" : "Permission (ex: dailyrewards.vip)")]
			public string Permission = string.Empty;

			[JsonProperty(PropertyName = LangRu ? "Изображение" : "Image")]
			public string Image = string.Empty;

			[JsonProperty(PropertyName = LangRu ? "Команда (%steamid%)" : "Command (%steamid%)")]
			public string Command = string.Empty;

			[JsonProperty(PropertyName = LangRu ? "Название набора" : "Kit Name")]
			public string Kit = string.Empty;

			[JsonProperty(PropertyName = LangRu ? "Настройка плагина" : "Plugin settings")]
			public PluginItem Plugin;

			[JsonProperty(PropertyName = LangRu ? "Отображаемое имя (пусто – по умолчанию)" : "DisplayName (empty - default)")]
			public string DisplayName = string.Empty;

			[JsonProperty(PropertyName = "ShortName")]
			public string ShortName = string.Empty;

			[JsonProperty(PropertyName = LangRu ? "Скин" : "Skin")]
			public ulong Skin;

			[JsonProperty(PropertyName = LangRu ? "Это Blueprint?" : "Is Blueprint?")]
			public bool Blueprint;

			[JsonProperty(PropertyName = LangRu ? "Количество" : "Amount")]
			public int Amount;

			[JsonProperty(PropertyName = LangRu ? "Настройка содержимого" : "Content settings")]
			public ItemContent Content;

			[JsonProperty(PropertyName = LangRu ? "Настройка оружия" : "Weapon settings")]
			public ItemWeapon Weapon;

			[JsonProperty(PropertyName = LangRu ? "Запрещать разделение предмета на стаки?" : "Prohibit splitting item into stacks?")]
			public bool ProhibitSplit;

			#endregion

			#region Main

			public void Get(BasePlayer player, int count = 1)
			{
				switch (Type)
				{
					case ItemType.Item:
						ToItem(player, count);
						break;
					case ItemType.Command:
						ToCommand(player, count);
						break;
					case ItemType.Plugin:
						ToPlugin(player, count);
						break;
					case ItemType.Kit:
						ToKit(player, count);
						break;
				}
			}

			private void ToItem(BasePlayer player, int count)
			{
				if (Definition == null) return;

				var item = ItemManager.Create(Definition, Amount * count, Skin);
				if (item == null) return;

				if (Blueprint)
					item.blueprintTarget = Definition.itemid;

				if (Content != null)
					Content.Apply(item);

				if (Weapon != null)
					Weapon.Apply(item);

				if (ProhibitSplit)
				{
					// Splitting prohibition is not supported in this environment; skipping.
				}

				player.GiveItem(item);
			}

			public void ToItem(BasePlayer player, ItemContainer container)
			{
				if (Definition == null || container == null) return;

				var item = ItemManager.Create(Definition, Amount, Skin);
				if (item == null) return;

				if (Blueprint)
					item.blueprintTarget = Definition.itemid;

				if (Content != null)
					Content.Apply(item);

				if (Weapon != null)
					Weapon.Apply(item);

				item.MoveToContainer(container);
			}

			private void ToCommand(BasePlayer player, int count)
			{
				if (string.IsNullOrEmpty(Command) || _instance == null) return;

				var cmd = Command.Replace("%steamid%", player.UserIDString);
				for (var i = 0; i < count; i++)
					_instance.Server.Command(cmd);
			}

			private void ToPlugin(BasePlayer player, int count)
			{
				if (Plugin == null) return;
				Plugin.Get(player, count);
			}

			private void ToKit(BasePlayer player, int count)
			{
				if (string.IsNullOrEmpty(Kit) || _instance == null) return;

				for (var i = 0; i < count; i++)
					_instance.Server.Command($"kit give {player.userID} {Kit}");
			}

			public static AwardInfo CreateItem(string shortName, int amount = 1, ulong skin = 0)
			{
				return new AwardInfo
				{
					Type = ItemType.Item,
					ShortName = shortName,
					Amount = amount,
					Skin = skin
				};
			}

			public static AwardInfo CreateCommand(string command, int amount = 1, string image = "")
			{
				return new AwardInfo
				{
					Type = ItemType.Command,
					Command = command,
					Amount = amount,
					Image = image
				};
			}

			[JsonIgnore]
			public ItemDefinition Definition
			{
				get
				{
					if (_definition != null) return _definition;
					return _definition = ItemManager.FindItemDefinition(ShortName);
				}
			}

			[JsonIgnore]
			private ItemDefinition _definition;

			#endregion
		}

		private class PluginItem
		{
			[JsonProperty("Plugin name")]
			public string Plugin;

			[JsonProperty("Hook name")]
			public string Hook;

			[JsonProperty("Data")]
			public JObject Data;

			public void Get(BasePlayer player, int count)
			{
				if (string.IsNullOrEmpty(Plugin) || string.IsNullOrEmpty(Hook)) return;

				var plugin = _instance.plugins.Find(Plugin);
				if (plugin == null) return;

				for (var i = 0; i < count; i++)
				{
					try
					{
						plugin.Call(Hook, player, Data);
					}
					catch (System.Exception ex)
					{
						_instance.PrintError($"Error calling plugin {Plugin}.{Hook}: {ex.Message}");
					}
				}
			}
		}

		private class ItemContent
		{
			[JsonProperty("Items", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<AwardInfo> Items = new();

			public void Apply(Item item)
			{
				if (Items.Count == 0) return;

				var container = item.contents;
				if (container == null) return;

				foreach (var award in Items)
				{
					award.ToItem(null, container);
				}
			}
		}

		private class ItemWeapon
		{
			[JsonProperty("Ammo count")]
			public int Ammo;

			[JsonProperty("Ammo shortname")]
			public string AmmoShortName;

			public void Apply(Item item)
			{
				if (Ammo <= 0 || string.IsNullOrEmpty(AmmoShortName)) return;

				var weapon = item.GetHeldEntity() as BaseProjectile;
				if (weapon == null) return;

				var ammoType = ItemManager.FindItemDefinition(AmmoShortName);
				if (ammoType == null) return;

				weapon.primaryMagazine.ammoType = ammoType;
				weapon.primaryMagazine.contents = Ammo;
			}
		}

		private class PlayerData
		{
			#region Fields

			[JsonProperty(PropertyName = "Player ID")]
			public string PlayerID;

			[JsonProperty(PropertyName = "Last Take")]
			public DateTime LastTake;

			[JsonProperty(PropertyName = "Last Reset")]
			public DateTime LastReset;

			[JsonProperty(PropertyName = "Played Time")]
			public float PlayedTime;

			[JsonProperty(PropertyName = "Taked Days", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public HashSet<int> TakedDays = new();

			[JsonProperty(PropertyName = "Stored Items", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, int> StoredItems = new();

			#endregion

			#region Stored Items

			public void ResetRewards()
			{
				LastTake = default;
				TakedDays.Clear();
			}

			public List<KeyValuePair<string, AwardInfo>> GetStoredItems(int skip, int take)
			{
				var list = new List<KeyValuePair<string, AwardInfo>>();

				if (_instance == null) return list;

				var i = 0;
				foreach (var item in StoredItems)
				{
					if (!_instance.TryFindAwardByID(item.Value, out var awardInfo)) continue;

					if (i >= skip) list.Add(new KeyValuePair<string, AwardInfo>(item.Key, awardInfo));

					i++;

					if (list.Count >= take) break;
				}

				return list;
			}

			public int GetItemID(string guid)
			{
				return StoredItems.GetValueOrDefault(guid);
			}

			public void OnGiveItem(string guid)
			{
				StoredItems?.Remove(guid);
			}

			public void AddItemToStorage(AwardInfo award)
			{
				if (award == null) return;

				StoredItems?.TryAdd(CuiHelper.GetGuid(), award.ID);
			}

			#endregion

			#region Cooldown

			public bool HasCooldown()
			{
				var cd = GetCooldown();
				return cd > 0;
			}

			public float GetCooldown()
			{
				var cd = _config.Cooldown.GetCooldown(PlayerID);
				if (cd <= 0)
					return 0;

				return cd - PlayedTime;
			}

			public bool TryGetCooldown(out float value)
			{
				var cd = _config.Cooldown.GetCooldown(PlayerID);
				if (cd <= 0)
				{
					value = 0;
					return false;
				}

				value = cd - PlayedTime;
				return value > 0;
			}

			#endregion

			#region Take Day

			public void OnTake(int day)
			{
				LastTake = DateTime.UtcNow;
				TakedDays.Add(day);
				StartReset();
			}

			private void StartReset()
			{
				// Update reset anchor and reset played time counter
				LastReset = DateTime.UtcNow;
				PlayedTime = 0f;
			}

			public bool IsTaked(int day)
			{
				return TakedDays.Contains(day);
			}

			public bool CanTake(ulong player, int day)
			{
				if (IsTaked(day) || _instance == null) return false;

				var awards = _instance.GetAvailabeAwards(player);

				int nextDayToTake;
				if (TakedDays.Count == 0)
				{
					nextDayToTake = awards.Any() ? awards.Min(x => x.day) : 1;
				}
				else
				{
					var lastTakedDay = TakedDays.Append(0).Max();
					var availableAwards = awards.Where(x => x.day > lastTakedDay);
					nextDayToTake = availableAwards.Any() ? availableAwards.Min(x => x.day) : int.MaxValue;
				}

				return nextDayToTake == day && CanTakeNextDay(player);
			}

			#endregion

			#region Next Day

			public int GetTimeForNextDay(BasePlayer player)
			{
				if (_instance == null) return 0;

				var lastTakedDay = TakedDays.Count > 0 ? TakedDays.Max() : 0;

				var availableAwardDays = _instance.GetAvailabeAwards(player.userID).Select(x => x.day).Where(day => day > lastTakedDay).ToList();

				if (!availableAwardDays.Any()) return 0;

				var nextDayToTake = availableAwardDays.Min();

				var daysToAwait = Mathf.Max(nextDayToTake - lastTakedDay, 0);

				var minTimeToTake = LastTake.Date.AddDays(daysToAwait)
					.AddSeconds(_config.Reset.GetResetTime().TotalSeconds);

				return (int)minTimeToTake.Subtract(DateTime.UtcNow).TotalSeconds;
			}

			public bool HasNextDay(BasePlayer player)
			{
				if (_instance == null) return false;

				var lastTakedDay = TakedDays.Append(0).Max();
				var availableDays = _instance.GetAvailabeAwards(player.userID).Where(day => day.day > lastTakedDay);
				var nextDayToTake = availableDays.Any() ? availableDays.Min(day => day.day) : int.MaxValue;

				var daysToAwait = Mathf.Max(nextDayToTake - lastTakedDay, 0);

				return daysToAwait > 0;
			}

			public bool CanTakeNextDay(ulong player)
			{
				if (LastTake == default)
					return true;

				if (_instance == null)
					return false;

				var currentTime = ResetSettings.GetNowTime();

				var lastTakedDay = TakedDays.Count > 0 ? TakedDays.Max() : 0;

				var availableAwardDays = _instance.GetAvailabeAwards(player).Select(x => x.day).Where(day => day > lastTakedDay).ToList();
				if (availableAwardDays.Count == 0)
					return false;

				var nextDayToTake = availableAwardDays.Min();
				if (nextDayToTake <= 0)
					return false;

				var daysToAwait = Mathf.Max(nextDayToTake - lastTakedDay, 0);

				var minTimeToTake = LastTake.Date.AddDays(daysToAwait)
					.AddSeconds(_config.Reset.GetResetTime().TotalSeconds);

				return DateTime.UtcNow >= minTimeToTake;
			}

			#endregion

			#region Static

			private static readonly Dictionary<string, PlayerData> _playerData = new();

			public static PlayerData GetOrCreate(ulong playerID)
			{
				var playerIdString = playerID.ToString();
				if (_playerData.TryGetValue(playerIdString, out var data))
					return data;

				data = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<PlayerData>($"PlayerData/{playerIdString}") ?? new PlayerData { PlayerID = playerIdString };
				if (string.IsNullOrEmpty(data.PlayerID))
					data.PlayerID = playerIdString;
				_playerData[playerIdString] = data;

				return data;
			}

			public static PlayerData GetOrLoad(ulong playerID)
			{
				return GetOrCreate(playerID);
			}

			public static IEnumerable<PlayerData> GetAllPlayerData()
			{
				return _playerData.Values;
			}

			#endregion
		}

		#region Data Management
		private int _awardIDCounter = 0;
		private Dictionary<ulong, OpenedRewardPlayer> _openedRewardPlayers;
		private Dictionary<ulong, Timer> _updateTimes;

		private void LoadData()
		{
			// Load player data if needed
		}

		private void SaveData()
		{
			foreach (var data in PlayerData.GetAllPlayerData())
			{
				SaveData(data);
			}
		}

		private void SaveData(PlayerData data)
		{
			if (data == null) return;
			Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject($"PlayerData/{data.PlayerID}", data);
		}

		#endregion

		private class OpenedRewardPlayer
		{
			#region Fields

			public BasePlayer Player;

			public bool useMainUI;

			public List<(int day, AwardDayInfo dayInfo)> availableAwards;

			#endregion Fields

			public OpenedRewardPlayer(BasePlayer player, bool mainUI = true)
			{
				Player = player;
				useMainUI = mainUI;

				UpdateAwards();
			}

			#region Pages

			public int currentPage;

			public void OnChangePage(int page)
			{
				currentPage = page;

				if (_config.Cooldown.Enabled)
				{
					if (currentPage == updateAwardPage)
						CheckIfNeedUpdateAward();
					else
					{
						TryDestroyTimer();
					}
				}
			}

			#endregion

			#region Update

			public void UpdateAwards()
			{
				availableAwards = _instance.GetAvailabeAwards(Player.userID);
				CheckIfNeedUpdateAward();
			}

			public bool needUpdateAward = false;

			public (int day, AwardDayInfo dayInfo)? awardToUpdate = null;

			private int updateAwardPage = 0;

			private void CheckIfNeedUpdateAward()
			{
				if (!_config.Cooldown.Enabled || availableAwards == null) return;

				var data = PlayerData.GetOrCreate(Player.userID);
				if (data?.HasCooldown() != true) return;

				var targetAward = availableAwards.Find(award => data.CanTake(Player.userID, award.day));
				if (targetAward == default) return;

				awardToUpdate = targetAward;

				needUpdateAward = true;

				updateAwardPage = currentPage;

				TryDestroyTimer();

				SetUpdateTimer(_instance.timer.Every(1, CooldownUpdateAction));
			}

			#endregion

			#region Times

			private Timer GetUpdateTimer()
			{
				return _instance?._updateTimes?.TryGetValue(Player.userID, out var timer) == true ? timer : null;
			}

			public void TryDestroyTimer()
			{
				needUpdateAward = false;

				GetUpdateTimer()?.Destroy();
			}

			private void SetUpdateTimer(Timer timer)
			{
				_instance._updateTimes[Player.userID] = timer;
			}

			private void CooldownUpdateAction()
			{
				if (!needUpdateAward || awardToUpdate == null) return;

				var data = PlayerData.GetOrCreate(Player.userID);
				if (!data.CanTake(Player.userID, awardToUpdate.Value.day))
				{
					TryDestroyTimer();
					return;
				}

				// Update UI if needed
				if (useMainUI)
				{
					_instance?.UI_UpdateButton(Player, false);
				}
			}

			#endregion

			public UIConfig GetUI()
			{
				return new UIConfig
				{
					AwardsOnLine = 5,
					AwardWidth = 110.4545f,
					AwardIndentX = 13.3636f,
					UpIndent = 40f
				};
			}
		}

		private class UIConfig
		{
			public int AwardsOnLine = 5;
			public float AwardWidth = 110.4545f;
			public float AwardIndentX = 13.3636f;
			public float UpIndent = 40f;
		}

		private class Switchable
		{
			[JsonProperty("Enabled")]
			public bool Enabled = true;
		}

		private class ResetSettings
		{
			[JsonProperty("Reset Time")]
			public TimeSpan ResetTime = TimeSpan.FromHours(24);

			[JsonProperty("Reset Type")]
			public ResetType Type = ResetType.Daily;

			public TimeSpan GetResetTime()
			{
				return ResetTime;
			}

			public static DateTime GetNowTime()
			{
				return DateTime.UtcNow;
			}
		}

		private enum ItemType
		{
			Item,
			Command,
			Plugin,
			Kit
		}

		private enum ResetType
		{
			Daily,
			Weekly,
			Monthly
		}

		#endregion

		#region Helper Methods
		private OpenedRewardPlayer GetOrCreateOpenedRewardPlayer(BasePlayer player, bool mainUI = true)
		{
			if (!_openedRewardPlayers.TryGetValue(player.userID, out var openedRewardPlayer))
				_openedRewardPlayers.TryAdd(player.userID, openedRewardPlayer = new OpenedRewardPlayer(player, mainUI));

			return openedRewardPlayer;
		}

		private OpenedRewardPlayer GetOpenedRewardPlayer(ulong userID)
		{
			return _openedRewardPlayers.GetValueOrDefault(userID);
		}

		private bool IsOpenedRewardPlayer(ulong userID)
		{
			return _openedRewardPlayers.ContainsKey(userID);
		}

		private void RemoveOpenedRewardPlayer(BasePlayer player)
		{
			RemoveOpenedRewardPlayer(player.userID);
		}

		private void RemoveOpenedRewardPlayer(ulong userID)
		{
			GetOpenedRewardPlayer(userID)?.TryDestroyTimer();
			_openedRewardPlayers.Remove(userID);
		}

		private bool TryFindAwardByID(int id, out AwardInfo awardInfo)
		{
			return _awardsByID.TryGetValue(id, out awardInfo);
		}

		private List<(int day, AwardDayInfo dayInfo)> GetAvailabeAwards(ulong player, bool hasInConfig = false)
		{
			var list = new List<(int day, AwardDayInfo dayInfo)>();

			if (_config == null || _config.DailyAwards == null) return list;

			foreach (var award in _config.DailyAwards)
			{
				var dayInfo = award.Value;
				if (!dayInfo.Enabled && !hasInConfig) continue;

				// Day-level permission
				if (!string.IsNullOrEmpty(dayInfo.RequiredPermission))
				{
					if (_instance?.permission?.UserHasPermission(player.ToString(), dayInfo.RequiredPermission) != true)
						continue;
				}

				// Day-level played minutes requirement
				if (dayInfo.RequiredPlayedMinutes > 0)
				{
					var pdata = PlayerData.GetOrCreate(player);
					if (pdata == null || pdata.PlayedTime < dayInfo.RequiredPlayedMinutes * 60)
						continue;
				}

				var availableAwards = dayInfo.GetAvailableAwards(null, true);
				if (availableAwards.Count == 0) continue;

				list.Add((award.Key, dayInfo));
			}

			return list;
		}

		private string GetImage(string image)
		{
			if (string.IsNullOrEmpty(image)) return string.Empty;

			if (_enabledImageLibrary && ImageLibrary != null)
				return ImageLibrary.Call<string>("GetImage", image) ?? string.Empty;

			return image;
		}

		#endregion

		#region Core Methods
		private void Init()
		{
			_instance = this;
			_config = Config.ReadObject<Configuration>() ?? new Configuration();

			// Backfill missing sections
			if (_config.Texts == null) _config.Texts = new TextSettings();
			if (_config.Inventory == null) _config.Inventory = new InventorySettings();

			// If config exists but is effectively empty, build defaults and persist
			if (_config.DailyAwards == null || _config.DailyAwards.Count == 0)
			{
				_config.BuildDefault30DaysIfEmpty();
				Config.WriteObject(_config, true);
			}

			if (_config.Cooldown.Enabled)
				_updateTimes = new Dictionary<ulong, Timer>();

			_openedRewardPlayers = new Dictionary<ulong, OpenedRewardPlayer>();

			foreach (var award in _config.DailyAwards)
			{
				foreach (var awardInfo in award.Value.Awards)
				{
					awardInfo.ID = ++_awardIDCounter;
					_awardsByID[awardInfo.ID] = awardInfo;
				}
			}

			if (ImageLibrary != null)
				_enabledImageLibrary = ImageLibrary.IsLoaded;
		}

		protected override void LoadDefaultConfig()
		{
			_config = new Configuration();
			_config.BuildDefault30DaysIfEmpty();
			// Force write to disk synchronously
			Config.WriteObject(_config, true);
		}

		private void OnServerInitialized()
		{
			LoadData();
		}

		private void OnPlayerConnected(BasePlayer player)
		{
			var data = PlayerData.GetOrCreate(player.userID);
			data.PlayedTime = (float)(DateTime.UtcNow - data.LastReset).TotalSeconds;
		}

		private void OnPlayerDisconnected(BasePlayer player)
		{
			RemoveOpenedRewardPlayer(player);
			var data = PlayerData.GetOrCreate(player.userID);
			data.PlayedTime = (float)(DateTime.UtcNow - data.LastReset).TotalSeconds;
			SaveData(data);
		}

		private void Unload()
		{
			foreach (var openedPlayer in _openedRewardPlayers.Values)
			{
				openedPlayer.TryDestroyTimer();
			}

			if (_updateTimes != null)
			{
				foreach (var timer in _updateTimes.Values)
				{
					timer?.Destroy();
				}
			}

			SaveData();
		}

		#endregion

		[ConsoleCommand("dru.resetconfig")]
		private void CmdResetConfig(ConsoleSystem.Arg arg)
		{
			if (arg.Player() != null && !permission.UserHasPermission(arg.Player().UserIDString, "server.admin"))
				return;

			_config = new Configuration();
			_config.BuildDefault30DaysIfEmpty();
			Config.WriteObject(_config, true);
			Puts("DailyRewardsUI: Default config regenerated.");
		}

		[ConsoleCommand("mb.daily.page")]
		private void CmdAwardsPage(ConsoleSystem.Arg arg)
		{
			var player = arg.Player();
			if (player == null || !arg.HasArgs(1)) return;
			var page = arg.GetInt(0, -1);
			if (page < 0) return;

			var opened = GetOrCreateOpenedRewardPlayer(player);
			var totalAwards = opened?.availableAwards?.Count ?? 0;
			var perPage = opened?.GetUI()?.AwardsOnLine ?? 4;
			var totalPages = Mathf.CeilToInt((float)totalAwards / perPage);
			if (page >= totalPages) return;

			opened.OnChangePage(page);
			UI_DrawItems(player);
		}

	}
}
