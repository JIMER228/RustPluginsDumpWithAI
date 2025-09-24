using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using UnityEngine;
using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{
	/*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("Barrel Events", "https://discord.gg/dNGbxafuJn", "1.3.0")]

	public class BarrelEvents : RustPlugin
	{
		#region Classes
		private class Barrel
		{			public string Name;
			//[JsonProperty("Название действия")]
			public string Action;
			//[JsonProperty("Счастливый: true/false")]
			public bool Happy;
		}
		#endregion

		#region Variables
		//[JsonProperty("Шанс удачной бочки")]
		private int luckyChance;
		// [JsonProperty("Шанс срабатывания ивента")]
		private int eventChance;
		const string MurdPrefab = "assets/prefabs/npc/murderer/murderer.prefab";
		private const string BearPrefab = "assets/rust.ai/agents/bear/bear.prefab";
		//Количество наносимового урона(без учета резиста)
		private float HurtDamage;
		private float HealAmount;
		private float radAmount;
		private float caloriesAmount;
		private float HydrotateAmount;
		private int amountBears;
		private int amountMurd;
		private string colorTag;
		private string colorMes;
		private bool extraEvent;
		private int extraChance;
		
		private string tag = "[Barrel Events]";
		

		private List<Barrel> barrelList = new List<Barrel>();
		private Dictionary<string, int> randomLoot = new Dictionary<string, int>();
		private Dictionary<string, int> extraLoot = new Dictionary<string, int>();
		

		#endregion

		#region Initialization

		void Loaded()
		{
			LoadDefaultConfig();
			if (!Interface.Oxide.DataFileSystem.ExistsDatafile("BarrelEvents/Events"))
			{
				barrelList = new List<Barrel>
				{
					new Barrel
					{

						Action = "SpawnBear",
						Happy = false,
						Name = "Мишка"
					},
					new Barrel
					{

						Action = "SpawnMurd",
						Happy = false,
						Name = "Зомби"
					},
					new Barrel
					{

						Action = "DealDamage",
						Happy = false,
						Name = "Насение урона"
					},
					new Barrel
					{

						Action = "RadiationPlus",
						Happy = false,
						Name = "Доза радиации"
					},
					new Barrel
					{

						Action = "StarvingMinus",
						Happy = false,
						Name = "Голод"
					},
					new Barrel
					{

						Action = "DehidratateMinus",
						Happy = false,
						Name = "жажда"
					},
					new Barrel
					{

						Action = "HealthRegen",
						Happy = true,
						Name = "Исцеление"
					},
					new Barrel
					{

						Action = "SpawnLoot",
						Happy = true,
						Name = "Дополнительный лут",
					},
					new Barrel
					{

						Action = "RadiationReduce",
						Happy = true,
						Name = "Снижение радиации"
					},
					new Barrel
					{

						Action = "StarvingPlus",
						Happy = true,
						Name = "Частичная сытость"
					},
					new Barrel
					{

						Action = "DehidratatePlus",
						Happy = true,
						Name = "Частичное утоление жажды"
					}

				};
				Interface.Oxide.DataFileSystem.WriteObject("BarrelEvents/Events", barrelList);
			}
			else
			{
				barrelList = Interface.Oxide.DataFileSystem.ReadObject<List<Barrel>>("BarrelEvents/Events");
				PrintWarning("Настройки загржуены");
			}

			if (!Interface.Oxide.DataFileSystem.ExistsDatafile("BarrelEvents/Loot"))
			{
				randomLoot = new Dictionary<string, int>
				{
					["sulfur"] = 100,
					["sulfur"] = 10,
					["bandage"] = 10
				};
				Interface.Oxide.DataFileSystem.WriteObject("BarrelEvents/Loot", randomLoot);
			}
			else
			{
				randomLoot = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, int>>("BarrelEvents/Loot");
				PrintWarning("Рандомный лут загржуен");
			}
			if (!Interface.Oxide.DataFileSystem.ExistsDatafile("BarrelEvents/Loot"))
			{
				extraLoot = new Dictionary<string, int>
				{
					["rifle.ak"] = 1,
					["ammo.rocket.basic"] = 3
					
				};
				Interface.Oxide.DataFileSystem.WriteObject("BarrelEvents/Loot", randomLoot);
			}
			else
			{
				extraLoot = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, int>>("BarrelEvents/Loot");
				PrintWarning("Дополнительный лут загржуен");
			}
			
		}

		protected override void LoadDefaultConfig()
		{
			Config["Шанс срабатывания ивента"] = eventChance = GetConfig("Шанс срабатывания ивента", 3);
			Config["Шанс удачного сложения обстоятельств"] = luckyChance = GetConfig("Шанс удачного сложения обстоятельств", 50);
			Config["Количество калорий"] = caloriesAmount = GetConfig("Количество калорий", 50f);
			Config["Наносимый урон"] = HurtDamage = GetConfig("Наносимый урон", 10f);
			Config["Количество восстановленного хп"] = HealAmount = GetConfig("Количество восстановленного хп", 15f);
			Config["Количество жажды"] = HydrotateAmount = GetConfig("Количество жажды", 50f);
			Config["Количество радиации"] = radAmount = GetConfig("Количество радиации", 15);
			Config["Количество медведей"] = amountBears = GetConfig("Количество медведей", 1);
			Config["Количество зомби"] = amountMurd = GetConfig("Количество зомби", 1);
			Config["Настройка цвета тэга"] = colorTag = GetConfig("Настройка цвета тэга", "#8B008B");
			Config["Настройка цвета сообщения"] = colorMes = GetConfig("Настройка цвета сообщения", "#FFFF00");
			Config["Включить специальные ивенты?"] = extraEvent = GetConfig("Включить специальные ивенты?", true);
			Config["Включить специальные ивенты?"] = extraChance = GetConfig("Включить специальные ивенты?", 10);
			SaveConfig();
		}

		#endregion

		#region OxideHooks

		void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
		{

			if (!(entity is LootContainer) || !(info.InitiatorPlayer is BasePlayer) || Random.Range(0, 100) > eventChance)
				return;
			Barrel newAction = barrelList.Where(p => p.Happy == Random.Range(0, 100) < luckyChance).ToList().GetRandom();
			Call(newAction.Action, info.InitiatorPlayer, (BaseEntity) entity);
			SendReply(info.InitiatorPlayer, $"<color={colorTag}>{tag}</color> <color={colorMes}>{newAction.Name}</color>");
			
			

		}
		

		#endregion

		#region Actions

		void DealDamage(BasePlayer player, BaseEntity entity) => player.Hurt(HurtDamage);

		void HealthRegen(BasePlayer player, BaseEntity entity)
		{
			player.health = Math.Min(player.health + HealAmount, 100);
			if (extraEvent != false && Random.Range(0,100) > extraChance)
			{
				player.health = 100;
				player.metabolism.bleeding.value = 0;
				player.metabolism.SendChangesToClient();
				SendReply(player,$"<color={colorTag}>{tag}</color> <color={colorMes}>Ты полностью здоров!</color>");
			}
		}

		void SpawnBear(BasePlayer player, BaseEntity entity)
		{

			for (int i = 0; i != amountBears; i++)
			GameManager.server.CreateEntity(BearPrefab, entity.transform.position).Spawn();
			}
		
		void SpawnMurd(BasePlayer player, BaseEntity entity)
		{

			for (int i = 0; i != amountMurd; i++)
				GameManager.server.CreateEntity(MurdPrefab, entity.transform.position).Spawn();
		}

	void RadiationPlus(BasePlayer player, BaseEntity entity)
        {
            player.metabolism.radiation_poison.value = player.metabolism.radiation_poison.value + radAmount;
            player.metabolism.SendChangesToClient();
        }
        void RadiationReduce(BasePlayer player, BaseEntity entity)
        {
            player.metabolism.radiation_poison.value = player.metabolism.radiation_poison.value - radAmount;
            player.metabolism.SendChangesToClient();
	        if (extraEvent != false && Random.Range(0,100) > extraChance)
	        {
		        player.metabolism.radiation_poison.value = 0;
		        player.metabolism.SendChangesToClient();
		        SendReply(player,$"<color={colorTag}>{tag}</color> <color={colorMes}>Ты полностью избавлен от радиации!</color>");
	        }
        }
        void SpawnLoot(BasePlayer player, BaseEntity entity)
        {
            
			var randomDrop = randomLoot.Keys.ToList().GetRandom();
            ItemManager.CreateByPartialName(randomDrop, randomLoot[randomDrop]).Drop(entity.transform.position, Vector3.zero);
	        SendReply(player,$"<color={colorTag}>{tag} You recive {randomDrop} {randomLoot[randomDrop]}!</color>");
	        if (extraEvent != false && Random.Range(0,100) > extraChance)
	        {
		        var extraDrop = extraLoot.Keys.ToList().GetRandom();
		        ItemManager.CreateByPartialName(extraDrop,extraLoot[extraDrop]).Drop(entity.transform.position, Vector3.zero);
		        SendReply(player,$"<color={colorTag}>{tag}</color> <color={colorMes}>Поздравляю! Ты выбил {extraLoot[extraDrop]} x {extraDrop}</color>");
	        }
	        
	        
        }
		void StarvingPlus(BasePlayer player, BaseEntity entity){
			player.metabolism.calories.value = player.metabolism.calories.value + caloriesAmount;
            player.metabolism.SendChangesToClient();
			if (extraEvent != false && Random.Range(0,100) > extraChance)
			{
				player.metabolism.calories.value = 500;
				player.metabolism.SendChangesToClient();
				SendReply(player,$"<color={colorTag}>{tag}</color> <color={colorMes}>Ты полностью сыт!</color>");
			}
		}
		void StarvingMinus(BasePlayer player, BaseEntity entity){
			player.metabolism.calories.value = player.metabolism.calories.value - caloriesAmount;
            player.metabolism.SendChangesToClient();	
		}
		void DehidratatePlus(BasePlayer player, BaseEntity entity){
			player.metabolism.hydration.value = player.metabolism.hydration.value + HydrotateAmount; 
            player.metabolism.SendChangesToClient();
			if (extraEvent != false && Random.Range(0,100) > extraChance)
			{
				player.metabolism.hydration.value = 250;
				player.metabolism.SendChangesToClient();
				SendReply(player,$"<color={colorTag}>{tag}</color> <color={colorMes}>Ты полностью утолил жажду!</color>");
			}
		}
		void DehidratateMinus(BasePlayer player, BaseEntity entity){
			player.metabolism.hydration.value = player.metabolism.hydration.value - HydrotateAmount; 
            player.metabolism.SendChangesToClient();
		}

        #endregion

		

        #region Helpers
        
        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        #endregion
    }
}