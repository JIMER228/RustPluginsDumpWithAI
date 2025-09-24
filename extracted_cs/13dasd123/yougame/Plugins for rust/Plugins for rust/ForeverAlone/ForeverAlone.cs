using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ForeverAlone", "S1m0n", "1.0")]

    class ForeverAlone : RustPlugin
    {
        static float AroundRadius = 10f; // радиус зоны для проверки
        static float CheckInterval = 5f; // частота проверок
        static float timeBeforeShock = 60f; // сколько секунд разрешено находиться свыше лимита
        static float DamagePerTime = 50f; // кол-во урона каждый интервал 

        private readonly FieldInfo whitelistPlayersField = typeof(CodeLock).GetField("whitelistPlayers", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly FieldInfo guestPlayersField = typeof(CodeLock).GetField("guestPlayers", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly int playerLayer = LayerMask.GetMask("Player (Server)");
        private static readonly Collider[] colBuffer = (Collider[])typeof(Vis).GetField("colBuffer", (BindingFlags.Static | BindingFlags.NonPublic))?.GetValue(null);

        static int MaxAllowedPlayers = 2;

        void Loaded()
        {
            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                CheckComponent(player);
            }

            MaxAllowedPlayers = Config.Get<int>("MaxAllowedPlayers");

            Puts("Max allowed players in team: " + MaxAllowedPlayers);
        }

        protected override void LoadDefaultConfig()
        {
            Config["MaxAllowedPlayers"] = 2;
        }

        void Unload()
        {
            foreach (AroundTimer arTimer in Resources.FindObjectsOfTypeAll<AroundTimer>())
            {
                UnityEngine.Object.Destroy(arTimer);
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            CheckComponent(player);
        }

        void CheckComponent(BasePlayer player)
        {
            AroundTimer arTimer = player.GetComponent<AroundTimer>();

            if (!arTimer)
            {
                player.gameObject.AddComponent<AroundTimer>();
            }
        }

        class AroundTimer : MonoBehaviour
        {
            float ElaspedSeconds;
            BasePlayer player;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                InvokeRepeating("CheckAround", CheckInterval, CheckInterval);
            }

            void CheckAround()
            {
                if (!player.IsConnected)
                {
                    Destroy(this);
                    return;
                }

                if (player.IsDead() || player.IsSleeping()) return;

                int entities = Physics.OverlapSphereNonAlloc(player.transform.position, AroundRadius, colBuffer, playerLayer);

                int playersAround = 0;

                for (var i = 0; i < entities; i++)
                {
                    var player = colBuffer[i].GetComponentInParent<BasePlayer>();

                    if (player != null && (player == this.player || !player.IsDead() && !player.IsSleeping() && IsVisible(player, this.player.eyes.position, player.eyes.position)))
                    {
                        playersAround++;           
                    }
                }

                if(playersAround > MaxAllowedPlayers)
                {
                    ElaspedSeconds += CheckInterval;

                    if (ElaspedSeconds >= timeBeforeShock)
                    {
                        player.ChatMessage($"Вы превышаете лимит совместной игры.\nРазрешено не более {MaxAllowedPlayers} человек в команде.");
                        DoShock(player);
                    }
                }
                else
                {
                    ElaspedSeconds = Mathf.Max(0, ElaspedSeconds - CheckInterval); 
                }
            }
        }

        int GetLimit => MaxAllowedPlayers;

        static void DoShock(BasePlayer player)
        {
            player.Hurt(DamagePerTime, DamageType.ElectricShock, player, false);
            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.shock.prefab", player, 0, Vector3.zero, Vector3.forward, null, false);
        }

        static bool IsVisible(BasePlayer player, Vector3 source, Vector3 dest) => player.IsVisible(source, dest);
    }
}
