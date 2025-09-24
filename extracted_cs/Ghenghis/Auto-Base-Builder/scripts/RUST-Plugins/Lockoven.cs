using System;
using UnityEngine;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Convert = System.Convert;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Rust;

namespace Oxide.Plugins
{
    [Info("Lock oven", "Julio Juajez", "2.1.1")]
    [Description("Lock your furnaces and refineries")]
    class Lockoven : RustPlugin
    {
        public static Lockoven Instance;

        ConfigData configData;
        HashSet<ulong> playersInAddMode = new HashSet<ulong>();
        
        class OvenConfig
        {
            public Vector3 LockPosition { get; set; }
            public Vector3 LockRotation { get; set; }
            
            public OvenConfig() { }
            
            public OvenConfig(Vector3 pos, Vector3 rot)
            {
                LockPosition = pos;
                LockRotation = rot;
            }
        }
        
        class ConfigData
        { 
            public string RefreshCommand { get; set; } = "lockoven_refresh";
            public string AddCommand { get; set; } = "lockoven_add";
            public string ListCommand { get; set; } = "lockoven_list";
            public string CleanCommand { get; set; } = "lockoven_clean";
            public string ResetConfigCommand { get; set; } = "lockoven_reset";
            public string PermAdmin { get; set; } = "lockoven.admin";
            public bool ActivatePermUse { get; set; } = false;
            public string PermUse { get; set; } = "lockoven.use";
            public Dictionary<string, OvenConfig> SupportedOvens { get; set; }
        }

