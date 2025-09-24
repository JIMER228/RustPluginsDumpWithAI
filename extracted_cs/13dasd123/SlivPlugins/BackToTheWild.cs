using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Facepunch;
using Rust.Ai.HTN;

namespace Oxide.Plugins
{
    [Info("Back To The Wild", "Krungh Crow", "2.0.0", ResourceId = 157)]
    [Description("Animal Randomised health, damage and info")]

    class BackToTheWild : RustPlugin
    {
        #region Changelogs and ToDo
        /*******************************************************************************************
        * 
        * 1.0.4     :   Fixed console logging for chickens when set to false
        * 1.0.5     :   Added ResourceId (157)
        *               Added Global NPC vs Animal Attack true/false
        *               Added zero treshold on fleeing when targeting true/false
        * 1.0.6     :   Optimised npc and animal global ignore settings and added them to the chat command
        * 1.0.7     :   Rollback on animal behaviour like fleeing and npc targeting this is still needing a monobehaviour so this is now removed from the plugin
        * 1.1.0     :   Added support for polarbears
        * 2.0.0     :   Rewrite (config can be used from old version)
        *           :   Removed Ridable Horse
        *           :   Added Alpha Animals(Bear Polarbear Wolf)
        *           :   Added Grid locations in the console logs
        *           :   Can use console command (del bear) and (del alpha bear) instead of using the prefablink
        *           :   Can use native rust APi determing if it is a normal or Alpha animal (animal.name)
        * 
        *******************************************************************************************/
        #endregion

        [PluginReference] Plugin ChickenBow;

        #region Variables

        const ulong chaticon = 76561199090290915;
        const string Admin_Perm = "backtothewild.Admin";
        const string prefix = "<color=yellow>[Back To The Wild]</color> ";
        bool BlockSpawn;
        System.Random _Random = new System.Random();

        #endregion

        #region Configuration
        void Init()
        {
            if (!LoadConfigVariables())
            {
            Puts("Config file issue detected. Please delete file, or check syntax and fix.");
            return;
            }
            permission.RegisterPermission(Admin_Perm, this);
        }

        private ConfigData configData;

        class ConfigData
        {
            [JsonProperty(PropertyName = "Skip Huntsman when using ChickenBow Plugin")]
            public bool SkipChickenBow = true;
            [JsonProperty(PropertyName = "Console Logging settings")]
            public SettingsLogging Console = new SettingsLogging();
            [JsonProperty(PropertyName = "Population settings")]
            public SettingsPopulation PoP = new SettingsPopulation();
            [JsonProperty(PropertyName = "Bear settings")]
            public SettingsBearSpawns BearSpawns = new SettingsBearSpawns();
            [JsonProperty(PropertyName = "PolarBear settings")]
            public SettingsPBearSpawns PBearSpawns = new SettingsPBearSpawns();
            [JsonProperty(PropertyName = "Wolf settings")]
            public SettingsWolfSpawns WolfSpawns = new SettingsWolfSpawns();
            [JsonProperty(PropertyName = "Boar settings")]
            public SettingsBoarSpawns BoarSpawns = new SettingsBoarSpawns();
            [JsonProperty(PropertyName = "Stag settings")]
            public SettingsStagSpawns StagSpawns = new SettingsStagSpawns();
            [JsonProperty(PropertyName = "Horse settings")]
            public SettingsHorseSpawns HorseSpawns = new SettingsHorseSpawns();
            [JsonProperty(PropertyName = "Chicken settings")]
            public SettingsChickenSpawns ChickenSpawns = new SettingsChickenSpawns();
        }

        class SettingsLogging
        {
            [JsonProperty(PropertyName = "Show Bear spawns in Console")]
            public bool ConBear = false;
            [JsonProperty(PropertyName = "Show PolarBear spawns in Console")]
            public bool ConPBear = false;
            [JsonProperty(PropertyName = "Show Wolf spawns in Console")]
            public bool ConWolf = false;
             [JsonProperty(PropertyName = "Show Boar spawns in Console")]
            public bool ConBoar = false;
             [JsonProperty(PropertyName = "Show Stag spawns in Console")]
            public bool ConStag = false;
             [JsonProperty(PropertyName = "Show Horse spawns in Console")]
            public bool ConHorse = false;
            [JsonProperty(PropertyName = "Show Chicken spawns in Console")]
            public bool ConChicken = false;
        }
        class SettingsAlpha
        {
            [JsonProperty(PropertyName = "Can spawn as alpha")]
            public bool CanBeAlpha = false;
            [JsonProperty(PropertyName = "Spawnrate (0-100)")]
            public int AlphaChance = 10;
            [JsonProperty(PropertyName = "Health Multiplier")]
            public float AlphaMulti = 3.0f;
            [JsonProperty(PropertyName = "Strength Multiplier(Att dmg)")]
            public float AlphaDamageMulti = 1.5f;
            [JsonProperty(PropertyName = "Speed Multiplier")]
            public float AlphaSpeed = 1.0f;
        }

