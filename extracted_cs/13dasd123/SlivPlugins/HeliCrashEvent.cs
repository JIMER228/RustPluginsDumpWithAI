using Newtonsoft.Json;

using Oxide.Core.Libraries.Covalence;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("HeliCrashEvent", "Cahnu", "1.0.3")]
    [Description("This plugin was fixed by Инкуб to order [Rust Plugin Sliv]: https://discord.gg/pFgKw6Dyyq")]
    public class HeliCrashEvent : CovalencePlugin
    {
        private const string _heliPrefab = "assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab";
        //private const string _heavyScientistPrefab = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_heavy.prefab";
        //private const string _heliCratePrefab = "assets/prefabs/npc/patrol helicopter/heli_crate.prefab";
        private List<BaseHelicopter> _activehelis = new List<BaseHelicopter>();
        private List<BaseEntity> _spawnedAI = new List<BaseEntity>();
        //private bool _waitingForCratesSpawn;
        private PluginConfiguration _pluginConfiguration;

        #region Commands
        [Command("HeliCrashStart")]
        private void cmdStartHeliEvent(IPlayer player, string command)
        {
            if (!player.IsAdmin)
                return;

            RandomHeliCallLoop(true);

            ServerConsole.print("Heli Crash Event was manually started.");
        }

        [Command("HeliCrashStop")]
        private void cmdStopHeliEvent(IPlayer player, string command)
        {
            if (!player.IsAdmin)
                return;

            if (_activehelis.Count > 0)
            {
                var heli = _activehelis.First();
                heli.Kill();
                _activehelis.Remove(heli);
            }

            foreach (var npc in _spawnedAI.ToList())
            {
                npc.Kill();
                _spawnedAI.Remove(npc);
            }

            ServerConsole.print("Heli Crash Event was manually stopped.");
        }

        #endregion

        //#region Oxide Callbacks
        //void OnEntitySpawned(BaseNetworkable entity)
        //{
        //    if (!_waitingForCratesSpawn || entity.name != _heliCratePrefab)
        //        return;

        //        _waitingForCratesSpawn = false;
        //        entity.StartCoroutine(SpawnNPCs(entity));
        //}
        //#endregion

        void SaveConfig(PluginConfiguration config) => base.Config.WriteObject(config, true);

        private void Init()
        {
            _pluginConfiguration = Config.ReadObject<PluginConfiguration>();
            timer.Once(UnityEngine.Random.Range(_pluginConfiguration.Minimum * 60, _pluginConfiguration.Maximum * 60), () => RandomHeliCallLoop());
        }

        private void RandomHeliCallLoop(bool isManual = false)
        {
            if (players.Connected.Count() >= _pluginConfiguration.MinimumPlayers || isManual)
            {
                if (!isManual)
                    ServerConsole.print("Started Heli Crash Event");

                var POS = GetValidMapPOS();

                if (POS == null)
                    return;

                var heli = callHeli(POS.Value);

                _activehelis.Add(heli);

                BroadCastCrashSoon();

                if (_pluginConfiguration.HeliCrashTime >= 2)
                {
                    timer.Once(_pluginConfiguration.HeliCrashTime / 2 * 60, BroadCastCrashSoon);
                }

                timer.Once(_pluginConfiguration.HeliCrashTime * 60, StartHeliCrashRoutine);
            }
            else
            {
                ServerConsole.print("Skipping Heli Crash Event - Minimum player count not met.");
            }

            timer.Once(UnityEngine.Random.Range(_pluginConfiguration.Minimum * 60, _pluginConfiguration.Maximum * 60), () => RandomHeliCallLoop());
        }

        private void StartHeliCrashRoutine()
        {
            if (_activehelis.Count == 0)
                return;

            var heli = _activehelis.First();

            heli.StartCoroutine(DestroyHeli());
        }

        private void BroadCastCrashSoon()
        {
            if (_activehelis.Count == 0)
                return;

            server.Broadcast("<color=#b5440b>An incoming Heli is malfunctioning. It may crash somewhere on the map.</color>");
        }

        private Vector3? GetValidMapPOS()
        {
            for (int i = 0; i < 2000; i++)
            {
                var randomPOS = new Vector3(Oxide.Core.Random.Range((int)-TerrainMeta.Size.x,
                    (int)TerrainMeta.Size.x), 50, Oxide.Core.Random.Range((int)-TerrainMeta.Size.z,
                    (int)TerrainMeta.Size.z));

                randomPOS.y = TerrainMeta.HeightMap.GetHeight(randomPOS) + 50;

                if (!WaterLevel.Test(randomPOS, false, false, null))
                    return randomPOS;
            }

            return null;
        }




        private IEnumerator DestroyHeli()
        {
            if (_activehelis.Count() > 0)
            {
                var heli = _activehelis.First();

                while (IsHeliOverDeepWater(heli))
                {
                    yield return new WaitForSeconds(5);
                }

                //_waitingForCratesSpawn = true;

                server.Broadcast("<color=#b5440b>The Heli has crash landed. Get the loot before someone else gets there! </color>");
                _activehelis.Remove(heli);
                heli.Hurt(10000);
            }
        }

        private bool IsHeliOverDeepWater(BaseEntity heli)
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(new Ray(heli.transform.position, heli.transform.up * -1), out hitInfo, 1000, LayerMask.GetMask("Terrain")))
            {
                return WaterLevel.Test(hitInfo.point, false, false, null);
            }

            return true;
        }

        //private IEnumerator SpawnNPCs(BaseNetworkable locationObj)
        //{
        //    yield return new WaitForSeconds(10);

        //    var crashPos = locationObj.transform.position;

        //    for (int i = 0; i < _pluginConfiguration.HeavyScientistCount; i++)
        //    {
        //        var randomOffsetX = Random.Range(0, 10);
        //        var randomOffsetZ = Random.Range(0, 10);
        //        var finalPos = new Vector3(crashPos.x + randomOffsetX, crashPos.y, crashPos.z + randomOffsetZ);
        //        finalPos.y = TerrainMeta.HeightMap.GetHeight(finalPos);
        //        if (WaterLevel.Test(finalPos))
        //            continue;

        //        var heavy = GameManager.server.CreateEntity(_heavyScientistPrefab, finalPos, Quaternion.identity);
        //        //need to figure out how to disable navmesh errors 
        //        var npc = heavy as HumanNPC;
        //        npc.NavAgent.agentTypeID = 0;
        //        heavy.Spawn();

        //        heavy.StartCoroutine(DestroyEntity(heavy, 60));

        //        _spawnedAI.Add(heavy);
        //    }

        //}

        private BaseHelicopter callHeli(Vector3 coordinates = new Vector3(), bool forced = true)
        {
            var heli = (BaseHelicopter)GameManager.server.CreateEntity(_heliPrefab, coordinates, new Quaternion(), true);
            if (heli == null)
            {
                return null;
            }
            var heliAI = heli?.GetComponent<PatrolHelicopterAI>() ?? null;
            if (heliAI == null)
            {
                return null;
            }
            if (coordinates != Vector3.zero)
            {
                if (coordinates.y < 225) coordinates.y = 225;
                heliAI.SetInitialDestination(coordinates, 0.25f);
                heli.transform.position = heliAI.transform.position = coordinates;
                heli.SendNetworkUpdate();
            }

            heli.Spawn();
            return heli;
        }

        protected override void LoadDefaultConfig()
        {
            SaveConfig(new PluginConfiguration());
        }

        private sealed class PluginConfiguration
        {
            [JsonProperty(PropertyName = "Minimum time between events(minutes):")]
            public int Minimum { get; set; } = 60;

            [JsonProperty(PropertyName = "Maximum time between events(minutes):")]
            public int Maximum { get; set; } = 180;

            [JsonProperty(PropertyName = "How long until Heli should crash after first notification(minutes):")]
            public int HeliCrashTime { get; set; } = 2;

            [JsonProperty(PropertyName = "Minimum number of players to start event:")]
            public int MinimumPlayers { get; set; } = 4;

            //[JsonProperty(PropertyName = "How many heavy scientists should spawn at the crash site?:")]
            //public int HeavyScientistCount { get; set; } = 4;
        }
    }
}