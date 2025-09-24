// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿// Requires: MenuBase

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using UnityEngine.UI;

namespace Oxide.Plugins
{
	[Info("MenuInfo", "Forum: https://topplugin.ru Ds: alone_sempai Vk: https://vk.com/rustnastroika", "1.0.5")]
	class MenuInfo : RustPlugin
	{
		#region Classes

		internal class FAQSection
		{
			public string Label;
			public string InsideText;
			public float PanelDownOffset;
		}
		internal class Command
		{
			public string Description;
			public string Text;
		}
		internal class HelpSection
		{
			public string TextOnButton;
			public int DrawOrder;

			public List<HelpSubSection> SubSections;

			internal class HelpSubSection
			{
				public string Label;
				public string InternalText;

				public float DownOffset;
			}
		}
		internal class CategoryButton
		{
			public string Text;
		}
		#endregion

		#region Fields
		
		private const string GRADIENT_RIGHT = "assets/content/ui/ui.background.transparent.linearltr.tga";
		private const string GRADIENTDOWN_COLOR = "0 0 0 0.7";
		
		private const string WHITE_TRANSPARENT_BACKGROUND = "1 1 1 0.3";
		private const string ORANGE_COLOR = "0.9490196 0.5019608 0.05490196 1";
		private const string BACKGROUND_COLOR = "0.3568628 0.3568628 0.3568628 0.75";

		private const string TEXT_COLOR = "1 1 1 1";

		private const string RED_COLOR = "0.6901961 0.3490196 0.3490196 0.8";
		
		private const string Layer = "ui.MenuBase.bg";

		private readonly Dictionary<string, CategoryButton> _categoryButtons = new()
		{
			["help"] = new()
			{
				Text = "ПОМОЩЬ",
			},
			["commands"] = new()
			{
				Text = "КОМАНДЫ"
			},
			["faq"] = new()
			{
				Text = "ЧАСТЫЕ ВОПРОСЫ"
			},
			["rules"] = new()
			{
				Text = "ПРАВИЛА"
			}
		};
		#endregion

		#region Hooks

		#endregion

		#region Methods

		#endregion

		#region UI

