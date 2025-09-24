// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Random = UnityEngine.Random;
using System;
using Oxide.Core;
using Rust;
using Oxide.Core.Plugins;
using System.Collections;
using ru = Oxide.Game.Rust;

namespace Oxide.Plugins
{
    [Info("SkillRates", "fermens", "2.0.1")]
    class SkillRates : RustPlugin
    {
        const bool fermensEN = true; // true - config in english | false - конфиг на русском

        #region Images
        [PluginReference] private Plugin ImageLibrary, HaxBot, LootDefender;
        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        #endregion

        #region Lang
        private string GetLang(string key, string userid)
        {
            return lang.GetMessage(key, this, userid);
        }
        #endregion

        #region X2Rates
        class modificator
        {
            public float gather;
            public float xp;
        }

        Dictionary<string, modificator> modificators = new Dictionary<string, modificator>();

        void OnGroupPermissionGranted(string name, string perm)
        {
            foreach (BasePlayer player in BasePlayer.allPlayerList)
            {
                if (permission.UserHasGroup(player.UserIDString, name))
                {
                    UPDATEMOD(player.UserIDString);
                }
            }
        }

        void OnGroupPermissionRevoked(string name, string perm)
        {
            foreach (BasePlayer player in BasePlayer.allPlayerList)
            {
                if (permission.UserHasGroup(player.UserIDString, name))
                {
                    UPDATEMOD(player.UserIDString);
                    
                }
            }
        }

        void OnUserGroupRemoved(string id, string groupName)
        {
            NextTick(() =>
            {
                UPDATEMOD(id);
            });
        }

        void OnUserGroupAdded(string id, string groupName)
        {
            NextTick(() =>
            {
                UPDATEMOD(id);
            });
        }

        void OnUserPermissionGranted(string id, string permName)
        {
            UPDATEMOD(id);
            TermLevelADD(id, permName);
        }

        void OnUserPermissionRevoked(string id, string permName)
        {
            UPDATEMOD(id);
            TermLevelRemove(id, permName);
        }

        private void TermLevelADD(string id, string permName)
        {
            string perm = permName.ToLower();
            if (!config.settings.Keys.Any(x => perm == "skillrates." + x)) return;
            BasePlayer player = BasePlayer.FindByID(Convert.ToUInt64(id)) ?? BasePlayer.FindAwakeOrSleeping(id);
            if(player == null)
            {
                Debug.LogError($"[SkillRates] {id} player not found! <set max lvl>");
                return;
            }
            string skill = perm.Replace("skillrates.", "");
            SetMaxLvl(player, skill);
        }

        private void TermLevelRemove(string id, string permName)
        {
            string perm = permName.ToLower();
            if (!config.settings.Keys.Any(x => perm == "skillrates." + x)) return;
            string skill = perm.Replace("skillrates.", "");
            DelMaxLvl(id, skill);
        }
        #endregion

        #region Cash
        static string maxlevelstring;
        class mod
        {
            public float exp;
            public Dictionary<MODIFICATOR, float> modificator;
        }
        Dictionary<int, mod> levels = new Dictionary<int, mod>();
        enum SKILL { miner, alchemist, woodcutter, hunter, marauder, technicist, jeweler, dustman, farmer };
        enum MODIFICATOR { HQM, STONE, METAL, SULFUR, REMELTINGSPEED, REFINERYSPEED, WOOD, COALSHANCE, ANIMAL, ANIMALARMOR, ANIMALDAMAGE, NPC, NPCARMOR, NPCDAMAGE, MECH, MECHARMOR, MECHDAMAGE, AIR, LOCKEDCRATE, ELITE, BARREL, CRATE, GROWABLEVEGETABLES, GROWABLEBERRY };
        Dictionary<ulong, Dictionary<SKILL, mainskill>> players = new Dictionary<ulong, Dictionary<SKILL, mainskill>>();
        class mainskill
        {
            public float exp;
            public int lvl;
            public int lastlvl;
        }
        const string prefabstone = "stone";
        const string prefabmetal = "metal";
        const string prefabsulfur = "sulfur";
        #endregion

        #region Config
        private PluginConfig config;
        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        class INITLVLS
        {
            [JsonProperty(fermensEN ? "Maximal lvl" : "Максимальный уровень")]
            public int maxlevel;

            [JsonProperty(fermensEN ? "Exp - 0 lvl" : "Опыт - 0 уровень")]
            public float modificator_start;

            [JsonProperty(fermensEN ? "Magnification" : "Увеличение")]
            public float modificator_multiplication;
        }

        class EXPERIMENT
        {
            [JsonProperty(fermensEN ? "Enable?" : "Включено?")]
            public bool enable;

            [JsonProperty(fermensEN ? "Amount crates" : "Количество ящиков")]
            public int amount;
        }

        class setting2
        {
            [JsonProperty(fermensEN ? "First lvl" : "Первый уровень")]
            public float first;

            [JsonProperty(fermensEN ? "Last lvl" : "Последний уровень")]
            public float last;
        }

        class setting
        {
            [JsonProperty(fermensEN ? "Background" : "Фон")]
            public string background;

            [JsonProperty(fermensEN ? "Color - lvl" : "Цвет уровня")]
            public string lvlcolor;

            [JsonProperty(fermensEN ? "Color - progress, main" : "Цвет прогресса - Основной")]
            public string mainbar;

            [JsonProperty(fermensEN ? "Color - progress, background" : "Цвет прогресса - Фон")]
            public string backbar;

            [JsonProperty(fermensEN ? "Bonuses" : "Бонусы - настройка")]
            public Dictionary<MODIFICATOR, setting2> mod;

            [JsonProperty(fermensEN ? "Reward for reaching a certain level" : "Награда за достижение определенного уровня")]
            public Dictionary<int, List<string>> reward;
        }

        class expelement
        {
            [JsonProperty(fermensEN ? "Barrel" : "Бочка")]
            public float barrel;

            [JsonProperty(fermensEN ? "Regular box" : "Обычный ящик")]
            public float crate;

            [JsonProperty(fermensEN ? "Stone - mined" : "Камень - добытый")]
            public float ore_stone;

            [JsonProperty(fermensEN ? "Metal - mined" : "Метал - добытый")]
            public float ore_metal;

            [JsonProperty(fermensEN ? "Sulfur - mined" : "Сульфур - добытый")]
            public float ore_sulfur;

            [JsonProperty(fermensEN ? "Tree - downed" : "Дерево - поваленное")]
            public float wood;

            [JsonProperty(fermensEN ? "Helicopter - downed" : "Вертолет - сбитый")]
            public float heli;

            [JsonProperty(fermensEN ? "Stone - picked up" : "Камень - поднятый")]
            public float grab_stone;

            [JsonProperty(fermensEN ? "Metal - picked up" : "Метал - поднятый")]
            public float grab_metal;

            [JsonProperty(fermensEN ? "Sulfur - picked up" : "Сульфур - поднятый")]
            public float grab_sulfur;

            [JsonProperty(fermensEN ? "Wood - picked up" : "Дерево - поднятое")]
            public float grab_wood;

            [JsonProperty(fermensEN ? "Bradley - exploded" : "Танк - взорванный")]
            public float tank;

            [JsonProperty(fermensEN ? "Bradley - mining parts" : "Танк - добыча частей")]
            public float tank_fleshed;

            [JsonProperty(fermensEN ? "Helicopter - mining parts" : "Вертолет - добыча частей")]
            public float heli_fleshed;

            [JsonProperty(fermensEN ? "Helicopter - crate" : "Вертолет - ящик")]
            public float heli_cont;

            [JsonProperty(fermensEN ? "Bradley - crate" : "Танк - ящик")]
            public float tank_cont;

            [JsonProperty(fermensEN ? "Wolf - killed" : "Волк - убитый")]
            public float wolf;
            [JsonProperty(fermensEN ? "Boar - killed" : "Кабан - убитый")]
            public float boar;
            [JsonProperty(fermensEN ? "Horse - killed" : "Лошадь - убитая")]
            public float horse;
            [JsonProperty(fermensEN ? "Riding horse - killed" : "Верховая лошадь - убитая")]
            public float ridablehorse;
            [JsonProperty(fermensEN ? "Stag - killed" : "Олень - убитый")]
            public float stag;
            [JsonProperty(fermensEN ? "Chicken - killed" : "Курица - убитая")]
            public float chicken;
            [JsonProperty(fermensEN ? "Bear - killed" : "Медведь - убитый")]
            public float bear;
            [JsonProperty(fermensEN ? "Supply crate" : "Аир дроп")]
            public float air;
            [JsonProperty(fermensEN ? "Locked crate" : "Закрытый ящик")]
            public float lockedcrate;
            [JsonProperty(fermensEN ? "Elit crate" : "Элитный ящик")]
            public float elite;
            [JsonProperty(fermensEN ? "Wolf - fleshed" : "Волк - добытый")]
            public float wolf_fleshed;
            [JsonProperty(fermensEN ? "Horse - fleshed" : "Лошадь - добытая")]
            public float horse_fleshed;
            [JsonProperty(fermensEN ? "Boar - fleshed" : "Кабан - добытый")]
            public float boar_fleshed;
            [JsonProperty(fermensEN ? "Stag - fleshed" : "Олень - добытый")]
            public float stag_fleshed;
            [JsonProperty(fermensEN ? "Chicken - fleshed" : "Курица - добытая")]
            public float chicken_fleshed;
            [JsonProperty(fermensEN ? "Bear - fleshed" : "Медведь - добытый")]
            public float bear_fleshed;
            [JsonProperty(fermensEN ? "NPC - killed" : "Бот - убитый")]
            public float npc;
            [JsonProperty(fermensEN ? "NPC - looted" : "Бот - залутаный")]
            public float npc_looted;
            [JsonProperty(fermensEN ? "Vegetables - crop" : "Овощи - урожай")]
            public float grow_vegetables;
            [JsonProperty(fermensEN ? "Berry - harvest" : "Ягоды - урожай")]
            public float grow_berrys;
            [JsonProperty(fermensEN ? "Vegetables - planting" : "Овощи - посадка")]
            public float planting_vegetables;
            [JsonProperty(fermensEN ? "Berry - planting" : "Ягоды - посадка")]
            public float planting_berrys;
        }

        class EXPBar
        {
            [JsonProperty(fermensEN ? "Enable experience bar?" : "Включить полосу опыта?")]
            public bool enable;
        }

