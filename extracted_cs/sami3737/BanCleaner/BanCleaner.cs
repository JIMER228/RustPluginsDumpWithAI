namespace Oxide.Plugins
{
    [Info("BanCleaner", "sami37", "1.0.0")]
    public class BanCleaner : RustPlugin
    {
        void OnUserBanned(string name, string id, string address, string reason)
        {
            BasePlayer player = BasePlayer.Find(id);
            if (player != null)
                DestroyEntity(player);
        }

        private void DestroyEntity(BasePlayer player)
        {
            //if (player.net.connection.authLevel == 0)
            foreach (var entity in UnityEngine.Object.FindObjectsOfType<BaseEntity>())
                if (entity.OwnerID == player.userID)
                    entity.Kill();
        }
    }
}