        class SettingsPopulation
        {
            [JsonProperty(PropertyName = "Set population variables ?")]
            public bool PopUse = false;
            [JsonProperty(PropertyName = "Bear population")]
            public float BearPoP = 2f;
            [JsonProperty(PropertyName = "PolarBear population")]
            public float PBearPoP = 1f;
            [JsonProperty(PropertyName = "Wolf population")]
            public float WolfPoP = 2f;
            [JsonProperty(PropertyName = "Boar population")]
            public float BoarPoP = 5f;
            [JsonProperty(PropertyName = "Stag population")]
            public float StagPoP = 3f;
            [JsonProperty(PropertyName = "Horse population")]
            public float HorsePoP = 0f;
            [JsonProperty(PropertyName = "Ridable Horse population")]
            public float RHorsePoP = 4f;
            [JsonProperty(PropertyName = "Chicken population")]
            public float ChickenPoP = 3f;
        }

        class SettingsBearSpawns
        {
            [JsonProperty(PropertyName = "Change Bear stats on spawns")]
            public bool BearUse = true;
            [JsonProperty(PropertyName = "Minimum Health")]
            public int BearHealthmin = 400;
            [JsonProperty(PropertyName = "Maximum Health")]
            public int BearHealthmax = 1000;
            [JsonProperty(PropertyName = "Minimum Strength (Att dmg)")]
            public int BearDamage = 20;
            [JsonProperty(PropertyName = "Maximum Strength (Att dmg")]
            public int BearDamageMax = 40;
            [JsonProperty(PropertyName = "Running Speed")]
            public float BearSpeed = 6f;
            [JsonProperty(PropertyName = "Alpha")]
            public SettingsAlpha Alpha = new SettingsAlpha();
        }

        class SettingsPBearSpawns
        {
            [JsonProperty(PropertyName = "Change PolarBear stats on spawns")]
            public bool BearUse = true;
            [JsonProperty(PropertyName = "Minimum Health")]
            public int BearHealthmin = 400;
            [JsonProperty(PropertyName = "Maximum Health")]
            public int BearHealthmax = 1200;
            [JsonProperty(PropertyName = "Minimum Strength (Att dmg)")]
            public int BearDamage = 22;
            [JsonProperty(PropertyName = "Maximum Strength (Att dmg")]
            public int BearDamageMax = 44;
            [JsonProperty(PropertyName = "Running Speed")]
            public float BearSpeed = 6f;
            [JsonProperty(PropertyName = "Alpha")]
            public SettingsAlpha Alpha = new SettingsAlpha();
        }

        class SettingsWolfSpawns
        {
            [JsonProperty(PropertyName = "Change Wolf stats on spawns")]
            public bool WolfUse = true;
            [JsonProperty(PropertyName = "Minimum Health")]
            public int WolfHealthmin = 150;
            [JsonProperty(PropertyName = "Maximum Health")]
            public int WolfHealthmax = 300;
            [JsonProperty(PropertyName = "Minimum Strength (Att dmg)")]
            public int WolfDamage = 15;
            [JsonProperty(PropertyName = "Maximum Strength (Att dmg)")]
            public int WolfDamageMax = 25;
            [JsonProperty(PropertyName = "Running Speed")]
            public float WolfSpeed = 6f;
            [JsonProperty(PropertyName = "Alpha")]
            public SettingsAlpha Alpha = new SettingsAlpha();
        }

