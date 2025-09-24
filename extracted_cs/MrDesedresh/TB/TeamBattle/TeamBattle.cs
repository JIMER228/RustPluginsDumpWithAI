// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Plugins.TeamBattle.Core;
using Oxide.Plugins.TeamBattle.Data;
using Oxide.Plugins.TeamBattle.Systems;
using Oxide.Plugins.TeamBattle.UI;
using Oxide.Plugins.TeamBattle.Commands;

namespace Oxide.Plugins
{
    [Info("TeamBattle", "YourName", "1.2.4")]
    [Description("Система комнат для командных сражений с таймером, снаряжением и респавном")]
    public class TeamBattle : RustPlugin
    {
        void Init()
        {
            Configuration.Init(this);
            RoomData.Init(this);
            PlayerData.Init(this);
            TeleportSystem.Init(this);
            RoomSelectionUI.Init(this);
            PlayerCommands.Init(this);
        }
        
        void Unload()
        {
            RoomSelectionUI.Unload();
            
            foreach (var timer in PlayerData.RespawnTimers.Values)
            {
                timer?.Destroy();
            }
        }
        
        void OnPlayerDisconnected(BasePlayer player)
        {
            PlayerData.OnPlayerDisconnected(player);
            RoomData.OnPlayerDisconnected(player);
        }
    }
}