        class UI
        {
            public string anchor_min;
            public string anchor_max;
            public string offset_min;
            public string offset_max;
            public string fadein;
            public string backgroundcolor;
        }

        private class PluginConfig
        {
            public string chatcommand { get; set; } = "skill";

            #region Exp bar
            public EXPBar eXPBar { get; set; } = new EXPBar { enable = true };
            #endregion

            #region ui
            [JsonProperty("UI")]
            public UI uI { get; set; } = new UI { fadein = "0.5", offset_min = "0 0", offset_max = "0 0", anchor_min = "0 0", anchor_max = "1 1", backgroundcolor = "0 0 0 0.9960784" };
            #endregion

            #region lvl
            [JsonProperty(fermensEN ? "Level generation" : "Генерация уровней")]
            public INITLVLS lvl { get; set; } = new INITLVLS { maxlevel = 100, modificator_start = 1f, modificator_multiplication = 1.05f };
            #endregion

            #region carier
            [JsonProperty(fermensEN ? "Quary rates - static" : "Рейты карьера - статика")]
            public float carier { get; set; } = 5f;
            #endregion

            #region loottoheli
            [JsonProperty(fermensEN ? "Additional boxes from helicopter and tank" : "Экспериментально - [доп ящики для вертолета с танка]")]
            public EXPERIMENT loottoheli { get; set; } = new EXPERIMENT { enable = true, amount = 2 };
            #endregion
            #region settings
            [JsonProperty(fermensEN ? "Skills - setting" : "Навыки - настройка")]
            public Dictionary<SKILL, setting> settings { get; set; } = new Dictionary<SKILL, setting>
            {
                { SKILL.miner, new setting { lvlcolor = "0.73 0.87 0.745 1", mainbar = "0.21 0.42 0.26 1", backbar = "0.16 0.16 0.16 1", mod = new Dictionary<MODIFICATOR, setting2> { { MODIFICATOR.STONE, new setting2 { first = 10f, last = 20f } }, { MODIFICATOR.METAL, new setting2 { first = 10f, last = 20f } }, { MODIFICATOR.HQM, new setting2 { first = 10f, last = 20f } } } } },
                { SKILL.alchemist, new setting { lvlcolor = "0.73 0.87 0.745 1", mainbar = "0.21 0.42 0.26 1", backbar = "0.16 0.16 0.16 1", mod = new Dictionary<MODIFICATOR, setting2> { { MODIFICATOR.SULFUR, new setting2 { first = 5f, last = 10f } }, { MODIFICATOR.REMELTINGSPEED, new setting2 { first = 5f, last = 20f } } } } },
                { SKILL.woodcutter, new setting { lvlcolor = "0.73 0.87 0.745 1", mainbar = "0.21 0.42 0.26 1", backbar = "0.16 0.16 0.16 1", mod = new Dictionary<MODIFICATOR, setting2> { { MODIFICATOR.WOOD, new setting2 { first = 10f, last = 20f } }, { MODIFICATOR.COALSHANCE, new setting2 { first = 0.75f, last = 1f } }, { MODIFICATOR.REFINERYSPEED, new setting2 { first = 5f, last = 20f } } } } },
                { SKILL.hunter, new setting { lvlcolor = "0.91 0.75 0.75 1", mainbar = "0.51 0.25 0.25 1", backbar = "0.16 0.16 0.16 1", mod = new Dictionary<MODIFICATOR, setting2> { { MODIFICATOR.ANIMAL, new setting2 { first = 10f, last = 20f } }, { MODIFICATOR.ANIMALARMOR, new setting2 { first = 0f, last = 0.25f } } } } },
                { SKILL.marauder, new setting { lvlcolor = "0.91 0.75 0.75 1", mainbar = "0.51 0.25 0.25 1", backbar = "0.16 0.16 0.16 1", mod = new Dictionary<MODIFICATOR, setting2> { { MODIFICATOR.NPC, new setting2 { first = 10f, last = 20f } }, { MODIFICATOR.NPCARMOR, new setting2 { first = 0f, last = 0.1f } }, { MODIFICATOR.NPCDAMAGE, new setting2 { first = 0f, last = 0.25f } } } } },
                { SKILL.technicist, new setting { lvlcolor = "0.91 0.75 0.75 1",mainbar = "0.51 0.25 0.25 1", backbar = "0.16 0.16 0.16 1", mod = new Dictionary<MODIFICATOR, setting2> { { MODIFICATOR.MECH, new setting2 { first = 10f, last = 20f } }, { MODIFICATOR.MECHARMOR, new setting2 { first = 0f, last = 0.25f } }, { MODIFICATOR.MECHDAMAGE, new setting2 { first = 0f, last = 0.1f } } } } },
                { SKILL.jeweler, new setting { lvlcolor = "0.756 0.75 0.87 1", mainbar = "0.26 0.25 0.46 1", backbar = "0.16 0.16 0.16 1", mod = new Dictionary<MODIFICATOR, setting2> { { MODIFICATOR.AIR, new setting2 { first = 10f, last = 20f } }, { MODIFICATOR.LOCKEDCRATE, new setting2 { first = 10f, last = 20f } }, { MODIFICATOR.ELITE, new setting2 { first = 10f, last = 20f } } } } },
                { SKILL.dustman, new setting { lvlcolor = "0.756 0.75 0.87 1", mainbar = "0.26 0.25 0.46 1", backbar = "0.16 0.16 0.16 1", mod = new Dictionary<MODIFICATOR, setting2> { { MODIFICATOR.BARREL, new setting2 { first = 10f, last = 20f } }, { MODIFICATOR.CRATE, new setting2 { first = 10f, last = 20f } } } } },
                { SKILL.farmer, new setting { lvlcolor = "0.756 0.75 0.87 1", mainbar = "0.26 0.25 0.46 1", backbar = "0.16 0.16 0.16 1", mod = new Dictionary<MODIFICATOR, setting2> { { MODIFICATOR.GROWABLEVEGETABLES, new setting2 { first = 1f, last = 5f } }, { MODIFICATOR.GROWABLEBERRY, new setting2 { first = 1f, last = 5f } } } } }
            };
            #endregion

            #region exp
            [JsonProperty(fermensEN ? "Exp" : "Опыт")]
            public expelement exp { get; set; } = new expelement
            {
                barrel = 1.5f,
                crate = 2f,
                ore_stone = 1.5f,
                ore_metal = 1.5f,
                ore_sulfur = 2f,
                wood = 2f,
                grab_stone = 0.1f,
                grab_metal = 0.1f,
                grab_sulfur = 0.1f,
                grab_wood = 0.1f,
                heli = 50f,
                tank = 25f,
                tank_fleshed = 0.5f,
                heli_fleshed = 0.5f,
                heli_cont = 2f,
                tank_cont = 2f,
                wolf = 1f,
                stag = 0.75f,
                horse = 0.75f,
                ridablehorse = 0.1f,
                boar = 0.75f,
                chicken = 0.2f,
                bear = 1.5f,
                air = 2f,
                lockedcrate = 10f,
                elite = 3.5f,
                wolf_fleshed = 1.5f,
                horse_fleshed = 1.25f,
                boar_fleshed = 1.25f,
                stag_fleshed = 1.25f,
                chicken_fleshed = 0.4f,
                bear_fleshed = 3f,
                npc = 1.5f,
                npc_looted = 1f,
                grow_berrys = 0.75f,
                grow_vegetables = 0.5f
            };
            #endregion

            #region broadcast
            [JsonProperty(fermensEN ? "Show progress of other players in global chat" : "Отображать повышения уровней у других")]
            public bool broadcast { get; set; } = false;
            #endregion

            #region boosters
            [JsonProperty(fermensEN ? "Exp boosters : permissions" : "Бустеры - пермишены")]
            public Dictionary<string, float> boosters { get; set; } = new Dictionary<string, float>
            {
                { "skillrates.x3boost", 3f },
                { "skillrates.x2boost", 2f }
            };
            #endregion

            #region gathers
            [JsonProperty(fermensEN ? "Gather/loot boosters : permissions" : "Увеличители добычи - пермишены")]
            public Dictionary<string, float> gathers { get; set; } = new Dictionary<string, float>
            {
                { "skillrates.x3", 3f },
                { "skillrates.x2", 2f }
            };
            #endregion
        }
        #endregion

        #region DAMAGETOMECH
        Dictionary<BaseCombatEntity, Dictionary<BasePlayer, float>> mechdamage = new Dictionary<BaseCombatEntity, Dictionary<BasePlayer, float>>();
        private void DAMAGETOMECH(BaseCombatEntity entity, BasePlayer player, float damage)
        {
            if (!mechdamage.ContainsKey(entity)) mechdamage.Add(entity, new Dictionary<BasePlayer, float>());
            if (!mechdamage[entity].ContainsKey(player)) mechdamage[entity].Add(player, damage);
            else mechdamage[entity][player] += damage;
        }
        #endregion

        #region SSS
        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (modificators.ContainsKey(player.UserIDString)) modificators.Remove(player.UserIDString);
            if (players.ContainsKey(player.userID)) Interface.Oxide.DataFileSystem.WriteObject("SKILLS/" + player.UserIDString, players[player.userID]);
        }


        private static SkillRates ins;

        private void Init()
        {
            ins = this;
        }

