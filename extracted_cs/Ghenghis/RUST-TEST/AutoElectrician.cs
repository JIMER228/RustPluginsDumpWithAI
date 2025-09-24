using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Carbon.Core;
using Carbon.Extensions;
using Carbon.Plugins;
using Network;

namespace Oxide.Plugins
{
    [Info("AutoElectrician", "Enhanced", "2.0.0")]
    [Description("Enhanced AI-powered electrical system analysis and optimization with improved error handling")]
    public class AutoElectrician : RustPlugin
    {
        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Admin Permission")]
            public string AdminPermission { get; set; } = "autoelectrician.admin";

            [JsonProperty("Use Permission")]
            public string UsePermission { get; set; } = "autoelectrician.use";

            [JsonProperty("Analysis Settings")]
            public AnalysisSettings Analysis { get; set; } = new AnalysisSettings();

            [JsonProperty("Optimization Settings")]
            public OptimizationSettings Optimization { get; set; } = new OptimizationSettings();

            [JsonProperty("Performance Settings")]
            public PerformanceSettings Performance { get; set; } = new PerformanceSettings();
        }

        public class AnalysisSettings
        {
            [JsonProperty("Max Scan Distance")]
            public float MaxScanDistance { get; set; } = 100f;

            [JsonProperty("Auto Analyze On Build")]
            public bool AutoAnalyzeOnBuild { get; set; } = true;

            [JsonProperty("Show Detailed Reports")]
            public bool ShowDetailedReports { get; set; } = true;

            [JsonProperty("Check Power Efficiency")]
            public bool CheckPowerEfficiency { get; set; } = true;
        }

        public class OptimizationSettings
        {
            [JsonProperty("Auto Fix Issues")]
            public bool AutoFixIssues { get; set; } = false;

            [JsonProperty("Suggest Improvements")]
            public bool SuggestImprovements { get; set; } = true;

            [JsonProperty("Optimize Power Flow")]
            public bool OptimizePowerFlow { get; set; } = true;

            [JsonProperty("Remove Redundant Connections")]
            public bool RemoveRedundantConnections { get; set; } = false;
        }

        public class PerformanceSettings
        {
            [JsonProperty("Max Entities Per Scan")]
            public int MaxEntitiesPerScan { get; set; } = 500;

            [JsonProperty("Scan Interval")]
            public float ScanInterval { get; set; } = 2f;

            [JsonProperty("Enable Caching")]
            public bool EnableCaching { get; set; } = true;

