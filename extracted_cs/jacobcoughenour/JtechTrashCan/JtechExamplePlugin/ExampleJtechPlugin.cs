// Requires: Jtech

using System.Collections.Generic;
using UnityEngine;
using Oxide.Plugins.JtechCore;

namespace Oxide.Plugins {

	[Info("ExampleJtechPlugin", "TheGreatJ", "0.0.0")]
	class ExampleJtechPlugin : RustPlugin {
		
		void RegisterJDeployables() {
			JDeployableManager.RegisterJDeployable<TrashCan>();
		}

		[JInfo(typeof(ExampleJtechPlugin), "Trash Can", "https://i.imgur.com/lEXshkx.png")]
		[JRequirement("scrap", 10)]
		[JUpdate(10, 5)]
		public class TrashCan : JDeployable {

			public new static void OnStartPlacing(UserInfo userInfo) {
				userInfo.ShowMessage("Placing Trash Can");
			}

			public new static Item GetPlaceholderItem(UserInfo userInfo) {
				// vending machine with assembler skin
				return ItemManager.CreateByName("skull_fire_pit", 1);
			}

			public static void OnDeployPlaceholder(UserInfo userInfo, BaseEntity baseEntity) {
				// save the deployed placeholder
				userInfo.placingSelected = new List<BaseEntity> { baseEntity };
				userInfo.DonePlacing();
			}

			public override bool Place(UserInfo userInfo) {

				if (userInfo.placingSelected == null || userInfo.placingSelected.Count != 1)
					return false;

				data = new SaveData();
				data.SetUser(userInfo);

				BaseEntity placeholder = userInfo.placingSelected[0];

				SetHealth(placeholder.Health()); // set health based on placeholder
				data.SetTransform(placeholder.transform);

				if (!Spawn())
					return false;

				Effect.server.Run("assets/bundled/prefabs/fx/build/promote_metal.prefab", GetEntities()[0], 0U, Vector3.zero, Vector3.zero);

				return true;
			}

			public override bool Spawn(bool placing = false) {

				// spawn new vending machine
				BaseCombatEntity ent = (BaseCombatEntity) GameManager.server.CreateEntity("assets/bundled/prefabs/autospawn/resource/loot/loot-barrel-1.prefab", data.GetPosition(), data.GetRotation());

				ent.Spawn();

				SetMainParent(ent);

				return true;
			}

		}

	}
}