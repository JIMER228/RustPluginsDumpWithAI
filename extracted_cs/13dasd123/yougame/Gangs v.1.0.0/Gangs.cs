using System;
using System.Net;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Gangs", "A001", "1.0.0")]
    public class Gangs : RustPlugin
    {
        #region Variables

        private List<ulong> enableDamage = new List<ulong>();
        private List<ulong> disableMessage = new List<ulong>();

        #endregion

        #region Commands

        [ChatCommand("ff")]
        private void cmdChatDamage(BasePlayer player, string command, string[] args)
        {
			if (args.Length == 0)
            {
                SendReply(player, "Используйте:\n" +
                                        "<color=#FFA500>/ff on</color> - <color=#81B67A>включить</color> урон по союзникам\n" +
                                        "<color=#FFA500>/ff off</color> - <color=#DC143C>выключить</color> урон по союзникам");
                return;
            }

            if (args[0].ToLower() == "on")
            {
                if (enableDamage.Contains(player.userID))
                {
                    SendReply(player, "У вас уже <color=#81B67A>включен</color> урон по союзникам!");
                    return;
                }
                enableDamage.Add(player.userID);
                SendReply(player, "Вы <color=#81B67A>включили</color> урон по союзникам!");
                return;
            }

            if (args[0].ToLower() == "off")
            {
                if (!enableDamage.Contains(player.userID))
                {
                    SendReply(player, "У вас уже <color=#DC143C>выключен</color> урон по союзникам!");
                    return;
                }
                enableDamage.Remove(player.userID);
                SendReply(player, "Вы <color=#DC143C>выключили</color> урон по союзникам!");
                return;
            }

            SendReply(player, "Используйте:\n" +
                                    "<color=#FFA500>/ff on</color> - <color=#81B67A>включить</color> урон по союзникам\n" +
                                    "<color=#FFA500>/ff off</color> - <color=#DC143C>выключить</color> урон по союзникам");
        }

        [ChatCommand("friend")]
        private void cmdChatFriends(BasePlayer player, string command, string[] args)
        {
			if (args == null || args.Length <= 0 || args.Length == 1 && args[0].ToLower() != "accept" && args[0].ToLower() != "cancel")
			{
                SendReply(player, "Используйте:\n" +
				                        "<color=#FFA500>/friend add 'НИК'</color> - отправить запрос игроку\n" +
										//"<color=#FFA500>/friend accept</color> - принять запрос от игрока\n" +
										//"<color=#FFA500>/friend cancel</color> - отклонить запрос от игрока\n" +
										"<color=#FFA500>/friend remove 'НИК'</color> - исключить игрока из банды");
                return;
            }

			var playerTeam = API_GetPlayerTeam(player);
			switch (args[0].ToLower())
            {
                case "add":
				    if (playerTeam == null)
                    {
                        SendReply(player, "У вас нет банды, сначала создайте её!\n<size=12>Создать её можно в левом нижнем углу инвентаря</size>");
                        return;
                    }

                    if (playerTeam.GetLeader() != player)
                    {
                        SendReply(player, "Вы не лидер этой банды, вы не можете приглашать игроков!");
                        return;
                    }

                    var playerArg = args[1];
                    BasePlayer target = BasePlayer.Find(playerArg);
                    if (target == null)
                    {
                        SendReply(player, $"Игрок с именем <color=#BEF781>{playerArg}</color> не найден!");
                        return;
                    }

                    if (target == player)
                    {
                        SendReply(player, "Вы не можете добавить себя в банду!");
                        return;
                    }

                    var targetTeam = API_GetPlayerTeam(target);
                    if (targetTeam != null)
                    {
                        SendReply(player, $"Игрок <color=#BEF781>{target.displayName}</color> уже в вашей банде!");
                        return;
                    }
                    playerTeam.SendInvite(target);

                    SendReply(player, $"Вы успешно пригласили игрока <color=#BEF781>{target.displayName}</color> в свою банду!");
                    //SendReply(target, $"Игрок <color=#BEF781>{player.displayName}</color> пригласил вас в банду\nПримите приглашение в инвентаре или набрав в чате <color=#FFA500>/friend accept</color>");
					SendReply(target, $"Игрок <color=#BEF781>{player.displayName}</color> пригласил вас в банду\nПримите приглашение в инвентаре!");
                    break;

                /*case "accept":
                    playerTeam.AcceptInvite(player);
                    SendReply(player, "Вы приняли запрос в банду!");
                    break;

                case "cancel":
                    playerTeam.RejectInvite(player);
                    SendReply(player, "Вы отклонили запрос в банду!");
                    break;*/

				case "remove":
				    var playerArg2 = args[1];
                    BasePlayer target2 = BasePlayer.Find(playerArg2);
				    if (target2 == null)
					{
						SendReply(player, $"Игрока с именем <color=#BEF781>{playerArg2}</color> нет в вашей банде!");
						return;
					}
					if (target2 == player)
                    {
						SendReply(player, "Вы не можете исключить себя из банды!");
						return;
					}
					if (playerTeam == null)
				    {
				        SendReply(player, "У вас нет банды, сначала создайте её!\n<size=12>Создать её можно в левом нижнем углу инвентаря</size>");
				        return;
				    }
					if (playerTeam.GetLeader() != player)
				    {
				        SendReply(player, "Вы не лидер этой банды, вы не можете выгонять игроков!");
				        return;
				    }
                    playerTeam.RemovePlayer(target2.userID);
                    SendReply(player, $"Вы успешно исключили игрока <color=#BEF781>{target2.displayName}</color> из вашей банды!");
					SendReply(target2, $"Игрок <color=#BEF781>{player.displayName}</color> исключил вас из банды!");
				    break;
            }
        }

		#endregion

		#region Hooks

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (!(entity is BasePlayer) || info?.InitiatorPlayer == null || info?.InitiatorPlayer.GetComponent<NPCPlayer>() != null || info.damageTypes.GetMajorityDamageType() == DamageType.Suicide)
                return null;

            if (info.InitiatorPlayer == entity as BasePlayer)
                return null;
            if (API_AreTeamMates(entity.GetComponent<BasePlayer>(), info.InitiatorPlayer) && !enableDamage.Contains(info.InitiatorPlayer.userID))
            {
                if (!disableMessage.Contains(info.InitiatorPlayer.userID))
                {
                    disableMessage.Add(info.InitiatorPlayer.userID);
                    SendReply(info.InitiatorPlayer, "Вы не можете нанести урон по своим!");
                    timer.Once(1, () =>
                    {
                        if (disableMessage.Contains(info.InitiatorPlayer.userID))
                            disableMessage.Remove(info.InitiatorPlayer.userID);
                    });
                }
                info.damageTypes.ScaleAll(0);
                return false;
            }

            return null;
        }

        #endregion

        #region API

        private RelationshipManager.PlayerTeam API_GetPlayerTeam(BasePlayer player)
        {
            RelationshipManager.PlayerTeam playerTeam = RelationshipManager._instance.FindTeam(player.currentTeam);

            return playerTeam ?? null;
        }

        private bool API_AreTeamMates(BasePlayer player, BasePlayer target)
        {
            if (player.currentTeam == 0 || target.currentTeam == 0)
                return false;

            return player.currentTeam == target.currentTeam;
        }

        private bool API_AreTeamMates(ulong playerId, ulong targetId)
        {
            if (!RelationshipManager._instance.cachedPlayers.ContainsKey(playerId))
                return false;

            ulong teamId1 = RelationshipManager._instance.playerGangs[playerId].teamID;
            if (!RelationshipManager._instance.cachedPlayers.ContainsKey(targetId))
                return false;
            
            ulong teamId2 = RelationshipManager._instance.playerGangs[targetId].teamID;
            
            return teamId2 == teamId1;
        }

        #endregion
    }
}
