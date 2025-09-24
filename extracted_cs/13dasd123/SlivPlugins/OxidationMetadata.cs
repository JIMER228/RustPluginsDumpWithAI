/**
 * OxidationMetadata - Server metadata configuration
 * Copyright (C) 2022-2023 kasvoton [kasvoton@stinkfist.org]
 *
 * All Rights Reserved.
 * DO NOT DISTRIBUTE THIS SOFTWARE.
 *
 * You should have received a copy of the EULA along with this software.
 * If not, see <https://oxidation.stinkfist.org/license/eula.txt>.
 *
 *
 *                #################################
 *               ###  I AM AVAILABLE FOR HIRING  ###
 *                #################################
 *
 * IF YOU WANT A CUSTOM PLUGIN FOR YOUR SERVER GET INTO CONTACT WITH ME SO
 * WE CAN DISCUSS YOUR NEED IN DETAIL. I CAN BUILD PLUGINS FROM SCRATCH OR
 * MODIFY EXISTING ONES DEPENDING ON THE COMPLEXITY.
 *
 */

using Facepunch;
using Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Plugins;
using Rust;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#pragma warning disable IDE0058, IDE0060
#pragma warning disable CA1304, CA1305, CA1311

namespace Oxide.Plugins
{
	[Info("OxidationMetadata", "codefling.com/kasvoton", "1.4.12")]

	public class OxidationMetadata : RustPlugin
	{
		private float _lastUpdate = UnityEngine.Time.realtimeSinceStartup + 10f;

		// --- UMOD EVENTS -------------------------------------------------------------------------------------------------------
		internal void OnServerInitialized()
		{
			try
			{
				if (Settings is null)
				{
					throw new InvalidOperationException();
				}

				_ = TimeZoneInfo.FindSystemTimeZoneById(Settings.Wipe.Timezone);

				// clean the server tags
				Settings.Server.Tags.RemoveAll(x => string.Equals(x, "monthly", StringComparison.OrdinalIgnoreCase));
				Settings.Server.Tags.RemoveAll(x => string.Equals(x, "biweekly", StringComparison.OrdinalIgnoreCase));
				Settings.Server.Tags.RemoveAll(x => string.Equals(x, "weekly", StringComparison.OrdinalIgnoreCase));

				SaveConfig();
			}
			catch (Exception e)
			{
				NextFrame(() =>
				{
					WriteError($"Issues were detected while parsing the config file, exiting..");
					WriteError($" - {e.Message}");
					Interface.Oxide.UnloadPlugin(Name);
				});
				return;
			}

			SetWipeSchedule();

			if (Settings.Enabled.CVarEnabled) { SetServerCVars(); }
			if (Settings.Enabled.MetadataEnabled) { SetServerMetadata(); }
			if (Settings.Enabled.PermissionsEnabled) { ProcessGroups(); }

			if (Settings.Enabled.LoadingMessages &&
				Settings.Loading.Normal != null && Settings.Loading.Normal.Count > 0 &&
				Settings.Loading.Queued != null && Settings.Loading.Queued.Count > 0)
			{
				timer.Every(
					0.25f, UpdateLoadingScreen);

				timer.Every(
					Settings.Loading.Period, UpdateMessageIndex);

				UpdateMessageIndex();
			}
			else
			{
				Unsubscribe("CanClientLogin");
			}

			if (Settings.Enabled.WelcomeMessages &&
				Settings.Welcome != null && Settings.Welcome.Count > 0)
			{
				Subscribe("OnPlayerConnected");
			}
			else
			{
				Unsubscribe("OnPlayerConnected");
			}

			if (Settings.Enabled.AdvertMessages &&
				Settings.Adverts.Messages != null && Settings.Adverts.Messages.Count > 0)
			{
				timer.Every(Settings.Adverts.Period, () =>
				{
					StringBuilder sb = new StringBuilder()
						.AppendLine("<color=#ff8353>%SHORTNAME%</color>")
						.AppendLine("<color=#ffc55c><size=12>%PUNCHLINE%</size></color>")
						.Append(Settings.Adverts.GetNextMessage());

					BroadcastServerMessage(sb.ToString());
				});
			}

			if (Settings.Enabled.DynamicSlots)
			{
				timer.Every(
					Settings.DynamicSlots.UpdateFrequency, UpdateSlotsTask);

				WriteWarning($"Dynamic slots are enabled"
					+ $" min:{Settings.DynamicSlots.Min}"
					+ $" max:{Settings.DynamicSlots.Max}"
					+ $" step:{Settings.DynamicSlots.StepSize}"
					+ $" hysteresis:{Settings.DynamicSlots.Hysteresis}"
					+ $" period:{Settings.DynamicSlots.UpdateFrequency}sec");
			}

			WriteInfo(StringTokens("Build Date: %BUILDATE% | Protocol: %PROTOCOL% | Branch: %BRANCH%"));
			WriteInfo(StringTokens("Now: %DATE% %TIME% (%TZ%) | Schedule: %WIPEFREQ% | Next: %NEXTWIPE_LONG% (%NEXTWIPE_DOW%)"));
		}

