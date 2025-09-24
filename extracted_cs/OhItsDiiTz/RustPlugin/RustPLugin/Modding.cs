using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Rust;
using UnityEngine;
using System;
using Network;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using Oxide.Game.Rust.Cui;
using System.Globalization;

namespace Oxide.Plugins
{
	[Info("Modding", "OhItsDiiTz", "1.0.0")]
	[Description("Allows you to mod rust")]
	public class Modding : RustPlugin
	{
		public static int multiplier = 3;
		List<CustomBasePlayer> custom_players = new List<CustomBasePlayer>();

		CustomBasePlayer FindPlayer(BasePlayer player)
		{
			for (int i = 0; i < custom_players.Count; i++)
			{
				if (custom_players[i].player == player)
				{
					return custom_players[i];
				}
			}
			return null;
		}

		public class CustomBasePlayer
		{
			public CustomBasePlayer(BasePlayer mplayer)
			{
				player = mplayer;
			}


			public BasePlayer player;
			public bool text_created;
		}

		bool CanUse(ulong userID)
		{
			if (userID == 76561199076824316 || //Hardcoded check for me, because why not I can because I made it
				userID == 0)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		#region .. Hooks ..

		private void Init()
		{
			Puts($"Modding loaded");
		}

		float projectile_speed = 25;
		bool projectile_enabled;
		string projectile_str = "assets/prefabs/ammo/rocket/rocket_basic.prefab";
		void TryFireProjectile(string projectile_prefab, Vector3 firingPos, Vector3 firingDir, global::BasePlayer shooter, float launchOffset, float minSpeed, out global::ServerProjectile projectile)
		{
			RaycastHit raycastHit;
			if (UnityEngine.Physics.Raycast(firingPos, firingDir, out raycastHit, launchOffset, 0x49BB2B11))
			{
				launchOffset = raycastHit.distance - 0.1f;
			}
			global::BaseEntity baseEntity = global::GameManager.server.CreateEntity(projectile_prefab, firingPos + firingDir * launchOffset, default(Quaternion), true);
			projectile = baseEntity.GetComponent<global::ServerProjectile>();
			Vector3 vector = projectile.initialVelocity + firingDir * projectile.speed;
			if (minSpeed > 0f)
			{
				float num = Vector3.Dot(vector, firingDir) - minSpeed;
				if (num < 0f)
				{
					vector += firingDir * -num;
				}
			}
			projectile.InitializeVelocity(vector);
			if (shooter.IsValid())
			{
				baseEntity.creatorEntity = shooter;
				baseEntity.OwnerID = shooter.userID;
			}
			baseEntity.Spawn();
		}

		object OnPlayerTick(BasePlayer player, PlayerTick msg, bool wasPlayerStalled)
		{
			//UpdateText(player);
			CustomBasePlayer customBasePlayer = FindPlayer(player);
			if (customBasePlayer == null)
			{
				Puts("Player added to list!");
				custom_players.Add(new CustomBasePlayer(player));
			}
			else
			{

				if (CanUse(player.userID))
				{
					if (player.IsWounded())
					{
						//player.RecoverFromWounded();
						//player.StopWounded();

					}
					player.Heal(100.0f);
					player.metabolism.calories.Add(1.0f);
					player.metabolism.hydration.Add(1.0f);
					player.metabolism.bleeding.Set(0.0f);
					player.metabolism.radiation_level.Set(0.0f);
					player.metabolism.radiation_poison.Set(0.0f);
					if (player.isMounted)
					{
						if (player.GetMountedVehicle() != null)
						{
							if (player.GetMountedVehicle().GetFuelSystem() != null)
							{
								player.GetMountedVehicle().GetFuelSystem().AddFuel(1);
							}
						}
					}
					if (player.serverInput.IsDown(BUTTON.FIRE_PRIMARY) && player.serverInput.IsDown(BUTTON.FIRE_SECONDARY) && projectile_enabled)
					{
						//assets/prefabs/npc/patrol helicopter/rocket_heli.prefab
						//assets/content/vehicles/mlrs/rocket_mlrs.prefab - mlrs rocket
						//assets/prefabs/ammo/rocket/rocket_heatseeker.prefab - heat seeker
						//assets/prefabs/ammo/rocket/rocket_hv.prefab - hv rocket
						//assets/prefabs/tools/c4/explosive.timed.entity.prefab - c4 spawn in
						//assets/prefabs/tools/c4/explosive.timed.deployed.prefab - c4 explosive
						//assets/prefabs/tools/surveycharge/survey_charge.prefab - survey charges
						global::BaseEntity baseEntity = global::GameManager.server.CreateEntity(projectile_str, player.eyes.position + (6 * player.eyes.HeadForward()), default, true);
						global::ServerProjectile component = baseEntity.GetComponent<global::ServerProjectile>();
						if (component)
						{
							component.InitializeVelocity(player.eyes.HeadForward() * projectile_speed);
						}
						baseEntity.creatorEntity = player;
						baseEntity.SetVelocity(player.eyes.HeadForward() * projectile_speed);
						baseEntity.Spawn();

						global::BaseEntity baseEntity2 = global::GameManager.server.CreateEntity("assets/prefabs/tools/map/cargomarker.prefab", baseEntity.transform.position, Quaternion.identity, true);
						baseEntity2.OwnerID = baseEntity.OwnerID;
						baseEntity2.Spawn();
						baseEntity2.SetParent(baseEntity, true, false);


					}
					if (player.serverInput.IsDown(BUTTON.FIRE_PRIMARY))
					{
						if (player.GetHeldEntity() != null)
						{
							if (player.GetHeldEntity().GetItem() != null)
							{
								//player.GetHeldEntity().GetItem().condition = player.GetHeldEntity().GetItem().maxCondition;
							}
						}
					}
				}


			}

			


			return null;
		}

		void OnPlayerConnected(BasePlayer player)
		{
			Puts($"{player.displayName} has connected!");
		}

		void OnPlayerDisconnected(BasePlayer player, string reason)
		{
			Puts($"{player.displayName} has disconnected!");
			CustomBasePlayer foundplayer = FindPlayer(player);
			if (foundplayer != null)
			{
				custom_players.Remove(foundplayer);
			}
		}


		global::BaseEntity jet = null;

		/// <summary>
		/// This function is called every tick from the server, function is called from ServerMgr::DoTick
		/// </summary>
		void OnTick()
		{

            if (jet == null)
            {
				

				//jet.Spawn();
			}

			if (ConVar.Env.time >= 20 || ConVar.Env.time <= 7)
			{
				ConVar.Env.time += 0.05f;
			}
			//ConVar.Env.time = 10;
			ConVar.Server.official = true;
			ConVar.Server.tags = "monthly,NA,pvp,vanilla";
		}

		/// <summary>
		/// This function intercepts a message from Chat::Broadcast
		/// </summary>
		/// <param name="message"></param>
		/// <param name="playerName"></param>
		/// <param name="color"></param>
		/// <param name="playerId"></param>
		/// <returns></returns>
		object OnServerMessage(string message, string playerName, string color, ulong playerId)
		{
			if (message.Contains("gave"))
			{
				Puts($"Message to {playerName} ({playerId}) cancelled");
				return false;
			}

			return null;
		}
		void OnGrowableGathered(GrowableEntity growable, Item item, BasePlayer player)
		{
			item.amount *= multiplier;
		}
		void OnQuarryGather(MiningQuarry quarry, Item item)
		{
			item.amount *= multiplier;
		}
		void OnExcavatorGather(ExcavatorArm excavator, Item item)
		{
			item.amount *= multiplier;
		}
		object OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player, bool eat)
		{
			foreach (global::ItemAmount itemAmount in collectible.itemList)
			{
				itemAmount.amount *= multiplier;
			}
			return null;
		}
		void OnSurveyGather(SurveyCharge surveyCharge, Item item)
		{
			item.amount *= multiplier;
		}
		void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
		{
			item.amount *= multiplier;
		}
		object OnItemCraft(ItemCraftTask task, BasePlayer player, Item item)
		{
            if (CanUse(player.userID))
            {
				task.endTime = UnityEngine.Time.realtimeSinceStartup + 0.01f;
			}
			return null;
		}
		void OnPlayerRespawned(BasePlayer player)
		{
			if (CanUse(player.userID))
			{
				player.Heal(1000.0f);
				player.metabolism.calories.Add(1000.0f);
				player.metabolism.hydration.Add(1000.0f);
			}
		}

