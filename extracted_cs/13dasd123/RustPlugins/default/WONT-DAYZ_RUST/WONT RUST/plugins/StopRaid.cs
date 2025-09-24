using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using ru = Oxide.Game.Rust;

namespace Oxide.Plugins
{
    [Info("StopRaid", "FONAREK", "1.0.0")]
    public class StopRaid : RustPlugin
    {
        private void Unload()
        {
            foreach(var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, "StopMAIN");
        }
        private void OnServerInitialized()
        {
            var com = Interface.Oxide.GetLibrary<ru.Libraries.Command>(null);
            
            com.AddChatCommand("sr", this, nameof(chatCommand));
        }

        #region Hooks
        [PluginReference] Plugin ImageLibrary, Clans;
        private List<BasePlayer> PlayerList = new List<BasePlayer>();
        Timer start;
        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if(entity == null || info == null) return;
            var Player = info.InitiatorPlayer;
            if(PlayerList.Contains(Player))
                if(entity is BasePlayer || entity is BuildingBlock)
                    info.damageTypes.ScaleAll(0f);
        }
        private object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            if (planner == null || prefab == null || target.GetWorldPosition() == null || planner.GetOwnerPlayer()?.GetActiveItem() == null) return null;
            var player = planner.GetOwnerPlayer();

            if(PlayerList.Contains(player))
                return false;
            
            return null;
        }
        private void chatCommand(BasePlayer player, string command, string[] args)
        {
            if (string.IsNullOrEmpty(command)) command = "sr";

            if (!player.IsAdmin)
            {
                player.ChatMessage("Вы не админ, идите нахуй!");
                return;
            }
            if (args != null && args.Length > 0)
            {
                if(args.Length == 2)
                {
                    if(args[0] == "start")
                    {
                        var Clan = Clans?.Call<JObject>("GetClan", args[1]);
                        if (Clan == null)
                        {
                            SendReply(player, "Клан не найден!");
                            return;
                        } 

                        var names = "";

                        var members = Clan.GetValue("members") as JArray;
                        foreach (var member in members)
                        {
                            var id = Convert.ToUInt64(member);
                            var Player = BasePlayer.FindByID(id);
                            if (Player == null)
                                continue;

                            if (Player.IsConnected)
                                lockPosition(Player);

                            PlayerList.Add(Player);

                            names = Clans?.Call<string>("GetClanOf", Player);
                        }
                        SendReply(player, $"Вы начали стоп рейд клану {names}!");
                    }
                    else if(args[0] == "stop")
                    {
                        var Clan = Clans?.Call<JObject>("GetClan", args[1]);
                        if (Clan == null)
                        {
                            SendReply(player, "Клан не найден!");
                            return;
                        } 

                        var names = "";

                        var members = Clan.GetValue("members") as JArray;
                        foreach (var member in members)
                        {
                            var id = Convert.ToUInt64(member);
                            var Player = BasePlayer.FindByID(id);
                            if (Player == null)
                                continue;

                            if (Player.IsConnected)
                                UnlockPosition(Player);

                            PlayerList.Remove(Player);

                            names = Clans?.Call<string>("GetClanOf", Player);
                        }
                        SendReply(player, $"Вы закончили стоп рейд клану {names}!");
                    }
                }
            }
            else
                SendReply(player, "<size=16>Неправильный синтаксис!</size>\n/sr stop [Название клана]\n/sr start [Название клана]");
        }

        private void lockPosition(BasePlayer player)
        {
            var position = player.transform.position;
            SendReply(player, "Вы не може двигаться, так как действует стоп рейд!");
            StopMAIN(player); 

            start = timer.Every(0.3f, () =>
            {
                player.MovePosition(position);
            });
        }
        void UnlockPosition(BasePlayer player)
        {
            SendReply(player, "Стоп рейд окончен, можете играть!");
            CuiHelper.DestroyUi(player, "StopMAIN");
            start.Destroy();
        }
        #endregion

        #region GUI

        private void StopMAIN(BasePlayer player) 
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = "0.1110.5 0.1110.5 0.1110.5 0.1110.5", Material = "assets/icons/greyout.mat" },
                RectTransform ={ AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-700.002 0", OffsetMax = "700.998 304" }
            },"Overlay","StopMAIN");

            container.Add(new CuiElement
            {
                Name = "stopText",
                Parent = "StopMAIN",
                Components = 
                {
                    new CuiTextComponent { Text = "Стоп рейд!\nВы не можете двигаться и наносить урон.\nПожалуйста! Подождите пока Администратор выключит его вам", Font = "robotocondensed-regular.ttf", FontSize = 40, Align = TextAnchor.UpperCenter, Color = "1 1 1 0.65" },
                    new CuiOutlineComponent { Color = "1 1 1 0.65", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-640 -125.025", OffsetMax = "640 132.124" }
                }
            });

            CuiHelper.DestroyUi(player, "StopMAIN");
            CuiHelper.AddUi(player, container);
        }

        #endregion
    }
}