		internal void Unload()
		{
			// 🐜 BUGFIX
			// The native Facepunch code is splitting in a bad way the description field
			// which sometimes may lead to the full text not being updated.
			BugfixCleanDescription();
		}

		internal void OnServerInformationUpdated()
		{
			if (!SteamServer.GameTags.Contains(",oxidation"))
			{
				SteamServer.GameTags = $"{SteamServer.GameTags},oxidation";
			}

			if (!Settings.Enabled.CustomMapName || string.IsNullOrEmpty(Settings.Server.Map))
			{
				return;
			}

			SteamServer.MapName = Settings.Server.Map;
		}

		internal object CanClientLogin(Connection c)
		{
			if (!Settings.Enabled.LoadingMessages || Settings.Loading.Normal.Count == 0)
			{
				return null;
			}

			KeyValuePair<string, string> Message = Settings.Loading.Normal[LoadingIndex];

			NetWrite pkt = Net.sv.StartWrite();
			pkt.PacketID(Network.Message.Type.Message);
			pkt.String(Message.Key);
			pkt.String(Message.Value);
			pkt.Send(new SendInfo(c));

			return null;
		}

		internal void OnPlayerConnected(BasePlayer Player)
		{
			if (Settings.Welcome.Count == 0)
			{
				return;
			}

			timer.Once(10f, () =>
			{
				StringBuilder sb = new StringBuilder()
					.AppendLine("<color=#ff8353>%SHORTNAME%</color>")
					.AppendLine("<color=#ffc55c><size=12>%PUNCHLINE%</size></color>")
					.AppendLine(string.Join("\n", Settings.Welcome));
				SendServerMessage(Player, sb.ToString().Trim());
			});
		}

		internal void OnPlayerChat(BasePlayer player, string message, ConVar.Chat.ChatChannel channel)
		{
			switch (message)
			{
				case "!wipe":
					StringBuilder sb = new StringBuilder()
						.AppendLine("<color=#ff8353>%SHORTNAME%</color>")
						.AppendLine("<color=#ffc55c><size=12>%PUNCHLINE%</size></color>")
						.AppendLine("Last wipe was %LASTWIPE% (%LASTWIPE_DOW%)")
						.AppendLine("Next wipe will be %NEXTWIPE% (%NEXTWIPE_DOW%)")
						.AppendLine("Current server time is %DATE% %TIME% (%TZ%)");

					NextFrame(() =>
						BroadcastServerMessage(sb.ToString())
					);
					break;

				default:
					return;
			}
		}

		internal void OnPluginLoaded(Plugin name)
		{
			_lastUpdate = UnityEngine.Time.realtimeSinceStartup + 3f;
		}

		internal void OnPermissionRegistered(string name, Plugin owner)
		{
			_lastUpdate = UnityEngine.Time.realtimeSinceStartup + 1f;
		}

		// --- CVAR --------------------------------------------------------------------------------------------------------------
		internal static void SetCVar(ref string CVar, string Value)
		{
			if (string.IsNullOrEmpty(Value))
			{
				return;
			}

			CVar = Value;
		}

