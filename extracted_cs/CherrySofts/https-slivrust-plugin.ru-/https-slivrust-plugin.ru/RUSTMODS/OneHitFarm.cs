// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Collections.Generic;
using System;
using Rust;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("One Hit Farm", "rustmods.ru", "1.5.2")]
    [Description("Instantly gather resources from trees and ores with one hit")]
    public class OneHitFarm : RustPlugin
    {
        #region Fields
        private ConfigData configData;
        private const string PERMISSION_USE = "onehitfarm.use";
        private const string PERMISSION_ADMIN = "onehitfarm.admin";
        private readonly DateTime startTime = DateTime.UtcNow;
        private bool isInitialized;
        private bool isLoaded;
        private const float MIN_HEALTH = 0f;
        private const float MAX_GATHER_DISTANCE = 5f;
        #endregion

        #region Configuration
        private class ConfigData
        {
            public bool Enabled { get; set; }
            public bool EnableLogging { get; set; }
            public bool RequirePermission { get; set; }
            public bool RequireTool { get; set; }
            public bool EnablePerformanceLogging { get; set; }
            public float MaxGatherDistance { get; set; }

            // Detaylı toplama ayarları
            public ResourceSettings ResourceSettings { get; set; }

            public ConfigData()
            {
                SetDefaults();
            }

            private void SetDefaults()
            {
                Enabled = true;
                EnableLogging = false;
                RequirePermission = true;
                RequireTool = true;
                EnablePerformanceLogging = false;
                MaxGatherDistance = 5f;

                ResourceSettings = new ResourceSettings
                {
                    // Rock ayarları
                    Rock = new ToolSettings
                    {
                        Enabled = true,
                        GatherMultiplier = 0.1f,
                        MinAmount = 1,
                        MaxMultiplier = 1.0f,
                        GatherAttempts = 2,
                        DamageAmount = 1f,
                        ShowGatherEffects = true
                    },

                    // Normal alet ayarları
                    Tools = new ToolSettings
                    {
                        Enabled = true,
                        GatherMultiplier = 1.0f,
                        MinAmount = 1,
                        MaxMultiplier = 10.0f,
                        GatherAttempts = 1,
                        DamageAmount = 50f,
                        ShowGatherEffects = true
                    },

                    // Kaynak tipi ayarları
                    ResourceTypes = new ResourceTypeSettings
                    {
                        // Ağaç ayarları
                        Tree = new ResourceTypeConfig
                        {
                            Enabled = true,
                            BaseMultiplier = 1.0f,
                            MinAmount = 1,
                            MaxAmount = 1000,
                            EffectPrefab = "assets/bundled/prefabs/fx/hit_mark_wood.prefab",
                            CustomDrops = new List<CustomDrop>()
                        },

                        // Metal ayarları
                        Metal = new ResourceTypeConfig
                        {
                            Enabled = true,
                            BaseMultiplier = 1.0f,
                            MinAmount = 1,
                            MaxAmount = 1000,
                            EffectPrefab = "assets/bundled/prefabs/fx/hit_mark_metal.prefab",
                            CustomDrops = new List<CustomDrop>()
                        },

                        // Sülfür ayarları
                        Sulfur = new ResourceTypeConfig
                        {
                            Enabled = true,
                            BaseMultiplier = 1.0f,
                            MinAmount = 1,
                            MaxAmount = 1000,
                            EffectPrefab = "assets/bundled/prefabs/fx/ore_break.prefab",
                            CustomDrops = new List<CustomDrop>()
                        },

                        // Taş ayarları
                        Stone = new ResourceTypeConfig
                        {
                            Enabled = true,
                            BaseMultiplier = 1.0f,
                            MinAmount = 1,
                            MaxAmount = 1000,
                            EffectPrefab = "assets/bundled/prefabs/fx/hit_mark_stone.prefab",
                            CustomDrops = new List<CustomDrop>()
                        }
                    }
                };
            }
        }

        private class ResourceSettings
        {
            public ToolSettings Rock { get; set; }
            public ToolSettings Tools { get; set; }
            public ResourceTypeSettings ResourceTypes { get; set; }
        }

        private class ToolSettings
        {
            public bool Enabled { get; set; }
            public float GatherMultiplier { get; set; }
            public int MinAmount { get; set; }
            public float MaxMultiplier { get; set; }
            public int GatherAttempts { get; set; }
            public float DamageAmount { get; set; }
            public bool ShowGatherEffects { get; set; }
        }

        private class ResourceTypeSettings
        {
            public ResourceTypeConfig Tree { get; set; }
            public ResourceTypeConfig Metal { get; set; }
            public ResourceTypeConfig Sulfur { get; set; }
            public ResourceTypeConfig Stone { get; set; }
        }

        private class ResourceTypeConfig
        {
            public bool Enabled { get; set; }
            public float BaseMultiplier { get; set; }
            public int MinAmount { get; set; }
            public int MaxAmount { get; set; }
            public string EffectPrefab { get; set; }
            public List<CustomDrop> CustomDrops { get; set; }
        }

        private class CustomDrop
        {
            public string ItemShortName { get; set; }
            public float Chance { get; set; }
            public int MinAmount { get; set; }
            public int MaxAmount { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            configData = new ConfigData();
            Config.WriteObject(configData, true);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                configData = Config.ReadObject<ConfigData>();
                if (configData == null)
                {
                    throw new Exception("Config is null!");
                }
                ValidateConfig();
            }
            catch (Exception ex)
            {
                PrintError($"Configuration error: {ex.Message}");
                LoadDefaultConfig();
            }
        }

        private void ValidateConfig()
        {
            if (configData.ResourceSettings == null)
                configData.ResourceSettings = new ResourceSettings();

            // Rock ayarlarını kontrol et
            if (configData.ResourceSettings.Rock == null)
                configData.ResourceSettings.Rock = new ToolSettings();

            // Tool ayarlarını kontrol et
            if (configData.ResourceSettings.Tools == null)
                configData.ResourceSettings.Tools = new ToolSettings();

            // Resource type ayarlarını kontrol et
            if (configData.ResourceSettings.ResourceTypes == null)
                configData.ResourceSettings.ResourceTypes = new ResourceTypeSettings();

            // Sınırları kontrol et
            configData.MaxGatherDistance = Mathf.Clamp(configData.MaxGatherDistance, 1f, 10f);
            
            ValidateToolSettings(configData.ResourceSettings.Rock);
            ValidateToolSettings(configData.ResourceSettings.Tools);
            ValidateResourceTypeSettings(configData.ResourceSettings.ResourceTypes);

            SaveConfig();
        }

        private void ValidateToolSettings(ToolSettings settings)
        {
            if (settings == null) return;

            settings.GatherMultiplier = Mathf.Clamp(settings.GatherMultiplier, 0.1f, 100f);
            settings.MinAmount = Mathf.Max(1, settings.MinAmount);
            settings.MaxMultiplier = Mathf.Clamp(settings.MaxMultiplier, 0.1f, 100f);
            settings.GatherAttempts = Mathf.Clamp(settings.GatherAttempts, 1, 10);
            settings.DamageAmount = Mathf.Clamp(settings.DamageAmount, 0.1f, 1000f);
        }

        private void ValidateResourceTypeSettings(ResourceTypeSettings settings)
        {
            if (settings == null) return;

            ValidateResourceTypeConfig(settings.Tree);
            ValidateResourceTypeConfig(settings.Metal);
            ValidateResourceTypeConfig(settings.Sulfur);
            ValidateResourceTypeConfig(settings.Stone);
        }

        private void ValidateResourceTypeConfig(ResourceTypeConfig config)
        {
            if (config == null) return;

            config.BaseMultiplier = Mathf.Clamp(config.BaseMultiplier, 0.1f, 100f);
            config.MinAmount = Mathf.Max(1, config.MinAmount);
            config.MaxAmount = Mathf.Max(config.MinAmount, config.MaxAmount);

            if (string.IsNullOrEmpty(config.EffectPrefab))
                config.EffectPrefab = "assets/bundled/prefabs/fx/hit_mark_metal.prefab";

            if (config.CustomDrops == null)
                config.CustomDrops = new List<CustomDrop>();
        }
        #endregion

        #region Localization
        protected override void LoadDefaultMessages()
        {
            // Load language files
            LoadLanguageFile("en");
            LoadLanguageFile("tr");
            LoadLanguageFile("de");
            LoadLanguageFile("fr");
            LoadLanguageFile("ru");
        }

        private void LoadLanguageFile(string code)
        {
            var messages = new Dictionary<string, string>();
            var path = $"lang/{code}.json";

            try
            {
                if (Interface.Oxide.DataFileSystem.ExistsDatafile(path))
                {
                    messages = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, string>>(path);
                }
                else
                {
                    messages = GetDefaultMessages(code);
                }

                lang.RegisterMessages(messages, this, code);
            }
            catch (Exception ex)
            {
                PrintError($"Error loading language file {code}: {ex.Message}");
            }
        }

        private Dictionary<string, string> GetDefaultMessages(string code)
        {
            switch (code.ToLower())
            {
                case "tr":
                    return new Dictionary<string, string>
                    {
                        ["NoPermission"] = "Bu komutu kullanma izniniz yok!",
                        ["Usage"] = "Kullanım: /onehitfarm <on|off|status>",
                        ["Enabled"] = "OneHitFarm aktif edildi!",
                        ["Disabled"] = "OneHitFarm devre dışı bırakıldı!",
                        ["Status"] = "OneHitFarm şu anda {0}",
                        ["StatusEnabled"] = "aktif",
                        ["StatusDisabled"] = "devre dışı",
                        ["InvalidArg"] = "Geçersiz argüman! Kullanım: /onehitfarm <on|off|status>",
                        ["ToolUsed"] = "Kullanılan alet: {0}",
                        ["HitTree"] = "{0} bir ağaca vurdu!",
                        ["HitOre"] = "{0} bir madene vurdu!",
                        ["ResourceGathered"] = "{0} kaynak topladı! Miktar: {1}",
                        ["Error"] = "Bir hata oluştu: {0}"
                    };
                case "de":
                    return new Dictionary<string, string>
                    {
                        ["NoPermission"] = "Sie haben keine Berechtigung, diesen Befehl zu verwenden!",
                        ["Usage"] = "Verwendung: /onehitfarm <on|off|status>",
                        ["Enabled"] = "OneHitFarm wurde aktiviert!",
                        ["Disabled"] = "OneHitFarm wurde deaktiviert!",
                        ["Status"] = "OneHitFarm ist derzeit {0}",
                        ["StatusEnabled"] = "aktiviert",
                        ["StatusDisabled"] = "deaktiviert",
                        ["InvalidArg"] = "Ungültiges Argument! Verwendung: /onehitfarm <on|off|status>",
                        ["ToolUsed"] = "Verwendetes Werkzeug: {0}",
                        ["HitTree"] = "{0} hat einen Baum getroffen!",
                        ["HitOre"] = "{0} hat ein Erz getroffen!",
                        ["ResourceGathered"] = "{0} hat Ressourcen gesammelt! Menge: {1}",
                        ["Error"] = "Ein Fehler ist aufgetreten: {0}"
                    };
                case "fr":
                    return new Dictionary<string, string>
                    {
                        ["NoPermission"] = "Vous n'avez pas la permission d'utiliser cette commande!",
                        ["Usage"] = "Utilisation: /onehitfarm <on|off|status>",
                        ["Enabled"] = "OneHitFarm a été activé!",
                        ["Disabled"] = "OneHitFarm a été désactivé!",
                        ["Status"] = "OneHitFarm est actuellement {0}",
                        ["StatusEnabled"] = "activé",
                        ["StatusDisabled"] = "désactivé",
                        ["InvalidArg"] = "Argument invalide! Utilisation: /onehitfarm <on|off|status>",
                        ["ToolUsed"] = "Outil utilisé: {0}",
                        ["HitTree"] = "{0} a frappé un arbre!",
                        ["HitOre"] = "{0} a frappé un minerai!",
                        ["ResourceGathered"] = "{0} a récolté des ressources! Quantité: {1}",
                        ["Error"] = "Une erreur s'est produite: {0}"
                    };
                case "ru":
                    return new Dictionary<string, string>
                    {
                        ["NoPermission"] = "У вас нет разрешения использовать эту команду!",
                        ["Usage"] = "Использование: /onehitfarm <on|off|status>",
                        ["Enabled"] = "OneHitFarm был включен!",
                        ["Disabled"] = "OneHitFarm был отключен!",
                        ["Status"] = "OneHitFarm сейчас {0}",
                        ["StatusEnabled"] = "включен",
                        ["StatusDisabled"] = "выключен",
                        ["InvalidArg"] = "Неверный аргумент! Использование: /onehitfarm <on|off|status>",
                        ["ToolUsed"] = "Использованный инструмент: {0}",
                        ["HitTree"] = "{0} ударил дерево!",
                        ["HitOre"] = "{0} ударил руду!",
                        ["ResourceGathered"] = "{0} собрал ресурсы! Количество: {1}",
                        ["Error"] = "Произошла ошибка: {0}"
                    };
                default:
                    return new Dictionary<string, string>
                    {
                        ["NoPermission"] = "You don't have permission to use this command!",
                        ["Usage"] = "Usage: /onehitfarm <on|off|status>",
                        ["Enabled"] = "OneHitFarm has been enabled!",
                        ["Disabled"] = "OneHitFarm has been disabled!",
                        ["Status"] = "OneHitFarm is currently {0}",
                        ["StatusEnabled"] = "enabled",
                        ["StatusDisabled"] = "disabled",
                        ["InvalidArg"] = "Invalid argument! Usage: /onehitfarm <on|off|status>",
                        ["ToolUsed"] = "Tool used: {0}",
                        ["HitTree"] = "{0} hit a tree!",
                        ["HitOre"] = "{0} hit an ore!",
                        ["ResourceGathered"] = "{0} gathered resources! Amount: {1}",
                        ["Error"] = "An error occurred: {0}"
                    };
            }
        }

        private string Lang(string key, string userId = null, params object[] args)
        {
            try
            {
                return string.Format(lang.GetMessage(key, this, userId), args);
            }
            catch (Exception)
            {
                return key;
            }
        }
        #endregion

        #region Helper Methods
        private void LogError(string message)
        {
            // Loglama devre dışı
            return;
        }

        private void LogInfo(string message)
        {
            // Loglama devre dışı
            return;
        }

        private bool IsCompatibleVersion()
        {
            return true;
        }

        private void LogPerformance(string methodName, long startTime)
        {
            // Loglama devre dışı
            return;
        }

        private bool IsWithinGatherLimit(BasePlayer player)
        {
            if (player == null) return false;
            
            // Anti-cheat checks
            if (!player.IsConnected || player.IsSleeping() || player.IsSpectating())
                return false;
                
            // Player health status check
            if (player.IsDead() || player.IsWounded())
                return false;

            return true;
        }
        #endregion

        #region Core Methods
        private bool HasAllowedTool(BasePlayer player)
        {
            if (!configData.RequireTool) return true;
            if (player == null || !player.IsConnected || player.IsDead()) return false;

            try
            {
                Item activeItem = player.GetActiveItem();
                if (activeItem?.info == null) return false;

                // Geçerli alet kontrolü
                if (activeItem.info.category != ItemCategory.Tool && 
                    activeItem.info.category != ItemCategory.Weapon)
                    return false;

                // Durabilite kontrolü
                if (activeItem.hasCondition && activeItem.condition <= 0)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                if (configData.EnableLogging)
                    LogError($"HasAllowedTool error: {ex.Message}");
                return false;
            }
        }

        private void ProcessResource(BaseEntity entity, BasePlayer player)
        {
            if (!isInitialized || !configData.Enabled || entity == null || player == null) return;

            try
            {
                if (!IsWithinGatherLimit(player)) return;
                
                float distance = Vector3.Distance(player.transform.position, entity.transform.position);
                if (distance > configData.MaxGatherDistance)
                {
                    if (configData.EnableLogging)
                        LogInfo($"Player {player.displayName} tried to gather from too far away: {distance}m");
                    return;
                }

                // Performans için tip kontrolünü optimize et
                if (entity is TreeEntity tree && !tree.IsDestroyed)
                {
                    ProcessTree(tree, player);
                    return;
                }

                if (entity is ResourceEntity resource && !resource.IsDestroyed)
                {
                    ProcessOre(resource, player);
                }
            }
            catch (Exception ex)
            {
                LogError($"ProcessResource error: {ex.Message}");
            }
        }

        private void ProcessTree(TreeEntity tree, BasePlayer player)
        {
            if (tree == null || tree.IsDestroyed) return;

            try
            {
                if (tree.health <= MIN_HEALTH) return;

                var tool = player.GetActiveItem();
                if (tool == null) return;

                // Config ayarlarını al
                ToolSettings toolSettings = tool.info.shortname.Contains("rock") 
                    ? configData.ResourceSettings.Rock 
                    : configData.ResourceSettings.Tools;

                if (!toolSettings.Enabled) return;

                // Gather Manager çarpanını al
                float gatherManagerMultiplier = 1f;
                var gatherManager = plugins.Find("GatherManager");
                if (gatherManager != null)
                {
                    try
                    {
                        gatherManagerMultiplier = Convert.ToSingle(gatherManager.Call("GetGatherMultiplier", "wood"));
                        if (configData.EnableLogging)
                            LogInfo($"GatherManager multiplier for wood: {gatherManagerMultiplier}");
                    }
                    catch (Exception ex)
                    {
                        LogError($"GatherManager error in ProcessTree: {ex.Message}");
                    }
                }

                // Ağaç toplama işlemi
                var dispenser = tree.GetComponent<ResourceDispenser>();
                if (dispenser != null)
                {
                    var hit = CreateHitInfo(player, tree, tool);

                    // Çarpanları hesapla
                    float baseMultiplier = configData.ResourceSettings.ResourceTypes.Tree.BaseMultiplier;
                    float toolMultiplier = toolSettings.GatherMultiplier;
                    float finalMultiplier = (baseMultiplier * toolMultiplier) / gatherManagerMultiplier;

                    // Hit info'yu ayarla
                    hit.gatherScale = finalMultiplier;
                    hit.damageTypes.Add(tool.info.shortname.Contains("rock") ? DamageType.Blunt : DamageType.Generic, toolSettings.DamageAmount);

                    // Cache contained items
                    var containedItems = dispenser.containedItems?.ToList();
                    if (containedItems != null && containedItems.Count > 0)
                    {
                        for (int i = 0; i < toolSettings.GatherAttempts; i++)
                        {
                            foreach (var item in containedItems)
                            {
                                if (item == null || item.amount <= 0) continue;

                                try
                                {
                                    var itemDef = ItemManager.FindItemDefinition(item.itemid);
                                    if (itemDef == null) continue;

                                    int amount = Mathf.CeilToInt(item.amount * finalMultiplier);
                                    amount = Mathf.Min(amount, configData.ResourceSettings.ResourceTypes.Tree.MaxAmount);
                                    amount = Mathf.Max(configData.ResourceSettings.ResourceTypes.Tree.MinAmount, amount);

                                    if (amount > 0)
                                    {
                                        // Perform gather
                                        dispenser.OnAttacked(hit);

                                        // Give item to player
                                        Item collect = ItemManager.Create(itemDef, amount, 0uL);
                                        if (!collect.MoveToContainer(player.inventory.containerMain))
                                        {
                                            collect.Drop(player.GetDropPosition(), player.GetDropVelocity());
                                        }

                                        // Show gather amount
                                        player.Command("note.inv", new object[] { itemDef.itemid, amount });

                                        if (configData.EnableLogging)
                                        {
                                            LogInfo($"Tree gathered\n" +
                                                   $"Base Amount: {item.amount}\n" +
                                                   $"GatherManager Multiplier: {gatherManagerMultiplier}\n" +
                                                   $"Base Multiplier: {baseMultiplier}\n" +
                                                   $"Tool Multiplier: {toolMultiplier}\n" +
                                                   $"Final Multiplier: {finalMultiplier}\n" +
                                                   $"Final Amount: {amount}");
                                        }
                                    }
                                }
                                catch (Exception itemEx)
                                {
                                    LogError($"Tree item processing error: {itemEx.Message}");
                                }
                            }
                        }
                    }

                    // Show effects
                    if (toolSettings.ShowGatherEffects)
                    {
                        var effectPrefab = configData.ResourceSettings.ResourceTypes.Tree.EffectPrefab;
                        if (!string.IsNullOrEmpty(effectPrefab))
                        {
                            Effect.server.Run(effectPrefab, tree.transform.position, Vector3.up, null, false);
                        }
                    }

                    // Process custom drops
                    ProcessCustomDrops(player, configData.ResourceSettings.ResourceTypes.Tree);
                }

                // Destroy tree
                tree.health = MIN_HEALTH;
                NextTick(() => 
                {
                    if (tree != null && !tree.IsDestroyed)
                    {
                        tree.Kill(BaseNetworkable.DestroyMode.Gib);
                    }
                });
            }
            catch (Exception ex)
            {
                LogError($"ProcessTree error: {ex.Message}");
            }
        }

        private void ProcessOre(ResourceEntity resource, BasePlayer player)
        {
            if (resource == null || resource.IsDestroyed || player == null) return;

            try
            {
                var dispenser = resource.GetComponent<ResourceDispenser>();
                if (dispenser == null) return;

                var tool = player.GetActiveItem();
                if (tool == null || tool.GetHeldEntity() == null) return;

                if (resource.health <= MIN_HEALTH) return;

                // Determine resource type
                ResourceTypeConfig resourceConfig = null;
                string effectPrefab = "";
                string resourceType = "";

                if (resource.ShortPrefabName.Contains("sulfur", StringComparison.OrdinalIgnoreCase))
                {
                    resourceConfig = configData.ResourceSettings.ResourceTypes.Sulfur;
                    resourceType = "sulfur.ore";
                    effectPrefab = resourceConfig.EffectPrefab;
                }
                else if (resource.ShortPrefabName.Contains("metal", StringComparison.OrdinalIgnoreCase))
                {
                    resourceConfig = configData.ResourceSettings.ResourceTypes.Metal;
                    resourceType = "metal.ore";
                    effectPrefab = resourceConfig.EffectPrefab;
                }
                else if (resource.ShortPrefabName.Contains("stone", StringComparison.OrdinalIgnoreCase))
                {
                    resourceConfig = configData.ResourceSettings.ResourceTypes.Stone;
                    resourceType = "stone";
                    effectPrefab = resourceConfig.EffectPrefab;
                }

                if (resourceConfig == null || !resourceConfig.Enabled) return;

                // Get tool settings
                ToolSettings toolSettings = tool.info.shortname.Contains("rock") 
                    ? configData.ResourceSettings.Rock 
                    : configData.ResourceSettings.Tools;

                if (!toolSettings.Enabled) return;

                // Gather Manager çarpanını al
                float gatherManagerMultiplier = 1f;
                var gatherManager = plugins.Find("GatherManager");
                if (gatherManager != null)
                {
                    try
                    {
                        gatherManagerMultiplier = Convert.ToSingle(gatherManager.Call("GetGatherMultiplier", resourceType));
                        if (configData.EnableLogging)
                            LogInfo($"GatherManager multiplier for {resourceType}: {gatherManagerMultiplier}");
                    }
                    catch (Exception ex)
                    {
                        LogError($"GatherManager error: {ex.Message}");
                    }
                }

                // Create HitInfo once and reuse
                var hit = CreateHitInfo(player, resource, tool);

                // Çarpanları hesapla
                float baseMultiplier = resourceConfig.BaseMultiplier;
                float toolMultiplier = toolSettings.GatherMultiplier;
                float finalMultiplier = (baseMultiplier * toolMultiplier) / gatherManagerMultiplier;

                // Hit info'yu ayarla
                hit.gatherScale = finalMultiplier;
                hit.damageTypes.Add(tool.info.shortname.Contains("rock") ? DamageType.Blunt : DamageType.Generic, toolSettings.DamageAmount);

                // Cache contained items
                var containedItems = dispenser.containedItems?.ToList();
                if (containedItems == null || containedItems.Count == 0) return;

                // Gather işlemi
                for (int i = 0; i < toolSettings.GatherAttempts; i++)
                {
                    foreach (var item in containedItems)
                    {
                        if (item == null || item.amount <= 0) continue;

                        try
                        {
                            var itemDef = ItemManager.FindItemDefinition(item.itemid);
                            if (itemDef == null) continue;

                            int amount = Mathf.CeilToInt(item.amount * finalMultiplier);
                            amount = Mathf.Min(amount, resourceConfig.MaxAmount);
                            amount = Mathf.Max(resourceConfig.MinAmount, amount);

                            if (amount > 0)
                            {
                                // Perform gather
                                dispenser.OnAttacked(hit);

                                // Give item to player
                                Item collect = ItemManager.Create(itemDef, amount, 0uL);
                                if (!collect.MoveToContainer(player.inventory.containerMain))
                                {
                                    collect.Drop(player.GetDropPosition(), player.GetDropVelocity());
                                }

                                // Show gather amount
                                player.Command("note.inv", new object[] { itemDef.itemid, amount });

                                if (configData.EnableLogging)
                                {
                                    LogGatherInfo(resourceType, item.amount, gatherManagerMultiplier, baseMultiplier, toolMultiplier, finalMultiplier, amount, 0, 0);
                                }
                            }
                        }
                        catch (Exception itemEx)
                        {
                            LogError($"Item processing error: {itemEx.Message}");
                        }
                    }
                }

                // Show effects
                if (toolSettings.ShowGatherEffects && !string.IsNullOrEmpty(effectPrefab))
                {
                    Effect.server.Run(effectPrefab, resource.transform.position, Vector3.up, null, false);
                }

                // Process custom drops
                ProcessCustomDrops(player, resourceConfig);

                // Destroy resource
                DestroyResource(resource);
            }
            catch (Exception ex)
            {
                LogError($"ProcessOre error: {ex.Message}");
            }
        }

        private void DestroyResource(ResourceEntity resource)
        {
            if (resource == null) return;
            
            resource.health = MIN_HEALTH;
            NextTick(() => 
            {
                if (resource != null && !resource.IsDestroyed)
                {
                    resource.Kill(BaseNetworkable.DestroyMode.Gib);
                }
            });
        }

        private HitInfo CreateHitInfo(BasePlayer player, BaseEntity resource, Item tool)
        {
            var hit = new HitInfo
            {
                Initiator = player,
                HitEntity = resource,
                HitPositionWorld = resource.transform.position,
                HitNormalWorld = Vector3.up,
                HitMaterial = StringPool.Get("Rock"),
                WeaponPrefab = tool.GetHeldEntity(),
                damageTypes = new DamageTypeList(),
                DoHitEffects = true,
                DidHit = true,
                PointStart = player.transform.position,
                PointEnd = resource.transform.position
            };

            // Optimize edilmiş gather scale hesaplama
            var resourceConfig = GetResourceType(resource);
            hit.gatherScale = tool.info.shortname.Contains("rock") 
                ? configData.ResourceSettings.Rock.GatherMultiplier
                : (resourceConfig?.BaseMultiplier ?? configData.ResourceSettings.Tools.GatherMultiplier);

            return hit;
        }

        private ResourceTypeConfig GetResourceType(BaseEntity resource)
        {
            if (resource == null) return null;

            var shortName = resource.ShortPrefabName.ToLower();
            
            if (shortName.Contains("sulfur"))
                return configData.ResourceSettings.ResourceTypes.Sulfur;
            if (shortName.Contains("metal"))
                return configData.ResourceSettings.ResourceTypes.Metal;
            if (shortName.Contains("stone"))
                return configData.ResourceSettings.ResourceTypes.Stone;
            if (shortName.Contains("tree") || shortName.Contains("wood"))
                return configData.ResourceSettings.ResourceTypes.Tree;

            return null;
        }

        protected override void SaveConfig()
        {
            if (configData == null)
                configData = new ConfigData();

            Config.WriteObject(configData, true);
        }

        private void InitializePlugin()
        {
            try
            {
                // Permission'ları sadece kayıtlı değillerse kaydet
                if (!permission.PermissionExists(PERMISSION_USE))
                    permission.RegisterPermission(PERMISSION_USE, this);
                    
                if (!permission.PermissionExists(PERMISSION_ADMIN))
                    permission.RegisterPermission(PERMISSION_ADMIN, this);
                
                LoadDefaultMessages();
                
                isInitialized = true;
                isLoaded = true;

                if (configData.EnableLogging)
                    LogInfo($"Plugin initialized successfully. Version: {Version}");
            }
            catch (Exception ex)
            {
                PrintError($"Failed to initialize plugin: {ex.Message}");
                isInitialized = false;
                isLoaded = false;
            }
        }

        private void OnServerInitialized()
        {
            InitializePlugin();
        }

        private void Unload()
        {
            // SaveConfig kaldırıldı
        }

        private void ProcessTreeGather(TreeEntity tree, BasePlayer player, Item tool, ResourceTypeConfig treeConfig)
        {
            if (tree == null || player == null || tool == null || treeConfig == null) return;

            var dispenser = tree.GetComponent<ResourceDispenser>();
            if (dispenser == null) return;

            var toolSettings = tool.info.shortname.Contains("rock") 
                ? configData.ResourceSettings.Rock 
                : configData.ResourceSettings.Tools;

            var hit = CreateHitInfo(player, tree, tool);
            hit.damageTypes.Add(tool.info.shortname.Contains("rock") ? DamageType.Blunt : DamageType.Generic, toolSettings.DamageAmount);

            for (int i = 0; i < toolSettings.GatherAttempts; i++)
            {
                dispenser.OnAttacked(hit);
            }

            // Efektleri göster
            if (toolSettings.ShowGatherEffects && !string.IsNullOrEmpty(treeConfig.EffectPrefab))
            {
                Effect.server.Run(
                    treeConfig.EffectPrefab,
                    tree.transform.position,
                    Vector3.up,
                    null,
                    false
                );
            }

            // Özel dropları işle
            ProcessCustomDrops(player, treeConfig);
        }

        private void ProcessCustomDrops(BasePlayer player, ResourceTypeConfig resourceConfig)
        {
            if (player == null || resourceConfig?.CustomDrops == null) return;

            foreach (var customDrop in resourceConfig.CustomDrops)
            {
                if (UnityEngine.Random.value <= customDrop.Chance)
                {
                    var itemDef = ItemManager.FindItemDefinition(customDrop.ItemShortName);
                    if (itemDef != null)
                    {
                        int amount = UnityEngine.Random.Range(customDrop.MinAmount, customDrop.MaxAmount + 1);
                        var item = ItemManager.Create(itemDef, amount, 0uL);
                        if (!item.MoveToContainer(player.inventory.containerMain))
                        {
                            item.Drop(player.GetDropPosition(), player.GetDropVelocity());
                        }
                    }
                }
            }
        }

        private float CalculateMultiplier(float baseAmount, float gatherManagerMultiplier, float baseMultiplier, float toolMultiplier)
        {
            if (gatherManagerMultiplier <= 0f) gatherManagerMultiplier = 1f;
            float normalAmount = baseAmount / gatherManagerMultiplier;
            return normalAmount * baseMultiplier * toolMultiplier;
        }

        private int CalculateFinalAmount(float calculatedAmount, int minAmount, int maxAmount, int maxStackSize)
        {
            return Mathf.Min(
                maxStackSize,
                Mathf.Min(
                    maxAmount,
                    Mathf.Max(
                        minAmount,
                        Mathf.CeilToInt(calculatedAmount)
                    )
                )
            );
        }
        #endregion

        #region Hooks
        object OnEntityTakeDamage(BaseEntity entity, HitInfo info)
        {
            if (!isInitialized || !configData.Enabled || entity == null || info?.InitiatorPlayer == null)
                return null;

            try
            {
                var player = info.InitiatorPlayer;

                if (configData.RequirePermission && !permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
                    return null;

                if (!HasAllowedTool(player))
                    return null;

                // Tip kontrolünü optimize et
                if (entity is TreeEntity || entity is ResourceEntity)
                {
                    ProcessResource(entity, player);
                }
            }
            catch
            {
                // Sessizce devam et
            }
            return null;
        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!isInitialized || !configData.Enabled || dispenser == null || entity == null || item == null)
                return;

            try
            {
                var player = entity.ToPlayer();
                if (player == null || !IsWithinGatherLimit(player)) return;

                if (configData.RequirePermission && !permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
                    return;

                if (!HasAllowedTool(player))
                    return;

                if (dispenser.gatherType == ResourceDispenser.GatherType.Tree || 
                    dispenser.gatherType == ResourceDispenser.GatherType.Ore)
                {
                    int maxStackSize = item.MaxStackable();
                    if (maxStackSize <= 0) return;
                    
                    var tool = player.GetActiveItem();
                    if (tool == null) return;

                    // Başlangıç miktarını al (bu Gather Manager'ın etkilemediği ham miktar)
                    float baseAmount = item.amount;

                    // Kaynak tipini belirle
                    ResourceTypeConfig resourceConfig = null;
                    string resourceType = "";

                    if (dispenser.gatherType == ResourceDispenser.GatherType.Tree)
                    {
                        resourceConfig = configData.ResourceSettings.ResourceTypes.Tree;
                        resourceType = "wood";
                    }
                    else
                    {
                        string itemName = item.info.shortname.ToLower();
                        if (itemName.Contains("metal"))
                        {
                            resourceConfig = configData.ResourceSettings.ResourceTypes.Metal;
                            resourceType = "metal.ore";
                        }
                        else if (itemName.Contains("sulfur"))
                        {
                            resourceConfig = configData.ResourceSettings.ResourceTypes.Sulfur;
                            resourceType = "sulfur.ore";
                        }
                        else if (itemName.Contains("stone"))
                        {
                            resourceConfig = configData.ResourceSettings.ResourceTypes.Stone;
                            resourceType = "stone";
                        }
                    }

                    if (resourceConfig == null) return;

                    // Tool ayarlarını al
                    ToolSettings toolSettings = tool.info.shortname.Contains("rock") 
                        ? configData.ResourceSettings.Rock 
                        : configData.ResourceSettings.Tools;

                    // Gather Manager çarpanını al
                    float gatherManagerMultiplier = 1f;
                    var gatherManager = plugins.Find("GatherManager");
                    if (gatherManager != null)
                    {
                        try
                        {
                            gatherManagerMultiplier = Convert.ToSingle(gatherManager.Call("GetGatherMultiplier", resourceType));
                            if (configData.EnableLogging)
                                LogInfo($"GatherManager multiplier for {resourceType}: {gatherManagerMultiplier}");
                        }
                        catch (Exception ex)
                        {
                            LogError($"GatherManager error: {ex.Message}");
                        }
                    }

                    // Yeni hesaplama mantığı
                    float normalAmount = baseAmount / gatherManagerMultiplier; // GatherManager etkisini kaldır
                    float pluginMultiplier = resourceConfig.BaseMultiplier * toolSettings.GatherMultiplier;
                    int finalAmount = Mathf.CeilToInt(normalAmount * pluginMultiplier);

                    // Minimum miktar kontrolü
                    finalAmount = Mathf.Max(toolSettings.MinAmount, finalAmount);

                    // Maksimum miktar kontrolü
                    finalAmount = Mathf.Min(finalAmount, resourceConfig.MaxAmount);
                    finalAmount = Mathf.Min(finalAmount, maxStackSize);

                    // Sonucu uygula
                    item.amount = finalAmount;
                    item.MarkDirty();

                    // Gather miktarını göster
                    player.Command("note.inv", new object[] { item.info.itemid, finalAmount });

                    if (configData.EnableLogging)
                    {
                        LogGatherInfo(resourceType, baseAmount, gatherManagerMultiplier, resourceConfig.BaseMultiplier, toolSettings.GatherMultiplier, pluginMultiplier, finalAmount, maxStackSize, resourceConfig.MaxAmount);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"OnDispenserGather error: {ex.Message}");
            }
        }
        #endregion

        #region Commands
        [ChatCommand("onehitfarm")]
        private void CmdOneHitFarm(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;

            if (!isInitialized || !configData.Enabled)
            {
                SendReply(player, "Plugin is still initializing, please wait...");
                return;
            }

            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN))
            {
                SendReply(player, Lang("NoPermission", player.UserIDString));
                return;
            }

            if (args == null || args.Length == 0)
            {
                SendReply(player, Lang("Usage", player.UserIDString));
                return;
            }

            try
            {
                switch (args[0].ToLower())
                {
                    case "on":
                        if (configData.Enabled)
                        {
                            SendReply(player, "Plugin is already enabled!");
                            return;
                        }
                        configData.Enabled = true;
                        // SaveConfig kaldırıldı
                        SendReply(player, Lang("Enabled", player.UserIDString));
                        break;

                    case "off":
                        if (!configData.Enabled)
                        {
                            SendReply(player, "Plugin is already disabled!");
                            return;
                        }
                        configData.Enabled = false;
                        // SaveConfig kaldırıldı
                        SendReply(player, Lang("Disabled", player.UserIDString));
                        break;

                    case "status":
                        string status = configData.Enabled ? 
                            Lang("StatusEnabled", player.UserIDString) : 
                            Lang("StatusDisabled", player.UserIDString);
                        SendReply(player, Lang("Status", player.UserIDString, status));
                        break;

                    default:
                        SendReply(player, Lang("InvalidArg", player.UserIDString));
                        break;
                }
            }
            catch
            {
                SendReply(player, Lang("Error", player.UserIDString, "An error occurred."));
            }
        }
        #endregion

        private void LogGatherInfo(string resourceType, float baseAmount, float gatherManagerMultiplier, float baseMultiplier, float toolMultiplier, float finalMultiplier, int finalAmount, int maxStackSize = 0, int resourceMax = 0)
        {
            // Loglama devre dışı
            return;
        }
    }
} 