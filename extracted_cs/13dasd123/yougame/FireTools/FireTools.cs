using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using System.Linq;
using System.Text;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
	[Info("FireTools", "Fartus", "1.0.4")]
	[Description("Выдается огненный инструмент, используя который добываются переплавленные ресурсы.")]
	class FireTools : RustPlugin
	{ 
	    #region Classes
		public List<uint> fireTools;
		private uint uid;
		readonly DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("FireTools");
		#endregion

		#region Oxide hooks
		void OnServerInitialized()
		{
			InitCockables();
			try
			{
				fireTools = dataFile.ReadObject<List<uint>>();
			}
			catch
			{
				fireTools = new List<uint>();
			}
		}
		void Save()
		{
			dataFile.WriteObject(fireTools);
		}
		#endregion

        #region Console command
		[ConsoleCommand("hatchet.create")] // топор
		void cmdConsole_ToolhatchetCreate(ConsoleSystem.Arg arg)
		{
			if(!arg.HasArgs() || arg.Args.Length < 1)
			{
				SendError(arg, "Должен быть 1 аргумент.");
				return;
			}

			int toolId = -1252059217;
			ulong playerId = arg.GetUInt64(0);
			
			if (arg.Connection == null || arg.Connection.authLevel > 1)
			{
				BasePlayer targetPlayer = BasePlayer.FindByID(playerId);

				if (targetPlayer == null)
				{
					SendError(arg, "Игрок не найден [" + playerId + "].");
					return;
				}

				Item item = ItemManager.CreateByItemID(toolId, skin: (ulong)815040374);

				if (item == null)
				{
					SendError(arg, "При создании предмета произошла ошибка!");
					return;
				}

				fireTools.Add(item.uid);

				targetPlayer.GiveItem(item);

				SendReply(arg, "Предмет успешно выдан игроку: " + targetPlayer.displayName);

				SendReply(targetPlayer, "<size=15>Вы получили Огненный инструмент.</size>");
				Save();
				return;
			}

			SendError(arg, "У вас нет прав, чтобы использовать эту команду!");
		}

		[ConsoleCommand("pickaxe.create")] // кирка
		void cmdConsole_ToolpickaxeCreate(ConsoleSystem.Arg arg)
		{
			if(!arg.HasArgs() || arg.Args.Length < 1)
			{
				SendError(arg, "Должен быть 1 аргумент.");
				return;
			}

			int toolId = -1302129395;
			ulong playerId = arg.GetUInt64(0);

			if (arg.Connection == null || arg.Connection.authLevel > 1)
			{
				BasePlayer targetPlayer = BasePlayer.FindByID(playerId);
				
				if (targetPlayer == null)
				{
					SendError(arg, "Игрок не найден [" + playerId + "].");
					return;
				}

				Item item = ItemManager.CreateByItemID(toolId, skin: (ulong)820199230);

				if (item == null)
				{
					SendError(arg, "При создании предмета произошла ошибка!");
					return;
				}

				fireTools.Add(item.uid);

				targetPlayer.GiveItem(item);

				SendReply(arg, "Предмет успешно выдан игроку: " + targetPlayer.displayName);

				SendReply(targetPlayer, "<size=15>Вы получили Огненный инструмент.</size>");
				Save();
				return;
			}

			SendError(arg, "У вас нет прав, чтобы использовать эту команду!");
		}

		[ConsoleCommand("icepick.create")] // ледоруб
		void cmdConsole_ToolicepickCreate(ConsoleSystem.Arg arg)
		{
			if(!arg.HasArgs() || arg.Args.Length < 1)
			{
				SendError(arg, "Должен быть 1 аргумент.");
				return;
			}

			int toolId = -1780802565;
			ulong playerId = arg.GetUInt64(0);

			if (arg.Connection == null || arg.Connection.authLevel > 1)
			{
				BasePlayer targetPlayer = BasePlayer.FindByID(playerId);
				
				if (targetPlayer == null)
				{
					SendError(arg, "Игрок не найден [" + playerId + "].");
					return;
				}

				Item item = ItemManager.CreateByItemID(toolId, skin: (ulong)844666224);

				if (item == null)
				{
					SendError(arg, "При создании предмета произошла ошибка!");
					return;
				}

				fireTools.Add(item.uid);

				targetPlayer.GiveItem(item);

				SendReply(arg, "Предмет успешно выдан игроку: " + targetPlayer.displayName);

				SendReply(targetPlayer, "<size=15>Вы получили Огненный инструмент.</size>");
				Save();
				return;
			}

			SendError(arg, "У вас нет прав, чтобы использовать эту команду!");
		}

        [ConsoleCommand("chainsaw.create")] // бензопила
		void cmdConsole_ToolchainsawCreate(ConsoleSystem.Arg arg)
		{
			if(!arg.HasArgs() || arg.Args.Length < 1)
			{
				SendError(arg, "Должен быть 1 аргумент.");
				return;
			}

			int toolId = 1104520648;
			ulong playerId = arg.GetUInt64(0);

			if (arg.Connection == null || arg.Connection.authLevel > 1)
			{
				BasePlayer targetPlayer = BasePlayer.FindByID(playerId);

				if (targetPlayer == null)
				{
					SendError(arg, "Игрок не найден [" + playerId + "].");
					return;
				}

				Item item = ItemManager.CreateByItemID(toolId, skin: (ulong)1744052235);

				if (item == null)
				{
					SendError(arg, "При создании предмета произошла ошибка!");
					return;
				}

				fireTools.Add(item.uid);

				targetPlayer.GiveItem(item);

				SendReply(arg, "Предмет успешно выдан игроку: " + targetPlayer.displayName);

				SendReply(targetPlayer, "<size=15>Вы получили Огненный инструмент.</size>");
				Save();
				return;
			}

			SendError(arg, "У вас нет прав, чтобы использовать эту команду!");
		}

        [ConsoleCommand("jackhammer.create")] // бур
		void cmdConsole_TooljackhammerCreate(ConsoleSystem.Arg arg)
		{
			if(!arg.HasArgs() || arg.Args.Length < 1)
			{
				SendError(arg, "Должен быть 1 аргумент.");
				return;
			}

			int toolId = 1488979457;
			ulong playerId = arg.GetUInt64(0);

			if (arg.Connection == null || arg.Connection.authLevel > 1)
			{
				BasePlayer targetPlayer = BasePlayer.FindByID(playerId);

				if (targetPlayer == null)
				{
					SendError(arg, "Игрок не найден [" + playerId + "].");
					return;
				}

				Item item = ItemManager.CreateByItemID(toolId, skin: (ulong)1744050300);

				if (item == null)
				{
					SendError(arg, "При создании предмета произошла ошибка!");
					return;
				}

				fireTools.Add(item.uid);

				targetPlayer.GiveItem(item);

				SendReply(arg, "Предмет успешно выдан игроку: " + targetPlayer.displayName);

				SendReply(targetPlayer, "<size=15>Вы получили Огненный инструмент.</size>");
				Save();
				return;
			}

			SendError(arg, "У вас нет прав, чтобы использовать эту команду!");
		}
		#endregion

		#region Chat command
		[ChatCommand("ftool")]
		void CmdChat_CreateTool(BasePlayer player)
		{
		    if(!player.IsAdmin)
			{
			    SendReply(player, "<size=15>У вас нет доступа к этой команде!</size>");
			    return;
		    }

			Item item = ItemManager.CreateByItemID(-1252059217);
			Item item2 = ItemManager.CreateByItemID(-1302129395);
			Item item3 = ItemManager.CreateByItemID(-1780802565);
			Item item4 = ItemManager.CreateByItemID(1104520648);
			Item item5 = ItemManager.CreateByItemID(1488979457);

			item.skin = (ulong)815040374;
			item.GetHeldEntity().skinID = (ulong)815040374;
			item.skin = (ulong)815040374;

			item2.skin = (ulong)820199230;
			item2.GetHeldEntity().skinID = (ulong)820199230;
			item2.skin = (ulong)820199230;

			item3.skin = (ulong)844666224;
			item3.GetHeldEntity().skinID = (ulong)844666224;
			item3.skin = (ulong)844666224;

			item4.skin = (ulong)1744052235;
			item4.GetHeldEntity().skinID = (ulong)1744052235;
			item4.skin = (ulong)1744052235;

			item5.skin = (ulong)1744050300;
			item5.GetHeldEntity().skinID = (ulong)1744050300;
			item5.skin = (ulong)1744050300;

			player.GiveItem(item, BaseEntity.GiveItemReason.ResourceHarvested);
			player.GiveItem(item2, BaseEntity.GiveItemReason.ResourceHarvested);
			player.GiveItem(item3, BaseEntity.GiveItemReason.ResourceHarvested);
			player.GiveItem(item4, BaseEntity.GiveItemReason.ResourceHarvested);
			player.GiveItem(item5, BaseEntity.GiveItemReason.ResourceHarvested);

			fireTools.Add(item.uid);
			fireTools.Add(item2.uid);
			fireTools.Add(item3.uid);
			fireTools.Add(item4.uid);
			fireTools.Add(item5.uid);

			Puts(item.info.itemid.ToString());
			Puts(item2.info.itemid.ToString());
			Puts(item3.info.itemid.ToString());
			Puts(item4.info.itemid.ToString());
			Puts(item5.info.itemid.ToString());
			Save();
		}
		#endregion

		/*void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
		{
			if (entity.name.Contains("loot_barrel")) // все виды бочек, даже нефтяные 
			{
				if (Random.Range(1, 100) == 1f)
				{
					Item item = ItemManager.CreateByItemID(-1252059217, skin: (ulong)851146955);
					if (Random.Range(1, 3) == 1f)
					{
					item = ItemManager.CreateByItemID(-1302129395, skin: (ulong)815040374);	
					}
					fireTools.Add(item.uid);
					
					item.Drop(entity.transform.position, entity.GetDropVelocity());
					
					Save();
				}
			}
		}*/

		#region Helpers
		object OnItemRepair(BasePlayer player, Item item)
		{
			if (fireTools.Contains(item.uid))
			{
				SendReply(player, "<size=15>Огненные инструменты не поддаются ремонту!</size>");
				return false;
			}

			return null;
		}

		Dictionary<ItemDefinition, ItemModCookable> cookables;
		void InitCockables()
        {
            cookables =
                ItemManager.itemList.ToDictionary(item => item, item => item.GetComponent<ItemModCookable>())
                    .Where(item => item.Value != null && !item.Value.becomeOnCooked.shortname.Contains("burned")).ToDictionary(item => item.Key, mod => mod.Value);
        }
		void OnLoseCondition(Item item, ref float amount)
		{
		    uid = item.uid;
		}
		void OnDispenserGather(ResourceDispenser dispenser, BaseEntity ent, Item item)
		{
			if (dispenser == null || ent == null || item == null) 
            {
                return;
            }

			BasePlayer player = ent as BasePlayer;
            if (player == null) return; 
			
				if (fireTools.Contains(uid))
				{
				    ItemModCookable cookable;
					ItemDefinition gfiofufujfu = item.info;
					if (!cookables.TryGetValue(item.info, out cookable) && gfiofufujfu.itemid != -151838493) return;
					var amount = item.amount;
            NextTick(() =>
            {
                List<Item> items = new List<Item>();
                player.inventory.Take(items, gfiofufujfu.itemid, amount);
                items.ForEach(i=> i.Remove());
				if(gfiofufujfu.itemid == -151838493){
				player.inventory.GiveItem(ItemManager.CreateByItemID(-1938052175, amount));
				return;
				}
                var cockItem = ItemManager.Create(cookable.becomeOnCooked, amount);
                if (!cockItem.MoveToContainer(player.inventory.containerMain))
                {
                    cockItem.Drop(player.GetCenter(), Vector3.up);
                }
            });
				}

		//	return null;
		}
		#endregion
	}
}