        private void OnContainerDropItems(ItemContainer container)
        {
            LootContainer lootcont = container.entityOwner as LootContainer;
            if (lootcont == null || lootcont.OwnerID != 0) return;
            var player = lootcont?.lastAttacker?.ToPlayer();
            if (lootcont.HasFlag(BaseEntity.Flags.Reserved7) || !lootcont.PrefabName.Contains("barrel")) return;
            if (player != null)
            {
                Dictionary<SKILL, mainskill> skill;
                if (!players.TryGetValue(player.userID, out skill))
                {
                    //   Debug.Log("[OnContainerDropItems] skill id:" + player.UserIDString + " mini-error");
                    return;
                }
                UPRATELOOT(player, lootcont, MODGET(skill[SKILL.dustman].lvl, MODIFICATOR.BARREL));
                ADDEXP(player, config.exp.barrel, SKILL.dustman);
            }
            else
            {
                foreach (var item in lootcont.inventory.itemList.Where(x => x.info.stackable > 1))
                {
                    item.amount = (int)(item.amount * MODGET(0, MODIFICATOR.BARREL));
                }
            }
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null || info.Initiator == null) return;
            if (info.InitiatorPlayer != null)
            {
                if (!IsNPC(info.InitiatorPlayer))
                {
                    Dictionary<SKILL, mainskill> skill;
                    if (!players.TryGetValue(info.InitiatorPlayer.userID, out skill))
                    {
                        //      Debug.Log("[OnEntityTakeDamage] skill id:" + info.InitiatorPlayer.UserIDString + " mini-error");
                        return;
                    }
                    if (entity is BaseAnimalNPC)
                    {
                        info.damageTypes.ScaleAll(1f + MODGET(skill[SKILL.hunter].lvl, MODIFICATOR.ANIMALDAMAGE));
                    }
                    else if (entity is BaseHelicopter || entity is BradleyAPC)
                    {
                        info.damageTypes.ScaleAll(1f + MODGET(skill[SKILL.technicist].lvl, MODIFICATOR.MECHDAMAGE));
                        DAMAGETOMECH(entity, info.InitiatorPlayer, info.damageTypes.Total());
                    }
                    else if (entity is BasePlayer)
                    {
                        BasePlayer player = entity.ToPlayer();
                        if (player != null && IsNPC(player))
                        {
                            info.damageTypes.ScaleAll(1f + MODGET(skill[SKILL.marauder].lvl, MODIFICATOR.NPCDAMAGE));
                        }
                    }
                }
                else
                {
                    if (entity is BasePlayer)
                    {
                        BasePlayer player = entity.ToPlayer();
                        if (player != null && !IsNPC(player))
                        {
                            Dictionary<SKILL, mainskill> skill;
                            if (!players.TryGetValue(player.userID, out skill))
                            {
                                //     Debug.Log("[OnEntityTakeDamage] skill id:" + player.UserIDString + " mini-error");
                                return;
                            }
                            info.damageTypes.ScaleAll(1f - MODGET(skill[SKILL.marauder].lvl, MODIFICATOR.NPCARMOR));
                        }
                    }
                }
            }
            else if (entity is BasePlayer)
            {
                BasePlayer player = entity.ToPlayer();
                if (player == null || IsNPC(player)) return;
                Dictionary<SKILL, mainskill> skill;
                if (!players.TryGetValue(player.userID, out skill))
                {
                    //Debug.Log("[OnEntityTakeDamage] entity skill id:" + player.UserIDString + " mini-error");
                    return;
                }

                if (info.Initiator is BaseAnimalNPC)
                {
                    info.damageTypes.ScaleAll(1f - MODGET(skill[SKILL.hunter].lvl, MODIFICATOR.ANIMALARMOR));
                }
                else if (info.Initiator is BaseHelicopter || info.Initiator is BradleyAPC)
                {
                    info.damageTypes.ScaleAll(1f - MODGET(skill[SKILL.technicist].lvl, MODIFICATOR.MECHARMOR));
                }
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (IsNewPlayer(player))
            {
                player.ChatMessage(GetLang("chat_welcome", player.UserIDString));
            }

            foreach (var x in config.settings)
            {
                SetMaxLvl(player, x.Key.ToString());
            }

            UIRand(player);
        }

        private void UIRand(BasePlayer player)
        {
            if (!config.eXPBar.enable) return;
            int count = config.settings.Count;
            int rand = Random.Range(0, count);
            var x = config.settings.ElementAtOrDefault(rand);

            Dictionary<SKILL, mainskill> skill;
            if (!players.TryGetValue(player.userID, out skill))
            {
                IsNewPlayer(player);
                skill = players[player.userID];
            }

            int mylevel = skill[x.Key].lvl;

            mod level;
            if (!levels.TryGetValue(mylevel, out level)) level = levels.LastOrDefault().Value;

            UISkillBar(player, x.Key.ToString(), skill[x.Key].exp, level.exp, skill[x.Key].lvl);
            UPDATEMOD(player.UserIDString);
        }

        private void UPDATEMOD(string id)
        {
            modificator modificator;
            if (!modificators.TryGetValue(id, out modificator))
            {
                modificators.Add(id, new modificator { gather = GETGATHER(id), xp = GETBOOST(id) });
                return;
            }
            modificator.xp = GETBOOST(id);
            modificator.gather = GETGATHER(id);
        }

        private float GETBOOST(string id)
        {
            float value = 1f;
            foreach (var z in config.boosters)
            {
                if (permission.UserHasPermission(id, z.Key)) value *= z.Value;
            }
            return value;
        }

        private float GETGATHER(string id)
        {
            foreach (var z in config.gathers)
            {
                if (permission.UserHasPermission(id, z.Key)) return z.Value;
            }
            return 1f;
        }

        private string token = "030320221715fermens";
        private string namer = "SkillRates";

        private string UITEST = "[{\"name\":\"SkillBar\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.1753284 0.1753284 0.1753284 0.4970338\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 0\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 10\"}]},{\"name\":\"LTSK\",\"parent\":\"SkillBar\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"assets/content/textures/generic/fulltransparent.tga\",\"color\":\"1 0.9058824 0.2039216 0.7843137\",\"png\":\"{png}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0 0\",\"offsetmin\":\"2 -8\",\"offsetmax\":\"37 27\"}]},{\"name\":\"TextLTSK\",\"parent\":\"LTSK\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{lv}\",\"fontSize\":16,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ERrrq\",\"parent\":\"SkillBar\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{text}\",\"fontSize\":16, \"color\":\"1 1 1 0.7\",\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0 0\",\"offsetmin\":\"43 0\",\"offsetmax\":\"300 39\"}]},{\"name\":\"_bar\",\"parent\":\"SkillBar\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.2\",\"anchormax\":\"1 0.7\",\"offsetmin\":\"43 0\",\"offsetmax\":\"0 0\"}]}]";
        string barpanel = "{\"name\":\"barpanel\",\"parent\":\"_bar\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{color}\"},{\"type\":\"RectTransform\",\"anchormin\":\"{min} 0\",\"anchormax\":\"{max} 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]}";

        void DoBar(ref float x, ref string xx, float b = 0.095f)
        {
            float max = x + b;
            xx += (string.IsNullOrEmpty(xx) ? "" : ",") + barpanel.Replace("{color}", "1 0.9372549 0.4666667 " + max).Replace("{min}", x.ToString()).Replace("{max}", max.ToString());
            x = max + 0.005f;
        }

        private void UISkillBar(BasePlayer player, string key, float exp, float needexp, int lvl)
        {
            if (!config.eXPBar.enable) return;
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "SkillBar");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", UITEST.Replace("{text}", GetLang("expbar_main", player.UserIDString).Replace("{skillname}", GetLang(key, player.UserIDString)).Replace("{exp}", exp.ToString("0.0")).Replace("{needexp}", needexp.ToString("0.0"))).Replace("{png}", GetImage("https://i.ibb.co/jhXTp9f/b.png")).Replace("{lv}", lvl.ToString()));

            float calculeted = (exp / needexp) * 10;
            int rov = (int)Math.Floor(calculeted);
            float ost = (calculeted - rov) / 10f;

            float x = 0;
            string xx = "";

            for (int a = 0; a < rov; a++) DoBar(ref x, ref xx);

