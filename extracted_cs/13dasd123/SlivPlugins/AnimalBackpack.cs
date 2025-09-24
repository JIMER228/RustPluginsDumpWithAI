using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using Oxide.Core.Libraries.Covalence;


namespace Oxide.Plugins
{
    [Info("AnimalBackpack", "EcoSmile", "1.2.4")]
    [Description("This plugin was fixed by Инкуб to order [Rust Plugin Sliv]: https://discord.gg/pFgKw6Dyyq")]
    class AnimalBackpack : RustPlugin
    {
        static AnimalBackpack ins;
        PluginConfig config;
        public class PluginConfig
        {
            [JsonProperty("Включить PVE режим для животных?")]
            public bool isPVE;
            [JsonProperty("Настройка животных")]
            public Dictionary<Animal, PrivelageSetting> animalsetting;
        }
        [JsonConverter(typeof(StringEnumConverter))]
        public enum Animal
        {
            Wolf, Boar, Horse, Bear, RiderHorse
        }
        public class PrivelageSetting
        {
            [JsonProperty("Разрешить лутать контейнер только хозяину?")]
            public bool OnlyOwner;
            [JsonProperty("Разрешить лутать контейнер тимейтам хозяина?")]
            public bool TeamateAcces;
            [JsonProperty("Количество доступных слотов в ОДНОМ рюкзаке (На медведе их 2)")]
            public int slots;
            [JsonProperty("Количество ХП")]
            public float maxhealt;
            [JsonProperty("Скорость ходьбы животного")]
            public float speedvalue;
            [JsonProperty("Скорость бега животного")]
            public float speedrunvalue;
            [JsonProperty("Урон животного")]
            public float damagevalue;
            [JsonProperty("Телепортировать животное к хозяину, если хозяин убежал слишком далеко?")]
            public bool AnimalTp;
            [JsonProperty("Максимальное расстояние после которого животное будет телепортировано к игроку.")]
            public float maxdistance;
        }
        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig
            {
                isPVE = false,
                animalsetting = new Dictionary<Animal, PrivelageSetting>()
                {
                    [Animal.Wolf] = new PrivelageSetting()
                    {
                        OnlyOwner = false,
                        TeamateAcces = false,
                        slots = 12,
                        maxhealt = 150,
                        speedvalue = 4f,
                        speedrunvalue = 9f,
                        damagevalue = 13f,
                        AnimalTp = true,
                        maxdistance = 100
                    },
                    [Animal.Boar] = new PrivelageSetting()
                    {
                        OnlyOwner = false,
                        TeamateAcces = false,
                        slots = 18,
                        maxhealt = 300,
                        speedvalue = 4f,
                        speedrunvalue = 7f,
                        damagevalue = 20f,
                        AnimalTp = true,
                        maxdistance = 100
                    },
                    [Animal.Horse] = new PrivelageSetting()
                    {
                        OnlyOwner = false,
                        TeamateAcces = false,
                        slots = 24,
                        maxhealt = 150,
                        speedvalue = 4f,
                        speedrunvalue = 12f,
                        damagevalue = 10f,
                        AnimalTp = true,
                        maxdistance = 100
                    },
                    [Animal.Bear] = new PrivelageSetting()
                    {
                        OnlyOwner = false,
                        TeamateAcces = false,
                        slots = 30,
                        maxhealt = 1000,
                        speedvalue = 4f,
                        speedrunvalue = 8f,
                        damagevalue = 40f,
                        AnimalTp = true,
                        maxdistance = 100
                    },
                    [Animal.RiderHorse] = new PrivelageSetting()
                    {
                        OnlyOwner = false,
                        TeamateAcces = false,
                        slots = 30,
                        maxhealt = 1000,
                        speedvalue = 0,
                        speedrunvalue = 0,
                        damagevalue = 0,
                        AnimalTp = false,
                        maxdistance = 0
                    }
                }
            }
            ;
        }
        public static Dictionary<string, Animal> prefab = new Dictionary<string, Animal>()
        {
            ["assets/rust.ai/agents/wolf/wolf.prefab"] = Animal.Wolf,
            ["assets/rust.ai/agents/boar/boar.prefab"] = Animal.Boar,
            ["assets/rust.ai/agents/horse/horse.prefab"] = Animal.Horse,
            ["assets/rust.ai/agents/bear/bear.prefab"] = Animal.Bear,
            ["assets/rust.ai/nextai/testridablehorse.prefab"] = Animal.RiderHorse
        }
        ;
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        public Dictionary<ulong, ulong> animaldata = new Dictionary<ulong, ulong>();
        #region Start⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠
        void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("AnimalData")) animaldata = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, ulong>>("AnimalData");
            else
            {
                animaldata = new Dictionary<ulong, ulong>();
                Interface.Oxide.DataFileSystem.WriteObject("AnimalData", animaldata);
            }
        }
                
        private void OnServerInitialized()
        {
            ins = this; 
            LoadData();
            LoadMessages();
            AnimalRestore();
            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerInit);
            timer.Every(1f, () => AggroCheker());
        }
        private void Unload()
        {
            OnServerSave();
            DestroyAll();
        }
        private void DestroyAll()
        {
            var objects = UnityEngine.Object.FindObjectsOfType<AnimalStorage>();
            if (objects == null) return;
            foreach (var gameObj in objects) gameObj.DestroyInvoke();


        }
        void OnServerSave()
        {
            Interface.Oxide.DataFileSystem.WriteObject("AnimalData", animaldata);
        }
        #endregion
        void AnimalRestore()
        {
            foreach (var ai in animaldata.Keys.ToList())
            {
                NetworkableId networkableId = new NetworkableId(ai);
                var animal = BaseNetworkable.serverEntities.Find(networkableId) as BaseNpc;
                if (animal == null)
                {
                    animaldata.Remove(ai);
                    continue;
                }
                if (animal.GetComponent<AnimalStorage>() == null)
                {
                    animal.gameObject.AddComponent<AnimalStorage>();
                }
            }
        }
        public Dictionary<ulong, string> skinprefab = new Dictionary<ulong, string>()
        {
            [1813670239] = "assets/rust.ai/agents/bear/bear.prefab",
            [1813670815] = "assets/rust.ai/agents/boar/boar.prefab",
            [1813671243] = "assets/rust.ai/agents/horse/horse.prefab",
            [1813671637] = "assets/rust.ai/agents/wolf/wolf.prefab",
            [1813671912] = "assets/rust.ai/nextai/testridablehorse.prefab"
        };
        [ConsoleCommand("getpet")]
        void GetPet_cmd(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (arg.Args == null || arg.Args.Length < 2)
            {
                Puts("Use: getpet STEAMID type (wolf, boar, horse, bear, riderhorse)");
                return;
            }
            ulong uid = arg.GetUInt64(0);
            string type = arg.Args[1].ToLower();
            BasePlayer player = BasePlayer.FindByID(uid);
            ulong skinid = 0;
            switch (type)
            {
                case "wolf":
                    skinid = 1813671637;
                    break;
                case "boar":
                    skinid = 1813670815;
                    break;
                case "horse":
                    skinid = 1813671243;
                    break;
                case "bear":
                    skinid = 1813670239;
                    break;
                case "riderhorse":
                    skinid = 1813671912;
                    break;
                default:
                    Puts("Use: getpet STEAMID type (wolf, boar, horse, bear, riderhorse)");
                    return;
            }
            Item x = ItemManager.CreateByName("box.wooden.large", 1, skinid);
            x.name = skinid == 1813671637 ? "Волк" : skinid == 1813670815 ? "Кабан" : skinid == 1813671243 ? "Лошадь" : skinid == 1813670239 ? "Медведь" : skinid == 1813671912 ? "Ездовая Лошадь" : "";
            player.GiveItem(x, BaseEntity.GiveItemReason.PickedUp);
            SendReply(player, string.Format(GetMsg("GetPet", player), x.name));
        }

        object OnEntityBuilt(Planner plan, GameObject obj)
        {
            var entity = obj.GetComponent<BaseEntity>();
            if (entity != null && entity.ShortPrefabName.Contains("box.wooden.large") && skinprefab.ContainsKey(entity.skinID))
            {
                BasePlayer player = plan.GetOwnerPlayer();
                var pos = entity.transform.position;
                var rot = entity.transform.rotation;
                if (!skinprefab[entity.skinID].Contains("testridablehorse")) AnimalSpawn(player, pos, rot, entity.skinID);
                else SpawnRidingHorse(player, pos, rot, entity.skinID);
                timer.Once(1f, () => entity.Kill());
            }
            return null;
        } 

        void AnimalSpawn(BasePlayer player, Vector3 pos, Quaternion rot, ulong skinid, ulong playerid = 4790272)
        {
            BaseNpc npc = GameManager.server.CreateEntity($"{(skinprefab.ContainsKey(skinid) ? skinprefab[skinid] : "assets/rust.ai/agents/wolf/wolf.prefab")}", pos, rot) as BaseNpc;
            npc.enableSaving = true;
            npc.Spawn();
            animaldata.Add(npc.net.ID.Value, player.userID);
            npc.gameObject.AddComponent<AnimalStorage>();
            npc.GetComponent<AnimalStorage>().owner = player;
        }
        void SpawnRidingHorse(BasePlayer player, Vector3 pos, Quaternion rot, ulong skinid)
        {
            RidableHorse horse = GameManager.server.CreateEntity("assets/rust.ai/nextai/testridablehorse.prefab", pos, rot) as RidableHorse;
            horse.enableSaving = true;
            horse.Spawn();
            animaldata.Add(horse.net.ID.Value, player.userID);
            horse.gameObject.AddComponent<AnimalStorage>();
            horse.GetComponent<AnimalStorage>().owner = player;
        }
        object OnNpcTarget(BaseNpc entity, BaseEntity target)
        {
            AnimalStorage npcAi = entity.GetComponent<AnimalStorage>();
            if (npcAi != null) return false;
            return null;
        }
        public Dictionary<BaseNpc, float> agrotime = new Dictionary<BaseNpc, float>();
        void AggroCheker()
        {
            foreach (var npc in agrotime.Keys.ToList())
            {
                if (agrotime.ContainsKey(npc))
                {
                    if (agrotime[npc] > 0)
                    {
                        agrotime[npc]--;
                        if (!npc.GetComponent<AnimalStorage>().CheckTarget())
                        {
                            agrotime[npc] = 0;
                            npc.GetComponent<AnimalStorage>().TargerResset();
                        }
                        else if (npc.GetComponent<AnimalStorage>().CheckTarget() && agrotime[npc] == 0)
                        {
                            npc.GetComponent<AnimalStorage>().TargerResset();
                        }
                    }
                }
            }
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;
            if (entity?.net?.ID == null) return;
            var player = info.InitiatorPlayer;
            if (player == null) return;
            var victum = entity as BaseNpc;
            if (victum == null) return;
            if (victum.GetComponent<AnimalStorage>() == null) return;
            if (config.isPVE && !IsNPC(player))
            {
                info.damageTypes.ScaleAll(0);
                return;
            }
            if (agrotime.ContainsKey(victum) && agrotime[victum] > 0) return;
            if (!agrotime.ContainsKey(victum))
            {
                var animal = victum.GetComponent<AnimalStorage>();
                animal.SetTargetAnimal(player);
                agrotime.Add(victum, 20);
            }
            if (agrotime.ContainsKey(victum) && agrotime[victum] <= 0)
            {
                var animal = victum.GetComponent<AnimalStorage>();
                animal.SetTargetAnimal(player);
                agrotime[victum] = 20;
            }
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null) return;
            if (entity.GetComponent<AnimalStorage>() != null)
            {
                var pos = entity.transform.position;
                pos.y += 1;
                var storage = entity.GetComponentsInChildren<StorageContainer>();
                List<Item> itemlist = new List<Item>();
                foreach (var inv in storage) inv.inventory.itemList.ForEach(x => itemlist.Add(x));
                foreach (var item in itemlist) item.Drop(pos, Vector3.zero);
                itemlist.Clear();
                entity.GetComponent<AnimalStorage>().Death();
            }
        }
        private bool IsNPC(BasePlayer player)
        {
            if (player == null) return false;
            if (player is NPCPlayer) return true;
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L))
                return true;
            return false;
        }

        static Dictionary<string, string> prefabbone = new Dictionary<string, string>()
        {
            ["wolf"] = "spine2",
            ["boar"] = "spine2",
            ["horse"] = "spine_2",
            ["bear"] = "spine2",
        };

        static Dictionary<string, string> prefabHead = new Dictionary<string, string>()
        {
            ["wolf"] = "Head",
            ["boar"] = "head",
            ["horse"] = "head",
            ["bear"] = "head",
        };

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (!input.WasJustPressed(BUTTON.RELOAD)) return;
            RaycastHit hit;
            if (Physics.Raycast(player.eyes.position, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward, out hit, 2f) && hit.collider.name.Contains("rust.ai/agents"))
            {
                var npc = hit.GetEntity() as BaseNpc;
                if (npc == null) return;
                if (npc.GetComponent<AnimalStorage>() == null) return;
                if (npc.GetComponent<AnimalStorage>().owner == player) npc.GetComponent<AnimalStorage>().GoToSleep();
                else SendReply(player, string.Format(GetMsg("CantOrders", player)));
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                timer.Once(1f, () => OnPlayerInit(player));
                return;
            }
            foreach (var ai in animaldata.ToList())
            {
                if (ai.Value == player.userID)
                {
                    NetworkableId networkableId = new NetworkableId(ai.Key);
                    var animal = BaseNetworkable.serverEntities.Find(networkableId) as BaseNpc;
                    if (animal == null)
                    {
                        animaldata.Remove(ai.Key);
                        continue;
                    }
                    if (animal.GetComponent<AnimalStorage>() != null)
                    {
                        animal.GetComponent<AnimalStorage>().owner = player;
                    }
                }
            }
        }

        object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            var animal = container.GetComponentInParent<BaseNpc>();
            if (animal == null) return null;
            var aStorage = animal.GetComponent<AnimalStorage>();
            if (aStorage == null) return null;
            if (config.animalsetting[prefab[animal.PrefabName]].TeamateAcces)
            {
                var team = aStorage.owner.Team;
                if (team != null && team.members.Contains(player.userID))
                    return null;

                if (team == null && aStorage.owner == player)
                    return null;
            }
            if (config.animalsetting[prefab[animal.PrefabName]].OnlyOwner)
            {
                if (aStorage.owner == player)
                {
                    return null;
                }
                return false;
            }
            return null; 
        }

        public class AnimalStorage : FacepunchBehaviour
        {
            BaseNpc animalNPC;
            RidableHorse horse;
            public BasePlayer owner; 
            StorageContainer contaner;
            StorageContainer contaner2;
            DroppedItem hat;
            ulong ID;
            ulong Hid;
            void Awake()
            {
                if (GetComponent<BaseNpc>() != null) animalNPC = GetComponent<BaseNpc>();
                else
                {
                    horse = GetComponent<RidableHorse>();
                }
                if (animalNPC != null)
                {
                    if (animalNPC.MaxHealth() < ins.config.animalsetting[prefab[animalNPC.PrefabName]].maxhealt) animalNPC.InitializeHealth(ins.config.animalsetting[prefab[animalNPC.PrefabName]].maxhealt, ins.config.animalsetting[prefab[animalNPC.PrefabName]].maxhealt);
                    animalNPC.AttackDamage = ins.config.animalsetting[prefab[animalNPC.PrefabName]].damagevalue;
                    animalNPC.Stats.TurnSpeed = ins.config.animalsetting[prefab[animalNPC.PrefabName]].speedvalue;
                    animalNPC.Stats.Speed = ins.config.animalsetting[prefab[animalNPC.PrefabName]].speedrunvalue;
                    animalNPC.CurrentBehaviour = BaseNpc.Behaviour.Idle;
                    if (animalNPC.GetComponentInChildren<StorageContainer>() == null)
                    {
                        if (animalNPC.ShortPrefabName != "bear")
                        {
                            SetStorage(false);
                        }
                        else BearContainer();
                    }
                    else
                    {
                        if (animalNPC.ShortPrefabName != "bear")
                        {
                            RestoreStorage();
                        }
                        else BearContainer(true);
                        IsSleep = true;
                    }

                    if (animalNPC.GetComponentInChildren<DroppedItem>() == null)
                        SetHeat();
                    else
                        SetHeat(true);

                    animalNPC.SendNetworkUpdate();
                    InvokeRepeating(ChekTP, 5f, 5f);
                }
                if (horse != null)
                {
                    if (horse.MaxHealth() < ins.config.animalsetting[prefab[horse.PrefabName]].maxhealt) horse.InitializeHealth(ins.config.animalsetting[prefab[horse.PrefabName]].maxhealt, ins.config.animalsetting[prefab[horse.PrefabName]].maxhealt);
                    SetStorage(true);
                    horse.SendNetworkUpdate();
                    Hid = horse.net.ID.Value;
                }
                ID = animalNPC.net.ID.Value;
            }
            void SetHeat(bool restore = false)
            {
                if (!restore) hat = ItemManager.CreateByName("hat.boonie", 1, 2195341793).Drop(animalNPC.transform.position, new Vector3()).GetComponent<DroppedItem>();
                else hat = GetComponentInChildren<DroppedItem>();
                hat.GetComponent<Rigidbody>().isKinematic = true;
                hat.GetComponent<Rigidbody>().useGravity = false;
                var reply = 4678;
                if (reply == 0) { }
                hat.allowPickup = false;
                var boneID = StringPool.Get($"{(animalNPC.ShortPrefabName == "wolf" ? "Head" : "head")}");
                hat.gameObject.Identity();
                hat.SetParent(animalNPC, boneID);

                hat.enableSaving = true;
                if (animalNPC.ShortPrefabName == "wolf")
                {
                    hat.transform.localPosition = new Vector3(0, 0, -0.08f);
                    hat.transform.localRotation = Quaternion.Euler(new Vector3(-90, 0, 0));
                }
                if (animalNPC.ShortPrefabName == "boar")
                {
                    hat.transform.localPosition = new Vector3(0, 0, 0.16f);
                    hat.transform.localRotation = Quaternion.Euler(new Vector3(90, 0, 0));
                }
                if (animalNPC.ShortPrefabName == "horse")
                {
                    hat.transform.localPosition = new Vector3(0, 0, 0.1f);
                    hat.transform.localRotation = Quaternion.Euler(new Vector3(90, 0, 0));
                }
                if (animalNPC.ShortPrefabName == "bear")
                {
                    hat.transform.localPosition = new Vector3(0, 0, 0.2f);
                    hat.transform.localRotation = Quaternion.Euler(new Vector3(90, 0, 0));
                }
                CancelInvoke(hat.GetComponent<DroppedItem>().IdleDestroy);
            }
            void SetStorage(bool isriderhorse)
            {
                if (animalNPC.GetComponentsInChildren<StorageContainer>().Length == 0)
                {
                    if (animalNPC.PrefabName.Contains("wolf") || animalNPC.PrefabName.Contains("boar") || (animalNPC.PrefabName.Contains("horse") && !isriderhorse))
                        contaner = GameManager.server.CreateEntity("assets/prefabs/deployable/small stash/small_stash_deployed.prefab") as StorageContainer;
                    else
                        contaner = GameManager.server.CreateEntity("assets/prefabs/deployable/small stash/small_stash_deployed.prefab") as StorageContainer;
                    contaner.Spawn();
                }
                else
                    contaner = GetComponentsInChildren<StorageContainer>()[0];

                contaner.gameObject.Identity();
                if (contaner.GetComponent<Rigidbody>() == null) contaner.gameObject.AddComponent<Rigidbody>();
                contaner.GetComponent<Rigidbody>().isKinematic = true;
                contaner.GetComponent<Rigidbody>().useGravity = false;
                contaner.panelName = "generic_resizable";
                int slots = 0;
                 
                contaner.enableSaving = true;
                if (!isriderhorse)
                {
                    if (animalNPC.ShortPrefabName == "boar")
                        contaner.SetParent(animalNPC);
                    else
                        contaner.SetParent(animalNPC, "spine1");
                }
                else contaner.SetParent(horse, "spine_2");

                contaner.onlyAcceptCategory = ItemCategory.All;
                slots = prefab.ContainsKey(animalNPC.PrefabName) ? ins.config.animalsetting[prefab[animalNPC.PrefabName]].slots : 6;
                contaner.inventory.capacity = slots;
                contaner.inventorySlots = slots;
                contaner.SendNetworkUpdate();
                if (!isriderhorse)
                {
                    slots = prefab.ContainsKey(animalNPC.PrefabName) ? ins.config.animalsetting[prefab[animalNPC.PrefabName]].slots : 6;
                    if (animalNPC.ShortPrefabName == "wolf")
                    {
                        contaner.transform.localPosition = new Vector3(0, 0.75f, 0.0f);
                        contaner.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
                    }
                    else if (animalNPC.ShortPrefabName == "boar")
                    {
                        contaner.transform.localPosition = new Vector3(-0.0f, 0.85f, 0.0f);
                        contaner.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
                    }
                    else
                    {
                        contaner.transform.localPosition = new Vector3(0f, 1.45f, 0f);
                        contaner.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
                    }

                }
                else
                {
                    slots = prefab.ContainsKey(horse.PrefabName) ? ins.config.animalsetting[prefab[horse.PrefabName]].slots : 6;
                    contaner.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
                    contaner.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 90f));
                }
                contaner.SendNetworkUpdate();
            }

            void BearContainer(bool restore = false)
            {
                int slots = prefab.ContainsKey(animalNPC.PrefabName) ? ins.config.animalsetting[prefab[animalNPC.PrefabName]].slots : 6;
                if (!restore) contaner = GameManager.server.CreateEntity("assets/prefabs/deployable/small stash/small_stash_deployed.prefab") as StorageContainer;
                else contaner = GetComponentsInChildren<StorageContainer>()[0];
                if (contaner.GetComponent<Rigidbody>() == null) contaner.gameObject.AddComponent<Rigidbody>();
                contaner.GetComponent<Rigidbody>().isKinematic = true;
                contaner.GetComponent<Rigidbody>().useGravity = false;
                contaner.panelName = "generic_resizable";
                contaner.transform.localPosition = new Vector3(-0.0f, 0.4f, -0.2f);
                contaner.transform.localRotation = Quaternion.Euler(new Vector3(180f, 0f, 180f));
                contaner.enableSaving = true;
                if (!restore) contaner.Spawn();
                contaner.inventory.capacity = slots;
                contaner.inventorySlots = slots;
                contaner.SetParent(animalNPC, prefabbone[animalNPC.ShortPrefabName]);
                contaner.SendNetworkUpdate();
                if (!restore) contaner2 = GameManager.server.CreateEntity("assets/prefabs/deployable/small stash/small_stash_deployed.prefab") as StorageContainer;
                else contaner2 = GetComponentsInChildren<StorageContainer>()[1];
                if (contaner2.GetComponent<Rigidbody>() == null) contaner2.gameObject.AddComponent<Rigidbody>();
                contaner2.GetComponent<Rigidbody>().isKinematic = true;
                contaner2.GetComponent<Rigidbody>().useGravity = false;
                contaner2.panelName = "generic_resizable";
                contaner2.transform.localPosition = new Vector3(-0.0f, -0.4f, -0.2f);
                contaner2.transform.localRotation = Quaternion.Euler(new Vector3(0f, 180f, 180));
                contaner2.enableSaving = true;
                if (!restore) contaner2.Spawn();
                contaner2.inventory.capacity = slots;
                contaner2.inventorySlots = slots;
                contaner2.SetParent(animalNPC, prefabbone[animalNPC.ShortPrefabName]);
                contaner2.SendNetworkUpdate();
            }
            void RestoreStorage()
            {
                contaner = GetComponentInChildren<StorageContainer>();
                contaner.gameObject.Identity();
                if (contaner.GetComponent<Rigidbody>() == null) contaner.gameObject.AddComponent<Rigidbody>();
                contaner.GetComponent<Rigidbody>().isKinematic = true;
                contaner.GetComponent<Rigidbody>().useGravity = false;
                contaner.panelName = "generic_resizable";
                int slots = prefab.ContainsKey(animalNPC.PrefabName) ? ins.config.animalsetting[prefab[animalNPC.PrefabName]].slots : 6;
                contaner.inventory.capacity = slots;
                contaner.inventorySlots = slots;
                if (animalNPC.ShortPrefabName == "wolf")
                {
                    contaner.transform.localPosition = new Vector3(0, 0.75f, 0.0f);
                    contaner.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
                }
                else if (animalNPC.ShortPrefabName == "boar")
                {
                    contaner.transform.localPosition = new Vector3(-0.0f, 0.75f, 0.0f);
                    contaner.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
                }
                else
                {
                    contaner.transform.localPosition = new Vector3(0f, 1.45f, 0f);
                    contaner.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
                }

                contaner.enableSaving = true;
                contaner.SendNetworkUpdate();
            }

            void OnDestroy()
            {
                if (hat != null) hat.Kill();
            }

            public void Death()
            {
                if (animalNPC != null && ins.animaldata.ContainsKey(animalNPC.net.ID.Value))
                {
                    if (ins.agrotime.ContainsKey(animalNPC)) ins.agrotime.Remove(animalNPC);
                    ins.animaldata.Remove(animalNPC.net.ID.Value);
                }
                if (horse != null && horse.net != null && ins.animaldata.ContainsKey(horse.net.ID.Value)) ins.animaldata.Remove(horse.net.ID.Value);
                if (contaner != null) contaner.Kill();
                if (contaner2 != null) contaner2.Kill();
                if (hat != null) hat.Kill();
                CancelInvoke(ChekTP);
                Destroy(this);
            }

            public void DestroyInvoke()
            {
                CancelInvoke(ChekTP);
                foreach (var obj in animalNPC.GetComponentsInChildren<DroppedItem>())
                    obj.Kill();
                Destroy(this);
            }

            private bool IsSleep = false;
            public void GoToSleep()
            {
                IsSleep = !IsSleep;
                if (!IsSleep) ins.SendReply(owner, string.Format(ins.GetMsg("follow", owner)));
                else ins.SendReply(owner, string.Format(ins.GetMsg("relax", owner)));
            }

            private float lastFireMG;
            bool Aggro = false;
            void FixedUpdate()
            {
                if (animalNPC == null) return;
                if (hat == null || hat.IsDestroyed)
                    SetHeat();
                if (Aggro) return;
                animalNPC.SetFact(BaseNpc.Facts.CanTargetEnemies, 0);
                if (IsSleep) 
                {
                    StopMoving();
                    SetBehaviour(BaseNpc.Behaviour.Sleep);
                    if (Time.realtimeSinceStartup >= lastFireMG)
                    {
                        animalNPC.health += 0.5f;
                        animalNPC.health = Mathf.Clamp(animalNPC.health, 0, animalNPC.MaxHealth());
                        lastFireMG = Time.realtimeSinceStartup + 1f;
                        animalNPC.SendNetworkUpdate();
                    }
                }
                else
                {
                    SetBehaviour(BaseNpc.Behaviour.Idle);
                    if (owner != null)
                    {
                        float distance = Vector3.Distance(transform.position, owner.transform.position);
                        if (distance > 5) UpdateDestination(owner.transform.position + (-owner.eyes.HeadForward() * 2), distance > 10);
                        else StopMoving();
                    }
                }
            }

            private BasePlayer animaltarger;

            public void SetTargetAnimal(BasePlayer targer)
            {
                if (animalNPC == null) return;
                if (targer == null) return;
                if (owner != null && targer.userID == owner.userID) return;
                if (targer.IsDead()) return;
                Aggro = true;
                this.animaltarger = targer;
                SetBehaviour(BaseNpc.Behaviour.Attack);
                animalNPC.SetFact(BaseNpc.Facts.HasEnemy, (byte)3);
                animalNPC.AttackTarget = targer;
                animalNPC.SendNetworkUpdate();
            }

            public bool CheckTarget()
            {
                if (animaltarger == null || animaltarger.IsDead()) return false;
                return true;
            }

            public void TargerResset()
            {
                Aggro = false;
            }

            void ChekTP()
            {
                if (owner == null) return;
                if (IsSleep) return;
                float distance = Vector3.Distance(transform.position, owner.transform.position);
                if (distance > ins.config.animalsetting[prefab[animalNPC.PrefabName]].maxdistance && ins.config.animalsetting[prefab[animalNPC.PrefabName]].AnimalTp)
                {
                    var pos = owner.transform.position + (-owner.eyes.BodyForward() * 2);
                    animalNPC.transform.position = GetGroundPosition(pos);
                    lastFireMG = Time.realtimeSinceStartup + 10;
                }
            }

            Vector3 GetGroundPosition(Vector3 pos)
            {
                float y = TerrainMeta.HeightMap.GetHeight(pos);
                RaycastHit hit;
                Vector3 poss = Vector3.zero;
                int radius = 15;
                while (poss == Vector3.zero)
                {
                    poss = RandomCircle(pos, radius);
                    if (Physics.Raycast(new Vector3(poss.x, poss.y + 200f, poss.z), Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask(new[] {
                        "Terrain", "World", "Default", "Construction", "Deployed"
                    }
                    )) && !hit.collider.name.Contains("building")) poss.y = Mathf.Max(hit.point.y, y);
                    else
                    {
                        poss = Vector3.zero;
                        radius += 1;
                    }
                }
                return poss;
            }

            Vector3 RandomCircle(Vector3 center, float radius = 15)
            {
                float ang = UnityEngine.Random.value * 360;
                Vector3 pos;
                pos.x = center.x + radius * Mathf.Sin(ang * Mathf.Deg2Rad);
                pos.z = center.z + radius * Mathf.Cos(ang * Mathf.Deg2Rad);
                pos.y = center.y;
                return pos;
            }
            private void UpdateDestination(Vector3 position, bool run)
            {
                animalNPC.UpdateDestination(position);
                animalNPC.TargetSpeed = run ? animalNPC.Stats.Speed : animalNPC.Stats.Speed * 0.3f;
            }
            private void StopMoving()
            {
                animalNPC.IsStopped = true;
                animalNPC.ChaseTransform = null;
                animalNPC.SetFact(BaseNpc.Facts.PathToTargetStatus, 0, true, true);
            }
            private void SetBehaviour(BaseNpc.Behaviour behaviour)
            {
                if (animalNPC.CurrentBehaviour != behaviour) animalNPC.CurrentBehaviour = behaviour;
            }
        }
        string GetMsg(string key, BasePlayer player = null)
        {
            return lang.GetMessage(key, this, player == null ? null : player.UserIDString);
        }
        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["GetPet"] = "You get a pet - {0}! Set it on the ground!",
                ["CantOrders"] = "You can't give orders to someone else's pet!",
                ["follow"] = "Your pet is now following you",
                ["relax"] = "Your pet is resting and gaining strength."
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["GetPet"] = "Вы получили питомца - {0}! Установите его на землю!",
                ["CantOrders"] = "Вы не можете отдавать приказы чужому животному",
                ["follow"] = "Ваш питомец теперь следует за вами",
                ["relax"] = "Ваш питомец отдыхает и набирается сил."
            }, this, "ru");
        }
    }
}