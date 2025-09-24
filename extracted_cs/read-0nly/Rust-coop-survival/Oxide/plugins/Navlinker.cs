
#region using
using Convert = System.Convert;
using Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
using Oxide.Core.Plugins;
using Oxide.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using Rust.Ai;
using Oxide.Ext.RustEdit;
using Oxide.Ext.RustEdit.NPC;
#endregion
namespace Oxide.Plugins
{
    [Info("Navlinker", "obsol", "0.2.1")]
    [Description("Makes NPCs and Animals attack any NPC/animal that doesn't share their prefab name")]
    public class Navlinker : CovalencePlugin
    {
        static BasePlayer Spectator = null;
        Vector3 pt1v;
        Vector3 pt2v;
        GameObject offmesh;
        OffMeshLink omLink;

        void OnTerrainInitialized()
        {
            createOffmeshes();
			createOffmesh(new Vector3(-11.55f,11.44f,-14.98f),new Vector3(-11.76f,4.85f,-19.45f),false,true,true);

        }

        /*
    assets/content/structures/sewers/sewer_tunnel_300_high_ladder.prefab 
    assets/content/structures/sewers/sewer_tunnel_300_ladder.prefab
    -1.5,0.4,0<>-1.5,TERRAIN.Y,0 (Bi)
        */
        struct OffmeshMetadata
        {
            public string[] prefabs;
            public Vector3 a;
            public Vector3 b;
            public bool bidirectional;
            public bool terrainAY;
            public bool terrainBY;
            public OffmeshMetadata(string[] s, Vector3 a1, Vector3 b1, bool isBidirectional = false, bool tAY = false, bool tBY = false)
            {
                prefabs = s; a = a1; b = b1; bidirectional = isBidirectional;
                terrainAY = tAY; terrainBY = tBY;
            }
        }
        struct NavmeshObstructMetadata
        {
            public string[] prefabs;
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale;
            public NavmeshObstructMetadata(string[] s, Vector3 pos, Vector3 rot, Vector3 scl)
            {
                prefabs = s;
                position = pos; rotation = rot;scale = scl;

            }
        }
        List<OffmeshMetadata> metadata = new List<OffmeshMetadata>();        
        List<NavmeshObstructMetadata> obstructions = new List<NavmeshObstructMetadata>();
        public void setupMetadata()
        {
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/content/structures/sewers/sewer_tunnel_300_high_ladder.prefab",
                "assets/content/structures/sewers/sewer_tunnel_300_ladder.prefab"
            }, new Vector3(-1.50f, 0.40f, 0f), new Vector3(-1.50f, 0f, 0f), true, false, true));
            /*
             * 
    assets/content/structures/train_tunnels/entrance_monuments_a.prefab
    assets/content/structures/train_tunnels/corridor_prefabs/modules/corridor_train_tunnel_entrance_a.prefab
    assets/content/structures/train_tunnels/corridor_prefabs/corridor_train_tunnel_entrance_a.prefab
    8.75,0.25,4.5<>13.75,TERRAIN.Y,4.5 (Bi)
    1.5,0.25,11.5<>1.5,TERAIN.Y,16.5 (Bi)
    -3.5,6.5,8.75>-3.5,0.25,10 (Jump)
             * */
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/content/structures/train_tunnels/entrance_monuments_a.prefab",
                "assets/content/structures/train_tunnels/corridor_prefabs/modules/corridor_train_tunnel_entrance_a.prefab",
                "assets/content/structures/train_tunnels/corridor_prefabs/corridor_train_tunnel_entrance_a.prefab"
            }, new Vector3(8.75f, 0.25f, 4.5f), new Vector3(-13.75f, 0f, 4.5f), true, false, true));

            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/content/structures/train_tunnels/entrance_monuments_a.prefab",
                "assets/content/structures/train_tunnels/corridor_prefabs/modules/corridor_train_tunnel_entrance_a.prefab",
                "assets/content/structures/train_tunnels/corridor_prefabs/corridor_train_tunnel_entrance_a.prefab"
            }, new Vector3(1.5f, 0.25f, 11.5f), new Vector3(1.5f, 0f, 16.5f), true, false, true));


            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/content/structures/train_tunnels/entrance_monuments_a.prefab",
                "assets/content/structures/train_tunnels/corridor_prefabs/modules/corridor_train_tunnel_entrance_a.prefab",
                "assets/content/structures/train_tunnels/corridor_prefabs/corridor_train_tunnel_entrance_a.prefab"
            }, new Vector3(-3.5f, 6.5f, 8.75f), new Vector3(-3.5f, 0.25f, 10f), false, false, false));

            /*
             * 
             * 
    assets/content/structures/train_tunnels/entrance_monuments_b.prefab
    5,6.75,-4.75  > 5.75,0.25,-4.75
    8,0.25,0 <> 13,TERRAIN.y,0
    0,0.25,8 <> 13,TERRAIN.y,0
             * 
             */

            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/content/structures/train_tunnels/entrance_monuments_b.prefab"
            }, new Vector3(8f, 0.25f, 0f), new Vector3(13f, 0f, 0f), true, false, true));
            
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/bundled/prefabs/autospawn/monument/offshore/oilrig_1.prefab"
            }, new Vector3(-13.45f, 5.4f, -2.25f), new Vector3(-14.6f, 1.125f, -2.25f), true, true, true));
			/*
			
			*/
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/content/structures/train_tunnels/entrance_monuments_b.prefab"
            }, new Vector3(0f, 0.25f, 8f), new Vector3(0f, 0f, 13f), true, false, true));
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/content/structures/train_tunnels/entrance_monuments_b.prefab"
            }, new Vector3(0f, 0.25f, -8f), new Vector3(0f, 0f, -13f), true, false, true));
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/content/structures/train_tunnels/entrance_monuments_b.prefab"
            }, new Vector3(-3.25f, 0.25f, 2.5f), new Vector3(-3.25f,-1.5f, -2.75f), true, false, false));

            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/content/structures/train_tunnels/entrance_monuments_b.prefab"
            }, new Vector3(5f, 6.75f, -4.75f), new Vector3(5.75f, 0.25f, -4.75f), false, false, false));
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/content/structures/train_tunnels/entrance_monuments_b.prefab"
            }, new Vector3(3.45f, 0.25f, 2f), new Vector3(4f, -4.25f, 1f), false, false, false));


            /*
             * 
    assets/prefabs/building/watchtower.wood/watchtower.wood.prefab
    1.75,7.25,-0.5 = 2.25,TERRAIN.Y,-0.5
             */

            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/prefabs/building/watchtower.wood/watchtower.wood.prefab"
            }, new Vector3(1.75f,7.25f,-0.5f), new Vector3(2.25f, 0f, -0.5f), true, false, true));

            /*
             * 
    assets/content/structures/watchtower_a/watchtower_a.prefab
    1.35,5.75,-0.5 = 1.75,TERRAIN.Y,-0.5
             */

            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/content/structures/watchtower_a/watchtower_a.prefab"
            }, new Vector3(1.35f,5.75f,-0.5f), new Vector3(1.75f, 0f, -0.5f), true, false, true));
        }

        public void setupMonumentMetadata()
        {
            /*
    Bandit Town
            assets/bundled/prefabs/autospawn/monument/medium/bandit_town.prefab
    Obstacle: 0,-0.3,0 : 0,0,0 : 150,1,150
    34,1.85,-17.25 = 31.5,TERRAIN.Y,-14.75
    40.75,11.3,-21.25 = 40.25,7.0,-21
    47.25,11.6,-14.25 = 47.13,7,-14.14
             */

            /*obstructions.Add(new NavmeshObstructMetadata(new string[] {
                "assets/bundled/prefabs/autospawn/monument/medium/bandit_town.prefab"
            }, new Vector3(0f, -0.3f, 0f), new Vector3(0f, 0f, 0f), new Vector3(150, 1, 150)));*/
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/bundled/prefabs/autospawn/monument/medium/bandit_town.prefab"
            }, new Vector3(34.0f, 1.850f, -17.250f), new Vector3(31.5f, 0f, 0f), true, false, true));
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/bundled/prefabs/autospawn/monument/medium/bandit_town.prefab"
            }, new Vector3(40.75f,11.3f,-21.25f), new Vector3(40.25f,7,-21), true, false, false));
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/bundled/prefabs/autospawn/monument/medium/bandit_town.prefab"
            }, new Vector3(47.25f, 11.6f, -14.25f), new Vector3(47.13f, 7f, -14.14f), true, false, false));
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/bundled/prefabs/autospawn/monument/medium/bandit_town.prefab"
            }, new Vector3(19.4f, 0f, 0), new Vector3(17.85f, 2.75f, -0.85f), true, false, false));
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/bundled/prefabs/autospawn/monument/medium/bandit_town.prefab"
            }, new Vector3(3.25f, 5f, -27.5f), new Vector3(3.25f, 1.75f, -28.5f), true, false, false));
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/bundled/prefabs/autospawn/monument/medium/bandit_town.prefab"
            }, new Vector3(-1.75f, 5f, -24.15f), new Vector3(-1.216f, 6f, -24.75f), false, false, false));
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/bundled/prefabs/autospawn/monument/medium/bandit_town.prefab"
            }, new Vector3(5.85f, 5f, -24f), new Vector3(5f, 6f, -24.75f), false, false, false));
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/bundled/prefabs/autospawn/monument/medium/bandit_town.prefab"
            }, new Vector3(1.5f, 9.5f, -24.6f), new Vector3(1.75f, 6f, -24.75f), false, false, false));

            /*
    Compound
            assets/bundled/prefabs/autospawn/monument/medium/compound.prefab
    Obstacle: -55,-0.6,84.5 : 0,0,0, : 22,1,22
    4.75,10.75,-22.75 = 4.25,15.25,-22.75
    -2.75,4.45,-1.25 = -2.75,10.7,-2
    -4.25,0.25,0.25 = -3.75,4.75,0.25
             */
            obstructions.Add(new NavmeshObstructMetadata(new string[] {
                "assets/bundled/prefabs/autospawn/monument/medium/compound.prefab"
            }, new Vector3(-55, -0.6f, 84.5f), new Vector3(0f, 0f, 0f), new Vector3(22, 1, 22)));
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/bundled/prefabs/autospawn/monument/medium/compound.prefab"
            }, new Vector3(4.75f, 10.75f, -22.75f), new Vector3(4.25f, 15.25f, -22.75f), true, false, false));
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/bundled/prefabs/autospawn/monument/medium/compound.prefab"
            }, new Vector3(-2.75f, 4.45f, -1.25f), new Vector3(-2.75f, 10.7f, -2f), true, false, false));
            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/bundled/prefabs/autospawn/monument/medium/compound.prefab"
            }, new Vector3(-4.25f, 0.25f, 0.25f), new Vector3(-3.75f, 4.75f, 0.25f), true, false, false));
            /*
             * 
            obstructions.Add(new NavmeshObstructMetadata(new string[] {
                "assets/bundled/prefabs/autospawn/monument/fishing_village/fishing_village_a.prefab",
                "assets/bundled/prefabs/autospawn/monument/fishing_village/fishing_village_b.prefab",
                "assets/bundled/prefabs/autospawn/monument/fishing_village/fishing_village_c.prefab"
            }, new Vector3(0f, -10f, 0f), new Vector3(0f, 0f, 0f), new Vector3(150, 20, 150)));
            obstructions.Add(new NavmeshObstructMetadata(new string[] {
                "assets/bundled/prefabs/autospawn/monument/harbor/harbor_1.prefab",
                "assets/bundled/prefabs/autospawn/monument/harbor/harbor_2.prefab"
            }, new Vector3(0f, -10f, 0f), new Vector3(0f, 0f, 0f), new Vector3(400, 20, 400)));
             * */

            metadata.Add(new OffmeshMetadata(new string[] {
                "assets/bundled/prefabs/autospawn/monument/large/military_tunnel_1.prefab"
            }, new Vector3(-6f, 09f, -10f), new Vector3(-6f, 09f, -18f), true, false, false));
        }

        /*
         * 
         * */
        [Command("Fullscan")]
        void Fullscan(IPlayer player, string command, string[] args)
        {
            player.Message("Creating offmesh links");
            createOffmeshes();
            player.Message("Created!");
        }
        public void createOffmeshes()
        {
            setupMetadata();
            setupMonumentMetadata();
            UnityEngine.Object[] OffmeshGameobjectList = Resources.FindObjectsOfTypeAll(typeof(GameObject));
            List<GameObject> offmeshGameObjects = new List<GameObject>();
            List<string> offmeshGameObjectNames = new List<string>();
            UnityEngine.Object[] navObsGameobjectList = Resources.FindObjectsOfTypeAll(typeof(GameObject));
            List<GameObject> navObsGameObjects = new List<GameObject>();
            List<string> navObsGameObjectNames = new List<string>();
            foreach (OffmeshMetadata offmeshMetadata in metadata)
            {
                foreach (string s in offmeshMetadata.prefabs)
                {
                    if (!offmeshGameObjectNames.Contains(s)) { offmeshGameObjectNames.Add(s); }
                }
            }
            foreach (NavmeshObstructMetadata offmeshMetadata in obstructions)
            {
                foreach (string s in offmeshMetadata.prefabs)
                {
                    if (!navObsGameObjectNames.Contains(s)) { navObsGameObjectNames.Add(s); }
                }
            }
            foreach (GameObject gameObject in OffmeshGameobjectList)
            {
                foreach (string s in offmeshGameObjectNames)
                {
                    if (gameObject.name.Contains(s))
                    {
                        offmeshGameObjects.Add(gameObject);
                        break;
                    }
                }
            }
            foreach (GameObject gameObject in navObsGameobjectList)
            {
                foreach (string s in navObsGameObjectNames)
                {
                    if (gameObject.name.Contains(s))
                    {
                        navObsGameObjects.Add(gameObject);
                        break;
                    }
                }
            }
            foreach (GameObject go in offmeshGameObjects)
            {
                foreach (OffmeshMetadata offmeshMetadata in metadata)
                {
                    bool create = false;
                    foreach (string s in offmeshMetadata.prefabs)
                    {
                        if (go.name.Contains(s)) { create = true; }
                    }
                    if (create)
                    {
                        Vector3 pt1;
                        Vector3 pt2;
                        pt1 = go.transform.TransformPoint(offmeshMetadata.a);
                        pt2 = go.transform.TransformPoint(offmeshMetadata.b);
                        Puts("Creating offmesh for " + go.transform.name);
                        createOffmesh(pt1, pt2, offmeshMetadata.bidirectional, offmeshMetadata.terrainAY, offmeshMetadata.terrainBY);
                    }
                }
            }
            foreach (GameObject go in navObsGameObjects)
            {
                foreach (NavmeshObstructMetadata offmeshMetadata in obstructions)
                {
                    bool create = false;
                    foreach (string s in offmeshMetadata.prefabs)
                    {
                        if (go.name.Contains(s)) { create = true; }
                    }
                    if (create)
                    {
                        GameObject parent = new GameObject();
                        parent.transform.parent=go.transform;
                        parent.transform.position = go.transform.TransformPoint(offmeshMetadata.position);
                        parent.transform.Rotate(go.transform.rotation.eulerAngles);
                        parent.transform.Rotate(offmeshMetadata.rotation);
                        createNMO(parent, offmeshMetadata);


                    }
                }
            }
        }
        [Command("point1")]
        void pt1(IPlayer player, string command, string[] args)
        {
            pt1v = (player.Object as BasePlayer).transform.position;
            player.Message("Generated point1 "+pt1v);
        }
        [Command("point2")]
        void pt2(IPlayer player, string command, string[] args)
        {
            pt2v = (player.Object as BasePlayer).transform.position;
            player.Message("Generated point2 "+pt2v);
        }
        [Command("generate")]
        void pgeneratet2(IPlayer player, string command, string[] args)
        {
            if (pt1v == new Vector3(0, 0, 0) || pt2v == new Vector3(0, 0, 0)) return;
            omLink=createOffmesh(pt1v,pt2v,true);
            player.Message("Generated Link");

        }
        [Command("generatejump")]
        void pgeneratet2jump(IPlayer player, string command, string[] args)
        {
            if (pt1v == new Vector3(0, 0, 0) || pt2v == new Vector3(0, 0, 0)) return;
            omLink=createOffmesh(pt1v,pt2v,false);
            player.Message("Generated Link");

        }
        OffMeshLink createOffmesh(Vector3 pt1, Vector3 pt2, bool bidirectional = false, bool anchorA = false, bool anchorB = false) {

            if (pt1 == new Vector3(0, 0, 0) || pt2 == new Vector3(0, 0, 0)) return null;
            Vector3 finalPt1 = new Vector3();
            Vector3 finalPt2 = new Vector3();
            if (anchorA)
                RadialPoint(out finalPt1, pt1, pt1, true, 0.0f, 0.0f);
            else
                RadialPoint(out finalPt1, pt1, pt1, false, 0.0f, 0.0f);
            if (anchorB)
                RadialPoint(out finalPt2, pt2, pt2,  true, 0.0f, 0.0f);
            else
                RadialPoint(out finalPt2, pt2, pt2, false, 0.0f, 0.0f);
            offmesh = new GameObject();
            offmesh.transform.position = pt1;
            GameObject endpoint = new GameObject();
            endpoint.transform.position = pt2;
            omLink = offmesh.AddComponent<OffMeshLink>();
            omLink.startTransform = offmesh.transform;
            omLink.endTransform = endpoint.transform;
            omLink.activated = true;
            omLink.biDirectional = bidirectional;
            omLink.area = 0;
            omLink.enabled = true;
            return omLink;
        }
        private void createNMO(GameObject parent,NavmeshObstructMetadata nmometa)
        {
            Collider col = parent.GetComponentInChildren<Collider>();

            Bounds b = new Bounds(new Vector3(0, 0, 0), new Vector3(10, 5, 10));
            if (col != null) b = col.bounds;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("Adding Monument NMO for " + parent.transform.parent.name.Split("/\\".ToCharArray()).Last());
            Console.ResetColor();
            float min = (b.extents.x > b.extents.z ? b.extents.x : b.extents.z);
            try
            {
                NavMeshObstacle nmo = parent.gameObject.GetComponent<NavMeshObstacle>();
                nmo = (nmo == null ? parent.gameObject.AddComponent<NavMeshObstacle>() : nmo);
                nmo.shape = NavMeshObstacleShape.Box;
                nmo.center = new Vector3(0, 0, 0);
                nmo.size = nmometa.scale;
                nmo.carving = true;
                nmo.enabled = true;
                Console.Write(" - ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Success");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.Write(" - ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failure");
                Console.ResetColor();
            }
        }

        static bool RadialPoint(out Vector3 outvect, Vector3 target, Vector3 self, bool terrain = false, float minDist = 5, float maxDist = 8, int areamask = 25)
        {

            float dist = UnityEngine.Random.Range(minDist, maxDist);
            float angle = UnityEngine.Random.Range(-360f, 360f);
            float x = dist * Mathf.Cos(angle * Mathf.Deg2Rad);
            float y = dist * Mathf.Sin(angle * Mathf.Deg2Rad);
            Vector3 newPosition = target;
            newPosition.x += x;
            newPosition.z += y;
            newPosition.y = target.y + (UnityEngine.Random.Range(-5, 6));
            if (terrain)
            {
                newPosition.y = Terrain.activeTerrain.SampleHeight(newPosition);
            }
            else
            {
                UnityEngine.AI.NavMeshHit nmh = new UnityEngine.AI.NavMeshHit();
                NavMesh.SamplePosition(newPosition, out nmh, 20, areamask);
                newPosition = nmh.position;
            }
            outvect = newPosition;
            return true;
        }
    }
}