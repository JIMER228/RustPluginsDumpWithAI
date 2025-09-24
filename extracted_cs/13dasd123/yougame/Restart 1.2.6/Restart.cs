using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Linq;
using System.Text;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Configuration;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("Restart", "A0001", "1.2.6", ResourceId = 1826)]
	public class Restart : RustPlugin
	{
		bool Changed;
		JsonSerializerSettings jsonsettings;
		Dictionary<BasePlayer, Timer> timers = new Dictionary<BasePlayer, Timer>();
		Dictionary<string, string> userAgent;
		Timer _blogTimer = null;
		Timer _oxideTimer = null;
		int serverOxideVersion;

		bool initCheckDevblog;
		bool initCheckOxideBuild;
		bool simulationActive;
		bool patchRestartRunning;

		int lastMinute;
		int lastSecond;
		int currentCountDown;
		bool countDownActive;
		bool secondsActive;
		bool timerActivated;
		bool newDevblogDetected;
		bool newOxideBuildDetected;

		UIColor deathNoticeShadowColor = new UIColor(0.1, 0.1, 0.1, 0.8);
		UIColor deathNoticeColor = new UIColor(0.85, 0.85, 0.85, 0.1);

		Dictionary<string, object> rebootTimes = new Dictionary<string, object>();
		List<object> countDownMinutes = new List<object>();
		List<object> countDownSeconds = new List<object>();
		bool useTimers;

		int currentDevblog;
		int currentOxideBuild;
		int checkIntervalMinutes;
		bool enableAutoChecks;
		bool enableAutoReboot;
		int autoRebootCountDown;
		bool notifyOnlineAdmins;

		bool SimpleUI_Enable;
		int SimpleUI_FontSize;
		float SimpleUI_Top;
		float SimpleUI_Left;
		float SimpleUI_MaxWidth;
		float SimpleUI_MaxHeight;
		float SimpleUI_HideTimer;

		static Dictionary<string, object> defaultRebootTimes()
		{
			var dp = new Dictionary<string, object>();
			dp.Add("23:30","30");
			dp.Add("11:15","45");
			return dp;
		}

		static List<object> defaultCountDownMinutes()
		{
			var dp = new List<object>();
			dp.Add(60);
			dp.Add(45);
			dp.Add(30);
			dp.Add(15);
			dp.Add(10);
			dp.Add(5);
			dp.Add(4);
			dp.Add(3);
			dp.Add(2);
			dp.Add(1);
			return dp;
		}

		static List<object> defaultCountDownSeconds()
		{
			var dp = new List<object>();
			dp.Add(50);
			dp.Add(40);
			dp.Add(30);
			dp.Add(20);
			dp.Add(10);
			dp.Add(5);
			dp.Add(4);
			dp.Add(3);
			dp.Add(2);
			dp.Add(1);
			return dp;
		}

		List<Regex> regexTags = new List<Regex>
		{
			new Regex(@"<color=.+?>", RegexOptions.Compiled),
			new Regex(@"<size=.+?>", RegexOptions.Compiled)
		};

		List<string> tags = new List<string>
		{
			"</color>",
			"</size>",
			"<i>",
			"</i>",
			"<b>",
			"</b>"
		};

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
			rebootTimes = (Dictionary<string, object>)GetConfig("Таймер", "Время перезагрузки", defaultRebootTimes());
			countDownMinutes = (List<object>)GetConfig("Настройки", "Показывать минуты", defaultCountDownMinutes());
			countDownSeconds = (List<object>)GetConfig("Настройки", "Показывать секунды", defaultCountDownSeconds());
			useTimers = Convert.ToBoolean(GetConfig("Таймер", "Включить таймер", false));

			currentDevblog = Convert.ToInt32(GetConfig("Проверки", "Текущий Devblog", 0));
			currentOxideBuild = Convert.ToInt32(GetConfig("Проверки", "Текущий OxideBuild", 0));
			checkIntervalMinutes = Convert.ToInt32(GetConfig("Проверки", "Проверка интервала минут", 5));
			enableAutoChecks = Convert.ToBoolean(GetConfig("Проверки", "Включить автоматическую проверку", true));
			enableAutoReboot = Convert.ToBoolean(GetConfig("Проверки", "Включить автоматическую перезагрузку", false));
			autoRebootCountDown = Convert.ToInt32(GetConfig("Проверки", "Автоматичекое появление", 3));
			notifyOnlineAdmins = Convert.ToBoolean(GetConfig("Проверки", "Уведомлять онлайн админов", true));

			SimpleUI_Enable = Convert.ToBoolean(GetConfig("UI панель", "Включить UI панель", true));
			SimpleUI_FontSize = Convert.ToInt32(GetConfig("UI панель", "Размер текста UI панели", 30));
			SimpleUI_Top = Convert.ToSingle(GetConfig("UI панель", "Перемещение UI вверх/вниз", 0.1));
			SimpleUI_Left = Convert.ToSingle(GetConfig("UI панель", "Перемещение UI вправо/влево", 0.1));
			SimpleUI_MaxWidth = Convert.ToSingle(GetConfig("UI панель", "Макс Ширина UI панели", 0.8));
			SimpleUI_MaxHeight = Convert.ToSingle(GetConfig("UI панель", "Макс высота UI панели", 0.05));
			SimpleUI_HideTimer = Convert.ToSingle(GetConfig("UI панель", "UI таймер", 10));

			if (!Changed) return;
			SaveConfig();
			Changed = false;
		}

		protected override void LoadDefaultConfig()
		{
			Config.Clear();
			LoadVariables();
		}
		
		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			                      {
									{"RestartInit", "<color=lime>Рестарт сервера через</color> <color=red>{0}</color> <color=lime>минут(ы)!</color>"},
									{"RestartInitPatch", "<color=red>Вышло обновление OxideBuild</color> <color=lime>рестарт через</color> <color=red>{0}</color> <color=lime>минут(ы)!</color>"},
									{"RestartInitSec", "<color=lime>Рестарт сервера через</color> <color=red>{0}</color> <color=lime>секунд(ы/у)!</color>"},
									{"RestartCancel", "<b><color=orange>Рестарт сервера остановлен!</color></b>"},
									{"ManualInit", "<color=orange>Отсчет таймера начнется со следующей полной минуты</color>"},
									{"ManualCancel", "<color=orange>Таймер был отменён</color>"},
									{"DevblogDetected", "<color=orange>Вышел новый DevBlog</color> <color=green>{0}</color>"},
									{"OxideBuildDetected", "<color=orange>Вышло обновление OxideBuild</color> <color=green>{0}</color>"},
									},this);
		}

		void Init()
		{
			LoadVariables();
			LoadDefaultMessages();
			lastMinute = DateTime.Now.Minute;
			lastSecond = DateTime.Now.Second;
			currentCountDown = 0;
			jsonsettings = new JsonSerializerSettings();
			jsonsettings.Converters.Add(new KeyValuePairConverter());
			newDevblogDetected = false;
			newOxideBuildDetected = false;
			userAgent = new Dictionary<string, string>();
			userAgent.Add("User-Agent", "OxideMod");
			serverOxideVersion = Convert.ToInt32(Manager.GetPlugin("RustCore").Version.Patch);
			initCheckDevblog = true;
			initCheckOxideBuild = true;
			simulationActive = false;
			patchRestartRunning = false;
		}

		void OnServerInitialized()
		{
			if (!permission.PermissionExists("restart.admin"))
				permission.RegisterPermission("restart.admin", this);
				CheckDevBlog(4);
				CheckOxideCommits();
			if (enableAutoChecks)
				_blogTimer = timer.Every(checkIntervalMinutes*60, () => CheckDevBlog(4));
		}

		void OnTick()
		{
			if ((!secondsActive && lastMinute == DateTime.Now.Minute)) return;
			if (secondsActive && lastSecond == DateTime.Now.Second) return;
			if (secondsActive)
			{
				lastSecond = DateTime.Now.Second;
				if (currentCountDown == 0)
				{
					if (!simulationActive)
					{
						ConVar.Global.quit(null);
						return;
					}
					else
					{
						secondsActive = false;
						timerActivated = false;
						simulationActive = false;
						enableAutoReboot = (bool)Config["Проверки", "enableAutoReboot"];
						Puts("Patch simulation finished");
						return;
					}
				}
				if (countDownSeconds.Contains(currentCountDown))
					DoSmoothRestart(currentCountDown, false);
				currentCountDown--;
				return;
				// breaks here on times under a minute
			}
			lastMinute = DateTime.Now.Minute;
			if (countDownActive || (!countDownActive && timerActivated && currentCountDown == 1))
			{
				if (countDownMinutes.Contains(currentCountDown))
					DoSmoothRestart(currentCountDown);
				currentCountDown--;
				if (currentCountDown == 0)
				{
					secondsActive = true;
					lastSecond = DateTime.Now.Second;
					currentCountDown = 59;
					return;
				}
				return;
			}
			if ((useTimers && !countDownActive && rebootTimes.ContainsKey(DateTime.Now.ToString("HH:mm"))) || timerActivated)
			{
				if (!timerActivated)
					currentCountDown = Convert.ToInt32(rebootTimes[DateTime.Now.ToString("HH:mm")]);
				countDownActive = true;
				DoSmoothRestart(currentCountDown);
				currentCountDown--;
			}
		}

		void CheckDevBlog(int countNews)
		{
			var url = $"http://api.steampowered.com/ISteamNews/GetNewsForApp/v0002/?appid=252490&count={countNews}&maxlength=1&format=json";
			try { webrequest.Enqueue(url, null, (code, response) => APIResponse(code, response, "devblog", countNews), this, RequestMethod.GET); }
			catch { timer.Once(60f, () => CheckDevBlog(4)); }
		}

		void CheckOxideCommits()
		{
			var url = $"https://api.github.com/repos/oxidemod/oxide.rust/releases/latest";
			Dictionary<string, string> userAgent  = new Dictionary<string, string>();
			userAgent.Add("User-Agent", "OxideMod");
			try { webrequest.Enqueue(url, null, (code, response) => APIResponse(code, response, "oxide", 0), this, RequestMethod.GET, userAgent); }
			catch { timer.Once(60f, CheckOxideCommits);}
		}

		void APIResponse(int code, string response, string apiType, int numberCheck)
		{
			if (!(response == null || code != 200) && apiType == "devblog")
			{
				var news = JsonConvert.DeserializeObject<AppNewsClass>(response);
				foreach (var item in news.appnews.newsitems)
				{
					if (item.title.Contains("Devblog"))
					{
						if (currentDevblog == 0 || initCheckDevblog)
						{
							var resultBlog = Convert.ToInt32(item.title.Replace(" ", "").Replace("Devblog", ""));
							if (resultBlog != currentDevblog)
							{
								currentDevblog = resultBlog;
								Config["Проверки", "currentDevblog"] = currentDevblog;
								Config.Save();
							}
							if (initCheckDevblog) initCheckDevblog = false;
							break;
						}
						var checkedDevblog = Convert.ToInt32(item.title.Replace(" ", "").Replace("Devblog", ""));
						bool blogChanged = false;
						if (currentDevblog != checkedDevblog)
						{
							blogChanged = true;
							currentDevblog = checkedDevblog;
							Config["Проверки", "currentDevblog"] = currentDevblog;
							Config.Save();
						}
						if (blogChanged)
						{
							Puts(StripTags(string.Format(lang.GetMessage("DevblogDetected", this), currentDevblog)));
							if (notifyOnlineAdmins)
								foreach (var player in BasePlayer.activePlayerList.Where(p => p.IsAdmin).ToList())
									SendReply(player, string.Format(lang.GetMessage("DevblogDetected", this), currentDevblog));
							newDevblogDetected = true;
							if ( _blogTimer == null || _blogTimer.Destroyed)
								return;
							_blogTimer.Destroy();
							_blogTimer = null;
							_oxideTimer = timer.Every(60f, CheckOxideCommits);
						}
						break;
					}
				} 
			}
			else if (!(response == null || code != 200) && apiType == "oxide")
			{
				var jsonresponse2 = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, jsonsettings);
				if (!(jsonresponse2 is Dictionary<string, object>) || jsonresponse2.Count == 0 || !jsonresponse2.ContainsKey("name")) return;
				string message = (string)jsonresponse2["name"];
				if (message == "")
					message = Oxide.Core.OxideMod.Version.ToString();
				var buildNum = Convert.ToInt32(message.Substring(4));
				if (currentOxideBuild == 0 || initCheckOxideBuild)
				{
					if ( buildNum != currentOxideBuild)
					{
						currentOxideBuild = buildNum;
						Config["Проверки", "currentOxideBuild"] = currentOxideBuild;
						Config.Save();
					}
					if (initCheckOxideBuild)
					{
						if (currentOxideBuild != serverOxideVersion)
							Puts($"Серверный оксид новой сборки '{currentOxideBuild}'");
						else
							Puts($"Серверный оксид обновлён");
					}
					if (initCheckOxideBuild) initCheckOxideBuild = false;
					return;
				}
				bool buildChanged = false;
				if (currentOxideBuild != buildNum)
				{
					buildChanged = true;
					currentOxideBuild = buildNum;
					Config["Проверки", "currentOxideBuild"] = currentOxideBuild;
					Config.Save();
				}
				if (buildChanged)
				{
					Puts(StripTags(string.Format(lang.GetMessage("OxideBuildDetected", this), currentOxideBuild)));
					if (notifyOnlineAdmins)
						foreach (var player in BasePlayer.activePlayerList.Where(p => p.IsAdmin).ToList())
							SendReply(player, string.Format(lang.GetMessage("OxideBuildDetected", this), currentOxideBuild));
					newOxideBuildDetected = true;
					if ( _oxideTimer == null || _oxideTimer.Destroyed)
						return;
					_oxideTimer.Destroy();
					_oxideTimer = null;
					CheckAutoRestart();
				}
			}
		}

		void CheckAutoRestart()
		{
			if (!enableAutoReboot) return;
			if (!newDevblogDetected || !newOxideBuildDetected) return;
			patchRestartRunning = true;
			timerActivated = true;
			currentCountDown = autoRebootCountDown;
		}

		[ConsoleCommand("r.simulatepatch")]
		void checkOxideCommits(ConsoleSystem.Arg arg)
		{
			if(arg.Connection != null && arg.Connection.authLevel < 2) return;
			currentDevblog--;
			currentOxideBuild--;
			SendReply(arg, "Изменены текущие номера Devblog и OxideBuild для имитации успешных проверок");
			simulationActive = true;
			enableAutoReboot = true;
		}

		[ConsoleCommand("r.restart")]
		void smoothRestartConsoleCommand(ConsoleSystem.Arg arg)
		{
			if(arg.Connection != null && arg.Connection.authLevel < 2) return;
			if (arg.Args != null && arg.Args.Length == 1)
			{
				if (arg.Args[0].ToLower() == "stop")
				{
					if (countDownActive)
					{
						timerActivated = false;
						secondsActive = false;
						patchRestartRunning = false;
						StopRestart();
						return;
					}
					if (timerActivated)
					{
						timerActivated = false;
						secondsActive = false;
						SendReply(arg, StripTags(lang.GetMessage("ManualCancel", this)));
					}
				}
				else
				{
					int minutes;
					if (int.TryParse(arg.Args[0], out minutes))
					{
						currentCountDown = Convert.ToInt32(minutes);
						lastMinute = DateTime.Now.Minute;
						timerActivated = true;
						SendReply(arg, StripTags(lang.GetMessage("ManualInit", this)));
					}
					else
						SendReply(arg, "Неверный формат <minutes> ! Должно быть число!");
				}
			}
			else
				SendReply(arg, "Доступные команды:\nr.restart <минуты> - запустить ручной рестарт\nПример: r.restart 5 (рестарт сервере через 5 минут)\nr.restart stop - отменить ручной рестарт");
		}

		[ChatCommand("restart")]
		void smoothRestartCommand(BasePlayer player, string command, string[] args)
		{
			if (permission.UserHasPermission(player.UserIDString, "restart.admin") || player.net.connection.authLevel > 1)
			{
				if (args != null && args.Length == 1)
				{
					if (args[0].ToLower() == "stop")
					{
						if (countDownActive)
						{
							timerActivated = false;
							secondsActive = false;
							patchRestartRunning = false;
							StopRestart();
							return;
						}
						if (timerActivated)
						{
							timerActivated = false;
							secondsActive = false;
							SendReply(player, lang.GetMessage("ManualCancel", this));
						}
					}
					else
					{
						int minutes;
						if (int.TryParse(args[0], out minutes))
						{
							currentCountDown = Convert.ToInt32(minutes);
							lastMinute = DateTime.Now.Minute;
							timerActivated = true;
							SendReply(player, lang.GetMessage("ManualInit", this));
						}
						else
							SendReply(player, "Неверный формат <minutes> ! Должно быть число!");
					}
				}
				else
					SendReply(player, "<size=16><color=orange>Доступные команды:</color></size>\n<color=orange>/restart <минуты> </color> - запустить ручной рестарт\nПример: <color=orange>/restart 5</color> (рестарт сервера через 5 минут)\n<color=orange>/restart stop</color> - отменить ручной рестарт");
			}
		}

		void StopRestart()
		{
			if (!countDownActive) return;
			countDownActive = false;
			currentCountDown = 0;
			string msg = string.Format(lang.GetMessage("RestartCancel", this));
			Puts(StripTags(msg));
			if(SimpleUI_Enable)
				foreach (BasePlayer player in BasePlayer.activePlayerList)
				{
					UIMessage(player, msg);
				}
			else
				Server.Broadcast(msg);
		}

		void DoSmoothRestart(int timer, bool unitMinutes = true)
		{
			string msg = "";
			if (unitMinutes)
			{
				if (patchRestartRunning)
					msg = string.Format(lang.GetMessage("RestartInitPatch", this), timer);
				else
					msg = string.Format(lang.GetMessage("RestartInit", this), timer);
			}
			else
				msg = string.Format(lang.GetMessage("RestartInitSec", this), timer);
			Puts(StripTags(msg));
			if(SimpleUI_Enable)
				foreach (BasePlayer player in BasePlayer.activePlayerList)
				{
					UIMessage(player, msg);
				}
			else
				Server.Broadcast(msg);
		}

		class UIColor
		{
			string color;

			public UIColor(double red, double green, double blue, double alpha)
			{
				color = $"{red} {green} {blue} {alpha}";
			}

			public override string ToString() => color;
		}

		class UIObject
		{
			List<object> ui = new List<object>();
			List<string> objectList = new List<string>();

			public UIObject()
			{
			}

			string RandomString()
			{
				const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
				List<char> charList = chars.ToList();

				string random = "";

				for (int i = 0; i <= UnityEngine.Random.Range(5, 10); i++)
					random = random + charList[UnityEngine.Random.Range(0, charList.Count - 1)];

				return random;
			}

			public void Draw(BasePlayer player)
			{
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", JsonConvert.SerializeObject(ui).Replace("{NEWLINE}", Environment.NewLine));
			}

			public void Destroy(BasePlayer player)
			{
				foreach (string uiName in objectList)
					CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", uiName);
			}

			public string AddText(string name, double left, double top, double width, double height, UIColor color, string text, int textsize = 15, string parent = "Hud", int alignmode = 0, float fadeIn = 0f, float fadeOut = 0f)
			{
				//name = name + RandomString();
				text = text.Replace("\n", "{NEWLINE}");
				string align = "";

				switch (alignmode)
				{
					case 0: { align = "LowerCenter"; break; };
					case 1: { align = "LowerLeft"; break; };
					case 2: { align = "LowerRight"; break; };
					case 3: { align = "MiddleCenter"; break; };
					case 4: { align = "MiddleLeft"; break; };
					case 5: { align = "MiddleRight"; break; };
					case 6: { align = "UpperCenter"; break; };
					case 7: { align = "UpperLeft"; break; };
					case 8: { align = "UpperRight"; break; };
				}

				ui.Add(new Dictionary<string, object> {
					{"name", name},
					{"parent", parent},
					{"fadeOut", fadeOut.ToString()},
					{"components",
						new List<object> {
							new Dictionary<string, string> {
								{"type", "UnityEngine.UI.Text"},
								{"text", text},
								{"fontSize", textsize.ToString()},
								{"color", color.ToString()},
								{"align", align},
								{"fadeIn", fadeIn.ToString()}
							},
							new Dictionary<string, string> {
								{"type", "RectTransform"},
								{"anchormin", $"{left} {((1 - top) - height)}"},
								{"anchormax", $"{(left + width)} {(1 - top)}"}
							}
						}
					}
				});

				objectList.Add(name);
				return name;
			}
		}

		void UIMessage(BasePlayer player, string message)
		{
			bool replaced = false;
			float fadeIn = 0.2f;

			Timer playerTimer;

			timers.TryGetValue(player, out playerTimer);

			if (playerTimer != null && !playerTimer.Destroyed)
			{
				playerTimer.Destroy();
				fadeIn = 0.1f;

				replaced = true;
			}

			UIObject ui = new UIObject();

			ui.AddText("DeathNotice_DropShadow", SimpleUI_Left + 0.001, SimpleUI_Top + 0.001, SimpleUI_MaxWidth, SimpleUI_MaxHeight, deathNoticeShadowColor, StripTags(message), SimpleUI_FontSize, "Hud", 3, fadeIn, 0.2f);
			ui.AddText("DeathNotice", SimpleUI_Left, SimpleUI_Top, SimpleUI_MaxWidth, SimpleUI_MaxHeight, deathNoticeColor, message, SimpleUI_FontSize, "Hud", 3, fadeIn, 0.2f);

			ui.Destroy(player);

			if(replaced)
			{
				timer.Once(0.1f, () =>
				{
					ui.Draw(player);

					timers[player] = timer.Once(SimpleUI_HideTimer, () => ui.Destroy(player));
				});
			}
			else
			{
				ui.Draw(player);

				timers[player] = timer.Once(SimpleUI_HideTimer, () => ui.Destroy(player));
			}
		}

		string StripTags(string original)
		{
			foreach (string tag in tags)
				original = original.Replace(tag, "");

			foreach (Regex regexTag in regexTags)
				original = regexTag.Replace(original, "");

			return original;
		}
		
		public class AppNewsClass
		{
			public Appnews appnews { get; set; }
			
			public class Appnews
			{
				public int appid { get; set; }
				public List<Newsitem> newsitems { get; set; }
				public int count { get; set; }
				
				public class Newsitem
				{
					public string gid { get; set; }
					public string title { get; set; }
					public string url { get; set; }
					public bool is_external_url { get; set; }
					public string author { get; set; }
					public string contents { get; set; }
					public string feedlabel { get; set; }
					public int date { get; set; }
					public string feedname { get; set; }
					public int feed_type { get; set; }
					public int appid { get; set; }
				}
			}
		}

	}
}