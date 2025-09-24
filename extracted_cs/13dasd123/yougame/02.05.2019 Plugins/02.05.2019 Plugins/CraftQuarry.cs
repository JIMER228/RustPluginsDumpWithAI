using Newtonsoft.Json;
using Facepunch;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
namespace Oxide.Plugins
{
    [Info("CraftQuarry", "Дмитрий Анатольевич | Night_Tiger", "1.0.1")]
      //  Слив плагинов server-rust by Apolo YouGame
	[Description("Позволяет крафтить карьер, геозаряды и нефтекачку  с возможностью её установки")]
    class CraftQuarry : RustPlugin
    {
        #region Fields
        static CraftQuarry ins;
        private bool initialized;
		private readonly List<Timer> timers = new List<Timer>();
        private readonly List<string> duplicate = new List<string>();

        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
			permission.RegisterPermission("CraftQuarry.use", this);
            if (configData.PC.oilCrateChance < 0) configData.PC.oilCrateChance = 0;
            if (configData.PC.oilCrateChance > 100) configData.PC.oilCrateChance = 100;
        }
		
		private void Unload()
        {
            foreach (var time in timers)
                time.Destroy();
        }

        private void OnServerInitialized()
        {
            ins = this;
            //  LoadData();

            initialized = true;
        }
		private void OnResourceDepositCreated(ResourceDepositManager.ResourceDeposit resourceDeposit)
        {
			if (Random.Range(0f, 100f) >= configData.PC.oilCrateChance) return;
            resourceDeposit._resources.Clear();
            resourceDeposit.Add(ItemManager.FindItemDefinition("crude.oil"), 1f, 50000, 10f, ResourceDepositManager.ResourceDeposit.surveySpawnType.ITEM, false);
        }
		private void OnSurveyGather(SurveyCharge survey, Item item)
        {
            if (item.info.name != "crude_oil.item") return;
            var pos = survey.transform.position;
            var posID = $"{pos.x}{pos.y}{pos.z}";
            if (duplicate.Contains(posID))
            {
                timer.In(1f, () =>
                {
                    duplicate.Remove(posID);
                });
                return;
            }
            if (!IsAllowed((BasePlayer)survey.creatorEntity, true))
            {
                item.Remove();
                DeSpawn("survey_crater", pos, 0f);
                // SendReply((BasePlayer)survey.creatorEntity, "У вас нет привилегии для установки нефтекачки");
                return;
            }
            Spawn("assets/prefabs/tools/surveycharge/survey_crater_oil.prefab", pos);
            DeSpawn("survey_crater", pos, configData.PC.oilCrateDespawn);
            duplicate.Add(posID);
        }
		private void OnEntitySpawned(BaseNetworkable entity)
        {
            var quarry = entity as MiningQuarry;
            if (quarry == null) return;
            if (!quarry.ShortPrefabName.Contains("pumpjack-static"))
                quarry.canExtractSolid = true;
			if (entity.ShortPrefabName.Contains("mining_quarry"))
                quarry.canExtractLiquid = false;
        }
		private void Spawn(string prefab, Vector3 position)
        {
            Quaternion rot;
            Vector3 pos;
            GetLocation(position, out pos, out rot);
            var createdPrefab = GameManager.server.CreatePrefab(prefab, pos, rot);
            if (createdPrefab == null) return;
            var entity = createdPrefab.GetComponent<BaseEntity>();
            entity.Spawn();
        }
        private void DeSpawn(string prefab, Vector3 position, float time)
        {
            timers.Add(timer.Once(time, () =>
            {
                var nearby = Pool.GetList<SurveyCrater>();
                Vis.Entities(position, 1f, nearby);
                foreach (var ent in nearby)
                    if (ent.PrefabName.Contains(prefab)) ent.KillMessage();
                Pool.FreeList(ref nearby);
            }));
        }
		private void GetLocation(Vector3 startPos, out Vector3 pos, out Quaternion rot)
        {
            pos = startPos;
            pos.y = 0f;
            rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            RaycastHit raycastHit;
            if (TerrainMeta.HeightMap)
            {
                var height = TerrainMeta.HeightMap.GetHeight(pos) - 0.2f;
                pos.y = Mathf.Max(pos.y, height);
            }
            if (TransformUtil.GetGroundInfo(pos, out raycastHit, 20f, -1063190527))
      //  Слив плагинов server-rust by Apolo YouGame
            {
                pos = raycastHit.point;
                rot = Quaternion.LookRotation(rot * Vector3.forward, raycastHit.normal);
            }
        }
		private bool IsAllowed(BasePlayer player, bool perm = false)
        {
            return player.net?.connection?.authLevel > 1 || perm && permission.UserHasPermission(player.UserIDString, "CraftQuarry.use");
        }
        #endregion
		

