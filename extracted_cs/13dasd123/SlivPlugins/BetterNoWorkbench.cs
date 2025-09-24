// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿/*
 * Tryhard Plugins © 2023
 * File can not be copied, modified and/or distributed without the express permission from Tryhard
 * For support join our discord - https://discord.gg/YnbYaugRMh
 */


// Reference: 0Harmony
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Better No Workbench", "Tryhard", "1.3.3")]
    [Description("Adds workbench access to players with permission")]
    public class BetterNoWorkbench : RustPlugin
    {
        #region Fields

        private const string InstanceId = "com.Tryhard.BetterNoWorkbench";
        private static HarmonyInstance _harmony;
        private TriggerWorkbench trigger;
        static BetterNoWorkbench instance;
        ListHashSet<BasePlayer> benchedPlayers = new ListHashSet<BasePlayer>();

        #endregion

        private void Init()
        {
            permission.RegisterPermission("BetterNoWorkbench.on", this);
        }

        #region Oxide Hooks 

        private void OnServerInitialized()
        {
            instance = this;

            trigger = new GameObject().AddComponent<TriggerWorkbench>();

            if (_harmony == null)
                _harmony = HarmonyInstance.Create(InstanceId);

            _harmony.PatchAll();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (permission.UserHasPermission(player.UserIDString, "BetterNoWorkbench.on"))
                {
                    AddWorkbench(player);
                }
            }
        }
        
        private void Unload()
        {
            _harmony.UnpatchAll(InstanceId);

            foreach (BasePlayer player in benchedPlayers)
            {
                if (player != null) RemoveWorkbench(player);
            }

            instance = null;
            benchedPlayers.Clear();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "BetterNoWorkbench.on"))
            {
                AddWorkbench(player);
            }
        }

        private void AddWorkbench(BasePlayer player)
        {
            player.nextCheckTime = float.MaxValue;
            player.cachedCraftLevel = 3f;
            player.EnterTrigger(trigger);
            player.SendNetworkUpdateImmediate();
            if (!benchedPlayers.Contains(player)) benchedPlayers.Add(player);
        }

        private void RemoveWorkbench(BasePlayer player)
        {
            player.LeaveTrigger(trigger);
            player.nextCheckTime = float.MaxValue;
            player.cachedCraftLevel = 0f;
            player.SendNetworkUpdateImmediate();
            if (benchedPlayers.Contains(player)) benchedPlayers.Remove(player);
        }

        #endregion

        #region Harmony

        [HarmonyPatch(typeof(TriggerWorkbench), nameof(TriggerWorkbench.OnEntityEnter))]
        public static class TriggerWorkbench_Enter_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix(TriggerWorkbench __instance, BaseEntity ent)
            {
                if (ent != null)
                {
                    var player = ent as BasePlayer;
                    if (player != null)
                    {
                        if (instance.benchedPlayers.Contains(player))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(TriggerWorkbench), nameof(TriggerWorkbench.OnEntityLeave))]
        public static class TriggerWorkbench_Leave_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix(TriggerWorkbench __instance, BaseEntity ent)
            {
                if (ent != null)
                {
                    var player = ent as BasePlayer;
                    if (player != null)
                    {
                        if (instance.benchedPlayers.Contains(player))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        #endregion
    }
}