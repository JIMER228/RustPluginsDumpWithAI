using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Vehicles Extended", "Lomarine/Ernieleo/Kleopatra", "1.2.1")] 
    [Description("Плагин для работы с транспортом")] 
    public class VehiclesExt : RustPlugin
    {
        private const string 
            chinook = "assets/prefabs/npc/ch47/ch47.entity.prefab", 
            sedan = "assets/content/vehicles/sedan_a/sedantest.entity.prefab", 
            boat = "assets/content/vehicles/boats/rowboat/rowboat.prefab",
			rboat = "assets/content/vehicles/boats/rhib/rhib.prefab",
			copter = "assets/content/vehicles/minicopter/minicopter.entity.prefab",
			ballon = "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab",
			scraptransporthelicopter = "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab";

        [ConsoleCommand("ext.spawn")]
        private void Console(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
            {
                PrintError("У вас нет доступа к данной команде!");
                return;
            }
            
            var playerArg = arg.Args[0];
            
            BasePlayer player = BasePlayer.Find(playerArg);
            if (player == null)
            {
                PrintError($"Не удаётся найти игрока <{playerArg}>");
                return;
            }

            var entityArg = arg.Args[1];

            switch (entityArg)
            {
                case "boat":
                    entityArg = boat;
                    break;

                case "car":
                    entityArg = sedan;
                    break;

                case "heli":
                    entityArg = chinook;
                    break;
					
				case "rboat":
                    entityArg = rboat;
                    break;
				
				case "copter":
                    entityArg = copter;
                    break;
					
				case "ballon":
                    entityArg = ballon;
                    break;
					
				case "scraptransporthelicopter":
                    entityArg = scraptransporthelicopter;
                    break;	

                default:
                    PrintError("Неизвестный тип транспорта!");
                    return;
            }
            

            var pos = new Vector3(player.transform.position.x + 10, player.transform.position.y + 5,player.transform.position.z + 10); // TODO: Better spawn
            
            var entity = GameManager.server.CreateEntity(entityArg, pos);
            
            entity.OwnerID = player.OwnerID;
            entity.Spawn();
            
            //player.MountObject(entity as BaseMountable); TODO: Mount on spawn
        } 
    }
}