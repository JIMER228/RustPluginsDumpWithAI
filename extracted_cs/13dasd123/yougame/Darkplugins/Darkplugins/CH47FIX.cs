namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("CH47FIX", "https://discord.gg/dNGbxafuJn", "1.0.0")]
    // 06D 03M 2019Y
    // CH47FIX
    //    ___    ___    ___    ____  _____  __  __   _  __
    //   / _ \  / _ |  / _ \  / __/ / ___/ / / / /  / |/ /
    //  / , _/ / __ | / , _/ / _/  / (_ / / /_/ /  /    / 
    // /_/|_| /_/ |_|/_/|_| /___/  \___/  \____/  /_/|_/  ^
	
    public class CH47FIX : RustPlugin
    {
		private object CanMountEntity(BasePlayer d, BaseNetworkable e)
		{
			if (d != null && !d.IsAdmin && d.userID >= 76560000000000000L && !d.IsNpc && d.GetComponent<NPCPlayer>() == null && d.GetComponent<BaseNpc>() == null && e.GetParentEntity() is CH47HelicopterAIController) return false;
			return null;
		}
    }
}