		#region Main
		private void UI_DrawMain(BasePlayer player)
		{
			var container = new CuiElementContainer();
			container.Add(new CuiElement
			{
				Name = Layer + ".main.div" + ".label",
				Parent = Layer + ".main.div",
				Components = {
					new CuiTextComponent { Text = "ИНФОРМАЦИЯ", Font = "robotocondensed-bold.ttf", FontSize = 27, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
					new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-301.932 176.992", OffsetMax = "66.788 229.225" }
				}
			});
			CuiHelper.AddUi(player, container);
			UI_UpdateBackground(player);
			UI_DrawSections(player, "help");
			UI_DrawHelp(player);
		}

		private void UI_UpdateBackground(BasePlayer player)
		{
			var container = new CuiElementContainer();
			container.Add(new CuiPanel
			{
				CursorEnabled = false,
				Image = { Color = "0.3568628 0.3568628 0.3568628 0" },
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
			}, Layer + ".main.div", Layer + ".main.div" + ".info", Layer + ".main.div" + ".info");
			container.Add(new CuiButton
			{
				Button = { Color = RED_COLOR, Close = "ui.MenuBase.bg.blur" },
				Text = { Text = "X", Font = "permanentmarker.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = TEXT_COLOR },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "285.139 199.674", OffsetMax = "314.69 229.226" }
			}, Layer + ".main.div" + ".info", Layer + ".main.div" + ".info" + ".close");
			CuiHelper.AddUi(player, container);
		}

		private void UI_DrawSections(BasePlayer player, string activeSection)
		{
			var container = new CuiElementContainer();
			container.Add(new CuiPanel
			{
				CursorEnabled = false,
				Image = { Color = "1 1 1 0" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-301.93 147.408", OffsetMax = "301.887 183.2" }
			}, Layer + ".main.div" + ".info", Layer + ".main.div" + ".categories.div", Layer + ".main.div" + ".categories.div");

			float minx = -301.9093f;
			float maxx = -155.0386f;  
			float miny = -17.89599f;
			float maxy = 17.89601f;
			
			foreach (var x in _categoryButtons)
			{
				container.Add(new CuiPanel
				{
					CursorEnabled = false,
					Image = { Color = activeSection == x.Key ? GRADIENTDOWN_COLOR : "0 0 0 0" },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{minx} {miny}", OffsetMax =  $"{maxx} {maxy}" }
				}, Layer + ".main.div" + ".categories.div", Layer + ".main.div" + ".categories.div" + $".{x.Key}");

				container.Add(new CuiButton
				{
					Button = { Color = activeSection == x.Key ? ORANGE_COLOR : WHITE_TRANSPARENT_BACKGROUND, Command = activeSection == x.Key ? "" : $"mb.info.category {x.Key}" },
					Text = { Text = x.Value.Text, Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-73.436 -14.867", OffsetMax = "73.434 17.976" }
				}, Layer + ".main.div" + ".categories.div" + $".{x.Key}");
				
				minx += 152.389f;
				maxx += 151.959f;
			}
			
			CuiHelper.AddUi(player, container);
		}
		#endregion

		#region Rules

		private void UI_DrawRules(BasePlayer player)
		{
			var container = new CuiElementContainer();
			container.Add(new CuiElement()
			{
				Name = Layer + ".main.div" + ".info" + ".rules.div",
				Parent = Layer + ".main.div" + ".info",
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
							AnchorMin = "0 1",
							AnchorMax = "0 1",
							OffsetMin = "0 0",
							OffsetMax = "0 0"
						},
						HorizontalScrollbar = null,
						VerticalScrollbar = new()
						{
							Invert = false,
							AutoHide = false,
							HandleSprite = null,
							Size = 2,
							HandleColor = ORANGE_COLOR,
							HighlightColor = ORANGE_COLOR,
							PressedColor = ORANGE_COLOR,
							TrackSprite = null,
							TrackColor = "0 0 0 0.4"
						}
					},
					new CuiRectTransformComponent()
					{
						AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0.37 -450.414", OffsetMax = "613.63 -100.416"
					}
				}
			});
			container.Add(new CuiPanel()
			{
				Image = { Color = "0 0 0 0" },
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-1000 -10000", OffsetMax = "1000 0"}
			}, Layer + ".main.div" + ".info" + ".rules.div");
			float miny = -27.76031f;
			float maxy = 0.4199829f;
			int i = 0;
			foreach (var x in cfg.RulesStrings)
			{
				int newLinesAmount = x.Split("\n").Length;
				container.Add(new CuiElement
				{
					Parent = Layer + ".main.div" + ".info" + ".rules.div",
					Components = {
						new CuiTextComponent { Text = x, Font = "robotocondensed-regular.ttf", FontSize = 13, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
						new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"11.37 {miny - 100}", OffsetMax = $"603.63 {maxy}" }
					}
				});
				
				
				
				miny -= 28.18f * Mathf.Max(1, newLinesAmount);
				maxy -= 28.18f * Mathf.Max(1, newLinesAmount);
			}

			(container[0].Components[0] as CuiScrollViewComponent).ContentTransform.OffsetMin = $"0 {Mathf.Min(miny, -350.4366f)}";
			if (miny > -350.4366)
			{
				(container[0].Components[0] as CuiScrollViewComponent).VerticalScrollbar = null;
			}
			CuiHelper.AddUi(player, container);
		}

		#endregion
		
		#region FAQ

		private void UI_DrawFAQ(BasePlayer player, string activeSection)
		{
			var container = new CuiElementContainer();
			container.Add(new CuiPanel
			{
				CursorEnabled = false,
				Image = { Color = "1 1 1 0" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-301.931 -229.23", OffsetMax = "301.889 140.422" }
			}, Layer + ".main.div" + ".info", Layer + ".main.div" + ".info" + ".Faqs.div", Layer + ".main.div" + ".info" + ".Faqs.div");

			container.Add(new CuiPanel
			{
				CursorEnabled = false,
				Image = { Color = "1 1 1 0" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-301.91 -184.828", OffsetMax = "301.91 184.832" }
			}, Layer + ".main.div" + ".info" + ".Faqs.div", Layer + ".main.div" + ".info" + ".Faqs.div" + ".items", Layer + ".main.div" + ".info" + ".Faqs.div" + ".items");

			float minx = -301.91f;
			float maxx = 295.91f; 
			float miny = 143.0026f;
			float maxy = 184.8301f;
			foreach (var x in cfg.FAQSections)
			{
				float additionalOffset = 0;
				container.Add(new CuiButton()
				{
					Button = { Color = WHITE_TRANSPARENT_BACKGROUND, Command = $"mb.info.faqsection {x.Key}{(activeSection == x.Key ? " close" : "")}" },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-301.91 {miny}", OffsetMax = $"301.91 {maxy}" }
				}, Layer + ".main.div" + ".info" + ".Faqs.div" + ".items", Layer + ".main.div" + ".info" + ".Faqs.div" + ".items" + $".{x.Key}");

				container.Add(new CuiPanel
				{
					CursorEnabled = false,
					Image = { Color = "1 1 1 1", Sprite = "assets/icons/connection.png" },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-290.7 -8.1", OffsetMax = "-274.5 8.1" }
				}, Layer + ".main.div" + ".info" + ".Faqs.div" + ".items" + $".{x.Key}", Layer + ".main.div" + ".info" + ".Faqs.div" + ".items" + $".{x.Key}" + ".icon");

				container.Add(new CuiElement
				{
					Name = Layer + ".main.div" + ".info" + ".Faqs.div" + ".items" + $".{x.Key}" + ".text",
					Parent = Layer + ".main.div" + ".info" + ".Faqs.div" + ".items" + $".{x.Key}",
					Components = {
						new CuiTextComponent { Text = x.Value.Label, Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
						new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-261.753 -20.914", OffsetMax = "258.587 20.914" }
					}
				});

				container.Add(new CuiButton
				{
					Button = { Color = "1 1 1 1", Sprite = activeSection == x.Key ? "assets/icons/dir_left.png" : "assets/icons/dir_right.png", Command = $"mb.info.faqsection {x.Key}{(activeSection == x.Key ? " close" : "")}"},
					Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "271.049 -10.251", OffsetMax = "291.551 10.252" }
				}, Layer + ".main.div" + ".info" + ".Faqs.div" + ".items" + $".{x.Key}", Layer + ".main.div" + ".info" + ".Faqs.div" + ".items" + $".{x.Key}" + ".open");

				if (activeSection == x.Key)
				{
					container.Add(new CuiPanel()
					{
						Image = { Color = GRADIENTDOWN_COLOR },
						RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-301.91 {x.Value.PanelDownOffset - 21}", OffsetMax = "301.91 -21"}
					}, Layer + ".main.div" + ".info" + ".Faqs.div" + ".items" + $".{x.Key}", Layer + ".main.div" + ".info" + ".Faqs.div" + ".items" + $".{x.Key}" + ".opened");

					container.Add(new CuiLabel()
					{
						Text = { Text = x.Value.InsideText, Align = TextAnchor.UpperLeft, FontSize = 11, Font = "robotocondensed-regular.ttf" },
						RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "10 12", OffsetMax = "-4 -12"}
					}, Layer + ".main.div" + ".info" + ".Faqs.div" + ".items" + $".{x.Key}" + ".opened");
					additionalOffset += -x.Value.PanelDownOffset;
				}
				
				miny -= 48.316f + additionalOffset;
				maxy -= 48.316f + additionalOffset;
			}
			
			CuiHelper.AddUi(player, container);
		}

		#endregion
		
		#region Commands

		private void UI_DrawCommands(BasePlayer player)
		{
			var container = new CuiElementContainer();
			container.Add(new CuiElement()
			{
				Name = Layer + ".main.div" + ".commands.div",
				Parent = Layer + ".main.div" + ".info",
				DestroyUi = Layer + ".main.div" + ".commands.div",
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
							AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "0 -1000", OffsetMax = "0 0"
						},
						HorizontalScrollbar = null,
						VerticalScrollbar = new()
						{
							Invert = false,
							AutoHide = false,
							HandleSprite = null,
							Size = 2,
							HandleColor = ORANGE_COLOR,
							HighlightColor = ORANGE_COLOR,
							PressedColor = ORANGE_COLOR,
							TrackSprite = null,
							TrackColor = "0 0 0 0.4"
						}
					},
					new CuiRectTransformComponent() { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-301.931 -209.23", OffsetMax = "312.889 134.609" }
				}
			});
			container.Add(new CuiPanel()
			{
				Image = { Color = "0 0 0 0" },
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-1000 -10000", OffsetMax = "1000 0"}
			}, Layer + ".main.div" + ".commands.div");
			// container.Add(new CuiPanel
			// {
			// 	CursorEnabled = false,
			// 	Image = { Color = "1 1 1 0" },
			// 	RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-301.931 -229.23", OffsetMax = "301.889 134.609" }
			// }, Layer + ".main.div" + ".info", Layer + ".main.div" + ".commands.div", Layer + ".main.div" + ".commands.div");
			float minx = -307.61f;
			float maxx = -307.61f; 
			// float miny = 165.71f;
			// float maxy = 165.71f;
			float miny = -20;
			float maxy = -20;
			
			float minyCommand = -30.23857f;
			float maxyCommand = 1.50607f;
			int i = 0;
			
			foreach (var x in cfg.Commands) 
			{
				container.Add(new CuiPanel
				{
					CursorEnabled = false,
					Image = { Color = "1 1 1 1" },
					RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = $"{minx} {miny}", OffsetMax =  $"{maxx} {maxy}" }
				}, Layer + ".main.div" + ".commands.div", Layer + ".main.div" + ".commands.div" + $".{i}");
				
				container.Add(new CuiElement
				{
					Name = Layer + ".main.div" + ".commands.div" + $".{i}" + ".label",
					Parent = Layer + ".main.div" + ".commands.div" + $".{i}",
					Components = {
						new CuiTextComponent { Text = x.Key, Font = "robotocondensed-bold.ttf", FontSize = 15, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
						new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-0.3 -1.371", OffsetMax = "603.52 20.21" }
					}
				});

				minyCommand = -30.23857f;
				maxyCommand = 1.50607f;
				float totalOffset = 0;
				int j = 0;
				
				foreach (var y in x.Value)
				{
					container.Add(new CuiPanel
					{
						CursorEnabled = false,
						Image = { Color = "1 1 1 0" },
						RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-0.075 {minyCommand}", OffsetMax = $"603.745 {maxyCommand}" }
					}, Layer + ".main.div" + ".commands.div" + $".{i}", Layer + ".main.div" + ".commands.div" + $".{i}" + ".commands" + $".{j}");

					container.Add(new CuiPanel
					{
						CursorEnabled = false,
						Image = { Color = "1 1 1 0.2" },
						RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-301.91 -15.366", OffsetMax = "149.175 15.366" }
					}, Layer + ".main.div" + ".commands.div" + $".{i}" + ".commands" + $".{j}", Layer + ".main.div" + ".commands.div" + $".{i}" + ".commands" + $".{j}" + ".text.bg");

					container.Add(new CuiElement
					{
						Parent = Layer + ".main.div" + ".commands.div" + $".{i}" + ".commands" + $".{j}" + ".text.bg",
						Components = {
										new CuiTextComponent { Text = y.Description, Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
										new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-218.743 -15.366", OffsetMax = "225.541 15.366" }
									}
					});

					container.Add(new CuiPanel
					{
						CursorEnabled = false,
						Image = { Color = "0.3568628 0.3568628 0.3568628 0.6" },
						RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "154.892 -15.366", OffsetMax = "301.91 15.366" }
					}, Layer + ".main.div" + ".commands.div" + $".{i}" + ".commands" + $".{j}", Layer + ".main.div" + ".commands.div" + $".{i}" + ".commands" + $".{j}" + ".bind.bg");

					container.Add(new CuiElement
					{
						Parent = Layer + ".main.div" + ".commands.div" + $".{i}" + ".commands" + $".{j}" + ".bind.bg",
						Components = {
										new CuiInputFieldComponent { Text = y.Text, Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", ReadOnly = true, Autofocus = false },
										new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-73.51 -15.366", OffsetMax = "73.51 15.366" }
									}
					});
					j++;
					minyCommand -= 34.573f;
					maxyCommand -= 34.573f;
					totalOffset -= 34.573f;
				}

				i++;
				miny += totalOffset - 30f;
				maxy += totalOffset - 30f;
			}

			(container[0].Components[0] as CuiScrollViewComponent).ContentTransform.OffsetMin =
				$"0 {Mathf.Min(-350.4366f, miny + 32)}";
			if (miny > -350.4366)
			{
				(container[0].Components[0] as CuiScrollViewComponent).VerticalScrollbar = null;
			}
			CuiHelper.AddUi(player, container);
		}

		#endregion
		
		#region Help
		
		private void UI_DrawHelp(BasePlayer player)
		{
			var startSection = cfg.HelpSections.First();
			UI_DrawHelpSections(player, startSection.Key);
			UI_DrawHelpPage(player, startSection.Key);
		}

		private void UI_DrawHelpSections(BasePlayer player, string activeSection)
		{
			var container = new CuiElementContainer();
			container.Add(new CuiPanel
			{
				CursorEnabled = false,
				Image = { Color = "1 1 1 0" },
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-301.93 -150.947", OffsetMax = "-177.878 140.3" }
			}, Layer + ".main.div" + ".info", Layer + ".main.div" + ".sections.div", Layer + ".main.div" + ".sections.div");

			float minx = -62.02518f;
			float maxx = 62.02482f; 
			float miny = 116.6622f;
			float maxy = 145.626f;
			foreach (var x in cfg.HelpSections) 
			{
				container.Add(new CuiPanel
				{
					CursorEnabled = false,
					Image = { Color = activeSection == x.Key ? GRADIENTDOWN_COLOR : "0 0 0 0" },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{minx} {miny}", OffsetMax =  $"{maxx} {maxy}" }
				}, Layer + ".main.div" + ".sections.div", Layer + ".main.div" + ".sections.div" + $".{x.Key}");

				container.Add(new CuiButton
				{
					Button = { Color = activeSection == x.Key ? ORANGE_COLOR : WHITE_TRANSPARENT_BACKGROUND, Command = activeSection == x.Key ? "" : $"mb.info.helpsection {x.Key}"},
					Text = { Text = x.Value.TextOnButton, Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-62.025 -12.388", OffsetMax = "62.025 14.482" }
				}, Layer + ".main.div" + ".sections.div" + $".{x.Key}");
				
				
				miny -= 28.964f;
				maxy -= 28.964f;
			}

			CuiHelper.AddUi(player, container);
		}

		private void UI_DrawHelpPage(BasePlayer player, string pageKey)
		{
			var container = new CuiElementContainer();
			container.Add(new CuiElement()
			{
				Name = Layer + ".main.div" + ".subsections.div",
				DestroyUi = Layer + ".main.div" + ".subsections.div",
				Parent = Layer + ".main.div" + ".info",
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
							AnchorMin = "0 1",
							AnchorMax = "0 1",
							OffsetMin = "35 -1000",
							OffsetMax = "0 0"
						},
						HorizontalScrollbar = null,
						VerticalScrollbar = new()
						{
							Invert = false,
							AutoHide = false,
							HandleSprite = null,
							Size = 2,
							HandleColor = ORANGE_COLOR,
							HighlightColor = ORANGE_COLOR,
							PressedColor = ORANGE_COLOR,
							TrackSprite = null,
							TrackColor = "0 0 0 0.4"
						}
					},
					new CuiRectTransformComponent()
					{
						AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-170.001 -409.23", OffsetMax = "308.889 -89.7"
					}
				}
			});
			container.Add(new CuiPanel()
			{
				Image = { Color = "0 0 0 0" },
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-1000 -10000", OffsetMax = "10000 0"}
			}, Layer + ".main.div" + ".subsections.div");


