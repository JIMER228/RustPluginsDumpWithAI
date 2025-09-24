// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("NoDud", "Ryamkk", "1.0.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    class NoDud : RustPlugin
    {
        int DudMax = 15;
		int DudMin = 3;
		float ChanceDud = 0f;
		
		private void LoadDefaultConfig()
        {
            GetConfig("Основные настройки", "Минимальная задержка перед взрывом", ref DudMin);
            GetConfig("Основные настройки", "Максимальная задержка перед взрывом", ref DudMax);
			GetConfig("Основные настройки", "Шанс срабатывания осечки (1.0f = 1%)", ref ChanceDud);
            SaveConfig();
        }
		
		void OnServerInitialized() => LoadDefaultConfig();
		
        void OnExplosiveThrown(BasePlayer player, BaseNetworkable baseEnt)
        {
            if (baseEnt is DudTimedExplosive)
            {
                DudTimedExplosive beancanExplosive = baseEnt as DudTimedExplosive;

                beancanExplosive.dudChance = ChanceDud;
                
                NextTick(() =>
                {
                    beancanExplosive.SetFuse(Core.Random.Range(DudMin, DudMax));
                });
            }
        }
		
		private void GetConfig<T>(string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[menu, Key], typeof(T));
            }

            Config[menu, Key] = var;
        }
    }
}
