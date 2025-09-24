using Oxide.Core.Plugins;
using ProtoBuf;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealTurretManager", "Kira", "1.0.0")]
    public class ZealTurretManager : RustPlugin
    {
        private object OnSwitchToggle(ElectricSwitch electricSwitch, BasePlayer player)
        {
            if (!(electricSwitch.GetParentEntity() is AutoTurret)) return null;
            var turret = electricSwitch.GetParentEntity() as AutoTurret;
            if (turret == null) return null;
            if (!turret.IsAuthed(player)) return 0;
            turret.SetIsOnline(!electricSwitch.IsOn());

            return null;
        } 

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            if ((go.ToBaseEntity() is AutoTurret)) AddSwitchTurret(plan, go);
            if ((go.ToBaseEntity() is ModularCarGarage)) AddSwitchCarLift(plan, go);
            if ((go.ToBaseEntity() is SamSite)) AddSwitchSamSite(plan, go);
        }

        private void AddSwitchTurret(Planner plan, GameObject go)
        {
            if ((plan == null) | (go == null)) return;
            if (!go.ToBaseEntity().IsFullySpawned()) return; 

            NextTick((() => go.ToBaseEntity().Kill()));
            var player = plan.GetOwnerPlayer();
            var turret = GameManager.server.CreateEntity("assets/prefabs/npc/autoturret/autoturret_deployed.prefab",
                go.transform.position, go.transform.rotation) as AutoTurret;
            if (ReferenceEquals(turret, null)) return;
            var transform = turret.transform;
            var electricSwitch = GameManager.server.CreateEntity(
                "assets/prefabs/deployable/playerioents/simpleswitch/switch.prefab",
                transform.position) as ElectricSwitch;

            turret.creatorEntity = player;
            turret.authorizedPlayers.Add(new PlayerNameID
            {
                userid = player.userID,
                username = player.displayName
            });
            turret.Spawn();
            turret.SetParent(player.GetParentEntity());
            RemoveColliderProtection(electricSwitch);
            if (ReferenceEquals(electricSwitch, null)) return;
            electricSwitch.Spawn();
            electricSwitch.creatorEntity = player;
            electricSwitch.SetParent(turret);
            var switchTransform = electricSwitch.transform;
            switchTransform.localPosition = new Vector3(0.095f, -0.64f, -0.3f);
        }

        private void AddSwitchSamSite(Planner plan, GameObject go)
        {
            var transform = go.transform;
            var samSite = GameManager.server.CreateEntity(
                "assets/prefabs/npc/sam_site_turret/sam_site_turret_deployed.prefab", transform.position,
                transform.rotation) as SamSite;
            var electricSwitch = GameManager.server.CreateEntity(
                "assets/prefabs/deployable/playerioents/simpleswitch/switch.prefab",
                transform.position, transform.rotation) as ElectricSwitch;
            samSite.Spawn();
            RemoveColliderProtection(electricSwitch);
            var switchTransform = electricSwitch.transform;
            switchTransform.localRotation = Quaternion.Euler(new Vector3(33, 180, 0));
            switchTransform.localPosition = new Vector3(1.095f, -0.64f, -0.3f);
            electricSwitch.SetParent(samSite);
            electricSwitch.Spawn();
            NextTick((() => go.ToBaseEntity().Kill()));
        }

        private void AddSwitchCarLift(Planner plan, GameObject go)
        {
            var carLift = go.ToBaseEntity() as ModularCarGarage;
            if (!ReferenceEquals(carLift, null)) carLift.UpdateFromInput(25, 0);
        }

        public void AuthTurret(AutoTurret turret, ulong[] friends)
        {
            if (turret == null | friends == null)
            {
                PrintError($"При авторизации в турели произошла ошибка, турель не существует");
                return;
            }

            foreach (var friend in friends)
            {
                if (BasePlayer.FindByID(friend) == null)
                {
                    turret.authorizedPlayers.Add(new PlayerNameID
                        {userid = friend, username = covalence.Players.FindPlayerById(friend.ToString()).Name});
                }
                else
                {
                    turret.authorizedPlayers.Add(new PlayerNameID
                        {userid = friend, username = BasePlayer.FindByID(friend).displayName});
                }
            }
        }
        
        private static void RemoveColliderProtection(BaseEntity ent)
        {
            foreach (var meshCollider in ent.GetComponentsInChildren<MeshCollider>())
                UnityEngine.Object.DestroyImmediate(meshCollider);

            UnityEngine.Object.DestroyImmediate(ent.GetComponent<GroundWatch>());
        }
    }
}