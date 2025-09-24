namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("HomesGUI", "https://discord.gg/dNGbxafuJn", "1.0.0")]
    public class HomesGUI : RustPlugin
    {
        System.Collections.Generic.Dictionary<string, UnityEngine.Vector3> GetPlayerHomes(string player) => (System.Collections.Generic.Dictionary<string, UnityEngine.Vector3>)plugins?.Find("TpController")?.Call("GetPlayerHomes", player);
    }
}
