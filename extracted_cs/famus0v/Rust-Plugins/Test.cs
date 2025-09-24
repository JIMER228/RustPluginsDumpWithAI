using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Test", "Kira", "1.0.0")]
    [Description("d")]
    public class Test : RustPlugin
    {
        private static Test _;

        public class Huy : MonoBehaviour
        {
            private static BasePlayer player;

            private void Awake()
            {
                player = GetComponentInParent<BasePlayer>();
                InvokeRepeating(nameof(Hren), 1f, 1f);
                _.PrintError($"Load : {player.userID}");
            }

            public void Hren()
            {
                player.Heal(1);
            }

            private void OnDestroy()
            {
                _.PrintError($"Unload : {player.userID}");
            }
        }

        private void OnServerInitialized()
        {
            _ = this;
        }

        private void Unload()
        {
            foreach (var d in asd)
                UnityEngine.Object.Destroy(d.Value);
        }

        public Dictionary<ulong, Huy> asd = new Dictionary<ulong, Huy>();

        [ConsoleCommand("test1")]
        private void SDAsd(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (!asd.ContainsKey(player.userID))
            {
                var obj = player.gameObject.AddComponent<Huy>();
                asd.Add(player.userID, obj);
                _.PrintError($"1 : {player.userID}");
            }
            else
            {
                UnityEngine.Object.Destroy(asd[player.userID]);
                _.PrintError($"2 : {player.userID}");
                asd.Remove(player.userID);
            }
        }
    }
}