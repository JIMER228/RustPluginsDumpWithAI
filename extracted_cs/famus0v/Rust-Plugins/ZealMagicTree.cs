using System.Collections.Generic;
using Oxide.Core.Plugins;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealMagicTree", "Kira", "1.0.0")]
    [Description("Event magic tree")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ZealMagicTree : RustPlugin
    {
        #region [References] / [Ссылки]

        [PluginReference] private Plugin ZealMStatistics, ZealKarma;

        #endregion

        #region [Vars] / [Переменные]

        private static ZealMagicTree _;

        private const int TreeTime = 10;
        private const int Stages = 4;
        private const int StageTime = TreeTime / Stages;

        private static readonly Dictionary<int, string> TreeLvl = new Dictionary<int, string>
        {
            [1] = "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/douglas_fir_d.prefab",
            [2] = "assets/bundled/prefabs/autospawn/resource/v3_tundra_forestside/douglas_fir_d_small.prefab",
            [3] = "assets/bundled/prefabs/autospawn/resource/v3_tundra_forestside/pine_d.prefab",
            [4] = "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/pine_a.prefab"
        };

        #endregion

        #region [MonoBehaviours]

        public class MagicTree : MonoBehaviour
        {
            public ulong owner;
            public int lvl;
            public int lifeTime;
            public GameObject go;
            public BaseEntity tree;
            public SphereCollider trigger;
            public Vector3 position;
            public Vector3 ddrawposition;
            public List<BasePlayer> playersInTrigger = new List<BasePlayer>();

            private void Start()
            {
                go = gameObject;
                go.layer = (int) Layer.Reserved1;
                go.transform.position = position;
                ddrawposition = new Vector3(position.x, position.y + 1.5f, position.z);
                SpawnTrigger();
                InvokeRepeating(nameof(Handler), 1f, 1f);
            }

            public void Handler()
            {
                DDrawRefresh();
                if (lvl == Stages) _.ZealKarma?.Call("GiveKarma", owner, 0.6f);
                lifeTime++;
                if (tree.IsDestroyed) Destroy(this);
                if ((StageTime * lvl) < lifeTime)
                {
                    if (lvl >= Stages) return;
                    lvl++;
                    RefreshTree();
                }
            }

            public void SpawnTrigger()
            {
                trigger = go.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 3f;
                trigger.name = $"{owner}";
                trigger.transform.position = position;
            }

            public void RefreshTree()
            {
                tree.Kill();
                tree.SendNetworkUpdate();
                tree = GameManager.server.CreateEntity(TreeLvl[lvl]);
                tree.ServerPosition = position;
                tree.Spawn();
                tree.SendNetworkUpdate();
            }

            private void OnTriggerEnter(Collider other)
            {
                var ent = other.ToBaseEntity();
                if (ent == null) return;
                if (!ent.IsValid()) return;
                if (!(ent is BasePlayer)) return;
                var player = (BasePlayer) ent;
                if (playersInTrigger.Contains(player)) return;
                playersInTrigger.Add(player);
            }

            private void OnTriggerExit(Collider other)
            {
                var ent = other.ToBaseEntity();
                if (ent == null) return;
                if (!ent.IsValid()) return;
                if (!(ent is BasePlayer)) return;
                var player = (BasePlayer) ent;
                if (!playersInTrigger.Contains(player)) return;
                playersInTrigger.Remove(player);
            }

            private void DDrawRefresh()
            {
                if (playersInTrigger.Count == 0) return;
                foreach (var player in playersInTrigger)
                {
                    player.SendConsoleCommand("ddraw.text", 1f, Color.white, ddrawposition,
                        $"<size=30>{GetStage()}</size>");
                }
            }

            private string GetStage()
            {
                var stage = $"{lvl} стадия из {Stages}";
                return lvl == Stages ? "+10 кармы/мин ▲" : stage;
            }

            private void OnDestroy()
            {
                tree.Kill();
                Destroy(trigger);
                tree.SendNetworkUpdate();
            }
        }

        #endregion
 
        #region [Hooks] / [Крюки]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            _ = this;
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            var ent = go.ToBaseEntity();
            if (ent == null) return;
            if (ent.skinID != 5) return;
            SpawnTree(plan.GetOwnerPlayer().userID, ent, ent.ServerPosition);
        }

        #endregion
 
        #region [Commands] / [Команды]

        [ConsoleCommand("give.tree")]
        // ReSharper disable once UnusedMember.Local
        private void GiveTree(ConsoleSystem.Arg args)
        {
            var item = ItemManager.CreateByName("seed.hemp", 10, 5);
            args.Player().GiveItem(item);
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private static void SpawnTree(ulong owner, BaseEntity tree, Vector3 position)
        {
            var component = new GameObject().AddComponent<MagicTree>();
            component.owner = owner;
            component.tree = tree;
            component.position = position;
        }

        #endregion
    }
}