// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Plugins.TeamBattle.Data;
using Oxide.Plugins.TeamBattle.Systems;
using Oxide.Plugins.TeamBattle.UI;

namespace Oxide.Plugins.TeamBattle
{
    public partial class TeamBattle : RustPlugin
    {
        private void InitDependencies()
        {
            // Инициализация всех систем
            Configuration.Init(this);
            PlayerData.Init(this);
            RoomData.Init(this);
            SpawnData.Init(this);
            TeleportSystem.Init(this);
            RespawnSystem.Init(this);
            GameSystem.Init(this);
            TimerSystem.Init(this);
            
            // Инициализация UI
            RoomSelectionUI.Init(this);
            TeamSelectionUI.Init(this);
            
            // Инициализация команд
            PlayerCommands.Init(this);
            AdminCommands.Init(this);
        }

        void Init() => InitDependencies();
        
        void Unload()
        {
            // Очистка всех систем
            TimerSystem.Unload();
            RespawnSystem.Unload();
            RoomSelectionUI.Unload();
            TeamSelectionUI.Unload();
            
            // Дополнительная очистка...
        }
        
        void OnPlayerDisconnected(BasePlayer player)
        {
            PlayerData.OnPlayerDisconnected(player);
            RoomData.OnPlayerDisconnected(player);
            RespawnSystem.OnPlayerDisconnected(player);
        }
    }
}