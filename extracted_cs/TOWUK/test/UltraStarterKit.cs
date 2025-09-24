using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("UltraStarterKit", "YourName", "1.6.1")]
    [Description("Оптимизированный стартовый набор")]
    public class UltraStarterKit : RustPlugin
    {
        const string PERM = "ultrastarterkit.use";
        readonly string[] _clothes = { "burlap.headwrap", "burlap.shirt", "burlap.gloves.new", "burlap.trousers", "burlap.shoes" };
        readonly string[] _tools = { "concretehatchet", "concretepickaxe" };

        void Init() => permission.RegisterPermission(PERM, this);
        
        void OnPlayerInit(BasePlayer p) => GiveKit(p);
        void OnPlayerRespawned(BasePlayer p) => GiveKit(p);

        void GiveKit(BasePlayer p)
        {
            if (p?.IsConnected != true || !permission.UserHasPermission(p.UserIDString, PERM)) return;

            p.inventory.Strip();
            
            // Одежда
            foreach (var item in _clothes)
                ItemManager.CreateByName(item, 1)?.MoveToContainer(p.inventory.containerWear);
            
            // Инструменты
            foreach (var tool in _tools)
            {
                var item = ItemManager.CreateByName(tool, 1);
                item.condition = item.maxCondition;
                item?.MoveToContainer(p.inventory.containerBelt);
            }

            p.SendNetworkUpdateImmediate();
            p.ChatMessage("Стартовый набор получен!");
        }
    }
}