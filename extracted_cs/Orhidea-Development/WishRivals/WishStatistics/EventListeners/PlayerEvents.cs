using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxide.Plugins
{
    public partial class WishStatistics
    {

        //Scrap Gambling
        void OnBigWheelLoss(BigWheelGame wheel, Item scrap)
        {
            Database.SetPlayerData(scrap.text.ToString(), "ScrapLost", Database.GetPlayerDataRaw<int>(scrap.text, "ScrapLost") + scrap.amount);
        }
        object OnBigWheelWin(BigWheelGame wheel, Item scrap, BigWheelBettingTerminal terminal, int multiplier)
        {
            Database.SetPlayerData(scrap.text.ToString(), "ScrapWon", Database.GetPlayerDataRaw<int>(scrap.text, "ScrapWon") + scrap.amount);
            return null;
        }

        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
        {
            if (item.info.shortname == "scrap")
            {
                item.text = playerLoot.baseEntity.UserIDString;
            }
            return null;
        }
        Item OnItemSplit(Item item, int amount)
        {
            if (item.info.shortname == "scrap")
            {
                Item newItem = ItemManager.CreateByItemID(item.info.itemid, amount, 0uL);
                item.amount -= amount;
                newItem.text = item.text;
                return newItem;
            }
            return null;
        }
        //Scrap Gaming End


        //Kills & Deaths
        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            // Check for null or NPC
            if (player == null || player.IsNpc || !player.userID.IsSteamId() || info == null || player == info?.Initiator) return null;
            
            Database.SetPlayerData(player.UserIDString.ToString(), "Deaths", Database.GetPlayerDataRaw<int>(player.UserIDString, "Deaths") + 1);
            
            var attacker = info.InitiatorPlayer;
            if (attacker != null || attacker.userID.IsSteamId() || attacker != player) 
            {
                Database.SetPlayerData(attacker.UserIDString.ToString(), "Kills", Database.GetPlayerDataRaw<int>(attacker.UserIDString, "Kills") + 1);
                if (attacker.Team != null && attacker?.Team?.teamID != player?.Team?.teamID)
                {
                    
                    Database.SetClanData(attacker.Team.teamID.ToString(), "Kills", Database.GetClanDataRaw<int>(attacker.Team.teamID.ToString(), "Kills") + 1);
                }
            }
            return null;
        }
        //End Kills & Deaths


        //Npc & animal kills
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
			if (entity == null || info == null || info.Initiator == null) return;

            var attacker = info.InitiatorPlayer;
            if (attacker == null || !attacker.userID.IsSteamId() || attacker == entity) return;

            if (entity is BaseAnimalNPC)
            {
                Database.SetPlayerData(attacker.UserIDString.ToString(), "AnimalKills", Database.GetPlayerDataRaw<int>(attacker.UserIDString, "AnimalKills") + 1);
            }else{

            if (entity is ScientistNPC)
                {
                    Database.SetPlayerData(attacker.UserIDString.ToString(), "NpcKills", Database.GetPlayerDataRaw<int>(attacker.UserIDString, "NpcKills") + 1);
                }
            }
        }
        //End Npc & animal kills

        //Damage done
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null || entity == null || info?.HitEntity == null || entity.IsNpc || !info?.HitEntity.IsSteamId())
                return null;

            var attacker = info.InitiatorPlayer;
            if (attacker == null || !attacker.userID.IsSteamId())
                return null;

            Database.SetPlayerData(attacker.UserIDString.ToString(), "Hits", Database.GetPlayerDataRaw<int>(attacker.UserIDString, "Hits") + 1);

            {
                NextTick(() =>
                {
                    var damages = info?.damageTypes?.Total() ?? 0f;
                    Database.SetPlayerData(attacker.UserIDString.ToString(), "Damage", (int)(Database.GetPlayerDataRaw<int>(attacker.UserIDString, "Damage") + damages));
                });

                if (info.isHeadshot)
                {
                    Database.SetPlayerData(attacker.UserIDString.ToString(), "Headshots", Database.GetPlayerDataRaw<int>(attacker.UserIDString, "Headshots") + 1);
                }
                return null;
            }
            //End Damage done
        }

        //Accuracy
        //Shots:hits 
        void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
        {
            if (projectile == null || player == null || !player.userID.IsSteamId())
                return;

            Database.SetPlayerData(player.UserIDString.ToString(), "Shots", Database.GetPlayerDataRaw<int>(player.UserIDString, "Shots") + 1);
        }
        //End Accuracy

        //Revives
        object OnPlayerRevive(BasePlayer reviver, BasePlayer player)
        {
            if (reviver == null || player == null) return null;

            Database.SetPlayerData(reviver.UserIDString.ToString(), "Revives", Database.GetPlayerDataRaw<int>(reviver.UserIDString, "Revives") + 1);
            return null;
        }
        //End Revives

        //Crafts
        void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            Database.SetPlayerData(task.owner.UserIDString.ToString(), "ItemsCrafted", Database.GetPlayerDataRaw<int>(task.owner.UserIDString, "ItemsCrafted") + 1);
        }
        //End Crafting



        //wood & ores mined
		void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
		{
			if (player == null || !player.userID.IsSteamId() || dispenser == null || item == null) return;
			var gatherType = dispenser.gatherType;
			if (gatherType == ResourceDispenser.GatherType.Tree)
			{
                Database.SetPlayerData(player.UserIDString.ToString(), "TreesFarmed", Database.GetPlayerDataRaw<int>(player.UserIDString, "TreesFarmed") + 1);
			}
			else if (gatherType == ResourceDispenser.GatherType.Ore)
			{
                Database.SetPlayerData(player.UserIDString.ToString(), "OresFarmed", Database.GetPlayerDataRaw<int>(player.UserIDString, "OresFarmed") + 1);
			}
		}
        //end woood & ores mined
    }
}