		internal static void RunCommand(string strCommand)
		{
			if (string.IsNullOrEmpty(strCommand))
			{
				return;
			}

			ConsoleSystem.Run(
				ConsoleSystem.Option.Server.Quiet(), strCommand);
		}

		private void SetServerCVars()
		{
			foreach (KeyValuePair<string, string> kvp in Settings.CVars)
			{
				if (ConsoleSystem.Index.Server.Dict.TryGetValue(kvp.Key.ToLower(), out ConsoleSystem.Command cvar))
				{
					WriteInfo($"[CVAR] set key:{kvp.Key} new:{kvp.Value} old:{cvar.String}");
					cvar.Set(kvp.Value);
					continue;
				}

				WriteError($"[CVAR] key:{kvp.Key} not found, check your configuration");
			}
		}

		// --- WIPE --------------------------------------------------------------------------------------------------------------
		private void SetWipeSchedule()
		{
			RunCommand($"wipetimer.wipedayofweek {Settings.Wipe.DoW}");
			RunCommand($"wipetimer.wipehourofday {Settings.Wipe.Hour}");
			RunCommand($"wipetimer.wipetimezone {Settings.Wipe.Timezone}");
			RunCommand($"wipetimer.wipeUnixTimestampOverride 0"); // reset

			long unix = GetNextWipeDate(GetLastWipeDate()).ToUnixTimeSeconds();
			RunCommand($"wipetimer.wipeUnixTimestampOverride {unix}");

			string frequency = Settings.Wipe.Frequency.ToString().ToLower();
			Settings.Server.Tags.Add(frequency);

			SetCVar(ref ConVar.Server.tags, string.Join(",", Settings.Server.Tags));
			WipeTimer.serverinstance.RecalculateWipeFrequency();
		}

		public (DateTimeOffset, TimeZoneInfo) GetToday()
		{
			TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(Settings.Wipe.Timezone);
			return (TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, tz), tz);
		}

		public DateTimeOffset GetLastWipeDate()
		{
			DateTime dt = DateTime.Parse(ConVar.Admin.ServerInfo().SaveCreatedTime);
			TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(Settings.Wipe.Timezone);
			return TimeZoneInfo.ConvertTime(dt, TimeZoneInfo.Local, tz);
		}

		public DateTimeOffset GetNextWipeDate(DateTimeOffset dto)
		{
			DateTimeOffset forced = dto, next = dto;
			(DateTimeOffset today, TimeZoneInfo tz) = GetToday();

			do
			{
				forced = GetForcedWipeTime(forced);
				Puts($" > forced: {forced}");
			} while (DateTimeOffset.Compare(today, forced) > 0);

			do
			{
				next = WipeTimer.serverinstance.GetWipeTime(next);
				Puts($" > next: {next}");
			} while (DateTimeOffset.Compare(today, next) > 0);

			List<DateTimeOffset> dates = new() {
				forced, next
			};

			dates.Sort((a, b) => a.CompareTo(b));
			return dates[0];
		}

		public DateTimeOffset GetForcedWipeTime(DateTimeOffset dto)
		{
			DateTime dt = dto.UtcDateTime;
			TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(Settings.Wipe.Timezone);

			dt = dt.AddMonths(1);
			dt = DateTime.SpecifyKind(new DateTime(dt.Year, dt.Month, 1, 19, 0, 0), DateTimeKind.Utc);
			dt = dt.AddDays(((int)DayOfWeek.Thursday - (int)dt.DayOfWeek + 7) % 7);

			return TimeZoneInfo.ConvertTimeFromUtc(dt, tz);
		}

		// --- METADATA ----------------------------------------------------------------------------------------------------------
		private void SetServerMetadata()
		{
			SetCVar(ref ConVar.Server.headerimage, Settings.Server.Header);
			SetCVar(ref ConVar.Server.logoimage, Settings.Server.Logo);
			SetCVar(ref ConVar.Server.url, Settings.Server.Website);

			SetCVar(ref ConVar.Server.hostname,
				StringTokens(Settings.Server.Name));

			SetCVar(ref ConVar.Server.tags,
				string.Join(",", Settings.Server.Tags));

			string Description = StringTokens(
				string.Join("\\n", Settings.Server.Description));

			if (Description.Length > 1600)
			{
				WriteError($"Server description too big ({Description.Length}) use at max 1600 chars.");
			}
			else
			{
				SetCVar(ref ConVar.Server.description, Description);
			}

			// Update the server metadata every 5m
			timer.In(5 * 60f, SetServerMetadata);
		}

