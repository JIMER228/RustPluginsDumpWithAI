using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace Oxide.Plugins
{
	[Info("LShop", "Forum: https://topplugin.ru Ds: alone_sempai Vk: https://vk.com/rustnastroika", "1.0.5")]
	class LShop : RustPlugin
	{
		#region Classes

		private class DataCart
		{
			public Dictionary<ConfigData.ShopItemData, int> Items = new Dictionary<ConfigData.ShopItemData, int>();

			public void AddCartItem(ConfigData.ShopItemData item)
			{
				if (Items.TryGetValue(item, out _))
				{
					Items[item]++;
				}
				else
				{
					Items.Add(item, 1);
				}
			}
			public void RemoveCartItem(ConfigData.ShopItemData item)
			{
				Items.Remove(item);
			}

			public void ChangeAmountItem(ConfigData.ShopItemData item, int amount)
			{
				if (amount > 0)
				{
					Items[item] = amount;
				}
				else
				{
					Items.Remove(item);
				}
			}

			public int GetAmount()
			{
				return Items.Sum(x => x.Key.Amount * x.Value);
			}

			public double GetPrice()
			{
				return Items.Sum(x => x.Key.GetPrice() * x.Value);
			}

			public void ClearItems()
			{
				Items.Clear();
			}
		}
		#endregion

		#region Fields

		[PluginReference] private Plugin IQEconomic;

		internal enum ItemType : byte
		{
			Command = 0,
			Item = 1
		}

		private Dictionary<ulong, DataCart> _carts = new();
		private const string WHITE_TRANSPARENT_BACKGROUND = "1 1 1 0.3";
		private const string ORANGE_COLOR = "0.9490196 0.5019608 0.05490196 1";
		private const string BACKGROUND_COLOR = "0.3568628 0.3568628 0.3568628 0.75";
		private const string GRADIENTDOWN_COLOR = "0 0 0 0.7";
		private const string TEXT_COLOR = "1 1 1 1";

		private const string GREEN = "0.247 0.933 0.0 0.26";

		private const string RED_COLOR = "0.6901961 0.3490196 0.3490196 0.8";

		private Dictionary<int, ConfigData.ShopItemData> _shopItems = new();
		#endregion

		#region Hooks

		private void OnServerInitialized()
		{
			var images = new List<string>();

			foreach (var x in cfg.Shop)
			{
				if (!x.Value.Enabled)
					continue;

				foreach (var y in x.Value.Items)
				{
					_shopItems.Add(y.ID, y);

					if (!string.IsNullOrEmpty(y.Image))
						images.Add(y.Image);
				}
			}
			images.AddRange(cfg.VoltsShop.Where(x => !string.IsNullOrEmpty(x.Value.Image)).Select(x => x.Value.Image));

			GuiManager.LoadImages(images);
		}

		private void Unload()
		{
			GuiManager.Clear();
		}
		#endregion

		#region Methods

		private bool PurchaseIsBlockedFor(BasePlayer player, string shortname)
		{
			var tc = player.GetBuildingPrivilege();
			if (tc && !tc.IsAuthed(player))
				return true;

			var combatBlock = plugins.Find("CombatBlock");
			if (combatBlock)
				return (bool)combatBlock.Call("IsCombatBlocked", player);


			if (!string.IsNullOrEmpty(shortname))
			{
				var wipeBlock = plugins.Find("IQWipeBlock");
				if (wipeBlock)
					return (bool)wipeBlock.Call("IsItemBlock", player, shortname);
			}



			return false;
		}
		private void TryBuyItems(BasePlayer player)
		{
			if (!_carts.TryGetValue(player.userID, out var cart))
				return;

			var availableItems = cart.Items.Where(x => !PurchaseIsBlockedFor(player, x.Key.Shortname)).ToArray();

			var buyedSum = availableItems.Sum(x => x.Key.GetPrice() * x.Value);

			if (GetBalanceCoins(player) < buyedSum)
				return;

			IQEconomic?.Call("API_REMOVE_BALANCE", player.userID.Get(), (int)buyedSum);

			foreach (var x in availableItems)
			{
				if (string.IsNullOrEmpty(x.Key.Command))
					GiveItem(player, x.Key, x.Value);
				else
					Server.Command(x.Key.Command.Replace("%STEAMID%", player.UserIDString));
			}

			cart.ClearItems();

			EffectNetwork.Send(new("assets/prefabs/deployable/vendingmachine/effects/vending-machine-purchase-human.prefab", player, 0, new(), new()), player.Connection);

			UI_UpdateCart(player);
			UI_UpdateBuyButton(player);
			UI_DrawBalance(player, false);
		}

		private void GiveItem(BasePlayer player, ConfigData.ItemData itemData, int multiplierAmount)
		{
			if (multiplierAmount < 1)
				return;

			var item = itemData.Get(multiplierAmount);
			player.GiveItem(item);
		}

		private void AddAllItemsToConfig()
		{
			int i = 0;
			int subcategoryNum = 0;
			foreach (var x in ItemManager.itemList)
			{
				if (i % 10 == 0 && i != 0)
					subcategoryNum++;
				var category = x.category.ToString();

				if (!cfg.Shop.ContainsKey(category))
					cfg.Shop.Add(category, new()
					{
						Enabled = true,
						Items = new(),
						Localization = new()
						{
							Messages = new()
							{
								["en"] = category,
								["ru"] = category
							}
						}
					});

				cfg.Shop[category].Items.Add(new()
				{
					Shortname = x.shortname,
					SkinID = 0,
					DisplayName = null,
					Amount = 1,
					Image = null,
					Command = null,
					IsBlueprint = false,
					Subcategory = $"SUBCATEGORY {subcategoryNum}",
					Price = 50
				});

				i++;
			}
		}
		private ConfigData.ShopItemData FindItemById(int id)
		{
			ConfigData.ShopItemData item;
			return _shopItems.TryGetValue(id, out item) ? item : null;
		}
		private static class GuiManager
		{
			public static void Clear()
			{
				iconImageInfos.Clear();
				FailedLoad.Clear();
			}

			public static string Get(string key)
			{
				if (iconImageInfos.TryGetValue(key, out var id))
					return id.ToString();
				return "";
			}

			private static Dictionary<string, uint> iconImageInfos = new();

			private static List<string> FailedLoad = new();

			internal static void LoadImages(List<string> imageFiles)
			{
				foreach (var x in imageFiles)
				{
					if (iconImageInfos.ContainsKey(x))
						continue;
					iconImageInfos.Add(x, 0);
				}

				ServerMgr.Instance.StartCoroutine(LoadIconsCoroutine());
			}

			private static IEnumerator LoadIconsCoroutine()
			{
				for (int i = 0; i < iconImageInfos.Count; i++)
				{
					var imageInfo = iconImageInfos.ElementAtOrDefault(i);
					string url = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar +
								 "/LShop/Images/" +
								 imageInfo.Key + ".png";

					using (WWW www = new WWW(url))
					{
						yield return www;

						if (www.error != null)
						{
							FailedLoad.Add(imageInfo.Key);
						}
						else
						{
							var texture = www.texture;
							var imageId = FileStorage.server.Store(texture.EncodeToPNG(), FileStorage.Type.png,
								CommunityEntity.ServerInstance.net.ID);
							iconImageInfos[imageInfo.Key] = imageId;
							GameObject.DestroyImmediate(texture);
						}
					}
				}

				if (FailedLoad.IsNullOrEmpty())
					yield break;

				Debug.LogError($"\n\nFailed for loading {FailedLoad.Count} images in plugin LShop");
				Debug.LogError("__________________________________");
				for (int i = 0; i < FailedLoad.Count; i++)
					Debug.LogWarning($"[{i + 1}]" + $"{FailedLoad[i]}".PadLeft(31 - (i + 1 >= 10 ? 1 : 0), ' '));
				Debug.LogError("__________________________________\n\n");

			}
		}
		private string GetImage(string key)
		{
			return (string)MenuBase.Call("API_GetImage", key);
		}
		private int GetBalanceVolts(BasePlayer player)
		{
			return (int)IQEconomic.Call("API_GET_BALANCE", player.UserIDString);
		}

		private int GetBalanceCoins(BasePlayer player)
		{
#if DEBUGSERVER
			return 100000;
#endif
			if (!IQEconomic)
				return -1;
			return (int)IQEconomic.Call("API_GET_BALANCE", player.UserIDString);
		}
		#endregion

		#region UI

		[PluginReference] private Plugin Volts, TPEconomic, MenuBase;

		private const string Layer = "ui.MenuBase.bg";

		[ConsoleCommand("mb.shop.openvolts")]
		private void cmdOpenVoltsShop(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null)
				return;

			UI_DrawVoltsShop(arg.Player());
		}

		[ConsoleCommand("mb.shop.buy")]
		private void cmdBuy(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null)
				return;

			TryBuyItems(arg.Player());
		}

		[ConsoleCommand("mb.shop.removefromcart")]
		private void cmdRemoveFromCart(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null || arg.Args.IsNullOrEmpty())
				return;

			var player = arg.Player();

			if (!int.TryParse(arg.Args[0], out var id))
				return;

			EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);
			if (!_carts.ContainsKey(player.userID))
				_carts.Add(player.userID, new());

			_carts[player.userID].RemoveCartItem(FindItemById(id));
			// _carts[player.userID].AddCartItem(FindItemById(id), player);
			UI_UpdateCart(player);
			UI_UpdateBuyButton(player);
		}

		[ConsoleCommand("mb.shop.addtocart")]
		private void cmdAddToCart(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null || arg.Args.IsNullOrEmpty())
				return;

			var player = arg.Player();

			if (!int.TryParse(arg.Args[0], out var id))
				return;

			EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);
			if (!_carts.ContainsKey(player.userID))
				_carts.Add(player.userID, new());

			_carts[player.userID].AddCartItem(FindItemById(id));
			UI_UpdateCart(player);
			UI_UpdateBuyButton(player);
		}

		[ConsoleCommand("mb.shop")]
		private void cmdOpenShop(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null)
				return;

			UI_DrawMain(arg.Player());
		}

		[ConsoleCommand("mb.shop.donatevolts")]
		private void cmdDonateVolts(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null || arg.Args.IsNullOrEmpty())
				return;

			if (int.TryParse(arg.Args[0], out var amount))
				Volts.Call("DonateVolts", arg.Player().userID.Get(), amount);

			UI_UpdateTransferVoltsButton(arg.Player());
			UI_DrawBalance(arg.Player(), true);
		}

		[ConsoleCommand("mb.shop.category")]
		private void cmdCategory(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null || arg.Args.IsNullOrEmpty())
				return;

			string categoryID = arg.Args[0];


			EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", arg.Player(), 0, new Vector3(), new Vector3()), arg.Player().Connection);
			UI_DrawCategories(arg.Player(), categoryID);
			UI_DrawItems(arg.Player(), categoryID, 0);
		}

		[ConsoleCommand("mb.shop.buyforvolts")]
		private void cmdBuyForVolts(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null || arg.Args.IsNullOrEmpty())
				return;

			string key = arg.Args[0];
			if (!cfg.VoltsShop.TryGetValue(key, out var item))
				return;

			var player = arg.Player();

			var balance = GetBalanceVolts(player);
			if (balance < item.Price)
				return;

			IQEconomic?.Call("API_REMOVE_BALANCE", player.userID.Get(), (int)item.Price);

			if (string.IsNullOrEmpty(item.Command))
				player.GiveItem(item.Get(1));
			else
				Server.Command(item.Command.Replace("%STEAMID%", player.UserIDString));

			EffectNetwork.Send(new("assets/prefabs/deployable/vendingmachine/effects/vending-machine-purchase-human.prefab", player, 0, new(), new()), player.Connection);
			UI_DrawBalance(player, true);
		}

		[ConsoleCommand("mb.shop.backtodefaultshop")]
		private void cmdBackToDefaultShop(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null)
				return;

			UI_DrawMain(arg.Player());
		}

		private void UI_DrawVoltsShop(BasePlayer player)
		{
			UI_DrawBackground(player, true);
			UI_DrawBalance(player, true);
			CuiHelper.DestroyUi(player, Layer + ".main.div" + ".checkout.div");
			var container = new CuiElementContainer();
			container.Add(new CuiButton
			{
				Button = { Color = WHITE_TRANSPARENT_BACKGROUND, Command = "mb.shop.backtodefaultshop" },
				Text = { Text = "НАЗАД", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FontSize = 15 },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-302.51 160.261", OffsetMax = "-206.346 183.11" }
			}, Layer + ".main.div" + ".main.div.customshop", Layer + ".main.div" + ".main.div.customshop" + ".back.div");

			container.Add(new CuiElement
			{
				Name = Layer + ".main.div" + ".main.div.customshop" + ".back.div" + ".arrow",
				Parent = Layer + ".main.div" + ".main.div.customshop" + ".back.div",
				Components = {
								new CuiTextComponent { Text = "<", Font = "robotocondensed-regular.ttf", FontSize = 15, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
								new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-48.082 -11.424", OffsetMax = "-20 11.424" }
							}
			});

			container.Add(new CuiPanel
			{
				CursorEnabled = false,
				Image = { Color = WHITE_TRANSPARENT_BACKGROUND },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-202.782 160.266", OffsetMax = "304.173 183.114" }
			}, Layer + ".main.div" + ".main.div.customshop", Layer + ".main.div" + ".main.div.customshop" + ".label.div");

			container.Add(new CuiElement
			{
				Parent = Layer + ".main.div" + ".main.div.customshop" + ".label.div",
				Components = {
								new CuiTextComponent { Text = "VOLT'S", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
								new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-253.482 -11.424", OffsetMax = "150.384 11.424" }
							}
			});

			container.Add(new CuiPanel
			{
				CursorEnabled = false,
				Image = { Color = "1 1 1 0" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-302.508 28.152", OffsetMax = "304.172 157.2" }
			}, Layer + ".main.div" + ".main.div.customshop", Layer + ".main.div" + ".main.div.customshop" + ".transfertoproject.div");

			container.Add(new CuiElement()
			{
				Parent = Layer + ".main.div" + ".main.div.customshop" + ".transfertoproject.div",
				Components =
				{
					new CuiRawImageComponent() { Png = GetImage("banner_shop") },
					new CuiRectTransformComponent() { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-303.34 -43.121", OffsetMax = "303.34 64.524" }
				}
			});

			CuiHelper.AddUi(player, container);

			UI_DrawVoltsItems(player);
			UI_UpdateTransferVoltsButton(player);
		}

		[ChatCommand("tet")]
		private void cmdsADASD(BasePlayer player)
		{
			if (!player.IsAdmin)
				return;


			var item = cfg.VoltsShop.First();
			for (int i = 0; i < 200; i++)
			{
				if (i == 99)
				{
					cfg.VoltsShop.Add(Guid.NewGuid().ToString(), new ConfigData.VoltItemData
					{
						Shortname = "sulfur.ore",
						SkinID = 0,
						DisplayName = null,
						Amount = 1,
						Image = null,
						Command = null,
						IsBlueprint = false,
						ShopFormat = "end {0}",
						Price = 100
					});
					break;
				}
				cfg.VoltsShop.Add(Guid.NewGuid().ToString(), item.Value);
			}
		}
		private void UI_DrawVoltsItems(BasePlayer player)
		{
			var container = new CuiElementContainer();

			container.Add(new CuiElement()
			{
				Name = Layer + ".main.div" + ".main.div.customshop" + ".items.div",
				Parent = Layer + ".main.div" + ".main.div.customshop",
				Components =
				{
					new CuiScrollViewComponent
					{
						Vertical = true,
						Horizontal = false,
						MovementType = ScrollRect.MovementType.Unrestricted,
						Elasticity = 0,
						Inertia = false,
						DecelerationRate = 0,
						ScrollSensitivity = 20,
						ContentTransform = new()
						{
							AnchorMin = $"0 1",
							AnchorMax = "0 1",
							OffsetMin = $"0 0",
							OffsetMax = "0 0"
						},
						HorizontalScrollbar = null,
						VerticalScrollbar = null
					},
					new CuiRectTransformComponent()
					{
						AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "11.672 -440.37", OffsetMax = "619.172 -210.9"
					}
				}
			});

			container.Add(new CuiElement()
			{
				Parent = Layer + ".main.div" + ".main.div.customshop" + ".items.div",
				Components =
				{
					new CuiImageComponent() { Color = "0 0 0 0" },
					new CuiRectTransformComponent() { AnchorMin = $"0 0", AnchorMax = "1 1", OffsetMin = "0 -10000", OffsetMax = "1000 100"}
				}
			});

			float minx = -0.04997253f;
			float maxx = 95.37013f;
			float miny = -140.926f;
			float maxy = -0.2659607f;
			var balance = GetBalanceVolts(player);

			int i = 0;

			foreach (var x in cfg.VoltsShop)
			{
				if (i % 6 == 0 && i != 0)
				{
					minx = -0.04997253f;
					maxx = 95.37013f;
					miny -= 146.704f;
					maxy -= 146.704f;
				}

				container.Add(new CuiPanel
				{
					CursorEnabled = false,
					Image = { Color = "1 1 1 0" },
					RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{minx} {miny}", OffsetMax = $"{maxx} {maxy}" }
				}, Layer + ".main.div" + ".main.div.customshop" + ".items.div", Layer + ".main.div" + ".main.div.customshop" + ".items.div" + $".{x.Key}");

				container.Add(new CuiPanel
				{
					CursorEnabled = false,
					Image = { Color = WHITE_TRANSPARENT_BACKGROUND },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-47.71 -47.926", OffsetMax = "47.71 70.33" }
				}, Layer + ".main.div" + ".main.div.customshop" + ".items.div" + $".{x.Key}", Layer + ".main.div" + ".main.div.customshop" + ".items.div" + $".{x.Key}" + ".main");


				if (!string.IsNullOrEmpty(x.Value.Image))
					container.Add(new CuiElement()
					{
						Parent = Layer + ".main.div" + ".main.div.customshop" + ".items.div" + $".{x.Key}" + ".main",
						Components =
						{
							new CuiRawImageComponent() { Png = GuiManager.Get(x.Value.Image) },
							new CuiRectTransformComponent()
							{
								AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-43.324 -37.024",
								OffsetMax = "43.324 49.624"
							}
						}
					});
				else
					container.Add(new CuiPanel()
					{
						Image = { ItemId = ItemManager.FindItemDefinition(x.Value.Shortname).itemid, SkinId = x.Value.SkinID },
						RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-43.324 -37.024",
							OffsetMax = "43.324 49.624" }
					}, Layer + ".main.div" + ".main.div.customshop" + ".items.div" + $".{x.Key}" + ".main");

				container.Add(new CuiElement
				{
					Name = Layer + ".main.div" + ".main.div.customshop" + ".items.div" + $".{x.Key}" + ".main" + ".amount",
					Parent = Layer + ".main.div" + ".main.div.customshop" + ".items.div" + $".{x.Key}" + ".main",
					Components = {
									new CuiTextComponent { Text = string.Format(x.Value.ShopFormat, x.Value.Amount.ToString()), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.UpperCenter, Color = "0.847 0.847 0.847 1" },
									new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-47.71 -56.552", OffsetMax = "47.71 -34.448" }
								}
				});

				container.Add(new CuiElement
				{
					Name = Layer + ".main.div" + ".main.div.customshop" + ".items.div" + $".{x.Key}" + ".main" + ".cost",
					Parent = Layer + ".main.div" + ".main.div.customshop" + ".items.div" + $".{x.Key}" + ".main",
					Components = {
									new CuiTextComponent { Text = "<color=orange><size=14><b>ϟ</b></size></color>" + x.Value.Price.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 13, Align = TextAnchor.LowerCenter, Color = "1 1 1 1" },
									new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-47.71 -59.128", OffsetMax = "47.71 -37.024" }
								}
				});

				container.Add(new CuiButton
				{
					Button = { Color = balance >= x.Value.Price ? GREEN : RED_COLOR, Command = $"mb.shop.buyforvolts {x.Key}" },
					Text = { Text = balance >= x.Value.Price ? "КУПИТЬ" : "НЕДОСТАТОЧНО СРЕДСТВ", Font = "robotocondensed-regular.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-47.71 -70.33", OffsetMax = "47.71 -51.717" }
				}, Layer + ".main.div" + ".main.div.customshop" + ".items.div" + $".{x.Key}", Layer + ".main.div" + ".main.div.customshop" + ".items.div" + $".{x.Key}" + ".buy.btn");

				minx += 102.2f;
				maxx += 102.2f;
				i++;
			}

			foreach (var x in container[0].Components)
			{
				if (x is CuiScrollViewComponent sw)
				{
					sw.ContentTransform.OffsetMin = $"0 {Mathf.Clamp(miny, float.MinValue, -280)}";
				}
			}

			CuiHelper.AddUi(player, container);
		}

		private void UI_UpdateTransferVoltsButton(BasePlayer player)
		{
			var balance = GetBalanceVolts(player);
			var container = new CuiElementContainer();
			container.Add(new CuiButton
			{
				Button = { Color = balance <= 0 ? RED_COLOR : GREEN, Command = $"mb.shop.donatevolts {balance}" },
				Text = { Text = balance <= 0 ? "НЕДОСТАТОЧНО Volt's" : $"ПЕРЕДАТЬ {balance} <color=orange><size=14><b>ϟ</b></size></color>", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-303.34 -70.524", OffsetMax = "303.34 -46.107" }
			}, Layer + ".main.div" + ".main.div.customshop" + ".transfertoproject.div", Layer + ".main.div" + ".main.div.customshop" + ".transfertoproject.div" + ".transfer.btn", Layer + ".main.div" + ".main.div.customshop" + ".transfertoproject.div" + ".transfer.btn");
			CuiHelper.AddUi(player, container);
		}

		private void UI_DrawBackground(BasePlayer player, bool full)
		{
			var container = new CuiElementContainer();
			container.Add(new CuiPanel
			{
				CursorEnabled = false,
				Image = { Color = BACKGROUND_COLOR },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-314.69 {(full ? -229.23 : -156.781)}", OffsetMax = "314.69 229.232" }
			}, Layer + ".main.div", Layer + ".main.div" + ".main.div.customshop", Layer + ".main.div" + ".main.div.customshop");

			container.Add(new CuiElement
			{
				Name = Layer + ".main.div" + ".main.div.customshop" + ".label",
				Parent = Layer + ".main.div" + ".main.div.customshop",
				Components = {
					new CuiTextComponent { Text = "МИНИ-МАГАЗИН", Font = "robotocondensed-bold.ttf", FontSize = 27, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = full ? "-301.932 176.992" : "-301.932 140.767", OffsetMax = full ? "66.788 229.225" : "66.788 193" }
				}
			});
			container.Add(new CuiButton
			{
				Button = { Color = RED_COLOR, Close = Layer + ".blur" },
				Text = { Text = "X", Font = "permanentmarker.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = TEXT_COLOR },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "285.139 199.674", OffsetMax = "314.69 229.226" }
			}, Layer + ".main.div", Layer + ".main.div" + ".close");

			CuiHelper.AddUi(player, container);
		}
		private void UI_DrawMain(BasePlayer player)
		{
			UI_DrawBackground(player, false);

			UI_DrawBalance(player);

			var category = cfg.Shop.Where(x => x.Value.Enabled).FirstOrDefault();
			if (category.Value == null)
			{
				PrintError("No one enabled category finded!");
				return;
			}
			UI_UpdateCart(player);
			UI_UpdateBuyButton(player);
			UI_DrawCategories(player, category.Key);
			UI_DrawItems(player, category.Key);
		}

		private void UI_DrawBalance(BasePlayer player, bool fullbg = false)
		{
			var container = new CuiElementContainer();
			container.Add(new CuiPanel
			{
				Image = { Color = "1 1 1 0" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = fullbg ? "-99.223 193.026" : "-99.223 156.801", OffsetMax = fullbg ? "108.34 210.412" : "108.34 174.186" }
			}, Layer + ".main.div" + ".main.div.customshop", Layer + ".main.div" + ".main.div.customshop" + ".balances.div", Layer + ".main.div" + ".main.div.customshop" + ".balances.div");

			container.Add(new CuiPanel
			{
				Image = { Color = "0.6156863 0.6156863 0.6156863 0.5019608" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-103.782 -8.693", OffsetMax = "-5.253 8.693" }
			}, Layer + ".main.div" + ".main.div.customshop" + ".balances.div", Layer + ".main.div" + ".main.div.customshop" + ".balances.div" + ".coins.div");

			container.Add(new CuiElement()
			{
				Parent = Layer + ".main.div" + ".main.div.customshop" + ".balances.div" + ".coins.div",
				Components =
				{
					new CuiTextComponent() { Text = "\u274d", FontSize = 12, Align = TextAnchor.MiddleCenter},
					new CuiRectTransformComponent() { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-48.144 -7.544", OffsetMax = "-33.056 7.544" }
				}
			});
			// container.Add(new CuiElement()
			// {
			// 	Parent = Layer + ".main.div" + ".main.div.customshop" + ".balances.div" + ".coins.div",
			// 	Components =
			// 	{
			// 		new CuiRawImageComponent() { Png = GetImage("coins") },
			// 		new CuiRectTransformComponent() { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-48.144 -7.544", OffsetMax = "-33.056 7.544" }
			// 	}
			// });

			container.Add(new CuiElement
			{
				Parent = Layer + ".main.div" + ".main.div.customshop" + ".balances.div" + ".coins.div",
				Components = {
								new CuiTextComponent { Text = GetBalanceCoins(player).ToString(), Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
								new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-49.264 -8.693", OffsetMax = "49.265 8.694" }
							}
			});

			container.Add(new CuiButton
			{
				Button = { Color = "0.5254902 0.682353 0.4941177 1" },
				Text = { Text = "+", Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "31.877 -8.693", OffsetMax = "49.264 8.694" }
			}, Layer + ".main.div" + ".main.div.customshop" + ".balances.div" + ".coins.div");

			container.Add(new CuiPanel
			{
				Image = { Color = "0.6156863 0.6156863 0.6156863 0.5019608" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "5.253 -8.693", OffsetMax = "103.781 8.693" }
			}, Layer + ".main.div" + ".main.div.customshop" + ".balances.div", Layer + ".main.div" + ".main.div.customshop" + ".balances.div" + ".volts.div");

			container.Add(new CuiElement()
			{
				Parent = Layer + ".main.div" + ".main.div.customshop" + ".balances.div" + ".volts.div",
				Components =
				{
					new CuiTextComponent() { Text = "<color=orange><size=12><b>ϟ</b></size></color>", FontSize = 10, Align = TextAnchor.MiddleCenter},
					new CuiRectTransformComponent() { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-48.144 -7.544", OffsetMax = "-33.056 7.544" }
				}
			});

			container.Add(new CuiElement
			{
				Name = Layer + ".main.div" + ".main.div.customshop" + ".balances.div" + ".volts.div" + ".balance",
				Parent = Layer + ".main.div" + ".main.div.customshop" + ".balances.div" + ".volts.div",
				Components = {
								new CuiTextComponent { Text = GetBalanceVolts(player).ToString(), Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
								new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-49.264 -8.693", OffsetMax = "49.265 8.694" }
							}
			});

			container.Add(new CuiButton
			{
				Button = { Color = "0.5254902 0.682353 0.4941177 1" },
				Text = { Text = "+", Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "31.877 -8.693", OffsetMax = "49.264 8.694" }
			}, Layer + ".main.div" + ".main.div.customshop" + ".balances.div" + ".volts.div", Layer + ".main.div" + ".main.div.customshop" + ".balances.div" + ".volts.div" + ".addbalance.btn");
			CuiHelper.AddUi(player, container);
		}

		private void UI_DrawCategories(BasePlayer player, string activeCategoryID)
		{
			var container = new CuiElementContainer();
			container.Add(new CuiPanel
			{
				CursorEnabled = false,
				Image = { Color = "1 1 1 0" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-302.92 -188.523", OffsetMax = "-209.997 147.025" }
			}, Layer + ".main.div" + ".main.div.customshop", Layer + ".main.div" + ".main.div.customshop" + ".categories.div", Layer + ".main.div" + ".main.div.customshop" + ".categories.div");
			float minx = -46.46149f;
			float maxx = 46.46151f;
			float miny = 142.7876f;
			float maxy = 167.77f;
			foreach (var x in cfg.Shop.Where(x => x.Value.Enabled).Take(12))
			{
				bool isActive = activeCategoryID == x.Key;
				container.Add(new CuiPanel
				{
					CursorEnabled = false,
					Image = { Color = isActive ? GRADIENTDOWN_COLOR : "0 0 0 0" },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{minx} {miny}", OffsetMax = $"{maxx} {maxy}" }
				}, Layer + ".main.div" + ".main.div.customshop" + ".categories.div", Layer + ".main.div" + ".main.div.customshop" + ".categories.div" + $".{x.Key}");

				container.Add(new CuiButton
				{
					Button = { Color = isActive ? ORANGE_COLOR : WHITE_TRANSPARENT_BACKGROUND, Command = isActive ? "" : $"mb.shop.category {x.Key}" },
					Text = { Text = x.Value.Localization.GetMessage(player).ToUpper(), Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-46.461 -10.443", OffsetMax = "46.462 12.491" }
				}, Layer + ".main.div" + ".main.div.customshop" + ".categories.div" + $".{x.Key}");

				miny -= 26.189f;
				maxy -= 26.189f;
			}

			container.Add(new CuiElement()
			{
				Name = Layer + ".main.div" + ".main.div.customshop" + ".categories.div" + ".volts",
				Parent = Layer + ".main.div" + ".main.div.customshop" + ".categories.div",
				Components =
				{
					new CuiRawImageComponent() { Png = GetImage("volts_shop_btn") },
					new CuiRectTransformComponent() { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{minx} {miny}", OffsetMax = $"{maxx} {maxy}" }
				}
			});
			container.Add(new CuiButton()
			{
				Button = { Command = "mb.shop.openvolts", Color = "0 0 0.7 0.0" },
				Text = { Text = "" },
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
			}, Layer + ".main.div" + ".main.div.customshop" + ".categories.div" + ".volts");

			CuiHelper.AddUi(player, container);
		}

		private int GetRowsAmount(int capacity, int rowWidth)
		{
			int rows = 1;
			while (capacity > rowWidth)
			{
				capacity -= rowWidth;
				rows++;
			}
			return rows;
		}

		private int GetRows(IEnumerable<KeyValuePair<string, List<ConfigData.ShopItemData>>> items, int rowAmount)
		{
			int totalLines = items
				.Select(entry => (int)Math.Ceiling(entry.Value.Count / (double)rowAmount))
				.Sum();

			// int amount = 0;
			// foreach (var x in items)
			// {
			// 	amount += Mathf.Clamp(x.Value.Count, 7, int.MaxValue);
			// }
			//
			// return amount;
			return totalLines;
		}

		private void UI_DrawItems(BasePlayer player, string category, int page = 0)
		{
			if (!cfg.Shop.TryGetValue(category, out var categoryObject))
				return;
			var container = new CuiElementContainer();


			var items = categoryObject.GetItemsBySubcategory;

			container.Add(new CuiElement()
			{
				DestroyUi = Layer + ".main.div" + ".main.div.customshop" + ".items.div",
				Name = Layer + ".main.div" + ".main.div.customshop" + ".items.div",
				Parent = Layer + ".main.div" + ".main.div.customshop",
				Components =
				{
					new CuiScrollViewComponent
					{
						Vertical = true,
						Horizontal = false,
						MovementType = ScrollRect.MovementType.Clamped,
						Elasticity = 0,
						Inertia = false,
						DecelerationRate = 0,
						ScrollSensitivity = 20,
						ContentTransform = new()
						{
							AnchorMin = $"0 1",
							AnchorMax = "0 1",
							OffsetMin = $"0 0",
							OffsetMax = "0 0"
						},
						VerticalScrollbar = null
					},
					new CuiRectTransformComponent()
					{
						AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "110.5 -383.939", OffsetMax = "619.225 -46.549"
					}
				}
			});
			container.Add(new CuiPanel()
			{
				Image = { Color = "0 0 0 0" },
				RectTransform = { AnchorMin = $"0 0", AnchorMax = "1 1", OffsetMin = "0 -10000", OffsetMax = "1000 100" }
			}, Layer + ".main.div" + ".main.div.customshop" + ".items.div");

			float minx = -0.3599548f;
			float maxx = 68.80644f;
			float miny = -101.4161f;
			float maxy = -26.10797f;
			int i = 0;

			float offsetWhenNewCategory = 22.913f;
			float offsetCategoryNameFromItemTop = 3f;

			// Puts(items.Count);
			// Puts(anchorMinYScroll);
			int j = 0;
			foreach (var x in items)
			{
				container.Add(new CuiPanel
				{
					CursorEnabled = false,
					Image = { Color = WHITE_TRANSPARENT_BACKGROUND },
					RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"-0.365 {maxy + offsetCategoryNameFromItemTop}", OffsetMax = $"508.365 {maxy + offsetWhenNewCategory + (offsetCategoryNameFromItemTop)}" }
				}, Layer + ".main.div" + ".main.div.customshop" + ".items.div", Layer + ".main.div" + ".main.div.customshop" + ".items.div" + ".categoryname.div" + x.Key);

				container.Add(new CuiElement
				{
					Parent = Layer + ".main.div" + ".main.div.customshop" + ".items.div" + ".categoryname.div" + x.Key,
					Components = {
						new CuiTextComponent { Text = x.Key.Replace("_", " "), Font = "robotocondensed-bold.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
						new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-250.36 -11.459", OffsetMax = "254.368 11.46" }
					}
				});

				foreach (var y in x.Value)
				{
					if (i != 0 && i % 7 == 0)
					{
						minx = -0.3599472f;
						maxx = 68.80645f;
						miny -= 79.074f;
						maxy -= 79.074f;
					}
					container.Add(new CuiButton
					{
						Button = { Color = WHITE_TRANSPARENT_BACKGROUND, Command = $"mb.shop.addtocart {y.ID}" },
						RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{minx} {miny}", OffsetMax = $"{maxx} {maxy}" }
					}, Layer + ".main.div" + ".main.div.customshop" + ".items.div", Layer + ".main.div" + ".main.div.customshop" + ".items.div" + $".{i}");

					if (y.IsBlueprint)
					{
						container.Add(new CuiPanel()
						{
							Image = { ItemId = ItemManager.FindItemDefinition("blueprintbase").itemid },
							RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-20.762 -15.263", OffsetMax = "20.763 26.263" }
						});
					}

					container.Add(y.GetImage("0.5 0.5", "0.5 0.5", "-19.762 -14.263", "19.763 25.263",
						Layer + ".main.div" + ".main.div.customshop" + ".items.div" + $".{i}"));

					container.Add(new CuiElement
					{
						Parent = Layer + ".main.div" + ".main.div.customshop" + ".items.div" + $".{i}",
						Components = {
							new CuiTextComponent { Text = $"x{y.Amount}", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.UpperCenter, Color = "0.847 0.847 0.847 1" },
							new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-34.583 -37.653", OffsetMax = "34.583 -14.262" }
						}
					});

					container.Add(new CuiElement
					{
						Parent = Layer + ".main.div" + ".main.div.customshop" + ".items.div" + $".{i}",
						Components = {
							new CuiTextComponent { Text = y.GetPrice().ToString(), Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.LowerCenter, Color = "1 1 1 1" },
							new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-34.583 -37.653", OffsetMax = "34.583 -14.262" }
						}
					});


					minx += 73.277f;
					maxx += 73.277f;
					i++;
				}
				minx = -0.3599472f;
				maxx = 68.80645f;
				miny -= 78.074f;
				maxy -= 78.074f;

				miny -= offsetWhenNewCategory + offsetCategoryNameFromItemTop;
				maxy -= offsetWhenNewCategory + offsetCategoryNameFromItemTop;
				i = 0;
				j++;
			}

			foreach (var x in container[0].Components)
			{
				if (x is CuiScrollViewComponent sw)
				{
					sw.ContentTransform.OffsetMin = $"0 {Mathf.Clamp(miny + 81.074f, float.MinValue, -338.4f)}";
				}
			}
			CuiHelper.AddUi(player, container);
		}

		private void UI_UpdateBuyButton(BasePlayer player)
		{
			var cost = 0;
			if (_carts.TryGetValue(player.userID, out var cart))
			{
				cost = (int)cart.GetPrice();
			}

			if (cost <= 0)
			{
				CuiHelper.DestroyUi(player, Layer + ".main.div" + ".checkout.div" + ".buy.btn");
				return;
			}

			var balance = GetBalanceCoins(player);

			var container = new CuiElementContainer();
			container.Add(new CuiButton
			{
				Button = { Color = balance < cost ? RED_COLOR : GREEN, Command = "mb.shop.buy" },
				Text = { Text = balance < cost ? "НЕДОСТАТОЧНО СРЕДСТВ" : $"КУПИТЬ ВСЕ ЗА {cost}\u274d", Font = "robotocondensed-regular.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "135.45 -15.9", OffsetMax = "299.356 15.903" }
			}, Layer + ".main.div" + ".checkout.div", Layer + ".main.div" + ".checkout.div" + ".buy.btn", Layer + ".main.div" + ".checkout.div" + ".buy.btn");
			// container.Add(new CuiElement()
			// {
			// 	Parent = Layer + ".main.div" + ".checkout.div" + ".buy.btn",
			// 	Components = 
			// 	{ 
			// 		new CuiRawImageComponent()
			// 		{
			// 			Png		= GetImage("coins")
			// 		} ,
			// 		new CuiRectTransformComponent()
			// 		{
			// 			AnchorMin = "0.5 0.5",
			//                      AnchorMax = "0.5 0.5",
			//                      OffsetMin = "60 -9",
			//                      OffsetMax = "78 9"
			// 		}
			// 	}
			// });
			CuiHelper.AddUi(player, container);
		}

		private void UI_UpdateCart(BasePlayer player)
		{
			var container = new CuiElementContainer();

			bool hasCart = _carts.TryGetValue(player.userID, out var cart);

			container.Add(new CuiPanel
			{
				CursorEnabled = false,
				Image = { Color = BACKGROUND_COLOR },
				RectTransform =
				{
					AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-314.69 -229.23",
					OffsetMax = $"314.69 {(hasCart && !(cart?.Items).IsNullOrEmpty() ? -161.586 : -156.781)}"
				}
			}, Layer + ".main.div", Layer + ".main.div" + ".checkout.div", Layer + ".main.div" + ".checkout.div");
			CuiHelper.AddUi(player, container);
			container.Clear();
			if (!hasCart)
			{
				CuiHelper.DestroyUi(player, Layer + ".main.div" + ".checkout.div" + ".cart.div");
				return;
			}
			float anchorMaxXScroll = 1 + 0.156f * (cart.Items.Count - 6);
			anchorMaxXScroll = Mathf.Clamp(anchorMaxXScroll, 1, float.MaxValue);
			container.Add(new CuiElement()
			{
				Parent = Layer + ".main.div" + ".checkout.div",
				Name = Layer + ".main.div" + ".checkout.div" + ".cart.div",
				DestroyUi = Layer + ".main.div" + ".checkout.div" + ".cart.div",
				Components =
				{
					new CuiScrollViewComponent
					{
						Vertical = false,
						Horizontal = true,
						MovementType = ScrollRect.MovementType.Unrestricted,
						Elasticity = 0,
						Inertia = false,
						DecelerationRate = 0,
						ScrollSensitivity = 20,
						ContentTransform = new()
						{
							AnchorMin = "0 0",
							AnchorMax = $"{anchorMaxXScroll} 1"
						},
						HorizontalScrollbar = null,
						VerticalScrollbar = null
					},
					new CuiRectTransformComponent()
					{
						AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0.31 -67.822", OffsetMax = "419.193 -0.178"
					}
				}
			});
			container.Add(new CuiPanel()
			{
				Image = { Color = "0 0 0 0" },
				RectTransform =
				{
					AnchorMin = "0 0",
					AnchorMax = $"{anchorMaxXScroll} 1"
				}
			}, Layer + ".main.div" + ".checkout.div" + ".cart.div");

			float minx = 4.006546f;
			float maxx = 66.88765f;
			float miny = -61.62355f;
			float maxy = -6.280052f;

			int i = 0;

			foreach (var x in cart.Items)
			{
				container.Add(new CuiPanel
				{
					CursorEnabled = false,
					Image = { Color = GREEN },
					RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{minx} {miny}", OffsetMax = $"{maxx} {maxy}" }
				}, Layer + ".main.div" + ".checkout.div" + ".cart.div", Layer + ".main.div" + ".checkout.div" + ".cart.div" + $".{i}");

				container.Add(x.Key.GetImage("0.5 0.5", "0.5 0.5", "-19.163 -16.963", "19.163 21.363",
					Layer + ".main.div" + ".checkout.div" + ".cart.div" + $".{i}"));

				container.Add(new CuiElement
				{
					Parent = Layer + ".main.div" + ".checkout.div" + ".cart.div" + $".{i}",
					Components = {
						new CuiTextComponent { Text = $"x{x.Key.Amount * x.Value}", Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
						new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-31.44 -27.672", OffsetMax = "31.441 -14.124" }
					}
				});

				container.Add(new CuiButton
				{
					Button = { Color = "0 0 0 0", Command = $"mb.shop.removefromcart {x.Key.ID}" },
					Text = { Text = "X", Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "0.9058824 0.2039216 0.1294118 1" },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "20.598 15.088", OffsetMax = "30.762 25.252" }
				}, Layer + ".main.div" + ".checkout.div" + ".cart.div" + $".{i}");

				minx += 65.36f;
				maxx += 65.36f;
				i++;
			}
			CuiHelper.AddUi(player, container);
		}
		#endregion

		#region Commands

		[ConsoleCommand("lshop.fillitems")]
		private void cmdFillItems(ConsoleSystem.Arg arg)
		{
			if (arg.Player() != null)
				return;

			AddAllItemsToConfig();
			SaveConfig(cfg);
			PrintWarning("Config was updated!");
		}

		#endregion

		#region Config

		private ConfigData cfg;

		public class ConfigData
		{
			[JsonProperty("Магазин Volts")] public Dictionary<string, VoltItemData> VoltsShop;
			[JsonProperty("Обычный магазин")] public Dictionary<string, Category> Shop;

			internal interface IItem
			{
				public float GetPrice();
				public CuiElement GetImage(string aMin, string aMax, string oMin, string oMax, string parent,
					string name = null);

				public Item Get(int amount);
			}
			internal class ItemData : IItem
			{
				[JsonIgnore] private ICuiComponent _image;
				public CuiElement GetImage(string aMin, string aMax, string oMin, string oMax, string parent,
					string name = null)
				{
					if (_image == null)
					{
						if (!string.IsNullOrEmpty(Image))
							_image = new CuiRawImageComponent
							{
								Png = GuiManager.Get(Image)
							};
						else
						{
							var def = ItemManager.FindItemDefinition(Shortname);
							if (def == null)
							{
								Debug.LogError($"[LShop] Shortname {Shortname} is invalid!");
								return null;
							}
							_image = new CuiImageComponent
							{
								ItemId = ItemManager.FindItemDefinition(Shortname).itemid,
								SkinId = SkinID
							};
						}
					}

					return new CuiElement
					{
						Name = string.IsNullOrEmpty(name) ? CuiHelper.GetGuid() : name,
						Parent = parent,
						Components =
						{
							_image,
							new CuiRectTransformComponent
							{
								AnchorMin = aMin, AnchorMax = aMax,
								OffsetMin = oMin, OffsetMax = oMax
							}
						}
					};
				}

				public string Shortname;
				[JsonProperty("Скин")]
				public ulong SkinID;
				[JsonProperty("Отображаемое имя предмета (ост. пустым если не нужно)")]
				public string DisplayName;
				[JsonProperty("Кол-во предмета")]
				public int Amount;
				[JsonProperty("Изображение (ост. пустым если не нужно)")]
				public string Image;
				[JsonProperty("Команда (заменяет предмет, %STEAMID% - id игрока)")]
				public string Command;

				[JsonProperty("Это чертеж?")] public bool IsBlueprint;

				[JsonIgnore] private int _id = -1;

				[JsonIgnore]
				public int ID
				{
					get
					{
						if (_id == -1)
							_id = Core.Random.Range(0, int.MaxValue);

						return _id;
					}
				}

				[JsonIgnore]
				public ItemType Type => string.IsNullOrEmpty(Command) ? ItemType.Item : ItemType.Command;

				public float GetPrice() => -1;
				public Item Get(int multiplier = 1)
				{
					var item = ItemManager.CreateByName(Shortname, Amount * multiplier, SkinID);

					if (!string.IsNullOrEmpty(DisplayName))
						item.name = DisplayName;

					var held = item.GetHeldEntity();
					if (held)
						held.SendNetworkUpdate();

					return item;
				}
			}
			public class VoltItemData : ItemData
			{
				[JsonProperty("Формат для магазина ({0} - кол-во предмета)")]
				public string ShopFormat;
				[JsonProperty("Цена в вольтах")] public int Price;
				public new float GetPrice() => Price;
			}
			public class ShopItemData : ItemData
			{
				[JsonProperty("Подкатегория")]
				public string Subcategory;
				[JsonProperty("Цена предмета")] public float Price;
				public new float GetPrice() => Price;
			}

			internal class Category
			{
				[JsonProperty("Приоритет")] public int Order;
				[JsonProperty(PropertyName = "Включена?")]
				public bool Enabled;
				public List<ShopItemData> Items;


				[JsonProperty(PropertyName = "Локализация")]
				public Localization Localization;

				[JsonIgnore] public IEnumerable<KeyValuePair<string, List<ShopItemData>>> CachedItemsBySubcategory;

				[JsonIgnore] public int CachedSubcategories = int.MinValue;

				[JsonIgnore]
				public int SubcategoriesCount
				{
					get
					{
						if (CachedSubcategories != int.MinValue)
							return CachedSubcategories;

						List<string> categories = new();
						foreach (var x in GetItemsBySubcategory)
						{
							if (!categories.Contains(x.Key))
								categories.Add(x.Key);
						}

						CachedSubcategories = categories.Count;
						return CachedSubcategories;
					}
				}

				[JsonIgnore]
				public IEnumerable<KeyValuePair<string, List<ShopItemData>>> GetItemsBySubcategory
				{
					get
					{
						if (CachedItemsBySubcategory != null)
							return CachedItemsBySubcategory;
						var dict = new Dictionary<string, List<ShopItemData>>()
						{
							["OTHER"] = new()
						};
						foreach (var x in Items)
						{
							if (string.IsNullOrEmpty(x.Subcategory ?? ""))
							{
								dict["OTHER"].Add(x);
								continue;
							}
							if (!dict.ContainsKey(x.Subcategory ?? ""))
								dict.Add(x.Subcategory, new());

							dict[x.Subcategory].Add(x);
						}

						CachedItemsBySubcategory = dict.Where(x => !x.Value.IsNullOrEmpty());
						return GetItemsBySubcategory;
					}
				}

				public string GetTitle(BasePlayer player)
				{
					if (Localization != null)
						return Localization.GetMessage(player);

					return "LOCALIZATION_NOT_FOUND";
				}
			}
			internal class Localization
			{
				[JsonProperty(PropertyName = "Text (language - text)",
					ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public Dictionary<string, string> Messages = new Dictionary<string, string>();

				public string GetMessage(BasePlayer player = null)
				{
					if (Messages.Count == 0)
						throw new Exception("The use of localization is enabled, but there are no messages!");

					var userLang = "en";
					if (player != null) userLang = GetLibrary<Lang>().GetLanguage(player.UserIDString);

					string message;
					if (Messages.TryGetValue(userLang, out message))
						return message;

					if (Messages.TryGetValue("en", out message))
						return message;

					return Messages.ElementAt(0).Value;
				}
			}
		}

		protected override void LoadDefaultConfig()
		{
			var config = new ConfigData
			{
				VoltsShop = new()
				{
					["item1"] = new()
					{
						Shortname = "rifle.ak",
						SkinID = 0,
						DisplayName = "TESTRIFLEAK",
						Amount = 2,
						Image = null,
						ShopFormat = "АВТОМАТИКС",
						Command = null,
						Price = 100
					},
					["item2"] = new()
					{
						Shortname = "rifle.lr300",
						SkinID = 0,
						DisplayName = "TESTRIFLELR300",
						Amount = 1,
						Image = null,
						ShopFormat = "АВТОМАТИКС x{0}",
						Command = null,
						Price = 10000
					}
				},
				Shop = new()
			};
			SaveConfig(config);
		}

		protected override void LoadConfig()
		{
			base.LoadConfig();
			cfg = Config.ReadObject<ConfigData>();
			SaveConfig(cfg);

			cfg.Shop = cfg.Shop.OrderBy(x => x.Value.Order).ToDictionary(x => x.Key, x => x.Value);
		}

		private void SaveConfig(object config)
		{
			Config.WriteObject(config, true);
		}
		#endregion
	}
}