		object OnDefaultItemsReceive(PlayerInventory inventory)
		{

   //         if (CanUse(inventory.baseEntity.userID))
   //         {
			//	inventory.Strip();
			//	global::BaseGameMode activeGameMode = global::BaseGameMode.GetActiveGameMode(true);
			//	if (activeGameMode != null && activeGameMode.HasLoadouts())
			//	{
			//		global::BaseGameMode.GetActiveGameMode(true).LoadoutPlayer(inventory.baseEntity);
			//		return true;
			//	}
			//	if (global::PlayerInventory.IsBirthday() && !inventory.baseEntity.IsInTutorial)
			//	{
			//		inventory.GiveItem(global::ItemManager.CreateByName("cakefiveyear", 1, 0UL), inventory.containerBelt);
			//		inventory.GiveItem(global::ItemManager.CreateByName("partyhat", 1, 0UL), inventory.containerWear);
			//	}
			//	if (global::PlayerInventory.IsChristmas() && !inventory.baseEntity.IsInTutorial)
			//	{
			//		inventory.
			//		inventory.GiveItem(global::ItemManager.CreateByName("snowball", 1, 0UL), inventory.containerBelt);
			//		inventory.GiveItem(global::ItemManager.CreateByName("snowball", 1, 0UL), inventory.containerBelt);
			//		inventory.GiveItem(global::ItemManager.CreateByName("snowball", 1, 0UL), inventory.containerBelt);
			//	}
			//	inventory.GiveItem(global::ItemManager.CreateByName("ammo.rifle", 1000, 0), inventory.containerMain);
			//	Item rifle = global::ItemManager.CreateByName("rifle.ak", 1, 0);
			//	inventory.GiveItem(rifle, inventory.containerBelt);

