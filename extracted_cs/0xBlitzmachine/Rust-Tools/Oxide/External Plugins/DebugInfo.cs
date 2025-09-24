using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Debug Info", "Billy Joe", "1.0.1")]
    [Description("A plugin that gives debug information for developers.")]

    public class DebugInfo : CovalencePlugin
    {
        private void GetInfo(BasePlayer player, BaseEntity ent)
        {
            player.ConsoleMessage("--------------------| <color=#fff>Object Info</color> |--------------------");
            GetObjectInfo(player, ent);
            GetComponents(player, ent);
        }

        private void GetObjectInfo(BasePlayer player, BaseEntity ent)
        {
            var message =
                $" >> Prefab Shortname: <color=#fff>{ent.ShortPrefabName}</color>\n" +
                $" >> Prefab Location: <color=#fff>{ent.name}</color>\n" +
                $" >> Prefab ID: <color=#fff>{ent.prefabID}</color>\n" +
                $"\n >> Position:\n" +
                $" X: <color=#fff>{ent.transform.position.x}</color>\n" +
                $" Y: <color=#fff>{ent.transform.position.y}</color>\n" +
                $" Z: <color=#fff>{ent.transform.position.z}</color>\n" +
                $" Local: <color=#fff>{ent.transform.localPosition}</color>\n" +
                $" Normalized: <color=#fff>{ent.transform.position.normalized}</color>\n" +
                $" Magnitude: <color=#fff>{ent.transform.position.magnitude}</color>\n" +
                $"\n >> Rotation: \n" +
                $" X: <color=#fff>{ent.transform.rotation.x}</color>\n" +
                $" Y: <color=#fff>{ent.transform.rotation.y}</color>\n" +
                $" Z: <color=#fff>{ent.transform.rotation.z}</color>\n" +
                $" Local: <color=#fff>{ent.transform.localRotation}</color>\n" +
                $" Euler: <color=#fff>{ent.transform.rotation.eulerAngles}</color>\n" +
                $"\n >> Bounds:\n" +
                $" Center: <color=#fff>{ent.bounds.center}</color>\n" +
                $" Extents: <color=#fff>{ent.bounds.extents}</color>\n" +
                $" Size: <color=#fff>{ent.bounds.size}</color>\n" +
                $"\n >> Network ID: <color=#fff>{ent.net.ID}</color>\n" +
                $" >> Layer: <color=#fff>{LayerMask.LayerToName(ent.gameObject.layer)} ({ent.gameObject.layer})</color>\n" +
                $" >> Distance: <color=#fff>{player.Distance(ent)}</color>\n" +
                $" >> Category: <color=#fff>{ent.GetType()}</color>\n" +
                $" >> Health: <color=#fff>{Math.Round(ent.Health())}/{ent.MaxHealth()}</color>\n" +
                $" >> Local Scale: <color=#fff>{ent.transform.localScale}</color>\n";

            player.ConsoleMessage(message);
        }

        private string GetInherantance(Component comp)
        {
            List<string> temp = new List<string>();
            StringBuilder sb = new StringBuilder();
            sb.Append("(<color=#7d8971>");
            for (var current = comp.GetType(); current != null; current = current.BaseType)
            {
                if (current.Name.Contains("Mono") || current.Name.Contains("Component")) break;
                temp.Add(current.Name);
            }

            foreach (var current in temp)
            {
                if (temp.LastOrDefault() == current)
                    sb.Append(" " + current + " ");
                else
                    sb.Append(" " + current + " -> ");
            }

            sb.Append("</color>)");
            temp.Clear();
            return sb.ToString();
        }

        private void GetComponents(BasePlayer player, BaseEntity ent)
        {
            player.ConsoleMessage("<br>");
            player.ConsoleMessage("--------------------| <color=#fff>Components</color> |--------------------");
            foreach (var comp in ent.GetComponents(typeof(Component)))
            {
                player.ConsoleMessage($" >> <color=#fff>{comp.GetType().Name} -------- {GetInherantance(comp)}</color>");
            }
        }

        #region Commands
        [Command("getinfo")]
        void GetInfoCMD(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (player == null || !player.IsAdmin) return;

            RaycastHit hit;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f))
            {
                BaseEntity entity = hit.GetEntity();
                if (entity == null) return;

                GetInfo(player, entity);
            }
        }
        #endregion

    }
}