            [JsonProperty("Cache Duration")]
            public float CacheDuration { get; set; } = 30f;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }
            }
            catch
            {
                PrintWarning("Configuration file is corrupt, using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You don't have permission to use Auto Electrician!",
                ["ScanStarted"] = "Electrical system scan started...",
                ["ScanCompleted"] = "Scan completed. Found {0} electrical components",
                ["NoElectricalFound"] = "No electrical components found in scan area",
                ["AnalysisReport"] = "Electrical Analysis Report:",
                ["PowerSources"] = "Power Sources: {0}",
                ["PowerConsumers"] = "Power Consumers: {0}",
                ["TotalPowerGenerated"] = "Total Power Generated: {0}W",
                ["TotalPowerConsumed"] = "Total Power Consumed: {0}W",
                ["PowerEfficiency"] = "Power Efficiency: {0:F1}%",
                ["IssuesFound"] = "Issues Found: {0}",
                ["SuggestionsAvailable"] = "Optimization Suggestions: {0}",
                ["OptimizationApplied"] = "Optimization applied successfully",
                ["NoIssuesFound"] = "No electrical issues detected",
                ["InvalidTarget"] = "Please look at an electrical component",
                ["ScanInProgress"] = "Scan already in progress, please wait...",
                ["ComponentInfo"] = "{0} - Power: {1}W - Status: {2}",
                ["CircuitOverloaded"] = "Warning: Circuit overloaded!",
                ["InsufficientPower"] = "Warning: Insufficient power generation!",
                ["RedundantConnections"] = "Found {0} redundant connections",
                ["OptimizationSuggestion"] = "Suggestion: {0}"
            }, this);
        }

        private string GetMessage(string key, string playerId = null, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this, playerId), args);
        }

        #endregion

        #region Fields

        private readonly Dictionary<ulong, ElectricalScanData> activeScanners = new Dictionary<ulong, ElectricalScanData>();
        private readonly Dictionary<string, CachedAnalysisData> analysisCache = new Dictionary<string, CachedAnalysisData>();
        private readonly HashSet<uint> processedCircuits = new HashSet<uint>();

        private class ElectricalScanData
        {
            public bool IsScanning { get; set; }
            public DateTime LastScan { get; set; }
            public Vector3 ScanPosition { get; set; }
            public List<IOEntity> FoundComponents { get; set; } = new List<IOEntity>();
        }

        private class CachedAnalysisData
        {
            public ElectricalAnalysis Analysis { get; set; }
            public DateTime CacheTime { get; set; }
        }

        private class ElectricalAnalysis
        {
            public List<IOEntity> PowerSources { get; set; } = new List<IOEntity>();
            public List<IOEntity> PowerConsumers { get; set; } = new List<IOEntity>();
            public float TotalPowerGenerated { get; set; }
            public float TotalPowerConsumed { get; set; }
            public List<string> Issues { get; set; } = new List<string>();
            public List<string> Suggestions { get; set; } = new List<string>();
            public float Efficiency { get; set; }
        }

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(config.AdminPermission, this);
            permission.RegisterPermission(config.UsePermission, this);
            
            AddCovalenceCommand("electrician", "ElectricianCommand");
            AddCovalenceCommand("escan", "ElectricianCommand");
            AddCovalenceCommand("eanalyze", "AnalyzeCommand");
            AddCovalenceCommand("eoptimize", "OptimizeCommand");

            Puts("AutoElectrician Enhanced v2.0.0 initialized successfully");
        }

        private void Unload()
        {
            activeScanners.Clear();
            analysisCache.Clear();
            processedCircuits.Clear();
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null) return;
            activeScanners.Remove(player.userID);
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (!config.Analysis.AutoAnalyzeOnBuild) return;

            var entity = go?.GetComponent<IOEntity>();
            if (entity == null) return;

            var player = plan?.GetOwnerPlayer();
            if (player == null || !permission.UserHasPermission(player.UserIDString, config.UsePermission))
                return;

            NextTick(() => AnalyzeNearbyElectricals(player, entity.transform.position));
        }

        #endregion

        #region Commands

        [Command("electrician", "escan")]
        private void ElectricianCommand(IPlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.Id, config.UsePermission))
            {
                player.Reply(GetMessage("NoPermission", player.Id));
                return;
            }

            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;

            if (args.Length > 0)
            {
                HandleElectricianCommand(basePlayer, args);
            }
            else
            {
                StartElectricalScan(basePlayer);
            }
        }

        [Command("eanalyze")]
        private void AnalyzeCommand(IPlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.Id, config.UsePermission))
            {
                player.Reply(GetMessage("NoPermission", player.Id));
                return;
            }

            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;

            AnalyzeElectricalSystem(basePlayer);
        }

        [Command("eoptimize")]
        private void OptimizeCommand(IPlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.Id, config.AdminPermission))
            {
                player.Reply(GetMessage("NoPermission", player.Id));
                return;
            }

            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;

            OptimizeElectricalSystem(basePlayer);
        }

        #endregion

        #region Core Methods

        private void HandleElectricianCommand(BasePlayer player, string[] args)
        {
            var subCommand = args[0].ToLower();

            switch (subCommand)
            {
                case "scan":
                    StartElectricalScan(player);
                    break;
                case "analyze":
                    AnalyzeElectricalSystem(player);
                    break;
                case "optimize":
                    if (permission.UserHasPermission(player.UserIDString, config.AdminPermission))
                        OptimizeElectricalSystem(player);
                    else
                        player.ChatMessage(GetMessage("NoPermission", player.UserIDString));
                    break;
                case "help":
                    ShowElectricianHelp(player);
                    break;
                default:
                    ShowElectricianHelp(player);
                    break;
            }
        }

        private void StartElectricalScan(BasePlayer player)
        {
            if (activeScanners.TryGetValue(player.userID, out var scanData) && scanData.IsScanning)
            {
                player.ChatMessage(GetMessage("ScanInProgress", player.UserIDString));
                return;
            }

            player.ChatMessage(GetMessage("ScanStarted", player.UserIDString));

            var newScanData = new ElectricalScanData
            {
                IsScanning = true,
                LastScan = DateTime.Now,
                ScanPosition = player.transform.position
            };

            activeScanners[player.userID] = newScanData;

            timer.Once(0.1f, () => PerformElectricalScan(player, newScanData));
        }

        private void PerformElectricalScan(BasePlayer player, ElectricalScanData scanData)
        {
            var components = new List<IOEntity>();
            var processed = 0;
            var playerPos = player.transform.position;

            foreach (var entity in BaseNetworkable.serverEntities)
            {
                if (processed >= config.Performance.MaxEntitiesPerScan)
                {
                    break;
                }

                if (entity is IOEntity ioEntity && 
                    Vector3.Distance(playerPos, ioEntity.transform.position) <= config.Analysis.MaxScanDistance)
                {
                    components.Add(ioEntity);
                }
                processed++;
            }

            scanData.FoundComponents = components;
            scanData.IsScanning = false;

            if (player != null && player.IsConnected)
            {
                player.ChatMessage(GetMessage("ScanCompleted", player.UserIDString, components.Count));
                
                if (components.Count == 0)
                {
                    player.ChatMessage(GetMessage("NoElectricalFound", player.UserIDString));
                }
                else if (config.Analysis.ShowDetailedReports)
                {
                    ShowScanResults(player, components);
                }
            }
        }

        private void ShowScanResults(BasePlayer player, List<IOEntity> components)
        {
            var report = new StringBuilder();
            report.AppendLine(GetMessage("AnalysisReport", player.UserIDString));

            var powerSources = components.Where(c => IsPowerSource(c)).ToList();
            var powerConsumers = components.Where(c => IsPowerConsumer(c)).ToList();

            report.AppendLine(GetMessage("PowerSources", player.UserIDString, powerSources.Count));
            report.AppendLine(GetMessage("PowerConsumers", player.UserIDString, powerConsumers.Count));

            foreach (var component in components.Take(10)) // Limit output
            {
                var info = GetComponentInfo(component);
                report.AppendLine(GetMessage("ComponentInfo", player.UserIDString, 
                    component.ShortPrefabName, info.Power, info.Status));
            }

            if (components.Count > 10)
            {
                report.AppendLine($"... and {components.Count - 10} more components");
            }

            player.ChatMessage(report.ToString());
        }

        private void AnalyzeElectricalSystem(BasePlayer player)
        {
            if (!activeScanners.TryGetValue(player.userID, out var scanData) || 
                scanData.FoundComponents.Count == 0)
            {
                StartElectricalScan(player);
                return;
            }

            var cacheKey = GenerateCacheKey(player.transform.position, scanData.FoundComponents);
            
            if (config.Performance.EnableCaching && 
                analysisCache.TryGetValue(cacheKey, out var cached) &&
                (DateTime.Now - cached.CacheTime).TotalSeconds < config.Performance.CacheDuration)
            {
                ShowAnalysisResults(player, cached.Analysis);
                return;
            }

            var analysis = PerformElectricalAnalysis(scanData.FoundComponents);
            
            if (config.Performance.EnableCaching)
            {
                analysisCache[cacheKey] = new CachedAnalysisData
                {
                    Analysis = analysis,
                    CacheTime = DateTime.Now
                };
            }

            ShowAnalysisResults(player, analysis);
        }

        private ElectricalAnalysis PerformElectricalAnalysis(List<IOEntity> components)
        {
            var analysis = new ElectricalAnalysis();

            foreach (var component in components)
            {
                if (IsPowerSource(component))
                {
                    analysis.PowerSources.Add(component);
                    analysis.TotalPowerGenerated += GetPowerOutput(component);
                }
                else if (IsPowerConsumer(component))
                {
                    analysis.PowerConsumers.Add(component);
                    analysis.TotalPowerConsumed += GetPowerConsumption(component);
                }
            }

            analysis.Efficiency = analysis.TotalPowerGenerated > 0 
                ? (analysis.TotalPowerConsumed / analysis.TotalPowerGenerated) * 100f 
                : 0f;

            // Analyze issues
            AnalyzeIssues(analysis);
            GenerateSuggestions(analysis);

            return analysis;
        }

        private void AnalyzeIssues(ElectricalAnalysis analysis)
        {
            if (analysis.TotalPowerConsumed > analysis.TotalPowerGenerated)
            {
                analysis.Issues.Add("Insufficient power generation for current consumption");
            }

            if (analysis.Efficiency > 95f)
            {
                analysis.Issues.Add("Circuit may be overloaded");
            }

            if (analysis.PowerSources.Count == 0 && analysis.PowerConsumers.Count > 0)
            {
                analysis.Issues.Add("Power consumers found without power sources");
            }

            // Check for disconnected components
            var disconnected = analysis.PowerConsumers.Where(c => !IsConnectedToPower(c)).ToList();
            if (disconnected.Count > 0)
            {
                analysis.Issues.Add($"{disconnected.Count} components are not connected to power");
            }
        }

        private void GenerateSuggestions(ElectricalAnalysis analysis)
        {
            if (analysis.TotalPowerGenerated > analysis.TotalPowerConsumed * 1.5f)
            {
                analysis.Suggestions.Add("Consider reducing power generation to improve efficiency");
            }

            if (analysis.PowerSources.Count > 1)
            {
                analysis.Suggestions.Add("Consider using electrical branches to better distribute power");
            }

            if (analysis.Issues.Count == 0)
            {
                analysis.Suggestions.Add("System is running optimally");
            }
        }

        private void ShowAnalysisResults(BasePlayer player, ElectricalAnalysis analysis)
        {
            var report = new StringBuilder();
            report.AppendLine(GetMessage("AnalysisReport", player.UserIDString));
            report.AppendLine(GetMessage("PowerSources", player.UserIDString, analysis.PowerSources.Count));
            report.AppendLine(GetMessage("PowerConsumers", player.UserIDString, analysis.PowerConsumers.Count));
            report.AppendLine(GetMessage("TotalPowerGenerated", player.UserIDString, analysis.TotalPowerGenerated));
            report.AppendLine(GetMessage("TotalPowerConsumed", player.UserIDString, analysis.TotalPowerConsumed));
            report.AppendLine(GetMessage("PowerEfficiency", player.UserIDString, analysis.Efficiency));
            
            if (analysis.Issues.Count > 0)
            {
                report.AppendLine(GetMessage("IssuesFound", player.UserIDString, analysis.Issues.Count));
                foreach (var issue in analysis.Issues)
                {
                    report.AppendLine($"• {issue}");
                }
            }
            else
            {
                report.AppendLine(GetMessage("NoIssuesFound", player.UserIDString));
            }

            if (analysis.Suggestions.Count > 0)
            {
                report.AppendLine(GetMessage("SuggestionsAvailable", player.UserIDString, analysis.Suggestions.Count));
                foreach (var suggestion in analysis.Suggestions)
                {
                    report.AppendLine(GetMessage("OptimizationSuggestion", player.UserIDString, suggestion));
                }
            }

            player.ChatMessage(report.ToString());
        }

        private void OptimizeElectricalSystem(BasePlayer player)
        {
            if (!activeScanners.TryGetValue(player.userID, out var scanData) || 
                scanData.FoundComponents.Count == 0)
            {
                player.ChatMessage("Please scan the electrical system first");
                return;
            }

            var optimized = 0;

            if (config.Optimization.AutoFixIssues)
            {
                optimized += FixElectricalIssues(scanData.FoundComponents);
            }

            if (config.Optimization.OptimizePowerFlow)
            {
                optimized += OptimizePowerFlow(scanData.FoundComponents);
            }

            player.ChatMessage(optimized > 0 
                ? GetMessage("OptimizationApplied", player.UserIDString) 
                : "No optimizations were applied");
        }

        private int FixElectricalIssues(List<IOEntity> components)
        {
            int fixedCount = 0;
            // Implementation for fixing common electrical issues
            // This would include reconnecting loose wires, balancing loads, etc.
            return fixedCount;
        }

        private int OptimizePowerFlow(List<IOEntity> components)
        {
            var optimized = 0;
            // Implementation for optimizing power flow
            // This would include rerouting connections for better efficiency
            return optimized;
        }

        private void AnalyzeNearbyElectricals(BasePlayer player, Vector3 position)
        {
            timer.Once(1f, () =>
            {
                if (player != null && player.IsConnected)
                {
                    StartElectricalScan(player);
                }
            });
        }

        private void ShowElectricianHelp(BasePlayer player)
        {
            var help = "Auto Electrician Commands:\n" +
                      "• /electrician scan - Scan for electrical components\n" +
                      "• /eanalyze - Analyze electrical system\n" +
                      "• /eoptimize - Optimize electrical system (admin)\n" +
                      "• /electrician help - Show this help";

            player.ChatMessage(help);
        }

        #endregion

        #region Utility Methods

        private bool IsPowerSource(IOEntity entity)
        {
            return entity is ElectricGenerator || 
                   entity is SolarPanel || 
                   entity.ShortPrefabName.Contains("windmill") ||
                   entity.ShortPrefabName.Contains("generator") ||
                   entity.ShortPrefabName.Contains("solar") ||
                   entity.ShortPrefabName.Contains("wind");
        }

        private bool IsPowerConsumer(IOEntity entity)
        {
            return entity is ElectricBattery ||
                   entity is AutoTurret ||
                   entity is SearchLight ||
                   entity.ShortPrefabName.Contains("light") ||
                   entity.ShortPrefabName.Contains("turret") ||
                   entity.ShortPrefabName.Contains("door");
        }

        private float GetPowerOutput(IOEntity entity)
        {
            if (entity is ElectricGenerator generator)
                return generator.electricAmount;
            
            return entity.GetPassthroughAmount(0);
        }

        private float GetPowerConsumption(IOEntity entity)
        {
            return entity.ConsumptionAmount();
        }

        private bool IsConnectedToPower(IOEntity entity)
        {
            return entity.HasFlag(BaseEntity.Flags.Reserved8); // Powered flag
        }

        private (float Power, string Status) GetComponentInfo(IOEntity entity)
        {
            var power = IsPowerSource(entity) ? GetPowerOutput(entity) : GetPowerConsumption(entity);
            var status = IsConnectedToPower(entity) ? "Powered" : "Unpowered";
            
            if (entity.IsDestroyed)
                status = "Destroyed";
            else if (!entity.IsOn())
                status = "Off";

            return (power, status);
        }

        private string GenerateCacheKey(Vector3 position, List<IOEntity> components)
        {
            return $"{(int)position.x}_{(int)position.z}_{components.Count}";
        }

        #endregion

        #region API

        private List<IOEntity> GetNearbyElectricalComponents(BasePlayer player, float distance = 0)
        {
            if (distance <= 0)
                distance = config.Analysis.MaxScanDistance;

            if (activeScanners.TryGetValue(player.userID, out var scanData))
                return scanData.FoundComponents;

            return new List<IOEntity>();
        }

        private ElectricalAnalysis AnalyzePlayerElectricals(BasePlayer player)
        {
            if (activeScanners.TryGetValue(player.userID, out var scanData))
                return PerformElectricalAnalysis(scanData.FoundComponents);

            return new ElectricalAnalysis();
        }

        #endregion
    }
}

