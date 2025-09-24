using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("SkipNight", "Mevent", "1.0.5")]
	public class SkipNight : RustPlugin
	{
		#region Fields

		[PluginReference] private Plugin ImageLibrary = null;

		private static SkipNight _instance;

		private const string Layer = "UI.SkipNight";

		private SkipEngine _engine;

		#endregion

		#region Config

		private static Configuration _config;

		private class Configuration
		{
			[JsonProperty(PropertyName = "Time Settings")]
			public TimeSettings Time = new TimeSettings
			{
				DayStart = "08:00",
				NightStart = "20:00",
				TimeStart = "20:00",
				TimeEnd = "21:00",
				TimeSet = "08:00",
				TimeVote = 60,
				ForceSkip = true,
				LengthNight = 5,
				LengthFastNight = 2,
				LengthDay = 45,
				FullMoon = true,
				FullMoonDates = new List<DateTime>
				{
					new DateTime(2024, 1, 25),
					new DateTime(2024, 2, 24),
					new DateTime(2024, 3, 25),
					new DateTime(2024, 4, 23),
					new DateTime(2024, 5, 23),
					new DateTime(2024, 6, 21),
					new DateTime(2024, 7, 21),
					new DateTime(2024, 8, 19),
					new DateTime(2024, 9, 17),
					new DateTime(2024, 10, 17),
					new DateTime(2024, 11, 15),
					new DateTime(2024, 12, 15)
				}
			};

			[JsonProperty(PropertyName = "UI Settings")]
			public InterfaceSettings UI = new InterfaceSettings
			{
				InitialDisplay = true,
				DestroyTime = 5,
				ShowImage = true,
				Image = "https://i.imgur.com/uNSAY42.png",
				ImageWidth = 42,
				ImageHeight = 33,
				ImageUpIndent = 16,
				LeftIndent = 212,
				BottomIndent = 16,
				Width = 178,
				DefaultHeight = 82,
				UnfoldedHeight = 184,
				BackgroundColor = new IColor("#F8EBE3",
					4),
				BackgroundMaterial = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
				VotingButton = new InterfaceSettings.BtnInfo
				{
					Width = 150,
					Height = 30,
					BottomIndent = 15
				},
				ProgressBar = new InterfaceSettings.ProgressInfo
				{
					Width = 150,
					Height = 20,
					BottomIndent = 55
				},
				Colors = new InterfaceSettings.ColorsInfo
				{
					Color1 = new IColor("#ABE04E"),
					Color2 = new IColor("#595651",
						75),
					Color3 = new IColor("#74884A",
						95),
					Color4 = new IColor("#FFFFFF")
				},
				HideBtn = new InterfaceSettings.InterfacePosition
				{
					AnchorMin = "0 1", AnchorMax = "0 1",
					OffsetMin = "0 -20",
					OffsetMax = "20 0"
				},
				CloseBtn = new InterfaceSettings.BtnConf
				{
					Enabled = false,
					AnchorMin = "1 1", AnchorMax = "1 1",
					OffsetMin = "-20 -20",
					OffsetMax = "0 0"
				},
				CloseAfterVoting = false
			};

			[JsonProperty(PropertyName = "Votes Settings")]
			public VotesSettings Votes = new VotesSettings
			{
				VoteCommands = new[] {"voteday", "vt"},
				OnStartBroadcasting = false,
				VotesCount = 5,
				UsePercent = true,
				Percent = 30,
				CommandsAfterLostVoting = "example.cmd",
				CommandsAfterSuccessfulVoting = "cmd1|cmd2"
			};
		}

		private class VotesSettings
		{
			[JsonProperty(PropertyName = "Vote Commands", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public string[] VoteCommands;

			[JsonProperty(PropertyName = "Chat notification when voting starts")]
			public bool OnStartBroadcasting;

			[JsonProperty(PropertyName = "Number of votes")]
			public int VotesCount;

			[JsonProperty(PropertyName = "Use a percentage of the online?")]
			public bool UsePercent;

			[JsonProperty(PropertyName = "Percentage of the online")]
			public float Percent;

			[JsonProperty(PropertyName = "Commands after lost voting")]
			public string CommandsAfterLostVoting;

			[JsonProperty(PropertyName = "Commands after successful voting")]
			public string CommandsAfterSuccessfulVoting;

			public int GetVotesCount()
			{
				return UsePercent
					? Mathf.Max(Mathf.RoundToInt(Percent / 100f * BasePlayer.activePlayerList.Count), 1)
					: VotesCount;
			}

			public void StartCommands(bool successful)
			{
				var commands = successful ? CommandsAfterSuccessfulVoting : CommandsAfterLostVoting;

				foreach (var cmd in commands.Split('|')) StartCommand(cmd);
			}

			private void StartCommand(string command)
			{
				_instance?.Server.Command(command);
			}
		}

		private class InterfaceSettings
		{
			[JsonProperty(PropertyName = "Type of initial display (true - collapsed, false - expanded)")]
			public bool InitialDisplay;

			[JsonProperty(PropertyName = "Destroy Time")]
			public float DestroyTime;

			[JsonProperty(PropertyName = "Show Image?")]
			public bool ShowImage;

			[JsonProperty(PropertyName = "Image")] public string Image;

			[JsonProperty(PropertyName = "Image Width")]
			public float ImageWidth;

			[JsonProperty(PropertyName = "Image Height")]
			public float ImageHeight;

			[JsonProperty(PropertyName = "Image Up Indent")]
			public float ImageUpIndent;

			[JsonProperty(PropertyName = "Left Indent")]
			public float LeftIndent;

			[JsonProperty(PropertyName = "Bottom Indent")]
			public float BottomIndent;

			[JsonProperty(PropertyName = "Width")] public float Width;

			[JsonProperty(PropertyName = "Height for default version")]
			public float DefaultHeight;

			[JsonProperty(PropertyName = "Height for unfolded version")]
			public float UnfoldedHeight;

			[JsonProperty(PropertyName = "Background Color")]
			public IColor BackgroundColor;

			[JsonProperty(PropertyName = "Background Materal")]
			public string BackgroundMaterial;

			[JsonProperty(PropertyName = "Voting Button")]
			public BtnInfo VotingButton;

			[JsonProperty(PropertyName = "Progress Bar")]
			public ProgressInfo ProgressBar;

			[JsonProperty(PropertyName = "Colors")]
			public ColorsInfo Colors;

			[JsonProperty(PropertyName = "Hide button position")]
			public InterfacePosition HideBtn;

			[JsonProperty(PropertyName = "Close button position")]
			public BtnConf CloseBtn;

			[JsonProperty(PropertyName = "Close the interface after voting?")]
			public bool CloseAfterVoting;

			public class BtnConf : InterfacePosition
			{
				[JsonProperty(PropertyName = "Enabled")]
				public bool Enabled;
			}

			public class InterfacePosition
			{
				[JsonProperty(PropertyName = "AnchorMin")]
				public string AnchorMin;

				[JsonProperty(PropertyName = "AnchorMax")]
				public string AnchorMax;

				[JsonProperty(PropertyName = "OffsetMin")]
				public string OffsetMin;

				[JsonProperty(PropertyName = "OffsetMax")]
				public string OffsetMax;
			}

			public class ProgressInfo
			{
				[JsonProperty(PropertyName = "Width")] public float Width;

				[JsonProperty(PropertyName = "Height")]
				public float Height;

				[JsonProperty(PropertyName = "Bottom Indent")]
				public float BottomIndent;
			}

			public class BtnInfo
			{
				[JsonProperty(PropertyName = "Width")] public float Width;

				[JsonProperty(PropertyName = "Height")]
				public float Height;

				[JsonProperty(PropertyName = "Bottom Indent")]
				public float BottomIndent;
			}

			public class ColorsInfo
			{
				[JsonProperty(PropertyName = "Color 1")]
				public IColor Color1;

				[JsonProperty(PropertyName = "Color 2")]
				public IColor Color2;

				[JsonProperty(PropertyName = "Color 3")]
				public IColor Color3;

				[JsonProperty(PropertyName = "Color 4")]
				public IColor Color4;
			}
		}

		private class IColor
		{
			[JsonProperty(PropertyName = "HEX")] public string Hex;

			[JsonProperty(PropertyName = "Opacity (0 - 100)")]
			public readonly float Alpha;

			[JsonIgnore] private string _color;

			[JsonIgnore]
			public string Get
			{
				get
				{
					if (string.IsNullOrEmpty(_color))
						_color = GetColor();

					return _color;
				}
			}

			private string GetColor()
			{
				if (string.IsNullOrEmpty(Hex)) Hex = "#FFFFFF";

				var str = Hex.Trim('#');
				if (str.Length != 6) throw new Exception(Hex);
				var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
				var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
				var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);

				return $"{(double) r / 255} {(double) g / 255} {(double) b / 255} {Alpha / 100}";
			}

			public IColor()
			{
			}

			public IColor(string hex, float alpha = 100)
			{
				Hex = hex;
				Alpha = alpha;
			}
		}

		private class TimeSettings
		{
			[JsonProperty(PropertyName = "Day Start")]
			public string DayStart;

			[JsonProperty(PropertyName = "Night Start")]
			public string NightStart;

			[JsonProperty(PropertyName = "Voting time")]
			public short TimeVote;

			[JsonProperty(PropertyName = "Voting start time (time to check)")]
			public string TimeStart;

			[JsonProperty(PropertyName = "Time until which hour the voting will take place (time to check)")]
			public string TimeEnd;

			[JsonProperty(PropertyName = "Time after voting (to which the night passes)")]
			public string TimeSet;

			[JsonProperty(PropertyName = "Fast skip the night")]
			public bool ForceSkip;

			[JsonProperty(PropertyName = "Length of the night (minutes)")]
			public float LengthNight;

			[JsonProperty(PropertyName = "Length of the FAST night (minutes)")]
			public float LengthFastNight;

			[JsonProperty(PropertyName = "Length of the day (minutes)")]
			public float LengthDay;

			[JsonProperty(PropertyName = "Night with a full moon")]
			public bool FullMoon;

			[JsonProperty(PropertyName = "Full Moon Dates", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<DateTime> FullMoonDates;

			[JsonIgnore] public TimeSpan DayStartHours;
			[JsonIgnore] public TimeSpan NightStartHours;
			[JsonIgnore] public TimeSpan TimeStartHours;
			[JsonIgnore] public TimeSpan TimeEndHours;

			public void Init()
			{
				DayStartHours = TimeSpan.Parse(_config.Time.DayStart);
				NightStartHours = TimeSpan.Parse(_config.Time.NightStart);
				TimeStartHours = TimeSpan.Parse(_config.Time.TimeStart);
				TimeEndHours = TimeSpan.Parse(_config.Time.TimeEnd);
			}
		}

		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				_config = Config.ReadObject<Configuration>();
				if (_config == null) throw new Exception();
			}
			catch (Exception ex)
			{
				PrintError("Your configuration file contains an error. Using default configuration values.");
				LoadDefaultConfig();
				Debug.LogException(ex);
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

		#region Hooks

		private void Init()
		{
			_instance = this;
		}

		private void OnServerInitialized()
		{
			LoadImages();

			_config.Time.Init();

			InitEngine();

			RegisterCommands();
		}

		private void Unload()
		{
			DestroyEngine();

			foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, Layer);

			_instance = null;
			_config = null;
		}

		#endregion

		#region Commands

		private void CmdVote(IPlayer cov, string command, string[] args)
		{
			var player = cov?.Object as BasePlayer;
			if (player == null) return;

			_engine.Vote(player);

			Reply(player, MsgVoted);
		}

		[ConsoleCommand("UI_SkipNight")]
		private void CmdSkipNight(ConsoleSystem.Arg arg)
		{
			var player = arg.Player();
			if (player == null || !arg.HasArgs()) return;

			switch (arg.Args[0])
			{
				case "vote":
				{
					_engine.Vote(player);
					break;
				}

				case "hide":
				{
					_engine.SwitchHide(player);
					break;
				}

				case "close":
				{
					_engine.Close(player);
					break;
				}
			}
		}

		#endregion

		#region Component

		private class SkipEngine : FacepunchBehaviour
		{
			#region Fields

			private CuiElementContainer _tempContainer = new CuiElementContainer();

			private float _startVoteTime;

			private int needVotes;

			private bool _finished = true;

			private uint componentSearchAttempts;

			private TOD_Time timeComponent;

			private bool isDay;

			private TimeSpan CurrentTime => TOD_Sky.Instance.Cycle.DateTime.TimeOfDay;

			#endregion

			#region Init

			private void Awake()
			{
				GetTimeComponent();
			}

			private void GetTimeComponent()
			{
				if (TOD_Sky.Instance == null)
				{
					++componentSearchAttempts;
					if (componentSearchAttempts < 50)
					{
						Invoke(GetTimeComponent, 3);
						return;
					}
				}

				timeComponent = TOD_Sky.Instance.Components.Time;

				if (timeComponent == null)
				{
					_instance.RaiseError("Could not fetch time component. Plugin will not work without it.");
					return;
				}

				timeComponent.OnMinute += OnMinute;

				OnMinute();
			}

			#endregion

			#region Main

			private void StartVote()
			{
				if (!_finished)
					return;

				CancelInvoke();

				_startVoteTime = Time.time;

				_finished = false;

				needVotes = _config.Votes.GetVotesCount();

				if (_config.UI.InitialDisplay)
					_openedUi = new HashSet<BasePlayer>(BasePlayer.activePlayerList);

				if (_config.Votes.OnStartBroadcasting)
					foreach (var player in BasePlayer.activePlayerList)
						_instance.Reply(player, VotingBroadcast);

				DrawUIs();

				InvokeRepeating(UpdatePlayers, 1, 1);
			}

			private void StopVote()
			{
				CancelInvoke();

				if (_finished)
					return;

				_finished = true;

				var canBeSkip = CanBeSkip();
				if (canBeSkip)
					StartSkip();

				_config.Votes.StartCommands(canBeSkip);

				DrawUIs();

				Invoke(DestroyUIs, _config.UI.DestroyTime);
			}

			private void UpdatePlayers()
			{
				if (_startVoteTime + _config.Time.TimeVote - Time.time <= 0)
				{
					StopVote();
					return;
				}

				foreach (var player in _openedUi.ToArray())
				{
					_tempContainer.Clear();

					ProgressUi(ref _tempContainer, player);

					TimerUi(ref _tempContainer, player);

					CuiHelper.AddUi(player, _tempContainer);
				}
			}

			#endregion

			#region Interface

			private void MainUi(BasePlayer player)
			{
				var container = new CuiElementContainer();

				var hideVersion = IsHided(player);

				var voted = IsVoted(player);

				if (hideVersion)
				{
					container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "1 0", AnchorMax = "1 0",
							OffsetMin = $"-{_config.UI.LeftIndent + _config.UI.Width} {_config.UI.BottomIndent}",
							OffsetMax = $"-{_config.UI.LeftIndent} {_config.UI.BottomIndent + _config.UI.DefaultHeight}"
						},
						Image =
						{
							Color = _config.UI.BackgroundColor.Get,
							Material = _config.UI.BackgroundMaterial
						}
					}, "Overlay", Layer);

					container.Add(new CuiLabel
					{
						RectTransform =
						{
							AnchorMin = "0 1", AnchorMax = "1 1",
							OffsetMin = "0 -35",
							OffsetMax = "0 0"
						},
						Text =
						{
							Text = Msg(player, TitleSkipNight),
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-bold.ttf",
							FontSize = 14,
							Color = _config.UI.Colors.Color4.Get
						}
					}, Layer);
				}
				else
				{
					container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "1 0", AnchorMax = "1 0",
							OffsetMin = $"-{_config.UI.LeftIndent + _config.UI.Width} {_config.UI.BottomIndent}",
							OffsetMax =
								$"-{_config.UI.LeftIndent} {_config.UI.BottomIndent + _config.UI.UnfoldedHeight}"
						},
						Image =
						{
							Color = _config.UI.BackgroundColor.Get,
							Material = _config.UI.BackgroundMaterial
						}
					}, "Overlay", Layer);

					if (_config.UI.ShowImage)
						container.Add(new CuiElement
						{
							Parent = Layer,
							Components =
							{
								new CuiRawImageComponent
								{
									Png = _instance.GetImage(_config.UI.Image)
								},
								new CuiRectTransformComponent
								{
									AnchorMin = "0.5 1", AnchorMax = "0.5 1",
									OffsetMin =
										$"-{_config.UI.ImageWidth / 2f} -{_config.UI.ImageUpIndent + _config.UI.ImageHeight}",
									OffsetMax = $"{_config.UI.ImageWidth / 2f} -{_config.UI.ImageUpIndent}"
								}
							}
						});

					ProgressUi(ref container, player);

					TimerUi(ref container, player);
				}

				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = "0.5 0", AnchorMax = "0.5 0",
						OffsetMin = $"-{_config.UI.VotingButton.Width / 2f} {_config.UI.VotingButton.BottomIndent}",
						OffsetMax =
							$"{_config.UI.VotingButton.Width / 2f} {_config.UI.VotingButton.BottomIndent + _config.UI.VotingButton.Height}"
					},
					Text =
					{
						Text = _finished ? CanBeSkip() ? Msg(player, WillBeSkip) : Msg(player, VoteCancelled)
							: voted ? Msg(player, YouVoted) : Msg(player, VoteBtn),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-bold.ttf",
						FontSize = 14,
						Color = _config.UI.Colors.Color1.Get
					},
					Button =
					{
						Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
						Command = voted ? "" : "UI_SkipNight vote",
						Color = voted
							? _config.UI.Colors.Color2.Get
							: _config.UI.Colors.Color3.Get
					}
				}, Layer, Layer + ".VoteBtn");

				container.Add(new CuiButton
				{
					RectTransform =
					{
						AnchorMin = _config.UI.HideBtn.AnchorMin,
						AnchorMax = _config.UI.HideBtn.AnchorMax,
						OffsetMin = _config.UI.HideBtn.OffsetMin,
						OffsetMax = _config.UI.HideBtn.OffsetMax
					},
					Text =
					{
						Text = hideVersion ? Msg(player, UnHideBtn) : Msg(player, HideBtn),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-regular.ttf",
						FontSize = 15,
						Color = _config.UI.Colors.Color4.Get
					},
					Button =
					{
						Color = "0 0 0 0",
						Command = "UI_SkipNight hide"
					}
				}, Layer);

				if (_config.UI.CloseBtn.Enabled)
					container.Add(new CuiButton
					{
						RectTransform =
						{
							AnchorMin = _config.UI.CloseBtn.AnchorMin,
							AnchorMax = _config.UI.CloseBtn.AnchorMax,
							OffsetMin = _config.UI.CloseBtn.OffsetMin,
							OffsetMax = _config.UI.CloseBtn.OffsetMax
						},
						Text =
						{
							Text = Msg(player, CloseBtn),
							Align = TextAnchor.MiddleCenter,
							Font = "robotocondensed-regular.ttf",
							FontSize = 15,
							Color = _config.UI.Colors.Color4.Get
						},
						Button =
						{
							Color = "0 0 0 0",
							Command = "UI_SkipNight close"
						}
					}, Layer);

				CuiHelper.DestroyUi(player, Layer);
				CuiHelper.AddUi(player, container);
			}

			private void TimerUi(ref CuiElementContainer container, BasePlayer player)
			{
				container.Add(new CuiLabel
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 0",
						OffsetMin = "0 85",
						OffsetMax = "0 120"
					},
					Text =
					{
						Text = Msg(player, MainTitle, FormatTime(_startVoteTime + _config.Time.TimeVote - Time.time)),
						Align = TextAnchor.MiddleCenter,
						Font = "robotocondensed-bold.ttf",
						FontSize = 14,
						Color = _config.UI.Colors.Color4.Get
					}
				}, Layer, Layer + ".Timer");

				CuiHelper.DestroyUi(player, Layer + ".Timer");
			}

			private void ProgressUi(ref CuiElementContainer container, BasePlayer player)
			{
				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0.5 0", AnchorMax = "0.5 0",
						OffsetMin = $"-{_config.UI.ProgressBar.Width / 2f} {_config.UI.ProgressBar.BottomIndent}",
						OffsetMax =
							$"{_config.UI.ProgressBar.Width / 2f} {_config.UI.ProgressBar.BottomIndent + _config.UI.ProgressBar.Height}"
					},
					Image =
					{
						Color = _config.UI.Colors.Color3.Get
					}
				}, Layer, Layer + ".Progress");

				var progress = _votedPlayers.Count / (float) needVotes;
				if (progress > 0)
					container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "0 0", AnchorMax = $"{progress} 1",
							OffsetMin = "2 2", OffsetMax = "-2 -2"
						},
						Image =
						{
							Color = _config.UI.Colors.Color1.Get
						}
					}, Layer + ".Progress");

				container.Add(new CuiElement
				{
					Parent = Layer + ".Progress",
					Components =
					{
						new CuiTextComponent
						{
							Text = Msg(player, NeedVotes),
							Align = TextAnchor.MiddleLeft,
							Font = "robotocondensed-bold.ttf",
							FontSize = 10,
							Color = "1 1 1"
						},
						new CuiRectTransformComponent
						{
							AnchorMin = "0 0", AnchorMax = "1 1",
							OffsetMin = "5 0", OffsetMax = "-5 0"
						}
					}
				});

				container.Add(new CuiLabel
				{
					RectTransform =
					{
						AnchorMin = "0 0", AnchorMax = "1 1",
						OffsetMin = "5 0", OffsetMax = "-5 0"
					},
					Text =
					{
						Text = $"{needVotes - _votedPlayers.Count}",
						Align = TextAnchor.MiddleRight,
						Font = "robotocondensed-bold.ttf",
						FontSize = 10,
						Color = _config.UI.Colors.Color4.Get
					}
				}, Layer + ".Progress");

				CuiHelper.DestroyUi(player, Layer + ".Progress");
			}

			#endregion

			#region Helpers

			private void DrawUIs()
			{
				foreach (var player in BasePlayer.activePlayerList) MainUi(player);
			}

			private void DestroyUIs()
			{
				CancelInvoke();

				_votedPlayers.Clear();
				_openedUi.Clear();

				foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, Layer);
			}

			private HashSet<BasePlayer> _openedUi = new HashSet<BasePlayer>();

			private bool IsHided(BasePlayer player)
			{
				return !_openedUi.Contains(player);
			}

			public void SwitchHide(BasePlayer player)
			{
				if (!_openedUi.Remove(player))
					_openedUi.Add(player);

				MainUi(player);
			}

			public void Close(BasePlayer player)
			{
				_openedUi.Remove(player);

				CuiHelper.DestroyUi(player, Layer);
			}

			private HashSet<BasePlayer> _votedPlayers = new HashSet<BasePlayer>();

			private bool IsVoted(BasePlayer player)
			{
				return _votedPlayers.Contains(player);
			}

			public void Vote(BasePlayer player)
			{
				if (_votedPlayers.Count < needVotes)
					_votedPlayers.Add(player);

				MainUi(player);

				if (CanBeSkip())
				{
					StopVote();
				}
				else
				{
					if (_config.UI.CloseAfterVoting)
						Invoke(() => Close(player), _config.UI.DestroyTime);
				}
			}

			private bool CanBeSkip()
			{
				return _votedPlayers.Count == needVotes;
			}

			private void StartSkip()
			{
				if (_config.Time.ForceSkip)
				{
					TOD_Sky.Instance.Cycle.Hour = (float) TimeSpan.Parse(_config.Time.TimeSet).TotalHours;
					return;
				}

				if (_config.Time.FullMoon)
					SetFullMoon();

				isDay = false;
				UpdateDayLenght(_config.Time.LengthFastNight, true);
				Interface.Oxide.CallHook("OnNightStart");
			}

			private void SetFullMoon()
			{
				TOD_Sky.Instance.Cycle.DateTime = _config.Time.FullMoonDates.GetRandom();
			}

			public void OnMinute()
			{
				if (CurrentTime >= _config.Time.TimeStartHours &&
				    CurrentTime < _config.Time.TimeEndHours)
				{
					StartVote();
					return;
				}

				if (_config.Time.DayStartHours <= CurrentTime &&
				    CurrentTime < _config.Time.NightStartHours)
				{
					if (isDay) return;
					isDay = true;
					UpdateDayLenght(_config.Time.LengthDay, false);
					Interface.Oxide.CallHook("OnDayStart");
				}
				else
				{
					if (!isDay) return;
					isDay = false;
					UpdateDayLenght(_config.Time.LengthNight, true);
					Interface.Oxide.CallHook("OnNightStart");
				}
			}

			private void UpdateDayLenght(float Lenght, bool night)
			{
				var dif = (float) (_config.Time.NightStartHours -
				                   _config.Time.DayStartHours).TotalHours;

				if (night) dif = 24 - dif;

				var part = 24.0f / dif;

				var newLenght = part * Lenght;

				if (newLenght <= 0) newLenght = 0.1f;

				timeComponent.DayLengthInMinutes = newLenght;
			}

			#endregion

			#region Destroy

			public void Kill()
			{
				DestroyImmediate(this);
			}

			private void OnDestroy()
			{
				CancelInvoke();

				if (timeComponent != null)
					timeComponent.OnMinute -= OnMinute;

				Destroy(gameObject);
				Destroy(this);
			}

			#endregion
		}

		#endregion

		#region Utils

		private void InitEngine()
		{
			_engine = new GameObject().AddComponent<SkipEngine>();
		}

		private void DestroyEngine()
		{
			if (_engine != null)
				_engine.Kill();
		}

		private static string FormatTime(double seconds)
		{
			var time = TimeSpan.FromSeconds(seconds);

			var result = new List<int>();
			if (time.Days != 0)
				result.Add(time.Days);

			if (time.Hours != 0)
				result.Add(time.Hours);

			if (time.Minutes != 0)
				result.Add(time.Minutes);

			if (time.Seconds != 0)
				result.Add(time.Seconds);

			return string.Join(":", result.Select(x => x.ToString()));
		}

		private void LoadImages()
		{
			if (ImageLibrary == null || !ImageLibrary.IsLoaded)
			{
				PrintError("IMAGE LIBRARY IS NOT INSTALLED!");
			}
			else
			{
				var imagesList = new Dictionary<string, string>();

				if (_config.UI.ShowImage)
					AddImage(ref imagesList, _config.UI.Image);

				ImageLibrary?.Call("ImportImageList", Title, imagesList, 0UL, true);
			}
		}

		private void RegisterCommands()
		{
			AddCovalenceCommand(_config.Votes.VoteCommands, nameof(CmdVote));
		}

		private void AddImage(ref Dictionary<string, string> imagesList, string image)
		{
			if (!string.IsNullOrEmpty(image) && !imagesList.ContainsKey(image))
				imagesList.Add(image, image);
		}

		private string GetImage(string image, ulong skin = 0)
		{
			return ImageLibrary.Call<string>("GetImage", image, skin);
		}

		#endregion

		#region Lang

		private const string
			VotingBroadcast = "VotingBroadcast",
			CloseBtn = "CloseBtn",
			MsgVoted = "MsgVoted",
			NeedVotes = "NeedVotes",
			MainTitle = "MainTitle",
			UnHideBtn = "UnHideBtn",
			HideBtn = "HideBtn",
			VoteBtn = "VoteBtn",
			YouVoted = "YouVoted",
			VoteCancelled = "VoteCancelled",
			WillBeSkip = "WillBeSkip",
			TitleSkipNight = "TitleSkipNight";

		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				[TitleSkipNight] = "Skip Night",
				[WillBeSkip] = "The night will be skipped",
				[VoteCancelled] = "Voting canceled",
				[YouVoted] = "You voted",
				[VoteBtn] = "Vote",
				[HideBtn] = "▼",
				[UnHideBtn] = "▲",
				[MainTitle] = "Skip Night:\n{0}",
				[NeedVotes] = "Votes required",
				[MsgVoted] = "You have voted to skip night",
				[VotingBroadcast] = "Voting has begun for skip the night!",
				[CloseBtn] = "✕"
			}, this);

			lang.RegisterMessages(new Dictionary<string, string>
			{
				[TitleSkipNight] = "Пропустить ночь",
				[WillBeSkip] = "Ночь будет пропущена",
				[VoteCancelled] = "Голосование отменено",
				[YouVoted] = "Вы проголосовали",
				[VoteBtn] = "Проголосовать",
				[HideBtn] = "▼",
				[UnHideBtn] = "▲",
				[MainTitle] = "Пропустить ночь:\n{0}",
				[NeedVotes] = "Требуется голосов",
				[VotingBroadcast] = "Началось голосование за пропуск ночи!",
				[CloseBtn] = "✕"
			}, this, "ru");
		}

		private static string Msg(string key, string userid = null, params object[] obj)
		{
			return string.Format(_instance.lang.GetMessage(key, _instance, userid), obj);
		}

		private static string Msg(BasePlayer player, string key, params object[] obj)
		{
			return string.Format(_instance.lang.GetMessage(key, _instance, player.UserIDString), obj);
		}

		private void Reply(BasePlayer player, string key, params object[] obj)
		{
			SendReply(player, Msg(player, key, obj));
		}

		#endregion
	}
}