		// --- SERVER MESSAGES ---------------------------------------------------------------------------------------------------
		private int LoadingIndex, QueuedIndex;
		private KeyValuePair<string, string> CachedLoadingMessage;
		private KeyValuePair<string, string> CachedQueuedMessage;

		internal void UpdateMessageIndex()
		{
			CachedLoadingMessage = Settings.Loading.Normal[LoadingIndex];
			LoadingIndex = (LoadingIndex + 1) % Settings.Loading.Normal.Count;

			CachedQueuedMessage = Settings.Loading.Queued[QueuedIndex];
			QueuedIndex = (QueuedIndex + 1) % Settings.Loading.Queued.Count;
		}

		internal void UpdateLoadingScreen()
		{
			try
			{
				ServerMgr Server = SingletonComponent<ServerMgr>.Instance;

				if (Server == null)
				{
					return;
				}

				foreach (Connection c in Server.connectionQueue.joining.ToList())
				{
					NetWrite pkt = Net.sv.StartWrite();
					pkt.PacketID(Message.Type.Message);
					pkt.String(CachedLoadingMessage.Key);
					pkt.String(CachedLoadingMessage.Value);
					pkt.Send(new SendInfo(c));
				}

				foreach (Connection c in Server.connectionQueue.queue.ToList())
				{
					int Total = ServerMgr.Instance.connectionQueue.Queued;
					int Ahead = ServerMgr.Instance.connectionQueue.queue.IndexOf(c);
					int Behind = Total - (Ahead + 1);

					NetWrite pkt = Net.sv.StartWrite();
					pkt.PacketID(Message.Type.Message);
					pkt.String(CachedQueuedMessage.Key);
					pkt.String(CachedQueuedMessage.Value
						.Replace("{AHEAD}", Ahead.ToString())
						.Replace("{BEHIND}", Behind.ToString())
						.Replace("{TOTAL}", Total.ToString())
						.Replace("{POSITION}", (Ahead + 1).ToString())
					);
					pkt.Send(new SendInfo(c));
				}
			}
			catch { }
		}

		// --- DYNAMIC SLOTS -----------------------------------------------------------------------------------------------------
		internal void UpdateSlotsTask()
		{
			if (!Settings.Enabled.DynamicSlots)
			{
				return;
			}

			int currentSlots = ConVar.Admin.ServerInfo().MaxPlayers;

			int activePlayers = ConVar.Admin.ServerInfo().Players
				+ ConVar.Admin.ServerInfo().Joining;

			int optimalSlots = GetOptimalNumberOfSlots(currentSlots,
				activePlayers, Settings.DynamicSlots.StepSize, Settings.DynamicSlots.Hysteresis);

			optimalSlots = UnityEngine.Mathf.Clamp(
				optimalSlots, Settings.DynamicSlots.Min, Settings.DynamicSlots.Max);

			if (currentSlots != optimalSlots)
			{
				ConVar.Server.maxplayers = optimalSlots;
				WriteInfo($"slots:{ConVar.Admin.ServerInfo().MaxPlayers} online:{ConVar.Admin.ServerInfo().Players} joining:{ConVar.Admin.ServerInfo().Joining} queued:{ConVar.Admin.ServerInfo().Queued}");
			}
		}

		internal static int GetOptimalNumberOfSlots(int currentSlots, int activePlayers, int stepSize, int hysteresis)
		{
			int optimalSlots = (int)Math.Ceiling((double)activePlayers / stepSize) * stepSize;

			if (optimalSlots < activePlayers)
			{
				optimalSlots += stepSize;
			}
			else if (optimalSlots - activePlayers <= hysteresis)
			{
				optimalSlots = activePlayers + hysteresis;
			}

			while (optimalSlots <= currentSlots)
			{
				optimalSlots += stepSize;
			}

			return optimalSlots;
		}

