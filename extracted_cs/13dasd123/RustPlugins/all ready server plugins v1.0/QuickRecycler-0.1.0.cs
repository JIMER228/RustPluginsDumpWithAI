using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("QuickRecycler", "rostov114", "0.1.0", ResourceId = 000)]
      //  Слив плагинов server-rust by Apolo YouGame
	public class QuickRecycler : RustPlugin
	{
		#region Variables
		bool bonusOn = false;
		string pluginPrefix = "[<color=#fbf6a1>QuickRecycler</color>]:";
		bool broadcastEnabledNight = true;
		Dictionary<string, Dictionary<string, int>> CustomItemsRecycler = new Dictionary<string, Dictionary<string, int>>()
		{
			{"keycard_green.item", new Dictionary<string, int>(){{"scrap", 15}}},
			{"keycard_blue.item", new Dictionary<string, int>(){{"scrap", 40}}},
			{"keycard_red.item", new Dictionary<string, int>(){{"scrap", 80}}},
		};
		#endregion
		
		
        void LoadDefaultMessages()
        {
			lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NightBonusOn"] = "Скорость работы переработчика изменена на <color=#fbf6a1>x5</color>",
                ["NightBonusOff"] = "Скорость работы переработчика изменена на <color=#fbf6a1>x1</color>",
            }, this, "ru");
        }

		#region Hooks
		private void OnRecyclerToggle(Recycler recycler, BasePlayer player)
		{
			timer.Once(0.5f, () =>
			{
				if (recycler.IsOn())
				{
					recycler.CancelInvoke(new Action(recycler.RecycleThink));

					if (bonusOn)
						recycler.InvokeRepeating(new Action(recycler.RecycleThink), 1f, 1f);
					else
						recycler.InvokeRepeating(new Action(recycler.RecycleThink), 5f, 5f);
				}
			});
        }
		
		private object CanRecycle(Recycler recycler, Item slot)
		{
			if (CustomItemsRecycler.ContainsKey(slot.info.name))
				return true;

			return null;
		}
		
		private object OnRecycleItem(Recycler recycler, Item slot)
		{
			if (CustomItemsRecycler.ContainsKey(slot.info.name))
			{
				float num = recycler.recycleEfficiency;
				int num2 = 1;

				if (slot.hasCondition)
					num = Mathf.Clamp01(num * Mathf.Clamp(slot.conditionNormalized * slot.maxConditionNormalized, 0.1f, 1f));

				if (slot.amount > 1)
					num2 = Mathf.CeilToInt(Mathf.Min((float)slot.amount, (float)slot.info.stackable * 0.1f));
				
				foreach (var item in CustomItemsRecycler[slot.info.name])
				{
					int num3 = Mathf.CeilToInt(((float)item.Value * (float)num2) * num);

					if (slot.info.stackable == 1 && slot.hasCondition)
					{
						num3 = Mathf.CeilToInt((float)num3 * slot.conditionNormalized);
					}

					if (num3 >= 1)
					{
						Item newItem = ItemManager.CreateByName(item.Key, num3, 0UL);
						
						if (newItem != null)
							recycler.MoveItemToOutput(newItem);
					}
				}

				slot.UseItem(num2);

				return true;
			}

			return null;
		}

		void OnTimeSunset()
		{
			if (bonusOn) return;
			bonusOn = true;

			if (broadcastEnabledNight == true)
				foreach (BasePlayer player in BasePlayer.activePlayerList)
					rust.SendChatMessage(player, pluginPrefix, lang.GetMessage("NightBonusOn", this, player.UserIDString), "0");
		}

		void OnTimeSunrise()
		{
			if (!bonusOn) return;
			bonusOn = false;

			if (broadcastEnabledNight == true)
				foreach (BasePlayer player in BasePlayer.activePlayerList)
					rust.SendChatMessage(player, pluginPrefix, lang.GetMessage("NightBonusOff", this, player.UserIDString), "0");
		}
		#endregion

	}
}