			float miny = 0;
			float maxy = 0;
			
			foreach (var x in cfg.HelpSections[pageKey].SubSections)
			{
				container.Add(new CuiPanel()
				{
					Image = { Color = "0 0 0 0" },
					RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = $"237 {miny}", OffsetMax = $"237 {maxy}"}
				}, Layer + ".main.div" + ".subsections.div", Layer + ".main.div" + ".subsections.div" + $".subsection.{x.Label}");
				container.Add(new CuiPanel
				{
					CursorEnabled = false,
					Image = { Color = "1 1 1 0.2" },
					RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-235.943 -16.285", OffsetMax = "235.948 -0.24" }
				}, Layer + ".main.div" + ".subsections.div" + $".subsection.{x.Label}", Layer + ".main.div" + ".subsections.div" + $".subsection.{x.Label}" + ".header.bg");

				container.Add(new CuiElement
				{
					Parent = Layer + ".main.div" + ".subsections.div" + $".subsection.{x.Label}" + ".header.bg",
					Components = {
						new CuiTextComponent { Text = x.Label, Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
						new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-234.813 -8.022", OffsetMax = "235.947 8.022" }
					}
				});

				container.Add(new CuiElement
				{
					Parent = Layer + ".main.div" + ".subsections.div" + $".subsection.{x.Label}",
					Components = {
						new CuiTextComponent { Text = x.InternalText, Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
						new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-233.867 -5200.872", OffsetMax = "233.867 -19.6" }
					}
				});
				miny -= x.DownOffset;
				maxy -= x.DownOffset;
			}
			
			(container[0].Components[0] as CuiScrollViewComponent).ContentTransform.OffsetMin = $"0 {Mathf.Min(miny, -319.4366f)}";
			if (miny > -319.4366)
			{
				(container[0].Components[0] as CuiScrollViewComponent).VerticalScrollbar = null;
			}
			CuiHelper.AddUi(player, container);
		}
		
