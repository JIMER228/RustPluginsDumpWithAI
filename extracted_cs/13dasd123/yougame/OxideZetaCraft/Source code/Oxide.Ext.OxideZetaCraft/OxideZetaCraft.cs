// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using ConVar;
using Network;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Game.Rust.Libraries;
using Oxide.Plugins;
using UnityEngine;

namespace ZetaCraft.Oxide
{
	// Token: 0x02000011 RID: 17
	public class OxideZetaCraft : CSPlugin
	{
		// Token: 0x060000A3 RID: 163 RVA: 0x0000527C File Offset: 0x0000347C
		public OxideZetaCraft(OxideZetaCraftExtension oxideZetaCraftExtension_1)
		{
			base.Name = "ZetaCraft";
			base.Title = "ZetaCraft for Oxide";
			base.Author = "fermenspwnz(pososal bibu)";
			base.Version = Class5.versionNumber_0;
			base.HasConfig = true;
			this.oxideZetaCraftExtension_0 = oxideZetaCraftExtension_1;
			this.logger_0 = Interface.GetMod().RootLogger;
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x060000A4 RID: 164 RVA: 0x00005308 File Offset: 0x00003508
		public static Permission permission
		{
			get
			{
				return Interface.Oxide.GetLibrary<Permission>("Permission");
			}
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x060000A5 RID: 165 RVA: 0x00005324 File Offset: 0x00003524
		public static Lang lang
		{
			get
			{
				return Interface.Oxide.GetLibrary<Lang>("Lang");
			}
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x060000A6 RID: 166 RVA: 0x00005340 File Offset: 0x00003540
		private Plugin ImageLibrary
		{
			get
			{
				return Interface.Oxide.RootPluginManager.GetPlugin("ImageLibrary");
			}
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x00005364 File Offset: 0x00003564
		public string GetImage(string shortname, ulong skin = 0UL)
		{
			return (string)this.ImageLibrary.Call("GetImage", new object[]
			{
				shortname,
				skin
			});
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x0000539C File Offset: 0x0000359C
		public bool AddImage(string url, string shortname, ulong skin = 0UL)
		{
			Plugin imageLibrary = this.ImageLibrary;
			return (bool)((imageLibrary != null) ? imageLibrary.Call("AddImage", new object[]
			{
				url,
				shortname,
				skin
			}) : null);
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x000053DC File Offset: 0x000035DC
		protected override void LoadDefaultConfig()
		{
			this.class6_0 = OxideZetaCraft.Class6.smethod_0();
		}

		// Token: 0x060000AA RID: 170 RVA: 0x000053F4 File Offset: 0x000035F4
		protected override void LoadConfig()
		{
			base.LoadConfig();
			this.class6_0 = base.Config.ReadObject<OxideZetaCraft.Class6>(null);
		}

		// Token: 0x060000AB RID: 171 RVA: 0x0000541C File Offset: 0x0000361C
		protected override void SaveConfig()
		{
			base.Config.WriteObject<OxideZetaCraft.Class6>(this.class6_0, false, null);
		}

		// Token: 0x060000AC RID: 172 RVA: 0x0000543C File Offset: 0x0000363C
		private void method_0()
		{
			WebClient webClient = new WebClient();
			string text = webClient.DownloadString("http://api.ipify.org");
			string a = webClient.DownloadString(string.Format("https://pastebin.com/raw/bEfrYt3d", text, Server.port, base.Name));
			if (a != "1" || text == null)
			{
				Debug.LogError("Unpacked by Kaidoz");
				Interface.Oxide.UnloadPlugin(base.Name);
			}
			else
			{
				this.dateTime_0 = DateTime.Now;
			}
			new PluginTimers(this).Once(60f, new Action(this.method_16));
		}

		// Token: 0x060000AD RID: 173 RVA: 0x000054D8 File Offset: 0x000036D8
		[HookMethod("OnServerInitialized")]
		private void method_1()
		{
			this.method_0();
			this.LoadConfig();
			this.dictionary_1 = this.class6_0.dictionary_0;
			Interface.Oxide.GetLibrary<Command>(null).AddConsoleCommand("craft.menu", this, "consolecommand");
			Interface.Oxide.GetLibrary<Command>(null).AddConsoleCommand("craft.recipt", this, "createitem");
			Interface.Oxide.GetLibrary<Command>(null).AddConsoleCommand("craft.grenadeexit", this, "consolecommandexit");
			Interface.Oxide.GetLibrary<Command>(null).AddChatCommand("craft", this, "chatcommand");
			if (this.dictionary_1.Count<KeyValuePair<string, OxideZetaCraft.Class7>>() == 0)
			{
				this.dictionary_1.Add("multiplegrenadelauncher", new OxideZetaCraft.Class7
				{
					string_0 = "Многозарядный гранатомёт",
					int_0 = 1,
					list_0 = new List<OxideZetaCraft.Class8>
					{
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						}
					}
				});
				this.dictionary_1.Add("ammo.grenadelauncher.he", new OxideZetaCraft.Class7
				{
					string_0 = "40мм граната",
					int_0 = 50,
					list_0 = new List<OxideZetaCraft.Class8>
					{
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						}
					}
				});
				this.dictionary_1.Add("ammo.grenadelauncher.smoke", new OxideZetaCraft.Class7
				{
					string_0 = "40мм дымовая граната",
					int_0 = 50,
					list_0 = new List<OxideZetaCraft.Class8>
					{
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						}
					}
				});
				this.dictionary_1.Add("addgroup {steamid} vip 7d", new OxideZetaCraft.Class7
				{
					string_0 = "VIP на 7 дней",
					string_1 = "https://gamestores.pictures/images/2017/07/03/VIP_7.jpg",
					int_0 = 1,
					list_0 = new List<OxideZetaCraft.Class8>
					{
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "autoturret",
							int_0 = 2
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						}
					}
				});
				this.dictionary_1.Add("addgroup {steamid} vip 14d", new OxideZetaCraft.Class7
				{
					string_0 = "VIP на 14 дней",
					string_1 = "https://gamestores.pictures/images/2017/07/03/VIP_14.jpg",
					int_0 = 1,
					list_0 = new List<OxideZetaCraft.Class8>
					{
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "autoturret",
							int_0 = 2
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						}
					}
				});
				this.dictionary_1.Add("addgroup {steamid} vip 30d", new OxideZetaCraft.Class7
				{
					string_0 = "VIP на 30 дней",
					string_1 = "https://gamestores.pictures/images/2017/07/03/VIP_30.jpg",
					int_0 = 1,
					list_0 = new List<OxideZetaCraft.Class8>
					{
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "autoturret",
							int_0 = 2
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						}
					}
				});
				this.dictionary_1.Add("autoturret", new OxideZetaCraft.Class7
				{
					string_0 = "Автоматическая турель",
					int_0 = 1,
					list_0 = new List<OxideZetaCraft.Class8>
					{
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "autoturret",
							int_0 = 2
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						},
						new OxideZetaCraft.Class8
						{
							string_0 = "wood",
							int_0 = 1000
						}
					}
				});
				this.SaveConfig();
			}
			new PluginTimers(this).Once(60f, new Action(this.method_17));
		}