        #region Commands
        [ChatCommand("craft")]
        void cmdCraftUser(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "CraftQuarry.use") && !player.IsAdmin)
            {
                SendReply(player, "<color=#1E90FF>У тебя нету прав использовать эту команду!</color>");
                return;
            }
			
			if (args.Length == 0)
			player.ChatMessage($"<b><color=#4169E1>Крафт карьера, геозарядов и нефтекачки!</color></b>\n<b><color=#6A5ACD> <size=15>Для крафта карьера, геозарядов и нефтекачки используйте команды:</size></color></b>\n<b>/craft quarry</b>\n<b>/craft survey</b>\n<b>/craft neft</b>");

            else if (args.Length == 1)
            {
                switch (args[0].ToLower())
				{
					case "quarry":
					{
						var wood = player.inventory.GetAmount(-151838493);//Дерево
						var lowgradefuel = player.inventory.GetAmount(69511070);//Фрагменты металла
						var gears = player.inventory.GetAmount(479143914);//Шестерни
						var stones = player.inventory.GetAmount(-2099697608);//Камень
						var rope = player.inventory.GetAmount(1414245522);//Верёвки
						var ladder = player.inventory.GetAmount(-316250604);//Лестница
						var propane = player.inventory.GetAmount(-1673693549);//Пропановый баллон
						var sheet = player.inventory.GetAmount(-1994909036);//Листовой металл
						
						if (wood >= configData.CC.quarry.comp1 & lowgradefuel >= configData.CC.quarry.comp2 & gears >= configData.CC.quarry.comp3 & stones >= configData.CC.quarry.comp4 & rope >= configData.CC.quarry.comp5 & ladder >= configData.CC.quarry.comp6 & propane >= configData.CC.quarry.comp7 & sheet >= configData.CC.quarry.comp8)
						{
							player.inventory.Take(null, -151838493, configData.CC.quarry.comp1);//Дерево
							player.inventory.Take(null, 69511070, configData.CC.quarry.comp2);//Фрагменты металла
							player.inventory.Take(null, 479143914, configData.CC.quarry.comp3);//Шестерни
							player.inventory.Take(null, -2099697608, configData.CC.quarry.comp4);//Камень
							player.inventory.Take(null, 1414245522, configData.CC.quarry.comp5);//Верёвки
							player.inventory.Take(null, -316250604, configData.CC.quarry.comp6);//Лестница
							player.inventory.Take(null, -1673693549, configData.CC.quarry.comp7);//Пропановый баллон
							player.inventory.Take(null, -1994909036, configData.CC.quarry.comp8);//Листовой металл
						}
						else
						{
						player.ChatMessage($"<b><color=#4169E1>Недостаточно ресурсов!</color></b>\n<b><color=#6A5ACD> <size=15>Для крафта карьера нужны ресурсы:</size></color></b>\n<b>Дерево <color=#6A5ACD>{configData.CC.quarry.comp1}</color> шт. (В наличии <color=#6A5ACD>{wood}</color> шт.)</b>\nМеталл <color=#6A5ACD>{configData.CC.quarry.comp2}</color> шт. (В наличии <color=#6A5ACD>{lowgradefuel}</color> шт.)\n<b>Шестерни <color=#6A5ACD>{configData.CC.quarry.comp3}</color> шт. (В наличии <color=#6A5ACD>{gears}</color> шт.)</b>\n<b>Камень <color=#6A5ACD>{configData.CC.quarry.comp4}</color> шт. (В наличии <color=#6A5ACD>{stones}</color> шт.)</b>\n<b>Верёвки <color=#6A5ACD>{configData.CC.quarry.comp5}</color> шт. (В наличии <color=#6A5ACD>{rope}</color> шт.)</b>\n<b>Лестница <color=#6A5ACD>{configData.CC.quarry.comp6}</color> шт. (В наличии <color=#6A5ACD>{ladder}</color> шт.)</b>\n<b>Пропановый баллон <color=#6A5ACD>{configData.CC.quarry.comp7}</color> шт. (В наличии <color=#6A5ACD>{propane}</color> шт.)</b>\n<b>Листовой металл <color=#6A5ACD>{configData.CC.quarry.comp8}</color> шт. (В наличии <color=#6A5ACD>{sheet}</color> шт.)</b>\nКак соберешь эти ресурсы, прописывай <color=#1974D2>/craft quarry</color>");
							return;
						}

						player.inventory.GiveItem(ItemManager.CreateByItemID(1052926200, 1));//Карьер
						player.ChatMessage($"<color=#1FCECB>Карьер успешно скрафчен!</color>");
						return;
					}
					case "survey":
					{
						var gunpowder = player.inventory.GetAmount(-265876753);//Порох
						var lowgradefuel = player.inventory.GetAmount(-946369541);//ТНК
						var cloth = player.inventory.GetAmount(-858312878);//Ткань
						var metal = player.inventory.GetAmount(69511070);//Фрагменты металла
						if (gunpowder >= configData.CC.survey.comp1 & lowgradefuel >= configData.CC.survey.comp2 & cloth >= configData.CC.survey.comp3 & metal >= configData.CC.survey.comp4)
						{
							player.inventory.Take(null, -265876753, configData.CC.survey.comp1);//Порох
							player.inventory.Take(null, -946369541, configData.CC.survey.comp2);//ТНК
							player.inventory.Take(null, -858312878, configData.CC.survey.comp3);//Ткань
							player.inventory.Take(null, 69511070, configData.CC.survey.comp4);//Фрагменты металла
						}
						else
						{
						player.ChatMessage($"<b><color=#4169E1>Недостаточно ресурсов!</color></b>\n<b><color=#6A5ACD> <size=15>Для крафта геологического заряда нужны ресурсы:</size></color></b>\n<b>Порох <color=#6A5ACD>{configData.CC.survey.comp1}</color> шт. (В наличии <color=#6A5ACD>{gunpowder}</color> шт.)</b>\nТНК <color=#6A5ACD>{configData.CC.survey.comp2}</color> шт. (В наличии <color=#6A5ACD>{lowgradefuel}</color> шт.)\n<b>Ткань <color=#6A5ACD>{configData.CC.survey.comp3}</color> шт. (В наличии <color=#6A5ACD>{cloth}</color> шт.)</b>\n<b>Металл фрагменты <color=#6A5ACD>{configData.CC.survey.comp4}</color> шт. (В наличии <color=#6A5ACD>{metal}</color> шт.)</b>\n\nКак соберешь эти ресурсы, прописывай <color=#1974D2>/craft survey</color>");
							return;
						}

						player.inventory.GiveItem(ItemManager.CreateByItemID(1975934948, 1));//Геологический заряд
						player.ChatMessage($"<color=#1FCECB>Геологический заряд успешно скрафчен!</color>");
						return;
					}
					case "neft":
					{
						var wood = player.inventory.GetAmount(-151838493);//Дерево
						var metal = player.inventory.GetAmount(69511070);//Фрагменты металла
						var gears = player.inventory.GetAmount(479143914);//Шестерёнки
						var stone = player.inventory.GetAmount(-2099697608);//Камень
						var rope = player.inventory.GetAmount(1414245522);//Верёвки
						var ladder = player.inventory.GetAmount(-316250604);//Лестница
						var propane = player.inventory.GetAmount(-1673693549);//Пропановый баллон
						var sheet = player.inventory.GetAmount(-1994909036);//Листовой металл
						if (wood >= configData.CC.neft.comp1 & metal >= configData.CC.neft.comp2 & gears >= configData.CC.neft.comp3 & stone >= configData.CC.neft.comp4 & rope >= configData.CC.neft.comp5 & ladder >= configData.CC.neft.comp6 & propane >= configData.CC.neft.comp7 & sheet >= configData.CC.neft.comp8)
						{
							player.inventory.Take(null, -151838493, configData.CC.neft.comp1);//Дерево
							player.inventory.Take(null, 695110701, configData.CC.neft.comp2);//Фрагменты металла
							player.inventory.Take(null, 479143914, configData.CC.neft.comp3);//Шестерёнки
							player.inventory.Take(null, -2099697608, configData.CC.neft.comp4);//Камень
							player.inventory.Take(null, 1414245522, configData.CC.neft.comp5);//Верёвки
							player.inventory.Take(null, -316250604, configData.CC.neft.comp6);//Лестница
							player.inventory.Take(null, -1673693549, configData.CC.neft.comp7);//Пропановый баллон
							player.inventory.Take(null, -1994909036, configData.CC.neft.comp8);//Листовой металл
						}
						else
						{
						player.ChatMessage($"<b><color=#4169E1>Недостаточно ресурсов!</color></b>\n<b><color=#6A5ACD> <size=15>Для крафта нефтекачки нужны ресурсы:</size></color></b>\n<b>Дерево <color=#6A5ACD>{configData.CC.neft.comp1}</color> шт. (В наличии <color=#6A5ACD>{wood}</color> шт.)</b>\nФрагменты металла <color=#6A5ACD>{configData.CC.neft.comp2}</color> шт. (В наличии <color=#6A5ACD>{metal}</color> шт.)\n<b>Шестерёнки <color=#6A5ACD>{configData.CC.neft.comp3}</color> шт. (В наличии <color=#6A5ACD>{gears}</color> шт.)</b>\n<b>Камень <color=#6A5ACD>{configData.CC.neft.comp4}</color> шт. (В наличии <color=#6A5ACD>{stone}</color> шт.)</b>\n<b>Верёвки <color=#6A5ACD>{configData.CC.neft.comp5}</color> шт. (В наличии <color=#6A5ACD>{rope}</color> шт.)</b>\n<b>Лестница <color=#6A5ACD>{configData.CC.neft.comp6}</color> шт. (В наличии <color=#6A5ACD>{ladder}</color> шт.)</b>\n<b>Пропановый баллон <color=#6A5ACD>{configData.CC.neft.comp7}</color> шт. (В наличии <color=#6A5ACD>{propane}</color> шт.)</b>\n<b>Листовой металл <color=#6A5ACD>{configData.CC.neft.comp8}</color> шт. (В наличии <color=#6A5ACD>{sheet}</color> шт.)</b>\nКак соберешь эти ресурсы, прописывай <color=#1974D2>/craft neft</color>");
							return;
						}

						player.inventory.GiveItem(ItemManager.CreateByItemID(-1130709577, 1));//Нефтекачка
						player.ChatMessage($"<color=#1FCECB>Нефтекачка успешно скрафчена!</color>");
						return;
					}
				}
            
            } else {
				return;
			}

        }
		

        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Настройки нефтекачки")]
            public PumpConf PC { get; set; }
			
			[JsonProperty(PropertyName = "Компоненты необходимые для крафта")]
            public CrafterComponents CC { get; set; }

            public class PumpConf
            {
				[JsonProperty(PropertyName = "Шанс появления лунки")]
                public float oilCrateChance { get; set; }
				[JsonProperty(PropertyName = "Таймер удаления лунки(в секундах)")]
                public float oilCrateDespawn { get; set; }
			}
			public class CrafterComponents
            {
                [JsonProperty(PropertyName = "Для карьера")]
                public CCquarry quarry { get; set; }
				[JsonProperty(PropertyName = "Для геологических зарядов")]
                public CCsurvey survey { get; set; }
				[JsonProperty(PropertyName = "Для нефтекачки")]
                public CCneft neft { get; set; }

                public class CCquarry
                {
                    [JsonProperty(PropertyName = "Дерево")]
                    public int comp1 { get; set; }
                    [JsonProperty(PropertyName = "Фрагменты металла")]
                    public int comp2 { get; set; }
                    [JsonProperty(PropertyName = "Шестерни")]
                    public int comp3 { get; set; }
                    [JsonProperty(PropertyName = "Камень")]
                    public int comp4 { get; set; }
					[JsonProperty(PropertyName = "Верёвки")]
                    public int comp5 { get; set; }
                    [JsonProperty(PropertyName = "Лестница")]
                    public int comp6 { get; set; }
                    [JsonProperty(PropertyName = "Баллоны из под пропана")]
                    public int comp7 { get; set; }
                    [JsonProperty(PropertyName = "Листовой металл")]
                    public int comp8 { get; set; }
                }
				public class CCsurvey
                {
                    [JsonProperty(PropertyName = "Порох")]
                    public int comp1 { get; set; }
                    [JsonProperty(PropertyName = "Топливо низкого качества (ТНК)")]
                    public int comp2 { get; set; }
                    [JsonProperty(PropertyName = "Ткань")]
                    public int comp3 { get; set; }
                    [JsonProperty(PropertyName = "Металл")]
                    public int comp4 { get; set; }
                }
				public class CCneft
                {
                    [JsonProperty(PropertyName = "Дерево")]
                    public int comp1 { get; set; }
                    [JsonProperty(PropertyName = "Фрагменты металла")]
                    public int comp2 { get; set; }
                    [JsonProperty(PropertyName = "Шестерни")]
                    public int comp3 { get; set; }
                    [JsonProperty(PropertyName = "Камень")]
                    public int comp4 { get; set; }
					[JsonProperty(PropertyName = "Верёвки")]
                    public int comp5 { get; set; }
                    [JsonProperty(PropertyName = "Лестница")]
                    public int comp6 { get; set; }
                    [JsonProperty(PropertyName = "Баллоны из под пропана")]
                    public int comp7 { get; set; }
                    [JsonProperty(PropertyName = "Листовой металл")]
                    public int comp8 { get; set; }
                }
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                PC = new ConfigData.PumpConf
                {
					oilCrateChance = 50f,
					oilCrateDespawn = 300f
				},
				CC = new ConfigData.CrafterComponents
                {
                    quarry = new ConfigData.CrafterComponents.CCquarry
                    {
                        comp1 = 10000,
                        comp2 = 2000,
                        comp3 = 20,
                        comp4 = 5000,
						comp5 = 20,
                        comp6 = 1,
                        comp7 = 3,
                        comp8 = 10
                    },
                    survey = new ConfigData.CrafterComponents.CCsurvey
                    {
                        comp1 = 10,
                        comp2 = 5,
                        comp3 = 4,
                        comp4 = 10
                    },
					neft = new ConfigData.CrafterComponents.CCneft
                    {
                        comp1 = 10000,
                        comp2 = 2000,
                        comp3 = 10,
                        comp4 = 5000,
						comp5 = 20,
                        comp6 = 1,
                        comp7 = 5,
                        comp8 = 3
                    },
                }
            };
        }
        #endregion
    }
}