		#endregion
		
		#endregion

		#region Commands

		[ConsoleCommand("mb.info.externalopen")]
		private void cmdExternalOpen(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null || arg.Args.IsNullOrEmpty())
				return;

			var key = arg.Args[0];

			UI_DrawMain(arg.Player());
			UI_UpdateBackground(arg.Player());
			
			if (cfg.HelpSections.ContainsKey(key))
			{
				UI_DrawSections(arg.Player(), "help");
				UI_DrawHelp(arg.Player());
				UI_DrawHelpSections(arg.Player(), key);
				UI_DrawHelpPage(arg.Player(), key);
			}
			else if (cfg.FAQSections.ContainsKey(key))
			{
				UI_DrawSections(arg.Player(), "faq");
				UI_DrawFAQ(arg.Player(), key);
			}
			else if (key == "commands")
			{
				UI_DrawSections(arg.Player(), "commands");
				UI_DrawCommands(arg.Player());
			}
			else if (key == "rules")
			{
				UI_DrawSections(arg.Player(), "rules");
				UI_DrawRules(arg.Player());
			}
			else
				throw new ArgumentOutOfRangeException(key);
		}
		
		[ConsoleCommand("mb.info.faqsection")]
		private void cmdFAQSection(ConsoleSystem.Arg arg)
		{
			if (arg.Player == null || arg.Args.IsNullOrEmpty())
				return;

			var player = arg.Player();

			if (arg.HasArgs(2))
			{
				UI_DrawFAQ(player, "");
				return;
			}
			UI_DrawFAQ(player, arg.Args[0]);
		}
		[ConsoleCommand("mb.info.open")]
		private void cmdOpen(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null)
				return;
			
