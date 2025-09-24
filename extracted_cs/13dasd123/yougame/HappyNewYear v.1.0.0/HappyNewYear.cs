// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;

namespace Oxide.Plugins
{
    [Info("happy new year", "kill me", "1.0.0")]
    class HappyNewYear : RustPlugin
    {
        #region Config

        private float TimeDrop = 3600f;
        private string ItemShortName = "xmas.present.large";
        private int ItemCol = 1;

        private void LoadDefaultConfig()
        {
            GetConfig("Setting", "after how much time the player is given a gift in seconds | через сколько времени игроку дается подарок в секундах", ref TimeDrop);
            GetConfig("Setting", "shortname item | шортайди предмета", ref ItemShortName);
            GetConfig("Setting", "number item | Кол-во предметов", ref ItemCol);
            SaveConfig();
        }

        #endregion

        #region Hooks

        private void OnServerInitialized()
        {
            LoadDefaultConfig();
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() =>
                {
                    OnPlayerInit(player);
                    return;
                });
            }

            DrawGameTip(player);
        }

        [ChatCommand("ttg")]
        private void CmdTesting(BasePlayer player)
        {
            DrawGameTip(player);
        }

        private void DrawGameTip(BasePlayer player)
        {
            player.SendConsoleCommand("gametip.showgametip", "Санта: ХО - ХО - ХО! Получай подарки за игру на сервере!");
            timer.Once(5f, () => player.SendConsoleCommand("gametip.hidegametip"));

            timer.Repeat(TimeDrop, 0, () =>
            {
                player.inventory.GiveItem(ItemManager.CreateByName(ItemShortName, ItemCol));
                player.SendConsoleCommand("gametip.showgametip", "Санта: ХО - ХО - ХО!");
                timer.Once(5f, () => player.SendConsoleCommand("gametip.hidegametip"));
            });
        }

        #endregion

        #region Helpers

        private void GetConfig<T>(string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[menu, Key], typeof(T));
            }

            Config[menu, Key] = var;
        }

        #endregion
    }
}