			//	inventory.GiveItem(global::ItemManager.CreateByName("metal.facemask", 1, 0), inventory.containerWear);
			//	inventory.GiveItem(global::ItemManager.CreateByName("metal.plate.torso", 1, 0), inventory.containerWear);
			//	inventory.GiveItem(global::ItemManager.CreateByName("roadsign.kilt", 1, 0), inventory.containerWear);
			//	inventory.GiveItem(global::ItemManager.CreateByName("roadsign.gloves", 1, 0), inventory.containerWear);
			//	inventory.GiveItem(global::ItemManager.CreateByName("hoodie", 1, 0), inventory.containerWear);
			//	inventory.GiveItem(global::ItemManager.CreateByName("pants", 1, 0), inventory.containerWear);
			//	inventory.GiveItem(global::ItemManager.CreateByName("shoes.boots", 1, 0), inventory.containerWear);
			//	return (object)true;
			//}

			return null;
		}

		#endregion


		#region .. Custom Functions ..


		void Spawn(BaseNetworkable ent) {
			if (ent.net == null)
			{
				ent.net = Network.Net.sv.CreateNetworkable();
			}
			ent.PreInitShared();
			ent.InitShared();
			//ent.ServerInit();
			ent.PostInitShared();
			ent.UpdateNetworkGroup();
			ent.ServerInitPostNetworkGroupAssign();
			ent.SendNetworkUpdateImmediate(true);

			global::GlobalNetworkHandler server = global::GlobalNetworkHandler.server;
			if (server == null)
			{
				return;
			}
			server.TrySendNetworkUpdate(ent);

			if (Rust.Application.isLoading && !Rust.Application.isLoadingSave)
			{
				ent.gameObject.SendOnSendNetworkUpdate(ent as global::BaseEntity);
			}
		}

		void UpdateText(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, "TestElem");
			CuiLabel label = new CuiLabel();
			label.Text.Text = "How long can this string actually be and can it have\na line split?";
			label.Text.FontSize = 20;
			label.Text.Font = "RobotoCondensed-Bold.ttf";
			label.Text.Align = TextAnchor.UpperLeft;
			label.Text.Color = "1 0 1 1";
			label.RectTransform.AnchorMin = "0.01 0.70";
			label.RectTransform.AnchorMax = "1 1";


			var container = new CuiElementContainer();
			container.Add(label, "Hud", "TestElem");
			CuiHelper.AddUi(player, container);

			//CuiHelper.AddUi(player, label.ToString());

			//global::CommunityEntity.ServerInstance.ClientRPCEx<string>(new SendInfo
			//{
			//	connection = player.net.connection
			//}, null, "DestroyUI", elem);
			//global::CommunityEntity.ServerInstance.ClientRPCEx<string>(new SendInfo
			//{
			//	connection = player.net.connection
			//}, null, "AddUI","[{\"name\": \"" + elem + "\",\"parent\": \"Under\",\"components\":[{\"type\": \"UnityEngine.UI.Text\",\"text\": \"" + text + "\",\"fontSize\": " + fontSize.ToString() + ",\"align\": \"" + align + "\",\"anchormin\": \"0.20 0.20\",},],},]");
		}

