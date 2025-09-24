#region License (GPL v2)
/*
    TinCanBoom! Add Explosive to TinCanAlarm
    Copyright (c) 2024 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; version 2
    of the License only.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion License (GPL v2)
using HarmonyLib;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Chaos;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
//Reference: 0Harmony

namespace Oxide.Plugins
{
    [Info("TinCanBoom", "RFC1920", "1.0.3")]
    [Description("Add explosives to TinCanAlarm")]
    internal class TinCanBoom : RustPlugin
    {
        [PluginReference]
        private readonly Plugin Friends, Clans, GridAPI;

        ConfigData configData;
        private Dictionary<ulong, List<TinCanEnhanced>> playerAlarms = new Dictionary<ulong, List<TinCanEnhanced>>();
        public SortedDictionary<string, Vector3> monPos = new SortedDictionary<string, Vector3>();
        public SortedDictionary<string, Vector3> monSize = new SortedDictionary<string, Vector3>();
        public const string permUse = "tincanboom.use";

        private readonly List<string> orDefault = new List<string>();

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Message(Lang(key, player.Id, args));
        private void LMessage(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        public class TinCanEnhanced
        {
            public string location;
            public NetworkableId alarm;
            public NetworkableId te;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                { "off", "OFF" },
                { "on", "ON" },
                { "notauthorized", "You don't have permission to do that !!" },
                { "alarmtripped", "TinCanBoom tripped by {1} at {0}" },
                { "enabled", "TinCanBoom enabled" },
                { "disabled", "TinCanBoom disabled" }
            }, this);
        }

        [Command("ente"), Permission(permUse)]
        private void EnableDisable(IPlayer iplayer, string command, string[] args)
        {
            if (!iplayer.HasPermission(permUse) && configData.Options.RequirePermission) { Message(iplayer, "notauthorized"); return; }

            bool en = configData.Options.startEnabled;
            if (orDefault.Contains(iplayer.Id))
            {
                orDefault.Remove(iplayer.Id);
            }
            else
            {
                orDefault.Add(iplayer.Id);
                en = !en;
            }
            switch (en)
            {
                case true:
                    Message(iplayer, "enabled");
                    break;
                case false:
                    Message(iplayer, "disabled");
                    break;
            }
        }

        private void OnServerInitialized()
        {
            LoadConfigVariables();
            AddCovalenceCommand("ente", "EnableDisable");
            permission.RegisterPermission(permUse, this);
            LoadData();
            FindMonuments();
        }

        private object CanDeployItem(BasePlayer player, Deployer deployer, NetworkableId entityId)
        {
            TinCanAlarm alarm = BaseNetworkable.serverEntities.Find(entityId) as TinCanAlarm;
            if (alarm == null) return null;

            return null;
        }

        private void OnEntitySpawned(TinCanAlarm alarm)
        {
            BasePlayer player = FindPlayerByID(alarm.OwnerID);
            if (player == null) return;
            if (configData.Options.RequirePermission && !player.HasPermission(permUse)) return;

            if (configData.Options.startEnabled && orDefault.Contains(player.UserIDString))
            {
                DoLog("Plugin enabled by default, but player-disabled");
                return;
            }
            else if (!configData.Options.startEnabled && !orDefault.Contains(player.UserIDString))
            {
                DoLog("Plugin disabled by default, and not player-enabled");
                return;
            }

            if (NearMonument(player.transform.position))
            {
                return;
            }

            RFTimedExplosive exp = GameManager.server.CreateEntity("assets/prefabs/tools/c4/explosive.timed.deployed.prefab") as RFTimedExplosive;
            exp.enableSaving = false;
            exp.transform.localPosition = new Vector3(0f, 1f, 0f);
            exp.flags = 0;
            exp.SetFuse(float.PositiveInfinity);
            exp.timerAmountMin = float.PositiveInfinity;
            exp.timerAmountMax = float.PositiveInfinity;

            exp.SetParent(alarm);
            RemoveComps(exp);
            exp.Spawn();
            exp.stickEffect = null;
            UnityEngine.Object.DestroyImmediate(exp.beepLoop);
            exp.beepLoop = null;
            exp._limitedNetworking = true;
            exp.SendNetworkUpdateImmediate();

            SpawnRefresh(exp);
            if (!playerAlarms.ContainsKey(player.userID))
            {
                playerAlarms.Add(player.userID, new List<TinCanEnhanced>());
            }
            playerAlarms[player.userID].Add(new TinCanEnhanced()
            {
                location = alarm.transform.position.ToString(),
                alarm = alarm.net.ID,
                te = exp.net.ID
            });
            SaveData();
        }

        private void DoLog(string message)
        {
            if (configData.Options.debug) Puts(message);
        }

        [AutoPatch]
        [HarmonyPatch(typeof(TinCanAlarm), "TriggerAlarm")]
        public static class TriggerAlarmPatch
        {
            [HarmonyPrefix]
            static void Prefix(TinCanAlarm __instance)
            {
                Interface.CallHook("OnTinCanAlarmTrigger", __instance);
            }
        }

        private void OnTinCanAlarmTrigger(TinCanAlarm alarm)
        {
            if (alarm == null) return;
            RFTimedExplosive te = alarm.gameObject.GetComponentInChildren<RFTimedExplosive>();
            if (te != null)
            {
                te._limitedNetworking = false;
                te.SetFuse(configData.Options.fireDelay);
                te.SetFlag(BaseEntity.Flags.On, true, false, false);

                FieldInfo lastTriggeredBy = typeof(TinCanAlarm).GetField("lastTriggerEntity", (BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
                object victimEnt = lastTriggeredBy.GetValue(alarm);
                string victim = "unknown";
                if (victimEnt is BasePlayer)
                {
                    victim = (victimEnt as BasePlayer).displayName;
                }

                BasePlayer player = FindPlayerByID(alarm.OwnerID);
                if (configData.Options.NotifyOwner)
                {
                    string pos = PositionToGrid(alarm.transform.position);
                    SendReply(player, Lang("alarmtripped", null, pos, victim));
                }
                DoLog($"Removing destroyed alarm from data for {player?.displayName}");

                playerAlarms[player.userID].RemoveAll(x => x.te == te.net.ID);
                SaveData();
            }
        }

        public void RemoveComps(BaseEntity obj)
        {
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<GroundWatch>());
            foreach (MeshCollider mesh in obj.GetComponentsInChildren<MeshCollider>())
            {
                DoLog($"Destroying MeshCollider for {obj.ShortPrefabName}");
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        private BasePlayer FindPlayerByID(ulong userid, bool includeSleepers = true)
        {
            foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.userID == userid)
                {
                    return activePlayer;
                }
            }
            if (includeSleepers)
            {
                foreach (BasePlayer sleepingPlayer in BasePlayer.sleepingPlayerList)
                {
                    if (sleepingPlayer.userID == userid)
                    {
                        return sleepingPlayer;
                    }
                }
            }
            return null;
        }

        private void SpawnRefresh(BaseEntity entity)
        {
            StabilityEntity hasstab = entity.GetComponent<StabilityEntity>();
            if (hasstab != null)
            {
                hasstab.grounded = true;
            }
            BaseMountable hasmount = entity.GetComponent<BaseMountable>();
            if (hasmount != null)
            {
                hasmount.isMobile = true;
            }
        }

        private bool NearMonument(Vector3 pos)
        {
            if (configData.Options.CanDeployAtMonuments) return false;

            foreach (KeyValuePair<string, Vector3> entry in monPos)
            {
                string monname = entry.Key;
                Vector3 monvector = entry.Value;
                float realDistance = monSize[monname].z;
                monvector.y = pos.y;
                float dist = Vector3.Distance(pos, monvector);

                DoLog($"Checking {monname} dist: {dist}, realDistance: {realDistance}");
                if (dist < realDistance)
                {
                    DoLog($"Player in range of {monname}");
                    return true;
                }
            }
            return false;
        }

        public string PositionToGrid(Vector3 position)
        {
            if (GridAPI != null)
            {
                string[] g = (string[])GridAPI.CallHook("GetGrid", position);
                return string.Concat(g);
            }
            else
            {
                // From GrTeleport for display only
                Vector2 r = new Vector2((World.Size / 2) + position.x, (World.Size / 2) + position.z);
                float x = Mathf.Floor(r.x / 146.3f) % 26;
                float z = Mathf.Floor(World.Size / 146.3f) - Mathf.Floor(r.y / 146.3f);

                return $"{(char)('A' + x)}{z - 1}";
            }
        }

        private void FindMonuments()
        {
            foreach (MonumentInfo monument in UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
            {
                if (monument.name.Contains("power_sub") || monument.name.Contains("derwater")) continue;

                float realWidth = 0f;
                string name;
                if (monument.name == "OilrigAI")
                {
                    name = "Small Oilrig";
                    realWidth = 100f;
                }
                else if (monument.name == "OilrigAI2")
                {
                    name = "Large Oilrig";
                    realWidth = 200f;
                }
                else if (monument.name == "assets/bundled/prefabs/autospawn/monument/medium/radtown_small_3.prefab")
                {
                    name = "Sewer Branch";
                    realWidth = 100;
                }
                else
                {
                    name = Regex.Match(monument.name, @"\w{6}\/(.+\/)(.+)\.(.+)").Groups[2].Value.Replace("_", " ").Replace(" 1", "").Titleize();// + " 0";
                }
                if (name.Length == 0) continue;
                if (monPos.ContainsKey(name))
                {
                    if (monPos[name] == monument.transform.position) continue;
                    string newname = name.Remove(name.Length - 1, 1) + "1";
                    if (monPos.ContainsKey(newname))
                    {
                        newname = name.Remove(name.Length - 1, 1) + "2";
                    }
                    if (monPos.ContainsKey(newname))
                    {
                        continue;
                    }
                    name = newname;
                }

                Vector3 extents = monument.Bounds.extents;
                if (realWidth > 0f)
                {
                    extents.z = realWidth;
                }
                if (extents.z < 1)
                {
                    extents.z = 50f;
                }
                monPos.Add(name.Trim(), monument.transform.position);
                monSize.Add(name.Trim(), extents);
                Puts($"Found monument {name} at {monument.transform.position.ToString()}");
            }
        }

        #region Data
        private void LoadData()
        {
            playerAlarms = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, List<TinCanEnhanced>>>(Name + "/playerAlarms");
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name + "/playerAlarms", playerAlarms);
        }
        #endregion Data

        #region config
        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();

            configData.Version = Version;
            SaveConfig(configData);
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData
            {
                Options = new Options()
                {
                    RequirePermission = true,
                    startEnabled = false,
                    NotifyOwner = false,
                    fireDelay = 2f,
                    debug = false
                },
                Version = Version
            };

            SaveConfig(config);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }

        private class ConfigData
        {
            public Options Options;
            public VersionNumber Version;
        }

        public class Options
        {
            public bool RequirePermission;
            public bool startEnabled;
            public bool NotifyOwner;
            public bool CanDeployAtMonuments;
            public float fireDelay;
            public bool debug;
        }
        #endregion
    }
}
