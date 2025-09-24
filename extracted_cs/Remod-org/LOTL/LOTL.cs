#region License (GPL v2)
/*
    Living Off The Loot - Inspired by Faceless
    ------------------------------------------
    NO farming: trees, nodes, barrels, animals
    NO picking up: stone, metal, sulfur, wood
    NO harvesting: wild veg, hemp
    CAN LOOT: bodies, items on the ground, directly lootable objects, your own plants from seed
    CAN CRAFT: all
    ------------------------------------------

    Copyright (c) 2023 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License v2.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion License Information (GPL v2)
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Rust;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("LOTL", "RFC1920", "1.0.4")]
    [Description("Living off the loot!")]
    internal class LOTL : RustPlugin
    {
        private ConfigData configData;
        public static LOTL Instance;

        private const string permBypass = "lotl.bypass";
        private Dictionary<ulong, string> canBypass = new Dictionary<ulong, string>();

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Message(Lang(key, player.Id, args));
        private void LMessage(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["notauthorized"] = "You don't have permission to do that !!",
                ["yougather"] = "You are a gatherer for this session!"
            }, this);
        }

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permBypass, this);
            LoadConfigVariables();

            foreach (BasePlayer pl in BasePlayer.activePlayerList)
            {
                RunChances(pl);
            }
            Instance = this;
        }

        private void DoLog(string message)
        {
            if (configData.debug) Interface.Oxide.LogInfo(message);
        }

        private void Unload()
        {
            foreach (KeyValuePair<ulong, string> userid in canBypass)
            {
                permission.RevokeUserPermission(userid.Key.ToString(), permBypass);
            }
            canBypass = new Dictionary<ulong, string>();
        }

        private void DestroyAll<T>() where T : MonoBehaviour
        {
            foreach (T type in UnityEngine.Object.FindObjectsOfType<T>())
            {
                UnityEngine.Object.Destroy(type);
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            RunChances(player);
        }

        private void RunChances(BasePlayer player)
        {
            if (player.IsAdmin && configData.allowAdmin)
            {
                DoLog($"Granting bypass permission to admin, {player.displayName}");
                permission.GrantUserPermission(player?.UserIDString, permBypass, this);
                canBypass.Remove(player.userID);
                canBypass.Add(player.userID, player.displayName);
                return;
            }
            if (!configData.seedPlayers) return;

            // Seed a small number of players who CAN gather resources to be stolen by others, if they can...
            int chance = UnityEngine.Random.Range(1, 100000);
            if (chance > 50000)
            {
                if (canBypass.Count / BasePlayer.activePlayerList.Count > 0.10f)
                {
                    // Don't allow more than 10% of players at a time to have bypass rights
                    canBypass.Remove(player.userID);
                    return;
                }
                DoLog($"Randomly granting bypass permission to {player.displayName}");
                permission.GrantUserPermission(player?.UserIDString, permBypass, this);
                canBypass.Remove(player.userID);
                canBypass.Add(player.userID, player.displayName);
                Message(player.IPlayer, "yougather");
                return;
            }
            DoLog($"Removing bypass permission from {player.displayName}");
            permission.RevokeUserPermission(player?.UserIDString, permBypass);
            canBypass.Remove(player.userID);
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            permission.RevokeUserPermission(player?.UserIDString, permBypass);
            canBypass.Remove(player.userID);
        }

        private object OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
        {
            if (player == null) return null;
            if (permission.UserHasPermission(player.UserIDString, permBypass)) return null;
            return true;
        }

        private object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity as BasePlayer;
            if (player == null) return null;
            if (permission.UserHasPermission(player.UserIDString, permBypass)) return null;
            return true;
        }

        private object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (player == null) return null;
            if (permission.UserHasPermission(player.UserIDString, permBypass)) return null;
            return true;
        }

        private object OnGrowableGather(GrowableEntity plant, Item item, BasePlayer player)
        {
            if (player == null) return null;
            if (plant == null) return null;
            if (permission.UserHasPermission(player.UserIDString, permBypass)) return null;
            if (player.OwnerID == plant.OwnerID) return null;
            return true;
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            if (hitinfo.damageTypes.GetMajorityDamageType() == DamageType.Decay && !configData.allowDecay) return true;

            BasePlayer player = hitinfo.Initiator as BasePlayer;
            if (player == null) return null;
            if (permission.UserHasPermission(player.UserIDString, permBypass)) return null;
            if (entity.ShortPrefabName == null) return null;
            if (entity.ShortPrefabName.Contains("loot_barrel") || entity.ShortPrefabName.Contains("loot-barrel") || entity is BaseAnimalNPC)
            {
                return true;
            }
            return null;
        }

        private object OnEntityTakeDamage(TreeEntity tree, HitInfo hitinfo)
        {
            BasePlayer player = hitinfo.Initiator as BasePlayer;
            if (player == null) return null;
            if (permission.UserHasPermission(player.UserIDString, permBypass)) return null;
            return true;
        }

        private object OnEntityTakeDamage(OreResourceEntity ore, HitInfo hitinfo)
        {
            BasePlayer player = hitinfo.Initiator as BasePlayer;
            if (player == null) return null;
            if (permission.UserHasPermission(player.UserIDString, permBypass)) return null;
            return true;
        }

        private class ConfigData
        {
            public bool debug;
            public bool seedPlayers;
            public bool allowAdmin;
            public bool allowDecay;
            public VersionNumber Version;
        }

        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();

            configData.Version = Version;
            SaveConfig(configData);
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData()
            {
                debug= false,
                seedPlayers = true,
                allowAdmin = true
            };

            SaveConfig(config);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
    }
}
