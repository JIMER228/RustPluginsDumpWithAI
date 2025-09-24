using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
	/*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("Test Loot", "https://discord.gg/dNGbxafuJn", "0.1.5")]
	[Description("Checking containers on the server!!!")]
	public class TestLoot : RustPlugin
	{
		private List<string> entitys = new List<string>()
		{
			"assets/bundled/prefabs/radtown/crate_basic.prefab",
			"assets/bundled/prefabs/radtown/crate_elite.prefab",
			"assets/bundled/prefabs/radtown/crate_mine.prefab",
			"assets/bundled/prefabs/radtown/crate_normal.prefab",
			"assets/bundled/prefabs/radtown/crate_normal_2.prefab",
			"assets/bundled/prefabs/radtown/crate_normal_2_food.prefab",
			"assets/bundled/prefabs/radtown/crate_normal_2_medical.prefab",
			"assets/bundled/prefabs/radtown/crate_tools.prefab",
			"assets/bundled/prefabs/radtown/foodbox.prefab",
			"assets/bundled/prefabs/radtown/loot_barrel_1.prefab",
			"assets/bundled/prefabs/radtown/loot_barrel_2.prefab",
			"assets/bundled/prefabs/radtown/loot_trash.prefab",
			"assets/bundled/prefabs/radtown/minecart.prefab",
			"assets/bundled/prefabs/radtown/oil_barrel.prefab",
			"assets/prefabs/npc/m2bradley/bradley_crate.prefab",
			"assets/prefabs/npc/patrol helicopter/heli_crate.prefab",
			"assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab",
			"assets/prefabs/misc/supply drop/supply_drop.prefab",
			"assets/bundled/prefabs/radtown/crate_underwater_basic.prefab",
			"assets/bundled/prefabs/radtown/crate_underwater_advanced.prefab"
		};

		[ChatCommand("testloot")]
		void testloot(BasePlayer player, string command, string[] args)
		{
			if (!player.IsAdmin)
				return;

			float x = 2f;
			foreach (string entityName in this.entitys)
			{
				Vector3 position = this.CalculateGroundPos(player.eyes.position + (player.eyes.BodyRay().direction * x));
				if (position != new Vector3())
				{
					BaseEntity entity = GameManager.server.CreateEntity(entityName, position);
					if (entity)
					{
						entity.Spawn();
						player.SendConsoleCommand("ddraw.text", 1800f, Color.green, entity.transform.position + new Vector3(0, 0.1f, 0), entityName);
					}
				}
				x += 2f;
			}
		}

		public Vector3 CalculateGroundPos(Vector3 sourcePos)
		{
			if (sourcePos == null)
				return new Vector3();

			RaycastHit hitInfo;
			Physics.Raycast(sourcePos, Vector3.down, out hitInfo, 1000f, LayerMask.GetMask("Terrain", "Water", "World"), QueryTriggerInteraction.Ignore);

			if (hitInfo.collider == null || !hitInfo.collider.name.Contains("rock"))
				Physics.Raycast(sourcePos, Vector3.down, out hitInfo, 1000f, LayerMask.GetMask("Terrain", "Water"), QueryTriggerInteraction.Ignore);

			if (hitInfo.collider != null && (hitInfo.collider.tag == "Main Terrain" || hitInfo.collider.name.Contains("rock")))
			{
				sourcePos.y = hitInfo.point.y;
				return sourcePos;
			}
			return new Vector3();
		}
	 }
}