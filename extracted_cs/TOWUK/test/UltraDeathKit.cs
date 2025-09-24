using Oxide.Core.Plugins;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("UltraDeathKit", "YourName", "1.6.2")]
    [Description("Стартовый набор с удалением при смерти и индивидуальными пермишенами")]
    public class UltraDeathKit : RustPlugin
    {
        const string PERM_KIT = "ultradeathkit.kit";
        const string PERM_CLEAN = "ultradeathkit.clean";
        const float CLEAN_RADIUS = 3f;
        readonly HashSet<string> _clothes = new HashSet<string> { "burlap.headwrap", "burlap.shirt", "burlap.gloves.new", "burlap.trousers", "burlap.shoes" };
        readonly HashSet<string> _tools = new HashSet<string> { "concretehatchet", "concretepickaxe" };
        HashSet<string> _itemsToRemove => new HashSet<string>(_clothes) { "concretehatchet", "concretepickaxe" };

        void Init() {
            permission.RegisterPermission(PERM_KIT, this);
            permission.RegisterPermission(PERM_CLEAN, this);
        }

        #region Выдача набора
        void OnPlayerInit(BasePlayer p) {
            if (permission.UserHasPermission(p.UserIDString, PERM_KIT)) GiveKit(p);
        }
        
        void OnPlayerRespawned(BasePlayer p) {
            if (permission.UserHasPermission(p.UserIDString, PERM_KIT)) GiveKit(p);
        }
        
        void GiveKit(BasePlayer p) {
            if (p?.IsConnected != true) return;
            p.inventory.Strip();
            foreach (var item in _clothes) ItemManager.CreateByName(item, 1)?.MoveToContainer(p.inventory.containerWear);
            foreach (var tool in _tools) {
                var item = ItemManager.CreateByName(tool, 1);
                if (item != null) { item.condition = item.maxCondition; item.MoveToContainer(p.inventory.containerBelt); }
            }
            p.SendNetworkUpdateImmediate();
            p.ChatMessage("Стартовый набор получен!");
        }
        #endregion

        #region Удаление при смерти
        void OnEntityDeath(BaseCombatEntity e, HitInfo i) {
            var p = e as BasePlayer;
            if (p == null || !permission.UserHasPermission(p.UserIDString, PERM_CLEAN)) return;
            CleanPlayerInventory(p.inventory);
            CleanNearbyItems(p.transform.position);
        }
        
        void CleanPlayerInventory(PlayerInventory i) {
            CleanContainer(i.containerWear);
            CleanContainer(i.containerBelt);
            CleanContainer(i.containerMain);
        }
        
        void CleanContainer(ItemContainer c) {
            if (c == null) return;
            foreach (var item in c.itemList.ToArray()) {
                if (item?.info == null) continue;
                if (_itemsToRemove.Contains(item.info.shortname)) { item.RemoveFromContainer(); item.Remove(); }
            }
        }
        
        void CleanNearbyItems(Vector3 pos) {
            var entities = new List<BaseEntity>();
            Vis.Entities(pos, CLEAN_RADIUS, entities);
            foreach (var e in entities) {
                if (e is DroppedItem di && di.item?.info != null && _itemsToRemove.Contains(di.item.info.shortname)) di.Kill();
            }
        }
        #endregion
    }
}