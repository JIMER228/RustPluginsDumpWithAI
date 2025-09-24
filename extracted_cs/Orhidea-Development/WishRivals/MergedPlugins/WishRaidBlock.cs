//Reference: WishInfrastructure
#define DEBUG
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


//WishRaidBlock created with PluginMerge v(1.0.4.0) by MJSU @ https://github.com/dassjosh/Plugin.Merge
namespace Oxide.Plugins
{
    [Info("WishRaidBlock", "Latvish", "0.0.1")]
    [Description("WishRaidBlock")]
    public partial class WishRaidBlock : RustPlugin
    {
        #region ChatCommands.cs
        [ChatCommand("raidblock")]
        private void raidblock(BasePlayer player, string command, string[] args)
        {
            PrintToChat(player, lang.GetMessage("nodamage", this, player.UserIDString)
            .Replace("{starttime}", ShowTime(_raidBlockService.GetStartTime()))
            .Replace("{endtime}", ShowTime(_raidBlockService.GetEndTime())));
            
            if (!permission.UserHasPermission(player.UserIDString, adminPriv))
            return;
            
            if (args.Count() > 0)
            {
                
                
                if (args[0].ToLower() == "activate")
                {
                    _raidBlockService.Enable();
                    PrintToChat(lang.GetMessage("activate", this));
                    return;
                }
                
                if (args[0].ToLower() == "deactivate")
                {
                    _raidBlockService.Disable();
                    PrintToChat(lang.GetMessage("deactivate", this));
                    return;
                }
            }
        }
        #endregion

        #region Events.cs
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (entity == null) return null;
                if (info == null) return null;
                if (info.InitiatorPlayer == null) return null;
                if (!_raidBlockService.IsOn()) return null;                  // check if on or off
                if (entity is BasePlayer) return null;                          // damage to players ok!!
                if (entity.OwnerID == 0) return null;
                if (entity.OwnerID == info.InitiatorPlayer.userID) return null; // owner can damage own stuff
                //Allow attacking teams stuff
                if (info.InitiatorPlayer.Team != null)
                {
                    if (info.InitiatorPlayer.Team.teamID != 0UL)
                    {
                        if (info.InitiatorPlayer.Team?.members?.Any(teamUserId => teamUserId == entity.OwnerID) == true)
                        {
                            return null;
                        }
                    }
                }
                
                
                if (info.InitiatorPlayer != null)
                {
                    
                    if ((bool)Config["RaidBlockInformPlayer"] == true)
                    {
                        if (!_raidBlockService.IsForceActivated())
                        {
                            PrintToChat(info.InitiatorPlayer, lang.GetMessage("nodamage", this, info.InitiatorPlayer.UserIDString)
                            .Replace("{starttime}", ShowTime(_raidBlockService.GetStartTime()))
                            .Replace("{endtime}", ShowTime(_raidBlockService.GetEndTime())));
                            
                        }
                        else
                        {
                            PrintToChat(info.InitiatorPlayer, lang.GetMessage("activated", this, info.InitiatorPlayer.UserIDString));
                        }
                    }
                }
                