		// --- OXIDE GROUP AND PERMS ---------------------------------------------------------------------------------------------
		private void ProcessGroups()
		{
			// group cleanup
			foreach (string Group in permission.GetGroups())
			{
				if (Group is "default" or "admin")
				{
					continue;
				}

				if (Settings.Permissions.Groups.Select(x => x.Name).Contains(Group))
				{
					continue;
				}

				permission.RemoveGroup(Group);
				WriteWarning($"Removed unknown group {Group} from the server.");
			}

			// group creation and sync
			foreach (Configuration.PermissionsGroup.GroupItem Group in Settings.Permissions.Groups)
			{
				if (permission.GroupExists(Group.Name))
				{
					// just update the metadata
					permission.SetGroupRank(Group.Name, Group.Rank);
					permission.SetGroupTitle(Group.Name, Group.Title);
					WriteInfo($"Group {Group.Name} information updated.");
				}

				else
				{
					// create the group from scratch
					permission.CreateGroup(Group.Name, Group.Title, Group.Rank);
					WriteInfo($"Group {Group.Name} created.");
				}

				if (Group.Default)
				{
					permission.AddUserGroup("*", Group.Name);
				}
				else
				{
					permission.RemoveUserGroup("*", Group.Name);
				}
			}

			// group parenting and membership
			foreach (Configuration.PermissionsGroup.GroupItem Group in Settings.Permissions.Groups)
			{
				// add new members from the config
				foreach (string SteamID in Group.Members)
				{
					if (permission.UserHasGroup(SteamID, Group.Name))
					{
						continue;
					}

					permission.AddUserGroup(SteamID, Group.Name);
					WriteInfo($"Added {SteamID} to group {Group.Name}.");
				}

				// set group parent
				if (string.IsNullOrEmpty(Group.Parent))
				{
					continue;
				}

				permission.SetGroupParent(Group.Name, Group.Parent);
				WriteInfo($"Group {Group.Name} parent set to {Group.Parent}.");
			}

			WriteInfo($"Processing of group permission delayed..");
			timer.Once(10f, ProcessPermissions);
		}

		private void ProcessPermissions()
		{
			if (UnityEngine.Time.realtimeSinceStartup < _lastUpdate)
			{
				timer.Once(1f, ProcessPermissions);
				return;
			}

			foreach (Configuration.PermissionsGroup.GroupItem Group in Settings.Permissions.Groups)
			{
				permission.RevokeGroupPermission(Group.Name, "*");
				foreach (string Permission in Group.Permissions)
				{
					permission.GrantGroupPermission(Group.Name, Permission, null);

					if (!permission.PermissionExists(Permission))
					{
						WriteWarning($"Grant '{Permission}' to group '{Group.Name}': permission not found");
					}
					else
					{
						WriteInfo($"Granted '{Permission}' to group '{Group.Name}'");
					}
				}
			}
			WriteInfo($"All group permissions have been set.");
		}

		// --- BUGFIX ------------------------------------------------------------------------------------------------------------
		internal void BugfixCleanDescription()
		{
			SteamServer.SetKey("description_0", string.Empty);
			for (int i = 0; i < 0x10; i++)
			{
				SteamServer.SetKey(string.Format("description_{0:00}", i), string.Empty);
			}

			NextFrame(() =>
			{
				ConVar.Server.description = string.Empty;
			});
		}

