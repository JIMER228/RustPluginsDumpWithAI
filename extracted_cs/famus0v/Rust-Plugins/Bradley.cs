using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Bradley", "Kira", "1.0.0")]
    [Description("Нормальный танк")]
    public class Bradley : RustPlugin
    {
        public Dictionary<BasePlayer, BradleyCore> Cores = new Dictionary<BasePlayer, BradleyCore>();
        private const string CHAIR_PREFAB = "assets/bundled/prefabs/static/chair.invisible.static.prefab";
        private static Bradley _;

        public class BradleyCore : MonoBehaviour
        {
            public BasePlayer Player;
            public BradleyAPC BradleyApc;
            public BaseChair Seat;

   
            private void Awake()
            {
                BradleyApc = GetComponent<BradleyAPC>();
                BradleyApc.enabled = false;
                BradleyApc.CancelInvoke(BradleyApc.UpdateTargetList);
                BradleyApc.CancelInvoke(BradleyApc.UpdateTargetVisibilities);
            }

            private void Start()
            {
                
                SpawnChair();
                _.Cores.Add(Player, this);
            }

            private void FixedUpdate() 
            {
                var Ptransform = Player.eyes.GetLookRotation().eulerAngles;
                BradleyApc.CannonMuzzle.LookAt(Ptransform);
                BradleyApc.turretAimVector = Ptransform;
                BradleyApc.AimWeaponAt(BradleyApc.mainTurret, BradleyApc.coaxPitch, Ptransform, 1, 1);
                BradleyApc.AimWeaponAt(BradleyApc.mainTurret, BradleyApc.CannonPitch, Ptransform, 1, 1);
                BradleyApc.SendNetworkUpdate();
                Player.transform.position = new Vector3(0, 3.5f, 0); 
                Player.ChatMessage(Ptransform.ToString());
            }

            private void SpawnChair() 
            {
                Player.SetParent(BradleyApc, 0);   
                Player.Command("client.camoffset", new object[] { new Vector3(0, 3.5f, 0) });     
                Player.spectateFilter = "@123nofilter123";
                Player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true); 
                Player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, true);
                Player.gameObject.SetLayerRecursive(10); 
                Player.CancelInvoke("InventoryUpdate");
            }

            private void OnDestroy()
            {
                Player.SetParent(null);
                Player.spectateFilter = string.Empty;
                Player.SetParent(null);
                Player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
                Player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
                Player.gameObject.SetLayerRecursive(17);
                Player.InvokeRepeating("InventoryUpdate", 1f, 0.1f * UnityEngine.Random.Range(0.99f, 1.01f));
                Player.Command("client.camoffset", new object[] { new Vector3(0, 1.2f, 0) });
            }
        }

        private void StartSpectating(BasePlayer player, BradleyCore controller, bool isOperator)
        {
            // player.spectateFilter = "@123nofilter123";
            // player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true); 
            // player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, true);
            // player.gameObject.SetLayerRecursive(10);
            // player.CancelInvoke("InventoryUpdate");
            player.SendNetworkUpdateImmediate();
 
            timer.In(0.5f, () =>
            { 
                player.transform.position = controller.transform.position;
                player.SetParent(controller.BradleyApc, 0);
                // player.Command("client.camoffset", new object[] { new Vector3(0, 3.5f, 0) });
            });
        }
        private void EndSpectating(BasePlayer player, BradleyCore commander, bool isOperator)
        {
            player.spectateFilter = string.Empty;
            player.SetParent(null);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
            player.gameObject.SetLayerRecursive(17);
            player.InvokeRepeating("InventoryUpdate", 1f, 0.1f * UnityEngine.Random.Range(0.99f, 1.01f));
            player.Command("client.camoffset", new object[] { new Vector3(0, 1.2f, 0) });
            player.transform.position = commander.transform.position + Vector3.up + (commander.transform.right * 3);

        }
        private void OnServerInitialized()
        {
            _ = this;
        }

        private void Unload()
        {
            foreach (var core in Cores) UnityEngine.Object.Destroy(core.Value);
        }

        [ChatCommand("bradley")]
        private void SpawnBradley(BasePlayer player)
        {
            var bradley = GameManager.server.CreateEntity("assets/prefabs/npc/m2bradley/bradleyapc.prefab");
            bradley.transform.SetPositionAndRotation(player.transform.position, player.eyes.rotation);
            bradley.Spawn();
            var component = bradley.gameObject.AddComponent<BradleyCore>();
            component.Player = player;
        }
    }
}