                info.damageTypes.ScaleAll(0.0f);                                // no damage
                return false;
            }
            catch (Exception ex)
            {
                PrintError("Error OnEntityTakeDamage: " + ex.Message);
                
            }
            return null;
        }
        #endregion

        #region WishRaidBlock.cs
        const string adminPriv = "Raidblock.admin";
        private RaidBlockService _raidBlockService;
        void Init()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Interface.Oxide.LogDebug($"START Init WishRaidBlock");
            
            LoadConfig();
            _raidBlockService = new RaidBlockService(Config);
            LoadRaidBlock();
            SubscribeToEvents();
            
            stopwatch.Stop();
            GuiService guiService = new GuiService();
            timer.Every(30, () =>
            {
                _raidBlockService.UpdateIsOnUsingConfigTime();
                if (_raidBlockService.IsOn())
                {
                    Interface.Oxide.LogDebug("Raidlock active, enabling UI");
                    
                    guiService.ActivateGui();
                }
                else
                {
                    Interface.Oxide.LogDebug("Raidlock disabled, destroying UI");
                    guiService.DestroyGui();
                }
            });
            Interface.Oxide.LogDebug($"END Init WishRaidBlock {stopwatch.ElapsedMilliseconds}ms");
            
        }
        void LoadRaidBlock()
        {
            try
            {
                RegisterMessages();
                
                if ((bool)Config["RaidBlockOn"])
                {
                    PrintWarning("Raid block starts at " + ShowTime(_raidBlockService.GetStartTime()));
                    PrintWarning("Raid block ends at  " + ShowTime(_raidBlockService.GetEndTime()));
                }
                else
                {
                    PrintWarning("Raid block is off.");
                }
                permission.RegisterPermission(adminPriv, this);
                
                PrintWarning(adminPriv + " privilidge is registered.");
                
            }
            catch (Exception ex)
            {
                PrintError($"Error Loaded: {ex.StackTrace}");
            }
            
        }
        protected override void LoadDefaultConfig() // Only called if the config file does not already exist
        {
            Interface.Oxide.LogWarning("Creating a new configuration file.");
            
            Config.Clear();
            
            Config["RaidBlockStart"] = "23:30";           // 8:30 AM
            Config["RaidBlockEnd"] = "12:00";
            Config["RaidBlockOn"] = true;
            Config["RaidBlockInformPlayer"] = true;
            SaveConfig();
        }
        private void RegisterMessages()
        {
            MessageRegister messageRegister = new MessageRegister(this);
            messageRegister.Register();
        }
        
        private void SubscribeToEvents()
        {
            Subscribe("OnEntityTakeDamage");
        }
        
        public string ShowTime(TimeSpan TimeIn)
        {
            return TimeIn.ToString(@"hh\:mm");
        }
        #endregion

        #region GuiService.cs
        public class GuiService
        {
            public static readonly string RAIDBLOCKENABLEDGUI = @"[
            {
                ""name"": ""RaidLockBackground"",
                ""parent"": ""Overlay"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Image"",
                    ""sprite"": """",
                    ""material"": """",
                    ""color"": ""1 0 0 0.5756225""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.0140625 0.4407408"",
                    ""anchormax"": ""0.1479166 0.5083333"",
                    ""offsetmax"": ""0 0""
                }
                ]
            },
            {
                ""name"": ""RaidlockText"",
                ""parent"": ""RaidLockBackground"",
                ""components"": [
                {
                    ""type"": ""UnityEngine.UI.Text"",
                    ""text"": ""Raiding is disabled"",
                    ""align"": ""MiddleCenter""
                },
                {
                    ""type"": ""RectTransform"",
                    ""anchormin"": ""0.003890991 -5.960464E-08"",
                    ""anchormax"": ""1.003891 1.041097"",
                    ""offsetmax"": ""0 0""
                }
                ]
            }
            ]";
            static List<BasePlayer> _players = new List<BasePlayer>();
            
            public void ActivateGui()
            {
                Interface.Oxide.LogDebug($"Active player with ui enabled {_players.Count}");
                
                _players = _players.Where(player => IsOnline(player)).ToList();
                Interface.Oxide.LogDebug($"Active player with ui enabled {_players.Count}");
                
                var activePlayers = BasePlayer.activePlayerList;
                
                foreach (var player in activePlayers)
                {
                    if (_players.Any(x => x.userID == player.userID))
                    {
                        continue;
                    }
                    
                    CuiHelper.AddUi(player, RAIDBLOCKENABLEDGUI);
                    _players.Add(player);
                };
            }
            
            private static bool IsOnline(BasePlayer player)
            {
                return BasePlayer.activePlayerList.Any(x => x.userID == player.userID);
            }
            
            public void DestroyGui()
            {
                foreach (var player in _players)
                {
                    if (IsOnline(player))
                    {
                        CuiHelper.DestroyUi(player, "RaidLockBackground");
                    }
                };
                _players = new List<BasePlayer>();
            }
        }
        #endregion

        #region MessageRegister.cs
        public class MessageRegister
        {
            private readonly RustPlugin _plugin;
            private readonly Lang _lang;
            
            public MessageRegister(RustPlugin plugin)
            {
                _plugin = plugin;
                _lang = Interface.Oxide.GetLibrary<Lang>();
            }
            public void Register()
            {
                RegisterEnglishMessages();
            }
            
            private void RegisterEnglishMessages()
            {
                _lang.RegisterMessages(new Dictionary<string, string>
                {
                    { "localtime", "Local time is {localtime}." },
                    { "nodamage", "You cannot cause damage to buildings from {starttime} until {endtime}" },
                    { "activated", "You cannot cause damage while Raid block is activated." },
                    { "starts", "Raid block starts at {starts}." },
                    { "onstatus", "Raid block is ON. It is active  from @ {starttime} until {endtime}" },
                    { "offstatus", "Raid block is {status}. It will NOT become active." },
                    { "activate", "- Raid Block is enabled"},
                    { "deactivate", "- Raid Block is disabled"},
                }, _plugin, "en");
            }
        }
        #endregion

        #region RaidBlockService.cs
        public class RaidBlockService
        {
            private readonly DynamicConfigFile _config;
            
            private bool _isForceActive;
            private bool _isActive;
            public RaidBlockService(DynamicConfigFile config)
            {
                _config = config;
            }
            
            public bool IsOn()
            {
                if (_isForceActive) return true;
                
                if ((bool)_config["RaidBlockOn"] == false) return false;
                
                return _isActive;
                //return IsOnUsingConfigTime();
            }
            public bool IsForceActivated()
            {
                return _isForceActive;
            }
            public void Enable()
            {
                _isForceActive = true;
            }
            public void Disable()
            {
                _isForceActive = false;
            }
            
            public void UpdateIsOnUsingConfigTime()
            {
                var start = GetStartTime();
                var end = GetEndTime();
                var now = GetLatvianTime().TimeOfDay;
                
                if (start <= end)
                {
                    _isActive = now >= start && now <= end;
                }
                _isActive = now >= start || now <= end;
            }
            
            public TimeSpan GetStartTime()
            {
                return GetTimeFromConfig("RaidBlockStart");
                
            }
            public TimeSpan GetEndTime()
            {
                return GetTimeFromConfig("RaidBlockEnd");
            }
            
            private TimeSpan GetTimeFromConfig(string key)
            {
                try
                {
                    var time = DateTime.Parse(_config[key].ToString()).TimeOfDay;
                    Interface.Oxide.LogDebug($"Raidblock: Read time from config {time}");
                    return time;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error GetTimeFromConfig {key}. {key}", ex);
                }
            }
            
            private static DateTime GetLatvianTime()
            {
                var currentTime = DateTime.Now.AddHours(1);
                Interface.Oxide.LogDebug($"Got latvian time: {currentTime.TimeOfDay}");
                return currentTime;
            }
        }
        #endregion

    }

}