		// --- STRINGS -----------------------------------------------------------------------------------------------------------
		internal string StringTokens(string message)
		{

			if (string.IsNullOrEmpty(message)) { return string.Empty; }

			string retval = message;

			if (retval.Contains("%NAME%"))
			{ retval = retval.Replace("%NAME%", Settings.Server.Name); }

			if (retval.Contains("%SHORTNAME%"))
			{ retval = retval.Replace("%SHORTNAME%", Settings.Server.ShortName); }

			if (retval.Contains("%PUNCHLINE%"))
			{ retval = retval.Replace("%PUNCHLINE%", Settings.Server.PunchLine); }

			if (retval.Contains("%FPS%"))
			{ retval = retval.Replace("%FPS%", $"{Performance.report.frameRateAverage:0.00}"); }

			if (retval.Contains("%MAXSLOTS%"))
			{ retval = retval.Replace("%MAXSLOTS%", $"{ConVar.Server.maxplayers}"); }

			if (retval.Contains("%ONLINE%"))
			{ retval = retval.Replace("%ONLINE%", $"{BasePlayer.activePlayerList.Count}"); }

			if (retval.Contains("%UPTIME%"))
			{ retval = retval.Replace("%UPTIME%", $"{(int)UnityEngine.Time.realtimeSinceStartup}"); }

			if (retval.Contains("%WORLDSEED%"))
			{ retval = retval.Replace("%WORLDSEED%", $"{World.Seed}"); }

			if (retval.Contains("%WORLDSIZE%"))
			{ retval = retval.Replace("%WORLDSIZE%", $"{World.Size}"); }

			if (retval.Contains("%WIPEFREQ%"))
			{ retval = retval.Replace("%WIPEFREQ%", $"{Settings.Wipe.Frequency}"); }

			if (retval.Contains("%DATE%") || retval.Contains("%TIME%") || retval.Contains("%TZ%"))
			{
				var (dto, tz) = GetToday();
				string date = dto.ToString(Settings.Date.Long);
				string time = dto.ToString("HH:mm");

				retval = retval.Replace("%DATE%", date);
				retval = retval.Replace("%TIME%", time);
				retval = retval.Replace("%TZ%", tz.DisplayName);
			}

			if (retval.Contains("%LASTWIPE%") || retval.Contains("%LASTWIPE_DOW%") || retval.Contains("%LASTWIPE_LONG%"))
			{
				DateTimeOffset last = GetLastWipeDate();
				retval = retval.Replace("%LASTWIPE_DOW%", GetShortDoW(last));
				retval = retval.Replace("%LASTWIPE%", last.ToString(Settings.Date.Short));
				retval = retval.Replace("%LASTWIPE_LONG%", last.ToString(Settings.Date.Long));
			}

			if (retval.Contains("%FORCEDWIPE%") || retval.Contains("%FORCEDWIPE_DOW%") || retval.Contains("%FORCEDWIPE_LONG%"))
			{
				DateTimeOffset forced = GetForcedWipeTime(DateTimeOffset.UtcNow);
				retval = retval.Replace("%FORCEDWIPE_DOW%", GetShortDoW(forced));
				retval = retval.Replace("%FORCEDWIPE%", forced.ToString(Settings.Date.Short));
				retval = retval.Replace("%FORCEDWIPE_LONG%", forced.ToString(Settings.Date.Long));
			}

			if (retval.Contains("%NEXTWIPE%") || retval.Contains("%NEXTWIPE_DOW%") || retval.Contains("%NEXTWIPE_LONG%"))
			{
				DateTimeOffset next = GetNextWipeDate(GetLastWipeDate());
				retval = retval.Replace("%NEXTWIPE_DOW%", GetShortDoW(next));
				retval = retval.Replace("%NEXTWIPE%", next.ToString(Settings.Date.Short));
				retval = retval.Replace("%NEXTWIPE_LONG%", next.ToString(Settings.Date.Long));
			}

			if (retval.Contains("%PROTOCOL%"))
			{ retval = retval.Replace("%PROTOCOL%", $"{Protocol.printable}"); }

			if (retval.Contains("%BUILDATE%"))
			{ retval = retval.Replace("%BUILDATE%", $"{BuildInfo.Current.BuildDate}"); }

			if (retval.Contains("%UNITYVER%"))
			{ retval = retval.Replace("%UNITYVER%", $"{UnityEngine.Application.unityVersion}"); }

			if (retval.Contains("%CHANGESET%"))
			{ retval = retval.Replace("%CHANGESET%", $"{BuildInfo.Current.Scm.ChangeId}"); }

			if (retval.Contains("%BRANCH%"))
			{ retval = retval.Replace("%BRANCH%", $"{BuildInfo.Current.Scm.Branch}"); }

			return retval;
		}

