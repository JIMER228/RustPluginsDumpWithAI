using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("PPeace", "CODERUST.SPACE", "1.0.0")]
      //  Слив плагинов server-rust by Apolo YouGame
    class PPeace : RustPlugin
    {
        /*
         * ------http://coderust.space-----
         */
        void OnPlayerSleepEnded(BasePlayer player)
        {
            DrawUI(player);
        }

        void OnServerInitialized()
        {
            PrintWarning("Последующее обновление будет только у нас на форуме http://coderust.space");
            timer.Every(5, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                    DrawUI(player);
                foreach (var player in BasePlayer.sleepingPlayerList)
                    DrawUI(player);
            });
        }

        const string Layer = "lay";
        private void DrawUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.012018", AnchorMax = "0.5 0.012018", OffsetMin = "-200 -8", OffsetMax = "181 9" },
                CursorEnabled = false,
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "1 1 1 0" },
                Text = { Text = $"Общий онлайн: <color=#5f7489>{BasePlayer.activePlayerList.Count}</color> | Спящих игроков: <color=#5f7489>{BasePlayer.sleepingPlayerList.Count}</color> | Заходят: <color=#5f7489>{SingletonComponent<ServerMgr>.Instance.connectionQueue.joining.Count}</color>", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Font = "robotocondensed-regular.ttf" }
            }, Layer);

            CuiHelper.AddUi(player, container);
        }
    }
}