        class SettingsBoarSpawns
        {
            [JsonProperty(PropertyName = "Change Boar stats on spawns")]
            public bool BoarUse = true;
            [JsonProperty(PropertyName = "Minimum Health")]
            public int BoarHealthmin = 150;
            [JsonProperty(PropertyName = "Maximum Health")]
            public int BoarHealthmax = 450;
            [JsonProperty(PropertyName = "Minimum Strength (Att dmg)")]
            public int BoarDamage = 15;
            [JsonProperty(PropertyName = "Maximum Strength (Att dmg)")]
            public int BoarDamageMax = 25;
            [JsonProperty(PropertyName = "Running Speed")]
            public float BoarSpeed = 6f;
        }

        class SettingsStagSpawns
        {
            [JsonProperty(PropertyName = "Change Stag stats on spawns")]
            public bool StagUse = true;
            [JsonProperty(PropertyName = "Minimum Health")]
            public int StagHealthmin = 150;
            [JsonProperty(PropertyName = "Maximum Health")]
            public int StagHealthmax = 425;
            [JsonProperty(PropertyName = "Minimum Strength (Att dmg)")]
            public int StagDamage = 15;
            [JsonProperty(PropertyName = "Maximum Strength (Att dmg)")]
            public int StagDamageMax = 25;
            [JsonProperty(PropertyName = "Running Speed")]
            public float StagSpeed = 10f;
        }

        class SettingsHorseSpawns
        {
            [JsonProperty(PropertyName = "Change Horse stats on spawns")]
            public bool HorseUse = true;
            [JsonProperty(PropertyName = "Minimum Health")]
            public int HorseHealthmin = 150;
            [JsonProperty(PropertyName = "Maximum Health")]
            public int HorseHealthmax = 600;
            [JsonProperty(PropertyName = "Minimum Strength (Att dmg)")]
            public int HorseDamage = 15;
            [JsonProperty(PropertyName = "Maximum Strength (Att dmg)")]
            public int HorseDamageMax = 25;
            [JsonProperty(PropertyName = "Running Speed")]
            public float HorseSpeed = 10f;
        }

        class SettingsChickenSpawns
        {
            [JsonProperty(PropertyName = "Change Chicken stats on spawns")]
            public bool ChickenUse = true;
            [JsonProperty(PropertyName = "Minimum Health")]
            public int ChickenHealthmin = 25;
            [JsonProperty(PropertyName = "Maximum Health")]
            public int ChickenHealthmax = 100;
            [JsonProperty(PropertyName = "Minimum Strength (Att dmg)")]
            public int ChickenDamage = 1;
            [JsonProperty(PropertyName = "Maximum Strength (Att dmg)")]
            public int ChickenDamageMax = 5;
            [JsonProperty(PropertyName = "Running Speed")]
            public float ChickenSpeed = 10f;
        }

        private bool LoadConfigVariables()
        {
            try
            {
                configData = Config.ReadObject<ConfigData>();
            }
            catch
            {
                return false;
            }
            
            SaveConf();
            return true;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Fresh install detected Creating a new config file.");
            configData = new ConfigData();
            SaveConf();
        }

        void SaveConf() => Config.WriteObject(configData, true);
        #endregion

