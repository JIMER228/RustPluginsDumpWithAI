using System;
using System.Collections.Generic;
using Oxide.Core;
using Physics = UnityEngine.Physics;

namespace Oxide.Plugins
{
    [Info("PoweredTurrets", "Я и Я", "0.0.1")]
    public class PoweredTurrets : RustPlugin
    {
        // vk - vk.com/rustnastroika
        // Forum - topplugin.ru
        
        
        private List<uint> AuthTurret = new List<uint>();
        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (input.WasJustPressed(BUTTON.RELOAD))
            {
                var hitpoints = Physics.RaycastAll(player.eyes.HeadRay(), 2f);
                Array.Sort(hitpoints, (a, b) => a.distance == b.distance ? 0 : a.distance > b.distance ? 1 : -1);
                for (var i = 0; i < hitpoints.Length; i++)
                {
                    var turret = hitpoints[i].collider.GetComponentInParent<AutoTurret>();
                    if (turret != null)
                    {
                        if (!turret.IsAuthed(player))
                        {
                            SendReply(player, "Вы не авторизованы в турели");
                            return;
                        }
                        if (AuthTurret.Contains(turret.net.ID) || turret.IsOnline())
                        {
                            turret.SetIsOnline(false);
                            AuthTurret.Remove(turret.net.ID);
                            SendReply(player, "Вы выключили турель");
                            turret.SendNetworkUpdateImmediate();
                        }
                        else
                        {
                            turret.SetIsOnline(true);
                            AuthTurret.Add(turret.net.ID);
                            SendReply(player, "Вы включили турель");
                            turret.SendNetworkUpdateImmediate();
                        }
                        break;
                    }
                }
            }
        }
        object OnTurretShutdown(AutoTurret turret)
        {
            if (AuthTurret.Contains(turret.net.ID))
            {
                AuthTurret.Remove(turret.net.ID);
                turret.SendNetworkUpdate();
            }
            return null;
        }
    }
}