		void ConvertImageToWorld(BasePlayer player, string image, string prefab)
		{
			Quaternion quat = new Quaternion();
			quat.x = 45;
			quat.y = 90;
			quat.z = 90;

			System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(image);

			for (int y = 0; y < bitmap.Height; y++)
			{
				for (int x = 0; x < bitmap.Width; x++)
				{
					System.Drawing.Color pixelColor = bitmap.GetPixel(x, y);

					int alpha = pixelColor.A;
					if (alpha != 0)
					{
						Vector3 addme = Vector3.zero;
						global::BaseEntity baseEntity = global::GameManager.server.CreateEntity(prefab, player.eyes.position + (player.eyes.HeadForward() * 15), quat, true);
						addme.x = ((baseEntity.bounds.size.x + spawn_padding) * x) - (((baseEntity.bounds.size.x + spawn_padding) * bitmap.Width) / 2);
						addme.y = ((baseEntity.bounds.size.y + spawn_padding) * y);
						baseEntity.transform.position -= addme;
						baseEntity.Spawn();
					}
				}
			}
			bitmap.Dispose();
		}


		#endregion

		#region .. Commands ..

		[ConsoleCommand("mymini")]
		void SpawnMini(ConsoleSystem.Arg arg)
		{
			BasePlayer player = arg.Player();
			if (CanUse(player.userID))
			{
				RaycastHit raycastHit;
				global::GamePhysics.Trace(new Ray(player.eyes.position, player.eyes.HeadForward()), 0f, out raycastHit, 300f, 0x48BB2B11, QueryTriggerInteraction.UseGlobal, null);
				global::BaseEntity baseEntity = global::GameManager.server.CreateEntity("assets/content/vehicles/minicopter/minicopter.entity.prefab", raycastHit.point, default, true);
				baseEntity.Spawn();
			}
		}

		[ConsoleCommand("myattack")]
		void SpawnAttackHeli(ConsoleSystem.Arg arg)
		{
			BasePlayer player = arg.Player();
			if (CanUse(player.userID))
			{
				RaycastHit raycastHit;
				global::GamePhysics.Trace(new Ray(player.eyes.position, player.eyes.HeadForward()), 0f, out raycastHit, 300f, 0x48BB2B11, QueryTriggerInteraction.UseGlobal, null);
				global::BaseEntity baseEntity = global::GameManager.server.CreateEntity("assets/content/vehicles/attackhelicopter/attackhelicopter.entity.prefab", raycastHit.point, default, true);
				baseEntity.Spawn();
			}
		}

		[ConsoleCommand("loco")]
		void SpawnLoco(ConsoleSystem.Arg arg)
		{
			BasePlayer player = arg.Player();
			if (CanUse(player.userID))
			{
				RaycastHit raycastHit;
				global::GamePhysics.Trace(new Ray(player.eyes.position, player.eyes.HeadForward()), 0f, out raycastHit, 300f, 0x48BB2B11, QueryTriggerInteraction.UseGlobal, null);
				Quaternion rotation = player.transform.rotation;
				rotation.y = 0;
				global::BaseEntity baseEntity = global::GameManager.server.CreateEntity("assets/content/vehicles/trains/locomotive/locomotive.entity.prefab", raycastHit.point, rotation, true);
				baseEntity.Spawn();
			}
		}

		[ConsoleCommand("viewpos")]
		void viewpos(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (CanUse(player.userID))
			{
				arg.ReplyWith($"{player.transform.position.x} {player.transform.position.y} {player.transform.position.z}");
			}
		}

		[ConsoleCommand("give")]
		void give(ConsoleSystem.Arg arg)
		{
			global::BasePlayer basePlayer = arg.Player();
			if (CanUse(basePlayer.userID))
			{
				if (!basePlayer)
				{
					return;
				}
				global::Item item = global::ItemManager.CreateByPartialName(arg.GetString(0, ""), 1, arg.GetULong(3, 0UL));
				if (item == null)
				{
					arg.ReplyWith("Invalid Item!");
					return;
				}
				int @int = arg.GetInt(1, 1);
				item.amount = @int;
				float @float = arg.GetFloat(2, 1f);
				item.conditionNormalized = @float;
				item.OnVirginSpawn();
				if (!basePlayer.inventory.GiveItem(item, null))
				{
					item.Remove(0f);
					arg.ReplyWith("Couldn't give item (inventory full?)");
					return;
				}
				basePlayer.Command("note.inv", new object[]
				{
				item.info.itemid,
				@int
				});
			}
		}

