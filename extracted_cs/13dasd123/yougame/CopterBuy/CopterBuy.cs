// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using Physics = UnityEngine.Physics;

namespace Oxide.Plugins
{
    [Info("Покупка миникоптеров в магазине", "poof.", "1.0.0")]
    class CopterBuy : RustPlugin
    {
        private Dictionary<uint, uint> PRM = new Dictionary<uint, uint>();

        void OnEntityBuilt(Planner planner, GameObject gameobject)
        {
            BaseEntity entity = gameobject.ToBaseEntity();
            if(entity == null) return;
	        
            if (entity.skinID != 1663370375) return;   
            entity.Kill();

            var ePos = entity.transform.position;
	       
            Vector3 position = new Vector3(ePos.x,ePos.y+1, ePos.z);

            var hitted = false;

            RaycastHit Hit;
            if (Physics.Raycast(position, Vector3.down, out Hit, LayerMask.GetMask(new string[] {"Construction"})))
            {
                var rhEntity = Hit.GetEntity();
		        
                if (rhEntity != null)
                {
                    hitted = true;
                    if(!PRM.ContainsKey(rhEntity.net.ID))
                        PRM.Add(rhEntity.net.ID, 0);
                }
            }

            BaseEntity rEntity = GameManager.server.CreateEntity("assets/content/vehicles/minicopter/minicopter.entity.prefab", entity.transform.position, entity.GetNetworkRotation(), true);                                 // 1
            rEntity.Spawn();
            rEntity.skinID = 1663370375;
            
            if (!hitted) return;
            PRM[Hit.GetEntity().net.ID] = rEntity.net.ID;
        }

        bool GiveRecycler(ItemContainer container)
        {
            var item = ItemManager.CreateByItemID(833533164, 1, 1663370375);
            item.name = "Миникоптер";
            return item.MoveToContainer(container, -1, false);
        }

        bool GiveRecycler(BasePlayer player)
        {
            var item = ItemManager.CreateByItemID(833533164, 1, 1663370375);
            item.name = "Миникоптер";
            if (!player.inventory.GiveItem(item))
            {
                item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity, new Quaternion());
                return false;
            }
            return true;
        }

        [ConsoleCommand("copter.add")]
        private void CmdAddCopter(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
            {
                SendError(arg, "[Ошибка] У вас нет доступа к этой команде!");
                return;
            }

            if (!arg.HasArgs())
            {
                PrintError(
                ":\n[Ошибка] Введите copter.add steamid/nickname\n[Пример] copter.add Jjj\n[Пример] copter.add 76561198311233564");
                return;
            }

            var player = BasePlayer.Find(arg.Args[0]);
            if (player == null)
            {
                PrintError($"[Ошибка] Не удается найти игрока {arg.Args[0]}");
                return;
            }

            GiveRecycler(player);
        }
    }
}