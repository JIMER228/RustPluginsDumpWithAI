// Requires: ZealAlbionInfoPanel

using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("ZealRemove", "Kira", "1.0.0")]
    public class ZealRemove : RustPlugin
    {
        private static ZealAlbionInfoPanel ZealAlbionInfoPanel;
        void OnServerInitialized()
        {
            ZealAlbionInfoPanel = (ZealAlbionInfoPanel);
            plugins.Find(nameof(ZealAlbionInfoPanel));
        }

        [ChatCommand("remove")]
        private void CommandRemove(BasePlayer player)
        {
        }

        object OnHammerHit(BasePlayer player, HitInfo info)
        {
            /*
            NextTick(() =>
            {
                if (!info.HitEntity) return;
                if (info.HitEntity.OwnerID == player.userID)
                {
                    info.HitEntity.Kill();
                }
                else
                {
                    player.ChatMessage("Вы не являетесь владельцем данного объекта");
                }
            });
            */
            return null;
        }
    }
}