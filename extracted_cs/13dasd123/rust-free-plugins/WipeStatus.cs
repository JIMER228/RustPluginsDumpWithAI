// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿/*
*  < ----- End-User License Agreement ----->
*  
*  You may not copy, modify, merge, publish, distribute, sublicense, or sell copies of this software without the developer’s consent.
*
*  THIS SOFTWARE IS PROVIDED BY IIIaKa AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, 
*  THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS 
*  BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
*  GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
*  LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*
*  Developer: IIIaKa
*      https://t.me/iiiaka
*      Discord: @iiiaka
*      https://github.com/IIIaKa
*      https://umod.org/user/IIIaKa
*      https://codefling.com/iiiaka
*      https://lone.design/vendor/iiiaka/
*      https://www.patreon.com/iiiaka
*      https://boosty.to/iiiaka
*  Codefling plugin page: https://codefling.com/plugins/wipe-status
*  Codefling license: https://codefling.com/plugins/wipe-status?tab=downloads_field_4
*  
*  Lone.Design plugin page: https://lone.design/product/wipe-status/
*
*  Copyright © 2023-2025 IIIaKa
*/

using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
	[Info("Wipe Status", "IIIaKa", "0.1.8")]
	[Description("The plugin displays the time until the next wipe in the status bar. Depends on AdvancedStatus plugin.")]
	class WipeStatus : RustPlugin
	{
		[PluginReference]
		private Plugin ImageLibrary, AdvancedStatus;
		
		#region ~Variables~
		private bool _imgLibIsLoaded = false, _statusIsLoaded = false;
		private const string PERMISSION_ADMIN = "wipestatus.admin", BarID = "WipeStatus_Wipe", LangKey = "BarText", StatusCreateBar = "CreateBar", StatusUpdateContent = "UpdateContent", StatusDeleteBar = "DeleteBar", StatusDeleteBarForAll = "DeleteBarForAll", StatusInBuilding = "InBuildingPrivilege", TimeFormat = "yyyy-MM-dd HH:mm";
		private readonly string[] HttpScheme = new string[] { "http://", "https://" };
		private double _timeStamp = 0d;
		private Timer _timer;
		private readonly string[] _hooks = new string[] { "OnPlayerConnected", "OnPlayerLanguageChanged", "OnEntityEnter", "OnEntityLeave", "OnPlayerGainedBuildingPrivilege", "OnPlayerLostBuildingPrivilege" };
		private Dictionary<int, object> _statusBar;
		private Dictionary<ulong, bool> _playersSettings;
		#endregion

        #region ~Configuration~
        private static Configuration _config;

		private class Configuration
		{
			[JsonProperty(PropertyName = "Chat command")]
			public string Command = string.Empty;
			
			[JsonProperty(PropertyName = "Is it worth enabling GameTips for messages?")]
			public bool GameTips_Enabled = true;
			
			[JsonProperty(PropertyName = "List of language keys for creating language files")]
            public List<string> LanguageKeys;
			
			[JsonProperty(PropertyName = "Is it worth displaying the wipe timer only when players in the safe zone or building privilege?")]
			public bool Display_InSafeZone = false;
			
			[JsonProperty(PropertyName = "The number of days before the wipe when the status bar will start to display. A value of 0 enables constant display")]
			public int Display_DaysLeft = 0;
			
			[JsonProperty(PropertyName = "Status. Bar - Type(TimeProgressCounter or TimeCounter)")]
            public string Status_Bar_Type = string.Empty;
			
			[JsonProperty(PropertyName = "Status. Bar - Height")]
            public int Status_Bar_Height = 26;

            [JsonProperty(PropertyName = "Status. Bar - Order")]
            public int Status_Bar_Order = 10;

            [JsonProperty(PropertyName = "Status. Background - Color(Hex or RGBA)")]
            public string Status_Main_Color = "#0370A4";

            [JsonProperty(PropertyName = "Status. Background - Transparency")]
            public float Status_Main_Transparency = 0.7f;

            [JsonProperty(PropertyName = "Status. Background - Material(empty to disable)")]
            public string Status_Main_Material = string.Empty;

            [JsonProperty(PropertyName = "Status. Image - Url")]
            public string Status_Image_Url = "https://i.imgur.com/FKrFYN5.png";

            [JsonProperty(PropertyName = "Status. Image - Local(Leave empty to use Image_Url)")]
            public string Status_Image_Local = "WipeStatus_Wipe";

            [JsonProperty(PropertyName = "Status. Image - Sprite(Leave empty to use Image_Local or Image_Url)")]
            public string Status_Image_Sprite = string.Empty;

            [JsonProperty(PropertyName = "Status. Image - Is raw image")]
            public bool Status_Image_IsRawImage = false;

            [JsonProperty(PropertyName = "Status. Image - Color(Hex or RGBA)")]
            public string Status_Image_Color = "#0370A4";

            [JsonProperty(PropertyName = "Status. Image - Transparency")]
            public float Status_Image_Transparency = 1f;

            [JsonProperty(PropertyName = "Status. Image Outline - Is it worth enabling an outline for the image?")]
            public bool Status_Image_Outline_Enabled = false;

            [JsonProperty(PropertyName = "Status. Image Outline - Color(Hex or RGBA)")]
            public string Status_Image_Outline_Color = "0.1 0.3 0.8 0.9";

            [JsonProperty(PropertyName = "Status. Image Outline - Transparency")]
            public float Status_Image_Outline_Transparency = 1f;

            [JsonProperty(PropertyName = "Status. Image Outline - Distance")]
            public string Status_Image_Outline_Distance = "0.75 0.75";

            [JsonProperty(PropertyName = "Status. Text - Size")]
            public int Status_Text_Size = 12;

            [JsonProperty(PropertyName = "Status. Text - Color(Hex or RGBA)")]
            public string Status_Text_Color = "#FFFFFF";

            [JsonProperty(PropertyName = "Status. Text - Font(https://umod.org/guides/rust/basic-concepts-of-gui#fonts)")]
            public string Status_Text_Font = "RobotoCondensed-Bold.ttf";

            [JsonProperty(PropertyName = "Status. Text - Offset Horizontal")]
            public int Status_Text_Offset_Horizontal = 0;

            [JsonProperty(PropertyName = "Status. Text Outline - Is it worth enabling an outline for the text?")]
            public bool Status_Text_Outline_Enabled = false;

            [JsonProperty(PropertyName = "Status. Text Outline - Color(Hex or RGBA)")]
            public string Status_Text_Outline_Color = "#000000";

            [JsonProperty(PropertyName = "Status. Text Outline - Transparency")]
            public float Status_Text_Outline_Transparency = 1f;

            [JsonProperty(PropertyName = "Status. Text Outline - Distance")]
            public string Status_Text_Outline_Distance = "0.75 0.75";

            [JsonProperty(PropertyName = "Status. SubText - Size")]
            public int Status_SubText_Size = 12;

            [JsonProperty(PropertyName = "Status. SubText - Color(Hex or RGBA)")]
            public string Status_SubText_Color = "#FFFFFF";

            [JsonProperty(PropertyName = "Status. SubText - Font")]
            public string Status_SubText_Font = "RobotoCondensed-Bold.ttf";

            [JsonProperty(PropertyName = "Status. SubText Outline - Is it worth enabling an outline for the sub text?")]
            public bool Status_SubText_Outline_Enabled = false;

            [JsonProperty(PropertyName = "Status. SubText Outline - Color(Hex or RGBA)")]
            public string Status_SubText_Outline_Color = "0.5 0.6 0.7 0.5";

            [JsonProperty(PropertyName = "Status. SubText Outline - Transparency")]
            public float Status_SubText_Outline_Transparency = 1f;

            [JsonProperty(PropertyName = "Status. SubText Outline - Distance")]
            public string Status_SubText_Outline_Distance = "0.75 0.75";
			
			[JsonProperty(PropertyName = "Status. Progress - Background Color(Hex or RGBA)")]
            public string Status_Progress_Main_Color = "1 1 1 0.15";

            [JsonProperty(PropertyName = "Status. Progress - Background Transparency")]
            public float Status_Progress_Main_Transparency = 0.15f;
			
			[JsonProperty(PropertyName = "Status. Progress - Reverse")]
			public bool Status_Progress_Reverse = true;
			
			[JsonProperty(PropertyName = "Status. Progress - Color(Hex or RGBA)")]
			public string Status_Progress_Color = "#0370A4";
			
			[JsonProperty(PropertyName = "Status. Progress - Transparency")]
			public float Status_Progress_Transparency = 0.7f;
			
			[JsonProperty(PropertyName = "Status. Progress - OffsetMin")]
			public string Status_Progress_OffsetMin = "0 0";
			
			[JsonProperty(PropertyName = "Status. Progress - OffsetMax")]
			public string Status_Progress_OffsetMax = "0 0";
			
			[JsonProperty(PropertyName = "Custom wipe dates list(empty to use default). Format: yyyy-MM-dd HH:mm. Example: 2024-10-25 13:00")]
            public List<string> CustomDates;
			
			public Oxide.Core.VersionNumber Version;
		}
		
		protected override void LoadConfig()
        {
            base.LoadConfig();
            try { _config = Config.ReadObject<Configuration>(); }
            catch (Exception ex) { PrintError($"{ex.Message}\n\n[{Title}] Your configuration file contains an error."); }
            if (_config == null || _config.Version == new VersionNumber())
            {
                PrintWarning("The configuration file is not found or contains errors. Creating a new one...");
                LoadDefaultConfig();
            }
            else if (_config.Version < Version)
            {
                PrintWarning($"Your configuration file version({_config.Version}) is outdated. Updating it to {Version}.");
                _config.Version = Version;
                PrintWarning($"The configuration file has been successfully updated to version {_config.Version}!");
            }
			
			if (string.IsNullOrWhiteSpace(_config.Command))
				_config.Command = "wipe";
			
			if (_config.LanguageKeys == null)
                _config.LanguageKeys = new List<string>();
            if (_config.LanguageKeys.Any())
            {
                for (int i = _config.LanguageKeys.Count - 1; i >= 0; i--)
                {
                    string langKey = ToLangKey(_config.LanguageKeys[i]);
                    if (langKey.Equals("en", StringComparison.OrdinalIgnoreCase) || langKey.Equals("ru", StringComparison.OrdinalIgnoreCase))
                        _config.LanguageKeys.RemoveAt(i);
                    else
                        _config.LanguageKeys[i] = langKey;
                }
            }
            _config.LanguageKeys.Add("en");
			
			_config.Display_DaysLeft = Math.Max(_config.Display_DaysLeft, 0);
			if (_config.Status_Bar_Type != "TimeProgressCounter" && _config.Status_Bar_Type != "TimeCounter")
				_config.Status_Bar_Type = "TimeCounter";
			
			_statusBar = new Dictionary<int, object>
            {
                { 0, BarID },
                { 1, Name },
				{ 2, _config.Status_Bar_Type },
				{ 4, _config.Status_Bar_Order },
                { 5, _config.Status_Bar_Height },
                { 6, _config.Status_Main_Color },
                { 11, _config.Status_Image_IsRawImage },
				{ 16, _config.Status_Text_Size },
                { 17, _config.Status_Text_Color },
                { 18, _config.Status_Text_Font },
                { 23, _config.Status_SubText_Size },
                { 24, _config.Status_SubText_Color },
                { 25, _config.Status_SubText_Font }
			};

            if (_config.Status_Main_Color.StartsWith("#"))
                _statusBar.Add(-6, _config.Status_Main_Transparency);
            if (!string.IsNullOrWhiteSpace(_config.Status_Main_Material))
                _statusBar.Add(7, _config.Status_Main_Material);
			if (!_config.Status_Image_IsRawImage)
            {
				_statusBar.Add(12, _config.Status_Image_Color);
                if (_config.Status_Image_Color.StartsWith("#"))
                    _statusBar.Add(-12, _config.Status_Image_Transparency);
            }
			if (_config.Status_Image_Outline_Enabled)
            {
                _statusBar.Add(13, _config.Status_Image_Outline_Color);
                if (_config.Status_Image_Outline_Color.StartsWith("#"))
                    _statusBar.Add(-13, _config.Status_Image_Outline_Transparency);
                _statusBar.Add(14, _config.Status_Image_Outline_Distance);
            }
            if (_config.Status_Text_Offset_Horizontal != 0)
                _statusBar.Add(19, _config.Status_Text_Offset_Horizontal);
            if (_config.Status_Text_Outline_Enabled)
            {
                _statusBar.Add(20, _config.Status_Text_Outline_Color);
                if (_config.Status_Text_Outline_Color.StartsWith("#"))
                    _statusBar.Add(-20, _config.Status_Text_Outline_Transparency);
                _statusBar.Add(21, _config.Status_Text_Outline_Distance);
            }
            if (_config.Status_SubText_Outline_Enabled)
            {
                _statusBar.Add(26, _config.Status_SubText_Outline_Color);
                if (_config.Status_SubText_Outline_Color.StartsWith("#"))
                    _statusBar.Add(-26, _config.Status_SubText_Outline_Transparency);
                _statusBar.Add(27, _config.Status_SubText_Outline_Distance);
            }
			
			if (_config.Status_Bar_Type == "TimeProgressCounter")
            {
				_statusBar[6] = _config.Status_Progress_Main_Color;
                if (_config.Status_Progress_Main_Color.StartsWith("#"))
                    _statusBar[-6] = _config.Status_Progress_Main_Transparency;
				_statusBar.Add(32, _config.Status_Progress_Reverse);
				_statusBar.Add(33, _config.Status_Progress_Color);
				if (_config.Status_Progress_Color.StartsWith("#"))
					_statusBar.Add(-33, _config.Status_Progress_Transparency);
				_statusBar.Add(34, _config.Status_Progress_OffsetMin);
				_statusBar.Add(35, _config.Status_Progress_OffsetMax);
			}
			
			if (_config.CustomDates == null)
				_config.CustomDates = new List<string>();
			else if (_config.CustomDates.Any())
			{
				var nowDate = DateTime.Now;
				_config.CustomDates = _config.CustomDates.Where(dateString => DateTime.TryParseExact(dateString, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) && date > nowDate)
					.OrderBy(dateString => DateTime.ParseExact(dateString, TimeFormat, CultureInfo.InvariantCulture)).ToList();
			}
			
			SaveConfig();
		}
		
		protected override void SaveConfig() => Config.WriteObject(_config);
		protected override void LoadDefaultConfig() => _config = new Configuration() { Version = Version };
		#endregion
		
		#region ~Language~
		protected override void LoadDefaultMessages()
		{
			var enLang = new Dictionary<string, string>
			{
				["CmdMainAdmin"] = string.Join("\n", new string[]
				{
					"Available admin commands:\n",
                    "<color=#D1CBCB>/wipe</color> <color=#D1AB9A>bar</color> <color=#D1CBCB>*booleanValue*(optional)</color> - Toggle wipe bar visibility",
					"<color=#D1CBCB>/wipe</color> <color=#D1AB9A>time</color> - Show current time based on server's timezone",
                    "<color=#D1CBCB>/wipe</color> <color=#D1AB9A>add</color> <color=#D1CBCB>*wipeDate* *numberValue*(optional) *numberValue*(optional)</color> - Add a custom wipe date. If two numbers follow the date, wipe dates will be added with that interval, for the given count. Date format: yyyy-MM-dd HH:mm",
					"<color=#D1CBCB>/wipe</color> <color=#D1AB9A>clear</color> - Clear all custom wipe dates",
					"\n--------------------------------------------------"
				}),
				["CmdMain"] = string.Join("\n", new string[]
				{
					"Available commands:\n",
					"<color=#D1CBCB>/wipe</color> <color=#D1AB9A>bar</color> <color=#D1CBCB>*booleanValue*</color>(optional) - Toggle wipe bar visibility",
					"\n--------------------------------------------------"
				}),
				["BarText"] = "WIPE IN",
				["CmdServerTime"] = "Current real server time: {0}",
				["CmdNewDateAdded"] = "The new date {0} has been successfully added!",
				["CmdNewDateRangeAdded"] = "The list of dates was successfully added!",
				["CmdNewDateAddFailed"] = "Invalid format or the date is earlier than the current one. Example: {0}",
				["CmdClearDates"] = "Custom dates list has been successfully cleared!",
				["CmdBarEnabled"] = "Displaying the wipe bar is enabled!",
				["CmdBarDisabled"] = "Displaying the wipe bar is disabled!"
            };
			var ruLang = new Dictionary<string, string>
			{
				["CmdMainAdmin"] = string.Join("\n", new string[]
                {
                    "Доступные админ команды:\n",
                    "<color=#D1CBCB>/wipe</color> <color=#D1AB9A>bar</color> <color=#D1CBCB>*булевоеЗначение*(опционально)</color> - Переключение отображения вайп бара",
                    "<color=#D1CBCB>/wipe</color> <color=#D1AB9A>time</color> - Текущее время по часовому поясу сервера",
                    "<color=#D1CBCB>/wipe</color> <color=#D1AB9A>add</color> <color=#D1CBCB>*датаВайпа* *числовоеЗначение*(опционально) *числовоеЗначение*(опционально)</color> - Добавление кастомной даты вайпа. Если после даты указать 2 числа, то вайп даты добавятся с указаным интервалом, указанное кол-во раз. Формат даты: yyyy-MM-dd HH:mm",
                    "<color=#D1CBCB>/wipe</color> <color=#D1AB9A>clear</color> - Очистка всех кастомных дат вайпа",
					"\n--------------------------------------------------"
                }),
				["CmdMain"] = string.Join("\n", new string[]
                {
                    "Доступные команды:\n",
                    "<color=#D1CBCB>/wipe</color> <color=#D1AB9A>bar</color> <color=#D1CBCB>*булевоеЗначение*</color>(опционально) - Переключение отображения вайп бара",
                    "\n--------------------------------------------------"
                }),
				["BarText"] = "ВАЙП ЧЕРЕЗ",
				["CmdServerTime"] = "Текущее реальное серверное время: {0}",
				["CmdNewDateAdded"] = "Новая дата {0} успешно добавлена!",
				["CmdNewDateRangeAdded"] = "Список дат был успешно добавлен!",
				["CmdNewDateAddFailed"] = "Не верный формат или дата меньше текущей. Пример: {0}",
				["CmdClearDates"] = "Список дат был успешно очищен!",
				["CmdBarEnabled"] = "Отображение вайп бара включено!",
				["CmdBarDisabled"] = "Отображение вайп бара выключено!"
			};
			
			for (int i = 0; i < _config.LanguageKeys.Count; i++)
                lang.RegisterMessages(enLang, this, _config.LanguageKeys[i]);
            lang.RegisterMessages(ruLang, this, "ru");
		}
        #endregion

        #region ~Methods~
		private void ToggleImageLib(bool isLoaded)
        {
            _imgLibIsLoaded = isLoaded;
            if (_imgLibIsLoaded)
            {
                if (string.IsNullOrWhiteSpace(_config.Status_Image_Sprite) && string.IsNullOrWhiteSpace(_config.Status_Image_Local) && _config.Status_Image_Url.StartsWithAny(HttpScheme))
                    ImageLibrary?.Call("AddImage", _config.Status_Image_Url, BarID, 0uL);
            }
			
			_statusBar.Remove(10);
            _statusBar.Remove(9);
            _statusBar.Remove(8);
            if (!string.IsNullOrWhiteSpace(_config.Status_Image_Sprite))
                _statusBar.Add(10, _config.Status_Image_Sprite);
            else if (!string.IsNullOrWhiteSpace(_config.Status_Image_Local))
                _statusBar.Add(9, _config.Status_Image_Local);
            else
                _statusBar.Add(8, _imgLibIsLoaded && _config.Status_Image_Url.StartsWithAny(HttpScheme) ? BarID : _config.Status_Image_Url);
		}
		
		private void SendBar(ulong userID) => AdvancedStatus?.Call(StatusCreateBar, userID, new Dictionary<int, object>(_statusBar) { { 15, lang.GetMessage(LangKey, this, userID.ToString()) }, { 29, _timeStamp } });
		
		private bool IsDisplayAllowed(BasePlayer player)
        {
			if (_playersSettings.TryGetValue(player.userID, out var canDisplay) && !canDisplay)
				return false;
			if (_config.Display_InSafeZone && !player.InSafeZone() && !InBuildingPrivilege(player.userID))
                return false;
			return true;
        }
		
		private void TryDisplay()
        {
			if (_timer != null)
				_timer.Destroy();
			
			if (!_statusIsLoaded || !TryGetWipeSpan(out var timeSpan)) return;
			_timeStamp = timeSpan.TotalSeconds + Network.TimeEx.currentTimestamp;
			if (_config.Display_DaysLeft == 0 || (int)Math.Ceiling(timeSpan.TotalDays) <= _config.Display_DaysLeft)
            {
				foreach (var player in BasePlayer.activePlayerList)
				{
					if (player.userID.IsSteamId() && IsDisplayAllowed(player))
						SendBar(player.userID);
				}
				Subscribe(nameof(OnPlayerConnected));
				Subscribe(nameof(OnPlayerLanguageChanged));
				if (_config.Display_InSafeZone)
				{
					Subscribe(nameof(OnEntityEnter));
					Subscribe(nameof(OnEntityLeave));
					Subscribe(nameof(OnPlayerGainedBuildingPrivilege));
					Subscribe(nameof(OnPlayerLostBuildingPrivilege));
				}
				_timer = timer.Once((float)(timeSpan.TotalSeconds + 1f), () =>
				{
					UnsubscribeHooks();
					TryDisplay();
				});
			}
			else
            {
				_timeStamp = 0d;
				_timer = timer.Once(Mathf.Max((float)(timeSpan.TotalSeconds - TimeSpan.FromDays(_config.Display_DaysLeft).TotalSeconds), 1f), () => { TryDisplay(); });
            }
		}
		
		private bool TryGetWipeSpan(out TimeSpan result)
        {
			var currentDate = DateTime.Now;
			if (TryGetCustomSpan(out result))
				return true;
			if (WipeTimer.serverinstance != null)
				result = WipeTimer.serverinstance.GetTimeSpanUntilWipe();
			else
			{
				PrintError("The custom date list is empty and the vanilla wipe date could not be obtained! The first Thursday of the next month will be used instead.");
				result = GetNextFirstThursdayOfMonth() - DateTime.UtcNow;
			}
			return result != TimeSpan.Zero;
			
			bool TryGetCustomSpan(out TimeSpan customSpan)
            {
				customSpan = TimeSpan.Zero;
                if (_config.CustomDates.Any())
                {
					var minDate = DateTime.MaxValue;
					foreach (var dateString in _config.CustomDates)
                    {
                        if (DateTime.TryParseExact(dateString, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) && date > currentDate && date < minDate)
                            minDate = date;
                    }
					if (minDate != DateTime.MaxValue)
                        customSpan = minDate.Subtract(currentDate);
                }
				return customSpan != TimeSpan.Zero;
			}
			
			DateTime GetNextFirstThursdayOfMonth()
            {
				var gmtTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
				var currentGMT = TimeZoneInfo.ConvertTime(currentDate, gmtTimeZone);
				int year = currentGMT.Year, month = currentGMT.Month;
				var firstOfMonthUtc = TimeZoneInfo.ConvertTimeToUtc(new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified), gmtTimeZone);
				
				var firstOfMonth = TimeZoneInfo.ConvertTimeFromUtc(firstOfMonthUtc, gmtTimeZone);
                int daysUntilThursday = ((int)DayOfWeek.Thursday - (int)firstOfMonth.DayOfWeek + 7) % 7;
				var firstThursday = firstOfMonth.AddDays(daysUntilThursday).Date.AddHours(19);
				
				var firstThursdayUtc = TimeZoneInfo.ConvertTimeToUtc(firstThursday, gmtTimeZone);
				if (firstThursdayUtc <= currentDate.ToUniversalTime())
                {
                    month++;
                    if (month > 12)
                    {
                        month = 1;
                        year++;
                    }
					
					firstOfMonthUtc = TimeZoneInfo.ConvertTimeToUtc(new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified), gmtTimeZone);
                    firstOfMonth = TimeZoneInfo.ConvertTimeFromUtc(firstOfMonthUtc, gmtTimeZone);
                    daysUntilThursday = ((int)DayOfWeek.Thursday - (int)firstOfMonth.DayOfWeek + 7) % 7;
                    firstThursday = firstOfMonth.AddDays(daysUntilThursday).Date.AddHours(19);
                    firstThursdayUtc = TimeZoneInfo.ConvertTimeToUtc(firstThursday, gmtTimeZone);
                }

                return firstThursdayUtc;
            }
		}

        private void UnsubscribeHooks()
		{
			for (int i = 0; i < _hooks.Length; i++)
				Unsubscribe(_hooks[i]);
		}
		
		private bool InBuildingPrivilege(ulong userID) => (bool)(AdvancedStatus?.Call(StatusInBuilding, userID) ?? false);
		private void DestroyBar(ulong userID) => AdvancedStatus?.Call(StatusDeleteBar, userID, BarID, Name);
		
		private static void SendMessage(IPlayer player, string text, bool isWarning = true)
        {
            if (_config.GameTips_Enabled && !player.IsServer)
                player.Command("gametip.showtoast", (int)(isWarning ? GameTip.Styles.Error : GameTip.Styles.Blue_Long), text, string.Empty);
            else
                player.Reply(text);
        }

        public static string ToLangKey(string langKey) => string.IsNullOrWhiteSpace(langKey) || langKey.Length != 2 || !langKey.All(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z') ? "en" : langKey.ToLower(System.Globalization.CultureInfo.InvariantCulture);
		#endregion

        #region ~Oxide Hooks~
        void OnPlayerConnected(BasePlayer player)
		{
			if (IsDisplayAllowed(player))
				SendBar(player.userID);
		}
		
		void OnPlayerLanguageChanged(BasePlayer player, string key)
		{
			if (IsDisplayAllowed(player))
			{
				AdvancedStatus?.Call(StatusUpdateContent, player.userID.Get(), new Dictionary<int, object>
				{
					{ 0, BarID },
					{ 1, Name },
					{ 15, lang.GetMessage(LangKey, this, player.UserIDString) }
				});
			}
		}
		
		void OnEntityEnter(TriggerSafeZone trigger, BasePlayer player)
		{
			if (player.userID.IsSteamId() && (!_playersSettings.TryGetValue(player.userID, out var canDisplay) || canDisplay))
				SendBar(player.userID);
		}
		
		void OnEntityLeave(TriggerSafeZone trigger, BasePlayer player)
		{
			if (player.userID.IsSteamId() && !player.InSafeZone() && !InBuildingPrivilege(player.userID))
				DestroyBar(player.userID);
		}
		
		void OnPlayerGainedBuildingPrivilege(BasePlayer player)
		{
            if (!_playersSettings.TryGetValue(player.userID, out var canDisplay) || canDisplay)
                SendBar(player.userID);
		}
		
		void OnPlayerLostBuildingPrivilege(BasePlayer player)
        {
			if (!player.HasPlayerFlag(BasePlayer.PlayerFlags.SafeZone))
				DestroyBar(player.userID);
		}
		
		void OnAdvancedStatusLoaded()
        {
			_statusIsLoaded = true;
			if (!string.IsNullOrWhiteSpace(_config.Status_Image_Local))
				AdvancedStatus?.Call("LoadImage", _config.Status_Image_Local);
			ToggleImageLib(_imgLibIsLoaded);
			TryDisplay();
		}
		
		void OnPluginLoaded(Plugin plugin)
        {
            if (plugin == ImageLibrary)
                ToggleImageLib(true);
        }
		
		void OnPluginUnloaded(Plugin plugin)
        {
			if (plugin.Name == "ImageLibrary")
				ToggleImageLib(false);
			else if (plugin.Name == "AdvancedStatus")
			{
				_statusIsLoaded = false;
				_timer?.Destroy();
                UnsubscribeHooks();
                _timeStamp = 0d;
			}
		}
		
		void Init()
		{
			UnsubscribeHooks();
			Unsubscribe(nameof(OnAdvancedStatusLoaded));
			permission.RegisterPermission(PERMISSION_ADMIN, this);
			AddCovalenceCommand(_config.Command, nameof(Command_Wipe));
			try { _playersSettings = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>(Name); }
			catch {}
			if (_playersSettings == null)
				_playersSettings = new Dictionary<ulong, bool>();
		}
		
		void OnServerInitialized(bool initial)
        {
			if (_config.Status_Bar_Type == "TimeProgressCounter")
				_statusBar[28] = (double)new DateTimeOffset(SaveRestore.SaveCreatedTime).ToUnixTimeMilliseconds() / 1000d;
			ToggleImageLib(ImageLibrary != null && ImageLibrary.IsLoaded);
			if (AdvancedStatus != null && AdvancedStatus?.Call("IsReady") != null)
				OnAdvancedStatusLoaded();
			else
			{
                if (initial && AdvancedStatus != null)
                    PrintWarning("AdvancedStatus plugin found, but not ready yet. Waiting for it to load...");
                else
                    PrintWarning("AdvancedStatus plugin not found! To function, it is necessary to install it!\n* https://codefling.com/plugins/advanced-status\n* https://lone.design/product/advanced-status/");
            }
			Subscribe(nameof(OnAdvancedStatusLoaded));
		}
        #endregion

        #region ~Commands~
        private readonly string[] _cmdKeysMain = { "bar", "time", "add", "clear" };
        private void Command_Wipe(IPlayer player, string command, string[] args)
        {
            bool isAdmin = player.IsAdmin || permission.UserHasPermission(player.Id, PERMISSION_ADMIN);
            int index = args != null && args.Length > 0 ? Array.FindIndex(_cmdKeysMain, key => key.Equals(args[0], StringComparison.OrdinalIgnoreCase)) : -1;
            if (index < 0)
            {
                player.Reply(lang.GetMessage(isAdmin ? "CmdMainAdmin" : "CmdMain", this, player.Id));
                return;
            }
			
			if (index == 0)
			{
				//bar
				if (player.Object is not BasePlayer bPlayer || bPlayer == null)
					player.Reply("This command is available to players only!");
				else
                {
					bool canDisplay;
					if (args.Length > 1 && bool.TryParse(args[1], out canDisplay))
						_playersSettings[bPlayer.userID] = canDisplay;
					else
					{
						if (!_playersSettings.TryGetValue(bPlayer.userID, out canDisplay))
							canDisplay = true;
						_playersSettings[bPlayer.userID] = canDisplay = !canDisplay;
					}
					if (_timeStamp > 0d)
                    {
                        if (IsDisplayAllowed(bPlayer))
                            SendBar(bPlayer.userID);
                        else
                            DestroyBar(bPlayer.userID);
                    }
                    SendMessage(player, lang.GetMessage(canDisplay ? "CmdBarEnabled" : "CmdBarDisabled", this, player.Id), !canDisplay);
				}
            }
			else if (!isAdmin)
				player.Reply(lang.GetMessage("CmdMain", this, player.Id));
			else
			{
                var nowDate = DateTime.Now;
                if (index == 1)//time
					SendMessage(player, string.Format(lang.GetMessage("CmdServerTime", this, player.Id), nowDate.ToString(TimeFormat, CultureInfo.InvariantCulture)), false);
				else if (index == 2)
                {
                    //add
					if (args.Length < 2 || DateTime.TryParseExact(args[1], TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var newDate) && newDate > nowDate)
						SendMessage(player, string.Format(lang.GetMessage("CmdNewDateAddFailed", this, player.Id), nowDate.AddDays(1).ToString(TimeFormat, CultureInfo.InvariantCulture)), true);
					else
					{
                        string dateString = args[1];
                        if (!_config.CustomDates.Contains(dateString))
                            _config.CustomDates.Add(dateString);
                        bool shouldDisplay = _timeStamp == 0d || newDate.Subtract(nowDate).TotalSeconds + Network.TimeEx.currentTimestamp < _timeStamp;
                        if (args.Length > 3 && int.TryParse(args[2], out var daysToAdd) && int.TryParse(args[3], out var total))
                        {
                            daysToAdd = Math.Min(daysToAdd, 365);
                            for (int i = 1; i < Math.Min(total, 100); i++)
                            {
                                newDate = newDate.AddDays(daysToAdd);
                                dateString = newDate.ToString(TimeFormat);
                                if (!_config.CustomDates.Contains(dateString))
                                    _config.CustomDates.Add(dateString);
                            }
							SendMessage(player, lang.GetMessage("CmdNewDateRangeAdded", this, player.Id), false);
						}
                        else
							SendMessage(player, string.Format(lang.GetMessage("CmdNewDateAdded", this, player.Id), dateString), false);
						SaveConfig();
                        if (shouldDisplay)
                            TryDisplay();
					}
				}
                else if (index == 3)
                {
					//clear
                    _config.CustomDates.Clear();
                    SaveConfig();
                    if (_timeStamp > 0d)
                    {
                        AdvancedStatus?.Call(StatusDeleteBarForAll, BarID, Name);
                        TryDisplay();
                    }
					SendMessage(player, lang.GetMessage("CmdClearDates", this, player.Id), false);
				}
            }
		}
		#endregion
		
		#region ~Unload~
		void Unload()
		{
			Interface.Oxide.DataFileSystem.WriteObject(Name, _playersSettings);
			if (_timer != null)
				_timer.Destroy();
			_config = null;
		}
		#endregion
	}
} 