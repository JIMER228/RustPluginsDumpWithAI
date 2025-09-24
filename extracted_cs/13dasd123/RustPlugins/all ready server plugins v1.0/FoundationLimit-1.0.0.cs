using System.Collections.Generic;
using Facepunch;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("FoundationLimit", "LAGZYA", "1.0.0")]
    public class FoundationLimit : RustPlugin
    {
        private static int construction = LayerMask.GetMask("Construction");

        private object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            List<BaseEntity> entList = new List<BaseEntity>();
            Puts(T(target.GetWorldPosition(), entList).ToString());  
            return null;
        }

        int T(Vector3 pos, List<BaseEntity> entList)
        {
            List<BaseEntity> list = Pool.GetList<BaseEntity>(); 
            Vis.Entities(pos, 5f, list, construction);
            foreach (var baseEntity in list)
            {
                if(!entList.Contains(baseEntity))
                {
                    entList.Add(baseEntity);
                    T(baseEntity.transform.position, entList);
                }
            }
            return entList.Count;
        }
    }
}
 