        #region LanguageAPI
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["InvalidInput"] = "<color=red>Please enter a valid command!</color>",
                ["Version"] = "\nVersion : V",
                ["Info"] = "\n<color=green>List of current Population(KM2)/Health min/max settings\nAnd counts how many are on the map</color>",
                ["NoPermission"] = "<color=red>You do not have permission to use that command!</color>",
            }, this);
        }

        #endregion

        #region Commands

        [ChatCommand("bttw")]
        private void cmdbttw(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, Admin_Perm))
            {
            Player.Message(player,prefix + string.Format(msg("NoPermission", player.UserIDString)), chaticon);
            return;
            }

            if (args.Length == 0)
            {
            Player.Message(player,prefix + string.Format(msg("InvalidInput", player.UserIDString)), chaticon);
            return;
            }

            {
            if (args[0].ToLower() == "animals")
            {
                Player.Message(player,prefix + string.Format(msg("Version", player.UserIDString)) + this.Version.ToString() + " By : " + this.Author.ToString()
                + msg("Info")
                + msg("\n\n")
                //Bear Info
                + msg("<color=orange>Bear</color> : Pop <color=purple>") + Bear.Population.ToString() + msg("</color> ")
                + msg("Health <color=purple>") + configData.BearSpawns.BearHealthmin.ToString() + msg("</color>/<color=purple>") + configData.BearSpawns.BearHealthmax.ToString() + msg("</color>")
                + msg(" Alive <color=purple>") + BaseNetworkable.serverEntities.OfType<Bear>().Count().ToString() + msg("</color> ")
                //PolarBear Info
                + msg("\n<color=orange>PolarBear</color> : Pop <color=purple>") + Polarbear.Population.ToString() + msg("</color> ")
                + msg("Health <color=purple>") + configData.PBearSpawns.BearHealthmin.ToString() + msg("</color>/<color=purple>") + configData.PBearSpawns.BearHealthmax.ToString() + msg("</color>")
                + msg(" Alive <color=purple>") + BaseNetworkable.serverEntities.OfType<Polarbear>().Count().ToString() + msg("</color> ")
                //Wolf Info
                + msg("\n<color=orange>Wolf</color> : Pop <color=purple>") + Wolf.Population.ToString() + msg("</color> ")
                + msg("Health <color=purple>") + configData.WolfSpawns.WolfHealthmin.ToString() + msg("</color>/<color=purple>") + configData.WolfSpawns.WolfHealthmax.ToString() + msg("</color>")
                + msg(" Alive <color=purple>") + BaseNetworkable.serverEntities.OfType<Wolf>().Count().ToString() + msg("</color> ")
                //Boar Info
                + msg("\n<color=orange>Boar</color> : Pop <color=purple>") + Boar.Population.ToString() + msg("</color> ")
                + msg("Health <color=purple>") + configData.BoarSpawns.BoarHealthmin.ToString() + msg("</color>/<color=purple>") + configData.BoarSpawns.BoarHealthmax.ToString() + msg("</color>")
                + msg(" Alive <color=purple>") + BaseNetworkable.serverEntities.OfType<Boar>().Count().ToString() + msg("</color> ")
                //Stag info
                + msg("\n<color=orange>Stag</color> : Pop <color=purple>") + Stag.Population.ToString() + msg("</color> ")
                + msg("Health <color=purple>") + configData.StagSpawns.StagHealthmin.ToString() + msg("</color>/<color=purple>") + configData.StagSpawns.StagHealthmax.ToString() + msg("</color>")
                + msg(" Alive <color=purple>") + BaseNetworkable.serverEntities.OfType<Stag>().Count().ToString() + msg("</color> ")
                //Horse Info
                + msg("\n<color=orange>Horse</color> : Pop <color=purple>") + Horse.Population.ToString() + msg("</color> ")
                + msg("Health <color=purple>") + configData.HorseSpawns.HorseHealthmin.ToString() + msg("</color>/<color=purple>") + configData.HorseSpawns.HorseHealthmax.ToString() + msg("</color>")
                + msg(" Alive <color=purple>") + BaseNetworkable.serverEntities.OfType<Horse>().Count().ToString() + msg("</color> ")
                //Ridable Horse info
                + msg("\n<color=orange>RidableHorse</color> : Pop <color=purple>") + RidableHorse.Population.ToString() + msg("</color> ")
                + msg(" Alive <color=purple>") + BaseNetworkable.serverEntities.OfType<RidableHorse>().Count().ToString() + msg("</color> ")
                //Chicken Info
                + msg("\n<color=orange>Chicken</color> : Pop <color=purple>") + Chicken.Population.ToString() + msg("</color> ")
                + msg("Health <color=purple>") + configData.ChickenSpawns.ChickenHealthmin.ToString() + msg("</color>/<color=purple>") + configData.ChickenSpawns.ChickenHealthmax.ToString() + msg("</color>")
                + msg(" Alive <color=purple>") + BaseNetworkable.serverEntities.OfType<Chicken>().Count().ToString() + msg("</color> ")
                , chaticon);
                }
            }
        }
        #endregion

        #region Helpers

        private string msg(string key, string id = null) => lang.GetMessage(key, this, id);

        private static string GetGrid(Vector3 position) => PhoneController.PositionToGridCoord(position);

        private bool SpawnRate(int AnimalRate)
        {
            if (_Random.Next(1, 101) <= AnimalRate)
            {
                return true;
            }
            return false;
        }

        #endregion

        #region Oxide Hooks

        void OnServerInitialized()
        {
            if (configData.PoP.PopUse)//Executes the population settings if True in CFG and prints to console
            {
                Puts(" Changing the following server population settings :\n");
                Server.Command("bear.population", configData.PoP.BearPoP);
                Server.Command("polarbear.population", configData.PoP.PBearPoP);
                Server.Command("wolf.population", configData.PoP.WolfPoP);
                Server.Command("boar.population", configData.PoP.BoarPoP);
                Server.Command("stag.population", configData.PoP.StagPoP);
                Server.Command("horse.population", configData.PoP.HorsePoP);
                Server.Command("ridablehorse.population", configData.PoP.RHorsePoP);
                Server.Command("chicken.population", configData.PoP.ChickenPoP);
            }
        }

        void OnEntityDeath(BaseAnimalNPC animal, HitInfo info)
        {
            if (animal == null) return;
            BasePlayer attacker = info.InitiatorPlayer;

            if (attacker != null && animal.name.Contains("Alpha"))
            {
                Puts($"The animal was a {animal.name}");
            }
            return;
        }

        void OnEntitySpawned(BaseAnimalNPC animal)
        {
            if (animal == null) return;
            var RandomHealth = animal.health;
            var MinHealth = animal.health;
            var MAxHealth = animal.health;
            var MinDamage = animal.AttackDamage;
            var MaxDamage = animal.AttackDamage;
            var RandomDamage = animal.AttackDamage;
            var Speed = animal.Stats.Speed;
            var SpawnLoc = GetGrid(animal.transform.position);
            bool ShowConsole = false;

            if (animal is Bear && configData.BearSpawns.BearUse)
            {
                MinHealth = configData.BearSpawns.BearHealthmin;
                MAxHealth = configData.BearSpawns.BearHealthmax;
                MinDamage = configData.BearSpawns.BearDamage;
                MaxDamage = configData.BearSpawns.BearDamageMax;
                Speed = configData.BearSpawns.BearSpeed;
                ShowConsole = configData.Console.ConBear;
                animal.name = "Bear";
                if (configData.BearSpawns.Alpha.CanBeAlpha == true)
                {
                    if (SpawnRate(configData.BearSpawns.Alpha.AlphaChance) == true)
                    {
                        var _AlphaMulti = configData.BearSpawns.Alpha.AlphaMulti;//hp
                        var _AlphaDamage = configData.BearSpawns.Alpha.AlphaDamageMulti;//strenghth
                        animal.name = "Alpha Bear";
                        MinHealth = MinHealth * _AlphaMulti;
                        MAxHealth = MAxHealth * _AlphaMulti;
                        MinDamage = MinDamage * _AlphaDamage;
                        MaxDamage = MaxDamage * _AlphaDamage;
                        Speed = Speed * configData.BearSpawns.Alpha.AlphaSpeed;
                    }
                }
            }
            if (animal is Polarbear && configData.PBearSpawns.BearUse)
            {
                MinHealth = configData.PBearSpawns.BearHealthmin;
                MAxHealth = configData.PBearSpawns.BearHealthmax;
                MinDamage = configData.PBearSpawns.BearDamage;
                MaxDamage = configData.PBearSpawns.BearDamageMax;
                Speed = configData.PBearSpawns.BearSpeed;
                animal.name = "Polarbear";
                ShowConsole = configData.Console.ConPBear;
                if (configData.WolfSpawns.Alpha.CanBeAlpha == true)
                {
                    if (SpawnRate(configData.WolfSpawns.Alpha.AlphaChance) == true)
                    {
                        var _AlphaMulti = configData.PBearSpawns.Alpha.AlphaMulti;//hp
                        var _AlphaDamage = configData.PBearSpawns.Alpha.AlphaDamageMulti;//strenghth
                        animal.name = "Alpha Polarbear";
                        MinHealth = MinHealth * _AlphaMulti;
                        MAxHealth = MAxHealth * _AlphaMulti;
                        MinDamage = MinDamage * _AlphaDamage;
                        MaxDamage = MaxDamage * _AlphaDamage;
                        Speed = Speed * configData.PBearSpawns.Alpha.AlphaSpeed;
                    }
                }
            }
            if (animal is Chicken && configData.ChickenSpawns.ChickenUse)
            {
                BlockSpawn = false;
                ShowConsole = configData.Console.ConChicken;

                if (ChickenBow != null && ChickenBow.Call<bool>("IsSpawnedChicken", animal.net.ID) == true) BlockSpawn = true;

                if (BlockSpawn)
                {
                    if (ShowConsole) Puts($"ChickenBow Chicken spawned skipping Stat changes");
                    return;
                }
                MinHealth = configData.ChickenSpawns.ChickenHealthmin;
                MAxHealth = configData.ChickenSpawns.ChickenHealthmax;
                MinDamage = configData.ChickenSpawns.ChickenDamage;
                MaxDamage = configData.ChickenSpawns.ChickenDamageMax;
                Speed = configData.ChickenSpawns.ChickenSpeed;
                animal.name = "Chicken";
            }
            if (animal is Boar && configData.BoarSpawns.BoarUse)
            {
                MinHealth = configData.BoarSpawns.BoarHealthmin;
                MAxHealth = configData.BoarSpawns.BoarHealthmax;
                MinDamage = configData.BoarSpawns.BoarDamage;
                MaxDamage = configData.BoarSpawns.BoarDamageMax;
                Speed = configData.BoarSpawns.BoarSpeed;
                animal.name = "Boar";
                ShowConsole = configData.Console.ConBoar;
            }
            if (animal is Horse && configData.HorseSpawns.HorseUse)
            {
                MinHealth = configData.HorseSpawns.HorseHealthmin;
                MAxHealth = configData.HorseSpawns.HorseHealthmax;
                MinDamage = configData.HorseSpawns.HorseDamage;
                MaxDamage = configData.HorseSpawns.HorseDamageMax;
                Speed = configData.HorseSpawns.HorseSpeed;
                animal.name = "Horse";
                ShowConsole = configData.Console.ConHorse;
            }
            if (animal is Stag && configData.StagSpawns.StagUse)
            {
                MinHealth = configData.StagSpawns.StagHealthmin;
                MAxHealth = configData.StagSpawns.StagHealthmax;
                MinDamage = configData.StagSpawns.StagDamage;
                MaxDamage = configData.StagSpawns.StagDamageMax;
                Speed = configData.StagSpawns.StagSpeed;
                animal.name = "Stag";
                ShowConsole = configData.Console.ConStag;
            }
            if (animal is Wolf && configData.WolfSpawns.WolfUse)
            {
                MinHealth = configData.WolfSpawns.WolfHealthmin;
                MAxHealth = configData.WolfSpawns.WolfHealthmax;
                MinDamage = configData.WolfSpawns.WolfDamage;
                MaxDamage = configData.WolfSpawns.WolfDamageMax;
                Speed = configData.WolfSpawns.WolfSpeed;
                animal.name = "Wolf";
                ShowConsole = configData.Console.ConWolf;
                if (configData.WolfSpawns.Alpha.CanBeAlpha == true)
                {
                    if (SpawnRate(configData.WolfSpawns.Alpha.AlphaChance) == true)
                    {
                        var _AlphaMulti = configData.WolfSpawns.Alpha.AlphaMulti;//hp
                        var _AlphaDamage = configData.WolfSpawns.Alpha.AlphaDamageMulti;//strenghth
                        animal.name = "Alpha Wolf";
                        MinHealth = MinHealth * _AlphaMulti;
                        MAxHealth = MAxHealth * _AlphaMulti;
                        MinDamage = MinDamage * _AlphaDamage;
                        MaxDamage = MaxDamage * _AlphaDamage;
                        Speed = Speed * configData.WolfSpawns.Alpha.AlphaSpeed;
                    }
                }
            }

            RandomHealth = UnityEngine.Random.Range(MinHealth, MAxHealth);
            RandomDamage = UnityEngine.Random.Range(MinDamage, MaxDamage);

            animal.InitializeHealth(RandomHealth, RandomHealth);
            animal.lifestate = BaseCombatEntity.LifeState.Alive;
            animal.AttackDamage = RandomDamage;
            animal.Stats.Speed = Speed;
            animal.Stats.TurnSpeed = Speed;
            animal.NewAI = true;
            if (ShowConsole || animal.name.Contains("Alpha")) Puts($"{animal.name}[{animal.net.ID}] spawned with {(int)RandomHealth} HP and {(int)RandomDamage} Strength in Grid ({SpawnLoc})");
        }

        #endregion
    }
}