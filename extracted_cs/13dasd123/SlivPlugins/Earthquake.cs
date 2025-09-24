// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Earthquake", "k1lly0u", "0.1.4")]
    [Description("Create island wide earthquakes with the option to damage players and structures")]
    class Earthquake : RustPlugin
    {
        #region Fields
        private Quake quakeObject;
        private static Earthquake ins;

        private string[] shakeEffects = new string[]
        {
            "assets/bundled/prefabs/fx/screen_land.prefab",
            "assets/bundled/prefabs/fx/screen_jump.prefab",
            "assets/bundled/prefabs/fx/takedamage_hit.prefab",
            "assets/bundled/prefabs/fx/takedamage_generic.prefab"
        };

        private string[] soundEffects = new string[]
        {
            "assets/bundled/prefabs/fx/bucket_drop_debris.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/concrete/concrete1.prefab",
            "assets/bundled/prefabs/fx/building/stone_gib.prefab",
            "assets/bundled/prefabs/fx/dig_effect.prefab",
            "assets/bundled/prefabs/fx/entities/pumpkin/gib.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/cloth/cloth1.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/grass/grass1.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/rock/slash_rock_01.prefab",
            "assets/bundled/prefabs/fx/impacts/blunt/sand/sand.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/cloth/jump-land-cloth.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/concrete/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/dirt/jump-land-dirt.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/forest/jump-land-forest.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/generic/jump-land-concrete.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/grass/jump-land-grass.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/gravel/jump-land-gravel.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/metal/jump-land-metal.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/metalore/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/rock/jump-land-rock.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/sand/jump-land-sand.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/snow/jump-land-snow.prefab",
            "assets/bundled/prefabs/fx/impacts/jump-land/barefoot/stones/jump-land-stones.prefab"
        };

        #endregion

        #region Oxide Hooks
        private void OnServerInitialized()
        {
            ins = this;
            permission.RegisterPermission("earthquake.run", this);

            if (configData.AutoEvents.Enabled)
                timer.In(GetRandomTime(), RunRandomEvent);
        }
                
        private void Unload()
        {
            if (quakeObject != null)
            {
                UnityEngine.Object.Destroy(quakeObject);
            }
            ins = null;
        }
        #endregion

        #region Functions
        private void RunRandomEvent()
        {
            if (configData.AutoEvents.Types.Length == 0)
                return;

            string intensity = configData.AutoEvents.Types.GetRandom();

            if (configData.Settings.ContainsKey(intensity))
            {
                if (quakeObject == null)
                {
                    quakeObject = new GameObject().AddComponent<Quake>();
                    quakeObject.InitializeQuake(configData.Settings[intensity]);
                }
            }
            else PrintError($"There was a error running a random EarthQuake event. The specified event name '{intensity}' does not exist...");        

            timer.In(GetRandomTime(), RunRandomEvent);
        }

        private int GetRandomTime() => UnityEngine.Random.Range(configData.AutoEvents.Minimum, configData.AutoEvents.Maximum) * 60;
        #endregion

        #region Commands
        [ChatCommand("quake")]
        private void cmdEarthquake(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "earthquake.run"))
                return;

            if (args.Length == 0)
            {
                SendReply(player, $"<color=#ce422b>/quake <name></color><color=#939393> - Start a earthquake using the predefined settings with the specified name in the config\nAvailable types: </color><color=#ce422b>{configData.Settings.Keys.ToSentence()}</color>");
                SendReply(player, "<color=#ce422b>/quake cancel</color><color=#939393> - Cancel a earthquake in progress</color>");
                return;
            }

            if (args[0].ToLower() == "cancel")
            {
                if (quakeObject == null)
                {
                    SendReply(player, "<color=#939393>There is no earthquake in progress</color>");
                    return;
                }

                UnityEngine.Object.Destroy(quakeObject);
                SendReply(player, "<color=#ce422b>Earthquake cancelled</color>");
                return;
            }

            if (quakeObject != null)
            {
                SendReply(player, "<color=#939393>A earthquake is already in progress</color>");
                return;
            }

            if (!configData.Settings.ContainsKey(args[0]))
            {
                SendReply(player, $"<color=#939393>There is no quake settings with that name.\nAvailable types: </color><color=#ce422b>{configData.Settings.Keys.ToSentence()}</color>");
                return;
            }

            quakeObject = new GameObject().AddComponent<Quake>();
            quakeObject.InitializeQuake(configData.Settings[args[0]]);
            SendReply(player, $"<color=#939393>You have started a <color=#ce422b>{args[0]}</color> earthquake</color>");
        }

        [ConsoleCommand("quake")]
        private void ccmdEarthquake(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                if (!permission.UserHasPermission(arg.Connection.userid.ToString(), "earthquake.run"))
                    return;
            }

            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "quake <name> - Start a earthquake using the predefined settings with the name specified in the config");
                SendReply(arg, "quake cancel - Cancel a earthquake in progress");
                return;
            }

            if (arg.Args[0].ToLower() == "cancel")
            {
                if (quakeObject == null)
                {
                    SendReply(arg, "There is no earthquake in progress");
                    return;
                }

                UnityEngine.Object.Destroy(quakeObject);
                SendReply(arg, "Earthquake cancelled");
                return;
            }

            if (quakeObject != null)
            {
                SendReply(arg, "A earthquake is already in progress");
                return;
            }

            if (!configData.Settings.ContainsKey(arg.Args[0]))
            {
                SendReply(arg, $"There is no quake settings with that name.\nAvailable types: {configData.Settings.Keys.ToSentence()}");
                return;
            }

            quakeObject = new GameObject().AddComponent<Quake>();
            quakeObject.InitializeQuake(configData.Settings[arg.Args[0]]);
            SendReply(arg, $"You have started a {arg.Args[0]} earthquake");
        }
        #endregion

        #region Quake Manager
        private class Quake : MonoBehaviour
        {
            private ConfigData.QuakeSettings settings;
            private List<BuildingBlock> buildings = new List<BuildingBlock>();
            private float timeTaken;

            private int blockNumber = 0;
            private float nextDamageTime;

            private void OnDestroy()
            {
                if (InvokeHandler.IsInvoking(this, RunDamage))
                    InvokeHandler.CancelInvoke(this, RunDamage);

                if (InvokeHandler.IsInvoking(this, RunQuake))
                    InvokeHandler.CancelInvoke(this, RunQuake);

                if (ins.configData.DisplayFinishMessage && !string.IsNullOrEmpty(ins.configData.FinishMessage))
                    ins.PrintToChat(ins.configData.FinishMessage);

                Interface.CallHook("OnEarthquakeFinished");

            }

            private void Update()
            {
                float time = Time.realtimeSinceStartup;
                if (time < nextDamageTime)
                    return;

                if (buildings.Count == 0 || blockNumber > buildings.Count - 1)
                {
                    PopulateBlockList();
                    return;
                }

                BuildingBlock block = buildings[blockNumber];
                block.health = block.health - (block.health * (settings.StructureDamage / 100));
                block.SendNetworkUpdate();
                blockNumber++;

                nextDamageTime = time + 0.1f;
            }

            public void InitializeQuake(ConfigData.QuakeSettings settings)
            {
                this.settings = settings;

                if (settings.StructureDamageChance > 0 && settings.StructureDamage > 0)
                    PopulateBlockList();
                else enabled = false;

                InvokeHandler.InvokeRepeating(this, RunQuake, 0, 0.2f);

                if (settings.PlayerDamageChance > 0 || settings.StructureDamageChance > 0)
                    InvokeHandler.InvokeRepeating(this, RunDamage, 0, 1f);

                if (ins.configData.DisplayStartMessage && !string.IsNullOrEmpty(ins.configData.StartMessage))
                    ins.PrintToChat(ins.configData.StartMessage);

                Interface.CallHook("OnEarthquakeStarted");
            }

            private void RunQuake()
            {
                if (timeTaken > settings.Duration)
                {
                    InvokeHandler.CancelInvoke(this, RunQuake);
                    Destroy(this);
                    return;
                }

                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    Effect.server.Run(ins.shakeEffects.GetRandom(), player.transform.position);
                    Effect.server.Run(ins.soundEffects.GetRandom(), player.transform.position + Vector3.down * 5);                    
                }                

                timeTaken += 0.2f;
            }

            private void RunDamage()
            {
                if (settings.PlayerDamageChance > 0)
                {
                    foreach (BasePlayer player in BasePlayer.activePlayerList)
                    {
                        if (settings.PlayerDamageChance > UnityEngine.Random.Range(1, 99))
                            player.Hurt(player.health * (settings.PlayerDamage / 100), Rust.DamageType.Blunt);
                    }
                }                
            }  
           
            private void PopulateBlockList()
            {                
                buildings.Clear();
                blockNumber = 0;
                foreach (BuildingBlock block in BaseEntity.saveList.Where(x => x is BuildingBlock).Cast<BuildingBlock>())
                {
                    if (settings.StructureDamageChance > UnityEngine.Random.Range(1, 99))
                        buildings.Add(block);
                }
                
                nextDamageTime = UnityEngine.Time.realtimeSinceStartup;
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Earthquake Presets")]
            public Dictionary<string, QuakeSettings> Settings { get; set; }

            [JsonProperty(PropertyName = "Broadcast message when earthquake begins")]
            public bool DisplayStartMessage { get; set; }

            [JsonProperty(PropertyName = "Broadcast message when earthquake finishes")]
            public bool DisplayFinishMessage { get; set; }

            [JsonProperty(PropertyName = "Earthquake begin message")]
            public string StartMessage { get; set; }

            [JsonProperty(PropertyName = "Earthquake finish message")]
            public string FinishMessage { get; set; }

            [JsonProperty(PropertyName = "Event Automation")]
            public Automation AutoEvents { get; set; }

            public class Automation
            {
                [JsonProperty(PropertyName = "Automate earthquake events")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Minimum amount of time between events (minutes)")]
                public int Minimum { get; set; }

                [JsonProperty(PropertyName = "Maximum amount of time between events (minutes)")]
                public int Maximum { get; set; }

                [JsonProperty(PropertyName = "Presets to choose from (chosen at random)")]
                public string[] Types { get; set; }
            }
            

            public class QuakeSettings
            {
                [JsonProperty(PropertyName = "Amount of damage to deal to players (percentage of current health)")]
                public float PlayerDamage { get; set; }

                [JsonProperty(PropertyName = "Chance of players being damaged (0 - 100)")]
                public float PlayerDamageChance { get; set; }

                [JsonProperty(PropertyName = "Amount of damage to deal to structures (percentage of current health)")]
                public float StructureDamage { get; set; }

                [JsonProperty(PropertyName = "Chance of structures being damaged (0 - 100)")]
                public float StructureDamageChance { get; set; }

                [JsonProperty(PropertyName = "Duration of earthquake (seconds)")]
                public float Duration { get; set; }
            }
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                DisplayFinishMessage = true,
                DisplayStartMessage = true,
                StartMessage = "<color=#ce422b>A earthquake has been detected on the island!</color>",
                FinishMessage = "<color=#ce422b>The earthquake has subsided!</color>",
                Settings = new Dictionary<string, ConfigData.QuakeSettings>()
                {
                    ["Mild"] = new ConfigData.QuakeSettings
                    {
                        Duration = 30,
                        PlayerDamage = 0,
                        PlayerDamageChance = 0,
                        StructureDamage = 0,
                        StructureDamageChance = 0
                    },
                    ["Medium"] = new ConfigData.QuakeSettings
                    {
                        Duration = 60,
                        PlayerDamage = 2,
                        PlayerDamageChance = 5,
                        StructureDamage = 0,
                        StructureDamageChance = 0
                    },
                    ["Intense"] = new ConfigData.QuakeSettings
                    {
                        Duration = 120,
                        PlayerDamage = 5,
                        PlayerDamageChance = 10,
                        StructureDamage = 20,
                        StructureDamageChance = 20
                    }
                },
                AutoEvents = new ConfigData.Automation
                {
                    Enabled = false,
                    Minimum = 45,
                    Maximum = 75,
                    Types = new string[] { "Mild", "Medium" }
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(0, 1, 3))
                configData.AutoEvents = baseConfig.AutoEvents;

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion
    }
}
