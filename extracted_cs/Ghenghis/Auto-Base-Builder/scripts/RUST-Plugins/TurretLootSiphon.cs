
using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TurretLootSiphon", "ChatGPT", "0.2.0")]
    [Description("When an AutoTurret or trap kills a player near a turret, moves corpse loot into nearby storage boxes (3–4 per turret).")]
    public class TurretLootSiphon : RustPlugin
    {
        #region Config

        private ConfigData _config;

        private class ConfigData
        {
            public bool Enabled = true;

            // Meters from a turret within which a death is considered part of the trap zone
            public float KillProximityFromTurret = 12f;

            // Meters around the turret to search for destination boxes
            public float BoxSearchRadius = 4f;

            // Max number of storage boxes to use per turret
            public int MaxBoxesPerTurret = 4;

            // Only pull items to boxes whose prefab short names match this whitelist (leave empty to allow any StorageContainer)
            public List<string> AllowedBoxShortPrefabs = new List<string>
            {
                "box.wooden.large", // Large Wood Box
                "box.wooden"        // Small Wood Box
                // add "coffinstorage" etc. if you like
            };

            // Whether to only siphon if the killer is an AutoTurret / FlameTurret / GunTrap (true) vs. any death in the proximity (false)
            public bool RequireTurretOrTrapKiller = true;

            // Log moves to console
            public bool DebugLog = false;
        }

        protected override void LoadDefaultConfig()
        {
            _config = new ConfigData();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<ConfigData>();
                if (_config == null) throw new Exception("Config is null");
            }
            catch
            {
                PrintWarning("Creating a new configuration file.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config, true);

        #endregion

        #region State

        // Remember that a player died and should be siphoned once the corpse spawns
        private class Pending
        {
            public ulong UserId;
            public Vector3 DeathPos;
            public uint TurretNetId; // The nearest turret at time of death
            public double Time;
        }

        private readonly Dictionary<ulong, Pending> _pending = new Dictionary<ulong, Pending>();

        #endregion

        #region Hooks

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!_config.Enabled) return;

            var player = entity as BasePlayer;
            if (player == null || player.IsNpc) return; // Only real players

            // Determine if death should trigger siphon
            bool killerIsTrap = info != null && (info.Initiator is AutoTurret || info.Initiator is GunTrap || info.Initiator is FlameTurret);

            if (_config.RequireTurretOrTrapKiller && !killerIsTrap)
                return;

            // Find the nearest AutoTurret to victim
            var nearestTurret = FindNearestAutoTurret(player.transform.position, _config.KillProximityFromTurret);
            if (nearestTurret == null) return;

            // If victim is authorized on that turret, skip (don't steal your own/ally's items)
            if (IsAuthorizedOnTurret(nearestTurret, player.userID))
                return;

            if (_config.DebugLog)
                Puts($"[TLS] Pending siphon for {player.displayName} near turret {nearestTurret.net.ID}");

            _pending[player.userID] = new Pending
            {
                UserId = player.userID,
                DeathPos = player.transform.position,
                TurretNetId = nearestTurret.net.ID,
                Time = Time.realtimeSinceStartup
            };
        }

        // Corpse is created a moment after death; move items then
        private void OnEntitySpawned(PlayerCorpse corpse)
        {
            if (!_config.Enabled || corpse == null) return;

            Pending pend;
            if (!_pending.TryGetValue(corpse.playerSteamID, out pend))
                return;

            // Sanity check distance (corpse can ragdoll slightly)
            var turret = BaseNetworkable.serverEntities.Find(pend.TurretNetId) as AutoTurret;
            if (turret == null)
            {
                if (_config.DebugLog) Puts("[TLS] Nearest turret disappeared before siphon.");
                _pending.Remove(corpse.playerSteamID);
                return;
            }

            if (Vector3.Distance(turret.transform.position, corpse.transform.position) > _config.KillProximityFromTurret + 3f)
            {
                if (_config.DebugLog) Puts("[TLS] Corpse too far from turret, aborting siphon.");
                _pending.Remove(corpse.playerSteamID);
                return;
            }

            // Collect destination boxes near turret
            var destBoxes = FindNearbyBoxes(turret.transform.position, _config.BoxSearchRadius, _config.MaxBoxesPerTurret);
            if (destBoxes.Count == 0)
            {
                if (_config.DebugLog) Puts("[TLS] No destination boxes found near turret.");
                _pending.Remove(corpse.playerSteamID);
                return;
            }

            // Build destination ItemContainers list
            var destContainers = destBoxes.Select(b => b.inventory).Where(inv => inv != null).ToList();
            int moved = 0, total = 0;

            // Go through main, wear, belt
            foreach (var container in corpse.containers)
            {
                if (container == null) continue;
                // We need a copy because we'll mutate during iteration
                var items = container.itemList.ToList();
                foreach (var item in items)
                {
                    if (item == null) continue;
                    total += item.amount;
                    if (TryMoveItemToAny(item, destContainers))
                        moved += item.amount;
                }
            }

            if (_config.DebugLog)
                Puts($"[TLS] Siphoned {moved}/{total} items from corpse {corpse.playerName} into {destBoxes.Count} boxes near turret {turret.net.ID}.");

            _pending.Remove(corpse.playerSteamID);
        }

        private void Unload()
        {
            _pending.Clear();
        }

        #endregion

        #region Core helpers

        private AutoTurret FindNearestAutoTurret(Vector3 pos, float maxDist)
        {
            var list = Pool.GetList<AutoTurret>();
            Vis.Entities(pos, maxDist, list);
            AutoTurret nearest = null;
            var best = float.MaxValue;
            foreach (var t in list)
            {
                if (t == null || t.IsDestroyed) continue;
                float d = Vector3.Distance(pos, t.transform.position);
                if (d < best)
                {
                    best = d;
                    nearest = t;
                }
            }
            Pool.FreeList(ref list);
            return nearest;
        }

        private bool IsAuthorizedOnTurret(AutoTurret turret, ulong userId)
        {
            if (turret?.authorizedPlayers == null) return false;
            for (int i = 0; i < turret.authorizedPlayers.Count; i++)
                if (turret.authorizedPlayers[i]?.userid == userId) return true;
            return false;
        }

        private List<StorageContainer> FindNearbyBoxes(Vector3 center, float radius, int maxCount)
        {
            var found = Pool.GetList<StorageContainer>();
            Vis.Entities(center, radius, found);

            var filtered = new List<StorageContainer>(maxCount);
            foreach (var box in found)
            {
                if (box == null || box.IsDestroyed) continue;
                var shortName = box.ShortPrefabName; // e.g., "box.wooden.large"
                if (_config.AllowedBoxShortPrefabs != null && _config.AllowedBoxShortPrefabs.Count > 0)
                {
                    if (!_config.AllowedBoxShortPrefabs.Contains(shortName))
                        continue;
                }
                filtered.Add(box);
                if (filtered.Count >= maxCount) break;
            }

            // Sort by distance
            filtered = filtered.OrderBy(b => Vector3.Distance(center, b.transform.position)).Take(maxCount).ToList();
            Pool.FreeList(ref found);
            return filtered;
        }

        private bool TryMoveItemToAny(Item item, List<ItemContainer> destinations)
        {
            foreach (var dest in destinations)
            {
                if (dest == null || dest.IsFull()) continue;
                // MoveToContainer returns true if fully moved (including stacking)
                if (item.MoveToContainer(dest, -1, true))
                    return true;
            }
            return false;
        }

        #endregion

        #region Commands (optional)

        [ChatCommand("tls.scan")]
        private void CmdScan(BasePlayer player, string cmd, string[] args)
        {
            if (player == null) return;
            var t = FindNearestAutoTurret(player.transform.position, 30f);
            if (t == null) { SendReply(player, "<color=#ffa500>[TLS]</color> No turret within 30m."); return; }
            var boxes = FindNearbyBoxes(t.transform.position, _config.BoxSearchRadius, _config.MaxBoxesPerTurret);
            if (boxes.Count == 0) { SendReply(player, $"<color=#ffa500>[TLS]</color> Turret {t.net.ID}: no boxes within {_config.BoxSearchRadius}m."); return; }
            var lines = boxes.Select(b => $"{b.ShortPrefabName} @ {Vector3.Distance(t.transform.position, b.transform.position):0.0}m");
            SendReply(player, $"<color=#ffa500>[TLS]</color> Turret {t.net.ID} boxes:\n - " + string.Join("\n - ", lines));
        }

        #endregion
    }
}
