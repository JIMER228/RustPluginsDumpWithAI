using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ItemFromShop", "CASHR#6906", "1.0.0")]
    internal class ItemFromShop : RustPlugin
    {
       
        #region OxideHooks
        private void OnServerInitialized()
        {
            if (lang.GetServerLanguage() == "ru")
            {
                PrintWarning("" + "\n=====================" + "\n=====================Author: CASHR" +
                             "\n=====================VK: vk.com/cashrdev" +
                             "\n=====================Discord: CASHR#6906" +
                             "\n=====================Email: pipnik99@gmail.com" +
                             "\n=====================Если вы хотите заказать у меня плагин, я жду вас в любой момент." +
                             "\n=====================");
                PrintWarning(
                    "Благодарим за покупку плагина на сайте RustPlugin.ru. Если вы передадите этот плагин сторонним лицам знайте - это лишает вас гарантированных обновлений!");
            }
            else
            {
                PrintWarning("" + "\n=====================" + "\n=====================Author: CASHR" +
                             "\n=====================VK: vk.com/cashrdev" +
                             "\n=====================Discord: CASHR#6906" +
                             "\n=====================Email: pipnik99@gmail.com" +
                             "\n=====================If you want to order a plugin from me, I am waiting for you in discord." +
                             "\n=====================");
            }
        }
        #endregion


        [ConsoleCommand("ITEMFROMSHOP")]
        private void cmdChatITEMFROMSHOP(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin) return;


            if (arg.Args == null || arg.Args.Length < 4)
            {
                PrintError("Не правильное использование команды. Пример: ITEMSFORSHOP STEAMID SHORTNAME КОЛИЧЕСТВО СКИНИД ИМЯ");
                return;
            }

            int amount;
            ulong skinID;
            if(int.TryParse(arg.Args[2], out amount))
            {
                if (ulong.TryParse(arg.Args[3], out skinID))
                {
                    var item = ItemManager.CreateByName(arg.Args[1], amount, skinID);
                    if (item == null)
                    {
                        PrintError($"Предмета {arg.Args[1]} не существует");
                        return;
                    }
                    item.name = string.Join(" ", arg.Args.Skip(4));
                    var player = BasePlayer.Find(arg.Args[0]);
                    if (player == null)
                    {
                        PrintError("Игрок не был найден");
                        return;
                    }
                    player.GiveItem(item);
                }
                else
                {
                    PrintError("Не правильно указан SkinID");
                }
            }
            else
            {
             PrintError("Не правильно указано число");
            }

        }
    }
}