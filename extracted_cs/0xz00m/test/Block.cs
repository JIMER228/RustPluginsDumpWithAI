using System;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Block", "YourName", "1.0.0")]
    [Description("Блокирует действия игроков при нулевой энергии")]
    public class Block : RustPlugin
    {
        [PluginReference]
        private Plugin EnergySystem;

        #region Plugin Hooks
        private void Init()
        {
            Puts("Block плагин загружен");
        }

        private void OnServerInitialized()
        {
            // Проверяем, что EnergySystem плагин загружен
            if (EnergySystem == null)
            {
                PrintError("EnergySystem плагин не найден! Block плагин не будет работать корректно.");
                return;
            }
            
            Puts("Block плагин успешно подключен к EnergySystem");
        }

        // Хук для блокировки экипировки предметов
        private object CanEquipItem(PlayerInventory inventory, Item item, int targetPos)
        {
            if (inventory?.baseEntity == null) return null;
            
            BasePlayer player = inventory.baseEntity as BasePlayer;
            if (player == null) return null;

            // Получаем энергию игрока через API EnergySystem
            float playerEnergy = GetPlayerEnergy(player.userID);
            
            // Если энергия равна 0, блокируем экипировку
            if (playerEnergy <= 0f)
            {
                SendReply(player, "<color=red>У вас недостаточно энергии для экипировки предметов!</color>");
                return false;
            }

            return null; // Разрешаем экипировку
        }
        #endregion

        #region Helper Methods
        private float GetPlayerEnergy(ulong playerId)
        {
            if (EnergySystem == null) return 100f; // Возвращаем положительное значение если плагин не загружен
            
            try
            {
                // Вызываем API метод EnergySystem плагина
                var result = EnergySystem.Call("GetPlayerEnergy", playerId);
                if (result != null && result is float energy)
                {
                    return energy;
                }
            }
            catch (Exception ex)
            {
                PrintError($"Ошибка при получении энергии игрока {playerId}: {ex.Message}");
            }
            
            return 100f; // Возвращаем положительное значение в случае ошибки
        }
        #endregion

        #region Commands
        [ChatCommand("block_test")]
        private void TestBlockCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                SendReply(player, "Только администраторы могут использовать эту команду!");
                return;
            }

            float energy = GetPlayerEnergy(player.userID);
            SendReply(player, $"Ваша текущая энергия: {energy}");
            
            if (energy <= 0f)
            {
                SendReply(player, "<color=red>Экипировка заблокирована из-за нулевой энергии!</color>");
            }
            else
            {
                SendReply(player, "<color=green>Экипировка разрешена!</color>");
            }
        }
        #endregion
    }
} 