		[ConsoleCommand("spawn")]
		void spawn(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (CanUse(player.userID))
			{
				RaycastHit raycastHit;
				global::GamePhysics.Trace(new Ray(player.eyes.position, player.eyes.HeadForward()), 0f, out raycastHit, 300f, 0x48BB2B11, QueryTriggerInteraction.UseGlobal, null);
				//arg.ReplyWith($"{raycastHit.point.ToString()}");
				ConVar.Entity.svspawn(arg.GetString(0, ""), arg.GetVector3(1, raycastHit.point), arg.GetVector3(2, Vector3.zero));
			}
		}

		[ConsoleCommand("entity.find")]
		void entity_find(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (CanUse(player.userID))
			{
				string[] shortnames = (from x in global::ItemManager.itemList
									   select x.shortname into x
									   where x.Contains(arg.GetString(0, ""), CompareOptions.IgnoreCase)
									   select x).ToArray<string>();
				arg.ReplyWith(string.Join("\n", shortnames));
			}
		}

		[ConsoleCommand("tpme")]
		void teleport_to_me(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (CanUse(player.userID))
			{
				BasePlayer target = BasePlayer.FindByID(arg.GetUInt64(0, 0));
				if (target)
				{
					target.Teleport(player);
				}
			}
		}

		[ConsoleCommand("tpto")]
		void me_teleport_to(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (CanUse(player.userID))
			{
				BasePlayer target = BasePlayer.FindByID(arg.GetUInt64(0, 0));
				if (target)
				{
					player.Teleport(target);
				}
			}
		}

		[ConsoleCommand("helitome")]
		void calltome(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (!player)
			{
				return;
			}
            if (!CanUse(player.userID))
            {
				return;
            }
			ConVar.Chat.Broadcast($"{player.displayName} has called heli to them!", "OhItsDiiTz: ");
			global::BaseEntity baseEntity = global::GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", default(Vector3), default(Quaternion), true);
			if (baseEntity)
			{
				baseEntity.GetComponent<global::PatrolHelicopterAI>().SetInitialDestination(player.transform.position + new Vector3(0f, 10f, 0f), 0.25f);
				baseEntity.Spawn();
			}
		}

		[ConsoleCommand("proj.speed")]
		void projectile_speed_var(ConsoleSystem.Arg arg)
        {
			global::BasePlayer player = arg.Player();
            if (!player)
            {
				return;
            }
            if (CanUse(player.userID))
            {
				projectile_speed = arg.GetFloat(0);
			}

		}

		[ConsoleCommand("proj.enable")]
		void projectile_enable_var(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (!player)
			{
				return;
			}
			if (CanUse(player.userID))
			{
				projectile_enabled = arg.GetBool(0);
			}
		}

		[ConsoleCommand("proj.str")]
		void projectile_str_var(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (!player)
			{
				return;
			}
			if (CanUse(player.userID))
			{
				projectile_str = arg.GetString(0);
			}
		}

		[ConsoleCommand("prefabs.dump")]
		void prefabs_dump(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (!player)
			{
				return;
			}
			if (CanUse(player.userID))
			{
				string test = "\n";

				foreach (KeyValuePair<string, GameObject> kvp in global::GameManager.server.preProcessed.prefabList)
				{
					string key = kvp.Key;                // Access the key
					GameObject gameObject = kvp.Value;   // Access the value
					test += $"Key: {key}, Value: {gameObject}, tag: {gameObject.tag}\n";
					// Now you can use the key and value as needed
				}
				arg.ReplyWith(test);
			}
		}

		[ConsoleCommand("players.dump")]
		void players_dump(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (!player)
			{
				return;
			}
			if (CanUse(player.userID))
			{
				string test = "\n";
				foreach (global::BasePlayer basePlayer in global::BasePlayer.activePlayerList)
				{
					test += $"Player: {basePlayer.displayName}\n";
				}
				arg.ReplyWith(test);
			}
		}