		// Token: 0x060000AE RID: 174 RVA: 0x00005B98 File Offset: 0x00003D98
		private void method_2()
		{
			if (this.ImageLibrary != null)
			{
				this.AddImage("https://files.facepunch.com/helkus/1b0411b1/mgl.png", "multiplegrenadelauncher", 0UL);
				this.AddImage("https://rustlabs.com/img/items180/ammo.grenadelauncher.he.png", "ammo.grenadelauncher.he", 0UL);
				this.AddImage("https://rustlabs.com/img/items180/ammo.grenadelauncher.smoke.png", "ammo.grenadelauncher.smoke", 0UL);
				List<string> list = new List<string>();
				foreach (KeyValuePair<string, OxideZetaCraft.Class7> keyValuePair in this.dictionary_1)
				{
					if (keyValuePair.Value.string_1 != null)
					{
						this.AddImage(keyValuePair.Value.string_1, keyValuePair.Value.string_1, 0UL);
					}
					foreach (OxideZetaCraft.Class8 @class in keyValuePair.Value.list_0)
					{
						if (!list.Contains(@class.string_0))
						{
							list.Add(@class.string_0);
						}
					}
				}
				using (List<string>.Enumerator enumerator3 = list.GetEnumerator())
				{
					while (enumerator3.MoveNext())
					{
						string shortname = enumerator3.Current;
						this.GetImage(shortname, 0UL);
					}
					return;
				}
			}
			new PluginTimers(this).Once(60f, new Action(this.method_18));
		}