			UI_DrawMain(arg.Player());
		}
		[ConsoleCommand("mb.info.category")]
		private void cmdCategory(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null || arg.Args.IsNullOrEmpty())
				return;

			UI_UpdateBackground(arg.Player());
			
			var player = arg.Player();
			var section = arg.Args[0];
			switch (section)
			{
				case "help":
					UI_DrawHelp(player);
					break;
				case "commands":
					UI_DrawCommands(player);
					break;
				case "rules":
					UI_DrawRules(player);
					break;
				case "faq":
					UI_DrawFAQ(player, "");
					break;
				default:
					throw new ArgumentOutOfRangeException(
						$"Player {player.userID} entered {nameof(cmdCategory)} but section {section} not found.");
			}
			UI_DrawSections(player, section);
		}

		[ConsoleCommand("mb.info.helpsection")]
		private void cmdHelpSection(ConsoleSystem.Arg arg)
		{
			if (arg.Player() == null || arg.Args.IsNullOrEmpty())
				return;
			var player = arg.Player();
			var section = arg.Args[0];
			
			UI_DrawHelpSections(player, section);
			UI_DrawHelpPage(player, section);
		}

		#endregion
		
		#region Config

		private ConfigData cfg;

		public class ConfigData
		{
			[JsonProperty("Разделы в информации")] public Dictionary<string, HelpSection> HelpSections;
			[JsonProperty("Разделы в командах")] public Dictionary<string, List<Command>> Commands;
			[JsonProperty("Разделы в FAQ")] public Dictionary<string, FAQSection> FAQSections;
			[JsonProperty("Строки правил")] public List<string> RulesStrings;
		}

		protected override void LoadDefaultConfig()
		{
			var config = new ConfigData
			{
				RulesStrings = new()
				{
					"124124124",
					"124124124",
					"12354213525",
					"12451235235",
					"5235235"
				},
				FAQSections = new()
				{
					["promocodes"] = new()
					{
						Label = "ГДЕ МОЖНО НАЙТИ ПРОМОКОДЫ?",
						InsideText = "Промокоды можно найти в наших социальных сетях. Там мы публикуем новости, акции и промокоды. Также, в закрытых чатах Discord, каждый вайп, мы выкладываем промокоды для бустеров сервера.",
						PanelDownOffset = -50
					},
					["shitpost"] = new()
					{
						Label = "Ну пиздец он долгий, когда инфо?",
						InsideText = "Да почти готово, ща правила доделаю и четенько",
						PanelDownOffset = -50
					}
				},
				Commands = new()
				{
					["ОСНОВНЫЕ"] = new()
					{
						new ()
						{
							Description = "Открыть раздел Menu",
							Text = "bind <key> menu"
						},
						new ()
						{
							Description = "Открыть раздел Menu #2",
							Text = "bind <key> menu test"
						},
					},
					["ДРУЗЬЯ"] = new()
					{
						new ()
						{
							Description = "Открыть раздел друзей",
							Text = "bind <key> menu"
						},
						new ()
						{
							Description = "Открыть раздел друзей #2",
							Text = "bind <key> menu test"
						},
					}
				},
				HelpSections = new()
				{
					["menu"] = new()
					{
						TextOnButton = "МЕНЮ",
						DrawOrder = 0,
						SubSections = new()
						{
							new()
							{
								Label = "ЧТО ЭТО ТАКОЕ?",
								InternalText = "Ивент \"Спутник\" - это главный ивент сервера, в котором можно получить ценный лут и Volt's молнии",
								DownOffset = 50
							},
							new()
							{
								Label = "ОПИСАНИЕ ИВЕНТА",
								InternalText = "Два раза в день (12:00 и 18:00) автоматически запускается событие. Во время его начала на экране появляется уведомление о падении обломков \"Спутника\". Через некоторое время в чате появляется информация о квадратах, в которых упали обломки. Также, на G-MAP отображаются точки, выделенные красным кругом."
							}
						}
					},
					["friends"] = new()
					{
						TextOnButton = "ДРУЗЬЯ",
						DrawOrder = 1,
						SubSections = new()
						{
							new()
							{
								Label = "Чё то про друзейВ",
								InternalText = "эх шарик я как и ты жил на цепи\nрубал хозяйские харчи",
								DownOffset = 70
							},
							new()
							{
								Label = "<color=red>оу</color>",
								InternalText = "Какие пиздатые оффсеты для тонкой настройки текста ммммм\n<size=5>руки бы разработчику поотрывать нахуй за эту дрочь</size>"
							}
						}
					}
				}
			};
			SaveConfig(config);
		}

		protected override void LoadConfig()
		{
			base.LoadConfig();
			cfg = Config.ReadObject<ConfigData>();
			SaveConfig(cfg);
		}

		private void SaveConfig(object config)
		{
			Config.WriteObject(config, true);
		}
		#endregion

		#region Data

		#endregion

		#region API

		#endregion
	}
}