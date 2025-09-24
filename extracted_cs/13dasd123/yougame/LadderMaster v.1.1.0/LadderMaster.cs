// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Core;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("LadderMaster", "Lomarine", "1.1.0")]
    class LadderMaster : RustPlugin
    {
		[PluginReference] Plugin NoEscape;

		private int LadderMode;
		private bool PrivelegeMode;
		
		void Init()
        {			
			if (!plugins.Exists("NoEscape"))
			{
				PrintError("Отсутствует NoEscape!");
				Interface.Oxide.UnloadPlugin("LadderMaster");
			} 
			LoadDefaultMessages();
			LoadDefaultConfig();
        }
		
		void OnEntityBuilt(Planner planner, GameObject gameobject)
        {
			BaseEntity entity = UnityEngine.GameObjectEx.ToBaseEntity(gameobject);
			
			if(entity is BaseLadder == false)
				return;
			
			BasePlayer player = planner.GetOwnerPlayer();
			
			if(PrivelegeMode && !player.CanBuild())
			{
				entity.Kill();
				player.inventory.GiveItem(ItemManager.CreateByItemID(108061910));
				SendReply(player, lang.GetMessage("L.PRIVELEGE", this, player.UserIDString));
				return;
			}
			
			if(LadderMode == 0)
				return;
			
			if(LadderMode == 3)
			{
				entity.Kill();
				SendReply(player, lang.GetMessage("L.BLOCKED", this, player.UserIDString));
			}
			
			var raid = (bool?)NoEscape.Call("IsRaidBlocked", player);						
			if (raid == null)
			{
				PrintError("Неверная версия NoEscape!");
				return;
			}	
			
			if(LadderMode == 1 && raid == false)
			{
				entity.Kill();
				player.inventory.GiveItem(ItemManager.CreateByItemID(108061910));
				SendReply(player, lang.GetMessage("L.ONLYRAID", this, player.UserIDString));
			}
			if(LadderMode == 2 && raid == true)
			{
				entity.Kill();
				player.inventory.GiveItem(ItemManager.CreateByItemID(108061910));
				SendReply(player, lang.GetMessage("L.ONLYFREE", this, player.UserIDString));
			}
			
        }

		#region Language

		protected override void LoadDefaultMessages()
		{
		    lang.RegisterMessages(new Dictionary<string, string>
		    {
			    ["L.ONLYRAID"] = "<color=#DC143C>[Ladder Master]</color> Штурмовые лестницы можно ставить только во время рейдблока!",
			    ["L.ONLYFREE"] = "<color=#DC143C>[Ladder Master]</color> Штурмовые лестницы нельзя ставитть во время рейдблока!",
				["L.PRIVELEGE"] = "<color=#DC143C>[Ladder Master]</color> Вам нужно право на постройку для установки штурмовой лестницы!",
				["L.BLOCKED"] = "<color=#DC143C>[Ladder Master]</color> Строительство штурмовых лестниц запрещено!",
		    }, this);
	    }

		#endregion

		#region Config

		protected override void LoadDefaultConfig()
        {
            Config["Тип работы лестниц"] = LadderMode = GetConfig("Тип работы лестниц", 0);
			Config["Требование права на постройку"] = PrivelegeMode = GetConfig("Требование права на постройку", false);
            SaveConfig();
        }

		T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

		#endregion
	}
}