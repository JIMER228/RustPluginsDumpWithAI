using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("DroneStrike", "Sempai#3239", "1.0.1")]
    [Description("Call is drones that rain death on your enemies! ")]
    public class DroneStrike : CovalencePlugin
    {
        private static int _groundLayer = LayerMask.GetMask("Terrain", "Default", "Construction", "Deployed", "Construction Trigger", "Prevent Building", "Trigger", "Invisible");
        public static Oxide.Core.Libraries.Timer customTimer = Interface.Oxide.GetLibrary<Oxide.Core.Libraries.Timer>();

        string[] bombs = new string[]
        {
            "assets/prefabs/weapons/beancan grenade/grenade.beancan.deployed.prefab",
            "assets/prefabs/weapons/f1 grenade/grenade.f1.deployed.prefab",
            "assets/prefabs/ammo/rocket/rocket_basic.prefab",
            "assets/prefabs/ammo/rocket/rocket_fire.prefab",
            "assets/prefabs/ammo/rocket/rocket_hv.prefab",
            "assets/prefabs/weapons/satchelcharge/explosive.satchel.deployed.prefab",
            "assets/prefabs/tools/c4/explosive.timed.deployed.prefab"
        };
        
        Dictionary<BasePlayer, Strike> activeStrikes = new Dictionary<BasePlayer, Strike>();
        private void Init()
        {
            permission.RegisterPermission("DroneStrike.use", this);
        }

        object OnMapMarkerAdd(BasePlayer player, MapNote note)
        {
            if (activeStrikes.ContainsKey(player))
            {
                Strike strike = activeStrikes[player];

                if (!strike.start.HasValue)
                {
                    strike.start = note.worldPosition;
                    Effect effect = new Effect();
                    effect.Init(Effect.Type.Generic, player.transform.position, Vector3.zero);
                    effect.pooledString = "assets/prefabs/npc/autoturret/effects/targetacquired.prefab";
                    EffectNetwork.Send(effect, player.net.connection);
                }
                else if (strike.start.HasValue && !strike.end.HasValue)
                {
                    Effect effect = new Effect();
                    effect.Init(Effect.Type.Generic, player.transform.position, Vector3.zero);
                    effect.pooledString = "assets/prefabs/npc/autoturret/effects/targetacquired.prefab";
                    EffectNetwork.Send(effect, player.net.connection);

                    strike.end = note.worldPosition;
                    StartDroneStike(strike);
                    activeStrikes.Remove(player);
                }
            }
            
            return null;
        }
        [Command("AddStrike")]
        private void AddStrike(IPlayer iplayer, string command, string[] args)
        {
            if (iplayer.HasPermission("DroneStrike.use"))
            {
                
                if (args.Length < 6)
                {
                    iplayer.Reply("Some arguments are missing! Format \n/AddStrike [player] [type] [range] [bombSpeed] [heightAboveGround] [droneCount]");
                    return;
                }
                int type;
                int range;
                float bombSpeed;
                int hieghtAboveGround;
                int amountOfDrones;
                BasePlayer striker = BasePlayer.Find(args[0]);
                if (striker == null || !striker.IsConnected) { iplayer.Reply("Could not find this player or they are not connected!"); return; }

                if (!int.TryParse(args[1], out type)) { iplayer.Reply($"Error in args: Invalid type, Type must be a number from  0-{bombs.Length}"); return; }

                if (type < 0 || type >= bombs.Length) { iplayer.Reply($"Error in args: Invalid type, Type must be a number from  0-{bombs.Length - 1}"); return; }

                if (!int.TryParse(args[2], out range)) { iplayer.Reply($"Error in args: Invalid range, Range must be a number"); return; }

                if (!float.TryParse(args[3], out bombSpeed)) { iplayer.Reply($"Error in args: Invalid range, Range must be a number"); return; }
                if (!int.TryParse(args[4], out hieghtAboveGround)) { iplayer.Reply($"Error in args: Invalid height, Height must be a number"); return; }
                if (!int.TryParse(args[5], out amountOfDrones)) { iplayer.Reply($"Error in args: Invalid amount of drones, Amount of drones must be a number"); return; }

                if (activeStrikes.ContainsKey(striker))
                {
                    striker.ChatMessage("You already have a airstrike pending!");
                    return;
                }
                activeStrikes.Add(striker, new Strike(striker, bombs[type], range, bombSpeed, hieghtAboveGround, amountOfDrones));
                striker.ChatMessage("<color=#59981A>Air Strike Succesfully redeemed!</color>\n Open your map and right click the map to set the start point of the AIRSTRIKE and then right click another spot to set the end zone!");
                return;
            }
            iplayer.Reply("You dont have permission!");
        }
        private void StartDroneStike(Strike strike)
        {
            server.Broadcast($"<size=25>{strike.player.displayName} has just called in an Drone Strike\n TAKE COVER - Check your map for the Drone Strikes route!</size>");
            strike.StartStrike();
        }
        public class Strike
        {
            Oxide.Core.Libraries.Timer.TimerInstance bombTimer;
            Oxide.Core.Libraries.Timer.TimerInstance hieghtTimer;

            float heightAboveGround = 30.0f;
            int amountOfDrones;
            float bombSpeed;

            List<DroneBomber> drones = new List<DroneBomber>();
            List<MapMarkerExplosion> markers = new List<MapMarkerExplosion>();
            public Vector3? start { get; set; }
            public Vector3? end { get; set; }
            public BasePlayer player { get; set; }
            string prefab { get; set; }
            int distance { get; set; }
            public Strike(BasePlayer player, string prefab, int range,float bombSpeed,int heightAboveGround,int amountOfDrones)
            {
                this.player = player;
                this.prefab = prefab;
                this.distance = range;
                this.bombSpeed = bombSpeed;
                this.heightAboveGround = heightAboveGround;
                this.amountOfDrones = amountOfDrones;
                Effect effect = new Effect();
                effect.Init(Effect.Type.Generic, player.transform.position, Vector3.zero);
                effect.pooledString = "assets/prefabs/misc/easter/painted eggs/effects/eggpickup.prefab";
                EffectNetwork.Send(effect, player.net.connection);
            }
            public void StartStrike()
            {
                Vector3 dir = end.Value - start.Value;

                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.forward);
                targetRot = Quaternion.Euler(0, targetRot.eulerAngles.y, 0);

                float totalLineDistance = Vector3.Distance(start.Value, end.Value);

                if (totalLineDistance > distance)
                {
                    player.ChatMessage($"The strike path is too long! Your path is {totalLineDistance} but you only have {distance} meters. Ending stike early");
                    Vector3 newEnd = start.Value + (dir.normalized * distance);
                    end = newEnd;
                    totalLineDistance = Vector3.Distance(start.Value, end.Value);
                }

                MapMarkerExplosion marker = GameManager.server.CreateEntity("assets/prefabs/tools/map/explosionmarker.prefab", start.Value, new Quaternion(), true) as MapMarkerExplosion;
                marker.Spawn();
                markers.Add(marker);

                MapMarkerExplosion marker2 = GameManager.server.CreateEntity("assets/prefabs/tools/map/explosionmarker.prefab", end.Value, new Quaternion(), true) as MapMarkerExplosion;
                marker2.Spawn();
                markers.Add(marker2);

                int numberOfPoints = Mathf.FloorToInt(totalLineDistance / 30);
                float actualSpacing = totalLineDistance / numberOfPoints;

                for (int i = 1; i < numberOfPoints; i++)
                {
                    Vector3 newPoint = GetPointOnLine(start.Value, end.Value, i * actualSpacing / totalLineDistance);

                    MapMarkerExplosion marker3 = GameManager.server.CreateEntity("assets/prefabs/tools/map/explosionmarker.prefab", newPoint, new Quaternion(), true) as MapMarkerExplosion;
                    marker3.Spawn();
                    markers.Add(marker3);
                }

                DroneBomber drone = new DroneBomber(start.Value, end.Value, targetRot, prefab, heightAboveGround,player);
                drones.Add(drone);
                for (int i = 1; i < amountOfDrones; i++)
                {
                    Vector3 worldPos = start.Value + (targetRot * new Vector3(-5 * i, 0, -5 * i));
                    Vector3 finishPos = end.Value + (targetRot * new Vector3(-5 * i, 0, -5 * i));
                    drones.Add(new DroneBomber(worldPos, finishPos, targetRot, prefab, heightAboveGround,player));

                    Vector3 worldPos2 = start.Value + (targetRot * new Vector3(5 * i, 0, -5 * i));
                    Vector3 finishPos2 = end.Value + (targetRot * new Vector3(5 * i, 0, -5 * i));
                    drones.Add(new DroneBomber(worldPos2, finishPos2, targetRot, prefab, heightAboveGround,player));
                }
                DropBomb();
                HeightCheck();
            }
            private Vector3 GetPointOnLine(Vector3 x, Vector3 y, float normalizedDistance)
            {
                return x + (y - x) * normalizedDistance;
            }
            private void Destroy()
            {
                foreach (DroneBomber dr in drones)
                {
                    dr.KillDrone();
                }
                foreach (MapMarkerExplosion mark in markers)
                {
                    mark.Kill();
                }
                bombTimer.Destroy();
                hieghtTimer.Destroy();
            }
            private void DropBomb()
            {
                bombTimer = customTimer.Repeat(bombSpeed, 0, () =>
                  {
                      int count = 0;
                      float time = 0.0f;
                      bool droneAlive = false;
                      foreach (DroneBomber d in drones)
                      {
                          if (d.drone == null || d.drone.IsDestroyed) continue;
                         
                          if (Vector3.Distance(d.drone.transform.position, d.drone.targetPosition.Value) < 3.0f)
                          {
                              Destroy();
                              return;
                          }

                          if (count % 2 != 0)
                          {
                              time = time + 0.2f;
                          }
                          customTimer.Once(time, () =>
                          {
                              if (d != null)
                                  d.DropBomb();

                          });
                          count++;
                          droneAlive = true;
                      }
                      if (!droneAlive) Destroy();
                  });
            }
            private void HeightCheck()
            {
                hieghtTimer = customTimer.Repeat(0.25f, 0, () =>
                {
                    foreach (DroneBomber d in drones)
                    {
                        if (d.drone == null) continue;
                        d.HeightUpdate();
                    }
                });
            }
        }

        public class DroneBomber
        {
            BasePlayer owner;
            Vector3 startPoint;
            Vector3 endPoint;
            Vector3 direction;
            float heightAboveGround;
            public Drone drone { get; private set;}
            string bomb;

            public Vector3 lastDropPoint { get; private set; }
            public Vector3 EndPoint { get { return endPoint; } set { endPoint = value; } }

            public DroneBomber(Vector3 startPoint, Vector3 endPoint, Quaternion rot, string prefab, float heightAboveGround,BasePlayer owner)
            {
                this.heightAboveGround = heightAboveGround;
                this.direction = endPoint - startPoint;
                this.bomb = prefab;
                this.startPoint = startPoint;
                this.EndPoint = endPoint;
                this.owner = owner;
                float height = TerrainMeta.HeightMap.GetHeight(startPoint);
                if (height < 5)
                    height = 5;
                drone = GameManager.server.CreateEntity("assets/prefabs/deployable/drone/drone.deployed.prefab", new Vector3(startPoint.x,height + heightAboveGround, startPoint.z), rot, true) as Drone;

                drone.targetPosition = endPoint;
                drone.altitudeAcceleration = 50;
                drone.movementAcceleration = 5f;
                drone.Spawn();
                drone.keepAboveTerrain = true;
            }
            public void DropBomb()
            {
               
                if (drone == null) return;  
                
                var rocket = GameManager.server.CreateEntity(bomb, drone.transform.position - (Vector3.up * 5), Quaternion.Euler(0, 45, 0), true);
                rocket.creatorEntity = (BaseEntity)owner;
                rocket.OwnerID = owner.userID;
                rocket.Spawn();

                lastDropPoint = drone.transform.position;
            }
            private Vector3 GetGround(Vector3 pos)
            {
                RaycastHit hitInfo;
                pos += new Vector3(0, 100, 0);

                if (Physics.Raycast(pos, Vector3.down, out hitInfo, 500))
                    return hitInfo.point;

                return new Vector3();
            }
            public void HeightUpdate()
            {
               
                Vector3 newEnd = drone.transform.position + (direction.normalized * 4) + (Vector3.up * 20);
                float height = 5;
                

                height = GetGround(newEnd).y;
                float h2 = FindHieghtBuildingBlock();
                if (height < h2)
                    height = h2;
                    //height = TerrainMeta.HeightMap.GetHeight(newEnd);

                if (height < 5)
                    height = 5;
                drone.targetPosition = new Vector3(EndPoint.x, height + heightAboveGround, EndPoint.z);
            }
            public void KillDrone()
            {
                if (drone != null && !drone.IsDestroyed) drone.Kill();
            }

            private float FindHieghtBuildingBlock()
            {
                OBB obb = drone.WorldSpaceBounds();
                System.Collections.Generic.List<BuildingBlock> list = Facepunch.Pool.GetList<BuildingBlock>();
                Vis.Entities<BuildingBlock>(obb.position, 16f + obb.extents.magnitude, list, 2097152);
                float height = 0;
                for (int index = 0; index < list.Count; ++index)
                {
                    
                    BuildingBlock buildingBlock2 = list[index];
                    if (height < buildingBlock2.transform.position.y)
                        height = buildingBlock2.transform.position.y;
                }
                Facepunch.Pool.FreeList<BuildingBlock>(ref list);
                if (height < drone.transform.position.y)
                {
                    
                    return 0;
                }
                return height;
            }


        }
        
    
    }
}