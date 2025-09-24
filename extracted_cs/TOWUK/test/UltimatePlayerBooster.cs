using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("UltimatePlayerBooster", "YourName", "2.2.0")]
    [Description("Бустер при инициализации и респавне игрока")]
    public class UltimatePlayerBooster : RustPlugin
    {
        const string BP_PERM = "playerbooster.blueprints", STATS_PERM = "playerbooster.stats";

        void Init() { permission.RegisterPermission(BP_PERM, this); permission.RegisterPermission(STATS_PERM, this); }

        void OnPlayerInit(BasePlayer p) => Boost(p);
        void OnPlayerConnected(BasePlayer p) => Boost(p);
        void OnPlayerRespawned(BasePlayer p) => Boost(p);

        void Boost(BasePlayer p)
        {
            if (p?.IsConnected != true) return;
            if (permission.UserHasPermission(p.UserIDString, BP_PERM)) UnlockBP(p);
            if (permission.UserHasPermission(p.UserIDString, STATS_PERM)) Stats(p);
        }

        void UnlockBP(BasePlayer p)
        {
            bool u = false;
            foreach (var bp in ItemManager.bpList) if (!p.blueprints.IsUnlocked(bp.targetItem)) { p.blueprints.Unlock(bp.targetItem); u = true; }
            if (u) p.ChatMessage("Все блюпринты разблокированы!");
        }

        void Stats(BasePlayer p)
        {
            p.health = p.MaxHealth();
            var m = p.metabolism;
            m.calories.value = m.calories.max;
            m.hydration.value = m.hydration.max;
            p.ChatMessage("Характеристики максимальны!");
        }
    }
}