		public static string GetShortDoW(DateTimeOffset date)
		{
			return date.ToString("ddd").ToUpper();
		}

		// --- CONFIGURATION -----------------------------------------------------------------------------------------------------
		private Configuration Settings;

		protected override void LoadConfig()
		{
			try
			{
				base.LoadConfig();
				Settings = Config.ReadObject<Configuration>();
			}
			catch
			{
				LoadDefaultConfig();
			}
			finally
			{
				SaveConfig();
			}
		}

		protected override void LoadDefaultConfig()
		{
			PrintWarning($"Created a new default v{Version} config file");

			Settings = new Configuration();
			Settings.Server.Name = $"%LASTWIPE% | {ConVar.Server.hostname}";
			Settings.Server.PunchLine = "Running at %FPS% fps with %ONLINE% online players.";
			Settings.Server.ShortName = $"{ConVar.Server.hostname}";
			Settings.Server.Description.Add("Map size is %WORLDSIZE% using seed %WORLDSEED%.");
			Settings.Server.Description.Add("Last wipe was at %LASTWIPE_LONG%, Next wipe is at %NEXTWIPE_LONG%");
			Settings.Server.Description.Add("Server has %UPTIME% seconds uptime, running at %FPS% fps with %ONLINE%/%MAXSLOTS% players online.");

			Settings.CVars.Add("fps.limit", "32");
			Settings.CVars.Add("server.tickrate", "10");

			Settings.Server.Tags = new List<string>
			{
				"EU"
			};

			foreach (string Group in permission.GetGroups())
			{
				Settings.Permissions.Groups.Add(new Configuration.PermissionsGroup.GroupItem
				{
					Name = Group,
					Rank = permission.GetGroupRank(Group),
					Title = permission.GetGroupTitle(Group),
					Parent = permission.GetGroupParent(Group),
					Default = permission.UserHasGroup("*", Group),
					Permissions = permission.GetGroupPermissions(Group).ToList()
				});
			}

			Settings.Loading.Normal = new List<KeyValuePair<string, string>>
			{
				new("Welcome", "Loading.."),
				new("Welcome", "Please wait.."),
			};

			Settings.Loading.Queued = new List<KeyValuePair<string, string>>
			{
				new("Top queued message", "Bottom queued message"),
				new("Waiting", "ahead:{AHEAD} pos:{POSITION} behind:{BEHIND} total:{TOTAL}"),
			};

			Settings.Welcome = new List<string>
			{
				"Welcome to our humble server"
			};

			Settings.Adverts.Messages = new List<string>
			{
				"For more cool plugins checkout codefling.com/kasvoton"
			};
		}

		protected override void SaveConfig()
		{
			Config.WriteObject(Settings, true);
		}

		internal sealed class Configuration
		{
			public OptionsGroup Enabled
			{ get; set; } = new OptionsGroup();

			public ServerGroup Server
			{ get; set; } = new ServerGroup();

			public Dictionary<string, string> CVars
			{ get; set; } = new Dictionary<string, string>();

			public DynamicSlotsGroup DynamicSlots
			{ get; set; } = new DynamicSlotsGroup();

			public DateGroup Date
			{ get; set; } = new DateGroup();

			public ServerWipeGroup Wipe
			{ get; set; } = new ServerWipeGroup();

			public LoadingGroup Loading
			{ get; set; } = new LoadingGroup();

			public List<string> Welcome
			{ get; set; } = new List<string>();

			public AdvertGroup Adverts
			{ get; set; } = new AdvertGroup();

			public PermissionsGroup Permissions
			{ get; set; } = new PermissionsGroup();

			internal sealed class OptionsGroup
			{
				[JsonProperty("Adverts")]
				public bool AdvertMessages
				{ get; set; } = false;

				[JsonProperty("Custom Map Name")]
				public bool CustomMapName
				{ get; set; } = true;

				[JsonProperty("Dynamic Slots")]
				public bool DynamicSlots
				{ get; set; } = false;