            if (ost > 0f) DoBar(ref x, ref xx, ost);

            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", "[" + xx + "]");
        }

        private void OnServerInitialized()
        {
            permission.RegisterPermission("skillrates.instant", this);
            ServerMgr.Instance.StartCoroutine(GetCallback());
        }

        #region START
        public string mainGui = "[{\"name\":\"TokenMenu\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"{anchormin}\",\"anchormax\":\"{anchormax}\",\"offsetmin\":\"{offsetmin}\",\"offsetmax\":\"{offsetmax}\"},{\"type\":\"NeedsCursor\"}]},{\"name\":\"TokenCLOSE2\",\"parent\":\"TokenMenu\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"close\":\"TokenMenu\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"-2000 -2000\",\"anchormax\":\"2000 2000\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"cui\",\"parent\":\"TokenMenu\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{backgroundcolor}\",\"fadeIn\":{fadein}},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TokenHEADER\",\"parent\":\"cui\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.7490196 0.7490196 0.7490196 0.1647059\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.93\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TokenHEADERTEXT\",\"parent\":\"TokenHEADER\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{header}\",\"fontSize\":26,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TokenCLOSE\",\"parent\":\"TokenHEADER\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"close\":\"TokenMenu\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.9 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TokenTEXT\",\"parent\":\"TokenCLOSE\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"fontSize\":16,\"color\":\"1 1 1 0.5\",\"text\":\"{closetext}\",\"align\":\"MiddleRight\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"-10 0\"}]}{main}]";
        public string mainGui2 = "[{\"name\":\"TokenMenu\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"{anchormin}\",\"anchormax\":\"{anchormax}\",\"offsetmin\":\"{offsetmin}\",\"offsetmax\":\"{offsetmax}\"},{\"type\":\"NeedsCursor\"}]},{\"name\":\"cui\",\"parent\":\"TokenMenu\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{backgroundcolor}\",\"fadeIn\":{fadein}},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]}{main}]";
        public string mainBar = ",{\"name\":\"MAIN\",\"parent\":\"cui\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"{minx} {miny}\",\"anchormax\":\"{maxx} {maxy}\",\"offsetmax\":\"0 0\"}]},{\"name\":\"MAINBAR\",\"parent\":\"MAIN\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{backbar}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.055 0.09\",\"anchormax\":\"0.945 0.235\",\"offsetmax\":\"0 0\"}]},{\"name\":\"MAIIMAGE\",\"parent\":\"MAIN\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"assets/content/textures/generic/fulltransparent.tga\",\"png\":\"{png}\",\"color\":\"1 1 1 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]},{\"name\":\"NAMEFRAG\",\"parent\":\"MAIN\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{NAME}\",\"fontSize\":26,\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.02 0.7\",\"anchormax\":\"1 0.98\",\"offsetmin\":\"10 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"LEVEL\",\"parent\":\"MAIN\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{LVL}/{MAXLVL}\",\"fontSize\":26,\"align\":\"MiddleRight\",\"color\":\"{lvlcolor}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.6\",\"anchormax\":\"0.98 1\",\"offsetmax\":\"-10 0\"}]},{\"name\":\"ACTIVEBAR\",\"parent\":\"MAINBAR\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{mainbar}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"{bar} 1\",\"offsetmax\":\"0 0\"}]},{\"name\":\"BONUSE\",\"parent\":\"MAIN\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{info}\",\"align\":\"MiddleLeft\",\"fontSize\":14,\"color\":\"1 1 1 0.787812\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.07 0.25\",\"anchormax\":\"1 0.65\",\"offsetmin\":\"10 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"SMALLINFO\",\"parent\":\"MAIN\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{smallinfo}\",\"fontSize\":12,\"align\":\"LowerLeft\",\"color\":\"1 1 1 0.4\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.02 0.68\",\"anchormax\":\"1 0.8\",\"offsetmin\":\"10 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"EXP\",\"parent\":\"MAIN\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{exp}\",\"fontSize\":12,\"align\":\"LowerLeft\",\"color\":\"1 1 1 0.4\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.07 0.127\",\"anchormax\":\"0.5 0.25\",\"offsetmin\":\"5 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"MAXEXP\",\"parent\":\"MAIN\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{maxexp}\",\"fontSize\":12,\"align\":\"LowerRight\",\"color\":\"1 1 1 0.4\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.127\",\"anchormax\":\"0.92 0.25\",\"offsetmax\":\"-5 0\"}]}";
        public string mainBar2 = ",{\"name\":\"MAIN\",\"parent\":\"cui\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"{minx} {miny}\",\"anchormax\":\"{maxx} {maxy}\",\"offsetmax\":\"0 0\"}]},{\"name\":\"MAINBAR\",\"parent\":\"MAIN\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{backbar}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.055 0.09\",\"anchormax\":\"0.945 0.235\",\"offsetmax\":\"0 0\"}]},{\"name\":\"MAIIMAGE\",\"parent\":\"MAIN\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"assets/content/textures/generic/fulltransparent.tga\",\"png\":\"{png}\",\"color\":\"1 1 1 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]},{\"name\":\"NAMEFRAG\",\"parent\":\"MAIN\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{NAME}\",\"fontSize\":18,\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.02 0.7\",\"anchormax\":\"1 0.98\",\"offsetmin\":\"10 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"LEVEL\",\"parent\":\"MAIN\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{LVL}/{MAXLVL}\",\"fontSize\":18,\"align\":\"MiddleRight\",\"color\":\"{lvlcolor}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.7\",\"anchormax\":\"0.98 1\",\"offsetmax\":\"-10 0\"}]},{\"name\":\"ACTIVEBAR\",\"parent\":\"MAINBAR\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{mainbar}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"{bar} 1\",\"offsetmax\":\"0 0\"}]},{\"name\":\"BONUSE\",\"parent\":\"MAIN\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{info}\",\"align\":\"MiddleLeft\",\"fontSize\":10,\"color\":\"1 1 1 0.787812\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.07 0.25\",\"anchormax\":\"1 0.65\",\"offsetmin\":\"10 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"SMALLINFO\",\"parent\":\"MAIN\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{smallinfo}\",\"fontSize\":8,\"align\":\"LowerLeft\",\"color\":\"1 1 1 0.4\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.02 0.68\",\"anchormax\":\"1 0.8\",\"offsetmin\":\"10 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"EXP\",\"parent\":\"MAIN\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{exp}\",\"fontSize\":8,\"align\":\"LowerLeft\",\"color\":\"1 1 1 0.4\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.07 0.127\",\"anchormax\":\"0.5 0.25\",\"offsetmin\":\"5 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"MAXEXP\",\"parent\":\"MAIN\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{maxexp}\",\"fontSize\":8,\"align\":\"LowerRight\",\"color\":\"1 1 1 0.4\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.127\",\"anchormax\":\"0.92 0.25\",\"offsetmax\":\"-5 0\"}]}";
        public Dictionary<string, string> messagesRU = new Dictionary<string, string>
        {
            { "miner", "ШАХТЕР" },
            { "miner_hint", "добывай камень и железную руду" },
            { "miner_description", "<color=#5da86c>x{0}</color> добыча камня\n<color=#5da86c>x{1}</color> добыча железной руды\n<color=#5da86c>x{2}</color> добыча высококачественной железной руды" },

            { "alchemist", "АЛХИМИК" },
            { "alchemist_hint", "добывай сульфурную руду" },
            { "alchemist_description", "<color=#5da86c>x{0}</color> добыча сульфура\n<color=#5da86c>x{1}</color> скорость переплавки в печах" },

            { "woodcutter", "ДРОВОСЕК" },
            { "woodcutter_hint", "добывай дерево" },
            { "woodcutter_description_new", "<color=#5da86c>x{0}</color> добыча дерева\n<color=#5da86c>{1}%</color> шанс угля с дерева\n<color=#5da86c>x{2}</color> cкорость производства в НПЗ" },

            { "hunter", "ОХОТНИК" },
            { "hunter_hint", "убивай животных и потроши их" },
            { "hunter_description", "<color=#b76c6c>x{0}</color> получаемые ресурсы при потрошении\n<color=#b76c6c>{1}%</color> уменьшение урона от животных\n<color=#b76c6c>{2}%</color> увеличение урона по животным" },

            { "marauder", "МАРОДЕР" },
            { "marauder_hint", "убивай ботов и лутай их" },
            { "marauder_description", "<color=#b76c6c>x{0}</color> лут с ботов\n<color=#b76c6c>{1}%</color> уменьшение урона от ботов\n<color=#b76c6c>{2}%</color> увеличение урона по ботам" },

            { "technicist", "ТЕХНИК" },
            { "technicist_hint", "уничтожай технику и лутай ее" },
            { "technicist_description", "<color=#b76c6c>x{0}</color> лут с танка и вертолета\n<color=#b76c6c>{1}%</color> уменьшение урона от танка и вертолета\n<color=#b76c6c>{2}%</color> увеличение урона по танку и вертолету" },

            { "jeweler", "ЮВЕЛИР" },
            { "jeweler_hint", "облутывай аирдропы, закрытые и элитные ящики" },
            { "jeweler_description", "<color=#7773b4>x{0}</color> лут с аирдропов\n<color=#7773b4>x{1}</color> лут с закрытых ящиков\n<color=#7773b4>x{2}</color> лут с элитных ящиков" },

            { "dustman", "МУСОРЩИК" },
            { "dustman_hint", "лутай бочки, мусорки и обычные ящики" },
            { "dustman_description", "<color=#7773b4>x{0}</color> лут с бочек\n<color=#7773b4>x{1}</color> лут с обычных ящиков и мусорок" },

            { "farmer", "ФЕРМЕР" },
            { "farmer_hint", "собирай урожай" },
            { "farmer_description", "<color=#7773b4>x{0}</color> урожай овощей\n<color=#7773b4>x{1}</color> урожай ягод" },

            { "ui_exit", "ЗАКРЫТЬ" },
            { "ui_header", "НАВЫКИ" },

            { "expbar_main", "{skillname} ◉ EXP {exp}/{needexp}" },

            { "chat_uplevel", "Навык <color=#ccff66>{skill}</color> прокачался до <color=#ccff66>{level} ур.</color>\nВаши новые бонусы:\n{bonuses}\n\n<color=#ccff66>/skill</color> - список доступных навыков и их бонусы" },
            { "chat_welcome", "Добро пожаловать на сервер.\nНа сервере присутствует система прокачки навыков, команда <color=#ccff66>/skill</color>." },
            { "chat_broadcast", "<size=11>Игрок <color=#ccff99>{name}</color> повысил навык <color=#ccff66>{skill}</color> до <color=#ccff66>{level} ур.</color>\n<color=#ccff66>/skill</color></size>" }
        };

        public Dictionary<string, string> messagesEN = new Dictionary<string, string>
        {
            { "miner", "MINER" },
            { "miner_hint", "mine stone and iron ore" },
            { "miner_description", "<color=#5da86c>x{0}</color> stone mining\n<color=#5da86c>x{1}</color> iron ore mining\n<color=#5da86c>x{2}</color> mining of high quality iron ore" },

            { "alchemist", "ALCHEMIST" },
            { "alchemist_hint", "mine sulfur ore" },
            { "alchemist_description", "<color=#5da86c>x{0}</color> sulfur mining\n<color=#5da86c>x{1}</color> melting speed in furnaces" },

            { "woodcutter", "WOODCUTTER" },
            { "woodcutter_hint", "chop down trees" },
            { "woodcutter_description_new", "<color=#5da86c>x{0}</color> wood mining\n<color=#5da86c>{1}%</color> chance of coal from a tree\n<color=#5da86c>x{2}</color> refinery production speed" },

            { "hunter", "HUNTER" },
            { "hunter_hint", "kill animals and fleshed them" },
            { "hunter_description", "<color=#b76c6c>x{0}</color> resource from fleshed animals\n<color=#b76c6c>{1}%</color> animal damage reduction\n<color=#b76c6c>{2}%</color> increased damage to animals" },

            { "marauder", "MARAUDER" },
            { "marauder_hint", "kill bots and loot them" },
            { "marauder_description", "<color=#b76c6c>x{0}</color> loot from bots\n<color=#b76c6c>{1}%</color> bot damage reduction\n<color=#b76c6c>{2}%</color> increased damage against bots" },

            { "technicist", "TECHNICIAN" },
            { "technicist_hint", "destroy bradley and helicopter and loot it" },
            { "technicist_description", "<color=#b76c6c>x{0}</color> loot from tank and helicopter\n<color=#b76c6c>{1}%</color> reduced damage from tanks and helicopters\n<color=#b76c6c>{2}%</color> increased damage to tanks and helicopters" },

            { "jeweler", "JEWELER" },
            { "jeweler_hint", "loot airdrops, elite and locked crates" },
            { "jeweler_description", "<color=#7773b4>x{0}</color> loot from supply drops\n<color=#7773b4>x{1}</color> loot from locked boxes\n<color=#7773b4>x{2}</color> loot from elite crates" },

            { "dustman", "SCAVENGER" },
            { "dustman_hint", "loot barrels, trash cans and regular crates" },
            { "dustman_description", "<color=#7773b4>x{0}</color> loot from barrels\n<color=#7773b4>x{1}</color> loot from regular crates and trash cans" },

            { "farmer", "FARMER" },
            { "farmer_hint", "plant and harvest" },
            { "farmer_description", "<color=#7773b4>x{0}</color> crop of vegetables\n<color=#7773b4>x{1}</color> berry harvest" },

            { "ui_exit", "CLOSE" },
            { "ui_header", "SKILLS" },

            { "expbar_main", "{skillname} ◉ EXP {exp}/{needexp}" },

            { "chat_uplevel", "The skill <color=#ccff66>{skill}</color> has been upgraded to <color=#ccff66>{level} lvl</color>\nYour new bonuses:\n{bonuses}\n\n<color=#ccff66>/skill</color> - list of available skills and their bonuses" },
            { "chat_welcome", "Welcome to the server.\nThere is a skill leveling system on the server, command <color=#ccff66>/skill</color>." },
            { "chat_broadcast", "<size=11>Player <color=#ccff99>{name}</color> upgraded <color=#ccff66>{skill}</color> to <color=#ccff66>{level} lvl</color>\n<color=#ccff66>/skill</color></size>" }
        };

        IEnumerator GetCallback()
        {
            yield return CoroutineEx.waitForSeconds(1f);

            if (!ImageLibrary)
            {
                PrintWarning(fermensEN ? "Image Library not found!" : "Image Library не обнаружен, отгружаем Панель.");
                Interface.Oxide.UnloadPlugin(Name);
                yield break;
            }

            if (!config.settings.ContainsKey(SKILL.farmer))
            {
                config.settings.Add(SKILL.farmer, new setting { lvlcolor = "0.756 0.75 0.87 1", mainbar = "0.26 0.25 0.46 1", backbar = "0.16 0.16 0.16 1", mod = new Dictionary<MODIFICATOR, setting2> { { MODIFICATOR.GROWABLEVEGETABLES, new setting2 { first = 1f, last = 5f } }, { MODIFICATOR.GROWABLEBERRY, new setting2 { first = 1f, last = 5f } } } });
                SaveConfig();
            }

            if (config.exp.grow_vegetables == 0f)
            {
                config.exp.grow_berrys = 0.75f;
                config.exp.grow_vegetables = 0.5f;
                SaveConfig();
            }

            if (config.exp.planting_berrys == 0f)
            {
                config.exp.planting_berrys = 0.2f;
                config.exp.planting_vegetables = 0.1f;
                SaveConfig();
            }

            lang.RegisterMessages(messagesEN, this, "en");
            lang.RegisterMessages(messagesRU, this, "ru");


            if (config.uI.backgroundcolor == null) config.uI.backgroundcolor = "0 0 0 0.9960784";

            if (!config.settings[SKILL.hunter].mod.ContainsKey(MODIFICATOR.ANIMALDAMAGE))
            {
                config.settings[SKILL.hunter].mod.Add(MODIFICATOR.ANIMALDAMAGE, new setting2 { first = 0f, last = 0.25f });
            }

            if (!config.settings[SKILL.woodcutter].mod.ContainsKey(MODIFICATOR.REFINERYSPEED))
            {
                config.settings[SKILL.woodcutter].mod.Add(MODIFICATOR.REFINERYSPEED, new setting2 { first = 5f, last = 20f });
            }

            SaveConfig();
            foreach (var z in config.gathers) permission.RegisterPermission(z.Key, this);
            foreach (var z in config.boosters) permission.RegisterPermission(z.Key, this);

            maxlevelstring = config.lvl.maxlevel.ToString();

            levels.Clear();
            for (int i = 0; i <= config.lvl.maxlevel; i++)
            {
                mod level;
                if (!levels.TryGetValue(i - 1, out level)) level = new mod();
                int nextlevel = i + 1;
                Dictionary<MODIFICATOR, float> modificator = new Dictionary<MODIFICATOR, float>();
                foreach (var z in config.settings)
                {
                    foreach (var x in z.Value.mod) modificator[x.Key] = x.Value.first + ((x.Value.last - x.Value.first) / config.lvl.maxlevel) * i;
                }
                levels.Add(i, new mod { exp = config.lvl.modificator_start * config.lvl.modificator_multiplication * nextlevel + level.exp, modificator = modificator });
            }

            foreach (var x in config.settings)
            {
                if(x.Value.reward == null)
                {
                    x.Value.reward = new Dictionary<int, List<string>>
                    {
                        { 10, new List<string> { "addgroup {steamid} viptest 1h", "grantperm {steamid} skillrates.x2boost 3h" } },
                        { 20, new List<string> { "grantperm {steamid} skillrates.x2boost 3h" } },
                        { 30, new List<string> { "grantperm {steamid} skillrates.x2boost 3h" } },
                        { 40, new List<string> { "grantperm {steamid} skillrates.x2boost 3h" } },
                        { 50, new List<string> { "grantperm {steamid} skillrates.x2boost 3h" } },
                        { 60, new List<string> { "grantperm {steamid} skillrates.x2boost 3h" } },
                        { 70, new List<string> { "grantperm {steamid} skillrates.x2boost 3h" } },
                        { 80, new List<string> { "grantperm {steamid} skillrates.x2boost 3h" } },
                        { 90, new List<string> { "grantperm {steamid} skillrates.x2boost 3h" } },
                    };
                }
                string kk = x.Key.ToString();
                if (x.Value.background == null || x.Value.background.Contains("foxplugins"))
                {
                    if (kk == "alchemist") x.Value.background = "https://i.ibb.co/xSrFSfL/alchemist.png";
                    else if (kk == "dustman") x.Value.background = "https://i.ibb.co/qmJh6wv/dustman.png"; 
                    else if (kk == "farmer") x.Value.background = "https://i.ibb.co/TwCxMyj/farmer.png";
                    else if (kk == "hunter") x.Value.background = "https://i.ibb.co/MG065LZ/hunter.png";
                    else if (kk == "jeweler") x.Value.background = "https://i.ibb.co/gFWK42T/jeweler.png";
                    else if (kk == "marauder") x.Value.background = "https://i.ibb.co/9hJ2H6N/marauder.png";
                    else if (kk == "miner") x.Value.background = "https://i.ibb.co/WxWf2JN/miner.png";
                    else if (kk == "technicist") x.Value.background = "https://i.ibb.co/NjWJTfZ/technicist.png";
                    else if (kk == "woodcutter") x.Value.background = "https://i.ibb.co/KjRB9xt/woodcutter.png";
                }
                permission.RegisterPermission("skillrates." + x.Key, this);
                AddImage(x.Value.background, x.Value.background);
            }
            AddImage($"https://i.ibb.co/jhXTp9f/b.png", $"https://i.ibb.co/jhXTp9f/b.png"); 
            if (string.IsNullOrEmpty(config.chatcommand)) config.chatcommand = "skill";
            SaveConfig();
            MAINBAR = mainBar;
            if (ConVar.Server.ip == "37.230.228.232" /* MOD WITHOUT EXIT BUTTON SPEACIAL FOR IN BUILT IN MENU"78.46.56.22  37.230.228.232"*/)
            {
                MAINGUI = mainGui2.Replace(",{\"name\":\"TokenHEADER\",\"parent\":\"cui\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.7490196 0.7490196 0.7490196 0.1647059\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.93\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TokenHEADERTEXT\",\"parent\":\"TokenHEADER\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{header}\",\"fontSize\":26,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TokenCLOSE\",\"parent\":\"TokenHEADER\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"close\":\"TokenMenu\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.9 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TokenTEXT\",\"parent\":\"TokenCLOSE\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"fontSize\":16,\"color\":\"1 1 1 0.5\",\"text\":\"{closetext}\",\"align\":\"MiddleRight\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"-10 0\"}]}", "");
                MAINBAR = mainBar2;
            }
            else
            {
                MAINGUI = mainGui;
            }

            MAINGUI = MAINGUI.Replace("{backgroundcolor}", config.uI.backgroundcolor).Replace("{fadein}", config.uI.fadein).Replace("{anchormin}", config.uI.anchor_min).Replace("{anchormax}", config.uI.anchor_max).Replace("{offsetmin}", config.uI.offset_min).Replace("{offsetmax}", config.uI.offset_max);

            Interface.Oxide.GetLibrary<ru.Libraries.Command>(null).AddChatCommand(config.chatcommand, this, "cmdskill");

            foreach (var z in BasePlayer.activePlayerList) OnPlayerConnected(z);

            Debug.Log("[Initialization successful] >>SkillRates<< [fermens#8767]");

            yield break;
        }
        #endregion

        void OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
        {
            if (collectible == null || collectible.itemList.Count() == 0 || player == null || player.IsNpc) return;

            foreach (var item in collectible.itemList)
            {
                if (item == null) continue;
                BONUSE(player, item, true, true);
            }
        }
        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            BONUSE(player, item, true, true);
        }

        private void OnGrowableGather(GrowableEntity plant, Item item, BasePlayer player)
        {
            BONUSE(player, item, true, true);
        }

        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            if (player == null) return;
            BONUSE(player, item);
        }

        private void OnExcavatorGather(ExcavatorArm arm, Item item)
        {
            item.amount = (int)(item.amount * config.carier);
        }

        private void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            item.amount = (int)(item.amount * config.carier);
        }

        void BONUSE(BasePlayer player, Item item, bool grab = false, bool grow = false)
        {
            if (player.userID < 999999) return;
            Dictionary<SKILL, mainskill> skill;
            if (!players.TryGetValue(player.userID, out skill)) return;
   
            item.amount = AMOUNTBOUNSE(player, skill, item.amount, item.info.itemid, item.info.shortname, grab, grow);
        }

        void BONUSE(BasePlayer player, ItemAmount item, bool grab = false, bool grow = false)
        {
            if (player.userID < 999999) return;

            Dictionary<SKILL, mainskill> skill;
            if (!players.TryGetValue(player.userID, out skill)) return;
            
            item.amount = AMOUNTBOUNSE(player, skill, item.amount, item.itemDef.itemid, item.itemDef.shortname, grab, grow);
        }

        int AMOUNTBOUNSE(BasePlayer player, Dictionary<SKILL, mainskill> skill, float amount, int itemid, string itemname, bool grab = false, bool grow = false)
        {
            if (itemid == -2099697608) // КАМЕНЬ
            {
                if (grab) ADDEXP(player, config.exp.grab_stone, SKILL.miner);
                return GATHERUP(player.UserIDString, amount, MODGET(skill[SKILL.miner].lvl, MODIFICATOR.STONE));
            }
            else if (itemid == -4031221) // МЕТАЛ
            {
                if (grab) ADDEXP(player, config.exp.grab_metal, SKILL.miner);
                return GATHERUP(player.UserIDString, amount, MODGET(skill[SKILL.miner].lvl, MODIFICATOR.METAL));
            }
            else if (itemid == -1982036270) // МВК
            {
                return GATHERUP(player.UserIDString, amount, MODGET(skill[SKILL.miner].lvl, MODIFICATOR.HQM));
            }
            else if (itemid == -1157596551) // СУЛЬФУР
            {
                if (grab) ADDEXP(player, config.exp.grab_sulfur, SKILL.alchemist);
                return GATHERUP(player.UserIDString, amount, MODGET(skill[SKILL.alchemist].lvl, MODIFICATOR.SULFUR));
            }
            else if (itemid == -151838493) // ДЕРЕВО
            {
                if (grab) ADDEXP(player, config.exp.grab_wood, SKILL.woodcutter);
                return GATHERUP(player.UserIDString, amount, MODGET(skill[SKILL.woodcutter].lvl, MODIFICATOR.WOOD));
            }
            else if (itemid == 1568388703)
            {
                if (grab) ADDEXP(player, config.exp.barrel, SKILL.dustman);
                return GATHERUP(player.UserIDString, amount, MODGET(skill[SKILL.dustman].lvl, MODIFICATOR.BARREL));
            }
            else if (itemname.Contains(".berry"))
            {
                if (grab) ADDEXP(player, config.exp.grow_berrys, SKILL.farmer);
                return GATHERUP(player.UserIDString, amount, MODGET(skill[SKILL.farmer].lvl, MODIFICATOR.GROWABLEBERRY));
            }
            else if (grow)
            {
                if (grab) ADDEXP(player, config.exp.grow_vegetables, SKILL.farmer);
                return GATHERUP(player.UserIDString, amount, MODGET(skill[SKILL.farmer].lvl, MODIFICATOR.GROWABLEVEGETABLES));
            }
            else
            {
                return GATHERUP(player.UserIDString, amount, MODGET(skill[SKILL.hunter].lvl, MODIFICATOR.ANIMAL));
            }
        }

        int GATHERUP(string id, float amount, float mod)
        {
            modificator modificator;
            if (!modificators.TryGetValue(id, out modificator)) modificator = new modificator { gather = 1f };
            int amountI = (int)(amount * mod * modificator.gather);
            float amountF = (float)(amount * mod * modificator.gather);
            float rand = amountF - amountI;
            float random = Random.Range(0f, 1f);
            if (rand > 0f && rand > random) amountI += 1;
           // Debug.Log($"M {mod} F {amountF} I {amountI} R {rand}<->{random}");
            return amountI;
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            BONUSE(player, item);
            if (item.info.itemid != -1982036270)
            {
                if (dispenser.gatherType == ResourceDispenser.GatherType.Tree) ADDEXP(player, config.exp.wood, SKILL.woodcutter);
                else if (dispenser.name.Contains(prefabmetal)) ADDEXP(player, config.exp.ore_metal, SKILL.miner);
                else if (dispenser.name.Contains(prefabsulfur)) ADDEXP(player, config.exp.ore_sulfur, SKILL.alchemist);
                else if (dispenser.name.Contains(prefabstone)) ADDEXP(player, config.exp.ore_stone, SKILL.miner);
            }
        }

        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            Dictionary<SKILL, mainskill> skill;
            if (!players.TryGetValue(player.userID, out skill))
            {
             //   Debug.Log("[OnLootEntity] skill id:" + player.UserIDString + " mini-error");
                return;
            }
            if (entity is NPCPlayerCorpse)
            {
                if (entity.HasFlag(BaseEntity.Flags.Reserved7)) return;
                ItemContainer cont = entity.GetComponent<NPCPlayerCorpse>().containers.FirstOrDefault();
                foreach (var item in cont.itemList.Where(x => x.info.stackable > 1))
                {
                    item.amount = GATHERUP(player.UserIDString, item.amount, MODGET(skill[SKILL.marauder].lvl, MODIFICATOR.NPC));
                }
                ADDEXP(player, config.exp.npc_looted, SKILL.marauder);
                entity.SetFlag(BaseEntity.Flags.Reserved7, true);
            }
            else if (entity is LootContainer)
            {
                LootContainer lootcont = entity.GetComponent<LootContainer>();
                if (lootcont == null || lootcont.HasFlag(BaseEntity.Flags.Reserved7)) return;
                if (entity.prefabID == 1737870479) // танк-ящик
                {
                    UPRATELOOT(player, lootcont, MODGET(skill[SKILL.technicist].lvl, MODIFICATOR.MECH));
                    ADDEXP(player, config.exp.tank_cont, SKILL.technicist);
                }
                else if (entity.prefabID == 1314849795) // верт-ящик
                {
                    UPRATELOOT(player, lootcont, MODGET(skill[SKILL.technicist].lvl, MODIFICATOR.MECH));
                    ADDEXP(player, config.exp.heli_cont, SKILL.technicist);
                }
                else if (entity.prefabID == 3286607235) // элит-ящик
                {
                    UPRATELOOT(player, lootcont, MODGET(skill[SKILL.jeweler].lvl, MODIFICATOR.ELITE));
                    ADDEXP(player, config.exp.elite, SKILL.jeweler);
                }
                else if (entity is HackableLockedCrate || entity is LockedByEntCrate)
                {
                    UPRATELOOT(player, lootcont, MODGET(skill[SKILL.jeweler].lvl, MODIFICATOR.LOCKEDCRATE));
                    ADDEXP(player, config.exp.lockedcrate, SKILL.jeweler);
                }
                else if (entity is SupplyDrop)
                {
                    UPRATELOOT(player, lootcont, MODGET(skill[SKILL.jeweler].lvl, MODIFICATOR.AIR));
                    ADDEXP(player, config.exp.air, SKILL.jeweler);
                }
                else if(lootcont.OwnerID == 0)
                {
                    UPRATELOOT(player, lootcont, MODGET(skill[SKILL.dustman].lvl, MODIFICATOR.CRATE));
                    ADDEXP(player, config.exp.crate, SKILL.dustman);
                }
                // Debug.Log(entity.prefabID + " " + entity.PrefabName);
                lootcont.SetFlag(BaseEntity.Flags.Reserved7, true);
            }
        }

        private void UPRATELOOT(BasePlayer player, LootContainer lootContainer, float rateup)
        {
            foreach (var item in lootContainer.inventory.itemList)
            {
                if (item.IsBlueprint() || item.MaxStackable() == 1) continue;
                int amount = GATHERUP(player.UserIDString, item.amount, rateup);
                if (item.info.itemid == 1248356124) amount /= 2;
                if (amount < 1) amount = 1;
                item.amount = amount;
            }
        }

        float MODGET(int lvl, MODIFICATOR mODIFICATOR)
        {
            mod mod;
            if (!levels.TryGetValue(lvl, out mod)) return levels.LastOrDefault().Value.modificator[mODIFICATOR];
            return mod.modificator[mODIFICATOR];
        }

        private bool IsNPC(BasePlayer player)
        {
            if (player is NPCPlayer) return true;
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L)) return true;
            return false;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BaseHelicopter)
            {
                if (config.loottoheli.enable)
                {
                    for (int index = 0; index < config.loottoheli.amount; ++index)
                    {
                        Vector3 onUnitSphere = UnityEngine.Random.onUnitSphere;
                        onUnitSphere.y = 0.0f;
                        onUnitSphere.Normalize();
                        Vector3 pos = entity.transform.position + new Vector3(0.0f, 1.5f, 0.0f) + onUnitSphere * UnityEngine.Random.Range(2f, 3f);
                        BaseEntity entity1 = GameManager.server.CreateEntity("assets/prefabs/npc/m2bradley/bradley_crate.prefab", pos, Quaternion.LookRotation(onUnitSphere), true);
                        entity1.Spawn();
                        LootContainer lootContainer = entity1 as LootContainer;
                        if ((bool)((UnityEngine.Object)lootContainer))
                            lootContainer.Invoke(new Action(lootContainer.RemoveMe), 1800f);
                        Collider component = entity1.GetComponent<Collider>();
                        Rigidbody rigidbody = entity1.gameObject.AddComponent<Rigidbody>();
                        rigidbody.useGravity = true;
                        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                        rigidbody.mass = 2f;
                        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                        rigidbody.velocity = Vector3.zero + onUnitSphere * UnityEngine.Random.Range(1f, 3f);
                        rigidbody.angularVelocity = Vector3Ex.Range(-1.75f, 1.75f);
                        rigidbody.drag = (float)(0.5 * ((double)rigidbody.mass / 5.0));
                        rigidbody.angularDrag = (float)(0.200000002980232 * ((double)rigidbody.mass / 5.0));
                        FireBall entity2 = GameManager.server.CreateEntity("assets/bundled/prefabs/oilfireballsmall.prefab", new Vector3(), new Quaternion(), true) as FireBall;
                        if ((bool)((UnityEngine.Object)entity2))
                        {
                            entity2.SetParent(entity1, false, false);
                            entity2.Spawn();
                            entity2.GetComponent<Rigidbody>().isKinematic = true;
                            entity2.GetComponent<Collider>().enabled = false;
                        }
                        
                        entity1.SendMessage("SetLockingEnt", (object)entity2.gameObject, SendMessageOptions.DontRequireReceiver);
                    }
                }
                MECHEXP(entity, config.exp.heli);
                return;
            }
            BasePlayer initiator = info?.InitiatorPlayer;
            if (initiator != null)
            {
                if (IsNPC(initiator)) return;
               // Debug.Log(entity.prefabID + " " + entity.PrefabName);
                if (entity is BasePlayer)
                {
                    BasePlayer player = entity.ToPlayer();
                    if (player == null) return;
                    if (IsNPC(player))
                    {
                        ADDEXP(initiator, config.exp.npc, SKILL.marauder);
                    }
                }
                else if (entity is Chicken)
                {
                    ADDEXP(initiator, config.exp.chicken, SKILL.hunter);
                }
                else if (entity is Stag)
                {
                    ADDEXP(initiator, config.exp.stag, SKILL.hunter);
                }
                else if (entity is Horse)
                {
                    ADDEXP(initiator, config.exp.horse, SKILL.hunter);
                }
                else if (entity is RidableHorse)
                {
                    ADDEXP(initiator, config.exp.ridablehorse, SKILL.hunter);
                }
                else if (entity is Bear || entity is Polarbear)
                {
                    ADDEXP(initiator, config.exp.bear, SKILL.hunter);
                }
                else if (entity is Wolf)
                {
                    ADDEXP(initiator, config.exp.wolf, SKILL.hunter);
                }
                else if (entity is Boar)
                {
                    ADDEXP(initiator, config.exp.boar, SKILL.hunter);
                }
                else if (entity is BradleyAPC)
                {
                    MECHEXP(entity, config.exp.tank);
                }
                else if (entity.prefabID == 4214400966) // тушка танка
                {
                    ADDEXP(initiator, config.exp.tank_fleshed, SKILL.technicist);
                }
                else if (entity.prefabID == 1829321077) // тушка вертолета
                {
                    ADDEXP(initiator, config.exp.heli_fleshed, SKILL.technicist);
                }
                else if (entity.prefabID == 4107384580) // тушка волка
                {
                    ADDEXP(initiator, config.exp.wolf_fleshed, SKILL.hunter);
                }
                else if (entity.prefabID == 3307373733) // тушка кабанчика
                {
                    ADDEXP(initiator, config.exp.wolf_fleshed, SKILL.hunter);
                }
                else if (entity.prefabID == 345706504) // тушка курочки
                {
                    ADDEXP(initiator, config.exp.chicken_fleshed, SKILL.hunter);
                }
                else if (entity.prefabID == 4102891990 || entity.prefabID == 2275652760) // тушка мишки
                {
                    ADDEXP(initiator, config.exp.bear_fleshed, SKILL.hunter);
                }
                else if (entity.prefabID == 2898915566) // тушка коня
                {
                    ADDEXP(initiator, config.exp.horse_fleshed, SKILL.hunter);
                }
                else if (entity.prefabID == 784238137) // тушка оленя
                {
                    ADDEXP(initiator, config.exp.stag_fleshed, SKILL.hunter);
                }
                //Debug.Log(entity.name + " " + entity.prefabID);
            }
        }

        void MECHEXP(BaseCombatEntity entity, float maxexp)
        {
            Dictionary<BasePlayer, float> damages;
            if (!mechdamage.TryGetValue(entity, out damages)) return;
            float totaldamage = damages.Sum(x => x.Value);
            foreach (var z in damages)
            {
                if (z.Key == null || !z.Key.IsConnected) continue;
                float exp = (z.Value / totaldamage) * maxexp;
                ADDEXP(z.Key, exp, SKILL.technicist);
            }
            mechdamage.Remove(entity);
        }

        [ConsoleCommand("set.lvl")]
        void Dosetsss(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;
            if (!arg.HasArgs())
            {
                arg.ReplyWith(fermensEN ? "set.lvl steamid lvl" : "set.lvl ник уровень");
                return;
            }

            int lvl;
            if (!int.TryParse(arg.Args[1], out lvl) || lvl <= 0 || lvl > 100)
            {
                arg.ReplyWith(fermensEN ? "LVL?" : "УРОВЕНЬ?!");
                return;
            }

            ulong steamid;
            if (!ulong.TryParse(arg.Args[0], out steamid))
            {
                arg.ReplyWith("STEAMID?!");
                return;
            }
            BasePlayer player = BasePlayer.FindByID(steamid);
            if (player == null)
            {
                arg.ReplyWith("player = null!");
                return;
            }

            int skl;
            if (!int.TryParse(arg.Args[2], out skl) || skl < 0 || skl > 7)
            {
                arg.ReplyWith("0 - miner, 1 - alchemist, 2 - woodcutter, 3 - hunter, 4 - marauder, 5 - technicist, 6 - jeweler, 7 - dustman");
                return;
            }

            SKILL sKILL = (SKILL)skl;
            Dictionary<SKILL, mainskill> skill;
            if (!players.TryGetValue(steamid, out skill)) return;
            mainskill mainskill;
            if (!skill.TryGetValue(sKILL, out mainskill))
            {
                AddSkills(steamid);
                mainskill = players[steamid][sKILL];
            }
            arg.ReplyWith($"COOL ^^");

            string nextstring = mainskill.lvl.ToString();
            setting setting = config.settings[sKILL];

            for (int i = 0; i < lvl; i++)
            {
                mainskill.lvl++;
                RewardPlayer(player.UserIDString, mainskill.lvl, setting);
            }
            
            string minername = sKILL.ToString();
            NotifyAll(player.displayName, nextstring, minername);
            string bonuses = GetLang(minername+"_description" + (minername == "woodcutter" ? "_new" : ""), player.UserIDString).Replace("{0}", setting.mod.Count > 0 ? MOD(player.UserIDString, levels[mainskill.lvl], setting.mod.ElementAt(0).Key) : "").Replace("{1}", setting.mod.Count > 1 ? MOD(player.UserIDString, levels[mainskill.lvl], setting.mod.ElementAt(1).Key) : "").Replace("{2}", setting.mod.Count > 2 ? MOD(player.UserIDString, levels[mainskill.lvl], setting.mod.ElementAt(2).Key) : "");
            NotifyPlayer(player, bonuses, nextstring, minername);
        }

        private void DelMaxLvl(string userid, string minername)
        {
            ulong _userid = Convert.ToUInt64(userid);
            if (!players.ContainsKey(_userid)) AddDL(userid);
            Dictionary<SKILL, mainskill> user = players[_userid];
            if (user == null || user.Count == 0) return;
            SKILL sKILL = (SKILL)Enum.Parse(typeof(SKILL), minername, true);
            Dictionary<SKILL, mainskill> skill;
            if (!players.TryGetValue(_userid, out skill)) return;
            mainskill mainskill;
            if (!skill.TryGetValue(sKILL, out mainskill))
            {
                AddSkills(_userid);
                mainskill = players[_userid][sKILL];
            }
            setting setting = config.settings[sKILL];
            mainskill.lvl = mainskill.lastlvl;
            mainskill.lastlvl = 0;
            Interface.Oxide.DataFileSystem.WriteObject("SKILLS/" + userid, players[_userid]);
        }

        private void SetMaxLvl(BasePlayer player, string minername)
        {
            if (!permission.UserHasPermission(player.UserIDString, "skillrates." + minername.ToLower())) return;
            SKILL sKILL = (SKILL)Enum.Parse(typeof(SKILL), minername, true);
            Dictionary<SKILL, mainskill> skill;
            if (!players.TryGetValue(player.userID, out skill)) return;
            mainskill mainskill;
            if (!skill.TryGetValue(sKILL, out mainskill))
            {
                AddSkills(player.userID);
                mainskill = players[player.userID][sKILL];
            }
            setting setting = config.settings[sKILL];
            var lvl = levels.LastOrDefault();
            if (lvl.Key == mainskill.lvl) return;
            mainskill.lastlvl = mainskill.lvl;
            mainskill.lvl = lvl.Key;
            string nextstring = lvl.Key.ToString();
            NotifyAll(player.displayName, nextstring, minername);
            string bonuses = GetLang(minername + "_description" + (minername == "woodcutter" ? "_new" : ""), player.UserIDString).Replace("{0}", setting.mod.Count > 0 ? MOD(player.UserIDString, lvl.Value, setting.mod.ElementAt(0).Key) : "").Replace("{1}", setting.mod.Count > 1 ? MOD(player.UserIDString, lvl.Value, setting.mod.ElementAt(1).Key) : "").Replace("{2}", setting.mod.Count > 2 ? MOD(player.UserIDString, lvl.Value, setting.mod.ElementAt(2).Key) : "");
            NotifyPlayer(player, bonuses, nextstring, minername);
            if (players.ContainsKey(player.userID)) Interface.Oxide.DataFileSystem.WriteObject("SKILLS/" + player.UserIDString, players[player.userID]);
        }

        void ADDEXP(BasePlayer player, float exp, SKILL sKILL)
        {
            Dictionary<SKILL, mainskill> skill;
            if (!players.TryGetValue(player.userID, out skill)) return;
            mainskill mainskill;
            if (!skill.TryGetValue(sKILL, out mainskill))
            {
                AddSkills(player.userID);
                mainskill = players[player.userID][sKILL];
            }

            modificator modificator;
            if (!modificators.TryGetValue(player.UserIDString, out modificator)) modificator = new modificator { xp = 1f };

            mainskill.exp += exp * modificator.xp;

            string minername = sKILL.ToString();

            mod level;
            if (!levels.TryGetValue(mainskill.lvl, out level)) return;
            if (mainskill.exp >= level.exp)
            {
                string nextstring = (mainskill.lvl + 1).ToString();
                setting setting = config.settings[sKILL];
                bool stop = false;
                while (!stop)
                {
                    mod mainlevel;
                    if (!levels.TryGetValue(mainskill.lvl, out mainlevel))
                    {
                        stop = true;
                        return;
                    }
                    int next = mainskill.lvl + 1;
                    mod nextlevel;
                    if (!levels.TryGetValue(next, out nextlevel))
                    {
                        stop = true;
                        return;
                    }
                    level = mainlevel;
                    mainskill.exp -= mainlevel.exp;
                    mainskill.lvl = next;

                    RewardPlayer(player.UserIDString, next, setting);

                    if (mainskill.exp < nextlevel.exp) stop = true;
                }
                NotifyAll(player.displayName, nextstring, minername);
                string bonuses = GetLang(minername + "_description" + (minername == "woodcutter" ? "_new" : ""), player.UserIDString).Replace("{0}", setting.mod.Count > 0 ? MOD(player.UserIDString, levels[mainskill.lvl], setting.mod.ElementAt(0).Key) : "").Replace("{1}", setting.mod.Count > 1 ? MOD(player.UserIDString, levels[mainskill.lvl], setting.mod.ElementAt(1).Key) : "").Replace("{2}", setting.mod.Count > 2 ? MOD(player.UserIDString, levels[mainskill.lvl], setting.mod.ElementAt(2).Key) : "");
                NotifyPlayer(player, bonuses, nextstring, minername);
            }
            UISkillBar(player, minername, mainskill.exp, level.exp, mainskill.lvl);
        }

        private void RewardPlayer(string id, int level, setting setting)
        {
            List<string> coms;
            if (setting.reward.TryGetValue(level, out coms))
            {
                foreach (var x in coms) Server.Command(x.Replace("{steamid}", id));
            }
        }

        private void NotifyPlayer(BasePlayer player, string bonuses, string level, string skill)
        {
            player.ChatMessage(GetLang("chat_uplevel", player.UserIDString).Replace("{level}", level).Replace("{bonuses}", bonuses).Replace("{skill}", GetLang(skill, player.UserIDString)));
        }

        private void NotifyAll(string name, string level, string skill)
        {
            HaxBot?.Call("SENDTODISCORD", $"{name} повысил навык {skill} до {level} ур.", 1, 13036665);

            if (!config.broadcast) return;

            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                player.ChatMessage(GetLang("chat_broadcast", player.UserIDString).Replace("{name}", name).Replace("{level}", level).Replace("{skill}", skill));
            }
        }

        private bool IsNewPlayer(BasePlayer player)
        {
            Dictionary<SKILL, mainskill> user = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<SKILL, mainskill>>("SKILLS/" + player.UserIDString);
            if (user == null || user.Count == 0)
            {
                if (!players.ContainsKey(player.userID))
                {
                    players.Add(player.userID, new Dictionary<SKILL, mainskill>());
                    AddSkills(player.userID);
                    return true;
                }
            }
            else
            {
                players[player.userID] = user;
            }
            AddSkills(player.userID);
            return false;
        }

        private void AddDL(string userid)
        {
            ulong _userid = Convert.ToUInt64(userid);
            Dictionary<SKILL, mainskill> user = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<SKILL, mainskill>>("SKILLS/" + userid);
            if (user == null || user.Count == 0)
            {
                if (!players.ContainsKey(_userid))
                {
                    players.Add(_userid, new Dictionary<SKILL, mainskill>());
                }
            }
            else
            {
                players[_userid] = user;
            }
            AddSkills(_userid);
        }

        void AddSkills(ulong id)
        {
            foreach (var z in config.settings.Keys)
            {
                if (!players[id].ContainsKey(z)) players[id].Add(z, new mainskill());
            }
        }

        void OnServerShutdown()
        {
            Unload();
        }

        void Unload()
        {
            foreach (var z in BasePlayer.activePlayerList)
            {
                OnPlayerDisconnected(z);
            }

            var con = Network.Net.sv.connections;
            if(con.Count > 0) CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = con }, null, "DestroyUI", "SkillBar");

            timer.Once(1f, () => ins = null);
        }

        string MAINGUI = "";
        string MAINBAR = "";

        const float constminx = 0.05f;

        
        private void cmdskill(BasePlayer player, string command, string[] args)
        {
            Dictionary<SKILL, mainskill> skill;
            if (!players.TryGetValue(player.userID, out skill))
            {
                IsNewPlayer(player);
                skill = players[player.userID];
            }
            float startx = constminx;
            //0.95
            //0.69
            float starty = 0.9f;
            string GUI = "";
            int i = 0;
            foreach (var z in config.settings)
            {
                //.ToString("F1")
                mod level;
                mainskill mainskill;
                if (!skill.TryGetValue(z.Key, out mainskill)) continue;

                if (!levels.TryGetValue(mainskill.lvl, out level)) level = levels.LastOrDefault().Value;
                float bar = mainskill.exp / level.exp;
                if (bar > 1f) bar = 1f;
                int lvl = mainskill.lvl;
                if (lvl > config.lvl.maxlevel) lvl = config.lvl.maxlevel;
                string name = z.Key.ToString();
                GUI += MAINBAR.Replace("{maxexp}", lvl >= config.lvl.maxlevel ? "∞" : level.exp.ToString("F1")).Replace("{lvlcolor}", z.Value.lvlcolor).Replace("{mainbar}", z.Value.mainbar).Replace("{backbar}", z.Value.backbar).Replace("{png}", GetImage(z.Value.background)).Replace("{exp}", mainskill.exp.ToString("F1")).Replace("{miny}", (starty - 0.26f).ToString()).Replace("{maxy}", starty.ToString()).Replace("{maxx}", (startx + 0.285f).ToString()).Replace("{minx}", startx.ToString()).Replace("{bar}", bar.ToString()).Replace("{info}", GetLang(name + "_description" + (name == "woodcutter" ? "_new" : ""), player.UserIDString).Replace("{0}", z.Value.mod.Count > 0 ? MOD(player.UserIDString, level, z.Value.mod.ElementAt(0).Key) : "").Replace("{1}", z.Value.mod.Count > 1 ? MOD(player.UserIDString, level, z.Value.mod.ElementAt(1).Key) : "").Replace("{2}", z.Value.mod.Count > 2 ? MOD(player.UserIDString, level, z.Value.mod.ElementAt(2).Key) : "")).Replace("{smallinfo}", GetLang(name + "_hint", player.UserIDString)).Replace("{MAXLVL}", maxlevelstring).Replace("{LVL}", lvl.ToString()).Replace("{NAME}", GetLang(name, player.UserIDString));
                i++;
                if (i % 3 == 0)
                {
                    startx = constminx;
                    starty -= 0.3f;
                }
                else
                {
                    startx += 0.305f;
                }
            }
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "TokenMenu");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", MAINGUI.Replace("{header}", GetLang("ui_header", player.UserIDString)).Replace("{closetext}", GetLang("ui_exit", player.UserIDString)).Replace("{main}", GUI));
        }
        static MODIFICATOR[] modx2 = new MODIFICATOR[] { MODIFICATOR.STONE, MODIFICATOR.ANIMAL, MODIFICATOR.HQM, MODIFICATOR.MECH, MODIFICATOR.METAL, MODIFICATOR.NPC, MODIFICATOR.SULFUR, MODIFICATOR.WOOD, MODIFICATOR.ELITE, MODIFICATOR.AIR, MODIFICATOR.LOCKEDCRATE, MODIFICATOR.BARREL, MODIFICATOR.CRATE };
        static MODIFICATOR[] percent = new MODIFICATOR[] { MODIFICATOR.MECHARMOR, MODIFICATOR.MECHDAMAGE, MODIFICATOR.COALSHANCE, MODIFICATOR.NPCARMOR, MODIFICATOR.NPCDAMAGE, MODIFICATOR.ANIMALARMOR, MODIFICATOR.ANIMALDAMAGE };
        string MOD(string id, mod lvl, MODIFICATOR sKILL)
        {
            float num = lvl.modificator[sKILL];
            if (percent.Contains(sKILL)) num *= 100f;
            if (modx2.Contains(sKILL))
            {
                modificator modificator;
                if (!modificators.TryGetValue(id, out modificator)) modificator = new modificator { gather = 1f, xp = 1f };
                num *= modificator.gather;
            }
            return num.ToString("F1");
        }
        #endregion

        #region FURNACE
        // 2931042549 furnace
        // 1374462671 furnace.large
        // 1057236622 refinery_small_deployed
        Dictionary<string, int> DefaultSmeltSpeed = new Dictionary<string, int>
        {
          //  { "bbq.deployed", 15 },
           // { "campfire", 2 },
            { "furnace", 3 },
         //  { "hobobarrel.deployed", 2 },
           // { "lantern.deployed", 1 },
            { "furnace.large", 15 },
            { "refinery_small_deployed", 15 },
         //   { "fireplace.deployed", 2 },
        //    { "tunalight.deployed", 1 },
            { "electricfurnace.deployed", 5 }
        };

        Dictionary<ulong, float> coalchance = new Dictionary<ulong, float>();

        private object OnOvenToggle(StorageContainer st, BasePlayer player)
        {
            var entity = st.GetEntity();
            int dspeed;
            if (entity is BaseOven)
            {
                Dictionary<SKILL, mainskill> skill;
                if (players.TryGetValue(player.userID, out skill))
                {
                    if (DefaultSmeltSpeed.TryGetValue(entity.ShortPrefabName, out dspeed))
                    {
                        int number = entity.ShortPrefabName != "refinery_small_deployed" ? (int)MODGET(skill[SKILL.alchemist].lvl, MODIFICATOR.REMELTINGSPEED) : (int)MODGET(skill[SKILL.woodcutter].lvl, MODIFICATOR.REFINERYSPEED);

                        ///refinery_small_deployed
                        BaseOven oven = entity as BaseOven;
                        oven.smeltSpeed = number * dspeed;
                        coalchance[oven.net.ID.Value] = MODGET(skill[SKILL.woodcutter].lvl, MODIFICATOR.COALSHANCE);
                    }
                }
            }
            return null;
        } 

        object OnFuelConsume(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            int dspeed;
            if (DefaultSmeltSpeed.TryGetValue(oven.ShortPrefabName, out dspeed) && oven.smeltSpeed > dspeed)
            {
                int rate = oven.smeltSpeed / dspeed;
                if (fuel.amount <= rate) rate = fuel.amount;

                float chance;
                if (!coalchance.TryGetValue(oven.net.ID.Value, out chance)) chance = 1 - burnable.byproductChance;

                if (oven.allowByproductCreation && burnable.byproductItem != null && chance > Random.Range(0.0f, 1f))
                {
                    Item obj = ItemManager.Create(burnable.byproductItem, burnable.byproductAmount * rate);
                    if (!obj.MoveToContainer(oven.inventory))
                    {
                        oven.OvenFull();
                        obj.Drop(oven.inventory.dropPosition, oven.inventory.dropVelocity, new Quaternion());
                    }
                }
                if (fuel.amount <= rate)
                {
                    fuel.Remove();
                }
                else
                {
                    fuel.UseItem(rate);
                    fuel.fuel = burnable.fuelAmount;
                    fuel.MarkDirty();
                }
                return false;
            }
            return null;
        }
        #endregion

        #region Instant
        private void OnPlayerAttack(BasePlayer player, HitInfo hit)
        {
            if (!permission.UserHasPermission(player.UserIDString, "skillrates.instant") || hit.HitEntity == null) return;

            if (hit.HitEntity is OreResourceEntity)
            {
                OreResourceEntity oreResourceEntity = hit.HitEntity as OreResourceEntity;
                if (!oreResourceEntity.IsDestroyed) oreResourceEntity._hotSpot.Kill();
                oreResourceEntity._hotSpot = null;
                hit.gatherScale = 666;
            }
            else if (hit.HitEntity is TreeEntity)
            {
                (hit.HitEntity as TreeEntity).hasBonusGame = false;
                hit.gatherScale = 666;
            }
            else if (hit.HitEntity.ShortPrefabName.Contains("dead_log") || hit.HitEntity.ShortPrefabName.Contains("driftwood"))
            {
                hit.gatherScale = 666;
            }
        }
        #endregion

        #region FARMER
        private void OnEntityBuilt(Planner plan, GameObject seed)
        {
            BasePlayer player = plan.GetOwnerPlayer();
            GrowableEntity plant = seed.GetComponent<GrowableEntity>();
            if (player == null || plant == null) return;
            if (plant.ShortPrefabName.Contains("_berry"))
            {
                ADDEXP(player, config.exp.planting_berrys, SKILL.farmer);
            }
            else
            {
                ADDEXP(player, config.exp.planting_vegetables, SKILL.farmer);
            }
            
        }

        #endregion
    }
}