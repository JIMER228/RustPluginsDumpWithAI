using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    /// <summary>
    /// SettlementManager provides a framework for players to build AI‑driven settlements
    /// on their Rust server.  A settlement consists of a defined zone (via ZoneManager)
    /// and a roster of NPC workers.  Workers can be assigned specialised roles such
    /// as builder, farmer, miner, guard or vendor.  This plugin integrates with
    /// existing AI plugins (PersonalBuilder, PersonalFarmer, PersonalNPC, etc.) to
    /// delegate the heavy lifting of pathfinding and task execution.  The goal of
    /// this plugin is to orchestrate these workers, track settlement resources and
    /// provide a simple command interface for players.
    ///
    /// NOTE: This file is a **skeleton** to demonstrate the concept.  Many of the
    /// functions here contain TODOs where detailed logic should be implemented.
    /// Use this as a starting point to build a fully‑fledged plugin.
    /// </summary>
    [Info("Settlement Manager", "AI Companion", "0.1.0")]
    [Description("Allows players to create AI managed settlements with workers.")]
    public class SettlementManager : CovalencePlugin
    {
        #region Plugin References

        // Existing plugins we depend on.  These will be assigned automatically
        // when the server loads the plugin.  You should ensure that the
        // referenced plugins are installed and loaded or these variables will be null.
        [PluginReference] private Plugin PersonalNPC;
        [PluginReference] private Plugin PersonalBuilder;
        [PluginReference] private Plugin PersonalFarmer;
        [PluginReference] private Plugin ZoneManager;
        [PluginReference] private Plugin Economics;

        #endregion

        #region Data Models

        /// <summary>
        /// Represents a player‑owned settlement.  A settlement has a unique ID,
        /// an owner (steamID) and a collection of workers.  Additional state
        /// variables such as resource stores, settlement level or defences can be
        /// added as needed.
        /// </summary>
        public class Settlement
        {
            /// <summary>
            /// Unique identifier for the settlement.  This could be a GUID or
            /// simply the owner's ID combined with a counter.  It's used to
            /// reference the settlement in ZoneManager.
            /// </summary>
            public string Id;

            /// <summary>
            /// The owner of the settlement (steam ID in string form).
            /// </summary>
            public string OwnerId;

            /// <summary>
            /// Name of the settlement, chosen by the owner when creating it.
            /// </summary>
            public string Name;

            /// <summary>
            /// List of workers currently assigned to this settlement.
            /// </summary>
            public readonly List<NpcWorker> Workers = new List<NpcWorker>();

            /// <summary>
            /// Coordinates of the settlement centre.  This is used when
            /// creating the zone and for workers to navigate relative to the
            /// settlement.  It is stored as a string because the data file
            /// format requires serializable types; Vector3 is not serializable
            /// by default.
            /// </summary>
            public string Centre;

            /// <summary>
            /// Radius of the settlement zone.  Workers should not wander
            /// outside of this radius when performing tasks.
            /// </summary>
            public float Radius;
        }

        /// <summary>
        /// Enum describing the roles available to workers.  Each role corresponds
        /// to an integration with another plugin.  New roles can be added here
        /// along with corresponding logic in the spawner.
        /// </summary>
        public enum WorkerRole
        {
            Builder,
            Farmer,
            Guard,
            Vendor,
            Miner,
            Lumberjack
        }

        /// <summary>
        /// Represents an NPC worker in a settlement.  It holds the role and
        /// references to any objects needed to control the worker.  In a full
        /// implementation you would also persist the worker's last position,
        /// inventory and task queue.
        /// </summary>
        public class NpcWorker
        {
            /// <summary>
            /// Unique ID of the worker (steamID for the NPC).  This is
            /// assigned by the underlying plugin that spawns the bot.
            /// </summary>
            public ulong Id;

            /// <summary>
            /// The role of the worker.
            /// </summary>
            public WorkerRole Role;

            /// <summary>
            /// Human‑readable name of the worker.  Useful for UI.
            /// </summary>
            public string Name;

            /// <summary>
            /// Reference to the controlling object returned by the plugin.
            /// For example, PersonalNPC returns a PlayerBotController when
            /// spawning a bot.  We store this as an object because the type
            /// depends on the plugin used.
            /// </summary>
            public object Controller;
        }

        #endregion

        #region Storage

        // Configuration file for plugin options
        private PluginConfig config;
        private DynamicConfigFile dataFile;

        // All settlements keyed by their unique identifier
        private Dictionary<string, Settlement> settlements = new Dictionary<string, Settlement>();

        #endregion

        #region Configuration

        /// <summary>
        /// Holds all configurable options for the plugin.  Exposed values
        /// include settlement radius, max workers per settlement and cost to
        /// create workers.  Users can adjust these via the config file.
        /// </summary>
        public class PluginConfig
        {
            [JsonProperty("Settlement radius")]
            public float DefaultSettlementRadius = 50f;

            [JsonProperty("Maximum workers per settlement")]
            public int MaxWorkersPerSettlement = 5;

            [JsonProperty("Worker costs")]
            public Dictionary<WorkerRole, double> WorkerCosts = new Dictionary<WorkerRole, double>
            {
                { WorkerRole.Builder, 1000 },
                { WorkerRole.Farmer, 800 },
                { WorkerRole.Guard, 500 },
                { WorkerRole.Vendor, 1500 },
                { WorkerRole.Miner, 700 },
                { WorkerRole.Lumberjack, 600 }
            };
        }

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<PluginConfig>();
                if (config == null)
                    throw new Exception("Config file is empty");
            }
            catch (Exception)
            {
                PrintWarning("Using default configuration");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        #endregion

        #region Initialization

        private void Init()
        {
            // Register chat commands.  These provide the core functionality of
            // the plugin.  Additional commands can be added as needed.
            AddCovalenceCommand("settlement.create", nameof(CmdCreateSettlement));
            AddCovalenceCommand("settlement.addworker", nameof(CmdAddWorker));
            AddCovalenceCommand("settlement.status", nameof(CmdSettlementStatus));
            AddCovalenceCommand("settlement.delete", nameof(CmdDeleteSettlement));

            // Load persistent data (settlements)
            dataFile = Interface.Oxide.DataFileSystem.GetFile(Name);
            LoadData();
        }

        /// <summary>
        /// Loads previously stored settlements from the data file.  If no
        /// settlements exist, initialises an empty dictionary.  In a more
        /// advanced version you could migrate data between versions here.
        /// </summary>
        private void LoadData()
        {
            try
            {
                settlements = dataFile.ReadObject<Dictionary<string, Settlement>>();
            }
            catch
            {
                settlements = new Dictionary<string, Settlement>();
            }
        }

        /// <summary>
        /// Saves settlement data to disk.  Call this after making changes
        /// (creating or deleting settlements, adding workers, etc.).
        /// </summary>
        private void SaveData()
        {
            dataFile.WriteObject(settlements);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Creates a settlement at the player's location.  The player becomes the
        /// owner and can subsequently add workers.  A zone is created via the
        /// ZoneManager plugin to bound the settlement.  Usage: /settlement.create
        /// (creates with default name) or /settlement.create name radius
        /// </summary>
        private void CmdCreateSettlement(IPlayer player, string command, string[] args)
        {
            // Ensure the command was issued by a player (not console)
            if (player.IsServer)
            {
                player.Reply("Only players can create settlements.");
                return;
            }

            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null)
            {
                player.Reply("Only in‑game players can create settlements.");
                return;
            }

            // Determine settlement name and radius
            string name = args.Length > 0 ? args[0] : $"{player.Name}'s Settlement";
            float radius = config.DefaultSettlementRadius;
            if (args.Length > 1)
            {
                float.TryParse(args[1], out radius);
                if (radius <= 0) radius = config.DefaultSettlementRadius;
            }

            // Generate a unique ID (GUID) for the settlement.  We use a guid to
            // avoid collisions across reloads.  The owner ID is kept separately.
            string id = Guid.NewGuid().ToString("N");

            // Create the settlement model
            var settlement = new Settlement
            {
                Id = id,
                OwnerId = player.Id,
                Name = name,
                Centre = basePlayer.transform.position.ToString(),
                Radius = radius
            };
            settlements[id] = settlement;
            SaveData();

            // Create a zone via ZoneManager.  We pass a list of rules to
            // disable PVP and building by non‑owners.  Additional rules can be
            // configured via the config or by plugin commands.
            if (ZoneManager != null)
            {
                var rules = new List<string>
                {
                    "no_pvp",        // disable player versus player combat
                    "undestr",       // prevent building destruction
                    "noplayerloot"   // prevent loot boxes being looted by others
                };
                var msg = new List<string>();
                ZoneManager.Call("CreateOrUpdateZone", id, radius, basePlayer.transform.position, new Vector3(), rules, msg);
            }

            player.Reply($"Settlement '{name}' created with radius {radius} at your current location.  Use /settlement.addworker role to add workers.");
        }

        /// <summary>
        /// Adds a worker of the specified role to the player's settlement.  The
        /// plugin verifies that the settlement exists and that the player is
        /// the owner.  It also checks resource costs using the Economics
        /// plugin (if available).  Usage: /settlement.addworker role
        /// </summary>
        private void CmdAddWorker(IPlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                player.Reply("You must specify a worker role (builder, farmer, guard, vendor, miner, lumberjack)");
                return;
            }

            // Find the player's settlement.  In this demo we assume a player owns only
            // one settlement.  If multiple settlements per player are supported,
            // additional logic would be needed to select the correct one.
            var settlement = settlements.Values.FirstOrDefault(s => s.OwnerId == player.Id);
            if (settlement == null)
            {
                player.Reply("You don't have a settlement.  Use /settlement.create first.");
                return;
            }
            if (settlement.Workers.Count >= config.MaxWorkersPerSettlement)
            {
                player.Reply($"Your settlement already has the maximum number of workers ({config.MaxWorkersPerSettlement}).");
                return;
            }

            // Parse the role
            if (!Enum.TryParse(args[0], true, out WorkerRole role))
            {
                player.Reply($"Unknown worker role '{args[0]}'.  Valid roles: {string.Join(", ", Enum.GetNames(typeof(WorkerRole)))}");
                return;
            }

            // Check cost and deduct from player's balance (Economics).  If the
            // Economics plugin isn't installed the worker is free.
            double cost = 0;
            config.WorkerCosts.TryGetValue(role, out cost);
            if (Economics != null && cost > 0)
            {
                var balance = (double)Economics.Call("GetPlayerMoney", player.Id);
                if (balance < cost)
                {
                    player.Reply($"You need {cost} coins to hire a {role}, but you only have {balance}.");
                    return;
                }
                Economics.Call("Withdraw", player.Id, cost);
            }

            // Spawn the worker via the appropriate plugin.  The details of
            // spawning vary per role and plugin.  We encapsulate this logic in
            // SpawnWorker() for clarity.  If spawning fails the cost should be
            // refunded.
            var worker = SpawnWorker(role, player.Object as BasePlayer, settlement);
            if (worker == null)
            {
                player.Reply("Failed to spawn worker.  Please check server logs for details.");
                if (Economics != null && cost > 0)
                {
                    // Refund
                    Economics.Call("Deposit", player.Id, cost);
                }
                return;
            }

            settlement.Workers.Add(worker);
            SaveData();

            player.Reply($"Hired {role} '{worker.Name}' for your settlement!");
        }

        /// <summary>
        /// Shows information about the player's settlement: workers, roles and
        /// zone details.  Usage: /settlement.status
        /// </summary>
        private void CmdSettlementStatus(IPlayer player, string command, string[] args)
        {
            var settlement = settlements.Values.FirstOrDefault(s => s.OwnerId == player.Id);
            if (settlement == null)
            {
                player.Reply("You don't have a settlement.  Use /settlement.create first.");
                return;
            }

            var msg = $"Settlement '{settlement.Name}' (ID: {settlement.Id})\n";
            msg += $"Centre: {settlement.Centre}, Radius: {settlement.Radius}\n";
            msg += $"Workers ({settlement.Workers.Count}/{config.MaxWorkersPerSettlement}):\n";
            if (settlement.Workers.Count == 0)
            {
                msg += "  None\n";
            }
            else
            {
                foreach (var w in settlement.Workers)
                {
                    msg += $"  • {w.Role} – {w.Name} (ID: {w.Id})\n";
                }
            }
            player.Reply(msg);
        }

        /// <summary>
        /// Deletes the player's settlement and despawns all workers.  Usage:
        /// /settlement.delete
        /// </summary>
        private void CmdDeleteSettlement(IPlayer player, string command, string[] args)
        {
            var settlement = settlements.Values.FirstOrDefault(s => s.OwnerId == player.Id);
            if (settlement == null)
            {
                player.Reply("You don't have a settlement.");
                return;
            }

            // Despawn workers.  We call the appropriate plugin to despawn
            // depending on the worker's role.  After despawning we remove
            // their entry from the settlement.
            foreach (var worker in settlement.Workers)
            {
                try
                {
                    DespawnWorker(worker);
                }
                catch (Exception ex)
                {
                    PrintError($"Error despawning worker {worker.Id}: {ex.Message}");
                }
            }

            // Delete the zone via ZoneManager
            if (ZoneManager != null)
            {
                ZoneManager.Call("EraseZone", settlement.Id);
            }

            settlements.Remove(settlement.Id);
            SaveData();
            player.Reply($"Settlement '{settlement.Name}' deleted.");
        }

        #endregion

        #region Worker Management

        /// <summary>
        /// Spawns a worker of the given role for the specified settlement.  This
        /// method uses the appropriate plugin to spawn the bot and returns a
        /// NpcWorker with all necessary info populated.  If the spawn fails it
        /// returns null.  In a full implementation you should capture
        /// exceptions and handle plugin API changes gracefully.
        /// </summary>
        private NpcWorker SpawnWorker(WorkerRole role, BasePlayer owner, Settlement settlement)
        {
            try
            {
                switch (role)
                {
                    case WorkerRole.Builder:
                        // Spawn a PersonalBuilder bot.  The PersonalBuilder plugin
                        // exposes a chat command /pbuilder; however we can call
                        // its API directly to create a bot if it provides one.  For
                        // demonstration we call the chat command on behalf of
                        // the player.  A production implementation should use
                        // PersonalBuilder.Call("SpawnBot", ...) if available.
                        owner.ChatMessage("Spawning builder bot...");
                        owner.SendConsoleCommand("pbuilder");
                        return new NpcWorker
                        {
                            Id = 0, // Unknown because we aren't capturing the bot's ID
                            Role = role,
                            Name = $"BuilderBot_{settlement.Workers.Count + 1}",
                            Controller = null
                        };

                    case WorkerRole.Farmer:
                        owner.ChatMessage("Spawning farmer bot...");
                        owner.SendConsoleCommand("pfarmer");
                        return new NpcWorker
                        {
                            Id = 0,
                            Role = role,
                            Name = $"FarmerBot_{settlement.Workers.Count + 1}",
                            Controller = null
                        };

                    case WorkerRole.Guard:
                        owner.ChatMessage("Spawning guard bot...");
                        owner.SendConsoleCommand("pnpc");
                        owner.SendConsoleCommand("pnpc combat");
                        return new NpcWorker
                        {
                            Id = 0,
                            Role = role,
                            Name = $"GuardBot_{settlement.Workers.Count + 1}",
                            Controller = null
                        };

                    case WorkerRole.Vendor:
                        owner.ChatMessage("Spawning vendor NPC...");
                        return new NpcWorker
                        {
                            Id = 0,
                            Role = role,
                            Name = $"Vendor_{settlement.Workers.Count + 1}",
                            Controller = null
                        };

                    case WorkerRole.Miner:
                        owner.ChatMessage("Spawning miner bot...");
                        owner.SendConsoleCommand("pnpc auto-farm stone");
                        owner.SendConsoleCommand("pnpc auto-farm metal");
                        owner.SendConsoleCommand("pnpc auto-farm enable");
                        return new NpcWorker
                        {
                            Id = 0,
                            Role = role,
                            Name = $"MinerBot_{settlement.Workers.Count + 1}",
                            Controller = null
                        };

                    case WorkerRole.Lumberjack:
                        owner.ChatMessage("Spawning lumberjack bot...");
                        owner.SendConsoleCommand("pnpc auto-farm wood");
                        owner.SendConsoleCommand("pnpc auto-farm enable");
                        return new NpcWorker
                        {
                            Id = 0,
                            Role = role,
                            Name = $"LumberjackBot_{settlement.Workers.Count + 1}",
                            Controller = null
                        };

                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error spawning {role} worker: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Despawns a worker.  This method should call into the relevant plugin
        /// to remove the bot from the world.  In this skeleton we simply
        /// attempt to call the despawn commands.  A full implementation would
        /// need to look up the bot by ID and call the correct API.
        /// </summary>
        private void DespawnWorker(NpcWorker worker)
        {
            switch (worker.Role)
            {
                case WorkerRole.Builder:
                    // TODO: Use PersonalBuilder.Call("DespawnBot", worker.Id);
                    break;
                case WorkerRole.Farmer:
                    // TODO: Use PersonalFarmer.Call("DespawnFarmer", worker.Id);
                    break;
                case WorkerRole.Guard:
                case WorkerRole.Miner:
                case WorkerRole.Lumberjack:
                    // TODO: Use PersonalNPC.Call("DespawnBot", worker.Id);
                    break;
                case WorkerRole.Vendor:
                    // TODO: Use HumanNPC to despawn
                    break;
            }
        }

        #endregion
    }
}