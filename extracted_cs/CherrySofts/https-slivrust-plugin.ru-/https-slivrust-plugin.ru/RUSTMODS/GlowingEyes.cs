// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿﻿using System.Linq;

namespace Oxide.Plugins
{
    // Creation date: 16-11-2021
    // Last update date: UPDATE_DATE
    [Info("Glowing Eyes", "rustmods.ru / updated by Germanov)", "1.0.2")]
    [Description("https://rustmods.ru")]
    public class GlowingEyes : RustPlugin
    {
        #region Vars

        private const string permUse = "glowingeyes.use";
        private const string itemShortname = "gloweyes";

        #endregion
        
        #region Oxide Hooks

        private void Init()
        {
            permission.RegisterPermission(permUse, this);
        }

        private void OnPlayerCorpse(BasePlayer player, BaseCorpse corpse)
        {
            // Проверяем все контейнеры игрока на наличие gloweyes
            var containers = new[] { player.inventory.containerMain, player.inventory.containerBelt, player.inventory.containerWear };
            
            foreach (var container in containers)
            {
                if (container == null) continue;
                
                foreach (var item in container.itemList.ToList()) // ToList() для безопасного удаления
                {
                    if (item.info.shortname == itemShortname)
                    {
                        item.Remove(0f);
                    }
                }
            }
            
            ItemManager.DoRemoves();
        }
        
        #endregion

        #region Commands

        [ChatCommand("eyes")]
        private void cmdChat(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, permUse) == false)
            {
                player.ChatMessage("You don't have permission to do that!");
                return;
            }
            
            if (HaveEyes(player))
            {
                player.ChatMessage("Removing eyes...");
                Disable(player);
            }
            else
            {
                player.ChatMessage("Adding eyes...");
                Enable(player);
            }
        }

        #endregion

        #region Core

        private void Enable(BasePlayer player)
        {
            var container = player.inventory.containerWear;
            var item = ItemManager.CreateByName(itemShortname);
            item.SetParent(container);
            item.position = 100;
        }

        private void Disable(BasePlayer player)
        {
            var item = player.inventory.containerWear.itemList.FirstOrDefault(x => x.info.shortname == itemShortname);
            if (item == null) return;
            
            item.RemoveFromContainer();
            
            var held = item.GetHeldEntity();
            if (held != null)
            {
                held.Kill();
            }
           
            item.DoRemove();
        }

        private bool HaveEyes(BasePlayer player)
        {
            return player.inventory.containerWear.itemList.Any(x => x.info.shortname == itemShortname);
        }

        #endregion
    }
}