		// Token: 0x060000AF RID: 175 RVA: 0x00005D54 File Offset: 0x00003F54
		[HookMethod("Unload")]
		private void method_3()
		{
			foreach (BasePlayer basePlayer_ in BasePlayer.activePlayerList)
			{
				this.method_4(basePlayer_);
			}
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x00005DA8 File Offset: 0x00003FA8
		private void method_4(BasePlayer basePlayer_0)
		{
			if (this.list_0.Contains(basePlayer_0.UserIDString))
			{
				this.list_0.Remove(basePlayer_0.UserIDString);
			}
			CuiHelper.DestroyUi(basePlayer_0, "crafteruigrenade");
			CuiHelper.DestroyUi(basePlayer_0, "crafteruigrenademain");
			CuiHelper.DestroyUi(basePlayer_0, "MiddlePanelcraft");
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x00005E00 File Offset: 0x00004000
		[HookMethod("consolecommandexit")]
		private void method_5(ConsoleSystem.Arg arg_0)
		{
			BasePlayer basePlayer = ArgEx.Player(arg_0);
			if (!(basePlayer == null) && (DateTime.Now - this.dateTime_0).TotalSeconds <= 120.0)
			{
				this.method_4(basePlayer);
			}
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x00005E4C File Offset: 0x0000404C
		[HookMethod("chatcommand")]
		private void method_6(BasePlayer basePlayer_0, string string_0)
		{
			if ((DateTime.Now - this.dateTime_0).TotalSeconds <= 120.0 && !(basePlayer_0 == null))
			{
				this.method_15(basePlayer_0, 0);
			}
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x00005E90 File Offset: 0x00004090
		[HookMethod("consolecommand")]
		private void method_7(ConsoleSystem.Arg arg_0)
		{
			BasePlayer basePlayer = ArgEx.Player(arg_0);
			if (!(basePlayer == null) && (DateTime.Now - this.dateTime_0).TotalSeconds <= 120.0)
			{
				int int_;
				if (!arg_0.HasArgs(1))
				{
					this.method_15(basePlayer, 0);
				}
				else if (!int.TryParse(arg_0.Args[0], out int_))
				{
					this.method_15(basePlayer, 0);
				}
				else
				{
					this.method_15(basePlayer, int_);
				}
			}
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x00005F0C File Offset: 0x0000410C
		private void method_8(BasePlayer basePlayer_0, string string_0, float float_0 = 3f)
		{
			OxideZetaCraft.Class10 @class = new OxideZetaCraft.Class10();
			@class.basePlayer_0 = basePlayer_0;
			Effect.server.Run("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", @class.basePlayer_0.transform.position, default(Vector3), null, false);
			if (this.dictionary_2.ContainsKey(@class.basePlayer_0.UserIDString))
			{
				Timer timer = this.dictionary_2[@class.basePlayer_0.UserIDString];
				new PluginTimers(this).Destroy(ref timer);
				this.dictionary_2.Remove(@class.basePlayer_0.UserIDString);
			}
			CuiHelper.DestroyUi(@class.basePlayer_0, "MiddlePanelcraft");
			CuiElementContainer cuiElementContainer = OxideZetaCraft.Class9.smethod_0("MiddlePanelcraft", "0.1 0.1 0.1 0.2", "0 0.6", "1 .7", false, "Overlay", "Assets/Icons/IconMaterial.mat", 0f);
			OxideZetaCraft.Class9.smethod_1(ref cuiElementContainer, "MiddlePanelcraft", "0.3 0.3 0.3 0.95", "0 0", "1 1", false, "Assets/Icons/IconMaterial.mat");
			OxideZetaCraft.Class9.smethod_5(ref cuiElementContainer, "MiddlePanelcraft", "1 1 1 0.5", string_0, 20, "0.05 0", "0.95 1", 4, "0 0 0 1", 0f);
			CuiHelper.AddUi(@class.basePlayer_0, cuiElementContainer);
			this.dictionary_2.Add(@class.basePlayer_0.UserIDString, new PluginTimers(this).Once(float_0, new Action(@class.method_0)));
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x00006060 File Offset: 0x00004260
		private ItemDefinition method_9(string string_0)
		{
			ItemDefinition itemDefinition = ItemManager.FindItemDefinition(string_0.ToLower());
			int num;
			if (itemDefinition == null && int.TryParse(string_0, out num))
			{
				itemDefinition = ItemManager.FindItemDefinition(num);
			}
			return itemDefinition;
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x00006098 File Offset: 0x00004298
		private bool method_10(PlayerInventory playerInventory_0, string string_0, int int_0)
		{
			List<Item> list = new List<Item>();
			if (this.method_9(string_0) != null)
			{
				list = playerInventory_0.FindItemIDs(this.method_9(string_0).itemid).ToList<Item>();
			}
			if (list.Count > 0)
			{
				int num = list.Sum(new Func<Item, int>(OxideZetaCraft.Class11.<>9.method_0));
				if (num >= int_0)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x00006118 File Offset: 0x00004318
		private void method_11(Item item_0, Vector3 vector3_0)
		{
			ItemContainer itemContainer = new ItemContainer();
			itemContainer.Insert(item_0);
			DropUtil.DropItems(itemContainer, vector3_0, 1f);
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x00006140 File Offset: 0x00004340
		public void Command(string command, params object[] args)
		{
			ConsoleSystem.Run(ConsoleSystem.Option.Server, command, args);
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x0000615C File Offset: 0x0000435C
		private void method_12(BasePlayer basePlayer_0, string string_0, int int_0)
		{
			ItemDefinition itemDefinition = this.method_9(string_0);
			if (itemDefinition != null)
			{
				Item item = ItemManager.Create(this.method_9(string_0), 1, 0UL);
				item.amount = int_0;
				if (!basePlayer_0.inventory.GiveItem(item, null))
				{
					this.method_11(item, basePlayer_0.transform.position);
				}
			}
			else
			{
				this.Command(string_0.Replace("{steamid}", basePlayer_0.UserIDString), Array.Empty<object>());
			}
		}

		// Token: 0x060000BA RID: 186 RVA: 0x000061DC File Offset: 0x000043DC
		private void method_13(PlayerInventory playerInventory_0, string string_0, int int_0)
		{
			List<Item> list = playerInventory_0.FindItemIDs(this.method_9(string_0).itemid).ToList<Item>();
			int num = 0;
			foreach (Item item in list)
			{
				int num2 = Mathf.Min(int_0 - num, item.amount);
				((item.amount > num2) ? item.SplitItem(num2) : item).DoRemove();
				num += num2;
				if (num >= int_0)
				{
					break;
				}
			}
		}

		// Token: 0x060000BB RID: 187 RVA: 0x00006278 File Offset: 0x00004478
		[HookMethod("createitem")]
		private void method_14(ConsoleSystem.Arg arg_0)
		{
			BasePlayer basePlayer = ArgEx.Player(arg_0);
			if (!(basePlayer == null) && arg_0.HasArgs(1) && (DateTime.Now - this.dateTime_0).TotalSeconds <= 120.0)
			{
				string text = string.Join(" ", arg_0.Args.Skip(0));
				if (this.dictionary_1.ContainsKey(text))
				{
					bool flag = true;
					Dictionary<string, int> dictionary = new Dictionary<string, int>();
					foreach (OxideZetaCraft.Class8 @class in this.dictionary_1[text].list_0)
					{
						if (dictionary.ContainsKey(@class.string_0))
						{
							Dictionary<string, int> dictionary2 = dictionary;
							string string_ = @class.string_0;
							dictionary2[string_] += @class.int_0;
						}
						else
						{
							dictionary.Add(@class.string_0, @class.int_0);
						}
					}
					foreach (KeyValuePair<string, int> keyValuePair in dictionary)
					{
						if (!this.method_10(basePlayer.inventory, keyValuePair.Key, keyValuePair.Value))
						{
							flag = false;
						}
					}
					if (flag)
					{
						foreach (OxideZetaCraft.Class8 class2 in this.dictionary_1[text].list_0)
						{
							this.method_13(basePlayer.inventory, class2.string_0, class2.int_0);
						}
						this.method_12(basePlayer, text, this.dictionary_1[text].int_0);
						this.method_8(basePlayer, "Рецепт " + this.dictionary_1[text].string_0 + " успешно скрафчен!", 1.5f);
						Effect.server.Run("assets/bundled/prefabs/fx/repairbench/itemrepair.prefab", basePlayer.transform.position, default(Vector3), null, false);
					}
					else
					{
						this.method_8(basePlayer, "У вас нет всех предметов нужных для крафта!", 1.5f);
					}
				}
				else
				{
					this.method_8(basePlayer, "Рецепт не найден!", 3f);
				}
			}
		}

		// Token: 0x060000BC RID: 188 RVA: 0x000064E4 File Offset: 0x000046E4
		private void method_15(BasePlayer basePlayer_0, int int_0 = 0)
		{
			if (!this.list_0.Contains(basePlayer_0.UserIDString))
			{
				this.list_0.Add(basePlayer_0.UserIDString);
				if (this.dictionary_0.ContainsKey(basePlayer_0.UserIDString))
				{
					int_0 = this.dictionary_0[basePlayer_0.UserIDString];
				}
				CuiHelper.DestroyUi(basePlayer_0, "crafteruigrenade");
				CuiElementContainer cuiElementContainer = OxideZetaCraft.Class9.smethod_0("crafteruigrenade", "0.005 0.005 0.005 0", "0 0", "1 1", true, "Overlay", "assets/content/ui/uibackgroundblur-ingamemenu.mat", 0f);
				CuiHelper.AddUi(basePlayer_0, cuiElementContainer);
			}
			else if (!this.dictionary_0.ContainsKey(basePlayer_0.UserIDString))
			{
				this.dictionary_0.Add(basePlayer_0.UserIDString, int_0);
			}
			else
			{
				this.dictionary_0[basePlayer_0.UserIDString] = int_0;
			}
			CuiHelper.DestroyUi(basePlayer_0, "crafteruigrenademain");
			CuiElementContainer cuiElementContainer2 = OxideZetaCraft.Class9.smethod_0("crafteruigrenademain", "0.005 0.005 0.005 0", "0 0", "1 1", true, "Overlay", "assets/content/ui/uibackgroundblur-ingamemenu.mat", 0f);
			OxideZetaCraft.Class9.smethod_3(ref cuiElementContainer2, "crafteruigrenademain", "0.005 0.005 0.005 0.9", "", 0, "0 0", "1 1", "craft.grenadeexit", 4, 0f, "assets/content/ui/uibackgroundblur-ingamemenu.mat");
			float num = 0.98f;
			float num2 = 0.1f;
			float num3 = 0f;
			int num4 = 0;
			foreach (KeyValuePair<string, OxideZetaCraft.Class7> keyValuePair in this.dictionary_1.Skip(int_0 * 6).Take((int_0 + 1) * 6))
			{
				num3 = num - 0.29f;
				OxideZetaCraft.Class9.smethod_1(ref cuiElementContainer2, "crafteruigrenademain", "0.3 0.3 0.3 0.4", num2 + " " + num3, num2 + 0.38f + " " + num, false, "Assets/Icons/IconMaterial.mat");
				OxideZetaCraft.Class9.smethod_3(ref cuiElementContainer2, "crafteruigrenademain", "0.5 0.5 0.7 0.4", "Скрафтить", 16, num2 + 0.3f + " " + (num - 0.05f), num2 + 0.38f + " " + (num - 0.01f), "craft.recipt " + keyValuePair.Key, 4, 0f, "assets/content/ui/uibackgroundblur-ingamemenu.mat");
				OxideZetaCraft.Class9.smethod_2(ref cuiElementContainer2, "crafteruigrenademain", "1 1 1 0.9", "<b>" + keyValuePair.Value.string_0 + "</b>", 19, num2 + " " + (num - 0.05f), num2 + 0.3f + " " + (num - 0.01f), 4, 0f, 0f);
				OxideZetaCraft.Class9.smethod_1(ref cuiElementContainer2, "crafteruigrenademain", "0.3 0.4 0.3 0.4", num2 + 0.15f + " " + (num - 0.18f), num2 + 0.22f + " " + (num - 0.07f), false, "Assets/Icons/IconMaterial.mat");
				OxideZetaCraft.Class9.smethod_4(ref cuiElementContainer2, "crafteruigrenademain", this.GetImage((keyValuePair.Value.string_1 == null) ? keyValuePair.Key : keyValuePair.Value.string_1, 0UL), num2 + 0.158f + " " + (num - 0.17f), num2 + 0.212f + " " + (num - 0.08f));
				OxideZetaCraft.Class9.smethod_2(ref cuiElementContainer2, "crafteruigrenademain", "1 1 1 0.5", "x" + keyValuePair.Value.int_0, 17, num2 + 0.18f + " " + (num - 0.18f), num2 + 0.22f - 0.001f + " " + (num - 0.07f), 8, 0f, 0f);
				float num5 = 0.04f;
				foreach (OxideZetaCraft.Class8 @class in keyValuePair.Value.list_0)
				{
					float num6 = num5 + 0.05f;
					OxideZetaCraft.Class9.smethod_1(ref cuiElementContainer2, "crafteruigrenademain", "0.3 0.3 0.3 0.4", num2 + num5 + " " + (num - 0.28f), num2 + num6 + " " + (num - 0.19f), false, "Assets/Icons/IconMaterial.mat");
					OxideZetaCraft.Class9.smethod_4(ref cuiElementContainer2, "crafteruigrenademain", this.GetImage(@class.string_0, 0UL), num2 + num5 + 0.005f + " " + (num - 0.27f), num2 + num6 - 0.005f + " " + (num - 0.2f));
					OxideZetaCraft.Class9.smethod_2(ref cuiElementContainer2, "crafteruigrenademain", "1 1 1 0.5", "x" + @class.int_0, 14, num2 + num5 + " " + (num - 0.28f), num2 + num6 - 0.001f + " " + (num - 0.19f), 8, 0f, 0f);
					num5 += num6 - num5 + 0.01f;
				}
				if (num4 == 5)
				{
					break;
				}
				if (num4 != 2)
				{
					num -= num - num3 + 0.01f;
				}
				else
				{
					num2 = 0.52f;
					num = 0.98f;
				}
				num4++;
			}
			if (int_0 > 0)
			{
				OxideZetaCraft.Class9.smethod_3(ref cuiElementContainer2, "crafteruigrenademain", "0.5 0.5 0.7 0.9", "<<", 16, "0 0.5", "0.02 0.6", string.Format("craft.menu {0}", int_0 - 1), 4, 0f, "assets/content/ui/uibackgroundblur-ingamemenu.mat");
			}
			if (this.dictionary_1.Count<KeyValuePair<string, OxideZetaCraft.Class7>>() > (int_0 + 1) * 6)
			{
				OxideZetaCraft.Class9.smethod_3(ref cuiElementContainer2, "crafteruigrenademain", "0.5 0.5 0.7 0.9", ">>", 16, "0.98 0.5", "1 0.6", string.Format("craft.menu {0}", int_0 + 1), 4, 0f, "assets/content/ui/uibackgroundblur-ingamemenu.mat");
			}
			CuiHelper.AddUi(basePlayer_0, cuiElementContainer2);
		}

		// Token: 0x060000BD RID: 189 RVA: 0x00006BC4 File Offset: 0x00004DC4
		[CompilerGenerated]
		private void method_16()
		{
			this.method_0();
		}

		// Token: 0x060000BE RID: 190 RVA: 0x00006BD8 File Offset: 0x00004DD8
		[CompilerGenerated]
		private void method_17()
		{
			this.method_2();
		}

		// Token: 0x060000BF RID: 191 RVA: 0x00006BD8 File Offset: 0x00004DD8
		[CompilerGenerated]
		private void method_18()
		{
			this.method_2();
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x00006BEC File Offset: 0x00004DEC
		static void smethod_0(Plugin plugin_0, string string_0)
		{
			plugin_0.Name = string_0;
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x00004CD0 File Offset: 0x00002ED0
		static OxideMod smethod_1()
		{
			return Interface.GetMod();
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x000051CC File Offset: 0x000033CC
		static CompoundLogger smethod_2(OxideMod oxideMod_0)
		{
			return oxideMod_0.RootLogger;
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x00006C00 File Offset: 0x00004E00
		static OxideMod smethod_3()
		{
			return Interface.Oxide;
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x00004D5C File Offset: 0x00002F5C
		static PluginManager smethod_4(OxideMod oxideMod_0)
		{
			return oxideMod_0.RootPluginManager;
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x00006C14 File Offset: 0x00004E14
		static Plugin smethod_5(PluginManager pluginManager_0, string string_0)
		{
			return pluginManager_0.GetPlugin(string_0);
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x00006C28 File Offset: 0x00004E28
		static object smethod_6(Plugin plugin_0, string string_0, object[] object_0)
		{
			return plugin_0.Call(string_0, object_0);
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x00006C40 File Offset: 0x00004E40
		static object smethod_7(Plugin plugin_0, string string_0, object[] object_0)
		{
			return plugin_0.Call(string_0, object_0);
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x00006C58 File Offset: 0x00004E58
		static DynamicConfigFile smethod_8(Plugin plugin_0)
		{
			return plugin_0.Config;
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x00006C6C File Offset: 0x00004E6C
		static WebClient smethod_9()
		{
			return new WebClient();
		}

		// Token: 0x060000CA RID: 202 RVA: 0x00006C80 File Offset: 0x00004E80
		static string smethod_10(WebClient webClient_0, string string_0)
		{
			return webClient_0.DownloadString(string_0);
		}

		// Token: 0x060000CB RID: 203 RVA: 0x00006C94 File Offset: 0x00004E94
		static string smethod_11(Plugin plugin_0)
		{
			return plugin_0.Name;
		}

		// Token: 0x060000CC RID: 204 RVA: 0x00006CA8 File Offset: 0x00004EA8
		static string smethod_12(string string_0, object object_0, object object_1, object object_2)
		{
			return string.Format(string_0, object_0, object_1, object_2);
		}

		// Token: 0x060000CD RID: 205 RVA: 0x00006CC0 File Offset: 0x00004EC0
		static bool smethod_13(string string_0, string string_1)
		{
			return string_0 != string_1;
		}

		// Token: 0x060000CE RID: 206 RVA: 0x00006CD4 File Offset: 0x00004ED4
		static void smethod_14(object object_0)
		{
			Debug.LogError(object_0);
		}

		// Token: 0x060000CF RID: 207 RVA: 0x00006CE8 File Offset: 0x00004EE8
		static bool smethod_15(OxideMod oxideMod_0, string string_0)
		{
			return oxideMod_0.UnloadPlugin(string_0);
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x00006CFC File Offset: 0x00004EFC
		static PluginTimers smethod_16(Plugin plugin_0)
		{
			return new PluginTimers(plugin_0);
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x00006D10 File Offset: 0x00004F10
		static Timer smethod_17(PluginTimers pluginTimers_0, float float_0, Action action_0)
		{
			return pluginTimers_0.Once(float_0, action_0);
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x00006D28 File Offset: 0x00004F28
		static void smethod_18(Command command_0, string string_0, Plugin plugin_0, string string_1)
		{
			command_0.AddConsoleCommand(string_0, plugin_0, string_1);
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x00006D40 File Offset: 0x00004F40
		static void smethod_19(Command command_0, string string_0, Plugin plugin_0, string string_1)
		{
			command_0.AddChatCommand(string_0, plugin_0, string_1);
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x00006D58 File Offset: 0x00004F58
		static bool smethod_20(BasePlayer basePlayer_0, string string_0)
		{
			return CuiHelper.DestroyUi(basePlayer_0, string_0);
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x00006D6C File Offset: 0x00004F6C
		static BasePlayer smethod_21(ConsoleSystem.Arg arg_0)
		{
			return ArgEx.Player(arg_0);
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x00006D80 File Offset: 0x00004F80
		static bool smethod_22(Object object_0, Object object_1)
		{
			return object_0 == object_1;
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x00006D94 File Offset: 0x00004F94
		static Transform smethod_23(Component component_0)
		{
			return component_0.transform;
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x00006DA8 File Offset: 0x00004FA8
		static Vector3 smethod_24(Transform transform_0)
		{
			return transform_0.position;
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x00006DBC File Offset: 0x00004FBC
		static void smethod_25(string string_0, Vector3 vector3_0, Vector3 vector3_1, Connection connection_0, bool bool_1)
		{
			Effect.server.Run(string_0, vector3_0, vector3_1, connection_0, bool_1);
		}

		// Token: 0x060000DA RID: 218 RVA: 0x00006DD4 File Offset: 0x00004FD4
		static void smethod_26(PluginTimers pluginTimers_0, ref Timer timer_0)
		{
			pluginTimers_0.Destroy(ref timer_0);
		}

		// Token: 0x060000DB RID: 219 RVA: 0x00006DE8 File Offset: 0x00004FE8
		static bool smethod_27(BasePlayer basePlayer_0, List<CuiElement> list_1)
		{
			return CuiHelper.AddUi(basePlayer_0, list_1);
		}

		// Token: 0x060000DC RID: 220 RVA: 0x00006DFC File Offset: 0x00004FFC
		static string smethod_28(string string_0)
		{
			return string_0.ToLower();
		}

		// Token: 0x060000DD RID: 221 RVA: 0x00006E10 File Offset: 0x00005010
		static ItemDefinition smethod_29(string string_0)
		{
			return ItemManager.FindItemDefinition(string_0);
		}

		// Token: 0x060000DE RID: 222 RVA: 0x00006E24 File Offset: 0x00005024
		static ItemDefinition smethod_30(int int_0)
		{
			return ItemManager.FindItemDefinition(int_0);
		}

		// Token: 0x060000DF RID: 223 RVA: 0x00006E38 File Offset: 0x00005038
		static bool smethod_31(Object object_0, Object object_1)
		{
			return object_0 != object_1;
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x00006E4C File Offset: 0x0000504C
		static List<Item> smethod_32(PlayerInventory playerInventory_0, int int_0)
		{
			return playerInventory_0.FindItemIDs(int_0);
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x00006E60 File Offset: 0x00005060
		static ItemContainer smethod_33()
		{
			return new ItemContainer();
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x00006E74 File Offset: 0x00005074
		static bool smethod_34(ItemContainer itemContainer_0, Item item_0)
		{
			return itemContainer_0.Insert(item_0);
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x00006E88 File Offset: 0x00005088
		static void smethod_35(ItemContainer itemContainer_0, Vector3 vector3_0, float float_0)
		{
			DropUtil.DropItems(itemContainer_0, vector3_0, float_0);
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x00006EA0 File Offset: 0x000050A0
		static string smethod_36(ConsoleSystem.Option option_0, string string_0, object[] object_0)
		{
			return ConsoleSystem.Run(option_0, string_0, object_0);
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x00006EB8 File Offset: 0x000050B8
		static Item smethod_37(ItemDefinition itemDefinition_0, int int_0, ulong ulong_0)
		{
			return ItemManager.Create(itemDefinition_0, int_0, ulong_0);
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x00006ED0 File Offset: 0x000050D0
		static bool smethod_38(PlayerInventory playerInventory_0, Item item_0, ItemContainer itemContainer_0)
		{
			return playerInventory_0.GiveItem(item_0, itemContainer_0);
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x00006EE8 File Offset: 0x000050E8
		static string smethod_39(string string_0, string string_1, string string_2)
		{
			return string_0.Replace(string_1, string_2);
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x00006F00 File Offset: 0x00005100
		static Item smethod_40(Item item_0, int int_0)
		{
			return item_0.SplitItem(int_0);
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x00006F14 File Offset: 0x00005114
		static void smethod_41(Item item_0)
		{
			item_0.DoRemove();
		}

		// Token: 0x060000EA RID: 234 RVA: 0x00006F28 File Offset: 0x00005128
		static bool smethod_42(ConsoleSystem.Arg arg_0, int int_0)
		{
			return arg_0.HasArgs(int_0);
		}

		// Token: 0x060000EB RID: 235 RVA: 0x00006F3C File Offset: 0x0000513C
		static string smethod_43(object object_0, object object_1, object object_2)
		{
			return object_0 + object_1 + object_2;
		}

		// Token: 0x060000EC RID: 236 RVA: 0x00002868 File Offset: 0x00000A68
		static string smethod_44(string string_0, string string_1)
		{
			return string_0 + string_1;
		}

		// Token: 0x060000ED RID: 237 RVA: 0x00006F54 File Offset: 0x00005154
		static string smethod_45(string string_0, string string_1, string string_2)
		{
			return string_0 + string_1 + string_2;
		}

		// Token: 0x060000EE RID: 238 RVA: 0x00006F6C File Offset: 0x0000516C
		static string smethod_46(object object_0, object object_1)
		{
			return object_0 + object_1;
		}

		// Token: 0x060000EF RID: 239 RVA: 0x00004F70 File Offset: 0x00003170
		static bool smethod_47(IEnumerator ienumerator_0)
		{
			return ienumerator_0.MoveNext();
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x00004F84 File Offset: 0x00003184
		static void smethod_48(IDisposable idisposable_0)
		{
			idisposable_0.Dispose();
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x00006F80 File Offset: 0x00005180
		static string smethod_49(string string_0, object object_0)
		{
			return string.Format(string_0, object_0);
		}

		// Token: 0x04000037 RID: 55
		private Logger logger_0;

		// Token: 0x04000038 RID: 56
		private OxideZetaCraftExtension oxideZetaCraftExtension_0;

		// Token: 0x04000039 RID: 57
		internal bool bool_0;

		// Token: 0x0400003A RID: 58
		private OxideZetaCraft.Class6 class6_0;

		// Token: 0x0400003B RID: 59
		private Dictionary<string, int> dictionary_0 = new Dictionary<string, int>();

		// Token: 0x0400003C RID: 60
		private List<string> list_0 = new List<string>();

		// Token: 0x0400003D RID: 61
		private Dictionary<string, OxideZetaCraft.Class7> dictionary_1 = new Dictionary<string, OxideZetaCraft.Class7>();

		// Token: 0x0400003E RID: 62
		private DateTime dateTime_0;

		// Token: 0x0400003F RID: 63
		private Dictionary<string, Timer> dictionary_2 = new Dictionary<string, Timer>();

		// Token: 0x02000012 RID: 18
		private class Class6
		{
			// Token: 0x060000F2 RID: 242 RVA: 0x00006F94 File Offset: 0x00005194
			public static OxideZetaCraft.Class6 smethod_0()
			{
				return new OxideZetaCraft.Class6
				{
					dictionary_0 = new Dictionary<string, OxideZetaCraft.Class7>()
				};
			}

			// Token: 0x04000040 RID: 64
			[JsonProperty("Список крафтов")]
			public Dictionary<string, OxideZetaCraft.Class7> dictionary_0;
		}

		// Token: 0x02000013 RID: 19
		private class Class7
		{
			// Token: 0x04000041 RID: 65
			[JsonProperty("name")]
			public string string_0;

			// Token: 0x04000042 RID: 66
			[JsonProperty("amount")]
			public int int_0;

			// Token: 0x04000043 RID: 67
			[JsonProperty("image")]
			public string string_1;

			// Token: 0x04000044 RID: 68
			[JsonProperty("items")]
			public List<OxideZetaCraft.Class8> list_0 = new List<OxideZetaCraft.Class8>();
		}

		// Token: 0x02000014 RID: 20
		private class Class8
		{
			// Token: 0x04000045 RID: 69
			[JsonProperty("name")]
			public string string_0;

			// Token: 0x04000046 RID: 70
			[JsonProperty("amount")]
			public int int_0;
		}

		// Token: 0x02000015 RID: 21
		private class Class9
		{
			// Token: 0x060000F6 RID: 246 RVA: 0x00006FD4 File Offset: 0x000051D4
			public static CuiElementContainer smethod_0(string string_0, string string_1, string string_2, string string_3, bool bool_0 = false, string string_4 = "Overlay", string string_5 = "assets/content/ui/uibackgroundblur-ingamemenu.mat", float float_0 = 0f)
			{
				CuiElementContainer cuiElementContainer = new CuiElementContainer();
				CuiPanel cuiPanel = new CuiPanel();
				cuiPanel.Image.Color = string_1;
				cuiPanel.Image.Material = string_5;
				cuiPanel.RectTransform.AnchorMin = string_2;
				cuiPanel.RectTransform.AnchorMax = string_3;
				cuiPanel.CursorEnabled = bool_0;
				cuiPanel.FadeOut = float_0;
				new CuiElement().Parent = string_4;
				cuiElementContainer.Add(cuiPanel, string_4, string_0);
				return cuiElementContainer;
			}

			// Token: 0x060000F7 RID: 247 RVA: 0x00007048 File Offset: 0x00005248
			public static void smethod_1(ref CuiElementContainer cuiElementContainer_0, string string_0, string string_1, string string_2, string string_3, bool bool_0 = false, string string_4 = "Assets/Icons/IconMaterial.mat")
			{
				cuiElementContainer_0.Add(new CuiPanel
				{
					Image = 
					{
						Color = string_1,
						Material = string_4
					},
					RectTransform = 
					{
						AnchorMin = string_2,
						AnchorMax = string_3
					},
					CursorEnabled = bool_0
				}, string_0, CuiHelper.GetGuid());
			}

			// Token: 0x060000F8 RID: 248 RVA: 0x000070A4 File Offset: 0x000052A4
			public static void smethod_2(ref CuiElementContainer cuiElementContainer_0, string string_0, string string_1, string string_2, int int_0, string string_3, string string_4, TextAnchor textAnchor_0 = 3, float float_0 = 0f, float float_1 = 0f)
			{
				cuiElementContainer_0.Add(new CuiLabel
				{
					Text = 
					{
						Color = string_1,
						FontSize = int_0,
						Align = textAnchor_0,
						FadeIn = float_0,
						Text = string_2
					},
					RectTransform = 
					{
						AnchorMin = string_3,
						AnchorMax = string_4
					},
					FadeOut = float_1
				}, string_0, CuiHelper.GetGuid());
			}

			// Token: 0x060000F9 RID: 249 RVA: 0x00007128 File Offset: 0x00005328
			public static void smethod_3(ref CuiElementContainer cuiElementContainer_0, string string_0, string string_1, string string_2, int int_0, string string_3, string string_4, string string_5, TextAnchor textAnchor_0 = 4, float float_0 = 0f, string string_6 = "assets/content/ui/uibackgroundblur-ingamemenu.mat")
			{
				cuiElementContainer_0.Add(new CuiButton
				{
					Button = 
					{
						Color = string_1,
						Command = string_5,
						FadeIn = float_0,
						Material = string_6
					},
					RectTransform = 
					{
						AnchorMin = string_3,
						AnchorMax = string_4
					},
					Text = 
					{
						Text = string_2,
						FontSize = int_0,
						Align = textAnchor_0
					}
				}, string_0, CuiHelper.GetGuid());
			}

			// Token: 0x060000FA RID: 250 RVA: 0x000071BC File Offset: 0x000053BC
			public static void smethod_4(ref CuiElementContainer cuiElementContainer_0, string string_0, string string_1, string string_2, string string_3)
			{
				cuiElementContainer_0.Add(new CuiElement
				{
					Name = CuiHelper.GetGuid(),
					Parent = string_0,
					Components = 
					{
						new CuiRawImageComponent
						{
							Png = string_1,
							Sprite = <Module>.smethod_25<string>(3761650295u)
						},
						new CuiRectTransformComponent
						{
							AnchorMin = string_2,
							AnchorMax = string_3
						}
					}
				});
			}

			// Token: 0x060000FB RID: 251 RVA: 0x00007230 File Offset: 0x00005430
			public static void smethod_5(ref CuiElementContainer cuiElementContainer_0, string string_0, string string_1, string string_2, int int_0, string string_3, string string_4, TextAnchor textAnchor_0 = 3, string string_5 = "0 0 0 1", float float_0 = 0f)
			{
				cuiElementContainer_0.Add(new CuiElement
				{
					Name = CuiHelper.GetGuid(),
					Parent = string_0,
					Components = 
					{
						new CuiTextComponent
						{
							Color = string_1,
							FontSize = int_0,
							Align = textAnchor_0,
							FadeIn = float_0,
							Text = string_2
						},
						new CuiOutlineComponent
						{
							Distance = <Module>.smethod_25<string>(2539282297u),
							Color = string_5
						},
						new CuiRectTransformComponent
						{
							AnchorMin = string_3,
							AnchorMax = string_4
						}
					}
				});
			}

			// Token: 0x060000FC RID: 252 RVA: 0x000072DC File Offset: 0x000054DC
			public static void smethod_6(ref CuiElementContainer cuiElementContainer_0, string string_0, string string_1, string string_2, int int_0, string string_3, string string_4, string string_5, int int_1 = 100, TextAnchor textAnchor_0 = 0)
			{
				cuiElementContainer_0.Add(new CuiElement
				{
					Name = CuiHelper.GetGuid(),
					Parent = string_0,
					Components = 
					{
						new CuiInputFieldComponent
						{
							Color = string_1,
							Text = string_2,
							FontSize = int_0,
							Command = string_5,
							CharsLimit = int_1,
							Align = textAnchor_0,
							IsPassword = false
						},
						new CuiRectTransformComponent
						{
							AnchorMin = string_3,
							AnchorMax = string_4
						}
					}
				});
			}

			// Token: 0x060000FE RID: 254 RVA: 0x0000736C File Offset: 0x0000556C
			static CuiElementContainer smethod_7()
			{
				return new CuiElementContainer();
			}

			// Token: 0x060000FF RID: 255 RVA: 0x00007380 File Offset: 0x00005580
			static CuiPanel smethod_8()
			{
				return new CuiPanel();
			}

			// Token: 0x06000100 RID: 256 RVA: 0x00007394 File Offset: 0x00005594
			static CuiImageComponent smethod_9(CuiPanel cuiPanel_0)
			{
				return cuiPanel_0.Image;
			}

			// Token: 0x06000101 RID: 257 RVA: 0x000073A8 File Offset: 0x000055A8
			static void smethod_10(CuiImageComponent cuiImageComponent_0, string string_0)
			{
				cuiImageComponent_0.Color = string_0;
			}

			// Token: 0x06000102 RID: 258 RVA: 0x000073BC File Offset: 0x000055BC
			static void smethod_11(CuiImageComponent cuiImageComponent_0, string string_0)
			{
				cuiImageComponent_0.Material = string_0;
			}

			// Token: 0x06000103 RID: 259 RVA: 0x000073D0 File Offset: 0x000055D0
			static CuiRectTransformComponent smethod_12(CuiPanel cuiPanel_0)
			{
				return cuiPanel_0.RectTransform;
			}

			// Token: 0x06000104 RID: 260 RVA: 0x000073E4 File Offset: 0x000055E4
			static void smethod_13(CuiRectTransformComponent cuiRectTransformComponent_0, string string_0)
			{
				cuiRectTransformComponent_0.AnchorMin = string_0;
			}

			// Token: 0x06000105 RID: 261 RVA: 0x000073F8 File Offset: 0x000055F8
			static void smethod_14(CuiRectTransformComponent cuiRectTransformComponent_0, string string_0)
			{
				cuiRectTransformComponent_0.AnchorMax = string_0;
			}

			// Token: 0x06000106 RID: 262 RVA: 0x0000740C File Offset: 0x0000560C
			static void smethod_15(CuiPanel cuiPanel_0, bool bool_0)
			{
				cuiPanel_0.CursorEnabled = bool_0;
			}

			// Token: 0x06000107 RID: 263 RVA: 0x00007420 File Offset: 0x00005620
			static void smethod_16(CuiPanel cuiPanel_0, float float_0)
			{
				cuiPanel_0.FadeOut = float_0;
			}

			// Token: 0x06000108 RID: 264 RVA: 0x00007434 File Offset: 0x00005634
			static CuiElement smethod_17()
			{
				return new CuiElement();
			}

			// Token: 0x06000109 RID: 265 RVA: 0x00007448 File Offset: 0x00005648
			static void smethod_18(CuiElement cuiElement_0, string string_0)
			{
				cuiElement_0.Parent = string_0;
			}

			// Token: 0x0600010A RID: 266 RVA: 0x0000745C File Offset: 0x0000565C
			static string smethod_19(CuiElementContainer cuiElementContainer_0, CuiPanel cuiPanel_0, string string_0, string string_1)
			{
				return cuiElementContainer_0.Add(cuiPanel_0, string_0, string_1);
			}

			// Token: 0x0600010B RID: 267 RVA: 0x00007474 File Offset: 0x00005674
			static string smethod_20()
			{
				return CuiHelper.GetGuid();
			}

			// Token: 0x0600010C RID: 268 RVA: 0x00007488 File Offset: 0x00005688
			static CuiLabel smethod_21()
			{
				return new CuiLabel();
			}

			// Token: 0x0600010D RID: 269 RVA: 0x0000749C File Offset: 0x0000569C
			static CuiTextComponent smethod_22(CuiLabel cuiLabel_0)
			{
				return cuiLabel_0.Text;
			}

			// Token: 0x0600010E RID: 270 RVA: 0x000074B0 File Offset: 0x000056B0
			static void smethod_23(CuiTextComponent cuiTextComponent_0, string string_0)
			{
				cuiTextComponent_0.Color = string_0;
			}

			// Token: 0x0600010F RID: 271 RVA: 0x000074C4 File Offset: 0x000056C4
			static void smethod_24(CuiTextComponent cuiTextComponent_0, int int_0)
			{
				cuiTextComponent_0.FontSize = int_0;
			}

			// Token: 0x06000110 RID: 272 RVA: 0x000074D8 File Offset: 0x000056D8
			static void smethod_25(CuiTextComponent cuiTextComponent_0, TextAnchor textAnchor_0)
			{
				cuiTextComponent_0.Align = textAnchor_0;
			}

			// Token: 0x06000111 RID: 273 RVA: 0x000074EC File Offset: 0x000056EC
			static void smethod_26(CuiTextComponent cuiTextComponent_0, float float_0)
			{
				cuiTextComponent_0.FadeIn = float_0;
			}

			// Token: 0x06000112 RID: 274 RVA: 0x00007500 File Offset: 0x00005700
			static void smethod_27(CuiTextComponent cuiTextComponent_0, string string_0)
			{
				cuiTextComponent_0.Text = string_0;
			}

			// Token: 0x06000113 RID: 275 RVA: 0x00007514 File Offset: 0x00005714
			static CuiRectTransformComponent smethod_28(CuiLabel cuiLabel_0)
			{
				return cuiLabel_0.RectTransform;
			}

			// Token: 0x06000114 RID: 276 RVA: 0x00007528 File Offset: 0x00005728
			static void smethod_29(CuiLabel cuiLabel_0, float float_0)
			{
				cuiLabel_0.FadeOut = float_0;
			}

			// Token: 0x06000115 RID: 277 RVA: 0x0000753C File Offset: 0x0000573C
			static string smethod_30(CuiElementContainer cuiElementContainer_0, CuiLabel cuiLabel_0, string string_0, string string_1)
			{
				return cuiElementContainer_0.Add(cuiLabel_0, string_0, string_1);
			}

			// Token: 0x06000116 RID: 278 RVA: 0x00007554 File Offset: 0x00005754
			static CuiButton smethod_31()
			{
				return new CuiButton();
			}

			// Token: 0x06000117 RID: 279 RVA: 0x00007568 File Offset: 0x00005768
			static CuiButtonComponent smethod_32(CuiButton cuiButton_0)
			{
				return cuiButton_0.Button;
			}

			// Token: 0x06000118 RID: 280 RVA: 0x0000757C File Offset: 0x0000577C
			static void smethod_33(CuiButtonComponent cuiButtonComponent_0, string string_0)
			{
				cuiButtonComponent_0.Color = string_0;
			}

			// Token: 0x06000119 RID: 281 RVA: 0x00007590 File Offset: 0x00005790
			static void smethod_34(CuiButtonComponent cuiButtonComponent_0, string string_0)
			{
				cuiButtonComponent_0.Command = string_0;
			}

			// Token: 0x0600011A RID: 282 RVA: 0x000075A4 File Offset: 0x000057A4
			static void smethod_35(CuiButtonComponent cuiButtonComponent_0, float float_0)
			{
				cuiButtonComponent_0.FadeIn = float_0;
			}

			// Token: 0x0600011B RID: 283 RVA: 0x000075B8 File Offset: 0x000057B8
			static void smethod_36(CuiButtonComponent cuiButtonComponent_0, string string_0)
			{
				cuiButtonComponent_0.Material = string_0;
			}

			// Token: 0x0600011C RID: 284 RVA: 0x000075CC File Offset: 0x000057CC
			static CuiRectTransformComponent smethod_37(CuiButton cuiButton_0)
			{
				return cuiButton_0.RectTransform;
			}

			// Token: 0x0600011D RID: 285 RVA: 0x000075E0 File Offset: 0x000057E0
			static CuiTextComponent smethod_38(CuiButton cuiButton_0)
			{
				return cuiButton_0.Text;
			}

			// Token: 0x0600011E RID: 286 RVA: 0x000075F4 File Offset: 0x000057F4
			static string smethod_39(CuiElementContainer cuiElementContainer_0, CuiButton cuiButton_0, string string_0, string string_1)
			{
				return cuiElementContainer_0.Add(cuiButton_0, string_0, string_1);
			}

			// Token: 0x0600011F RID: 287 RVA: 0x0000760C File Offset: 0x0000580C
			static void smethod_40(CuiElement cuiElement_0, string string_0)
			{
				cuiElement_0.Name = string_0;
			}

			// Token: 0x06000120 RID: 288 RVA: 0x00007620 File Offset: 0x00005820
			static void smethod_41(CuiElement cuiElement_0, string string_0)
			{
				cuiElement_0.Parent = string_0;
			}

			// Token: 0x06000121 RID: 289 RVA: 0x00007634 File Offset: 0x00005834
			static List<ICuiComponent> smethod_42(CuiElement cuiElement_0)
			{
				return cuiElement_0.Components;
			}

			// Token: 0x06000122 RID: 290 RVA: 0x00007648 File Offset: 0x00005848
			static CuiRawImageComponent smethod_43()
			{
				return new CuiRawImageComponent();
			}

			// Token: 0x06000123 RID: 291 RVA: 0x0000765C File Offset: 0x0000585C
			static void smethod_44(CuiRawImageComponent cuiRawImageComponent_0, string string_0)
			{
				cuiRawImageComponent_0.Png = string_0;
			}

			// Token: 0x06000124 RID: 292 RVA: 0x00007670 File Offset: 0x00005870
			static void smethod_45(CuiRawImageComponent cuiRawImageComponent_0, string string_0)
			{
				cuiRawImageComponent_0.Sprite = string_0;
			}

			// Token: 0x06000125 RID: 293 RVA: 0x00007684 File Offset: 0x00005884
			static CuiRectTransformComponent smethod_46()
			{
				return new CuiRectTransformComponent();
			}

			// Token: 0x06000126 RID: 294 RVA: 0x00007698 File Offset: 0x00005898
			static CuiTextComponent smethod_47()
			{
				return new CuiTextComponent();
			}

			// Token: 0x06000127 RID: 295 RVA: 0x000076AC File Offset: 0x000058AC
			static CuiOutlineComponent smethod_48()
			{
				return new CuiOutlineComponent();
			}

			// Token: 0x06000128 RID: 296 RVA: 0x000076C0 File Offset: 0x000058C0
			static void smethod_49(CuiOutlineComponent cuiOutlineComponent_0, string string_0)
			{
				cuiOutlineComponent_0.Distance = string_0;
			}

			// Token: 0x06000129 RID: 297 RVA: 0x000076D4 File Offset: 0x000058D4
			static void smethod_50(CuiOutlineComponent cuiOutlineComponent_0, string string_0)
			{
				cuiOutlineComponent_0.Color = string_0;
			}

			// Token: 0x0600012A RID: 298 RVA: 0x000076E8 File Offset: 0x000058E8
			static CuiInputFieldComponent smethod_51()
			{
				return new CuiInputFieldComponent();
			}

			// Token: 0x0600012B RID: 299 RVA: 0x000076FC File Offset: 0x000058FC
			static void smethod_52(CuiInputFieldComponent cuiInputFieldComponent_0, string string_0)
			{
				cuiInputFieldComponent_0.Color = string_0;
			}

			// Token: 0x0600012C RID: 300 RVA: 0x00007710 File Offset: 0x00005910
			static void smethod_53(CuiInputFieldComponent cuiInputFieldComponent_0, string string_0)
			{
				cuiInputFieldComponent_0.Text = string_0;
			}

			// Token: 0x0600012D RID: 301 RVA: 0x00007724 File Offset: 0x00005924
			static void smethod_54(CuiInputFieldComponent cuiInputFieldComponent_0, int int_0)
			{
				cuiInputFieldComponent_0.FontSize = int_0;
			}

			// Token: 0x0600012E RID: 302 RVA: 0x00007738 File Offset: 0x00005938
			static void smethod_55(CuiInputFieldComponent cuiInputFieldComponent_0, string string_0)
			{
				cuiInputFieldComponent_0.Command = string_0;
			}

			// Token: 0x0600012F RID: 303 RVA: 0x0000774C File Offset: 0x0000594C
			static void smethod_56(CuiInputFieldComponent cuiInputFieldComponent_0, int int_0)
			{
				cuiInputFieldComponent_0.CharsLimit = int_0;
			}

			// Token: 0x06000130 RID: 304 RVA: 0x00007760 File Offset: 0x00005960
			static void smethod_57(CuiInputFieldComponent cuiInputFieldComponent_0, TextAnchor textAnchor_0)
			{
				cuiInputFieldComponent_0.Align = textAnchor_0;
			}

			// Token: 0x06000131 RID: 305 RVA: 0x00007774 File Offset: 0x00005974
			static void smethod_58(CuiInputFieldComponent cuiInputFieldComponent_0, bool bool_0)
			{
				cuiInputFieldComponent_0.IsPassword = bool_0;
			}
		}

		// Token: 0x02000016 RID: 22
		[CompilerGenerated]
		private sealed class Class10
		{
			// Token: 0x06000133 RID: 307 RVA: 0x00007788 File Offset: 0x00005988
			internal void method_0()
			{
				CuiHelper.DestroyUi(this.basePlayer_0, <Module>.smethod_28<string>(4152733269u));
			}

			// Token: 0x06000134 RID: 308 RVA: 0x00006D58 File Offset: 0x00004F58
			static bool smethod_0(BasePlayer basePlayer_1, string string_0)
			{
				return CuiHelper.DestroyUi(basePlayer_1, string_0);
			}

			// Token: 0x04000047 RID: 71
			public BasePlayer basePlayer_0;
		}

		// Token: 0x02000017 RID: 23
		[CompilerGenerated]
		[Serializable]
		private sealed class Class11
		{
			// Token: 0x06000137 RID: 311 RVA: 0x000077C4 File Offset: 0x000059C4
			internal int method_0(Item item_0)
			{
				return item_0.amount;
			}

			// Token: 0x04000048 RID: 72
			public static readonly OxideZetaCraft.Class11 <>9 = new OxideZetaCraft.Class11();

			// Token: 0x04000049 RID: 73
			public static Func<Item, int> <>9__34_0;
		}
	}
}
