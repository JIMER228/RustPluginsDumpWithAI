using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Oxide.Plugins
{
    [Info("ZealTreasure", "Kira", "1.0.0")]
    [Description("Клад для сервера Rust")]
    public class ZealTreasure : RustPlugin
    {
        public List<Vector3> Zones = new List<Vector3>();
        public Coroutine Thread;
        public static ZealTreasure _;
        private static readonly int playerLayer = LayerMask.GetMask("Deployed");
        private static readonly Collider[] colBuffer = Vis.colBuffer;

        public class TreasureDetector : MonoBehaviour
        {
            private BasePlayer player;
            private BaseEntity chest;

   
            private void Awake() 
            {
                player = GetComponent<BasePlayer>();

                InvokeRepeating("Find_Chest", 1f, 1f);
            }

            public void Find_Chest()
            {
                var chests = Physics.OverlapSphereNonAlloc(player.transform.position, 50, colBuffer, playerLayer,
                    QueryTriggerInteraction.Collide);

                if (chests <= 0) return;
                for (int i = 0; i < chests; i++)
                {
                    var obj = colBuffer[i].GetComponentInParent<BaseEntity>();
                    if (obj.skinID == 2)
                    {
                        chest = obj;
                    } 
                }

                if (chest == null) return;
                for (int i = 0; i < 50 / player.Distance2D(chest); i++)
                {
                    Effect Sound = new Effect("assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab", player, 0,
                        new Vector3(),
                        new Vector3());
                    EffectNetwork.Send(Sound, player.Connection);


                    player.ChatMessage($"{player.Distance2D(chest)}");
                }

                if (player.Distance2D(chest) < 5)
                {
                    Effect Sound1 = new Effect("assets/prefabs/locks/keypad/effects/lock.code.shock.prefab", player,
                        0,
                        new Vector3(),
                        new Vector3());
                    EffectNetwork.Send(Sound1, player.Connection);
                }
            }

            private void OnDestroy()
            {
            }
        }


        private void Spawn_TreasureChest()
        {
            int Radius = 10;
            if (Thread != null)
            {
                Global.Runner.StopCoroutine(Thread);
            }

            Thread = Global.Runner.StartCoroutine(Generate_TreasureChest(Radius));
        }

        private void OnServerInitialized()
        {
            _ = this;
            foreach (var obj in BasePlayer.activePlayerList)
            {
                UnityEngine.Object.Destroy(obj.gameObject.GetComponent<TreasureDetector>());
            }
        }

        private void Unload()
        {
            if (Thread != null)
            {
                Global.Runner.StopCoroutine(Thread);
            }

            foreach (var obj in BasePlayer.activePlayerList)
            {
                UnityEngine.Object.Destroy(obj.gameObject.GetComponent<TreasureDetector>());
            }
        }
 
        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (newItem != null || oldItem != null)
            {
                if (newItem.info.shortname == "geiger.counter") 
                {
                    if (player.gameObject.GetComponent<TreasureDetector>() == null)
                    {
                        player.gameObject.AddComponent<TreasureDetector>(); 
                        return;
                    }
                }
            }

            if (player.GetComponent<TreasureDetector>() != null)
                UnityEngine.Object.Destroy(player.GetComponent<TreasureDetector>());
        }

        IEnumerator Generate_TreasureChest(int radius)
        {
            foreach (var position in Zones)
            {
                for (int i = 0; i < 1; i++)
                {
                    Vector3 pos = new Vector3(position.x + Core.Random.Range(-radius, radius), 0,
                        position.z + Core.Random.Range(-radius, radius));
                    var ent = GameManager.server.CreateEntity(
                        "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab");
                    ent.transform.position = new Vector3(pos.x, TerrainMeta.HeightMap.GetHeight(pos) - 1, pos.z);
                    ent.transform.hasChanged = true;
                    ent.GetComponent<BaseEntity>().skinID = 2;
                    ent.Spawn();
                    Server.Command($"say Заспавнен сундук {pos.x}");
                    yield return new WaitForSeconds(1f);
                }
            }

            yield return 0;  
        }

        [ConsoleCommand("set.spawnpoint")]
        private void GiveDetector(ConsoleSystem.Arg args)
        {
            Zones.Add(args.Player().transform.position);
            SendReply(args.Player(), $"Точка спавна установлена : {args.Player().transform.position}");
            Spawn_TreasureChest();
        }
 
        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            } 

            var str = hex.Trim('#');
            if (str.Length == 6) 
                str += "FF";
            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        
        [ConsoleCommand("dist")]
        private void DistDetector(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            CuiElementContainer UI = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "UI_Layer");

            UI.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image =
                {
                    Color = HexToRustFormat("#8B85801C"),
                    Material = "assets/content/ui/ui.background.tile.psd"
                },
                RectTransform = 
                {
                    AnchorMin = "0.8156254 0.02222205",
                    AnchorMax = "0.835417 0.174"
                }
            },"Hud", "UI_Layer");

            UI.Add(new CuiElement
            {
                Name = "IC_Fuel",
                Parent = "UI_Layer",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#8B8580"),
                        Sprite = "assets/icons/iscooking.png"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1842117 0.03660318",
                        AnchorMax = "0.8421105 0.1891164"
                    }
                }
            });

            CuiHelper.AddUi(player, UI);
        }
    }
}