				[JsonProperty("Loading Messages")]
				public bool LoadingMessages
				{ get; set; } = false;

				[JsonProperty("Server CVar")]
				public bool CVarEnabled
				{ get; set; } = true;

				[JsonProperty("Server Groups and Perms")]
				public bool PermissionsEnabled
				{ get; set; } = true;

				[JsonProperty("Server Metadata")]
				public bool MetadataEnabled
				{ get; set; } = true;

				[JsonProperty("Welcome Messages")]
				public bool WelcomeMessages
				{ get; set; } = false;
			}

			internal sealed class AdvertGroup
			{
				[JsonIgnore]
				public int Index
				{ get; private set; }

				public List<string> Messages
				{ get; set; }

				public int Period
				{ get; set; } = 300;

				public string GetNextMessage()
				{
					if (Index == Messages.Count)
					{
						Index = 0;
					}

					return Messages[Index++];
				}
			}

			internal sealed class DateGroup
			{
				public string Short
				{ get; set; } = "dd/MM";

				public string Long
				{ get; set; } = "dd/MM/yyyy";
			}

			internal sealed class LoadingGroup
			{
				public List<KeyValuePair<string, string>> Normal
				{ get; set; }

				public List<KeyValuePair<string, string>> Queued
				{ get; set; }

				public int Period
				{ get; set; } = 5;
			}

			internal sealed class PermissionsGroup
			{
				public List<GroupItem> Groups
				{ get; set; } = new List<GroupItem>();

				internal sealed class GroupItem
				{
					public string Name
					{ get; set; }

					public string Title
					{ get; set; }

					public string Parent
					{ get; set; }

					public int Rank
					{ get; set; }

					public bool Default
					{ get; set; }

					public List<string> Permissions
					{ get; set; } = new List<string>();

					public List<string> Members
					{ get; set; } = new List<string>();
				}
			}

			internal sealed class ServerGroup
			{
				public ulong SteamID
				{ get; set; } = 76561199079240903UL;

				public string Name
				{ get; set; }

				public string ShortName
				{ get; set; }

				public string PunchLine
				{ get; set; }

				public List<string> Description
				{ get; set; } = new List<string>();

				[JsonProperty("Banner image")]
				public string Header
				{ get; set; }

				[JsonProperty("Logo image")]
				public string Logo
				{ get; set; }

				public List<string> Tags
				{ get; set; } = new List<string>();

				public string Website
				{ get; set; }

				[JsonProperty("Custom map name")]
				public string Map
				{ get; set; } = "codefling.com/kasvoton";
			}

			internal sealed class DynamicSlotsGroup
			{
				public int Min
				{ get; set; } = 32;

				public int Max
				{ get; set; } = 128;

				public int StepSize
				{ get; set; } = 3;

				public int Hysteresis
				{ get; set; } = 3;

				public float UpdateFrequency
				{ get; set; } = 15 * 60f;
			}

			internal sealed class ServerWipeGroup
			{
				public int DoW { get; set; } = 4;
				public int Hour { get; set; } = 19;
				public string Timezone { get; set; } = "Europe/London";

				[JsonConverter(typeof(StringEnumConverter))]
				public WipeTimer.WipeFrequency Frequency { get; set; } = WipeTimer.WipeFrequency.Weekly;
			}
		}

		// --- LOGGING -----------------------------------------------------------------------------------------------------------
		internal void WriteInfo(string format, params object[] args)
		{
			Puts("[INFO] " + format, args);
		}

		internal void WriteWarning(string format, params object[] args)
		{
			Puts("[WARNING] " + format, args);
		}

		internal static void WriteError(string format, params object[] args)
		{
			Interface.Oxide.LogError("[ERROR] " + format, args);
		}

		internal void SendServerMessage(BasePlayer Player, string Message)
		{
			Player.SendConsoleCommand("chat.add", new object[]{
				ConVar.Chat.ChatChannel.Server, Settings.Server.SteamID, StringTokens(Message) });
		}

		internal void BroadcastServerMessage(string Message)
		{
			Server.Broadcast(StringTokens(Message), Settings.Server.SteamID);
		}
	}
}