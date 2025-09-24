using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("DeathCounter", "mleem97", "1.3.5")]
    [Description("Displays player death counts in the InfoPanel GUI")]
    public class DeathCounter : RustPlugin
    {
        #region Fields

        [PluginReference]
        Plugin InfoPanel;

        private Timer updateTimer;
        private Dictionary<ulong, int> playerDeaths = new Dictionary<ulong, int>();
        private List<string> panels = new List<string> { "DeathCounterPanel" };

        // Permission constants
        private const string PERMISSION_ADMIN = "deathcounter.admin";
        private const string PERMISSION_USE = "deathcounter.use";
        private const string PERMISSION_VIEW_ALL = "deathcounter.viewall";

        // Track if panel is registered
        private bool isPanelRegistered = false;

        #endregion

        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Update Interval in seconds (Default: 10)")]
            public int UpdateInterval = 10;

            [JsonProperty("Panel Position (TopPanel/BottomPanel/LeftPanel/RightPanel)")]
            public string PanelPosition = "TopPanel";

            [JsonProperty("Show Own Deaths Only (true) or All Players (false)")]
            public bool ShowOwnDeathsOnly = true;

            [JsonProperty("Panel Background Color (RGBA: Red Green Blue Alpha)")]
            public string BackgroundColor = "0.1 0.1 0.1 0.4";

            [JsonProperty("Default Text Color (RGBA: Red Green Blue Alpha)")]
            public string TextColor = "1 0.2 0.2 1";

            [JsonProperty("Font Size (Default: 12)")]
            public int FontSize = 12;

            [JsonProperty("Panel Width (0.0 - 1.0, Default: 0.12)")]
            public float PanelWidth = 0.12f;

            [JsonProperty("Panel Height (0.0 - 1.0, Default: 0.95)")]
            public float PanelHeight = 0.95f;

            [JsonProperty("Auto-show Panel on Player Connect")]
            public bool AutoShowOnConnect = true;

            [JsonProperty("Death Icon URL (leave empty for default skull)")]
            public string DeathIconUrl = "https://i.imgur.com/YCmvxMN.png";

            [JsonProperty("Enable Debug Mode")]
            public bool DebugMode = false;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) 
                {
                    LogWarning("Configuration file is null, creating new one...");
                    throw new Exception();
                }
                ValidateConfig();
            }
            catch (Exception ex)
            {
                LogError($"Error loading configuration: {ex.Message}");
                Config.WriteObject(config = new Configuration(), true);
            }
        }

        private void ValidateConfig()
        {
            bool configChanged = false;

            // Validate update interval
            if (config.UpdateInterval < 1 || config.UpdateInterval > 300)
            {
                LogWarning("Update interval out of range (1-300), resetting to 10");
                config.UpdateInterval = 10;
                configChanged = true;
            }

            // Validate panel dimensions
            if (config.PanelWidth < 0.01f || config.PanelWidth > 1.0f)
            {
                LogWarning("Panel width out of range (0.01-1.0), resetting to 0.12");
                config.PanelWidth = 0.12f;
                configChanged = true;
            }

            if (config.PanelHeight < 0.01f || config.PanelHeight > 1.0f)
            {
                LogWarning("Panel height out of range (0.01-1.0), resetting to 0.95");
                config.PanelHeight = 0.95f;
                configChanged = true;
            }

            // Validate font size
            if (config.FontSize < 8 || config.FontSize > 24)
            {
                LogWarning("Font size out of range (8-24), resetting to 12");
                config.FontSize = 12;
                configChanged = true;
            }

            if (configChanged)
            {
                SaveConfig();
            }
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            // English (default) translations
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["DeathCount"] = "Deaths: {0}",
                ["YourDeaths"] = "You have died {0} times.",
                ["NoDeaths"] = "No deaths recorded yet.",
                ["TopDeaths"] = "Top 5 Deaths:",
                ["TopDeathsEntry"] = "{0}. {1}: {2} deaths",
                ["UnknownPlayer"] = "Unknown",
                ["PanelShown"] = "Death Counter panel is now shown.",
                ["PanelHidden"] = "Death Counter panel has been hidden.",
                ["DeathsReset"] = "Your deaths have been reset.",
                ["DeathsResetTarget"] = "Deaths for {0} have been reset.",
                ["PlayerNotFound"] = "Player not found.",
                ["NoPermission"] = "You don't have permission to use this command.",
                ["AllDeathsReset"] = "All deaths have been reset.",
                ["ConfigReloaded"] = "DeathCounter configuration has been reloaded.",
                ["AvailableCommands"] = "Available commands:",
                ["CmdDeaths"] = "/deaths - Shows your own death count",
                ["CmdDeathsTop"] = "/deathstop - Shows top 5 players with most deaths",
                ["CmdPanelShow"] = "/deathpanel show - Shows the Death Counter panel",
                ["CmdPanelHide"] = "/deathpanel hide - Hides the Death Counter panel",
                ["CmdPanelReset"] = "/deathpanel reset [playername] - Resets death count (Admin)",
                ["UnknownCommand"] = "Unknown command. Use /deathpanel for help.",
                ["InfoPanelNotLoaded"] = "InfoPanel plugin is not loaded or available."
            }, this, "en");

            // German translations
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["DeathCount"] = "Todesfälle: {0}",
                ["YourDeaths"] = "Du bist {0} mal gestorben.",
                ["NoDeaths"] = "Noch keine Todesfälle verzeichnet.",
                ["TopDeaths"] = "Top 5 Todesfälle:",
                ["TopDeathsEntry"] = "{0}. {1}: {2} Todesfälle",
                ["UnknownPlayer"] = "Unbekannt",
                ["PanelShown"] = "Death Counter Panel wird angezeigt.",
                ["PanelHidden"] = "Death Counter Panel wurde versteckt.",
                ["DeathsReset"] = "Todesfälle wurden zurückgesetzt.",
                ["DeathsResetTarget"] = "Todesfälle für {0} wurden zurückgesetzt.",
                ["PlayerNotFound"] = "Spieler nicht gefunden.",
                ["NoPermission"] = "Du hast keine Berechtigung für diesen Befehl.",
                ["AllDeathsReset"] = "Alle Todesfälle wurden zurückgesetzt.",
                ["ConfigReloaded"] = "DeathCounter Konfiguration wurde neu geladen.",
                ["AvailableCommands"] = "Verfügbare Befehle:",
                ["CmdDeaths"] = "/deaths - Zeigt deine eigenen Todesfälle an",
                ["CmdDeathsTop"] = "/deathstop - Zeigt die Top 5 Spieler mit den meisten Todesfällen",
                ["CmdPanelShow"] = "/deathpanel show - Zeigt das Death Counter Panel an",
                ["CmdPanelHide"] = "/deathpanel hide - Versteckt das Death Counter Panel",
                ["CmdPanelReset"] = "/deathpanel reset [spielername] - Setzt Todesfälle zurück (Admin)",
                ["UnknownCommand"] = "Unbekannter Befehl. Verwende /deathpanel für Hilfe.",
                ["InfoPanelNotLoaded"] = "InfoPanel Plugin ist nicht geladen oder verfügbar."
            }, this, "de");
        }

        private string Lang(string key, string id = null, params object[] args) => 
            string.Format(lang.GetMessage(key, this, id), args);

        #endregion

        #region Hooks

        void Init()
        {
            // Initialize config first
            if (config == null)
            {
                LoadConfig();
            }

            // Register permissions
            permission.RegisterPermission(PERMISSION_ADMIN, this);
            permission.RegisterPermission(PERMISSION_USE, this);
            permission.RegisterPermission(PERMISSION_VIEW_ALL, this);

            // Ensure all required files exist and are up to date
            EnsureRequiredFiles();
            
            LoadData();
        }

        private void EnsureRequiredFiles()
        {
            try
            {
                // Check and create/update configuration file
                EnsureConfigurationFile();
                
                // Check and create/update language files
                EnsureLanguageFiles();
                
                // Check and create data file if it doesn't exist
                EnsureDataFile();
                
                LogInfo("All required files checked and ensured");
            }
            catch (Exception ex)
            {
                LogError($"Error ensuring required files: {ex.Message}");
            }
        }

        private void EnsureConfigurationFile()
        {
            var configPath = $"{Interface.Oxide.ConfigDirectory}{System.IO.Path.DirectorySeparatorChar}DeathCounter.json";
            bool needsUpdate = false;
            
            if (!System.IO.File.Exists(configPath))
            {
                LogInfo("Configuration file not found, creating new one...");
                Config.WriteObject(new Configuration(), true);
                needsUpdate = true;
            }
            else
            {
                // Check if config file is outdated (version check)
                try
                {
                    var existingConfig = Config.ReadObject<Configuration>();
                    var newConfig = new Configuration();
                    
                    // Check if new properties are missing (simple version check)
                    var existingJson = JsonConvert.SerializeObject(existingConfig);
                    var newJson = JsonConvert.SerializeObject(newConfig);
                    
                    if (existingJson.Length < newJson.Length * 0.8) // If existing config is significantly smaller
                    {
                        LogWarning("Configuration file appears outdated, backing up and creating new one...");
                        System.IO.File.Copy(configPath, $"{configPath}.backup.{DateTime.Now:yyyyMMdd_HHmmss}");
                        Config.WriteObject(newConfig, true);
                        needsUpdate = true;
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"Could not validate config file: {ex.Message}. Creating new one...");
                    System.IO.File.Copy(configPath, $"{configPath}.backup.{DateTime.Now:yyyyMMdd_HHmmss}");
                    Config.WriteObject(new Configuration(), true);
                    needsUpdate = true;
                }
            }
            
            if (needsUpdate)
            {
                LogInfo("Configuration file created/updated successfully");
            }
        }

        private void EnsureLanguageFiles()
        {
            try
            {
                var langDir = $"{Interface.Oxide.DataDirectory}{System.IO.Path.DirectorySeparatorChar}lang";
                
                // Ensure language directories exist
                var enDir = $"{langDir}{System.IO.Path.DirectorySeparatorChar}en";
                var deDir = $"{langDir}{System.IO.Path.DirectorySeparatorChar}de";
                
                if (!System.IO.Directory.Exists(enDir))
                    System.IO.Directory.CreateDirectory(enDir);
                if (!System.IO.Directory.Exists(deDir))
                    System.IO.Directory.CreateDirectory(deDir);
                
                // Force recreation of language files to ensure they're up to date
                LoadDefaultMessages();
                
                LogInfo("Language files ensured and updated");
            }
            catch (Exception ex)
            {
                LogError($"Error ensuring language files: {ex.Message}");
            }
        }

        private void EnsureDataFile()
        {
            try
            {
                var dataPath = $"{Interface.Oxide.DataDirectory}{System.IO.Path.DirectorySeparatorChar}DeathCounter_Deaths.json";
                
                if (!System.IO.File.Exists(dataPath))
                {
                    LogInfo("Data file not found, creating new one...");
                    Interface.Oxide.DataFileSystem.WriteObject("DeathCounter_Deaths", new Dictionary<ulong, int>());
                }
                else
                {
                    // Validate data file integrity
                    try
                    {
                        var testData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>("DeathCounter_Deaths");
                        if (testData == null)
                        {
                            LogWarning("Data file corrupted, creating backup and new file...");
                            System.IO.File.Copy(dataPath, $"{dataPath}.backup.{DateTime.Now:yyyyMMdd_HHmmss}");
                            Interface.Oxide.DataFileSystem.WriteObject("DeathCounter_Deaths", new Dictionary<ulong, int>());
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Data file validation failed: {ex.Message}. Creating backup and new file...");
                        System.IO.File.Copy(dataPath, $"{dataPath}.backup.{DateTime.Now:yyyyMMdd_HHmmss}");
                        Interface.Oxide.DataFileSystem.WriteObject("DeathCounter_Deaths", new Dictionary<ulong, int>());
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error ensuring data file: {ex.Message}");
            }
        }

        void Loaded()
        {
            // Don't initialize InfoPanel immediately - wait for OnServerInitialized
            LogInfo("DeathCounter plugin loaded, waiting for server initialization...");
        }

        void OnPluginLoaded(Plugin plugin)
        {
            if (plugin?.Title == "InfoPanel" || plugin?.Name == "InfoPanel")
            {
                LogInfo("InfoPanel detected, initializing DeathCounter integration...");
                InfoPanel = plugin;
                
                // Only initialize if server is already initialized
                if (IsServerInitialized())
                {
                    // Small delay to ensure InfoPanel is fully initialized
                    timer.Once(1f, () => {
                        InfoPanelInit();
                        LogInfo("InfoPanel integration reestablished");
                    });
                }
                else
                {
                    LogInfo("Server not yet initialized, will connect InfoPanel later");
                }
            }
        }
        
        private bool IsServerInitialized()
        {
            try
            {
                // Check if server is running and initialized
                return ServerMgr.Instance != null && BaseNetworkable.serverEntities != null;
            }
            catch
            {
                return false;
            }
        }

        void Unload()
        {
            try
            {
                LogInfo("Unloading DeathCounter plugin...");
                
                // Save data before unloading
                try
                {
                    SaveData();
                }
                catch (Exception ex)
                {
                    LogError($"Error saving data during unload: {ex.Message}");
                }
                
                // Destroy timer
                try
                {
                    if (updateTimer != null)
                    {
                        updateTimer.Destroy();
                        updateTimer = null;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error destroying timer during unload: {ex.Message}");
                }
                
                // Enhanced InfoPanel cleanup with multiple safety strategies
                try
                {
                    LogInfo($"InfoPanel cleanup - panel registered: {isPanelRegistered}, InfoPanel available: {InfoPanel != null}");
                    
                    if (InfoPanel != null && isPanelRegistered)
                    {
                        try
                        {
                            // Strategy 1: Try to explicitly unregister our panel first
                            LogInfo("Attempting explicit panel unregistration...");
                            InfoPanel.Call("PanelUnregister", "DeathCounter", "DeathCounterPanel");
                            LogInfo("Panel unregistration completed");
                        }
                        catch (Exception unregisterEx)
                        {
                            LogInfo($"Panel unregistration failed (expected): {unregisterEx.Message}");
                        }
                        
                        try
                        {
                            // Strategy 2: Send empty panel list to signal cleanup
                            LogInfo("Sending empty panel list for cleanup...");
                            InfoPanel.Call("SendPanelInfo", "DeathCounter", new List<string>());
                            LogInfo("Empty panel list sent successfully");
                        }
                        catch (Exception cleanupEx)
                        {
                            LogInfo($"Empty panel list signal failed: {cleanupEx.Message}");
                        }
                        
                        // Strategy 3: Add small delay to let InfoPanel process
                        timer.Once(0.1f, () => {
                            LogInfo("InfoPanel cleanup delay completed");
                        });
                    }
                    else
                    {
                        LogInfo("Skipping InfoPanel cleanup - not registered or InfoPanel unavailable");
                    }
                    
                    // Reset our flag regardless of cleanup success
                    isPanelRegistered = false;
                    
                    LogInfo("InfoPanel cleanup sequence completed");
                }
                catch (Exception ex)
                {
                    LogError($"Error during InfoPanel cleanup: {ex.Message}");
                    isPanelRegistered = false; // Reset flag even if cleanup failed
                }
                
                LogInfo("DeathCounter plugin unloaded successfully");
            }
            catch (Exception ex)
            {
                LogError($"Error during plugin unload: {ex.Message}");
            }
        }

        void OnServerSave()
        {
            SaveData();
        }

        void OnPluginUnloaded(Plugin plugin)
        {
            if (plugin.Name == "InfoPanel")
            {
                LogWarning("InfoPanel plugin unloaded - GUI features disabled");
                InfoPanel = null;
                isPanelRegistered = false; // Reset panel registration when InfoPanel unloads
            }
        }

        void OnServerInitialized()
        {
            LogInfo("Server initialized, checking InfoPanel dependency...");
            
            // Check if InfoPanel is available
            CheckInfoPanelDependency();
            
            // Register InfoPanel if available
            if (InfoPanel != null)
            {
                InfoPanelInit();
                LogInfo("InfoPanel integration ready");
            }
            else
            {
                LogWarning("InfoPanel plugin not found - GUI features will be unavailable");
                LogWarning("Please install InfoPanel plugin by Ghosst for full functionality");
            }
            
            // Start update timer regardless of InfoPanel availability
            if (config?.UpdateInterval > 0)
            {
                updateTimer = timer.Every(config.UpdateInterval, UpdateAllPanels);
                LogInfo($"Started update timer with {config.UpdateInterval}s interval");
            }
        }

        private void CheckInfoPanelDependency()
        {
            try
            {
                // Try to get InfoPanel plugin reference
                var infoPanelPlugin = plugins.Find("InfoPanel");
                
                if (infoPanelPlugin == null)
                {
                    LogWarning("InfoPanel plugin not found in plugin list");
                    return;
                }
                
                if (!infoPanelPlugin.IsLoaded)
                {
                    LogWarning("InfoPanel plugin found but not loaded");
                    return;
                }
                
                // InfoPanel is available
                InfoPanel = infoPanelPlugin;
                LogInfo("InfoPanel plugin found and loaded successfully");
                
                // Test if we can call InfoPanel methods
                var result = InfoPanel.Call("API_VERSION");
                if (result != null)
                {
                    LogInfo($"InfoPanel API version: {result}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error checking InfoPanel dependency: {ex.Message}");
            }
        }

        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null) return;

            try
            {
                var playerId = player.userID;
                
                if (!playerDeaths.ContainsKey(playerId))
                    playerDeaths[playerId] = 0;
                
                playerDeaths[playerId]++;
                
                if (config?.DebugMode == true)
                    LogInfo($"Player {player.displayName} died. Total deaths: {playerDeaths[playerId]}");
                
                UpdatePlayerPanel(player);
                
                // Auto-save after every death to prevent data loss
                timer.Once(1f, () => SaveData());
            }
            catch (Exception ex)
            {
                LogError($"Error in OnPlayerDeath for {player?.displayName}: {ex.Message}");
            }
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player == null || !HasPermission(player, PERMISSION_USE)) return;
            
            timer.Once(3f, () =>
            {
                try
                {
                    if (player == null || !player.IsConnected) return;
                    
                    if (InfoPanel != null && config?.AutoShowOnConnect == true)
                    {
                        // Check if InfoPanel GUI is loaded for the player
                        bool isGUILoaded = (bool?)InfoPanel.Call("IsPlayerGUILoaded", player.UserIDString) ?? false;
                        
                        if (isGUILoaded)
                        {
                            ShowPanelToPlayer(player);
                        }
                        else
                        {
                            // Retry after a delay if GUI is not ready
                            timer.Once(2f, () => {
                                if (player != null && player.IsConnected)
                                    ShowPanelToPlayer(player);
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error in OnPlayerConnected for {player?.displayName}: {ex.Message}");
                }
            });
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null) return;
            
            try
            {
                // Let InfoPanel handle its own cleanup during disconnection
                // Removed HidePanel call to avoid KeyNotFoundException errors
                if (config?.DebugMode == true)
                {
                    LogInfo($"Player {player.displayName} disconnected");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error in OnPlayerDisconnected for {player?.displayName}: {ex.Message}");
            }
        }

        #endregion

        #region InfoPanel Integration

        public void InfoPanelInit()
        {
            try
            {
                // Check if already initialized to prevent duplicate registration
                if (isPanelRegistered)
                {
                    LogInfo("InfoPanel already initialized, skipping re-initialization");
                    return;
                }
                
                LogInfo("Initializing InfoPanel integration...");
                
                InfoPanel.Call("SendPanelInfo", "DeathCounter", panels);
                AddDeathCounterPanel();
                
                // Only restart timer if panel registration was successful
                if (isPanelRegistered)
                {
                    updateTimer?.Destroy();
                    if (config?.UpdateInterval > 0)
                    {
                        updateTimer = timer.Repeat(config.UpdateInterval, 0, () => UpdateAllPanels());
                        LogInfo($"Update timer started with {config.UpdateInterval}s interval");
                    }
                    
                    LogInfo("DeathCounter successfully integrated with InfoPanel");
                }
                else
                {
                    LogWarning("InfoPanel integration failed - panel registration unsuccessful");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error initializing InfoPanel integration: {ex.Message}");
                isPanelRegistered = false;
            }
        }

        public void AddDeathCounterPanel()
        {
            try
            {
                // First check if panel is already registered to avoid duplicates
                if (isPanelRegistered)
                {
                    LogInfo("DeathCounter panel already registered, skipping registration");
                    return;
                }
                
                LogInfo("Attempting to register DeathCounter panel with InfoPanel...");
                var panelConfig = GetPanelConfig();
                LogInfo($"Panel config generated: {panelConfig.Length} characters");
                
                // Enhanced panel registration with verification
                var result = InfoPanel.Call("PanelRegister", "DeathCounter", "DeathCounterPanel", panelConfig);
                LogInfo($"PanelRegister result: {result} (type: {result?.GetType()})");
                
                bool success = result != null && (bool)result;
                
                if (success)
                {
                    // Double-check that InfoPanel actually has our panel registered
                    try
                    {
                        var verifyResult = InfoPanel.Call("IsPanelRegistered", "DeathCounter", "DeathCounterPanel");
                        bool isVerified = verifyResult != null && (bool)verifyResult;
                        
                        if (isVerified)
                        {
                            isPanelRegistered = true;
                            LogInfo("DeathCounter panel registered and verified successfully");
                        }
                        else
                        {
                            LogWarning("Panel registration reported success but verification failed");
                            isPanelRegistered = true; // Assume success since PanelRegister returned true
                        }
                    }
                    catch (Exception verifyEx)
                    {
                        LogInfo($"Panel verification not available: {verifyEx.Message}");
                        isPanelRegistered = true; // Assume success since PanelRegister returned true
                    }
                    
                    // Small delay before showing panels to ensure registration is complete
                    timer.Once(0.5f, () => {
                        // Show panel to connected players with permission
                        foreach (var player in BasePlayer.activePlayerList)
                        {
                            if (HasPermission(player, PERMISSION_USE))
                            {
                                ShowPanelToPlayer(player);
                            }
                        }
                    });
                }
                else
                {
                    LogError($"Failed to register DeathCounter panel with InfoPanel (result: {result})");
                    isPanelRegistered = false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error adding DeathCounter panel: {ex.Message}");
                isPanelRegistered = false;
            }
        }

        private string GetPanelConfig()
        {
            var panelConfig = new
            {
                Autoload = false,
                AnchorX = "Left",
                AnchorY = "Top",
                Available = true,
                BackgroundColor = config.BackgroundColor,
                Dock = config.PanelPosition,
                Width = config.PanelWidth,
                Height = config.PanelHeight,
                Margin = "0.005 0 0 0.005",
                Order = 10,
                Image = new
                {
                    AnchorX = "Left",
                    AnchorY = "Top",
                    Available = true,
                    BackgroundColor = "0.2 0.1 0.1 0.6",
                    Dock = config.PanelPosition,
                    Height = 0.3,
                    Margin = "0.05 0.05 0.35 0.05",
                    Order = 1,
                    Url = !string.IsNullOrEmpty(config.DeathIconUrl) ? config.DeathIconUrl : "https://i.imgur.com/YCmvxMN.png"
                },
                Text = new
                {
                    Align = "MiddleCenter",
                    AnchorX = "Left",
                    AnchorY = "Top",
                    Available = true,
                    BackgroundColor = "0 0 0 0",
                    Dock = config.PanelPosition,
                    FontColor = config.TextColor,
                    FontSize = config.FontSize,
                    Content = Lang("DeathCount", null, 0),
                    Height = 0.65,
                    Margin = "0.35 0.05 0.05 0.05",
                    Order = 2,
                    Width = 0.9
                }
            };

            return JsonConvert.SerializeObject(panelConfig, Formatting.Indented);
        }

        #endregion

        #region Panel Updates

        private void UpdatePlayerPanel(BasePlayer player)
        {
            if (InfoPanel == null || player == null || !isPanelRegistered) return;

            try
            {
                var playerId = player.userID;
                var deaths = GetPlayerDeaths(playerId);
                
                string content = Lang("DeathCount", player.UserIDString, deaths);
                string color = GetDeathColor(deaths);

                // Use correct InfoPanel naming convention: PanelName + "Text" for text elements
                InfoPanel.Call("SetPanelAttribute", "DeathCounter", "DeathCounterPanelText", "Content", content, player.UserIDString);
                InfoPanel.Call("SetPanelAttribute", "DeathCounter", "DeathCounterPanelText", "FontColor", color, player.UserIDString);
                InfoPanel.Call("RefreshPanel", "DeathCounter", "DeathCounterPanel", player.UserIDString);

                if (config?.DebugMode == true)
                    LogInfo($"Updated panel for {player.displayName}: {deaths} deaths");
            }
            catch (Exception ex)
            {
                LogError($"Error updating panel for {player?.displayName}: {ex.Message}");
            }
        }

        private void UpdateAllPanels()
        {
            if (InfoPanel == null || !isPanelRegistered) return;

            try
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (HasPermission(player, PERMISSION_USE))
                    {
                        UpdatePlayerPanel(player);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error updating all panels: {ex.Message}");
            }
        }

        private void ShowPanelToPlayer(BasePlayer player)
        {
            if (InfoPanel == null || player == null || !HasPermission(player, PERMISSION_USE) || !isPanelRegistered) return;
            
            try
            {
                UpdatePlayerPanel(player);
                InfoPanel.Call("ShowPanel", "DeathCounter", "DeathCounterPanel", player.UserIDString);

                if (config?.DebugMode == true)
                    LogInfo($"Showing panel to {player.displayName}");
            }
            catch (Exception ex)
            {
                LogError($"Error showing panel to {player?.displayName}: {ex.Message}");
            }
        }

        private string GetDeathColor(int deaths)
        {
            // Color gradient based on death count
            if (deaths == 0) return "0.2 0.8 0.2 1"; // Green
            if (deaths <= 3) return "0.8 0.8 0.2 1"; // Yellow
            if (deaths <= 7) return "0.8 0.6 0.2 1"; // Orange
            if (deaths <= 15) return "0.8 0.3 0.2 1"; // Red-Orange
            if (deaths <= 25) return "0.8 0.2 0.2 1"; // Red
            return "0.6 0.1 0.1 1"; // Dark Red
        }

        #endregion

        #region Helper Methods

        private bool HasPermission(BasePlayer player, string permission)
        {
            return this.permission.UserHasPermission(player.UserIDString, permission);
        }

        private bool IsAdmin(BasePlayer player)
        {
            return player.IsAdmin || HasPermission(player, PERMISSION_ADMIN);
        }

        private void LogInfo(string message)
        {
            try
            {
                if (config?.DebugMode == true)
                    Puts($"[INFO] {message}");
            }
            catch
            {
                // Fallback if config is null
                Puts($"[INFO] {message}");
            }
        }

        private void LogWarning(string message)
        {
            try
            {
                PrintWarning($"[WARNING] {message}");
            }
            catch
            {
                // Fallback
                Puts($"[WARNING] {message}");
            }
        }

        private void LogError(string message)
        {
            try
            {
                PrintError($"[ERROR] {message}");
            }
            catch
            {
                // Fallback
                Puts($"[ERROR] {message}");
            }
        }

        #endregion

        #region Data Management

        private int GetPlayerDeaths(ulong playerId)
        {
            return playerDeaths.ContainsKey(playerId) ? playerDeaths[playerId] : 0;
        }

        void LoadData()
        {
            try
            {
                playerDeaths = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>("DeathCounter_Deaths");
                if (playerDeaths == null)
                {
                    LogWarning("Death data was null, initializing empty dictionary");
                    playerDeaths = new Dictionary<ulong, int>();
                    SaveData(); // Save empty data immediately
                }
                else
                {
                    LogInfo($"Loaded death data for {playerDeaths.Count} players");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error loading death data: {ex.Message}");
                LogWarning("Initializing with empty death data");
                playerDeaths = new Dictionary<ulong, int>();
                SaveData(); // Save empty data immediately
            }
        }

        void SaveData()
        {
            try
            {
                Interface.Oxide.DataFileSystem.WriteObject("DeathCounter_Deaths", playerDeaths);
                if (config?.DebugMode == true)
                    LogInfo($"Saved death data for {playerDeaths.Count} players");
            }
            catch (Exception ex)
            {
                LogError($"Error saving death data: {ex.Message}");
            }
        }

        #endregion

        #region Chat Commands

        [ChatCommand("deaths")]
        void DeathsCommand(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;

            if (!HasPermission(player, PERMISSION_USE))
            {
                SendReply(player, Lang("NoPermission", player.UserIDString));
                return;
            }

            var deaths = GetPlayerDeaths(player.userID);
            SendReply(player, Lang("YourDeaths", player.UserIDString, deaths));
        }

        [ChatCommand("deathstop")]
        void DeathsTopCommand(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;

            if (!HasPermission(player, PERMISSION_USE))
            {
                SendReply(player, Lang("NoPermission", player.UserIDString));
                return;
            }

            var topDeaths = playerDeaths
                .OrderByDescending(x => x.Value)
                .Take(5)
                .ToList();

            if (topDeaths.Count == 0)
            {
                SendReply(player, Lang("NoDeaths", player.UserIDString));
                return;
            }

            SendReply(player, Lang("TopDeaths", player.UserIDString));
            for (int i = 0; i < topDeaths.Count; i++)
            {
                var targetPlayer = BasePlayer.FindByID(topDeaths[i].Key);
                var playerName = targetPlayer?.displayName ?? Lang("UnknownPlayer", player.UserIDString);
                SendReply(player, Lang("TopDeathsEntry", player.UserIDString, i + 1, playerName, topDeaths[i].Value));
            }
        }

        [ChatCommand("deathpanel")]
        void DeathPanelCommand(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;

            if (!HasPermission(player, PERMISSION_USE))
            {
                SendReply(player, Lang("NoPermission", player.UserIDString));
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, Lang("AvailableCommands", player.UserIDString));
                SendReply(player, Lang("CmdDeaths", player.UserIDString));
                SendReply(player, Lang("CmdDeathsTop", player.UserIDString));
                SendReply(player, Lang("CmdPanelShow", player.UserIDString));
                SendReply(player, Lang("CmdPanelHide", player.UserIDString));
                if (IsAdmin(player))
                    SendReply(player, Lang("CmdPanelReset", player.UserIDString));
                return;
            }

            switch (args[0].ToLower())
            {
                case "show":
                    if (InfoPanel != null)
                    {
                        ShowPanelToPlayer(player);
                        SendReply(player, Lang("PanelShown", player.UserIDString));
                    }
                    else
                    {
                        SendReply(player, Lang("InfoPanelNotLoaded", player.UserIDString));
                    }
                    break;

                case "hide":
                    if (InfoPanel != null && isPanelRegistered)
                    {
                        try
                        {
                            InfoPanel.Call("HidePanel", "DeathCounter", "DeathCounterPanel", player.UserIDString);
                            SendReply(player, Lang("PanelHidden", player.UserIDString));
                        }
                        catch (Exception ex)
                        {
                            LogError($"Error hiding panel for {player.displayName}: {ex.Message}");
                            SendReply(player, "Error hiding panel.");
                        }
                    }
                    else
                    {
                        SendReply(player, Lang("InfoPanelNotLoaded", player.UserIDString));
                    }
                    break;

                case "reset":
                    if (!IsAdmin(player))
                    {
                        SendReply(player, Lang("NoPermission", player.UserIDString));
                        return;
                    }

                    if (args.Length > 1)
                    {
                        var targetPlayer = BasePlayer.Find(args[1]);
                        if (targetPlayer != null)
                        {
                            playerDeaths[targetPlayer.userID] = 0;
                            UpdatePlayerPanel(targetPlayer);
                            SendReply(player, Lang("DeathsResetTarget", player.UserIDString, targetPlayer.displayName));
                            SaveData();
                        }
                        else
                        {
                            SendReply(player, Lang("PlayerNotFound", player.UserIDString));
                        }
                    }
                    else
                    {
                        playerDeaths[player.userID] = 0;
                        UpdatePlayerPanel(player);
                        SendReply(player, Lang("DeathsReset", player.UserIDString));
                        SaveData();
                    }
                    break;

                default:
                    SendReply(player, Lang("UnknownCommand", player.UserIDString));
                    break;
            }
        }

        #endregion

        #region Console Commands

        [ConsoleCommand("deathcounter.reset")]
        void ResetDeathsConsole(ConsoleSystem.Arg arg)
        {
            if (!arg.IsRcon && (arg.Connection == null || arg.Connection.authLevel < 2))
            {
                arg.ReplyWith("Access denied. Admin permission required.");
                return;
            }

            try
            {
                if (arg.Args.Length > 0)
                {
                    var targetPlayer = BasePlayer.Find(arg.Args[0]);
                    if (targetPlayer != null)
                    {
                        playerDeaths[targetPlayer.userID] = 0;
                        UpdatePlayerPanel(targetPlayer);
                        arg.ReplyWith($"Death count reset for {targetPlayer.displayName}");
                        SaveData();
                    }
                    else
                    {
                        arg.ReplyWith("Player not found.");
                    }
                }
                else
                {
                    int playersReset = playerDeaths.Count;
                    playerDeaths.Clear();
                    SaveData();
                    arg.ReplyWith($"All death counts reset. Affected {playersReset} players.");
                    
                    // Update all active players
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        UpdatePlayerPanel(player);
                    }
                }
            }
            catch (Exception ex)
            {
                arg.ReplyWith($"Error resetting death counts: {ex.Message}");
                LogError($"Console command error: {ex.Message}");
            }
        }

        [ConsoleCommand("deathcounter.reload")]
        void ReloadPluginConsole(ConsoleSystem.Arg arg)
        {
            if (!arg.IsRcon && (arg.Connection == null || arg.Connection.authLevel < 2))
            {
                arg.ReplyWith("Access denied. Admin permission required.");
                return;
            }

            try
            {
                LoadConfig();
                
                // Reinitialize timer with new interval
                updateTimer?.Destroy();
                if (config?.UpdateInterval > 0)
                {
                    updateTimer = timer.Repeat(config.UpdateInterval, 0, () => UpdateAllPanels());
                }
                
                arg.ReplyWith("DeathCounter configuration reloaded successfully.");
            }
            catch (Exception ex)
            {
                arg.ReplyWith($"Error reloading configuration: {ex.Message}");
                LogError($"Console reload error: {ex.Message}");
            }
        }

        [ConsoleCommand("deathcounter.status")]
        void StatusConsole(ConsoleSystem.Arg arg)
        {
            if (!arg.IsRcon && (arg.Connection == null || arg.Connection.authLevel < 2))
            {
                arg.ReplyWith("Access denied. Admin permission required.");
                return;
            }

            try
            {
                var totalPlayers = playerDeaths.Count;
                var totalDeaths = playerDeaths.Values.Sum();
                var infoPanelStatus = InfoPanel != null ? "Loaded" : "Not Found";
                var activePlayers = BasePlayer.activePlayerList.Count;

                arg.ReplyWith($"=== DeathCounter Status ===");
                arg.ReplyWith($"Plugin Version: 1.3.1");
                arg.ReplyWith($"InfoPanel Status: {infoPanelStatus}");
                arg.ReplyWith($"Total Players Tracked: {totalPlayers}");
                arg.ReplyWith($"Total Deaths Recorded: {totalDeaths}");
                arg.ReplyWith($"Active Players Online: {activePlayers}");
                arg.ReplyWith($"Update Interval: {config?.UpdateInterval ?? 10}s");
                arg.ReplyWith($"Debug Mode: {(config?.DebugMode == true ? "Enabled" : "Disabled")}");
            }
            catch (Exception ex)
            {
                arg.ReplyWith($"Error getting status: {ex.Message}");
                LogError($"Console status error: {ex.Message}");
            }
        }

        #endregion
    }
}
