using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("LadderFix", "https://discord.gg/dNGbxafuJn", "1.0.3")]
    class LadderFix : RustPlugin
    {				
		
		private static List<ulong> LastDetect = new List<ulong>();
		
		private void OnServerInitialized() => RunCheckLadder();				
		
		private void RunCheckLadder()
		{							
			var affectPlayers = BasePlayer.activePlayerList.Where(x=> !x.IsSleeping() && !x.IsDead() && !x.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot) && x.modelState.onLadder && x.FindTrigger<TriggerLadder>() == null).ToList();
			foreach(var player in affectPlayers)
			{
				if (LastDetect.Contains(player.userID))		
				{					
					Puts($"Игрок {player.displayName} ({player.userID}) был кикнут за использование Ladder Hack");
					player.Kick("ladder hack");				
				}
				else					
					LastDetect.Add(player.userID);
			}		
			LastDetect.RemoveAll(x=> !affectPlayers.Exists(y=> y.userID == x));						
			timer.Once(4f, RunCheckLadder);
		}				
		
	}	
}	