        protected override void LoadDefaultConfig()
        { 
            configData = GetDefaultConfig();
            SaveConfig(configData);
            PrintWarning("New configuration file created.");
        }
        
        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                RefreshCommand = "lockoven_refresh",
                AddCommand = "lockoven_add",
                ListCommand = "lockoven_list",
                CleanCommand = "lockoven_clean",
                ResetConfigCommand = "lockoven_reset",
                PermAdmin = "lockoven.admin",
                ActivatePermUse = false,
                PermUse = "lockoven.use",
                SupportedOvens = new Dictionary<string, OvenConfig>
                {
                    // Furnace normal
                    ["assets/prefabs/deployable/furnace/furnace.prefab"] = new OvenConfig(
                        new Vector3(-0.02f, 0.3f, 0.5f),
                        new Vector3(0, 90, 0)
                    ),
                    
                    // Legacy furnace (mêmes coordonnées que furnace normal)
                    ["assets/prefabs/deployable/legacyfurnace/legacy_furnace.prefab"] = new OvenConfig(
                        new Vector3(-0.02f, 1.1f, 0.3f),
                        new Vector3(0, 88, -4)
                    ),
                    
                    // Large furnace
                    ["assets/prefabs/deployable/furnace.large/furnace.large.prefab"] = new OvenConfig(
                        new Vector3(0.62f, 1.0f, -0.63f),
                        new Vector3(0, 45, 0)
                    ),
                    
                    // Small oil refinery
                    ["assets/prefabs/deployable/oil refinery/refinery_small_deployed.prefab"] = new OvenConfig(
                        new Vector3(-0.02f, 1.45f, -0.7f),
                        new Vector3(0, 90, 0)
                    ),
                    
                    // Electric furnace
                    ["assets/prefabs/deployable/playerioents/electricfurnace/electricfurnace.deployed.prefab"] = new OvenConfig(
                        new Vector3(0f, 0.2f, 0.3f),
                        new Vector3(0, 90, 0)
                    )
                }
            };
        }

        private void Init()
        {
            Instance = this;
            
            // Charger la config existante ou créer une nouvelle si elle n'existe pas
            try
            {
                configData = Config.ReadObject<ConfigData>();
                if (configData == null)
                {
                    LoadDefaultConfig();
                }
                else
                {
                    // Sauvegarder seulement si la migration a eu lieu
                    bool migrated = MigrateConfigIfNeeded();
                    if (migrated)
                    {
                        SaveConfig(configData);
                        PrintWarning("Configuration updated with new parameters.");
                    }
                }
            }
            catch
            {
                LoadDefaultConfig();
            }
            
            permission.RegisterPermission(configData.PermAdmin, this);
            permission.RegisterPermission(configData.PermUse, this);
            
            cmd.AddChatCommand(configData.RefreshCommand, this, "CmdRefresh");
            cmd.AddChatCommand(configData.AddCommand, this, "CmdAddOven");
            cmd.AddChatCommand(configData.ListCommand, this, "CmdListOvens");
            cmd.AddChatCommand(configData.CleanCommand, this, "CmdCleanOvens");
            cmd.AddChatCommand(configData.ResetConfigCommand, this, "CmdResetConfig");
            
            // Démarrer un timer pour vérifier les marteaux
            timer.Every(2f, CheckHammersInHand);
        }

        private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        
        private bool MigrateConfigIfNeeded()
        {
            bool needsUpdate = false;
            var defaultConfig = GetDefaultConfig();

            Puts("Starting configuration migration check...");
            
            // Lire directement le fichier JSON pour voir ce qui manque vraiment
            var jsonConfig = Config.ReadObject<Dictionary<string, object>>();
            
            // Vérifier si c'est une première installation (aucun SupportedOvens dans le JSON)
            bool isFirstInstall = !jsonConfig.ContainsKey("SupportedOvens");
            
            // Vérifier les nouveaux champs de configuration
            if (!jsonConfig.ContainsKey("AddCommand"))
            {
                configData.AddCommand = defaultConfig.AddCommand;
                needsUpdate = true;
                Puts("Missing AddCommand in JSON file - will be added");
            }
            
            if (!jsonConfig.ContainsKey("ListCommand"))
            {
                configData.ListCommand = defaultConfig.ListCommand;
                needsUpdate = true;
                Puts("Missing ListCommand in JSON file - will be added");
            }
            
            if (!jsonConfig.ContainsKey("CleanCommand"))
            {
                configData.CleanCommand = defaultConfig.CleanCommand;
                needsUpdate = true;
                Puts("Missing CleanCommand in JSON file - will be added");
            }
            
            if (!jsonConfig.ContainsKey("ResetConfigCommand"))
            {
                configData.ResetConfigCommand = defaultConfig.ResetConfigCommand;
                needsUpdate = true;
                Puts("Missing ResetConfigCommand in JSON file - will be added");
            }
            
            if (!jsonConfig.ContainsKey("RefreshCommand"))
            {
                configData.RefreshCommand = defaultConfig.RefreshCommand;
                needsUpdate = true;
                Puts("Missing RefreshCommand in JSON file - will be added");
            }

            // Gestion spéciale pour SupportedOvens
            if (isFirstInstall)
            {
                // Première installation : on ajoute tous les fours par défaut
                configData.SupportedOvens = defaultConfig.SupportedOvens;
                needsUpdate = true;
                Puts($"First installation detected - Added {defaultConfig.SupportedOvens.Count} default ovens");
            }
            else if (configData.SupportedOvens == null)
            {
                // Cas rare où la liste est null mais le champ existe dans le JSON
                configData.SupportedOvens = new Dictionary<string, OvenConfig>();
                needsUpdate = true;
                Puts("SupportedOvens was null - initialized empty dictionary");
            }
            else
            {
                // Vérifier si legacy_furnace existe, sinon l'ajouter
                if (!configData.SupportedOvens.ContainsKey("assets/prefabs/deployable/legacyfurnace/legacy_furnace.prefab"))
                {
                    configData.SupportedOvens["assets/prefabs/deployable/legacyfurnace/legacy_furnace.prefab"] = 
                        defaultConfig.SupportedOvens["assets/prefabs/deployable/legacyfurnace/legacy_furnace.prefab"];
                    needsUpdate = true;
                    Puts("Added missing legacy_furnace.prefab to configuration");
                }
            }

            if (needsUpdate)
            {
                Puts("Configuration migration required - updating JSON file.");
            }
            else
            {
                Puts("Configuration is complete, no migration needed.");
            }

            return needsUpdate;
        }
        
        private void CheckHammersInHand()
        {
            var playersToRemove = new List<ulong>();
            
            foreach (var playerID in playersInAddMode)
            {
                var player = BasePlayer.FindByID(playerID);
                if (player == null || !player.IsConnected)
                {
                    playersToRemove.Add(playerID);
                    continue;
                }
                
                var activeItem = player.GetActiveItem();
                if (activeItem == null || activeItem.info.shortname != "hammer")
                {
                    playersToRemove.Add(playerID);
                    Message(player, "AddModeDisabledNoHammer");
                    Puts($"Admin {player.displayName} left oven add mode (no hammer)");
                }
            }
            
            foreach (var playerID in playersToRemove)
            {
                playersInAddMode.Remove(playerID);
            }
        }

        private void CmdRefresh(BasePlayer player, string command, string[] args)
        {
            if(!permission.UserHasPermission(player.UserIDString, configData.PermAdmin))
            {
                Message(player, "Usage");
                return;
            }
            
            NextTick(() =>
            {
                int count = 0;
                foreach (var entity in UnityEngine.Object.FindObjectsOfType<BaseOven>())
                {
                    if (entity == null) continue;
                    
                    if(isPlayer(entity.OwnerID) && IsSupportedOven(entity.PrefabName))
                    {
                        entity.isLockable = true;
                        entity.SendNetworkUpdate();
                        entity.SendNetworkUpdateImmediate();
                        count++;
                    }
                }
                Message(player, "RefreshSuccess", count);
            });
        }
        
        private void CmdAddOven(BasePlayer player, string command, string[] args)
        {
            if(!permission.UserHasPermission(player.UserIDString, configData.PermAdmin))
            {
                Message(player, "Usage");
                return;
            }

            // Toggle le mode d'ajout
            if (playersInAddMode.Contains(player.userID))
            {
                // Désactiver le mode
                playersInAddMode.Remove(player.userID);
                Message(player, "AddModeDisabled");
                Puts($"Admin {player.displayName} disabled oven add mode");
            }
            else
            {
                // Activer le mode
                playersInAddMode.Add(player.userID);
                Message(player, "AddModeEnabled");
                Puts($"Admin {player.displayName} enabled oven add mode");
            }
        }
        
        private void CmdListOvens(BasePlayer player, string command, string[] args)
        {
            if(!permission.UserHasPermission(player.UserIDString, configData.PermAdmin))
            {
                Message(player, "Usage");
                return;
            }

            Message(player, "OvenListHeader", configData.SupportedOvens.Count);
            foreach (var oven in configData.SupportedOvens)
            {
                string shortName = GetShortName(oven.Key);
                var config = oven.Value;
                SendReply(player, $"- {shortName} | Pos: {config.LockPosition} | Rot: {config.LockRotation}");
            }
        }
        
        private void CmdCleanOvens(BasePlayer player, string command, string[] args)
        {
            if(!permission.UserHasPermission(player.UserIDString, configData.PermAdmin))
            {
                Message(player, "Usage");
                return;
            }

            // Pour les dictionnaires, on vérifie les clés dupliquées (normalement impossible)
            // mais on peut nettoyer les entrées invalides
            var keysToRemove = new List<string>();
            
            foreach (var kvp in configData.SupportedOvens)
            {
                if (string.IsNullOrEmpty(kvp.Key) || kvp.Value == null)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                configData.SupportedOvens.Remove(key);
            }
            
            SaveConfig(configData);
            
            if (keysToRemove.Count > 0)
            {
                Message(player, "InvalidEntriesRemoved", keysToRemove.Count);
                Puts($"Admin {player.displayName} cleaned {keysToRemove.Count} invalid oven entries");
            }
            else
            {
                Message(player, "NoInvalidEntries");
            }
        }
        
        private void CmdResetConfig(BasePlayer player, string command, string[] args)
        {
            if(!permission.UserHasPermission(player.UserIDString, configData.PermAdmin))
            {
                Message(player, "Usage");
                return;
            }

            // Réinitialiser la configuration avec les valeurs par défaut
            configData = GetDefaultConfig();
            SaveConfig(configData);
            
            Message(player, "ConfigReset");
            Puts($"Admin {player.displayName} reset the plugin configuration");
        }

        static bool isPlayer(ulong id) => id > 76560000000000000L;
        
        private bool IsSupportedOven(string prefabName)
        {
            return configData.SupportedOvens.ContainsKey(prefabName);
        }
        
        private string GetShortName(string prefabName)
        {
            return prefabName.Split('/').LastOrDefault()?.Replace(".prefab", "") ?? prefabName;
        }
        
        private bool IsValidOven(BaseOven oven)
        {
            if (oven == null || string.IsNullOrEmpty(oven.PrefabName))
                return false;
                
            // Vérifier que le prefab contient "furnace" ou "refinery" dans son nom (insensible à la casse)
            string prefabLower = oven.PrefabName.ToLower();
            return prefabLower.Contains("furnace") || prefabLower.Contains("refinery");
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            var oven = entity as BaseOven;
            if (oven == null)
            {
                var ent = entity as BaseLock;
                if(ent == null)
                {
                    return;
                }
                else
                {
                    BaseEntity par = ent.GetParentEntity();
                    if(par is BaseOven)
                    {
                        // Vérifier si ce four est supporté
                        if (!IsSupportedOven(par.PrefabName))
                        {
                            return;
                        }
                        
                        if (configData.ActivatePermUse)
                        {
                            if (!permission.UserHasPermission(ent.OwnerID.ToString(), configData.PermUse))
                            {
                                BasePlayer player = BasePlayer.FindByID(ent.OwnerID);
                                var PrefabName = ent.PrefabName;
                                if (player != null)
                                {
                                    Message(player, "NoPerm");
                                    var shortname = "";
                                    if(PrefabName == "assets/prefabs/locks/keylock/lock.key.prefab")
                                    {
                                        shortname = "lock.key";
                                    }
                                    else if(PrefabName == "assets/prefabs/locks/keypad/lock.code.prefab")
                                    {
                                        shortname = "lock.code";
                                    }
                                    player.inventory.GiveItem(ItemManager.CreateByName(shortname, 1));
                                }
                                ent.Kill();
                                return;
                            }
                        }
                        
                        // Appliquer la position et rotation depuis la config
                        if (configData.SupportedOvens.TryGetValue(par.PrefabName, out OvenConfig ovenConfig))
                        {
                            ent.transform.localPosition = ovenConfig.LockPosition;
                            ent.transform.localRotation = Quaternion.Euler(ovenConfig.LockRotation.x, ovenConfig.LockRotation.y, ovenConfig.LockRotation.z);
                        }
                    }
                }
                return;
            }
            else
            {
                // Vérifier si ce four est supporté
                if(!IsSupportedOven(oven.PrefabName))
                {
                    return;
                }
                
                if(oven.OwnerID != 0L && isPlayer(oven.OwnerID))
                {
                    oven.isLockable = true;
                    oven.SendNetworkUpdate();
                    oven.SendNetworkUpdateImmediate();
                }
            }
        }
        
        void OnHammerHit(BasePlayer player, HitInfo info)
        {
            // Vérifier si le joueur est en mode d'ajout
            if (!playersInAddMode.Contains(player.userID)) return;
            
            // Vérifier s'il a les permissions
            if (!permission.UserHasPermission(player.UserIDString, configData.PermAdmin)) return;

            // Vérifier si l'entité touchée est un four
            var entity = info.HitEntity;
            if (entity != null && entity is BaseOven)
            {
                var oven = entity as BaseOven;
                
                // Vérifier que c'est bien un four ou une raffinerie (contient "furnace" ou "refinery" dans le nom)
                if (!IsValidOven(oven)) {
                    Message(player, "NotAnOven");
                    return;
                }
                
                // Vérifier si ce four est déjà supporté
                if (IsSupportedOven(oven.PrefabName))
                {
                    Message(player, "AlreadySupported", GetShortName(oven.PrefabName));
                    return;
                }

                // Ajouter le four à la liste avec des positions par défaut
                // Les positions par défaut peuvent être ajustées dans la config après
                configData.SupportedOvens[oven.PrefabName] = new OvenConfig(
                    new Vector3(0f, 0.5f, 0.5f),  // Position par défaut
                    new Vector3(0, 90, 0)          // Rotation par défaut
                );
                SaveConfig(configData);
                
                // Rendre ce four verrouillable immédiatement
                oven.isLockable = true;
                oven.SendNetworkUpdate();
                oven.SendNetworkUpdateImmediate();

                string shortName = GetShortName(oven.PrefabName);
                Message(player, "OvenAdded", shortName);
                Message(player, "OvenAddedNote");
                Puts($"Admin {player.displayName} added new oven type: {oven.PrefabName}");
            }
        }
        
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (playersInAddMode.Contains(player.userID))
            {
                playersInAddMode.Remove(player.userID);
                Puts($"Admin {player.displayName} left oven add mode (disconnected)");
            }
        }

        protected override void LoadDefaultMessages()
        {
            // Messages en anglais
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Usage", "You do not have permission to use this command"},
                {"RefreshSuccess", "Refreshing complete - {0} ovens updated"},
                {"NoPerm", "You do not have permission to lock this oven"},
                {"NotAnOven", "The target is not a valid oven/furnace"},
                {"AlreadySupported", "This oven type is already supported: {0}"},
                {"OvenAdded", "Successfully added new oven type: {0}"},
                {"OvenAddedNote", "Note: Default lock position applied. Edit config to adjust position if needed."},
                {"OvenListHeader", "Currently supported oven types ({0} total):"},
                {"InvalidEntriesRemoved", "Removed {0} invalid entries"},
                {"NoInvalidEntries", "No invalid entries found"},
                {"AddModeEnabled", "Oven add mode ENABLED. Hit ovens with hammer to add them!"},
                {"AddModeDisabled", "Oven add mode DISABLED"},
                {"AddModeDisabledNoHammer", "Oven add mode disabled (no hammer in hand)"},
                {"ConfigReset", "Configuration has been reset to default values"}
            }, this, "en");
            
            // Messages en français
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Usage", "Vous n'avez pas la permission d'utiliser cette commande"},
                {"RefreshSuccess", "Actualisation terminée - {0} fours mis à jour"},
                {"NoPerm", "Vous n'avez pas la permission de verrouiller ce four"},
                {"NotAnOven", "La cible n'est pas un four/raffinerie valide"},
                {"AlreadySupported", "Ce type de four est déjà supporté : {0}"},
                {"OvenAdded", "Nouveau type de four ajouté avec succès : {0}"},
                {"OvenAddedNote", "Note : Position de verrou par défaut appliquée. Modifiez la config pour ajuster la position si nécessaire."},
                {"OvenListHeader", "Types de fours actuellement supportés ({0} au total) :"},
                {"InvalidEntriesRemoved", "{0} entrées invalides supprimées"},
                {"NoInvalidEntries", "Aucune entrée invalide trouvée"},
                {"AddModeEnabled", "Mode d'ajout de fours ACTIVÉ. Tapez sur les fours avec le marteau pour les ajouter !"},
                {"AddModeDisabled", "Mode d'ajout de fours DÉSACTIVÉ"},
                {"AddModeDisabledNoHammer", "Mode d'ajout de fours désactivé (pas de marteau en main)"},
                {"ConfigReset", "La configuration a été réinitialisée aux valeurs par défaut"}
            }, this, "fr");
        }

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        
        private void Message(BasePlayer player, string key, params object[] args)
        {
            SendReply(player, Lang(key, player.UserIDString, args));
        }
    }
} 