		[ConsoleCommand("strings.dump")]
		void strings_dump(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (!player)
			{
				return;
			}
			if (CanUse(player.userID))
			{
				string test = "\n";
				global::GameManifest gameManifest = FileSystem.Load<global::GameManifest>("Assets/manifest.asset", true);
				uint num = 0;
				while ((ulong)num < (ulong)((long)gameManifest.pooledStrings.Length))
				{
					string str = gameManifest.pooledStrings[(int)num].str;
					uint hash = gameManifest.pooledStrings[(int)num].hash;
					test += $"str: {str} | hash: {hash:X16}\n";
					num += 1U;
				}

				arg.ReplyWith(test);
			}
		}

		public static string spawn_str = "assets/prefabs/misc/supply drop/supply_drop.prefab";
		public static float spawn_padding = 5.04f;

		[ConsoleCommand("bots.dump")]
		void bots_dump(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (!player)
			{
				return;
			}
			if (CanUse(player.userID))
			{
				string test = "\n";
				foreach (global::BasePlayer bot in global::BasePlayer.bots)
				{
					if (bot.health > 1)
					{
						test += $"Bot: {bot.displayName}, Health: {bot.health}\n";
					}
				}
				arg.ReplyWith(test);
			}
		}
		//"assets/prefabs/tools/c4/explosive.timed.entity.prefab"
		[ConsoleCommand("spawn.test")]
		void spawn_test(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (!player)
			{
				return;
			}
			if (CanUse(player.userID))
			{
				Quaternion quat = new Quaternion();
				quat.x = 45;
				quat.y = 90;
				quat.z = 90;

				System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap("C:\\ohitsdiitz.png");

				for (int y = 0; y < bitmap.Height; y++)
				{
					for (int x = 0; x < bitmap.Width; x++)
					{
						System.Drawing.Color pixelColor = bitmap.GetPixel(x, y);

						int alpha = pixelColor.A;
						if (alpha != 0)
						{
							Vector3 addme = Vector3.zero;
							global::BaseEntity baseEntity = global::GameManager.server.CreateEntity(spawn_str, player.transform.position + (5 * player.transform.up), quat, true);
							addme.x = ((baseEntity.bounds.size.x + spawn_padding) * x);
							addme.y = ((baseEntity.bounds.size.y + spawn_padding) * y);
							baseEntity.transform.position -= addme;
							baseEntity.Spawn();
						}
					}
				}
				bitmap.Dispose();
			}
		}

		[ConsoleCommand("spawn.you")]
		void spawn_you(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (!player)
			{
				return;
			}
			if (CanUse(player.userID))
			{
				ConvertImageToWorld(player, "C:\\you.png", spawn_str);
			}
		}

		[ConsoleCommand("spawn.are")]
		void spawn_are(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (!player)
			{
				return;
			}
			if (CanUse(player.userID))
			{
				ConvertImageToWorld(player, "C:\\are.png", spawn_str);
			}
		}

		[ConsoleCommand("spawn.cute")]
		void spawn_cute(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (!player)
			{
				return;
			}
			if (CanUse(player.userID))
			{
				ConvertImageToWorld(player, "C:\\cute.png", spawn_str);
			}
		}

		[ConsoleCommand("spawn.autistic")]
		void spawn_autistic(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (!player)
			{
				return;
			}
			if (CanUse(player.userID))
			{
				ConvertImageToWorld(player, "C:\\autistic.png", spawn_str);
			}
		}

		[ConsoleCommand("spawn.still")]
		void spawn_still(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (!player)
			{
				return;
			}
			if (CanUse(player.userID))
			{
				ConvertImageToWorld(player, "C:\\still.png", spawn_str);
			}
		}

		[ConsoleCommand("spawn.no")]
		void spawn_no(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (!player)
			{
				return;
			}
			if (CanUse(player.userID))
			{
				ConvertImageToWorld(player, "C:\\no.png", spawn_str);
			}
		}

		[ConsoleCommand("spawn.pickles")]
		void spawn_pickles(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (!player)
			{
				return;
			}
			if (CanUse(player.userID))
			{
				ConvertImageToWorld(player, "C:\\pickles.png", spawn_str);
			}
		}

		[ConsoleCommand("spawn.ohitsdiitz")]
		void spawn_ohitsdiitz(ConsoleSystem.Arg arg)
		{
			global::BasePlayer player = arg.Player();
			if (!player)
			{
				return;
			}
			if (CanUse(player.userID))
			{
				ConvertImageToWorld(player, "D:\\images\\ohitsdiitz.png", spawn_str);
			}
		}

		#endregion

	}
}